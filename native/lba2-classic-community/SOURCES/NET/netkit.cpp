#include "netkit.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <winsock2.h>
#include <ws2tcpip.h>
typedef SOCKET nk_sock;
#define NK_BAD_SOCK INVALID_SOCKET
#else
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <netdb.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>
typedef int nk_sock;
#define NK_BAD_SOCK (-1)
#endif

#ifdef _WIN32
typedef int nk_socklen;
#else
typedef socklen_t nk_socklen;
#endif

#include <chrono>

namespace {

const uint8_t NK_MAGIC = 0x4B;
const uint8_t NK_PROTO = 1;
const int NK_HDR = 12;

const int NK_DGRAM_MAX = 1216;

const int NK_REL_MAX = NK_DGRAM_MAX - NK_HDR;

const uint8_t NK_HELLO = 1;
const uint8_t NK_CHALLENGE = 2;
const uint8_t NK_CONNECT = 3;
const uint8_t NK_ACCEPT = 4;
const uint8_t NK_REFUSE = 5;
const uint8_t NK_MEMBER_ADD = 6;
const uint8_t NK_MEMBER_LEAVE = 7;
const uint8_t NK_APP = 8;
const uint8_t NK_BYE = 9;
const uint8_t NK_PING = 10;
const uint8_t NK_ACK = 11;

const uint8_t NK_FLAG_RELIABLE = 0x01;

const uint32_t NK_RETRY_MS = 150;
const int NK_RETRY_MAX = 24;

const uint32_t NK_JOIN_MS = 8000;

const uint32_t NK_SILENCE_MS = 6000;
const uint32_t NK_PING_MS = 1000;

const uint32_t NK_HOLD_MS = 30000;

const int NK_REL_QUEUE = 32;

const int NK_INBOX = 128;

const int NK_EVENTS = 64;

const int NK_PEER_PENDING = 1;
const int NK_PEER_CHALLENGED = 2;
const int NK_PEER_ACTIVE = 3;

const uint8_t NK_MEMBER_FREE = 0;
const uint8_t NK_MEMBER_JOINING = 1;
const uint8_t NK_MEMBER_ACTIVE = 2;
const uint8_t NK_MEMBER_AWAY = 3;

uint16_t nk_get16(const uint8_t *p) {
    return (uint16_t)((uint16_t)p[0] | ((uint16_t)p[1] << 8));
}

void nk_put16(uint8_t *p, uint16_t v) {
    p[0] = (uint8_t)(v & 0xFFu);
    p[1] = (uint8_t)((v >> 8) & 0xFFu);
}

uint32_t nk_get32(const uint8_t *p) {
    return ((uint32_t)p[0]) | ((uint32_t)p[1] << 8) | ((uint32_t)p[2] << 16) | ((uint32_t)p[3] << 24);
}

void nk_put32(uint8_t *p, uint32_t v) {
    p[0] = (uint8_t)(v & 0xFFu);
    p[1] = (uint8_t)((v >> 8) & 0xFFu);
    p[2] = (uint8_t)((v >> 16) & 0xFFu);
    p[3] = (uint8_t)((v >> 24) & 0xFFu);
}

void nk_member_name(char *dst, const char *src) {
    int at = 0;

    if (src != NULL) {
        while (at < NETKIT_NAME_MAX && src[at] != '\0') {
            const unsigned char c = (unsigned char)src[at];

            dst[at] = (char)((c >= 0x20 && c != 0x7F) ? c : ' ');
            at++;
        }
    }
    dst[at] = '\0';
}

uint32_t nk_now_ms() {
    static const std::chrono::steady_clock::time_point start = std::chrono::steady_clock::now();
    const std::chrono::milliseconds d =
        std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::steady_clock::now() - start);

    return (uint32_t)d.count();
}

int32_t nk_since(uint32_t now, uint32_t then) {
    return (int32_t)(now - then);
}

struct NK_ADDR {
    struct sockaddr_in sa;
    int set;
};

void nk_addr_reset(NK_ADDR *a) {
    memset(&a->sa, 0, sizeof a->sa);
    a->sa.sin_family = AF_INET;
    a->set = 0;
}

int nk_addr_same(const NK_ADDR *a, const NK_ADDR *b) {
    if (!a->set || !b->set)
        return 0;
    return a->sa.sin_family == b->sa.sin_family && a->sa.sin_port == b->sa.sin_port &&
           a->sa.sin_addr.s_addr == b->sa.sin_addr.s_addr;
}

struct NK_REL {
    uint8_t packet[NK_DGRAM_MAX];
    int len;
    uint32_t seq;
    uint32_t lastMs;
    int tries;
    int used;
};

struct NK_PEER {
    NK_ADDR addr;
    int used;
    int state;

    uint32_t memberId;
    uint32_t challenge;
    uint32_t challengeMs;

    uint32_t relNext;
    uint32_t relExpect;
    NK_REL out;
    NK_REL queue[NK_REL_QUEUE];
    int qHead;
    int qCount;

    uint32_t lastHeardMs;
    uint32_t lastPingMs;
};

struct NK_MEMBER {
    uint32_t id;
    char name[NETKIT_NAME_MAX + 1];
    uint8_t state;
    uint8_t isHost;
    uint32_t sinceMs;
    int peer;
};

struct NK_INBOX_SLOT {
    uint32_t from;
    uint32_t to;
    int len;
    int reliable;
    uint8_t bytes[NETKIT_MSG_MAX];
};

struct NK_EVENT_SLOT {
    NetKitEvent ev;
};

}

struct NetKit {
    nk_sock sock;
    int isHost;
    unsigned short port;
    int connected;
    int closed;
    int dead;

    NK_ADDR hostAddr;
    uint32_t localId;
    uint32_t hostId;
    uint32_t nextId;
    uint32_t seed;
    uint32_t counter;
    uint32_t startedMs;

    NK_PEER peer[NETKIT_MAX_MEMBERS];
    NK_MEMBER member[NETKIT_MAX_MEMBERS];

