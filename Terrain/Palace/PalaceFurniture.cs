using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using LBAAssembler.Lba1;

namespace LBAAssembler.Terrain.Palace;

// The palace rooms' furniture (the user, 2026-10-12: "Let's also see if for each inside room of the palace we can recreate the decor that
// each room naturally has"). A room's furniture is in its grid: blocks of the palace's brick library -- a red marble column, a potted plant,
// a lamp on the wall, a bench, a fireplace, a candelabra, a portrait -- each a few cells of bricks, pictures; the Palace island made each
// cell a box of its brick's average colour, so a plant was a purple block and a column a red post. Here each is made again in 3D where its
// bricks were, from what the bricks picture:
//   - Find: the furniture blocks' placements in the rooms' grids (a block's cells say which block they are and where in it, so the cells
//     of one placement come together again; a column stood on another of itself -- the grids make a tall one so -- is one); their cells
//     stay in the collision (the furniture is in the way, as in the game), and the standing pieces' cells leave the drawn faces, the model
//     drawn instead;
//   - the pieces on the walls -- portraits, the windows, the round window at the long hall's end (the one Twinsen leaves the palace by:
//     room 80's way out is in front of it, the big chest under it) -- are the wall itself in the grid: their cells stay a wall, in the
//     wallpaper, and the picture goes on its face: the portraits and the round window sampled from their bricks' pictures (each spot of the
//     face looked up where the isometric picture draws it), the windows a gold frame round panes of glass; the windows inside and out;
//   - Chests: the rooms' chests are actors, sprites (the closed chest of every other room, the big one under room 80's round window), as
//     3D chests.
// The doors are sprite actors too, but the game slides them open and shut (their track scripts); left out, the doorways stay open.
// A model is made in a room's own units (512 a cell across, 256 a layer up) about the middle of its block's foot, turned to face out of the
// wall it stands at, and goes to the island as the rooms do: Across times as wide, Up times as high; its faces flat, shaded by the way they
// face as the rooms' are, in Otringal's palette.
internal static class PalaceFurniture
{
    public enum Kind { Column, Plant, PlinthPlant, Lamp, Bench, Candelabra, Candle, Fireplace, Portrait, RoundWindow, Window }

    // the palace library's blocks that picture furniture, and their size (a block of another size is not taken for one)
    private static readonly Dictionary<int, (Kind Kind, int Dx, int Dy, int Dz)> Blocks = new()
    {
        [32] = (Kind.Column, 1, 8, 1), [31] = (Kind.Plant, 2, 4, 2), [51] = (Kind.PlinthPlant, 2, 5, 2), [29] = (Kind.Lamp, 1, 2, 1), [30] = (Kind.Lamp, 1, 2, 1),
        [61] = (Kind.Bench, 3, 2, 2), [62] = (Kind.Bench, 2, 2, 3), [63] = (Kind.Candelabra, 1, 4, 2), [64] = (Kind.Candelabra, 2, 4, 1),
        [102] = (Kind.Candle, 1, 3, 1), [68] = (Kind.Fireplace, 1, 5, 4), [69] = (Kind.Fireplace, 4, 5, 1), [105] = (Kind.Portrait, 1, 8, 4),
        [101] = (Kind.RoundWindow, 4, 8, 1), [57] = (Kind.Window, 1, 10, 4), [58] = (Kind.Window, 4, 10, 1),
    };
    // the chests' sprites (SPRITES.HQR): closed and open, the long side along z (342, 343, 346, 347) or x (344, 345, 348); 394 the big one
    private static readonly int[] ChestsAlongZ = { 342, 343, 346, 347 }, ChestsAlongX = { 344, 345, 348 };
    public const int BigChest = 394;

    // A placement of a furniture block in a room: its block, room (tile), first cell and size (cells of the island's palace) and the cells it
    // fills there; a portrait's or the round window's block picture (its bricks drawn alone, as the joined map draws them).
    public sealed record Piece(Kind Kind, int Block, int Tile, (int X, int Y, int Z) Origin, (int X, int Y, int Z) Size, List<(int X, int Y, int Z)> Cells,
        Lba1SceneImage? Image = null)
    {
        // a standing piece: its cells drawn by its model, not as boxes (the wall pieces' cells stay the wall)
        public bool Standing => Kind is not (Kind.Portrait or Kind.RoundWindow or Kind.Window);
    }

    // A chest: its room (tile), the middle of its foot (cells of the island's palace, fractions of one), its size (room units) and its long
    // side along x or not.
    public sealed record Chest(int Tile, int Sprite, double X, double Y, double Z, double Long, double Short, double High, bool AlongX);

