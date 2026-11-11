// Online races: the game's side of netkit (NET/netkit.h). One player hosts and every other joins them; see NETRACE.CPP.
#ifndef NETRACE_H
#define NETRACE_H

#include <stdint.h>

#define RACENET_PORT 21997                  // UDP, unless the host says otherwise. Nothing common uses it; tested through the user's router by UPnP, 2026-10-30
#define RACENET_MAX_OTHERS 15               // the other players a game can have (netkit: 16 members)
#define RACENET_NAME 16

// A car, as a ghost's sample is (RACEMOD.CPP T_RACE_LAPGHOST_PT): its place in island units, its angles as drawn, the body and animation shown
typedef struct {
    int32_t X, Y, Z;
    int16_t Alpha, Beta, Gamma, Body, Anim, Island;
    int32_t Lap, Progress;                  // laps done, and how far round the race it is (RaceMod's PlayerProgress): the order of the race
    int32_t Speed;                          // along the way it points, units a second
    int16_t Flags;                          // RACENET_CAR_
} T_RACENET_CAR;

#define RACENET_CAR_JET 1                   // on the super jet-pack: drawn as it, and nothing stops it

// What one game's car does to the others' (RACEMOD.CPP NetSay, NetEvents); reliable, in order
enum {
    RACENET_EV_MUSHROOM = 1,                // A: which mushroom it took
    RACENET_EV_ITEM,                        // A: the power-up used. X Y Z: where (a drop's landing, a ball's thrower); B: facing. Target: a red ball's car
    RACENET_EV_GONE,                        // A: a slick (0), penguin (1) or fake mushroom (2) its car took or set off, at X Y Z
    RACENET_EV_STRUCK,                      // its car hit, at X Y Z
    RACENET_EV_PIT                          // (Story Mode, two players) Zoe gives Twinsen his pit stop: Action in the pit box
};

typedef struct {
    int Type, A, B;
    int32_t X, Y, Z;
    uint32_t Target;
    char Name[RACENET_NAME + 1];            // whose
} T_RACENET_EVENT;

typedef struct {
    char Name[RACENET_NAME + 1];
    int Ready;                              // at the start line, for this race
    int Finish;                             // the race's time, GO to the line (ms); 0 still racing
    int Lap, Progress;
    int Pending;                            // yet to give the race's password
    int Vote;                               // the track voted for, -1 none (RaceNet_Stage)
    int Chosen;                             // has chosen a car, this round
    int InRace;                             // still out on the road, not back in the lobby
    uint32_t Id;
} T_RACENET_PLAYER;

// What this game is, before hosting or joining: `content` a key of its tracks and cars, `version` its name for people. Games with the same key race
// each other whatever their builds; a game with another is turned away, and told
void RaceNet_SetGame(uint32_t content, const char *version);
int RaceNet_Host(const char *name, const char *password, unsigned short port);   // hosts on `port`, and asks the router to open it: 0 and RaceNet_Error when it can't. password "": none
int RaceNet_Port(void);                     // the port hosted on
int RaceNet_HostLocal(const char *name, unsigned short port);   // ... two players at one PC: no router
int RaceNet_Local(void);                    // hosting that way
// Starts the second player's game: a copy of this one, with its own saves in `userDir`, in the rectangle given, on pad `pad`. It joins on `port` by itself
int RaceNet_SpawnPlayer2(unsigned short port, const char *userDir, int x, int y, int w, int h, int resW, int resH, int pad, int titled);
void RaceNet_SpawnStory(int on);           // the next player 2 started plays Zoe in Story Mode (LBA2_STORY_ZOE)
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
const char *RaceNet_Ipv6(void);             // a host's IPv6 address, to give to friends with IPv6 ("": none)

// Joining a race with a password: non-zero once the host has asked for it, once. Then send it (a wrong one ends the session)
int RaceNet_PasswordAsked(void);
void RaceNet_SendPassword(const char *password);
// The host: the last player its game turned away, and why ("" none)
const char *RaceNet_Turned(void);

int RaceNet_Others(void);                   // how many other players
int RaceNet_Other(int i, T_RACENET_PLAYER *out);

