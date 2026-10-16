using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// A tunnel over a stretch of the raised road (RaceTrackPlan.Tunnels): Otringal's over the bridge to its square island (the user, 2026-10-08:
// "the red is intended to be a tunnel to drive through"). From and To are the plan's points, Roof its roof's height over the deck.
internal sealed class TunnelRun
{
    public int From { get; set; }
    public int To { get; set; }
    public double Roof { get; set; } = 2800;
    // Pieces: false for a stretch that is under a roof of the island's own -- only the camera kept under it (Otringal's bridge, a tube the
    // road is cut through: RaceTrackDriveThrough).
    public bool Pieces { get; set; } = true;
    // Sunk: the road in a cutting under the ground, the tunnel its roof -- the ground cut down only between its walls, straight down beside
    // them (RaceTrackBuilder.CutUnderRaised leaves the stretch alone), and the roof's top never over Top (the ground's level: a road that
    // climbs under a level roof) and reaching Eave cells past the walls over the cut's edges -- Otringal's climb from the bridge under the
    // gatehouse up into the town (2026-10-08).
    public bool Sunk { get; set; }
    public double? Top { get; set; }
    public double Eave { get; set; }
    // UnderGround: the roof's top kept this far under the island's ground over the tunnel, where the road is under the ground -- a tall
    // tunnel down under a plateau whose edge it passes under (Island CX's ramps from its doors, 2026-10-16)
    public double? UnderGround { get; set; }
    // CloseAbove: at an end of the tunnel that no other tunnel goes on from, a wall across it from this height (island units) up to its
    // roof -- a tunnel that comes out under a roof of the island's own, through which it passes: the rock over that roof isn't drawn
    // (Island CX's ramps, out into its hall under the hall's roof)
    public double? CloseAbove { get; set; }
    // Ceiling: for the camera alone (RACEMOD.CPP tunnel='s fourth number), a height its eye is kept under -- a stretch under a level roof of
    // the island's own whose height over the road changes (Island CX's ramps in its hall, under the hall's roof)
    public double? Ceiling { get; set; }
}

// A place in the lava that shoots balls of fire onto a stretch of the lap (RaceTrackPlan.LavaBalls, RACEMOD.CPP lavaball=): Source in the
// plan's cells (and, a third number, its height: one of Volcano Island's bowls of lava, its brim -- else the ground's there), From and To
// the plan's points, about one every Every ms.
internal sealed class LavaBallRun
{
    public double[] Source { get; set; } = System.Array.Empty<double>();
    public int From { get; set; }
    public int To { get; set; }
    public int Every { get; set; } = 5000;
    // (a lava geyser's, RaceTrackPlan.LavaGeysers: the balls of each shower)
    public int Balls { get; set; }
}

// The tunnel's pieces, every Piece cells along the road, a decor each: a wall just outside each rail, from under the deck up to the roof,
// a light strip along its inside, and the roof over the road from wall to wall -- each a box with its faces outwards (the walls' inner
// faces and the roof's underside are what the car sees from inside). Their boxes are ones nothing can touch (the raised road's rail keeps
// the car between the walls; the roof is over it). The race-track mode keeps the camera's eye under the roof from a little before the
// tunnel's mouth to its end (RACEMOD.CPP tunnel=: report.Tunnels, the raised road's points of its ends).
internal static class RaceTrackTunnel
{
    public const double Piece = 4, WallOut = 0.3, Wall = 280, Slab = 320, Under = 500, StripHigh = 900, Strip = 60;
    private const int NoBoxTop = -32000;
    // colours (unlit: each face its own): the walls' grey, the roof's lavender grey (the town's), the light strips' gold
    public const int WallLight = 58, WallDark = 53, WallTop = 56, RoofLight = 214, RoofDark = 210, RoofTop = 212, StripColour = 101;

