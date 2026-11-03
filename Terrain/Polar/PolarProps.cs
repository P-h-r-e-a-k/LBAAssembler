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
    // Lathe: rings measured from the picture by hand, where a fit can't see what LBA1's view hides: a pine's tiers overlap, so the rings fitted ran straight past the creases, a
    // smooth spinning top from any lower angle. Bench, Deck, Bin and Logs the same, by their own measures below.
    public enum Form { Round, Box, Sheet, Cross, Mound, Lathe, Bench, Deck, Bin, Logs }

    // What is made this way: a block of a scene, its form, how wide a Round is along x and z, how many of its layers are its own (another block sits on the rest), and Deep:
    // at the fortress's ground's level but over the pit's wall, no ground left under it (what stands lower has none anyway, what stands on the ground keeps it).
    // (Cut: a Box only this many cells of its block long, the rest of the picture unused. Water: its foot stands in water drawn in the block, not to be fitted: the rings there the
    // ones above it, on down to the island's sea. Solid: a Box with the picture filled out, not see-through. Fit: its box drawn in to the corner the picture has. Share: how much of
    // a ring has to be on the picture, for a thing that isn't one round shape. Skirt: on down past its foot, for a thing on the shore)
    public sealed record Kind(int Scene, int Block, Form Form, double WideX = 1, double WideZ = 1, int Layers = 99, bool Deep = false, int Cut = 0, int Water = 0,
        bool Solid = false, bool Fit = false, double Share = 0, bool Skirt = false, bool NoGreen = false);
    public static readonly Kind[] Kinds =
    {
        new(93, 112, Form.Lathe), new(100, 112, Form.Lathe),   // the pines
        new(93, 207, Form.Lathe),                              // the dying ones
        new(93, 115, Form.Round, 0.42, 1),                     // the runic stone: broad across z, its carved face to +x
        new(93, 104, Form.Lathe),                              // the burst barrels
        new(93, 106, Form.Round),                              // the tank
        new(93, 49, Form.Bin), new(100, 49, Form.Bin),         // the fire baskets
        new(100, 247, Form.Round),                             // the rock huts
        new(100, 234, Form.Logs, Deep: true, Water: 1), new(100, 238, Form.Round), new(100, 239, Form.Round),
        new(100, 142, Form.Deck, Deep: true, Water: 1),        // the dock's planks
        new(93, 105, Form.Box), new(93, 172, Form.Box, Solid: true, Fit: true), new(93, 116, Form.Bench), new(93, 186, Form.Bench), new(93, 229, Form.Box),
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
    private const double SkirtRock = 0.5;
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

        // A face of those corners, textured by the places given, one each
        public void FaceAs((double X, double Y, double Z)[] corners, (double X, double Y, double Z)[] places)
        {
            var i = 0;
            Face(_ => places[i++], corners);
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
        // (a Skirt's stretch down past the foot textured from the rock, not the picture's foot, its grass, in stripes down to the sea)
        (double, double, double) SkirtSeen((double X, double Y, double Z) w) => Seen((w.X, SkirtRock, w.Z));
        for (var k = 0; k + 1 < rings.Count; k++)
            for (var j = 0; j < Sides; j++)
                mesh.Face(kind.Skirt && k == 0 ? SkirtSeen : Seen, At(k, j), At(k + 1, j), At(k + 1, (j + 1) % Sides), At(k, (j + 1) % Sides));
        var last = rings[^1];
        var apex = (mesh.Cx + last.Slide, last.Y + height * 0.6 / count, mesh.Cz - last.Slide);
        for (var j = 0; j < Sides; j++) mesh.Face(Seen, At(rings.Count - 1, j), apex, At(rings.Count - 1, (j + 1) % Sides));
    }

    // ---- things measured from their pictures --------------------------------------------------------------------------------------------------------------------

    // A ring of a lathe: its height and size in layers and cells, and which ring of the picture textures it (a trunk's hidden top the trunk below it; a tier's underside the dark
    // crease along its rim). From LBA1's angle a ring shows the picture wherever it stands, so what is measured is what LBA1's view leaves open: the rims come from the creases
    // down the middle of the picture (a rim of size r at height y crosses it at y - 1.131 r) and from the outline's width (r = half width / 33.94).
    private readonly record struct Turn(double Y, double R, double SeenY, double SeenR, double Slide = 0, double SeenSlide = 0, double Torn = 0)
    {
        public Turn(double y, double r) : this(y, r, y, r) { }
    }

    // A torn rim (Turn.Torn, layers): on the side LBA1's camera sees, the rim ripped down into a V, and the metal either side of it curled up into points (the burst barrel's,
    // its picture's; a level rim had the V only painted on, 2026-11-03). `off`: how far round from the near side (radians).
    // (chosen by polarpreview against the picture, five tries: the V half the barrel deep, facing LBA1's camera -- 1.05 round from the lathe's first side -- its points curled up
    // either side, the left one dark against the inside as in the picture)
    private const double TornV = 0.8, TornCurl = 1.7, TornCurlUp = 0.6, TornFacing = 1.05;
    private static double TornAt(double torn, double off)
    {
        if (torn == 0) return 0;
        if (off < TornV) return -torn * (1 - off / TornV);
        if (off < TornCurl) return torn * TornCurlUp * Math.Sin(Math.PI * (off - TornV) / (TornCurl - TornV));
        return 0;
    }

    private static readonly (double Along, double Size)[] TreeDome = { (0.3, 0.97), (0.65, 0.86), (1.0, 0.66) };
    // (a tier curving in under its rim, not a flat plate: flat, they were mushrooms on a stalk. The lowest goes furthest, as far as the outline's foot lets it: y - 1.131 r over it)
    private const double TierCrease = 0.03, TierUnder = 0.38, LowestUnder = 0.74, UnderHalfway = 0.5, UnderFilled = 0.42;

    // A tree: its flared trunk, then tiers, each a dome off a rim that overhangs the next one down, then its top (which stops short of the picture's top behind it: a ring of size r
    // shows up to y + 1.131 r). `seenTop`: how high the picture shows bare trunk and nothing else. `slide`: how far it leans there, across the picture
    private static List<Turn> Tree((double Y, double R)[] trunk, double seenTop, (double Y, double R)[] tiers, (double Y, double R)[] top, Func<double, double> slide)
    {
        Turn Plain(double y, double r) => new(y, r, y, r, slide(y), slide(y));
        Turn Seen(double y, double r, double seenY, double seenR) => new(y, r, seenY, seenR, slide(y), slide(seenY));
        var turns = trunk.Select(t => t.Y <= seenTop ? Plain(t.Y, t.R) : Seen(t.Y, t.R, seenTop, t.R)).ToList();
        for (var i = 0; i < tiers.Length; i++)
        {
            var (y, r) = tiers[i];
            var neck = turns[^1];
            var under = i == 0 ? LowestUnder : TierUnder;
            // (up to under the rim, what was below it; then its underside, all the crease)
            turns.Add(Seen(y - under, neck.R, neck.SeenY, neck.SeenR));
            turns.Add(Seen(y - under, neck.R, y - TierCrease, r));
            turns.Add(Seen(y - under * UnderHalfway, neck.R + (r - neck.R) * UnderFilled, y - TierCrease, r));
            turns.Add(Seen(y - TierCrease, r * 0.96, y - TierCrease, r));
            turns.Add(Plain(y, r));
            if (i + 1 == tiers.Length) break;
            var dome = tiers[i + 1].Y - TierUnder - y;
            foreach (var (along, size) in TreeDome) turns.Add(Plain(y + dome * along, r * size));
        }
        turns.AddRange(top.Select(t => Plain(t.Y, t.R)));
        return turns;
    }

    private static List<Turn> Pine() => Tree(new[] { (0.0, 0.70), (0.15, 0.62), (0.4, 0.40), (0.8, 0.28), (1.35, 0.24) }, 1.35,
        new[] { (2.94, 1.27), (4.87, 1.12), (6.56, 0.85), (7.97, 0.50) },
        new[] { (8.2, 0.49), (8.45, 0.40), (8.7, 0.18), (8.93, 0.0) }, _ => 0);

    // (the middle of the outline, row by row, over the 48 pixels a slide of one cell moves it)
    private static readonly (double Y, double Slide)[] DyingLean =
        { (0, 0.07), (2.6, 0.07), (3.4, 0.04), (4.2, 0.02), (5.0, -0.05), (5.8, -0.09), (6.6, -0.29), (7.4, -0.43), (8.2, -0.52), (8.72, -0.56) };

    private static List<Turn> Dying() => Tree(new[] { (0.0, 0.62), (0.2, 0.55), (0.5, 0.42), (0.9, 0.30) }, 0.6,
        new[] { (3.05, 1.25), (4.85, 0.97), (6.55, 0.95), (7.85, 0.62) },
        new[] { (8.1, 0.50), (8.3, 0.34), (8.5, 0.16), (8.72, 0.0) }, Between(DyingLean));

    private static Func<double, double> Between((double Y, double Slide)[] table) => y =>
    {
        for (var i = 0; i + 1 < table.Length; i++)
            if (y <= table[i + 1].Y) return table[i].Slide + (table[i + 1].Slide - table[i].Slide) * Math.Clamp((y - table[i].Y) / (table[i + 1].Y - table[i].Y), 0, 1);
        return table[^1].Slide;
    };

    // The burst barrel: hooped, open at the top, down into the dark (a fit closed it with a hood of the dark inside)
    private const double BarrelDarkY = 2.4;
    private static List<Turn> Barrel() => new List<Turn>
    {
        new(0, 0.35), new(0.27, 0.57), new(0.67, 0.53), new(1.07, 0.57), new(1.47, 0.53), new(1.87, 0.57), new(2.25, 0.54), new(2.58, 0.55),
        new(2.62, 0.50, BarrelDarkY, 0), new(1.2, 0.46, BarrelDarkY, 0), new(1.2, 0, BarrelDarkY, 0),
    }.Select((t, k) => k is 7 or 8 ? t with { Torn = BarrelTorn } : t).ToList();
    // (how far down the V is torn at its point: about half the barrel, as LBA1's picture)
    private const double BarrelTorn = 1.15;

    private static List<Turn> Turned(int block) => block switch { 207 => Dying(), 104 => Barrel(), _ => Pine() };

    // Round from (x, z): the rings over one another, closed at the top. Past NearAngle round from LBA1's camera, the far side, as MakeRound's
    private static void Lathe(Mesh mesh, double x, double z, IReadOnlyList<Turn> turns, int sides, double seenBelow = double.MinValue, double nearest = NearAngle)
    {
        (double, double, double) At(int k, int j)
        {
            var angle = Math.PI / 4 + j * 2 * Math.PI / sides;
            var torn = TornAt(turns[k].Torn, Math.Abs(Math.IEEERemainder(j * 2 * Math.PI / sides - TornFacing, 2 * Math.PI)));
            return (x + turns[k].Slide + turns[k].R * Math.Cos(angle), turns[k].Y + torn, z - turns[k].Slide + turns[k].R * Math.Sin(angle));
        }
        (double X, double Y, double Z) SeenOf(int k, int j)
        {
            var t = turns[k];
            var round = Math.IEEERemainder(j * 2 * Math.PI / sides, 2 * Math.PI);
            var off = Math.Abs(round);
            var near = off <= nearest ? off : FarAngle + (nearest - FarAngle) * (Math.PI - off) / (Math.PI - nearest);
            var angle = Math.PI / 4 + Math.Sign(round) * near;
            return (x + t.SeenSlide + t.SeenR * Math.Cos(angle), Math.Max(t.SeenY, seenBelow), z - t.SeenSlide + t.SeenR * Math.Sin(angle));
        }
        for (var k = 0; k + 1 < turns.Count; k++)
        {
            // (the same ring with another texture: the next faces start from there, nothing between)
            if (turns[k].Y == turns[k + 1].Y && turns[k].R == turns[k + 1].R) continue;
            for (var j = 0; j < sides; j++)
                mesh.FaceAs(new[] { At(k, j), At(k + 1, j), At(k + 1, (j + 1) % sides), At(k, (j + 1) % sides) },
                            new[] { SeenOf(k, j), SeenOf(k + 1, j), SeenOf(k + 1, (j + 1) % sides), SeenOf(k, (j + 1) % sides) });
        }
        var last = turns.Count - 1;
        if (turns[last].R <= 0) return;
        for (var j = 0; j < sides; j++)
            mesh.FaceAs(new[] { At(last, j), (x + turns[last].Slide, turns[last].Y, z - turns[last].Slide), At(last, (j + 1) % sides) },
                        new[] { SeenOf(last, j), (x + turns[last].SeenSlide, turns[last].SeenY, z - turns[last].SeenSlide), SeenOf(last, (j + 1) % sides) });
    }

    // The fire basket: a hooped barrel, and out of its top a fan of black blades, each a real curved spike with a face each way (rings textured with the picture's holes were
    // right from LBA1's side and a cage from the others). A blade anywhere round is textured as the one at the same place on the near side, mirrored past NearAngle like a lathe's
    private static void MakeBin(Mesh mesh)
    {
        Lathe(mesh, mesh.Cx, mesh.Cz, new Turn[] { new(0, 0.38), new(0.3, 0.40), new(0.85, 0.40), new(1.4, 0.40), new(1.62, 0.38), new(1.66, 0.0) }, BinSides);
        (double, double, double) Place(double r, double y, double angle) => (mesh.Cx + r * Math.Cos(angle), y, mesh.Cz + r * Math.Sin(angle));
        for (var b = 0; b < BinBlades; b++)
        {
            var round = b * 2 * Math.PI / BinBlades + Math.PI / BinBlades;
            var seen = Math.IEEERemainder(round, 2 * Math.PI);
            var off = Math.Abs(seen);
            var near = Math.Sign(seen) * (off <= NearAngle ? off : FarAngle + (NearAngle - FarAngle) * (Math.PI - off) / (Math.PI - NearAngle));
            for (var k = 0; k + 1 < BinBlade.Length; k++)
            {
                var (r0, y0, w0) = BinBlade[k];
                var (r1, y1, w1) = BinBlade[k + 1];
                double a = Math.PI / 4 + round, s = Math.PI / 4 + near;
                var corners = new[] { Place(r0, y0, a - w0), Place(r0, y0, a + w0), Place(r1, y1, a + w1), Place(r1, y1, a - w1) };
                var places = new[] { Place(r0, y0, s - w0), Place(r0, y0, s + w0), Place(r1, y1, s + w1), Place(r1, y1, s - w1) };
                mesh.FaceAs(corners, places);
                mesh.FaceAs(corners.Reverse().ToArray(), places.Reverse().ToArray());
            }
        }
    }

    // A length of timber along z, `across` its corners (x, y) anticlockwise, each face textured where it stands; its far end, which LBA1 doesn't show, as its near one
    private static void Plank(Mesh mesh, (double X, double Y)[] across, double z0, double z1)
    {
        static (double, double, double) Same((double X, double Y, double Z) c) => c;
        var steps = Math.Max(1, (int)Math.Ceiling(z1 - z0));
        for (var i = 0; i < across.Length; i++)
        {
            var (a, b) = (across[i], across[(i + 1) % across.Length]);
            mesh.Patch(1, steps, (u, v) => (a.X + (b.X - a.X) * u, a.Y + (b.Y - a.Y) * u, z0 + (z1 - z0) * v), Same);
        }
        mesh.Face(Same, across.Select(c => (c.X, c.Y, z1)).ToArray());
        mesh.Face(c => (c.X, c.Y, z1), across.Reverse().Select(c => (c.X, c.Y, z0)).ToArray());
    }

    private sealed record BenchShape((double X, double Y)[] Seat, (double X, double Y)[] Back, (double X, double Y)[] Leg, double Z0, double Z1, double[] Legs, double LegThick);
    private static readonly Dictionary<int, BenchShape> Benches = new()
    {
        [116] = new(new[] { (0.62, 0.92), (1.65, 0.92), (1.65, 1.04), (0.62, 1.10) }, new[] { (0.50, 1.10), (0.62, 1.10), (0.47, 2.13), (0.35, 2.13) },
                    new[] { (1.25, 0.0), (1.50, 0.0), (1.50, 0.92), (0.70, 0.92) }, 0.15, 2.85, new[] { 0.45, 2.68 }, 0.09),
        // (the same bench from behind: the seat at the far side, the back's dark rear facing LBA1's camera)
        [186] = new(new[] { (0.28, 0.78), (1.30, 0.85), (1.30, 0.97), (0.28, 0.86) }, new[] { (1.25, 0.97), (1.37, 0.97), (1.70, 1.90), (1.58, 1.90) },
                    new[] { (0.60, 0.0), (1.50, 0.0), (1.25, 0.85), (0.95, 0.80) }, 0.05, 3.0, new[] { 0.45, 2.68 }, 0.09),
    };

    private static void MakeBench(Mesh mesh, BenchShape s)
    {
        Plank(mesh, s.Seat, s.Z0, s.Z1);
        Plank(mesh, s.Back, s.Z0, s.Z1);
        foreach (var z in s.Legs) Plank(mesh, s.Leg, z, z + s.LegThick);
    }

    // The dock: a slab of planks as thin as the picture has it, on posts down to the sea (LBA1's stands on nothing to see, a layer over its water; the sea here is two down)
    private static void MakeDeck(Mesh mesh, int dx, int dz)
    {
        Plank(mesh, new[] { (0.0, DeckTop - DeckThick), ((double)dx, DeckTop - DeckThick), ((double)dx, DeckTop), (0.0, DeckTop) }, 0, dz);
        foreach (var x in new[] { DeckPostIn, dx - DeckPostIn })
            for (var z = 0.75; z < dz; z += 1.5)
                Lathe(mesh, x, z, new Turn[] { new(-WaterBelow, DeckPost, DeckTop, DeckPost), new(DeckTop - DeckThick, DeckPost, DeckTop, DeckPost) }, DeckPostSides);
    }
    private const double DeckTop = 1, DeckThick = 0.25, DeckPost = 0.14, DeckPostIn = 0.35;
    private const int DeckPostSides = 6, BinSides = 12, BinBlades = 13, LogSides = 10;
    // (along a blade of the fan: out from the barrel's rim, how high, and half its width as an angle)
    private static readonly (double R, double Y, double Width)[] BinBlade = { (0.34, 1.56, 0.20), (0.52, 2.0, 0.17), (0.66, 2.35, 0.12), (0.74, 2.62, 0.015) };

    // The dock's end post: two logs, a tall one behind a short one (their feet in the picture's water, so textured from above it; a log is so few pixels wide that its dark outline
    // starts well in, a stripe down its side from behind if taken as far round as a tree's)
    private const double LogNear = 55 * Math.PI / 180;
    private static void MakeLogs(Mesh mesh)
    {
        const double AboveWater = 0.9;
        Lathe(mesh, mesh.Cx - 0.175, mesh.Cz + 0.075, new Turn[] { new(-WaterBelow, 0.25), new(AboveWater, 0.25), new(3.73, 0.25), new(3.73, 0.0) }, LogSides, AboveWater, LogNear);
        Lathe(mesh, mesh.Cx + 0.195, mesh.Cz + 0.005, new Turn[] { new(-WaterBelow, 0.25), new(AboveWater, 0.25), new(3.0, 0.25), new(3.0, 0.0) }, LogSides, AboveWater, LogNear);
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
        // (and down past its foot on both sides: on the shore it stands over the sea. Textured from the rock halfway up it: the picture's foot is its grass, drawn down the
        // sides in stripes)
        (double, double, double) Skirt((double X, double Y, double Z) w)
        {
            var a = alongX ? w.X : w.Z;
            var y = tops[Math.Clamp((int)Math.Round(a / length * steps), 0, steps)] * SkirtRock;
            return alongX ? (a, y, mesh.Cz + 0.5 * Math.Cos(Math.Asin(SkirtRock))) : (mesh.Cx + 0.5 * Math.Cos(Math.Asin(SkirtRock)), y, a);
        }
        for (var i = 0; i < steps; i++)
        {
            double a0 = length * i / (double)steps, a1 = length * (i + 1) / (double)steps;
            if (alongX)
            {
                mesh.Face(Skirt, (a0, -WaterBelow, mesh.Cz + 0.5), (a1, -WaterBelow, mesh.Cz + 0.5), (a1, 0, mesh.Cz + 0.5), (a0, 0, mesh.Cz + 0.5));
                mesh.Face(Skirt, (a0, -WaterBelow, mesh.Cz - 0.5), (a0, 0, mesh.Cz - 0.5), (a1, 0, mesh.Cz - 0.5), (a1, -WaterBelow, mesh.Cz - 0.5));
            }
            else
            {
                mesh.Face(Skirt, (mesh.Cx + 0.5, -WaterBelow, a0), (mesh.Cx + 0.5, 0, a0), (mesh.Cx + 0.5, 0, a1), (mesh.Cx + 0.5, -WaterBelow, a1));
                mesh.Face(Skirt, (mesh.Cx - 0.5, -WaterBelow, a0), (mesh.Cx - 0.5, -WaterBelow, a1), (mesh.Cx - 0.5, 0, a1), (mesh.Cx - 0.5, 0, a0));
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

    // A kind's body as the game gets it, and the picture its faces are textured from
    public static (Picture Shown, List<System.Numerics.Vector3> Points, List<LbaBodyStudio.Face> Faces) Shape(Lba1Game game, Kind kind)
    {
        var library = game.ReadBlocks(kind.Scene);
        var (dx, dy, dz) = Size(library, kind.Block);
        dy = Math.Min(dy, kind.Layers);
        var (wholeX, wholeZ) = (dx, dz);
        if (kind.Cut > 0) { dx = Math.Min(dx, kind.Cut); dz = Math.Min(dz, kind.Cut); }
        var picture = Roomy(Draw(game, library, kind.Block, dy), wholeX, dy, wholeZ);
        // (rings fitted to the outline, then the picture filled out past it)
        var round = kind.Solid || kind.Form is Form.Round or Form.Mound or Form.Lathe or Form.Bench or Form.Deck or Form.Bin or Form.Logs;
        var mesh = new Mesh(picture, dx / 2.0, dz / 2.0) { Fill = round ? Solid : SeeThrough };
        switch (kind.Form)
        {
            case Form.Round: MakeRound(mesh, kind, dy); break;
            case Form.Mound: MakeMound(mesh, dx, dy, dz); break;
            case Form.Box: MakeBox(mesh, dx, dy, dz, kind.Fit); break;
            case Form.Sheet: MakeSheet(mesh, dx >= dz); break;
            case Form.Lathe: Lathe(mesh, mesh.Cx, mesh.Cz, Turned(kind.Block), LatheSides); break;
            case Form.Bench: MakeBench(mesh, Benches[kind.Block]); break;
            case Form.Deck: MakeDeck(mesh, dx, dz); break;
            case Form.Bin: MakeBin(mesh); break;
            case Form.Logs: MakeLogs(mesh); break;
            default: MakeSheet(mesh, true); MakeSheet(mesh, false); break;
        }
        // (a wall's grass, on its top edge, not spread down its face)
        var shown = round ? Filled(kind.NoGreen ? NoGrass(picture, game.Palette) : picture) : picture;
        return (shown, mesh.Points, mesh.Faces);
    }
    private const int LatheSides = 16;

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
            if (kind.Cut > 0) { dx = Math.Min(dx, kind.Cut); dz = Math.Min(dz, kind.Cut); }
            if (!made.TryGetValue(kind, out var m))
            {
                var (shown, points, faces) = Shape(game, kind);
                if (pages.AddPicture(shown.Pixels, shown.Width, shown.Height) is not { } at) { log.Add($"block {kind.Block}: no page for its picture"); continue; }
                var header = new byte[96];
                header[0] = 0x10;
                var body = new LbaBodyStudio.Body
                {
                    Game = 2, Static = true, Lit = false, Header = header, Vertices = points,
                    Bones = new List<LbaBodyStudio.Bone> { new(0, points.Count, 0, -1, new byte[8]) }, Faces = faces, Textures = new[] { 0xFFFF0000u | (uint)(at.Y * 256 + at.X) },
                };
                // (what stops a car: a box's whole box, a fence's line, the trunk of what's round)
                var (hx, hz) = kind.Form switch
                {
                    Form.Box or Form.Mound or Form.Deck => (dx / 2.0, dz / 2.0),
                    Form.Bench => (0.65, dz / 2.0),
                    Form.Round or Form.Lathe or Form.Bin or Form.Logs => (0.3 * Math.Min(dx, dz), 0.3 * Math.Min(dx, dz)),
                    Form.Sheet => dx >= dz ? (dx / 2.0, 0.2) : (0.2, dz / 2.0),
                    _ => (0.3, 0.3),
                };
                m = (bodies.Count, at.Page, hx, hz, dy * 256.0 * S);
                made[kind] = m;
                bodies.Add(body.Write());
                log.Add($"block {kind.Block} of scene {kind.Scene}: a {kind.Form} of {faces.Count} faces, {points.Count} points, y {points.Min(v => v.Y):0}..{points.Max(v => v.Y):0}, x {points.Min(v => v.X):0}..{points.Max(v => v.X):0}, its picture {shown.Width} x {shown.Height} on page {at.Page}");
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
