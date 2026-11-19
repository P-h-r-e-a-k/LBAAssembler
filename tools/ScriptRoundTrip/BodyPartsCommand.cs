using System.Buffers.Binary;
using System.Numerics;
using LBAAssembler;
using LBAAssembler.Terrain.Palace;

namespace ScriptRoundTrip;

// bodyparts <game folder> <BODY.HQR entry>...: each body's bones (parent, pivot, points, the box of their points as the body stands) and
// its polygons by the bone of their first point, with their colours -- to tell a body's parts apart (Twinsen's protopack from his tunic).
internal static class BodyPartsCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("bodyparts <game folder> <BODY.HQR entry>..."); return 1; }
        var hqr = HqrArchive.Open(Path.Combine(args[1], "BODY.HQR"));
        foreach (var entry in args.Skip(2).Select(int.Parse))
        {
            var p = PalaceTorturers.Parts.Read(hqr.Read(entry));
            var world = p.World();
            Console.WriteLine($"BODY.HQR {entry}: {p.Bones.Count} bones, {p.Points.Count} points");
            var polyBone = new Dictionary<int, List<int>>();
            foreach (var (type, records) in p.Blocks)
            {
                var corners = (type & 0x8000) != 0 ? 4 : 3;
                foreach (var r in records)
                {
                    if (r.Length < corners * 2 + 2) continue;
                    var first = BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(0));
                    if (first >= p.Points.Count) continue;
                    var bone = p.Points[first].Bone;
                    // (the colour: the word after the corners)
                    var colour = r.Length >= corners * 2 + 2 ? BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(corners * 2)) & 0xFF : -1;
                    (polyBone.TryGetValue(bone, out var l) ? l : polyBone[bone] = new()).Add(colour);
                }
            }
            for (var b = 0; b < p.Bones.Count; b++)
            {
                var start = p.StartOf(b);
                var pts = Enumerable.Range(start, p.Bones[b].Count).Select(i => world[i]).ToList();
                var lo = pts.Count > 0 ? pts.Aggregate(Vector3.Min) : Vector3.Zero;
                var hi = pts.Count > 0 ? pts.Aggregate(Vector3.Max) : Vector3.Zero;
                var polys = polyBone.TryGetValue(b, out var c) ? c : new();
                var colours = string.Join(" ", polys.GroupBy(x => x).OrderByDescending(g => g.Count()).Take(5).Select(g => $"{g.Key}x{g.Count()}"));
                Console.WriteLine($"  bone {b}: parent {p.Bones[b].Parent}, pivot {p.Bones[b].Pivot}, {p.Bones[b].Count} points, box ({lo.X:0},{lo.Y:0},{lo.Z:0})..({hi.X:0},{hi.Y:0},{hi.Z:0}), {polys.Count} polygons: {colours}");
            }
            // (BODYPARTS_HOLES=<bone>: the edges of that bone's polygons only one polygon has -- its holes' and openings' rims -- chained
            // into loops, each point as the body stands)
            if (int.TryParse(Environment.GetEnvironmentVariable("BODYPARTS_HOLES"), out var holeBone))
            {
                var body = LbaBodyStudio.Body.Read(hqr.Read(entry), 2);
                var w = body.World();
                var owner = body.Bones[holeBone];
                bool Mine(int i) => i >= owner.Start && i < owner.Start + owner.Count;
                // (points at the same place one: the body's own seams between bones)
                var same = new Dictionary<int, int>();
                for (var i = 0; i < w.Length; i++)
                    same[i] = Enumerable.Range(0, i + 1).First(j => Vector3.DistanceSquared(w[j], w[i]) < 4);
                var edges = new Dictionary<(int, int), int>();
                var directed = new List<(int A, int B)>();
                foreach (var face in body.Faces.Where(f => f.Points.Any(Mine)))
                    for (var c = 0; c < face.Points.Length; c++)
                    {
                        int a = same[face.Points[c]], b = same[face.Points[(c + 1) % face.Points.Length]];
                        var key = (Math.Min(a, b), Math.Max(a, b));
                        edges[key] = edges.GetValueOrDefault(key) + 1;
                        directed.Add((a, b));
                    }
                var open = directed.Where(d => edges[(Math.Min(d.A, d.B), Math.Max(d.A, d.B))] == 1).ToList();
                var next = new Dictionary<int, int>();
                foreach (var (a, b) in open) next[a] = b;
                var seen = new HashSet<int>();
                foreach (var (a, _) in open)
                {
                    if (seen.Contains(a)) continue;
                    var loop = new List<int>();
                    for (var q = a; !seen.Contains(q) && next.ContainsKey(q); q = next[q]) { seen.Add(q); loop.Add(q); }
                    Console.WriteLine($"    rim of {loop.Count}: " + string.Join(" ", loop.Select(i => $"{i}({w[i].X:0},{w[i].Y:0},{w[i].Z:0})")));
                }
            }
            // (BODYPARTS_FACES=<bone>: every polygon with a point of that bone -- its points, its colour, its points as the body stands)
            if (int.TryParse(Environment.GetEnvironmentVariable("BODYPARTS_FACES"), out var faceBone))
            {
                var body = LbaBodyStudio.Body.Read(hqr.Read(entry), 2);
                var w = body.World();
                var owner = body.Bones[faceBone];
                bool Mine(int i) => i >= owner.Start && i < owner.Start + owner.Count;
                for (var f = 0; f < body.Faces.Count; f++)
                {
                    var face = body.Faces[f];
                    if (!face.Points.Any(Mine)) continue;
                    Console.WriteLine($"    face {f}: colour {face.Colour} type {face.Lba2Type} material {face.Material}: " + string.Join(" ", face.Points.Select(i => $"{i}({w[i].X:0},{w[i].Y:0},{w[i].Z:0}){(Mine(i) ? "" : "*")}")));
                }
            }
        }
        return 0;
    }
}
