using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;

namespace LBAAssembler.Terrain.Citadel;

// The Tavern regulars' secret bar, the build's side: scene 19's hall (the Tralu's caves' second scene, room 21) made bigger and furnished
// (the user, 2026-11-06: "Let's move the bar scene area to scene '19: Citadel Island, Tralu 2nd scene (room #21)', it's supposed to be a
// secret easter egg for people to find. Let's expand the scene, perhaps a massive old fashioned wooden table in the centre, some smaller
// round tables around the edge"). The people, their lines and the scene's actors and zones are the story's (RaceTrackStory.TraluTavern).
// All of it out of the hall's own block library (library 1, the Tralu's caves' and the sewers'), cells set in its grid (Lba1GridEdit):
//   - the hall: the room's long back wall (x 39) taken out from z 35 to 53 and the floor carried west to x 28 -- the paving of the room
//     (block 14, 3 x 3 slabs), a hall of 18 x 19 cells with the room's own floor east of the old wall (x 40-45, cleared of its stalagmites);
//     its walls the room's own: the west wall (x 27) the old back wall's cells z for z (its barred windows with it), the north wall (z 34)
//     built as the room's north wall is (its stones, caps and merlons), and the front's rocky parapet (z 54) the room's own (blocks 47,
//     54, 32: four cells repeating) along the new stretch;
//   - the massive old table in its middle: the caves' wooden table (block 100, 2 x 4 cells, three layers -- the one in the cave's first
//     scene, scene 2) three times end to end, x 36-37, z 40-51, a bench (block 101, 1 x 3 cells, two layers) all along either side (x 35
//     and 38), plates, bowls, bottles and an oil lamp on it (blocks 102-110);
//   - the round tables round the edge: the caves' barrels (block 10, 2 x 2 cells, four layers) stood up as tall round tables, with a
//     bottle and a plate or a bowl on each -- by the west wall, at the front and by the old room;
//   - two of the caves' iron candlesticks (block 95) against the north wall, either side of where Luc's counter stands.
// Where the build finds scene 19's grid not the game's (the back wall not where it was), the hall is left as it is and said so.
internal static class TraluBarRoom
{
    public const int Scene = 19;
    // the hall: the new floor (x 28-39), the room's floor east of the old wall (x 40-45), z 35-53; the floor's layer (its top at 2304)
    public const int West = 28, OldWall = 39, East = 45, North = 35, South = 53, FloorLayer = 8;
    public const int FloorY = (FloorLayer + 1) * 256;
    // library 1's blocks: the paving, the table, the bench, a barrel, the candlestick, the parapet's rocky face (the front's columns x 41-44)
    private const int Paving = 14, Table = 100, Bench = 101, Barrel = 10, Candlestick = 95;
    private const int ParapetFrom = 41, ParapetPeriod = 4;
    // the massive table: x 36-37, z 40-51 (three tables end to end); its benches at x 35 and 38
    public const int TableX = 36, TableZ0 = 40, TableZ1 = 51, BenchWest = 35, BenchEast = 38;
    // the round tables (barrels): their north-west cells
    public static readonly (int X, int Z)[] Barrels = { (29, 37), (29, 44), (29, 50), (41, 51), (43, 39) };
    // what stands on them and on the table: (x, z, block) -- plates 108 109, bowls 102 104, bottles 105 106 107, the oil lamp 110 (the
    // tall ones on the table's near half, x 37: on the far half they hid the sitters on the far bench from the camera)
    private static readonly (int X, int Z, int Block)[] OnTable =
    {
        (36, 40, 108), (37, 41, 106), (36, 42, 109), (37, 43, 108), (37, 45, 104), (37, 47, 110), (36, 47, 107), (37, 49, 105), (36, 50, 102),
    };
    private static readonly (int X, int Z, int Block)[] OnBarrels =
    {
        (29, 37, 107), (30, 38, 108), (30, 44, 105), (29, 45, 109), (29, 50, 106), (30, 51, 102), (41, 51, 108), (42, 52, 107), (44, 39, 104), (43, 40, 106),
    };
    private static readonly (int X, int Z)[] Candlesticks = { (31, 35), (36, 35) };

