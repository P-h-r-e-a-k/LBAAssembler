using System.Buffers.Binary;
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
//   - every body and box in every cube it reaches (the engine collides only with the decors of the cube the hero is in), the bodies in
//     every cube they can be seen from too (it draws those of the 3 x 3 cubes round the camera's), as many a cube as this editor's engine
//     takes;
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
    public const int NorthCells = 120;                  // and north of it, for the race's leap out of the round window over the sea
    public const double QuayY = 1400;                   // the harbour's quay (Otringal's lowest)
    public const int PillarOut = PalaceCliff.Margin + 50; // the teleporter's pillar: cells east of the rooms' east wall, past the cliffs ...
    public const double PillarRadius = 6;               // ... its top's radius (cells) ...
    public const double PillarRise = 9550;              // ... its top this much over the hilltop, the teleporter on it in the way of the race's leap
                                                        // off its spiral (2026-10-13: the jump's lip some 30,200 high; was 700) ...
    public const int PillarNorth = 78;                  // ... and this many cells north of the sixteen rooms' north wall: on the leap's line (was on
                                                        // their middle row's, by the quay)
                                                        // (the road's 60 over the hilltop, its ramp's 700 -- palace_design.py), on the pillar
    public const double RoadOver = 60;                  // (the race's road over what it lies on)
    public const int DoorBricks = 4;                    // a door between two rooms this many bricks wide (the game's are two: 8 cells, the
                                                        // race's road through them too narrow)
    public const double CorridorHalf = 9;               // the ways across the hilltop's rocks: half their width (cells)
    public const int RingOut = 9;                       // the way round the palace's west and south sides: cells out from its walls
    public const int StartScene = 154;                  // the race's start: its room's way in from outside (Build: the entrance)
    public const int PortalFrom = 151;                  // the room whose fireplace is a teleporter -- to a fireplace made in the same room (Build)
    // the race's track through the rooms, the floor's only red (Build): west along the bottom row from the start's room to the warp's, then
    // north up the west column from there into the long hall
    private static readonly int[] TrackRow = { StartScene, 153, 152, PortalFrom };
    private static readonly int[] TrackColumn = { PortalFrom, 155, 159, 163, HallScene };
    public const int FireplaceWide = 4, FireplaceHigh = 5;   // (a fireplace made: the cells along its wall and its layers, as the grids' own)
    public const int PortraitsPerRoom = 2;              // pictures of the game's characters on a room's walls (PalacePortraits) ...
    public const int HallPortraits = 6;                 // ... and this many more along the long hall's
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
        // (the grids' own cells, before the walls and floors are made whole and the doors wider: the floor the rooms had -- PaintFloor)
        var original = cells.Keys.ToHashSet();
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
        // ---- the doors made wider (the user, 2026-10-12: "The palace track is far too narrow ... Let's make the inside tracks wider"): a door
        // between two rooms is two bricks wide, through a wall band four bricks deep -- both rooms' walls and the shaft between them. Each is
        // opened to DoorBricks, as much on either side, through the band and up to its lintel; a piece of furniture in the way goes, its
        // model and its cells with it (the floor stays)
        var widened = new HashSet<(int X, int Y, int Z)>();
        var doors = 0;
        // (each door: the two rooms, whether it is in a wall along z, its span along the wall once widened, its top layer)
        var doorways = new List<(int A, int B, bool AlongZ, int U0, int U1, int Top)>();
        for (var a = 0; a < extents.Length; a++)
            for (var b = 0; b < extents.Length; b++)
            {
                var (ea, eb) = (extents[a], extents[b]);
                // b east of a -- the band their columns share along x -- or south of it, along z (u along the wall, v across it)
                var alongZ = eb.X0 > ea.X0 && eb.X0 <= ea.X1 && Math.Min(ea.Z1, eb.Z1) - Math.Max(ea.Z0, eb.Z0) > 4;
                var alongX = eb.Z0 > ea.Z0 && eb.Z0 <= ea.Z1 && Math.Min(ea.X1, eb.X1) - Math.Max(ea.X0, eb.X0) > 4;
                if (a == b || !(alongZ || alongX)) continue;
                int v0 = alongZ ? eb.X0 : eb.Z0, v1 = alongZ ? ea.X1 : ea.Z1;
                int u0 = (alongZ ? Math.Max(ea.Z0, eb.Z0) : Math.Max(ea.X0, eb.X0)) + 1, u1 = (alongZ ? Math.Min(ea.Z1, eb.Z1) : Math.Min(ea.X1, eb.X1)) - 1;
                (int X, int Y, int Z) At(int u, int y, int v) => alongZ ? (v, y, u) : (u, y, v);
                bool Open(int u) { for (var v = v0; v <= v1; v++) if (cells.ContainsKey(At(u, minY + 1, v))) return false; return true; }
                for (var u = u0; u <= u1; u++)
                {
                    if (!Open(u) || (u > u0 && Open(u - 1))) continue;
                    var d1 = u;
                    while (d1 + 1 <= u1 && Open(d1 + 1)) d1++;
                    var width = d1 - u + 1;
                    // (its lintel: the lowest filled layer over the door)
                    var top = minY + 1;
                    while (top < maxY && Enumerable.Range(v0, v1 - v0 + 1).All(v => !cells.ContainsKey(At(u, top + 1, v)))) top++;
                    if (width >= DoorBricks) { doorways.Add((a, b, alongZ, u, d1, top)); continue; }
                    var more = (DoorBricks - width + 1) / 2;
                    for (var w = u - more; w <= d1 + more; w++)
                        if (w < u || w > d1)
                            for (var v = v0; v <= v1; v++)
                                for (var y = minY + 1; y <= top; y++) widened.Add(At(w, y, v));
                    doorways.Add((a, b, alongZ, u - more, d1 + more, top));
                    doors++;
                }
            }
        // (and the way across a room between two of its doors that face each other kept clear, as high as the doors: a piece of furniture
        // standing in it goes, and any other block -- the race's road runs from door to door; room 156's block and room 160's stood in the
        // middle of it)
        var lines = new Dictionary<(int X, int Z), int>();
        for (var t = 0; t < extents.Length; t++)
            foreach (var alongZ in new[] { true, false })
            {
                // (its doors in its two walls along z -- west and east -- or along x -- north and south)
                var near = doorways.Where(d => d.AlongZ == alongZ && d.B == t).ToList();
                var far = doorways.Where(d => d.AlongZ == alongZ && d.A == t).ToList();
                foreach (var p in near)
                    foreach (var q in far)
                    {
                        int s0 = Math.Max(p.U0, q.U0), s1 = Math.Min(p.U1, q.U1), top = Math.Min(p.Top, q.Top);
                        if (s1 < s0) continue;
                        var e = extents[t];
                        for (var w = alongZ ? e.X0 : e.Z0; w <= (alongZ ? e.X1 : e.Z1); w++)
                            for (var s = s0; s <= s1; s++)
                            {
                                var key = alongZ ? (w, s) : (s, w);
                                lines[key] = Math.Max(lines.GetValueOrDefault(key, minY), top);
                            }
                    }
            }
        var inTheWay = pieces.Where(p => p.Standing && p.Cells.Any(c => c.Y > minY && lines.ContainsKey((c.X, c.Z)))).ToList();
        foreach (var p in inTheWay) widened.UnionWith(p.Cells.Where(c => c.Y > minY));
        var blocks = 0;
        foreach (var ((x, z), top) in lines)
            for (var y = minY + 1; y <= top; y++)
                if (cells.ContainsKey((x, y, z)) && widened.Add((x, y, z))) blocks++;
        // (the chests too: one in the middle of rooms 156 and 160 -- a chest's size is the game's, 512 to a cell)
        var chestsGone = chests.RemoveAll(c =>
        {
            double hx = (c.AlongX ? c.Long : c.Short) / 1024, hz = (c.AlongX ? c.Short : c.Long) / 1024;
            for (var x = (int)Math.Floor(c.X - hx); x <= (int)Math.Floor(c.X + hx); x++)
                for (var z = (int)Math.Floor(c.Z - hz); z <= (int)Math.Floor(c.Z + hz); z++)
                    if (lines.ContainsKey((x, z))) return true;
            return false;
        });
        if (widened.Count > 0)
        {
            var gone = pieces.Where(p => p.Cells.Any(widened.Contains)).ToList();
            foreach (var p in gone)
            {
                pieces.Remove(p);
                if (p.Standing) foreach (var c in p.Cells.Where(c => c.Y > minY)) widened.Add(c);
            }
            foreach (var c in widened)
            {
                if (cells.Remove(c) && !ReferenceEquals(shown, cells)) shown.Remove(c);
                modelled.Remove(c);
                air.Add(c);
            }
            papered = pieces.Where(p => !p.Standing).SelectMany(p => p.Cells).ToHashSet();
            log.Add($"the doors made wider: {doors} doors between rooms {DoorBricks} bricks wide ({DoorBricks * Across} cells), {widened.Count} cells opened" +
                    (gone.Count > 0 ? $", {gone.Count} pieces of furniture in the way gone ({string.Join(", ", gone.GroupBy(p => p.Kind).Select(g => $"{g.Count()} {g.Key}"))})" : "") +
                    (blocks > 0 ? $", {blocks} cells of other blocks on the ways between facing doors" : "") +
                    (chestsGone > 0 ? $", {chestsGone} chests on them gone" : ""));
        }
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
        // (the way a piece of furniture looks: away from its wall -- the side with the most of a room's air in front of it, three cells deep,
        // less what is behind it)
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
        var hidden = new HashSet<((int X, int Y, int Z) Cell, int Dir)>();   // (wall faces left out: behind the entrance's doors, the teleporters' glow)
        var blocked = new HashSet<(int X, int Y, int Z)>();                  // (wall cells no picture may hang on)

        // ---- the palace's way in (the user, 2026-10-13: "Add an entrance door to the palace in the start room, we may need to reposition the
        // large purple chest on the wall Perhaps over to the wall to the left of it"): a pair of doors in the start room's east wall, as wide
        // as the room's west door and on its line -- the race's grid lines up in front of them, the start line on the straight -- in a gold
        // frame; on the wall's plane, its faces behind them left out. The chest that stood in front of them goes to the north wall, east of
        // the room's north door, against it (out of the way, or out altogether).
        var startTile = Enumerable.Range(0, area.Tiles.Count).FirstOrDefault(ti => area.Tiles[ti].Scene == StartScene, -1);
        var entranceFaces = new List<BrickBuilding.Face>();
        var entranceFloor = new HashSet<(int X, int Z)>();   // (the carpet on up to the doors: PaintFloor widens it as the rest)
        (double X, double Z0, double Z1)? entrance = null;
        if (startTile >= 0 && doorways.Where(d => d.AlongZ && d.B == startTile).Select(d => ((int U0, int U1, int Top)?)(d.U0, d.U1, d.Top)).FirstOrDefault() is { } wd)
        {
            int mz = (wd.U0 + wd.U1) / 2, xw = (extents[startTile].X0 + extents[startTile].X1) / 2;
            while (xw <= extents[startTile].X1 && air.Contains((xw, minY + 1, mz))) xw++;
            var flat = xw <= extents[startTile].X1 && Enumerable.Range(wd.U0, wd.U1 - wd.U0 + 1)
                .All(z => Enumerable.Range(minY + 1, wd.Top - minY).All(y => shown.ContainsKey((xw, y, z)) && air.Contains((xw - 1, y, z))));
            if (flat)
            {
                for (var z = wd.U0; z <= wd.U1; z++)
                    for (var y = minY + 1; y <= wd.Top; y++) hidden.Add(((xw, y, z), 3));
                for (var z = wd.U0 - 1; z <= wd.U1 + 1; z++)
                    for (var y = minY + 1; y <= wd.Top + 2; y++) blocked.Add((xw, y, z));
                EntranceDoors(entranceFaces, WX(xw), WZ(wd.U0), WZ(wd.U1 + 1), WY(minY + 1), WY(wd.Top + 1), Shaded);
                entrance = (WX(xw) / 512, WZ(wd.U0) / 512, WZ(wd.U1 + 1) / 512);
                for (var x = xw - 8; x < xw; x++)
                    for (var z = wd.U0 + 1; z < wd.U1; z++) entranceFloor.Add((x, z));
                log.Add($"the palace's way in: doors in room {StartScene}'s east wall, cells z {entrance.Value.Z0:0}..{entrance.Value.Z1:0} at x {entrance.Value.X:0}, " +
                        $"{wd.Top - minY} layers high");
                // (the chest in front of them to the north wall)
                var ci = chests.FindIndex(c => c.Tile == startTile && c.Z >= wd.U0 - 1 && c.Z <= wd.U1 + 2);
                if (ci >= 0)
                {
                    var chest = chests[ci];
                    chests.RemoveAt(ci);
                    var nd = doorways.Where(d => !d.AlongZ && d.B == startTile).Select(d => ((int U0, int U1)?)(d.U0, d.U1)).FirstOrDefault();
                    double halfLong = chest.Long / 1024, halfShort = chest.Short / 1024;
                    var moved = false;
                    if (nd is { } n)
                    {
                        var cx = (n.U1 + 1 + xw) / 2.0;
                        var zn = mz;
                        while (zn > extents[startTile].Z0 && air.Contains(((int)Math.Floor(cx), minY + 1, zn))) zn--;
                        var cz = zn + 1 + halfShort + 0.1;
                        var clear = true;
                        for (var x = (int)Math.Floor(cx - halfLong); x <= (int)Math.Floor(cx + halfLong) && clear; x++)
                            for (var z = (int)Math.Floor(cz - halfShort); z <= (int)Math.Floor(cz + halfShort) && clear; z++)
                                clear = air.Contains((x, minY + 1, z)) && !modelled.Contains((x, minY + 1, z));
                        if (clear && cx + halfLong < xw)
                        {
                            chests.Add(chest with { X = cx, Z = cz, AlongX = true });
                            moved = true;
                            log.Add($"  its chest moved to the north wall, at cells ({(x0 + (cx - minX) * U) / 512:0.#}, {(z0 + (cz - minZ) * U) / 512:0.#})");
                        }
                    }
                    if (!moved) log.Add("  its chest taken away: no room for it by the north wall");
                }
            }
            else log.Add($"the palace's way in: room {StartScene}'s east wall isn't flat on its west door's line -- no doors");
        }

        // ---- the fireplaces made a teleporter (the user, 2026-10-13: "Our fireplace that's here (see image) near the dinofly picture let's
        // turn it into a teleporter so driving into it brings us out of a second fire place where the Queen of the mosquibees is on the
        // wall"; then, with a picture of the room: "the exit fireplace should be between the two portraits"): room 151's -- at the end of the
        // bottom row's carpet, by the Dino-Fly's picture, now the Queen of the Mosquibees' -- leads to a fireplace made in the same room,
        // on the wall the Queen hangs on, between her picture and the next one (below, with the pictures). Each hollow
        // (PalaceFurniture.Portal): its hearth -- the middle two cells of the four along the wall, the bottom three layers -- out of the
        // collision, the wall's faces at its back left out under the glow. The race-track mode takes a car into the first's hearth out of
        // the second's (the layout's teleports, RACEMOD.CPP teleport=). (First the second was room 159's own fireplace.)
        var portals = new HashSet<PalaceFurniture.Piece>();
        var teleports = new List<double[]>();
        // (a fireplace's hearth: its cells; and its front's middle -- palace cells)
        List<(int X, int Y, int Z)> Hearth(PalaceFurniture.Piece p, int nx, int nz)
        {
            var along = nx != 0 ? p.Size.Z : p.Size.X;
            int a0 = along / 2 - 1, a1 = along / 2;
            return p.Cells.Where(c => c.Y >= p.Origin.Y && c.Y <= p.Origin.Y + 2 && (nx != 0 ? c.Z - p.Origin.Z : c.X - p.Origin.X) is var i && i >= a0 && i <= a1).ToList();
        }
        (double X, double Z) Front(PalaceFurniture.Piece p, int nx, int nz) =>
            (p.Origin.X + p.Size.X / 2.0 + nx * p.Size.X / 2.0, p.Origin.Z + p.Size.Z / 2.0 + nz * p.Size.Z / 2.0);
        void Hollow(PalaceFurniture.Piece p)
        {
            portals.Add(p);
            var (nx, nz) = Looks(p);
            foreach (var c in Hearth(p, nx, nz))
            {
                cells.Remove(c);
                air.Add(c);
                var back = (c.X - nx, c.Y, c.Z - nz);
                if (!p.Cells.Contains(back)) hidden.Add((back, nx > 0 ? 2 : nx < 0 ? 3 : nz > 0 ? 4 : 5));
            }
        }
        var portalFrom = furnished ? pieces.FirstOrDefault(p => p.Kind == PalaceFurniture.Kind.Fireplace && area.Tiles[p.Tile].Scene == PortalFrom) : null;
        PalaceFurniture.Piece? portalTo = null;
        if (portalFrom is not null) Hollow(portalFrom);
        // ---- the floor painted (the user, 2026-10-13: "Let's make the main road section that's mostly red at the moment pure red just with
        // the white arrows. The surrounding area varies a lot, let's keep this consistent with a check pattern based on the green"; then
        // "We have lots of red that isn't the inside track. The red track area should be the width of the door way, there should be no other
        // red on the floor ... let's keep the corners nice and square"): the race's track red -- from the palace's doors in the start's room
        // west through the bottom row's doors (TrackRow), as wide as they are, to the west column's line; and from there north through that
        // column's doors (TrackColumn) and up the long hall to its north wall, the round window -- the two meeting in a square corner; the
        // tiles along it gold; the rest of the floor the two greens of its tiles, cell by cell. (Until then every carpet of the grids was red,
        // widened to the doors' width -- a red lattice through all sixteen rooms, its corners the grids' carpets' cut ones)
        int TileOf(int scene) => Enumerable.Range(0, area.Tiles.Count).FirstOrDefault(ti => area.Tiles[ti].Scene == scene, -1);
        // (the doors between a line of rooms, one after another: their spans along their walls)
        List<(int U0, int U1)> Doors(int[] scenes, bool alongZ)
        {
            var spans = new List<(int U0, int U1)>();
            for (var k = 0; k + 1 < scenes.Length; k++)
            {
                int p = TileOf(scenes[k]), q = TileOf(scenes[k + 1]);
                spans.AddRange(doorways.Where(d => d.AlongZ == alongZ && ((d.A == p && d.B == q) || (d.A == q && d.B == p))).Select(d => (d.U0, d.U1)));
            }
            return spans;
        }
        var track = new HashSet<(int X, int Z)>();
        var rowDoors = Doors(TrackRow, true);
        var columnDoors = Doors(TrackColumn, false);
        if (rowDoors.Count == TrackRow.Length - 1 && columnDoors.Count == TrackColumn.Length - 1 && TileOf(StartScene) >= 0 && TileOf(HallScene) >= 0)
        {
            // (the doors on a line are in line -- the grids put them so; should one be off, the band takes the span most of them have)
            var (r0, r1) = rowDoors.GroupBy(s => s).MaxBy(g => g.Count())!.Key;
            var (c0, c1) = columnDoors.GroupBy(s => s).MaxBy(g => g.Count())!.Key;
            var trackEast = entranceFloor.Count > 0 ? entranceFloor.Max(c => c.X) : extents[TileOf(StartScene)].X1;
            var trackNorth = extents[TileOf(HallScene)].Z0;
            for (var x = c0; x <= trackEast; x++)
                for (var z = r0; z <= r1; z++) track.Add((x, z));
            for (var z = trackNorth; z <= r1; z++)
                for (var x = c0; x <= c1; x++) track.Add((x, z));
            if (rowDoors.Concat(columnDoors).Any(s => s.U1 - s.U0 + 1 != DoorBricks) || rowDoors.Distinct().Count() > 1 || columnDoors.Distinct().Count() > 1)
                log.Add($"  WARNING: the track's doors aren't all in line and {DoorBricks} wide: the bottom row's {string.Join(", ", rowDoors)}, the west column's {string.Join(", ", columnDoors)}");
        }
        else log.Add($"WARNING: the track's doors not found ({rowDoors.Count} of the bottom row's, {columnDoors.Count} of the west column's): no red on the floor");
        var floorPaint = PaintFloor(cells, air, minY, track);
        log.Add($"the floor painted: {floorPaint.Count(p => p.Value == CarpetRed)} cells of the track's red ({DoorBricks} across, as the doors: from the palace's doors " +
                $"west along the bottom row, north up the west column and the long hall to the round window), {floorPaint.Count(p => p.Value == BandLight || p.Value == BandDark)} " +
                "of gold tiles along it, the rest checked in two greens");
        // (LBA2_PALACE_PAINT=<file>: the paint, a line a cell -- "x z r g b", the palace's cells -- for a look at it from above)
        if (Environment.GetEnvironmentVariable("LBA2_PALACE_PAINT") is { Length: > 0 } paintFile)
            File.WriteAllLines(paintFile, floorPaint.Select(p => $"{p.Key.X} {p.Key.Z} {p.Value.R:0} {p.Value.G:0} {p.Value.B:0} " +
                $"{(cells.TryGetValue((p.Key.X, minY, p.Key.Z), out var pc) ? pc.Brick : -2)} {(original.Contains((p.Key.X, minY, p.Key.Z)) && !original.Contains((p.Key.X, minY + 1, p.Key.Z)) ? 1 : 0)}"));
        int FaceColour((int X, int Y, int Z) c, int d, int brick, float shade)
        {
            var (dx, dy, dz, _) = BrickBuilding.Directions[d];
            var outward2 = !air.Contains((c.X + dx, c.Y + dy, c.Z + dz));
            if (dy > 0 && c.Y == minY && floorPaint.TryGetValue((c.X, c.Z), out var paint)) return Shaded(paint, shade);
            // (a room's wall: in bands by height -- PaintWall)
            if (dy == 0 && !outward2) return Shaded(PaintWall(c.Y - minY, maxY - minY, brick >= 0 && Drawn(brick) && !papered.Contains(c) ? Average(brick) : null), shade);
            // (the palace's outside: Otringal's palace's walls, by height; and its stone on the top of a wall made whole -- a ledge)
            if (dy == 0 && outward2)
                return Shaded(c.Y - minY < FootLayers ? Teal : c.Y > maxY - TopLayers ? Turquoise : Stone, shade);
            if (outward2 && !Drawn(brick)) return Shaded(Stone, shade);
            return Drawn(brick) && !papered.Contains(c) ? ColourOf(brick, shade) : Shaded(WallpaperAt(c.Y), shade);
        }

        // ---- the pictures of the game's characters (PalacePortraits): painted (pages of the island's object textures), and their places on
        // the rooms' plain walls -- PortraitsPerRoom a room, more along the long hall (its gallery) -- the wall's faces behind them left out
        var spots = new List<PalacePortraits.Spot>();
        PalacePortraits.Painted? painted = null;
        if (furnished)
        {
            painted = PalacePortraits.PaintAll(gameDirectory, Nearest);
            if (painted.Pages.Count > 0 && island.ObjectPages.Count + painted.Pages.Count < IslandFile.MaxObjectPages)
            {
                var furniture = modelled.Concat(papered).Concat(blocked).ToHashSet();
                int RoomOf((int X, int Y, int Z) c) => Array.FindIndex(extents, e => c.X > e.X0 && c.X < e.X1 && c.Z > e.Z0 && c.Z < e.Z1);
                spots = PalacePortraits.Spots(shown, air, furniture, minY + 1, RoomOf, PortraitsPerRoom, out var all, ti => ti == hallTile ? HallPortraits : 0);
                // (the teleporter's picture, the Queen of the Mosquibees: by its fireplace the picture nearest it -- where the Dino-Fly was; and
                // its way out, a fireplace made on the wall she hangs on, between her picture and the next one along it -- in front of the
                // wall, the room's air there, nothing else standing there)
                if (portalFrom is not null)
                {
                    var (fx, fz) = Looks(portalFrom);
                    var (ax, az) = Front(portalFrom, fx, fz);
                    var near = spots.Where(sp => sp.Tile == portalFrom.Tile)
                        .OrderBy(sp => PalacePortraits.Middle(sp) is var m ? (m.X - ax) * (m.X - ax) + (m.Z - az) * (m.Z - az) : 0).FirstOrDefault();
                    if (near is not null)
                    {
                        spots[spots.IndexOf(near)] = near with { Sitter = PalacePortraits.Queen };
                        var beside = spots.Where(sp => sp.U0 != near.U0 && sp.Tile == near.Tile && sp.Dir == near.Dir && sp.At == near.At)
                            .OrderBy(sp => Math.Abs(sp.U0 - near.U0)).FirstOrDefault();
                        var (dx, _, dz, _) = BrickBuilding.Directions[near.Dir];
                        // (the gap between the two: from the one's end to the other's start; with no other, beside her towards the wall's middle)
                        int g0, g1;
                        if (beside is not null) (g0, g1) = beside.U0 > near.U0 ? (near.U0 + PalacePortraits.Cells, beside.U0 - 1) : (beside.U0 + PalacePortraits.Cells, near.U0 - 1);
                        else (g0, g1) = (near.U0 + PalacePortraits.Cells + 1, near.U0 + PalacePortraits.Cells + FireplaceWide);
                        var u0 = g0 + Math.Max(0, (g1 - g0 + 1 - FireplaceWide) / 2);
                        var made = new List<(int X, int Y, int Z)>();
                        for (var u = u0; u < u0 + FireplaceWide; u++)
                            for (var y = minY + 1; y <= minY + FireplaceHigh; y++)
                                made.Add(dx != 0 ? (near.At + dx, y, u) : (u, y, near.At + dz));
                        if (g1 - g0 + 1 >= FireplaceWide && made.All(c => air.Contains(c) && !cells.ContainsKey(c) && !modelled.Contains(c) && !papered.Contains(c)))
                        {
                            var origin = (made.Min(c => c.X), minY + 1, made.Min(c => c.Z));
                            var size = dx != 0 ? (1, FireplaceHigh, FireplaceWide) : (FireplaceWide, FireplaceHigh, 1);
                            portalTo = new PalaceFurniture.Piece(PalaceFurniture.Kind.Fireplace, dx != 0 ? 68 : 69, near.Tile, origin, size, made);
                            pieces.Add(portalTo);
                            foreach (var c in made) { cells[c] = (-1, near.Tile); modelled.Add(c); }
                            Hollow(portalTo);
                        }
                        else log.Add($"  no room for the teleporter's way out between the pictures on room {PortalFrom}'s wall (cells {g0}..{g1} along it)");
                    }
                    // (the teleport: into the first's hearth -- its width less a little, from its back to a little in front of it, the floor's
                    // height up -- out of the second's, its middle just in its hearth, looking out of it; island cells, world heights, the
                    // engine's turn)
                    if (portalTo is not null)
                    {
                        var (tx, tz) = Looks(portalTo);
                        var (bx, bz) = Front(portalTo, tx, tz);
                        double I(double x) => (x0 + (x - minX) * U) / 512;
                        double J(double z) => (z0 + (z - minZ) * U) / 512;
                        double depth = (fx != 0 ? portalFrom.Size.X : portalFrom.Size.Z) * U / 512, inset = 0.8, ahead = 1.2, halfW = U / 512 - inset;
                        var (ix, jz) = (I(ax), J(az));
                        var box = fx != 0
                            ? new[] { Math.Min(ix, ix - fx * depth) - (fx < 0 ? ahead : 0), jz - halfW, Math.Max(ix, ix - fx * depth) + (fx > 0 ? ahead : 0), jz + halfW }
                            : new[] { ix - halfW, Math.Min(jz, jz - fz * depth) - (fz < 0 ? ahead : 0), ix + halfW, Math.Max(jz, jz - fz * depth) + (fz > 0 ? ahead : 0) };
                        var beta = (int)Math.Round(Math.Atan2(tx, tz) * 4096 / (2 * Math.PI)) & 4095;
                        teleports.Add(new[] { box[0], box[1], box[2], box[3], WY(minY + 1) - 600, WY(minY + 1) + 2400, I(bx) - tx * 0.4, J(bz) - tz * 0.4, WY(minY + 1), beta });
                        log.Add($"the teleporter: room {PortalFrom}'s fireplace (cells x {box[0]:0.#}..{box[2]:0.#}, z {box[1]:0.#}..{box[3]:0.#}) to a fireplace made " +
                                $"between the pictures on its {(tz < 0 ? "south" : tz > 0 ? "north" : tx < 0 ? "east" : "west")} wall (out at cells ({I(bx) - tx * 0.4:0.#}, " +
                                $"{J(bz) - tz * 0.4:0.#}), turned {beta})");
                    }
                }
            }
        }
        var covered = spots.SelectMany(PalacePortraits.Covered).Concat(hidden).ToHashSet();

        // ---- the faces that show, merged into rectangles of one colour, room by room
        var roomFaces = Faces(shown, FaceColour, (x, y, z) => new Vector3((float)WX(x), (float)WY(y), (float)WZ(z)), covered);
        foreach (var ti in Enumerable.Range(0, area.Tiles.Count)) roomFaces.TryAdd(ti, new List<BrickBuilding.Face>());
        if (startTile >= 0) roomFaces[startTile].AddRange(entranceFaces);

        // ---- the furniture's models, with each room's faces (in its bodies)
        var chestBoxes = new List<(double X0, double Y0, double Z0, double X1, double Y1, double Z1)>();
        if (pieces.Count + chests.Count > 0)
        {
            Vector3 Place(double x, double y, double z) => new((float)(x0 + (x - minX) * U), (float)(Floor + (y - minY) * V), (float)(z0 + (z - minZ) * U));
            // (seen from a way: shaded as the rooms' faces are -- top, bottom, east and west, south and north -- in between for the slopes)
            int Seen((float R, float G, float B) c, Vector3 n) => Shaded(c, n.Y * n.Y * (n.Y > 0 ? 1f : 0.5f) + n.X * n.X * 0.84f + n.Z * n.Z * 0.70f);
            var made = PalaceFurniture.Build(pieces, chests, Place, Seen, Looks, y => WallpaperAt(y), portals);
            foreach (var (ti, list) in made.Faces) roomFaces[ti].AddRange(list);
            chestBoxes = made.Boxes;
            log.AddRange(made.Log);
            // (where the fireplaces and chests are: island cells)
            foreach (var p in pieces.Where(p => p.Kind == PalaceFurniture.Kind.Fireplace))
                log.Add($"  a fireplace in room {area.Tiles[p.Tile].Scene}: cells x {WX(p.Origin.X) / 512:0}..{WX(p.Origin.X + p.Size.X) / 512:0}, " +
                        $"z {WZ(p.Origin.Z) / 512:0}..{WZ(p.Origin.Z + p.Size.Z) / 512:0}, looking {Looks(p)}");
            foreach (var c in chests)
                log.Add($"  a chest in room {area.Tiles[c.Tile].Scene} at cells ({(x0 + (c.X - minX) * U) / 512:0.#}, {(z0 + (c.Z - minZ) * U) / 512:0.#})");
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
            Slab(list, new((float)xa, (float)ya, (float)za), new((float)xb, (float)yb, (float)zb), roofSide, (float)(U * NearRun));
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
        var harbour = new PalaceCliff.Harbour((quayX0, midZ - 34, quayX0 + 26, midZ + 34), QuayY, (east + PillarOut, sqZ0 - PillarNorth, PillarRadius, hilltop + PillarRise));

        // ---- the bodies: each room's faces (a few bodies where they are many), each a decor nothing touches, in every cube it can be seen
        // from (the engine draws the decors of the 3 x 3 cubes round the camera's: a room further off, seen down a row of doors, wasn't) --
        // and drawn close to the camera (BrickBuilding.CloseFlag: cut at the near plane, the floor under the camera let the ground show)
        var oblBytes = File.ReadAllBytes(Path.Combine(sourceDirectory, SourceObl));
        var next = HqrArchive.CountEntries(Path.Combine(sourceDirectory, SourceObl));
        var newBodies = new List<byte[]>();
        const int Cap = IslandDecors.MaxPerCubeOwnEngine;
        var (bodyCount, faceCount, decorsLeft) = BrickBuilding.AddBodies(island, roomFaces, newBodies, next, ti => $"room {area.Tiles[ti].Scene}", Cap, everyCube: true,
            close: true, reach: PalaceOutside.Seen);
        var (rockBodies, rockFaces) = RockRoof(island, roofRects, newBodies, next, Cap);
        // (the pictures hung: a body each, its page the decor's)
        if (painted is not null)
        {
            log.AddRange(painted.Log);
            if (spots.Count > 0)
            {
                var firstPage = island.ObjectPages.Count + 1;
                island.ObjectPages.AddRange(painted.Pages);
                var hung = PalacePortraits.Hang(island, spots, painted, firstPage, (x, y, z) => new Vector3((float)WX(x), (float)WY(y), (float)WZ(z)), newBodies, next, Cap,
                    PalaceOutside.Seen);
                log.Add($"the pictures hung: {hung} of {spots.Count} -- " + string.Join("; ", spots.GroupBy(s => s.Tile).Select(g =>
                    $"room {area.Tiles[g.Key].Scene}: {string.Join(", ", g.Select(s => PalacePortraits.Sitters[s.Sitter].Name))}")));
                foreach (var s in spots)
                {
                    var (dx, _, dz, _) = BrickBuilding.Directions[s.Dir];
                    double px = dx != 0 ? WX(s.At + (dx > 0 ? 1 : 0)) : WX(s.U0 + 1), pz = dx != 0 ? WZ(s.U0 + 1) : WZ(s.At + (dz > 0 ? 1 : 0));
                    log.Add($"  {PalacePortraits.Sitters[s.Sitter].Name} in room {area.Tiles[s.Tile].Scene} at cells ({px / 512:0}, {pz / 512:0}), looking ({dx}, {dz})");
                }
            }
        }

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
                (int)Math.Floor(ty - Reach), BrickBuilding.NoBoxTop, tz - Reach, tz + Reach, Cap, PalaceOutside.Seen);
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
        // pillar -- and the way round the palace's west and south sides)
        var corridors = new List<PalaceCliff.Corridor>
        {
            new(new() { (sqX1 - 4, midZ), (sqX1 + PalaceCliff.Margin + PalaceCliff.Wobble + 10, midZ) }, CorridorHalf),
        };
        if (hallTile >= 0)
        {
            double hz0 = Math.Round(WZ(extents[hallTile].Z0) / 512);
            // (round the palace's west and south sides, RingOut cells out from them: from out past the hill's north edge -- where the race's
            // road from the round window comes up onto the hill, round the long hall's end -- south past the long hall and the sixteen rooms,
            // and east along their south side to its corner; the user, 2026-10-12: "Add a section where we're outside on the roof looping
            // around the palace roof tops")
            corridors.Add(new(new() { (sqX0 - RingOut, hz0 - 4), (sqX0 - RingOut, sqZ1 + RingOut), (sqX1 - 14, sqZ1 + RingOut) }, CorridorHalf));
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
            // (the palace's doors in the start room's east wall: the wall's face and their span, island cells; the teleporters: each [into the
            // first fireplace's hearth: x0, z0, x1, z1 (island cells), y0, y1; out of the second's: x, z (island cells), y, the turn])
            ["entrance"] = entrance is { } way ? new[] { way.X, way.Z0, way.Z1 } : null,
            ["teleports"] = teleports,
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

    // The floor's and the walls' paint (the user, 2026-10-13, the floor's: see Build; then "Whilst we're cleaning up the floor let's see if we
    // can make the colours on the walls more consistent too as they're currently very blocky"): colours of the grids' own, averaged.
    internal static readonly (float R, float G, float B) CarpetRed = (119, 42, 30);      // the carpets' red (their border's brown with it)
    private static readonly (float R, float G, float B) TileLight = (127, 139, 94), TileDark = (64, 82, 53);   // the floor tiles' greens
    private static readonly (float R, float G, float B) BandLight = (196, 156, 80), BandDark = (140, 100, 44);  // the tiles along the road: golds
    private static readonly (float R, float G, float B) Skirting = (64, 80, 48), Wallpaper = (112, 110, 70), Trim = (144, 112, 48), Cornice = (80, 80, 48);
    // A colour of the carpets: their red and the brown of their border (a red at least half as strong again as its green, near twice its blue).
    private static bool IsCarpet((float R, float G, float B) a) => a.R > 60 && a.R > 1.4f * a.G && a.R > 1.8f * a.B;

    // The floor's paint, cell by cell (x, z of the bottom layer's cells with a room's air over them): the race's track (`track`) red, the tiles
    // along it gold, the rest the tiles' greens.
    private static Dictionary<(int X, int Z), (float R, float G, float B)> PaintFloor(Dictionary<(int X, int Y, int Z), (int Brick, int Tile)> cells,
        HashSet<(int X, int Y, int Z)> air, int minY, IReadOnlySet<(int X, int Z)> track)
    {
        var floor = new HashSet<(int X, int Z)>();
        foreach (var (x, y, z) in cells.Keys)
            if (y == minY && air.Contains((x, minY + 1, z))) floor.Add((x, z));
        var road = floor.Where(track.Contains).ToHashSet();
        // (and the tiles along it in gold, a cell either side -- "Let's change the tile colour next to the track to a different colour than
        // the rest of the floor"; the rest in the greens)
        var paint = new Dictionary<(int X, int Z), (float R, float G, float B)>();
        foreach (var c in floor)
        {
            if (road.Contains(c)) { paint[c] = CarpetRed; continue; }
            var edge = false;
            for (var dx = -1; dx <= 1 && !edge; dx++)
                for (var dz = -1; dz <= 1 && !edge; dz++) edge = road.Contains((c.X + dx, c.Z + dz));
            var light = ((c.X + c.Z) & 1) == 0;
            paint[c] = edge ? (light ? BandLight : BandDark) : light ? TileLight : TileDark;
        }
        return paint;
    }

    // A room's wall at a layer over the floor's (1 the lowest, `top` the highest): a skirting, a gold trim over it, the wallpaper, a gold
    // trim under the top and a cornice -- and the grids' red pillars red, as the carpets (`brick`: the colour of the cell's own brick, when
    // it has one drawn).
    private static (float R, float G, float B) PaintWall(int layer, int top, (float R, float G, float B)? brick)
    {
        if (brick is { } b && IsCarpet(b) && b.R > 1.8f * b.G) return CarpetRed;
        return layer <= 2 ? Skirting : layer == 3 || layer == top - 3 ? Trim : layer >= top ? Cornice : Wallpaper;
    }

    // The palace's doors (Build's entrance): on a wall's plane `x` (its face looking -x, into the room), from z0 to z1 and from y0 (the floor's
    // top) to y1 -- two leaves of dark wood, each with two raised panels and a gold handle by the gold seam between them -- in a frame
    // standing out from the wall, a crest over it. Each face shaded the way it looks (`shaded`: a colour and a shade to the island's palette).
    private static readonly (float R, float G, float B) DoorWood = (92, 36, 22), DoorPanel = (132, 56, 32), DoorGold = (200, 154, 66), DoorFrame = (164, 124, 54);
    private static void EntranceDoors(List<BrickBuilding.Face> list, double x, double z0, double z1, double y0, double y1, Func<(float R, float G, float B), float, int> shaded)
    {
        // (a box against the wall: its faces but the back, each in its shade)
        void Box(double xa, double ya, double za, double xb, double yb, double zb, (float R, float G, float B) c)
        {
            float a = (float)xa, b = (float)xb, lo = (float)ya, hi = (float)yb, za2 = (float)za, zb2 = (float)zb;
            void Q(Vector3 p, Vector3 q, Vector3 r, Vector3 t, Vector3 n, float shade) => list.Add(new BrickBuilding.Face(new[] { p, q, r, t }, shaded(c, shade), n));
            Q(new(a, hi, za2), new(b, hi, za2), new(b, hi, zb2), new(a, hi, zb2), Vector3.UnitY, 1.00f);
            Q(new(a, lo, za2), new(b, lo, za2), new(b, lo, zb2), new(a, lo, zb2), -Vector3.UnitY, 0.50f);
            Q(new(a, lo, za2), new(b, lo, za2), new(b, hi, za2), new(a, hi, za2), -Vector3.UnitZ, 0.70f);
            Q(new(a, lo, zb2), new(b, lo, zb2), new(b, hi, zb2), new(a, hi, zb2), Vector3.UnitZ, 0.70f);
            Q(new(a, lo, za2), new(a, lo, zb2), new(a, hi, zb2), new(a, hi, za2), -Vector3.UnitX, 0.84f);
        }
        double h = y1 - y0, zm = (z0 + z1) / 2;
        // (the leaves, on the wall's plane where its faces are left out)
        foreach (var (za, zb) in new[] { (z0, zm), (zm, z1) })
        {
            list.Add(new BrickBuilding.Face(new[] { new Vector3((float)x - 2, (float)y0, (float)za), new Vector3((float)x - 2, (float)y0, (float)zb),
                new Vector3((float)x - 2, (float)y1, (float)zb), new Vector3((float)x - 2, (float)y1, (float)za) }, shaded(DoorWood, 0.84f), -Vector3.UnitX));
            double w = zb - za, i0 = za + w * 0.16, i1 = zb - w * 0.16;
            Box(x - 70, y0 + h * 0.07, i0, x - 2, y0 + h * 0.42, i1, DoorPanel);
            Box(x - 70, y0 + h * 0.50, i0, x - 2, y1 - h * 0.07, i1, DoorPanel);
        }
        Box(x - 90, y0, zm - 70, x - 2, y1, zm + 70, DoorGold);
        foreach (var sg in new[] { -1, 1 })
            Box(x - 260, y0 + h * 0.44, Math.Min(zm + sg * 200, zm + sg * 330), x - 90, y0 + h * 0.52, Math.Max(zm + sg * 200, zm + sg * 330), DoorGold);
        // (the frame: its two sides, the lintel and the crest over it)
        const double Out = 340, Wide = 760;
        Box(x - Out, y0, z0 - Wide, x, y1 + Wide, z0, DoorFrame);
        Box(x - Out, y0, z1, x, y1 + Wide, z1 + Wide, DoorFrame);
        Box(x - Out, y1, z0, x, y1 + Wide, z1, DoorFrame);
        Box(x - Out - 40, y1 + Wide, zm - 520, x, y1 + Wide + 560, zm + 520, DoorGold);
    }

    // A roof slab's sides and bottom (its top is the rock: RockRoof).
    private static void Slab(List<BrickBuilding.Face> list, Vector3 lo, Vector3 hi, int side, float cell)
    {
        float xa = lo.X, ya = lo.Y, za = lo.Z, xb = hi.X, yb = hi.Y, zb = hi.Z;
        void Q(Vector3 a, Vector3 b, Vector3 c, Vector3 e, Vector3 n) => list.Add(new BrickBuilding.Face(new[] { a, b, c, e }, side, n));
        // (its underside -- the room's ceiling, seen from in it -- in squares `cell` across at the most: Faces' NearRun)
        for (var i = 0; xa + i * cell < xb - 0.5f; i++)
            for (var k = 0; za + k * cell < zb - 0.5f; k++)
            {
                float x0 = xa + i * cell, z0 = za + k * cell, x1 = Math.Min(xb, x0 + cell), z1 = Math.Min(zb, z0 + cell);
                Q(new(x0, ya, z0), new(x1, ya, z0), new(x1, ya, z1), new(x0, ya, z1), -Vector3.UnitY);
            }
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
                // (the header's first byte: the body format's version, 0x10 as every retail body has it -- the engine reads textured polygons by
                // it; and drawn close to the camera, as the rooms are: BrickBuilding.CloseFlag)
                var header = new byte[96];
                BinaryPrimitives.WriteInt32LittleEndian(header, 0x10 | BrickBuilding.CloseFlag);
                var body = new Body
                {
                    Game = 2, Static = true, Lit = false, Header = header, Vertices = points,
                    Bones = new List<Bone> { new(0, points.Count, 0, -1, new byte[8]) },
                    Faces = bodyFaces, Textures = new[] { RoofRock },
                };
                var number = firstBody + newBodies.Count;
                newBodies.Add(body.Write());
                bodies++; faces += bodyFaces.Count;
                // (in every cube it can be seen from: the engine draws the decors of the 3 x 3 cubes round the camera's, and from further
                // the roof was see-through -- PalaceOutside.Seen)
                BrickBuilding.AddToCubes(island, number, 0, origin.X, (int)Math.Round(origin.Y), origin.Z, lx, hx, (int)Math.Floor(ly), BrickBuilding.NoBoxTop, lz, hz, maxPerCube, PalaceOutside.Seen);
            }
        return (bodies, faces);
    }

    // BrickBuilding.Faces, a face's colour told its cell and the way it faces too (the walls' outer faces are coloured apart from their
    // inner ones): the faces that show, merged into rectangles of one colour, group by group -- none more than NearRun cells across (along
    // a wall: up it as far as MaxRun).
    //
    // (The engine leaves out a body's polygon with any corner behind the camera's near plane: a floor, a ceiling or a wall merged a dozen
    // cells long, passing under or beside a camera low in a room, went from the picture whole -- the hill's paving under the rooms showed
    // through the floor near the camera, the sky through the long hall's roof (the user, 2026-10-12: the palace's rooms with no road in them,
    // the floor was the cars' own). A cell across, 2,048: what goes is under the bottom of the picture at a near plane of 2,000 or less --
    // the game's 3,000 at 480 lines high, scaled down for more, EXTFUNC.CPP)
    // (Since 2026-10-13 the engine cuts such polygons at the near plane instead -- but keep the cell: the filler takes a polygon's depth
    // straight across the screen, not as the perspective has it, and on a rectangle 12 cells long running away from the camera that is
    // too far in its middle -- the ground under the floor, 700 below, came through in big patches again; tried and measured: 10 % faster.)
    private const int NearRun = 1;
    // (`skip`: faces left out -- a cell's, the way it looks: where a picture hangs, PalacePortraits)
    private static Dictionary<int, List<BrickBuilding.Face>> Faces(Dictionary<(int X, int Y, int Z), (int Brick, int Tile)> cells,
        Func<(int X, int Y, int Z), int, int, float, int> colour, Func<int, int, int, Vector3> corner, HashSet<((int X, int Y, int Z) Cell, int Dir)>? skip = null)
    {
        var planes = new Dictionary<(int Group, int Dir, int Plane), Dictionary<(int U, int V), int>>();
        foreach (var ((x, y, z), (brick, group)) in cells)
            for (var d = 0; d < BrickBuilding.Directions.Length; d++)
            {
                var (dx, dy, dz, shade) = BrickBuilding.Directions[d];
                if (cells.ContainsKey((x + dx, y + dy, z + dz)) || skip?.Contains(((x, y, z), d)) == true) continue;
                var key = (group, d, dy != 0 ? y : dx != 0 ? x : z);
                if (!planes.TryGetValue(key, out var plane)) planes[key] = plane = new();
                plane[dy != 0 ? (x, z) : dx != 0 ? (z, y) : (x, y)] = colour((x, y, z), d, brick, shade);
            }
        var faces = new Dictionary<int, List<BrickBuilding.Face>>();
        foreach (var ((group, d, at), plane) in planes)
        {
            var (dx, dy, dz, _) = BrickBuilding.Directions[d];
            var list = faces.TryGetValue(group, out var l) ? l : faces[group] = new();
            foreach (var (u0, v0, u1, v1, c) in dy != 0 ? BrickBuilding.Rectangles(plane, NearRun, NearRun) : BrickBuilding.Rectangles(plane, NearRun, BrickBuilding.MaxRun))
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