    NK_INBOX_SLOT inbox[NK_INBOX];
    int inboxHead;
    int inboxCount;

    NK_EVENT_SLOT events[NK_EVENTS];
    int evHead;
    int evCount;

    char name[NETKIT_NAME_MAX + 1];
    char lastError[160];
};

namespace {

#ifdef _WIN32
int nk_net_init() {
    static int started = 0;

    if (started)
        return 1;

    WSADATA wsa;
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
        return 0;
    started = 1;
    return 1;
}
#else
int nk_net_init() { return 1; }
#endif

void nk_set_socket_error(NetKit *k, const char *what) {
    if (k == NULL)
        return;
#ifdef _WIN32
    snprintf(k->lastError, sizeof k->lastError, "%s (winsock error %d)", what, (int)WSAGetLastError());
#else
    snprintf(k->lastError, sizeof k->lastError, "%s (%s)", what, strerror(errno));
#endif
}

void nk_close(nk_sock s) {
    if (s == NK_BAD_SOCK)
        return;
#ifdef _WIN32
    closesocket(s);
#else
    close(s);
#endif
}

int nk_nonblocking(nk_sock s) {
#ifdef _WIN32
    u_long on = 1;
    return ioctlsocket(s, FIONBIO, &on) == 0;
#else
    const int flags = fcntl(s, F_GETFL, 0);
    if (flags < 0)
        return 0;
    return fcntl(s, F_SETFL, flags | O_NONBLOCK) == 0;
#endif
}

nk_sock nk_open(unsigned short bindPort) {
    if (!nk_net_init())
        return NK_BAD_SOCK;

    nk_sock s = socket(AF_INET, SOCK_DGRAM, 0);
    if (s == NK_BAD_SOCK)
        return NK_BAD_SOCK;

    int one = 1;
    setsockopt(s, SOL_SOCKET, SO_REUSEADDR, (const char *)&one, (int)sizeof one);

    struct sockaddr_in sa;
    memset(&sa, 0, sizeof sa);
    sa.sin_family = AF_INET;
    sa.sin_addr.s_addr = htonl(INADDR_ANY);
    sa.sin_port = htons(bindPort);

    if (bind(s, (const struct sockaddr *)&sa, (nk_socklen)sizeof sa) != 0) {
        nk_close(s);
        return NK_BAD_SOCK;
    }

    if (!nk_nonblocking(s)) {
        nk_close(s);
        return NK_BAD_SOCK;
    }

    return s;
}

unsigned short nk_local_port(nk_sock s) {
    struct sockaddr_in sa;
    nk_socklen len = (nk_socklen)sizeof sa;

    if (getsockname(s, (struct sockaddr *)&sa, &len) != 0)
        return 0;
    return ntohs(sa.sin_port);
}

int nk_resolve(const char *host, unsigned short port, NK_ADDR *out) {
    struct addrinfo hints;
    struct addrinfo *list = NULL;
    char portText[16];
    int ok = 0;

    if (host == NULL || out == NULL)
        return 0;

    memset(&hints, 0, sizeof hints);
    hints.ai_family = AF_INET;
    hints.ai_socktype = SOCK_DGRAM;
    snprintf(portText, sizeof portText, "%u", (unsigned)port);

    if (getaddrinfo(host, portText, &hints, &list) != 0 || list == NULL ||
        (size_t)list->ai_addrlen < sizeof(struct sockaddr_in)) {
        if (list != NULL)
            freeaddrinfo(list);
        return 0;
    }

    nk_addr_reset(out);
    memcpy(&out->sa, list->ai_addr, sizeof out->sa);
    out->set = 1;
    ok = 1;
    freeaddrinfo(list);
    return ok;
}

int nk_send_packet(NetKit *k, const NK_ADDR *to, uint8_t type, uint8_t flags, uint32_t seq,
                   const void *payload, int len) {
    uint8_t buf[NK_DGRAM_MAX];
    int total;

    if (k == NULL || to == NULL || !to->set)
        return -1;
    if (len < 0 || len > NK_DGRAM_MAX - NK_HDR)
        return -1;

    buf[0] = NK_MAGIC;
    buf[1] = NK_PROTO;
    buf[2] = type;
    buf[3] = flags;
    nk_put16(buf + 4, (uint16_t)len);
    nk_put16(buf + 6, 0);
    nk_put32(buf + 8, seq);
    if (len > 0)
        memcpy(buf + NK_HDR, payload, (size_t)len);

    total = NK_HDR + len;

    if (sendto(k->sock, (const char *)buf, total, 0, (const struct sockaddr *)&to->sa,
               (nk_socklen)sizeof to->sa) != total) {
        nk_set_socket_error(k, "sendto");
        return -1;
    }
    return 0;
}

void nk_push_event(NetKit *k, int type, uint32_t id, const char *name, int reason) {
    NetKitEvent *ev;

    if (k->evCount == NK_EVENTS) {

        k->evHead = (k->evHead + 1) % NK_EVENTS;
        k->evCount--;
    }
    ev = &k->events[(k->evHead + k->evCount) % NK_EVENTS].ev;
    memset(ev, 0, sizeof *ev);
    ev->type = type;
    ev->id = id;
    ev->reason = reason;
    if (name != NULL)
        nk_member_name(ev->name, name);
    k->evCount++;
}

void nk_push_message(NetKit *k, uint32_t from, uint32_t to, const uint8_t *data, int len, int reliable) {
    NK_INBOX_SLOT *slot;

    if (k == NULL || data == NULL || len <= 0 || len > NETKIT_MSG_MAX)
        return;

    if (k->inboxCount == NK_INBOX) {
        k->inboxHead = (k->inboxHead + 1) % NK_INBOX;
        k->inboxCount--;
    }
    slot = &k->inbox[(k->inboxHead + k->inboxCount) % NK_INBOX];
    slot->from = from;
    slot->to = to;
    slot->len = len;
    slot->reliable = reliable;
    memcpy(slot->bytes, data, (size_t)len);
    k->inboxCount++;
}

int nk_member_slot(const NetKit *k, uint32_t id) {
    int i;

    if (id == 0)
        return -1;
    for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
        if (k->member[i].state != NK_MEMBER_FREE && k->member[i].id == id)
            return i;
    }
    return -1;
}

