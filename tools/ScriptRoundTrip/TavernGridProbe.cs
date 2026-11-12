using LBAAssembler;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;

// a probe: the non-empty cells of a scene's grid in a box -- args: gamedir scene x0 x1 z0 z1
internal static class TavernGridProbe
{
    public static int Run(string[] args)
    {
        var dir = args[1]; var scene = int.Parse(args[2]);
        int x0 = int.Parse(args[3]), x1 = int.Parse(args[4]), z0 = int.Parse(args[5]), z1 = int.Parse(args[6]);
        var bkg = HqrFile.Parse(File.ReadAllBytes(Path.Combine(dir, "LBA_BKG.HQR")));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        var raw = bkg.Read(header.Gri + table[scene * 2 + 1]);
        var cells = Lba1GridCodec.Decode(Lba2GridBackend.ToLba1Shape(raw));
        for (var z = z0; z <= z1; z++)
            for (var x = x0; x <= x1; x++)
            {
                var s = "";
                for (var y = 0; y < 25; y++)
                {
                    var i = ((z * 64 + x) * 25 + y) * 2;
                    if (cells[i] != 0) s += $" y{y}:b{cells[i]}/{cells[i + 1]}";
                }
                Console.WriteLine($"x{x} z{z}:{s}");
            }
        return 0;
    }
}
