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
// The temple's way from its door to the stairs down (the zone into scene 11, cells x 16, z 19-21) had neither: the door's hall and the
// corridor east from it a layer under the halls beyond (8 to 9), a two-cell corridor, walls and ledges. The way made here (cells):
//   - the door's hall (x 0-14, z 52-61) and its corridor east (x 15-35, z 56-57) raised a layer to the halls' 9 -- every floor tile there
//     laid again a layer up (the door's own arrival raised with it: RaceTrackStory.TempleGate; the actors and points stood on it:
//     RaceTrackStory.TempleDrive);
//   - a doorway in the hall's north wall (x 12-14, z 51: layers 10-15 taken out -- the car's box stands over 1024 tall: at 10-13 the lintel
//     stopped it -- the wall's top left as its lintel), a bridge north across
//     the gap between the hall and the halls beyond (z 45-50, floor at 9, invisible walls either side), and a doorway through those halls'
//     south wall (z 44), the strip behind it (z 43) raised to 9 too, and the low bench in the way of the turn east (x 12, z 40-42) gone;
//   - from there east along z 40-42 and north along x 20-22 the halls are already one floor three cells wide, to the stairs down: there the
//     car's own door, to the Esmers' hangar (scene 12), on only while he drives (RaceTrackStory.TempleDrive) -- scene 11 is all stairs.
internal static class TempleOfBuDrive
{
    public const int Scene = 10, OldFloor = 8, NewFloor = 9, Raise = 256;
    private const int Size = 64, Height = 25;

    // the cells whose floor goes up a layer (the door's hall, its corridor east, the strip behind the far doorway)
    public static bool Raised(int x, int z) =>
        (x >= 0 && x <= 14 && z >= 52 && z <= 61) || (x >= 15 && x <= 35 && z >= 56 && z <= 57) || (x >= 12 && x <= 14 && z == 43);

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
        // (already done: the bridge's floor there)
        if (At(13, NewFloor, 47).Block != 0) { log.Add("The Temple of Bù's drive: in scene 10 already"); return log; }
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
        // the hall's north wall: a doorway (layers 10-13 out, the floor in it); the decoration in front of it, by the wall, out of the way
        for (var x = 12; x <= 14; x++)
        {
            var t = Tile(x, 51);
            Set(x, NewFloor, 51, t.Block, t.Pos);
            for (var y = NewFloor + 1; y <= 15; y++) Set(x, y, 51, 0, 0);
        }
        for (var y = NewFloor + 1; y <= 13; y++) Set(12, y, 52, 0, 0);
        if (At(12, NewFloor, 52).Block == 0) { var b = At(12, OldFloor, 52); Set(12, NewFloor, 52, b.Block, b.Pos); }
        // the bridge: two layers of the hall's tiles, invisible walls either side
        for (var z = 45; z <= 50; z++)
        {
            for (var x = 12; x <= 14; x++)
            {
                var t = Tile(x, z);
                Set(x, OldFloor, z, t.Block, t.Pos);
                Set(x, NewFloor, z, t.Block, t.Pos);
            }
            for (var y = NewFloor; y <= 13; y++) { Set(11, y, z, 0, 1); Set(15, y, z, 0, 1); }
        }
        // the halls' south wall: a doorway (the wall's foot the floor, its height out)
        for (var x = 12; x <= 14; x++)
        {
            var t = Tile(x, 44);
            Set(x, NewFloor, 44, t.Block, t.Pos);
            for (var y = NewFloor + 1; y <= 13; y++) Set(x, y, 44, 0, 0);
        }
        // the bench in the way of the turn east: the halls' floor tile there instead (its neighbour's, the pattern running along z)
        for (var z = 40; z <= 42; z++)
        {
            var t = At(13, NewFloor, z);
            Set(12, NewFloor, z, t.Block, t.Pos);
            Set(12, NewFloor + 1, z, 0, 0);
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
                $"the door's hall and its corridor east raised a layer ({raised} floor cells), a doorway north from the hall, a bridge over the gap, a " +
                $"doorway into the halls beyond, the bench in the turn east gone: {edits.Count} cells");
        return log;
    }
}
