// Online races: one player hosts, the others join them (the user, 2026-10-30), over netkit (NET/: a friend's UDP room library, free to use).
//
// The game's loop stops for seconds at a time -- a scene loading, the pause menu -- and netkit lets a player go after 6 s of silence, so netkit runs on a
// thread of its own here, behind a lock, and the game only reads and writes this file's state.
//
// What crosses the network, by type (first byte):
//   HELLO   reliable   to all, on joining: the protocol, the game's version and the player's name (netkit calls a host "host")
//   TRACK   reliable   host to all: the race, its laps and its track's name
//   READY   reliable   to the host: this game's car is at the start
//   GO      reliable   host to all: count down now
//   CAR     as it may  to all, 30 a second: as a ghost's sample, so a car is drawn as a ghost is (RACEMOD.CPP DrawNetCars)
//   FINISH  reliable   to all: this player's time
// Each car is drawn RACENET_DELAY_MS in the past, between the samples either side of that moment: on time they come 33 ms apart, and late ones don't jump.
#include "NETRACE.H"
#include "NET/netkit.h"
#include "NETUPNP.H"

#include <SDL3/SDL.h>
#include <SYSTEM/LOG.H>

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define RACENET_PROTOCOL 1
#define RACENET_DELAY_MS 100                // how far behind a car is drawn
#define RACENET_QUIET_MS 2000               // a car not heard from this long isn't drawn
#define RACENET_GO_WAIT_MS 30000            // the slowest loader waited for this long
#define RACENET_POLL_MS 5
#define RACENET_SAMPLES 16
#define RACENET_TRACK 96

enum { M_HELLO = 1, M_TRACK, M_READY, M_GO, M_CAR, M_FINISH };

typedef struct {
    uint32_t T;                             // the sender's clock
    int32_t X, Y, Z;
    int16_t Alpha, Beta, Gamma, Body, Anim, Island;
} T_RN_SAMPLE;

