using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Tavern's dartboard (the user, 2026-11-08, and 2026-11-12: "implementing a darts mini game for the citadel tavern ... for Twinsen to play
// against a random character from the car or in two player mode with Zoe then to play against Zoe"): a board on the Tavern's wall
// from chapter 2, when the Esmers have put the Tavern back; the game is the engine's (RACEMOD.CPP DartFrame, the race file's darts= and
// dart_foe= lines): Action by the board starts a match of 501 against one of the foes below, chosen at random -- or against Zoe, played by
// player 2, in a game with two players.
internal static partial class RaceTrackStory
{
    // the board on the Tavern's wall (scene 3), on the window wall in the corner at the end of the bar where the potted plant stood (the user,
    // 2026-11-12: "remove plant from the corner at the end of the bar and put the dart board against that wall instead"; the plant is
    // Citadel/TavernPlant's), facing the floor (beta 1024: toward +x) -- and where Twinsen stands to play, a few steps out in front of it
    internal const int DartsX = 584, DartsZ = 7930, DartsBeta = 1024, DartsReach = 1500;
    // a foe: its body and standing animation (to be shown beside the board), its name, how steady its hand is (the tenths of the board's
    // radius its darts scatter by: smaller is better) -- Luc behind his bar and Baldino best, Pat the worst
    internal static readonly (int Body, int Anim, string Name, int Scatter)[] DartFoes =
    {
        (39, 282, "Luc", 9), (139, 715, "Baldino", 11), (32, 224, "Raph", 14), (40, 289, "Tim", 16), (59, 383, "Mr. Paul", 18), (44, 302, "Pat", 22),
    };
    // Zoe, for the game with two players: her body and animation, and her own steadiness
    internal const int ZoeDartBody = 26, ZoeDartAnim = 207;

    // (how far out from the wall the thrower stands, the oche just in front of him; the one waiting: further back and to the side, facing the board
    // -- the user, 2026-11-12: "Twinsen's opponent let's have them standing behind him, and watch them alternate positions"; the blackboard: along
    // the wall beside the board, its middle that far from the board's)
    internal const int DartsSpotOut = 1700, DartsOcheOut = 1500, DartsWaitOut = 2600, DartsWaitAside = 700, DartsSlateAside = 1000;

    // a point out from the board's wall (along its facing) and aside along the wall (+: toward larger z, with the board facing +x)
    private static (int X, int Z) DartsAt(int outward, int aside)
    {
        var a = DartsBeta * 2 * Math.PI / 4096;
        double fx = Math.Sin(a), fz = Math.Cos(a);
        return (DartsX + (int)Math.Round(fx * outward - fz * aside), DartsZ + (int)Math.Round(fz * outward + fx * aside));
    }

    private static void DartsTavern(StoryParts p)
    {
        const string fromChapter2 = "void comportement_0()\n{\n    if (2 > chapter())\n    {\n        suicide();\n    }\n}\n";
        p.Edit(TavernScene, "the Tavern's dartboard on the wall (from chapter 2): Action by it plays darts -- RACEMOD.CPP DartFrame", model =>
        {
            var board = Citadel.CitadelProps.Actor(Citadel.CitadelProps.Dartboard, DartsX, 1024, DartsZ, DartsBeta);
            return p.Add(model, board, fromChapter2);
        });
        p.Edit(TavernScene, "the darts' blackboard beside the board, for the scores", model =>
        {
            var (x, z) = DartsAt(0, DartsSlateAside);
            var slate = Citadel.CitadelProps.Actor(Citadel.CitadelProps.DartScoreBoard, x, 1024, z, DartsBeta);
            return p.Add(model, slate, fromChapter2);
        });
        p.Edit(TavernScene, "the oche, the line the darts' throwers stand behind (walked over)", model =>
        {
            var (x, z) = DartsAt(DartsOcheOut, 0);
            var oche = Citadel.CitadelProps.Actor(Citadel.CitadelProps.DartOche, x, 1024, z, DartsBeta);
            oche.Armor = WalkOverArmour;
            return p.Add(model, oche, fromChapter2);
        });
        // (the one played against, standing where the waiting thrower stands: the engine gives it the foe's body and moves it -- hidden but in a
        // match, and walked through)
        p.Edit(TavernScene, "the darts' opponent's place (its body the foe's in a match: RACEMOD.CPP DartFrame)", model =>
        {
            var (x, z) = DartsAt(DartsWaitOut, DartsWaitAside);
            var stand = Citadel.CitadelProps.Actor(Citadel.CitadelProps.DartOche, x, 1024, z, (DartsBeta + 2048) & 4095);
            stand.Armor = WalkOverArmour;
            return p.Add(model, stand, fromChapter2);
        });
    }

    // The race file's lines for the game
    internal static IEnumerable<string> DartsLines()
    {
        // (the board's middle: where the post stands, the board's height up; the spot: three cells out in front of it, along its facing)
        var (spotX, spotZ) = DartsAt(DartsSpotOut, 0);
        yield return $"darts={TavernScene} {spotX} {spotZ} {DartsReach} {DartsX} {DartsZ} {Citadel.CitadelProps.DartMiddleHigh + 1024}";
        // (the board's face and size, for the darts stuck in it: its face's x, z out from the wall, its radius, its facing; the waiting place)
        var (faceX, faceZ) = DartsAt(Citadel.CitadelProps.DartStandOff, 0);
        var (waitX, waitZ) = DartsAt(DartsWaitOut, DartsWaitAside);
        yield return $"dart_board={faceX} {faceZ} {Citadel.CitadelProps.DartRadius} {DartsBeta} {waitX} {waitZ}";
        foreach (var f in DartFoes) yield return $"dart_foe={f.Body} {f.Anim} {f.Scatter} {f.Name.Replace(' ', '_')}";
        yield return $"dart_zoe={ZoeDartBody} {ZoeDartAnim} 12 Zoe";
    }
}
