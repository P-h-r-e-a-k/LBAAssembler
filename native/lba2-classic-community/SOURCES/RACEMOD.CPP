/* RACEMOD: the engine's race-track options, for the race-track mods the level editor builds (LBA Assembler, Tools > LBA2: Desert island
 * race track).
 *
 * Off unless the environment names a car setup file (LBA2_RACETRACK_FILE). The editor sets it only when the game folder it plays has a race
 * track built, so with any other game the engine is unchanged. With it on:
 *   - the buggy's body follows the decor floor that carries it (a bridge deck) instead of the ground under it (BUGGY.CPP BuggyFloorY);
 *   - the car handles as the file says: a gearbox (X shifts up, Z down, or automatic), each gear with its own top speed, the acceleration
 *     scaled to the gear (a low gear pulls harder), braking, coasting, the speed backwards and the steering;
 *   - laps are counted at a start line, and only once the car has crossed every checkpoint round the lap in order (reversing back over
 *     the line and across it again is not a lap);
 *   - opponents (up to four): each a copy of a car the build put in every scene, driven along its own racing line round the lap, placed
 *     in whichever scene the player is in. Each drives the player's own car as the setup makes it -- its top gear, its steering (the
 *     buggy turns at a fixed rate, so a bend of radius R is taken at up to that rate times R), its pull through the gears and its
 *     brakes -- as well as it can on its line, times its skill (the pace); a character makes one faster on the straights or in the
 *     bends; and one that has fallen behind the player pushes harder, up to its catch-up;
 *   - a grid and a start: the opponents wait in the pit lane while the player drives his qualifying lap (on the grid they stand across
 *     the start line he has to cross), then the cars line up on grid spots (behind the start line, staggered), in the order the
 *     qualifying decides (the player's timed lap against each opponent's lap at its skill, a few seconds either way at random), then a
 *     count-down, 3, 2, 1, GO, the player's car held until GO;
 *   - a small display of the gear, the speed, the lap times, the checkpoints, the qualifying and the grid, and the race position.
 *
 * The file is "key=value" lines ('#' starts a comment); anything missing keeps the original car's value. Speeds are world units per second
 * (512 to a cell), rates the speed gained or lost per millisecond (decimals allowed), the steering in 4096ths of a turn per second:
 *     gears=5
 *     gear1=1500  gear2=2300  ...          top speed of each gear
 *     accel=4  brake=12  coast=7  reverse=2000  steer=1024  automatic=0  hud=1
 *     startline=<cube x> <cube z> <x0> <z0> <x1> <z1> <dir x> <dir z>     cube-local ends, and the way a lap crosses it
 *     checkpoint=<the same>                                                  one per line, in the order a lap crosses them
 *     opponent_path=<file>       lines "x z y speed [radius]": world units from the island's corner (32768 to a cube), from the start
 *                                line round; with the line's bend radius at each point the speeds are planned from the player's car,
 *                                without it the file's speeds are driven
 *     opponent_grid=<points>     how many of the path's last points the opponent starts before the start line
 *     opponent_pace=<percent>    its skill: of the speeds planned (100: the player's car driven perfectly on the line)
 *     opponent_top=<percent>     its character: the top speed and pull, and the cornering, as a share of the player's car's
 *     opponent_grip=<percent>
 *     opponent_catchup=<percent> how much harder it pushes when it has fallen behind the player (at most, 60 cells behind)
 *     opponent_actor=<scene> <actor>   the opponent's car in that scene (one line per scene)
 *     opponent_name=<name>       for the grid's list
 *     opponent2_path=...  opponent2_grid=...  opponent2_pace=...  opponent2_actor=...   the second opponent, and so on up to opponent4
 *     grid=<cube x> <cube z> <x> <y> <z> <turn>   a grid spot, cube-local; one line per spot, pole first (with none, the old start:
 *                                the opponents set off when the player first drives)
 *     pit=<the same>             a spot in the pit lane an opponent waits on while the player qualifies; one line per spot
 *     weather=fine               Citadel Island's weather once the storm is over (RaceMod_FineWeather), whatever the chapter
 *     holo_arrow=<n>             the holomap arrow the mod's story points at the start line: cleared once Twinsen drives
 *     qualifying=1               a qualifying lap first (0: the grid is formed as soon as the player is in his car, he on pole)
 *     qualifying_seed=<n>        the opponents' qualifying times' randomness (default: the clock)
 *     hide_actor=<scene> <actor> a car in a scene that isn't racing (an opponent the player left out): kept out of sight
 *     raised=<file>              a raised road: lines "x z y half", its middle point by point in lap order and half its width, world
 *                                units from the island's corner. A floor of its own over the ground, at any grade (see "the raised road")
 *     statue=1                   Celebration Island drawn from its file with the statue (CELEBRA2), whatever game variable 79 says;
 *                                statue=0 from its file before the statue rises (CELEBRAT, the lava lake)
 *     rescue=0                   a car that goes into the lava or the sea burns or drowns Twinsen as in the game (default 1: it is put
 *                                back on the road, RaceMod_Rescue)
 *     jump_speed=<lo> <hi>       a jump's flight flown at the car's speed at the take-off (RaceMod_JumpClock): its clock that many times
 *                                as fast as the flight's own speed, kept between lo and hi (default 0.6 4; 0: the flight in its own time)
 *     jumpcam=<anim> <cube x> <cube z> <x> <y> <z>   while the hero flies the jump whose flight is generic animation <anim>, and a moment
 *                                after it lands, the camera that follows the car stands there (cube-local) and looks at the car (JumpCamera)
 *   A startline or checkpoint line may end in a ninth number, the road's height there: the line then only counts for a car within
 *   RACE_LINE_REACH of that height (a lap that passes over itself crosses a line's place at other heights too).
 */
#include "C_EXTERN.H"
#include "COMMON.H"
#include "INPUT.H"
#include "RACEMOD.H"
extern void ClrHoloPos(S32 num);   // HOLOGLOB.CPP (HOLO.H itself clashes with C_EXTERN.H)

#include <SVGA/BOX.H>
#include <SVGA/CLIP.H>
#include <SYSTEM/LOG.H>
#include <SVGA/FONT.H>
#include <SVGA/SCREEN.H>
#include <SYSTEM/KEYBOARD.H>
#include <SYSTEM/KEYBOARD_KEYS.H>
#include <cmath>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <ctime>

static int s_checked = 0;
static int s_active = 0;
static T_RACE_CAR s_car;

static S32 s_gear = 1;
static int s_lastX = 0, s_lastZ = 0;       // the shift keys' state last frame

static S32 s_laps = 0;                     // laps started (0: the start line not crossed yet)
static S32 s_lapStart = 0, s_lastLap = 0, s_bestLap = 0;

// checkpoints, and the next one the car has to cross
#define RACE_MAX_CHECKPOINTS 32
typedef struct {
    S32 CubeX, CubeZ, X0, Z0, X1, Z1, DirX, DirZ;
    S32 Y, HasY;                           // the road's height at the line, when the file gives it
} T_RACE_LINE;
#define RACE_LINE_REACH 1200
static S32 s_lineY = 0, s_lineHasY = 0;    // the start line's
static T_RACE_LINE s_checkpoints[RACE_MAX_CHECKPOINTS];
static int s_nbCheckpoints = 0;
static int s_next = 0;

// a short message on the display ("Missed checkpoint 3") and until when
static char s_message[64];
static S32 s_messageUntil = 0;

// the opponents: each one's line, where it is along it (world units from the start line), laps begun, the scenes' copies of its car
#define RACE_MAX_OPPONENTS 4
#define RACE_MAX_OPPONENT_SCENES 64
typedef struct {
    int Count;                             // points on its line (0: no line, no opponent)
    S32 *X, *Z, *Y, *V;
    double *Cum;                           // distance from the first point to each point
    double Length;                         // the lap along the line
    double S;
    int Laps, Grid, Pace;
    S32 *R;                                // the line's bend radius at each point (NULL: drive the file's speeds)
    int Top, Grip, Catchup;                // its character (percent) and how much harder it pushes when behind
    S32 LapStart;
    int Scene[RACE_MAX_OPPONENT_SCENES], Actor[RACE_MAX_OPPONENT_SCENES];
    int Scenes;
    char Name[32];
    int AnimIdle, AnimDrive, HasAnims;   // opponentN_anim: the generic animations it stands and drives with (else 0 and 1, the racer car's)
    double PlannedLap;                     // seconds, at its skill
    S32 QualTime;                          // its qualifying time, ms
    // off its line on the grid: the way across from the line to its grid spot, which fades out over its first RACE_MERGE units of
    // driving (it moves onto its line), and the way it faces there
    double MergeX, MergeY, MergeZ, MergeLeft;
    S32 SpotBeta;
    double Speed;                          // how fast it is going (it starts from a standstill and pulls away as the car would)
} T_RACE_OPPONENT;
static T_RACE_OPPONENT s_opp[RACE_MAX_OPPONENTS];
static int s_oppStarted = 0;               // (no grid: the race has started when the player first drove off)
#define RACE_MERGE (12 * 512)

// the grid and the start: a qualifying lap (when the file asks for one), then the grid and its count-down, then the race
#define RACE_MAX_SPOTS 6
typedef struct {
    S32 CubeX, CubeZ, X, Y, Z, Beta;
} T_RACE_SPOT;
static T_RACE_SPOT s_spots[RACE_MAX_SPOTS];
static int s_nbSpots = 0;
static T_RACE_SPOT s_pits[RACE_MAX_SPOTS];
static int s_nbPits = 0;
enum { PHASE_RACE, PHASE_QUALIFYING, PHASE_WAIT, PHASE_GRID };
static int s_phase = PHASE_RACE;
static int s_qualifying = 1;
static int s_fineWeather = 0;
static int s_holoArrow = -1;
static unsigned s_seed = 0;
static S32 s_qualifyTime = 0;              // the player's, ms
static int s_wantGrid = 0;                 // form the grid (asked for by the lap, done before the next drawing)
static S32 s_gridAt = 0, s_goAt = 0;       // when the grid was formed, and when the count-down says GO
static int s_order[RACE_MAX_OPPONENTS + 1]; // the grid, pole first: -1 the player, n the opponent n
static S32 s_orderTime[RACE_MAX_OPPONENTS + 1];
static int s_nbOrder = 0;
static int s_forgetLap = 0;                // the player's car was moved: the lap check forgets its last position
#define RACE_MAX_HIDDEN 64
static int s_hideScene[RACE_MAX_HIDDEN], s_hideActor[RACE_MAX_HIDDEN];
static int s_nbHidden = 0;
#define RACE_RESULTS_MS 4000               // how long the qualifying's grid is shown before the count-down
#define RACE_QUALIFY_SPREAD 3000           // an opponent's qualifying time: its lap at its skill, this many ms either way at random
#define RACE_QUALIFY_SHARE 0.1             // ... or this share of that lap, if less (a short lap: the lava lake's is 10 s)
static S32 s_oppLast = 0;
static int s_playerIndex = -1;             // the first opponent's line point nearest the player, for the race position
// The whole scene is redrawn while the opponent moves: with the camera standing still, only what moves is redrawn over a copy of the
// scene, and the car, drawn into that copy while it waited on the grid, stayed there as a picture after it set off. Asked for while
// drawing (RaceMod_Frame) and set from the buggy's move before the next frame's drawing (RaceMod_Lap): the main loop resets FirstTime
// right after drawing, so setting it while drawing had no effect.
static int s_wantFlip = 0;

static void Defaults(T_RACE_CAR *c) {
    memset(c, 0, sizeof(*c));
    memset(s_opp, 0, sizeof(s_opp));
    for (int n = 0; n < RACE_MAX_OPPONENTS; n++) { s_opp[n].Grid = 4; s_opp[n].Pace = 100; s_opp[n].Top = 100; s_opp[n].Grip = 100; }
    c->Gears = 1;
    c->GearTop[0] = RACE_REF_TOP;
    c->Accel = 4000;
    c->Brake = 12000;
    c->Coast = 7000;
    c->Reverse = 2000;
    c->Steer = 1024;
    c->Hud = 1;
}

static S32 Milli(const char *v) { return (S32)(atof(v) * 1000.0 + 0.5); }

static int ReadLine(const char *v, T_RACE_LINE *l) {
    const int n = sscanf(v, "%d %d %d %d %d %d %d %d %d", &l->CubeX, &l->CubeZ, &l->X0, &l->Z0, &l->X1, &l->Z1, &l->DirX, &l->DirZ, &l->Y);
    l->HasY = n == 9;
    if (!l->HasY) l->Y = 0;
    return n >= 8;
}

// ---- the raised road --------------------------------------------------------------------------------------------------------------
// A race track's raised road (raised=<file>): a road that stands in the air on piers, passes over itself and climbs at any grade --
// Celebration Island's lap round the statue -- which the ground, one height map, cannot be, and which decor boxes, level and
// axis-aligned, cannot carry on a slope (a car stepping down from one box to the next is dropped by the engine's fall, which stops it
// dead each time). So it is a floor of its own: while an object is moved (RaceMod_FloorFor), the ground's height under it is the
// road's wherever the road passes within its width and no more than RAISED_STEP above the object -- the lap's other levels lie
// thousands over or under it -- and the ground's blocking triangles far below don't count there. The hero is kept RAISED_INSET inside
// its edges (RaceMod_Rail), a rail that needs no collision boxes. The build's decor pieces are only what it looks like.
// A point with no width (Half 0) is in a gap of the road -- a jump's, between its take-off lip and its landing lip (the lava lake's
// causeways, 2026-10-01): a segment that touches one has no floor, no rail and hides nothing.
typedef struct {
    S32 X, Z, Y, Half;
    S32 Bank;                              // its banking: the height gained across it, per RAISED_BANK_UNITS to the left of the way it runs
} T_RACE_RAISED;
#define RAISED_BANK_UNITS 10000.0
static T_RACE_RAISED *s_raised = NULL;
static int s_nbRaised = 0;
#define RAISED_BUCKET 2048                 // the road's points are kept in squares of four cells
#define RAISED_STEP 700                    // how far above an object the road may be and still be its floor
#define RAISED_LEVEL 1200                  // two parts of the road within this of each other's height are one level (a bend's segments)
#define RAISED_PAD 96                      // the floor reaches this far past the road's edge
#define RAISED_INSET 640                   // how far inside the edge the hero's middle is kept: a car's half width
#define RAISED_NOREF 0x7FFFFFFF
static int s_rbX0 = 0, s_rbZ0 = 0, s_rbW = 0, s_rbH = 0;
static int *s_rbStart = NULL, *s_rbItem = NULL;
static S32 s_floorRef = RAISED_NOREF;      // the height of what the ground is being asked for (nothing: the ground alone)
static int s_onlyNear = -1;                // RaisedAt looks only at the road's points within RAISED_SIGHT of this one (-1: all of it)
#define RAISED_SIGHT 70                    // 35 cells either way: the stretch of road round the hero that can hide him from the camera
#define RAISED_OWN 24                      // twelve cells either way: the stretch of road a place on it belongs to (RaisedBetween)
#define RAISED_STACK 12                    // six cells either way: a level over the road all that way runs with it (a helix)
#define RAIL_CAMERA_CLEAR 500               // the rail camera's line to the car passes this far over the road between them
#define RAIL_CAMERA_AIM 300                 // ... and it looks at a point this far over the road ahead of the car
#define RAIL_CAMERA_LOOK 0.6                // ... that far of the way from the car to the road railcam's lead ahead
#define RAIL_CAMERA_SIDE 50                 // the rail camera's eye comes nearer round a bend until it looks this near the car's way (degrees)
#define RAIL_CAMERA_NEAREST 2560            // ... but no nearer than this
#define RAISED_UNDER 650                   // how far under such a level's surface the following camera's eye stays (its slab is 200)
static int s_rescueOn = 1;                 // rescue=: a car in the lava or the sea is put back on the road (RaceMod_Rescue)
// island_file=<island> <name>: while racing, that island is drawn from the file <name> instead of its own -- a track on an island file the
// game never loads (MOON.ILE, the Emerald Moon's older copy, raced as island 3's)
static int s_fileIsland = -1;
static char s_fileName[32] = "";
// Vertical loops (loop=, RaceMod_Loop): a ring standing on the road, its foot at the lap's point there (X, Y, Z, cube-local) and its plane
// along the lap's way (DirX, DirZ, a unit vector). R: from the ring's middle to its road surface. Shift: how far across the car comes
// down from where it went in (+ to the right of the way, as the builder's lanes are: in on the left, out on the right, past the ring's
// own foot). Gap: the part of its top that is missing (radians, 0 none), which the car is carried over upside down.
#define RACE_MAX_LOOPS 4
typedef struct {
    int CubeX, CubeZ;
    double X, Y, Z, DirX, DirZ, R, Shift, Gap;
} T_RACE_LOOP;
static T_RACE_LOOP s_loops[RACE_MAX_LOOPS];
static int s_nbLoops = 0;
// Jump cameras (jumpcam=, JumpCamera): a drop -- a flight off a raised road's end and down a hillside onto the ground far below (the lava
// lake's, 2026-10-01) -- took the camera behind the car down into the hill, and the landing was never seen. While the hero flies such a
// jump, and RACE_JUMPCAM_HOLD after he lands, the camera stands at a place of its own beside the flight (the builder's, with the whole
// flight and the landing in sight) and looks at the car.
#define RACE_MAX_JUMPCAMS 4
#define RACE_JUMPCAM_HOLD 1500
typedef struct { int Anim, CubeX, CubeZ, X, Y, Z; } T_RACE_JUMPCAM;
static T_RACE_JUMPCAM s_jumpCams[RACE_MAX_JUMPCAMS];
static int s_nbJumpCams = 0;
static S32 s_flying = -1;                  // the flight the hero is flying (its generic animation), -1 none
static S32 s_landed = -1;                  // the flight he last landed from ...
static U32 s_landedAt = 0;                 // ... and when (TimerRefHR)
static int s_statue = -1;                // statue=: 1 with the statue, 0 without it, -1 (not given) as the game says
static int s_raisedLoop = 0;               // the road is a whole lap: its last point is its first
static double s_gravity = 0;               // gravity=<k>: a car on the raised road is pulled down its slopes (RaceMod_Gravity), k times as hard as RACE_GRAVITY
// railcam=<back> <up> <lead>: the camera that follows the car rides the raised road behind it (RailCamera)
static double s_railBack = 0, s_railUp = 0, s_railLead = 0;
static int s_railTouch = 0;                // the rail held the hero back since RaceMod_RailTouched was last asked

