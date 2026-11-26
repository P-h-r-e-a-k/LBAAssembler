using System.IO;
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
    private const int GangwayAsked = 76, JustArrived = 77, FerryAway = 5000, CitadelArrive = -900, FerryHull = Citadel.CitadelProps.FerryHalfWide;
    // the deck the car stands on (the ferry's own frame: x across, z along; the ramps either side, not the wheelhouse)
    private static readonly int[] FerryDeckBox = { -(Citadel.CitadelProps.FerryHalfWide + Citadel.CitadelProps.FerryRamp), -Citadel.CitadelProps.FerryHalfLong + 200,
        Citadel.CitadelProps.FerryHalfWide + Citadel.CitadelProps.FerryRamp, Citadel.CitadelProps.FerryHouseFrom - 100 };

    // The race file's lines (the story's file: read for every island)
    internal static IEnumerable<string> FerryLines()
    {
        string Line(int scene, int ferry, int body, int deck, int plus = 0, int arrive = 0) =>
            $"car_ferry={scene} {ferry} {Citadel.CitadelProps.Prop} {body} {FerryDeckBox[0]} {FerryDeckBox[1]} {FerryDeckBox[2] + plus} {FerryDeckBox[3]} {deck} {FerryHold} {FerryAboard} {FerryInCar} {arrive} {FerryHull}";
        // (the hull's own deck, for "aboard": not its ramps, nor -- at the Desert's quay, the deck at the quay's height -- the quay beside the
        // bow, which the floor's box takes in with the ramps: there the car counted as aboard, the departure began and Mr. Paul, walking to his
        // post past it, never got there, so the ferry never sailed -- found driving it, 2026-11-20)
        // (back at Citadel's quay the car comes off at x 8,140, the ferry's middle at 9,040: clear of Mr. Paul at his post, x 9,175)
        yield return Line(FerryScene, CitadelFerry, Citadel.CitadelProps.CarFerry, Citadel.CitadelProps.FerryDeck, arrive: CitadelArrive);
        yield return Line(SeaScene, SeaFerry, Citadel.CitadelProps.CarFerry, Citadel.CitadelProps.FerryDeck);
        yield return Line(SeaBack, SeaBackFerry, Citadel.CitadelProps.CarFerry, Citadel.CitadelProps.FerryDeck);
        yield return Line(DesertQuay, DesertFerry, Citadel.CitadelProps.CarFerryTall, Citadel.CitadelProps.FerryDeckTall, Citadel.CitadelProps.FerryRampTall - Citadel.CitadelProps.FerryRamp);
        // (and the game's other scenes of the same four cubes, 197-200 -- reached by its own way, 195 and 196 into 197 -- each with the
        // hovercraft at the same index: the car ferry's look and deck there too, the user, 2026-11-19, "We need to make sure the ferrry on
        // Citadel and Desert island is the correct body when viewed from all cubes nearby")
        yield return Line(AltQuay, CitadelFerry, Citadel.CitadelProps.CarFerry, Citadel.CitadelProps.FerryDeck);
        yield return Line(AltSea, SeaFerry, Citadel.CitadelProps.CarFerry, Citadel.CitadelProps.FerryDeck);
        yield return Line(AltDesertQuay, DesertFerry, Citadel.CitadelProps.CarFerryTall, Citadel.CitadelProps.FerryDeckTall, Citadel.CitadelProps.FerryRampTall - Citadel.CitadelProps.FerryRamp);
        yield return Line(AltSeaBack, SeaBackFerry, Citadel.CitadelProps.CarFerry, Citadel.CitadelProps.FerryDeck);
    }

    private const int AltQuay = 197, AltSea = 198, AltDesertQuay = 199, AltSeaBack = 200;
    // The hovercraft the island itself draws at Citadel's mooring -- CITADEL.OBL body 56 and CITABAU.OBL 86, a decor shown while game
    // variable 166 is on (the game's scenes 197-200 set it) -- seen from every cube about: the car ferry's body in its place, as the
    // crossing's scenes show it (same place, same facing as the ferry actor at rest there).
    internal static List<string> FerryDecor(string gameDirectory)
    {
        var log = new List<string>();
        var bodies = Path.Combine(gameDirectory, "BODY.HQR");
        if (!File.Exists(bodies)) return log;
        var header = LbaBodyStudio.Body.Read(HqrArchive.Open(bodies).Read(30), 2, allowStatic: true).Header;
        foreach (var (file, entry) in new[] { ("CITADEL.OBL", 56), ("CITABAU.OBL", 86) })
        {
            var path = Path.Combine(gameDirectory, file);
            if (!File.Exists(path) || HqrArchive.CountEntries(path) <= entry) { log.Add($"the car ferry at Citadel's mooring left out of {file}: no body {entry}"); continue; }
            HqrWriter.ReplaceInFile(path, entry, Citadel.CitadelProps.BuildCarFerry(header).Write());
            log.Add($"the car ferry at Citadel's mooring: {file} body {entry} (the hovercraft's decor) the car ferry");
        }
        return log;
    }

    // A quay's ticket check and departure in the car, at the start of the hero's comportement_1: the ticket asked over the gangway (held
    // while Mr. Paul says it), and, the ticket shown and the car aboard and still, `depart` -- the game's own departure, the car held and
    // the bars first
    // (asked over the gangway -- or on the deck, driven aboard past the gangway's zone: the Desert's is narrow -- once a time on: asked
    // again only once he has driven FerryAway from the ferry -- cleared as he left the gangway's zone, the stretch between it and the
    // engine counting him aboard asked him twice, the user, 2026-11-20: "once before he drives on, and once when he's already on")
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
    if ({gangway} != zone() && 0 == var_game({FerryAboard}) && {FerryAway} < distance({ferryActor}))
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

    // The ferry in every chapter (the user, 2026-11-26: "After Twinsen wins the race on Desert Island the Ferry is gone, this is probably
    // due to the retail chapter scripting, can we change this please so the ferry is always available"): the game takes the hovercraft and
    // Mr. Paul away from both quays after its chapter 3 -- the story's Desert race won is its chapter 4 -- and the Desert's arrival no
    // longer sees the hovercraft drawn (game variable 167); in its other scenes of the quays (197, 199) the same. Their chapter test made
    // one that never holds; Citadel's Mr. Paul still comes with chapter 2, as the crossing does. (The Desert's ferry office's clerk is in
    // every chapter already: Travels.)
    private static void FerryEveryChapter(Action<int, string, Func<SceneModel, Action<SceneScripts>>> edit, params int[] scenes)
    {
        foreach (var scene in scenes)
            edit(scene, "the ferry and Mr. Paul at the quay in every chapter -- not gone after the game's chapter 3", _ => scripts =>
            {
                void Keep(int actor, string from, string to, bool needed = true)
                {
                    var life = scripts.GetText(actor, ScriptKind.Life);
                    if (!life.Contains(from)) { if (needed) throw new NotTheGames($"actor {actor}'s \"{from}\""); return; }
                    scripts.SetText(actor, ScriptKind.Life, life.Replace(from, to));
                }
                if (scene is FerryScene or AltQuay)
                {
                    Keep(CitadelFerry, "if (3 < chapter())", "if (0 > chapter())");
                    Keep(QuayFerryman, "if (4 == chapter() || 2 > chapter())", "if (2 > chapter())");
                }
                else
                {
                    Keep(DesertFerry, "if (3 < chapter())", "if (0 > chapter())");
                    Keep(QuayFerryman, "if (3 < chapter())", "if (0 > chapter())");
                    Keep(0, "3 == var_game(59) || 3 < chapter() || 0 == processor()", "3 == var_game(59) || 0 == processor()", needed: false);
                }
            });
    }

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

    // The Desert's quay: the ferry's berth opposite the slope (the user, 2026-11-23: "On Desert Island let's have the ferry park opposite the
    // slope to make driving onto it easier, we'll also need to move the grobo at the same time"). The slope comes up from the cobbles to the
    // quay's walkway (1,360) from the east across z 14,700-16,700; the ferry was moored 6,000 further north (z 9,344), the car up the slope,
    // north along the walkway and west at the gangway. Now its berth is 12 bricks (6,144) south, level with the slope's top: up the slope,
    // over the walkway and straight on to its deck. Moved with it, by the same: the ferry's way in along x 9,616 and its berth (points 1, 2,
    // 3, 5) and its way out (20); Mr. Paul's post, his place at the gangway and Twinsen's there (4, 6, 7) and Mr. Paul himself; the gangway
    // (scenario zone 0) and the crossing's two cameras (zones 2 and 3, their targets in bricks). Its way in from the sea (0) and out (8) as
    // they were.
    private const int BerthShift = 12 * 512;
    // (and its way in runs past the berth only so far before it backs in -- point 3, FerryOvershoot past the berth, point 2: moved south
    // with the rest, the game's 4,736 ran its bow and cabin into the basin's south wall -- the user, 2026-11-24, "When the ferry is
    // manouvering into place it clips into the side of the quay")
    // (point 1, where its way in from the sea comes to the berth's line, FerryTurnIn past it: it runs on a little past each point)
    private const int FerryOvershoot = 1300, FerryTurnIn = 900;
    private static void ShiftBerth(SceneModel model)
    {
        foreach (var i in new[] { 1, 2, 3, 5, 20, 4, 6, 7 })
            if (i < model.TrackPoints.Count) model.TrackPoints[i] = model.TrackPoints[i] with { Z = model.TrackPoints[i].Z + BerthShift };
        if (model.TrackPoints.Count > 3)
        {
            model.TrackPoints[1] = model.TrackPoints[1] with { Z = model.TrackPoints[2].Z + FerryTurnIn };
            model.TrackPoints[3] = model.TrackPoints[3] with { Z = model.TrackPoints[2].Z + FerryOvershoot };
        }
        foreach (var z in model.Zones)
        {
            if (!((z.Type == 2 && z.Num == 0) || (z.Type == 1 && z.Num is 2 or 3))) continue;
            z.Z0 += BerthShift; z.Z1 += BerthShift;
            if (z.Type == 1) z.Info[2] += BerthShift / 512;
        }
        if (model.Actors.ElementAtOrDefault(QuayFerryman) is { } paul) paul.Z += BerthShift;
    }

    // The Desert's quay (65), from the Desert's story (its lines are the Desert's): the car ferry's deck (a deeper hull: the quay stands
    // higher), the ticket shown to Mr. Paul from the car and the crossing back to Citadel; and arriving from Citadel in the car, held till
    // docked, then driven off
    private static void CarFerryDesertQuay(Action<int, string, Func<SceneModel, Action<SceneScripts>>> edit, Func<SceneModel, SceneActorModel, string, Action<SceneScripts>> add,
        Func<DesertLine, int> say)
    {
        // (the game's other scene of the same cube: its ferry at the same berth)
        edit(AltDesertQuay, "the Desert's quay (the game's other scene of it): the ferry's berth opposite the slope", model =>
        {
            ShiftBerth(model);
            return _ => { };
        });
        edit(DesertQuay, "the car ferry at the Desert's quay: its deck in the hovercraft's place, the ticket shown to Mr. Paul from the car, driven aboard -- held, the bars, the crossing; from Citadel in the car, held till docked, then driven off", model =>
        {
            ShiftBerth(model);
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