    public static List<int> Place(IslandFile island, TrackRoad r, IReadOnlyList<(int From, int To, TunnelRun Run)> tunnels, RaceTrackOptions o, RaceTrackReport report)
    {
        var made = new List<int>();
        if (o.NewBodyBase < 0) { report.Notes.Add("WARNING: no place for the tunnels' bodies was prepared -- no tunnel"); return made; }
        foreach (var (from, to, run) in tunnels)
        {
            var n = r.Count;
            var span = ((to - from) % n + n) % n;
            // (the roof's height over the deck, its least: the camera's eye is kept under it)
            double RoofAt(int i)
            {
                var roof = run.Top is { } top && r.H[i] + run.Roof + Slab > top ? top - Slab : r.H[i] + run.Roof;
                if (run.UnderGround is { } under && GroundOver(island, r, i, o) is { } g && g > r.H[i]) roof = Math.Min(roof, g - under - Slab);
                return roof;
            }
            var least = run.Roof;
            for (var k = 0; k <= span; k++) { var i = (from + k) % n; least = Math.Min(least, RoofAt(i) - r.H[i]); }
            if (!run.Pieces)
            {
                report.Tunnels.Add((from, to, least, run.Ceiling));
                report.Notes.Add($"tunnel: {span * o.Spacing:0} cells from cell ({r.X[from]:0.0}, {r.Z[from]:0.0}) to ({r.X[to]:0.0}, {r.Z[to]:0.0}) under the island's own roof, {least:0} over the deck");
                continue;
            }
            var cut = run.Sunk ? CutSunk(island, r, from, span, o) : (0, 0.0);
            // (whether another tunnel of pieces goes on from either end)
            var startFree = !tunnels.Any(t => t.Run.Pieces && t.To == from && t.Run != run);
            var endFree = !tunnels.Any(t => t.Run.Pieces && t.From == to && t.Run != run);
            var step = Math.Max(2, (int)Math.Round(Piece / o.Spacing));
            int pieces = 0, skipped = 0;
            for (var k0 = 0; k0 < span; k0 += step)
            {
                var k1 = Math.Min(span, k0 + step);
                int a = (from + k0) % n, b = (from + k1) % n, mid = (from + (k0 + k1) / 2) % n;
                var origin = new Vector3((float)(r.X[mid] * 512), (float)r.H[mid], (float)(r.Z[mid] * 512));
                var mesh = new List<Vector3>(); var faces = new List<Face>();
                var lo = new Vector3(float.MaxValue); var hi = new Vector3(float.MinValue);
                for (var k = k0; k < k1; k++)
                {
                    int i = (from + k) % n, j = (from + k + 1) % n;
                    var p = new Vector3((float)(r.X[i] * 512), (float)r.H[i], (float)(r.Z[i] * 512)) - origin;
                    var q = new Vector3((float)(r.X[j] * 512), (float)r.H[j], (float)(r.Z[j] * 512)) - origin;
                    var along = new Vector3(q.X - p.X, 0, q.Z - p.Z);
                    if (along.Length() < 1e-3) continue;
                    var dir = Vector3.Normalize(along);
                    var across = new Vector3(-dir.Z, 0, dir.X);
                    var centre = (p + q) / 2;
                    var halfAlong = dir * (along.Length() / 2 + 12);   // (a little into the next: no crack)
                    var half = (float)((HalfAt(r, i, o) + WallOut) * 512);
                    var floor = centre.Y;
                    var roof = (float)((RoofAt(i) + RoofAt(j)) / 2 - (r.H[i] + r.H[j]) / 2 + floor);
                    foreach (var side in new[] { -1f, 1f })
                    {
                        // the wall: from under the deck to the roof's top, Wall thick, outside the rail
                        var wc = new Vector3(centre.X, 0, centre.Z) + across * side * (half + (float)Wall / 2);
                        RaceTrackPipes.Box(mesh, faces, wc, halfAlong, across * ((float)Wall / 2), floor - (float)Under, roof + (float)Slab, WallLight, WallDark, WallTop);
                        // its light strip, on its inside
                        var sc = new Vector3(centre.X, 0, centre.Z) + across * side * (half - (float)Strip / 2);
                        RaceTrackPipes.Box(mesh, faces, sc, halfAlong, across * ((float)Strip / 2), floor + (float)StripHigh, floor + (float)StripHigh + 90, StripColour, StripColour, StripColour);
                    }
                    // the roof: from wall to wall
                    var rc = new Vector3(centre.X, 0, centre.Z);
                    var eave = (float)(run.Eave * 512);
                    RaceTrackPipes.Box(mesh, faces, rc, halfAlong, across * (half + (float)Wall + eave), roof, roof + (float)Slab, RoofLight, RoofDark, RoofTop);
                    // (a sunk tunnel's end where the road goes on under the island, `under`: a wall over the next tunnel's mouth, from its roof
                    // -- this one's Roof over the deck -- up to this one's top: the ground's face over the mouth is left undrawn
                    // (RaceTrackBuilder.OpenTunnelMouths) and the gap between the two roofs showed the sky -- Island CX's doors, 2026-10-15)
                    if (run.Sunk)
                        foreach (var (isEnd, at, sign) in new[] { (k == span - 1 && r.Under?[j] == true, q, 1f), (k == 0 && r.Under?[i] == true, p, -1f) })
                        {
                            if (!isEnd) continue;
                            var hc = new Vector3(at.X, 0, at.Z) - dir * sign * ((float)Wall / 2);
                            var bottom = at.Y + (float)run.Roof;
                            // (up to the ground behind it where that is higher than this tunnel's top: Island CX's plateau stands 320 over
                            // door A's portal, and its edge showed over the portal's roof, floating)
                            var headTop = roof + (float)Slab;
                            var ex = (r.X[at == q ? j : i]); var ez = (r.Z[at == q ? j : i]);
                            var reachCells = HalfAt(r, at == q ? j : i, o) + WallOut + Wall / 512 + run.Eave + 1;
                            for (var gz = (int)Math.Floor(ez - reachCells); gz <= (int)Math.Ceiling(ez + reachCells); gz++)
                                for (var gx = (int)Math.Floor(ex - reachCells); gx <= (int)Math.Ceiling(ex + reachCells); gx++)
                                    if ((gx - ex) * (gx - ex) + (gz - ez) * (gz - ez) <= reachCells * reachCells && island.HeightAt(gx, gz) is { } gh)
                                        headTop = Math.Max(headTop, gh - origin.Y + 20);
                            if (headTop > bottom + 50)
                                RaceTrackPipes.Box(mesh, faces, hc, dir * ((float)Wall / 2 + 12), across * (half + (float)Wall + eave), bottom, headTop, WallLight, WallDark, WallTop);
                        }
                    // (an end no other tunnel goes on from, CloseAbove: a wall across it from that height up to the roof's top)
                    if (run.CloseAbove is { } above)
                        foreach (var (isEnd, at, sign) in new[] { (k == span - 1 && endFree, q, 1f), (k == 0 && startFree, p, -1f) })
                        {
                            var bottom = (float)(above - origin.Y);
                            if (!isEnd || roof + (float)Slab < bottom + 50) continue;
                            var cc = new Vector3(at.X, 0, at.Z) - dir * sign * ((float)Wall / 2);
                            RaceTrackPipes.Box(mesh, faces, cc, dir * ((float)Wall / 2 + 12), across * (half + (float)Wall + eave), bottom, roof + (float)Slab, WallLight, WallDark, WallTop);
                        }
                    foreach (var v in new[] { p, q })
                        foreach (var side in new[] { -1f, 1f })
                        {
                            var e = v + across * side * (half + (float)Wall + eave);
                            lo = Vector3.Min(lo, e - new Vector3(0, (float)Under, 0)); hi = Vector3.Max(hi, new Vector3(e.X, roof + (float)Slab, e.Z));
                        }
                }
                if (faces.Count == 0) continue;
                if (IslandDecors.Locate(island, origin.X, origin.Z) is not { } where || where.Cube.Decors.Count >= IslandDecors.MaxPerCubeRaceTrack) { skipped++; continue; }
                var (cube, lx, lz) = where;
                var d = IslandDecors.Blank(o.NewBodyBase + report.NewBodies.Count, lx, (int)Math.Round(origin.Y), lz, 0);
                // (drawn close to the camera, BrickBuilding.CloseFlag: the camera rides inside a tunnel, and the near plane cut away the
                // wall beside it and the roof over it -- the ground outside showed through, Island CX's tall ramps, 2026-10-16)
                var body = RaceTrackPipes.Write(mesh, faces, lit: false);
                System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(body, System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(body) | LBAAssembler.Terrain.Palace.BrickBuilding.CloseFlag);
                report.NewBodies.Add(body);
                d.XMin = (int)Math.Floor(lx + lo.X); d.XMax = (int)Math.Ceiling(lx + hi.X);
                // (a box nothing can touch, its top far under its bottom -- as the raised road's pieces have: a box round the walls and the roof
                // takes in the road between them, and the engine stopped the car dead at the tunnel's mouth; its bottom corners, where the
                // piece is, are what decides whether it is drawn)
                d.YMin = (int)Math.Floor(origin.Y + lo.Y); d.YMax = NoBoxTop;
                d.ZMin = (int)Math.Floor(lz + lo.Z); d.ZMax = (int)Math.Ceiling(lz + hi.Z);
                cube.Decors.Add(d);
                made.Add(d.Body);
                pieces++;
            }
            report.Tunnels.Add((from, to, least, run.Ceiling));
            report.Notes.Add($"tunnel: {span * o.Spacing:0} cells from cell ({r.X[from]:0.0}, {r.Z[from]:0.0}) to ({r.X[to]:0.0}, {r.Z[to]:0.0}), its roof {run.Roof:0} over the deck" +
                             (run.Top is { } t ? $" (its top no higher than {t:0}: {least:0} over the deck at the least)" : "") + $", {pieces} pieces{(skipped > 0 ? $" ({skipped} left out: a cube full)" : "")}" +
                             (run.Sunk ? $"; sunk: the ground cut down between its walls at {cut.Item1} vertices, {cut.Item2:0} at the most" : ""));
        }
        return made;
    }

