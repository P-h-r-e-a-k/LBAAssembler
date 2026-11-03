using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
    // The test race's start (the chequered flag on the road; the user, 2026-11-03: "allow the user to drag and drop for where they want to
    // start a test race from so they can test individual segments"): a point of the road, the flag being placed (the next click on the
    // road puts it there) or dragged along it
    private (double X, double Z, double Y)? trackFlag;
    private bool trackFlagArmed, trackFlagDragging;
    private Button? trackTestButton;
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
        var test = new WrapPanel();
        test.Children.Add(Btn("Place the test start", "Then click on the road: a test race starts there (or drag the chequered flag along the road)", ArmTrackFlag));
        trackTestButton = Btn("Test from the flag", "Build the road from the flag on -- to its end, or once round a lap -- into the game folder, and race it in Play", TestTrackFromFlag);
        test.Children.Add(trackTestButton);
        root.Children.Add(test);
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
        trackPieces.Pieces.Clear(); trackUndo.Clear(); trackSelected = null; trackPlanPath = null; trackFlag = null; trackFlagArmed = false;
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
        // (the flag: put down on the road, or picked up to drag along it)
        if (trackFlagArmed)
        {
            if (TrackRoadNear(at, 30) is { } spot) { trackFlag = spot; trackFlagArmed = false; TerrainViewport.Cursor = null; SetTrackStatus("The test start is on the road: Test from the flag races from it. Drag the flag to move it."); }
            else SetTrackStatus("Click on the road (a piece laid) to put the test start there.");
            DrawTrackOverlay();
            e.Handled = true;
            return true;
        }
        if (TrackFlagScreen() is { } flagAt && (flagAt - at).Length < 14)
        {
            trackFlagDragging = true;
            TerrainViewport.CaptureMouse();
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
            // (LBA2_TRACKPIECES_FLAG="x,z": the test start put on the road nearest that cell, through the click's own path -- the flag chosen,
            // the pointer where the view draws that road point; LBA2_TRACKPIECES_TESTFLAG=1: then Test from the flag)
            if (Environment.GetEnvironmentVariable("LBA2_TRACKPIECES_FLAG") is { Length: > 0 } flagAt)
            {
                var fv = flagAt.Split(',').Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                var near = trackPieces.Pieces.Where(p => !p.IsPit).SelectMany(p => p.Line()).MinBy(q => Math.Pow(q.X - fv[0], 2) + Math.Pow(q.Z - fv[1], 2));
                ArmTrackFlag();
                if (Screen(near.X, near.Z, near.Y) is { } fp && TrackRoadNear(fp, 30) is { } spot) { trackFlag = spot; trackFlagArmed = false; TerrainViewport.Cursor = null; }
                DrawTrackOverlay();
                DebugLog.Log($"track pieces test: the test start {(trackFlag is { } tf ? $"at ({tf.X:0.0}, {tf.Z:0.0}) y {tf.Y:0}" : "not placed")}");
                if (Environment.GetEnvironmentVariable("LBA2_TRACKPIECES_TESTFLAG") == "1") { TestTrackFromFlag(); return; }
            }
            // (LBA2_TRACKPIECES_SHOW="cx,cz,alpha,beta,distance,piece": a picture of the track being laid -- the view on it, the next piece
            // chosen and following the pointer, held at the open end; no plan made)
            if (Environment.GetEnvironmentVariable("LBA2_TRACKPIECES_SHOW") is { Length: > 0 } show)
            {
                var v = show.Split(',');
                double D(int i) => double.Parse(v[i], System.Globalization.CultureInfo.InvariantCulture);
                targetX = D(0) * 512; targetZ = D(1) * 512;
                targetY = trackPieces.Pieces.Count > 0 ? trackPieces.Pieces.Average(p => p.Y) : 2000;
                nativeAlpha = (int)D(2); nativeBeta = (int)D(3); nativeDistance = (int)D(4);
                RenderNativeCamera();
                var next = Section.All.FirstOrDefault(s => s.Name == v[5]);
                var wait = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2500) };
                wait.Tick += (_, _) =>
                {
                    wait.Stop();
                    var ends = trackPieces.OpenEnds().ToList();
                    if (next is null || cameraModel is not { } m2 || ends.Count == 0) return;
                    var end = ends[0];
                    ArmTrackPiece(next);
                    trackHeading = end.Heading;
                    // (the pointer a little short of the end, on the ground under it: the piece snaps on)
                    double ex = end.X - Math.Cos(end.Heading) * 2, ez = end.Z - Math.Sin(end.Heading) * 2;
                    var gy = IslandOps.Altitude(ground, ex * 512, ez * 512) ?? 0;
                    if (m2.Project(ex * 512, gy, ez * 512, out var u, out var w)) UpdateTrackGhost(ToWidget(m2, u, w));
                    DrawTrackOverlay();
                    DebugLog.Log($"track pieces test: showing {next.Name} being laid, ghost {(trackGhost is null ? "none" : "at the open end")}");
                };
                wait.Start();
                return;
            }
            MakeTrackFromPieces();
            DebugLog.Log($"track pieces test: {laid} of {items.Length - 1} laid; {trackStatus?.Text}");
            // (LBA2_TRACKPIECES_BUILD=1: on to the race track window with the plan, as Build it... does)
            if (Environment.GetEnvironmentVariable("LBA2_TRACKPIECES_BUILD") == "1" && trackPlanPath is not null) BuildTrackFromPieces();
        };
        timer.Start();
    }

    private bool HandleTrackMouseMove(MouseEventArgs e)
    {
        if (!TrackPiecesActive) return false;
        var at = e.GetPosition(TerrainViewport);
        if (trackFlagDragging)
        {
            if (TrackRoadNear(at, 60) is { } spot) { trackFlag = spot; DrawTrackOverlay(); }
            return true;
        }
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
        if (trackFlagDragging && e.ChangedButton == MouseButton.Left)
        {
            trackFlagDragging = false;
            Mouse.Capture(null);
            SetTrackStatus("The test start moved: Test from the flag races from it.");
            return true;
        }
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

    private void ArmTrackFlag()
    {
        if (!TrackPiecesActive) return;
        if (trackPieces.Pieces.All(p => p.IsPit)) { SetTrackStatus("Lay some road first: the test start goes on it."); return; }
        DisarmTrackPiece();
        trackFlagArmed = true;
        TerrainViewport.Cursor = Cursors.Cross;
        SetTrackStatus("Click on the road where the test race is to start.");
    }

    // The road's middle point (a laid piece's) drawn nearest a point of the view, within `pixels`
    private (double X, double Z, double Y)? TrackRoadNear(Point at, double pixels)
    {
        if (cameraModel is not { } model) return null;
        (double X, double Z, double Y)? best = null; var nearest = pixels;
        foreach (var p in trackPieces.Pieces.Where(p => !p.IsPit))
            foreach (var q in p.Line())
            {
                if (!model.Project(q.X * 512, q.Y, q.Z * 512, out var u, out var v)) continue;
                var d = (ToWidget(model, u, v) - at).Length;
                if (d < nearest) { nearest = d; best = (q.X, q.Z, q.Y); }
            }
        return best;
    }

    // Where the flag's foot is drawn, or null
    private Point? TrackFlagScreen() =>
        trackFlag is { } f && cameraModel is { } model && model.Project(f.X * 512, f.Y, f.Z * 512, out var u, out var v) ? ToWidget(model, u, v) : null;

    // Test from the flag: the road from the flag on made into a sprint, built into the game folder with the tracks built there already
    // (each island's as it was, this island's the test), and raced in Play -- the race car setup asked first, as Play asks
    private async void TestTrackFromFlag()
    {
        if (trackFlag is not { } flag) { SetTrackStatus("Place the test start first (Place the test start, then click on the road), or drag the chequered flag onto it."); return; }
        if (TrackIslandOfView() is not { } island || TrackIslandAsBuilt(island) is not { } ground) { SetTrackStatus("Open one of the race tracks' islands first."); return; }
        DisarmTrackPiece();
        var result = trackPieces.MakePlan(ground, flag);
        if (result.Plan is not { } plan) { SetTrackStatus("No test: " + string.Join(" ", result.Problems.Concat(result.Notes).Select(n => n.TrimEnd('.') + "."))); return; }
        var others = RaceTrackService.BuiltIslands(gameRoot).Where(i => !string.Equals(i.IleFile, island.IleFile, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(i => i.IleFile, StringComparer.OrdinalIgnoreCase).ToList();
        var answer = MessageBox.Show(this,
            $"Build the test -- {island.Shown} from the flag, {string.Join(", ", result.Notes.Take(2))} -- into\n{gameRoot}\nand race it?" +
            (others.Count > 0 ? $"\n\nThe tracks built there before ({string.Join(", ", others.Select(o => o.Shown))}) are built again with it." : ""),
            "Test race", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (answer != MessageBoxResult.OK) return;
        if (playing) StopPlay();
        RaceTrackOptions Options(RaceTrackIsland i)
        {
            var o = RaceTrackOptions.For(i);
            o.Story = o.Story && i.IleFile == RaceTrackIsland.Citadel.IleFile;
            return o;
        }
        var builds = others.Select(i => new RaceTrackService.TrackBuild(RaceTrackPlan.Built(i), Options(i))).ToList();
        // (Citadel Island: a plan is its town circuit's, the storm track built as it is)
        builds.Insert(0, island.RacesTwin ? new RaceTrackService.TrackBuild(RaceTrackPlan.Built(island), Options(island), plan) : new RaceTrackService.TrackBuild(plan, Options(island)));
        if (trackTestButton is not null) trackTestButton.IsEnabled = false;
        SetTrackStatus("Building the test…");
        var root = gameRoot;
        RaceTrackService.BuildResult built;
        try { built = await Task.Run(() => RaceTrackService.Build(root, builds)); }
        finally { if (trackTestButton is not null) trackTestButton.IsEnabled = true; }
        if (!built.Ok) { SetTrackStatus("The test couldn't be built: " + built.Summary); MessageBox.Show(this, built.Summary, "Test race", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        DebugLog.Log($"track pieces: test from the flag built -- {built.Summary}");
        RaceTrackChanged();
        SetTrackStatus("The test is built: racing it. " + string.Join(" ", result.Notes.Select(n => n.TrimEnd('.') + ".")));
        StartPlay(GameKind.Lba2);
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
            case Key.Escape when trackFlagArmed: trackFlagArmed = false; TerrainViewport.Cursor = null; SetTrackStatus("The test start not placed."); break;
            case Key.Escape when trackArmed is not null: DisarmTrackPiece(); SetTrackStatus("Stopped laying. Choose a piece to lay another."); break;
            case Key.Escape when trackSelected is not null: trackSelected = null; DrawTrackOverlay(); break;
            default: return false;
        }
        e.Handled = true;
        return true;
    }

    // ---- drawn over the view ----------------------------------------------------------------------------------------------------------
    // Each piece as the road it will be (the user, 2026-11-03: "Instead of just the centre line let's show actual track pieces"): the asphalt
    // as wide as the builder lays it, red and white kerbs along its edges, a raised piece's deck edge and its piers down to the ground, a
    // loop's ring stood up across the way, a jump's gap left open with its flight's arc over it, the pit lane in its own colour -- every
    // face drawn far to near, so a road passing over another covers it. Drawn by one element (TrackDrawing), thousands of faces at a time.

    private static readonly Brush TrackAsphalt = Frozen(Color.FromArgb(235, 58, 58, 66)), TrackAsphaltSelected = Frozen(Color.FromArgb(240, 52, 82, 130)),
        TrackPitAsphalt = Frozen(Color.FromArgb(235, 110, 78, 44)), TrackKerbRed = Frozen(Color.FromArgb(245, 205, 45, 40)), TrackKerbWhite = Frozen(Color.FromArgb(245, 240, 240, 240)),
        TrackDeck = Frozen(Color.FromArgb(235, 34, 34, 40)), TrackOpen = Frozen(Color.FromRgb(255, 220, 40)),
        TrackGhostAsphalt = Frozen(Color.FromArgb(150, 220, 225, 235)), TrackGhostKerb = Frozen(Color.FromArgb(150, 255, 255, 255)),
        TrackLoopRoad = Frozen(Color.FromArgb(245, 92, 92, 100)), TrackLoopShell = Frozen(Color.FromArgb(245, 56, 140, 64)), TrackLoopSide = Frozen(Color.FromArgb(245, 40, 104, 48));
    private static readonly Pen TrackLoopLeg = FrozenPen(Color.FromArgb(245, 50, 120, 56), 5), TrackFlagPole = FrozenPen(Color.FromRgb(30, 30, 30), 2.5),
        TrackFlagEdge = FrozenPen(Color.FromRgb(20, 20, 20), 1);
    private static readonly Pen TrackPier = FrozenPen(Color.FromArgb(200, 120, 120, 130), 3), TrackFlight = FrozenPen(Color.FromArgb(230, 255, 80, 200), 2, dashed: true),
        TrackGhostEdge = FrozenPen(Color.FromArgb(230, 255, 255, 255), 1.5, dashed: true), TrackRing = FrozenPen(Color.FromRgb(255, 220, 40), 2.5);
    private static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    private static Pen FrozenPen(Color c, double width, bool dashed = false)
    {
        var pen = new Pen(Frozen(c), width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (dashed) pen.DashStyle = new DashStyle(new[] { 3.0, 2.0 }, 0);
        pen.Freeze();
        return pen;
    }
    // the road's half widths (cells): the asphalt's and to the kerbs' outer edge -- RaceTrackOptions' AsphaltHalfWidth, CurbHalfWidth
    private const double TrackAsphaltHalf = 3.5, TrackKerbHalf = 4.5, TrackDeckDepth = 300, TrackLoopRadius = 4, TrackLoopAcross = 3;
    // a loop's band and rails as the build makes them (RaceTrackBuilder.LoopBandHalf, cells; RaceTrackLoopBody: rail width, height and the
    // band's thickness, world units -- the rail's width here in cells)
    private const double TrackLoopBand = 1.3, TrackLoopRailWidth = 90 / 512.0, TrackLoopRail = 150, TrackLoopThickness = 160;

    // The faces, far to near, drawn as they come
    private sealed class TrackDrawing : FrameworkElement
    {
        public readonly List<(double Depth, Point[] Points, Brush? Fill, Pen? Pen)> Faces = new();
        public readonly List<Point> Rings = new();
        // the test start's chequered flag: its foot on the road, the top of its pole
        public (Point Foot, Point Top)? Flag;
        protected override void OnRender(DrawingContext dc)
        {
            foreach (var (_, points, fill, pen) in Faces.OrderByDescending(f => f.Depth))
            {
                if (points.Length == 2) { if (pen is not null) dc.DrawLine(pen, points[0], points[1]); continue; }
                var g = new StreamGeometry();
                using (var c = g.Open())
                {
                    c.BeginFigure(points[0], fill is not null, true);
                    c.PolyLineTo(points.Skip(1).ToArray(), true, false);
                }
                g.Freeze();
                dc.DrawGeometry(fill, pen, g);
            }
            foreach (var r in Rings) dc.DrawEllipse(null, TrackRing, r, 7, 7);
            if (Flag is { } f)
            {
                dc.DrawLine(TrackFlagPole, f.Foot, f.Top);
                dc.DrawEllipse(Brushes.White, TrackFlagPole, f.Foot, 4, 4);
                // (4 x 3 squares of 7 pixels, black and white, off the pole's top; the pole at least 36 pixels, seen from far off)
                var top = (f.Top - f.Foot).Length < 36 ? new Point(f.Foot.X, f.Foot.Y - 36) : f.Top;
                dc.DrawLine(TrackFlagPole, f.Foot, top);
                for (var r = 0; r < 3; r++)
                    for (var c = 0; c < 4; c++)
                        dc.DrawRectangle((r + c) % 2 == 0 ? Brushes.Black : Brushes.White, null, new Rect(top.X + 1 + c * 7, top.Y + r * 7, 7, 7));
                dc.DrawRectangle(null, TrackFlagEdge, new Rect(top.X + 1, top.Y, 28, 21));
            }
        }
    }
    private TrackDrawing? trackDrawing;

    private void DrawTrackOverlay()
    {
        if (trackCanvas is null && BrushOverlayCanvas.Parent is Panel host)
        {
            trackCanvas = new Canvas { IsHitTestVisible = false };
            host.Children.Insert(host.Children.IndexOf(BrushOverlayCanvas) + 1, trackCanvas);
            trackDrawing = new TrackDrawing { IsHitTestVisible = false };
            trackCanvas.Children.Add(trackDrawing);
        }
        if (trackCanvas is null || trackDrawing is null) return;
        trackDrawing.Faces.Clear(); trackDrawing.Rings.Clear(); trackDrawing.Flag = null;
        trackDrawing.Width = TerrainViewport.ActualWidth; trackDrawing.Height = TerrainViewport.ActualHeight;
        if (!TrackPiecesActive || cameraModel is not { } model || !nativeViewActive || interiorSceneActive) { trackDrawing.InvalidateVisual(); return; }
        var ground = SceneGround();
        double Ground(double x, double z) => ground is not null && IslandOps.Altitude(ground, x * 512, z * 512) is double g ? g : double.NaN;
        var faces = trackDrawing.Faces;
        // a face of world points (cells across, world units up): kept when all its corners are in front of the camera
        void Face(Brush? fill, Pen? pen, params (double X, double Y, double Z)[] corners)
        {
            var points = new Point[corners.Length];
            double depth = 0;
            for (var k = 0; k < corners.Length; k++)
            {
                var (x, y, z) = corners[k];
                if (!model.Project(x * 512, y, z * 512, out var u, out var v)) return;
                points[k] = ToWidget(model, u, v);
                depth += model.Depth(x * 512, y, z * 512);
            }
            faces.Add((depth / corners.Length, points, fill, pen));
        }
        void Piece(TrackPiece p, bool selected, bool ghost)
        {
            var line = p.Line();
            var n = line.Count;
            // each point's way across (unit, cells): the road's edges are its middle that far either side
            var across = new (double X, double Z)[n];
            for (var i = 0; i < n; i++)
            {
                var a = line[Math.Max(0, i - 1)]; var b = line[Math.Min(n - 1, i + 1)];
                double dx = b.X - a.X, dz = b.Z - a.Z, len = Math.Sqrt(dx * dx + dz * dz);
                across[i] = len < 1e-9 ? (0, 0) : (-dz / len, dx / len);
            }
            (double X, double Y, double Z) At(int i, double off, double up = 0) => (line[i].X + across[i].X * off, line[i].Y + up, line[i].Z + across[i].Z * off);
            var asphalt = ghost ? TrackGhostAsphalt : selected ? TrackAsphaltSelected : p.IsPit ? TrackPitAsphalt : TrackAsphalt;
            for (var i = 0; i + 1 < n; i++)
            {
                var kind = line[i + 1].Kind;
                if (kind == SketchKind.Jump) continue;
                var raised = !double.IsNaN(Ground(line[i].X, line[i].Z)) && line[i].Y > Ground(line[i].X, line[i].Z) + 300;
                // (the deck's edges, a raised road's: its thickness, both sides)
                if (raised && !ghost)
                    foreach (var side in new[] { -TrackKerbHalf, TrackKerbHalf })
                        Face(TrackDeck, null, At(i, side), At(i + 1, side), At(i + 1, side, -TrackDeckDepth), At(i, side, -TrackDeckDepth));
                Face(asphalt, ghost ? TrackGhostEdge : null, At(i, -TrackAsphaltHalf), At(i + 1, -TrackAsphaltHalf), At(i + 1, TrackAsphaltHalf), At(i, TrackAsphaltHalf));
                // (the kerbs: a cell red, a cell white -- two of the line's half cells each)
                var kerb = ghost ? TrackGhostKerb : (i / 2) % 2 == 0 ? TrackKerbRed : TrackKerbWhite;
                Face(kerb, null, At(i, -TrackKerbHalf), At(i + 1, -TrackKerbHalf), At(i + 1, -TrackAsphaltHalf), At(i, -TrackAsphaltHalf));
                Face(kerb, null, At(i, TrackAsphaltHalf), At(i + 1, TrackAsphaltHalf), At(i + 1, TrackKerbHalf), At(i, TrackKerbHalf));
                // (a pier down to the ground every 4 cells under a raised piece)
                if (raised && !ghost && i % 8 == 0) Face(null, TrackPier, At(i, 0, -TrackDeckDepth), (line[i].X, Ground(line[i].X, line[i].Z), line[i].Z));
            }
            // a loop: the ring stood up across the way at the middle of its stretch, the road going round it and coming down beside where it went in
            // a jump: the gap left open, its flight's arc over it
            for (var i = 0; i < n;)
            {
                var kind = line[i].Kind;
                var j = i;
                while (j + 1 < n && line[j + 1].Kind == kind) j++;
                if (kind == SketchKind.Loop)
                {
                    // (the ring as the build makes it -- RaceTrackLoopBody: a band of road 2.6 cells across bent into a circle standing on the
                    // road at the stretch's middle, its road on the inside, red and white rails along its edges, green outside, two green legs
                    // at its widest; over the turn it winds across by TrackLoopAcross, so the car comes out beside where it went in)
                    var m = (i + j) / 2;
                    double hx = line[Math.Min(n - 1, m + 1)].X - line[Math.Max(0, m - 1)].X, hz = line[Math.Min(n - 1, m + 1)].Z - line[Math.Max(0, m - 1)].Z;
                    var hl = Math.Sqrt(hx * hx + hz * hz); if (hl < 1e-9) { i = j + 1; continue; }
                    hx /= hl; hz /= hl;
                    double ax = -hz, az = hx;
                    // a point of the ring: at angle a round it, `off` cells across its band, `inward` world units in towards its middle
                    (double X, double Y, double Z) Ring(double a, double off, double inward = 0)
                    {
                        var side = off + TrackLoopAcross * (a / (2 * Math.PI) - 0.5);
                        double r = TrackLoopRadius * 512 - inward;
                        return (line[m].X + hx * r * Math.Sin(a) / 512 + ax * side, line[m].Y + TrackLoopRadius * 512 - r * Math.Cos(a), line[m].Z + hz * r * Math.Sin(a) / 512 + az * side);
                    }
                    const int steps = 48;
                    double band = TrackLoopBand, rail = band + TrackLoopRailWidth;
                    for (var k = 0; k < steps; k++)
                    {
                        double a0 = 2 * Math.PI * k / steps, a1 = 2 * Math.PI * (k + 1) / steps;
                        var stripe = ghost ? TrackGhostKerb : k % 2 == 0 ? TrackKerbRed : TrackKerbWhite;
                        // its outside, its sides, its road, its rails standing in from its edges
                        Face(ghost ? TrackGhostAsphalt : TrackLoopShell, ghost ? TrackGhostEdge : null, Ring(a0, -rail, -TrackLoopThickness), Ring(a1, -rail, -TrackLoopThickness), Ring(a1, rail, -TrackLoopThickness), Ring(a0, rail, -TrackLoopThickness));
                        if (!ghost)
                            foreach (var s in new[] { -rail, rail })
                                Face(TrackLoopSide, null, Ring(a0, s, -TrackLoopThickness), Ring(a1, s, -TrackLoopThickness), Ring(a1, s, TrackLoopRail), Ring(a0, s, TrackLoopRail));
                        Face(ghost ? TrackGhostAsphalt : TrackLoopRoad, null, Ring(a0, -band), Ring(a1, -band), Ring(a1, band), Ring(a0, band));
                        foreach (var (s0, s1) in new[] { (-rail, -band), (band, rail) })
                            Face(stripe, null, Ring(a0, s0, TrackLoopRail), Ring(a1, s0, TrackLoopRail), Ring(a1, s1, TrackLoopRail), Ring(a0, s1, TrackLoopRail));
                    }
                    // its legs, ahead of its foot and behind it at its widest, down to the road under it
                    if (!ghost)
                        foreach (var a in new[] { Math.PI / 2, 3 * Math.PI / 2 })
                        {
                            var top = Ring(a, 0, -TrackLoopThickness);
                            Face(null, TrackLoopLeg, top, (top.X, line[m].Y - 200, top.Z));
                        }
                }
                else if (kind == SketchKind.Jump && j > i)
                {
                    // (from the lip before the gap to the landing after it, a third as high as it is long)
                    int a = Math.Max(0, i - 1), b = Math.Min(n - 1, j + 1);
                    double length = Math.Sqrt(Math.Pow(line[b].X - line[a].X, 2) + Math.Pow(line[b].Z - line[a].Z, 2)) * 512;
                    for (var k = 0; k < 16; k++)
                    {
                        double f0 = k / 16.0, f1 = (k + 1) / 16.0;
                        (double X, double Y, double Z) Arc(double f) => (line[a].X + (line[b].X - line[a].X) * f, line[a].Y + (line[b].Y - line[a].Y) * f + length / 3 * 4 * f * (1 - f), line[a].Z + (line[b].Z - line[a].Z) * f);
                        Face(null, TrackFlight, Arc(f0), Arc(f1));
                    }
                }
                i = j + 1;
            }
        }
        foreach (var p in trackPieces.Pieces) Piece(p, ReferenceEquals(p, trackSelected), false);
        if (trackGhost is { } g) Piece(g, false, true);
        foreach (var e in trackPieces.OpenEnds().Concat(trackPieces.OpenStarts()))
            if (model.Project(e.X * 512, e.Y, e.Z * 512, out var u, out var v)) trackDrawing.Rings.Add(ToWidget(model, u, v));
        // (the test start: a pole 900 high on the road)
        if (trackFlag is { } flag && model.Project(flag.X * 512, flag.Y, flag.Z * 512, out var fu, out var fv) && model.Project(flag.X * 512, flag.Y + 900, flag.Z * 512, out var tu, out var tv))
            trackDrawing.Flag = (ToWidget(model, fu, fv), ToWidget(model, tu, tv));
        trackDrawing.InvalidateVisual();
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
