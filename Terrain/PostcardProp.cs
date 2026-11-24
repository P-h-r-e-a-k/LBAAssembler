using System.Numerics;
using LbaBodyStudio;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Little Big Postcards' card as it lies to be found (RaceTrackStory.Eggs.Postcards): a postcard on a little wooden stake, upright,
// its picture to the road -- a sky over hills, the sun, a stamp in the corner, a white border -- its back a writing side. The user,
// 2026-11-24: "I think there are wider post card issues I've noticed some half buried or barely visible. Let's check every postcard in the
// game": the game's sheet of paper (SPRITES.HQR 238), flat on the ground, was a white sliver where it showed at all. On an entity of its
// own (CitadelProps.InstallOwn), its body 0. Along x (its face +z), its stake's foot on y = 0.
internal static class PostcardProp
{
    public const int Body = 0;
    private const string What = "the Little Big Postcards";
    // the card: how wide, how tall, how thick, its foot over the ground (the stake's height under it)
    public const float Wide = 560, Tall = 380, Thick = 18, Foot = 380;
    // (its box: the stake and the card -- walked into, not through; picked up from further than that, RaceTrackStory.CardReachUnits)
    private static readonly short[] Box = { -290, 0, -60, 290, (short)(Foot + Tall + 20), 60 };

    // the palette's colours (ramp starts: a lit polygon climbs its ramp with the light)
    private const int Paper = 3 * 16 + 8, Sky = 12 * 16 + 4, Hill = 8 * 16 + 2, HillFar = 8 * 16 + 5, Sun = 6 * 16 + 6, Stamp = 4 * 16 + 2, Wood = 20, Ink = 3 * 16;

    public static int Entity(string gameDirectory) => Citadel.CitadelProps.InstallOwn(gameDirectory, What, Own).Entity;
    public static string Install(string gameDirectory) => Citadel.CitadelProps.InstallOwn(gameDirectory, What, Own).Log;
    private static IEnumerable<(int, string, Func<byte[], Body>, short[]?)> Own =>
        new (int, string, Func<byte[], Body>, short[]?)[] { (Body, "a postcard on its stake", Build, Box) };

    // A card's actor at a place, turned `beta`, on the postcards' entity
    public static SceneActorModel Actor(string gameDirectory, int x, int y, int z, int beta)
    {
        var a = Citadel.CitadelProps.Actor(Body, x, y, z, beta);
        if (Entity(gameDirectory) is var e && e >= 0) a.Entity = e;
        a.Flags &= ~0x1000u;     // (a shadow under it: it stands on the ground)
        return a;
    }

