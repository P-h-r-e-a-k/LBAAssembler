using LBAAssembler.Scenes;

namespace ScriptRoundTrip;

// nuke <lba1|lba2> <game folder> <scene> [island .ILE] [--dry]: Build > Nuke without the window (SceneNuke): what goes, then (not with --dry)
// writes it as one undo step. For a copy of the game, never the real folder.
internal static class NukeCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 4) { Console.WriteLine("nuke <lba1|lba2> <game folder> <scene> [island .ILE] [--dry]"); return 1; }
        var lba1 = args[1].Equals("lba1", StringComparison.OrdinalIgnoreCase);
        var dir = args[2]; var scene = int.Parse(args[3]);
        var island = args.Skip(4).FirstOrDefault(a => !a.StartsWith("--"));
        var nuke = lba1 ? SceneNuke.ForLba1(dir, scene) : SceneNuke.ForLba2(dir, scene, island);
        Console.WriteLine($"{nuke.Where}: goes: {nuke.Summary}; exits kept {nuke.Exits}; floored columns {nuke.Columns}; island objects {nuke.Decors}; level {nuke.Level}");
        foreach (var w in nuke.Warnings) Console.WriteLine("  warning: " + w);
        if (args.Contains("--dry")) return 0;
        nuke.Commit();
        Console.WriteLine($"written: {SceneHistory.UndoDescription}");
        return 0;
    }
}
