using LBAAssembler;
using LBAAssembler.Terrain;
using LbaBodyStudio;

namespace ScriptRoundTrip;

// templeremodel <game folder> <out folder>: the Temple of Bú's remodel (TempleRemodelProps) looked at before a build -- each prop drawn as the
// game's iso camera sees it (HotelPreview.Draw), then the skeletons dressed and animated put into the game folder's BODY.HQR, ANIM.HQR,
// RESS.HQR and SAMPLES.HQR (use a copy of just those four) and each dressed skeleton drawn at rest; the tune's number said
internal static class TempleRemodelPreview
{
    public static int Run(string[] args)
    {
        var game = args[1];
        Directory.CreateDirectory(args[2]);
        var header = Body.Read(HqrArchive.Open(Path.Combine(game, "BODY.HQR")).Read(30), 2, allowStatic: true).Header;
        var raw = HqrArchive.Open(Path.Combine(game, "RESS.HQR")).Read(0);
        var palette = Enumerable.Range(0, 256).Select(i => (raw[i * 3], raw[i * 3 + 1], raw[i * 3 + 2])).ToArray();
        foreach (var (generic, name, build, _) in TempleRemodelProps.Props)
        {
            var body = build(header);
            var path = Path.Combine(args[2], $"prop{generic}.png");
            HotelPreview.Draw(body, palette, path);
            Console.WriteLine($"prop {generic} {name}: {body.Vertices.Count} points, {body.Faces.Count} polygons -> {path}");
        }
        foreach (var line in TempleRemodelProps.InstallSkeletons(game)) Console.WriteLine(line);
        var table = Lba2EntityTable.Load(game)!;
        var entity = TempleRemodelProps.SkeletonEntity(game);
        foreach (var (generic, index) in table.Entities[entity].Bodies)
        {
            if (generic < TempleRemodelProps.Skittle) continue;
            var body = Body.Read(HqrArchive.Open(Path.Combine(game, "BODY.HQR")).Read(index), 2);
            var path = Path.Combine(args[2], $"skeleton{generic}.png");
            HotelPreview.Draw(body, palette, path);
            Console.WriteLine($"skeleton {generic}: BODY.HQR {index}, {body.Vertices.Count} points, {body.Faces.Count} polygons -> {path}");
        }
        Console.WriteLine($"the tune: SAMPLES.HQR {TempleRemodelProps.InstallTune(game)}, {TempleRemodelProps.TuneSeconds:0.00} s");
        return 0;
    }
}
