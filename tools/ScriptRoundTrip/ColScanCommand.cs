using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// colscan <built .ILE> <original .ILE> [gx0 gz0 gx1 gz1]: the ground cells Twinsen can't walk on -- a triangle with the blocking bit and
// not the cars-only wall (game code 10, which the engine opens to him on foot) -- each with its height step across the cell, whether the
// original island had it so, and its game code; grouped into runs along x, a line per run.
internal static class ColScanCommand
{
    public static int Run(string[] args)
    {
        var built = IslandFile.Load(args[1]);
        var original = IslandFile.Load(args[2]);
        // (colscan <A .ILE> <B .ILE> heights gx0 gz0 gx1 gz1: the two islands' ground heights at each vertex, A/B, a row a line)
        if (args.Length > 7 && args[3] == "heights")
        {
            int hx0 = int.Parse(args[4]), hz0 = int.Parse(args[5]), hx1 = int.Parse(args[6]), hz1 = int.Parse(args[7]);
            Console.WriteLine("gz\\gx " + string.Join(" ", Enumerable.Range(hx0, hx1 - hx0 + 1).Select(x => $"{x,11}")));
            for (var gz = hz0; gz <= hz1; gz++)
                Console.WriteLine($"{gz,5} " + string.Join(" ", Enumerable.Range(hx0, hx1 - hx0 + 1).Select(x => $"{built.HeightAt(x, gz)?.ToString() ?? "-",5}/{original.HeightAt(x, gz)?.ToString() ?? "-",-5}")));
            return 0;
        }
        int x0 = 0, z0 = 0, x1 = IslandFile.GridSize, z1 = IslandFile.GridSize;
        if (args.Length > 6) { x0 = int.Parse(args[3]); z0 = int.Parse(args[4]); x1 = int.Parse(args[5]); z1 = int.Parse(args[6]); }
        bool Blocked(IslandFile f, int gx, int gz, out int code)
        {
            code = -1;
            if (f.CubeAt(gx / IslandCube.Cells, gz / IslandCube.Cells) is not { HasPolygons: true } cube) return false;
            var blocked = false;
            for (var half = 0; half < 2; half++)
            {
                var p = new IslandPolygon(cube.Polygon(gx % IslandCube.Cells, gz % IslandCube.Cells, half));
                if (p.Col && p.CodeJeu != RaceTrackBuilder.CarWall) { blocked = true; code = p.CodeJeu; }
            }
            return blocked;
        }
        int Step(int gx, int gz)
        {
            var h = new[] { built.HeightAt(gx, gz), built.HeightAt(gx + 1, gz), built.HeightAt(gx, gz + 1), built.HeightAt(gx + 1, gz + 1) }.Where(v => v is not null).Select(v => (int)v!).ToList();
            return h.Count == 0 ? 0 : h.Max() - h.Min();
        }
        var total = 0; var fresh = 0;
        for (var gz = z0; gz < z1; gz++)
        {
            var run = -1; var runFresh = 0; var maxStep = 0; var codes = new HashSet<int>();
            void Flush(int end)
            {
                if (run < 0) return;
                Console.WriteLine($"z {gz}: x {run}..{end - 1} ({end - run} cells, {runFresh} new in the build), steepest step {maxStep}, codes {string.Join(",", codes)}");
                run = -1; runFresh = 0; maxStep = 0; codes.Clear();
            }
            for (var gx = x0; gx < x1; gx++)
            {
                if (!Blocked(built, gx, gz, out var code)) { Flush(gx); continue; }
                if (run < 0) run = gx;
                total++;
                if (!Blocked(original, gx, gz, out _)) { runFresh++; fresh++; }
                maxStep = Math.Max(maxStep, Step(gx, gz)); codes.Add(code);
            }
            Flush(x1);
        }
        Console.WriteLine($"{total} blocked cells ({fresh} new in the build)");
        return 0;
    }
}
