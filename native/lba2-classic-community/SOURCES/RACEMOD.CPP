/* RACEMOD: the engine's race-track options, for the race-track mods the level editor builds (LBA Assembler, Tools > LBA2: Desert island
 * race track).
 *
 * Off unless the environment names a car setup file (LBA2_RACETRACK_FILE). The editor sets it only when the game folder it plays has a race
 * track built, so with any other game the engine is unchanged. With it on:
 *   - the buggy's body follows the decor floor that carries it (a bridge deck) instead of the ground under it (BUGGY.CPP BuggyFloorY);
 *   - the car handles as the file says: a gearbox (X shifts up, Z down, or automatic), each gear with its own top speed, the acceleration
 *     scaled to the gear (a low gear pulls harder), braking, coasting, the speed backwards and the steering;
 *   - a small display of the gear, the speed and the lap times, the laps counted at a start line the file gives.
 *
 * The file is "key=value" lines ('#' starts a comment); anything missing keeps the original car's value. Speeds are world units per second
 * (512 to a cell), rates the speed gained or lost per millisecond (decimals allowed), the steering in 4096ths of a turn per second:
 *     gears=5
 *     gear1=1500  gear2=2300  ...          top speed of each gear
 *     accel=4  brake=12  coast=7  reverse=2000  steer=1024  automatic=0  hud=1
 *     startline=<cube x> <cube z> <x0> <z0> <x1> <z1> <dir x> <dir z>     cube-local ends, and the way a lap crosses it
 */
#include "C_EXTERN.H"
#include "COMMON.H"
#include "RACEMOD.H"

#include <SVGA/CLIP.H>
#include <SVGA/FONT.H>
#include <SVGA/SCREEN.H>
#include <SYSTEM/KEYBOARD.H>
#include <SYSTEM/KEYBOARD_KEYS.H>
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

static void Defaults(T_RACE_CAR *c) {
    memset(c, 0, sizeof(*c));
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

static void Load(const char *path) {
    FILE *f = fopen(path, "r");
    if (!f) return;
    char line[256];
    while (fgets(line, sizeof(line), f)) {
        char *hash = strchr(line, '#');
        if (hash) *hash = 0;
        char *eq = strchr(line, '=');
        if (!eq) continue;
        *eq = 0;
        char key[64];
        if (sscanf(line, " %63s", key) != 1) continue;
        const char *v = eq + 1;
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
            if (sscanf(v, "%d %d %d %d %d %d %d %d", &s_car.LineCubeX, &s_car.LineCubeZ, &s_car.LineX0, &s_car.LineZ0, &s_car.LineX1, &s_car.LineZ1,
                       &s_car.LineDirX, &s_car.LineDirZ) == 8)
                s_car.HasLine = 1;
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

// Whether the move (x0,z0)->(x1,z1) crosses the segment (ax,az)-(bx,bz).
static int Crosses(double x0, double z0, double x1, double z1, double ax, double az, double bx, double bz) {
    const double d1 = (bx - ax) * (z0 - az) - (bz - az) * (x0 - ax);
    const double d2 = (bx - ax) * (z1 - az) - (bz - az) * (x1 - ax);
    if ((d1 > 0) == (d2 > 0) || d1 == d2) return 0;
    const double e1 = (x1 - x0) * (az - z0) - (z1 - z0) * (ax - x0);
    const double e2 = (x1 - x0) * (bz - z0) - (z1 - z0) * (bx - x0);
    return (e1 > 0) != (e2 > 0);
}

void RaceMod_Lap(S32 x, S32 z) {
    // (the previous frame's position; forgotten when the car changes cube, where the coordinates jump)
    static S32 px = 0, pz = 0, pcx = -1, pcz = -1;
    if (!s_active || !s_car.HasLine || CubeMode != CUBE_EXTERIEUR) { pcx = -1; return; }
    const S32 x0 = px, z0 = pz;
    const int same = pcx == CurrentCubeX && pcz == CurrentCubeY;
    px = x; pz = z; pcx = CurrentCubeX; pcz = CurrentCubeY;
    if (!same || CurrentCubeX != s_car.LineCubeX || CurrentCubeY != s_car.LineCubeZ) return;
    if ((double)(x - x0) * s_car.LineDirX + (double)(z - z0) * s_car.LineDirZ <= 0) return;
    if (!Crosses(x0, z0, x, z, s_car.LineX0, s_car.LineZ0, s_car.LineX1, s_car.LineZ1)) return;
    const S32 now = TimerRefHR;
    if (s_laps > 0) {
        s_lastLap = now - s_lapStart;
        if (!s_bestLap || s_lastLap < s_bestLap) s_bestLap = s_lastLap;
    }
    s_laps++;
    s_lapStart = now;
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
        if (s_lastLap) {
            Time(a, sizeof(a), s_lastLap);
            Time(b, sizeof(b), s_bestLap);
            snprintf(text, sizeof(text), "Last %s  Best %s", a, b);
            Line(right, y, text, &left);
            y += step;
        }
    }
    BoxMovingAdd(left - 4, top - 4, right + 6, y + 6);

    RestoreClipWindow();
    UnsetClip();
    PtrFont = font;
    InterLeave = leave;
    InterSpace = space;
}
