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
        if (island.NotShareable is { } why) throw new InvalidDataException(why + ".");
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
        if (island.NotShareable is { } why) throw new InvalidDataException($"{name}: {why}.");
        if (o["plan"] is not JsonObject plan) throw new InvalidDataException($"{name} has no plan in it.");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(plan.ToJsonString()));
        return new Track(name, island, RaceTrackPlan.Read(stream));
    }

    // A shared track's copy of its island (RaceTrackIsland.Shared) made in the folder: the island's ground and decor bodies as the game has
    // them, under the copy's names. The build then puts the track on them, as on the island's own.
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
        return $"{shared.Name}: {shared.IleFile} and {shared.OblFile}, copies of {ile} and {obl} as the game has them, raced as island {shared.IslandByte} drawn from them";
    }
}
