using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LBAAssembler.Terrain;
using Section = LBAAssembler.Terrain.TrackSection;

namespace LBAAssembler;

// Build > Race track: a race track laid in the 3D view from the generator's track pieces (the user, 2026-11-03: "improve the track generation
// by allowing a user to pick an island in our main viewport and by selecting pre-drawn track pieces drag and drop them. This should help with
// making vertical tracks as this viewport is fully turnable and true to size"). A piece chosen in the panel follows the pointer over the
// ground, true to size, and drops where it is clicked: on from the nearest open end within reach (at that end's height and way), else on the
// ground under the pointer at the height chosen (PgUp, PgDn). Q and E turn it, a piece laid is picked up again by dragging it, Delete takes
// it away. The pieces are kept for the island (TrackPlans\<island>_pieces.json beside the editor); Make the track turns them into a plan
// through the generator's tracer (TrackPieceSet.MakePlan), and Build it... hands that to the race track window, as the generator does.
public partial class MainWindow
{
    private bool buildTrackView;                 // Build on an island: the track pieces (BuildTrackRadio)
    private readonly TrackPieceSet trackPieces = new();
    private readonly Stack<List<TrackPiece>> trackUndo = new();
    private Section? trackArmed;                 // the panel's piece being laid
    private TrackPiece? trackGhost;              // ... where it would go
    private double trackHeading;                 // its way while it follows the pointer (Q, E)
    private double trackLift;                    // how high over the ground it goes where it joins nothing (PgUp, PgDn)
    private TrackPiece? trackSelected;
    private TrackPiece? trackDragging;           // the piece picked up and moving, as it was
    private (double X, double Z) trackDragOffset;
    private string trackIsland = "";             // the island file the pieces are of
    private Canvas? trackCanvas;
    private TextBlock? trackStatus;
    private Button? trackBuildButton;
    private string? trackPlanPath;
    private const double TrackTurnStep = Math.PI / 12, TrackLiftStep = 250;

    private bool TrackPiecesActive => editMode == EditMode.Build && buildTrackView && TerrainEditable;

    // The island the open file is, as a race track's (Citadel Island's two files one island)
    private RaceTrackIsland? TrackIslandOfView() => RaceTrackIsland.BuiltIn.FirstOrDefault(i =>
        string.Equals(i.IleFile, activeFile, StringComparison.OrdinalIgnoreCase) || string.Equals(i.TwinIleFile, activeFile, StringComparison.OrdinalIgnoreCase));

    private static string TrackPiecesFolder => System.IO.Path.Combine(AppContext.BaseDirectory, "TrackPlans");
    private string TrackPiecesFile(string ile) => System.IO.Path.Combine(TrackPiecesFolder, System.IO.Path.GetFileNameWithoutExtension(ile).ToLowerInvariant() + "_pieces.json");

    // ---- the panel ------------------------------------------------------------------------------------------------------------------------

