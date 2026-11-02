using System.IO;
using LBAAssembler.Terrain.Polar;
using LBAAssembler.Terrain.Snow;

namespace LBAAssembler.Terrain.Harvest;

// The Long Table's ground (HarvestDesign's heights) as an LBA2 island file, as the snow island's is made (SnowTerrain): its cubes' settings
// Otringal's (the island it is raced as), the sea in every patch, each cell a tile of its own kind, drawn here from nothing -- the whole island
// is the build's, no LBA1 scenes or retail ground -- in the palette of HarvestPalette; the sea undrawn; the light baked from the cubes' sun.
internal static class HarvestTerrain
{
    public const string LookIsland = "OTRINGAL";
    // how far up the sky stands (the road's table, the mill and the stands all well under it), the light on flat ground (SnowTerrain's)
    private const int SkyHeight = 30000, NormalLight = 12;
    private const double SlopeLight = 0.8;
    // four of each kind, picked by a noise: no two cubes look stamped
    private const int Variants = 4;

    public sealed record Result(IslandFile Island, List<string> Log);

    public static Result Build(string gameDirectory, HarvestDesign design, byte[] palette, string fileName)
    {
        var log = new List<string>();
        var source = IslandFile.Load(Path.Combine(gameDirectory, LookIsland + ".ILE"));
        var cubes = design.Cubes().ToList();
        var island = IslandFile.Parse(IslandScaler.NewFile(source, cubes), fileName);
        var settings = source.Cubes.Values.Where(c => c.HasPolygons).OrderByDescending(c => c.Heights.Count(h => h > 0)).First();
        foreach (var (cx, cz) in cubes)
        {
            var cube = island.CubeAt(cx, cz)!;
            cube.Info = (int[])settings.Info.Clone();
            cube.Info[IslandCube.InfoNbDecors] = 0;
            cube.Info[IslandCube.InfoSkyY] = SkyHeight;
            cube.BitField = 0xFFFF;
            for (var i = 6; i < IslandCube.InfoSize; i++) cube.Info[i] = -1;
            int u0 = (cx - HarvestDesign.CubeX0) * 64, v0 = (cz - HarvestDesign.CubeZ0) * 64;
            for (var z = 0; z <= 64; z++)
                for (var x = 0; x <= 64; x++)
                    cube.Heights[z * IslandCube.Vertices + x] = (short)Math.Clamp(Math.Round(design.Heights[u0 + x, v0 + z]), -32000, 32000);
        }
        var all = new List<(int Gx, int Gz)>();
        foreach (var (cx, cz) in cubes) for (var z = 0; z < 64; z++) for (var x = 0; x < 64; x++) all.Add((cx * 64 + x, cz * 64 + z));
        IslandGround.OptimiseDiagonals(island, new Cells(all));

        // the tiles on the ground's pages (the last row of the first kept for a road's tiles, as Polar Island's)
        var pages = new PolarTextures.Pages(island.GroundTexture, PolarTextures.Colours.AsTheyAre(palette), IslandFile.MaxGroundPages);
        pages.Reserve(PolarTextures.Pages.SlotsPerRow - 1);
        foreach (var kind in Enum.GetValues<HarvestDesign.Kind>().Where(k => k != HarvestDesign.Kind.Sea))
            for (var n = 0; n < Variants; n++)
                if (!pages.Add((kind, n), HarvestTiles.Tile(kind, n, palette), PolarTextures.TileSize, PolarTextures.TileSize))
                    throw new InvalidOperationException($"The ground's textures take more than {IslandFile.MaxGroundPages} pages.");
        island.GroundPages.Clear();
        island.GroundPages.AddRange(pages.List.Skip(1));

        // the cells: the sea first (undrawn, water), then each drawn one its tile
        var counts = new Dictionary<HarvestDesign.Kind, int>();
        var most = 0;
        foreach (var (gx, gz) in all)
        {
            var cube = island.CubeAt(gx / 64, gz / 64)!;
            for (var half = 0; half < 2; half++)
                cube.SetPolygon(gx % 64, gz % 64, half, new IslandPolygon(cube.Polygon(gx % 64, gz % 64, half)).With(bank: 6, texFlag: 0, polyFlag: 0, sampleStep: 0, codeJeu: 1, textureIndex: 0).Raw);
            int u = gx - HarvestDesign.CubeX0 * 64, v = gz - HarvestDesign.CubeZ0 * 64;
            var top = Math.Max(Math.Max(design.Heights[u, v], design.Heights[u + 1, v]), Math.Max(design.Heights[u, v + 1], design.Heights[u + 1, v + 1]));
            if (top <= 0) continue;
            var kind = design.Kinds[u, v] == HarvestDesign.Kind.Sea ? HarvestDesign.Kind.Shore : design.Kinds[u, v];
            counts[kind] = counts.TryGetValue(kind, out var c) ? c + 1 : 1;
            var (page, tx, ty) = pages.Place[(kind, (int)(Noise.Of(gx, gz, 5) * 1000) % Variants)];
            var diagonal = new IslandPolygon(cube.Polygon(gx % 64, gz % 64, 0)).Diagonal;
            for (var half = 0; half < 2; half++)
            {
                var definition = IslandGround.TileDefinition(tx, ty, PolarTextures.TileSize, PolarTextures.TileSize, diagonal, half);
                var index = IslandGround.TextureIndexFor(cube, definition);
                if (index >= island.MaxGroundDefinitions) throw new InvalidOperationException($"Cube {cube.Id} needs more than {island.MaxGroundDefinitions} texture definitions.");
                most = Math.Max(most, index + 1);
                var p = new IslandPolygon(cube.Polygon(gx % 64, gz % 64, half));
                cube.SetPolygon(gx % 64, gz % 64, half, island.WithGroundTexture(p.With(bank: Land.Bank, texFlag: Land.TexFlag, polyFlag: Land.PolyFlag, sampleStep: Land.SampleStep, codeJeu: 0), page, index).Raw);
            }
        }
        log.Add("the ground: " + string.Join(", ", counts.OrderByDescending(k => k.Value).Select(k => $"{k.Value} {k.Key}")) + $"; {pages.Place.Count} tiles on {pages.Count} page(s), at most {most} texture definitions in a cube");

        // the light: the bake's, moved so flat ground has the palette's own colours (SnowTerrain's)
        var bake = BakeOptions.For(island.CubeAt(cubes[0].X, cubes[0].Z)!);
        bake.TerrainShadows = true;
        var vertices = new List<(int, int)>();
        foreach (var (cx, cz) in cubes) for (var z = 0; z <= 64; z++) for (var x = 0; x <= 64; x++) vertices.Add((cx * 64 + x, cz * 64 + z));
        IslandBake.Bake(island, new Cells(vertices), bake);
        var flat = cubes.SelectMany(c => island.CubeAt(c.X, c.Z)!.Intensity).GroupBy(b => b & 15).OrderByDescending(g => g.Count()).First().Key;
        foreach (var (cx, cz) in cubes)
        {
            var cube = island.CubeAt(cx, cz)!;
            for (var i = 0; i < cube.Intensity.Length; i++)
                cube.Intensity[i] = (byte)((cube.Intensity[i] & 0xF0) | Math.Clamp(NormalLight + (int)Math.Round(((cube.Intensity[i] & 15) - flat) * SlopeLight), 0, 15));
        }
        return new Result(island, log);
    }

