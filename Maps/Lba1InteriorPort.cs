using System.Buffers.Binary;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Scenes;

namespace LBAAssembler.Maps;

// An LBA1 interior as an LBA2 one: an LBA1 scene's grid, block library and bricks made an LBA2 interior map with a brick package of its own
// (LbaMap: Lba2Interior, OwnBricks), for MapBricks.Install to put into a game folder's LBA_BKG.HQR -- the way Citadel Island's easter eggs
// bring back LBA1's rooms (Terrain/Citadel/CitadelLba1Rooms.cs). The two games' interiors are the same layout (Lba2Interiors: grids of 64 x
// 25 x 64 cells, block libraries of 4-byte cells -- shape, sound, brick + 1 -- and RLE bricks 48 pixels wide); what differs:
//   - the colours: an interior is drawn in LBA2's one palette (the race-track mode's island_palette= is for islands only), so each LBA1
//     brick is recoloured, every pixel the LBA2 colour nearest LBA1's (a perceptual distance: "redmean");
//   - the cells' sound byte: LBA1 keeps two step sounds in it, one in each half (0x33, 0xAA; 0xF0 none), LBA2 its floor's step sound in the
//     low half and its game code in the high half (COMMON.H CJ_*: 1 water, Twinsen drowns; 2 electrified; 8 a place he dies) -- so the high
//     half goes, and LBA1's "none" is LBA2's 0;
//   - the shapes: LBA1's 1-13 (solid, stairs, the corner stairs) are LBA2's; anything else is made solid;
//   - the grid: LBA2's entry is LBA1's with a 34-byte header in front (library, fragments, the used blocks' bitmap LBA1 keeps at its end).
// The empty cells' second byte (a cell's collision code: LBA1's invisible walls) is kept as it is: LBA2 reads it the same way.
internal static class Lba1InteriorPort
{
    public static LbaMap Port(string lba1Directory, int scene, byte[] lba2Palette, string name, Func<byte[], byte[]>? editGrid = null)
    {
        var game = new Lba1Game(lba1Directory);
        var grid = game.ReadGrid(scene);
        if (editGrid is not null) grid = editGrid(grid);
        var library = (byte[])game.ReadBlocks(scene).Clone();
        var nearest = NearestColours(game.Palette, lba2Palette);

        // the bricks the library names, numbered from 0 in the order the library first names them
        var positions = MapBricks.BrickPositions(library);
        var local = new Dictionary<int, int>();
        var bricks = new HqrFile();
        foreach (var p in positions)
        {
            var b = BinaryPrimitives.ReadUInt16LittleEndian(library.AsSpan(p)) - 1;
            if (b < 0) continue;
            if (!local.TryGetValue(b, out var k))
            {
                var picture = game.ReadBrick(b);
                k = local[b] = bricks.Count;
                bricks.Slots.Add(new HqrFile.Slot { Extent = HqrWriter.StoredEntry(picture is null ? Array.Empty<byte>() : Recolour(picture, nearest)) });
            }
            BinaryPrimitives.WriteUInt16LittleEndian(library.AsSpan(p), (ushort)(k + 1));
            // (the cell's shape and sound: the bytes before its brick)
            if (library[p - 2] > 13) library[p - 2] = 1;
            library[p - 1] = (byte)(library[p - 1] == 0xF0 ? 0 : library[p - 1] & 0x0F);
        }
        var entry = Lba2GridBackend.FromLba1Shape(new byte[2], grid);
        return new LbaMap { Kind = MapKind.Lba2Interior, Name = name, IslandByte = 0, Grid = entry, Library = library, Bricks = bricks.ToBytes() };
    }

    // Each LBA1 colour's nearest LBA2 colour (palettes of 256 RGB triples, 0-255 or the VGA's 0-63)
    public static byte[] NearestColours(byte[] from, byte[] to)
    {
        static (double R, double G, double B) Rgb(byte[] p, int i, int scale) => (p[i * 3] * scale, p[i * 3 + 1] * scale, p[i * 3 + 2] * scale);
        static int Scale(byte[] p) => p.Take(768).Max() < 64 ? 4 : 1;
        int sf = Scale(from), st = Scale(to);
        var map = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            var a = Rgb(from, i, sf);
            var best = 0; var bestD = double.MaxValue;
            for (var j = 0; j < 256; j++)
            {
                var b = Rgb(to, j, st);
                var rm = (a.R + b.R) / 2;
                double dr = a.R - b.R, dg = a.G - b.G, db = a.B - b.B;
                var d = (2 + rm / 256) * dr * dr + 4 * dg * dg + (2 + (255 - rm) / 256) * db * db;
                if (d < bestD) { bestD = d; best = j; }
            }
            map[i] = (byte)best;
        }
        return map;
    }

    // A brick (or sprite picture) with its colours mapped: per line a run count, then runs -- skip, copy (`count` colours), fill (one colour)
    public static byte[] Recolour(byte[] brick, byte[] map)
    {
        var data = (byte[])brick.Clone();
        if (data.Length < 4) return data;
        int lines = data[1], src = 4;
        for (var line = 0; line < lines && src < data.Length; line++)
        {
            int runs = data[src++];
            for (var r = 0; r < runs && src < data.Length; r++)
            {
                var control = data[src++];
                var count = (control & 0x3F) + 1;
                switch (control >> 6)
                {
                    case 0: break;
                    case 1:
                        for (var k = 0; k < count && src < data.Length; k++, src++) data[src] = map[data[src]];
                        break;
                    default:
                        if (src < data.Length) { data[src] = map[data[src]]; src++; }
                        break;
                }
            }
        }
        return data;
    }
}
