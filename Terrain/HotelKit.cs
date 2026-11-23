using System.Numerics;

namespace LBAAssembler.Terrain;

// A finish: a colour (a palette entry near its ramp's start: the game adds the light, up the ramp) and how much of the light its points
// take (CarMesh.Light: 1 runs nine steps or so up the ramp, less keeps a dark finish dark)
internal readonly record struct Finish(int Colour, float Light);

// Modelling with normals, for the Imperial Hotel's furniture (2026-11-23, the user: "The new objects you've added all look flat and need
// more normals adding ... They need to look sleek, uniform, upmarket, and modelled to a high degree"). The engine lights a body's polygon
// from its corners' normals, each the average of the faces that share that corner (Body.VertexNormals): a box of eight shared corners gets
// eight diagonal normals and its six faces come out alike -- flat, its edges lost. So here:
//   - a hard face (Face, HardBox, BevelBox) has corners of its own: it is lit as the plane it is, its edges show; a bevel's slope catches
//     the light between the top and the side;
//   - a round thing (Lathe, Orb, Cushion, Drape) shares its corners with the faces next to it, so it is lit smoothly across -- as the
//     game's own leather armchairs and turned gunmetal posts are.
internal static class HotelKit
{
    private static readonly Vector3 X = Vector3.UnitX, Y = Vector3.UnitY, Z = Vector3.UnitZ;

    // a polygon (3 or 4 corners) of new points of its own, facing `normal`
    public static void Face(CarMesh m, Finish f, Vector3 normal, params Vector3[] corners)
    {
        m.Light = f.Light;
        var hs = corners.Select(c => m.P(0, c)).ToArray();
        var centre = corners.Aggregate(Vector3.Zero, (s, c) => s + c) / corners.Length;
        m.In(hs, f.Colour, centre + normal * 1000);
    }

    // a polygon drawn in its colour as it is (a canvas's dab, a flame, a mirror's streak)
    public static void Unlit(CarMesh m, int colour, Vector3 normal, params Vector3[] corners)
    {
        var hs = corners.Select(c => m.P(0, c)).ToArray();
        var centre = corners.Aggregate(Vector3.Zero, (s, c) => s + c) / corners.Length;
        m.In(hs, colour, centre + normal * 1000, unlit: true);
    }

    // A flat quad (corners a, b, c, d round it) cut into nu x nv tiles that share their points -- one plane, one normal, so they are lit
    // alike -- each tile in its own colour, colour(i, j): marble's mottling
    public static void Tiles(CarMesh m, Finish f, Vector3 normal, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int nu, int nv, Func<int, int, int> colour)
    {
        m.Light = f.Light;
        var g = new int[nu + 1, nv + 1];
        for (var i = 0; i <= nu; i++)
            for (var j = 0; j <= nv; j++)
            {
                float s = (float)i / nu, t = (float)j / nv;
                g[i, j] = m.P(0, Vector3.Lerp(Vector3.Lerp(a, b, s), Vector3.Lerp(d, c, s), t));
            }
        for (var i = 0; i < nu; i++)
            for (var j = 0; j < nv; j++)
            {
                var hs = new[] { g[i, j], g[i + 1, j], g[i + 1, j + 1], g[i, j + 1] };
                var mid = hs.Aggregate(Vector3.Zero, (s, h) => s + m.At(h)) / 4;
                m.In(hs, colour(i, j), mid + normal * 1000);
            }
    }

    // a tile of the dark marble (the game's own wardrobes': its violet greys, now and then a near black), picked by its place
    public static Func<int, int, int> Marble(int seed) => (i, j) =>
    {
        var h = (uint)(i * 73856093) ^ (uint)(j * 19349663) ^ (uint)(seed * 83492791);
        h = (h ^ (h >> 13)) * 1274126177u;
        int[] tones = { 13 * 16 + 0, 13 * 16 + 1, 13 * 16 + 0, 13 * 16 + 2, 13 * 16 + 0, 13 * 16 + 1, 14 * 16 + 0, 13 * 16 + 0 };
        return tones[(h >> 9) & 7];
    };

