using LBAAssembler.Scenes;
using LBAAssembler.LbaScript;

namespace LBAAssembler.Terrain;

// Stella Entities' lounge (the user, 2026-11-13): "Let's add some booths around the edge of the room, and some cocktail waitresses";
// "chairs that Twinsen can actually sit on ... Twinsen already has a sitting animation that we can reuse. If Twinsen is sat down a girl comes
// over to him and starts talking to him, and asks him if he wants a private show"; "make this super expensive ... 250 zilitos and add in a
// currency change machine where the player gets ... 2 kashes into 1 zilito"; "the curtained booths in a back area that a dancer can lead him
// off into".
//   - the booths (CitadelProps.ClubBooth at BoothAngles): Action in front of one (its zone) sits Twinsen on its seat (his sitting animation,
//     CitadelSpanking.TwinsenSit, as at the Tralu's bar); Action again stands him up in front of it;
//   - sat a while, a hostess walks over from beside the catwalk round the hall (never across the catwalk) and asks -- the two take turns,
//     the rabbibunny first: a private show, 250 zlitos?; the Grobo: a lap dance, 50 zlitos? (StellaStaff, the user, 2026-11-13). Yes and he
//     has them: paid, "Follow me", into the private room (StellaRooms.Private); not enough: the change machine by the bar; no: she goes
//     back, and the other comes later if he stays;
//   - the private room: Twinsen on the sofa, the one he paid dancing before him (the rabbibunny in a thong, or the stage's Grobo), some
//     seconds of her dance, her thanks, back out into the hall (who: SeatVar 3 or 4, set as he pays);
//   - two waitresses walk round the hall between the booths and the bar, trays of cocktails in hand (the rabbibunny again);
//   - the change machine against the wall by the bar's end: Action -- change the kashes for zlitos? -- ExchangeVar (RACEMOD.CPP exchange=:
//     two kashes a zlito). The hall's and the private room's money is zlitos (zlitos_scene=), what the scripts count and pay and the counter shows.
internal static partial class RaceTrackStory
{
    internal const int ExchangeVar = 196;
    // (seated in a booth: 1; the engine makes it 2 at a key -- RACEMOD.CPP seat_var= -- and he gets up)
    internal const int SeatVar = 210;
    private const int StewardessScene = 41, StewardessActor = 6;
    private const int BoothZone0 = 80, SitLabel = 100, StandLabel = 120, HostGo = 200, HostAt = 210, HostBack = 220, HostHome = 230;
    private const int SeatBooth = 60, SeatWait = 61, Offered = 62, LaterRounds = 63, ShowFrames = 0, ShowRounds = 1;
    // (turned down -- or too dear -- she comes again only after LaterAfter rounds of 250 frames more, if he's still sat there)
    private const int LaterAfter = 4;
    private const int ShowPrice = 250, LapPrice = 50, ShowWait = 200, ReturnSwitch = 7;
    // (who comes next to his booth: 0 the rabbibunny, 1 the Grobo; who he paid, SeatVar: 3 the rabbibunny, 4 the Grobo; in the private room,
    // who dances: 1 the Grobo)
    private const int NextHost = 64, PaidBunny = 3, PaidGrobo = 4, ShowWho = 2;
    // (into the private room: a door zone over each booth's seat, off till the hostess's "Follow me" -- change_cube takes scenes up to 255)
    private const int PrivateSwitch = 8;
    private const float LoungePath = 13.3f, HostHomeAngle = 250, GroboHomeAngle = 236, HostHomeCells = 15.5f, FrontCells = 15.9f;

