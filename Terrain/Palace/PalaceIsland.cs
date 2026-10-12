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
//
// 2026-10-12 (the user: "The Palace island we created needs work. We need to add in the outside of the palace as featured in Otringal.ile
// ... Let's also see if for each inside room of the palace we can recreate the decor that each room naturally has"):
//   - the outside (PalaceOutside): Otringal's own palace -- its domed halls, towers and wings, OTRINGAL.OBL's bodies -- made big and stood on
//     the sixteen rooms' roof, the arch round the door out; the walls' outer faces in its colours (a teal foot, its pale stone, a turquoise
//     top) instead of the rooms' wallpaper seen from behind, and the roofs in its stone;
//   - the rooms' near walls: in the game the two walls nearest the camera are bricks with nothing drawn (they would hide the room), so they
//     came out a plain grey; here they are the room's wallpaper, layer by layer the colour its other walls have at that height;
//   - the walls made whole: the gaps the isometric pictures hide (beside a fireplace, under a wall) that let a room see the outside, and
//     the pockets no room reaches, filled -- the door out of room 154 left open;
//   - each room's furniture (PalaceFurniture): what its bricks picture -- columns, plants, lamps, benches, fireplaces, candelabras, portraits,
//     windows -- and its chests (the scenes' sprites), as 3D bodies where the bricks were, instead of boxes in their average colour.
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
    public const int FirstCube = 6;                     // the island's cubes from (6, 6) on
    public const int HallScene = 80;                    // the long last room: the others are the sixteen under Otringal's palace
    // Otringal's paved palace courtyard, whose ground the plateau is painted with (an island cell)
    private static readonly (int Gx, int Gz) PavingCell = (494, 474);
    // the colours of Otringal's palace (OTRINGAL.OBL's halls, in its palette): its pale stone (walls and flat roofs), the dark teal of its
    // foot, the turquoise of its drums
    private static readonly (float R, float G, float B) Stone = (176, 164, 160), StoneShadow = (160, 144, 140), Teal = (0, 48, 60), Turquoise = (116, 156, 160);
    private const int FootLayers = 3;                   // the walls' outer faces: this many layers of teal at the foot ...
    private const int TopLayers = 1;                    // ... and this many of turquoise at the top, the stone between

    public sealed record Built(int Rooms, int Cells, int Bodies, int Faces, int Boxes, int Roofs, (int X, int Z) FirstCube, (int X, int Z) Cubes, string Log);

    // Builds PALACE.ILE and PALACE.OBL into `outDirectory` from the game folder's palace scenes and Otringal's island files (`sourceDirectory`:
    // where pristine OTRINGAL.ILE/.OBL are, the game folder itself by default).
    // (`roofed`: false leaves the roofs off -- a look inside, and the outside with them; `outside` false leaves Otringal's palace off the
    // roof, `furnished` false the rooms' furniture out)
    public static Built Build(string gameDirectory, string outDirectory, string? sourceDirectory = null, bool roofed = true, bool outside = true, bool furnished = true)
    {
        sourceDirectory ??= gameDirectory;
        var log = new List<string>();
        // ---- the rooms' cells, as the joined map places them (without its separations)
        var interiors = new Lba2Interiors(gameDirectory);
        var area = Lba2Areas.Find(interiors.LoadScene).FirstOrDefault(a => a.Tiles.Any(t => t.Scene == 151))
                   ?? throw new InvalidDataException("The game folder has no Emperor's palace (scenes 151-166 and 80).");
        var cells = new Dictionary<(int X, int Y, int Z), (int Brick, int Tile)>();
        // (each room's place -- the cell its scene's (0, 0, 0) is at -- and the columns its scene's cells cover)
        var offsets = new (int X, int Y, int Z)[area.Tiles.Count];
        var extents = new (int X0, int Z0, int X1, int Z1)[area.Tiles.Count];
        for (var ti = 0; ti < area.Tiles.Count; ti++)
        {
            var tile = area.Tiles[ti];
            var sep = Lba2Areas.Separations.Where(s => s.Scene == tile.Scene).Select(s => (s.Dx, s.Dy, s.Dz)).FirstOrDefault();
            int ox = tile.OffsetX / 512 - sep.Dx, oy = tile.OffsetY / 256 - sep.Dy, oz = tile.OffsetZ / 512 - sep.Dz;
            offsets[ti] = (ox, oy, oz);
            extents[ti] = (int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);
            foreach (var p in interiors.Placements(tile))
            {
                cells.TryAdd((p.X + ox, p.Y + oy, p.Z + oz), (p.Brick, ti));
                var e = extents[ti];
                extents[ti] = (Math.Min(e.X0, p.X + ox), Math.Min(e.Z0, p.Z + oz), Math.Max(e.X1, p.X + ox), Math.Max(e.Z1, p.Z + oz));
            }
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
        int Nearest((float R, float G, float B) c) => BrickBuilding.Nearest(islandPalette, c, matched);
        (float R, float G, float B) Average(int brick)
        {
            if (!averages.TryGetValue(brick, out var a)) averages[brick] = a = BrickBuilding.BrickAverage(interiors.ReadBrick(brick), palette);
            return a;
        }
        int Shaded((float R, float G, float B) c, float shade) => Nearest((MathF.Round(c.R * shade), MathF.Round(c.G * shade), MathF.Round(c.B * shade)));
        int ColourOf(int brick, float shade) => Shaded(Average(brick), shade);
        var drawn = new Dictionary<int, bool>();
        bool Drawn(int brick) => drawn.TryGetValue(brick, out var known) ? known : drawn[brick] = HasPixels(interiors.ReadBrick(brick));

        // ---- the rooms' furniture (PalaceFurniture): its blocks' placements -- a standing piece's cells drawn by its model, not as boxes; a
        // wall piece's (a portrait, the round window, a window) still the wall, in the wallpaper, its picture on it -- and the chests
        var pieces = furnished ? PalaceFurniture.Find(gameDirectory, area, offsets, cells) : new List<PalaceFurniture.Piece>();
        var chests = furnished ? PalaceFurniture.Chests(interiors, area, offsets) : new List<PalaceFurniture.Chest>();
        var modelled = pieces.Where(p => p.Standing).SelectMany(p => p.Cells).ToHashSet();
        var papered = pieces.Where(p => !p.Standing).SelectMany(p => p.Cells).ToHashSet();
        var shown = modelled.Count == 0 ? cells : cells.Where(c => !modelled.Contains(c.Key)).ToDictionary(c => c.Key, c => c.Value);

        // ---- the air: the rooms' -- from each room's middle over the empty cells under the roof (the furniture's among them), within the
        // rooms' columns but their outer ring (the grids leave gaps under some walls: the outside beyond them is not a room) -- and the rest,
        // the outside's: an upright face with a room's air in front of it is a room's wall, one with the outside in front of it the palace's
        var air = new HashSet<(int X, int Y, int Z)>();
        bool InRooms(int x, int z) => extents.Any(e => x > e.X0 && x < e.X1 && z > e.Z0 && z < e.Z1);
        var queue = new Queue<(int X, int Y, int Z)>();
        foreach (var e in extents)
        {
            int mx = (e.X0 + e.X1) / 2, mz = (e.Z0 + e.Z1) / 2;
            for (var y = minY; y <= maxY; y++)
                if (!shown.ContainsKey((mx, y, mz))) { if (air.Add((mx, y, mz))) queue.Enqueue((mx, y, mz)); break; }
        }
        while (queue.Count > 0)
        {
            var (x, y, z) = queue.Dequeue();
            foreach (var (dx, dy, dz, _) in BrickBuilding.Directions)
            {
                var n = (x + dx, y + dy, z + dz);
                if (n.Item2 < minY || n.Item2 > maxY || !InRooms(n.Item1, n.Item3) || shown.ContainsKey(n) || !air.Add(n)) continue;
                queue.Enqueue(n);
            }
        }
        // ---- the walls made whole: the grids leave gaps -- by a fireplace, under a wall -- that their isometric picture hides behind what
        // stands in front, and pockets no room reaches. Where a room's air meets the outside's beside it, the outside's cell becomes a wall;
        // and every empty cell within the rooms that isn't a room's air (a shaft between rooms, a hollow under a wall) is filled: drawn as
        // walls (the room's wallpaper inside, the palace's stone outside) and in the collision. The door out stays open.
        var door = DoorZone(interiors, area, offsets, extents);
        var whole = new HashSet<(int X, int Y, int Z)>();
        foreach (var (x, y, z) in air)
            foreach (var (dx, dy, dz, _) in BrickBuilding.Directions)
                if (dy == 0) whole.Add((x + dx, y, z + dz));
        for (var x = minX; x <= maxX; x++)
            for (var z = minZ; z <= maxZ; z++)
                if (InRooms(x, z))
                    for (var y = minY; y <= maxY; y++) whole.Add((x, y, z));
        var gaps = 0;
        foreach (var n in whole)
        {
            if (cells.ContainsKey(n) || air.Contains(n)) continue;
            if (door is { } d && n.X >= d.X0 && n.X <= d.X1 && n.Z >= d.Z0 && n.Z <= d.Z1) continue;
            var tile = Array.FindIndex(extents, e => n.X >= e.X0 && n.X <= e.X1 && n.Z >= e.Z0 && n.Z <= e.Z1);
            cells[n] = (-1, Math.Max(0, tile));
            if (!ReferenceEquals(shown, cells)) shown[n] = (-1, Math.Max(0, tile));
            gaps++;
        }
        if (gaps > 0) log.Add($"the walls made whole: {gaps} cells of gaps to the outside and pockets no room reaches filled");
        // ---- the rooms' wallpaper, layer by layer: the mean colour of the drawn bricks of the walls (columns filled most of the way up)
        // that face a room's air -- what the near walls, bricks with nothing drawn, take
        var wallColumns = cells.Keys.GroupBy(c => (c.X, c.Z)).Where(g => g.Count() >= (maxY - minY + 1) * 6 / 7).Select(g => g.Key).ToHashSet();
        var sums = new Dictionary<int, (double R, double G, double B, int N)>();
        foreach (var ((x, y, z), (brick, _)) in cells)
        {
            if (!wallColumns.Contains((x, z)) || !Drawn(brick)) continue;
            foreach (var (dx, dy, dz, _) in BrickBuilding.Directions)
                if (dy == 0 && air.Contains((x + dx, y, z + dz)))
                {
                    var a = Average(brick); var s = sums.GetValueOrDefault(y);
                    sums[y] = (s.R + a.R, s.G + a.G, s.B + a.B, s.N + 1);
                }
        }
        var wallpaper = sums.ToDictionary(s => s.Key, s => ((float)(s.Value.R / s.Value.N), (float)(s.Value.G / s.Value.N), (float)(s.Value.B / s.Value.N)));
        (float R, float G, float B) WallpaperAt(int y) =>
            wallpaper.TryGetValue(y, out var w) ? w : wallpaper.Count > 0 ? wallpaper.MinBy(p => Math.Abs(p.Key - y)).Value : (150f, 150f, 150f);
        int FaceColour((int X, int Y, int Z) c, int d, int brick, float shade)
        {
            var (dx, dy, dz, _) = BrickBuilding.Directions[d];
            var outward = !air.Contains((c.X + dx, c.Y + dy, c.Z + dz));
            // (the palace's outside: Otringal's palace's walls, by height; and its stone on the top of a wall made whole -- a ledge)
            if (dy == 0 && outward)
                return Shaded(c.Y - minY < FootLayers ? Teal : c.Y > maxY - TopLayers ? Turquoise : Stone, shade);
            if (outward && !Drawn(brick)) return Shaded(Stone, shade);
            return Drawn(brick) && !papered.Contains(c) ? ColourOf(brick, shade) : Shaded(WallpaperAt(c.Y), shade);
        }

        // ---- the faces that show, merged into rectangles of one colour, room by room
        var roomFaces = Faces(shown, FaceColour, (x, y, z) => new Vector3((float)WX(x), (float)WY(y), (float)WZ(z)));
        foreach (var ti in Enumerable.Range(0, area.Tiles.Count)) roomFaces.TryAdd(ti, new List<BrickBuilding.Face>());

        // ---- the furniture's models, with each room's faces (in its bodies)
        var chestBoxes = new List<(double X0, double Y0, double Z0, double X1, double Y1, double Z1)>();
        if (pieces.Count + chests.Count > 0)
        {
            Vector3 Place(double x, double y, double z) => new((float)(x0 + (x - minX) * U), (float)(Floor + (y - minY) * V), (float)(z0 + (z - minZ) * U));
            // (seen from a way: shaded as the rooms' faces are -- top, bottom, east and west, south and north -- in between for the slopes)
            int Seen((float R, float G, float B) c, Vector3 n) => Shaded(c, n.Y * n.Y * (n.Y > 0 ? 1f : 0.5f) + n.X * n.X * 0.84f + n.Z * n.Z * 0.70f);
            // (the way a piece looks: away from its wall -- the side with the most of a room's air in front of it, three cells deep, less what
            // is behind it)
            (int Nx, int Nz) Looks(PalaceFurniture.Piece p)
            {
                var (ox, oy, oz) = p.Origin; var (sx, sy, sz) = p.Size;
                int AirOn(int nx, int nz)
                {
                    var n = 0;
                    for (var far = 0; far < 3; far++)
                        for (var y = oy; y < oy + sy; y++)
                            for (var i = 0; i < (nx != 0 ? sz : sx); i++)
                                if (air.Contains(nx > 0 ? (ox + sx + far, y, oz + i) : nx < 0 ? (ox - 1 - far, y, oz + i) : nz > 0 ? (ox + i, y, oz + sz + far) : (ox + i, y, oz - 1 - far))) n++;
                    return n;
                }
                return new[] { (1, 0), (0, 1), (-1, 0), (0, -1) }.MaxBy(d => AirOn(d.Item1, d.Item2) - AirOn(-d.Item1, -d.Item2));
            }
            var made = PalaceFurniture.Build(pieces, chests, Place, Seen, Looks, y => WallpaperAt(y));
            foreach (var (ti, list) in made.Faces) roomFaces[ti].AddRange(list);
            chestBoxes = made.Boxes;
            log.AddRange(made.Log);
        }

        // ---- the roofs: over each room, level with the top of its walls, in the stone of Otringal's palace's flat roofs
        var roofs = 0;
        var roofBoxes = new List<(double X0, double Y0, double Z0, double X1, double Y1, double Z1)>();
        var roofTops = new Dictionary<int, double>();
        foreach (var ti in roofed ? roomFaces.Keys.ToList() : new List<int>())
        {
            var mine = cells.Where(c => c.Value.Tile == ti).Select(c => c.Key).ToList();
            if (mine.Count == 0) continue;
            int rx0 = mine.Min(c => c.X), rx1 = mine.Max(c => c.X), rz0 = mine.Min(c => c.Z), rz1 = mine.Max(c => c.Z);
            // (the walls' top: the highest layer that the room's edge cells reach, most of the way round)
            var edge = mine.Where(c => c.X == rx0 || c.X == rx1 || c.Z == rz0 || c.Z == rz1).GroupBy(c => (c.X, c.Z)).Select(g => g.Max(c => c.Y)).OrderBy(h => h).ToList();
            var top = edge.Count > 0 ? edge[(int)(edge.Count * 0.8)] + 1 : mine.Max(c => c.Y) + 1;
            double ya = WY(top), yb = ya + V * 0.4;
            roofTops[ti] = yb;
            int roofTop = Nearest(Stone), roofSide = Nearest(StoneShadow);
            var list = roomFaces[ti];
            // (in slabs of MaxRun cells at the most: a body holds no more)
            for (var sx = rx0; sx <= rx1; sx += BrickBuilding.MaxRun)
            for (var sz = rz0; sz <= rz1; sz += BrickBuilding.MaxRun)
            {
            double xa = WX(sx), xb = WX(Math.Min(rx1, sx + BrickBuilding.MaxRun - 1) + 1), za = WZ(sz), zb = WZ(Math.Min(rz1, sz + BrickBuilding.MaxRun - 1) + 1);
            BrickBuilding.AddBox(list, new((float)xa, (float)ya, (float)za), new((float)xb, (float)yb, (float)zb), roofTop, roofSide);
            roofBoxes.Add((xa, ya, za, xb, yb, zb));
            }
            roofs++;
        }

        // ---- the bodies: each room's faces (a few bodies where they are many), each a decor nothing touches
        var oblBytes = File.ReadAllBytes(Path.Combine(sourceDirectory, SourceObl));
        var next = HqrArchive.CountEntries(Path.Combine(sourceDirectory, SourceObl));
        var newBodies = new List<byte[]>();
        var (bodyCount, faceCount, decorsLeft) = BrickBuilding.AddBodies(island, roomFaces, newBodies, next, ti => $"room {area.Tiles[ti].Scene}");

        // ---- the outside: Otringal's palace on the sixteen rooms' roof, the arch round the door out
        var outsideBoxes = new List<(double X0, double Y0, double Z0, double X1, double Y1, double Z1)>();
        var outsideBodies = 0;
        if (outside && roofed)
        {
            var under = cells.Where(c => area.Tiles[c.Value.Tile].Scene != HallScene).Select(c => c.Key).ToList();
            var tops = roofTops.Where(r => area.Tiles[r.Key].Scene != HallScene).Select(r => r.Value).ToList();
            if (under.Count > 0 && tops.Count > 0)
            {
                var site = new PalaceOutside.Site(WX(under.Min(c => c.X)), WZ(under.Min(c => c.Z)), WX(under.Max(c => c.X) + 1), WZ(under.Max(c => c.Z) + 1),
                    tops.Max(), Floor - 60, U, V, door is { } d ? DoorOut(d, extents[d.Tile], WX, WZ) : null);
                var placed = PalaceOutside.Add(island, source, HqrArchive.Open(Path.Combine(sourceDirectory, SourceObl)), newBodies, next, site);
                outsideBoxes = placed.Boxes; outsideBodies = placed.Bodies;
                log.AddRange(placed.Log);
            }
        }

        // ---- the collision: the filled cells merged into boxes (the furniture's with them), the roofs, the chests and the outside's, each a
        // decor with an empty body
        var empty = next + newBodies.Count;
        newBodies.Add(RaceTrackPipes.Write(new List<Vector3> { Vector3.Zero }, new List<Face>(), lit: false));
        var boxes = BrickBuilding.Boxes(cells.Keys.ToHashSet())
            .Select(b => (WX(b.X0), WY(b.Y0), WZ(b.Z0), WX(b.X1 + 1), WY(b.Y1 + 1), WZ(b.Z1 + 1))).Concat(roofBoxes).Concat(chestBoxes).Concat(outsideBoxes).ToList();
        var (boxCount, boxesLeft) = BrickBuilding.AddBoxes(island, empty, boxes);
        decorsLeft += boxesLeft;
        foreach (var cube in island.Cubes.Values) cube.Info[IslandCube.InfoNbDecors] = cube.Decors.Count;
        var busiest = IslandOps.CubeCells(island).MaxBy(c => c.Cube.Decors.Count);

        // ---- written
        Directory.CreateDirectory(outDirectory);
        // (a build's own file replaced: no .bak of it left beside it)
        var ilePath = Path.Combine(outDirectory, IleFile);
        if (File.Exists(ilePath)) File.Delete(ilePath);
        island.Save(ilePath);
        foreach (var b in newBodies) oblBytes = HqrWriter.AppendEntry(oblBytes, HqrWriter.StoredEntry(b));
        File.WriteAllBytes(Path.Combine(outDirectory, OblFile), oblBytes);
        log.Add($"{IleFile}: {cubesX} x {cubesZ} cubes from cube ({FirstCube}, {FirstCube}), a paved plateau at {Floor} round the palace, the sea round it; " +
                $"the palace {Across} times a brick across and {Up} times a layer up ({(maxX - minX + 1) * Across} x {(maxZ - minZ + 1) * Across} cells)");
        log.Add($"{OblFile}: {bodyCount} bodies of the rooms ({faceCount} faces, {roofs} roofs and the furniture among them), " +
                (outsideBodies > 0 ? $"{outsideBodies} of the outside, " : "") + $"and an empty one for the {boxCount} collision boxes, from body {next} on" +
                (decorsLeft > 0 ? $"; WARNING: {decorsLeft} decors left out (a cube full)" : ""));
        log.Add($"the busiest cube: ({busiest.X}, {busiest.Z}), {busiest.Cube.Decors.Count} objects of the {IslandDecors.MaxPerCube} a cube holds");
        return new Built(area.Tiles.Count, cells.Count, bodyCount, faceCount, boxCount, roofs, (FirstCube, FirstCube), (cubesX, cubesZ), string.Join("\n", log));
    }

    // BrickBuilding.Faces, a face's colour told its cell and the way it faces too (the walls' outer faces are coloured apart from their
    // inner ones): the faces that show, merged into rectangles of one colour, group by group.
    private static Dictionary<int, List<BrickBuilding.Face>> Faces(Dictionary<(int X, int Y, int Z), (int Brick, int Tile)> cells,
        Func<(int X, int Y, int Z), int, int, float, int> colour, Func<int, int, int, Vector3> corner)
    {
        var planes = new Dictionary<(int Group, int Dir, int Plane), Dictionary<(int U, int V), int>>();
        foreach (var ((x, y, z), (brick, group)) in cells)
            for (var d = 0; d < BrickBuilding.Directions.Length; d++)
            {
                var (dx, dy, dz, shade) = BrickBuilding.Directions[d];
                if (cells.ContainsKey((x + dx, y + dy, z + dz))) continue;
                var key = (group, d, dy != 0 ? y : dx != 0 ? x : z);
                if (!planes.TryGetValue(key, out var plane)) planes[key] = plane = new();
                plane[dy != 0 ? (x, z) : dx != 0 ? (z, y) : (x, y)] = colour((x, y, z), d, brick, shade);
            }
        var faces = new Dictionary<int, List<BrickBuilding.Face>>();
        foreach (var ((group, d, at), plane) in planes)
        {
            var (dx, dy, dz, _) = BrickBuilding.Directions[d];
            var list = faces.TryGetValue(group, out var l) ? l : faces[group] = new();
            foreach (var (u0, v0, u1, v1, c) in BrickBuilding.Rectangles(plane))
            {
                Vector3 P(int u, int v) => dy != 0 ? corner(u, at + (dy > 0 ? 1 : 0), v)
                                       : dx != 0 ? corner(at + (dx > 0 ? 1 : 0), v, u)
                                                 : corner(u, v, at + (dz > 0 ? 1 : 0));
                list.Add(new BrickBuilding.Face(new[] { P(u0, v0), P(u1 + 1, v0), P(u1 + 1, v1 + 1), P(u0, v1 + 1) }, c, new Vector3(dx, dy, dz)));
            }
        }
        return faces;
    }

    // Whether a brick (LBA_BKG's run-length picture) has anything drawn: the near walls' bricks have nothing (BrickBuilding.BrickAverage's runs).
    private static bool HasPixels(byte[]? data)
    {
        if (data is null || data.Length < 4) return false;
        // (a line's runs: a skip has no bytes after it, and any other run draws -- so the first that isn't a skip answers)
        var src = 4;
        for (int line = 0, lines = data[1]; line < lines && src < data.Length; line++)
        {
            int runs = data[src++];
            for (var run = 0; run < runs && src < data.Length; run++)
                if (data[src++] >> 6 != 0) return true;
        }
        return false;
    }

    // The palace's door to the outside: a room's cube change to an island's ground at its floor (room 154's, which Otringal's palace door
    // leads to -- not the hall's round window, higher up), as the columns of the palace's cells it covers, the way out (the room's side it
    // is nearest) and the room. Null when there is none.
    private static (int X0, int X1, int Z0, int Z1, int Nx, int Nz, int Tile)? DoorZone(Lba2Interiors interiors, Lba1Area area, (int X, int Y, int Z)[] offsets,
        (int X0, int Z0, int X1, int Z1)[] extents)
    {
        for (var ti = 0; ti < area.Tiles.Count; ti++)
            foreach (var zone in interiors.LoadScene(area.Tiles[ti].Scene)?.Zones ?? new List<Scenes.SceneZoneModel>())
            {
                if (zone.Type != 0 || zone.Y0 > 512 || interiors.LoadScene(zone.Num) is not { CubeMode: 1 }) continue;
                int x0 = zone.X0 / 512 + offsets[ti].X, x1 = (zone.X1 - 1) / 512 + offsets[ti].X;
                int z0 = zone.Z0 / 512 + offsets[ti].Z, z1 = (zone.Z1 - 1) / 512 + offsets[ti].Z;
                var e = extents[ti];
                var sides = new[] { (e.Z1 - z1, 0, 1), (z0 - e.Z0, 0, -1), (e.X1 - x1, 1, 0), (x0 - e.X0, -1, 0) };
                var (_, nx, nz) = sides.MinBy(s => s.Item1);
                return (x0, x1, z0, z1, nx, nz, ti);
            }
        return null;
    }

    // ... as its middle on the wall's outer face (the room's side), the way out and its width (world units).
    private static (double X, double Z, int Nx, int Nz, double Width) DoorOut((int X0, int X1, int Z0, int Z1, int Nx, int Nz, int Tile) door,
        (int X0, int Z0, int X1, int Z1) room, Func<int, double> wx, Func<int, double> wz) =>
        door.Nz != 0
            ? ((wx(door.X0) + wx(door.X1 + 1)) / 2, door.Nz > 0 ? wz(room.Z1 + 1) : wz(room.Z0), 0, door.Nz, wx(door.X1 + 1) - wx(door.X0))
            : (door.Nx > 0 ? wx(room.X1 + 1) : wx(room.X0), (wz(door.Z0) + wz(door.Z1 + 1)) / 2, door.Nx, 0, wz(door.Z1 + 1) - wz(door.Z0));

    // Tools > LBA2: Palace island -- into the game folder (its own new files: no backups), from the folder's own palace scenes and Otringal.
    public static bool IsInstalled(string gameDirectory) => File.Exists(Path.Combine(gameDirectory, IleFile));
    public static List<string> Add(string gameDirectory) => new() { Build(gameDirectory, gameDirectory).Log };
    public static List<string> Remove(string gameDirectory)
    {
        var log = new List<string>();
        foreach (var f in new[] { IleFile, OblFile, IleFile + ".bak" })
        {
            var path = Path.Combine(gameDirectory, f);
            if (File.Exists(path)) { File.Delete(path); log.Add($"{f} deleted"); }
        }
        return log;
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
            var region = new BrickBuilding.CubeCells(cx, cz);
            IslandGround.Paint(island, region, sample, PolygonFields.Texture | PolygonFields.GameCode);
        }
    }
}