extern S32 (*AltitudeHook)(S32 x, S32 z, S32 y);         // 3DEXT/MAPTOOLS.CPP
extern S32 (*TerrainColHook)(S32 x, S32 z, S32 col);
extern S32 (*SightFloorHook)(S32 x, S32 z, S32 y);
extern S32 FollowCamera;                                 // FOLLOWCAM.CPP
extern S32 (*FollowCamArmHook)(S32 base);
extern S32 (*FollowCamEyeHook)(S32 *eye, S32 *target);
extern S32 CalculAltitudeTerrain(S32 x, S32 z, S32 cj);

// Whether a place `tt` along segment i (its projection, unclamped) is past an open end of the road -- the road's own last point, or a
// jump's take-off lip, where a gap (Half 0) follows: there is no floor out there (a segment's ends are otherwise round, so the deck
// reached a half-width past the lava lake's drop lip and held the car up over the gap, 2026-10-01).
static int RaisedOpenEnd(int i, double tt) {
    if (tt > 1) return i + 2 >= s_nbRaised ? !s_raisedLoop : s_raised[i + 2].Half <= 0;
    if (tt < 0) return i == 0 ? !s_raisedLoop : s_raised[i - 1].Half <= 0;
    return 0;
}

// The road at an island position (world units) for something at height `ref`, looked for `pad` past its edges: its height there, the
// segment (from point *seg to the next), how far along it (*t, 0..1) and how far to the side of its middle (*lat, to the left of the way
// the lap runs positive). Of the parts of the road in reach -- each no more than RAISED_STEP above `ref` -- the highest level, and of
// that level's segments the nearest. 0 when the road isn't there. A banked road's height is its surface's at that place, not its
// middle's.
static int RaisedAt(double wx, double wz, S32 ref, double pad, S32 *y, int *seg, double *lat, double *t) {
    if (!s_nbRaised) return 0;
    const int bx = (int)floor(wx / RAISED_BUCKET) - s_rbX0, bz = (int)floor(wz / RAISED_BUCKET) - s_rbZ0;
    const int reach = 1 + (int)(pad / RAISED_BUCKET);
    double top = -1e18;
    for (int pass = 0; pass < 2; pass++) {
        double bestD = 1e30;
        int found = 0;
        for (int dz = -reach; dz <= reach; dz++)
            for (int dx = -reach; dx <= reach; dx++) {
                const int x = bx + dx, z = bz + dz;
                if (x < 0 || z < 0 || x >= s_rbW || z >= s_rbH) continue;
                const int b = z * s_rbW + x;
                for (int k = s_rbStart[b]; k < s_rbStart[b + 1]; k++) {
                    const int i = s_rbItem[k];
                    if (i >= s_nbRaised - 1) continue;
                    if (s_onlyNear >= 0) {
                        int apart = abs(i - s_onlyNear);
                        if (s_raisedLoop && apart > (s_nbRaised - 1) / 2) apart = s_nbRaised - 1 - apart;
                        if (apart > RAISED_SIGHT) continue;
                    }
                    const T_RACE_RAISED *a = &s_raised[i], *c = a + 1;
                    if (a->Half <= 0 || c->Half <= 0) continue;
                    const double sx = c->X - a->X, sz = c->Z - a->Z, len2 = sx * sx + sz * sz;
                    if (len2 <= 0) continue;
                    double tt = ((wx - a->X) * sx + (wz - a->Z) * sz) / len2;
                    if (RaisedOpenEnd(i, tt)) continue;
                    if (tt < 0) tt = 0; else if (tt > 1) tt = 1;
                    const double px = a->X + sx * tt, pz = a->Z + sz * tt;
                    const double d2 = (wx - px) * (wx - px) + (wz - pz) * (wz - pz);
                    const double half = a->Half + (c->Half - a->Half) * tt + pad;
                    if (d2 > half * half) continue;
                    const double side = (sx * (wz - pz) - sz * (wx - px)) / sqrt(len2);
                    const double yy = a->Y + (c->Y - a->Y) * tt + side * (a->Bank + (c->Bank - a->Bank) * tt) / RAISED_BANK_UNITS;
                    if (yy > ref + RAISED_STEP) continue;
                    if (!pass) { if (yy > top) top = yy; continue; }
                    if (yy < top - RAISED_LEVEL || d2 >= bestD) continue;
                    bestD = d2;
                    found = 1;
                    *y = (S32)floor(yy + 0.5);
                    *seg = i;
                    *t = tt;
                    *lat = side;
                }
            }
        if (pass) return found;
        if (top < -1e17) return 0;
    }
    return 0;
}

static S32 RaisedAltitude(S32 x, S32 z, S32 y) {
    if (s_floorRef == RAISED_NOREF || CubeMode != CUBE_EXTERIEUR) return y;
    S32 f;
    int seg;
    double lat, t;
    if (!RaisedAt(x + CurrentCubeX * 32768.0, z + CurrentCubeY * 32768.0, s_floorRef, RAISED_PAD, &f, &seg, &lat, &t)) return y;
    return f > y ? f : y;
}

static S32 RaisedCol(S32 x, S32 z, S32 col) {
    if (!col || s_floorRef == RAISED_NOREF || CubeMode != CUBE_EXTERIEUR) return col;
    S32 f;
    int seg;
    double lat, t;
    if (!RaisedAt(x + CurrentCubeX * 32768.0, z + CurrentCubeY * 32768.0, s_floorRef, RAISED_PAD, &f, &seg, &lat, &t)) return col;
    return f > CalculAltitudeTerrain(x, z, 0) ? 0 : col;
}

// For the cameras: what would hide the hero from an eye at (x, z) -- the stretch of raised road he is on, where it passes there (the
// road's other levels, over and under him, are not between him and an eye that follows him), or the ground (y) where that is higher.
static S32 RaisedSight(S32 x, S32 z, S32 y) {
    if (CubeMode != CUBE_EXTERIEUR) return y;
    const T_OBJET *hero = &ListObjet[NUM_PERSO];
    const double cx = CurrentCubeX * 32768.0, cz = CurrentCubeY * 32768.0;
    S32 f;
    int seg;
    double lat, t;
    if (!RaisedAt(hero->Obj.X + cx, hero->Obj.Z + cz, hero->Obj.Y + 64, RAISED_PAD, &f, &seg, &lat, &t) || abs(f - hero->Obj.Y) > 300) return y;
    s_onlyNear = seg;
    const int found = RaisedAt(x + cx, z + cz, 0x3FFFFFFF, RAISED_PAD, &f, &seg, &lat, &t);
    s_onlyNear = -1;
    return found && f > y ? f : y;
}

// The road's slope at a segment for something facing `beta`: the height it gains per unit that way (over a cell or two of road).
static double RaisedSlope(int seg, S32 beta) {
    int a = seg - 2, b = seg + 3;
    if (s_raisedLoop) {
        const int n = s_nbRaised - 1;
        a = ((a % n) + n) % n; b = b % n;
    } else {
        if (a < 0) a = 0;
        if (b > s_nbRaised - 1) b = s_nbRaised - 1;
    }
    const double sx = s_raised[b].X - s_raised[a].X, sz = s_raised[b].Z - s_raised[a].Z, len = sqrt(sx * sx + sz * sz);
    if (len < 1) return 0;
    const double grade = (s_raised[b].Y - s_raised[a].Y) / len;
    const double angle = (beta & 4095) * 2 * 3.14159265358979 / 4096.0;
    return grade * (sin(angle) * sx + cos(angle) * sz) / len;
}

// gravity=<k>: a rollercoaster of a lap. A car on the raised road is pulled down its slopes and held back up them (BUGGY.CPP
// RaceSpeedOnSlope): it gathers speed down a drop, past what its gears give, and carries it over the next hill. The key's value (k
// times RACE_GRAVITY) where the hero is on the raised road, with the road's slope the way he faces (the height gained per unit
// forwards) and how much it is banked there (the height across it per unit, either way); 0 anywhere else and without the key.
double RaceMod_Gravity(double *slope, double *bank) {
    *slope = 0;
    *bank = 0;
    if (s_gravity <= 0 || !s_nbRaised || CubeMode != CUBE_EXTERIEUR) return 0;
    const T_OBJET *hero = &ListObjet[NUM_PERSO];
    S32 f;
    int seg;
    double lat, t;
    if (!RaisedAt(hero->Obj.X + CurrentCubeX * 32768.0, hero->Obj.Z + CurrentCubeY * 32768.0, hero->Obj.Y + 64, RAISED_PAD, &f, &seg, &lat, &t)
            || abs(f - hero->Obj.Y) > 300) return 0;
    *slope = RaisedSlope(seg, hero->Obj.Beta);
    *bank = fabs(s_raised[seg].Bank + (s_raised[seg + 1].Bank - s_raised[seg].Bank) * t) / RAISED_BANK_UNITS;
    return s_gravity;
}

// The share of a gear's top speed the engine can hold up a slope (RACE_CLIMB of it lost per unit of slope, a half at least).
double RaceMod_ClimbShare(double gravity, double slope) {
    if (slope <= 0 || gravity <= 0) return 1;
    const double share = 1 - gravity * RACE_CLIMB * slope;
    return share < 0.5 ? 0.5 : share;
}

S32 RaceMod_RailTouched(void) {
    const int touched = s_railTouch;
    s_railTouch = 0;
    return touched;
}

// The lowest part of the road over a place between two heights: 1 and its height there, or 0. Not the stretch of road round point
// `own` (RAISED_OWN points either way): on a steep slope the road a few cells further up it is "over" a place on it too.
static int RaisedBetween(double wx, double wz, double lo, double hi, int own, double *at) {
    if (!s_nbRaised) return 0;
    const int bx = (int)floor(wx / RAISED_BUCKET) - s_rbX0, bz = (int)floor(wz / RAISED_BUCKET) - s_rbZ0;
    int found = 0;
    for (int dz = -1; dz <= 1; dz++)
        for (int dx = -1; dx <= 1; dx++) {
            const int x = bx + dx, z = bz + dz;
            if (x < 0 || z < 0 || x >= s_rbW || z >= s_rbH) continue;
            const int b = z * s_rbW + x;
            for (int k = s_rbStart[b]; k < s_rbStart[b + 1]; k++) {
                const int i = s_rbItem[k];
                if (i >= s_nbRaised - 1) continue;
                if (own >= 0) {
                    int apart = abs(i - own);
                    if (s_raisedLoop && apart > (s_nbRaised - 1) / 2) apart = s_nbRaised - 1 - apart;
                    if (apart <= RAISED_OWN) continue;
                }
                const T_RACE_RAISED *a = &s_raised[i], *c = a + 1;
                if (a->Half <= 0 || c->Half <= 0) continue;
                const double sx = c->X - a->X, sz = c->Z - a->Z, len2 = sx * sx + sz * sz;
                if (len2 <= 0) continue;
                double tt = ((wx - a->X) * sx + (wz - a->Z) * sz) / len2;
                if (RaisedOpenEnd(i, tt)) continue;
                if (tt < 0) tt = 0; else if (tt > 1) tt = 1;
                const double px = a->X + sx * tt, pz = a->Z + sz * tt;
                const double half = a->Half + (c->Half - a->Half) * tt;
                if ((wx - px) * (wx - px) + (wz - pz) * (wz - pz) > half * half) continue;
                const double yy = a->Y + (c->Y - a->Y) * tt;
                if (yy <= lo || yy >= hi || (found && yy >= *at)) continue;
                found = 1;
                *at = yy;
            }
        }
    return found;
}

// The road's place `distance` along it from a point of it (segment `seg`, `t` along it), forwards or (negative) backwards: its middle
// there, at the road's own height. Round a loop's seam; stopped at an open road's ends.
static int RaisedAlong(int seg, double t, double distance, double *x, double *y, double *z) {
    const int n = s_nbRaised - 1;                       // its segments
    int i = seg;
    double at = t;
    for (int guard = 0; guard < 2 * n + 8; guard++) {
        const T_RACE_RAISED *a = &s_raised[i], *c = a + 1;
        const double len = sqrt((double)(c->X - a->X) * (c->X - a->X) + (double)(c->Z - a->Z) * (c->Z - a->Z));
        const int forward = distance >= 0;
        const double room = forward ? (1 - at) * len : at * len;      // what is left of this segment that way
        if (len > 1e-6 && fabs(distance) <= room) { at += distance / len; break; }
        distance += forward ? -room : room;
        if (forward) {
            if (i + 1 >= n) { if (!s_raisedLoop) { at = 1; break; } i = 0; } else i++;
            at = 0;
        } else {
            if (i == 0) { if (!s_raisedLoop) { at = 0; break; } i = n - 1; } else i--;
            at = 1;
        }
    }
    const T_RACE_RAISED *a = &s_raised[i], *c = a + 1;
    *x = a->X + (c->X - a->X) * at;
    *y = a->Y + (c->Y - a->Y) * at;
    *z = a->Z + (c->Z - a->Z) * at;
    return i;
}

