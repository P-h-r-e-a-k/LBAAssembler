# Maps in files of their own: File > New map, Open map, Save map, Install map into game, Export map

The user (2026-10-15): "Do we have the ability to create new maps for both games? Perhaps add a File>New option then we can specify the type
ile, lba2 inside, lba1". Then: "Let's also add the ability to load new maps from a file instead of them having to be in a HQR file."

A **map** is one of three kinds:

| Kind | What it holds |
|---|---|
| LBA2 island | its ground and its objects (an ILE and an OBL), and an outside scene for each of its cubes |
| LBA2 interior | a scene and its grid of blocks, in one of the game's block libraries |
| LBA1 interior | a scene, its grid and its block library |

It lives in a file of its own, a **`.lbamap`** (`Maps/LbaMap.cs`), not inside the game's HQR files.

## The File menu

- **New map…** (`NewMapWindow`, `Maps/MapNew.cs`): the kind, a name, and the game's own map it takes its look from.
  - *An LBA2 island*:
    - Its file name (one to eight letters or digits) and the game island whose look it has: ground and sea textures, light, sky, music.
    - Its size, 1 to 6 cubes each way, and its land's height.
    - It is flat land with a beach all round sloping into the sea. Its flat ground, steep ground and sea are textured as the base island's most
      common ones, and its light is baked from the base island's sun.
    - Its OBL is the base island's, so that island's objects can be placed on it.
    - It has an outside scene for each cube, made from the base island's first outside scene, with Twinsen alone in the first cube and
      cube-change zones between the cubes.
  - *An LBA2 interior*: built from one of the game's interiors (its block library, light and music). It is 32 x 32 cells of that interior's
    floor, with Twinsen on it (`Lba2BlankScene`).
  - *An LBA1 interior*: built from an LBA1 scene slot (its island, music and block library), as a blank floor with Twinsen on it
    (`Lba1BlankScene`). LBA1 can't have scenes added, so installed into the game it **replaces that slot's scene**.
- **Open map…**:
  - Opens a `.lbamap`.
  - Also opens an LBA2 island (`.ILE`) from outside the game folder, with its `.OBL` from beside it, or the base island's. It is given an
    outside scene for each of its cubes.
  - File > Open (Ctrl+O) also takes both.
- **`--map <file>`** on the command line, or a `.lbamap` as the only argument, opens a map at start.
- **Save map / Save map as…** (Ctrl+S while a map is open): reads the map back out of its workspace (below) into its file. The island's
  unsaved ground is saved first.
- **Install map into game…**: puts the open map into the real game folder, after a question.
  - An LBA2 map is added as new scenes from 400 on.
  - An LBA1 map goes into its slot.
  - A map installed before is put back where it was: an island onto the scenes `ISLANDS.TXT` draws on its file, an LBA2 interior onto the
    map scene with its name and onto its grid.
  - Every file changed keeps a `.bak` (`FileTransaction`).
- **Close map**: asks to save unsaved changes, then goes back to the game folder.
- **Export map…**: the island or interior scene in view, out of the game folder into a `.lbamap`.

Map files are saved by default in the open map's own folder, else in `Documents\LBA Assembler\Maps`.

## The workspace

Opening a map makes a copy of the game folder, `<game folder>\_MAP_WORKSPACE`. This is `LiveDataRoot`'s mirror, as Test edits makes: its
files are hard links until written, and every write goes beside the file and is renamed over it, so the game's own files are never written.
The map is installed in it (`Maps/MapInstall.cs`), and it stands in for the game folder (`EditorSettings.TestModeActive`).

So the editor's island list, its scenes, the 3D view, Build mode, the scene, grid and actor editors, and Play all work on the map as on any
of the game's own. The window's title shows the workspace's folder.

Leaving the workspace:
- The process's working folder is moved out first, since the renderer's data root is set by `chdir`.
- If something still holds a file, it is removed again a little later.
- A workspace a crashed session left behind is removed at the next start, or at the next map opened.

## The `.lbamap` file

A zip:

- `map.json`, which holds:
  - the format (1) and the kind
  - the name and a description
  - the island number its scenes carry (LBA2: the game island whose sky, palette, music and holomap they borrow)
  - the slot (an LBA1 map's; an LBA2 interior's base scene)
  - an island's file name
  - its scenes' files and cubes
