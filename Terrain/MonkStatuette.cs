using System.Numerics;
using LbaBodyStudio;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Dark Monk's statuette (Celebration Island, the souvenir seller's lost stock: RaceTrackStory.LavaLake's four and the egg's fifth --
// RaceTrackStory.Eggs.Celebration). It was SPRITES.HQR's picture 250 drawn at its full size, a statue taller than Twinsen, and a picture
// isn't sorted against the island's 3D: it seemed to move as he walked and stood out of a building's wall (the user, 2026-11-23: "The
// mini-statues of the dark monk statue that the souvenir seller has lost are absolutely massive. they seem to move around as Twinsen moves,
// and even clip through a building"). Now a little 3D statuette, a souvenir's size -- knee high to Twinsen: a stepped stone plinth, the
// hooded monk in his robe pointing ahead, the ring over his head as the great statue has it -- every face at its own angle, lit (the user,
// 2026-11-23: "make sure you use lots of normals to give the models a 3D look").
internal static class MonkStatuette
{
    public const int Generic = 252;
    // (the stone's ramp: dark slate-teal, low on it so the light stays in it; the plinth a darker grey)
    private const int Robe = 9 * 16 + 3, RobeDark = 9 * 16 + 1, Plinth = 11 * 16 + 2, PlinthDark = 11 * 16 + 1, Ring = 6 * 16 + 4, Shadow = 3 * 16 + 1;

    // (on an entity of their own -- CitadelProps.InstallOwn, 2026-11-24 -- its body this number; whoever places one gives it that entity)
    private static IEnumerable<(int, string, Func<byte[], Body>, short[]?)> Own => new (int, string, Func<byte[], Body>, short[]?)[] { (Generic, "a Dark Monk statuette", Build, Box) };
    public static List<string> Install(string gameDirectory) => new() { Citadel.CitadelProps.InstallOwn(gameDirectory, "the Dark Monk statuettes", Own).Log };
    public static int Entity(string gameDirectory) => Citadel.CitadelProps.InstallOwn(gameDirectory, "the Dark Monk statuettes", Own).Entity;
    // A statuette's actor, on the statuettes' entity
    public static SceneActorModel Actor(string gameDirectory, int x, int y, int z, int beta = 0)
    {
        var a = Citadel.CitadelProps.Actor(Generic, x, y, z, beta);
        if (Entity(gameDirectory) is var e && e >= 0) a.Entity = e;
        return a;
    }

    // (its box: the plinth and the figure -- small, so Twinsen comes right up to it)
    public static readonly short[] Box = { -150, 0, -150, 150, 760, 150 };

    private static Body Build(byte[] header)
    {
        var m = new CarMesh("a Dark Monk statuette", new[] { -1 });
        m.Light = 0.8f;
        // the plinth: a base, a bevelled step, a top slab
        m.Box(0, new Vector3(0, 30, 0), new Vector3(300, 60, 300), Plinth, PlinthDark);
        var b0 = m.Loop(0, new Vector3(0, 60, 0), new Vector3(150, 0, 0), new Vector3(0, 0, 150), 4);
        var b1 = m.Loop(0, new Vector3(0, 95, 0), new Vector3(118, 0, 0), new Vector3(0, 0, 118), 4);
        m.Skin(b0, b1, _ => Plinth, new Vector3(0, 70, 0));
        m.Box(0, new Vector3(0, 110, 0), new Vector3(170, 30, 170), Plinth, PlinthDark);
        // the robe: a fluted cone from the hem to the shoulders, eight faces round, flaring at the foot
        var rings = new (float Y, float R)[] { (125, 105), (190, 95), (330, 78), (450, 66), (480, 60) };
        var loops = rings.Select(r => m.Loop(0, new Vector3(0, r.Y, 0), new Vector3(r.R, 0, 0), new Vector3(0, 0, r.R), 8)).ToArray();
        for (var k = 0; k + 1 < loops.Length; k++) m.Skin(loops[k], loops[k + 1], i => i % 2 == 0 ? Robe : RobeDark, new Vector3(0, (rings[k].Y + rings[k + 1].Y) / 2, 0));
        // the hood: from the shoulders up to its point, leaning a little forward, the face's dark hollow under its brim
        var h0 = m.Loop(0, new Vector3(0, 480, 4), new Vector3(58, 0, 0), new Vector3(0, 0, 58), 8);
        var h1 = m.Loop(0, new Vector3(0, 560, 10), new Vector3(46, 0, 0), new Vector3(0, 0, 46), 8);
        m.Skin(h0, h1, i => i % 2 == 0 ? Robe : RobeDark, new Vector3(0, 520, 6));
        m.Cap(h1, m.P(0, new Vector3(0, 650, 22)), Robe, new Vector3(0, 590, 12));
        m.Box(0, new Vector3(0, 528, 48), new Vector3(52, 46, 14), Shadow, Shadow);
        // the pointing arm: a sleeve from the right shoulder forward and up, the hand at its end; the other arm folded at his side
        var shoulder = new Vector3(52, 452, 10);
        var hand = new Vector3(70, 520, 170);
        m.Strut(0, shoulder, hand, 20, Robe);
        m.Ball(0, hand, 18, RobeDark);
        m.Strut(0, new Vector3(-52, 450, 6), new Vector3(-44, 360, 34), 18, RobeDark);
        // the ring over his head, as the great statue's: eight bars round a hoop, upright behind the hood
        const int N = 8; const float R = 70;
        var ringC = new Vector3(0, 690, -10);
        for (var k = 0; k < N; k++)
        {
            var a0 = MathF.PI * 2 * k / N; var a1 = MathF.PI * 2 * (k + 1) / N;
            m.Strut(0, ringC + new Vector3(R * MathF.Cos(a0), R * MathF.Sin(a0), 0), ringC + new Vector3(R * MathF.Cos(a1), R * MathF.Sin(a1), 0), 7, Ring);
        }
        return m.ToBody(header);
    }
}
