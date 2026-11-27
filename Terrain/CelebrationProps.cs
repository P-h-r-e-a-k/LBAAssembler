using System.Numerics;
using LbaBodyStudio;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Celebration Island's ways to the Dark Monk statuettes (the user, 2026-11-26/27: "Let's add a ladder to the back of the building and have
// one at the top on the book. The floating platforms that were in the lava in the retail game, let's add them back in and have one in the
// centre of the lava on a fixed block, and have others that sink below, then re-emerge, and some that move backwards and forwards for a
// timing puzzle"): the temple's ladder up its back wall to the book that is its roof, the lava's stepping slabs -- basalt, glowing where
// the lava licks them -- and the fixed block in the lava's middle. Their own entity (CitadelProps.InstallOwn). Built lit, each face at its
// own angle (the user's rule: lots of normals).
internal static class CelebrationProps
{
    public const int Ladder = 1, Step = 2, Pillar = 3, Lift = 4;
    // the ladder: its foot at the actor's place, up its own +y LadderHigh, its rungs this far apart; against a wall on its own -z side
    public const int LadderHigh = 4900, RungGap = 250, LadderHalf = 230;
    // a stepping slab: its top at the actor's place (the box's top: a carrier carries what stands on it there), StepHalf either way, StepDeep down
    // (a cell wide: Twinsen's standing Sporty jump, measured 2026-11-27, lands 1,826-1,919 out -- from a slab's middle, 900 over the lava,
    // into the next one's middle)
    public const int StepHalf = 512, StepDeep = 420;
    // the lift in the lava's middle: a slab a little smaller (LiftHalf: out of a standing jump's reach of every shore while it is down)
    public const int LiftHalf = 448;
    // the block: its top at the actor's place, PillarHalf either way, a column down PillarDeep into the lava -- glowing only its last
    // PillarGlow, where the lava is
    public const int PillarHalf = 448, PillarDeep = 2500, PillarGlow = 520;
    private const int Lacquer = 4 * 16 + 3, LacquerDark = 4 * 16 + 1, Rope = 1 * 16 + 5, Rock = 3 * 16 + 2, RockDark = 3 * 16 + 1, RockTop = 3 * 16 + 4,
        Ember = 4 * 16 + 12, EmberHot = 6 * 16 + 13;

    private const string What = "Celebration Island's ways to the statuettes";
    private static IEnumerable<(int, string, Func<byte[], Body>, short[]?)> Own => new (int, string, Func<byte[], Body>, short[]?)[]
    {
        // (the ladder no box: climbed by its zone, walked past at its foot)
        (Ladder, "the temple's ladder", BuildLadder, new short[] { -1, 0, -1, 1, 1, 1 }),
        (Step, "a stepping slab in the lava", BuildStep, new short[] { -StepHalf, -StepDeep, -StepHalf, StepHalf, 0, StepHalf }),
        (Pillar, "the block in the lava's middle", BuildPillar, new short[] { -PillarHalf, -PillarDeep, -PillarHalf, PillarHalf, 0, PillarHalf }),
        (Lift, "the lift slab in the lava", BuildLift, new short[] { -LiftHalf, -StepDeep, -LiftHalf, LiftHalf, 0, LiftHalf }),
    };
    public static List<string> Install(string gameDirectory) => new() { Citadel.CitadelProps.InstallOwn(gameDirectory, What, Own).Log };
    public static int Entity(string gameDirectory) => Citadel.CitadelProps.InstallOwn(gameDirectory, What, Own).Entity;
    public static SceneActorModel Actor(string gameDirectory, int generic, int x, int y, int z, int beta = 0)
    {
        var a = Citadel.CitadelProps.Actor(generic, x, y, z, beta);
        if (Entity(gameDirectory) is var e && e >= 0) a.Entity = e;
        return a;
    }