    // The furniture blocks placed in the palace's rooms (the scenes of `area`, each at its offset in cells) whose cells the rooms kept
    // (`cells`: a room's cell another one's wall claimed is not its own).
    public static List<Piece> Find(string gameDirectory, Lba1Area area, (int X, int Y, int Z)[] offsets, IReadOnlyDictionary<(int X, int Y, int Z), (int Brick, int Tile)> cells)
    {
        var backend = new Grids.Lba2GridBackend(gameDirectory);
        var pieces = new List<Piece>();
        for (var ti = 0; ti < area.Tiles.Count; ti++)
        {
            var tile = area.Tiles[ti];
            if (backend.GridOfScene(tile.Scene) is not { } grid) continue;
            var library = backend.LoadLibrary(grid);
            var decoded = Lba1GridRenderer.DecodeGrid(backend.LoadGrid(grid));
            var count = (int)(BinaryPrimitives.ReadUInt32LittleEndian(library) / 4);
            var found = new Dictionary<(int Block, int X, int Y, int Z), List<(int, int, int)>>();
            for (var z = 0; z < Lba1GridRenderer.SizeZ; z++)
            for (var x = 0; x < Lba1GridRenderer.SizeX; x++)
            for (var y = 0; y < Lba1GridRenderer.SizeY; y++)
            {
                var i = ((z * Lba1GridRenderer.SizeX + x) * Lba1GridRenderer.SizeY + y) * 2;
                int block = decoded[i], pos = decoded[i + 1];
                if (block == 0 || block > count || !Blocks.TryGetValue(block, out var known) || !tile.Holds(x, z)) continue;
                var at = (int)BinaryPrimitives.ReadUInt32LittleEndian(library.AsSpan((block - 1) * 4));
                if (at + 3 > library.Length || (library[at], library[at + 1], library[at + 2]) != (known.Dx, known.Dy, known.Dz)) continue;
                // (the cell at (i, j, k) of a block is its entry i + dx (j + dy k): GridPaint)
                int bi = pos % known.Dx, bj = pos / known.Dx % known.Dy, bk = pos / known.Dx / known.Dy;
                var cell = (x + offsets[ti].X, y + offsets[ti].Y, z + offsets[ti].Z);
                if (!cells.TryGetValue(cell, out var c) || c.Tile != ti) continue;
                var key = (block, x - bi, y - bj, z - bk);
                if (!found.TryGetValue(key, out var list)) found[key] = list = new();
                list.Add(cell);
            }
            // (placements of a block on the same foot, one over another and touching -- a column twice as tall -- as one, from the lowest's foot
            // to the highest's top)
            var placed = found.Select(f => (f.Key.Block, f.Key.X, Y0: f.Key.Y, f.Key.Z, Y1: f.Key.Y + Blocks[f.Key.Block].Dy, Cells: f.Value))
                .OrderBy(f => f.Block).ThenBy(f => f.X).ThenBy(f => f.Z).ThenBy(f => f.Y0).ToList();
            for (var k = 0; k < placed.Count; k++)
            {
                var (block, x, y0, z, y1, list) = placed[k];
                while (k + 1 < placed.Count && placed[k + 1] is var n && (n.Block, n.X, n.Z) == (block, x, z) && n.Y0 <= y1)
                {
                    y1 = Math.Max(y1, n.Y1); list = list.Concat(n.Cells).ToList(); k++;
                }
                var (kind, dx, _, dz) = Blocks[block];
                var image = kind is Kind.Portrait or Kind.RoundWindow ? Picture(backend, library, block) : null;
                pieces.Add(new Piece(kind, block, ti, (x + offsets[ti].X, y0 + offsets[ti].Y, z + offsets[ti].Z), (dx, y1 - y0, dz), list, image));
            }
        }
        return pieces;
    }

    // A block's picture: its bricks drawn alone at their cells of the block, as the joined map draws a grid.
    private static Lba1SceneImage? Picture(Grids.Lba2GridBackend backend, byte[] library, int block)
    {
        var at = (int)BinaryPrimitives.ReadUInt32LittleEndian(library.AsSpan((block - 1) * 4));
        int dx = library[at], dy = library[at + 1], dz = library[at + 2];
        var placements = new List<Lba1Placement>();
        for (var p = 0; p < dx * dy * dz; p++)
        {
            var number = BinaryPrimitives.ReadUInt16LittleEndian(library.AsSpan(at + 3 + p * 4 + 2));
            if (number != 0) placements.Add(new Lba1Placement(p % dx, p / dx % dy, p / dx / dy, number - 1));
        }
        return placements.Count == 0 ? null : Lba1GridRenderer.Render(new[] { new Lba1Tile(placements, 0, 0, 0) }, backend.Brick, backend.Palette);
    }

    // The rooms' chests: their scenes' sprite actors that picture one.
    public static List<Chest> Chests(Lba2Interiors interiors, Lba1Area area, (int X, int Y, int Z)[] offsets)
    {
        var chests = new List<Chest>();
        for (var ti = 0; ti < area.Tiles.Count; ti++)
        {
            if (interiors.LoadScene(area.Tiles[ti].Scene) is not { } scene) continue;
            foreach (var a in scene.Actors.Skip(1).Where(a => a.IsSprite))
            {
                // (a sprite's place is the middle of its foot; its size measured on its picture: the small chests 78 pixels wide and 65 high,
                // the big one 120 by 98 -- 24 pixels a cell along each of the long and short sides, 15 a layer up)
                bool alongZ = ChestsAlongZ.Contains(a.Sprite), alongX = ChestsAlongX.Contains(a.Sprite), big = a.Sprite == BigChest;
                if (!alongZ && !alongX && !big) continue;
                var (lng, sht, high) = big ? (1700.0, 860.0, 650.0) : (1130.0, 530.0, 440.0);
                chests.Add(new Chest(ti, a.Sprite, offsets[ti].X + a.X / 512.0, offsets[ti].Y + a.Y / 256.0, offsets[ti].Z + a.Z / 512.0, lng, sht, high, alongX || big));
            }
        }
        return chests;
    }

