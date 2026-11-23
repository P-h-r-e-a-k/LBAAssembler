using System.Numerics;
using LBAAssembler;
using LBAAssembler.Terrain;
using LbaBodyStudio;

namespace ScriptRoundTrip;

// hotelpreview <game folder> <palette.bin> <out folder> [generic...]: each of the Imperial Hotel's bodies (HotelProps.Bodies) drawn as the
// game's iso camera sees it -- turned by each of four betas -- lit the engine's way (each corner's normal, scaled by its light share, its
// steps up the polygon's colour's ramp, blended across the polygon), into <out>/<generic>.png. A look at a prop before a build.
internal static class HotelPreview
{
    public static int Run(string[] args)
    {
        var header = Body.Read(HqrArchive.Open(Path.Combine(args[1], "BODY.HQR")).Read(30), 2, allowStatic: true).Header;
        var raw = File.ReadAllBytes(args[2]);
        var palette = Enumerable.Range(0, 256).Select(i => (raw[i * 3], raw[i * 3 + 1], raw[i * 3 + 2])).ToArray();
        Directory.CreateDirectory(args[3]);
        var only = args.Skip(4).Select(int.Parse).ToHashSet();
        foreach (var (generic, name, build) in HotelProps.Bodies)
        {
            if (only.Count > 0 && !only.Contains(generic)) continue;
            var body = build(header);
            var path = Path.Combine(args[3], $"{generic}.png");
            Draw(body, palette, path);
            Console.WriteLine($"{generic} {name}: {body.Vertices.Count} points, {body.Faces.Count} polygons, {body.Lines.Count} lines -> {path}");
        }
        return 0;
    }

