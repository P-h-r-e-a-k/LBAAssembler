using LBAAssembler.Scenes;

namespace ScriptRoundTrip;

// scenechecksum <game folder> <scene>...: each LBA2 scene's checksum as its file has it (what a save's is compared with -- SAVEGAME.CPP:
// a save whose checksum isn't its scene's starts that scene afresh)
internal static class SceneChecksumCommand
{
    public static int Run(string[] args)
    {
        var store = new SceneStore(SceneGame.Lba2, args[1]);
        foreach (var scene in args.Skip(2).Select(int.Parse))
            Console.WriteLine(store.SceneExists(scene) ? $"scene {scene}: checksum {store.Load(scene).Checksum:X8}" : $"scene {scene}: none");
        return 0;
    }
}
