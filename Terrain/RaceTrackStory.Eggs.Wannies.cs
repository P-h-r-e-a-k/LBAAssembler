using System.IO;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Wannies Island's easter eggs (chapter 9; the user, 2026-11-04: "Let's make all those changes", of the proposal's picks).
//
// ---- kiss the frog (the proposal's "Action at the frog's open mouth without the glove quest on: Twinsen: 'Should I...?' Zoe: 'Don't you
// dare.' Do it in the race chapter after the glove's found: the frog blushes (its colour flashes) and keeps its mouth open for one lap"):
// an invisible talker at the giant frog's mouth (the race-track mode's frog=: its mouth's place, where the glove is too) -- on foot, Action
// there, Twinsen's line and Zoe's. The blush and the lap with the mouth held open are the engine's to do: the mouth opens and shuts on the
// race-track mode's own clock (RACEMOD.CPP FrogPhase: TimerRefHR modulo the period, FrogOpen, FrogPose), which reads no game variable.
//
// ---- heigh-ho (the proposal's "after the race, the Wannie miners walk off to the mine in a line, whistling. The miner, last in line: 'Off
// to work. Somebody has to dig the next tunnel.'"): three Wannies of the game's (scene 96's miner, copied) stand in a line near the
// elevator's foot, where Twinsen arrives, in the start line's scene (where the race ends), the way to the mine's door (scene 99's way into
// the mine, 122) in front of them -- or, where the pier's machinery is in that way, the nearest way off with room. The race won, Twinsen out
// of his car in their scene (the after-race step, GroupB.AfterRace: the fragment, the old Wannie's clue), they walk off in a line -- the
// first whistling (SAMPLES.HQR WhistleSample) -- the camera on the last, who says his line; a few seconds on, the tunnel to Mosquibees
// Island as before (the after-race step's change_cube, moved to the end of this). Twinsen stands still meanwhile (his scene could change
// under him otherwise, and with it the walk's end). Elsewhere the after-race step is as it was.
internal static partial class RaceTrackStory
{
    // the mine's door (scene 99's zone into scene 122: its middle, island cells)
    private static readonly (double X, double Z) MineDoor = (547.5, 544.0);
    // (a whistle-like sample of the game's: a pure tone rising from 1.3 to 2.8 kHz over 0.8 s -- found by its pitch, not heard)
    private const int WhistleSample = 24;
    // the walk started (1) and under way (2): a variable of the miners' scene
    private const int HeighHoCube = 62;
    private const int Miners = 3;
    // (cells) between the miners in the line; the walk at the most; its first leg; how far off the roads they keep
    private const double MinerGap = 1.6, WalkMost = 14, WalkFirst = 5, MinWalk = 6, WalkLong = 10, MinerClear = 1, MinerAway = 3, DecorGap = 0.6, SearchMost = 30;

    // The miners' scene, their places (in line, the first first) and each one's two points of the walk (island cells and heights)
    private sealed record HeighHoLine(int Scene, (double X, double Z, double Y, int Beta)[] Miners, (double X, double Z, double Y)[][] Way);

