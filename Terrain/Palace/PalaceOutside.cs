using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Palace;

// The palace's outside (the user, 2026-10-12: "We need to add in the outside of the palace as featured in Otringal.ile"). On Otringal's
// island the palace is a cluster of decor objects on the hill of cube (8, 7): four octagonal halls round four points -- three gold domes
// ringed in turquoise and a grey one -- each hall cut into the quarters of OTRINGAL.OBL's bodies 36-48 (a quarter turned about its hall's
// middle, the towers, wings and the grey stone dome to the north-west built onto them), the porch with the red front door on its west
// side (49), and the stone arch in front of it (33-35). The interior is five times its size and of another shape -- sixteen square rooms
// four by four and the long hall (80) off them -- so the two can't be the same walls; here the cluster stands on the sixteen rooms' roof,
// the way it stands on its hill:
//   - all of it Scale times its size about its middle, over the middle of the sixteen rooms (it fits their roof with a little to spare;
//     the long hall's roof stays a terrace), its foot on the roof. Each body is drawn only (its box touches nothing, as the rooms' are);
//   - its shape seen from above (Cell x Cell columns as high as its top, up to MaxClimb layers over the roof) as collision boxes, so a car
//     on the rooftops drives round the halls and towers, not through them;
//   - the arch round the palace's own door to the outside (room 154's: the door the game's Otringal palace door leads to), on the ground,
//     GateScale times its size, turned to face out of the door.
// The rooms' roof slabs stay where they are: the terrace the cluster stands on (they are under its halls, and between them the rooftops).
internal static class PalaceOutside
{
    // Otringal's palace: the decors of this cube of OTRINGAL.ILE with these bodies of OTRINGAL.OBL
    public static readonly (int X, int Z) SourceCube = (8, 7);
    public static readonly int[] ClusterBodies = { 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49 };
    public static readonly int[] ArchBodies = { 33, 34, 35 };
    public const double Scale = 4.5;            // the cluster over the sixteen rooms
    public const double GateScale = 4.0;        // the arch round the door: a little wider than it
    public const int MaxClimb = 12;             // the collision's layers over the roof at the most

    // Where the cluster goes: the sixteen rooms' extent (world units), the roof's top, the ground, the building's cell and layer, and the
    // door to the outside -- its middle on the wall's outer face, the way out of it (a unit along x or z) and its width -- when there is one.
    public sealed record Site(double X0, double Z0, double X1, double Z1, double RoofTop, double Ground, double Cell, double Layer,
        (double X, double Z, int Nx, int Nz, double Width)? Door);

    public sealed record Placed(int Decors, int Bodies, List<(double X0, double Y0, double Z0, double X1, double Y1, double Z1)> Boxes, List<string> Log);

