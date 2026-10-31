// Online races: one player hosts, the others join them (the user, 2026-10-30), over netkit (NET/: a friend's UDP room library, free to use).
//
// The game's loop stops for seconds at a time -- a scene loading, the pause menu -- and netkit lets a player go after 6 s of silence, so netkit runs on a
// thread of its own here, behind a lock, and the game only reads and writes this file's state.
//
// What crosses the network, by type (first byte):
//   HELLO   reliable   to all, on joining: the protocol, the game's content key, its version and the player's name (netkit calls a host "host")
//   ASK     reliable   host to a new player, with a race that has a password: a number to answer with
//   PASS    reliable   to the host: that number and the password, hashed
//   WRONG   reliable   host to a player it is turning away: its own version, to tell them
//   TRACK   reliable   host to all: the race, its laps and its track's name; to a player joining a race under way, how long it has been going too
//   READY   reliable   to the host: this game's car is at the start
//   GO      reliable   host to all: count down now
//   CAR     as it may  to all, 30 a second: as a ghost's sample, so a car is drawn as a ghost is (RACEMOD.CPP DrawNetCars)
//   EVENT   reliable   to all: a mushroom taken, an item used, a car hit (T_RACENET_EVENT)
//   FINISH  reliable   to all: this player's time
//   PING    as it may  each player to each, once a second, and PONG back: how far behind its samples arrive
// Each car is drawn where it is now, RACENET_DELAY_MS ago at most: cars touch by what the player sees, and at 150 km/h 100 ms is two car lengths. So the clock is the
// sender's, moved on by half the round trip, and past the newest sample the car goes on as it was going (RACENET_AHEAD_MS at most), the jump a late sample
// would make eased out (RACENET_EASE_MS).
#include "NETRACE.H"
#include "NET/netkit.h"
#include "NETUPNP.H"

#include <SDL3/SDL.h>
#include <SYSTEM/LOG.H>

#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#ifdef _WIN32
#include <windows.h>
#endif

#define RACENET_PROTOCOL 2
#define RACENET_DELAY_MS 40                 // how far behind a car is drawn
#define RACENET_AHEAD_MS 250                // how far past its newest sample
#define RACENET_EASE_MS 90.0                // a car's correction eased out over this long ...
#define RACENET_EASE_MOST 1500              // ... up to this much; further is a jump (a rescue)
#define RACENET_PING_MS 1000
#define RACENET_ASK_MS 60000                // how long a player has to give the password
#define RACENET_QUIET_MS 2000               // a car not heard from this long isn't drawn
#define RACENET_GO_WAIT_MS 30000            // the slowest loader waited for this long
#define RACENET_POLL_MS 5
#define RACENET_SAMPLES 16
#define RACENET_TRACK 96
#define RACENET_VERSION 32
#define RACENET_EVENTS 128

enum { M_HELLO = 1, M_TRACK, M_READY, M_GO, M_CAR, M_FINISH, M_ASK, M_PASS, M_WRONG, M_EVENT, M_PING, M_PONG };

typedef struct {
    uint32_t T;                             // the sender's clock
    int32_t X, Y, Z, Speed;
    int16_t Alpha, Beta, Gamma, Body, Anim, Island, Flags;
} T_RN_SAMPLE;

typedef struct {
    uint32_t Id;
    char Name[RACENET_NAME + 1];
    int Ready, Finish, Lap, Progress, Hello;
    int Pending, Admitted;                  // (the host) yet to give the password; let in
    uint32_t Nonce, AskedAt;
    int Rtt;                                // the round trip to it, ms (0: not known yet)
    double ShownX, ShownY, ShownZ;          // where it was last drawn ...
    uint32_t ShownAt;                       // ... and when (0: not yet)
    T_RN_SAMPLE S[RACENET_SAMPLES];
    int N, Head;                            // samples held, and the newest
    int32_t Offset;                         // our clock less the sender's, at its quickest arrival: their clock on ours, plus the fastest trip
    int HasOffset;
    uint32_t HeardAt;
} T_RN_PEER;

static SDL_Mutex *s_lock = NULL;
static SDL_Thread *s_thread = NULL;
static SDL_AtomicInt s_quit;
static struct NetKit *s_kit = NULL;
static T_RN_PEER s_peer[RACENET_MAX_OTHERS];
static int s_nbPeers = 0;
static char s_error[160] = "";
static int s_hosting = 0, s_local = 0;
static char s_name[RACENET_NAME + 1] = "";
static int s_selfReady = 0, s_goSent = 0, s_goPending = 0;
static uint32_t s_announcedAt = 0, s_wentAt = 0, s_pingAt = 0;
static int s_racing = 0;                    // a race announced and not yet gone
static int s_live = 0;                      // (the host) a race under way
static int s_lateMs = 0;                    // this game joined a race under way, this long after its go
static uint32_t s_lateAt = 0;
static uint32_t s_content = 0;
static char s_version[RACENET_VERSION] = "";
static char s_password[64] = "", s_hostVersion[RACENET_VERSION] = "", s_turned[96] = "";
static uint32_t s_nonce = 0;                // the host's number, once it has asked for the password ...
static int s_asked = 0;                     // ... and not yet answered
static T_RACENET_EVENT s_events[RACENET_EVENTS];
static int s_evHead = 0, s_evCount = 0;
static char s_track[RACENET_TRACK] = "";
static int s_trackLaps = 0, s_trackPending = 0;
static T_RACENET_CAR s_own;                 // (only for the Lap and Progress a player's list shows)