    // ---- the models ------------------------------------------------------------------------------------------------------------------

    // the colours the bricks and sprites picture (the interiors' palette; matched in Otringal's when drawn)
    private static readonly (float R, float G, float B) Gold = (190, 145, 70), DarkGold = (132, 100, 50), RedMarble = (156, 60, 36),
        Purple = (125, 104, 155), DarkPurple = (98, 82, 126), PotGold = (196, 146, 70), Soil = (60, 36, 12), Globe = (190, 245, 210), GlobeTop = (240, 250, 225),
        Cushion = (170, 62, 44), Fringe = (140, 40, 34), Wood = (60, 36, 12), Pewter = (96, 96, 100), Wax = (215, 215, 205), Flame = (250, 200, 80),
        GreenMarble = (70, 98, 70), WhiteMarble = (225, 228, 228), Soot = (14, 12, 12), Iron = (44, 44, 48), Glass = (66, 92, 100), GlassLow = (92, 84, 112),
        ChestBody = (96, 92, 110), ChestLid = (118, 112, 132), ChestBand = (44, 30, 20), PlinthGreen = (64, 88, 64);

    // Everything the rooms' furniture adds: each room's faces (BrickBuilding faces, in the island's world) and the chests' collision boxes.
    public sealed record Made(Dictionary<int, List<BrickBuilding.Face>> Faces, List<(double X0, double Y0, double Z0, double X1, double Y1, double Z1)> Boxes, List<string> Log);

    // The pieces' and chests' models. `place`: a point of the palace in cells (fractions of one) and layers to the island's world;
    // `colour`: a colour seen from a way (a unit normal in the world) as a colour of the island's palette; `faces`: which way a piece
    // looks (a unit along x or z: away from the wall it stands at, into its room); `wall`: the wallpaper's colour at a layer.
    public static Made Build(List<Piece> pieces, List<Chest> chests, Func<double, double, double, Vector3> place, Func<(float R, float G, float B), Vector3, int> colour,
        Func<Piece, (int Nx, int Nz)> faces, Func<int, (float R, float G, float B)> wall)
    {
        var made = new Dictionary<int, List<BrickBuilding.Face>>();
        var boxes = new List<(double, double, double, double, double, double)>();
        var counts = new Dictionary<(string One, string Many), int>();
        void Count(string one, string many) => counts[(one, many)] = counts.GetValueOrDefault((one, many)) + 1;
        foreach (var piece in pieces)
        {
            var list = made.TryGetValue(piece.Tile, out var l) ? l : made[piece.Tile] = new();
            var (ox, oy, oz) = piece.Origin; var (dx, dy, dz) = piece.Size;
            var (nx, nz) = faces(piece);
            // (the model's frame: its front along +x, about the middle of the block's foot; turned to the way the piece looks)
            var kit = new Kit(list, place, colour, ox + dx / 2.0, oy, oz + dz / 2.0, nx, nz);
            // (its depth -- along the way it looks -- and width, in room units)
            double depth = (nx != 0 ? dx : dz) * 512.0, width = (nx != 0 ? dz : dx) * 512.0, high = dy * 256.0;
            switch (piece.Kind)
            {
                case Kind.Column: Column(kit, high); Count("column", "columns"); break;
                case Kind.Plant: Plant(kit, 0); Count("potted plant", "potted plants"); break;
                case Kind.PlinthPlant:
                    kit.Box(new(-480, 0, -480), new(480, 230, 480), PlinthGreen, PlinthGreen);
                    kit.Box(new(-500, 230, -500), new(500, 256, 500), DarkGold, Gold);
                    Plant(kit, 256); Count("plant on a plinth", "plants on plinths"); break;
                case Kind.Lamp: Lamp(kit); Count("wall lamp", "wall lamps"); break;
                case Kind.Bench:
                    // (its long side along the block's longer one: across the way it "looks", a bench has no front)
                    var benchKit = new Kit(list, place, colour, ox + dx / 2.0, oy, oz + dz / 2.0, dx > dz ? 0 : 1, dx > dz ? 1 : 0);
                    Bench(benchKit, Math.Max(dx, dz) * 512.0, Math.Min(dx, dz) * 512.0); Count("bench", "benches"); break;
                case Kind.Candelabra:
                    var armsKit = new Kit(list, place, colour, ox + dx / 2.0, oy, oz + dz / 2.0, dx > dz ? 0 : 1, dx > dz ? 1 : 0);
                    Candelabra(armsKit); Count("candelabra", "candelabras"); break;
                case Kind.Candle: Candle(kit); Count("candlestick", "candlesticks"); break;
                case Kind.Fireplace: Fireplace(kit, depth, width, high); Count("fireplace", "fireplaces"); break;
                case Kind.Portrait:
                    if (piece.Image is not { } portrait) break;
                    Picture(list, portrait, piece, nx, nz, place, colour, wall);
                    Count("portrait", "portraits");
                    break;
                case Kind.RoundWindow:
                    if (piece.Image is not { } round) break;
                    // (a window: its picture on both faces of the wall -- outside, the window alone, not the frame and leaves round it)
                    Picture(list, round, piece, nx, nz, place, colour, wall);
                    Picture(list, round, piece, -nx, -nz, place, colour, wall, trim: 0.17);
                    Count("round window (inside and out)", "round windows (inside and out)");
                    break;
                case Kind.Window:
                    Window(kit, depth, width, high, inside: true);
                    // (and on the wall's other face, looking out: the same window seen from outside)
                    Window(new Kit(list, place, colour, ox + dx / 2.0, oy, oz + dz / 2.0, -nx, -nz), depth, width, high, inside: false);
                    Count("window (inside and out)", "windows (inside and out)"); break;
            }
        }
        foreach (var chest in chests)
        {
            var list = made.TryGetValue(chest.Tile, out var l) ? l : made[chest.Tile] = new();
            var kit = new Kit(list, place, colour, chest.X, chest.Y, chest.Z, chest.AlongX ? 0 : 1, chest.AlongX ? 1 : 0);
            ChestModel(kit, chest.Long, chest.Short, chest.High);
            // (in the way, as the actor is)
            double hx = (chest.AlongX ? chest.Long : chest.Short) / 1024.0, hz = (chest.AlongX ? chest.Short : chest.Long) / 1024.0;
            var lo = place(chest.X - hx, chest.Y, chest.Z - hz); var hi = place(chest.X + hx, chest.Y + chest.High / 256.0, chest.Z + hz);
            boxes.Add((lo.X, lo.Y, lo.Z, hi.X, hi.Y, hi.Z));
            Count(chest.Sprite == BigChest ? "big chest" : "chest", chest.Sprite == BigChest ? "big chests" : "chests");
        }
        var log = new List<string> { $"the rooms' furniture: {string.Join(", ", counts.OrderByDescending(c => c.Value).Select(c => $"{c.Value} {(c.Value == 1 ? c.Key.One : c.Key.Many)}"))}" };
        return new Made(made, boxes, log);
    }

