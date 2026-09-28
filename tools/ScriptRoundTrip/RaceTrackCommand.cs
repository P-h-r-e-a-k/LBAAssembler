using LBAAssembler;
using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// Builds the Desert island race track into a copy of the island file and draws the result.
//   buildtrack <plan.json> <pristine folder> <game folder> [png] [scale]
// The three files the track changes (DESERT.ILE, DESERT.OBL, SCENE.HQR) are copied from the pristine folder to the game folder first, so a build always starts clean.
internal static class RaceTrackCommand
{
    public static int Run(string[] args)
    {
        var plan = RaceTrackPlan.Load(args[1]);
        var pristine = args[2]; var game = args[3];
        foreach (var f in new[] { "DESERT.ILE", "DESERT.OBL", "SCENE.HQR" }) File.Copy(Path.Combine(pristine, f), Path.Combine(game, f), overwrite: true);
        var island = IslandFile.Load(Path.Combine(game, "DESERT.ILE"));
        var options = new RaceTrackOptions();
        options.Keep.Add((0, 195, 62, 252));
        if (Environment.GetEnvironmentVariable("RT_CLEARANCE") is { } rc) options.RoadBridgeClearance = double.Parse(rc);
        if (Environment.GetEnvironmentVariable("RT_TILELEN") is { } tl) options.RoadBridgeTileLength = double.Parse(tl);
        if (options.Crossing == CrossingStyle.Bridge)
            options.DeckBodyIndex = RaceTrackDeckBody.AppendTo(Path.Combine(game, "DESERT.OBL"), options);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var report = RaceTrackBuilder.Build(island, plan, options);
        island.Save(Path.Combine(game, "DESERT.ILE"));
        var scenes = RaceTrackScenes.Apply(game, report, options);
        foreach (var l in scenes.Log) Console.WriteLine("  " + l);
        Console.WriteLine($"  {scenes.ActorsRemoved} actors removed from {scenes.ScenesChanged} scenes");
        Console.WriteLine($"built in {watch.Elapsed.TotalSeconds:0.0}s: lap {report.Length:0} cells, {report.Vertices} vertices levelled, {report.Cells} cells painted, {report.DecorsRemoved} props and {report.SolidDecorsRemoved} solid decors removed");
        foreach (var (a, b) in report.BridgeSpans) Console.WriteLine($"  bridge span: points {a}..{b}");
        foreach (var r in report.Removed.Where(r => r.Kind == "solid")) Console.WriteLine($"  removed solid decor: body {r.Body} in cube ({r.CubeX},{r.CubeZ})");
        foreach (var note in report.Notes) Console.WriteLine("  " + note);
        Console.WriteLine($"  {report.Arrows.Count} arrows");
        foreach (var c in report.Crossings) Console.WriteLine($"  crossing at cell ({c.X:0.0}, {c.Z:0.0}), height {c.Y:0}, angle {c.Angle:0} degrees");
        foreach (var b in report.BridgeCoords) Console.WriteLine($"  bridge from ({b.X0:0.0}, {b.Z0:0.0}) to ({b.X1:0.0}, {b.Z1:0.0})");
        foreach (var l in report.StartLine) Console.WriteLine($"  start line at cell ({l.X:0.0}, {l.Z:0.0}), height {l.Y:0}, heading ({l.DirX:0.00}, {l.DirZ:0.00})");
        foreach (var l in report.Placed) Console.WriteLine("  placed " + l);
        if (report.RoadBridge is { } rb) Console.WriteLine($"  road bridge: cell ({rb.X:0.0},{rb.Z:0.0}) dir ({rb.DirX:0.00},{rb.DirZ:0.00}) height {rb.Height:0} size {rb.Width / 512:0.#}x{rb.Length / 512:0.#} cells, deck body {options.DeckBodyIndex}");
        if (args.Length > 4)
        {
            var dir = game;
            var scale = args.Length > 5 ? int.Parse(args[5]) : 3;
            var renderer = new IslandMapRenderer(island, IslandMapRenderer.LoadPalette(dir, "DESERT"), scale);
            renderer.RenderAll(MapView.Terrain);
            PngWriter.Write(args[4], renderer.Pixels, renderer.PixelWidth, renderer.PixelHeight);
            Console.WriteLine($"{args[4]}: {renderer.PixelWidth}x{renderer.PixelHeight}");
        }
        return 0;
    }

