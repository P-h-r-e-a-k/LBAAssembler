using System.IO;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The scene side of the Desert island race track: every exterior scene of the island loses its actors except Twinsen and the buggy,
// and the scene the start line lies in starts Twinsen and the buggy on the line.
internal static class RaceTrackScenes
{
    public const int BuggyEntity = 152;
    public const int ZoeEntity = 14;

    public sealed record Result(List<string> Log, int ScenesChanged, int ActorsRemoved);

    public const int DesertIsland = 2;

    // Edits the outside scenes of the Desert island as the options say, from what the build of the island found (start line, jump, road).
    public static Result Apply(string gameDirectory, RaceTrackReport report, RaceTrackOptions options)
    {
        var island = DesertIsland;
        (double X, double Z, double Y, double DirX, double DirZ)? start = options.StartAtLine && report.StartLine.Count > 0 ? report.StartLine[0] : null;
        var distanceToRoad = report.DistanceToRoad; var roadReach = 6.5; var jump = report.Jump;
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var log = new List<string>();
        var changes = new List<SceneChange>();
        var removed = 0; var zonesRemoved = 0; var camerasRemoved = 0;
        for (var scene = 0; scene < store.SceneCount; scene++)
        {
            if (!store.SceneExists(scene)) continue;
            SceneModel model;
            try { model = store.Load(scene); } catch (Exception e) when (e is InvalidDataException or ArgumentException or IOException) { continue; }
            if (model.Island != island || model.CubeMode != 1) continue;
            // the demo scenes are copies the game plays as films; they are left as they are
            if (scene >= 190) { log.Add($"scene {scene}: demo scene, left alone"); continue; }
            var count = 0;
            if (options.RemoveActors)
            {
                // slot 1 of every scene is the engine's own placeholder for Zoe (entity 14, no body, parked at 0,0,0); the engine treats that slot
                // specially -- a buggy that took its place came up with no life -- so it stays
                var needed = CutsceneActors(model);
                var doomed = Enumerable.Range(1, model.Actors.Count - 1)
                    .Where(i => model.Actors[i].Entity != BuggyEntity && !(i == 1 && model.Actors[i].Entity == ZoeEntity && model.Actors[i].X == 0 && model.Actors[i].Z == 0))
                    .Where(i => !needed.Contains(i))
                    .ToList();
                if (needed.Count > 0) log.Add($"scene {scene}: actors {string.Join(", ", needed.Order())} kept -- Twinsen's own script waits on them in its travel cutscenes (the ferry, the Dino-Fly), which would never end without them");
                var standIn = AddStandIn(model, doomed);
                foreach (var i in doomed.OrderByDescending(i => i))
                {
                    SceneOps.DeleteActor(model, i, retarget: standIn is { } stand ? stand - count : 0);
                    count++;
                }
                if (standIn is { } si)
                {
                    TidyStandIn(model, si - count);
                    log.Add($"scene {scene}: the scripts that stay referred to removed actors; those references now go to a stand-in (actor {si - count})");
                }
            }
            removed += count;
            if (report.GroundBefore is { } groundBefore && report.GroundAfter is { } groundAfter && report.WasGround is { } wasGround)
                Reseat(model, groundBefore, groundAfter, wasGround, scene, log);
            // The buggy's own script removes it until the quest that mends it is done (game variable 74 >= 3). The compare is
            //   IF VAR_GAME(74) >= 3   =   0C 0F 4A 03 03 00 ..
            // and reads >= 0 with the 3 zeroed, so the buggy is there from the start of any game.
            foreach (var actor in model.Actors.Skip(1).Where(a => a.Entity == BuggyEntity && options.BuggyAlways))
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
                    // the buggy stands a few cells before the line, Twinsen beside it; each on the ground at its own spot (the road climbs there)
                    int At(double back, double side, bool z) => (int)Math.Round((z ? lz : lx) + (z ? (-s.DirZ * back + s.DirX * side) : (-s.DirX * back - s.DirZ * side)) * 512);
                    int Ground(int x, int z) => report.GroundAfter is { } g ? (int)Math.Round(g(cx * 64 + x / 512.0, cz * 64 + z / 512.0)) : y;
                    var buggy = model.Actors.Skip(1).FirstOrDefault(a => a.Entity == BuggyEntity);
                    var buggyIndex = buggy is null ? -1 : model.Actors.IndexOf(buggy);
                    if (buggy is not null) { buggy.X = At(4, 0, false); buggy.Z = At(4, 0, true); buggy.Y = Ground(buggy.X, buggy.Z); buggy.Beta = beta; }
                    model.Hero.X = At(7, 3.2, false); model.Hero.Z = At(7, 3.2, true); model.Hero.Y = Ground(model.Hero.X, model.Hero.Z) + 100; model.Hero.Beta = beta;
                    log.Add($"scene {scene}: Twinsen at ({model.Hero.X},{model.Hero.Y},{model.Hero.Z}) turn {beta}, buggy at ({buggy?.X},{buggy?.Y},{buggy?.Z})");
                    if (buggyIndex > 0 && StartBuggyScript(model, scene, buggyIndex, log) is { } withBuggy) model = withBuggy;
                }
            }
            // zones that would act on a car driving along the road: doors into buildings (cube changes to scenes that are not part of the island's
            // outside), hit, ladder, escalator, grid and rail zones; and, with RemoveTrackCameras, the fixed cameras (type 1) the car would
            // drive into. The cube-edge changes, scenario, giver and message zones stay.
            if (options.RemoveRoadZones || options.RemoveTrackCameras)
                for (var z = model.Zones.Count - 1; z >= 0; z--)
                {
                    var zone = model.Zones[z];
                    var kind = zone.Type;
                    var door = kind == 0 && (zone.Num < 55 || zone.Num > 73);
                    // (only cameras that are on from the start: one that starts off is switched on only by a cutscene's script -- the
                    // ferry's arrival, a call of the car -- which then needs it, and the car never meets it)
                    var camera = kind == 1 && options.RemoveTrackCameras && zone.Info.Length > 7 && (zone.Info[7] & 1) != 0;
                    if (!(camera || options.RemoveRoadZones && (door || kind is 3 or 6 or 7 or 8 or 9))) continue;
                    // (tested at the centres of the cells the box covers: half a cell's diagonal more makes it the box itself that counts)
                    var reach = camera ? roadReach + options.CameraMargin + Math.Sqrt(0.5) : roadReach;
                    var ox = model.CubeX * (double)IslandFile.CubeSize; var oz = model.CubeY * (double)IslandFile.CubeSize;
                    var hit = false;
                    for (var cz = (int)Math.Floor((oz + zone.Z0) / 512); cz <= (int)Math.Floor((oz + zone.Z1) / 512) && !hit; cz++)
                    for (var cx = (int)Math.Floor((ox + zone.X0) / 512); cx <= (int)Math.Floor((ox + zone.X1) / 512) && !hit; cx++)
                        if (distanceToRoad(cx + 0.5, cz + 0.5) <= reach) hit = true;
                    if (!hit) continue;
                    log.Add(camera ? $"scene {scene}: fixed camera zone {z} (number {zone.Num}) reaches the track and is removed"
                                   : $"scene {scene}: zone {z} (type {kind}, number {zone.Num}) lies on the road and is removed");
                    SceneOps.DeleteZone(model, z);
                    if (camera) camerasRemoved++; else zonesRemoved++;
                }
            if (jump is not null && model.CubeX == jump.CubeX && model.CubeY == jump.CubeZ)
            {
                var jumped = AddJump(model, scene, jump, log);
                if (jumped is not null) model = jumped;
            }
            changes.Add(new SceneChange(scene, model, null));
        }
        if (changes.Count > 0) store.SaveMany(changes, allowErrors: true);
        log.Add($"{zonesRemoved} zones on the road removed");
        if (options.RemoveTrackCameras) log.Add($"{camerasRemoved} fixed camera zones along the track removed");
        return new Result(log, changes.Count, removed);
    }

    // Where the scripts that stay (Twinsen's own life script, mostly) refer to actors about to be removed -- "if Twinsen is near the
    // shopkeeper and presses Action, talk, then send the shopkeeper's track to @45" -- those references need somewhere harmless to go.
    // Pointing them at Twinsen (actor 0) made his own scripts act on him: in scene 67 every press of Action (which is also how he gets
    // into the car) found him at distance 0 from "the shopkeeper", made him speak and jumped his own track script to a foreign
    // offset. The stand-in is an invisible actor with no body and no shadow, 20000 below the ground (the engine's distance() and
    // distance_message() give 32000, "far away", when the heights differ by 1500 or more), whose life and track scripts are a single
    // END (TidyStandIn sends every jump into them to that END). Added at the end of the list; returns its index before the
    // removals, or null when nothing that stays refers to a removed actor.
    private static int? AddStandIn(SceneModel model, List<int> doomed)
    {
        if (doomed.Count == 0) return null;
        var gone = doomed.ToHashSet();
        var referred = doomed.Any(i => SceneOps.ReferencesTo(model, ArgRole.Obj, i).Any(r => !gone.Contains(r.Actor)));
        if (!referred || model.Actors.Count >= SceneValidator.MaxObjects) return null;
        // where it stands across the ground: where the removed actor Twinsen's script turns him towards (set_dir(follow, n) -- the car
        // he calls, the bell he rings, the telescope) stood, so he still faces the right way
        var (sx, sz) = (512, 512);
        using (Opcodes.Use(Opcodes.Lba2))
        {
            var code = Bytecode.DecodeLife(model.Hero.Life, out var failure);
            if (failure is null)
                foreach (var ins in code)
                {
                    var def = Opcodes.Life(ins.Op);
                    if (def?.Form != LifeForm.Dir || ins.A.Length <= def.Args.Length || ins.A[def.Args.Length - 1] != 2) continue;
                    var followed = (int)ins.A[def.Args.Length];
                    if (!gone.Contains(followed)) continue;
                    (sx, sz) = (model.Actors[followed].X, model.Actors[followed].Z);
                    break;
                }
        }
        var standIn = SceneOps.BlankActor(SceneGame.Lba2, sx, -20000, sz, entity: 16);
        standIn.Life = new byte[] { 0 }; standIn.Track = new byte[] { 0 };
        standIn.Flags = InvisibleNoShadow; standIn.Body = -1; standIn.Armor = 51; standIn.LifePoints = -1; standIn.CoulObj = 4;
        return SceneOps.AddActor(model, standIn);
    }

    // INVISIBLE | NO_SHADOW, as the retail invisible helper actors ("Dots") have: a script's BODY_OBJ can give the stand-in a body.
    private const uint InvisibleNoShadow = 0x1200;
    // OBJ_FALLABLE: the actor falls onto the ground below it.
    private const uint Fallable = 0x0800;

    // After the removals: every set_track_obj / set_comportement_obj the kept scripts aim at the stand-in goes to offset 0 (its one
    // END), and a camera told to follow it follows Twinsen instead -- it stands 20000 below the ground, and a cutscene's
    // cam_follow blanked the screen. (Its scripts could be ENDs as long as the removed ones' instead, but the editor can't show
    // such a script as text.)
    private static void TidyStandIn(SceneModel model, int standIn)
    {
        using var scope = Opcodes.Use(Opcodes.Lba2);
        for (var n = 0; n < model.Actors.Count; n++)
        {
            var actor = model.Actors[n];
            if (n == standIn || actor.Life.Length == 0) continue;
            var code = Bytecode.DecodeLife(actor.Life, out var failure);
            if (failure is not null) continue;
            var changed = false;
            foreach (var ins in code)
            {
                if (ins.A.Length < 1 || ins.A[0] != standIn) continue;
                var name = Opcodes.Life(ins.Op)?.Name;
                if (name is "SET_TRACK_OBJ" or "SET_COMPORTEMENT_OBJ" && ins.A.Length > 1 && ins.A[1] != 0) { ins.A[1] = 0; changed = true; }
                else if (name == "CAM_FOLLOW") { ins.A[0] = 0; changed = true; }
            }
            if (changed) actor.Life = Bytecode.EncodeLife(code);
        }
    }

    // The actors a normal game can't do without: the ones Twinsen's own life script waits on (a test of their track's label,
    // l_track_obj(n) == k) or points the camera at (cam_follow(n)) -- the ferry, the Dino-Fly and their helpers, in the cutscenes
    // of arriving on and leaving the island. With them removed, arriving on the island by ferry or Dino-Fly, or leaving it, left
    // the player stuck for good. Not the actors that drive them: in scene 60 that would keep the game ending's whole cast -- the
    // Temple Park guard waits for its hidden director and the bowl players, who play petanque beside the road in normal play --
    // so the ending (played in scene 60) doesn't finish on a race track build; putting the original files back undoes it.
    private static HashSet<int> CutsceneActors(SceneModel model)
    {
        var needed = new HashSet<int>();
        var count = model.Actors.Count;
        using (Opcodes.Use(Opcodes.Lba2))
        {
            var code = Bytecode.DecodeLife(model.Hero.Life, out var failure);
            if (failure is not null) return needed;
            foreach (var ins in code)
            {
                var def = Opcodes.Life(ins.Op);
                if (def is null) continue;
                if (def.Name == "CAM_FOLLOW" && ins.A.Length > 0 && ins.A[0] > 0 && ins.A[0] < count) needed.Add((int)ins.A[0]);
                if (def.Form is LifeForm.Cond or LifeForm.Switch && Opcodes.Cond(ins.Func)?.Name == "L_TRACK_OBJ" && ins.FuncArg > 0 && ins.FuncArg < count
                    && (def.Form == LifeForm.Switch || ins.Test == 0))
                    needed.Add(ins.FuncArg);
            }
        }
        return needed;
    }

    // Track points, actors and Twinsen's start that stood on ground the build reshaped go with the ground (keeping their height
    // above it): left where the ground used to be they end up buried -- scene 57's buggy recovery point (42) was 911 under the new
    // road, and a buggy put there by its script showed only as a shadow. Only what stood on drawn ground, and only actors that
    // fall onto the ground (OBJ_FALLABLE): the sea is height 0 too, and the harbour ferry and its route, which the water
    // bridge's causeway now crosses, were lifted 1300 above the water.
    private static void Reseat(SceneModel model, Func<double, double, double> before, Func<double, double, double> after, Func<double, double, bool> wasGround, int scene, List<string> log)
    {
        var ox = model.CubeX * 64.0; var oz = model.CubeY * 64.0;
        int Rise(int x, int y, int z)
        {
            var cx = ox + x / 512.0; var cz = oz + z / 512.0;
            if (!wasGround(cx, cz)) return 0;
            var b = before(cx, cz); var a = after(cx, cz);
            return Math.Abs(a - b) > 100 && Math.Abs(y - b) < 400 ? (int)Math.Round(a - b) : 0;
        }
        var moved = 0;
        for (var i = 0; i < model.TrackPoints.Count; i++)
        {
            var p = model.TrackPoints[i]; var d = Rise(p.X, p.Y, p.Z);
            if (d != 0) { model.TrackPoints[i] = p with { Y = p.Y + d }; moved++; }
        }
        foreach (var a in model.Actors)
        {
            if ((a.Flags & Fallable) == 0 && a != model.Hero) continue;
            var d = Rise(a.X, a.Y, a.Z);
            if (d != 0) { a.Y += d; moved++; }
        }
        if (moved > 0) log.Add($"scene {scene}: {moved} track points and actors moved up or down with the reshaped ground");
    }

    // Scene 67, the start: the buggy stands on the start line whenever the scene starts on foot (INIT_BUGGY 2 puts the game's one
    // buggy at the actor's own place; the island's scenes run INIT_BUGGY 0, which only shows it where it already is) -- but not
    // when Twinsen drives back into the scene on the next lap: forcing it then parked a second, solid buggy on the start line
    // for the car to crash into.
    private static SceneModel? StartBuggyScript(SceneModel model, int scene, int buggy, List<string> log)
    {
        try
        {
            var scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
            var text = scripts.GetText(buggy, ScriptKind.Life);
            var pattern = new System.Text.RegularExpressions.Regex(@"init_buggy\(0\);\s*if \(12 == comportement_hero\(\)\)\s*\{\s*set_comportement\(comportement_2\);\s*\}\s*else\s*\{\s*set_comportement\(comportement_1\);\s*\}");
            if (!pattern.IsMatch(text)) { log.Add($"scene {scene}: the buggy's script isn't the expected one; it is left to show the buggy where it is"); return null; }
            text = pattern.Replace(text, "if (12 == comportement_hero())\n        {\n            init_buggy(0);\n            set_comportement(comportement_2);\n        }\n        else\n        {\n            init_buggy(2);\n            set_comportement(comportement_1);\n        }", 1);
            scripts.SetText(buggy, ScriptKind.Life, text);
            var built = scripts.Build();
            if (!built.Ok) { foreach (var e in built.Errors) log.Add($"scene {scene}: buggy script: {e}"); return null; }
            log.Add($"scene {scene}: the buggy is put on the start line whenever the scene starts on foot (INIT_BUGGY 2), not when Twinsen drives in");
            return SceneSerializer.Parse(SceneGame.Lba2, built.Record!);
        }
        catch (Exception error) when (error is ScriptCompileException or InvalidDataException or ArgumentException or InvalidOperationException)
        {
            log.Add($"scene {scene}: the buggy's start could not be set: {error.Message}");
            return null;
        }
    }

    // The retail "car jump" (scene 62 of the Desert island) is not physics -- the ground is one height map and a falling object does not move
    // sideways -- but a scripted flight: the hero's own life script watches for the buggy (behaviour 12) in a scenario zone while heading
    // roughly the right way, then sets the hero's movement to 12 (MOVE_BUGGY, moved by its animation alone) and starts a track that plays
    // animation 67 (ANIM.HQR 51: about 14 cells forward, up and down), and when the track reaches its last label gives the controls back
    // (movement 13). Here a small actor does what the retail hero script does, so no scene's own hero script has to be edited; the hero's
    // track script only gets the two labels.
    private static SceneModel? AddJump(SceneModel model, int scene, JumpInfo jump, List<string> log)
    {
        var ox = jump.CubeX * 64.0; var oz = jump.CubeZ * 64.0;
        var y = (int)Math.Round(jump.Height);
        var template = model.Zones.FirstOrDefault(z => z.Type == 2);
        foreach (var b in jump.Boxes)
        {
            var zone = template?.Clone() ?? new SceneZoneModel { Info = new int[8] };
            zone.Type = 2; zone.Num = jump.Zone; zone.Info = new int[8]; zone.Info[7] = 1;
            zone.X0 = (int)((b.X0 - ox) * 512); zone.X1 = (int)((b.X1 - ox) * 512) - 1;
            zone.Z0 = (int)((b.Z0 - oz) * 512); zone.Z1 = (int)((b.Z1 - oz) * 512) - 1;
            zone.Y0 = y - 300; zone.Y1 = y + 900;
            model.Zones.Add(zone);
        }
        var controller = SceneOps.BlankActor(SceneGame.Lba2, (int)((jump.StartX - ox) * 512), y, (int)((jump.StartZ - oz) * 512), entity: 16);
        controller.Life = new byte[] { 0 }; controller.Track = new byte[] { 0 };
        controller.Flags = InvisibleNoShadow; controller.Body = -1; controller.Armor = 51; controller.LifePoints = -1; controller.CoulObj = 4;
        var index = SceneOps.AddActor(model, controller);

        // the turn window: 640 units (56 degrees) either side of the road's heading
        var lo = ((jump.Beta - 640) % 4096 + 4096) % 4096; var hi = (jump.Beta + 640) % 4096;
        var window = lo < hi ? $"{lo} < beta_obj(0) && {hi} > beta_obj(0)" : $"{lo} < beta_obj(0) || {hi} > beta_obj(0)";
        const int startLabel = 90, endLabel = 91;
        var life = $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if (12 == comportement_hero() && {jump.Zone} == zone_obj(0))
    {{
        if ({window})
        {{
            set_dir_obj(0, 12);
            set_track_obj(0, label_{startLabel});
            set_comportement(comportement_2);
        }}
    }}
}}

