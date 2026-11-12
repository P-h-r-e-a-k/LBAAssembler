using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;

namespace LBAAssembler.Terrain.Citadel;

// The Tavern's potted plant, in the corner at the end of the bar, taken out of its grid (the user, 2026-11-12: "remove plant from the corner at
// the end of the bar and put the dart board against that wall instead"): block 73, four cells (x 1-2, z 14-15) four layers high on the
// flagstones, against the north wall (the stairs' retaining wall, whose face is at z 7168) -- the dartboard hangs there in its place.
internal static class TavernPlant
{
    private const int Scene = 3, Size = 64, Height = 25, X0 = 1, X1 = 2, Z0 = 14, Z1 = 15, PlantBlock = 73, FloorLayer = 3;

    public static string Remove(string gameDirectory)
    {
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Scene * 2 + 1 >= table.Length) return "the Tavern's plant left out: no scene 3";
        var entry = header.Gri + table[Scene * 2 + 1];
        var raw = bkg.Read(entry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        var edits = new List<Lba1GridCell>();
        for (var z = Z0; z <= Z1; z++)
            for (var x = X0; x <= X1; x++)
                for (var y = FloorLayer + 1; y < Height; y++)
                    if (cells[((z * Size + x) * Height + y) * 2] == PlantBlock) edits.Add(new Lba1GridCell(x, y, z, 0, 0));
        // (the flagstones under it were its shadow's darker tiles: the floor's own, as in the rows round it -- block 3, position 8 in the column
        // at x 1 on even rows and 4 on odd, 6 at x 2)
        for (var z = Z0; z <= Z1; z++)
            for (var x = X0; x <= X1; x++)
                if (edits.Count > 0) edits.Add(new Lba1GridCell(x, FloorLayer, z, 3, x == 1 ? (z % 2 == 0 ? 8 : 4) : 6));
        if (edits.Count == 0) return "the Tavern's plant: gone already (or the grid isn't the game's)";
        var stored = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, edits));
        bkg.SetStored(entry, stored);
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Commit();
        return $"the Tavern's plant: {edits.Count} bricks taken out of scene {Scene}'s grid, cells x {X0}-{X1}, z {Z0}-{Z1}";
    }
}
