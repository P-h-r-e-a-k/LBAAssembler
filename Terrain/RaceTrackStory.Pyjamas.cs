using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Twinsen's pyjamas, the night of the spanking (the user, 2026-11-17, item 35 of the day's list: "When they get back up to the house let's
// have Zoe tell Twinsen to brush his teeth then go straight to bed. At this point we script Twinsen walking into the bathroom, and a few
// seconds later Twinsen comes back out again this time changed into traditional pyjamas complete with a long hat ... The following morning
// Twinsen should wake up in his pyjamas, and not be allowed to leave the house until he gets dressed again. Let's have his racing suit
// folded neatly on the chair to the left of the bed. Using action on the racing suit swaps Twinsen back into it and his pyjamas take their
// place on the chair"):
//   - PyjamaVar: 1 sent to wash (Zoe's word up from the chamber, RaceTrackStory.Evening's night house), 2 in his pyjamas (out of the bathroom
//     into the night's house), 3 dressed again (the chair, the morning after) -- the engine keeps his body the pyjamas' while it is 2
//     (RACEMOD.CPP pyjamas=, PyjamaLine; the body Citadel/CitadelPyjamas);
//   - the bathroom at night, sent to wash: up to the basin, a while there, and out again through its doorway (into the night's house, where
//     he comes out in his pyjamas);
//   - the house by day while he is in them: the front door shut (Twinsen's word there), his racing suit folded on the chair left of the bed
//     (block 48, cell 5,3 of the house's grid) -- Action by it and he is dressed, his pyjamas folded there instead from then on.
internal static partial class RaceTrackStory
{
    public const int PyjamaVar = 265, PyjamasWash = 1, PyjamasOn = 2, PyjamasOff = 3;
    internal static string PyjamaLine => $"pyjamas={PyjamaVar} {PyjamasOn} {Citadel.CitadelPyjamas.PyjamaBody}\n";
    // the night house: a cell variable counting the moment after the lift is up; the hero's track's way to the bathroom (label ToBath, then
    // ToBath + 1 when there); its points, from the lift in the storeroom (raised two layers) down its two steps (x 10-11, z 12-13), across
    // the bedroom to the bathroom's door (its opening x 6-7 in the north wall) and on through it -- found walking it, 2026-11-17
    private const int PyjamaWait = 46, ToBath = 160;
    private static readonly (int X, int Z)[] BathWay = { (5632, 7400), (5632, 6000), (5632, 4500), (3584, 1800), (3584, -400) };
    // the bathroom: the hero's track's labels (to the basin, out); where he stands at the basin (the basin against the north wall at x 8.5
    // cells), how long; the house by day: the chair's seat, the zones round the chair and before the front door, the front door's switch, a
    // cell variable (his word at the door said once a visit)
    private const int AtBasin = 150, WashedOut = 151, BasinX = 8 * 512 + 256, BasinZ = 1250, WashSeconds = 3;
    private const int ChairSeatX = 5 * 512 + 290, ChairSeatY = 2048 + 450, ChairSeatZ = 3 * 512 + 256;
    private const int DressZone = 94, PyjamaDoorTry = 95, PyjamaDoorSwitch = 14, PyjamaSaid = 47;

