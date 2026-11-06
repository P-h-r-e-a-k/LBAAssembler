using LBAAssembler;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;

namespace ScriptRoundTrip;

// museumblock <game> <scene> <block> <out.png> [scale]: one block of a scene's grid library alone, drawn big, its entries listed
internal static class MuseumStudy
{
    public static int Block(string[] args)
    {
        var b = new Lba2GridBackend(args[1]);
        if (args[2] == "install")
        {
            foreach (var line in LBAAssembler.Terrain.Citadel.CitadelMuseum.Install(args[1])) Console.WriteLine(line);
            return 0;
        }
        if (args[2] == "enc")
        {
            // enc <brick>...: each brick's bytes against its picture encoded again (CitadelMuseum.EncodeBrick), and its lines' runs
            foreach (var n in args.Skip(3).Select(int.Parse))
            {
                var data = b.Brick(n)!;
                var again = LBAAssembler.Terrain.Citadel.CitadelMuseum.EncodeBrick(LBAAssembler.Assets.GphImage.Decode(data));
                Console.WriteLine($"brick {n}: {data[0]}x{data[1]} hot {(sbyte)data[2]},{(sbyte)data[3]}, {data.Length} bytes; again {again.Length} bytes, same {data.AsSpan().SequenceEqual(again)}");
                var at = 4;
                var runs = new List<int>();
                for (var line = 0; line < data[1]; line++)
                {
                    int r = data[at++]; runs.Add(r);
                    for (var k = 0; k < r; k++) { var c = data[at++]; var cnt = (c & 0x3F) + 1; at += (c >> 6) switch { 0 => 0, 1 => cnt, _ => 1 }; }
                }
                Console.WriteLine($"  runs a line: {string.Join(",", runs)}");
            }
            return 0;
        }
        if (args[2] == "pal")
        {
            for (var r = 0; r < 16; r++)
                Console.WriteLine($"ramp {r,2}: " + string.Join(" ", Enumerable.Range(0, 16).Select(s => { var c = (r * 16 + s) * 3; return $"{b.Palette[c]},{b.Palette[c + 1]},{b.Palette[c + 2]}"; })));
            return 0;
        }
        var g = b.GridOfScene(int.Parse(args[2]))!.Value;
        var library = b.LoadLibrary(g);
        var block = int.Parse(args[3]);
        var scale = args.Length > 5 ? int.Parse(args[5]) : 4;
        Console.WriteLine($"library {b.RawGrid(g)[0]}: {GridPaint.BlockCount(library)} blocks");
        var info = GridPaint.Info(library, block)!.Value;
        Console.WriteLine($"block {block}: {info.Dx} x {info.Dy} x {info.Dz}");
        foreach (var e in GridPaint.Entries(library, block)) Console.WriteLine($"  pos {e.Pos} shape {e.Shape} sound {e.Sound:x2} brick {e.Brick}");
        var grid = Lba1GridEdit.SetCells(b.LoadGrid(g), Enumerable.Range(0, 64 * 64 * 25).Select(i => new Lba1GridCell(i / 25 % 64, i % 25, i / 25 / 64, 0, 0)).ToList());
        grid = GridPaint.Place(grid, library, block, 10, 0, 10);
        var image = Lba1GridRenderer.Render(grid, library, b.Brick, b.Palette);
        var big = new byte[image.Width * scale * image.Height * scale * 4];
        for (var y = 0; y < image.Height * scale; y++)
            for (var x = 0; x < image.Width * scale; x++)
            {
                var s = ((y / scale) * image.Width + x / scale) * 4;
                var d = (y * image.Width * scale + x) * 4;
                if (image.Bgra[s + 3] == 0) { big[d] = big[d + 1] = big[d + 2] = (byte)((x / 8 + y / 8) % 2 == 0 ? 60 : 80); big[d + 3] = 255; continue; }
                Array.Copy(image.Bgra, s, big, d, 4);
            }
        PngWriter.Write(args[4], big, image.Width * scale, image.Height * scale);
        Console.WriteLine($"picture {image.Width} x {image.Height}, origin {image.OriginX},{image.OriginY}");
        // (map=x0,y0,x1,y1: each pixel's palette ramp there, and the colours counted)
        if (args.Skip(6).FirstOrDefault(a => a.StartsWith("map=")) is { } m)
        {
            var v = m[4..].Split(',').Select(int.Parse).ToArray();
            var pal = b.Palette;
            var counts = new SortedDictionary<int, int>();
            for (var y = v[1]; y <= v[3]; y++)
            {
                var line = new System.Text.StringBuilder($"{y,4} ");
                for (var x = v[0]; x <= v[2]; x++)
                {
                    var s = (y * image.Width + x) * 4;
                    if (image.Bgra[s + 3] == 0) { line.Append(". "); continue; }
                    var best = 0; var bd = int.MaxValue;
                    for (var c = 0; c < 256; c++)
                    {
                        int dr = pal[c * 3] - image.Bgra[s + 2], dg = pal[c * 3 + 1] - image.Bgra[s + 1], db = pal[c * 3 + 2] - image.Bgra[s];
                        var d = dr * dr + dg * dg + db * db; if (d < bd) { bd = d; best = c; }
                    }
                    counts[best] = counts.GetValueOrDefault(best) + 1;
                    line.Append($"{best >> 4:x}{best & 15:x}"[0]).Append($"{best & 15:x}");
                }
                Console.WriteLine(line);
            }
            foreach (var (c, n) in counts) Console.WriteLine($"  {c} (ramp {c >> 4} shade {c & 15}) rgb {pal[c * 3]},{pal[c * 3 + 1]},{pal[c * 3 + 2]}: {n}");
        }
        return 0;
    }
}
