using LBAAssembler;
using System.Numerics;
using LBAAssembler.Terrain.Palace;

namespace ScriptRoundTrip;

// animsheet <game folder> <BODY.HQR entry> <out.png> <ANIM.HQR entry>...: each animation's keyframes posed on the body, a row each (up to 12
// frames, evenly picked), flat-shaded from in front and a little to the side -- to see what an entity's animations do (the bowlers' throws,
// cheers and shrugs, 2026-11-15)
internal static class AnimSheetCommand
{
    public static int Run(string[] args)
    {
        var dir = args[1];
        var body = LbaBodyStudio.Body.Read(HqrArchive.Open(Path.Combine(dir, "BODY.HQR")).Read(int.Parse(args[2])), 2);
        var datas = args.Skip(4).Select(a => HqrArchive.Open(Path.Combine(dir, "ANIM.HQR")).Read(int.Parse(a))).ToList();
        Render(body, datas, args[3]);
        return 0;
    }

    // bowlanims <game folder> <out folder>: the bowlers' own reactions (Terrain/BowlingAnims) drawn, a sheet each -- before they go into a game
    public static int Bowlers(string[] args)
    {
        Directory.CreateDirectory(args[2]);
        foreach (var r in LBAAssembler.Terrain.BowlingAnims.Rigs)
        {
            var body = LbaBodyStudio.Body.Read(HqrArchive.Open(Path.Combine(args[1], "BODY.HQR")).Read(r.Body), 2);
            var (c, s, m) = LBAAssembler.Terrain.BowlingAnims.Make(r, HqrArchive.Open(Path.Combine(args[1], "ANIM.HQR")).Read(r.Stand));
            Render(body, new List<byte[]> { c.Write(r.Bones), s.Write(r.Bones), m.Write(r.Bones) }, Path.Combine(args[2], $"{r.Entity}.png"));
        }
        return 0;
    }

    public static void Render(LbaBodyStudio.Body body, List<byte[]> datas, string output)
    {
        var anims = datas;
        const int W = 140, H = 180, Most = 12;
        var sheet = new byte[W * Most * H * anims.Count * 4];
        int sw = W * Most, sh = H * anims.Count;
        for (var i = 0; i < sheet.Length; i += 4) { sheet[i] = 40; sheet[i + 1] = 40; sheet[i + 2] = 48; sheet[i + 3] = 255; }
        var yaw = 35 * Math.PI / 180;
        var light = Vector3.Normalize(new Vector3(-0.4f, -0.7f, -0.6f));
        for (var r = 0; r < anims.Count; r++)
        {
            var frames = PalaceTorturers.ReadFrames(anims[r], body.Bones.Count);
            var pick = Enumerable.Range(0, Math.Min(Most, frames.Count)).Select(k => frames.Count <= Most ? k : k * (frames.Count - 1) / (Most - 1)).ToList();
            // (one scale for the row: the body's height at rest)
            var rest = PalaceTorturers.Posed(body, frames[0].Bones).Points;
            var tall = Math.Max(1, rest.Max(p => p.Y) - rest.Min(p => p.Y));
            var scale = (H - 30) / (tall * 1.6f);
            for (var c = 0; c < pick.Count; c++)
            {
                var pts = PalaceTorturers.Posed(body, frames[pick[c]].Bones).Points;
                var proj = pts.Select(p => (X: (float)(p.X * Math.Cos(yaw) - p.Z * Math.Sin(yaw)), Y: p.Y, D: (float)(p.X * Math.Sin(yaw) + p.Z * Math.Cos(yaw)))).ToArray();
                var faces = body.Faces.Where(f => f.Points.Length >= 3).Select(f => (F: f, D: f.Points.Average(i => proj[i].D))).OrderByDescending(f => f.D).ToList();
                int ox = c * W + W / 2, oy = r * H + H - 20;
                foreach (var (f, _) in faces)
                {
                    var a = pts[f.Points[0]]; var b = pts[f.Points[1]]; var cc = pts[f.Points[2]];
                    var n = Vector3.Cross(b - a, cc - a);
                    var shade = n.LengthSquared() < 1e-6f ? 0.6f : 0.35f + 0.65f * Math.Abs(Vector3.Dot(Vector3.Normalize(n), light));
                    var col = (byte)(60 + 180 * shade);
                    var poly = f.Points.Select(i => (X: ox + proj[i].X * scale, Y: oy - proj[i].Y * scale)).ToArray();
                    Fill(sheet, sw, sh, poly, (byte)(col * ((f.Colour * 37) % 100 + 155) / 255), col, (byte)(col * ((f.Colour * 59) % 100 + 155) / 255), c * W, (c + 1) * W, r * H, (r + 1) * H);
                }
            }
            Console.WriteLine($"row {r}: {frames.Count} keyframes, shown {string.Join(",", pick)}");
        }
        PngWriter.Write(output, sheet, sw, sh);
    }

    private static void Fill(byte[] img, int w, int h, (float X, float Y)[] poly, byte rr, byte gg, byte bb, int x0, int x1, int y0, int y1)
    {
        var minY = Math.Max(y0, (int)Math.Floor(poly.Min(p => p.Y))); var maxY = Math.Min(y1 - 1, (int)Math.Ceiling(poly.Max(p => p.Y)));
        for (var y = minY; y <= maxY; y++)
        {
            var xs = new List<float>();
            for (var i = 0; i < poly.Length; i++)
            {
                var p = poly[i]; var q = poly[(i + 1) % poly.Length];
                if ((p.Y <= y + 0.5f && q.Y > y + 0.5f) || (q.Y <= y + 0.5f && p.Y > y + 0.5f)) xs.Add(p.X + (y + 0.5f - p.Y) / (q.Y - p.Y) * (q.X - p.X));
            }
            xs.Sort();
            for (var k = 0; k + 1 < xs.Count; k += 2)
                for (var x = Math.Max(x0, (int)Math.Ceiling(xs[k])); x <= Math.Min(x1 - 1, (int)Math.Floor(xs[k + 1])); x++)
                { var o = (y * w + x) * 4; img[o] = bb; img[o + 1] = gg; img[o + 2] = rr; img[o + 3] = 255; }
        }
    }
}
