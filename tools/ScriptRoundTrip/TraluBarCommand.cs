using LBAAssembler.Grids;
using LBAAssembler.Lba1;

namespace ScriptRoundTrip;

// The Tavern regulars' secret bar in the Tralu's caves (Terrain/Citadel/RaceTrackStory.TraluTavern.cs, Terrain/Citadel/TraluBarRoom.cs), looked at:
//   gridtops <game folder> <scene> [x0 x1 z0 z1]: an interior's grid seen from above, a column a character: its top layer (0-9, a-o:
//     10-24, '.' empty) -- and its block library, the grid's number, the blocks it uses.
//   gridcells <game folder> <scene> <x0> <x1> <z0> <z1> [y0 y1]: each column's cells: layer:block/place (col<n>: an empty cell's collision).
//   blocksheet2, actorfields, blockcodes, barinstall: see each below.
internal static class TraluBarCommand
{
    private static (Lba2GridBackend Backend, int Grid, byte[] Cells, byte[] Library) Load(string folder, int scene)
    {
        var backend = new Lba2GridBackend(folder);
        var grid = backend.GridOfScene(scene) ?? throw new InvalidOperationException($"scene {scene}: no grid");
        return (backend, grid, Lba1GridCodec.Decode(backend.LoadGrid(grid)), backend.LoadLibrary(grid));
    }

