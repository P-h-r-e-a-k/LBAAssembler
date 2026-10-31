// Asks the router to open the port a hosted online race runs on (the user, 2026-10-30: "let's have the game ask the router to open the port, and if it fails
// alert the user"). UPnP's Internet Gateway Device: find the router by SSDP, read its description, then SOAP over HTTP to its WAN connection.
//
// From the user's router, a Sagemcom F@st 5364, 2026-10-30, which any router may share:
// - GetSpecificPortMappingEntry on a port with no mapping crashed its UPnP service, which came back on another TCP port. So that is asked only after adding,
//   and the router is looked for afresh every time: nothing is kept from an earlier search.
// - A router can list a WAN connection it doesn't use: the one that says what its outside address is is the one.
// - Some refuse a timed port (725): then asked for one with no time, which NetUpnp_Close takes off again.
// - An outside address that is private, or 100.64/10: the provider's own network is in front of this router, and an open port reaches nobody.
#include "NETUPNP.H"

#include <SDL3/SDL.h>
#include <SYSTEM/LOG.H>

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
typedef SOCKET T_SOCK;
#define BAD_SOCK INVALID_SOCKET
#define CloseSock closesocket
#else
#include <arpa/inet.h>
#include <netdb.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <sys/time.h>
#include <unistd.h>
typedef int T_SOCK;
#define BAD_SOCK (-1)
#define CloseSock close
#endif

#define UPNP_DESCRIPTION "LBRA online race"
#define UPNP_LEASE 7200                     // seconds: a game that crashes doesn't leave the port open for good
#define UPNP_REPLY_MAX 65536
#define UPNP_LOCATIONS 8

static SDL_Mutex *s_lock = NULL;
static SDL_Thread *s_thread = NULL;
static int s_done = 0, s_ok = 0;
static char s_text[200] = "", s_outside[64] = "", s_inside[64] = "";
static unsigned short s_port = 0;
static char s_control[256] = "", s_service[128] = "";   // what opened it, to close it with

static void SetTimeouts(T_SOCK s, int ms) {
#ifdef _WIN32
    const DWORD t = (DWORD)ms;
#else
    struct timeval t;
    t.tv_sec = ms / 1000; t.tv_usec = (ms % 1000) * 1000;
#endif
    setsockopt(s, SOL_SOCKET, SO_RCVTIMEO, (const char *)&t, sizeof(t));
    setsockopt(s, SOL_SOCKET, SO_SNDTIMEO, (const char *)&t, sizeof(t));
}

// This PC's address on the network the internet is reached by (no packet is sent: this only chooses the interface)
static int LocalAddress(char *out, int size) {
    T_SOCK s = socket(AF_INET, SOCK_DGRAM, 0);
    if (s == BAD_SOCK) return 0;
    struct sockaddr_in a;
    memset(&a, 0, sizeof(a));
    a.sin_family = AF_INET; a.sin_port = htons(9);
    a.sin_addr.s_addr = inet_addr("192.0.2.1");
    int ok = connect(s, (struct sockaddr *)&a, sizeof(a)) == 0;
    struct sockaddr_in me;
    socklen_t n = sizeof(me);
    if (ok) ok = getsockname(s, (struct sockaddr *)&me, &n) == 0;
    if (ok) snprintf(out, size, "%s", inet_ntoa(me.sin_addr));
    CloseSock(s);
    return ok;
}

