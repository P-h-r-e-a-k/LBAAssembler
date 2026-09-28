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
 *   - a small display of the gear, the speed, the lap times, the checkpoints and the race position.
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
 *     opponent2_path=...  opponent2_grid=...  opponent2_pace=...  opponent2_actor=...   the second opponent, and so on up to opponent4
 */
#include "C_EXTERN.H"
#include "COMMON.H"
#include "INPUT.H"
#include "RACEMOD.H"

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
} T_RACE_OPPONENT;
static T_RACE_OPPONENT s_opp[RACE_MAX_OPPONENTS];
static int s_oppStarted = 0;               // the race has started (the player drove off)
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
        } else {
            const char *rest;
            T_RACE_OPPONENT *o = OpponentKey(key, &rest);
            if (!o) continue;
            if (!strcmp(rest, "path")) LoadPath(o, v);
            else if (!strcmp(rest, "grid")) o->Grid = atoi(v);
            else if (!strcmp(rest, "pace")) o->Pace = atoi(v);
            else if (!strcmp(rest, "top")) o->Top = atoi(v);
            else if (!strcmp(rest, "grip")) o->Grip = atoi(v);
            else if (!strcmp(rest, "catchup")) o->Catchup = atoi(v);
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
    for (int n = 0; n < RACE_MAX_OPPONENTS; n++) {
        T_RACE_OPPONENT *o = &s_opp[n];
        if (o->Pace < 10) o->Pace = 10;
        if (!o->Count) continue;
        PlanSpeeds(o);
        // the grid: a few points before the start line
        const int grid = o->Grid < 0 ? 0 : o->Grid >= o->Count ? o->Count - 1 : o->Grid;
        o->S = grid ? o->Cum[o->Count - grid] : 0;
        o->Laps = grid ? 0 : 1;
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
    // (the previous frame's position; forgotten when the car changes cube, where the coordinates jump)
    static S32 px = 0, pz = 0, pcx = -1, pcz = -1;
    if (s_wantFlip) { FirstTime = AFF_ALL_FLIP; s_wantFlip = 0; }
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

void RaceMod_Frame(void) {
    if (!RaceMod_Active() || !Opponents()) return;
    const S32 now = TimerRefHR;
    S32 dt = s_oppLast ? now - s_oppLast : 0;
    s_oppLast = now;
    if (dt < 0) dt = 0;
    if (dt > 100) dt = 100;
    const int driving = Comportement == C_BUGGY;
    // the race starts when the player first drives off; the opponents wait while he is out of his car
    if (!s_oppStarted && driving && (Input & I_UP)) s_oppStarted = 1;
    const int moving = s_oppStarted && driving;
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
            const double before = o->S;
            o->S += v * pace / 100.0 * dt / 1000.0;
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
        T_OBJET *car = &ListObjet[index];
        car->Obj.X = (S32)(x - CurrentCubeX * 32768.0);
        car->Obj.Z = (S32)(z - CurrentCubeY * 32768.0);
        car->Obj.Y = (S32)y;
        S32 beta = (S32)(atan2(dx, dz) * 4096.0 / (2 * 3.14159265358979)) & 4095;
        car->Obj.Beta = beta;
        ClearRealAngle(car);
        // the driving animation while it moves (its steps are undone every frame: the position is set here)
        const U16 anim = moving ? 1 : 0;
        if (car->GenAnim != anim) InitAnim(anim, ANIM_REPEAT, (U8)index);
        if (moving) s_wantFlip = 1;
    }
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
    if (s_car.HasLine) {
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
    if (Opponents() && s_opp[0].Count) {
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

    RestoreClipWindow();
    UnsetClip();
    PtrFont = font;
    InterLeave = leave;
    InterSpace = space;
}
