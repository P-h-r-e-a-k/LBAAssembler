using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using LBAAssembler.Scenes;

namespace LBAAssembler;

// Build > Nuke: everything in the scene that is open blows up, and a flat empty scene is left (SceneNuke: what goes and what stays;
// NukeOverlay: the explosion). Saved to the game folder at once, as one undo step: Edit > Undo brings it all back.
public partial class MainWindow
{
    private bool nuking;

    // Whether there is a scene open here to nuke.
    private bool CanNuke => editMode == EditMode.Build && !lba2JoinedView && (currentGame == GameKind.Lba1
        ? Lba1Configured && lba1CurrentTiles is { Count: > 0 }
        : Lba2Configured && (interiorSceneActive ? interiorSceneNumber >= 0 : currentIsland is not null));

    private async void Nuke_Click(object sender, RoutedEventArgs e)
    {
        if (nuking) return;
        if (!CanNuke) { Refuse("Open a scene first (an LBA1 scene, an LBA2 interior or an island) in Build mode."); return; }
        SceneNuke nuke;
        var lba1 = currentGame == GameKind.Lba1;
        try
        {
            if (lba1)
            {
                if (Lba1FocusScene() is not { } scene) return;
                nuke = SceneNuke.ForLba1(EditorSettings.Current.Lba1Directory, scene);
            }
            else
            {
                var scene = Lba2SceneToPlay();
                // (the island's file is written: unsaved terrain edits would be lost -- ask, as leaving the island does)
                if (!interiorSceneActive && !ConfirmTerrainDiscard()) return;
                nuke = SceneNuke.ForLba2(gameRoot, scene, interiorSceneActive ? null : activeFile);
            }
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)      // (SceneEditException is an IOException)
        {
            Refuse($"This scene can't be nuked: {error.Message}");
            return;
        }
        var pendingScripts = lba1 ? lba1Session.EditedScenes : scriptSession.EditedScenes;
        if (nuke.Scenes.FirstOrDefault(pendingScripts.Contains, -1) is var edited and >= 0)
        {
            Refuse($"Scene {edited} has unsaved script edits. Save or discard them first.");
            return;
        }

        var left = nuke.Exits > 0 ? $"Twinsen on flat ground, and the {nuke.Exits} exit{(nuke.Exits == 1 ? "" : "s")} to other scenes (so the game can still leave)" : "Twinsen on flat ground";
        var warnings = nuke.Warnings.Count == 0 ? "" : "\n\n" + string.Join("\n", nuke.Warnings);
        var question = $"Nuke {nuke.Where}?\n\nEverything in it goes: {nuke.Summary}.\nWhat is left: {left}.{warnings}\n\n" +
                       "It is saved to the game folder straight away. Edit > Undo (Ctrl+Z) brings it all back.";
        if (MessageBox.Show(this, question, "Nuke", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;

        // open actor windows would be left editing actors that are gone (one with unsaved edits asks first, and may refuse)
        foreach (var window in openAttributesWindows.Values.ToList()) window.Close();
        foreach (var window in openLba1ActorWindows.Values.ToList()) window.Close();
        if (openAttributesWindows.Count > 0 || openLba1ActorWindows.Count > 0) { Refuse("Close the actor windows first."); return; }

        nuking = true;
        NukeButton.IsEnabled = false;
        var view = terrainShown ? (FrameworkElement)TerrainMapHost : ViewportHost;
        NukeOverlay? overlay = null;
        try
        {
            // the view as it is, held on screen while the scene is emptied and drawn again underneath
            overlay = NukeOverlay.Cover(view, NukeOverlay.Snapshot(view));
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
            nuke.Commit();
            ReloadAfterNuke(nuke, lba1);
            await WaitForViewToSettle();
            await overlay.Detonate(NukeOverlay.Snapshot(view));
            overlay = null;
            var kept = nuke.Exits > 0 ? $" (its {nuke.Exits} exit{(nuke.Exits == 1 ? "" : "s")} kept)" : "";
            SetStatus($"Nuked {nuke.Where}: {nuke.Summary} gone{kept}. Edit > Undo brings it back.", StatusKind.Success);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException)
        {
            overlay?.Remove();
            DebugLog.Log($"MainWindow: nuke failed: {error}");
            MessageBox.Show(this, $"Not nuked: {error.Message}", "Nuke", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            nuking = false;
            NukeButton.IsEnabled = true;
            Keyboard.Focus(this);          // (the overlay had it for Esc, and is gone: Ctrl+Z is to work straight away)
        }
    }

    private void Refuse(string message) => MessageBox.Show(this, message, "Nuke", MessageBoxButton.OK, MessageBoxImage.Information);

    // The views after the nuke's files were written (as after an undo step: RunHistoryStep).
    private void ReloadAfterNuke(SceneNuke nuke, bool lba1)
    {
        zoneCache.Clear();
        if (lba1)
        {
            foreach (var scene in nuke.Scenes) lba1Session.ForgetScene(scene);
            ReloadLba1AfterEdit();
        }
        else
        {
            foreach (var scene in nuke.Scenes) scriptSession.ForgetScene(scene);
            if (interiorSceneActive) ShowInteriorScene(interiorSceneNumber, keepView: true);
            else if (nuke.ChangesIsland) ReloadIslandFromDisk();
            else
            {
                InvalidateNativeIsland();
                actorMarkersIsland = null;
                if (nativeViewActive) RenderNativeCamera();
            }
        }
        RefreshZoneListIfVisible();
    }

    // The island's file changed on disk behind the views (a nuke, or undoing one): the terrain editor, the 3D view, the minimap and the
    // actor dots read it again.
    private void ReloadIslandFromDisk()
    {
        if (currentGame != GameKind.Lba2 || interiorSceneActive || currentIsland is null) return;
        // (the terrain editor holds its own copy of the island; it was saved or thrown away before the file changed)
        if (terrainEditor is not null && (terrainToolsActive || string.Equals(terrainEditor.CurrentName, activeFile, StringComparison.OrdinalIgnoreCase)))
            terrainEditor.Open(activeFile, reload: true);
        InvalidateNativeIsland();
        UpdateLive();
        if (live is not null) PushLive();      // (the live preview's island is a copy of its own, which Sync leaves alone)
        actorMarkersIsland = null;
        sceneViewStale = true;
        if (!terrainShown) RefreshSceneViewIfStale();
    }

    // Waits for the view to show the scene as it now is: the island's 3D view is drawn on a render thread and arrives a moment later
    // (an interior, an LBA1 scene and the terrain map are drawn at once). Gives up after a few seconds -- the animation then reveals
    // whatever is there.
    private async Task WaitForViewToSettle()
    {
        var start = Environment.TickCount64;
        while (nativeViewActive && !interiorSceneActive && !terrainShown && Environment.TickCount64 - start < 5000)
        {
            await Task.Delay(40);
            bool busy;
            lock (nativeRenderGate) busy = nativeRenderInFlight || nativeRenderDirty;
            if (!busy) break;
        }
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
    }
}
