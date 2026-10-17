using System.Numerics;

namespace LBAAssembler.Terrain.Temple;

// The flat things, and the stairs.
internal static partial class TempleModels
{
    // A grille, a spiked rail, an iron fan: bars in one upright plane along the block's long way (x, or z). The picture places the plane:
    // a drawn pixel is the point of the plane at t (across the block, 1/32 of a cell apart) that LBA1's view sees there, and the plane is
    // the t whose points of the drawn pixels all lie inside the block (of those that do, the one standing lowest: a grille's bars stand on
    // its foot). Each cell's square of the plane is a see-through quad, LBA1's picture unskewed onto it, seen from both sides.
    private static void MakePanel(Model m)
    {
        var alongX = m.Dx >= m.Dz;
        int length = alongX ? m.Dx : m.Dz, across = alongX ? m.Dz : m.Dx;
        Vector3 At(double a, double y, double t) => alongX ? new((float)a, (float)y, (float)t) : new((float)t, (float)y, (float)a);
        var p = m.Picture;
        var drawn = new List<(double X, double Y)>();
        for (var y = 0; y < p.Height; y++)
            for (var x = 0; x < p.Width; x++)
                if (p.At(x, y) >= 0) drawn.Add((x + 0.5, y + 0.5));
        // (a pixel back onto the plane at t: its place along it and its height)
        (double A, double Y) Back((double X, double Y) s, double t)
        {
            var d = (s.X - p.OriginX - 24) / 24;
            double x = alongX ? d + t : t, z = alongX ? t : t - d;
            return (alongX ? x : z, (p.OriginY + 15 + 12 * (x + z) - s.Y) / 15);
        }
        var best = (T: 0.0, Inside: -1, Foot: double.MaxValue);
        for (var k = 0; k <= 32 * across; k++)
        {
            var t = k / 32.0;
            var points = drawn.Select(d => Back(d, t)).ToList();
            var inside = points.Count(q => q.A >= -0.02 && q.A <= length + 0.02 && q.Y >= -0.05 && q.Y <= m.Dy + 0.05);
            var foot = Math.Abs(points.Min(q => q.Y));
            if (inside > best.Inside || inside == best.Inside && foot < best.Foot) best = (t, inside, foot);
        }
        var plane = best.T;
        var normal = alongX ? Vector3.UnitZ : Vector3.UnitX;
        var cells = 0;
        for (var a = 0; a < length; a++)
            for (var j = 0; j < m.Dy; j++)
            {
                int a0 = a, j0 = j;
                var tile = m.AddTile(32, 16, (u, v) => At(a0 + u, j0 + 1 - v, plane), m.Paint, solid: false);
                if (m.Tiles[tile].Pixels.All(c => c < 0)) { m.Tiles.RemoveAt(tile); continue; }
                (Vector3, double, double)[] quad = { (At(a, j + 1, plane), 0, 0), (At(a + 1, j + 1, plane), 1, 0), (At(a + 1, j, plane), 1, 1), (At(a, j, plane), 0, 1) };
                // (each column along it a group: the race track's doorways take the columns they cut)
                Face(m, tile, normal, quad, group: a);
                Face(m, tile, -normal, quad, group: a);
                cells++;
            }
        m.Reading = $"a plane along {(alongX ? "x" : "z")} at {plane:0.00} across ({100.0 * best.Inside / Math.Max(1, drawn.Count):0}% of the picture inside the block), {cells} squares of it drawn";
    }

