using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using LBAAssembler.Characters;
using LBAAssembler.Grids;
using LBAAssembler.LbaScript;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Demo96;

// The 1996 demo's rooms in the retail game's slots: the race mod's School of Magic is the demo's (the user, 2026-11-03: "The demo scenes
// for the school of magic, let's switch them in to our race mod in place of the retail school of magic scenes"). From a folder the
// converter made (Demo96Converter: the retail formats, the scripts in the retail bytecode), into a game folder:
//   the interior   the demo's grid in the slot's grid, drawn with the demo's block library -- a library of its own in LBA_BKG.HQR,
//                  its bricks the game's where they are the same (all but 52) and the rest added (MapBricks, as a map's interior goes in:
//                  a build again finds them there and adds nothing)
//   the people     the demo's characters (the Dean, his assistant, the students, the props) as entities of their own, their bodies and
//                  animations the demo's -- its scripts name them by the demo's generic numbers, and its skeletons aren't the game's --
//                  the hero (entity 0) and the Meca-penguin's slot (the demo's 9, the game's 14) the game's
//   the scene      the slot's header (its light, ambience, music) with the demo's actors, zones and track points; the doors to the demo's
//                  scenes go to the game's (the converter's map, the rooms to their slots), a door the demo shut that the slot's own scene
//                  has goes where the game has it (the hall's into the training room, scene 33); the samples the game's of the same
//                  bytes (Samples). Texts: the demo's School lines are the game's Desert texts under the same numbers, in every language.
internal static class Demo96Rooms
{
    // (Arrival: where a door of the game's into the slot lands Twinsen when the demo has no door from that place -- its own way in)
    public sealed record Room(int DemoScene, int Slot, string Name, (int X, int Y, int Z)? Arrival = null);

    public static readonly Room[] School =
    {
        new(27, 27, "the School of Magic's hall"),
        new(28, 28, "the School of Magic's entrance"),
    };

    // The Citadel Island sewers (the user, 2026-11-03: "Let's make sure we swap in the citadel sewers tomorrow"): the demo's 17 and 18,
    // the painter at work on the signs, in the game's sewers' slots. The game's doors into them land where the demo's own doors from
    // the same places do (the Tavern's cellar lift, scenes 21 and 34, the lift down from the harbour, 48, into the far halls); the
    // storm track's lift (scene 42), which the demo hasn't, lands where the demo starts Twinsen in them (on the cellar lift's
    // platform the demo's lift ride starts, and Twinsen, come any other way, fell from it and drowned).
    public static readonly Room[] Sewers =
    {
        new(17, 17, "the sewers", (20357, 1536, 15140)),
        new(18, 18, "the sewers' far halls"),
    };

    // The sewers' lines in the demo's Citadel texts, as the game's: Twinsen's greetings to the painter (the demo's 6, 100-102: the game's
    // 100 "Hello." and 101 "Hi."), asking after the lighthouse keeper (105, 106: the game's 520, 107), the painter's own two (236, 237: the
    // game's under the same numbers) and the broken device by the gate (383: the game's 110, "It's locked.").
    public static readonly Dictionary<int, int> SewerTexts = new() { [6] = 100, [102] = 101, [105] = 520, [106] = 107, [383] = 110 };

    // The demo's samples the rooms play, as the game's: 35, 41 and 54 byte for byte retail 617, 501 and 126; 122, which the demo plays
    // at the hall's flow points, as retail's hall does there (441). (118, the painter's splash into the sewer's water, is the demo's own:
    // no script of the game's plays that number.)
    private static readonly Dictionary<int, int> Samples = new() { [35] = 617, [41] = 501, [54] = 126, [122] = 441 };
    private static readonly Regex MessageCall = new(@"\b(message|message_zoe)\((\d+)\)|\b(message_obj)\((\d+), (\d+)\)", RegexOptions.Compiled);
    private const int DemoMecaPenguin = 9, MecaPenguin = 14;
    private static readonly Regex SampleCall = new(@"\b(sample|sample_rnd|sample_always|stop_sample|repeat_sample)\((\d+)", RegexOptions.Compiled);

