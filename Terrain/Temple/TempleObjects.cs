using System.Numerics;
using LBAAssembler.Lba1;
using LBAAssembler.Terrain.Palace;
using LBAAssembler.Terrain.Polar;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Temple;

// The Temple of Bú's cells as LBA2 island objects. LBA1 draws a cell's brick as an isometric picture of its top and of the two sides its
// camera sees (+x, +z); each cell here is a box -- the part of the cell its brick fills (PolarTextures.Sprite.Footprint: a lamp's pole a thin
// one) -- each face of it textured with that face of the brick's picture, unskewed (the -x and -z sides, which LBA1 never draws, with the
// opposite side's), so that from LBA1's own angle the boxes draw the picture LBA1 draws. Where a brick draws nothing on a face -- the gaps
// of a railing, a cobweb's strands, a skull lamp's surroundings -- the face is see-through (an LBA2 "incrusted" texture: its colour 0 is not
// drawn), as LBA1's picture shows what is behind. A face is left out where what is beyond it hides it: a box as wide that is solid there.
//
// Every face is drawn as it is (the palette's normal level: TemplePalette's row 12, LBA1's colours), unlit -- LBA1's bricks have their light
// drawn in. The bodies only draw (their decors' boxes touch nothing, BrickBuilding.NoBoxTop); what is solid is a set of plain boxes of their
// own, the cells merged (Boxes), each a decor with an empty body.
internal static class TempleObjects
{
    // a body's cells: at most this many LBA1 columns each way (split again while it is too big for a body)
    public const int Chunk = 6;
    private const int MaxPoints = 500, MaxFaces = 500;
    // a face the brick draws this much of and more is solid (its few gaps -- the picture's jagged edges -- the nearest drawn pixel's
    // colour); less, see-through where it draws nothing
    public const double Solid = 0.9;
    // a face the brick hardly draws (a wall brick's top, a few pixels of its edge) against a cell beside it: left out
    public const double Sliver = 0.15;
    // a brick that draws less than this of each of its box's faces is a wisp (a cobweb's strands): nothing to bump into
    public const double Wisp = 0.35;
    // the engine's polygon types: a textured face drawn as its texture is, and one whose colour 0 isn't drawn (POLY_TEXTURE_SOLID(_INC))
    private const int TextureSolid = 8, TextureSeeThrough = 12;

    private static uint TileEntry(int x, int y) => 0x1F1F0000u | (uint)(y * 256 + x);

    public sealed record Built(List<byte[]> Bodies, int Decors, int Boxes, int Pages, List<string> Log);

    // How many of the temple's pixels each LBA1 colour is: each brick's picture as often as the temple places it (for its palette).
    public static long[] ColourCounts(Lba1Game game, TempleLayout layout)
    {
        var counts = new long[256];
        foreach (var g in layout.Cells.Values.GroupBy(c => c.Brick))
            foreach (var px in PolarTextures.Sprite.Decode(game.ReadBrick(g.Key)).Pixels)
                if (px >= 0) counts[px] += g.Count();
        return counts;
    }