    // Stairs (a cell of LBA1's slope shapes 2-5: 2 rises toward +x, 3 toward +z, 4 toward -z, 5 toward -x -- TempleObjects.Steps): the
    // steps LBA1's picture draws, each a tread, a riser and its two sides, and the cell's high end's back, each textured with the picture
    // where its points fall. What is solid is TempleObjects' steps, as before.
    private static void MakeSteps(Model m, int shape)
    {
        var n = Math.Max(1, m.Kind.Steps);
        // (w up the stairs, s across them, y up)
        Vector3 P(double w, double y, double s) => shape switch
        {
            2 => new((float)w, (float)y, (float)s), 3 => new((float)s, (float)y, (float)w), 4 => new((float)s, (float)y, (float)(1 - w)), _ => new((float)(1 - w), (float)y, (float)s),
        };
        Vector3 Up(double w, double s) => P(w, 0, s) - P(0, 0, 0);
        var upW = Up(1, 0); var acrossS = Up(0, 1);
        // (the cell's sides that way: TempleObjects' directions)
        int SideOf(Vector3 d) => d.X > 0.5 ? 1 : d.X < -0.5 ? 2 : d.Z > 0.5 ? 3 : d.Z < -0.5 ? 4 : d.Y > 0.5 ? 0 : 5;
        for (var k = 0; k < n; k++)
        {
            double w0 = (double)k / n, w1 = (double)(k + 1) / n, top = (double)(k + 1) / n, low = (double)k / n;
            double kw0 = w0, kw1 = w1, kTop = top, kLow = low;
            var tread = m.AddTile(32, 32 / n, (u, v) => P(kw0 + v * (kw1 - kw0), kTop, u), m.Paint, solid: true);
            Face(m, tread, Vector3.UnitY, new[] { (P(w0, top, 0), 0.0, 0.0), (P(w0, top, 1), 1.0, 0.0), (P(w1, top, 1), 1.0, 1.0), (P(w1, top, 0), 0.0, 1.0) },
                k == n - 1 ? 0 : -1);
            var riser = m.AddTile(32, 16, (u, v) => P(kw0, kTop - v * (kTop - kLow), u), m.Paint, solid: true);
            Face(m, riser, -upW, new[] { (P(w0, top, 0), 0.0, 0.0), (P(w0, top, 1), 1.0, 0.0), (P(w0, low, 1), 1.0, 1.0), (P(w0, low, 0), 0.0, 1.0) },
                k == 0 ? SideOf(-upW) : -1);
        }
        // (each side one tile -- the stairs' outline on it, u up the stairs, v down the cell -- its steps' rectangles parts of it)
        foreach (var s in new[] { 0.0, 1.0 })
        {
            var s0 = s;
            var tile = m.AddTile(32, 16, (u, v) => P(u, 1 - v, s0), m.Paint, solid: true);
            var normal = s == 0 ? -acrossS : acrossS;
            for (var k = 0; k < n; k++)
            {
                double w0 = (double)k / n, w1 = (double)(k + 1) / n, top = (double)(k + 1) / n;
                Face(m, tile, normal, new[] { (P(w0, top, s), w0, 1 - top), (P(w1, top, s), w1, 1 - top), (P(w1, 0, s), w1, 1.0), (P(w0, 0, s), w0, 1.0) }, SideOf(normal));
            }
        }
        var back = m.AddTile(32, 16, (u, v) => P(1, 1 - v, u), m.Paint, solid: true);
        Face(m, back, upW, new[] { (P(1, 1, 0), 0.0, 0.0), (P(1, 1, 1), 1.0, 0.0), (P(1, 0, 1), 1.0, 1.0), (P(1, 0, 0), 0.0, 1.0) }, SideOf(upW));
        m.Reading = $"slope shape {shape}: {n} steps rising toward {(shape switch { 2 => "+x", 3 => "+z", 4 => "-z", _ => "-x" })}";
    }

    // the spikes of a bed of them, each way in a cell, and its plate's height (layers)
    private const int SpikesPerCell = 4;
    private const double SpikePlate = 0.2;

