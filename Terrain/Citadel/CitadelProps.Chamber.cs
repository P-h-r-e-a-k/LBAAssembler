using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The secret chamber's furnishings (the user, 2026-11-08/09: "the hole in the wall has a sendell seal filling it ... let's restore this";
// "some hanging tapestries around the place"; "a rack with different paddles and whips hanging on it"; "soften it slightly with some
// pictures hanging on the wall"; "one of the chairs from the bedroom down there for Zoe to sit on"). Props of the static prop entity as
// the others (CitadelProps); the wall pieces built flat in the x-y plane, their faces to +z (an actor's beta turns them to face out of
// its wall), their backs against it.
internal static partial class CitadelProps
{
    public const int Seal = 11, TapestryRed = 12, TapestryBlue = 13, Picture = 14, Rack = 15, Chair = 16;
    // (the palette: the gold ramp, a deep blue, a teal for the chair's cushion, the dark flat)
    private const int Gold = 106, Blue = 197, Teal = 150, Dark = 49, Sky = 200, Leather = 50;

    // A flat quad in the x-y plane at depth z, its face to +z (or both sides)
    private static void Panel(CarMesh m, float x0, float y0, float x1, float y1, float z, int colour, bool both = false, bool flat = false)
    {
        var q = new[] { m.P(0, new Vector3(x0, y0, z)), m.P(0, new Vector3(x1, y0, z)), m.P(0, new Vector3(x1, y1, z)), m.P(0, new Vector3(x0, y1, z)) };
        if (both) { m.Both(q, colour, flat ? 0 : -1); return; }
        m.Out(q, colour, new Vector3((x0 + x1) / 2, (y0 + y1) / 2, z - 10), flat);
    }

    // The Sendell seal over the hole in the hall's wall: a round bronze medallion, its raised rim, its inner disc a shade darker, and
    // Sendell's sign on it in dark lines -- an arc over a ball, as LBA1's seal has it
    public static Body BuildSeal(byte[] header)
    {
        var m = new CarMesh("the Sendell seal", new[] { -1 });
        m.Light = 0.7f;
        const int N = 20;
        const float R = 540, Inner = 410, Rim = 50;
        var back = m.Loop(0, new Vector3(0, 0, 0), new Vector3(R, 0, 0), new Vector3(0, R, 0), N);
        var front = m.Loop(0, new Vector3(0, 0, Rim), new Vector3(R, 0, 0), new Vector3(0, R, 0), N);
        var inner = m.Loop(0, new Vector3(0, 0, Rim), new Vector3(Inner, 0, 0), new Vector3(0, Inner, 0), N);
        var face = m.Loop(0, new Vector3(0, 0, Rim - 14), new Vector3(Inner, 0, 0), new Vector3(0, Inner, 0), N);
        var mid = m.P(0, new Vector3(0, 0, Rim - 14));
        for (var i = 0; i < N; i++)
        {
            var n = (i + 1) % N;
            m.Out(new[] { back[i], back[n], front[n], front[i] }, Rust, new Vector3(0, 0, Rim / 2));
            m.Out(new[] { front[i], front[n], inner[n], inner[i] }, Wood, new Vector3(0, 0, 0));
            m.Out(new[] { inner[i], inner[n], face[n], face[i] }, Rust, new Vector3(0, 0, Rim + 50));
            m.Out(new[] { face[i], face[n], mid }, Rust, new Vector3(0, 0, 0));
        }
        // Sendell's sign: an arc (a thick crescent) over a ball, in dark flat lines just proud of the face
        const float Z = Rim - 10;
        const int Arc = 9;
        for (var k = 0; k < Arc; k++)
        {
            float a0 = MathF.PI * (0.15f + 0.7f * k / Arc), a1 = MathF.PI * (0.15f + 0.7f * (k + 1) / Arc);
            Vector3 P(float a, float r) => new(r * MathF.Cos(a), 60 + r * MathF.Sin(a), Z);
            var q = new[] { m.P(0, P(a0, 300)), m.P(0, P(a1, 300)), m.P(0, P(a1, 225)), m.P(0, P(a0, 225)) };
            m.Out(q, Dark, new Vector3(0, 0, 0), unlit: true);
        }
        var ball = m.Loop(0, new Vector3(0, -60, Z), new Vector3(105, 0, 0), new Vector3(0, 105, 0), 10);
        m.Cap(ball, m.P(0, new Vector3(0, -60, Z + 4)), Dark, new Vector3(0, -60, 0), unlit: true);
        return m.ToBody(header);
    }

    // A hanging tapestry: a cloth in its colour with a gold border and a gold lozenge in its middle, on a rod with gold ends; its top at 0
    private static Body Tapestry(byte[] header, int cloth, string name)
    {
        var m = new CarMesh(name, new[] { -1 });
        m.Light = 0.7f;
        const float W = 340, H = 1100, B = 40;
        Panel(m, -W, -H, W, 0, 0, cloth, both: true);
        Panel(m, -W, -H, W, -H + B, 4, Gold); Panel(m, -W, -B, W, 0, 4, Gold);
        Panel(m, -W, -H, -W + B, 0, 4, Gold); Panel(m, W - B, -H, W, 0, 4, Gold);
        // (the lozenge)
        var c = new Vector3(0, -H / 2, 6);
        var q = new[] { m.P(0, c + new Vector3(0, 220, 0)), m.P(0, c + new Vector3(160, 0, 0)), m.P(0, c + new Vector3(0, -220, 0)), m.P(0, c + new Vector3(-160, 0, 0)) };
        m.Out(q, Gold, c - new Vector3(0, 0, 10));
        m.Box(0, new Vector3(0, 10, 10), new Vector3(2 * W + 120, 24, 24), Wood);
        foreach (var s in new[] { -1, 1 }) m.Box(0, new Vector3(s * (W + 60), 10, 10), new Vector3(36, 36, 36), Gold);
        return m.ToBody(header);
    }
    public static Body BuildTapestryRed(byte[] header) => Tapestry(header, Red, "a red tapestry");
    public static Body BuildTapestryBlue(byte[] header) => Tapestry(header, Blue, "a blue tapestry");

