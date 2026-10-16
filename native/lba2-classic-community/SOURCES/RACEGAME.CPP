/* RACEGAME: the race game -- LBA Assembler's race tracks played as a game of their own, the standalone Little Big Halloween Racing
 * Adventure (2026-10-16), on when RACEGAME.TXT is beside the game's data (its installer writes it). The game's main menu is then the race
 * game's (GAMEMENU.CPP MainGameMenu asks RaceGame_Menu), in the retail game's look:
 *     Resume (with a race on)   Story Mode   Track select   Specials   Best times   Options   Quit
 * Story Mode and Track select are greyed and passed over until the file opens them. Specials: the special tracks, then a race (the line-up,
 * the power-ups) or a time trial (Twinsen alone, a can of Gazogem for every lap), then the car, its body turning on the page; then the race
 * -- the special's race file with the car's and the race game's keys (RACEMOD.CPP drive_as=, start_scene=, start_in_car=, race_laps=,
 * end_to_menu=, trial=) written to RACE_NOW.TXT in the user's folder and loaded (RaceMod_LoadFile), and a new game at the start line, no
 * introduction. A time trial's time goes to the track's best times, its ten fastest, kept in BESTTIMES.TXT in the user's folder; Best
 * times shows them.
 *
 * RACEGAME.TXT, "key=value" lines ('#' starts a comment), UTF-8:
 *     title=<the game's name>
 *     story=0|1  tracks=0|1        Story Mode, Track select open (0: greyed)
 *     laps=<n>                     a race's laps (3)
 *     end_to_menu=<ms>             a finished race back to the menu this long after its end (8000)
 *     special=<name>|<race file>|<time trial file>|<start scene>     files beside the game's data (and so are those they name)
 *     car=<name>|<drive_as value: BODY.HQR body, animation, shrunk body>
 * BESTTIMES.TXT: "<track>|<time ms>|<best lap ms>|<car>|<date>" lines, the fastest first (other tracks' lines are kept as they are).
 */
#include "C_EXTERN.H"
#include "DIRECTORIES.H"
#include "INPUT.H"
#include "JOYSTICK.H"
#include "RACEGAME.H"
#include "UI_LAYOUT.H"

#include <SVGA/SCREEN.H>
#include <SYSTEM/KEYBOARD.H>
#include <SYSTEM/KEYBOARD_KEYS.H>
#include <SYSTEM/LOG.H>
#include <SYSTEM/MOUSE.H>
#include <SYSTEM/STRING.H>

#include <cctype>
#include <cstdio>
#include <cstring>
#include <ctime>

extern void RaceMod_DrawBody(S32 x0, S32 y0, S32 x1, S32 y1, S32 body, S32 beta, S32 measure);   // RACEMOD.CPP

#define RG_MAX_SPECIALS 8
#define RG_MAX_CARS 16
#define RG_MAX_TIMES 10
#define RG_OTHER_SIZE 16384

// the menu's rows (BuildCustomMenuText labels them: RaceGame_MenuText)
#define RG_ID_STORY 2300
#define RG_ID_TRACKS 2301
#define RG_ID_SPECIALS 2302
#define RG_ID_TIMES 2303
#define RG_ID_RACE 2310
#define RG_ID_TRIAL 2311
#define RG_ID_SPECIAL 2320                  // + the special's index
#define RG_ID_BACK 26                       // the game's own "Back"
#define RG_ID_RESUME 70
#define RG_ID_OPTIONS 74
#define RG_ID_QUIT 75

typedef struct {
    char Name[96], Race[160], Trial[160];
    S32 Scene;
} T_RG_SPECIAL;

typedef struct {
    char Name[96], Drive[64];
    S32 Body;
} T_RG_CAR;

typedef struct {
    S32 Time, Lap;
    char Car[96], Date[16];
} T_RG_TIME;

static int s_read = 0, s_on = 0;
static char s_title[96] = "";
static int s_story = 0, s_tracks = 0, s_laps = 3;
static S32 s_endMs = 8000;
static T_RG_SPECIAL s_sp[RG_MAX_SPECIALS];
static int s_nbSp = 0;
static T_RG_CAR s_cars[RG_MAX_CARS];
static int s_nbCars = 0;
static int s_car = 0;                       // the car chosen last
static int s_lastSp = 0, s_lastTrial = 0;

