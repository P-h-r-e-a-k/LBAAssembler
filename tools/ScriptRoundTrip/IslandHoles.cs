using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// holes <built ILE> <retail ILE>: the ground's triangles drawn by nothing (no texture, no polygon: see-through) that no decor of the
// built island covers -- with whether a decor of the retail island covered them (taken off by the build), the cell's height and its
// corners' -- the island's transparent gaps
internal static class IslandHoles
{
    public static int Run(string[] args)
    {
        var built = IslandFile.Load(args[1]);
        var retail = args.Length > 2 ? IslandFile.Load(args[2]) : null;
        static bool Covered(IslandCube? cube, int x, int z) => cube is not null && cube.Decors.Any(d => d.YMax > d.YMin
            && x * 512 + 256 >= d.XMin && x * 512 + 256 <= d.XMax && z * 512 + 256 >= d.ZMin && z * 512 + 256 <= d.ZMax);
        var count = 0;
        foreach (var (cx, cz, cube) in IslandOps.CubeCells(built))
        {
            if (!cube.HasPolygons) continue;
            var rcube = retail?.CubeAt(cx, cz);
            for (var z = 0; z < 64; z++)
                for (var x = 0; x < 64; x++)
                {
                    var tris = new[] { 0, 1 }.Select(h => new IslandPolygon(cube.Polygon(x, z, h))).ToArray();
                    var hidden = tris.Select(t => t.TexFlag == 0 && t.PolyFlag == 0).ToArray();
                    if (!hidden.Any(h => h)) continue;
                    var corners = new[] { (0, 0), (1, 0), (0, 1), (1, 1) }.Select(c => built.HeightAt(cx * 64 + x + c.Item1, cz * 64 + z + c.Item2) ?? 0).ToArray();
                    if (corners.Max() <= 0) continue;   // (the sea's floor)
                    if (Covered(cube, x, z)) continue;
                    var rtris = rcube is { HasPolygons: true } ? new[] { 0, 1 }.Select(h => new IslandPolygon(rcube.Polygon(x, z, h))).ToArray() : null;
                    var wasHidden = rtris?.Any(t => t.TexFlag == 0 && t.PolyFlag == 0) ?? false;
                    var wasCovered = Covered(rcube, x, z);
                    var rcorners = retail is null ? corners : new[] { (0, 0), (1, 0), (0, 1), (1, 1) }.Select(c => retail.HeightAt(cx * 64 + x + c.Item1, cz * 64 + z + c.Item2) ?? 0).ToArray();
                    Console.WriteLine($"cube ({cx},{cz}) cell ({x},{z}) island ({cx * 64 + x},{cz * 64 + z}) hidden {(hidden[0] ? "A" : "")}{(hidden[1] ? "B" : "")} corners {string.Join(" ", corners)} retail: {(wasHidden ? "hidden" : "drawn")}{(wasCovered ? ", under a decor" : "")} rc {string.Join(" ", rcorners)} raised {corners.Max() - rcorners.Max()}");
                    count++;
                }
        }
        Console.WriteLine($"{count} cells with a see-through triangle no decor covers");
        return 0;
    }
}