// Whose answer a search gets: each device's description, by its LOCATION: header
static int Search(const char *inside, char found[UPNP_LOCATIONS][200]) {
    static const char *const want[] = {"urn:schemas-upnp-org:device:InternetGatewayDevice:1", "urn:schemas-upnp-org:device:InternetGatewayDevice:2",
                                       "urn:schemas-upnp-org:service:WANIPConnection:1", "urn:schemas-upnp-org:service:WANIPConnection:2",
                                       "urn:schemas-upnp-org:service:WANPPPConnection:1"};
    T_SOCK s = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
    if (s == BAD_SOCK) return 0;
    struct sockaddr_in me;
    memset(&me, 0, sizeof(me));
    me.sin_family = AF_INET;
    me.sin_addr.s_addr = inet_addr(inside);
    bind(s, (struct sockaddr *)&me, sizeof(me));
    const char ttl = 2;
    setsockopt(s, IPPROTO_IP, IP_MULTICAST_TTL, &ttl, sizeof(ttl));
    SetTimeouts(s, 250);
    struct sockaddr_in to;
    memset(&to, 0, sizeof(to));
    to.sin_family = AF_INET; to.sin_port = htons(1900);
    to.sin_addr.s_addr = inet_addr("239.255.255.250");
    int nb = 0;
    for (int attempt = 0; attempt < 3 && !nb; attempt++) {
        for (size_t i = 0; i < sizeof(want) / sizeof(want[0]); i++) {
            char m[256];
            const int n = snprintf(m, sizeof(m), "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nMAN: \"ssdp:discover\"\r\nMX: 2\r\nST: %s\r\n\r\n", want[i]);
            sendto(s, m, n, 0, (struct sockaddr *)&to, sizeof(to));
        }
        const Uint64 end = SDL_GetTicks() + 2500;
        while (SDL_GetTicks() < end) {
            char r[2048];
            const int n = (int)recv(s, r, sizeof(r) - 1, 0);
            if (n <= 0) continue;
            r[n] = 0;
            for (char *line = r; line && *line;) {
                char *next = strstr(line, "\r\n");
                if (next) *next = 0;
                if (!SDL_strncasecmp(line, "location:", 9)) {
                    const char *v = line + 9;
                    while (*v == ' ') v++;
                    int seen = 0;
                    for (int k = 0; k < nb; k++) if (!strcmp(found[k], v)) seen = 1;
                    if (!seen && nb < UPNP_LOCATIONS) snprintf(found[nb++], 200, "%s", v);
                }
                line = next ? next + 2 : NULL;
            }
        }
    }
    CloseSock(s);
    return nb;
}

// "http://192.168.1.1:49152/x.xml": the host, the port and the path
static int ParseUrl(const char *url, char *host, int hostSize, int *port, char *path, int pathSize) {
    if (strncmp(url, "http://", 7)) return 0;
    const char *h = url + 7, *slash = strchr(h, '/');
    if (!slash) slash = h + strlen(h);
    const char *colon = strchr(h, ':');
    int n;
    *port = 80;
    if (colon && colon < slash) { n = (int)(colon - h); *port = atoi(colon + 1); }
    else n = (int)(slash - h);
    if (n <= 0 || n >= hostSize) return 0;
    memcpy(host, h, n);
    host[n] = 0;
    snprintf(path, pathSize, "%s", *slash ? slash : "/");
    return 1;
}

// One HTTP exchange: the status, and the body into `body`. 0: no answer
static int Http(const char *url, const char *request, char *body, int size, int timeoutMs) {
    char host[128], path[256], *reply = (char *)malloc(UPNP_REPLY_MAX);
    int port = 0, status = 0;
    if (!reply) return 0;
    if (ParseUrl(url, host, sizeof(host), &port, path, sizeof(path))) {
        T_SOCK s = socket(AF_INET, SOCK_STREAM, 0);
        if (s != BAD_SOCK) {
            struct sockaddr_in a;
            memset(&a, 0, sizeof(a));
            a.sin_family = AF_INET; a.sin_port = htons((unsigned short)port);
            a.sin_addr.s_addr = inet_addr(host);
            SetTimeouts(s, timeoutMs);
            if (connect(s, (struct sockaddr *)&a, sizeof(a)) == 0 && send(s, request, (int)strlen(request), 0) > 0) {
                int got = 0, n;
                while (got < UPNP_REPLY_MAX - 1 && (n = (int)recv(s, reply + got, UPNP_REPLY_MAX - 1 - got, 0)) > 0) got += n;
                reply[got] = 0;
                if (got > 12 && !strncmp(reply, "HTTP/", 5)) {
                    const char *sp = strchr(reply, ' ');
                    status = sp ? atoi(sp + 1) : 0;
                    const char *b = strstr(reply, "\r\n\r\n");
                    snprintf(body, size, "%s", b ? b + 4 : "");
                }
            }
            CloseSock(s);
        }
    }
    free(reply);
    return status;
}

// The text of the first <tag>..</tag> at or after p (namespaces and all: "errorCode>" is enough to find one)
static int TagText(const char *p, const char *tag, char *out, int size) {
    char open[64], close[64];
    snprintf(open, sizeof(open), "%s>", tag);
    snprintf(close, sizeof(close), "</");
    const char *a = strstr(p, open);
    if (!a) return 0;
    a += strlen(open);
    const char *b = strstr(a, close);
    if (!b) return 0;
    const int n = (int)(b - a) < size - 1 ? (int)(b - a) : size - 1;
    memcpy(out, a, n);
    out[n] = 0;
    return 1;
}

