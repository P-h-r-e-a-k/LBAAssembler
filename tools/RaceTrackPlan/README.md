# Race track planning scripts (Desert island, 2026-09-27)

The Python (numpy, scipy, scikit-image, opencv, PIL; `pip install` into a venv) that turned the picture of the proposed track into the plan the C# builder reads. They were run from `E:\dump\TEMP` and still use that folder and the picture's original path; change the paths at the top when reusing them. Method and results: [docs/LBA2_DESERT_RACE_TRACK_BUILD.md](../../docs/LBA2_DESERT_RACE_TRACK_BUILD.md).

Order: `ile.py` / `basemap.py` (read DESERT.ILE, draw the island top-down; helpers) - `landmarks.json` + `pic3.py` (register picture to island) - `pic5.py`, `pic6.py`, `pic7.py` (road skeleton, centre line by waypoints, warp to island cells) - `plan5.py` (fit the line to the real ground) - `plan_export.py` (writes `track_plan.json`, also the pit lane's two ends) - `overlay_final.py` (built route over the picture). `hshot.ps1` and `cellinfo.py` take engine screenshots of the built copy.
