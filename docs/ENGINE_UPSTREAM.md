# The engine and its upstream

`native/lba2-classic-community` is a copy of [LBALab/lba2-classic-community](https://github.com/LBALab/lba2-classic-community).
It is not a submodule or subtree, and it has been changed heavily for the editor and the race game: RACEMOD, RACEGAME, FOLLOWCAM, the menus, audio, the renderer API.

The original source release, [2point21/lba2-classic](https://github.com/2point21/lba2-classic), has had no code changes since 2021. Its last commit (2021-12-22) only changed the website link, and the community fork already contains all of it. Checked 2026-10-30: nothing to take from there.

## Baseline

- **Taken from upstream commit `c22c86ba`** (2026-09-14, after PR #683). It was added to this repo in fb20c9c on 2026-09-15.
- At that commit, 1,412 of our 1,421 files were identical to upstream.

## Merged from upstream (2026-10-30)

Upstream was reviewed up to `2fa1c7a6` (2026-10-23): 311 non-merge commits since the baseline.

Each fix below was applied by patch to the source files only; upstream's docs and tests are not kept here.

| Upstream | Fix |
|---|---|
| 75c1139e | save: read and write the save buffer without casting the pointer |
| 6c034d0a | display: show the selected resolution indoors |
| 2519c683 | svga: clip CopyMask at the top edge instead of moving the mask down |
| 0d7421b2 | render: re-mask and draw left-edge bricks at widths off the 24-px grid |
| 75275ebc | render: room for two diagonals of bricks in column 0 |
| 95e2ad01 | config: reach the second column of the gamepad screen from the keyboard |
| 8e0799fe | inventory: a direction held through the found-item popup no longer sticks |
| 13eb9fd7 | saves: restore the clover snapshot in the layout it was written in |
| ac75a7b6 | menus: keep the name editor's cursor inside its box |
| 0cdad16c | svga: copy nothing for an inverted SaveBlock / RestoreBlock rectangle |
| ca3337b1 | display: wipe the pointer sprite on the frame that drew it |
| c1f10bbf | render: restore the 1997 scale of INV64 (sky, sea and lava texels) |
| 8df3bf85 | render: keep scenery at the same depth in list order |
| fb5bf4dd | flow: draw a throw's random spread in the 1997 order |
| 7d49cf76 | render: draw the scene before a screen opens on a loop's first frame |
| 5a9c8996 | collision: slide along slanted ground edges again (taken later on 2026-10-30, at the user's request) |
| 3dd5e5dd | saves: portable (ASCII) save file names, and "Save failed" when a save can't be written (taken later on 2026-10-30, at the user's request) |

`3dd5e5dd` calls upstream's `ade_strcasecmp` (from its refactor `1392f941`); here it is SDL's `SDL_strcasecmp`.

Checks for the last two:
- **`5a9c8996`:** 7 tracks raced for a minute each with the overtaking test (Otringal, the Francos, the Emerald Moon, Island CX Mk2, Citadel 2, Volcano Island, the Elevator Platform). No new stuck spots: Volcano Island's one stuck spot happens at the same place before it. No warnings.
- **`3dd5e5dd`:** a save made and loaded headless, and Story Mode's `story.lba` kept and continued.

## Second round (2026-10-30, the outstanding ones)

Done by three-way merge rather than plain patches. Our engine was committed onto a branch from `c22c86ba` in a clone of upstream, and each change was cherry-picked onto it.

**Merged as they are:**

| Upstream | Fix |
|---|---|
| c6e57701 | svga: clip SaveBlock and RestoreBlock to the screen |
| 1a167f16 | saves: initialise SaveGame's compression pointers |
| 74368c49 | music: drop an older park when a pause finds nothing to park |
| 6a90417e | saves: typing over an automatic name replaces it |

**Merged with their conflicts resolved:**

| Upstream | Fix | How |
|---|---|---|
| 0d1fb19b | rain: keep the storm on Citadel, let script rain fall anywhere | RAIN_FALLS also needs `!RaceMod_FineWeather()`, as TEMPETE_ACTIVE does here (Citadel 2's fine weather) |
| 51b2ecfb | saves: name an overwritten automatic save afresh | upstream's MakeAutoSaveName in place of our inline copy |
| 567df6b2 | menu: repaint the full backdrop when opening Options | the console's `ui` usage string kept as ours |
| 9b01232f | render: sort the scenery with the 1997 game's qsort | QSORT.CPP added to LIB386/SYSTEM's list beside our ZIPREAD.CPP |
| da4e5271 | system: keep RetailQsort's pivot copy aligned | (on top of 9b01232f) |

**Ported by hand:**

| Upstream | Fix | Port |
|---|---|---|
| 093aaab6 | audio: join CD track paths with a separator | a separator in CD.CPP's BuildCDTrackPath where the folder has none at its end |
| 0510cebd, feb92518 | audio: close SDL audio before the process exits | `atexit(ClearAIL)` once audio is up (COMMON.CPP) |
| 1cdbe361 (part) | render: preserve backgrounds during holomap and save zooms | `Phys` allocated zeroed (calloc) in SVGA/SDL.CPP; the rest is upstream's platform layer |

**Already fixed here in our own way:**
- `15e84452` / `2c41154f` (a parked track resumed from the right frame): our STREAM.CPP counts the position from where the stream began, wrapped round a looping track.
- `7884083d` / `4d96049d` (the rounding mode after a triangle): our POLY.CPP saves and restores it.

**Checks:**
- lba2cc and lba2_renderer built.
- Raced: Citadel 1 (the storm rains), Citadel 2 (fine weather, no rain), Island CX Mk2 and Desert Island. No warnings.
- Story Mode new and continue; a save made and loaded headless.
- The race game's Options page opens with its whole backdrop, and the editor starts and draws Desert Island.

## Third round (2026-10-30): the input rework

Merged at the user's request ("Do the upstream large rewrite to port more fixes"), with the fixes that build on it:

| Upstream | Change | How |
|---|---|---|
| 5b1b7055 | input: separate UI navigation from gameplay bindings (MENU_INPUT.CPP/H; menus, inventory, memo, behaviour, holomap and resolution dialog read four UI directions) | Three-way merge with 3 conflicts. CONFIG.CPP keeps our Tab alias on the gamepad screen. GAMEMENU.CPP's save list takes upstream's `MenuInput_Get`. Menu() keeps our race game's window-fit relayout and redraw hook, but loses our pad-to-arrow translation, which the UI directions now do. |
| af624c82 | input: fixed controls for shared UI navigation | clean on top |
| d3df2dc7 | inventory: pause gameplay during the UI direction release | clean on top |
| 0bb3b871 | input: release the resolution dialog's navigation before gameplay | clean on top |

**Checks:**
- Every race game page driven from the keyboard:
  - the main menu (one row a press, from the first press);
  - Track select's code and its list (13 rows down);
  - Story Mode's code and its new/continue;
  - the car grid with 7777;
  - Options > Controls > keyboard.
- A race driven with the arrow keys accelerated and stopped, and the Auto pilot races ran.
- lba2.cfg is unchanged, so key remaps are kept.
- **Not tested:** a gamepad (none on this machine).
- A key pressed in the first seconds after the game starts can go unseen while the menu waits for every key to be up (upstream's release latch). The tests now wait 14 s, not 9.

## Fourth round (2026-10-30): the platform layer, audio and mouse

At the user's request ("Let's do the audio streaming and mouse fixes next"). It needed upstream's platform layer first: an SDK-free host contract (LIB386/H/PLATFORM, LIB386/PLATFORM/SDL3, SDL2 and NULL), with every engine call to SDL routed through `g_platform`.

**Merged in upstream order:**
- **Platform layer:** f500eca2, 676b768d, 9d5f2ed0, 1392f941, f7459f8f, 03cac739, 53629dc2, 5bef878e, 19884bdb, 3e0fb132 (the SDL2 backend and the shared audio device, AUDIO_DEVICE/SDK.H).
- **Present filter and scaled displays:** 5ec1e23b, 650e9fc2, e69b116e. Plus 4520596b, 68dc9ab6 and de946406 (SDL2).
- **Audio:** 241541ce, 7db97fe1, feb92518 / 0510cebd (now in upstream's own form), 093aaab6 (likewise), f93b6a9c, c8acd312 and 5c55353a (SDL2's bounded conversion).
- **Mouse:** ef01813b (taps on a touch screen), c383bcb5 (the cursor kept in the window, relative mode ended with the camera), cb1fed1b (relative mode kept across frames with no game step).

**Ours, kept and moved onto the platform layer:**
- **Window, in LIB386/PLATFORM/SDL3/WINDOW.CPP (the SDL3 backend; Window_* in LIB386/SYSTEM/WINDOW.CPP forward there):**
  - `LBA2_WINDOW_POS` (the editor's embedded window);
  - the race game's window title;
  - the settled resize, the windowed size and its restore (RES_SWITCH.CPP's window fit);
  - Window_SetVisible (RENDERER_API.CPP);
  - Window_IsRendererTextureLocked, now true while SDL3_Present has the texture locked and its overlays run.
- **SVGA/SDL.CPP:** the pre-frame callback (SetPreFrameCallback: the console's pending resolution switch) and the zeroed Phys.
- **KEYBOARD.CPP:** F11 / Alt+Enter, now read from the platform's event as SDL scancodes and modifiers.
- **Build:** CMake's winmm for MIDI, and SDL3 for the console.
- **STREAM.CPP:** our music on upstream's audio wrappers (AILAudio_*, AIL_Atomic*). That covers the position counted from where the stream began and wrapped round a looping track, our track looping (PumpStreamLoop, loopPcm) and the miss cache. Parking always has audio left, since our music loops.
- **CONFIG_FILE.CPP:** c383bcb5's `MouseConfine` is read and written as our file's other mouse settings are. Upstream keeps it in settings tables made for its recorder, which we don't have.
- **GAMEMENU.CPP:** back to upstream's `ade_strcasecmp`.

**Not taken, by choice:**
- The bounded WAV producer: bed475f9, 3cf55f5e, 1c14ec24, 887bf037, 8962debd, 873dccd5. It plays WAV music through its own producer, past our looping (race music such as the Temple's would stop looping).
- c87fd56a (CD and OGG streamed, not held whole). It replaces our OGG cache, miss cache and looping.

Both save memory, which a Windows build doesn't need.

**Checks:**
- Raced Island CX Mk2 and Desert Island, the frames drawn through the new present path, HUD and overlays included.
- Story Mode new and continue; the car grid and 7777.
- With audio on (master volume 0, music 1): the Desert's race music streamed and queued again behind itself as it should.
- The game's own window resized through Windows: the race game fitted its picture to it. F11 posted to it: full screen and back.
- The editor started, drew Desert Island, and played a scene inside its viewport.
- **Mouse**, driven for real later on 2026-10-30 (the user lent it). Three monitors, the game on the middle one:
  - The main menu's row lights under the pointer, and a click opens it (Track select's code page).
  - MouseConfine 1, the default: the cursor is free in a window. In full screen it stops at the game screen's edge (clip 0,0-1920,1080), and is freed again back in a window.
  - MouseConfine 2: windowed, it stops at the window's edge (clip = the client rectangle), and is freed when the game closes.
  - A race's right-drag orbit: relative mode while the button is held (the cursor clipped to a point), the camera's AddBetaCam turning (orbit 1); on release the cursor is free again. It ends up where the drag's motion left it, as with the engine before this round.
- Still not tested: a gamepad (none here).

## Fifth round (2026-10-30): the Auto camera

At the user's request ("then move on to the camera fixes"). Merged in upstream order:
- **Scripted shots:** 850caede, e701a120, e98cbb7c, 7a169760 (the camera given back after a shot); 17cb1f63, 7c84ef7e (blended into and out of one); c9aea5f7 (the camera the frame was drawn with handed back).
- **Scenes and zones:** 72513137, 598c75f9 (the camera carried into a new outdoor scene); 6551a795 (latching zones left unfired while the player steers).
- **Obstruction:** d6973b5a, 7811cb1b, ec2fade9, 476bad38, 48e6790d (eased clearance, and the hidden hero).
- **Defaults:** 8277fb02 (those settings shipped on).
- **Pacing:** 520c5f1f (the turn and the look-at point paced by the clock).
- **World anchor:** 0ecf09d7, 5998fe88 (off by default).
- **Rain:** bb0e8dd1, 58542424 (rain splashes kept through a blend, cleared on a cut).

**Ours, kept:**
- The race-track mode's hooks: FollowCamEyeHook, the own-eye ease, FollowCamDropOwnEye, FollowCamReapplyLift after FollowCamSetView, the arm percent, the eye turn and cut.
- Our camtrace line, with upstream's new fields added at its end (inshot, loan, obs, hid, dt, tlag, vx..sz).
- The dirty check with our FollowCamEyeCut.
- INTEXT.CPP: the classic obstruction search still off for the race-track mode's follow camera.

**The race camera kept as it was:**
- 520c5f1f's look-at point trailing the hero (cam_target_ms, 300 ms) put a race car's camera a third further back: Citadel 2's lap went from 11,574 from the car to 15,041.
- In the race-track mode the look-at point stays on the car (FollowCamEaseLookAt). Everything else is kept, including the clock-paced yaw ease (cam_ease_ms).
- **Measured** with camtrace over 40 s of autodrive, against the engine before this round (eye from the car, mean; the eye's largest step in a frame):

  | Track | Distance before | Distance after | Largest step before | Largest step after |
  |---|---|---|---|---|
  | Emerald Moon | 11,026 | 11,007 | 3,250 | 1,075 |
  | Citadel 2 | 11,574 | 11,577 | 8,467 | 1,565 |
  | Island CX Mk2 | — | — | 8,383 | 960 |

- Island CX Mk2's halls: the tunnel camera on every frame down there, as before.

**Not taken:** f00a446e and 477352a5 (the console's camera cvars bounded by, and started from, upstream's settings tables, which we don't have).

**Also checked:** Story Mode new (the dream) and continued, and the editor playing a scene in its viewport.

## Sixth round (2026-10-30): the integer 3D maths

At the user's request ("analyse the integer maths ... if there's none or minor differences it may be worth doing"). Merged in upstream order:
- **Distance:** 0063f18e (Distance2D/3D square in 64 bits, as DISTANCE.ASM did).
- **The integer path:** 374cf91b (the 1997 integer matrix, rotation and projection), e068589f (the exterior terrain, sea and sky drawn through it), ea3ec40e (projrec), 833b34ae (the integer inverse rotation), 15375c53 (a point at or behind the camera clipped).
- **The float path's width:** ddff2b9b, 2ea5c5dd, d62b4b11, 5c689ee5, 1370cccf (X87_REAL: long double on x86, as before; double elsewhere).
- **bf6379db** came without upstream's retail-fix settings (RETAILFIX.CPP): the integer terrain is always on, and `LBA2_TERRAIN_FLOAT=1` puts the float one back for comparing (TerrainSeamsFixed in TERRAIN.CPP).

**What it changes, measured** (headless 1280x720 frames of 13 race tracks, against the engine before this round):
- **With LBA2_TERRAIN_FLOAT=1:** the same picture. Only animated actors, rain and the Wannies lift film's letterbox differ, and the old engine alone differs on those from run to run.
- **The integer terrain:** 0.6-7 % of pixels move, all on the terrain, sea and sky: the dither lands on a neighbouring texel, which is invisible at 5x zoom. It does close the hairline seams between terrain cubes: Desert's dunes had dotted sky-blue lines along cube edges, now gone. No new seams were found.
- **Distance:** before this round, a distance past 46,340 (2D) or about 26,754 a side (3D) overflowed to nonsense, usually near 0. It feeds 46 calls: ambience falloff, life-script distance tests, extras' hits, and the race mod's opponent item sounds (a far opponent's item was heard at full volume).
- **Speed:** the same, about 4 ms a frame headless (Desert, Polar; three runs each).
- **Races:** `eng\run_all.ps1` on all 16 tracks: recorded, raced against and played back, with the same lap times and scene changes as the 2026-10-28 run.
- **Not covered:** Island CX Mk2. The test sandbox predates it, so neither engine can load it there.

## Seventh round (2026-10-30): cinematics streamed from the HQR

At the user's request ("What is cinematics from HQR? This sounds like something we should implement"). It was left by choice earlier the same day, before the films could come from the CD. The CD changed the case for it: each film used to be read whole before its first frame (the largest in VIDEO.HQR is 73 MB, 36 MB the next; 34 films, 223 MB), and from a real drive that is many seconds of black screen.
- **Merged:** `23677fb4`. VIDEO_SOURCE.CPP reads the film from its HQR entry as it plays (64 KB read-ahead, both HQR compressions), through the engine's file API, so disc images and the CD's path work as before. libsmacker reads through callbacks and no longer keeps its own copy of the frames.
- **Ours, kept:** a film on the CD that can't be opened is skipped, not the game ended. Upstream's new read error part way through a film ends that film (upstream). On the CD it also forgets the drive, so the next film looks for the CD again.
- **Not taken:** SYSTEM/LE.H, part of upstream's big-endian work. VIDEO_SOURCE.CPP has its own two little-endian readers.
- **Tested** with Story Mode's intro (film 16) in `review\lbra_story` (`eng\run_video.ps1`, now with `-EjectDuring <s>` and the peak memory):
  - From the installed disc image: the same frame at the same moment as before. Peak memory went from 207 MB to 62 MB.
  - From the LBA2 CD (the ISO mounted), the disc image set aside: plays, 62 MB.
  - The CD taken out 3 s into the film: "unable to read frame 104". The film ended and the dream began, with its dialogue.
  - No CD and no disc image: the film skipped, as before.

## Left for later

- **Not taken:**
  - Upstream's retail-fix settings (RETAILFIX.CPP and the settings tables): the one fix we wanted from it, the terrain seams, is on by itself here.
  - Not relevant here: Vita, Switch, Android and macOS ports, big-endian support, the recorder, CI, packaging and tools.

## How to check the next merge

1. Clone upstream and confirm the baseline: compare `git ls-tree -r` blob hashes with fb20c9c's `native/lba2-classic-community`.
2. Commit our current engine onto a branch from that base in the clone.
3. Cherry-pick each candidate onto it, in upstream order (`-X ignore-space-at-eol`; leave core.autocrlf as it is, or the working tree looks modified and every pick is refused).
4. Resolve what conflicts, then carry the source part over with `git diff ours HEAD -- . ':(exclude)docs' ':(exclude)tests'` and `git apply --directory=native/lba2-classic-community`.
5. Build `lba2cc` and `lba2_renderer`.
6. Race-test, save and load, and start the editor.