    private UIElement TrackPanel()
    {
        var root = new StackPanel { Margin = new Thickness(16, 8, 16, 8) };
        TextBlock Text(string s, double top = 6, bool muted = true)
        {
            var t = new TextBlock { Text = s, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, top, 0, 0) };
            t.SetResourceReference(TextBlock.ForegroundProperty, muted ? "ThemeTextMuted2Brush" : "ThemeTextBrush");
            return t;
        }
        Button Btn(string s, string tip, Action click)
        {
            var b = new Button { Content = s, ToolTip = tip, Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 6, 6) };
            b.Click += (_, _) => { click(); Keyboard.Focus(this); };
            return b;
        }
        var pieces = new WrapPanel { Margin = new Thickness(0, 0, 0, 0) };
        foreach (var section in Section.All)
            pieces.Children.Add(Btn(section.Name, section.Tip, () => ArmTrackPiece(section)));
        root.Children.Add(pieces);
        var row = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
        row.Children.Add(Btn("Turn left (Q)", "Turn the piece being laid, or the one selected, 15 degrees", () => TurnTrackPiece(-1)));
        row.Children.Add(Btn("Turn right (E)", "Turn the piece being laid, or the one selected, 15 degrees", () => TurnTrackPiece(1)));
        row.Children.Add(Btn("Up (PgUp)", "Raise the piece being laid (where it joins nothing), or the one selected, by 250", () => LiftTrackPiece(1)));
        row.Children.Add(Btn("Down (PgDn)", "Lower it by 250", () => LiftTrackPiece(-1)));
        root.Children.Add(row);
        var edit = new WrapPanel();
        edit.Children.Add(Btn("Delete (Del)", "Take the selected piece away", DeleteTrackPiece));
        edit.Children.Add(Btn("Undo", "Take back the last change to the pieces", UndoTrackPieces));
        edit.Children.Add(Btn("Clear all", "Take every piece away", () =>
        {
            if (trackPieces.Pieces.Count == 0 || MessageBox.Show(this, "Take every piece away?", "Race track", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
            RememberTrackPieces(); trackPieces.Pieces.Clear(); trackSelected = null; TrackPiecesChanged();
        }));
        root.Children.Add(edit);
        var make = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        make.Children.Add(Btn("Make the track", "Follow the pieces into a race track plan: the road's middle, its heights, jumps, loops and pit lane", MakeTrackFromPieces));
        trackBuildButton = Btn("Build it…", "The plan to the race track window, to build into the game folder (or share for LBRA from there)", BuildTrackFromPieces);
        trackBuildButton.IsEnabled = false;
        make.Children.Add(trackBuildButton);
        root.Children.Add(make);
        trackStatus = Text("", 6, false);
        root.Children.Add(trackStatus);
        // (how it works, under the pieces: the buttons first, where they are seen)
        root.Children.Add(Text("Lay a race track in the 3D view from the generator's pieces, true to size. Choose a piece, then click on the island: " +
                               "it goes on from the nearest open end (the yellow rings) at that end's height, or onto the ground under the pointer. " +
                               "Q and E turn it, PgUp and PgDn raise and lower it, Esc stops laying. Drag a piece to move it, Delete takes the selected one away. " +
                               "Right drag turns the view, middle drag pans it.", 10));
        return new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = root };
    }

    // Brings the track pieces in line with the mode and the island: the panel, the island's pieces, the overlay
    private void SyncTrackPieces()
    {
        var on = TrackPiecesActive;
        TrackPanelHost.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        if (on && TrackPanelHost.Content is null) TrackPanelHost.Content = TrackPanel();
        if (on && !string.Equals(trackIsland, activeFile, StringComparison.OrdinalIgnoreCase)) LoadTrackPieces();
        if (!on) { trackArmed = null; trackGhost = null; trackDragging = null; }
        else RunTrackPiecesTest();
        if (on)
        {
            var island = TrackIslandOfView();
            SetTrackStatus(island is null ? $"{activeFile} isn't one of the race tracks' islands: open one of theirs (the island box) to lay a track on it."
                : !nativeViewActive ? "The pieces are laid in the 3D view: turn it on (View)."
                : $"{trackPieces.Pieces.Count} piece(s) on {island.Shown}. Choose a piece to lay." + ScaledNote(island));
        }
        DrawTrackOverlay();
    }

    // (an island its track's build makes bigger: the plan's cells are the bigger island's -- the view must show it as built)
    private string ScaledNote(RaceTrackIsland island)
    {
        if (island.Scale <= 1 || SceneGround() is not { } shown) return "";
        var (x0, z0, x1, z1) = shown.PresentBounds();
        var original = System.IO.Path.Combine(gameRoot, island.IleFile);
        var source = File.Exists(original + RaceTrackService.BackupSuffix) ? original + RaceTrackService.BackupSuffix : original;
        try
        {
            var (a0, b0, a1, b1) = IslandScaler.AsBuilt(IslandFile.Load(source), island, new List<string>()).PresentBounds();
            if (x1 - x0 == a1 - a0 && z1 - z0 == b1 - b0) return "";
        }
        catch (Exception e) when (e is IOException or InvalidDataException) { return ""; }
        return $" {island.Shown} is built {island.Scale} times its size for its track: build its track once (Tools > LBA2: race track) so the view shows it as it will be, or the pieces won't match.";
    }

    private void SetTrackStatus(string text) { if (trackStatus is not null) trackStatus.Text = text; }

    private void ArmTrackPiece(Section section)
    {
        if (!TrackPiecesActive) return;
        trackArmed = section; trackGhost = null; trackSelected = null;
        SetTrackStatus($"{section.Name}: click on the island to lay it ({section.Tip}). Q, E turn it; PgUp, PgDn raise it; Esc stops.");
        TerrainViewport.Cursor = Cursors.Cross;
        DrawTrackOverlay();
    }

    private void DisarmTrackPiece()
    {
        trackArmed = null; trackGhost = null;
        TerrainViewport.Cursor = null;
        DrawTrackOverlay();
    }

    private void TurnTrackPiece(int way)
    {
        if (trackArmed is not null) { trackHeading += way * TrackTurnStep; if (trackGhost is { } g) trackGhost = trackPieces.Snapped(g with { Heading = g.Heading + way * TrackTurnStep }); }
        else if (trackSelected is { } s) ReplaceTrackPiece(s, s with { Heading = s.Heading + way * TrackTurnStep });
        DrawTrackOverlay();
    }

    private void LiftTrackPiece(int way)
    {
        if (trackArmed is not null) { trackLift = Math.Max(0, trackLift + way * TrackLiftStep); if (trackGhost is { } g) trackGhost = g with { Y = g.Y + way * TrackLiftStep }; SetTrackStatus($"Laid {trackLift:0} over the ground where it joins nothing."); }
        else if (trackSelected is { } s) ReplaceTrackPiece(s, s with { Y = s.Y + way * TrackLiftStep });
        DrawTrackOverlay();
    }

    private void DeleteTrackPiece()
    {
        if (trackSelected is not { } s) return;
        RememberTrackPieces();
        trackPieces.Pieces.Remove(s); trackSelected = null;
        TrackPiecesChanged();
    }

    private void ReplaceTrackPiece(TrackPiece old, TrackPiece now)
    {
        var i = trackPieces.Pieces.IndexOf(old);
        if (i < 0) return;
        RememberTrackPieces();
        trackPieces.Pieces[i] = now; trackSelected = now;
        TrackPiecesChanged();
    }

    private void RememberTrackPieces()
    {
        trackUndo.Push(trackPieces.Pieces.ToList());
        trackPlanPath = null;
        if (trackBuildButton is not null) trackBuildButton.IsEnabled = false;
    }

    private void UndoTrackPieces()
    {
        if (trackUndo.Count == 0) return;
        trackPieces.Pieces.Clear(); trackPieces.Pieces.AddRange(trackUndo.Pop()); trackSelected = null;
        TrackPiecesChanged();
    }

    private void TrackPiecesChanged()
    {
        SaveTrackPieces();
        DrawTrackOverlay();
        if (trackArmed is null) SetTrackStatus($"{trackPieces.Pieces.Count} piece(s). Make the track when they are one road.");
    }

    // ---- kept for the island ------------------------------------------------------------------------------------------------------------

    private sealed record SavedPiece(string Section, double X, double Z, double Y, double Heading);

    private void SaveTrackPieces()
    {
        if (trackIsland.Length == 0) return;
        try
        {
            Directory.CreateDirectory(TrackPiecesFolder);
            var saved = trackPieces.Pieces.Select(p => new SavedPiece(p.Section.Name, p.X, p.Z, p.Y, p.Heading)).ToList();
            File.WriteAllText(TrackPiecesFile(trackIsland), JsonSerializer.Serialize(saved, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { DebugLog.Log($"track pieces not saved: {e.Message}"); }
    }

    private void LoadTrackPieces()
    {
        trackIsland = activeFile ?? "";
        trackPieces.Pieces.Clear(); trackUndo.Clear(); trackSelected = null; trackPlanPath = null;
        if (trackBuildButton is not null) trackBuildButton.IsEnabled = false;
        var file = TrackPiecesFile(trackIsland);
        if (!File.Exists(file)) return;
        try
        {
            foreach (var s in JsonSerializer.Deserialize<List<SavedPiece>>(File.ReadAllText(file)) ?? new())
                if (Section.All.FirstOrDefault(a => a.Name == s.Section) is { } section) trackPieces.Pieces.Add(new TrackPiece(section, s.X, s.Z, s.Y, s.Heading));
        }
        catch (Exception e) when (e is IOException or JsonException) { DebugLog.Log($"track pieces not read: {e.Message}"); }
    }

    // ---- the mouse and the keys -----------------------------------------------------------------------------------------------------------

    // The ground under a point of the view, island cells, and its height there; false off the island
    private bool TrackGround(Point at, out double x, out double z, out double y)
    {
        x = z = y = 0;
        if (SceneGround() is not { } island) return false;
        var (lo, hi) = IslandOps.HeightRange(island);
        double Altitude(double wx, double wz) => IslandOps.Altitude(island, wx, wz) is double a ? a : double.NaN;
        if (!PickGroundWith(at, lo, hi, Altitude, out var wx, out var wz)) return false;
        x = wx / 512; z = wz / 512; y = Math.Max(0, Altitude(wx, wz));
        return true;
    }

    // Left button: lay the piece being laid, or pick up the one under the pointer. True: taken.
    private bool HandleTrackMouseDown(MouseButtonEventArgs e)
    {
        if (!TrackPiecesActive || e.ChangedButton != MouseButton.Left) return false;
        var at = e.GetPosition(TerrainViewport);
        if (trackArmed is not null)
        {
            LayTrackPiece(at);
            e.Handled = true;
            return true;
        }
        var hit = TrackPieceAt(at);
        trackSelected = hit;
        if (hit is not null && TrackGround(at, out var gx, out var gz, out _))
        {
            trackDragging = hit; trackDragOffset = (hit.X - gx, hit.Z - gz);
            RememberTrackPieces();
            TerrainViewport.CaptureMouse();
            SetTrackStatus($"{hit.Section.Name}: drag to move it, Q, E to turn it, PgUp, PgDn to raise it, Delete to take it away.");
        }
        DrawTrackOverlay();
        if (hit is null) return false;
        e.Handled = true;
        return true;
    }

    // The piece being laid put down where the pointer at `at` has it
    private bool LayTrackPiece(Point at)
    {
        UpdateTrackGhost(at);
        if (trackGhost is not { } g) { SetTrackStatus("Point at the island's ground to lay the piece."); return false; }
        RememberTrackPieces();
        trackPieces.Pieces.Add(g);
        // (the next one of the same goes on from it: its way kept)
        trackHeading = g.End.Heading;
        TrackPiecesChanged();
        SetTrackStatus($"{g.Section.Name} laid ({trackPieces.Pieces.Count} pieces). Click again for another, or choose another piece; Esc stops.");
        UpdateTrackGhost(at);
        return true;
    }

    // Tests (LBA2_TRACKPIECES_TEST="x,z,lift,degrees|piece|piece|..."): the pieces laid through the pointer's own path -- each piece chosen,
    // the pointer put where the view draws its target's ground (the first at cell x,z; each next just past the last one's end) and clicked
    // -- then the track made; the status line said in the log. Once, as the mode first shows an island in the 3D view.
    private bool trackTestRun;
    private void RunTrackPiecesTest()
    {
        if (trackTestRun || Environment.GetEnvironmentVariable("LBA2_TRACKPIECES_TEST") is not { Length: > 0 } spec) return;
        trackTestRun = true;
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        var tries = 0;
        var framed = false;
        timer.Tick += (_, _) =>
        {
            if (cameraModel is null && ++tries < 60) return;
            // (the view over the track's start, high and looking down, first: the pointer's targets on the screen and none behind a hill)
            if (!framed && SceneGround() is { } g0)
            {
                framed = true;
                var s0 = spec.Split('|')[0].Split(',').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                targetX = s0[0] * 512 + 12000; targetZ = s0[1] * 512 + 4000; targetY = IslandOps.Altitude(g0, targetX, targetZ) ?? 2000;
                nativeAlpha = 700; nativeDistance = 60000;
                RenderNativeCamera();
                tries = 0;
                return;
            }
            if (++tries < 6) return;
            timer.Stop();
            if (cameraModel is not { } model || SceneGround() is not { } ground) { DebugLog.Log("track pieces test: no 3D view"); return; }
            Point? Screen(double x, double z, double y)
            {
                if (!model.Project(x * 512, y, z * 512, out var u, out var v)) return null;
                return ToWidget(model, u, v);
            }
            var items = spec.Split('|');
            var first = items[0].Split(',').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            trackPieces.Pieces.Clear();
            trackLift = first[2]; trackHeading = first[3] * Math.PI / 180;
            var laid = 0;
            foreach (var name in items.Skip(1))
            {
                if (Section.All.FirstOrDefault(s => s.Name == name) is not { } section) { DebugLog.Log($"track pieces test: no piece {name}"); continue; }
                ArmTrackPiece(section);
                double x = first[0], z = first[1];
                if (trackPieces.Pieces.LastOrDefault() is { } last) { var e = last.End; x = e.X + Math.Cos(e.Heading) * 0.6; z = e.Z + Math.Sin(e.Heading) * 0.6; }
                var gy = IslandOps.Altitude(ground, x * 512, z * 512) ?? 0;
                if (Screen(x, z, gy) is { } at && LayTrackPiece(at))
                {
                    laid++;
                    var p = trackPieces.Pieces[^1];
                    DebugLog.Log($"track pieces test: {name} aimed at ({x:0.0}, {z:0.0}), laid at ({p.X:0.0}, {p.Z:0.0}) y {p.Y:0}, open ends {trackPieces.OpenEnds().Count()}");
                }
                else DebugLog.Log($"track pieces test: {name} at ({x:0.0}, {z:0.0}) not laid (off the view or the island)");
            }
            DisarmTrackPiece();
            MakeTrackFromPieces();
            DebugLog.Log($"track pieces test: {laid} of {items.Length - 1} laid; {trackStatus?.Text}");
        };
        timer.Start();
    }

    private bool HandleTrackMouseMove(MouseEventArgs e)
    {
        if (!TrackPiecesActive) return false;
        var at = e.GetPosition(TerrainViewport);
        if (trackDragging is { } held && trackSelected is { } current)
        {
            if (!TrackGround(at, out var gx, out var gz, out _)) return true;
            var moved = trackPieces.Snapped(current with { X = gx + trackDragOffset.X, Z = gz + trackDragOffset.Z, Y = held.Y, Heading = held.Heading }, current);
            var i = trackPieces.Pieces.IndexOf(current);
            if (i >= 0) { trackPieces.Pieces[i] = moved; trackSelected = moved; }
            DrawTrackOverlay();
            return true;
        }
        if (trackArmed is null) return false;
        UpdateTrackGhost(at);
        DrawTrackOverlay();
        return false;
    }

    private bool HandleTrackMouseUp(MouseButtonEventArgs e)
    {
        if (trackDragging is null || e.ChangedButton != MouseButton.Left) return false;
        trackDragging = null;
        Mouse.Capture(null);
        TrackPiecesChanged();
        return true;
    }

    private void UpdateTrackGhost(Point at)
    {
        if (trackArmed is not { } section || !TrackGround(at, out var gx, out var gz, out var gy)) { trackGhost = null; return; }
        trackGhost = trackPieces.Snapped(new TrackPiece(section, gx, gz, gy + trackLift, trackHeading));
    }

    // The piece whose middle line passes nearest a point of the view, within 10 pixels
    private TrackPiece? TrackPieceAt(Point at)
    {
        if (cameraModel is not { } model) return null;
        TrackPiece? best = null; var nearest = 10.0;
        foreach (var p in trackPieces.Pieces)
        {
            Point? last = null;
            foreach (var q in p.Line())
            {
                if (!model.Project(q.X * 512, q.Y, q.Z * 512, out var u, out var v)) { last = null; continue; }
                var s = ToWidget(model, u, v);
                if (last is { } a)
                {
                    var d = DistanceToSegment(at, a, s);
                    if (d < nearest) { nearest = d; best = p; }
                }
                last = s;
            }
        }
        return best;
    }

    private static double DistanceToSegment(Point p, Point a, Point b)
    {
        var ab = b - a; var len = ab.LengthSquared;
        var t = len < 1e-9 ? 0 : Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / len, 0, 1);
        return (p - (a + ab * t)).Length;
    }

    // Q, E, PgUp, PgDn, Delete, Esc while laying or with a piece selected. True: taken.
    private bool HandleTrackKey(KeyEventArgs e)
    {
        if (!TrackPiecesActive || Keyboard.FocusedElement is TextBox) return false;
        switch (e.Key)
        {
            case Key.Q: TurnTrackPiece(-1); break;
            case Key.E: TurnTrackPiece(1); break;
            case Key.PageUp: LiftTrackPiece(1); break;
            case Key.PageDown: LiftTrackPiece(-1); break;
            case Key.Delete when trackArmed is null && trackSelected is not null: DeleteTrackPiece(); break;
            case Key.Escape when trackArmed is not null: DisarmTrackPiece(); SetTrackStatus("Stopped laying. Choose a piece to lay another."); break;
            case Key.Escape when trackSelected is not null: trackSelected = null; DrawTrackOverlay(); break;
            default: return false;
        }
        e.Handled = true;
        return true;
    }

    // ---- drawn over the view ----------------------------------------------------------------------------------------------------------

    private static readonly Brush TrackRoad = Frozen(Color.FromRgb(70, 220, 90)), TrackJump = Frozen(Color.FromRgb(255, 80, 200)), TrackLoop = Frozen(Color.FromRgb(60, 130, 255)),
        TrackPit = Frozen(Color.FromRgb(255, 150, 20)), TrackOpen = Frozen(Color.FromRgb(255, 220, 40)), TrackDrop = Frozen(Color.FromArgb(110, 255, 255, 255));
    private static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    private static Brush TrackBrush(SketchKind kind) => kind switch { SketchKind.Jump => TrackJump, SketchKind.Loop => TrackLoop, SketchKind.Pit => TrackPit, _ => TrackRoad };

    private void DrawTrackOverlay()
    {
        if (trackCanvas is null && BrushOverlayCanvas.Parent is Panel host)
        {
            trackCanvas = new Canvas { IsHitTestVisible = false };
            host.Children.Insert(host.Children.IndexOf(BrushOverlayCanvas) + 1, trackCanvas);
        }
        if (trackCanvas is null) return;
        trackCanvas.Children.Clear();
        if (!TrackPiecesActive || cameraModel is not { } model || !nativeViewActive || interiorSceneActive) return;
        var ground = SceneGround();
        void Piece(TrackPiece p, double width, double opacity, bool selected)
        {
            var line = p.Line();
            Point? last = null;
            for (var i = 0; i < line.Count; i++)
            {
                var q = line[i];
                if (!model.Project(q.X * 512, q.Y, q.Z * 512, out var u, out var v)) { last = null; continue; }
                var s = ToWidget(model, u, v);
                if (last is { } a)
                {
                    if (selected) trackCanvas.Children.Add(new Line { X1 = a.X, Y1 = a.Y, X2 = s.X, Y2 = s.Y, Stroke = Brushes.White, StrokeThickness = width + 4, Opacity = opacity });
                    trackCanvas.Children.Add(new Line { X1 = a.X, Y1 = a.Y, X2 = s.X, Y2 = s.Y, Stroke = TrackBrush(q.Kind), StrokeThickness = width, Opacity = opacity, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
                }
                // (a post down to the ground every 4 cells where it's up in the air: how high it is, seen from any side)
                if (i % 8 == 0 && ground is not null && IslandOps.Altitude(ground, q.X * 512, q.Z * 512) is double g && q.Y > g + 300
                    && model.Project(q.X * 512, g, q.Z * 512, out var gu, out var gv))
                {
                    var f = ToWidget(model, gu, gv);
                    trackCanvas.Children.Add(new Line { X1 = s.X, Y1 = s.Y, X2 = f.X, Y2 = f.Y, Stroke = TrackDrop, StrokeThickness = 1 });
                }
                last = s;
            }
        }
        foreach (var p in trackPieces.Pieces) Piece(p, 5, 0.95, ReferenceEquals(p, trackSelected));
        if (trackGhost is { } ghost) Piece(ghost, 5, 0.55, false);
        foreach (var e in trackPieces.OpenEnds().Concat(trackPieces.OpenStarts()))
        {
            if (!model.Project(e.X * 512, e.Y, e.Z * 512, out var u, out var v)) continue;
            var s = ToWidget(model, u, v);
            var ring = new Ellipse { Width = 14, Height = 14, Stroke = TrackOpen, StrokeThickness = 2 };
            Canvas.SetLeft(ring, s.X - 7); Canvas.SetTop(ring, s.Y - 7);
            trackCanvas.Children.Add(ring);
        }
    }

    // ---- the track ---------------------------------------------------------------------------------------------------------------------

    // The island as its track is built on it: its own ground before any road (the build's original), made bigger where its track's build
    // makes it bigger -- what the plan's cells and the generator's are of
    private IslandFile? TrackIslandAsBuilt(RaceTrackIsland island)
    {
        var path = System.IO.Path.Combine(gameRoot, island.RacesTwin && island.TwinIleFile is { } twin ? twin : island.IleFile);
        var source = File.Exists(path + RaceTrackService.BackupSuffix) ? path + RaceTrackService.BackupSuffix : path;
        if (!File.Exists(source)) return SceneGround();
        try { return IslandScaler.AsBuilt(IslandFile.Load(source), island, new List<string>()); }
        catch (Exception e) when (e is IOException or InvalidDataException) { DebugLog.Log($"track pieces: {source}: {e.Message}"); return SceneGround(); }
    }

    private SketchResult? trackResult;

    private void MakeTrackFromPieces()
    {
        if (TrackIslandOfView() is not { } island || TrackIslandAsBuilt(island) is not { } ground) { SetTrackStatus("Open one of the race tracks' islands first."); return; }
        DisarmTrackPiece();
        trackResult = trackPieces.MakePlan(ground);
        trackPlanPath = null;
        var ok = trackResult.Plan is not null;
        if (ok)
        {
            try
            {
                Directory.CreateDirectory(TrackPiecesFolder);
                var path = System.IO.Path.Combine(TrackPiecesFolder, System.IO.Path.GetFileNameWithoutExtension(island.IleFile).ToLowerInvariant() + $"_pieces_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                var json = JsonSerializer.Serialize(trackResult.Plan, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                });
                File.WriteAllText(path, $"// {island.Name}'s track, laid from track pieces in LBA Assembler's 3D view, {DateTime.Now:yyyy-MM-dd HH:mm}{Environment.NewLine}{json}{Environment.NewLine}");
                trackPlanPath = path;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { trackResult.Problems.Add($"The plan can't be saved: {e.Message}"); ok = false; }
        }
        if (trackBuildButton is not null) trackBuildButton.IsEnabled = ok;
        SetTrackStatus((ok ? $"The track is made ({trackPlanPath}): Build it… to put it into the game folder. " : "No track: ") +
                       string.Join(" ", trackResult.Problems.Concat(trackResult.Notes).Select(n => n.TrimEnd('.') + ".")));
    }

    private void BuildTrackFromPieces()
    {
        if (trackPlanPath is not { } path || TrackIslandOfView() is not { } island) return;
        OpenRaceTrackWindow(w => w.UsePlan(island, path));
    }
}
