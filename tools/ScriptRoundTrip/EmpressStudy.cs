using LBAAssembler;
using LBAAssembler.Terrain.Palace;

namespace ScriptRoundTrip;

// animstudy <game folder> <body> <anim>...: a body's bones as it stands (pivot, how far its points reach) and each animation's keyframes
// (time, each bone's type and values: degrees, or a translation's units).
internal static class AnimStudy
{
    public static int Run(string[] args)
    {
        var dir = args[1];
        var p = PalaceTorturers.Parts.Read(HqrArchive.Open(Path.Combine(dir, "BODY.HQR")).Read(int.Parse(args[2])));
        var world = p.World();
        Console.WriteLine($"body {args[2]}: {p.Bones.Count} bones, {p.Points.Count} points; x {world.Min(v => v.X)}..{world.Max(v => v.X)} y {world.Min(v => v.Y)}..{world.Max(v => v.Y)} z {world.Min(v => v.Z)}..{world.Max(v => v.Z)}");
        for (var b = 0; b < p.Bones.Count; b++)
        {
            var start = p.StartOf(b);
            var pts = world.Skip(start).Take(p.Bones[b].Count).ToList();
            var pivot = b == 0 ? System.Numerics.Vector3.Zero : world[p.Bones[b].Pivot];
            Console.WriteLine($"  bone {b}: parent {p.Bones[b].Parent} pivot {pivot} points {pts.Count}" + (pts.Count > 0 ? $" y {pts.Min(v => v.Y):0}..{pts.Max(v => v.Y):0} centre ({pts.Average(v => v.X):0},{pts.Average(v => v.Y):0},{pts.Average(v => v.Z):0})" : ""));
        }
        foreach (var a in args.Skip(3))
        {
            var bytes = HqrArchive.Open(Path.Combine(dir, "ANIM.HQR")).Read(int.Parse(a));
            var frames = PalaceTorturers.ReadFrames(bytes, 64);
            int have = BitConverter.ToUInt16(bytes, 2), loop = BitConverter.ToUInt16(bytes, 4);
            Console.WriteLine($"anim {a}: {frames.Count} frames, {have} bones, loop {loop}");
            foreach (var (time, bones) in frames.Take(4))
                Console.WriteLine($"  t {time}: " + string.Join(" | ", bones.Take(have).Select((v, i) => v.Type == 1 ? $"{i}T {v.A:0},{v.B:0},{v.C:0}" : $"{i} {v.A * 180 / Math.PI:0},{v.B * 180 / Math.PI:0},{v.C * 180 / Math.PI:0}")));
        }
        return 0;
    }
}
