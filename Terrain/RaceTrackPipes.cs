using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// Pipes along a stretch of the road (RaceTrackPlan.Pipes): the Island of the Francos' refinery, the user's "lots of pipe and steam, and
// perhaps some oil dripping in places" (2026-10-07). From its first point to its last, every Every cells, a gantry over the road -- a
// pipe standing on the ground each side of it, up past the road, joined over it by a pipe as high as a car and more, and a pipe either
// side along the road to the next -- the race-track mode puffing steam from the tops of its pipes (steam=) and, with Drip, oil dripping
// from the pipe over the road onto it, where it lies as a slick a car skids on (drip=).
internal sealed class PipeRun
{
    public int From { get; set; }
    public int To { get; set; }
    public double Every { get; set; } = 10;
    public bool Drip { get; set; } = true;
}

internal static class RaceTrackPipes
{
    // the pipes' sizes (world units): the uprights' and the cross pipe's radius, the side pipes', how far out the uprights stand past the
    // road's rails (cells), the cross pipe's height over the deck, the uprights' over it, the side pipes' over it
    public const double Upright = 220, Cross = 200, Side = 130, Out = 1.3, CrossHigh = 2200, Top = 2900, SideHigh = 350;
    // colours: the shared ramps' starts (greys, reds), lit
    public const int Grey = 48, Red = 64;
    public const int SteamEvery = 900, DripEvery = 5000;
    // how far to one side of the road's middle the oil drips, as a share of the way to its rail
    public const double DripAcross = 0.45;