    private static void Draw(Body body, (byte R, byte G, byte B)[] palette, string path)
    {
        const float Scale = 4 * 24f / 512;
        var world = body.World();
        var normals = body.VertexNormals();
        var light = Vector3.Normalize(new Vector3(-0.3f, 1f, 0.6f));
        var views = new[] { 0, 1024, 2048, 3072 };
        // each view's picture side by side
        var pts = views.Select(beta =>
        {
            var a = beta * MathF.Tau / 4096;
            float c = MathF.Cos(a), s = MathF.Sin(a);
            // (an actor's beta turns its +z towards +x at 1024: x' = x cos + z sin, z' = z cos - x sin)
            return world.Select(v => new Vector3(v.X * c + v.Z * s, v.Y, v.Z * c - v.X * s)).ToArray();
        }).ToArray();
        var rotNormals = views.Select(beta =>
        {
            var a = beta * MathF.Tau / 4096;
            float c = MathF.Cos(a), s = MathF.Sin(a);
            return normals.Select(v => new Vector3(v.X * c + v.Z * s, v.Y, v.Z * c - v.X * s)).ToArray();
        }).ToArray();
        (float X, float Y) Screen(Vector3 v) => (Scale * (v.X - v.Z), Scale * (0.5f * (v.X + v.Z)) - Scale * 1.25f * v.Y);
        float Depth(Vector3 v) => (v.X + v.Z) * 0.615f + v.Y * 0.492f;
        var bounds = pts.Select(p => p.Select(Screen).ToArray()).Select(p => (X0: p.Min(q => q.X), X1: p.Max(q => q.X), Y0: p.Min(q => q.Y), Y1: p.Max(q => q.Y))).ToArray();
        var cellW = (int)bounds.Max(b => b.X1 - b.X0) + 20;
        var height = (int)bounds.Max(b => b.Y1 - b.Y0) + 20;
        var width = cellW * views.Length;
        var px = new byte[width * height * 4];
        for (var i = 0; i < px.Length; i += 4) { px[i] = 90; px[i + 1] = 70; px[i + 2] = 60; px[i + 3] = 255; }
        var depth = Enumerable.Repeat(float.NegativeInfinity, width * height).ToArray();
        var scales = body.LightScale;
        for (var v = 0; v < views.Length; v++)
        {
            var p = pts[v];
            var ox = v * cellW + 10 - bounds[v].X0; var oy = 10 - bounds[v].Y0;
            float Steps(int i) => body.Lit ? Math.Max(0, Vector3.Dot(rotNormals[v][i], light)) * 9.4f * (scales is { } ls && i < ls.Length ? ls[i] : 1) : 0;
            foreach (var f in body.Faces)
            {
                var unlit = f.Material == 0;
                var seeThrough = f.Material == 2;
                for (var t = 1; t + 1 < f.Points.Length; t++)
                {
                    int ia = f.Points[0], ib = f.Points[t], ic = f.Points[t + 1];
                    var (ax, ay) = Screen(p[ia]); var (bx, by) = Screen(p[ib]); var (cx, cy) = Screen(p[ic]);
                    ax += ox; bx += ox; cx += ox; ay += oy; by += oy; cy += oy;
                    var area = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
                    // (the engine draws a polygon only from its front: here the points go round clockwise on the screen)
                    if (area >= 0) continue;
                    float la = unlit ? 0 : Steps(ia), lb = unlit ? 0 : Steps(ib), lc = unlit ? 0 : Steps(ic);
                    float da = Depth(p[ia]), db = Depth(p[ib]), dc = Depth(p[ic]);
                    int x0 = (int)MathF.Floor(MathF.Min(ax, MathF.Min(bx, cx))), x1 = (int)MathF.Ceiling(MathF.Max(ax, MathF.Max(bx, cx)));
                    int y0 = (int)MathF.Floor(MathF.Min(ay, MathF.Min(by, cy))), y1 = (int)MathF.Ceiling(MathF.Max(ay, MathF.Max(by, cy)));
                    for (var y = Math.Max(0, y0); y <= Math.Min(height - 1, y1); y++)
                        for (var x = Math.Max(0, x0); x <= Math.Min(width - 1, x1); x++)
                        {
                            float qx = x + 0.5f, qy = y + 0.5f;
                            var w0 = ((bx - qx) * (cy - qy) - (by - qy) * (cx - qx)) / area;
                            var w1 = ((cx - qx) * (ay - qy) - (cy - qy) * (ax - qx)) / area;
                            var w2 = 1 - w0 - w1;
                            if (w0 < 0 || w1 < 0 || w2 < 0) continue;
                            var d = w0 * da + w1 * db + w2 * dc;
                            var k = y * width + x;
                            if (d < depth[k]) continue;
                            var colour = Math.Clamp(f.Colour + (int)(w0 * la + w1 * lb + w2 * lc), 0, 255);
                            var (r, g, b) = palette[colour];
                            if (seeThrough) { px[k * 4] = (byte)((px[k * 4] + b) / 2); px[k * 4 + 1] = (byte)((px[k * 4 + 1] + g) / 2); px[k * 4 + 2] = (byte)((px[k * 4 + 2] + r) / 2); continue; }
                            depth[k] = d;
                            px[k * 4] = b; px[k * 4 + 1] = g; px[k * 4 + 2] = r;
                        }
                }
            }
            foreach (var l in body.Lines)
            {
                var (ax, ay) = Screen(p[l.A]); var (bx, by) = Screen(p[l.B]);
                var steps = (int)MathF.Max(MathF.Abs(bx - ax), MathF.Abs(by - ay)) + 1;
                for (var s = 0; s <= steps; s++)
                {
                    int x = (int)(ax + ox + (bx - ax) * s / steps), y = (int)(ay + oy + (by - ay) * s / steps);
                    if (x < 0 || y < 0 || x >= width || y >= height) continue;
                    var (r, g, b) = palette[l.Colour];
                    var k = y * width + x;
                    px[k * 4] = b; px[k * 4 + 1] = g; px[k * 4 + 2] = r;
                }
            }
        }
        PngWriter.Write(path, px, width, height);
    }
}