static T_RG_TIME s_times[RG_MAX_SPECIALS][RG_MAX_TIMES];
static int s_nbTimes[RG_MAX_SPECIALS];
static char s_other[RG_OTHER_SIZE];         // BESTTIMES.TXT's lines of other tracks
static int s_timesRead = 0;

// the race on
static int s_raceSp = -1, s_raceTrial = 0, s_raceCar = -1, s_raceOver = 0;

static U16 s_mainMenu[4 + 2 * 8];
static U16 s_spMenu[4 + 2 * (1 + RG_MAX_SPECIALS)];
static U16 s_modeMenu[4 + 2 * 3];

// ---- RACEGAME.TXT ------------------------------------------------------------------------------------------------------------------

static void Trim(char *s) {
    char *end = s + strlen(s);
    while (end > s && (end[-1] == '\n' || end[-1] == '\r' || end[-1] == ' ' || end[-1] == '\t')) *--end = 0;
}

// The next "|" field of `p` into `out` (the rest of the line for the last).
static const char *Field(const char *p, char *out, size_t size) {
    size_t n = 0;
    while (*p && *p != '|') {
        if (n + 1 < size) out[n++] = *p;
        p++;
    }
    out[n] = 0;
    while (n > 0 && out[n - 1] == ' ') out[--n] = 0;
    return *p == '|' ? p + 1 : p;
}

static void Read(void) {
    if (s_read) return;
    s_read = 1;
    char path[ADELINE_MAX_PATH];
    GetResPath(path, ADELINE_MAX_PATH, "RACEGAME.TXT");
    FILE *f = fopen(path, "r");
    if (!f) return;
    char line[512];
    while (fgets(line, sizeof(line), f)) {
        char *hash = strchr(line, '#');
        if (hash) *hash = 0;
        Trim(line);
        char *eq = strchr(line, '=');
        if (!eq) continue;
        *eq = 0;
        const char *key = line, *v = eq + 1;
        while (*v == ' ') v++;
        if (!strcmp(key, "title")) snprintf(s_title, sizeof(s_title), "%s", v);
        else if (!strcmp(key, "story")) s_story = atoi(v);
        else if (!strcmp(key, "tracks")) s_tracks = atoi(v);
        else if (!strcmp(key, "laps")) s_laps = atoi(v) > 0 ? atoi(v) : 3;
        else if (!strcmp(key, "end_to_menu")) s_endMs = atoi(v);
        else if (!strcmp(key, "special") && s_nbSp < RG_MAX_SPECIALS) {
            T_RG_SPECIAL *sp = &s_sp[s_nbSp];
            char scene[16];
            v = Field(v, sp->Name, sizeof(sp->Name));
            v = Field(v, sp->Race, sizeof(sp->Race));
            v = Field(v, sp->Trial, sizeof(sp->Trial));
            Field(v, scene, sizeof(scene));
            sp->Scene = scene[0] ? atoi(scene) : -1;
            if (sp->Name[0] && sp->Race[0]) s_nbSp++;
        } else if (!strcmp(key, "car") && s_nbCars < RG_MAX_CARS) {
            T_RG_CAR *c = &s_cars[s_nbCars];
            v = Field(v, c->Name, sizeof(c->Name));
            Field(v, c->Drive, sizeof(c->Drive));
            c->Body = c->Drive[0] ? atoi(c->Drive) : -1;
            if (c->Name[0] && c->Drive[0]) s_nbCars++;
        }
    }
    fclose(f);
    s_on = 1;
    Log_Info("[racegame] %s: %d special track(s), %d cars%s%s", s_title[0] ? s_title : "the race game", s_nbSp, s_nbCars,
             s_story ? "" : ", Story Mode greyed", s_tracks ? "" : ", Track select greyed");
}

S32 RaceGame_On(void) {
    Read();
    return s_on;
}

// ---- the best times ----------------------------------------------------------------------------------------------------------------

static void TimesPath(char *path) { GetCfgPath(path, ADELINE_MAX_PATH, "BESTTIMES.TXT"); }

