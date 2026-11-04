using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Celebration Island's easter eggs (the user, 2026-11-04, of the proposal's picks: "Let's make all those changes"; the variables:
// RaceTrackStory.Eggs.cs).
//   - The fifth statuette (the proposal: "the seller wanted four Dark Monk statuettes. A fifth sits where nobody would drop one: on the lava
//     lake's jump, at the lip. Brought to the seller: 'Five? I only ever made four.' Zoe: 'Twinsen, put it down. It's looking at me.' In the
//     final (chapter 14), on the statue's ring round its head, the same statuette sits facing the road."): in the lava lake's race (scene
//     223), the Dark Monk's statuette (sprite 250) at the edge of the lap's first jump's lip, away from the racing line -- a car taking off
//     along that edge, or Twinsen on foot beside it, picks it up (EggStatuette 1; a sound then, its line once out of the car). Seen only on
//     foot: a SPRITES.HQR picture is drawn at its own size however far off, and in front of a racing car it stood over the jump like a
//     statue. The seller (scene 95), talked to with it: his line and Zoe's (EggStatuette 2). In the final, the statuette by the outer rail
//     of the ring round the statue's head while EggStatuette is 1 or more.
//   - Picking the Dark Monk's nose (the proposal: "in the final, on the spiral round the statue's head, a clover leaf hidden up the statue's
//     nostril; the car's camera catches it only on the ring. Picked up: 'Twinsen picked the Dark Monk's nose. A clover leaf. Lucky.'"): a
//     clover leaf (the game's own leaf, raw sprite 7) under the statue's nose, which looks west, along the ring's first stretch (where the
//     spiral comes onto it): a car on the tight line round the head, against the ring's inner rail, takes it (its line once out of the car).
//     One leaf a visit (var_cube), Twinsen's clover leaves one more (FLAG_CLOVER, var 251; no more than MAX_CLOVER_BOX: a script can't read
//     how many boxes he has).
internal static partial class RaceTrackStory
{
    // ---- the fifth statuette: the lava lake (chapter 6) ---------------------------------------------------------------------------------
    // (how near Twinsen comes to pick it up, and the bonus's sound -- SAMPLE_BONUS, COMMON.H -- as a car takes it)
    private const int StatuetteReach = 650, BonusSample = 3;
    // The flags that tell the race-track mode an actor on the road is the build's (RACEMOD.CPP RACE_BUILD_ACTOR: NO_SHADOW | NO_CHOC |
    // NO_PRE_CLIP | OBJ_ZBUFFER): anything else on the road its roadwatch takes out of sight while a car races -- the eggs on the road with it
    internal const uint RaceProp = 0x1A1000;

    // A sprite on the road while a car races (RaceTrackScenes.SpriteProp, the race-track mode's own)
    private static SceneActorModel RoadSprite(int x, int y, int z, int sprite)
    {
        var a = RaceTrackScenes.SpriteProp(x, y, z, sprite, 9);
        a.Flags |= RaceProp;
        return a;
    }

