using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// groundcells <ILE> <cube x> <cube z> <x0> <x1> <z0> <z1>: each ground cell's texture (IndexTex, bits 19-31 of its first triangle) and height
// in a cube, as a grid -- where a pattern painted on the ground lies (Otringal's square platform by the prison exit, 2026-11-15)
internal static class GroundCellsCommand
{
    public static int Run(string[] args)
    {
        var island = IslandFile.Load(args[1]);
        var cube = island.CubeAt(int.Parse(args[2]), int.Parse(args[3])) ?? throw new InvalidOperationException("no such cube");
        int x0 = int.Parse(args[4]), x1 = int.Parse(args[5]), z0 = int.Parse(args[6]), z1 = int.Parse(args[7]);
        Console.WriteLine("      " + string.Concat(Enumerable.Range(x0, x1 - x0 + 1).Select(x => $"{x,5}")));
        for (var z = z0; z <= z1; z++)
            Console.WriteLine($"z{z,3}: " + string.Concat(Enumerable.Range(x0, x1 - x0 + 1).Select(x => $"{(int)(cube.Polygon(x, z, 0) >> 19),5}")));
        return 0;
    }
}
