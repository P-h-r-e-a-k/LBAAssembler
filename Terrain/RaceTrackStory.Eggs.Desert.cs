using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The White Leaf Desert's easter eggs (the user, 2026-11-04, of the proposal's picks: "Let's make all those changes"; the variables:
// RaceTrackStory.Eggs.cs; the lines at the end of DesertLines, from PageEgg on).
//   - The fourth exam page (the proposal: "three pages blew off the road, but a fourth landed somewhere only a jump reaches ... 'A page: How
//     to Pass Your Exams Without Studying. Step 1: let racing cars blow everyone else's papers away.' Given to the Dean: 'That's not one of
//     mine. ...Whose is it? Flick!' Flick, at her plinth: 'I have no idea what you mean.' A clover box."): a sheet of paper (sprite 238) in
//     the air over the jump at the crossing, beside the flight's line on the other side from the racing line's -- a car taking off from
//     that side of the ramp flies through it (EggPage 1, chapter 3; a sound, and its line once out of the car: the race-track mode leaves a
//     script's lines out while Twinsen drives); the Dean, talked to with it: Twinsen reads it out, the Dean's line (2); Flick, then (3), and
//     her clover box (inc_clover_box).
//   - Bù's lap record (the proposal: "a rune tablet by the Temple bend. Before the race: 'Twinsen can't read the runes. If only he still had
//     the Book of Bù.' After winning: the runes are clear to him: 'Here raced the chariot of Bù. Lap record: 1 minute 2 seconds. Nobody will
//     ever beat it.' Beat that time in a Desert race and the tablet gains a line: '...except Twinsen.' 50 kashes from the Dean."): a standing
//     stone (the game's, the lava's: entity 218, BODY 316) off the road where it passes the Temple of Bù's door, read with Action; the lap
//     beaten is EggBu 1 (the race-track mode's lap_record=, set by the engine), and the Dean pays once (EggBu 2): after the race won, among
//     the lines out of the car, or in the School if the race was lost.
internal static partial class RaceTrackStory
{
    // the Temple of Bù's first scene (its door is a cube-change zone of the Desert's outside scene there, 67), the standing stone (entity,
    // body), the Dean's reward, and the clover box's sounds (the game's own clover box, scene 100's)
    private const int TempleOfBu = 10, TabletEntity = 218, StoneBody = 0, BuReward = 50, CloverBoxSample1 = 662, CloverBoxSample2 = 663;
    // The fourth page over the jump: how far along the flight (its start to its landing), how far to the side of its line (cells), how high
    // over the take-off (units: a car flies 2,400 high at the flight's middle, the take-off 830, the crossing road under it 300 --
    // distance() counts only within 1,500 up or down, so the cars on that road never come near it), and how near the car flies to take it
    private const double PageAlong = 0.5, PageSide = 2.0;
    private const int PageOver = 1300, PageReach = 700;

    // The Dean's talk, before his own: Bù's record beaten (the race lost: won, it's paid out of the car), and the fourth page
    private static string DeanEggs(Func<DesertLine, int> say) => $@"            if ({BuBeaten} == var_game({EggBu}))
            {{
                message({say(DesertLine.DeanBu)});
                add_gold_pieces({BuReward});
                set_var_game({EggBu}, {BuPaid});
            }}
            else if ({PageFound} == var_game({EggPage}))
            {{
                message_obj(0, {say(DesertLine.PageEgg)});
                message({say(DesertLine.DeanNotMine)});
                set_var_game({EggPage}, {PageGiven});
            }}
            else
";

    // ... Flick's, before hers: the page, and the clover box
    private static string FlickEggs(Func<DesertLine, int> say) => $@"            if ({PageGiven} == var_game({EggPage}))
            {{
                message({say(DesertLine.FlickNoIdea)});
                message({say(DesertLine.FlickBox)});
                inc_clover_box();
                sample({CloverBoxSample1});
                sample({CloverBoxSample2});
                set_var_game({EggPage}, {PageDenied});
            }}
            else
";

    // ... and after the race won, out of the car: the Dean pays for Bù's record
    private static string BuAfterRace(Func<DesertLine, int> say) => $@"        if ({BuBeaten} == var_game({EggBu}))
        {{
            message({say(DesertLine.DeanBu)});
            add_gold_pieces({BuReward});
            set_var_game({EggBu}, {BuPaid});
        }}
";

