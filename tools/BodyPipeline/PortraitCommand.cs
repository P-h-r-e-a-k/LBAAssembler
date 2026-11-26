using System.Drawing;
using System.Drawing.Imaging;
using LbaBodyStudio;
using LBAAssembler;

namespace BodyPipeline;

// portrait <game folder> <BODY.HQR entry> <out.png> [size] [yaw degrees] [top fraction]: a body drawn lit in the game's palette by Body
// Studio's renderer, from in front (yaw: turned about the upright, 0 its front), and only its top part kept -- [top fraction] of its
// height from the top (0.32: head and shoulders) -- squared about the head, the background made see-through. For LBRA's app icon
// (the user, 2026-11-25: "generate new app icon. Twinsen's head in his racing overalls, but no helmet").
internal static class PortraitCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 4) { Console.Error.WriteLine("portrait <game folder> <BODY.HQR entry> <out.png> [size] [yaw] [top fraction]"); return 2; }
        var folder = args[1];
        var entry = int.Parse(args[2]);
        var size = args.Length > 4 ? int.Parse(args[4]) : 512;
        var yaw = (args.Length > 5 ? float.Parse(args[5], System.Globalization.CultureInfo.InvariantCulture) : 0) * MathF.PI / 180;
        var top = args.Length > 6 ? float.Parse(args[6], System.Globalization.CultureInfo.InvariantCulture) : 0.32f;
        var palette = Generator.Palette(folder);
        var body = Body.Read(new Hqr(Path.Combine(folder, "BODY.HQR")).Read(entry), 2);
        // (drawn big, the whole body, on a key colour no palette entry has; then the top part cut out of it)
        var key = Color.FromArgb(255, 0, 255);
        var big = size * 4;
        using var whole = Renderer.Render(body, palette, big, big, yaw, wire: false, background: key, gridLine: key);
        // the body's extent on the picture
        int x0 = big, x1 = -1, y0 = big, y1 = -1;
        for (var y = 0; y < big; y++)
            for (var x = 0; x < big; x++)
                if (whole.GetPixel(x, y).ToArgb() != key.ToArgb()) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
        if (x1 < 0) { Console.Error.WriteLine("nothing drawn"); return 1; }
        var h = (int)((y1 - y0) * top);
        // (square, centred on the drawn part's middle across within the kept rows)
        int cx0 = big, cx1 = -1;
        for (var y = y0; y <= y0 + h; y++)
            for (var x = 0; x < big; x++)
                if (whole.GetPixel(x, y).ToArgb() != key.ToArgb()) { cx0 = Math.Min(cx0, x); cx1 = Math.Max(cx1, x); }
        var side = Math.Max(h, cx1 - cx0 + 1);
        var left = (cx0 + cx1) / 2 - side / 2;
        using var cut = new Bitmap(side, side, PixelFormat.Format32bppArgb);
        for (var y = 0; y < side; y++)
            for (var x = 0; x < side; x++)
            {
                int sx = left + x, sy = y0 + y;
                var c = sx >= 0 && sx < big && sy >= 0 && sy < big ? whole.GetPixel(sx, sy) : key;
                cut.SetPixel(x, y, c.ToArgb() == key.ToArgb() ? Color.Transparent : c);
            }
        using var outBmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(outBmp))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.Clear(Color.Transparent);
            g.DrawImage(cut, 0, 0, size, size);
        }
        outBmp.Save(args[3], ImageFormat.Png);
        Console.WriteLine($"portrait: BODY.HQR {entry}, {body.Vertices.Count} points, top {top:0.00} of its height, {side} px square scaled to {size} -> {args[3]}");
        return 0;
    }
}