static void ReadTimes(void) {
    if (s_timesRead) return;
    s_timesRead = 1;
    memset(s_nbTimes, 0, sizeof(s_nbTimes));
    s_other[0] = 0;
    char path[ADELINE_MAX_PATH];
    TimesPath(path);
    FILE *f = fopen(path, "r");
    if (!f) return;
    char line[512];
    while (fgets(line, sizeof(line), f)) {
        Trim(line);
        if (!line[0]) continue;
        char track[96], ms[16], lap[16];
        T_RG_TIME t;
        const char *p = Field(line, track, sizeof(track));
        p = Field(p, ms, sizeof(ms));
        p = Field(p, lap, sizeof(lap));
        p = Field(p, t.Car, sizeof(t.Car));
        Field(p, t.Date, sizeof(t.Date));
        t.Time = atoi(ms);
        t.Lap = atoi(lap);
        int sp = -1;
        for (int i = 0; i < s_nbSp && sp < 0; i++) if (!strcmp(s_sp[i].Name, track)) sp = i;
        if (sp < 0) {
            if (strlen(s_other) + strlen(line) + 2 < sizeof(s_other)) { strcat(s_other, line); strcat(s_other, "\n"); }
            continue;
        }
        if (t.Time <= 0 || s_nbTimes[sp] >= RG_MAX_TIMES) continue;
        int k = s_nbTimes[sp]++;
        for (; k > 0 && s_times[sp][k - 1].Time > t.Time; k--) s_times[sp][k] = s_times[sp][k - 1];
        s_times[sp][k] = t;
    }
    fclose(f);
}

static void WriteTimes(void) {
    char path[ADELINE_MAX_PATH];
    TimesPath(path);
    FILE *f = fopen(path, "w");
    if (!f) { Log_Warn("[racegame] the best times could not be written to %s", path); return; }
    for (int sp = 0; sp < s_nbSp; sp++)
        for (int k = 0; k < s_nbTimes[sp]; k++) {
            const T_RG_TIME *t = &s_times[sp][k];
            fprintf(f, "%s|%d|%d|%s|%s\n", s_sp[sp].Name, (int)t->Time, (int)t->Lap, t->Car, t->Date);
        }
    fputs(s_other, f);
    fclose(f);
}

static void TimeText(char *out, size_t size, S32 ms) {
    if (ms < 0) ms = 0;
    snprintf(out, size, "%d:%02d.%02d", (int)(ms / 60000), (int)(ms / 1000 % 60), (int)(ms / 10 % 100));
}

// RACEMOD.CPP: a race is over. A time trial's time into its track's best times: the rank it took (1-10), 0 for none.
S32 RaceGame_Finished(S32 trial, S32 total, S32 bestLap, S32 place, S32 racers) {
    if (!RaceGame_On()) return 0;
    s_raceOver = 1;
    char t[24];
    TimeText(t, sizeof(t), total);
    Log_Info("[racegame] %s over: %s, %d of %d", trial ? "the time trial" : "the race", t, (int)place, (int)racers);
    if (!trial || s_raceSp < 0 || s_raceSp >= s_nbSp || total <= 0) return 0;
    ReadTimes();
    const int sp = s_raceSp;
    int k = 0;
    while (k < s_nbTimes[sp] && s_times[sp][k].Time <= total) k++;
    if (k >= RG_MAX_TIMES) return 0;
    if (s_nbTimes[sp] < RG_MAX_TIMES) s_nbTimes[sp]++;
    for (int j = s_nbTimes[sp] - 1; j > k; j--) s_times[sp][j] = s_times[sp][j - 1];
    T_RG_TIME *e = &s_times[sp][k];
    e->Time = total;
    e->Lap = bestLap;
    snprintf(e->Car, sizeof(e->Car), "%s", s_raceCar >= 0 && s_raceCar < s_nbCars ? s_cars[s_raceCar].Name : "");
    const time_t now = time(NULL);
    const struct tm *tm = localtime(&now);
    if (tm) strftime(e->Date, sizeof(e->Date), "%Y-%m-%d", tm);
    else e->Date[0] = 0;
    WriteTimes();
    Log_Info("[racegame] best times of %s: %d%s", s_sp[sp].Name, k + 1, k == 0 ? " (the best)" : "");
    return k + 1;
}

// ---- the race ----------------------------------------------------------------------------------------------------------------------

static int Absolute(const char *p) { return p[0] == '/' || p[0] == '\\' || (isalpha((unsigned char)p[0]) && p[1] == ':'); }