// The router's description: its WAN connections' (service type, control URL)
static int Connections(const char *location, char kinds[4][128], char controls[4][256]) {
    char host[128], path[256], request[512];
    int port = 0, nb = 0;
    if (!ParseUrl(location, host, sizeof(host), &port, path, sizeof(path))) return 0;
    snprintf(request, sizeof(request), "GET %s HTTP/1.1\r\nHost: %s:%d\r\nConnection: close\r\n\r\n", path, host, port);
    char *xml = (char *)malloc(UPNP_REPLY_MAX);
    if (!xml) return 0;
    if (Http(location, request, xml, UPNP_REPLY_MAX, 5000) == 200) {
        for (const char *p = xml; (p = strstr(p, "<service>")) != NULL && nb < 4; p += 9) {
            const char *end = strstr(p, "</service>");
            char kind[128], control[200];
            if (!end || !TagText(p, "serviceType", kind, sizeof(kind)) || !TagText(p, "controlURL", control, sizeof(control))) continue;
            if (!strstr(kind, "WANIPConnection") && !strstr(kind, "WANPPPConnection")) continue;
            snprintf(kinds[nb], 128, "%s", kind);
            if (!strncmp(control, "http://", 7)) snprintf(controls[nb], 256, "%s", control);
            else snprintf(controls[nb], 256, "http://%s:%d%s%s", host, port, control[0] == '/' ? "" : "/", control);
            nb++;
        }
    }
    free(xml);
    return nb;
}

// A SOAP action: 200, or the UPnP error (714, 718, 725 ..), or -1 no answer
static int Soap(const char *control, const char *kind, const char *action, const char *args, char *body, int size) {
    char host[128], path[256], envelope[1024], request[1800];
    int port = 0;
    if (!ParseUrl(control, host, sizeof(host), &port, path, sizeof(path))) return -1;
    const int n = snprintf(envelope, sizeof(envelope), "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" "
                           "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\"><s:Body><u:%s xmlns:u=\"%s\">%s</u:%s></s:Body></s:Envelope>", action, kind, args, action);
    snprintf(request, sizeof(request), "POST %s HTTP/1.1\r\nHost: %s:%d\r\nContent-Type: text/xml; charset=\"utf-8\"\r\nSOAPAction: \"%s#%s\"\r\nContent-Length: %d\r\nConnection: close\r\n\r\n%s",
             path, host, port, kind, action, n, envelope);
    const int status = Http(control, request, body, size, 6000);
    if (status == 200) return 200;
    if (!status) return -1;
    char code[16];
    return TagText(body, "errorCode", code, sizeof(code)) ? atoi(code) : status;
}

static int AddMapping(const char *control, const char *kind, unsigned short port, const char *inside, int lease, char *body, int size) {
    char args[640];
    snprintf(args, sizeof(args), "<NewRemoteHost></NewRemoteHost><NewExternalPort>%d</NewExternalPort><NewProtocol>UDP</NewProtocol><NewInternalPort>%d</NewInternalPort>"
             "<NewInternalClient>%s</NewInternalClient><NewEnabled>1</NewEnabled><NewPortMappingDescription>%s</NewPortMappingDescription><NewLeaseDuration>%d</NewLeaseDuration>",
             port, port, inside, UPNP_DESCRIPTION, lease);
    return Soap(control, kind, "AddPortMapping", args, body, size);
}

// An outside address no one on the internet can reach
static int Shared(const char *address) {
    int a = 0, b = 0;
    if (sscanf(address, "%d.%d", &a, &b) != 2) return 0;
    return a == 10 || (a == 172 && b >= 16 && b <= 31) || (a == 192 && b == 168) || (a == 100 && b >= 64 && b <= 127);
}

static void Answer(int ok, const char *text) {
    SDL_LockMutex(s_lock);
    s_ok = ok;
    snprintf(s_text, sizeof(s_text), "%s", text);
    s_done = 1;
    SDL_UnlockMutex(s_lock);
    if (ok) Log_Info("[racenet] upnp: %s", text);
    else Log_Warn("[racenet] upnp: %s", text);
}

