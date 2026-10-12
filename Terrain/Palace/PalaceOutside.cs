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
// the way it stands on its hill.
//
// 2026-10-12, later (the user: "We need to make the outside look more like the original. Perhaps scaling up the roof section, or adding more
// towers to the roof section. It's okay for the inside to be underground"): the rooms are under the hill now (PalaceCliff), their roofs the
// rock of its top, and on them:
//   - the cluster, Scale times its size about its middle, over the middle of the sixteen rooms, its foot on the roof; the arch where Otringal
//     has it, in front of the porch, as big -- on the hilltop now, the palace's front door below it in the hill closed;
//   - on the long hall's roof, a wing of the palace: Towers smaller halls in a row along it, each as Otringal's north hall (two small domes,
//     its wing, its turret), TowerScale times its size;
//   - each body drawn only (its box touches nothing, as the rooms' are), in every cube it reaches (the engine draws and collides only the
//     decors of the cube the hero is in); the arch solid, as Otringal's is;
//   - the halls' shape seen from above (Cell x Cell columns as high as their tops, up to MaxClimb layers over the roof) as collision boxes,
//     so a car on the rooftops drives round the halls and towers, not through them.
internal static class PalaceOutside
{
    // Otringal's palace: the decors of this cube of OTRINGAL.ILE with these bodies of OTRINGAL.OBL
    public static readonly (int X, int Z) SourceCube = (8, 7);
    public static readonly int[] ClusterBodies = { 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49 };
    public static readonly int[] ArchBodies = { 33, 34, 35 };
    // the hall the wing's towers copy: Otringal's north hall (its two small domes, its wing and its turret), about its middle
    private static readonly (int X, int Z) TowerHall = (13692, 10520);
    public const double Scale = 5.5;            // the cluster (and its arch) over the sixteen rooms
    public const double TowerScale = 3.5;       // the towers on the long hall's roof
    public const int Towers = 2;
    public const int MaxClimb = 12;             // the collision's layers over the roof at the most

    // Where the cluster goes: the sixteen rooms' extent (world units), the roof's top, the building's cell and layer; the long hall's extent
    // (its towers on it) when there is one.
    public sealed record Site(double X0, double Z0, double X1, double Z1, double RoofTop, double Cell, double Layer, (double X0, double Z0, double X1, double Z1)? Hall);

    public sealed record Placed(int Decors, int Bodies, List<(double X0, double Y0, double Z0, double X1, double Y1, double Z1)> Boxes, List<string> Log);

    // Adds the cluster, its arch and the wing's towers to `island` (their bodies, scaled, appended to `newBodies`, numbered from `firstBody`
    // on; at most `maxPerCube` decors a cube) and returns the collision boxes for the caller to add with its empty body.
    public static Placed Add(IslandFile island, IslandFile source, HqrArchive sourceObl, List<byte[]> newBodies, int firstBody, Site site, int maxPerCube)
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
        var triangles = new List<(Vector3 A, Vector3 B, Vector3 C)>();
        // (a source decor Scale times its size about `about` (Otringal's world), its foot at `foot`, its middle at `to`: in every cube its box
        // reaches, drawn only or solid)
        void Place(IslandDecor d, double k, (double X, double Z) about, double baseY, (double X, double Z) to, double foot, bool solid)
        {
            var body = BodyOf(d.Body & 0xFFFF, k);
            double X(double x) => to.X + (ox + x - about.X) * k;
            double Z(double z) => to.Z + (oz + z - about.Z) * k;
            double Y(double y) => foot + (y - baseY) * k;
            double wx = X(d.X), wz = Z(d.Z);
            var yMax = solid ? (int)Math.Ceiling(Y(d.YMax)) : BrickBuilding.NoBoxTop;
            var (placed, full) = AddCopies(island, d, body, wx, (int)Math.Round(Y(d.Y)), wz, X(d.XMin), X(d.XMax), (int)Math.Floor(Y(d.YMin)), yMax, Z(d.ZMin), Z(d.ZMax), maxPerCube);
            if (placed == 0) left++;
            left += full; decors += placed;
            if (!solid) triangles.AddRange(Triangles(newBodies[body - firstBody], wx, Y(d.Y), wz, d.Beta & 0xFFFF));
        }