void comportement_2()
{{
    if ({endLabel} == l_track_obj(0))
    {{
        set_dir_obj(0, 13);
        set_comportement(comportement_1);
    }}
}}
";
        try
        {
            var scripts = LBAAssembler.LbaScript.SceneScripts.Load(SceneSerializer.Write(model), scene);
            var heroTrack = scripts.GetText(0, LBAAssembler.LbaScript.ScriptKind.Track).TrimEnd();
            if (heroTrack.Contains($"label({startLabel})") || heroTrack.Contains($"label({endLabel})")) { log.Add($"scene {scene}: the hero's track script already uses labels {startLabel}/{endLabel}; no jump"); return null; }
            heroTrack += $@"

label({startLabel});
beta({jump.Beta});
anim({jump.Anim});
wait_anim();
anim(0);

label({endLabel});
stop();
";
            scripts.SetText(0, LBAAssembler.LbaScript.ScriptKind.Track, heroTrack);
            scripts.SetText(index, LBAAssembler.LbaScript.ScriptKind.Life, life);
            var built = scripts.Build();
            if (!built.Ok) { foreach (var e in built.Errors) log.Add($"scene {scene}: jump script: {e}"); return null; }
            log.Add($"scene {scene}: jump added -- {jump.Boxes.Count} zone boxes numbered {jump.Zone}, controller actor {index}, hero track labels {startLabel}/{endLabel}, heading turn {jump.Beta}");
            return SceneSerializer.Parse(SceneGame.Lba2, built.Record!);
        }
        catch (Exception error) when (error is LBAAssembler.LbaScript.ScriptCompileException or InvalidDataException or ArgumentException)
        {
            log.Add($"scene {scene}: the jump could not be built: {error.Message}");
            return null;
        }
    }
}
