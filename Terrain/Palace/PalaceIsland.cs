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
//   - the island: OTRINGAL.ILE's atlases and palette (so the game draws it as Otringal when it is raced -- the race-track mode's island_file=).
// PALACE.OBL is OTRINGAL.OBL with the palace's bodies on its end.
//
// 2026-10-12 (the user: "The Palace island we created needs work. We need to add in the outside of the palace as featured in Otringal.ile
// ... Let's also see if for each inside room of the palace we can recreate the decor that each room naturally has"):
//   - the outside (PalaceOutside): Otringal's own palace -- its domed halls, towers and wings, OTRINGAL.OBL's bodies -- made big and stood on
//     the sixteen rooms' roof; the walls' outer faces in its colours (a teal foot, its pale stone, a turquoise top);
//   - the rooms' near walls: in the game the two walls nearest the camera are bricks with nothing drawn (they would hide the room), so they
//     came out a plain grey; here they are the room's wallpaper, layer by layer the colour its other walls have at that height;
//   - the walls made whole: the gaps the isometric pictures hide (beside a fireplace, under a wall) that let a room see the outside, and
//     the pockets no room reaches, filled;
//   - each room's furniture (PalaceFurniture): what its bricks picture -- columns, plants, lamps, benches, fireplaces, candelabras, portraits,
//     windows -- and its chests (the scenes' sprites), as 3D bodies where the bricks were, instead of boxes in their average colour.
//
// 2026-10-12, later (the user, with a picture of Otringal's palace from above: "Our new palace has a weird ground area that we should get rid
// of. The original palace is sat on a raised cliff area ... We need to make the outside look more like the original. Perhaps scaling up the
// roof section, or adding more towers to the roof section. It's okay for the inside to be underground. We may also need to scale up the
// size of the inside of the palace so we can drive through the rooms"; the race through it: "through the palace rooms, coming out the window
// and around the roof tops ... Point-to-point ... finishing with massive jump to harbor teleport"):
//   - the rooms Across = 4 times a brick across (they were 3): a door between two rooms, two bricks, 4,096 wide for a car whose box is some
//     1,300 across (the race cars'), a room some 27,000 across;
//   - the hill (PalaceCliff) instead of the paved plateau: the rooms are under it, their roofs its top -- rock-topped, Otringal's mossy rock --
//     and round them its plateau, its rim of rocks and its cliffs falling to the cloud sea, as Otringal's palace hill; the palace's front door
//     (room 154's), in the hill now, closed;
//   - the long hall's end stands on the cliff's edge, its round window (the one Twinsen leaves by) open through the wall onto the sky -- the
//     cars' way out -- the big chest under it gone;
//   - on the roofs, Otringal's palace bigger, its arch in front of its porch, and a wing of towers on the long hall's roof (PalaceOutside);
//   - every body and box in every cube it reaches (the engine draws and collides only the decors of the cube the hero is in), as many a
//     cube as this editor's engine takes;
//   - east of the hill, a harbour: a quay at its foot and, out in the bay, a rock pillar level with the hilltop -- the teleporter's.
internal static class PalaceIsland
{
    public const string IleFile = "PALACE.ILE", OblFile = "PALACE.OBL";
    // where things are on the island, for the race track built on it (its plan's design, tools/RaceTrackPlan/palace_design.py)
    public const string LayoutFile = "PALACE.JSON";
    public const string SourceIle = "OTRINGAL.ILE", SourceObl = "OTRINGAL.OBL";
    public const int PaletteEntry = 31;                 // RESS.HQR: Otringal's
    public const int Across = 4;                        // a brick's 512 across, this many times
    public const double Up = 2.5;                       // a layer's 256 up, this many times
    public const int Floor = 10000;                     // the rooms' bottom layer's foot, over the island's zero: the hilltop over them some 19,000
                                                        // (Otringal's palace hill is 17,000 high, over a much smaller top)
    public const int HallScene = 80;                    // the long last room: the others are the sixteen under Otringal's palace
    public const int HarbourCells = 70;                 // cells east of the hill's cubes for the harbour's bay
    public const int NorthCells = 50;                   // and north of it, for the road out of the round window to loop round over the sea
    public const double QuayY = 1400;                   // the harbour's quay (Otringal's lowest)
    public const int PillarOut = 80;                    // the teleporter's pillar: cells east of the rooms' east wall ...
    public const double PillarRadius = 6;               // ... its top's radius (cells) ...
    public const double PillarRise = 700;               // ... and its top this much over the hilltop: the race's jump lands as high as its lip
                                                        // (the road's 60 over the hilltop, its ramp's 700 -- palace_design.py), on the pillar
    public const double RoadOver = 60;                  // (the race's road over what it lies on)
    public const double CorridorHalf = 9;               // the ways across the hilltop's rocks: half their width (cells)
    // the colours of Otringal's palace (OTRINGAL.OBL's halls, in its palette): its pale stone (walls and flat roofs), the dark teal of its
    // foot, the turquoise of its drums
    private static readonly (float R, float G, float B) Stone = (176, 164, 160), StoneShadow = (160, 144, 140), Teal = (0, 48, 60), Turquoise = (116, 156, 160);
    private const int FootLayers = 3;                   // the walls' outer faces: this many layers of teal at the foot ...
    private const int TopLayers = 1;                    // ... and this many of turquoise at the top, the stone between
    // the roofs' rock: OTRINGAL.OBL's texture page's mossy rock (64 x 64 pixels from (192, 192), its rock objects') -- the hill's ground's rock
    // at half its size, so 32 world units a pixel here, as the ground's 128 pixels are spread over four cells
    private const uint RoofRock = 0x3F3F0000u | (192 * 256 + 192);
    private const double RockSpan = 8192;               // world units of one roof polygon (256 pixels of the tile, repeated)

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
        var hallTile = Enumerable.Range(0, area.Tiles.Count).FirstOrDefault(ti => area.Tiles[ti].Scene == HallScene, -1);

