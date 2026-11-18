using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The bowling alley's bar and games room, third round (the user, 2026-11-16):
//   - the bar's counter, straight, in three pieces making a U whose gap is against the north wall ("the U shape of the bar in the bowling
//     alley needs turning around so the gap is against the wall" -- Rick's bar's blocks can't be turned): the front run facing the room
//     (+z), the arms facing out west and east. Each piece built in its own direction: an actor's box isn't turned with it;
//   - the bottles' shelf on the wall behind the barman;
//   - a pool table, a cue standing against it, and a player bent over the table with his cue, baked in three poses -- drawn back, at the
//     ball, through -- a track changing them ("perhaps a pool table for a couple of NPCs to be playing on to help fill up the space").
internal static partial class CitadelProps
{
    public const int BarFront = 135, BarWest = 136, BarEast = 137, BarShelf = 138, PoolTable = 139;
    public const int PoolCueLaid = 76, PoolCue = 79;
    // (the two balls in play, actors of their own so they can roll: the white the sailor strikes and the red it sends into a pocket --
    // the user, 2026-11-17, "Our pool players in the bowling alley don't appear to actually be playing". The rest of the balls stay part
    // of the table, racked at its far end)
    // (193 and 194, not 189 and 190: those are the office's beast-hide tiles -- BeastFirst. The props' generic numbers are one space for
    // every family in CitadelProps, and the build log's "(body N)" lines are the list of what is taken)
    public const int PoolWhite = 193, PoolRed = 194;
    public const int PoolBallR = 26;
    public const int BarFrontLength = 4096, BarArmLength = 3164, BarDepth = 420, BarHigh = 600, ShelfLength = 3000;
    public const int PoolLength = 2000, PoolWidth = 1100, PoolTop = 520;
    // (the pool table's box the table alone: its cue reaches out over where the player stands)
    // (the bar's pieces each their own long box, its top over the slab: with none the engine gave each a square of (width + depth) / 4 round
    // its middle -- an arm's reached 896 along it of its 1,582, so Twinsen walked into the arm's front half, and with his box south of the
    // arm's the engine's sort drew him over it: the user, 2026-11-16, "We need to make sure the bar is solid all the way around. Twinsen
    // can't walk any further but has visually walked through the left side of the bar")
    private const int BarHalfDeep = BarDepth / 2 + 50, BarBoxTop = BarHigh + 60;
    public static short[]? BarBox(int generic) => generic switch
    {
        PoolTable => new short[] { -PoolLength / 2, 0, -PoolWidth / 2, PoolLength / 2, PoolTop + 80, PoolWidth / 2 },
        // (a ball's box stands up out of the table's: the table's box top is PoolTop + 80, and a box inside another leaves the sort free to
        // draw the ball behind the table -- see BuildPoolBall)
        PoolWhite or PoolRed => new short[] { -PoolBallR - 10, 0, -PoolBallR - 10, PoolBallR + 10, PoolTop + 200, PoolBallR + 10 },
        BarFront => new short[] { -BarFrontLength / 2, 0, -BarHalfDeep, BarFrontLength / 2, BarBoxTop, BarHalfDeep },
        BarWest or BarEast => new short[] { -BarHalfDeep, 0, -BarArmLength / 2, BarHalfDeep, BarBoxTop, BarArmLength / 2 },
        _ => null,
    };
    private const int BarWood = 1 * 16 + 3, BarWoodDark = 1 * 16 + 1, BarTop = 4 * 16 + 4, BarBrass = 6 * 16 + 7;

