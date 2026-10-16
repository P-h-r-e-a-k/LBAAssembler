using LBAAssembler;
using LBAAssembler.Lba1;
using LBAAssembler.Terrain;
using LBAAssembler.Terrain.Polar;
using LBAAssembler.Terrain.Temple;

namespace ScriptRoundTrip;

// Studies for the Temple of Bú's port (Terrain/Temple) -- read only:
//   templecolours <LBA1 folder> <LBA2 game folder>: the colours the temple's bricks draw (pixels, weighted by how often each brick is placed),
//     and how near the retail island palettes' colours come to them
//   templeregion <LBA1 folder> <out.png> [x0 x1 z0 z1]: the temple (or a region of it) as LBA1 draws it
//   templemap <LBA1 folder> <out.png> [scale]: the temple from above -- each column's top layer as a shade, its top brick's code
//   templebuild <LBA1 folder> <LBA2 game folder>: builds the island into the game folder (TempleIsland.Add) -- a sandbox's, never the game's
internal static class TempleStudy
{
    // templebricks <LBA1 folder> [n]: the temple's most placed bricks -- how often, their box, how much of each face they draw
    public static int Bricks(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = TempleLayout.Build(game);
        var n = args.Length > 2 ? int.Parse(args[2]) : 30;
        foreach (var g in layout.Cells.Values.GroupBy(c => c.Brick).OrderByDescending(g => g.Count()).Take(n))
        {
            var s = PolarTextures.Sprite.Decode(game.ReadBrick(g.Key));
            var box = s.Footprint();
            var fit = PolarTextures.Sprite.LastFit;
            Console.WriteLine($"brick {g.Key,4} x{g.Count(),5} shape {g.First().Shape} code {g.First().Code:X2} sprite {s.Width}x{s.Lines} hot ({s.HotX},{s.HotY}) box {box.U0:0.00}-{box.U1:0.00} x {box.V0:0.00}-{box.V1:0.00} " +
                $"(full {fit.Full:0.00} best {fit.Best:0.00}) drawn top {s.Coverage(PolarTextures.Face.Top):0.00} +x {s.Coverage(PolarTextures.Face.SideX):0.00} +z {s.Coverage(PolarTextures.Face.SideZ):0.00}");
        }
        return 0;
    }