static int SDLCALL Ask(void *unused) {
    (void)unused;
    char inside[64] = "", body[2048], text[200];
    if (!LocalAddress(inside, sizeof(inside))) { Answer(0, "This PC isn't on a network"); return 0; }
    SDL_LockMutex(s_lock);
    snprintf(s_inside, sizeof(s_inside), "%s", inside);
    SDL_UnlockMutex(s_lock);
    static char found[UPNP_LOCATIONS][200];
    const int nb = Search(inside, found);
    Log_Info("[racenet] upnp: %d device(s) answered", nb);
    if (!nb) { Answer(0, "No router answered. Turn UPnP on in the router, or forward the port by hand."); return 0; }
    char kinds[4][128], controls[4][256], outside[64] = "";
    int chosen = -1, wans = 0;
    for (int i = 0; i < nb && chosen < 0; i++) {
        const int n = Connections(found[i], kinds, controls);
        wans += n;
        for (int k = 0; k < n && chosen < 0; k++)
            if (Soap(controls[k], kinds[k], "GetExternalIPAddress", "", body, sizeof(body)) == 200 && TagText(body, "NewExternalIPAddress", outside, sizeof(outside)) && outside[0]) chosen = k;
        if (chosen < 0 && n) { chosen = 0; outside[0] = 0; }
    }
    if (chosen < 0) { Answer(0, wans ? "The router wouldn't say its address." : "A device answered, but it isn't a router that opens ports. Forward the port by hand."); return 0; }
    const char *control = controls[chosen], *kind = kinds[chosen];
    SDL_LockMutex(s_lock);
    snprintf(s_outside, sizeof(s_outside), "%s", outside);
    SDL_UnlockMutex(s_lock);
    int r = AddMapping(control, kind, s_port, inside, UPNP_LEASE, body, sizeof(body));
    if (r == 725) r = AddMapping(control, kind, s_port, inside, 0, body, sizeof(body));
    if (r != 200) {
        if (r == 718) snprintf(text, sizeof(text), "The router already has port %d open for another PC or program.", s_port);
        else if (r == -1) snprintf(text, sizeof(text), "The router stopped answering. Try again, or forward port %d by hand.", s_port);
        else snprintf(text, sizeof(text), "The router refused to open port %d (error %d). Forward it by hand.", s_port, r);
        Answer(0, text);
        return 0;
    }
    SDL_LockMutex(s_lock);
    snprintf(s_control, sizeof(s_control), "%s", control);
    snprintf(s_service, sizeof(s_service), "%s", kind);
    SDL_UnlockMutex(s_lock);
    // (a router that says yes and does nothing: asked back, now that there is something to ask about)
    char args[256], client[64] = "";
    snprintf(args, sizeof(args), "<NewRemoteHost></NewRemoteHost><NewExternalPort>%d</NewExternalPort><NewProtocol>UDP</NewProtocol>", s_port);
    const int back = Soap(control, kind, "GetSpecificPortMappingEntry", args, body, sizeof(body));
    const int there = back == 200 && TagText(body, "NewInternalClient", client, sizeof(client)) && !strcmp(client, inside);
    if (back == 200 && !there) { snprintf(text, sizeof(text), "The router sent port %d to another PC (%s). Forward it by hand.", s_port, client); Answer(0, text); return 0; }
    if (outside[0] && Shared(outside)) {
        Answer(0, "The port is open, but your provider's network is in front of your router: players on the internet can't reach it. Ask your provider for a public address.");
        return 0;
    }
    snprintf(text, sizeof(text), "The router opened UDP port %d for this game.", s_port);
    Answer(1, text);
    return 0;
}

void NetUpnp_Open(unsigned short port) {
    if (!s_lock) s_lock = SDL_CreateMutex();
    if (s_thread) { SDL_WaitThread(s_thread, NULL); s_thread = NULL; }
    s_done = s_ok = 0;
    s_text[0] = s_outside[0] = s_inside[0] = s_control[0] = s_service[0] = 0;
    s_port = port;
#ifdef _WIN32
    WSADATA data;
    WSAStartup(MAKEWORD(2, 2), &data);
#endif
    s_thread = SDL_CreateThread(Ask, "racenet-upnp", NULL);
}

void NetUpnp_Close(void) {
    if (s_thread) { SDL_WaitThread(s_thread, NULL); s_thread = NULL; }
    if (!s_control[0]) return;
    char args[256], body[2048];
    snprintf(args, sizeof(args), "<NewRemoteHost></NewRemoteHost><NewExternalPort>%d</NewExternalPort><NewProtocol>UDP</NewProtocol>", s_port);
    const int r = Soap(s_control, s_service, "DeletePortMapping", args, body, sizeof(body));
    Log_Info("[racenet] upnp: port %d taken off the router (%d)", s_port, r);
    s_control[0] = 0;
}

int NetUpnp_Result(int *ok, char *text, int textSize, char *outside, int outsideSize, char *inside, int insideSize) {
    if (!s_lock) return 0;
    SDL_LockMutex(s_lock);
    const int done = s_done;
    if (ok) *ok = s_ok;
    if (text) snprintf(text, textSize, "%s", s_text);
    if (outside) snprintf(outside, outsideSize, "%s", s_outside);
    if (inside) snprintf(inside, insideSize, "%s", s_inside);
    SDL_UnlockMutex(s_lock);
    return done;
}