// The race: the special's race file (or its time trial's), the files it names found beside the game's data, then the car and the race
// game's keys -- written to RACE_NOW.TXT in the user's folder and loaded in place of any other.
static int StartRace(int sp, int trial, int car) {
    const T_RG_SPECIAL *s = &s_sp[sp];
    char src[ADELINE_MAX_PATH], out[ADELINE_MAX_PATH];
    GetResPath(src, ADELINE_MAX_PATH, trial && s->Trial[0] ? s->Trial : s->Race);
    GetCfgPath(out, ADELINE_MAX_PATH, "RACE_NOW.TXT");
    FILE *in = fopen(src, "r");
    if (!in) { Log_Warn("[racegame] %s's race file %s could not be read", s->Name, src); return 0; }
    FILE *f = fopen(out, "w");
    if (!f) { fclose(in); Log_Warn("[racegame] %s could not be written", out); return 0; }
    fprintf(f, "# %s: %s, %s (RACEGAME.CPP)\n", s_title, s->Name, trial ? "a time trial" : "a race");
    char line[1024];
    while (fgets(line, sizeof(line), in)) {
        char *eq = strchr(line, '=');
        if (eq && line[0] != '#') {
            *eq = 0;
            const size_t n = strlen(line);
            char *v = eq + 1;
            while (*v == ' ') v++;
            Trim(v);
            if (((n > 5 && !strcmp(line + n - 5, "_path")) || !strcmp(line, "raised")) && v[0] && !Absolute(v)) {
                char full[ADELINE_MAX_PATH];
                GetResPath(full, ADELINE_MAX_PATH, v);
                fprintf(f, "%s=%s\n", line, full);
            } else fprintf(f, "%s=%s\n", line, v);
        } else fputs(line, f);
    }
    fclose(in);
    if (car >= 0 && car < s_nbCars) fprintf(f, "# %s\ndrive_as=%s\n", s_cars[car].Name, s_cars[car].Drive);
    if (s->Scene >= 0) fprintf(f, "start_scene=%d\n", (int)s->Scene);
    fprintf(f, "start_in_car=1\nrace_laps=%d\nend_to_menu=%d\n", s_laps, (int)s_endMs);
    if (trial) fprintf(f, "trial=1\n");
    fclose(f);
    s_raceSp = sp; s_raceTrial = trial; s_raceCar = car; s_raceOver = 0;
    Log_Info("[racegame] %s: %s with %s", s->Name, trial ? "a time trial" : "a race", car >= 0 && car < s_nbCars ? s_cars[car].Name : "Twinsen's car");
    RaceMod_LoadFile(out);
    return 1;
}

// ---- the menu ----------------------------------------------------------------------------------------------------------------------

S32 RaceGame_RowDisabled(S32 num) {
    if (!s_on) return 0;
    return (num == RG_ID_STORY && !s_story) || (num == RG_ID_TRACKS && !s_tracks);
}

static void Label(char *string, const char *utf8) { CopyUtf8ToCp850(string, 256, utf8); }

S32 RaceGame_MenuText(S32 num, char *string) {
    if (!s_on || num < RG_ID_STORY || num >= RG_ID_STORY + 100) return 0;
    switch (num) {
    case RG_ID_STORY: Label(string, "Story Mode"); return 1;
    case RG_ID_TRACKS: Label(string, "Track select"); return 1;
    case RG_ID_SPECIALS: Label(string, "Specials"); return 1;
    case RG_ID_TIMES: Label(string, "Best times"); return 1;
    case RG_ID_RACE: Label(string, "Race"); return 1;
    case RG_ID_TRIAL: Label(string, "Time trial"); return 1;
    }
    if (num >= RG_ID_SPECIAL && num < RG_ID_SPECIAL + s_nbSp) { Label(string, s_sp[num - RG_ID_SPECIAL].Name); return 1; }
    string[0] = 0;
    return 1;
}

static void SetRow(U16 *menu, S32 row, U16 id) {
    menu[4 + row * 2 + 0] = 0;
    menu[4 + row * 2 + 1] = id;
}

// A page of the menu: the menu's backdrop put back (the menu's picture, or the scene shaded behind it), a header line over the rows.
static void Page(const char *header, S32 y) {
    CopyScreen(Screen, Log);
    BoxStaticAdd(0, 0, (S32)ModeDesiredX - 1, (S32)ModeDesiredY - 1);
    if (header && header[0]) {
        char text[256];
        Label(text, header);
        DrawSingleString(UI_CENTER_X, y + UI_VCENTER_OFS, text);
    }
}