    // Places the gantries; `runs` with their points the road's. Returns the bodies made.
    public static List<int> Place(IslandFile island, TrackRoad r, IReadOnlyList<(int From, int To, PipeRun Run)> runs, RaceTrackOptions o, RaceTrackReport report)
    {
        var made = new List<int>();
        if (o.NewBodyBase < 0) { report.Notes.Add("WARNING: no place for the pipes' bodies was prepared -- no pipes"); return made; }
        int gantries = 0, skipped = 0;
        var why = new List<string>();
        foreach (var (from, to, run) in runs)
        {
            var n = r.Count;
            var span = ((to - from) % n + n) % n;
            var step = Math.Max(2, (int)Math.Round(run.Every / o.Spacing));
            var placed = 0;
            for (var k = step / 2; k <= span - step / 2; k += step)
            {
                var i = (from + k) % n;
                double x = r.X[i], z = r.Z[i], y = r.H[i];
                var a = (i + n - 1) % n; var b = (i + 1) % n;
                var along = new Vector3((float)(r.X[b] - r.X[a]), 0, (float)(r.Z[b] - r.Z[a]));
                if (along.Length() < 1e-6) continue;
                along = Vector3.Normalize(along);
                var across = new Vector3(-along.Z, 0, along.X);
                var half = (r.Raised is { } up && up[i] ? r.RaisedHalfs is { } halfs && i < halfs.Length ? halfs[i] : o.RaisedHalfWidth : r.CurbHalf) + Out;
                // the uprights' feet on the ground under them; none where another part of the lap -- its deck, its rails and a little more --
                // is in an upright's way up, or passes over the road in the cross pipe's (a road under the deck between the uprights is clear)
                var feet = new double[2];
                for (var s = 0; s < 2; s++)
                    feet[s] = IslandOps.Altitude(island, (x + across.X * (s == 0 ? -1 : 1) * half) * 512, (z + across.Z * (s == 0 ? -1 : 1) * half) * 512) ?? 0;
                var blocked = false;
                for (var j = 0; j < n && !blocked; j++)
                {
                    var sep = Math.Min(Math.Abs(j - i), n - Math.Abs(j - i));
                    if (sep < 30 || r.H[j] > y + Top + 1400) continue;
                    var other = (r.Raised is { } on && on[j] ? r.RaisedHalfs is { } wide && j < wide.Length ? wide[j] : o.RaisedHalfWidth : r.CurbHalf) + 0.6;
                    double dx = r.X[j] - x, dz = r.Z[j] - z;
                    var a0 = dx * across.X + dz * across.Z;
                    for (var s = 0; s < 2 && !blocked; s++)
                    {
                        var sign = s == 0 ? -1 : 1;
                        double ex = dx - across.X * sign * half, ez = dz - across.Z * sign * half;
                        if (Math.Sqrt(ex * ex + ez * ez) < other && r.H[j] > feet[s] - 400) blocked = true;
                    }
                    var off = Math.Abs(dx * along.X + dz * along.Z);
                    if (Math.Abs(a0) <= half && off < other && r.H[j] > y - 300) blocked = true;
                }
                if (blocked || feet.Any(f => f > y + 600)) { skipped++; why.Add($"({x:0.#}, {z:0.#}): {(blocked ? "the lap" : "the ground")}"); continue; }
                var len = (float)(run.Every * 512);
                var body = Gantry(along, across, half * 512, feet[0] - y, feet[1] - y, len);
                if (IslandDecors.Locate(island, x * 512, z * 512) is not { } at || at.Cube.Decors.Count >= IslandDecors.MaxPerCube) { skipped++; why.Add($"({x:0.#}, {z:0.#}): its cube full"); continue; }
                var (cube, lx, lz) = at;
                var d = IslandDecors.Blank(o.NewBodyBase + report.NewBodies.Count, lx, (int)Math.Round(y), lz, 0);
                report.NewBodies.Add(body);
                // (its box the cross pipe's only, over the road: the uprights and the side pipes stand outside the rails, where no car goes)
                var reach = half * 512 + Upright;
                int bx0 = (int)Math.Round(lx - Math.Abs(across.X) * reach - Cross * 2), bx1 = (int)Math.Round(lx + Math.Abs(across.X) * reach + Cross * 2);
                int bz0 = (int)Math.Round(lz - Math.Abs(across.Z) * reach - Cross * 2), bz1 = (int)Math.Round(lz + Math.Abs(across.Z) * reach + Cross * 2);
                d.XMin = bx0; d.XMax = bx1; d.ZMin = bz0; d.ZMax = bz1;
                d.YMin = (int)Math.Round(y + CrossHigh - Cross); d.YMax = (int)Math.Round(y + Top);
                cube.Decors.Add(d);
                made.Add(d.Body);
                gantries++;
                // steam from the uprights' tops; oil dripping from the cross pipe onto the road's middle
                for (var s = 0; s < 2; s++)
                {
                    var sign = s == 0 ? -1 : 1;
                    report.Steam.Add(new[] { (int)Math.Round((x + across.X * sign * half) * 512), (int)Math.Round(y + Top + 80), (int)Math.Round((z + across.Z * sign * half) * 512), SteamEvery });
                }
                // (in places: every other gantry of the stretch drips, onto one side of the road and then the other -- a car can keep clear)
                if (run.Drip && placed++ % 2 == 0)
                {
                    var side = (report.Drips.Count % 2 == 0 ? 1 : -1) * (half - Out) * DripAcross;
                    report.Drips.Add(new[] { (int)Math.Round((x + across.X * side) * 512), (int)Math.Round(y + CrossHigh - Cross - 40),
                        (int)Math.Round((z + across.Z * side) * 512), (int)Math.Round(y), DripEvery });
                }
            }
        }
        report.Notes.Add($"pipes: {gantries} gantries over the road ({skipped} left out: another part of the lap in an upright's way, or the ground over the road), " +
                         $"{report.Steam.Count} steam vents, {report.Drips.Count} oil drips{(why.Count > 0 ? " -- left out: " + string.Join(", ", why) : "")}");
        return made;
    }

