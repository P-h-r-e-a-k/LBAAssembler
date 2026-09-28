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
 *     qualifying=1               a qualifying lap first (0: the grid is formed as soon as the player is in his car, he on pole)
 *     qualifying_seed=<n>        the opponents' qualifying times' randomness (default: the clock)
 *     hide_actor=<scene> <actor> a car in a scene that isn't racing (an opponent the player left out): kept out of sight
 */
#include "C_EXTERN.H"
#include "COMMON.H"
#include "INPUT.H"
#include "RACEMOD.H"

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
} T_RACE_LINE;
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
    return sscanf(v, "%d %d %d %d %d %d %d %d", &l->CubeX, &l->CubeZ, &l->X0, &l->Z0, &l->X1, &l->Z1, &l->DirX, &l->DirZ) == 8;
}

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
static void OnSpot(T_RACE_OPPONENT *o, const T_RACE_SPOT *spot) {
    const double wx = spot->CubeX * 32768.0 + spot->X, wz = spot->CubeZ * 32768.0 + spot->Z;
    int best = 0;
    double bd = 1e30;
    for (int i = 0; i < o->Count; i++) {
        const double d = (o->X[i] - wx) * (o->X[i] - wx) + (o->Z[i] - wz) * (o->Z[i] - wz);
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
            else if (!strcmp(rest, "actor")) {
                int scene, actor;
                if (o->Scenes < RACE_MAX_OPPONENT_SCENES && sscanf(v, "%d %d", &scene, &actor) == 2) {
                    o->Scene[o->Scenes] = scene; o->Actor[o->Scenes] = actor; o->Scenes++;
                }
            }
        }
    }
    fclose(f);
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
        o->QualTime = (S32)(o->PlannedLap * 1000 + (RaceRandom() * 2 - 1) * RACE_QUALIFY_SPREAD);
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
static int CrossLine(const T_RACE_LINE *l, S32 x0, S32 z0, S32 x1, S32 z1) {
    if (CurrentCubeX != l->CubeX || CurrentCubeY != l->CubeZ) return 0;
    if (!Crosses(x0, z0, x1, z1, l->X0, l->Z0, l->X1, l->Z1)) return 0;
    return (double)(x1 - x0) * l->DirX + (double)(z1 - z0) * l->DirZ > 0 ? 1 : -1;
}

void RaceMod_Lap(S32 x, S32 z) {
    // (the previous frame's position; forgotten when the car changes cube, where the coordinates jump, or is put on the grid)
    static S32 px = 0, pz = 0, pcx = -1, pcz = -1;
    if (s_wantFlip) { FirstTime = AFF_ALL_FLIP; s_wantFlip = 0; }
    if (s_forgetLap) { pcx = -1; s_forgetLap = 0; }
    if (!s_active || !s_car.HasLine || CubeMode != CUBE_EXTERIEUR) { pcx = -1; return; }
    const S32 x0 = px, z0 = pz;
    const int same = pcx == CurrentCubeX && pcz == CurrentCubeY;
    px = x; pz = z; pcx = CurrentCubeX; pcz = CurrentCubeY;
    if (!same || (x == x0 && z == z0)) return;

    // the checkpoints: the next one crossed the lap's way moves on; the last one crossed backwards moves back
    if (s_laps > 0)
        for (int i = 0; i < s_nbCheckpoints; i++) {
            const int way = CrossLine(&s_checkpoints[i], x0, z0, x, z);
            if (way > 0 && i == s_next) s_next++;
            else if (way < 0 && i == s_next - 1) { s_next--; Message("Wrong way"); }
        }

    T_RACE_LINE line = {s_car.LineCubeX, s_car.LineCubeZ, s_car.LineX0, s_car.LineZ0, s_car.LineX1, s_car.LineZ1, s_car.LineDirX, s_car.LineDirZ};
    if (CrossLine(&line, x0, z0, x, z) <= 0) return;
    const S32 now = TimerRefHR;
    if (s_laps > 0 && s_next < s_nbCheckpoints) {
        char text[64];
        snprintf(text, sizeof(text), "Missed checkpoint %d: no lap", s_next + 1);
        Message(text);
        return;
    }
    if (s_laps > 0) {
        s_lastLap = now - s_lapStart;
        if (!s_bestLap || s_lastLap < s_bestLap) s_bestLap = s_lastLap;
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

void RaceMod_Frame(void) {
    if (!RaceMod_Active()) return;
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
        const U16 anim = moving ? 1 : 0;
        if (car->GenAnim != anim) InitAnim(anim, ANIM_REPEAT, (U8)index);
        if (moving) s_wantFlip = 1;
    }
}

// The player's car stands on the grid until the count-down's GO (BUGGY.CPP RaceSpeed).
S32 RaceMod_Held(void) {
    return RaceMod_Active() && s_phase == PHASE_GRID;
}

// How far round the race the player is, and an opponent, in world units from the start of lap 1 (negative before it). The player is
// placed on the first opponent's line (the lines run side by side round the same lap).
static double PlayerProgress(void) {
    const T_RACE_OPPONENT *o = &s_opp[0];
    if (!o->Count) return 0;
    const double wx = ListObjet[NUM_PERSO].Obj.X + CurrentCubeX * 32768.0, wz = ListObjet[NUM_PERSO].Obj.Z + CurrentCubeY * 32768.0;
    int best = -1;
    double bd = 1e30;
    if (s_playerIndex >= 0)
        for (int k = -150; k <= 150; k++) {
            const int i = ((s_playerIndex + k) % o->Count + o->Count) % o->Count;
            const double d = (o->X[i] - wx) * (o->X[i] - wx) + (o->Z[i] - wz) * (o->Z[i] - wz);
            if (d < bd) { bd = d; best = i; }
        }
    if (best < 0 || bd > 20.0 * 512 * 20 * 512)
        for (int i = 0; i < o->Count; i++) {
            const double d = (o->X[i] - wx) * (o->X[i] - wx) + (o->Z[i] - wz) * (o->Z[i] - wz);
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
