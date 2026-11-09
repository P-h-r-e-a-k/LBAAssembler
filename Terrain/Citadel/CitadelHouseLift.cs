using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;

namespace LBAAssembler.Terrain.Citadel;

// The lift in Twinsen's house down to the secret chamber (the user, 2026-11-07: "A hidden area in Twinsen's house accessed by an elevator,
// perhaps with 4 slightly darker squares that takes Twinsen and Zoe down into a secret area" -- the four squares marked on a screenshot):
// the storeroom up the half flight from the bedroom (scene 0's grid: its floor at layer 9, cells x 5-14, z 13-19, the stepladder, the
// bucket and the rolled-up rugs), the two by two of its floor between the stepladder and the low wall -- cells x 8-9, z 16-17 -- open
// down to layer 1, a shaft the lift's platform (flush with the floor, at Foot) covers and rides down (the user, 2026-11-09: "when we
// take the elevator down we just teleport into the downstairs scene instead of riding the elevator down"). The house at night is made
// from this grid after it (CitadelNightHouse), so it has the shaft too. The story's side -- the lift itself, the chamber and who goes down it -- is RaceTrackStory.SecretChamber.
internal static class CitadelHouseLift
{
    public const int House = 0, FloorLayer = 9, X0 = 8, X1 = 9, Z0 = 16, Z1 = 17;
    // the squares' middle and its floor (the top of layer 9)
    public static readonly (int X, int Y, int Z) Middle = ((X0 + 1) * 512, (FloorLayer + 1) * 256, (Z0 + 1) * 512);
    // the platform's foot, its top flush with the floor
    public static readonly (int X, int Y, int Z) Foot = (Middle.X, Middle.Y - CitadelProps.LiftTop, Middle.Z);

    public static string Install(string gameDirectory)
    {
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (House * 2 + 1 >= table.Length) return "the house's lift squares left out: scene 0 has no grid";
        var gridEntry = header.Gri + table[House * 2 + 1];
        var raw = bkg.Read(gridEntry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * 64 + x) * 25 + y) * 2;
        // (a second pass: the shaft is open already)
        var squares = new List<(int X, int Z)>();
        for (var z = Z0; z <= Z1; z++)
            for (var x = X0; x <= X1; x++)
                squares.Add((x, z));
        if (squares.All(q => cells[At(q.X, FloorLayer, q.Z)] == 0)) return "the house's lift shaft: open already";
        if (squares.Any(q => cells[At(q.X, FloorLayer, q.Z)] == 0)) return "the house's lift shaft left out: scene 0's storeroom floor isn't where it was";
        // the squares' column emptied from the floor down to layer 1 (the platform, flush with the floor, is the floor there)
        var edits = new List<Lba1GridCell>();
        foreach (var q in squares)
            for (var y = 1; y <= FloorLayer; y++)
                edits.Add(new Lba1GridCell(q.X, y, q.Z, 0, 0));
        var grid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, edits));
        bkg.SetStored(gridEntry, grid);
        if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
        bkg.SetStored(0, header.Bytes);
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Commit();
        return $"the house's lift shaft: scene 0's storeroom floor, cells x {X0}-{X1}, z {Z0}-{Z1}, open from layer {FloorLayer} down to 1 under the platform";
    }
}
