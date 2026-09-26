# The proposed race track, built on the Desert island (2026-09-27)

The picture the track came from is [racetrack/concept_track.png](racetrack/concept_track.png). It is built on a **copy** of the game, so nothing in the real game folders is touched.

| | |
|---|---|
| Where the copy is | `E:\dump\LBA2RaceTrackBuild\Game` (a plain copy of the "Level viewer" install without the CD images, `DOSBOX`, `DRIVERS` and the intro video except `VIDEO\VIDEO.HQR`, which the engine needs) |
| The originals of the three files that change | `E:\dump\LBA2RaceTrackBuild\Pristine` (`DESERT.ILE`, `DESERT.OBL`, `SCENE.HQR`); a build always starts by copying them back over the copy, so it can be repeated |
| Files the build changes | `DESERT.ILE` (heights, ground triangles, decor objects) and `SCENE.HQR` (scenes 55-73). `DESERT.OBL` is not changed. |
| To look at it | LBAAssembler > File > Settings > LBA2 folder = the copy, then Desert island (Explore), or Play any of scenes 55-73; Scene 67 starts on the start line |
| To rebuild | `ScriptRoundTrip buildtrack docs\racetrack\track_plan.json E:\dump\LBA2RaceTrackBuild\Pristine E:\dump\LBA2RaceTrackBuild\Game out.png 4` |

Pictures (all from the built copy): [top-down map](racetrack/build/built_map.png), [the built route over your picture](racetrack/build/built_over_concept.png) (cyan is the built lap), in the game: [start line](racetrack/build/h_start.png), [water bridge](racetrack/build/h_bridge.png), [crossing bridge](racetrack/build/h_cross.png), [pit lane entry](racetrack/build/h_pit_entry.png) and [exit](racetrack/build/h_pit_exit.png).

## What was built

- **The lap**: 1222 cells (about 626,000 world units) long, 9 cells wide with one cell of red/white curb each side, the same widths as the retail Desert track. The centre line comes from the picture (see "Where it goes").
- **Ground**: the ground under the road follows the natural height, smoothed and limited to a 9 % climb, and is levelled across the road with an embankment of about 7 cells each side that blends into the ground around. 28,407 vertices were changed.
- **Banking**: each bend's outside edge is raised, by a cross slope of 700 / turn radius in cells (at most 70 height units per cell, about 8 degrees).
- **Textures** (the ones catalogued in [LBA2_DESERT_RACE_TRACK.md](LBA2_DESERT_RACE_TRACK.md)): asphalt tile (96,0), the red curb (flat bank 4) alternating with the white curb texel (180,155), 14 orange arrow shapes (flat bank 5) after bends and every 150 cells on straights, the red/gold hatch tile (192,48) on the outer verge of bends, sand on the rest of the verge, the white start line row.
- **Pit lane**: 137 cells beside the bottom straight of the map (the yellow line of the picture), 5 cells of asphalt with its own curbs, tapering onto the lap at both ends, 10.5 cells from its centre line.
- **Start line**: a one-cell white row across the road at the middle of the pit lane, with the retail gantry (bodies 64 + 65 + 66, checkered beam and red posts) over it, turned to the road.
- **Bridge over the water**: the lap crosses a corner of the harbour by a raised deck (31 cells, 700 units above the sea at least) with steep rock sides that are blocked (the Col bit), so a car stays on it.
- **Bridge over the lap**: where the lap crosses itself (one place, at 17 degrees) it is a **level crossing**, with a viaduct of three arched decks and two abutments (retail bodies 68, 69, 70, the retail overpass) standing over it.
- **Decor**: 111 plants, posts, fences and small props on the road were removed (the set of body numbers is `RemovableBodies` in the code). The route was planned around every house and rock, so no solid decor was in the way. The retail Desert track (cube 7,10) is untouched.
- **Scenes** (55-73, every outside scene of the island): all actors removed except Twinsen, the buggy and slot 1 (see below); the light is re-baked along the road.
- **Twinsen and the buggy** start in scene 67 next to the start line, facing the way the lap runs.

