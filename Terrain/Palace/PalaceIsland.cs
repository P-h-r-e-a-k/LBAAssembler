using System.IO;
using System.Numerics;
using LbaBodyStudio;
using LBAAssembler.Lba1;

namespace LBAAssembler.Terrain.Palace;

// The Palace island (the user, 2026-10-08: "We need to create a new track based on the Palace that's featured on Otringal, this will need to
// be recreated as a new island. The idea behind this track is that the cars race through the inside of the palace, through the window by
// the chest that Twinsen normally leaves by then around the roof tops. To accomodate all of this we'll probably need to scale up everything
// ... let's just get our Palace island created for now"). The Emperor's palace is a maze of sixteen rooms of 13 x 13 cells, four by four,
// and the last room (80, the chest and the window Twinsen leaves by) off its fourth row: interiors, grids of bricks -- pictures, not shapes.
// Here they become a 3D building on an island of its own:
//   - the rooms placed as the zones between them put them (the editor's joined map, Lba2Areas, without the few cells it moves them apart to
//     keep their shared walls from overlapping on its picture: here neighbours share their walls), every filled cell a box Across times
//     a brick across and Up times a layer high, in the average colour of its brick (the exporter's blocky map, GridMesher) matched in
//     Otringal's palette, its faces shaded by the way they face; only the faces that show, merged into rectangles of one colour;
//   - one body for each room's faces (more where they are many), each a decor whose box nothing touches (it is drawn, not walked on);
//   - the walls, floors and furniture as collision boxes: the filled cells merged into boxes, each a decor with an empty body;
//   - a roof over each room, level with the top of its walls -- the rooftops -- walkable;
//   - the island: OTRINGAL.ILE's atlases and palette (so the game draws it as Otringal when it is raced -- the race-track mode's island_file=),
//     its cubes round the palace a paved plateau at the rooms' floor, falling to the sea at its edge.
// PALACE.OBL is OTRINGAL.OBL with the palace's bodies on its end.
internal static class PalaceIsland
{
    public const string IleFile = "PALACE.ILE", OblFile = "PALACE.OBL";
    public const string SourceIle = "OTRINGAL.ILE", SourceObl = "OTRINGAL.OBL";
    public const int PaletteEntry = 31;                 // RESS.HQR: Otringal's
    public const int Across = 3;                        // a brick's 512 across, this many times
    public const double Up = 2.5;                       // a layer's 256 up, this many times
    public const int Floor = 2200;                      // the rooms' bottom layer's foot, over the island's zero
    public const int Margin = 28;                       // cells of paved ground round the palace
    public const int Slope = 10;                        // ... the last of them falling to the sea
    public const int MaxFaces = 540, MaxPoints = 540;   // a body's polygons and points at the most (the engine's 550: Body.Limit)
    public const int MaxRun = 12;                       // a merged rectangle's cells along a side at the most ...
    public const float MaxSpan = 56000;                 // ... and a body's extent (its points within a signed 16-bit of its origin)
    public const int FirstCube = 6;                     // the island's cubes from (6, 6) on
    private const int NoBoxTop = -32000;
    // Otringal's paved palace courtyard, whose ground the plateau is painted with (an island cell)
    private static readonly (int Gx, int Gz) PavingCell = (494, 474);
    // the faces' shades, as the exporter's: top, bottom, east and west, south and north
    private static readonly (int Dx, int Dy, int Dz, float Shade)[] Directions =
        { (0, 1, 0, 1.00f), (0, -1, 0, 0.50f), (1, 0, 0, 0.84f), (-1, 0, 0, 0.84f), (0, 0, 1, 0.70f), (0, 0, -1, 0.70f) };
    private static readonly (float R, float G, float B) RoofColour = (176, 78, 52), RoofEdge = (120, 50, 36);

    public sealed record Built(int Rooms, int Cells, int Bodies, int Faces, int Boxes, int Roofs, (int X, int Z) FirstCube, (int X, int Z) Cubes, string Log);

