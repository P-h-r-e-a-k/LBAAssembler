using LBAAssembler.Lba1;

namespace LBAAssembler.Terrain.Polar;

// The things LBA1 draws round in its bricks -- trees, the runic stone -- as bodies of their own on Polar's Fortress Island scenes, not as a box a brick (the user, 2026-11-01: "The trees
// for example are terrible, the runic stone that is important and iconic from the first game is the wrong shape"; "the sections of Fortress island from the same angle should be visibly
// identical if not slightly different colours"; "if in doubt we should take what we can see in the LBA1 scene and copy it symetrically"). A library block is one such thing, placed many
// times: its bricks drawn as LBA1 draws them are the picture (Picture), the body is made from the picture and textured with it, so from LBA1's own angle it looks as LBA1 has it.
internal static class PolarProps
{
    // A block of a scene's library drawn alone: LBA1 palette indices, -1 nothing; (OriginX, OriginY) where cell (0,0,0)'s middle foot falls.
    public sealed record Picture(int[] Pixels, int Width, int Height, int OriginX, int OriginY);

    public static (int Dx, int Dy, int Dz) Size(byte[] library, int block)
    {
        var at = BitConverter.ToInt32(library, (block - 1) * 4);
        return (library[at], library[at + 1], library[at + 2]);
    }

    // Where a position in a block is, inside it (x first, then layers, then z)
    public static (int X, int Y, int Z) Local(int pos, int dx, int dy) => (pos % dx, pos / dx % dy, pos / (dx * dy));

    // The shape of a thing's body. Whatever the shape, each point of it is textured with the point of the picture it falls on from LBA1's angle, so from there all look
    // the same -- the picture -- and what differs is how it looks from elsewhere: Round, rings fitted to the picture (leaning with it, flattened by Wide); Box, its block's
    // box; Sheet, a flat piece along its longer side through its middle (a fence); Cross, two of them across each other (a coil of wire, an anti-tank obstacle).
    public enum Form { Round, Box, Sheet, Cross }

    // What is made this way: a block of a scene, its form, how wide a Round is along x and z, how many of its layers are its own (another block sits on the rest), and Deep:
    // at the fortress's ground's level but over the pit's wall, no ground left under it (what stands lower has none anyway, what stands on the ground keeps it).
    public sealed record Kind(int Scene, int Block, Form Form, double WideX = 1, double WideZ = 1, int Layers = 99, bool Deep = false);
    public static readonly Kind[] Kinds =
    {
        new(93, 112, Form.Round), new(100, 112, Form.Round),   // the pines
        new(93, 207, Form.Round),                              // the dying ones
        new(93, 115, Form.Round, 0.42, 1),                     // the runic stone: broad across z, its carved face to +x
        new(93, 104, Form.Round),                              // the burst barrels
        new(93, 106, Form.Round),                              // the tank
        new(93, 49, Form.Round), new(100, 49, Form.Round),     // the fire baskets
        new(100, 247, Form.Round),                             // the rock huts
        new(100, 234, Form.Round), new(100, 238, Form.Round), new(100, 239, Form.Round),
        new(93, 105, Form.Box), new(93, 172, Form.Box), new(93, 116, Form.Box), new(93, 186, Form.Box), new(93, 229, Form.Box),
        new(93, 60, Form.Box, Layers: 3), new(93, 63, Form.Box, Deep: true),    // the pit's walls and the lips on them
        new(93, 61, Form.Box, Layers: 2), new(93, 62, Form.Box),
        new(93, 67, Form.Box), new(93, 68, Form.Box), new(93, 69, Form.Box), new(93, 70, Form.Box), new(93, 74, Form.Box), new(93, 75, Form.Box), new(93, 76, Form.Box),
        new(100, 67, Form.Box), new(100, 68, Form.Box), new(100, 69, Form.Box), new(100, 70, Form.Box), new(100, 71, Form.Box), new(100, 74, Form.Box),
        new(100, 75, Form.Box), new(100, 76, Form.Box), new(100, 77, Form.Box), new(100, 78, Form.Box),
        new(93, 225, Form.Sheet), new(93, 228, Form.Sheet), new(93, 91, Form.Sheet),
        new(93, 175, Form.Cross), new(93, 176, Form.Cross), new(93, 245, Form.Cross),
    };

    // One placed, in the layout's cells: its block's cell (0,0,0)
    public sealed record Placing(Kind Kind, int X, int Y, int Z);

