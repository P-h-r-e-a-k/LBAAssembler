using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// lavamap <ILE> <cube x> <cube z> [<raised file>]: a cube's cells as letters -- L lava/deadly ground, ~ under the sea, # a decor's box,
// = the raised road's deck over it, . ground -- and the heights (hundreds) of a band of it; the planning sheet for things put in the lava
internal static class LavaMap
{
    private static readonly HashSet<int> Deadly = new() { 1, 2, 8, 9, 11, 13, 14, 15 };
    public static int Run(string[] args)
    {
        var ile = IslandFile.Load(args[1]);
        int cx = int.Parse(args[2]), cz = int.Parse(args[3]);
        var cube = ile.CubeAt(cx, cz);
        if (cube is null) { Console.WriteLine("no cube"); return 1; }
        var deck = new HashSet<(int, int)>();
        if (args.Length > 4)
            foreach (var line in File.ReadAllLines(args[4]))
            {
                var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (p.Length < 4 || !double.TryParse(p[0], out var x) || !double.TryParse(p[1], out var z) || !double.TryParse(p[3], out var half) || half <= 0) continue;
                var r = (int)Math.Ceiling(half / 512) + 1;
                int mx = (int)Math.Floor(x / 512) - cx * 64, mz = (int)Math.Floor(z / 512) - cz * 64;
                for (var dz = -r; dz <= r; dz++) for (var dx = -r; dx <= r; dx++) if ((dx * dx + dz * dz) * 512.0 * 512 <= (half + 256) * (half + 256)) deck.Add((mx + dx, mz + dz));
            }
        var boxes = cube.Decors.Where(d => d.YMax > d.YMin).ToList();
        Console.WriteLine("    " + string.Concat(Enumerable.Range(0, 64).Select(x => (x % 10).ToString())));
        for (var z = 0; z < 64; z++)
        {
            var row = new char[64];
            for (var x = 0; x < 64; x++)
            {
                var h = IslandOps.Altitude(ile, (cx * 64 + x + 0.5) * 512, (cz * 64 + z + 0.5) * 512) ?? 0;
                var codes = cube.HasPolygons ? new[] { 0, 1 }.Select(half => new IslandPolygon(cube.Polygon(x, z, half)).CodeJeu).ToArray() : Array.Empty<int>();
                var c = h < 200 ? '~' : codes.Any(k => Deadly.Contains(k)) ? 'L' : '.';
                if (boxes.Any(b => x * 512 + 256 > b.XMin && x * 512 + 256 < b.XMax && z * 512 + 256 > b.ZMin && z * 512 + 256 < b.ZMax)) c = '#';
                if (deck.Contains((x, z))) c = c == 'L' ? '=' : c == '#' ? '%' : '-';
                row[x] = c;
            }
            Console.WriteLine($"{z,3} {new string(row)}");
        }
        if (args.Length > 6)
        {
            int x0 = int.Parse(args[5]), x1 = int.Parse(args[6]), z0 = int.Parse(args[7]), z1 = int.Parse(args[8]);
            Console.WriteLine("heights (hundreds):");
            for (var z = z0; z <= z1; z++)
                Console.WriteLine($"{z,3} " + string.Join(" ", Enumerable.Range(x0, x1 - x0 + 1).Select(x => ((int)((IslandOps.Altitude(ile, (cx * 64 + x + 0.5) * 512, (cz * 64 + z + 0.5) * 512) ?? 0) / 100)).ToString().PadLeft(3))));
        }
        return 0;
    }
}
