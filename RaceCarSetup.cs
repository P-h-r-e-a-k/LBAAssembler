namespace LBAAssembler;

// The buggy's setup for a race-track mod: what the engine's race-track mode (native RACEMOD.CPP) does with the car when Play runs a game folder that
// has a race track built (Tools > LBA2: race track). Kept in the settings; RaceCarWindow edits it.
//
// Speeds are km/h as the engine's own display shows them, a cell (512 world units) taken as a metre: the original buggy's top speed, 3800 units a
// second, is 27 km/h. The rates are percentages of the original car's (it gains 4 units a second every millisecond of throttle, loses 12 braking
// and 7 rolling, and turns a quarter turn a second). In each gear the car pulls harder the lower the gear's top speed, as a gearbox does, so a
// gear with the original top speed accelerates like the original car.
public sealed class RaceCarSetup
{
    public const int MaxGears = 6;
    // the fastest a gear or reverse may go (km/h): ten times the original buggy's 27
    public const int MaxKmh = 270;
    public const int OriginalTop = 3800, OriginalReverse = 2000, OriginalSteer = 1024;
    public const double OriginalAccel = 4, OriginalBrake = 12, OriginalCoast = 7;

    public int Gears { get; set; } = 5;
    public List<int> GearTopKmh { get; set; } = new() { 11, 16, 22, 28, 34, 40 };
    public int AccelerationPercent { get; set; } = 100;
    public int BrakingPercent { get; set; } = 130;
    public int CoastingPercent { get; set; } = 100;
    public int ReverseKmh { get; set; } = 14;
    public int SteeringPercent { get; set; } = 110;
    public bool Automatic { get; set; }
    public bool ShowDisplay { get; set; } = true;
    // Show the setup before each race-track play (Play on a folder with a race track).
    public bool AskBeforePlay { get; set; } = true;
    // Race the opponents: the track's drivers (Terrain.RaceDriver: each track's own line-up, or the retail track's racer, Baldino in his
    // rocket car and the motorbike Rabbibunny), driven round the lap by the race-track mode on racing lines of their own. Each drives this
    // car as the setup makes it (its top gear, steering, pull and brakes, a little quicker on the straights or in the bends as its driver
    // is) as well as its skill says: 100 % drives it perfectly on its line, faster than a player can. RacerSkill is the drivers' skill,
    // each a few points either side of it; LeftOut the drivers (by name) not raced.
    public bool Opponent { get; set; } = true;
    // The skill of the one to beat on each track (the others' are drawn at random round it each race): beatable, with a little challenge
    // (2026-10-03: at 92 the test pilot, a better driver than most, lost the lava lake to the souvenir seller by half a second). RacerSkill is
    // the opponents' skill as it was before there was one to beat, kept for the settings already saved.
    public int MainSkill { get; set; } = 86;
    public int RacerSkill { get; set; } = 92;
    // Power-ups hidden in small brown mushrooms along the lap (RACEMOD.CPP): a car that drives over one gets it.
    public bool PowerUps { get; set; } = true;
    // A nitro penguin dropped walks the track until a car comes near it, and goes off; off: it goes off a second after it is dropped.
    public bool PenguinsWalk { get; set; } = true;
    // The checkpoints drawn as red lines across the road (RACEMOD.CPP checkpoint_lines=), for testing where they are; off: unseen. Off
    // unless asked for, since 2026-10-08 (in pictures of a race they read as something wrong with the track; the race log says when each
    // is crossed).
    public bool ShowCheckpoints { get; set; }
    public List<string> LeftOut { get; set; } = new();
    // A qualifying lap before the race: its time against the opponents' sets the grid (off: Twinsen starts on pole).
    public bool Qualifying { get; set; } = true;
    // Citadel Island's weather once the storm is over -- as after the lighthouse keeper is freed and the aliens land: no rain, the
    // brighter island file, its light and its sky. Only the weather: the story stays where the game is. (A folder whose Citadel Island
    // has a track in each file races the one chosen in the race track window, in its own weather: RaceTrackService.FineWeather.)
    public bool FineWeather { get; set; } = true;
    // An opponent that has fallen behind pushes harder (up to CatchUpPercent more, 60 cells behind).
    public bool OpponentsFightBack { get; set; } = true;
    public const int CatchUpPercent = 8;
    // Play starts on the start/finish straight, Twinsen beside his car, whatever scene is open (and with the zones and routes the editor
    // draws over the game hidden).
    public bool StartAtLine { get; set; } = true;
    // Play starts a new game -- the story from its start, Twinsen in his house -- instead (the folder's tracks each raced where and when the
    // game is: RaceCarEngineFile.WriteStorySet).
    public bool NewGame { get; set; }
    // The car Twinsen drives: -1 the track's own -- his buggy, or the car a track has for him (the Temple of Bú's pumpkin F1, since
    // 2026-10-17: Terrain.RaceTrackCharacterCars.TrackCar) --, -4 his buggy on every track, -2 or -3 his buggy firing green or red lasers out
    // of its gun (his laser cars), else a race car by its number -- the racer entity's bodies (RaceTrackScenes.RacerEntity: the retail racer's
    // car 0, Baldino's rocket car 1, the cars made after the characters 2 on, Terrain.RaceTrackCharacterCars), the cast's cars on from 64
    // (RACECARS.JSON) and the pumpkin F1, 220 -- for a test drive of a track in it (the engine's drive_as= and drive_laser=:
    // RaceCarEngineFile.DriveAsLine). It handles as this setup makes the car.
    public int DriveAs { get; set; } = -1;
    public const int OwnBuggy = -1, LaserGreen = -2, LaserRed = -3, BuggyAlways = -4;
    // (that car as the folder's files have it, worked out when the car file is written: RaceTrackService.CarFileWriter; and with OwnBuggy,
    // each track's own car, by the track's name: RaceCarEngineFile.TrackCarLines)
    [System.Text.Json.Serialization.JsonIgnore] internal string? DriveAsKey { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] internal Dictionary<string, string>? TrackCarKeys { get; set; }
    // Each track's best setup (TrackSetups) in place of the sliders' top speed, acceleration and steering on the track Play races; braking,
    // rolling, reverse and the gearbox stay this setup's own, and a track with no best setup drives as the sliders say. Off unless asked for
    // (2026-10-17, the user: "Yesterday we calculated the optimum car setup for each track, let's add this as an option to override the test
    // sliders we currently have for car setup"): RaceCarEngineFile.EngineFile.
    public bool UseTrackSetups { get; set; }