    private static void Pyjamas(StoryParts p)
    {
        if (!p.Store.SceneExists(Citadel.CitadelHouseRooms.Bathroom)) { p.Log.Add("Twinsen's pyjamas left out: no bathroom"); return; }
        p.Edit(Citadel.CitadelHouseRooms.Bathroom, "the bathroom at night, sent to wash: up to the basin, a while, and out again (his pyjamas on in the night's house)", model =>
        {
            var basin = SceneOps.AddTrackPoint(model, new SceneTrackPoint(BasinX, Citadel.CitadelHouseRooms.FloorY, BasinZ));
            var way = model.Zones.FirstOrDefault(z => z.Type == 0 && z.Num == OpeningScene) ?? throw new NotTheGames("the bathroom's way out");
            var inside = SceneOps.AddTrackPoint(model, new SceneTrackPoint((way.X0 + way.X1) / 2, Citadel.CitadelHouseRooms.FloorY, way.Z0 - 700));
            var outside = SceneOps.AddTrackPoint(model, new SceneTrackPoint((way.X0 + way.X1) / 2, Citadel.CitadelHouseRooms.FloorY, way.Z1 + 600));
            var helper = p.Add(model, RaceTrackScenes.InvisibleHelper(0, Citadel.CitadelHouseRooms.FloorY, 0), $@"void comportement_0()
{{
    if ({PyjamasWash} == var_game({PyjamaVar}))
    {{
        set_dir_obj(0, NO_MOVE);
        set_track_obj(0, label_{AtBasin});
    }}
    set_comportement(comportement_1);
}}

void comportement_1()
{{
}}
");
            return scripts =>
            {
                helper(scripts);
                scripts.SetText(0, ScriptKind.Track, scripts.GetText(0, ScriptKind.Track).TrimEnd() + $@"

label({AtBasin});
anim(1);
goto_point({basin});
anim(0);
beta(2048);
wait_nb_second({WashSeconds});
anim(1);
goto_point({inside});
goto_point({outside});
anim(0);

label({WashedOut});
stop();
");
            };
        });

        p.Edit(OpeningScene, "the morning after the spanking: Twinsen in his pyjamas, the front door shut till he dresses from the chair by his bed", model =>
        {
            var front = model.Zones.FirstOrDefault(z => z.Type == 0 && z.Num == Citadel.CitadelNightHouse.Yard) ?? throw new NotTheGames("the house's front door");
            front.Info[4] = PyjamaDoorSwitch;
            model.Zones.Add(new SceneZoneModel { Type = 2, Num = PyjamaDoorTry, X0 = front.X0 - 256, X1 = front.X1 + 256, Y0 = front.Y0, Y1 = front.Y1, Z0 = front.Z0 - 256, Z1 = front.Z1 + 256, Info = new int[8] });
            model.Zones.Add(new SceneZoneModel { Type = 2, Num = DressZone, X0 = ChairSeatX - 500, X1 = ChairSeatX + 1400, Z0 = ChairSeatZ - 900, Z1 = ChairSeatZ + 900, Y0 = 2048 - 100, Y1 = 2048 + 1500, Info = new int[8] });
            var clothes = Citadel.CitadelProps.Actor(Citadel.CitadelProps.FoldedSuit, ChairSeatX + Citadel.CitadelProps.FoldedShiftX, ChairSeatY, ChairSeatZ + Citadel.CitadelProps.FoldedShiftZ, 0);
            clothes.Armor = WalkOverArmour;
            return p.Add(model, clothes, $@"void comportement_0()
{{
    if ({PyjamasOn} == var_game({PyjamaVar}))
    {{
        set_change_cube({PyjamaDoorSwitch}, 0);
        set_comportement(comportement_1);
    }}
    else if ({PyjamasOff} == var_game({PyjamaVar}))
    {{
        body({Citadel.CitadelProps.FoldedPyjamas});
        set_comportement(comportement_2);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
    if ({PyjamaDoorTry} == zone_obj(0))
    {{
        if (0 == var_cube({PyjamaSaid}))
        {{
            set_var_cube({PyjamaSaid}, 1);
            message_obj(0, {EveningId(p.Say, EveningLine.TwinsenDressFirst)});
        }}
    }}
    else
    {{
        set_var_cube({PyjamaSaid}, 0);
    }}
    swif (1 == action())
    {{
        if ({DressZone} == zone_obj(0) && {Driving} != comportement_hero())
        {{
            set_var_game({PyjamaVar}, {PyjamasOff});
            body({Citadel.CitadelProps.FoldedPyjamas});
            set_change_cube({PyjamaDoorSwitch}, 1);
            set_comportement(comportement_2);
        }}
    }}
}}

void comportement_2()
{{
}}
");
        });
    }
}
