using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LBAAssembler.Terrain;
using Microsoft.Win32;

namespace LBAAssembler;

// Build > LBA2: race track generator (the user, 2026-10-27: "Let's update LBAAssembler with the ability to take a screenshot of an island
// with a map drawn on it, then we can automatically generate the track accordingly. Let's have this as a new track generator wizard. The
// user selects an island, then with an image of the map they draw the track in one colour, let's go with green, jumps in pink, pitlane in
// orange, and vertical loops in blue. Let's also let the user place track sections onto an island by hand, and automatically snap them
// together so the user doesn't have to perfectly align them").
//
// Three pages: the island; the drawing -- on the island's map (IslandMapRenderer, MapScale pixels a cell): the four pens and an eraser,
// track sections placed by hand (each snaps onto the open end of the one before), a picture drawn on the map elsewhere loaded back
// (TrackSketch.FromImage), the map saved as a picture to draw on; and the track -- followed from the drawing (TrackSketchTracer), shown over
// the map, saved as a plan file and handed to the race track window to build (Build: the window opens with the island ticked and the plan
// chosen).
internal sealed class TrackGeneratorWindow : Window
{
    public const int MapScale = 3;
    private const double RoadCells = 3;          // a pen's stroke and a section's, half its width (cells): the road's middle is what counts
    private const double SnapCells = 8;          // a section within this of an open end snaps onto it
    private const double BridgeCells = 14;       // open ends of sections this near are joined when the track is made

    private readonly string gameRoot;
    // what Build hands the race track window: the island and the plan file
    public RaceTrackIsland? BuildIsland { get; private set; }
    public string? BuildPlan { get; private set; }