    private static Body Build(byte[] header)
    {
        var m = new CarMesh("a postcard on its stake", new[] { -1 }) { Rounded = true, Limit = 1100 };
        m.Light = 1f;
        float x0 = -Wide / 2, x1 = Wide / 2, y0 = Foot, y1 = Foot + Tall, zf = Thick / 2, zb = -Thick / 2;
        // the stake, a little pointed at its foot, the card's clip at its top
        m.Strut(0, new Vector3(0, 30, -Thick - 16), new Vector3(0, Foot + Tall * 0.55f, -Thick - 16), 34, Wood);
        m.Strut(0, new Vector3(0, 0, -Thick - 16), new Vector3(0, 30, -Thick - 16), 18, Wood);
        m.Box(0, new Vector3(0, Foot + Tall * 0.45f, -Thick / 2 - 12), new Vector3(70, 60, 26), Ink);
        // the card: its edge all round, its back's writing side
        m.Box(0, new Vector3(0, (y0 + y1) / 2, 0), new Vector3(Wide, Tall, Thick), Paper);
        Vector3 F(float x, float y, float dz = 2) => new(x, y, zf + dz);
        Vector3 B(float x, float y) => new(x, y, zb - 2);
        // the face: the border's inside, the sky, the far hills, the near hills, the sun, the stamp
        const float Border = 30;
        float ix0 = x0 + Border, ix1 = x1 - Border, iy0 = y0 + Border, iy1 = y1 - Border;
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int colour) => m.Out(new[] { m.P(0, a), m.P(0, b), m.P(0, c), m.P(0, d) }, colour, new Vector3((a.X + c.X) / 2, (a.Y + c.Y) / 2, 0));
        float horizon = iy0 + (iy1 - iy0) * 0.42f;
        Quad(F(ix0, horizon), F(ix1, horizon), F(ix1, iy1), F(ix0, iy1), Sky);
        // (the hills: a ridge of points across, each band a fan to the bottom edge)
        int[] Ridge(float baseY, float amp, float phase, float dz)
        {
            const int N = 8;
            var top = new int[N + 1];
            for (var k = 0; k <= N; k++)
            {
                var x = ix0 + (ix1 - ix0) * k / N;
                top[k] = m.P(0, F(x, baseY + amp * MathF.Sin(phase + k * 0.9f), dz));
            }
            return top;
        }
        void Hills(int[] top, float bottom, int colour, float dz)
        {
            for (var k = 0; k + 1 < top.Length; k++)
            {
                var a = m.At(top[k]); var b = m.At(top[k + 1]);
                var hs = new[] { m.P(0, new Vector3(a.X, bottom, zf + dz)), m.P(0, new Vector3(b.X, bottom, zf + dz)), top[k + 1], top[k] };
                m.Out(hs, colour, new Vector3((a.X + b.X) / 2, bottom, 0));
            }
        }
        Hills(Ridge(horizon + 40, 30, 0.6f, 3), iy0, HillFar, 3);
        Hills(Ridge(horizon - 30, 24, 2.1f, 4), iy0, Hill, 4);
        // the sun: a disc of triangles in the sky
        {
            var centre = F(ix0 + (ix1 - ix0) * 0.28f, iy1 - 70, 3);
            var c = m.P(0, centre);
            const int N = 10;
            var ring = Enumerable.Range(0, N).Select(k => m.P(0, centre + new Vector3(42 * MathF.Cos(k * MathF.Tau / N), 42 * MathF.Sin(k * MathF.Tau / N), 0))).ToArray();
            for (var k = 0; k < N; k++) m.Out(new[] { ring[k], ring[(k + 1) % N], c }, Sun, centre - new Vector3(0, 0, 10));
        }
        // the stamp: white with a red middle, in the top right corner, over the border
        Quad(F(x1 - 120, y1 - 112, 5), F(x1 - 22, y1 - 112, 5), F(x1 - 22, y1 - 18, 5), F(x1 - 120, y1 - 18, 5), Paper + 4);
        Quad(F(x1 - 108, y1 - 100, 6), F(x1 - 34, y1 - 100, 6), F(x1 - 34, y1 - 30, 6), F(x1 - 108, y1 - 30, 6), Stamp);
        // the back: the address's lines, the stamp's square, the line down the middle
        void Line(float ax, float ay, float bx, float by) => m.Out(new[] { m.P(0, B(ax, ay)), m.P(0, B(bx, ay)), m.P(0, B(bx, ay + 6)), m.P(0, B(ax, ay + 6)) }, Ink, new Vector3((ax + bx) / 2, ay, 0));
        for (var k = 0; k < 4; k++) Line(20, y0 + 70 + k * 50, x1 - 40, 0);
        Line(-6, y0 + 40, 6, 0);
        m.Out(new[] { m.P(0, B(-3, y0 + 30)), m.P(0, B(3, y0 + 30)), m.P(0, B(3, y1 - 30)), m.P(0, B(-3, y1 - 30)) }, Ink, new Vector3(0, (y0 + y1) / 2, 0));
        m.Out(new[] { m.P(0, B(x1 - 110, y1 - 100)), m.P(0, B(x1 - 30, y1 - 100)), m.P(0, B(x1 - 30, y1 - 24)), m.P(0, B(x1 - 110, y1 - 24)) }, Ink + 3, new Vector3(x1 - 70, y1 - 60, 0));
        return m.ToBody(header);
    }
}
