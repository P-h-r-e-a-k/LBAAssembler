/* RACEGAME: the race game -- LBA Assembler's race tracks played as a game of their own, the standalone Little Big Halloween Racing
 * Adventure (2026-10-16), on when RACEGAME.TXT is beside the game's data (its installer writes it). The game's main menu is then the race
 * game's (GAMEMENU.CPP MainGameMenu asks RaceGame_Menu), in the retail game's look:
 *     Resume (with a race on)   Story Mode   Track select   Specials   Best laps   Options   Quit
 * Story Mode and Track select are greyed and passed over until the file opens them. Specials: the special tracks, then a race (the line-up,
 * the power-ups) or a time trial (Twinsen alone, no power-ups); a race on a track with levels (its race file has
 * difficulty_level= lines: the Temple of Bú's, 2026-10-17) then its difficulty, easy, medium or hard (the last one chosen first);
 * then the car, its body turning on the page; then the race -- the special's race file with the car's and the race game's keys
 * (RACEMOD.CPP drive_as=, start_scene=, start_in_car=, race_laps=, end_to_menu=, trial=, difficulty=) written to RACE_NOW.TXT in
 * the user's folder and loaded (RaceMod_LoadFile), and a new game at the start line, no introduction. A time trial's fastest single lap
 * (2026-10-18; before, its whole time) goes to the track's best laps, its ten fastest, kept in BESTTIMES.TXT in the user's folder; Best
 * laps shows them (a time trial is the same at every level: Twinsen alone, no opponents' levels to it). A race's laps, and a qualifying
 * lap's, never go there.
 * The first run (2026-10-17): the player's first race or time trial is preceded by the controls page, and the first race with power-ups
 * by the power-up page (the track's own power-ups, each in its item box with what it does) -- right after the car page, as the race is
 * loaded, before its scene: Enter turns the pages, Esc skips them. Options shows them again (its row "Race controls, power-ups"
 * -- two rows, Power-ups and Race controls, until 2026-10-19, when Updates took the menu's last place: GAMEMENU.CPP OptionsMenu asks
 * RaceGame_OptionsMenu). The first race always starts with a qualifying lap; once one has been driven, a race asks after
 * the car page -- a qualifying lap, or none and Twinsen at the back of the grid (RACEMOD.CPP grid_back=) -- the last answer chosen first.
 * The player's settings, RACEGAME_SETTINGS.TXT in the user's folder ("key=value" lines; other keys are kept as they are):
 *     controls_seen=1  powerups_seen=1     the first run's pages shown
 *     qualified=1                         a qualifying lap driven (RaceGame_Qualified): it may be skipped from then on
 *     skip_qualifying=0|1                 the last answer
 *     difficulty=0|1|2                    the difficulty chosen last (LevelPage)
 *     updates=auto|ask|off                new versions installed when the game is closed, asked first, or not looked for (auto)
 *     update_checked=<unix time>          when the game last started the updater (0: never)
 * Updates (2026-10-19): the installer leaves a copy of itself in the game's folder, Update\LBRA-Setup.exe. At the start (the race game
 * as a player runs it: none of the command harness's flags, so never an editor's Play or a scripted test -- those skip the menu), if
 * updates aren't off and a week has gone by since the last check (or there was none), the game starts it hidden --
 * `--check-update "<install dir>" --wait-pid <its pid>` (RACEUPDATE.CPP) -- and goes on: the updater looks for a new version
 * (https://lbabirthdayevent.com/LBRA-Setup.exe), downloads it, and installs it once the game is closed (or asks first), keeping the
 * player's User\ folder. Options, Updates: the mode (Automatic, Ask first, Off) and "Check for updates now" (`--manual`: a check even
 * with updates off, asked first then); the updater's last word (Update\status.txt, "<unix time>|<text>") under them.
 *
 * RACEGAME.TXT, "key=value" lines ('#' starts a comment), UTF-8:
 *     title=<the game's name>
 *     story=0|1  tracks=0|1        Story Mode, Track select open (0: greyed)
 *     laps=<n>                     a race's laps (3), unless its race file has its own (race_laps=: the Temple of Bú's five)
 *     end_to_menu=<ms>             a finished race back to the menu this long after its end (8000)
 *     special=<name>|<race file>|<time trial file>|<start scene>     files beside the game's data (and so are those they name)
 *     car=<name>|<drive_as value: BODY.HQR body, animation, shrunk body>
 * BESTTIMES.TXT: "<track>|<trial's time ms>|<best lap ms>|<car>|<date>|<laps>|lap" lines, a time trial's fastest single lap (and its
 * whole time, GO to the last lap's line, over <laps> laps), the fastest lap first. A line without "lap" at its end -- the whole trials'
 * times kept before 2026-10-18, "<track>|<time ms>|<best lap ms>|<car>|<date>[|<laps>]" -- is kept as it is but not shown, as are other
 * tracks' lines and lines that can't be read: a whole trial's time is never taken for a lap's.
 */
#include "C_EXTERN.H"
#include "CONFIG_FILE.H"
#include "CONSOLE/CONSOLE.H"
#include "CONTROL.H"
#include "DIRECTORIES.H"
#include "INPUT.H"
#include "JOYSTICK.H"
#include "RACEGAME.H"
#include "RACEMOD.H"
#include "RACEUPDATE.H"
#include "RES_SWITCH.H"
#include "UI_LAYOUT.H"

#include <FILEIO/SAVEPNG.H>
#include <SVGA/SCREEN.H>
#include <SYSTEM/KEYBOARD.H>
#include <SYSTEM/KEYBOARD_KEYS.H>
#include <SYSTEM/WINDOW.H>
#include <SYSTEM/LOG.H>
#include <SYSTEM/MOUSE.H>
#include <SYSTEM/STRING.H>

#include <cctype>
#include <cstdio>
#include <cstring>
#include <ctime>

extern void RaceMod_DrawBody(S32 x0, S32 y0, S32 x1, S32 y1, S32 body, S32 beta, S32 measure, S32 anim);   // RACEMOD.CPP

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
#define RG_ID_EASY 2312                     // the difficulty's rows: easy, medium, hard
#define RG_ID_MEDIUM 2313
#define RG_ID_HARD 2314
#define RG_ID_SPECIAL 2320                  // + the special's index
#define RG_ID_QUALIFY 2350                  // the qualifying page's rows (QualifyingPage)
#define RG_ID_SKIP 2351
#define RG_ID_POWERUPS 2352                 // the Options menu's rows of the race game's (RaceGame_OptionsMenu)
#define RG_ID_CONTROLS 2353
#define RG_ID_UPDATES 2354
#define RG_ID_UPDATE_MODE 2355              // the Updates page's rows (UpdatesPage)
#define RG_ID_UPDATE_NOW 2356
#define RG_ID_WINDOW 2357                   // the Updates page's full screen <-> a window row (CONFIG_FILE.CPP ToggleDisplayFullscreen; F11 / Alt+Enter too)
#define RG_ID_BACK 26                       // the game's own "Back"
#define RG_ID_RESUME 70
#define RG_ID_OPTIONS 74
#define RG_ID_QUIT 75