        // ---- the cluster and its arch: Scale times their size about the middle of the cluster's boxes, over the middle of the sixteen rooms,
        // their foot on the roof
        double sx0 = cluster.Min(d => ox + d.XMin), sx1 = cluster.Max(d => ox + d.XMax), sz0 = cluster.Min(d => oz + d.ZMin), sz1 = cluster.Max(d => oz + d.ZMax);
        double baseY = cluster.Min(d => d.YMin);
        var middle = ((sx0 + sx1) / 2, (sz0 + sz1) / 2);
        var over = ((site.X0 + site.X1) / 2, (site.Z0 + site.Z1) / 2);
        var all = cluster.Concat(arch).ToList();
        double ax0 = all.Min(d => ox + d.XMin), ax1 = all.Max(d => ox + d.XMax), az0 = all.Min(d => oz + d.ZMin), az1 = all.Max(d => oz + d.ZMax);
        if (over.Item1 + (ax0 - middle.Item1) * Scale < site.X0 || over.Item1 + (ax1 - middle.Item1) * Scale > site.X1 ||
            over.Item2 + (az0 - middle.Item2) * Scale < site.Z0 || over.Item2 + (az1 - middle.Item2) * Scale > site.Z1)
            throw new InvalidDataException($"Otringal's palace {Scale} times its size ({(ax1 - ax0) * Scale:0} x {(az1 - az0) * Scale:0}) is bigger than the rooms' roof.");
        foreach (var d in cluster) Place(d, Scale, middle, baseY, over, site.RoofTop, solid: false);
        foreach (var d in arch) Place(d, Scale, middle, baseY, over, site.RoofTop, solid: true);
        log.Add($"the outside: Otringal's palace ({cluster.Count} of {PalaceIsland.SourceIle}'s objects, bodies {string.Join(", ", cluster.Select(d => d.Body & 0xFFFF).Distinct().OrderBy(b => b))}) " +
                $"{Scale} times its size ({(sx1 - sx0) * Scale:0} x {(sz1 - sz0) * Scale:0}, {(cluster.Max(d => d.YMax) - baseY) * Scale:0} high) on the sixteen rooms' roof, " +
                $"its stone arch (bodies {string.Join(", ", arch.Select(d => d.Body & 0xFFFF).Distinct().OrderBy(b => b))}) in front of its porch");

        // ---- the wing on the long hall's roof: Towers halls in a row along it, each as Otringal's north hall
        var towerParts = cluster.Where(d => d.X == TowerHall.X && d.Z == TowerHall.Z).ToList();
        if (site.Hall is { } hall && towerParts.Count > 0)
        {
            var alongZ = hall.Z1 - hall.Z0 >= hall.X1 - hall.X0;
            for (var t = 0; t < Towers; t++)
            {
                // (towards the hall's far end: the rooftops' road passes between the near one and the sixteen rooms' domes)
                var f = 0.18 + 0.38 * t / Math.Max(1, Towers - 1);
                var at = alongZ ? ((hall.X0 + hall.X1) / 2, hall.Z0 + (hall.Z1 - hall.Z0) * f) : (hall.X0 + (hall.X1 - hall.X0) * f, (hall.Z0 + hall.Z1) / 2);
                foreach (var d in towerParts) Place(d, TowerScale, (ox + TowerHall.X, oz + TowerHall.Z), baseY, at, site.RoofTop, solid: false);
            }
            log.Add($"  a wing on the long hall's roof: {Towers} halls as Otringal's north hall ({towerParts.Count} objects each), {TowerScale} times their size");
        }