// A line of text centred on x, with the menus' dark shadow.
static void Text(S32 x, S32 y, const char *utf8, S32 colour) {
    char text[256];
    Label(text, utf8);
    const S32 w = SizeFont(text);
    ColorFont(BLACK);
    Font(x - w / 2 + 3, y + 3, text);
    ColorFont(colour);
    Font(x - w / 2, y, text);
}

// A key that steps a page's choice (the arrows, the pad's d-pad or stick, edge-only as DoGameMenu has them): -1 left, 1 right, 0 none.
static S32 Step(S32 *padDir) {
    S32 key = MyKey;
    if (!key) {
        const S32 raw = JoystickMenuNavOnly();
        if (raw != *padDir) {
            *padDir = raw;
            if (raw == K_LEFT) key = K_GRAY_LEFT;
            else if (raw == K_RIGHT) key = K_GRAY_RIGHT;
        }
    }
    if (Input & I_LEFT || key == K_GRAY_LEFT) { InitWaitNoInput(I_LEFT); InitWaitNoKey(); return -1; }
    if (Input & I_RIGHT || key == K_GRAY_RIGHT) { InitWaitNoInput(I_RIGHT); InitWaitNoKey(); return 1; }
    return 0;
}

// A mouse button let go this pass (the menus' mouse on): 1 the left, 2 the right; where, in (*x, *y). s_clickWas primed as a page opens.
static S32 s_clickWas = 0;
static int Clicked(S32 *x, S32 *y) {
    const S32 now = Click & 3;
    const S32 released = s_clickWas & ~now;
    s_clickWas = now;
    if (!released || !FlagMenuMouse) return 0;
    *x = (S32)MouseX;
    *y = (S32)MouseY;
    return released & 1 ? 1 : 2;
}

static int Confirmed(void) { return Input & I_RETURN || Input & I_ACTION_M || MyKey == K_ENTER || MyKey == K_NUMPAD_ENTER || MyKey == K_SPACE; }
static int Escaped(void) { return MyKey == K_ESC || Input & I_MENUS; }

#define RG_ORANGE 243                       // the palette's fire orange, for the pages' headers
#define RG_GREY (3 * 16 + 11)               // the greyed rows' colour (GAMEMENU.CPP COUL_TEXT_MENU_DISABLE)

// The car: its body turning on the page, its name under it; left and right go through the cars, Enter takes it, Esc goes back.
// The car taken (its index), or -1.
static int CarPage(const char *what) {
    if (s_nbCars <= 0) return -1;
    if (s_car < 0 || s_car >= s_nbCars) s_car = 0;
    Page(NULL, 0);
    BoxUpdate();
    ManageTime();
    InitWaitNoKey();
    InitWaitNoInput(I_JOY | I_FIRE | I_MENUS | I_ACTION);
    S32 padDir = JoystickMenuNavOnly();
    AsciiMode = TRUE;
    const S32 cx = UI_CENTER_X, oy = UI_VCENTER_OFS;
    const S32 x0 = cx - 230, x1 = cx + 230, y0 = 130 + oy, y1 = 330 + oy;
    int chosen = -2;
    S32 start = TimerRefHR;
    s_clickWas = Click & 3;
    while (chosen == -2) {
        ManageTime();
        MyGetInput();
        S32 step = Step(&padDir);
        // (the mouse: a click on the box's left or right quarter the car before or after, in its middle this one; the right button back)
        S32 mx, my;
        const int click = Clicked(&mx, &my);
        if (click == 2) chosen = -1;
        else if (click == 1 && my >= y0 && my <= y1 && mx >= x0 && mx <= x1) {
            if (mx < x0 + (x1 - x0) / 4) step = -1;
            else if (mx > x1 - (x1 - x0) / 4) step = 1;
            else { WaitNoInput(); chosen = s_car; }
        }
        if (step) { s_car = (s_car + step + s_nbCars) % s_nbCars; start = TimerRefHR; }
        // (a car not seen yet measured first, under the backdrop drawn next: RaceMod_DrawBody)
        RaceMod_DrawBody(x0 + 2, y0 + 2, x1 - 2, y1 - 2, s_cars[s_car].Body, 0, 1);
        // the page: the backdrop, the header, the box with the car, its name and the keys
        const S32 top = y0 - 36, bottom = y1 + 120 < (S32)ModeDesiredY ? y1 + 120 : (S32)ModeDesiredY - 1;
        CopyBlock(0, top, (S32)ModeDesiredX - 1, bottom, Screen, 0, top, Log);
        Text(cx, y0 - 34, "Choose your car", RG_ORANGE);
        ShadeBoxBlk(x0, y0, x1, y1, MENU_SHADE_LVL);
        RaceMod_DrawBody(x0 + 2, y0 + 2, x1 - 2, y1 - 2, s_cars[s_car].Body, (S32)(((TimerRefHR - start) / 3 + 3072) & 4095), 0);
        DrawCadre(x0, y0, x1, y1, ALL_ANGLES);
        char name[160];
        snprintf(name, sizeof(name), "%s", s_cars[s_car].Name);
        Text(cx, y1 + 10, name, LBAWHITE);
        char count[64];
        snprintf(count, sizeof(count), "Car %d of %d, for %s", s_car + 1, s_nbCars, what);
        Text(cx, y1 + 46, count, RG_GREY);
        Text(cx, y1 + 82, "Left, right: car    Enter: race    Esc: back", RG_GREY);
        BoxStaticAdd(0, top, (S32)ModeDesiredX - 1, bottom);
        BoxUpdate();
        if (chosen != -2) break;
        if (Confirmed()) { WaitNoInput(); chosen = s_car; }
        else if (Escaped()) chosen = -1;
    }
    AsciiMode = FALSE;
    Page(NULL, 0);
    return chosen;
}

