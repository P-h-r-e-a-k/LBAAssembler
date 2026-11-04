using System.IO;

namespace LBAAssembler.Terrain;

// The road's own look, in the terms an island's ground is drawn with: tiles of its 256 x 256 texture page, and flat colours of its palette.
// The retail Desert race track's asphalt, curb, hatching and rock are the Desert island's; on any other island those coordinates draw
// whatever happens to be there, so the tiles are copied into the island's spare texture space (Import) and their colours remapped to its
// own palette. The flat colours (the red curb, the arrows, the verge) are picked by matching the Desert colour in the island's palette.
internal sealed record RaceTrackTheme(
    (int X, int Y, int W, int H) Asphalt,
    (int X, int Y) WhiteCurb,          // one pixel of white, stretched over the cell
    (int X, int Y, int W, int H) Hatch,
    (int X, int Y, int W, int H) Rock,
    (int Bank, int Pos) RedCurb,
    (int Bank, int Pos) Arrow,
    (int Bank, int Pos) Sand,
    (int Bank, int Pos)? FlatHatch = null,   // the hatching as a flat colour, where the island has no room for its tile
    byte[]? Palette = null)                 // the island's palette (768 bytes), to match a flat colour to a tile by
{
    // What the Desert island's own ground has, where the retail race track is painted from.
    public static readonly RaceTrackTheme Retail = new((96, 0, 32, 32), (180, 155), (192, 48, 16, 16), (0, 128, 32, 32), (4, 5), (5, 5), (2, 12));
}

internal static class RaceTrackTextures
{
    // The road's tiles on `island`: the Desert island's own, or, for another island, copied into its texture page. The copies go where no
    // cube's polygon reads (Free), their palette indices turned into the nearest colour of the island's own palette. Returns the theme the
    // painter then uses, and a line for the build's log.
    // (`ileFile`: which of the island's files this is -- its fine-weather twin has a palette of its own)
    public static (RaceTrackTheme Theme, string Log) Import(IslandFile island, RaceTrackIsland where, string gameDirectory, string? ileFile = null)
    {
        // (a shared track's copy of an island: as the island's own file, whose texture page and palette the copy has)
        var file = ileFile ?? (where.SharedFrom is { } own ? own.SharedSource.Ile : where.IleFile);
        if (file == RaceTrackIsland.Desert.IleFile) return (RaceTrackTheme.Retail with { Palette = IslandMapRenderer.LoadPalette(gameDirectory, "DESERT") }, "");

        // (the original, kept beside it by a build: the Desert island's own track, built before this one in the same build, has changed the file)
        var desertPath = Path.Combine(gameDirectory, RaceTrackIsland.Desert.IleFile);
        var desert = IslandFile.Load(File.Exists(desertPath + RaceTrackService.BackupSuffix) ? desertPath + RaceTrackService.BackupSuffix : desertPath);
        var from = IslandMapRenderer.LoadPalette(gameDirectory, "DESERT");
        var to = IslandMapRenderer.LoadPalette(gameDirectory, Path.GetFileNameWithoutExtension(file));

        // the nearest colour of the island's palette to each of the Desert's (index 0 is the transparent one on both)
        var map = new byte[256];
        for (var i = 1; i < 256; i++)
        {
            int r = from[i * 3], g = from[i * 3 + 1], b = from[i * 3 + 2];
            var best = 1; var bd = int.MaxValue;
            for (var j = 1; j < 256; j++)
            {
                var d = (to[j * 3] - r) * (to[j * 3] - r) + (to[j * 3 + 1] - g) * (to[j * 3 + 1] - g) + (to[j * 3 + 2] - b) * (to[j * 3 + 2] - b);
                if (d < bd) { bd = d; best = j; }
            }
            map[i] = (byte)best;
        }

        var free = FreeBlocks(island);
        // (an island whose page every block of is used -- Celebration Island's is five big tiles, edge to edge -- has no room for copies:
        // the road is painted with what the page already has)
        if (!HasRoom(free)) return Borrowed(island, where, file, from, to);
        var placed = new List<string>();
        (int X, int Y, int W, int H) Copy(string what, (int X, int Y, int W, int H) tile)
        {
            if (Place(free, tile.W, tile.H) is not { } at) throw new InvalidDataException($"{file}'s ground texture has no room for the road's {what}.");
            for (var y = 0; y < tile.H; y++)
            for (var x = 0; x < tile.W; x++)
                island.GroundTexture[(at.Y + y) * 256 + at.X + x] = map[desert.GroundTexture[(tile.Y + y) * 256 + tile.X + x]];
            placed.Add($"{what} {tile.W}x{tile.H} at ({at.X},{at.Y})");
            return (at.X, at.Y, tile.W, tile.H);
        }

        var asphalt = Copy("asphalt", RaceTrackTheme.Retail.Asphalt);
        var rock = Copy("rock", RaceTrackTheme.Retail.Rock);
        var hatch = Copy("hatching", RaceTrackTheme.Retail.Hatch);
        // the white curb is one pixel; it is given a block of its own so a neighbouring tile's colours can never bleed into it
        var white = Copy("white curb", (RaceTrackTheme.Retail.WhiteCurb.X, RaceTrackTheme.Retail.WhiteCurb.Y, 8, 8));
        // (the whitest the island's palette has, not the nearest to the Desert's kerb, which is a little grey and took a grey one on islands
        // with no colour near it: the user, 2026-10-31, "They're white on other tracks but these appear grey")
        var whitest = 1;
        for (var j = 1; j < 256; j++) if (Math.Min(Math.Min(to[j * 3], to[j * 3 + 1]), to[j * 3 + 2]) >= Math.Min(Math.Min(to[whitest * 3], to[whitest * 3 + 1]), to[whitest * 3 + 2])) whitest = j;
        for (var y = 0; y < white.H; y++)
        for (var x = 0; x < white.W; x++) island.GroundTexture[(white.Y + y) * 256 + white.X + x] = (byte)whitest;

        var theme = new RaceTrackTheme(asphalt, (white.X + 3, white.Y + 3), hatch, rock,
            Near(from, to, RaceTrackTheme.Retail.RedCurb), Near(from, to, RaceTrackTheme.Retail.Arrow), Near(from, to, RaceTrackTheme.Retail.Sand), Palette: to);
        var log = $"the road's look on {where.Name} ({file}): the Desert track's tiles copied into its spare texture space ({string.Join(", ", placed)}), " +
                  $"their colours matched in its own palette; the red curb is colour {theme.RedCurb.Bank * 16 + theme.RedCurb.Pos}, the arrows {theme.Arrow.Bank * 16 + theme.Arrow.Pos}";
        return (theme, log);
    }