    // The converted demo a build takes the rooms from: LBA2_DEMO96 (a converted folder, or the demo's own: converted into the scratch
    // folder, with what it lacks from `gameDirectory`), else the package the program carries (Pack: the repo's Demo96\rooms.zip, embedded
    // by LBAAssembler.csproj and LBRA's installer as PackageResource -- the user, 2026-11-03: "We should ship the demo school packaged
    // inside the installer"), unpacked into the scratch folder, else the editor's (File > Open the 1996 demo), or null.
    public static string? Source(string gameDirectory)
    {
        var editors = Path.Combine(DataPaths.LocalAppData, "LBAAssembler", "Demo96");
        var given = Environment.GetEnvironmentVariable("LBA2_DEMO96");
        if (given is { Length: > 0 } && Directory.Exists(given))
        {
            if (File.Exists(Path.Combine(given, Demo96Converter.MarkerFile))) return given;
            if (Demo96Converter.IsDemoFolder(given))
            {
                var into = Path.Combine(Path.GetTempPath(), "LBAAssembler-Demo96");
                if (!Demo96Converter.IsConverted(into, given)) Demo96Converter.Convert(given, into, gameDirectory);
                return into;
            }
        }
        if (Carried() is { } carried) return carried;
        return Demo96Converter.IsConverted(editors, Demo96Converter.SourceOf(editors) ?? "") ? editors : null;
    }

    public const string PackageResource = "Demo96/rooms.zip";

