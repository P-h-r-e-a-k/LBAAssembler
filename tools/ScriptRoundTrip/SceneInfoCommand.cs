using LBAAssembler;
using LBAAssembler.Scenes;

namespace ScriptRoundTrip;

// sceneinfo <game folder> <scene>...: each LBA2 scene's island, cube, mode and actors, and its zones (type, destination, box).
internal static class SceneInfoCommand
{
    // racecheck <game folder>: every actor left in a built game that goes after another (follow, attack) wherever it stood: the road means nothing to it.
    // Nothing but cars and a race's own should do that outside on a race island. The list is the test: 0 lines is a pass.
    public static int Check(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("racecheck <game folder>"); return 1; }
        var store = new SceneStore(SceneGame.Lba2, args[1]);
        var found = 0;
        for (var scene = 0; scene < 400; scene++)
        {
            if (!store.SceneExists(scene)) continue;
            var m = store.Load(scene);
            if (m.CubeMode != 1) continue;
            for (var i = 1; i < m.Actors.Count; i++)
            {
                var a = m.Actors[i];
                var why = new List<string>();
                if (a.Move is 2 or 4 or 5) why.Add($"starts on move {a.Move}");
                for (var n = 1; n < m.Actors.Count; n++)
                {
                    if (m.Actors[n].Life.Length == 0) continue;
                    var code = LBAAssembler.LbaScript.Bytecode.DecodeLife(m.Actors[n].Life, out var failure);
                    if (failure is not null) continue;
                    foreach (var ins in code)
                    {
                        var name = LBAAssembler.LbaScript.Opcodes.Life(ins.Op)?.Name;
                        if (name == "SET_DIR" && n == i && ins.A.Length > 0 && ins.A[0] is 2 or 4 or 5) why.Add($"SET_DIR({string.Join(",", ins.A)})");
                        if (name == "SET_DIR_OBJ" && ins.A.Length > 1 && ins.A[0] == i && ins.A[1] is 2 or 4 or 5) why.Add($"actor {n}: SET_DIR_OBJ({string.Join(",", ins.A)})");
                    }
                }
                if (why.Count == 0) continue;
                found++;
                Console.WriteLine($"scene {scene} island {m.Island} actor {i}: entity {a.Entity} at {a.X},{a.Y},{a.Z} flags {a.Flags:x} life {a.Life.Length} -- {string.Join("; ", why.Distinct())}");
            }
        }
        Console.WriteLine($"{found} actor(s)");
        return found;
    }

    // islandview <game folder>: how far each island is seen: the fog's start and the far clip of its cubes, each pair and how many cubes have it.
    public static int IslandView(string[] args)
    {
        foreach (var file in Directory.GetFiles(args[1], "*.ILE").OrderBy(f => f))
        {
            var island = LBAAssembler.Terrain.IslandFile.Load(file);
            var pairs = island.Cubes.Values.GroupBy(c => (Fog: c.Info[LBAAssembler.Terrain.IslandCube.InfoStartZFog], Far: c.Info[LBAAssembler.Terrain.IslandCube.InfoClipZFar], Sky: c.Info[LBAAssembler.Terrain.IslandCube.InfoSkyY]))
                .Select(g => $"fog {g.Key.Fog} far {g.Key.Far} sky {g.Key.Sky} x{g.Count()}");
            Console.WriteLine($"{Path.GetFileName(file),-16} {string.Join("; ", pairs)}");
        }
        return 0;
    }

    // entityanims <game folder> <entity>: an entity's records, bytes and all (an animation's own actions: what it throws, and when).
    public static int EntityAnims(string[] args)
    {
        var table = HqrArchive.Open(Path.Combine(args[1], "RESS.HQR")).Read(44);
        foreach (var r in Lba2EntityTable.Records(table, int.Parse(args[2])))
            Console.WriteLine($"command {r.Command} generic {r.Generic} size {r.Size}: {Convert.ToHexString(table, r.At, Math.Min(r.Size, 80))}");
        return 0;
    }

    public static int Run(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("sceneinfo <game folder> <scene>..."); return 1; }
        var store = new SceneStore(SceneGame.Lba2, args[1]);
        foreach (var number in args.Skip(2).Select(int.Parse))
        {
            var m = store.Load(number);
            Console.WriteLine($"scene {number}: island {m.Island}, cube ({m.CubeX},{m.CubeY}), mode {m.CubeMode}, {m.Actors.Count} actors, {m.Zones.Count} zones, {m.TrackPoints.Count} track points");
            foreach (var z in m.Zones)
                Console.WriteLine($"  zone type {z.Type} num {z.Num}: x {z.X0}..{z.X1} y {z.Y0}..{z.Y1} z {z.Z0}..{z.Z1} info {string.Join(",", z.Info)}");
            // (SCENEINFO_POINTS=1: the track points and where each actor stands too)
            if (Environment.GetEnvironmentVariable("SCENEINFO_POINTS") != "1") continue;
            for (var i = 0; i < m.TrackPoints.Count; i++) Console.WriteLine($"  point {i}: {m.TrackPoints[i].X}, {m.TrackPoints[i].Y}, {m.TrackPoints[i].Z}");
            for (var i = 0; i < m.Actors.Count; i++) Console.WriteLine($"  actor {i}: entity {m.Actors[i].Entity} at {m.Actors[i].X}, {m.Actors[i].Y}, {m.Actors[i].Z} beta {m.Actors[i].Beta} flags {m.Actors[i].Flags:x} body {m.Actors[i].Body} sprite {m.Actors[i].Sprite} move {m.Actors[i].Move}");
        }
        return 0;
    }
}