    // The kerb texture (RaceTrackBuilder.PaintRoad's smooth kerbs): a block of the page drawn as the road's edge is crossed, mapped onto
    // each triangle along the kerb by the road's own coordinates -- along it in x, KerbTexels a cell, the red and white blocks KerbBlock
    // cells long (red from 0); across it in y, from a cell inside the asphalt's edge (asphalt) over the kerb (KerbTexels rows) to a cell
    // past it (colour 0: the triangle's own flat colour, the verge, shows there). The kerb's red is the colour the flat red kerb shows at
    // the ground's usual light (its ramp's 9th: Desert island's 73), its white the white curb's pixel, the asphalt the asphalt tile's
    // pixels at this scale. Null where the page has no free block for it.
    public const int KerbTexels = 10, KerbWide = 48, KerbTall = 32;
    public const double KerbBlock = 1.6;
    // (`repainted`: the cells the road is about to paint over, whose tiles are free once it has: an island whose page is full, Otringal, Celebration Island)
    public static (int X, int Y)? KerbTexture(IslandFile island, RaceTrackTheme theme, HashSet<(int, int)>? repainted = null)
    {
        var free = FreeBlocks(island, repainted);
        // (the road's own tiles, copied into the page for this build, are not read by any polygon yet)
        void Taken((int X, int Y, int W, int H) t)
        {
            for (var by = t.Y / 8; by <= Math.Min(31, (t.Y + t.H - 1) / 8); by++)
            for (var bx = t.X / 8; bx <= Math.Min(31, (t.X + t.W - 1) / 8); bx++) free[bx, by] = false;
        }
        Taken(theme.Asphalt); Taken(theme.Hatch); Taken(theme.Rock); Taken((theme.WhiteCurb.X - 3, theme.WhiteCurb.Y - 3, 8, 8));
        if (Place(free, KerbWide, KerbTall) is not { } at) return null;
        DrawKerb(island.GroundTexture, island.GroundTexture, theme, at);
        return at;
    }

