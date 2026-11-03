using System.Buffers.Binary;
using LBAAssembler;
using LBAAssembler.Assets;
using LBAAssembler.Scenes;

namespace ScriptRoundTrip;

// spritesurvey <lba2 folder> <out.bmp> [sprite...]: every LBA2 scene's sprite actors by sprite number (how many, the first scene and
// actor), and a sheet of those sprites (or the given ones) drawn through RESS.HQR's palette, each over its number's row of the list.
internal static class SpriteSurvey
{
    public static int Run(string[] args)
    {
        var dir = args[1];
        var store = new SceneStore(SceneGame.Lba2, dir);
        var uses = new SortedDictionary<int, List<string>>();
        for (var scene = 0; scene < store.SceneCount; scene++)
        {
            SceneModel model;
            try { model = store.Load(scene); } catch { continue; }
            for (var n = 0; n < model.Actors.Count; n++)
                if (model.Actors[n].IsSprite)
                {
                    if (!uses.TryGetValue(model.Actors[n].Sprite, out var list)) uses[model.Actors[n].Sprite] = list = new();
                    list.Add($"{scene}:{n}");
                }
        }
        foreach (var (sprite, list) in uses) Console.WriteLine($"sprite {sprite}: {list.Count} actor(s), {string.Join(" ", list.Take(6))}");

        var wanted = args.Length > 3 ? args.Skip(3).Select(int.Parse).ToList() : uses.Keys.ToList();
        var sprites = GphLibrary.Lba2Sprites(dir);
        var palette = HqrFile.Parse(File.ReadAllBytes(Path.Combine(dir, "RESS.HQR"))).Read(0);
        const int Cell = 96, Columns = 12;
        int rows = (wanted.Count + Columns - 1) / Columns, width = Cell * Columns, height = Cell * rows;
        var sheet = new byte[width * height * 4];
        for (var i = 0; i < sheet.Length; i += 4) { sheet[i] = 60; sheet[i + 1] = 60; sheet[i + 2] = 60; sheet[i + 3] = 255; }
        for (var k = 0; k < wanted.Count; k++)
        {
            GphImage image;
            try { image = sprites.Read(wanted[k]); } catch { continue; }
            var bgra = image.ToBgra(palette);
            int ox = k % Columns * Cell, oy = k / Columns * Cell;
            var scale = Math.Max(1, Math.Min(4, (Cell - 14) / Math.Max(image.Width, image.Height)));
            for (var y = 0; y < image.Height * scale && y < Cell - 12; y++)
                for (var x = 0; x < image.Width * scale && x < Cell; x++)
                {
                    var s = (y / scale * image.Width + x / scale) * 4;
                    if (bgra[s + 3] == 0) continue;
                    var d = ((oy + 12 + y) * width + ox + x) * 4;
                    Array.Copy(bgra, s, sheet, d, 4);
                }
            Digits(sheet, width, ox + 2, oy + 2, wanted[k]);
        }
        WriteBmp(args[2], sheet, width, height);
        Console.WriteLine($"{wanted.Count} sprites drawn to {args[2]}");
        return 0;
    }

    // a number in a 3x5 pixel font (white), at (x, y)
    private static void Digits(byte[] sheet, int width, int x, int y, int value)
    {
        string[] font = { "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001", "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111" };
        foreach (var c in value.ToString())
        {
            var glyph = font[c - '0'];
            for (var gy = 0; gy < 5; gy++)
                for (var gx = 0; gx < 3; gx++)
                    if (glyph[gy * 3 + gx] == '1')
                        for (var sy = 0; sy < 2; sy++)
                            for (var sx = 0; sx < 2; sx++)
                            {
                                var d = ((y + gy * 2 + sy) * width + x + gx * 2 + sx) * 4;
                                sheet[d] = sheet[d + 1] = sheet[d + 2] = 255;
                            }
            x += 8;
        }
    }

    private static void WriteBmp(string path, byte[] bgra, int width, int height)
    {
        var bmp = new byte[54 + bgra.Length];
        bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2), bmp.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10), 54);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18), width);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22), -height);
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(26), 1);
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(28), 32);
        bgra.CopyTo(bmp, 54);
        File.WriteAllBytes(path, bmp);
    }
}