    // Adds the cluster and the arch to `island` (their bodies, scaled, appended to `newBodies`, numbered from `firstBody` on) and returns
    // the collision boxes for the caller to add with its empty body.
    public static Placed Add(IslandFile island, IslandFile source, HqrArchive sourceObl, List<byte[]> newBodies, int firstBody, Site site)
    {
        var log = new List<string>();
        var boxes = new List<(double, double, double, double, double, double)>();
        var cube = source.CubeAt(SourceCube.X, SourceCube.Z) ?? throw new InvalidDataException($"{PalaceIsland.SourceIle} has no cube {SourceCube}: no palace to copy.");
        double ox = SourceCube.X * (double)IslandFile.CubeSize, oz = SourceCube.Z * (double)IslandFile.CubeSize;
        var cluster = cube.Decors.Where(d => ClusterBodies.Contains(d.Body & 0xFFFF)).ToList();
        var arch = cube.Decors.Where(d => ArchBodies.Contains(d.Body & 0xFFFF)).ToList();
        if (cluster.Count == 0) throw new InvalidDataException($"{PalaceIsland.SourceIle}'s cube {SourceCube} has no palace (bodies {ClusterBodies[0]}-{ClusterBodies[^1]}).");
        int decors = 0, bodies = 0, left = 0;
        // (each source body scaled once, however often it is placed)
        var scaled = new Dictionary<(int Body, double Scale), int>();
        int BodyOf(int body, double k)
        {
            if (scaled.TryGetValue((body, k), out var index)) return index;
            var clipped = 0;
            newBodies.Add(IslandScaler.ScaledBody(sourceObl.Read(body), k, ref clipped));
            if (clipped > 0) throw new InvalidDataException($"{PalaceIsland.SourceObl} body {body} {k} times its size is bigger than a body holds.");
            bodies++;
            return scaled[(body, k)] = firstBody + newBodies.Count - 1;
        }

        // ---- the cluster: Scale times its size about the middle of its boxes, over the middle of the rooms, its foot on the roof
        double sx0 = cluster.Min(d => ox + d.XMin), sx1 = cluster.Max(d => ox + d.XMax), sz0 = cluster.Min(d => oz + d.ZMin), sz1 = cluster.Max(d => oz + d.ZMax);
        double baseY = cluster.Min(d => d.YMin);
        double scx = (sx0 + sx1) / 2, scz = (sz0 + sz1) / 2, tcx = (site.X0 + site.X1) / 2, tcz = (site.Z0 + site.Z1) / 2;
        if ((sx1 - sx0) * Scale > site.X1 - site.X0 || (sz1 - sz0) * Scale > site.Z1 - site.Z0)
            throw new InvalidDataException($"Otringal's palace {Scale} times its size ({(sx1 - sx0) * Scale:0} x {(sz1 - sz0) * Scale:0}) is bigger than the rooms' roof.");
        double X(double x) => tcx + (ox + x - scx) * Scale;
        double Z(double z) => tcz + (oz + z - scz) * Scale;
        double Y(double y) => site.RoofTop + (y - baseY) * Scale;
        var triangles = new List<(Vector3 A, Vector3 B, Vector3 C)>();
        foreach (var d in cluster)
        {
            var body = BodyOf(d.Body & 0xFFFF, Scale);
            double wx = X(d.X), wz = Z(d.Z);
            if (IslandDecors.Locate(island, wx, wz) is not { } at || at.Cube.Decors.Count >= IslandDecors.MaxPerCube) { left++; continue; }
            var copy = d.Clone();
            copy.Body = (d.Body & ~0xFFFF) | body;
            copy.X = at.X; copy.Y = (int)Math.Round(Y(d.Y)); copy.Z = at.Z;
            copy.XMin = (int)Math.Floor(at.X + X(d.XMin) - wx); copy.XMax = (int)Math.Ceiling(at.X + X(d.XMax) - wx);
            copy.ZMin = (int)Math.Floor(at.Z + Z(d.ZMin) - wz); copy.ZMax = (int)Math.Ceiling(at.Z + Z(d.ZMax) - wz);
            // (drawn only: what it is made of is the collision below -- and a body this big is drawn while any corner of its foot is in
            // front of the camera, its middle maybe not: DECORS.CPP's test for a box whose top is under its bottom)
            copy.YMin = (int)Math.Floor(Y(d.YMin)); copy.YMax = BrickBuilding.NoBoxTop;
            at.Cube.Decors.Add(copy);
            decors++;
            triangles.AddRange(Triangles(newBodies[body - firstBody], wx, Y(d.Y), wz, d.Beta & 0xFFFF));
        }
        log.Add($"the outside: Otringal's palace ({cluster.Count} of {PalaceIsland.SourceIle}'s objects, bodies {string.Join(", ", cluster.Select(d => d.Body & 0xFFFF).Distinct().OrderBy(b => b))}) " +
                $"{Scale} times its size ({(sx1 - sx0) * Scale:0} x {(sz1 - sz0) * Scale:0}, {(cluster.Max(d => d.YMax) - baseY) * Scale:0} high) on the sixteen rooms' roof");

        // ---- its collision: the shape seen from above, columns of cells as high as its top (MaxClimb layers at the most), merged
        var tops = new Dictionary<(int X, int Z), double>();
        const int Sub = 3;      // (samples a cell each way: thin walls and towers still count)
        foreach (var (a, b, c) in triangles)
        {
            double minX = Math.Min(a.X, Math.Min(b.X, c.X)), maxX = Math.Max(a.X, Math.Max(b.X, c.X));
            double minZ = Math.Min(a.Z, Math.Min(b.Z, c.Z)), maxZ = Math.Max(a.Z, Math.Max(b.Z, c.Z));
            double det = (b.Z - c.Z) * (a.X - c.X) + (c.X - b.X) * (a.Z - c.Z);
            if (Math.Abs(det) < 1e-3) continue;     // (an upright face: the faces over it give the top)
            var step = site.Cell / Sub;
            int i0 = (int)Math.Floor((minX - site.X0) / step), i1 = (int)Math.Ceiling((maxX - site.X0) / step);
            int j0 = (int)Math.Floor((minZ - site.Z0) / step), j1 = (int)Math.Ceiling((maxZ - site.Z0) / step);
            for (var j = j0; j <= j1; j++)
                for (var i = i0; i <= i1; i++)
                {
                    double px = site.X0 + (i + 0.5) * step, pz = site.Z0 + (j + 0.5) * step;
                    var w0 = ((b.Z - c.Z) * (px - c.X) + (c.X - b.X) * (pz - c.Z)) / det;
                    var w1 = ((c.Z - a.Z) * (px - c.X) + (a.X - c.X) * (pz - c.Z)) / det;
                    var w2 = 1 - w0 - w1;
                    if (w0 < -1e-6 || w1 < -1e-6 || w2 < -1e-6) continue;
                    var y = w0 * a.Y + w1 * b.Y + w2 * c.Y;
                    var key = ((int)Math.Floor((double)i / Sub), (int)Math.Floor((double)j / Sub));
                    if (!tops.TryGetValue(key, out var h) || y > h) tops[key] = y;
                }
        }
        var solid = new HashSet<(int X, int Y, int Z)>();
        foreach (var ((gx, gz), h) in tops)
        {
            var layers = Math.Min(MaxClimb, (int)Math.Ceiling((h - site.RoofTop) / site.Layer - 0.25));
            for (var l = 0; l < layers; l++) solid.Add((gx, l, gz));
        }
        var clusterBoxes = BrickBuilding.Boxes(solid);
        foreach (var bx in clusterBoxes)
            boxes.Add((site.X0 + bx.X0 * site.Cell, site.RoofTop + bx.Y0 * site.Layer, site.Z0 + bx.Z0 * site.Cell,
                       site.X0 + (bx.X1 + 1) * site.Cell, site.RoofTop + (bx.Y1 + 1) * site.Layer, site.Z0 + (bx.Z1 + 1) * site.Cell));
        log.Add($"  its shape from above as {clusterBoxes.Count} collision boxes ({tops.Count} columns of the roof's cells, up to {MaxClimb} layers)");

        // ---- the arch, round the door to the outside
        if (arch.Count > 0 && site.Door is { } door)
        {
            double ax0 = arch.Min(d => ox + d.XMin), ax1 = arch.Max(d => ox + d.XMax), az0 = arch.Min(d => oz + d.ZMin), az1 = arch.Max(d => oz + d.ZMax);
            double acx = (ax0 + ax1) / 2, acz = (az0 + az1) / 2, archBase = arch.Min(d => d.YMin);
            // (its way through: across its narrower side; the side towards the palace's porch is its inside, which goes to the wall)
            var alongX = ax1 - ax0 < az1 - az0;
            var depth = alongX ? ax1 - ax0 : az1 - az0;
            var inward = alongX ? (Math.Sign(scx - acx), 0) : (0, Math.Sign(scz - acz));
            // (a turn of `delta` (4096 a turn) takes the inside to face the wall: a way (x, z) is at angle atan2(-z, x) -- the engine turns
            // a body's (x, z) to (x cos + z sin, -x sin + z cos))
            int Angle(int x, int z) => (int)Math.Round(Math.Atan2(-z, x) / (2 * Math.PI) * 4096);
            var delta = ((Angle(-door.Nx, -door.Nz) - Angle(inward.Item1, inward.Item2)) % 4096 + 4096) % 4096;
            double cos = Math.Cos(delta * 2 * Math.PI / 4096), sin = Math.Sin(delta * 2 * Math.PI / 4096);
            double cx = door.X + door.Nx * depth * GateScale / 2, cz = door.Z + door.Nz * depth * GateScale / 2;
            (double X, double Z) Turned(double x, double z)
            {
                double dx = (ox + x - acx) * GateScale, dz = (oz + z - acz) * GateScale;
                return (cx + dx * cos + dz * sin, cz - dx * sin + dz * cos);
            }
            foreach (var d in arch)
            {
                var body = BodyOf(d.Body & 0xFFFF, GateScale);
                var (wx, wz) = Turned(d.X, d.Z);
                if (IslandDecors.Locate(island, wx, wz) is not { } at || at.Cube.Decors.Count >= IslandDecors.MaxPerCube) { left++; continue; }
                var copy = d.Clone();
                copy.Body = (d.Body & ~0xFFFF) | body;
                copy.Beta = (d.Beta & ~0xFFFF) | (((d.Beta & 0xFFFF) + delta) & 4095);
                copy.X = at.X; copy.Z = at.Z; copy.Y = (int)Math.Round(site.Ground + (d.Y - archBase) * GateScale);
                // (its box, turned with it: solid, as Otringal's is)
                var (p0x, p0z) = Turned(d.XMin, d.ZMin); var (p1x, p1z) = Turned(d.XMax, d.ZMax);
                copy.XMin = (int)Math.Floor(at.X + Math.Min(p0x, p1x) - wx); copy.XMax = (int)Math.Ceiling(at.X + Math.Max(p0x, p1x) - wx);
                copy.ZMin = (int)Math.Floor(at.Z + Math.Min(p0z, p1z) - wz); copy.ZMax = (int)Math.Ceiling(at.Z + Math.Max(p0z, p1z) - wz);
                copy.YMin = (int)Math.Floor(site.Ground + (d.YMin - archBase) * GateScale); copy.YMax = (int)Math.Ceiling(site.Ground + (d.YMax - archBase) * GateScale);
                at.Cube.Decors.Add(copy);
                decors++;
            }
            log.Add($"  the stone arch (bodies {string.Join(", ", arch.Select(d => d.Body & 0xFFFF).Distinct().OrderBy(b => b))}) {GateScale} times its size round the door out " +
                    $"({door.Width:0} wide), on the ground");
        }
        if (left > 0) log.Add($"  WARNING: {left} of its objects left out (a cube full)");
        return new Placed(decors, bodies, boxes, log);
    }

    // A placed body's polygons as triangles in the world: its points turned by the decor's angle (the engine's way) about its place.
    private static IEnumerable<(Vector3, Vector3, Vector3)> Triangles(byte[] data, double x, double y, double z, int beta)
    {
        var body = Body.Read(data, 2, allowStatic: true);
        var turn = Matrix4x4.CreateRotationY((float)(beta * 2 * Math.PI / 4096)) * Matrix4x4.CreateTranslation((float)x, (float)y, (float)z);
        var world = body.World().Select(v => Vector3.Transform(v, turn)).ToArray();
        foreach (var f in body.Faces)
            for (var k = 1; k + 1 < f.Points.Length; k++)
                yield return (world[f.Points[0]], world[f.Points[k]], world[f.Points[k + 1]]);
    }
}
