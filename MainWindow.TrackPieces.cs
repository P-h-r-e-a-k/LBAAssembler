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
    // The pieces' history (Edit > Undo / Redo, Ctrl+Z / Ctrl+Y and the panel's Undo step it while the mode is on -- the user, 2026-11-03: "Our
    // LBAAssembler undo function doesn't appear to work with our race track"): each step the pieces and the road's width as they were, and
    // what was done
    private sealed record TrackState(double Width, List<TrackPiece> Pieces, string What);
    private readonly Stack<TrackState> trackUndo = new(), trackRedo = new();
    private long lastTrackEditAt;
    private Section? trackArmed;                 // the panel's piece being laid
    private TrackPiece? trackGhost;              // ... where it would go
    private double trackHeading;                 // its way while it follows the pointer (Q, E)
    private double trackLift;                    // how high over the ground it goes where it joins nothing (PgUp, PgDn)
    private TrackPiece? trackSelected;
    private TrackPiece? trackDragging;           // the piece picked up and moving, as it was
    private (double X, double Z) trackDragOffset;
    // A whole section selected (Drag whole sections, or Shift): its pieces as they are now; while it's dragged, each as it was and where it
    // is in the list, and the ground under the pointer when it was picked up
    private List<TrackPiece>? trackGroup;
    private List<(int Index, TrackPiece Was)>? trackGroupDrag;
    private (double X, double Z) trackGroupAnchor;
    private CheckBox? trackSectionsBox;
    private double trackTilt;                    // the Ramp being laid: its climb (R, F)
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
    // the track's check (TrackPieceSet.Check), listed in the panel; its problems' places, marked red in the view
    private StackPanel? trackChecks;
    private List<(double X, double Z, double Y)> trackProblemSpots = new();
    private Slider? trackWidthSlider;
    private TextBlock? trackWidthLabel;
    private bool trackWidthSyncing;
    // (the slider as the island's pieces have it, without saving again)
    private void SyncTrackWidthSlider()
    {
        if (trackWidthSlider is null) return;
        trackWidthSyncing = true;
        try { trackWidthSlider.Value = trackPieces.Width * 100; }
        finally { trackWidthSyncing = false; }
    }
    private const double TrackTurnStep = Math.PI / 12, TrackLiftStep = 250;

    private bool TrackPiecesActive => editMode == EditMode.Build && buildTrackView && TerrainEditable;

    // The island the open file is, as a race track's (Citadel Island's two files one island)
    // (an open map's island: its own -- MainWindow.NewTrack's MapTrackIsland)
    private RaceTrackIsland? TrackIslandOfView() => MapTrackIsland() ?? RaceTrackIsland.BuiltIn.FirstOrDefault(i =>
        string.Equals(i.IleFile, activeFile, StringComparison.OrdinalIgnoreCase) || string.Equals(i.TwinIleFile, activeFile, StringComparison.OrdinalIgnoreCase));

    private static string TrackPiecesFolder => System.IO.Path.Combine(AppContext.BaseDirectory, "TrackPlans");
    // Where the island's pieces are kept: an open map's in its workspace (LbaMap.RaceTrack, into the map's file on Save map), else beside the editor
    private string TrackPiecesFile(string ile) =>
        MapOpen && openMap is { Kind: Maps.MapKind.Lba2Island } map && openMapInstalled is { } at && string.Equals(map.IslandFile + ".ILE", ile, StringComparison.OrdinalIgnoreCase)
            ? Maps.MapInstall.RaceTrackPath(map, at.Directory)
            : System.IO.Path.Combine(TrackPiecesFolder, System.IO.Path.GetFileNameWithoutExtension(ile).ToLowerInvariant() + "_pieces.json");
    // (the pieces in hand are of this: the island file in view, and the map it's of -- a map's copy of an island has the island's name)
    private string TrackKey => (MapOpen ? (openMapPath ?? openMap!.Name) + "|" : "") + activeFile;
    private string trackKey = "";

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
        // (each piece's button with its shape drawn on it, seen from above -- the user, 2026-11-04: track making "more accessible")
        var pieces = new WrapPanel { Margin = new Thickness(0, 0, 0, 0) };
        foreach (var section in Section.Laid)
        {
            var b = Btn(section.Name, section.Tip, () => ArmTrackPiece(section));
            var face = new StackPanel { Orientation = Orientation.Horizontal };
            face.Children.Add(TrackPieceIcon(section));
            face.Children.Add(new TextBlock { Text = section.Name, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0) });
            b.Content = face;
            System.Windows.Automation.AutomationProperties.SetName(b, section.Name);
            pieces.Children.Add(b);
        }
        root.Children.Add(pieces);
        var row = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
        row.Children.Add(Btn("Turn left (Q)", "Turn the piece being laid, or the one or the section selected, 15 degrees", () => TurnTrackPiece(-1)));
        row.Children.Add(Btn("Turn right (E)", "Turn the piece being laid, or the one or the section selected, 15 degrees", () => TurnTrackPiece(1)));
        row.Children.Add(Btn("Up (PgUp)", "Raise the piece being laid (where it joins nothing), or the one or the section selected, by 250", () => LiftTrackPiece(1)));
        row.Children.Add(Btn("Down (PgDn)", "Lower it by 250", () => LiftTrackPiece(-1)));
        row.Children.Add(Btn("Tilt up (R)", "Tilt the Ramp being laid, or the one selected, 5 % steeper upwards: a laid one about its middle", () => TiltTrackPiece(1)));
        row.Children.Add(Btn("Tilt down (F)", "Tilt the Ramp 5 % further downwards", () => TiltTrackPiece(-1)));
        root.Children.Add(row);
        // (a drag on a piece moves that piece, or with this -- or Shift held -- every piece joined to it)
        trackSectionsBox = new CheckBox { Content = "Drag whole sections (or hold Shift)", Margin = new Thickness(0, 4, 0, 0),
            ToolTip = "A drag on a piece moves every piece joined to it, and Q, E, PgUp, PgDn and Delete then work on all of them; without, the piece alone" };
        trackSectionsBox.SetResourceReference(Control.ForegroundProperty, "ThemeTextBrush");
        root.Children.Add(trackSectionsBox);
        // (the road's width: the user, 2026-11-03, "add a setting to allow a user to make the track wider if they wish")
        var widthRow = new DockPanel { Margin = new Thickness(0, 6, 0, 6) };
        var widthText = Text("Road width", 0, false);
        widthText.Width = 80; widthText.VerticalAlignment = VerticalAlignment.Center;
        trackWidthLabel = Text("100 %", 0, false);
        trackWidthLabel.Width = 48; trackWidthLabel.VerticalAlignment = VerticalAlignment.Center;
        trackWidthSlider = new Slider { Minimum = 100, Maximum = TrackPieceSet.WidthMost * 100, Value = 100, TickFrequency = 10, IsSnapToTickEnabled = true, VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "How wide the road is built: its asphalt, kerbs and a raised road's deck, from the usual (100 %) to twice it" };
        trackWidthSlider.ValueChanged += (_, e) =>
        {
            if (trackWidthLabel is not null) trackWidthLabel.Text = $"{e.NewValue:0} %";
            if (trackWidthSyncing || Math.Abs(trackPieces.Width - e.NewValue / 100) < 1e-6) return;
            // (one step for a drag of the slider: the steps of a slide within a second of each other are one)
            if (!(trackUndo.TryPeek(out var last) && last.What == "change the road's width" && Environment.TickCount64 - lastTrackEditAt < 1000))
                RememberTrackPieces("change the road's width");
            lastTrackEditAt = Environment.TickCount64;
            trackPieces.Width = e.NewValue / 100;
            trackPlanPath = null;
            if (trackBuildButton is not null) trackBuildButton.IsEnabled = false;
            SaveTrackPieces();
            DrawTrackOverlay();
            SetTrackStatus($"The road {e.NewValue:0} % of the usual width: Make the track (or Test from the flag) to build it so.");
        };
        DockPanel.SetDock(widthText, Dock.Left); DockPanel.SetDock(trackWidthLabel, Dock.Right);
        widthRow.Children.Add(widthText); widthRow.Children.Add(trackWidthLabel); widthRow.Children.Add(trackWidthSlider);
        root.Children.Add(widthRow);
        // (a track to start from, laid in the middle of the view, and the pieces that bring the road's end round to its start)
        var starters = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
        var starterBox = new ComboBox { MinWidth = 150, Margin = new Thickness(0, 0, 6, 6), Padding = new Thickness(4, 3, 4, 3), ToolTip = "A ready-made track to begin from, laid in the middle of the view" };
        foreach (var s in TrackPieceSet.Starters) starterBox.Items.Add(new ComboBoxItem { Content = s.Name, ToolTip = s.Tip });
        starterBox.SelectedIndex = 0;
        System.Windows.Automation.AutomationProperties.SetName(starterBox, "Starter track");
        starters.Children.Add(starterBox);
        starters.Children.Add(Btn("Start from it", "Lay the starter track in the middle of the view (the pieces laid already are replaced, after asking)", () => LayStarterTrack(TrackPieceSet.Starters[Math.Max(0, starterBox.SelectedIndex)].Name)));
        starters.Children.Add(Btn("Close the lap", "Add the pieces (four at most, a Ramp first where the heights differ) that take the road's end round to its start", CloseTrackLap));
        root.Children.Add(starters);
        var edit = new WrapPanel();
        edit.Children.Add(Btn("Delete (Del)", "Take the selected piece away", DeleteTrackPiece));
        edit.Children.Add(Btn("Undo (Ctrl+Z)", "Take back the last change to the pieces", () => StepTrackHistory(undo: true)));
        edit.Children.Add(Btn("Redo (Ctrl+Y)", "Do again the change last taken back", () => StepTrackHistory(undo: false)));
        edit.Children.Add(Btn("Clear all", "Take every piece away", () =>
        {
            if (trackPieces.Pieces.Count == 0 || MessageBox.Show(this, "Take every piece away?", "Race track", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
            RememberTrackPieces("clear all"); trackPieces.Pieces.Clear(); trackSelected = null; trackGroup = null; trackFlag = null; TrackPiecesChanged();
        }));
        root.Children.Add(edit);
        // (the track as it is, checked on every change: what's fine, what to know, what stops it -- each with its sign as well as its colour)
        var trackHeading = Text("The track", 0, false);
        trackHeading.FontWeight = FontWeights.SemiBold;
        root.Children.Add(trackHeading);
        trackChecks = new StackPanel { Margin = new Thickness(0, 2, 0, 4) };
        System.Windows.Automation.AutomationProperties.SetName(trackChecks, "Track check");
        root.Children.Add(trackChecks);
        // (race it: the main thing to do, first -- in a scratch copy, the game folder untouched; from the flag if there is one)
        var test = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
        trackTestButton = Btn("▶ Race it", "Build the track into a scratch copy of the game folder (yours isn't changed) and race it: from the chequered flag if you placed one " +
                                            "(to the road's end or once round a lap; a flag at the road's end drives it the other way), else the whole track from its start", TestTrackFromFlag);
        trackTestButton.FontWeight = FontWeights.SemiBold;
        trackTestButton.Padding = new Thickness(14, 5, 14, 5);
        System.Windows.Automation.AutomationProperties.SetName(trackTestButton, "Race it");
        test.Children.Add(trackTestButton);
        test.Children.Add(Btn("Place the test start", "Optional: then click on the road, and Race it starts there (drag the chequered flag along the road to move it)", ArmTrackFlag));
        test.Children.Add(Btn("Remove the test start", "Take the chequered flag off the road: Race it races the whole track", RemoveTrackFlag));
        root.Children.Add(test);
        var make = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
        make.Children.Add(Btn("Make the track", "Follow the pieces into a race track plan: the road's middle, its heights, jumps, loops and pit lane", MakeTrackFromPieces));
        trackBuildButton = Btn("Build into the game…", "The track to the race track window, to build into the game folder itself", BuildTrackFromPieces);
        trackBuildButton.IsEnabled = false;
        make.Children.Add(trackBuildButton);
        make.Children.Add(Btn("Share for LBRA…", "Save the track as a file to send to friends: Little Big Racing Adventure builds it from their own game (Racing > Shared tracks)", ShareTrackFromPieces));
        root.Children.Add(make);
        trackStatus = Text("", 6, false);
        root.Children.Add(trackStatus);
        // (how it works, under the pieces: the buttons first, where they are seen)
        root.Children.Add(Text("Lay a race track in the 3D view from the generator's pieces, true to size. Choose a piece, then click on the island: " +
                               "it goes on from the nearest open end (the yellow rings) at that end's height, or onto the ground under the pointer. " +
                               "Q and E turn it, PgUp and PgDn raise and lower it, R and F tilt a Ramp, Esc stops laying. Drag a piece to move it, or Shift+drag " +
                               "to move the whole section it's part of; Delete takes the selected one away. Ctrl+Z undoes, Ctrl+Y redoes. " +
                               "Drag the ground (Rotate/tilt with mouse) or right drag to turn the view, middle drag to pan it.", 10));
        // (Race it and the check first, where a short panel shows them; the pieces after)
        foreach (var top in new UIElement[] { test, trackChecks, trackHeading }) { root.Children.Remove(top); root.Children.Insert(0, top); }
        test.Margin = new Thickness(0, 4, 0, 8);
        return new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = root };
    }

    // Brings the track pieces in line with the mode and the island: the panel, the island's pieces, the overlay
    private void SyncTrackPieces()
    {
        var on = TrackPiecesActive;
        TrackPanelHost.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        if (on && TrackPanelHost.Content is null) TrackPanelHost.Content = TrackPanel();
        if (on && !string.Equals(trackKey, TrackKey, StringComparison.OrdinalIgnoreCase)) LoadTrackPieces();
        if (on) UpdateTrackChecks();
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
        trackArmed = section; trackGhost = null; trackSelected = null; trackGroup = null;
        SetTrackStatus($"{section.Name}: click on the island to lay it ({section.Tip}). Q, E turn it; PgUp, PgDn raise it;{(section.Tilts ? $" R, F tilt it ({RampSlope(trackTilt)});" : "")} Esc stops.");
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
        else if (trackGroup is { Count: > 0 } group)
        {
            // (the section turned about its middle: each piece's start round it, its way with it)
            var points = group.SelectMany(p => new[] { (p.X, p.Z), (p.End.X, p.End.Z) }).ToList();
            double cx = points.Average(q => q.X), cz = points.Average(q => q.Z), a = way * TrackTurnStep, cos = Math.Cos(a), sin = Math.Sin(a);
            ChangeTrackGroup(p => p with { X = cx + (p.X - cx) * cos - (p.Z - cz) * sin, Z = cz + (p.X - cx) * sin + (p.Z - cz) * cos, Heading = p.Heading + a }, "turn the section");
        }
        else if (trackSelected is { } s) ReplaceTrackPiece(s, s with { Heading = s.Heading + way * TrackTurnStep }, "turn");
        DrawTrackOverlay();
    }

    // R, F: the Ramp being laid steeper or less steep, or the one selected tilted about its middle
    private void TiltTrackPiece(int way)
    {
        if (trackArmed is { } armed)
        {
            if (!armed.Tilts) { SetTrackStatus("Only the Ramp tilts: choose the Ramp."); return; }
            trackTilt = Math.Clamp(trackTilt + way * TrackPiece.TiltStep, -TrackPiece.TiltMost, TrackPiece.TiltMost);
            if (trackGhost is { } g) trackGhost = trackPieces.Snapped(g with { Tilt = trackTilt });
            SetTrackStatus($"The Ramp {RampSlope(trackTilt)}.");
        }
        else if (trackSelected is { Section.Tilts: true } s)
        {
            var tilted = s.TiltedBy(way);
            if (tilted == s) { SetTrackStatus($"The Ramp is as steep as it goes ({RampSlope(s.Tilt)})."); return; }
            ReplaceTrackPiece(s, tilted, way > 0 ? "tilt up" : "tilt down");
            SetTrackStatus($"The Ramp {RampSlope(tilted.Tilt)}, tilted about its middle.");
        }
        else SetTrackStatus("Only the Ramp tilts: select one (click it), or choose the Ramp to lay.");
        DrawTrackOverlay();
    }

    private static string RampSlope(double tilt) =>
        Math.Abs(tilt) < 1 ? "level" : $"{(tilt > 0 ? "climbing" : "dropping")} {Math.Abs(tilt) / (Section.RampLength * 512) * 100:0} % ({Math.Abs(tilt):0} over its {Section.RampLength:0} cells)";

    // Every piece of the section selected changed by `change`, as one step
    private void ChangeTrackGroup(Func<TrackPiece, TrackPiece> change, string what)
    {
        if (trackGroup is not { Count: > 0 } group) return;
        RememberTrackPieces(what);
        var now = new List<TrackPiece>();
        foreach (var p in group)
        {
            var i = trackPieces.Pieces.FindIndex(q => ReferenceEquals(q, p));
            if (i < 0) continue;
            var q = change(p);
            trackPieces.Pieces[i] = q; now.Add(q);
            if (ReferenceEquals(p, trackSelected)) trackSelected = q;
        }
        trackGroup = now;
        TrackPiecesChanged();
    }

    private void LiftTrackPiece(int way)
    {
        if (trackArmed is not null) { trackLift = Math.Max(0, trackLift + way * TrackLiftStep); if (trackGhost is { } g) trackGhost = g with { Y = g.Y + way * TrackLiftStep }; SetTrackStatus($"Laid {trackLift:0} over the ground where it joins nothing."); }
        else if (trackGroup is { Count: > 0 }) ChangeTrackGroup(p => p with { Y = p.Y + way * TrackLiftStep }, way > 0 ? "raise the section" : "lower the section");
        else if (trackSelected is { } s) ReplaceTrackPiece(s, s with { Y = s.Y + way * TrackLiftStep }, way > 0 ? "raise" : "lower");
        DrawTrackOverlay();
    }

    private void DeleteTrackPiece()
    {
        if (trackGroup is { Count: > 1 } group)
        {
            RememberTrackPieces($"delete a section of {group.Count} pieces");
            trackPieces.Pieces.RemoveAll(p => group.Any(g => ReferenceEquals(g, p)));
        }
        else if (trackSelected is { } s)
        {
            RememberTrackPieces($"delete the {s.Section.Name.ToLowerInvariant()}");
            trackPieces.Pieces.RemoveAll(p => ReferenceEquals(p, s));
        }
        else return;
        trackSelected = null; trackGroup = null;
        TrackPiecesChanged();
    }

    private void ReplaceTrackPiece(TrackPiece old, TrackPiece now, string what)
    {
        var i = trackPieces.Pieces.FindIndex(p => ReferenceEquals(p, old));
        if (i < 0) return;
        RememberTrackPieces($"{what} the {old.Section.Name.ToLowerInvariant()}");
        trackPieces.Pieces[i] = now; trackSelected = now;
        if (trackGroup is { } group) { var k = group.FindIndex(p => ReferenceEquals(p, old)); if (k >= 0) group[k] = now; }
        TrackPiecesChanged();
    }

    private void RememberTrackPieces(string what)
    {
        trackUndo.Push(new TrackState(trackPieces.Width, trackPieces.Pieces.ToList(), what));
        trackRedo.Clear();
        lastTrackEditAt = Environment.TickCount64;
        trackPlanPath = null;
        if (trackBuildButton is not null) trackBuildButton.IsEnabled = false;
    }

    // What Undo (or Redo) would take back on the pieces, or null: nothing, or the mode is off
    private string? TrackHistoryLabel(bool undo) => TrackPiecesActive && (undo ? trackUndo : trackRedo).TryPeek(out var s) ? s.What : null;

    // One step back (or forward) through the pieces' history: the state before it kept on the other stack
    private void StepTrackHistory(bool undo)
    {
        var from = undo ? trackUndo : trackRedo;
        if (!from.TryPop(out var step)) return;
        (undo ? trackRedo : trackUndo).Push(new TrackState(trackPieces.Width, trackPieces.Pieces.ToList(), step.What));
        lastTrackEditAt = Environment.TickCount64;
        trackPieces.Pieces.Clear(); trackPieces.Pieces.AddRange(step.Pieces); trackSelected = null; trackGroup = null; trackDragging = null; trackGroupDrag = null;
        trackPieces.Width = step.Width; SyncTrackWidthSlider();
        trackPlanPath = null;
        if (trackBuildButton is not null) trackBuildButton.IsEnabled = false;
        TrackPiecesChanged();
        SetTrackStatus($"{(undo ? "Undid" : "Redid")}: {step.What} ({trackPieces.Pieces.Count} piece(s)).");
    }

    private void TrackPiecesChanged()
    {
        // (the test start goes with the road under it -- the user, 2026-11-03: "The "Test start" flag seems undeleteable even when the
        // underlying track is removed")
        if (trackFlag is { } f && !TrackFlagOnRoad(f)) { trackFlag = null; trackFlagDragging = false; SetTrackStatus("The test start went with the road under it."); }
        SaveTrackPieces();
        UpdateTrackChecks();
        DrawTrackOverlay();
        if (trackArmed is null) SetTrackStatus($"{trackPieces.Pieces.Count} piece(s).");
    }

    // A starter track (TrackPieceSet.Starters) laid in the middle of the view, on the ground there, heading east
    private void LayStarterTrack(string name)
    {
        if (!TrackPiecesActive || TrackIslandOfView() is null) { SetTrackStatus("Open one of the race tracks' islands first."); return; }
        if (trackPieces.Pieces.Count > 0
            && MessageBox.Show(this, $"Replace the {trackPieces.Pieces.Count} piece(s) laid with the {name}? (Ctrl+Z brings them back.)", "Race track", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
        DisarmTrackPiece();
        double cx = targetX / 512, cz = targetZ / 512;
        var ground = SceneGround();
        var y = Math.Max(300.0, ground is not null ? IslandOps.Altitude(ground, targetX, targetZ) ?? 0 : 0);
        RememberTrackPieces($"start from the {name}");
        trackPieces.Pieces.Clear(); trackSelected = null; trackGroup = null; trackFlag = null;
        trackPieces.Pieces.AddRange(TrackPieceSet.Starter(name, cx - 15, cz - 12, y, 0));
        trackPlanPath = null;
        if (trackBuildButton is not null) trackBuildButton.IsEnabled = false;
        TrackPiecesChanged();
        SetTrackStatus($"The {name} is laid in the middle of the view: change it as you like (drag a piece, Delete, lay more), then Race it.");
    }

    // The pieces that take the road's end round to its start (TrackPieceSet.CloseLap), added
    private void CloseTrackLap()
    {
        if (!TrackPiecesActive) return;
        if (trackPieces.CloseLap(out var why) is not { } closing) { SetTrackStatus(why); return; }
        DisarmTrackPiece();
        RememberTrackPieces("close the lap");
        trackPieces.Pieces.AddRange(closing);
        trackPlanPath = null;
        if (trackBuildButton is not null) trackBuildButton.IsEnabled = false;
        TrackPiecesChanged();
        SetTrackStatus($"The lap is closed with {string.Join(", ", closing.Select(c => c.Section.Name))}.");
    }

    // The track's check, listed: a sign and a colour for each (✓ fine, • to know, ✗ stops it), the problems' places kept to mark in the view
    private void UpdateTrackChecks()
    {
        if (trackChecks is null) return;
        trackChecks.Children.Clear();
        var items = trackPieces.Check();
        trackProblemSpots = items.Where(i => i.Kind == TrackPieceSet.CheckKind.Problem).SelectMany(i => i.Spots).ToList();
        foreach (var item in items)
        {
            var (sign, brush) = item.Kind switch
            {
                TrackPieceSet.CheckKind.Ok => ("✓", "ThemeSuccessBrush"),
                TrackPieceSet.CheckKind.Note => ("•", "ThemeTextMuted2Brush"),
                _ => ("✗", "ThemeErrorBrush"),
            };
            var row = new DockPanel { Margin = new Thickness(0, 1, 0, 1) };
            var mark = new TextBlock { Text = sign, Width = 16, FontWeight = FontWeights.Bold };
            if (TryFindResource(brush) is Brush) mark.SetResourceReference(TextBlock.ForegroundProperty, brush);
            else mark.Foreground = item.Kind == TrackPieceSet.CheckKind.Ok ? Brushes.SeaGreen : item.Kind == TrackPieceSet.CheckKind.Problem ? Brushes.IndianRed : Brushes.Gray;
            DockPanel.SetDock(mark, Dock.Left);
            var text = new TextBlock { Text = item.Text, TextWrapping = TextWrapping.Wrap };
            text.SetResourceReference(TextBlock.ForegroundProperty, "ThemeTextBrush");
            System.Windows.Automation.AutomationProperties.SetName(row, (item.Kind == TrackPieceSet.CheckKind.Problem ? "Problem: " : item.Kind == TrackPieceSet.CheckKind.Ok ? "Fine: " : "Note: ") + item.Text);
            row.Children.Add(mark); row.Children.Add(text);
            trackChecks.Children.Add(row);
        }
    }

    // A piece's shape seen from above, for its button: its middle line, fitted into a small square (a climb drawn darker as it goes up)
    private static FrameworkElement TrackPieceIcon(Section section)
    {
        const double size = 22;
        var line = new TrackPiece(section, 0, 0, 0, -Math.PI / 2).Line();
        double x0 = line.Min(p => p.X), x1 = line.Max(p => p.X), z0 = line.Min(p => p.Z), z1 = line.Max(p => p.Z);
        var scale = (size - 6) / Math.Max(1e-6, Math.Max(x1 - x0, z1 - z0));
        double ox = (size - (x1 - x0) * scale) / 2 - x0 * scale, oz = (size - (z1 - z0) * scale) / 2 - z0 * scale;
        var canvas = new Canvas { Width = size, Height = size };
        Brush Of(SketchKind k) => k switch { SketchKind.Jump => Brushes.HotPink, SketchKind.Loop => Brushes.DeepSkyBlue, SketchKind.Pit => Brushes.Orange, _ => Brushes.Gray };
        for (var i = 1; i < line.Count; i++)
        {
            var kind = line[i].Kind;
            // (a jump's gap left open: no road drawn over it)
            if (kind == SketchKind.Jump) continue;
            canvas.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = line[i - 1].X * scale + ox, Y1 = line[i - 1].Z * scale + oz, X2 = line[i].X * scale + ox, Y2 = line[i].Z * scale + oz,
                Stroke = Of(kind), StrokeThickness = 3, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
            });
        }
        // (a loop: its ring stood up, a circle on the road)
        if (section.Parts.Any(p => p.Kind == SketchKind.Loop))
            canvas.Children.Add(new System.Windows.Shapes.Ellipse { Width = 9, Height = 9, Stroke = Brushes.DeepSkyBlue, StrokeThickness = 2, Margin = new Thickness(size / 2 - 4.5, size / 2 - 4.5, 0, 0) });
        // (its start: a dot where it begins)
        canvas.Children.Add(new System.Windows.Shapes.Ellipse { Width = 5, Height = 5, Fill = Brushes.LimeGreen, Margin = new Thickness(line[0].X * scale + ox - 2.5, line[0].Z * scale + oz - 2.5, 0, 0) });
        // (a climb or a drop: an arrow up or down beside it)
        if (Math.Abs(section.Rise) > 0 || section.Tilts)
            canvas.Children.Add(new TextBlock { Text = section.Tilts ? "↕" : section.Rise > 0 ? "↑" : "↓", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Brushes.Goldenrod, Margin = new Thickness(size - 8, -3, 0, 0) });
        return canvas;
    }

    // The track to a file to send to friends (RaceTrackShare: Little Big Racing Adventure's Racing > Shared tracks builds it) -- from where
    // it's laid, not by way of the race track window's plan file (the user, 2026-11-04: make track making "more accessible and user friendly")
    private void ShareTrackFromPieces()
    {
        if (TrackIslandOfView() is not { } island) { SetTrackStatus("Open one of the race tracks' islands first."); return; }
        // (a map's island: a copy of one of the game's islands shares as that island's track -- the same ground, cell for cell; a new island
        // of its own can't be yet, LBRA builds shared tracks on the game's islands)
        if (island.CustomMap is { } map)
        {
            var from = map.TrackFrom
                ?? (System.Text.RegularExpressions.Regex.Match(map.Description ?? "", @"^A race track on a copy of ([A-Z0-9_]+)\.") is { Success: true } m ? m.Groups[1].Value : null);
            var game = from is null ? null : RaceTrackIsland.BuiltIn.FirstOrDefault(i =>
                string.Equals(i.IleFile, from + ".ILE", StringComparison.OrdinalIgnoreCase) || string.Equals(i.TwinIleFile, from + ".ILE", StringComparison.OrdinalIgnoreCase));
            if (game is null)
            {
                SetTrackStatus("A track on an island of its own can't be shared yet: Little Big Racing Adventure builds shared tracks on the game's islands. " +
                               "Lay it on a copy of one of them (File > New track, \"A copy of one of the islands\") to share it.");
                return;
            }
            if (game.Scale > 1)
            {
                SetTrackStatus($"{game.Shown} is built bigger for its tracks than the copy you laid on: lay the track on {game.Shown} itself (open it, Build > Race track) to share it.");
                return;
            }
            island = game;
        }
        if (trackPlanPath is null) MakeTrackFromPieces();
        if (trackPlanPath is not { } plan) return;
        if (RaceTrackWindow.AskName(this, $"{island.Shown} by me") is not { } name) return;
        var safe = new string(name.Select(c => System.IO.Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
        var dialog = new Microsoft.Win32.SaveFileDialog { Title = "Share the track for LBRA", Filter = "LBRA shared track (*.lbrtrack)|*.lbrtrack", FileName = safe + RaceTrackShare.Extension };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            RaceTrackShare.Write(dialog.FileName, name, island, plan);
            SetTrackStatus($"\"{name}\" is saved as {dialog.FileName}. Send it to whoever you race: in Little Big Racing Adventure, Racing > Shared tracks > " +
                           "Add or remove tracks, then this file (or put it in the shared tracks folder). To race online, both of you add the same tracks.");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            SetTrackStatus($"The track can't be shared: {error.Message}");
        }
    }

    // ---- kept for the island ------------------------------------------------------------------------------------------------------------

    private sealed record SavedPiece(string Section, double X, double Z, double Y, double Heading, double Tilt = 0);
    private sealed record SavedPieces(double Width, List<SavedPiece> Pieces);

    private void SaveTrackPieces()
    {
        if (trackIsland.Length == 0) return;
        try
        {
            Directory.CreateDirectory(TrackPiecesFolder);
            var saved = new SavedPieces(trackPieces.Width, trackPieces.Pieces.Select(p => new SavedPiece(p.Section.Name, p.X, p.Z, p.Y, p.Heading, p.Tilt)).ToList());
            var file = TrackPiecesFile(trackIsland);
            // (a map's: written as the map's workspace files are, beside it and swapped in -- its folder's files are hard links)
            if (MapOpen) new FileTransaction().Write(file, System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(saved, new JsonSerializerOptions { WriteIndented = true }))).Commit();
            else File.WriteAllText(file, JsonSerializer.Serialize(saved, new JsonSerializerOptions { WriteIndented = true }));
            // (and into the map's file, a moment later)
            ScheduleMapTrackSave();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { DebugLog.Log($"track pieces not saved: {e.Message}"); }
    }

    private void LoadTrackPieces()
    {
        trackIsland = activeFile ?? "";
        trackKey = TrackKey;
        trackPieces.Pieces.Clear(); trackUndo.Clear(); trackRedo.Clear(); trackSelected = null; trackGroup = null; trackGroupDrag = null; trackPlanPath = null; trackFlag = null; trackFlagArmed = false; trackPieces.Width = 1;
        if (trackBuildButton is not null) trackBuildButton.IsEnabled = false;
        var file = TrackPiecesFile(trackIsland);
        if (!File.Exists(file)) return;
        try
        {
            var text = File.ReadAllText(file);
            var saved = text.TrimStart().StartsWith('[') ? new SavedPieces(1, JsonSerializer.Deserialize<List<SavedPiece>>(text) ?? new())
                : JsonSerializer.Deserialize<SavedPieces>(text) ?? new SavedPieces(1, new());
            trackPieces.Width = Math.Clamp(saved.Width, 1, TrackPieceSet.WidthMost);
            foreach (var s in saved.Pieces ?? new())
                if (Section.Named(s.Section, out var tilt) is { } section) trackPieces.Pieces.Add(new TrackPiece(section, s.X, s.Z, s.Y, s.Heading, s.Tilt != 0 ? s.Tilt : tilt));
        }
        catch (Exception e) when (e is IOException or JsonException) { DebugLog.Log($"track pieces not read: {e.Message}"); }
        finally { SyncTrackWidthSlider(); }
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
        // (the whole section it is part of: Drag whole sections ticked, or Shift held)
        var whole = hit is not null && (trackSectionsBox?.IsChecked == true || (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
        trackGroup = whole ? trackPieces.SectionOf(hit!) : null;
        if (hit is not null && TrackGround(at, out var gx, out var gz, out _))
        {
            if (trackGroup is { } group)
            {
                trackGroupDrag = group.Select(p => (trackPieces.Pieces.FindIndex(q => ReferenceEquals(q, p)), p)).Where(d => d.Item1 >= 0).ToList();
                trackGroupAnchor = (gx, gz);
                RememberTrackPieces($"move a section of {group.Count} pieces");
                SetTrackStatus($"A section of {group.Count} pieces: drag to move it (it goes on from an open end near it), Q, E to turn it, PgUp, PgDn to raise it, Delete to take it away.");
            }
            else
            {
                trackDragOffset = (hit.X - gx, hit.Z - gz);
                RememberTrackPieces($"move the {hit.Section.Name.ToLowerInvariant()}");
                SetTrackStatus($"{hit.Section.Name}: drag to move it, Q, E to turn it, PgUp, PgDn to raise it,{(hit.Section.Tilts ? $" R, F to tilt it ({RampSlope(hit.Tilt)})," : "")} " +
                               "Delete to take it away. Shift+drag moves the whole section.");
            }
            trackDragging = hit;
            TerrainViewport.CaptureMouse();
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
        RememberTrackPieces($"lay a {g.Section.Name.ToLowerInvariant()}");
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
            // (LBA2_TRACKPIECES_WIDTH: the road's width, a share of the usual, as the panel's slider sets it)
            if (double.TryParse(Environment.GetEnvironmentVariable("LBA2_TRACKPIECES_WIDTH"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var width))
            { trackPieces.Width = Math.Clamp(width, 1, TrackPieceSet.WidthMost); SyncTrackWidthSlider(); }
            foreach (var item in items.Skip(1))
            {
                // ("piece@x,z": dropped where it says, its way the last one's -- a gap left before it)
                // ("Ramp~1500": the Ramp tilted to climb that much, as R and F would)
                var name = item.Split('@')[0].Split('~')[0];
                if (Section.Named(name, out var oldTilt) is not { } section) { DebugLog.Log($"track pieces test: no piece {name}"); continue; }
                trackTilt = item.Split('@')[0].Split('~') is { Length: 2 } t ? double.Parse(t[1], System.Globalization.CultureInfo.InvariantCulture) : oldTilt;
                ArmTrackPiece(section);
                double x = first[0], z = first[1];
                if (trackPieces.Pieces.LastOrDefault() is { } last) { var e = last.End; x = e.X + Math.Cos(e.Heading) * 0.6; z = e.Z + Math.Sin(e.Heading) * 0.6; trackHeading = e.Heading; }
                if (item.Contains('@')) { var at2 = item.Split('@')[1].Split(','); x = double.Parse(at2[0], System.Globalization.CultureInfo.InvariantCulture); z = double.Parse(at2[1], System.Globalization.CultureInfo.InvariantCulture); }
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
            // (LBA2_TRACKPIECES_UNDO="n,m": n steps undone through Edit > Undo's own path -- Ctrl+Z's -- then m redone)
            if (Environment.GetEnvironmentVariable("LBA2_TRACKPIECES_UNDO") is { Length: > 0 } undoSpec)
            {
                var nm = undoSpec.Split(',').Select(int.Parse).ToArray();
                for (var k = 0; k < nm[0]; k++) { DebugLog.Log($"track pieces test: Edit > Undo would say \"{TrackHistoryLabel(true)}\" (the track's: {UndoIsTrack(true)})"); StepHistory(undo: true); }
                DebugLog.Log($"track pieces test: {nm[0]} undone, {trackPieces.Pieces.Count} piece(s)");
                for (var k = 0; k < (nm.Length > 1 ? nm[1] : 0); k++) StepHistory(undo: false);
                DebugLog.Log($"track pieces test: {(nm.Length > 1 ? nm[1] : 0)} redone, {trackPieces.Pieces.Count} piece(s); status: {trackStatus?.Text}");
            }
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
                var next = Section.Named(v[5], out _);
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
        if (trackDragging is not null && trackGroupDrag is { } drag)
        {
            // (the section moved as one by how far the ground under the pointer has moved, then on from an open end near it)
            if (!TrackGround(at, out var sx, out var sz, out _)) return true;
            double dx = sx - trackGroupAnchor.X, dz = sz - trackGroupAnchor.Z;
            void Place(double ox, double oz, double oy)
            {
                foreach (var (i, was) in drag)
                {
                    var now = was with { X = was.X + ox, Z = was.Z + oz, Y = was.Y + oy };
                    if (ReferenceEquals(trackPieces.Pieces[i], trackSelected)) trackSelected = now;
                    trackPieces.Pieces[i] = now;
                }
                trackGroup = drag.Select(d => trackPieces.Pieces[d.Index]).ToList();
            }
            Place(dx, dz, 0);
            var snap = trackPieces.SnapSection(trackGroup!);
            if (snap != (0, 0, 0)) Place(dx + snap.X, dz + snap.Z, snap.Y);
            DrawTrackOverlay();
            return true;
        }
        if (trackDragging is { } held && trackSelected is { } current)
        {
            if (!TrackGround(at, out var gx, out var gz, out _)) return true;
            var moved = trackPieces.Snapped(current with { X = gx + trackDragOffset.X, Z = gz + trackDragOffset.Z, Y = held.Y, Heading = held.Heading }, current);
            var i = trackPieces.Pieces.FindIndex(p => ReferenceEquals(p, current));
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
        trackDragging = null; trackGroupDrag = null;
        Mouse.Capture(null);
        // (a piece or a section clicked, to select it, and not moved: no step to undo)
        if (trackUndo.TryPeek(out var top) && top.Pieces.SequenceEqual(trackPieces.Pieces)) trackUndo.Pop();
        TrackPiecesChanged();
        return true;
    }

    private void UpdateTrackGhost(Point at)
    {
        if (trackArmed is not { } section || !TrackGround(at, out var gx, out var gz, out var gy)) { trackGhost = null; return; }
        trackGhost = trackPieces.Snapped(new TrackPiece(section, gx, gz, gy + trackLift, trackHeading, section.Tilts ? trackTilt : 0));
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

    // Whether a road piece still runs under the flag (within 2 cells of its middle line, about at its height)
    private bool TrackFlagOnRoad((double X, double Z, double Y) f) => trackPieces.Pieces.Where(p => !p.IsPit).SelectMany(p => p.Line())
        .Any(q => Math.Pow(q.X - f.X, 2) + Math.Pow(q.Z - f.Z, 2) < 4 && Math.Abs(q.Y - f.Y) < 1200);

    private void RemoveTrackFlag()
    {
        if (trackFlag is null && !trackFlagArmed) { SetTrackStatus("There's no test start to remove."); return; }
        trackFlag = null; trackFlagArmed = false; trackFlagDragging = false; TerrainViewport.Cursor = null;
        DrawTrackOverlay();
        SetTrackStatus("The test start removed: Place the test start puts it on the road again.");
    }

    // Where the flag's foot is drawn, or null
    private Point? TrackFlagScreen() =>
        trackFlag is { } f && cameraModel is { } model && model.Project(f.X * 512, f.Y, f.Z * 512, out var u, out var v) ? ToWidget(model, u, v) : null;

    // Test from the flag: the road from the flag on made into a sprint, built into the game folder with the tracks built there already
    // (each island's as it was, this island's the test), and raced in Play -- the race car setup asked first, as Play asks
    private async void TestTrackFromFlag()
    {
        // (no flag: the whole track from its start -- the user, 2026-11-04: track making "more accessible and user friendly")
        var flag = trackFlag;
        if (TrackIslandOfView() is not { } island || TrackIslandAsBuilt(island) is not { } ground) { SetTrackStatus("Open one of the race tracks' islands first."); return; }
        DisarmTrackPiece();
        var result = trackPieces.MakePlan(ground, flag, out var gridRoom);
        if (result.Plan is not { } plan) { SetTrackStatus("No test: " + string.Join(" ", result.Problems.Concat(result.Notes).Select(n => n.TrimEnd('.') + "."))); return; }
        // (a map's island: as the map is now -- its ground edited, its scenes -- MapInstall.Capture)
        if (island.CustomMap is not null && openMapInstalled is { } at)
        {
            if (terrainEditor is { Dirty: true } editor) editor.Save();
            try { island = RaceTrackIsland.Custom(Maps.MapInstall.Capture(at)); }
            catch (Exception e) when (e is IOException or InvalidDataException) { SetTrackStatus($"The map's island can't be read back: {e.Message}"); return; }
        }
        var answer = MessageBox.Show(this,
            $"Build the test -- {island.Shown} {(flag is null ? "from its start" : "from the flag")}, {string.Join(", ", result.Notes.Take(2))} -- and race it?\n\n" +
            "It's built into a scratch copy of the game folder: the game folder's own islands and tracks aren't changed.",
            "Test race", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (answer != MessageBoxResult.OK) return;
        if (playing) StopPlay();
        // (the scratch copy: the game folder hard-linked, the files the build writes made its own -- RaceTrackService.Build)
        if (trackTestRoot is null || !string.Equals(trackTestFor, gameRoot, StringComparison.OrdinalIgnoreCase))
        {
            trackTestRoot?.Dispose();
            LiveDataRoot.CleanStale(gameRoot, TrackTestFolderName);
            trackTestRoot = LiveDataRoot.Create(gameRoot, null, TrackTestFolderName);
            trackTestFor = gameRoot;
        }
        else
        {
            try { trackTestRoot.Sync(); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { DebugLog.Log($"track test: the scratch copy not brought up to date: {e.Message}"); }
        }
        if (trackTestRoot is null) { SetTrackStatus("The test couldn't be built: no scratch copy of the game folder could be made beside it."); return; }
        RaceTrackOptions Options(RaceTrackIsland i)
        {
            var o = RaceTrackOptions.For(i);
            o.Story = o.Story && i.IleFile == RaceTrackIsland.Citadel.IleFile;
            return o;
        }
        // (the tested track alone: the scratch copy needn't have the folder's other tracks)
        var builds = new List<RaceTrackService.TrackBuild>();
        // (no road behind the flag for a grid: Twinsen's car alone on the flag, no opponents -- they would stand on nothing)
        var tested = Options(island);
        if (!gridRoom) tested.Drivers = new List<RaceDriver>();
        // (Citadel Island: a plan is its town circuit's, the storm track built as it is)
        builds.Insert(0, island.RacesTwin ? new RaceTrackService.TrackBuild(RaceTrackPlan.Built(island), tested, plan) : new RaceTrackService.TrackBuild(plan, tested));
        if (trackTestButton is not null) trackTestButton.IsEnabled = false;
        SetTrackStatus("Building the test…");
        var root = trackTestRoot.Directory;
        DebugLog.Log($"track pieces: building the test of {island.Name} in {root}");
        var clock = System.Diagnostics.Stopwatch.StartNew();
        RaceTrackService.BuildResult built;
        try { built = await Task.Run(() => RaceTrackService.Build(root, builds)); }
        finally { if (trackTestButton is not null) trackTestButton.IsEnabled = true; }
        if (!built.Ok)
        {
            DebugLog.Log($"track pieces: the test couldn't be built ({clock.Elapsed.TotalSeconds:0} s): {built.Summary}");
            SetTrackStatus("The test couldn't be built: " + built.Summary); MessageBox.Show(this, built.Summary, "Test race", MessageBoxButton.OK, MessageBoxImage.Warning); return;
        }
        DebugLog.Log($"track pieces: test from the flag built in {root} ({clock.Elapsed.TotalSeconds:0} s) -- {built.Summary}");
        SetTrackStatus("The test is built (in a scratch copy: the game folder isn't changed): racing it. " + string.Join(" ", result.Notes.Select(n => n.TrimEnd('.') + ".")));
        StartPlay(GameKind.Lba2, root: root);
    }

    // The track test's scratch copy of the game folder (beside it: hard links need the same drive), made at the first test and kept for the
    // next, removed when the editor closes
    public const string TrackTestFolderName = "_TRACK_TEST";
    private LiveDataRoot? trackTestRoot;
    private string? trackTestFor;
    private void DisposeTrackTest() { trackTestRoot?.Dispose(); trackTestRoot = null; trackTestFor = null; }

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
            case Key.R when Keyboard.Modifiers == ModifierKeys.None: TiltTrackPiece(1); break;
            case Key.F when Keyboard.Modifiers == ModifierKeys.None: TiltTrackPiece(-1); break;
            case Key.Delete when trackArmed is null && trackSelected is not null: DeleteTrackPiece(); break;
            case Key.Delete when trackArmed is null && trackSelected is null && trackFlag is not null: RemoveTrackFlag(); break;
            case Key.Escape when trackFlagArmed: trackFlagArmed = false; TerrainViewport.Cursor = null; SetTrackStatus("The test start not placed."); break;
            case Key.Escape when trackArmed is not null: DisarmTrackPiece(); SetTrackStatus("Stopped laying. Choose a piece to lay another."); break;
            case Key.Escape when trackSelected is not null: trackSelected = null; trackGroup = null; DrawTrackOverlay(); break;
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
        TrackGhostEdge = FrozenPen(Color.FromArgb(230, 255, 255, 255), 1.5, dashed: true), TrackRing = FrozenPen(Color.FromRgb(255, 220, 40), 2.5),
        TrackProblem = FrozenPen(Color.FromRgb(235, 40, 40), 3);
    private static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    private static Pen FrozenPen(Color c, double width, bool dashed = false)
    {
        var pen = new Pen(Frozen(c), width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (dashed) pen.DashStyle = new DashStyle(new[] { 3.0, 2.0 }, 0);
        pen.Freeze();
        return pen;
    }
    // the road's half widths (cells): the asphalt's and to the kerbs' outer edge -- RaceTrackOptions' AsphaltHalfWidth, CurbHalfWidth
    private const double TrackDeckDepth = 300, TrackLoopRadius = 4, TrackLoopAcross = 3;
    // a jump's ramps as the build makes them (RaceTrackOptions.JumpRampHeight, JumpRampLength, JumpLandingLength; the landing ramp's height
    // the flight's end's, about 300)
    private const double JumpUpRamp = 800, JumpRampCells = 8, JumpDownRamp = 300, JumpLandCells = 12;
    // a loop's band and rails as the build makes them (RaceTrackBuilder.LoopBandHalf, cells; RaceTrackLoopBody: rail width, height and the
    // band's thickness, world units -- the rail's width here in cells)
    private const double TrackLoopBand = 1.3, TrackLoopRailWidth = 90 / 512.0, TrackLoopRail = 150, TrackLoopThickness = 160;

    // The faces, far to near, drawn as they come
    private sealed class TrackDrawing : FrameworkElement
    {
        public readonly List<(double Depth, Point[] Points, Brush? Fill, Pen? Pen)> Faces = new();
        public readonly List<Point> Rings = new();
        // the track check's problems: a red ring with a cross, seen as well as by its colour
        public readonly List<Point> Problems = new();
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
            foreach (var r in Problems)
            {
                dc.DrawEllipse(null, TrackProblem, r, 11, 11);
                dc.DrawLine(TrackProblem, new Point(r.X - 6, r.Y - 6), new Point(r.X + 6, r.Y + 6));
                dc.DrawLine(TrackProblem, new Point(r.X - 6, r.Y + 6), new Point(r.X + 6, r.Y - 6));
            }
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
        trackDrawing.Faces.Clear(); trackDrawing.Rings.Clear(); trackDrawing.Problems.Clear(); trackDrawing.Flag = null;
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
        double asphaltHalf = trackPieces.AsphaltHalf, kerbHalf = trackPieces.KerbHalf;
        var leaps = trackPieces.GapJumps();
        void Piece(TrackPiece p, bool selected, bool ghost)
        {
            var line = p.Line();
            var n = line.Count;
            // (a jump's ramps, as the build makes them: up JumpUpRamp over its last JumpRampCells before the lip -- a Jump piece's gap, or a gap
            // this piece's end leaps -- and down JumpDownRamp over JumpLandCells after the landing lip)
            var lift = new double[n];
            static double Rise(double f) => (1 - Math.Cos(Math.PI * Math.Clamp(f, 0, 1))) / 2;
            void Ramp(int lip, int way, double height, double cells)
            {
                for (var k = 0; k <= cells * 2; k++)
                {
                    var i = lip - way * k;
                    if (i < 0 || i >= n || line[i].Kind == SketchKind.Jump) continue;
                    lift[i] = Math.Max(lift[i], height * Rise(1 - k / (cells * 2)));
                }
            }
            for (var i = 0; i + 1 < n; i++)
            {
                if (line[i].Kind != SketchKind.Jump && line[i + 1].Kind == SketchKind.Jump) Ramp(i, 1, JumpUpRamp, JumpRampCells);
                if (line[i].Kind == SketchKind.Jump && line[i + 1].Kind != SketchKind.Jump) Ramp(i + 1, -1, JumpDownRamp, JumpLandCells);
            }
            if (leaps.Any(l => ReferenceEquals(l.From, p))) Ramp(n - 1, 1, JumpUpRamp, JumpRampCells);
            if (leaps.Any(l => ReferenceEquals(l.To, p))) Ramp(0, -1, JumpDownRamp, JumpLandCells);
            if (lift.Any(v => v > 0)) line = line.Select((q, i) => (q.X, q.Z, q.Y + lift[i], q.Kind)).ToList();
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
                    foreach (var side in new[] { -kerbHalf, kerbHalf })
                        Face(TrackDeck, null, At(i, side), At(i + 1, side), At(i + 1, side, -TrackDeckDepth), At(i, side, -TrackDeckDepth));
                Face(asphalt, ghost ? TrackGhostEdge : null, At(i, -asphaltHalf), At(i + 1, -asphaltHalf), At(i + 1, asphaltHalf), At(i, asphaltHalf));
                // (the kerbs: a cell red, a cell white -- two of the line's half cells each)
                var kerb = ghost ? TrackGhostKerb : (i / 2) % 2 == 0 ? TrackKerbRed : TrackKerbWhite;
                Face(kerb, null, At(i, -kerbHalf), At(i + 1, -kerbHalf), At(i + 1, -asphaltHalf), At(i, -asphaltHalf));
                Face(kerb, null, At(i, asphaltHalf), At(i + 1, asphaltHalf), At(i + 1, kerbHalf), At(i, kerbHalf));
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
        foreach (var p in trackPieces.Pieces) Piece(p, ReferenceEquals(p, trackSelected) || trackGroup?.Any(g => ReferenceEquals(g, p)) == true, false);
        // (a gap left between pieces, which the build makes a jump: its flight's arc from the up ramp's lip to the landing)
        foreach (var (a, b) in leaps)
        {
            var e = a.End;
            double length = Math.Sqrt(Math.Pow(b.X - e.X, 2) + Math.Pow(b.Z - e.Z, 2)) * 512, y0 = e.Y + JumpUpRamp, y1 = b.Y + JumpDownRamp;
            for (var k = 0; k < 16; k++)
            {
                double f0 = k / 16.0, f1 = (k + 1) / 16.0;
                (double X, double Y, double Z) Arc(double f) => (e.X + (b.X - e.X) * f, y0 + (y1 - y0) * f + length / 4 * 4 * f * (1 - f), e.Z + (b.Z - e.Z) * f);
                Face(null, TrackFlight, Arc(f0), Arc(f1));
            }
        }
        if (trackGhost is { } g) Piece(g, false, true);
        foreach (var e in trackPieces.OpenEnds().Concat(trackPieces.OpenStarts()))
            if (model.Project(e.X * 512, e.Y, e.Z * 512, out var u, out var v)) trackDrawing.Rings.Add(ToWidget(model, u, v));
        foreach (var e in trackProblemSpots)
            if (model.Project(e.X * 512, e.Y, e.Z * 512, out var u, out var v)) trackDrawing.Problems.Add(ToWidget(model, u, v));
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
        if (island.CustomMap is not null) { SetTrackStatus("A map's island has no race track window: Test from the flag races its track (put the flag at its start for the whole of it)."); return; }
        OpenRaceTrackWindow(w => w.UsePlan(island, path));
    }
}
