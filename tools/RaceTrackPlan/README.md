# Race track planning scripts (Desert island, 2026-09-27)

The Python (numpy, scipy, scikit-image, opencv, PIL; `pip install` into a venv) that turned the picture of the proposed track into the plan the C# builder reads. They were run from `E:\dump\TEMP` and still use that folder and the picture's original path; change the paths at the top when reusing them. Method and results: [docs/LBA2_DESERT_RACE_TRACK_BUILD.md](../../docs/LBA2_DESERT_RACE_TRACK_BUILD.md).

Order: `ile.py` / `basemap.py` (read DESERT.ILE, draw the island top-down; helpers) - `landmarks.json` + `pic3.py` (register picture to island) - `pic5.py`, `pic6.py`, `pic7.py` (road skeleton, centre line by waypoints, warp to island cells) - `plan5.py` (fit the line to the real ground) - `plan_export.py` (writes `track_plan.json`, also the pit lane's two ends) - `overlay_final.py` (built route over the picture). `hshot.ps1` and `cellinfo.py` take engine screenshots of the built copy; `drive.ps1` drives the buggy (both headless and muted, on the sandbox copy).

The jump proposal: `jumpsite.py` searches the built lap for stretches straight enough for a 23-cell flight inside one cube (it leaves out the bridge, the start, the pit ends and the water bridge; writes `E:\dump\TEMP\jumpsites.json`), and `jump_proposal.py` draws `docs/racetrack/build/jump_proposal.png` from a built copy (`E:\dump\TEMP\rt4_game`, its top-down render `E:\dump\TEMP\rt4_built.png`) and the pristine island.

The race-track mode and the jump (2026-09-28): `gears.ps1` (Twinsen in the buggy on the start line, the speed while it shifts gear at set ticks, with a car setup file) and `jumpdrive.py` (drives a jump build over the jump from a parked buggy and prints the car's path through the flight). `hshot.ps1` and `drive.ps1` take `-car <file>` for the engine's race-track mode.

Cube edges (2026-09-29): `edgetest.py <game> <scratch> [island byte] [first scene] [last scene]` walks Twinsen over every place the built lap crosses a cube edge, both ways, in the engine, and reports the scene he ends in -- a crossing with no cube-change zone is an invisible wall. Citadel Island: `0 42 50`, the Desert island: `2 55 73`.