    // (a land cell's flags, SnowTerrain's: textured, Gouraud-lit)
    private static readonly IslandPolygon Land = new IslandPolygon(0).With(bank: 1, texFlag: 3, polyFlag: 0, sampleStep: 2);

    private sealed class Cells(IEnumerable<(int Gx, int Gz)> cells) : IslandRegion
    {
        private readonly List<(int Gx, int Gz)> list = cells.ToList();
        public override IEnumerable<(int Gx, int Gz, double Weight)> Vertices(IslandFile island) => list.Select(c => (c.Gx, c.Gz, 1.0));
        public override (double Gx, double Gz) Center => list.Count == 0 ? (0, 0) : (list.Average(c => c.Gx), list.Average(c => c.Gz));
    }
}

// The ground's tiles, 32 x 32, in the island's palette: every one drawn from a noise and a few dabs, each repeating across its edges well
// enough that the grid of cells doesn't show.
internal static class HarvestTiles
{
    private const int Size = PolarTextures.TileSize;

    public static int[] Tile(HarvestDesign.Kind kind, int n, byte[] palette)
    {
        var tile = new (double R, double G, double B)[Size * Size];
        var seed = 40 + (int)kind * 11 + n * 3;
        double N(int x, int y, double scale, int s) => Noise.Fbm(x / scale, y / scale, 2, seed + s);
        for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                var a = N(x, y, 8, 0); var b = N(x, y, 3, 1);
                tile[y * Size + x] = kind switch
                {
                    HarvestDesign.Kind.Maple => Mix((116, 62, 34), (168, 84, 34), a),
                    HarvestDesign.Kind.Grass => Mix((132, 118, 56), (176, 150, 70), a * 0.6 + b * 0.4),
                    HarvestDesign.Kind.Fair => Mix((194, 158, 94), (224, 196, 126), b),
                    HarvestDesign.Kind.Bog => Mix((116, 16, 32), (168, 28, 42), a),
                    HarvestDesign.Kind.Dike => Mix((104, 96, 54), (134, 120, 64), b),
                    HarvestDesign.Kind.Shore => Mix((126, 110, 84), (170, 150, 116), a),
                    HarvestDesign.Kind.Dirt => Mix((96, 66, 42), (128, 92, 58), a),
                    HarvestDesign.Kind.Rug => (x / 8 + y / 8) % 2 == 0 ? Mix((128, 34, 36), (150, 44, 40), b) : Mix((150, 96, 44), (172, 116, 52), b),
                    HarvestDesign.Kind.Border => Mix((168, 162, 150), (204, 198, 186), b),
                    HarvestDesign.Kind.Stubble => y % 6 < 2 ? Mix((88, 78, 62), (108, 94, 72), b) : Mix((118, 104, 78), (150, 136, 108), a * 0.4 + b * 0.6),
                    HarvestDesign.Kind.Corn => y % 8 < 3 ? Mix((96, 70, 44), (118, 88, 54), b) : Mix((188, 156, 74), (216, 186, 92), a),
                    HarvestDesign.Kind.Street => (y % 16 == 0 || (x + (y / 16 % 2) * 8) % 16 == 0) ? (86, 82, 82) : Mix((118, 114, 110), (136, 130, 124), b),
                    HarvestDesign.Kind.Turf => n % 2 == 0 ? Mix((64, 122, 56), (76, 138, 64), b) : Mix((54, 106, 50), (66, 120, 56), b),
                    _ => (y % 10 == 0 || (x + (y / 10) * 13) % 32 == 0) ? (64, 44, 30) : Mix((122, 86, 56), (146, 106, 68), b),
                };
            }
        // the maple's fallen leaves, the bog's floating berries: dabs of colour on the ground's own
        if (kind is HarvestDesign.Kind.Maple or HarvestDesign.Kind.Bog)
        {
            var random = new Random(seed);
            (double, double, double)[] maple = { (214, 62, 32), (232, 110, 30), (245, 170, 40), (196, 40, 40) };
            (double, double, double)[] bog = { (196, 40, 48), (214, 62, 52), (140, 20, 36), (236, 96, 80) };
            var dabs = kind == HarvestDesign.Kind.Maple ? maple : bog;
            for (var d = 0; d < (kind == HarvestDesign.Kind.Maple ? 34 : 60); d++)
            {
                int x = random.Next(Size), y = random.Next(Size); var c = dabs[random.Next(dabs.Length)];
                foreach (var (dx, dy) in new[] { (0, 0), (1, 0), (0, 1), (-1, 0) })
                    tile[(y + dy + Size) % Size * Size + (x + dx + Size) % Size] = c;
            }
        }
        return tile.Select(c => SnowColour.Nearest(palette, c.R, c.G, c.B)).ToArray();
    }

    private static (double R, double G, double B) Mix((double R, double G, double B) a, (double R, double G, double B) b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return (a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
    }
}
