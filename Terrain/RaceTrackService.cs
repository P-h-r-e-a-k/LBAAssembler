using System.IO;
using System.Text.Json;

namespace LBAAssembler.Terrain;

// Builds an island's race track (RaceTrackIsland) into an LBA2 game folder, and puts the folder back. The files it changes are kept beside the originals as
// *.before-racetrack the first time; every later build starts from those copies, so building again never piles a track on a track, and Restore
// puts them back. DESERT.ILE, DESERT.OBL and SCENE.HQR always change; ANIM.HQR and RESS.HQR for a jump (its flight, RaceTrackJumpAnim), BODY.HQR
// and RESS.HQR for Baldino's car (RaceTrackBaldinoCar); they are kept from the first build on too (a folder built before one of them changed keeps
// it from its next build on, while it is still the original), so every build starts from the originals. RACETRACK.JSON, beside them, tells Play
// where the start line, the checkpoints and the opponents' lines are (what the engine's race-track mode uses).
internal static class RaceTrackService
{
    public const string BackupSuffix = ".before-racetrack";
    // What every build changes, whichever island it is on; the island's own ground and decor bodies are added to these (RaceTrackIsland).
    public static readonly string[] Files = { "SCENE.HQR" };
    public static readonly string[] ExtraFiles = { "ANIM.HQR", "RESS.HQR", "BODY.HQR" };
    public static string[] FilesFor(RaceTrackIsland island) => Files.Concat(island.IslandFiles).ToArray();
    public static string[] AllFiles => Files.Concat(ExtraFiles).Concat(RaceTrackIsland.All.SelectMany(i => i.IslandFiles)).Distinct().ToArray();
    public const string InfoFile = "RACETRACK.JSON";

    public sealed record BuildResult(bool Ok, string Summary, List<string> Log, RaceTrackReport? Report);

    // Where a lap is counted, in the terms the engine uses: the island cube the line is in, its ends in that cube's own world units, the way
    // a lap crosses it.
    public sealed record StartLineInfo(int CubeX, int CubeZ, int X0, int Z0, int X1, int Z1, int DirX, int DirZ);
    // Checkpoints: the same kind of lines, in the order a lap crosses them. Path: the opponent's line from the start line round the lap,
    // each point [x, z, y, speed, bend radius] in world units (x and z counted from the island's corner, 32768 to a cube). PathGrid: how many points
    // before the start line the opponent starts. Opponent: scene -> the index of that scene's copy of the racer. StartScene: the scene
    // the start line is in. Rivals: the other opponents (Baldino), each with a line, a grid and the scenes' copies of its car of its own.
    // Grid: the grid spots, pole first, each [cube x, cube z, x, y, z, turn] (the race-track mode lines the cars up on them), and Pits:
    // the spots in the pit lane the opponents wait on while the player qualifies.
    public sealed record TrackInfo(string Crossing, StartLineInfo? StartLine, List<StartLineInfo>? Checkpoints = null, List<int[]>? Path = null,
        int PathGrid = 0, Dictionary<int, int>? Opponent = null, int StartScene = -1, List<RivalInfo>? Rivals = null, List<int[]>? Grid = null,
        List<int[]>? Pits = null, string Island = "Desert island");
    public sealed record RivalInfo(string Name, List<int[]> Path, int Grid, Dictionary<int, int> Actors);

    // Play's race-track mode on a folder with a race track built: writes the engine's car file (the car setup in the settings, and the track's
    // start line, checkpoints and opponents from RACETRACK.JSON); null for any other folder, which plays the game as it is.
    public static Action<string>? CarFileWriter(string gameDirectory)
    {
        if (!HasBackups(gameDirectory)) return null;
        var car = EditorSettings.Current.RaceCar.Clone();
        return path => car.WriteEngineFile(path, ReadInfo(gameDirectory));
    }

    // A race track built by an older version of the editor, before the grid (and with it the qualifying lap and the count-down): its
    // RACETRACK.JSON has no grid spots, or there is none. The race-track mode then starts the old way; building it again brings them.
    public static bool IsOutdated(string gameDirectory) => HasBackups(gameDirectory) && ReadInfo(gameDirectory)?.Grid is not { Count: > 0 };