    public RaceCarSetup Clone()
    {
        var copy = (RaceCarSetup)MemberwiseClone();
        copy.GearTopKmh = new List<int>(GearTopKmh);
        copy.LeftOut = new List<string>(LeftOut);
        return copy;
    }

    public sealed record Preset(string Name, RaceCarSetup Setup);

    public static IReadOnlyList<Preset> Presets { get; } = new[]
    {
        new Preset("The original buggy (one gear, as in the game)", new RaceCarSetup { Gears = 1, GearTopKmh = new() { 27, 34, 40, 46, 52, 58 }, AccelerationPercent = 100, BrakingPercent = 100, CoastingPercent = 100, ReverseKmh = 14, SteeringPercent = 100 }),
        new Preset("Race car (5 gears)", new RaceCarSetup()),
        new Preset("Fast race car (6 gears)", new RaceCarSetup { Gears = 6, GearTopKmh = new() { 12, 18, 24, 30, 37, 44 }, AccelerationPercent = 130, BrakingPercent = 160, CoastingPercent = 110, ReverseKmh = 16, SteeringPercent = 125 }),
    };

    // A track's best setup, in the Race car window's terms: the top gear's top speed (the other gears scaled with it, as the top speed
    // slider does: SetTopSpeed), the acceleration and the steering. Track: as the stress test's table names it.
    public sealed record TrackSetup(string Track, int TopKmh, int AccelerationPercent, int SteeringPercent)
    {
        public override string ToString() => $"{Track}: top speed {TopKmh} km/h, acceleration {AccelerationPercent} %, steering {SteeringPercent} %";
    }

