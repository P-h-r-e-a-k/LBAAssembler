using LBAAssembler.Scenes;

namespace ScriptRoundTrip;

// actors <game folder> <scene> [first [last]] [scripts]: an LBA2 scene's actors as the scene file has them -- entity, body, anim, place,
// turn, flags, move, armour, life -- and with "scripts" each one's life and track scripts too; read only (to compare a build's scene with
// the game's own).
internal static class ActorsCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("actors <game folder> <scene> [first [last]] [scripts]"); return 1; }
        var withScripts = args[^1] == "scripts";
        if (withScripts) args = args[..^1];
        var store = new SceneStore(SceneGame.Lba2, args[1]);
        var scene = int.Parse(args[2]);
        var model = store.Load(scene);
        var scripts = withScripts ? LBAAssembler.LbaScript.SceneScripts.Load(store.LoadRecord(scene), scene) : null;
        int first = args.Length > 3 ? int.Parse(args[3]) : 0, last = args.Length > 4 ? int.Parse(args[4]) : model.Actors.Count - 1;
        for (var i = Math.Max(0, first); i <= Math.Min(last, model.Actors.Count - 1); i++)
        {
            var a = model.Actors[i];
            Console.WriteLine($"actor {i}: entity {a.Entity} body {a.Body} anim {a.Anim} at ({a.X},{a.Y},{a.Z}) beta {a.Beta} flags 0x{a.Flags:x} move {a.Move} armour {a.Armor} life {a.LifePoints}");
            if (scripts is null) continue;
            foreach (var kind in new[] { LBAAssembler.LbaScript.ScriptKind.Life, LBAAssembler.LbaScript.ScriptKind.Track })
            {
                string text;
                try { text = scripts.GetText(i, kind); } catch (InvalidOperationException) { continue; }
                if (string.IsNullOrWhiteSpace(text)) continue;
                Console.WriteLine($"  -- {kind}:");
                foreach (var line in text.Split('\n')) Console.WriteLine("  " + line.TrimEnd('\r'));
            }
        }
        return 0;
    }
}
