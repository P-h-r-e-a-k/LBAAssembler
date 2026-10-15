using System.Windows;
using System.Windows.Controls;
using LBAAssembler.Maps;

namespace LBAAssembler;

// File > New map: what kind of map -- an LBA2 island, an LBA2 interior or an LBA1 interior -- its name, and the game's own map it takes its
// look from (MapNew). Built in code, as ListPickWindow is.
internal sealed class NewMapWindow : Window
{
    public MapKind Kind { get; private set; }
    public string MapName { get; private set; } = "";
    public string IslandFileName { get; private set; } = "";
    public string BaseIsland { get; private set; } = "";
    public int CubesX { get; private set; } = 2;
    public int CubesZ { get; private set; } = 2;
    public int LandHeight { get; private set; } = 1200;
    public int BaseScene { get; private set; }

    public NewMapWindow(bool lba2, bool lba1, IReadOnlyList<string> islands, IReadOnlyList<(int Id, string Label)> lba2Interiors, IReadOnlyList<(int Id, string Label)> lba1Scenes)
    {
        Title = "New map";
        Width = 520; SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        SetResourceReference(BackgroundProperty, "ThemeWindowBrush");
        SetResourceReference(ForegroundProperty, "ThemeTextBrush");

        var root = new StackPanel { Margin = new Thickness(16) };
        TextBlock Label(string text, double top = 10) { var t = new TextBlock { Text = text, Margin = new Thickness(0, top, 0, 4) }; return t; }
        TextBlock Note(string text)
        {
            var t = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) };
            t.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextMutedBrush");
            return t;
        }
        TextBox Box(string text, double width = 0) { var b = new TextBox { Text = text, Padding = new Thickness(4, 3, 4, 3) }; if (width > 0) { b.Width = width; b.HorizontalAlignment = HorizontalAlignment.Left; } return b; }
        ComboBox Combo<T>(IEnumerable<T> items) { var c = new ComboBox { Padding = new Thickness(4, 3, 4, 3) }; foreach (var i in items) c.Items.Add(i!); if (c.Items.Count > 0) c.SelectedIndex = 0; return c; }

        // the kind
        root.Children.Add(Label("What kind of map", 0));
        var island = new RadioButton { Content = "An LBA2 island (an ILE: ground, sea and objects, an outside scene for each cube)", IsEnabled = lba2, Margin = new Thickness(0, 2, 0, 2) };
        var interior = new RadioButton { Content = "An LBA2 interior (a scene and its grid of blocks)", IsEnabled = lba2 && lba2Interiors.Count > 0, Margin = new Thickness(0, 2, 0, 2) };
        var lba1Room = new RadioButton { Content = "An LBA1 interior (a scene, its grid and its block library)", IsEnabled = lba1 && lba1Scenes.Count > 0, Margin = new Thickness(0, 2, 0, 2) };
        root.Children.Add(island); root.Children.Add(interior); root.Children.Add(lba1Room);
        if (!lba2) root.Children.Add(Note("LBA2's maps need the LBA2 game folder (File > Settings)."));
        if (!lba1) root.Children.Add(Note("LBA1's maps need the LBA1 game folder (File > Settings)."));

        root.Children.Add(Label("Name"));
        var name = Box("New map");
        root.Children.Add(name);

        // an island's settings
        var islandPanel = new StackPanel();
        islandPanel.Children.Add(Label("Its file name in the game folder (up to eight letters or digits)"));
        var file = Box("NEWISLE", 140);
        islandPanel.Children.Add(file);
        islandPanel.Children.Add(Label("Its look (ground and sea textures, light, sky, music): the game's island"));
        var baseIsland = Combo(islands);
        if (islands.Contains("DESERT")) baseIsland.SelectedItem = "DESERT";
        islandPanel.Children.Add(baseIsland);
        islandPanel.Children.Add(Label("Its size, in cubes (64 x 64 cells each), and its land's height"));
        var size = new StackPanel { Orientation = Orientation.Horizontal };
        var cubesX = Box("2", 48); var cubesZ = Box("2", 48); var height = Box("1200", 70);
        size.Children.Add(cubesX); size.Children.Add(new TextBlock { Text = "  by  ", VerticalAlignment = VerticalAlignment.Center }); size.Children.Add(cubesZ);
        size.Children.Add(new TextBlock { Text = "  cubes,  land", VerticalAlignment = VerticalAlignment.Center }); size.Children.Add(new TextBlock { Width = 8 }); size.Children.Add(height);
        size.Children.Add(new TextBlock { Text = "  high", VerticalAlignment = VerticalAlignment.Center });
        islandPanel.Children.Add(size);
        islandPanel.Children.Add(Note("Flat land with a beach all round into the sea, Twinsen standing in the first cube. Shape it in Build mode."));

        // an interior's
        var interiorPanel = new StackPanel();
        interiorPanel.Children.Add(Label("Built from the game's interior (its block library, light and music)"));
        var baseInterior = Combo(lba2Interiors.Select(s => s.Label));
        interiorPanel.Children.Add(baseInterior);
        interiorPanel.Children.Add(Note("A floor of 32 x 32 cells of that interior's own floor, Twinsen standing on it. Paint it in the grid editor."));

        var lba1Panel = new StackPanel();
        lba1Panel.Children.Add(Label("The scene it takes the place of in the LBA1 game (its island, music and block library)"));
        var baseLba1 = Combo(lba1Scenes.Select(s => s.Label));
        lba1Panel.Children.Add(baseLba1);
        lba1Panel.Children.Add(Note("LBA1 can't have scenes added: installed into the game, the map replaces that scene."));

        root.Children.Add(islandPanel); root.Children.Add(interiorPanel); root.Children.Add(lba1Panel);
        void Show()
        {
            islandPanel.Visibility = island.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            interiorPanel.Visibility = interior.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            lba1Panel.Visibility = lba1Room.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
        foreach (var r in new[] { island, interior, lba1Room }) r.Checked += (_, _) => Show();
        (island.IsEnabled ? island : interior.IsEnabled ? interior : lba1Room).IsChecked = true;
        Show();

        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0), Visibility = Visibility.Collapsed };
        error.SetResourceReference(TextBlock.ForegroundProperty, "ThemeWarningBrush");
        root.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        var ok = new Button { Content = "Create", Padding = new Thickness(18, 4, 18, 4), Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(14, 4, 14, 4), IsCancel = true };
        buttons.Children.Add(ok); buttons.Children.Add(cancel);
        root.Children.Add(buttons);
        Content = root;

        name.TextChanged += (_, _) => { if (island.IsChecked == true && !fileEdited) { settingFile = true; file.Text = LbaMap.FileNameFrom(name.Text, 8); settingFile = false; } };
        file.TextChanged += (_, _) => { if (!settingFile) fileEdited = true; };
        ok.Click += (_, _) =>
        {
            string? problem = null;
            MapName = name.Text.Trim().Length > 0 ? name.Text.Trim() : "New map";
            if (island.IsChecked == true)
            {
                Kind = MapKind.Lba2Island;
                IslandFileName = file.Text.Trim().ToUpperInvariant();
                BaseIsland = baseIsland.SelectedItem as string ?? "";
                if (!LbaMap.ValidIslandFile(IslandFileName)) problem = "The file name is one to eight letters, digits or underscores.";
                else if (islands.Contains(IslandFileName, StringComparer.OrdinalIgnoreCase)) problem = $"The game folder already has {IslandFileName}.ILE: choose another name.";
                else if (!int.TryParse(cubesX.Text, out var x) || !int.TryParse(cubesZ.Text, out var z) || x is < 1 or > 6 || z is < 1 or > 6) problem = "An island is 1 to 6 cubes each way.";
                else if (!int.TryParse(height.Text, out var h) || h is < 300 or > 20000) problem = "The land's height is 300 to 20,000.";
                else { CubesX = x; CubesZ = z; LandHeight = h; }
                if (BaseIsland.Length == 0) problem ??= "Choose the island it takes its look from.";
            }
            else if (interior.IsChecked == true) { Kind = MapKind.Lba2Interior; BaseScene = lba2Interiors[Math.Max(0, baseInterior.SelectedIndex)].Id; }
            else { Kind = MapKind.Lba1Interior; BaseScene = lba1Scenes[Math.Max(0, baseLba1.SelectedIndex)].Id; }
            if (problem is not null) { error.Text = problem; error.Visibility = Visibility.Visible; return; }
            DialogResult = true;
        };
        Loaded += (_, _) => { name.Focus(); name.SelectAll(); };
    }

    private bool fileEdited, settingFile;
}