    private static void Column(Kit k, double high)
    {
        // a red marble shaft on a gold base, a gold capital and its square top
        k.Box(new(-220, 0, -220), new(220, 60, 220), DarkGold, Gold, bottom: false);
        k.Lathe(new[] { (190.0, 60.0), (190.0, 110.0), (150.0, 130.0) }, 8, _ => Gold, top: false, bottom: false);
        k.Lathe(new[] { (150.0, 130.0), (150.0, high - 150) }, 8, _ => RedMarble, top: false, bottom: false);
        k.Lathe(new[] { (150.0, high - 150), (195.0, high - 110), (195.0, high - 70) }, 8, _ => Gold, top: false, bottom: false);
        k.Box(new(-230, (float)high - 70, -230), new(230, (float)high, 230), DarkGold, Gold, bottom: true);
    }

    private static void Plant(Kit k, double y0)
    {
        // a gold pot, its earth, and purple leaves fanning up and out of it
        k.Lathe(new[] { (150.0, y0), (250.0, y0 + 60), (320.0, y0 + 190), (310.0, y0 + 320), (255.0, y0 + 395), (295.0, y0 + 430), (285.0, y0 + 470) }, 8,
            band => band is 1 or 4 ? DarkGold : PotGold, top: false, bottom: true);
        k.Lathe(new[] { (285.0, y0 + 470), (250.0, y0 + 450), (0.0, y0 + 450) }, 8, _ => Soil, top: false, bottom: false);
        var root = new Vector3(0, (float)(y0 + 450), 0);
        for (var i = 0; i < 9; i++)
        {
            var a = i * Math.PI * 2 / 9 + (i % 2) * 0.3;
            var reach = i % 3 == 0 ? 470 : 400;
            var rise = i % 3 == 1 ? 560 : i % 3 == 2 ? 380 : 460;
            var tip = root + new Vector3((float)(Math.Cos(a) * reach), rise, (float)(Math.Sin(a) * reach));
            var side = Vector3.Normalize(new Vector3((float)-Math.Sin(a), 0, (float)Math.Cos(a))) * 120;
            k.Leaf(root + new Vector3(0, 30, 0), tip, side, i % 2 == 0 ? Purple : DarkPurple);
        }
        // (a tuft standing up in the middle)
        k.Leaf(root, root + new Vector3(40, 620, 30), new Vector3(110, 0, -60), Purple);
        k.Leaf(root, root + new Vector3(-30, 600, -40), new Vector3(-60, 0, 110), DarkPurple);
    }

