using System.Numerics;

namespace LBAAssembler.Terrain;

// The hovercraft hire's beach on the White Leaf Desert (cube (10, 8)) set out as LBA1's credits have it by the hydroglider (the user,
// 2026-11-24: "Let's show off the hovercraft hire, but let's improve it. I think I gave you a picture with some deck chairs and umbrellas
// taken from the end scene of LBA1, let's model those in please" -- the picture, E:\dump\TEMP\nov21\lba1_hovercraft_beach_user.png:
// parasols in blue and yellow panels, red deck chairs on wooden frames, a green towel on the sand). Three bodies of the build's own, each
// placed as the island's decors are, in place of the Desert beach's straw umbrellas and stools that stood there before (PlaceDecors):
//   - the parasol: a pole, a domed canopy of eight panels, blue and yellow in turn, its hem scalloped between the ribs, the ribs under
//     it, a collar and a finial -- the canopy's points shared all round, so it is lit smoothly as the cloth it is, its underside darker;
//   - the deck chair: its wooden frame of rounded rails and bars, a red canvas sling sagging between the top bar and the front, lit
//     smoothly down its curve, both its sides drawn;
//   - the towel: green with a white band near each end, rippled a little, its points shared.
// The parasol's box only round its pole (Twinsen walks under its canopy); the chair's and the towel's round them.
internal static class BeachHire
{
    // (the Desert's palette: a ramp's low entry, the light runs it up -- blue, golden yellow, red, wood, steel grey, teal green, white)
    private static readonly Finish Blue = new(12 * 16 + 5, 1), Yellow = new(6 * 16 + 7, 1), BlueUnder = new(12 * 16 + 3, 0.7f), YellowUnder = new(6 * 16 + 4, 0.7f);
    private static readonly Finish Canvas = new(4 * 16 + 5, 1), CanvasUnder = new(4 * 16 + 3, 0.8f), Wood = new(1 * 16 + 8, 1), Steel = new(3 * 16 + 7, 1);
    private static readonly Finish Towel = new(9 * 16 + 5, 0.9f), TowelBand = new(2 * 16 + 12, 0.9f);

    // Where they go, in the cube's own units: each parasol's foot, each chair's and towel's middle with the way it faces (beta, 0 its
    // front to +x)
    public sealed record Spec(int CubeX, int CubeZ, (int X, int Z)[] Parasols, (int X, int Z, int Beta)[] Chairs, (int X, int Z, int Beta)[] Towels);

    public static void Apply(IslandFile island, Spec spec, RaceTrackOptions o, RaceTrackReport report)
    {
        if (island.CubeAt(spec.CubeX, spec.CubeZ) is not { } cube || o.NewBodyBase < 0) { report.Notes.Add("the hovercraft hire's beach left as it was: no cube or no new bodies' base"); return; }
        int Add(byte[] body) { report.NewBodies.Add(body); return o.NewBodyBase + report.NewBodies.Count - 1; }
        int parasol = Add(Parasol()), chair = Add(Chair()), towel = Add(TowelBody());
        int Ground(int x, int z) => (int)Math.Round(IslandOps.Altitude(island, spec.CubeX * (double)IslandFile.CubeSize + x, spec.CubeZ * (double)IslandFile.CubeSize + z) ?? 0);
        foreach (var (x, z) in spec.Parasols)
            cube.Decors.Add(Boxed(parasol, x, Ground(x, z), z, 0, 90, 90, 2250));
        foreach (var (x, z, beta) in spec.Chairs)
        {
            // (the chair's box: 1,200 along its length, 520 across, turned with it)
            var along = (beta & 2047) == 0;
            cube.Decors.Add(Boxed(chair, x, Ground(x, z), z, beta, along ? 620 : 270, along ? 270 : 620, 900));
        }
        foreach (var (x, z, beta) in spec.Towels)
        {
            var along = (beta & 2047) == 0;
            cube.Decors.Add(Boxed(towel, x, Ground(x, z), z, beta, along ? 660 : 340, along ? 340 : 660, 30));
        }
        report.Notes.Add($"the hovercraft hire's beach as LBA1's credits: {spec.Parasols.Length} parasols in blue and yellow, {spec.Chairs.Length} red deck chairs, {spec.Towels.Length} towel(s), in cube ({spec.CubeX},{spec.CubeZ})");
    }

    private static IslandDecor Boxed(int body, int x, int y, int z, int beta, int halfX, int halfZ, int height)
    {
        var d = IslandDecors.Blank(body, x, y, z, beta);
        d.XMin = x - halfX; d.XMax = x + halfX; d.ZMin = z - halfZ; d.ZMax = z + halfZ; d.YMin = y; d.YMax = y + height;
        return d;
    }

