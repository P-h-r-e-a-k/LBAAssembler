using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Scenes;
using LBAAssembler.Terrain.Polar;
using LBAAssembler.Terrain.Snow;

namespace LBAAssembler.Terrain.Harvest;

// The Long Table (the user, 2026-11-02: "designing a thanksgiving track. This will be a totally new track we create from scratch instead of
// based on anything. Let's make it Canadian thanksgiving related too and not just American"): an island of its own, raced as Otringal -- island
// 4 -- drawn from HARVEST.ILE in its own palette under its own sky, the way the Temple of Bú is (TempleTrack): the race track's build makes the
// island and a scene for each cube the road runs through, each a copy of the header of Otringal's scene 91 with nobody in it but Twinsen, Zoe's
// placeholder and the Desert island's buggy, and a cube-change zone along each edge it shares with another. The design: docs/LBA2_HARVEST_TRACK.md,
// tools/RaceTrackPlan/harvest_design.py.
internal static class HarvestTrack
{
    // past the other tracks' (Island CX Mk2's 340-343)
    public const int FirstScene = 350;
    public const string PlanResource = "RaceTrackPlan.Harvest.json";
    public const string IleFile = "HARVEST.ILE", OblFile = "HARVEST.OBL";
    public const int IslandByte = 4;
    // RESS.HQR's empty entries (the Temple of Bú has 40 and 24; the retail game none of these)
    public const int PaletteEntry = 41, SkyEntry = 25;
    private const int PaletteSource = 31;
    // the border, island z (cell 416), and three of the palette's leaf colours: maple red, orange, gold
    public const string LeavesLine = "leaves=212992 74 88 106\n";
    private const int SourceScene = 91;
    // Citadel Island's first outside scene: the game's sunniest music and its birds, for a day in the country
    private const int SoundScene = 42;
    private const int Near = 512, Far = 32768 - 1024, Edge = 512, Side = 32768, ZoneTop = 32000;

    public static readonly (int Cx, int Cz)[] SceneCubes = CubesOf(PlanResource);
    public static int LastScene => FirstScene + Math.Max(1, SceneCubes.Length) - 1;
    public static int SceneOf(int cx, int cz) => Array.IndexOf(SceneCubes, (cx, cz)) is var i and >= 0 ? FirstScene + i : -1;

    private static (int Cx, int Cz)[] CubesOf(string resource)
    {
        using var stream = typeof(HarvestTrack).Assembly.GetManifestResourceStream(resource);
        if (stream is null) return Array.Empty<(int, int)>();
        var plan = RaceTrackPlan.Read(stream);
        var cubes = new HashSet<(int Cx, int Cz)>();
        for (var i = 0; i < plan.Points.Length; i++)
        {
            var reach = (plan.RaisedHalfs is { } halfs && i < halfs.Length ? halfs[i] : plan.RaisedHalf ?? 5) + 1;
            double x = plan.OriginCellX + plan.Points[i][0], z = plan.OriginCellZ + plan.Points[i][1];
            for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                    cubes.Add(((int)Math.Floor((x + dx * reach) / 64), (int)Math.Floor((z + dz * reach) / 64)));
        }
        return cubes.OrderBy(c => c.Cz).ThenBy(c => c.Cx).ToArray();
    }

    // The island and its scenes, made afresh in a game folder whose files are the originals (the race track's build).
    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var ress = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR"));
        var palette = HarvestPalette.Build(ress.Read(PaletteSource));
        log.AddRange(palette.Log);
        var design = HarvestDesign.Load();
        var terrain = HarvestTerrain.Build(gameDirectory, design, palette.Palette, IleFile);
        log.AddRange(terrain.Log);
        var sun = BakeOptions.For(terrain.Island.CubeAt(HarvestDesign.CubeX0, HarvestDesign.CubeZ0)!);
        var objects = HarvestObjects.Build(terrain.Island, new SnowShading(palette.Palette, sun.Azimuth, sun.Elevation), design);
        log.AddRange(objects.Log);

