using System.Numerics;
using LBAAssembler.Lba1;
using LBAAssembler.Terrain.Polar;

namespace LBAAssembler.Terrain.Temple;

// The Temple of Bú's shaped things -- its rocks, its iron grilles and spiked rails, its statues, its stairs, its bed of spikes -- as models of
// what they are, where TempleObjects makes every other cell a box with its brick's picture on its faces and TempleShapes turns the round
// things (2026-10-17, the user's: "The Temple track still has a lot of blocky objects from the conversion that need refining"). A box with a
// statue's picture on each side is a crate with a statue painted on it; a grille's bars, unskewed onto the faces of the boxes of its cells,
// come apart into crosses floating at different depths; a rock's lobes are a curtain of flat slabs.
//
// LBA1 places each as a block of its scene's library, the thing's picture split among the block's bricks. The block's bricks drawn together
// (Picture) are the thing as LBA1's camera sees it, looking down at 30 degrees. Each kind is read from that picture its own way (Make):
//   - a grille, a spiked rail, an iron fan (Panel, TempleModels.Panels.cs): flat, in one upright plane, which the picture places -- the
//     plane whose cells the picture fits; a see-through cut-out there, LBA1's picture unskewed onto it, seen from both sides;
//   - stairs (Steps, .Panels.cs): their steps; a bed of spikes (Spikes, .Panels.cs): its spikes;
//   - rocks (Lobes, .Lobes.cs): a row of upright lobes evenly along the block, each as tall as the picture's outline over it;
//   - statues (Figure, .Figures.cs): what both LBA1's view and its mirror image (the statue's other side) leave of the block, slice by slice.
// A model's faces are in the block's cells and layers; each texel of its tiles is the picture's pixel where that point falls from LBA1's
// angle (Paint), so that from LBA1's angle it draws much the picture LBA1 draws. Where the race track's doorways cut a placement's cells,
// all of it goes -- of a grille or a row of rocks, the columns they cut (CutRemains). What is solid is the cells' boxes, as before
// (TempleObjects), but those of what the doorways took with them.
internal static partial class TempleModels
{
    public enum Make { Panel, Steps, Spikes, Lobes, Figure }

    // a kind: its name, a brick its block has and no other (LBA_BRK entry; the blocks of the three scenes that share a picture share its
    // bricks), how it is made, and (Steps) how many steps a cell has
    public sealed record Kind(string Name, int Brick, Make How, int Steps = 0);
    public static readonly Kind[] Kinds =
    {
        new("grille", 2366, Make.Panel), new("grille", 2383, Make.Panel),
        new("spiked rail", 2567, Make.Panel), new("spiked rail", 2575, Make.Panel),
        new("iron fan", 2460, Make.Panel), new("iron fan", 2474, Make.Panel), new("iron fan", 2491, Make.Panel), new("iron fan", 2508, Make.Panel),
        new("stairs", 2327, Make.Steps, Steps: 2), new("stairs", 2328, Make.Steps, Steps: 2), new("spikes", 5713, Make.Spikes),
        new("rocks", 2207, Make.Lobes), new("rocks", 2231, Make.Lobes), new("rocks", 2257, Make.Lobes), new("rocks", 2292, Make.Lobes),
        new("rocks", 2303, Make.Lobes), new("rocks", 2306, Make.Lobes), new("rocks", 2309, Make.Lobes), new("rocks", 5670, Make.Lobes),
        new("statue", 2403, Make.Figure), new("statue", 5683, Make.Figure), new("small statue", 2420, Make.Figure),
        new("small statue", 2423, Make.Figure), new("flame mask", 2412, Make.Figure), new("flame mask", 5692, Make.Figure),
        new("rabbit", 5705, Make.Figure),
    };
    private static readonly int[] LibraryScenes = TempleLayout.Scenes;

    // A block's picture as LBA1 draws it (LBA1 palette indices, -1 where nothing), and where the block's point (0, 0, 0) is drawn.
    public sealed class Picture
    {
        public int[] Pixels = Array.Empty<int>();
        public int Width, Height, OriginX, OriginY;

