using Section = LBAAssembler.Terrain.TrackSection;

namespace LBAAssembler.Terrain;

// Track pieces laid on the island in the editor's 3D view (MainWindow.TrackPieces; the user, 2026-11-03: "allowing a user to pick an island
// in our main viewport and by selecting pre-drawn track pieces drag and drop them. This should help with making vertical tracks as this
// viewport is fully turnable and true to size"): the race track generator's sections (TrackGeneratorWindow.Section) placed where they begin
// (island cells), how high (world units) and their way (radians: 0 along +x, + turning towards +z, as the generator's map has it). A piece
// dropped near another's open end goes on from it, at its height; the pieces in one chain become a plan through the generator's own tracer
// (TrackSketchTracer), at the heights they were laid at -- one over another where they were stacked.
// Tilt: the Ramp's climb over its length (world units, - a drop), the piece's own (TrackSection.Tilts); 0 for every other piece.
internal sealed record TrackPiece(Section Section, double X, double Z, double Y, double Heading, double Tilt = 0)
{
    // the Ramp's steepest either way and its step (R, F): 5 % of its length a step, 30 % at most
    public const double TiltStep = 0.05 * Section.RampLength * 512, TiltMost = 6 * TiltStep;

    // The piece tilted `way` steps about its middle (the Ramp): its middle where it was, its start as far down as its end goes up
    public TrackPiece TiltedBy(int way)
    {
        var tilt = Math.Clamp(Tilt + way * TiltStep, -TiltMost, TiltMost);
        return this with { Tilt = tilt, Y = Y - (tilt - Tilt) / 2 };
    }

    // Its middle line, a point every half cell: island cells across, world units up, each point's kind
    public List<(double X, double Z, double Y, SketchKind Kind)> Line()
    {
        var line = new List<(double, double, double, SketchKind)> { (X, Z, Y, Section.Parts[0].Kind) };
        double x = X, z = Z, y = Y, h = Heading;
        var length = Section.Parts.Sum(p => p.Length);
        foreach (var part in Section.Parts)
        {
            var n = Math.Max(2, (int)Math.Ceiling(part.Length * 2));
            var rise = part.Rise + (Section.Tilts ? Tilt * part.Length / length : 0);
            for (var i = 1; i <= n; i++)
            {
                var ds = part.Length / n;
                if (part.Radius > 0) h += part.Turn / n;
                // (an arc: each small step along the way turned so far, half of this step's turn in it -- the generator's Strokes)
                var mid = part.Radius > 0 ? h - part.Turn / n / 2 : h;
                x += Math.Cos(mid) * ds; z += Math.Sin(mid) * ds; y += rise / n;
                line.Add((x, z, y, part.Kind));
            }
        }
        return line;
    }

    public (double X, double Z, double Y, double Heading) End
    {
        get { var last = Line()[^1]; return (last.X, last.Z, last.Y, Heading + Section.Turn); }
    }

    public bool IsPit => Section.Kind == SketchKind.Pit;
}

internal sealed class TrackPieceSet
{
    // a piece dropped within this of an open end (cells across, world units up) goes on from it; open ends this near are one road
    public const double SnapCells = 8, SnapHeight = 1200, JoinCells = 3;
    // (the last piece's end this near the first's start closes the lap: laid by hand, round a lap, it seldom meets it exactly -- the
    // generator's BridgeCells)
    public const double CloseCells = 14;
    // (the shortest stretch that can be tried: a start line, room to get going and a finish line -- a lap of the generator's is 120)
    public const double ShortestTry = 40;
    // (the road a sprint's start line has behind it: its grid's six cars, three rows of two a car's length and more apart)
    public const double RunUp = 24;
    // (turning a piece this much to put it on an end weighs as much as moving it SnapCells -- the generator's)
    private const double SnapTurn = 70 * Math.PI / 180;
    // the road's half width on the tracer's sketch (cells) and the sketch's pixels to a cell
    private const double RoadCells = 3;
    private const int Scale = 2;

    public List<TrackPiece> Pieces { get; } = new();
    // How wide the road is, a share of the usual (the user, 2026-11-03: "add a setting to allow a user to make the track wider if they
    // wish"): its asphalt, its kerbs' and verge's edges and a raised road's deck that many times as far from its middle -- a kerb a cell wide
    public double Width { get; set; } = 1;
    public const double WidthMost = 2;
    // the road's half widths as built (RaceTrackOptions: AsphaltHalfWidth 3.5, VergeHalfWidth 6; RaisedHalfWidth 3.75)
    public double AsphaltHalf => 3.5 * Width;
    public double KerbHalf => AsphaltHalf + 1;

