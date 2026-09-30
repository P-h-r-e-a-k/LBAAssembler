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
    // The Desert island: the retail race track's own island, and the first track built. Its lap jumps over itself.
    public static readonly RaceTrackIsland Desert = new("Desert island", "DESERT.ILE", "DESERT.OBL", 2, 55, 73, 29,
        "RaceTrackPlan.Desert.json", (7, 10))
    {
        Crossing = CrossingStyle.Jump,
    };

    // Citadel Island: two tracks, one for each of its files, no retail track to clear, and no buggy in its scenes (the build puts one at each
    // start line). The storm's file, CITADEL, carries a short lap round the town -- along the west rampart and off its end over a jump,
    // round the harbour and back up through a double hairpin -- with no opponents yet. Once the storm is over (chapter 2, after the
    // lighthouse; in Play, the car setup's fine weather) the engine draws the island from CITABAU instead -- the same ground with its own
    // light, palette and decor bodies -- and that carries the town circuit, with its opponents and the story. The two share the island's
    // scenes (RaceTrackScenes applies both), so the island is built with both whichever one is chosen: the two entries, the town circuit
    // and the storm track, differ only in which one Play races (RacesTwin) -- and with it the weather it plays in -- when the editor has
    // neither file open (the race track window lists the island once, as this entry). The town circuit crosses itself on a bridge.
    public static readonly RaceTrackIsland Citadel = new("Citadel Island", "CITADEL.ILE", "CITADEL.OBL", 0, 42, 50, 27,
        "RaceTrackPlan.CitadelStorm.json", null)
    {
        TwinIleFile = "CITABAU.ILE", TwinOblFile = "CITABAU.OBL", TwinPlanResource = "RaceTrackPlan.Citadel.json", RacesTwin = true,
        Title = "Citadel Island: the town circuit (once the storm is over)",
    };

    public static readonly RaceTrackIsland CitadelStorm = Citadel with
    {
        Name = "Citadel Island, storm track", RacesTwin = false, Title = "Citadel Island: the storm track (in the rain)",
    };

    // Mosquibees Island: a mountain lap from a drawing -- two loops winding up round the Mosquibees' mountain from the shore, a bridge
    // from its top over the first loop and the channel to the plateau's ridge, a jump over the plateau's west bay, the start line on its
    // north edge, and the long way down its east side. The plan carries its own heights, bridge and jump (RaceTrackPlan.Heights). Its
    // outside scenes are 102, 103 and 105; 104 between them is the Queen's throne, an inside scene.
    public static readonly RaceTrackIsland Mosquibe = new("Mosquibees Island", "MOSQUIBE.ILE", "MOSQUIBE.OBL", 7, 102, 105, 34,
        "RaceTrackPlan.Mosquibe.json", null);

    // Celebration Island with the statue (CELEBRA2: the file the engine draws once the statue has risen, game variable 79; before that
    // it is CELEBRAT, the same island with its lava lake empty): from the dock where the taxi lands, round the island and up round the
    // statue in a spiral to its shoulders, a ring round its head, and a bridge straight back down to the dock. All of it but the dock is
    // a raised road on piers (RaceTrackPlan.Raised), which only the engine's race-track mode can drive; that mode draws the island with
    // the statue whatever the game variable says (Statue). One outside scene, 95, and one cube.
    public static readonly RaceTrackIsland Celebration = new("Celebration Island", "CELEBRA2.ILE", "CELEBRA2.OBL", 5, 95, 95, 32,
        "RaceTrackPlan.Celebration.json", null)
    {
        Statue = true, Title = "Celebration Island: round the statue",
    };

    // The Elevator Platform (ASCENCE: the foot of the elevator from Otringal, a few decks on legs over the sea with the elevator's tower
    // in the middle and an airship moored beside it -- 28 cells by 24, all of it decor, the ground under it nothing but sea). Too small
    // for a road on it, so the lap is built upwards, a rollercoaster: all of it a raised road (RaceTrackPlan.Raised, the whole lap),
    // banked in its bends (Bank), from a station in front of the elevator's door up a helix round the tower, down a drop, over hills and
    // the airship and back. Only the engine's race-track mode can drive it, and there the grade changes the car's speed (Gravity).
    // One outside scene, 120, and one cube.
    public static readonly RaceTrackIsland Elevator = new("Elevator Platform", "ASCENCE.ILE", "ASCENCE.OBL", 10, 120, 120, 37,
        "RaceTrackPlan.Elevator.json", null)
    {
        Title = "The Elevator Platform: the rollercoaster",
    };

    // What carries the lap over itself where its plan doesn't draw that itself (RaceTrackPlan.Planned: Mosquibees Island's, Celebration
    // Island's, the Elevator Platform's and Citadel Island's storm track draw their own): the Desert island's jumps, Citadel Island's town
    // circuit (its twin's plan) has a bridge.
    public CrossingStyle Crossing { get; init; } = CrossingStyle.Bridge;

    // The island's other file for other weather (EXTFUNC.CPP loads it instead once the storm is over), built with the same track -- or,
    // with TwinPlanResource, with a track of its own (the route built into the program for it).
    public string? TwinIleFile { get; init; }
    public string? TwinOblFile { get; init; }
    public string? TwinPlanResource { get; init; }
    // For an island with a track in each file: whether Play races the twin's (Citadel Island's town circuit, in fine weather) or the
    // island's own file's (its storm track, in the rain).
    public bool RacesTwin { get; init; }
    // As the race track window lists it (the name, unless the island has two entries).
    public string? Title { get; init; }
    // The track is in the file the engine draws only while game variable 79 is set (Celebration Island's statue): the race-track mode
    // is told to draw that one (the car file's statue=1).
    public bool Statue { get; init; }
    public string Shown => Title ?? Name;
    // How many tracks a build of the island makes: Citadel Island's two files each carry one (its storm track and its town circuit).
    public int Tracks => TwinPlanResource is not null ? 2 : 1;
    // As a list of the tracks built names it.
    public string Built => TwinPlanResource is not null ? $"{Name.Split(',')[0]} (the storm track and the town circuit)" : Name;

    public static readonly RaceTrackIsland[] All = { Desert, Citadel, CitadelStorm, Mosquibe, Celebration, Elevator };

    public static RaceTrackIsland ByName(string name) => All.FirstOrDefault(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)) ?? Desert;

    // The files a build of this island changes, besides the ones every build does (SCENE.HQR, and BODY.HQR / ANIM.HQR / RESS.HQR for the
    // cars and the jump).
    public string[] IslandFiles => TwinIleFile is { } ti && TwinOblFile is { } to ? new[] { IleFile, OblFile, ti, to } : new[] { IleFile, OblFile };
}
