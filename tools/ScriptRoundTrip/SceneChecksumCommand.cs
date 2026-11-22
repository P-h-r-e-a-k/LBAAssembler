using LBAAssembler.Scenes;

namespace ScriptRoundTrip;

// scenechecksum <game folder> <scene>...: each LBA2 scene's checksum as its file has it (what a save's is compared with -- SAVEGAME.CPP:
// a save whose checksum isn't its scene's starts that scene afresh)
// scenepoints <game folder> <scene> [first [last]]: a scene's track points (x, y, z)
internal static class SceneChecksumCommand
{
    public static int Run(string[] args)
    {
        var store = new SceneStore(SceneGame.Lba2, args[1]);
        foreach (var scene in args.Skip(2).Select(int.Parse))
            Console.WriteLine(store.SceneExists(scene) ? $"scene {scene}: checksum {store.Load(scene).Checksum:X8}" : $"scene {scene}: none");
        return 0;
    }

    public static int Points(string[] args)
    {
        var model = new SceneStore(SceneGame.Lba2, args[1]).Load(int.Parse(args[2]));
        int first = args.Length > 3 ? int.Parse(args[3]) : 0, last = args.Length > 4 ? int.Parse(args[4]) : model.TrackPoints.Count - 1;
        for (var i = Math.Max(0, first); i <= Math.Min(last, model.TrackPoints.Count - 1); i++)
            Console.WriteLine($"point {i}: ({model.TrackPoints[i].X}, {model.TrackPoints[i].Y}, {model.TrackPoints[i].Z})");
        return 0;
    }
}