typedef struct {
    uint32_t Id;
    char Name[RACENET_NAME + 1];
    int Ready, Finish, Lap, Progress, Wrong, Hello;
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
static int s_hosting = 0;
static char s_name[RACENET_NAME + 1] = "";
static int s_selfReady = 0, s_goSent = 0, s_goPending = 0;
static uint32_t s_announcedAt = 0;
static int s_racing = 0;                    // a race announced and not yet gone
static char s_track[RACENET_TRACK] = "";
static int s_trackLaps = 0, s_trackPending = 0;
static T_RACENET_CAR s_own;                 // (only for the Lap and Progress a player's list shows)

// ---- bytes on the wire: little-endian, whatever the machine ----

static void Put16(uint8_t **p, int v) { (*p)[0] = (uint8_t)v; (*p)[1] = (uint8_t)(v >> 8); *p += 2; }
static void Put32(uint8_t **p, int32_t v) { (*p)[0] = (uint8_t)v; (*p)[1] = (uint8_t)(v >> 8); (*p)[2] = (uint8_t)(v >> 16); (*p)[3] = (uint8_t)(v >> 24); *p += 4; }
static int Get16(const uint8_t **p) { const int v = (int16_t)((*p)[0] | (*p)[1] << 8); *p += 2; return v; }
static int32_t Get32(const uint8_t **p) { const uint32_t v = (uint32_t)(*p)[0] | (uint32_t)(*p)[1] << 8 | (uint32_t)(*p)[2] << 16 | (uint32_t)(*p)[3] << 24; *p += 4; return (int32_t)v; }

// (the game's version, to tell a player whose game doesn't match: bodies and tracks are numbered by the build)
extern const char *BuildInfo;
static uint32_t VersionHash(void) {
    uint32_t h = 2166136261u;
    for (const char *c = BuildInfo; *c; c++) h = (h ^ (uint8_t)*c) * 16777619u;
    return h;
}

static T_RN_PEER *PeerOf(uint32_t id) {
    for (int i = 0; i < s_nbPeers; i++) if (s_peer[i].Id == id) return &s_peer[i];
    return NULL;
}

static void SendHello(void) {
    uint8_t b[8 + RACENET_NAME], *p = b;
    *p++ = M_HELLO;
    *p++ = RACENET_PROTOCOL;
    Put32(&p, (int32_t)VersionHash());
    const int n = (int)strlen(s_name);
    memcpy(p, s_name, n);
    p += n;
    NetKit_Broadcast(s_kit, b, (int)(p - b), 1);
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
        if (peer && end - p >= 5) {
            const int protocol = *p++;
            peer->Wrong = protocol != RACENET_PROTOCOL || (uint32_t)Get32(&p) != VersionHash();
            peer->Hello = 1;
            const int n = (int)(end - p) < RACENET_NAME ? (int)(end - p) : RACENET_NAME;
            if (n > 0) { memcpy(peer->Name, p, n); peer->Name[n] = 0; }
            if (peer->Wrong) Log_Warn("[racenet] %s is on another version of the game", peer->Name);
        }
        break;
    case M_TRACK:
        if (end - p >= 2 && !s_hosting) {
            s_trackLaps = *p++;
            const int n = (int)(end - p) < RACENET_TRACK - 1 ? (int)(end - p) : RACENET_TRACK - 1;
            memcpy(s_track, p, n);
            s_track[n] = 0;
            s_trackPending = 1;
            s_selfReady = s_goPending = 0;
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
        if (peer && end - p >= 4 + 12 + 12 + 8) {
            T_RN_SAMPLE s;
            s.T = (uint32_t)Get32(&p);
            s.X = Get32(&p); s.Y = Get32(&p); s.Z = Get32(&p);
            s.Alpha = (int16_t)Get16(&p); s.Beta = (int16_t)Get16(&p); s.Gamma = (int16_t)Get16(&p);
            s.Body = (int16_t)Get16(&p); s.Anim = (int16_t)Get16(&p); s.Island = (int16_t)Get16(&p);
            peer->Lap = Get32(&p);
            peer->Progress = Get32(&p);
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
    for (int i = 0; i < s_nbPeers; i++) if (!s_peer[i].Ready) ready = 0;
    if (!ready && now - s_announcedAt < RACENET_GO_WAIT_MS) return;
    const uint8_t b = M_GO;
    NetKit_Broadcast(s_kit, &b, 1, 1);
    s_goSent = s_goPending = 1;
    s_racing = 0;
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
            GoWhenReady((uint32_t)SDL_GetTicks());
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
    s_selfReady = s_goSent = s_goPending = s_racing = s_trackPending = 0;
    memset(&s_own, 0, sizeof(s_own));
    SDL_SetAtomicInt(&s_quit, 0);
    s_thread = SDL_CreateThread(Worker, "racenet", NULL);
    return s_thread != NULL;
}

int RaceNet_Host(const char *name) {
    RaceNet_Stop();
    struct NetKit *kit = NetKit_Host(RACENET_PORT, name);
    if (!kit) { snprintf(s_error, sizeof(s_error), "Port %d is in use on this PC", RACENET_PORT); Log_Warn("[racenet] %s", s_error); return 0; }
    s_kit = kit; s_hosting = 1;
    snprintf(s_name, sizeof(s_name), "%s", name);
    if (!Begin()) { RaceNet_Stop(); return 0; }
    NetUpnp_Open(RACENET_PORT);
    Log_Info("[racenet] hosting as %s on UDP port %d", name, RACENET_PORT);
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
    s_hosting = 0; s_nbPeers = 0;
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
        out->Ready = p->Ready; out->Finish = p->Finish; out->Lap = p->Lap; out->Progress = p->Progress; out->Wrong = p->Wrong;
    }
    SDL_UnlockMutex(s_lock);
    return ok;
}

void RaceNet_Announce(const char *track, int laps) {
    if (!s_kit || !s_hosting) return;
    uint8_t b[2 + RACENET_TRACK];
    const int n = (int)strlen(track) < RACENET_TRACK - 1 ? (int)strlen(track) : RACENET_TRACK - 1;
    b[0] = M_TRACK; b[1] = (uint8_t)laps;
    memcpy(b + 2, track, n);
    SDL_LockMutex(s_lock);
    for (int i = 0; i < s_nbPeers; i++) { s_peer[i].Ready = s_peer[i].Finish = s_peer[i].Lap = s_peer[i].Progress = 0; s_peer[i].N = 0; }
    s_selfReady = s_goSent = s_goPending = 0;
    s_racing = 1;
    s_announcedAt = (uint32_t)SDL_GetTicks();
    NetKit_Broadcast(s_kit, b, 2 + n, 1);
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
    SDL_UnlockMutex(s_lock);
    return go;
}

void RaceNet_SendCar(const T_RACENET_CAR *car) {
    if (!s_kit) return;
    uint8_t b[64], *p = b;
    *p++ = M_CAR;
    Put32(&p, (int32_t)(uint32_t)SDL_GetTicks());
    Put32(&p, car->X); Put32(&p, car->Y); Put32(&p, car->Z);
    Put16(&p, car->Alpha); Put16(&p, car->Beta); Put16(&p, car->Gamma);
    Put16(&p, car->Body); Put16(&p, car->Anim); Put16(&p, car->Island);
    Put32(&p, car->Lap); Put32(&p, car->Progress);
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
        const T_RN_PEER *p = &s_peer[i];
        const uint32_t now = (uint32_t)SDL_GetTicks();
        if (now - p->HeardAt < RACENET_QUIET_MS) {
            const uint32_t at = now - (uint32_t)p->Offset - RACENET_DELAY_MS;
            const T_RN_SAMPLE *newer = &p->S[p->Head], *older = newer;
            // (the two samples either side of the moment; at the newest, or before the oldest, that one)
            for (int k = 1; k < p->N; k++) {
                older = &p->S[(p->Head + RACENET_SAMPLES - k) % RACENET_SAMPLES];
                if ((int32_t)(older->T - at) <= 0) break;
                newer = older;
            }
            double t = 0;
            if (newer != older && newer->T != older->T && (int32_t)(at - older->T) > 0) t = (double)(int32_t)(at - older->T) / (double)(int32_t)(newer->T - older->T);
            if (t > 1) t = 1;
            // (a rescue, a teleporter: put there, not slid there -- as a ghost's RACE_GHOST_JUMP)
            if (abs(newer->X - older->X) > 4096 || abs(newer->Z - older->Z) > 4096) t = t < 0.5 ? 0 : 1;
            out->X = (int32_t)(older->X + (newer->X - older->X) * t);
            out->Y = (int32_t)(older->Y + (newer->Y - older->Y) * t);
            out->Z = (int32_t)(older->Z + (newer->Z - older->Z) * t);
            out->Alpha = Between(older->Alpha, newer->Alpha, t);
            out->Beta = Between(older->Beta, newer->Beta, t);
            out->Gamma = Between(older->Gamma, newer->Gamma, t);
            out->Body = newer->Body; out->Anim = newer->Anim; out->Island = newer->Island;
            out->Lap = p->Lap; out->Progress = p->Progress;
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
    Put32(&p, ms);
    SDL_LockMutex(s_lock);
    NetKit_Broadcast(s_kit, b, (int)(p - b), 1);
    SDL_UnlockMutex(s_lock);
    Log_Info("[racenet] finished in %d ms", ms);
}
