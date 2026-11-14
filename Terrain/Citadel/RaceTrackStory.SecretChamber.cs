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
    // the scenario zone round LBA1's catamaran in the lower room (Action there: Twinsen's memories of it) and where the boat stands
    private const int BoatZone = 74;
    // the chamber's lift (CitadelProps.BuildLiftPlatform and its rail) on the hall's free strip right of the Sendell rug (cells x 49-50, z
    // 41-42): it comes down from ChamberLiftDown over the hall's floor with Twinsen on it (CitadelLba1Rooms.ChamberArrival), and Action on it
    // takes it up ChamberLiftUp, into the way up over it (CitadelLba1Rooms.ChamberExit); the scenario zones over it: Action's, and his
    // arrival's (he waits on it till it is down: LiftDownZone). In the house, the lift's squares carry a platform of their own down
    // HouseLiftDown, through the floor, into the way down under it. Each a carrier gliding on its track (GERETRAK's goto_point_3d for
    // carriers, 2026-11-09: the user, "when we take the elevator down we just teleport ... When we're riding the elevator back up again
    // it's not smooth"), Twinsen carried; a change of scene gives him back to the player.
    internal const int LiftUpZone = 75, LiftDownZone = 76, LiftRide = 1, LiftDown = 2, ZoeRide = 30, LiftSpeed = 600;
    internal const int ChamberLiftDown = 1200, ChamberLiftUp = 1800, HouseLiftDown = 1600;
    // Up from the chamber: out of its lift's way up onto the house's platform HouseLiftUp under the house's floor, in its shaft (a scenario
    // zone, LiftShaftZone), the platform put there and gliding up to the floor with him (label LiftRise; LiftRisen when it is up) -- deep
    // enough that he isn't in the platform's box where it stands at the floor till then, which would push him off it
    internal const int HouseLiftUp = 1200, LiftShaftZone = 77, LiftRise = 3, LiftRisen = 4, ZoeLiftRise = 40;
    private static readonly (int X, int Y, int Z) LiftAt = (25600, 1792, 21504);
    private static readonly (int X, int Y, int Z) BoatAt = (25300, 1280, 28700);
    private static readonly (int X0, int X1, int Y0, int Y1, int Z0, int Z1) LiftBox = (4096, 5119, 2560, 3583, 8192, 9215);
    // Zoe in the chamber's hall, on the bedroom's chair in the middle of the Sendell rug, facing the camera's way (+x +z): Twinsen across her
    // lap broadside to it, his bottom up in plain sight (the user, 2026-11-09: "The orientation of the chair in the spanking scene means we
    // can't see Twinsen's bottom ... Let's move this scene to the upper area where the sendell scene is"), the Sendell seal behind them
    private static readonly (int X, int Y, int Z) ZoeInChamber = (22528, 1792, 21504);
    private const int ZoeFacing = 512;
    // the Sendell rug's middle (the hall's floor, x 35-48 and z 34-45, almost all of it) and the Dinofly rug's (the lower room's middle)
    private static readonly (int X, int Y, int Z) SendellRugAt = (21504, 1792, 20480), DinoRugAt = (21760, 1280, 28416);

    // A lift: its platform (and its rail, `rails`), each a carrier with a body on its own track, at `foot` (the platform's foot at its
    // lowest), starting `startUp` over it. Label 0: down to its foot if it started up (then label LiftDown), else still; label LiftRide:
    // to `rideTo` over its foot. Its actors, and their scripts.
    private static (int[] Actors, Action<SceneScripts> Scripts) LiftRig(StoryParts p, SceneModel model, (int X, int Y, int Z) foot, int startUp, int rideTo, bool rails, int riseFrom = 0)
    {
        var low = model.TrackPoints.Count;
        model.TrackPoints.Add(new SceneTrackPoint(foot.X, foot.Y, foot.Z));
        var to = model.TrackPoints.Count;
        model.TrackPoints.Add(new SceneTrackPoint(foot.X, foot.Y + rideTo, foot.Z));
        var under = model.TrackPoints.Count;
        if (riseFrom != 0) model.TrackPoints.Add(new SceneTrackPoint(foot.X, foot.Y - riseFrom, foot.Z));
        var actors = new List<int>();
        var adds = new List<Action<SceneScripts>>();
        // (the chamber's: planks in an iron frame, and the rail; the house's: the storeroom's four darker squares)
        var parts = rails ? new[] { Citadel.CitadelProps.LiftPlatform, Citadel.CitadelProps.LiftRailBack, Citadel.CitadelProps.LiftRailLeft, Citadel.CitadelProps.LiftRailRight }
                          : new[] { Citadel.CitadelProps.HouseLiftPlatform };
        foreach (var generic in parts)
        {
            actors.Add(model.Actors.Count);
            var a = Citadel.CitadelProps.Actor(generic, foot.X, foot.Y + startUp, foot.Z);
            a.Flags |= RaceTrackScenes.CarrierFlag;
            // (its track running from the scene's start: label 0, down to its foot when it starts up -- an actor's track starts by itself
            // only when it moves by its track)
            a.Move = 3;
            adds.Add(p.Add(model, a, "void comportement_0()\n{\n}\n"));
        }
        var down = startUp != 0 ? $"speed({LiftSpeed});\ngoto_point_3d({low});\n" : "";
        var track = $"label(0);\n{down}\nlabel({LiftDown});\nstop();\n\nlabel({LiftRide});\nspeed({LiftSpeed});\ngoto_point_3d({to});\nstop();\n";
        if (riseFrom != 0) track += $"\nlabel({LiftRise});\npos_point({under});\nspeed({LiftSpeed});\ngoto_point_3d({low});\n\nlabel({LiftRisen});\nstop();\n";
        return (actors.ToArray(), scripts => { foreach (var add in adds) add(scripts); foreach (var i in actors) scripts.SetText(i, ScriptKind.Track, track); });
    }
    // (a lift's ride: its actors off up -- or down -- together)
    internal static string Ride(int[] lift) => string.Concat(lift.Select(i => $"set_track_obj({i}, label_{LiftRide});\n"));

    // The lift in a house scene: the platform on the storeroom's squares, down through the floor on Action there, into the way down under
    // the floor (a door zone, off till the Action switches it on), which brings Twinsen out on the chamber's lift as it starts down -- his
    // height in the zone (he comes into it from its top, falling with the platform) taken off. Returns the lift's actors and the scripts.
    private static (int[] Lift, Action<SceneScripts> Scripts) AddLift(StoryParts p, SceneModel model)
    {
        var b = LiftBox;
        var arrival = Citadel.CitadelLba1Rooms.ChamberArrival;
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = LiftZone, X0 = b.X0, X1 = b.X1, Y0 = b.Y0, Y1 = b.Y1, Z0 = b.Z0, Z1 = b.Z1, Info = new int[8] });
        const int DoorTop = 1500, DoorBottom = 1000;
        const int DontReadjust = 1;     // (a zone's Info6 in the file, bit 1: the engine's ZONE_DONT_REAJUST_POS_TWINSEN -- else he is put on the floor under him, the chamber's)
        // (he comes into the zone a step under its top -- the platform's step a frame -- and out that far over the chamber's platform, not into it,
        // which would push him off it)
        const int ArriveOver = 40;
        model.Zones.Add(new SceneZoneModel
        {
            Type = 0, Num = Chamber, X0 = b.X0, X1 = b.X1, Y0 = DoorBottom, Y1 = DoorTop, Z0 = b.Z0, Z1 = b.Z1,
            Info = new[] { arrival.X - 512, arrival.Y - (DoorTop - DoorBottom) + ArriveOver, arrival.Z - 512, 0, LiftSwitch, 0, DontReadjust, 0 },
        });
        var foot = Citadel.CitadelHouseLift.Foot;
        var (lift, liftScripts) = LiftRig(p, model, (foot.X, foot.Y, foot.Z), 0, -HouseLiftDown, false, HouseLiftUp);
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = LiftShaftZone, X0 = b.X0, X1 = b.X1, Y0 = foot.Y - HouseLiftUp - 300, Y1 = foot.Y - 200, Z0 = b.Z0, Z1 = b.Z1, Info = new int[8] });
        var helper = p.Add(model, RaceTrackScenes.InvisibleHelper(foot.X, foot.Y, foot.Z), LiftHelper(lift));
        return (lift, scripts => { liftScripts(scripts); helper(scripts); });
    }
    // A house's helper: Action on the lift, on foot, and down it
    internal static string LiftHelper(int[] lift) => $@"void comportement_0()
{{
    if ({LiftShaftZone} == zone_obj(0))
    {{
        set_dir_obj(0, NO_MOVE);
        {string.Concat(lift.Select(i => $"set_track_obj({i}, label_{LiftRise});\n"))}        set_comportement(comportement_2);
    }}
    else
    {{
        set_comportement(comportement_1);
    }}
}}