## Things worth knowing

- **The crossing is level.** The engine's ground is one height map (`CalculAltitudeObjet`) and only the ground counts for the car, so a road cannot pass *over* another road. The arches over the junction are scenery; the car drives under them. A jump ramp over the other road is the only way to get a real grade separation; not done.
- **The buggy is one object for the whole game** (BUGGY.CPP): it exists in the cube that INIT_BUGGY last put it in. The island's scenes run `INIT_BUGGY(0)`, which only shows it where it already is, and every scene deletes it (SUICIDE) until the car quest (game variable 74) reaches 3. For the track the copy's scripts were changed: the quest test now always passes (`IF VAR_GAME(74) >= 0`), and scene 67 uses `INIT_BUGGY(2)` so the buggy is put on the start line whenever that scene starts.
- **Actor slot 1 is kept.** Every scene has a bodiless "Zoe" placeholder in slot 1 (entity 14, at 0,0,0). With it deleted, the buggy moved into slot 1 and came up with no life; with it kept the buggy's state matches the retail one exactly. So "all actors but Twinsen and the buggy" is that plus this hidden slot.
- **Not touched**: the demo copies of these scenes (198-206), the interiors, the texts and everything outside these three files. Zones on the road were checked (doors, hit, ladder, escalator, grid, rail): none lie on it.
- **The hairpin near the retail track** keeps about 4 cells between them.
- **Where the track differs from the picture**: the picture is a hand-painted view of the island, not a map, so the lap was fitted to the real island: on average 3 cells from the drawing (90 % of it within 7 cells, at worst 17 cells), to go round the town's houses, the fortress and the big rocks. See the overlay.

## Where it goes (how the picture became coordinates)

1. The picture is the island seen from the other side (turned 180 degrees, tilted, and stretched), so it was registered to the island's own top-down map by 15 landmarks (the islets, the oasis, the harbour, the town, the retail track...) with a thin-plate spline: [tools/RaceTrackPlan/landmarks.json](../tools/RaceTrackPlan/landmarks.json), checked by [pic3.py](../tools/RaceTrackPlan/pic3.py).
2. The road was cut out of the picture by colour (lime + red "Bridge" label), skeletonised and followed along the skeleton between hand-placed waypoints ([pic5.py](../tools/RaceTrackPlan/pic5.py), [pic6.py](../tools/RaceTrackPlan/pic6.py), [pic7.py](../tools/RaceTrackPlan/pic7.py)); the yellow pit lane gave its two ends.
3. The centre line was fitted to the real ground ([plan5.py](../tools/RaceTrackPlan/plan5.py)): cheapest path within 28 cells of the drawing, where houses, rocks, cliffs, the sea and the retail track cost a lot, then a relaxation that keeps different parts of the track 14 cells apart except at the one crossing. Result: [racetrack/track_plan.json](racetrack/track_plan.json) (island cells counted from cell 448,448).
4. `Terrain/RaceTrackBuilder.cs` (ground, painting, decor) and `Terrain/RaceTrackScenes.cs` (scenes) build it; `tools/ScriptRoundTrip/RaceTrackCommand.cs` is the command line (`buildtrack`, `herostart`).

## Checking it without the editor

`tools/RaceTrackPlan/hshot.ps1 -cube 67 -tp "x y z"` starts the engine without a window on the copy, teleports Twinsen and saves a screenshot and a state dump (the actors' positions and life); this is how the pictures above and the buggy fault were found. `ScriptRoundTrip racetrack island 2` lists the island's scenes, `racetrack 9 10 --scripts` a cube's actors and scripts.

## Not done / next

- Nothing has been driven all the way round yet (the level crossing, the pit lane taper and the water bridge were looked at from Twinsen's feet).
- A Tools menu command in the editor to build a track from a plan (the builder is in the app's own sources already).
- Pillars under the water bridge, a hatch tile check against the retail one for the outer verge, and a real jump over the crossing.
