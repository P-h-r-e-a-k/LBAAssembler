using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LBAAssembler;

// File > New track (the user, 2026-11-03: "Let's have an option File > New Track > Either the user selects an existing island, out of any
// of our islands that exist or can create a brand new island by placing new map squares on a grid to give them their own basic shape. This
// can auto-save to a new map file, and we should allow the user to load it also"): the track's name, and its island -- a copy of one of the
// folder's islands, or a new one of the squares ticked on a grid, in the look of one of the game's islands. MainWindow.NewTrack makes it a
// map and opens it in Build > Race track. Built in code, as NewMapWindow is.
internal sealed class NewTrackWindow : Window
{
    public const int Grid = 6;
    public string TrackName { get; private set; } = "";
    // a copy of this island of the folder's (its file's name, no extension), or null: a new island
    public string? Existing { get; private set; }
    // a new island's look (the game's island its ground, sea, sky and music are), its squares (x, z on the grid) and its land's height
    public string Look { get; private set; } = "DESERT";
    public HashSet<(int X, int Z)> Squares { get; } = new();
    public int LandHeight { get; private set; } = 1200;

    public NewTrackWindow(IReadOnlyList<(string File, string Label)> islands, IReadOnlyList<string> looks)
    {
        Title = "New track";
        Width = 520; SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        SetResourceReference(BackgroundProperty, "ThemeWindowBrush");
        SetResourceReference(ForegroundProperty, "ThemeTextBrush");

        var root = new StackPanel { Margin = new Thickness(16) };
        TextBlock Label(string text, double top = 10) => new() { Text = text, Margin = new Thickness(0, top, 0, 4) };
        TextBlock Note(string text)
        {
            var t = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) };
            t.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextMutedBrush");
            return t;
        }

        root.Children.Add(Label("The track's name", 0));
        var name = new TextBox { Text = "My track", Padding = new Thickness(4, 3, 4, 3) };
        AutomationProperties.SetName(name, "Track name");
        root.Children.Add(name);

        root.Children.Add(Label("Its island"));
        var existing = new RadioButton { Content = "A copy of one of the islands", GroupName = "island", IsChecked = islands.Count > 0, IsEnabled = islands.Count > 0, Margin = new Thickness(0, 2, 0, 2) };
        var fresh = new RadioButton { Content = "A new island of my own shape", GroupName = "island", IsChecked = islands.Count == 0, Margin = new Thickness(0, 2, 0, 2) };
        AutomationProperties.SetName(existing, "A copy of one of the islands"); AutomationProperties.SetName(fresh, "A new island of my own shape");
        root.Children.Add(existing);
        var islandList = new ComboBox { Padding = new Thickness(4, 3, 4, 3), Margin = new Thickness(18, 2, 0, 6) };
        foreach (var (_, label) in islands) islandList.Items.Add(label);
        if (islandList.Items.Count > 0) islandList.SelectedIndex = Math.Max(0, islands.ToList().FindIndex(i => i.File == "DESERT"));
        AutomationProperties.SetName(islandList, "Island");
        root.Children.Add(islandList);
        root.Children.Add(fresh);

        // a new island: its look, its squares, its height
        var freshPanel = new StackPanel { Margin = new Thickness(18, 2, 0, 0) };
        freshPanel.Children.Add(Label("Its look (ground and sea, light, sky, music): the game's island", 2));
        var look = new ComboBox { Padding = new Thickness(4, 3, 4, 3) };
        foreach (var l in looks) look.Items.Add(l);
        look.SelectedItem = looks.Contains("DESERT") ? "DESERT" : looks.FirstOrDefault();
        AutomationProperties.SetName(look, "Look");
        freshPanel.Children.Add(look);
        freshPanel.Children.Add(Label("Its shape: tick the squares that are land (each a cube, 64 cells a side)"));
        var grid = new UniformGrid { Rows = Grid, Columns = Grid, Width = 6 * 34, Height = 6 * 34, HorizontalAlignment = HorizontalAlignment.Left };
        var squares = new ToggleButton[Grid, Grid];
        for (var z = 0; z < Grid; z++)
            for (var x = 0; x < Grid; x++)
            {
                // (a 3 by 2 block ticked to start with, in the middle)
                var square = new ToggleButton { Margin = new Thickness(1), IsChecked = x is >= 1 and <= 3 && z is >= 2 and <= 3, ToolTip = $"Square {x + 1}, {z + 1}" };
                AutomationProperties.SetName(square, $"Square {x + 1} {z + 1}");
                squares[x, z] = square;
                grid.Children.Add(square);
            }
        freshPanel.Children.Add(grid);
        var count = Note("");
        void Count()
        {
            var n = 0;
            foreach (var s in squares) if (s.IsChecked == true) n++;
            count.Text = n == 0 ? "Tick one square at least." : $"{n} square{(n > 1 ? "s" : "")}: a beach all round the land's edge, down into the sea.";
        }
        foreach (var s in squares) { s.Checked += (_, _) => Count(); s.Unchecked += (_, _) => Count(); }
        Count();
        freshPanel.Children.Add(count);
        var heightRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        heightRow.Children.Add(new TextBlock { Text = "Its land  ", VerticalAlignment = VerticalAlignment.Center });
        var height = new TextBox { Text = "1200", Width = 70, Padding = new Thickness(4, 3, 4, 3) };
        AutomationProperties.SetName(height, "Land height");
        heightRow.Children.Add(height);
        heightRow.Children.Add(new TextBlock { Text = "  high (Twinsen is about 1,000 tall)", VerticalAlignment = VerticalAlignment.Center });
        freshPanel.Children.Add(heightRow);
        root.Children.Add(freshPanel);
        void Show() { islandList.IsEnabled = existing.IsChecked == true; freshPanel.IsEnabled = fresh.IsChecked == true; }
        existing.Checked += (_, _) => Show(); fresh.Checked += (_, _) => Show();
        Show();

        root.Children.Add(Note("The track is a map of its own: saved as you lay it to its map file (in the Maps folder beside the editor), and opened " +
                               "again with File > Open map. Your game folder's islands aren't changed; Test from the flag races it."));

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        var ok = new Button { Content = "Make the track", IsDefault = true, Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "Cancel", IsCancel = true, Padding = new Thickness(12, 4, 12, 4) };
        buttons.Children.Add(ok); buttons.Children.Add(cancel);
        root.Children.Add(buttons);
        ok.Click += (_, _) =>
        {
            TrackName = name.Text.Trim();
            if (TrackName.Length == 0) { MessageBox.Show(this, "Give the track a name.", Title, MessageBoxButton.OK, MessageBoxImage.Information); return; }
            if (existing.IsChecked == true)
            {
                if (islandList.SelectedIndex < 0) return;
                Existing = islands[islandList.SelectedIndex].File;
            }
            else
            {
                Existing = null;
                Look = look.SelectedItem as string ?? "DESERT";
                Squares.Clear();
                for (var z = 0; z < Grid; z++) for (var x = 0; x < Grid; x++) if (squares[x, z].IsChecked == true) Squares.Add((x, z));
                if (Squares.Count == 0) { MessageBox.Show(this, "Tick one square of the shape at least.", Title, MessageBoxButton.OK, MessageBoxImage.Information); return; }
                if (!int.TryParse(height.Text.Trim(), out var h) || h is < 200 or > 12000) { MessageBox.Show(this, "The land's height is 200 to 12000.", Title, MessageBoxButton.OK, MessageBoxImage.Information); return; }
                LandHeight = h;
            }
            DialogResult = true;
        };
        Content = root;
    }
}
