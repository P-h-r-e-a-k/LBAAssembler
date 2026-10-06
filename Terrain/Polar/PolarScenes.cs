using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Lba1;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain.Polar;

// Polar Island's scenes: an LBA2 outside scene is one cube of its island, so the island's twelve cubes are twelve scenes (FirstScene on,
// row by row from cube (CubeX0, CubeZ0)), each a copy of the Citadel's scene 44's header (an outside scene's: its light, ambience, music)
// with island 12 and its own cube, nobody in it but Twinsen (and the engine's Zoe placeholder), and a cube-change zone along each edge it
// shares with another cube of the island -- the zones retail outside scenes have (OBJECT.CPP GereZoneChangeCube: a zone half a cell
// wide on the edge, its Info0/Info2 the place on the far side, 512 or 32768 - 1024 for the edge Twinsen comes in by). Twinsen starts in
// the scene of the dock -- where LBA1 starts him on the island (scene 115, "1st scene", from Fortress Island).
//
// HOLOMAP.HQR's positions (entry 12): record 12 becomes the island's place on the planet (the twelve retail islands' are records 0-11;
// 12 is empty), and each scene's record (50 + scene) says it is an outside scene (FlagHolo 4, which the cube-change zones test) of island
// 12. TEXT.HQR: island 12's text file (the engine's file 15, MESSAGE.CPP TextEntry) is a pair of entries for each language after the
// retail ones; its one text so far is the island's name.
internal static class PolarScenes
{
    // past the retail game's 0..221 and the race track builder's 222..232 (the story's holomap arrow, the lava lake, Sendell's Well, the
    // old moon's four, the Emerald Moon's four)
    public const int FirstScene = 233;
    public const int DockScene = 115;
    private const int SourceScene = 44;
    public const int PositionsEntry = 12, PositionSize = 32, MaxObjectif = 50, FlagExterior = 4;

    // The holomap position records the island has: its place on the planet (12) and its scenes'.
    public static IEnumerable<int> Records() => Enumerable.Range(FirstScene, Count).Select(n => MaxObjectif + n).Prepend(PolarIsland.IslandByte);

    // A record as the retail file has it: island 12's an empty label (no planet, no island), a scene's past the game's own empty.
    public static byte[] RetailRecord(int record)
    {
        var r = new byte[PositionSize];
        if (record < MaxObjectif) { BinaryPrimitives.WriteInt32LittleEndian(r.AsSpan(24), 0); r[28] = 0xFF; r[30] = 0xFF; r[31] = 0xFF; }
        else BinaryPrimitives.WriteInt32LittleEndian(r.AsSpan(24), -1);
        return r;
    }
    // the island's place on the planet (Twinsun, near its north pole): latitude 0..2048 from the pole, longitude 0..4096, height
    public const int Alpha = 180, Beta = 1200, Altitude = 1100;
    // TEXT.HQR: the engine's languages and files to a language (MESSAGE.H NB_LANGUAGES, MESSAGE.CPP MAX_TEXT_LANG); island 12's file
    public const int Languages = 6, RetailFiles = 15, TextFile = 15;
    public static int TextEntry(int language) => Languages * RetailFiles * 2 + ((TextFile - RetailFiles) * Languages + language) * 2;

    // a cube-change zone's edges (OBJECT.CPP: ZONE_EST / ZONE_OUEST in Info0, ZONE_SUD / ZONE_NORD in Info2) and its height
    private const int Near = 512, Far = 32768 - 1024, Edge = 512, Side = 32768, ZoneTop = 25000;

    public static int SceneOf(int cx, int cz) => FirstScene + (cz - PolarTerrain.CubeZ0) * PolarTerrain.CubesX + (cx - PolarTerrain.CubeX0);
    public static int Count => PolarTerrain.CubesX * PolarTerrain.CubesZ;

    // An LBA1 position of a scene of the layout as a place on the island: its cube and the place in it (the island's ground is a layer
    // lower than LBA1's: a brick's top, (layer + 1) * 256 in LBA1, is the island's layer * 256 -- PolarTerrain.SurfaceOf).
    public static (int Cx, int Cz, int X, int Y, int Z) OnIsland(PolarLayout layout, PolarTerrain.Result terrain, int scene, int x, int y, int z)
    {
        var p = layout.Placements.First(q => q.Scene == scene);
        int wx = x + (p.X + terrain.OffsetX) * 512, wz = z + (p.Z + terrain.OffsetZ) * 512, wy = y + p.Y * 256 - 256;
        return (wx / 32768, wz / 32768, wx % 32768, Math.Max(0, wy), wz % 32768);
    }

