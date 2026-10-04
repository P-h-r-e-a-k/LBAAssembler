using System.IO;

namespace LBAAssembler.Terrain;

// What Twinsen's car turns into while the super jet-pack drives it (RACEMOD.CPP, superjet_model=): the game's own super jet-pack -- the
// inventory's model, OBJFIX.HQR 48, the protopack's second look -- at about the race car's size. The engine can't scale a body as it draws
// it, so the build appends a copy of the model at Scale (RaceTrackSmallCars.Scaled: its points, spheres and box) to OBJFIX.HQR; the
// race-track mode draws it in the car's place, leaning forward, while the jet-pack lasts.
internal static class RaceTrackSuperJet
{
    public const int Source = 48;
    // (the model is 3,000 wide and 2,800 tall, the racer's car 1,270 wide and 900 tall)
    public const double Scale = 0.45;

    // Into the game folder: the copy appended to OBJFIX.HQR. Its index, and a line for the log.
    public static (int Index, string Log) Install(string gameDirectory)
    {
        var path = Path.Combine(gameDirectory, "OBJFIX.HQR");
        var model = RaceTrackSmallCars.Scaled(HqrArchive.Open(path).Read(Source), Scale);
        var index = HqrArchive.CountEntries(path);
        File.WriteAllBytes(path, HqrWriter.AppendEntry(File.ReadAllBytes(path), HqrWriter.StoredEntry(model)));
        return (index, $"the super jet-pack the car turns into: OBJFIX.HQR entry {index} (entry {Source} at {Scale:0.##} of its size)");
    }
}