    private static void Lamp(Kit k)
    {
        // a gold bracket out of the wall (behind it, -x), a glass globe under it, a gold leaf over it on the wall
        k.Box(new(-256, 300, -30), new(-40, 340, 30), DarkGold, Gold, bottom: true);
        k.Lathe(new[] { (0.0, 290.0), (35.0, 300.0), (35.0, 330.0), (0.0, 340.0) }, 6, _ => Gold, top: false, bottom: false, cx: -10);
        k.Lathe(new[] { (0.0, 120.0), (80.0, 145.0), (112.0, 200.0), (95.0, 255.0), (45.0, 290.0), (0.0, 295.0) }, 8,
            band => band >= 3 ? GlobeTop : Globe, top: false, bottom: false, cx: -10);
        k.Quad(new(-254, 330, -10), new(-254, 470, -110), new(-254, 600, 0), new(-254, 470, 110), Gold, Vector3.UnitX);
    }

    private static void Bench(Kit k, double length, double depth)
    {
        // a red cushion on a fringed red skirt with a gold band, on four dark legs -- its length along z
        double l = length / 2 - 60, d = depth / 2 - 60;
        k.Box(new((float)-d, 300, (float)-l), new((float)d, 470, (float)l), Cushion, (196, 72, 54), bottom: false);
        k.Box(new((float)-d - 10, 280, (float)-l - 10), new((float)d + 10, 305, (float)l + 10), DarkGold, Gold, bottom: true);
        k.Box(new((float)-d + 30, 150, (float)-l + 30), new((float)d - 30, 280, (float)l - 30), Fringe, Fringe, lid: false, bottom: true);
        foreach (var sx in new[] { -1, 1 })
            foreach (var sz in new[] { -1, 1 })
                k.Box(new((float)(sx * (d - 70) - 40), 0, (float)(sz * (l - 70) - 40)), new((float)(sx * (d - 70) + 40), 150, (float)(sz * (l - 70) + 40)), Wood, Wood, lid: false);
    }

    private static void Candelabra(Kit k)
    {
        // a pewter foot and stem, an arm across with three candles -- the arm along z
        k.Lathe(new[] { (140.0, 0.0), (140.0, 30.0), (70.0, 60.0), (35.0, 120.0) }, 8, _ => Pewter, top: false, bottom: true);
        k.Box(new(-25, 120, -25), new(25, 600, 25), Pewter, Pewter, lid: false);
        k.Box(new(-22, 580, -260), new(22, 625, 260), Pewter, Pewter, bottom: true);
        foreach (var (z, rise) in new[] { (-240.0, 0.0), (0.0, 60.0), (240.0, 0.0) })
        {
            k.Box(new(-35, (float)(625 + rise), (float)(z - 35)), new(35, (float)(660 + rise), (float)(z + 35)), Pewter, Pewter, bottom: true);
            k.Box(new(-22, (float)(660 + rise), (float)(z - 22)), new(22, (float)(900 + rise), (float)(z + 22)), Wax, Wax);
            k.Leaf(new Vector3(0, (float)(905 + rise), (float)z), new Vector3(0, (float)(990 + rise), (float)z), new Vector3(0, 0, 20), Flame);
            k.Leaf(new Vector3(0, (float)(905 + rise), (float)z), new Vector3(0, (float)(990 + rise), (float)z), new Vector3(20, 0, 0), Flame);
        }
    }

    private static void Candle(Kit k)
    {
        k.Lathe(new[] { (100.0, 0.0), (100.0, 25.0), (40.0, 55.0) }, 8, _ => Pewter, top: true, bottom: true);
        k.Box(new(-20, 55, -20), new(20, 450, 20), Pewter, Pewter, lid: false);
        k.Box(new(-45, 450, -45), new(45, 490, 45), Pewter, Pewter, bottom: true);
        k.Box(new(-24, 490, -24), new(24, 700, 24), Wax, Wax);
        k.Leaf(new Vector3(0, 705, 0), new Vector3(0, 785, 0), new Vector3(0, 0, 20), Flame);
        k.Leaf(new Vector3(0, 705, 0), new Vector3(0, 785, 0), new Vector3(20, 0, 0), Flame);
    }

    private static void Fireplace(Kit k, double depth, double width, double high)
    {
        // a green marble chimney breast against the wall (behind it, -x), a gold mantel shelf on it, the hearth in front: a white marble
        // surround round the black fireplace, an iron grate in it
        double d = depth / 2, w = width / 2, mantel = high - 130;
        k.Box(new((float)-d, 0, (float)-w), new((float)d, (float)mantel, (float)w), GreenMarble, GreenMarble, lid: false);
        k.Box(new((float)-d, (float)mantel, (float)-w - 20), new((float)d + 50, (float)high, (float)w + 20), DarkGold, Gold, bottom: true);
        var front = (float)d;
        k.Quad(new(front + 4, 0, -700), new(front + 4, 0, 700), new(front + 4, 920, 700), new(front + 4, 920, -700), WhiteMarble, Vector3.UnitX);
        k.Quad(new(front + 8, 0, -470), new(front + 8, 0, 470), new(front + 8, 640, 470), new(front + 8, 640, -470), Soot, Vector3.UnitX);
        k.Quad(new(front + 8, 640, -470), new(front + 8, 640, 470), new(front + 8, 720, 360), new(front + 8, 720, -360), Soot, Vector3.UnitX);
        for (var bar = 0; bar < 3; bar++)
            k.Box(new(front - 60, (float)(70 + bar * 70), -380), new(front + 30, (float)(95 + bar * 70), 380), Iron, Iron);
        // (gold strips down the surround's sides)
        foreach (var s in new[] { -1, 1 })
            k.Quad(new(front + 12, 0, s * 700 - 60), new(front + 12, 0, s * 700 + 60), new(front + 12, 920, s * 700 + 60), new(front + 12, 920, s * 700 - 60), Gold, Vector3.UnitX);
    }