// Best times: a track's ten fastest time trials (left and right go through the tracks); Esc, Enter or a click goes back.
static void TimesPage(void) {
    ReadTimes();
    Page(NULL, 0);
    BoxUpdate();
    ManageTime();
    InitWaitNoKey();
    InitWaitNoInput(I_JOY | I_FIRE | I_MENUS | I_ACTION);
    S32 padDir = JoystickMenuNavOnly();
    AsciiMode = TRUE;
    int sp = s_lastSp >= 0 && s_lastSp < s_nbSp ? s_lastSp : 0, flag = 1, done = 0;
    const S32 cx = UI_CENTER_X, oy = UI_VCENTER_OFS;
    const S32 x0 = cx - 280, x1 = cx + 280, y0 = 60 + oy, y1 = 440 + oy;
    s_clickWas = Click & 3;
    while (!done) {
        ManageTime();
        MyGetInput();
        S32 mx, my;
        if (Clicked(&mx, &my)) done = 1;
        const S32 step = s_nbSp > 1 ? Step(&padDir) : 0;
        if (step) { sp = (sp + step + s_nbSp) % s_nbSp; flag = 1; }
        if (flag) {
            flag = 0;
            Page(NULL, 0);
            ShadeBoxBlk(x0, y0, x1, y1, MENU_SHADE_LVL);
            DrawCadre(x0, y0, x1, y1, ALL_ANGLES);
            Text(cx, y0 + 12, "Best times", RG_ORANGE);
            Text(cx, y0 + 48, s_nbSp ? s_sp[sp].Name : "No tracks", LBAWHITE);
            if (sp < s_nbSp && !s_nbTimes[sp]) Text(cx, y0 + 150, "No times yet: drive a time trial!", RG_GREY);
            for (int k = 0; sp < s_nbSp && k < s_nbTimes[sp]; k++) {
                const T_RG_TIME *t = &s_times[sp][k];
                char time[24], line[200];
                TimeText(time, sizeof(time), t->Time);
                snprintf(line, sizeof(line), "%2d.  %s   %s", k + 1, time, t->Car);
                char text[256];
                Label(text, line);
                const S32 y = y0 + 86 + k * 26;
                ColorFont(BLACK);
                Font(x0 + 34 + 3, y + 3, text);
                ColorFont(k == 0 ? RG_ORANGE : LBAWHITE);
                Font(x0 + 34, y, text);
            }
            Text(cx, y1 - 36, s_nbSp > 1 ? "Left, right: track    Esc: back" : "Esc: back", RG_GREY);
            BoxStaticAdd(0, 0, (S32)ModeDesiredX - 1, (S32)ModeDesiredY - 1);
        }
        // (a frame every pass, drawn or not -- a box of it put up again: a present happens only for one -- the mouse's pointer goes with
        // it, and the engine's control socket is served at a present)
        BoxStaticAdd(x0, y1 - 40, x1, y1 - 8);
        BoxUpdate();
        if (Confirmed()) { WaitNoInput(); done = 1; }
        else if (Escaped()) done = 1;
    }
    AsciiMode = FALSE;
    Page(NULL, 0);
}