    // A straight piece of counter `length` long, its front to `front` (+z, -x or +x), round its own middle: the front panel and its brass foot
    // rail, the back (the barman's side) with a shelf under the top, the top slab overhanging both, its ends closed
    private static Body BuildBarCounter(byte[] header, float length, Vector3 front)
    {
        var m = new CarMesh("the alley's bar", new[] { -1 });
        m.Light = 0.42f;
        var along = new Vector3(MathF.Abs(front.Z), 0, MathF.Abs(front.X));     // the piece's length across its front
        Vector3 At(float a, float d, float y) => along * a + front * d + new Vector3(0, y, 0);
        const float Slab = 44, Over = 50;
        float h = length / 2, d = BarDepth / 2;
        int[] Quad(Vector3 p, Vector3 q, Vector3 r, Vector3 s) => new[] { m.P(0, p), m.P(0, q), m.P(0, r), m.P(0, s) };
        var middle = new Vector3(0, BarHigh / 2, 0);
        // the front's panels (a darker frame round each), the back, the ends
        const int Panels = 8;
        for (var k = 0; k < Panels; k++)
        {
            float a0 = -h + k * length / Panels, a1 = a0 + length / Panels;
            m.Out(Quad(At(a0, d, 0), At(a1, d, 0), At(a1, d, BarHigh), At(a0, d, BarHigh)), BarWoodDark, middle);
            m.Out(Quad(At(a0 + 40, d + 6, 90), At(a1 - 40, d + 6, 90), At(a1 - 40, d + 6, BarHigh - 70), At(a0 + 40, d + 6, BarHigh - 70)), BarWood, middle);
        }
        m.Out(Quad(At(-h, -d, 0), At(h, -d, 0), At(h, -d, BarHigh), At(-h, -d, BarHigh)), BarWoodDark, middle);
        foreach (var e in new[] { -h, h })
            m.Out(Quad(At(e, -d, 0), At(e, d, 0), At(e, d, BarHigh), At(e, -d, BarHigh)), BarWood, middle + along * MathF.Sign(e) * h);
        // the top, its front edge, the foot rail
        m.Up(Quad(At(-h - Over, -d - Over, BarHigh + Slab), At(h + Over, -d - Over, BarHigh + Slab), At(h + Over, d + Over, BarHigh + Slab), At(-h - Over, d + Over, BarHigh + Slab)), BarTop);
        m.Out(Quad(At(-h - Over, d + Over, BarHigh), At(h + Over, d + Over, BarHigh), At(h + Over, d + Over, BarHigh + Slab), At(-h - Over, d + Over, BarHigh + Slab)), BarTop - 2, middle);
        m.Out(Quad(At(-h - Over, -d - Over, BarHigh), At(h + Over, -d - Over, BarHigh), At(h + Over, -d - Over, BarHigh + Slab), At(-h - Over, -d - Over, BarHigh + Slab)), BarTop - 2, middle);
        m.Out(Quad(At(-h, d + 70, 100), At(h, d + 70, 100), At(h, d + 70, 140), At(-h, d + 70, 140)), BarBrass, middle);
        // glasses and a bottle or two on the top
        var rnd = new Random((int)length);
        for (var a = -h + 260; a < h - 200; a += 420 + rnd.Next(0, 300))
        {
            var p = At(a, rnd.Next(-60, 60), BarHigh + Slab);
            if (rnd.Next(3) == 0) m.Box(0, p + new Vector3(0, 110, 0), new Vector3(46, 220, 46), 7 * 16 + 4, top: 7 * 16 + 6);
            else m.Box(0, p + new Vector3(0, 45, 0), new Vector3(40, 90, 40), 3 * 16 + 9, top: 6 * 16 + 9);
        }
        return m.ToBody(header);
    }
    public static Body BuildBarFront(byte[] header) => BuildBarCounter(header, BarFrontLength, Vector3.UnitZ);
    public static Body BuildBarWest(byte[] header) => BuildBarCounter(header, BarArmLength, -Vector3.UnitX);
    public static Body BuildBarEast(byte[] header) => BuildBarCounter(header, BarArmLength, Vector3.UnitX);