    // `map`: each LBA1 colour's index in the island's palette (TemplePalette). `firstBody`: the OBL entry the bodies start at; `emptyBody`:
    // an empty body for the boxes (its own entry, made by the caller).
    public static Built Build(Lba1Game game, TempleLayout layout, IslandFile island, TempleFrame frame, int[] map, int firstBody, int emptyBody)
    {
        var log = new List<string>();
        var cells = layout.Cells;
        var sprites = new Dictionary<int, PolarTextures.Sprite>();
        PolarTextures.Sprite Sprite(int brick) => sprites.TryGetValue(brick, out var s) ? s : sprites[brick] = PolarTextures.Sprite.Decode(game.ReadBrick(brick));
        var boxes = new Dictionary<int, PolarTextures.Box>();
        // (LBA1 draws of a brick only the faces its grid leaves to be seen -- a floor's tiles their tops, a wall's bricks the side they face:
        // a brick that draws a face whole is a whole block; one that draws none whole -- a lamp's pole, a rail -- the box its outline fits)
        PolarTextures.Box BoxOf(int brick)
        {
            if (boxes.TryGetValue(brick, out var b)) return b;
            var s = Sprite(brick);
            var whole = Enum.GetValues<PolarTextures.Face>().Any(f => s.Coverage(f) >= Solid);
            return boxes[brick] = whole ? PolarTextures.Box.Full : s.Footprint();
        }

        // each brick's three faces as tiles, in the island's colours (0 where the face is see-through), and whether each is solid
        var faceTiles = new Dictionary<(int Brick, PolarTextures.Face Face), (int[] Tile, bool Solid, double Drawn)>();
        (int[] Tile, bool Solid, double Drawn) TileOf(int brick, PolarTextures.Face face)
        {
            if (faceTiles.TryGetValue((brick, face), out var known)) return known;
            var s = Sprite(brick); var box = BoxOf(brick);
            var (w, h) = PolarTextures.Size(face);
            var tile = new int[w * h];
            var drawn = new List<(int I, int J, int C)>();
            for (var j = 0; j < h; j++)
                for (var i = 0; i < w; i++)
                {
                    var (sx, sy) = PolarTextures.Sprite.Where(face, (i + 0.5) / w, (j + 0.5) / h, box);
                    var c = s.At(sx, sy);
                    tile[j * w + i] = c;
                    if (c >= 0) drawn.Add((i, j, c));
                }
            var share = (double)drawn.Count / tile.Length;
            var solid = share >= Solid;
            for (var k = 0; k < tile.Length; k++)
            {
                if (tile[k] >= 0) { tile[k] = map[tile[k]]; continue; }
                if (!solid || drawn.Count == 0) { tile[k] = 0; continue; }
                int i = k % w, j = k / w;
                tile[k] = map[drawn.MinBy(d => (d.I - i) * (d.I - i) + (d.J - j) * (d.J - j)).C];
            }
            return faceTiles[(brick, face)] = (tile, solid, share);
        }

        // the faces each cell shows: its top, its four sides, and its bottom where there is something under it in its column (an arch's
        // underside, a bridge's) -- each left out where it lies on its cell's side and the box beyond is solid and as wide there
        var under = new HashSet<(int X, int Z)>();
        var lowest = new Dictionary<(int X, int Z), int>();
        foreach (var (x, y, z) in cells.Keys) lowest[(x, z)] = lowest.TryGetValue((x, z), out var l) ? Math.Min(l, y) : y;
        bool Hidden((int X, int Y, int Z) c, int dir)
        {
            var (dx, dy, dz) = Dirs[dir];
            var box = BoxOf(cells[c].Brick);
            var onSide = dir switch { 1 => box.U1 >= 1, 2 => box.U0 <= 0, 3 => box.V1 >= 1, 4 => box.V0 <= 0, _ => true };
            if (!onSide || !cells.TryGetValue((c.X + dx, c.Y + dy, c.Z + dz), out var other)) return false;
            // (the face of the box beyond that lies against this one: solid?)
            if (!TileOf(other.Brick, FaceOf(Opposite(dir))).Solid) return false;
            var nb = BoxOf(other.Brick);
            return dir switch
            {
                0 or 5 => nb.U0 <= box.U0 && nb.U1 >= box.U1 && nb.V0 <= box.V0 && nb.V1 >= box.V1,
                1 => nb.U0 <= 0 && nb.V0 <= box.V0 && nb.V1 >= box.V1,
                2 => nb.U1 >= 1 && nb.V0 <= box.V0 && nb.V1 >= box.V1,
                3 => nb.V0 <= 0 && nb.U0 <= box.U0 && nb.U1 >= box.U1,
                _ => nb.V1 >= 1 && nb.U0 <= box.U0 && nb.U1 >= box.U1,
            };
        }
        var shown = new Dictionary<(int X, int Y, int Z), List<int>>();
        var faceCount = 0;
        foreach (var c in cells.Keys)
        {
            var list = new List<int>();
            for (var d = 0; d < Dirs.Length; d++)
            {
                if (d == 5 && (cells.ContainsKey((c.X, c.Y - 1, c.Z)) || lowest[(c.X, c.Z)] >= c.Y)) continue;
                if (Hidden(c, d)) continue;
                // (a face the brick draws nothing of: nothing to draw; hardly anything, against another cell: nothing to see)
                var drawn = TileOf(cells[c].Brick, FaceOf(d)).Drawn;
                if (drawn <= 0 || drawn < Sliver && cells.ContainsKey((c.X + Dirs[d].Dx, c.Y + Dirs[d].Dy, c.Z + Dirs[d].Dz))) continue;
                list.Add(d);
            }
            if (list.Count > 0) { shown[c] = list; faceCount += list.Count; }
        }
        var shownFaces = shown.SelectMany(s => s.Value.Select(d => (cells[s.Key].Brick, FaceOf(d)))).Distinct().ToList();
        log.Add($"{cells.Count} cells, {shown.Count} with faces to draw ({faceCount} faces); {shownFaces.Count(f => !faceTiles[f].Solid)} of the {shownFaces.Count} faces of bricks drawn see-through");
        var thin = cells.Values.Select(c => c.Brick).Distinct().Count(b => !BoxOf(b).IsFull);
        log.Add($"{thin} of the {cells.Values.Select(c => c.Brick).Distinct().Count()} bricks fill part of their cell (poles, rails, lamps): thinner boxes");

        // the chunks: the cells with faces, by the island's cubes and Chunk x Chunk columns in them
        var chunks = shown.Keys.GroupBy(c => (CubeX: frame.IslandX(c.X) / 64, CubeZ: frame.IslandZ(c.Z) / 64, Cx: Math.DivRem(c.X - frame.FirstColumnOfCubeX(c.X), Chunk).Quotient,
                Cz: Math.DivRem(c.Z - frame.FirstColumnOfCubeZ(c.Z), Chunk).Quotient))
            .OrderBy(g => g.Key.CubeZ).ThenBy(g => g.Key.CubeX).ThenBy(g => g.Key.Cz).ThenBy(g => g.Key.Cx).Select(g => g.ToList()).ToList();

        // the tiles on pages: each tile once a page (the same picture on many bricks' faces once), a chunk's tiles all on one page -- the
        // page being filled, or a new one when they don't fit
        var pages = new PolarTextures.Pages(island.ObjectTexture, PolarTextures.Colours.AsTheyAre(Array.Empty<byte>()), IslandFile.MaxObjectPages);
        var contentKey = new Dictionary<(int Brick, PolarTextures.Face Face), string>();
        string KeyOf(int brick, PolarTextures.Face face)
        {
            if (contentKey.TryGetValue((brick, face), out var k)) return k;
            var t = TileOf(brick, face).Tile;
            return contentKey[(brick, face)] = $"{(int)face}:{Convert.ToBase64String(System.Security.Cryptography.SHA1.HashData(t.Select(v => (byte)v).ToArray()))}";
        }
        {
            var all = shown.SelectMany(s => s.Value.Select(d => (Key: KeyOf(cells[s.Key].Brick, FaceOf(d)), Face: FaceOf(d)))).DistinctBy(t => t.Key).ToList();
            log.Add($"the faces shown: {all.Count} different pictures ({all.Count(t => t.Face == PolarTextures.Face.Top)} tops), " +
                    $"{all.Sum(t => PolarTextures.Pages.HalvesOf(PolarTextures.Size(t.Face).H))} half-slots of the {IslandFile.MaxObjectPages * PolarTextures.Pages.Halves} the pages hold");
        }
        // (each brick's faces on one page -- the page being filled when they fit on it, else the next -- in the order the chunks first use
        // the bricks, so that a chunk's are mostly on one page; a picture several bricks share once on a page; a chunk whose bricks are on
        // more than one page a body for each)
        var onPage = new List<HashSet<string>> { new() };
        var pageOf = new Dictionary<int, int>();
        var filling = 0;
        var brickFaces = shown.GroupBy(s => cells[s.Key].Brick).ToDictionary(g => g.Key, g => g.SelectMany(s => s.Value.Select(FaceOf)).Distinct().OrderBy(f => f).ToList());
        foreach (var chunk in chunks)
            foreach (var brick in chunk.Select(c => cells[c].Brick).Distinct())
            {
                if (pageOf.ContainsKey(brick)) continue;
                var tiles = brickFaces[brick].Select(f => (Key: KeyOf(brick, f), Face: f)).DistinctBy(t => t.Key).ToList();
                // (a half-slot more than they take: a top needs a slot's two halves together)
                if (tiles.Where(t => !onPage[filling].Contains(t.Key)).Sum(t => PolarTextures.Pages.HalvesOf(PolarTextures.Size(t.Face).H)) + 1 > pages.Free(filling))
                {
                    filling++;
                    if (filling >= IslandFile.MaxObjectPages) throw new InvalidOperationException($"The temple's textures take more than {IslandFile.MaxObjectPages} pages.");
                    onPage.Add(new());
                }
                foreach (var t in tiles.Where(t => !onPage[filling].Contains(t.Key)))
                {
                    var (w, h) = PolarTextures.Size(t.Face);
                    if (!pages.Add((t.Key, filling), TileOf(brick, t.Face).Tile, w, h, filling)) throw new InvalidOperationException("A page took fewer tiles than it had room for.");
                    onPage[filling].Add(t.Key);
                }
                pageOf[brick] = filling;
            }
        var placedChunks = chunks.SelectMany(chunk => chunk.GroupBy(c => pageOf[cells[c].Brick]).Select(g => (Cells: g.ToList(), Page: g.Key))).ToList();
        log.Add($"the textures: {contentKey.Values.Distinct().Count()} different brick faces ({faceTiles.Count} faces of bricks) at {PolarTextures.TileSize} pixels to a cell, on {pages.Count} pages");

        // the bodies: each chunk's faces (halved while too big for a body), a drawn-only decor in its cube
        var bodies = new List<byte[]>();
        var decors = 0; var left = 0;
        var work = new Queue<(List<(int X, int Y, int Z)> Cells, int Page)>(placedChunks);
        while (work.Count > 0)
        {
            var (chunk, page) = work.Dequeue();
            var body = Mesh(chunk, shown, cells, frame, key => { var (_, x, y) = pages.Place[(KeyOf(key.Brick, key.Face), page)]; return (x, y); }, BoxOf,
                (brick, face) => TileOf(brick, face).Solid, out var lo, out var hi, out var origin);
            if (body is null)
            {
                var alongX = chunk.Max(c => c.X) - chunk.Min(c => c.X) >= chunk.Max(c => c.Z) - chunk.Min(c => c.Z);
                var mid = (int)Math.Floor(alongX ? (chunk.Min(c => c.X) + chunk.Max(c => c.X)) / 2.0 : (chunk.Min(c => c.Z) + chunk.Max(c => c.Z)) / 2.0);
                var first = chunk.Where(c => (alongX ? c.X : c.Z) <= mid).ToList();
                if (first.Count == 0 || first.Count == chunk.Count)
                {
                    // (one column too big for a body: by layers)
                    var midY = (chunk.Min(c => c.Y) + chunk.Max(c => c.Y)) / 2;
                    first = chunk.Where(c => c.Y <= midY).ToList();
                }
                var firstSet = first.ToHashSet();
                work.Enqueue((first, page));
                work.Enqueue((chunk.Where(c => !firstSet.Contains(c)).ToList(), page));
                continue;
            }
            if (IslandDecors.Locate(island, origin.X, origin.Z) is not { } at || at.Cube.Decors.Count >= IslandDecors.MaxPerCubeOwnEngine) { left++; continue; }
            var d = IslandDecors.Blank(firstBody + bodies.Count | page << IslandFile.DecorPageShift, at.X, (int)Math.Round(origin.Y), at.Z);
            d.XMin = (int)Math.Floor(at.X + lo.X - origin.X); d.XMax = (int)Math.Ceiling(at.X + hi.X - origin.X);
            d.ZMin = (int)Math.Floor(at.Z + lo.Z - origin.Z); d.ZMax = (int)Math.Ceiling(at.Z + hi.Z - origin.Z);
            d.YMin = (int)Math.Floor(lo.Y); d.YMax = BrickBuilding.NoBoxTop;
            at.Cube.Decors.Add(d);
            bodies.Add(body.Write());
            decors++;
        }
        if (left > 0) log.Add($"{left} bodies left out: their cubes full");
        island.ObjectPages.Clear();
        island.ObjectPages.AddRange(pages.List.Skip(1));

        // what is solid: each cell's box (but a wisp's), the cells that fill theirs merged into as few boxes as will do; the steps of a
        // stair (LBA1's slopes, shapes 2-5) a box each step of four
        var full = new HashSet<(int X, int Y, int Z)>();
        var solidBoxes = new List<(double X0, double Y0, double Z0, double X1, double Y1, double Z1)>();
        var wisps = 0;
        foreach (var (c, cell) in cells)
        {
            var box = BoxOf(cell.Brick);
            // (a floor's tile draws only its top, a wall's brick one side: the most it draws of any face)
            var drawn = Math.Max(TileOf(cell.Brick, PolarTextures.Face.Top).Drawn, Math.Max(TileOf(cell.Brick, PolarTextures.Face.SideX).Drawn, TileOf(cell.Brick, PolarTextures.Face.SideZ).Drawn));
            if (drawn < Wisp) { wisps++; continue; }
            if (cell.Shape is >= 2 and <= 5)
            {
                foreach (var step in Steps(c, cell.Shape, frame)) solidBoxes.Add(step);
                continue;
            }
            if (box.IsFull) { full.Add(c); continue; }
            solidBoxes.Add((frame.WorldX(c.X + box.U0), frame.WorldY(c.Y), frame.WorldZ(c.Z + box.V0), frame.WorldX(c.X + box.U1), frame.WorldY(c.Y + 1), frame.WorldZ(c.Z + box.V1)));
        }
        foreach (var (x0, y0, z0, x1, y1, z1) in BrickBuilding.Boxes(full))
            solidBoxes.Add((frame.WorldX(x0), frame.WorldY(y0), frame.WorldZ(z0), frame.WorldX(x1 + 1), frame.WorldY(y1 + 1), frame.WorldZ(z1 + 1)));
        var (placedBoxes, boxesLeft) = BrickBuilding.AddBoxes(island, emptyBody, solidBoxes, IslandDecors.MaxPerCubeOwnEngine, everyCube: true);
        log.Add($"the solid: {solidBoxes.Count} boxes ({wisps} wisps of cells left out), {placedBoxes} placed" + (boxesLeft > 0 ? $", {boxesLeft} copies left out (cubes full)" : ""));
        log.Add($"{decors} bodies drawn; the busiest cube {IslandOps.CubeCells(island).Max(c => c.Item3.Decors.Count)} decors");
        return new Built(bodies, decors, placedBoxes, pages.Count, log);
    }