    // Each track's best setup (UseTrackSetups), by its race track entry's name (Terrain.RaceTrackIsland: Citadel Island's town circuit is
    // its entry "Citadel Island", its storm track "Citadel Island, storm track"; Celebration Island's statue track and its lava lake are
    // two islands). A setup that is easy to drive there, not just quick (2026-10-17, the user: "the best setup is not the one that gives the
    // fastest time ... Reducing the speed to 80 and setting steering to 150 feels better to drive and allows us to more easily stay in the
    // centre of the track"): docs/LBA2_RACE_TRACK_STRESS_TEST.md, "Easy to drive, not only fast"; tools/RaceTrackStress/drive.py. A light
    // tap of the steering at the setup's speed, and the tap back a moment later, moves the car no further across the road -- for the road's
    // room there -- than it does on the Temple of Bú at 80 km/h with steering 150 % (the user's own setup: the Temple's is that); of those
    // setups, the engine's planner's fastest lap. From the standalone game's car -- five gears to 45/57/63/97/110 km/h, acceleration 300 %,
    // braking 300 %, steering 200 % -- top speed, acceleration and steering each 70-130 % of the car's, the three summing to no more than 300
    // (the stress test's budget of 2026-10-16: what the tap takes from top speed and steering goes to acceleration), the braking as it was.
    // Island CX, Otringal and the lava lake are at the grid's least (77-79 km/h, steering 140 %), still a little over the Temple's tap.
    // A track built later has none until it is planned too.
    public static IReadOnlyDictionary<string, TrackSetup> TrackSetups { get; } = new (Terrain.RaceTrackIsland Island, TrackSetup Best)[]
    {
        (Terrain.RaceTrackIsland.Desert, new("Desert island", 99, 390, 140)),
        (Terrain.RaceTrackIsland.CitadelStorm, new("Citadel, storm lap", 96, 390, 150)),
        (Terrain.RaceTrackIsland.Citadel, new("Citadel, town circuit", 93, 390, 150)),
        (Terrain.RaceTrackIsland.Mosquibe, new("Mosquibees Island", 99, 390, 140)),
        (Terrain.RaceTrackIsland.Celebration, new("Celebration Island", 82, 390, 140)),
        (Terrain.RaceTrackIsland.CelebrationLava, new("Celebration, lava lake", 79, 390, 140)),
        (Terrain.RaceTrackIsland.Elevator, new("Elevator Platform", 79, 390, 145)),
        (Terrain.RaceTrackIsland.Sendell, new("Sendell's Well", 101, 390, 155)),
        (Terrain.RaceTrackIsland.Moon, new("The old moon", 79, 390, 140)),
        // (a dreamt sprint: in place of the dream's own top speed, DreamInfo.TopKmh, too)
        (Terrain.RaceTrackIsland.Polar, new("Polar Island", 77, 390, 140)),
        (Terrain.RaceTrackIsland.Knartas, new("Island of the Francos", 104, 390, 145)),
        (Terrain.RaceTrackIsland.Otringal, new("Otringal", 77, 390, 140)),
        (Terrain.RaceTrackIsland.Volcano, new("Volcano Island", 85, 390, 175)),
        (Terrain.RaceTrackIsland.Emerald, new("The Emerald Moon", 104, 390, 140)),
        (Terrain.RaceTrackIsland.Wannies, new("Wannies Island", 101, 390, 150)),
        (Terrain.RaceTrackIsland.Palace, new("The Emperor's palace", 93, 390, 140)),
        (Terrain.RaceTrackIsland.CxTower, new("Island CX", 77, 390, 140)),
        (Terrain.RaceTrackIsland.Temple, new("The Temple of Bú", 80, 390, 150)),
    }.ToDictionary(t => t.Island.Name, t => t.Best, StringComparer.OrdinalIgnoreCase);

    // This setup with a track's best top speed, acceleration and steering in place of its own.
    public RaceCarSetup With(TrackSetup best)
    {
        var copy = Clone();
        copy.SetTopSpeed(best.TopKmh);
        copy.AccelerationPercent = best.AccelerationPercent;
        copy.SteeringPercent = best.SteeringPercent;
        return copy;
    }

    // The top speed slider's: every gear's top speed scaled so the top gear's is `kmh`, each still over the one below it.
    public void SetTopSpeed(double kmh)
    {
        var gears = Math.Clamp(Gears, 1, MaxGears);
        var top = TopKmh(gears - 1);
        if (top <= 0) return;
        while (GearTopKmh.Count < gears) GearTopKmh.Add(GearTopKmh.LastOrDefault(27));
        var scaled = Enumerable.Range(0, gears).Select(g => TopKmh(g) * kmh / top).ToArray();
        for (var g = 0; g < gears; g++)
        {
            if (g == gears - 1) { GearTopKmh[g] = (int)Math.Round(kmh); continue; }
            var lo = g == 0 ? 1 : GearTopKmh[g - 1] + 1;
            GearTopKmh[g] = Math.Clamp((int)Math.Round(scaled[g]), lo, Math.Max(lo, (int)Math.Round(kmh) - (gears - 1 - g)));
        }
    }

    public static int KmhToUnits(double kmh) => (int)Math.Round(kmh / 3.6 * 512);
    public static double UnitsToKmh(double units) => units * 3.6 / 512;

    public int TopKmh(int gear) => gear < GearTopKmh.Count ? GearTopKmh[gear] : GearTopKmh.LastOrDefault(27);
}