// ---- bytes on the wire: little-endian, whatever the machine ----

static void Put16(uint8_t **p, int v) { (*p)[0] = (uint8_t)v; (*p)[1] = (uint8_t)(v >> 8); *p += 2; }
static void Put32(uint8_t **p, int32_t v) { (*p)[0] = (uint8_t)v; (*p)[1] = (uint8_t)(v >> 8); (*p)[2] = (uint8_t)(v >> 16); (*p)[3] = (uint8_t)(v >> 24); *p += 4; }
static int Get16(const uint8_t **p) { const int v = (int16_t)((*p)[0] | (*p)[1] << 8); *p += 2; return v; }
static int32_t Get32(const uint8_t **p) { const uint32_t v = (uint32_t)(*p)[0] | (uint32_t)(*p)[1] << 8 | (uint32_t)(*p)[2] << 16 | (uint32_t)(*p)[3] << 24; *p += 4; return (int32_t)v; }

static uint32_t PasswordHash(uint32_t nonce, const char *password) {
    uint32_t h = 2166136261u;
    for (int i = 0; i < 4; i++) h = (h ^ (uint8_t)(nonce >> (i * 8))) * 16777619u;
    for (const char *c = password; *c; c++) h = (h ^ (uint8_t)*c) * 16777619u;
    return h;
}

static T_RN_PEER *PeerOf(uint32_t id) {
    for (int i = 0; i < s_nbPeers; i++) if (s_peer[i].Id == id) return &s_peer[i];
    return NULL;
}

static void SendHello(void) {
    uint8_t b[8 + RACENET_VERSION + RACENET_NAME], *p = b;
    *p++ = M_HELLO;
    *p++ = RACENET_PROTOCOL;
    Put32(&p, (int32_t)s_content);
    const int v = (int)strlen(s_version);
    *p++ = (uint8_t)v;
    memcpy(p, s_version, v);
    p += v;
    const int n = (int)strlen(s_name);
    memcpy(p, s_name, n);
    p += n;
    NetKit_Broadcast(s_kit, b, (int)(p - b), 1);
}

// (to everyone, or a player who joins with it on: `late` how long the race has been going, 0 not yet)
static void SendTrack(uint32_t to, int late) {
    uint8_t b[6 + RACENET_TRACK], *p = b;
    *p++ = M_TRACK;
    *p++ = (uint8_t)s_trackLaps;
    Put32(&p, late);
    const int n = (int)strlen(s_track);
    memcpy(p, s_track, n);
    p += n;
    if (to) NetKit_Send(s_kit, to, b, (int)(p - b), 1);
    else NetKit_Broadcast(s_kit, b, (int)(p - b), 1);
}

// (the host) a player through the door: with a race on, it is sent it
static void Admit(T_RN_PEER *peer, uint32_t now) {
    if (peer->Admitted || !peer->Hello || peer->Pending) return;
    peer->Admitted = 1;
    if (s_racing) SendTrack(peer->Id, 0);
    else if (s_live) { const int late = (int)(now - s_wentAt); SendTrack(peer->Id, late > 0 ? late : 1); Log_Info("[racenet] %s joins the race under way, %.1f s in", peer->Name, late / 1000.0); }
}

static void Turn(T_RN_PEER *peer, int reason, const char *why) {
    snprintf(s_turned, sizeof(s_turned), "%s: %s", peer->Name, why);
    Log_Warn("[racenet] turned away %s", s_turned);
    NetKit_Kick(s_kit, peer->Id, reason);
}

// The members as netkit has them: a player gone is taken off, a new one put on
static void SyncMembers(void) {
    const uint32_t self = NetKit_LocalId(s_kit);
    int was[RACENET_MAX_OTHERS];
    memset(was, 0, sizeof(was));
    const int n = NetKit_Count(s_kit);
    for (int i = 0; i < n; i++) {
        const NetKitMember *m = NetKit_MemberAt(s_kit, i);
        if (!m || m->id == self) continue;
        T_RN_PEER *p = PeerOf(m->id);
        if (!p) {
            if (s_nbPeers >= RACENET_MAX_OTHERS) continue;
            p = &s_peer[s_nbPeers++];
            memset(p, 0, sizeof(*p));
            p->Id = m->id;
            snprintf(p->Name, sizeof(p->Name), "%s", m->name);
            Log_Info("[racenet] %s joined", p->Name);
            SendHello();
            if (s_hosting && s_password[0]) {
                uint8_t b[5], *q = b;
                p->Pending = 1;
                p->AskedAt = (uint32_t)SDL_GetTicks();
                p->Nonce = ((uint32_t)rand() << 16) ^ (uint32_t)rand() ^ p->AskedAt ^ p->Id;
                *q++ = M_ASK;
                Put32(&q, (int32_t)p->Nonce);
                NetKit_Send(s_kit, p->Id, b, 5, 1);
            }
        }
        was[p - s_peer] = 1;
    }
    for (int i = s_nbPeers - 1; i >= 0; i--)
        if (!was[i]) {
            Log_Info("[racenet] %s left", s_peer[i].Name);
            memmove(&s_peer[i], &s_peer[i + 1], sizeof(s_peer[0]) * (s_nbPeers - i - 1));
            memmove(&was[i], &was[i + 1], sizeof(was[0]) * (s_nbPeers - i - 1));
            s_nbPeers--;
        }
}