    public static List<Placing> Find(Lba1Game game, IEnumerable<PolarLayout.Placed> placed, Func<int, List<PolarLayout.SceneBrick>> bricks,
        Dictionary<(int X, int Z), int> owner, Dictionary<(int, int, int), PolarLayout.Cell> cells)
    {
        var found = new List<Placing>();
        foreach (var p in placed)
        {
            var library = game.ReadBlocks(p.Scene);
            var ground = PolarLayout.FortressGround[p.Scene] + p.Y;
            foreach (var kind in Kinds.Where(k => k.Scene == p.Scene))
            {
                var (dx, dy, dz) = Size(library, kind.Block);
                var anchors = bricks(p.Scene).Where(b => b.Block == kind.Block).Select(b => { var l = Local(b.Pos, dx, dy); return (X: b.X - l.X + p.X, Y: b.Y - l.Y + p.Y, Z: b.Z - l.Z + p.Z); }).Distinct();
                foreach (var a in anchors)
                {
                    if (!owner.TryGetValue((a.X, a.Z), out var scene) || scene != p.Scene) continue;
                    found.Add(new Placing(kind, a.X, a.Y, a.Z));
                    // (the ground under it stays ground, the fortress's or the pit's)
                    var keep = kind.Deep || a.Y < ground ? a.Y - 1 : ground;
                    for (var z = 0; z < dz; z++)
                    for (var y = 0; y < dy; y++)
                    for (var x = 0; x < dx; x++)
                        if (a.Y + y > keep && cells.TryGetValue((a.X + x, a.Y + y, a.Z + z), out var c) && c.Block == kind.Block && c.Scene == p.Scene) cells.Remove((a.X + x, a.Y + y, a.Z + z));
                }
            }
        }
        return found;
    }

    // ---- the bodies ----------------------------------------------------------------------------------------------------------------------------------------

    private const int MostRings = 24, RingsToALayer = 3, Sides = 12, EllipsePoints = 28;
    private const double InsideShare = 0.82;
    private const int Fill = 12;   // POLY_TEXTURE_INCRUST: colour 0 not drawn, what the picture hasn't got shows through

    // Where a place of a block falls on its picture: (x, z in cells from the block's corner, y in layers from its foot)
    private static (double X, double Y) Project(Picture p, double x, double y, double z) => (p.OriginX + 24 + 24 * (x - z), p.OriginY + 15 * (1 - y) + 12 * (x + z));

    private static bool Inside(Picture p, double sx, double sy)
    {
        int x = (int)Math.Floor(sx), y = (int)Math.Floor(sy);
        return x >= 0 && y >= 0 && x < p.Width && y < p.Height && p.Pixels[y * p.Width + x] >= 0;
    }

    // With room for the whole of its block's box on it
    private static Picture Roomy(Picture p, int dx, int dy, int dz)
    {
        int left = Math.Min(0, p.OriginX + 24 - 24 * dz - 1), top = Math.Min(0, p.OriginY + 15 * (1 - dy) - 1);
        int right = Math.Max(p.Width, p.OriginX + 24 + 24 * dx + 1), bottom = Math.Max(p.Height, p.OriginY + 15 + 12 * (dx + dz) + 1);
        int w = right - left, h = bottom - top;
        var px = new int[w * h];
        Array.Fill(px, -1);
        for (var y = 0; y < p.Height; y++) Array.Copy(p.Pixels, y * p.Width, px, (y - top) * w - left, p.Width);
        return new Picture(px, w, h, p.OriginX - left, p.OriginY - top);
    }

    // The ring at height y: the widest ellipse that stays on the picture, sliding across the screen (a leaning tree); its size (the half widths as parts of Wide) and slide
    private static (double Size, double Slide) Ring(Picture p, Kind kind, double cx, double cz, double y)
    {
        var best = (Size: 0.0, Slide: 0.0);
        for (var slide = -1.0; slide <= 1.0; slide += 0.05)
        {
            bool Fits(double s)
            {
                var hits = 0;
                for (var i = 0; i < EllipsePoints; i++)
                {
                    var t = i * 2 * Math.PI / EllipsePoints;
                    var (sx, sy) = Project(p, cx + slide + s * kind.WideX * Math.Cos(t), y, cz - slide + s * kind.WideZ * Math.Sin(t));
                    if (Inside(p, sx, sy)) hits++;
                }
                return hits >= InsideShare * EllipsePoints;
            }
            double lo = best.Size, hi = 3.5;
            if (!Fits(lo + 0.01)) continue;
            for (var n = 0; n < 9; n++) { var m = (lo + hi) / 2; if (Fits(m)) lo = m; else hi = m; }
            if (lo > best.Size) best = (lo, slide);
        }
        return best;
    }

