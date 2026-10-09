using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// Gas monsters along a stretch of the lap over Zeelich's gas (RaceTrackPlan.GasMonsters; RACEMOD.CPP gasmonster=): Otringal's, at its docks
// and round the islets where Baldino's plane lies crashed among the rocks -- the user, 2026-10-09: "let's have gas monsters coming out of
// the gas and attempting to bite cars, any bitten cars should be stunned for a couple of seconds as though they've been hit then be able to
// continue". Each is a serpent: a long neck that rises out of the gas beside the road, sways, and strikes its head down onto the road's near
// half -- a car on its far half goes by -- its jaws snapping shut there. Two actors of the mushroom's entity (RaceTrackOil.Entity: a fixed
// object of one bone, as these are), its bodies Neck and HeadOpen or HeadShut, out of sight until the race-track mode raises them.
internal sealed class GasMonsterRun
{
    public int From { get; set; }
    public int To { get; set; }
    // one every so many cells along the stretch, where there is gas beside the road; how far out past the rail it rises (cells)
    public double Every { get; set; } = 9;
    public double Out { get; set; } = 3;
    public int Most { get; set; } = 5;
}

// A monster as placed: where it rises out of the gas, and where on the road it bites (island cells, the bite's height the road's).
internal sealed record GasMonsterSpot(double X, double Z, double BiteX, double BiteZ, double BiteY);

internal static class RaceTrackGasMonster
{
    public const int Neck = 3, HeadOpen = 4, HeadShut = 5;
    // the neck's length (RACEMOD.CPP RACE_MONSTER_NECK): the most of it under the gas
    public const double NeckLength = 7000;
    // how far across from the road's middle towards the monster it bites, as a share of the road's half width
    private const double BiteAcross = 0.45;
    private const double GasLevel = 60;
    // the least distance between two monsters (cells)
    private const double Apart = 9;

    // the colours (the islands' shared ramps): a sickly teal skin, a yellow-green belly, purple spines, the mouth's red, teeth, eyes
    private const int Skin = 147, Belly = 116, Spine = 210, Mouth = 68, Teeth = 62, Eye = 103, Pupil = 48;
    private static readonly Vector3 Light = Vector3.Normalize(new Vector3(-0.35f, 0.85f, -0.4f));

    private sealed class Mesh
    {
        public readonly List<Vector3> Points = new();
        public readonly List<Face> Faces = new();
        public readonly List<BodySphere> Spheres = new();
        public int P(Vector3 v) { Points.Add(v); return Points.Count - 1; }
        // a face its colour shaded by its slope to the light (the body is unlit: its shading is baked in), a ramp's few steps
        public void Face(int[] ids, int colour, Vector3 outward, int steps = 4)
        {
            var n = Vector3.Cross(Points[ids[1]] - Points[ids[0]], Points[ids[2]] - Points[ids[0]]);
            if (Vector3.Dot(n, outward) < 0) { ids = ids.Reverse().ToArray(); n = -n; }
            var lit = n.LengthSquared() > 1e-6 ? Vector3.Dot(Vector3.Normalize(n), Light) : 0;
            Faces.Add(new Face(ids, colour + (int)Math.Round(Math.Clamp((lit + 0.2) / 1.2, 0, 1) * steps)));
        }
        public byte[] Write(byte[] header)
        {
            var body = new Body { Game = 2, Lit = false, Static = true, Header = header };
            body.Bones.Add(new Bone(0, Points.Count, 0, -1, new byte[8]));
            body.Vertices.AddRange(Points);
            body.SetWorld(Points.ToArray());
            body.Faces.AddRange(Faces);
            body.Spheres.AddRange(Spheres);
            body.Validate();
            return body.Write();
        }
    }