    // A folder has a race track when the files of the island its RACETRACK.JSON names were kept.
    // A copy the build can then write to: a game folder taken off a disc (or from a reference set kept read-only) has read-only files, and
    // File.Copy carries that to the copy, so the next build would fail on its own backup.
    public static void CopyWritable(string from, string to)
    {
        File.Copy(from, to, overwrite: true);
        var info = new FileInfo(to);
        if (info.IsReadOnly) info.IsReadOnly = false;
    }

    public static bool HasBackups(string gameDirectory) => BuiltIsland(gameDirectory) is { } island && FilesFor(island).All(f => File.Exists(Path.Combine(gameDirectory, f + BackupSuffix)));

    // The island the folder's track was built on (its RACETRACK.JSON says; a track built before there was a choice is the Desert island's).
    public static RaceTrackIsland? BuiltIsland(string gameDirectory)
    {
        if (!File.Exists(Path.Combine(gameDirectory, InfoFile))) return null;
        return RaceTrackIsland.ByName(ReadInfo(gameDirectory)?.Island ?? RaceTrackIsland.Desert.Name);
    }

    public static string? Problem(string gameDirectory)
    {
        if (!Directory.Exists(gameDirectory)) return "The LBA2 game folder isn't set. Choose it under File > Settings.";
        foreach (var f in AllFiles) if (!File.Exists(Path.Combine(gameDirectory, f))) return $"{f} isn't in the game folder.";
        return null;
    }

    public static BuildResult Build(string gameDirectory, RaceTrackPlan plan, RaceTrackOptions options)
    {
        if (Problem(gameDirectory) is { } problem) return new(false, problem, new(), null);
        try
        {
            var files = FilesFor(options.Island).Concat(ExtraFiles).ToArray();
            foreach (var f in files)
            {
                var backup = Path.Combine(gameDirectory, f + BackupSuffix);
                if (!File.Exists(backup)) CopyWritable(Path.Combine(gameDirectory, f), backup);
            }
            // every build starts from the originals (the island's own ground is loaded from its copy below)
            foreach (var f in files.Where(f => f != options.Island.IleFile))
                CopyWritable(Path.Combine(gameDirectory, f + BackupSuffix), Path.Combine(gameDirectory, f));
            var log = new List<string>();
            var extra = Prepare(gameDirectory, options);
            var island = IslandFile.Load(Path.Combine(gameDirectory, options.Island.IleFile + BackupSuffix));
            var themed = RaceTrackTextures.Import(island, options.Island, gameDirectory);
            options.Theme = themed.Theme;
            if (themed.Log.Length > 0) extra.Add(themed.Log);
            var report = RaceTrackBuilder.Build(island, plan, options);
            island.Save(Path.Combine(gameDirectory, options.Island.IleFile));
            if (BuildTwin(gameDirectory, plan, options, report) is { } twin) extra.Add(twin);
            extra.AddRange(Finish(gameDirectory, report, options));
            var scenes = RaceTrackScenes.Apply(gameDirectory, report, options);
            WriteInfo(gameDirectory, report, options, scenes);

            log.Add($"The lap is {report.Length:0} cells ({report.Length * 512:0} game units) long: {report.Vertices} ground points levelled, {report.Cells} cells painted, {report.DecorsRemoved + report.SolidDecorsRemoved} decor objects taken off the road.");
            log.AddRange(extra);
            log.AddRange(report.Notes);
            foreach (var placed in report.Placed) log.Add(placed);
            log.AddRange(scenes.Log.Where(l => !l.Contains("actors removed,") && !l.Contains("no longer waits") && !l.Contains("demo scene")));
            log.Add($"{scenes.ActorsRemoved} actors removed from {scenes.ScenesChanged} scenes.");
            var where = scenes.StartScene >= 0 ? $"Scene {scenes.StartScene} ({options.Island.Name}) starts on the grid." : "";
            return new(true, $"The race track is built. {where}".Trim(), log, report);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or LBAAssembler.LbaScript.ScriptCompileException)
        {
            // leave the folder as it was
            try { Restore(gameDirectory); } catch (Exception again) when (again is IOException or UnauthorizedAccessException) { /* the backups are still there */ }
            return new(false, $"Nothing was changed: {error.Message}", new(), null);
        }
    }