    // The ladder: two red-lacquered rails, bevelled, a rung every RungGap bound with rope at each end, standing off the wall on stand-offs
    private static Body BuildLadder(byte[] header)
    {
        var m = new CarMesh("the temple's ladder", new[] { -1 });
        m.Light = 0.95f;
        const float Rail = 70, Off = 90;
        foreach (var x in new[] { -LadderHalf, LadderHalf })
        {
            // (a rail: four long faces and the two bevels at its corners -- an octagon's section)
            var low = m.Loop(0, new Vector3(x, 0, Off), new Vector3(Rail / 2, 0, 0), new Vector3(0, 0, Rail / 2), 8);
            var high = m.Loop(0, new Vector3(x, LadderHigh, Off), new Vector3(Rail / 2, 0, 0), new Vector3(0, 0, Rail / 2), 8);
            m.Skin(low, high, i => i % 2 == 0 ? Lacquer : LacquerDark, new Vector3(x, LadderHigh / 2f, Off));
            m.Cap(high, m.P(0, new Vector3(x, LadderHigh + 50, Off)), Lacquer, new Vector3(x, LadderHigh - 30, Off));
            // (stand-offs to the wall, every few rungs)
            for (var y = 600; y < LadderHigh; y += 1500)
                m.Box(0, new Vector3(x, y, Off / 2), new Vector3(Rail * 0.7f, Rail * 0.7f, Off), LacquerDark, Lacquer);
        }
        for (var y = RungGap; y < LadderHigh - 40; y += RungGap)
        {
            m.Strut(0, new Vector3(-LadderHalf, y, Off), new Vector3(LadderHalf, y, Off), 20, Lacquer);
            foreach (var x in new[] { -LadderHalf, LadderHalf }) m.Ball(0, new Vector3(x, y, Off), 30, Rope);
        }
        return m.ToBody(header);
    }

    // A stepping slab: a basalt plate, eight-sided, its top a little domed and its edge bevelled, its sides glowing hot down to the lava
    private static Body BuildStep(byte[] header)
    {
        var m = new CarMesh("a stepping slab in the lava", new[] { -1 });
        m.Light = 0.9f;
        Slab(m, StepHalf, StepDeep);
        return m.ToBody(header);
    }

    // The lift: a stepping slab a little smaller, the same rock -- it rises out of the lava high over it and sinks back
    private static Body BuildLift(byte[] header)
    {
        var m = new CarMesh("the lift slab in the lava", new[] { -1 });
        m.Light = 0.9f;
        Slab(m, LiftHalf, StepDeep);
        return m.ToBody(header);
    }

    // The block: a column of the same rock standing high out of the lava, its top a slab like the steps' -- the statuette's
    private static Body BuildPillar(byte[] header)
    {
        var m = new CarMesh("the block in the lava's middle", new[] { -1 });
        m.Light = 0.9f;
        Slab(m, PillarHalf, PillarDeep, PillarGlow);
        return m.ToBody(header);
    }

    // (glow: how far up from its foot its sides glow hot -- a slab's whole lower half, the column's only at the lava)
    private static void Slab(CarMesh m, float half, float deep, float glow = 0)
    {
        // (an octagon a little irregular -- a stone, not a coin -- its rings from the top down: the dome, the bevel, the sides, the hot foot)
        var r = half * 1.04f;
        int[] Ring(float y, float s, float wobble) => m.Loop(0, new Vector3(0, y, 0), new Vector3(r * s * (1 + wobble), 0, 0), new Vector3(0, 0, r * s * (1 - wobble)), 8);
        var top = m.P(0, new Vector3(0, 12, 0));
        var dome = Ring(-6, 0.62f, 0.03f);
        var edge = Ring(-30, 0.9f, 0.04f);
        var lip = Ring(-70, 1.0f, 0.05f);
        var side = Ring(glow > 0 ? -(deep - glow) : -deep * 0.55f, 0.97f, 0.06f);
        var foot = Ring(-deep, 0.9f, 0.05f);
        m.Cap(dome, top, RockTop, new Vector3(0, -40, 0));
        m.Skin(dome, edge, i => i % 2 == 0 ? RockTop : Rock, new Vector3(0, -18, 0));
        m.Skin(edge, lip, i => i % 2 == 0 ? Rock : RockDark, new Vector3(0, -50, 0));
        m.Skin(lip, side, i => i % 2 == 0 ? RockDark : Rock, new Vector3(0, glow > 0 ? -(deep - glow) / 2 : -deep * 0.3f, 0));
        m.Skin(side, foot, i => i % 2 == 0 ? Ember : EmberHot, new Vector3(0, glow > 0 ? -(deep - glow / 2) : -deep * 0.78f, 0));
    }
}
