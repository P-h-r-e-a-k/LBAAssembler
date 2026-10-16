# Race track stress test and car setups (2026-10-16)

The user: "Stress test all of our tracks and suggest car setup tweaks for each track, perhaps tracks with long straights would favour
cornering and tracks with tight corners better turning."

All 18 tracks were built together from the pristine data (`ScriptRoundTrip buildtogether`, 378 s) into `E:\dump\TEMP\stress\game` and
driven headless by the test pilot (`autodrive`). The scripts are in `tools/RaceTrackStress/`:

- `stress.py files` writes each track's race file (`racecarfile`, `RT_RACE=<island>`) with the standalone game's car appended: five gears to
  45/57/63/97/110 km/h, acceleration, braking and rolling 300 %, steering 200 %, automatic, no qualifying lap, three laps.
- `stress.py plan` scores setups with the engine's own speed planner (the console's new `raceplan`, below).
- `stress.py pilot` drives four races per track (the base car on the racing line, the base car 1.5 cells to the left of it, a hot car with
  everything +20 %, the suggested setup), with the opponents and power-ups; `report.py` reads them up to the finish, with a position
  trace (`racecars` every 5 s) to find cars standing still; `repro.py` repeats one with screenshots and state dumps.
- `stress.py trial` drives clean time trials (no opponents, no power-ups) of the base car and the suggested one.

## How a setup is scored: `raceplan`

The engine plans every line's speeds from the player's car (RACEMOD.CPP `PlanSpeeds`): a bend of radius R at up to the steering's turn
rate times R, no faster than the top gear, braking into bends with the brakes and pulling away through the gears. The new console
command `raceplan <top%> <accel%> <brake%> <steer%>` (and `raceplan file <setups> <out>`) re-plans the first opponent's line (or the
guide) for the race file's car scaled by those percentages, at a neutral character, and returns its perfect lap and where its time goes:
flat out (at the top gear's speed), round the bends (at the steering's limit) and between (braking and pulling away). 236 setups take
1.5 s per track. "Top speed" scales every gear, as the Race car window's top speed slider does, so a higher one also pulls less hard.

Two grids: every attribute 70-130 % with all four summing to 400, and -- since the brakes turned out worth almost nothing -- the brakes
left at 100 % and top speed, acceleration and steering (70-130 % in steps of 5) summing to 300: a setup "budget", so a gain in one costs
another. The best of the second grid is each track's suggestion.

## What each track rewards

Time shares are the base car's planned lap. The three middle columns are the lap time saved by 10 % more of one attribute alone. The
suggestion is in the Race car window's terms (base: top speed 110 km/h, acceleration 300 %, braking 300 %, steering 200 %). Planned and
driven laps are base -> suggested; driven is the mean of three clean laps (the sprints: the run).