static void Take(const NetKitMessage *m) {
    const uint8_t *p = (const uint8_t *)m->data, *const end = p + m->len;
    if (m->len < 1) return;
    const int type = *p++;
    T_RN_PEER *peer = PeerOf(m->from);
    switch (type) {
    case M_HELLO:
        if (peer && end - p >= 1) {
            const int protocol = *p++;
            char version[RACENET_VERSION] = "";
            uint32_t content = 0;
            if (protocol == RACENET_PROTOCOL && end - p >= 5) {
                content = (uint32_t)Get32(&p);
                int v = *p++;
                if (v > (int)(end - p)) v = (int)(end - p);
                if (v >= RACENET_VERSION) v = RACENET_VERSION - 1;
                memcpy(version, p, v);
                version[v] = 0;
                p += v;
                const int n = (int)(end - p) < RACENET_NAME ? (int)(end - p) : RACENET_NAME;
                if (n > 0) { memcpy(peer->Name, p, n); peer->Name[n] = 0; }
            }
            peer->Hello = 1;
            if (protocol == RACENET_PROTOCOL && content == s_content) break;
            // (the host says what it is, then shows them the door; a player whose host doesn't match goes by itself)
            if (s_hosting) {
                uint8_t b[1 + RACENET_VERSION];
                const int n = (int)strlen(s_version);
                b[0] = M_WRONG;
                memcpy(b + 1, s_version, n);
                NetKit_Send(s_kit, m->from, b, 1 + n, 1);
                char why[64];
                snprintf(why, sizeof(why), "version %s", version[0] ? version : "(older)");
                Turn(peer, NETKIT_REFUSE_VERSION, why);
            }
            else if (const NetKitMember *who = NetKit_MemberById(s_kit, peer->Id)) {
                if (!who->isHost || s_error[0]) break;
                snprintf(s_hostVersion, sizeof(s_hostVersion), "%s", version);
                snprintf(s_error, sizeof(s_error), "The host is on version %s, this game on %s: update the older one", version[0] ? version : "(older)", s_version);
                Log_Warn("[racenet] %s", s_error);
            }
        }
        break;
    case M_ASK:
        if (!s_hosting && end - p >= 4) { s_nonce = (uint32_t)Get32(&p); s_asked = 1; }
        break;
    case M_PASS:
        if (peer && s_hosting && peer->Pending && end - p >= 4) {
            if ((uint32_t)Get32(&p) == PasswordHash(peer->Nonce, s_password)) { peer->Pending = 0; Log_Info("[racenet] %s gave the password", peer->Name); }
            else Turn(peer, NETKIT_REFUSE_PASSWORD, "wrong password");
        }
        break;
    case M_WRONG:
        if (!s_hosting && !s_error[0]) {
            char version[RACENET_VERSION];
            const int n = (int)(end - p) < RACENET_VERSION - 1 ? (int)(end - p) : RACENET_VERSION - 1;
            memcpy(version, p, n);
            version[n] = 0;
            snprintf(s_hostVersion, sizeof(s_hostVersion), "%s", version);
            snprintf(s_error, sizeof(s_error), "The host is on version %s, this game on %s: update the older one", version, s_version);
            Log_Warn("[racenet] %s", s_error);
        }
        break;
    case M_EVENT:
        if (peer && end - p >= 21) {
            T_RACENET_EVENT *e = &s_events[(s_evHead + s_evCount) % RACENET_EVENTS];
            if (s_evCount == RACENET_EVENTS) { s_evHead = (s_evHead + 1) % RACENET_EVENTS; s_evCount--; e = &s_events[(s_evHead + s_evCount) % RACENET_EVENTS]; }
            e->Type = *p++;
            e->A = Get16(&p); e->B = Get16(&p);
            e->X = Get32(&p); e->Y = Get32(&p); e->Z = Get32(&p);
            e->Target = (uint32_t)Get32(&p);
            snprintf(e->Name, sizeof(e->Name), "%s", peer->Name);
            s_evCount++;
        }
        break;
    case M_PING:
        if (end - p >= 4) {
            uint8_t b[5];
            memcpy(b + 1, p, 4);
            b[0] = M_PONG;
            NetKit_Send(s_kit, m->from, b, 5, 0);
        }
        break;
    case M_PONG:
        if (peer && end - p >= 4) {
            const int rtt = (int)((uint32_t)SDL_GetTicks() - (uint32_t)Get32(&p));
            if (rtt >= 0 && rtt < 5000) peer->Rtt = peer->Rtt ? (peer->Rtt * 3 + rtt) / 4 : rtt > 0 ? rtt : 1;
        }
        break;
    case M_TRACK:
        if (end - p >= 6 && !s_hosting) {
            s_trackLaps = *p++;
            const int late = Get32(&p);
            const int n = (int)(end - p) < RACENET_TRACK - 1 ? (int)(end - p) : RACENET_TRACK - 1;
            memcpy(s_track, p, n);
            s_track[n] = 0;
            s_trackPending = 1;
            s_selfReady = 0;
            s_goPending = late > 0;
            s_lateMs = late;
            s_lateAt = (uint32_t)SDL_GetTicks();
            s_evHead = s_evCount = 0;
            for (int i = 0; i < s_nbPeers; i++) { s_peer[i].Ready = s_peer[i].Finish = s_peer[i].Lap = s_peer[i].Progress = 0; s_peer[i].N = 0; }
            Log_Info("[racenet] the host starts %s, %d lap(s)", s_track, s_trackLaps);
        }
        break;
    case M_READY:
        if (peer) peer->Ready = 1;
        break;
    case M_GO:
        if (!s_hosting) s_goPending = 1;
        break;
    case M_CAR:
        if (peer && end - p >= 4 + 12 + 12 + 8 + 6) {
            T_RN_SAMPLE s;
            s.T = (uint32_t)Get32(&p);
            s.X = Get32(&p); s.Y = Get32(&p); s.Z = Get32(&p);
            s.Alpha = (int16_t)Get16(&p); s.Beta = (int16_t)Get16(&p); s.Gamma = (int16_t)Get16(&p);
            s.Body = (int16_t)Get16(&p); s.Anim = (int16_t)Get16(&p); s.Island = (int16_t)Get16(&p);
            peer->Lap = Get32(&p);
            peer->Progress = Get32(&p);
            s.Speed = Get32(&p);
            s.Flags = (int16_t)Get16(&p);
            // (out of order: an old sample isn't put after a newer one)
            if (peer->N && (int32_t)(s.T - peer->S[peer->Head].T) <= 0) break;
            const uint32_t now = (uint32_t)SDL_GetTicks();
            const int32_t offset = (int32_t)(now - s.T);
            // (the quickest trip seen, let drift up slowly: a clock isn't quite the other's)
            if (!peer->HasOffset || offset < peer->Offset) { peer->Offset = offset; peer->HasOffset = 1; }
            else if ((s.T & 31) == 0) peer->Offset++;
            peer->Head = peer->N ? (peer->Head + 1) % RACENET_SAMPLES : 0;
            peer->S[peer->Head] = s;
            if (peer->N < RACENET_SAMPLES) peer->N++;
            peer->HeardAt = now;
        }
        break;
    case M_FINISH:
        if (peer && end - p >= 4) peer->Finish = Get32(&p);
        break;
    default:
        break;
    }
}

