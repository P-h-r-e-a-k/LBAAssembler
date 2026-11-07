using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using LBAAssembler.Characters;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
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
    // (Arrival: where a door of the game's into the slot lands Twinsen when the demo has no door from that place -- its own way in;
    // Pave: cells of the demo's grid paved over -- Paving; Wade: cells of its water made water Twinsen can stand in -- Wading)
    public sealed record Room(int DemoScene, int Slot, string Name, (int X, int Y, int Z)? Arrival = null, Paving? Pave = null, Wading? Wade = null);

    // The cells x X0-X1, z Z0-Z1 floored at `Layer` as the row z `Like` is there, cell for cell (its blocks and places), and emptied under
    // it down to `Under`
    public sealed record Paving(int X0, int X1, int Z0, int Z1, int Layer, int Like, int Under);

    // The cells x X0-X1, z Z0-Z1 at `Layer` drawn as they are, but water Twinsen can stand in (the game code CJ_FOOT_WATER, 12, for the
    // demo's water's 1): each block there copied once at the end of the room's library with that code, the cells set to the copy
    public sealed record Wading(int X0, int X1, int Z0, int Z1, int Layer);
    private const int FootWater = 12;

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

    // The Tralu's caves' third hall (the user, 2026-11-06: "replace scene '20: Citadel Island, Tralu 3rd scene (room #22)' with the one
    // from the 96 demo, this should be a like for like drop in replacement"): the demo's 20 in the game's slot 20 -- the same hall, its
    // own creatures and the lever that opens its door; its doors to the cave's first hall (2) and second (19) the game's, as the game's
    // doors from those halls land where the demo's do. (The game's own 20 has Joe the elf by the door to 19 in chapter 1, var 161, the
    // demo's hasn't: no story line uses him.)
    // The ladder up to the hall's back walk (the way on to the second hall, 19) stands in the demo in a pool of water a layer under the
    // floor (cells x 44-46, z 48-50, layer 4: the demo's water, 0xF1, the game's 0x10, as the demo's own drowning test reads it too).
    // It was paved over at first (Paving, 8d37014: walked into, Twinsen drowned). Now it is the demo's again, its bricks and its grid (the
    // user, 2026-11-06: "restore the water at the foot of the ladder", water "the player had to jump over"): the front row (z 50) the
    // demo's deadly water, walked into Twinsen drowns; but the engine's ladder isn't caught in the air, and a jump from the floor in
    // front lands in the pool's middle row (z 49, a cell short of the ladder) -- in the demo's water a drowning, and the bar's first way
    // in (this ladder, the back walk, the door to 19) shut for good. So the two rows at the ladder's foot (z 48-49) are the same water to
    // look at, a wading depth: jumped over the front row, he lands at the ladder and climbs it.
    public static readonly Room[] TraluCave =
    {
        new(20, 20, "the Tralu's caves' third hall", Wade: new Wading(44, 46, 48, 49, 4)),
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

    // Twinsen's generic animations, the demo's numbers as the game's. The rooms' Twinsen is the game's (entity 0, its animations), but the
    // demo's scripts name his animations by the demo's generic numbers, and from 25 on they aren't the game's: the game put in more of
    // them (the sabre and drawing it, 23 and 24, the gloves' punches, 36 and 37 ...) before the ones the two have alike. Matched
    // 2026-11-05 by the animations themselves -- the demo's entity 0 against the game's, keyframe by keyframe (their times, steps and
    // loop frame), most of them the same entry of ANIM.HQR in both (the demo's 37 is entry 24 as the game's 51 is, 38 entry 29 byte for
    // byte as 52 is). 0-22 are the same numbers. Read as the game's, the sewers' door from the Weather Wizard's house (scene 21 into 17,
    // zone 12) walked Twinsen in with the demo's 37, the stepping-through-a-door walk, which for the game's Twinsen is 37, a punch with
    // the left glove: it doesn't move him, so his goto_point(13) never got there and he punched on for ever; and the School hall's 34
    // (ANIM.HQR 14 in both, the game's 48) was the game's 34, the skates backwards.
    private static readonly Dictionary<int, int> HeroAnims = new()
    {
        [25] = 27, [27] = 29, [28] = 30, [29] = 31, [31] = 33, [32] = 34, [33] = 35,
        [34] = 48, [35] = 49, [36] = 50, [37] = 51, [38] = 52, [39] = 54, [40] = 55, [41] = 56, [42] = 57, [43] = 58,
    };
    // (Twinsen's own scripts: anim(n), anim_set(n), set_anim_dial(n), his anim() in a test; anyone's anim_obj(0, n) and anim_obj(0) tests)
    private static readonly Regex HeroAnimCall = new(@"\b(anim|anim_set|set_anim_dial)\((\d+)\)", RegexOptions.Compiled);
    private static readonly Regex HeroAnimTest = new(@"\b(\d+)(\s*(?:==|!=)\s*anim\(\))|\b(anim\(\)\s*(?:==|!=)\s*)(\d+)\b", RegexOptions.Compiled);
    private static readonly Regex HeroAnimObj = new(@"\b(anim_obj\(0,\s*)(\d+)\)", RegexOptions.Compiled);
    private static readonly Regex HeroAnimObjTest = new(@"\b(\d+)(\s*(?:==|!=)\s*anim_obj\(0\))|\b(anim_obj\(0\)\s*(?:==|!=)\s*)(\d+)\b", RegexOptions.Compiled);

    private static string HeroAnim(string n) => HeroAnims.TryGetValue(int.Parse(n), out var g) ? g.ToString() : n;

    // A script's text with Twinsen's animations the game's (HeroAnims): `hero` for his own scripts (actor 0's).
    public static string WithHeroAnims(string text, bool hero)
    {
        string Test(Match m) => m.Groups[1].Success ? HeroAnim(m.Groups[1].Value) + m.Groups[2].Value : m.Groups[3].Value + HeroAnim(m.Groups[4].Value);
        if (hero)
        {
            text = HeroAnimCall.Replace(text, m => $"{m.Groups[1].Value}({HeroAnim(m.Groups[2].Value)})");
            text = HeroAnimTest.Replace(text, Test);
        }
        text = HeroAnimObj.Replace(text, m => $"{m.Groups[1].Value}{HeroAnim(m.Groups[2].Value)})");
        return HeroAnimObjTest.Replace(text, Test);
    }

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

    // Whether a converted demo (or a package of it) has the rooms' scenes: a package made before a room was added to it hasn't
    public static bool Carries(string converted, IReadOnlyList<Room> rooms)
    {
        var store = new SceneStore(SceneGame.Lba2, converted);
        return rooms.All(r => store.SceneExists(r.DemoScene));
    }

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
            if (r.Pave is { } pave)
            {
                var shape = Lba2GridBackend.ToLba1Shape(grid);
                var edits = new List<Lba1GridCell>();
                for (var z = pave.Z0; z <= pave.Z1; z++)
                    for (var x = pave.X0; x <= pave.X1; x++)
                    {
                        var like = Lba1GridEdit.Get(shape, x, pave.Layer, pave.Like);
                        edits.Add(new Lba1GridCell(x, pave.Layer, z, like.Block, like.Pos));
                        for (var y = pave.Under; y < pave.Layer; y++) edits.Add(new Lba1GridCell(x, y, z, 0, 0));
                    }
                grid = Lba2GridBackend.FromLba1Shape(grid, Lba1GridEdit.SetCells(shape, edits));
                log.Add($"  scene {r.Slot}: cells x {pave.X0}-{pave.X1}, z {pave.Z0}-{pave.Z1} paved at layer {pave.Layer} as row z {pave.Like} is");
            }
            if (r.Wade is { } wade)
            {
                var head = new MapBricks.BkgHeader(bkg.Read(0));
                var lib = bkg.Read(head.Bll + library);
                var shape = Lba2GridBackend.ToLba1Shape(grid);
                var copies = new Dictionary<int, int>();
                var edits = new List<Lba1GridCell>();
                for (var z = wade.Z0; z <= wade.Z1; z++)
                    for (var x = wade.X0; x <= wade.X1; x++)
                    {
                        var cell = Lba1GridEdit.Get(shape, x, wade.Layer, z);
                        if (cell.Block == 0 || GridPaint.Info(lib, cell.Block) is not { } info) continue;
                        if (!copies.TryGetValue(cell.Block, out var copy))
                        {
                            var entries = GridPaint.Entries(lib, cell.Block).ToList();
                            lib = GridPaint.AppendBlock(lib, info.Dx, info.Dy, info.Dz, 0);
                            copy = GridPaint.BlockCount(lib);
                            foreach (var e in entries) lib = GridPaint.SetEntry(lib, copy, e.Pos, e.Brick, e.Shape, FootWater << 4 | e.Sound & 0x0F);
                            copies[cell.Block] = copy;
                        }
                        edits.Add(new Lba1GridCell(x, wade.Layer, z, copy, cell.Pos));
                    }
                grid = Lba2GridBackend.FromLba1Shape(grid, Lba1GridEdit.SetCells(shape, edits));
                bkg.SetStored(head.Bll + library, lib);
                if (head.MaxBll < (uint)lib.Length) { head.MaxBll = (uint)lib.Length; bkg.SetStored(0, head.Bytes); }
                log.Add($"  scene {r.Slot}: cells x {wade.X0}-{wade.X1}, z {wade.Z0}-{wade.Z1}, layer {wade.Layer}: water to wade in (blocks {string.Join(", ", copies.Select(c => $"{c.Key} as {c.Value}"))})");
            }
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
                    // (and Twinsen's animations the game's: HeroAnims)
                    mapped = WithHeroAnims(mapped, a == 0);
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
    // the converter's map) lands -- the nearest of them to the game's door -- and turns him as it does, else at the room's Arrival; the
    // rest of the door (its switch, whether it is on) the game's.
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
                    // (and the demo's turn with it: Info3 turns Twinsen's place in the zone round as he comes in -- OBJECT.CPP, the arrival is
                    // Info0-2 plus his offset from the zone's corner turned by -Info3 quarters -- and the demo's arrival is the corner for
                    // its own turn. The harbour's sewer lift, scene 48, turns half round in the game and not at all in the demo: with the
                    // demo's place and the game's turn Twinsen came out a cell and a half off the far halls' lift platform, standing on the
                    // wall between the gargoyles over the grate -- the user, 2026-11-06)
                    door.Info[3] = demo.Info[3];
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
