namespace LBAAssembler.Terrain;

// A race's level, easy, medium or hard (2026-10-17, the user: "Let's also create an easy/medium/hard mode, but let's focus that on the
// temple track for now, we can worry about the other tracks at a later date"): what the level changes, always on the opponents' side --
// the player's car is as his setup makes it --
//   Skill    points added to every opponent's skill (the one to beat's, the car setup's MainSkill, and the others' range round it);
//   CatchUp  how much harder one pushes when it has fallen behind the player (percent at 60 cells behind: RaceCarSetup.CatchUpPercent, 8,
//            is medium's; none when the setup's "they push harder" is off);
//   Ease     how much one ahead of the player eases off (percent at 60 cells ahead): the rubber band of an easy race;
//   Items    how many of the mushroom rows an opponent takes an item from (percent);
//   AimMs    0: an opponent uses an item as soon as it takes it, as before; else it holds an attack -- a penguin, oil, a fake mushroom, the
//            yellow, green or red ball -- until it would hit the player, this long at most (RACEMOD.CPP OpponentAims);
//   Hazards  how often the track's own hazards come at the cars (percent: the Temple's witches' fire balls and their dives).
// Medium is the race as it was before the levels. A track has levels when its island says so (RaceTrackIsland.Levels): the race file then
// has them all (difficulty_level=) and the one the setup races (difficulty=); the race game's menu adds the player's pick after them. A
// race with no opponents (a time trial) has none: its hazards are as before.
internal sealed record RaceLevel(int Skill, int CatchUp, int Ease, int Items, int AimMs, int Hazards)
{
    // the engine's line for it, as level n (with `catchUp` false: the setup's "they push harder" off -- no catch-up)
    public string Line(int n, bool catchUp = true) => $"difficulty_level={n} {Skill} {(catchUp ? CatchUp : 0)} {Ease} {Items} {AimMs} {Hazards}";
}

internal static class RaceDifficulty
{
    public const int Easy = 0, Medium = 1, Hard = 2;
    public static readonly string[] Names = { "Easy", "Medium", "Hard" };

    public static string Name(int level) => Names[Math.Clamp(level, Easy, Hard)];

    // The Temple of Bú's (the benchmark behind them: docs/LBA2_TEMPLE_OF_BU.md, "Easy, medium and hard")
    public static readonly RaceLevel[] Temple =
    {
        new(Skill: -10, CatchUp: 0, Ease: 15, Items: 40, AimMs: 0, Hazards: 50),
        new(Skill: 0, CatchUp: RaceCarSetup.CatchUpPercent, Ease: 0, Items: 100, AimMs: 0, Hazards: 100),
        new(Skill: 8, CatchUp: 14, Ease: 0, Items: 100, AimMs: 6000, Hazards: 140),
    };

    // Every other track's (2026-11-06, the user: "Let's have the game have a difficulty setting that sets how hard each race is to compete.
    // We can probably reuse our ghost information for this"), on the ghosts' scale. The game's ghosts to beat were driven at a share of the
    // line's pace -- Easy at 70 %, Medium at 85 %, Hard at 100 % with the best car and the shortcuts (E:\dump\TEMP\lbra\alltracks\gen.ps1)
    // -- and an opponent's skill is that same share of its own line's perfect lap (RACEMOD.CPP: "a perfect lap 63.8 s (skill 86%: 74.2 s)").
    // The races' opponents have driven at 86 %, the Medium ghost's pace: Easy takes them down to the Easy ghost's, Hard up towards the Hard
    // ghost's (+12, not +15: the Hard ghost's shortcuts are not on an opponent's line). The ghosts' own laps can't be the targets: the race
    // game's time trials are their tracks' laps, the story's races mostly other ones (Wannies Island's story lap is 64 s, its ghosts' 11.7).
    public const int GhostEasyPace = 70, GhostMediumPace = 85, GhostHardPace = 100;
    public static readonly RaceLevel[] Standard =
    {
        new(Skill: GhostEasyPace - GhostMediumPace, CatchUp: 0, Ease: 12, Items: 50, AimMs: 0, Hazards: 100),
        new(Skill: 0, CatchUp: RaceCarSetup.CatchUpPercent, Ease: 0, Items: 100, AimMs: 0, Hazards: 100),
        new(Skill: 12, CatchUp: 14, Ease: 0, Items: 100, AimMs: 6000, Hazards: 100),
    };

    // What a level does, in a line for the Race car window.
    public static string Describe(RaceLevel l)
    {
        var parts = new List<string>();
        parts.Add(l.Skill == 0 ? "the opponents at the skill set above" : $"the opponents {Math.Abs(l.Skill)} points {(l.Skill > 0 ? "quicker" : "slower")} than the skill set above");
        parts.Add(l.CatchUp > 0 ? $"pushing up to {l.CatchUp} % harder when behind you" : "never pushing harder when behind you");
        if (l.Ease > 0) parts.Add($"easing off up to {l.Ease} % when ahead of you");
        parts.Add(l.Items >= 100 ? "an item from every row of pumpkins" : $"items from {l.Items} % of the rows");
        if (l.AimMs > 0) parts.Add($"holding penguins, oil and magic balls until they would hit you ({l.AimMs / 1000.0:0.#} s at most)");
        if (l.Hazards != 100) parts.Add($"the Temple's witches {(l.Hazards < 100 ? "less" : "more")} often ({l.Hazards} %)");
        return string.Join("; ", parts) + ".";
    }
}
