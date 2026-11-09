using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The secret chamber under Twinsen's house (the user, 2026-11-07: "A hidden area in Twinsen's house accessed by an elevator, perhaps with 4
// slightly darker squares that takes Twinsen and Zoe down into a secret area ... loosely model this on the scene from LBA1 '21: Citadel
// Island, secret chamber in the house'", and, if Twinsen doesn't go home with Zoe after the lighthouse "and instead discovers the bar area,
// when he comes home we get a cut scene where Zoe takes him down on the elevator and spanks him because instead of being at home looking
// after his pregnant wife he was out drinking when drinking is banned" -- "the Tralu's bar in the cave" -- "after the spanking scene Zoe
// sends Twinsen back upstairs to bed, let's have the door locked at this point so he can't leave the house again"):
//   - the lift: the storeroom's four darker squares (Citadel/CitadelHouseLift), Action on them in the house by day or at night and it takes
//     Twinsen down -- a door zone onto the chamber, switched on by the Action (a scene past 255 is reached by a door zone only);
//   - the chamber: LBA1's own room (Citadel/CitadelLba1Rooms, scene 400), its way back up onto the squares: into the house by day, into
//     the house at night while Twinsen is tired (two doors, the one for the time switched on as he comes in);
//   - the night out: the Tralu's bar walked into in the evening (DayVar Tired, chapter 1) sets Drunk; home, Zoe is waiting at the door
//     (RaceTrackStory.Evening.cs NightHouseStory): "where have you been", the storeroom, the lift down; in the chamber her scolding and the
//     spanking, "now upstairs and straight to bed", up the lift (Drunk 2), and the front door locked for the night ("Zoe's locked the
//     front door" if he tries it). His bed then as every night.
internal static partial class RaceTrackStory
{
    // Drunk: 1 the Tralu's bar walked into that evening, 2 scolded (a spare game variable: RaceTrackStory.Eggs.cs's list)
    public const int Drunk = 96, DrankTonight = 1, Scolded = 2;
    public const int Chamber = Citadel.CitadelLba1Rooms.SecretChamber;
    // the lift's scenario zone and the switches of the lift's door and the chamber's two ways up, and of the front door at night
    internal const int LiftZone = 70, LiftSwitch = 7, ChamberUpNight = 8, ChamberUpDay = 9, FrontDoorSwitch = 10;
    // the scenario zone at LBA1's old way out of the chamber, filled with rock (a scene variable: said once a visit)
    private const int LandslideZone = 73, LandslideSaid = 3;
    private static readonly (int X0, int X1, int Y0, int Y1, int Z0, int Z1) LiftBox = (4096, 5119, 2560, 3583, 8192, 9215);
    // Zoe in the chamber, to the stairs' side of where Twinsen comes down, facing him
    private static readonly (int X, int Y, int Z) ZoeInChamber = (26368, 1280, 29184);

