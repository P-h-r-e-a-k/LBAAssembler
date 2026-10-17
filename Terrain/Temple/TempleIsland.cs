using System.IO;
using LBAAssembler.Lba1;
using LBAAssembler.Terrain.Palace;
using LBAAssembler.Terrain.Polar;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Temple;

// Where the temple's layout lies on the island: a layout cell is Scale x Scale of the island's cells, a layer Scale layers -- everything
// Scale times as big each way, so that from LBA1's angle it looks as LBA1 draws it (three times: the temple's corridors, five to seven
// cells wide, are 15 to 21 for the race's cars, the Emperor's palace's doorways; and its 34 layers, 26,112 high, stay under the 32,767 the
// engine's heights hold). The layout's (0, 0) is island cell (OffsetX, OffsetZ), its lowest layer's foot BaseY over the island's zero.
internal sealed class TempleFrame
{
    public const int Scale = 3;
    // the island's cubes (cubes 2..12 x 3..10: the temple and a cube round it), and the layout's place in them
    public const int CubeX0 = 2, CubeZ0 = 3, CubesX = 11, CubesZ = 8;
    public const int OffsetX = 224, OffsetZ = 256;
    public const int BaseY = 1024;
    public readonly int MinY;
    public TempleFrame(int minY) => MinY = minY;

    // a layout cell's first island cell; the world units of a layout point (cells, layers)
    public int IslandX(int x) => OffsetX + x * Scale;
    public int IslandZ(int z) => OffsetZ + z * Scale;
    public double WorldX(double x) => (OffsetX + x * Scale) * 512.0;
    public double WorldZ(double z) => (OffsetZ + z * Scale) * 512.0;
    public double WorldY(double layer) => BaseY + (layer - MinY) * 256.0 * Scale;
    // the layout's x (or z) of an island cell, and the first layout column whose cells start in the cube of x's
    public static double LayoutX(double islandCell) => (islandCell - OffsetX) / Scale;
    public static double LayoutZ(double islandCell) => (islandCell - OffsetZ) / Scale;
    public int FirstColumnOfCubeX(int x) => (int)Math.Ceiling((IslandX(x) / 64 * 64 - OffsetX) / (double)Scale);
    public int FirstColumnOfCubeZ(int z) => (int)Math.Ceiling((IslandZ(z) / 64 * 64 - OffsetZ) / (double)Scale);
    // an LBA1 place in a scene of the temple (its world units: a cell 512, a layer 256 -- its cell (X, Z) from X * 512 - 256, LBA1's OBJECT.C)
    // as island world units
    public (double X, double Y, double Z) FromLba1(TempleLayout.Placed scene, int x, int y, int z)
        => (WorldX(scene.X + (x + 256) / 512.0), WorldY(scene.Y + y / 256.0), WorldZ(scene.Z + (z + 256) / 512.0));
    public IEnumerable<(int X, int Z)> Cubes()
    {
        for (var cz = CubeZ0; cz < CubeZ0 + CubesZ; cz++)
            for (var cx = CubeX0; cx < CubeX0 + CubesX; cx++) yield return (cx, cz);
    }
}

