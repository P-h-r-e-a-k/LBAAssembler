namespace LBAAssembler.Terrain;

// Flying gas monsters over a stretch of the lap (RaceTrackPlan.Flyers; RACEMOD.CPP flyer=): Island CX's, in the hall under its gas -- the
// user, 2026-10-15: "Naturally the inside has two types of gas monster, one that flies around and shoots red fire balls, and the other one
// that you've already implemented". The game's own (entity 329: it rises out of the gas in Island CX's room under the emperor's, scene 181,
// and spits red fire balls at Twinsen), flown by the race-track mode to and fro along a way beside the road, over the hall's floor, stopping
// now and then to spit a fire ball at a car below it. One actor each.
internal sealed class FlyerRun
{
    public int From { get; set; }
    public int To { get; set; }
    // how far beside the road's middle it flies (cells; plus: the road's left, as it runs; minus: its right), how high over the floor,
    // and the floor's height
    public double Out { get; set; } = 5.5;
    public double Height { get; set; } = 2600;
    public double Floor { get; set; }
}

// A monster as placed: its way (island cells), flown to and fro, and its height.
internal sealed record FlyerSpot(IReadOnlyList<(double X, double Z)> Way, double Y);

internal static class RaceTrackFlyers
{
    public const int Entity = 329;
    // cells between its way's points
    private const double Step = 2;
    // what stands in its way: anything over the floor reaching up to within this of its height (the hall's pillars and walls, not the road's
    // decks under it)
    private const double Below = 1500, Above = 1500;

    public static FlyerSpot? Place(IslandFile island, TrackRoad r, int from, int to, FlyerRun run, RaceTrackOptions o)
    {
        var n = r.Count;
        var span = ((to - from) % n + n) % n + 1;
        var per = Math.Max(1, (int)Math.Round(Step / o.Spacing));
        var y = run.Floor + run.Height;
        var boxes = IslandOps.CubeCells(island).SelectMany(c => c.Item3.Decors.Select(d =>
            (X0: (c.Item1 * (double)IslandFile.CubeSize + d.XMin) / 512, Z0: (c.Item2 * (double)IslandFile.CubeSize + d.ZMin) / 512,
             X1: (c.Item1 * (double)IslandFile.CubeSize + d.XMax) / 512, Z1: (c.Item2 * (double)IslandFile.CubeSize + d.ZMax) / 512, Y0: (double)d.YMin, Y1: (double)d.YMax)))
            .Where(b => b.Y1 > b.Y0 && b.Y1 > y - Below && b.Y0 < y + Above).ToList();
        bool Clear(double x, double z) => !boxes.Any(b => x > b.X0 - 1 && x < b.X1 + 1 && z > b.Z0 - 1 && z < b.Z1 + 1);
        var way = new List<(double X, double Z)>();
        for (var j = 0; j < span; j += per)
        {
            var k = (from + j) % n;
            double x = r.X[k] - r.Tz[k] * run.Out, z = r.Z[k] + r.Tx[k] * run.Out;
            if (Clear(x, z)) way.Add((x, z));
        }
        return way.Count >= 2 ? new FlyerSpot(way, y) : null;
    }
}
