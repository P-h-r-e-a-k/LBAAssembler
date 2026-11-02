using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace LBAAssembler.Terrain.Harvest;

// The Long Table's island as tools/RaceTrackPlan/harvest_design.py draws it (docs/racetrack/harvest_ground.bin, harvest_decor.json): six cubes
// by five from cube (4, 4), the heights at each vertex, what each cell is, and the things that stand on it. The script draws the lap and the
// ground together, the land a little under the road's deck, so the two can't disagree; the build only turns them into the game's files.
internal sealed class HarvestDesign
{
    public const int CubeX0 = 4, CubeZ0 = 4, CubesX = 6, CubesZ = 5;
    public const string GroundResource = "RaceTrackPlan.HarvestGround.bin", DecorResource = "RaceTrackPlan.HarvestDecor.json";
    // (in the order the script's KINDS has them)
    public enum Kind : byte { Sea, Maple, Grass, Fair, Bog, Dike, Shore, Dirt, Rug, Border, Stubble, Corn, Street, Turf, Wharf }
    public sealed record Thing(double X, double Z, double Y, int Turn, double Scale);

    public int Width { get; private init; }
    public int Depth { get; private init; }
    public double[,] Heights { get; private init; } = new double[0, 0];
    public Kind[,] Kinds { get; private init; } = new Kind[0, 0];
    public Dictionary<string, List<Thing>> Things { get; private init; } = new();

    public IEnumerable<(int X, int Z)> Cubes()
    {
        for (var z = 0; z < CubesZ; z++) for (var x = 0; x < CubesX; x++) yield return (CubeX0 + x, CubeZ0 + z);
    }
    public static double WorldX(double u) => (CubeX0 * 64 + u) * IslandFile.CellSize;
    public static double WorldZ(double v) => (CubeZ0 * 64 + v) * IslandFile.CellSize;

    public static HarvestDesign Load()
    {
        var assembly = typeof(HarvestDesign).Assembly;
        using var stream = assembly.GetManifestResourceStream(GroundResource) ?? throw new InvalidDataException("This build of the program has no ground for the Long Table.");
        using var inflated = new ZLibStream(stream, CompressionMode.Decompress);
        using var bytes = new MemoryStream();
        inflated.CopyTo(bytes);
        var b = bytes.ToArray();
        int width = BitConverter.ToInt32(b, 0), depth = BitConverter.ToInt32(b, 4);
        var heights = new double[width + 1, depth + 1];
        var at = 8;
        for (var v = 0; v <= depth; v++) for (var u = 0; u <= width; u++) { heights[u, v] = BitConverter.ToInt16(b, at); at += 2; }
        var kinds = new Kind[width, depth];
        for (var v = 0; v < depth; v++) for (var u = 0; u < width; u++) kinds[u, v] = (Kind)b[at++];
        using var things = assembly.GetManifestResourceStream(DecorResource) ?? throw new InvalidDataException("This build of the program has no things for the Long Table.");
        var read = JsonSerializer.Deserialize<Dictionary<string, double[][]>>(things) ?? new();
        return new HarvestDesign
        {
            Width = width, Depth = depth, Heights = heights, Kinds = kinds,
            Things = read.ToDictionary(k => k.Key, k => k.Value.Select(t => new Thing(t[0], t[1], t[2], (int)t[3], t[4])).ToList()),
        };
    }
}