// LBA1's Temple of Bú as an LBA2 island of its own (2026-10-16, the user's: "Port the temple of Bu from LBA1 into LBA2 as a separate island.
// Make sure it is visually the same with the same colour palette, which means when viewed from the same angle as the fixed LBA1 angle it
// should match"), into a game folder:
//   - TEMPLE.ILE: its cubes (TempleFrame), their ground undrawn -- the island's sea under the temple, black as LBA1's void (its sky's) -- and
//     the temple's objects (TempleObjects): every cell of its three scenes a box of the brick's own faces, and the boxes it is solid with;
//   - TEMPLE.OBL: their bodies, an empty one for the boxes first;
//   - RESS.HQR: its palette (TemplePalette: Otringal's, LBA1's colours for the temple written in) in entry 40 and its sky in 24 -- RESS_XPL0 +
//     13 and RESS_SKYSEA0 + 13, two slots the retail file leaves empty, which no retail island's number reaches. The race-track mode draws
//     the island as Otringal's (island 4: its scenes are 4's) from the island's file with that palette and sky (the car file's island_file=,
//     island_palette=, island_sky=); the editor's view knows it by its name (RENDERER_API.CPP).
internal static class TempleIsland
{
    public const string IleFile = "TEMPLE.ILE", OblFile = "TEMPLE.OBL";
    // the layout, for the race track's design (tools/RaceTrackPlan/temple_design.py): the frame, and each LBA1 column's cells
    public const string LayoutFile = "TEMPLE.JSON";
    // (the file's layout made from Otringal's -- its atlases are replaced -- and its cubes' settings from Otringal's palace cube's)
    public const string SourceIle = "OTRINGAL.ILE";
    private static readonly (int X, int Z) SettingsCube = (8, 7);
    public const int IslandByte = 4;
    public const int PaletteEntry = 40, SkyEntry = 24;
    // the fog: black, from this far in front of the camera to the far plane, where nothing more is drawn -- inside the 3 x 3 cubes round the
    // camera whose objects the engine draws (a cube's width)
    public const int StartZFog = 12000, ClipZFar = 32000;
    public const int EmptyBody = 0;

    // (Layout: the temple as LBA1 has it -- the layout file's; the objects are built without the cells cut for a race track's road)
    public sealed record Built(TempleLayout Layout, TempleFrame Frame, IslandFile Island, List<byte[]> Bodies, TemplePalette.Result Palette, byte[] Sky, List<string> Log);

    public static bool IsInstalled(string gameDirectory) => File.Exists(Path.Combine(gameDirectory, IleFile));

