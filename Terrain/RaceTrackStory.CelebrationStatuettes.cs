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
//   - on a column in the lava, west of the causeway (CelebrationProps.Pillar), high out of everyone's reach but the lift's: from the north
//     beach a slab sinking under the lava and coming up again, one sliding back and forth, and the lift in the lava's middle -- surfacing,
//     rising to the column's height, sinking back under (carriers on their tracks). Spaced for Twinsen's standing Sporty jump (the user,
//     2026-11-27: "measure Twinsen's static jump distance ... then just design our lava timing puzzle around this distance");
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
    // (moved a cell back from the road with the temple, RaceTrackIsland.TempleBack; the ladder over the strip's ground, z 40-41)
    private const int TempleBackX = 13088 + RaceTrackIsland.TempleBack, BookTop = 10660, LadderAtZ = 20600, BookStatuetteX = 11288 + RaceTrackIsland.TempleBack, BookStatuetteZ = 19700;
    // the temple's door into its inside (scene 93) and the way out of it, moved with it
    private const int TempleInside = 93;
    // The lava's way, spaced for Twinsen's standing Sporty jump, measured on flat ground 2026-11-27 (E:/dump/TEMP/nov27/jump): 0.53 s
    // crouched, then 1.04 s in the air, landing 1,919 ahead (1,826 where the last drop starts), at most 714 up (1,343 out) -- so a slab's
    // middle 1,924 from the next's (StepHalf 512, 900 of lava between): jumped from about its middle, Twinsen lands in the next one's.
    // The lake west of the causeway is small for that -- ten cells across, a road deck on two sides (up to 7,100 at the lap's jump lips) and
    // the beaches on the others -- and everywhere in it is a jump from somewhere but its middle: so the lift there, out of a standing
    // jump's reach from every shore while it is down (102 to spare), and the column for the statuette at 7,900, out of reach of every shore,
    // deck and slab (+714 at the most) but the lift at its top, 7,500 (cube-local cells; heights):
    //   - the first slab in the lava river's mouth by the north beach, sinking under the lava and coming up;
    //   - the second sliding between its home -- a jump from the lift -- and 1.5 cells east, waiting at home;
    //   - the lift in the lake's middle: surfacing to 6,000, rising to 7,500, waiting, back down and under the lava;
    //   - the column a jump east of the lift at its top.
    private const double StoneX = 33.5, StoneZ = 23.0, SlideHomeX = 30.35, SlideEastX = 31.85, SlideZ = 26.5, LiftX = 30.1, LiftZ = 29.9, BlockX = 33.4, BlockZ = 30.0;
    private const int SlabUp = 6000, SlabSunk = 5380, LavaLiftTop = 7500, BlockTop = 7900;
    // (an eruption of the lake's within this of the way moved off it -- an eruption hurts within 1,000)
    private const int EruptionClear = 1500;
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
        // (and the door in the lava lake's raced copy of the scene, 223)
        var raced = RaceTrackIsland.CelebrationLava.FirstScene;
        if (store.SceneExists(raced))
            GroupB.EditScene(store, raced, log, $"the temple's door into scene {TempleInside} in the raced copy, a cell back with it", model =>
            {
                foreach (var door in model.Zones.Where(z => z.Type == 0 && z.Num == TempleInside && z.X0 < 9500)) { door.X0 += RaceTrackIsland.TempleBack; door.X1 += RaceTrackIsland.TempleBack; }
                return _ => { };
            });
        // (the way out of the temple's inside to its door, a cell back with it)
        GroupB.EditScene(store, TempleInside, log, $"the way out of the temple to its door in scene {CelebrationScene}, a cell back with it", model =>
        {
            var moved = 0;
            foreach (var exit in model.Zones.Where(z => z.Type == 0 && z.Num == CelebrationScene && z.Info[0] < 9000)) { exit.Info[0] += RaceTrackIsland.TempleBack; moved++; }
            if (moved == 0) throw new NotTheGames($"no way out to scene {CelebrationScene}");
            return _ => { };
        });
        // (the island's cube for scene 95: (7,7) -- checked against the scene's own as it's edited)
        GroupB.EditScene(store, CelebrationScene, log, "the four Dark Monk statuettes, all there at once, found in any order (variables 296-299): on the temple's book (a ladder up its back), on a block in the lava (stepping slabs, sinking and sliding), over the lap's second and third jumps", model =>
        {
            if (model.CubeX != 7 || model.CubeY != 7) throw new NotTheGames($"scene {CelebrationScene} isn't cube (7,7)");
            var edits = new List<Action<SceneScripts>>();
            // (the temple's door, a cell back with it)
            foreach (var door in model.Zones.Where(z => z.Type == 0 && z.Num == TempleInside && z.X0 < 9500))
            {
                door.X0 += RaceTrackIsland.TempleBack; door.X1 += RaceTrackIsland.TempleBack;
                log.Add($"the temple's door into scene {TempleInside}: a cell back with it, x {door.X0}..{door.X1}");
            }
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
            // (the lake's eruptions -- the game's, one burst after another at its points 34-44, hurting whoever is within 1,000: one between
            // the old slabs threw Twinsen off into the lava -- any near the way moved to the lake's open lava away from it)
            var way = new[] { (StoneX, StoneZ), (SlideHomeX, SlideZ), (SlideEastX, SlideZ), (LiftX, LiftZ), (BlockX, BlockZ) }.Select(c => (X: Cell(c.Item1), Z: Cell(c.Item2))).ToArray();
            bool NearWay(int x, int z) => way.Any(w => Math.Sqrt((double)(w.X - x) * (w.X - x) + (double)(w.Z - z) * (w.Z - z)) < EruptionClear);
            var open = new Queue<(int X, int Z)>(new[] { (13600, 18200), (14336, 18688), (13312, 15300), (20700, 17400), (21000, 15600), (14600, 12500) }.Where(o => !NearWay(o.Item1, o.Item2)));
            var taken = new HashSet<(int, int)>();
            for (var point = 34; point <= 44 && point < model.TrackPoints.Count; point++)
                if (model.TrackPoints[point] is var tp && Math.Abs(tp.Y - 5700) < 200 && (NearWay(tp.X, tp.Z) || point is 36 or 37) && open.Count > 0)
                {
                    var to = open.Dequeue();
                    model.TrackPoints[point] = tp with { X = to.X, Z = to.Z };
                    log.Add($"the lake's eruption at point {point}: from ({tp.X}, {tp.Z}) to ({to.X}, {to.Z}), off the way to the statuette");
                }
            int Point(int x, int y, int z) => SceneOps.AddTrackPoint(model, new SceneTrackPoint(x, y, z));
            SceneActorModel Carrier(int generic, int x, int y, int z)
            {
                var a = CelebrationProps.Actor(gameDirectory, generic, x, y, z);
                a.Flags |= RaceTrackScenes.CarrierFlag | 1u << 12; a.Move = 3;
                return a;
            }
            var block = Carrier(CelebrationProps.Pillar, Cell(BlockX), BlockTop, Cell(BlockZ));
            Put(block, While(lakeShown));
            // (the first slab: up four seconds, then under the lava two)
            {
                int up = Point(Cell(StoneX), SlabUp, Cell(StoneZ)), sunk = Point(Cell(StoneX), SlabSunk, Cell(StoneZ));
                Put(Carrier(CelebrationProps.Step, Cell(StoneX), SlabUp, Cell(StoneZ)), While(lakeShown),
                    $"label(0);\nspeed(420);\ngoto_point_3d({up});\nwait_nb_dizieme(40);\nspeed(520);\ngoto_point_3d({sunk});\nwait_nb_dizieme(20);\ngoto(label_0);\n");
            }
            // (the second: two and a half seconds at home -- a jump from the lift -- then out east and back)
            {
                int home = Point(Cell(SlideHomeX), SlabUp, Cell(SlideZ)), east = Point(Cell(SlideEastX), SlabUp, Cell(SlideZ));
                Put(Carrier(CelebrationProps.Step, Cell(SlideHomeX), SlabUp, Cell(SlideZ)), While(lakeShown),
                    $"label(0);\nspeed(650);\ngoto_point_3d({home});\nwait_nb_dizieme(25);\ngoto_point_3d({east});\nwait_nb_dizieme(10);\ngoto(label_0);\n");
            }
            // (the lift: surfaced three seconds -- jumped onto from the second slab at home -- up to the column's height, three seconds there,
            // down, under the lava a second and a half: who is still on it then goes into the lava)
            {
                int low = Point(Cell(LiftX), SlabUp, Cell(LiftZ)), top = Point(Cell(LiftX), LavaLiftTop, Cell(LiftZ)), sunk = Point(Cell(LiftX), SlabSunk, Cell(LiftZ));
                Put(Carrier(CelebrationProps.Lift, Cell(LiftX), SlabSunk, Cell(LiftZ)), While(lakeShown),
                    $"label(0);\nspeed(420);\ngoto_point_3d({low});\nwait_nb_dizieme(30);\nspeed(520);\ngoto_point_3d({top});\nwait_nb_dizieme(30);\n" +
                    $"goto_point_3d({low});\nspeed(420);\ngoto_point_3d({sunk});\nwait_nb_dizieme(15);\ngoto(label_0);\n");
            }
            Put(Turning(MonkStatuette.Actor(gameDirectory, Cell(BlockX), BlockTop, Cell(BlockZ), 3072)), Life(1, $"{FootTake} > distance_3d(0)", false), TurningTrack);
            log.Add($"the lava's column at cell ({BlockX}, {BlockZ}), its top {BlockTop}; the lift at ({LiftX}, {LiftZ}), {SlabSunk} to {SlabUp} to {LavaLiftTop}; the sliding slab ({SlideHomeX}-{SlideEastX}, {SlideZ}), the sinking slab ({StoneX}, {StoneZ}), from the north beach");

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
