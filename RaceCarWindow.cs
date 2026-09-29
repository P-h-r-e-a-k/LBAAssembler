using System.Windows;
using System.Windows.Controls;

namespace LBAAssembler;

// The race car's setup (RaceCarSetup): the buggy's gears, acceleration, brakes, steering and the on-screen display when Play runs an LBA2 folder with a
// race track built. Shown before such a play (forPlay: Play / Cancel), or from the race track dialog (Save / Cancel). Saved in the settings.
internal sealed class RaceCarWindow : Window
{
    private readonly RaceCarSetup setup = EditorSettings.Current.RaceCar.Clone();
    private readonly ComboBox preset = new() { MinWidth = 320 };
    private readonly ComboBox gears = new() { Width = 70 };
    private readonly StackPanel gearRows = new();
    private readonly List<(FrameworkElement Row, Slider Slider)> gearSliders = new();
    private readonly CheckBox automatic = new() { Content = "Automatic gearbox (the gears change by themselves)" };
    private readonly CheckBox display = new() { Content = "Show the gear, the speed and the lap times on screen" };
    private readonly CheckBox askBeforePlay = new() { Content = "Show this before each race-track play" };
    private readonly CheckBox opponent = new() { Content = "Race the original track's racer" };
    private readonly CheckBox baldino = new() { Content = "Race Baldino too, in his rocket car" };
    private readonly CheckBox biker = new() { Content = "Race the motorbike Rabbibunny too (quicker in the bends, slower on the straights)" };
    private readonly CheckBox fightBack = new() { Content = "They push harder when they fall behind you" };
    private readonly CheckBox qualifying = new() { Content = "Drive a qualifying lap first: the times set the grid" };
    private readonly CheckBox fineWeather = new() { Content = "Stop the rain on Citadel Island (the weather after the lighthouse, when the aliens land)", ToolTip = "No rain or thunder, the brighter island with its own light and sky. Only the weather changes: the story stays where it is." };
    private readonly CheckBox startAtLine = new() { Content = "Start beside the car on the start/finish straight, with the editor's markings hidden" };
    private readonly List<Action> refresh = new();
    private bool updating;