int nk_member_free_slot(const NetKit *k) {
    int i;

    for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
        if (k->member[i].state == NK_MEMBER_FREE)
            return i;
    }
    return -1;
}

int nk_member_count(const NetKit *k) {
    int i, n = 0;

    for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
        if (k->member[i].state == NK_MEMBER_ACTIVE || k->member[i].state == NK_MEMBER_JOINING)
            n++;
    }
    return n;
}

int nk_member_by_peer(const NetKit *k, int peer) {
    int i;

    for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
        if (k->member[i].state != NK_MEMBER_FREE && k->member[i].peer == peer)
            return i;
    }
    return -1;
}

int nk_peer_by_addr(const NetKit *k, const NK_ADDR *addr) {
    int i;

    for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
        if (k->peer[i].used && nk_addr_same(&k->peer[i].addr, addr))
            return i;
    }
    return -1;
}

NK_PEER *nk_peer_alloc(NetKit *k, const NK_ADDR *addr) {
    int i;

    for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
        NK_PEER *p = &k->peer[i];

        if (p->used)
            continue;
        memset(p, 0, sizeof *p);
        p->used = 1;
        p->state = NK_PEER_PENDING;
        p->addr = *addr;
        p->relNext = 1;
        p->relExpect = 1;
        p->lastHeardMs = nk_now_ms();
        p->lastPingMs = p->lastHeardMs;
        return p;
    }
    return NULL;
}

void nk_peer_free(NetKit *k, int index) {
    if (index < 0 || index >= NETKIT_MAX_MEMBERS)
        return;
    memset(&k->peer[index], 0, sizeof k->peer[index]);
    (void)k;
}

void nk_member_away(NetKit *k, uint32_t id, uint32_t now);
void nk_announce_leave(NetKit *k, uint32_t id);

void nk_drop_peer(NetKit *k, int index, uint32_t now, int tellGame) {
    if (k->isHost) {
        const int slot = nk_member_by_peer(k, index);

        if (slot >= 0) {
            const uint32_t gone = k->member[slot].id;

            k->member[slot].peer = -1;
            nk_member_away(k, gone, now);
            nk_announce_leave(k, gone);
            if (tellGame)
                nk_push_event(k, NETKIT_EVENT_LEFT, gone, NULL, NETKIT_REFUSE_NONE);
        }
    } else if (tellGame && !k->closed) {

        k->connected = 0;
        k->closed = 1;
        nk_push_event(k, NETKIT_EVENT_CLOSED, 0, NULL, NETKIT_REFUSE_NONE);
    }

    nk_peer_free(k, index);
}

void nk_rel_start(NetKit *k, NK_PEER *p, NK_REL *slot) {
    slot->tries = 0;
    slot->lastMs = nk_now_ms();
    nk_send_packet(k, &p->addr, slot->packet[2], NK_FLAG_RELIABLE, slot->seq, slot->packet + NK_HDR,
                   slot->len - NK_HDR);
}

void nk_rel_pump(NetKit *k, int index, uint32_t now) {
    NK_PEER *p = &k->peer[index];

    if (!p->used)
        return;

    if (p->out.used) {
        if (nk_since(now, p->out.lastMs) >= (int32_t)NK_RETRY_MS) {
            if (p->out.tries >= NK_RETRY_MAX) {

                nk_drop_peer(k, index, now, 1);
                return;
            }
            p->out.tries++;
            p->out.lastMs = now;
            nk_send_packet(k, &p->addr, p->out.packet[2], NK_FLAG_RELIABLE, p->out.seq,
                           p->out.packet + NK_HDR, p->out.len - NK_HDR);
        }
        return;
    }

    if (p->qCount > 0) {
        p->out = p->queue[p->qHead];
        p->qHead = (p->qHead + 1) % NK_REL_QUEUE;
        p->qCount--;
        p->out.used = 1;
        nk_rel_start(k, p, &p->out);
    }
}

int nk_rel_send(NetKit *k, NK_PEER *p, uint8_t type, const void *payload, int len) {
    NK_REL built;
    NK_REL *slot;

    if (!p->used || len < 0 || len > NK_REL_MAX)
        return -1;

    memset(&built, 0, sizeof built);
    built.packet[0] = NK_MAGIC;
    built.packet[1] = NK_PROTO;
    built.packet[2] = type;
    built.packet[3] = NK_FLAG_RELIABLE;
    nk_put16(built.packet + 4, (uint16_t)len);
    nk_put16(built.packet + 6, 0);
    nk_put32(built.packet + 8, 0);
    if (len > 0)
        memcpy(built.packet + NK_HDR, payload, (size_t)len);
    built.len = NK_HDR + len;

    if (!p->out.used && p->qCount == 0) {
        p->out = built;
        p->out.used = 1;
        p->out.seq = p->relNext++;
        nk_put32(p->out.packet + 8, p->out.seq);
        nk_rel_start(k, p, &p->out);
        return 0;
    }

    if (p->qCount == NK_REL_QUEUE) {

        return -1;
    }

    slot = &p->queue[(p->qHead + p->qCount) % NK_REL_QUEUE];
    *slot = built;
    slot->used = 1;
    slot->seq = p->relNext++;
    nk_put32(slot->packet + 8, slot->seq);
    p->qCount++;
    return 0;
}

void nk_rel_ack(NetKit *k, int index, uint32_t seq, uint32_t now) {
    NK_PEER *p = &k->peer[index];

    if (!p->out.used || p->out.seq != seq)
        return;
    p->out.used = 0;
    nk_rel_pump(k, index, now);
}