    // One gantry, from its origin on the road's middle at the deck: `along` the road's way, `across` to its left; the uprights at `half`
    // either side (world units) from their feet (`down0`, `down1`: the ground under them, from the deck) to Top; the cross pipe between
    // them at CrossHigh; a side pipe along each, `len` long, at SideHigh outside the rails; a red band round each upright under its vent.
    private static byte[] Gantry(Vector3 along, Vector3 across, double half, double down0, double down1, float len)
    {
        var pts = new List<Vector3>();
        var faces = new List<Face>();
        void Cylinder(Vector3 a, Vector3 b, float radius, int colour, int sides = 8)
        {
            var axis = Vector3.Normalize(b - a);
            var u = Vector3.Normalize(Math.Abs(axis.Y) > 0.9 ? Vector3.Cross(axis, Vector3.UnitX) : Vector3.Cross(axis, Vector3.UnitY));
            var v = Vector3.Cross(axis, u);
            var ring0 = new int[sides]; var ring1 = new int[sides];
            for (var k = 0; k < sides; k++)
            {
                var t = 2 * Math.PI * k / sides;
                var off = (u * (float)Math.Cos(t) + v * (float)Math.Sin(t)) * radius;
                ring0[k] = pts.Count; pts.Add(a + off);
                ring1[k] = pts.Count; pts.Add(b + off);
            }
            for (var k = 0; k < sides; k++)
            {
                var k2 = (k + 1) % sides;
                int p0 = ring0[k], p1 = ring0[k2], p2 = ring1[k2], p3 = ring1[k];
                var mid = (pts[p0] + pts[p1] + pts[p2] + pts[p3]) / 4;
                var axisPoint = a + axis * Vector3.Dot(mid - a, axis);
                var outward = mid - axisPoint;
                var nrm = Vector3.Cross(pts[p1] - pts[p0], pts[p2] - pts[p0]);
                faces.Add(Vector3.Dot(nrm, outward) >= 0 ? new Face(new[] { p0, p1, p2, p3 }, colour) : new Face(new[] { p3, p2, p1, p0 }, colour));
            }
            // its ends: a cap each
            foreach (var (ring, end, sign) in new[] { (ring0, a, -1f), (ring1, b, 1f) })
            {
                var c = pts.Count; pts.Add(end);
                for (var k = 0; k < sides; k++)
                {
                    int p0 = ring[k], p1 = ring[(k + 1) % sides];
                    var nrm = Vector3.Cross(pts[p0] - pts[c], pts[p1] - pts[c]);
                    faces.Add(Vector3.Dot(nrm, axis * sign) >= 0 ? new Face(new[] { c, p0, p1 }, colour) : new Face(new[] { p1, p0, c }, colour));
                }
            }
        }
        var up = Vector3.UnitY;
        var left = across * (float)half; var right = -across * (float)half;
        Cylinder(left + up * (float)down0, left + up * (float)(Top - 260), (float)Upright, Grey);
        Cylinder(right + up * (float)down1, right + up * (float)(Top - 260), (float)Upright, Grey);
        // the vents: a red band and a wider cap at each top
        Cylinder(left + up * (float)(Top - 260), left + up * (float)Top, (float)(Upright * 1.35), Red);
        Cylinder(right + up * (float)(Top - 260), right + up * (float)Top, (float)(Upright * 1.35), Red);
        // the cross pipe over the road (through the uprights) and the side pipes along it outside the rails
        Cylinder(left * 1.08f + up * (float)CrossHigh, right * 1.08f + up * (float)CrossHigh, (float)Cross, Grey);
        foreach (var side in new[] { across * (float)(half - 420), -across * (float)(half - 420) })
            Cylinder(side + up * (float)SideHigh - along * (len / 2), side + up * (float)SideHigh + along * (len / 2), (float)Side, Grey, 6);
        var body = new Body
        {
            Game = 2, Static = true, Lit = true, Header = new byte[96],
            Vertices = pts,
            Bones = new List<Bone> { new(0, pts.Count, 0, -1, new byte[8]) },
            Faces = faces,
        };
        return body.Write();
    }
}