    // Where the miners stand and walk: in the scene of the elevator's foot (WanniesArrival), off the roads, on the ground, clear of the
    // island's decor (the pier's machinery: the ground's heights don't know it), the way to the mine's door -- or as near that way as
    // there's room for. Null (and why in the log) when there's no room.
    private static HeighHoLine? HeighHoPlace(string gameDirectory, SceneStore store, SceneStore originals, RaceTrackIsland island, RaceTrackReport report, List<string> log)
    {
        if (report.GroundAfter is not { } ground) { log.Add("heigh-ho (an easter egg) left out: no ground"); return null; }
        IslandFile file;
        try { file = IslandFile.Load(Path.Combine(gameDirectory, island.IleFile)); }
        catch (Exception e) when (e is IOException or InvalidDataException) { log.Add($"heigh-ho (an easter egg) left out: {e.Message}"); return null; }
        var arrival = originals.Load(WanniesArrival);
        double ax = arrival.CubeX * 64 + arrival.Hero.X / 512.0, az = arrival.CubeY * 64 + arrival.Hero.Z / 512.0;
        var toMine = Math.Atan2(MineDoor.X - ax, MineDoor.Z - az);
        (int X, int Z) home = report.StartLine.Count > 0 ? ((int)Math.Floor(report.StartLine[0].X / 64), (int)Math.Floor(report.StartLine[0].Z / 64)) : (arrival.CubeX, arrival.CubeY);
        // (the glove's miner's place, as ApplyWannies finds it: they keep MinerAway from him)
        var glove = GroupB.SpotAt(store, island, report, ax + 2, az + 2, arrival.Hero.Y, ax, az)?.At;
        int cubeX = int.MinValue, cubeZ = int.MinValue;
        double hy = 0, boxesY = 0;     // (the head's ground; the height the boxes were gathered for)
        // (the decors' boxes that stand in the way of someone on the ground there, in the head's cube: island cells)
        var boxes = new List<(double X0, double Z0, double X1, double Z1)>();
        void Boxes()
        {
            boxes.Clear();
            if (file.CubeAt(cubeX, cubeZ) is not { } cube) return;
            foreach (var d in cube.Decors.Where(d => d.YMax > d.YMin && d.YMax > hy + 100 && d.YMin < hy + 1500))
                boxes.Add((cubeX * 64 + d.XMin / 512.0, cubeZ * 64 + d.ZMin / 512.0, cubeX * 64 + d.XMax / 512.0, cubeZ * 64 + d.ZMax / 512.0));
        }
        // (a place they can be: the head's cube, not by its edge, off the roads, the ground near the head's, in no decor's box, not by the
        // glove's miner)
        bool Fits(double x, double z)
        {
            if ((int)Math.Floor(x / 64) != cubeX || (int)Math.Floor(z / 64) != cubeZ) return false;
            double cx = x - cubeX * 64, cz = z - cubeZ * 64;
            if (cx < 3 || cx > 61 || cz < 3 || cz > 61) return false;
            if (glove is { } g && (x - g.X) * (x - g.X) + (z - g.Z) * (z - g.Z) < MinerAway * MinerAway) return false;
            if (boxes.Any(b => x > b.X0 - DecorGap && x < b.X1 + DecorGap && z > b.Z0 - DecorGap && z < b.Z1 + DecorGap)) return false;
            return OffRoads(report, x, z) >= MinerClear && Math.Abs(ground(x, z) - hy) <= 300;
        }
        // (the way they go: the mine's first, then turned from it either side by turns, a twelfth of a turn at a time; the line's head the
        // nearest place to the elevator's foot -- a few cells along the way: the glove's miner is at its foot -- where all of them stand
        // and have WalkLong cells on along it at the least -- or MinWalk, where none has -- searched outward ring by ring, half a cell at a
        // time)
        foreach (var least in new[] { WalkLong, MinWalk })
        foreach (var turn in new[] { 0, 1, -1, 2, -2, 3, -3 })
        {
            double way = toMine + turn * Math.PI / 6, dx = Math.Sin(way), dz = Math.Cos(way);
            double Walk(double hx, double hz)
            {
                var walk = -0.5;
                for (var d = 0.0; d <= WalkMost; d += 0.5)
                {
                    if (!Enumerable.Range(0, Miners).All(i => Fits(hx + dx * (d - MinerGap * i), hz + dz * (d - MinerGap * i)))) break;
                    walk = d;
                }
                return walk;
            }
            double bx = ax + dx * 4, bz = az + dz * 4;
            for (var r = 0.0; r <= SearchMost; r += 0.5)
            {
                var steps = r == 0 ? 1 : (int)Math.Ceiling(2 * Math.PI * r / 0.5);
                for (var k = 0; k < steps; k++)
                {
                    double a = 2 * Math.PI * k / steps, hx = bx + r * Math.Sin(a), hz = bz + r * Math.Cos(a);
                    var (cx, cz, gy) = ((int)Math.Floor(hx / 64), (int)Math.Floor(hz / 64), ground(hx, hz));
                    // (in the start line's cube -- where the race ends: the after-race step there -- on ground over the sea's)
                    if (gy < 200 || cx != home.X || cz != home.Z) continue;
                    hy = gy;
                    if (cx != cubeX || cz != cubeZ || Math.Abs(gy - boxesY) > 200) { (cubeX, cubeZ, boxesY) = (cx, cz, gy); Boxes(); }
                    var walk = Walk(hx, hz);
                    if (walk < least) continue;
                    var scene = GroupB.OutsideScene(store, island, hx, hz);
                    if (scene < 0) continue;
                    var beta = RaceTrackMath.Turn(Math.Atan2(dx, dz));
                    var line = Enumerable.Range(0, Miners).Select(i => (hx - dx * MinerGap * i, hz - dz * MinerGap * i)).Select(p => (p.Item1, p.Item2, ground(p.Item1, p.Item2), beta)).ToArray();
                    var points = Enumerable.Range(0, Miners).Select(i => new[] { Math.Min(walk, WalkFirst), walk }.Select(d =>
                    {
                        double x = hx + dx * (d - MinerGap * i), z = hz + dz * (d - MinerGap * i);
                        return (x, z, ground(x, z));
                    }).ToArray()).ToArray();
                    return new HeighHoLine(scene, line, points);
                }
            }
        }
        log.Add($"heigh-ho (an easter egg) left out: no room for the miners in a line with a way off near the elevator's foot ({ax:0.#}, {az:0.#})");
        return null;
    }

    // The after-race step's `won` in a scene: in the miners', their walk at its end instead of the tunnel at once
    private static string HeighHoWon(string won, HeighHoLine? h, int scene) =>
        h is null || h.Scene != scene ? won : won.Replace($"change_cube({MosquibeesArrival});", $"set_var_cube({HeighHoCube}, 1);");

