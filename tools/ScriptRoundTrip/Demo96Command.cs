using LBAAssembler.Demo96;

namespace ScriptRoundTrip;

// demo96 <demo folder> <out folder> [retail folder]: the 1996 LBA2 demo converted into the retail layout (Demo96Converter), the log printed.
internal static class Demo96Command
{
    public static int Run(string[] args)
    {
        var retail = args.Length > 3 ? args[3] : @"E:\GOG Games\Little Big Adventure 2";
        foreach (var line in Demo96Converter.Convert(args[1], args[2], retail)) Console.WriteLine(line);
        return 0;
    }
}

// demo96rooms <game folder> <converted demo folder>: the demo's School of Magic, sewers and Tralu's caves' third hall into the game folder's
// scenes 27/28, 17/18 and 20 (Demo96Rooms).
internal static class Demo96RoomsCommand
{
    public static int Run(string[] args)
    {
        foreach (var line in Demo96Rooms.Install(args[1], args[2], Demo96Rooms.School)) Console.WriteLine(line);
        foreach (var line in Demo96Rooms.Install(args[1], args[2], Demo96Rooms.Sewers, Demo96Rooms.SewerTexts)) Console.WriteLine(line);
        foreach (var line in Demo96Rooms.Install(args[1], args[2], Demo96Rooms.TraluCave)) Console.WriteLine(line);
        return 0;
    }
}

// demo96pack <converted demo folder> <out.zip>: the School of Magic's, the sewers' and the Tralu's caves' third hall's package the program carries (Demo96Rooms.Pack).
internal static class Demo96PackCommand
{
    public static int Run(string[] args)
    {
        foreach (var line in Demo96Rooms.Pack(args[1], args[2], Demo96Rooms.School.Concat(Demo96Rooms.Sewers).Concat(Demo96Rooms.TraluCave).ToList())) Console.WriteLine(line);
        return 0;
    }
}
