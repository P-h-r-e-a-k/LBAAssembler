namespace LBAAssembler.Terrain;

// The race track generator's track pieces (TrackGeneratorWindow; the 3D view's, TrackPieceSet): each a run of parts, straights and arcs of
// a kind, climbing or dropping as they go.
// A section's parts in turn: a straight (length, cells) or an arc (radius, cells, and its turn, radians: + to the right), each of a kind
// (and how much it climbs over its length, world units: a ramp, a spiral -- the user, 2026-10-27: "make sure the track builder allows
// for slopes, and upward spirals, along with other over lapping sections of track")
internal sealed record TrackPart(SketchKind Kind, double Length, double Radius = 0, double Turn = 0, double Rise = 0);

internal sealed record TrackSection(string Name, string Tip, TrackPart[] Parts)
{
    public SketchKind Kind => Parts.Any(p => p.Kind == SketchKind.Pit) ? SketchKind.Pit : SketchKind.Road;
    public double Turn => Parts.Sum(p => p.Turn);
    public double Rise => Parts.Sum(p => p.Rise);
    // The same piece of road driven the other way: its parts in the other order, each turning and climbing the other way (a left hairpin
    // driven backwards is a right one, a ramp up a ramp down)
    public TrackSection Reversed() => this with
    {
        Name = Name.EndsWith(" (reversed)") ? Name[..^11] : Name + " (reversed)",
        Parts = Parts.Reverse().Select(p => p with { Turn = -p.Turn, Rise = -p.Rise }).ToArray(),
    };

    public static readonly TrackSection[] All =
    {
        new("Straight", "10 cells of road", new[] { new TrackPart(SketchKind.Road, 10) }),
        new("Long straight", "30 cells of road", new[] { new TrackPart(SketchKind.Road, 30) }),
        new("Left 45°", "A bend to the left, 16 cells round", new[] { Arc(16, -45) }),
        new("Right 45°", "A bend to the right, 16 cells round", new[] { Arc(16, 45) }),
        new("Left 90°", "A bend to the left, 16 cells round", new[] { Arc(16, -90) }),
        new("Right 90°", "A bend to the right, 16 cells round", new[] { Arc(16, 90) }),
        new("Hairpin left", "Round to the left, 12 cells round", new[] { Arc(12, -180) }),
        new("Hairpin right", "Round to the right, 12 cells round", new[] { Arc(12, 180) }),
        new("Jump", "Road, a jump over 18 cells, road", new[] { new TrackPart(SketchKind.Road, 12), new TrackPart(SketchKind.Jump, 18), new TrackPart(SketchKind.Road, 14) }),
        new("Loop", "Road, a vertical loop, road", new[] { new TrackPart(SketchKind.Road, 16), new TrackPart(SketchKind.Loop, 6), new TrackPart(SketchKind.Road, 16) }),
        new("Pit lane", "40 cells of pit lane: place it beside a straight", new[] { new TrackPart(SketchKind.Pit, 40) }),
        // climbs and drops: the track becomes a raised road on piers (the Elevator Platform's, Volcano Island's)
        new("Ramp up", "Climbs 1500 over 30 cells (10 %): the track becomes a raised road", new[] { new TrackPart(SketchKind.Road, 30, Rise: 1500) }),
        new("Ramp down", "Drops 1500 over 30 cells", new[] { new TrackPart(SketchKind.Road, 30, Rise: -1500) }),
        new("Steep up", "Climbs 2000 over 20 cells (20 %)", new[] { new TrackPart(SketchKind.Road, 20, Rise: 2000) }),
        new("Steep down", "Drops 2000 over 20 cells", new[] { new TrackPart(SketchKind.Road, 20, Rise: -2000) }),
        new("Spiral up left", "A turn and a quarter to the left, 12 cells round, climbing 4000: it passes over itself", new[] { Arc(12, -450, 4000) }),
        new("Spiral up right", "A turn and a quarter to the right, 12 cells round, climbing 4000", new[] { Arc(12, 450, 4000) }),
        new("Spiral down left", "A turn and a quarter to the left, dropping 4000", new[] { Arc(12, -450, -4000) }),
        new("Spiral down right", "A turn and a quarter to the right, dropping 4000", new[] { Arc(12, 450, -4000) }),
    };

    private static TrackPart Arc(double radius, double degrees, double rise = 0) => new(SketchKind.Road, Math.Abs(degrees) * Math.PI / 180 * radius, radius, degrees * Math.PI / 180, rise);
}