        var ilePath = Path.Combine(gameDirectory, IleFile);
        terrain.Island.Save(ilePath);
        if (File.Exists(ilePath + ".bak")) File.Delete(ilePath + ".bak");
        File.WriteAllBytes(Path.Combine(gameDirectory, OblFile), PolarIsland.Hqr(objects.Bodies));
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var hqr = HqrFile.Parse(File.ReadAllBytes(ressPath));
        foreach (var (entry, bytes) in new[] { (PaletteEntry, palette.Xpl), (SkyEntry, HarvestPalette.Sky(palette.Palette)) })
        {
            while (hqr.Count <= entry) hqr.Slots.Add(new HqrFile.Slot());
            hqr.SetStored(entry, bytes);
        }
        File.WriteAllBytes(ressPath, hqr.ToBytes());
        log.Add($"{IleFile} and {OblFile} written; RESS.HQR: the island's palette (entry {PaletteEntry}) and its sky (entry {SkyEntry})");
        log.AddRange(AddScenes(gameDirectory));
        return log;
    }

    private static List<string> AddScenes(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var source = SceneSerializer.Parse(SceneGame.Lba2, HqrArchive.Open(scenePath).Read(SourceScene + 1));
        var sound = SceneSerializer.Parse(SceneGame.Lba2, HqrArchive.Open(scenePath).Read(SoundScene + 1));
        source.Music = sound.Music;
        source.Ambient = sound.Ambient.Select(a => a.Clone()).ToArray();
        source.SecondMin = sound.SecondMin; source.SecondEcart = sound.SecondEcart;
        var hqr = HqrFile.Parse(File.ReadAllBytes(scenePath));
        var buggy = HqrArchive.Open(scenePath) is var archive && SceneSerializer.Parse(SceneGame.Lba2, archive.Read(RaceTrackScenes.BuggyScene + 1)) is var desert
            ? desert.Actors.Skip(1).FirstOrDefault(a => a.Entity == RaceTrackScenes.BuggyEntity)?.Clone() : null;
        if (buggy is not null)
        {
            RaceTrackScenes.BuggyAlwaysThere(buggy);
            if (buggy.Life.Length > 9 && buggy.Life[8] == 0x46 && buggy.Life[9] == 0) buggy.Life[9] = 1;
        }
        var cubes = SceneCubes.ToHashSet();
        var largest = BinaryPrimitives.ReadInt32LittleEndian(hqr.Read(0));
        var zones = 0;
        var island = IslandFile.Load(Path.Combine(gameDirectory, IleFile));
        foreach (var (cx, cz) in SceneCubes)
        {
            var model = source.Clone();
            model.Island = IslandByte; model.CubeX = cx; model.CubeY = cz;
            var keepZoe = model.Actors.Count > 1 && model.Actors[1].Entity == 14 && model.Actors[1].X == 0 && model.Actors[1].Z == 0;
            model.Actors.RemoveRange(keepZoe ? 2 : 1, model.Actors.Count - (keepZoe ? 2 : 1));
            model.Zones.Clear(); model.TrackPoints.Clear();
            foreach (var a in model.Actors) { a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 }; }
            var hero = model.Hero;
            hero.X = Side / 2; hero.Z = Side / 2; hero.Y = Math.Max(0, (int)(IslandOps.Altitude(island, cx * Side + Side / 2.0, cz * Side + Side / 2.0) ?? 0));
            hero.Beta = 0;
            if (buggy is not null)
            {
                var copy = buggy.Clone();
                copy.X = hero.X + 1024; copy.Y = hero.Y; copy.Z = hero.Z; copy.Beta = 0;
                SceneOps.AddActor(model, copy);
            }
            void Zone(int dx, int dz, int x0, int z0, int x1, int z1, int info0, int info2)
            {
                if (!cubes.Contains((cx + dx, cz + dz))) return;
                var zone = new SceneZoneModel { Type = 0, Num = SceneOf(cx + dx, cz + dz), X0 = x0, Y0 = 0, Z0 = z0, X1 = x1, Y1 = ZoneTop, Z1 = z1, Info = new int[8] };
                zone.Info[0] = info0; zone.Info[2] = info2; zone.Info[7] = 1;   // ZONE_ON
                model.Zones.Add(zone);
                zones++;
            }
            Zone(1, 0, Side - Edge, 0, Side, Side, Near, 0);
            Zone(-1, 0, 0, 0, Edge, Side, Far, 0);
            Zone(0, 1, 0, Side - Edge, Side, Side, 0, Near);
            Zone(0, -1, 0, 0, Side, Edge, 0, Far);
            var record = SceneSerializer.Write(model);
            var entry = SceneOf(cx, cz) + 1;
            while (hqr.Count < entry) hqr.Slots.Add(new HqrFile.Slot());
            if (hqr.Count == entry) hqr.Add(record); else hqr.SetStored(entry, record);
            largest = Math.Max(largest, record.Length);
        }
        var size = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(size, largest);
        hqr.SetStored(0, size);
        File.WriteAllBytes(scenePath, hqr.ToBytes());
        string? names = null;
        foreach (var (cx, cz) in SceneCubes)
            names = HqdWriter.Describe(gameDirectory, "SCENE.HQR", SceneGame.Lba2, SceneOf(cx, cz) + 1, $"The Long Table: cube ({cx}, {cz})", names);
        if (names is not null) File.WriteAllText(Path.Combine(gameDirectory, HqdWriter.SidecarName("SCENE.HQR")), names, System.Text.Encoding.Latin1);
        log.Add($"SCENE.HQR: scenes {FirstScene}-{LastScene}, the island's cubes the road runs through ({zones} cube-change zones), Otringal's scene {SourceScene}'s light");
        return log;
    }
}
