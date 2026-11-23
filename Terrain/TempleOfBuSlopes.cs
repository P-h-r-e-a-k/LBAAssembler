using LBAAssembler.Assets;
using LBAAssembler.Grids;
using LBAAssembler.Maps;

namespace LBAAssembler.Terrain;

// Smooth stone slopes for the Temple of Bù's minecart inclines (the user, 2026-11-23: "Please remove the rails from this scene 11 ... any
// slopes that have no terrain under them we should add a slope that'd driveable. We also need to add a slope to the doorway into the esmer
// base"). The game's inclines are rails and sleepers on a trestle, nothing under them, and the temple's library has a stone ramp for one
// way only (block 195). So new ones, drawn from the halls' own floor tile (block 8, a 3 x 3 pattern of top-only bricks): a stone cube, and
// a stone slope rising each of the four ways -- each a 3 x 3 block in the tile's pattern, so a ramp is the same stone as the floor it
// leaves, carried on up or down. A brick is the cell as the grid draws it (48 x 38, the top's diamond and the two near sides): the top
// textured with the tile's pixel at the same place on its top, the sides with the tile's darkened, the slopes' tops a shade lighter or
// darker by the way they face.
internal static class TempleOfBuSlopes
{
    // the collision shapes: 1 a full cube; slopes 2 rising towards +x, 3 towards +z, 4 towards -z, 5 towards -x (the game's inclines:
    // 64 is 4, 65 is 5, 66 is 3, 67 is 2)
    public static readonly int[] Shapes = { 1, 2, 3, 4, 5 };
    private const int Width = 48, Height = 38;

    private static double Top(int shape, double u, double w) => shape switch { 2 => u, 3 => w, 4 => 1 - w, 5 => 1 - u, _ => 1 };

    // a palette index a few shades along its ramp (16 to a ramp, brighter up it)
    private static byte Shade(byte c, int by) => (byte)((c & 0xF0) | Math.Clamp((c & 0x0F) + by, 0, 15));

    public static GphImage Draw(GphImage tile, int shape)
    {
        var image = new GphImage(Width, Height);
        var depth = new double[Width * Height];
        Array.Fill(depth, double.NegativeInfinity);
        // the tile's pixel at the top's (u, w) -- its own diamond, 48 x 23 -- the nearest drawn one
        byte Texture(double u, double w)
        {
            int x = (int)Math.Round(24 + 24 * (u - w)), y = (int)Math.Round(11 * (u + w));
            for (var r = 0; r < 6; r++)
                for (var dy = -r; dy <= r; dy++)
                    for (var dx = -r; dx <= r; dx++)
                    {
                        int px = Math.Clamp(x + dx, 0, tile.Width - 1), py = Math.Clamp(y + dy, 0, tile.Height - 1);
                        if (tile.Opaque[py * tile.Width + px]) return tile.Pixels[py * tile.Width + px];
                    }
            return tile.Pixels[tile.Pixels.Length / 2];
        }
        void Plot(double u, double v, double w, byte c)
        {
            int x = (int)Math.Round(24 + 24 * (u - w) - 0.5), y = (int)Math.Round(11.5 * (u + w) + 14.5 * (1 - v) - 0.5);
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            var d = u + w + 1.5 * v;
            if (d < depth[y * Width + x]) return;
            depth[y * Width + x] = d;
            image.Set(x, y, c);
        }
        // (the top's shade by the way it faces: towards the viewer's left or right a shade darker, away from him a shade lighter)
        var topShade = shape switch { 4 or 5 => -1, 2 or 3 => 1, _ => 0 };
        const int N = 192;
        for (var a = 0; a <= N; a++)
            for (var b = 0; b <= N; b++)
            {
                double s = a / (double)N, t = b / (double)N;
                Plot(s, Top(shape, s, t), t, Shade(Texture(s, t), topShade));
                // the near sides, up to the top's height at them: +x (the right) and +z (the left), the tile's stone darkened
                if (t <= Top(shape, 1, s)) Plot(1, t, s, Shade(Texture(1 - 0.6 * t, s), -2));
                if (t <= Top(shape, s, 1)) Plot(s, t, 1, Shade(Texture(s, 1 - 0.6 * t), -4));
            }
        return image;
    }

    // The five blocks added to the temple's library (after its last), each the tile's dimensions and its nine places' bricks drawn anew:
    // their numbers by shape (1 the cube)
    public static Dictionary<int, int> AddBlocks(HqrFile bkg, MapBricks.BkgHeader header, ref byte[] library, int tileBlock, Func<int, byte[]?> brick)
    {
        var info = GridPaint.Info(library, tileBlock)!.Value;
        var entries = GridPaint.Entries(library, tileBlock).ToList();
        var tiles = entries.ToDictionary(e => e.Pos, e => brick(e.Brick) is { Length: >= 4 } data ? GphImage.Decode(data) : null);
        var numbers = new Dictionary<int, int>();
        foreach (var shape in Shapes)
        {
            var number = GridPaint.BlockCount(library) + 1;
            library = GridPaint.AppendBlock(library, info.Dx, info.Dy, info.Dz, entries[0].Brick, shape, entries[0].Sound);
            foreach (var e in entries)
            {
                var b = tiles[e.Pos] is { } tile ? Citadel.CitadelMuseum.AddBrickPublic(bkg, header, Citadel.CitadelMuseum.EncodeBrick(Draw(tile, shape))) : e.Brick;
                library = GridPaint.SetEntry(library, number, e.Pos, b, shape, e.Sound);
            }
            numbers[shape] = number;
        }
        return numbers;
    }
}