    // The kerb texture drawn into `into` at `at` from the island's first page (`from`: its asphalt tile and white curb's pixel are there).
    private static void DrawKerb(byte[] from, byte[] into, RaceTrackTheme theme, (int X, int Y) at)
    {
        var red = (byte)(theme.RedCurb.Bank * 16 + 9);
        var white = from[theme.WhiteCurb.Y * 256 + theme.WhiteCurb.X];
        var a = theme.Asphalt;
        for (var y = 0; y < KerbTall; y++)
        for (var x = 0; x < KerbWide; x++)
        {
            byte c;
            if (y < KerbTexels) c = from[(a.Y + y * a.H / KerbTexels % a.H) * 256 + a.X + x * a.W / KerbTexels % a.W];
            else if (y < 2 * KerbTexels) c = (int)Math.Floor(x / (KerbBlock * KerbTexels)) % 2 == 0 ? red : white;
            else c = 0;
            into[(at.Y + y) * 256 + at.X + x] = c;
        }
    }

    // The kerb texture on a page of its own, added to an island whose one page the game fills (Celebration Island's: every block of it read, the kerbs were
    // painted cell by cell in steps). Its first page and its triangles stay as they were: with pages a ground triangle's texture index carries its page in its top
    // bits (IslandFile.GroundPageShift), and while each cube holds fewer definitions than that leaves room for, an index on the first page is the number it was.
    // Null where a cube holds more (its triangles would have to be numbered again) or the island has pages already.
    public static (int X, int Y, int Page)? KerbTextureOwnPage(IslandFile island, RaceTrackTheme theme)
    {
        if (island.GroundPages.Count > 0) return null;
        if (IslandOps.CubeCells(island).Any(c => c.Cube.HasPolygons && c.Cube.TextureDefs.Length / 6 >= 1 << IslandFile.GroundPageShift)) return null;
        var page = new byte[256 * 256];
        DrawKerb(island.GroundTexture, page, theme, (0, 0));
        island.GroundPages.Add(page);
        return (0, 0, island.GroundPages.Count);
    }

    // Whether the page has free blocks for all four of the road's tiles.
    private static bool HasRoom(bool[,] free)
    {
        var copy = (bool[,])free.Clone();
        return Place(copy, 32, 32) is not null && Place(copy, 32, 32) is not null && Place(copy, 16, 16) is not null && Place(copy, 8, 8) is not null;
    }

    // The road's look from the island's own page, where it is full: for the asphalt its flattest dark grey patch (16 x 16, repeated cell
    // by cell it is an even dark surface), for the white curb and the start line a pixel in the middle of its whitest 3 x 3, for the
    // blocking rock and the shoulders its own cliff (the tile most of its blocking triangles are drawn with), and the hatching as the
    // arrows' flat colour. The flat colours are matched as ever.
    // A colour of `from`'s palette (the Desert's) to the nearest of `to`'s in the same position of some bank (the ramp the colour sits in
    // matters as much as the colour: the engine adds the light to it, so its position in the bank is kept)
    private static (int Bank, int Pos) Near(byte[] from, byte[] to, (int Bank, int Pos) colour)
    {
        var i = colour.Bank * 16 + colour.Pos;
        int r = from[i * 3], g = from[i * 3 + 1], b = from[i * 3 + 2];
        var pick = i; var bd = int.MaxValue;
        for (var bank = 0; bank < 16; bank++)
        {
            var j = bank * 16 + colour.Pos;
            var d = (to[j * 3] - r) * (to[j * 3] - r) + (to[j * 3 + 1] - g) * (to[j * 3 + 1] - g) + (to[j * 3 + 2] - b) * (to[j * 3 + 2] - b);
            if (d < bd) { bd = d; pick = j; }
        }
        return (pick / 16, pick % 16);
    }