    // The lift's two zones in a house scene: the scenario zone Action is pressed in, and the door down, off till the Action switches it on
    // (Twinsen in it, he goes: his place in it added to where he comes out, at the chamber's foot of the stairs)
    internal static void AddLift(SceneModel model)
    {
        var b = LiftBox;
        var arrival = Citadel.CitadelLba1Rooms.ChamberArrival;
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = LiftZone, X0 = b.X0, X1 = b.X1, Y0 = b.Y0, Y1 = b.Y1, Z0 = b.Z0, Z1 = b.Z1, Info = new int[8] });
        model.Zones.Add(new SceneZoneModel
        {
            Type = 0, Num = Chamber, X0 = b.X0, X1 = b.X1, Y0 = b.Y0, Y1 = b.Y1, Z0 = b.Z0, Z1 = b.Z1,
            Info = new[] { arrival.X - 512, arrival.Y, arrival.Z - 512, 0, LiftSwitch, 0, 0, 0 },
        });
    }
    // A house's helper: Action on the squares, on foot, and down the lift
    internal static string LiftHelper() => $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if ({LiftZone} == zone_obj(0) && {Driving} != comportement_hero())
        {{
            set_change_cube({LiftSwitch}, 1);
        }}
    }}
}}
";

    private static void SecretChamber(StoryParts p)
    {
        if (!p.Store.SceneExists(Chamber)) { p.Log.Add($"the secret chamber left out: no scene {Chamber} (Citadel/CitadelLba1Rooms)"); return; }
        // the house by day: the lift
        p.Edit(OpeningScene, "the house's lift down to the secret chamber: Action on the storeroom's darker squares", model =>
        {
            AddLift(model);
            return p.Add(model, RaceTrackScenes.InvisibleHelper(Citadel.CitadelHouseLift.Middle.X, Citadel.CitadelHouseLift.Middle.Y, Citadel.CitadelHouseLift.Middle.Z), LiftHelper());
        });
        // the Tralu's bar in the evening: Drunk
        p.Edit(BarHall, "the Tralu's bar walked into in the evening: Zoe will know (Drunk 1)", model =>
            p.Add(model, RaceTrackScenes.InvisibleHelper(model.Actors[0].X, model.Actors[0].Y, model.Actors[0].Z), $@"void comportement_0()
{{
    if (2 > chapter() && {Tired} == var_game({DayVar}) && 0 == var_game({Drunk}))
    {{
        set_var_game({Drunk}, {DrankTonight});
    }}
    suicide();
}}
"));
        // the spanking's animations, bodies and slap (Citadel/CitadelSpanking)
        var (spankLog, slap) = Citadel.CitadelSpanking.Install(p.Store.Directory);
        p.Log.AddRange(spankLog);
        // the chamber: its two ways up, Zoe, the scolding
        p.Edit(Chamber, "the secret chamber: up to the house by day or at night; home from the bar, Zoe's scolding and the spanking", model =>
        {
            var day = model.Zones.FirstOrDefault(z => z.Type == 0 && z.Num == Citadel.CitadelLba1Rooms.House) ?? throw new NotTheGames("the chamber's way up");
            day.Info[4] = ChamberUpDay; day.Info[7] = 0;
            var night = new SceneZoneModel { Type = 0, Num = NightHouse, X0 = day.X0, X1 = day.X1, Y0 = day.Y0, Y1 = day.Y1, Z0 = day.Z0, Z1 = day.Z1, Info = (int[])day.Info.Clone() };
            night.Info[4] = ChamberUpNight;
            model.Zones.Add(night);
            var up = model.TrackPoints.Count;
            model.TrackPoints.Add(new SceneTrackPoint((day.X0 + day.X1) / 2, day.Y0, (day.Z0 + day.Z1) / 2));
            var zoe = Copy(p.Originals, OpeningScene, Zoe) ?? throw new NotTheGames("Zoe");
            (zoe.X, zoe.Y, zoe.Z) = ZoeInChamber;
            var arrival = Citadel.CitadelLba1Rooms.ChamberArrival;
            zoe.Beta = RaceTrackMath.Turn(Math.Atan2(arrival.X - ZoeInChamber.X, arrival.Z - ZoeInChamber.Z));
            // (her stool under her, a little behind her middle -- none of the three pushing another away, their objects' collision off: the flags' CHECK_OBJ_COL, and Twinsen's obj_col(0) till he's up; Twinsen across her lap: her thighs' middle in front of her, his head to her
            // left -- turned a quarter from her -- his hips at her lap's height by his own animation)
            var turn = zoe.Beta * Math.PI / 2048;
            double fx = Math.Sin(turn), fz = Math.Cos(turn);
            // (the bedroom's chair, the user, 2026-11-08: "one of the chairs from the bedroom down there for Zoe to sit on" -- the room's,
            // there always)
            var chair = Citadel.CitadelProps.Actor(Citadel.CitadelProps.Chair, (int)Math.Round(ZoeInChamber.X - fx * 20), ZoeInChamber.Y, (int)Math.Round(ZoeInChamber.Z - fz * 20), zoe.Beta);
            chair.Flags &= ~1u;
            var stoolLife = p.Add(model, chair, "void comportement_0()\n{\n}\n");
            // the room's furnishings (Citadel/CitadelProps.Chamber): the Sendell seal back in the hole in the hall's back wall (LBA1's sprite
            // 84 there, cell 41.2, layer 10.1, 33.2); on the rock wall facing the lower floor (z 50's face, z 26112) a red tapestry, the rack
            // of paddles and whips and a picture; on the left wall (x 30's face, x 15872) by LBA1's old way out, a blue tapestry
            var furnishings = new List<Action<SceneScripts>>();
            void Furnish(int generic, int x, int y, int z, int beta) =>
                furnishings.Add(p.Add(model, Citadel.CitadelProps.Actor(generic, x, y, z, beta), "void comportement_0()\n{\n}\n"));
            Furnish(Citadel.CitadelProps.Seal, 21420, 3100, 17408 + 60, 0);
            Furnish(Citadel.CitadelProps.TapestryRed, 20224, 2560, 26112 + 6, 0);
            Furnish(Citadel.CitadelProps.Rack, 21888, 1980, 26112 + 4, 0);
            Furnish(Citadel.CitadelProps.Picture, 23552, 2080, 26112 + 4, 0);
            Furnish(Citadel.CitadelProps.TapestryBlue, 15872 + 6, 2560, 31232, 1024);
            // (LBA1's old way out, filled with rock: Twinsen's word there -- the user: "if Twinsen goes over to it have him comment 'This exit is
            // no longer useable due to a land slide'")
            model.Zones.Add(new SceneZoneModel { Type = 2, Num = LandslideZone, X0 = 15872, X1 = 17919, Y0 = 1280, Y1 = 2815, Z0 = 31232, Z1 = 32255, Info = new int[8] });
            var lap = model.TrackPoints.Count;
            model.TrackPoints.Add(new SceneTrackPoint((int)Math.Round(ZoeInChamber.X + fx * 170), ZoeInChamber.Y, (int)Math.Round(ZoeInChamber.Z + fz * 170)));
            var across = (zoe.Beta - 1024 + 4096) % 4096;
            const int Slaps1 = 10, Slapped1 = 11, Slaps2 = 20, Slapped2 = 21;
            string Slaps(int n) => slap < 0 ? "wait_nb_second(1);\n" : string.Concat(Enumerable.Repeat($"sample({slap});\nwait_nb_dizieme(3);\n", n));
            zoe.Flags &= ~1u;
            var zoeIndex = model.Actors.Count;
            var zoeLife = p.Add(model, zoe, $@"void comportement_0()
{{
    if ({DrankTonight} == var_game({Drunk}))
    {{
        body(0);
    }}
    else
    {{
        suicide();
    }}
}}
");
            return scripts =>
            {
                zoeLife(scripts);
                stoolLife(scripts);
                foreach (var f in furnishings) f(scripts);
                scripts.SetText(zoeIndex, ScriptKind.Track, $@"label(0);
anim({Citadel.CitadelSpanking.ZoeSit});
stop();

label({Slaps1});
anim({Citadel.CitadelSpanking.ZoeSpank});
wait_nb_dizieme(2);
{Slaps(3)}
label({Slapped1});
anim({Citadel.CitadelSpanking.ZoeSit});
stop();

label({Slaps2});
anim({Citadel.CitadelSpanking.ZoeSpank});
wait_nb_dizieme(2);
{Slaps(4)}
label({Slapped2});
anim({Citadel.CitadelSpanking.ZoeSit});
stop();
");
                scripts.SetText(0, ScriptKind.Life, $@"void comportement_0()
{{
    if ({Tired} == var_game({DayVar}) || {TiredAside} == var_game({DayVar}))
    {{
        set_change_cube({ChamberUpNight}, 1);
    }}
    else
    {{
        set_change_cube({ChamberUpDay}, 1);
    }}
    if ({DrankTonight} == var_game({Drunk}))
    {{
        set_dir(NO_MOVE);
        cinema_mode(1);
        set_var_game({DayVar}, {TiredAside});
        message_obj({zoeIndex}, {EveningId(p.Say, EveningLine.ZoeScold)});
        obj_col(0);
        pos_point({lap});
        beta({across});
        body({Citadel.CitadelSpanking.TwinsenBare});
        anim({Citadel.CitadelSpanking.TwinsenKick});
        set_track_obj({zoeIndex}, label_{Slaps1});
        set_comportement(comportement_2);
    }}
    else
    {{
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    if ({LandslideZone} == zone())
    {{
        if (0 == var_cube({LandslideSaid}))
        {{
            message({EveningId(p.Say, EveningLine.Landslide)});
            set_var_cube({LandslideSaid}, 1);
        }}
    }}
    else
    {{
        set_var_cube({LandslideSaid}, 0);
    }}
}}

void comportement_2()
{{
    if ({Slapped1} == l_track_obj({zoeIndex}))
    {{
        anim({Citadel.CitadelSpanking.TwinsenLap});
        message({EveningId(p.Say, EveningLine.TwinsenOw)});
        body({Citadel.CitadelSpanking.TwinsenRed});
        anim({Citadel.CitadelSpanking.TwinsenKick});
        set_track_obj({zoeIndex}, label_{Slaps2});
        set_comportement(comportement_3);
    }}
}}

void comportement_3()
{{
    if ({Slapped2} == l_track_obj({zoeIndex}))
    {{
        anim({Citadel.CitadelSpanking.TwinsenLap});
        message_obj({zoeIndex}, {EveningId(p.Say, EveningLine.ZoeScoldMore)});
        message({EveningId(p.Say, EveningLine.TwinsenOw)});
        message_obj({zoeIndex}, {EveningId(p.Say, EveningLine.ZoeBed)});
        set_var_game({DayVar}, {Tired});
        set_var_game({Drunk}, {Scolded});
        body(1);
        anim(0);
        cinema_mode(0);
        set_dir(MOVE_MANUAL);
        pos_point({up});
        obj_col(1);
        set_comportement(comportement_1);
    }}
}}
");
            };
        });
    }
}
