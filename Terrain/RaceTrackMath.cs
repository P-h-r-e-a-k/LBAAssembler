namespace LBAAssembler.Terrain;

// Small sums the race track's files share.
internal static class RaceTrackMath
{
    // A direction in radians as the engine's turn (0..4095, 4096 a full turn), rounded from radians / 2π * 4096 ...
    public static int Turn(double radians) => (int)Math.Round(radians / (2 * Math.PI) * 4096) & 4095;

    // ... and from radians * 4096 / 2π, the order some places have it in (it can round the other way at a half: the two are kept apart)
    public static int TurnOf(double radians) => (int)Math.Round(radians * 4096 / (2 * Math.PI)) & 4095;

    // A road's half width at point i, to the edge that holds a car: a raised stretch's own (its width there, else the plan's), else its curbs'.
    public static double HalfAt(this TrackRoad r, int i, RaceTrackOptions o) =>
        r.Raised is { } up && up[i] ? r.RaisedHalfs is { } halfs && i < halfs.Length ? halfs[i] : o.RaisedHalfWidth : r.CurbHalf;
}