    // The page and the stone, into the Desert's scenes (`b`, saved with the rest of the chapter). `desert`: chapter 3's condition.
    private static void DesertEggs(IslandBeatsA b, RaceTrackReport report, Func<DesertLine, int> say, string desert)
    {
        // the fourth page, over the jump at the crossing, to the side of the flight's line
        if (report.Jumps.Count == 0) b.Log.Add("the fourth exam page left out: no jump");
        else
        {
            var j = report.Jumps[0];
            double mx = j.StartX + (j.LandX - j.StartX) * PageAlong, mz = j.StartZ + (j.LandZ - j.StartZ) * PageAlong;
            // (on the other side of the flight's line from the racing line's -- the opponents' line, PlanRacePath's, where it leaves the ramp:
            // a car flies on as far to the side as it took off, so off that line it has to take the ramp along its other edge to fly through
            // the page; not the line's nearest point to the flight's middle, which is on the road under it)
            var side = PageSide;
            var lineSide = report.RacePath.Count == 0 ? 0 : report.RacePath.MinBy(p => Math.Pow(p.X - j.StartX, 2) + Math.Pow(p.Z - j.StartZ, 2)) is var line ? (line.X - j.StartX) * -j.DirZ + (line.Z - j.StartZ) * j.DirX : 0;
            if (lineSide > 0) side = -PageSide;
            double x = mx - j.DirZ * side, z = mz + j.DirX * side;
            var y = (int)Math.Round(j.Height) + PageOver;
            var scene = b.SceneAt(x, z);
            if (scene < 0) b.Log.Add($"the fourth exam page left out: no scene at cell ({x:0.#}, {z:0.#})");
            else b.Edit(scene, $"the fourth exam page in the air over the jump, {Math.Abs(side):0.#} cells to the side of the flight's line at cell ({x:0.#}, {z:0.#}), {y} -- the racing line takes off {lineSide:0.#} cells to the other side (variable {EggPage} 0 -> {PageFound})", model =>
                IslandBeatsA.Add(model, RoadSprite(RaceTrackScenes.CubeLocal(x, model.CubeX), y, RaceTrackScenes.CubeLocal(z, model.CubeY), RaceTrackScenes.PaperSprite),
                    Pickup($"{desert} && 0 == var_game({EggPage})", PageReach, true, $"        set_var_game({EggPage}, {PageFound});\n", BonusSample, say(DesertLine.PageEgg))));
        }

        // Bù's stone: off the road where the lap passes nearest the Temple of Bù's door, facing the road
        (double X, double Z)? door = null;
        foreach (var s in b.OutsideScenes)
        {
            var m = b.Store.Load(s);
            if (m.Zones.FirstOrDefault(zn => zn.Type == 0 && zn.Num == TempleOfBu) is { } zone)
            { door = (m.CubeX * 64 + (zone.X0 + zone.X1) / 1024.0, m.CubeY * 64 + (zone.Z0 + zone.Z1) / 1024.0); break; }
        }
        if (door is not { } d || report.LapX.Length == 0) { b.Log.Add($"Bù's stone left out: no door into the Temple of Bù (scene {TempleOfBu})"); return; }
        var near = Enumerable.Range(0, report.LapX.Length).MinBy(i => Math.Pow(report.LapX[i] - d.X, 2) + Math.Pow(report.LapZ[i] - d.Z, 2));
        double lx = report.LapX[near], lz = report.LapZ[near];
        // (a third of the way from the road to the door, on the temple's side: three cells clear of the road, out of a car's way)
        if (GroupB.OffRoadNear(report, lx + (d.X - lx) / 3, lz + (d.Z - lz) / 3, 3, 8) is not { } o) { b.Log.Add("Bù's stone left out: no ground off the road by the Temple of Bù"); return; }
        var at = Facing(o, lx, lz);
        var stoneScene = b.SceneAt(at.X, at.Z);
        if (stoneScene < 0) { b.Log.Add($"Bù's stone left out: no scene at cell ({at.X:0.#}, {at.Z:0.#})"); return; }
        b.Edit(stoneScene, $"Bù's lap record: a standing stone with runes by the Temple bend at cell ({at.X:0.#}, {at.Z:0.#}) (the race won: Q_PAGES {PagesWon}; beaten: variable {EggBu} {BuBeaten})", model =>
        {
            var stone = SceneOps.BlankActor(SceneGame.Lba2, RaceTrackScenes.CubeLocal(at.X, model.CubeX), (int)Math.Round(at.Y), RaceTrackScenes.CubeLocal(at.Z, model.CubeY), TabletEntity);
            stone.Body = StoneBody; stone.Beta = at.Beta; stone.CoulObj = 9;
            return IslandBeatsA.Add(model, stone, Talker("0 <= chapter()", $@"            if ({PagesWon} > var_game({QPages}))
            {{
                message({say(DesertLine.RunesUnread)});
            }}
            else
            {{
                message({say(DesertLine.RunesRecord)});
                if (0 < var_game({EggBu}))
                {{
                    message({say(DesertLine.RunesExcept)});
                }}
            }}"));
        });
    }
}