typedef struct {
    char Name[96], Race[160], Trial[160];
    S32 Scene;
    int Levels;                             // its race has levels, easy to hard (difficulty_level= in its race file)
    int Laps;                               // its race's laps once looked up (SpecialLaps; 0: not yet)
} T_RG_SPECIAL;

typedef struct {
    char Name[96], Drive[64];
    S32 Body, Anim;                         // drive_as='s body and driving animation (the car page poses the car by it)
} T_RG_CAR;

typedef struct {
    S32 Time, Lap, Laps;                    // the trial's whole time, its fastest lap (what ranks it), its laps
    char Car[96], Date[16];
} T_RG_TIME;

static int s_read = 0, s_on = 0;
static char s_title[96] = "";
static char s_windowTitle[96] = "";        // window_title=: the window's title bar ("Little Big Racing Adventure"; the engine's own otherwise)
static int s_story = 0, s_tracks = 0, s_laps = 3;
static S32 s_endMs = 8000;
static T_RG_SPECIAL s_sp[RG_MAX_SPECIALS];
static int s_nbSp = 0;
static T_RG_CAR s_cars[RG_MAX_CARS];
static int s_nbCars = 0;
static int s_car = 0;                       // the car chosen last
static int s_lastSp = 0, s_lastTrial = 0;
static int s_level = 1;                     // the difficulty chosen last: 0 easy, 1 medium, 2 hard (RACEGAME_SETTINGS.TXT)
static const char *const s_levelName[3] = {"Easy", "Medium", "Hard"};

static T_RG_TIME s_times[RG_MAX_SPECIALS][RG_MAX_TIMES];   // each special's best laps, the fastest first
static int s_nbTimes[RG_MAX_SPECIALS];
static char s_other[RG_OTHER_SIZE];         // BESTTIMES.TXT's other lines: other tracks', the whole trials' times of before 2026-10-18
static int s_timesRead = 0;

// the race on
static int s_raceSp = -1, s_raceTrial = 0, s_raceCar = -1, s_raceOver = 0;
// the race to start: a qualifying lap first (1), none and Twinsen at the back of the grid (0), or as its file says (-1) -- QualifyingPage
static int s_qualifyNow = -1;
static void Introduction(int sp, int trial);
static int s_raceLevel = -1;                // its difficulty (-1: none, a time trial or a track without levels)

static U16 s_mainMenu[4 + 2 * 8];
static U16 s_spMenu[4 + 2 * (1 + RG_MAX_SPECIALS)];
static U16 s_modeMenu[4 + 2 * 3];
static U16 s_levelMenu[4 + 2 * 4];

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

// Whether a race file (beside the game's data) has levels: a difficulty_level= line.
static int HasLevels(const char *race) {
    char path[ADELINE_MAX_PATH];
    GetResPath(path, ADELINE_MAX_PATH, race);
    FILE *f = fopen(path, "r");
    if (!f) return 0;
    char line[1024];
    int found = 0;
    while (!found && fgets(line, sizeof(line), f)) found = !strncmp(line, "difficulty_level=", 17);
    fclose(f);
    return found;
}

static void ReadSettings(void);
static void UpdateAtStart(void);

// The window's title for INITADEL.C, before the window is made (RACEGAME.TXT's window_title= alone, read on its own: the race game's
// Read waits for the game's paths and settings); NULL without the file or the key -- the engine's own title then.
extern "C" const char *RaceGame_WindowTitle(void) {
    static char title[96];
    char path[ADELINE_MAX_PATH];
    GetResPath(path, ADELINE_MAX_PATH, "RACEGAME.TXT");
    FILE *f = fopen(path, "r");
    if (!f) return NULL;
    char line[512];
    title[0] = 0;
    while (fgets(line, sizeof(line), f))
        if (!strncmp(line, "window_title=", 13)) {
            snprintf(title, sizeof(title), "%s", line + 13);
            Trim(title);
            break;
        }
    fclose(f);
    return title[0] ? title : NULL;
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
        else if (!strcmp(key, "window_title")) snprintf(s_windowTitle, sizeof(s_windowTitle), "%s", v);
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
            sp->Levels = sp->Race[0] ? HasLevels(sp->Race) : 0;
            if (sp->Name[0] && sp->Race[0]) s_nbSp++;
        } else if (!strcmp(key, "car") && s_nbCars < RG_MAX_CARS) {
            T_RG_CAR *c = &s_cars[s_nbCars];
            v = Field(v, c->Name, sizeof(c->Name));
            Field(v, c->Drive, sizeof(c->Drive));
            c->Body = c->Drive[0] ? atoi(c->Drive) : -1;
            if (!c->Drive[0] || sscanf(c->Drive, "%*d %d", &c->Anim) != 1) c->Anim = -1;
            if (c->Name[0] && c->Drive[0]) s_nbCars++;
        }
    }
    fclose(f);
    s_on = 1;
    if (s_windowTitle[0]) SetWindowTitle(s_windowTitle);
    ReadSettings();
    UpdateAtStart();
    int levelled = 0;
    for (int i = 0; i < s_nbSp; i++) levelled += s_sp[i].Levels;
    Log_Info("[racegame] %s: %d special track(s) (%d with levels: the last difficulty %s), %d cars%s%s", s_title[0] ? s_title : "the race game", s_nbSp,
             levelled, s_levelName[s_level], s_nbCars, s_story ? "" : ", Story Mode greyed", s_tracks ? "" : ", Track select greyed");
}

S32 RaceGame_On(void) {
    Read();
    return s_on;
}

// ---- the player's settings ---------------------------------------------------------------------------------------------------------

static int s_setRead = 0;
static int s_controlsSeen = 0, s_powerupsSeen = 0;  // the first run's pages shown
static int s_qualified = 0;                 // a qualifying lap driven: the next races may skip it
static int s_skipQualifying = 0;            // the last answer to "a qualifying lap?"
static int s_updates = 0;                   // new versions: 0 installed when the game is closed, 1 asked first, 2 not looked for
static long long s_updateChecked = 0;       // when the updater was last started (unix time; 0 never)
static const char *const s_updateKey[3] = {"auto", "ask", "off"};
static const char *const s_updateLabel[3] = {"Updates: Automatic", "Updates: Ask first", "Updates: Off"};
static char s_setOther[RG_OTHER_SIZE];      // RACEGAME_SETTINGS.TXT's lines of other keys, kept as they are

static void SettingsPath(char *path) { GetCfgPath(path, ADELINE_MAX_PATH, "RACEGAME_SETTINGS.TXT"); }

