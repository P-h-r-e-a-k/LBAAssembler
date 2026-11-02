using LBAAssembler;
using LBAAssembler.Lba1;
using LBAAssembler.Terrain;
using LBAAssembler.Terrain.Polar;

namespace ScriptRoundTrip;

// polarstats <LBA1 folder> <scene>...: how much a set of LBA1 scenes holds -- their drawn bricks, the distinct bricks and blocks, the
// layers they reach -- to size a port of them to an LBA2 island (Polar Island, 2026-10-05).
internal static class PolarStudy
{
    // decordiff <island file A> <island file B> <x0> <x1> <z0> <z1>: the decors of A (a box reaching into those island cells) that B hasn't
    // got -- by cube, place and body -- and B's that A hasn't: what a build took away and added there.
    public static int DecorDiff(string[] args)
    {
        var a = IslandFile.Load(args[1]); var b = IslandFile.Load(args[2]);
        double x0 = double.Parse(args[3]), x1 = double.Parse(args[4]), z0 = double.Parse(args[5]), z1 = double.Parse(args[6]);
        IEnumerable<(int Cx, int Cz, IslandDecor D)> In(IslandFile f) => LBAAssembler.Terrain.IslandOps.CubeCells(f).SelectMany(c => c.Item3.Decors.Select(d => (c.Item1, c.Item2, d)))
            .Where(t => (t.Item1 * 32768.0 + t.d.XMax) / 512 >= x0 && (t.Item1 * 32768.0 + t.d.XMin) / 512 <= x1 && (t.Item2 * 32768.0 + t.d.ZMax) / 512 >= z0 && (t.Item2 * 32768.0 + t.d.ZMin) / 512 <= z1);
        static (int, int, int, int, int, int) Key((int Cx, int Cz, IslandDecor D) t) => (t.Cx, t.Cz, t.D.X, t.D.Y, t.D.Z, t.D.Body);
        var inA = In(a).ToList(); var inB = In(b).ToList();
        var keysB = inB.Select(Key).ToHashSet(); var keysA = inA.Select(Key).ToHashSet();
        string Show((int Cx, int Cz, IslandDecor D) t) => FormattableString.Invariant(
            $"cube ({t.Cx},{t.Cz}) body {t.D.Body & 0xFFFF} page {t.D.Body >> IslandFile.DecorPageShift}: cells x {(t.Cx * 32768.0 + t.D.XMin) / 512:0.0}..{(t.Cx * 32768.0 + t.D.XMax) / 512:0.0} z {(t.Cz * 32768.0 + t.D.ZMin) / 512:0.0}..{(t.Cz * 32768.0 + t.D.ZMax) / 512:0.0} y {t.D.YMin}..{t.D.YMax}; origin ({t.D.X},{t.D.Y},{t.D.Z}) turn {t.D.Beta}");
        var gone = inA.Where(t => !keysB.Contains(Key(t))).ToList(); var added = inB.Where(t => !keysA.Contains(Key(t))).ToList();
        Console.WriteLine($"{inA.Count} decors in A there, {inB.Count} in B; {gone.Count} of A's not in B, {added.Count} of B's not in A");
        foreach (var t in gone.OrderByDescending(t => t.D.YMax)) Console.WriteLine("  gone  " + Show(t));
        foreach (var t in added.OrderByDescending(t => t.D.YMax)) Console.WriteLine("  added " + Show(t));
        return 0;
    }

    // defsuse <ILE> <cube x> <cube z>: a cube's texture definitions -- how many, how many its triangles use, and by what kind of triangle
    // (texture flag, polygon flag, bank), each kind's triangles and distinct definitions.
    public static int DefsUse(string[] args)
    {
        var island = IslandFile.Load(args[1]);
        var cube = island.CubeAt(int.Parse(args[2]), int.Parse(args[3]))!;
        var used = new Dictionary<(int Tex, int Poly, int Bank), (int Triangles, HashSet<int> Defs)>();
        for (var z = 0; z < 64; z++)
            for (var x = 0; x < 64; x++)
                for (var h = 0; h < 2; h++)
                {
                    var p = new IslandPolygon(cube.Polygon(x, z, h));
                    if (p.TexFlag == 0) continue;
                    var key = (p.TexFlag, p.PolyFlag, p.Bank);
                    if (!used.TryGetValue(key, out var u)) used[key] = u = (0, new HashSet<int>());
                    used[key] = (u.Triangles + 1, u.Defs);
                    var (page, def) = island.GroundTextureOf(p);
                    u.Defs.Add(def | page << 16);
                }
        // (DEFS_CELLS="x0 x1 z0 z1": each smooth-kerb triangle's definition in those cells, cube-local)
        if (Environment.GetEnvironmentVariable("DEFS_CELLS") is { } box && box.Split(' ').Select(int.Parse).ToArray() is { Length: 4 } b)
            for (var z = b[2]; z <= b[3]; z++)
                for (var x = b[0]; x <= b[1]; x++)
                    for (var h = 0; h < 2; h++)
                    {
                        var p = new IslandPolygon(cube.Polygon(x, z, h));
                        if (p.TexFlag != 1 || p.PolyFlag != 3) continue;
                        var i = island.GroundTextureOf(p).Definition;
                        Console.WriteLine($"  ({x},{z}) half {h} diag {(p.Diagonal ? 1 : 0)}: def {i} = {string.Join(' ', cube.TextureDefs.AsSpan(i * 6, 6).ToArray())}");
                    }
        Console.WriteLine($"{cube.TextureDefs.Length / 6} definitions; used: {used.Values.SelectMany(u => u.Defs).Distinct().Count()}");
        foreach (var (k, (t, d)) in used.OrderByDescending(u => u.Value.Defs.Count)) Console.WriteLine($"  tex {k.Tex} poly {k.Poly} bank {k.Bank}: {t} triangles, {d.Count} definitions");
        return 0;
    }

    // entityanims <game> <entity>...: each entity's animations (generic number -> ANIM.HQR entry, its keyframes and length) and bodies.
    public static int EntityAnims(string[] args)
    {
        var table = LBAAssembler.Lba2EntityTable.Load(args[1]) ?? throw new InvalidDataException("no entity table");
        var anims = HqrArchive.Open(Path.Combine(args[1], "ANIM.HQR"));
        foreach (var id in args.Skip(2).Select(int.Parse))
        {
            var e = table.Entities[id];
            Console.WriteLine($"entity {id}: bodies {string.Join(", ", e.Bodies.Select(b => $"{b.Generic}->{b.Body}"))}");
            foreach (var (g, a) in e.Anims)
            {
                var data = anims.Read(a);
                int frames = data.Length >= 2 ? BitConverter.ToUInt16(data, 0) : 0;
                Console.WriteLine($"  anim {g,3} -> ANIM.HQR {a,4}: {frames} keyframes, {data.Length} bytes");
            }
        }
        return 0;
    }

    // gametext <game> <file> <id>...: texts of a text file of TEXT.HQR, in English.
    public static int Lba2Text(string[] args)
    {
        var bank = Lba2TextBank.Load(HqrArchive.Open(Path.Combine(args[1], "TEXT.HQR")), 0, int.Parse(args[2]));
        foreach (var id in args.Skip(3).Select(int.Parse))
            Console.WriteLine($"{id}: {(bank.Find(id) is { } t ? LBAAssembler.Terrain.Polar.PolarDream.Dos.GetString(t.Bytes) : "(none)")}");
        return 0;
    }

    public static int Stats(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var allBricks = new HashSet<int>(); var allBlocks = new HashSet<int>();
        var total = 0;
        foreach (var scene in args.Skip(2).Select(int.Parse))
        {
            var grid = game.ReadGrid(scene); var blocks = game.ReadBlocks(scene);
            var placements = Lba1GridRenderer.Placements(grid, blocks).ToList();
            var cells = Lba1GridCodec.Decode(grid);
            var blockIds = new HashSet<int>();
            for (var i = 0; i < 64 * 64 * 25; i++) if (cells[i * 2] != 0) blockIds.Add(cells[i * 2]);
            var bricks = placements.Select(p => p.Brick).ToHashSet();
            // the top surface: per column, its highest drawn layer
            var tops = placements.GroupBy(p => (p.X, p.Z)).Select(g => g.Max(p => p.Y)).ToList();
            Console.WriteLine($"scene {scene,3}: {placements.Count} bricks drawn, {bricks.Count} distinct bricks, {blockIds.Count} blocks; " +
                              $"{tops.Count} columns, top layers {tops.Min()}..{tops.Max()} (mean {tops.Average():0.0})");
            allBricks.UnionWith(bricks); allBlocks.UnionWith(blockIds.Select(b => scene * 10000 + b)); total += placements.Count;
        }
        Console.WriteLine($"all: {total} bricks drawn, {allBricks.Count} distinct bricks (LBA_BRK entries), {allBlocks.Count} scene blocks");
        return 0;
    }

    // polarlayout <LBA1 folder> <out.png>: the joined Polar Island (Terrain.Polar.PolarLayout) drawn as LBA1 draws it, and what it did.
    public static int Layout(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = LBAAssembler.Terrain.Polar.PolarLayout.Build(game);
        foreach (var line in layout.Log) Console.WriteLine(line);
        foreach (var p in layout.Placements) Console.WriteLine($"  scene {p.Scene} {p.Name}: at ({p.X}, {p.Y}, {p.Z})");
        int x0 = layout.Cells.Keys.Min(k => k.X), x1 = layout.Cells.Keys.Max(k => k.X), z0 = layout.Cells.Keys.Min(k => k.Z), z1 = layout.Cells.Keys.Max(k => k.Z);
        Console.WriteLine($"{layout.Cells.Count} cells; x {x0}..{x1}, z {z0}..{z1} ({x1 - x0 + 1} x {z1 - z0 + 1} cells, {(x1 - x0 + 64) / 64} x {(z1 - z0 + 64) / 64} LBA2 cubes at most)");
        var image = Lba1GridRenderer.Render(new[] { layout.Tile() }, game.ReadBrick, game.Palette);
        PngWriter.Write(args[2], image.Bgra, image.Width, image.Height);
        Console.WriteLine($"{args[2]}: {image.Width} x {image.Height}");
        return 0;
    }

    // polarmountain <LBA1 folder>: where 110's mountain columns land in the joined island (which scene's ground is there, and how high)
    public static int Mountain(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = LBAAssembler.Terrain.Polar.PolarLayout.Build(game);
        var o = layout.PeakCopyOffset;
        var peak = Lba1GridRenderer.Placements(game.ReadGrid(110), game.ReadBlocks(110));
        var tops = peak.GroupBy(c => (c.X, c.Z)).ToDictionary(g => g.Key, g => g.Max(c => c.Y));
        var counts = new Dictionary<string, int>();
        foreach (var ((x, z), top) in tops.Where(t => t.Value >= LBAAssembler.Terrain.Polar.PolarLayout.MountainTop))
        {
            int gx = x + o.X, gz = z + o.Z;
            var here = layout.Cells.Where(c => c.Key.X == gx && c.Key.Z == gz).ToList();
            var ground = here.Where(c => c.Value.Scene != 111).ToList();
            var key = ground.Count == 0 ? (here.Count == 0 ? "empty" : "only 111") : $"scene {ground.GroupBy(c => c.Value.Scene).MaxBy(g => g.Count())!.Key} top {ground.Max(c => c.Key.Y)} vs mountain {top + o.Y}";
            key = key.Contains("vs") ? (ground.Max(c => c.Key.Y) >= top + o.Y - 1 ? $"scene {ground[0].Value.Scene}: as high" : $"scene {ground[0].Value.Scene}: lower") : key;
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }
        foreach (var (k, n) in counts.OrderByDescending(c => c.Value)) Console.WriteLine($"  {n,4} mountain columns: {k}");
        return 0;
    }

    // polarblocks <LBA1 folder> <scene>: the scene's blocks (library entries) as its grid uses them: size, how many placed, their bricks'
    // shape and sound codes, and the mean colour of their top bricks -- to tell ground from objects.
    public static int Blocks(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var scene = int.Parse(args[2]);
        var cells = Lba1GridCodec.Decode(game.ReadGrid(scene));
        var lib = game.ReadBlocks(scene);
        var use = new Dictionary<int, int>();
        for (var i = 0; i < 64 * 64 * 25; i++) { var b = cells[i * 2]; if (b != 0 && cells[i * 2 + 1] == 0) use[b] = use.GetValueOrDefault(b) + 1; }
        foreach (var (block, n) in use.OrderByDescending(u => u.Value))
        {
            var at = BitConverter.ToInt32(lib, (block - 1) * 4);
            int dx = lib[at], dy = lib[at + 1], dz = lib[at + 2];
            var shapes = new SortedSet<int>(); var sounds = new SortedSet<int>(); var bricks = new List<int>();
            for (var k = 0; k < dx * dy * dz; k++)
            {
                var e = at + 3 + k * 4;
                shapes.Add(lib[e]); sounds.Add(lib[e + 1]);
                var brick = BitConverter.ToUInt16(lib, e + 2);
                if (brick != 0) bricks.Add(brick - 1);
            }
            Console.WriteLine($"block {block,3}: {dx}x{dy}x{dz} placed {n,4}; shapes {string.Join(",", shapes)} sounds {string.Join(",", sounds.Select(s => s.ToString("X2")))}; " +
                              $"{bricks.Count} bricks");
        }
        return 0;
    }