    private static byte[] Write(CarMesh m)
    {
        var body = m.ToBody(new byte[96]);
        body.Static = true;
        return body.Write();
    }

    // ---- the parasol: its foot at the origin
    private static byte[] Parasol()
    {
        var m = new CarMesh("a beach parasol", new[] { -1 });
        HotelKit.Rod(m, new Vector3(0, -40, 0), new Vector3(0, 2130, 0), 26, Steel, 8);
        // (a collar where the ribs meet the pole, a finial on top)
        HotelKit.Turned(m, new Vector3(0, 1930, 0), new[] { (0f, 0f), (44f, 0f), (52f, 40f), (52f, 90f), (40f, 120f), (0f, 120f) }, 10, Steel);
        HotelKit.Orb(m, new Vector3(0, 2205, 0), 42, new Finish(3 * 16 + 11, 1), 8, 4);
        // the canopy: rings out from the top, sixteen points round (each panel two segments), the hem dipping at each panel's middle
        const int N = 16;
        var profile = new (float R, float Y)[] { (0, 2185), (170, 2160), (420, 2085), (680, 1975), (900, 1850), (1010, 1790) };
        int[][] Rings(float drop)
        {
            var rings = new int[profile.Length][];
            for (var k = 0; k < profile.Length; k++)
            {
                var (r, y) = profile[k];
                if (r == 0) { rings[k] = new[] { m.P(0, new Vector3(0, y - drop, 0)) }; continue; }
                rings[k] = Enumerable.Range(0, N).Select(j =>
                {
                    var a = j * MathF.Tau / N;
                    // (the hem: up at each rib, j even, down between them)
                    var hem = k == profile.Length - 1 && j % 2 == 1 ? -55f : 0f;
                    return m.P(0, new Vector3(MathF.Cos(a) * r, y - drop + hem, MathF.Sin(a) * r));
                }).ToArray();
            }
            return rings;
        }
        var top = Rings(0);
        var under = Rings(22);
        for (var k = 0; k + 1 < profile.Length; k++)
            for (var j = 0; j < N; j++)
            {
                var yellow = (j / 2) % 2 == 1;
                int[] Hs(int[][] rr) => rr[k].Length == 1 ? new[] { rr[k][0], rr[k + 1][j], rr[k + 1][(j + 1) % N] } : new[] { rr[k][j], rr[k][(j + 1) % N], rr[k + 1][(j + 1) % N], rr[k + 1][j] };
                var hs = Hs(top);
                var mid = hs.Aggregate(Vector3.Zero, (s, h) => s + m.At(h)) / hs.Length;
                // (outward and up from the canopy; the underside outward and down)
                var radial = Vector3.Normalize(new Vector3(mid.X, 0, mid.Z) + new Vector3(0.0001f, 0, 0));
                var f = yellow ? Yellow : Blue;
                m.Light = f.Light;
                m.In(hs, f.Colour, mid + (radial * 0.6f + Vector3.UnitY) * 1000);
                var us = Hs(under);
                var umid = us.Aggregate(Vector3.Zero, (s, h) => s + m.At(h)) / us.Length;
                var fu = yellow ? YellowUnder : BlueUnder;
                m.In(us, fu.Colour, umid + (radial * 0.3f - Vector3.UnitY) * 1000);
            }
        // (the ribs under the canopy: from the collar out to the hem at each panel's edge, and the hem's edge between the two skins)
        for (var j = 0; j < N; j += 2)
        {
            var a = j * MathF.Tau / N;
            var tip = new Vector3(MathF.Cos(a) * 990, 1772, MathF.Sin(a) * 990);
            HotelKit.Rod(m, new Vector3(0, 2040, 0), tip, 9, Steel, 4);
        }
        for (var j = 0; j < N; j++)
        {
            var last = profile.Length - 1;
            var hs = new[] { top[last][j], top[last][(j + 1) % N], under[last][(j + 1) % N], under[last][j] };
            var mid = hs.Aggregate(Vector3.Zero, (s, h) => s + m.At(h)) / 4;
            var f = (j / 2) % 2 == 1 ? Yellow : Blue;
            m.In(hs, f.Colour, mid + Vector3.Normalize(new Vector3(mid.X, 0, mid.Z)) * 1000);
        }
        return Write(m);
    }