    public static List<string> Install(string gameDirectory, Lba1Game game, PolarLayout layout, PolarTerrain.Result terrain)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var source = SceneSerializer.Parse(SceneGame.Lba2, HqrArchive.Open(scenePath).Read(SourceScene + 1));
        var hqr = HqrFile.Parse(File.ReadAllBytes(scenePath));
        var lba1Hero = game.LoadScene(DockScene).Actors[0];
        var buggy = HqrArchive.Open(scenePath) is var archive && SceneSerializer.Parse(SceneGame.Lba2, archive.Read(RaceTrackScenes.BuggyScene + 1)) is var desert
            ? desert.Actors.Skip(1).FirstOrDefault(a => a.Entity == RaceTrackScenes.BuggyEntity)?.Clone() : null;
        if (buggy is not null)
        {
            RaceTrackScenes.BuggyAlwaysThere(buggy);
            // (its INIT_BUGGY, 46 xx after the quest's IF, with mode 0 only puts back a car the game already has -- the Desert's, once the
            // quest is done; mode 1 makes it here the first time, and leaves one the player has elsewhere where it is: GERELIFE.CPP, BUGGY.CPP)
            if (buggy.Life.Length > 9 && buggy.Life[8] == 0x46 && buggy.Life[9] == 0) buggy.Life[9] = 1;
        }
        (int Scene, (int X, int Y, int Z) Place)? carAt = null;
        var cars = 0;
        var start = OnIsland(layout, terrain, DockScene, lba1Hero.X, lba1Hero.Y, lba1Hero.Z);
        // (the cubes with land: one of open sea has no scene, nor a zone into it -- Twinsen would drown before he got there)
        var cubes = terrain.Columns.Where(c => !c.Value.Water).Select(c => (Cx: (c.Key.X + terrain.OffsetX) / 64, Cz: (c.Key.Z + terrain.OffsetZ) / 64)).ToHashSet();
        var largest = BinaryPrimitives.ReadInt32LittleEndian(hqr.Read(0));
        var zones = 0;
        foreach (var (cx, cz) in cubes.OrderBy(c => SceneOf(c.Cx, c.Cz)))
        {
            var model = source.Clone();
            model.Island = PolarIsland.IslandByte; model.CubeX = cx; model.CubeY = cz;
            var keepZoe = model.Actors.Count > 1 && model.Actors[1].Entity == 14 && model.Actors[1].X == 0 && model.Actors[1].Z == 0;
            model.Actors.RemoveRange(keepZoe ? 2 : 1, model.Actors.Count - (keepZoe ? 2 : 1));
            model.Zones.Clear(); model.TrackPoints.Clear();
            foreach (var a in model.Actors) { a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 }; }
            var hero = model.Hero;
            if ((cx, cz) == (start.Cx, start.Cz)) { hero.X = start.X; hero.Y = start.Y; hero.Z = start.Z; }
            else { var (x, y, z) = StandingPlace(layout, terrain, cx, cz); hero.X = x; hero.Y = y; hero.Z = z; }
            // Twinsen's car (the island is to have a race track): the Desert island's own buggy (RaceTrackScenes.BuggyScene), there from
            // the start of any game (its script waits for the car quest: BuggyAlwaysThere), in every scene as on the Desert island -- the
            // engine hands the car on to the next scene's at a cube change (BUGGY.CPP InitBuggy: without one there, the car stopped at the
            // first edge). In the scene Twinsen first comes to on the island it stands beside him (INIT_BUGGY 1); after that the car is where
            // he left it.
            if (buggy is not null && CarPlace(layout, terrain, cx, cz, (hero.X, hero.Z)) is { } car)
            {
                var copy = buggy.Clone();
                copy.X = car.X; copy.Y = car.Y; copy.Z = car.Z; copy.Beta = 0;
                SceneOps.AddActor(model, copy);
                cars++;
                if ((cx, cz) == (start.Cx, start.Cz)) carAt = (SceneOf(cx, cz), car);
            }
            hero.Beta = 0;
            // the edges it shares: east (x + 1), west, south (z + 1), north
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
        // (a scene an earlier build of the island had for a cube that has none now goes)
        for (var n = FirstScene; n < FirstScene + Count; n++)
            if (!cubes.Any(c => SceneOf(c.Cx, c.Cz) == n) && n + 1 < hqr.Count && !hqr.IsEmpty(n + 1)) hqr.Clear(n + 1);
        var size = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(size, largest);
        hqr.SetStored(0, size);
        File.WriteAllBytes(scenePath, hqr.ToBytes());
        // (their names, for the editor's scene list: the folder's SCENE.HQD, which it reads over the game's own descriptions)
        ForgetNames(gameDirectory);
        string? names = null;
        foreach (var (cx, cz) in cubes.OrderBy(c => SceneOf(c.Cx, c.Cz)))
            names = HqdWriter.Describe(gameDirectory, "SCENE.HQR", SceneGame.Lba2, SceneOf(cx, cz) + 1, NameOf(cx, cz, terrain), names);
        if (names is not null) File.WriteAllText(Path.Combine(gameDirectory, HqdWriter.SidecarName("SCENE.HQR")), names, System.Text.Encoding.Latin1);
        log.Add(carAt is { } c ? $"Twinsen's car (scene {RaceTrackScenes.BuggyScene}'s buggy) in {cars} scenes; in the dock's, {c.Scene}, at {c.Place}" : "no car: scene 67 has no buggy, or no room beside Twinsen");
        log.Add($"SCENE.HQR: scenes {string.Join(", ", cubes.Select(c => SceneOf(c.Item1, c.Item2)).Order())} (a scene to a cube with land, {zones} cube-change zones); Twinsen starts in scene {SceneOf(start.Cx, start.Cz)} at ({start.X}, {start.Y}, {start.Z}), LBA1's start on the dock");