    // polarcodes <LBA1 folder>: the joined island's cells by their brick's code -- how many, how many are the top of their column, their
    // mean colour, their shapes -- to tell ground from objects.
    public static int Codes(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = LBAAssembler.Terrain.Polar.PolarLayout.Build(game);
        var tops = layout.Cells.GroupBy(c => (c.Key.X, c.Key.Z)).Select(g => g.MaxBy(c => c.Key.Y)).Select(c => c.Key).ToHashSet();
        foreach (var g in layout.Cells.GroupBy(c => c.Value.Code).OrderByDescending(g => g.Count()))
        {
            var cols = g.Select(c => LBAAssembler.Terrain.Polar.PolarLayout.BrickColour(game, c.Value.Brick)).ToList();
            Console.WriteLine($"code {g.Key:X2}: {g.Count(),6} cells, {g.Count(c => tops.Contains(c.Key)),5} tops, {g.Select(c => c.Value.Brick).Distinct().Count(),4} bricks; " +
                              $"colour ({cols.Average(c => c.R):0},{cols.Average(c => c.G):0},{cols.Average(c => c.B):0}); shapes {string.Join(",", g.Select(c => c.Value.Shape).Distinct().Order())}; " +
                              $"layers {g.Min(c => c.Key.Y)}..{g.Max(c => c.Key.Y)}");
        }
        return 0;
    }

    // polarshow <LBA1 folder> <out.png> <code,code...>: only the joined island's cells of those codes (hex), drawn as LBA1 draws them.
    public static int Show(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = LBAAssembler.Terrain.Polar.PolarLayout.Build(game);
        var codes = args[3].Split(',').Select(c => Convert.ToInt32(c, 16)).ToHashSet();
        var cells = layout.Cells.Where(c => codes.Contains(c.Value.Code)).Select(c => new Lba1Placement(c.Key.X, c.Key.Y, c.Key.Z, c.Value.Brick)).ToList();
        var image = Lba1GridRenderer.Render(new[] { new Lba1Tile(cells, 0, 0, 0) }, game.ReadBrick, game.Palette);
        PngWriter.Write(args[2], image.Bgra, image.Width, image.Height);
        Console.WriteLine($"{args[2]}: {cells.Count} cells, {image.Width} x {image.Height}");
        return 0;
    }

    // polarbrick <LBA1 folder> <brick>...: a brick sprite's header (width, lines, hot spot) and its opaque rows' extents.
    public static int Brick(string[] args)
    {
        var game = new Lba1Game(args[1]);
        foreach (var brick in args.Skip(2).Select(int.Parse))
        {
            var data = game.ReadBrick(brick)!;
            Console.WriteLine($"brick {brick}: {data[0]} x {data[1]}, hot ({(sbyte)data[2]}, {(sbyte)data[3]})");
            int p = 4;
            for (var line = 0; line < data[1]; line++)
            {
                int runs = data[p++], x = 0, first = -1, last = -1;
                for (var k = 0; k < runs; k++)
                {
                    int control = data[p++], length = (control & 0x3F) + 1;
                    switch (control >> 6)
                    {
                        case 0: x += length; break;
                        case 1: if (first < 0) first = x; x += length; last = x - 1; p += length; break;
                        default: if (first < 0) first = x; x += length; last = x - 1; p++; break;
                    }
                }
                if (line < 4 || line % 6 == 0 || line > data[1] - 3) Console.WriteLine($"  row {line,2}: {first}..{last}");
            }
        }
        return 0;
    }

    // polarterrain <LBA1 folder> <LBA2 folder (MOON.ILE, RESS.HQR)> <out folder> [palette entry]: Polar Island's ground (Terrain.Polar.PolarTerrain)
    // as POLAR.ILE in the out folder, and its map drawn (terrain view, 3 pixels a cell) beside it as POLAR_map.png.
    public static int Terrain(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = LBAAssembler.Terrain.Polar.PolarLayout.Build(game);
        if (args.Length > 4) LBAAssembler.Terrain.Polar.PolarTerrain.ChosenPalette = int.Parse(args[4]);
        var result = LBAAssembler.Terrain.Polar.PolarTerrain.Build(game, layout, args[2]);
        foreach (var line in layout.Log.Concat(result.Log)) Console.WriteLine(line);
        Directory.CreateDirectory(args[3]);
        var path = Path.Combine(args[3], LBAAssembler.Terrain.Polar.PolarTerrain.IleFile);
        File.WriteAllBytes(path, result.Island.ToBytes());
        var island = LBAAssembler.Terrain.IslandFile.Load(path);
        var renderer = new LBAAssembler.Terrain.IslandMapRenderer(island, LBAAssembler.Terrain.IslandMapRenderer.LoadPaletteEntry(args[2], LBAAssembler.Terrain.Polar.PolarTerrain.ChosenPalette), 3);
        renderer.RenderAll(LBAAssembler.Terrain.MapView.Terrain);
        var map = Path.Combine(args[3], "POLAR_map.png");
        PngWriter.Write(map, renderer.Pixels, renderer.PixelWidth, renderer.PixelHeight);
        Console.WriteLine($"{path}: {new FileInfo(path).Length} bytes, {island.Cubes.Count} cubes; {map}");
        return 0;
    }

    // polarinstall <LBA1 folder> <LBA2 game folder>: Polar Island built and written into the game folder (a sandbox's): POLAR.ILE/OBL and
    // island 12's sky and palette in RESS.HQR.
    public static int Install(string[] args)
    {
        var built = LBAAssembler.Terrain.Polar.PolarIsland.Build(args[1], args[2]);
        foreach (var line in LBAAssembler.Terrain.Polar.PolarIsland.Install(args[2], built)) Console.WriteLine(line);
        return 0;
    }