// railcam=: the camera that follows the car on a rollercoaster of a road. An arm behind the car does badly there -- the road's other
// levels are over it, a drop's crest between it and the car, a bend swings it out over nothing -- so the eye rides the road itself:
// over its middle s_railBack behind the car (behind the way the car faces), s_railUp over it, looking at the road s_railLead ahead of
// the car. Kept under a level of the road that passes over it, and high enough to see the car over a crest between them.
static S32 RailCamera(S32 *eye, S32 *target) {
    if (s_railBack <= 0 || !s_nbRaised || CubeMode != CUBE_EXTERIEUR || Comportement != C_BUGGY) return FALSE;
    const T_OBJET *hero = &ListObjet[NUM_PERSO];
    const double cx = CurrentCubeX * 32768.0, cz = CurrentCubeY * 32768.0;
    S32 f;
    int seg;
    double lat, t;
    if (!RaisedAt(hero->Obj.X + cx, hero->Obj.Z + cz, hero->Obj.Y + 64, RAISED_PAD, &f, &seg, &lat, &t) || abs(f - hero->Obj.Y) > 300) return FALSE;
    // which way along the road the car faces
    const T_RACE_RAISED *a = &s_raised[seg], *c = a + 1;
    const double angle = (hero->Obj.Beta & 4095) * 2 * 3.14159265358979 / 4096.0;
    const double way = sin(angle) * (c->X - a->X) + cos(angle) * (c->Z - a->Z) >= 0 ? 1 : -1;
    double ex, ey, ez, tx, ty, tz;
    const double hx = hero->Obj.X + cx, hy = hero->Obj.Y, hz = hero->Obj.Z + cz;
    // (round a tight bend the road s_railBack behind the car is off to its side: the eye comes nearer, until it looks along the way
    // the car faces within RAIL_CAMERA_SIDE)
    const double fx = sin(angle), fz = cos(angle);
    double back = s_railBack;
    int behind = RaisedAlong(seg, t, -way * back, &ex, &ey, &ez);
    while (back > RAIL_CAMERA_NEAREST) {
        const double dx = hx - ex, dz = hz - ez, d = sqrt(dx * dx + dz * dz);
        if (d < 1 || (dx * fx + dz * fz) / d >= cos(RAIL_CAMERA_SIDE * 3.14159265358979 / 180)) break;
        back -= 256;
        behind = RaisedAlong(seg, t, -way * back, &ex, &ey, &ez);
    }
    RaisedAlong(seg, t, way * s_railLead, &tx, &ty, &tz);
    // (it looks between the car and the road ahead of it: the car stays in the middle of the view, the road ahead above it)
    tx = hx + (tx - hx) * RAIL_CAMERA_LOOK; ty = hy + (ty - hy) * RAIL_CAMERA_LOOK; tz = hz + (tz - hz) * RAIL_CAMERA_LOOK;
    // (lower too when nearer, so the view is no steeper)
    const double up = s_railUp * sqrt(back / s_railBack);
    ey += up;
    // over a crest between the eye and the car: the line to the car clears the road by RAIL_CAMERA_CLEAR all the way
    double lift = 0;
    for (int k = 1; k < 8; k++) {
        double mx, my, mz;
        RaisedAlong(seg, t, -way * back * k / 8.0, &mx, &my, &mz);
        const double line = hy + (ey - hy) * k / 8.0;
        const double need = (my + RAIL_CAMERA_CLEAR - line) * 8.0 / k;      // what the eye has to rise for the line to clear this point
        if (need > lift) lift = need;
    }
    ey += lift;
    // under a level of the road over the eye
    double over;
    if (RaisedBetween(ex, ez, ey - up - lift + 800.0, ey + RAISED_UNDER, behind, &over) && ey > over - RAISED_UNDER) ey = over - RAISED_UNDER;
    eye[0] = (S32)(ex - cx); eye[1] = (S32)ey; eye[2] = (S32)(ez - cz);
    target[0] = (S32)(tx - cx); target[1] = (S32)(ty + RAIL_CAMERA_AIM); target[2] = (S32)(tz - cz);
    return TRUE;
}

// For the camera that follows the hero: how long its arm may be while he is on the raised road. Behind a car coming down a steep
// stretch the road rises: an eye out at the arm's usual length would have to stand as high above the car as the road climbs in that
// length to see it over the road -- 10,000 and more behind a car on Celebration Island's bridge -- which is above the lap's other
// levels that the bridge dives under, and they then hide the car. So the arm is shortened as the road behind the car steepens (the eye
// is lifted over the road by the camera's own ground clearance, which asks RaisedSight): a close view from above, under the levels
// overhead.
static S32 RaisedArm(S32 base) {
    if (CubeMode != CUBE_EXTERIEUR) return base;
    const T_OBJET *hero = &ListObjet[NUM_PERSO];
    S32 f;
    int seg;
    double lat, t;
    if (!RaisedAt(hero->Obj.X + CurrentCubeX * 32768.0, hero->Obj.Z + CurrentCubeY * 32768.0, hero->Obj.Y + 64, RAISED_PAD, &f, &seg, &lat, &t)
            || abs(f - hero->Obj.Y) > 300) return base;
    // the road's rise over a cell or two, the way the lap runs, and whether the car faces that way
    const double behind = -RaisedSlope(seg, hero->Obj.Beta);   // how steeply the road rises behind the car
    double arm = behind < 0.3 ? base : 2800 / behind + 1200;
    // Under another level of the road that runs with this one -- a helix, its levels one over the other -- the eye has to stay under
    // that level: over the car, and over the road a few cells before or after it. (A level that only crosses overhead hides the car
    // for a moment and is let pass.)
    double ceiling = 1e18;
    int over[3] = {0, 0, 0};
    for (int k = 0; k < 3; k++) {
        int i = seg + (k - 1) * RAISED_STACK;
        if (s_raisedLoop) { const int n = s_nbRaised - 1; i = ((i % n) + n) % n; }
        else if (i < 0 || i > s_nbRaised - 1) continue;
        double level;
        if (!RaisedBetween(s_raised[i].X, s_raised[i].Z, s_raised[i].Y + 800.0, s_raised[i].Y + 9000.0, i, &level)) continue;
        over[k] = 1;
        if (level - s_raised[i].Y < ceiling) ceiling = level - s_raised[i].Y;
    }
    if (over[1] && (over[0] || over[2])) {
        double up = sin((AlphaCam & 4095) * 2 * 3.14159265358979 / 4096.0);
        if (up < 0.2) up = 0.2;
        const double under = (ceiling - RAISED_UNDER) / up;     // the arm whose eye is RAISED_UNDER below the level overhead
        if (under < arm) arm = under;
    }
    return arm < 3000 ? 3000 : arm > base ? base : (S32)arm;
}

void RaceMod_FloorFor(S32 y) { s_floorRef = s_nbRaised ? y : RAISED_NOREF; }
void RaceMod_FloorForNothing(void) { s_floorRef = RAISED_NOREF; }

// The hero kept on the raised road: standing on it at (ox, oy, oz) (cube-local) -- not under it -- a move to (*nx, *nz) that would take
// his middle nearer than RAISED_INSET to an edge ends on that line instead, beside the same point of the road; a move that leaves the
// road altogether is not made. Where the road lies on the ground (its ends, coming down onto it) he may drive off it to the side -- but
// not on a side where the ground beside it falls away: the lava lake's ramp comes down onto the dock along the island's west cliff, and
// a car against its left rail, there where the road met the ground, went over the cliff into the sea (2026-10-02).
#define RAISED_GROUNDED 300                // a road no higher than this over the ground under it lies on the ground
#define RAISED_BESIDE 384                  // how far past the floor's edge the ground beside the road is looked at

// Whether a place (world units) is past the road's own first or last point -- off its end, near it -- on a road that isn't a loop.
// (RaisedAt then finds the segment before the open last one, clamped to its end, and that is no edge to be held at: a car against the
// ramp's left rail where it came down onto the dock was pulled back to it at every step, 2026-10-02.)
static int RaisedPastEnd(double wx, double wz) {
    if (s_raisedLoop || s_nbRaised < 2) return 0;
    for (int end = 0; end < 2; end++) {
        const T_RACE_RAISED *tip = end ? &s_raised[s_nbRaised - 1] : &s_raised[0], *in = end ? tip - 1 : tip + 1;
        const double ox = tip->X - in->X, oz = tip->Z - in->Z, reach = tip->Half + 4 * 512.0;    // (out of the road at that end)
        const double dx = wx - tip->X, dz = wz - tip->Z;
        if (dx * ox + dz * oz > 0 && dx * dx + dz * dz < reach * reach) return 1;
    }
    return 0;
}

void RaceMod_Rail(S32 ox, S32 oy, S32 oz, S32 *nx, S32 *nz) {
    if (!s_nbRaised || CubeMode != CUBE_EXTERIEUR) return;
    const double cx = CurrentCubeX * 32768.0, cz = CurrentCubeY * 32768.0;
    S32 f;
    int seg;
    double lat, t;
    if (!RaisedAt(ox + cx, oz + cz, oy + 64, RAISED_PAD, &f, &seg, &lat, &t)) return;
    if (oy < f - 200 || oy > f + 1500) return;
    const int grounded = f - CalculAltitudeTerrain(ox, oz, 0) < RAISED_GROUNDED;
    if (!RaisedAt(*nx + cx, *nz + cz, oy + 64, 4 * 512, &f, &seg, &lat, &t)) {
        if (grounded) return;
        *nx = ox; *nz = oz; s_railTouch = 1; return;
    }
    // (past the road's own end: onto the ground it comes down to)
    if (!s_raisedLoop && ((seg == 0 && t <= 0) || (seg == s_nbRaised - 2 && t >= 1) || RaisedPastEnd(*nx + cx, *nz + cz))) return;
    const T_RACE_RAISED *a = &s_raised[seg], *c = a + 1;
    const double half = a->Half + (c->Half - a->Half) * t, limit = half - RAISED_INSET;
    if (fabs(lat) <= limit) return;
    const double sx = c->X - a->X, sz = c->Z - a->Z, len = sqrt(sx * sx + sz * sz);
    const double px = a->X + sx * t, pz = a->Z + sz * t, side = lat > 0 ? limit : -limit;
    if (grounded) {
        // the ground just past the floor on the side he is going: about as high as the road, he drives off onto it
        const double out = (lat > 0 ? 1 : -1) * (half + RAISED_PAD + RAISED_BESIDE);
        const S32 bx = (S32)floor(px - sz / len * out - cx + 0.5), bz = (S32)floor(pz + sx / len * out - cz + 0.5);
        if (f - CalculAltitudeTerrain(bx, bz, 0) < RAISED_GROUNDED) return;
    }
    s_railTouch = 1;
    *nx = (S32)floor(px - sz / len * side - cx + 0.5);
    *nz = (S32)floor(pz + sx / len * side - cz + 0.5);
}

// The hero is not let under the surface of the road he stood on. The engine lowers an object that nothing of the ground holds up by half
// a brick to see whether it stands on another object, and leaves it lowered when that finds one: with an opponent's car against his --
// their boxes overlapping -- he rode half a brick into the road. Called with the rail (RaceMod_Rail) after everything else in his move:
// the same car's box also pushed him sideways in places after the rail had had its say, past the road's edge, and he fell to the level
// below. Baldino's car, the widest of the opponents' (its box 1,396 across, the racer's 1,266, the bike's 894), did it the most.
void RaceMod_Floor(S32 ox, S32 oy, S32 oz, S32 nx, S32 *ny, S32 nz) {
    if (!s_nbRaised || CubeMode != CUBE_EXTERIEUR) return;
    const double cx = CurrentCubeX * 32768.0, cz = CurrentCubeY * 32768.0;
    S32 f;
    int seg;
    double lat, t;
    // (standing on the road -- or a little over it, a jump on foot -- where he was)
    if (!RaisedAt(ox + cx, oz + cz, oy + 64, RAISED_PAD, &f, &seg, &lat, &t) || oy < f - 200 || oy > f + 1500) return;
    if (f - CalculAltitudeTerrain(ox, oz, 0) < 300) return;
    if (!RaisedAt(nx + cx, nz + cz, oy + 64, RAISED_PAD, &f, &seg, &lat, &t)) return;
    if (*ny < f) *ny = f;
}

// ---- a jump flown at the car's speed ------------------------------------------------------------------------------------------------
// A jump is a scripted flight: the hero's track script plays one of RACE_JUMP_ANIM_FIRST..LAST (the build's copy of the retail flight,
// sized to the gap: each island's own, so the tracks of several islands can be built into one folder), and
// the animation's own steps carry him along its arc in its own time, whatever speed the car came to the take-off at. Here the flight's
// clock runs as many times as fast as the car's speed at the take-off is over the flight's own speed (its keyframes' steps along the
// ground over their time): the same arc, so the same landing on the far ramp, flown faster by a faster car and slower by a slower one,
// within jump_speed's bounds (a car crawling onto the take-off still jumps, not in slow motion). The car keeps its speed through the air
// (BUGGY.CPP leaves it alone while the animation carries the car), so it lands going as fast as it took off.
// The animation moves on a keyframe at most per frame, and the engine drops the time it ran past a keyframe's end (LastTimer = Time):
// that time is carried into the next keyframe here, or a flight flown fast would lose a good part of its rate to it (at 60 frames a
// second, twice as fast, a third of it).
#define RACE_JUMP_ANIM_FIRST 200           // the flights' generic animations (Terrain/RaceTrackJumpAnim.cs Generic..Last: 200 + the island's
#define RACE_JUMP_ANIM_LAST 295            // number, 12 more for a track in the island's other file, 24 more for each of a track's jumps after its first)
static double s_jumpLo = 0.6, s_jumpHi = 4.0; // jump_speed=<lo> <hi>: the bounds of the rate (0: the flight in its own time)
static double s_jumpRate = 0;              // the flight's clock rate (0: not flying)
static double s_jumpFrac = 0;              // the part of a millisecond of the flight's clock carried to the next frame
static U32 s_jumpOver = 0;                 // how far the flight's clock ran past a keyframe's end this frame

// The flight's own speed along the ground, world units a second: its keyframes' root steps over their times, each as the engine moves
// the car by it (LIB386/ANIM/INTERDEP.CPP: by M = M(Alpha) M(Gamma) M(Beta) of the car's angles, which a flight that pitches the car --
// the lava lake's drop -- turns as it goes: its steps are in the car's own pitched frame).
static double FlightSpeed(const T_OBJ_3D *obj) {
    const U8 *anim = (const U8 *)obj->Anim.Ptr;
    if (TransFctAnim) anim = (const U8 *)TransFctAnim(obj->Anim.Num);
    if (!anim) return 0;
    const S32 frames = ((const U16 *)anim)[0], groups = ((const U16 *)anim)[1];
    const double unit = 2 * 3.14159265358979 / 4096;
    double along = 0, ms = 0, alpha = obj->Alpha, beta = obj->Beta, gamma = obj->Gamma;
    for (S32 k = 1; k < frames; k++) {
        const S16 *f = (const S16 *)(anim + 8 + k * (groups + 1) * 8); // time, step X, Y, Z, master bits, Alpha, Beta, Gamma, ...
        double da = 0, db = 0, dg = 0;
        if (f[4] & 1) { da = (S16)(f[5] << 4) / 16; db = (S16)(f[6] << 4) / 16; dg = (S16)(f[7] << 4) / 16; }
        const double a = (alpha + da / 2) * unit, b = (beta + db / 2) * unit, g = (gamma + dg / 2) * unit;
        alpha += da; beta += db; gamma += dg;
        const double sa = sin(a), ca = cos(a), sb = sin(b), cb = cos(b), sg = sin(g), cg = cos(g);
        const double wx = cg * cb * f[1] - sg * f[2] + cg * sb * f[3];
        const double wz = (sa * sg * cb - ca * sb) * f[1] + sa * cg * f[2] + (sa * sg * sb + ca * cb) * f[3];
        ms += (U16)f[0];
        along += sqrt(wx * wx + wz * wz);
    }
    return ms > 0 ? along * 1000.0 / ms : 0;
}