// (the host: all of them at the start, or waited for long enough)
static void GoWhenReady(uint32_t now) {
    if (!s_hosting || !s_racing || s_goSent || !s_selfReady) return;
    int ready = 1;
    for (int i = 0; i < s_nbPeers; i++) if (!s_peer[i].Ready && !s_peer[i].Pending) ready = 0;
    if (!ready && now - s_announcedAt < RACENET_GO_WAIT_MS) return;
    const uint8_t b = M_GO;
    NetKit_Broadcast(s_kit, &b, 1, 1);
    s_goSent = s_goPending = 1;
    s_racing = 0;
    s_live = 1;
    s_wentAt = now;
    Log_Info("[racenet] go%s", ready ? "" : " (a player not ready given up on)");
}

static int SDLCALL Worker(void *unused) {
    (void)unused;
    static uint8_t buffer[NETKIT_MSG_MAX];
    while (!SDL_GetAtomicInt(&s_quit)) {
        SDL_LockMutex(s_lock);
        if (s_kit) {
            if (NetKit_Poll(s_kit) < 0 && !s_error[0]) snprintf(s_error, sizeof(s_error), "%s", NetKit_LastError(s_kit));
            NetKitEvent e;
            while (NetKit_NextEvent(s_kit, &e)) {
                // (the first word on it: a host gone is followed by netkit's join window running out, which says nothing)
                if (e.type != NETKIT_EVENT_REFUSED && e.type != NETKIT_EVENT_CLOSED) continue;
                if (!s_error[0]) {
                    if (e.type == NETKIT_EVENT_REFUSED) snprintf(s_error, sizeof(s_error), "The host refused: %s", NetKit_RefuseText(e.reason));
                    else snprintf(s_error, sizeof(s_error), "%s", s_hosting ? "The game closed" : "The host left");
                    Log_Warn("[racenet] %s", s_error);
                }
                if (!s_hosting) s_nbPeers = 0;
            }
            if (NetKit_Connected(s_kit) || s_hosting) SyncMembers();
            NetKitMessage m;
            while (NetKit_NextMessage(s_kit, &m, buffer, sizeof(buffer))) Take(&m);
            const uint32_t now = (uint32_t)SDL_GetTicks();
            if (s_hosting)
                for (int i = 0; i < s_nbPeers; i++) {
                    if (s_peer[i].Pending && now - s_peer[i].AskedAt > RACENET_ASK_MS) Turn(&s_peer[i], NETKIT_REFUSE_TIMEOUT, "no password given");
                    Admit(&s_peer[i], now);
                }
            if (now - s_pingAt >= RACENET_PING_MS) {
                uint8_t b[5], *p = b;
                s_pingAt = now;
                *p++ = M_PING;
                Put32(&p, (int32_t)now);
                for (int i = 0; i < s_nbPeers; i++) NetKit_Send(s_kit, s_peer[i].Id, b, 5, 0);
            }
            GoWhenReady(now);
        }
        SDL_UnlockMutex(s_lock);
        SDL_Delay(RACENET_POLL_MS);
    }
    return 0;
}

