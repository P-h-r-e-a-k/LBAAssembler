using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Assets;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;

namespace LBAAssembler.Terrain.Citadel;

// Miss Bloop's museum's displays the story adds, in the museum's own bricks (scene 15's grid, LBA_BKG.HQR): the build's side -- the
// story's (who reads what, RaceTrackStory.Chapters.cs HiddenPages) stands its people by them.
//   - the blueprints' case (the user, 2026-11-06: "Inside the museum the blueprints have been put into a copy of the holder of Twinsen's
//     Tunic instead of the standard display cases"): a copy of the hall's standard display case -- the glass case on legs by the west wall
//     with an open book under its glass (block StandardCase of the museum's library) -- along the same wall under the window, the book
//     under its glass a sheet of blueprints: the bricks that draw the book's pages drawn again in blueprint blue, a ruled grid and the
//     track's loop on it in white.
//   - the picture of Twinsen with a trophy (a visitor: "you look like the guy in the picture! The one with the trophy!", IslandRetell's
//     21; the user, 2026-11-06, of the picture that wasn't there: add it): a copy of the hall's portrait on its north wall (the Sup in a
//     gold frame, block PortraitBlock) on the same wall by the corner, near the tunic's case, its canvas painted again -- Twinsen in his
//     tunic, head and shoulders, holding a gold cup in front of him (TrophyPainting, painted onto the wall's slant); Action in front of it
//     says what it is (the story's, RaceTrackStory.Chapters.cs: TrophyPicture).
// The bricks and the block they make go at the ends of LBA_BKG.HQR's bricks and of the museum's block library (the game's own grids,
// which share that library, don't name them), the case into the grid's empty cells. A build over a folder a build already changed finds
// its bricks and block there and uses them again (LBA_BKG.HQR isn't one of the files a build puts back first).
internal static class CitadelMuseum
{
    public const int Museum = 15;
    // the hall's standard display case: 1 x 4 x 3 cells (x 23, layers 3-6, z 50-52 in the hall, its glass facing east into the room)
    public const int StandardCase = 120;
    // the blueprints' case: along the west wall from it, under the window (the chair, z 48, between the two)
    public static readonly (int X, int Y, int Z) BlueprintCase = (23, 3, 44);
    // the hall's portrait on its north wall (z 38): 2 x 4 x 1 cells, the Sup's, at x 31-32, layers 6-9 -- the wall there is block WallBlock
    // (3 wide), the portrait in the place of its first two cells
    public const int PortraitBlock = 101, WallBlock = 2;
    // the trophy picture: the same wall's first two cells by the corner with the west wall (x 23-24: the wall's own block starts at 23,
    // as at 31), the tunic's case on its pedestal three cells along
    public static readonly (int X, int Y, int Z) TrophyPicture = (23, 6, 38);

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Museum * 2 + 1 >= table.Length) { log.Add("The museum's displays left out: no scene 15"); return log; }
        var gridEntry = header.Gri + table[Museum * 2 + 1];
        var raw = bkg.Read(gridEntry);
        var libraryEntry = header.Bll + raw[0];
        var library = bkg.Read(libraryEntry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        byte[]? Brick(int b) => b >= 0 && b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? bkg.Read(header.Brk + b) : null;

        // ---- the blueprints' case
        if (GridPaint.Info(library, StandardCase) is not { Dx: 1, Dy: 4, Dz: 3 })
            log.Add("The blueprints' case left out: the museum's library has no standard display case (block 120)");
        else
        {
            var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
            var painted = Repaint(library, StandardCase, Brick, Blueprint(palette));
            var block = AddBlock(bkg, header, ref library, StandardCase, painted);
            var (x, y, z) = BlueprintCase;
            var cells = GridPaint.PlaceCells(library, block, x, y, z)!;
            var now = cells.Select(c => Lba1GridEdit.Get(shape, c.X, c.Y, c.Z)).ToList();
            if (now.Zip(cells).All(p => p.First.Block == p.Second.Block && p.First.Pos == p.Second.Pos))
                log.Add($"The blueprints' case: in the museum already (block {block})");
            else if (now.Any(c => c.Block != 0))
                log.Add("The blueprints' case left out: the museum's west wall under the window isn't clear");
            else
            {
                shape = Lba1GridEdit.SetCells(shape, cells);
                log.Add($"The blueprints' case: the museum's standard display case (block {StandardCase}) copied at cells ({x}, {z})-({x}, {z + 2}), " +
                        $"layers {y}-{y + 3}, block {block}, its book's {painted.Count} bricks drawn as blueprints");
            }
        }

        // ---- the picture of Twinsen with a trophy
        if (GridPaint.Info(library, PortraitBlock) is not { Dx: 2, Dy: 4, Dz: 1 })
            log.Add("The trophy picture left out: the museum's library has no portrait on the north wall (block 101)");
        else
        {
            var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
            var painted = Repaint(library, PortraitBlock, Brick, TrophyPainting(palette));
            var block = AddBlock(bkg, header, ref library, PortraitBlock, painted);
            var (x, y, z) = TrophyPicture;
            var cells = GridPaint.PlaceCells(library, block, x, y, z)!;
            var now = cells.Select(c => Lba1GridEdit.Get(shape, c.X, c.Y, c.Z)).ToList();
            if (now.Zip(cells).All(p => p.First.Block == p.Second.Block && p.First.Pos == p.Second.Pos))
                log.Add($"The trophy picture: in the museum already (block {block})");
            // (the wall's cells as the portrait's are: the wall block's first two cells of each layer)
            else if (now.Zip(cells).Any(p => p.First.Block != WallBlock || p.First.Pos % 3 != (p.Second.X - x)))
                log.Add("The trophy picture left out: the museum's north wall by the corner isn't the plain wall");
            else
            {
                shape = Lba1GridEdit.SetCells(shape, cells);
                log.Add($"The trophy picture: the museum's portrait (block {PortraitBlock}) copied on the north wall at cells ({x}-{x + 1}, {z}), " +
                        $"layers {y}-{y + 3}, block {block}, its canvas's {painted.Count} bricks painted: Twinsen holding up a trophy");
            }
        }

        // ---- into the file: the library, the grid, and the engine's room for a cube's bricks (MapBricks.Install's sums)
        bkg.SetStored(libraryEntry, library);
        if (header.MaxBll < (uint)library.Length) header.MaxBll = (uint)library.Length;
        raw = Lba2GridBackend.FromLba1Shape(raw, shape);
        bkg.SetStored(gridEntry, raw);
        if (header.MaxGri < (uint)raw.Length) header.MaxGri = (uint)raw.Length;
        var (bytes, n) = MapBricks.UsedBrickBytes(raw, library, b => b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? BinaryPrimitives.ReadUInt32LittleEndian(bkg.ExtentOf(header.Brk + b)) : 0);
        if (header.MaxBrickCube < (uint)((n + 1) * 4 + bytes)) header.MaxBrickCube = (uint)((n + 1) * 4 + bytes);
        if (header.MaxMaskCube < (uint)((n + 1) * 4 + bytes + n * 64)) header.MaxMaskCube = (uint)((n + 1) * 4 + bytes + n * 64);
        bkg.SetStored(0, header.Bytes);
        File.WriteAllBytes(bkgPath, bkg.ToBytes());
        return log;
    }

    // ---- the blueprints: the book's pages (its greys, the glass's light pinks over them, the violet of its print) inside the case's glass,
    // in its picture (Repaint's: the case's top-left corner at 16, 16): the paper blue, its print darker, a ruled grid of white lines over it
    internal static Func<int, int, byte, byte> Blueprint(byte[] palette)
    {
        // (the game's blue ramp, 12: its shades 6-8 the paper, 15 the lines)
        byte deep = Nearest(palette, 31, 87, 119), mid = Nearest(palette, 35, 103, 135), light = Nearest(palette, 35, 119, 151), grid = Nearest(palette, 131, 183, 215), line = Nearest(palette, 191, 219, 239);
        return (x, y, colour) =>
        {
            if (x < 30 || x > 95 || y < 30 || y > 76) return colour;
            int ramp = colour >> 4, shade = colour & 15;
            var page = ramp == 3 && shade >= 8 || ramp == 14 && shade >= 9;
            var print = ramp == 3 && shade == 7 || ramp == 14 && shade == 8 || ramp == 13 && shade >= 8;
            if (!page && !print) return colour;
            // (a sheet lying flat, seen as the floor is: u along the world's x, v along its z. On it the track's loop, round the sheet's
            // middle, and its ruled grid across both ways)
            int u = x + 2 * y, v = 2 * y - x;
            double du = (u - 164) / 27.0, dv = (v - 41) / 36.0, ring = du * du + dv * dv;
            if (ring > 0.74 && ring < 1.0) return line;
            if ((u % 12 + 12) % 12 < 2 || (v % 12 + 12) % 12 < 2) return grid;
            // (the print a shade darker: the drawings under the grid)
            if (print) return deep;
            var bright = ramp == 3 ? shade - 8 : shade - 9;
            return bright >= 2 ? light : bright >= 1 ? mid : deep;
        };
    }

    // ---- the trophy painting: the portrait's canvas inside its gold frame (in Repaint's picture of the portrait block: x 27-47, its top
    // edge y 40 + (x - 27) / 2, 30 lines down -- the wall's slant), painted again from the rows below, one character a pixel: Twinsen
    // head and shoulders, his hair's tuft on top, holding a gold cup in both hands in front of his tunic, against a dark red ground with
    // a lighter glow round his head, every shape outlined dark (the user, 2026-11-06: "the poster of Twinsen with the trophy looks
    // terrible" -- the first was a whole figure with the cup over his head, too small to make out at 21 x 30)
    private static readonly string[] Trophy =
    {
        "rrrrrrrrRRRkkrrrrrrrr",
        "rrrrrrRRRRkhhRRrrrrrr",
        "rrrrrRRRRkhhkRRRrrrrr",
        "rrrrRRRRkhhRRRRRRrrrr",
        "rrrRRRRRkkhkkRRRRRrrr",
        "rrrRRRRkhhhhhkRRRRrrr",
        "rrRRRRkhhhhhhHkRRRRrr",
        "rrRRRkhhhssshHHkRRRrr",
        "rrRRRkhsssssssHkRRRrr",
        "rrRRRkhsHsssHsHkRRRrr",
        "rrRRRkssesssesSkRRRrr",
        "rrRRRkssssSsssSkRRRrr",
        "rrRRRkssssssssSkRRRrr",
        "rrrRRRkssmmmsskRRRrrr",
        "rrrRRRRkssssskRRRRrrr",
        "rrrrRRRRkSsSkRRRRrrrr",
        "rrrrrkkkkkkkkkkkrrrrr",
        "rrrrGGgyyggggggGGrrrr",
        "rrrGkGgygggggggGkGrrr",
        "rrkGtkGyggggggGkTGkrr",
        "rkrtGkGyggggggGkGTrkr",
        "kttksskGgggggGksskTTk",
        "tttksstkGgggGkTsskTTT",
        "ttttkkttkGgGktTkkTTTT",
        "ttttttttkGgGktTTTTTTT",
        "tttttttkGgggGkTTTTTTT",
        "ttttttkGgggggGkTTTTTT",
        "tttttttkkkkkkkTTTTTTT",
        "ttttttttttttttTTTTTTT",
        "ttttttttttttttTTTTTTT",
    };

    internal static Func<int, int, byte, byte> TrophyPainting(byte[] palette)
    {
        // (the ground dark red, the outlines near black, the cup the gold ramp, Twinsen's tunic its blue, his hair the brown ramp)
        var colours = new Dictionary<char, byte>
        {
            ['r'] = Nearest(palette, 110, 32, 30), ['R'] = Nearest(palette, 140, 48, 40), ['k'] = Nearest(palette, 28, 22, 20),
            ['h'] = Nearest(palette, 126, 80, 44), ['H'] = Nearest(palette, 88, 54, 30), ['s'] = Nearest(palette, 232, 186, 146),
            ['S'] = Nearest(palette, 198, 146, 112), ['e'] = Nearest(palette, 30, 30, 40), ['m'] = Nearest(palette, 170, 52, 48),
            ['t'] = Nearest(palette, 40, 110, 190), ['T'] = Nearest(palette, 28, 78, 140), ['w'] = Nearest(palette, 235, 235, 230),
            ['g'] = Nearest(palette, 228, 178, 60), ['G'] = Nearest(palette, 160, 110, 30), ['y'] = Nearest(palette, 255, 236, 150),
        };
        byte sky = Nearest(palette, 131, 183, 215), ground = Nearest(palette, 155, 171, 123);
        return (x, y, colour) =>
        {
            // (the frame: the gold and brown ramps, 6 and 1, kept)
            if (x < 27 || x > 47 || colour >> 4 is 6 or 1) return colour;
            int u = x - 27, v = y - 40 - u / 2;
            if (v < 0 || v >= Trophy.Length) return colour;
            var c = Trophy[v][u];
            return colours.TryGetValue(c, out var paint) ? paint : v < 16 ? sky : ground;
        };
    }

    // The palette's colour nearest an RGB (redmean; CitadelLba1Rooms'), never 0 (a brick's 0 is drawn as it is, but kept clear of it)
    private static byte Nearest(byte[] palette, int r, int g, int b)
    {
        var best = 1; var bestD = double.MaxValue;
        for (var j = 1; j < 256; j++)
        {
            double pr = palette[j * 3], pg = palette[j * 3 + 1], pb = palette[j * 3 + 2];
            var rm = (r + pr) / 2;
            var d = (2 + rm / 256) * (r - pr) * (r - pr) + 4 * (g - pg) * (g - pg) + (2 + (255 - rm) / 256) * (b - pb) * (b - pb);
            if (d < bestD) { bestD = d; best = j; }
        }
        return (byte)best;
    }

    // A block's bricks drawn as the grid draws them (the block alone, cells far to near), each drawn pixel offered to `paint` with its
    // place in the block's picture (its drawn bricks' top-left corner at 16, 16: lba2scene's / museumblock's pictures) and its colour.
    // The block's positions whose bricks it changed, each with its brick's new picture.
    internal static Dictionary<int, GphImage> Repaint(byte[] library, int block, Func<int, byte[]?> brick, Func<int, int, byte, byte> paint)
    {
        var info = GridPaint.Info(library, block)!.Value;
        var entries = GridPaint.Entries(library, block).ToList();
        var images = new Dictionary<int, GphImage>();
        var placed = new List<(int Pos, int Left, int Top)>();
        // (painter's order: z, then x, then y -- Lba1GridRenderer's)
        for (var k = 0; k < info.Dz; k++)
            for (var i = 0; i < info.Dx; i++)
                for (var j = 0; j < info.Dy; j++)
                {
                    var pos = i + info.Dx * (j + info.Dy * k);
                    if (pos >= entries.Count || brick(entries[pos].Brick) is not { Length: >= 4 } data) continue;
                    images[pos] = GphImage.Decode(data);
                    placed.Add((pos, 24 * (i - k) + (sbyte)data[2], 12 * (i + k) - 15 * j + (sbyte)data[3]));
                }
        if (placed.Count == 0) return new();
        int minX = placed.Min(p => p.Left), minY = placed.Min(p => p.Top);
        // the pixel each place shows: the last brick drawn there
        var shown = new Dictionary<(int X, int Y), (int Pos, int At)>();
        foreach (var (pos, left, top) in placed)
        {
            var image = images[pos];
            for (var v = 0; v < image.Height; v++)
                for (var u = 0; u < image.Width; u++)
                    if (image.Opaque[v * image.Width + u]) shown[(left - minX + 16 + u, top - minY + 16 + v)] = (pos, v * image.Width + u);
        }
        var changed = new HashSet<int>();
        foreach (var ((x, y), (pos, at)) in shown)
        {
            var image = images[pos];
            var now = paint(x, y, image.Pixels[at]);
            if (now == image.Pixels[at]) continue;
            image.Pixels[at] = now;
            changed.Add(pos);
        }
        return changed.ToDictionary(p => p, p => images[p]);
    }

    // A copy of `block` with the repainted positions' bricks the new ones (the bricks added after LBA_BKG.HQR's last, or the ones a
    // build added before), the copy added after the library's last block (or the one a build added before): its number.
    private static int AddBlock(HqrFile bkg, MapBricks.BkgHeader header, ref byte[] library, int block, Dictionary<int, GphImage> painted)
    {
        var info = GridPaint.Info(library, block)!.Value;
        var entries = GridPaint.Entries(library, block).ToList();
        var bricks = entries.Select(e => e.Brick).ToArray();
        foreach (var (pos, image) in painted.OrderBy(p => p.Key)) bricks[pos] = AddBrick(bkg, header, EncodeBrick(image));
        var count = GridPaint.BlockCount(library);
        for (var b = 1; b <= count; b++)
        {
            if (GridPaint.Info(library, b) is not { } other || (other.Dx, other.Dy, other.Dz) != (info.Dx, info.Dy, info.Dz)) continue;
            var theirs = GridPaint.Entries(library, b).ToList();
            if (theirs.Count == entries.Count && theirs.Select((e, p) => e.Shape == entries[p].Shape && e.Sound == entries[p].Sound && e.Brick == bricks[p]).All(s => s)) return b;
        }
        if (count >= 255) throw new InvalidDataException("The museum's block library has 255 blocks already: a grid can't name another.");
        library = GridPaint.AppendBlock(library, info.Dx, info.Dy, info.Dz, 0);
        for (var p = 0; p < entries.Count; p++) library = GridPaint.SetEntry(library, count + 1, p, bricks[p], entries[p].Shape, entries[p].Sound);
        return count + 1;
    }

    // A brick: the file's own with these bytes, else added after its last (header.MaxBrk one more)
    private static int AddBrick(HqrFile bkg, MapBricks.BkgHeader header, byte[] bytes)
    {
        var stored = HqrWriter.StoredEntry(bytes);
        for (var b = header.MaxBrk - 1; b >= 0; b--)
            if (!bkg.IsEmpty(header.Brk + b) && bkg.ExtentOf(header.Brk + b).AsSpan().SequenceEqual(stored)) return b;
        if (header.MaxBrk + 1 > 20000) throw new InvalidDataException("LBA_BKG.HQR has 20000 bricks already: the engine holds no more.");
        bkg.InsertAt(header.Brk + header.MaxBrk, bytes);
        header.MaxBrk += 1;
        return header.MaxBrk - 1;
    }

    // A brick's bytes as the engine draws them (GRAPH.CPP AffGraph): every line's runs cover the brick's whole width, the transparent
    // pixels at its end too -- the engine steps to the next line by the width less what the runs moved it. (GphImage.Encode leaves a
    // line's trailing transparent pixels out: a brick written so drew each line further left than the last, in the game a smear of a
    // few lines and nothing else; the editor's own drawing starts each line afresh and showed it right.)
    internal static byte[] EncodeBrick(GphImage image)
    {
        var output = new List<byte> { (byte)image.Width, (byte)image.Height, (byte)image.OffsetX, (byte)image.OffsetY };
        int w = image.Width;
        for (var y = 0; y < image.Height; y++)
        {
            var runs = new List<byte[]>();
            var x = 0;
            bool Opaque(int at) => image.Opaque[y * w + at];
            byte Pixel(int at) => image.Pixels[y * w + at];
            while (x < w)
            {
                if (!Opaque(x))
                {
                    var n = 0;
                    while (x + n < w && !Opaque(x + n) && n < 64) n++;
                    runs.Add(new[] { (byte)(n - 1) });
                    x += n;
                    continue;
                }
                // (three or more of a colour: a repeat run; else the pixels as they are, up to the next three of a colour)
                var same = 1;
                while (x + same < w && Opaque(x + same) && Pixel(x + same) == Pixel(x) && same < 64) same++;
                if (same >= 3) { runs.Add(new[] { (byte)(0x80 | (same - 1)), Pixel(x) }); x += same; continue; }
                var copy = new List<byte>();
                while (x < w && Opaque(x) && copy.Count < 64)
                {
                    var ahead = 1;
                    while (x + ahead < w && Opaque(x + ahead) && Pixel(x + ahead) == Pixel(x) && ahead < 3) ahead++;
                    if (ahead >= 3 && copy.Count > 0) break;
                    copy.Add(Pixel(x)); x++;
                }
                runs.Add(new[] { (byte)(0x40 | (copy.Count - 1)) }.Concat(copy).ToArray());
            }
            if (runs.Count > 255) throw new InvalidDataException("A brick's line needs more than 255 runs.");
            output.Add((byte)runs.Count);
            foreach (var run in runs) output.AddRange(run);
        }
        return output.ToArray();
    }
}
