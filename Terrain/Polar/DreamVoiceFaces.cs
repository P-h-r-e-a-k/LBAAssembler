using System.IO;
using System.Buffers.Binary;
using System.Numerics;
using LbaBodyStudio;
using LBAAssembler.Assets;

namespace LBAAssembler.Terrain.Polar;

// The faces of the voices over the white as the Polar dream ends -- FunFrock's and Sendell's -- drawn in the dialogue box as they speak,
// where the game draws Zoe's or Baldino's picture for a radio call (MESSAGE.CPP TestAndDrawRadio): the user, 2026-11-26, "The unseen
// voices of FunFrock and Sendell, let's see if we can add a small picture of both characters faces to the text box to show who's saying
// it ... and use a different font colour instead of Zoe's so it's clear it's 3 different people". Made from the player's own game:
//   - FunFrock's from his body (BODY.HQR 313, the unmasked FunFrock of entity 217), drawn from in front -- his head and shoulders, the top
//     48% of him -- in the game's palette, lit as Body Studio lights a body (a light from above left, up to 9 steps along each colour's
//     ramp), four times over and brought down;
//   - Sendell's from the game's own picture of her as she speaks (SCREEN.HQR 70, its palette 71: the glowing figure scene 188 shows with
//     her lines), her head and shoulders cut out of the dark round her by her cyan glow, put into the game's palette.
// Each a sprite of its own at the end of SPRITES.HQR, its place in RESS.HQR's sprites' table: from the box's top right corner, as the
// radio's pictures are. DIALFACES.TXT tells the engine which speaker shows which (RACEMOD.CPP RaceMod_DialFace).
internal static class DreamVoiceFaces
{
    public const int Size = 112;
    private const int FunFrockBody = 313, SendellPicture = 70, RessSpriteZv = 5;
    private const float FunFrockTop = 0.48f;
    // (Sendell's head and shoulders in the 640 x 480 picture, a square)
    private const int SendellX = 160, SendellY = 10, SendellSide = 300;
    // (from the box's top right corner, MESSAGE.CPP: the face's left and top -- a margin round it inside the frame)
    private const int Margin = 10;

