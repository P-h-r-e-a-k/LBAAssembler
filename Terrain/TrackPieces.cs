using Section = LBAAssembler.Terrain.TrackSection;

namespace LBAAssembler.Terrain;

// Track pieces laid on the island in the editor's 3D view (MainWindow.TrackPieces; the user, 2026-11-03: "allowing a user to pick an island
// in our main viewport and by selecting pre-drawn track pieces drag and drop them. This should help with making vertical tracks as this
// viewport is fully turnable and true to size"): the race track generator's sections (TrackGeneratorWindow.Section) placed where they begin
// (island cells), how high (world units) and their way (radians: 0 along +x, + turning towards +z, as the generator's map has it). A piece
// dropped near another's open end goes on from it, at its height; the pieces in one chain become a plan through the generator's own tracer
// (TrackSketchTracer), at the heights they were laid at -- one over another where they were stacked.
internal sealed record TrackPiece(Section Section, double X, double Z, double Y, double Heading)
{
    // Its middle line, a point every half cell: island cells across, world units up, each point's kind
    public List<(double X, double Z, double Y, SketchKind Kind)> Line()
    {
        var line = new List<(double, double, double, SketchKind)> { (X, Z, Y, Section.Parts[0].Kind) };
        double x = X, z = Z, y = Y, h = Heading;
        foreach (var part in Section.Parts)
        {
            var n = Math.Max(2, (int)Math.Ceiling(part.Length * 2));
            for (var i = 1; i <= n; i++)
            {
                var ds = part.Length / n;
                if (part.Radius > 0) h += part.Turn / n;
                // (an arc: each small step along the way turned so far, half of this step's turn in it -- the generator's Strokes)
                var mid = part.Radius > 0 ? h - part.Turn / n / 2 : h;
                x += Math.Cos(mid) * ds; z += Math.Sin(mid) * ds; y += part.Rise / n;
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

    // The road pieces end to start in one chain: their middle line in order (cells, world units up, kinds), and whether it comes back round
    // to its first; null when there are none. Pieces that aren't one chain -- a track still being laid, a piece put down apart -- give the
    // longest run of them that is (`left`: the road pieces left out), so a track can be tried before it is whole.
    public List<(double X, double Z, double Y, SketchKind Kind)>? Chain(out bool closed) => Chain(out closed, out _, out _);

    public List<(double X, double Z, double Y, SketchKind Kind)>? Chain(out bool closed, out int left, out List<TrackPiece> order)
    {
        closed = false; left = 0; order = new List<TrackPiece>();
        var roads = Pieces.Where(p => !p.IsPit).ToList();
        if (roads.Count == 0) return null;
        TrackPiece? Next(TrackPiece a, IEnumerable<TrackPiece> from) =>
            from.Where(b => !ReferenceEquals(a, b) && Joins((a.End.X, a.End.Z, a.End.Y), (b.X, b.Z, b.Y), JoinCells)).OrderBy(b => Gap((a.End.X, a.End.Z, a.End.Y), (b.X, b.Z, b.Y))).FirstOrDefault();
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
        foreach (var p in order.ToList())
            foreach (var q in p.Line())
                if (line.Count == 0 || Math.Abs(line[^1].X - q.X) + Math.Abs(line[^1].Z - q.Z) > 0.2) line.Add(q);
        return line;
    }

    // The plan the pieces make on `island` (the island as its track is built on it: IslandScaler.AsBuilt), or why not (the result's Problems)
    // `from`: a test race's start (the 3D view's flag; the user, 2026-11-03: "drag and drop for where they want to start a test race from so
    // they can test individual segments") -- the road from the point nearest it, a sprint: to the road's end, or once round a lap
    public SketchResult MakePlan(IslandFile island, (double X, double Z, double Y)? from = null)
    {
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
        // (a sprint's start line: RunUp cells along it where the road before it allows -- its grid stands on that road, its cars none of them
        // past the road's end in the air: the user, 2026-11-03, of a test from a flag on a raised road, "That opponent is floating in mid-air")
        double LengthTo(List<(double X, double Z, double Y, SketchKind Kind)> c, int upTo)
        {
            double s = 0;
            for (var i = 1; i <= upTo && i < c.Count; i++) s += Math.Sqrt(Math.Pow(c[i].X - c[i - 1].X, 2) + Math.Pow(c[i].Z - c[i - 1].Z, 2));
            return s;
        }
        int? openStart = null;
        if (from is { } f)
        {
            var k = Enumerable.Range(0, chain.Count).MinBy(i => Math.Pow(chain[i].X - f.X, 2) + Math.Pow(chain[i].Z - f.Z, 2) + Math.Pow((chain[i].Y - f.Y) / 512, 2));
            // (from as far back as RunUp cells before the flag -- round a lap, or to an open road's beginning -- the start line on the flag)
            var back = k;
            double behind = 0;
            for (var steps = 0; steps < chain.Count - 1 && behind < RunUp; steps++)
            {
                var prev = closed ? (back - 1 + chain.Count) % chain.Count : back - 1;
                if (prev < 0) break;
                behind += Math.Sqrt(Math.Pow(chain[back].X - chain[prev].X, 2) + Math.Pow(chain[back].Z - chain[prev].Z, 2));
                back = prev;
            }
            chain = closed ? chain.Skip(back).Concat(chain.Take(back)).ToList() : chain.Skip(back).ToList();
            openStart = (int)Math.Round(behind);
            notes.Add(closed ? "tried from the flag: once round the lap from it, as a sprint" : "tried from the flag to the road's end, as a sprint");
            closed = false;
            if (chain.Count < 2 || LengthTo(chain, chain.Count - 1) - behind < 14)
            {
                problems.Add("The flag is at the road's very end: put it further back.");
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
        var first = TrackSketchTracer.Trace(sketch, island, false, given, closed, rise, shortest: ShortestTry, openStart: openStart);
        if (first.Route.Count < 10 || !chain.Any(p => p.Y > Ground(p.X, p.Z) + 500)) { first.Notes.InsertRange(0, notes); return first; }
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
        var second = TrackSketchTracer.Trace(sketch, island, false, given, closed, rise, profile, route.ToList(), ShortestTry, openStart);
        second.Notes.InsertRange(0, notes);
        return second;
    }
}
