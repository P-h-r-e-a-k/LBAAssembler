namespace LBAAssembler.Terrain;

// Who races on a track: each driver a car of the racer's entity (its body: 0 the retail track's racer, 1 Baldino's rocket car, 2 and on
// the cars made after the game's characters, RaceTrackCharacterCars) or the motorbike Rabbibunny (Bike), on a racing line of its own
// (RaceTrackBuilder.PlanRacePath: the side of the road it leans to, its top speed and pull, its cornering) and with a skill a few points
// either side of the car setup's (SkillShift). A Ghost has a line and no car: its lap at its skill is the time to beat (the car file's
// beat=; Raph's on Citadel Island's storm track).
//
// The rosters are the user's (CharactersForTrack.txt, 2026-10-03): an island with none races the retail track's racer, Baldino and the biker.
internal sealed record RaceDriver(string Name, int Body, RaceTrackBuilder.RacingLine Line, int SkillShift = 0, bool Bike = false, bool Ghost = false)
{
    public const int RacerBody = 0;

    // the racer, Baldino and the biker: every track's before the rosters, and still any track's with none
    public static readonly RaceDriver Racer = new("The racer", RacerBody, RaceTrackBuilder.RacerLine);
    public static readonly RaceDriver Baldino = new("Baldino", RaceTrackBaldinoCar.Generic, RaceTrackBuilder.BaldinoLine, -1);
    public static readonly RaceDriver Biker = new(RaceCarEngineFile.BikerName, -1, RaceTrackBuilder.BikerLine, -2, Bike: true);

    private static RaceDriver Of(RaceTrackCharacterCars.Car car, string name, double side, double top, double grip, int shift) =>
        new(name, car.Generic, new RaceTrackBuilder.RacingLine(side, 4, top, grip), shift);

    // Citadel Island, the storm track: Raph, the lighthouse keeper, too busy racing to let the weather wizard up the lighthouse -- "beat my
    // time" (StoryNotes.txt). His time is his lap at a skill a few points under the setup's.
    public static readonly List<RaceDriver> CitadelStorm = new()
    {
        Of(RaceTrackCharacterCars.Raph, "Raph", -1.5, 1.0, 1.0, -6) with { Ghost = true },
    };

    // Citadel Island, the town circuit (the aliens' "even better race track", the day after the storm): Raph, Zoe, Mr. Paul (whose prize
    // is a ferry ticket), the Tralu and the thief
    public static readonly List<RaceDriver> CitadelTown = new()
    {
        Of(RaceTrackCharacterCars.Raph, "Raph", -1.5, 1.0, 1.0, 0),
        Of(RaceTrackCharacterCars.Zoe, "Zoe", 1.5, 0.99, 1.03, -1),
        Of(RaceTrackCharacterCars.Paul, "Mr. Paul", 2.5, 0.98, 0.97, -2),
        Of(RaceTrackCharacterCars.Tralu, "The Tralu", 0, 1.03, 0.93, -2),
        Of(RaceTrackCharacterCars.Thief, "The thief", -2.5, 1.02, 0.99, -1),
    };

    // The Desert island: Moya, the Dino-Fly, the Dean of the School of Magic, the retail track's racer and Baldino
    public static readonly List<RaceDriver> Desert = new()
    {
        Of(RaceTrackCharacterCars.Moya, "Moya", 2.5, 0.95, 1.08, -2),
        Of(RaceTrackCharacterCars.DinoFly, "The Dino-Fly", 0, 1.04, 0.94, -1),
        Of(RaceTrackCharacterCars.Dean, "The Dean", -1.5, 0.99, 1.0, -2),
        Racer,
        Baldino,
    };

    // The Emerald Moon: Baldino in his space suit, in his lander
    public static readonly List<RaceDriver> Emerald = new()
    {
        Of(RaceTrackCharacterCars.Lander, "Baldino", 0, 1.02, 0.95, 0),
    };

    // Mosquibees Island: the Queen, and the monkey monster with the sword in his war cart
    public static readonly List<RaceDriver> Mosquibe = new()
    {
        Of(RaceTrackCharacterCars.Queen, "The Queen", -2.5, 1.01, 1.0, 0),
        Of(RaceTrackCharacterCars.WarCart, "The monkey monster", 2.5, 0.98, 1.03, -1),
    };

    // Otringal's palace has no track yet; its drivers are Stan, the pighead with the broom and the two-headed monster (CharactersForTrack.txt)
}