- `scene<k>.bin`: each scene's record as SCENE.HQR holds it. A zone into another of the map's own scenes names it `30000 + k`
  (`LbaMap.LocalScene`). The real numbers are given at install, and turned back at save.
- `grid.bin`:
  - LBA2: LBA_BKG.HQR's entry, with its 34-byte header (library, fragments, used blocks).
  - LBA1: LBA_GRI.HQR's entry.
- `library.bin`: LBA1's LBA_BLL.HQR entry.
- `island.ile`, `island.obl`: an island's files.

## Installing into a game folder (`MapInstall`)

- **LBA2 scenes** are appended to SCENE.HQR from scene 400 on, past the game's own and the race tracks' (222-300). SCENE.HQR is padded with
  empty entries up to them, entry 0 keeps the biggest scene's size, and they are named in the folder's SCENE.HQD.
- **An LBA2 interior's grid** is inserted into LBA_BKG.HQR after its last grid:
  - The grid fragments, block libraries, bricks and the scene-to-grid table after it each move up an entry.
  - Entry 0 says where they now start, and its biggest-grid size is raised if needed.
  - The table names the scene's new grid.
  - Grids are numbered with a byte, so there can be 256. The retail file has 149.
- **An LBA2 island's** ILE and OBL are written under its file name. The game folder's **`ISLANDS.TXT`** gets:
  - `island <file> <number>`: whose look it has (the editor's renderer and palette)
  - `scene <number> <file>`: each of its scenes (the engine draws them from that file)
- **LBA1**: the scene, grid and library entries of its slot are replaced.

## The engine (`native/lba2-classic-community`)

- `EXTFUNC.CPP` `SceneIslandFile`: reads `ISLANDS.TXT` once. A scene listed there is drawn from its own island file instead of its island
  number's.
  - The island is loaded again when the file changes, not just the number: a map island's scenes share their number with the base
    island's.
  - Its sky, palette, music and holomap are still its number's.
- `RENDERER_API.CPP` `lba2_renderer_load_island`: an island name the engine doesn't know takes its look (sky, palette) from its
  `island <file> <number>` line.
- `DEFINES.H` `MAX_GRID_SCENES` 1024: LBA_BKG.HQR's scene-to-grid table (`TabAllCube`, 256 retail) can name interiors past scene 255.
  `InitGrille` guards the index.

## In the editor

- The scene list puts a scene that `ISLANDS.TXT` draws on an island file of its own under that file (`BuildSceneEntries`).
- The island's palette is its look's (`LoadIslandPalette`, `IslandMapRenderer.LoadPalette`): RESS.HQR entry 27 + its island number.

## Tests

`ScriptRoundTrip`:

- `mapnew lba2interior|lba1interior|lba2island ...`
- `mapexport lba2interior|lba1interior|lba2island ...`
- `mapinstall <map> <folder>`: installs the map, reads it back, and says whether it is the same map.
- `mapinfo <map>`

On sandboxes (2026-10-15):

- An LBA2 interior built from scene 4 became scene 400, grid 149. Played headless (`cube 400`), Twinsen stands on its floor.
- A 2 x 2 island built from Citadel became MYISLE, scenes 401-404. Played headless (`cube 401`), Twinsen stands on its land with the sea
  beyond, in Citadel's rain.
- An LBA1 interior from slot 28 was installed and read back the same.
- Scene 5, MYISLE and LBA1's scene 1 were exported.
- Installing again replaced the earlier copy.
- The UI (UI Automation on a sandbox): New map for all three kinds, `--map`, Save map as, Install map into game, Close map.

## Limits

- LBA1's scenes can't be added to: an LBA1 map takes a scene slot of the game.
- An LBA2 map island's scenes have no place on the holomap, and the renderer shows no actors on it in the 3D view outside Play: its scenes
  carry the base island's number.
- A race track build puts SCENE.HQR back from its own copy of the originals (`.before-racetrack`), so maps installed after a race track
  build go with the next one. Install them again after it.
- Islands are made from the game's own islands: those with an OBL beside their ILE.