        // the holomap's records
        var holoPath = Path.Combine(gameDirectory, "HOLOMAP.HQR");
        var positions = HqrArchive.Open(holoPath).Read(PositionsEntry);
        if (positions.Length < (MaxObjectif + FirstScene + Count) * PositionSize) throw new InvalidDataException("The holomap's position table is shorter than the game's own.");
        void Record(int index, int x, int y, int z, int mess, byte flag)
        {
            var at = index * PositionSize;
            int[] words = { x, y, z, Alpha, Beta, Altitude, mess };
            for (var i = 0; i < words.Length; i++) BinaryPrimitives.WriteInt32LittleEndian(positions.AsSpan(at + i * 4), words[i]);
            positions[at + 28] = 0xFF;                 // ObjFix: none
            positions[at + 29] = flag;                 // FlagHolo
            positions[at + 30] = 0;                    // Planet: Twinsun
            positions[at + 31] = PolarIsland.IslandByte;
        }
        Record(PolarIsland.IslandByte, 0, 0, 0, PolarHolomap.LabelText, 0);
        foreach (var (cx, cz) in cubes)
            Record(MaxObjectif + SceneOf(cx, cz), cx * Side + Side / 2, 0, cz * Side + Side / 2, -1, FlagExterior);
        foreach (var (cx, cz, _) in PolarTerrain.Cubes(terrain.Island).Where(c => !cubes.Contains((c.Cx, c.Cz))))
            RetailRecord(MaxObjectif + SceneOf(cx, cz)).CopyTo(positions.AsSpan((MaxObjectif + SceneOf(cx, cz)) * PositionSize));
        File.WriteAllBytes(holoPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(holoPath), PositionsEntry, HqrWriter.StoredEntry(positions)));
        log.Add($"HOLOMAP.HQR: island 12's place on the planet (record 12) and its scenes' records (outside scenes of island 12)");

        log.Add(WriteTexts(gameDirectory));
        // (the rocky peak and its plateau on the holomap's picture: each column a box, from the sea to its top, in its top brick's colour)
        var solids = layout.Cells.Where(c => layout.Peak.Contains((c.Key.X, c.Key.Z)) || c.Value.Scene == PolarLayout.PlateauScene)
            .GroupBy(c => (c.Key.X, c.Key.Z)).Select(g =>
            {
                var top = g.MaxBy(c => c.Key.Y);
                double x0 = (g.Key.X + terrain.OffsetX) * 512.0, z0 = (g.Key.Z + terrain.OffsetZ) * 512.0;
                return (x0, z0, x0 + 512, z0 + 512, 0.0, top.Key.Y * 256.0, PolarLayout.BrickColour(game, top.Value.Brick));
            }).ToList();
        log.AddRange(PolarHolomap.Install(gameDirectory, terrain.Island, solids));
        return log;
    }

    // Where Twinsen starts in a cube's scene (cube-local, the ground's height): the flat land cell nearest the cube's middle with nothing
    // standing on it.
    public static (int X, int Y, int Z) StandingPlace(PolarLayout layout, PolarTerrain.Result terrain, int cx, int cz)
    {
        var best = (X: Side / 2, Y: 0, Z: Side / 2); var bestD = double.MaxValue;
        foreach (var ((x, z), c) in terrain.Columns)
        {
            int gx = x + terrain.OffsetX, gz = z + terrain.OffsetZ;
            if (c.Water || gx / 64 != cx || gz / 64 != cz) continue;
            var surface = PolarTerrain.SurfaceOf(c);
            if (terrain.Island.HeightAt(gx, gz) != surface || terrain.Island.HeightAt(gx + 1, gz) != surface
                || terrain.Island.HeightAt(gx, gz + 1) != surface || terrain.Island.HeightAt(gx + 1, gz + 1) != surface) continue;
            if (layout.Cells.ContainsKey((x, c.Top + 1, z)) || layout.Cells.ContainsKey((x, c.Top + 2, z))) continue;
            double dx = gx % 64 + 0.5 - 32, dz = gz % 64 + 0.5 - 32, d = dx * dx + dz * dz;
            if (d < bestD) { bestD = d; best = ((gx % 64) * 512 + 256, surface, (gz % 64) * 512 + 256); }
        }
        return best;
    }

    // Where Twinsen's car stands (cube-local, the ground's height): the land cell nearest `near` (cube-local) but two cells and more from
    // it, flat and clear in the 3 x 3 cells round it (the car is about two cells long).
    public static (int X, int Y, int Z)? CarPlace(PolarLayout layout, PolarTerrain.Result terrain, int cx, int cz, (int X, int Z) near)
    {
        (int X, int Y, int Z)? best = null; var bestD = double.MaxValue;
        bool Clear(int gx, int gz, int surface)
        {
            if (!terrain.Columns.TryGetValue((gx - terrain.OffsetX, gz - terrain.OffsetZ), out var c) || c.Water || PolarTerrain.SurfaceOf(c) != surface) return false;
            if (terrain.Island.HeightAt(gx, gz) != surface || terrain.Island.HeightAt(gx + 1, gz) != surface || terrain.Island.HeightAt(gx, gz + 1) != surface || terrain.Island.HeightAt(gx + 1, gz + 1) != surface) return false;
            return !layout.Cells.ContainsKey((gx - terrain.OffsetX, c.Top + 1, gz - terrain.OffsetZ)) && !layout.Cells.ContainsKey((gx - terrain.OffsetX, c.Top + 2, gz - terrain.OffsetZ));
        }
        foreach (var ((x, z), c) in terrain.Columns)
        {
            int gx = x + terrain.OffsetX, gz = z + terrain.OffsetZ;
            if (c.Water || gx / 64 != cx || gz / 64 != cz || gx % 64 < 1 || gx % 64 > 62 || gz % 64 < 1 || gz % 64 > 62) continue;
            double dx = (gx % 64 + 0.5) * 512 - near.X, dz = (gz % 64 + 0.5) * 512 - near.Z, d = Math.Sqrt(dx * dx + dz * dz);
            if (d < 2 * 512 || d >= bestD) continue;
            var surface = PolarTerrain.SurfaceOf(c);
            var clear = true;
            for (var oz = -1; oz <= 1 && clear; oz++) for (var ox = -1; ox <= 1 && clear; ox++) clear = Clear(gx + ox, gz + oz, surface);
            if (!clear) continue;
            bestD = d; best = ((gx % 64) * 512 + 256, surface, (gz % 64) * 512 + 256);
        }
        return best;
    }

    // A cube's scene's name: "Polar Island: " and the LBA1 scenes its land comes from (those with a twelfth of it and more -- the plateau on
    // the mountain is a little over that of its cube -- the most first),
    // by their own names (SCENE1.HQD's, less "Polar Island, "; LBA1's SCENE.HQR has no first entry of sizes: its entry n is scene n).
    public static string NameOf(int cx, int cz, PolarTerrain.Result terrain)
    {
        var lba1 = HqdDescriptions.Load("SCENE1.HQD", 0).Names;
        var land = terrain.Columns.Where(c => !c.Value.Water && (c.Key.X + terrain.OffsetX) / 64 == cx && (c.Key.Z + terrain.OffsetZ) / 64 == cz).ToList();
        var parts = land.GroupBy(c => c.Value.Cell.Scene).Where(g => g.Key >= 0 && g.Count() * 12 >= land.Count).OrderByDescending(g => g.Count())
            .Select(g => g.Key < lba1.Count && lba1[g.Key] is { } n ? n.Replace("Polar Island, ", "").Trim() : $"scene {g.Key}")
            .Select(n => n.Length > 0 ? char.ToLowerInvariant(n[0]) + n[1..] : n).ToList();
        return "Polar Island: " + (parts.Count == 0 ? "the sea" : string.Join(", ", parts));
    }

    // Takes the scenes' names out of the folder's SCENE.HQD (lines left empty: the game's own names show again) -- the file itself when that
    // leaves nothing in it but the game's own descriptions (Install made it).
    public static void ForgetNames(string gameDirectory)
    {
        var path = Path.Combine(gameDirectory, HqdWriter.SidecarName("SCENE.HQR"));
        if (!File.Exists(path)) return;
        var lines = File.ReadAllLines(path, System.Text.Encoding.Latin1).ToList();
        for (var n = FirstScene; n < FirstScene + Count; n++)
            if (n + 2 < lines.Count) lines[n + 2] = "";
        while (lines.Count > 1 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);
        var reference = HqdDescriptions.LoadLines("SCENE2.HQD").ToList();
        while (reference.Count > 1 && reference[^1].Length == 0) reference.RemoveAt(reference.Count - 1);
        if (lines.SequenceEqual(reference)) { File.Delete(path); return; }
        File.WriteAllText(path, string.Join("\r\n", lines) + "\r\n", System.Text.Encoding.Latin1);
    }

    // The island's text file (TEXT.HQR, island 12's: TextEntry), in every language: its name, and the dream race's lines (PolarDream: the
    // race track's intro and its loss's line). Returns a line for the log.
    public static string WriteTexts(string gameDirectory)
    {
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrFile.Parse(File.ReadAllBytes(textPath));
        if (text.Count < Languages * RetailFiles * 2) throw new InvalidDataException($"TEXT.HQR has {text.Count} entries, fewer than the game's {Languages * RetailFiles * 2}.");
        for (var lang = 0; lang < Languages; lang++)
        {
            var (ids, data) = Bank(PolarDream.IslandTexts(lang).Prepend((0, "Polar Island")).ToList());
            foreach (var (k, payload) in new[] { (0, ids), (1, data) })
            {
                var at = TextEntry(lang) + k;
                while (text.Count < at) text.Slots.Add(new HqrFile.Slot());
                if (text.Count == at) text.Add(payload); else text.SetStored(at, payload);
            }
        }
        File.WriteAllBytes(textPath, text.ToBytes());
        return $"TEXT.HQR: island 12's text file, entries {TextEntry(0)}..{TextEntry(Languages - 1) + 1}: its name and the dream race's lines";
    }

    // A text file's two entries: the ids, then the offsets and texts (Lba2TextBank's layout; the game's code page, 850).
    private static (byte[] Ids, byte[] Data) Bank(IReadOnlyList<(int Id, string Text)> texts)
    {
        var ids = new byte[texts.Count * 2];
        var table = (texts.Count + 1) * 2;
        var body = new MemoryStream();
        var offsets = new List<int>();
        for (var i = 0; i < texts.Count; i++)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(ids.AsSpan(i * 2), (ushort)texts[i].Id);
            offsets.Add(table + (int)body.Length);
            body.WriteByte(Lba2TextBank.NormalAttribute);
            body.Write(PolarDream.Dos.GetBytes(texts[i].Text));
            body.WriteByte(0);
        }
        offsets.Add(table + (int)body.Length);
        var data = new byte[offsets[^1]];
        for (var i = 0; i < offsets.Count; i++) BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(i * 2), (ushort)offsets[i]);
        body.ToArray().CopyTo(data, table);
        return (ids, data);
    }
}