    // a box, each face its own corners; its top in its own finish if given; no bottom unless asked
    public static void HardBox(CarMesh m, Vector3 c, Vector3 size, Finish f, Finish? top = null, bool bottom = false)
    {
        var h = size / 2;
        Vector3 P(float x, float y, float z) => c + new Vector3(x * h.X, y * h.Y, z * h.Z);
        Face(m, top ?? f, Y, P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1));
        if (bottom) Face(m, f, -Y, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1));
        Face(m, f, X, P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1));
        Face(m, f, -X, P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), P(-1, -1, 1));
        Face(m, f, Z, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1));
        Face(m, f, -Z, P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1));
    }

    // a box with its top edges and its four upright edges chamfered `b`: the top, four sloping bevels, four corner facets, the four sides
    // and the four upright chamfers -- each its own corners, so each catches its own light
    // (marbled: its top and its front (+z) cut into tiles of the dark marble, `marbled` (across, along) of them)
    public static void BevelBox(CarMesh m, Vector3 c, Vector3 size, float b, Finish f, Finish? top = null, (int Top, int Front)? marbled = null, int seed = 0)
    {
        var h = size / 2;
        float x0 = c.X - h.X, x1 = c.X + h.X, y0 = c.Y - h.Y, y1 = c.Y + h.Y, z0 = c.Z - h.Z, z1 = c.Z + h.Z, yb = y1 - b;
        Vector3 V(float x, float y, float z) => new(x, y, z);
        // (tiles about square: so many along the longer side, as many across as fit)
        static (int, int) Grid(int along, float lu, float lv) => lu >= lv ? (along, Math.Max(1, (int)MathF.Round(along * lv / lu))) : (Math.Max(1, (int)MathF.Round(along * lu / lv)), along);
        if (marbled is { Top: > 0 } mt)
        {
            var (nu, nv) = Grid(mt.Top, x1 - x0, z1 - z0);
            Tiles(m, top ?? f, Y, V(x0 + b, y1, z0 + b), V(x1 - b, y1, z0 + b), V(x1 - b, y1, z1 - b), V(x0 + b, y1, z1 - b), nu, nv, Marble(seed));
        }
        else Face(m, top ?? f, Y, V(x0 + b, y1, z0 + b), V(x1 - b, y1, z0 + b), V(x1 - b, y1, z1 - b), V(x0 + b, y1, z1 - b));
        // the bevels round the top
        Face(m, f, new(1, 1, 0), V(x1 - b, y1, z0 + b), V(x1 - b, y1, z1 - b), V(x1, yb, z1 - b), V(x1, yb, z0 + b));
        Face(m, f, new(-1, 1, 0), V(x0 + b, y1, z0 + b), V(x0 + b, y1, z1 - b), V(x0, yb, z1 - b), V(x0, yb, z0 + b));
        Face(m, f, new(0, 1, 1), V(x0 + b, y1, z1 - b), V(x1 - b, y1, z1 - b), V(x1 - b, yb, z1), V(x0 + b, yb, z1));
        Face(m, f, new(0, 1, -1), V(x0 + b, y1, z0 + b), V(x1 - b, y1, z0 + b), V(x1 - b, yb, z0), V(x0 + b, yb, z0));
        // the corners' little facets
        foreach (var (sx, sz) in new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })
        {
            float cx = sx > 0 ? x1 : x0, cz = sz > 0 ? z1 : z0;
            Face(m, f, new(sx, 1, sz), V(cx - sx * b, y1, cz - sz * b), V(cx, yb, cz - sz * b), V(cx - sx * b, yb, cz));
        }
        // the sides, and the upright chamfers between them
        Face(m, f, X, V(x1, y0, z0 + b), V(x1, yb, z0 + b), V(x1, yb, z1 - b), V(x1, y0, z1 - b));
        Face(m, f, -X, V(x0, y0, z0 + b), V(x0, yb, z0 + b), V(x0, yb, z1 - b), V(x0, y0, z1 - b));
        if (marbled is { Front: > 0 } mf)
        {
            var (nu, nv) = Grid(mf.Front, x1 - x0, yb - y0);
            Tiles(m, f, Z, V(x0 + b, y0, z1), V(x1 - b, y0, z1), V(x1 - b, yb, z1), V(x0 + b, yb, z1), nu, nv, Marble(seed + 1));
        }
        else Face(m, f, Z, V(x0 + b, y0, z1), V(x0 + b, yb, z1), V(x1 - b, yb, z1), V(x1 - b, y0, z1));
        Face(m, f, -Z, V(x0 + b, y0, z0), V(x0 + b, yb, z0), V(x1 - b, yb, z0), V(x1 - b, y0, z0));
        foreach (var (sx, sz) in new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })
        {
            float cx = sx > 0 ? x1 : x0, cz = sz > 0 ? z1 : z0;
            Face(m, f, new(sx, 0, sz), V(cx, y0, cz - sz * b), V(cx, yb, cz - sz * b), V(cx - sx * b, yb, cz), V(cx - sx * b, y0, cz));
        }
    }

    // A turned shape: `profile` (radius, height along `axis`) from one end to the other along its outside, round `axis` through
    // `origin` (`u` and `v` the radius' directions, as long as each other for a round thing, not for an oval), `n` sides. Neighbouring
    // rings share their points, so it is lit smoothly round and along; an entry given twice makes a sharp step; a radius of 0 a point (a
    // pole, a tip). Each band in the finish `finish` gives it (by its index in the profile).
    public static void Lathe(CarMesh m, Vector3 origin, Vector3 axis, Vector3 u, Vector3 v, (float R, float Y)[] profile, int n, Func<int, Finish> finish, float phase = 0)
    {
        var rings = new int[profile.Length][];
        for (var k = 0; k < profile.Length; k++)
        {
            m.Light = finish(Math.Min(k, profile.Length - 2)).Light;
            var (r, y) = profile[k];
            var centre = origin + axis * y;
            rings[k] = r == 0 ? new[] { m.P(0, centre) }
                : Enumerable.Range(0, n).Select(j => { var a = (j + phase) * MathF.Tau / n; return m.P(0, centre + (u * MathF.Cos(a) + v * MathF.Sin(a)) * r); }).ToArray();
        }
        for (var k = 0; k + 1 < profile.Length; k++)
        {
            var (r0, y0) = profile[k];
            var (r1, y1) = profile[k + 1];
            if ((r0 == r1 && y0 == y1) || (r0 == 0 && r1 == 0)) continue;
            // (the band's outward normal in the profile's plane: the profile runs along the outside, the solid on the axis' side)
            float nr = y1 - y0, ny = -(r1 - r0);
            var f = finish(k);
            for (var j = 0; j < n; j++)
            {
                var a = (j + 0.5f + phase) * MathF.Tau / n;
                var radial = Vector3.Normalize(u * MathF.Cos(a) + v * MathF.Sin(a));
                var outward = radial * nr + axis * ny;
                int[] hs = r0 == 0 ? new[] { rings[k][0], rings[k + 1][j], rings[k + 1][(j + 1) % n] }
                    : r1 == 0 ? new[] { rings[k][j], rings[k][(j + 1) % n], rings[k + 1][0] }
                    : new[] { rings[k][j], rings[k][(j + 1) % n], rings[k + 1][(j + 1) % n], rings[k + 1][j] };
                var mid = hs.Aggregate(Vector3.Zero, (s, h) => s + m.At(h)) / hs.Length;
                m.In(hs, f.Colour, mid + outward * 1000);
            }
        }
    }

    // the same, upright (round the y axis) at `foot`
    public static void Turned(CarMesh m, Vector3 foot, (float R, float Y)[] profile, int n, Finish f, float phase = 0)
        => Lathe(m, foot, Y, X, Z, profile, n, _ => f, phase);

    // a rod of radius r from a to b, its ends rounded off a little (a rail, a pipe, a log)
    public static void Rod(CarMesh m, Vector3 a, Vector3 b, float r, Finish f, int n = 6)
    {
        var d = b - a;
        var len = d.Length();
        var axis = d / len;
        var u = Vector3.Normalize(Vector3.Cross(axis, MathF.Abs(axis.Y) > 0.9f ? X : Y));
        var v = Vector3.Cross(axis, u);
        Lathe(m, a, axis, u, v, new[] { (0f, 0f), (r * 0.8f, 0f), (r, r * 0.3f), (r, len - r * 0.3f), (r * 0.8f, len), (0f, len) }, n, _ => f);
    }

    // a ball (squashed or stretched upright by `tall`), smooth
    public static void Orb(CarMesh m, Vector3 c, float r, Finish f, int n = 8, int rings = 4, float tall = 1)
    {
        var p = Enumerable.Range(0, rings + 1).Select(k =>
        {
            var t = MathF.PI * k / rings;
            return (k == 0 || k == rings ? 0f : r * MathF.Sin(t), r * tall * (1 - MathF.Cos(t)));
        }).ToArray();
        Lathe(m, c - Y * r * tall, Y, X, Z, p, n, _ => f);
    }

    // A rounded block: a superellipsoid -- `power` 2 an ellipsoid, more a box with round corners and edges -- of half sizes `half`; its
    // points shared, so it is lit as the smooth thing it is: leather upholstery, a pillow, a mattress, porcelain
    public static void Cushion(CarMesh m, Vector3 c, Vector3 half, float power, Finish f, int nu = 12, int nv = 5)
    {
        m.Light = f.Light;
        var e = 2 / power;
        float F(float w) => MathF.Sign(w) * MathF.Pow(MathF.Abs(w), e);
        var bottom = m.P(0, c - Y * half.Y);
        var top = m.P(0, c + Y * half.Y);
        var rings = Enumerable.Range(1, nv - 1).Select(i =>
        {
            var th = -MathF.PI / 2 + MathF.PI * i / nv;
            return Enumerable.Range(0, nu).Select(j =>
            {
                var ph = MathF.Tau * j / nu;
                return m.P(0, c + new Vector3(half.X * F(MathF.Cos(th)) * F(MathF.Cos(ph)), half.Y * F(MathF.Sin(th)), half.Z * F(MathF.Cos(th)) * F(MathF.Sin(ph))));
            }).ToArray();
        }).ToArray();
        for (var j = 0; j < nu; j++)
        {
            m.Out(new[] { rings[0][(j + 1) % nu], rings[0][j], bottom }, f.Colour, c);
            m.Out(new[] { rings[^1][j], rings[^1][(j + 1) % nu], top }, f.Colour, c);
        }
        for (var i = 0; i + 1 < rings.Length; i++)
            for (var j = 0; j < nu; j++)
                m.Out(new[] { rings[i][j], rings[i][(j + 1) % nu], rings[i + 1][(j + 1) % nu], rings[i + 1][j] }, f.Colour, c);
    }

    // A hanging cloth in pleats: its top along `from` -> `to`, hanging `drop` down, `pleats` folds `depth` deep towards `front`; each row
    // (top to bottom, as many as `gather` has) only that fraction of the top's width, measured from `from` (a curtain tied back to its
    // post); `scallop` lifts the hem between the folds (a pelmet's scallops). Its points shared: the folds lit softly, as cloth is.
    public static void Drape(CarMesh m, Vector3 from, Vector3 to, float drop, int pleats, float depth, Vector3 front, Finish f, float[]? gather = null, float scallop = 0)
    {
        m.Light = f.Light;
        gather ??= new[] { 1f, 1f };
        var cols = 2 * pleats + 1;
        var rows = gather.Select((g, r) => Enumerable.Range(0, cols).Select(j =>
        {
            var t = (float)j / (cols - 1);
            var y = -drop * r / (gather.Length - 1);
            if (r == gather.Length - 1 && scallop > 0) y += scallop * MathF.Sin(MathF.PI * (t * pleats - MathF.Floor(t * pleats)));
            return m.P(0, from + (to - from) * t * g + new Vector3(0, y, 0) + front * (j % 2 == 1 ? depth : 0));
        }).ToArray()).ToArray();
        for (var r = 0; r + 1 < rows.Length; r++)
            for (var j = 0; j + 1 < cols; j++)
            {
                var hs = new[] { rows[r][j], rows[r][j + 1], rows[r + 1][j + 1], rows[r + 1][j] };
                var mid = hs.Aggregate(Vector3.Zero, (s, h) => s + m.At(h)) / 4;
                m.In(hs, f.Colour, mid + front * 1000);
            }
    }

    // A turned post in the game's own hotel style (its wardrobes', beds' and chairs' gunmetal posts): a disc foot, a slim shaft with
    // `beads` beads along it, a collar, a bulb and a finial at its head
    public static void Post(CarMesh m, Vector3 foot, float height, float r, int beads, Finish f, int n = 6)
    {
        var p = new List<(float, float)> { (0, 0), (r * 2.6f, 0), (r * 2.5f, r * 0.6f), (r * 1.4f, r * 1.3f), (r, r * 2.2f) };
        for (var k = 1; k <= beads; k++)
        {
            var yb = r * 3 + (height - r * 8) * k / (beads + 1);
            p.Add((r, yb - r * 1.2f)); p.Add((r * 1.75f, yb)); p.Add((r, yb + r * 1.2f));
        }
        p.Add((r, height - r * 3.4f)); p.Add((r * 1.9f, height - r * 1.9f)); p.Add((r * 1.6f, height - r * 0.7f));
        p.Add((r * 0.7f, height)); p.Add((r * 0.55f, height + r * 0.8f)); p.Add((0, height + r * 1.8f));
        Turned(m, foot, p.ToArray(), n, f);
    }
}
