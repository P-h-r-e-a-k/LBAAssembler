using System.IO;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The scene side of the Desert island race track: every exterior scene of the island loses its actors except Twinsen and the buggy,
// and the scene the start line lies in starts Twinsen and the buggy on the line.
internal static class RaceTrackScenes
{
    public const int BuggyEntity = 152;
    public const int ZoeEntity = 14;

    public sealed record Result(List<string> Log, int ScenesChanged, int ActorsRemoved);

    // island: the island number the scenes carry (Desert = 2). start: island cell coordinates of the start line and the way the lap runs.
    public static Result Apply(string gameDirectory, int island, (double X, double Z, double Y, double DirX, double DirZ)? start, Func<double, double, double>? distanceToRoad = null, double roadReach = 6.5)
    {
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var log = new List<string>();
        var changes = new List<SceneChange>();
        var removed = 0; var zonesRemoved = 0;
        for (var scene = 0; scene < store.SceneCount; scene++)
        {
            if (!store.SceneExists(scene)) continue;
            SceneModel model;
            try { model = store.Load(scene); } catch (Exception e) when (e is InvalidDataException or ArgumentException or IOException) { continue; }
            if (model.Island != island || model.CubeMode != 1) continue;
            // the demo scenes are copies the game plays as films; they are left as they are
            if (scene >= 190) { log.Add($"scene {scene}: demo scene, left alone"); continue; }
            var count = 0;
            for (var i = model.Actors.Count - 1; i >= 1; i--)
            {
                if (model.Actors[i].Entity == BuggyEntity) continue;
                // slot 1 of every scene is the engine's own placeholder for Zoe (entity 14, no body, parked at 0,0,0); the engine treats that slot
                // specially -- a buggy that took its place came up with no life -- so it stays
                if (i == 1 && model.Actors[i].Entity == ZoeEntity && model.Actors[i].X == 0 && model.Actors[i].Z == 0) continue;
                SceneOps.DeleteActor(model, i, retarget: 0);
                count++;
            }
            removed += count;
            // The buggy's own script removes it until the quest that mends it is done (game variable 74 >= 3). The compare is
            //   IF VAR_GAME(74) >= 3   =   0C 0F 4A 03 03 00 ..
            // and reads >= 0 with the 3 zeroed, so the buggy is there from the start of any game.
            foreach (var actor in model.Actors.Skip(1).Where(a => a.Entity == BuggyEntity))
                if (actor.Life.Length > 6 && actor.Life[0] == 0x0C && actor.Life[1] == 0x0F && actor.Life[2] == 0x4A && actor.Life[3] == 0x03 && actor.Life[4] == 0x03 && actor.Life[5] == 0x00)
                { actor.Life[4] = 0x00; log.Add($"scene {model.CubeX},{model.CubeY}: the buggy no longer waits for the car quest"); }
            log.Add($"scene {scene} (cube {model.CubeX},{model.CubeY}): {count} actors removed, {model.Actors.Count - 1} left");

            if (start is { } s)
            {
                var wx = s.X * 512; var wz = s.Z * 512;
                var cx = (int)Math.Floor(wx / IslandFile.CubeSize); var cz = (int)Math.Floor(wz / IslandFile.CubeSize);
                if (cx == model.CubeX && cz == model.CubeY)
                {
                    var lx = (int)Math.Round(wx - cx * (double)IslandFile.CubeSize); var lz = (int)Math.Round(wz - cz * (double)IslandFile.CubeSize);
                    var beta = (int)Math.Round(Math.Atan2(s.DirX, s.DirZ) / (2 * Math.PI) * 4096); beta = ((beta % 4096) + 4096) % 4096;
                    var y = (int)Math.Round(s.Y);
                    // the buggy stands a few cells before the line, Twinsen beside it
                    int At(double back, double side, bool z) => (int)Math.Round((z ? lz : lx) + (z ? (-s.DirZ * back + s.DirX * side) : (-s.DirX * back - s.DirZ * side)) * 512);
                    var buggy = model.Actors.Skip(1).FirstOrDefault(a => a.Entity == BuggyEntity);
                    if (buggy is not null)
                    {
                        buggy.X = At(4, 0, false); buggy.Z = At(4, 0, true); buggy.Y = y; buggy.Beta = beta;
                        // The buggy is one object for the whole game (BUGGY.CPP): it stands where INIT_BUGGY last put it, and INIT_BUGGY(0), which the
                        // island's scenes run, only shows it if it already stands in this cube. INIT_BUGGY(2) puts it at the actor's own place: here, the start line.
                        if (buggy.Life.Length > 10 && buggy.Life[8] == 0x46 && buggy.Life[9] == 0x00) { buggy.Life[9] = 0x02; log.Add($"scene {scene}: the buggy is put on the start line whenever the scene starts (INIT_BUGGY 2)"); }
                    }
                    model.Hero.X = At(7, 3.2, false); model.Hero.Z = At(7, 3.2, true); model.Hero.Y = y + 300; model.Hero.Beta = beta;
                    log.Add($"scene {scene}: Twinsen at ({model.Hero.X},{model.Hero.Y},{model.Hero.Z}) turn {beta}, buggy at ({buggy?.X},{buggy?.Y},{buggy?.Z})");
                }
            }
            // zones that would act on a car driving along the road: doors into buildings (cube changes to scenes that are not part of the island's
            // outside), hit, ladder, escalator, grid and rail zones. The cube-edge changes, camera, scenario, giver and message zones stay.
            if (distanceToRoad is not null)
                for (var z = model.Zones.Count - 1; z >= 0; z--)
                {
                    var zone = model.Zones[z];
                    var kind = zone.Type;
                    var door = kind == 0 && (zone.Num < 55 || zone.Num > 73);
                    if (!(door || kind is 3 or 6 or 7 or 8 or 9)) continue;
                    var ox = model.CubeX * (double)IslandFile.CubeSize; var oz = model.CubeY * (double)IslandFile.CubeSize;
                    var hit = false;
                    for (var cz = (int)Math.Floor((oz + zone.Z0) / 512); cz <= (int)Math.Floor((oz + zone.Z1) / 512) && !hit; cz++)
                    for (var cx = (int)Math.Floor((ox + zone.X0) / 512); cx <= (int)Math.Floor((ox + zone.X1) / 512) && !hit; cx++)
                        if (distanceToRoad(cx + 0.5, cz + 0.5) <= roadReach) hit = true;
                    if (!hit) continue;
                    log.Add($"scene {scene}: zone {z} (type {kind}, number {zone.Num}) lies on the road and is removed");
                    SceneOps.DeleteZone(model, z);
                    zonesRemoved++;
                }
            changes.Add(new SceneChange(scene, model, null));
        }
        if (changes.Count > 0) store.SaveMany(changes, allowErrors: true);
        log.Add($"{zonesRemoved} zones on the road removed");
        return new Result(log, changes.Count, removed);
    }
}
