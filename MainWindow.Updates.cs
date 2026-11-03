using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;

namespace LBAAssembler;

// Help's Check for updates, and the check at the start (EditorUpdater): a newer release found is offered (UpdateWindow), swapped in as the editor closes and started.
public partial class MainWindow
{
    private EditorUpdater.Release? updateWaiting;
    private bool updateOnClose, updateChecking, updatesStarted;

    // Once, as the window is first shown: what the last update left removed, the menu's tick set, and -- unless turned off -- a look at GitHub a few seconds later.
    private void StartUpdates()
    {
        if (updatesStarted) return;
        updatesStarted = true;
        EditorUpdater.CleanUp();
        AutoUpdateMenuItem.IsChecked = EditorSettings.Current.CheckForUpdates;
        Closed += (_, _) => UpdateAsClosing();
        if (EditorUpdater.CanUpdate && EditorSettings.Current.CheckForUpdates) _ = CheckForUpdates(manual: false);
    }

    private async Task CheckForUpdates(bool manual)
    {
        if (updateChecking) return;
        if (!EditorUpdater.CanUpdate)
        {
            if (manual) MessageBox.Show(this, "Only the released LBAAssembler.exe updates itself. This one is a build from the source.", "LBA Assembler update", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        updateChecking = true;
        try
        {
            if (!manual) await Task.Delay(4000);
            else SetStatus("Looking for a newer LBA Assembler ...", StatusKind.Info);
            var skipped = EditorSettings.Current.SkippedUpdate;
            var (found, said) = await Task.Run(() => EditorUpdater.Check(skipped, manual));
            if (found is null)
            {
                DebugLog.Log($"[update] {said}");
                if (manual) { SetStatus(said, StatusKind.Info); MessageBox.Show(this, said, "LBA Assembler update", MessageBoxButton.OK, MessageBoxImage.Information); }
                return;
            }
            Offer(found);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException or JsonException or UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException)
        {
            DebugLog.Log($"[update] the check failed: {e.GetType().Name}: {e.Message}{(e.InnerException is { } inner ? $" ({inner.GetType().Name}: {inner.Message})" : "")}");
            if (manual) SetStatus($"Couldn't look for updates: {e.Message}", StatusKind.Warning);
        }
        finally { updateChecking = false; }
    }

    private void Offer(EditorUpdater.Release release)
    {
        var window = new UpdateWindow(release) { Owner = this };
        window.ShowDialog();
        DebugLog.Log($"[update] {release.Tag}: {window.Choice}");
        switch (window.Choice)
        {
            case UpdateWindow.Answer.Now when release.NewFile is null:
                EditorUpdater.OpenPage(release);
                break;
            case UpdateWindow.Answer.Now:
                updateWaiting = release; updateOnClose = true;
                Close();
                // (still open: something unsaved and the user kept it -- the update waits for the next close, or the next start)
                if (IsVisible) { updateOnClose = false; SetStatus($"The update to {release.Tag} waits: it is put in when LBA Assembler next starts and you say so.", StatusKind.Info); }
                break;
            case UpdateWindow.Answer.Skip:
                EditorSettings.Current.SkippedUpdate = release.Digest;
                try { EditorSettings.Current.Save(); } catch (Exception error) { SetStatus($"Couldn't save that setting: {error.Message}", StatusKind.Warning); }
                if (release.NewFile is not null) try { File.Delete(release.NewFile); } catch (IOException) { }
                SetStatus($"{release.Tag} skipped. Help > Check for updates still finds it.", StatusKind.Info);
                break;
            default:
                SetStatus($"{release.Tag} is ready to put in: you'll be asked again at the next start.", StatusKind.Info);
                break;
        }
    }

    // The window closed with an update chosen: swapped in and started (the process ends as it always would).
    private void UpdateAsClosing()
    {
        if (!updateOnClose || updateWaiting is null) return;
        if (EditorUpdater.Swap(updateWaiting) is { } problem) { MessageBox.Show(problem, "LBA Assembler update", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        try { EditorUpdater.Start(); }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { MessageBox.Show($"The new LBA Assembler is in place but didn't start: {e.Message}", "LBA Assembler update"); }
    }

    private void CheckUpdates_Click(object sender, RoutedEventArgs e) => _ = CheckForUpdates(manual: true);

    private void AutoUpdate_Click(object sender, RoutedEventArgs e)
    {
        EditorSettings.Current.CheckForUpdates = AutoUpdateMenuItem.IsChecked;
        try { EditorSettings.Current.Save(); } catch (Exception error) { SetStatus($"Couldn't save that setting: {error.Message}", StatusKind.Warning); }
    }
}