// Before the hero's animation is moved on (OBJECT.CPP GereObjAnim): while he flies a jump, the keyframe's start and end are brought nearer
// by the extra time the flight's clock runs this frame (or put back, flown slower), so ObjectSetInterDep finds it that much further on.
void RaceMod_JumpClock(T_OBJ_3D *obj, S32 genAnim, S32 move) {
    if (!s_active || genAnim < RACE_JUMP_ANIM_FIRST || genAnim > RACE_JUMP_ANIM_LAST || move != MOVE_BUGGY) {
        if (s_flying >= 0) { s_landed = s_flying; s_landedAt = TimerRefHR; s_flying = -1; }
        s_jumpRate = 0;
        return;
    }
    s_flying = genAnim;
    if (s_jumpHi <= 0) { s_jumpRate = 0; return; }
    if ((S32)(TimerRefHR - obj->Time) <= 0) return; // no time has passed (or the clock went back: ObjectSetInterDep's own case)
    if (!s_jumpRate) {
        const double own = FlightSpeed(obj), car = ListBuggy[0].Speed;
        double rate = own > 0 ? car / own : 1;
        if (rate < s_jumpLo) rate = s_jumpLo;
        if (rate > s_jumpHi) rate = s_jumpHi;
        s_jumpRate = rate; s_jumpFrac = 0;
        Log_Info("[racemod] jump: the car at %.0f km/h, the flight's own speed %.0f km/h: flown x%.2f", car * 3.6 / 512, own * 3.6 / 512, rate);
    }
    const U32 dt = TimerRefHR - obj->Time;
    const double run = s_jumpRate * dt + s_jumpFrac;
    const U32 step = (U32)run;
    s_jumpFrac = run - step;
    obj->LastTimer += dt - step; // (modulo 2^32: brought nearer when the step is longer than the frame)
    obj->NextTimer += dt - step;
    s_jumpOver = (S32)(TimerRefHR - obj->NextTimer) > 0 ? TimerRefHR - obj->NextTimer : 0;
}

// ... and after it: a keyframe just reached, the time run past its end goes into the next one (no more than the whole of it).
void RaceMod_JumpClockAfter(T_OBJ_3D *obj, S32 flag) {
    if (!s_jumpRate || !(flag & FLAG_FRAME) || (flag & FLAG_LAST_FRAME) || !s_jumpOver) return;
    U32 over = s_jumpOver;
    const U32 whole = obj->NextTimer - obj->LastTimer;
    if (over > whole) over = whole;
    obj->LastTimer -= over;
    obj->NextTimer -= over;
    s_jumpOver = 0;
}

static void LoadRaised(const char *path) {
    FILE *f = fopen(path, "r");
    if (!f) { Log_Info("[racemod] raised road: %s could not be read", path); return; }
    int capacity = 2048, count = 0;
    T_RACE_RAISED *r = (T_RACE_RAISED *)malloc(capacity * sizeof(T_RACE_RAISED));
    char line[128];
    while (fgets(line, sizeof(line), f)) {
        int a, b, c, d, e = 0;
        if (sscanf(line, "%d %d %d %d %d", &a, &b, &c, &d, &e) < 4) continue;
        if (count == capacity) { capacity *= 2; r = (T_RACE_RAISED *)realloc(r, capacity * sizeof(T_RACE_RAISED)); }
        r[count].X = a; r[count].Z = b; r[count].Y = c; r[count].Half = d; r[count].Bank = e;
        count++;
    }
    fclose(f);
    if (count < 2) { free(r); return; }
    // the squares over its extent, each with the points in it
    int x0 = 0x7FFFFFFF, z0 = 0x7FFFFFFF, x1 = -0x7FFFFFFF, z1 = -0x7FFFFFFF;
    for (int i = 0; i < count; i++) {
        const int bx = (int)floor((double)r[i].X / RAISED_BUCKET), bz = (int)floor((double)r[i].Z / RAISED_BUCKET);
        if (bx < x0) x0 = bx;
        if (bx > x1) x1 = bx;
        if (bz < z0) z0 = bz;
        if (bz > z1) z1 = bz;
    }
    s_rbX0 = x0; s_rbZ0 = z0; s_rbW = x1 - x0 + 1; s_rbH = z1 - z0 + 1;
    s_rbStart = (int *)calloc(s_rbW * s_rbH + 1, sizeof(int));
    s_rbItem = (int *)malloc(count * sizeof(int));
    for (int i = 0; i < count; i++)
        s_rbStart[((int)floor((double)r[i].Z / RAISED_BUCKET) - z0) * s_rbW + (int)floor((double)r[i].X / RAISED_BUCKET) - x0 + 1]++;
    for (int b = 0; b < s_rbW * s_rbH; b++) s_rbStart[b + 1] += s_rbStart[b];
    int *fill = (int *)calloc(s_rbW * s_rbH, sizeof(int));
    for (int i = 0; i < count; i++) {
        const int b = ((int)floor((double)r[i].Z / RAISED_BUCKET) - z0) * s_rbW + (int)floor((double)r[i].X / RAISED_BUCKET) - x0;
        s_rbItem[s_rbStart[b] + fill[b]++] = i;
    }
    free(fill);
    s_raised = r;
    s_nbRaised = count;
    s_raisedLoop = count > 3 && r[0].X == r[count - 1].X && r[0].Z == r[count - 1].Z && r[0].Y == r[count - 1].Y;
    AltitudeHook = RaisedAltitude;
    TerrainColHook = RaisedCol;
    SightFloorHook = RaisedSight;
    FollowCamArmHook = RaisedArm;
    FollowCamEyeHook = RailCamera;
    // (the camera that follows the car is turned on with the first frame: RaceMod_Frame)
    int lowest = r[0].Y, highest = r[0].Y;
    for (int i = 1; i < count; i++) {
        if (r[i].Y < lowest) lowest = r[i].Y;
        if (r[i].Y > highest) highest = r[i].Y;
    }
    Log_Info("[racemod] raised road: %d points, heights %d to %d%s", count, lowest, highest, s_raisedLoop ? ", a loop" : "");
}

S32 RaceMod_StatueShown(S32 risen) { return RaceMod_Active() && s_statue >= 0 ? s_statue != 0 : risen != 0; }

static void LoadPath(T_RACE_OPPONENT *o, const char *path) {
    FILE *f = fopen(path, "r");
    if (!f) return;
    int capacity = 4096, count = 0;
    S32 *x = (S32 *)malloc(capacity * sizeof(S32)), *z = (S32 *)malloc(capacity * sizeof(S32));
    S32 *y = (S32 *)malloc(capacity * sizeof(S32)), *v = (S32 *)malloc(capacity * sizeof(S32));
    S32 *r = (S32 *)malloc(capacity * sizeof(S32));
    int radii = 1;
    char line[128];
    while (fgets(line, sizeof(line), f) && count < capacity) {
        int a, b, c, d, e;
        const int n = sscanf(line, "%d %d %d %d %d", &a, &b, &c, &d, &e);
        if (n < 4) continue;
        x[count] = a; z[count] = b; y[count] = c; v[count] = d;
        if (n == 5) r[count] = e; else radii = 0;
        count++;
    }
    fclose(f);
    if (count < 3) { free(x); free(z); free(y); free(v); free(r); return; }
    if (!radii) { free(r); r = NULL; }
    o->X = x; o->Z = z; o->Y = y; o->V = v; o->R = r; o->Count = count;
    o->Cum = (double *)malloc((count + 1) * sizeof(double));
    o->Cum[0] = 0;
    for (int i = 1; i <= count; i++) {
        const int a = i - 1, b = i % count;
        o->Cum[i] = o->Cum[i - 1] + sqrt((double)(x[b] - x[a]) * (x[b] - x[a]) + (double)(z[b] - z[a]) * (z[b] - z[a]));
    }
    o->Length = o->Cum[count];
}

// "opponent_..." is the first opponent, "opponent2_..." the second, and so on: which one, and the rest of the key after the '_'
static T_RACE_OPPONENT *OpponentKey(const char *key, const char **rest) {
    if (strncmp(key, "opponent", 8)) return NULL;
    int n = 0;
    const char *p = key + 8;
    if (*p >= '2' && *p <= '0' + RACE_MAX_OPPONENTS) n = *p++ - '1';
    if (*p != '_') return NULL;
    *rest = p + 1;
    return &s_opp[n];
}

// How hard the player's car pulls at a speed: in the gear the automatic gearbox would be in, the gear's pull (RaceSpeed in BUGGY.CPP).
static double Pull(double speed) {
    S32 g = 0;
    while (g < s_car.Gears - 1 && s_car.GearTop[g] <= speed) g++;
    double pull = (double)RACE_REF_TOP / s_car.GearTop[g];
    return pull > 4.0 ? 4.0 : pull < 0.25 ? 0.25 : pull;
}

// An opponent's speeds along its line, from the player's car: as fast as the car's steering takes each bend (the buggy turns at a fixed
// rate, SpeedRot a second, so a bend of radius R is taken at up to that rate times R) and no faster than its top gear, braking before
// the bends as hard as its brakes and pulling away after them as hard as its gears; the opponent's character scales the top speed and
// pull, and the cornering. Two rounds each way, since the lap is a loop.
static void PlanSpeeds(T_RACE_OPPONENT *o) {
    if (!o->R) return;
    const int count = o->Count;
    const double top = (double)s_car.GearTop[s_car.Gears - 1] * o->Top / 100.0;
    const double turn = s_car.Steer * 2 * 3.14159265358979 / 4096.0 * o->Grip / 100.0;
    const double brake = s_car.Brake, accel = s_car.Accel * o->Top / 100.0;
    double *v = (double *)malloc(count * sizeof(double));
    if (s_gravity > 0 && s_nbRaised) {
        // A rollercoaster of a lap (gravity=): the line's slopes pull at the car as they do at the player's (BUGGY.CPP
        // RaceSpeedOnSlope) -- the engine only up to the share of its top speed it holds on a climb, the slope's own pull, and the drag
        // that takes a car's speed over its top gear's back off it. Braking for a bend, the slope counts too.
        double *pullBySlope = (double *)malloc(count * sizeof(double)), *share = (double *)malloc(count * sizeof(double));
        for (int i = 0; i < count; i++) {
            const int next = (i + 1) % count;
            const double ds = o->Cum[i + 1] - o->Cum[i];
            const double slope = ds > 1 ? (o->Y[next] - o->Y[i]) / ds : 0;
            pullBySlope[i] = -s_gravity * RACE_GRAVITY * slope / sqrt(1 + slope * slope);
            share[i] = RaceMod_ClimbShare(s_gravity, slope);
        }
        // (over its top gear's speed the car steers as much quicker as it goes faster -- BUGGY.CPP -- so a bend it can take at that
        // speed it can take at any; and a banked bend turns it: the line's radii come with the banking counted in)
        for (int i = 0; i < count; i++) {
            const double limit = turn * o->R[i];
            v[i] = limit < top ? limit : 4 * top;
            if (v[i] < 400) v[i] = 400;
        }
        for (int round = 0; round < 3; round++) {
            for (int i = 0; i < count; i++) {
                const int next = (i + 1) % count;
                const double ds = o->Cum[i + 1] - o->Cum[i];
                double a = pullBySlope[i];
                if (v[i] < top * share[i]) a += accel * Pull(v[i] / share[i]);
                if (v[i] > top) a -= RACE_OVERSPEED_DRAG * (v[i] - top);
                double reach2 = v[i] * v[i] + 2 * a * ds;
                // (the engine holds the car at its share of the top speed up a climb: the slope does not slow it further)
                if (a < 0 && v[i] >= top * share[i] && reach2 < top * share[i] * top * share[i]) reach2 = top * share[i] * top * share[i];
                const double reach = reach2 > 400.0 * 400.0 ? sqrt(reach2) : 400;
                if (v[next] > reach) v[next] = reach;
            }
            for (int i = count - 1; i >= 0; i--) {
                const int next = (i + 1) % count;
                const double ds = o->Cum[i + 1] - o->Cum[i];
                double slowing = brake - pullBySlope[i];
                if (slowing < brake / 4) slowing = brake / 4;
                const double reach = sqrt(v[next] * v[next] + 2 * slowing * ds);
                if (v[i] > reach) v[i] = reach;
            }
        }
        free(pullBySlope);
        free(share);
    } else {
        for (int i = 0; i < count; i++) {
            double limit = turn * o->R[i];
            v[i] = limit < top ? limit : top;
            if (v[i] < 400) v[i] = 400;
        }
        for (int round = 0; round < 2; round++) {
            for (int i = count - 1; i >= 0; i--) {
                const int next = (i + 1) % count;
                const double ds = o->Cum[i + 1] - o->Cum[i];
                const double reach = sqrt(v[next] * v[next] + 2 * brake * ds);
                if (v[i] > reach) v[i] = reach;
            }
            for (int i = 0; i < count; i++) {
                const int next = (i + 1) % count;
                const double ds = o->Cum[i + 1] - o->Cum[i];
                const double reach = sqrt(v[i] * v[i] + 2 * accel * Pull(v[i]) * ds);
                if (v[next] > reach) v[next] = reach;
            }
        }
    }
    double lap = 0;
    for (int i = 0; i < count; i++) {
        o->V[i] = (S32)v[i];
        lap += (o->Cum[i + 1] - o->Cum[i]) / v[i];
    }
    free(v);
    Log_Info("[racemod] opponent line of %d points planned from the car: a perfect lap %.1f s (skill %d%%: %.1f s)", count, lap,
             o->Pace, lap * 100.0 / o->Pace);
}

static void PathAt(const T_RACE_OPPONENT *o, double s, double *x, double *z, double *y, double *v, double *dx, double *dz);

// the qualifying times' randomness: 0..1
static double RaceRandom(void) {
    s_seed = s_seed * 1103515245u + 12345u;
    return ((s_seed >> 8) & 0xFFFFFF) / 16777216.0;
}

// An opponent on a spot (a grid spot, or a pit spot while the player qualifies): along its line at the line's point nearest the spot
// (behind the start line: its lap 1 is ahead), with the way across to the spot kept (MergeX..) and faded out as it drives off.
static double LineDistance(const T_RACE_OPPONENT *o, int i, double wx, double wy, double wz);

static void OnSpot(T_RACE_OPPONENT *o, const T_RACE_SPOT *spot) {
    const double wx = spot->CubeX * 32768.0 + spot->X, wz = spot->CubeZ * 32768.0 + spot->Z;
    int best = 0;
    double bd = 1e30;
    // (the line's nearest point at the spot's height: a road that winds up over its own start has points of its other levels over
    // the grid, and a car put on one of those started the race a third of a lap ahead)
    for (int i = 0; i < o->Count; i++) {
        const double d = LineDistance(o, i, wx, spot->Y, wz);
        if (d < bd) { bd = d; best = i; }
    }
    o->S = o->Cum[best];
    o->Laps = o->S > o->Length / 2 ? 0 : 1;
    double x, z, y, v, dx, dz;
    PathAt(o, o->S, &x, &z, &y, &v, &dx, &dz);
    o->MergeX = wx - x; o->MergeZ = wz - z; o->MergeY = spot->Y - y;
    o->MergeLeft = RACE_MERGE;
    o->SpotBeta = spot->Beta;
    o->Speed = 0;
}

static S32 RaceCameraEye(S32 *eye, S32 *target);

