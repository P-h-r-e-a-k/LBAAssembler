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
    (int Bank, int Pos) Sand)
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
        var file = ileFile ?? where.IleFile;
        if (file == RaceTrackIsland.Desert.IleFile) return (RaceTrackTheme.Retail, "");

        var desertPath = Path.Combine(gameDirectory, RaceTrackIsland.Desert.IleFile);
        var desert = IslandFile.Load(File.Exists(desertPath) ? desertPath : Path.Combine(gameDirectory, RaceTrackIsland.Desert.IleFile + RaceTrackService.BackupSuffix));
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

        (int Bank, int Pos) Near((int Bank, int Pos) colour)
        {
            var i = colour.Bank * 16 + colour.Pos;
            int r = from[i * 3], g = from[i * 3 + 1], b = from[i * 3 + 2];
            var best = map[i];
            // (the ramp the colour sits in matters as much as the colour: the engine adds the light to it, so keep its position in the bank)
            var bd = int.MaxValue;
            for (var bank = 0; bank < 16; bank++)
            {
                var j = bank * 16 + colour.Pos;
                var d = (to[j * 3] - r) * (to[j * 3] - r) + (to[j * 3 + 1] - g) * (to[j * 3 + 1] - g) + (to[j * 3 + 2] - b) * (to[j * 3 + 2] - b);
                if (d < bd) { bd = d; best = (byte)j; }
            }
            return (best / 16, best % 16);
        }

        var theme = new RaceTrackTheme(asphalt, (white.X + 3, white.Y + 3), hatch, rock,
            Near(RaceTrackTheme.Retail.RedCurb), Near(RaceTrackTheme.Retail.Arrow), Near(RaceTrackTheme.Retail.Sand));
        var log = $"the road's look on {where.Name} ({file}): the Desert track's tiles copied into its spare texture space ({string.Join(", ", placed)}), " +
                  $"their colours matched in its own palette; the red curb is colour {theme.RedCurb.Bank * 16 + theme.RedCurb.Pos}, the arrows {theme.Arrow.Bank * 16 + theme.Arrow.Pos}";
        return (theme, log);
    }

    // The 8 x 8 blocks of the island's texture page no cube's polygon reads: every texture definition gives the (u, v) corners of a
    // triangle, 256 to a pixel.
    private static bool[,] FreeBlocks(IslandFile island)
    {
        var used = new bool[256, 256];
        foreach (var cube in island.Cubes.Values)
        {
            var t = cube.TextureDefs;
            for (var i = 0; i + 5 < t.Length; i += 6)
            {
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
        var free = new bool[32, 32];
        for (var by = 0; by < 32; by++)
        for (var bx = 0; bx < 32; bx++)
        {
            var any = false;
            for (var y = by * 8; y < by * 8 + 8 && !any; y++)
            for (var x = bx * 8; x < bx * 8 + 8 && !any; x++) any = used[x, y];
            free[bx, by] = !any;
        }
        return free;
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