    // ---- the pages
    private readonly ContentControl page = new();
    private readonly TextBlock stepText = new() { FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) };
    private readonly Button backButton = new() { Content = "< Back", Padding = new Thickness(14, 5, 14, 5), Margin = new Thickness(0, 0, 8, 0) };
    private readonly Button nextButton = new() { Content = "Next >", Padding = new Thickness(14, 5, 14, 5), IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
    private readonly Button closeButton = new() { Content = "Close", Padding = new Thickness(14, 5, 14, 5), IsCancel = true };
    private int step;

    // page 1: the island
    private readonly ListBox islandList = new() { Height = 330 };

    // page 2: the drawing
    private RaceTrackIsland? island;
    private IslandFile? islandFile;
    private byte[] mapPixels = Array.Empty<byte>();
    private int mapW, mapH, originX, originZ;
    private SketchKind[] drawn = Array.Empty<SketchKind>();
    private readonly Stack<SketchKind[]> undo = new();
    private WriteableBitmap? overlay;
    private readonly Grid board = new();
    private readonly Image mapImage = new() { Stretch = Stretch.None };
    private readonly Image overlayImage = new() { Stretch = Stretch.None };
    private readonly Canvas pieceCanvas = new();
    private readonly ScrollViewer boardScroll = new() { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private readonly ScaleTransform zoom = new(1, 1);
    private readonly Slider penWidth = new() { Minimum = 1, Maximum = 6, Value = RoadCells, Width = 150, TickFrequency = 0.5, IsSnapToTickEnabled = true };
    private readonly List<RadioButton> tools = new();
    private enum Tool { Road, Jump, Pit, Loop, Eraser, Move }
    private Tool tool = Tool.Road;
    private readonly List<Placed> placed = new();
    private Section? placing;
    private double placingHeading;
    private Placed? ghost;
    private Placed? dragging;
    private Point dragFrom;
    private Point? lastPen;
    private readonly TextBlock drawHelp = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0), FontSize = 11 };

    // page 3: the track
    private readonly Grid previewBoard = new();
    private readonly ScrollViewer previewScroll = new() { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private readonly ScaleTransform previewZoom = new(1, 1);
    private readonly TextBox report = new() { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Height = 120, FontSize = 11 };
    private readonly CheckBox reverse = new() { Content = "The other way round", Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
    private readonly Button savePlanButton = new() { Content = "Save the plan…", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(0, 0, 8, 0) };
    private readonly Button buildButton = new() { Content = "Build…", Padding = new Thickness(12, 4, 12, 4), ToolTip = "Saves the plan and opens the race track window with this island ticked and the plan chosen, to build it there" };
    private SketchResult? result;

    public TrackGeneratorWindow(string gameRoot)
    {
        this.gameRoot = gameRoot;
        Title = "LBA2 race track generator";
        Width = 1180; Height = Math.Min(900, SystemParameters.WorkArea.Height); MinWidth = 900; MinHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty, "ThemeWindowBrush");
        SetResourceReference(ForegroundProperty, "ThemeTextBrush");

        var root = new DockPanel { Margin = new Thickness(12) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        buttons.Children.Add(backButton); buttons.Children.Add(nextButton); buttons.Children.Add(closeButton);
        DockPanel.SetDock(buttons, Dock.Bottom); DockPanel.SetDock(stepText, Dock.Top);
        stepText.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        root.Children.Add(buttons); root.Children.Add(stepText); root.Children.Add(page);
        Content = root;

        backButton.Click += (_, _) => Show(step - 1);
        nextButton.Click += (_, _) => Show(step + 1);
        closeButton.Click += (_, _) => Close();
        reverse.Checked += (_, _) => MakeTrack();
        reverse.Unchecked += (_, _) => MakeTrack();
        savePlanButton.Click += (_, _) => SavePlan(null);
        buildButton.Click += (_, _) => Build();
        PreviewKeyDown += KeyPressed;

        foreach (var (isl, label, tip) in RaceTrackWindow.Listed)
        {
            // (an island the build makes with a road of its own -- the Temple's raised road over its floors, the palace's rooms -- is no
            // island to draw a road on; nor one whose file the folder hasn't got yet)
            if (isl == RaceTrackIsland.Temple || isl == RaceTrackIsland.Palace) continue;
            if (!File.Exists(IslandPath(isl))) continue;
            islandList.Items.Add(new ListBoxItem { Content = label, Tag = isl, ToolTip = tip });
        }
        if (islandList.Items.Count > 0) islandList.SelectedIndex = 0;
        islandList.MouseDoubleClick += (_, _) => Show(1);
        BuildDrawPage();
        Show(0);
    }

    private string IslandPath(RaceTrackIsland isl)
    {
        var path = System.IO.Path.Combine(gameRoot, isl.IleFile);
        // (the island as it was before a race track was built on it: its own ground, no road)
        return File.Exists(path + RaceTrackService.BackupSuffix) ? path + RaceTrackService.BackupSuffix : path;
    }

    // ---- the pages, in turn

    private void Show(int to)
    {
        if (to == 1 && step == 0 && !LoadIsland()) return;
        if (to == 2) MakeTrack();
        step = Math.Clamp(to, 0, 2);
        backButton.IsEnabled = step > 0;
        nextButton.Visibility = step < 2 ? Visibility.Visible : Visibility.Collapsed;
        switch (step)
        {
            case 0:
                stepText.Text = "1 of 3 -- the island";
                // (made once: the list can belong to one page only -- made again on Back, adding it to a second page crashed the editor, 2026-10-27)
                if (islandPage is null)
                {
                    islandPage = new DockPanel();
                    var intro = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8), Text =
                        "Draw a race track on an island's map and the generator makes it: the road in green, jumps in pink, vertical loops in blue and the pit lane in orange. " +
                        "Draw with the pens here, place track sections that snap together, or save the map as a picture, draw on it in any paint program and load it back. Which island?" };
                    DockPanel.SetDock(intro, Dock.Top);
                    islandPage.Children.Add(intro); islandPage.Children.Add(islandList);
                }
                page.Content = islandPage;
                break;
            case 1:
                stepText.Text = $"2 of 3 -- draw the track on {island?.Name}";
                page.Content = drawPage;
                break;
            case 2:
                stepText.Text = $"3 of 3 -- the track on {island?.Name}";
                page.Content = trackPage;
                break;
        }
    }

    private bool LoadIsland()
    {
        if (islandList.SelectedItem is not ListBoxItem { Tag: RaceTrackIsland chosen }) return false;
        if (chosen == island && islandFile is not null) return true;
        try
        {
            var file = IslandFile.Load(IslandPath(chosen));
            var renderer = new IslandMapRenderer(file, IslandMapRenderer.LoadPalette(gameRoot, System.IO.Path.GetFileNameWithoutExtension(chosen.IleFile)), MapScale);
            renderer.RenderAll(MapView.Terrain);
            island = chosen; islandFile = file;
            mapW = renderer.PixelWidth; mapH = renderer.PixelHeight; originX = renderer.OriginX; originZ = renderer.OriginZ;
            mapPixels = renderer.Pixels;
            drawn = new SketchKind[mapW * mapH];
            undo.Clear(); placed.Clear(); pieceCanvas.Children.Clear();
            mapImage.Source = BitmapSource.Create(mapW, mapH, 96, 96, PixelFormats.Bgra32, null, mapPixels, mapW * 4);
            overlay = new WriteableBitmap(mapW, mapH, 96, 96, PixelFormats.Bgra32, null);
            overlayImage.Source = overlay;
            board.Width = mapW; board.Height = mapH; pieceCanvas.Width = mapW; pieceCanvas.Height = mapH;
            Redraw(0, 0, mapW, mapH);
            TestSections();
            // (the whole island in sight)
            var fit = Math.Min(1.6, Math.Max(0.3, Math.Min((Width - 360) / mapW, (Height - 200) / mapH)));
            zoom.ScaleX = zoom.ScaleY = fit;
            return true;
        }
        catch (Exception error) when (error is IOException or InvalidDataException or ArgumentException)
        {
            MessageBox.Show(this, $"{chosen.Name}'s island file can't be read: {error.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    // ---- page 2: the drawing

    private DockPanel? islandPage;
    private DockPanel drawPage = new();
    private DockPanel trackPage = new();

    private void BuildDrawPage()
    {
        var bar = new StackPanel { Width = 300, Margin = new Thickness(0, 0, 10, 0) };
        bar.Children.Add(Heading("Pens"));
        AddTool(bar, "The road (green)", Tool.Road, SketchKind.Road);
        AddTool(bar, "A jump (pink): over the road where it flies", Tool.Jump, SketchKind.Jump);
        AddTool(bar, "A vertical loop (blue): over the road where it stands", Tool.Loop, SketchKind.Loop);
        AddTool(bar, "The pit lane (orange): beside the road", Tool.Pit, SketchKind.Pit);
        AddTool(bar, "Eraser", Tool.Eraser, SketchKind.None);
        AddTool(bar, "Move or turn sections", Tool.Move, SketchKind.None);
        var widthRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
        widthRow.Children.Add(new TextBlock { Text = "Pen width ", VerticalAlignment = VerticalAlignment.Center });
        widthRow.Children.Add(penWidth);
        bar.Children.Add(widthRow);

        bar.Children.Add(Heading("Track sections"));
        var sections = new WrapPanel();
        foreach (var s in Section.All)
        {
            var b = new Button { Content = s.Name, Margin = new Thickness(0, 0, 4, 4), Padding = new Thickness(6, 2, 6, 2), ToolTip = s.Tip };
            b.Click += (_, _) => StartPlacing(s);
            sections.Children.Add(b);
        }
        bar.Children.Add(sections);

        bar.Children.Add(Heading("The picture"));
        var saveMap = new Button { Content = "Save the map as a picture…", Margin = new Thickness(0, 0, 0, 4), Padding = new Thickness(6, 3, 6, 3),
            ToolTip = "The island's map, to draw the track on in a paint program (the road green, jumps pink, loops blue, the pit lane orange) and load back" };
        saveMap.Click += (_, _) => SaveMap();
        var loadPic = new Button { Content = "Load a drawn picture…", Margin = new Thickness(0, 0, 0, 4), Padding = new Thickness(6, 3, 6, 3),
            ToolTip = "The map picture with the track drawn on it (the same picture, any size): what is drawn in the pens' colours is taken" };
        loadPic.Click += (_, _) => LoadPicture();
        var undoButton = new Button { Content = "Undo", Margin = new Thickness(0, 0, 0, 4), Padding = new Thickness(6, 3, 6, 3) };
        undoButton.Click += (_, _) => Undo();
        var clear = new Button { Content = "Clear everything", Padding = new Thickness(6, 3, 6, 3) };
        clear.Click += (_, _) => { Remember(); Array.Clear(drawn); placed.Clear(); RedrawPieces(); Redraw(0, 0, mapW, mapH); };
        bar.Children.Add(saveMap); bar.Children.Add(loadPic); bar.Children.Add(undoButton); bar.Children.Add(clear);
        bar.Children.Add(drawHelp);
        drawHelp.Text = "Draw the road as one line round the island (a lap: back to where it began; a sprint: two ends). Cross it over itself where you " +
                        "like: the build makes a jump there. Pink over the road is a jump, blue a loop; the orange pit lane beside the road, along a straight. " +
                        "Sections: pick one, then click on the map -- near the open end of a section it snaps on. The mouse wheel turns a section; Ctrl+wheel zooms; " +
                        "a right click takes a section away; Esc stops placing.";
        foreach (var t in bar.Children.OfType<TextBlock>()) t.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");

        board.Children.Add(mapImage); board.Children.Add(overlayImage); board.Children.Add(pieceCanvas);
        board.LayoutTransform = zoom;
        board.Background = Brushes.Transparent;
        RenderOptions.SetBitmapScalingMode(mapImage, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetBitmapScalingMode(overlayImage, BitmapScalingMode.NearestNeighbor);
        boardScroll.Content = board;
        board.MouseLeftButtonDown += BoardDown;
        board.MouseMove += BoardMove;
        board.MouseLeftButtonUp += BoardUp;
        board.MouseRightButtonDown += BoardRight;
        boardScroll.PreviewMouseWheel += Wheel;
        board.MouseLeave += (_, _) => { if (ghost is not null) { ghost = null; RedrawPieces(); } };

        var scrollBar = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = bar };
        DockPanel.SetDock(scrollBar, Dock.Left);
        drawPage.Children.Add(scrollBar); drawPage.Children.Add(boardScroll);

        // page 3
        var tools3 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
        tools3.Children.Add(reverse); tools3.Children.Add(savePlanButton); tools3.Children.Add(buildButton);
        DockPanel.SetDock(tools3, Dock.Top);
        DockPanel.SetDock(report, Dock.Bottom);
        report.Margin = new Thickness(0, 6, 0, 0);
        report.SetResourceReference(BackgroundProperty, "ThemeFieldBrush");
        report.SetResourceReference(ForegroundProperty, "ThemeTextBrush");
        previewBoard.LayoutTransform = previewZoom;
        previewScroll.Content = previewBoard;
        previewScroll.PreviewMouseWheel += (s, e) => { if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) { var f = e.Delta > 0 ? 1.15 : 1 / 1.15; previewZoom.ScaleX = previewZoom.ScaleY = Math.Clamp(previewZoom.ScaleX * f, 0.25, 6); e.Handled = true; } };
        trackPage.Children.Add(tools3); trackPage.Children.Add(report); trackPage.Children.Add(previewScroll);
    }

    private TextBlock Heading(string text)
    {
        var t = new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 4) };
        t.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
        return t;
    }

    private void AddTool(Panel bar, string label, Tool which, SketchKind kind)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        if (kind != SketchKind.None)
        {
            var (r, g, b) = TrackSketch.Colour(kind);
            row.Children.Add(new Border { Width = 14, Height = 14, Background = new SolidColorBrush(Color.FromRgb(r, g, b)), Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
        }
        row.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, MaxWidth = 250 });
        var radio = new RadioButton { Content = row, GroupName = "tool", IsChecked = which == Tool.Road, Margin = new Thickness(0, 2, 0, 2) };
        radio.SetResourceReference(ForegroundProperty, "ThemeTextBrush");
        radio.Checked += (_, _) => { tool = which; StopPlacing(); };
        tools.Add(radio);
        bar.Children.Add(radio);
    }

    // the overlay: what is drawn, in the pens' colours, over the map
    private void Redraw(int x0, int y0, int x1, int y1)
    {
        if (overlay is null) return;
        x0 = Math.Clamp(x0, 0, mapW); y0 = Math.Clamp(y0, 0, mapH); x1 = Math.Clamp(x1, 0, mapW); y1 = Math.Clamp(y1, 0, mapH);
        int w = x1 - x0, h = y1 - y0;
        if (w <= 0 || h <= 0) return;
        var px = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var k = drawn[(y0 + y) * mapW + x0 + x];
                if (k == SketchKind.None) continue;
                var (r, g, b) = TrackSketch.Colour(k);
                var o = (y * w + x) * 4; px[o] = b; px[o + 1] = g; px[o + 2] = r; px[o + 3] = 255;
            }
        overlay.WritePixels(new Int32Rect(x0, y0, w, h), px, w * 4, 0);
    }

    // (each change kept to undo, the last 40)
    private void Remember()
    {
        undo.Push((SketchKind[])drawn.Clone());
        undoPlaced.Push(placed.ToList());
        if (undo.Count > 40)
        {
            var keep = undo.Take(40).Reverse().ToList(); undo.Clear(); foreach (var k in keep) undo.Push(k);
            var keepPlaced = undoPlaced.Take(40).Reverse().ToList(); undoPlaced.Clear(); foreach (var k in keepPlaced) undoPlaced.Push(k);
        }
    }
    private readonly Stack<List<Placed>> undoPlaced = new();

    private void Undo()
    {
        if (undo.Count == 0) return;
        drawn = undo.Pop();
        placed.Clear(); if (undoPlaced.Count > 0) placed.AddRange(undoPlaced.Pop());
        Redraw(0, 0, mapW, mapH); RedrawPieces();
    }

    private void Stamp(Point a, Point b, SketchKind kind, double radius)
    {
        var len = Math.Max(1, (int)Math.Ceiling((b - a).Length));
        for (var s = 0; s <= len; s++)
        {
            double x = a.X + (b.X - a.X) * s / len, y = a.Y + (b.Y - a.Y) * s / len;
            for (var yy = (int)(y - radius); yy <= (int)(y + radius); yy++)
                for (var xx = (int)(x - radius); xx <= (int)(x + radius); xx++)
                {
                    if (xx < 0 || yy < 0 || xx >= mapW || yy >= mapH || (xx - x) * (xx - x) + (yy - y) * (yy - y) > radius * radius) continue;
                    drawn[yy * mapW + xx] = kind;
                }
        }
        var r = (int)Math.Ceiling(radius) + 1;
        Redraw((int)Math.Min(a.X, b.X) - r, (int)Math.Min(a.Y, b.Y) - r, (int)Math.Max(a.X, b.X) + r + 1, (int)Math.Max(a.Y, b.Y) + r + 1);
    }

    private SketchKind PenKind => tool switch { Tool.Road => SketchKind.Road, Tool.Jump => SketchKind.Jump, Tool.Pit => SketchKind.Pit, Tool.Loop => SketchKind.Loop, _ => SketchKind.None };
    private double PenRadius => penWidth.Value * MapScale / (tool == Tool.Eraser ? 0.5 : 1);

    private void BoardDown(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(board);
        if (placing is not null && ghost is not null)
        {
            Remember();
            placed.Add(ghost with { });
            RedrawPieces();
            return;
        }
        if (tool == Tool.Move)
        {
            dragging = placed.LastOrDefault(pc => pc.Near(p, RoadCells * MapScale * 2));
            if (dragging is not null) { Remember(); dragFrom = p; board.CaptureMouse(); }
            return;
        }
        Remember();
        lastPen = p;
        Stamp(p, p, PenKind, PenRadius);
        board.CaptureMouse();
    }

    private void BoardMove(object sender, MouseEventArgs e)
    {
        var p = e.GetPosition(board);
        if (placing is not null) { ghost = Snapped(new Placed(placing, p.X, p.Y, placingHeading), null); RedrawPieces(); return; }
        if (dragging is not null && e.LeftButton == MouseButtonState.Pressed)
        {
            var moved = dragging with { X = dragging.X + p.X - dragFrom.X, Y = dragging.Y + p.Y - dragFrom.Y };
            var i = placed.IndexOf(dragging);
            if (i >= 0) { placed[i] = moved; dragging = moved; }
            dragFrom = p;
            RedrawPieces();
            return;
        }
        if (lastPen is { } last && e.LeftButton == MouseButtonState.Pressed) { Stamp(last, p, PenKind, PenRadius); lastPen = p; }
    }

    private void BoardUp(object sender, MouseButtonEventArgs e)
    {
        if (dragging is not null)
        {
            // (let go near an open end: snapped onto it)
            var i = placed.IndexOf(dragging);
            if (i >= 0) placed[i] = Snapped(dragging, dragging);
            dragging = null; RedrawPieces();
        }
        lastPen = null;
        board.ReleaseMouseCapture();
    }

    private void BoardRight(object sender, MouseButtonEventArgs e)
    {
        if (placing is not null) { StopPlacing(); return; }
        var p = e.GetPosition(board);
        if (placed.LastOrDefault(pc => pc.Near(p, RoadCells * MapScale * 2)) is { } hit) { Remember(); placed.Remove(hit); RedrawPieces(); }
    }

    private void Wheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            var f = e.Delta > 0 ? 1.15 : 1 / 1.15;
            zoom.ScaleX = zoom.ScaleY = Math.Clamp(zoom.ScaleX * f, 0.25, 6);
            e.Handled = true;
            return;
        }
        // (turning the section being placed, or the one under the mouse in Move)
        var turn = (e.Delta > 0 ? 1 : -1) * Math.PI / 12;
        if (placing is not null)
        {
            placingHeading += turn;
            ghost = Snapped(new Placed(placing, e.GetPosition(board).X, e.GetPosition(board).Y, placingHeading), null);
            RedrawPieces(); e.Handled = true;
        }
        else if (tool == Tool.Move && placed.LastOrDefault(pc => pc.Near(e.GetPosition(board), RoadCells * MapScale * 2)) is { } hit)
        {
            Remember();
            placed[placed.IndexOf(hit)] = hit with { Heading = hit.Heading + turn };
            RedrawPieces(); e.Handled = true;
        }
    }

    private void KeyPressed(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && placing is not null) { StopPlacing(); e.Handled = true; }
        if (e.Key == Key.Z && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && step == 1) { Undo(); e.Handled = true; }
    }

    // (LBA2_TRACKGEN_SECTIONS="x,y,degrees|name|name|...": those sections placed as the user would place them -- the first at (x, y) turned
    // so, each next dropped a few pixels off the open end of the last, snapping on -- the UI test's)
    private void TestSections()
    {
        if (Environment.GetEnvironmentVariable("LBA2_TRACKGEN_SECTIONS") is not { Length: > 0 } spec) return;
        var parts = spec.Split('|');
        var at = parts[0].Split(',').Select(v => double.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        double x = at[0], y = at[1], heading = at[2] * Math.PI / 180;
        var jitter = 0;
        foreach (var name in parts.Skip(1))
        {
            var section = Section.All.First(c => c.Name == name);
            var piece = Snapped(new Placed(section, x + (jitter % 3 - 1) * 4, y + (jitter % 2) * 5, heading + (jitter % 2) * 0.2), null);
            placed.Add(piece);
            (x, y, heading) = piece.End; jitter++;
        }
        RedrawPieces();
    }

    private void StartPlacing(Section s)
    {
        placing = s;
        tools.First(t => t.IsChecked == true).IsChecked = true;
        board.Cursor = Cursors.Cross;
    }

    private void StopPlacing()
    {
        placing = null; ghost = null; board.Cursor = null;
        RedrawPieces();
    }

    // A section near an open end of another (one placed: its start; or its end near another's start): put onto it, as it goes on from there
    private Placed Snapped(Placed piece, Placed? self)
    {
        var snap = SnapCells * MapScale;
        Placed? best = null; var bestD = snap;
        foreach (var (x, y, heading) in OpenEnds(self))
        {
            var d = Math.Sqrt((piece.X - x) * (piece.X - x) + (piece.Y - y) * (piece.Y - y));
            if (d < bestD) { bestD = d; best = piece with { X = x, Y = y, Heading = heading }; }
        }
        if (best is not null) return best;
        // (its end onto another's open start: placed so it runs into it)
        foreach (var (x, y, heading) in OpenStarts(self))
        {
            var d = Math.Sqrt((piece.End.X - x) * (piece.End.X - x) + (piece.End.Y - y) * (piece.End.Y - y));
            if (d >= bestD) continue;
            var turned = piece with { Heading = heading - piece.Section.Turn };
            var (ex, ey, _) = turned.End;
            bestD = d; best = turned with { X = turned.X + x - ex, Y = turned.Y + y - ey };
        }
        return best ?? piece;
    }

    private IEnumerable<(double X, double Y, double Heading)> OpenEnds(Placed? self)
    {
        foreach (var p in placed)
        {
            if (ReferenceEquals(p, self) || p.Section.Kind == SketchKind.Pit) continue;
            var e = p.End;
            if (placed.Any(o => !ReferenceEquals(o, p) && !ReferenceEquals(o, self) && Close(o.X, o.Y, e.X, e.Y))) continue;
            yield return e;
        }
    }

    private IEnumerable<(double X, double Y, double Heading)> OpenStarts(Placed? self)
    {
        foreach (var p in placed)
        {
            if (ReferenceEquals(p, self) || p.Section.Kind == SketchKind.Pit) continue;
            if (placed.Any(o => !ReferenceEquals(o, p) && !ReferenceEquals(o, self) && Close(o.End.X, o.End.Y, p.X, p.Y))) continue;
            yield return (p.X, p.Y, p.Heading);
        }
    }

    private static bool Close(double ax, double ay, double bx, double by) => (ax - bx) * (ax - bx) + (ay - by) * (ay - by) < Math.Pow(1.5 * MapScale, 2);

    private void RedrawPieces()
    {
        pieceCanvas.Children.Clear();
        foreach (var p in placed) DrawPiece(p, 1.0);
        if (ghost is not null) DrawPiece(ghost, 0.55);
        // (the open ends, so where the next one snaps on is seen)
        foreach (var (x, y, _) in OpenEnds(null))
        {
            var dot = new Ellipse { Width = 10, Height = 10, Stroke = Brushes.White, StrokeThickness = 2, Fill = Brushes.Black };
            Canvas.SetLeft(dot, x - 5); Canvas.SetTop(dot, y - 5);
            pieceCanvas.Children.Add(dot);
        }
    }

    private void DrawPiece(Placed p, double opacity)
    {
        foreach (var (kind, points) in p.Strokes())
        {
            var (r, g, b) = TrackSketch.Colour(kind);
            var line = new Polyline { Stroke = new SolidColorBrush(Color.FromRgb(r, g, b)), StrokeThickness = RoadCells * 2 * MapScale, Opacity = opacity,
                StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, IsHitTestVisible = false };
            foreach (var pt in points) line.Points.Add(pt);
            pieceCanvas.Children.Add(line);
        }
        // (its way: an arrow at its end)
        var (ex, ey, eh) = p.End;
        var arrow = new Polygon { Fill = Brushes.White, Opacity = opacity, IsHitTestVisible = false };
        arrow.Points.Add(new Point(ex + Math.Cos(eh) * 6, ey + Math.Sin(eh) * 6));
        arrow.Points.Add(new Point(ex + Math.Cos(eh + 2.5) * 5, ey + Math.Sin(eh + 2.5) * 5));
        arrow.Points.Add(new Point(ex + Math.Cos(eh - 2.5) * 5, ey + Math.Sin(eh - 2.5) * 5));
        pieceCanvas.Children.Add(arrow);
    }

    private void SaveMap()
    {
        if (island is null) return;
        var dialog = new SaveFileDialog { Title = "Save the island's map", Filter = "PNG picture (*.png)|*.png", FileName = System.IO.Path.GetFileNameWithoutExtension(island.IleFile).ToLowerInvariant() + "_map.png" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(mapW, mapH, 96, 96, PixelFormats.Bgra32, null, mapPixels, mapW * 4)));
            using var file = File.Create(dialog.FileName);
            encoder.Save(file);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"The map can't be saved: {error.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void LoadPicture()
    {
        // (LBA2_TRACKGEN_PICTURE: that picture, no dialog -- the UI test's)
        var path = Environment.GetEnvironmentVariable("LBA2_TRACKGEN_PICTURE");
        if (string.IsNullOrEmpty(path))
        {
            var dialog = new OpenFileDialog { Title = "Load the map with the track drawn on it", Filter = "Pictures (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*" };
            if (dialog.ShowDialog(this) != true) return;
            path = dialog.FileName;
        }
        try
        {
            var decoder = BitmapDecoder.Create(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var bmp = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
            int pw = bmp.PixelWidth, ph = bmp.PixelHeight;
            var px = new byte[pw * ph * 4];
            bmp.CopyPixels(px, pw * 4, 0);
            var aspect = (double)pw / ph / ((double)mapW / mapH);
            var sketch = TrackSketch.FromImage(px, pw, ph, mapPixels, mapW, mapH, MapScale, originX, originZ);
            var found = sketch.Kinds.Count(k => k != SketchKind.None);
            if (found == 0)
            {
                MessageBox.Show(this, "Nothing is drawn on that picture in the pens' colours (green, pink, blue, orange) -- draw on the map picture this window saves.", Title, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Remember();
            for (var i = 0; i < drawn.Length; i++) if (sketch.Kinds[i] != SketchKind.None) drawn[i] = sketch.Kinds[i];
            Redraw(0, 0, mapW, mapH);
            if (Math.Abs(aspect - 1) > 0.03)
                MessageBox.Show(this, "The picture isn't the map's shape: it was stretched to fit. If the drawing is out of place, draw on the map picture this window saves (any size, the whole of it).", Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception error) when (error is IOException or NotSupportedException or ArgumentException or InvalidOperationException or FileFormatException)
        {
            MessageBox.Show(this, $"The picture can't be read: {error.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ---- page 3: the track

    // What is drawn and placed together, the sections' open ends joined, followed into a plan
    private bool MakeTrack()
    {
        if (island is null) return false;
        var sketch = new TrackSketch(mapW, mapH, MapScale, originX, originZ);
        Array.Copy(drawn, sketch.Kinds, drawn.Length);
        var radius = RoadCells * MapScale;
        void StampInto(Point a, Point b, SketchKind kind)
        {
            var len = Math.Max(1, (int)Math.Ceiling((b - a).Length));
            for (var s = 0; s <= len; s++)
            {
                double x = a.X + (b.X - a.X) * s / len, y = a.Y + (b.Y - a.Y) * s / len;
                for (var yy = (int)(y - radius); yy <= (int)(y + radius); yy++)
                    for (var xx = (int)(x - radius); xx <= (int)(x + radius); xx++)
                        if (xx >= 0 && yy >= 0 && xx < mapW && yy < mapH && (xx - x) * (xx - x) + (yy - y) * (yy - y) <= radius * radius) sketch.Kinds[yy * mapW + xx] = kind;
            }
        }
        foreach (var p in placed)
            foreach (var (kind, points) in p.Strokes())
                for (var i = 0; i + 1 < points.Count; i++) StampInto(points[i], points[i + 1], kind);
        // (the sections' open ends near each other's open starts: joined -- they needn't be placed exactly)
        var ends = OpenEnds(null).ToList(); var starts = OpenStarts(null).ToList();
        foreach (var (ex, ey, _) in ends)
        {
            var near = starts.Where(s => Math.Sqrt((s.X - ex) * (s.X - ex) + (s.Y - ey) * (s.Y - ey)) < BridgeCells * MapScale)
                .OrderBy(s => (s.X - ex) * (s.X - ex) + (s.Y - ey) * (s.Y - ey)).FirstOrDefault();
            if (near != default) StampInto(new Point(ex, ey), new Point(near.X, near.Y), SketchKind.Road);
        }
        // (nothing drawn by hand on the road -- only sections, in one chain: the chain's own middle, as it is)
        var handRoad = drawn.Any(k => k is SketchKind.Road or SketchKind.Jump or SketchKind.Loop);
        var chainClosed = false;
        var chain = handRoad ? null : Chain(out chainClosed);
        result = chain is not null ? TrackSketchTracer.Trace(sketch, islandFile, reverse.IsChecked == true, chain, chainClosed)
            : TrackSketchTracer.Trace(sketch, islandFile, reverse.IsChecked == true);
        ShowTrack();
        return result.Plan is not null;
    }

    // The road sections placed, end to start, in one chain (each joined to the nearest open start within BridgeCells of its end): their
    // middle line in order, and whether the chain comes back round to its first; null when they aren't one chain
    private List<(double X, double Y)>? Chain(out bool closed)
    {
        closed = false;
        var roads = placed.Where(p => p.Section.Kind != SketchKind.Pit).ToList();
        if (roads.Count == 0) return null;
        var bridge = BridgeCells * MapScale;
        double Gap(Placed a, Placed b) => Math.Sqrt((a.End.X - b.X) * (a.End.X - b.X) + (a.End.Y - b.Y) * (a.End.Y - b.Y));
        Placed? Next(Placed a, IEnumerable<Placed> from) => from.Where(b => !ReferenceEquals(a, b) && Gap(a, b) < bridge).OrderBy(b => Gap(a, b)).FirstOrDefault();
        // (the first: one nothing leads into, or any of a closed ring)
        var first = roads.FirstOrDefault(r => !roads.Any(o => !ReferenceEquals(o, r) && ReferenceEquals(Next(o, roads), r))) ?? roads[0];
        var order = new List<Placed> { first };
        while (Next(order[^1], roads.Except(order)) is { } n) order.Add(n);
        if (order.Count != roads.Count) return null;
        closed = order.Count > 1 && Gap(order[^1], order[0]) < bridge;
        var line = new List<(double X, double Y)>();
        foreach (var p in order)
            foreach (var (_, pts) in p.Strokes())
                foreach (var pt in pts)
                    if (line.Count == 0 || Math.Abs(line[^1].X - pt.X) + Math.Abs(line[^1].Y - pt.Y) > 0.5) line.Add((pt.X, pt.Y));
        return line;
    }

    private void ShowTrack()
    {
        if (result is null) return;
        previewBoard.Children.Clear();
        previewBoard.Width = mapW; previewBoard.Height = mapH;
        var img = new Image { Source = mapImage.Source, Stretch = Stretch.None };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
        previewBoard.Children.Add(img);
        var canvas = new Canvas { Width = mapW, Height = mapH };
        previewBoard.Children.Add(canvas);
        previewZoom.ScaleX = previewZoom.ScaleY = zoom.ScaleX;
        Point At((double X, double Z) c) => new((c.X - originX) * MapScale, (c.Z - originZ) * MapScale);
        // the road found, a stretch at a time in its kind's colour
        var route = result.Route;
        for (var i = 0; i + 1 < route.Count + (result.Closed ? 1 : 0); i++)
        {
            var kind = result.RouteKinds[Math.Min(i, result.RouteKinds.Count - 1)];
            var color = kind switch { SketchKind.Jump => Color.FromRgb(255, 0, 255), SketchKind.Loop => Color.FromRgb(0, 255, 255), _ => Color.FromRgb(255, 225, 0) };
            canvas.Children.Add(new Line { X1 = At(route[i]).X, Y1 = At(route[i]).Y, X2 = At(route[(i + 1) % route.Count]).X, Y2 = At(route[(i + 1) % route.Count]).Y,
                Stroke = new SolidColorBrush(color), StrokeThickness = 3, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
        }
        // its way round: arrows every so often
        for (var i = 0; i + 3 < route.Count; i += 40)
        {
            var a = At(route[i]); var b = At(route[i + 3]);
            var h = Math.Atan2(b.Y - a.Y, b.X - a.X);
            var arrow = new Polygon { Fill = Brushes.White, Stroke = Brushes.Black, StrokeThickness = 1 };
            arrow.Points.Add(new Point(a.X + Math.Cos(h) * 9, a.Y + Math.Sin(h) * 9));
            arrow.Points.Add(new Point(a.X + Math.Cos(h + 2.5) * 7, a.Y + Math.Sin(h + 2.5) * 7));
            arrow.Points.Add(new Point(a.X + Math.Cos(h - 2.5) * 7, a.Y + Math.Sin(h - 2.5) * 7));
            canvas.Children.Add(arrow);
        }
        if (result.PitA is { } pa && result.PitB is { } pb)
            canvas.Children.Add(new Line { X1 = At(pa).X, Y1 = At(pa).Y, X2 = At(pb).X, Y2 = At(pb).Y, Stroke = Brushes.Orange, StrokeThickness = 3, StrokeDashArray = new DoubleCollection { 2, 2 } });
        if (result.Start >= 0 && result.Start < route.Count)
        {
            var s = At(route[result.Start]);
            var flag = new Rectangle { Width = 14, Height = 14, Fill = Brushes.White, Stroke = Brushes.Black, StrokeThickness = 2 };
            Canvas.SetLeft(flag, s.X - 7); Canvas.SetTop(flag, s.Y - 7);
            canvas.Children.Add(flag);
        }
        var text = new List<string>();
        foreach (var p in result.Problems) text.Add("✖ " + p);
        foreach (var n in result.Notes) text.Add("• " + n);
        if (result.Plan is not null) text.Add("The track is ready: Save the plan, or Build to build it with the race track window (the island ticked, this plan chosen). Wrong way round? Tick \"The other way round\".");
        report.Text = string.Join(Environment.NewLine, text);
        // (LBA2_TRACKGEN_SAVE: the plan written there as soon as it is made -- the UI test's)
        if (Environment.GetEnvironmentVariable("LBA2_TRACKGEN_SAVE") is { Length: > 0 } testPath && result.Plan is not null) SavePlan(testPath);
        savePlanButton.IsEnabled = buildButton.IsEnabled = result.Plan is not null;
    }

    private string? SavePlan(string? path)
    {
        if (result?.Plan is not { } plan || island is null) return null;
        if (path is null)
        {
            var dialog = new SaveFileDialog { Title = "Save the track plan", Filter = "Race track plan (*.json)|*.json", FileName = System.IO.Path.GetFileNameWithoutExtension(island.IleFile).ToLowerInvariant() + "_drawn_track_plan.json" };
            if (dialog.ShowDialog(this) != true) return null;
            path = dialog.FileName;
        }
        try
        {
            var json = JsonSerializer.Serialize(plan, new JsonSerializerOptions
            {
                WriteIndented = false, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            });
            File.WriteAllText(path, $"// {island.Name}'s track, made by LBA Assembler's race track generator from a drawing, {DateTime.Now:yyyy-MM-dd HH:mm}{Environment.NewLine}{json}{Environment.NewLine}");
            return path;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"The plan can't be saved: {error.Message}", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }
    }

    // The plan saved (beside the editor, TrackPlans\) and handed to the race track window
    private void Build()
    {
        if (island is null) return;
        var folder = System.IO.Path.Combine(AppContext.BaseDirectory, "TrackPlans");
        try { Directory.CreateDirectory(folder); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        var path = SavePlan(System.IO.Path.Combine(folder, System.IO.Path.GetFileNameWithoutExtension(island.IleFile).ToLowerInvariant() + $"_drawn_{DateTime.Now:yyyyMMdd_HHmmss}.json"));
        if (path is null) return;
        BuildIsland = island; BuildPlan = path;
        DialogResult = true;
    }

    // ---- track sections

    // A section's parts in turn: a straight (length, cells) or an arc (radius, cells, and its turn, radians: + to the right), each of a kind
    internal sealed record Part(SketchKind Kind, double Length, double Radius = 0, double Turn = 0);

    internal sealed record Section(string Name, string Tip, Part[] Parts)
    {
        public SketchKind Kind => Parts.Any(p => p.Kind == SketchKind.Pit) ? SketchKind.Pit : SketchKind.Road;
        public double Turn => Parts.Sum(p => p.Turn);

        public static readonly Section[] All =
        {
            new("Straight", "10 cells of road", new[] { new Part(SketchKind.Road, 10) }),
            new("Long straight", "30 cells of road", new[] { new Part(SketchKind.Road, 30) }),
            new("Left 45°", "A bend to the left, 16 cells round", new[] { Arc(16, -45) }),
            new("Right 45°", "A bend to the right, 16 cells round", new[] { Arc(16, 45) }),
            new("Left 90°", "A bend to the left, 16 cells round", new[] { Arc(16, -90) }),
            new("Right 90°", "A bend to the right, 16 cells round", new[] { Arc(16, 90) }),
            new("Hairpin left", "Round to the left, 12 cells round", new[] { Arc(12, -180) }),
            new("Hairpin right", "Round to the right, 12 cells round", new[] { Arc(12, 180) }),
            new("Jump", "Road, a jump over 18 cells, road", new[] { new Part(SketchKind.Road, 12), new Part(SketchKind.Jump, 18), new Part(SketchKind.Road, 14) }),
            new("Loop", "Road, a vertical loop, road", new[] { new Part(SketchKind.Road, 16), new Part(SketchKind.Loop, 6), new Part(SketchKind.Road, 16) }),
            new("Pit lane", "40 cells of pit lane: place it beside a straight", new[] { new Part(SketchKind.Pit, 40) }),
        };

        private static Part Arc(double radius, double degrees) => new(SketchKind.Road, Math.Abs(degrees) * Math.PI / 180 * radius, radius, degrees * Math.PI / 180);
    }

    // A section placed: where it begins (map pixels) and its way (radians, 0 along +x, + clockwise on the map)
    internal sealed record Placed(Section Section, double X, double Y, double Heading)
    {
        public List<(SketchKind Kind, List<Point> Points)> Strokes()
        {
            var strokes = new List<(SketchKind, List<Point>)>();
            double x = X, y = Y, h = Heading;
            foreach (var part in Section.Parts)
            {
                var pts = new List<Point> { new(x, y) };
                var n = Math.Max(2, (int)Math.Ceiling(part.Length * 2));
                for (var i = 1; i <= n; i++)
                {
                    var ds = part.Length / n * MapScale;
                    if (part.Radius > 0) h += part.Turn / n;
                    // (an arc: each small step on along the way turned so far, half of this step's turn in it)
                    var mid = part.Radius > 0 ? h - part.Turn / n / 2 : h;
                    x += Math.Cos(mid) * ds; y += Math.Sin(mid) * ds;
                    pts.Add(new Point(x, y));
                }
                strokes.Add((part.Kind, pts));
            }
            return strokes;
        }

        public (double X, double Y, double Heading) End
        {
            get
            {
                var s = Strokes(); var last = s[^1].Points[^1];
                return (last.X, last.Y, Heading + Section.Turn);
            }
        }

        public bool Near(Point p, double within) => Strokes().Any(s => s.Points.Any(q => (q - p).Length < within));
    }
}
