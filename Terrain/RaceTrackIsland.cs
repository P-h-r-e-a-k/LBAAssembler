namespace LBAAssembler.Terrain;

// Which island a race track is built on, and what differs between islands: the files it changes, the scenes it edits, and the ground
// textures and colours the road is painted with (every island has its own texture page and palette, so the Desert track's own tile
// coordinates draw something else on another island -- RaceTrackTextures copies the road's tiles into the island's spare texture space
// and gives back where they landed).
internal sealed record RaceTrackIsland(
    string Name,                    // as the dialog and the log call it
    string IleFile,                 // the island's ground
    string OblFile,                 // its decor bodies (the bridge deck's are added here)
    int IslandByte,                 // what a scene's own island byte says (SceneModel.Island)
    int FirstScene, int LastScene,  // its outside scenes (the demo copies, 190 and up, are left alone)
    int PaletteEntry,               // RESS.HQR: the palette its ground is drawn with
    string PlanResource,            // the route built into the program
    (int X, int Z)? OldTrackCube)   // a retail race track to clear first (the Desert island's own)
{
    // The Desert island: the retail race track's own island, and the first track built.
    public static readonly RaceTrackIsland Desert = new("Desert island", "DESERT.ILE", "DESERT.OBL", 2, 55, 73, 29,
        "RaceTrackPlan.Desert.json", (7, 10));

    // Citadel Island: a town circuit, no retail track to clear, and no buggy in its scenes (the build puts one on the grid). Once the storm
    // is over (chapter 2, after the lighthouse) the engine draws it from CITABAU instead -- the same ground with its own light, palette and
    // decor bodies -- so the track is built into both, or it would vanish when the rain stops.
    public static readonly RaceTrackIsland Citadel = new("Citadel Island", "CITADEL.ILE", "CITADEL.OBL", 0, 42, 50, 27,
        "RaceTrackPlan.Citadel.json", null) { TwinIleFile = "CITABAU.ILE", TwinOblFile = "CITABAU.OBL" };

    // Mosquibees Island: a mountain lap from a drawing -- two loops winding up round the Mosquibees' mountain from the shore, a bridge
    // from its top over the first loop and the channel to the plateau's ridge, a jump over the plateau's west bay, the start line on its
    // north edge, and the long way down its east side. The plan carries its own heights, bridge and jump (RaceTrackPlan.Heights). Its
    // outside scenes are 102, 103 and 105; 104 between them is the Queen's throne, an inside scene.
    public static readonly RaceTrackIsland Mosquibe = new("Mosquibees Island", "MOSQUIBE.ILE", "MOSQUIBE.OBL", 7, 102, 105, 34,
        "RaceTrackPlan.Mosquibe.json", null);

    // The island's other file for other weather (EXTFUNC.CPP loads it instead once the storm is over), built with the same track.
    public string? TwinIleFile { get; init; }
    public string? TwinOblFile { get; init; }

    public static readonly RaceTrackIsland[] All = { Desert, Citadel, Mosquibe };

    public static RaceTrackIsland ByName(string name) => All.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)) ?? Desert;

    // The files a build of this island changes, besides the ones every build does (SCENE.HQR, and BODY.HQR / ANIM.HQR / RESS.HQR for the
    // cars and the jump).
    public string[] IslandFiles => TwinIleFile is { } ti && TwinOblFile is { } to ? new[] { IleFile, OblFile, ti, to } : new[] { IleFile, OblFile };
}
