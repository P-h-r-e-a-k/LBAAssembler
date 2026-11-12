using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Tavern's dartboard (the user, 2026-11-08, and 2026-11-12: "implementing a darts mini game for the citadel tavern ... for Twinsen to play
// against a random character from the car or in two player mode with Zoe then to play against Zoe"): a board on a post in the Tavern's floor
// from chapter 2, when the Esmers have put the Tavern back; the game is the engine's (RACEMOD.CPP DartFrame, the race file's darts= and
// dart_foe= lines): Action by the board starts a match of 301 against one of the foes below, chosen at random -- or against Zoe, played by
// player 2, in a game with two players.
internal static partial class RaceTrackStory
{
    // the board on the Tavern's wall (scene 3), on the window wall in the corner at the end of the bar where the potted plant stood (the user,
    // 2026-11-12: "remove plant from the corner at the end of the bar and put the dart board against that wall instead"; the plant is
    // Citadel/TavernPlant's), facing the floor (beta 1024: toward +x) -- and where Twinsen stands to play, a few steps out in front of it
    internal const int DartsX = 584, DartsZ = 7680, DartsBeta = 1024, DartsReach = 1500;
    // a foe: its body and standing animation (to be shown beside the board), its name, how steady its hand is (the tenths of the board's
    // radius its darts scatter by: smaller is better) -- Luc behind his bar and Baldino best, Pat the worst
    internal static readonly (int Body, int Anim, string Name, int Scatter)[] DartFoes =
    {
        (39, 282, "Luc", 9), (139, 715, "Baldino", 11), (32, 224, "Raph", 14), (40, 289, "Tim", 16), (59, 383, "Mr. Paul", 18), (44, 302, "Pat", 22),
    };
    // Zoe, for the game with two players: her body and animation, and her own steadiness
    internal const int ZoeDartBody = 26, ZoeDartAnim = 207;

    private static void DartsTavern(StoryParts p)
    {
        p.Edit(TavernScene, "the Tavern's dartboard on its post (from chapter 2): Action by it plays darts -- RACEMOD.CPP DartFrame", model =>
        {
            var board = Citadel.CitadelProps.Actor(Citadel.CitadelProps.Dartboard, DartsX, 1024, DartsZ, DartsBeta);
            return p.Add(model, board, "void comportement_0()\n{\n    if (2 > chapter())\n    {\n        suicide();\n    }\n}\n");
        });
    }

    // The race file's lines for the game
    internal static IEnumerable<string> DartsLines()
    {
        // (the board's middle: where the post stands, the board's height up; the spot: three cells out in front of it, along its facing)
        var a = DartsBeta * 2 * Math.PI / 4096;
        var spotX = DartsX + (int)Math.Round(Math.Sin(a) * 1700);
        var spotZ = DartsZ + (int)Math.Round(Math.Cos(a) * 1700);
        yield return $"darts={TavernScene} {spotX} {spotZ} {DartsReach} {DartsX} {DartsZ} {Citadel.CitadelProps.DartMiddleHigh + 1024}";
        foreach (var f in DartFoes) yield return $"dart_foe={f.Body} {f.Anim} {f.Scatter} {f.Name.Replace(' ', '_')}";
        yield return $"dart_zoe={ZoeDartBody} {ZoeDartAnim} 12 Zoe";
    }
}