    // A stair's cell (LBA1's slopes, shapes 2-5) as four steps, each a quarter of the cell along the way up and that much of its layer higher.
    // (LBA1's OBJECT.C ReajustPos: shape 2 rises toward +x, 3 toward +z, 4 toward -z, 5 toward -x; 6-13, two slopes meeting, are left whole)
    private static IEnumerable<(double, double, double, double, double, double)> Steps((int X, int Y, int Z) c, int shape, TempleFrame frame)
    {
        for (var k = 0; k < 4; k++)
        {
            var top = frame.WorldY(c.Y + (k + 1) / 4.0);
            var a = k / 4.0;
            yield return shape switch
            {
                2 => (frame.WorldX(c.X + a), frame.WorldY(c.Y), frame.WorldZ(c.Z), frame.WorldX(c.X + 1), top, frame.WorldZ(c.Z + 1)),
                3 => (frame.WorldX(c.X), frame.WorldY(c.Y), frame.WorldZ(c.Z + a), frame.WorldX(c.X + 1), top, frame.WorldZ(c.Z + 1)),
                4 => (frame.WorldX(c.X), frame.WorldY(c.Y), frame.WorldZ(c.Z), frame.WorldX(c.X + 1), top, frame.WorldZ(c.Z + 1 - a)),
                _ => (frame.WorldX(c.X), frame.WorldY(c.Y), frame.WorldZ(c.Z), frame.WorldX(c.X + 1 - a), top, frame.WorldZ(c.Z + 1)),
            };
        }
    }