static int Begin(void) {
    if (!s_lock) s_lock = SDL_CreateMutex();
    if (!s_lock) { snprintf(s_error, sizeof(s_error), "%s", "No lock to run the network with"); return 0; }
    s_nbPeers = 0; s_error[0] = 0;
    s_selfReady = s_goSent = s_goPending = s_racing = s_trackPending = s_live = s_lateMs = s_asked = s_evHead = s_evCount = 0;
    s_turned[0] = s_hostVersion[0] = 0;
    memset(&s_own, 0, sizeof(s_own));
    SDL_SetAtomicInt(&s_quit, 0);
    s_thread = SDL_CreateThread(Worker, "racenet", NULL);
    return s_thread != NULL;
}

void RaceNet_SetGame(uint32_t content, const char *version) {
    s_content = content;
    snprintf(s_version, sizeof(s_version), "%s", version);
}

int RaceNet_Host(const char *name, const char *password) {
    RaceNet_Stop();
    snprintf(s_password, sizeof(s_password), "%s", password);
    struct NetKit *kit = NetKit_Host(RACENET_PORT, name);
    if (!kit) { snprintf(s_error, sizeof(s_error), "Port %d is in use on this PC", RACENET_PORT); Log_Warn("[racenet] %s", s_error); return 0; }
    s_kit = kit; s_hosting = 1;
    snprintf(s_name, sizeof(s_name), "%s", name);
    if (!Begin()) { RaceNet_Stop(); return 0; }
    NetUpnp_Open(RACENET_PORT);
    Log_Info("[racenet] hosting as %s on UDP port %d", name, RACENET_PORT);
    return 1;
}

int RaceNet_HostLocal(const char *name, unsigned short port) {
    RaceNet_Stop();
    s_password[0] = 0;
    struct NetKit *kit = NetKit_Host(port, name);
    if (!kit) { snprintf(s_error, sizeof(s_error), "Port %d is in use on this PC", port); Log_Warn("[racenet] %s", s_error); return 0; }
    s_kit = kit; s_hosting = 1; s_local = 1;
    snprintf(s_name, sizeof(s_name), "%s", name);
    if (!Begin()) { RaceNet_Stop(); return 0; }
    Log_Info("[racenet] two players at this PC, on port %d", port);
    return 1;
}

int RaceNet_Local(void) { return s_kit != NULL && s_local; }

int RaceNet_SpawnPlayer2(unsigned short port, const char *userDir, int x, int y, int w, int h, int resW, int resH, int pad) {
    char exe[1024], text[64], res[32];
#ifdef _WIN32
    if (!GetModuleFileNameA(NULL, exe, sizeof(exe))) return 0;
#else
    snprintf(exe, sizeof(exe), "/proc/self/exe");
#endif
    snprintf(res, sizeof(res), "%dx%d", resW, resH);
    // (the music once: the second game is silent. And a test's way to reach it: LBA2_P2_LISTEN)
    const char *listen = getenv("LBA2_P2_LISTEN");
    const char *args[12];
    int n = 0;
    args[n++] = exe; args[n++] = "--user-dir"; args[n++] = userDir; args[n++] = "--resolution"; args[n++] = res; args[n++] = "--no-audio"; args[n++] = "--ignore-focus";
    if (listen) { args[n++] = "--listen"; args[n++] = listen; }
    args[n] = NULL;
    SDL_Environment *env = SDL_CreateEnvironment(true);
    if (!env) return 0;
    snprintf(text, sizeof(text), "%d", port);
    SDL_SetEnvironmentVariable(env, "LBA2_PLAYER2", text, true);
    snprintf(text, sizeof(text), "%d,%d,%d,%d", x, y, w, h);
    SDL_SetEnvironmentVariable(env, "LBA2_WINDOW_RECT", text, true);
    snprintf(text, sizeof(text), "%d", pad);
    SDL_SetEnvironmentVariable(env, "LBA2_PAD", text, true);
#ifdef _WIN32
    // (the keyboard is shared while this game, or the second's, is the one in front: RaceNet_SharedKeys)
    snprintf(text, sizeof(text), "%lu", (unsigned long)GetCurrentProcessId());
    SDL_SetEnvironmentVariable(env, "LBA2_P1_PID", text, true);
#endif
    // (its pad heard without the keyboard's focus, which stays with the first player)
    SDL_SetEnvironmentVariable(env, "SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS", "1", true);
    SDL_PropertiesID props = SDL_CreateProperties();
    SDL_SetPointerProperty(props, SDL_PROP_PROCESS_CREATE_ARGS_POINTER, (void *)args);
    SDL_SetPointerProperty(props, SDL_PROP_PROCESS_CREATE_ENVIRONMENT_POINTER, env);
    SDL_SetBooleanProperty(props, SDL_PROP_PROCESS_CREATE_BACKGROUND_BOOLEAN, true);
    SDL_Process *process = SDL_CreateProcessWithProperties(props);
    SDL_DestroyProperties(props);
    SDL_DestroyEnvironment(env);
    if (!process) { snprintf(s_error, sizeof(s_error), "Player 2's game wouldn't start"); Log_Warn("[racenet] %s: %s", s_error, SDL_GetError()); return 0; }
    // (it goes by itself when this game's session ends: the host gone)
    SDL_DestroyProcess(process);
    Log_Info("[racenet] player 2's game started, %s at %d,%d %dx%d, pad %d", res, x, y, w, h, pad);
    return 1;
}