    // An egg's pickup, as its life script: there while `when` holds as the scene starts; taken within `reach` of Twinsen (in his car too:
    // `driving`, only in it) -- `take` done, a sound -- and its `line` said once he is out of the car (the race-track mode leaves out a
    // script's lines while a car races, RACEMOD.CPP RaceMod_Hush: out of the car after a race, it is said after the race's own end's lines,
    // the same frame -- a change_cube leaves the rest of the frame's scripts to run). `onFoot`: seen only while Twinsen is on foot (a
    // SPRITES.HQR picture is drawn at its own size whatever the distance: a statuette as big as a statue in front of a racing car).
    private static string Pickup(string when, int reach, bool driving, string take, int sound, int line, bool onFoot = false) => $@"void comportement_0()
{{
    if ({when})
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
{(onFoot ? $"    if ({Driving} == comportement_hero())\n    {{\n        invisible(1);\n    }}\n    else\n    {{\n        invisible(0);\n    }}\n" : "")}    if ({reach} > distance(0){(driving ? $" && {Driving} == comportement_hero()" : "")})
    {{
{take}        sample({sound});
        invisible(1);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if ({Driving} != comportement_hero())
    {{
        message({line});
        suicide();
    }}
}}
";

    // `lava`: the first of LavaLines' ids (the lines LavaLine.Statuette5, SellerFive, ZoeLooking). Called by ApplyCelebration last.
    internal static List<string> FifthStatuette(string gameDirectory, RaceTrackReport? report, int lava)
    {
        var log = new List<string>();
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        int Say(LavaLine l) => lava + (int)l;
        // the seller, talked to with the fifth: his line and Zoe's, in place of what he'd say (his talk is comportement_2 at his track's label
        // 11: ApplyCelebration's script)
        GroupB.EditScene(store, CelebrationScene, log, $"the seller and the fifth statuette: \"Five?\" and Zoe's word (variable {EggStatuette} {StatuetteFound} -> {StatuetteShown})", model =>
        {
            var seller = model.Actors.FindIndex(a => a.Entity == SellerEntity);
            if (seller < 1) throw new NotTheGames($"no souvenir seller (entity {SellerEntity})");
            return scripts => scripts.SetText(seller, ScriptKind.Life, Loose(scripts.GetText(seller, ScriptKind.Life), "void comportement_2()\n{\n    if (11 == l_track())", $@"void comportement_2()
{{
    if (11 == l_track() && {StatuetteFound} == var_game({EggStatuette}))
    {{
        message({Say(LavaLine.SellerFive)});
        message_zoe({Say(LavaLine.ZoeLooking)});
        set_var_game({EggStatuette}, {StatuetteShown});
        set_var_cube(0, 0);
        set_track(label_20);
        set_comportement(comportement_1);
    }}
    else if (11 == l_track())"));
        });
        // the statuette at the lip of the lap's first jump (found as LavaDive finds it), at the road's edge
        if (FirstLip(report) is not { } lip) { log.Add("the fifth statuette left out: no jump with a lip on the raised road"); return log; }
        // (across the road from the lip's middle, just inside the deck's edge on the other side from the racing line -- the opponents' line,
        // PlanRacePath's: 3 cells from it on the lava lake, where a car that keeps to it never comes near)
        var side = Math.Max(0, lip.Half - 450) / 512.0;
        if (report!.RacePath.Count > 0 && report.RacePath.MinBy(p => Math.Pow(p.X - lip.X, 2) + Math.Pow(p.Z - lip.Z, 2)) is var line
            && (line.X - lip.X) * -lip.DirZ + (line.Z - lip.Z) * lip.DirX > 0) side = -side;
        double x = lip.X - lip.DirZ * side, z = lip.Z + lip.DirX * side;
        var scene = GroupB.OutsideScene(store, RaceTrackIsland.CelebrationLava, x, z);
        if (scene < 0) { log.Add($"the fifth statuette left out: no scene at cell ({x:0.#}, {z:0.#})"); return log; }
        GroupB.EditScene(store, scene, log, $"the fifth statuette at the edge of the first jump's lip, cell ({x:0.#}, {z:0.#}), {lip.Y:0} (variable {EggStatuette} 0 -> {StatuetteFound})", model =>
        {
            // (out of sight until its script sees Twinsen on foot)
            var statuette = RoadSprite(RaceTrackScenes.CubeLocal(x, model.CubeX), (int)Math.Round(lip.Y), RaceTrackScenes.CubeLocal(z, model.CubeY), RaceTrackScenes.MonkStatuetteSprite);
            statuette.Flags |= RaceTrackScenes.InvisibleFlag;
            return GroupB.Add(model, statuette,
                Pickup($"0 == var_game({EggStatuette})", StatuetteReach, false, $"        set_var_game({EggStatuette}, {StatuetteFound});\n", BonusSample, Say(LavaLine.Statuette5), onFoot: true));
        });
        return log;
    }

    // The lap's first jump's lip (as LavaDive finds it): a carried jump's, else the last point of the raised road before its first gap -- its
    // place and height (island cells; the deck's height), its half width (units), the way across the gap (a unit vector). Null: none.
    private static (double X, double Z, double Y, double Half, double DirX, double DirZ)? FirstLip(RaceTrackReport? report)
    {
        if (report is null) return null;
        int lipAt = -1, landAt = -1;
        if (report.ArcJumps.Count > 0) (_, lipAt, landAt, _, _, _) = report.ArcJumps[0];
        else
            for (var k = 1; k + 1 < report.Raised.Count && lipAt < 0; k++)
                if (report.Raised[k][3] == 0 && report.Raised[k - 1][3] > 0)
                {
                    var end = k;
                    while (end < report.Raised.Count && report.Raised[end][3] == 0) end++;
                    if (end < report.Raised.Count) { lipAt = k - 1; landAt = end; }
                }
        if (lipAt < 0 || landAt < 0 || lipAt >= report.Raised.Count || landAt >= report.Raised.Count) return null;
        int[] lip = report.Raised[lipAt], land = report.Raised[landAt];
        double dx = land[0] - lip[0], dz = land[1] - lip[1], len = Math.Sqrt(dx * dx + dz * dz);
        if (len < 512) return null;
        return (lip[0] / 512.0, lip[1] / 512.0, lip[2], lip[3], dx / len, dz / len);
    }

    // ---- the final (chapter 14): the statuette on the ring, the nose's clover leaf ---------------------------------------------------------
    private enum FinalEggLine { Nose }
    private static readonly string[][] FinalEggLines =
    {
        new[]
        {
            "Twinsen picked the Dark Monk's nose. A clover leaf. Lucky.",
            "Twinsen a curé le nez de Dark Monk. Une feuille de trèfle. Quelle veine.",
            "Twinsen hat Dark Monk in der Nase gebohrt. Ein Kleeblatt. Glück gehabt.",
            "Twinsen le ha hurgado la nariz a Dark Monk. Una hoja de trébol. Qué suerte.",
            "Twinsen ha frugato nel naso di Dark Monk. Un quadrifoglio. Che fortuna.",
            "O Twinsen meteu o dedo no nariz do Dark Monk. Uma folha de trevo. Que sorte.",
        },
    };
    // The statue's nose (island cells, its height): the statue's head is round the plan's HEAD (482.8, 481.4: celebration_design.py) and
    // looks west, down the ring's first stretch -- its decor's faces stand out furthest that way between 17,200 and 18,100 (the head's
    // middle at x 482.3, the nose's tip 2.5 cells west of it). The leaf just under its tip, in the nostril.
    private const double NoseX = 448 + 31.7, NoseZ = 448 + 33.5, NoseY = 17300;
    // (how near the car comes for the leaf: 2,550 -- the tightest line round the head, the test pilot's against the ring's inner rail,
    // passes 2,480 from the nose beside the face: within reach, a car a cell further out isn't; the leaf's raw sprite (SPRIRAW 7,
    // SPRITE_CLOVER), the visit's var_cube, the most leaves; the statuette's place on the ring, cells out from the road's middle: by the
    // outer rail, 3.75 out -- out of the tight line's way, as big as it is: a SPRITES.HQR picture isn't drawn smaller with distance)
    private const int NoseReach = 2550, CloverSprite = 7, NoseOnce = 47, MaxCloverBoxes = 10, CloverVar = 251;
    private const double RingStatuette = 2.9;

    // `report`: the statue track's, `startScene` its scene (95). Called by ApplyFinal last.
    internal static List<string> FinalEggs(string gameDirectory, RaceTrackReport report, int startScene)
    {
        var log = new List<string>();
        if (startScene < 0 || report.LapY.Length != report.LapX.Length || report.LapX.Length < 10) { log.Add("the final's eggs left out: no track"); return log; }
        var first = GroupB.AddLines(gameDirectory, FinalTexts, FinalEggLines, log, "the final's eggs (the Dark Monk's nose):");
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var here = $"{FinalChapter} == chapter() && 1 == var_game({StatueRisen})";
        // the ring round the head: the lap's highest level stretch (with the end of the spiral's last corner before it and the bridge's
        // first cells after); its middle from three points well inside it (a quarter, half and three quarters along)
        var top = report.LapY.Max();
        var ring = Enumerable.Range(0, report.LapX.Length).Where(i => report.LapY[i] >= top - 1).ToList();
        if (ring.Count < 8) { log.Add("the final's eggs left out: no ring round the statue's head"); return log; }
        int a = ring[ring.Count / 4], b = ring[ring.Count / 2], c = ring[ring.Count * 3 / 4];
        if (Circle((report.LapX[a], report.LapZ[a]), (report.LapX[b], report.LapZ[b]), (report.LapX[c], report.LapZ[c])) is not { } mid) { log.Add("the final's eggs left out: the ring isn't round"); return log; }
        log.Add($"the final's eggs: the ring round the statue's head at {top:0}, its middle at cell ({mid.X:0.0}, {mid.Z:0.0}), {Math.Sqrt(Math.Pow(report.LapX[b] - mid.X, 2) + Math.Pow(report.LapZ[b] - mid.Z, 2)):0.0} cells round");
        // (the statuette at the ring's middle point, inside its outer rail: facing the road, as a sprite always does)
        var m = ring[ring.Count / 2];
        double ox = report.LapX[m] - mid.X, oz = report.LapZ[m] - mid.Z, r = Math.Sqrt(ox * ox + oz * oz);
        double sx = mid.X + ox / r * (r + RingStatuette), sz = mid.Z + oz / r * (r + RingStatuette);
        GroupB.EditScene(store, startScene, log, $"chapter 14: the fifth statuette on the ring round the statue's head at cell ({sx:0.#}, {sz:0.#}), {top:0} (variable {EggStatuette} 1 or more), " +
            $"and a clover leaf in the statue's nose at cell ({NoseX:0.#}, {NoseZ:0.#}), {NoseY:0} (text {first})", model =>
        {
            if (!RaceTrackScenes.InCube(model, sx, sz) || !RaceTrackScenes.InCube(model, NoseX, NoseZ)) throw new NotTheGames("the ring isn't in the scene's cube");
            var statuette = GroupB.Add(model, RoadSprite(RaceTrackScenes.CubeLocal(sx, model.CubeX), (int)Math.Round(top), RaceTrackScenes.CubeLocal(sz, model.CubeY), RaceTrackScenes.MonkStatuetteSprite), $@"void comportement_0()
{{
    if (0 == var_game({EggStatuette}) || {FinalChapter} != chapter() || 1 != var_game({StatueRisen}))
    {{
        suicide();
    }}
}}
");
            var leaf = RoadSprite(RaceTrackScenes.CubeLocal(NoseX, model.CubeX), (int)NoseY, RaceTrackScenes.CubeLocal(NoseZ, model.CubeY), CloverSprite);
            var nose = GroupB.Add(model, leaf, Pickup($"{here} && 0 == var_cube({NoseOnce})", NoseReach, true, $@"        set_var_cube({NoseOnce}, 1);
        if ({MaxCloverBoxes} > var_game({CloverVar}))
        {{
            add_var_game({CloverVar}, 1);
        }}
", BonusSample, first + (int)FinalEggLine.Nose));
            return scripts => { statuette(scripts); nose(scripts); };
        });
        return log;
    }

    // The middle of the circle through three places (null: they're in a line)
    private static (double X, double Z)? Circle((double X, double Z) p, (double X, double Z) q, (double X, double Z) s)
    {
        var d = 2 * (p.X * (q.Z - s.Z) + q.X * (s.Z - p.Z) + s.X * (p.Z - q.Z));
        if (Math.Abs(d) < 1e-6) return null;
        double p2 = p.X * p.X + p.Z * p.Z, q2 = q.X * q.X + q.Z * q.Z, s2 = s.X * s.X + s.Z * s.Z;
        return ((p2 * (q.Z - s.Z) + q2 * (s.Z - p.Z) + s2 * (p.Z - q.Z)) / d, (p2 * (s.X - q.X) + q2 * (p.X - s.X) + s2 * (q.X - p.X)) / d);
    }
}