    // polarsea <LBA2 folder> <ILE> <cube x> <cube z> <cell x> <cell z>: a cell's polygon flags and texture corners (a retail sea cell).
    public static int Sea(string[] args)
    {
        var island = LBAAssembler.Terrain.IslandFile.Load(Path.Combine(args[1], args[2]));
        int cx = int.Parse(args[3]), cz = int.Parse(args[4]), x = int.Parse(args[5]), z = int.Parse(args[6]);
        for (var half = 0; half < 2; half++)
        {
            var s = LBAAssembler.Terrain.IslandGround.Pick(island, cx * 64 + x, cz * 64 + z, half)!;
            var p = s.Polygon;
            Console.WriteLine($"half {half}: bank {p.Bank} tex {p.TexFlag} poly {p.PolyFlag} step {p.SampleStep} code {p.CodeJeu} diag {p.Diagonal} col {p.Col} index {p.TextureIndex}; uv {string.Join(" ", (s.Texture ?? Array.Empty<ushort>()).Select(v => (v / 256.0).ToString("0.#")))}");
        }
        var cube = island.CubeAt(cx, cz)!;
        Console.WriteLine($"height {cube.Height(x, z)}, light {cube.Light(x, z)}, info {string.Join(",", cube.Info)}");
        return 0;
    }

    // polarobl <LBA2 folder> <OBL> [count]: the OBL's bodies -- points, faces, textured faces, their texture handles and UV ranges -- to see
    // how an island's objects take their textures.
    public static int Obl(string[] args)
    {
        var obl = LBAAssembler.HqrArchive.Open(Path.Combine(args[1], args[2]));
        var count = args.Length > 3 ? int.Parse(args[3]) : 12;
        for (var i = 0; i < Math.Min(count, LBAAssembler.HqrArchive.CountEntries(Path.Combine(args[1], args[2]))); i++)
        {
            LbaBodyStudio.Body body;
            try { body = LbaBodyStudio.Body.Read(obl.Read(i), 2, allowStatic: true); } catch (Exception e) { Console.WriteLine($"{i}: {e.Message}"); continue; }
            var tex = body.Faces.Where(f => f.Texture is not null).ToList();
            var uv = tex.SelectMany(f => f.Texture!.UV).ToList();
            foreach (var f in tex.Take(2)) Console.WriteLine($"    face colour {f.Colour} material {f.Material} type {f.Lba2Type} tone {f.DetailTone} points {f.Points.Length} uv {string.Join(",", f.Texture!.UV)}");
            Console.WriteLine($"{i}: {body.Vertices.Count} points, {body.Faces.Count} faces ({tex.Count} textured, handles {string.Join(",", tex.Select(f => f.Texture!.Handle).Distinct().Take(6))}; uv {(uv.Count > 0 ? $"{uv.Min()}..{uv.Max()}" : "-")}); textures [{string.Join(",", body.Textures.Take(4).Select(t => t.ToString("X")))}] static {body.Static} lit {body.Lit}");
        }
        return 0;
    }

    // polarobjects <LBA1 folder>: the island's object cells (not ground, above their column's ground): how many, their distinct bricks, and
    // their 6-connected pieces' sizes.
    public static int Objects(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = LBAAssembler.Terrain.Polar.PolarLayout.Build(game);
        var columns = LBAAssembler.Terrain.Polar.PolarTerrain.Columns(game, layout);
        var cells = layout.Cells.Where(c => !LBAAssembler.Terrain.Polar.PolarTerrain.IsGround(game, c.Value) && (!columns.TryGetValue((c.Key.X, c.Key.Z), out var col) || c.Key.Y > col.Top))
            .ToDictionary(c => c.Key, c => c.Value);
        Console.WriteLine($"{cells.Count} object cells, {cells.Values.Select(c => c.Brick).Distinct().Count()} distinct bricks");
        var seen = new HashSet<(int, int, int)>(); var sizes = new List<(int Cells, int W, int H, int D)>();
        foreach (var start in cells.Keys)
        {
            if (!seen.Add(start)) continue;
            var queue = new Queue<(int X, int Y, int Z)>(); queue.Enqueue(start); var part = new List<(int X, int Y, int Z)>();
            while (queue.Count > 0)
            {
                var c = queue.Dequeue(); part.Add(c);
                foreach (var (dx, dy, dz) in new[] { (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) })
                {
                    var n = (c.X + dx, c.Y + dy, c.Z + dz);
                    if (cells.ContainsKey(n) && seen.Add(n)) queue.Enqueue(n);
                }
            }
            sizes.Add((part.Count, part.Max(c => c.X) - part.Min(c => c.X) + 1, part.Max(c => c.Y) - part.Min(c => c.Y) + 1, part.Max(c => c.Z) - part.Min(c => c.Z) + 1));
        }
        Console.WriteLine($"{sizes.Count} pieces; cells per piece: max {sizes.Max(s => s.Cells)}, mean {sizes.Average(s => s.Cells):0.0}; footprint up to {sizes.Max(s => s.W)} x {sizes.Max(s => s.D)}, height up to {sizes.Max(s => s.H)}");
        foreach (var s in sizes.OrderByDescending(s => s.Cells).Take(12)) Console.WriteLine($"  {s.Cells} cells, {s.W} x {s.H} x {s.D}");
        return 0;
    }

    // polaratlas <LBA2 folder> <ILE> <out.png> [palette entry]: the island's object atlas (and ground atlas below it) in its palette.
    public static int AtlasPicture(string[] args)
    {
        var island = LBAAssembler.Terrain.IslandFile.Load(Path.Combine(args[1], args[2]));
        var palette = LBAAssembler.Terrain.IslandMapRenderer.LoadPaletteEntry(args[1], args.Length > 4 ? int.Parse(args[4]) : 39);
        var six = palette.Take(768).Max() <= 63;
        var px = new byte[256 * 512 * 4];
        for (var page = 0; page < 2; page++)
            for (var i = 0; i < 65536; i++)
            {
                var c = (page == 0 ? island.ObjectTexture : island.GroundTexture)[i];
                var o = (page * 65536 + i) * 4;
                px[o] = (byte)(palette[c * 3 + 2] * (six ? 4 : 1)); px[o + 1] = (byte)(palette[c * 3 + 1] * (six ? 4 : 1)); px[o + 2] = (byte)(palette[c * 3] * (six ? 4 : 1)); px[o + 3] = 255;
            }
        PngWriter.Write(args[3], px, 256, 512);
        Console.WriteLine($"{args[3]}: object atlas (top), ground atlas (bottom)");
        return 0;
    }

    // polarfoot <LBA1 folder> [count]: the object bricks most used, with the box each fills (PolarTextures.Sprite.Footprint) and how alike
    // its outline is to its whole cell's and to the box's.
    public static int Foot(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = LBAAssembler.Terrain.Polar.PolarLayout.Build(game);
        var columns = LBAAssembler.Terrain.Polar.PolarTerrain.Columns(game, layout);
        var objects = layout.Cells.Where(c => !LBAAssembler.Terrain.Polar.PolarTerrain.IsGround(game, c.Value) && (!columns.TryGetValue((c.Key.X, c.Key.Z), out var col) || c.Key.Y > col.Top));
        foreach (var g in objects.GroupBy(c => c.Value.Brick).OrderByDescending(g => g.Count()).Take(args.Length > 2 ? int.Parse(args[2]) : 30))
        {
            var sprite = PolarTextures.Sprite.Decode(game.ReadBrick(g.Key));
            var box = sprite.Footprint();
            var (full, best, fit) = PolarTextures.Sprite.LastFit;
            Console.WriteLine($"brick {g.Key,5} x{g.Count(),4} code {g.First().Value.Code:X2} shape {g.First().Value.Shape,2}: {(box.IsFull ? "full" : $"u {box.U0:0.00}..{box.U1:0.00} v {box.V0:0.00}..{box.V1:0.00}")}  likeness full {full:0.00} best {best:0.00} ({fit.U0:0.00}..{fit.U1:0.00}, {fit.V0:0.00}..{fit.V1:0.00})");
        }
        return 0;
    }

    // polarsheet <LBA1 folder> <out.png> <brick>...: the bricks' pictures side by side, four times their size, each over its cell's outline
    // (the top diamond and the two sides LBA1 draws) in grey.
    public static int Sheet(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var bricks = args.Skip(3).Select(int.Parse).ToList();
        const int k = 4, cw = 56, ch = 44;
        int w = cw * k * bricks.Count, h = ch * k;
        var px = new byte[w * h * 4];
        for (var i = 0; i < px.Length; i += 4) { px[i] = px[i + 1] = px[i + 2] = 40; px[i + 3] = 255; }
        var scale = game.Palette.Take(768).Max() <= 63 ? 4 : 1;
        for (var n = 0; n < bricks.Count; n++)
        {
            var sprite = PolarTextures.Sprite.Decode(game.ReadBrick(bricks[n]));
            void Put(int sx, int sy, byte r, byte g, byte b)
            {
                for (var dy = 0; dy < k; dy++) for (var dx = 0; dx < k; dx++)
                {
                    int x = (n * cw + sx + 4) * k + dx, y = (sy + 2) * k + dy;
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    var o = (y * w + x) * 4; px[o] = b; px[o + 1] = g; px[o + 2] = r;
                }
            }
            for (var t = 0.0; t <= 1; t += 0.01)
                foreach (var (u, v, hh) in new[] { (t, 0.0, 1.0), (t, 1.0, 1.0), (0.0, t, 1.0), (1.0, t, 1.0), (t, 1.0, 0.0), (1.0, t, 0.0), (1.0, 1.0, t), (0.0, 1.0, t), (1.0, 0.0, t) })
                {
                    var (x, y) = PolarTextures.Sprite.Project(u, v, hh);
                    Put((int)x, (int)y, 90, 90, 90);
                }
            for (var y = 0; y < sprite.Lines; y++)
                for (var x = 0; x < sprite.Width; x++)
                {
                    var c = sprite.Pixels[y * sprite.Width + x];
                    if (c < 0) continue;
                    Put(x + sprite.HotX, y + sprite.HotY, (byte)(game.Palette[c * 3] * scale), (byte)(game.Palette[c * 3 + 1] * scale), (byte)(game.Palette[c * 3 + 2] * scale));
                }
        }
        PngWriter.Write(args[2], px, w, h);
        Console.WriteLine($"{args[2]}: {string.Join(" ", bricks)}");
        return 0;
    }

    // polartall <LBA1 folder> [layers]: the ground columns that tall and taller, by the scene their top cell comes from, with their extents
    // in the island's cells.
    public static int Tall(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = PolarLayout.Build(game);
        var columns = PolarTerrain.Columns(game, layout);
        var min = args.Length > 2 ? int.Parse(args[2]) : 12;
        foreach (var g in columns.Where(c => c.Value.Top >= min).GroupBy(c => (c.Value.Cell.Scene, c.Value.Top)).OrderBy(g => g.Key))
            Console.WriteLine($"scene {g.Key.Scene,3} top {g.Key.Top,2}: {g.Count(),4} columns, layout x {g.Min(c => c.Key.X)}..{g.Max(c => c.Key.X)} z {g.Min(c => c.Key.Z)}..{g.Max(c => c.Key.Z)}");
        return 0;
    }

    // polaradd <LBA1 folder> <game folder> | polarremove <game folder>: Tools > LBA2: Polar Island's add (with its *.before-polar copies)
    // and remove.
    public static int Add(string[] args) { foreach (var line in PolarIsland.Add(args[1], args[2])) Console.WriteLine(line); return 0; }
    public static int Remove(string[] args) { foreach (var line in PolarIsland.Remove(args[1])) Console.WriteLine(line); return 0; }

    // polarsame <folder a> <folder b>: whether the shared files' entries (decoded) are the same in both, and which differ.
    public static int Same(string[] args)
    {
        var differ = 0;
        foreach (var f in PolarIsland.SharedFiles)
        {
            var a = HqrArchive.Open(Path.Combine(args[1], f)); var b = HqrArchive.Open(Path.Combine(args[2], f));
            int na = HqrArchive.CountEntries(Path.Combine(args[1], f)), nb = HqrArchive.CountEntries(Path.Combine(args[2], f));
            var bad = new List<int>();
            for (var i = 0; i < Math.Max(na, nb); i++)
            {
                byte[]? x = i < na && a.IsValid(i) ? a.Read(i) : null, y = i < nb && b.IsValid(i) ? b.Read(i) : null;
                if (x is null != y is null || x is not null && !x.AsSpan().SequenceEqual(y)) bad.Add(i);
            }
            Console.WriteLine($"{f}: {na} and {nb} entries, {(bad.Count == 0 ? "the same" : "differ at " + string.Join(", ", bad.Take(20)))}");
            differ += bad.Count + (na != nb ? 1 : 0);
        }
        return differ == 0 ? 0 : 1;
    }

    // polarcubes <LBA1 folder>: for each of the island's cubes, the LBA1 scenes its ground comes from (columns of each), to name its scene.
    public static int Cubes(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = PolarLayout.Build(game);
        var columns = PolarTerrain.Columns(game, layout);
        int minX = columns.Keys.Min(k => k.X), maxX = columns.Keys.Max(k => k.X), minZ = columns.Keys.Min(k => k.Z), maxZ = columns.Keys.Max(k => k.Z);
        foreach (var g in columns.Where(c => !c.Value.Water).GroupBy(c => (PolarTerrain.IslandX(c.Key.X) / 64, PolarTerrain.IslandZ(c.Key.Z) / 64)).OrderBy(g => PolarScenes.SceneOf(g.Key.Item1, g.Key.Item2)))
            Console.WriteLine($"scene {PolarScenes.SceneOf(g.Key.Item1, g.Key.Item2)} cube {g.Key}: {string.Join(", ", g.GroupBy(c => c.Value.Cell.Scene).OrderByDescending(h => h.Count()).Select(h => $"{h.Key} x{h.Count()}"))}");
        return 0;
    }

    // polartiles <LBA1 folder>: how many distinct top bricks the island's ground uses, how many cells each, and per cube.
    public static int Tiles(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = PolarLayout.Build(game);
        var columns = PolarTerrain.Columns(game, layout);
        int minX = columns.Keys.Min(k => k.X), maxX = columns.Keys.Max(k => k.X), minZ = columns.Keys.Min(k => k.Z), maxZ = columns.Keys.Max(k => k.Z);
        var land = columns.Where(c => !c.Value.Water).ToList();
        var byBrick = land.GroupBy(c => c.Value.Cell.Brick).OrderByDescending(g => g.Count()).ToList();
        Console.WriteLine($"{land.Count} land columns, {byBrick.Count} distinct top bricks");
        var total = 0; var k = 0;
        foreach (var g in byBrick) { total += g.Count(); k++; if (k is 16 or 32 or 48 or 64 or 96 or 128) Console.WriteLine($"  top {k} bricks cover {100.0 * total / land.Count:0.0}%"); }
        foreach (var g in land.GroupBy(c => (PolarTerrain.IslandX(c.Key.X) / 64, PolarTerrain.IslandZ(c.Key.Z) / 64)).OrderBy(g => g.Key))
            Console.WriteLine($"cube {g.Key}: {g.Count()} land columns, {g.Select(c => c.Value.Cell.Brick).Distinct().Count()} top bricks");
        Console.WriteLine("most used: " + string.Join(", ", byBrick.Take(24).Select(g => $"{g.Key} x{g.Count()} ({g.First().Value.Cell.Code:X2})")));
        return 0;
    }

    // polarpalette <LBA1 folder> <game folder>: for each retail island palette (RESS.HQR's XPL entries), whether its normal light row
    // (ShadeNormalLevel) leaves colours as they are, and how far its nearest colours are from LBA1's on the island's ground and objects
    // (the mean RGB distance over every drawn pixel of the bricks used, by how often).
    public static int Palette(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = PolarLayout.Build(game);
        var weights = new Dictionary<int, long>();
        foreach (var c in layout.Cells.Values)
        {
            var sprite = PolarTextures.Sprite.Decode(game.ReadBrick(c.Brick));
            foreach (var px in sprite.Pixels) if (px >= 0) weights[px] = weights.GetValueOrDefault(px) + 1;
        }
        var ress = HqrArchive.Open(Path.Combine(args[2], "RESS.HQR"));
        foreach (var entry in new[] { 27, 29, 30, 31, 32, 33, 34, 35, 36, 37, 42 })
        {
            if (!ress.IsValid(entry)) continue;
            var xpl = ress.Read(entry);
            int offPal = BitConverter.ToInt32(xpl, 4), offFog = BitConverter.ToInt32(xpl, 12), normal = BitConverter.ToInt32(xpl, 24);
            var pal = xpl[offPal..(offPal + 768)];
            var identity = Enumerable.Range(0, 256).Count(t => xpl[offFog + normal * 256 + t] == t);
            var colours = new PolarTextures.Colours(game.Palette, pal);
            double err = 0; long n = 0;
            foreach (var (c, w) in weights)
            {
                var (r, g, b) = PolarTextures.Colours.Rgb(game.Palette, c);
                var (pr, pg, pb) = PolarTextures.Colours.Rgb(pal, colours.Nearest[c]);
                err += Math.Sqrt((r - pr) * (r - pr) + (g - pg) * (g - pg) + (b - pb) * (b - pb)) * w; n += w;
            }
            Console.WriteLine($"palette {entry}: normal row {normal} keeps {identity}/256 colours; mean distance to LBA1's {err / n:0.0}");
        }
        return 0;
    }

    // polarwater <LBA1 folder>: the palette colours LBA1's water bricks are drawn in, and how much of the crystal's and of the rocks in the
    // water's bricks are those colours.
    public static int Water(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = PolarLayout.Build(game);
        Dictionary<int, long> Count(IEnumerable<int> bricks)
        {
            var counts = new Dictionary<int, long>();
            foreach (var b in bricks.Distinct())
                foreach (var px in PolarTextures.Sprite.Decode(game.ReadBrick(b)).Pixels) if (px >= 0) counts[px] = counts.GetValueOrDefault(px) + 1;
            return counts;
        }
        var water = Count(layout.Cells.Values.Where(c => c.Water).Select(c => c.Brick));
        var crystal = Count(layout.Cells.Values.Where(c => (c.Code & 0xF0) == 0xA0).Select(c => c.Brick));
        var total = water.Values.Sum();
        Console.WriteLine($"water bricks: {water.Count} colours: " + string.Join(" ", water.OrderByDescending(k => k.Value).Select(k => $"{k.Key}:{100.0 * k.Value / total:0.0}%")));
        Console.WriteLine($"crystal bricks: {crystal.Values.Sum()} pixels, {crystal.Where(k => water.ContainsKey(k.Key)).Sum(k => k.Value)} in water colours; colours " + string.Join(" ", crystal.OrderByDescending(k => k.Value).Take(20).Select(k => $"{k.Key}")));
        return 0;
    }

    // polarview <game folder> <out.png> <x> <z> [alpha beta distance wide]: the island (POLAR.ILE) as the native renderer draws it, looking
    // at world (x, z) -- island cells x 512 -- at its ground's height, from the camera's angles (4096 to a turn) and distance; `wide` cubes
    // round it drawn too. LBA1's view: alpha 341 (30 degrees down), a far distance.
    public static int View(string[] args)
    {
        var game = args[1];
        var dll = Environment.GetEnvironmentVariable("LBA2_RENDERER_DLL") ??
                  @"E:\dump\LBAAssembler\native\lba2-classic-community\out\build\windows_ucrt64_static\SOURCES\3DEXT\liblba2_renderer.dll";
        int x = int.Parse(args[3]), z = int.Parse(args[4]);
        int alpha = args.Length > 5 ? int.Parse(args[5]) : 341, beta = args.Length > 6 ? int.Parse(args[6]) : 512, distance = args.Length > 7 ? int.Parse(args[7]) : 40000;
        var wide = args.Length > 8 ? int.Parse(args[8]) : 1;
        var name = Environment.GetEnvironmentVariable("VIEW_ISLAND") ?? "POLAR";   // (another island: VIEW_ISLAND=KNARTAS)
        var island = LBAAssembler.Terrain.IslandFile.Load(Path.Combine(game, name + ".ILE"));
        var y = LBAAssembler.Terrain.IslandOps.Altitude(island, x, z) ?? 0;
        // (VIEW_Y=<height>: the point looked at that high -- an island whose objects stand over its ground, the Temple of Bú's)
        if (Environment.GetEnvironmentVariable("VIEW_Y") is { Length: > 0 } viewY) y = double.Parse(viewY);
        using var lib = new RendererLibraryApi(dll);
        if (!lib.IsLoaded || !lib.SetDataRoot(game) || !lib.Initialize()) { Console.WriteLine("native init failed"); return 2; }
        if (lib.LoadIsland(name.ToLowerInvariant()) == 0) { Console.WriteLine("LoadIsland failed"); return 2; }
        lib.SetDrawSky(true); lib.SetDrawSea(true);
        if (Environment.GetEnvironmentVariable("FAR") is { } far && far != "editor") { var f = int.Parse(far); lib.SetViewDistance(f * 4 / 5, f); }
        // (AREA=x0,z0,x1,z1: those cubes whichever the camera is over, as the editor's view draws them)
        // (AREA=auto: the island's cubes and a ring of 2 round them, as the editor's IslandArea)
        var areaText = Environment.GetEnvironmentVariable("AREA");
        if (areaText == "auto")
        {
            int ax0 = 16, az0 = 16, ax1 = -1, az1 = -1;
            for (var cz = 0; cz < 16; cz++)
                for (var cx = 0; cx < 16; cx++)
                    if (island.CubeAt(cx, cz) is not null) { ax0 = Math.Min(ax0, cx); az0 = Math.Min(az0, cz); ax1 = Math.Max(ax1, cx); az1 = Math.Max(az1, cz); }
            areaText = $"{Math.Max(0, ax0 - 2)},{Math.Max(0, az0 - 2)},{Math.Min(15, ax1 + 2)},{Math.Min(15, az1 + 2)}";
            Console.WriteLine($"area {areaText}");
        }
        var areaCubes = areaText?.Split(',').Select(int.Parse).ToArray();
        // (DISTANCES=d1,d2,...: one frame at each distance in turn, in the one session -- as the editor's zoom does -- <out>_<n>.png each)
        var distances = Environment.GetEnvironmentVariable("DISTANCES")?.Split(',').Select(int.Parse).ToArray() ?? new[] { distance };
        var palette = LBAAssembler.Terrain.IslandMapRenderer.LoadPalette(game, name);
        for (var n = 0; n < distances.Length; n++)
        {
            // (FAR=editor: the view distance the editor's 3D view gives that camera -- RendererLibraryApi.EditorViewDistance)
            if (Environment.GetEnvironmentVariable("FAR") == "editor")
            {
                var (startFog, clipFar) = RendererLibraryApi.EditorViewDistance(distances[n], x, z, areaCubes is { Length: 4 } c ? (c[0], c[1], c[2], c[3]) : null);
                lib.SetViewDistance(startFog, clipFar);
            }
            lib.SetViewTarget(x, (int)y, z);
            lib.SetCamera(alpha, beta, 0, distances[n]);
            if ((areaCubes is { Length: 4 } a ? lib.RenderFrameArea(a[0], a[1], a[2], a[3]) : wide > 0 ? lib.RenderFrameWide(wide) : lib.RenderFrame()) == 0) { Console.WriteLine("render failed"); return 2; }
            var outPath = distances.Length > 1 ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[2]))!, $"{Path.GetFileNameWithoutExtension(args[2])}_{n}.png") : args[2];
            WriteView(lib, palette, outPath);
            Console.WriteLine($"{outPath}: target ({x}, {y}, {z}), alpha {alpha} beta {beta} distance {distances[n]}");
        }
        return 0;
    }

    static void WriteView(RendererLibraryApi lib, byte[] palette, string outPath)
    {
        var p = lib.GetFramebuffer(out var w, out var h, out var pitch);
        var six = palette.Take(768).Max() <= 63;
        var px = new byte[w * h * 4];
        var row = new byte[w];
        var nonZero = 0;
        for (var r = 0; r < h; r++)
        {
            System.Runtime.InteropServices.Marshal.Copy(p + r * pitch, row, 0, w);
            for (var c = 0; c < w; c++)
            {
                if (row[c] != 0) nonZero++;
                var i = (r * w + c) * 4; var k = row[c] * 3;
                px[i] = (byte)(palette[k + 2] * (six ? 4 : 1)); px[i + 1] = (byte)(palette[k + 1] * (six ? 4 : 1)); px[i + 2] = (byte)(palette[k] * (six ? 4 : 1)); px[i + 3] = 255;
            }
        }
        PngWriter.Write(outPath, px, w, h);
        // (none: the editor's view takes such a frame for a failed render -- CommunityRendererBackend.RenderIslandDirect)
        Console.Write($"[{nonZero} pixels not index 0] ");
    }

    // polarprobe <LBA1 folder> <x0> <x1> <z0> <z1>: the layout's columns there -- each cell from the top down: layer, scene, brick, code.
    public static int Probe(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = PolarLayout.Build(game);
        int x0 = int.Parse(args[2]), x1 = int.Parse(args[3]), z0 = int.Parse(args[4]), z1 = int.Parse(args[5]);
        for (var z = z0; z <= z1; z++)
            for (var x = x0; x <= x1; x++)
            {
                var cells = layout.Cells.Where(c => c.Key.X == x && c.Key.Z == z).OrderByDescending(c => c.Key.Y).Take(4)
                    .Select(c => $"{c.Key.Y}:{c.Value.Scene}/{c.Value.Brick}/{c.Value.Code:X2}");
                Console.WriteLine($"({x},{z}) " + string.Join("  ", cells));
            }
        return 0;
    }

    // polarregion <LBA1 folder> <out.png> <scene|joined> <x0> <x1> <z0> <z1>: a region of one scene's own grid (its cells x, z), or of the
    // joined island (the layout's), drawn as LBA1 draws it.
    // polarcols <LBA1> <scene>...: each scene's columns, a character a column by its top brick's code (~ water, . none), then the codes and heights
    public static int Cols(string[] args)
    {
        var game = new Lba1Game(args[1]);
        foreach (var scene in args.Skip(2).Select(int.Parse))
        {
            var cells = PolarLayout.Bricks(game, scene);
            var tops = cells.GroupBy(c => (c.X, c.Z)).ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.Y).First());
            Console.WriteLine($"scene {scene}: {cells.Count} bricks, x {cells.Min(c => c.X)}..{cells.Max(c => c.X)} z {cells.Min(c => c.Z)}..{cells.Max(c => c.Z)} y {cells.Min(c => c.Y)}..{cells.Max(c => c.Y)}");
            foreach (var g in tops.Values.GroupBy(c => (c.Code, c.Y)).OrderByDescending(g => g.Count()).Take(14)) Console.WriteLine($"  code {g.Key.Code:X2} layer {g.Key.Y}: {g.Count()} columns");
            for (var z = 0; z < 64; z++)
            {
                var line = new System.Text.StringBuilder();
                for (var x = 0; x < 64; x++)
                {
                    if (!tops.TryGetValue((x, z), out var c)) { line.Append('.'); continue; }
                    line.Append(c.Code == 0xF1 ? '~' : c.Y > 35 ? '#' : "0123456789abcdefghijklmnopqrstuvwxyz"[c.Y]);
                }
                Console.WriteLine($"{z,3} {line}");
            }
        }
        return 0;
    }

    // waterpatches <ILE> [most]: patches of cells that are water by their game code, or undrawn, with their size, middle, height and how they are drawn
    // altgrid <ILE> <cell x0> <x1> <z0> <z1>: the ground's height at each cell, a row of z each
    public static int AltGrid(string[] args)
    {
        var island = IslandFile.Load(args[1]);
        var r = args[2..6].Select(int.Parse).ToArray();
        for (var z = r[2]; z <= r[3]; z++)
            Console.WriteLine($"{z,4}: " + string.Join(" ", Enumerable.Range(r[0], r[1] - r[0] + 1).Select(x => $"{IslandOps.Altitude(island, x * 512.0, z * 512.0),5:F0}")));
        return 0;
    }

    // polarblockorder <LBA1 folder> <scene> <block>: where each position of a block's first placing falls
    public static int BlockOrder(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var scene = int.Parse(args[2]); var block = int.Parse(args[3]);
        var cells = Lba1GridCodec.Decode(game.ReadGrid(scene));
        var (dx, dy, dz) = LBAAssembler.Terrain.Polar.PolarProps.Size(game.ReadBlocks(scene), block);
        Console.WriteLine($"block {block}: {dx} x {dy} x {dz}");
        for (var z = 0; z < 64; z++)
        for (var x = 0; x < 64; x++)
        for (var y = 0; y < 25; y++)
        {
            var i = ((z * 64 + x) * 25 + y) * 2;
            if (cells[i] != block || cells[i + 1] != 0) continue;
            for (var ky = 0; ky < dy; ky++)
            {
                var line = new List<string>();
                for (var kz = 0; kz < dz; kz++)
                for (var kx = 0; kx < dx; kx++)
                {
                    var j = (((z + kz) * 64 + x + kx) * 25 + y + ky) * 2;
                    line.Add($"({kx},{kz}):{cells[j]}/{cells[j + 1]}");
                }
                Console.WriteLine($"layer {ky}: " + string.Join(" ", line));
            }
            return 0;
        }
        return 0;
    }

    // polarplaced <LBA1 folder>: where the things made from their pictures stand on the island: cube, and world x, y, z of their foot
    public static int Placed(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = LBAAssembler.Terrain.Polar.PolarLayout.Build(game);
        foreach (var p in layout.Props.OrderBy(p => p.Kind.Block).ThenBy(p => p.X))
        {
            var (dx, dy, dz) = LBAAssembler.Terrain.Polar.PolarProps.Size(game.ReadBlocks(p.Kind.Scene), p.Kind.Block);
            var x = (LBAAssembler.Terrain.Polar.PolarTerrain.IslandX(p.X) + dx * LBAAssembler.Terrain.Polar.PolarTerrain.Scale / 2.0) * 512;
            var z = (LBAAssembler.Terrain.Polar.PolarTerrain.IslandZ(p.Z) + dz * LBAAssembler.Terrain.Polar.PolarTerrain.Scale / 2.0) * 512;
            Console.WriteLine($"scene {p.Kind.Scene} block {p.Kind.Block} cell ({p.X},{p.Y},{p.Z}): world {x:0} {LBAAssembler.Terrain.Polar.PolarTerrain.LayerY(p.Y - 1)} {z:0}");
        }
        return 0;
    }

    // polarscenecells <LBA1 folder> <scene> <x0> <x1> <z0> <z1>: a scene's grid there, by layer: block/position of each cell
    public static int SceneCells(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var cells = Lba1GridCodec.Decode(game.ReadGrid(int.Parse(args[2])));
        int x0 = int.Parse(args[3]), x1 = int.Parse(args[4]), z0 = int.Parse(args[5]), z1 = int.Parse(args[6]);
        for (var y = 0; y < 25; y++)
        {
            var lines = new List<string>();
            for (var z = z0; z <= z1; z++)
            {
                var line = new List<string>();
                for (var x = x0; x <= x1; x++)
                {
                    var i = ((z * 64 + x) * 25 + y) * 2;
                    line.Add(cells[i] == 0 ? "      ." : $"{cells[i],3}/{cells[i + 1],-3}");
                }
                lines.Add($"z {z,2}: " + string.Join(" ", line));
            }
            if (lines.Any(l => l.Contains('/'))) { Console.WriteLine($"layer {y}, x from {x0}"); foreach (var l in lines) Console.WriteLine(l); }
        }
        return 0;
    }

    // polarrows <LBA1 folder> <scene> <block>: a block's picture row by row: where its pixels run, against its block's middle column
    public static int Rows(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var lib = game.ReadBlocks(int.Parse(args[2]));
        var block = int.Parse(args[3]);
        var (dx, dy, dz) = LBAAssembler.Terrain.Polar.PolarProps.Size(lib, block);
        var p = LBAAssembler.Terrain.Polar.PolarProps.Draw(game, lib, block);
        Console.WriteLine($"block {block}: {dx} x {dy} x {dz}, picture {p.Width} x {p.Height}, origin {p.OriginX},{p.OriginY}");
        var middle = p.OriginX + 24 + 24 * (dx - dz) / 2.0;
        var mid = 12 * (dx + dz) / 2.0 + p.OriginY + 15;
        for (var row = 0; row < p.Height; row += args.Length > 4 ? int.Parse(args[4]) : 2)
        {
            int l = -1, r = -1, n = 0;
            for (var x = 0; x < p.Width; x++) if (p.Pixels[row * p.Width + x] >= 0) { if (l < 0) l = x; r = x; n++; }
            var y = (mid - row) / 15.0;
            var c = p.Pixels[row * p.Width + Math.Clamp((int)middle, 0, p.Width - 1)];
            var lum = c < 0 ? -1 : (game.Palette[c * 3] + game.Palette[c * 3 + 1] + game.Palette[c * 3 + 2]) / 3;
            Console.WriteLine($"row {row,3} y {y,5:0.00}  left {l - middle,6:0.0} right {r - middle,6:0.0}  half {(r - l + 1) / 2.0,5:0.0} = r {(r - l + 1) / 2.0 / 33.94,4:0.00}  pixels {n,3} middle {lum}");
        }
        return 0;
    }

    // polarpreview <LBA1 folder> <out.png> <scene> <block> [zoom]: a Fortress body without a build: LBA1's picture, the body from LBA1's angle, and from three low angles
    public static int Preview(string[] args)
    {
        var game = new Lba1Game(args[1]);
        int scene = int.Parse(args[3]), block = int.Parse(args[4]), zoom = args.Length > 5 ? int.Parse(args[5]) : 3;
        var kind = LBAAssembler.Terrain.Polar.PolarProps.Kinds.First(k => k.Scene == scene && k.Block == block);
        var (shown, points, faces) = LBAAssembler.Terrain.Polar.PolarProps.Shape(game, kind);
        var picture = LBAAssembler.Terrain.Polar.PolarProps.Draw(game, game.ReadBlocks(scene), block);
        const int S = LBAAssembler.Terrain.Polar.PolarTerrain.Scale;
        var cells = points.Select(v => (X: v.X / (512.0 * S), Y: v.Y / (256.0 * S), Z: v.Z / (512.0 * S))).ToArray();
        var (dx, dy, dz) = LBAAssembler.Terrain.Polar.PolarProps.Size(game.ReadBlocks(scene), block);
        var reach = 34 * zoom * (Math.Max(dx, dz) * 0.9 + dy * 0.4 + 0.5);
        int cw = (int)(reach * 2), ch = (int)(reach * 2);
        var views = new (double Yaw, double Pitch)[] { (45, 30), (135, 12), (225, 12), (315, 12) };
        int stripW = picture.Width * zoom + views.Length * cw;
        var strip = new byte[stripW * ch * 4];
        void Put(int x, int y, int colour)
        {
            if (x < 0 || y < 0 || x >= stripW || y >= ch) return;
            var i = (y * stripW + x) * 4;
            strip[i] = game.Palette[colour * 3 + 2]; strip[i + 1] = game.Palette[colour * 3 + 1]; strip[i + 2] = game.Palette[colour * 3]; strip[i + 3] = 255;
        }
        for (var i = 3; i < strip.Length; i += 4) { strip[i - 3] = 150; strip[i - 2] = 120; strip[i - 1] = 90; strip[i] = 255; }
        for (var y = 0; y < picture.Height * zoom && y < ch; y++)
            for (var x = 0; x < picture.Width * zoom; x++)
            {
                var c = picture.Pixels[y / zoom * picture.Width + x / zoom];
                if (c >= 0) Put(x, y, c);
            }
        for (var v = 0; v < views.Length; v++)
        {
            var depth = new double[cw * ch];
            Array.Fill(depth, double.MinValue);
            double yaw = views[v].Yaw * Math.PI / 180, pitch = views[v].Pitch * Math.PI / 180;
            var left = picture.Width * zoom + v * cw;
            (double U, double V, double D) Screen((double X, double Y, double Z) p)
            {
                double u = p.X * Math.Cos(yaw) - p.Z * Math.Sin(yaw), d = p.X * Math.Sin(yaw) + p.Z * Math.Cos(yaw), h = p.Y / 2;
                return (cw / 2 + u * 33.94 * zoom, ch * 0.72 + (d * Math.Sin(pitch) - h * Math.Cos(pitch)) * 33.94 * zoom, d * Math.Cos(pitch) + h * Math.Sin(pitch));
            }
            foreach (var f in faces)
            {
                var uv = f.Texture!.UV;
                for (var t = 1; t + 1 < f.Points.Length; t++)
                {
                    int[] c = { 0, t, t + 1 };
                    var s = c.Select(k => Screen(cells[f.Points[k]])).ToArray();
                    var area = (s[1].U - s[0].U) * (s[2].V - s[0].V) - (s[2].U - s[0].U) * (s[1].V - s[0].V);
                    if (Math.Abs(area) < 1e-6) continue;
                    int x0 = (int)Math.Max(0, s.Min(q => q.U)), x1 = (int)Math.Min(cw - 1, s.Max(q => q.U)), y0 = (int)Math.Max(0, s.Min(q => q.V)), y1 = (int)Math.Min(ch - 1, s.Max(q => q.V));
                    for (var y = y0; y <= y1; y++)
                        for (var x = x0; x <= x1; x++)
                        {
                            double px = x + 0.5, py = y + 0.5;
                            var w1 = ((px - s[0].U) * (s[2].V - s[0].V) - (s[2].U - s[0].U) * (py - s[0].V)) / area;
                            var w2 = ((s[1].U - s[0].U) * (py - s[0].V) - (px - s[0].U) * (s[1].V - s[0].V)) / area;
                            var w0 = 1 - w1 - w2;
                            if (w0 < 0 || w1 < 0 || w2 < 0) continue;
                            var d = w0 * s[0].D + w1 * s[1].D + w2 * s[2].D;
                            if (d <= depth[y * cw + x]) continue;
                            var tu = (w0 * uv[c[0] * 2] + w1 * uv[c[1] * 2] + w2 * uv[c[2] * 2]) / 256;
                            var tv = (w0 * uv[c[0] * 2 + 1] + w1 * uv[c[1] * 2 + 1] + w2 * uv[c[2] * 2 + 1]) / 256;
                            var col = shown.Pixels[Math.Clamp((int)tv, 0, shown.Height - 1) * shown.Width + Math.Clamp((int)tu, 0, shown.Width - 1)];
                            if (col < 0) continue;
                            depth[y * cw + x] = d;
                            Put(left + x, y, col);
                        }
                }
            }
        }
        PngWriter.Write(args[2], strip, stripW, ch);
        Console.WriteLine($"{kind.Form}: {faces.Count} faces, {points.Count} points; {stripW} x {ch}");
        return 0;
    }

    // groundpeaks <ILE> <cell x0> <x1> <z0> <z1> [step]: the ground's heights there in hundreds (island cells), and its summits
    public static int GroundPeaks(string[] args)
    {
        var island = IslandFile.Load(args[1]);
        int x0 = int.Parse(args[2]), x1 = int.Parse(args[3]), z0 = int.Parse(args[4]), z1 = int.Parse(args[5]), step = args.Length > 6 ? int.Parse(args[6]) : 2;
        double At(int x, int z) => IslandOps.Altitude(island, x * 512.0, z * 512.0) ?? -1;
        Console.WriteLine("        " + string.Concat(Enumerable.Range(0, (x1 - x0) / step + 1).Select(i => $"{x0 + i * step,4}")));
        for (var z = z0; z <= z1; z += step)
            Console.WriteLine($"z {z,4}: " + string.Concat(Enumerable.Range(0, (x1 - x0) / step + 1).Select(i => $"{At(x0 + i * step, z) / 100,4:0}")));
        const int Reach = 4;
        for (var z = z0; z <= z1; z++)
            for (var x = x0; x <= x1; x++)
            {
                var h = At(x, z);
                var top = true;
                for (var dz = -Reach; dz <= Reach && top; dz++)
                    for (var dx = -Reach; dx <= Reach && top; dx++)
                        if ((dx != 0 || dz != 0) && At(x + dx, z + dz) > h) top = false;
                if (top) Console.WriteLine($"summit at cell ({x},{z}): {h:0}");
            }
        return 0;
    }

    // boxesat <ILE> <cell x0> <x1> <z0> <z1> <y0> <y1>: the decors' collision boxes crossing that block of the island (world cells; y in units)
    public static int BoxesAt(string[] args)
    {
        var island = IslandFile.Load(args[1]);
        double x0 = double.Parse(args[2]), x1 = double.Parse(args[3]), z0 = double.Parse(args[4]), z1 = double.Parse(args[5]), y0 = double.Parse(args[6]), y1 = double.Parse(args[7]);
        for (var cz = 0; cz < 16; cz++)
            for (var cx = 0; cx < 16; cx++)
            {
                if (island.CubeAt(cx, cz) is not { } cube) continue;
                foreach (var d in cube.Decors)
                {
                    double bx0 = cx * 64 + d.XMin / 512.0, bx1 = cx * 64 + d.XMax / 512.0, bz0 = cz * 64 + d.ZMin / 512.0, bz1 = cz * 64 + d.ZMax / 512.0;
                    if (bx1 < x0 || bx0 > x1 || bz1 < z0 || bz0 > z1 || d.YMax < y0 || d.YMin > y1) continue;
                    Console.WriteLine($"cube ({cx},{cz}) body {d.Body & 0xFFFF,4}: x {bx0,7:0.0}..{bx1,7:0.0} z {bz0,7:0.0}..{bz1,7:0.0} y {d.YMin,6}..{d.YMax,6}");
                }
            }
        return 0;
    }

    // issuetest bug|feature <words>: IssueReport.Send, as Help's window does (LBRA_REPORT_URL for the local stand-in)
    public static int IssueTest(string[] args)
    {
        var said = LBAAssembler.IssueReport.Send(args[1] == "feature", args[2]).GetAwaiter().GetResult();
        Console.WriteLine(said ?? "sent");
        return said is null ? 0 : 1;
    }

    // texmap <ILE>: how much of the ground's texture page each 8 x 8 block is read: triangles, as a digit (log 2), . none; the cheapest places for a block of 6 x 4
    public static int TexMap(string[] args)
    {
        var island = IslandFile.Load(args[1]);
        if (args.Length > 3)
        {
            // texmap <ILE> <x> <y>: the page's pixels there, 48 x 32, as bytes
            int px = int.Parse(args[2]), py = int.Parse(args[3]);
            for (var y = py; y < py + 32; y += 2) Console.WriteLine(string.Concat(Enumerable.Range(px, 48).Where(x => (x - px) % 2 == 0).Select(x => island.GroundTexture[y * 256 + x].ToString("x2") + " ")));
            return 0;
        }
        var used = new int[32, 32];
        foreach (var (_, _, cube) in IslandOps.CubeCells(island))
        {
            if (!cube.HasPolygons) continue;
            var t = cube.TextureDefs;
            for (var z = 0; z < IslandCube.Cells; z++)
            for (var x = 0; x < IslandCube.Cells; x++)
            for (var half = 0; half < 2; half++)
            {
                var p = new IslandPolygon(cube.Polygon(x, z, half));
                if (p.TexFlag == 0 || island.GroundTextureOf(p) is not (0, var d)) continue;
                var i = d * 6;
                if (i + 5 >= t.Length) continue;
                int u0 = 65535, v0 = 65535, u1 = 0, v1 = 0;
                for (var k = 0; k < 3; k++) { u0 = Math.Min(u0, t[i + k * 2]); u1 = Math.Max(u1, t[i + k * 2]); v0 = Math.Min(v0, t[i + k * 2 + 1]); v1 = Math.Max(v1, t[i + k * 2 + 1]); }
                for (var v = v0 / 2048; v <= Math.Min(31, (v1 - 1) / 2048); v++)
                for (var u = u0 / 2048; u <= Math.Min(31, (u1 - 1) / 2048); u++) used[u, v]++;
            }
        }
        for (var y = 0; y < 32; y++)
            Console.WriteLine(string.Concat(Enumerable.Range(0, 32).Select(x => used[x, y] == 0 ? "." : ((int)Math.Log2(used[x, y])).ToString("x"))));
        var places = new List<(int Cost, int X, int Y)>();
        for (var y = 0; y + 4 <= 32; y++)
        for (var x = 0; x + 6 <= 32; x++)
        {
            var cost = 0;
            for (var v = y; v < y + 4; v++) for (var u = x; u < x + 6; u++) cost += used[u, v];
            places.Add((cost, x, y));
        }
        foreach (var c in places.OrderBy(c => c.Cost).Take(5)) Console.WriteLine($"block ({c.X},{c.Y}): {c.Cost} triangle-blocks read");
        return 0;
    }

    // polarcells <LBA1 folder> <x0> <x1> <z0> <z1>: the ground's top layer of each column of the layout, and what is above it (blocks by layer)
    public static int GroundCells(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = LBAAssembler.Terrain.Polar.PolarLayout.Build(game);
        var columns = LBAAssembler.Terrain.Polar.PolarTerrain.Columns(game, layout);
        int x0 = int.Parse(args[2]), x1 = int.Parse(args[3]), z0 = int.Parse(args[4]), z1 = int.Parse(args[5]);
        for (var z = z0; z <= z1; z++)
        {
            var line = new List<string>();
            for (var x = x0; x <= x1; x++) line.Add(columns.TryGetValue((x, z), out var c) ? (c.Water ? "  w" : $"{c.Top,3}") : "  .");
            Console.WriteLine($"z{z,4}: " + string.Join("", line));
        }
        if (args.Length > 6)
            foreach (var k in layout.Cells.Where(k => k.Key.Item1 >= x0 && k.Key.Item1 <= x1 && k.Key.Item3 >= z0 && k.Key.Item3 <= z1).OrderBy(k => k.Key.Item3).ThenBy(k => k.Key.Item1).ThenBy(k => k.Key.Item2))
                Console.WriteLine($"({k.Key.Item1},{k.Key.Item2},{k.Key.Item3}) block {k.Value.Block} code {k.Value.Code:X2} brick {k.Value.Brick}");
        return 0;
    }

    // polarblocktiles <LBA1 folder> <scene> <block> <out.png>: a block's bricks' top tiles side by side, as the ground has them, and each brick's picture beside it, 6 times the size
    public static int BlockTiles(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var scene = int.Parse(args[2]); var block = int.Parse(args[3]);
        var lib = game.ReadBlocks(scene);
        var at = BitConverter.ToInt32(lib, (block - 1) * 4);
        int dx = lib[at], dy = lib[at + 1], dz = lib[at + 2];
        const int K = 6, T = LBAAssembler.Terrain.Polar.PolarTextures.TileSize, W = (T + 52) * K;
        var n = dx * dy * dz;
        var px = new byte[W * n * K * 42 * 4];
        void Dot(int x, int y, int c)
        {
            var i = (y * W * n + x) * 4;
            px[i] = c < 0 ? (byte)255 : game.Palette[c * 3 + 2]; px[i + 1] = c < 0 ? (byte)0 : game.Palette[c * 3 + 1]; px[i + 2] = c < 0 ? (byte)255 : game.Palette[c * 3]; px[i + 3] = 255;
        }
        for (var pos = 0; pos < n; pos++)
        {
            var number = BitConverter.ToUInt16(lib, at + 3 + pos * 4 + 2);
            if (number == 0) continue;
            var s = LBAAssembler.Terrain.Polar.PolarTextures.Sprite.Decode(game.ReadBrick(number - 1));
            var tile = s.Tile(LBAAssembler.Terrain.Polar.PolarTextures.Face.Top);
            for (var y = 0; y < T * K; y++) for (var x = 0; x < T * K; x++) Dot(pos * W + x, y, tile[y / K * T + x / K]);
            Console.WriteLine($"pos {pos}: brick {number - 1}, {s.Width} x {s.Lines} hot ({s.HotX},{s.HotY})");
            for (var y = 0; y < s.Lines * K && y < 42 * K; y++) for (var x = 0; x < s.Width * K; x++) Dot(pos * W + (T + 2) * K + x, y, s.Pixels[y / K * s.Width + x / K]);
        }
        PngWriter.Write(args[4], px, W * n, 42 * K);
        return 0;
    }

    // polartopdown <LBA2 folder> <ILE> <cell x> <cell z> <cells> <out.png>: the built ground's textures from straight above, north up, 16 pixels to a cell (no light)
    public static int TopDown(string[] args)
    {
        var island = LBAAssembler.Terrain.IslandFile.Load(Path.Combine(args[1], args[2]));
        int gx0 = int.Parse(args[3]), gz0 = int.Parse(args[4]), n = int.Parse(args[5]);
        var palette = LBAAssembler.HqrArchive.Open(Path.Combine(args[1], "RESS.HQR")).Read(LBAAssembler.Terrain.Polar.PolarIsland.PaletteEntry);
        const int K = 16;
        var px = new byte[n * K * n * K * 4];
        for (var j = 0; j < n; j++)
            for (var i = 0; i < n; i++)
            {
                int gx = gx0 + i, gz = gz0 + j;
                var s = LBAAssembler.Terrain.IslandGround.Pick(island, gx, gz, 0);
                if (s is null) continue;
                var (page, definition) = island.GroundTextureOf(s.Polygon);
                var cube = island.CubeAt(gx / 64, gz / 64)!;
                if (definition * 6 + 6 > cube.TextureDefs.Length) continue;
                var straight = LBAAssembler.Terrain.IslandGround.TileDefinition(0, 0, 1, 1, s.Polygon.Diagonal, 0);
                var uv = cube.TextureDefs.AsSpan(definition * 6, 6).ToArray();
                double[] ox = new double[3], oz = new double[3];
                for (var k = 0; k < 3; k++) { ox[k] = straight[k * 2] > 128 ? 1 : 0; oz[k] = straight[k * 2 + 1] > 128 ? 1 : 0; }
                var det = (ox[1] - ox[0]) * (oz[2] - oz[0]) - (ox[2] - ox[0]) * (oz[1] - oz[0]);
                var bytes = island.GroundPage(page);
                for (var y = 0; y < K; y++)
                    for (var x = 0; x < K; x++)
                    {
                        double sx = (x + 0.5) / K, sz = (y + 0.5) / K;
                        var l1 = ((sx - ox[0]) * (oz[2] - oz[0]) - (ox[2] - ox[0]) * (sz - oz[0])) / det;
                        var l2 = ((ox[1] - ox[0]) * (sz - oz[0]) - (sx - ox[0]) * (oz[1] - oz[0])) / det;
                        var l0 = 1 - l1 - l2;
                        var u = (l0 * uv[0] + l1 * uv[2] + l2 * uv[4]) / 256;
                        var v = (l0 * uv[1] + l1 * uv[3] + l2 * uv[5]) / 256;
                        var c = bytes[Math.Clamp((int)v, 0, 255) * 256 + Math.Clamp((int)u, 0, 255)];
                        var at = ((j * K + y) * n * K + i * K + x) * 4;
                        px[at] = palette[c * 3 + 2]; px[at + 1] = palette[c * 3 + 1]; px[at + 2] = palette[c * 3]; px[at + 3] = 255;
                    }
            }
        PngWriter.Write(args[6], px, n * K, n * K);
        return 0;
    }

    // polargroundpage <LBA2 folder> <ILE> <page> <out.png>: a ground texture page, twice the size
    public static int GroundPage(string[] args)
    {
        var island = LBAAssembler.Terrain.IslandFile.Load(Path.Combine(args[1], args[2]));
        var palette = LBAAssembler.HqrArchive.Open(Path.Combine(args[1], "RESS.HQR")).Read(LBAAssembler.Terrain.Polar.PolarIsland.PaletteEntry);
        var bytes = island.GroundPage(int.Parse(args[3]));
        Console.WriteLine($"{island.GroundPages.Count + 1} pages");
        var px = new byte[512 * 512 * 4];
        for (var y = 0; y < 512; y++)
            for (var x = 0; x < 512; x++)
            {
                var c = bytes[y / 2 * 256 + x / 2];
                var at = (y * 512 + x) * 4;
                px[at] = palette[c * 3 + 2]; px[at + 1] = palette[c * 3 + 1]; px[at + 2] = palette[c * 3]; px[at + 3] = 255;
            }
        PngWriter.Write(args[4], px, 512, 512);
        return 0;
    }

    // polarlog <LBA1 folder> <LBA2 game folder>: the island's build log (nothing written)
    public static int BuildLog(string[] args)
    {
        foreach (var line in LBAAssembler.Terrain.Polar.PolarIsland.Build(args[1], args[2]).Log) Console.WriteLine(line);
        return 0;
    }

    // colourbodies <folder> <ILE> <OBL> [ramps]: the bodies of an OBL with faces of that many colour ramps and more (a rainbow), and where the island has them
    public static int ColourBodies(string[] args)
    {
        var want = args.Length > 4 ? int.Parse(args[4]) : 5;
        var island = LBAAssembler.Terrain.IslandFile.Load(Path.Combine(args[1], args[2]));
        var path = Path.Combine(args[1], args[3]);
        var obl = LBAAssembler.HqrArchive.Open(path);
        var count = LBAAssembler.HqrArchive.CountEntries(path);
        var found = new Dictionary<int, string>();
        for (var i = 0; i < count; i++)
        {
            LbaBodyStudio.Body body;
            try { body = LbaBodyStudio.Body.Read(obl.Read(i), 2, allowStatic: true); } catch { continue; }
            var ramps = body.Faces.Where(f => f.Texture is null).Select(f => f.Colour >> 4).Distinct().ToList();
            if (ramps.Count >= want) found[i] = $"{body.Vertices.Count} points, {body.Faces.Count} faces, ramps {string.Join(" ", ramps.Order())}, x {body.Vertices.Min(v => v.X):0}..{body.Vertices.Max(v => v.X):0} y {body.Vertices.Min(v => v.Y):0}..{body.Vertices.Max(v => v.Y):0} z {body.Vertices.Min(v => v.Z):0}..{body.Vertices.Max(v => v.Z):0}";
        }
        foreach (var (i, text) in found) Console.WriteLine($"body {i}: {text}");
        foreach (var (key, cube) in island.Cubes)
            foreach (var d in cube.Decors)
                if (found.ContainsKey(d.Body & 0xFFFF)) Console.WriteLine($"cube id {key}: body {d.Body & 0xFFFF} at ({d.X},{d.Y},{d.Z}) box {d.XMin}..{d.XMax} {d.YMin}..{d.YMax} {d.ZMin}..{d.ZMax}");
        return 0;
    }

    // groundbanks <folder> <ILE> <cube x> <cube z> <x0> <x1> <z0> <z1>: each cell's texture bank and height
    public static int GroundBanks(string[] args)
    {
        var island = LBAAssembler.Terrain.IslandFile.Load(Path.Combine(args[1], args[2]));
        var cube = island.CubeAt(int.Parse(args[3]), int.Parse(args[4]))!;
        for (var z = int.Parse(args[7]); z <= int.Parse(args[8]); z++)
        {
            var line = new List<string>();
            for (var x = int.Parse(args[5]); x <= int.Parse(args[6]); x++)
                line.Add($"{new LBAAssembler.Terrain.IslandPolygon(cube.Polygon(x, z, 0)).Bank,2}:{cube.Height(x, z),-5}");
            Console.WriteLine($"z{z,3} " + string.Join(" ", line));
        }
        return 0;
    }

    // polarprops <LBA1 folder> <out folder> <scene>...: each block of those scenes of more than a cell, alone, as <scene>_<block>_<placed>x.png, 4 times the size
    public static int Props(string[] args)
    {
        var game = new Lba1Game(args[1]);
        Directory.CreateDirectory(args[2]);
        foreach (var scene in args.Skip(3).Select(int.Parse))
        {
            var lib = game.ReadBlocks(scene);
            var cells = Lba1GridCodec.Decode(game.ReadGrid(scene));
            var use = new Dictionary<int, int>();
            for (var i = 0; i < 64 * 64 * 25; i++) { var b = cells[i * 2]; if (b != 0 && cells[i * 2 + 1] == 0) use[b] = use.GetValueOrDefault(b) + 1; }
            foreach (var (block, n) in use.OrderByDescending(u => u.Value))
            {
                var (dx, dy, dz) = LBAAssembler.Terrain.Polar.PolarProps.Size(lib, block);
                if (dy < 2 && dx * dz < 4) continue;
                var pic = LBAAssembler.Terrain.Polar.PolarProps.Draw(game, lib, block);
                if (pic.Width == 0) continue;
                const int K = 3;
                var px = new byte[pic.Width * K * pic.Height * K * 4];
                for (var y = 0; y < pic.Height * K; y++)
                    for (var x = 0; x < pic.Width * K; x++)
                    {
                        var c = pic.Pixels[y / K * pic.Width + x / K];
                        var i = (y * pic.Width * K + x) * 4;
                        if (c < 0) { px[i] = 255; px[i + 1] = 0; px[i + 2] = 255; px[i + 3] = 255; continue; }
                        px[i] = game.Palette[c * 3 + 2]; px[i + 1] = game.Palette[c * 3 + 1]; px[i + 2] = game.Palette[c * 3]; px[i + 3] = 255;
                    }
                PngWriter.Write(Path.Combine(args[2], $"{scene}_{block}_{n}x_{dx}.{dy}.{dz}.png"), px, pic.Width * K, pic.Height * K);
            }
        }
        return 0;
    }

    // flatcells <ILE> <cell x0> <x1> <z0> <z1>: the ground cells drawn in flat colour (no texture) there, by bank
    public static int FlatCells(string[] args)
    {
        var island = IslandFile.Load(args[1]);
        var r = args[2..6].Select(int.Parse).ToArray();
        var seen = new Dictionary<int, List<(int, int)>>();
        for (var gz = r[2]; gz <= r[3]; gz++)
        for (var gx = r[0]; gx <= r[1]; gx++)
        {
            if (island.CubeAt(gx / 64, gz / 64) is not { HasPolygons: true } c) continue;
            foreach (var h in new[] { 0, 1 })
            {
                var p = new IslandPolygon(c.Polygon(gx % 64, gz % 64, h));
                if (p.TexFlag != 0 || p.PolyFlag == 0) continue;
                if (!seen.TryGetValue(p.Bank, out var l)) seen[p.Bank] = l = new();
                l.Add((gx, gz));
            }
        }
        foreach (var (bank, l) in seen.OrderByDescending(k => k.Value.Count))
            Console.WriteLine($"bank {bank}: {l.Count} triangles: " + string.Join(" ", l.Take(14).Select(c => $"{c.Item1},{c.Item2}")));
        return 0;
    }

    // decorsnear <ILE> <cell x0> <x1> <z0> <z1>: the decors whose box's middle is in those island cells
    public static int DecorsNear(string[] args)
    {
        var island = IslandFile.Load(args[1]);
        var r = args[2..6].Select(double.Parse).ToArray();
        foreach (var (cx, cz, cube) in IslandOps.CubeCells(island))
            for (var i = 0; i < cube.Decors.Count; i++)
            {
                var d = cube.Decors[i];
                double x = (cx * 32768.0 + (d.XMin + d.XMax) / 2.0) / 512, z = (cz * 32768.0 + (d.ZMin + d.ZMax) / 2.0) / 512;
                if (x >= r[0] && x <= r[1] && z >= r[2] && z <= r[3])
                    Console.WriteLine($"cube ({cx},{cz}) decor {i} body {d.Body & 0xFFFF} cell ({x:F1},{z:F1}) at ({d.X},{d.Y},{d.Z}) box x {d.XMin}..{d.XMax} y {d.YMin}..{d.YMax} z {d.ZMin}..{d.ZMax}");
            }
        return 0;
    }

    public static int WaterPatches(string[] args)
    {
        var island = IslandFile.Load(args[1]);
        if (args.Length > 3 && args[2] == "near")
        {
            var pts = File.ReadAllLines(args[3]).Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Where(p => p.Length >= 3).Select(p => (X: double.Parse(p[0]) / 512, Z: double.Parse(p[1]) / 512)).ToList();
            var reach = args.Length > 4 ? double.Parse(args[4]) : 12;
            var found = new List<(int X, int Z, string How)>();
            for (var gz = 0; gz < 1024; gz++)
            for (var gx = 0; gx < 1024; gx++)
            {
                if (island.CubeAt(gx / 64, gz / 64) is not { HasPolygons: true } c) continue;
                var a0 = new IslandPolygon(c.Polygon(gx % 64, gz % 64, 0)); var b0 = new IslandPolygon(c.Polygon(gx % 64, gz % 64, 1));
                bool Un(IslandPolygon p) => p.TexFlag == 0 && p.PolyFlag == 0;
                if (!(Un(a0) || Un(b0) || a0.CodeJeu == 1 || b0.CodeJeu == 1)) continue;
                if (pts.Any(p => (p.X - gx) * (p.X - gx) + (p.Z - gz) * (p.Z - gz) < reach * reach)) found.Add((gx, gz, $"{(Un(a0) ? "a" : "")}{(Un(b0) ? "b" : "")} code {a0.CodeJeu},{b0.CodeJeu} h {IslandOps.Altitude(island, (gx + 0.5) * 512, (gz + 0.5) * 512):0}"));
            }
            if (args.Length > 8)
            {
                int x0 = int.Parse(args[5]), x1 = int.Parse(args[6]), z0 = int.Parse(args[7]), z1 = int.Parse(args[8]);
                var pal = IslandMapRenderer.LoadPalette(Path.GetDirectoryName(Path.GetFullPath(args[1]))!, Path.GetFileNameWithoutExtension(args[1]));
                for (var gz = z0; gz <= z1; gz++)
                {
                    var line = new System.Text.StringBuilder($"{gz,4} ");
                    for (var gx = x0; gx <= x1; gx++)
                    {
                        var c = island.CubeAt(gx / 64, gz / 64);
                        var a0 = c is { HasPolygons: true } ? new IslandPolygon(c.Polygon(gx % 64, gz % 64, 0)) : new IslandPolygon(0);
                        var b0 = c is { HasPolygons: true } ? new IslandPolygon(c.Polygon(gx % 64, gz % 64, 1)) : new IslandPolygon(0);
                        var ua = a0.TexFlag == 0 && a0.PolyFlag == 0; var ub = b0.TexFlag == 0 && b0.PolyFlag == 0;
                        var road = pts.Any(p => (p.X - gx) * (p.X - gx) + (p.Z - gz) * (p.Z - gz) < 16);
                        var blue = false;
                        if (!ua && a0.TexFlag != 0 && c is not null)
                        {
                            var (page, def) = island.GroundTextureOf(a0);
                            if (def * 6 + 6 <= c.TextureDefs.Length)
                            {
                                var pix = island.GroundPage(page);
                                int u = (c.TextureDefs[def * 6] + c.TextureDefs[def * 6 + 2] + c.TextureDefs[def * 6 + 4]) / 3 >> 8, v = (c.TextureDefs[def * 6 + 1] + c.TextureDefs[def * 6 + 3] + c.TextureDefs[def * 6 + 5]) / 3 >> 8;
                                var col = pix[(v & 255) * 256 + (u & 255)];
                                blue = pal[col * 3 + 2] > pal[col * 3] + 30 && pal[col * 3 + 2] > pal[col * 3 + 1];
                            }
                        }
                        line.Append(ua && ub ? ((IslandOps.Altitude(island, (gx + 0.5) * 512, (gz + 0.5) * 512) ?? 0) > 60 ? '^' : '~') : ua || ub ? '/' : blue ? 'B' : road ? '#' : '.');
                    }
                    Console.WriteLine(line);
                }
                return 0;
            }
            foreach (var g in found.GroupBy(f => (f.X / 8 * 8, f.Z / 8 * 8)).OrderBy(g => g.Key)) Console.WriteLine($"{g.Key}: {g.Count()} e.g. {g.First().X},{g.First().Z} {g.First().How}");
            return 0;
        }
        var most = args.Length > 2 ? int.Parse(args[2]) : 2000;
        const int grid = 1024;
        var seen = new HashSet<(int, int)>();
        bool Wet(int gx, int gz, out string how)
        {
            how = "";
            if (gx < 0 || gz < 0 || gx >= grid || gz >= grid || island.CubeAt(gx / 64, gz / 64) is not { HasPolygons: true } c) return false;
            var a = new IslandPolygon(c.Polygon(gx % 64, gz % 64, 0)); var b = new IslandPolygon(c.Polygon(gx % 64, gz % 64, 1));
            var undrawn = a.TexFlag == 0 && a.PolyFlag == 0 && b.TexFlag == 0 && b.PolyFlag == 0;
            var water = a.CodeJeu == 1 || b.CodeJeu == 1;
            how = $"tex {a.TexFlag} poly {a.PolyFlag} code {a.CodeJeu} bank {a.Bank}";
            return undrawn || water;
        }
        for (var z = 0; z < grid; z++)
        for (var x = 0; x < grid; x++)
        {
            if (seen.Contains((x, z)) || !Wet(x, z, out var how)) continue;
            var patch = new List<(int X, int Z)> { (x, z) }; seen.Add((x, z));
            var open = false;
            for (var i = 0; i < patch.Count; i++)
            {
                var (px, pz) = patch[i];
                foreach (var (nx, nz) in new[] { (px + 1, pz), (px - 1, pz), (px, pz + 1), (px, pz - 1) })
                {
                    if (nx < 0 || nz < 0 || nx >= grid || nz >= grid || island.CubeAt(nx / 64, nz / 64) is not { HasPolygons: true }) { open = true; continue; }
                    if (Wet(nx, nz, out _) && seen.Add((nx, nz))) patch.Add((nx, nz));
                }
            }
            if (patch.Count > most) continue;
            Console.WriteLine($"{patch.Count} cells{(open ? " (to the edge)" : "")} x {patch.Min(p => p.X)}..{patch.Max(p => p.X)} z {patch.Min(p => p.Z)}..{patch.Max(p => p.Z)} height {IslandOps.Altitude(island, (x + 0.5) * 512, (z + 0.5) * 512)}: {how}");
        }
        return 0;
    }

    // polarsheet <LBA1> <scene> <out.png>: its ground's commonest top bricks, each its sprite and its top tile
    public static int TileDump(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var cells = PolarLayout.Bricks(game, int.Parse(args[2]));
        var tops = cells.GroupBy(c => (c.X, c.Z)).Select(g => g.OrderByDescending(c => c.Y).First()).Where(c => c.Y <= 8).GroupBy(c => c.Brick).OrderByDescending(g => g.Count()).Take(32).ToList();
        const int Cell = 110;
        var w = Cell * 8; var h = Cell * 8; var rgb = new byte[w * h * 3];
        void Put(int x, int y, int c) { if (x < 0 || y < 0 || x >= w || y >= h) return; for (var k = 0; k < 3; k++) rgb[(y * w + x) * 3 + k] = game.Palette[c * 3 + k]; }
        for (var i = 0; i < tops.Count; i++)
        {
            var b = tops[i].Key;
            var sprite = PolarTextures.Sprite.Decode(game.ReadBrick(b));
            var cov = sprite.Coverage(PolarTextures.Face.Top);
            Console.WriteLine($"{i}: brick {b} used {tops[i].Count()} code {tops[i].First().Code:X2} shape {tops[i].First().Shape} coverage {cov:0.00}");
            var tile = sprite.Tile(PolarTextures.Face.Top);
            int ox = i % 8 * Cell, oy = i / 8 * Cell;
            for (var y = 0; y < PolarTerrain.Tile; y++) for (var x = 0; x < PolarTerrain.Tile; x++)
                for (var d = 0; d < 9; d++) Put(ox + x * 3 + d % 3, oy + y * 3 + d / 3, tile[y * PolarTerrain.Tile + x]);
        }
        var bgra = new byte[w * h * 4]; for (var i = 0; i < w * h; i++) { bgra[i * 4] = rgb[i * 3 + 2]; bgra[i * 4 + 1] = rgb[i * 3 + 1]; bgra[i * 4 + 2] = rgb[i * 3]; bgra[i * 4 + 3] = 255; }
        PngWriter.Write(args[3], bgra, w, h);
        return 0;
    }

    public static int Region(string[] args)
    {
        var game = new Lba1Game(args[1]);
        int x0 = int.Parse(args[4]), x1 = int.Parse(args[5]), z0 = int.Parse(args[6]), z1 = int.Parse(args[7]);
        List<Lba1Placement> cells;
        if (args[3] == "joined")
            cells = PolarLayout.Build(game).Cells.Where(c => c.Key.X >= x0 && c.Key.X <= x1 && c.Key.Z >= z0 && c.Key.Z <= z1)
                .Select(c => new Lba1Placement(c.Key.X - x0, c.Key.Y, c.Key.Z - z0, c.Value.Brick)).ToList();
        else
            cells = PolarLayout.Bricks(game, int.Parse(args[3])).Where(c => c.X >= x0 && c.X <= x1 && c.Z >= z0 && c.Z <= z1)
                .Select(c => new Lba1Placement(c.X - x0, c.Y, c.Z - z0, c.Brick)).ToList();
        var image = Lba1GridRenderer.Render(new[] { new Lba1Tile(cells, 0, 0, 0) }, game.ReadBrick, game.Palette);
        PngWriter.Write(args[2], image.Bgra, image.Width, image.Height);
        Console.WriteLine($"{args[2]}: {cells.Count} cells, {image.Width} x {image.Height}");
        return 0;
    }

    // polardark <LBA1 folder> <out.png>: the ground's top bricks by how much of their top is dark (r+g+b under 150), with a sheet of the
    // darkest 64 (their numbers and shares printed in that order).
    public static int Dark(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = PolarLayout.Build(game);
        var columns = PolarTerrain.Columns(game, layout);
        var counts = columns.Values.Where(c => !c.Water).GroupBy(c => c.Cell.Brick).ToDictionary(g => g.Key, g => g.Count());
        var shares = counts.Keys.Select(b =>
        {
            var tile = PolarTextures.Sprite.Decode(game.ReadBrick(b)).Tile(PolarTextures.Face.Top);
            var dark = tile.Count(c => c >= 0 && PolarTextures.Colours.Rgb(game.Palette, c) is var (r, g, bl) && r + g + bl < 150);
            return (Brick: b, Share: (double)dark / tile.Length);
        }).OrderByDescending(t => t.Share).ToList();
        foreach (var bucket in new[] { 0.4, 0.3, 0.2, 0.15, 0.1, 0.05 })
            Console.WriteLine($"share >= {bucket}: {shares.Count(t => t.Share >= bucket)} bricks, {shares.Where(t => t.Share >= bucket).Sum(t => counts[t.Brick])} cells");
        var top = shares.Take(64).ToList();
        Console.WriteLine(string.Join(" ", top.Select(t => $"{t.Brick}:{t.Share:0.00}")));
        const int k = 2;
        var w = 8 * 34 * k; var h = 8 * 34 * k;
        var px = new byte[w * h * 4];
        for (var n = 0; n < top.Count; n++)
        {
            var tile = PolarTextures.Sprite.Decode(game.ReadBrick(top[n].Brick)).Tile(PolarTextures.Face.Top);
            int ox = n % 8 * 34, oy = n / 8 * 34;
            for (var j = 0; j < 32; j++) for (var i = 0; i < 32; i++)
            {
                var (r, g, b) = PolarTextures.Colours.Rgb(game.Palette, Math.Max(0, tile[j * 32 + i]));
                for (var dy = 0; dy < k; dy++) for (var dx = 0; dx < k; dx++)
                {
                    var o = (((oy + j) * k + dy) * w + (ox + i) * k + dx) * 4;
                    px[o] = (byte)b; px[o + 1] = (byte)g; px[o + 2] = (byte)r; px[o + 3] = 255;
                }
            }
        }
        PngWriter.Write(args[2], px, w, h);
        return 0;
    }

    // polarseams <LBA1 folder> [range]: for each pair of the layout's scenes whose grids overlap or touch, how many cells hold the same
    // brick at the same place in both, at the layout's offset and at each offset round it (x, z within range, y within 2) -- the best ones.
    public static int Seams(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var range = args.Length > 2 ? int.Parse(args[2]) : 4;
        var placed = PolarLayout.Zoned.ToList();
        var bricks = placed.ToDictionary(p => p.Scene, p => PolarLayout.Bricks(game, p.Scene).ToDictionary(b => (b.X, b.Y, b.Z), b => b.Brick));
        foreach (var a in placed)
            foreach (var b in placed.Where(b => b.Scene > a.Scene))
            {
                // b's grid in a's: b's cell (x, y, z) is a's (x + rx, y + ry, z + rz)
                int rx = b.X - a.X, ry = b.Y - a.Y, rz = b.Z - a.Z;
                if (Math.Abs(rx) > 64 + range || Math.Abs(rz) > 64 + range) continue;
                var results = new List<(int Dx, int Dy, int Dz, int Same, int Both)>();
                for (var dx = -range; dx <= range; dx++)
                    for (var dz = -range; dz <= range; dz++)
                        for (var dy = -2; dy <= 2; dy++)
                        {
                            int same = 0, both = 0;
                            foreach (var ((x, y, z), brick) in bricks[b.Scene])
                            {
                                var at = (x + rx + dx, y + ry + dy, z + rz + dz);
                                if (!bricks[a.Scene].TryGetValue(at, out var other)) continue;
                                both++; if (other == brick) same++;
                            }
                            if (both > 0) results.Add((dx, dy, dz, same, both));
                        }
                if (results.Count == 0) continue;
                var current = results.FirstOrDefault(r => r.Dx == 0 && r.Dy == 0 && r.Dz == 0);
                var best = results.OrderByDescending(r => r.Same).Take(3).ToList();
                Console.WriteLine($"{a.Scene}-{b.Scene}: placed at ({rx},{ry},{rz}); same/overlap there {current.Same}/{current.Both}; best: " +
                    string.Join("  ", best.Select(r => $"({r.Dx},{r.Dy},{r.Dz}) {r.Same}/{r.Both}")));
            }
        return 0;
    }

    // polarseams2 <LBA1 folder> [range]: for each pair of scenes, by column tops: at each offset, the columns both grids have whose top
    // brick is the same (and at the same layer), and the track columns among them -- and where the grids only touch, the columns across the
    // seam whose tops are at the same layer.
    public static int Seams2(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var range = args.Length > 2 ? int.Parse(args[2]) : 6;
        var placed = PolarLayout.Zoned.ToList();
        var trackBricks = new Dictionary<int, bool>();
        bool Track(int brick) => trackBricks.TryGetValue(brick, out var t) ? t : trackBricks[brick] = PolarTerrain.IsTrackBrick(PolarTextures.Sprite.Decode(game.ReadBrick(brick)), game.Palette);
        var tops = placed.ToDictionary(p => p.Scene, p => PolarLayout.Bricks(game, p.Scene).GroupBy(b => (b.X, b.Z)).ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.Y).First()));
        foreach (var a in placed)
            foreach (var b in placed.Where(b => b.Scene > a.Scene))
            {
                int rx = b.X - a.X, ry = b.Y - a.Y, rz = b.Z - a.Z;
                if (Math.Abs(rx) > 64 + range || Math.Abs(rz) > 64 + range) continue;
                var results = new List<(int Dx, int Dy, int Dz, int Same, int Tracks, int Both)>();
                for (var dx = -range; dx <= range; dx++)
                    for (var dz = -range; dz <= range; dz++)
                        for (var dy = -2; dy <= 2; dy++)
                        {
                            int same = 0, tracks = 0, both = 0;
                            foreach (var ((x, z), t) in tops[b.Scene])
                            {
                                if (!tops[a.Scene].TryGetValue((x + rx + dx, z + rz + dz), out var o)) continue;
                                both++;
                                if (o.Brick == t.Brick && o.Y == t.Y + ry + dy) { same++; if (Track(t.Brick)) tracks++; }
                            }
                            if (both > 0) results.Add((dx, dy, dz, same, tracks, both));
                        }
                if (results.Count == 0) continue;
                var current = results.FirstOrDefault(r => r.Dx == 0 && r.Dy == 0 && r.Dz == 0);
                Console.WriteLine($"{a.Scene}-{b.Scene}: placed at ({rx},{ry},{rz}); same tops there {current.Same} ({current.Tracks} tracks) of {current.Both}; best: " +
                    string.Join("  ", results.OrderByDescending(r => r.Same).Take(4).Select(r => $"({r.Dx},{r.Dy},{r.Dz}) {r.Same} ({r.Tracks}t)/{r.Both}")));
            }
        return 0;
    }

    // polaredges <LBA1 folder> <scene>...: each scene's four border rows (and the one inside each): along it, a letter a column -- T a
    // track top, . other ground, # rock, ~ water, o an object top, space nothing -- and its top layer as a digit/letter (0-9, a-o).
    public static int Edges(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var trackBricks = new Dictionary<int, bool>();
        bool Track(int brick) => trackBricks.TryGetValue(brick, out var t) ? t : trackBricks[brick] = PolarTerrain.IsTrackBrick(PolarTextures.Sprite.Decode(game.ReadBrick(brick)), game.Palette);
        foreach (var scene in args.Skip(2).Select(int.Parse))
        {
            var bricks = PolarLayout.Bricks(game, scene);
            var tops = bricks.GroupBy(b => (b.X, b.Z)).ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.Y).First());
            char Kind(PolarLayout.SceneBrick b)
            {
                var cell = new PolarLayout.Cell(scene, b.Brick, b.Shape, b.Code, b.Block);
                if (cell.Water) return '~';
                if (!PolarTerrain.IsGround(game, cell)) return 'o';
                if (((b.Code & 0xF0) == 0x60 || b.Code == 0x06) && Track(b.Brick)) return 'T';
                return b.Code == 0x00 || (b.Code & 0xF0) == 0xA0 ? '#' : '.';
            }
            string Row(Func<int, (int X, int Z)> at)
            {
                var kinds = new char[64]; var heights = new char[64];
                for (var i = 0; i < 64; i++)
                {
                    if (!tops.TryGetValue(at(i), out var t)) { kinds[i] = ' '; heights[i] = ' '; continue; }
                    kinds[i] = Kind(t); heights[i] = "0123456789abcdefghijklmno"[t.Y];
                }
                return new string(kinds) + "\n            " + new string(heights);
            }
            Console.WriteLine($"scene {scene}");
            foreach (var (name, f) in new (string, Func<int, int, (int X, int Z)>)[] { ("z=0  ", (i, k) => (i, k)), ("z=63 ", (i, k) => (i, 63 - k)), ("x=0  ", (i, k) => (k, i)), ("x=63 ", (i, k) => (63 - k, i)) })
                for (var k = 0; k < 2; k++)
                    Console.WriteLine($"  {name}{(k == 0 ? "edge " : "in 1 ")} {Row(i => f(i, k))}");
        }
        return 0;
    }

    // polarexits <LBA1 folder> <scene>...: where car tracks reach each edge of a scene's grid (within its outer 3 rows): along the edge,
    // the track columns and their top layer.
    public static int Exits(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var trackBricks = new Dictionary<int, bool>();
        bool Track(int brick) => trackBricks.TryGetValue(brick, out var t) ? t : trackBricks[brick] = PolarTerrain.IsTrackBrick(PolarTextures.Sprite.Decode(game.ReadBrick(brick)), game.Palette);
        foreach (var scene in args.Skip(2).Select(int.Parse))
        {
            var tops = PolarLayout.Bricks(game, scene).GroupBy(b => (b.X, b.Z)).ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.Y).First());
            bool IsTrack((int X, int Z) at, out int y)
            {
                y = -1;
                if (!tops.TryGetValue(at, out var t) || !((t.Code & 0xF0) == 0x60 || t.Code == 0x06) || !Track(t.Brick)) return false;
                y = t.Y; return true;
            }
            foreach (var (name, f) in new (string, Func<int, int, (int X, int Z)>)[] { ("z=0", (i, k) => (i, k)), ("z=63", (i, k) => (i, 63 - k)), ("x=0", (i, k) => (k, i)), ("x=63", (i, k) => (63 - k, i)) })
            {
                var found = new List<string>();
                for (var i = 0; i < 64; i++)
                    for (var k = 0; k < 3; k++)
                        if (IsTrack(f(i, k), out var y)) { found.Add($"{i}@{y}" + (k > 0 ? $"(in {k})" : "")); break; }
                Console.WriteLine($"scene {scene} {name}: " + string.Join(" ", found));
            }
        }
        return 0;
    }

    // polarfit <LBA1 folder> <scene a> <scene b> <bx,by,bz>...: scene b placed at each offset from scene a (b's cell 0 at a's (bx, by, bz)):
    // the columns both grids have, and how many have the same top brick at the same layer.
    public static int Fit(string[] args)
    {
        var game = new Lba1Game(args[1]);
        int a = int.Parse(args[2]), b = int.Parse(args[3]);
        var topsA = PolarLayout.Bricks(game, a).GroupBy(c => (c.X, c.Z)).ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.Y).First());
        var topsB = PolarLayout.Bricks(game, b).GroupBy(c => (c.X, c.Z)).ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.Y).First());
        foreach (var o in args.Skip(4))
        {
            var v = o.Split(',').Select(int.Parse).ToArray();
            int both = 0, same = 0, heights = 0;
            foreach (var ((x, z), t) in topsB)
            {
                if (!topsA.TryGetValue((x + v[0], z + v[2]), out var u)) continue;
                both++;
                if (u.Y == t.Y + v[1]) { heights++; if (u.Brick == t.Brick) same++; }
            }
            Console.WriteLine($"{b} at ({o}) from {a}: {both} columns in both, {heights} the same height, {same} the same top brick");
        }
        return 0;
    }

    // polarmap <LBA1 folder> <scene> [x0 x1 z0 z1]: a scene's columns as a map, a row per z: each its top layer (0-9, a-o) in a colour
    // class -- upper case letters for crystal (A + layer), digits / lower case for the rest; ~ water, space none.
    public static int Map(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var scene = int.Parse(args[2]);
        int x0 = args.Length > 3 ? int.Parse(args[3]) : 0, x1 = args.Length > 4 ? int.Parse(args[4]) : 63, z0 = args.Length > 5 ? int.Parse(args[5]) : 0, z1 = args.Length > 6 ? int.Parse(args[6]) : 63;
        var tops = PolarLayout.Bricks(game, scene).GroupBy(b => (b.X, b.Z)).ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.Y).First());
        const string digits = "0123456789abcdefghijklmnop", upper = "0123456789ABCDEFGHIJKLMNOP";
        Console.WriteLine("     " + string.Concat(Enumerable.Range(x0, x1 - x0 + 1).Select(x => (x % 10).ToString())));
        for (var z = z0; z <= z1; z++)
        {
            var row = new System.Text.StringBuilder();
            for (var x = x0; x <= x1; x++)
            {
                if (!tops.TryGetValue((x, z), out var t)) { row.Append(' '); continue; }
                if (t.Code == 0xF1) { row.Append('~'); continue; }
                row.Append((t.Code & 0xF0) == 0xA0 ? upper[t.Y] : digits[t.Y]);
            }
            Console.WriteLine($"{z,3}  {row}");
        }
        return 0;
    }

    // polarjoined <LBA1 folder> <x0> <x1> <z0> <z1>: the joined layout's columns there as a map of top layers (0-9, a-z): upper case
    // where the column is the rocky peak's (PolarLayout.Peak), * where a column outside it holds teal rock or crystal above layer 8.
    public static int Joined(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = PolarLayout.Build(game);
        int x0 = int.Parse(args[2]), x1 = int.Parse(args[3]), z0 = int.Parse(args[4]), z1 = int.Parse(args[5]);
        var tops = layout.Cells.Where(c => c.Value.Scene != PolarLayout.PlateauScene).GroupBy(c => (c.Key.X, c.Key.Z)).ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.Key.Y).First());
        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz", upper = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Console.WriteLine("      " + string.Concat(Enumerable.Range(x0, x1 - x0 + 1).Select(x => ((x % 10 + 10) % 10).ToString())));
        for (var z = z0; z <= z1; z++)
        {
            var row = new System.Text.StringBuilder();
            for (var x = x0; x <= x1; x++)
            {
                if (!tops.TryGetValue((x, z), out var t)) { row.Append(' '); continue; }
                var y = Math.Min(35, t.Key.Y);
                if (layout.Peak.Contains((x, z))) { row.Append(upper[y]); continue; }
                if (t.Value.Water) { row.Append('~'); continue; }
                var teal = PolarLayout.BrickColour(game, t.Value.Brick) is var (r, g, b) && b > r;
                row.Append(teal && t.Key.Y > 8 ? '*' : digits[y]);
            }
            Console.WriteLine($"{z,4}  {row}");
        }
        return 0;
    }

    // polarcolumns <LBA1 folder> <out.csv>: every column of the joined island in island cells (layout + the terrain's offset): its highest
    // cell's top (units: (layer + 1) * 256 in LBA1's terms, the island's layer * 256), its ground's surface (PolarTerrain.SurfaceOf, -1
    // none), whether it is water, the rocky peak, an object or ground, and whether its ground is a car track.
    public static int ColumnsCsv(string[] args)
    {
        var game = new Lba1Game(args[1]);
        var layout = PolarLayout.Build(game);
        var columns = PolarTerrain.Columns(game, layout);
        int minX = columns.Keys.Min(k => k.X), maxX = columns.Keys.Max(k => k.X), minZ = columns.Keys.Min(k => k.Z), maxZ = columns.Keys.Max(k => k.Z);
        var tracks = columns.Values.Where(c => (c.Cell.Code & 0xF0) == 0x60 || c.Cell.Code == 0x06).Select(c => c.Cell.Brick).Distinct()
            .Where(b => PolarTerrain.IsTrackBrick(PolarTextures.Sprite.Decode(game.ReadBrick(b)), game.Palette)).ToHashSet();
        using var w = new StreamWriter(args[2]);
        w.WriteLine("x,z,top,ground,water,peak,object,track");
        foreach (var g in layout.Cells.GroupBy(c => (c.Key.X, c.Key.Z)))
        {
            var top = g.Max(c => c.Key.Y);
            var has = columns.TryGetValue(g.Key, out var col);
            var water = has && col.Water;
            var peak = layout.Peak.Contains(g.Key) || g.Any(c => c.Value.Scene == PolarLayout.PlateauScene);
            var obj = !peak && (!has || top > col.Top);
            // (each of the island's cells of the column: PolarTerrain.Scale x Scale)
            for (var j = 0; j < PolarTerrain.Scale; j++)
                for (var i = 0; i < PolarTerrain.Scale; i++)
                    w.WriteLine($"{PolarTerrain.IslandX(g.Key.X) + i},{PolarTerrain.IslandZ(g.Key.Z) + j},{PolarTerrain.LayerY(top)},{(has ? PolarTerrain.SurfaceOf(col) : -1)},{(water ? 1 : 0)},{(peak ? 1 : 0)},{(obj ? 1 : 0)},{(has && !water && tracks.Contains(col.Cell.Brick) ? 1 : 0)}");
        }
        Console.WriteLine($"{args[2]}: island cells = layout x {PolarTerrain.Scale} + ({PolarTerrain.OffsetX}, {PolarTerrain.OffsetZ})");
        return 0;
    }

    // polarzones <LBA1 folder> <scene>...: each scene's cube-change zones (type 0) in cells (x, layer, z) and where they lead.
    public static int Zones(string[] args)
    {
        var game = new Lba1Game(args[1]);
        foreach (var scene in args.Skip(2).Select(int.Parse))
        {
            var s = game.LoadScene(scene);
            foreach (var z in s.Zones.Where(z => z.Type == 0))
                Console.WriteLine($"scene {scene,3} -> {z.Info[0],3}: cells x {z.X0 / 512}..{z.X1 / 512} layers {z.Y0 / 256}..{z.Y1 / 256} z {z.Z0 / 512}..{z.Z1 / 512}; arrival ({z.Info[1] / 512.0:0.#}, {z.Info[2] / 256.0:0.#}, {z.Info[3] / 512.0:0.#})");
        }
        return 0;
    }
}