int nk_put_name(uint8_t *out, int max, const char *name) {
    char cut[NETKIT_NAME_MAX + 1];
    int n;

    nk_member_name(cut, name);
    n = (int)strlen(cut);
    if (n > max - 1)
        n = max - 1;
    out[0] = (uint8_t)n;
    if (n > 0)
        memcpy(out + 1, cut, (size_t)n);
    return 1 + n;
}

uint32_t nk_admit(NetKit *k, int peerIndex, const char *name, uint32_t now) {
    int slot = nk_member_free_slot(k);
    NK_MEMBER *m;

    if (slot < 0)
        return 0;
    m = &k->member[slot];
    memset(m, 0, sizeof *m);
    m->id = k->nextId++;
    m->state = NK_MEMBER_ACTIVE;
    m->isHost = 0;
    m->sinceMs = now;
    m->peer = peerIndex;
    nk_member_name(m->name, name);
    return m->id;
}

void nk_member_away(NetKit *k, uint32_t id, uint32_t now) {
    const int slot = nk_member_slot(k, id);

    if (slot < 0)
        return;
    if (k->member[slot].state == NK_MEMBER_AWAY)
        return;
    k->member[slot].state = NK_MEMBER_AWAY;
    k->member[slot].sinceMs = now;
    k->member[slot].peer = -1;
}

void nk_send_member_add(NetKit *k, NK_PEER *p, const NK_MEMBER *m) {
    uint8_t payload[NK_REL_MAX];
    int at = 0;

    nk_put32(payload + at, m->id);
    at += 4;
    payload[at++] = (uint8_t)(m->isHost ? 1 : 0);
    at += nk_put_name(payload + at, NK_REL_MAX - at, m->name);
    nk_rel_send(k, p, NK_MEMBER_ADD, payload, at);
}

void nk_announce_room(NetKit *k, int joinerPeer, uint32_t joinerId) {
    int i;

    for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
        const NK_MEMBER *m = &k->member[i];
        int q;

        if (m->state == NK_MEMBER_FREE)
            continue;

        if (m->id == joinerId) {
            for (q = 0; q < NETKIT_MAX_MEMBERS; q++) {
                if (q == joinerPeer || !k->peer[q].used || k->peer[q].state != NK_PEER_ACTIVE)
                    continue;
                nk_send_member_add(k, &k->peer[q], m);
            }
        } else if (m->state == NK_MEMBER_ACTIVE || m->state == NK_MEMBER_JOINING) {
            nk_send_member_add(k, &k->peer[joinerPeer], m);
        }
    }
}

void nk_announce_leave(NetKit *k, uint32_t id) {
    uint8_t payload[4];
    int i;

    nk_put32(payload, id);
    for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
        if (k->peer[i].used && k->peer[i].state == NK_PEER_ACTIVE)
            nk_rel_send(k, &k->peer[i], NK_MEMBER_LEAVE, payload, 4);
    }
}

void nk_host_refuse(NetKit *k, NK_PEER *p, int reason) {
    uint8_t payload[2];

    payload[0] = NK_PROTO;
    payload[1] = (uint8_t)reason;
    nk_rel_send(k, p, NK_REFUSE, payload, 2);
    p->state = NK_PEER_PENDING;
}

void nk_host_hello(NetKit *k, NK_PEER *p, const uint8_t *payload, int len, uint32_t now) {
    uint8_t out[5];

    if (len < 1 || payload[0] != NK_PROTO) {
        nk_host_refuse(k, p, NETKIT_REFUSE_PROTO);
        return;
    }

    if (nk_member_free_slot(k) < 0) {
        nk_host_refuse(k, p, NETKIT_REFUSE_FULL);
        return;
    }

    if (p->state == NK_PEER_ACTIVE) {

        const int slot = nk_member_by_peer(k, (int)(p - k->peer));

        if (slot >= 0) {
            const uint32_t gone = k->member[slot].id;

            nk_member_away(k, gone, now);
            nk_announce_leave(k, gone);
            nk_push_event(k, NETKIT_EVENT_LEFT, gone, NULL, NETKIT_REFUSE_NONE);
        }

        p->relNext = 1;
        p->out.used = 0;
        p->qCount = 0;
        p->qHead = 0;
    }

    k->counter++;
    p->challenge = (k->seed * 1103515245u + 12345u) ^ (k->counter * 2654435761u) ^ (k->counter << 13);
    p->challengeMs = now;
    p->state = NK_PEER_CHALLENGED;
    p->lastHeardMs = now;

    out[0] = NK_PROTO;
    nk_put32(out + 1, p->challenge);
    nk_rel_send(k, p, NK_CHALLENGE, out, 5);
}

void nk_host_connect(NetKit *k, NK_PEER *p, const uint8_t *payload, int len, uint32_t now) {
    uint8_t out[10];
    char name[NETKIT_NAME_MAX + 1];
    uint32_t slot;
    int nameLen;

    if (p->state != NK_PEER_CHALLENGED)
        return;

    if (len < 10 || payload[0] != NK_PROTO) {
        nk_host_refuse(k, p, NETKIT_REFUSE_PROTO);
        return;
    }
    if (nk_get32(payload + 1) != p->challenge) {
        nk_host_refuse(k, p, NETKIT_REFUSE_CHALLENGE);
        return;
    }

    nameLen = payload[9];
    if (nameLen > NETKIT_NAME_MAX || 10 + nameLen > len) {
        nk_host_refuse(k, p, NETKIT_REFUSE_BAD);
        return;
    }
    if (nameLen > 0)
        memcpy(name, payload + 10, (size_t)nameLen);
    name[nameLen] = '\0';

    slot = nk_admit(k, (int)(p - k->peer), name, now);
    if (slot == 0) {
        nk_host_refuse(k, p, NETKIT_REFUSE_FULL);
        return;
    }

    p->memberId = slot;
    p->state = NK_PEER_ACTIVE;
    p->lastHeardMs = now;

    out[0] = NK_PROTO;
    nk_put32(out + 1, slot);
    nk_put32(out + 5, k->localId);
    nk_rel_send(k, p, NK_ACCEPT, out, 9);

    nk_announce_room(k, (int)(p - k->peer), slot);
    nk_push_event(k, NETKIT_EVENT_JOINED, slot, name, NETKIT_REFUSE_NONE);
}

