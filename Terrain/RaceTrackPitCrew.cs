namespace LBAAssembler.Terrain;

// Zoe in the pit box (docs/LBRA_STORY_SCRIPT.md, "Twinsen and Zoe": "In a race she stands in the pit box by the start line, with a
// toolbox. Driving slowly through the pit box beside her (under 20 km/h) is a pit stop: the item box is filled with a fuel can, a jolt
// shaken off, and Zoe says one short line. Once a lap at most." -- 2026-11-11). The engine draws her (RACEMOD.CPP pit_crew=, PitCrew,
// DrawPitCrew) as it draws the flag marshal; this is where she stands: beside the road After cells past the start line, on the side away
// from the flag marshal's plinth (the marshal keeps away from the pit spots, so this is the pits' side where the track has them), just off
// the road's edge -- where nothing of the road is any more (the report's InTheWay: its real edge and a cell more) -- on the ground there,
// or, with no ground about level with the line beside it (a raised road), on the road's own edge, on its deck. Facing the cars coming,
// turned to the road.
internal static class RaceTrackPitCrew
{
    // the house's Zoe (entity 15): her body 0, standing (0), and the gesture she waves the car in with
    public const int ZoeEntity = 15, ZoeBody = 0, Idle = 0, Wave = 87;
    private const double After = 4, FarthestOut = 9, Step = 0.25, EdgeOut = 0.45, GroundBelow = 700, TurnToRoad = 25 * Math.PI / 180;
    // (how tall she stands, world units, for InTheWay)
    private const double Tall = 1600;
    // what she says at a stop (the race's banner)
    public const string Line = "Zoe: Fuel's in, Twinsen. Go, go, go!";

    // The race file's pit_crew= numbers: her entity, body, idle and wave animations, where she stands (island units: x, z, the floor) and the
    // way she faces -- or null (no start line).
    public static int[]? Place(RaceTrackReport report)
    {
        if (report.StartLine.Count == 0) return null;
        var line = report.StartLine[0];
        var dl = Math.Sqrt(line.DirX * line.DirX + line.DirZ * line.DirZ);
        if (dl < 1e-9) return null;
        double dx = line.DirX / dl, dz = line.DirZ / dl;
        // (the marshal's side -- a spot `o` out to side s is line - (dz, -dx) * s * o, as TempleMarshal.Place has it -- and hers the other)
        var side = 1;
        if (report.Marshal is { Length: >= 10 } m)
        {
            var across = (m[6] / 512.0 - line.X) * -dz + (m[7] / 512.0 - line.Z) * dx;
            side = across > 0 ? -1 : 1;
        }
        double ax = line.X + dx * After, az = line.Z + dz * After;
        (double X, double Z) At(double o) => (ax - dz * side * o, az + dx * side * o);
        for (var o = Step; o <= FarthestOut; o += Step)
        {
            var (x, z) = At(o);
            if (report.InTheWay(x, z, line.Y - 200, line.Y + Tall)) continue;
            // (the first place clear of the road: the ground there about level with the line, or the road raised and she on its edge)
            if (report.GroundAfter?.Invoke(x, z) is { } g && g <= line.Y + 200 && g >= line.Y - GroundBelow)
                return Numbers(x, z, g, dx, dz, side);
            var edge = Math.Max(0, o - 1 - EdgeOut);
            var (ex, ez) = At(edge);
            return Numbers(ex, ez, report.RaisedFloor?.Invoke(ex, ez, line.Y) ?? line.Y, dx, dz, side);
        }
        var (fx, fz) = At(FarthestOut);
        return Numbers(fx, fz, report.GroundAfter?.Invoke(fx, fz) ?? line.Y, dx, dz, side);
    }

    private static int[] Numbers(double x, double z, double y, double dx, double dz, int side)
    {
        var beta = (int)Math.Round((Math.Atan2(-dx, -dz) - side * TurnToRoad) * 4096 / (2 * Math.PI)) & 4095;
        return new[] { ZoeEntity, ZoeBody, Idle, Wave, (int)Math.Round(x * 512), (int)Math.Round(z * 512), (int)Math.Round(y), beta };
    }

    // Whether a story race has her: from chapter 2 (the storm track's race is chapter 1's, the dream's chapter 0's), not the Elevator
    // Platform's head-to-head (no pit box)
    public static bool InStory(string? island, bool dream) =>
        !dream && island is not null && island != "Citadel Island, storm track" && island != "Elevator Platform";
}