    // the faces a box shows: its top, its four sides, its bottom
    private static readonly (int Dx, int Dy, int Dz)[] Dirs = { (0, 1, 0), (1, 0, 0), (-1, 0, 0), (0, 0, 1), (0, 0, -1), (0, -1, 0) };
    private static int Opposite(int dir) => dir switch { 0 => 5, 1 => 2, 2 => 1, 3 => 4, 4 => 3, _ => 0 };
    private static PolarTextures.Face FaceOf(int dir) => dir switch { 0 or 5 => PolarTextures.Face.Top, 1 or 2 => PolarTextures.Face.SideX, _ => PolarTextures.Face.SideZ };

    // A chunk's body: its faces as textured quads, its points relative to its origin (the middle of what it covers, island world units).
    // Null when it is too big for a body. `lo`, `hi`: its points' extent (world units).
    private static Body? Mesh(List<(int X, int Y, int Z)> chunk, Dictionary<(int X, int Y, int Z), List<int>> shown, Dictionary<(int X, int Y, int Z), PolarLayout.Cell> cells,
        TempleFrame frame, Func<(int Brick, PolarTextures.Face Face), (int X, int Y)> place, Func<int, PolarTextures.Box> boxOf, Func<int, PolarTextures.Face, bool> solid,
        out Vector3 lo, out Vector3 hi, out Vector3 origin)
    {
        var faces = new List<(Vector3[] Quad, (double U, double V)[] Uv, int Dir, int Brick)>();
        lo = new Vector3(float.MaxValue); hi = new Vector3(float.MinValue);
        foreach (var c in chunk)
        {
            var brick = cells[c].Brick;
            var b = boxOf(brick);
            double x = c.X + b.U0, xe = c.X + b.U1, z = c.Z + b.V0, ze = c.Z + b.V1;
            int y0 = c.Y, y1 = c.Y + 1;
            foreach (var dir in shown[c])
            {
                // corners in order (outward winding) with their (u, v) in the tile: the top's u along x, v along z; a side's u along it, v down
                (double X, int Y, double Z, double U, double V)[] corners = dir switch
                {
                    0 => new[] { (x, y1, ze, 0.0, 1.0), (xe, y1, ze, 1.0, 1.0), (xe, y1, z, 1.0, 0.0), (x, y1, z, 0.0, 0.0) },
                    1 => new[] { (xe, y1, z, 0.0, 0.0), (xe, y1, ze, 1.0, 0.0), (xe, y0, ze, 1.0, 1.0), (xe, y0, z, 0.0, 1.0) },
                    2 => new[] { (x, y0, z, 0.0, 1.0), (x, y0, ze, 1.0, 1.0), (x, y1, ze, 1.0, 0.0), (x, y1, z, 0.0, 0.0) },
                    3 => new[] { (x, y0, ze, 0.0, 1.0), (xe, y0, ze, 1.0, 1.0), (xe, y1, ze, 1.0, 0.0), (x, y1, ze, 0.0, 0.0) },
                    4 => new[] { (x, y1, z, 0.0, 0.0), (xe, y1, z, 1.0, 0.0), (xe, y0, z, 1.0, 1.0), (x, y0, z, 0.0, 1.0) },
                    _ => new[] { (x, y0, z, 0.0, 0.0), (xe, y0, z, 1.0, 0.0), (xe, y0, ze, 1.0, 1.0), (x, y0, ze, 0.0, 1.0) },
                };
                var quad = corners.Select(k => new Vector3((float)frame.WorldX(k.X), (float)frame.WorldY(k.Y), (float)frame.WorldZ(k.Z))).ToArray();
                foreach (var p in quad) { lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
                faces.Add((quad, corners.Select(k => (k.U, k.V)).ToArray(), dir, brick));
            }
        }
        origin = new Vector3(MathF.Round((lo.X + hi.X) / 2), MathF.Round((lo.Y + hi.Y) / 2), MathF.Round((lo.Z + hi.Z) / 2));
        if (hi.X - lo.X > 64000 || hi.Y - lo.Y > 64000 || hi.Z - lo.Z > 64000) return null;
        var o = origin;
        var points = new List<Vector3>();
        var index = new Dictionary<(int, int, int), int>();
        int P(Vector3 v)
        {
            var key = ((int)Math.Round(v.X), (int)Math.Round(v.Y), (int)Math.Round(v.Z));
            if (index.TryGetValue(key, out var i)) return i;
            points.Add(new Vector3(key.Item1 - o.X, key.Item2 - o.Y, key.Item3 - o.Z));
            return index[key] = points.Count - 1;
        }
        var bodyFaces = new List<Face>();
        var handles = new Dictionary<(int, int), int>();
        var textures = new List<uint>();
        foreach (var (quad, uvs, dir, brick) in faces)
        {
            var face = FaceOf(dir);
            var (tx, ty) = place((brick, face));
            var (w, h) = PolarTextures.Size(face);
            var ids = quad.Select(P).ToArray();
            if (!handles.TryGetValue((tx, ty), out var handle)) { handle = handles[(tx, ty)] = textures.Count; textures.Add(TileEntry(tx, ty)); }
            var uv = uvs.SelectMany(k => new[] { (int)Math.Round((0.4 + k.U * (w - 0.8)) * 256), (int)Math.Round((0.4 + k.V * (h - 0.8)) * 256) }).ToArray();
            // (wound so the plain cross product points the way it faces: the engine's front faces)
            var n = Vector3.Cross(quad[1] - quad[0], quad[2] - quad[0]);
            var (dx, dy, dz) = Dirs[dir];
            if (Vector3.Dot(n, new Vector3(dx, dy, dz)) < 0)
            {
                ids = ids.Reverse().ToArray();
                uv = Enumerable.Range(0, 4).Reverse().SelectMany(i => new[] { uv[i * 2], uv[i * 2 + 1] }).ToArray();
            }
            bodyFaces.Add(new Face(ids, 0, Texture: new FaceTexture(handle, uv), Lba2Type: solid(brick, face) ? TextureSolid : TextureSeeThrough));
        }
        if (points.Count > MaxPoints || bodyFaces.Count > MaxFaces) return null;
        if (points.Any(p => Math.Abs(p.X) > 32000 || Math.Abs(p.Y) > 32000 || Math.Abs(p.Z) > 32000)) return null;
        // (the header's first word: the body format's version, 0x10 as every retail body has it, and the close flag -- cut at the near plane
        // close in front of the camera, which drives among these walls: BrickBuilding.CloseFlag)
        var header = new byte[96];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header, 0x10 | BrickBuilding.CloseFlag);
        return new Body
        {
            Game = 2, Static = true, Lit = false, Header = header,
            Vertices = points,
            Bones = new List<Bone> { new(0, points.Count, 0, -1, new byte[8]) },
            Faces = bodyFaces,
            Textures = textures.ToArray(),
        };
    }
}