    // The miners, their walk and the last one's line (`offToWork`), in their scene
    private static void HeighHo(SceneStore store, SceneStore originals, HeighHoLine? h, int offToWork, List<string> log)
    {
        if (h is null) return;
        var miner = Copy(originals, WannieScene, MinerActor);
        if (miner is null) { log.Add("heigh-ho (an easter egg) left out: the game's Wannie miner (scene 96) not found"); return; }
        GroupB.EditScene(store, h.Scene, log, $"heigh-ho (an easter egg): {h.Miners.Length} Wannie miners in a line at cell ({h.Miners[0].X:0.#}, {h.Miners[0].Z:0.#}), " +
            $"walking off towards the mine after the race; the last one's line {offToWork}, the whistle sample {WhistleSample}", model =>
        {
            var edits = new List<Action<SceneScripts>>();
            var last = model.Actors.Count + h.Miners.Length - 1;
            for (var i = 0; i < h.Miners.Length; i++)
            {
                var m = miner.Clone();
                Place(m, model, h.Miners[i]);
                var first = model.TrackPoints.Count;
                foreach (var (x, z, y) in h.Way[i])
                    model.TrackPoints.Add(new SceneTrackPoint(RaceTrackScenes.CubeLocal(x, model.CubeX), (int)Math.Round(y), RaceTrackScenes.CubeLocal(z, model.CubeY)));
                // (the first whistles as they set off and again on the way)
                var whistle = i == 0 ? $"sample({WhistleSample});\n" : "";
                edits.Add(AddWithTrack(model, m, $@"void comportement_0()
{{
    if ({WanniesChapter} == chapter())
    {{
        set_comportement(comportement_1);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
    if (0 < var_cube({HeighHoCube}))
    {{
        set_track(label_10);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
}}
", $@"label(0);
anim(0);
stop();

label(10);
anim(1);
{whistle}goto_point({first});
{whistle}goto_point({first + 1});
anim(0);

label(11);
stop();
"));
            }
            // (the walk's clock -- the camera on the last of them, his line, and the tunnel -- an invisible helper by them)
            var clock = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(h.Miners[0].X, model.CubeX), (int)Math.Round(h.Miners[0].Y),
                RaceTrackScenes.CubeLocal(h.Miners[0].Z, model.CubeY), 9);
            edits.Add(AddWithTrack(model, clock, $@"void comportement_0()
{{
    if ({WanniesChapter} == chapter())
    {{
        set_comportement(comportement_1);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
    if (1 == var_cube({HeighHoCube}))
    {{
        set_var_cube({HeighHoCube}, 2);
        set_dir_obj(0, NO_MOVE);
        cam_follow({last});
        set_track(label_10);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if (11 == l_track())
    {{
        message_obj({last}, {offToWork});
        set_track(label_12);
        set_comportement(comportement_3);
    }}
}}

void comportement_3()
{{
    if (13 == l_track())
    {{
        cam_follow(0);
        set_dir_obj(0, MOVE_MANUAL);
        change_cube({MosquibeesArrival});
    }}
}}
", @"label(0);
stop();

label(10);
wait_nb_second(3);

label(11);
stop();

label(12);
wait_nb_second(4);

label(13);
stop();
"));
            return scripts => { foreach (var e in edits) e(scripts); };
        });
    }

    // An actor added to a scene with a life and a track script of its own (GroupB.Add's, with a track)
    private static Action<SceneScripts> AddWithTrack(SceneModel model, SceneActorModel actor, string life, string track)
    {
        actor.Life = new byte[] { 0 }; actor.Track = new byte[] { 0 };
        var index = SceneOps.AddActor(model, actor);
        return scripts =>
        {
            scripts.SetText(index, ScriptKind.Life, life);
            scripts.SetText(index, ScriptKind.Track, track);
        };
    }

    // Kiss the frog: a talker at the frog's mouth while chapter 9 is on (`when`), Twinsen's line and Zoe's
    private static void KissTheFrog(SceneStore store, RaceTrackIsland island, RaceTrackReport report, string when, int should, int dare, List<string> log)
    {
        if (report.Frogs.Count == 0) { log.Add("kiss the frog (an easter egg) left out: no frog on the lap"); return; }
        var frog = report.Frogs[0];
        var scene = GroupB.OutsideScene(store, island, frog.X, frog.Z);
        if (scene < 0) { log.Add("kiss the frog (an easter egg) left out: the frog is in no outside scene"); return; }
        GroupB.EditScene(store, scene, log, $"kiss the frog (an easter egg): Action at the frog's mouth, cell ({frog.X:0.#}, {frog.Z:0.#}): lines {should} and {dare}", model =>
        {
            var talker = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(frog.X, model.CubeX), (int)Math.Round(frog.Y), RaceTrackScenes.CubeLocal(frog.Z, model.CubeY), 9);
            return GroupB.Add(model, talker, Talker(when, $@"            message_obj(0, {should});
            message_zoe({dare});", 2000));
        });
    }
}
