using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Polar dream's easter eggs (the user, 2026-11-04, of the proposal's picks: "Let's make all those changes"; the variables:
// RaceTrackStory.Eggs.cs).
//   - (taken out 2026-11-15 at the user's word) The meca-penguin crossed (the proposal: "on the straight after the start, a penguin waddles across the road, wind-up key turning on its
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

        // (the meca-penguin that crossed the straight after the start taken out -- the user, 2026-11-15: "Remove the meca penguin \"Easter
        // egg\" from the Polar track please")

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