static void Load(const char *path) {
    FILE *f = fopen(path, "r");
    if (!f) return;
    char line[512];
    while (fgets(line, sizeof(line), f)) {
        char *hash = strchr(line, '#');
        if (hash) *hash = 0;
        char *eq = strchr(line, '=');
        if (!eq) continue;
        *eq = 0;
        char key[64];
        if (sscanf(line, " %63s", key) != 1) continue;
        char *v = eq + 1;
        char *end = v + strlen(v);
        while (end > v && (end[-1] == '\n' || end[-1] == '\r' || end[-1] == ' ')) *--end = 0;
        while (*v == ' ') v++;
        if (!strcmp(key, "gears")) s_car.Gears = atoi(v);
        else if (!strncmp(key, "gear", 4) && key[4] >= '1' && key[4] <= '0' + RACE_MAX_GEARS && !key[5]) s_car.GearTop[key[4] - '1'] = atoi(v);
        else if (!strcmp(key, "accel")) s_car.Accel = Milli(v);
        else if (!strcmp(key, "brake")) s_car.Brake = Milli(v);
        else if (!strcmp(key, "coast")) s_car.Coast = Milli(v);
        else if (!strcmp(key, "reverse")) s_car.Reverse = atoi(v);
        else if (!strcmp(key, "steer")) s_car.Steer = atoi(v);
        else if (!strcmp(key, "automatic")) s_car.Automatic = atoi(v);
        else if (!strcmp(key, "hud")) s_car.Hud = atoi(v);
        else if (!strcmp(key, "startline")) {
            T_RACE_LINE l;
            if (ReadLine(v, &l)) {
                s_car.HasLine = 1;
                s_car.LineCubeX = l.CubeX; s_car.LineCubeZ = l.CubeZ;
                s_car.LineX0 = l.X0; s_car.LineZ0 = l.Z0; s_car.LineX1 = l.X1; s_car.LineZ1 = l.Z1;
                s_car.LineDirX = l.DirX; s_car.LineDirZ = l.DirZ;
                s_lineY = l.Y; s_lineHasY = l.HasY;
            }
        } else if (!strcmp(key, "checkpoint")) {
            if (s_nbCheckpoints < RACE_MAX_CHECKPOINTS && ReadLine(v, &s_checkpoints[s_nbCheckpoints])) s_nbCheckpoints++;
        } else if (!strcmp(key, "grid")) {
            T_RACE_SPOT *g = &s_spots[s_nbSpots];
            if (s_nbSpots < RACE_MAX_SPOTS && sscanf(v, "%d %d %d %d %d %d", &g->CubeX, &g->CubeZ, &g->X, &g->Y, &g->Z, &g->Beta) == 6) s_nbSpots++;
        } else if (!strcmp(key, "pit")) {
            T_RACE_SPOT *g = &s_pits[s_nbPits];
            if (s_nbPits < RACE_MAX_SPOTS && sscanf(v, "%d %d %d %d %d %d", &g->CubeX, &g->CubeZ, &g->X, &g->Y, &g->Z, &g->Beta) == 6) s_nbPits++;
        } else if (!strcmp(key, "qualifying")) s_qualifying = atoi(v);
        else if (!strcmp(key, "weather")) s_fineWeather = !strcmp(v, "fine");
        else if (!strcmp(key, "raised")) LoadRaised(v);
        else if (!strcmp(key, "statue")) s_statue = atoi(v);
        else if (!strcmp(key, "rescue")) s_rescueOn = atoi(v);
        else if (!strcmp(key, "gravity")) s_gravity = atof(v);
        else if (!strcmp(key, "jump_speed")) {
            if (sscanf(v, "%lf %lf", &s_jumpLo, &s_jumpHi) != 2) s_jumpHi = 0;
        }
        else if (!strcmp(key, "railcam")) {
            if (sscanf(v, "%lf %lf %lf", &s_railBack, &s_railUp, &s_railLead) != 3) s_railBack = 0;
        }
        else if (!strcmp(key, "holo_arrow")) s_holoArrow = atoi(v);
        else if (!strcmp(key, "island_file")) {
            if (sscanf(v, "%d %31s", &s_fileIsland, s_fileName) != 2) s_fileIsland = -1;
        }
        else if (!strcmp(key, "loop")) {
            T_RACE_LOOP *l = &s_loops[s_nbLoops];
            double dx, dz, gap;
            if (s_nbLoops < RACE_MAX_LOOPS
                    && sscanf(v, "%d %d %lf %lf %lf %lf %lf %lf %lf %lf", &l->CubeX, &l->CubeZ, &l->X, &l->Y, &l->Z, &dx, &dz, &l->R, &l->Shift, &gap) == 10) {
                const double len = sqrt(dx * dx + dz * dz);
                if (len > 0 && l->R > 0) {
                    l->DirX = dx / len; l->DirZ = dz / len;
                    l->Gap = gap * 3.14159265358979 / 180;
                    s_nbLoops++;
                }
            }
        }
        else if (!strcmp(key, "jumpcam")) {
            T_RACE_JUMPCAM *c = &s_jumpCams[s_nbJumpCams];
            if (s_nbJumpCams < RACE_MAX_JUMPCAMS && sscanf(v, "%d %d %d %d %d %d", &c->Anim, &c->CubeX, &c->CubeZ, &c->X, &c->Y, &c->Z) == 6) s_nbJumpCams++;
        }
        else if (!strcmp(key, "qualifying_seed")) s_seed = (unsigned)strtoul(v, NULL, 10);
        else if (!strcmp(key, "hide_actor")) {
            if (s_nbHidden < RACE_MAX_HIDDEN && sscanf(v, "%d %d", &s_hideScene[s_nbHidden], &s_hideActor[s_nbHidden]) == 2) s_nbHidden++;
        }
        else {
            const char *rest;
            T_RACE_OPPONENT *o = OpponentKey(key, &rest);
            if (!o) continue;
            if (!strcmp(rest, "path")) LoadPath(o, v);
            else if (!strcmp(rest, "grid")) o->Grid = atoi(v);
            else if (!strcmp(rest, "pace")) o->Pace = atoi(v);
            else if (!strcmp(rest, "top")) o->Top = atoi(v);
            else if (!strcmp(rest, "grip")) o->Grip = atoi(v);
            else if (!strcmp(rest, "catchup")) o->Catchup = atoi(v);
            else if (!strcmp(rest, "name")) snprintf(o->Name, sizeof(o->Name), "%s", v);
            else if (!strcmp(rest, "anim")) { int idle, drive; if (sscanf(v, "%d %d", &idle, &drive) == 2) { o->AnimIdle = idle; o->AnimDrive = drive; o->HasAnims = 1; } }
            else if (!strcmp(rest, "actor")) {
                int scene, actor;
                if (o->Scenes < RACE_MAX_OPPONENT_SCENES && sscanf(v, "%d %d", &scene, &actor) == 2) {
                    o->Scene[o->Scenes] = scene; o->Actor[o->Scenes] = actor; o->Scenes++;
                }
            }
        }
    }
    fclose(f);
    // (a lap with loops: the camera that follows the car looks at a loop from its side while the car goes round it; and at a drop from
    // its own place beside it)
    if (s_nbLoops || s_nbJumpCams) FollowCamEyeHook = RaceCameraEye;
    if (s_car.Gears < 1) s_car.Gears = 1;
    if (s_car.Gears > RACE_MAX_GEARS) s_car.Gears = RACE_MAX_GEARS;
    for (S32 g = 0; g < s_car.Gears; g++)
        if (s_car.GearTop[g] < 200) s_car.GearTop[g] = g ? s_car.GearTop[g - 1] : RACE_REF_TOP;
    if (s_car.Accel < 100) s_car.Accel = 100;
    if (s_car.Brake < 100) s_car.Brake = 100;
    if (s_car.Coast < 0) s_car.Coast = 0;
    if (s_car.Reverse < 0) s_car.Reverse = 0;
    if (!s_seed) s_seed = (unsigned)time(NULL);
    for (int n = 0; n < RACE_MAX_OPPONENTS; n++) {
        T_RACE_OPPONENT *o = &s_opp[n];
        if (o->Pace < 10) o->Pace = 10;
        if (!o->Name[0]) snprintf(o->Name, sizeof(o->Name), "Opponent %d", n + 1);
        if (!o->Count) continue;
        PlanSpeeds(o);
        // its lap at its skill, and a qualifying time a few seconds either way of it
        o->PlannedLap = 0;
        for (int i = 0; i < o->Count; i++) o->PlannedLap += (o->Cum[i + 1] - o->Cum[i]) / (o->V[i] > 1 ? o->V[i] : 1);
        o->PlannedLap *= 100.0 / o->Pace;
        const double spread = o->PlannedLap * 1000 * RACE_QUALIFY_SHARE < RACE_QUALIFY_SPREAD ? o->PlannedLap * 1000 * RACE_QUALIFY_SHARE : RACE_QUALIFY_SPREAD;
        o->QualTime = (S32)(o->PlannedLap * 1000 + (RaceRandom() * 2 - 1) * spread);
        // the grid: a few points before the start line
        const int grid = o->Grid < 0 ? 0 : o->Grid >= o->Count ? o->Count - 1 : o->Grid;
        o->S = grid ? o->Cum[o->Count - grid] : 0;
        o->Laps = grid ? 0 : 1;
    }
    if (s_nbSpots && s_car.HasLine) {
        // where they wait until the grid is formed: their pit spots, or (with no pit lane) the grid spots behind the player
        int waiting = 0, spot = 1;
        for (int n = 0; n < RACE_MAX_OPPONENTS; n++) {
            if (!s_opp[n].Count) continue;
            if (waiting < s_nbPits) OnSpot(&s_opp[n], &s_pits[waiting++]);
            else OnSpot(&s_opp[n], &s_spots[spot < s_nbSpots ? spot++ : s_nbSpots - 1]);
        }
        s_phase = s_qualifying ? PHASE_QUALIFYING : PHASE_WAIT;
    }
}

S32 RaceMod_Active(void) {
    if (!s_checked) {
        s_checked = 1;
        Defaults(&s_car);
        const char *path = getenv("LBA2_RACETRACK_FILE");
        if (path && path[0]) {
            s_active = 1;
            Load(path);
        }
    }
    return s_active;
}

// weather=fine: Citadel Island as it is once the storm is over (the chapter-2 weather: no rain or thunder, the CITABAU island file with
// its own light and palette, the clear sky). Asked by COMMON.H's storm macros whenever the island loads -- which can be before anything
// else has asked for the race file, so this reads it too.
S32 RaceMod_FineWeather(void) {
    return RaceMod_Active() && s_fineWeather;
}

const T_RACE_CAR *RaceMod_Car(void) {
    RaceMod_Active();
    return &s_car;
}

S32 RaceMod_Gear(void) { return s_gear; }

void RaceMod_ResetGear(void) {
    s_gear = 1;
    s_lastX = CheckKey(K_X) ? 1 : 0;
    s_lastZ = CheckKey(K_Z) ? 1 : 0;
}

void RaceMod_UpdateGear(S32 speed) {
    const int x = CheckKey(K_X) ? 1 : 0, z = CheckKey(K_Z) ? 1 : 0;
    const S32 was = s_gear;
    if (s_car.Automatic) {
        // up at the gear's top speed, down when the car has slowed well into the gear below's range
        if (s_gear < s_car.Gears && speed >= s_car.GearTop[s_gear - 1] - 40) s_gear++;
        else if (s_gear > 1 && speed < s_car.GearTop[s_gear - 2] * 7 / 10) s_gear--;
    } else {
        if (x && !s_lastX && s_gear < s_car.Gears) s_gear++;
        if (z && !s_lastZ && s_gear > 1) s_gear--;
    }
    s_lastX = x;
    s_lastZ = z;
    // (every change in the log, with what made it: the keys are the game window's keyboard, so a key typed while it has the focus counts)
    if (s_gear != was) Log_Info("[racemod] gear %d (%s)", (int)s_gear, s_car.Automatic ? "automatic" : s_gear > was ? "X" : "Z");
}

static void Message(const char *text) {
    snprintf(s_message, sizeof(s_message), "%s", text);
    s_messageUntil = TimerRefHR + 2500;
}

// Whether the move (x0,z0)->(x1,z1) crosses the segment (ax,az)-(bx,bz).
static int Crosses(double x0, double z0, double x1, double z1, double ax, double az, double bx, double bz) {
    const double d1 = (bx - ax) * (z0 - az) - (bz - az) * (x0 - ax);
    const double d2 = (bx - ax) * (z1 - az) - (bz - az) * (x1 - ax);
    if ((d1 > 0) == (d2 > 0) || d1 == d2) return 0;
    const double e1 = (x1 - x0) * (az - z0) - (z1 - z0) * (ax - x0);
    const double e2 = (x1 - x0) * (bz - z0) - (z1 - z0) * (bx - x0);
    return (e1 > 0) != (e2 > 0);
}

// +1 when the move crosses the line the lap's way, -1 the other way, 0 when it doesn't cross it (or the line is in another cube).
// slack: how far past each of its ends the line counts too.
static int CrossLine(const T_RACE_LINE *l, S32 x0, S32 z0, S32 x1, S32 z1, S32 slack = 0) {
    if (CurrentCubeX != l->CubeX || CurrentCubeY != l->CubeZ) return 0;
    // (a line with a height: only for a car on the road it lies across, not one passing over or under that place)
    if (l->HasY && abs(ListObjet[NUM_PERSO].Obj.Y - l->Y) > RACE_LINE_REACH) return 0;
    double ax = l->X0, az = l->Z0, bx = l->X1, bz = l->Z1;
    const double len = sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az));
    if (slack > 0 && len > 0) {
        const double ux = (bx - ax) / len * slack, uz = (bz - az) / len * slack;
        ax -= ux; az -= uz; bx += ux; bz += uz;
    }
    if (!Crosses(x0, z0, x1, z1, ax, az, bx, bz)) return 0;
    return (double)(x1 - x0) * l->DirX + (double)(z1 - z0) * l->DirZ > 0 ? 1 : -1;
}

// A checkpoint is taken going the lap's way RACE_CHECKPOINT_SLACK past either end of its line: the builder stops a line short of any
// other part of the lap (a car there must not cross it), so on a tight bend, where the road comes back round, its inner end is little
// past the verge -- and a car cutting the bend went round it (the lava lake's first loop, 2026-10-01). Crossed backwards it counts only
// on its own length: past its ends lies the road coming back the other way.
#define RACE_CHECKPOINT_SLACK 768

void RaceMod_Lap(S32 x, S32 z) {
    // (the previous frame's position; forgotten when the car changes cube, where the coordinates jump, or is put on the grid)
    static S32 px = 0, pz = 0, pcx = -1, pcz = -1;
    if (s_wantFlip) { FirstTime = AFF_ALL_FLIP; s_wantFlip = 0; }
    if (s_forgetLap) { pcx = -1; s_forgetLap = 0; }
    if (!s_active || !s_car.HasLine || CubeMode != CUBE_EXTERIEUR) { pcx = -1; return; }
    // the story's arrow to the start line (holo_arrow): Twinsen is at the wheel, so he has found it
    if (s_holoArrow >= 0) { ClrHoloPos(s_holoArrow); Log_Info("[racemod] holomap arrow %d cleared", s_holoArrow); s_holoArrow = -1; }
    const S32 x0 = px, z0 = pz;
    const int same = pcx == CurrentCubeX && pcz == CurrentCubeY;
    px = x; pz = z; pcx = CurrentCubeX; pcz = CurrentCubeY;
    if (!same || (x == x0 && z == z0)) return;

    // the checkpoints: the next one crossed the lap's way moves on; the last one crossed backwards moves back
    if (s_laps > 0)
        for (int i = 0; i < s_nbCheckpoints; i++) {
            if (i == s_next && CrossLine(&s_checkpoints[i], x0, z0, x, z, RACE_CHECKPOINT_SLACK) > 0) {
                s_next++;
                Log_Info("[racemod] checkpoint %d of %d", s_next, s_nbCheckpoints);
            } else if (i == s_next - 1 && CrossLine(&s_checkpoints[i], x0, z0, x, z) < 0) { s_next--; Message("Wrong way"); }
        }

    T_RACE_LINE line = {s_car.LineCubeX, s_car.LineCubeZ, s_car.LineX0, s_car.LineZ0, s_car.LineX1, s_car.LineZ1, s_car.LineDirX, s_car.LineDirZ, s_lineY, s_lineHasY};
    if (CrossLine(&line, x0, z0, x, z) <= 0) return;
    const S32 now = TimerRefHR;
    if (s_laps > 0 && s_next < s_nbCheckpoints) {
        char text[64];
        snprintf(text, sizeof(text), "Missed checkpoint %d: no lap", s_next + 1);
        Message(text);
        Log_Info("[racemod] %s", text);
        return;
    }
    if (s_laps > 0) {
        s_lastLap = now - s_lapStart;
        if (!s_bestLap || s_lastLap < s_bestLap) s_bestLap = s_lastLap;
        Log_Info("[racemod] lap %d in %.2f s%s", (int)s_laps, s_lastLap / 1000.0, s_phase == PHASE_QUALIFYING ? " (qualifying)" : "");
        if (s_phase == PHASE_QUALIFYING) {
            // the qualifying lap: its time decides the grid (formed before the next drawing, in the start line's scene)
            s_qualifyTime = s_lastLap;
            s_wantGrid = 1;
            s_phase = PHASE_WAIT;
            return;
        }
    }
    s_laps++;
    s_lapStart = now;
    s_next = 0;
}

