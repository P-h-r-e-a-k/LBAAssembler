using System.IO;
using System.Text.Json;

namespace LBAAssembler.Terrain;

// Builds a race track on an island from a centre line: levels the ground under it (with banking in the corners and an
// embankment either side), paints the retail Desert-island track's own pieces (asphalt, red/white curb line, orange arrows,
// red/gold hatching, the white start line) and clears the decor objects in the way. Works on the island-wide vertex grid, so a
// track can cross cube borders. The numbers (widths in cells of 512 units, heights in world units) are the ones the retail track
// uses: a 9-cell wide road with a one-cell curb line at each edge.
internal sealed class RaceTrackPlan
{
    // The plan's points are cells counted from this island cell.
    public int OriginCellX { get; set; }
    public int OriginCellZ { get; set; }
    // The closed centre line of the lap.
    public double[][] Points { get; set; } = Array.Empty<double[]>();
    // The two ends of the pit lane (it runs beside the lap between them).
    public double[]? PitA { get; set; }
    public double[]? PitB { get; set; }

    public static RaceTrackPlan Load(string path)
    {
        using var stream = File.OpenRead(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<RaceTrackPlan>(stream, options) ?? throw new InvalidDataException("Empty plan.");
    }
}

internal sealed class RaceTrackOptions
{
    public double AsphaltHalfWidth { get; set; } = 3.5;
    public double CurbHalfWidth { get; set; } = 4.5;
    public double VergeHalfWidth { get; set; } = 6.0;
    // Cells over which the levelled ground blends back into the natural ground beyond the verge.
    public double BlendWidth { get; set; } = 7.0;
    // Steepest climb along the road, height per horizontal distance.
    public double MaxGrade { get; set; } = 0.09;
    // Cross slope in a bend: height units per cell of the way across, per (1 / turn radius in cells).
    public double BankGain { get; set; } = 700;
    public double MaxBank { get; set; } = 70;
    // How far the ground height along the road is smoothed (cells).
    public double ProfileSmoothing { get; set; } = 6;
    // Where the road is lowest above the sea when it crosses water.
    public double BridgeClearance { get; set; } = 700;
    public double Spacing { get; set; } = 0.5;
    public bool RemoveSolidDecors { get; set; } = true;
    // Decor bodies that are plants, posts, fences and small props: cleared where they stand on the road. Everything else is
    // a building or a rock.
    public HashSet<int> RemovableBodies { get; set; } = new()
    {
        0, 1, 2, 4, 5, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 23, 29, 30, 31, 32, 34, 35, 36, 37, 39, 41, 44, 45, 47, 48, 50, 51, 55, 56, 57,
        58, 59, 60, 61, 62, 63, 72, 73, 74, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 93, 98, 99, 101, 103, 104, 105,
    };
    // Decor bodies of the retail race track that are left alone (its gantry and arch).
    public HashSet<int> ProtectedBodies { get; set; } = new() { 64, 65, 66, 67, 68, 69, 70, 71 };
    // Cells (island cell coordinates, inclusive) the new track must not touch: the retail track.
    public List<(int X0, int Z0, int X1, int Z1)> Keep { get; set; } = new();
}

internal sealed class RaceTrackReport
{
    public int Vertices, Cells, DecorsRemoved, SolidDecorsRemoved, BridgeCells;
    public double Length;
    public List<string> Notes { get; } = new();
    public List<(int Start, int End)> BridgeSpans { get; } = new();
    public List<(int CubeX, int CubeZ, int Body, string Kind)> Removed { get; } = new();
    public List<double[]> Arrows { get; } = new();
    // Where the lap crosses itself (island cell coordinates, the height there, the angle between the two roads in degrees).
    public List<(double X, double Z, double Y, double Angle)> Crossings { get; } = new();
    public List<(double X, double Z, double Y, double DirX, double DirZ)> StartLine { get; } = new();
    public List<(double X0, double Z0, double X1, double Z1)> BridgeCoords { get; } = new();
    public List<string> Placed { get; } = new();
    // How far (cells) an island cell position is from the nearest road's centre line, 1e9 when far away.
    public Func<double, double, double> DistanceToRoad { get; set; } = (_, _) => 1e9;
}

internal sealed class TrackRoad
{
    public string Name = "";
    public bool Closed;
    public double[] X = Array.Empty<double>(), Z = Array.Empty<double>();     // island cell coordinates
    public double[] S = Array.Empty<double>(), Tx = Array.Empty<double>(), Tz = Array.Empty<double>(), Kappa = Array.Empty<double>();
    public double[] H = Array.Empty<double>();
    public bool[] Bridge = Array.Empty<bool>();
    public double AsphaltHalf, CurbHalf, VergeHalf, Blend;
    public int Count => X.Length;
    public double Length;
}

internal readonly record struct RoadHit(int Road, double Dist, double Lat, double S, double H, double Bank, bool Bridge, double Kappa);

internal static class RaceTrackBuilder
{
    private const int Grid = IslandFile.GridSize;
    private static int PitMiddle;