    // A body's points and faces in cells and layers of its block, its middle (Cx, Cz) at the decor's place
    private sealed class Mesh
    {
        public readonly Picture Picture;
        public readonly double Cx, Cz;
        public readonly List<System.Numerics.Vector3> Points = new();
        public readonly List<LbaBodyStudio.Face> Faces = new();
        private readonly Dictionary<(int, int, int), int> ids = new();
        public Mesh(Picture picture, double cx, double cz) { Picture = picture; Cx = cx; Cz = cz; }

        private int Point((double X, double Y, double Z) at)
        {
            const int S = PolarTerrain.Scale;
            var (x, y, z) = ((int)Math.Round((at.X - Cx) * 512 * S), (int)Math.Round(at.Y * 256 * S), (int)Math.Round((at.Z - Cz) * 512 * S));
            if (ids.TryGetValue((x, y, z), out var id)) return id;
            Points.Add(new System.Numerics.Vector3(x, y, z));
            return ids[(x, y, z)] = Points.Count - 1;
        }

        // A face of those corners, each textured with the place of the block `seen` says
        public void Face(Func<(double X, double Y, double Z), (double X, double Y, double Z)> seen, params (double X, double Y, double Z)[] corners)
        {
            var uv = new List<int>();
            foreach (var c in corners)
            {
                var s = seen(c);
                var (sx, sy) = Project(Picture, s.X, s.Y, s.Z);
                uv.Add((int)Math.Round(Math.Clamp(sx, 0, Picture.Width - 1) * 256));
                uv.Add((int)Math.Round(Math.Clamp(sy, 0, Picture.Height - 1) * 256));
            }
            Faces.Add(new LbaBodyStudio.Face(corners.Select(Point).ToArray(), 0, Texture: new LbaBodyStudio.FaceTexture(0, uv.ToArray()), Lba2Type: Fill));
        }

        // A flat piece of nu x nv quads, facing along (the way along u) x (the way along v)
        public void Patch(int nu, int nv, Func<double, double, (double X, double Y, double Z)> at, Func<(double X, double Y, double Z), (double X, double Y, double Z)> seen)
        {
            for (var u = 0; u < nu; u++)
                for (var v = 0; v < nv; v++)
                {
                    double u0 = u / (double)nu, u1 = (u + 1) / (double)nu, v0 = v / (double)nv, v1 = (v + 1) / (double)nv;
                    Face(seen, at(u0, v0), at(u1, v0), at(u1, v1), at(u0, v1));
                }
        }
    }

    private static void MakeRound(Mesh mesh, Kind kind, double height)
    {
        var rings = new List<(double Y, double Size, double Slide)>();
        var count = Math.Clamp((int)height * RingsToALayer, 6, MostRings);
        for (var k = 0; k <= count; k++)
        {
            var y = height * k / count;
            // (a little in from the very top and foot, where nothing is left of the outline to fit)
            var (size, slide) = Ring(mesh.Picture, kind, mesh.Cx, mesh.Cz, Math.Clamp(y, 0.02, height - 0.02));
            rings.Add((y, size, slide));
        }
        while (rings.Count > 2 && rings[^1].Size <= 0.02) rings.RemoveAt(rings.Count - 1);
        while (rings.Count > 2 && rings[0].Size <= 0.02) rings.RemoveAt(0);
        (double, double, double) At(int k, int j)
        {
            var t = j * 2 * Math.PI / Sides;
            var r = rings[k];
            return (mesh.Cx + r.Slide + r.Size * kind.WideX * Math.Cos(t), r.Y, mesh.Cz - r.Slide + r.Size * kind.WideZ * Math.Sin(t));
        }
        // (from behind: the place in front it mirrors, as LBA1's things are round)
        (double, double, double) Seen((double X, double Y, double Z) c)
        {
            var depth = (c.X - mesh.Cx) + (c.Z - mesh.Cz);
            return depth < 0 ? (c.X - depth, c.Y, c.Z - depth) : c;
        }
        for (var k = 0; k + 1 < rings.Count; k++)
            for (var j = 0; j < Sides; j++)
                mesh.Face(Seen, At(k, j), At(k + 1, j), At(k + 1, (j + 1) % Sides), At(k, (j + 1) % Sides));
        var last = rings[^1];
        var apex = (mesh.Cx + last.Slide, last.Y + height * 0.6 / count, mesh.Cz - last.Slide);
        for (var j = 0; j < Sides; j++) mesh.Face(Seen, At(rings.Count - 1, j), apex, At(rings.Count - 1, (j + 1) % Sides));
    }