    // The package the program carries, unpacked into a scratch folder named by its contents' hash (unpacked once), or null.
    private static string? Carried()
    {
        using var stream = typeof(Demo96Rooms).Assembly.GetManifestResourceStream(PackageResource);
        if (stream is null) return null;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        var bytes = copy.ToArray();
        var into = Path.Combine(Path.GetTempPath(), "LBAAssembler-Demo96-" + Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(bytes))[..12]);
        if (File.Exists(Path.Combine(into, Demo96Converter.MarkerFile))) return into;
        var part = into + ".part";
        if (Directory.Exists(part)) Directory.Delete(part, true);
        using (var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes))) zip.ExtractToDirectory(part);
        if (Directory.Exists(into)) Directory.Delete(into, true);
        Directory.Move(part, into);
        return into;
    }

    // The rooms' package, made from a converted demo: the files the install reads (Install), each with only the entries the rooms take
    // -- their scenes, the interiors' grids, library and bricks, the characters' bodies and animations, the sprites, the entity table,
    // the palette -- the rest of each file's entries empty, so that the numbers stay the demo's; the converter's scene map beside them.
    public static List<string> Pack(string converted, string zipPath, IReadOnlyList<Room> rooms)
    {
        var keep = new Dictionary<string, HashSet<int>>
        {
            ["SCENE.HQR"] = new() { 0 }, ["LBA_BKG.HQR"] = new() { 0 }, ["RESS.HQR"] = new() { 0, 44 },
            ["BODY.HQR"] = new() { 0 }, ["ANIM.HQR"] = new() { 0 }, ["SPRITES.HQR"] = new() { 0 },
        };
        // (every file's entry 0 kept: a reader takes its offset, the first in the file, for the table's size -- HqrArchive)
        var store = new SceneStore(SceneGame.Lba2, converted);
        var backend = new Lba2GridBackend(converted);
        var bkg = HqrFile.Parse(File.ReadAllBytes(Path.Combine(converted, "LBA_BKG.HQR")));
        var header = bkg.Read(0);
        int gri = BinaryPrimitives.ReadUInt16LittleEndian(header), bll = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(4)),
            brk = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(6)), maxBrk = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(8));
        keep["LBA_BKG.HQR"].Add(brk + maxBrk);
        var demoTable = HqrArchive.Open(Path.Combine(converted, "RESS.HQR")).Read(44);
        // (the demo's scenes with doors into the rooms too: where those doors land is where the game's land -- Doors)
        for (var s = 0; s < store.SceneCount; s++)
            if (store.SceneExists(s) && store.Load(s).Zones.Any(z => z.Type == 0 && rooms.Any(r => r.DemoScene == z.Num))) keep["SCENE.HQR"].Add(s + 1);
        foreach (var r in rooms)
        {
            keep["SCENE.HQR"].Add(r.DemoScene + 1);
            var model = store.Load(r.DemoScene);
            if (model.Zones.Any(z => z.Type == 3)) throw new InvalidDataException($"The demo's scene {r.DemoScene} has grid fragments: the package doesn't carry them.");
            var grid = backend.GridOfScene(r.DemoScene) ?? throw new InvalidDataException($"The demo's scene {r.DemoScene} has no interior grid.");
            var raw = backend.RawGrid(grid);
            keep["LBA_BKG.HQR"].Add(gri + grid);
            keep["LBA_BKG.HQR"].Add(bll + raw[0]);
            var library = bkg.Read(bll + raw[0]);
            foreach (var p in MapBricks.BrickPositions(library))
                if (BinaryPrimitives.ReadUInt16LittleEndian(library.AsSpan(p)) is > 0 and var b && b - 1 < maxBrk) keep["LBA_BKG.HQR"].Add(brk + b - 1);
            foreach (var a in model.Actors.Skip(1))
            {
                if (a.IsSprite) { keep["SPRITES.HQR"].Add(a.Sprite); continue; }
                if (a.Entity <= 0 || a.Entity == DemoMecaPenguin) continue;
                MapCharacters.Renumber(MapCharacters.EntityOf(demoTable, a.Entity), b => { keep["BODY.HQR"].Add(b); return b; }, n => { keep["ANIM.HQR"].Add(n); return n; });
            }
        }
        var log = new List<string>();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(zipPath))!);
        using (var zip = new System.IO.Compression.ZipArchive(File.Create(zipPath), System.IO.Compression.ZipArchiveMode.Create))
        {
            foreach (var (name, entries) in keep)
            {
                var file = HqrFile.Parse(File.ReadAllBytes(Path.Combine(converted, name)));
                var trimmed = new HqrFile();
                for (var i = 0; i < file.Count; i++)
                    trimmed.Slots.Add(entries.Contains(i) && !file.IsEmpty(i) ? new HqrFile.Slot { Extent = file.ExtentOf(i) } : new HqrFile.Slot());
                var bytes = trimmed.ToBytes();
                using (var s = zip.CreateEntry(name, System.IO.Compression.CompressionLevel.SmallestSize).Open()) s.Write(bytes);
                log.Add($"{name}: {entries.Count} of {file.Count} entries, {bytes.Length} bytes");
            }
            zip.CreateEntryFromFile(Path.Combine(converted, Demo96Converter.MarkerFile), Demo96Converter.MarkerFile);
        }
        log.Add($"{zipPath}: {new FileInfo(zipPath).Length} bytes");
        return log;
    }

    public static List<string> Install(string gameDirectory, string converted, IReadOnlyList<Room> rooms, IReadOnlyDictionary<int, int>? texts = null)
    {
        var log = new List<string>();
        var demoStore = new SceneStore(SceneGame.Lba2, converted);
        var demoScenes = rooms.ToDictionary(r => r.DemoScene, r => demoStore.Load(r.DemoScene));
        // (the demo's scene numbers as the game's: the rooms their slots, the rest the converter's counterparts)
        var sceneOf = Demo96Converter.SceneMap(converted) ?? new Dictionary<int, int>();
        foreach (var r in rooms) sceneOf[r.DemoScene] = r.Slot;

        // ---- the people
        var entityOf = Characters(gameDirectory, converted, demoScenes.Values.SelectMany(m => m.Actors.Skip(1)).Select(a => a.Entity)
            .Where(e => e > 0 && e != DemoMecaPenguin).Distinct().Order().ToList(), log);
        entityOf[0] = 0; entityOf[DemoMecaPenguin] = MecaPenguin; entityOf[-1] = -1;

        // ---- the interiors
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var backend = new Lba2GridBackend(gameDirectory);
        var gridOf = rooms.ToDictionary(r => r.Slot, r => backend.GridOfScene(r.Slot) ?? throw new SceneEditException($"Scene {r.Slot} has no interior grid."));
        var oldFragments = rooms.ToDictionary(r => r.Slot, r => backend.RawGrid(gridOf[r.Slot])[1]);
        foreach (var r in rooms)
        {
            var map = MapInstall.ExportLba2Interior(converted, r.DemoScene, r.Name);
            var (library, fragments) = MapBricks.Install(map, bkg);
            var grid = (byte[])map.Grid!.Clone();
            grid[0] = (byte)library; grid[1] = map.Fragments is null ? oldFragments[r.Slot] : (byte)fragments;
            var header = bkg.Read(0);
            int gri = BinaryPrimitives.ReadUInt16LittleEndian(header);
            bkg.SetStored(gri + gridOf[r.Slot], grid);
            if (header.Length >= 16 && BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(12)) < (uint)grid.Length)
            { BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(12), (uint)grid.Length); bkg.SetStored(0, header); }
            log.Add($"{r.Name} (scene {r.Slot}): the demo's scene {r.DemoScene}'s interior in grid {gridOf[r.Slot]}, its block library LBA_BKG's library {library}");
        }

        // ---- the scenes
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var spriteOf = Sprites(converted, gameDirectory);
        foreach (var r in rooms)
        {
            var retail = store.Load(r.Slot);
            var demo = demoScenes[r.DemoScene];
            var model = retail.Clone();
            model.Actors.Clear(); model.Zones.Clear(); model.TrackPoints.Clear();
            model.Actors.AddRange(demo.Actors.Select(a => a.Clone()));
            model.TrackPoints.AddRange(demo.TrackPoints);
            foreach (var a in model.Actors.Skip(1))
            {
                if (a.IsSprite) a.Sprite = spriteOf.GetValueOrDefault(a.Sprite, a.Sprite);
                else a.Entity = entityOf.TryGetValue(a.Entity, out var e) ? e : throw new InvalidDataException($"The demo's entity {a.Entity} isn't brought over.");
            }
            foreach (var z in demo.Zones)
            {
                var zone = z.Clone();
                if (zone.Type == 0)
                {
                    var to = sceneOf.TryGetValue(zone.Num, out var s) ? s : -1;
                    // (a door the demo shut, X1 < X0, where the slot's own scene has one landing Twinsen at the same place: the game's --
                    // the hall's into the training room, which the demo numbers as another scene)
                    if (zone.X1 < zone.X0 && retail.Zones.FirstOrDefault(d => d.Type == 0 && d.X1 >= d.X0 && d.Info.Take(3).SequenceEqual(zone.Info.Take(3))) is { } open)
                    { zone = open.Clone(); log.Add($"  scene {r.Slot}: the door to scene {open.Num} the demo shut is the game's"); }
                    else if (to < 0) { log.Add($"  scene {r.Slot}: the door to the demo's scene {zone.Num} (none in the game) shut"); zone.X1 = zone.X0 - 1; }
                    else zone.Num = to;
                }
                // (a text zone's number a text too)
                if (zone.Type == 5 && texts is not null && texts.TryGetValue(zone.Num, out var said)) zone.Num = said;
                model.Zones.Add(zone);
            }
            var record = SceneSerializer.Write(model);
            // (the scripts: the samples the game's)
            var scripts = SceneScripts.Load(record, r.Slot);
            for (var a = 0; a < model.Actors.Count; a++)
                foreach (var kind in new[] { ScriptKind.Life, ScriptKind.Track })
                {
                    var text = scripts.GetText(a, kind);
                    var mapped = SampleCall.Replace(text, m => $"{m.Groups[1].Value}({(Samples.TryGetValue(int.Parse(m.Groups[2].Value), out var g) ? g : int.Parse(m.Groups[2].Value))}");
                    // (and the texts the game's, where the demo's numbers aren't: `texts`)
                    if (texts is not null)
                        mapped = MessageCall.Replace(mapped, m => m.Groups[1].Success
                            ? $"{m.Groups[1].Value}({texts.GetValueOrDefault(int.Parse(m.Groups[2].Value), int.Parse(m.Groups[2].Value))})"
                            : $"{m.Groups[3].Value}({m.Groups[4].Value}, {texts.GetValueOrDefault(int.Parse(m.Groups[5].Value), int.Parse(m.Groups[5].Value))})");
                    if (mapped != text) scripts.SetText(a, kind, mapped);
                }
            var built = scripts.Build();
            if (!built.Ok) throw new InvalidDataException($"The demo's scene {r.DemoScene}'s scripts don't compile again: {string.Join("; ", built.Errors.Take(3).Select(e => e.ToString()))}");
            PutScene(scenes, r.Slot, built.Record!);
            log.Add($"{r.Name} (scene {r.Slot}): the demo's scene {r.DemoScene}: {model.Actors.Count - 1} actors, {model.Zones.Count} zones, its scripts");
        }
        Doors(scenes, store, demoStore, sceneOf, rooms, log);
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        return log;
    }

    // The game's doors into the rooms from its other scenes: each lands where the demo's door from the same scene (its counterpart,
    // the converter's map) lands -- the nearest of them to the game's door -- else at the room's Arrival; the rest of the door (its
    // switch, whether it is on) the game's.
    private static void Doors(HqrFile scenes, SceneStore store, SceneStore demoStore, Dictionary<int, int> sceneOf, IReadOnlyList<Room> rooms, List<string> log)
    {
        var slots = rooms.ToDictionary(r => r.Slot);
        var demoOf = sceneOf.Where(p => !rooms.Any(r => r.DemoScene == p.Key)).GroupBy(p => p.Value).ToDictionary(g => g.Key, g => g.First().Key);
        for (var s = 0; s < store.SceneCount; s++)
        {
            if (slots.ContainsKey(s) || !store.SceneExists(s)) continue;
            SceneModel model;
            try { model = SceneSerializer.Parse(SceneGame.Lba2, scenes.Read(s + 1)); } catch (Exception e) when (e is InvalidDataException or ArgumentException) { continue; }
            var doors = model.Zones.Where(z => z.Type == 0 && slots.ContainsKey(z.Num)).ToList();
            if (doors.Count == 0) continue;
            var theirs = demoOf.TryGetValue(s, out var d) && demoStore.SceneExists(d)
                ? demoStore.Load(d).Zones.Where(z => z.Type == 0 && rooms.Any(r => r.DemoScene == z.Num)).ToList() : new List<SceneZoneModel>();
            foreach (var door in doors)
            {
                double Far(SceneZoneModel z) => Math.Abs((z.X0 + z.X1) / 2.0 - (door.X0 + door.X1) / 2.0) + Math.Abs((z.Z0 + z.Z1) / 2.0 - (door.Z0 + door.Z1) / 2.0);
                var was = $"scene {door.Num} at ({door.Info[0]},{door.Info[1]},{door.Info[2]})";
                if (theirs.OrderBy(Far).FirstOrDefault() is { } demo)
                {
                    door.Num = rooms.First(r => r.DemoScene == demo.Num).Slot;
                    for (var k = 0; k < 3; k++) door.Info[k] = demo.Info[k];
                }
                else if (slots[door.Num].Arrival is { } at) { door.Info[0] = at.X; door.Info[1] = at.Y; door.Info[2] = at.Z; }
                else { log.Add($"  scene {s}: its door to {was} has no counterpart in the demo -- left"); continue; }
                log.Add($"  scene {s}: its door to {was} now to scene {door.Num} at ({door.Info[0]},{door.Info[1]},{door.Info[2]})");
            }
            PutScene(scenes, s, SceneSerializer.Write(model));
        }
    }

    // The demo's characters (its entities: their records as they are, the bodies and animations they name added at the ends of BODY.HQR
    // and ANIM.HQR) into the game's entity table. Returns the demo's entity -> the game's.
    private static Dictionary<int, int> Characters(string gameDirectory, string converted, IReadOnlyList<int> entities, List<string> log)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var bodyFile = File.ReadAllBytes(bodyPath);
        var animFile = File.ReadAllBytes(animPath);
        var ress = File.ReadAllBytes(ressPath);
        var table = HqrArchive.FromBytes(ress).Read(44);
        int bodyCount = HqrArchive.FromBytes(bodyFile).Slots - 1, animCount = HqrArchive.FromBytes(animFile).Slots - 1;
        var demoBodies = HqrArchive.Open(Path.Combine(converted, "BODY.HQR"));
        var demoAnims = HqrArchive.Open(Path.Combine(converted, "ANIM.HQR"));
        var demoTable = HqrArchive.Open(Path.Combine(converted, "RESS.HQR")).Read(44);
        var bodyOf = new Dictionary<int, int>(); var animOf = new Dictionary<int, int>();
        var result = new Dictionary<int, int>();
        foreach (var e in entities)
        {
            var record = MapCharacters.EntityOf(demoTable, e);
            int Body(int entry)
            {
                if (!bodyOf.TryGetValue(entry, out var n)) { n = bodyOf[entry] = bodyCount++; bodyFile = HqrWriter.AppendEntry(bodyFile, HqrWriter.StoredEntry(demoBodies.Read(entry))); }
                return n;
            }
            int Anim(int entry)
            {
                if (!animOf.TryGetValue(entry, out var n)) { n = animOf[entry] = animCount++; animFile = HqrWriter.AppendEntry(animFile, HqrWriter.StoredEntry(demoAnims.Read(entry))); }
                return n;
            }
            (table, result[e]) = MapCharacters.WithEntity(table, -1, MapCharacters.Renumber(record, Body, Anim));
        }
        new FileTransaction()
            .Write(bodyPath, bodyFile)
            .Write(animPath, animFile)
            .Write(ressPath, HqrWriter.ReplaceEntry(ress, 44, HqrWriter.StoredEntry(table)))
            .Commit();
        log.Add($"the demo's characters {string.Join(", ", result.Select(p => $"{p.Key} -> entity {p.Value}"))}: {bodyOf.Count} bodies, {animOf.Count} animations");
        return result;
    }

    // The demo's sprites as the game's: the same number where the pictures are the same, else the game's of the same bytes.
    private static Dictionary<int, int> Sprites(string converted, string gameDirectory)
    {
        var demo = HqrArchive.Open(Path.Combine(converted, "SPRITES.HQR"));
        var game = HqrArchive.Open(Path.Combine(gameDirectory, "SPRITES.HQR"));
        var byBytes = new Dictionary<string, int>();
        for (var i = 0; i < game.Slots - 1; i++) if (game.IsValid(i)) byBytes.TryAdd(Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(game.Read(i))), i);
        var map = new Dictionary<int, int>();
        for (var i = 0; i < demo.Slots - 1; i++)
        {
            if (!demo.IsValid(i)) continue;
            var bytes = demo.Read(i);
            if (i < game.Slots - 1 && game.IsValid(i) && game.Read(i).AsSpan().SequenceEqual(bytes)) continue;
            if (byBytes.TryGetValue(Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(bytes)), out var g)) map[i] = g;
        }
        return map;
    }

    private static void PutScene(HqrFile scenes, int number, byte[] record)
    {
        scenes.SetStored(number + 1, record);
        var largest = BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0));
        if (record.Length > largest) { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, record.Length); scenes.SetStored(0, size); }
    }
}
