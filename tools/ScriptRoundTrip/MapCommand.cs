using LBAAssembler.Maps;

namespace ScriptRoundTrip;

// Maps in files of their own (Maps/: File > New map, Open map, Save map, Install map into game, Export map):
//   mapnew lba2interior <game folder> <base scene> <out.lbamap> [name]
//   mapnew lba1interior <LBA1 folder> <slot> <out.lbamap> [name]
//   mapnew lba2island <game folder> <file name> <base island> <cubes x> <cubes z> <land height> <out.lbamap> [name]
//   mapexport lba2interior|lba1interior <folder> <scene> <out.lbamap>  /  mapexport lba2island <folder> <island file> <out.lbamap>
//   mapinstall <map.lbamap> <folder>: installed into the folder (a sandbox: it writes the folder's files), and read back -- the map it gives
//     back the same as the map put in -- printing where it went
//   mapinfo <map.lbamap>
internal static class MapCommand
{
    public static int New(string[] args)
    {
        LbaMap map;
        string output;
        switch (args[1])
        {
            case "lba2interior": map = MapNew.Lba2Interior(args[2], int.Parse(args[3]), args.Length > 5 ? args[5] : "New interior"); output = args[4]; break;
            case "lba1interior": map = MapNew.Lba1Interior(args[2], int.Parse(args[3]), args.Length > 5 ? args[5] : "New interior"); output = args[4]; break;
            case "lba2island":
                map = MapNew.Lba2Island(args[2], args.Length > 9 ? args[9] : "New island", args[3], args[4], int.Parse(args[5]), int.Parse(args[6]), int.Parse(args[7]));
                output = args[8]; break;
            default: Console.WriteLine("mapnew lba2interior|lba1interior|lba2island ..."); return 1;
        }
        map.Save(output);
        Console.WriteLine($"{output}: {Describe(LbaMap.Load(output))}");
        return 0;
    }

    public static int Export(string[] args)
    {
        var map = args[1] switch
        {
            "lba2interior" => MapInstall.ExportLba2Interior(args[2], int.Parse(args[3]), $"Scene {args[3]}"),
            "lba1interior" => MapInstall.ExportLba1Interior(args[2], int.Parse(args[3]), $"Scene {args[3]}"),
            _ => MapInstall.ExportLba2Island(args[2], args[3], args[3]),
        };
        map.Save(args[4]);
        Console.WriteLine($"{args[4]}: {Describe(LbaMap.Load(args[4]))}");
        return 0;
    }

    public static int Install(string[] args)
    {
        var map = LbaMap.Load(args[1]);
        var at = MapInstall.Install(map, args[2]);
        Console.WriteLine($"installed into {args[2]}: scenes {string.Join(", ", at.Scenes)}{(at.Grid is { } g ? $", grid {g}" : "")}{(map.IslandFile is { } f ? $", island file {f}" : "")}");
        var back = MapInstall.Capture(at);
        var problems = Compare(map, back);
        Console.WriteLine(problems.Count == 0 ? "read back: the same map" : "read back DIFFERENT: " + string.Join("; ", problems));
        return problems.Count == 0 ? 0 : 1;
    }

    public static int Info(string[] args)
    {
        Console.WriteLine(Describe(LbaMap.Load(args[1])));
        return 0;
    }

    private static string Describe(LbaMap map) =>
        $"{map.KindName} \"{map.Name}\", island byte {map.IslandByte}{(map.Slot is { } s ? $", slot {s}" : "")}{(map.IslandFile is { } f ? $", file {f}" : "")}, " +
        $"{map.Scenes.Count} scene(s) ({string.Join(", ", map.Scenes.Select(x => $"{x.Record.Length} bytes" + (map.Kind == MapKind.Lba2Island ? $" at ({x.CubeX},{x.CubeZ})" : "")))})" +
        (map.Grid is { } g ? $", grid {g.Length} bytes" : "") + (map.Library is { } l ? $", library {l.Length} bytes" : "") +
        (map.Ile is { } i ? $", ILE {i.Length} bytes, OBL {map.Obl!.Length} bytes" : "");

    private static List<string> Compare(LbaMap a, LbaMap b)
    {
        var problems = new List<string>();
        if (a.Scenes.Count != b.Scenes.Count) problems.Add($"{a.Scenes.Count} scenes vs {b.Scenes.Count}");
        for (var k = 0; k < Math.Min(a.Scenes.Count, b.Scenes.Count); k++)
            if (!a.Scenes[k].Record.AsSpan().SequenceEqual(b.Scenes[k].Record)) problems.Add($"scene {k} differs");
        void Same(string what, byte[]? x, byte[]? y) { if ((x is null) != (y is null) || x is not null && !x.AsSpan().SequenceEqual(y)) problems.Add($"{what} differs"); }
        Same("grid", a.Grid, b.Grid); Same("library", a.Library, b.Library); Same("ILE", a.Ile, b.Ile); Same("OBL", a.Obl, b.Obl);
        return problems;
    }
}