| track | flat out | bends | +10 % top | +10 % accel | +10 % steer | top speed | accel | steering | planned lap (s) | driven lap (s) |
|---|---|---|---|---|---|---|---|---|---|---|
| Desert island | 34 % | 6 % | 0.99 | 0.61 | 1.64 | 110 km/h | 210 % | 260 % | 50.4 -> 47.4 | 48.0 -> 46.0 |
| Citadel, storm lap | 23 % | 9 % | 0.16 | 0.14 | 0.45 | 110 km/h | 210 % | 260 % | 11.1 -> 10.3 | 11.0 -> 10.5 |
| Citadel, town circuit | 21 % | 6 % | 0.50 | 0.50 | 1.62 | 110 km/h | 210 % | 260 % | 39.6 -> 36.5 | 38.7 -> 36.3 |
| Mosquibees Island | 41 % | 4 % | 0.53 | 0.16 | 0.58 | 116 km/h | 210 % | 250 % | 17.5 -> 16.3 | 17.6 -> 16.3 |
| Celebration Island | 2 % | 22 % | 0.07 | 0.23 | 1.09 | 104 km/h | 225 % | 260 % | 17.8 -> 15.5 | 17.2 -> 15.2 |
| Celebration, lava lake | 0 % | 15 % | 0.02 | 0.16 | 0.70 | 104 km/h | 225 % | 260 % | 12.2 -> 10.6 | 12.8 -> 11.1 |
| Elevator Platform | 3 % | 20 % | -0.01 | 0.11 | 1.03 | 110 km/h | 210 % | 260 % | 14.2 -> 11.9 | 13.0 -> 11.3 |
| Sendell's Well | 0 % | 17 % | 0.05 | 0.04 | 0.30 | 94 km/h | 255 % | 260 % | 4.6 -> 3.7 | 4.3 -> 3.6 |
| The old moon | 0 % | 44 % | -0.01 | 0.12 | 0.41 | 94 km/h | 255 % | 260 % | 12.9 -> 12.1 | 13.1 -> 12.5 |
| Polar Island (sprint) | 71 % | 3 % | 3.40 | 0.26 | 0.81 | 143 km/h | 210 % | 200 % | 56.4 -> 49.6 | 55.8 -> 50.9 |
| Island of the Francos | 48 % | 12 % | 1.38 | 0.26 | 1.77 | 116 km/h | 210 % | 250 % | 48.4 -> 44.0 | 39.2 -> 35.8 |
| Otringal | 18 % | 16 % | 0.20 | 0.37 | 1.29 | 110 km/h | 210 % | 260 % | 28.4 -> 26.0 | 29.1 -> 26.5 |
| Volcano Island | 19 % | 23 % | 0.26 | 0.23 | 1.29 | 116 km/h | 210 % | 250 % | 24.7 -> 22.2 | 24.9 -> 22.3 |
| The Emerald Moon | 21 % | 23 % | 0.35 | 0.27 | 1.09 | 110 km/h | 210 % | 260 % | 25.3 -> 23.1 | 24.6 -> 22.5 |
| Wannies Island | 47 % | 6 % | 0.76 | 0.19 | 0.72 | 116 km/h | 210 % | 250 % | 23.4 -> 21.7 | 22.2 -> 23.9 |
| The Emperor's palace (sprint) | 66 % | 20 % | 2.09 | 0.08 | 1.07 | 126 km/h | 210 % | 230 % | 40.3 -> 35.8 | 37.7 -> 34.7 |
| Island CX | 0 % | 12 % | 0.08 | 0.31 | 1.01 | 99 km/h | 240 % | 260 % | 20.2 -> 17.8 | 21.5 -> 19.5 |
| The Temple of Bù | 71 % | 7 % | 3.00 | 0.15 | 1.17 | 132 km/h | 210 % | 220 % | 55.3 -> 49.1 | 55.2 -> 48.9 |

Braking is left out of the table: 10 % more saves at most 0.04 s anywhere. At 300 % the brakes stop the car in a few metres, so the
plan barely brakes; they matter only to a driver reacting late.

What it says:

- **Long straights want top speed** (the user's guess, with "cornering" read as top speed): Polar Island, the Temple of Bù and the
  Emperor's palace spend two thirds of the lap flat out, and 10 % more top speed is worth 2-3.4 s there -- several times what steering is.
- **Tight tracks want steering, and give up top speed**: Celebration Island, the lava lake, the Elevator Platform, Sendell's Well, the old
  moon and Island CX never reach the base car's 110 km/h for long (0-3 % of the lap flat out); steering is worth ten times top speed there,
  and their suggestions trade top speed away (94-104 km/h) for steering and a little acceleration.
- **The rest are steering first, top speed second** (Desert, both Citadel tracks, Otringal, Volcano, Emerald, Francos, Mosquibees, Wannies).
- **Acceleration is the base car's surplus**: at 300 % it is what every suggestion gives up first.
- The driven laps follow the plan to within a second except on Wannies Island, whose laps scatter (17-27 s: the frog's mouth timing,
  the branches), and Island CX and the Francos, whose hazards make single laps noisy.

**A catch for the game**: the opponents drive the player's own car as the setup makes it (RACEMOD.CPP `PlanSpeeds`, "Each drives the
player's own car"), so a better setup makes the whole field faster and the race no easier. For per-track setups to matter in a race, the
opponents would need a car of their own (the track's reference setup).

## What the stress runs found

72 races and 36 time trials: no crashes, no game-clock warnings, no missed checkpoints, every race finished (the storm lap has no
opponents, only Raph's time to beat, so it never "ends"). The trouble was all off the racing line, where the AI never drives but a player
will:

1. **Otringal: the pit wall's blunt end.** A row of thin decor boxes (body 246, 0.4 x 2 cells, 944 high) runs along the start straight
   at island cells x 497.1-497.5, z 470.8-504.0, 1.6 cells left of the deck's middle. A car on that line drives into its end face at z 504
   head-on and stays there, wheels spinning at 110 km/h (55 s until the run ended). Wants a flared or angled leading end.
2. **Celebration Island: the start gantry's left post on the kerb** (cells about 458, 476). A car 1.5 cells left of the line runs into it and
   is held (90 s, never freed). Another stop of 30 s at cells (485, 474) in one race freed itself.
3. **The old moon: the same, its start gantry's left post** (cells about 532, 526), 65 s.
4. **Citadel town circuit: a brick building's corner at the road's edge** (cells about 512, 608, between checkpoints 14 and 15). Off the line
   the car wedges into it (15-30 s, and once for the whole race with the suggested setup).
5. **The Emerald Moon: one cube edge with no crossing zone**, from cube (7,8) into cube (7,7) at x 4,800-5,700, height 7,700, every lap of
   every run. The engine's fallback carries the car over (RACEMOD.CPP `HeldAtEdge`), so nothing shows, but the build should add the zone.
6. One race of the lava lake had a rescue out of the lava, which worked; in the first, untraced desert run Twinsen's car stopped for good
   after a jump flown at 2.74x its speed and a lightning hit -- not seen again in the four traced desert runs.
7. The old moon's "off the ring" lines are its first loop's gap, leapt on purpose, not failures.

The test pilot never reverses, so a car held against a wall stays there. A player can back out, but a stuck-car rescue (held still with the
throttle down for a few seconds: back on the road, as from the lava) would make all four holds harmless.

## The fixes (the same day)

The user: "Make the fixes and add the stuck car reset".

- **The stuck-car reset** (RACEMOD.CPP `StuckCar`): Twinsen's car that goes no further than a cell in 3 s while it drives on -- the
  throttle down, or its own speed still over 1,000 units/s (a car held against something keeps its speed, 70-90 km/h on the display,
  and the test pilot, which drives by that speed, lets go of the throttle near the old moon's gap loop) -- is put back on the road behind
  (`Rescue`), as from the sea. Not while it is held on purpose (the grid, a hit, a bite: `RaceMod_Held`), skidding, flying a jump, in a
  loop, on the super jet-pack, on the roulette wheel's drop or falling. The log says where, and the decor it last ran into (if within the
  last second): `stuck at (x,y,z) for 3.0 s driving on: back on the road (the last decor run into: ...)`. The car's own box is logged once
  a run too (`the car's box: x -633..633, y 0..940, z -633..633` -- the scenery's clearances are for that).
- **Glancing blows** (EXTFUNC.CPP `GlanceAside`, RACEMOD.CPP `RaceMod_Glance`): the decors' boxes are all square to the island, so no shape
  of a wall's end or a post could turn a car aside -- a car meeting one square on stopped dead. Now the race car, blocked along the way it
  mostly moves by a box it overlaps across that way by no more than 320 units (a gantry's post, 283 wide; a pit wall's end, about 200; a
  corner the road's edge brushes), is pushed aside past it, to its nearer side. Blocked across its way it slides along the box as the
  game has it (pushed to the nearer side there, a car running past a post beside it was pushed back, frame after frame).
- **Start gantries built to the road** (RaceTrackBuilder.cs `BuiltGantry`): the retail gantry is one length, 10.5 cells post to post, and
  over the 9-cell roads its posts stood 0-1.2 cells beyond the kerbs -- under the race car's half width (1.24 cells). Where either post
  would stand nearer its kerb than 1.75 cells, the gantry is built to the road instead: RaceTrackRaisedBody's gantry (as the raised roads
  have), as tall as the retail one, the frame drawn only and each post a solid box of an empty body. Nine tracks have one now: Desert,
  both Citadel tracks, Mosquibees, Celebration, Sendell's Well, the old moon, Polar Island and the Emperor's palace. (Its chequered beam
  is capped at 30 squares: the 20-cell gantries over the pit lanes would have passed the engine's 550 points.)
- **The Emerald Moon's edge zone** (RaceTrackScenes.cs `CoverEdges`): a zone's start along the edge is also its arrival's other
  coordinate, and the engine reads 512 and 31,744 there as the marks of the other edges' zones (OBJECT.CPP `GereZoneChangeCube`). Scene
  230's north zone happened to start at x 512, so it waited for the car to leave eastwards too and never took it. A start on either
  mark is now moved a unit.

Checked by building all 18 tracks fresh with the last commit's code and with these changes: only the nine gantried islands' ILE and OBL
and SCENE.HQR changed (28 of 47 files the same). Then the 72 races again: every one finished, and no car stood still. The reset fired 12
times before a race's end, at 6 places:

- off the racing line (the test pilot 1.5 cells to its left): the town circuit's narrow street, where a building's wall is the road's
  edge (2); the start of Celebration Island's raised road (1); Polar Island (1);
- on the racing line with the faster cars: Sendell's Well with the suggested setup (1), and Wannies Island at cells (538, 488) with the
  hot car (2);
- **on the racing line with every car, still to fix: Wannies Island, lap point 384 of its raised road (cells 464, 562, height 7,113)**,
  where the car is held against the left rail (base 2, hot 2, suggested setup 1). It used to cost about 10 s
  a lap there (a 35.6 s lap against 25); the reset now gets the car out in 3 s.
