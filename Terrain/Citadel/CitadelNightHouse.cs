using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain.Citadel;

// Twinsen's house at night, for going to bed at the end of chapter 1 (the user, 2026-11-06: "A NEW night-time house scene for going to
// bed: a copy of Twinsen's house, dark except for the light of the candle that already stands by the bed"). The build's side is here, the
// story's (who is there, the bed, the candle blown out and the morning) in RaceTrackStory.Evening.cs:
//   - the house's own grid (scene 0's), in bricks of its own: every brick the house shows copied and darkened -- each pixel the palette's
//     colour nearest its own at a night's share of its light, a little blue -- and round the candle on the bedside table (cell 12, 1, its
//     flame at layer 12) two rings lit warmer: the bedside table, the bed's head and the wall behind them in the candle's light, the floor
//     and the room's middle in its glow. Each ring's cells name copies of their blocks in those bricks; the candle's own bricks keep their
//     colours. A block library of its own, a grid after the game's, LBA_BKG.HQR's scene-to-grid table pointing the new scene at it (as the
//     LBA1 rooms go in: CitadelLba1Rooms);
//   - the scene (Scene, 193: under 255, so change_cube() can name it and the holomap and saved games hold it -- and every slot from 222 to
//     254 is a build's: the story's holomap arrows 222-223, the lava lake 223, Sendell's Well 224, the old moon 225-228, the LBA1 rooms
//     226-227, the Emerald Moon 229-232, Polar Island 233-254. 193 is the retail game's copy of Twinsen's house for its demo, which no
//     scene of the game itself leads to and no build uses; claimed by anything else first -- not the game's own record any more when this
//     runs -- the build stops with an error): scene 0's header -- its light, music, ambience --
//     Twinsen arriving by the front door, the Meca-penguin's slot, the front door back out into the yard (scene 49) and the house's
//     cameras; no attic door (the night is for bed), no search zones (the day's house has them);
//   - the holomap: the scene where the house is (scene 0's record copied), for "Twinsen is here".
internal static class CitadelNightHouse
{
    public const int Scene = 193, House = 0, Yard = 49;
    // where the front door from the yard brings Twinsen in (the yard's door zone into scene 0: its arrival)
    public static readonly (int X, int Y, int Z) Arrival = (12800, 768, 9728);
    // the candle on the bedside table (cell x 12, z 1; its bricks at layers 11-12) and its light's reach (cells, a layer half a cell)
    public const int CandleX = 12, CandleY = 12, CandleZ = 1;
    private const double LitReach = 2.6, GlowReach = 5.2;
    // each level's share of a colour's light (red, green, blue): the night's, the candle's glow, its light
    private static readonly (double R, double G, double B) Night = (0.27, 0.30, 0.44), Glow = (0.55, 0.45, 0.34), Lit = (0.88, 0.74, 0.55);

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        // (the slot as the game has it: the demo's house, or nothing -- anything else is another piece of the build in it)
        var original = RaceTrackService.OriginalScenes(new SceneStore(SceneGame.Lba2, gameDirectory));
        var now = Scene + 1 < scenes.Count && !scenes.IsEmpty(Scene + 1) ? scenes.Read(Scene + 1) : null;
        var was = original.SceneExists(Scene) ? original.LoadRecord(Scene) : null;
        if (now is not null && (was is null || !now.AsSpan().SequenceEqual(was)))
            throw new InvalidDataException($"Twinsen's house at night: scene {Scene} is already another part of the build's (Citadel/CitadelNightHouse needs it to itself)");
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(House)) { log.Add("Twinsen's house at night left out: no scene 0"); return log; }

        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (House * 2 + 1 >= table.Length) { log.Add("Twinsen's house at night left out: scene 0 has no grid"); return log; }
        var type = table[House * 2];
        var raw = bkg.Read(header.Gri + table[House * 2 + 1]);
        var library = bkg.Read(header.Bll + raw[0]);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * 64 + x) * 25 + y) * 2;

        // the library's blocks (each its own bytes: blocks may share theirs in the game's)
        var count = (int)(BinaryPrimitives.ReadUInt32LittleEndian(library) / 4);
        var blocks = new List<byte[]>();
        for (var b = 0; b < count; b++)
        {
            var at = (int)BinaryPrimitives.ReadUInt32LittleEndian(library.AsSpan(b * 4));
            if (at + 3 > library.Length) { blocks.Add(new byte[] { 0, 0, 0 }); continue; }
            var n = library[at] * library[at + 1] * library[at + 2];
            blocks.Add(library.AsSpan(at, Math.Min(3 + n * 4, library.Length - at)).ToArray());
        }

        // each filled cell's light: 2 lit, 1 the glow, 0 the night
        var candle = cells[At(CandleX, CandleY, CandleZ)];
        var level = new int[64, 25, 64];
        var used = new HashSet<int>();
        for (var z = 0; z < 64; z++)
            for (var x = 0; x < 64; x++)
                for (var y = 0; y < 25; y++)
                {
                    var block = cells[At(x, y, z)];
                    if (block == 0) continue;
                    used.Add(block);
                    if (candle == 0) continue;
                    double dx = x - CandleX, dz = z - CandleZ, dy = (y - CandleY) * 0.5;
                    var d = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    level[x, y, z] = d <= LitReach ? 2 : d <= GlowReach ? 1 : 0;
                }
        if (candle == 0) log.Add($"Twinsen's house at night: no candle at cell ({CandleX}, {CandleY}, {CandleZ}) -- the house all night-dark");
        var candleBricks = new HashSet<int>();
        if (candle != 0 && candle - 1 < blocks.Count)
            foreach (var brick in Bricks(blocks[candle - 1])) candleBricks.Add(brick);

        // the darkened bricks, made as they are first needed (added after the file's bricks)
        var maps = new[] { Shade(palette, Night), Shade(palette, Glow), Shade(palette, Lit) };
        var made = new Dictionary<(int Brick, int Level), int>();
        var added = new List<byte[]>();
        int Copy(int brick, int lv)
        {
            if (lv == 2 && candleBricks.Contains(brick)) return brick;
            if (made.TryGetValue((brick, lv), out var have)) return have;
            if (brick < 0 || brick >= header.MaxBrk || bkg.IsEmpty(header.Brk + brick)) return made[(brick, lv)] = brick;
            var number = header.MaxBrk + added.Count;
            added.Add(Lba1InteriorPort.Recolour(bkg.Read(header.Brk + brick), maps[lv]));
            return made[(brick, lv)] = number;
        }
        byte[] Variant(byte[] block, int lv)
        {
            var copy = (byte[])block.Clone();
            var n = copy[0] * copy[1] * copy[2];
            for (var c = 0; c < n && 3 + c * 4 + 4 <= copy.Length; c++)
            {
                var b = BinaryPrimitives.ReadUInt16LittleEndian(copy.AsSpan(3 + c * 4 + 2)) - 1;
                if (b >= 0) BinaryPrimitives.WriteUInt16LittleEndian(copy.AsSpan(3 + c * 4 + 2), (ushort)(Copy(b, lv) + 1));
            }
            return copy;
        }
        // (every block the house uses at night; the lit and glowing cells' blocks copied for their light -- the glow left out when the
        // library would hold more than a grid can name)
        var litBlocks = new SortedSet<int>(); var glowBlocks = new SortedSet<int>();
        for (var z = 0; z < 64; z++)
            for (var x = 0; x < 64; x++)
                for (var y = 0; y < 25; y++)
                {
                    var block = cells[At(x, y, z)];
                    if (block == 0) continue;
                    if (level[x, y, z] == 2) litBlocks.Add(block); else if (level[x, y, z] == 1) glowBlocks.Add(block);
                }
        var glow = count + litBlocks.Count + glowBlocks.Count <= 255;
        if (!glow && count + litBlocks.Count > 255) litBlocks.Clear();
        foreach (var block in used) if (block - 1 < blocks.Count) blocks[block - 1] = Variant(blocks[block - 1], 0);
        var variant = new Dictionary<(int Block, int Level), int>();
        foreach (var block in litBlocks) { variant[(block, 2)] = blocks.Count + 1; blocks.Add(Variant(OwnBlock(library, block), 2)); }
        if (glow) foreach (var block in glowBlocks) { variant[(block, 1)] = blocks.Count + 1; blocks.Add(Variant(OwnBlock(library, block), 1)); }
        var edits = new List<Lba1GridCell>();
        for (var z = 0; z < 64; z++)
            for (var x = 0; x < 64; x++)
                for (var y = 0; y < 25; y++)
                {
                    var block = cells[At(x, y, z)];
                    if (block != 0 && variant.TryGetValue((block, level[x, y, z]), out var v)) edits.Add(new Lba1GridCell(x, y, z, v, cells[At(x, y, z) + 1]));
                }

        // into LBA_BKG.HQR: the bricks after the file's, the library after its libraries, the grid after its grids (each section moving
        // the ones after it), the scene-to-grid table's entry for the new scene
        var brickAt = header.Brk + header.MaxBrk;
        foreach (var brick in added) bkg.InsertAt(brickAt++, brick);
        header.MaxBrk += added.Count;
        if (header.MaxBrk > MapBricks.RaceEngineMaxBricks) return Daylit("LBA_BKG.HQR would hold more bricks than the engine does");
        var newLibrary = Library(blocks);
        var libraryNumber = header.Brk - header.Bll;
        if (libraryNumber > 255) return Daylit("LBA_BKG.HQR has 256 block libraries already");
        bkg.InsertAt(header.Brk, newLibrary);
        header.Brk += 1;
        if (header.MaxBll < newLibrary.Length) header.MaxBll = (uint)newLibrary.Length;
        var grid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, edits));
        grid[0] = (byte)libraryNumber;
        var gridNumber = header.Grm - header.Gri;
        if (gridNumber > 255) return Daylit("LBA_BKG.HQR has 256 grids already");
        bkg.InsertAt(header.Grm, grid);
        header.Grm += 1; header.Bll += 1; header.Brk += 1;
        if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
        // (the engine's buffers for a cube's bricks and their masks: this grid's too -- MapBricks.Install)
        var (bytes, usedBricks) = MapBricks.UsedBrickBytes(grid, newLibrary, b => b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? BinaryPrimitives.ReadUInt32LittleEndian(bkg.ExtentOf(header.Brk + b)) : 0);
        if (header.MaxBrickCube < (uint)((usedBricks + 1) * 4 + bytes)) header.MaxBrickCube = (uint)((usedBricks + 1) * 4 + bytes);
        if (header.MaxMaskCube < (uint)((usedBricks + 1) * 4 + bytes + usedBricks * 64)) header.MaxMaskCube = (uint)((usedBricks + 1) * 4 + bytes + usedBricks * 64);
        bkg.SetStored(0, header.Bytes);
        table = bkg.Read(header.Brk + header.MaxBrk);
        if (table.Length < (Scene + 1) * 2) Array.Resize(ref table, (Scene + 1) * 2);
        table[Scene * 2] = type; table[Scene * 2 + 1] = (byte)gridNumber;
        bkg.SetStored(header.Brk + header.MaxBrk, table);
        Finish(bkg);
        log.Add($"Twinsen's house at night: scene {Scene}, scene 0's grid in {added.Count} darkened bricks (grid {gridNumber}, block library {libraryNumber}), " +
                $"{litBlocks.Count} blocks in the candle's light and {(glow ? glowBlocks.Count : 0)} in its glow, its front door back to scene {Yard}");
        return log;

        // (the night's bricks don't fit -- a build of every island holds all the bricks the engine takes: the scene is made all the same,
        // on the day's house's own grid, so the front door still brings Twinsen in by the door, Zoe beside him, and not into the demo's
        // copy of the house, whose start is in the bedroom -- 2026-11-07, the scripted playthrough)
        List<string> Daylit(string why)
        {
            var day = HqrFile.Parse(File.ReadAllBytes(bkgPath));
            var dayHeader = new MapBricks.BkgHeader(day.Read(0));
            var dayTable = day.Read(dayHeader.Brk + dayHeader.MaxBrk);
            if (dayTable.Length < (Scene + 1) * 2) Array.Resize(ref dayTable, (Scene + 1) * 2);
            dayTable[Scene * 2] = dayTable[House * 2]; dayTable[Scene * 2 + 1] = dayTable[House * 2 + 1];
            day.SetStored(dayHeader.Brk + dayHeader.MaxBrk, dayTable);
            Finish(day);
            log.Add($"Twinsen's house at night not darkened ({why}): scene {Scene} on the day's house's grid, its front door back to scene {Yard}");
            return log;
        }

        void Finish(HqrFile bkgFile)
        {
        // the scene: the house's header, Twinsen by the front door, the penguin's slot, the front door out and the cameras
        var model = store.Load(House).Clone();
        model.Actors.RemoveRange(2, model.Actors.Count - 2);
        model.Zones.RemoveAll(zn => !(zn.Type == 0 && zn.Num == Yard) && zn.Type is not (1 or 5 or 6));
        var hero = model.Hero;
        (hero.X, hero.Y, hero.Z) = Arrival;
        hero.Life = new byte[] { 0 }; hero.Track = new byte[] { 0 };
        var entry = Scene + 1;
        while (scenes.Count <= entry) scenes.Slots.Add(new HqrFile.Slot());
        var record = SceneSerializer.Write(model);
        scenes.SetStored(entry, record);
        if (record.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
        { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, record.Length); scenes.SetStored(0, size); }

        // the holomap: where the house is
        var holoPath = Path.Combine(gameDirectory, RaceTrackHolomap.File);
        var holo = File.ReadAllBytes(holoPath);
        var arrows = HqrArchive.Open(holoPath).Read(12);
        const int RecordSize = 32;
        if (arrows.Length >= (50 + Scene + 1) * RecordSize)
        {
            Array.Copy(arrows, (50 + House) * RecordSize, arrows, (50 + Scene) * RecordSize, RecordSize);
            holo = HqrWriter.ReplaceEntry(holo, 12, HqrWriter.StoredEntry(arrows));
        }
        else log.Add($"Twinsen's house at night: the holomap's table has no place {Scene} (Twinsen not shown there)");
        new FileTransaction().Write(bkgPath, bkgFile.ToBytes()).Write(scenePath, scenes.ToBytes()).Write(holoPath, holo).Commit();
        }
    }

    // A block's bytes in the game's library, as it was (its bricks the game's)
    private static byte[] OwnBlock(byte[] library, int block)
    {
        var at = (int)BinaryPrimitives.ReadUInt32LittleEndian(library.AsSpan((block - 1) * 4));
        var n = library[at] * library[at + 1] * library[at + 2];
        return library.AsSpan(at, Math.Min(3 + n * 4, library.Length - at)).ToArray();
    }

    private static IEnumerable<int> Bricks(byte[] block)
    {
        var n = block[0] * block[1] * block[2];
        for (var c = 0; c < n && 3 + c * 4 + 4 <= block.Length; c++)
        {
            var b = BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(3 + c * 4 + 2)) - 1;
            if (b >= 0) yield return b;
        }
    }

    // A library from its blocks: the offsets' table, then each block
    private static byte[] Library(List<byte[]> blocks)
    {
        var size = blocks.Count * 4 + blocks.Sum(b => b.Length);
        var bytes = new byte[size];
        var at = blocks.Count * 4;
        for (var b = 0; b < blocks.Count; b++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(b * 4), (uint)at);
            blocks[b].CopyTo(bytes, at);
            at += blocks[b].Length;
        }
        return bytes;
    }

    // Each palette colour's nearest at `share` of its light (the palette's own colours: not the interface's 1-15, nor its vivid ones from
    // 248), by the eye's weighting ("redmean", as Lba1InteriorPort's)
    private static byte[] Shade(byte[] palette, (double R, double G, double B) share)
    {
        var scale = palette.Take(768).Max() < 64 ? 4 : 1;
        (double R, double G, double B) Rgb(int i) => (palette[i * 3] * scale, palette[i * 3 + 1] * scale, palette[i * 3 + 2] * scale);
        var map = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            var c = Rgb(i);
            var want = (R: c.R * share.R, G: c.G * share.G, B: c.B * share.B);
            var best = 0; var bestD = double.MaxValue;
            for (var j = 0; j < 248; j++)
            {
                if (j is >= 1 and <= 15) continue;
                var o = Rgb(j);
                var rm = (want.R + o.R) / 2;
                double dr = want.R - o.R, dg = want.G - o.G, db = want.B - o.B;
                var d = (2 + rm / 256) * dr * dr + 4 * dg * dg + (2 + (255 - rm) / 256) * db * db;
                if (d < bestD) { bestD = d; best = j; }
            }
            map[i] = (byte)best;
        }
        return map;
    }
}
