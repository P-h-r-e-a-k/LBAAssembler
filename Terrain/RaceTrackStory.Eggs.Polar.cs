using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Polar dream's easter eggs (the user, 2026-11-04, of the proposal's picks: "Let's make all those changes"; the variables:
// RaceTrackStory.Eggs.cs).
//   - The meca-penguin crosses (the proposal: "on the straight after the start, a penguin waddles across the road, wind-up key turning on its
//     back. Hit it and it bounces off squeaking"): LBA1's Meca-Penguin as LBA2 has it (entity 14, the one Twinsen throws: its walk is its
//     animation 1) on a short track across the start's straight and back, again and again. A car that comes at it: its squeak (the
//     penguin's own sample) and it is knocked back to the roadside, out of the car's way (a body on the road stops a car dead).
//   - Sendell's light (the proposal: "at the top of the loop, a faint blue glow only reachable if the car takes the loop flat out. Driving
//     through it, a chime; at the white fade Sendell's line changes"): a glowing orb (the raw sprite 30) over the outer edge of the raised
//     road round the rocky peak, at its northernmost point -- Sendell's peak ("the loop": the dream's track has no vertical loop, and its
//     plan calls this one so); a car carried out to the rail there by its speed drives through it -- the racing line keeps 2.9 cells
//     inside the road's middle there and passes 5 cells from it -- a chime (the game's found-an-object sound) and EggSendell 1 (the
//     race-track mode picks the dream's waking text by it).
internal static partial class RaceTrackStory
{
    // the penguin: its entity (BODY 25, two animations: 0 standing, 1 walking), how far past the start line its crossing is (cells along the
    // straight), how far either side of the road's middle it walks to (cells: the kerbs' outer edge -- the straight is a causeway over the
    // sea there, its built ground no wider than the road and its verges), and how near a car comes before it's knocked aside (units), its
    // squeak (SAMPLE_PINGOUIN_ACTIF, COMMON.H)
    private const int PenguinEntity = 14, PenguinWalk = 1, PenguinNear = 900, PenguinSqueak = 11;
    private const double PenguinAlong = 18, PenguinSide = 4.5;
    // Sendell's light: the raw sprite (SPRIRAW 30, a blue-green glowing orb), how far out of the road's middle (cells: the raised road's
    // rail is 3.25 out), how high over the deck, how near the car comes, the chime (SAMPLE_FOUND_OBJ)
    private const int GlowSprite = 30, GlowOver = 300, GlowReach = 700, Chime = 6;
    private const double GlowOut = 2.5;

