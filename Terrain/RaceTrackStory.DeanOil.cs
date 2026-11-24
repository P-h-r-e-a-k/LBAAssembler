using System.Numerics;
using LbaBodyStudio;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Dean's oil on the Desert's road by the School of Magic (the user, 2026-11-17: "the dean currently has closed the race track, but
// there's nothing stopping us driving on it. Let's update the story line so there's a massive stretch of oil on the track around the school
// of magic making it unsafe to drive, the dean has put it there after he got angry his pages were lost. Speaking to the Dean has him
// begrudgingly say he'll remove it if his papers are returned"): while Q_PAGES is under the diploma's 6, the road DeanOilReach either way
// along the lap from the School's door is under oil -- a black, glossy strip over it (a body of its own, the prop entity's DeanOilBody,
// built along the lap's centre line on the ground there: scenes 61 and 201, cube (8, 9)), and the engine's oil_patch= over the same stretch
// of the racing line, where Twinsen's car skids, and skids again. Gone with the diploma (its actor's suicide()).
internal static partial class RaceTrackStory
{
    private const double SchoolDoorWX = 8 * 32768 + 9472, SchoolDoorWZ = 9 * 32768 + 10752, DeanOilReach = 7000;
    private const int DeanOilBody = 177, DeanOilHalf = 1250, DeanOilPatchHalf = 1400, DeanOilLift = 14, DeanOilCubeX = 8, DeanOilCubeZ = 9;
    private const int OilBlack = 224, OilSheen = 50;

    // The stretch of a line of world points nearest the School's door: from DeanOilReach before it to DeanOilReach after, along the line
    private static List<(double X, double Z, double Y)> DeanOilStretch(IReadOnlyList<(double X, double Z, double Y)> line)
    {
        var stretch = new List<(double, double, double)>();
        if (line.Count < 3) return stretch;
        var nearest = Enumerable.Range(0, line.Count).OrderBy(i => (line[i].X - SchoolDoorWX) * (line[i].X - SchoolDoorWX) + (line[i].Z - SchoolDoorWZ) * (line[i].Z - SchoolDoorWZ)).First();
        int from = nearest, to = nearest;
        double back = 0, ahead = 0;
        while (back < DeanOilReach && from > 0) { back += Dist(line[from - 1], line[from]); from--; }
        while (ahead < DeanOilReach && to < line.Count - 1) { ahead += Dist(line[to], line[to + 1]); to++; }
        for (var i = from; i <= to; i++) stretch.Add(line[i]);
        return stretch;
        static double Dist((double X, double Z, double Y) a, (double X, double Z, double Y) b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
    }

    // The race file's oil_patch= (the Desert's, in the story): the racing line's stretch, every other point at most 60
    internal static string? DeanOilLine(RaceTrackService.TrackInfo info)
    {
        if (RaceTrackIsland.ByName(info.Island) != RaceTrackIsland.Desert || info.Path is not { Count: > 3 } path) return null;
        var stretch = DeanOilStretch(path.Where(p => p.Length >= 3).Select(p => ((double)p[0], (double)p[1], (double)p[2])).ToList());
        if (stretch.Count < 2) return null;
        var step = Math.Max(1, (stretch.Count + 59) / 60);
        var points = stretch.Where((_, i) => i % step == 0 || i == stretch.Count - 1).Select(p => $"{p.X:0} {p.Z:0} {p.Y:0}");
        return $"oil_patch={QPages} {PagesDiploma} {DeanOilPatchHalf} {string.Join(' ', points)}\n";
    }

    private static void DeanOil(string gameDirectory, IslandBeatsA b, RaceTrackReport report, List<string> log)
    {
        if (report.LapX.Length < 10 || report.GroundAfter is not { } ground) { log.Add("the Dean's oil left out: no lap"); return; }
        var lap = Enumerable.Range(0, report.LapX.Length).Select(i => (report.LapX[i] * 512, report.LapZ[i] * 512, ground(report.LapX[i], report.LapZ[i]))).ToList();
        var stretch = DeanOilStretch(lap);
        if (stretch.Count < 2) { log.Add("the Dean's oil left out: no stretch of the lap by the School"); return; }
        // (at most 130 points of the line: the body's points two a point, under the engine's 550 with both faces of each strip)
        var step = Math.Max(1, (stretch.Count + 129) / 130);
        stretch = stretch.Where((_, i) => i % step == 0 || i == stretch.Count - 1).ToList();
        var mid = stretch[stretch.Count / 2];
        double ox = DeanOilCubeX * 32768.0, oz = DeanOilCubeZ * 32768.0;
        // (on an entity of its own -- CitadelProps.InstallOwn, 2026-11-24)
        var (oilEntity, oilLog) = Citadel.CitadelProps.InstallOwn(gameDirectory, "the Dean's oil", new (int, string, Func<byte[], Body>, short[]?)[]
        {
            (DeanOilBody, "the Dean's oil over the road", header => BuildDeanOil(header, stretch, mid), null),
        });
        log.Add(oilLog);
        foreach (var scene in new[] { SchoolOutside, SchoolOutsideToo })
            if (b.Store.SceneExists(scene))
                b.Edit(scene, $"the Dean's oil over the road by the School, {stretch.Count} points of the lap, while Q_PAGES is under {PagesDiploma}", model =>
                {
                    var oil = Citadel.CitadelProps.Actor(DeanOilBody, (int)Math.Round(mid.X - ox), (int)Math.Round(mid.Y + DeanOilLift), (int)Math.Round(mid.Z - oz), 0);
                    if (oilEntity >= 0) oil.Entity = oilEntity;
                    // (walked and driven over -- and, its armour the walk-over one, drawn before every other actor outside, SORT.CPP
                    // SortFloorFirst: by its box alone, the strip's, it came after the car on it and was drawn over it -- the user, 2026-11-18,
                    // "It also draws over the top of the car". In the Z-buffer instead, OBJ_ZBUFFER, it cut through the car's wheels.)
                    oil.Armor = WalkOverArmour;
                    return IslandBeatsA.Add(model, oil, $"void comportement_0()\n{{\n    if ({PagesDiploma} <= var_game({QPages}))\n    {{\n        suicide();\n    }}\n}}\n");
                });
        log.Add($"the Dean's oil: {stretch.Count} points of the lap by the School, from ({stretch[0].X / 512:0.#}, {stretch[0].Z / 512:0.#}) to ({stretch[^1].X / 512:0.#}, {stretch[^1].Z / 512:0.#})");
    }

    // The strip: the line's points, each a pair across the road DeanOilHalf either side, over the ground there; its patches black, a few
    // greyer for the sheen
    private static Body BuildDeanOil(byte[] header, List<(double X, double Z, double Y)> line, (double X, double Z, double Y) mid)
    {
        var m = new CarMesh("the Dean's oil", new[] { -1 });
        m.Light = 1.0f;
        Vector3 Local(double x, double z, double y) => new((float)(x - mid.X), (float)(y - mid.Y), (float)(z - mid.Z));
        var left = new List<Vector3>();
        var right = new List<Vector3>();
        for (var i = 0; i < line.Count; i++)
        {
            var a = line[Math.Max(0, i - 1)];
            var c = line[Math.Min(line.Count - 1, i + 1)];
            double dx = c.X - a.X, dz = c.Z - a.Z, len = Math.Sqrt(dx * dx + dz * dz);
            if (len < 1e-6) { dx = 1; dz = 0; len = 1; }
            double nx = -dz / len, nz = dx / len;
            // (the edges wavy: a puddle, not a painted band)
            var wobble = 1 + 0.12 * Math.Sin(i * 1.7) + 0.08 * Math.Sin(i * 0.6 + 1);
            var p = line[i];
            left.Add(Local(p.X + nx * DeanOilHalf * wobble, p.Z + nz * DeanOilHalf * wobble, p.Y));
            right.Add(Local(p.X - nx * DeanOilHalf * (2 - wobble), p.Z - nz * DeanOilHalf * (2 - wobble), p.Y));
        }
        for (var i = 0; i + 1 < line.Count; i++)
            m.Plate(0, i % 5 == 2 ? OilSheen : OilBlack, left[i], left[i + 1], right[i + 1], right[i]);
        return m.ToBody(header);
    }
}