void nk_client_challenge(NetKit *k, NK_PEER *p, const uint8_t *payload, int len, uint32_t now) {
    uint8_t out[10 + NETKIT_NAME_MAX];
    int at = 0;

    if (len < 5 || payload[0] != NK_PROTO)
        return;

    p->challenge = nk_get32(payload + 1);
    p->lastHeardMs = now;

    out[at++] = NK_PROTO;
    nk_put32(out + at, p->challenge);
    at += 4;
    nk_put32(out + at, 0);
    at += 4;
    out[at] = (uint8_t)strlen(k->name);
    memcpy(out + at + 1, k->name, (size_t)out[at]);
    at += 1 + (int)out[at];

    nk_rel_send(k, p, NK_CONNECT, out, at);
}

void nk_client_accept(NetKit *k, NK_PEER *p, const uint8_t *payload, int len) {
    int slot;

    if (len < 9 || payload[0] != NK_PROTO)
        return;
    if (k->connected)
        return;

    k->localId = nk_get32(payload + 1);
    k->hostId = nk_get32(payload + 5);
    k->connected = 1;
    p->memberId = k->localId;
    p->state = NK_PEER_ACTIVE;

    slot = nk_member_free_slot(k);
    if (slot >= 0) {
        NK_MEMBER *m = &k->member[slot];

        memset(m, 0, sizeof *m);
        m->id = k->localId;
        m->state = NK_MEMBER_ACTIVE;
        m->isHost = 0;
        m->sinceMs = nk_now_ms();
        m->peer = (int)(p - k->peer);
        nk_member_name(m->name, k->name);
    }

    slot = nk_member_slot(k, k->hostId);
    if (k->hostId != 0 && slot < 0) {
        slot = nk_member_free_slot(k);
        if (slot >= 0) {
            NK_MEMBER *m = &k->member[slot];

            memset(m, 0, sizeof *m);
            m->id = k->hostId;
            m->state = NK_MEMBER_ACTIVE;
            m->isHost = 1;
            m->sinceMs = nk_now_ms();
            m->peer = -1;
            nk_member_name(m->name, "host");
        }
    }
}

void nk_client_member_add(NetKit *k, const uint8_t *payload, int len, uint32_t now) {
    uint32_t id;
    int nameLen;
    char name[NETKIT_NAME_MAX + 1];
    int isHost;
    int slot;
    int fresh;

    if (len < 6)
        return;
    id = nk_get32(payload);
    isHost = payload[4] & 1;
    nameLen = payload[5];
    if (nameLen > NETKIT_NAME_MAX || 6 + nameLen > len)
        return;
    if (nameLen > 0)
        memcpy(name, payload + 6, (size_t)nameLen);
    name[nameLen] = '\0';

    slot = nk_member_slot(k, id);
    fresh = (slot < 0 || k->member[slot].state == NK_MEMBER_AWAY);
    if (slot < 0) {
        slot = nk_member_free_slot(k);
        if (slot < 0)
            return;
        memset(&k->member[slot], 0, sizeof k->member[slot]);
        k->member[slot].id = id;
    }

    k->member[slot].state = NK_MEMBER_ACTIVE;
    k->member[slot].isHost = (uint8_t)(isHost ? 1 : 0);
    k->member[slot].sinceMs = now;
    nk_member_name(k->member[slot].name, name);

    if (fresh && id != k->localId)
        nk_push_event(k, NETKIT_EVENT_JOINED, id, k->member[slot].name, NETKIT_REFUSE_NONE);
}

void nk_client_member_leave(NetKit *k, const uint8_t *payload, int len, uint32_t now) {
    uint32_t id;
    int slot;

    if (len < 4)
        return;
    id = nk_get32(payload);
    slot = nk_member_slot(k, id);
    if (slot < 0)
        return;

    k->member[slot].state = NK_MEMBER_AWAY;
    k->member[slot].sinceMs = now;
    k->member[slot].peer = -1;
    nk_push_event(k, NETKIT_EVENT_LEFT, id, NULL, NETKIT_REFUSE_NONE);
}

void nk_client_refuse(NetKit *k, const uint8_t *payload, int len) {
    int reason = NETKIT_REFUSE_BAD;

    if (len >= 2)
        reason = payload[1];

    k->connected = 0;
    k->closed = 1;
    nk_push_event(k, NETKIT_EVENT_REFUSED, 0, NULL, reason);
}

void nk_deliver_or_relay(NetKit *k, int fromPeer, const uint8_t *payload, int len, int reliable) {
    uint32_t from;
    uint32_t to;
    const uint8_t *body;
    int bodyLen;
    int slot;

    if (len < 8)
        return;

    from = nk_get32(payload);
    to = nk_get32(payload + 4);
    body = payload + 8;
    bodyLen = len - 8;

    if (bodyLen <= 0 || bodyLen > NETKIT_MSG_MAX)
        return;

    if (!k->isHost) {
        nk_push_message(k, from, to, body, bodyLen, reliable);
        return;
    }

    /* The host knows who sent this by the peer it came from: the id in the packet is only a claim. */
    slot = nk_member_by_peer(k, fromPeer);
    if (slot < 0 || k->member[slot].state != NK_MEMBER_ACTIVE)
        return;
    from = k->member[slot].id;

    if (to == 0) {
        int q;

        /* A broadcast is for every member, the host included. */
        nk_push_message(k, from, to, body, bodyLen, reliable);

        for (q = 0; q < NETKIT_MAX_MEMBERS; q++) {
            uint8_t out[NK_REL_MAX];

            if (q == fromPeer || !k->peer[q].used || k->peer[q].state != NK_PEER_ACTIVE)
                continue;
            if (bodyLen + 8 > NK_REL_MAX)
                continue;
            nk_put32(out, from);
            nk_put32(out + 4, 0);
            memcpy(out + 8, body, (size_t)bodyLen);
            if (reliable)
                nk_rel_send(k, &k->peer[q], NK_APP, out, bodyLen + 8);
            else
                nk_send_packet(k, &k->peer[q].addr, NK_APP, 0, 0, out, bodyLen + 8);
        }
        return;
    }

    slot = nk_member_slot(k, to);
    if (slot < 0)
        return;

    if (k->member[slot].peer == fromPeer)
        return;

    if (k->member[slot].peer < 0) {
        nk_push_message(k, from, to, body, bodyLen, reliable);
        return;
    }

    {
        NK_PEER *q = &k->peer[k->member[slot].peer];
        uint8_t out[NK_REL_MAX];

        if (bodyLen + 8 > NK_REL_MAX)
            return;
        nk_put32(out, from);
        nk_put32(out + 4, to);
        memcpy(out + 8, body, (size_t)bodyLen);
        if (reliable)
            nk_rel_send(k, q, NK_APP, out, bodyLen + 8);
        else
            nk_send_packet(k, &q->addr, NK_APP, 0, 0, out, bodyLen + 8);
    }
}

