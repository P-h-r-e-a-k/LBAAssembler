using System.Text.RegularExpressions;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The car ferry between Citadel Island and the White Leaf Desert (the user, 2026-11-18: "We need to redesign the ferry between Citadel and
// Desert Island into one that Twinsen can drive onto and off again, after Twinsen shows his ferry ticket and drives on, we need to assign
// NoMove whilst the ferry pulls out of the harbour, then back in on the other side until it's successfully docked again"):
//   - the ferry: each scene's hovercraft actor (moved by the game's own tracks) hidden, the car ferry's body (Citadel/CitadelProps.Ferry)
//     where it is -- RACEMOD.CPP car_ferry=: its deck the car's floor, the car on it carried as it moves and turns, held (FerryHold: no
//     input, the bars) from the departure to the docking, and put in his car on its deck as each of the crossing's next scenes starts
//     (FerryInCar);
//   - out: driving over the gangway (Citadel's quay 43: zone 3; the Desert's quay 65: zone 0) Twinsen shows his ticket to Mr. Paul from the
//     car (held while he says it); driven aboard and stopped (FerryAboard, the engine's), the crossing: held, the bars, the ticket used,
//     Mr. Paul to his post and the ferry out of the harbour -- the game's own crossing from there, Twinsen left in his car on the deck, not
//     hidden: 43 -> 59 (the sea) -> 65, 65 -> 60 (the sea) -> 43;
//   - in: docked (Mr. Paul back at his post: his track's 0 at the Desert, 105 at Citadel), the controls back, and he drives off.
//   On foot the game's own way aboard stays (Twinsen hidden aboard).
internal static partial class RaceTrackStory
{
    // the story's game variables for it (the wide ones, from 256: Game variables 0-1023): the car held on the deck; the car aboard and still
    // (the engine's); the ticket shown to Mr. Paul; the next scene's arrival in the car
    internal const int FerryHold = 268, FerryAboard = 269, FerryTicketShown = 270, FerryInCar = 271;
    // the crossing's scenes: Citadel's quay (43: the hovercraft actor 10, Mr. Paul actor 4, the gangway zone 3), the sea on the way out
    // (59: the hovercraft actor 2), the Desert's quay (65: the hovercraft actor 3, Mr. Paul actor 4 again, the gangway zone 0), the sea on
    // the way back (60: the hovercraft actor 13)
    private const int CitadelFerry = 10, QuayFerryman = 4, GangwayZone = 3;
    private const int SeaScene = 59, SeaFerry = 2, DesertQuay = 65, DesertFerry = 3, DesertGangway = 0, SeaBack = 60, SeaBackFerry = 13;
    // the scene variables: Mr. Paul has spoken at the gangway this time; the car has just come off the ferry (no ticket asked on the way
    // off -- again once it's FerryAway from the ferry)
    private const int GangwayAsked = 76, JustArrived = 77, FerryAway = 5000, CitadelArrive = -900;
    // the deck the car stands on (the ferry's own frame: x across, z along; the ramps either side, not the wheelhouse)
    private static readonly int[] FerryDeckBox = { -(Citadel.CitadelProps.FerryHalfWide + Citadel.CitadelProps.FerryRamp), -Citadel.CitadelProps.FerryHalfLong + 200,
        Citadel.CitadelProps.FerryHalfWide + Citadel.CitadelProps.FerryRamp, Citadel.CitadelProps.FerryHouseFrom - 100 };

    // The race file's lines (the story's file: read for every island)
    internal static IEnumerable<string> FerryLines()
    {
        string Line(int scene, int ferry, int body, int deck, int plus = 0, int arrive = 0) =>
            $"car_ferry={scene} {ferry} {Citadel.CitadelProps.Prop} {body} {FerryDeckBox[0]} {FerryDeckBox[1]} {FerryDeckBox[2] + plus} {FerryDeckBox[3]} {deck} {FerryHold} {FerryAboard} {FerryInCar} {arrive}";
        // (back at Citadel's quay the car comes off at x 8,140, the ferry's middle at 9,040: clear of Mr. Paul at his post, x 9,175)
        yield return Line(FerryScene, CitadelFerry, Citadel.CitadelProps.CarFerry, Citadel.CitadelProps.FerryDeck, arrive: CitadelArrive);
        yield return Line(SeaScene, SeaFerry, Citadel.CitadelProps.CarFerry, Citadel.CitadelProps.FerryDeck);
        yield return Line(SeaBack, SeaBackFerry, Citadel.CitadelProps.CarFerry, Citadel.CitadelProps.FerryDeck);
        yield return Line(DesertQuay, DesertFerry, Citadel.CitadelProps.CarFerryTall, Citadel.CitadelProps.FerryDeckTall, Citadel.CitadelProps.FerryRampTall - Citadel.CitadelProps.FerryRamp);
    }

    // A quay's ticket check and departure in the car, at the start of the hero's comportement_1: the ticket asked over the gangway (held
    // while Mr. Paul says it), and, the ticket shown and the car aboard and still, `depart` -- the game's own departure, the car held and
    // the bars first
    // (asked over the gangway -- or on the deck, driven aboard past the gangway's zone: the Desert's is narrow -- once a time on)
    private static string FerryBoarding(int gangway, int ferryActor, string ticketOk, string noTicket, string open, string depart) => $@"    if ({Driving} == comportement_hero() && ({gangway} == zone() || 1 == var_game({FerryAboard})) && 0 == var_cube({GangwayAsked}) && 0 == var_cube({JustArrived}) && 0 == var_game({FerryHold}))
    {{
        set_var_cube({GangwayAsked}, 1);
        set_var_game({FerryHold}, 1);
        if (1 == var_game({FerryTicket}) && {open})
        {{
            message_obj({QuayFerryman}, {ticketOk});
            set_var_game({FerryTicketShown}, 1);
        }}
        else
        {{
            message_obj({QuayFerryman}, {noTicket});
        }}
        set_var_game({FerryHold}, 0);
    }}
    if ({gangway} != zone() && 0 == var_game({FerryAboard}))
    {{
        set_var_cube({GangwayAsked}, 0);
    }}
    if (1 == var_cube({JustArrived}) && {FerryAway} < distance({ferryActor}))
    {{
        set_var_cube({JustArrived}, 0);
    }}
    if ({Driving} == comportement_hero() && 1 == var_game({FerryAboard}) && 1 == var_game({FerryTicketShown}) && 0 == var_game({FerryHold}))
    {{
        set_var_game({FerryHold}, 1);
        set_var_game({FerryTicketShown}, 0);
        cinema_mode(1);
        set_var_game({FerryTicket}, 0);
{depart}        return;
    }}
";
    // ... and the arrival's end: the controls back, the bars off, the camera on him, the ticket not asked on the way off
    private static readonly string FerryDocked = $@"if (1 == var_game({FerryHold}))
{{
set_var_game({FerryHold}, 0);
set_var_cube({JustArrived}, 1);
cinema_mode(0);
cam_follow(0);
";

    private static void CarFerryCitadel(Action<int, string, Func<SceneModel, Action<SceneScripts>>> edit, Func<SceneModel, SceneActorModel, string, Action<SceneScripts>> add,
        Func<ChapterLine, int> say)
    {
        var open = $"0 < var_game({WonVar}) && 1 == var_game({WeatherVar})";
        edit(FerryScene, "the car ferry at Citadel's quay: its deck in the hovercraft's place (RACEMOD.CPP car_ferry=), the ticket shown to Mr. Paul from the car, driven aboard -- held, the bars, the crossing; back from the Desert in the car, held till docked", model =>
        {
            var ferry = model.Actors.ElementAtOrDefault(CitadelFerry) ?? throw new NotTheGames($"no ferry (actor {CitadelFerry})");
            var deck = Citadel.CitadelProps.Actor(Citadel.CitadelProps.CarFerry, ferry.X, ferry.Y, ferry.Z, ferry.Beta);
            var deckEdit = add(model, deck, $@"void comportement_0()
{{
    if ({open} || 7 == var_game(59))
    {{
        invisible(0);
    }}
    else
    {{
        invisible(1);
    }}
}}
");
            return scripts =>
            {
                deckEdit(scripts);
                var hero = scripts.GetText(0, ScriptKind.Life);
                // (on foot only, the game's way aboard: in the car the gangway is the car's)
                hero = Loose(hero, "if (8 != anim() && 14 != anim() && 3 == zone())", $"if (8 != anim() && 14 != anim() && 3 == zone() && {Driving} != comportement_hero())");
                hero = AtStart(hero, "comportement_1", FerryBoarding(GangwayZone, CitadelFerry, $"{CitadelLaterId(say, CitadelLater.FerryTicketOk)}",
                    $"{CitadelLaterId(say, CitadelLater.FerryNoTicket)}", open, $@"        set_var_game(59, 3);
        set_dir_obj({QuayFerryman}, NO_MOVE);
        set_track_obj({QuayFerryman}, label_30);
        set_comportement_obj({QuayFerryman}, comportement_4);
        set_comportement(comportement_2);
"));
                // (out at sea, the film, and on: in the car the next scene starts him in it, on the deck)
                hero = Loose(hero, "change_cube(59);", $"if (1 == var_game({FerryHold}))\n{{\nset_var_game({FerryInCar}, 1);\n}}\nchange_cube(59);");
                // (back from the Desert in the car: not hidden, the bars; docked -- Mr. Paul back at his post -- the controls back)
                hero = Loose(hero, @"if (0 == var_game(249))
{
invisible(1);
set_dir(NO_MOVE);
pos_point(15);", $@"if (0 == var_game(249))
{{
if (1 == var_game({FerryHold}))
{{
cinema_mode(1);
set_comportement(comportement_3);
return;
}}
invisible(1);
set_dir(NO_MOVE);
pos_point(15);");
                hero = Loose(hero, @"if (105 == l_track_obj(4))
{", "if (105 == l_track_obj(4))\n{\n" + FerryDocked + "set_comportement(comportement_1);\nreturn;\n}\n");
                scripts.SetText(0, ScriptKind.Life, hero);
            };
        });
        // the open sea, both ways (59 out, 60 back): the car ferry's deck, Twinsen in his car on it (not hidden: the engine puts him in it),
        // and on in it to the next scene
        edit(SeaScene, "the car ferry at sea (out): its deck in the hovercraft's place, Twinsen in his car on it (RACEMOD.CPP car_ferry=), on to the quay in it", model =>
        {
            var ferry = model.Actors.ElementAtOrDefault(SeaFerry) ?? throw new NotTheGames($"no ferry (actor {SeaFerry})");
            var deckEdit = add(model, Citadel.CitadelProps.Actor(Citadel.CitadelProps.CarFerry, ferry.X, ferry.Y, ferry.Z, ferry.Beta), "void comportement_0()\n{\n}\n");
            return scripts =>
            {
                deckEdit(scripts);
                var hero = scripts.GetText(0, ScriptKind.Life);
                hero = Loose(hero, @"if (7 == var_game(59) || 3 == var_game(59))
{
set_var_game(167, 0);
set_dir(NO_MOVE);", $@"if (7 == var_game(59) || 3 == var_game(59))
{{
set_var_game(167, 0);
if (1 == var_game({FerryHold}))
{{
set_comportement(comportement_2);
return;
}}
set_dir(NO_MOVE);");
                hero = Loose(hero, @"if (10 == l_track_obj(2))
{
if (3 == var_game(59))", $@"if (10 == l_track_obj(2))
{{
if (1 == var_game({FerryHold}))
{{
set_var_game({FerryInCar}, 1);
}}
if (3 == var_game(59))");
                scripts.SetText(0, ScriptKind.Life, hero);
            };
        });
        edit(SeaBack, "the car ferry at sea (back): its deck in the hovercraft's place, Twinsen in his car on it (RACEMOD.CPP car_ferry=), on to Citadel's quay in it", model =>
        {
            var ferry = model.Actors.ElementAtOrDefault(SeaBackFerry) ?? throw new NotTheGames($"no ferry (actor {SeaBackFerry})");
            var deckEdit = add(model, Citadel.CitadelProps.Actor(Citadel.CitadelProps.CarFerry, ferry.X, ferry.Y, ferry.Z, ferry.Beta), "void comportement_0()\n{\n}\n");
            return scripts =>
            {
                deckEdit(scripts);
                var hero = scripts.GetText(0, ScriptKind.Life);
                hero = Loose(hero, @"set_dir(NO_MOVE);
comportement_hero(0);
cam_follow(13);", $@"if (0 == var_game({FerryHold}))
{{
set_dir(NO_MOVE);
comportement_hero(0);
}}
cam_follow(13);");
                hero = Loose(hero, @"pos_point(24);
invisible(1);", $@"if (0 == var_game({FerryHold}))
{{
pos_point(24);
invisible(1);
}}");
                hero = Loose(hero, "change_cube(43);", $"if (1 == var_game({FerryHold}))\n{{\nset_var_game({FerryInCar}, 1);\n}}\nchange_cube(43);");
                scripts.SetText(0, ScriptKind.Life, hero);
            };
        });
    }

    // The Desert's quay (65), from the Desert's story (its lines are the Desert's): the car ferry's deck (a deeper hull: the quay stands
    // higher), the ticket shown to Mr. Paul from the car and the crossing back to Citadel; and arriving from Citadel in the car, held till
    // docked, then driven off
    private static void CarFerryDesertQuay(Action<int, string, Func<SceneModel, Action<SceneScripts>>> edit, Func<SceneModel, SceneActorModel, string, Action<SceneScripts>> add,
        Func<DesertLine, int> say)
    {
        edit(DesertQuay, "the car ferry at the Desert's quay: its deck in the hovercraft's place, the ticket shown to Mr. Paul from the car, driven aboard -- held, the bars, the crossing; from Citadel in the car, held till docked, then driven off", model =>
        {
            var ferry = model.Actors.ElementAtOrDefault(DesertFerry) ?? throw new NotTheGames($"no ferry (actor {DesertFerry})");
            var deckEdit = add(model, Citadel.CitadelProps.Actor(Citadel.CitadelProps.CarFerryTall, ferry.X, ferry.Y, ferry.Z, ferry.Beta), "void comportement_0()\n{\n}\n");
            return scripts =>
            {
                deckEdit(scripts);
                var hero = scripts.GetText(0, ScriptKind.Life);
                // (on foot only, the game's way aboard)
                hero = Loose(hero, "if (14 != anim() && 8 != anim() && 0 == zone())", $"if (14 != anim() && 8 != anim() && 0 == zone() && {Driving} != comportement_hero())");
                hero = AtStart(hero, "comportement_1", FerryBoarding(DesertGangway, DesertFerry, $"{say(DesertLine.FerryTicketOk)}", $"{say(DesertLine.FerryNoTicket)}", "0 <= chapter()", $@"        kill_obj(14);
        kill_obj(15);
        set_var_game(59, 7);
        set_var_cube(3, 1);
        set_comportement_obj({QuayFerryman}, comportement_4);
        set_track_obj({QuayFerryman}, label_30);
        set_camera(3, 1);
        cam_follow({DesertFerry});
        set_comportement(comportement_3);
"));
                // (out, in the car: not hidden as Mr. Paul reaches his post; on to the sea in it)
                hero = Loose(hero, @"oneif (30 == l_track_obj(4))
{
invisible(1);", $@"oneif (30 == l_track_obj(4) && 0 == var_game({FerryHold}))
{{
invisible(1);");
                hero = Loose(hero, "change_cube(60);", $"if (1 == var_game({FerryHold}))\n{{\nset_var_game({FerryInCar}, 1);\n}}\nchange_cube(60);");
                // (in from Citadel, in the car: not hidden, the bars, the camera on the ferry; docked -- Mr. Paul back at his post -- the controls back)
                hero = Loose(hero, @"if (0 == var_game(249))
{
set_dir(NO_MOVE);
comportement_hero(0);", $@"if (0 == var_game(249))
{{
if (1 == var_game({FerryHold}))
{{
cinema_mode(1);
cam_follow({DesertFerry});
set_camera(2, 1);
set_comportement(comportement_2);
return;
}}
set_dir(NO_MOVE);
comportement_hero(0);");
                hero = Loose(hero, @"if (0 == l_track_obj(4))
{
body_obj(14, 38);", "if (0 == l_track_obj(4))\n{\n" + FerryDocked + "set_camera(2, 1);\nset_var_game(59, 4);\nset_comportement(comportement_5);\nreturn;\n}\nbody_obj(14, 38);");
                scripts.SetText(0, ScriptKind.Life, hero);
            };
        });
    }
}
