using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The dartboards (the user, 2026-11-08, and 2026-11-12: "implementing a darts mini game for the citadel tavern ... for Twinsen to play
// against a random character from the car or in two player mode with Zoe then to play against Zoe"): a board on the Tavern's wall
// from chapter 2, when the Esmers have put the Tavern back; and one in the Tralu's bar under the sewers, on its wall between two of its
// barred windows, against the regulars at the table -- the user, 2026-11-15: "Let's add a dart board to the tralu bar area too between
// these two windows on the wall. Let's make the opponent one of the people sat at the table". The game is the engine's (RACEMOD.CPP
// DartFrame, the race file's darts= and dart_foe= lines, a board's after its own darts= line): Action by a board starts a match of 501
// against one of its foes, chosen at random -- or against Zoe, played by player 2, in a game with two players.
internal static partial class RaceTrackStory
{
    // a foe: its body and standing animation (BODY.HQR, ANIM.HQR: shown beside the board), its name, how steady its hand is (the tenths of
    // the board's radius its darts scatter by: smaller is better)
    // (and, for a regular sat in the scene, its entity and its walk -- ANIM.HQR: he gets up and walks over to ask the wager, the user, 2026-11-16,
    // "If Twinsen triggers a dart game, one of the NPCs at the table stands up, walks over to him, and asks the question"; -1: none)
    internal sealed record DartBoard(int Scene, int X, int Z, int Beta, int Floor, int FromChapter, (int Body, int Anim, string Name, int Scatter, int Entity, int Walk)[] Foes);

    // the Tavern's (scene 3): on the window wall in the corner at the end of the bar where the potted plant stood (the user, 2026-11-12:
    // "remove plant from the corner at the end of the bar and put the dart board against that wall instead"; the plant is
    // Citadel/TavernPlant's), facing the floor (beta 1024: toward +x) -- Luc behind his bar and Baldino best, Pat the worst
    // the Tralu's bar's (scene 19, the hall): on its west wall (its face x 14,336, cell 28) between the two barred windows nearest the
    // corner, over the floor (2,304), facing the room -- the regulars at the massive table: Pat, Fab and the drunk rabbibunny (the
    // Tavern's own, their game bodies: entities 29, 28, 25)
    internal static readonly DartBoard[] DartBoards =
    {
        new(TavernScene, 584, 7930, 1024, 1024, 2, new[]
        {
            (39, 282, "Luc", 9, -1, -1), (139, 715, "Baldino", 11, -1, -1), (32, 224, "Raph", 14, -1, -1), (40, 289, "Tim", 16, -1, -1), (59, 383, "Mr. Paul", 18, -1, -1), (44, 302, "Pat", 22, -1, -1),
        }),
        new(TraluBarHall, 14408, 24560, 1024, 2304, 1, new[]
        {
            // (Pat and Fab walk -- entities 29 and 28's walks; the drunk rabbibunny, entity 25, has none: he lurches over, his sway)
            (44, 302, "Pat", 20, 29, 304), (41, 297, "Fab", 15, 28, 299), (38, 277, "Bunny", 24, 25, 279),
        }),
    };
    private const int TraluBarHall = 19;
    internal const int DartsReach = 1500;
    // Zoe, for the game with two players: her body and animation, and her own steadiness
    internal const int ZoeDartBody = 26, ZoeDartAnim = 207;

    // (how far out from the wall the thrower stands, the oche just in front of him; the one waiting: further back and to the side, facing the board
    // -- the user, 2026-11-12: "Twinsen's opponent let's have them standing behind him, and watch them alternate positions"; the blackboard: along
    // the wall beside the board, its middle that far from the board's)
    internal const int DartsSpotOut = 1700, DartsOcheOut = 1500, DartsWaitOut = 2600, DartsWaitAside = 700, DartsSlateAside = 1000;

    // a point out from a board's wall (along its facing) and aside along the wall (+: toward larger z, with the board facing +x)
    private static (int X, int Z) DartsAt(DartBoard b, int outward, int aside)
    {
        var a = b.Beta * 2 * Math.PI / 4096;
        double fx = Math.Sin(a), fz = Math.Cos(a);
        return (b.X + (int)Math.Round(fx * outward - fz * aside), b.Z + (int)Math.Round(fz * outward + fx * aside));
    }

    private static void DartsTavern(StoryParts p)
    {
        foreach (var b in DartBoards)
        {
            if (!p.Store.SceneExists(b.Scene)) { p.Log.Add($"the dartboard of scene {b.Scene} left out: no such scene"); continue; }
            var from = b.FromChapter > 0 ? $"void comportement_0()\n{{\n    if ({b.FromChapter} > chapter())\n    {{\n        suicide();\n    }}\n}}\n" : "void comportement_0()\n{\n}\n";
            p.Edit(b.Scene, $"the dartboard on the wall (from chapter {b.FromChapter}): Action by it plays darts -- RACEMOD.CPP DartFrame", model =>
            {
                var board = Citadel.CitadelProps.Actor(Citadel.CitadelProps.Dartboard, b.X, b.Floor, b.Z, b.Beta);
                return p.Add(model, board, from);
            });
            p.Edit(b.Scene, "the darts' blackboard beside the board, for the scores", model =>
            {
                var (x, z) = DartsAt(b, 0, DartsSlateAside);
                var slate = Citadel.CitadelProps.Actor(Citadel.CitadelProps.DartScoreBoard, x, b.Floor, z, b.Beta);
                return p.Add(model, slate, from);
            });
            p.Edit(b.Scene, "the oche, the line the darts' throwers stand behind (walked over)", model =>
            {
                var (x, z) = DartsAt(b, DartsOcheOut, 0);
                var oche = Citadel.CitadelProps.Actor(Citadel.CitadelProps.DartOche, x, b.Floor, z, b.Beta);
                oche.Armor = WalkOverArmour;
                return p.Add(model, oche, from);
            });
            // (the one played against, standing where the waiting thrower stands: the engine gives it the foe's body and moves it -- hidden but in
            // a match, and walked through)
            p.Edit(b.Scene, "the darts' opponent's place (its body the foe's in a match: RACEMOD.CPP DartFrame)", model =>
            {
                var (x, z) = DartsAt(b, DartsWaitOut, DartsWaitAside);
                var stand = Citadel.CitadelProps.Actor(Citadel.CitadelProps.DartOche, x, b.Floor, z, (b.Beta + 2048) & 4095);
                stand.Armor = WalkOverArmour;
                return p.Add(model, stand, from);
            });
        }
    }

    // The race file's lines for the game: each board's darts= line, then its face's and its foes'
    internal static IEnumerable<string> DartsLines()
    {
        foreach (var b in DartBoards)
        {
            // (the board's middle: where the post stands, the board's height up; the spot: three cells out in front of it, along its facing;
            // the chapter it's played from)
            var (spotX, spotZ) = DartsAt(b, DartsSpotOut, 0);
            yield return $"darts={b.Scene} {spotX} {spotZ} {DartsReach} {b.X} {b.Z} {Citadel.CitadelProps.DartMiddleHigh + b.Floor} {b.FromChapter}";
            // (the board's face and size, for the darts stuck in it: its face's x, z out from the wall, its radius, its facing; the waiting place)
            var (faceX, faceZ) = DartsAt(b, Citadel.CitadelProps.DartStandOff, 0);
            var (waitX, waitZ) = DartsAt(b, DartsWaitOut, DartsWaitAside);
            yield return $"dart_board={faceX} {faceZ} {Citadel.CitadelProps.DartRadius} {b.Beta} {waitX} {waitZ}";
            foreach (var f in b.Foes) yield return $"dart_foe={f.Body} {f.Anim} {f.Scatter} {f.Name.Replace(' ', '_')}" + (f.Entity >= 0 ? $" {f.Entity} {f.Walk}" : "");
        }
        yield return $"dart_zoe={ZoeDartBody} {ZoeDartAnim} 12 Zoe";
    }
}