    private static void StellaLounge(IslandBeatsA b, Func<MoonLine, int> say)
    {
        if (!b.Store.SceneExists(StellaEntities.Scene) || !b.Store.SceneExists(StellaRooms.Private)) { b.Log.Add("Stella Entities' lounge left out: no hall or no private room"); return; }
        var stewardess = Copy(b.Originals, StewardessScene, StewardessActor);
        var bouncer = CopyOf(b.Originals, RickBar, BouncerEntity);
        if (stewardess is null || bouncer is null) { b.Log.Add("Stella Entities' lounge left out: no stewardess (scene 41's actor 6) or no bouncer to copy"); return; }
        var angles = Citadel.CitadelProps.BoothAngles;
        b.Edit(StellaEntities.Scene, "Stella Entities' lounge: booths to sit in, the hostess's private show, the waitresses, the change machine", model =>
        {
            var adds = new List<Action<SceneScripts>>();
            int Point(float degrees, float cells, int y)
            {
                var (x, z) = StellaEntities.At(degrees, cells);
                return SceneOps.AddTrackPoint(model, new SceneTrackPoint(x, y, z));
            }
            // (round the hall, the angle going down -- the catwalk at 270 never crossed)
            List<int> Round(float from, float to)
            {
                var pts = new List<int>();
                for (var a = from; a > to; a -= 15) pts.Add(Point(a, LoungePath, StellaEntities.FloorY));
                pts.Add(Point(to, LoungePath, StellaEntities.FloorY));
                return pts;
            }
            // the benches, their zones, the seats and the places in front of them
            var seats = new List<int>(); var fronts = new List<int>(); var faces = new List<int>();
            for (var k = 0; k < angles.Length; k++)
            {
                var (bx, bz) = StellaEntities.At(angles[k], Citadel.CitadelProps.BoothCells);
                var bench = Citadel.CitadelProps.Actor(Citadel.CitadelProps.ClubBooth, bx, StellaEntities.FloorY, bz, StellaEntities.FacingMiddle(bx, bz));
                bench.Armor = WalkOverArmour;
                SceneOps.AddActor(model, bench);
                seats.Add(Point(angles[k], Citadel.CitadelProps.SeatCells, StellaEntities.FloorY + Citadel.CitadelProps.BoothSeat));
                fronts.Add(Point(angles[k], FrontCells, StellaEntities.FloorY));
                faces.Add(StellaEntities.FacingMiddle(bx, bz));
                var (zx, zz) = StellaEntities.At(angles[k], 16.0f);
                var (sx, sz) = StellaEntities.At(angles[k], Citadel.CitadelProps.SeatCells);
                var seatY = StellaEntities.FloorY + Citadel.CitadelProps.BoothSeat;
                model.Zones.Add(new SceneZoneModel { Type = 0, Num = StellaRooms.Private, X0 = sx - 300, X1 = sx + 300, Z0 = sz - 300, Z1 = sz + 300, Y0 = seatY - 100, Y1 = seatY + 1500,
                    Info = new[] { StellaRooms.SofaX - 300, 256 + Citadel.CitadelProps.BoothSeat - 100, StellaRooms.SofaZ - 300, 0, PrivateSwitch, 0, 0, 0 } });
                model.Zones.Add(new SceneZoneModel { Type = 2, Num = BoothZone0 + k, X0 = zx - 400, X1 = zx + 400, Z0 = zz - 400, Z1 = zz + 400, Y0 = StellaEntities.FloorY - 100, Y1 = StellaEntities.FloorY + 1500, Info = new int[8] });
            }
            // the hostesses (StellaStaff: the rabbibunny, the Grobo): home beside the catwalk's foot; each one's way to each booth and back
            var (hx, hz) = StellaEntities.At(HostHomeAngle, HostHomeCells);
            (int Index, string Track) Hostess(float homeAngle, int body, int stand, int walk)
            {
                var host = stewardess.Clone();
                var (x, z) = StellaEntities.At(homeAngle, HostHomeCells);
                (host.X, host.Y, host.Z) = (x, StellaEntities.FloorY, z);
                host.Beta = StellaEntities.FacingMiddle(x, z);
                host.Entity = StellaStaff.Entity; host.Body = body; host.Anim = stand;
                host.Move = 3; host.Flags &= ~1u;
                var index = model.Actors.Count;
                adds.Add(IslandBeatsA.Add(model, host, "void comportement_0()\n{\n}\n"));
                var homePoint = Point(homeAngle, HostHomeCells, StellaEntities.FloorY);
                var track = new System.Text.StringBuilder($"label(0);\nanim({stand});\nstop();\n\nlabel({HostHome});\nanim({stand});\nstop();\n");
                for (var k = 0; k < angles.Length; k++)
                {
                    var to = angles[k] < homeAngle ? angles[k] : angles[k] - 360;
                    var way = Round(homeAngle, to);
                    track.Append($"\nlabel({HostGo + k});\nanim({walk});\n{string.Concat(way.Select(p => $"goto_point({p});\n"))}goto_point({fronts[k]});\nanim({stand});\nbeta({(faces[k] + 2048) & 4095});\n\nlabel({HostAt + k});\nstop();\n");
                    track.Append($"\nlabel({HostBack + k});\nanim({walk});\n{string.Concat(Enumerable.Reverse(way).Select(p => $"goto_point({p});\n"))}goto_point({homePoint});\nanim({stand});\nbeta({host.Beta});\nstop();\n");
                }
                return (index, track.ToString());
            }
            var (hostIndex, hostTrack) = Hostess(HostHomeAngle, StellaStaff.Bunny, StellaStaff.BunnyStand, StellaStaff.BunnyWalk);
            var (groboIndex, groboTrack) = Hostess(GroboHomeAngle, StellaStaff.GroboHost, StellaStaff.GroboStand, StellaStaff.GroboWalk);
            // two waitresses, back and forth round the hall between the booths and the bar
            var waits = new List<(int Index, string Track)>();
            foreach (var (from, to) in new[] { (245f, 175f), (95f, -35f) })
            {
                var w = stewardess.Clone();
                var (wx, wz) = StellaEntities.At(from, LoungePath);
                (w.X, w.Y, w.Z) = (wx, StellaEntities.FloorY, wz);
                w.Entity = StellaStaff.Entity; w.Body = StellaStaff.BunnyTray; w.Anim = StellaStaff.TrayStand;
                w.Move = 3; w.Flags &= ~1u;
                var wi = model.Actors.Count;
                adds.Add(IslandBeatsA.Add(model, w, "void comportement_0()\n{\n}\n"));
                var way = Round(from, to);
                int walk = StellaStaff.TrayWalk, stand = StellaStaff.TrayStand;
                waits.Add((wi, $"label(0);\nanim({walk});\n{string.Concat(way.Select(p => $"goto_point({p});\n"))}anim({stand});\nwait_nb_dizieme(40);\nanim({walk});\n" +
                               $"{string.Concat(Enumerable.Reverse(way).Select(p => $"goto_point({p});\n"))}anim({stand});\nwait_nb_dizieme(40);\ngoto(label_0);\n"));
            }
            // the twinkling stars on the night sky, each its own beat, bright and dim (the user, 2026-11-13: "let's have some of the stars
            // twinkling emitting light around the room")
            var stars = new List<(int Index, string Track)>();
            var twinkle = new Random(11);
            for (var a = Citadel.CitadelProps.ClubWallFrom + 9; a < Citadel.CitadelProps.ClubWallTo - 5; a += 13)
            {
                if (a > Citadel.CitadelProps.BackstageAt - 20 && a < Citadel.CitadelProps.BackstageAt + 20) continue;   // (not over the club's name)
                var (sx, sz) = StellaEntities.At(a, Citadel.CitadelProps.ClubWallRadius / 512f - 0.12f);
                var y = StellaEntities.FloorY + 1700 + twinkle.Next(1600);
                var star = Citadel.CitadelProps.Actor(Citadel.CitadelProps.ClubStar, sx, y, sz, StellaEntities.FacingMiddle(sx, sz));
                star.Armor = WalkOverArmour;
                star.Move = 3;   // (its track: the twinkle)
                var si = model.Actors.Count;
                adds.Add(IslandBeatsA.Add(model, star, "void comportement_0()\n{\n}\n"));
                // (slow: bright 1.2-2.7 s, dim 0.8-1.7 s -- the user, 2026-11-14: "we need to slow the flashing down"; it was 0.3-1.0 and 0.2-0.6)
                int on = 12 + twinkle.Next(16), off = 8 + twinkle.Next(10);
                stars.Add((si, $"label(0);\nbody({Citadel.CitadelProps.ClubStar});\nwait_nb_dizieme({on});\nbody({Citadel.CitadelProps.ClubStarDim});\nwait_nb_dizieme({off});\ngoto(label_0);\n"));
            }
            // the change machine
            var (mx, mz) = StellaEntities.At(Citadel.CitadelProps.MachineAngle, Citadel.CitadelProps.MachineCells);
            var machine = Citadel.CitadelProps.Actor(Citadel.CitadelProps.ChangeMachine, mx, StellaEntities.FloorY, mz, StellaEntities.FacingMiddle(mx, mz));
            var machineIndex = model.Actors.Count;
            adds.Add(IslandBeatsA.Add(model, machine, $@"void comportement_0()
{{
    swif (1 == action())
    {{
        if (1100 > distance(0) && {Driving} != comportement_hero())
        {{
            add_choice({say(MoonLine.ClubMachineYes)});
            add_choice({say(MoonLine.ClubMachineNo)});
            ask_choice_obj({machineIndex}, {say(MoonLine.ClubMachineAsk)});
            if ({say(MoonLine.ClubMachineYes)} == choice())
            {{
                set_var_game({ExchangeVar}, 1);
                message_obj({machineIndex}, {say(MoonLine.ClubMachineDone)});
            }}
        }}
    }}
}}
"));
            // the lounge's keeper: sitting down and standing up, the hostess's offer
            string ForBooth(Func<int, string> body) => string.Concat(Enumerable.Range(0, angles.Length).Select(k => $"        if ({k} == var_cube({SeatBooth}))\n        {{\n{body(k)}        }}\n"));
            string sit = string.Concat(Enumerable.Range(0, angles.Length).Select(k => $@"            if ({BoothZone0 + k} == zone_obj(0))
            {{
                comportement_hero(0);
                set_var_game({SeatVar}, 1);
                set_dir_obj(0, NO_MOVE);
                set_track_obj(0, label_{SitLabel + k});
                set_var_cube({SeatBooth}, {k});
                set_var_cube({SeatWait}, 0);
                set_var_cube({Offered}, 0);
                set_var_cube({LaterRounds}, 0);
                set_comportement(comportement_2);
            }}
"));
            var keeper = RaceTrackScenes.InvisibleHelper(hx, StellaEntities.FloorY, hz);
            adds.Add(IslandBeatsA.Add(model, keeper, $@"void comportement_0()
{{
    set_var_game({SeatVar}, 0);
    set_change_cube({PrivateSwitch}, 0);
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if ({Driving} != comportement_hero())
        {{
{sit}        }}
    }}
}}

void comportement_2()
{{
    if (250 > var_cube({SeatWait}))
    {{
        add_var_cube({SeatWait}, 1);
    }}
    if (2 == var_game({SeatVar}))
    {{
{ForBooth(k => $"            set_track_obj(0, label_{StandLabel + k});\n")}        set_dir_obj(0, MOVE_MANUAL);
        set_var_game({SeatVar}, 0);
        set_comportement(comportement_1);
    }}
    if (2 == var_cube({Offered}) && 249 < var_cube({SeatWait}))
    {{
        set_var_cube({SeatWait}, 0);
        add_var_cube({LaterRounds}, 1);
        if ({LaterAfter} < var_cube({LaterRounds}))
        {{
            set_var_cube({Offered}, 0);
        }}
    }}
    if (0 == var_cube({Offered}) && {ShowWait} < var_cube({SeatWait}))
    {{
        set_var_cube({Offered}, 1);
        if (0 == var_cube({NextHost}))
        {{
{ForBooth(k => $"            set_track_obj({hostIndex}, label_{HostGo + k});\n")}        set_comportement(comportement_3);
        }}
        else
        {{
{ForBooth(k => $"            set_track_obj({groboIndex}, label_{HostGo + k});\n")}        set_comportement(comportement_4);
        }}
    }}
}}

void comportement_3()
{{
    if ({HostAt} <= l_track_obj({hostIndex}) && {HostAt + angles.Length} > l_track_obj({hostIndex}))
    {{
        add_choice({say(MoonLine.ClubShowYes)});
        add_choice({say(MoonLine.ClubShowNo)});
        ask_choice_obj({hostIndex}, {say(MoonLine.ClubOffer)});
        if ({say(MoonLine.ClubShowYes)} == choice() && {ShowPrice - 1} < nb_gold_pieces())
        {{
            give_gold_pieces({ShowPrice});
            set_var_game({SeatVar}, {PaidBunny});
            message_obj({hostIndex}, {say(MoonLine.ClubFollow)});
            set_dir_obj(0, MOVE_MANUAL);
            set_change_cube({PrivateSwitch}, 1);
        }}
        else
        {{
            if ({say(MoonLine.ClubShowYes)} == choice())
            {{
                message_obj({hostIndex}, {say(MoonLine.ClubBroke)});
            }}
            else
            {{
                message_obj({hostIndex}, {say(MoonLine.ClubLater)});
            }}
{ForBooth(k => $"            set_track_obj({hostIndex}, label_{HostBack + k});\n")}            set_var_cube({SeatWait}, 0);
            set_var_cube({LaterRounds}, 0);
            set_var_cube({Offered}, 2);
            set_var_cube({NextHost}, 1);
            set_comportement(comportement_2);
        }}
    }}
}}

void comportement_4()
{{
    if ({HostAt} <= l_track_obj({groboIndex}) && {HostAt + angles.Length} > l_track_obj({groboIndex}))
    {{
        add_choice({say(MoonLine.ClubShowYes)});
        add_choice({say(MoonLine.ClubShowNo)});
        ask_choice_obj({groboIndex}, {say(MoonLine.ClubLapOffer)});
        if ({say(MoonLine.ClubShowYes)} == choice() && {LapPrice - 1} < nb_gold_pieces())
        {{
            give_gold_pieces({LapPrice});
            set_var_game({SeatVar}, {PaidGrobo});
            message_obj({groboIndex}, {say(MoonLine.ClubFollow)});
            set_dir_obj(0, MOVE_MANUAL);
            set_change_cube({PrivateSwitch}, 1);
        }}
        else
        {{
            if ({say(MoonLine.ClubShowYes)} == choice())
            {{
                message_obj({groboIndex}, {say(MoonLine.ClubLapBroke)});
            }}
            else
            {{
                message_obj({groboIndex}, {say(MoonLine.ClubLater)});
            }}
{ForBooth(k => $"            set_track_obj({groboIndex}, label_{HostBack + k});\n")}            set_var_cube({SeatWait}, 0);
            set_var_cube({LaterRounds}, 0);
            set_var_cube({Offered}, 2);
            set_var_cube({NextHost}, 0);
            set_comportement(comportement_2);
        }}
    }}
}}
"));
            var sitDown = string.Concat(Enumerable.Range(0, angles.Length).Select(k =>
                $"\nlabel({SitLabel + k});\npos_point({seats[k]});\nbeta({faces[k]});\nanim({Citadel.CitadelSpanking.TwinsenSit});\nstop();\n\nlabel({StandLabel + k});\npos_point({fronts[k]});\nbeta({faces[k]});\nanim(0);\nstop();\n"));
            return scripts =>
            {
                foreach (var add in adds) add(scripts);
                scripts.SetText(hostIndex, ScriptKind.Track, hostTrack);
                scripts.SetText(groboIndex, ScriptKind.Track, groboTrack);
                foreach (var (wi, track) in waits) scripts.SetText(wi, ScriptKind.Track, track);
                foreach (var (si, track) in stars) scripts.SetText(si, ScriptKind.Track, track);
                var heroTrack = scripts.GetText(0, ScriptKind.Track).TrimEnd();
                if (!heroTrack.Contains("label(")) heroTrack += "\nlabel(0);\nstop();";
                scripts.SetText(0, ScriptKind.Track, heroTrack + "\n" + sitDown);
            };
        });

        // the private room: Twinsen on the sofa, a dancer before him, her dance a while, her thanks, and back out into the hall
        b.Edit(StellaRooms.Private, "Stella Entities' private room: a dancer's show for Twinsen on the sofa, then back to the hall", model =>
        {
            var adds = new List<Action<SceneScripts>>();
            int Dancer(int body, int anim)
            {
                var dancer = bouncer.Clone();
                (dancer.X, dancer.Y, dancer.Z) = (StellaRooms.SofaX, 256, StellaRooms.ShowZ);
                dancer.Entity = StellaStaff.Entity; dancer.Body = body; dancer.Anim = anim;
                dancer.Flags &= ~1u; dancer.Flags |= NoShadowFlag; dancer.Armor = WalkOverArmour;
                dancer.Beta = 2048; dancer.Move = 0;
                var index = model.Actors.Count;
                adds.Add(IslandBeatsA.Add(model, dancer, "void comportement_0()\n{\n}\n"));
                return index;
            }
            var bunnyIndex = Dancer(StellaStaff.BunnyThong, StellaStaff.BunnyLap);
            var groboIndex = Dancer(StellaPerformers.Body0, StellaStaff.GroboLap);
            // (the way back: a door zone round the sofa, off till the show's end -- into the hall in front of the booths)
            var (rx, rz) = StellaEntities.At(215, 15.5f);
            model.Zones.Add(new SceneZoneModel { Type = 0, Num = StellaEntities.Scene, X0 = StellaRooms.SofaX - 300, X1 = StellaRooms.SofaX + 300, Z0 = StellaRooms.SofaZ - 300, Z1 = StellaRooms.SofaZ + 300, Y0 = 0, Y1 = 2000,
                Info = new[] { rx - 300, StellaEntities.FloorY, rz - 300, 0, ReturnSwitch, 0, 0, 0 } });
            var heroLife = $@"void comportement_0()
{{
    set_change_cube({ReturnSwitch}, 0);
    set_var_cube({ShowWho}, 0);
    if ({PaidGrobo} == var_game({SeatVar}))
    {{
        set_var_cube({ShowWho}, 1);
        kill_obj({bunnyIndex});
    }}
    else
    {{
        kill_obj({groboIndex});
    }}
    set_var_game({SeatVar}, 0);
    set_dir(NO_MOVE);
    beta(0);
    anim({Citadel.CitadelSpanking.TwinsenSit});
    set_var_cube({ShowFrames}, 0);
    set_var_cube({ShowRounds}, 0);
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    add_var_cube({ShowFrames}, 1);
    if (200 < var_cube({ShowFrames}))
    {{
        set_var_cube({ShowFrames}, 0);
        add_var_cube({ShowRounds}, 1);
    }}
    if (3 < var_cube({ShowRounds}))
    {{
        if (1 == var_cube({ShowWho}))
        {{
            message_obj({groboIndex}, {say(MoonLine.ClubThanks)});
        }}
        else
        {{
            message_obj({bunnyIndex}, {say(MoonLine.ClubThanks)});
        }}
        anim(0);
        set_dir(MOVE_MANUAL);
        set_change_cube({ReturnSwitch}, 1);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
}}
";
            return scripts =>
            {
                foreach (var add in adds) add(scripts);
                scripts.SetText(0, ScriptKind.Life, heroLife);
            };
        });
    }
}