    private const int Size = 64, Height = 25;

    // Whether a game folder's scene 19 is the bar's hall (the massive table's first cell where Install puts it)
    public static bool Built(string gameDirectory)
    {
        var backend = new Lba2GridBackend(gameDirectory);
        if (backend.GridOfScene(Scene) is not { } grid) return false;
        var cells = Lba1GridCodec.Decode(backend.LoadGrid(grid));
        return cells[((TableZ0 * Size + TableX) * Height + FloorLayer + 1) * 2] == Table && cells[((North * Size + OldWall) * Height + 13) * 2] == 0;
    }

    // Into a game folder: scene 19's grid made the bar's. A line for the build's log.
    public static string Install(string gameDirectory)
    {
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Scene * 2 + 1 >= table.Length) return "the secret bar's hall left out: no scene 19";
        var entry = header.Gri + table[Scene * 2 + 1];
        var raw = bkg.Read(entry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * Size + x) * Height + y) * 2;
        (int Block, int Pos)[] Column(int x, int z) => Enumerable.Range(0, Height).Select(y => ((int)cells[At(x, y, z)], (int)cells[At(x, y, z) + 1])).ToArray();
        // (the game's: the back wall from layer 9 to 20 all along the stretch, nothing west of it, the floor east of it)
        bool Wall(int z) => Enumerable.Range(9, 12).All(y => cells[At(OldWall, y, z)] != 0);
        if (raw[0] != 1 || !Enumerable.Range(North, South - North + 1).All(Wall)
            || Enumerable.Range(West - 1, OldWall - West + 1).Any(x => Column(x, 40).Any(c => c.Block != 0))
            || cells[At(OldWall + 1, FloorLayer, 40)] == 0)
            return "the secret bar's hall left out: scene 19's grid isn't the game's (its back wall at x 39, z 35-53)";

        var edits = new Dictionary<(int X, int Y, int Z), (int Block, int Pos)>();
        void Set(int x, int y, int z, int block, int pos) => edits[(x, y, z)] = (block, pos);
        void Put(int x, int z, (int Block, int Pos)[] column) { for (var y = 0; y < Height; y++) Set(x, y, z, column[y].Block, column[y].Pos); }
        var oldWall = Enumerable.Range(0, Size).ToDictionary(z => z, z => Column(OldWall, z));
        var front = Enumerable.Range(ParapetFrom, ParapetPeriod).ToDictionary(x => x, x => Column(x, South + 1));

        // the hall's floor: the paving from x 28 to the old wall, nothing over it; the room's floor east of it cleared of its stalagmites
        for (var z = North; z <= South; z++)
        {
            for (var x = West; x <= OldWall; x++)
            {
                for (var y = 0; y < Height; y++) Set(x, y, z, 0, 0);
                Set(x, FloorLayer, z, Paving, (x - West) % 3 + 3 * ((z - North) % 3));
            }
            for (var x = OldWall + 1; x <= East; x++)
                for (var y = FloorLayer + 1; y < Height; y++) Set(x, y, z, 0, 0);
        }
        // its walls: west (x 27) the old wall z for z, north (z 34) the old wall's plain stretch again, the corners the old wall's own;
        // the front (z 54) the room's parapet, four cells repeating, its south-west corner the old wall's
        for (var z = North; z <= South; z++) Put(West - 1, z, oldWall[z]);
        for (var x = West; x < OldWall; x++) Put(x, North - 1, NorthWall(x));
        Put(West - 1, North - 1, oldWall[North - 1]);
        for (var x = West; x <= OldWall; x++) Put(x, South + 1, front[ParapetFrom + ((x - ParapetFrom) % ParapetPeriod + ParapetPeriod) % ParapetPeriod]);
        Put(West - 1, South + 1, oldWall[South + 1]);

        // the massive table, its benches, what is on it
        for (var t = 0; t * 4 + TableZ0 <= TableZ1; t++)
            for (var dz = 0; dz < 4; dz++)
                for (var dy = 0; dy < 3; dy++)
                    for (var dx = 0; dx < 2; dx++)
                        Set(TableX + dx, FloorLayer + 1 + dy, TableZ0 + t * 4 + dz, Table, dx + 2 * dy + 6 * dz);
        foreach (var x in new[] { BenchWest, BenchEast })
            for (var b = 0; b * 3 + TableZ0 <= TableZ1; b++)
                for (var dz = 0; dz < 3; dz++)
                    for (var dy = 0; dy < 2; dy++)
                        Set(x, FloorLayer + 1 + dy, TableZ0 + b * 3 + dz, Bench, dy + 2 * dz);
        foreach (var (x, z, block) in OnTable) Item(x, FloorLayer + 4, z, block);
        // the round tables (barrels) and what is on them
        foreach (var (bx, bz) in Barrels)
            for (var dz = 0; dz < 2; dz++)
                for (var dy = 0; dy < 4; dy++)
                    for (var dx = 0; dx < 2; dx++)
                        Set(bx + dx, FloorLayer + 1 + dy, bz + dz, Barrel, dx + 2 * dy + 8 * dz);
        foreach (var (x, z, block) in OnBarrels) Item(x, FloorLayer + 5, z, block);
        foreach (var (x, z) in Candlesticks)
            for (var dy = 0; dy < 6; dy++) Set(x, FloorLayer + 1 + dy, z, Candlestick, dy);

        // (a column of the north wall, built as the room's own north wall (z 6) is -- the old back wall's columns are capped for a wall
        // along z, their caps two cells along z, and showed their stones' tops along x: its foot (layers 9-12, block 57, as the back wall's),
        // the stones (blocks 1-4 in turn, eight layers), its cap (block 18, two cells along x) and every sixth column a merlon (block 56))
        (int Block, int Pos)[] NorthWall(int x)
        {
            var column = new (int Block, int Pos)[Height];
            column[FloorLayer] = (0, 1);
            for (var y = FloorLayer + 1; y <= 12; y++) column[y] = (57, (y - FloorLayer - 1) % 2);
            for (var y = 13; y <= 20; y++) column[y] = (1 + (x - West) % 4, y - 13);
            if ((x - West) % 6 == 4) { column[21] = (56, 0); column[22] = (56, 1); }
            else column[21] = (18, (x - West) % 2);
            return column;
        }

        // (an item: a block one cell across, standing from layer y up -- the oil lamp three layers, a bottle 105 two)
        void Item(int x, int y, int z, int block)
        {
            var tall = block switch { 110 => 3, 105 => 2, _ => 1 };
            for (var dy = 0; dy < tall; dy++) Set(x, y + dy, z, block, dy);
        }

        var edited = Lba1GridEdit.SetCells(shape, edits.Select(e => new Lba1GridCell(e.Key.X, e.Key.Y, e.Key.Z, e.Value.Block, e.Value.Pos)).ToList());
        var stored = Lba2GridBackend.FromLba1Shape(raw, edited);
        bkg.SetStored(entry, stored);
        if (header.MaxGri < (uint)stored.Length) { header.MaxGri = (uint)stored.Length; bkg.SetStored(0, header.Bytes); }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Commit();
        return $"the secret bar: scene 19's hall carried west to x {West} (z {North}-{South}), the massive table at x {TableX}-{TableX + 1}, z {TableZ0}-{TableZ1} with its benches, " +
               $"{Barrels.Length} barrels for round tables, two candlesticks ({edits.Count} cells)";
    }
}