        // ---- the island: its cubes round the palace's hill (the hill's plateau, rim and cliffs, and some sea), the harbour's bay east of it
        const double U = 512.0 * Across, V = 256.0 * Up;
        const int Pad = PalaceCliff.Margin + PalaceCliff.Wobble + PalaceCliff.Face + PalaceCliff.FarSea;
        int spanX = (maxX - minX + 1) * Across, spanZ = (maxZ - minZ + 1) * Across;
        int cubesX = (Pad + spanX + Pad + HarbourCells + 63) / 64, cubesZ = (Pad + NorthCells + spanZ + Pad + 63) / 64;
        if (cubesX > IslandFile.MapSize || cubesZ > IslandFile.MapSize) throw new InvalidDataException($"The palace's hill needs {cubesX} x {cubesZ} cubes: more than the map holds.");
        int firstX = (IslandFile.MapSize - cubesX) / 2, firstZ = (IslandFile.MapSize - cubesZ) / 2;
        // (world units of the palace's first cell's west and north faces: on a cell's edge, Pad cells into the first cube -- and NorthCells more
        // to the north)
        double x0 = (firstX * 64 + Pad) * 512.0, z0 = (firstZ * 64 + Pad + NorthCells) * 512.0;
        double WX(int x) => x0 + (x - minX) * U;
        double WZ(int z) => z0 + (z - minZ) * U;
        double WY(int y) => Floor + (y - minY) * V;
        var source = IslandFile.Load(Path.Combine(sourceDirectory, SourceIle));
        var cubeList = new List<(int X, int Z)>();
        for (var cz = 0; cz < cubesZ; cz++) for (var cx = 0; cx < cubesX; cx++) cubeList.Add((firstX + cx, firstZ + cz));
        var island = IslandFile.Parse(IslandScaler.NewFile(source, cubeList), Path.Combine(outDirectory, IleFile));
        // (the cubes' settings: Otringal's palace cube's -- its light, fog and sky; PalaceCliff raises the sky and lets the sea show)
        if (source.CubeAt(PalaceOutside.SourceCube.X, PalaceOutside.SourceCube.Z) is { } settings)
            foreach (var (cx, cz) in cubeList)
                if (island.CubeAt(cx, cz) is { } cube) cube.Info = (int[])settings.Info.Clone();

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
        // wall piece's (a portrait, the round window, a window) still the wall, in the wallpaper, its picture on it -- and the chests (but the
        // big one under the hall's round window: the cars' way out)
        var pieces = furnished ? PalaceFurniture.Find(gameDirectory, area, offsets, cells) : new List<PalaceFurniture.Piece>();
        var chests = furnished ? PalaceFurniture.Chests(interiors, area, offsets) : new List<PalaceFurniture.Chest>();
        chests.RemoveAll(c => c.Sprite == PalaceFurniture.BigChest && c.Tile == hallTile);
        var roundWindow = pieces.FirstOrDefault(p => p.Kind == PalaceFurniture.Kind.RoundWindow && p.Tile == hallTile);
        if (roundWindow is not null) pieces.Remove(roundWindow);
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
        // ---- the round window opened: its cells, and the wall's behind them out to the open air (away from the hall's air) -- the cars' way
        // out of the palace, as Twinsen's
        var opening = new HashSet<(int X, int Y, int Z)>();
        (int Nx, int Nz) outward = (0, 0);
        if (roundWindow is not null)
        {
            // (out: the side of the window with none of the hall's air in front of it)
            outward = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }
                .MinBy(d => roundWindow.Cells.Count(c => air.Contains((c.X + d.Item1, c.Y, c.Z + d.Item2))) - roundWindow.Cells.Count(c => air.Contains((c.X - d.Item1, c.Y, c.Z - d.Item2))));
            foreach (var c in roundWindow.Cells)
                for (var k = 0; k < 4; k++)
                {
                    var n = (c.X + outward.Nx * k, c.Y, c.Z + outward.Nz * k);
                    if (k > 0 && !cells.ContainsKey(n)) break;
                    opening.Add(n);
                }
            foreach (var n in opening) { cells.Remove(n); shown.Remove(n); }
        }
        // ---- the walls made whole: the grids leave gaps -- by a fireplace, under a wall -- that their isometric picture hides behind what
        // stands in front, and pockets no room reaches. Where a room's air meets the outside's beside it, the outside's cell becomes a wall;
        // and every empty cell within the rooms that isn't a room's air (a shaft between rooms, a hollow under a wall) is filled: drawn as
        // walls (the room's wallpaper inside, the palace's stone outside) and in the collision. The palace's front door (room 154's, onto
        // Otringal's hilltop) is in the hill now, filled with the rest; the round window stays open.
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
            if (cells.ContainsKey(n) || air.Contains(n) || opening.Contains(n)) continue;
            var tile = Array.FindIndex(extents, e => n.X >= e.X0 && n.X <= e.X1 && n.Z >= e.Z0 && n.Z <= e.Z1);
            cells[n] = (-1, Math.Max(0, tile));
            if (!ReferenceEquals(shown, cells)) shown[n] = (-1, Math.Max(0, tile));
            gaps++;
        }
        if (gaps > 0) log.Add($"the walls made whole: {gaps} cells of gaps to the outside, pockets no room reaches and the front door filled");
        // ---- the floors made whole: a cell of the bottom layer within a room with nothing in it -- a door's threshold, a hole its isometric
        // picture hides -- is floor, the room's commonest floor brick (the hill's rock under the rooms showed through by the race's carpet)
        var floorBricks = cells.Where(c => c.Key.Y == minY && c.Value.Brick >= 0).GroupBy(c => c.Value.Tile)
            .ToDictionary(g => g.Key, g => g.GroupBy(c => c.Value.Brick).MaxBy(b => b.Count())!.Key);
        var floored = 0;
        for (var ti = 0; ti < extents.Length; ti++)
        {
            var e = extents[ti];
            for (var x = e.X0; x <= e.X1; x++)
                for (var z = e.Z0; z <= e.Z1; z++)
                {
                    var c = (x, minY, z);
                    if (cells.ContainsKey(c) || opening.Contains(c)) continue;
                    var brick = floorBricks.TryGetValue(ti, out var b) ? b : -1;
                    cells[c] = (brick, ti);
                    if (!ReferenceEquals(shown, cells)) shown[c] = (brick, ti);
                    floored++;
                }
        }
        if (floored > 0) log.Add($"the floors made whole: {floored} cells of the bottom layer (doors' thresholds, holes) floored");
        if (opening.Count > 0)
            log.Add($"the long hall's round window open through its wall ({opening.Count} cells), its sill {WY(roundWindow!.Origin.Y):0} ({roundWindow.Origin.Y - minY} layers over the floor's foot), the big chest under it gone");
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
            var outward2 = !air.Contains((c.X + dx, c.Y + dy, c.Z + dz));
            // (the palace's outside: Otringal's palace's walls, by height; and its stone on the top of a wall made whole -- a ledge)
            if (dy == 0 && outward2)
                return Shaded(c.Y - minY < FootLayers ? Teal : c.Y > maxY - TopLayers ? Turquoise : Stone, shade);
            if (outward2 && !Drawn(brick)) return Shaded(Stone, shade);
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

        // ---- the roofs: over each room, level with the top of its walls -- the hilltop over the rooms: their tops Otringal's rock (drawn apart,
        // RockRoof), their sides its palace's stone
        var roofs = 0;
        var roofBoxes = new List<(double X0, double Y0, double Z0, double X1, double Y1, double Z1)>();
        var roofTops = new Dictionary<int, double>();
        var roofRects = new List<(double X0, double Z0, double X1, double Z1, double Y)>();
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
            int roofSide = Nearest(StoneShadow);
            var list = roomFaces[ti];
            // (in slabs of MaxRun cells at the most: a body holds no more)
            for (var sx = rx0; sx <= rx1; sx += BrickBuilding.MaxRun)
            for (var sz = rz0; sz <= rz1; sz += BrickBuilding.MaxRun)
            {
            double xa = WX(sx), xb = WX(Math.Min(rx1, sx + BrickBuilding.MaxRun - 1) + 1), za = WZ(sz), zb = WZ(Math.Min(rz1, sz + BrickBuilding.MaxRun - 1) + 1);
            Slab(list, new((float)xa, (float)ya, (float)za), new((float)xb, (float)yb, (float)zb), roofSide);
            roofBoxes.Add((xa, ya, za, xb, yb, zb));
            roofRects.Add((xa, za, xb, zb, yb));
            }
            roofs++;
        }

        // ---- the harbour: the sixteen rooms' middle row's line, east of the hill: the quay at the cliffs' foot, the pillar out in the bay
        var hilltop = roofTops.Count > 0 ? roofTops.Values.Max() : WY(maxY + 1);
        var east = (int)Math.Round(WX(maxX + 1) / 512);
        var sixteen = cells.Keys.Where(c => cells[c].Tile != hallTile).ToList();
        int sqX0 = (int)Math.Round(WX(sixteen.Min(c => c.X)) / 512), sqX1 = (int)Math.Round(WX(sixteen.Max(c => c.X) + 1) / 512);
        int sqZ0 = (int)Math.Round(WZ(sixteen.Min(c => c.Z)) / 512), sqZ1 = (int)Math.Round(WZ(sixteen.Max(c => c.Z) + 1) / 512);
        var midZ = (sqZ0 + sqZ1) / 2;
        var quayX0 = east + PalaceCliff.Margin + PalaceCliff.Face - 6;
        var harbour = new PalaceCliff.Harbour((quayX0, midZ - 34, quayX0 + 26, midZ + 34), QuayY, (east + PillarOut, midZ, PillarRadius, hilltop + PillarRise));

        // ---- the bodies: each room's faces (a few bodies where they are many), each a decor nothing touches, in every cube it reaches
        var oblBytes = File.ReadAllBytes(Path.Combine(sourceDirectory, SourceObl));
        var next = HqrArchive.CountEntries(Path.Combine(sourceDirectory, SourceObl));
        var newBodies = new List<byte[]>();
        const int Cap = IslandDecors.MaxPerCubeOwnEngine;
        var (bodyCount, faceCount, decorsLeft) = BrickBuilding.AddBodies(island, roomFaces, newBodies, next, ti => $"room {area.Tiles[ti].Scene}", Cap, everyCube: true);
        var (rockBodies, rockFaces) = RockRoof(island, roofRects, newBodies, next, Cap);

        // ---- the outside: Otringal's palace on the sixteen rooms' roof, its arch in front of its porch, a wing of towers on the long hall's
        var outsideBoxes = new List<(double X0, double Y0, double Z0, double X1, double Y1, double Z1)>();
        var outsideBodies = 0;
        if (outside && roofed)
        {
            var under = cells.Where(c => area.Tiles[c.Value.Tile].Scene != HallScene).Select(c => c.Key).ToList();
            var tops = roofTops.Where(r => area.Tiles[r.Key].Scene != HallScene).Select(r => r.Value).ToList();
            if (under.Count > 0 && tops.Count > 0)
            {
                var hall = hallTile >= 0 ? (WX(extents[hallTile].X0), WZ(extents[hallTile].Z0), WX(extents[hallTile].X1 + 1), WZ(extents[hallTile].Z1 + 1)) : ((double, double, double, double)?)null;
                var site = new PalaceOutside.Site(WX(under.Min(c => c.X)), WZ(under.Min(c => c.Z)), WX(under.Max(c => c.X) + 1), WZ(under.Max(c => c.Z) + 1),
                    tops.Max(), U, V, hall);
                var placed = PalaceOutside.Add(island, source, HqrArchive.Open(Path.Combine(sourceDirectory, SourceObl)), newBodies, next, site, Cap);
                outsideBoxes = placed.Boxes; outsideBodies = placed.Bodies;
                log.AddRange(placed.Log);
            }
        }

        // ---- the Dark Monk's teleporter on the harbour's pillar, standing across the race's jump: the road crosses its foot (PalaceTeleporter)
        if (outside && roofed)
        {
            var ring = next + newBodies.Count;
            newBodies.Add(PalaceTeleporter.Body());
            var (tx, ty, tz) = PalaceTeleporter.Place((harbour.Pillar.X - 2) * 512, harbour.Pillar.Z * 512, harbour.Pillar.Top + RoadOver);
            const double Reach = PalaceTeleporter.Radius + PalaceTeleporter.Tube;
            BrickBuilding.AddToCubes(island, ring, 0, tx, (int)Math.Round(ty), tz, tx - PalaceTeleporter.Tube, tx + PalaceTeleporter.Tube,
                (int)Math.Floor(ty - Reach), BrickBuilding.NoBoxTop, tz - Reach, tz + Reach, Cap);
            log.Add($"the Dark Monk's teleporter: a gold ring {2 * Reach:0} across on the harbour's pillar, its middle at ({tx / 512:0.#}, {ty:0}, {tz / 512:0.#})");
        }

        // ---- the collision: the filled cells merged into boxes (the furniture's with them), the roofs, the chests and the outside's, each a
        // decor with an empty body, in every cube it reaches
        var empty = next + newBodies.Count;
        newBodies.Add(RaceTrackPipes.Write(new List<Vector3> { Vector3.Zero }, new List<Face>(), lit: false));
        var boxes = BrickBuilding.Boxes(cells.Keys.ToHashSet())
            .Select(b => (WX(b.X0), WY(b.Y0), WZ(b.Z0), WX(b.X1 + 1), WY(b.Y1 + 1), WZ(b.Z1 + 1))).Concat(roofBoxes).Concat(chestBoxes).Concat(outsideBoxes).ToList();
        var (boxCount, boxesLeft) = BrickBuilding.AddBoxes(island, empty, boxes, Cap, everyCube: true);
        decorsLeft += boxesLeft;

        // ---- the hill: the plateau at the roofs' top round the rooms (under it), its rim, its cliffs; the hall's end on the cliff's edge; the
        // harbour east of it
        var rooms = extents.Select(e => ((int)Math.Round(WX(e.X0) / 512), (int)Math.Round(WZ(e.Z0) / 512), (int)Math.Round(WX(e.X1 + 1) / 512) - 1, (int)Math.Round(WZ(e.Z1 + 1) / 512) - 1)).ToList();
        (int X0, int X1, int Z)? cliffAt = hallTile >= 0 && opening.Count > 0 && outward.Nz < 0
            ? ((int)Math.Round(WX(extents[hallTile].X0) / 512), (int)Math.Round(WX(extents[hallTile].X1 + 1) / 512) - 1, (int)Math.Round(WZ(extents[hallTile].Z0) / 512))
            : null;
        // (the ways across the rocks, flat: the runway east from the sixteen rooms' roof to the cliff's edge -- the jump to the harbour's
        // pillar -- and, from the hill's north-east edge -- where a road from the round window comes up onto it -- to their roof's north side)
        var corridors = new List<PalaceCliff.Corridor>
        {
            new(new() { (sqX1 - 4, midZ), (sqX1 + PalaceCliff.Margin + PalaceCliff.Wobble + 10, midZ) }, CorridorHalf),
        };
        if (hallTile >= 0)
        {
            double hx1 = Math.Round(WX(extents[hallTile].X1 + 1) / 512), hz0 = Math.Round(WZ(extents[hallTile].Z0) / 512);
            // (north to south across the corner of the L, from out past the hill's north edge, level with the long hall's end, to their roof:
            // the race's road up from the round window comes onto the hill there. In line with the domes' north-east hall -- the cluster
            // stands over the rooms' middle, that hall 0.69 of their width in from the west -- so a car coming down it has the domes ahead,
            // the road on the roof winding in among them)
            var mx = Math.Round(sqX0 + (sqX1 - sqX0) * 0.69);
            corridors.Add(new(new() { (mx, hz0 - 4), (mx, sqZ0 + 2) }, CorridorHalf));
        }
        log.AddRange(PalaceCliff.Shape(island, cubeList, new PalaceCliff.Site(rooms, hilltop, Floor - 60, cliffAt, corridors, harbour)));
        log.Add($"  the sixteen rooms: cells x {sqX0}..{sqX1}, z {sqZ0}..{sqZ1}; the ways across the rocks: " +
                string.Join("; ", corridors.Select(c => string.Join(" -> ", c.Points.Select(p => $"({p.X:0}, {p.Z:0})")))));
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
        // (the layout: island cells and world heights -- the floor's top the cars drive on, the hilltop, each room, the round window's opening
        // and the way out of it, the ways across the rocks, the harbour's pillar and quay, the island's cubes)
        var layout = new Dictionary<string, object?>
        {
            ["floorTop"] = WY(minY + 1), ["hilltop"] = hilltop, ["cell"] = U, ["layer"] = V,
            ["cubes"] = new[] { firstX, firstZ, cubesX, cubesZ },
            ["rooms"] = rooms.Select((r, ti) => new Dictionary<string, int> { ["scene"] = area.Tiles[ti].Scene, ["x0"] = r.Item1, ["z0"] = r.Item2, ["x1"] = r.Item3, ["z1"] = r.Item4 }).ToList(),
            ["window"] = opening.Count == 0 ? null : new Dictionary<string, double>
            {
                ["x0"] = Math.Round(WX(opening.Min(c => c.X)) / 512), ["x1"] = Math.Round(WX(opening.Max(c => c.X) + 1) / 512),
                ["z0"] = Math.Round(WZ(opening.Min(c => c.Z)) / 512), ["z1"] = Math.Round(WZ(opening.Max(c => c.Z) + 1) / 512),
                ["sill"] = WY(opening.Min(c => c.Y)), ["top"] = WY(opening.Max(c => c.Y) + 1), ["outX"] = outward.Nx, ["outZ"] = outward.Nz,
            },
            ["corridors"] = corridors.Select(c => new Dictionary<string, object> { ["points"] = c.Points.Select(p => new[] { p.X, p.Z }).ToList(), ["half"] = c.Half }).ToList(),
            ["pillar"] = new[] { harbour.Pillar.X, harbour.Pillar.Z, harbour.Pillar.R, harbour.Pillar.Top },
            ["quay"] = new[] { harbour.Quay.X0, harbour.Quay.Z0, harbour.Quay.X1, harbour.Quay.Z1, harbour.QuayY },
        };
        File.WriteAllText(Path.Combine(outDirectory, LayoutFile), System.Text.Json.JsonSerializer.Serialize(layout, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        log.Add($"{IleFile}: {cubesX} x {cubesZ} cubes from cube ({firstX}, {firstZ}); the palace {Across} times a brick across and {Up} times a layer up " +
                $"({spanX} x {spanZ} cells), its floor's foot at {Floor}, the hilltop at {hilltop:0}");
        log.Add($"{OblFile}: {bodyCount} bodies of the rooms ({faceCount} faces, {roofs} roofs and the furniture among them), {rockBodies} of the roofs' rock ({rockFaces} faces), " +
                (outsideBodies > 0 ? $"{outsideBodies} of the outside, " : "") + $"and an empty one for the {boxCount} collision boxes, from body {next} on" +
                (decorsLeft > 0 ? $"; WARNING: {decorsLeft} decors left out (a cube full)" : ""));
        log.Add($"the busiest cube: ({busiest.X}, {busiest.Z}), {busiest.Cube.Decors.Count} objects of the {Cap} a cube holds in this editor's engine");
        return new Built(area.Tiles.Count, cells.Count, bodyCount, faceCount, boxCount, roofs, (firstX, firstZ), (cubesX, cubesZ), string.Join("\n", log));
    }

    // A roof slab's sides and bottom (its top is the rock: RockRoof).
    private static void Slab(List<BrickBuilding.Face> list, Vector3 lo, Vector3 hi, int side)
    {
        float xa = lo.X, ya = lo.Y, za = lo.Z, xb = hi.X, yb = hi.Y, zb = hi.Z;
        void Q(Vector3 a, Vector3 b, Vector3 c, Vector3 e, Vector3 n) => list.Add(new BrickBuilding.Face(new[] { a, b, c, e }, side, n));
        Q(new(xa, ya, za), new(xb, ya, za), new(xb, ya, zb), new(xa, ya, zb), -Vector3.UnitY);
        Q(new(xa, ya, za), new(xb, ya, za), new(xb, yb, za), new(xa, yb, za), -Vector3.UnitZ);
        Q(new(xa, ya, zb), new(xb, ya, zb), new(xb, yb, zb), new(xa, yb, zb), Vector3.UnitZ);
        Q(new(xa, ya, za), new(xa, ya, zb), new(xa, yb, zb), new(xa, yb, za), -Vector3.UnitX);
        Q(new(xb, ya, za), new(xb, ya, zb), new(xb, yb, zb), new(xb, yb, za), Vector3.UnitX);
    }

    // The roofs' tops as the hill's rock: textured polygons of OTRINGAL.OBL's page's mossy rock (its 64 x 64 tile repeated: 256 pixels a
    // polygon, RockSpan world units), on a grid of RockSpan squares fixed in the world (the rock runs on across the roofs), cut to the
    // roofs' edges; a body of them for each cube (the grid's squares are a cube's eighths: none crosses a cube's edge), each a decor drawn
    // only. Unlit, as Otringal's own textured rocks. Returns the bodies and polygons.
    private static (int Bodies, int Faces) RockRoof(IslandFile island, List<(double X0, double Z0, double X1, double Z1, double Y)> rects, List<byte[]> newBodies, int firstBody, int maxPerCube)
    {
        var quads = new Dictionary<(int Cx, int Cz), List<(double X0, double Z0, double X1, double Z1, double Y)>>();
        foreach (var (rx0, rz0, rx1, rz1, y) in rects)
            for (var gz = (int)Math.Floor(rz0 / RockSpan); gz * RockSpan < rz1; gz++)
            for (var gx = (int)Math.Floor(rx0 / RockSpan); gx * RockSpan < rx1; gx++)
            {
                double qx0 = Math.Max(rx0, gx * RockSpan), qx1 = Math.Min(rx1, (gx + 1) * RockSpan);
                double qz0 = Math.Max(rz0, gz * RockSpan), qz1 = Math.Min(rz1, (gz + 1) * RockSpan);
                if (qx1 - qx0 < 1 || qz1 - qz0 < 1) continue;
                var key = ((int)Math.Floor(gx * RockSpan / IslandFile.CubeSize), (int)Math.Floor(gz * RockSpan / IslandFile.CubeSize));
                if (!quads.TryGetValue(key, out var list)) quads[key] = list = new();
                list.Add((qx0, qz0, qx1, qz1, y));
            }
        int bodies = 0, faces = 0;
        foreach (var ((cx, cz), list) in quads.OrderBy(q => q.Key.Cz).ThenBy(q => q.Key.Cx))
            foreach (var chunk in list.Chunk(BrickBuilding.MaxFaces / 1).Select(c => c.ToList()))
            {
                // (a body: its points about the chunk's middle, each quad's corners with their place in the tile's repeat)
                double lx = chunk.Min(q => q.X0), hx = chunk.Max(q => q.X1), lz = chunk.Min(q => q.Z0), hz = chunk.Max(q => q.Z1), ly = chunk.Min(q => q.Y);
                var origin = new Vector3((float)((lx + hx) / 2), (float)ly, (float)((lz + hz) / 2));
                var points = new List<Vector3>(); var index = new Dictionary<(double, double, double), int>();
                int P(double x, double y, double z)
                {
                    if (index.TryGetValue((x, y, z), out var i)) return i;
                    points.Add(new Vector3((float)(x - origin.X), (float)(y - origin.Y), (float)(z - origin.Z)));
                    return index[(x, y, z)] = points.Count - 1;
                }
                int Uv(double w, double cellStart) => (int)Math.Clamp(Math.Round((w - cellStart) / RockSpan * 65535), 0, 65535);
                var bodyFaces = new List<Face>();
                foreach (var (qx0, qz0, qx1, qz1, y) in chunk)
                {
                    double sx = Math.Floor(qx0 / RockSpan) * RockSpan, sz = Math.Floor(qz0 / RockSpan) * RockSpan;
                    // (corners turning so that (p1 - p0) x (p2 - p0) points up: the face's outside)
                    (double X, double Z)[] c = { (qx0, qz1), (qx1, qz1), (qx1, qz0), (qx0, qz0) };
                    var ids = c.Select(p => P(p.X, y, p.Z)).ToArray();
                    var uv = c.SelectMany(p => new[] { Uv(p.X, sx), Uv(p.Z, sz) }).ToArray();
                    bodyFaces.Add(new Face(ids, 0, Texture: new FaceTexture(0, uv), Lba2Type: 8));
                }
                if (points.Count > BrickBuilding.MaxPoints) throw new InvalidDataException("The roofs' rock: a body of more points than one holds.");
                // (the header's first byte: the body format's version, 0x10 as every retail body has it -- the engine reads textured polygons by it)
                var header = new byte[96];
                header[0] = 0x10;
                var body = new Body
                {
                    Game = 2, Static = true, Lit = false, Header = header, Vertices = points,
                    Bones = new List<Bone> { new(0, points.Count, 0, -1, new byte[8]) },
                    Faces = bodyFaces, Textures = new[] { RoofRock },
                };
                var number = firstBody + newBodies.Count;
                newBodies.Add(body.Write());
                bodies++; faces += bodyFaces.Count;
                BrickBuilding.AddToCubes(island, number, 0, origin.X, (int)Math.Round(origin.Y), origin.Z, lx, hx, (int)Math.Floor(ly), BrickBuilding.NoBoxTop, lz, hz, maxPerCube);
            }
        return (bodies, faces);
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

    // Tools > LBA2: Palace island -- into the game folder (its own new files: no backups), from the folder's own palace scenes and Otringal.
    public static bool IsInstalled(string gameDirectory) => File.Exists(Path.Combine(gameDirectory, IleFile));
    public static List<string> Add(string gameDirectory) => new() { Build(gameDirectory, gameDirectory).Log };
    public static List<string> Remove(string gameDirectory)
    {
        var log = new List<string>();
        foreach (var f in new[] { IleFile, OblFile, IleFile + ".bak", LayoutFile })
        {
            var path = Path.Combine(gameDirectory, f);
            if (File.Exists(path)) { File.Delete(path); log.Add($"{f} deleted"); }
        }
        return log;
    }
}
