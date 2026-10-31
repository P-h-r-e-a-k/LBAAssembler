#ifndef NETKIT_H
#define NETKIT_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#define NETKIT_MAX_MEMBERS 16

#define NETKIT_NAME_MAX 16

#define NETKIT_MSG_MAX 1184

typedef struct {
    uint32_t id;
    char name[NETKIT_NAME_MAX + 1];
    int isHost;
    int active;
} NetKitMember;

typedef struct {
    uint32_t from;
    uint32_t to;
    const void *data;
    int len;
    int reliable;
} NetKitMessage;

enum {
    NETKIT_EVENT_NONE = 0,
    NETKIT_EVENT_JOINED = 1,
    NETKIT_EVENT_LEFT = 2,
    NETKIT_EVENT_REFUSED = 3,
    NETKIT_EVENT_CLOSED = 4
};

typedef struct {
    int type;
    uint32_t id;
    char name[NETKIT_NAME_MAX + 1];
    int reason;
} NetKitEvent;

enum {
    NETKIT_REFUSE_NONE = 0,
    NETKIT_REFUSE_FULL = 1,
    NETKIT_REFUSE_PROTO = 2,
    NETKIT_REFUSE_CHALLENGE = 3,
    NETKIT_REFUSE_CLOSED = 4,
    NETKIT_REFUSE_TIMEOUT = 5,
    NETKIT_REFUSE_BAD = 6,
    NETKIT_REFUSE_PASSWORD = 7,
    NETKIT_REFUSE_VERSION = 8
};

struct NetKit *NetKit_Host(unsigned short port, const char *name);

struct NetKit *NetKit_Join(const char *host, unsigned short port, const char *name);

void NetKit_Stop(struct NetKit *kit);

int NetKit_Poll(struct NetKit *kit);

int NetKit_Connected(const struct NetKit *kit);

int NetKit_IsHost(const struct NetKit *kit);
uint32_t NetKit_LocalId(const struct NetKit *kit);

int NetKit_Count(const struct NetKit *kit);

const NetKitMember *NetKit_MemberAt(const struct NetKit *kit, int index);
const NetKitMember *NetKit_MemberById(const struct NetKit *kit, uint32_t id);

unsigned short NetKit_Port(const struct NetKit *kit);

int NetKit_Send(struct NetKit *kit, uint32_t to, const void *data, int len, int reliable);

int NetKit_Broadcast(struct NetKit *kit, const void *data, int len, int reliable);

/* Host only: puts a member out, telling them why (a NETKIT_REFUSE_ reason). Non-zero if there was such a member. */
int NetKit_Kick(struct NetKit *kit, uint32_t id, int reason);

int NetKit_NextMessage(struct NetKit *kit, NetKitMessage *out, void *buf, int max);

int NetKit_NextEvent(struct NetKit *kit, NetKitEvent *out);

const char *NetKit_RefuseText(int reason);

const char *NetKit_LastError(const struct NetKit *kit);

#ifdef __cplusplus
}
#endif

#endif
