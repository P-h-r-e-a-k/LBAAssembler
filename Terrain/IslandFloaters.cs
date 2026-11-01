namespace LBAAssembler.Terrain;

// The decors held up by nothing: not standing on the ground, a raised road's deck (its box's top the "no box" mark) or the rocky peak's layers
// (over 14,000 up), nor touching -- resting on, or fixed beside within a quarter of a cell -- something held up. Undrawn ground is the sea,
// however high its vertices are: Polar Island's posts stood on ground no one could see. (The tool's `floaters` shows them, and the race
// track's build clears the ones it leaves.)
internal static class IslandFloaters
{
    private const int Gap = 300;

    private static double Ground(IslandFile island, double x, double z)
    {
        int gx = (int)Math.Floor(x / 512), gz = (int)Math.Floor(z / 512);
        if (island.CubeAt(gx / IslandCube.Cells, gz / IslandCube.Cells) is { HasPolygons: true } cube && gx >= 0 && gz >= 0)
        {
            var p = new IslandPolygon(cube.Polygon(gx % IslandCube.Cells, gz % IslandCube.Cells, 0));
            if (p.TexFlag == 0 && p.PolyFlag == 0) return 0;
        }
        return IslandOps.Altitude(island, x, z) ?? -99999;
    }

    public static List<(int Cx, int Cz, IslandDecor Decor)> Find(IslandFile island)
    {
        var all = IslandOps.CubeCells(island).SelectMany(c => c.Item3.Decors.Select(d => (Cx: c.Item1, Cz: c.Item2, D: d))).ToList();
        var boxes = all.Select(t => (X0: t.Cx * 32768.0 + t.D.XMin, Z0: t.Cz * 32768.0 + t.D.ZMin, X1: t.Cx * 32768.0 + t.D.XMax, Z1: t.Cz * 32768.0 + t.D.ZMax)).ToList();
        var held = new bool[all.Count];
        for (var i = 0; i < all.Count; i++)
        {
            var (b, d) = (boxes[i], all[i].D);
            var points = new[] { ((b.X0 + b.X1) / 2, (b.Z0 + b.Z1) / 2), (b.X0 + 64, b.Z0 + 64), (b.X1 - 64, b.Z0 + 64), (b.X0 + 64, b.Z1 - 64), (b.X1 - 64, b.Z1 - 64) };
            held[i] = points.Max(p => Ground(island, p.Item1, p.Item2)) >= d.YMin - Gap || d.YMax < d.YMin || d.YMin > 14000;
        }
        bool Touch(int i, int j)
        {
            var (a, b) = (boxes[i], boxes[j]);
            var (da, db) = (all[i].D, all[j].D);
            return da.YMin <= db.YMax + Gap && db.YMin <= da.YMax + Gap && a.X0 <= b.X1 + 128 && b.X0 <= a.X1 + 128 && a.Z0 <= b.Z1 + 128 && b.Z0 <= a.Z1 + 128;
        }
        var loose = Enumerable.Range(0, all.Count).Where(i => !held[i]).ToList();
        for (var changed = true; changed;)
        {
            changed = false;
            foreach (var i in loose.Where(i => !held[i]).ToList())
                for (var j = 0; j < all.Count; j++)
                    if (held[j] && j != i && Touch(i, j)) { held[i] = changed = true; break; }
        }
        return loose.Where(i => !held[i]).Select(i => (all[i].Cx, all[i].Cz, all[i].D)).ToList();
    }
}
