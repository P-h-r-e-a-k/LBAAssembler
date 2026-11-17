using System.Buffers.Binary;
using System.IO;

namespace LBAAssembler.Terrain;

// The Temple of Bú's doorway on Desert Island made big enough for a car (the user, 2026-11-17, a picture of it -- two leaning stone slabs
// either side of a dark doorway, a small red emblem over it: "Can we remodel the entrance to the temple of bu so it's big enough to accept a
// car, and does so"). The slabs are the ground's: two ridges of its height points 1870 high, island rows z 664 and 667 (x 631-632), the
// doorway the two-cell notch between them (z 665-666, the floor at 0) running into the cliff to x 633; over it the lintel with the emblem,
// decor body 79 (DESERT.OBL: y 1360-2200, its width along its x -- the decor turned so that is the world's z -- the doorway's middle at its
// x 0). A car needs three cells. So:
//   - the ridges moved out a cell each way (rows 663 and 668) and raised with the lintel (Ridge), the notch between them (rows 664-667)
//     floored at 0 and a cell deeper into the cliff (to x 634), its triangles no longer blocking, the ridges' slopes blocking;
//   - a way down to it from the road: the stone wall's last section taken out, the ledge under it graded (WallGap);
//   - the lintel a copy of body 79 Wide times as wide and High times as tall (a new body on the end of the OBL), its box with it.
// RaceTrackStory.Licence's drive then goes on off the road, down the slope and into the doorway (DoorwayMarks).
internal static class TempleDoorway
{
    public const int LintelBody = 79;
    public const double Wide = 5.0 / 3, High = 1.2;
    // island cells: the lintel decor's place, the floor's rows and its columns, the ridges' rows and columns
    private const double LintelX = 632.6, LintelZ = 665.4;
    private const int FloorZ0 = 664, FloorZ1 = 667, FloorX0 = 627, FloorX1 = 634, RidgeZA = 663, RidgeZB = 668, RidgeX0 = 631, RidgeX1 = 634;
    private const int Ridge = 2250;

    // The pit before the doorway is walled in from the road: the stone wall with its rail (body 78 sections, island row z 661.7) from x
    // 598.7 to 629.2, a fence down its west side (x 617.5), the dunes south. Its last section, nearest the doorway (WallGap: x 625.4-629.2),
    // taken out for the car, and the ledge under it (rows 660-663, x 625-629: a hump 700 high, then a drop to the pit) graded into a slope.
    private const int WallBody = 78;
    private const double WallGapX = 627.3, WallGapZ = 661.7;
    private const int GapX0 = 625, GapX1 = 629, GapZ0 = 660, GapZ1 = 663;
    private static readonly int[] GapSlope = { 400, 300, 200, 100 };
    // the drive's points off the road (cube-local units of scene 67, cube (9, 10)): south through the wall's gap, round to the doorway's
    // axis and in under the lintel (the car stops about a cell short of its last point) -- the door into the temple (scene 10) switched
    // off meanwhile, the car and Twinsen getting out of it in its zone (RaceTrackStory.Licence, TempleDoorSwitch)
    public static readonly (int X, int Z)[] DoorwayMarks = { (26266, 8448), (26266, 12032), (27392, 13056), (29440, 13056) };