    private static void Window(Kit k, double depth, double width, double high, bool inside)
    {
        // on the wall's face (in front of it, +x): a gold frame round two tall panes, a bar across a third of the way down, gold leaves on the
        // lower panes (as the bricks picture it)
        var f = (float)(depth / 2 + 6);
        double w = width / 2, u0 = -w + 0.08 * width, u1 = w - 0.08 * width, v0 = 0.05 * high, v1 = 0.95 * high, t = 0.045 * width;
        double cross = v1 - (v1 - v0) * 0.33, mid = 0.0;
        void Q(double ua, double va, double ub, double vb, (float, float, float) c, float off = 0) =>
            k.Quad(new(f + off, (float)va, (float)ua), new(f + off, (float)va, (float)ub), new(f + off, (float)vb, (float)ub), new(f + off, (float)vb, (float)ua), c, Vector3.UnitX);
        // the glass, darker low down, and the frame over it
        Q(u0, v0, u1, cross, GlassLow);
        Q(u0, cross, u1, v1, Glass);
        Q(u0, v0, u0 + t, v1, Gold, 4); Q(u1 - t, v0, u1, v1, Gold, 4);
        Q(u0, v0, u1, v0 + t, Gold, 4); Q(u0, v1 - t, u1, v1, Gold, 4);
        Q(mid - t / 2, v0, mid + t / 2, v1, DarkGold, 4);
        Q(u0, cross - t / 2, u1, cross + t / 2, DarkGold, 4);
        if (!inside) return;
        foreach (var s in new[] { -1, 1 })
        {
            double cu = s * (u1 - u0) / 4, cv = v0 + (cross - v0) * 0.35, r = (u1 - u0) * 0.09;
            k.Quad(new(f + 6, (float)(cv - r * 1.6), (float)cu), new(f + 6, (float)cv, (float)(cu + r)), new(f + 6, (float)(cv + r * 1.6), (float)cu), new(f + 6, (float)cv, (float)(cu - r)), Gold, Vector3.UnitX);
        }
    }

    private static void ChestModel(Kit k, double length, double depth, double high)
    {
        // a mauve-grey chest, its long side along z: the box, a dark band where the lid shuts, the lid, gold leaves on its long sides and lid,
        // a gold clasp on the front (+x)
        double l = length / 2, d = depth / 2, band = high * 0.6;
        k.Box(new((float)-d, 0, (float)-l), new((float)d, (float)band, (float)l), ChestBody, ChestBody, lid: false);
        k.Box(new((float)-d + 10, (float)band, (float)-l + 10), new((float)d - 10, (float)(band + 30), (float)l - 10), ChestBand, ChestBand, lid: false, bottom: false);
        k.Box(new((float)-d - 8, (float)(band + 30), (float)-l - 8), new((float)d + 8, (float)high, (float)l + 8), ChestBody, ChestLid, bottom: true);
        foreach (var s in new[] { -1, 1 })
        {
            var x = (float)(s * (d + 4));
            foreach (var z in new[] { -l * 0.45, l * 0.45 })
                k.Quad(new(x, (float)(band * 0.25), (float)z), new(x, (float)(band * 0.5), (float)(z + 90)), new(x, (float)(band * 0.75), (float)z), new(x, (float)(band * 0.5), (float)(z - 90)), Gold, new Vector3(s, 0, 0));
        }
        var top = (float)high + 4;
        k.Quad(new(0, top, (float)(-l * 0.3)), new((float)(d * 0.5), top, 0), new(0, top, (float)(l * 0.3)), new((float)(-d * 0.5), top, 0), Gold, Vector3.UnitY);
        k.Box(new((float)d, (float)(band - 90), -45), new((float)d + 30, (float)(band + 40), 45), DarkGold, Gold);
    }

