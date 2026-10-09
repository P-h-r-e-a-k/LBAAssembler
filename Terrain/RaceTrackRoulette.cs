using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// A giant roulette wheel the lap runs through (RaceTrackPlan.Roulette): Otringal's, by the casino -- the user, 2026-10-09: "let's have the
// jump land into a giant rotating roulette wheel complete with colours, numbers, and a giant white ball that rolls around for cars to
// avoid. Cars land in the roulette wheel from the jump, and drive around in a circle and out a hole in the bottom". A bowl on legs, its
// floor a cone down to a hole in its middle; the lap's lane round it is the raised road's (its rails the engine's, banked as the cone is:
// tools/RaceTrackPlan/otringal_design.py), its walls here. The wheel's head -- the 37 pockets, red, black and the green zero, in the order of
// a European wheel, their numbers in white -- turns under the lane, and a white ball rolls along it to and fro: both decors, so they are
// drawn from wherever the wheel is seen, the race-track mode turning the one and moving the other (RACEMOD.CPP roulette=). Nothing here
// is touched by anything: the lane is the engine's floor, the ball the race-track mode's own.
internal sealed class RouletteRun
{
    // its middle (the plan's cells), the bowl's wall (its inner face) and the hole (cells from the middle)
    public double[] Centre { get; set; } = Array.Empty<double>();
    public double Rim { get; set; } = 7.25;
    public double Hole { get; set; } = 2.2;
    // the floor at the hole's edge, and its rise a cell outwards; the wall's height over the floor at the rim
    public double HoleY { get; set; } = 9400;
    public double Slope { get; set; } = 170;
    public double Wall { get; set; } = 250;
    // the lane round it (the plan's points), where the ball rolls along it, how fast the head turns (degrees a second)
    public int From { get; set; }
    public int To { get; set; }
    public int BallFrom { get; set; }
    public int BallTo { get; set; }
    public double Spin { get; set; } = 24;
    // its legs round it (degrees from east towards south: clear of the road under it)
    public double[]? Legs { get; set; }
}

// The wheel as built: its middle (island world units), the cube its decors are in, the bodies (the island's) of its turning head and of
// its ball, the lap's points the ball rolls between, how fast the head turns and the ball's radius; its floor's height at the hole's edge
// and rise a cell outwards, the hole's radius and the bowl's (cells), how far its underside is under its floor (the camera stays under it
// while the car is on the road under the wheel).
internal sealed record RouletteSpot(double X, double Z, int CubeX, int CubeZ, int[] Turning, int Ball, int BallFrom, int BallTo, double Spin, int BallRadius,
    double HoleY, double Slope, double Hole, double Outer, double Under);