    private static double Gap((double X, double Z, double Y) a, (double X, double Z, double Y) b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
    private static bool Joins((double X, double Z, double Y) a, (double X, double Z, double Y) b, double cells) => Gap(a, b) < cells && Math.Abs(a.Y - b.Y) < SnapHeight;

    // The road pieces' ends no road piece goes on from, and their starts none leads into (`except` left out: the one being moved)
    public IEnumerable<(double X, double Z, double Y, double Heading)> OpenEnds(TrackPiece? except = null)
    {
        var roads = Pieces.Where(p => !p.IsPit && !ReferenceEquals(p, except)).ToList();
        foreach (var p in roads)
        {
            var e = p.End;
            if (!roads.Any(o => !ReferenceEquals(o, p) && Joins((o.X, o.Z, o.Y), (e.X, e.Z, e.Y), JoinCells))) yield return e;
        }
    }

    public IEnumerable<(double X, double Z, double Y, double Heading)> OpenStarts(TrackPiece? except = null)
    {
        var roads = Pieces.Where(p => !p.IsPit && !ReferenceEquals(p, except)).ToList();
        foreach (var p in roads)
            if (!roads.Any(o => !ReferenceEquals(o, p) && Joins((o.End.X, o.End.Z, o.End.Y), (p.X, p.Z, p.Y), JoinCells))) yield return (p.X, p.Z, p.Y, p.Heading);
    }

    // `piece` where it would go: its start on the nearest open end (on at that end's height and way), or its end on the nearest open start
    // (leading into it), whichever is nearer for the least turning -- or as it is, nothing being near. Pit lanes snap to nothing.
    public TrackPiece Snapped(TrackPiece piece, TrackPiece? self = null)
    {
        if (piece.IsPit) return piece;
        TrackPiece? best = null; var score = double.MaxValue;
        static double Turned(double a, double b) => Math.Abs(Math.IEEERemainder(a - b, 2 * Math.PI));
        foreach (var e in OpenEnds(self))
        {
            var d = Gap((piece.X, piece.Z, 0), (e.X, e.Z, 0));
            if (d > SnapCells) continue;
            var s = d + Turned(piece.Heading, e.Heading) / SnapTurn * SnapCells;
            if (s < score) { score = s; best = piece with { X = e.X, Z = e.Z, Y = e.Y, Heading = e.Heading }; }
        }
        var end = piece.End;
        foreach (var st in OpenStarts(self))
        {
            var d = Gap((end.X, end.Z, 0), (st.X, st.Z, 0));
            if (d > SnapCells) continue;
            var s = d + Turned(end.Heading, st.Heading) / SnapTurn * SnapCells;
            if (s >= score) continue;
            // (the piece turned so it ends on the start's way, then moved so it ends on it)
            var turned = piece with { X = 0, Z = 0, Y = 0, Heading = st.Heading - piece.Section.Turn };
            var off = turned.End;
            score = s;
            best = turned with { X = st.X - off.X, Z = st.Z - off.Z, Y = st.Y - off.Y };
        }
        return best ?? piece;
    }

    // The whole section of road `piece` is part of (the user, 2026-11-03: "allow the user to move whole sections of track, or click on an
    // individual piece of track to move it"): every road piece joined to it, end to start either way or across a gap jump, and those joined
    // to them; a pit lane alone
    public List<TrackPiece> SectionOf(TrackPiece piece)
    {
        if (piece.IsPit) return new() { piece };
        var roads = Pieces.Where(p => !p.IsPit).ToList();
        var leaps = GapJumps();
        bool Linked(TrackPiece a, TrackPiece b) =>
            Joins((a.End.X, a.End.Z, a.End.Y), (b.X, b.Z, b.Y), JoinCells) || Joins((b.End.X, b.End.Z, b.End.Y), (a.X, a.Z, a.Y), JoinCells)
            || leaps.Any(l => ReferenceEquals(l.From, a) && ReferenceEquals(l.To, b) || ReferenceEquals(l.From, b) && ReferenceEquals(l.To, a));
        var found = new List<TrackPiece> { piece };
        for (var k = 0; k < found.Count; k++)
            foreach (var r in roads)
                if (!found.Any(f => ReferenceEquals(f, r)) && Linked(found[k], r)) found.Add(r);
        return found;
    }

    // How far to move a section (`group`, as it lies now) so one of its open ends goes on from a road's open end, or its open start, outside
    // it, within SnapCells -- moved only, not turned (Q and E turn it); zero with none near
    public (double X, double Z, double Y) SnapSection(List<TrackPiece> group)
    {
        bool In(TrackPiece p) => group.Any(g => ReferenceEquals(g, p));
        var others = Pieces.Where(p => !p.IsPit && !In(p)).ToList();
        var mine = group.Where(p => !p.IsPit).ToList();
        IEnumerable<(double X, double Z, double Y)> Ends(List<TrackPiece> set) =>
            set.Where(p => !set.Any(o => !ReferenceEquals(o, p) && Joins((p.End.X, p.End.Z, p.End.Y), (o.X, o.Z, o.Y), JoinCells))).Select(p => (p.End.X, p.End.Z, p.End.Y));
        IEnumerable<(double X, double Z, double Y)> Starts(List<TrackPiece> set) =>
            set.Where(p => !set.Any(o => !ReferenceEquals(o, p) && Joins((o.End.X, o.End.Z, o.End.Y), (p.X, p.Z, p.Y), JoinCells))).Select(p => (p.X, p.Z, p.Y));
        (double X, double Z, double Y) best = (0, 0, 0); var nearest = SnapCells;
        void Try((double X, double Z, double Y) from, (double X, double Z, double Y) to)
        {
            var d = Gap(from, to);
            if (d < nearest) { nearest = d; best = (to.X - from.X, to.Z - from.Z, to.Y - from.Y); }
        }
        foreach (var s in Starts(mine)) foreach (var e in Ends(others)) Try(s, e);
        foreach (var e in Ends(mine)) foreach (var s in Starts(others)) Try(e, s);
        return best;
    }

    // The road pieces end to start in one chain: their middle line in order (cells, world units up, kinds), and whether it comes back round
    // to its first; null when there are none. Pieces that aren't one chain -- a track still being laid, a piece put down apart -- give the
    // longest run of them that is (`left`: the road pieces left out), so a track can be tried before it is whole.
    public List<(double X, double Z, double Y, SketchKind Kind)>? Chain(out bool closed) => Chain(out closed, out _, out _);

    // A gap left between two pieces that a car can leap (the user, 2026-11-03: "Any gaps that have been left let's automatically add scripting
    // to make them into a jump"): `b`'s start ahead of `a`'s end, JumpFrom to JumpTo cells on along a's way (within JumpAim of it), b going
    // the same way (within JumpTurn), and no higher than a car leaps up to (a drop below is fine). The build makes it a gap jump -- an up
    // ramp to the lip, the flight, a landing ramp -- as the generator's Jump piece is.
    public const double JumpFrom = JoinCells, JumpTo = 22;
    private const double JumpAim = 20 * Math.PI / 180, JumpTurn = 25 * Math.PI / 180, JumpUp = 300, JumpDown = 4000;
    public static bool Leaps(TrackPiece a, TrackPiece b)
    {
        var e = a.End;
        double dx = b.X - e.X, dz = b.Z - e.Z, d = Math.Sqrt(dx * dx + dz * dz);
        if (d <= JumpFrom || d > JumpTo || b.Y - e.Y > JumpUp || e.Y - b.Y > JumpDown) return false;
        static double Turned(double x, double y) => Math.Abs(Math.IEEERemainder(x - y, 2 * Math.PI));
        return Turned(Math.Atan2(dz, dx), e.Heading) <= JumpAim && Turned(b.Heading, e.Heading) <= JumpTurn;
    }

    // The gaps the road pieces leave that are jumps: each its take-off (a piece's end) and its landing (the next's start)
    public List<(TrackPiece From, TrackPiece To)> GapJumps()
    {
        var roads = Pieces.Where(p => !p.IsPit).ToList();
        var jumps = new List<(TrackPiece, TrackPiece)>();
        foreach (var a in roads)
        {
            if (roads.Any(b => !ReferenceEquals(a, b) && Joins((a.End.X, a.End.Z, a.End.Y), (b.X, b.Z, b.Y), JoinCells))) continue;
            var landing = roads.Where(b => !ReferenceEquals(a, b) && Leaps(a, b)
                                          && !roads.Any(o => !ReferenceEquals(o, b) && Joins((o.End.X, o.End.Z, o.End.Y), (b.X, b.Z, b.Y), JoinCells)))
                .OrderBy(b => Gap((a.End.X, a.End.Z, a.End.Y), (b.X, b.Z, b.Y))).FirstOrDefault();
            if (landing is not null) jumps.Add((a, landing));
        }
        return jumps;
    }

    public List<(double X, double Z, double Y, SketchKind Kind)>? Chain(out bool closed, out int left, out List<TrackPiece> order)
    {
        closed = false; left = 0; order = new List<TrackPiece>();
        var roads = Pieces.Where(p => !p.IsPit).ToList();
        if (roads.Count == 0) return null;
        var leaps = GapJumps();
        // (the next piece: the one its end joins, else the one it leaps a gap onto)
        TrackPiece? Next(TrackPiece a, IEnumerable<TrackPiece> from) =>
            from.Where(b => !ReferenceEquals(a, b) && Joins((a.End.X, a.End.Z, a.End.Y), (b.X, b.Z, b.Y), JoinCells)).OrderBy(b => Gap((a.End.X, a.End.Z, a.End.Y), (b.X, b.Z, b.Y))).FirstOrDefault()
            ?? leaps.Where(l => ReferenceEquals(l.From, a) && from.Contains(l.To)).Select(l => l.To).FirstOrDefault();
        // (each run from a piece nothing leads into -- or, a ring, from its first -- followed on; the longest of them)
        var firsts = roads.Where(r => !roads.Any(o => !ReferenceEquals(o, r) && ReferenceEquals(Next(o, roads), r))).ToList();
        if (firsts.Count == 0) firsts.Add(roads[0]);
        foreach (var first in firsts)
        {
            var run = new List<TrackPiece> { first };
            while (Next(run[^1], roads.Except(run)) is { } n) run.Add(n);
            if (run.Sum(p => p.Section.Parts.Sum(q => q.Length)) > order.Sum(p => p.Section.Parts.Sum(q => q.Length))) order = run;
        }
        left = roads.Count - order.Count;
        closed = order.Count > 1 && Joins((order[^1].End.X, order[^1].End.Z, order[^1].End.Y), (order[0].X, order[0].Z, order[0].Y), CloseCells);
        var line = new List<(double X, double Z, double Y, SketchKind Kind)>();
        TrackPiece? previous = null;
        foreach (var p in order.ToList())
        {
            // (a gap leapt from the piece before: the flight's line over it, a jump's, from its end to this one's start)
            if (previous is not null && leaps.Any(l => ReferenceEquals(l.From, previous) && ReferenceEquals(l.To, p)))
            {
                var e = previous.End;
                var steps = Math.Max(2, (int)Math.Ceiling(Gap((e.X, e.Z, e.Y), (p.X, p.Z, p.Y)) * 2));
                for (var s = 1; s < steps; s++)
                {
                    var f = (double)s / steps;
                    line.Add((e.X + (p.X - e.X) * f, e.Z + (p.Z - e.Z) * f, e.Y + (p.Y - e.Y) * f, SketchKind.Jump));
                }
            }
            foreach (var q in p.Line())
                if (line.Count == 0 || Math.Abs(line[^1].X - q.X) + Math.Abs(line[^1].Z - q.Z) > 0.2) line.Add(q);
            previous = p;
        }
        return line;
    }

    // The plan's road as wide as Width says -- and, a road all on the ground with no jump or loop (the tracer gives such a plan no heights),
    // its heights the ground's, smoothed: the builder lays a road where its plan says it starts only with them (a plan's Planned), and a
    // flat island's track laid from pieces had no start line, so no race (File > New track's own island, 2026-11-03)
    private void Widen(RaceTrackPlan? plan, IslandFile island)
    {
        if (plan is null) return;
        if (plan.Heights is not { Length: > 0 } && plan.Points is { Length: > 1 } points)
        {
            var ground = points.Select(p => Math.Max(300.0, IslandOps.Altitude(island, (p[0] + plan.OriginCellX) * 512, (p[1] + plan.OriginCellZ) * 512) ?? 0)).ToArray();
            for (var pass = 0; pass < 3; pass++)
            {
                var next = (double[])ground.Clone();
                for (var k = 0; k < ground.Length; k++)
                {
                    int a = plan.Open ? Math.Max(0, k - 3) : k - 3, b = plan.Open ? Math.Min(ground.Length - 1, k + 3) : k + 3;
                    double sum = 0; var n = 0;
                    for (var j = a; j <= b; j++) { sum += ground[((j % ground.Length) + ground.Length) % ground.Length]; n++; }
                    next[k] = sum / n;
                }
                ground = next;
            }
            plan.Heights = ground.Select(v => Math.Round(v)).ToArray();
            plan.MaxGrade ??= 0.15;
        }
        if (Math.Abs(Width - 1) < 0.01) return;
        plan.AsphaltHalf = AsphaltHalf; plan.CurbHalf = KerbHalf; plan.VergeHalf = 6 * Width;
        plan.RaisedHalf = (plan.RaisedHalf ?? 3.75) * Width;
    }

    // The plan the pieces make on `island` (the island as its track is built on it: IslandScaler.AsBuilt), or why not (the result's Problems)
    // `from`: a test race's start (the 3D view's flag; the user, 2026-11-03: "drag and drop for where they want to start a test race from so
    // they can test individual segments") -- the road from the point nearest it, a sprint: to the road's end, or once round a lap
    public SketchResult MakePlan(IslandFile island, (double X, double Z, double Y)? from = null) => MakePlan(island, from, out _);

    // (a test from the flag: the shortest stretch it races, and the least road ahead of the flag before the test drives the road the
    // other way -- the user, 2026-11-03: "I just tried to test a 3 segment track by placing the flag at one end but the test was refused
    // as it was at the end of the track. If we want to test a section let us test it, just position a car on the track at the flag point")
    public const double TestShortest = 10, TestAhead = 12;
    // (how far behind the start line Twinsen's car stands, and a cell more: RaceTrackScenes' pole, RaceTrackOptions.GridFirst 3)
    public const double PoleRoom = 4;

    // ... and whether the road behind the flag has room for a grid (RunUp cells): without, the test is Twinsen's car alone on the flag
    public SketchResult MakePlan(IslandFile island, (double X, double Z, double Y)? from, out bool gridRoom)
    {
        gridRoom = true;
        var notes = new List<string>(); var problems = new List<string>();
        var chain = Chain(out var closed, out var left, out var order);
        if (chain is null || chain.Count < 2)
        {
            problems.Add(Pieces.Count == 0 ? "No track pieces laid yet." : "No road pieces laid yet: a pit lane goes beside a road.");
            return new SketchResult(null, new(), new(), false, 0, null, null, notes, problems);
        }
        // (a track not finished yet: tried as far as it goes, start to finish once -- the user, 2026-11-03: "check users can test it in
        // LBAAssembler even when it isn't complete")
        if (left > 0) notes.Add($"{left} piece(s) not joined to the longest road left out (put each on from an open end, the yellow rings)");
        var leaps = GapJumps().Where(l => order.Contains(l.From) && order.Contains(l.To)).ToList();
        foreach (var (a, b) in leaps)
            notes.Add($"the gap of {Gap((a.End.X, a.End.Z, 0), (b.X, b.Z, 0)):0} cells after the {a.Section.Name.ToLowerInvariant()} made a jump: a ramp up to its lip, a landing ramp");
        // (a sprint's start line: RunUp cells along it where the road before it allows -- its grid stands on that road, its cars none of them
        // past the road's end in the air: the user, 2026-11-03, of a test from a flag on a raised road, "That opponent is floating in mid-air")
        double LengthTo(List<(double X, double Z, double Y, SketchKind Kind)> c, int upTo)
        {
            double s = 0;
            for (var i = 1; i <= upTo && i < c.Count; i++) s += Math.Sqrt(Math.Pow(c[i].X - c[i - 1].X, 2) + Math.Pow(c[i].Z - c[i - 1].Z, 2));
            return s;
        }
        int? openStart = null;
        var shortest = ShortestTry;
        if (from is { } f)
        {
            int Nearest() => Enumerable.Range(0, chain.Count).MinBy(i => Math.Pow(chain[i].X - f.X, 2) + Math.Pow(chain[i].Z - f.Z, 2) + Math.Pow((chain[i].Y - f.Y) / 512, 2));
            var k = Nearest();
            shortest = TestShortest;
            double roadLength = LengthTo(chain, chain.Count - 1), before = LengthTo(chain, k);
            // (an open road with the flag near its end: driven the other way, from the flag back along it)
            if (!closed && roadLength - before < TestAhead && before > roadLength - before)
            {
                chain.Reverse();
                k = Nearest();
                before = LengthTo(chain, k);
                notes.Add("the flag is near the road's end: the test drives the road the other way, from the flag");
            }
            if (closed)
            {
                // (round a lap: from RunUp cells before the flag, the start line on the flag, once round as a sprint)
                var back = k;
                double behind = 0;
                for (var steps = 0; steps < chain.Count - 1 && behind < RunUp; steps++)
                {
                    var prev = (back - 1 + chain.Count) % chain.Count;
                    behind += Math.Sqrt(Math.Pow(chain[back].X - chain[prev].X, 2) + Math.Pow(chain[back].Z - chain[prev].Z, 2));
                    back = prev;
                }
                chain = chain.Skip(back).Concat(chain.Take(back)).ToList();
                openStart = (int)Math.Round(behind);
                notes.Add("tried from the flag: once round the lap from it, as a sprint");
                closed = false;
            }
            else
            {
                // (an open road: all of it built, the start line on the flag, the road before it behind him -- or, with less road behind
                // the flag than his car stands back from the line, the line PoleRoom past the flag: his car on the flag, on the road)
                openStart = (int)Math.Round(before >= PoleRoom ? before : Math.Min(before + PoleRoom, Math.Max(before, roadLength - 6)));
                gridRoom = before >= RunUp;
                notes.Add(gridRoom ? "tried from the flag to the road's end, as a sprint"
                    : "tried from the flag to the road's end, as a sprint: Twinsen's car alone on the flag (no room behind it for a grid)");
            }
            if (chain.Count < 2 || LengthTo(chain, chain.Count - 1) - openStart < 4)
            {
                problems.Add("There is no road to drive from the flag: lay some first.");
                return new SketchResult(null, new(), new(), false, 0, null, null, notes, problems);
            }
        }
        var used = order.Concat(Pieces.Where(p => p.IsPit)).ToList();
        // the generator's sketch round the pieces: the road's kinds stamped (jumps, loops and the pit lane are read from it), the chain its middle
        var all = used.SelectMany(p => p.Line()).ToList();
        int x0 = (int)Math.Floor(all.Min(p => p.X)) - 30, z0 = (int)Math.Floor(all.Min(p => p.Z)) - 30;
        int w = ((int)Math.Ceiling(all.Max(p => p.X)) + 30 - x0) * Scale, h = ((int)Math.Ceiling(all.Max(p => p.Z)) + 30 - z0) * Scale;
        var sketch = new TrackSketch(w, h, Scale, x0, z0);
        var radius = RoadCells * Scale;
        void Stamp((double X, double Z) a, (double X, double Z) b, SketchKind kind, float height)
        {
            double ax = (a.X - x0) * Scale, ay = (a.Z - z0) * Scale, bx = (b.X - x0) * Scale, by = (b.Z - z0) * Scale;
            var len = Math.Max(1, (int)Math.Ceiling(Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay))));
            for (var s = 0; s <= len; s++)
            {
                double x = ax + (bx - ax) * s / len, y = ay + (by - ay) * s / len;
                for (var yy = (int)(y - radius); yy <= (int)(y + radius); yy++)
                    for (var xx = (int)(x - radius); xx <= (int)(x + radius); xx++)
                    {
                        if (xx < 0 || yy < 0 || xx >= w || yy >= h || (xx - x) * (xx - x) + (yy - y) * (yy - y) > radius * radius) continue;
                        // (a jump, a loop or the pit lane where the road crosses it stays one: the road over it is the plain road)
                        var i = yy * w + xx;
                        if (kind != SketchKind.Road || sketch.Kinds[i] == SketchKind.None) sketch.Kinds[i] = kind;
                        if (!float.IsNaN(height)) sketch.Heights[i] = float.IsNaN(sketch.Heights[i]) ? height : Math.Max(sketch.Heights[i], height);
                    }
            }
        }
        // (heights stamped where a piece is up off the ground: the tracer makes the road a raised one there; the exact heights follow below)
        double Ground(double x, double z) => Math.Max(0.0, IslandOps.Altitude(island, x * 512, z * 512) ?? 0);
        foreach (var (a, b) in leaps)
        {
            var e = a.End;
            var up = e.Y > Ground(e.X, e.Z) + 500 ? (float)e.Y : float.NaN;
            Stamp((e.X, e.Z), (b.X, b.Z), SketchKind.Jump, up);
        }
        foreach (var p in used)
        {
            var line = p.Line();
            for (var i = 0; i + 1 < line.Count; i++)
            {
                var up = line[i].Y > Ground(line[i].X, line[i].Z) + 500 ? (float)line[i].Y : float.NaN;
                Stamp((line[i].X, line[i].Z), (line[i + 1].X, line[i + 1].Z), line[i + 1].Kind, up);
            }
        }
        // (an unfinished track's sprint from its first piece: the start line as far along as the grid needs, where it is long enough)
        if (openStart is null && !closed) openStart = (int)Math.Min(RunUp, LengthTo(chain, chain.Count - 1) / 3);
        var given = chain.Select(p => ((p.X - x0) * Scale, (p.Z - z0) * Scale)).ToList();
        var rise = chain.Select(p => p.Y - chain[0].Y).ToList();
        var first = TrackSketchTracer.Trace(sketch, island, false, given, closed, rise, shortest: shortest, openStart: openStart);
        if (first.Route.Count < 10 || !chain.Any(p => p.Y > Ground(p.X, p.Z) + 500)) { first.Notes.InsertRange(0, notes); Widen(first.Plan, island); return first; }
        // the height each point of the route was laid at: the chain at the same share of the way along (a lap's route begins elsewhere
        // round it: the turn of the one against the other that puts their points nearest)
        var route = first.Route;
        var at = new double[chain.Count];
        for (var i = 1; i < chain.Count; i++) at[i] = at[i - 1] + Gap((chain[i].X, chain[i].Z, 0), (chain[i - 1].X, chain[i - 1].Z, 0));
        var total = at[^1];
        (double X, double Z, double Y) Along(double f)
        {
            var want = Math.Clamp(f, 0, 1) * total;
            var j = Array.BinarySearch(at, want); if (j < 0) j = ~j;
            j = Math.Clamp(j, 1, chain.Count - 1);
            var t = Math.Clamp((want - at[j - 1]) / Math.Max(1e-9, at[j] - at[j - 1]), 0, 1);
            return (chain[j - 1].X + (chain[j].X - chain[j - 1].X) * t, chain[j - 1].Z + (chain[j].Z - chain[j - 1].Z) * t, chain[j - 1].Y + (chain[j].Y - chain[j - 1].Y) * t);
        }
        var n = route.Count;
        var offset = 0;
        if (first.Closed)
        {
            var best = double.MaxValue;
            for (var o = 0; o < n; o++)
            {
                double sum = 0;
                for (var q = 0; q < 16 && sum < best; q++)
                {
                    var c = Along(q / 16.0); var r = route[(q * n / 16 + o) % n];
                    sum += (c.X - r.X) * (c.X - r.X) + (c.Z - r.Z) * (c.Z - r.Z);
                }
                if (sum < best) { best = sum; offset = o; }
            }
        }
        var profile = new double[n];
        for (var k = 0; k < n; k++)
        {
            var j = ((k - offset) % n + n) % n;
            profile[k] = Along(first.Closed ? (double)j / n : (double)j / Math.Max(1, n - 1)).Y;
        }
        var second = TrackSketchTracer.Trace(sketch, island, false, given, closed, rise, profile, route.ToList(), shortest, openStart);
        second.Notes.InsertRange(0, notes);
        Widen(second.Plan, island);
        return second;
    }

    // ---- starter tracks and closing a lap (the user, 2026-11-04: make track making "more accessible and user friendly"): a track to begin
    // from rather than an empty island, and the pieces that bring a road's end back round to its start

    // The starters: a name, a tip, and their pieces in turn, each on from the last one's end ("Piece~climb": the Ramp tilted so)
    public static readonly (string Name, string Tip, string[] Pieces)[] Starters =
    {
        ("Oval", "Two straights and two hairpins: a short lap on the ground to change as you like",
            new[] { "Long straight", "Hairpin right", "Long straight", "Hairpin right" }),
        ("Oval with jumps", "An oval whose straights each leap a gap",
            new[] { "Jump", "Hairpin right", "Jump", "Hairpin right" }),
        ("Oval with loops", "An oval with a vertical loop on each straight",
            new[] { "Loop", "Hairpin right", "Loop", "Hairpin right" }),
        ("Spiral climb", "Up a spiral, along a raised road and down another spiral: a lap that passes over itself",
            new[] { "Long straight", "Spiral up right", "Long straight", "Long straight", "Right 90°", "Long straight", "Long straight", "Spiral down right", "Long straight", "Long straight", "Right 90°", "Long straight" }),
    };

    // A starter's pieces from (x, z) at height y (its first piece's start), its way `heading`: each one on from the last one's end
    public static List<TrackPiece> Starter(string name, double x, double z, double y, double heading)
    {
        var pieces = new List<TrackPiece>();
        var starter = Starters.First(s => s.Name == name);
        (double X, double Z, double Y, double Heading) at = (x, z, y, heading);
        foreach (var item in starter.Pieces)
        {
            var parts = item.Split('~');
            if (Section.Named(parts[0], out var tilt) is not { } section) continue;
            if (parts.Length > 1) tilt = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            var piece = new TrackPiece(section, at.X, at.Z, at.Y, at.Heading, tilt);
            pieces.Add(piece);
            at = piece.End;
        }
        return pieces;
    }

    // The pieces (four at most, and a Ramp first when the ends are at different heights) that take the road's end round to its start, ending
    // near it heading its way; null and why not when none do -- or it's a lap already, or there's no road
    public List<TrackPiece>? CloseLap(out string why)
    {
        var chain = Chain(out var closed, out _, out var order);
        if (chain is null || order.Count == 0) { why = "Lay some road first."; return null; }
        if (closed) { why = "The road is a lap already."; return null; }
        var end = order[^1].End; var start = order[0];
        var rise = start.Y - end.Y;
        if (Math.Abs(rise) > TrackPiece.TiltMost * 2) { why = $"The road's end is {Math.Abs(rise):0} {(rise < 0 ? "above" : "below")} its start: more than two Ramps can make up -- lay a spiral to bring it {(rise < 0 ? "down" : "up")} first."; return null; }
        var flat = new[] { "Straight", "Long straight", "Left 45°", "Right 45°", "Left 90°", "Right 90°", "Hairpin left", "Hairpin right" }
            .Select(n => Section.Named(n, out _)!).ToArray();
        static double Turned(double a, double b) => Math.Abs(Math.IEEERemainder(a - b, 2 * Math.PI));
        List<TrackPiece>? best = null; var bestScore = double.MaxValue;
        // (the climb or drop first: one Ramp, or two, tilted to make up the difference)
        var lead = new List<TrackPiece>();
        var at = end;
        if (Math.Abs(rise) >= 200)
        {
            var ramps = Math.Abs(rise) > TrackPiece.TiltMost ? 2 : 1;
            for (var r = 0; r < ramps; r++)
            {
                var ramp = new TrackPiece(Section.Ramp, at.X, at.Z, at.Y, at.Heading, rise / ramps);
                lead.Add(ramp); at = ramp.End;
            }
        }
        void Search(List<TrackPiece> so, (double X, double Z, double Y, double Heading) from, int depth)
        {
            var gap = Gap((from.X, from.Z, from.Y), (start.X, start.Z, start.Y));
            var turn = Turned(from.Heading, start.Heading);
            if (so.Count > 0 && gap < CloseCells / 2 && turn < 20 * Math.PI / 180)
            {
                var score = gap + turn * 10 + so.Count * 2;
                if (score < bestScore) { bestScore = score; best = so.ToList(); }
            }
            if (depth == 0 || gap > 4 * 30 + 10) return;
            foreach (var s in flat)
            {
                var piece = new TrackPiece(s, from.X, from.Z, from.Y, from.Heading);
                so.Add(piece);
                Search(so, piece.End, depth - 1);
                so.RemoveAt(so.Count - 1);
            }
        }
        Search(new List<TrackPiece>(), at, 4);
        if (best is null) { why = "No four pieces bring the end round to its start from here: lay a few towards it first (the yellow rings), then try again."; return null; }
        why = "";
        return lead.Concat(best).ToList();
    }

    // ---- the check as the pieces are laid (the user, 2026-11-04: make track making "more accessible and user friendly"): what the track
    // is and what stops it, worked out from the pieces alone -- no tracing, quick enough for every change -- each with where it is (island
    // cells), to be marked in the view. Ok: good as it is; Note: worth knowing, nothing wrong; Problem: the track can't be made so.
    public enum CheckKind { Ok, Note, Problem }
    public sealed record CheckItem(CheckKind Kind, string Text, List<(double X, double Z, double Y)> Spots);

    public List<CheckItem> Check()
    {
        var items = new List<CheckItem>();
        CheckItem Item(CheckKind kind, string text, List<(double X, double Z, double Y)>? spots = null) { var i = new CheckItem(kind, text, spots ?? new()); items.Add(i); return i; }
        var roads = Pieces.Where(p => !p.IsPit).ToList();
        if (roads.Count == 0)
        {
            Item(CheckKind.Problem, Pieces.Count == 0 ? "No pieces yet: choose a piece and click on the island." : "Only a pit lane: lay some road first.");
            return items;
        }
        var line = Chain(out var closed, out var left, out var order) ?? new();
        double length = 0;
        for (var k = 1; k < line.Count; k++) length += Math.Sqrt(Math.Pow(line[k].X - line[k - 1].X, 2) + Math.Pow(line[k].Z - line[k - 1].Z, 2));
        // (pieces left out of the road: each one's middle)
        if (left > 0)
        {
            var apart = roads.Where(r => !order.Contains(r)).Select(r => { var l = r.Line(); var m = l[l.Count / 2]; return (m.X, m.Z, m.Y); }).ToList();
            Item(CheckKind.Problem, $"{left} piece{(left > 1 ? "s aren't" : " isn't")} joined to the road (marked in red): join {(left > 1 ? "them" : "it")} on, or delete {(left > 1 ? "them" : "it")}.", apart);
        }
        else Item(CheckKind.Ok, $"{order.Count} piece{(order.Count > 1 ? "s" : "")}, one road.");
        // (a lap, or a sprint -- and a sprint whose ends nearly meet, but at different heights)
        if (closed) Item(CheckKind.Ok, "A lap: the road comes back round to its start.");
        else
        {
            var s = order[0]; var e = order[^1].End;
            var across = Gap((e.X, e.Z, e.Y), (s.X, s.Z, s.Y));
            if (order.Count > 1 && across < CloseCells && Math.Abs(e.Y - s.Y) >= SnapHeight)
                Item(CheckKind.Problem, $"The road's end is over its start but {Math.Abs(e.Y - s.Y):0} {(e.Y > s.Y ? "higher" : "lower")}: tilt a Ramp the other way for a lap.", new() { (e.X, e.Z, e.Y), (s.X, s.Z, s.Y) });
            else Item(CheckKind.Note, "A sprint: its two ends (the yellow rings) don't meet. Join them for a lap, or race it as it is.");
        }
        if (length < ShortestTry) Item(CheckKind.Problem, $"Only {length:0} cells of road: {ShortestTry:0} at least to race (a start, room to get going, a finish).");
        else Item(CheckKind.Ok, $"{length:0} cells of road.");
        // (where the road passes over or across itself: room for a car between the decks, or a crossroads the build jumps)
        const double deckRoom = 1600, apartAlong = 30;
        var cumulative = new double[line.Count];
        for (var k = 1; k < line.Count; k++) cumulative[k] = cumulative[k - 1] + Math.Sqrt(Math.Pow(line[k].X - line[k - 1].X, 2) + Math.Pow(line[k].Z - line[k - 1].Z, 2));
        var tight = new List<(double X, double Z, double Y)>();
        var crossroads = new List<(double X, double Z, double Y)>();
        var overs = 0;
        bool Near(List<(double X, double Z, double Y)> spots, double x, double z) => spots.Any(p => (p.X - x) * (p.X - x) + (p.Z - z) * (p.Z - z) < 100);
        for (var i = 0; i < line.Count; i += 2)
            for (var j = i + 2; j < line.Count; j += 2)
            {
                var along = cumulative[j] - cumulative[i];
                if (along < apartAlong || (closed && length - along < apartAlong)) continue;
                double dx = line[i].X - line[j].X, dz = line[i].Z - line[j].Z;
                if (dx * dx + dz * dz > 9) continue;
                var gap = Math.Abs(line[i].Y - line[j].Y);
                var spot = ((line[i].X + line[j].X) / 2, (line[i].Z + line[j].Z) / 2, Math.Max(line[i].Y, line[j].Y));
                if (gap < 250) { if (!Near(crossroads, spot.Item1, spot.Item2)) crossroads.Add(spot); }
                else if (gap < deckRoom) { if (!Near(tight, spot.Item1, spot.Item2)) tight.Add(spot); }
                else overs++;
            }
        if (tight.Count > 0)
            Item(CheckKind.Problem, $"Where the road passes over itself ({tight.Count} place{(tight.Count > 1 ? "s" : "")}, marked in red) there's no room for a car between the decks: {deckRoom:0} apart at least (PgUp raises a piece by 250).", tight);
        else if (overs > 0) Item(CheckKind.Ok, "It passes over itself with room for a car underneath.");
        if (crossroads.Count > 0)
            Item(CheckKind.Note, $"The road crosses itself at the same height ({crossroads.Count} place{(crossroads.Count > 1 ? "s" : "")}): the build puts a jump there.", crossroads);
        // (the pit lane: optional)
        if (Pieces.Any(p => p.IsPit)) Item(CheckKind.Ok, "A pit lane.");
        else Item(CheckKind.Note, "No pit lane (it's optional: lay one beside a straight on the ground).");
        return items;
    }
}
