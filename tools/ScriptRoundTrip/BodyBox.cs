namespace ScriptRoundTrip;

// bodybox <BODY.HQR> <entry>...: each body's extent in its own frame (its points posed at rest: min and max x, y, z), its bones and points.
internal static class BodyBox
{
    public static int Run(string[] args)
    {
        var hqr = LBAAssembler.HqrArchive.Open(args[1]);
        foreach (var arg in args.Skip(2))
        {
            var body = LbaBodyStudio.Body.Read(hqr.Read(int.Parse(arg)), 2, allowStatic: true);
            var w = body.World();
            Console.WriteLine(FormattableString.Invariant($"body {arg}: {body.Bones.Count} bones, {w.Length} points, x {w.Min(p => p.X):0}..{w.Max(p => p.X):0} y {w.Min(p => p.Y):0}..{w.Max(p => p.Y):0} z {w.Min(p => p.Z):0}..{w.Max(p => p.Z):0}"));
        }
        return 0;
    }
}