void nk_handle_packet(NetKit *k, const uint8_t *buf, int n, const NK_ADDR *from, uint32_t now) {
    int peer;
    NK_PEER *p;
    const uint8_t *payload;
    int len;
    uint8_t type;
    uint8_t flags;
    uint32_t seq;

    if (n < NK_HDR || buf[0] != NK_MAGIC || buf[1] != NK_PROTO)
        return;

    type = buf[2];
    flags = buf[3];
    len = (int)nk_get16(buf + 4);
    seq = nk_get32(buf + 8);
    payload = buf + NK_HDR;

    if (len < 0 || NK_HDR + len > n)
        return;

    peer = nk_peer_by_addr(k, from);
    if (peer < 0) {

        if (!k->isHost || type != NK_HELLO)
            return;
        p = nk_peer_alloc(k, from);
        if (p == NULL)
            return;
        peer = (int)(p - k->peer);
    }

    p = &k->peer[peer];
    if (!p->used)
        return;
    p->lastHeardMs = now;

    if (flags & NK_FLAG_RELIABLE) {
        /* Only a fresh peer starts over. A HELLO repeated while the challenge is out (its ack is slow or lost) has to fall through as a
           duplicate: starting over here issued a new challenge the joiner never saw, and its CONNECT was refused. */
        if (type == NK_HELLO && p->state == NK_PEER_PENDING) {

            p->relExpect = seq;
            p->relNext = 1;
            p->out.used = 0;
            p->qCount = 0;
            p->qHead = 0;
        }

        if (seq == p->relExpect) {
            p->relExpect++;
        } else if ((int32_t)(seq - p->relExpect) < 0) {

            nk_send_packet(k, &p->addr, NK_ACK, 0, seq, NULL, 0);
            return;
        } else {
            return;
        }

        nk_send_packet(k, &p->addr, NK_ACK, 0, seq, NULL, 0);
    }

    switch (type) {
    case NK_ACK:
        nk_rel_ack(k, peer, seq, now);
        return;

    case NK_HELLO:
        if (!k->isHost)
            return;
        nk_host_hello(k, p, payload, len, now);
        return;

    case NK_CHALLENGE:
        if (k->isHost)
            return;
        nk_client_challenge(k, p, payload, len, now);
        return;

    case NK_CONNECT:
        if (!k->isHost)
            return;
        nk_host_connect(k, p, payload, len, now);
        return;

    case NK_ACCEPT:
        if (k->isHost)
            return;
        nk_client_accept(k, p, payload, len);
        return;

    case NK_REFUSE:
        if (!k->isHost)
            nk_client_refuse(k, payload, len);
        return;

    case NK_MEMBER_ADD:
        if (!k->isHost)
            nk_client_member_add(k, payload, len, now);
        return;

    case NK_MEMBER_LEAVE:
        if (!k->isHost)
            nk_client_member_leave(k, payload, len, now);
        return;

    case NK_APP:
        nk_deliver_or_relay(k, peer, payload, len, (flags & NK_FLAG_RELIABLE) ? 1 : 0);
        return;

    case NK_BYE:
        if (k->isHost) {

            nk_drop_peer(k, peer, now, 1);
        } else {
            k->connected = 0;
            k->closed = 1;
            nk_push_event(k, NETKIT_EVENT_CLOSED, 0, NULL, NETKIT_REFUSE_NONE);
        }
        return;

    case NK_PING:
        return;

    default:
        return;
    }
}

void nk_expire_members(NetKit *k, uint32_t now) {
    int i;

    for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
        NK_MEMBER *m = &k->member[i];

        if (m->state != NK_MEMBER_AWAY)
            continue;
        if (nk_since(now, m->sinceMs) >= (int32_t)NK_HOLD_MS)
            memset(m, 0, sizeof *m);
    }
}

void nk_ping_all(NetKit *k, uint32_t now) {
    int i;

    for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
        NK_PEER *p = &k->peer[i];

        if (!p->used)
            continue;

        if (p->state == NK_PEER_PENDING)
            continue;
        if (nk_since(now, p->lastPingMs) < (int32_t)NK_PING_MS)
            continue;
        p->lastPingMs = now;
        nk_send_packet(k, &p->addr, NK_PING, 0, 0, NULL, 0);
    }
}

void nk_reap_silent(NetKit *k, uint32_t now) {
    int i;

    for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
        NK_PEER *p = &k->peer[i];

        if (!p->used)
            continue;
        if (nk_since(now, p->lastHeardMs) < (int32_t)NK_SILENCE_MS)
            continue;
        nk_drop_peer(k, i, now, 1);
    }
}

}

