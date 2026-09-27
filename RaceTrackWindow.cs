using System.IO;
using System.Windows;
using System.Windows.Controls;
using LBAAssembler.Terrain;
using Microsoft.Win32;

namespace LBAAssembler;

// Tools > LBA2: Desert island race track: builds the proposed track (levelled, banked road with the retail track's own textures, pit lane, start gantry, a
// bridge over the harbour, and a jump where the lap crosses itself) into the LBA2 game folder, or puts the folder back as it was.
internal sealed class RaceTrackWindow : Window
{
    private readonly string gameRoot;
    private readonly Action changed;
    private readonly RadioButton builtInPlan = new() { Content = "The Desert island track (built into the program)", IsChecked = true };
    private readonly RadioButton filePlan = new() { Content = "A plan file:" };
    private readonly TextBox planPath = new() { Padding = new Thickness(3), IsEnabled = false };
    private readonly Button browseButton = new() { Content = "Browse…", Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(6, 0, 0, 0), IsEnabled = false };
    private readonly ComboBox crossing = new() { MinWidth = 420 };
    private readonly CheckBox removeActors = new() { Content = "Remove every actor of the island's outside scenes except Twinsen and the buggy", IsChecked = true, ToolTip = "Scenes 55-73. The engine's hidden Zoe placeholder in slot 1 stays: the buggy needs that slot to be there." };
    private readonly CheckBox buggyAlways = new() { Content = "The buggy is there from the start of any game (skip the car quest)", IsChecked = true, ToolTip = "The island's scenes delete the buggy until game variable 74 reaches 3; this makes that test always pass" };
    private readonly CheckBox startAtLine = new() { Content = "Twinsen and the buggy start on the start line (scene 67)", IsChecked = true };
    private readonly CheckBox roadZones = new() { Content = "Remove zones that would act on a car on the road (doors, hit, ladder, escalator, grid, rail)", IsChecked = true };
    private readonly TextBox log = new() { IsReadOnly = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, FontFamily = new System.Windows.Media.FontFamily("Consolas"), FontSize = 11, Height = 190, TextWrapping = TextWrapping.NoWrap };
    private readonly Button buildButton = new() { Content = "Build the track", Padding = new Thickness(18, 5, 18, 5), IsDefault = true };
    private readonly Button restoreButton = new() { Content = "Put the original files back", Padding = new Thickness(14, 5, 14, 5) };
    private readonly Button closeButton = new() { Content = "Close", Padding = new Thickness(14, 5, 14, 5), IsCancel = true };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };

    public RaceTrackWindow(string gameRoot, Action changed)
    {
        this.gameRoot = gameRoot;
        this.changed = changed;
        Title = "Desert island race track";
        Width = 760; SizeToContent = SizeToContent.Height; MinWidth = 640;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;
        SetResourceReference(BackgroundProperty, "ThemeWindowBrush");
        SetResourceReference(ForegroundProperty, "ThemeTextBrush");
        crossing.Items.Add(new ComboBoxItem { Content = "A physical bridge (a walkable deck, like Citadel Island's rope bridge)", Tag = CrossingStyle.Bridge });
        crossing.Items.Add(new ComboBoxItem { Content = "A jump over the junction (the retail Desert island car jump)", Tag = CrossingStyle.Jump });
        crossing.Items.Add(new ComboBoxItem { Content = "A viaduct of arches over a level junction", Tag = CrossingStyle.Viaduct });
        crossing.Items.Add(new ComboBoxItem { Content = "A level junction, nothing over it", Tag = CrossingStyle.Level });
        crossing.SelectedIndex = 0;
        BuildLayout();
        builtInPlan.Checked += (_, _) => PlanChoiceChanged();
        filePlan.Checked += (_, _) => PlanChoiceChanged();
        browseButton.Click += (_, _) => Browse();
        buildButton.Click += async (_, _) => await BuildAsync();
        restoreButton.Click += (_, _) => Restore();
        UpdateStatus();
    }

    private void BuildLayout()
    {
        foreach (var t in new TextBlock[] { status }) t.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        foreach (var box in new TextBox[] { planPath, log })
        {
            box.SetResourceReference(BackgroundProperty, "ThemeFieldBrush");
            box.SetResourceReference(ForegroundProperty, "ThemeTextBrush");
            box.SetResourceReference(BorderBrushProperty, "ThemeBorderBrush");
        }
        foreach (var c in new Control[] { builtInPlan, filePlan, removeActors, buggyAlways, startAtLine, roadZones }) c.SetResourceReference(ForegroundProperty, "ThemeTextBrush");

        var root = new StackPanel { Margin = new Thickness(16) };
        var intro = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Text = "Builds the proposed race track on the White Leaf Desert island: the ground under the road is levelled and banked, the road is painted with the retail " +
                   "track's asphalt, red and white curbs, arrows and red/gold hatching, with a pit lane, a start gantry and a bridge over the harbour. Where the lap crosses itself, " +
                   "the choice below decides what carries the one road over the other.\n\n" +
                   "It changes DESERT.ILE, DESERT.OBL and SCENE.HQR in the LBA2 game folder. The first time, the originals are kept beside them (as *" + RaceTrackService.BackupSuffix +
                   "); every build starts from those copies, and the button below puts them back. " +
                   "Use Tools > Test edits first to try it on a scratch copy of the game folder.",
        };
        intro.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        root.Children.Add(intro);
        root.Children.Add(new TextBlock { Text = $"Game folder: {gameRoot}", Margin = new Thickness(0, 8, 0, 8), TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold, Foreground = (System.Windows.Media.Brush)FindResource("ThemeTextBrush") });

        root.Children.Add(Section("The track"));
        root.Children.Add(builtInPlan);
        var fileRow = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
        DockPanel.SetDock(browseButton, Dock.Right);
        fileRow.Children.Add(filePlan); fileRow.Children.Add(browseButton); fileRow.Children.Add(planPath);
        filePlan.Margin = new Thickness(0, 0, 8, 0);
        root.Children.Add(fileRow);

        root.Children.Add(Section("Where the lap crosses itself"));
        root.Children.Add(crossing);
        root.Children.Add(Section("The scenes"));
        foreach (var c in new CheckBox[] { removeActors, buggyAlways, startAtLine, roadZones }) { c.Margin = new Thickness(0, 2, 0, 2); root.Children.Add(c); }

        root.Children.Add(status);
        log.Margin = new Thickness(0, 10, 0, 0);
        root.Children.Add(log);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        restoreButton.Margin = new Thickness(0, 0, 10, 0); buildButton.Margin = new Thickness(0, 0, 10, 0);
        buttons.Children.Add(restoreButton); buttons.Children.Add(buildButton); buttons.Children.Add(closeButton);
        root.Children.Add(buttons);
        Content = root;
    }

    private TextBlock Section(string text)
    {
        var t = new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 4) };
        t.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        return t;
    }

    private void PlanChoiceChanged()
    {
        var file = filePlan.IsChecked == true;
        planPath.IsEnabled = file; browseButton.IsEnabled = file;
    }

    private void Browse()
    {
        var dialog = new OpenFileDialog { Title = "Race track plan", Filter = "Race track plan (*.json)|*.json|All files|*.*" };
        if (dialog.ShowDialog(this) == true) planPath.Text = dialog.FileName;
    }

    private void UpdateStatus()
    {
        var has = RaceTrackService.HasBackups(gameRoot);
        restoreButton.IsEnabled = has;
        status.Text = has ? "A race track is built in this folder (the originals are kept)." : "No race track is built in this folder yet.";
    }

    private RaceTrackOptions Options() => new()
    {
        Crossing = crossing.SelectedItem is ComboBoxItem { Tag: CrossingStyle style } ? style : CrossingStyle.Bridge,
        RemoveActors = removeActors.IsChecked == true,
        BuggyAlways = buggyAlways.IsChecked == true,
        StartAtLine = startAtLine.IsChecked == true,
        RemoveRoadZones = roadZones.IsChecked == true,
    };

    private async Task BuildAsync()
    {
        RaceTrackPlan plan;
        try { plan = filePlan.IsChecked == true ? RaceTrackPlan.Load(planPath.Text.Trim()) : RaceTrackPlan.Desert(); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show(this, $"The plan can't be read: {error.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var answer = MessageBox.Show(this,
            $"Build the race track into\n{gameRoot}\n\nDESERT.ILE, DESERT.OBL and SCENE.HQR will change. " +
            (RaceTrackService.HasBackups(gameRoot) ? "The originals kept by the first build are used again." : "The originals are kept as *" + RaceTrackService.BackupSuffix + "."),
            Title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (answer != MessageBoxResult.OK) return;
        var options = Options();
        buildButton.IsEnabled = false; restoreButton.IsEnabled = false;
        log.Text = "Building…";
        var root = gameRoot;
        var result = await Task.Run(() => RaceTrackService.Build(root, plan, options));
        buildButton.IsEnabled = true;
        log.Text = string.Join("\n", result.Log);
        status.Text = result.Summary;
        UpdateStatus();
        if (result.Ok) changed();
    }

    private void Restore()
    {
        var answer = MessageBox.Show(this, "Put the original DESERT.ILE, DESERT.OBL and SCENE.HQR back? Anything else changed in them since the track was built is lost.", Title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (answer != MessageBoxResult.OK) return;
        try
        {
            var text = RaceTrackService.Restore(gameRoot);
            log.Text = text; status.Text = text;
            UpdateStatus();
            changed();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"Couldn't put the files back: {error.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