// A test pilot (the console's `autodrive`): while the player drives, the pedals and the steering that keep his car on the first opponent's
// line -- the bearing to a point s_autoLook ahead of the car's place on the line, steered to when the heading is more than a little off it,
// and the line's own speed (the build planned it for the reference car), braked down to when the car is quicker. For headless tests of a
// whole lap; nothing turns it on in play.
static int s_auto = 0, s_autoIndex = -1;
static double s_autoLook = 5 * 512;
static double s_autoSide = 0;              // how far to the side of the line it drives (to the left of the way it runs, x east, z south)

// How far (squared) a place is from point i of an opponent's line. With a raised road the lap passes over itself, so the height counts
// too, and twice: the line's point on another level is then never the nearest.
static double LineDistance(const T_RACE_OPPONENT *o, int i, double wx, double wy, double wz) {
    const double d = (o->X[i] - wx) * (o->X[i] - wx) + (o->Z[i] - wz) * (o->Z[i] - wz);
    return s_nbRaised ? d + 4.0 * (o->Y[i] - wy) * (o->Y[i] - wy) : d;
}

void RaceMod_AutoDrive(int on, double lookCells, double sideCells) {
    s_auto = on;
    s_autoIndex = -1;
    if (lookCells > 0) s_autoLook = lookCells * 512;
    s_autoSide = sideCells * 512;
    Log_Info("[racemod] autodrive %s (looking %.1f cells ahead, %.1f to the side)", on ? "on" : "off", s_autoLook / 512, s_autoSide / 512);
}

U32 RaceMod_AutoInput(void) {
    if (!s_auto || !s_active || Comportement != C_BUGGY || CubeMode != CUBE_EXTERIEUR) return 0;
    const T_RACE_OPPONENT *o = &s_opp[0];
    if (!o->Count) return 0;
    const T_OBJET *hero = &ListObjet[NUM_PERSO];
    const double wx = hero->Obj.X + CurrentCubeX * 32768.0, wz = hero->Obj.Z + CurrentCubeY * 32768.0, wy = hero->Obj.Y;
    int best = -1;
    double bd = 1e30;
    if (s_autoIndex >= 0)
        for (int k = -40; k <= 80; k++) {
            const int i = ((s_autoIndex + k) % o->Count + o->Count) % o->Count;
            const double d = LineDistance(o, i, wx, wy, wz);
            if (d < bd) { bd = d; best = i; }
        }
    if (best < 0 || bd > 12.0 * 512 * 12 * 512)
        for (int i = 0; i < o->Count; i++) {
            const double d = LineDistance(o, i, wx, wy, wz);
            if (d < bd) { bd = d; best = i; }
        }
    s_autoIndex = best;
    double x, z, y, v, dx, dz;
    // (looking further ahead the faster the car goes over its top gear's speed: a rollercoaster of a lap, gravity=)
    const double fastest = s_car.GearTop[s_car.Gears - 1];
    const double look = ListBuggy[0].Speed > fastest ? s_autoLook * ListBuggy[0].Speed / fastest : s_autoLook;
    PathAt(o, o->Cum[best] + look, &x, &z, &y, &v, &dx, &dz);
    const double dl = sqrt(dx * dx + dz * dz);
    if (s_autoSide != 0 && dl > 0) { x += dz / dl * s_autoSide; z -= dx / dl * s_autoSide; }
    const double want = atan2(x - wx, z - wz) * 4096.0 / (2 * 3.14159265358979);
    double err = want - (hero->Obj.Beta & 4095);
    while (err >= 2048) err -= 4096;
    while (err < -2048) err += 4096;
    U32 bits = 0;
    if (err > 24) bits |= I_LEFT;
    else if (err < -24) bits |= I_RIGHT;
    // the line's speed here (the reference car's), and the car's own
    const double here = o->V[best], speed = ListBuggy[0].Speed;
    bits |= speed > here * 1.05 + 150 ? I_DOWN : I_UP;
    return bits;
}

// An opponent's line at distance s: position (world units) and heading.
static void PathAt(const T_RACE_OPPONENT *o, double s, double *x, double *z, double *y, double *v, double *dx, double *dz) {
    while (s < 0) s += o->Length;
    while (s >= o->Length) s -= o->Length;
    int lo = 0, hi = o->Count;
    while (hi - lo > 1) { const int mid = (lo + hi) / 2; if (o->Cum[mid] <= s) lo = mid; else hi = mid; }
    const int a = lo, b = (lo + 1) % o->Count;
    const double span = o->Cum[lo + 1] - o->Cum[lo];
    const double t = span > 0 ? (s - o->Cum[lo]) / span : 0;
    *x = o->X[a] + (o->X[b] - o->X[a]) * t;
    *z = o->Z[a] + (o->Z[b] - o->Z[a]) * t;
    *y = o->Y[a] + (o->Y[b] - o->Y[a]) * t;
    *v = o->V[a] + (o->V[b] - o->V[a]) * t;
    *dx = o->X[b] - o->X[a];
    *dz = o->Z[b] - o->Z[a];
}

static int OpponentActor(const T_RACE_OPPONENT *o) {
    for (int i = 0; i < o->Scenes; i++)
        if (o->Scene[i] == NumCube && o->Actor[i] > 0 && o->Actor[i] < NbObjets) return o->Actor[i];
    return -1;
}

static double PlayerProgress(void);
static double OpponentProgress(const T_RACE_OPPONENT *o);

// the opponents racing: a line and cars in the scenes
static int Opponents(void) {
    int n = 0;
    for (int i = 0; i < RACE_MAX_OPPONENTS; i++) if (s_opp[i].Count && s_opp[i].Scenes) n++;
    return n;
}

// The player's car on a grid spot (in the start line's scene only), standing.
static void PlayerOnSpot(const T_RACE_SPOT *spot) {
    if (CurrentCubeX != spot->CubeX || CurrentCubeY != spot->CubeZ) return;
    T_OBJET *hero = &ListObjet[NUM_PERSO];
    hero->Obj.X = spot->X;
    hero->Obj.Y = spot->Y;
    hero->Obj.Z = spot->Z;
    hero->Obj.Beta = spot->Beta;
    ClearRealAngle(hero);
    ListBuggy[0].Speed = 0;
    ListBuggy[0].SpeedInc = 0;
    s_forgetLap = 1;
}

// The grid: every racer by its time (the player's qualifying lap; without qualifying the player is on pole), each on its spot, the laps
// counted afresh from the start line; then the grid is shown and the count-down runs.
static void FormGrid(S32 now) {
    s_nbOrder = 0;
    s_order[s_nbOrder] = -1;
    s_orderTime[s_nbOrder++] = s_qualifying ? s_qualifyTime : 0;
    for (int n = 0; n < RACE_MAX_OPPONENTS; n++)
        if (s_opp[n].Count && s_opp[n].Scenes) { s_order[s_nbOrder] = n; s_orderTime[s_nbOrder++] = s_opp[n].QualTime; }
    for (int i = 1; i < s_nbOrder; i++)
        for (int j = i; j > 0 && s_orderTime[j] < s_orderTime[j - 1]; j--) {
            const int t = s_order[j]; s_order[j] = s_order[j - 1]; s_order[j - 1] = t;
            const S32 u = s_orderTime[j]; s_orderTime[j] = s_orderTime[j - 1]; s_orderTime[j - 1] = u;
        }
    for (int i = 0; i < s_nbOrder; i++) {
        const T_RACE_SPOT *spot = &s_spots[i < s_nbSpots ? i : s_nbSpots - 1];
        if (s_order[i] < 0) PlayerOnSpot(spot);
        else OnSpot(&s_opp[s_order[i]], spot);
    }
    s_laps = 0; s_next = 0; s_lastLap = 0; s_bestLap = 0;
    s_gridAt = now;
    s_goAt = now + (s_qualifying ? RACE_RESULTS_MS : 1000) + 3000;
    s_phase = PHASE_GRID;
    s_wantFlip = 1;
    char text[256];
    int used = snprintf(text, sizeof(text), "[racemod] grid:");
    for (int i = 0; i < s_nbOrder && used < (int)sizeof(text); i++)
        used += snprintf(text + used, sizeof(text) - used, " %d %s %.2f s;", i + 1, s_order[i] < 0 ? "Twinsen" : s_opp[s_order[i]].Name, s_orderTime[i] / 1000.0);
    Log_Info("%s", text);
}

// A game that starts on foot beside the car on pole (Play's start, the scene's own start): Twinsen turned to face it, so the action key
// gets him in. A scene's start keeps no facing -- he comes in facing whatever way he last did -- and the car takes him only when he
// faces it. Once, in the start line's scene.
static int s_faced = 0;
// (a track with no grid -- no opponents to line up: the old moon's loops -- has the car itself to go by, and Twinsen is brought up to its
// side, where the action key reaches it: the build leaves him a couple of cells behind it)
static void FaceTheCar(void) {
    if (s_faced || CubeMode != CUBE_EXTERIEUR) return;
    T_OBJET *hero = &ListObjet[NUM_PERSO];
    double dx, dz;
    S32 y;
    int own = 0;
    if (s_nbSpots) {
        const T_RACE_SPOT *pole = &s_spots[0];
        if (CurrentCubeX != pole->CubeX || CurrentCubeY != pole->CubeZ) return;
        dx = pole->X - hero->Obj.X; dz = pole->Z - hero->Obj.Z; y = pole->Y;
    } else {
        if (!s_car.HasLine || !IsBuggyPresent() || CurrentCubeX != s_car.LineCubeX || CurrentCubeY != s_car.LineCubeZ) return;
        const T_OBJET *buggy = &ListObjet[NUM_BUGGY];
        dx = buggy->Obj.X - hero->Obj.X; dz = buggy->Obj.Z - hero->Obj.Z; y = buggy->Obj.Y;
        own = 1;
    }
    s_faced = 1;
    if (Comportement == C_BUGGY) return;
    if (dx * dx + dz * dz > 3.0 * 512 * 3.0 * 512 || abs(y - hero->Obj.Y) > 600) return;
    hero->Obj.Beta = (S32)(atan2(dx, dz) * 4096.0 / (2 * 3.14159265358979)) & 4095;
    if (own) {
        // (a cell short of the car's middle: within the action key's reach, outside its box)
        const double d = sqrt(dx * dx + dz * dz), step = d - 512;
        if (d > 0 && step > 0) { hero->Obj.X += (S32)(dx / d * step); hero->Obj.Z += (S32)(dz / d * step); }
    }
    ClearRealAngle(hero);
    Log_Info("[racemod] Twinsen turned to face his car (turn %d)", (int)hero->Obj.Beta);
}

// ---- back on the road -------------------------------------------------------------------------------------------------------------
// A car that goes into the lava or the sea (the ground's deadly codes) would burn or drown Twinsen, and the game would start again from
// his last save -- on Celebration Island's lava lake, after a leap that fell short or a wheel off a causeway. While he races it is put back
// on the road instead (OBJECT.CPP GereCodesJeu asks RaceMod_Rescue, and RaceMod_Frame does it): on the first opponent's line, a couple
// of cells behind the last place the car was on the ground beside it, facing the way the lap runs, stopped. The lap's count doesn't see
// the move. A track with no opponent has no line: there the game's own death stands.
#define RACE_RESCUE_BACK 2                 // line points (a cell each) behind the last safe place
static int s_safeIndex = -1;               // the first opponent's line point the car was last on the ground beside
static int s_rescue = 0;                   // asked for by GereCodesJeu, done by the next RaceMod_Frame

// A point of the line on the ground, in the cube the player is in: not over a jump's gap, where the line is the flight's arc.
static int LineOnGround(const T_RACE_OPPONENT *o, int i) {
    if (o->X[i] / 32768 != CurrentCubeX || o->Z[i] / 32768 != CurrentCubeY) return 0;
    if (s_nbRaised) return 1;
    return abs(o->Y[i] - CalculAltitudeTerrain(o->X[i] - CurrentCubeX * 32768, o->Z[i] - CurrentCubeY * 32768, 0)) < 400;
}

static void TrackSafePlace(void) {
    const T_RACE_OPPONENT *o = &s_opp[0];
    const T_OBJET *hero = &ListObjet[NUM_PERSO];
    if (!o->Count || CubeMode != CUBE_EXTERIEUR || hero->Move == MOVE_BUGGY || (hero->WorkFlags & FALLING)) return;
    const double wx = hero->Obj.X + CurrentCubeX * 32768.0, wz = hero->Obj.Z + CurrentCubeY * 32768.0, wy = hero->Obj.Y;
    int best = -1;
    double bd = 1e30;
    // (near the last safe place: where the lap crosses itself the other road's points are as near)
    const int window = s_safeIndex >= 0 ? 40 : o->Count;
    for (int k = -window; k <= window; k++) {
        const int i = ((s_safeIndex >= 0 ? s_safeIndex : 0) + k + 2 * o->Count) % o->Count;
        const double d = LineDistance(o, i, wx, wy, wz);
        if (d < bd) { bd = d; best = i; }
    }
    if (best < 0 || !LineOnGround(o, best) || abs(o->Y[best] - hero->Obj.Y) > 600) return;
    const double dx = o->X[best] - wx, dz = o->Z[best] - wz;
    if (dx * dx + dz * dz < 3.5 * 512 * 3.5 * 512) s_safeIndex = best;
}

// A hit -- a land mine going off under the car (Mosquibees Island's way round its jump) -- stops the car dead and holds it for
// RACE_HIT_HOLD ms (through RaceMod_Held): the mines are a penalty, and a hit that let the car drive on at full speed was none. It costs
// Twinsen no life: held in the blast the car takes a dozen pieces of it, and one pass through a minefield took 70 of his 100 -- a race
// is not to end in his death (HitObj, OBJECT.CPP, which asks). Non-zero: the hit was the race car's.
#define RACE_HIT_HOLD 600
static S32 s_hitUntil = 0;

S32 RaceMod_CarHit(void) {
    if (!RaceMod_Active() || Comportement != C_BUGGY) return 0;
    ListBuggy[0].Speed = 0;
    s_hitUntil = TimerRefHR + RACE_HIT_HOLD;
    Log_Info("[racemod] the car is hit: stopped");
    return 1;
}

