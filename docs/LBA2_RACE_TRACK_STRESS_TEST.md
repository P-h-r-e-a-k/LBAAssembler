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
driven laps are base -> suggested; driven is the mean of three clean laps (the sprints: the run). (These fastest setups were the window's
"best setups" for a day; since 2026-10-17 they are the drivable ones of "Easy to drive, not only fast", below.)

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

**In the Race car window (2026-10-17).** The user: "Yesterday we calculated the optimum car setup for each track, let's add this as an
option to override the test sliders we currently have for car setup." The Car tab's "Use each track's best setup for its top speed,
acceleration and steering" (off unless ticked; `RaceCarSetup.UseTrackSetups` in the settings) drives the track Play races with the
suggestion above in place of those three sliders: its top speed scales the gears as the top speed slider does, and braking, rolling,
reverse and the gearbox stay the sliders'. The sliders keep their values, greyed while they are overridden, and drive a track with no
suggestion (one built later) and every track once the box is cleared. The window says which setup applies (the track Play races; from
the race track window, each of the folder's tracks). The table is `RaceCarSetup.TrackSetups`, by race track entry (Citadel Island's
two tracks each have one). Each track's lowest planned lap in `plan3.txt` was the table's setup (until the next section replaced them);
Wannies Island and the Emperor's palace each have a tie at the third decimal place, and the table takes the first of the pair. The race file says which it
used (`# the track's best setup ...`, then the car keys), in each track's file of the story's set too, where the set's own keys (an
island with no track) stay the sliders'. Polar Island's dreamt sprint takes the suggestion's 143 km/h in place of the dream's own 120
(its waking gears stay the sliders'). Test: `RT_TRACK_SETUP=1 ScriptRoundTrip racecarfile ...`.

## Easy to drive, not only fast (2026-10-17)

The user: "We also need to refine the best setup for tracks, the best setup is not the one that gives the fastest time, the recommended
setup for the Temple track means a slight side arrow tap takes us from one side of the narrow track to the other at the recommended speed.
Reducing the speed to 80 and setting steering to 150 feels better to drive and allows us to more easily stay in the centre of the track."

**Why the fastest lap was the wrong test.** The planner's lap is a robot's: it drives the line at exactly the speed each bend allows and
steers as finely as it likes. Top speed and steering each make that lap quicker, so the fastest setups took as much of both as the budget
gave (steering 250-260 % on 15 tracks; 126-143 km/h on the palace, the Temple and Polar Island). A player steers with keys, a tap at a time,
and sees the car drift before tapping back -- and how far a tap sends the car across grows with both the speed and the turn the tap makes.
Nothing in the lap time knows how wide the road is.

**How the buggy steers** (BUGGY.CPP `MoveBuggy`, `RaceSpeed`): while a steering key is held the car turns at a fixed rate -- the race file's
`steer` (the window's percentage of 1024) in 4096ths of a turn a second, at any speed up to the top gear's (faster than that, more in
proportion) -- starting and stopping at once, and nothing turns it back: let go, it carries on along its new heading. A tap of t seconds turns
it by steer x t, and it then crosses the road at v·sin(turn) until the driver taps back. In the engine (`drive.py tap`: the test pilot
drives the Temple's long straight and lets go; `input` holds the throttle and taps left for 6 steps of 16 ms; `cartrace` logs each frame):

| setup | speed | heading turned (4096ths) | steer x 96 ms | across after the tap | v·sin(turn) |
|---|---|---|---|---|---|
| 80 km/h, steering 150 % | 79.7 km/h | 144 (12.7°), held after it | 147 | 4.91 cells/s | 5.00 cells/s |
| 132 km/h, steering 220 % | 128.5 km/h | 216 (19.0°), held after it | 216 | 9.44 cells/s | 10.46 cells/s |

**The measure: a tap's move across the road, for the road's room.** A tap of 0.1 s, and the tap back 0.25 s after it, move the car across
by 2v(1 - cos wt)/w + v·sin(wt)·0.25 (v the speed, w the turn rate, t the tap) -- nearly v x w x 0.1 x 0.35: the car's speed times its turn
rate, the sideways pull of a full-lock bend. At 132 km/h with steering 220 % that is 4.4 cells; at 80 km/h with 150 %, 1.8. The room is how
far the car's middle can move from the road's middle: on a raised road its half width less 640 (RAISED_INSET, the car's half width the
engine keeps inside the rail), on the ground the plan's kerb half width less the car's (633). The Temple's deck is 4.5 cells from middle to
rail (4.0 on its narrower stretches): 3.25 cells of room (2.75). The measure is the share of the room the tap takes, at every point of the
racing line at the speed the planner drives there with that setup (the console's `raceplan file <setups> <laps> <points>` now writes each
setup's plan point by point: RACEMOD.CPP `PlanPointsOut`), averaged over the lap's worst 2 s of driving -- one narrow gate doesn't decide
it, a narrow straight does. Where the engine carries the car over a jump nothing is steered; where a branch leaves or joins the road, the
widest piece there counts. `tools/RaceTrackStress/drive.py` does it all (`points`, `table`, `report`, `verify`, `tap`).

**Calibrated on the user's verdict.** At 80 km/h with steering 150 % the Temple's tap takes 0.66 of the room (1.8 cells of the narrow
stretches' 2.75, at full speed); with the old suggestion, 132 km/h and 220 %, 1.58 -- from the road's middle well past the rail, from one
side most of the way to the other, as the user found. The standalone game's base car (110 km/h, 200 %) takes about 1.2. The limit is the
user's: **0.66 of the room**. The tap's and the reaction's lengths cancel out in that calibration (every term goes as v x w): with 0.06 s
and 0.2 s the same 18 setups come out, with 0.15 s and 0.4 s the same but for a step of the grid on three tracks. The window matters
more: 1 s puts four more of the tight tracks at the floor below (Celebration Island, the Elevator Platform, the old moon, Polar Island),
4 s lets the narrow ones up to 20 km/h faster (the old moon 99 km/h, Otringal 90).

**The choice.** The grid: top speed and steering 70-130 % of the base car's in 2.5 % steps, acceleration 70-130 % in steps of 5, the three
summing to no more than 300 -- the stress test's budget, so what the tap takes from top speed and steering goes to acceleration. Of the
setups within the limit, the one with the fastest planned lap; top speeds rounded down to whole km/h. The Temple's is the user's own top
speed and steering, with the acceleration that laps them fastest (the planner's own pick within the limit there is 85 km/h with steering
140 %: 70.9 s against 73.9, for the same tap). How it was balanced:

- The limit is relative to each road's room, so a wider road takes a faster car: the Francos' and the Emerald Moon's 104 km/h against the
  Temple's 80. Narrow ground roads (the old moon's and Sendell's Well's kerbs at 3.5 cells) and raised ones (the lava lake's 3.05, Island
  CX's 3-4) take slower or less quick-steering cars.
- It reads the speed the car is driven at, not its top gear's: a track that never reaches its top speed is judged in its bends (Sendell's
  Well's car is set to 101 km/h and never goes over 76), and a rollercoaster's drops count at the speed they give (the Elevator Platform's
  car goes 91 km/h downhill with a top gear of 79).
- Nothing goes below 70 % of the base car's top speed or steering -- 77 km/h, 140 % -- the user's own 80 km/h and 150 % are just above
  both: slower than that would be absurd for a race car. Island CX, Otringal and the lava lake are narrower than even that floor manages
  (their tap takes 0.70-0.81 of the room) and get the floor, marked * below. If they still feel twitchy, steering under 140 % is the next
  thing to try.
- Acceleration doesn't move the car across, so every setup puts the budget's rest into it: 390 % (130 % of the base car's) on all 18 --
  at the Temple 0.2 s a lap better than 300 %.
- Steering came out low almost everywhere (140-155 %; Volcano Island, all bends, 175 %): for the same tap the planner gains more lap from
  top speed than from steering.
- The perfect lap is 18-56 % slower than the fastest setup's. The opponents drive the player's car as the setup makes it (above), so a
  race is no easier or harder for it.

Each track's setup in the Race car window's terms (top speed, acceleration, steering). The room is the car's middle's either side of the
road's middle, typical (half the lap's time) and narrowest (its narrowest tenth). The tap's move is at the car's fastest, and in brackets
the measure (the worst 2 s, as a share of the room; the limit 0.66). Laps are old setup -> new; planned is the perfect lap, driven the
mean of the test pilot's three clean laps (the sprints: the run).

| track | room: typical / narrowest (cells) | old setup | its tap | new setup | its tap | planned lap (s) | driven lap (s) |
|---|---|---|---|---|---|---|---|
| Desert island | 3.26 / 3.26 | 110 km/h, 210 %, 260 % | 4.3 cells (1.31) | 99 km/h, 390 %, 140 % | 2.1 cells (0.64) | 47.4 -> 56.0 | 46.3 -> 54.0 |
| Citadel, storm lap | 3.26 / 3.26 | 110 km/h, 210 %, 260 % | 4.3 cells (1.24) | 96 km/h, 390 %, 150 % | 2.2 cells (0.66) | 10.3 -> 12.3 | 10.5 -> 12.2 |
| Citadel, town circuit | 3.26 / 3.26 | 110 km/h, 210 %, 260 % | 4.3 cells (1.31) | 93 km/h, 390 %, 150 % | 2.1 cells (0.65) | 36.5 -> 44.6 | 36.1 -> 43.3 |
| Mosquibees Island | 3.26 / 3.26 | 116 km/h, 210 %, 250 % | 4.3 cells (1.33) | 99 km/h, 390 %, 140 % | 2.1 cells (0.64) | 16.2 -> 19.6 | 16.3 -> 19.5 |
| Celebration Island | 3.50 / 2.70 | 104 km/h, 225 %, 260 % | 4.0 cells (1.26) | 82 km/h, 390 %, 140 % | 1.7 cells (0.66) | 15.5 -> 21.4 | 15.3 -> 20.9 |
| Celebration, lava lake | 2.50 / 1.56 | 104 km/h, 225 %, 260 % | 4.0 cells (1.93) | 79 km/h, 390 %, 140 % | 1.9 cells (0.81) * | 10.6 -> 14.7 | 11.2 -> 15.1 |
| Elevator Platform | 3.50 / 2.15 | 110 km/h, 210 %, 260 % | 4.8 cells (1.57) | 79 km/h, 390 %, 145 % | 2.3 cells (0.65) | 11.9 -> 17.7 | 11.3 -> 16.5 |
| Sendell's Well | 2.26 / 2.26 | 94 km/h, 255 %, 260 % | 3.6 cells (1.49) | 101 km/h, 390 %, 155 % | 1.8 cells (0.66) | 3.7 -> 5.3 | 3.6 -> 5.1 |
| The old moon | 2.26 / 2.26 | 94 km/h, 255 %, 260 % | 3.6 cells (1.57) | 79 km/h, 390 %, 140 % | 1.7 cells (0.66) | 12.1 -> 14.3 | 12.8 -> 14.3 |
| Polar Island (sprint) | 2.76 / 2.76 | 143 km/h, 210 %, 200 % | 4.3 cells (1.58) | 77 km/h, 390 %, 140 % | 1.6 cells (0.64) | 49.6 -> 77.2 | 51.1 -> 75.2 |
| Island of the Francos | 3.50 / 3.50 | 116 km/h, 210 %, 250 % | 4.3 cells (1.25) | 104 km/h, 390 %, 145 % | 2.3 cells (0.65) | 43.9 -> 54.8 | 39.3 -> 42.6 |
| Otringal | 2.75 / 2.51 | 110 km/h, 210 %, 260 % | 4.3 cells (1.56) | 77 km/h, 390 %, 140 % | 1.6 cells (0.70) * | 26.0 -> 35.7 | 28.8 -> 35.7 |
| Volcano Island | 3.50 / 3.50 | 116 km/h, 210 %, 250 % | 4.3 cells (1.24) | 85 km/h, 390 %, 175 % | 2.2 cells (0.64) | 22.2 -> 27.8 | 22.4 -> 28.1 |
| The Emerald Moon | 3.25 / 3.25 | 110 km/h, 210 %, 260 % | 4.3 cells (1.29) | 104 km/h, 390 %, 140 % | 2.2 cells (0.66) | 23.1 -> 28.7 | 22.5 -> 29.0 |
| Wannies Island | 3.50 / 3.50 | 116 km/h, 210 %, 250 % | 4.3 cells (1.24) | 101 km/h, 390 %, 150 % | 2.3 cells (0.66) | 21.7 -> 26.1 | 20.6 -> 21.0 |
| The Emperor's palace (sprint) | 3.01 / 3.01 | 126 km/h, 210 %, 230 % | 4.3 cells (1.44) | 93 km/h, 390 %, 140 % | 2.0 cells (0.65) | 35.9 -> 49.6 | 34.9 -> 46.3 |
| Island CX | 2.15 / 1.75 | 99 km/h, 240 %, 260 % | 3.8 cells (1.96) | 77 km/h, 390 %, 140 % | 1.6 cells (0.80) * | 17.8 -> 24.2 | 18.4 -> 26.6 |
| The Temple of Bú | 3.25 / 3.25 | 132 km/h, 210 %, 220 % | 4.4 cells (1.58) | 80 km/h, 390 %, 150 % | 1.8 cells (0.66) | 49.1 -> 73.9 | 48.9 -> 73.4 |

Polar Island's dreamt sprint takes the table's 77 km/h in place of the dream's own 120 while the box is ticked (as it took 143 before).
(The Francos' and Wannies Island's driven laps are under their plans with either setup: the Francos' hazards and Wannies' branches, as in
the first table.)

**Checked.** All 18 tracks built together from the pristine data with this code (`E:\dump\TEMP\setupdrive\game`, STRESS_ROOT
`E:\dump\TEMP\setupdrive`) and each driven by the test pilot three times (`drive.py verify`, 54 runs): a clean time trial with the old
setup and one with the new, and a race with the new against the opponents, power-ups on. Every run reached its end with no rescue, fall,
missed checkpoint or hold at a cube's edge before it, and every jump landed (the Desert 3 a run, the storm lap 8-9, Mosquibees 3, the lava
lake 9); the old moon's "off the ring" lines are its first loop's gap, leapt on purpose, as many with either setup. After the finish, the
pilot driving on with power-ups still about, the stuck-car reset fired once on Mosquibees Island (against its decor 25 at cells
(488, 532), a nitro penguin just dropped) and once on Sendell's Well. With the new setups `RT_TRACK_SETUP=1 ScriptRoundTrip racecarfile`
writes each track's top speed, acceleration and steering into its race file and into the story set's track files (the set's own keys
the sliders').

## What the stress runs found

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

## The second round (the same evening)

The user: "Make the fixes". Every reset left was followed to its cause, with forced super jet-packs too (`tools/RaceTrackStress/jet.py
<track> <tag> <side> <tick>...`: `raceitem 4` at each tick and the cars' places every 10 ticks after it).

- **Wannies Island, lap point 384, held against the rail** (RACEMOD.CPP `RaceMod_AutoInput`): round a tight bend the point the pilot
  steers for, a look-ahead on along the line, is inside the bend from the car (the chord cuts the arc). Where the line takes the bend
  near the inner rail -- 155 units short of the rail's hold here -- that point was past the rail, and the car drove into the rail nose
  first and stood there pointing at it. On a raised road the point is now kept inside the rail's hold, half a cell in (left where it is
  on a strip beside the road, or on another stretch's deck). The super jet-pack drives by the same pilot, so it is kept in too.
- **The super jet-pack over Mosquibees Island's plateau's edge**: the pilot looks further ahead the faster the car goes, and at twice the
  speed the point ahead cut the bend after the start by the plateau's edge -- over it, every jet-pack. The look-ahead is now at most 0.7
  of the line's radius there (no nearer than two cells). The test pilot's own side offset (`autodrive 1 5 <side>`) is no longer added
  to the jet-pack's steering (a player's jet-pack has none). A rescue on the jet-pack ends it (it drove the car off the road, and at
  twice the speed it drove it off again at the same bend: seven rescues in one jet-pack), and the stuck-car reset now works on it too
  (it drives into a wall as a car does, and stood there pinned until it ran out).
- **The super jet-pack through Mosquibees Island's bridge** (EXTFUNC.CPP `GhostSkips`): since 2026-10-10 the jet-pack goes through the
  island's objects (the user's "the jet pack needs updating so it doesn't get stuck on objects"), and that took the bridge's deck too --
  a deck of the island's objects, not a raised road. The car fell into the valley at the bridge's start in 5 jet-packs of 6. An object
  the car is on -- its top no more than half a brick over where the car was, the game's own "on top of it" test -- now holds it up as
  it holds any car; walls, posts and the shuttles' sides are still gone through.
- **Twinsen on foot after a rescue into another scene**: a car that left its scene (the jet-pack off the plateau into scene 105, or over
  the island's edge) was rescued by setting the scene to change from the drawing, which begins the scene afresh -- and its own opening's
  `SET_MOVE` took Twinsen out of his car. On foot the race stood still for good. The rescue's scene change is now asked for as a zone's
  is (`RaceMod_EdgeCross`: the line point's place, the car carried over), and for 3 s after it Twinsen is put back in his car if he
  comes in on foot (`BackInCar`).
- **Off the island's edge** (EXTFUNC.CPP, `RaceMod_OffIsland`): the game sends a hero who moves where there is no cube into the phantom
  cube (94) to drown, which took Twinsen out of his car too. A race car is now held at the edge and put back on the road behind.
- **Opponents stopped for good by being run into** (RACEMOD.CPP `RaceMod_OpponentStruck`, EXTRA.CPP `RaceMod_ExtraHit`): the game
  counts a car running into an actor as a hit (OBJECT.CPP's object collisions), and the race took every hit by Twinsen for his magic
  ball, which stops an opponent for 1.5 s. Leant on, an opponent was stopped again each time its last stop ran out and never moved. On
  Wannies Island the old Wannie stood on the racing line at cells (537, 488) -- the first round's "hot car" spot -- with Twinsen's car
  against the rail behind him: four resets in a row, each putting Twinsen back behind him. Only a thrown ball's hit stops an opponent
  now; run into, a car is no more than in the way (`Twinsen's car runs into ...'s car: no hit`, at most every 5 s in the log). The runs
  showed 6-26 such run-ins a race.
- **The Desert's jump cut short over its gap** (RACEMOD.CPP `RaceMod_InFlight`; EXTFUNC.CPP `InitSkating`, OBJECT.CPP `HitObj`): a jump
  is the hero's flight animation (RACE_JUMP_ANIM_*), and anything that plays another animation on him ends the flight where he is. Two
  did: the game's skating, which the engine starts on a buggy over a forbidden slope (EXTFUNC.CPP `ReajustPosExt`'s "blindage buggy")
  -- at the lip, over the gap's steep edge -- and a hit in the air (his own nitro penguin going off beside him, slow after a rescue).
  The car fell into the gap and stood against the landing ramp's stepped foot, always at (317951, 1143, 319487), the reset getting it
  out. With jet-packs forced every 10 s, 3 of 27 jumps ended so (`jump: the flight over (now animation 33 ...)`: 33 the skate, 22 a hit,
  0 the landing). While Twinsen flies a jump he now neither skates nor takes a hit (`... hits Twinsen's car: in a jump's flight`); all
  24 jumps of the next six races landed. Each flight's end is logged now, with the height above the ground there.
- The log says where each sea or lava rescue was (`into the lava or the sea at (x,y,z)[, on the super jet-pack]`); the stuck-car
  reset adds the rail's last move on a raised road (`the rail's last move: <what> at segment <n>, <lat> across (kept <lo>..<hi>)`:
  1 the move not made, 2 past the road's end, 3 within the rail, 4 on a strip, 5 within another stretch's rail, 6 driven off a grounded
  end, 7 round a corner, 8 put on the rail, 9 not at the deck's height, 10 off a grounded end) and the objects within 3 cells of the
  car. `ScriptRoundTrip altitude <ILE> <x> <z> [radius]` prints the island's ground round a place every half cell (it found the bridge's
  valley). `report.py` now splits a log line written into the middle of another (the engine's lines and the console's are written
  apart: one Otringal race's end was hidden that way).

Checked with the 72 races and 36 time trials again on the last build: every race with opponents finished (the storm lap has none),
no crash, no flight cut short, and the time trials ran without a reset (Otringal's and the Francos' slow laps are their hazards -- the
gunners, a gas monster's bite, the shuttle, the steam jets on their lap's side). The reset fired 4 times before a race's end, against 12
in the first round, all with the test pilot 1.5 cells to the left of the line: the town circuit's narrow street (1), Celebration
Island's lap start -- where the line runs along the raised road's east edge, at ground level, and 1.5 cells beside it is the hillside --
(2), and once into the lava lake's lava. None is on the racing line. The forced jet-packs: 6 on Mosquibees Island and 12 on Otringal,
Wannies, the Emerald Moon and Island CX without a rescue, and six Desert races with one every 10 s, every jump landed.