    // `cuts`: cells left out (TempleLayout's x, layer, z) -- the doorways a race track's road goes through (RaceTrackPlan.TempleCuts).
    public static Built Build(string lba1Directory, string gameDirectory, string? sourceDirectory = null, IEnumerable<int[]>? cuts = null)
    {
        if (!Lba1Game.IsInstalled(lba1Directory)) throw new InvalidDataException($"No LBA1 game in {lba1Directory}.");
        sourceDirectory ??= gameDirectory;
        var log = new List<string>();
        var game = new Lba1Game(lba1Directory);
        var layout = TempleLayout.Build(game);
        log.AddRange(layout.Log);
        var frame = new TempleFrame(layout.MinY);
        if (frame.IslandX(layout.MinX) < TempleFrame.CubeX0 * 64 || frame.IslandX(layout.MaxX + 1) > (TempleFrame.CubeX0 + TempleFrame.CubesX) * 64
            || frame.IslandZ(layout.MinZ) < TempleFrame.CubeZ0 * 64 || frame.IslandZ(layout.MaxZ + 1) > (TempleFrame.CubeZ0 + TempleFrame.CubesZ) * 64)
            throw new InvalidOperationException("The temple is past the island's cubes.");
        if (frame.WorldY(layout.MaxY + 1) > 32000) throw new InvalidOperationException($"The temple's top ({frame.WorldY(layout.MaxY + 1):0}) is higher than the engine's heights hold.");

        // ---- the island: its cubes, every cell undrawn sea (the engine's sea under them), Otringal's palace cube's settings with the sea in
        // every patch, the black fog, and no animated ground
        var source = IslandFile.Load(Path.Combine(sourceDirectory, SourceIle));
        var cubes = frame.Cubes().ToList();
        var island = IslandFile.Parse(IslandScaler.NewFile(source, cubes), Path.Combine(gameDirectory, IleFile));
        var settings = source.CubeAt(SettingsCube.X, SettingsCube.Z)?.Info ?? throw new InvalidDataException($"{SourceIle} has no cube {SettingsCube}.");
        var sea = new IslandPolygon(0).With(bank: 6, texFlag: 0, polyFlag: 0, sampleStep: 0, codeJeu: 1, textureIndex: 0).Raw;
        foreach (var (cx, cz) in cubes)
        {
            var cube = island.CubeAt(cx, cz)!;
            var info = (int[])settings.Clone();
            info[IslandCube.InfoAlphaLight] = (info[IslandCube.InfoAlphaLight] & 0xFFFF) | unchecked((int)0xFFFF0000);
            info[IslandCube.InfoStartZFog] = StartZFog; info[IslandCube.InfoClipZFar] = ClipZFar;
            for (var i = 6; i < IslandCube.InfoSize; i++) info[i] = -1;
            cube.Info = info;
            for (var i = 0; i < cube.Polygons.Length; i++) cube.Polygons[i] = sea;
            Array.Fill(cube.Intensity, (byte)12);
        }
        log.Add($"{IleFile}: {cubes.Count} cubes ({TempleFrame.CubeX0}..{TempleFrame.CubeX0 + TempleFrame.CubesX - 1} x {TempleFrame.CubeZ0}..{TempleFrame.CubeZ0 + TempleFrame.CubesZ - 1}), " +
                $"the temple {TempleFrame.Scale} times LBA1's size from cell ({TempleFrame.OffsetX}, {TempleFrame.OffsetZ}), its foot at {TempleFrame.BaseY}, its top at {frame.WorldY(layout.MaxY + 1):0}; " +
                $"black fog from {StartZFog} to {ClipZFar}");

        // ---- the palette, the sky
        var ress = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR"));
        var palette = TemplePalette.Build(ress.Read(TemplePalette.Source), game.Palette, TempleObjects.ColourCounts(game, layout));
        log.AddRange(palette.Log);
        var sky = TemplePalette.Sky(palette.Palette);

        // ---- the objects
        var bodies = new List<byte[]> { RaceTrackPipes.Write(new List<System.Numerics.Vector3> { System.Numerics.Vector3.Zero }, new List<Face>(), lit: false) };
        var built = layout;
        // (the round things -- barrels, columns, amphorae, goblets, fire bowls -- turned, not boxes: TempleShapes; the shaped things -- rocks,
        // grilles, spiked rails, iron fans, statues, stairs, a bed of spikes -- models: TempleModels)
        var shapes = TempleShapes.Find(game, log);
        var models = TempleModels.Find(game, log);
        if (cuts is not null)
        {
            var cut = cuts.Where(c => c.Length == 3).Select(c => (c[0], c[1], c[2])).ToHashSet();
            built = layout.Without(cut);
            log.Add($"{layout.Cells.Count - built.Cells.Count} cells cut where the race track's road goes through the walls");
            // (and what the doorways leave of the round things they cut through, hanging over the road, or standing on nothing; and of the
            // shaped things, theirs and what stood on the round things' -- a statue whole or not at all, a grille or a row of rocks but the
            // columns along it they cut)
            var remains = TempleShapes.CutRemains(shapes, layout.Cells, cut);
            if (remains.Count > 0)
            {
                built = built.Without(remains);
                log.Add($"{remains.Count} cells of round things left hanging by the cuts taken out with them");
            }
            var modelRemains = TempleModels.CutRemains(models, layout.Cells, cut.Concat(remains).ToHashSet());
            if (modelRemains.Count > 0)
            {
                built = built.Without(modelRemains);
                log.Add($"{modelRemains.Count} cells of shaped things the cuts took part of taken out with them");
            }
        }
        var objects = TempleObjects.Build(game, built, island, frame, palette.Map, bodies.Count, EmptyBody, shapes, models);
        bodies.AddRange(objects.Bodies);
        log.AddRange(objects.Log);
        log.Add($"{OblFile}: {bodies.Count} bodies (an empty one for the boxes, then the temple's)");
        return new Built(layout, frame, island, bodies, palette, sky, log);
    }

    // Writes a built island into a game folder: its files, and its palette and sky into RESS.HQR. Returns lines for the log.
    public static List<string> Install(string gameDirectory, Built built)
    {
        var log = new List<string>();
        Writable(Path.Combine(gameDirectory, "RESS.HQR"));
        // (a build's own file replaced: no .bak of it left beside it)
        var ilePath = Path.Combine(gameDirectory, IleFile);
        if (File.Exists(ilePath)) { Writable(ilePath); File.Delete(ilePath); }
        built.Island.Save(ilePath);
        if (File.Exists(ilePath + ".bak")) File.Delete(ilePath + ".bak");
        File.WriteAllBytes(Path.Combine(gameDirectory, OblFile), PolarIsland.Hqr(built.Bodies));
        File.WriteAllText(Path.Combine(gameDirectory, LayoutFile), LayoutJson(built));
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var bytes = File.ReadAllBytes(ressPath);
        bytes = Set(bytes, PaletteEntry, built.Palette.Xpl);
        bytes = Set(bytes, SkyEntry, built.Sky);
        File.WriteAllBytes(ressPath, bytes);
        log.Add($"{IleFile} and {OblFile} written; RESS.HQR: the temple's palette (entry {PaletteEntry}) and its night sky (entry {SkyEntry})");
        return log;
    }