    // A portrait's or the round window's picture on a face of its wall (the side it looks out of, `nx`, `nz`): its block's picture looked up
    // where the isometric picture draws each spot of the face, a grid of them (Samples a cell across, a layer up), the ones in the
    // wallpaper's colour left out, the rest matched in Otringal's palette and merged into rectangles of one colour, just off the wall. (The
    // bricks picture a block's +x and +z sides: a picture on its other side is that one, as if seen through the wall.) `trim`: the share of
    // the face left out at each of its sides.
    private const int SamplesAcross = 6, SamplesUp = 5;
    private static void Picture(List<BrickBuilding.Face> list, Lba1SceneImage image, Piece piece, int nx, int nz, Func<double, double, double, Vector3> place,
        Func<(float R, float G, float B), Vector3, int> colour, Func<int, (float R, float G, float B)> wall, double trim = 0)
    {
        var (ox, oy, oz) = piece.Origin; var (dx, dy, dz) = piece.Size;
        var alongZ = nx != 0;
        var across = alongZ ? dz : dx;
        int nu = across * SamplesAcross, nv = dy * SamplesUp;
        var normal = Vector3.Normalize(nx != 0 ? new Vector3(nx, 0, 0) : new Vector3(0, 0, nz));
        var grid = new Dictionary<(int U, int V), int>();
        for (var v = (int)(nv * trim); v < nv - (int)(nv * trim); v++)
            for (var u = (int)(nu * trim); u < nu - (int)(nu * trim); u++)
            {
                // (a spot of the block's +x or +z face, in the block's own units: where the picture has it -- the joined map draws a cell's
                // brick with its top left corner where Project puts the cell's lowest corner, so a point is drawn 24 pixels right of and 15
                // below where Project puts it)
                double a = (u + 0.5) / SamplesAcross * 512, h = (v + 0.5) / SamplesUp * 256;
                var p = alongZ ? image.Project(dx * 512 - 1, h, a) : image.Project(a, h, dz * 512 - 1);
                int px = (int)Math.Floor(p.X + 24), py = (int)Math.Floor(p.Y + 15);
                if (px < 0 || py < 0 || px >= image.Width || py >= image.Height || image.Bgra[(py * image.Width + px) * 4 + 3] == 0) continue;
                // (the pixels round it, averaged: the bricks' dithering would come out as specks)
                double r = 0, g = 0, b = 0; var n = 0;
                for (var qy = Math.Max(0, py - 1); qy <= Math.Min(image.Height - 1, py + 1); qy++)
                    for (var qx = Math.Max(0, px - 1); qx <= Math.Min(image.Width - 1, px + 1); qx++)
                    {
                        var o = (qy * image.Width + qx) * 4;
                        if (image.Bgra[o + 3] == 0) continue;
                        r += image.Bgra[o + 2]; g += image.Bgra[o + 1]; b += image.Bgra[o]; n++;
                    }
                (float R, float G, float B) rgb = ((float)(r / n), (float)(g / n), (float)(b / n));
                // (the wall round it: near the wallpaper's colour at that height -- left to the wall's own face)
                var w = wall(oy + v / SamplesUp);
                var dist = (rgb.R - w.R) * (rgb.R - w.R) + (rgb.G - w.G) * (rgb.G - w.G) + (rgb.B - w.B) * (rgb.B - w.B);
                if (dist < 40 * 40) continue;
                var c = colour(rgb, normal);
                grid[(u, v)] = c;
            }
        foreach (var (u0, v0, u1, v1, c) in BrickBuilding.Rectangles(grid))
        {
            // (corners: the face's plane just in front of the wall, cells and layers of the palace)
            double ca = (double)u0 / SamplesAcross, cb = (double)(u1 + 1) / SamplesAcross, la = oy + (double)v0 / SamplesUp, lb = oy + (double)(v1 + 1) / SamplesUp;
            Vector3 P(double along, double layer) => alongZ
                ? place(ox + (nx > 0 ? dx + 0.01 : -0.01), layer, oz + along)
                : place(ox + along, layer, oz + (nz > 0 ? dz + 0.01 : -0.01));
            list.Add(new BrickBuilding.Face(new[] { P(ca, la), P(cb, la), P(cb, lb), P(ca, lb) }, c, normal));
        }
    }

    // A model's maker: points in a room's units about the middle of a piece's foot (+x its front, y up from its foot), turned so that +x is
    // the way it looks, and placed in the island's world; each face given the way it looks outwards (BrickBuilding.AddBodies winds it so).
    private sealed class Kit
    {
        private readonly List<BrickBuilding.Face> list;
        private readonly Func<double, double, double, Vector3> place;
        private readonly Func<(float R, float G, float B), Vector3, int> colour;
        private readonly double cx, cy, cz;
        private readonly int fx, fz;

        public Kit(List<BrickBuilding.Face> list, Func<double, double, double, Vector3> place, Func<(float R, float G, float B), Vector3, int> colour,
            double cx, double cy, double cz, int fx, int fz)
        {
            this.list = list; this.place = place; this.colour = colour; this.cx = cx; this.cy = cy; this.cz = cz;
            (this.fx, this.fz) = fx == 0 && fz == 0 ? (1, 0) : (fx, fz);
        }

        // (a model's (x, z) turned so that +x goes the way it looks: +x stays, +z: x -> z, z -> -x; and so on)
        private (double X, double Z) Turn(double x, double z) => (x * fx - z * fz, x * fz + z * fx);

        private Vector3 World(Vector3 p)
        {
            var (x, z) = Turn(p.X, p.Z);
            return place(cx + x / 512, cy + p.Y / 256, cz + z / 512);
        }