int RaceNet_Join(const char *address, const char *name) {
    RaceNet_Stop();
    char host[128];
    int port = RACENET_PORT;
    snprintf(host, sizeof(host), "%s", address);
    char *colon = strrchr(host, ':');
    if (colon) { port = atoi(colon + 1); *colon = 0; }
    if (!host[0] || port <= 0 || port > 65535) { snprintf(s_error, sizeof(s_error), "%s", "That address can't be right"); return 0; }
    struct NetKit *kit = NetKit_Join(host, (unsigned short)port, name);
    if (!kit) { snprintf(s_error, sizeof(s_error), "%s isn't an address that can be reached", host); Log_Warn("[racenet] %s", s_error); return 0; }
    s_kit = kit; s_hosting = 0;
    snprintf(s_name, sizeof(s_name), "%s", name);
    if (!Begin()) { RaceNet_Stop(); return 0; }
    Log_Info("[racenet] joining %s:%d as %s", host, port, name);
    return 1;
}

void RaceNet_Stop(void) {
    if (s_thread) {
        SDL_SetAtomicInt(&s_quit, 1);
        SDL_WaitThread(s_thread, NULL);
        s_thread = NULL;
    }
    if (s_kit) {
        const int hosted = s_hosting;
        NetKit_Stop(s_kit);
        s_kit = NULL;
        if (hosted) NetUpnp_Close();
        Log_Info("[racenet] stopped");
    }
    s_hosting = 0; s_local = 0; s_nbPeers = 0;
}

int RaceNet_On(void) { return s_kit != NULL; }
int RaceNet_IsHost(void) { return s_kit != NULL && s_hosting; }

int RaceNet_Connected(void) {
    if (!s_kit) return 0;
    SDL_LockMutex(s_lock);
    const int on = s_hosting || NetKit_Connected(s_kit);
    SDL_UnlockMutex(s_lock);
    return on;
}

const char *RaceNet_Error(void) { return s_error; }

int RaceNet_Router(int *ok, char *text, int textSize, char *outside, int outsideSize, char *inside, int insideSize) {
    return NetUpnp_Result(ok, text, textSize, outside, outsideSize, inside, insideSize);
}

int RaceNet_Others(void) {
    if (!s_kit) return 0;
    SDL_LockMutex(s_lock);
    const int n = s_nbPeers;
    SDL_UnlockMutex(s_lock);
    return n;
}

int RaceNet_Other(int i, T_RACENET_PLAYER *out) {
    if (!s_kit) return 0;
    SDL_LockMutex(s_lock);
    const int ok = i >= 0 && i < s_nbPeers;
    if (ok) {
        const T_RN_PEER *p = &s_peer[i];
        snprintf(out->Name, sizeof(out->Name), "%s", p->Name);
        out->Ready = p->Ready; out->Finish = p->Finish; out->Lap = p->Lap; out->Progress = p->Progress; out->Pending = p->Pending; out->Id = p->Id;
    }
    SDL_UnlockMutex(s_lock);
    return ok;
}

void RaceNet_Announce(const char *track, int laps) {
    if (!s_kit || !s_hosting) return;
    SDL_LockMutex(s_lock);
    snprintf(s_track, sizeof(s_track), "%s", track);
    s_trackLaps = laps;
    for (int i = 0; i < s_nbPeers; i++) { s_peer[i].Ready = s_peer[i].Finish = s_peer[i].Lap = s_peer[i].Progress = 0; s_peer[i].N = 0; }
    s_selfReady = s_goSent = s_goPending = s_live = s_evHead = s_evCount = 0;
    s_racing = 1;
    s_announcedAt = (uint32_t)SDL_GetTicks();
    SendTrack(0, 0);
    SDL_UnlockMutex(s_lock);
    Log_Info("[racenet] %s, %d lap(s), announced to %d player(s)", track, laps, s_nbPeers);
}

int RaceNet_Announced(char *track, int size, int *laps) {
    if (!s_kit) return 0;
    SDL_LockMutex(s_lock);
    const int got = s_trackPending;
    if (got) { snprintf(track, size, "%s", s_track); *laps = s_trackLaps; s_trackPending = 0; }
    SDL_UnlockMutex(s_lock);
    return got;
}

void RaceNet_Ready(void) {
    if (!s_kit) return;
    SDL_LockMutex(s_lock);
    if (!s_selfReady) {
        s_selfReady = 1;
        if (!s_hosting) { const uint8_t b = M_READY; NetKit_Broadcast(s_kit, &b, 1, 1); }
        Log_Info("[racenet] ready at the start");
    }
    SDL_UnlockMutex(s_lock);
}

