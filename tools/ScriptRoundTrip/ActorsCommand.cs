using LBAAssembler.Scenes;

namespace ScriptRoundTrip;

// actors <game folder> <scene> [first [last]]: an LBA2 scene's actors as the scene file has them -- entity, body, anim, place, turn,
// flags, move, armour, life -- read only (to compare a build's scene with the game's own).
internal static class ActorsCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("actors <game folder> <scene> [first [last]]"); return 1; }
        var model = new SceneStore(SceneGame.Lba2, args[1]).Load(int.Parse(args[2]));
        int first = args.Length > 3 ? int.Parse(args[3]) : 0, last = args.Length > 4 ? int.Parse(args[4]) : model.Actors.Count - 1;
        for (var i = Math.Max(0, first); i <= Math.Min(last, model.Actors.Count - 1); i++)
        {
            var a = model.Actors[i];
            Console.WriteLine($"actor {i}: entity {a.Entity} body {a.Body} anim {a.Anim} at ({a.X},{a.Y},{a.Z}) beta {a.Beta} flags 0x{a.Flags:x} move {a.Move} armour {a.Armor} life {a.LifePoints}");
        }
        return 0;
    }
}