static void ReadSettings(void) {
    if (s_setRead) return;
    s_setRead = 1;
    s_setOther[0] = 0;
    char path[ADELINE_MAX_PATH];
    SettingsPath(path);
    FILE *f = fopen(path, "r");
    if (!f) { Log_Info("[racegame] no settings yet (%s): the first run's pages, and a qualifying lap first", path); return; }
    char line[512];
    while (fgets(line, sizeof(line), f)) {
        Trim(line);
        if (!line[0] || line[0] == '#') continue;
        char *eq = strchr(line, '=');
        int *value = NULL;
        if (eq) {
            *eq = 0;
            if (!strcmp(line, "controls_seen")) value = &s_controlsSeen;
            else if (!strcmp(line, "powerups_seen")) value = &s_powerupsSeen;
            else if (!strcmp(line, "qualified")) value = &s_qualified;
            else if (!strcmp(line, "skip_qualifying")) value = &s_skipQualifying;
            else if (!strcmp(line, "difficulty")) value = &s_level;
            else if (!strcmp(line, "updates")) {
                value = &s_updates;
                *value = 0;
                for (int k = 0; k < 3; k++) if (!strcmp(eq + 1, s_updateKey[k])) *value = k;
                continue;
            } else if (!strcmp(line, "update_checked")) {
                s_updateChecked = strtoll(eq + 1, NULL, 10);
                continue;
            }
            if (value) *value = atoi(eq + 1);
            else *eq = '=';
        }
        if (!value && strlen(s_setOther) + strlen(line) + 2 < sizeof(s_setOther)) { strcat(s_setOther, line); strcat(s_setOther, "\n"); }
    }
    fclose(f);
    if (s_level < 0 || s_level > 2) s_level = 1;
    Log_Info("[racegame] settings: controls seen %d, power-ups seen %d, qualified %d, skip qualifying %d, updates %s (checked %lld)", s_controlsSeen,
             s_powerupsSeen, s_qualified, s_skipQualifying, s_updateKey[s_updates], s_updateChecked);
}

// ---- updates -----------------------------------------------------------------------------------------------------------------------

#define RG_UPDATE_EVERY (7 * 24 * 3600)     // a check a week
#define RG_UPDATER "Update\\LBRA-Setup.exe"  // the installer's copy of itself (the installer's Updater.cs)
static long long s_updateStarted = 0;       // the updater started this run (by the page's "Check for updates now": the page says so)
static void WriteSettings(void);

// The updater started: `manual` from the Updates page (a check even with updates off). Non-zero: started.
static int UpdateSpawn(int manual) {
    char exe[ADELINE_MAX_PATH], dir[ADELINE_MAX_PATH];
    GetResPath(exe, ADELINE_MAX_PATH, RG_UPDATER);
    if (!RaceUpdate_Exists(exe)) {
        Log_Info("[racegame] no updater (%s): no update check", exe);
        return 0;
    }
    // (the game's folder: RACEGAME.TXT's)
    GetResPath(dir, ADELINE_MAX_PATH, "RACEGAME.TXT");
    char *cut = dir + strlen(dir);
    while (cut > dir && cut[-1] != '\\' && cut[-1] != '/') cut--;
    *cut = 0;
    if (!dir[0]) snprintf(dir, sizeof(dir), ".");
    if (!RaceUpdate_Spawn(exe, dir, manual)) return 0;
    ReadSettings();
    s_updateChecked = (long long)time(NULL);
    WriteSettings();
    return 1;
}

// At the start: the updater, when updates aren't off and the last check is a week old (or there was none, or it is in the future: a clock
// put back). Only as a player runs the game: an editor's Play and the tests run with the command harness.
static void UpdateAtStart(void) {
    if (Control_IsActive()) return;
    if (s_updates == 2) { Log_Info("[racegame] updates off: no update check"); return; }
    const long long now = (long long)time(NULL);
    if (s_updateChecked > 0 && now >= s_updateChecked && now - s_updateChecked < RG_UPDATE_EVERY) {
        Log_Info("[racegame] updates %s: checked %lld h ago, the next check in %lld h", s_updateKey[s_updates], (now - s_updateChecked) / 3600,
                 (s_updateChecked + RG_UPDATE_EVERY - now + 3599) / 3600);
        return;
    }
    if (UpdateSpawn(0)) Log_Info("[racegame] updates %s: a week since the last check (or none): checking", s_updateKey[s_updates]);
}

// The updater's last word (Update\status.txt: "<unix time>|<text>"), its text into `out` and its time; 0: none.
static long long UpdateStatus(char *out, size_t size) {
    out[0] = 0;
    char path[ADELINE_MAX_PATH];
    GetResPath(path, ADELINE_MAX_PATH, "Update\\status.txt");
    FILE *f = fopen(path, "r");
    if (!f) return 0;
    char line[512];
    long long at = 0;
    if (fgets(line, sizeof(line), f)) {
        Trim(line);
        const char *bar = strchr(line, '|');
        if (bar) {
            at = strtoll(line, NULL, 10);
            snprintf(out, size, "%s", bar + 1);
        }
    }
    fclose(f);
    return at;
}

static void WriteSettings(void) {
    char path[ADELINE_MAX_PATH];
    SettingsPath(path);
    FILE *f = fopen(path, "w");
    if (!f) { Log_Warn("[racegame] the settings could not be written to %s", path); return; }
    fprintf(f, "# %s: the player's settings (RACEGAME.CPP)\n", s_title[0] ? s_title : "The race game");
    fprintf(f, "controls_seen=%d\npowerups_seen=%d\nqualified=%d\nskip_qualifying=%d\ndifficulty=%d\n", s_controlsSeen, s_powerupsSeen, s_qualified,
            s_skipQualifying, s_level);
    fprintf(f, "updates=%s\nupdate_checked=%lld\n", s_updateKey[s_updates >= 0 && s_updates <= 2 ? s_updates : 0], s_updateChecked);
    fputs(s_setOther, f);
    fclose(f);
}

// ---- the best times ----------------------------------------------------------------------------------------------------------------

static void TimesPath(char *path) { GetCfgPath(path, ADELINE_MAX_PATH, "BESTTIMES.TXT"); }

// A special's race's laps: its race file's own (race_laps=: the Temple of Bú's five), else the menu's laps=. Its time trial is as long.
static int SpecialLaps(int sp) {
    T_RG_SPECIAL *s = &s_sp[sp];
    if (s->Laps > 0) return s->Laps;
    s->Laps = s_laps;
    char path[ADELINE_MAX_PATH];
    GetResPath(path, ADELINE_MAX_PATH, s->Race);
    FILE *f = fopen(path, "r");
    if (!f) return s->Laps;
    char line[512];
    while (fgets(line, sizeof(line), f))
        if (!strncmp(line, "race_laps=", 10) && atoi(line + 10) > 0) s->Laps = atoi(line + 10);
    fclose(f);
    return s->Laps;
}