    public static void Widen(IslandFile island, string oblPath, RaceTrackOptions options, RaceTrackReport report)
    {
        // the lintel
        (IslandDecor Decor, int CubeX, int CubeZ)? found = null;
        var best = 9.0;
        foreach (var (cx, cz, cube) in IslandOps.CubeCells(island))
            foreach (var d in cube.Decors)
            {
                if ((d.Body & 0xFFFF) != LintelBody) continue;
                double dx = cx * 64 + d.X / 512.0 - LintelX, dz = cz * 64 + d.Z / 512.0 - LintelZ, d2 = dx * dx + dz * dz;
                if (d2 < best) { best = d2; found = (d, cx, cz); }
            }
        if (found is not { } lintel) { report.Notes.Add($"WARNING: the Temple of Bú's doorway left as it was: no lintel (body {LintelBody}) by cell ({LintelX}, {LintelZ})"); return; }
        if (options.NewBodyBase < 0) { report.Notes.Add("WARNING: the Temple of Bú's doorway left as it was: no place for new bodies"); return; }
        var body = HqrArchive.Open(oblPath).Read(LintelBody);
        var index = options.NewBodyBase + report.NewBodies.Count;
        report.NewBodies.Add(Scaled(body, Wide, High, 1));
        var d0 = lintel.Decor;
        d0.Body = (d0.Body & ~0xFFFF) | index;
        // (its box along the world's z -- the body's x, turned -- and up, round its place)
        (d0.ZMin, d0.ZMax) = (d0.Z + (int)Math.Round((d0.ZMin - d0.Z) * Wide), d0.Z + (int)Math.Round((d0.ZMax - d0.Z) * Wide));
        (d0.YMin, d0.YMax) = (d0.Y + (int)Math.Round((d0.YMin - d0.Y) * High), d0.Y + (int)Math.Round((d0.YMax - d0.Y) * High));

        // the ground: the floor, the ridges, the slope down to it
        var set = 0;
        void Height(int x, int z, int h) { if (island.HasVertex(x, z)) { island.SetHeight(x, z, h); set++; } }
        int Now(int x, int z) => island.HeightAt(x, z) ?? 0;
        for (var z = FloorZ0; z <= FloorZ1; z++)
            for (var x = FloorX0; x <= FloorX1; x++) Height(x, z, 0);
        foreach (var z in new[] { RidgeZA, RidgeZB })
            for (var x = RidgeX0; x <= RidgeX1; x++) Height(x, z, Math.Max(Ridge, x <= RidgeX0 + 1 ? 0 : Now(x, z)));
        // the wall's gap and the slope down through it
        var wallGone = false;
        foreach (var (cx, cz, cube) in IslandOps.CubeCells(island))
        {
            var wall = cube.Decors.FirstOrDefault(d => (d.Body & 0xFFFF) == WallBody && Math.Abs(cx * 64 + d.X / 512.0 - WallGapX) < 0.5 && Math.Abs(cz * 64 + d.Z / 512.0 - WallGapZ) < 0.5);
            if (wall is not null) { cube.Decors.Remove(wall); wallGone = true; }
        }
        for (var z = GapZ0; z <= GapZ1; z++)
            for (var x = GapX0; x <= GapX1; x++) Height(x, z, GapSlope[z - GapZ0]);
        if (!wallGone) report.Notes.Add($"WARNING: the Temple of Bú's doorway: no stone wall section (body {WallBody}) by cell ({WallGapX}, {WallGapZ}) to take out -- the car can't get down to it");
        // the triangles: the floor's not blocking, the ridges' slopes blocking
        var cleared = 0;
        void Col(int x, int z, bool col)
        {
            var cube = island.CubeAt(x / IslandCube.Cells, z / IslandCube.Cells);
            if (cube is null) return;
            for (var h = 0; h < 2; h++)
            {
                var p = new IslandPolygon(cube.Polygon(x % IslandCube.Cells, z % IslandCube.Cells, h));
                if (p.Col == col) continue;
                cube.SetPolygon(x % IslandCube.Cells, z % IslandCube.Cells, h, p.With(col: col).Raw);
                cleared++;
            }
        }
        for (var x = FloorX0; x < FloorX1; x++)
            for (var z = FloorZ0; z < FloorZ1; z++) Col(x, z, false);
        for (var x = GapX0; x < GapX1; x++)
            for (var z = GapZ0; z < GapZ1 + 1; z++) Col(x, z, false);
        for (var x = RidgeX0 - 1; x < FloorX1; x++) { Col(x, RidgeZA, true); Col(x, FloorZ1, true); }
        report.Notes.Add($"the Temple of Bú's doorway widened for a car: the lintel body {LintelBody} as body {index} ({Wide:0.##} wide, {High:0.##} tall), the ground's " +
                         $"slabs moved out to rows {RidgeZA} and {RidgeZB} ({Ridge} high), the floor between at 0 to x {FloorX1}, the wall's last section out and its ledge a slope ({set} points, {cleared} triangles' blocking changed)");
    }

    // A body scaled kx, ky, kz along its own axes: its box (header 8-31: x, y, z min and max) and its points (count at 40, offset at 44)
    private static byte[] Scaled(byte[] body, double kx, double ky, double kz)
    {
        var b = (byte[])body.Clone();
        int I(int at) => BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(at));
        var k = new[] { kx, kx, ky, ky, kz, kz };
        for (var j = 0; j < 6; j++) BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(8 + 4 * j), (int)Math.Round(I(8 + 4 * j) * k[j]));
        int points = I(40), p = I(44);
        if (p < 0 || p + points * 8 > b.Length) return body;
        for (var i = 0; i < points; i++, p += 8)
            for (var a = 0; a < 3; a++)
            {
                var v = Math.Round(BinaryPrimitives.ReadInt16LittleEndian(b.AsSpan(p + 2 * a)) * (a == 0 ? kx : a == 1 ? ky : kz));
                BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(p + 2 * a), (short)Math.Clamp(v, short.MinValue, short.MaxValue));
            }
        return b;
    }
}