    // The bottles' shelf: a back board on the wall (its back at -z), three shelves, bottles of every colour, a mirror strip
    public static Body BuildBarShelf(byte[] header)
    {
        var m = new CarMesh("the alley's bottles", new[] { -1 });
        m.Light = 0.5f;
        const float H = ShelfLength / 2f, Depth = 240, High = 1700;
        m.In(new[] { m.P(0, new(-H, 0, 0)), m.P(0, new(H, 0, 0)), m.P(0, new(H, High, 0)), m.P(0, new(-H, High, 0)) }, BarWoodDark, new Vector3(0, High / 2, 400));
        m.In(new[] { m.P(0, new(-H + 80, 1050, 4)), m.P(0, new(H - 80, 1050, 4)), m.P(0, new(H - 80, 1500, 4)), m.P(0, new(-H + 80, 1500, 4)) }, 3 * 16 + 6, new Vector3(0, 1200, 400));
        int[] colours = { 7 * 16 + 5, 4 * 16 + 6, 6 * 16 + 9, 12 * 16 + 6, 2 * 16 + 10, 8 * 16 + 6 };
        var rnd = new Random(16);
        foreach (var y in new[] { 300f, 650f, 1000f })
        {
            m.Box(0, new Vector3(0, y - 15, Depth / 2), new Vector3(ShelfLength, 30, Depth), BarWood, top: BarWood + 1);
            for (var x = -H + 120; x < H - 100; x += 110 + rnd.Next(0, 90))
            {
                var tall = 180 + rnd.Next(0, 120);
                m.Box(0, new Vector3(x, y + tall / 2, Depth / 2), new Vector3(56, tall, 56), colours[rnd.Next(colours.Length)], top: 3 * 16 + 4);
            }
        }
        foreach (var x in new[] { -H, H }) m.Box(0, new Vector3(x, High / 2, Depth / 2), new Vector3(60, High, Depth), BarWood);
        return m.ToBody(header);
    }

    // The pool table: along x, green cloth in wooden cushions, six pockets, thick legs; the balls racked at one end, the white at the other
    public static Body BuildPoolTable(byte[] header)
    {
        var m = new CarMesh("the alley's pool table", new[] { -1 });
        m.Light = 0.45f;
        const float L = PoolLength / 2f, W = PoolWidth / 2f, Rail = 110;
        const int Cloth = 8 * 16 + 5, Wood = 1 * 16 + 4, Pocket = 3 * 16 + 0;
        m.Up(new[] { m.P(0, new(-L + Rail, PoolTop, -W + Rail)), m.P(0, new(L - Rail, PoolTop, -W + Rail)), m.P(0, new(L - Rail, PoolTop, W - Rail)), m.P(0, new(-L + Rail, PoolTop, W - Rail)) }, Cloth);
        // (the cushions and the rails, the apron under them)
        foreach (var s in new[] { -1, 1 })
        {
            m.Box(0, new Vector3(0, PoolTop + 30, s * (W - Rail / 2)), new Vector3(PoolLength, 60, Rail), Wood, top: Wood + 2);
            m.Box(0, new Vector3(s * (L - Rail / 2), PoolTop + 30, 0), new Vector3(Rail, 60, PoolWidth - 2 * Rail), Wood, top: Wood + 2);
        }
        m.Box(0, new Vector3(0, PoolTop - 90, 0), new Vector3(PoolLength, 180, PoolWidth), Wood - 2);
        foreach (var (x, z) in new[] { (-L + 60, -W + 60), (0, -W + 50), (L - 60, -W + 60), (-L + 60, W - 60), (0, W - 50), (L - 60, W - 60) })
            m.Box(0, new Vector3(x, PoolTop + 62, z), new Vector3(90, 6, 90), Pocket);
        foreach (var (x, z) in new[] { (-L + 160, -W + 160), (L - 160, -W + 160), (-L + 160, W - 160), (L - 160, W - 160) })
            m.Box(0, new Vector3(x, (PoolTop - 180) / 2, z), new Vector3(150, PoolTop - 180, 150), Wood - 2);
        // (the balls: a triangle of reds and golds, the black in it, the white)
        var balls = new[] { 4 * 16 + 7, 6 * 16 + 10, 4 * 16 + 7, 6 * 16 + 10, 3 * 16 + 1, 4 * 16 + 7, 6 * 16 + 10, 4 * 16 + 7, 6 * 16 + 10, 4 * 16 + 7 };
        var n = 0;
        for (var row = 0; row < 4; row++)
            for (var k = 0; k <= row; k++)
                m.Ball(0, new Vector3(L - 420 + row * 52, PoolTop + 26, (k - row / 2f) * 60), 26, balls[n++ % balls.Length]);
        // (the white is an actor now -- PoolWhite, struck up the table -- so it is not part of the table any more)
        // (the cue is an actor now -- PoolCueLaid, slid back and through with his stroke on the same clock as his animation. Baked into the
        // table it stood still while his hands drew back and struck, so his hands passed straight through it: the user, 2026-11-17, "Our
        // pool players in the bowling alley don't appear to actually be playing")
        return m.ToBody(header);
    }