    private static void MakeBox(Mesh mesh, int dx, int dy, int dz)
    {
        static (double, double, double) Same((double X, double Y, double Z) c) => c;
        mesh.Patch(dy, dz, (u, v) => (dx, u * dy, v * dz), Same);
        mesh.Patch(dx, dy, (u, v) => (u * dx, v * dy, dz), Same);
        mesh.Patch(dz, dx, (u, v) => (v * dx, dy, u * dz), Same);
        // (its far sides, which LBA1 never shows: the near ones, mirrored)
        mesh.Patch(dz, dy, (u, v) => (0, v * dy, u * dz), c => (dx, c.Y, c.Z));
        mesh.Patch(dy, dx, (u, v) => (v * dx, u * dy, 0), c => (c.X, c.Y, dz));
    }

    // A flat piece along x (or z) through the block's middle, as wide and as high as the picture reaches on it, seen on both faces
    private static void MakeSheet(Mesh mesh, bool alongX)
    {
        var p = mesh.Picture;
        double a0 = double.MaxValue, a1 = double.MinValue, y0 = double.MaxValue, y1 = double.MinValue;
        var middle = alongX ? mesh.Cz : mesh.Cx;
        for (var py = 0; py < p.Height; py++)
            for (var px = 0; px < p.Width; px++)
            {
                if (p.Pixels[py * p.Width + px] < 0) continue;
                var along = (px - p.OriginX - 24) / 24.0;
                var a = alongX ? middle + along : middle - along;
                var y = 1 + (p.OriginY + 12 * (a + middle) - py) / 15.0;
                a0 = Math.Min(a0, a); a1 = Math.Max(a1, a); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y);
            }
        if (a0 > a1) return;
        y0 = Math.Max(0, y0);
        (double, double, double) At(double a, double y) => alongX ? (a0 + a * (a1 - a0), y0 + y * (y1 - y0), middle) : (middle, y0 + y * (y1 - y0), a0 + a * (a1 - a0));
        int na = Math.Max(1, (int)Math.Ceiling(a1 - a0)), ny = Math.Max(1, (int)Math.Ceiling(y1 - y0));
        static (double, double, double) Same((double X, double Y, double Z) c) => c;
        mesh.Patch(na, ny, At, Same);
        mesh.Patch(ny, na, (u, v) => At(v, u), Same);
    }

    public static int Build(Lba1Game game, PolarLayout layout, IslandFile island, PolarTextures.Pages pages, List<byte[]> bodies, List<string> log)
    {
        var made = new Dictionary<Kind, (int Body, int Page, double HalfX, double HalfZ, double Tall)>();
        var decors = 0;
        const int S = PolarTerrain.Scale;
        foreach (var placing in layout.Props)
        {
            var kind = placing.Kind;
            var library = game.ReadBlocks(kind.Scene);
            var (dx, dy, dz) = Size(library, kind.Block);
            dy = Math.Min(dy, kind.Layers);
            if (!made.TryGetValue(kind, out var m))
            {
                var picture = Roomy(Draw(game, library, kind.Block, dy), dx, dy, dz);
                if (pages.AddPicture(picture.Pixels, picture.Width, picture.Height) is not { } at) { log.Add($"block {kind.Block}: no page for its picture"); continue; }
                var mesh = new Mesh(picture, dx / 2.0, dz / 2.0);
                switch (kind.Form)
                {
                    case Form.Round: MakeRound(mesh, kind, dy); break;
                    case Form.Box: MakeBox(mesh, dx, dy, dz); break;
                    case Form.Sheet: MakeSheet(mesh, dx >= dz); break;
                    default: MakeSheet(mesh, true); MakeSheet(mesh, false); break;
                }
                var header = new byte[96];
                header[0] = 0x10;
                var body = new LbaBodyStudio.Body
                {
                    Game = 2, Static = true, Lit = false, Header = header, Vertices = mesh.Points,
                    Bones = new List<LbaBodyStudio.Bone> { new(0, mesh.Points.Count, 0, -1, new byte[8]) }, Faces = mesh.Faces, Textures = new[] { 0xFFFF0000u | (uint)(at.Y * 256 + at.X) },
                };
                // (what stops a car: a box's whole box, a fence's line, the trunk of what's round)
                var (hx, hz) = kind.Form switch
                {
                    Form.Box => (dx / 2.0, dz / 2.0),
                    Form.Round => (0.3 * Math.Min(dx, dz), 0.3 * Math.Min(dx, dz)),
                    Form.Sheet => dx >= dz ? (dx / 2.0, 0.2) : (0.2, dz / 2.0),
                    _ => (0.3, 0.3),
                };
                m = (bodies.Count, at.Page, hx, hz, dy * 256.0 * S);
                made[kind] = m;
                bodies.Add(body.Write());
                log.Add($"block {kind.Block} of scene {kind.Scene}: a {kind.Form} of {mesh.Faces.Count} faces, {mesh.Points.Count} points, its picture {picture.Width} x {picture.Height} on page {at.Page}");
            }
            int wx = (int)Math.Round((PolarTerrain.IslandX(placing.X) + dx * S / 2.0) * 512), wz = (int)Math.Round((PolarTerrain.IslandZ(placing.Z) + dz * S / 2.0) * 512);
            var cube = island.CubeAt(wx / IslandFile.CubeSize, wz / IslandFile.CubeSize);
            if (cube is null || cube.Decors.Count >= IslandDecors.MaxPerCube) { log.Add($"block {kind.Block} at ({placing.X},{placing.Z}): no room in its cube"); continue; }
            int lx = wx % IslandFile.CubeSize, lz = wz % IslandFile.CubeSize, foot = PolarTerrain.LayerY(placing.Y - 1);
            var decor = IslandDecors.Blank(m.Body | (m.Page << IslandFile.DecorPageShift), lx, foot, lz);
            decor.XMin = lx - (int)Math.Round(m.HalfX * 512 * S); decor.XMax = lx + (int)Math.Round(m.HalfX * 512 * S);
            decor.ZMin = lz - (int)Math.Round(m.HalfZ * 512 * S); decor.ZMax = lz + (int)Math.Round(m.HalfZ * 512 * S);
            decor.YMin = foot; decor.YMax = foot + (int)m.Tall;
            cube.Decors.Add(decor);
            decors++;
        }
        if (made.Count > 0) log.Add($"{decors} things of the fortress scenes as bodies of their own, {made.Count} bodies");
        return decors;
    }

    public static Picture Draw(Lba1Game game, byte[] library, int block, int layers = int.MaxValue)
    {
        var at = BitConverter.ToInt32(library, (block - 1) * 4);
        int dx = library[at], dy = library[at + 1], dz = library[at + 2];
        var parts = new List<(int X, int Y, int Z, PolarTextures.Sprite Sprite)>();
        for (var pos = 0; pos < dx * dy * dz; pos++)
        {
            var number = BitConverter.ToUInt16(library, at + 3 + pos * 4 + 2);
            if (number == 0) continue;
            var (x, y, z) = Local(pos, dx, dy);
            if (y >= layers) continue;
            parts.Add((x, y, z, PolarTextures.Sprite.Decode(game.ReadBrick(number - 1))));
        }
        if (parts.Count == 0) return new Picture(Array.Empty<int>(), 0, 0, 0, 0);
        int Left((int X, int Y, int Z, PolarTextures.Sprite S) p) => 24 * (p.X - p.Z) + p.S.HotX;
        int Top((int X, int Y, int Z, PolarTextures.Sprite S) p) => 12 * (p.X + p.Z) - 15 * p.Y + p.S.HotY;
        int x0 = parts.Min(Left), y0 = parts.Min(Top), x1 = parts.Max(p => Left(p) + p.Sprite.Width), y1 = parts.Max(p => Top(p) + p.Sprite.Lines);
        int w = x1 - x0, h = y1 - y0;
        var pixels = new int[w * h];
        Array.Fill(pixels, -1);
        foreach (var p in parts.OrderBy(p => p.Z).ThenBy(p => p.X).ThenBy(p => p.Y))
        {
            int left = Left(p) - x0, top = Top(p) - y0;
            for (var j = 0; j < p.Sprite.Lines; j++)
                for (var i = 0; i < p.Sprite.Width; i++)
                {
                    var c = p.Sprite.Pixels[j * p.Sprite.Width + i];
                    if (c >= 0) pixels[(top + j) * w + left + i] = c;
                }
        }
        return new Picture(pixels, w, h, -x0, -y0);
    }
}
