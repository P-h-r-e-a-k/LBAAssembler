using System.IO;

namespace LBAAssembler.Terrain;

// Builds the Desert island race track into an LBA2 game folder, and puts the folder back. The three files it changes (DESERT.ILE, DESERT.OBL, SCENE.HQR) are
// kept beside the originals as *.before-racetrack the first time; every later build starts from those copies, so building again never piles a
// track on a track, and Restore puts them back.
internal static class RaceTrackService
{
    public const string BackupSuffix = ".before-racetrack";
    public static readonly string[] Files = { "DESERT.ILE", "SCENE.HQR", "DESERT.OBL" };

    public sealed record BuildResult(bool Ok, string Summary, List<string> Log, RaceTrackReport? Report);

    public static bool HasBackups(string gameDirectory) => Files.All(f => File.Exists(Path.Combine(gameDirectory, f + BackupSuffix)));

    public static string? Problem(string gameDirectory)
    {
        if (!Directory.Exists(gameDirectory)) return "The LBA2 game folder isn't set. Choose it under File > Settings.";
        foreach (var f in Files) if (!File.Exists(Path.Combine(gameDirectory, f))) return $"{f} isn't in the game folder.";
        return null;
    }

    public static BuildResult Build(string gameDirectory, RaceTrackPlan plan, RaceTrackOptions options)
    {
        if (Problem(gameDirectory) is { } problem) return new(false, problem, new(), null);
        var kept = new List<string>();
        try
        {
            foreach (var f in Files)
            {
                var backup = Path.Combine(gameDirectory, f + BackupSuffix);
                if (File.Exists(backup)) continue;
                File.Copy(Path.Combine(gameDirectory, f), backup);
                kept.Add(f);
            }
            File.Copy(Path.Combine(gameDirectory, "DESERT.OBL" + BackupSuffix), Path.Combine(gameDirectory, "DESERT.OBL"), overwrite: true);
            if (options.Crossing == CrossingStyle.Bridge)
                options.DeckBodyIndex = RaceTrackDeckBody.AppendTo(Path.Combine(gameDirectory, "DESERT.OBL"), options);
            var island = IslandFile.Load(Path.Combine(gameDirectory, "DESERT.ILE" + BackupSuffix));
            var report = RaceTrackBuilder.Build(island, plan, options);
            island.Save(Path.Combine(gameDirectory, "DESERT.ILE"));
            File.Copy(Path.Combine(gameDirectory, "SCENE.HQR" + BackupSuffix), Path.Combine(gameDirectory, "SCENE.HQR"), overwrite: true);
            var scenes = RaceTrackScenes.Apply(gameDirectory, report, options);

            var log = new List<string>();
            log.Add($"The lap is {report.Length:0} cells ({report.Length * 512:0} game units) long: {report.Vertices} ground points levelled, {report.Cells} cells painted, {report.DecorsRemoved + report.SolidDecorsRemoved} decor objects taken off the road.");
            log.AddRange(report.Notes);
            foreach (var placed in report.Placed) log.Add(placed);
            log.AddRange(scenes.Log.Where(l => !l.Contains("actors removed,") && !l.Contains("no longer waits") && !l.Contains("demo scene")));
            log.Add($"{scenes.ActorsRemoved} actors removed from {scenes.ScenesChanged} scenes.");
            var where = report.StartLine.Count > 0 ? "Scene 67 (Desert island, at the Temple of Bù) starts on the start line." : "";
            return new(true, $"The race track is built. {where}".Trim(), log, report);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or LBAAssembler.LbaScript.ScriptCompileException)
        {
            // leave the folder as it was
            try { Restore(gameDirectory); } catch (Exception again) when (again is IOException or UnauthorizedAccessException) { /* the backups are still there */ }
            return new(false, $"Nothing was changed: {error.Message}", new(), null);
        }
    }

    // Puts DESERT.ILE, DESERT.OBL and SCENE.HQR back from the copies made by the first build and removes the copies.
    public static string Restore(string gameDirectory)
    {
        if (!HasBackups(gameDirectory)) return "There is no race track build to undo in this folder.";
        foreach (var f in Files) File.Copy(Path.Combine(gameDirectory, f + BackupSuffix), Path.Combine(gameDirectory, f), overwrite: true);
        foreach (var f in Files) File.Delete(Path.Combine(gameDirectory, f + BackupSuffix));
        return "The original DESERT.ILE, DESERT.OBL and SCENE.HQR are back.";
    }
}