    // Builds PALACE.ILE and PALACE.OBL into `outDirectory` from the game folder's palace scenes and Otringal's island files (`sourceDirectory`:
    // where pristine OTRINGAL.ILE/.OBL are, the game folder itself by default).
    // (`roofed`: false leaves the roofs off -- a look inside)
    public static Built Build(string gameDirectory, string outDirectory, string? sourceDirectory = null, bool roofed = true)
    {
        sourceDirectory ??= gameDirectory;
        var log = new List<string>();
        // ---- the rooms' cells, as the joined map places them (without its separations)
        var interiors = new Lba2Interiors(gameDirectory);
        var area = Lba2Areas.Find(interiors.LoadScene).FirstOrDefault(a => a.Tiles.Any(t => t.Scene == 151))
                   ?? throw new InvalidDataException("The game folder has no Emperor's palace (scenes 151-166 and 80).");
        var cells = new Dictionary<(int X, int Y, int Z), (int Brick, int Tile)>();
        for (var ti = 0; ti < area.Tiles.Count; ti++)
        {
            var tile = area.Tiles[ti];
            var sep = Lba2Areas.Separations.Where(s => s.Scene == tile.Scene).Select(s => (s.Dx, s.Dy, s.Dz)).FirstOrDefault();
            int ox = tile.OffsetX / 512 - sep.Dx, oy = tile.OffsetY / 256 - sep.Dy, oz = tile.OffsetZ / 512 - sep.Dz;
            foreach (var p in interiors.Placements(tile))
                cells.TryAdd((p.X + ox, p.Y + oy, p.Z + oz), (p.Brick, ti));
        }
        if (cells.Count == 0) throw new InvalidDataException("The palace's scenes have no cells.");
        int minX = cells.Keys.Min(c => c.X), maxX = cells.Keys.Max(c => c.X), minY = cells.Keys.Min(c => c.Y), maxY = cells.Keys.Max(c => c.Y);
        int minZ = cells.Keys.Min(c => c.Z), maxZ = cells.Keys.Max(c => c.Z);
        log.Add($"the palace: {area.Tiles.Count} rooms (scenes {string.Join(", ", area.Tiles.Select(t => t.Scene))}), {cells.Count} filled cells, " +
                $"{maxX - minX + 1} x {maxZ - minZ + 1} cells and {maxY - minY + 1} layers");

        // ---- the island: its cubes round the palace
        const double U = 512.0 * Across, V = 256.0 * Up;
        var spanX = (maxX - minX + 1) * Across + 2 * Margin; var spanZ = (maxZ - minZ + 1) * Across + 2 * Margin;
        int cubesX = (spanX + 63) / 64, cubesZ = (spanZ + 63) / 64;
        if (FirstCube + cubesX > 15 || FirstCube + cubesZ > 15) throw new InvalidDataException($"The palace needs {cubesX} x {cubesZ} cubes: more than the map holds from cube {FirstCube}.");
        // (the palace in the middle of its cubes: world units of its first cell's west and north faces)
        double x0 = FirstCube * 32768.0 + (cubesX * 64 - (maxX - minX + 1) * Across) / 2.0 * 512;
        double z0 = FirstCube * 32768.0 + (cubesZ * 64 - (maxZ - minZ + 1) * Across) / 2.0 * 512;
        double WX(int x) => x0 + (x - minX) * U;
        double WZ(int z) => z0 + (z - minZ) * U;
        double WY(int y) => Floor + (y - minY) * V;
        var source = IslandFile.Load(Path.Combine(sourceDirectory, SourceIle));
        var cubeList = new List<(int X, int Z)>();
        for (var cz = 0; cz < cubesZ; cz++) for (var cx = 0; cx < cubesX; cx++) cubeList.Add((FirstCube + cx, FirstCube + cz));
        var island = IslandFile.Parse(IslandScaler.NewFile(source, cubeList), Path.Combine(outDirectory, IleFile));
        Ground(island, source, cubeList, x0, z0, (maxX - minX + 1) * U, (maxZ - minZ + 1) * U);

        // ---- colours: each brick's average in the interiors' palette, matched in Otringal's, shaded by the way a face looks
        var palette = interiors.Palette;
        var islandPalette = IslandMapRenderer.LoadPaletteEntry(gameDirectory, PaletteEntry);
        var averages = new Dictionary<int, (float R, float G, float B)>();
        var matched = new Dictionary<(float, float, float), int>();
        int Nearest((float R, float G, float B) c)
        {
            if (matched.TryGetValue(c, out var known)) return known;
            var best = 1; var bd = double.MaxValue;
            for (var i = 1; i < 255; i++)
            {
                var p = Colour(islandPalette, i);
                double d = (p.R - c.R) * (p.R - c.R) * 0.3 + (p.G - c.G) * (p.G - c.G) * 0.59 + (p.B - c.B) * (p.B - c.B) * 0.11;
                if (d < bd) { bd = d; best = i; }
            }
            return matched[c] = best;
        }
        int ColourOf(int brick, float shade)
        {
            if (!averages.TryGetValue(brick, out var a)) averages[brick] = a = BrickAverage(interiors.ReadBrick(brick), palette);
            return Nearest((MathF.Round(a.R * shade), MathF.Round(a.G * shade), MathF.Round(a.B * shade)));
        }

        // ---- the faces that show, merged into rectangles of one colour, room by room
        // (each face: its room, which way it faces, the plane it is in, and its two coordinates in that plane)
        var planes = new Dictionary<(int Tile, int Dir, int Plane), Dictionary<(int U, int V), int>>();
        foreach (var ((x, y, z), (brick, ti)) in cells)
            for (var d = 0; d < Directions.Length; d++)
            {
                var (dx, dy, dz, shade) = Directions[d];
                if (cells.ContainsKey((x + dx, y + dy, z + dz))) continue;
                var key = (ti, d, dy != 0 ? y : dx != 0 ? x : z);
                if (!planes.TryGetValue(key, out var plane)) planes[key] = plane = new();
                plane[dy != 0 ? (x, z) : dx != 0 ? (z, y) : (x, y)] = ColourOf(brick, shade);
            }
        var roomFaces = new Dictionary<int, List<(Vector3[] Quad, int Colour, Vector3 Normal)>>();
        foreach (var ((ti, d, at), plane) in planes)
        {
            var (dx, dy, dz, _) = Directions[d];
            var list = roomFaces.TryGetValue(ti, out var l) ? l : roomFaces[ti] = new();
            foreach (var (u0, v0, u1, v1, colour) in Rectangles(plane))
            {
                // (the rectangle's corners in the world: the plane at the cells' face on that side)
                Vector3 P(int u, int v) => dy != 0 ? new((float)WX(u), (float)WY(at + (dy > 0 ? 1 : 0)), (float)WZ(v))
                                       : dx != 0 ? new((float)WX(at + (dx > 0 ? 1 : 0)), (float)WY(v), (float)WZ(u))
                                                 : new((float)WX(u), (float)WY(v), (float)WZ(at + (dz > 0 ? 1 : 0)));
                list.Add((new[] { P(u0, v0), P(u1 + 1, v0), P(u1 + 1, v1 + 1), P(u0, v1 + 1) }, colour, new Vector3(dx, dy, dz)));
            }
        }

        // ---- the roofs: over each room, level with the top of its walls
        var roofs = 0;
        var roofBoxes = new List<(double X0, double Y0, double Z0, double X1, double Y1, double Z1)>();
        foreach (var ti in roofed ? roomFaces.Keys.ToList() : new List<int>())
        {
            var mine = cells.Where(c => c.Value.Tile == ti).Select(c => c.Key).ToList();
            if (mine.Count == 0) continue;
            int rx0 = mine.Min(c => c.X), rx1 = mine.Max(c => c.X), rz0 = mine.Min(c => c.Z), rz1 = mine.Max(c => c.Z);
            // (the walls' top: the highest layer that the room's edge cells reach, most of the way round)
            var edge = mine.Where(c => c.X == rx0 || c.X == rx1 || c.Z == rz0 || c.Z == rz1).GroupBy(c => (c.X, c.Z)).Select(g => g.Max(c => c.Y)).OrderBy(h => h).ToList();
            var top = edge.Count > 0 ? edge[(int)(edge.Count * 0.8)] + 1 : mine.Max(c => c.Y) + 1;
            double ya = WY(top), yb = ya + V * 0.4;
            int roofTop = Nearest(RoofColour), roofSide = Nearest(RoofEdge);
            var list = roomFaces[ti];
            void Q(Vector3 a, Vector3 b, Vector3 c, Vector3 e, int colour, Vector3 n) => list.Add((new[] { a, b, c, e }, colour, n));
            // (in slabs of MaxRun cells at the most: a body holds no more)
            for (var sx = rx0; sx <= rx1; sx += MaxRun)
            for (var sz = rz0; sz <= rz1; sz += MaxRun)
            {
            double xa = WX(sx), xb = WX(Math.Min(rx1, sx + MaxRun - 1) + 1), za = WZ(sz), zb = WZ(Math.Min(rz1, sz + MaxRun - 1) + 1);
            Q(new((float)xa, (float)yb, (float)za), new((float)xb, (float)yb, (float)za), new((float)xb, (float)yb, (float)zb), new((float)xa, (float)yb, (float)zb), roofTop, Vector3.UnitY);
            Q(new((float)xa, (float)ya, (float)za), new((float)xb, (float)ya, (float)za), new((float)xb, (float)ya, (float)zb), new((float)xa, (float)ya, (float)zb), roofSide, -Vector3.UnitY);
            Q(new((float)xa, (float)ya, (float)za), new((float)xb, (float)ya, (float)za), new((float)xb, (float)yb, (float)za), new((float)xa, (float)yb, (float)za), roofSide, -Vector3.UnitZ);
            Q(new((float)xa, (float)ya, (float)zb), new((float)xb, (float)ya, (float)zb), new((float)xb, (float)yb, (float)zb), new((float)xa, (float)yb, (float)zb), roofSide, Vector3.UnitZ);
            Q(new((float)xa, (float)ya, (float)za), new((float)xa, (float)ya, (float)zb), new((float)xa, (float)yb, (float)zb), new((float)xa, (float)yb, (float)za), roofSide, -Vector3.UnitX);
            Q(new((float)xb, (float)ya, (float)za), new((float)xb, (float)ya, (float)zb), new((float)xb, (float)yb, (float)zb), new((float)xb, (float)yb, (float)za), roofSide, Vector3.UnitX);
            roofBoxes.Add((xa, ya, za, xb, yb, zb));
            }
            roofs++;
        }

        // ---- the bodies: each room's faces (a few bodies where they are many), each a decor nothing touches
        var oblBytes = File.ReadAllBytes(Path.Combine(sourceDirectory, SourceObl));
        var next = HqrArchive.CountEntries(Path.Combine(sourceDirectory, SourceObl));
        var newBodies = new List<byte[]>();
        int faceCount = 0, bodyCount = 0, decorsLeft = 0;
        foreach (var (ti, faces) in roomFaces)
            foreach (var chunk in Chunks(faces))
            {
                var lo = new Vector3(float.MaxValue); var hi = new Vector3(float.MinValue);
                foreach (var f in chunk) foreach (var p in f.Quad) { lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
                // (the origin at the middle of the chunk, at its foot: every point within what a body holds)
                var origin = new Vector3((lo.X + hi.X) / 2, (lo.Y + hi.Y) / 2, (lo.Z + hi.Z) / 2);
                var pts = new List<Vector3>(); var bodyFaces = new List<Face>();
                var shared = new Dictionary<Vector3, int>();
                foreach (var (quad, colour, normal) in chunk)
                {
                    var ids = quad.Select(p => { if (!shared.TryGetValue(p, out var i)) { pts.Add(p - origin); shared[p] = i = pts.Count - 1; } return i; }).ToArray();
                    // (wound so the plain cross product points the way it faces: outwards)
                    var n = Vector3.Cross(quad[1] - quad[0], quad[2] - quad[0]);
                    if (Vector3.Dot(n, normal) < 0) Array.Reverse(ids);
                    bodyFaces.Add(new Face(ids, colour, Material: 0));
                }
                var body = next + newBodies.Count;
                if (pts.Any(p => Math.Abs(p.X) > 32767 || Math.Abs(p.Y) > 32767 || Math.Abs(p.Z) > 32767))
                    throw new InvalidDataException($"room {area.Tiles[ti].Scene}: a body of {chunk.Count} faces from {lo} to {hi} is bigger than a body holds");
                newBodies.Add(RaceTrackPipes.Write(pts, bodyFaces, lit: false));
                faceCount += chunk.Count; bodyCount++;
                if (IslandDecors.Locate(island, origin.X, origin.Z) is not { } at || at.Cube.Decors.Count >= IslandDecors.MaxPerCube) { decorsLeft++; continue; }
                var d = IslandDecors.Blank(body, at.X, (int)Math.Round(origin.Y), at.Z);
                d.XMin = (int)Math.Floor(at.X + lo.X - origin.X); d.XMax = (int)Math.Ceiling(at.X + hi.X - origin.X);
                d.ZMin = (int)Math.Floor(at.Z + lo.Z - origin.Z); d.ZMax = (int)Math.Ceiling(at.Z + hi.Z - origin.Z);
                d.YMin = (int)Math.Floor(lo.Y); d.YMax = NoBoxTop;     // (its Y the middle of its points: the body is drawn round it)
                at.Cube.Decors.Add(d);
            }

        // ---- the collision: the filled cells merged into boxes, and the roofs, each a decor with an empty body
        var empty = next + newBodies.Count;
        newBodies.Add(RaceTrackPipes.Write(new List<Vector3> { Vector3.Zero }, new List<Face>(), lit: false));
        var boxes = Boxes(cells.Keys.ToHashSet())
            .Select(b => (WX(b.X0), WY(b.Y0), WZ(b.Z0), WX(b.X1 + 1), WY(b.Y1 + 1), WZ(b.Z1 + 1))).Concat(roofBoxes).ToList();
        var boxCount = 0;
        foreach (var (bx0, by0, bz0, bx1, by1, bz1) in boxes)
        {
            double cx = (bx0 + bx1) / 2, cz = (bz0 + bz1) / 2;
            if (IslandDecors.Locate(island, cx, cz) is not { } at || at.Cube.Decors.Count >= IslandDecors.MaxPerCube) { decorsLeft++; continue; }
            var d = IslandDecors.Blank(empty, at.X, (int)Math.Round(by0), at.Z);
            d.XMin = (int)Math.Floor(at.X + bx0 - cx); d.XMax = (int)Math.Ceiling(at.X + bx1 - cx);
            d.ZMin = (int)Math.Floor(at.Z + bz0 - cz); d.ZMax = (int)Math.Ceiling(at.Z + bz1 - cz);
            d.YMin = (int)Math.Floor(by0); d.YMax = (int)Math.Ceiling(by1);
            at.Cube.Decors.Add(d);
            boxCount++;
        }
        foreach (var cube in island.Cubes.Values) cube.Info[IslandCube.InfoNbDecors] = cube.Decors.Count;

        // ---- written
        Directory.CreateDirectory(outDirectory);
        island.Save(Path.Combine(outDirectory, IleFile));
        foreach (var b in newBodies) oblBytes = HqrWriter.AppendEntry(oblBytes, HqrWriter.StoredEntry(b));
        File.WriteAllBytes(Path.Combine(outDirectory, OblFile), oblBytes);
        log.Add($"{IleFile}: {cubesX} x {cubesZ} cubes from cube ({FirstCube}, {FirstCube}), a paved plateau at {Floor} round the palace, the sea round it; " +
                $"the palace {Across} times a brick across and {Up} times a layer up ({(maxX - minX + 1) * Across} x {(maxZ - minZ + 1) * Across} cells)");
        log.Add($"{OblFile}: {bodyCount} bodies of the rooms ({faceCount} faces, {roofs} roofs among them) and an empty one for the {boxCount} collision boxes, from body {next} on" +
                (decorsLeft > 0 ? $"; WARNING: {decorsLeft} decors left out (a cube full)" : ""));
        return new Built(area.Tiles.Count, cells.Count, bodyCount, faceCount, boxCount, roofs, (FirstCube, FirstCube), (cubesX, cubesZ), string.Join("\n", log));
    }

    private static readonly Dictionary<byte[], bool> SixBit = new(ReferenceEqualityComparer.Instance);
    // Tools > LBA2: Palace island -- into the game folder (its own new files: no backups), from the folder's own palace scenes and Otringal.
    public static bool IsInstalled(string gameDirectory) => File.Exists(Path.Combine(gameDirectory, IleFile));
    public static List<string> Add(string gameDirectory) => new() { Build(gameDirectory, gameDirectory).Log };
    public static List<string> Remove(string gameDirectory)
    {
        var log = new List<string>();
        foreach (var f in new[] { IleFile, OblFile })
        {
            var path = Path.Combine(gameDirectory, f);
            if (File.Exists(path)) { File.Delete(path); log.Add($"{f} deleted"); }
        }
        return log;
    }

    // A colour of a 256-entry palette (6-bit, as the islands' are, or 8-bit), as the exporter reads them (Export/Palettes).
    private static (byte R, byte G, byte B) Colour(byte[] palette, int index)
    {
        var i = index * 3;
        if (i + 2 >= palette.Length) return (200, 200, 200);
        if (!SixBit.TryGetValue(palette, out var six)) SixBit[palette] = six = palette.Take(Math.Min(768, palette.Length)).All(v => v < 64);
        byte S(byte v) => (byte)Math.Min(255, six ? v * 4 : v);
        return (S(palette[i]), S(palette[i + 1]), S(palette[i + 2]));
    }

    // The mean colour of a brick's drawn pixels (LBA_BKG's run-length pictures: width, lines, hot spot, then per line its runs), as the
    // exporter's blocky maps colour their boxes (Export/Meshers GridMesher).
    private static (float R, float G, float B) BrickAverage(byte[]? data, byte[] palette)
    {
        if (data is null || data.Length < 4) return (150, 150, 150);
        double r = 0, g = 0, b = 0; var n = 0;
        var src = 4;
        int lines = data[1];
        for (var line = 0; line < lines && src < data.Length; line++)
        {
            int runs = data[src++];
            for (var run = 0; run < runs && src < data.Length; run++)
            {
                var control = data[src++];
                var count = (control & 0x3F) + 1;
                switch (control >> 6)
                {
                    case 0: break;
                    case 1:
                        for (var k = 0; k < count && src < data.Length; k++) { var c = Colour(palette, data[src++]); r += c.R; g += c.G; b += c.B; n++; }
                        break;
                    default:
                        if (src >= data.Length) break;
                        var colour = Colour(palette, data[src++]);
                        r += colour.R * count; g += colour.G * count; b += colour.B * count; n += count;
                        break;
                }
            }
        }
        return n == 0 ? (150, 150, 150) : ((float)(r / n), (float)(g / n), (float)(b / n));
    }

    // A room's faces in bodies the engine takes: as many as fit under MaxFaces polygons and MaxPoints points (the corners they share counted
    // once), in the order they come.
    private static IEnumerable<List<(Vector3[] Quad, int Colour, Vector3 Normal)>> Chunks(List<(Vector3[] Quad, int Colour, Vector3 Normal)> faces)
    {
        var chunk = new List<(Vector3[] Quad, int Colour, Vector3 Normal)>();
        var points = new HashSet<Vector3>();
        var lo = new Vector3(float.MaxValue); var hi = new Vector3(float.MinValue);
        // (in order across the room: a body's faces near one another)
        foreach (var f in faces.OrderBy(f => Math.Floor(f.Quad[0].Z / 20000)).ThenBy(f => f.Quad[0].X).ThenBy(f => f.Quad[0].Z))
        {
            var fresh = f.Quad.Count(p => !points.Contains(p));
            var flo = f.Quad.Aggregate(lo, Vector3.Min); var fhi = f.Quad.Aggregate(hi, Vector3.Max);
            var span = fhi - flo;
            if (chunk.Count > 0 && (chunk.Count + 1 > MaxFaces || points.Count + fresh > MaxPoints || span.X > MaxSpan || span.Z > MaxSpan || span.Y > MaxSpan))
            {
                yield return chunk;
                chunk = new(); points.Clear();
                lo = new Vector3(float.MaxValue); hi = new Vector3(float.MinValue);
                flo = f.Quad.Aggregate(lo, Vector3.Min); fhi = f.Quad.Aggregate(hi, Vector3.Max);
            }
            chunk.Add(f);
            lo = flo; hi = fhi;
            foreach (var p in f.Quad) points.Add(p);
        }
        if (chunk.Count > 0) yield return chunk;
    }

    // A plane's faces merged into rectangles of one colour (u0, v0, u1, v1 inclusive): grown along u, then along v as far as the whole row
    // matches.
    private static IEnumerable<(int U0, int V0, int U1, int V1, int Colour)> Rectangles(Dictionary<(int U, int V), int> plane)
    {
        var done = new HashSet<(int, int)>();
        foreach (var (u, v) in plane.Keys.OrderBy(k => k.V).ThenBy(k => k.U))
        {
            if (done.Contains((u, v))) continue;
            var colour = plane[(u, v)];
            var u1 = u;
            while (u1 - u + 1 < MaxRun && plane.TryGetValue((u1 + 1, v), out var c) && c == colour && !done.Contains((u1 + 1, v))) u1++;
            var v1 = v;
            while (v1 - v + 1 < MaxRun)
            {
                var row = v1 + 1; var ok = true;
                for (var k = u; k <= u1 && ok; k++) ok = plane.TryGetValue((k, row), out var c) && c == colour && !done.Contains((k, row));
                if (!ok) break;
                v1 = row;
            }
            for (var j = v; j <= v1; j++) for (var k = u; k <= u1; k++) done.Add((k, j));
            yield return (u, v, u1, v1, colour);
        }
    }

    // The filled cells merged into boxes (x0, y0, z0, x1, y1, z1 inclusive): grown along x, then z, then y while every cell is filled.
    private static List<(int X0, int Y0, int Z0, int X1, int Y1, int Z1)> Boxes(HashSet<(int X, int Y, int Z)> filled)
    {
        var done = new HashSet<(int, int, int)>();
        var result = new List<(int, int, int, int, int, int)>();
        bool Free((int, int, int) c) => filled.Contains(c) && !done.Contains(c);
        foreach (var (x, y, z) in filled.OrderBy(c => c.Y).ThenBy(c => c.Z).ThenBy(c => c.X))
        {
            if (done.Contains((x, y, z))) continue;
            var x1 = x;
            while (Free((x1 + 1, y, z))) x1++;
            var z1 = z;
            while (Enumerable.Range(x, x1 - x + 1).All(k => Free((k, y, z1 + 1)))) z1++;
            var y1 = y;
            while (Enumerable.Range(x, x1 - x + 1).All(k => Enumerable.Range(z, z1 - z + 1).All(j => Free((k, y1 + 1, j))))) y1++;
            for (var a = x; a <= x1; a++) for (var b = y; b <= y1; b++) for (var c = z; c <= z1; c++) done.Add((a, b, c));
            result.Add((x, y, z, x1, y1, z1));
        }
        return result;
    }

    // The ground: every cell painted with Otringal's courtyard paving, its cubes' settings Otringal's palace cube's; flat at the floor round
    // the palace (just under it), falling to the sea over the last Slope cells of the margin.
    private static void Ground(IslandFile island, IslandFile source, List<(int X, int Z)> cubes, double x0, double z0, double width, double depth)
    {
        var sample = IslandGround.Pick(source, PavingCell.Gx, PavingCell.Gz, 0);
        var settings = source.CubeAt(PavingCell.Gx / 64, PavingCell.Gz / 64);
        double gx0 = x0 / 512 - (Margin - Slope), gz0 = z0 / 512 - (Margin - Slope), gx1 = (x0 + width) / 512 + (Margin - Slope), gz1 = (z0 + depth) / 512 + (Margin - Slope);
        foreach (var (cx, cz) in cubes)
        {
            if (island.CubeAt(cx, cz) is not { } cube) continue;
            if (settings is not null) cube.Info = (int[])settings.Info.Clone();
            for (var vz = 0; vz < IslandCube.Vertices; vz++)
                for (var vx = 0; vx < IslandCube.Vertices; vx++)
                {
                    double gx = cx * 64 + vx, gz = cz * 64 + vz;
                    var outside = Math.Max(Math.Max(gx0 - gx, gx - gx1), Math.Max(gz0 - gz, gz - gz1));
                    var h = outside <= 0 ? Floor - 60 : outside >= Slope ? 0 : (Floor - 60) * (1 - outside / Slope);
                    cube.Heights[vz * IslandCube.Vertices + vx] = (short)Math.Round(h);
                    cube.Intensity[vz * IslandCube.Vertices + vx] = 10;
                }
            if (sample is null) continue;
            var region = new CubeCells(cx, cz);
            IslandGround.Paint(island, region, sample, PolygonFields.Texture | PolygonFields.GameCode);
        }
    }

    // A cube's cells, as a region to paint.
    private sealed class CubeCells : IslandRegion
    {
        private readonly int cx, cz;
        public CubeCells(int cx, int cz) { this.cx = cx; this.cz = cz; }
        public override IEnumerable<(int Gx, int Gz, double Weight)> Vertices(IslandFile island)
        {
            for (var z = 0; z < 64; z++) for (var x = 0; x < 64; x++) yield return (cx * 64 + x, cz * 64 + z, 1.0);
        }
        public override (double Gx, double Gz) Center => (cx * 64 + 32, cz * 64 + 32);
    }
}
