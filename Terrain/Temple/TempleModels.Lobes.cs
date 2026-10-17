using System.Numerics;

namespace LBAAssembler.Terrain.Temple;

// The rocks.
internal static partial class TempleModels
{
    // a lobe's width along its row (cells, about LBA1's), and its round top's height for its half width (cells: 2.2 times it, a little more
    // than half an ellipse as tall as wide -- LBA1's lobes are taller than round)
    private const double LobeWidth = 0.5, LobeDome = 2.2;

    // LBA1's rocks: a row of upright lobes, rounded on top, like a stand of cypresses, along a wall -- the lobes overlapping, those in front
    // lower, their outlines drawn in the rock's shading. The lobes stand along the block's long way through the middle of its depth (a block
    // a cell each way: across LBA1's view), as many as the row is long over LobeWidth, evenly along it, by turns behind and in front (toward
    // LBA1's camera). Seen from LBA1's angle the row's outline is its tallest lobes' tops: a lobe behind is as tall as the outline over its
    // share of the row (an eighth of a layer under it), one in front three quarters to nine tenths of that, each a little wider or narrower
    // than its share (by turns that look random, the same every build) -- a cluster, not a fence; its foot where the picture's lowest is under
    // its share (on the block's floor, or coming to a point over it). A lobe: an ellipse across (along the row as wide, as deep, no deeper
    // than the block), a little narrower at its foot, its top rounded (LobeDome: a ring half way round) to a point; 6 sides round -- 18 faces
    // -- two tiles (its body, its top), each texel the picture's.
    private static void MakeLobes(Model m)
    {
        var p = m.Picture;
        // (the line: origin + s dir; across it, `deep` either way)
        Vector3 origin, dir;
        double s0, s1, deep;
        if (m.Dx > m.Dz) { origin = new(0, 0, m.Dz / 2f); dir = Vector3.UnitX; s0 = 0; s1 = m.Dx; deep = m.Dz / 2.0; }
        else if (m.Dz > m.Dx) { origin = new(m.Dx / 2f, 0, 0); dir = Vector3.UnitZ; s0 = 0; s1 = m.Dz; deep = m.Dx / 2.0; }
        else { origin = new(m.Dx / 2f, 0, m.Dz / 2f); dir = Vector3.Normalize(new Vector3(1, 0, -1)); s0 = -m.Dx / Math.Sqrt(2); s1 = -s0; deep = m.Dx / 2.0; }
        var across = Vector3.Normalize(new Vector3(-dir.Z, 0, dir.X));
        if (Vector3.Dot(across, new Vector3(1, 0, 1)) < 0) across = -across;
        Vector3 Point(double s, double h) => origin + dir * (float)s + new Vector3(0, (float)h, 0);

        // ---- the row as LBA1 sees it, on the line: drawn (gaps of two samples closed: LBA1's dithering) at s (1/24 cell apart) and h (1/15 layer)
        const double ds = 1 / 24.0, dh = 1 / 15.0;
        int ns = (int)Math.Ceiling((s1 - s0) / ds), nh = (int)Math.Ceiling(m.Dy / dh);
        var raw = new bool[ns, nh];
        for (var i = 0; i < ns; i++)
            for (var j = 0; j < nh; j++)
                raw[i, j] = p.Sample(Point(s0 + (i + 0.5) * ds, (j + 0.5) * dh)) >= 0;
        bool R(int i, int j) => i >= 0 && j >= 0 && i < ns && j < nh && raw[i, j];
        bool D(int i, int j) => R(i, j) || (R(i - 1, j) || R(i - 2, j)) && (R(i + 1, j) || R(i + 2, j)) || (R(i, j - 1) || R(i, j - 2)) && (R(i, j + 1) || R(i, j + 2));
        // (each column's highest and lowest drawn sample; -1 none)
        var top = new int[ns]; var foot = new int[ns];
        for (var i = 0; i < ns; i++)
        {
            top[i] = foot[i] = -1;
            for (var j = nh - 1; j >= 0; j--) if (D(i, j)) { top[i] = j; break; }
            for (var j = 0; j < nh; j++) if (D(i, j)) { foot[i] = j; break; }
        }
        int first = Array.FindIndex(top, v => v >= 0), last = Array.FindLastIndex(top, v => v >= 0);
        if (first < 0) { m.Reading = "nothing drawn"; return; }

        var count = Math.Max(1, (int)Math.Round((last - first + 1) * ds / LobeWidth));
        var share = (last - first + 1) * ds / count;
        var lobes = 0;
        for (var k = 0; k < count; k++)
        {
            // ---- its share of the row: how tall (the outline over its middle three fifths), where its foot is
            double from = s0 + first * ds + k * share, to = from + share;
            var columns = Enumerable.Range(0, ns).Where(i => s0 + (i + 0.5) * ds >= from && s0 + (i + 0.5) * ds < to && top[i] >= 0).ToList();
            var middle = columns.Where(i => Math.Abs(s0 + (i + 0.5) * ds - (from + to) / 2) <= share * 0.3).DefaultIfEmpty(columns.FirstOrDefault(-1)).ToList();
            if (columns.Count == 0 || middle[0] < 0) continue;
            var height = (middle.Max(i => top[i]) + 1) * dh - 0.12;
            var bottom = columns.Min(i => foot[i]) * dh;
            if (height - bottom < 0.4) continue;
            // (a number for the lobe that looks random, the same every build: 0..1)
            double Jitter(uint salt) => (((uint)k * 2654435761u + salt * 40503u) >> 16 & 255) / 255.0;
            // (by turns behind and in front -- lower -- no deeper than the block; wider or narrower than its share)
            var inFront = k % 2 == 1;
            if (inFront) height = bottom + (height - bottom) * (0.75 + 0.15 * Jitter(1));
            var a = share / 2 * (1 + 0.25 * Jitter(2));
            var offset = deep >= 0.4 ? (inFront ? 0.12 : -0.12) : 0;
            var b = Math.Max(0.05, Math.Min(a, deep - Math.Abs(offset) - 0.02));
            var centre = (from + to) / 2;
            // (its group: the block's column along the row its middle stands in -- the race track's doorways take the columns they cut)
            var group = m.Dx == m.Dz ? 0 : Math.Clamp((int)Math.Floor(centre), 0, Math.Max(m.Dx, m.Dz) - 1);
            // ---- its rings: (height, half width) from its foot up -- a point under a foot over the block's floor -- its top's rounded
            var dome = Math.Min(LobeDome * a * 2, (height - bottom) * 0.6);
            var baseH = height - dome;
            var rings = new List<(double H, double A)>();
            if (bottom > 0.15) rings.Add((Math.Max(0, bottom - Math.Min(0.3, a)), 0));
            else bottom = 0;
            rings.Add((bottom, a * 0.9));
            rings.Add((baseH, a));
            rings.Add((baseH + dome * Math.Sin(50 * Math.PI / 180), a * Math.Cos(50 * Math.PI / 180)));
            rings.Add((height, 0));
            double WidthAt(double h)
            {
                if (h <= rings[0].H) return rings[0].A;
                for (var i = 1; i < rings.Count; i++)
                    if (h <= rings[i].H) return rings[i - 1].A + (rings[i].A - rings[i - 1].A) * (h - rings[i - 1].H) / Math.Max(1e-9, rings[i].H - rings[i - 1].H);
                return 0;
            }
            Vector3 On(double angle, double h)
            {
                var w = WidthAt(h);
                var q = Point(centre, h) + across * (float)offset + dir * (float)(w * Math.Cos(angle)) + across * (float)(w / a * b * Math.Sin(angle));
                return new Vector3(Math.Clamp(q.X, 0.01f, m.Dx - 0.01f), q.Y, Math.Clamp(q.Z, 0.01f, m.Dz - 0.01f));
            }
            // ---- two tiles round it (u round from the row's way, v down): its body, from its foot to its top's start, and its top
            int Tile(double h0, double h1) => m.AddTile(32, h1 - h0 > 1 ? 32 : 16, (u, v) => On(u * 2 * Math.PI, h1 - v * (h1 - h0)), m.Paint, solid: true);
            var body = Tile(rings[0].H, baseH);
            var head = Tile(baseH, height);
            const int sides = 6;
            for (var r = 0; r + 1 < rings.Count; r++)
            {
                var (lo, hi) = (rings[r], rings[r + 1]);
                var (tile, t0, t1) = hi.H <= baseH + 1e-9 ? (body, rings[0].H, baseH) : (head, baseH, height);
                double V(double h) => (t1 - h) / Math.Max(1e-9, t1 - t0);
                for (var n = 0; n < sides; n++)
                {
                    double a0 = 2 * Math.PI * n / sides, a1 = 2 * Math.PI * (n + 1) / sides, u0 = (double)n / sides, u1 = (double)(n + 1) / sides;
                    var mid = (a0 + a1) / 2;
                    // (out from the axis, tilted up or down as the lobe narrows or widens: cells and layers)
                    var normal = Vector3.Normalize(dir * (float)Math.Cos(mid) + across * (float)Math.Sin(mid) + new Vector3(0, (float)((lo.A - hi.A) / Math.Max(1e-6, hi.H - lo.H)), 0));
                    var corners = new List<(Vector3, double, double)> { (On(a0, hi.H), u0, V(hi.H)), (On(a1, hi.H), u1, V(hi.H)), (On(a1, lo.H), u1, V(lo.H)), (On(a0, lo.H), u0, V(lo.H)) };
                    if (hi.A < 1e-3) corners.RemoveAt(1);
                    else if (lo.A < 1e-3) corners.RemoveAt(3);
                    Face(m, tile, normal, corners, group: group);
                }
            }
            lobes++;
        }
        m.Reading = $"{lobes} lobes along {(m.Dx > m.Dz ? "x" : m.Dz > m.Dx ? "z" : "the view's across")}, {share:0.00} cells apart";
    }
}
