using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LBAAssembler.Terrain;

// A race track shared (the user, 2026-10-31: "LBAAssembler allows a user to create custom tracks. Let's support them exporting a track,
// sharing it, then loading a track into LBRA possibly for them to race a friend"): a .lbrtrack file, made by the race track window's
// "Share for LBRA..." from a plan file and the island it is drawn on. It holds the track's name, the island's (RaceTrackIsland.Name) and
// the plan as a plan file has it -- the route, nothing of the game's: the race game's setup builds the track from the player's own copy
// of the game, on a copy of the island (RaceTrackIsland.Shared), beside the island's own track.
internal static class RaceTrackShare
{
    public const string Extension = ".lbrtrack";
    // (what the file says it is, and the newest layout this reads)
    public const int Format = 1;
    public const int MaxName = 40;

    public sealed record Track(string Name, RaceTrackIsland Island, RaceTrackPlan Plan);

    private static readonly JsonDocumentOptions Reading = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    // The plan file at `planPath`, drawn on `island`, saved as a shared track named `name` at `path`
    public static void Write(string path, string name, RaceTrackIsland island, string planPath)
    {
        name = name.Trim();
        if (name.Length == 0) throw new InvalidDataException("The track needs a name.");
        if (name.Length > MaxName) throw new InvalidDataException($"The name is longer than {MaxName} letters.");
        var text = File.ReadAllText(planPath);
        // (a plan that reads as one: the same check the build makes)
        using (var check = new MemoryStream(Encoding.UTF8.GetBytes(text))) RaceTrackPlan.Read(check);
        var plan = JsonNode.Parse(text, documentOptions: Reading) ?? throw new InvalidDataException("The plan is empty.");
        var made = typeof(RaceTrackShare).Assembly.GetName().Version?.ToString() ?? "";
        var file = new JsonObject
        {
            ["lbrtrack"] = Format,
            ["name"] = name,
            ["island"] = island.Name,
            ["made"] = $"LBA Assembler {made}".Trim(),
            ["plan"] = plan,
        };
        File.WriteAllText(path, file.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
    }

    // A shared track's file read back: its name, the game's island it is drawn on and its plan. InvalidDataException: not one this reads.
    public static Track Read(string path)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(File.ReadAllText(path), documentOptions: Reading); }
        catch (JsonException error) { throw new InvalidDataException($"{Path.GetFileName(path)} isn't a shared track (it doesn't read as one).", error); }
        if (root is not JsonObject o || o["lbrtrack"] is not JsonValue v || !v.TryGetValue<int>(out var format))
            throw new InvalidDataException($"{Path.GetFileName(path)} isn't a shared track.");
        if (format > Format) throw new InvalidDataException($"{Path.GetFileName(path)} was made by a newer LBA Assembler: update the game to race it.");
        var name = (o["name"] as JsonValue)?.TryGetValue<string>(out var n) == true ? n.Trim() : "";
        if (name.Length == 0) throw new InvalidDataException($"{Path.GetFileName(path)} has no name for its track.");
        if (name.Length > MaxName) name = name[..MaxName].TrimEnd();
        var islandName = (o["island"] as JsonValue)?.TryGetValue<string>(out var i) == true ? i : "";
        var island = RaceTrackIsland.BuiltIn.FirstOrDefault(b => string.Equals(b.Name, islandName, StringComparison.OrdinalIgnoreCase))
                     ?? throw new InvalidDataException($"{name} is drawn on an island this game doesn't know: \"{islandName}\".");
        if (o["plan"] is not JsonObject plan) throw new InvalidDataException($"{name} has no plan in it.");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(plan.ToJsonString()));
        return new Track(name, island, RaceTrackPlan.Read(stream));
    }

    // A shared track's copy of its island (RaceTrackIsland.Shared) made in the folder: the island's ground and decor bodies, and its outside
    // scenes, as they are before any track goes on -- the game's own (the originals the build keeps), or those the build has just made
    // (RaceTrackService.Build calls this after making its islands, before building any track) -- under the copy's names and numbers, the
    // copies' cube changes to one another pointed at one another, Twinsen's own scripts emptied (a story's scenes would take him out of his
    // car). The build then puts the track on them as on the island's own: making them bigger, giving cubes scenes.
    // A map's island's copy (RaceTrackIsland.Custom) made in the folder: its ground and decor bodies, and its outside scenes from the map
    // itself -- the map's own scene numbers (LbaMap.LocalScene + k) its cube changes give pointed at the copies', Twinsen's own scripts
    // emptied -- under the copy's names and numbers, before any track is built.
    public static string InstallCustom(string gameDirectory, RaceTrackIsland custom)
    {
        var map = custom.CustomMap ?? throw new ArgumentException($"{custom.Name} isn't a map's island.");
        if (map.Ile is null || map.Obl is null) throw new InvalidDataException($"The map \"{map.Name}\" has no island files.");
        foreach (var (bytes, copy) in new[] { (map.Ile, custom.IleFile), (map.Obl, custom.OblFile) })
        {
            var target = Path.Combine(gameDirectory, copy);
            if (File.Exists(target)) File.SetAttributes(target, FileAttributes.Normal);
            File.WriteAllBytes(target, bytes);
        }
        var pairs = custom.SharedScenes ?? Array.Empty<(int From, int To)>();
        var copyOf = pairs.ToDictionary(p => p.From, p => p.To);
        var path = Path.Combine(gameDirectory, "SCENE.HQR");
        var hqr = HqrFile.Parse(File.ReadAllBytes(path));
        var retargeted = 0;
        for (var k = 0; k < pairs.Length && k < map.Scenes.Count; k++)
        {
            var entry = pairs[k].To + 1;
            if (entry < hqr.Count && !hqr.IsEmpty(entry)) throw new InvalidDataException($"The game already has a scene {pairs[k].To}: the map's island would go there.");
            var model = LBAAssembler.Scenes.SceneSerializer.Parse(LBAAssembler.Scenes.SceneGame.Lba2, map.Scenes[k].Record);
            foreach (var zone in model.Zones.Where(z => z.Type == 0 && copyOf.ContainsKey(z.Num))) { zone.Num = copyOf[zone.Num]; retargeted++; }
            model.Hero.Life = new byte[] { 0 };
            var record = LBAAssembler.Scenes.SceneSerializer.Write(model);
            while (hqr.Count < entry) hqr.Slots.Add(new HqrFile.Slot());
            if (hqr.Count == entry) hqr.Add(record); else hqr.SetStored(entry, record);
            if (record.Length > System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(hqr.Read(0)))
            {
                var size = new byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(size, record.Length);
                hqr.SetStored(0, size);
            }
        }
        File.WriteAllBytes(path, hqr.ToBytes());
        return $"{custom.Name}: {custom.IleFile} and {custom.OblFile}, the map's island as it is, raced as island {custom.IslandByte}; scenes {custom.FirstScene}-{custom.LastScene}, " +
               $"the map's {map.Scenes.Count} ({retargeted} cube changes between them pointed at them)";
    }

    public static string Install(string gameDirectory, RaceTrackIsland shared)
    {
        var from = shared.SharedFrom ?? throw new ArgumentException($"{shared.Name} isn't a shared track's island.");
        var (ile, obl) = from.SharedSource;
        foreach (var (source, copy) in new[] { (ile, shared.IleFile), (obl, shared.OblFile) })
        {
            var target = Path.Combine(gameDirectory, copy);
            File.Copy(RaceTrackService.OriginalPath(gameDirectory, source), target, true);
            File.SetAttributes(target, FileAttributes.Normal);
        }
        var pairs = shared.SharedScenes ?? Array.Empty<(int From, int To)>();
        var path = Path.Combine(gameDirectory, "SCENE.HQR");
        var hqr = HqrFile.Parse(File.ReadAllBytes(path));
        var copyOf = pairs.ToDictionary(p => p.From, p => p.To);
        var retargeted = 0;
        var made = new List<(int From, int To)>();
        foreach (var (from1, to) in pairs)
        {
            var entry = to + 1;
            if (entry < hqr.Count && !hqr.IsEmpty(entry)) throw new InvalidDataException($"The game already has a scene {to}: {shared.Name} would copy scene {from1} there.");
            // (Sendell's Well's scene is made as its track is built, not with the island: the copy gets one of its own the same way)
            if (from1 + 1 >= hqr.Count || hqr.IsEmpty(from1 + 1))
            {
                if (from != RaceTrackIsland.Sendell) throw new InvalidDataException($"{from.Shown} has no scene {from1} to copy.");
                made.Add((from1, to));
                continue;
            }
            var model = LBAAssembler.Scenes.SceneSerializer.Parse(LBAAssembler.Scenes.SceneGame.Lba2, hqr.Read(from1 + 1));
            foreach (var zone in model.Zones.Where(z => z.Type == 0 && copyOf.ContainsKey(z.Num))) { zone.Num = copyOf[zone.Num]; retargeted++; }
            model.Hero.Life = new byte[] { 0 };
            var record = LBAAssembler.Scenes.SceneSerializer.Write(model);
            while (hqr.Count < entry) hqr.Slots.Add(new HqrFile.Slot());
            if (hqr.Count == entry) hqr.Add(record); else hqr.SetStored(entry, record);
            if (record.Length > System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(hqr.Read(0)))
            {
                var size = new byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(size, record.Length);
                hqr.SetStored(0, size);
            }
        }
        File.WriteAllBytes(path, hqr.ToBytes());
        foreach (var (_, to) in made) SendellWell.AddScene(gameDirectory, to, holomap: false);
        return $"{shared.Name}: {shared.IleFile} and {shared.OblFile}, copies of {ile} and {obl} as the build has them before its track, raced as island {shared.IslandByte} drawn from them; " +
               $"scenes {shared.FirstScene}-{shared.LastScene}, copies of {string.Join(",", pairs.Select(p => p.From))} ({retargeted} cube changes between them pointed at the copies)" +
               (shared.Scale > 1 ? $", made {shared.Scale} times bigger with the island (scenes {string.Join(",", shared.MoreScenes)} besides)" : "") +
               (shared.NewCubes.Length > 0 ? $", scenes {string.Join(",", shared.NewCubes.Select(c => c.Scene))} for the cubes the game has none for" : "");
    }
}
