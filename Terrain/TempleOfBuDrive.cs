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
//   - the car up the inclines: OBJECT.CPP's corners for the indoor car (CarInclineFoot);
//   - at the stairs down the car's own door, to the Esmers' hangar (scene 12), on only while he drives (RaceTrackStory.TempleDrive) --
//     scene 11 is all stairs.
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
    private static IEnumerable<(int X, int Low, int Z)> LevelCells(Func<int, int, int, (int Block, int Pos)> at)
    {
        foreach (var low in new[] { 0, 4, 8 })
            for (var z = 0; z < Size; z++)
                for (var x = 0; x < Size; x++)
                {
                    if (!FloorTiles.Contains(at(x, low, z).Block)) continue;
                    var clear = true;
                    for (var y = low + 1; y <= low + 3 && clear; y++) { var c = at(x, y, z); clear = c.Block == 0 && c.Pos == 0; }
                    if (clear) yield return (x, low, z);
                }
    }
    public static Dictionary<(int X, int Z), int> RaisedTops(string gameDirectory)
    {
        var tops = new Dictionary<(int X, int Z), int>();
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        if (!File.Exists(bkgPath)) return tops;
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Scene * 2 + 1 >= table.Length) return tops;
        var cells = Lba1GridCodec.Decode(Lba2GridBackend.ToLba1Shape(bkg.Read(header.Gri + table[Scene * 2 + 1])));
        (int Block, int Pos) At(int x, int y, int z) { var i = ((z * Size + x) * Height + y) * 2; return (cells[i], cells[i + 1]); }
        // (the grid's edit made already -- the builds run the grids' installs and the stories' edits in either order: a level's cells are
        // its floor tile laid on its floor tile, room over them)
        if (At(20, NewFloor, 58).Block == CorridorDeck)
        {
            foreach (var low in new[] { 0, 4, 8 })
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
        foreach (var (x, low, z) in LevelCells(At)) tops[(x, z)] = (low + 1) * 256;
        return tops;
    }

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        if (!File.Exists(bkgPath)) return log;
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Scene * 2 + 1 >= table.Length) { log.Add("The Temple of Bù's drive left out: no scene 10"); return log; }
        var gridNumber = table[Scene * 2 + 1];
        var sharers = Enumerable.Range(0, table.Length / 2).Where(s => s != Scene && table[s * 2 + 1] == gridNumber).ToList();
        var gridEntry = header.Gri + gridNumber;
        var raw = bkg.Read(gridEntry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        (int Block, int Pos) At(int x, int y, int z) { var i = ((z * Size + x) * Height + y) * 2; return (cells[i], cells[i + 1]); }
        // (already done: the corridor's third plank row raised there)
        if (At(20, NewFloor, 58).Block == CorridorDeck) { log.Add("The Temple of Bù's drive: in scene 10 already"); return log; }
        var edits = new Dictionary<(int, int, int), Lba1GridCell>();
        void Set(int x, int y, int z, int block, int pos) => edits[(x, y, z)] = new Lba1GridCell(x, y, z, block, pos);
        // the hall's floor tile for a cell, by the pattern's place (the same x, a row of the hall a multiple of three away)
        (int Block, int Pos) Tile(int x, int z)
        {
            for (var zs = 53; zs <= 61; zs++)
                if (((zs - z) % 3 + 3) % 3 == 0 && At(x, OldFloor, zs) is { Block: > 0 } t && At(x, NewFloor, zs).Block == 0) return t;
            return At(x, OldFloor, 56);
        }
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
                Set(x, NewFloor, z, t.Block == 0 ? 8 : t.Block, t.Pos);
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
        // the rail tracks gone, smooth floor in their place (the user, 2026-11-23: "We need to remove the rail tracks from the temple of Bu and
        // replace them with smooth ground"): every block of the minecart's track in the scene, at each of its three levels (layers 1, 5 and
        // 9 -- straights along x and z, curves, ends, the crossing) -- the stone floor tile under it laid in its place, at its own height: the
        // track's bed is a layer over the halls' floor, and the car's way along it (z 40-42, x 20-22) and the zones on it stay where they are.
        // Under a track block something other than the floor tile (a grate, a slab): the floor tile's 3 x 3 pattern carried on from the
        // nearest one there. And the track's junctions (block 20, a 3 x 3 tile at the floor's own level, its switch lever beside it): the
        // floor tile, its pattern carried on from the floor round it.
        var railBlocks = new HashSet<int> { 50, 51, 52, 53, 54, 55, 57, 58, 59, 61, 63 };
        const int Junction = 20;
        (int Block, int Pos) Now(int x, int y, int z) => edits.TryGetValue((x, y, z), out var e) ? (e.Block, e.Pos) : At(x, y, z);
        const int FloorTile = 8;
        (int Block, int Pos) FloorUnder(int x, int y, int z, int level)
        {
            if (level == y - 1 && y > 0 && At(x, y - 1, z) is { Block: FloorTile } under) return under;
            for (var r = 1; r <= 4 && level >= 0; r++)
                for (var dz = -r; dz <= r; dz++)
                    for (var dx = -r; dx <= r; dx++)
                    {
                        int nx = x + dx, nz = z + dz;
                        if (Math.Max(Math.Abs(dx), Math.Abs(dz)) != r || nx < 0 || nz < 0 || nx >= Size || nz >= Size) continue;
                        if (At(nx, level, nz) is not { Block: FloorTile } n || n.Pos > 8) continue;
                        // (the tile's rows run along z, its columns along x: pos = row * 3 + column)
                        int row = ((n.Pos / 3 - dz) % 3 + 3) % 3, col = ((n.Pos % 3 - dx) % 3 + 3) % 3;
                        return (FloorTile, row * 3 + col);
                    }
            return (FloorTile, 0);
        }
        var rails = 0;
        for (var y = 0; y < Height; y++)
            for (var z = 0; z < Size; z++)
                for (var x = 0; x < Size; x++)
                {
                    var block = Now(x, y, z).Block;
                    if (!railBlocks.Contains(block) && block != Junction) continue;
                    var t = FloorUnder(x, y, z, block == Junction ? y : y - 1);
                    Set(x, y, z, t.Block, t.Pos);
                    rails++;
                }
        // each level of the halls one flat floor at the track's height (the user, 2026-11-23: "There is a complete path through the temple
        // of Bu through to the spaceship area, the retail game takes it" -- the minecart's loop: the corridor east, round the east hall, down
        // to the bottom level, up, across, down to the middle level, up again into the halls before the stairs). The track's bed stood a
        // layer over its level's floor (the top level's floor 8, the bed 9; the middle's 4 and 5; the bottom's 0 and 1): the car drove a
        // three-cell causeway, fell off it at a corner and couldn't climb back. Every floor tile of a level with room over it (three layers
        // clear) laid again a layer up, the track's height -- the floor looks the same, the car drives anywhere on it, the inclines meet
        // it level. Whatever stood on such a cell goes up with it (RaisedTops: RaceTrackStory.TempleDrive).
        var levelled = 0;
        foreach (var (x, low, z) in LevelCells(Now))
        {
            var f = Now(x, low, z);
            Set(x, low + 1, z, f.Block, f.Pos);
            levelled++;
        }
        shape = Lba1GridEdit.SetCells(shape, edits.Values.ToList());
        raw = Lba2GridBackend.FromLba1Shape(raw, shape);
        bkg.SetStored(gridEntry, raw);
        if (header.MaxGri < (uint)raw.Length) header.MaxGri = (uint)raw.Length;
        var library = bkg.Read(header.Bll + raw[0]);
        var (bytes, n) = MapBricks.UsedBrickBytes(raw, library, b => b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? BinaryPrimitives.ReadUInt32LittleEndian(bkg.ExtentOf(header.Brk + b)) : 0);
        if (header.MaxBrickCube < (uint)((n + 1) * 4 + bytes)) header.MaxBrickCube = (uint)((n + 1) * 4 + bytes);
        if (header.MaxMaskCube < (uint)((n + 1) * 4 + bytes + n * 64)) header.MaxMaskCube = (uint)((n + 1) * 4 + bytes + n * 64);
        bkg.SetStored(0, header.Bytes);
        File.WriteAllBytes(bkgPath, bkg.ToBytes());
        log.Add($"The Temple of Bù's drive: scene 10's grid {gridNumber}{(sharers.Count > 0 ? $" (shared with scenes {string.Join(", ", sharers)})" : "")} -- " +
                $"the door's hall and its corridor east raised a layer ({raised} floor cells), the corridor three planks wide, the gap between the " +
                $"top's two tracks floored, the minecart's track floored over ({rails} cells), each level's floor at the track's height ({levelled} cells): " +
                $"{edits.Count} cells");
        return log;
    }
}
