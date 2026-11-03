using LBAAssembler;
using LBAAssembler.Maps;
using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// maptrack <game> <out folder>: File > New track's path from end to end without the editor -- a new island of an L of four squares in the
// Desert's look (MapNew, its shape), saved as a map and read back with a race track in it (LbaMap.RaceTrack), a road of pieces laid across
// it (TrackPieceSet) made a plan from a flag at its start, and the track built as the map's island's (RaceTrackIsland.Custom) into a
// scratch copy of the game folder (<game>\_MAPTRACK_TEST, LiveDataRoot): what the build says, and the race file's island and scenes.
internal static class MapTrackCommand
{
    public static int Run(string[] args)
    {
        var game = args[1]; var outDir = args[2];
        Directory.CreateDirectory(outDir);
        var squares = new[] { (0, 0), (1, 0), (2, 0), (0, 1) };
        var map = MapNew.Lba2Island(game, "L test", "LTEST", "DESERT", 3, 2, 1200, squares: squares);
        Console.WriteLine($"the island: {map.Scenes.Count} scenes (cubes {string.Join(" ", map.Scenes.Select(s => $"{s.CubeX},{s.CubeZ}"))}), ILE {map.Ile!.Length} bytes");
        var island = IslandFile.Parse(map.Ile, "LTEST.ILE");
        Console.WriteLine($"  cubes present: {string.Join(" ", island.Cubes.Keys.Select(k => $"{k}"))}");
        // (the heights along the L's middle, from the sea through the land: the beach at its edges)
        foreach (var (x, z) in new[] { (390.0, 480.0), (420.0, 480.0), (500.0, 480.0), (570.0, 480.0), (420.0, 540.0), (470.0, 540.0) })
            Console.WriteLine($"  ground at cell ({x}, {z}): {IslandOps.Altitude(island, x * 512, z * 512)?.ToString("0") ?? "none"}");

        // the map saved and read back, with a track in it
        var mapPath = Path.Combine(outDir, "ltest.lbamap");
        map.RaceTrack = System.Text.Encoding.UTF8.GetBytes("{\"Width\":1,\"Pieces\":[]}");
        map.Save(mapPath);
        var back = LbaMap.Load(mapPath);
        Console.WriteLine($"map round trip: {back.Scenes.Count} scenes, race track {(back.RaceTrack is { } t ? System.Text.Encoding.UTF8.GetString(t) : "none")}");

        // a road across the L's long arm
        var set = new TrackPieceSet();
        var straight = TrackSection.Laid.First(s => s.Name == "Long straight");
        double Ground(double x, double z) => Math.Max(0, IslandOps.Altitude(island, x * 512, z * 512) ?? 0);
        var x0 = 415.0;
        set.Pieces.Add(new TrackPiece(straight, x0, 480, Ground(x0, 480), 0));
        set.Pieces.Add(set.Snapped(new TrackPiece(straight, x0 + 30.5, 480, Ground(x0 + 30, 480), 0)));
        set.Pieces.Add(set.Snapped(new TrackPiece(straight, x0 + 60.5, 480, Ground(x0 + 60, 480), 0)));
        var result = set.MakePlan(island, (x0, 480, Ground(x0, 480)), out var gridRoom);
        foreach (var n in result.Notes) Console.WriteLine("  note: " + n);
        foreach (var p in result.Problems) Console.WriteLine("  PROBLEM: " + p);
        if (result.Plan is not { } plan) return 1;

        // built as the map's island's into a scratch copy of the game folder
        var custom = RaceTrackIsland.Custom(back);
        var root = LiveDataRoot.Create(game, null, "_MAPTRACK_TEST") ?? throw new IOException("no scratch copy");
        var options = RaceTrackOptions.For(custom);
        if (!gridRoom) options.Drivers = new List<RaceDriver>();
        var built = RaceTrackService.Build(root.Directory, new[] { new RaceTrackService.TrackBuild(plan, options) });
        Console.WriteLine($"build: {(built.Ok ? "ok" : "FAILED")} -- {built.Summary}");
        foreach (var line in built.Log.Where(l => l.Contains("map") || l.Contains("MAPTRACK") || l.Contains("WARNING") || l.Contains("flag marshal") || l.Contains("line") || l.Contains("gantry") || l.Contains("grid")).Take(40)) Console.WriteLine("  " + line);
        File.WriteAllLines(Path.Combine(outDir, "build.log"), built.Log);
        if (built.Report is { } rep) Console.WriteLine($"report: start lines {rep.StartLine.Count}{(rep.StartLine.Count > 0 ? $" first ({rep.StartLine[0].X:0.0}, {rep.StartLine[0].Z:0.0}) y {rep.StartLine[0].Y:0}" : "")}, notes: {string.Join(" / ", rep.Notes.Take(6))}");
        Console.WriteLine($"scratch copy: {root.Directory} (left for the engine test)");
        if (!built.Ok) return 1;
        // (its race file, written here where the map's island is known by its name -- as in the editor, where the open map's is: the
        // engine test's car file, <out>\car.txt)
        if (args.Length > 3)
        {
            Environment.SetEnvironmentVariable("RT_RACE", custom.IleFile);
            RaceTrackCommand.RaceCarFile(new[] { "racecarfile", root.Directory, args[3] });
            Console.WriteLine($"race file {args[3]}: {string.Join(" | ", File.ReadAllLines(args[3]).Where(l => l.StartsWith("island_file=") || l.StartsWith("track=") || l.StartsWith("marshal=")))}");
        }
        return 0;
    }
}
