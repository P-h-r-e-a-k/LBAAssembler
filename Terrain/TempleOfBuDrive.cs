using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;

namespace LBAAssembler.Terrain;

// The Temple of Bù (the Desert, scene 10) driven through (the user, 2026-11-18: "We need to improve the temple of Bu so Twinsen can drive
// through it. In my test earlier Twinsen had control taken from him and the game tried and failed to drive into the temple of Bu. We've
// already done work to implement indoor driving, so let's allow Twinsen to drive inside"). The car needs three cells of room (its box
// reaches about 658 either side of its middle) and one floor: indoors it drives down a step, never up one (project notes, 2026-11-15).
// Its way is the game's own, the minecart's loop (the user, 2026-11-23: "There is a complete path through the temple of Bu through to the
// spaceship area, the retail game takes it, which is why I asked you to remove your bridge" -- the bridge north from the door's hall that
// 11-19 made, a shortcut, gone again): the corridor east, round the east hall, down an incline to the bottom level, up one to the top,
// across, down one to the middle level, up the last into the halls before the stairs down (the zone into scene 11, cells x 16, z 19-21).
// Made drivable here (cells):
//   - the door's hall (x 0-14, z 52-61) and its corridor east (x 15-35, z 56-57) raised a layer to the halls' 9 -- every floor tile there
//     laid again a layer up (the door's own arrival raised with it: RaceTrackStory.TempleGate; the actors and points stood on it:
//     RaceTrackStory.TempleDrive); the corridor three planks wide, its south railing out a plank;
//   - the minecart's track floored over (the user, 2026-11-23: "replace them with smooth ground"), and each level's floor laid a layer up
//     to the track's height -- one flat floor a level, the inclines meeting it level;
//   - at the top, the strip between the two tracks' ends floored (a pillar in it gone); at the last incline's foot two columns gone;
//   - the inclines -- rails on a trestle -- smooth stone, drawn from the floor's own tile (TempleOfBuSlopes), with stone under them where
//     the ground is near;
//   - the car up the inclines: OBJECT.CPP's corners for the indoor car (CarInclineFoot);
//   - on through the stairs' door into scene 11, the temple's second room (drive_in=), the same done there: its track floored, its
//     levels raised, its inclines smooth stone, and a ramp from the track's end down to the corridor before the Esmers' base's door in
//     place of the ladder (the user, 2026-11-23: "Please remove the rails from this scene 11 ... any slopes that have no terrain under
//     them we should add a slope that'd driveable. We also need to add a slope to the doorway into the esmer base"); through that door
//     into the base, scene 12.
internal static class TempleOfBuDrive
{
    public const int Scene = 10, OldFloor = 8, NewFloor = 9, Raise = 256;
    private const int CorridorDeck = 70;
    private const int Size = 64, Height = 25;