int RaceNet_Go(void) {
    if (!s_kit) return 0;
    SDL_LockMutex(s_lock);
    const int go = s_goPending;
    s_goPending = 0;
    // (joined late: its race began this long ago -- a time is from the race's go, every player's)
    if (go && s_lateMs) { s_lateMs += (int)((uint32_t)SDL_GetTicks() - s_lateAt); s_lateAt = (uint32_t)SDL_GetTicks(); }
    SDL_UnlockMutex(s_lock);
    return go;
}

int RaceNet_Late(void) { return s_kit != NULL && s_lateMs > 0; }

void RaceNet_RaceOver(void) {
    if (!s_kit) return;
    SDL_LockMutex(s_lock);
    s_live = 0;
    SDL_UnlockMutex(s_lock);
}

uint32_t RaceNet_SelfId(void) {
    if (!s_kit) return 0;
    SDL_LockMutex(s_lock);
    const uint32_t id = NetKit_LocalId(s_kit);
    SDL_UnlockMutex(s_lock);
    return id;
}

int RaceNet_Slot(void) {
    if (!s_kit) return 0;
    SDL_LockMutex(s_lock);
    const uint32_t self = NetKit_LocalId(s_kit);
    int slot = 0;
    for (int i = 0; i < s_nbPeers; i++) if (s_peer[i].Id < self) slot++;
    SDL_UnlockMutex(s_lock);
    return slot;
}

int RaceNet_PasswordAsked(void) {
    if (!s_kit) return 0;
    SDL_LockMutex(s_lock);
    const int asked = s_asked;
    s_asked = 0;
    SDL_UnlockMutex(s_lock);
    return asked;
}

void RaceNet_SendPassword(const char *password) {
    if (!s_kit) return;
    uint8_t b[5], *p = b;
    SDL_LockMutex(s_lock);
    *p++ = M_PASS;
    Put32(&p, (int32_t)PasswordHash(s_nonce, password));
    NetKit_Broadcast(s_kit, b, 5, 1);
    SDL_UnlockMutex(s_lock);
}

const char *RaceNet_Turned(void) { return s_turned; }

void RaceNet_SendEvent(const T_RACENET_EVENT *e) {
    if (!s_kit) return;
    uint8_t b[32], *p = b;
    *p++ = M_EVENT;
    *p++ = (uint8_t)e->Type;
    Put16(&p, e->A); Put16(&p, e->B);
    Put32(&p, e->X); Put32(&p, e->Y); Put32(&p, e->Z);
    Put32(&p, (int32_t)e->Target);
    SDL_LockMutex(s_lock);
    NetKit_Broadcast(s_kit, b, (int)(p - b), 1);
    SDL_UnlockMutex(s_lock);
}

int RaceNet_NextEvent(T_RACENET_EVENT *out) {
    if (!s_kit) return 0;
    SDL_LockMutex(s_lock);
    const int got = s_evCount > 0;
    if (got) { *out = s_events[s_evHead]; s_evHead = (s_evHead + 1) % RACENET_EVENTS; s_evCount--; }
    SDL_UnlockMutex(s_lock);
    return got;
}

// The second player's keys, by where they are on the keyboard whatever its letters: W A S D, Q the item, E the handbrake, R the view. The second game never
// has the keyboard's focus, so it asks Windows; only with one of the two games in front, or it would drive on what's typed elsewhere.
int RaceNet_SharedKeys(void) {
#ifdef _WIN32
    static DWORD first = 0;
    if (!first) { const char *pid = getenv("LBA2_P1_PID"); first = pid ? (DWORD)strtoul(pid, NULL, 10) : 1; }
    DWORD front = 0;
    GetWindowThreadProcessId(GetForegroundWindow(), &front);
    if (front != first && front != GetCurrentProcessId()) return 0;
    static const struct { UINT Scan; int Bit; } keys[] = {{0x11, RACENET_KEY_UP}, {0x1F, RACENET_KEY_DOWN}, {0x1E, RACENET_KEY_LEFT}, {0x20, RACENET_KEY_RIGHT},
                                                          {0x10, RACENET_KEY_ITEM}, {0x12, RACENET_KEY_HANDBRAKE}, {0x13, RACENET_KEY_VIEW}};
    int down = 0;
    for (size_t i = 0; i < sizeof(keys) / sizeof(keys[0]); i++)
        if (GetAsyncKeyState((int)MapVirtualKeyA(keys[i].Scan, MAPVK_VSC_TO_VK)) & 0x8000) down |= keys[i].Bit;
    return down;
#else
    return 0;
#endif
}

void RaceNet_SendCar(const T_RACENET_CAR *car) {
    if (!s_kit) return;
    uint8_t b[72], *p = b;
    *p++ = M_CAR;
    Put32(&p, (int32_t)(uint32_t)SDL_GetTicks());
    Put32(&p, car->X); Put32(&p, car->Y); Put32(&p, car->Z);
    Put16(&p, car->Alpha); Put16(&p, car->Beta); Put16(&p, car->Gamma);
    Put16(&p, car->Body); Put16(&p, car->Anim); Put16(&p, car->Island);
    Put32(&p, car->Lap); Put32(&p, car->Progress);
    Put32(&p, car->Speed); Put16(&p, car->Flags);
    SDL_LockMutex(s_lock);
    s_own = *car;
    NetKit_Broadcast(s_kit, b, (int)(p - b), 0);
    SDL_UnlockMutex(s_lock);
}