// ---- vertical loops ----
// The car rides round a loop's ring instead of over the ground: up from its foot, over the top upside down and down again, its speed
// changed by the climb and the drop as a real car's would be. Going round a ring of radius R it stays on while v^2/R + g cos(theta)
// is positive (theta 0 at the foot, half a turn at the top), which over the top takes an entry speed of sqrt(5 g R) with nothing pushing
// it on. RACE_LOOP_G is that g (units/s^2): at full speed, 34 km/h (4836 units/s), a car goes round a ring of 4 cells (2048) with room
// to spare; under about 28 km/h it doesn't -- 21 km/h with the throttle held, which pushes it on with RACE_LOOP_PUSH as far as the
// ring presses back on its wheels (all of it at the foot, nothing at the top of a loop taken at the least speed it can be); the brakes
// slow it as far too. Too slow, it falls off -- or, below the quarter, where nothing throws it off, it rolls back down and out
// backwards. A ring with a gap at its top is the other kind: the car is carried round it at the speed it came in with (RACE_LOOP_CARRY
// at the least), the jump over the gap upside down as well (RACE_LOOP_DIP inside the ring's line at its middle) -- it never falls.
#define RACE_PI 3.14159265358979
#define RACE_LOOP_G 1600.0
#define RACE_LOOP_PUSH 600.0
#define RACE_LOOP_DIP 300.0
#define RACE_LOOP_CARRY 2500.0
#define RACE_LOOP_ENTRY 1100.0     // how far past either lane across the road the ring's foot still takes a car in (units)
#define RACE_LOOP_FALL_HOLD 900
enum { LOOP_NONE, LOOP_RIDE, LOOP_GAP, LOOP_FALL };
static int s_loopOn = -1, s_loopState = LOOP_NONE;
static double s_theta = 0, s_loopV = 0, s_loopErr = 0, s_gapOmega = 0;
static double s_fallX, s_fallY, s_fallZ, s_fallVX, s_fallVY, s_fallVZ, s_fallSpin, s_fallTheta;
static S32 s_loopBeta = 0, s_loopTime = 0;

const char *RaceMod_IslandFile(S32 island) {
    return RaceMod_Active() && s_fileName[0] && s_fileIsland == island ? s_fileName : NULL;
}

// Where the car is `theta` round the ring, `radius` from its middle: across the road it goes from Shift/2 left of the middle at the foot
// round to Shift/2 right of it, and the place across it went in at fades into that over the first quarter.
static void LoopPlace(const T_RACE_LOOP *l, double theta, double radius, double *x, double *y, double *z) {
    const double fade = theta < RACE_PI / 2 ? 1 - theta / (RACE_PI / 2) : 0;
    const double across = -l->Shift / 2 + l->Shift * theta / (2 * RACE_PI) + s_loopErr * fade;
    const double along = radius * sin(theta);
    *x = l->X + l->DirX * along - l->DirZ * across;
    *z = l->Z + l->DirZ * along + l->DirX * across;
    *y = l->Y + l->R - radius * cos(theta);
}

// The car pitched `phi` (radians, nose down positive) in its own frame while heading `beta`. The engine turns a body (and an animation's
// step) by M = M(Alpha) M(Gamma) M(Beta) (LIB386/3D/IMATSTDF.CPP): the heading first, then Gamma about the world's Z axis and Alpha about
// its X axis -- so Alpha alone pitches only a car heading along Z, and rolls one heading along X (the old moon's loops, 2026-10-01: the
// car went round them on its side). Here the three angles that make M the heading times the pitch, Mb(beta) Ma(phi):
// Alpha = atan2(cos b sin p, cos p), Beta = atan2(sin b cos p, cos b), Gamma = asin(-sin b sin p).
static void CarPitch(T_OBJ_3D *obj, S32 beta, double phi) {
    const double b = beta * 2 * RACE_PI / 4096, sb = sin(b), cb = cos(b), sp = sin(phi), cp = cos(phi);
    double g = -sb * sp;
    if (g > 1) g = 1; else if (g < -1) g = -1;
    obj->Alpha = (S32)floor(atan2(cb * sp, cp) * 4096 / (2 * RACE_PI) + 0.5) & 4095;
    obj->Beta = (S32)floor(atan2(sb * cp, cb) * 4096 / (2 * RACE_PI) + 0.5) & 4095;
    obj->Gamma = (S32)floor(asin(g) * 4096 / (2 * RACE_PI) + 0.5) & 4095;
}

static void LoopEnd(T_OBJ_3D *obj, double v, const char *how) {
    obj->Alpha = 0; obj->Gamma = 0; obj->Beta = s_loopBeta;
    ListBuggy[0].Speed = (S32)v;
    Log_Info("[racemod] loop %d: %s at %.0f units/s", s_loopOn + 1, how, v);
    s_loopOn = -1; s_loopState = LOOP_NONE;
    s_forgetLap = 1;
}

// For DoAnimExt (EXTFUNC.CPP), on the hero, after his car's move from (ox,oy,oz) towards (*nx,*ny,*nz): non-zero when a loop carries
// the car this frame -- the new place is in *nx..*nz, and nothing else is to move it (no ground, no collisions: the ring has none).
// Also stands the car level again whenever neither a loop nor a flight's master rotation pitches it (a flight cut short left it so).
S32 RaceMod_Loop(S32 ox, S32 oy, S32 oz, S32 *nx, S32 *ny, S32 *nz) {
    T_OBJET *hero = &ListObjet[NUM_PERSO];
    T_OBJ_3D *obj = &hero->Obj;
    if (!RaceMod_Active() || Comportement != C_BUGGY || CubeMode != CUBE_EXTERIEUR) { s_loopOn = -1; s_loopState = LOOP_NONE; return 0; }
    if (s_loopState == LOOP_NONE) {
        if (!(obj->Master & 1) && (obj->Alpha || obj->Gamma)) { obj->Alpha = 0; obj->Gamma = 0; }
        if (!s_nbLoops || (hero->WorkFlags & (FALLING | ANIM_MASTER_GRAVITY)) || ListBuggy[0].Speed <= 0) return 0;
        for (int i = 0; i < s_nbLoops; i++) {
            const T_RACE_LOOP *l = &s_loops[i];
            if (l->CubeX != CurrentCubeX || l->CubeZ != CurrentCubeY || fabs(oy - l->Y) > 600) continue;
            // (the line across the road at the ring's foot, crossed the lap's way)
            const double a0 = (ox - l->X) * l->DirX + (oz - l->Z) * l->DirZ;
            const double a1 = (*nx - l->X) * l->DirX + (*nz - l->Z) * l->DirZ;
            if (!(a0 < 0 && a1 >= 0)) continue;
            const double t = a0 / (a0 - a1);
            const double cx = ox + (*nx - ox) * t, cz = oz + (*nz - oz) * t;
            const double across = -(cx - l->X) * l->DirZ + (cz - l->Z) * l->DirX;
            if (fabs(across) > l->Shift / 2 + RACE_LOOP_ENTRY) continue;
            s_loopOn = i; s_loopState = LOOP_RIDE;
            s_theta = 0; s_loopV = ListBuggy[0].Speed; s_loopErr = across + l->Shift / 2;
            if (l->Gap > 0 && s_loopV < RACE_LOOP_CARRY) s_loopV = RACE_LOOP_CARRY;
            s_loopBeta = (S32)floor(atan2(l->DirX, l->DirZ) * 4096 / (2 * RACE_PI) + 0.5) & 4095;
            s_loopTime = TimerRefHR;
            Log_Info("[racemod] loop %d: in at %d units/s, %.0f across", i + 1, (int)ListBuggy[0].Speed, s_loopErr);
            break;
        }
        if (s_loopState == LOOP_NONE) return 0;
    }
    const T_RACE_LOOP *l = &s_loops[s_loopOn];
    double dt = (TimerRefHR - s_loopTime) / 1000.0;
    s_loopTime = TimerRefHR;
    if (dt < 0) dt = 0;
    if (dt > 0.1) dt = 0.1;
    double x = obj->X, y = obj->Y, z = obj->Z;
    if (s_loopState == LOOP_FALL) {
        s_fallVY -= RACE_LOOP_G * dt;
        s_fallX += s_fallVX * dt; s_fallY += s_fallVY * dt; s_fallZ += s_fallVZ * dt;
        s_fallTheta += s_fallSpin * dt;
        if (s_fallX < 600) s_fallX = 600;
        if (s_fallX > 32168) s_fallX = 32168;
        if (s_fallZ < 600) s_fallZ = 600;
        if (s_fallZ > 32168) s_fallZ = 32168;
        const S32 ground = CalculAltitudeObjet((S32)s_fallX, (S32)s_fallZ, -1);
        x = s_fallX; y = s_fallY; z = s_fallZ;
        CarPitch(obj, s_loopBeta, -s_fallTheta);
        if (y <= ground) {
            y = ground;
            s_hitUntil = TimerRefHR + RACE_LOOP_FALL_HOLD;
            Message("Fell off the loop");
            LoopEnd(obj, 0, "fell off, landed");
        }
    } else {
        if (s_loopState == LOOP_RIDE && l->Gap > 0) {
            // (carried round: the same speed all the way)
            s_theta += s_loopV * dt / l->R;
            if (s_theta >= RACE_PI - l->Gap / 2 && s_theta < RACE_PI + l->Gap / 2) {
                s_loopState = LOOP_GAP;
                s_gapOmega = s_loopV / l->R;
                Log_Info("[racemod] loop %d: over the gap at %.0f units/s", s_loopOn + 1, s_loopV);
            }
        } else if (s_loopState == LOOP_RIDE) {
            const double press = s_loopV * s_loopV / l->R + RACE_LOOP_G * cos(s_theta);
            if (press < 0) {
                // off the ring: thrown along it, still turning as it was
                LoopPlace(l, s_theta, l->R, &s_fallX, &s_fallY, &s_fallZ);
                s_fallVX = l->DirX * s_loopV * cos(s_theta); s_fallVZ = l->DirZ * s_loopV * cos(s_theta);
                s_fallVY = s_loopV * sin(s_theta);
                s_fallSpin = s_loopV / l->R; s_fallTheta = s_theta;
                s_loopState = LOOP_FALL;
                Log_Info("[racemod] loop %d: falls off at %.0f degrees, %.0f units/s", s_loopOn + 1, s_theta * 180 / RACE_PI, s_loopV);
                CarPitch(obj, s_loopBeta, -s_theta);
                *nx = (S32)s_fallX; *ny = (S32)s_fallY; *nz = (S32)s_fallZ;
                return 1;
            }
            const double grip = press >= RACE_LOOP_G ? 1 : press / RACE_LOOP_G;
            double a = -RACE_LOOP_G * sin(s_theta);
            const double top = s_car.GearTop[s_car.Gears - 1];
            if ((Input & I_UP) && s_loopV < top) a += RACE_LOOP_PUSH * grip;
            if ((Input & I_DOWN) && s_loopV > 0) a -= s_car.Brake * grip;
            s_loopV += a * dt;
            s_theta += s_loopV * dt / l->R;
        } else {
            s_theta += s_gapOmega * dt;
            if (s_theta >= RACE_PI + l->Gap / 2) { s_loopState = LOOP_RIDE; s_loopV = s_gapOmega * l->R; }
        }
        if (s_theta >= 2 * RACE_PI) {
            LoopPlace(l, 2 * RACE_PI, l->R, &x, &y, &z);
            y = l->Y;
            LoopEnd(obj, s_loopV, "round");
        } else if (s_theta < 0) {
            LoopPlace(l, 0, l->R, &x, &y, &z);
            y = l->Y;
            LoopEnd(obj, s_loopV, "rolled back out");
        } else {
            double radius = l->R;
            if (s_loopState == LOOP_GAP) {
                const double u = (s_theta - (RACE_PI - l->Gap / 2)) / l->Gap;
                radius -= RACE_LOOP_DIP * sin(RACE_PI * (u < 0 ? 0 : u > 1 ? 1 : u));
            }
            LoopPlace(l, s_theta, radius, &x, &y, &z);
            CarPitch(obj, s_loopBeta, -s_theta);
            ListBuggy[0].Speed = (S32)s_loopV;
        }
    }
    *nx = (S32)floor(x + 0.5); *ny = (S32)floor(y + 0.5); *nz = (S32)floor(z + 0.5);
    return 1;
}

// The camera while the car goes round a loop: an arm behind the car would look into the ring's own back -- the part of it coming down
// behind its foot, between the arm's eye and the car -- so the eye stands out to the ring's side, RACE_LOOP_CAM_FAR radii from its
// middle on the side the car goes in on and a little over it, looking at the ring's middle drawn a little towards the car.
#define RACE_LOOP_CAM_FAR 3.2
static S32 LoopCamera(S32 *eye, S32 *target) {
    if (s_loopOn < 0 || s_loopState == LOOP_NONE) return FALSE;
    const T_RACE_LOOP *l = &s_loops[s_loopOn];
    const T_OBJ_3D *car = &ListObjet[NUM_PERSO].Obj;
    const double nx = -l->DirZ, nz = l->DirX;
    const double mx = l->X, my = l->Y + l->R, mz = l->Z;
    const double far = l->R * RACE_LOOP_CAM_FAR + l->Shift / 2;
    eye[0] = (S32)(mx - nx * far); eye[1] = (S32)(l->Y + l->R * 1.2); eye[2] = (S32)(mz - nz * far);
    target[0] = (S32)(mx + (car->X - mx) * 0.3); target[1] = (S32)(my + (car->Y - my) * 0.3); target[2] = (S32)(mz + (car->Z - mz) * 0.3);
    return TRUE;
}

static S32 JumpCamera(S32 *eye, S32 *target) {
    if (!s_nbJumpCams) return FALSE;
    const S32 anim = s_flying >= 0 ? s_flying : (S32)(TimerRefHR - s_landedAt) < RACE_JUMPCAM_HOLD ? s_landed : -1;
    if (anim < 0) return FALSE;
    for (int i = 0; i < s_nbJumpCams; i++) {
        const T_RACE_JUMPCAM *c = &s_jumpCams[i];
        if (c->Anim != anim || c->CubeX != CurrentCubeX || c->CubeZ != CurrentCubeY) continue;
        const T_OBJ_3D *car = &ListObjet[NUM_PERSO].Obj;
        eye[0] = c->X; eye[1] = c->Y; eye[2] = c->Z;
        target[0] = car->X; target[1] = car->Y + 300; target[2] = car->Z;
        return TRUE;
    }
    return FALSE;
}

static S32 RaceCameraEye(S32 *eye, S32 *target) { return JumpCamera(eye, target) || LoopCamera(eye, target) || RailCamera(eye, target); }

S32 RaceMod_Rescue(void) {
    if (!RaceMod_Active() || !s_rescueOn || Comportement != C_BUGGY || !s_opp[0].Count || s_safeIndex < 0) return 0;
    s_rescue = 1;
    return 1;
}

static void Rescue(void) {
    const T_RACE_OPPONENT *o = &s_opp[0];
    int i = ((s_safeIndex - RACE_RESCUE_BACK) % o->Count + o->Count) % o->Count;
    for (int k = 0; k < 40 && !LineOnGround(o, i); k++) i = (i - 1 + o->Count) % o->Count;
    if (!LineOnGround(o, i)) return;
    T_OBJET *hero = &ListObjet[NUM_PERSO];
    const int j = (i + 1) % o->Count;
    const S32 lx = o->X[i] - CurrentCubeX * 32768, lz = o->Z[i] - CurrentCubeY * 32768;
    hero->Obj.X = lx;
    hero->Obj.Z = lz;
    hero->Obj.Y = s_nbRaised ? o->Y[i] : CalculAltitudeTerrain(lx, lz, 0);
    hero->Obj.Beta = (S32)(atan2((double)(o->X[j] - o->X[i]), (double)(o->Z[j] - o->Z[i])) * 4096.0 / (2 * 3.14159265358979)) & 4095;
    ClearRealAngle(hero);
    hero->WorkFlags &= ~FALLING;
    hero->CodeJeu = 0;
    ListBuggy[0].Speed = 0;
    s_forgetLap = 1;
    s_safeIndex = i;
    Message("Back on the road");
    Log_Info("[racemod] back on the road: line point %d (%d,%d,%d)", i, (int)hero->Obj.X, (int)hero->Obj.Y, (int)hero->Obj.Z);
}