    // The layout as the race track's design reads it: the frame (a layout cell's island cell is offset + scale * its own; a layer's foot
    // baseY + (layer - minY) * layer), the scenes' places, and each column's cells -- a bit for each layer from minY up -- with its top
    // brick's LBA1 code.
    private static string LayoutJson(Built built)
    {
        var columns = new Dictionary<(int X, int Z), (long Bits, int Top, int Code)>();
        foreach (var ((x, y, z), cell) in built.Layout.Cells)
        {
            columns.TryGetValue((x, z), out var c);
            var top = c.Bits == 0 || y > c.Top;
            columns[(x, z)] = (c.Bits | 1L << (y - built.Layout.MinY), top ? y : c.Top, top ? cell.Code : c.Code);
        }
        var layout = new Dictionary<string, object>
        {
            ["scale"] = TempleFrame.Scale, ["offsetX"] = TempleFrame.OffsetX, ["offsetZ"] = TempleFrame.OffsetZ, ["baseY"] = TempleFrame.BaseY,
            ["minY"] = built.Layout.MinY, ["maxY"] = built.Layout.MaxY, ["layer"] = 256 * TempleFrame.Scale,
            ["width"] = built.Layout.MaxX + 1, ["depth"] = built.Layout.MaxZ + 1,
            ["cubes"] = new[] { TempleFrame.CubeX0, TempleFrame.CubeZ0, TempleFrame.CubesX, TempleFrame.CubesZ },
            ["scenes"] = built.Layout.Placements.Select(p => new[] { p.Scene, p.X, p.Y, p.Z }).ToList(),
            ["columns"] = columns.OrderBy(c => c.Key.Z).ThenBy(c => c.Key.X).Select(c => new object[] { c.Key.X, c.Key.Z, c.Value.Bits, c.Value.Top, c.Value.Code }).ToList(),
        };
        return System.Text.Json.JsonSerializer.Serialize(layout);
    }

    public static List<string> Add(string lba1Directory, string gameDirectory)
    {
        var built = Build(lba1Directory, gameDirectory);
        return built.Log.Concat(Install(gameDirectory, built)).ToList();
    }

    // Takes the island out of a game folder: its files, and RESS.HQR's two entries emptied (the retail file has none there).
    public static bool Remove(string gameDirectory)
    {
        var gone = false;
        foreach (var f in new[] { IleFile, OblFile, IleFile + ".bak", LayoutFile })
        {
            var path = Path.Combine(gameDirectory, f);
            if (File.Exists(path)) { Writable(path); File.Delete(path); gone = true; }
        }
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        if (File.Exists(ressPath))
        {
            var ress = HqrFile.Parse(File.ReadAllBytes(ressPath));
            var changed = false;
            foreach (var entry in new[] { PaletteEntry, SkyEntry })
                if (entry < ress.Count && !ress.IsEmpty(entry)) { ress.Clear(entry); changed = true; }
            if (changed) { Writable(ressPath); File.WriteAllBytes(ressPath, ress.ToBytes()); gone = true; }
        }
        return gone;
    }

    private static void Writable(string path)
    {
        var info = new FileInfo(path);
        if (info.Exists && info.IsReadOnly) info.IsReadOnly = false;
    }

    // An HQR entry set: an empty retail slot is given data, one with data replaced.
    private static byte[] Set(byte[] hqr, int slot, byte[] data)
    {
        try { return HqrWriter.FillEntry(hqr, slot, HqrWriter.StoredEntry(data)); }
        catch (InvalidDataException) { return HqrWriter.ReplaceEntry(hqr, slot, HqrWriter.StoredEntry(data)); }
    }
}
