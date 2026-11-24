using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;

namespace LBAAssembler.Terrain;

// Kash's sea cave (the Desert, scene 113) driven into on the hovercraft (the user, 2026-11-24: "Twinsen is getting softlocked in the racer
// tomb cave with the hydroglyder ... We need to stop the scripting, have Twinsen back in the hydroglyder, and let's add a small slope that
// Twinsen can use to park on land"): the cave is a room the car may be driven into (drive_in=, RaceTrackStory.Desert's keys), the
// hovercraft held on its water there (RACEMOD.CPP HovercraftFrame, HoverRaftFrame), and here a ramp up out of the water onto the ledge by
// the tomb's steps -- the water's floor is layer 0 (its top 256), the ledge's layer 2 (768: the floor's tile, block 8, a 3 x 3 pattern),
// and indoors the car climbs slopes, never a step (project notes, 2026-11-15). Three cells wide (the car's box reaches about 658 either
// side of its middle), x 8-10: at z 28 a slope rising towards the ledge (-z) on layer 1 over a stone cube in the water's layer, at z 27
// the same on layer 2 over two cubes, and the ledge's lip at z 26 laid flat in its own floor's tile. The stone is the ledge's floor drawn
// as slopes and a cube (TempleOfBuSlopes), so the ramp is the floor it climbs to.
internal static class SeaCaveRamp
{
    public const int Scene = 113;
    private const int FloorTile = 8, Water = 111, FootZ = 28, HeadZ = 27, LipZ = 26, Ledge = 2, RampUp = 4, Stone = 1;
    private static readonly int[] Columns = { 8, 9, 10 };

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        if (!File.Exists(bkgPath)) return log;
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Scene * 2 + 1 >= table.Length) { log.Add($"Kash's sea cave's ramp left out: no scene {Scene}"); return log; }
        var g = new TempleOfBuDrive.GridCells(bkg, header, Scene, table[Scene * 2 + 1]);
        if (Columns.All(x => g.At(x, Ledge, HeadZ).Block != 0)) { log.Add("Kash's sea cave's ramp: in already"); return log; }
        if (Columns.Any(x => g.At(x, 0, FootZ).Block != Water || g.At(x, 0, HeadZ).Block != Water) || g.At(9, Ledge, 25).Block != FloorTile)
        {
            log.Add($"Kash's sea cave's ramp left out: scene {Scene}'s water and ledge aren't the game's there");
            return log;
        }
        var libraryEntry = header.Bll + g.Raw[0];
        var library = bkg.Read(libraryEntry);
        byte[]? Brick(int b) => b >= 0 && b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? bkg.Read(header.Brk + b) : null;
        var stone = TempleOfBuSlopes.AddBlocks(bkg, header, ref library, FloorTile, Brick);
        bkg.SetStored(libraryEntry, library);
        foreach (var x in Columns)
        {
            g.Set(x, 0, FootZ, stone[Stone], TempleOfBuDrive.Pattern(x, FootZ));
            g.Set(x, 1, FootZ, stone[RampUp], TempleOfBuDrive.Pattern(x, FootZ));
            g.Set(x, 0, HeadZ, stone[Stone], TempleOfBuDrive.Pattern(x, HeadZ));
            g.Set(x, 1, HeadZ, stone[Stone], TempleOfBuDrive.Pattern(x, HeadZ));
            g.Set(x, Ledge, HeadZ, stone[RampUp], TempleOfBuDrive.Pattern(x, HeadZ));
            g.Set(x, Ledge, LipZ, FloorTile, TempleOfBuDrive.Pattern(x, LipZ));
        }
        g.Write(bkg, header);
        bkg.SetStored(0, header.Bytes);
        File.WriteAllBytes(bkgPath, bkg.ToBytes());
        log.Add($"Kash's sea cave's ramp: scene {Scene}'s grid {g.Number}, cells x {Columns[0]}-{Columns[^1]}, z {HeadZ}-{FootZ} up out of the water onto the ledge " +
                $"(the stone's blocks {string.Join(", ", stone.Select(p => $"{p.Value} (shape {p.Key})"))})");
        return log;
    }
}
