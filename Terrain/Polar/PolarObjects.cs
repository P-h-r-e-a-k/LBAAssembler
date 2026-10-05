using System.Numerics;
using LBAAssembler.Lba1;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Polar;

// Polar Island's objects as LBA2 island objects (decors: a body of the island's OBL each, with its collision box). LBA1's objects are bricks
// in cells like its ground (PolarTerrain says which cells are ground); what is left -- the huts, barrels and pipes, the concrete walls, the
// fences and posts, the wood, the plateau's pillars -- is built as blocks: each piece of touching object cells (split into chunks of at most
// Chunk x Chunk columns, which never cross a cube's edge) a body of boxes, a box a cell -- the part of the cell its brick fills, as the
// brick's outline shows it (PolarTextures.Sprite.Footprint: a post a thin one) -- each of its faces that something else doesn't hide
// textured with the brick's own face (PolarTextures) -- the sides LBA1 never drew (-x, -z) with the opposite side's.
internal static class PolarObjects
{
    public const int Chunk = 8;
    // a brick this much lone pixels is mist
    public const double MistScattered = 0.4;
    // the engine's limits for a body (550 points, 550 polygons), with some room
    private const int MaxPoints = 500, MaxFaces = 500;
    // A face's texture: its tile's entry in the body's texture table -- the tile's place in the page (row * 256 + column) and a repeat mask
    // of 32 pixels each way (0x1F1F) -- and UVs inside the tile. (Not one entry for the whole page, 0xFFFF0000: the engine takes that for
    // the placeholder some retail bodies have and draws an unlit textured polygon in its flat colour instead -- AFF_OBJ.CPP.) A body's
    // textures are all on one page of the island's object pages, the decor's (IslandFile.DecorPageShift).
    private static uint TileEntry(int x, int y) => 0x1F1F0000u | (uint)(y * 256 + x);

    public sealed record Built(List<byte[]> Bodies, int Decors, List<string> Log);

