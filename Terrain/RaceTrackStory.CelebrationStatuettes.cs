using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Celebration Island's four Dark Monk statuettes, the souvenir seller's (scene 95, the lava lake's chapter -- RaceTrackStory.LavaLake): all
// four there at once, found in any order (the user, 2026-11-27: "make sure today that the 4 statues on celebration island are obtainable
// ... The two we discussed, the one on the top of the building on the book, where we'll need to add a ladder up to. The one in the middle of
// the lava with a timing puzzle for the user to jump between the ... floating blocks ... Let's add the other two in above the jumps on the
// track, so Twinsen can just drive around to collect them"; "All statues should be visible at the same time and the user should be able to
// collect them in any order"). Till then they were four on the ground by the lap, each seen only once the one before was found -- the user
// found none (2026-11-26: "I can't see them anywhere").
//   - on the temple's book (the roof: decors 2-3 of the cube, the book's south half's top 10,660): a ladder up the temple's back -- its east
//     wall, the door being on its west -- from the ground beside it (CelebrationProps.Ladder, the game's ladder zone, type 6);
//   - on a block in the lava's middle, west of the causeway (CelebrationProps.Pillar): stepping slabs from the shore -- one sinking under
//     the lava and coming up again, one sliding back and forth across the way, one sinking as the first comes up (carriers on their tracks);
//   - over the lap's second and third jumps (the raised road's gaps: the lava causeway's, and the one behind the temple), in the air where
//     the car flies -- taken driving (scene 95's road is driven outside a race: RaceCarEngineFile's _drive file).
// Each its own game variable (StatuetteVars + k: 1 found); the seller's count (Q_STALL 2-6) goes up by one each time, once he has told of
// them -- and when he tells, it starts from those found already.
internal static partial class RaceTrackStory
{
    public const int StatuetteVars = 296;
    // (the lava lake's island shown: the game's variable 79 -- the statue risen -- still 0)
    private const int StatueRisenVar = 79;
    // the temple (cube-local units): its back wall, the book's south half's top and middle; the ladder's place along the wall, over the strip
    // of ground east of it (cells z 39-41)
    private const int TempleBackX = 13088, BookTop = 10660, LadderAtZ = 20300, BookStatuetteX = 11288, BookStatuetteZ = 19700;
    // the lava's way (cube-local cells, the row z 28.5): the shore's edge at x 25; the slabs, a step apart (120: walked across as they
    // come up or slide by -- a Sporty jump does too); the block -- the causeway's deck from x 36
    private const double LavaRowZ = 28.5, SlabOneX = 26.13, SlabTwoX = 28.16, SlabThreeX = 30.2, BlockX = 32.66, SlideHalf = 2.2;
    // heights: a slab's top up and sunk (under the lava's 5,700), the block's top
    private const int SlabUp = 6000, SlabSunk = 5380, BlockTop = 6080;
    // (taken within this of Twinsen: on foot, a statuette on a top he stands on; the car's, in the air -- distance_3d)
    private const int FootTake = 900, FlightTake = 1300, FlightOver = 450;
    // (the ladder's zone number in scene 95 -- switched: echelle)
    private const int TempleLadderZone = 6;
    // a statuette turning where it stands (its track: TurningStatuette's)
    private static SceneActorModel Turning(SceneActorModel a) { a.Move = 3; return a; }

    // the seller's count when he tells of them: from those found already (LavaSeller)
    internal static string StatuettesCounted() => string.Concat(Enumerable.Range(0, 4).Select(k => $@"
                        if (1 == var_game({StatuetteVars + k}))
                        {{
                            add_var_game({QStall}, 1);
                        }}"));

    internal static void CelebrationStatuettes(string gameDirectory, SceneStore store, RaceTrackReport? report, int lava, string before, List<string> log)
    {
        int Say(LavaLine l) => lava + (int)l;
        IslandFile ile;
        try { ile = IslandFile.Load(Path.Combine(gameDirectory, RaceTrackIsland.CelebrationLava.IleFile)); }
        catch (Exception e) when (e is IOException or InvalidDataException) { log.Add($"the statuettes left out: {e.Message}"); return; }
        // a statuette's script: there while the seller isn't beaten and it isn't found; taken within `take` (driving only, for the jumps'),
        // its sound; its line once Twinsen is out of the car, and Zoe's word at the fourth
        string Life(int k, string near, bool driving) => $@"void comportement_0()
{{
    if ({before} && 0 == var_game({StatuetteVars + k}))
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
    if ({near}{(driving ? $" && {Driving} == comportement_hero()" : "")})
    {{
        set_var_game({StatuetteVars + k}, 1);
        if ({StallTold} <= var_game({QStall}) && {AllFound} > var_game({QStall}))
        {{
            add_var_game({QStall}, 1);
        }}
        sample({BonusSample});
        invisible(1);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if ({Driving} != comportement_hero())
    {{
        message({Say(LavaLine.Statuette)});
        if ({AllFound} == var_game({QStall}))
        {{
            message_zoe({Say(LavaLine.ZoeFour)});
        }}
        suicide();
    }}
}}
";
        // (a statuette turning slowly where it is: its track)
        const string TurningTrack = "label(0);\nangle(1024);\nangle(2048);\nangle(3072);\nangle(0);\ngoto(label_0);\n";
        int Ground(int x, int z) => (int)Math.Round(IslandOps.Altitude(ile, 7 * IslandFile.CubeSize + x, 7 * IslandFile.CubeSize + z) ?? 0);
        // (the island's cube for scene 95: (7,7) -- checked against the scene's own as it's edited)
        GroupB.EditScene(store, CelebrationScene, log, "the four Dark Monk statuettes, all there at once, found in any order (variables 296-299): on the temple's book (a ladder up its back), on a block in the lava (stepping slabs, sinking and sliding), over the lap's second and third jumps", model =>
        {
            if (model.CubeX != 7 || model.CubeY != 7) throw new NotTheGames($"scene {CelebrationScene} isn't cube (7,7)");
            var edits = new List<Action<SceneScripts>>();
            var lakeShown = $"0 == var_game({StatueRisenVar})";
            void Put(SceneActorModel actor, string life, string? track = null)
            {
                var index = model.Actors.Count;
                var add = GroupB.Add(model, actor, life);
                edits.Add(scripts => { add(scripts); if (track is not null) scripts.SetText(index, ScriptKind.Track, track); });
            }
            string While(string what) => $"void comportement_0()\n{{\n    if ({what})\n    {{\n    }}\n    else\n    {{\n        suicide();\n    }}\n}}\n";

            // ---- 1: the book, and the ladder up the temple's back
            var foot = Ground(TempleBackX + 300, LadderAtZ);
            var ladder = CelebrationProps.Actor(gameDirectory, CelebrationProps.Ladder, TempleBackX + 20, foot, LadderAtZ, 1024);
            ladder.Flags |= 1u << 12;   // (no shadow)
            // (walked past, not into: its box at its foot by the wall turned Twinsen aside along it as he walked up to climb, out of the zone)
            ladder.Armor = WalkOverArmour;
            // (its zone switched off with it once the statue has risen -- the lava lake's island gone: echelle)
            Put(ladder, $@"void comportement_0()
{{
    if ({lakeShown})
    {{
    }}
    else
    {{
        echelle({TempleLadderZone}, 0);
        suicide();
    }}
}}
");
            model.Zones.Add(new SceneZoneModel
            {
                Type = 6, Num = TempleLadderZone, X0 = TempleBackX, X1 = TempleBackX + 300, Y0 = foot - 100, Y1 = BookTop + 300,
                Z0 = LadderAtZ - CelebrationProps.LadderHalf, Z1 = LadderAtZ + CelebrationProps.LadderHalf, Info = new[] { 1, 1, 1, 0, 0, 0, 0, 1 },
            });
            log.Add($"the temple's ladder: up its back wall (x {TempleBackX}) at z {LadderAtZ}, from the ground at {foot} to the book's top, {BookTop}");
            Put(Turning(MonkStatuette.Actor(gameDirectory, BookStatuetteX, BookTop, BookStatuetteZ, 1024)), Life(0, $"{FootTake} > distance_3d(0)", false), TurningTrack);

            // ---- 2: the lava's block, and the slabs to it
            int Cell(double c) => (int)Math.Round(c * 512);
            // (the lake's eruptions -- the game's, one burst after another at its points 34-44, hurting whoever is within 1,000: two of them
            // in the slabs' lane, the one between the second and third slabs threw Twinsen off into the lava -- moved to the lake's open
            // lava either side of the lane)
            foreach (var (point, x, z) in new[] { (36, 13600, 18200), (37, 16400, 11900) })
                if (point < model.TrackPoints.Count && model.TrackPoints[point] is var tp && Math.Abs(tp.Y - 5700) < 200)
                    model.TrackPoints[point] = tp with { X = x, Z = z };
            int rowZ = Cell(LavaRowZ);
            int Point(int x, int y, int z) => SceneOps.AddTrackPoint(model, new SceneTrackPoint(x, y, z));
            SceneActorModel Carrier(int generic, int x, int y, int z)
            {
                var a = CelebrationProps.Actor(gameDirectory, generic, x, y, z);
                a.Flags |= RaceTrackScenes.CarrierFlag | 1u << 12; a.Move = 3;
                return a;
            }
            var block = Carrier(CelebrationProps.Pillar, Cell(BlockX), BlockTop, rowZ);
            Put(block, While(lakeShown));
            // (the first and third slabs: sinking under the lava and coming up, one as the other goes down)
            foreach (var (x, first) in new[] { (SlabOneX, true), (SlabThreeX, false) })
            {
                int up = Point(Cell(x), SlabUp, rowZ), sunk = Point(Cell(x), SlabSunk, rowZ);
                var stay = first ? "label(0);\nspeed(420);\ngoto_point_3d({0});\nwait_nb_dizieme(36);\nspeed(520);\ngoto_point_3d({1});\nwait_nb_dizieme(22);\ngoto(label_0);\n"
                                 : "label(0);\nspeed(520);\ngoto_point_3d({1});\nwait_nb_dizieme(22);\nspeed(420);\ngoto_point_3d({0});\nwait_nb_dizieme(36);\ngoto(label_0);\n";
                Put(Carrier(CelebrationProps.Step, Cell(x), SlabUp, rowZ), While(lakeShown), string.Format(stay, up, sunk));
            }
            // (the second: sliding across the way and back, waiting a moment at each end -- and two seconds in line with the others as it
            // passes, the moment to step on or off: walked on while it slid, it carried Twinsen sideways off its far side)
            {
                int a = Point(Cell(SlabTwoX), SlabUp, Cell(LavaRowZ - SlideHalf)), b = Point(Cell(SlabTwoX), SlabUp, Cell(LavaRowZ + SlideHalf)),
                    mid = Point(Cell(SlabTwoX), SlabUp, rowZ);
                Put(Carrier(CelebrationProps.Step, Cell(SlabTwoX), SlabUp, Cell(LavaRowZ - SlideHalf)), While(lakeShown),
                    $"label(0);\nspeed(650);\ngoto_point_3d({mid});\nwait_nb_dizieme(20);\ngoto_point_3d({b});\nwait_nb_dizieme(10);\n" +
                    $"goto_point_3d({mid});\nwait_nb_dizieme(20);\ngoto_point_3d({a});\nwait_nb_dizieme(10);\ngoto(label_0);\n");
            }
            Put(Turning(MonkStatuette.Actor(gameDirectory, Cell(BlockX), BlockTop, rowZ, 3072)), Life(1, $"{FootTake} > distance_3d(0)", false), TurningTrack);
            log.Add($"the lava's block at cell ({BlockX}, {LavaRowZ}), its top {BlockTop}; slabs at x {SlabOneX} (sinking), {SlabTwoX} (sliding z {LavaRowZ - SlideHalf}-{LavaRowZ + SlideHalf}), {SlabThreeX} (sinking, the other way about)");

            // ---- 3, 4: over the second and third jumps -- the raised road's gaps, in the air over their middles
            var gaps = new List<(int[] Lip, int[] Land)>();
            if (report is not null)
                for (var i = 1; i < report.Raised.Count; i++)
                    if (report.Raised[i][3] == 0 && report.Raised[i - 1][3] > 0)
                    {
                        var end = i;
                        while (end < report.Raised.Count && report.Raised[end][3] == 0) end++;
                        if (end < report.Raised.Count) gaps.Add((report.Raised[i - 1], report.Raised[end]));
                        i = end;
                    }
            for (var j = 1; j <= 2; j++)
            {
                if (j >= gaps.Count) { log.Add($"WARNING: the statuette over the lap's jump {j + 1} left out: the lap has {gaps.Count} jumps"); continue; }
                var (lip, land) = gaps[j];
                int x = (lip[0] + land[0]) / 2 - 7 * IslandFile.CubeSize, z = (lip[1] + land[1]) / 2 - 7 * IslandFile.CubeSize, y = Math.Max(lip[2], land[2]) + FlightOver;
                if (x < 0 || z < 0 || x >= IslandFile.CubeSize || z >= IslandFile.CubeSize) { log.Add($"WARNING: the statuette over the lap's jump {j + 1} left out: not in scene {CelebrationScene}'s cube"); continue; }
                var s = Turning(MonkStatuette.Actor(gameDirectory, x, y, z, 0));
                s.Flags |= 1u << 12;
                Put(s, Life(1 + j, $"{FlightTake} > distance_3d(0)", true), TurningTrack);
                log.Add($"the statuette over the lap's jump {j + 1}: cell ({x / 512.0:0.#}, {z / 512.0:0.#}), {y} -- the gap from ({lip[0] / 512.0 - 448:0.#}, {lip[1] / 512.0 - 448:0.#}) to ({land[0] / 512.0 - 448:0.#}, {land[1] / 512.0 - 448:0.#})");
            }
            return scripts => { foreach (var e in edits) e(scripts); };
        });
    }
}