void RaceMod_Frame(void) {
    if (!RaceMod_Active()) return;
    if (Comportement == C_BUGGY) TrackSafePlace();
    if (s_rescue) { s_rescue = 0; Rescue(); }
    // A raised road winds round itself in the air: the classic camera, which stands still until the hero leaves the screen, would watch
    // the car go out of sight behind the road's other levels without its ever leaving the screen. So the camera that follows the car
    // is turned on -- once, with the first frame (the settings have been read by then), and the console's cam_follow can still turn it off.
    static int followed = 0;
    if (!followed && s_nbRaised) { FollowCamera = TRUE; followed = 1; Log_Info("[racemod] raised road: the camera follows the car"); }
    if (!followed && s_nbLoops) { FollowCamera = TRUE; followed = 1; Log_Info("[racemod] loops: the camera follows the car"); }
    FaceTheCar();
    const S32 now = TimerRefHR;
    S32 dt = s_oppLast ? now - s_oppLast : 0;
    s_oppLast = now;
    if (dt < 0) dt = 0;
    if (dt > 100) dt = 100;
    const int driving = Comportement == C_BUGGY;
    const int inStartScene = s_nbSpots && CubeMode == CUBE_EXTERIEUR && CurrentCubeX == s_spots[0].CubeX && CurrentCubeY == s_spots[0].CubeZ;
    // the grid: after the qualifying lap, or (without qualifying) as soon as the player is in his car; formed in the start line's scene
    if (inStartScene && driving && (s_wantGrid || (s_phase == PHASE_WAIT && !s_qualifying))) { s_wantGrid = 0; FormGrid(now); }
    // the cars that aren't racing, out of sight
    for (int i = 0; i < s_nbHidden; i++)
        if (s_hideScene[i] == NumCube && s_hideActor[i] > 0 && s_hideActor[i] < NbObjets) ListObjet[s_hideActor[i]].Obj.Y = -20000;
    if (s_phase == PHASE_GRID && now >= s_goAt) {
        s_phase = PHASE_RACE;
        for (int n = 0; n < RACE_MAX_OPPONENTS; n++) s_opp[n].LapStart = now;
        Log_Info("[racemod] go");
    }
    if (!Opponents()) return;
    // the race starts at GO (without a grid: when the player first drives off); the opponents wait while he is out of his car
    if (!s_nbSpots && !s_oppStarted && driving && (Input & I_UP)) s_oppStarted = 1;
    const int moving = driving && (s_nbSpots ? s_phase == PHASE_RACE : s_oppStarted);
    // (how far round the player is, for the opponents that push harder when they have fallen behind)
    const double me = moving && s_opp[0].Count ? PlayerProgress() : 0;
    for (int n = 0; n < RACE_MAX_OPPONENTS; n++) {
        T_RACE_OPPONENT *o = &s_opp[n];
        if (!o->Count) continue;
        double x, z, y, v, dx, dz;
        PathAt(o, o->S, &x, &z, &y, &v, &dx, &dz);
        if (moving) {
            double pace = o->Pace;
            if (o->Catchup > 0 && s_opp[0].Count && s_laps > 0) {
                const double behind = me - OpponentProgress(o);
                if (behind > 0) pace *= 1 + o->Catchup / 100.0 * (behind < 60 * 512 ? behind / (60 * 512) : 1);
            }
            // its line's speed at its skill, reached from a standstill no faster than the car pulls away (the plan's braking is kept)
            const double target = v * pace / 100.0;
            if (o->Speed < target) {
                o->Speed += s_car.Accel * Pull(o->Speed) * o->Top / 100.0 * pace / 100.0 * dt / 1000.0;
                if (o->Speed > target) o->Speed = target;
            } else
                o->Speed = target;
            const double before = o->S, step = o->Speed * dt / 1000.0;
            o->S += step;
            o->MergeLeft = o->MergeLeft > step ? o->MergeLeft - step : 0;
            if (o->S >= o->Length) o->S -= o->Length;
            if (o->S < before) {                 // past the start line
                o->Laps++;
                if (o->Laps > 1) Log_Info("[racemod] opponent %d: lap %d in %.2f s", n + 1, o->Laps - 1, (now - o->LapStart) / 1000.0);
                o->LapStart = now;
            }
            PathAt(o, o->S, &x, &z, &y, &v, &dx, &dz);
        }
        if (CubeMode != CUBE_EXTERIEUR) continue;
        const int index = OpponentActor(o);
        if (index < 0) continue;
        // off its line while it moves onto it from its grid spot
        const double f = o->MergeLeft / RACE_MERGE;
        T_OBJET *car = &ListObjet[index];
        car->Obj.X = (S32)(x + o->MergeX * f - CurrentCubeX * 32768.0);
        car->Obj.Z = (S32)(z + o->MergeZ * f - CurrentCubeY * 32768.0);
        car->Obj.Y = (S32)(y + o->MergeY * f);
        S32 beta = (S32)(atan2(dx, dz) * 4096.0 / (2 * 3.14159265358979)) & 4095;
        car->Obj.Beta = f >= 1 ? o->SpotBeta : beta;
        ClearRealAngle(car);
        // the driving animation while it moves (its steps are undone every frame: the position is set here)
        const U16 anim = (U16)(o->HasAnims ? (moving ? o->AnimDrive : o->AnimIdle) : (moving ? 1 : 0));
        if (car->GenAnim != anim) InitAnim(anim, ANIM_REPEAT, (U8)index);
        if (moving) s_wantFlip = 1;
    }
}

// The player's car stands on the grid until the count-down's GO (BUGGY.CPP RaceSpeed).
S32 RaceMod_Held(void) {
    return RaceMod_Active() && (s_phase == PHASE_GRID || TimerRefHR < s_hitUntil);
}

// How far round the race the player is, and an opponent, in world units from the start of lap 1 (negative before it). The player is
// placed on the first opponent's line (the lines run side by side round the same lap).
static double PlayerProgress(void) {
    const T_RACE_OPPONENT *o = &s_opp[0];
    if (!o->Count) return 0;
    const double wx = ListObjet[NUM_PERSO].Obj.X + CurrentCubeX * 32768.0, wz = ListObjet[NUM_PERSO].Obj.Z + CurrentCubeY * 32768.0;
    const double wy = ListObjet[NUM_PERSO].Obj.Y;
    int best = -1;
    double bd = 1e30;
    if (s_playerIndex >= 0)
        for (int k = -150; k <= 150; k++) {
            const int i = ((s_playerIndex + k) % o->Count + o->Count) % o->Count;
            const double d = LineDistance(o, i, wx, wy, wz);
            if (d < bd) { bd = d; best = i; }
        }
    if (best < 0 || bd > 20.0 * 512 * 20 * 512)
        for (int i = 0; i < o->Count; i++) {
            const double d = LineDistance(o, i, wx, wy, wz);
            if (d < bd) { bd = d; best = i; }
        }
    s_playerIndex = best;
    const double s = o->Cum[best];
    // before lap 1 the player is behind the line, near the line's end
    if (s_laps == 0) return s > o->Length / 2 ? s - o->Length : s;
    return (s_laps - 1) * o->Length + (s > o->Length * 0.9 && s_next == 0 ? s - o->Length : s);
}

static double OpponentProgress(const T_RACE_OPPONENT *o) {
    if (o->Laps == 0) return o->S > o->Length / 2 ? o->S - o->Length : o->S;
    return (o->Laps - 1) * o->Length + o->S;
}

static void Time(char *out, size_t size, S32 ms) {
    if (ms < 0) ms = 0;
    snprintf(out, size, "%d:%02d.%02d", (int)(ms / 60000), (int)(ms / 1000 % 60), (int)(ms / 10 % 100));
}

// One line of the display, right-aligned at x, with a dark shadow so it reads on sand and on asphalt alike.
static void Line(S32 x, S32 y, const char *text, S32 *left) {
    char buf[96];
    snprintf(buf, sizeof(buf), "%s", text);
    const S32 w = SizeFont(buf);
    ColorFont(0);
    Font(x - w + 2, y + 2, buf);
    ColorFont(LBAWHITE);
    Font(x - w, y, buf);
    if (x - w < *left) *left = x - w;
}

// The count-down's big letters: 5 x 7 blocks each ('.' empty), drawn as filled boxes with a dark shadow.
static const struct {
    char C;
    const char *Rows;
} s_glyphs[] = {
    {'1', "..#.." ".##.." "..#.." "..#.." "..#.." "..#.." ".###."},
    {'2', ".###." "#...#" "....#" "...#." "..#.." ".#..." "#####"},
    {'3', "####." "....#" "....#" ".###." "....#" "....#" "####."},
    {'G', ".###." "#...#" "#...." "#.###" "#...#" "#...#" ".###."},
    {'O', ".###." "#...#" "#...#" "#...#" "#...#" "#...#" ".###."},
    {'!', "..#.." "..#.." "..#.." "..#.." "..#.." "....." "..#.."},
};

// The text centred on (cx, cy) in blocks of `k` pixels, in palette colour `colour`; returns nothing, marks the area to redraw.
static void BigText(S32 cx, S32 cy, const char *text, S32 k, S32 colour) {
    const S32 n = (S32)strlen(text), w = n * 6 * k - k, h = 7 * k;
    const S32 x0 = cx - w / 2, y0 = cy - h / 2, shadow = k / 3 > 1 ? k / 3 : 1;
    for (int pass = 0; pass < 2; pass++)
        for (S32 i = 0; i < n; i++) {
            const char *rows = NULL;
            for (size_t g = 0; g < sizeof(s_glyphs) / sizeof(s_glyphs[0]); g++)
                if (s_glyphs[g].C == text[i]) rows = s_glyphs[g].Rows;
            if (!rows) continue;
            for (S32 r = 0; r < 7; r++)
                for (S32 c = 0; c < 5; c++) {
                    if (rows[r * 5 + c] != '#') continue;
                    const S32 x = x0 + (i * 6 + c) * k + (pass ? 0 : shadow), y = y0 + r * k + (pass ? 0 : shadow);
                    Box(x, y, x + k - 1, y + k - 1, pass ? colour : 0);
                }
        }
    BoxMovingAdd(x0 - 2, y0 - 2, x0 + w + shadow + 2, y0 + h + shadow + 2);
}

// palette colours for the count-down (the game palette, RESS.HQR entry 0): its strongest red and its brightest green (it has no vivid one)
#define RACE_RED 72
#define RACE_GREEN 141

void RaceMod_Draw(void) {
    if (!RaceMod_Active() || !s_car.Hud || !LbaFont) return;
    if (Comportement != C_BUGGY || CubeMode != CUBE_EXTERIEUR) return;
    const S32 speed = ListBuggy[0].Speed;

    void *font = PtrFont;
    const S32 leave = InterLeave, space = InterSpace;
    SetFont(LbaFont, 2, 8);
    MemoClipWindow();
    UnsetClipWindow();

    char text[96], a[24], b[24];
    const S32 right = (S32)ModeDesiredX - 16, top = 12, step = 34;
    S32 left = right, y = top;
    if (speed < 0) snprintf(text, sizeof(text), "Gear R");
    else snprintf(text, sizeof(text), s_car.Automatic ? "Gear %d/%d auto" : "Gear %d/%d", (int)s_gear, (int)s_car.Gears);
    Line(right, y, text, &left);
    y += step;
    // (a cell taken as a metre: the original buggy's top speed, 3800 units a second, is 27 km/h)
    snprintf(text, sizeof(text), "%d km/h", (int)((abs(speed) * 36 + 2560) / 5120));
    Line(right, y, text, &left);
    y += step;
    const S32 now = TimerRefHR;
    if (s_car.HasLine && s_phase == PHASE_QUALIFYING) {
        if (s_laps > 0) {
            Time(a, sizeof(a), now - s_lapStart);
            snprintf(text, sizeof(text), "Qualifying lap  %s", a);
        } else
            snprintf(text, sizeof(text), "Qualifying: cross the start line");
        Line(right, y, text, &left);
        y += step;
        if (s_laps > 0 && s_nbCheckpoints > 0) {
            snprintf(text, sizeof(text), "Checkpoints %d/%d", s_next, s_nbCheckpoints);
            Line(right, y, text, &left);
            y += step;
        }
    } else if (s_car.HasLine && (s_phase == PHASE_WAIT || s_phase == PHASE_GRID)) {
        // the grid, while it is shown before the count-down
        if (s_phase == PHASE_GRID && s_qualifying && now < s_goAt - 3000) {
            Line(right, y, "Grid", &left);
            y += step;
            for (int i = 0; i < s_nbOrder; i++) {
                Time(a, sizeof(a), s_orderTime[i]);
                snprintf(text, sizeof(text), "%d  %s  %s", i + 1, s_order[i] < 0 ? "Twinsen" : s_opp[s_order[i]].Name, a);
                Line(right, y, text, &left);
                y += step;
            }
        } else if (s_phase == PHASE_WAIT) {
            Line(right, y, "Drive to the start line", &left);
            y += step;
        }
    } else if (s_car.HasLine) {
        if (s_laps > 0) {
            Time(a, sizeof(a), TimerRefHR - s_lapStart);
            snprintf(text, sizeof(text), "Lap %d  %s", (int)s_laps, a);
        } else
            snprintf(text, sizeof(text), "Cross the start line");
        Line(right, y, text, &left);
        y += step;
        if (s_laps > 0 && s_nbCheckpoints > 0) {
            snprintf(text, sizeof(text), "Checkpoints %d/%d", s_next, s_nbCheckpoints);
            Line(right, y, text, &left);
            y += step;
        }
        if (s_lastLap) {
            Time(a, sizeof(a), s_lastLap);
            Time(b, sizeof(b), s_bestLap);
            snprintf(text, sizeof(text), "Last %s  Best %s", a, b);
            Line(right, y, text, &left);
            y += step;
        }
    }
    if (Opponents() && s_opp[0].Count && s_phase == PHASE_RACE) {
        // (the player's place among the racers: one more for every opponent further round)
        const double me = PlayerProgress();
        int place = 1, racers = 1;
        for (int n = 0; n < RACE_MAX_OPPONENTS; n++) {
            if (!s_opp[n].Count || !s_opp[n].Scenes) continue;
            racers++;
            if (OpponentProgress(&s_opp[n]) > me) place++;
        }
        snprintf(text, sizeof(text), "Position %d/%d", place, racers);
        Line(right, y, text, &left);
        y += step;
    }
    if (s_messageUntil && TimerRefHR < s_messageUntil) {
        Line(right, y, s_message, &left);
        y += step;
    }
    BoxMovingAdd(left - 4, top - 4, right + 6, y + 6);

    // the count-down in the middle of the screen: 3, 2, 1 a second each, then GO for a second
    const char *count = NULL;
    if (s_phase == PHASE_GRID && now >= s_goAt - 3000) count = now < s_goAt - 2000 ? "3" : now < s_goAt - 1000 ? "2" : "1";
    else if (s_nbSpots && s_phase == PHASE_RACE && s_goAt && now < s_goAt + 1000) count = "GO!";
    if (count) {
        const S32 k = (S32)ModeDesiredY / 48 > 3 ? (S32)ModeDesiredY / 48 : 3;
        BigText((S32)ModeDesiredX / 2, (S32)ModeDesiredY / 3, count, k, count[0] == 'G' ? RACE_GREEN : RACE_RED);
    }

    RestoreClipWindow();
    UnsetClip();
    PtrFont = font;
    InterLeave = leave;
    InterSpace = space;
}
