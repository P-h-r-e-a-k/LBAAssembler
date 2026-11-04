using System.IO;
using System.Windows;
using LBAAssembler.Maps;
using LBAAssembler.Terrain;

namespace LBAAssembler;

// File > New track (the user, 2026-11-03: "Race track appears to be auto-saving over our island which it shouldn't be. Let's have an option
// File > New Track > Either the user selects an existing island, out of any of our islands that exist or can create a brand new island by
// placing new map squares on a grid to give them their own basic shape. This can auto-save to a new map file, and we should allow the user
// to load it also, we already have saving and loading custom maps so this should fall into this"): a map of an island for a race track --
// a copy of one of the folder's islands as it was before any track went on, or a new one of the squares ticked (MapNew) -- saved at once
// to a map file of its own in the Maps folder, opened (a copy of the game folder: the folder's own islands untouched) in Build > Race
// track. The pieces laid on it are the map's (LbaMap.RaceTrack), saved into its file as they change; File > Open map opens it again.
public partial class MainWindow
{
    private async void NewTrack_Click(object sender, RoutedEventArgs e)
    {
        if (!ReadyForMap()) return;
        if (!Lba2Configured) { SetStatus("A race track needs the LBA2 game folder (File > Settings).", StatusKind.Warning); return; }
        if (MapOpen && !CloseMapWorkspace()) return;
        var real = gameRoot;
        // (the folder's islands: the game's and those the race tracks' builds made -- not a shared track's copy, not a map track's)
        var islands = Directory.EnumerateFiles(real, "*.ILE").Select(p => Path.GetFileNameWithoutExtension(p).ToUpperInvariant())
            .Where(f => File.Exists(Path.Combine(real, f + ".OBL")) && !f.StartsWith("SHARE", StringComparison.Ordinal) && f != RaceTrackIsland.CustomFile)
            .Order()
            .Select(f => (File: f, Label: RaceTrackIsland.BuiltIn.FirstOrDefault(i => string.Equals(i.IleFile, f + ".ILE", StringComparison.OrdinalIgnoreCase))?.Shown is { } shown ? $"{shown} ({f})" : f))
            .ToList();
        var looks = MapNew.GameIslands.Select(g => g.File).Where(f => File.Exists(Path.Combine(real, f + ".ILE"))).Distinct().ToList();
        var dialog = new NewTrackWindow(islands, looks) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        // (its island's file name: from the track's name, none the folder has)
        var stem = LbaMap.FileNameFrom(dialog.TrackName, 8).ToUpperInvariant();
        if (!LbaMap.ValidIslandFile(stem)) stem = "TRACK";
        var taken = Directory.EnumerateFiles(real, "*.ILE").Select(p => Path.GetFileNameWithoutExtension(p).ToUpperInvariant()).ToHashSet();
        for (var n = 2; taken.Contains(stem) || MapNew.GameIslands.Any(g => g.File == stem); n++) stem = (stem.Length > 6 ? stem[..6] : stem) + n;
        LbaMap map;
        using (UiBusy.Progress(BusyPanel, BusyLabel, "Making the track's island…"))
        {
            try
            {
                var name = dialog.TrackName;
                var existing = dialog.Existing;
                var (look, squares, height) = (dialog.Look, dialog.Squares.ToList(), dialog.LandHeight);
                map = await Task.Run(() =>
                {
                    if (existing is not null) return MapInstall.ExportLba2Island(real, existing, name, originals: true, mapFile: stem);
                    // (the squares' box, the island as big as it)
                    int x0 = squares.Min(s => s.X), z0 = squares.Min(s => s.Z), x1 = squares.Max(s => s.X), z1 = squares.Max(s => s.Z);
                    var shape = squares.Select(s => (s.X - x0, s.Z - z0)).ToList();
                    return MapNew.Lba2Island(real, name, stem, look, x1 - x0 + 1, z1 - z0 + 1, height, squares: shape);
                });
                map.Description = existing is not null ? $"A race track on a copy of {existing}." : $"A race track on an island of its own, in {look}'s look.";
                map.TrackFrom = existing;
            }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or Scenes.SceneEditException or InvalidOperationException)
            {
                DebugLog.Log($"New track failed: {error}");
                SetStatus($"Couldn't make the track's island: {error.Message}", StatusKind.Warning);
                return;
            }
        }
        // (its map file at once: the track is saved into it as it is laid)
        var folder = MapsFolder();
        var file = LbaMap.FileNameFrom(dialog.TrackName, 24).ToLowerInvariant();
        var path = Path.Combine(folder, file + LbaMap.Extension);
        for (var n = 2; File.Exists(path); n++) path = Path.Combine(folder, $"{file}_{n}{LbaMap.Extension}");
        try { map.Save(path); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { SetStatus($"Couldn't save the track's map: {error.Message}", StatusKind.Warning); return; }
        OpenMapWorkspace(map, path);
        if (!MapOpen) return;
        BuildTrackRadio.IsChecked = true;
        SetMode(EditMode.Build);
        SetStatus($"New track \"{map.Name}\": {path}. Lay its pieces in Build > Race track; they're saved into the map file as you go. " +
                  "Test from the flag races it; File > Open map opens it again.", StatusKind.Success);
    }

    // Tools > Lay a track in the 3D view: Build > Race track on the open island (the user, 2026-11-04: track making "more accessible and user
    // friendly" -- the way in found from the menu, not only from the Build tab's radio buttons)
    private void LayTrack3D_Click(object sender, RoutedEventArgs e)
    {
        if (!Lba2Configured) { SetStatus("A race track needs the LBA2 game folder (File > Settings).", StatusKind.Warning); return; }
        BuildTrackRadio.IsChecked = true;
        SetMode(EditMode.Build);
        if (TrackIslandOfView() is null)
            SetStatus("Open one of the islands (the island box) to lay a track on it -- or File > New track for a track of its own.", StatusKind.Info);
    }

    // ---- the open map's track: saved into its file a moment after a change (SaveMap), as long as it has one
    private System.Windows.Threading.DispatcherTimer? mapTrackSave;

    private void ScheduleMapTrackSave()
    {
        if (!MapOpen || openMapPath is null) return;
        if (mapTrackSave is null)
        {
            mapTrackSave = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            mapTrackSave.Tick += (_, _) =>
            {
                mapTrackSave!.Stop();
                if (!MapOpen || openMapPath is null || playing) return;
                if (SaveMap(saveAs: false)) SetTrackStatus($"{trackPieces.Pieces.Count} piece(s), saved into {Path.GetFileName(openMapPath)}.");
            };
        }
        mapTrackSave.Stop();
        mapTrackSave.Start();
    }

    // The open map's island as a race track's (RaceTrackIsland.Custom), when the view is on it; kept for the map it is of
    private RaceTrackIsland? mapTrackIsland;
    private LbaMap? mapTrackIslandOf;

    private RaceTrackIsland? MapTrackIsland()
    {
        if (!MapOpen || openMap is not { Kind: MapKind.Lba2Island, IslandFile: { } file } map
            || !string.Equals(file + ".ILE", activeFile, StringComparison.OrdinalIgnoreCase)) return null;
        if (!ReferenceEquals(mapTrackIslandOf, map)) { mapTrackIsland = RaceTrackIsland.Custom(map); mapTrackIslandOf = map; }
        return mapTrackIsland;
    }
}