    public RaceCarWindow(bool forPlay)
    {
        Title = "Race car setup";
        Width = 560; SizeToContent = SizeToContent.Height; MinWidth = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        SetResourceReference(BackgroundProperty, "ThemeWindowBrush");
        SetResourceReference(ForegroundProperty, "ThemeTextBrush");

        var root = new StackPanel { Margin = new Thickness(16) };
        root.Children.Add(Text("How the buggy drives when you play a game folder that has a race track built. X shifts up a gear and Z down (unless the gearbox " +
                               "is automatic); each gear has its own top speed, and a low gear pulls harder than a high one. Speeds are as the game's display shows " +
                               "them, a cell taken as a metre: the original buggy tops out at 27 km/h. A lap counts once you have crossed every checkpoint round the " +
                               "track; the opponents (the original track's racer, Baldino in his rocket car and the motorbike Rabbibunny) line up with you on the grid and start on the count-down. " +
                               "Other game folders play the game as it is."));

        root.Children.Add(Section("Start from"));
        foreach (var p in RaceCarSetup.Presets) preset.Items.Add(new ComboBoxItem { Content = p.Name, Tag = p });
        preset.Items.Add(new ComboBoxItem { Content = "Custom" });
        preset.SelectionChanged += (_, _) => PresetChosen();
        root.Children.Add(preset);

        root.Children.Add(Section("Gearbox"));
        for (var g = 1; g <= RaceCarSetup.MaxGears; g++) gears.Items.Add(g);
        gears.SelectionChanged += (_, _) => { if (updating || gears.SelectedItem is not int count) return; setup.Gears = count; ShowGearRows(); Edited(); };
        var gearCount = new StackPanel { Orientation = Orientation.Horizontal };
        gearCount.Children.Add(Text("Gears:", new Thickness(0, 0, 8, 0)));
        gearCount.Children.Add(gears);
        root.Children.Add(gearCount);
        for (var g = 0; g < RaceCarSetup.MaxGears; g++)
        {
            var gear = g;
            var (row, slider) = SliderRow($"Gear {g + 1} top speed", 5, 80, 1, " km/h", () => setup.TopKmh(gear), v =>
            {
                while (setup.GearTopKmh.Count <= gear) setup.GearTopKmh.Add(setup.GearTopKmh.LastOrDefault(27));
                setup.GearTopKmh[gear] = (int)v;
            });
            gearSliders.Add((row, slider));
            gearRows.Children.Add(row);
        }
        root.Children.Add(gearRows);
        automatic.Checked += (_, _) => { setup.Automatic = true; Edited(); };
        automatic.Unchecked += (_, _) => { setup.Automatic = false; Edited(); };
        automatic.Margin = new Thickness(0, 4, 0, 0);
        root.Children.Add(automatic);

        root.Children.Add(Section("Handling (the original buggy is 100 %)"));
        root.Children.Add(SliderRow("Acceleration", 25, 300, 5, " %", () => setup.AccelerationPercent, v => setup.AccelerationPercent = (int)v).Row);
        root.Children.Add(SliderRow("Braking", 25, 300, 5, " %", () => setup.BrakingPercent, v => setup.BrakingPercent = (int)v).Row);
        root.Children.Add(SliderRow("Rolling to a stop (off the throttle)", 0, 300, 5, " %", () => setup.CoastingPercent, v => setup.CoastingPercent = (int)v).Row);
        root.Children.Add(SliderRow("Steering", 50, 200, 5, " %", () => setup.SteeringPercent, v => setup.SteeringPercent = (int)v).Row);
        root.Children.Add(SliderRow("Top speed backwards", 5, 40, 1, " km/h", () => setup.ReverseKmh, v => setup.ReverseKmh = (int)v).Row);

        root.Children.Add(Section("Race"));
        root.Children.Add(Text("The opponents drive this car, set up as above, on racing lines of their own: 100 % skill drives it perfectly, " +
                               "faster than you can; a little less gives a close race. The race starts with a count-down from the grid; with " +
                               "qualifying, your first timed lap decides where you start.", new Thickness(0, 0, 0, 4)));
        opponent.Checked += (_, _) => setup.Opponent = true;
        opponent.Unchecked += (_, _) => setup.Opponent = false;
        root.Children.Add(opponent);
        root.Children.Add(SliderRow("The racer's skill", 50, 120, 1, " %", () => setup.RacerSkill, v => setup.RacerSkill = (int)v).Row);
        baldino.Checked += (_, _) => setup.Baldino = true;
        baldino.Unchecked += (_, _) => setup.Baldino = false;
        baldino.Margin = new Thickness(0, 4, 0, 0);
        root.Children.Add(baldino);
        root.Children.Add(SliderRow("Baldino's skill", 50, 120, 1, " %", () => setup.BaldinoSkill, v => setup.BaldinoSkill = (int)v).Row);
        biker.Checked += (_, _) => setup.Biker = true;
        biker.Unchecked += (_, _) => setup.Biker = false;
        biker.Margin = new Thickness(0, 4, 0, 0);
        root.Children.Add(biker);
        root.Children.Add(SliderRow("The biker's skill", 50, 120, 1, " %", () => setup.BikerSkill, v => setup.BikerSkill = (int)v).Row);
        fightBack.Checked += (_, _) => setup.OpponentsFightBack = true;
        fightBack.Unchecked += (_, _) => setup.OpponentsFightBack = false;
        fightBack.Margin = new Thickness(0, 4, 0, 0);
        root.Children.Add(fightBack);
        qualifying.Checked += (_, _) => setup.Qualifying = true;
        qualifying.Unchecked += (_, _) => setup.Qualifying = false;
        qualifying.Margin = new Thickness(0, 4, 0, 0);
        root.Children.Add(qualifying);
        fineWeather.Checked += (_, _) => setup.FineWeather = true;
        fineWeather.Unchecked += (_, _) => setup.FineWeather = false;
        fineWeather.Margin = new Thickness(0, 4, 0, 0);
        root.Children.Add(fineWeather);
        startAtLine.Checked += (_, _) => setup.StartAtLine = true;
        startAtLine.Unchecked += (_, _) => setup.StartAtLine = false;
        startAtLine.Margin = new Thickness(0, 4, 0, 0);
        root.Children.Add(startAtLine);

        root.Children.Add(Section("On screen"));
        display.Checked += (_, _) => setup.ShowDisplay = true;
        display.Unchecked += (_, _) => setup.ShowDisplay = false;
        askBeforePlay.Checked += (_, _) => setup.AskBeforePlay = true;
        askBeforePlay.Unchecked += (_, _) => setup.AskBeforePlay = false;
        root.Children.Add(display);
        askBeforePlay.Margin = new Thickness(0, 4, 0, 0);
        root.Children.Add(askBeforePlay);
        foreach (var c in new Control[] { automatic, display, askBeforePlay, opponent, baldino, biker, fightBack, qualifying, fineWeather, startAtLine }) c.SetResourceReference(ForegroundProperty, "ThemeTextBrush");

        var ok = new Button { Content = forPlay ? "Play" : "Save", Padding = new Thickness(18, 5, 18, 5), IsDefault = true, Margin = new Thickness(0, 0, 10, 0) };
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(14, 5, 14, 5), IsCancel = true };
        ok.Click += (_, _) => Accept();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        buttons.Children.Add(ok); buttons.Children.Add(cancel);
        root.Children.Add(buttons);
        Content = root;
        Load();
    }

    private void Load()
    {
        updating = true;
        gears.SelectedItem = Math.Clamp(setup.Gears, 1, RaceCarSetup.MaxGears);
        automatic.IsChecked = setup.Automatic;
        display.IsChecked = setup.ShowDisplay;
        askBeforePlay.IsChecked = setup.AskBeforePlay;
        opponent.IsChecked = setup.Opponent;
        baldino.IsChecked = setup.Baldino;
        biker.IsChecked = setup.Biker;
        fightBack.IsChecked = setup.OpponentsFightBack;
        qualifying.IsChecked = setup.Qualifying;
        fineWeather.IsChecked = setup.FineWeather;
        startAtLine.IsChecked = setup.StartAtLine;
        foreach (var r in refresh) r();
        ShowGearRows();
        preset.SelectedIndex = MatchingPreset();
        updating = false;
    }

    // Which preset the setup is (the last item, Custom, when none).
    private int MatchingPreset()
    {
        for (var i = 0; i < RaceCarSetup.Presets.Count; i++)
        {
            var p = RaceCarSetup.Presets[i].Setup;
            if (p.Gears == setup.Gears && Enumerable.Range(0, setup.Gears).All(g => p.TopKmh(g) == setup.TopKmh(g)) && p.AccelerationPercent == setup.AccelerationPercent &&
                p.BrakingPercent == setup.BrakingPercent && p.CoastingPercent == setup.CoastingPercent && p.SteeringPercent == setup.SteeringPercent &&
                p.ReverseKmh == setup.ReverseKmh && p.Automatic == setup.Automatic)
                return i;
        }
        return RaceCarSetup.Presets.Count;
    }

    private void PresetChosen()
    {
        if (updating || preset.SelectedItem is not ComboBoxItem { Tag: RaceCarSetup.Preset p }) return;
        var chosen = p.Setup;
        setup.Gears = chosen.Gears; setup.GearTopKmh = new List<int>(chosen.GearTopKmh);
        setup.AccelerationPercent = chosen.AccelerationPercent; setup.BrakingPercent = chosen.BrakingPercent; setup.CoastingPercent = chosen.CoastingPercent;
        setup.SteeringPercent = chosen.SteeringPercent; setup.ReverseKmh = chosen.ReverseKmh; setup.Automatic = chosen.Automatic;
        Load();
    }

    // Any value changed by hand: the preset box says Custom unless the values happen to be a preset's.
    private void Edited()
    {
        if (updating) return;
        updating = true;
        preset.SelectedIndex = MatchingPreset();
        updating = false;
    }

    private void ShowGearRows()
    {
        for (var g = 0; g < gearSliders.Count; g++) gearSliders[g].Row.Visibility = g < setup.Gears ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Accept()
    {
        // the gears' top speeds from low to high
        for (var g = 1; g < setup.Gears; g++) if (setup.TopKmh(g) < setup.TopKmh(g - 1))
        {
            MessageBox.Show(this, $"Gear {g + 1} is slower than gear {g}. Each gear needs a higher top speed than the one below it.", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        EditorSettings.Current.RaceCar = setup;
        try { EditorSettings.Current.Save(); }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException) { DebugLog.Log($"RaceCarWindow: settings not saved: {error.Message}"); }
        DialogResult = true;
    }

    private (FrameworkElement Row, Slider Slider) SliderRow(string label, double min, double max, double step, string unit, Func<double> get, Action<double> set)
    {
        var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        var name = Text(label);
        var slider = new Slider { Minimum = min, Maximum = max, SmallChange = step, LargeChange = step * 5, TickFrequency = step, IsSnapToTickEnabled = true, VerticalAlignment = VerticalAlignment.Center };
        var value = Text("", new Thickness(8, 0, 0, 0));
        Grid.SetColumn(slider, 1); Grid.SetColumn(value, 2);
        grid.Children.Add(name); grid.Children.Add(slider); grid.Children.Add(value);
        slider.ValueChanged += (_, e) =>
        {
            value.Text = $"{e.NewValue:0}{unit}";
            if (updating) return;
            set(e.NewValue);
            Edited();
        };
        refresh.Add(() => { slider.Value = Math.Clamp(get(), min, max); value.Text = $"{slider.Value:0}{unit}"; });
        return (grid, slider);
    }

    private TextBlock Text(string text, Thickness? margin = null)
    {
        var t = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, Margin = margin ?? new Thickness(0) };
        t.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        return t;
    }

    private TextBlock Section(string text)
    {
        var t = new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 4) };
        t.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        return t;
    }
}