    // The neck: a tapering tube from its foot (the origin) straight up NeckLength (its top, where the head sits, over the origin), bulging a
    // little forwards (+Z) between; a row of purple spines down its back.
    public static byte[] BuildNeck(byte[] header)
    {
        var m = new Mesh();
        const int sides = 8, stations = 14;
        var rings = new int[stations][];
        Vector3 Centre(double s) => new(0, (float)(s * NeckLength), (float)(260 * Math.Sin(Math.PI * s)));
        double Radius(double s) => 330 * (1 - s) + 175 * s;
        for (var i = 0; i < stations; i++)
        {
            var s = i / (double)(stations - 1);
            rings[i] = new int[sides];
            for (var k = 0; k < sides; k++)
            {
                var a = 2 * Math.PI * k / sides;
                rings[i][k] = m.P(Centre(s) + new Vector3((float)(Radius(s) * Math.Sin(a)), 0, (float)(Radius(s) * Math.Cos(a))));
            }
        }
        for (var i = 0; i + 1 < stations; i++)
            for (var k = 0; k < sides; k++)
            {
                var j = (k + 1) % sides;
                var mid = (m.Points[rings[i][k]] + m.Points[rings[i][j]] + m.Points[rings[i + 1][k]] + m.Points[rings[i + 1][j]]) / 4;
                var axis = Centre((i + 0.5) / (stations - 1));
                // (the belly forwards, +Z, lighter)
                var belly = Math.Cos(2 * Math.PI * (k + 0.5) / sides) > 0.5;
                m.Face(new[] { rings[i][k], rings[i][j], rings[i + 1][j], rings[i + 1][k] }, belly ? Belly : Skin, mid - axis);
            }
        var top = m.P(Centre(1) + new Vector3(0, 40, 0));
        for (var k = 0; k < sides; k++) m.Face(new[] { rings[stations - 1][k], rings[stations - 1][(k + 1) % sides], top }, Skin, Vector3.UnitY);
        // the spines: a fin on its back (-Z) at every other ring, both its faces
        for (var i = 2; i < stations - 1; i += 2)
        {
            var s = i / (double)(stations - 1);
            var c = Centre(s);
            var r = Radius(s);
            int a = m.P(c + new Vector3(0, -160, (float)-r)), b = m.P(c + new Vector3(0, 220, (float)-r)), tip = m.P(c + new Vector3(0, -40, (float)(-r - 260)));
            m.Face(new[] { a, b, tip }, Spine, Vector3.UnitX, 3);
            m.Face(new[] { a, b, tip }, Spine, -Vector3.UnitX, 3);
        }
        return m.Write(header);
    }

