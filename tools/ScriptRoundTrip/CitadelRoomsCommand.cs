using LBAAssembler;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Scenes;

namespace ScriptRoundTrip;

// The LBA1 rooms of Citadel Island's easter eggs (Terrain/Citadel/CitadelLba1Rooms.cs), looked at:
//   lba2scene <game folder> <scene> <out.png> [crop=x,y,w,h] [mark=x,y,z (world units)]...: an LBA2 interior scene's grid drawn alone (the
//     managed renderer, LBA2's palette), each of its actors marked with a cross and its number (0-based, as the script dumps number them),
//     each zone's floor outlined (change-cube zones red, the rest grey) -- for a picture of a ported room and of where things stand in it.
//   lba1port <lba1 folder> <lba2 folder> <lba1 scene> <out.png>: an LBA1 scene's interior as the port makes it (Lba1InteriorPort: recoloured
//     to LBA2's palette), drawn in LBA2's palette -- the port checked before it goes into a game folder.
//   libstats <lba1|lba2> <folder> <scene>: the shapes and sound bytes the scene's grid's blocks use.
internal static class CitadelRoomsCommand
{
    // cellinfo <game folder> <scene> <x0> <x1> <z0> <z1>: each filled cell's block, position, shape and sound byte (its high half: the
    // engine's game code, COMMON.H CJ_*)
    public static int CellInfo(string[] args)
    {
        var b = new Lba2GridBackend(args[1]);
        var g = b.GridOfScene(int.Parse(args[2]))!.Value;
        var cells = Lba1GridCodec.Decode(b.LoadGrid(g));
        var library = b.LoadLibrary(g);
        var (x0, x1, z0, z1) = (int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]), int.Parse(args[6]));
        for (var z = z0; z <= z1; z++)
            for (var x = x0; x <= x1; x++)
                for (var y = 0; y < 25; y++)
                {
                    var i = ((z * 64 + x) * 25 + y) * 2;
                    if (cells[i] == 0) { if (cells[i + 1] != 0) Console.WriteLine($"  ({x},{y},{z}): empty, code {cells[i + 1]}"); continue; }
                    var e = GridPaint.Entries(library, cells[i]).ElementAtOrDefault(cells[i + 1]);
                    Console.WriteLine($"  ({x},{y},{z}): block {cells[i]} pos {cells[i + 1]} shape {e.Shape} sound {e.Sound:x2} brick {e.Brick}");
                }
        return 0;
    }

    public static int Port(string[] args)
    {
        var palette = HqrArchive.Open(Path.Combine(args[2], "RESS.HQR")).Read(0);
        var map = LBAAssembler.Maps.Lba1InteriorPort.Port(args[1], int.Parse(args[3]), palette, "port");
        var bricks = HqrFile.Parse(map.Bricks!);
        var image = Lba1GridRenderer.Render(Lba2GridBackend.ToLba1Shape(map.Grid!), map.Library!, n => n >= 0 && n < bricks.Count && !bricks.IsEmpty(n) ? bricks.Read(n) : null, palette);
        PngWriter.Write(args[4], image.Bgra, image.Width, image.Height);
        Console.WriteLine($"{bricks.Count} bricks; picture {image.Width} x {image.Height}");
        return 0;
    }

    // libstats <lba1|lba2> <folder> <scene>: the shapes and sound bytes the scene's grid's blocks use (cell counts), and the empty cells' codes
    public static int LibStats(string[] args)
    {
        var lba1 = args[1] == "lba1";
        byte[] grid, library;
        if (lba1) { var game = new Lba1Game(args[2]); grid = game.ReadGrid(int.Parse(args[3])); library = game.ReadBlocks(int.Parse(args[3])); }
        else { var b = new Lba2GridBackend(args[2]); var g = b.GridOfScene(int.Parse(args[3]))!.Value; grid = b.LoadGrid(g); library = b.LoadLibrary(g); }
        var cells = Lba1GridCodec.Decode(grid);
        var shapes = new SortedDictionary<int, int>(); var sounds = new SortedDictionary<int, int>(); var empty = new SortedDictionary<int, int>();
        for (var i = 0; i < cells.Length; i += 2)
        {
            int block = cells[i], pos = cells[i + 1];
            if (block == 0) { if (pos != 0) empty[pos] = empty.GetValueOrDefault(pos) + 1; continue; }
            var e = GridPaint.Entries(library, block).ElementAtOrDefault(pos);
            shapes[e.Shape] = shapes.GetValueOrDefault(e.Shape) + 1;
            sounds[e.Sound] = sounds.GetValueOrDefault(e.Sound) + 1;
        }
        Console.WriteLine("shapes: " + string.Join(" ", shapes.Select(p => $"{p.Key}:{p.Value}")));
        Console.WriteLine("sounds: " + string.Join(" ", sounds.Select(p => $"{p.Key:x2}:{p.Value}")));
        Console.WriteLine("empty codes: " + string.Join(" ", empty.Select(p => $"{p.Key}:{p.Value}")));
        return 0;
    }

    public static int Scene(string[] args)
    {
        var dir = args[1];
        var scene = int.Parse(args[2]);
        var backend = new Lba2GridBackend(dir);
        var grid = backend.GridOfScene(scene) ?? throw new InvalidDataException($"scene {scene} has no interior grid");
        var image = Lba1GridRenderer.Render(backend.LoadGrid(grid), backend.LoadLibrary(grid), backend.Brick, backend.Palette);
        var model = new SceneStore(SceneGame.Lba2, dir).Load(scene);
        Console.WriteLine($"scene {scene}: grid {grid}, library {backend.RawGrid(grid)[0]}, {model.Actors.Count} actors, {model.Zones.Count} zones; picture {image.Width} x {image.Height}");
        var px = (byte[])image.Bgra.Clone();
        void Plot(int x, int y, byte b, byte g, byte r)
        {
            if (x < 0 || y < 0 || x >= image.Width || y >= image.Height) return;
            var i = (y * image.Width + x) * 4;
            px[i] = b; px[i + 1] = g; px[i + 2] = r; px[i + 3] = 255;
        }
        void Line(System.Windows.Point a, System.Windows.Point c, byte b, byte g, byte r)
        {
            int x0 = (int)a.X, y0 = (int)a.Y, x1 = (int)c.X, y1 = (int)c.Y;
            int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            for (var guard = 0; guard < 20000; guard++)
            {
                Plot(x0, y0, b, g, r);
                if (x0 == x1 && y0 == y1) break;
                var e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }
        string[] digits = { "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001", "111100111001111", "111100111101111", "111001010010010", "111101111101111", "111101111001111" };
        void Number(int n, int x, int y, byte b, byte g, byte r)
        {
            var text = n.ToString();
            for (var d = 0; d < text.Length; d++)
                for (var yy = 0; yy < 5; yy++)
                    for (var xx = 0; xx < 3; xx++)
                        if (digits[text[d] - '0'][yy * 3 + xx] == '1')
                            for (var s = 0; s < 4; s++) Plot(x + (d * 4 + xx) * 2 + s % 2, y + yy * 2 + s / 2, b, g, r);
        }
        void Cross(double x, double y, double z, byte b, byte g, byte r, int n)
        {
            var p = image.Project(x, y, z);
            for (var d = -6; d <= 6; d++) { Plot((int)p.X + d, (int)p.Y, b, g, r); Plot((int)p.X, (int)p.Y + d, b, g, r); }
            if (n >= 0) Number(n, (int)p.X + 4, (int)p.Y + 4, b, g, r);
        }
        foreach (var z in model.Zones)
        {
            var (b, g, r) = z.Type == 0 ? ((byte)0, (byte)0, (byte)255) : ((byte)160, (byte)160, (byte)160);
            var pts = new[] { (z.X0, z.Z0), (z.X1, z.Z0), (z.X1, z.Z1), (z.X0, z.Z1) }.Select(c => image.Project(c.Item1, z.Y0, c.Item2)).ToArray();
            for (var i = 0; i < 4; i++) Line(pts[i], pts[(i + 1) % 4], b, g, r);
            Console.WriteLine($"  zone type {z.Type} num {z.Num}: x {z.X0}..{z.X1} y {z.Y0}..{z.Y1} z {z.Z0}..{z.Z1} info {string.Join(",", z.Info)}");
        }
        for (var a = 0; a < model.Actors.Count; a++)
        {
            var actor = model.Actors[a];
            Cross(actor.X, actor.Y, actor.Z, 0, 255, 255, a);
            Console.WriteLine($"  actor {a}: entity {actor.Entity} body {actor.Body} sprite {actor.Sprite} at {actor.X},{actor.Y},{actor.Z} beta {actor.Beta} flags {actor.Flags:x}");
        }
        for (var i = 0; i < model.TrackPoints.Count; i++) Console.WriteLine($"  point {i}: {model.TrackPoints[i].X},{model.TrackPoints[i].Y},{model.TrackPoints[i].Z}");
        int? cx = null;
        int cy = 0, cw = 0, ch = 0;
        foreach (var extra in args.Skip(4))
        {
            var parts = extra.Split('=');
            var v = parts[1].Split(',').Select(int.Parse).ToArray();
            if (parts[0] == "crop") (cx, cy, cw, ch) = (v[0], v[1], v[2], v[3]);
            else if (parts[0] == "mark") { Cross(v[0], v[1], v[2], 255, 0, 255, -1); var p = image.Project(v[0], v[1], v[2]); Console.WriteLine($"  mark ({v[0]},{v[1]},{v[2]}) at picture ({(int)p.X},{(int)p.Y})"); }
        }
        if (cx is { } x0)
        {
            var o = new byte[cw * ch * 4];
            for (var r = 0; r < ch; r++)
                for (var c = 0; c < cw; c++)
                {
                    int sx = x0 + c, sy = cy + r;
                    if (sx < 0 || sy < 0 || sx >= image.Width || sy >= image.Height) continue;
                    Array.Copy(px, (sy * image.Width + sx) * 4, o, (r * cw + c) * 4, 4);
                }
            PngWriter.Write(args[3], o, cw, ch);
        }
        else PngWriter.Write(args[3], px, image.Width, image.Height);
        return 0;
    }
}