    // A sunk tunnel's cutting: the ground between its walls (out to their outer faces) brought down to RaisedUnder under the deck, and
    // nothing beyond them -- the ground there stays the town's, its edge straight down beside the walls, under the roof's eaves.
    public const double RaisedUnder = 450;
    private static (int, double) CutSunk(IslandFile island, TrackRoad r, int from, int span, RaceTrackOptions o)
    {
        var n = r.Count;
        int count = 0; double deepest = 0;
        var lowest = new Dictionary<(int, int), double>();
        for (var k = 0; k < span; k++)
        {
            int a = (from + k) % n, b = (from + k + 1) % n;
            double sx = r.X[b] - r.X[a], sz = r.Z[b] - r.Z[a], len2 = sx * sx + sz * sz;
            if (len2 < 1e-12) continue;
            var reach = HalfAt(r, a, o) + WallOut + Wall / 512 + 0.05;
            for (var gz = (int)Math.Floor(Math.Min(r.Z[a], r.Z[b]) - reach); gz <= (int)Math.Ceiling(Math.Max(r.Z[a], r.Z[b]) + reach); gz++)
            for (var gx = (int)Math.Floor(Math.Min(r.X[a], r.X[b]) - reach); gx <= (int)Math.Ceiling(Math.Max(r.X[a], r.X[b]) + reach); gx++)
            {
                var t = ((gx - r.X[a]) * sx + (gz - r.Z[a]) * sz) / len2;
                if (t < -0.01 && k > 0 || t > 1.01 && k < span - 1) continue;
                // (nor past an end where the road goes on under the island, `under`: the ground there is over it -- Island CX's door A's
                // portal cut the plateau behind it 4 cells deep, and the road went into flat ground before the cliff, 2026-10-15)
                if (t < -0.01 && r.Under?[a] == true || t > 1.01 && r.Under?[b] == true) continue;
                t = Math.Clamp(t, 0, 1);
                var d = Math.Sqrt((gx - r.X[a] - sx * t) * (gx - r.X[a] - sx * t) + (gz - r.Z[a] - sz * t) * (gz - r.Z[a] - sz * t));
                if (d > reach) continue;
                var to = r.H[a] + (r.H[b] - r.H[a]) * t - RaisedUnder;
                lowest[(gx, gz)] = lowest.TryGetValue((gx, gz), out var was) ? Math.Min(was, to) : to;
            }
        }
        foreach (var ((gx, gz), to) in lowest)
        {
            if (island.HeightAt(gx, gz) is not { } h || h <= to) continue;
            deepest = Math.Max(deepest, h - to);
            island.SetHeight(gx, gz, Math.Max(0, (int)Math.Floor(to)));
            count++;
        }
        return (count, deepest);
    }

    private static double HalfAt(TrackRoad r, int i, RaceTrackOptions o) =>
        r.Raised is { } up && up[i] ? r.RaisedHalfs is { } halfs && i < halfs.Length ? halfs[i] : o.RaisedHalfWidth : r.CurbHalf;

    // The island's ground over a tunnel at point i, its least across the tunnel from wall to wall (null: none there).
    private static double? GroundOver(IslandFile island, TrackRoad r, int i, RaceTrackOptions o)
    {
        var reach = HalfAt(r, i, o) + WallOut + Wall / 512;
        double? least = null;
        for (var s = -1.0; s <= 1.0001; s += 0.25)
            if (IslandOps.Altitude(island, (r.X[i] - r.Tz[i] * reach * s) * 512, (r.Z[i] + r.Tx[i] * reach * s) * 512) is { } g)
                least = least is { } l ? Math.Min(l, g) : g;
        return least;
    }
}