NetKit *NetKit_Host(unsigned short port, const char *name) {
    NetKit *k = (NetKit *)calloc(1, sizeof(NetKit));

    if (k == NULL)
        return NULL;

    k->sock = nk_open(port);
    if (k->sock == NK_BAD_SOCK) {

        fprintf(stderr, "netkit: could not open UDP port %u for hosting\n", (unsigned)port);
        free(k);
        return NULL;
    }

    k->isHost = 1;
    k->port = nk_local_port(k->sock);
    k->localId = 1;
    k->hostId = 1;
    k->nextId = 2;
    k->connected = 1;
    k->startedMs = nk_now_ms();
    k->seed = (uint32_t)(k->startedMs ^ (uint32_t)k->port ^ 0x9E3779B9u);
    nk_member_name(k->name, name != NULL ? name : "host");

    {
        NK_MEMBER *m = &k->member[0];

        memset(m, 0, sizeof *m);
        m->id = k->localId;
        m->state = NK_MEMBER_ACTIVE;
        m->isHost = 1;
        m->sinceMs = k->startedMs;
        m->peer = -1;
        nk_member_name(m->name, k->name);
    }

    return k;
}

NetKit *NetKit_Join(const char *host, unsigned short port, const char *name) {
    NetKit *k;
    NK_PEER *p;

    if (host == NULL)
        return NULL;

    k = (NetKit *)calloc(1, sizeof(NetKit));
    if (k == NULL)
        return NULL;

    k->sock = nk_open(0);
    if (k->sock == NK_BAD_SOCK) {
        fprintf(stderr, "netkit: could not open a UDP socket to join %s:%u\n", host, (unsigned)port);
        free(k);
        return NULL;
    }

    if (!nk_resolve(host, port, &k->hostAddr)) {
        snprintf(k->lastError, sizeof k->lastError, "cannot resolve %s", host);
        nk_close(k->sock);
        free(k);
        return NULL;
    }

    k->isHost = 0;
    k->port = nk_local_port(k->sock);
    k->nextId = 2;
    k->startedMs = nk_now_ms();
    k->seed = (uint32_t)(k->startedMs ^ 0x9E3779B9u);
    nk_member_name(k->name, name != NULL ? name : "player");

    p = nk_peer_alloc(k, &k->hostAddr);
    if (p != NULL) {
        uint8_t hello[1];

        hello[0] = NK_PROTO;
        nk_rel_send(k, p, NK_HELLO, hello, 1);
    }

    return k;
}

void NetKit_Stop(NetKit *k) {
    int i;

    if (k == NULL)
        return;

    if (k->sock != NK_BAD_SOCK && !k->closed) {
        if (k->isHost) {
            for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
                if (k->peer[i].used && k->peer[i].state == NK_PEER_ACTIVE)
                    nk_send_packet(k, &k->peer[i].addr, NK_BYE, 0, 0, NULL, 0);
            }
        } else {

            for (i = 0; i < 3; i++) {
                if (k->peer[0].used)
                    nk_send_packet(k, &k->peer[0].addr, NK_BYE, 0, 0, NULL, 0);
            }
        }
    }

    nk_close(k->sock);
    free(k);
}

int NetKit_Poll(NetKit *k) {
    uint8_t buf[NK_DGRAM_MAX];
    uint32_t now;
    int handled = 0;

    if (k == NULL || k->dead)
        return -1;
    if (k->closed)
        return 0;

    now = nk_now_ms();

    for (int i = 0; i < 256; i++) {
        NK_ADDR from;
        int n;

        nk_addr_reset(&from);
        {
            nk_socklen len = (nk_socklen)sizeof from.sa;

            n = (int)recvfrom(k->sock, (char *)buf, NK_DGRAM_MAX, 0, (struct sockaddr *)&from.sa, &len);
        }

        if (n <= 0) {
#ifdef _WIN32
            const int err = WSAGetLastError();

            /* On Windows these belong to one datagram, not the socket: WSAECONNRESET is a packet we sent to a peer whose port has closed,
               WSAEMSGSIZE a datagram too big for buf (it is discarded). A silent peer is reaped by nk_reap_silent. */
            if (n < 0 && (err == WSAECONNRESET || err == WSAENETRESET || err == WSAEMSGSIZE))
                continue;

            if (n < 0 && err != WSAEWOULDBLOCK)
                nk_set_socket_error(k, "recvfrom");
            if (n < 0 && err != WSAEWOULDBLOCK) {
                k->dead = 1;
                return -1;
            }
#else
            if (n < 0 && errno != EAGAIN && errno != EWOULDBLOCK && errno != EINTR) {
                nk_set_socket_error(k, "recvfrom");
                k->dead = 1;
                return -1;
            }
#endif
            break;
        }

        from.set = 1;
        nk_handle_packet(k, buf, n, &from, now);
        handled++;
    }

    for (int i = 0; i < NETKIT_MAX_MEMBERS; i++)
        nk_rel_pump(k, i, now);

    nk_ping_all(k, now);
    nk_reap_silent(k, now);
    nk_expire_members(k, now);

    if (!k->isHost && !k->connected && nk_since(now, k->startedMs) >= (int32_t)NK_JOIN_MS) {
        k->closed = 1;
        nk_push_event(k, NETKIT_EVENT_REFUSED, 0, NULL, NETKIT_REFUSE_TIMEOUT);
    }

    return handled;
}

int NetKit_Connected(const NetKit *k) { return (k != NULL && k->connected && !k->closed) ? 1 : 0; }

int NetKit_IsHost(const NetKit *k) { return (k != NULL && k->isHost) ? 1 : 0; }

uint32_t NetKit_LocalId(const NetKit *k) { return (k != NULL) ? k->localId : 0u; }

unsigned short NetKit_Port(const NetKit *k) { return (k != NULL) ? k->port : 0; }

int NetKit_Count(const NetKit *k) {
    if (k == NULL || k->closed)
        return 0;
    return nk_member_count(k);
}