    // The head: from the neck's top (the origin) its snout forwards along +Z; a skull over its jaw line, a lower jaw hinged at its back
    // (open: turned down `gape` radians), teeth along both jaws, yellow eyes on the skull's sides.
    public static byte[] BuildHead(byte[] header, double gape)
    {
        var m = new Mesh();
        // the skull's cross-sections along it: z, half width, height over the jaw line
        (float Z, float W, float H)[] skull = { (0, 230, 230), (320, 430, 400), (720, 400, 330), (1150, 320, 240), (1560, 190, 130) };
        const int sides = 7;                       // round the top half and across the palate
        int[][] Section(IReadOnlyList<(float Z, float W, float H)> secs, float sign, Func<Vector3, Vector3> place)
        {
            var rings = new int[secs.Count][];
            for (var i = 0; i < secs.Count; i++)
            {
                rings[i] = new int[sides + 1];
                for (var k = 0; k <= sides; k++)
                {
                    var a = Math.PI * k / sides;     // 0..pi from one side over the top to the other
                    rings[i][k] = m.P(place(new Vector3((float)(secs[i].W * Math.Cos(a)), (float)(sign * secs[i].H * Math.Sin(a)), secs[i].Z)));
                }
            }
            return rings;
        }
        void Skin_(int[][] rings, float sign, int skinColour, int mouthColour, Func<Vector3, Vector3> place)
        {
            for (var i = 0; i + 1 < rings.Length; i++)
            {
                for (var k = 0; k < sides; k++)
                {
                    var q = new[] { rings[i][k], rings[i][k + 1], rings[i + 1][k + 1], rings[i + 1][k] };
                    var mid = q.Aggregate(Vector3.Zero, (acc, p) => acc + m.Points[p]) / 4;
                    var axis = (m.Points[rings[i][0]] + m.Points[rings[i][sides]] + m.Points[rings[i + 1][0]] + m.Points[rings[i + 1][sides]]) / 4;
                    m.Face(q, skinColour, mid - axis);
                }
                // (the jaw line's flat inside: the mouth)
                var inside = new[] { rings[i][0], rings[i][sides], rings[i + 1][sides], rings[i + 1][0] };
                m.Face(inside, mouthColour, place(new Vector3(0, -sign, 0)) - place(Vector3.Zero), 2);
            }
            // its front and its back closed
            foreach (var (ring, way) in new[] { (rings[0], -1f), (rings[^1], 1f) })
            {
                var c = m.P(ring.Aggregate(Vector3.Zero, (acc, p) => acc + m.Points[p]) / ring.Length);
                for (var k = 0; k < sides; k++) m.Face(new[] { ring[k], ring[k + 1], c }, skinColour, place(new Vector3(0, 0, way)) - place(Vector3.Zero));
            }
        }
        Vector3 Same(Vector3 v) => v;
        var upper = Section(skull, 1, Same);
        Skin_(upper, 1, Skin, Mouth, Same);
        // the lower jaw: shallower, from its hinge at z 160 to short of the snout's tip, turned down about the hinge
        (float Z, float W, float H)[] jaw = { (160, 360, 150), (600, 340, 170), (1050, 270, 130), (1450, 160, 70) };
        var hinge = new Vector3(0, 0, 160);
        var turn = Matrix4x4.CreateRotationX((float)gape);
        Vector3 Jaw(Vector3 v) => Vector3.Transform(v - hinge, turn) + hinge;
        var lower = Section(jaw, -1, Jaw);
        Skin_(lower, -1, Belly, Mouth, Jaw);
        // the teeth: down from the skull's jaw line, up from the jaw's, both sides
        void Tooth(Vector3 root, float down, Func<Vector3, Vector3> place)
        {
            int a = m.P(place(root + new Vector3(0, 0, -45))), b = m.P(place(root + new Vector3(0, 0, 45))), tip = m.P(place(root + new Vector3(0, down, 0)));
            m.Face(new[] { a, b, tip }, Teeth, place(new Vector3(1, 0, 0)) - place(Vector3.Zero), 1);
            m.Face(new[] { a, b, tip }, Teeth, place(new Vector3(-1, 0, 0)) - place(Vector3.Zero), 1);
        }
        foreach (var side in new[] { -1f, 1f })
        {
            for (var z = 520f; z <= 1420f; z += 180f)
            {
                var w = Interp(skull, z) * 0.92f;
                Tooth(new Vector3(side * w, 0, z), -170, Same);
            }
            for (var z = 600f; z <= 1350f; z += 190f)
            {
                var w = Interp(jaw, z) * 0.92f;
                Tooth(new Vector3(side * w, 0, z), 150, Jaw);
            }
            // the eyes, high on the skull's sides
            var eye = m.P(new Vector3(side * 340, 300, 430));
            m.Spheres.Add(new BodySphere(eye, 95, Eye));
            var pupil = m.P(new Vector3(side * 390, 310, 470));
            m.Spheres.Add(new BodySphere(pupil, 45, Pupil));
        }
        return m.Write(header);
    }

    private static float Interp((float Z, float W, float H)[] secs, float z)
    {
        for (var i = 0; i + 1 < secs.Length; i++)
            if (z <= secs[i + 1].Z) return secs[i].W + (secs[i + 1].W - secs[i].W) * (z - secs[i].Z) / (secs[i + 1].Z - secs[i].Z);
        return secs[^1].W;
    }