    // A ball in play on the cloth, an actor of its own so it can roll (PoolWhite, PoolRed): its own box, taller than the table's box top,
    // so the engine's sort has the two apart -- a box wholly inside the table's left the ball drawn behind the table it sits on
    // (reference: the catwalk and the bar's counter, where the same sort put an actor on the wrong side of a prop)
    public static Body BuildPoolBall(byte[] header, int colour)
    {
        var m = new CarMesh("a pool ball", new[] { -1 });
        m.Light = 0.75f;
        m.Ball(0, new Vector3(0, PoolBallR, 0), PoolBallR, colour);
        return m.ToBody(header);
    }

    // A cue leaning against the table's end (its tip up, at -x of its foot)
    public static Body BuildPoolCue(byte[] header)
    {
        var m = new CarMesh("a cue", new[] { -1 });
        m.Light = 0.6f;
        m.Strut(0, new Vector3(0, 0, 0), new Vector3(-260, 1350, 0), 30, 2 * 16 + 8);
        m.Strut(0, new Vector3(-260, 1350, 0), new Vector3(-268, 1400, 0), 18, 3 * 16 + 13);
        return m.ToBody(header);
    }

    // The pool player's stance (BowlingAnims.Pool, the sailor -- entity 297 -- bent over the table, facing +z): his bones' ways, the torso 2
    // over the hips, the right arm 7 8 drawing the cue back (`stroke` 0), at the ball (1) or through (2), the left 12 13 the bridge, the legs
    // 17 18 and 21 22 straight; his hips down PoolDrop
    public const int PoolDrop = 120, PoolStand = 330;
    public static Dictionary<int, Vector3> PoolAims(int stroke)
    {
        static Vector3 N(float x, float y, float z) => Vector3.Normalize(new Vector3(x, y, z));
        // (3: at the ball but a touch back -- the sighting waggle of his stance between shots, so he is not frozen over the table)
        var fore = stroke switch { 0 => N(0.05f, -0.75f, -0.66f), 1 => N(0, -1, 0.05f), 3 => N(0.02f, -0.95f, -0.2f), _ => N(0, -0.55f, 0.84f) };
        return new Dictionary<int, Vector3>
        {
            [2] = N(0, 0.5f, 0.87f),
            [17] = N(0.12f, -1, -0.05f), [18] = N(0, -1, 0.05f), [21] = N(-0.12f, -1, 0.1f), [22] = N(0, -1, 0.05f),
            [12] = N(-0.2f, -0.35f, 0.92f), [13] = N(0.12f, -0.55f, 0.83f),
            [7] = N(0.3f, 0.05f, -0.95f), [8] = fore,
        };
    }

    // His cue, laid along his line of play (+z, placed where he stands and turned as he is): its butt back by his right hip, over the cloth
    // past where his left hand bridges it, its tip at the white
    public static Body BuildPoolCueLaid(byte[] header)
    {
        var m = new CarMesh("the pool player's cue", new[] { -1 });
        m.Light = 0.7f;
        // (its box only just over the table's -- the table's drawn over a cue whose box runs into it: the tip over the cushion, the white
        // there; the player stands PoolStand back from the table's end)
        Vector3 butt = new(-150, PoolTop + 250, -420), tip = new(-40, PoolTop + 90, PoolStand + 60);   // (on his side toward the camera: -x)
        m.Strut(0, butt, tip, 30, 2 * 16 + 9);
        m.Strut(0, tip, tip + Vector3.Normalize(tip - butt) * 50, 18, 3 * 16 + 13);
        return m.ToBody(header);
    }
}
