using System.Numerics;
using LBAAssembler.Terrain;
using LBAAssembler.Terrain.Harvest;
using LBAAssembler.Terrain.Snow;

namespace ScriptRoundTrip;

// harvestclear [cells]: every thing the Long Table's build places, face by face, against the road: what stands inside the road's width (and `cells` more, 0.75 by default,
// the rails) from just above its surface to Clearance over it, the room the car and the camera riding behind it need. The check a thing's middle can't give: the user, 2026-11-02,
// "there's a lot of stuff over the track". Reads docs/racetrack as they stand, no build needed.
internal static class HarvestStudy
{
    private const double Clearance = 3000, Cell = 512, Sample = 200;

    public static int Clear(string[] args)
    {
        var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "racetrack"));
        var margin = args.Length > 1 ? double.Parse(args[1]) : 0.75;
        using var stream = File.OpenRead(Path.Combine(folder, "harvest_track_plan.json"));
        var plan = RaceTrackPlan.Read(stream);
        var design = HarvestDesign.Load(folder);
        var palette = new byte[768];
        for (var i = 0; i < 256; i++) palette[i * 3] = palette[i * 3 + 1] = palette[i * 3 + 2] = (byte)i;
        HarvestObjects.Recording = new();
        HarvestObjects.Build(null, new SnowShading(palette, 45, 45), design);

        // the road's points, by the cell they lie in
        var road = new Dictionary<(int, int), List<(double X, double Z, double Y, double Half)>>();
        const int Bucket = 16;
        for (var k = 0; k < plan.Points.Length; k++)
        {
            var p = (plan.Points[k][0], plan.Points[k][1], plan.Heights![k], plan.RaisedHalfs![k]);
            var key = ((int)(p.Item1 / Bucket), (int)(p.Item2 / Bucket));
            if (!road.TryGetValue(key, out var list)) road[key] = list = new();
            list.Add(p);
        }
        int bad = 0, shown = 0;
        var byKind = new Dictionary<string, int>();
        foreach (var t in HarvestObjects.Recording)
        {
            var mesh = t.Mesh;
            double c = Math.Cos(t.Beta * Math.Tau / 4096), s = Math.Sin(t.Beta * Math.Tau / 4096);
            Vector3 World(Vector3 p) => new((float)(t.U * Cell + p.X * c + p.Z * s), (float)(t.Y + p.Y), (float)(t.V * Cell - p.X * s + p.Z * c));
            var hits = 0; double wx = 0, wz = 0, wy = 0, ry = 0;
            foreach (var face in mesh.Faces)
            {
                var q = face.Points.Select(i => World(mesh.Points[i])).ToArray();
                for (var a = 1; a + 1 < q.Length; a++)
                {
                    var (p0, p1, p2) = (q[0], q[a], q[a + 1]);
                    var n = (int)Math.Ceiling(Math.Max((p1 - p0).Length(), (p2 - p0).Length()) / Sample) + 1;
                    for (var i = 0; i <= n; i++)
                        for (var j = 0; i + j <= n; j++)
                        {
                            var p = p0 + (p1 - p0) * (i / (float)n) + (p2 - p0) * (j / (float)n);
                            double cx = p.X / Cell, cz = p.Z / Cell;
                            for (var dz = -1; dz <= 1; dz++)
                                for (var dx = -1; dx <= 1; dx++)
                                {
                                    if (!road.TryGetValue(((int)(cx / Bucket) + dx, (int)(cz / Bucket) + dz), out var list)) continue;
                                    foreach (var r in list)
                                    {
                                        if (p.Y < r.Y - 30 || p.Y > r.Y + Clearance) continue;
                                        var reach = r.Half + margin;
                                        var d2 = (r.X - cx) * (r.X - cx) + (r.Z - cz) * (r.Z - cz);
                                        if (d2 > reach * reach) continue;
                                        hits++; wx = cx; wz = cz; wy = p.Y; ry = r.Y;
                                    }
                                }
                        }
                }
            }
            if (hits == 0) continue;
            bad++;
            byKind[t.What] = byKind.GetValueOrDefault(t.What) + 1;
            if (shown++ < 80) Console.WriteLine($"{t.What} at ({t.U:0.0}, {t.V:0.0}) turn {t.Beta}: on the road near ({wx:0.0}, {wz:0.0}), {wy - ry:0} over it");
        }
        Console.WriteLine($"{HarvestObjects.Recording.Count} things, {bad} on the road" + (byKind.Count > 0 ? ": " + string.Join(", ", byKind.OrderByDescending(k => k.Value).Select(k => $"{k.Value} {k.Key}")) : ""));
        return bad == 0 ? 0 : 1;
    }
}
