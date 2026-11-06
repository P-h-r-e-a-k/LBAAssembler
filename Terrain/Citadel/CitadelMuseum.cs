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
        foreach (var (pos, image) in painted.OrderBy(p => p.Key)) bricks[pos] = AddBrick(bkg, header, image.Encode());
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
}