// A best lap into its track's ten fastest, in its place (a lap as fast as one already there after it): its rank (1-10), 0 for none.
static int AddLap(int sp, const T_RG_TIME *t) {
    int k = 0;
    while (k < s_nbTimes[sp] && s_times[sp][k].Lap <= t->Lap) k++;
    if (k >= RG_MAX_TIMES) return 0;
    if (s_nbTimes[sp] < RG_MAX_TIMES) s_nbTimes[sp]++;
    for (int j = s_nbTimes[sp] - 1; j > k; j--) s_times[sp][j] = s_times[sp][j - 1];
    s_times[sp][k] = *t;
    return k + 1;
}

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
    int laps = 0, kept = 0;
    while (fgets(line, sizeof(line), f)) {
        Trim(line);
        if (!line[0]) continue;
        char track[96], ms[16], lap[16], nb[16], kind[16];
        T_RG_TIME t;
        const char *p = Field(line, track, sizeof(track));
        p = Field(p, ms, sizeof(ms));
        p = Field(p, lap, sizeof(lap));
        p = Field(p, t.Car, sizeof(t.Car));
        p = Field(p, t.Date, sizeof(t.Date));
        p = Field(p, nb, sizeof(nb));
        Field(p, kind, sizeof(kind));
        t.Time = atoi(ms);
        t.Lap = atoi(lap);
        t.Laps = nb[0] ? atoi(nb) : s_laps;
        // (a best lap's line ends in "lap"; one without it -- a whole trial's time, as they were kept before 2026-10-18 -- is kept, never
        // shown as a lap. A lap is a lap however long its trial was: the Temple of Bú's three-lap trials' too.)
        int sp = -1;
        if (!strcmp(kind, "lap") && t.Lap > 0)
            for (int i = 0; i < s_nbSp && sp < 0; i++) if (!strcmp(s_sp[i].Name, track)) sp = i;
        if (sp < 0) {
            if (strlen(s_other) + strlen(line) + 2 < sizeof(s_other)) { strcat(s_other, line); strcat(s_other, "\n"); kept++; }
            continue;
        }
        AddLap(sp, &t);
        laps++;
    }
    fclose(f);
    Log_Info("[racegame] best laps read: %d lap line(s), %d other line(s) kept as they are (other tracks', whole trials' times)", laps, kept);
}

static void WriteTimes(void) {
    char path[ADELINE_MAX_PATH];
    TimesPath(path);
    FILE *f = fopen(path, "w");
    if (!f) { Log_Warn("[racegame] the best laps could not be written to %s", path); return; }
    for (int sp = 0; sp < s_nbSp; sp++)
        for (int k = 0; k < s_nbTimes[sp]; k++) {
            const T_RG_TIME *t = &s_times[sp][k];
            fprintf(f, "%s|%d|%d|%s|%s|%d|lap\n", s_sp[sp].Name, (int)t->Time, (int)t->Lap, t->Car, t->Date, (int)t->Laps);
        }
    fputs(s_other, f);
    fclose(f);
}

static void TimeText(char *out, size_t size, S32 ms) {
    if (ms < 0) ms = 0;
    snprintf(out, size, "%d:%02d.%02d", (int)(ms / 60000), (int)(ms / 1000 % 60), (int)(ms / 10 % 100));
}

static int RecordLap(S32 total, S32 lap, int laps);

// RACEMOD.CPP: a race is over -- `total` its time from GO to the last lap's line, `bestLap` its fastest lap (the lap clock's: line to
// line, a qualifying lap's never). A time trial's fastest lap into its track's best laps: the rank it took (1-10), 0 for none.
S32 RaceGame_Finished(S32 trial, S32 total, S32 bestLap, S32 place, S32 racers) {
    if (!RaceGame_On()) return 0;
    s_raceOver = 1;
    char t[24], l[24];
    TimeText(t, sizeof(t), total);
    TimeText(l, sizeof(l), bestLap);
    Log_Info("[racegame] %s over: %s, best lap %s, %d of %d%s%s", trial ? "the time trial" : "the race", t, bestLap > 0 ? l : "none", (int)place,
             (int)racers, !trial && s_raceLevel >= 0 && s_raceLevel <= 2 ? ", difficulty " : "",
             !trial && s_raceLevel >= 0 && s_raceLevel <= 2 ? s_levelName[s_raceLevel] : "");
    if (!trial) return 0;
    return RecordLap(total, bestLap, SpecialLaps(s_raceSp >= 0 && s_raceSp < s_nbSp ? s_raceSp : 0));
}

// RACEMOD.CPP: the race's track's best lap, Best laps' first (ms; 0 for none or no race of the race game's) -- a time trial's display
// shows it over the lap, or the trial's own fastest lap when that is quicker.
S32 RaceGame_BestLap(void) {
    if (!RaceGame_On() || s_raceSp < 0 || s_raceSp >= s_nbSp) return 0;
    ReadTimes();
    return s_nbTimes[s_raceSp] > 0 ? s_times[s_raceSp][0].Lap : 0;
}

// A lap into the race's track's best laps (a time trial's fastest; a qualifying lap: RaceGame_Qualified -- no power-ups on either): the
// rank it took (1-10), 0 for none. `total` the whole run's time (a qualifying lap's own), `laps` how many laps that was.
static int RecordLap(S32 total, S32 lap, int laps) {
    if (s_raceSp < 0 || s_raceSp >= s_nbSp || lap <= 0) return 0;
    char l[24];
    TimeText(l, sizeof(l), lap);
    ReadTimes();
    const int sp = s_raceSp;
    T_RG_TIME e;
    e.Time = total;
    e.Lap = lap;
    e.Laps = laps;
    snprintf(e.Car, sizeof(e.Car), "%s", s_raceCar >= 0 && s_raceCar < s_nbCars ? s_cars[s_raceCar].Name : "");
    const time_t now = time(NULL);
    const struct tm *tm = localtime(&now);
    if (tm) strftime(e.Date, sizeof(e.Date), "%Y-%m-%d", tm);
    else e.Date[0] = 0;
    const int rank = AddLap(sp, &e);
    if (!rank) { Log_Info("[racegame] best laps of %s: %s not among the ten fastest", s_sp[sp].Name, l); return 0; }
    WriteTimes();
    Log_Info("[racegame] best laps of %s: %s is %d%s", s_sp[sp].Name, l, rank, rank == 1 ? " (the best)" : "");
    return rank;
}

// ---- the race ----------------------------------------------------------------------------------------------------------------------

static int Absolute(const char *p) { return p[0] == '/' || p[0] == '\\' || (isalpha((unsigned char)p[0]) && p[1] == ':'); }