    private static (RaceTrackTheme Theme, string Log) Borrowed(IslandFile island, RaceTrackIsland where, string file, byte[] from, byte[] to)
    {
        var page = island.GroundTexture;
        int R(int x, int y) => to[page[y * 256 + x] * 3]; int G(int x, int y) => to[page[y * 256 + x] * 3 + 1]; int B(int x, int y) => to[page[y * 256 + x] * 3 + 2];
        // (none of them from a square the engine animates its lava or sea in: what is there changes four times a second, and the start
        // line drawn from it flashed -- AnimatedBlocks)
        var animated = AnimatedBlocks(island);
        (int X, int Y) asphalt = (0, 0); var best = double.MaxValue;
        const int size = 16;
        for (var y = 0; y + size <= 256; y += 4)
        for (var x = 0; x + size <= 256; x += 4)
        {
            if (Animated(animated, x, y, size, size)) continue;
            double sum = 0, sq = 0, sat = 0; var n = size * size;
            for (var v = y; v < y + size; v++)
            for (var u = x; u < x + size; u++)
            {
                int r = R(u, v), g = G(u, v), b = B(u, v);
                var l = (r + g + b) / 3.0; sum += l; sq += l * l;
                sat += Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
            }
            var mean = sum / n; var spread = Math.Sqrt(Math.Max(0, sq / n - mean * mean));
            // an even, dark, colourless patch: about the Desert asphalt's own grey
            var score = spread * 2 + Math.Abs(mean - 62) + sat / n * 2.5;
            if (score < best) { best = score; asphalt = (x, y); }
        }
        (int X, int Y) white = (0, 0); var whitest = double.MinValue;
        for (var y = 1; y < 255; y++)
        for (var x = 1; x < 255; x++)
        {
            if (Animated(animated, x - 1, y - 1, 3, 3)) continue;
            double score = 0;
            for (var v = y - 1; v <= y + 1; v++)
            for (var u = x - 1; u <= x + 1; u++)
            {
                int r = R(u, v), g = G(u, v), b = B(u, v);
                score += r + g + b - 3 * (Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)));
            }
            if (score > whitest) { whitest = score; white = (x, y); }
        }
        // the cliff: the 32 x 32 tile square most blocking triangles read from
        var counts = new Dictionary<(int, int), int>();
        foreach (var cube in island.Cubes.Values)
        {
            if (!cube.HasPolygons) continue;
            var t = cube.TextureDefs;
            for (var z = 0; z < IslandCube.Cells; z++)
            for (var x = 0; x < IslandCube.Cells; x++)
                for (var half = 0; half < 2; half++)
                {
                    var p = new IslandPolygon(cube.Polygon(x, z, half));
                    if (!p.Col || p.TexFlag == 0 || p.TextureIndex * 6 + 6 > t.Length) continue;
                    int u0 = int.MaxValue, v0 = int.MaxValue;
                    for (var k = 0; k < 3; k++) { u0 = Math.Min(u0, t[p.TextureIndex * 6 + k * 2] >> 8); v0 = Math.Min(v0, t[p.TextureIndex * 6 + k * 2 + 1] >> 8); }
                    var key = (Math.Min(224, u0 / 32 * 32), Math.Min(224, v0 / 32 * 32));
                    if (Animated(animated, key.Item1, key.Item2, 32, 32)) continue;
                    counts[key] = counts.GetValueOrDefault(key) + 1;
                }
        }
        var rock = counts.Count > 0 ? counts.MaxBy(c => c.Value).Key : asphalt;
        var arrow = Near(from, to, RaceTrackTheme.Retail.Arrow);
        var theme = new RaceTrackTheme((asphalt.X, asphalt.Y, size, size), white, (0, 0, 0, 0), (rock.Item1, rock.Item2, 32, 32),
            Near(from, to, RaceTrackTheme.Retail.RedCurb), arrow, Near(from, to, RaceTrackTheme.Retail.Sand), arrow, to);
        return (theme, $"the road's look on {where.Name} ({file}): its ground texture has no spare space, so the road is painted with its own tiles -- " +
                       $"the asphalt from its darkest even patch at ({asphalt.X},{asphalt.Y}), white from the pixel at ({white.X},{white.Y}), its own cliff at ({rock.Item1},{rock.Item2}) " +
                       $"for the shoulders and walls, the hatching as the arrows' flat colour");
    }

    // The 8 x 8 blocks of the island's texture page no cube's polygon reads: every texture definition gives the (u, v) corners of a
    // triangle, 256 to a pixel. (An island with more ground pages -- POLAR.ILE: IslandFile.GroundPages -- has the definitions of all of
    // them in its cubes' lists; only those the first page's triangles read count, the page the road's are on.)
    private static bool[,] FreeBlocks(IslandFile island, HashSet<(int, int)>? repainted = null)
    {
        var used = new bool[256, 256];
        foreach (var (cx, cz, cube) in IslandOps.CubeCells(island))
        {
            var t = cube.TextureDefs;
            HashSet<int>? read = null;
            if (island.GroundPages.Count > 0 || repainted is not null)
            {
                read = new HashSet<int>();
                if (cube.HasPolygons)
                    for (var z = 0; z < IslandCube.Cells; z++)
                    for (var x = 0; x < IslandCube.Cells; x++)
                    {
                        if (repainted?.Contains((cx * IslandCube.Cells + x, cz * IslandCube.Cells + z)) == true) continue;
                        for (var half = 0; half < 2; half++)
                        {
                            var p = new IslandPolygon(cube.Polygon(x, z, half));
                            if (p.TexFlag != 0 && island.GroundTextureOf(p) is (0, var definition)) read.Add(definition * 6);
                        }
                    }
            }
            for (var i = 0; i + 5 < t.Length; i += 6)
            {
                if (read is not null && !read.Contains(i)) continue;
                int u0 = 65535, v0 = 65535, u1 = 0, v1 = 0;
                for (var k = 0; k < 3; k++)
                {
                    u0 = Math.Min(u0, t[i + k * 2]); u1 = Math.Max(u1, t[i + k * 2]);
                    v0 = Math.Min(v0, t[i + k * 2 + 1]); v1 = Math.Max(v1, t[i + k * 2 + 1]);
                }
                for (var v = v0 / 256; v <= Math.Min(255, v1 / 256); v++)
                for (var u = u0 / 256; u <= Math.Min(255, u1 / 256); u++) used[u, v] = true;
            }
        }
        // (and never a block the engine animates: one the road repaints the last of its sea or lava over is no longer read, but the
        // animation is still copied into it four times a second -- AnimatedBlocks)
        var animated = AnimatedBlocks(island);
        var free = new bool[32, 32];
        for (var by = 0; by < 32; by++)
        for (var bx = 0; bx < 32; bx++)
        {
            var any = animated[bx, by];
            for (var y = by * 8; y < by * 8 + 8 && !any; y++)
            for (var x = bx * 8; x < bx * 8 + 8 && !any; x++) any = used[x, y];
            free[bx, by] = !any;
        }
        return free;
    }

    // The 8 x 8 blocks of the island's ground page the engine draws its animated sea, lava and gas into. Each cube's INF names up to
    // four 32 x 32 squares of the page (its words 6 to 9, offsets into it), and while a triangle whose game code is an animated one is in
    // sight (CodeJeu 12 to 15: TERRAIN.CPP, "hihi je suis un polygone animé") the engine copies the next frame of its sea animation over
    // each of those squares four times a second (DoAnimatedPolys) -- whatever else reads a pixel there changes colour with it. The road's
    // own tiles must never be read from one: Celebration Island's page is full, so its road borrows its whitest pixel for the start line
    // (Borrowed), and the whitest the page had was in the lava's square -- the start line cycled lilac, beige and grey, once a quarter of
    // a second (the user, 2026-11-03: "I've noticed the start line on celebrat.ile flashing, we should make sure no start lines flash").
    // As the engine reads them: the first two offsets name a square unless they are -1 (0 is the page's corner), the last two unless
    // 0 or -1 (LOADISLE.CPP); an island with no animated triangle animates nothing (the old moon's cubes all say 0).
    public static bool[,] AnimatedBlocks(IslandFile island)
    {
        var blocks = new bool[32, 32];
        var cubes = island.Cubes.Values.Where(c => c.HasPolygons).ToList();
        var anyAnimated = cubes.Any(c => c.Polygons.Any(p => (new IslandPolygon(p).CodeJeu & 12) == 12));
        if (!anyAnimated) return blocks;
        foreach (var cube in cubes)
            for (var k = 0; k < 4; k++)
            {
                var offset = cube.Info[IslandCube.InfoOffsetAniPoly1 + k];
                if (offset < 0 || (k >= 2 && offset == 0) || offset >= 256 * 256) continue;
                // (32 rows of 32 pixels from the offset, a row 256 on: one past the page's right edge runs on into the next row's start, as
                // the engine's copy does)
                for (var row = 0; row < 32; row++)
                for (var i = 0; i < 32; i++)
                {
                    var at = offset + row * 256 + i;
                    if (at < 256 * 256) blocks[at % 256 / 8, at / 256 / 8] = true;
                }
            }
        return blocks;
    }

    // Whether a rectangle of the ground page touches a block the engine animates (AnimatedBlocks).
    private static bool Animated(bool[,] animated, int x, int y, int w, int h)
    {
        for (var by = Math.Max(0, y) / 8; by <= Math.Min(255, y + h - 1) / 8; by++)
        for (var bx = Math.Max(0, x) / 8; bx <= Math.Min(255, x + w - 1) / 8; bx++)
            if (animated[bx, by]) return true;
        return false;
    }

    // Takes a free run of blocks wide and tall enough for a tile, and marks it used.
    private static (int X, int Y)? Place(bool[,] free, int width, int height)
    {
        var bw = (width + 7) / 8; var bh = (height + 7) / 8;
        for (var by = 0; by + bh <= 32; by++)
        for (var bx = 0; bx + bw <= 32; bx++)
        {
            var all = true;
            for (var y = by; y < by + bh && all; y++)
            for (var x = bx; x < bx + bw && all; x++) all = free[x, y];
            if (!all) continue;
            for (var y = by; y < by + bh; y++)
            for (var x = bx; x < bx + bw; x++) free[x, y] = false;
            return (bx * 8, by * 8);
        }
        return null;
    }
}
