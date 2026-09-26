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
- **Where the lap crosses itself** (one place): a **jump**. The straighter of the two roads is turned near the crossing until they meet at 65 degrees (it was 17 in the picture, too shallow for a 17-cell jump to clear anything), and a strip of scenario zones across it, 8.75 cells before the crossing, sends the buggy flying over the junction: 17.5 cells, up 1900 units and down, landing on the same road. (Options: a viaduct of three arched decks and two abutments, retail bodies 68-70, over a level junction, or nothing.)
- **Decor**: 111 plants, posts, fences and small props on the road were removed (the set of body numbers is `RemovableBodies` in the code). The route was planned around every house and rock, so no solid decor was in the way. The retail Desert track (cube 7,10) is untouched.
- **Scenes** (55-73, every outside scene of the island): all actors removed except Twinsen, the buggy and slot 1 (see below); the light is re-baked along the road.
- **Twinsen and the buggy** start in scene 67 next to the start line, facing the way the lap runs.

## Things worth knowing

- **The crossing is a jump.** The engine's ground is one height map (`CalculAltitudeObjet`), so a road cannot pass *over* another road, and a ramp bridge is impossible. The Desert island's own **car jump** (scene 62) shows how the game does it, and the track uses the same thing (see "How the retail jump works"). A viaduct of arches over a level junction and a plain level junction are still offered (Crossing style in the menu dialog / `CrossingStyle`).
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

- Nothing has been driven all the way round yet (the jump, the pit lane taper and the water bridge were driven/looked at in short stretches).
- Pillars under the water bridge, and a hatch tile check against the retail one for the outer verge.
- A jump over water for the harbour crossing (the buggy flies 17.5 cells; the deck is 31 long).

## How the retail jump works (found 2026-09-27, scene 62 "near car jump")

There is no jumping in the engine's physics: the buggy follows the ground height map, and an object that falls (`FALLING`) only moves straight down. The retail jump is a **scripted flight**:

1. The **hero's own life script** (scene 62, actor 0) checks, every frame, `IF COMPORTEMENT_HERO == 12` (driving the buggy) and `IF ZONE == 1` (Twinsen is in a scenario zone numbered 1, the take-off) and `BETA` within about 45 degrees of the zone's direction (`ZONE == 2` and the opposite direction for the return jump).
2. Then it does `SET_DIR(MOVE_BUGGY)` (movement 12: the object is moved by its animation and its track, not the keys), `SET_TRACK(label 0)` and switches to a waiting behaviour.
3. The hero's **track script** label 0 is `ANIM(67); WAIT_ANIM; ANIM(0)`, then `LABEL(1); STOP`. Generic animation 67 of Twinsen is `ANIM.HQR` entry 51: 18 keyframes, 1780 ticks, every keyframe with the "master" bit set (no gravity), a root translation that adds up to **8990 units forward** (17.5 cells), climbs **1921** and ends 201 below the start.
4. When the track reaches label 1 the waiting behaviour does `SET_DIR(MOVE_BUGGY_MANUAL)` (13): the player drives again.

Measured in the engine (headless, `tools/RaceTrackPlan/drive.ps1`): the flight runs along the buggy's heading from the point of entering the zone, 17.5 cells long, Y up to +1892, and lands wherever that is: if the ground there is higher it is lifted to it (the retail landing is on a plateau 265 higher and comes out 7383 long).

In the retail scene the two plateaus are 4400 to 5200 high with a canyon between (heights down to 200), zone 1 is a 4 x 3 cell box on the west plateau's edge, zone 2 the same on the east one. The buggy's own script (actor 5) sets game variable 168 while the buggy is in zones 2-5 so the tour garage (scene 57) takes the buggy back when you leave it there.

### How the track does it

`Terrain/RaceTrackScenes.cs` (`AddJump`): instead of editing each scene's own long hero script, a small controller actor (entity 16, no body) is added to the scene the take-off lies in, with the same three checks (`zone_obj(0)`, `beta_obj(0)`, `comportement_hero`) and `set_dir_obj(0, 12)` / `set_track_obj(0, label_90)`; the hero's track script gets `label(90); beta(<heading>); anim(67); wait_anim(); anim(0); label(91); stop();` (the `beta` sets the heading of the road first, so the flight goes along the road whatever the car's heading was within the window); the controller gives the keys back when label 91 is reached. The take-off strip is a set of scenario zones numbered 40 (added at the end of the zone list, so they win where zones overlap), 3 cells deep and as wide as the road, cut to boxes from the road's own cells. `RaceTrackBuilder.SteepenCrossing` turns the road so the crossing is steep, and `PlanJump` places the strip so the flight's middle is over the crossing.

## The menu command

Tools > LBA2: Desert island race track... (`RaceTrackWindow.cs`, `Terrain/RaceTrackService.cs`): builds the track (the plan is built into the program, or a plan file), with the crossing style and the scene options as tick boxes; the first build keeps `DESERT.ILE.before-racetrack` and `SCENE.HQR.before-racetrack` beside the originals, every build starts from those, "Put the original files back" restores them. The editor's views are refreshed afterwards. Tested on a fresh copy of the game folder: the result is byte-identical to the command-line build, and restoring gives back the original files' hashes. ![dialog](racetrack/build/menu_dialog.png)

Extra commands in `ScriptRoundTrip`: `driveprep <game> <cellx> <cellz> <turn>` (buggy and Twinsen ready on the road at a cell), `initbuggy <game> <scene>`, `scripttext <game> <scene> <actor> life|track` (a script as the editor's C text), `racetrack island <n>` and `racetrack <cx> <cz> --scripts --hero`.