    // the cells whose floor goes up a layer (the door's hall, its corridor east)
    public static bool Raised(int x, int z) =>
        (x >= 0 && x <= 14 && z >= 52 && z <= 61) || (x >= 15 && x <= 35 && z >= 56 && z <= 57);
    // Each level's floor laid again a layer up (Install): a floor tile on layer 0, 4 or 8 with three layers clear over it. The same cells
    // read off the game's own grid (the story's edit of the scene's actors runs before the grid's edit): the cell, its floor's old top --
    // what stood on it at that height goes up with it (RaceTrackStory.TempleDrive)
    private static readonly HashSet<int> FloorTiles = new() { 8, 12, 13, 14, 15, 44, 47, 48 };
    private static IEnumerable<(int X, int Low, int Z)> LevelCells(Func<int, int, int, (int Block, int Pos)> at, int[] lows)
    {
        foreach (var low in lows)
            for (var z = 0; z < Size; z++)
                for (var x = 0; x < Size; x++)
                {
                    if (!FloorTiles.Contains(at(x, low, z).Block)) continue;
                    var clear = true;
                    for (var y = low + 1; y <= low + 3 && clear; y++) { var c = at(x, y, z); clear = c.Block == 0 && c.Pos == 0; }
                    if (clear) yield return (x, low, z);
                }
    }
    public static Dictionary<(int X, int Z), int> RaisedTops(string gameDirectory, int scene = Scene)
    {
        var tops = new Dictionary<(int X, int Z), int>();
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        if (!File.Exists(bkgPath)) return tops;
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Lower * 2 + 1 >= table.Length) return tops;
        var lows = scene == Lower ? LowerLows : UpperLows;
        var upper = Lba1GridCodec.Decode(Lba2GridBackend.ToLba1Shape(bkg.Read(header.Gri + table[Scene * 2 + 1])));
        var cells = Lba1GridCodec.Decode(Lba2GridBackend.ToLba1Shape(bkg.Read(header.Gri + table[scene * 2 + 1])));
        (int Block, int Pos) At(int x, int y, int z) { var i = ((z * Size + x) * Height + y) * 2; return (cells[i], cells[i + 1]); }
        // (the grids' edit made already -- the builds run the grids' installs and the stories' edits in either order: a level's cells are
        // its floor tile laid on its floor tile, room over them -- scene 10's corridor's third plank row raised the sign of it)
        if (upper[((58 * Size + 20) * Height + NewFloor) * 2] == CorridorDeck)
        {
            foreach (var low in lows)
                for (var z = 0; z < Size; z++)
                    for (var x = 0; x < Size; x++)
                    {
                        if (!FloorTiles.Contains(At(x, low, z).Block) || !FloorTiles.Contains(At(x, low + 1, z).Block)) continue;
                        var clear = true;
                        for (var y = low + 2; y <= low + 3 && clear; y++) { var c = At(x, y, z); clear = c.Block == 0 && c.Pos == 0; }
                        if (clear) tops[(x, z)] = (low + 1) * 256;
                    }
            return tops;
        }
        foreach (var (x, low, z) in LevelCells(At, lows)) tops[(x, z)] = (low + 1) * 256;
        return tops;
    }

    // scene 11, the temple's second room (the stairs down from scene 10, the minecart on down to the Esmers' base's door): its levels'
    // floors and its track's beds (the floor 7, the bed 8; 11 and 12; 15 and 16)
    public const int Lower = 11;
    private static readonly int[] UpperLows = { 0, 4, 8 }, LowerLows = { 7, 11, 15 };
    private static readonly int[] RailBlocks = { 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63 };
    private static readonly int[] InclineBlocks = { 64, 65, 66, 67 };
    private const int Junction = 20, FloorTile = 8;

    // A grid's cells, editable: the game's, the edits over them, and the edits written back
    private sealed class GridCells
    {
        public readonly int Scene, Number, Entry;
        public byte[] Raw;
        private readonly byte[] cells;
        public readonly Dictionary<(int, int, int), Lba1GridCell> Edits = new();
        public GridCells(HqrFile bkg, MapBricks.BkgHeader header, int scene, int number)
        {
            Scene = scene; Number = number; Entry = header.Gri + number;
            Raw = bkg.Read(Entry);
            cells = Lba1GridCodec.Decode(Lba2GridBackend.ToLba1Shape(Raw));
        }
        public (int Block, int Pos) At(int x, int y, int z)
        {
            if (x < 0 || z < 0 || y < 0 || x >= Size || z >= Size || y >= Height) return (0, 0);
            var i = ((z * Size + x) * Height + y) * 2;
            return (cells[i], cells[i + 1]);
        }
        public (int Block, int Pos) Now(int x, int y, int z) => Edits.TryGetValue((x, y, z), out var e) ? (e.Block, e.Pos) : At(x, y, z);
        public void Set(int x, int y, int z, int block, int pos) { if (x >= 0 && z >= 0 && y >= 0 && x < Size && z < Size && y < Height) Edits[(x, y, z)] = new Lba1GridCell(x, y, z, block, pos); }
        public void Write(HqrFile bkg, MapBricks.BkgHeader header)
        {
            var shape = Lba1GridEdit.SetCells(Lba2GridBackend.ToLba1Shape(Raw), Edits.Values.ToList());
            Raw = Lba2GridBackend.FromLba1Shape(Raw, shape);
            bkg.SetStored(Entry, Raw);
            if (header.MaxGri < (uint)Raw.Length) header.MaxGri = (uint)Raw.Length;
            var library = bkg.Read(header.Bll + Raw[0]);
            var (bytes, n) = MapBricks.UsedBrickBytes(Raw, library, b => b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? BinaryPrimitives.ReadUInt32LittleEndian(bkg.ExtentOf(header.Brk + b)) : 0);
            if (header.MaxBrickCube < (uint)((n + 1) * 4 + bytes)) header.MaxBrickCube = (uint)((n + 1) * 4 + bytes);
            if (header.MaxMaskCube < (uint)((n + 1) * 4 + bytes + n * 64)) header.MaxMaskCube = (uint)((n + 1) * 4 + bytes + n * 64);
        }
    }

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        if (!File.Exists(bkgPath)) return log;
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Lower * 2 + 1 >= table.Length) { log.Add("The Temple of Bù's drive left out: no scenes 10 and 11"); return log; }
        var upper = new GridCells(bkg, header, Scene, table[Scene * 2 + 1]);
        // (already done: the corridor's third plank row raised there)
        if (upper.At(20, NewFloor, 58).Block == CorridorDeck) { log.Add("The Temple of Bù's drive: in scenes 10 and 11 already"); return log; }
        var lower = new GridCells(bkg, header, Lower, table[Lower * 2 + 1]);
        var sharers = Enumerable.Range(0, table.Length / 2).Where(s => s != Scene && s != Lower && (table[s * 2 + 1] == upper.Number || table[s * 2 + 1] == lower.Number)).ToList();
        // the smooth stone slopes and cube, drawn from the halls' floor tile, into the two rooms' library (TempleOfBuSlopes)
        var libraryEntry = header.Bll + upper.Raw[0];
        if (lower.Raw[0] != upper.Raw[0]) log.Add($"The Temple of Bù's drive: scene 11's library ({lower.Raw[0]}) not scene 10's ({upper.Raw[0]}) -- its inclines left as they are");
        var library = bkg.Read(libraryEntry);
        byte[]? Brick(int b) => b >= 0 && b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? bkg.Read(header.Brk + b) : null;
        var stone = TempleOfBuSlopes.AddBlocks(bkg, header, ref library, FloorTile, Brick);
        bkg.SetStored(libraryEntry, library);
        var counts = UpperRoom(upper, stone, library);
        var lowerCounts = lower.Raw[0] == upper.Raw[0] ? LowerRoom(lower, stone, library) : "left out";
        upper.Write(bkg, header);
        lower.Write(bkg, header);
        bkg.SetStored(0, header.Bytes);
        File.WriteAllBytes(bkgPath, bkg.ToBytes());
        log.Add($"The Temple of Bù's drive: the smooth stone's blocks {string.Join(", ", stone.Select(p => $"{p.Value} (shape {p.Key})"))}; scene 10's grid " +
                $"{upper.Number}: {counts}; scene 11's grid {lower.Number}: {lowerCounts}{(sharers.Count > 0 ? $" (shared with scenes {string.Join(", ", sharers)})" : "")}");
        return log;
    }

    // Scene 10: the door's hall and corridor raised, the corridor widened, the top's gap floored, the two columns gone, the track floored,
    // the levels raised to it, its inclines smooth stone
    private static string UpperRoom(GridCells g, Dictionary<int, int> stone, byte[] library)
    {
        (int Block, int Pos) At(int x, int y, int z) => g.At(x, y, z);
        void Set(int x, int y, int z, int block, int pos) => g.Set(x, y, z, block, pos);
        // the floor raised: each tile laid again a layer up, where nothing stands on it (a ramp's collision code there replaced)
        var raised = 0;
        for (var z = 0; z < Size; z++)
            for (var x = 0; x < Size; x++)
            {
                if (!Raised(x, z)) continue;
                var below = At(x, OldFloor, z);
                var on = At(x, NewFloor, z);
                if (below.Block == 0 || (on.Block != 0 || (on.Pos != 0 && on.Pos < 2))) continue;
                Set(x, NewFloor, z, below.Block, below.Pos);
                raised++;
            }
        // the wooden corridor east three planks wide for the car (it needs three cells): its deck four planks across on layer 8 (z 55-58),
        // the railings standing on the outer two, the doorways either end between posts -- two cells free. The south railing (z 58) moved
        // out a plank onto a fourth plank laid at z 59, its posts at the doorways (x 15 and 36) gone, the floor raised over z 58 as over
        // z 56-57: the corridor z 56-58, a railing either side
        for (var x = 16; x <= 35; x++)
        {
            var edge = At(x, OldFloor, 58);
            var middle = At(x, OldFloor, 57);
            Set(x, OldFloor, 59, edge.Block, edge.Pos);
            Set(x, NewFloor, 58, middle.Block, middle.Pos);
            for (var y = NewFloor; y <= NewFloor + 3; y++)
            {
                var rail = At(x, y, 58);
                Set(x, y, 59, rail.Block, rail.Pos);
                if (y > NewFloor) Set(x, y, 58, 0, 0);
            }
        }
        foreach (var x in new[] { 15, 36 })
        {
            var t = At(x, NewFloor, 57).Block != 0 ? At(x, NewFloor, 57) : At(x, OldFloor, 57);
            Set(x, NewFloor, 58, t.Block, t.Pos);
            for (var y = NewFloor + 1; y <= NewFloor + 3; y++) Set(x, y, 58, 0, 0);
        }
        // at the top of the hall's north-east corner the minecart's two tracks end two cells apart (the one up from the bottom level at
        // x 57-59, the one on down to the middle level at x 52-54), a strip a layer lower between them (x 55-56) that a walker steps down
        // into and up out of: floored over at the tracks' height, the pillar standing in it (x 56, z 12-14) gone -- the car drives up
        // out of the bottom level, across, and on down to the middle level. Its tile the floor's under it.
        for (var z = 10; z <= 14; z++)
            for (var x = 55; x <= 56; x++)
            {
                var t = At(x, OldFloor, z);
                Set(x, NewFloor, z, t.Block == 0 ? FloorTile : t.Block, t.Pos);
                for (var y = NewFloor + 1; y <= NewFloor + 5; y++) if (At(x, y, z).Block != 0) Set(x, y, z, 0, 0);
            }
        // the two columns flanking the foot of the incline up from the middle level into the halls (x 40, z 36 and 40, free-standing, layers
        // 5-12): the incline three cells wide between them, the car's box through with a fifth of a brick either side -- gone, the level's
        // floor laid where they stood, at its height (the floor's tile under them a layer up, as the level's all round)
        foreach (var z in new[] { 36, 40 })
            if (At(40, 5, z).Block == 7)
            {
                for (var y = 5; y <= 12; y++) if (At(40, y, z).Block == 7) Set(40, y, z, 0, 0);
                var f = At(40, 4, z);
                if (f.Block != 0) Set(40, 5, z, f.Block, f.Pos);
            }
        var rails = FloorTrack(g);
        var levelled = Level(g, UpperLows);
        var inclines = Smooth(g, stone, library);
        return $"the door's hall and its corridor east raised a layer ({raised} floor cells), the corridor three planks wide, the gap between the top's two " +
               $"tracks floored, the minecart's track floored over ({rails} cells), each level's floor at the track's height ({levelled} cells), its inclines " +
               $"smooth stone ({inclines} cells): {g.Edits.Count} cells";
    }

    // Scene 11: the track floored, the levels raised to it, the inclines smooth stone, and a ramp from the track's end down to the base's door
    private static string LowerRoom(GridCells g, Dictionary<int, int> stone, byte[] library)
    {
        var rails = FloorTrack(g);
        var levelled = Level(g, LowerLows);
        var inclines = Smooth(g, stone, library);
        // the way down to the Esmers' base's door (the user, 2026-11-23: "We also need to add a slope to the doorway into the esmer base"):
        // the track's last stretch ends on its platform at x 14 (layer 8, z 29-31), the corridor to the door (x 26) six layers under it
        // (layer 1, x 16-25, z 29-32), a wall with a ladder down between (x 15, layers 2-7) -- now a ramp in the smooth stone, falling a
        // layer a cell from the platform's edge to the corridor's floor (x 15-21: the slope at layer 8 - n, stone cubes under it down to
        // the corridor's floor), three cells wide in the wall's gap and four beside the corridor
        var ramp = 0;
        for (var n = 0; n <= 6; n++)
        {
            var x = 15 + n;
            var top = 8 - n;
            for (var z = 29; z <= (x == 15 ? 31 : 32); z++)
            {
                var pos = Pattern(x, z);
                g.Set(x, top, z, stone[5], pos);
                for (var y = 2; y < top; y++) g.Set(x, y, z, stone[1], pos);
                for (var y = top + 1; y <= top + 4; y++) if (g.Now(x, y, z) != (0, 0)) g.Set(x, y, z, 0, 0);
                ramp++;
            }
        }
        return $"the minecart's track floored over ({rails} cells), each level's floor at the track's height ({levelled} cells), its inclines smooth " +
               $"stone ({inclines} cells), a ramp down to the base's door ({ramp} columns): {g.Edits.Count} cells";
    }

    // the smooth stone's 3 x 3 pattern by place (its rows along z, its columns along x)
    private static int Pattern(int x, int z) => ((z % 3 + 3) % 3) * 3 + (x % 3 + 3) % 3;

    // The rail tracks gone, smooth floor in their place (the user, 2026-11-23: "We need to remove the rail tracks from the temple of Bu and
    // replace them with smooth ground"): every block of the minecart's track -- straights along x and z, curves, ends, the crossing -- the
    // stone floor tile under it laid in its place, at its own height. Under a track block something other than the floor tile (a grate, a
    // slab): the floor tile's 3 x 3 pattern carried on from the nearest one there. And the track's junctions (block 20, a 3 x 3 tile at the
    // floor's own level, its switch lever beside it): the floor tile, its pattern carried on from the floor round it.
    private static int FloorTrack(GridCells g)
    {
        (int Block, int Pos) FloorUnder(int x, int y, int z, int level)
        {
            if (level == y - 1 && y > 0 && g.At(x, y - 1, z) is { Block: FloorTile } under) return under;
            for (var r = 1; r <= 4 && level >= 0; r++)
                for (var dz = -r; dz <= r; dz++)
                    for (var dx = -r; dx <= r; dx++)
                    {
                        int nx = x + dx, nz = z + dz;
                        if (Math.Max(Math.Abs(dx), Math.Abs(dz)) != r || nx < 0 || nz < 0 || nx >= Size || nz >= Size) continue;
                        if (g.At(nx, level, nz) is not { Block: FloorTile } n || n.Pos > 8) continue;
                        // (the tile's rows run along z, its columns along x: pos = row * 3 + column)
                        int row = ((n.Pos / 3 - dz) % 3 + 3) % 3, col = ((n.Pos % 3 - dx) % 3 + 3) % 3;
                        return (FloorTile, row * 3 + col);
                    }
            return (FloorTile, Pattern(x, z));
        }
        var rails = 0;
        for (var y = 0; y < Height; y++)
            for (var z = 0; z < Size; z++)
                for (var x = 0; x < Size; x++)
                {
                    var block = g.Now(x, y, z).Block;
                    if (!RailBlocks.Contains(block) && block != Junction) continue;
                    var t = FloorUnder(x, y, z, block == Junction ? y : y - 1);
                    g.Set(x, y, z, t.Block, t.Pos);
                    rails++;
                }
        return rails;
    }

    // Each level of the halls one flat floor at the track's height (the user, 2026-11-23: "There is a complete path through the temple of Bu
    // through to the spaceship area, the retail game takes it" -- the minecart's way). The track's bed stood a layer over its level's floor:
    // the car drove a three-cell causeway, fell off it at a corner and couldn't climb back. Every floor tile of a level with room over it
    // (three layers clear) laid again a layer up, the track's height -- the floor looks the same, the car drives anywhere on it, the
    // inclines meet it level. Whatever stood on such a cell goes up with it (RaisedTops: RaceTrackStory.TempleDrive).
    private static int Level(GridCells g, int[] lows)
    {
        var levelled = 0;
        foreach (var (x, low, z) in LevelCells(g.Now, lows).ToList())
        {
            var f = g.Now(x, low, z);
            g.Set(x, low + 1, z, f.Block, f.Pos);
            levelled++;
        }
        return levelled;
    }

    // The minecart's inclines -- rails and sleepers on a trestle, nothing under them -- smooth stone (TempleOfBuSlopes): each incline block's
    // slope cell the stone slope the same way, its support under it a stone cube; where the ground is near under it (three layers or less)
    // stone down to it, the incline's body to the floor
    private static int Smooth(GridCells g, Dictionary<int, int> stone, byte[] library)
    {
        var shapes = InclineBlocks.ToDictionary(b => b, b => GridPaint.Entries(library, b).ToDictionary(e => e.Pos, e => e.Shape));
        var lowest = new Dictionary<(int X, int Z), int>();
        var n = 0;
        for (var y = 0; y < Height; y++)
            for (var z = 0; z < Size; z++)
                for (var x = 0; x < Size; x++)
                {
                    var (block, pos) = g.Now(x, y, z);
                    if (!shapes.TryGetValue(block, out var byPos)) continue;
                    var shape = byPos.TryGetValue(pos, out var s) ? s : 1;
                    g.Set(x, y, z, stone[stone.ContainsKey(shape) ? shape : 1], Pattern(x, z));
                    if (!lowest.ContainsKey((x, z))) lowest[(x, z)] = y;
                    n++;
                }
        foreach (var ((x, z), y0) in lowest)
        {
            var empty = 0;
            for (var y = y0 - 1; y >= 0 && g.Now(x, y, z).Block == 0; y--) empty++;
            if (empty == 0 || empty > 3 || y0 - empty - 1 < 0) continue;
            for (var y = y0 - 1; y >= y0 - empty; y--) g.Set(x, y, z, stone[1], Pattern(x, z));
        }
        return n;
    }
}
