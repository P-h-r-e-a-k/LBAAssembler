# The engine and its upstream

`native/lba2-classic-community` is a copy of [LBALab/lba2-classic-community](https://github.com/LBALab/lba2-classic-community).
It is not a submodule or subtree, and it has been changed heavily for the editor and the race game: RACEMOD, RACEGAME, FOLLOWCAM, the menus, audio, the renderer API.

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

## Left for later

- **Needs upstream's input rework (`5b1b7055`):** `0bb3b871` (the resolution dialog's navigation release) uses its `WaitNoMenuInput`.
- **Conflicts with our changes, to port by hand if wanted:**
  - Audio streaming and music park/resume: `c87fd56a`, `bed475f9`, `c8acd312`, `15e84452`, `2c41154f`, `74368c49`, `873dccd5`. Our LIB386/AIL/SDL STREAM.CPP and CD.CPP differ.
  - Cinematics streamed from HQR: `23677fb4`.
  - Saves: `1a167f16`, `51b2ecfb`, `6a90417e`.
  - Rain: `0d1fb19b`, `58542424`, `bb0e8dd1`. Our storm and race-track rain differ.
  - Rendering precision: `4d96049d`, `7884083d`. SVGA clipping: `c6e57701`. Scenery qsort: `9b01232f`, `da4e5271`.
  - Input and menus: `c383bcb5`, `cb1fed1b`, `af624c82`, `d3df2dc7`, `567df6b2`, `ef01813b`.
- **Not taken:**
  - Too large to take now: the platform layer rework (SDL2/SDL3 host contract, `f500eca2`, `3e0fb132`, `5bef878e`); the integer 3D maths port (`374cf91b`, `833b34ae`, `e068589f`, `bf6379db`); and the Auto camera rework (`520c5f1f`, `5998fe88`, `8277fb02` and the other camera commits), which FOLLOWCAM's race camera depends on.
  - Not relevant here: Vita, Switch, Android and macOS ports, big-endian support, the recorder, CI, packaging and tools.

## How to check the next merge

1. Clone upstream and confirm the baseline: compare `git ls-tree -r` blob hashes with fb20c9c's `native/lba2-classic-community`.
2. Run `git format-patch -1 <hash>` for each candidate.
3. Check each with `git apply --check --directory=native/lba2-classic-community --exclude=*/docs/* --exclude=*/tests/* --exclude=*.md`.
4. Apply in upstream order.
5. Build `lba2cc` and `lba2_renderer`.
6. Race-test, save and load, and start the editor.
