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
                    if (cells[i] != 0 || cells[i + 1] != 0) s += $" y{y}:b{cells[i]}/{cells[i + 1]}";
                }
                Console.WriteLine($"x{x} z{z}:{s}");
            }
        return 0;
    }
}

// a probe: the polygons' colours of OBJFIX.HQR entries (the inventory's models) -- args: gamedir entry...
internal static class ObjFixColours
{
    public static int Run(string[] args)
    {
        var hqr = LBAAssembler.HqrArchive.Open(Path.Combine(args[1], "OBJFIX.HQR"));
        foreach (var e in args.Skip(2).Select(int.Parse))
        {
            var body = LbaBodyStudio.Body.Read(hqr.Read(e), 2, allowStatic: true);
            Console.WriteLine($"OBJFIX {e}: " + string.Join(", ", body.Faces.GroupBy(f => f.Colour).OrderByDescending(g => g.Count()).Select(g => $"{g.Key}x{g.Count()}")));
        }
        return 0;
    }
}

// a probe: the scenes with an actor of entity E (and body B, -1 any) -- args: gamedir entity body
internal static class FindActor
{
    public static int Run(string[] args)
    {
        var store = new LBAAssembler.Scenes.SceneStore(LBAAssembler.Scenes.SceneGame.Lba2, args[1]);
        int entity = int.Parse(args[2]), body = int.Parse(args[3]);
        for (var s = 0; s < 420; s++)
        {
            if (!store.SceneExists(s)) continue;
            LBAAssembler.Scenes.SceneModel m;
            try { m = store.Load(s); } catch { continue; }
            for (var i = 0; i < m.Actors.Count; i++)
                if (m.Actors[i].Entity == entity && (body < 0 || m.Actors[i].Body == body))
                    Console.WriteLine($"scene {s} (island {m.Island}, cube {m.CubeX},{m.CubeY}): actor {i} at ({m.Actors[i].X},{m.Actors[i].Y},{m.Actors[i].Z})");
        }
        return 0;
    }
}
