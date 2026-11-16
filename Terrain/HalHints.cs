using System.IO;

namespace LBAAssembler.Terrain;

// HAL on the radio (the user, 2026-11-16: "Adding HAL as an option to call with our phone, calling HAL should give a hint for hidden areas
// on the players current island"): the engine's HALHINTS.TXT (RACEMOD.CPP HalCall) -- the portable radio used with nobody's call to answer
// calls HAL, who gives the next hint for the island Twinsen is on whose hidden place the player hasn't found (its achievement not earned:
// RACEGAME.CPP's list), else any island's; before he's mended on the Emerald Moon (the story's race file's hal_mended=, Q_HAL) he only
// crackles. Lines: "hal <island file without .ILE, or *> <achievement id, or - for none> <his words>".
internal static class HalHints
{
    public const string File = "HALHINTS.TXT";
    private static string Stem(RaceTrackIsland island) => Path.GetFileNameWithoutExtension(island.IleFile).ToLowerInvariant();

    public static IEnumerable<string> Lines() => new[]
    {
        ($"{Stem(RaceTrackIsland.Citadel)}", "telescope", "HAL here. There's a telescope out by the lighthouse on Citadel Island. Have a look through it: a wizard is up to something with a cow. HAL!!"),
        ($"{Stem(RaceTrackIsland.Citadel)}", "spanked", "HAL here. Drinking is banned on Citadel, but the Tralu's bar in the caves opens in the evening. Zoe will not be pleased. HAL!!"),
        ($"{Stem(RaceTrackIsland.CitadelStorm)}", "telescope", "HAL here. There's a telescope out by the lighthouse. Have a look through it: a wizard is up to something with a cow. HAL!!"),
        ($"{Stem(RaceTrackIsland.Emerald)}", "stella", "HAL here. Baldino's old cell has a corner, left of its door, that goes further down than it looks. Something glitters under the reactor. HAL!!"),
        ($"{Stem(RaceTrackIsland.Otringal)}", "bowling", "HAL here. The square painted on the platform by the prison's way out isn't just paint. Stand on it and press Action. Bring your own ball. HAL!!"),
        ($"{Stem(RaceTrackIsland.Otringal)}", "-", "HAL here. The Imperial Hotel has a pool with a diving board, and Lyra keeps score of the dives. Mind you can't swim. HAL!!"),
        ($"{Stem(RaceTrackIsland.Polar)}", "dinofly", "HAL here. An old friend of yours is out there in the snow. He'd like a word. HAL!!"),
        ($"{Stem(RaceTrackIsland.Palace)}", "dungeon", "HAL here. The palace goes deeper than the Emperor lets on. Find a way down under its rooms to the dungeon. HAL!!"),
        ("*", "postcards", "HAL here. There's a postcard hidden on every island the Cup visits. Find them all and someone will be very grateful. HAL!!"),
    }.DistinctBy(h => (h.Item1, h.Item2)).Select(h => $"hal {h.Item1} {h.Item2} {h.Item3}");   // (the storm's Citadel is Citadel's file)

    public static string Install(string gameDirectory)
    {
        var lines = Lines().ToList();
        System.IO.File.WriteAllLines(Path.Combine(gameDirectory, File), lines);
        return $"HAL on the radio: {lines.Count} hints ({File})";
    }
}