    // The island's fine-weather file (Citadel Island's CITABAU), built with the same plan after the main one: the ground is the same, so
    // the road comes out the same, but its decors, its texture page and palette and its decor bodies are its own -- the tiles are copied
    // in its own palette, the deck gets a body in its own OBL, and whatever of its own decor is on the road is cleared. The scenes are
    // shared and were given the main build's cars, grid and zones. Returns a line for the log, or null for an island with no twin.
    public static string? BuildTwin(string gameDirectory, RaceTrackPlan plan, RaceTrackOptions options, RaceTrackReport main)
    {
        if (options.Island.TwinIleFile is not { } ile || options.Island.TwinOblFile is not { } obl) return null;
        var source = Path.Combine(gameDirectory, ile + BackupSuffix);
        var twin = IslandFile.Load(File.Exists(source) ? source : Path.Combine(gameDirectory, ile));
        var twinOptions = options.Copy();
        twinOptions.RetailBodies = new();
        CopyRetailBodies(gameDirectory, obl, twinOptions);
        if (twinOptions.Crossing == CrossingStyle.Bridge)
            twinOptions.DeckBodyIndex = RaceTrackDeckBody.AppendTo(Path.Combine(gameDirectory, obl), twinOptions);
        twinOptions.Theme = RaceTrackTextures.Import(twin, options.Island, gameDirectory, ile).Theme;
        var report = RaceTrackBuilder.Build(twin, plan, twinOptions);
        twin.Save(Path.Combine(gameDirectory, ile));
        var same = Math.Abs(report.Length - main.Length) < 0.01 && report.StartLine.SequenceEqual(main.StartLine) && report.Pits.SequenceEqual(main.Pits) && report.Checkpoints.Count == main.Checkpoints.Count;
        return $"{ile} (the island once the storm is over) built with the same track: lap {report.Length:0} cells, {report.DecorsRemoved + report.SolidDecorsRemoved} of its own decors taken off the road" +
               (same ? "" : " -- WARNING: its road came out different from the main build's");
    }

    // The retail race track's gantry (64-66) and viaduct arch (68-70) are bodies of DESERT.OBL; on another island those numbers are that
    // island's own objects, so the Desert's are copied to the end of its OBL (as they are: the island palettes put the same colours at
    // those indices -- Citadel's fine-weather one exactly, its storm one a shade darker, as its whole island is) and options.RetailBodies
    // maps each to its copy. Nothing for the Desert island itself. Returns a line for the log.
    public static readonly int[] RetailBodyNumbers = { 64, 65, 66, 68, 69, 70 };

    public static string? CopyRetailBodies(string gameDirectory, string oblFile, RaceTrackOptions options)
    {
        if (string.Equals(oblFile, RaceTrackIsland.Desert.OblFile, StringComparison.OrdinalIgnoreCase)) return null;
        var desertPath = Path.Combine(gameDirectory, RaceTrackIsland.Desert.OblFile);
        var desert = HqrArchive.Open(File.Exists(desertPath + BackupSuffix) ? desertPath + BackupSuffix : desertPath);
        var path = Path.Combine(gameDirectory, oblFile);
        var index = HqrArchive.CountEntries(path);
        var hqr = File.ReadAllBytes(path);
        foreach (var body in RetailBodyNumbers)
        {
            hqr = HqrWriter.AppendEntry(hqr, HqrWriter.StoredEntry(desert.Read(body)));
            options.RetailBodies[body] = index++;
        }
        File.WriteAllBytes(path, hqr);
        return $"the start gantry and the viaduct arch: the Desert track's own bodies copied into {oblFile} (as {options.RetailBodies[64]}-{options.RetailBodies[70]})";
    }

    // What a crossing style needs in the files besides the island and the scenes (the files are the originals when these run): before the
    // island is built, the bridge deck's bodies in DESERT.OBL; after it, the jump's flight in ANIM.HQR and RESS.HQR, as long as the layout
    // made it, and Baldino's car in BODY.HQR and RESS.HQR. Return lines for the build's log.
    public static List<string> Prepare(string gameDirectory, RaceTrackOptions options)
    {
        var log = new List<string>();
        if (CopyRetailBodies(gameDirectory, options.Island.OblFile, options) is { } copied) log.Add(copied);
        if (options.Crossing == CrossingStyle.Bridge)
            options.DeckBodyIndex = RaceTrackDeckBody.AppendTo(Path.Combine(gameDirectory, options.Island.OblFile), options);
        if (options.Crossing == CrossingStyle.Jump) options.JumpAnim = RaceTrackJumpAnim.Generic;
        return log;
    }