    public static int BuildInto(string[] args)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var line in TempleIsland.Add(args[1], args[2])) Console.WriteLine(line);
        Console.WriteLine($"built in {watch.Elapsed.TotalSeconds:0.0} s");
        return 0;
    }

    public static int Colours(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = TempleLayout.Build(game);
        foreach (var l in layout.Log) Console.WriteLine(l);
        var placed = layout.Cells.Values.GroupBy(c => c.Brick).ToDictionary(g => g.Key, g => g.Count());
        var counts = new long[256];
        foreach (var (brick, n) in placed)
            foreach (var px in PolarTextures.Sprite.Decode(game.ReadBrick(brick)).Pixels)
                if (px >= 0) counts[px] += n;
        var used = Enumerable.Range(0, 256).Where(c => counts[c] > 0).OrderByDescending(c => counts[c]).ToList();
        var total = counts.Sum();
        Console.WriteLine($"{placed.Count} bricks, {used.Count} colours, {total} pixels");
        Console.WriteLine("ramps used (16 each): " + string.Join(" ", Enumerable.Range(0, 16).Where(r => used.Any(c => c / 16 == r))
            .Select(r => $"{r * 16}:{used.Count(c => c / 16 == r)}/{100.0 * Enumerable.Range(r * 16, 16).Sum(c => counts[c]) / total:0.0}%")));
        var cumulative = 0L; var k = 0;
        foreach (var c in used)
        {
            cumulative += counts[c]; k++;
            if (k is 16 or 32 or 48 or 64 or 96 or 128) Console.WriteLine($"  top {k} colours: {100.0 * cumulative / total:0.00}% of pixels");
        }
        foreach (var entry in new[] { 27, 30, 31, 35, 42 })
        {
            var pal = IslandMapRenderer.LoadPaletteEntry(args[2], entry);
            double sum = 0, worst = 0; var bad = 0L;
            foreach (var c in used)
            {
                var (r, g, b) = PolarTextures.Colours.Rgb(game.Palette, c);
                var best = double.MaxValue;
                for (var p = 16; p <= 239; p++)
                {
                    var (pr, pg, pb) = PolarTextures.Colours.Rgb(pal, p);
                    best = Math.Min(best, Math.Sqrt((r - pr) * (r - pr) + (g - pg) * (g - pg) + (b - pb) * (b - pb)));
                }
                sum += best * counts[c]; worst = Math.Max(worst, best);
                if (best > 12) bad += counts[c];
            }
            Console.WriteLine($"palette {entry}: mean distance {sum / total:0.0}, worst {worst:0.0}, {100.0 * bad / total:0.0}% of pixels further than 12");
        }
        return 0;
    }

    public static int Region(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = TempleLayout.Build(game);
        var cells = layout.Cells.AsEnumerable();
        if (args.Length >= 7)
        {
            int x0 = int.Parse(args[3]), x1 = int.Parse(args[4]), z0 = int.Parse(args[5]), z1 = int.Parse(args[6]);
            cells = cells.Where(c => c.Key.X >= x0 && c.Key.X <= x1 && c.Key.Z >= z0 && c.Key.Z <= z1);
        }
        var list = cells.Select(c => new Lba1Placement(c.Key.X, c.Key.Y, c.Key.Z, c.Value.Brick)).ToList();
        var image = Lba1GridRenderer.Render(new[] { new Lba1Tile(list, 0, 0, 0) }, game.ReadBrick, game.Palette);
        PngWriter.Write(args[2], image.Bgra, image.Width, image.Height);
        Console.WriteLine($"{args[2]}: {list.Count} cells, {image.Width} x {image.Height}");
        return 0;
    }

    public static int Map(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = TempleLayout.Build(game);
        var k = args.Length > 3 ? int.Parse(args[3]) : 4;
        int w = (layout.MaxX - layout.MinX + 1) * k, h = (layout.MaxZ - layout.MinZ + 1) * k;
        var px = new byte[w * h * 4];
        var tops = new Dictionary<(int, int), (int Y, PolarLayout.Cell Cell)>();
        foreach (var ((x, y, z), c) in layout.Cells)
            if (!tops.TryGetValue((x, z), out var t) || y > t.Y) tops[(x, z)] = (y, c);
        foreach (var ((x, z), (y, cell)) in tops)
        {
            // (a heat map of the top layer: blue low, through green, to red high; the brick's own colour with HEAT=0)
            var t = (y - layout.MinY) / (double)Math.Max(1, layout.MaxY - layout.MinY);
            var (r, g, b) = Environment.GetEnvironmentVariable("HEAT") == "0" ? PolarLayout.BrickColour(game, cell.Brick)
                : (255 * Math.Clamp(2 * t - 0.6, 0, 1), 255 * (1 - Math.Abs(2 * t - 1) * 0.9), 255 * Math.Clamp(1 - 2 * t, 0, 1));
            var shade = 1.0;
            for (var dz = 0; dz < k; dz++)
                for (var dx = 0; dx < k; dx++)
                {
                    var i = (((z - layout.MinZ) * k + dz) * w + (x - layout.MinX) * k + dx) * 4;
                    var edge = dx == 0 || dz == 0;
                    px[i] = (byte)Math.Min(255, b * shade * (edge ? 0.8 : 1)); px[i + 1] = (byte)Math.Min(255, g * shade * (edge ? 0.8 : 1));
                    px[i + 2] = (byte)Math.Min(255, r * shade * (edge ? 0.8 : 1)); px[i + 3] = 255;
                }
        }
        PngWriter.Write(args[2], px, w, h);
        Console.WriteLine($"{args[2]}: {w} x {h} (x {layout.MinX}.. across, z {layout.MinZ}.. down, {k} px a cell)");
        return 0;
    }
}
