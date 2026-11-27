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

    // The faces the build made near-vertical -- the ground raised to the raised road's level beside the town, cut down under the north's
    // hills -- drawn as the cube's own cliff rock, the way the retail island draws its steep ground, not as its earth, grass or paving
    // stretched up them (the user, 2026-11-26, Citadel Island: "Paths that can't be used as they're too steep ... Massively different
    // terrain heights": a paved path ran up a wall west of the town, brown earth smeared down the cut faces). Each drawn triangle steeper than
    // FaceSteep that the retail island had gentler than FaceWas (or had as the sea): its look the triangles' commonest on the cube's cliffs
    // the build left as they were; its ground code, wall bit and cut its own. Under a decor it is left (a building's footing).
    private const double FaceSteep = 60, FaceWas = 45;
    private const double FaceTexturedShare = 0.25;
    private const int FaceNearTown = 4;
    private const uint FaceLook = LookMask | (0xFu << 8);
    private static readonly int[][] Halves = { new[] { 0, 1, 2 }, new[] { 2, 3, 0 }, new[] { 3, 0, 1 }, new[] { 1, 2, 3 } };

    public static string Faces(IslandFile built, IslandFile original)
    {
        int drawn = 0, cubes = 0, noRock = 0;
        double steepCos = Math.Cos(FaceSteep * Math.PI / 180), wasCos = Math.Cos(FaceWas * Math.PI / 180);
        // (a triangle's slope: the cosine of its normal from the vertical, the cell's corners 0 (x,z) 1 (x,z+1) 2 (x+1,z+1) 3 (x+1,z) --
        // IslandGround.SetSteepCollision's)
        static double? Cos(IslandFile f, int gx, int gz, bool diagonal, int half)
        {
            if (f.HeightAt(gx, gz) is not { } y0 || f.HeightAt(gx, gz + 1) is not { } y1 || f.HeightAt(gx + 1, gz + 1) is not { } y2 || f.HeightAt(gx + 1, gz) is not { } y3) return null;
            const double s = IslandFile.CellSize;
            var c = new (double X, double Y, double Z)[] { (0, y0, 0), (0, y1, s), (s, y2, s), (s, y3, 0) };
            var t = Halves[(diagonal ? 2 : 0) + half];
            var (ax, ay, az) = c[t[0]]; var (bx, by, bz) = c[t[1]]; var (qx, qy, qz) = c[t[2]];
            double ux = bx - ax, uy = by - ay, uz = bz - az, vx = qx - ax, vy = qy - ay, vz = qz - az;
            double nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
            var len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            return len < 1e-6 ? 1 : Math.Abs(ny) / len;
        }
        static bool Hidden(IslandPolygon p) => p.TexFlag == 0 && p.PolyFlag == 0;
        // the island's rock: the looks on the cliffs the build left as they were, where each is -- every cube's first, a face near a cube's
        // edge may take after the next cube's
        var counts = new Dictionary<IslandCube, Dictionary<uint, int>>();
        var cliffs = new List<(int Gx, int Gz, int Half, IslandPolygon P, IslandCube From)>();
        foreach (var (cx, cz, cube) in IslandOps.CubeCells(built))
        {
            if (!cube.HasPolygons) continue;
            var here = counts[cube] = new Dictionary<uint, int>();
            if (original.CubeAt(cx, cz) is not { HasPolygons: true } rcube) continue;
            for (var z = 0; z < 64; z++)
                for (var x = 0; x < 64; x++)
                    for (var half = 0; half < 2; half++)
                    {
                        var p = new IslandPolygon(cube.Polygon(x, z, half));
                        if (Hidden(p) || p.CodeJeu != 0 || p.Raw != rcube.Polygon(x, z, half)) continue;
                        int gx = cx * 64 + x, gz = cz * 64 + z;
                        var diagonal = new IslandPolygon(cube.Polygon(x, z, 0)).Diagonal;
                        if (Cos(built, gx, gz, diagonal, half) is not { } a || a >= steepCos || Cos(original, gx, gz, diagonal, half) is not { } b || b >= steepCos) continue;
                        var look = p.Raw & FaceLook;
                        here[look] = here.TryGetValue(look, out var n) ? n + 1 : 1;
                        if (p.TexFlag != 0) cliffs.Add((gx, gz, half, p, cube));
                    }
        }
        foreach (var (cx, cz, cube) in IslandOps.CubeCells(built))
        {
            if (!cube.HasPolygons) continue;
            var rcube = original.CubeAt(cx, cz);
            // (each face the look of the nearest textured cliff triangle of its kind, its piece of texture brought into the face's cube from
            // the next: the retail's rock is a different piece of its texture cell by cell, and one piece repeated came out a plain grey sheet
            // beside the coast's rock. In the town, whose cliffs are its grey retaining walls -- textured ones too few among them -- the
            // commonest look, the walls', unless a textured cliff is within FaceNearTown cells)
            var mine = counts[cube];
            uint? rock = mine.Count > 0 ? mine.MaxBy(k => k.Value).Key : null;
            var rocky = cliffs.Count(c => c.From == cube) >= FaceTexturedShare * mine.Values.Sum() && mine.Count > 0;
            uint? Nearest(int gx, int gz, int half)
            {
                (int Gx, int Gz, int Half, IslandPolygon P, IslandCube From)? best = null; var bestD = rocky ? int.MaxValue : FaceNearTown * FaceNearTown + 1;
                foreach (var c in cliffs)
                {
                    if (c.Half != half) continue;
                    var d = (c.Gx - gx) * (c.Gx - gx) + (c.Gz - gz) * (c.Gz - gz);
                    if (d < bestD) { bestD = d; best = c; }
                }
                if (best is not { } b) return rock;
                if (b.From == cube) return b.P.Raw & FaceLook;
                var (page, definition) = built.GroundTextureOf(b.P);
                if (definition * 6 + 6 > b.From.TextureDefs.Length) return rock;
                return built.WithGroundTexture(b.P, page, IslandGround.TextureIndexFor(cube, b.From.TextureDefs[(definition * 6)..(definition * 6 + 6)])).Raw & FaceLook;
            }
            var drawnHere = 0;
            for (var z = 0; z < 64; z++)
                for (var x = 0; x < 64; x++)
                {
                    int gx = cx * 64 + x, gz = cz * 64 + z;
                    if (cube.Decors.Any(d => d.YMax > d.YMin && x * 512 + 256 >= d.XMin && x * 512 + 256 <= d.XMax && z * 512 + 256 >= d.ZMin && z * 512 + 256 <= d.ZMax)) continue;
                    var diagonal = new IslandPolygon(cube.Polygon(x, z, 0)).Diagonal;
                    var rdiagonal = rcube is { HasPolygons: true } && new IslandPolygon(rcube.Polygon(x, z, 0)).Diagonal;
                    for (var half = 0; half < 2; half++)
                    {
                        var p = new IslandPolygon(cube.Polygon(x, z, half));
                        if (Hidden(p) || (p.CodeJeu != 0 && p.CodeJeu != RaceTrackBuilder.CarWall)) continue;
                        if (Cos(built, gx, gz, diagonal, half) is not { } now || now >= steepCos) continue;
                        // (the retail's slope here: either of its triangles over the same ground -- its cut may have been the other way)
                        var was = Math.Min(Cos(original, gx, gz, rdiagonal, 0) ?? 1, Cos(original, gx, gz, rdiagonal, 1) ?? 1);
                        if (was < wasCos) continue;
                        if (Nearest(gx, gz, half) is not { } look) { noRock++; continue; }
                        if ((p.Raw & FaceLook) == look) continue;
                        cube.SetPolygon(x, z, half, (p.Raw & ~FaceLook) | look);
                        drawn++; drawnHere++;
                    }
                }
            if (drawnHere > 0) cubes++;
        }
        return drawn == 0 && noRock == 0 ? "" : $"the build's near-vertical faces: {drawn} triangles steeper than {FaceSteep} degrees (the retail's under {FaceWas} there) drawn as their cube's cliff rock, in {cubes} cubes" +
                                                (noRock > 0 ? $" ({noRock} left: no cliff in their cube to copy)" : "");
    }
}