void comportement_2()
{{
    if ({LiftRisen} == l_track_obj({lift[0]}))
    {{
        set_dir_obj(0, MOVE_MANUAL);
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if ({LiftZone} == zone_obj(0) && {Driving} != comportement_hero() && {DrankTonight} == var_game({Drunk}))
        {{
            set_dir_obj(0, NO_MOVE);
            anim_obj(0, 0);
            set_change_cube({LiftSwitch}, 1);
            {Ride(lift)}        }}
    }}
}}
";

    private static void SecretChamber(StoryParts p)
    {
        if (!p.Store.SceneExists(Chamber)) { p.Log.Add($"the secret chamber left out: no scene {Chamber} (Citadel/CitadelLba1Rooms)"); return; }
        // the house by day: the lift
        // (the day's house: no lift -- it is the night home drunk's only, the user, 2026-11-09 evening: "Yes I still want it limited" --
        // unless the night's house had to share its grid, shaft and all: then a platform over the shaft here too, Action on it as at night)
        var dayShaft = Citadel.CitadelHouseLift.ShaftInDayHouse(p.Store.Directory);
        if (dayShaft) p.Edit(OpeningScene, "the house's lift over its shaft (the night's house on the day's grid)", model => AddLift(p, model).Scripts);
        // (the Tralu's bar in the evening: the drink Luc brings Twinsen at the regulars' celebration sets Drunk -- RaceTrackStory.TraluTavern)
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
            // (the day's way up -- the chamber is the night's, so it shouldn't be taken -- onto the storeroom's floor if there's no shaft there)
            if (!dayShaft) { day.Info[1] = Citadel.CitadelHouseLift.Middle.Y + 4; day.Info[6] = 0; }
            var zoe = Copy(p.Originals, OpeningScene, Zoe) ?? throw new NotTheGames("Zoe");
            (zoe.X, zoe.Y, zoe.Z) = ZoeInChamber;
            zoe.Beta = ZoeFacing;
            // (none of the three pushing another away, their objects' collision off: the flags' CHECK_OBJ_COL, and Twinsen's obj_col(0) till
            // he's up; Twinsen across her lap: her thighs' middle in front of her, turned a quarter from her, his hips at her lap's height by
            // his own animation)
            var turn = zoe.Beta * Math.PI / 2048;
            double fx = Math.Sin(turn), fz = Math.Cos(turn);
            (int X, int Z) Ahead(double d) => ((int)Math.Round(ZoeInChamber.X + fx * d), (int)Math.Round(ZoeInChamber.Z + fz * d));
            // the room's furnishings (Citadel/CitadelProps.Chamber): the Sendell seal back in the hole in the hall's back wall (LBA1's sprite
            // 84 there, cell 41.2, layer 10.1, 33.2); on the rock wall facing the lower floor (z 50's face, z 26112) a red tapestry and a
            // picture; on the left wall (x 30's face, x 15872) by LBA1's old way out, a blue tapestry
            var furnishings = new List<Action<SceneScripts>>();
            void Furnish(int generic, int x, int y, int z, int beta) =>
                furnishings.Add(p.Add(model, Citadel.CitadelProps.Actor(generic, x, y, z, beta), "void comportement_0()\n{\n}\n"));
            // (and the mushroom's entity's: the seal's parts, LBA1's old things)
            void Old(int generic, int x, int y, int z, int beta)
            {
                var a = SceneOps.BlankActor(SceneGame.Lba2, x, y, z, Citadel.CitadelLba1Rooms.PropEntity);
                a.Body = generic; a.Beta = beta; a.Flags = 0x1000u; a.Armor = 51; a.LifePoints = -1; a.CoulObj = DefaultVoice;
                furnishings.Add(p.Add(model, a, "void comportement_0()\n{\n}\n"));
            }
            // (the seal: LBA1's own picture of it, in as many parts as it took -- Citadel/CitadelLba1Rooms.SealBodies)
            for (var k = 0; k < Citadel.CitadelLba1Rooms.SealParts; k++)
                Old(Citadel.CitadelLba1Rooms.SealFirst + k, Citadel.CitadelLba1Rooms.SealAt.X, Citadel.CitadelLba1Rooms.SealAt.Y, Citadel.CitadelLba1Rooms.SealAt.Z, 0);
            // (round 3, the user: "The wall with the two posters on with curtains either side, let's remove these two posters and add another
            // bayeaux tapestry with different elements of LBA1 than we already have shown, this time the right way up so it's viewable" --
            // between the curtains, its top under the wall's, Citadel/CitadelProps.Rugs)
            for (var k = 0; k < Math.Min(Citadel.CitadelProps.Bayeux2Parts.Count, Citadel.CitadelProps.Bayeux2Most); k++)
                Furnish(Citadel.CitadelProps.Bayeux2First + k, 21975 + Citadel.CitadelProps.Bayeux2X(Citadel.CitadelProps.Bayeux2Parts[k]), 3250, 26112 + 4, 0);
            Furnish(Citadel.CitadelProps.TapestryBlue, 15872 + 6, 2560, 31232, 1024);
            // round 2 (the user, 2026-11-09): "add in some giant bolders" on LBA1's way out (its landing, x 31-34 at z 62, layer 3's top, and
            // its stairs down at z 63, layer 2's) -- they stand in Twinsen's way, as any actor with a body does
            Furnish(Citadel.CitadelProps.Boulder, 16450, 1024, 32050, 300);
            Furnish(Citadel.CitadelProps.Boulder2, 17550, 768, 32450, 1500);
            Furnish(Citadel.CitadelProps.Boulder2, 17450, 1024, 31850, 2900);
            // ("let's also add some more small boulders by the blocked exit")
            Furnish(Citadel.CitadelProps.SmallBoulder, 16150, 1280, 30950, 400);
            Furnish(Citadel.CitadelProps.SmallBoulder, 17000, 1280, 31100, 1700);
            Furnish(Citadel.CitadelProps.SmallBoulder, 17750, 1280, 30850, 3000);
            Furnish(Citadel.CitadelProps.SmallBoulder, 16700, 1280, 30500, 2400);
            Furnish(Citadel.CitadelProps.SmallBoulder, 15990, 1024, 31900, 900);
            // "some curtains around the edges": either side of the Sendell seal (the hall's back wall, z 17408) and at the two ends of the lower
            // room's back wall (z 26112: x 37.5 to 48)
            Furnish(Citadel.CitadelProps.CurtainLeft, 20150, 3900, 17408 + 4, 0);
            Furnish(Citadel.CitadelProps.CurtainRight, 22858, 3900, 17408 + 4, 0);
            Furnish(Citadel.CitadelProps.CurtainLeft, 19700, 1280 + Citadel.CitadelProps.CurtainDrop + 10, 26112 + 4, 0);
            Furnish(Citadel.CitadelProps.CurtainRight, 24250, 1280 + Citadel.CitadelProps.CurtainDrop + 10, 26112 + 4, 0);
            // round 3 (the user, 2026-11-09): the picture rugs -- the Dinofly with Twinsen riding him in the lower room, Sendell in the hall --
            // each in its parts side by side (Citadel/CitadelProps.Rugs)
            for (var k = 0; k < Math.Min(Citadel.CitadelProps.DinoTiles.Count, Citadel.CitadelProps.DinoMost); k++)
            {
                var (dx, dz) = Citadel.CitadelProps.RugTileAt(Citadel.CitadelProps.DinoRows, Citadel.CitadelProps.DinoStitch, Citadel.CitadelProps.DinoTiles[k]);
                Furnish(Citadel.CitadelProps.DinoFirst + k, DinoRugAt.X + dx, DinoRugAt.Y, DinoRugAt.Z + dz, 0);
            }
            for (var k = 0; k < Math.Min(Citadel.CitadelProps.SendellTiles.Count, Citadel.CitadelProps.SendellMost); k++)
            {
                var (dx, dz) = Citadel.CitadelProps.RugTileAt(Citadel.CitadelProps.SendellRows, Citadel.CitadelProps.SendellStitch, Citadel.CitadelProps.SendellTiles[k]);
                Furnish(Citadel.CitadelProps.SendellFirst + k, SendellRugAt.X + dx, SendellRugAt.Y, SendellRugAt.Z + dz, 0);
            }
            // the lift: a platform Twinsen stands on (OBJ_CARRIER), on its track -- still, or rising (LiftRide) -- and the zone over it Action
            // is pressed in; Zoe's place on it beside him, and both up
            var (lift, liftLife) = LiftRig(p, model, LiftAt, ChamberLiftDown, ChamberLiftUp, true);
            var platform = lift[0];
            var onLift = model.TrackPoints.Count;
            model.TrackPoints.Add(new SceneTrackPoint(LiftAt.X + 220, LiftAt.Y + Citadel.CitadelProps.LiftTop + 4, LiftAt.Z + 150));
            var zoeOn = model.TrackPoints.Count;
            model.TrackPoints.Add(new SceneTrackPoint(LiftAt.X - 250, LiftAt.Y + Citadel.CitadelProps.LiftTop + 4, LiftAt.Z + 330));
            var zoeHigh = model.TrackPoints.Count;
            model.TrackPoints.Add(new SceneTrackPoint(LiftAt.X - 250, LiftAt.Y + Citadel.CitadelProps.LiftTop + 4 + ChamberLiftUp, LiftAt.Z + 330));
            model.Zones.Add(new SceneZoneModel { Type = 2, Num = LiftUpZone, X0 = LiftAt.X - 512, X1 = LiftAt.X + 511, Y0 = LiftAt.Y, Y1 = LiftAt.Y + 800, Z0 = LiftAt.Z - 512, Z1 = LiftAt.Z + 511, Info = new int[8] });
            model.Zones.Add(new SceneZoneModel { Type = 2, Num = LiftDownZone, X0 = LiftAt.X - 512, X1 = LiftAt.X + 511, Y0 = LiftAt.Y + ChamberLiftDown - 200, Y1 = LiftAt.Y + ChamberLiftDown + 400, Z0 = LiftAt.Z - 512, Z1 = LiftAt.Z + 511, Info = new int[8] });
            // the bedroom's chair under Zoe (its seat CitadelSpanking.SeatBack behind her place, under her hips; the actor itself
            // CitadelProps.ChairAhead further back, its box behind hers), and the stand of paddles and whips beside it, to her left
            var (chairX, chairZ) = Ahead(-Citadel.CitadelSpanking.SeatBack - Citadel.CitadelProps.ChairAhead);
            Furnish(Citadel.CitadelProps.Chair, chairX, ZoeInChamber.Y, chairZ, ZoeFacing);
            Furnish(Citadel.CitadelProps.Rack, 23900, ZoeInChamber.Y, 20300, ZoeFacing);
            // "the lower area, let's fill it with some random clutter": Twinsen's and Zoe's bikes parked side by side, LBA1's snowboard and space
            // guitar leant against the back wall's right-hand stretch (x 52-54) under a Jolly Roger -- all clear of
            // the low front and right walls, which hide what stands near them from the camera, of the right-hand stairs' foot, and of Twinsen's
            // way between the lift's arrival and LBA1's way up
            // (the bikes by the stone, left of the Dinofly rug; Twinsen's catamaran on the right, clear of the lift's arrival (27127, 29918) and
            // of the way up, x 26368-27903 from z 30976 -- its box, the engine's square of half its length and breadth together, keeps
            // Twinsen off it -- and Action beside it, his memories of it: BoatZone)
            Furnish(Citadel.CitadelProps.BikeTwinsen, 18000, 1280, 28900, 0);
            Furnish(Citadel.CitadelProps.BikeZoe, 18700, 1280, 28900, 0);
            Old(Citadel.CitadelLba1Rooms.Catamaran, BoatAt.X, BoatAt.Y, BoatAt.Z, 0);
            model.Zones.Add(new SceneZoneModel { Type = 2, Num = BoatZone, X0 = BoatAt.X - 1900, X1 = BoatAt.X + 1700, Y0 = 1280, Y1 = 3000, Z0 = BoatAt.Z - 1800, Z1 = BoatAt.Z + 1800, Info = new int[8] });
            int Thing(int k) => Citadel.CitadelLba1Rooms.OldThingFirst + k;
            Old(Thing(0), 26900, 1280, 26112 + 10, 0);
            Old(Thing(1), 26250, 1280, 26112 + 10, 0);
            Furnish(Citadel.CitadelProps.JollyRoger, 26600, 3250, 26112 + 4, 0);
            // "a bayeaux tapestry style hanging based on the first game ... draping over the top of the wall" (the front wall, z 63, x 36-53,
            // its top layer 8's floor, y 2048): its parts side by side along it, each standing on the wall's top (so the engine redraws no
            // brick of the wall over it: only those from the actor's own layer up) and out by its outside face (Citadel/CitadelProps.Bayeux)
            for (var k = 0; k < Citadel.CitadelProps.BayeuxParts; k++)
            {
                const int Span = Citadel.CitadelProps.BayeuxColumns * Citadel.CitadelProps.BayeuxStitch;
                var part = Citadel.CitadelProps.Actor(Citadel.CitadelProps.Bayeux + k, 36 * 512 + k * Span + Span / 2, 2048, 64 * 512 - Citadel.CitadelProps.BayeuxBack);
                part.Flags |= 0x8000u;   // (MINI_ZV: its box from its least extent, its depth -- not reaching into the room)
                furnishings.Add(p.Add(model, part, "void comportement_0()\n{\n}\n"));
            }
            // (LBA1's old way out, its stairs blocked by the boulders: Twinsen's word there -- the user: "if Twinsen goes over to it have him comment 'This exit is
            // no longer useable due to a land slide'")
            // (from z 60: the boulders keep him from the landing itself)
            model.Zones.Add(new SceneZoneModel { Type = 2, Num = LandslideZone, X0 = 15872, X1 = 17919, Y0 = 768, Y1 = 2815, Z0 = 30000, Z1 = 32255, Info = new int[8] });
            var lap = model.TrackPoints.Count;
            model.TrackPoints.Add(new SceneTrackPoint((int)Math.Round(ZoeInChamber.X + fx * 170), ZoeInChamber.Y, (int)Math.Round(ZoeInChamber.Z + fz * 170)));
            var across = (zoe.Beta - 1024 + 4096) % 4096;
            const int Slaps1 = 10, Slapped1 = 11, Slaps2 = 20, Slapped2 = 21;
            string Slaps(int n) => slap < 0 ? "wait_nb_second(1);\n" : string.Concat(Enumerable.Repeat($"sample({slap});\nwait_nb_dizieme(3);\n", n));
            zoe.Flags &= ~3u;   // (and no brick collision: her hips back over the chair)
            zoe.Flags |= RaceTrackScenes.CarrierFlag;   // (a carrier: goto_point_3d glides her up the lift beside Twinsen)
            var zoeIndex = model.Actors.Count;
            // (her check once, as the scene starts: run every frame, the scolding's end -- Drunk past DrankTonight -- took her out of the
            // scene just as she went up the lift with him)
            var zoeLife = p.Add(model, zoe, $@"void comportement_0()
{{
    if ({DrankTonight} == var_game({Drunk}))
    {{
        body(0);
        set_comportement(comportement_1);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
}}
");
            return scripts =>
            {
                zoeLife(scripts);
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

label({ZoeRide});
anim(0);
pos_point({zoeOn});
speed({LiftSpeed});
goto_point_3d({zoeHigh});
stop();
");
                liftLife(scripts);
                // (his behaviour put back to Normal first: each behaviour is its own entity, and only Normal's has the spanking's bodies and his
                // lap's animations -- the user, 2026-11-09, home in Sporty or the like: he stood there in his everyday clothes, body(30) and
                // anim(240) not found)
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
        set_comportement(comportement_5);
    }}
    else if ({LiftDownZone} == zone())
    {{
        set_dir(NO_MOVE);
        set_comportement(comportement_4);
    }}
    else
    {{
        set_comportement(comportement_1);
    }}
}}

void comportement_4()
{{
    if ({LiftDown} == l_track_obj({platform}))
    {{
        set_dir(MOVE_MANUAL);
        set_comportement(comportement_1);
    }}
}}

void comportement_5()
{{
    if ({LiftDown} == l_track_obj({platform}))
    {{
        set_var_game({DayVar}, {TiredAside});
        message_obj({zoeIndex}, {EveningId(p.Say, EveningLine.ZoeScold)});
        obj_col(0);
        comportement_hero(0);
        pos_point({lap});
        beta({across});
        body({Citadel.CitadelSpanking.TwinsenBare});
        anim({Citadel.CitadelSpanking.TwinsenKick});
        set_track_obj({zoeIndex}, label_{Slaps1});
        set_comportement(comportement_2);
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
    swif (1 == action())
    {{
        if ({BoatZone} == zone())
        {{
            message({EveningId(p.Say, EveningLine.Boat)});
        }}
        if ({LiftUpZone} == zone() && {Driving} != comportement_hero())
        {{
            set_dir(NO_MOVE);
            {Ride(lift)}        }}
    }}
}}

void comportement_2()
{{
    if ({Slapped1} == l_track_obj({zoeIndex}))
    {{
        anim({Citadel.CitadelSpanking.TwinsenLap});
        body({Citadel.CitadelSpanking.TwinsenPink});
        message({EveningId(p.Say, EveningLine.TwinsenOw)});
        message_obj({zoeIndex}, {EveningId(p.Say, EveningLine.ZoeNotInCharge)});
        message({EveningId(p.Say, EveningLine.TwinsenYesMaam)});
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
        body({Citadel.CitadelSpanking.TwinsenRed});
        message({EveningId(p.Say, EveningLine.TwinsenOw)});
        message_obj({zoeIndex}, {EveningId(p.Say, EveningLine.ZoeScoldMore)});
        message_obj({zoeIndex}, {EveningId(p.Say, EveningLine.ZoeNoMoreBars)});
        message({EveningId(p.Say, EveningLine.TwinsenPromise)});
        message_obj({zoeIndex}, {EveningId(p.Say, EveningLine.ZoeBed)});
        set_var_game({DayVar}, {Tired});
        set_var_game({Drunk}, {Scolded});
        body(1);
        anim(0);
        pos_point({onLift});
        obj_col(1);
        cinema_mode(0);
        set_dir(NO_MOVE);
        set_track_obj({zoeIndex}, label_{ZoeRide});
        {Ride(lift)}
        set_comportement(comportement_1);
    }}
}}
");
            };
        });
    }
}
