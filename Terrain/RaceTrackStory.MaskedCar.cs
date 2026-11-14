using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The masked driver's black car (the user, 2026-11-13: "The masked black car" -- docs/LBRA_STORY_SCRIPT.md, each chapter's cast: "The
// masked driver | the black car | past the finish"; chapter 2: "past the finish, a black car stands with its engine running. When Twinsen
// comes near, it drives off and is gone"): in each chapter that has it, on that chapter's island, a few cells past the race's start line,
// off the road beside it, facing along the lap (RaceTrackCharacterCars.Masked: Raph's car empty, black). Twinsen within MaskedReach --
// on foot or in his car -- and it pulls away along the lap, out of sight, and stays gone for the chapter (MaskedGone: the chapter it left
// in). The user, 2026-11-13: "build it everywhere the story calls for it, and spot test if possible, but no full runs".
internal static partial class RaceTrackStory
{
    private const int MaskedGone = 198, MaskedReach = 2600, MaskedDrive = 1, MaskedAway = 2, MaskedSpeed = 3200;
    private const double MaskedPast = 7, MaskedRun = 14;
    // the chapters with the black car past the finish, by the island raced in them
    private static readonly Dictionary<string, int> MaskedChapters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Citadel Island"] = 2, ["Desert island"] = 3, ["The Emerald Moon"] = 4, ["Otringal"] = 5, ["Celebration Island, lava lake"] = 6,
        ["Island of the Francos"] = 7, ["Wannies Island"] = 9, ["Mosquibees Island"] = 10, ["Volcano Island"] = 11,
    };

    internal static List<string> MaskedCar(string gameDirectory, RaceTrackReport report, RaceTrackIsland island)
    {
        var log = new List<string>();
        if (!MaskedChapters.TryGetValue(island.Name, out var chapter)) return log;
        if (report.StartLine.Count == 0) { log.Add($"the masked driver's black car left out on {island.Name}: no start line"); return log; }
        var s = report.StartLine[0];
        // (beside the road; where there's no ground for it there -- a raised road, a cliff -- on the road itself, further on: it's gone before
        // Twinsen reaches it either way)
        var at = Beside(report, s, -MaskedPast, 1.5, new[] { 1, -1 }) ?? (s.X + s.DirX * MaskedPast * 1.5, s.Z + s.DirZ * MaskedPast * 1.5, s.Y, 0);
        var b = new IslandBeatsA(gameDirectory, island, log);
        var scene = b.SceneAt(at.X, at.Z);
        if (scene < 0) { log.Add($"the masked driver's black car left out on {island.Name}: no scene at cell ({at.X:0.#}, {at.Z:0.#})"); return log; }
        b.Edit(scene, $"chapter {chapter}: the masked driver's black car past the finish (cell {at.X:0.#}, {at.Z:0.#})", model =>
        {
            int Local(double cells, int cube) => Math.Clamp(RaceTrackScenes.CubeLocal(cells, cube), 0, IslandFile.CubeSize - 1);
            var x = Local(at.X, model.CubeX); var z = Local(at.Z, model.CubeY);
            var car = SceneOps.BlankActor(SceneGame.Lba2, x, (int)Math.Round(at.Y), z, RaceTrackBaldinoCar.RacerEntity);
            car.Body = RaceTrackCharacterCars.MaskedGeneric;
            car.Beta = RaceTrackMath.Turn(Math.Atan2(s.DirX, s.DirZ));
            car.Flags = 0x1000u | RaceTrackScenes.CarrierFlag; car.Move = 3; car.LifePoints = -1; car.Armor = WalkOverArmour;
            // (its way off: along the lap from where it stands, out of sight past the cube's edge at the furthest)
            var from = SceneOps.AddTrackPoint(model, new SceneTrackPoint(x, car.Y, z));
            var away = SceneOps.AddTrackPoint(model, new SceneTrackPoint(Local(at.X + s.DirX * MaskedRun, model.CubeX), car.Y, Local(at.Z + s.DirZ * MaskedRun, model.CubeY)));
            var index = model.Actors.Count;
            var add = IslandBeatsA.Add(model, car, $@"void comportement_0()
{{
    if ({chapter} != chapter() || {chapter} == var_game({MaskedGone}))
    {{
        invisible(1);
        suicide();
    }}
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if ({MaskedReach} > distance(0))
    {{
        set_var_game({MaskedGone}, {chapter});
        set_track(label_{MaskedDrive});
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if ({MaskedAway} == l_track())
    {{
        invisible(1);
        suicide();
    }}
}}
");
            return scripts =>
            {
                add(scripts);
                scripts.SetText(index, LbaScript.ScriptKind.Track, $"label(0);\npos_point({from});\nstop();\n\nlabel({MaskedDrive});\nspeed({MaskedSpeed});\ngoto_point_3d({away});\n\nlabel({MaskedAway});\nstop();\n");
            };
        });
        b.Save();
        return log;
    }
}