// A special's race or time trial; then the car. Non-zero: the race is set up.
static int ModePage(int sp) {
    s_modeMenu[0] = (U16)(s_lastTrial ? 2 : 1);
    s_modeMenu[1] = 3;
    s_modeMenu[2] = 280;
    s_modeMenu[3] = 0;
    SetRow(s_modeMenu, 0, RG_ID_BACK);
    SetRow(s_modeMenu, 1, RG_ID_RACE);
    SetRow(s_modeMenu, 2, RG_ID_TRIAL);
    for (;;) {
        Page(s_sp[sp].Name, 128);
        const S32 select = DoGameMenu(s_modeMenu);
        if (select == RG_ID_BACK || select == 1000) { Page(NULL, 0); return 0; }
        if (select != RG_ID_RACE && select != RG_ID_TRIAL) continue;
        const int trial = select == RG_ID_TRIAL;
        s_lastTrial = trial;
        const int car = CarPage(trial ? "a time trial" : "a race");
        if (car >= 0 && StartRace(sp, trial, car)) return 1;
    }
}

static int SpecialsPage(void) {
    if (s_nbSp <= 0) return 0;
    s_spMenu[0] = (U16)(1 + (s_lastSp >= 0 && s_lastSp < s_nbSp ? s_lastSp : 0));
    s_spMenu[1] = (U16)(1 + s_nbSp);
    s_spMenu[2] = 280;
    s_spMenu[3] = 0;
    SetRow(s_spMenu, 0, RG_ID_BACK);
    for (int i = 0; i < s_nbSp; i++) SetRow(s_spMenu, 1 + i, (U16)(RG_ID_SPECIAL + i));
    for (;;) {
        Page("Specials", 280 - (56 * (1 + s_nbSp)) / 2 - 50);
        const S32 select = DoGameMenu(s_spMenu);
        if (select == RG_ID_BACK || select == 1000) { Page(NULL, 0); return 0; }
        if (select < RG_ID_SPECIAL || select >= RG_ID_SPECIAL + s_nbSp) continue;
        s_lastSp = select - RG_ID_SPECIAL;
        if (ModePage(s_lastSp)) return 1;
    }
}

S32 RaceGame_Menu(S32 inGame) {
    Read();
    const int resume = inGame && !s_raceOver;
    S32 nb = 0;
    if (resume) SetRow(s_mainMenu, nb++, RG_ID_RESUME);
    SetRow(s_mainMenu, nb++, RG_ID_STORY);
    SetRow(s_mainMenu, nb++, RG_ID_TRACKS);
    const S32 specials = nb;
    SetRow(s_mainMenu, nb++, RG_ID_SPECIALS);
    SetRow(s_mainMenu, nb++, RG_ID_TIMES);
    SetRow(s_mainMenu, nb++, RG_ID_OPTIONS);
    SetRow(s_mainMenu, nb++, RG_ID_QUIT);
    s_mainMenu[0] = (U16)(resume ? 0 : specials);
    s_mainMenu[1] = (U16)nb;
    s_mainMenu[2] = (U16)(nb > 6 ? 290 : 300);
    s_mainMenu[3] = 0;
    for (;;) {
        const S32 select = DoGameMenu(s_mainMenu);
        switch (select) {
        case 1000:
            if (resume) return 1000;
            break;
        case RG_ID_RESUME:
        case RG_ID_OPTIONS:
        case RG_ID_QUIT:
            return select;
        case RG_ID_SPECIALS:
            if (SpecialsPage()) return RACEGAME_START;
            break;
        case RG_ID_TIMES:
            TimesPage();
            break;
        }
        // (the main menu again: its page put back; DoGameMenu draws its rows)
        for (S32 i = 0; i < nb; i++) if (s_mainMenu[4 + 2 * i + 1] == (U16)select) s_mainMenu[0] = (U16)i;
        Page(NULL, 0);
    }
}