        // ---- the halls' collision: their shape seen from above, columns of cells as high as their tops (MaxClimb layers at the most), merged
        var tops = new Dictionary<(int X, int Z), double>();
        const int Sub = 3;      // (samples a cell each way: thin walls and towers still count)
        var gx0 = Math.Min(site.X0, site.Hall?.X0 ?? site.X0); var gz0 = Math.Min(site.Z0, site.Hall?.Z0 ?? site.Z0);
        foreach (var (a, b, c) in triangles)
        {
            double minX = Math.Min(a.X, Math.Min(b.X, c.X)), maxX = Math.Max(a.X, Math.Max(b.X, c.X));
            double minZ = Math.Min(a.Z, Math.Min(b.Z, c.Z)), maxZ = Math.Max(a.Z, Math.Max(b.Z, c.Z));
            double det = (b.Z - c.Z) * (a.X - c.X) + (c.X - b.X) * (a.Z - c.Z);
            if (Math.Abs(det) < 1e-3) continue;     // (an upright face: the faces over it give the top)
            var step = site.Cell / Sub;
            int i0 = (int)Math.Floor((minX - gx0) / step), i1 = (int)Math.Ceiling((maxX - gx0) / step);
            int j0 = (int)Math.Floor((minZ - gz0) / step), j1 = (int)Math.Ceiling((maxZ - gz0) / step);
            for (var j = j0; j <= j1; j++)
                for (var i = i0; i <= i1; i++)
                {
                    double px = gx0 + (i + 0.5) * step, pz = gz0 + (j + 0.5) * step;
                    var w0 = ((b.Z - c.Z) * (px - c.X) + (c.X - b.X) * (pz - c.Z)) / det;
                    var w1 = ((c.Z - a.Z) * (px - c.X) + (a.X - c.X) * (pz - c.Z)) / det;
                    var w2 = 1 - w0 - w1;
                    if (w0 < -1e-6 || w1 < -1e-6 || w2 < -1e-6) continue;
                    var y = w0 * a.Y + w1 * b.Y + w2 * c.Y;
                    var key = ((int)Math.Floor((double)i / Sub), (int)Math.Floor((double)j / Sub));
                    if (!tops.TryGetValue(key, out var h) || y > h) tops[key] = y;
                }
        }
        var solidCells = new HashSet<(int X, int Y, int Z)>();
        foreach (var ((gx, gz), h) in tops)
        {
            var layers = Math.Min(MaxClimb, (int)Math.Ceiling((h - site.RoofTop) / site.Layer - 0.25));
            for (var l = 0; l < layers; l++) solidCells.Add((gx, l, gz));
        }
        var hallBoxes = BrickBuilding.Boxes(solidCells);
        foreach (var bx in hallBoxes)
            boxes.Add((gx0 + bx.X0 * site.Cell, site.RoofTop + bx.Y0 * site.Layer, gz0 + bx.Z0 * site.Cell,
                       gx0 + (bx.X1 + 1) * site.Cell, site.RoofTop + (bx.Y1 + 1) * site.Layer, gz0 + (bx.Z1 + 1) * site.Cell));
        log.Add($"  their shape from above as {hallBoxes.Count} collision boxes ({tops.Count} columns of the roof's cells, up to {MaxClimb} layers); " +
                $"{decors} objects in all, counting the copies in each cube a body reaches");
        if (left > 0) log.Add($"  WARNING: {left} of its objects left out (a cube full)");
        return new Placed(decors, bodies, boxes, log);
    }

    // A source decor's copy in every cube its box reaches (world units), each in that cube's frame, its body `body`, its turn and the rest
    // as the source's.
    private static (int Placed, int Full) AddCopies(IslandFile island, IslandDecor source, int body, double wx, int y, double wz,
        double minX, double maxX, int yMin, int yMax, double minZ, double maxZ, int maxPerCube)
    {
        var size = (double)IslandFile.CubeSize;
        int placed = 0, full = 0;
        for (var cz = (int)Math.Floor(minZ / size); cz <= (int)Math.Floor((maxZ - 1e-6) / size); cz++)
        for (var cx = (int)Math.Floor(minX / size); cx <= (int)Math.Floor((maxX - 1e-6) / size); cx++)
        {
            if (island.CubeAt(cx, cz) is not { } cube) continue;
            if (cube.Decors.Count >= maxPerCube) { full++; continue; }
            double ox = cx * size, oz = cz * size;
            var copy = source.Clone();
            copy.Body = (source.Body & ~0xFFFF) | body;
            copy.X = (int)Math.Round(wx - ox); copy.Y = y; copy.Z = (int)Math.Round(wz - oz);
            copy.XMin = (int)Math.Floor(minX - ox); copy.XMax = (int)Math.Ceiling(maxX - ox);
            copy.ZMin = (int)Math.Floor(minZ - oz); copy.ZMax = (int)Math.Ceiling(maxZ - oz);
            copy.YMin = yMin; copy.YMax = yMax;
            cube.Decors.Add(copy);
            placed++;
        }
        return (placed, full);
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
