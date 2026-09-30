using System.IO;
using System.Windows;
using System.Windows.Controls;
using LBAAssembler.Terrain;
using Microsoft.Win32;

namespace LBAAssembler;

// Tools > LBA2: race track: builds a proposed track (levelled, banked road with the retail track's own textures, pit lane, start gantry,
// and a bridge or a jump where the lap crosses itself) into the LBA2 game folder, or puts the folder back as it was. One island's track at
// a time: the Desert island's, Citadel Island's town circuit, or Mosquibees Island's mountain lap (whose plan draws its own bridge and
// jump, so it takes no crossing style).
internal sealed class RaceTrackWindow : Window
{
    private readonly string gameRoot;
    private readonly Action changed;
    private readonly ComboBox islandBox = new() { MinWidth = 260 };
    private readonly RadioButton builtInPlan = new() { Content = "The track built into the program", IsChecked = true };
    private readonly RadioButton filePlan = new() { Content = "A plan file:" };
    private readonly TextBox planPath = new() { Padding = new Thickness(3), IsEnabled = false };
    private readonly Button browseButton = new() { Content = "Browse…", Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(6, 0, 0, 0), IsEnabled = false };
    private readonly ComboBox crossing = new() { MinWidth = 420 };
    private readonly CheckBox removeActors = new() { Content = "Remove the actors of the island's outside scenes, except Twinsen, the buggy and those the travel cutscenes need", IsChecked = true, ToolTip = "The engine's hidden Zoe placeholder in slot 1 stays (the buggy needs that slot), and so do the ferry, the Dino-Fly and the actors Twinsen's own script waits on in the cutscenes of arriving, leaving and the game's ending." };
    private readonly CheckBox buggyAlways = new() { Content = "The buggy is there from the start of any game (skip the car quest)", IsChecked = true, ToolTip = "The island's scenes delete the buggy until game variable 74 reaches 3; this makes that test always pass" };
    private readonly CheckBox startAtLine = new() { Content = "Twinsen and the buggy start on the grid", IsChecked = true };
    private readonly CheckBox roadZones = new() { Content = "Remove zones that would act on a car on the road (doors, hit, ladder, escalator, grid, rail)", IsChecked = true };
    private readonly CheckBox trackCameras = new() { Content = "Remove the fixed camera angles along the track (the view keeps following the car)", IsChecked = true, ToolTip = "Camera zones (type 1) that reach the road or come within a few cells of it" };
    private readonly CheckBox clearOldTrack = new() { Content = "Clear what's left of the original race track (its road paint, start gantry, arch, billboard)", IsChecked = true, ToolTip = "The Desert island only, cube (7,10), scene 57: its painted road becomes sand where the new track doesn't run over it. The garage and its lamp stay." };
    private readonly CheckBox holomap = new() { Content = "Draw the track on the island's holomap picture", IsChecked = true, ToolTip = "The pre-rendered island picture the holomap zooms into (HOLOMAP.HQR): the road, curbs and start line drawn through the holomap's own camera, hidden behind hills. Citadel Island's storm and fine-weather pictures both." };
    private readonly CheckBox story = new() { Content = "Story: Zoe sends Twinsen to the start line, and he finds racing gloves in the attic (Citadel Island)", IsChecked = true, ToolTip = "The game's opening: Zoe's line (in all six languages), a holomap arrow on the start line instead of the pharmacy's, and a pair of racing gloves where the darts lay in Twinsen's attic -- a new inventory model and texts in the slot of the car part the mod has no use for." };
    private readonly TextBox log = new() { IsReadOnly = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, FontFamily = new System.Windows.Media.FontFamily("Consolas"), FontSize = 11, Height = 190, TextWrapping = TextWrapping.NoWrap };
    private readonly Button buildButton = new() { Content = "Build the track", Padding = new Thickness(18, 5, 18, 5), IsDefault = true };
    private readonly Button restoreButton = new() { Content = "Put the original files back", Padding = new Thickness(14, 5, 14, 5) };
    private readonly Button closeButton = new() { Content = "Close", Padding = new Thickness(14, 5, 14, 5), IsCancel = true };
    private readonly Button carButton = new() { Content = "Race car setup…", Padding = new Thickness(14, 5, 14, 5), ToolTip = "The buggy's gears, acceleration, brakes and steering when you play a folder with a race track built" };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };

    public RaceTrackWindow(string gameRoot, Action changed)
    {
        this.gameRoot = gameRoot;
        this.changed = changed;
        Title = "LBA2 race track";
        Width = 760; SizeToContent = SizeToContent.Height; MinWidth = 640;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;
        SetResourceReference(BackgroundProperty, "ThemeWindowBrush");
        SetResourceReference(ForegroundProperty, "ThemeTextBrush");
        foreach (var i in RaceTrackIsland.All) islandBox.Items.Add(new ComboBoxItem { Content = i.Shown, Tag = i });
        islandBox.SelectedIndex = 0;
        // the island the folder's track is on, when it has one (building again keeps to it)
        if (RaceTrackService.BuiltIsland(gameRoot) is { } builtOn)
            islandBox.SelectedItem = islandBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag is RaceTrackIsland s && s.Name == builtOn.Name) ?? islandBox.SelectedItem;
        islandBox.SelectionChanged += (_, _) => IslandChanged();
        crossing.Items.Add(new ComboBoxItem { Content = "A physical bridge (a walkable deck, like Citadel Island's rope bridge)", Tag = CrossingStyle.Bridge });
        crossing.Items.Add(new ComboBoxItem { Content = "A jump: a ramp up, a gap over the other road, a ramp down (a longer retail car jump)", Tag = CrossingStyle.Jump });
        crossing.Items.Add(new ComboBoxItem { Content = "A viaduct of arches over a level junction", Tag = CrossingStyle.Viaduct });
        crossing.Items.Add(new ComboBoxItem { Content = "A level junction, nothing over it", Tag = CrossingStyle.Level });
        // the style the folder's track was built with, when it has one (building it again keeps it), else the bridge
        crossing.SelectedIndex = 0;
        // (Citadel Island's: its town circuit's, in the fine-weather file -- the storm track draws its own jump)
        var info = Terrain.RaceTrackService.ReadInfo(gameRoot);
        if ((info?.Twin ?? info)?.Crossing is { } built && Enum.TryParse<CrossingStyle>(built, out var builtStyle))
            crossing.SelectedItem = crossing.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag is CrossingStyle s && s == builtStyle) ?? crossing.SelectedItem;
        BuildLayout();
        builtInPlan.Checked += (_, _) => PlanChoiceChanged();
        filePlan.Checked += (_, _) => PlanChoiceChanged();
        browseButton.Click += (_, _) => Browse();
        buildButton.Click += async (_, _) => await BuildAsync();
        restoreButton.Click += (_, _) => Restore();
        carButton.Click += (_, _) => new RaceCarWindow(forPlay: false, gameRoot) { Owner = this }.ShowDialog();
        IslandChanged();
        UpdateStatus();
    }

    private RaceTrackIsland Island() => islandBox.SelectedItem is ComboBoxItem { Tag: RaceTrackIsland i } ? i : RaceTrackIsland.Desert;

    // What only one island has: the retail race track to clear is the Desert island's own.
    private void IslandChanged()
    {
        var desert = Island().IleFile == RaceTrackIsland.Desert.IleFile;
        clearOldTrack.IsEnabled = desert;
        if (!desert) clearOldTrack.IsChecked = false;
        // the story is Citadel Island's own opening
        var citadel = Island().IleFile == RaceTrackIsland.Citadel.IleFile;
        story.IsEnabled = citadel;
        story.IsChecked = citadel;
        CrossingChoice();
        UpdateStatus();
    }

    // A built-in plan that draws its own bridge and jump (RaceTrackPlan.Heights: Mosquibees Island's) takes no crossing style. Citadel
    // Island's storm track is one, but the style still goes to its town circuit (the fine-weather file's own track).
    private void CrossingChoice()
    {
        RaceTrackPlan? plan = null;
        var twinChooses = false;
        try
        {
            if (builtInPlan.IsChecked == true && RaceTrackPlan.Built(Island()) is { Planned: true } p) plan = p;
            twinChooses = RaceTrackPlan.BuiltTwin(Island()) is { Planned: false };
        }
        catch (InvalidDataException) { plan = null; }
        var planned = plan is not null && !twinChooses;
        // (the box shows what the build will use: RaceTrackService.FollowPlan)
        if (planned)
        {
            var style = new RaceTrackOptions();
            RaceTrackService.FollowPlan(plan!, style);
            crossing.SelectedItem = crossing.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Tag is CrossingStyle s && s == style.Crossing) ?? crossing.SelectedItem;
        }
        crossing.IsEnabled = !planned;
        crossing.ToolTip = planned ? "This track's plan draws its own bridge and jump"
            : twinChooses ? $"For the town circuit ({Island().TwinIleFile}, once the storm is over); the storm track ({Island().IleFile}) draws its own jump" : null;
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
        foreach (var c in new Control[] { builtInPlan, filePlan, removeActors, buggyAlways, startAtLine, roadZones, trackCameras, clearOldTrack, holomap, story }) c.SetResourceReference(ForegroundProperty, "ThemeTextBrush");

        var root = new StackPanel { Margin = new Thickness(16) };
        var intro = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Text = "Builds a proposed race track on an island: the ground under the road is levelled and banked, the road is painted with the retail " +
                   "track's asphalt, red and white curbs, arrows and red/gold hatching, with a pit lane and a start gantry. Where the lap crosses itself, " +
                   "the choice below decides what carries the one road over the other. On an island other than the Desert one the road's tiles are copied " +
                   "into its own spare texture space and matched to its palette, and its scenes are given a buggy.\n\n" +
                   "It changes the island's ground and decor bodies and SCENE.HQR in the LBA2 game folder (and, for a jump, ANIM.HQR and RESS.HQR: its flight; BODY.HQR and RESS.HQR for the cars made after the game's characters: Baldino's, and fifty-seven more nobody drives yet, three or more for each island). The first time, the originals are kept beside them (as *" + RaceTrackService.BackupSuffix +
                   "); every build starts from those copies, and the button below puts them back. When you play a folder with a race track built, the game " +
                   "runs in its race-track mode: the car setup below (gears on X and Z, brakes, steering), the car staying level on the bridge, checkpoints round the lap, " +
                   "an opponent (the retail track's racer), and the gear, speed, lap times and position on screen. " +
                   "Use Tools > Test edits first to try it on a scratch copy of the game folder.",
        };
        intro.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        root.Children.Add(intro);
        root.Children.Add(new TextBlock { Text = $"Game folder: {gameRoot}", Margin = new Thickness(0, 8, 0, 8), TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold, Foreground = (System.Windows.Media.Brush)FindResource("ThemeTextBrush") });

        root.Children.Add(Section("The island"));
        root.Children.Add(islandBox);

        root.Children.Add(Section("The track"));
        root.Children.Add(builtInPlan);
        var fileRow = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
        DockPanel.SetDock(browseButton, Dock.Right);
        fileRow.Children.Add(filePlan); fileRow.Children.Add(browseButton); fileRow.Children.Add(planPath);
        filePlan.Margin = new Thickness(0, 0, 8, 0);
        root.Children.Add(fileRow);
        clearOldTrack.Margin = new Thickness(0, 6, 0, 0);
        root.Children.Add(clearOldTrack);

        root.Children.Add(Section("Where the lap crosses itself"));
        root.Children.Add(crossing);
        root.Children.Add(Section("The scenes"));
        foreach (var c in new CheckBox[] { removeActors, buggyAlways, startAtLine, roadZones, trackCameras, holomap, story }) { c.Margin = new Thickness(0, 2, 0, 2); root.Children.Add(c); }

        root.Children.Add(status);
        log.Margin = new Thickness(0, 10, 0, 0);
        root.Children.Add(log);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        restoreButton.Margin = new Thickness(0, 0, 10, 0); buildButton.Margin = new Thickness(0, 0, 10, 0); carButton.Margin = new Thickness(0, 0, 10, 0);
        buttons.Children.Add(carButton); buttons.Children.Add(restoreButton); buttons.Children.Add(buildButton); buttons.Children.Add(closeButton);
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
        CrossingChoice();
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
        RemoveTrackCameras = trackCameras.IsChecked == true,
        OldTrackCube = clearOldTrack.IsChecked == true ? Island().OldTrackCube : null,
        Island = Island(),
        DrawOnHolomap = holomap.IsChecked == true,
        Story = story.IsChecked == true,
    };

    private async Task BuildAsync()
    {
        RaceTrackPlan plan;
        // (a plan file is the track of the entry chosen: for Citadel Island's town circuit, its fine-weather file's, the storm track built in)
        RaceTrackPlan? twinPlan = null;
        try
        {
            plan = RaceTrackPlan.Built(Options().Island);
            if (filePlan.IsChecked == true)
            {
                if (Island().RacesTwin) twinPlan = RaceTrackPlan.Load(planPath.Text.Trim());
                else plan = RaceTrackPlan.Load(planPath.Text.Trim());
            }
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show(this, $"The plan can't be read: {error.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var answer = MessageBox.Show(this,
            $"Build {Island().Shown} into\n{gameRoot}\n\n{string.Join(", ", RaceTrackService.FilesFor(Island()))} will change{(Island().TwinPlanResource is not null ? " (both of the island's tracks are built: they share its scenes)" : "")}. " +
            (RaceTrackService.HasBackups(gameRoot) ? "The originals kept by the first build are used again." : "The originals are kept as *" + RaceTrackService.BackupSuffix + "."),
            Title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (answer != MessageBoxResult.OK) return;
        var options = Options();
        buildButton.IsEnabled = false; restoreButton.IsEnabled = false;
        log.Text = "Building…";
        var root = gameRoot;
        var result = await Task.Run(() => RaceTrackService.Build(root, plan, options, twinPlan));
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