    // `report`: Polar Island's track (nothing for any other island's). Called by RaceTrackService.Story.
    internal static List<string> PolarEggs(string gameDirectory, RaceTrackReport report, RaceTrackIsland island)
    {
        var log = new List<string>();
        if (island != RaceTrackIsland.Polar) return log;
        if (report.StartLine.Count == 0 || report.LapX.Length < 10 || report.LapY.Length != report.LapX.Length) { log.Add("the Polar dream's eggs left out: no track"); return log; }
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        int SceneAt(double x, double z)
        {
            foreach (var s in island.Scenes)
            {
                if (!store.SceneExists(s)) continue;
                var m = store.Load(s);
                if (m.CubeMode == 1 && m.CubeX == (int)Math.Floor(x / 64) && m.CubeY == (int)Math.Floor(z / 64)) return s;
            }
            return -1;
        }

        // the meca-penguin: across the straight, PenguinAlong cells past the start line
        var start = report.StartLine[0];
        double px = start.X + start.DirX * PenguinAlong, pz = start.Z + start.DirZ * PenguinAlong;
        // (at the road's height there: the lap's nearest point's)
        var road = report.LapY[Enumerable.Range(0, report.LapX.Length).MinBy(i => Math.Pow(report.LapX[i] - px, 2) + Math.Pow(report.LapZ[i] - pz, 2))];
        (double X, double Z, double Y) Side(double sign) => (px - start.DirZ * PenguinSide * sign, pz + start.DirX * PenguinSide * sign, road);
        var (a, b) = (Side(1), Side(-1));
        var scene = SceneAt(px, pz);
        if (scene < 0 || SceneAt(a.X, a.Z) != scene || SceneAt(b.X, b.Z) != scene) log.Add($"the meca-penguin left out: its crossing at cell ({px:0.#}, {pz:0.#}) isn't in one scene");
        else GroupB.EditScene(store, scene, log, $"the meca-penguin waddles across the straight after the start, from cell ({a.X:0.#}, {a.Z:0.#}) to ({b.X:0.#}, {b.Z:0.#}), squeaking when a car comes at it", model =>
        {
            int pa = SceneOps.AddTrackPoint(model, new SceneTrackPoint(RaceTrackScenes.CubeLocal(a.X, model.CubeX), (int)Math.Round(a.Y), RaceTrackScenes.CubeLocal(a.Z, model.CubeY)));
            int pb = SceneOps.AddTrackPoint(model, new SceneTrackPoint(RaceTrackScenes.CubeLocal(b.X, model.CubeX), (int)Math.Round(b.Y), RaceTrackScenes.CubeLocal(b.Z, model.CubeY)));
            var penguin = SceneOps.BlankActor(SceneGame.Lba2, RaceTrackScenes.CubeLocal(a.X, model.CubeX), (int)Math.Round(a.Y), RaceTrackScenes.CubeLocal(a.Z, model.CubeY), PenguinEntity);
            // (on its track: MOVE_TRACK; on the ground and over the kerbs, no scene zones)
            // (and RaceProp: the race-track mode's roadwatch leaves it where it is -- else it takes it out of sight once the race is on)
            penguin.Move = 3; penguin.Flags = 0x0802 | RaceProp; penguin.SRot = 300;
            penguin.Life = new byte[] { 0 }; penguin.Track = new byte[] { 0 };
            var index = SceneOps.AddActor(model, penguin);
            return scripts =>
            {
                scripts.SetText(index, ScriptKind.Life, $@"void comportement_0()
{{
    if ({PenguinNear} > distance(0) && {Driving} == comportement_hero())
    {{
        sample({PenguinSqueak});
        pos_point({pa});
        set_track(label_0);
    }}
}}
");
                scripts.SetText(index, ScriptKind.Track, $@"label(0);
anim({PenguinWalk});
goto_point({pb});
goto_point({pa});
goto(label_0);
");
            };
        });

        // Sendell's light: the raised road round the rocky peak (the lap's longest raised stretch, out of the jumps), at its northernmost
        // point, out over its rail
        var raised = report.LapRaised is { } r && r.Length == report.LapX.Length ? r : null;
        if (raised is null) { log.Add("Sendell's light left out: no raised road"); return log; }
        var loop = new List<int>();
        for (int i = 0, n = report.LapX.Length; i < n;)
        {
            if (!raised[i] || (report.LapArc?[i] ?? false)) { i++; continue; }
            var run = new List<int>();
            while (i < n && raised[i] && !(report.LapArc?[i] ?? false)) run.Add(i++);
            if (run.Count > loop.Count) loop = run;
        }
        if (loop.Count < 20) { log.Add("Sendell's light left out: no raised road round the peak"); return log; }
        var top = loop.MinBy(i => report.LapZ[i]);
        // (out: away from the loop's middle, its points' average)
        double cx = loop.Average(i => report.LapX[i]), cz = loop.Average(i => report.LapZ[i]);
        double ox = report.LapX[top] - cx, oz = report.LapZ[top] - cz, ol = Math.Sqrt(ox * ox + oz * oz);
        double gx = report.LapX[top] + ox / ol * GlowOut, gz = report.LapZ[top] + oz / ol * GlowOut;
        var gy = (int)Math.Round(report.LapY[top]) + GlowOver;
        var glowScene = SceneAt(gx, gz);
        if (glowScene < 0) { log.Add($"Sendell's light left out: no scene at cell ({gx:0.#}, {gz:0.#})"); return log; }
        GroupB.EditScene(store, glowScene, log, $"Sendell's light over the outer rail of the raised road round the peak, at cell ({gx:0.#}, {gz:0.#}), {gy} (variable {EggSendell} 1)", model =>
            GroupB.Add(model, RoadSprite(RaceTrackScenes.CubeLocal(gx, model.CubeX), gy, RaceTrackScenes.CubeLocal(gz, model.CubeY), GlowSprite), $@"void comportement_0()
{{
    if ({GlowReach} > distance(0) && {Driving} == comportement_hero())
    {{
        set_var_game({EggSendell}, 1);
        sample({Chime});
        suicide();
    }}
}}
"));
        return log;
    }
}
