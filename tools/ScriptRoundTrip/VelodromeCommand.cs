using LBAAssembler;
using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// The Dome of the Slate's velodrome (Terrain/DomeVelodrome): velodromepicture <game> <out.png> draws the hall as the model has it, before
// it is cut into bricks; velodrome <game> builds it into a game folder's files.
internal static class VelodromeCommand
{
    public static int Picture(string[] args)
    {
        var palette = HqrArchive.Open(System.IO.Path.Combine(args[1], "RESS.HQR")).Read(0);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var (bgra, w, h) = DomeVelodrome.Preview(palette);
        PngWriter.Write(args[2], bgra, w, h);
        Console.WriteLine($"{w} x {h} in {clock.Elapsed.TotalSeconds:0.0} s");
        return 0;
    }

    public static int Pixel(string[] args)
    {
        var palette = HqrArchive.Open(System.IO.Path.Combine(args[1], "RESS.HQR")).Read(0);
        for (var i = 2; i + 1 < args.Length; i += 2) Console.WriteLine(DomeVelodrome.DebugPixel(palette, double.Parse(args[i]) - 1090 + 0.5, double.Parse(args[i + 1]) - 170 + 0.5));
        return 0;
    }

    public static int Install(string[] args)
    {
        foreach (var line in DomeVelodrome.Install(args[1])) Console.WriteLine(line);
        return 0;
    }
}