    public static List<string> Finish(string gameDirectory, RaceTrackReport report, RaceTrackOptions options)
    {
        var log = new List<string>();
        if (report.Jump is { } jump) log.Add(RaceTrackJumpAnim.Install(gameDirectory, jump.FlightScale));
        if (options.AddOpponent && options.AddBaldino) log.Add(RaceTrackBaldinoCar.Install(gameDirectory).Log);
        return log;
    }

    // RACETRACK.JSON: the crossing style and the start line, for Play.
    public static void WriteInfo(string gameDirectory, RaceTrackReport report, RaceTrackOptions options, RaceTrackScenes.Result scenes)
    {
        static StartLineInfo Line((double X0, double Z0, double X1, double Z1, double DirX, double DirZ) l)
        {
            var cx = (int)Math.Floor((l.X0 + l.X1) / 2 / 64); var cz = (int)Math.Floor((l.Z0 + l.Z1) / 2 / 64);
            int Local(double cells, int cube) => (int)Math.Round((cells - cube * 64) * 512);
            return new StartLineInfo(cx, cz, Local(l.X0, cx), Local(l.Z0, cz), Local(l.X1, cx), Local(l.Z1, cz), (int)Math.Round(l.DirX * 1000), (int)Math.Round(l.DirZ * 1000));
        }
        var line = report.LapLine is { } l ? Line(l) : null;
        var checkpoints = report.Checkpoints.Select(Line).ToList();
        static List<int[]> Points(List<(double X, double Z, double Y, double Speed, double Radius)> line)
            => line.Select(p => new[] { (int)Math.Round(p.X * 512), (int)Math.Round(p.Z * 512), (int)Math.Round(p.Y), (int)Math.Round(p.Speed), (int)Math.Round(Math.Min(p.Radius, 1e6)) }).ToList();
        var path = Points(report.RacePath);
        var rivals = new List<RivalInfo>();
        if (report.BaldinoPath.Count > 0 && scenes.Baldino.Count > 0) rivals.Add(new RivalInfo("Baldino", Points(report.BaldinoPath), RaceTrackScenes.BaldinoGridBack, scenes.Baldino));
        var info = new TrackInfo(options.Crossing.ToString(), line, checkpoints, path.Count > 0 ? path : null, 4, scenes.Opponent.Count > 0 ? scenes.Opponent : null, scenes.StartScene,
            rivals.Count > 0 ? rivals : null, scenes.Grid.Count > 0 ? scenes.Grid : null, scenes.Pits.Count > 0 ? scenes.Pits : null, options.Island.Name);
        File.WriteAllText(Path.Combine(gameDirectory, InfoFile), JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
    }

    // The folder's RACETRACK.JSON, or null when there is none (or it can't be read: a track built before the file existed).
    public static TrackInfo? ReadInfo(string gameDirectory)
    {
        try
        {
            var path = Path.Combine(gameDirectory, InfoFile);
            return File.Exists(path) ? JsonSerializer.Deserialize<TrackInfo>(File.ReadAllText(path)) : null;
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException)
        {
            DebugLog.Log($"RaceTrackService.ReadInfo: {error.Message}");
            return null;
        }
    }

    // Puts the changed files back from the copies made by the first build and removes the copies (and RACETRACK.JSON).
    public static string Restore(string gameDirectory)
    {
        if (!HasBackups(gameDirectory)) return "There is no race track build to undo in this folder.";
        var back = new List<string>();
        foreach (var f in AllFiles)
        {
            var backup = Path.Combine(gameDirectory, f + BackupSuffix);
            if (!File.Exists(backup)) continue;
            CopyWritable(backup, Path.Combine(gameDirectory, f));
            back.Add(f);
        }
        foreach (var f in back) File.Delete(Path.Combine(gameDirectory, f + BackupSuffix));
        var info = Path.Combine(gameDirectory, InfoFile);
        if (File.Exists(info)) File.Delete(info);
        return $"The original {string.Join(", ", back)} are back.";
    }
}
