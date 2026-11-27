using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// groundaudit <built ILE> <retail ILE> <out.csv>: every cell the build changed, one line each -- x z, its height (highest corner) built and
// retail, its rise (highest corner less lowest) built and retail, its two triangles' look built and retail (texflag/polyflag/bank/pos or
// the texture index), whether a triangle blocks (Col) and its ground code -- to find the island's clean-up: flat colours where there was
// texture, steep ground Twinsen can't walk where he could, heights far from the retail's
internal static class GroundAudit
{
    public static int Run(string[] args)
    {
        var built = IslandFile.Load(args[1]);
        var retail = IslandFile.Load(args[2]);
        using var w = new StreamWriter(args[3]);
        w.WriteLine("x,z,h,rh,rise,rrise,lookA,lookB,rlookA,rlookB,colA,colB,rcolA,rcolB,codeA,codeB");
        static string Look(IslandPolygon p) => p.TexFlag == 0 ? (p.PolyFlag == 0 ? "none" : $"flat{p.PolyFlag}:{p.Bank}:{p.SampleStep}") : $"tex{p.TexFlag}:{p.TextureIndex}:{p.Bank}";
        var rows = 0;
        foreach (var (cx, cz, cube) in IslandOps.CubeCells(built))
        {
            if (!cube.HasPolygons) continue;
            var rcube = retail.CubeAt(cx, cz);
            for (var z = 0; z < 64; z++)
                for (var x = 0; x < 64; x++)
                {
                    int gx = cx * 64 + x, gz = cz * 64 + z;
                    int[] C(IslandFile f) => new[] { (0, 0), (1, 0), (0, 1), (1, 1) }.Select(c => (int)(f.HeightAt(gx + c.Item1, gz + c.Item2) ?? 0)).ToArray();
                    var h = C(built); var rh = C(retail);
                    var a = new IslandPolygon(cube.Polygon(x, z, 0)); var b = new IslandPolygon(cube.Polygon(x, z, 1));
                    IslandPolygon ra = default, rb = default;
                    if (rcube is { HasPolygons: true }) { ra = new IslandPolygon(rcube.Polygon(x, z, 0)); rb = new IslandPolygon(rcube.Polygon(x, z, 1)); }
                    if (a.Raw == ra.Raw && b.Raw == rb.Raw && h.SequenceEqual(rh)) continue;
                    w.WriteLine($"{gx},{gz},{h.Max()},{rh.Max()},{h.Max() - h.Min()},{rh.Max() - rh.Min()},{Look(a)},{Look(b)},{Look(ra)},{Look(rb)},{(a.Col ? 1 : 0)},{(b.Col ? 1 : 0)},{(ra.Col ? 1 : 0)},{(rb.Col ? 1 : 0)},{a.CodeJeu},{b.CodeJeu}");
                    rows++;
                }
        }
        Console.WriteLine($"{args[3]}: {rows} cells changed");
        return 0;
    }
}

// steeplooks <built ILE> <retail ILE>: per cube, the commonest looks of its steep cells (a side rising 900 or more): all of them, and those
// the build left as they were -- which rock a cube's faces could be drawn in
internal static class SteepLooks
{
    public static int Run(string[] args)
    {
        var built = IslandFile.Load(args[1]);
        var retail = IslandFile.Load(args[2]);
        foreach (var (cx, cz, cube) in IslandOps.CubeCells(built))
        {
            if (!cube.HasPolygons) continue;
            var rcube = retail.CubeAt(cx, cz);
            var all = new Dictionary<string, int>(); var kept = new Dictionary<string, int>();
            for (var z = 0; z < 64; z++)
                for (var x = 0; x < 64; x++)
                {
                    var h = new[] { (0, 0), (1, 0), (0, 1), (1, 1) }.Select(c => built.HeightAt(cx * 64 + x + c.Item1, cz * 64 + z + c.Item2) ?? 0).ToArray();
                    if (h.Max() - h.Min() < 900) continue;
                    for (var half = 0; half < 2; half++)
                    {
                        var p = new IslandPolygon(cube.Polygon(x, z, half));
                        if ((p.TexFlag == 0 && p.PolyFlag == 0) || p.CodeJeu != 0) continue;
                        var key = p.TexFlag == 0 ? $"flat:{p.Bank}:{p.SampleStep}" : $"tex{p.TexFlag}:{p.TextureIndex}:{p.Bank}:{p.SampleStep}";
                        all[key] = all.GetValueOrDefault(key) + 1;
                        if (rcube is { HasPolygons: true } && rcube.Polygon(x, z, half) == p.Raw) kept[key] = kept.GetValueOrDefault(key) + 1;
                    }
                }
            if (all.Count == 0) continue;
            Console.WriteLine($"cube ({cx},{cz}) all: {string.Join(", ", all.OrderByDescending(k => k.Value).Take(5).Select(k => $"{k.Key} x{k.Value}"))}");
            Console.WriteLine($"           kept: {string.Join(", ", kept.OrderByDescending(k => k.Value).Take(5).Select(k => $"{k.Key} x{k.Value}"))}");
        }
        return 0;
    }
}