// The race: the special's race file (or its time trial's), the files it names found beside the game's data, then the car and the race
// game's keys -- and the difficulty chosen (`level`, -1 none: after the file's own, so it counts) -- written to RACE_NOW.TXT in the user's
// folder and loaded in place of any other.
static int StartRace(int sp, int trial, int car, int level) {
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
    fprintf(f, "start_in_car=1\nrace_laps=%d\nend_to_menu=%d\n", SpecialLaps(sp), (int)s_endMs);
    if (trial) fprintf(f, "trial=1\n");
    // (a qualifying lap or none, the player's answer -- QualifyingPage: the first race always has one)
    else if (s_qualifyNow >= 0) fprintf(f, s_qualifyNow ? "qualifying=1\n" : "qualifying=0\ngrid_back=1\n");
    if (level >= 0 && level <= 2) fprintf(f, "# the difficulty chosen: %s\ndifficulty=%d\n", s_levelName[level], level);
    fclose(f);
    s_raceSp = sp; s_raceTrial = trial; s_raceCar = car; s_raceOver = 0; s_raceLevel = level;
    Log_Info("[racegame] %s: %s with %s%s%s%s", s->Name, trial ? "a time trial" : "a race", car >= 0 && car < s_nbCars ? s_cars[car].Name : "Twinsen's car",
             level >= 0 && level <= 2 ? ", difficulty " : "", level >= 0 && level <= 2 ? s_levelName[level] : "",
             trial || s_qualifyNow < 0 ? "" : s_qualifyNow ? ", a qualifying lap first" : ", no qualifying: from the back of the grid");
    s_qualifyNow = -1;
    RaceMod_LoadFile(out);
    // (the first run: the controls and the power-ups before the race -- once)
    Introduction(sp, trial);
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
    case RG_ID_TIMES: Label(string, "Best laps"); return 1;
    case RG_ID_RACE: Label(string, "Race"); return 1;
    case RG_ID_TRIAL: Label(string, "Time trial"); return 1;
    case RG_ID_QUALIFY: Label(string, "Qualifying lap"); return 1;
    case RG_ID_SKIP: Label(string, "Skip: start at the back of the grid"); return 1;
    case RG_ID_POWERUPS: Label(string, "Power-ups"); return 1;
    case RG_ID_CONTROLS: Label(string, "Race controls, power-ups"); return 1;   // (the game's own Controls row changes the keys)
    case RG_ID_UPDATES: Label(string, "Window mode, updates"); return 1;
    case RG_ID_UPDATE_MODE: Label(string, s_updateLabel[s_updates >= 0 && s_updates <= 2 ? s_updates : 0]); return 1;
    case RG_ID_UPDATE_NOW: Label(string, "Check for updates now"); return 1;
    case RG_ID_WINDOW: Label(string, DisplayFullScreen ? "Window mode: full screen (F11)" : "Window mode: window (F11)"); return 1;
    case RG_ID_EASY: case RG_ID_MEDIUM: case RG_ID_HARD: Label(string, s_levelName[num - RG_ID_EASY]); return 1;
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

// DoGameMenu for a page of the race game's: MENU_RELAYOUT when the window was resized and the picture fitted to it -- the backdrop made
// again at the new size, the page (its header, its text) to be drawn again over it before the menu is asked again (each page's loop
// goes round for any row it doesn't know)
static S32 Menu(U16 *menu) {
    const S32 was = MenuRelayoutReturns;
    MenuRelayoutReturns = 1;
    const S32 select = DoGameMenu(menu);
    MenuRelayoutReturns = was;
    return select;
}

// A page's own loop (the car, Best laps, the controls and power-ups): the window resized and the picture fitted to it -- the backdrop
// made again at the new size, the page to be laid out and drawn again (non-zero)
static int Refitted(void) {
    if (!Res_ApplyWindowFit()) return 0;
    Menu_RepaintAfterWindowFit();
    Page(NULL, 0);
    return 1;
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
    S32 cx = UI_CENTER_X, oy = UI_VCENTER_OFS;
    S32 x0 = cx - 230, x1 = cx + 230, y0 = 130 + oy, y1 = 330 + oy;
    int chosen = -2;
    S32 start = TimerRefHR;
    s_clickWas = Click & 3;
    while (chosen == -2) {
        ManageTime();
        MyGetInput();
        // (the window resized: the page laid out again at the new size)
        if (Refitted()) {
            cx = UI_CENTER_X; oy = UI_VCENTER_OFS;
            x0 = cx - 230; x1 = cx + 230; y0 = 130 + oy; y1 = 330 + oy;
        }
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
        RaceMod_DrawBody(x0 + 2, y0 + 2, x1 - 2, y1 - 2, s_cars[s_car].Body, 0, 1, s_cars[s_car].Anim);
        // the page: the backdrop, the header, the box with the car, its name and the keys
        const S32 top = y0 - 36, bottom = y1 + 120 < (S32)ModeDesiredY ? y1 + 120 : (S32)ModeDesiredY - 1;
        CopyBlock(0, top, (S32)ModeDesiredX - 1, bottom, Screen, 0, top, Log);
        Text(cx, y0 - 34, "Choose your car", RG_ORANGE);
        ShadeBoxBlk(x0, y0, x1, y1, MENU_SHADE_LVL);
        RaceMod_DrawBody(x0 + 2, y0 + 2, x1 - 2, y1 - 2, s_cars[s_car].Body, (S32)(((TimerRefHR - start) / 3 + 3072) & 4095), 0, s_cars[s_car].Anim);
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

// Best laps: a track's ten fastest laps of a time trial, with the car (left and right go through the tracks); Esc, Enter or a click
// goes back.
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
    S32 cx = UI_CENTER_X, oy = UI_VCENTER_OFS;
    S32 x0 = cx - 280, x1 = cx + 280, y0 = 60 + oy, y1 = 440 + oy;
    s_clickWas = Click & 3;
    while (!done) {
        ManageTime();
        MyGetInput();
        // (the window resized: the page laid out and drawn again at the new size)
        if (Refitted()) {
            cx = UI_CENTER_X; oy = UI_VCENTER_OFS;
            x0 = cx - 280; x1 = cx + 280; y0 = 60 + oy; y1 = 440 + oy;
            flag = 1;
        }
        S32 mx, my;
        if (Clicked(&mx, &my)) done = 1;
        const S32 step = s_nbSp > 1 ? Step(&padDir) : 0;
        if (step) { sp = (sp + step + s_nbSp) % s_nbSp; flag = 1; }
        if (flag) {
            flag = 0;
            Page(NULL, 0);
            ShadeBoxBlk(x0, y0, x1, y1, MENU_SHADE_LVL);
            DrawCadre(x0, y0, x1, y1, ALL_ANGLES);
            Text(cx, y0 + 12, "Best laps", RG_ORANGE);
            Text(cx, y0 + 48, s_nbSp ? s_sp[sp].Name : "No tracks", LBAWHITE);
            if (sp < s_nbSp && !s_nbTimes[sp]) {
                Text(cx, y0 + 150, "No laps yet: drive a time trial!", RG_GREY);
                Text(cx, y0 + 186, "Its fastest lap goes here.", RG_GREY);
            }
            for (int k = 0; sp < s_nbSp && k < s_nbTimes[sp]; k++) {
                const T_RG_TIME *t = &s_times[sp][k];
                char time[24], line[200];
                TimeText(time, sizeof(time), t->Lap);
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

// ---- the power-ups, the controls, the qualifying -------------------------------------------------------------------------------------

// The console's screenshot (PERSO.CPP takes it in the game's loop) taken on these pages too, as they stand drawn -- in the menus'
// palette, which they are shown in
static void Shot(void) {
    char path[ADELINE_MAX_PATH];
    int png = 0;
    if (!Console_HasPendingScreenshot(path, sizeof(path), &png)) return;
    if (png) SavePNG(path, Log, (S32)ModeDesiredX, (S32)ModeDesiredY, PtrPalNormal);
    Console_ClearPendingScreenshot();
    Log_Info("[racegame] screenshot %s", path);
}

// A line of text from x (its left end), with the menus' dark shadow; already in the font's code page.
static void TextAt(S32 x, S32 y, const char *cp850, S32 colour) {
    ColorFont(BLACK);
    Font(x + 3, y + 3, (char *)cp850);
    ColorFont(colour);
    Font(x, y, (char *)cp850);
}

// A sentence from (x, y), broken between words into lines no wider than `width`, `step` apart: the lines drawn.
static int Wrapped(S32 x, S32 y, S32 width, S32 step, const char *utf8, S32 colour) {
    char text[512], line[512];
    CopyUtf8ToCp850(text, sizeof(text), utf8);
    int lines = 0;
    const char *p = text;
    while (*p) {
        while (*p == ' ') p++;
        size_t n = 0;
        // (word by word while the line still fits: at least one word a line)
        for (const char *q = p; *q;) {
            const char *end = q;
            while (*end && *end != ' ') end++;
            const size_t len = (size_t)(end - p);
            if (len >= sizeof(line)) break;
            memcpy(line, p, len);
            line[len] = 0;
            if (n && SizeFont(line) > width) break;
            n = len;
            q = end;
            while (*q == ' ') q++;
        }
        if (!n) break;
        memcpy(line, p, n);
        line[n] = 0;
        TextAt(x, y + lines * step, line, colour);
        lines++;
        p += n;
    }
    return lines;
}

// A page's frame: the menu's backdrop, a box shaded darker than the menus' (a page of text over the menu's picture) and framed as they
// are, the page's header in it
#define RG_PAGE_HALF 300                    // the box's half width
#define RG_PAGE_TOP 8                       // ... its top and bottom on the 480-tall canvas
#define RG_PAGE_BOTTOM 472
#define RG_PAGE_SHADE 13
#define RG_LINE 26                          // a line of text
static void PageFrame(const char *header) {
    const S32 cx = UI_CENTER_X, oy = UI_VCENTER_OFS;
    Page(NULL, 0);
    ShadeBoxBlk(cx - RG_PAGE_HALF, RG_PAGE_TOP + oy, cx + RG_PAGE_HALF, RG_PAGE_BOTTOM + oy, RG_PAGE_SHADE);
    DrawCadre(cx - RG_PAGE_HALF, RG_PAGE_TOP + oy, cx + RG_PAGE_HALF, RG_PAGE_BOTTOM + oy, ALL_ANGLES);
    Text(cx, RG_PAGE_TOP + oy + 8, header, RG_ORANGE);
}

// The controls: the keys the race game uses (the game's defaults -- Options, Controller changes the keyboard's), then a pad's.
// (2026-10-19: and, on a track whose race lets Twinsen out of his car to explore -- RACEMOD.CPP on_foot=, the Temple of Bú's secret
// party -- the action key's row, its lines a little closer: two more rows over the page's own keys at its foot)
#define RG_ON_FOOT "Get out / back in"
static void DrawControls(const T_RACE_PAGE *pg) {
    static const char *const keys[][2] = {
        {"Up arrow", "Accelerate"},
        {"Down arrow", "Brake, reverse"},
        {"Left, right", "Steer"},
        {"Ctrl", "Use your item"},
        {"", ""},                           // (the gears: by hand or by themselves)
        {"Space", RG_ON_FOOT},
        {"P", "Pause"},
        {"Esc", "Menu"},
    };
    static const char *const pad[][2] = {
        {"Right trigger", "Accelerate"},
        {"Left trigger", "Brake, reverse"},
        {"Left stick", "Steer"},
        {"Left shoulder", "Use your item"},
        {"X button", RG_ON_FOOT},
        {"Start", "Menu"},
    };
    const S32 cx = UI_CENTER_X, oy = UI_VCENTER_OFS, kx = cx - RG_PAGE_HALF + 36, ax = cx - 40;
    const int onFoot = pg && pg->OnFoot;
    const S32 line = onFoot ? RG_LINE - 3 : RG_LINE;
    PageFrame("Controls");
    S32 y = RG_PAGE_TOP + oy + 48;
    char text[256];
    for (size_t i = 0; i < sizeof(keys) / sizeof(keys[0]); i++) {
        const char *key = keys[i][0], *what = keys[i][1];
        if (!onFoot && !strcmp(what, RG_ON_FOOT)) continue;
        if (!key[0]) {
            key = pg && pg->Manual ? "X, Z" : "Gears";
            what = pg && pg->Manual ? "Gear up, down" : "Automatic";
        }
        Label(text, key);
        TextAt(kx, y, text, LBAWHITE);
        Label(text, what);
        TextAt(ax, y, text, RG_GREY);
        y += line;
    }
    y += line / 2;
    Label(text, "With a gamepad");
    TextAt(kx, y, text, RG_ORANGE);
    y += line + 4;
    for (size_t i = 0; i < sizeof(pad) / sizeof(pad[0]); i++) {
        if (!onFoot && !strcmp(pad[i][1], RG_ON_FOOT)) continue;
        Label(text, pad[i][0]);
        TextAt(kx, y, text, LBAWHITE);
        Label(text, pad[i][1]);
        TextAt(ax, y, text, RG_GREY);
        y += line;
    }
}

// The power-ups: the pickups first (what a car drives into for an item), then the track's power-ups `first`.. (RG_PAGE_ITEMS of them a
// page), each in its item box turning as the race shows it, its name and what it does.
#define RG_PAGE_ITEMS 4
#define RG_ICON 52
#define RG_ITEM_LINE 24                     // (the items' lines a little closer, the items further apart)
#define RG_ITEM_STEP 84
static void DrawPowerUps(const T_RACE_PAGE *pg, int first, S32 now) {
    const S32 cx = UI_CENTER_X, oy = UI_VCENTER_OFS, ix = cx - RG_PAGE_HALF + 18, tx = ix + RG_ICON + 16, width = cx + RG_PAGE_HALF - 12 - tx;
    PageFrame("Power-ups");
    S32 y = RG_PAGE_TOP + oy + 38;
    if (!pg || !pg->Count) { Text(cx, y + 100, "No power-ups on this track.", LBAWHITE); return; }
    // (a time trial has no power-ups since 2026-10-19: its laps, as the qualifying laps, go to the best laps)
    if (pg->Trial) { Wrapped(ix, y + 2, cx + RG_PAGE_HALF - 12 - ix, RG_ITEM_LINE, "A time trial: no power-ups, just you and the clock. Its fastest lap goes to Best laps.", LBAWHITE); return; }
    else {
        RaceMod_DrawItem(pg, -1, ix, y, RG_ICON, now);
        Wrapped(tx, y + 2, width, RG_ITEM_LINE, "Drive into these for a power-up in your item box. Ctrl uses it.", LBAWHITE);
    }
    y += RG_ICON + 12;
    char text[256];
    for (int k = first; k < pg->Count && k < first + RG_PAGE_ITEMS; k++, y += RG_ITEM_STEP) {
        RaceMod_DrawItem(pg, pg->Item[k], ix, y, RG_ICON, now);
        Label(text, RaceMod_ItemName(pg->Item[k]));
        TextAt(tx, y - 2, text, RG_ORANGE);
        Wrapped(tx, y - 2 + RG_ITEM_LINE, width, RG_ITEM_LINE, RaceMod_ItemHelp(pg, pg->Item[k]), LBAWHITE);
    }
}

// The track a page shows: the special raced last (or chosen last), its race file read -- once a special
static T_RACE_PAGE s_page;
static int s_pageSp = -1;
static const T_RACE_PAGE *TrackPage(int sp) {
    if (sp < 0 || sp >= s_nbSp) return NULL;
    if (sp != s_pageSp) {
        char path[ADELINE_MAX_PATH];
        GetResPath(path, ADELINE_MAX_PATH, s_sp[sp].Race);
        RaceMod_ReadPage(path, &s_page);
        s_pageSp = sp;
    }
    return &s_page;
}

// The pages in a row: the controls (`controls`), then the power-ups (`items`: the track's, a few a page). Left and right turn them, Enter
// the next (after the last: done), Esc done; the mouse's left button the next, its right done. `intro`: before a race (the first run's).
static void Pages(const T_RACE_PAGE *pg, int controls, int items, int intro) {
    const int itemPages = !items ? 0 : pg && pg->Count ? (pg->Count + RG_PAGE_ITEMS - 1) / RG_PAGE_ITEMS : 1;
    const int pages = (controls ? 1 : 0) + itemPages;
    if (pages <= 0) return;
    Page(NULL, 0);
    BoxUpdate();
    ManageTime();
    InitWaitNoKey();
    InitWaitNoInput(I_JOY | I_FIRE | I_MENUS | I_ACTION);
    S32 padDir = JoystickMenuNavOnly();
    AsciiMode = TRUE;
    s_clickWas = Click & 3;
    int at = 0, done = 0;
    S32 cx = UI_CENTER_X, oy = UI_VCENTER_OFS;
    Log_Info("[racegame] %s: %d page(s)%s", controls && itemPages ? "the controls and the power-ups" : controls ? "the controls" : "the power-ups",
             pages, intro ? " before the race" : "");
    while (!done) {
        ManageTime();
        MyGetInput();
        // (the window resized: the page laid out again at the new size -- it is drawn whole every pass)
        if (Refitted()) { cx = UI_CENTER_X; oy = UI_VCENTER_OFS; }
        S32 mx, my;
        const int click = Clicked(&mx, &my);
        S32 step = pages > 1 ? Step(&padDir) : 0;
        if (click == 2) done = 1;
        else if (click == 1) { if (at + 1 < pages) at++; else done = 1; }
        if (step && at + step >= 0 && at + step < pages) at += step;
        if (done) break;
        const S32 now = TimerRefHR;
        if (controls && at == 0) DrawControls(pg);
        else DrawPowerUps(pg, (at - (controls ? 1 : 0)) * RG_PAGE_ITEMS, now);
        char keys[160];
        const char *last = intro ? "Enter: start" : "Enter: back";
        if (pages > 1) snprintf(keys, sizeof(keys), "Page %d of %d    %s    Esc: %s", at + 1, pages, at + 1 < pages ? "Enter: next" : last, intro ? "skip" : "back");
        else if (intro) snprintf(keys, sizeof(keys), "%s    Esc: skip", last);
        else snprintf(keys, sizeof(keys), "Esc: back");
        Text(cx, RG_PAGE_BOTTOM + oy - 32, keys, RG_GREY);
        BoxStaticAdd(0, 0, (S32)ModeDesiredX - 1, (S32)ModeDesiredY - 1);
        Shot();
        BoxUpdate();
        if (Confirmed()) { WaitNoInput(); if (at + 1 < pages) at++; else done = 1; }
        else if (Escaped()) { WaitNoInput(); done = 1; }
    }
    AsciiMode = FALSE;
    Page(NULL, 0);
}

// The first run's: before the player's first race or time trial the controls, before the first race with power-ups its power-ups --
// each once (the settings say so after).
static void Introduction(int sp, int trial) {
    ReadSettings();
    const T_RACE_PAGE *pg = TrackPage(sp);
    const int controls = !s_controlsSeen, items = !trial && pg && pg->Count && !s_powerupsSeen;
    if (!controls && !items) return;
    Pages(pg, controls, items, 1);
    if (controls) s_controlsSeen = 1;
    if (items) s_powerupsSeen = 1;
    WriteSettings();
}

// After the car, before a race: a qualifying lap, or none and Twinsen at the back of the grid (the opponents before him in their usual
// order) -- the answer last given chosen first. Not asked for a time trial, on a track with no grid, nor before the player has driven a
// qualifying lap (the first race always has one). Zero: back (Esc), the car page again.
static U16 s_qualMenu[4 + 2 * 3];
static int QualifyingPage(int sp, int trial) {
    s_qualifyNow = -1;
    if (trial) return 1;
    ReadSettings();
    const T_RACE_PAGE *pg = TrackPage(sp);
    if (!pg || !pg->Spots) return 1;
    if (!s_qualified) {
        s_qualifyNow = 1;
        Log_Info("[racegame] the first race: a qualifying lap, not to be skipped");
        return 1;
    }
    s_qualMenu[0] = (U16)(s_skipQualifying ? 2 : 1);
    s_qualMenu[1] = 3;
    s_qualMenu[2] = 280;
    s_qualMenu[3] = 0;
    SetRow(s_qualMenu, 0, RG_ID_BACK);
    SetRow(s_qualMenu, 1, RG_ID_QUALIFY);
    SetRow(s_qualMenu, 2, RG_ID_SKIP);
    for (;;) {
        Page("Qualifying", 128);
        const S32 select = Menu(s_qualMenu);
        if (select == RG_ID_BACK || select == 1000) { Page(NULL, 0); return 0; }
        if (select != RG_ID_QUALIFY && select != RG_ID_SKIP) continue;
        s_skipQualifying = select == RG_ID_SKIP;
        s_qualifyNow = !s_skipQualifying;
        WriteSettings();
        Page(NULL, 0);
        return 1;
    }
}

// RACEMOD.CPP: a qualifying lap driven (in a race the race game started). The first: qualifying may be skipped from the next race on.
// Every one goes to the track's best laps too (the user, 2026-10-19: "qualifying laps also count for Best laps, as these don't contain
// powerups"), as a lap of its own.
void RaceGame_Qualified(S32 lap) {
    if (!RaceGame_On() || s_raceSp < 0) return;
    RecordLap(lap, lap, 1);
    ReadSettings();
    if (s_qualified) return;
    s_qualified = 1;
    WriteSettings();
    Log_Info("[racegame] the first qualifying lap driven (%.2f s): the next races may skip it", lap / 1000.0);
}

// Options, "Window mode, updates": full screen or a window (F11 / Alt+Enter too), then the update mode (Automatic, Ask first, Off: Enter goes round them) and "Check for updates now", what the mode does and the
// updater's last word under them.
static U16 s_updMenu[4 + 2 * 4];
// (two lines each at most, and the updater's word two more: the page's text below the rows fits the 480-tall canvas)
static const char *const s_updateHelp[3] = {
    "Weekly: a new version is installed when you close the game.",
    "Weekly: you are asked before a new version is installed.",
    "No weekly check. Check for updates here when you like.",
};
#define RG_UPD_Y 240                        // the rows' centre
static void UpdatesPage(void) {
    ReadSettings();
    s_updMenu[0] = 1;
    s_updMenu[1] = 4;
    s_updMenu[2] = RG_UPD_Y;
    s_updMenu[3] = 0;
    SetRow(s_updMenu, 0, RG_ID_BACK);
    SetRow(s_updMenu, 1, RG_ID_WINDOW);
    SetRow(s_updMenu, 2, RG_ID_UPDATE_MODE);
    SetRow(s_updMenu, 3, RG_ID_UPDATE_NOW);
    char exe[ADELINE_MAX_PATH];
    GetResPath(exe, ADELINE_MAX_PATH, RG_UPDATER);
    for (;;) {
        const S32 cx = UI_CENTER_X, oy = UI_VCENTER_OFS;
        Page("Window mode, updates", RG_UPD_Y - (56 * 4) / 2 - 50);
        const S32 textY = RG_UPD_Y + (56 * 4) / 2 + 6 + oy;
        const int lines = Wrapped(cx - 280, textY, 560, RG_LINE, s_updateHelp[s_updates], LBAWHITE);
        // (the updater's last word -- or, while a check started here hasn't said anything yet, that it is checking)
        char status[400], said[400];
        const long long at = UpdateStatus(said, sizeof(said));
        if (!RaceUpdate_Exists(exe)) snprintf(status, sizeof(status), "No updater in the game's folder: install the game again.");
        else if (s_updateStarted && at < s_updateStarted) snprintf(status, sizeof(status), "Checking for a new version...");
        else if (said[0]) snprintf(status, sizeof(status), "%s", said);
        else status[0] = 0;
        if (status[0]) Wrapped(cx - 280, textY + lines * RG_LINE + 6, 560, RG_LINE, status, RG_GREY);
        const S32 select = Menu(s_updMenu);
        if (select == RG_ID_BACK || select == 1000) { Page(NULL, 0); return; }
        for (S32 i = 0; i < 4; i++) if (s_updMenu[4 + 2 * i + 1] == (U16)select) s_updMenu[0] = (U16)i;
        if (select == RG_ID_WINDOW) ToggleDisplayFullscreen();
        else if (select == RG_ID_UPDATE_MODE) {
            s_updates = (s_updates + 1) % 3;
            WriteSettings();
            Log_Info("[racegame] updates: %s", s_updateKey[s_updates]);
        } else if (select == RG_ID_UPDATE_NOW && UpdateSpawn(1)) {
            s_updateStarted = (long long)time(NULL);
        }
    }
}

// GAMEMENU.CPP OptionsMenu: the race game's options -- the game's own rows, then the race's controls and power-ups (one row: the menu has
// room for eight) and Updates (RaceGame_OptionsRow); the game's own menu when the race game isn't on.
static U16 s_optMenu[4 + 2 * 12];
U16 *RaceGame_OptionsMenu(U16 *menu) {
    if (!RaceGame_On() || !menu || menu[1] + 3 > 12) return menu;
    const int nb = menu[1];
    memcpy(s_optMenu, menu, (4 + 2 * nb) * sizeof(U16));
    SetRow(s_optMenu, nb, RG_ID_CONTROLS);
    SetRow(s_optMenu, nb + 1, RG_ID_UPDATES);
    s_optMenu[1] = (U16)(nb + 2);
    return s_optMenu;
}

S32 RaceGame_OptionsRow(S32 num) {
    if (!RaceGame_On() || (num != RG_ID_POWERUPS && num != RG_ID_CONTROLS && num != RG_ID_UPDATES)) return 0;
    if (num == RG_ID_UPDATES) {
        UpdatesPage();
        return 1;
    }
    const T_RACE_PAGE *pg = TrackPage(s_raceSp >= 0 ? s_raceSp : s_lastSp >= 0 && s_lastSp < s_nbSp ? s_lastSp : 0);
    if (num == RG_ID_POWERUPS) Pages(pg, 0, 1, 0);
    else Pages(pg, 1, 1, 0);
    return 1;
}


// A race's difficulty, on a track with levels: easy, medium or hard -- the one chosen last first -- kept for the next time. The level
// (0-2), or -1 for Back.
static int LevelPage(void) {
    s_levelMenu[0] = (U16)(1 + s_level);
    s_levelMenu[1] = 4;
    s_levelMenu[2] = 280;
    s_levelMenu[3] = 0;
    SetRow(s_levelMenu, 0, RG_ID_BACK);
    SetRow(s_levelMenu, 1, RG_ID_EASY);
    SetRow(s_levelMenu, 2, RG_ID_MEDIUM);
    SetRow(s_levelMenu, 3, RG_ID_HARD);
    for (;;) {
        Page("Difficulty", 280 - (56 * 4) / 2 - 50);
        // (what the levels do, under the rows)
        Text(UI_CENTER_X, 400 + UI_VCENTER_OFS, "Easy: slower monsters, fewer items", RG_GREY);
        Text(UI_CENTER_X, 436 + UI_VCENTER_OFS, "Hard: quicker monsters, aimed attacks", RG_GREY);
        const S32 select = Menu(s_levelMenu);
        if (select == RG_ID_BACK || select == 1000) { Page(NULL, 0); return -1; }
        if (select < RG_ID_EASY || select > RG_ID_HARD) continue;
        s_level = select - RG_ID_EASY;
        WriteSettings();
        Log_Info("[racegame] difficulty: %s", s_levelName[s_level]);
        return s_level;
    }
}

// A special's race or time trial; then (a race on a track with levels) its difficulty; then the car -- Esc there back to the difficulty.
// Non-zero: the race is set up.
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
        const S32 select = Menu(s_modeMenu);
        if (select == RG_ID_BACK || select == 1000) { Page(NULL, 0); return 0; }
        if (select != RG_ID_RACE && select != RG_ID_TRIAL) continue;
        const int trial = select == RG_ID_TRIAL;
        s_lastTrial = trial;
        // (a race on a track with levels: its difficulty first; then the car; then a race's qualifying lap, or none -- Esc on the
        // qualifying page is the car page again, Esc on the car page the difficulty again)
        for (;;) {
            int level = -1;
            if (!trial && s_sp[sp].Levels && (level = LevelPage()) < 0) break;
            int car;
            while ((car = CarPage(trial ? "a time trial" : "a race")) >= 0 && !QualifyingPage(sp, trial)) {}
            if (car >= 0) { if (StartRace(sp, trial, car, level)) return 1; break; }
            if (level < 0) break;
        }
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
        const S32 select = Menu(s_spMenu);
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
        const S32 select = Menu(s_mainMenu);
        // (the window resized: the backdrop is made again -- shaded in a race -- and the rows go on it)
        if (select == MENU_RELAYOUT) continue;
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