    // Into the game folder: the neck and the head (open, shut) appended to BODY.HQR as the mushroom's entity's bodies Neck, HeadOpen and
    // HeadShut (the mushroom's own header: a fixed object). A line for the log.
    public static string Install(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var header = Body.Read(HqrArchive.Open(bodyPath).Read(RaceTrackOil.MushroomBody), 2, allowStatic: true).Header;
        var first = HqrArchive.CountEntries(bodyPath);
        var bytes = File.ReadAllBytes(bodyPath);
        foreach (var body in new[] { BuildNeck(header), BuildHead(header, 0.75), BuildHead(header, 0.05) })
            bytes = HqrWriter.AppendEntry(bytes, HqrWriter.StoredEntry(body));
        File.WriteAllBytes(bodyPath, bytes);
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        table = RaceTrackBaldinoCar.WithBody(table, RaceTrackOil.Entity, Neck, first);
        table = RaceTrackBaldinoCar.WithBody(table, RaceTrackOil.Entity, HeadOpen, first + 1);
        table = RaceTrackBaldinoCar.WithBody(table, RaceTrackOil.Entity, HeadShut, first + 2);
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return $"the gas monsters: BODY.HQR entries {first}-{first + 2}, the mushroom's entity ({RaceTrackOil.Entity}) bodies {Neck} (neck), {HeadOpen} and {HeadShut} (head, jaws open and shut)";
    }

    // Where along the stretch From..To (the lap's points) monsters rise: every Every cells, out past the rail over the gas on one side or the
    // other (each side in turn where both have it), clear of the lap elsewhere and of what stands there; at most Most.
    public static List<GasMonsterSpot> Place(IslandFile island, TrackRoad r, int from, int to, GasMonsterRun run, RaceTrackOptions o, RaceTrackReport report)
    {
        var placed = report.GasMonsters;
        var n = r.Count;
        var span = ((to - from) % n + n) % n + 1;
        var per = Math.Max(1, (int)Math.Round(run.Every / o.Spacing));
        var spots = new List<GasMonsterSpot>();
        var boxes = IslandOps.CubeCells(island).SelectMany(c => c.Item3.Decors.Select(d =>
            ((c.Item1 * (double)IslandFile.CubeSize + d.XMin) / 512, (c.Item2 * (double)IslandFile.CubeSize + d.ZMin) / 512,
             (c.Item1 * (double)IslandFile.CubeSize + d.XMax) / 512, (c.Item2 * (double)IslandFile.CubeSize + d.ZMax) / 512))).ToList();
        bool Gas(double x, double z)
        {
            for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                    if ((IslandOps.Altitude(island, (x + dx * 0.8) * 512, (z + dz * 0.8) * 512) ?? 0) > GasLevel) return false;
            return !boxes.Any(b => x > b.Item1 - 1 && x < b.Item3 + 1 && z > b.Item2 - 1 && z < b.Item4 + 1);
        }
        bool ClearOfLap(double x, double z, int k)
        {
            for (var i = 0; i < n; i++)
            {
                var apart = Math.Abs(i - k); apart = Math.Min(apart, n - apart);
                if (apart * o.Spacing < 6) continue;
                var half = r.RaisedHalfs?[i] ?? o.RaisedHalfWidth;
                if ((r.X[i] - x) * (r.X[i] - x) + (r.Z[i] - z) * (r.Z[i] - z) < (half + 2.5) * (half + 2.5)) return false;
            }
            return true;
        }
        var prefer = 1;
        for (var j = per / 2; j < span && spots.Count < run.Most; j += per)
        {
            var k = (from + j) % n;
            if (r.Gap[k]) continue;
            double tx = r.Tx[k], tz = r.Tz[k], ax = -tz, az = tx;
            var half = r.RaisedHalfs?[k] ?? o.RaisedHalfWidth;
            foreach (var side in new[] { prefer, -prefer })
            {
                double sx = r.X[k] + ax * side * (half + run.Out), sz = r.Z[k] + az * side * (half + run.Out);
                if (!Gas(sx, sz) || !ClearOfLap(sx, sz, k)) continue;
                // (and not near another: two side by side bit a car one after the other)
                if (spots.Concat(placed).Any(o => (o.X - sx) * (o.X - sx) + (o.Z - sz) * (o.Z - sz) < Apart * Apart)) continue;
                spots.Add(new GasMonsterSpot(sx, sz, r.X[k] + ax * side * half * BiteAcross, r.Z[k] + az * side * half * BiteAcross, r.H[k]));
                prefer = -side;
                break;
            }
        }
        report.Notes.Add($"gas monsters along the lap's points {from}-{to}: {spots.Count} ({string.Join(", ", spots.Select(s => $"({s.X:0.0}, {s.Z:0.0})"))})");
        return spots;
    }
}