        // The screen point of a point of the block (cells, layers).
        public (double X, double Y) Screen(double x, double y, double z) => (OriginX + 24 + 24 * (x - z), OriginY + 15 + 12 * (x + z) - 15 * y);
        public (double X, double Y) Screen(Vector3 p) => Screen(p.X, p.Y, p.Z);
        public int At(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? -1 : Pixels[y * Width + x];
        public int Sample(Vector3 p) { var (sx, sy) = Screen(p); return At((int)Math.Floor(sx), (int)Math.Floor(sy)); }

        // The block's bricks drawn far to near (z, then x, then y), as LBA1 draws them.
        public static Picture Draw(Lba1Game game, IEnumerable<(int Brick, int I, int J, int K)> bricks)
        {
            var sprites = bricks.Select(b => (b.I, b.J, b.K, Sprite: PolarTextures.Sprite.Decode(game.ReadBrick(b.Brick)))).OrderBy(b => b.K).ThenBy(b => b.I).ThenBy(b => b.J).ToList();
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var (i, j, k, s) in sprites)
            {
                int left = 24 * (i - k) + s.HotX, top = 12 * (i + k) - 15 * j + s.HotY;
                minX = Math.Min(minX, left); minY = Math.Min(minY, top); maxX = Math.Max(maxX, left + s.Width); maxY = Math.Max(maxY, top + s.Lines);
            }
            var p = new Picture { Width = maxX - minX, Height = maxY - minY, OriginX = -minX, OriginY = -minY };
            p.Pixels = new int[p.Width * p.Height];
            Array.Fill(p.Pixels, -1);
            foreach (var (i, j, k, s) in sprites)
            {
                int left = 24 * (i - k) + s.HotX + p.OriginX, top = 12 * (i + k) - 15 * j + s.HotY + p.OriginY;
                for (var y = 0; y < s.Lines; y++)
                    for (var x = 0; x < s.Width; x++)
                        if (s.Pixels[y * s.Width + x] is >= 0 and var c) p.Pixels[(top + y) * p.Width + left + x] = c;
            }
            return p;
        }
    }

    // A tile of a model: its size, its texels in LBA1's colours (-1 see-through), and whether it is solid (drawn as it is, its gaps filled).
    public sealed record ModelTile(int W, int H, int[] Pixels, bool Solid);

    // A face of a model: its corners (the block's cells and layers, from its point (0, 0, 0)), their places in its tile (0..1), the tile
    // (the model's Tiles), the way it faces (out, in cells and layers), and, of a face that lies on a side of a cell (the face of a step's
    // side), that side (TempleObjects' directions: 0 top, 1 +x, 2 -x, 3 +z, 4 -z, 5 bottom): left out where what is beyond hides it. Group:
    // of a model that may be cut in pieces (a grille's, a row of rocks'), which of its block's columns along its long way the face goes with
    // (Model.GroupOf) -- the race track's doorways take the columns they cut, the rest stays.
    public readonly record struct ModelFace(Vector3[] Corners, (double U, double V)[] Uv, int Tile, Vector3 Normal, int Side = -1, int Group = -1);

    // A kind as its block has it, made.
    public sealed class Model
    {
        public required Kind Kind { get; init; }
        public int Dx, Dy, Dz;
        public required Picture Picture { get; init; }
        // the block's cells that have a brick that draws something
        public HashSet<(int I, int J, int K)> Bricked = new();
        public readonly List<ModelFace> Faces = new();
        public readonly List<ModelTile> Tiles = new();
        // what was read from the picture, for the log
        public string Reading = "";

        // Whether it may be cut in pieces (every face in a group), and the group -- the column along its long way (x, or z where the block is
        // longer that way) -- of a cell of its block.
        public bool Grouped => Faces.Count > 0 && Faces.All(f => f.Group >= 0);
        public int GroupOf(int i, int k) => Dx >= Dz ? i : k;

        // A tile, its texels each the colour `paint` gives the point of the model `point` gives for its place (u, v) in the tile -- null where
        // the tile has no point -- and solid (gaps the nearest texel's colour) or see-through where the picture has nothing. Its index.
        public int AddTile(int w, int h, Func<double, double, Vector3?> point, Func<Vector3, int> paint, bool solid)
            => AddTile(w, h, (u, v) => point(u, v) is { } p ? paint(p) : -1, solid);
        // (a tile whose texels `texel` gives for their places)
        public int AddTile(int w, int h, Func<double, double, int> texel, bool solid)
        {
            var pixels = new int[w * h];
            for (var j = 0; j < h; j++)
                for (var i = 0; i < w; i++)
                    pixels[j * w + i] = texel((i + 0.5) / w, (j + 0.5) / h);
            Tiles.Add(Finish(w, h, pixels, solid));
            return Tiles.Count - 1;
        }

        // the picture's colour where a point falls from LBA1's angle
        public int Paint(Vector3 p) => Picture.Sample(p);
    }