internal static class RaceTrackRoulette
{
    public const int BallRadius = 360;
    // the bowl: the wall's thickness out from its inner face, the floor's under it; the pockets' inner edge past the hole, and the ring
    // their numbers are on, in from the wall (cells); the numbers' pixels (world units)
    private const double WallOut = 0.3, Thick = 300, PocketsPast = 0.6, NumbersIn = 1.3, NumberIn = 0.65, Pixel = 60;
    private const double LaneWall = 180, LaneWallThick = 40;
    private const int Around = 48, Bodies = 5;
    // the legs: how far in from the wall (cells), their half width; where they stand round it unless the plan says
    private const double LegIn = 1.7, LegHalf = 170;
    private static readonly double[] LegAngles = { 80, 200, 320 };
    // the island palettes' shared ramps: the pockets' red, black and green, gold, the woods, white
    private const int Red = 71, Black = 49, Green = 134, Gold = 105, GoldDark = 101, Wood = 25, WoodLight = 27, WoodDark = 20, Skirt = 23, White = 62, HoleDark = 48;
    // a European wheel's pockets round it, and its reds
    private static readonly int[] Order = { 0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26 };
    private static readonly HashSet<int> Reds = new() { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
    // the digits, three pixels by five, row by row from the top
    private static readonly string[] Font =
    {
        "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
        "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111",
    };
    private const int NoBoxTop = -32000;

    private sealed class Mesh
    {
        public readonly List<Vector3> Points = new();
        public readonly List<Face> Faces = new();
        public int P(Vector3 v) { Points.Add(v); return Points.Count - 1; }
        public void Quad(int a, int b, int c, int d, int colour, Vector3 outward)
        {
            var n = Vector3.Cross(Points[b] - Points[a], Points[c] - Points[a]);
            Faces.Add(Vector3.Dot(n, outward) >= 0 ? new Face(new[] { a, b, c, d }, colour) : new Face(new[] { d, c, b, a }, colour));
        }
        public byte[] Write() => RaceTrackPipes.Write(Points, Faces, lit: false);
    }

    // Builds the wheel at (cx, cz) (island cells): the ground under it lowered to the town's square, what stood there taken away, and its
    // decors added. `kept`: decors the plan keeps. Null when its cube is full.
    public static RouletteSpot? Place(IslandFile island, TrackRoad r, RouletteRun run, double cx, double cz, int from, int to, int ballFrom, int ballTo,
        RaceTrackOptions o, RaceTrackReport report, Func<int, bool> kept)
    {
        var rim = run.Rim; var outer = rim + WallOut;
        double Pockets = run.Hole + PocketsPast, Numbers0 = rim - NumbersIn, NumberMid = rim - NumberIn, LegAt = rim - LegIn;
        double Floor(double cells) => run.Slope * (cells - run.Hole);               // the cone over the hole's edge (world units)
        double Under(double cells) => Floor(cells) - Thick;
        // the ground under it: down to the square's level (the road under the wheel runs on it), rising steeply outside it
        var square = run.HoleY - 1800;
        var lowered = 0;
        for (var gz = (int)Math.Floor(cz - outer - 4); gz <= (int)Math.Ceiling(cz + outer + 4); gz++)
        for (var gx = (int)Math.Floor(cx - outer - 4); gx <= (int)Math.Ceiling(cx + outer + 4); gx++)
        {
            var d = Math.Sqrt((gx - cx) * (gx - cx) + (gz - cz) * (gz - cz));
            var limit = d <= outer + 0.5 ? square : square + (d - outer - 0.5) * 1500;
            if (island.HeightAt(gx, gz) is not { } h || h <= limit) continue;
            island.SetHeight(gx, gz, (int)Math.Round(limit));
            lowered++;
        }
        // what stood there (not what the plan keeps)
        var removed = 0;
        foreach (var (qx, qz, cube) in IslandOps.CubeCells(island))
            foreach (var d in cube.Decors.ToList())
            {
                var body = d.Body & 0xFFFF;
                if (kept(body)) continue;
                double x0 = (qx * (double)IslandFile.CubeSize + Math.Min(d.XMin, d.X)) / 512, x1 = (qx * (double)IslandFile.CubeSize + Math.Max(d.XMax, d.X)) / 512;
                double z0 = (qz * (double)IslandFile.CubeSize + Math.Min(d.ZMin, d.Z)) / 512, z1 = (qz * (double)IslandFile.CubeSize + Math.Max(d.ZMax, d.Z)) / 512;
                double nx = Math.Clamp(cx, x0, x1), nz = Math.Clamp(cz, z0, z1);
                if ((nx - cx) * (nx - cx) + (nz - cz) * (nz - cz) > (outer + 0.3) * (outer + 0.3)) continue;
                if (Math.Max(d.YMax, d.Y + 1) <= square + 50) continue;
                cube.Decors.Remove(d);
                report.Removed.Add((qx, qz, body, "roulette wheel"));
                removed++;
            }
        if (IslandDecors.Locate(island, cx * 512, cz * 512) is not { } home) return null;
        var homeX = (int)Math.Floor(cx * 512 / IslandFile.CubeSize); var homeZ = (int)Math.Floor(cz * 512 / IslandFile.CubeSize);
        if (home.Cube.Decors.Count + Bodies + 6 > IslandDecors.MaxPerCube) { report.Notes.Add("WARNING: the roulette wheel: no room for it in its cube"); return null; }

        var made = new List<int>();
        int Add(byte[] body, double wx, double wy, double wz, double ext, double bottom)
        {
            if (IslandDecors.Locate(island, wx, wz) is not { } at || at.Cube.Decors.Count >= IslandDecors.MaxPerCube) return -1;
            var number = o.NewBodyBase + report.NewBodies.Count;
            var d = IslandDecors.Blank(number, at.X, (int)Math.Round(wy), at.Z, 0);
            d.XMin = at.X - (int)ext; d.XMax = at.X + (int)ext; d.ZMin = at.Z - (int)ext; d.ZMax = at.Z + (int)ext;
            d.YMin = (int)Math.Floor(bottom); d.YMax = NoBoxTop;
            at.Cube.Decors.Add(d);
            report.NewBodies.Add(body);
            made.Add(number);
            return number;
        }
        double wcx = cx * 512, wcz = cz * 512, y0 = run.HoleY, ext = outer * 512 + 64, bottom = y0 + Under(run.Hole);
        static Vector3 At(double cells, double angle, double y) => new((float)(cells * 512 * Math.Cos(angle)), (float)y, (float)(cells * 512 * Math.Sin(angle)));
        static Vector3 Radial(double angle) => new((float)Math.Cos(angle), 0, (float)Math.Sin(angle));

        // the bowl: its wall round the rim (gold on top), its wooden side, its underside, and the hole's own wall
        var bowl = new Mesh();
        int[] Ring(Mesh m, double cells, double y) => Enumerable.Range(0, Around).Select(k => m.P(At(cells, 2 * Math.PI * k / Around, y))).ToArray();
        int[] wallFoot = Ring(bowl, rim, Floor(rim) - 30), wallTop = Ring(bowl, rim, Floor(rim) + run.Wall), outTop = Ring(bowl, outer, Floor(rim) + run.Wall),
            outFoot = Ring(bowl, outer, Under(outer)), holeFoot = Ring(bowl, run.Hole, Under(run.Hole)), holeTop = Ring(bowl, run.Hole, Floor(run.Hole));
        for (var k = 0; k < Around; k++)
        {
            int j = (k + 1) % Around; var radial = Radial(2 * Math.PI * (k + 0.5) / Around);
            bowl.Quad(wallFoot[k], wallFoot[j], wallTop[j], wallTop[k], k % 2 == 0 ? WoodLight : Wood, -radial);
            bowl.Quad(wallTop[k], wallTop[j], outTop[j], outTop[k], Gold, Vector3.UnitY);
            bowl.Quad(outTop[k], outTop[j], outFoot[j], outFoot[k], Skirt, radial);
            bowl.Quad(outFoot[k], outFoot[j], holeFoot[j], holeFoot[k], WoodDark, radial * (float)(run.Slope / 512) - Vector3.UnitY);
            bowl.Quad(holeFoot[k], holeFoot[j], holeTop[j], holeTop[k], HoleDark, -radial);
        }
        Add(bowl.Write(), wcx, y0, wcz, ext, bottom);

        // the floor inside the pockets, round the hole: wood, in segments of two tones, a gold ring round the hole's edge
        var inner = new Mesh();
        {
            int[] a = Ring(inner, run.Hole, Floor(run.Hole) + 4), b = Ring(inner, run.Hole + 0.14, Floor(run.Hole + 0.14) + 4),
                  c = Ring(inner, run.Hole + 0.14, Floor(run.Hole + 0.14) + 2), e = Ring(inner, Pockets, Floor(Pockets) + 2);
            for (var k = 0; k < Around; k++)
            {
                var j = (k + 1) % Around;
                inner.Quad(a[k], a[j], b[j], b[k], Gold, Vector3.UnitY);
                inner.Quad(c[k], c[j], e[j], e[k], k / 3 % 2 == 0 ? Wood : WoodLight, Vector3.UnitY);
            }
        }
        Add(inner.Write(), wcx, y0, wcz, ext, bottom);

        // the head: the pockets, each its colour from the floor round the hole to the wall, gold frets between them and gold rings round them,
        // its number in white on its outer part -- in Bodies pieces, each a few pockets (the engine's limit on a body's points)
        var turning = new List<int>();
        var pocket = 2 * Math.PI / Order.Length;
        for (var piece = 0; piece < Bodies; piece++)
        {
            var head = new Mesh();
            int k0 = piece * Order.Length / Bodies, k1 = (piece + 1) * Order.Length / Bodies;
            for (var k = k0; k < k1; k++)
            {
                double a0 = k * pocket, a1 = (k + 1) * pocket, am = (a0 + a1) / 2;
                var n = Order[k];
                var colour = n == 0 ? Green : Reds.Contains(n) ? Red : Black;
                void Band(double r0, double r1, double lift, int c) =>
                    head.Quad(head.P(At(r0, a0, Floor(r0) + lift)), head.P(At(r0, a1, Floor(r0) + lift)), head.P(At(r1, a1, Floor(r1) + lift)), head.P(At(r1, a0, Floor(r1) + lift)), c, Vector3.UnitY);
                Band(Pockets, rim - 0.03, 6, colour);
                Band(Pockets, Pockets + 0.1, 10, Gold);
                Band(Numbers0 - 0.05, Numbers0 + 0.05, 10, Gold);
                // (the fret on the pocket's first edge: a strip 30 wide)
                var side = new Vector3((float)-Math.Sin(a0), 0, (float)Math.Cos(a0)) * 15;
                Vector3 F(double cells, float s) => At(cells, a0, Floor(cells) + 12) + side * s;
                head.Quad(head.P(F(Pockets, -1)), head.P(F(Pockets, 1)), head.P(F(rim - 0.03, 1)), head.P(F(rim - 0.03, -1)), GoldDark, Vector3.UnitY);
                // (its number: upright seen from outside the wheel -- its top towards the middle)
                var digits = n.ToString();
                var width = digits.Length * 3 + (digits.Length - 1);
                Vector3 up = -Radial(am), right = new((float)Math.Sin(am), 0, (float)-Math.Cos(am));
                var mid = Radial(am) * (float)(NumberMid * 512);
                Vector3 Pix(double u, double v)
                {
                    var p = mid + right * (float)((u - width / 2.0) * Pixel) + up * (float)((2.5 - v) * Pixel);
                    var cells = Math.Sqrt(p.X * p.X + p.Z * p.Z) / 512;
                    return new Vector3(p.X, (float)(Floor(cells) + 16), p.Z);
                }
                for (var di = 0; di < digits.Length; di++)
                    foreach (var (u0, v0, u1, v1) in Runs(Font[digits[di] - '0']))
                    {
                        var ox = di * 4;
                        head.Quad(head.P(Pix(ox + u0, v0)), head.P(Pix(ox + u1, v0)), head.P(Pix(ox + u1, v1)), head.P(Pix(ox + u0, v1)), White, Vector3.UnitY);
                    }
            }
            var number = Add(head.Write(), wcx, y0, wcz, ext, bottom);
            if (number >= 0) turning.Add(number);
        }

        // the lane's walls: along its inner rail all the way, and along its outer rail where it leaves the bowl's wall on the dive
        var walls = new Mesh();
        var n_ = r.Count;
        var lane = new List<int>();
        for (var k = from; ; k = (k + 1) % n_) { lane.Add(k); if (k == to || lane.Count > n_) break; }
        Vector3 Lane(int k) => new((float)(r.X[k] * 512 - wcx), (float)(r.H[k] - y0), (float)(r.Z[k] * 512 - wcz));
        Vector3 Across(int k) => new((float)-r.Tz[k], (float)(r.RoadBank?[k] ?? 0), (float)r.Tx[k]);
        double Half(int k) => (r.RaisedHalfs?[k] ?? o.RaisedHalfWidth) * 512;
        static double Flat(Vector3 v) => Math.Sqrt(v.X * v.X + v.Z * v.Z);
        void Wall(IReadOnlyList<int> ks, float side)
        {
            if (ks.Count < 2) return;
            var rows = ks.Select(k =>
            {
                var a = Across(k); var flat = Vector3.Normalize(new Vector3(a.X, 0, a.Z));
                var foot = Lane(k) + a * (float)(side * Half(k));
                return (In: walls.P(foot - Vector3.UnitY * 20), InTop: walls.P(foot + Vector3.UnitY * (float)LaneWall),
                        OutTop: walls.P(foot + flat * (float)(side * LaneWallThick) + Vector3.UnitY * (float)LaneWall), Out: walls.P(foot + flat * (float)(side * LaneWallThick) - Vector3.UnitY * 20),
                        Facing: flat * side);
            }).ToList();
            for (var i = 0; i + 1 < rows.Count; i++)
            {
                var (p, q) = (rows[i], rows[i + 1]);
                // (none over the hole: the lane narrows into it, its end carried over it)
                if (Math.Min(Flat(walls.Points[p.In]), Flat(walls.Points[q.In])) < run.Hole * 512 + 30) continue;
                walls.Quad(p.In, q.In, q.InTop, p.InTop, GoldDark, -p.Facing);
                walls.Quad(p.InTop, q.InTop, q.OutTop, p.OutTop, Gold, Vector3.UnitY);
                walls.Quad(p.OutTop, q.OutTop, q.Out, p.Out, GoldDark, p.Facing);
            }
        }
        // (inward: the across that points at the wheel's middle -- the lap's left is outwards round the bowl)
        var inward = Vector3.Dot(Across(lane[lane.Count / 4]), -Lane(lane[lane.Count / 4])) > 0 ? 1f : -1f;
        Wall(lane, inward);
        var leaves = lane.FindIndex(k => { var p = Lane(k) + Across(k) * (float)(-inward * Half(k)); return Math.Sqrt(p.X * p.X + p.Z * p.Z) / 512 < rim - 0.25; });
        if (leaves >= 0) Wall(lane.Skip(Math.Max(0, leaves - 2)).ToList(), -inward);
        Add(walls.Write(), wcx, y0, wcz, ext, bottom);

        // the ball: a white sphere, where it starts (the race-track mode rolls it along the lane)
        var ball = new Body { Game = 2, Static = true, Lit = false, Header = new byte[96], Vertices = new List<Vector3> { Vector3.Zero },
            Bones = new List<Bone> { new(0, 1, 0, -1, new byte[8]) } };
        ball.Spheres.Add(new BodySphere(0, BallRadius, White));
        var ballAt = Lane(ballFrom);
        var ballBody = Add(ball.Write(), wcx + ballAt.X, y0 + ballAt.Y + BallRadius, wcz + ballAt.Z, BallRadius + 64, y0 + ballAt.Y);

        // the legs: square columns from the ground to the underside, a cap under it
        var legs = 0;
        foreach (var deg in run.Legs is { Length: > 0 } planned ? planned : LegAngles)
        {
            var a = deg * Math.PI / 180;
            double lx = wcx + LegAt * 512 * Math.Cos(a), lz = wcz + LegAt * 512 * Math.Sin(a);
            var ground = IslandOps.Altitude(island, lx, lz) ?? square;
            var top = y0 + Under(LegAt) - ground;
            var leg = new Mesh();
            var u = new Vector3((float)Math.Cos(a), 0, (float)Math.Sin(a)); var v = new Vector3(-u.Z, 0, u.X);
            RaceTrackPipes.Box(leg.Points, leg.Faces, Vector3.Zero, u * (float)LegHalf, v * (float)LegHalf, -60, (float)(top - 260), Gold, GoldDark, GoldDark);
            RaceTrackPipes.Box(leg.Points, leg.Faces, Vector3.Zero, u * (float)(LegHalf * 1.8), v * (float)(LegHalf * 1.8), (float)(top - 260), (float)(top + 40), WoodLight, Wood, Wood);
            if (Add(leg.Write(), lx, ground, lz, LegHalf * 2, ground - 60) >= 0) legs++;
        }

        report.Notes.Add($"a roulette wheel at cell ({cx:0.0}, {cz:0.0}): its wall {rim:0.##} cells out, its floor {run.HoleY:0} at the hole ({run.Hole:0.#} cells) rising {run.Slope:0} a cell; " +
                         $"{turning.Count} turning pieces of its head, its ball, {legs} legs, {made.Count} decors in all; {lowered} points of the ground lowered under it, {removed} decors taken away");
        return new RouletteSpot(wcx, wcz, homeX, homeZ, turning.ToArray(), ballBody, ballFrom, ballTo, run.Spin, BallRadius, run.HoleY, run.Slope, run.Hole, outer, Thick);
    }

    // A digit's pixels as rectangles: each row's runs, runs alike in rows one under another joined (pixel corners: left, top, right, bottom).
    private static List<(int U0, int V0, int U1, int V1)> Runs(string rows)
    {
        var runs = new List<(int U0, int V0, int U1, int V1)>();
        for (var v = 0; v < 5; v++)
        {
            var u = 0;
            while (u < 3)
            {
                if (rows[v * 3 + u] != '1') { u++; continue; }
                var e = u;
                while (e < 3 && rows[v * 3 + e] == '1') e++;
                var joined = runs.FindIndex(q => q.V1 == v && q.U0 == u && q.U1 == e);
                if (joined >= 0) runs[joined] = runs[joined] with { V1 = v + 1 };
                else runs.Add((u, v, e, v + 1));
                u = e;
            }
        }
        return runs;
    }
}