    // A framed picture: a gold frame round a little landscape -- sky, green hills, a sun; its middle at 0
    public static Body BuildPicture(byte[] header)
    {
        var m = new CarMesh("a picture", new[] { -1 });
        m.Light = 0.7f;
        const float W = 300, H = 220, F = 36;
        m.Box(0, new Vector3(0, H - F / 2, 10), new Vector3(2 * W, F, 20), Gold);
        m.Box(0, new Vector3(0, -H + F / 2, 10), new Vector3(2 * W, F, 20), Gold);
        m.Box(0, new Vector3(-W + F / 2, 0, 10), new Vector3(F, 2 * H, 20), Gold);
        m.Box(0, new Vector3(W - F / 2, 0, 10), new Vector3(F, 2 * H, 20), Gold);
        Panel(m, -W + F, -10, W - F, H - F, 6, Sky);
        Panel(m, -W + F, -H + F, W - F, -10, 6, Green);
        var sun = m.Loop(0, new Vector3(110, 100, 8), new Vector3(40, 0, 0), new Vector3(0, 40, 0), 8);
        m.Cap(sun, m.P(0, new Vector3(110, 100, 10)), FlameCore, new Vector3(110, 100, 0), unlit: true);
        return m.ToBody(header);
    }

    // The rack on the wall: a wooden board with pegs, and hanging from them two paddles (a wooden one, a black leather one), a whip
    // coiled in a loop and a riding crop; its middle at 0
    public static Body BuildRack(byte[] header)
    {
        var m = new CarMesh("a rack of paddles and whips", new[] { -1 });
        m.Light = 0.7f;
        const float W = 520;
        m.Box(0, new Vector3(0, 300, 20), new Vector3(2 * W, 90, 40), Wood);
        var pegs = new[] { -390f, -130f, 130f, 390f };
        foreach (var x in pegs) m.Box(0, new Vector3(x, 300, 70), new Vector3(24, 24, 70), Steel);
        // a wooden paddle: its handle up to the peg, its broad blade below
        void Paddle(float x, int colour)
        {
            m.Box(0, new Vector3(x, 180, 60), new Vector3(40, 220, 18), colour);
            m.Box(0, new Vector3(x, -60, 60), new Vector3(150, 280, 18), colour);
        }
        Paddle(pegs[0], Wood);
        Paddle(pegs[1], Leather);
        // a whip: its handle, then its lash hanging in a long loop of short black lengths
        m.Box(0, new Vector3(pegs[2], 170, 60), new Vector3(36, 240, 36), Dark);
        var prev = new Vector3(pegs[2], 50, 60);
        for (var k = 1; k <= 10; k++)
        {
            var a = k / 10f * MathF.PI;
            var next = new Vector3(pegs[2] + 90 * MathF.Sin(a), 50 - 420 * MathF.Sin(a / 2), 60);
            var mid = (prev + next) / 2;
            var len = Vector3.Distance(prev, next);
            m.Box(0, mid, new Vector3(18, len + 6, 18), Dark);
            prev = next;
        }
        // a riding crop: a thin rod and its leather flap
        m.Box(0, new Vector3(pegs[3], 40, 60), new Vector3(16, 520, 16), Dark);
        m.Box(0, new Vector3(pegs[3], -240, 60), new Vector3(60, 70, 10), Leather);
        return m.ToBody(header);
    }

    // The bedroom's chair (the user's picture: a wooden ladder-back chair with a teal cushion), its seat at CitadelSpanking.StoolTop --
    // Zoe sits on it; its back to -z
    public static Body BuildChair(byte[] header)
    {
        var m = new CarMesh("a chair", new[] { -1 });
        m.Light = 0.7f;
        const float Top = CitadelSpanking.StoolTop, Half = 170, Seat = 50, Leg = 24, Back = 820;
        m.Box(0, new Vector3(0, Top - Seat - 10, 0), new Vector3(2 * Half, 20, 2 * Half), Wood);
        m.Box(0, new Vector3(0, Top - Seat / 2, 10), new Vector3(2 * Half - 30, Seat - 20, 2 * Half - 40), Teal, Teal);
        foreach (var (x, z) in new[] { (1, 1), (-1, 1) })
            m.Box(0, new Vector3(x * (Half - Leg), (Top - Seat) / 2, z * (Half - Leg)), new Vector3(2 * Leg, Top - Seat, 2 * Leg), Wood);
        // the back legs run on up as the back's uprights, three slats between them
        foreach (var x in new[] { 1, -1 })
            m.Box(0, new Vector3(x * (Half - Leg), Back / 2, -(Half - Leg)), new Vector3(2 * Leg, Back, 2 * Leg), Wood);
        foreach (var y in new[] { Top + 180, Top + 340, Top + 500 })
            m.Box(0, new Vector3(0, y, -(Half - Leg)), new Vector3(2 * (Half - Leg), 50, 22), Wood);
        return m.ToBody(header);
    }
}
