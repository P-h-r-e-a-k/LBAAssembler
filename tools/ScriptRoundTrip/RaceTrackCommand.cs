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
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var report = RaceTrackBuilder.Build(island, plan, options);
        island.Save(Path.Combine(game, "DESERT.ILE"));
        var scenes = RaceTrackScenes.Apply(game, 2, report.StartLine.Count > 0 ? report.StartLine[0] : null, report.DistanceToRoad);
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
}
