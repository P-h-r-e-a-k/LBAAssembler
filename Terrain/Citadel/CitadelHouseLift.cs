using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;

namespace LBAAssembler.Terrain.Citadel;

// The lift in Twinsen's house down to the secret chamber (the user, 2026-11-07: "A hidden area in Twinsen's house accessed by an elevator,
// perhaps with 4 slightly darker squares that takes Twinsen and Zoe down into a secret area" -- the four squares marked on a screenshot):
// the storeroom up the half flight from the bedroom (scene 0's grid: its floor at layer 9, cells x 5-14, z 13-19, the stepladder, the
// bucket and the rolled-up rugs), the two by two of its floor between the stepladder and the low wall -- cells x 8-9, z 16-17 -- each
// a copy of its block with its bricks a shade darker. The house at night is made from this grid after it (CitadelNightHouse), so its
// squares are darker too. The story's side -- the lift itself, the chamber and who goes down it -- is RaceTrackStory.SecretChamber.
internal static class CitadelHouseLift
{
    public const int House = 0, FloorLayer = 9, X0 = 8, X1 = 9, Z0 = 16, Z1 = 17;
    // the squares' middle and its floor (the top of layer 9)
    public static readonly (int X, int Y, int Z) Middle = ((X0 + 1) * 512, (FloorLayer + 1) * 256, (Z0 + 1) * 512);
    private const double Shade = 0.8;

    public static string Install(string gameDirectory)
    {
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (House * 2 + 1 >= table.Length) return "the house's lift squares left out: scene 0 has no grid";
        var gridEntry = header.Gri + table[House * 2 + 1];
        var raw = bkg.Read(gridEntry);
        var libraryEntry = header.Bll + raw[0];
        var library = bkg.Read(libraryEntry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * 64 + x) * 25 + y) * 2;
        var count = (int)(BinaryPrimitives.ReadUInt32LittleEndian(library) / 4);
        // (a second pass: the squares' blocks are past the game's own already)
        var squares = new List<(int X, int Z, int Block, int Pos)>();
        for (var z = Z0; z <= Z1; z++)
            for (var x = X0; x <= X1; x++)
                squares.Add((x, z, cells[At(x, FloorLayer, z)], cells[At(x, FloorLayer, z) + 1]));
        if (squares.Any(s => s.Block == 0)) return "the house's lift squares left out: scene 0's storeroom floor isn't where it was";

        // each distinct block a darker copy, its bricks darkened (added after the file's bricks)
        var map = Darker(palette);
        var blocks = new List<byte[]>();
        for (var b = 0; b < count; b++)
        {
            var at = (int)BinaryPrimitives.ReadUInt32LittleEndian(library.AsSpan(b * 4));
            var end = b + 1 < count ? (int)BinaryPrimitives.ReadUInt32LittleEndian(library.AsSpan((b + 1) * 4)) : library.Length;
            blocks.Add(library.AsSpan(at, end - at).ToArray());
        }
        if (count + squares.Select(s => s.Block).Distinct().Count() > 255) return "the house's lift squares left out: its block library is full";
        var darkBrick = new Dictionary<int, int>();
        var newBlock = new Dictionary<int, int>();
        foreach (var block in squares.Select(s => s.Block).Distinct())
        {
            var copy = (byte[])blocks[block - 1].Clone();
            var n = copy[0] * copy[1] * copy[2];
            for (var c = 0; c < n && 3 + c * 4 + 4 <= copy.Length; c++)
            {
                var brick = BinaryPrimitives.ReadUInt16LittleEndian(copy.AsSpan(3 + c * 4 + 2)) - 1;
                if (brick < 0 || brick >= header.MaxBrk || bkg.IsEmpty(header.Brk + brick)) continue;
                if (!darkBrick.TryGetValue(brick, out var dark))
                {
                    dark = header.MaxBrk;
                    bkg.InsertAt(header.Brk + header.MaxBrk, Lba1InteriorPort.Recolour(bkg.Read(header.Brk + brick), map));
                    header.MaxBrk += 1;
                    darkBrick[brick] = dark;
                }
                BinaryPrimitives.WriteUInt16LittleEndian(copy.AsSpan(3 + c * 4 + 2), (ushort)(dark + 1));
            }
            blocks.Add(copy);
            newBlock[block] = blocks.Count;
        }
        if (header.MaxBrk > MapBricks.RaceEngineMaxBricks) return "the house's lift squares left out: LBA_BKG.HQR would hold more bricks than the engine does";
        // the library with the copies at its end, in its place; the grid's squares naming them
        var size = blocks.Count * 4 + blocks.Sum(b => b.Length);
        var bytes = new byte[size];
        var pos = blocks.Count * 4;
        for (var b = 0; b < blocks.Count; b++) { BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(b * 4), (uint)pos); blocks[b].CopyTo(bytes, pos); pos += blocks[b].Length; }
        bkg.SetStored(libraryEntry, bytes);
        if (header.MaxBll < bytes.Length) header.MaxBll = (uint)bytes.Length;
        var edits = squares.Select(s => new Lba1GridCell(s.X, FloorLayer, s.Z, newBlock[s.Block], s.Pos)).ToList();
        var grid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, edits));
        bkg.SetStored(gridEntry, grid);
        if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
        bkg.SetStored(0, header.Bytes);
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Commit();
        return $"the house's lift squares: scene 0's storeroom floor, cells x {X0}-{X1}, z {Z0}-{Z1}, a shade darker ({newBlock.Count} blocks, {darkBrick.Count} bricks)";
    }

    // Each palette colour's nearest at Shade of its light (the palette's own, not the interface's 1-15)
    private static byte[] Darker(byte[] palette)
    {
        var scale = palette.Take(768).Max() < 64 ? 4 : 1;
        var map = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            double r = palette[i * 3] * scale * Shade, g = palette[i * 3 + 1] * scale * Shade, b = palette[i * 3 + 2] * scale * Shade;
            var best = 0; var bestD = double.MaxValue;
            for (var j = 0; j < 248; j++)
            {
                if (j is >= 1 and <= 15) continue;
                double dr = r - palette[j * 3] * scale, dg = g - palette[j * 3 + 1] * scale, db = b - palette[j * 3 + 2] * scale;
                var d = 2 * dr * dr + 4 * dg * dg + 3 * db * db;
                if (d < bestD) { bestD = d; best = j; }
            }
            map[i] = (byte)best;
        }
        return map;
    }
}