    public static int Tops(string[] args)
    {
        var (backend, grid, cells, _) = Load(args[1], int.Parse(args[2]));
        int x0 = 0, x1 = 63, z0 = 0, z1 = 63;
        if (args.Length > 6) (x0, x1, z0, z1) = (int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]), int.Parse(args[6]));
        Console.WriteLine($"scene {args[2]}: grid {grid}, library {backend.RawGrid(grid)[0]}, fragments {backend.RawGrid(grid)[1]}");
        Console.WriteLine("     " + string.Concat(Enumerable.Range(x0, x1 - x0 + 1).Select(x => (x % 10).ToString())));
        for (var z = z0; z <= z1; z++)
        {
            var line = new char[x1 - x0 + 1];
            for (var x = x0; x <= x1; x++)
            {
                var top = -1;
                for (var y = 24; y >= 0; y--) if (cells[((z * 64 + x) * 25 + y) * 2] != 0) { top = y; break; }
                line[x - x0] = top < 0 ? '.' : top < 10 ? (char)('0' + top) : (char)('a' + top - 10);
            }
            Console.WriteLine($"z{z,2}  {new string(line)}");
        }
        var use = new SortedDictionary<int, int>();
        for (var i = 0; i < 64 * 64 * 25; i++) { var b = cells[i * 2]; if (b != 0) use[b] = use.GetValueOrDefault(b) + 1; }
        Console.WriteLine("blocks: " + string.Join(" ", use.Select(u => $"{u.Key}x{u.Value}")));
        return 0;
    }

    // blocksheet2 <game folder> <scene> <out.png> [first last]: the scene's block library's blocks, each drawn alone with its number and size
    public static int Sheet(string[] args)
    {
        var (backend, _, _, library) = Load(args[1], int.Parse(args[2]));
        var count = (int)(System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(library) / 4);
        int first = args.Length > 5 ? int.Parse(args[4]) : 1, last = args.Length > 5 ? Math.Min(count, int.Parse(args[5])) : count;
        const int CellW = 200, CellH = 200, PerRow = 10;
        var rows = (last - first) / PerRow + 1;
        int width = PerRow * CellW, height = rows * CellH;
        var bgra = new byte[width * height * 4];
        for (var i = 0; i < bgra.Length; i += 4) { bgra[i] = 40; bgra[i + 1] = 40; bgra[i + 2] = 40; bgra[i + 3] = 255; }
        for (var b = first; b <= last; b++)
        {
            var at = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(library.AsSpan((b - 1) * 4));
            int dx = library[at], dy = library[at + 1], dz = library[at + 2];
            var placements = new List<Lba1Placement>();
            for (var p = 0; p < dx * dy * dz; p++)
            {
                var number = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(library.AsSpan(at + 3 + p * 4 + 2));
                if (number != 0) placements.Add(new Lba1Placement(p % dx, p / dx % dy, p / dx / dy, number - 1));
            }
            var index = b - first;
            int ox = index % PerRow * CellW, oy = index / PerRow * CellH;
            if (placements.Count > 0)
            {
                var image = Lba1GridRenderer.Render(new[] { new Lba1Tile(placements, 0, 0, 0) }, backend.Brick, backend.Palette);
                var scale = Math.Max(1, Math.Max((image.Width + CellW - 21) / (CellW - 20), (image.Height + CellH - 31) / (CellH - 30)));
                for (var y = 0; y < image.Height / scale && y < CellH - 20; y++)
                    for (var x = 0; x < image.Width / scale && x < CellW - 4; x++)
                    {
                        var s = ((y * scale) * image.Width + x * scale) * 4;
                        if (image.Bgra[s + 3] == 0) continue;
                        var d = ((oy + 18 + y) * width + ox + 2 + x) * 4;
                        Array.Copy(image.Bgra, s, bgra, d, 4);
                    }
            }
            var label = $"{b} {dx}x{dy}x{dz}";
            for (var c = 0; c < label.Length; c++)
            {
                if (label[c] is < '0' or > '9') continue;
                var glyph = BlockSheet.Glyph(label[c]);
                for (var k = 0; k < 15; k++)
                    if (glyph[k] == '1')
                        for (var sy = 0; sy < 2; sy++) for (var sx = 0; sx < 2; sx++)
                        {
                            var d = ((oy + 2 + k / 3 * 2 + sy) * width + ox + 2 + c * 8 + k % 3 * 2 + sx) * 4;
                            bgra[d] = 0; bgra[d + 1] = 255; bgra[d + 2] = 255; bgra[d + 3] = 255;
                        }
            }
        }
        DoorRender.WritePng(args[3], bgra, width, 0, 0, width, height, 1);
        return 0;
    }

    // blockcodes <game folder> <scene> <block>...: each block's size and its cells' shape, floor code (hex) and brick
    public static int Codes(string[] args)
    {
        var (_, _, _, library) = Load(args[1], int.Parse(args[2]));
        foreach (var block in args.Skip(3).Select(int.Parse))
        {
            var at = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(library.AsSpan((block - 1) * 4));
            int dx = library[at], dy = library[at + 1], dz = library[at + 2];
            Console.WriteLine($"block {block}: {dx}x{dy}x{dz}: " + string.Join(" ", Enumerable.Range(0, dx * dy * dz).Select(p =>
                $"{p}:s{library[at + 3 + p * 4]}/c{library[at + 3 + p * 4 + 1]:X2}/b{System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(library.AsSpan(at + 3 + p * 4 + 2))}")));
        }
        return 0;
    }

    // actorfields <game folder> <scene>: every actor's fields (0-based, as the scripts number them)
    public static int Actors(string[] args)
    {
        var model = new LBAAssembler.Scenes.SceneStore(LBAAssembler.Scenes.SceneGame.Lba2, args[1]).Load(int.Parse(args[2]));
        for (var i = 0; i < model.Actors.Count; i++)
        {
            var a = model.Actors[i];
            Console.WriteLine($"{i}: entity {a.Entity} body {a.Body} anim {a.Anim} sprite {a.Sprite} at ({a.X},{a.Y},{a.Z}) beta {a.Beta} flags 0x{a.Flags:X} " +
                              $"options 0x{a.OptionFlags:X} bonus {a.NbBonus} life {a.LifePoints} armor {a.Armor} hit {a.HitForce} move {a.Move} srot {a.SRot} info {string.Join(",", a.Info)}");
        }
        return 0;
    }

    // barinstall <game folder>: the secret bar's grid edits into a game folder (TraluBarRoom.Install), to look at
    public static int Install(string[] args)
    {
        Console.WriteLine(LBAAssembler.Terrain.Citadel.TraluBarRoom.Install(args[1]));
        return 0;
    }

    public static int Cells(string[] args)
    {
        var (_, _, cells, _) = Load(args[1], int.Parse(args[2]));
        int x0 = int.Parse(args[3]), x1 = int.Parse(args[4]), z0 = int.Parse(args[5]), z1 = int.Parse(args[6]);
        int y0 = args.Length > 8 ? int.Parse(args[7]) : 0, y1 = args.Length > 8 ? int.Parse(args[8]) : 24;
        for (var z = z0; z <= z1; z++)
            for (var x = x0; x <= x1; x++)
                Console.WriteLine($"x{x,2} z{z,2}: " + string.Join(" ", Enumerable.Range(y0, y1 - y0 + 1).Select(y =>
                {
                    var i = ((z * 64 + x) * 25 + y) * 2;
                    return cells[i] == 0 ? (cells[i + 1] == 0 ? "." : $"{y}:col{cells[i + 1]}") : $"{y}:{cells[i]}/{cells[i + 1]}";
                }).Where(s => s != ".")));
        return 0;
    }
}
