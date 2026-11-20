using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;

namespace LBAAssembler.Terrain;

// Kash's tomb's cave (the Desert's sea cave, scene 113, where the giant clam kept the pearl: RaceTrackStory.Desert's KashTomb) without its
// two stone divers either side of the raised floor at the back -- the user, 2026-11-19: "There's a candle hidden behind a statue in the tomb
// area, let's get rid of the statues from both sides, keep the candles". Each is block 69 of the cave's grid, two cells by two (x 7-8 and
// 13-14, z 2-3) and five layers high (9-13) on the raised floor's tiles (layer 8, kept): those cells emptied, the back wall behind them
// whole as it was.
internal static class KashTombCave
{
    public const int Scene = 113, Statue = 69, FloorLayer = 8, StatueTop = 13;
    private const int Size = 64, Height = 25;
    private static readonly (int X0, int X1, int Z0, int Z1)[] Statues = { (7, 8, 2, 3), (13, 14, 2, 3) };

    public static List<string> RemoveStatues(string gameDirectory)
    {
        var log = new List<string>();
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        if (!File.Exists(bkgPath)) return log;
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Scene * 2 + 1 >= table.Length) { log.Add($"Kash's tomb's cave: no scene {Scene}"); return log; }
        var gridNumber = table[Scene * 2 + 1];
        var gridEntry = header.Gri + gridNumber;
        var raw = bkg.Read(gridEntry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        int BlockAt(int x, int y, int z) => cells[((z * Size + x) * Height + y) * 2];
        var edits = new List<Lba1GridCell>();
        foreach (var (x0, x1, z0, z1) in Statues)
            for (var x = x0; x <= x1; x++)
                for (var z = z0; z <= z1; z++)
                    for (var y = FloorLayer + 1; y <= StatueTop; y++)
                        if (BlockAt(x, y, z) == Statue) edits.Add(new Lba1GridCell(x, y, z, 0, 0));
        if (edits.Count == 0) { log.Add($"Kash's tomb's cave (scene {Scene}): its statues gone already"); return log; }
        shape = Lba1GridEdit.SetCells(shape, edits);
        raw = Lba2GridBackend.FromLba1Shape(raw, shape);
        bkg.SetStored(gridEntry, raw);
        if (header.MaxGri < (uint)raw.Length) header.MaxGri = (uint)raw.Length;
        bkg.SetStored(0, header.Bytes);
        File.WriteAllBytes(bkgPath, bkg.ToBytes());
        log.Add($"Kash's tomb's cave (scene {Scene}, grid {gridNumber}): the two stone divers either side of the tomb gone ({edits.Count} cells of block {Statue})");
        return log;
    }
}