    public static RaceTrackReport Build(IslandFile island, RaceTrackPlan plan, RaceTrackOptions options)
    {
        var report = new RaceTrackReport();
        var field = new Field(island);
        var roads = new List<TrackRoad>();

        var main = MakeRoad("lap", plan, options, closed: true);
        roads.Add(main);
        report.Length = main.Length;
        Profile(main, field, options, report);
        var crossings = FindCrossings(main, options);
        EqualiseCrossings(main, crossings, options);
        foreach (var c in crossings) report.Crossings.Add((c.X, c.Z, main.H[c.I], c.Angle));
        foreach (var (a, b) in report.BridgeSpans) report.BridgeCoords.Add((main.X[a], main.Z[a], main.X[b], main.Z[b]));

        if (plan.PitA is { } pa && plan.PitB is { } pb) roads.Add(MakePit(main, plan, pa, pb, options, report));

        var index = new RoadIndex(roads);
        report.DistanceToRoad = (x, z) => { var h = index.Near(x, z, 14, 1); return h.Count == 0 ? 1e9 : h[0].Dist; };
        var startIndex = roads.Count > 1 ? PitMiddle : -1;
        var follow = new IslandOps.DecorFollow(island);
        ClearDecors(island, index, options, report);
        ModifyGround(island, field, index, roads, options, report);
        PaintRoad(island, field, index, roads, options, report, startIndex);
        follow.Apply();
        PlaceStructures(island, main, crossings, startIndex, report);
        Relight(island, index, options);
        return report;
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // the centre line

    private static TrackRoad MakeRoad(string name, RaceTrackPlan plan, RaceTrackOptions o, bool closed)
    {
        var raw = plan.Points.Select(p => (X: p[0] + plan.OriginCellX, Z: p[1] + plan.OriginCellZ)).ToList();
        return Resample(name, raw, o, closed);
    }

    private static TrackRoad Resample(string name, List<(double X, double Z)> raw, RaceTrackOptions o, bool closed)
    {
        var pts = closed ? raw.Append(raw[0]).ToList() : raw;
        var cum = new List<double> { 0 };
        for (var i = 1; i < pts.Count; i++) cum.Add(cum[^1] + Math.Sqrt(Sq(pts[i].X - pts[i - 1].X) + Sq(pts[i].Z - pts[i - 1].Z)));
        var total = cum[^1];
        var n = Math.Max(8, (int)Math.Round(total / o.Spacing));
        var count = closed ? n : n + 1;
        var road = new TrackRoad { Name = name, Closed = closed, X = new double[count], Z = new double[count], Length = total };
        var j = 0;
        for (var i = 0; i < count; i++)
        {
            var d = closed ? total * i / n : total * i / n;
            while (j < pts.Count - 2 && cum[j + 1] < d) j++;
            var seg = cum[j + 1] - cum[j];
            var t = seg <= 1e-9 ? 0 : (d - cum[j]) / seg;
            road.X[i] = pts[j].X + (pts[j + 1].X - pts[j].X) * t;
            road.Z[i] = pts[j].Z + (pts[j + 1].Z - pts[j].Z) * t;
        }
        road.AsphaltHalf = o.AsphaltHalfWidth; road.CurbHalf = o.CurbHalfWidth; road.VergeHalf = o.VergeHalfWidth; road.Blend = o.BlendWidth;
        Geometry(road, o);
        return road;
    }

    private static void Geometry(TrackRoad r, RaceTrackOptions o)
    {
        var n = r.Count;
        r.S = new double[n]; r.Tx = new double[n]; r.Tz = new double[n]; r.Kappa = new double[n];
        for (var i = 1; i < n; i++) r.S[i] = r.S[i - 1] + Math.Sqrt(Sq(r.X[i] - r.X[i - 1]) + Sq(r.Z[i] - r.Z[i - 1]));
        var total = r.Closed ? r.S[n - 1] + Math.Sqrt(Sq(r.X[0] - r.X[n - 1]) + Sq(r.Z[0] - r.Z[n - 1])) : r.S[n - 1];
        r.Length = total;
        var theta = new double[n];
        var w = Math.Max(1, (int)Math.Round(3 / o.Spacing));
        for (var i = 0; i < n; i++)
        {
            var a = At(r, i - w); var b = At(r, i + w);
            var dx = r.X[b] - r.X[a]; var dz = r.Z[b] - r.Z[a];
            if (!r.Closed && (i - w < 0 || i + w >= n)) { a = Math.Max(0, i - w); b = Math.Min(n - 1, i + w); dx = r.X[b] - r.X[a]; dz = r.Z[b] - r.Z[a]; }
            var len = Math.Sqrt(dx * dx + dz * dz) + 1e-12;
            r.Tx[i] = dx / len; r.Tz[i] = dz / len; theta[i] = Math.Atan2(dz, dx);
        }
        var k = new double[n];
        for (var i = 0; i < n; i++)
        {
            var a = At(r, i - w); var b = At(r, i + w);
            var d = theta[b] - theta[a];
            while (d > Math.PI) d -= 2 * Math.PI;
            while (d < -Math.PI) d += 2 * Math.PI;
            var ds = Math.Max(1e-6, Dist(r, a, b));
            k[i] = d / ds;
        }
        r.Kappa = Smooth(k, 4 / o.Spacing, r.Closed);
    }

    private static int At(TrackRoad r, int i) => r.Closed ? ((i % r.Count) + r.Count) % r.Count : Math.Clamp(i, 0, r.Count - 1);

    // Along-the-road distance between two point indices (the short way round a closed road).
    private static double Dist(TrackRoad r, int a, int b)
    {
        var d = Math.Abs(r.S[b] - r.S[a]);
        return r.Closed ? Math.Min(d, r.Length - d) : d;
    }

    private static double[] Smooth(double[] v, double sigma, bool closed)
    {
        var n = v.Length; var radius = (int)Math.Ceiling(sigma * 3);
        var kernel = new double[radius * 2 + 1]; double sum = 0;
        for (var i = -radius; i <= radius; i++) { kernel[i + radius] = Math.Exp(-0.5 * i * i / (sigma * sigma)); sum += kernel[i + radius]; }
        var result = new double[n];
        for (var i = 0; i < n; i++)
        {
            double acc = 0, wsum = 0;
            for (var k = -radius; k <= radius; k++)
            {
                var j = i + k;
                if (closed) j = ((j % n) + n) % n; else if (j < 0 || j >= n) continue;
                acc += v[j] * kernel[k + radius]; wsum += kernel[k + radius];
            }
            result[i] = acc / wsum;
        }
        return result;
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // the height profile along the road

    private static void Profile(TrackRoad r, Field field, RaceTrackOptions o, RaceTrackReport report)
    {
        var n = r.Count;
        var h = new double[n]; var sea = new bool[n];
        for (var i = 0; i < n; i++)
        {
            sea[i] = !field.Drawn(r.X[i], r.Z[i]);
            h[i] = field.Height(r.X[i], r.Z[i]);
        }
        // water crossings: merge short gaps, then add the approach on either side
        var bridge = new bool[n];
        var i0 = 0;
        while (i0 < n)
        {
            if (!sea[i0]) { i0++; continue; }
            var i1 = i0; var gap = 0;
            for (var k = i0; k < n; k++)
            {
                if (sea[k]) { i1 = k; gap = 0; } else if (++gap > 8 / o.Spacing) break;
            }
            var approach = (int)(5 / o.Spacing);
            var a = i0 - approach; var b = i1 + approach;
            for (var k = a; k <= b; k++) bridge[At(r, k)] = true;
            report.BridgeSpans.Add((At(r, a), At(r, b)));
            i0 = i1 + 1;
        }
        // the ground under a bridge is the sea: replace it by a straight line between the two shores
        for (var i = 0; i < n; i++)
        {
            if (!bridge[i]) continue;
            var a = i; while (bridge[At(r, a)] && a > i - n) a--;
            var b = i; while (bridge[At(r, b)] && b < i + n) b++;
            var ha = h[At(r, a)]; var hb = h[At(r, b)];
            var t = (double)(i - a) / Math.Max(1, b - a);
            h[i] = ha + (hb - ha) * t;
        }
        var smooth = Smooth(h, o.ProfileSmoothing / o.Spacing, r.Closed);
        for (var i = 0; i < n; i++) if (bridge[i]) smooth[i] = Math.Max(smooth[i], o.BridgeClearance);
        r.H = smooth; r.Bridge = bridge;
        LimitGrade(r, o);
        for (var i = 0; i < n; i++) if (bridge[i] && r.H[i] < o.BridgeClearance) r.H[i] = o.BridgeClearance;
        LimitGrade(r, o);
    }

    private static void LimitGrade(TrackRoad r, RaceTrackOptions o)
    {
        var n = r.Count; var step = o.MaxGrade * 512 * o.Spacing;
        for (var pass = 0; pass < 60; pass++)
        {
            var changed = false;
            for (var i = 1; i < n * 2; i++)
            {
                var a = At(r, i - 1); var b = At(r, i);
                if (!r.Closed && i >= n) break;
                var d = r.H[b] - r.H[a];
                if (d > step) { var m = (d - step) / 2; r.H[b] -= m; r.H[a] += m; changed = true; }
                else if (d < -step) { var m = (-d - step) / 2; r.H[b] += m; r.H[a] -= m; changed = true; }
            }
            if (!changed) break;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // the pit lane: a short road beside the lap, tapering onto it at both ends

    private static TrackRoad MakePit(TrackRoad main, RaceTrackPlan plan, double[] a, double[] b, RaceTrackOptions o, RaceTrackReport report)
    {
        var ax = a[0] + plan.OriginCellX; var az = a[1] + plan.OriginCellZ; var bx = b[0] + plan.OriginCellX; var bz = b[1] + plan.OriginCellZ;
        int Near(double x, double z) { var best = 0; var bd = double.MaxValue; for (var i = 0; i < main.Count; i++) { var d = Sq(main.X[i] - x) + Sq(main.Z[i] - z); if (d < bd) { bd = d; best = i; } } return best; }
        var ia = Near(ax, az); var ib = Near(bx, bz);
        // the way from a to b that follows the lap's own direction and is the shorter of the two
        var fwd = ((ib - ia) % main.Count + main.Count) % main.Count;
        var step = fwd <= main.Count / 2 ? 1 : -1;
        var count = step == 1 ? fwd : main.Count - fwd;
        var mid = At(main, ia + step * count / 2);
        // which side the pit lane's picture sits on
        var mx = (ax + bx) / 2; var mz = (az + bz) / 2;
        var side = Math.Sign(main.Tx[mid] * (mz - main.Z[mid]) - main.Tz[mid] * (mx - main.X[mid]));
        if (side == 0) side = 1;
        const double offset = 10.5, halfAsphalt = 2.5, halfCurb = 3.5, taper = 24;
        var pts = new List<(double X, double Z)>();
        var hs = new List<double>();
        for (var k = 0; k <= count; k++)
        {
            var i = At(main, ia + step * k);
            var fromEnd = Math.Min(k, count - k) * o.Spacing;
            var t = Math.Clamp(fromEnd / taper, 0, 1); var e = t * t * (3 - 2 * t);
            var nx = -main.Tz[i] * side; var nz = main.Tx[i] * side;      // toward the pit side
            pts.Add((main.X[i] + nx * offset * e, main.Z[i] + nz * offset * e));
            hs.Add(main.H[i]);
        }
        var pit = Resample("pit", pts, o, closed: false);
        pit.AsphaltHalf = halfAsphalt; pit.CurbHalf = halfCurb; pit.VergeHalf = halfCurb + 1.0; pit.Blend = 4;
        // heights: follow the lap's profile at the same place
        pit.H = new double[pit.Count]; pit.Bridge = new bool[pit.Count];
        for (var i = 0; i < pit.Count; i++)
        {
            var f = pit.Count <= 1 ? 0 : (double)i / (pit.Count - 1);
            var src = f * count;
            var lo = (int)Math.Floor(src); var hi = Math.Min(count, lo + 1); var tt = src - lo;
            pit.H[i] = hs[lo] * (1 - tt) + hs[hi] * tt;
        }
        PitMiddle = mid;
        report.Notes.Add($"pit lane: {pit.Length:0} cells long, {(side > 0 ? "left" : "right")} of the lap (points {ia} to {At(main, ia + step * count)}, from cell ({main.X[ia]:0.0}, {main.Z[ia]:0.0}) to ({main.X[At(main, ia + step * count)]:0.0}, {main.Z[At(main, ia + step * count)]:0.0}))");
        return pit;
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // finding the road near a place

    private sealed class RoadIndex
    {
        private const int Bucket = 4;
        private readonly Dictionary<(int, int), List<(int Road, int Index)>> grid = new();
        private readonly List<TrackRoad> roads;
        public (int X0, int Z0, int X1, int Z1) Bounds { get; }

        public RoadIndex(List<TrackRoad> roads)
        {
            this.roads = roads;
            double minX = 1e9, minZ = 1e9, maxX = -1e9, maxZ = -1e9;
            for (var ri = 0; ri < roads.Count; ri++)
            {
                var r = roads[ri];
                for (var i = 0; i < r.Count; i++)
                {
                    var key = ((int)Math.Floor(r.X[i] / Bucket), (int)Math.Floor(r.Z[i] / Bucket));
                    if (!grid.TryGetValue(key, out var list)) grid[key] = list = new();
                    list.Add((ri, i));
                    minX = Math.Min(minX, r.X[i]); maxX = Math.Max(maxX, r.X[i]); minZ = Math.Min(minZ, r.Z[i]); maxZ = Math.Max(maxZ, r.Z[i]);
                }
            }
            var pad = roads.Max(r => r.VergeHalf + r.Blend) + 2;
            Bounds = ((int)Math.Floor(minX - pad), (int)Math.Floor(minZ - pad), (int)Math.Ceiling(maxX + pad), (int)Math.Ceiling(maxZ + pad));
        }

        // The nearest road point of each different piece of road within `radius` (up to `max`), closest first.
        public List<RoadHit> Near(double x, double z, double radius, int max = 3)
        {
            var found = new List<(double D2, int Road, int Index)>();
            var b0 = (int)Math.Floor((x - radius) / Bucket); var b1 = (int)Math.Floor((x + radius) / Bucket);
            var c0 = (int)Math.Floor((z - radius) / Bucket); var c1 = (int)Math.Floor((z + radius) / Bucket);
            var r2 = radius * radius;
            for (var bz = c0; bz <= c1; bz++)
            for (var bx = b0; bx <= b1; bx++)
                if (grid.TryGetValue((bx, bz), out var list))
                    foreach (var (ri, i) in list)
                    {
                        var d2 = Sq(roads[ri].X[i] - x) + Sq(roads[ri].Z[i] - z);
                        if (d2 <= r2) found.Add((d2, ri, i));
                    }
            found.Sort((a, b) => a.D2.CompareTo(b.D2));
            var picked = new List<(int Road, int Index)>();
            var hits = new List<RoadHit>();
            foreach (var (_, ri, i) in found)
            {
                var road = roads[ri];
                if (picked.Any(p => p.Road == ri && Separation(road, p.Index, i) < 18)) continue;
                picked.Add((ri, i));
                hits.Add(Project(road, ri, i, x, z));
                if (hits.Count >= max) break;
            }
            return hits;
        }

        private static double Separation(TrackRoad r, int a, int b)
        {
            var d = Math.Abs(r.S[a] - r.S[b]);
            return r.Closed ? Math.Min(d, r.Length - d) : d;
        }

        private static RoadHit Project(TrackRoad r, int ri, int i, double x, double z)
        {
            // exact distance to the two segments around the point
            var best = new RoadHit(ri, double.MaxValue, 0, 0, 0, 0, false, 0);
            for (var side = -1; side <= 0; side++)
            {
                int a, b;
                if (r.Closed) { a = At(r, i + side); b = At(r, i + side + 1); }
                else { a = i + side; b = i + side + 1; if (a < 0 || b >= r.Count) continue; }
                var sx = r.X[b] - r.X[a]; var sz = r.Z[b] - r.Z[a];
                var len2 = sx * sx + sz * sz;
                var t = len2 < 1e-12 ? 0 : Math.Clamp(((x - r.X[a]) * sx + (z - r.Z[a]) * sz) / len2, 0, 1);
                var px = r.X[a] + sx * t; var pz = r.Z[a] + sz * t;
                var d = Math.Sqrt(Sq(x - px) + Sq(z - pz));
                if (d >= best.Dist) continue;
                var tx = r.Tx[a] * (1 - t) + r.Tx[b] * t; var tz = r.Tz[a] * (1 - t) + r.Tz[b] * t;
                var tl = Math.Sqrt(tx * tx + tz * tz) + 1e-12; tx /= tl; tz /= tl;
                var lat = tx * (z - pz) - tz * (x - px);
                var k = r.Kappa[a] * (1 - t) + r.Kappa[b] * t;
                var s = r.S[a] + Math.Sqrt(len2) * t;
                var h = r.H[a] * (1 - t) + r.H[b] * t;
                best = new RoadHit(ri, d, lat, s, h, 0, r.Bridge[a] && r.Bridge[b], k);
            }
            return best;
        }
    }

    private static double BankOf(TrackRoad r, RoadHit hit, RaceTrackOptions o)
        => hit.Bridge ? 0 : Math.Clamp(-hit.Kappa * o.BankGain, -o.MaxBank, o.MaxBank);

    // ---------------------------------------------------------------------------------------------------------------------
    // where the lap crosses itself

    private readonly record struct Crossing(int I, int J, double X, double Z, double Angle, double BisX, double BisZ);

    private static List<Crossing> FindCrossings(TrackRoad r, RaceTrackOptions o)
    {
        var found = new List<(int I, int J, double D)>();
        for (var i = 0; i < r.Count; i++)
        for (var j = i + 1; j < r.Count; j++)
        {
            var sep = Math.Abs(r.S[j] - r.S[i]); sep = Math.Min(sep, r.Length - sep);
            if (sep < 40) continue;
            var d = Math.Sqrt(Sq(r.X[i] - r.X[j]) + Sq(r.Z[i] - r.Z[j]));
            if (d < 1.5) found.Add((i, j, d));
        }
        var result = new List<Crossing>();
        foreach (var (i, j, _) in found.OrderBy(f => f.D))
        {
            if (result.Any(c => (Math.Abs(c.I - i) < 80 || Math.Abs(c.I - j) < 80) && (Math.Abs(c.J - j) < 80 || Math.Abs(c.J - i) < 80))) continue;
            var dot = r.Tx[i] * r.Tx[j] + r.Tz[i] * r.Tz[j];
            var angle = Math.Acos(Math.Clamp(Math.Abs(dot), 0, 1)) * 180 / Math.PI;
            var sign = dot >= 0 ? 1 : -1;
            var bx = r.Tx[i] + sign * r.Tx[j]; var bz = r.Tz[i] + sign * r.Tz[j];
            var bl = Math.Sqrt(bx * bx + bz * bz) + 1e-9;
            result.Add(new Crossing(i, j, (r.X[i] + r.X[j]) / 2, (r.Z[i] + r.Z[j]) / 2, angle, bx / bl, bz / bl));
        }
        return result;
    }

    // Both roads meet at the same height where they cross (the ground is one surface): pull the two profiles together over the
    // stretch where they share cells, then re-limit the grade.
    private static void EqualiseCrossings(TrackRoad r, List<Crossing> crossings, RaceTrackOptions o)
    {
        if (crossings.Count == 0) return;
        var pairs = new List<(int I, int J)>();
        for (var i = 0; i < r.Count; i++)
        for (var j = i + 1; j < r.Count; j++)
        {
            var sep = Math.Abs(r.S[j] - r.S[i]); sep = Math.Min(sep, r.Length - sep);
            if (sep < 40) continue;
            if (Sq(r.X[i] - r.X[j]) + Sq(r.Z[i] - r.Z[j]) < 7 * 7) pairs.Add((i, j));
        }
        for (var round = 0; round < 120; round++)
        {
            foreach (var (i, j) in pairs)
            {
                var m = (r.H[i] + r.H[j]) / 2;
                r.H[i] += (m - r.H[i]) * 0.5; r.H[j] += (m - r.H[j]) * 0.5;
            }
            var sm = Smooth(r.H, 2.0 / o.Spacing, r.Closed);
            for (var i = 0; i < r.Count; i++) r.H[i] = r.Bridge[i] ? r.H[i] : sm[i];
        }
        LimitGrade(r, o);
        for (var round = 0; round < 40; round++)
            foreach (var (i, j) in pairs) { var m = (r.H[i] + r.H[j]) / 2; r.H[i] = m; r.H[j] = m; }
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // decor objects in the way

    private static void ClearDecors(IslandFile island, RoadIndex index, RaceTrackOptions o, RaceTrackReport report)
    {
        foreach (var (cx, cz, cube) in IslandOps.CubeCells(island))
        {
            var remove = new List<IslandDecor>();
            foreach (var d in cube.Decors)
            {
                var body = d.Body & 0xFFFF;
                if (o.ProtectedBodies.Contains(body)) continue;
                var x0 = (int)Math.Floor((cx * (double)IslandFile.CubeSize + d.XMin) / 512); var x1 = (int)Math.Floor((cx * (double)IslandFile.CubeSize + d.XMax) / 512);
                var z0 = (int)Math.Floor((cz * (double)IslandFile.CubeSize + d.ZMin) / 512); var z1 = (int)Math.Floor((cz * (double)IslandFile.CubeSize + d.ZMax) / 512);
                var hit = false;
                for (var z = z0; z <= z1 && !hit; z++)
                for (var x = x0; x <= x1 && !hit; x++)
                {
                    var near = index.Near(x + 0.5, z + 0.5, 8, 2);
                    if (near.Any(h => h.Dist <= (h.Road == 0 ? 6.5 : 4.5))) hit = true;
                }
                if (!hit) continue;
                var removable = o.RemovableBodies.Contains(body);
                if (!removable && !o.RemoveSolidDecors) { report.Notes.Add($"solid decor left on the road: body {body} in cube ({cx},{cz})"); continue; }
                remove.Add(d);
                report.Removed.Add((cx, cz, body, removable ? "prop" : "solid"));
                if (removable) report.DecorsRemoved++; else report.SolidDecorsRemoved++;
            }
            foreach (var d in remove) cube.Decors.Remove(d);
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // the ground

    private static void ModifyGround(IslandFile island, Field field, RoadIndex index, List<TrackRoad> roads, RaceTrackOptions o, RaceTrackReport report)
    {
        var b = index.Bounds;
        var maxReach = roads.Max(r => r.VergeHalf + r.Blend);
        var updates = new List<(int Gx, int Gz, double Height, double Weight)>();
        for (var gz = Math.Max(0, b.Z0); gz <= Math.Min(Grid, b.Z1); gz++)
        for (var gx = Math.Max(0, b.X0); gx <= Math.Min(Grid, b.X1); gx++)
        {
            if (!island.HasVertex(gx, gz)) continue;
            var hits = index.Near(gx, gz, maxReach);
            if (hits.Count == 0) continue;
            double keep = 1, num = 0, den = 0;
            foreach (var hit in hits)
            {
                var r = roads[hit.Road];
                var verge = hit.Bridge ? r.CurbHalf + 0.5 : r.VergeHalf;
                var blend = hit.Bridge ? 1.2 : r.Blend;
                var d = Math.Abs(hit.Lat);
                double w;
                if (d <= verge) w = 1;
                else if (d >= verge + blend) continue;
                else { var t = (d - verge) / blend; w = 1 - t * t * (3 - 2 * t); }
                var bank = BankOf(r, hit, o);
                var target = hit.H + bank * Math.Clamp(hit.Lat, -r.CurbHalf - 1, r.CurbHalf + 1);
                keep *= 1 - w; num += w * target; den += w;
            }
            if (den <= 0) continue;
            var total = 1 - keep;
            var mixed = num / den;
            var original = field.VertexHeight(gx, gz);
            var value = original * (1 - total) + mixed * total;
            updates.Add((gx, gz, value, total));
        }
        foreach (var (gx, gz, height, _) in updates) island.SetHeight(gx, gz, (int)Math.Round(height));
        report.Vertices = updates.Count;
        field.Refresh(island);
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // painting

    private enum Kind { None, Asphalt, RedCurb, WhiteCurb, Sand, Hatch, Wall, Arrow, Start }

    private static void PaintRoad(IslandFile island, Field field, RoadIndex index, List<TrackRoad> roads, RaceTrackOptions o, RaceTrackReport report, int startIndex)
    {
        var b = index.Bounds;
        var kinds = new Dictionary<(int, int), Kind>();
        var maxReach = roads.Max(r => r.VergeHalf) + 0.8;
        for (var gz = Math.Max(0, b.Z0); gz < Math.Min(Grid, b.Z1); gz++)
        for (var gx = Math.Max(0, b.X0); gx < Math.Min(Grid, b.X1); gx++)
        {
            if (island.CubeAt(gx / IslandCube.Cells, gz / IslandCube.Cells) is not { HasPolygons: true }) continue;
            var hits = index.Near(gx + 0.5, gz + 0.5, maxReach, 2);
            if (hits.Count == 0) continue;
            // the road whose surface the cell is really part of: a pit lane cell belongs to the pit lane when it is inside its asphalt
            RoadHit? chosen = null; var ck = Kind.None;
            foreach (var hit in hits)
            {
                var r = roads[hit.Road]; var d = Math.Abs(hit.Lat);
                Kind k;
                if (d <= r.AsphaltHalf) k = Kind.Asphalt;
                else if (d <= r.CurbHalf) k = (int)Math.Floor(hit.S / 1.6) % 2 == 0 ? Kind.RedCurb : Kind.WhiteCurb;
                else if (d <= r.VergeHalf) k = hit.Bridge ? Kind.Wall : Kind.Sand;
                else continue;
                var rank = k == Kind.Asphalt ? 3 : k is Kind.RedCurb or Kind.WhiteCurb ? 2 : 1;
                var crank = ck == Kind.Asphalt ? 3 : ck is Kind.RedCurb or Kind.WhiteCurb ? 2 : ck == Kind.None ? 0 : 1;
                if (rank > crank) { chosen = hit; ck = k; }
            }
            if (chosen is null || ck == Kind.None) continue;
            // banked bends: the outside verge is hatched
            if (ck == Kind.Sand && Math.Abs(chosen.Value.Kappa) > 0.05 && chosen.Value.Lat * chosen.Value.Kappa < 0) ck = Kind.Hatch;
            kinds[(gx, gz)] = ck;
        }
        MarkArrows(roads[0], kinds, o, report);
        if (startIndex >= 0) MarkStartLine(roads[0], startIndex, kinds, report);
        var painter = new Painter(island);
        foreach (var ((gx, gz), kind) in kinds)
        {
            painter.Paint(gx, gz, kind);
            report.Cells++;
            if (kind == Kind.Wall) report.BridgeCells++;
        }
        Console.WriteLine($"  painted {kinds.Count} cells");
    }

    // Orange arrow shapes on the straight before bends, like the retail ones: four cells long, pointing the way of the lap.
    private static void MarkArrows(TrackRoad r, Dictionary<(int, int), Kind> kinds, RaceTrackOptions o, RaceTrackReport report)
    {
        var n = r.Count;
        // find corner exits: where |kappa| drops below the threshold after a bend of at least 20 cells, plus every 140 cells on long straights
        var marks = new List<int>();
        var lastMark = -1000.0;
        for (var i = 0; i < n; i++)
        {
            if (r.Bridge[i]) continue;
            var straight = Math.Abs(r.Kappa[i]) < 0.02;
            var prevBend = false;
            for (var k = 1; k <= (int)(14 / o.Spacing); k++) if (Math.Abs(r.Kappa[At(r, i - k)]) > 0.06) { prevBend = true; break; }
            if (!straight) continue;
            var s = r.S[i];
            var since = s - lastMark; if (since < 0) since += r.Length;
            if ((prevBend && since > 40) || since > 150)
            {
                marks.Add(i); lastMark = s;
            }
        }
        foreach (var i in marks)
        {
            var i2 = At(r, i + (int)(18 / o.Spacing));
            var cx = r.X[i2]; var cz = r.Z[i2]; var tx = r.Tx[i2]; var tz = r.Tz[i2];
            // an arrow in the local frame (forward f, sideways u), 4 cells long
            for (var gz = (int)Math.Floor(cz - 3); gz <= (int)Math.Ceiling(cz + 3); gz++)
            for (var gx = (int)Math.Floor(cx - 3); gx <= (int)Math.Ceiling(cx + 3); gx++)
            {
                var px = gx + 0.5 - cx; var pz = gz + 0.5 - cz;
                var f = px * tx + pz * tz; var u = -px * tz + pz * tx;
                var inShaft = f >= -2.0 && f <= 0.4 && Math.Abs(u) <= 0.75;
                var inHead = f >= 0.0 && f <= 2.2 && Math.Abs(u) <= 2.0 * (1 - f / 2.2) + 0.15;
                if (!(inShaft || inHead)) continue;
                if (kinds.TryGetValue((gx, gz), out var k) && k == Kind.Asphalt) kinds[(gx, gz)] = Kind.Arrow;
            }
            report.Arrows.Add(new[] { cx, cz });
        }
    }

    private sealed class Painter
    {
        private readonly IslandFile island;
        public Painter(IslandFile island) => this.island = island;

        // sample words of the retail track's cells: (bank, texFlag, polyFlag, step)
        private static IslandPolygon Flat(int bank, int step) => new IslandPolygon(0).With(bank: bank, texFlag: 0, polyFlag: 3, sampleStep: step, codeJeu: 0);
        private static IslandPolygon Textured(int step) => new IslandPolygon(0).With(bank: 2, texFlag: 1, polyFlag: 0, sampleStep: step, codeJeu: 0);

        public void Paint(int gx, int gz, Kind kind)
        {
            var cube = island.CubeAt(gx / IslandCube.Cells, gz / IslandCube.Cells)!;
            var x = gx % IslandCube.Cells; var z = gz % IslandCube.Cells;
            var diagonal = new IslandPolygon(cube.Polygon(x, z, 0)).Diagonal;
            for (var half = 0; half < 2; half++)
            {
                IslandPolygon p;
                bool col = false;
                switch (kind)
                {
                    case Kind.Asphalt: p = Textured(5).With(textureIndex: IslandGround.TextureIndexFor(cube, IslandGround.TileDefinition(96, 0, 32, 32, diagonal, half))); break;
                    case Kind.Start:
                    case Kind.WhiteCurb: p = Textured(4).With(textureIndex: IslandGround.TextureIndexFor(cube, IslandGround.TileDefinition(180, 155, 1, 1, diagonal, half))); break;
                    case Kind.RedCurb: p = Flat(4, 5); break;
                    case Kind.Arrow: p = Flat(5, 5); break;
                    case Kind.Hatch: p = Textured(1).With(textureIndex: IslandGround.TextureIndexFor(cube, IslandGround.TileDefinition(192, 48, 16, 16, diagonal, half))); break;
                    case Kind.Wall: p = Textured(5).With(texFlag: 3, textureIndex: IslandGround.TextureIndexFor(cube, IslandGround.TileDefinition(0, 128, 32, 32, diagonal, half))); col = true; break;
                    default: p = Flat(2, 12); break;
                }
                p = p.With(diagonal: diagonal, col: col);
                cube.SetPolygon(x, z, half, p.Raw);
            }
            // no water code, no water depth under the road
            for (var dz = 0; dz <= 1; dz++)
            for (var dx = 0; dx <= 1; dx++)
                foreach (var (c, vx, vz) in island.Owners(gx + dx, gz + dz))
                    if (c.HasIntensity) c.Intensity[vz * IslandCube.Vertices + vx] &= 0x0F;
        }
    }

    private static void MarkStartLine(TrackRoad r, int i, Dictionary<(int, int), Kind> kinds, RaceTrackReport report)
    {
        // a one-cell white row across the road at point i
        var cx = r.X[i]; var cz = r.Z[i]; var tx = r.Tx[i]; var tz = r.Tz[i];
        for (var gz = (int)Math.Floor(cz - 6); gz <= (int)Math.Ceiling(cz + 6); gz++)
        for (var gx = (int)Math.Floor(cx - 6); gx <= (int)Math.Ceiling(cx + 6); gx++)
        {
            var px = gx + 0.5 - cx; var pz = gz + 0.5 - cz;
            var f = px * tx + pz * tz; var u = -px * tz + pz * tx;
            if (Math.Abs(f) <= 0.6 && Math.Abs(u) <= r.CurbHalf && kinds.ContainsKey((gx, gz))) kinds[(gx, gz)] = Kind.Start;
        }
        report.StartLine.Add((cx, cz, r.H[i], tx, tz));
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // the gantry over the start line and the bridge-like gantry where the lap crosses itself

    // Retail gantry (the Desert track's start line): body 64 the beam, 65 and 66 its posts, all placed at one origin. Boxes are the
    // retail ZVs relative to that origin: (xMin, yMin, zMin, xMax, yMax, zMax).
    private static readonly (int Body, int[] Box)[] Gantry =
    {
        (64, new[] { -5519, 3141, -397, -141, 3990, -114 }),
        (65, new[] { -424, 28, -397, -141, 3141, -114 }),
        (66, new[] { -5519, -512, -397, -5236, 3141, -114 }),
    };
    private const double GantryCentreOffset = 2830;      // the origin is this far from the middle of the gantry, along its beam

    private static void PlaceGantry(IslandFile island, double cx, double cz, double height, double bisX, double bisZ, string what, RaceTrackReport report)
    {
        // the beam runs across the road, i.e. at right angles to the way the road runs
        var ex = -bisZ; var ez = bisX;
        var theta = Math.Atan2(-ez, ex);
        var beta = (int)Math.Round(theta / (2 * Math.PI) * 4096); beta = ((beta % 4096) + 4096) % 4096;
        var cos = Math.Cos(theta); var sin = Math.Sin(theta);
        var originX = (cx + ex * GantryCentreOffset / 512) * 512; var originZ = (cz + ez * GantryCentreOffset / 512) * 512;
        var y = (int)Math.Round(height);
        if (IslandDecors.Locate(island, originX, originZ) is not { } at) { report.Placed.Add($"{what}: off the island"); return; }
        foreach (var (body, box) in Gantry)
        {
            var d = IslandDecors.Blank(body, at.X, y, at.Z, beta);
            double minX = 1e18, maxX = -1e18, minZ = 1e18, maxZ = -1e18;
            foreach (var (bx, bz) in new[] { (box[0], box[2]), (box[3], box[2]), (box[0], box[5]), (box[3], box[5]) })
            {
                var wx = bx * cos + bz * sin; var wz = -bx * sin + bz * cos;
                minX = Math.Min(minX, wx); maxX = Math.Max(maxX, wx); minZ = Math.Min(minZ, wz); maxZ = Math.Max(maxZ, wz);
            }
            d.XMin = at.X + (int)Math.Floor(minX); d.XMax = at.X + (int)Math.Ceiling(maxX);
            d.ZMin = at.Z + (int)Math.Floor(minZ); d.ZMax = at.Z + (int)Math.Ceiling(maxZ);
            d.YMin = y + box[1]; d.YMax = y + box[4];
            if (at.Cube.Decors.Count < IslandDecors.MaxPerCube) at.Cube.Decors.Add(d);
        }
        report.Placed.Add($"{what}: gantry at cell ({cx:0.0}, {cz:0.0}), height {y}, turn {beta}");
    }

    private static void PlaceStructures(IslandFile island, TrackRoad main, List<Crossing> crossings, int startIndex, RaceTrackReport report)
    {
        if (startIndex >= 0 && report.StartLine.Count > 0)
        {
            var d = report.StartLine[0];
            PlaceGantry(island, d.X, d.Z, d.Y, d.DirX, d.DirZ, "start line", report);
        }
        foreach (var c in crossings) PlaceBridge(island, c.X, c.Z, main.H[c.I], c.BisX, c.BisZ, report);
    }

    // The retail overpass of the Desert track (bodies 68 the arched deck, 69 and 70 the abutments), boxes relative to the piece's own origin.
    private static readonly (int Body, int[] Box)[] ArchPieces =
    {
        (68, new[] { -3499, 2260, -853, -616, 3288, 340 }),
        (69, new[] { -4115, 683, -1043, -3499, 2692, 530 }),
        (70, new[] { -616, 85, -1043, 0, 2692, 530 }),
    };

    // Three arched decks side by side between the two abutments, across the two roads where the lap crosses itself. The ground is one
    // surface, so the roads meet at grade and the bridge stands over the junction: the car drives under it.
    private static void PlaceBridge(IslandFile island, double cx, double cz, double height, double bisX, double bisZ, RaceTrackReport report)
    {
        var ex = -bisZ; var ez = bisX;
        var theta = Math.Atan2(-ez, ex);
        var beta = (int)Math.Round(theta / (2 * Math.PI) * 4096); beta = ((beta % 4096) + 4096) % 4096;
        var cos = Math.Cos(theta); var sin = Math.Sin(theta);
        const double deckWidth = 2883, abutment = 616, decks = 3;
        var half = decks * deckWidth / 2;
        var pieces = new List<(int Body, int[] Box, double OriginX)>();
        for (var k = 0; k < decks; k++) pieces.Add((68, ArchPieces[0].Box, -half + deckWidth * (k + 0.5) + 2057.5));
        pieces.Add((69, ArchPieces[1].Box, -half - abutment + 4115));
        pieces.Add((70, ArchPieces[2].Box, half + abutment));
        var y = (int)Math.Round(height);
        var placed = 0;
        foreach (var (body, box, originLocal) in pieces)
        {
            var wx = (cx * 512 + ex * originLocal); var wz = (cz * 512 + ez * originLocal);
            if (IslandDecors.Locate(island, wx, wz) is not { } at || at.Cube.Decors.Count >= IslandDecors.MaxPerCube) continue;
            var d = IslandDecors.Blank(body, at.X, y, at.Z, beta);
            double minX = 1e18, maxX = -1e18, minZ = 1e18, maxZ = -1e18;
            foreach (var (bx, bz) in new[] { (box[0], box[2]), (box[3], box[2]), (box[0], box[5]), (box[3], box[5]) })
            {
                var rx = bx * cos + bz * sin; var rz = -bx * sin + bz * cos;
                minX = Math.Min(minX, rx); maxX = Math.Max(maxX, rx); minZ = Math.Min(minZ, rz); maxZ = Math.Max(maxZ, rz);
            }
            d.XMin = at.X + (int)Math.Floor(minX); d.XMax = at.X + (int)Math.Ceiling(maxX);
            d.ZMin = at.Z + (int)Math.Floor(minZ); d.ZMax = at.Z + (int)Math.Ceiling(maxZ);
            d.YMin = y + box[1]; d.YMax = y + box[4];
            at.Cube.Decors.Add(d);
            placed++;
        }
        report.Placed.Add($"crossing bridge: {placed} pieces (3 arched decks, 2 abutments) over cell ({cx:0.0}, {cz:0.0}), road height {y}, turn {beta}");
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // light

    private static void Relight(IslandFile island, RoadIndex index, RaceTrackOptions o)
    {
        var field = new IslandHeightField(island);
        var b = index.Bounds;
        foreach (var (cx, cz, cube) in IslandOps.CubeCells(island))
        {
            var options = BakeOptions.For(cube);
            _ = options;
        }
        var reference = island.Cubes.Values.First();
        var bake = BakeOptions.For(reference);
        for (var gz = Math.Max(0, b.Z0); gz <= Math.Min(Grid, b.Z1); gz++)
        for (var gx = Math.Max(0, b.X0); gx <= Math.Min(Grid, b.X1); gx++)
        {
            if (!island.HasVertex(gx, gz)) continue;
            var hits = index.Near(gx, gz, 14, 1);
            if (hits.Count == 0) continue;
            var target = IslandBake.Compute(field, new List<(double, double, double, double, double, double)>(), gx, gz, bake);
            var current = island.LightAt(gx, gz) ?? 15;
            island.SetLight(gx, gz, (int)Math.Round(current + (target - current) * 0.9));
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------
    // the island's heights and what is drawn, as flat arrays

    private sealed class Field
    {
        private readonly double[] heights = new double[(Grid + 1) * (Grid + 1)];
        private readonly bool[] has = new bool[(Grid + 1) * (Grid + 1)];
        private readonly bool[] drawn = new bool[Grid * Grid];

        public Field(IslandFile island)
        {
            Refresh(island);
            for (var cz = 0; cz < IslandFile.MapSize; cz++)
            for (var cx = 0; cx < IslandFile.MapSize; cx++)
            {
                if (island.CubeAt(cx, cz) is not { HasPolygons: true } cube) continue;
                for (var z = 0; z < 64; z++)
                for (var x = 0; x < 64; x++)
                {
                    var any = false;
                    for (var h = 0; h < 2; h++) { var p = new IslandPolygon(cube.Polygon(x, z, h)); if (p.TexFlag != 0 || p.PolyFlag != 0) any = true; }
                    drawn[(cz * 64 + z) * Grid + cx * 64 + x] = any;
                }
            }
        }

        public void Refresh(IslandFile island)
        {
            for (var gz = 0; gz <= Grid; gz++)
            for (var gx = 0; gx <= Grid; gx++)
            {
                var h = island.HeightAt(gx, gz);
                has[gz * (Grid + 1) + gx] = h.HasValue;
                heights[gz * (Grid + 1) + gx] = h ?? 0;
            }
        }

        public double VertexHeight(int gx, int gz) => heights[gz * (Grid + 1) + gx];

        public bool Drawn(double x, double z)
        {
            var gx = (int)Math.Floor(x); var gz = (int)Math.Floor(z);
            return (uint)gx < Grid && (uint)gz < Grid && drawn[gz * Grid + gx];
        }

        public double Height(double x, double z)
        {
            var gx = Math.Clamp((int)Math.Floor(x), 0, Grid - 1); var gz = Math.Clamp((int)Math.Floor(z), 0, Grid - 1);
            var fx = Math.Clamp(x - gx, 0, 1); var fz = Math.Clamp(z - gz, 0, 1);
            double H(int ix, int iz) => heights[iz * (Grid + 1) + ix];
            return H(gx, gz) * (1 - fx) * (1 - fz) + H(gx + 1, gz) * fx * (1 - fz) + H(gx, gz + 1) * (1 - fx) * fz + H(gx + 1, gz + 1) * fx * fz;
        }
    }

    private static double Sq(double v) => v * v;
}