    // herostart <game folder> <cell x> <cell z> [turn]: puts Twinsen's start in the scene of the cube that holds an island cell, on the ground there
    // (for looking at a place of the track in the game).
    public static int HeroStart(string[] args)
    {
        var game = args[1]; var cellX = double.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture); var cellZ = double.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
        var turn = args.Length > 4 ? int.Parse(args[4]) : 0;
        var island = IslandFile.Load(Path.Combine(game, "DESERT.ILE"));
        var store = new LBAAssembler.Scenes.SceneStore(LBAAssembler.Scenes.SceneGame.Lba2, game);
        var cx = (int)Math.Floor(cellX / 64); var cz = (int)Math.Floor(cellZ / 64);
        for (var scene = 55; scene <= 73; scene++)
        {
            var model = store.Load(scene);
            if (model.CubeMode != 1 || model.CubeX != cx || model.CubeY != cz) continue;
            var y = IslandOps.Altitude(island, cellX * 512, cellZ * 512) ?? 0;
            model.Hero.X = (int)Math.Round(cellX * 512 - cx * 32768.0); model.Hero.Z = (int)Math.Round(cellZ * 512 - cz * 32768.0); model.Hero.Y = (int)Math.Round(y) + 200; model.Hero.Beta = turn;
            store.Save(scene, model, allowErrors: true);
            Console.WriteLine($"scene {scene}: Twinsen at ({model.Hero.X},{model.Hero.Y},{model.Hero.Z})");
            return 0;
        }
        Console.WriteLine("no scene holds that cell");
        return 1;
    }

    // initbuggy <game folder> <scene>: the scene's buggy always exists (the car-quest test passes) and INIT_BUGGY(2) puts it at its own place.
    public static int InitBuggy(string[] args)
    {
        var store = new LBAAssembler.Scenes.SceneStore(LBAAssembler.Scenes.SceneGame.Lba2, args[1]);
        var scene = int.Parse(args[2]);
        var model = store.Load(scene);
        foreach (var actor in model.Actors.Skip(1).Where(a => a.Entity == RaceTrackScenes.BuggyEntity))
        {
            var life = actor.Life;
            if (life.Length > 10 && life[0] == 0x0C && life[1] == 0x0F && life[2] == 0x4A && life[4] == 0x03) life[4] = 0;
            for (var i = 6; i < Math.Min(30, life.Length - 1); i++)
                if (life[i] == 0x46 && life[i + 1] == 0x00) { life[i + 1] = 0x02; Console.WriteLine($"INIT_BUGGY(2) at byte {i}"); break; }
        }
        store.Save(scene, model, allowErrors: true);
        return 0;
    }

    // scripttext <game folder> <scene> <actor> life|track: the script as the editor's C text.
    public static int ScriptText(string[] args)
    {
        var store = new LBAAssembler.Scenes.SceneStore(LBAAssembler.Scenes.SceneGame.Lba2, args[1]);
        var scene = int.Parse(args[2]);
        var scripts = LBAAssembler.LbaScript.SceneScripts.Load(store.LoadRecord(scene), scene);
        Console.WriteLine(scripts.GetText(int.Parse(args[3]), args[4] == "life" ? LBAAssembler.LbaScript.ScriptKind.Life : LBAAssembler.LbaScript.ScriptKind.Track));
        return 0;
    }

    // driveprep <game folder> <cell x> <cell z> <turn>: the buggy stands on the ground at that island cell facing `turn`, always there (INIT_BUGGY 2), Twinsen two cells
    // behind it -- a place to start a test drive from.
    public static int DrivePrep(string[] args)
    {
        var game = args[1];
        var cellX = double.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture); var cellZ = double.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
        var turn = int.Parse(args[4]);
        var island = IslandFile.Load(Path.Combine(game, "DESERT.ILE"));
        var store = new LBAAssembler.Scenes.SceneStore(LBAAssembler.Scenes.SceneGame.Lba2, game);
        var cx = (int)Math.Floor(cellX / 64); var cz = (int)Math.Floor(cellZ / 64);
        for (var scene = 55; scene <= 73; scene++)
        {
            var model = store.Load(scene);
            if (model.CubeMode != 1 || model.CubeX != cx || model.CubeY != cz) continue;
            var y = (int)Math.Round(IslandOps.Altitude(island, cellX * 512, cellZ * 512) ?? 0);
            var dx = Math.Sin(turn * 2 * Math.PI / 4096); var dz = Math.Cos(turn * 2 * Math.PI / 4096);
            foreach (var actor in model.Actors.Skip(1).Where(a => a.Entity == RaceTrackScenes.BuggyEntity))
            {
                actor.X = (int)Math.Round(cellX * 512 - cx * 32768.0); actor.Z = (int)Math.Round(cellZ * 512 - cz * 32768.0); actor.Y = y; actor.Beta = turn;
                var life = actor.Life;
                for (var i = 6; i < Math.Min(30, life.Length - 1); i++) if (life[i] == 0x46 && life[i + 1] == 0x00) { life[i + 1] = 0x02; break; }
                model.Hero.X = actor.X - (int)(dx * 1024); model.Hero.Z = actor.Z - (int)(dz * 1024); model.Hero.Y = y + 200; model.Hero.Beta = turn;
            }
            store.Save(scene, model, allowErrors: true);
            Console.WriteLine($"scene {scene}: buggy at ({cellX},{cellZ}) turn {turn}");
            return 0;
        }
        return 1;
    }

    // bodyname <game> <index...>: BODY2.HQD names for a list of body indices.
    public static int BodyName(string[] args)
    {
        var names = HqdDescriptions.Load("BODY2.HQD", 0).Names;
        foreach (var a in args.Skip(1)) { var i=int.Parse(a); Console.WriteLine($"{i}: {(i+1<names.Count?names[i+1]:"?")}"); }
        return 0;
    }

    // scenenames <game>: every SCENE2.HQD name containing a word.
    public static int SceneNames(string[] args)
    {
        var names = HqdDescriptions.Load("SCENE2.HQD", 0).Names;
        var word = args[1].ToLowerInvariant();
        for (var i=0;i<names.Count;i++) if (names[i] is { } n && n.ToLowerInvariant().Contains(word)) Console.WriteLine($"{i-1}: {n}");
        return 0;
    }
}