    public static Built Build(Lba1Game game, PolarLayout layout, IReadOnlyDictionary<(int X, int Z), PolarTerrain.Column> columns, IReadOnlySet<(int X, int Z)> rocks,
        IslandFile island, int offsetX, int offsetZ, PolarTextures.Colours colours)
    {
        var log = new List<string>();
        // the object cells: not ground, above their column's ground, and not a wisp of mist (dithered: Sprite.Scattered); and the rocks in
        // the water (PolarTerrain.RockCells), whole, from the water's surface up
        var mist = new Dictionary<int, bool>();
        bool Mist(int brick) => mist.TryGetValue(brick, out var m) ? m : mist[brick] = PolarTextures.Sprite.Decode(game.ReadBrick(brick)).Scattered >= MistScattered;
        bool Rock(KeyValuePair<(int X, int Y, int Z), PolarLayout.Cell> c) => rocks.Contains((c.Key.X, c.Key.Z)) && !c.Value.Water && c.Key.Y >= 1;
        var cells = layout.Cells.Where(c => (Rock(c) || !PolarTerrain.IsGround(game, c.Value) && (!columns.TryGetValue((c.Key.X, c.Key.Z), out var col) || c.Key.Y > col.Top)) && !Mist(c.Value.Brick))
            .ToDictionary(c => c.Key, c => c.Value);
        log.Add($"{mist.Count(m => m.Value)} bricks of mist (dithered) left out");
        // (objects standing on the car tracks: as LBA1 has them, but worth a look)
        var onTracks = cells.Keys.Where(k => PolarTerrain.TrackCells.Contains((k.X, k.Z)) && columns.TryGetValue((k.X, k.Z), out var col) && k.Y == col.Top + 1).ToList();
        log.Add($"{onTracks.Count} object cells stand on car track cells" + (onTracks.Count > 0 ? ": " + string.Join(" ", onTracks.Take(40).Select(k => $"({k.X},{k.Z}){cells[k].Brick}/{cells[k].Code:X2}")) : ""));

        // the pieces, in chunks
        var chunks = new List<List<(int X, int Y, int Z)>>();
        var seen = new HashSet<(int, int, int)>();
        foreach (var start in cells.Keys.OrderBy(k => k.Z).ThenBy(k => k.X).ThenBy(k => k.Y))
        {
            if (!seen.Add(start)) continue;
            var queue = new Queue<(int X, int Y, int Z)>(); queue.Enqueue(start); var piece = new List<(int X, int Y, int Z)>();
            while (queue.Count > 0)
            {
                var c = queue.Dequeue(); piece.Add(c);
                foreach (var (dx, dy, dz) in Neighbours)
                {
                    var n = (c.X + dx, c.Y + dy, c.Z + dz);
                    if (cells.ContainsKey(n) && seen.Add(n)) queue.Enqueue(n);
                }
            }
            foreach (var g in piece.GroupBy(c => (Math.DivRem(c.X + offsetX, Chunk, out _), Math.DivRem(c.Z + offsetZ, Chunk, out _)))) chunks.Add(g.ToList());
        }

        // the faces each chunk shows, and the tiles they need: a face is hidden where it lies on its cell's side and what is beyond covers
        // it -- ground, or an object's box as wide
        bool Solid((int X, int Y, int Z) c) => layout.Cells.ContainsKey(c) || columns.TryGetValue((c.X, c.Z), out var col) && c.Y <= col.Top;
        // (a rock in the water is drawn in LBA1 with the water round it on its brick: the water's own colours -- those most of LBA1's water
        // bricks are drawn in, the crystal's left out -- are taken off the rocks' bricks, so their boxes are the stones and the sea shows
        // round them)
        var water = WaterColours(game, layout);
        var rockBricks = cells.Where(c => rocks.Contains((c.Key.X, c.Key.Z))).Select(c => c.Value.Brick).ToHashSet();
        var sprites = new Dictionary<int, PolarTextures.Sprite>();
        PolarTextures.Sprite Sprite(int brick)
        {
            if (sprites.TryGetValue(brick, out var s)) return s;
            s = PolarTextures.Sprite.Decode(game.ReadBrick(brick));
            if (rockBricks.Contains(brick)) for (var i = 0; i < s.Pixels.Length; i++) if (water.Contains(s.Pixels[i])) s.Pixels[i] = -1;
            return sprites[brick] = s;
        }
        var boxes = new Dictionary<int, PolarTextures.Box>();
        PolarTextures.Box BoxOf(int brick) => boxes.TryGetValue(brick, out var b) ? b : boxes[brick] = Sprite(brick).Footprint();
        bool Hidden((int X, int Y, int Z) c, int dir)
        {
            var (dx, dy, dz) = Dirs[dir];
            var n = (c.X + dx, c.Y + dy, c.Z + dz);
            var box = BoxOf(cells[c].Brick);
            var onSide = dir switch { 1 => box.U1 >= 1, 2 => box.U0 <= 0, 3 => box.V1 >= 1, 4 => box.V0 <= 0, _ => true };
            if (!onSide) return false;
            if (!cells.TryGetValue(n, out var other)) return Solid(n);
            var nb = BoxOf(other.Brick);
            return dir switch
            {
                0 => nb.U0 <= box.U0 && nb.U1 >= box.U1 && nb.V0 <= box.V0 && nb.V1 >= box.V1,
                1 => nb.U0 <= 0 && nb.V0 <= box.V0 && nb.V1 >= box.V1,
                2 => nb.U1 >= 1 && nb.V0 <= box.V0 && nb.V1 >= box.V1,
                3 => nb.V0 <= 0 && nb.U0 <= box.U0 && nb.U1 >= box.U1,
                _ => nb.V1 >= 1 && nb.U0 <= box.U0 && nb.U1 >= box.U1,
            };
        }
        var tiles = new Dictionary<object, (int[] Tile, int W, int H)>();
        (int Brick, PolarTextures.Face Face) Key(int brick, PolarTextures.Face face) => (brick, face);
        var faces = new List<List<((int X, int Y, int Z) Cell, int Dir)>>();
        foreach (var chunk in chunks)
        {
            var list = new List<((int X, int Y, int Z), int)>();
            foreach (var c in chunk)
                for (var d = 0; d < Dirs.Length; d++)
                {
                    if (Hidden(c, d)) continue;
                    list.Add((c, d));
                    var face = FaceOf(d);
                    var key = Key(cells[c].Brick, face);
                    if (!tiles.ContainsKey(key)) { var (w, h) = PolarTextures.Size(face); tiles[key] = (Sprite(cells[c].Brick).Tile(face, BoxOf(cells[c].Brick)), w, h); }
                }
            faces.Add(list);
        }
        // (the pages: each brick's faces on one, the pages filled in the order the chunks first use the bricks -- a chunk's bricks mostly
        // together; a chunk whose bricks are on more than one page is a body for each)
        var pages = new PolarTextures.Pages(island.ObjectTexture, colours, IslandFile.MaxObjectPages);
        var pageOf = new Dictionary<int, int>();
        var filling = 0;
        foreach (var list in faces)
            foreach (var brick in list.Select(f => cells[f.Cell].Brick).Distinct())
            {
                if (pageOf.ContainsKey(brick)) continue;
                var keys = tiles.Keys.Where(k => k is ValueTuple<int, PolarTextures.Face> t && t.Item1 == brick).ToList();
                var need = keys.Sum(k => PolarTextures.Pages.HalvesOf(tiles[k].H));
                if (need > pages.Free(filling)) filling++;
                if (filling >= IslandFile.MaxObjectPages) throw new InvalidOperationException($"The objects' textures take more than {IslandFile.MaxObjectPages} pages.");
                foreach (var k in keys) { var (t, w, h) = tiles[k]; pages.Add(k, t, w, h, filling); }
                pageOf[brick] = filling;
            }
        var thin = cells.Values.Select(c => c.Brick).Distinct().Count(b => !BoxOf(b).IsFull);
        log.Add($"{thin} of the {cells.Values.Select(c => c.Brick).Distinct().Count()} object bricks fill part of their cell (posts, poles, fences): thinner boxes");

        // a body and a decor for each chunk (split in two along its longer side while it is too big for a body)
        var bodies = new List<byte[]>();
        var decors = 0;
        var queue2 = new Queue<(List<(int X, int Y, int Z)> Cells, List<((int X, int Y, int Z) Cell, int Dir)> Faces)>(chunks.Zip(faces));
        while (queue2.Count > 0)
        {
            var (chunk, shown) = queue2.Dequeue();
            if (shown.Count == 0) continue;
            // (bricks on more than one page: a body for each page's cells)
            var byPage = chunk.GroupBy(c => pageOf.TryGetValue(cells[c].Brick, out var pg) ? pg : 0).ToList();
            if (byPage.Count > 1)
            {
                foreach (var g in byPage) { var set = g.ToHashSet(); queue2.Enqueue((g.ToList(), shown.Where(f => set.Contains(f.Cell)).ToList())); }
                continue;
            }
            var page = byPage[0].Key;
            var body = Mesh(chunk, shown, cells, key => { var (_, x, y) = pages.Place[key]; return (x, y); }, BoxOf, out var origin);
            if (body is null)
            {
                // too big: halves along x or z
                var alongX = chunk.Max(c => c.X) - chunk.Min(c => c.X) >= chunk.Max(c => c.Z) - chunk.Min(c => c.Z);
                var mid = alongX ? (chunk.Min(c => c.X) + chunk.Max(c => c.X)) / 2 : (chunk.Min(c => c.Z) + chunk.Max(c => c.Z)) / 2;
                bool First((int X, int Y, int Z) c) => (alongX ? c.X : c.Z) <= mid;
                queue2.Enqueue((chunk.Where(First).ToList(), shown.Where(f => First(f.Cell)).ToList()));
                queue2.Enqueue((chunk.Where(c => !First(c)).ToList(), shown.Where(f => !First(f.Cell)).ToList()));
                continue;
            }
            // the decor: in the cube of the chunk's first column, at the body's origin, its box the chunk's cells' (cube-local)
            int gx0 = chunk.Min(c => c.X) + offsetX, gz0 = chunk.Min(c => c.Z) + offsetZ;
            var cube = island.CubeAt(gx0 / 64, gz0 / 64);
            if (cube is null) { log.Add($"a chunk at cell ({gx0}, {gz0}) is outside the island's cubes: left out"); continue; }
            if (cube.Decors.Count >= IslandDecors.MaxPerCube) { log.Add($"cube ({gx0 / 64}, {gz0 / 64}) has its {IslandDecors.MaxPerCube} objects: a chunk left out"); continue; }
            int cubeX = gx0 / 64 * IslandFile.CubeSize, cubeZ = gz0 / 64 * IslandFile.CubeSize;
            // (the origin is in the layout's cells and layers: to world units)
            var decor = IslandDecors.Blank(bodies.Count | (page << IslandFile.DecorPageShift), (int)Math.Round((origin.X + offsetX) * 512) - cubeX, (int)Math.Round(origin.Y * 256), (int)Math.Round((origin.Z + offsetZ) * 512) - cubeZ);
            decor.XMin = (chunk.Min(c => c.X) + offsetX) * 512 - cubeX; decor.XMax = (chunk.Max(c => c.X) + 1 + offsetX) * 512 - cubeX;
            decor.ZMin = (chunk.Min(c => c.Z) + offsetZ) * 512 - cubeZ; decor.ZMax = (chunk.Max(c => c.Z) + 1 + offsetZ) * 512 - cubeZ;
            decor.YMin = (chunk.Min(c => c.Y) - 1) * 256; decor.YMax = chunk.Max(c => c.Y) * 256;
            cube.Decors.Add(decor);
            bodies.Add(body.Write());
            decors++;
        }
        island.ObjectPages.Clear();
        island.ObjectPages.AddRange(pages.List.Skip(1));
        log.Add($"the objects' textures: {tiles.Count} faces of bricks at {PolarTextures.TileSize} pixels to a cell, on {pages.Count} pages");
        log.Add($"{cells.Count} object cells ({cells.Keys.Count(c => rocks.Contains((c.X, c.Z)))} of rocks in the water) in {chunks.Count} chunks: {decors} objects, {bodies.Count} bodies");
        return new Built(bodies, decors, log);
    }

