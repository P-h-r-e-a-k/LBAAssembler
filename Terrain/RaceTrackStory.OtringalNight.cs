using System.Text.RegularExpressions;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Otringal's night between qualifying and race day, and the coil's fetch quest (the user, 2026-11-21/22: "let's have Twinsen told that all
// the cars are going to be held over night in the palace, and suggested to him that he get a room for the night in the Imperial hotel. When
// Twinsen arrives at the race track the following day then he gets told his car is missing something, and let's make it so Twinsen's car
// doesn't drive until he's managed to fix it"; "let's change the response when Twinsen asks 'Do you know where I can get an ignition coil?' To
// 'Maybe yes, maybe no ...' ... take a love letter to the singer in Rick's bar ... he agrees to sell Twinsen the ignition coil for 10 zilitos,
// and tells Twinsen 'Somebody dressed all in black and wearing a mask sold it to him yesterday'"; "We need to make Twinsen's opening line
// when he talks to somebody relevant to his current objective"; the hotel's upper floor, OtringalHotel):
//   - qualified (the day's variable DayQualified): out of his car by the line, the marshal -- every car in the Emperor's palace overnight, a
//     room at the Imperial Hotel (Q_NIGHT 1); the race file's car_held= keeps his car away (in the palace's room, scene 80) all night;
//   - the hotel's reception (a desk in the lobby, a clerk in the doorman's uniform): a room -- the Classic Room 100 zlitos, the Junior Suite
//     250, the Imperial Suite 500 (Q_NIGHT 2-4); the lift in the alcove's wall: in, the doors shut, the ride, out on the first floor
//     (LIFT_RIDE); the corridor: his room's door open to him (its zone), the others locked; in his room, Action by the bed: asleep, the night,
//     the morning -- race day -- and his word on the bed (the dearer, the better he slept);
//   - race day: his car back on the track (car_held= lets it go), its coil gone: at the line the marshal says so (Q_COIL 1), and the car
//     won't start (car_dead=) till the coil is back. Asked about a coil, Rick, Johnny, Lyra, the clerk and the marshal all send him to the
//     Lower City's gazogem seller; the seller: "maybe yes, maybe no" -- a love letter for the singer at Rick's bar (Q_COIL 2); she's on the
//     stage while he has it (game variable 131, the stage's turn), reads it, writes back (3); the seller, overjoyed, sells him the coil for 10
//     zlitos -- a masked man in black sold it him yesterday (4, the day DayCoil: the car goes).
internal static partial class RaceTrackStory
{
    // Q_COIL: 1 told the coil's gone, 2 the seller's letter carried, 3 the singer's answer carried, 4 the coil bought
    public const int QCoil = 276, CoilTold = 1, CoilLetter = 2, CoilAnswer = 3, CoilBought = 4;
    // Q_NIGHT: 1 told to take a room, 2 the Classic Room, 3 the Junior Suite, 4 the Imperial Suite; LIFT_RIDE: 1 going up, 2 going down (the
    // scene it comes to opens its doors for him)
    public const int QNight = 277, NightTold = 1, NightClassic = 2, NightJunior = 3, NightSuite = 4, HotelLiftRide = 278;
    private const int PriceClassic = 100, PriceJunior = 250, PriceSuite = 500, CoilPrice = 10, PalaceRoom = 80;
    // Rick's bar: the singer (actor 28) on the stage while game variable 131 is 2 -- the guitarist (24) at 0 and 3
    private const int Singer = 28;
    // her room behind the stage (scene 134, in from the stage's back door): the game's singer sat at her dressing table there, actor 8 --
    // her walking self, actor 11, hands over to it as she sits down
    private const int SingerRoom = 134, SingerSeated = 8;
    // the lobby: the clerk (the doorman's uniform: scene 81's actor 8) by the lift, beside its doors, facing the room -- the reception desk
    // that stood facing the street's door is gone, and the sofa at the alcove's edge (the user, 2026-11-23: the desk "is ugly and seems to
    // have no point. It also stops Twinsen getting inside to speak to the race marshall"; the sofa "near the pool needs removing")
    // (since 2026-11-27 behind a counter again -- the user, 2026-11-26: "we need to change the imperial hotel so the receptionist is behind
    // the counter. Let's remodel the counter to a more traditional look complete with keys and pigeon holes": HotelProps.ReceptionCounter,
    // its long side along z facing the room (+x), in front of him, its north end clear of the lift's doors; its keys' cabinet behind him.
    // He a little forward from where he stood, between the two)
    private static readonly (int X, int Z) ClerkAt = (OtringalHotel.LobbyLift.X + 824, OtringalHotel.LobbyLift.Z + 1700);
    private static readonly (int X, int Z) CounterAt = (OtringalHotel.LobbyLift.X + 1474, OtringalHotel.LobbyLift.Z + 1700);
    // the pool's day pass (game variable 300: 1 bought, or a room taken -- a guest swims) and its price; the engine keeps whoever hasn't one
    // out of the water (RACEMOD.CPP pool_pass=: back on the deck at the steps' foot) -- the user, 2026-11-26, "allow Twinsen to be able to
    // pay 50 zilitos for a day pass to the hotel to be able to use the swimming pool"
    public const int PoolPass = 300, PoolPassPrice = 50;
    internal static readonly (int X, int Y, int Z, int Beta) PoolOutAt = (16900, 2048, 8400, 0);
    internal const string PoolPassText = "The pool is for guests -- a day pass is 50 zlitos at the reception.";
    // (the scene variable the engine sets as it puts him out -- pool_refused= -- and the pool's attendant, by the steps' foot, saying why)
    internal const int PoolRefused = 76;
    private static readonly (int X, int Z, int Beta) AttendantAt = (17650, 8650, 3072);
    private const int HotelDoorman = 8, HotelGuardFar = 11, HotelBellboy = 4;
    // the hero's labels: to the lift's doors and into them; out of them on arriving; in bed, the night, up again; the lift car's walk out
    private const int ToLift = 220, AtLift = 222, FromLift = 224, OutOfLift = 225, Sleeping = 230, NightOver = 231, GettingUp = 232, RoomUp = 233,
        NightFall = 234, CarWalk = 240, CarWalked = 241;
    private const int Asleep_ = 56, GetUp_ = 58;

    // Twinsen's question about a coil and the answer, before a talker's own lines on race day while the coil's still to be got
    private static string CoilAsked(Func<OtringalLine, int> say, OtringalLine answer) =>
        $@"            if ({DayRaceDay} == var_game({OtringalDay}) && {CoilTold} <= var_game({QCoil}) && {CoilBought} > var_game({QCoil}))
            {{
                message_obj(0, {say(OtringalLine.TwinsenAskCoil)});
                message({say(answer)});
            }}
            else
";

    private static void OtringalNight(IslandBeatsA b, Func<OtringalLine, int> say, string when)
    {
        var floors = new[] { OtringalHotel.Corridor, OtringalHotel.Classic, OtringalHotel.Junior, OtringalHotel.Suite };
        if (!b.Store.SceneExists(HotelScene) || floors.Any(s => !b.Store.SceneExists(s))) { b.Log.Add("the night at the Imperial Hotel left out: no hotel or no upper floor (OtringalHotel)"); return; }

        // ---- the lobby: the reception, the lift, a sofa
        if (Copy(b.Originals, HotelScene, HotelDoorman) is not { } clerk) { b.Log.Add("the hotel's reception left out: no doorman to dress its clerk"); return; }
        b.Edit(HotelScene, "the Imperial Hotel's lobby: the reception (a room for the night), the lift up to the first floor in the alcove's wall", model =>
        {
            var adds = new List<Action<SceneScripts>>();
            (clerk.X, clerk.Y, clerk.Z, clerk.Beta) = (ClerkAt.X, OtringalHotel.LobbyFloorY, ClerkAt.Z, 1024);
            clerk.Move = 0;
            string Book(OtringalLine room, int price, int night) => $@"if ({say(room)} == choice())
                {{
                    if ({price} > nb_gold_pieces())
                    {{
                        message({say(OtringalLine.ReceptionShort)});
                    }}
                    else
                    {{
                        give_gold_pieces({price});
                        set_var_game({QNight}, {night});
                        set_var_game({PoolPass}, 1);
                        message({say(OtringalLine.ReceptionKey)});
                    }}
                }}";
            adds.Add(IslandBeatsA.Add(model, clerk, Talker(when, $@"            if ({DayQualified} == var_game({OtringalDay}) && {NightClassic} > var_game({QNight}))
            {{
                add_choice({say(OtringalLine.RoomClassic)});
                add_choice({say(OtringalLine.RoomJunior)});
                add_choice({say(OtringalLine.RoomSuite)});
                add_choice({say(OtringalLine.ReceptionNoRoom)});
                ask_choice({say(OtringalLine.ReceptionOffer)});
                {Book(OtringalLine.RoomClassic, PriceClassic, NightClassic)}
                else {Book(OtringalLine.RoomJunior, PriceJunior, NightJunior)}
                else {Book(OtringalLine.RoomSuite, PriceSuite, NightSuite)}
            }}
            else if ({DayQualified} == var_game({OtringalDay}))
            {{
                message({say(OtringalLine.ReceptionKey)});
            }}
            else {CoilAsked(say, OtringalLine.ReceptionCoil).Trim()}
            if ({DayRaceDay} <= var_game({OtringalDay}))
            {{
                message({say(OtringalLine.ReceptionMorning)});
            }}
            else
            {{
                message({say(OtringalLine.ReceptionDay)});
            }}
            if (0 == var_game({PoolPass}))
            {{
                add_choice({say(OtringalLine.PassYes)});
                add_choice({say(OtringalLine.PassNo)});
                ask_choice({say(OtringalLine.PassOffer)});
                if ({say(OtringalLine.PassYes)} == choice())
                {{
                    if ({PoolPassPrice} > nb_gold_pieces())
                    {{
                        message({say(OtringalLine.ReceptionShort)});
                    }}
                    else
                    {{
                        give_gold_pieces({PoolPassPrice});
                        set_var_game({PoolPass}, 1);
                        message({say(OtringalLine.PassSold)});
                    }}
                }}
            }}
            else
            {{
                message({say(OtringalLine.PassHeld)});
            }}")));
            // (the pool's attendant at the steps' foot on the deck -- the hotel's bellboy's uniform -- who has put a swimmer without a pass
            // out of the water says why; talked to, the pass or the swim)
            if (Copy(b.Originals, HotelScene, HotelBellboy) is { } attendant)
            {
                (attendant.X, attendant.Y, attendant.Z, attendant.Beta) = (AttendantAt.X, Citadel.CitadelProps.PoolDeck, AttendantAt.Z, AttendantAt.Beta);
                attendant.Move = 0;
                adds.Add(IslandBeatsA.Add(model, attendant, $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if (1 == var_cube({PoolRefused}))
    {{
        set_var_cube({PoolRefused}, 0);
        message({say(OtringalLine.AttendantRefuse)});
    }}
    swif (1 == action())
    {{
        if (1500 > distance(0) && {Driving} != comportement_hero())
        {{
            if (0 == var_game({PoolPass}))
            {{
                message({say(OtringalLine.AttendantRefuse)});
            }}
            else
            {{
                message({say(OtringalLine.AttendantEnjoy)});
            }}
        }}
    }}
}}
"));
            }
            // (the counter in front of him, the keys' cabinet behind him, the sign over it: three bodies round the counter's middle, each its
            // own box -- HotelProps.Reception)
            var counter = Citadel.CitadelProps.Actor(HotelProps.ReceptionCounter, CounterAt.X, OtringalHotel.LobbyFloorY, CounterAt.Z, 1024);
            if (HotelProps.Entity(b.GameDirectory) is var counterEntity && counterEntity >= 0) counter.Entity = counterEntity;
            adds.Add(IslandBeatsA.Add(model, counter, "void comportement_0()\n{\n}\n"));
            foreach (var piece in new[] { HotelProps.ReceptionKeys, HotelProps.ReceptionSign })
            {
                var part = Citadel.CitadelProps.Actor(piece, CounterAt.X, OtringalHotel.LobbyFloorY, CounterAt.Z, 1024);
                part.Entity = counter.Entity;
                adds.Add(IslandBeatsA.Add(model, part, "void comportement_0()\n{\n}\n"));
            }
            // the lift: Action before it with a room booked tonight -- its doors open, Twinsen walks into them, into the lift car (its zone on,
            // Twinsen in it: OtringalHotel.LiftCar, the ride there); back from it, out of the doors and they shut behind him
            var front = SceneOps.AddTrackPoint(model, new SceneTrackPoint(OtringalHotel.LobbyLift.X + OtringalHotel.LiftFrontOut, OtringalHotel.LobbyFloorY, OtringalHotel.LobbyLift.Z));
            var doorway = SceneOps.AddTrackPoint(model, new SceneTrackPoint(OtringalHotel.LobbyLift.X + OtringalHotel.LiftDoorOut, OtringalHotel.LobbyFloorY, OtringalHotel.LobbyLift.Z));
            var lift = Citadel.CitadelProps.Actor(HotelProps.LiftFront, OtringalHotel.LobbyLift.X, OtringalHotel.LobbyFloorY, OtringalHotel.LobbyLift.Z, 1024);
            lift.Move = 3;
            // (the hotel's props' entity: HotelProps.Entity)
            if (HotelProps.Entity(b.GameDirectory) is var hotelEntity && hotelEntity >= 0) lift.Entity = hotelEntity;
            var liftIndex = model.Actors.Count;
            model.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = OtringalHotel.LiftCar, X0 = OtringalHotel.LobbyLift.X, X1 = OtringalHotel.LobbyLift.X + 1100, Y0 = OtringalHotel.LobbyFloorY, Y1 = OtringalHotel.LobbyFloorY + 2000,
                Z0 = OtringalHotel.LobbyLift.Z - 500, Z1 = OtringalHotel.LobbyLift.Z + 500,
                Info = new[] { OtringalHotel.CarStand.X, OtringalHotel.FloorY, OtringalHotel.CarStand.Z, 0, OtringalHotel.LiftSwitch, 0, 0, 0 },
            });
            adds.Add(scripts =>
            {
                // the guards (the doorman, actor 8, inside the door; actor 11, by the far end): in chapter 5 a racer is a guest -- their
                // word a welcome and the way to the reception, not "members only, get moving", and bumped into, no fight (the user,
                // 2026-11-24: "the guard at the front won't let Twinsen in so he can't buy a room" -- run into, the game's guard set on
                // him with a second one, and spoken to, "You can't get in here")
                foreach (var (guard, refusal) in new[] { (HotelDoorman, 510), (HotelGuardFar, 511) })
                    try
                    {
                        var life = Loose(scripts.GetText(guard, ScriptKind.Life), "if (0 == hit_by())", "if (0 == hit_by() && 5 != chapter())");
                        life = Loose(life, $"message({refusal});", $"if (5 == chapter()) {{ message({say(OtringalLine.GuardGuest)}); }} else {{ message({refusal}); }}");
                        scripts.SetText(guard, ScriptKind.Life, life);
                    }
                    catch (NotTheGames e) { b.Log.Add($"the hotel's guard (actor {guard}) left as the game has him: {e.Message}"); }
                // the bellboy (actor 4): no more sitting down at track point 10 now and then -- on the sofa that came out of the alcove for
                // the lift (the user, 2026-11-24: "we need to stop this NPC sitting in front of the lift on the sofa we removed")
                try { scripts.SetText(HotelBellboy, ScriptKind.Life, Loose(scripts.GetText(HotelBellboy, ScriptKind.Life), "1 == l_track() && 0 == rnd(25)", "1 == l_track() && 0 > rnd(25)")); }
                catch (NotTheGames e) { b.Log.Add($"the hotel's bellboy (actor {HotelBellboy}) left as the game has him: {e.Message}"); }
                var track = scripts.GetText(0, ScriptKind.Track).TrimEnd();
                if (!track.Contains("label(")) track += "\nlabel(0);\nstop();";
                scripts.SetText(0, ScriptKind.Track, track + "\n\n" + HeroLiftTrack(front, doorway));
                scripts.SetText(liftIndex, ScriptKind.Life, LiftLife(say, when, rideTo: 1, arrive: 2, needsRoom: true));
                scripts.SetText(liftIndex, ScriptKind.Track, LiftTrack);
            });
            var addLift = IslandBeatsA.Add(model, lift, "void comportement_0()\n{\n}\n");
            return scripts => { foreach (var a in adds.Take(adds.Count - 1)) a(scripts); addLift(scripts); adds[^1](scripts); };
        });

        // ---- the corridor: his room's door open to him, the others locked; the lift down
        b.Edit(OtringalHotel.Corridor, "the hotel's corridor: his room's door open to him, the others locked; the lift down", model =>
        {
            var front = SceneOps.AddTrackPoint(model, new SceneTrackPoint(OtringalHotel.CorridorLift.X + OtringalHotel.LiftFrontOut, OtringalHotel.FloorY, OtringalHotel.CorridorLift.Z));
            var doorway = SceneOps.AddTrackPoint(model, new SceneTrackPoint(OtringalHotel.CorridorLift.X + OtringalHotel.LiftDoorOut, OtringalHotel.FloorY, OtringalHotel.CorridorLift.Z));
            model.Actors[OtringalHotel.CorridorLiftActor].Move = 3;
            // (the doors: actor 4 + k room 101 + k, the suite's after them -- OtringalHotel puts them in that order)
            const int FirstDoor = 4;
            var suiteDoor = FirstDoor + HotelProps.Rooms;
            string Locked(int actor, string mine) => $@"
            else if ({mine}1300 > distance_message({actor}))
            {{
                bulle(0);
                message({say(OtringalLine.RoomLocked)});
                bulle(1);
            }}";
            var doors = string.Concat(Enumerable.Range(0, HotelProps.Rooms).Select(k =>
            {
                var room = HotelProps.FirstRoom + k;
                var mine = room == OtringalHotel.ClassicDoor ? $"{NightClassic} != var_game({QNight}) && " : room == OtringalHotel.JuniorDoor ? $"{NightJunior} != var_game({QNight}) && " : "";
                return Locked(FirstDoor + k, mine);
            })) + Locked(suiteDoor, $"{NightSuite} != var_game({QNight}) && ");
            return scripts =>
            {
                scripts.SetText(0, ScriptKind.Life, $@"void comportement_0()
{{
    if ({NightClassic} == var_game({QNight}))
    {{
        set_change_cube({OtringalHotel.ClassicSwitch}, 1);
    }}
    if ({NightJunior} == var_game({QNight}))
    {{
        set_change_cube({OtringalHotel.JuniorSwitch}, 1);
    }}
    if ({NightSuite} == var_game({QNight}))
    {{
        set_change_cube({OtringalHotel.SuiteSwitch}, 1);
    }}
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if ({Driving} == comportement_hero())
        {{
        }}{doors}
    }}
}}
");
                scripts.SetText(0, ScriptKind.Track, "label(0);\nstop();\n\n" + HeroLiftTrack(front, doorway));
                scripts.SetText(OtringalHotel.CorridorLiftActor, ScriptKind.Life, LiftLife(say, when, rideTo: 2, arrive: 1, needsRoom: false));
                scripts.SetText(OtringalHotel.CorridorLiftActor, ScriptKind.Track, LiftTrack);
            };
        });

        // ---- the lift car: the ride between the floors (2026-11-23, the user: "Let's add an actual scene for the lift that Twinsen can walk
        // inside, have the door close, we'll play some music for 5 seconds, then bing and the doors open again at the different floor") --
        // in through its open doors, the cut-scene's bars, Twinsen held facing them; they shut, the lift's tune five seconds (the floor's
        // music stopped as it loads: RACEMOD.CPP quiet_scene=), its bell, they open, out he walks -- to the floor LIFT_RIDE says (1 the first
        // floor, 2 the lobby; a game saved in the car, and loaded: the lobby)
        var liftSounds = HotelLiftSounds.Install(b.GameDirectory);
        b.Log.Add($"the hotel lift's tune SAMPLES.HQR {liftSounds.Tune}, its bell {liftSounds.Bell}");
        b.Edit(OtringalHotel.LiftCar, "the Imperial Hotel's lift car: in, the doors shut, its tune, its bell, out at the other floor", model =>
        {
            var outPoint = SceneOps.AddTrackPoint(model, new SceneTrackPoint(OtringalHotel.CarOut.X, OtringalHotel.FloorY, OtringalHotel.CarOut.Z));
            return scripts =>
            {
                scripts.SetText(0, ScriptKind.Track, $"label(0);\nstop();\n\nlabel({CarWalk});\nanim(1);\ngoto_point({outPoint});\n\nlabel({CarWalked});\nstop();\n");
                scripts.SetText(OtringalHotel.CarDoorsActor, ScriptKind.Life, $@"void comportement_0()
{{
    cinema_mode(1);
    set_dir_obj(0, NO_MOVE);
    anim_obj(0, 0);
    body({HotelProps.LiftInsideOpen});
    set_track(label_10);
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if (11 == l_track())
    {{
        body({HotelProps.LiftInside});
        sample({liftSounds.Tune});
        set_track(label_12);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if (13 == l_track())
    {{
        sample({liftSounds.Bell});
        set_track(label_14);
        set_comportement(comportement_3);
    }}
}}

void comportement_3()
{{
    if (15 == l_track())
    {{
        body({HotelProps.LiftInsideOpen});
        set_track_obj(0, label_{CarWalk});
        set_comportement(comportement_4);
    }}
}}

void comportement_4()
{{
    if ({CarWalked} == l_track_obj(0))
    {{
        if (1 == var_game({HotelLiftRide}))
        {{
            set_change_cube({OtringalHotel.CarToCorridor}, 1);
        }}
        else
        {{
            set_var_game({HotelLiftRide}, 2);
            set_change_cube({OtringalHotel.CarToLobby}, 1);
        }}
        set_comportement(comportement_5);
    }}
}}

void comportement_5()
{{
}}
");
                scripts.SetText(OtringalHotel.CarDoorsActor, ScriptKind.Track, $"label(0);\nstop();\n\nlabel(10);\nwait_nb_dizieme(9);\n\nlabel(11);\nstop();\n\n" +
                    $"label(12);\nwait_nb_dizieme({(int)Math.Round(HotelLiftSounds.TuneSeconds * 10)});\n\nlabel(13);\nstop();\n\nlabel(14);\nwait_nb_dizieme(6);\n\nlabel(15);\nstop();\n");
            };
        });

        // ---- the rooms: Action by the bed on the night his room is: asleep, the morning (race day), up, his word on the bed.
        // (In bed he takes no notice of the bricks, nor falls: the Classic Room's and the Junior Suite's beds are the game's own rooms' bricks,
        // solid in their whole box up to the canopy, and laid on the mattress he was pushed up out of them onto its top -- asleep in the air
        // above the bed, the user, 2026-11-25. The Junior Suite's way up was the gap between the bed and the coffee table, narrower than him:
        // stuck there, the same day; now off the bed's foot, in the open.)
        foreach (var (scene, night, bed, side, zone, wake) in new[]
                 {
                     (OtringalHotel.Classic, NightClassic, (X: 1280, Y: 256 + 450, Z: 3328, Beta: 1024), (X: 2900, Z: 3328), (X0: 512, X1: 3000, Z0: 2300, Z1: 4300), OtringalLine.WakeClassic),
                     (OtringalHotel.Junior, NightJunior, (X: 1280, Y: 256 + 450, Z: 2300, Beta: 1024), (X: 2800, Z: 3700), (X0: 512, X1: 3000, Z0: 1300, Z1: 3300), OtringalLine.WakeJunior),
                     (OtringalHotel.Suite, NightSuite, (X: OtringalHotel.SuiteBed.X, Y: 256 + HotelProps.BedTop + 10, Z: OtringalHotel.SuiteBed.Z, Beta: 0), (X: OtringalHotel.SuiteBed.X, Z: OtringalHotel.SuiteBed.Z + 1900), (X0: 6800, X1: 10100, Z0: 1000, Z1: 4700), OtringalLine.WakeSuite),
                 })
            b.Edit(scene, "a room upstairs at the hotel: Action by its bed on his night there -- asleep, the morning, his word on the bed", model =>
            {
                var bedPoint = SceneOps.AddTrackPoint(model, new SceneTrackPoint(bed.X, bed.Y, bed.Z));
                var sidePoint = SceneOps.AddTrackPoint(model, new SceneTrackPoint(side.X, OtringalHotel.FloorY, side.Z));
                model.Zones.Add(new SceneZoneModel { Type = 2, Num = 1, X0 = zone.X0, X1 = zone.X1, Y0 = 0, Y1 = 2500, Z0 = zone.Z0, Z1 = zone.Z1, Info = new int[8] });
                return scripts =>
                {
                    scripts.SetText(0, ScriptKind.Life, $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if (1 == zone() && {Driving} != comportement_hero() && {DayQualified} == var_game({OtringalDay}) && {night} == var_game({QNight}))
        {{
            set_dir(NO_MOVE);
            cinema_mode(1);
            message_obj(0, {say(OtringalLine.TwinsenSleep)});
            brick_col(0);
            fallable(0);
            pos_point({bedPoint});
            beta({bed.Beta});
            anim({Asleep_});
            set_track(label_{Sleeping});
            set_comportement(comportement_2);
        }}
    }}
}}

// (a moment in bed, then the night)
void comportement_2()
{{
    if ({NightFall} == l_track())
    {{
        fade_to_pal(3);
        set_comportement(comportement_4);
    }}
}}

void comportement_4()
{{
    if ({NightOver} == l_track())
    {{
        set_var_game({OtringalDay}, {DayRaceDay});
        fade_to_pal(0);
        set_track(label_{GettingUp});
        set_comportement(comportement_3);
    }}
}}

void comportement_3()
{{
    if ({RoomUp} == l_track())
    {{
        brick_col(1);
        fallable(1);
        message_obj(0, {say(wake)});
        cinema_mode(0);
        set_dir(MOVE_MANUAL);
        set_comportement(comportement_1);
    }}
}}
");
                    scripts.SetText(0, ScriptKind.Track, $"label(0);\nstop();\n\nlabel({Sleeping});\nwait_nb_dizieme(20);\n\nlabel({NightFall});\nwait_nb_dizieme(35);\n\nlabel({NightOver});\nstop();\n\nlabel({GettingUp});\nanim({GetUp_});\nwait_anim();\n\nlabel({RoomUp});\npos_point({sidePoint});\nanim(0);\nstop();\n");
                };
            });

        // ---- Rick's bar and the room behind its stage: the singer where the game's own scripts have her (the user, 2026-11-25: "The singer
        // in Otringal is only supposed to be either on the stage or in the backroom ... we should defer to the retail scripting and force
        // Twinsen to wait until she's back stage sat at her dressing table"). The game's stage turn (game variable 131: 0 the guitarist on,
        // 1 her going out, 2 her singing, 3 her coming off) is left alone. On the stage, talked to while Twinsen carries the letter or her
        // answer: not now, wait in the back (the user, 2026-11-23). Behind the stage (scene 134) she sits at her dressing table as the game's
        // actor 8 -- there when he comes in between her songs, or once she has come off the stage and sat down (her walking self, actor 11,
        // hands over to it at its track 53): Twinsen's hello there (the hero's script: var_cube(0) 1 near her) gets the letter taken and
        // her answer written, in place of her own word -- so nothing comes of it till she is sat at the table.
        if (b.Store.SceneExists(RickBar))
            b.Edit(RickBar, "Rick's bar: the singer on the stage while Twinsen carries the gazogem seller's letter -- not now, wait in the back", model =>
            {
                return scripts =>
                {
                    var her = scripts.GetText(Singer, ScriptKind.Life);
                    scripts.SetText(Singer, ScriptKind.Life, AtStart(her, "comportement_2", $@"    swif (1 == action())
    {{
        if (2300 > distance(0) && {Driving} != comportement_hero() && {DayRaceDay} == var_game({OtringalDay}) && {CoilLetter} <= var_game({QCoil}) && {CoilBought} > var_game({QCoil}))
        {{
            message({say(OtringalLine.SingerOnStage)});
        }}
    }}
"));
                };
            });
        else b.Log.Add($"the singer's letter left out: no Rick's bar (scene {RickBar})");
        if (b.Store.SceneExists(SingerRoom))
            b.Edit(SingerRoom, "the singer's room behind Rick's stage: sat at her dressing table, she takes the gazogem seller's letter and writes back", model =>
            {
                return scripts =>
                {
                    var seated = scripts.GetText(SingerSeated, ScriptKind.Life);
                    scripts.SetText(SingerSeated, ScriptKind.Life, AtStart(seated, "comportement_1", $@"    if (1 == var_cube(0) && 2200 > distance(0) && {DayRaceDay} == var_game({OtringalDay}) && {CoilLetter} <= var_game({QCoil}) && {CoilBought} > var_game({QCoil}))
    {{
        if ({CoilLetter} == var_game({QCoil}))
        {{
            message_obj(0, {say(OtringalLine.TwinsenGiveLetter)});
            message({say(OtringalLine.SingerReply)});
            set_var_game({QCoil}, {CoilAnswer});
            set_var_game({LetterSlot}, {ReplyHeld});
            found_object({LetterSlot});
        }}
        else
        {{
            message({say(OtringalLine.SingerWaiting)});
        }}
        set_var_cube(0, 0);
    }}
"));
                };
            });
        else b.Log.Add($"the singer's back room left out: no room behind the stage (scene {SingerRoom})");
    }

    // A lift's life: Action before it -- its doors open, Twinsen walks to them and is in (his comportement `hidden`: out of sight), the doors
    // shut, eight seconds, LIFT_RIDE `rideTo` and the zone round him on (the other floor); come up or down to it (LIFT_RIDE `arrive`): open, a
    // moment, shut, Twinsen free. The lobby's only with a room booked tonight.
    // A floor's lift: Action before it -- the doors open, Twinsen walks into them and so into the lift car (LIFT_RIDE the floor he's going
    // to); come back from the car -- the doors open, he walks out of them, they shut behind him
    private static string LiftLife(Func<OtringalLine, int> say, string when, int rideTo, int arrive, bool needsRoom) => $@"void comportement_0()
{{
    if ({arrive} == var_game({HotelLiftRide}))
    {{
        cinema_mode(1);
        set_dir_obj(0, NO_MOVE);
        body({HotelProps.LiftOpen});
        set_track_obj(0, label_{FromLift});
        set_track(label_20);
        set_comportement(comportement_4);
    }}
    else
    {{
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if (1300 > distance(0) && {Driving} != comportement_hero())
        {{
{(needsRoom ? $@"            if ({when} && {DayQualified} == var_game({OtringalDay}) && {NightClassic} <= var_game({QNight}))
            {{
{RideIn}            }}
            else
            {{
                message_obj(0, {say(OtringalLine.TwinsenNoKey)});
            }}
" : RideIn)}        }}
    }}
}}

void comportement_2()
{{
    if ({AtLift} == l_track_obj(0))
    {{
        set_var_game({HotelLiftRide}, {rideTo});
        set_change_cube({OtringalHotel.LiftSwitch}, 1);
        set_comportement(comportement_5);
    }}
}}

void comportement_3()
{{
}}

void comportement_4()
{{
    if (21 == l_track())
    {{
        body({HotelProps.LiftFront});
        cinema_mode(0);
        set_dir_obj(0, MOVE_MANUAL);
        set_var_game({HotelLiftRide}, 0);
        set_comportement(comportement_1);
    }}
}}

void comportement_5()
{{
}}
";
    // (into the lift: the cut-scene's bars, Twinsen held, the doors open, his walk to them)
    private static readonly string RideIn = $@"                cinema_mode(1);
                set_dir_obj(0, NO_MOVE);
                body({HotelProps.LiftOpen});
                set_track_obj(0, label_{ToLift});
                set_comportement(comportement_2);
";
    private const string LiftTrack = "label(0);\nstop();\n\nlabel(20);\nwait_nb_dizieme(20);\n\nlabel(21);\nstop();\n";
    // (the hero's walk into a floor's lift -- up to its doors, through them -- and out of it, from its doorway to before it, turned to the room)
    private static string HeroLiftTrack(int front, int doorway) =>
        $"label({ToLift});\nanim(1);\ngoto_point({front});\ngoto_point({doorway});\n\nlabel({AtLift});\nstop();\n\n" +
        $"label({FromLift});\nanim(1);\ngoto_point({front});\nanim(0);\nbeta(1024);\n\nlabel({OutOfLift});\nstop();\n";
}
