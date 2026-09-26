using LBAAssembler;
using LBAAssembler.Scenes;
using LbaScript = LBAAssembler.LbaScript;

namespace ScriptRoundTrip;

// What the scenes of one island cube hold (actors with their scripts and routes, zones, route points): the Desert island race track is cube (7, 10).
//   racetrack [island cube x] [cube z] [--scripts]
internal static class RaceTrackProbe
{
    public static int Run(string[] args)
    {
        var dir = Environment.GetEnvironmentVariable("LBA2_DIR") ?? @"E:\GOG Games\Little Big Adventure 2 - Level viewer";
        int cx = args.Length > 1 && int.TryParse(args[1], out var a) ? a : 7, cz = args.Length > 2 && int.TryParse(args[2], out var b) ? b : 10;
        var scripts = args.Contains("--scripts");
        var store = new SceneStore(SceneGame.Lba2, dir);
        var names = HqdDescriptions.Load("SCENE2.HQD", 0).Names;
        var bodyNames = HqdDescriptions.Load("BODY2.HQD", 0).Names;
        var entities = Lba2EntityTable.Load(dir);
        for (var scene = 0; scene < 260; scene++)
        {
            if (!store.SceneExists(scene)) continue;
            SceneModel model;
            try { model = store.Load(scene); } catch { continue; }
            if (model.CubeMode != 1 || model.CubeX != cx || model.CubeY != cz) continue;
            Console.WriteLine($"=== scene {scene}: {(scene + 1 < names.Count ? names[scene + 1] : "?")}  island {model.Island} cube ({model.CubeX},{model.CubeY}) music {model.Music} light {model.AlphaLight}/{model.BetaLight}");
            Console.WriteLine($"  hero start ({model.Hero.X},{model.Hero.Y},{model.Hero.Z}) beta {model.Hero.Beta}");
            for (var i = 1; i < model.Actors.Count; i++)
            {
                var actor = model.Actors[i];
                string body = "-", anim = "-";
                if (entities?.Entities.FirstOrDefault(e => e.Id == actor.Entity) is { } entity)
                {
                    var bodyIndex = entity.Bodies.Where(x => x.Generic == actor.Body).Select(x => (int?)x.Body).FirstOrDefault();
                    if (bodyIndex is { } bi) body = $"{bi} \"{(bi + 1 < bodyNames.Count ? bodyNames[bi + 1] : "?")}\"";
                    var animIndex = entity.Anims.Where(x => x.Generic == actor.Anim).Select(x => (int?)x.Anim).FirstOrDefault();
                    if (animIndex is { } ai) anim = ai.ToString();
                }
                Console.WriteLine($"  actor {i}: entity {actor.Entity} body {actor.Body} (BODY.HQR {body}) anim {actor.Anim} ({anim}) sprite {actor.Sprite} flags 0x{actor.Flags:X} pos ({actor.X},{actor.Y},{actor.Z}) beta {actor.Beta} move {actor.Move} life {actor.LifePoints} hit {actor.HitForce} armor {actor.Armor}  life script {actor.Life.Length} B, track script {actor.Track.Length} B");
                if (scripts)
                    foreach (var line in LbaScript.Disassembly.Text(actor.Life, actor.Track, LbaScript.Opcodes.Lba2).Split('\n')) Console.WriteLine("      " + line.TrimEnd('\r'));
            }
            for (var i = 0; i < model.Zones.Count; i++)
            {
                var z = model.Zones[i];
                Console.WriteLine($"  zone {i}: type {z.Type} num {z.Num} box ({z.X0},{z.Y0},{z.Z0})-({z.X1},{z.Y1},{z.Z1}) info [{string.Join(",", z.Info)}]");
            }
            for (var i = 0; i < model.TrackPoints.Count; i++) Console.WriteLine($"  point {i}: ({model.TrackPoints[i].X},{model.TrackPoints[i].Y},{model.TrackPoints[i].Z})");
        }
        return 0;
    }
}
