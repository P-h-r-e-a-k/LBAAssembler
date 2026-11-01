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
    // box; Sheet, a flat piece along its longer side through its middle (a fence); Cross, two of them across each other (a coil of wire, an anti-tank obstacle); Mound, a row
    // of boulders as one solid hump along its block, as high as the picture is at each place (as a box, a painted shell, hollow from behind).
    public enum Form { Round, Box, Sheet, Cross, Mound }

    // What is made this way: a block of a scene, its form, how wide a Round is along x and z, how many of its layers are its own (another block sits on the rest), and Deep:
    // at the fortress's ground's level but over the pit's wall, no ground left under it (what stands lower has none anyway, what stands on the ground keeps it).
    // (Cut: a Box only this many cells of its block long, the rest of the picture unused. Water: its foot stands in water drawn in the block, not to be fitted: the rings there the
    // ones above it, on down to the island's sea. Solid: a Box with the picture filled out, not see-through. Fit: its box drawn in to the corner the picture has. Share: how much of
    // a ring has to be on the picture, for a thing that isn't one round shape. Skirt: on down past its foot, for a thing on the shore)
    public sealed record Kind(int Scene, int Block, Form Form, double WideX = 1, double WideZ = 1, int Layers = 99, bool Deep = false, int Cut = 0, int Water = 0,
        bool Solid = false, bool Fit = false, double Share = 0, bool Skirt = false, bool NoGreen = false);
    public static readonly Kind[] Kinds =
    {
        new(93, 112, Form.Round), new(100, 112, Form.Round),   // the pines
        new(93, 207, Form.Round),                              // the dying ones
        new(93, 115, Form.Round, 0.42, 1),                     // the runic stone: broad across z, its carved face to +x
        new(93, 104, Form.Round),                              // the burst barrels
        new(93, 106, Form.Round),                              // the tank
        new(93, 49, Form.Round), new(100, 49, Form.Round),     // the fire baskets
        new(100, 247, Form.Round),                             // the rock huts
        new(100, 234, Form.Round, Deep: true, Water: 1), new(100, 238, Form.Round), new(100, 239, Form.Round),
        new(93, 105, Form.Box), new(93, 172, Form.Box, Solid: true, Fit: true), new(93, 116, Form.Box), new(93, 186, Form.Box), new(93, 229, Form.Box),
        new(93, 60, Form.Box, Layers: 3, Solid: true, NoGreen: true), new(93, 63, Form.Box, Deep: true, Solid: true, NoGreen: true),    // the pit's walls and the lips on them
        new(93, 61, Form.Box, Layers: 2, Solid: true, NoGreen: true), new(93, 62, Form.Box, Solid: true, NoGreen: true),
        new(93, 67, Form.Mound), new(93, 68, Form.Mound), new(93, 69, Form.Mound), new(93, 70, Form.Mound), new(93, 74, Form.Round, Skirt: true), new(93, 75, Form.Round, Skirt: true), new(93, 76, Form.Round, Skirt: true),
        new(100, 67, Form.Mound), new(100, 68, Form.Mound), new(100, 69, Form.Mound), new(100, 70, Form.Mound), new(100, 71, Form.Round, Skirt: true), new(100, 74, Form.Round, Skirt: true),
        new(100, 75, Form.Round, Skirt: true), new(100, 76, Form.Round, Skirt: true), new(100, 77, Form.Round, Skirt: true), new(100, 78, Form.Round, Skirt: true),
        new(93, 225, Form.Sheet), new(93, 228, Form.Sheet), new(93, 91, Form.Sheet),
        new(93, 175, Form.Cross), new(93, 176, Form.Cross), new(93, 245, Form.Cross),
    };

    // One placed, in the layout's cells: its block's cell (0,0,0)
    public sealed record Placing(Kind Kind, int X, int Y, int Z, int Beta = 0);

    // The pit's two sides LBA1's camera has its back to: nothing drawn there, the ground dropping into black. Its far walls copied onto them, whole, as a Box's far side shows
    // the near one's picture (the user, 2026-11-01: "the pit still has black where walls should be"; "take what we can see in the LBA1 scene and copy it symetrically").
    // (x0..x1, z0..z1: the pit's floor. Its walls stand at x0 - 1 and z0 - 1, these at x1 + 1 and z1 + 1, the ground under them the floor's: the drop from the fortress's ground
    // is inside the wall's box, as it is on the far sides)
    private const int PitX0 = 49, PitX1 = 62, PitZ0 = 166, PitZ1 = 179, PitFloor = -2, PitWallFoot = -1, WallLength = 5;
    public static bool InPit(int x, int z) => x >= PitX0 - 2 && x <= PitX1 + 2 && z >= PitZ0 - 2 && z <= PitZ1 + 2;
    private static readonly Kind Corner = new(93, 60, Form.Box, Cut: 1, Solid: true, NoGreen: true);

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
                        // (kept only as ground: a row of boulders' foot on the shore, no ground, stayed an object, a flat black thing on the water)
                        if (cells.TryGetValue((a.X + x, a.Y + y, a.Z + z), out var c) && c.Block == kind.Block && c.Scene == p.Scene && (a.Y + y > keep || !PolarTerrain.IsGround(game, c))) cells.Remove((a.X + x, a.Y + y, a.Z + z));
                }
            }
        }
        // (any of the floor's bricks: most of it has none, the ground filled in as under a building's roof, PolarTerrain.Columns)
        var floors = cells.Where(c => c.Key.Item2 == PitFloor && c.Key.Item1 >= PitX0 && c.Key.Item1 <= PitX1 && c.Key.Item3 >= PitZ0 && c.Key.Item3 <= PitZ1 && c.Value.Scene == PolarLayout.FortressOutside).ToList();
        if (floors.Count > 0)
        {
            var floor = floors[0].Value;
            void Open(int x, int z)
            {
                foreach (var y in Enumerable.Range(PitWallFoot, PolarLayout.FortressGround[floor.Scene] + 1 + 8))
                    if (cells.TryGetValue((x, y, z), out var c) && c.Scene == floor.Scene && !Kinds.Any(k => k.Scene == c.Scene && k.Block == c.Block && y > 3)) cells.Remove((x, y, z));
                cells[(x, PitFloor, z)] = floor;
            }
            for (var x = PitX0; x <= PitX1 + 1; x++) Open(x, PitZ1 + 1);
            for (var z = PitZ0; z <= PitZ1; z++) Open(PitX1 + 1, z);
            // (and the four corners: a corner's ground left at the fortress's height pulled the floor's corner up to it, a shard of grass in the pit's angle)
            foreach (var (x, z) in new[] { (PitX0 - 1, PitZ0 - 1), (PitX1 + 1, PitZ0 - 1), (PitX0 - 1, PitZ1 + 1) }) Open(x, z);
            // (the far walls' own bodies, the wall and the lip on it, turned round: their faces into the pit)
            const int TurnedRound = 2048;
            Kind Of(int block) => Kinds.First(k => k.Scene == PolarLayout.FortressOutside && k.Block == block);
            for (var x = PitX0; x <= PitX1; x += WallLength)
            {
                found.Add(new Placing(Of(60), x, PitWallFoot, PitZ1, TurnedRound));
                found.Add(new Placing(Of(63), x, PitWallFoot + 3, PitZ1, TurnedRound));
            }
            for (var z = PitZ0; z <= PitZ1; z += WallLength)
            {
                found.Add(new Placing(Of(61), PitX1, PitWallFoot, z, TurnedRound));
                found.Add(new Placing(Of(62), PitX1, PitWallFoot + 2, z, TurnedRound));
            }
            found.Add(new Placing(Corner, PitX1, PitWallFoot, PitZ0 - 1));
            found.Add(new Placing(Corner, PitX0 - 1, PitWallFoot, PitZ1));
        }
        return found;
    }

    // ---- the bodies ----------------------------------------------------------------------------------------------------------------------------------------

    private const int MostRings = 24, RingsToALayer = 3, Sides = 12, EllipsePoints = 28;
    private const double InsideShare = 0.9;
    // (the far side of a round thing is its near side mirrored, but not right out to the edge: there LBA1's picture has a few pixels for a quarter of the way round, its dark
    // outline, and from the side the whole of it was that, streaked. Round to here it is as LBA1 has it, past here it runs back to the middle)
    private const double NearAngle = 75 * Math.PI / 180, FarAngle = 15 * Math.PI / 180;
    private const int MoundSteps = 7, MoundLengthSteps = 4, WaterBelow = 2;
    private const int Solid = 8, SeeThrough = 12;   // POLY_TEXTURE, and POLY_TEXTURE_INCRUST: colour 0 not drawn, what the picture hasn't got shows through

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

    // Every pixel the picture hasn't got its nearest that it has: a Round is solid, and from a low angle more of it shows than LBA1's camera saw (see-through, the undersides
    // of a pine's tiers were holes in it, the user, 2026-11-01)
    private static Picture Filled(Picture p)
    {
        var px = (int[])p.Pixels.Clone();
        var edge = new Queue<int>();
        for (var i = 0; i < px.Length; i++) if (px[i] >= 0) edge.Enqueue(i);
        while (edge.Count > 0)
        {
            var i = edge.Dequeue();
            int x = i % p.Width, y = i / p.Width;
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= p.Width || ny >= p.Height || px[ny * p.Width + nx] >= 0) continue;
                px[ny * p.Width + nx] = px[i];
                edge.Enqueue(ny * p.Width + nx);
            }
        }
        return p with { Pixels = px };
    }

    // A place round a ring of size 1
    private static (double X, double Z) Around(Kind kind, double t) => (Math.Cos(t) * kind.WideX, Math.Sin(t) * kind.WideZ);

    private static Picture NoGrass(Picture p, byte[] palette) =>
        p with { Pixels = p.Pixels.Select(c => { if (c < 0) return c; var (r, g, _) = PolarTextures.Colours.Rgb(palette, c); return g > r ? -1 : c; }).ToArray() };

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
                    var (ox, oz) = Around(kind, i * 2 * Math.PI / EllipsePoints);
                    var (sx, sy) = Project(p, cx + slide + s * ox, y, cz - slide + s * oz);
                    if (Inside(p, sx, sy)) hits++;
                }
                return hits >= (kind.Share > 0 ? kind.Share : InsideShare) * EllipsePoints;
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
        public int Fill = SeeThrough;
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
        if (kind.Water > 0)
        {
            var (size, slide) = Ring(mesh.Picture, kind, mesh.Cx, mesh.Cz, kind.Water + 0.1);
            rings.RemoveAll(r => r.Y < kind.Water);
            rings.Insert(0, (kind.Water, size, slide));
            rings.Insert(0, (-WaterBelow, size, slide));
        }
        while (rings.Count > 2 && rings[^1].Size <= 0.02) rings.RemoveAt(rings.Count - 1);
        while (rings.Count > 2 && rings[0].Size <= 0.02) rings.RemoveAt(0);
        if (kind.Skirt) rings.Insert(0, (-WaterBelow, rings[0].Size, rings[0].Slide));
        (double, double, double) At(int k, int j)
        {
            var r = rings[k];
            var (ox, oz) = Around(kind, j * 2 * Math.PI / Sides);
            return (mesh.Cx + r.Slide + r.Size * ox, r.Y, mesh.Cz - r.Slide + r.Size * oz);
        }
        // (where on the picture: LBA1's camera looks along x + z. As far round as NearAngle the place itself; past it, back to the middle at the far side, as LBA1's things are round)
        double Slide(double y) => rings.OrderBy(r => Math.Abs(r.Y - y)).First().Slide;
        (double, double, double) Seen((double X, double Y, double Z) w)
        {
            var c = (w.X, Y: Math.Max(w.Y, kind.Water), w.Z);
            var s = Slide(c.Y);
            double ox = c.X - mesh.Cx - s, oz = c.Z - mesh.Cz + s, depth = (ox + oz) / Math.Sqrt(2), across = (ox - oz) / Math.Sqrt(2);
            var angle = Math.Atan2(Math.Abs(across), depth);
            if (angle <= NearAngle) return c;
            var near = FarAngle + (NearAngle - FarAngle) * (Math.PI - angle) / (Math.PI - NearAngle);
            var reach = Math.Sqrt(depth * depth + across * across);
            double d = reach * Math.Cos(near), a = reach * Math.Sin(near) * Math.Sign(across);
            return (mesh.Cx + s + (d + a) / Math.Sqrt(2), c.Y, mesh.Cz - s + (d - a) / Math.Sqrt(2));
        }
        for (var k = 0; k + 1 < rings.Count; k++)
            for (var j = 0; j < Sides; j++)
                mesh.Face(Seen, At(k, j), At(k + 1, j), At(k + 1, (j + 1) % Sides), At(k, (j + 1) % Sides));
        var last = rings[^1];
        var apex = (mesh.Cx + last.Slide, last.Y + height * 0.6 / count, mesh.Cz - last.Slide);
        for (var j = 0; j < Sides; j++) mesh.Face(Seen, At(rings.Count - 1, j), apex, At(rings.Count - 1, (j + 1) % Sides));
    }

    // A row of boulders: a hump along the block's long side, half an ellipse across it, as high at each place as the picture's outline. Its far side the near one mirrored
    private static void MakeMound(Mesh mesh, int dx, int dy, int dz)
    {
        var p = mesh.Picture;
        var alongX = dx >= dz;
        var length = alongX ? dx : dz;
        var steps = length * MoundLengthSteps;
        // the top of the picture at a place along the middle of the row (in layers)
        double Top(double a)
        {
            double x = alongX ? a : mesh.Cx, z = alongX ? mesh.Cz : a;
            var (sx, _) = Project(p, x, 0, z);
            var column = Math.Clamp((int)Math.Floor(sx), 0, p.Width - 1);
            for (var py = 0; py < p.Height; py++)
                if (p.Pixels[py * p.Width + column] >= 0) return Math.Max(0, 1 + (p.OriginY + 12 * (x + z) - py) / 15.0);
            return 0;
        }
        var tops = new double[steps + 1];
        for (var i = 0; i <= steps; i++) tops[i] = i == 0 || i == steps ? 0.15 : Math.Max(0.15, Math.Min(dy, Top(length * i / (double)steps)));
        (double, double, double) At(int i, int j)
        {
            var angle = Math.PI * j / MoundSteps;
            var a = length * i / (double)steps;
            double across = 0.5 * Math.Cos(angle), y = tops[i] * Math.Sin(angle);
            return alongX ? (a, y, mesh.Cz + across) : (mesh.Cx + across, y, a);
        }
        (double, double, double) Seen((double X, double Y, double Z) w)
        {
            var y = Math.Max(0, w.Y);
            return alongX ? (w.X, y, mesh.Cz + Math.Abs(w.Z - mesh.Cz)) : (mesh.Cx + Math.Abs(w.X - mesh.Cx), y, w.Z);
        }
        for (var i = 0; i < steps; i++)
            for (var j = 0; j < MoundSteps; j++)
                if (alongX) mesh.Face(Seen, At(i, j), At(i + 1, j), At(i + 1, j + 1), At(i, j + 1));
                else mesh.Face(Seen, At(i, j), At(i, j + 1), At(i + 1, j + 1), At(i + 1, j));
        // (and down past its foot on both sides: on the shore it stands over the sea)
        for (var i = 0; i < steps; i++)
        {
            double a0 = length * i / (double)steps, a1 = length * (i + 1) / (double)steps;
            if (alongX)
            {
                mesh.Face(Seen, (a0, -WaterBelow, mesh.Cz + 0.5), (a1, -WaterBelow, mesh.Cz + 0.5), (a1, 0, mesh.Cz + 0.5), (a0, 0, mesh.Cz + 0.5));
                mesh.Face(Seen, (a0, -WaterBelow, mesh.Cz - 0.5), (a0, 0, mesh.Cz - 0.5), (a1, 0, mesh.Cz - 0.5), (a1, -WaterBelow, mesh.Cz - 0.5));
            }
            else
            {
                mesh.Face(Seen, (mesh.Cx + 0.5, -WaterBelow, a0), (mesh.Cx + 0.5, 0, a0), (mesh.Cx + 0.5, 0, a1), (mesh.Cx + 0.5, -WaterBelow, a1));
                mesh.Face(Seen, (mesh.Cx - 0.5, -WaterBelow, a0), (mesh.Cx - 0.5, -WaterBelow, a1), (mesh.Cx - 0.5, 0, a1), (mesh.Cx - 0.5, 0, a0));
            }
        }
    }

    private static void MakeBox(Mesh mesh, int dx, int dy, int dz, bool fit)
    {
        // (drawn in: the near corner where the picture's foot is, its roof where its top is)
        double a = 0, top = dy;
        if (fit)
        {
            var p = mesh.Picture;
            var (sx, _) = Project(p, mesh.Cx, 0, mesh.Cz);
            var column = Math.Clamp((int)sx, 0, p.Width - 1);
            int low = 0, high = p.Height - 1;
            for (var y = 0; y < p.Height; y++) if (p.Pixels[y * p.Width + column] >= 0) { low = Math.Max(low, y); high = Math.Min(high, y); }
            a = Math.Clamp((dx + dz - (low - p.OriginY - 15) / 12.0) / 2, 0, 0.8);
            top = Math.Clamp(1 + (p.OriginY + 24 * a - high) / 15.0, 1, dy);
        }
        double x0 = a, x1 = dx - a, z0 = a, z1 = dz - a;
        var (nx, ny, nz) = (dx, (int)Math.Ceiling(top), dz);
        static (double, double, double) Same((double X, double Y, double Z) c) => c;
        mesh.Patch(ny, nz, (u, v) => (x1, u * top, z0 + v * (z1 - z0)), Same);
        mesh.Patch(nx, ny, (u, v) => (x0 + u * (x1 - x0), v * top, z1), Same);
        mesh.Patch(nz, nx, (u, v) => (x0 + v * (x1 - x0), top, z0 + u * (z1 - z0)), Same);
        // (its far sides, which LBA1 never shows: the near ones, mirrored)
        mesh.Patch(nz, ny, (u, v) => (x0, v * top, z0 + u * (z1 - z0)), c => (x1, c.Y, c.Z));
        mesh.Patch(ny, nx, (u, v) => (x0 + v * (x1 - x0), u * top, z0), c => (c.X, c.Y, z1));
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
            var (wholeX, wholeZ) = (dx, dz);
            if (kind.Cut > 0) { dx = Math.Min(dx, kind.Cut); dz = Math.Min(dz, kind.Cut); }
            if (!made.TryGetValue(kind, out var m))
            {
                var picture = Roomy(Draw(game, library, kind.Block, dy), wholeX, dy, wholeZ);
                // (rings fitted to the outline, then the picture filled out past it)
                var round = kind.Solid || kind.Form is Form.Round or Form.Mound;
                var mesh = new Mesh(picture, dx / 2.0, dz / 2.0) { Fill = round ? Solid : SeeThrough };
                switch (kind.Form)
                {
                    case Form.Round: MakeRound(mesh, kind, dy); break;
                    case Form.Mound: MakeMound(mesh, dx, dy, dz); break;
                    case Form.Box: MakeBox(mesh, dx, dy, dz, kind.Fit); break;
                    case Form.Sheet: MakeSheet(mesh, dx >= dz); break;
                    default: MakeSheet(mesh, true); MakeSheet(mesh, false); break;
                }
                // (a wall's grass, on its top edge, not spread down its face)
                var shown = round ? Filled(kind.NoGreen ? NoGrass(picture, game.Palette) : picture) : picture;
                if (pages.AddPicture(shown.Pixels, shown.Width, shown.Height) is not { } at) { log.Add($"block {kind.Block}: no page for its picture"); continue; }
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
                    Form.Box or Form.Mound => (dx / 2.0, dz / 2.0),
                    Form.Round => (0.3 * Math.Min(dx, dz), 0.3 * Math.Min(dx, dz)),
                    Form.Sheet => dx >= dz ? (dx / 2.0, 0.2) : (0.2, dz / 2.0),
                    _ => (0.3, 0.3),
                };
                m = (bodies.Count, at.Page, hx, hz, dy * 256.0 * S);
                made[kind] = m;
                bodies.Add(body.Write());
                log.Add($"block {kind.Block} of scene {kind.Scene}: a {kind.Form} of {mesh.Faces.Count} faces, {mesh.Points.Count} points, y {mesh.Points.Min(v => v.Y):0}..{mesh.Points.Max(v => v.Y):0}, x {mesh.Points.Min(v => v.X):0}..{mesh.Points.Max(v => v.X):0}, its picture {picture.Width} x {picture.Height} on page {at.Page}");
            }
            int wx = (int)Math.Round((PolarTerrain.IslandX(placing.X) + dx * S / 2.0) * 512), wz = (int)Math.Round((PolarTerrain.IslandZ(placing.Z) + dz * S / 2.0) * 512);
            var cube = island.CubeAt(wx / IslandFile.CubeSize, wz / IslandFile.CubeSize);
            if (cube is null || cube.Decors.Count >= IslandDecors.MaxPerCube) { log.Add($"block {kind.Block} at ({placing.X},{placing.Z}): no room in its cube"); continue; }
            int lx = wx % IslandFile.CubeSize, lz = wz % IslandFile.CubeSize, foot = PolarTerrain.LayerY(placing.Y - 1);
            var decor = IslandDecors.Blank(m.Body | (m.Page << IslandFile.DecorPageShift), lx, foot, lz, placing.Beta);
            decor.XMin = lx - (int)Math.Round(m.HalfX * 512 * S); decor.XMax = lx + (int)Math.Round(m.HalfX * 512 * S);
            decor.ZMin = lz - (int)Math.Round(m.HalfZ * 512 * S); decor.ZMax = lz + (int)Math.Round(m.HalfZ * 512 * S);
            // (what stands in the water down to the sea: held up by it, IslandFloaters, not a thing in the air the track's build takes away)
            decor.YMin = kind.Water > 0 ? 0 : foot; decor.YMax = foot + (int)m.Tall;
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