    // LBA1's water colours: those that make up 85 % of its water bricks' pixels, less any the crystal's bricks are much drawn in.
    private static HashSet<int> WaterColours(Lba1Game game, PolarLayout layout)
    {
        Dictionary<int, long> Count(IEnumerable<int> bricks)
        {
            var counts = new Dictionary<int, long>();
            foreach (var b in bricks.Distinct())
                foreach (var px in PolarTextures.Sprite.Decode(game.ReadBrick(b)).Pixels) if (px >= 0) counts[px] = counts.GetValueOrDefault(px) + 1;
            return counts;
        }
        var water = Count(layout.Cells.Values.Where(c => c.Water).Select(c => c.Brick));
        var crystal = Count(layout.Cells.Values.Where(c => (c.Code & 0xF0) == 0xA0).Select(c => c.Brick));
        long total = water.Values.Sum(), crystalTotal = Math.Max(1, crystal.Values.Sum()), sum = 0;
        var result = new HashSet<int>();
        foreach (var (colour, n) in water.OrderByDescending(k => k.Value))
        {
            if (sum >= total * 0.85) break;
            sum += n;
            if (crystal.GetValueOrDefault(colour) * 200 < crystalTotal) result.Add(colour);
        }
        return result;
    }

    private static readonly (int, int, int)[] Neighbours = { (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) };
    // the faces a box shows: its top and its four sides (nothing sees under an object)
    private static readonly (int Dx, int Dy, int Dz)[] Dirs = { (0, 1, 0), (1, 0, 0), (-1, 0, 0), (0, 0, 1), (0, 0, -1) };
    private static PolarTextures.Face FaceOf(int dir) => dir switch { 0 => PolarTextures.Face.Top, 1 or 2 => PolarTextures.Face.SideX, _ => PolarTextures.Face.SideZ };

