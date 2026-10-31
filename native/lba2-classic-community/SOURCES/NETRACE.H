// Online races: the game's side of netkit (NET/netkit.h). One player hosts and every other joins them; see NETRACE.CPP.
#ifndef NETRACE_H
#define NETRACE_H

#include <stdint.h>

#define RACENET_PORT 21997                  // UDP. Nothing common uses it; tested through the user's router by UPnP, 2026-10-30
#define RACENET_MAX_OTHERS 15               // the other players a game can have (netkit: 16 members)
#define RACENET_NAME 16

// A car, as a ghost's sample is (RACEMOD.CPP T_RACE_LAPGHOST_PT): its place in island units, its angles as drawn, the body and animation shown
typedef struct {
    int32_t X, Y, Z;
    int16_t Alpha, Beta, Gamma, Body, Anim, Island;
    int32_t Lap, Progress;                  // laps done, and how far round the lap (RaceMod's road point): the order of the race
} T_RACENET_CAR;

typedef struct {
    char Name[RACENET_NAME + 1];
    int Ready;                              // at the start line, for this race
    int Finish;                             // the race's time, GO to the line (ms); 0 still racing
    int Lap, Progress;
    int Wrong;                              // on another version of the game: the cars and tracks may not match
} T_RACENET_PLAYER;

int RaceNet_Host(const char *name);         // hosts on RACENET_PORT, and asks the router to open it: 0 and RaceNet_Error when it can't
int RaceNet_HostLocal(const char *name, unsigned short port);   // ... two players at one PC: no router
int RaceNet_Local(void);                    // hosting that way
// Starts the second player's game: a copy of this one, with its own saves in `userDir`, in the rectangle given, on pad `pad`. It joins on `port` by itself
int RaceNet_SpawnPlayer2(unsigned short port, const char *userDir, int x, int y, int w, int h, int resW, int resH, int pad);
int RaceNet_Join(const char *address, const char *name);   // "81.2.3.4" or "81.2.3.4:21997" or a name
void RaceNet_Stop(void);
int RaceNet_On(void);
int RaceNet_IsHost(void);
int RaceNet_Connected(void);
const char *RaceNet_Error(void);            // what ended the session, or kept it from starting ("" none)

// The router's answer, for the host to read: filled in a moment after RaceNet_Host. Non-zero once it has answered. *ok: 1 opened, 0 not, and players
// on the internet can't reach this game. `text`: what to tell the host. `outside`: the address to give to friends ("" not known); `inside`: this PC's on
// the home network, the one to use from the same house (most routers don't loop their own outside address back)
int RaceNet_Router(int *ok, char *text, int textSize, char *outside, int outsideSize, char *inside, int insideSize);

int RaceNet_Others(void);                   // how many other players
int RaceNet_Other(int i, T_RACENET_PLAYER *out);

// The host starts a race: every game gets it from RaceNet_Announced, once
void RaceNet_Announce(const char *track, int laps);
int RaceNet_Announced(char *track, int size, int *laps);

// In the race. Ready: this game's car is at the start. Go: non-zero once the host has said go (all of them ready, or the slow ones given up on), once
void RaceNet_Ready(void);
int RaceNet_Go(void);
void RaceNet_SendCar(const T_RACENET_CAR *car);
int RaceNet_CarOf(int i, T_RACENET_CAR *out);   // where the other player's car is to be drawn now: 0 none yet, or it has gone quiet
void RaceNet_Finished(int ms);

#endif