    // The two faces as sprites (the build's second run finds them there already). Sprite numbers, -1 where one couldn't be made.
    public static (int FunFrock, int Sendell, string Log) Install(string gameDirectory)
    {
        var spritesPath = Path.Combine(gameDirectory, "SPRITES.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var bodyPath = Path.Combine(gameDirectory, File.Exists(Path.Combine(gameDirectory, "OrigBODY.HQR")) ? "OrigBODY.HQR" : "BODY.HQR");
        var screenPath = Path.Combine(gameDirectory, "SCREEN.HQR");
        if (!File.Exists(spritesPath) || !File.Exists(ressPath)) return (-1, -1, "the dream's voices' faces left out: no SPRITES.HQR or RESS.HQR");
        var ress = HqrFile.Parse(File.ReadAllBytes(ressPath));
        var palette = ress.Read(0);
        if (palette.Length < 768) return (-1, -1, "the dream's voices' faces left out: RESS.HQR has no palette");
        var sprites = HqrFile.Parse(File.ReadAllBytes(spritesPath));
        var zv = ress.Read(RessSpriteZv);
        var notes = new List<string>();
        int Add(GphImage? face, string who)
        {
            if (face is null) { notes.Add($"{who}'s left out"); return -1; }
            var picture = Citadel.CitadelMuseum.EncodeBrick(face);
            var made = new byte[8 + picture.Length];
            BinaryPrimitives.WriteUInt32LittleEndian(made, 8);
            BinaryPrimitives.WriteUInt32LittleEndian(made.AsSpan(4), (uint)picture.Length);
            picture.CopyTo(made, 8);
            var number = -1;
            for (var n = sprites.Count - 1; n >= Math.Max(0, sprites.Count - 16) && number < 0; n--)
                if (!sprites.IsEmpty(n) && sprites.Read(n).AsSpan().SequenceEqual(made)) number = n;
            if (number < 0)
            {
                number = sprites.Count;
                sprites.Slots.Add(new HqrFile.Slot());
                sprites.SetStored(number, made);
            }
            if (zv.Length < (number + 1) * 16) Array.Resize(ref zv, (number + 1) * 16);
            Array.Clear(zv, number * 16, 16);
            BinaryPrimitives.WriteInt16LittleEndian(zv.AsSpan(number * 16), (short)-(face.Width + Margin));
            BinaryPrimitives.WriteInt16LittleEndian(zv.AsSpan(number * 16 + 2), (short)Margin);
            notes.Add($"{who}'s sprite {number}");
            return number;
        }
        GphImage? funFrock = null, sendell = null;
        try { if (File.Exists(bodyPath)) funFrock = BodyFace(Body.Read(HqrArchive.Open(bodyPath).Read(FunFrockBody), 2), palette); }
        catch (Exception e) { notes.Add($"FunFrock's face: {e.Message}"); }
        try
        {
            if (File.Exists(screenPath))
            {
                var screen = HqrArchive.Open(screenPath);
                sendell = PictureFace(screen.Read(SendellPicture), screen.Read(SendellPicture + 1), palette);
            }
        }
        catch (Exception e) { notes.Add($"Sendell's face: {e.Message}"); }
        var ff = Add(funFrock, "FunFrock");
        var se = Add(sendell, "Sendell");
        ress.SetStored(RessSpriteZv, zv);
        new FileTransaction().Write(spritesPath, sprites.ToBytes()).Write(ressPath, ress.ToBytes()).Commit();
        return (ff, se, $"the dream's voices' faces ({Size} px, from the box's top right as the radio's pictures): {string.Join(", ", notes)}");
    }

    // ---- FunFrock: his body drawn from in front, his head and shoulders
    private static GphImage BodyFace(Body body, byte[] palette)
    {
        const int ss = 4;
        var n = Size * ss;
        var world = body.World();
        var normals = body.VertexNormals();
        float minY = world.Min(v => v.Y), maxY = world.Max(v => v.Y), height = maxY - minY;
        var cut = maxY - height * FunFrockTop;
        var upper = world.Where(v => v.Y >= cut).ToList();
        float x0 = upper.Min(v => v.X), x1 = upper.Max(v => v.X);
        var side = Math.Max(maxY - cut, x1 - x0);
        var left = (x0 + x1) / 2 - side / 2;
        var scale = n / side;
        // (the light Body Studio's preview takes: from above left of the viewer; up to 9 steps along a colour's ramp, LightModel)
        var toLight = Vector3.Normalize(new Vector3(-0.35f, 0.55f, 0.75f));
        var light = normals.Select(v => v.LengthSquared() > 0 ? Math.Clamp(Vector3.Dot(Vector3.Normalize(v), toLight), 0, 1) * 9 : 4.5f).ToArray();
        var depth = Enumerable.Repeat(float.NegativeInfinity, n * n).ToArray();
        var index = new int[n * n];
        Array.Fill(index, -1);
        (float X, float Y, float Z) P(Vector3 v) => ((v.X - left) * scale, (maxY - v.Y) * scale, v.Z);
        foreach (var f in body.Faces)
        {
            if (f.Material == 2 && f.Texture is null) continue;            // (see-through: drawn over what is behind it -- left out here)
            var lit = f.Material < 0 ? body.Lit : f.Material is not (0 or 3);
            for (var t = 1; t + 1 < f.Points.Length; t++)
            {
                int ia = f.Points[0], ib = f.Points[t], ic = f.Points[t + 1];
                var (ax, ay, az) = P(world[ia]); var (bx, by, bz) = P(world[ib]); var (cx, cy, cz) = P(world[ic]);
                var area = (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
                if (Math.Abs(area) < 1e-4f) continue;
                int px0 = Math.Max(0, (int)MathF.Floor(Math.Min(ax, Math.Min(bx, cx)))), px1 = Math.Min(n - 1, (int)MathF.Ceiling(Math.Max(ax, Math.Max(bx, cx))));
                int py0 = Math.Max(0, (int)MathF.Floor(Math.Min(ay, Math.Min(by, cy)))), py1 = Math.Min(n - 1, (int)MathF.Ceiling(Math.Max(ay, Math.Max(by, cy))));
                for (var y = py0; y <= py1; y++)
                    for (var x = px0; x <= px1; x++)
                    {
                        float sx = x + 0.5f, sy = y + 0.5f;
                        var wa = ((bx - sx) * (cy - sy) - (by - sy) * (cx - sx)) / area;
                        var wb = ((cx - sx) * (ay - sy) - (cy - sy) * (ax - sx)) / area;
                        var wc = 1 - wa - wb;
                        if (wa < -1e-4f || wb < -1e-4f || wc < -1e-4f) continue;
                        var z = wa * az + wb * bz + wc * cz;
                        var i = y * n + x;
                        if (z <= depth[i]) continue;
                        depth[i] = z;
                        var c = Math.Clamp(f.Colour, 0, 255);
                        if (lit) c = Math.Min((c & ~15) + 15, c + (int)MathF.Round(wa * light[ia] + wb * light[ib] + wc * light[ic]));
                        index[i] = c;
                    }
            }
        }
        return Down(n, ss, i => index[i] < 0 ? null : ((byte, byte, byte)?)(palette[index[i] * 3], palette[index[i] * 3 + 1], palette[index[i] * 3 + 2]), palette);
    }

    // ---- Sendell: her head and shoulders cut from the game's picture by her glow
    private static GphImage PictureFace(byte[] picture, byte[] own, byte[] palette)
    {
        const int w = 640, s = SendellSide;
        if (picture.Length < w * 480 || own.Length < 768) throw new InvalidDataException("not a 640 x 480 picture and its palette");
        var rgb = new (byte R, byte G, byte B)[s * s];
        var glow = new bool[s * s];
        for (var y = 0; y < s; y++)
            for (var x = 0; x < s; x++)
            {
                var k = picture[(SendellY + y) * w + SendellX + x];
                (byte r, byte g, byte b) c = (own[k * 3], own[k * 3 + 1], own[k * 3 + 2]);
                rgb[y * s + x] = c;
                glow[y * s + x] = Math.Min(c.g, c.b) - c.r >= 48;      // (her cyan: the dark round her is not)
            }
        // (her glow closed over -- its gaps, her dark eyes, the white of its highlights -- and everything not reached from the edges
        // over the dark is her; a little in from her edge, where the glow fades into the dark)
        var shut = Erode(Dilate(glow, s, 7), s, 7);
        var outside = new bool[s * s];
        var queue = new Queue<int>();
        for (var i = 0; i < s; i++)
            foreach (var e in new[] { i, (s - 1) * s + i, i * s, i * s + s - 1 })
                if (!shut[e] && !outside[e]) { outside[e] = true; queue.Enqueue(e); }
        while (queue.Count > 0)
        {
            var i = queue.Dequeue();
            int x = i % s, y = i / s;
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= s || ny >= s) continue;
                var j = ny * s + nx;
                if (outside[j] || shut[j]) continue;
                outside[j] = true; queue.Enqueue(j);
            }
        }
        var her = Erode(outside.Select(o => !o).ToArray(), s, 2);
        return DownBox(s, i => her[i] ? ((byte, byte, byte)?)rgb[i] : null, palette);
    }