    // A tile as it is drawn: solid -- every texel the picture leaves empty the nearest drawn texel's colour (none drawn: the picture's
    // commonest colour) -- or see-through, solid only where the picture draws TempleObjects.Solid of it and more.
    private static ModelTile Finish(int w, int h, int[] pixels, bool solid)
    {
        var drawn = Enumerable.Range(0, pixels.Length).Where(k => pixels[k] >= 0).ToList();
        if (!solid) solid = drawn.Count >= TempleObjects.Solid * pixels.Length;
        if (!solid || drawn.Count == pixels.Length) return new ModelTile(w, h, pixels, solid);
        if (drawn.Count == 0) return new ModelTile(w, h, pixels, false);
        var filled = (int[])pixels.Clone();
        for (var k = 0; k < pixels.Length; k++)
        {
            if (pixels[k] >= 0) continue;
            int i = k % w, j = k / w;
            filled[k] = pixels[drawn.MinBy(d => (d % w - i) * (d % w - i) + (d / w - j) * (d / w - j))];
        }
        return new ModelTile(w, h, filled, true);
    }

    // A cell of a model's block: its model and its place in the block.
    public readonly record struct Part(Model Model, int I, int J, int K);

    // The models' bricks: each, its model and its place in the model's block. (Kinds whose block no library has are left out; a brick that
    // draws nothing -- LBA1's empty filler brick, in many blocks -- is no model's.)
    public static Dictionary<int, Part> Find(Lba1Game game, List<string>? log = null)
    {
        var parts = new Dictionary<int, Part>();
        var libraries = LibraryScenes.Select(game.ReadBlocks).ToList();
        var draws = new Dictionary<int, bool>();
        bool Draws(int brick) => draws.TryGetValue(brick, out var d) ? d : draws[brick] = PolarTextures.Sprite.Decode(game.ReadBrick(brick)).Pixels.Any(c => c >= 0);
        foreach (var kind in Kinds)
        {
            var found = false;
            foreach (var lib in libraries)
            {
                var count = BitConverter.ToInt32(lib, 0) / 4;
                for (var block = 0; block < count && !found; block++)
                {
                    var at = BitConverter.ToInt32(lib, block * 4);
                    int dx = lib[at], dy = lib[at + 1], dz = lib[at + 2];
                    var bricks = new List<(int Brick, int I, int J, int K)>();
                    for (var p = 0; p < dx * dy * dz; p++)
                    {
                        var number = BitConverter.ToUInt16(lib, at + 3 + p * 4 + 2);
                        if (number != 0) bricks.Add((number - 1, p % dx, p / dx % dy, p / dx / dy));
                    }
                    if (!bricks.Any(b => b.Brick == kind.Brick)) continue;
                    bricks = bricks.Where(b => Draws(b.Brick)).ToList();
                    var model = new Model { Kind = kind, Dx = dx, Dy = dy, Dz = dz, Picture = Picture.Draw(game, bricks), Bricked = bricks.Select(b => (b.I, b.J, b.K)).ToHashSet() };
                    switch (kind.How)
                    {
                        case Make.Panel: MakePanel(model); break;
                        case Make.Steps: MakeSteps(model, lib[at + 3]); break;
                        case Make.Spikes: MakeSpikes(model); break;
                        case Make.Lobes: MakeLobes(model); break;
                        default: MakeFigure(model); break;
                    }
                    foreach (var (brick, i, j, k) in bricks) parts[brick] = new Part(model, i, j, k);
                    log?.Add($"{kind.Name} (brick {kind.Brick}): block of {dx} x {dy} x {dz} cells, {model.Faces.Count} faces, {model.Tiles.Count} tiles; {model.Reading}");
                    found = true;
                }
                if (found) break;
            }
            if (!found) log?.Add($"{kind.Name}: no block of the temple's libraries has brick {kind.Brick}");
        }
        return parts;
    }

    // The cells of models a race track's cuts (TempleIsland: its road's doorways) leave hanging: of a placement the cuts take cells of, the
    // rest; and of one standing on a cell the cuts or this take, all of it (as TempleShapes.CutRemains, the round things') -- but of a model
    // that may be cut in pieces (Model.Grouped: a grille, a row of rocks), only the columns along its long way that the cuts take cells of,
    // all of each (no grille's top hanging over a doorway, no rock lobe cut in two); the rest of it stays, as the boxes did.
    public static HashSet<(int X, int Y, int Z)> CutRemains(Dictionary<int, Part> parts, Dictionary<(int X, int Y, int Z), PolarLayout.Cell> cells,
        ICollection<(int X, int Y, int Z)> cut)
    {
        var bricksOf = parts.GroupBy(p => p.Value.Model).ToDictionary(g => g.Key, g => g.Select(p => (Brick: p.Key, p.Value.I, p.Value.J, p.Value.K)).ToList());
        var gone = new HashSet<(int X, int Y, int Z)>(cut.Where(cells.ContainsKey));
        var remains = new HashSet<(int X, int Y, int Z)>();
        var hit = new Queue<(int X, int Y, int Z)>(gone);
        while (hit.Count > 0)
        {
            var c = hit.Dequeue();
            foreach (var at in new (int X, int Y, int Z)[] { c, (c.X, c.Y + 1, c.Z) })
            {
                if (!cells.TryGetValue(at, out var cell) || !parts.TryGetValue(cell.Brick, out var part)) continue;
                if (at != c && (part.J != 0 || gone.Contains(at))) continue;
                var anchor = (X: at.X - part.I, Y: at.Y - part.J, Z: at.Z - part.K);
                var model = part.Model;
                foreach (var (brick, i, j, k) in bricksOf[model])
                {
                    if (model.Grouped && model.GroupOf(i, k) != model.GroupOf(part.I, part.K)) continue;
                    var other = (anchor.X + i, anchor.Y + j, anchor.Z + k);
                    if (!cells.TryGetValue(other, out var o) || o.Brick != brick || !gone.Add(other)) continue;
                    remains.Add(other);
                    hit.Enqueue(other);
                }
            }
        }
        return remains;
    }