    // A bed of spikes on the floor (a trap): a plate, SpikesPerCell x SpikesPerCell steel spikes a cell on it, four-sided points as tall as the
    // picture's top says (its farthest spike's point). Its top is a height field -- a cell's spikes are drawn with one tile, the cell from
    // above, each texel the picture's where the spike's side under it is.
    private static void MakeSpikes(Model m)
    {
        var p = m.Picture;
        var s = 1.0 / SpikesPerCell;
        var topRow = Enumerable.Range(0, p.Height).First(y => Enumerable.Range(0, p.Width).Any(x => p.At(x, y) >= 0));
        var tip = Math.Clamp((p.OriginY + 15 + 12 * s - topRow) / 15.0, SpikePlate + 0.2, 1.0);
        double Height(double x, double z)
        {
            double fx = x / s - Math.Floor(x / s) - 0.5, fz = z / s - Math.Floor(z / s) - 0.5;
            return SpikePlate + (tip - SpikePlate) * (1 - 2 * Math.Max(Math.Abs(fx), Math.Abs(fz)));
        }
        for (var i = 0; i < m.Dx; i++)
            for (var k = 0; k < m.Dz; k++)
            {
                int ci = i, ck = k;
                var tile = m.AddTile(32, 32, (u, v) => new Vector3((float)(ci + u), (float)Height(ci + u, ck + v), (float)(ck + v)), m.Paint, solid: true);
                for (var a = 0; a < SpikesPerCell; a++)
                    for (var b = 0; b < SpikesPerCell; b++)
                    {
                        double x0 = i + a * s, z0 = k + b * s;
                        var apex = (new Vector3((float)(x0 + s / 2), (float)tip, (float)(z0 + s / 2)), (a + 0.5) * s, (b + 0.5) * s);
                        (Vector3 P, double U, double V) C(double x, double z) => (new Vector3((float)x, (float)SpikePlate, (float)z), x - i, z - k);
                        // (each side of the point: its base edge, and the apex; facing out and up)
                        Face(m, tile, new Vector3(0, 0.5f, -1), new[] { C(x0, z0), C(x0 + s, z0), apex });
                        Face(m, tile, new Vector3(1, 0.5f, 0), new[] { C(x0 + s, z0), C(x0 + s, z0 + s), apex });
                        Face(m, tile, new Vector3(0, 0.5f, 1), new[] { C(x0 + s, z0 + s), C(x0, z0 + s), apex });
                        Face(m, tile, new Vector3(-1, 0.5f, 0), new[] { C(x0, z0 + s), C(x0, z0), apex });
                    }
            }
        // (the plate's edges, round the block)
        var edgeX = m.AddTile(32, 16, (u, v) => new Vector3(m.Dx, (float)(SpikePlate * (1 - v)), (float)u), m.Paint, solid: true);
        var edgeZ = m.AddTile(32, 16, (u, v) => new Vector3((float)u, (float)(SpikePlate * (1 - v)), m.Dz), m.Paint, solid: true);
        var h = (float)SpikePlate;
        for (var k = 0; k < m.Dz; k++)
            foreach (var x in new[] { 0, m.Dx })
                Face(m, edgeX, new Vector3(x == 0 ? -1 : 1, 0, 0), new[] { (new Vector3(x, h, k), 0.0, 0.0), (new Vector3(x, h, k + 1), 1.0, 0.0), (new Vector3(x, 0, k + 1), 1.0, 1.0), (new Vector3(x, 0, k), 0.0, 1.0) });
        for (var i = 0; i < m.Dx; i++)
            foreach (var z in new[] { 0, m.Dz })
                Face(m, edgeZ, new Vector3(0, 0, z == 0 ? -1 : 1), new[] { (new Vector3(i, h, z), 0.0, 0.0), (new Vector3(i + 1, h, z), 1.0, 0.0), (new Vector3(i + 1, 0, z), 1.0, 1.0), (new Vector3(i, 0, z), 0.0, 1.0) });
        m.Reading = $"{SpikesPerCell * m.Dx} x {SpikesPerCell * m.Dz} spikes {tip:0.00} layers tall on a plate {SpikePlate:0.00} thick";
    }
}