// The lobby's chat: a line sent to everyone, and the last RACENET_CHAT_LINES said, the oldest first
#define RACENET_CHAT 60
#define RACENET_CHAT_LINES 8
void RaceNet_Say(const char *text);
int RaceNet_ChatLine(int i, char *out, int size);

// The host starts a race: every game gets it from RaceNet_Announced, once. A player who joins with a race on is sent it: RaceNet_Late, non-zero
// `qualifying`: the one lap each that decides the grid, not the race (RaceNet_QualifyingOver)
void RaceNet_Announce(const char *track, int laps, int qualifying);
int RaceNet_Announced(char *track, int size, int *laps, int *qualifying);
int RaceNet_Late(void);
void RaceNet_RaceOver(void);                // the host is back from the race
int RaceNet_Slot(void);                     // this game's place on the grid: 0 the first

// A round, as the host leads it (RACEGAME.CPP LobbyPage): the lobby, a vote for a track, a minute for cars, a qualifying lap each or a grid dealt at random, the
// grid shown, the race, the lobby again. The host says where it has got to, and every game follows; a player who comes in is told. `track`: the track won, -1 none yet
enum { RACENET_STAGE_LOBBY, RACENET_STAGE_VOTE, RACENET_STAGE_CARS, RACENET_STAGE_QUALIFYING, RACENET_STAGE_GRID, RACENET_STAGE_RACE };
void RaceNet_SetStage(int stage, int ms, int track);
int RaceNet_Stage(int *msLeft, int *track);
void RaceNet_Vote(int track);
int RaceNet_Votes(int *counts, int size, int *players);   // the votes for each track; returns how many have voted, *players how many can
void RaceNet_CarChosen(void);
int RaceNet_CarsChosen(void);               // every player in the lobby has one
void RaceNet_InRace(int on);                // this game is off to race, or back in the lobby
int RaceNet_QualifyingOver(void);           // all over the line, or time is up: non-zero from then on
// The host: the grid from the qualifying times, those with none last, or (`byTimes` 0) at random, sent to all. Then each game's RaceNet_Slot is its place on it
void RaceNet_DealGrid(int byTimes);
int RaceNet_GridOf(int i, char *name, int size, int *ms);   // who is at place i, and their time; 0 past the last
uint32_t RaceNet_SelfId(void);

// In the race. Ready: this game's car is at the start. Go: non-zero once the host has said go (all of them ready, or the slow ones given up on), once
void RaceNet_Ready(void);
int RaceNet_Go(void);
void RaceNet_SendCar(const T_RACENET_CAR *car);
// Story Mode with two players (2026-11-11: player 2 is Zoe): player 1's game sends a save to load as it enters a scene (the same PC: its
// path), and the game's and the scene's variables as they change; player 2's takes each once
#define RACENET_STORY_PATH 260
#define RACENET_STORY_VARS 256
#define RACENET_STORY_CUBE_VARS 80
#define RACENET_CAR_STORY 2                 // a story sample: on foot or in the car, Lap the scene, Progress the hero's generic animation
void RaceNet_StoryLoad(const char *path, int cube);
void RaceNet_StoryVars(int cube, const int16_t *vars, const uint8_t *cubeVars);
int RaceNet_TakeStoryLoad(char *path, int size, int *cube);
int RaceNet_TakeStoryVars(int *cube, int16_t *vars, uint8_t *cubeVars);
int RaceNet_CarOf(int i, T_RACENET_CAR *out);   // where the other player's car is to be drawn now: 0 none yet, or it has gone quiet
void RaceNet_Finished(int ms);
void RaceNet_SendEvent(const T_RACENET_EVENT *e);
int RaceNet_NextEvent(T_RACENET_EVENT *out);

// Two players on one keyboard: which of the second player's keys are down (RACENET_KEY_), the first's game having the keyboard's focus or this one
#define RACENET_KEY_UP 1
#define RACENET_KEY_DOWN 2
#define RACENET_KEY_LEFT 4
#define RACENET_KEY_RIGHT 8
#define RACENET_KEY_ITEM 16
#define RACENET_KEY_HANDBRAKE 32
#define RACENET_KEY_VIEW 64
int RaceNet_SharedKeys(void);

#endif
