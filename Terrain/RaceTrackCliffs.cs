namespace LBAAssembler.Terrain;

// The island's edge where a track's build raised it: closed in rock. A retail island leaves its cliff's steep triangles down to the sea
// undrawn -- see-through -- and stands rock faces (decors) over them, as tall as the cliff was; where the build raised the ground there
// (Citadel Island's town circuit, CITABAU: up to 5,500 higher at the north coast), the cliff rose over its rock faces and the sky and the
// sea showed through the slots between them (the user, 2026-11-26: "where we've remodelled it, there's a lot of transparent gaps around
// the edge of the island that we should clean up"). Each such triangle -- undrawn, under no decor, its cell raised by the build -- drawn
// as its cube's own cliff rock: the triangle most drawn on the cube's steep cells, its texture and lighting kept, the triangle's own
// ground code and wall bit as they were.
internal static class RaceTrackCliffs
{
    // (raised this much over the retail cell's highest corner; a steep cell's side rising this much)
    private const int RaisedBy = 300, Steep = 900;
    private const uint LookMask = 0xFu | (3u << 4) | (3u << 6) | (1u << 18) | (0x1FFFu << 19);

    public static string Close(IslandFile built, IslandFile original)
    {
        int closed = 0, cubes = 0, noRock = 0;
        foreach (var (cx, cz, cube) in IslandOps.CubeCells(built))
        {
            if (!cube.HasPolygons) continue;
            int[] Corners(IslandFile f, int x, int z) => new[] { (0, 0), (1, 0), (0, 1), (1, 1) }.Select(c => (int)(f.HeightAt(cx * 64 + x + c.Item1, cz * 64 + z + c.Item2) ?? 0)).ToArray();
            bool Hidden(IslandPolygon p) => p.TexFlag == 0 && p.PolyFlag == 0;
            // the cube's rock: its commonest drawn look on its steep cells
            var counts = new Dictionary<uint, int>();
            for (var z = 0; z < 64; z++)
                for (var x = 0; x < 64; x++)
                {
                    var h = Corners(built, x, z);
                    if (h.Max() - h.Min() < Steep) continue;
                    for (var half = 0; half < 2; half++)
                    {
                        var p = new IslandPolygon(cube.Polygon(x, z, half));
                        if (Hidden(p) || p.CodeJeu != 0) continue;
                        var look = p.Raw & LookMask;
                        counts[look] = counts.TryGetValue(look, out var n) ? n + 1 : 1;
                    }
                }
            var cellsHere = 0;
            uint? rock = counts.Count > 0 ? counts.MaxBy(k => k.Value).Key : null;
            for (var z = 0; z < 64; z++)
                for (var x = 0; x < 64; x++)
                {
                    var tris = new[] { 0, 1 }.Select(half => new IslandPolygon(cube.Polygon(x, z, half))).ToArray();
                    if (!tris.Any(Hidden)) continue;
                    var h = Corners(built, x, z);
                    if (h.Max() <= 0 || h.Max() - Corners(original, x, z).Max() < RaisedBy) continue;
                    if (cube.Decors.Any(d => d.YMax > d.YMin && x * 512 + 256 >= d.XMin && x * 512 + 256 <= d.XMax && z * 512 + 256 >= d.ZMin && z * 512 + 256 <= d.ZMax)) continue;
                    if (rock is not { } look) { noRock++; continue; }
                    for (var half = 0; half < 2; half++)
                        if (Hidden(tris[half])) { cube.SetPolygon(x, z, half, (tris[half].Raw & ~LookMask) | look); closed++; }
                    cellsHere++;
                }
            if (cellsHere > 0) cubes++;
        }
        return closed == 0 && noRock == 0 ? "" : $"the island's edge: {closed} see-through cliff triangles the build raised drawn in rock, in {cubes} cubes" +
                                                (noRock > 0 ? $" ({noRock} cells left: no rock in their cube to copy)" : "");
    }
}