const NetKitMember *NetKit_MemberAt(const NetKit *k, int index) {
    static NetKitMember out;
    int seen = 0;
    int pass;
    int i;

    if (k == NULL || index < 0)
        return NULL;

    /* Members away come after the rest: NetKit_Count leaves them out, so a loop to the count has to reach everyone who is here. */
    for (pass = 0; pass < 2; pass++) {
        for (i = 0; i < NETKIT_MAX_MEMBERS; i++) {
            const NK_MEMBER *m = &k->member[i];

            if (pass == 0 ? (m->state != NK_MEMBER_ACTIVE && m->state != NK_MEMBER_JOINING) : m->state != NK_MEMBER_AWAY)
                continue;
            if (seen++ != index)
                continue;

            out.id = m->id;
            out.isHost = m->isHost ? 1 : 0;
            out.active = (m->state == NK_MEMBER_ACTIVE) ? 1 : 0;
            memcpy(out.name, m->name, sizeof out.name);
            return &out;
        }
    }
    return NULL;
}

const NetKitMember *NetKit_MemberById(const NetKit *k, uint32_t id) {
    int index;
    const NetKitMember *m;

    if (k == NULL || id == 0)
        return NULL;

    for (index = 0; (m = NetKit_MemberAt(k, index)) != NULL; index++) {
        if (m->id == id)
            return m;
    }
    return NULL;
}

int NetKit_Kick(NetKit *k, uint32_t id, int reason) {
    const uint32_t now = nk_now_ms();
    int slot, peer;

    if (!k->isHost || id == k->localId)
        return 0;
    slot = nk_member_slot(k, id);
    if (slot < 0 || k->member[slot].state == NK_MEMBER_AWAY)
        return 0;
    peer = k->member[slot].peer;
    nk_member_away(k, id, now);
    nk_announce_leave(k, id);
    nk_push_event(k, NETKIT_EVENT_LEFT, id, NULL, NETKIT_REFUSE_NONE);
    if (peer >= 0)
        nk_host_refuse(k, &k->peer[peer], reason);
    return 1;
}

int NetKit_Send(NetKit *k, uint32_t to, const void *data, int len, int reliable) {
    uint8_t payload[NK_REL_MAX];

    if (k == NULL || data == NULL || len <= 0 || len > NETKIT_MSG_MAX)
        return -1;
    if (!k->connected || k->closed)
        return -1;

    if (k->isHost) {
        if (to == 0) {
            int q, sent = 0;

            for (q = 0; q < NETKIT_MAX_MEMBERS; q++) {
                NK_PEER *p = &k->peer[q];

                if (!p->used || p->state != NK_PEER_ACTIVE)
                    continue;
                nk_put32(payload, k->localId);
                nk_put32(payload + 4, 0);
                memcpy(payload + 8, data, (size_t)len);
                if (reliable ? (nk_rel_send(k, p, NK_APP, payload, len + 8) == 0)
                             : (nk_send_packet(k, &p->addr, NK_APP, 0, 0, payload, len + 8) == 0))
                    sent++;
            }
            return sent;
        }

        {
            const int slot = nk_member_slot(k, to);

            if (slot < 0)
                return -1;
            if (k->member[slot].peer < 0)
                return -1;
            {
                NK_PEER *p = &k->peer[k->member[slot].peer];

                nk_put32(payload, k->localId);
                nk_put32(payload + 4, to);
                memcpy(payload + 8, data, (size_t)len);
                if (reliable)
                    return nk_rel_send(k, p, NK_APP, payload, len + 8) == 0 ? 1 : -1;
                return nk_send_packet(k, &p->addr, NK_APP, 0, 0, payload, len + 8) == 0 ? 1 : -1;
            }
        }
    }

    {
        NK_PEER *p = &k->peer[0];

        if (!p->used)
            return -1;
        nk_put32(payload, k->localId);
        nk_put32(payload + 4, to);
        memcpy(payload + 8, data, (size_t)len);
        if (reliable)
            return nk_rel_send(k, p, NK_APP, payload, len + 8) == 0 ? 1 : -1;
        return nk_send_packet(k, &p->addr, NK_APP, 0, 0, payload, len + 8) == 0 ? 1 : -1;
    }
}

int NetKit_Broadcast(NetKit *k, const void *data, int len, int reliable) {

    return NetKit_Send(k, 0, data, len, reliable);
}

int NetKit_NextMessage(NetKit *k, NetKitMessage *out, void *buf, int max) {
    NK_INBOX_SLOT *slot;

    if (k == NULL || out == NULL || buf == NULL)
        return 0;
    if (k->inboxCount == 0)
        return 0;

    slot = &k->inbox[k->inboxHead];
    if (slot->len > max)
        return -1;

    memcpy(buf, slot->bytes, (size_t)slot->len);
    out->from = slot->from;
    out->to = slot->to;
    out->data = buf;
    out->len = slot->len;
    out->reliable = slot->reliable;

    k->inboxHead = (k->inboxHead + 1) % NK_INBOX;
    k->inboxCount--;
    return 1;
}

int NetKit_NextEvent(NetKit *k, NetKitEvent *out) {
    if (k == NULL || out == NULL)
        return 0;
    if (k->evCount == 0)
        return 0;

    *out = k->events[k->evHead].ev;
    k->evHead = (k->evHead + 1) % NK_EVENTS;
    k->evCount--;
    return 1;
}

const char *NetKit_RefuseText(int reason) {
    switch (reason) {
    case NETKIT_REFUSE_FULL:
        return "the room is full";
    case NETKIT_REFUSE_PROTO:
        return "the peer speaks a different multiplayer protocol";
    case NETKIT_REFUSE_CHALLENGE:
        return "the connect did not echo the challenge that was sent";
    case NETKIT_REFUSE_CLOSED:
        return "this session is not taking joiners";
    case NETKIT_REFUSE_TIMEOUT:
        return "the join window ran out";
    case NETKIT_REFUSE_PASSWORD:
        return "wrong password";
    case NETKIT_REFUSE_VERSION:
        return "another version of the game";
    case NETKIT_REFUSE_BAD:
        return "the peer is not speaking this protocol";
    default:
        return "";
    }
}

const char *NetKit_LastError(const NetKit *k) { return (k != NULL) ? k->lastError : ""; }

/* WOW you really looked at all my source, you fucking pervert */