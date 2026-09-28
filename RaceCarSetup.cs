namespace LBAAssembler;

// The buggy's setup for a race-track mod: what the engine's race-track mode (native RACEMOD.CPP) does with the car when Play runs a game folder that
// has a race track built (Tools > LBA2: Desert island race track). Kept in the settings; RaceCarWindow edits it.
//
// Speeds are km/h as the engine's own display shows them, a cell (512 world units) taken as a metre: the original buggy's top speed, 3800 units a
// second, is 27 km/h. The rates are percentages of the original car's (it gains 4 units a second every millisecond of throttle, loses 12 braking
// and 7 rolling, and turns a quarter turn a second). In each gear the car pulls harder the lower the gear's top speed, as a gearbox does, so a
// gear with the original top speed accelerates like the original car.
public sealed class RaceCarSetup
{
    public const int MaxGears = 6;
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
    // Race the opponent (the retail track's racer, driven round the lap by the race-track mode), and how fast it goes (percent of its
    // line's speeds: a top speed of 31 km/h, slower in bends).
    public bool Opponent { get; set; } = true;
    public int OpponentPace { get; set; } = 100;
    // Race Baldino too, in his rocket car (a second opponent, on a line of his own), and how fast (percent of his line's speeds: up to 33 km/h
    // on the straights, slower than the racer in bends).
    public bool Baldino { get; set; } = true;
    public int BaldinoPace { get; set; } = 100;
    // Play starts on the start/finish straight, Twinsen beside his car, whatever scene is open (and with the zones and routes the editor
    // draws over the game hidden).
    public bool StartAtLine { get; set; } = true;

    public RaceCarSetup Clone()
    {
        var copy = (RaceCarSetup)MemberwiseClone();
        copy.GearTopKmh = new List<int>(GearTopKmh);
        return copy;
    }

    public sealed record Preset(string Name, RaceCarSetup Setup);

    public static IReadOnlyList<Preset> Presets { get; } = new[]
    {
        new Preset("The original buggy (one gear, as in the game)", new RaceCarSetup { Gears = 1, GearTopKmh = new() { 27, 34, 40, 46, 52, 58 }, AccelerationPercent = 100, BrakingPercent = 100, CoastingPercent = 100, ReverseKmh = 14, SteeringPercent = 100 }),
        new Preset("Race car (5 gears)", new RaceCarSetup()),
        new Preset("Fast race car (6 gears)", new RaceCarSetup { Gears = 6, GearTopKmh = new() { 12, 18, 24, 30, 37, 44 }, AccelerationPercent = 130, BrakingPercent = 160, CoastingPercent = 110, ReverseKmh = 16, SteeringPercent = 125 }),
    };

    public static int KmhToUnits(double kmh) => (int)Math.Round(kmh / 3.6 * 512);
    public static double UnitsToKmh(double units) => units * 3.6 / 512;

    public int TopKmh(int gear) => gear < GearTopKmh.Count ? GearTopKmh[gear] : GearTopKmh.LastOrDefault(27);
}