    // A chunk's body: its faces as textured quads, its points relative to the origin (the middle of its footprint, at the foot of its
    // lowest cell, island-wide world units). Null when it is too big for a body.
    private static Body? Mesh(List<(int X, int Y, int Z)> chunk, List<((int X, int Y, int Z) Cell, int Dir)> shown, Dictionary<(int X, int Y, int Z), PolarLayout.Cell> cells,
        Func<object, (int X, int Y)> place, Func<int, PolarTextures.Box> boxOf, out Vector3 origin)
    {
        int x0 = chunk.Min(c => c.X), x1 = chunk.Max(c => c.X) + 1, z0 = chunk.Min(c => c.Z), z1 = chunk.Max(c => c.Z) + 1, y0 = chunk.Min(c => c.Y) - 1;
        // (island-wide world coordinates come later: here the layout's, the origin in cells)
        origin = new Vector3((x0 + x1) / 2f, y0, (z0 + z1) / 2f);
        var o = origin;
        var points = new List<Vector3>();
        var index = new Dictionary<(int, int, int), int>();
        int P(double x, int y, double z)
        {
            // (the same point to 1/64 of a cell)
            var key = ((int)Math.Round(x * 64), y, (int)Math.Round(z * 64));
            if (index.TryGetValue(key, out var i)) return i;
            points.Add(new Vector3((float)((x - o.X) * 512), (y - o.Y) * 256, (float)((z - o.Z) * 512)));
            return index[key] = points.Count - 1;
        }
        var faces = new List<Face>();
        var handles = new Dictionary<(int, int), int>();
        var textures = new List<uint>();
        foreach (var (c, dir) in shown)
        {
            // the cell spans x .. x+1, z .. z+1 and the layer's y-1 .. y (its top at y); its box the part its brick fills
            var b = boxOf(cells[c].Brick);
            double x = c.X + b.U0, xe = c.X + b.U1, z = c.Z + b.V0, ze = c.Z + b.V1;
            var y = c.Y;
            var face = FaceOf(dir);
            var (tx, ty) = place((cells[c].Brick, face));
            var (w, h) = PolarTextures.Size(face);
            // corners in order (outward winding) with their (u, v) in the tile
            (double X, int Y, double Z, double U, double V)[] corners = dir switch
            {
                0 => new[] { (x, y, ze, 0.0, 1.0), (xe, y, ze, 1.0, 1.0), (xe, y, z, 1.0, 0.0), (x, y, z, 0.0, 0.0) },
                1 => new[] { (xe, y, z, 0.0, 0.0), (xe, y, ze, 1.0, 0.0), (xe, y - 1, ze, 1.0, 1.0), (xe, y - 1, z, 0.0, 1.0) },
                2 => new[] { (x, y - 1, z, 0.0, 1.0), (x, y - 1, ze, 1.0, 1.0), (x, y, ze, 1.0, 0.0), (x, y, z, 0.0, 0.0) },
                3 => new[] { (x, y - 1, ze, 0.0, 1.0), (xe, y - 1, ze, 1.0, 1.0), (xe, y, ze, 1.0, 0.0), (x, y, ze, 0.0, 0.0) },
                _ => new[] { (x, y, z, 0.0, 0.0), (xe, y, z, 1.0, 0.0), (xe, y - 1, z, 1.0, 1.0), (x, y - 1, z, 0.0, 1.0) },
            };
            var ids = corners.Select(k => P(k.X, k.Y, k.Z)).ToArray();
            if (!handles.TryGetValue((tx, ty), out var handle)) { handle = handles[(tx, ty)] = textures.Count; textures.Add(TileEntry(tx, ty)); }
            var uv = corners.SelectMany(k => new[] { (int)Math.Round((0.4 + k.U * (w - 0.8)) * 256), (int)Math.Round((0.4 + k.V * (h - 0.8)) * 256) }).ToArray();
            faces.Add(new Face(ids, 0, Texture: new FaceTexture(handle, uv), Lba2Type: 8));
        }
        if (points.Count > MaxPoints || faces.Count > MaxFaces) return null;
        // (the winding: the engine takes a face as facing out when its first three corners turn so that (p1 - p0) x (p2 - p0) points out,
        // as RaceTrackDeckBody's boxes are made: each face reversed where it doesn't)
        for (var k = 0; k < faces.Count; k++)
        {
            var f = faces[k];
            var p = f.Points.Select(i => points[i]).ToArray();
            var n = Vector3.Cross(p[1] - p[0], p[2] - p[0]);
            var (dx, dy, dz) = Dirs[shown[k].Dir];
            if (Vector3.Dot(n, new Vector3(dx, dy, dz)) >= 0) continue;
            var uv = f.Texture!.UV;
            var reversedUv = Enumerable.Range(0, f.Points.Length).Reverse().SelectMany(i => new[] { uv[i * 2], uv[i * 2 + 1] }).ToArray();
            faces[k] = f with { Points = f.Points.Reverse().ToArray(), Texture = f.Texture with { UV = reversedUv } };
        }
        // (the header's first byte: the body format's version, 0x10 as every retail body has it -- the engine reads textured polygons
        // by it; the race tracks' untextured deck bodies got by with 0)
        var header = new byte[96];
        header[0] = 0x10;
        var body = new Body
        {
            Game = 2, Static = true, Lit = false, Header = header,
            Vertices = points,
            Bones = new List<Bone> { new(0, points.Count, 0, -1, new byte[8]) },
            Faces = faces,
            Textures = textures.ToArray(),
        };
        return body;
    }
}
