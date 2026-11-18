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
//   - sent to wash: to the bathroom's doorway, gone in it a few seconds and back in it in his pyjamas -- the bathroom itself never loaded
//     (the user, 2026-11-18: "instead of watching him going in there, let's have him turn invisible at the door way, then a few seconds
//     later reappear in the doorway facing the other way now dressed in his pyjamas. Let's never actually load the bathroom";
//     RaceTrackStory.Evening's night house);
//   - the house by day while he is in them: the front door shut (Twinsen's word there), his racing suit folded on the chair left of the bed
//     (block 48, cell 5,3 of the house's grid) -- Action by it and he is dressed, his pyjamas folded there instead from then on;
//   - and the game's first morning too, after the dream (the user, 2026-11-18: "When Twinsen wakes up initially let's have him wake up in
//     his pyjamas, with his casual clothes on the chair for him to change into"): PolarDream's wake sets PyjamaVar to PyjamasOn, and in
//     chapter 1 the chair holds his everyday clothes (FoldedCasual) -- dressed, he is in them (the engine's GEN_BODY_TUNIQUE: his everyday
//     body till he has his tunic, ChoiceHeroBody).
internal static partial class RaceTrackStory
{
    public const int PyjamaVar = 265, PyjamasWash = 1, PyjamasOn = 2, PyjamasOff = 3;
    internal static string PyjamaLine => $"pyjamas={PyjamaVar} {PyjamasOn} {Citadel.CitadelPyjamas.PyjamaBody}\n";
    // the night house: a cell variable counting the moment after the lift is up; the hero's track's way to the bathroom (label ToBath, then
    // ToBath + 1 in its doorway, ToBath + 2 back out of it in his pyjamas); its points, from the lift in the storeroom (raised two layers)
    // down its two steps (x 10-11, z 12-13), across the bedroom to the bathroom's doorway (its opening x 6-7 in the north wall) -- found
    // walking it, 2026-11-17 -- and stopping in it short of its zone into the bathroom (x 3072-4095, z 0-560): the bathroom isn't loaded,
    // he is gone in the doorway BathDoorSeconds and back in it in his pyjamas (the user, 2026-11-18)
    private const int PyjamaWait = 46, ToBath = 160, BathDoorSeconds = 3;
    private static readonly (int X, int Z)[] BathWay = { (5632, 7400), (5632, 6000), (5632, 4500), (3584, 1800), (3584, 700) };
    // the house by day: the chair's seat, the zones round the chair and before the front door, the front door's switch, a cell variable (his
    // word at the door said once a visit)
    private const int ChairSeatX = 5 * 512 + 290, ChairSeatY = 2048 + 450, ChairSeatZ = 3 * 512 + 256;
    private const int DressZone = 94, PyjamaDoorTry = 95, PyjamaDoorSwitch = 14, PyjamaSaid = 47;

    private static void Pyjamas(StoryParts p)
    {
        if (!p.Store.SceneExists(Citadel.CitadelHouseRooms.Bathroom)) { p.Log.Add("Twinsen's pyjamas left out: no bathroom"); return; }
        p.Edit(OpeningScene, "the mornings he wakes in his pyjamas (the game's first, after the dream; the one after the spanking): the front door shut till he dresses from the chair by his bed", model =>
        {
            var front = model.Zones.FirstOrDefault(z => z.Type == 0 && z.Num == Citadel.CitadelNightHouse.Yard) ?? throw new NotTheGames("the house's front door");
            front.Info[4] = PyjamaDoorSwitch;
            // (each clear of every scenario zone the house has: an actor in two of them is in the later one only -- the dressing zone over the
            // bathroom door's kept its door shut, the user, 2026-11-17: "Twinsen's bathroom door is refusing to open"; and the door's word over
            // the game's own zone 3 at the front door, where Zoe's script lets her out. The word's zone just inside the front door's; the
            // dressing zone east of the chair, short of the bed's zone and south of the bathroom door's)
            var word = new SceneZoneModel { Type = 2, Num = PyjamaDoorTry, X0 = front.X0 - 256, X1 = front.X1 + 256, Y0 = front.Y0, Y1 = front.Y1, Z0 = front.Z0 - 450, Z1 = front.Z0 - 10, Info = new int[8] };
            var dress = new SceneZoneModel { Type = 2, Num = DressZone, X0 = ChairSeatX - 550, X1 = 7 * 512 + 254, Y0 = 2048 - 100, Y1 = 2048 + 1500, Z0 = 1720, Z1 = ChairSeatZ + 900, Info = new int[8] };
            foreach (var mine in new[] { word, dress })
            {
                var under = model.Zones.FirstOrDefault(z => z.Type == 2 && z.X0 <= mine.X1 && mine.X0 <= z.X1 && z.Z0 <= mine.Z1 && mine.Z0 <= z.Z1 && z.Y0 <= mine.Y1 && mine.Y0 <= z.Y1);
                if (under is not null) throw new NotTheGames($"the pyjamas' zone {mine.Num} would lie over the house's zone {under.Num}");
                model.Zones.Add(mine);
            }
            var clothes = Citadel.CitadelProps.Actor(Citadel.CitadelProps.FoldedSuit, ChairSeatX + Citadel.CitadelProps.FoldedShiftX, ChairSeatY, ChairSeatZ + Citadel.CitadelProps.FoldedShiftZ, 0);
            clothes.Armor = WalkOverArmour;
            return p.Add(model, clothes, $@"void comportement_0()
{{
    if ({PyjamasOn} == var_game({PyjamaVar}))
    {{
        // (the game's first morning, chapter 1: his everyday clothes on the chair; after the spanking, his racing suit)
        if (1 >= chapter())
        {{
            body({Citadel.CitadelProps.FoldedCasual});
        }}
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
