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

## Left for later

- **They need an upstream rework we haven't taken:**
  - The input rework `5b1b7055` (separate UI navigation from gameplay bindings, 29 files). With it would come `0bb3b871`, `d3df2dc7` and `af624c82` (`WaitNoMenuInput`).
  - The Auto camera rework: `58542424` and `bb0e8dd1` (rain splashes on a camera cut or blend: FollowCamBlendCut, FollowCamBlending).
  - The platform layer (LIB386/PLATFORM, AUDIO_DEVICE, WAV_STREAM, SDK.H, MOUSE_CAPTURE):
    - audio streaming `c87fd56a`, `bed475f9`, `c8acd312`, `873dccd5`, `887bf037`, `241541ce`, `3cf55f5e`, `1c14ec24`, `f93b6a9c`;
    - input `c383bcb5`, `cb1fed1b`, `ef01813b`;
    - settings `a1783394`, `32ebffab`.
- **Not taken, by choice:** `23677fb4` (cinematics streamed from the HQR). It changes libsmacker and our video loading. Its gain is memory, which a Windows build doesn't need, and the risk is to Story Mode's intro film.
- **Not taken:**
  - Too large to take now: the platform layer rework (SDL2/SDL3 host contract, `f500eca2`, `3e0fb132`, `5bef878e`); the integer 3D maths port (`374cf91b`, `833b34ae`, `e068589f`, `bf6379db`); and the Auto camera rework (`520c5f1f`, `5998fe88`, `8277fb02` and the other camera commits), which FOLLOWCAM's race camera depends on.
  - Not relevant here: Vita, Switch, Android and macOS ports, big-endian support, the recorder, CI, packaging and tools.

## How to check the next merge

1. Clone upstream and confirm the baseline: compare `git ls-tree -r` blob hashes with fb20c9c's `native/lba2-classic-community`.
2. Commit our current engine onto a branch from that base in the clone.
3. Cherry-pick each candidate onto it, in upstream order (`-X ignore-space-at-eol`; leave core.autocrlf as it is, or the working tree looks modified and every pick is refused).
4. Resolve what conflicts, then carry the source part over with `git diff ours HEAD -- . ':(exclude)docs' ':(exclude)tests'` and `git apply --directory=native/lba2-classic-community`.
5. Build `lba2cc` and `lba2_renderer`.
6. Race-test, save and load, and start the editor.