        private Vector3 WorldNormal(Vector3 n)
        {
            // (the island stretches a room Across times across and Up times up: a normal shrinks by the same)
            var (x, z) = Turn(n.X, n.Z);
            var w = new Vector3((float)(x / PalaceIsland.Across), (float)(n.Y / PalaceIsland.Up), (float)(z / PalaceIsland.Across));
            return w.LengthSquared() > 0 ? Vector3.Normalize(w) : Vector3.UnitY;
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, (float R, float G, float B) rgb, Vector3 normal)
        {
            var n = WorldNormal(normal);
            list.Add(new BrickBuilding.Face(new[] { World(a), World(b), World(c), World(d) }, colour(rgb, n), n));
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, (float R, float G, float B) rgb, Vector3 normal)
        {
            var n = WorldNormal(normal);
            list.Add(new BrickBuilding.Face(new[] { World(a), World(b), World(c) }, colour(rgb, n), n));
        }

        // A box's faces: its sides and bottom in `side`, its top in `top` (its bottom only when asked, its top -- `lid` -- unless told not:
        // the faces nobody sees left off).
        public void Box(Vector3 lo, Vector3 hi, (float R, float G, float B) side, (float R, float G, float B) top, bool bottom = false, bool lid = true)
        {
            float xa = lo.X, ya = lo.Y, za = lo.Z, xb = hi.X, yb = hi.Y, zb = hi.Z;
            if (lid) Quad(new(xa, yb, za), new(xb, yb, za), new(xb, yb, zb), new(xa, yb, zb), top, Vector3.UnitY);
            if (bottom) Quad(new(xa, ya, za), new(xb, ya, za), new(xb, ya, zb), new(xa, ya, zb), side, -Vector3.UnitY);
            Quad(new(xa, ya, za), new(xb, ya, za), new(xb, yb, za), new(xa, yb, za), side, -Vector3.UnitZ);
            Quad(new(xa, ya, zb), new(xb, ya, zb), new(xb, yb, zb), new(xa, yb, zb), side, Vector3.UnitZ);
            Quad(new(xa, ya, za), new(xa, ya, zb), new(xa, yb, zb), new(xa, yb, za), side, -Vector3.UnitX);
            Quad(new(xb, ya, za), new(xb, ya, zb), new(xb, yb, zb), new(xb, yb, za), side, Vector3.UnitX);
        }

        // A turned shape: rings of `sides` points at each (radius, height) of the profile (from the foot up), about (cx, cz); band i (between
        // rings i and i + 1) in band(i); caps closing the top and the bottom rings when asked.
        public void Lathe((double R, double Y)[] profile, int sides, Func<int, (float R, float G, float B)> band, bool top, bool bottom, double cx = 0, double cz = 0)
        {
            Vector3 P(int ring, int s)
            {
                var a = 2 * Math.PI * (s + 0.5) / sides;
                return new((float)(cx + Math.Cos(a) * profile[ring].R), (float)profile[ring].Y, (float)(cz + Math.Sin(a) * profile[ring].R));
            }
            for (var i = 0; i + 1 < profile.Length; i++)
            {
                var (r0, y0) = profile[i]; var (r1, y1) = profile[i + 1];
                if (r0 <= 0 && r1 <= 0) continue;
                for (var s = 0; s < sides; s++)
                {
                    var a = 2 * Math.PI * (s + 1.0) / sides;
                    // (outwards: across the profile's slope, round the axis)
                    var n = new Vector3((float)(Math.Cos(a) * (y1 - y0)), (float)(r0 - r1), (float)(Math.Sin(a) * (y1 - y0)));
                    if (n.LengthSquared() < 1e-6f) continue;
                    n = Vector3.Normalize(n);
                    var s1 = (s + 1) % sides;
                    if (r0 <= 0) Tri(P(i, s), P(i + 1, s), P(i + 1, s1), band(i), n);
                    else if (r1 <= 0) Tri(P(i, s), P(i, s1), P(i + 1, s), band(i), n);
                    else Quad(P(i, s), P(i, s1), P(i + 1, s1), P(i + 1, s), band(i), n);
                }
            }
            void Cap(int ring, Vector3 n)
            {
                if (profile[ring].R <= 0) return;
                var middle = new Vector3((float)cx, (float)profile[ring].Y, (float)cz);
                for (var s = 0; s < sides; s += 2)
                    Quad(middle, P(ring, s), P(ring, (s + 1) % sides), P(ring, (s + 2) % sides), band(Math.Max(0, ring - (n.Y > 0 ? 1 : 0))), n);
            }
            if (top) Cap(profile.Length - 1, Vector3.UnitY);
            if (bottom) Cap(0, -Vector3.UnitY);
        }

        // A leaf (a flame, a frond): a flat diamond from its root to its tip, `side` its half width at its widest, seen from both sides.
        public void Leaf(Vector3 root, Vector3 tip, Vector3 side, (float R, float G, float B) rgb)
        {
            var wide = root + (tip - root) * 0.4f;
            var n = Vector3.Cross(tip - root, side);
            if (n.LengthSquared() < 1e-6f) return;
            n = Vector3.Normalize(n);
            Quad(root, wide + side, tip, wide - side, rgb, n);
            Quad(root, wide - side, tip, wide + side, rgb, -n);
        }
    }
}