    // ---- the deck chair: its middle on the ground at the origin, facing +x (the sitter looks along +x), its width along z
    private static byte[] Chair()
    {
        var m = new CarMesh("a deck chair", new[] { -1 });
        const float W = 250, R = 17;
        // the frame, each side: the back rail from its foot up to the top of the back, the front leg up to the seat's front bar, a rail
        // from the back rail down to the front leg's foot (the stretcher the back rests on)
        Vector3 S(float x, float y, float z) => new(x, y, z);
        foreach (var z in new[] { -W, W })
        {
            HotelKit.Rod(m, S(-260, 0, z), S(-560, 880, z), R, Wood);
            HotelKit.Rod(m, S(560, 0, z), S(330, 440, z), R, Wood);
            HotelKit.Rod(m, S(-330, 210, z), S(470, 170, z), R * 0.85f, Wood);
        }
        // the cross bars: at the top of the back, the seat's front, the feet behind and before
        HotelKit.Rod(m, S(-560, 880, -W - 20), S(-560, 880, W + 20), R, Wood);
        HotelKit.Rod(m, S(330, 440, -W - 20), S(330, 440, W + 20), R, Wood);
        HotelKit.Rod(m, S(-275, 40, -W), S(-275, 40, W), R * 0.8f, Wood);
        HotelKit.Rod(m, S(545, 40, -W), S(545, 40, W), R * 0.8f, Wood);
        // the sling: from the top bar to the front bar, sagging most a little below its middle; ten rows along, four across, shared
        const int Along = 12, Across = 4;
        var top = S(-560, 880, 0); var front = S(330, 440, 0);
        var chord = front - top;
        var down = Vector3.Normalize(new Vector3(chord.Y, -chord.X, 0)) * (chord.X > 0 ? 1 : -1);
        if (down.Y > 0) down = -down;
        int[,] Grid(float lift)
        {
            var g = new int[Along + 1, Across + 1];
            for (var i = 0; i <= Along; i++)
            {
                var t = (float)i / Along;
                var sag = MathF.Sin(MathF.PI * MathF.Pow(t, 0.85f)) * 300;
                var p = top + chord * t + down * sag + Vector3.UnitY * lift;
                for (var j = 0; j <= Across; j++)
                {
                    var z = -W + 2 * W * j / Across;
                    // (the canvas a touch lower in its middle across, slack between the rails)
                    var slack = MathF.Sin(MathF.PI * j / Across) * 22;
                    g[i, j] = m.P(0, new Vector3(p.X, p.Y - slack, z * 0.96f));
                }
            }
            return g;
        }
        var upper = Grid(0);
        var lower = Grid(-14);
        for (var i = 0; i < Along; i++)
            for (var j = 0; j < Across; j++)
            {
                var hs = new[] { upper[i, j], upper[i + 1, j], upper[i + 1, j + 1], upper[i, j + 1] };
                var mid = hs.Aggregate(Vector3.Zero, (s, h) => s + m.At(h)) / 4;
                var n = Vector3.Normalize(Vector3.Cross(m.At(hs[1]) - m.At(hs[0]), m.At(hs[3]) - m.At(hs[0])));
                if (n.Y < 0) n = -n;
                m.Light = Canvas.Light;
                m.In(hs, Canvas.Colour, mid + n * 1000);
                var ls = new[] { lower[i, j], lower[i + 1, j], lower[i + 1, j + 1], lower[i, j + 1] };
                var lmid = ls.Aggregate(Vector3.Zero, (s, h) => s + m.At(h)) / 4;
                m.Light = CanvasUnder.Light;
                m.In(ls, CanvasUnder.Colour, lmid - n * 1000);
            }
        return Write(m);
    }

    // ---- the towel: its middle on the ground at the origin, its length along x
    private static byte[] TowelBody()
    {
        var m = new CarMesh("a beach towel", new[] { -1 });
        const int Nu = 14, Nv = 6;
        const float L = 640, Wd = 330;
        var g = new int[Nu + 1, Nv + 1];
        m.Light = Towel.Light;
        for (var i = 0; i <= Nu; i++)
            for (var j = 0; j <= Nv; j++)
            {
                float u = (float)i / Nu, v = (float)j / Nv;
                var ripple = 6 + 7 * MathF.Sin(u * 9.4f + 0.6f) * MathF.Sin(v * 3.1f + 0.3f);
                g[i, j] = m.P(0, new Vector3(-L + 2 * L * u, ripple, -Wd + 2 * Wd * v));
            }
        for (var i = 0; i < Nu; i++)
            for (var j = 0; j < Nv; j++)
            {
                var hs = new[] { g[i, j], g[i + 1, j], g[i + 1, j + 1], g[i, j + 1] };
                var mid = hs.Aggregate(Vector3.Zero, (s, h) => s + m.At(h)) / 4;
                var band = i == 1 || i == Nu - 2;
                m.In(hs, (band ? TowelBand : Towel).Colour, mid + Vector3.UnitY * 1000);
            }
        return Write(m);
    }
}