    private static bool[] Dilate(bool[] m, int s, int r) => Morph(m, s, r, true);
    private static bool[] Erode(bool[] m, int s, int r) => Morph(m, s, r, false);
    private static bool[] Morph(bool[] m, int s, int r, bool grow)
    {
        var across = new bool[s * s];
        for (var y = 0; y < s; y++)
            for (var x = 0; x < s; x++)
            {
                var v = !grow;
                for (var d = -r; d <= r; d++)
                {
                    var xx = x + d;
                    var here = xx >= 0 && xx < s ? m[y * s + xx] : !grow;
                    if (grow ? here : !here) { v = grow; break; }
                }
                across[y * s + x] = v;
            }
        var both = new bool[s * s];
        for (var y = 0; y < s; y++)
            for (var x = 0; x < s; x++)
            {
                var v = !grow;
                for (var d = -r; d <= r; d++)
                {
                    var yy = y + d;
                    var here = yy >= 0 && yy < s ? across[yy * s + x] : !grow;
                    if (grow ? here : !here) { v = grow; break; }
                }
                both[y * s + x] = v;
            }
        return both;
    }

    // A square of n x n (ss of its pixels to each of the face's) brought down to the face's size: a pixel drawn where at least half of
    // its own were, their colour's mean put into the game's palette (never index 0: the sprite's see-through)
    private static GphImage Down(int n, int ss, Func<int, (byte, byte, byte)?> at, byte[] palette)
    {
        var face = new GphImage(Size, Size);
        for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                int r = 0, g = 0, b = 0, count = 0;
                for (var yy = 0; yy < ss; yy++)
                    for (var xx = 0; xx < ss; xx++)
                        if (at((y * ss + yy) * n + x * ss + xx) is { } c) { r += c.Item1; g += c.Item2; b += c.Item3; count++; }
                if (count * 2 < ss * ss) continue;
                face.Pixels[y * Size + x] = Nearest(palette, r / count, g / count, b / count);
                face.Opaque[y * Size + x] = true;
            }
        return face;
    }

    // ... and a square of s x s brought down by boxes of s / Size of its pixels each way (not a whole number)
    private static GphImage DownBox(int s, Func<int, (byte, byte, byte)?> at, byte[] palette)
    {
        var face = new GphImage(Size, Size);
        for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                int sx0 = x * s / Size, sx1 = Math.Max(sx0 + 1, (x + 1) * s / Size), sy0 = y * s / Size, sy1 = Math.Max(sy0 + 1, (y + 1) * s / Size);
                int r = 0, g = 0, b = 0, count = 0, all = 0;
                for (var yy = sy0; yy < sy1; yy++)
                    for (var xx = sx0; xx < sx1; xx++, all++)
                        if (at(yy * s + xx) is { } c) { r += c.Item1; g += c.Item2; b += c.Item3; count++; }
                if (count * 2 < all) continue;
                face.Pixels[y * Size + x] = Nearest(palette, r / count, g / count, b / count);
                face.Opaque[y * Size + x] = true;
            }
        return face;
    }

    private static byte Nearest(byte[] palette, int r, int g, int b)
    {
        var best = 1;
        var bestD = int.MaxValue;
        for (var i = 1; i < 256; i++)
        {
            int dr = palette[i * 3] - r, dg = palette[i * 3 + 1] - g, db = palette[i * 3 + 2] - b;
            var d = dr * dr * 3 + dg * dg * 4 + db * db * 2;
            if (d < bestD) { bestD = d; best = i; }
        }
        return (byte)best;
    }

    // DIALFACES.TXT: each speaker with a face (scene, actor, sprite) -- RACEMOD.CPP RaceMod_DialFace
    public static void WriteMap(string gameDirectory, IEnumerable<(int Scene, int Actor, int Sprite)> faces)
    {
        var lines = new List<string> { "# the faces drawn in the dialogue box as a speaker speaks (LBA Assembler's Terrain/Polar/DreamVoiceFaces): face <scene> <actor> <sprite>" };
        lines.AddRange(faces.Where(f => f.Sprite >= 0).Select(f => $"face {f.Scene} {f.Actor} {f.Sprite}"));
        File.WriteAllLines(Path.Combine(gameDirectory, "DIALFACES.TXT"), lines);
    }
}
