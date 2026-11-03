using System.Buffers.Binary;
using LBAAssembler.Terrain.Palace;

namespace ScriptRoundTrip;

// bodypolys <BODY.HQR> <index> [bone...]: a body's points (as it stands) and polygons -- each its type, its points, its colour -- bone by
// bone (a polygon under the bone of its first point), for reshaping one of the game's bodies by hand.
internal static class BodyPolysCommand
{
    public static int Run(string[] args)
    {
        var p = PalaceTorturers.Parts.Read(LBAAssembler.HqrArchive.Open(args[1]).Read(int.Parse(args[2])));
        var only = args.Skip(3).Select(int.Parse).ToHashSet();
        var world = p.World();
        int BoneOf(int point) { var s = 0; for (var b = 0; b < p.Bones.Count; b++) { if (point < s + p.Bones[b].Count) return b; s += p.Bones[b].Count; } return -1; }
        for (var b = 0; b < p.Bones.Count; b++)
        {
            if (only.Count > 0 && !only.Contains(b)) continue;
            var start = p.StartOf(b);
            Console.WriteLine($"bone {b}: parent {p.Bones[b].Parent}, pivot {p.Bones[b].Pivot}, points {start}..{start + p.Bones[b].Count - 1}, face normals {p.Bones[b].FaceNormals}");
            for (var i = start; i < start + p.Bones[b].Count; i++) Console.WriteLine($"  p{i}: ({world[i].X:0}, {world[i].Y:0}, {world[i].Z:0})");
            foreach (var (type, records) in p.Blocks)
                foreach (var r in records)
                {
                    var n = (type & 0x8000) != 0 ? 4 : 3;
                    var pts = Enumerable.Range(0, n).Select(k => (int)BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(k * 2))).ToArray();
                    if (BoneOf(pts[0]) != b) continue;
                    Console.WriteLine($"  poly type {type:X4} [{string.Join(",", pts)}] colour {BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(8))} normal {BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(10))}");
                }
        }
        return 0;
    }
}