// (angles go round at 4096: the short way between two)
static int16_t Between(int a, int b, double t) {
    int d = (b - a) & 4095;
    if (d > 2048) d -= 4096;
    return (int16_t)(((int)(a + d * t)) & 4095);
}

int RaceNet_CarOf(int i, T_RACENET_CAR *out) {
    if (!s_kit) return 0;
    int ok = 0;
    SDL_LockMutex(s_lock);
    if (i >= 0 && i < s_nbPeers && s_peer[i].N) {
        T_RN_PEER *p = &s_peer[i];
        const uint32_t now = (uint32_t)SDL_GetTicks();
        if (now - p->HeardAt < RACENET_QUIET_MS) {
            const uint32_t at = now - (uint32_t)p->Offset + (uint32_t)(p->Rtt / 2) - RACENET_DELAY_MS;
            const T_RN_SAMPLE *newer = &p->S[p->Head], *older = newer;
            // (the two samples either side of the moment; at the newest, or before the oldest, that one)
            for (int k = 1; k < p->N; k++) {
                older = &p->S[(p->Head + RACENET_SAMPLES - k) % RACENET_SAMPLES];
                if ((int32_t)(older->T - at) <= 0) break;
                newer = older;
            }
            double t = 0;
            if (newer != older && newer->T != older->T && (int32_t)(at - older->T) > 0) {
                // (past the newest, on as it was going, so far)
                int32_t past = (int32_t)(at - older->T);
                const int32_t most = (int32_t)(newer->T - older->T) + RACENET_AHEAD_MS;
                if (past > most) past = most;
                t = (double)past / (double)(int32_t)(newer->T - older->T);
            }
            // (a rescue, a teleporter: put there, not slid there -- as a ghost's RACE_GHOST_JUMP)
            const int jumped = abs(newer->X - older->X) > 4096 || abs(newer->Z - older->Z) > 4096;
            if (jumped) t = t < 0.5 ? 0 : 1;
            double x = older->X + (newer->X - older->X) * t, y = older->Y + (newer->Y - older->Y) * t, z = older->Z + (newer->Z - older->Z) * t;
            // (eased from where it was shown: a sample that has it somewhere else than it was going is no jump on the screen)
            if (p->ShownAt && !jumped && fabs(p->ShownX - x) < RACENET_EASE_MOST && fabs(p->ShownZ - z) < RACENET_EASE_MOST) {
                const double keep = exp(-(double)(now - p->ShownAt) / RACENET_EASE_MS);
                const double since = now - p->ShownAt, span = newer->T != older->T ? (double)(int32_t)(newer->T - older->T) : 0;
                // (where it was shown, gone on as the car is going, against where it should be)
                const double sx = p->ShownX + (span > 0 ? (newer->X - older->X) / span * since : 0), sz = p->ShownZ + (span > 0 ? (newer->Z - older->Z) / span * since : 0);
                x += (sx - x) * keep; z += (sz - z) * keep; y += (p->ShownY - y) * keep;
            }
            p->ShownX = x; p->ShownY = y; p->ShownZ = z; p->ShownAt = now;
            out->X = (int32_t)x;
            out->Y = (int32_t)y;
            out->Z = (int32_t)z;
            out->Alpha = Between(older->Alpha, newer->Alpha, t);
            out->Beta = Between(older->Beta, newer->Beta, t);
            out->Gamma = Between(older->Gamma, newer->Gamma, t);
            out->Body = newer->Body; out->Anim = newer->Anim; out->Island = newer->Island;
            out->Lap = p->Lap; out->Progress = p->Progress;
            out->Speed = newer->Speed; out->Flags = newer->Flags;
            ok = 1;
        }
    }
    SDL_UnlockMutex(s_lock);
    return ok;
}

// (a test's look at the session: the console's netstatus)
int RaceNet_StatusLine(char *out, int size) {
    int ok = 0;
    char said[220] = "", outside[64] = "", inside[64] = "";
    const int answered = RaceNet_Router(&ok, said, sizeof(said), outside, sizeof(outside), inside, sizeof(inside));
    int used = snprintf(out, size, "net on=%d host=%d connected=%d others=%d error='%s' router=%s ok=%d outside=%s inside=%s said='%s'", RaceNet_On(), RaceNet_IsHost(), RaceNet_Connected(),
                        RaceNet_Others(), s_error, answered ? "answered" : "waiting", ok, outside, inside, said);
    return used;
}

void RaceNet_Finished(int ms) {
    if (!s_kit) return;
    uint8_t b[8], *p = b;
    *p++ = M_FINISH;
    Put32(&p, ms + s_lateMs);
    SDL_LockMutex(s_lock);
    NetKit_Broadcast(s_kit, b, (int)(p - b), 1);
    SDL_UnlockMutex(s_lock);
    Log_Info("[racenet] finished in %d ms", ms);
}