    // The faces each cell draws of the models the temple places: each face of a placement drawn by the cell of the placement it lies in, or
    // where the layout has some other brick there (LBA1 placing a block over another's cells), by the placement's nearest cell -- with the
    // placement's point (0, 0, 0) in the layout. A step's side against a step of the same stairs is left out, and a face of a group (a
    // column of a grille, of a row of rocks) whose cells the layout has none of (the race track's doorways took them: CutRemains).
    public static Dictionary<(int X, int Y, int Z), List<(ModelFace Face, Model Model, Vector3 Anchor)>> Placed(Dictionary<int, Part> parts,
        Dictionary<(int X, int Y, int Z), PolarLayout.Cell> cells)
    {
        var faces = new Dictionary<(int X, int Y, int Z), List<(ModelFace, Model, Vector3)>>();
        var placements = cells.Where(c => parts.ContainsKey(c.Value.Brick))
            .GroupBy(c => { var p = parts[c.Value.Brick]; return (p.Model, Anchor: (X: c.Key.X - p.I, Y: c.Key.Y - p.J, Z: c.Key.Z - p.K)); });
        foreach (var placement in placements)
        {
            var (model, anchor) = placement.Key;
            var held = placement.Select(c => c.Key).OrderBy(c => c.Y).ThenBy(c => c.Z).ThenBy(c => c.X).ToList();
            var heldSet = held.ToHashSet();
            foreach (var face in model.Faces)
            {
                // (the cell it lies in: its middle, a little inside it)
                var mid = face.Corners.Aggregate(Vector3.Zero, (a, b) => a + b) / face.Corners.Length - face.Normal * 0.01f;
                var cell = (X: anchor.X + Math.Clamp((int)Math.Floor(mid.X), 0, model.Dx - 1), Y: anchor.Y + Math.Clamp((int)Math.Floor(mid.Y), 0, model.Dy - 1),
                    Z: anchor.Z + Math.Clamp((int)Math.Floor(mid.Z), 0, model.Dz - 1));
                if (face.Side >= 0)
                {
                    var (dx, dy, dz) = Sides[face.Side];
                    if (cells.TryGetValue((cell.X + dx, cell.Y + dy, cell.Z + dz), out var beyond) && parts.TryGetValue(beyond.Brick, out var bp) && bp.Model.Kind.How == Make.Steps
                        && bp.Model == model) continue;
                }
                var candidates = face.Group < 0 ? held : held.Where(c => model.GroupOf(c.X - anchor.X, c.Z - anchor.Z) == face.Group).ToList();
                if (candidates.Count == 0) continue;
                if (!heldSet.Contains(cell))
                {
                    var c0 = cell;
                    cell = candidates.MinBy(c => Math.Abs(c.Y - c0.Y) * 4 + Math.Abs(c.X - c0.X) + Math.Abs(c.Z - c0.Z));
                }
                if (!faces.TryGetValue(cell, out var list)) faces[cell] = list = new();
                list.Add((face, model, new Vector3(anchor.X, anchor.Y, anchor.Z)));
            }
        }
        return faces;
    }

    // the sides of a cell, as TempleObjects has them
    public static readonly (int Dx, int Dy, int Dz)[] Sides = { (0, 1, 0), (1, 0, 0), (-1, 0, 0), (0, 0, 1), (0, 0, -1), (0, -1, 0) };

    // A quad's or triangle's face, wound either way (TempleObjects winds it to face `normal`).
    private static void Face(Model m, int tile, Vector3 normal, IReadOnlyList<(Vector3 P, double U, double V)> corners, int side = -1, int group = -1)
        => m.Faces.Add(new ModelFace(corners.Select(c => c.P).ToArray(), corners.Select(c => (c.U, c.V)).ToArray(), tile, normal, side, group));
}
