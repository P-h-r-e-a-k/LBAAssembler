using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Esmers' hangar (scene 12, the Desert's Esmer base room): the way to the Emerald Moon (the user, 2026-11-13: "totally redesign scene 12
// ... a giant space ship with alien cars being loaded on and off of it, we could include our black car ... a way of Twinsen getting to
// Emerald Moon and onto Otringal without just teleporting"; 2026-11-14, option C: the hangar indoors, on foot -- his car loaded for him).
//   - The Desert won, Baldino takes him to the Esmers' hangar (change_cube 12, chapter 4) instead of his rocket: Twinsen at the foot of
//     the long hall south of the pad.
//   - The freighter on the raised pad (HangarProps.Freighter: the game's Esmer saucer, its cargo hatch east, a ramp down to the pad, the
//     pad's own ramp down to the hall); the base's people and its small saucer gone.
//   - Alien cars (the moon rover, Baldino's lander, the guard's saucer: the racers' bodies) driven up the ramps into the hold and out again
//     down to the hall's far end, over and over.
//   - The masked driver's black car (body 95) parked by the ramp's foot: Twinsen comes near and it drives up and into the hold -- off to the
//     moon ahead of him (the Moon's chapter has it past the finish there).
//   - The Esmer dock officer by the ramp's foot (the moon base's mechanic): a word as Twinsen comes near, and on Action: the freighter to the
//     Emerald Moon, shall we load your car? Yes: his buggy (HangarProps.FreighterBuggy) drives up into the hold and he's told to go aboard;
//     walking up to the hatch (HatchZone) he goes in, the freighter lifts off, and he's on the moon (change_cube 229, the Moon's start).
internal static partial class RaceTrackStory
{
    private const int HangarScene = 12, HangarSaucer = 6, HatchZone = 30;
    // the base's people (its guards, prisoners, workers: entities 31, 46, 48-54) and the small saucer (entity 47) taken away
    private static readonly HashSet<int> HangarGone = new() { 31, 46, 47, 48, 49, 50, 51, 52, 53, 54 };
    // cube variables: the officer's offer made (1), boarding (1 told to, 2 aboard), the black car gone
    private const int HangarOffered = 40, Boarding = 41, BlackGone = 42;
    // the track's labels: into the hold (the car hidden there), out of it, the freighter up
    private const int InHold = 10, OutOfHold = 11, LiftOff = 1, LiftHigh = 2;
    // the freighter's middle on the pad (its top 2304), the pad's ramp (its foot on the hall's floor, 1024, its top at the pad's edge),
    // the hall's corridor south of it, its far end; the officer; Twinsen's arrival; the black car's and the buggy's places
    private const int PadY = 2304, HallY = 1024, ShipX = 6300, ShipZ = 9472;
    private static readonly (int X, int Y, int Z) RampTop = (9300, PadY, ShipZ), RampFoot = (11264, HallY, ShipZ), Corridor = (12032, HallY, ShipZ),
        FarEnd = (12032, HallY, 25600), HoldHatch = (ShipX + (int)HangarProps.HatchX, PadY + (int)HangarProps.HatchY, ShipZ), Hold = (ShipX, PadY + 300, ShipZ),
        OfficerAt = (12544, HallY, 10752), ArriveAt = (12032, HallY, 24064), BlackAt = (12032, HallY, 12800), BuggyAt = (11400, HallY, 11776),
        LiftUp = (ShipX, PadY + 2000, ShipZ), LiftTop = (ShipX, PadY + 3900, ShipZ);
    // the alien cars (the racers' entity's bodies), each its own start in the loop and speed
    private static readonly (int Body, int Wait, int Speed)[] HangarCars = { (8, 0, 1800), (14, 90, 1600), (15, 180, 2000) };

    // the hangar's lines (text ids of its island's file): the officer's offer, the answers, go aboard, later, the lift-off, no flights
    internal sealed record HangarLines(int Offer, int Yes, int No, int Board, int Later, int Lift, int Closed);

    // The hangar in `scene` (12 on the Desert; its copy, 208, on the Moon: MoonHangar) for the chapter it flies in, off to `destination`
    private static void Hangar(IslandBeatsA b, int scene, int chapter, int destination, HangarLines say)
    {
        if (!b.Store.SceneExists(scene)) { b.Log.Add($"the Esmers' hangar left out: no scene {scene}"); return; }
        var officer = Copy(b.Originals, MoonBase, BaseMechanic);
        if (officer is null) { b.Log.Add("the Esmers' hangar left out: no Esmer mechanic in the moon base (scene 23) to copy"); return; }
        b.Edit(scene, $"the Esmers' hangar: the freighter (on to scene {destination}), the cars loaded, the black car, the dock officer, the lift-off (chapter {chapter})", model =>
        {
            var adds = new List<Action<SceneScripts>>();
            var tracks = new List<(int Index, string Track)>();
            var gone = Enumerable.Range(1, model.Actors.Count - 1).Where(i => i == HangarSaucer || HangarGone.Contains(model.Actors[i].Entity)).ToList();
            int Pt((int X, int Y, int Z) p) => SceneOps.AddTrackPoint(model, new SceneTrackPoint(p.X, p.Y, p.Z));
            int far = Pt(FarEnd), corridor = Pt(Corridor), foot = Pt(RampFoot), top = Pt(RampTop), hatch = Pt(HoldHatch), hold = Pt(Hold),
                up = Pt(LiftUp), high = Pt(LiftTop), black = Pt(BlackAt), buggyAt = Pt(BuggyAt);
            string Aboard(int speed) => $"speed({speed});\ngoto_point_3d({corridor});\ngoto_point_3d({foot});\ngoto_point_3d({top});\ngoto_point_3d({hatch});\n";
            // (hidden while its track is in the hold)
            const string HiddenInHold = "void comportement_0()\n{\n    if (10 == l_track())\n    {\n        invisible(1);\n    }\n    if (11 == l_track())\n    {\n        invisible(0);\n    }\n}\n";

            // Twinsen arrives at the hall's south end, facing north up it
            var hero = model.Actors[0];
            (hero.X, hero.Y, hero.Z, hero.Beta) = (ArriveAt.X, ArriveAt.Y, ArriveAt.Z, 2048);

            // the freighter
            var ship = Citadel.CitadelProps.Actor(HangarProps.Freighter, ShipX, PadY, ShipZ, 0);
            ship.Move = 3;
            var shipIndex = model.Actors.Count;
            adds.Add(IslandBeatsA.Add(model, ship, "void comportement_0()\n{\n}\n"));
            // (lifted a step at a time: a prop has no walk to carry it, and goto_point_3d set it at the top at once -- slow off the pad,
            // quicker as it climbs; LiftHigh half way up)
            var climb = new System.Text.StringBuilder($"label(0);\nstop();\n\nlabel({LiftOff});\n");
            for (var k = 1; k <= 36; k++)
            {
                var y = PadY + (int)(LiftTop.Y - PadY) * k * k / (36 * 36);
                if (k == 26) climb.Append($"label({LiftHigh});\n");
                climb.Append($"pos_point({Pt((ShipX, y, ShipZ))});\nwait_nb_dizieme(1);\n");
            }
            climb.Append("stop();\n");
            tracks.Add((shipIndex, climb.ToString()));

            SceneActorModel Car(int body, (int X, int Y, int Z) at, int beta)
            {
                var car = SceneOps.BlankActor(SceneGame.Lba2, at.X, at.Y, at.Z, RaceTrackBaldinoCar.RacerEntity);
                car.Body = body; car.Beta = beta; car.Flags = 0x1000u; car.Move = 3; car.LifePoints = -1; car.Armor = WalkOverArmour;
                return car;
            }
            // the alien cars: in from the hall's far end, up into the hold, a while there, out and down and away, again
            foreach (var (body, wait, speed) in HangarCars)
            {
                var i = model.Actors.Count;
                adds.Add(IslandBeatsA.Add(model, Car(body, FarEnd, 2048), HiddenInHold));
                tracks.Add((i, $"label(0);\npos_point({far});\nwait_nb_dizieme({wait + 10});\n\nlabel(1);\n{Aboard(speed)}\nlabel({InHold});\npos_point({hold});\nwait_nb_dizieme(120);\npos_point({hatch});\n\nlabel({OutOfHold});\n" +
                               $"speed({speed});\ngoto_point_3d({top});\ngoto_point_3d({foot});\ngoto_point_3d({corridor});\ngoto_point_3d({far});\nwait_nb_dizieme(60);\ngoto(label_1);\n"));
            }

            // the black car: parked by the ramp's foot, off up into the hold as Twinsen comes near (chapter 4, once)
            var blackIndex = model.Actors.Count;
            adds.Add(IslandBeatsA.Add(model, Car(RaceTrackCharacterCars.MaskedGeneric, BlackAt, 2048), $@"void comportement_0()
{{
    if ({chapter} != chapter() || 1 == var_cube({BlackGone}))
    {{
        invisible(1);
        suicide();
    }}
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if (4000 > distance(0))
    {{
        set_var_cube({BlackGone}, 1);
        set_track(label_1);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if ({InHold} == l_track())
    {{
        invisible(1);
        suicide();
    }}
}}
"));
            tracks.Add((blackIndex, $"label(0);\npos_point({black});\nstop();\n\nlabel(1);\n{Aboard(2600)}\nlabel({InHold});\nstop();\n"));

            // Twinsen's buggy, beside the ramp's foot (chapter 4): loaded on the officer's word
            var buggyIndex = model.Actors.Count;
            var buggy = Citadel.CitadelProps.Actor(HangarProps.FreighterBuggy, BuggyAt.X, BuggyAt.Y, BuggyAt.Z, 3072);
            buggy.Move = 3; buggy.Armor = WalkOverArmour;
            adds.Add(IslandBeatsA.Add(model, buggy, $@"void comportement_0()
{{
    if ({chapter} != chapter())
    {{
        invisible(1);
        suicide();
    }}
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if ({InHold} == l_track())
    {{
        invisible(1);
    }}
}}
"));
            // (driven a step at a time, as the freighter is lifted: a prop, goto_point_3d set it in the hold at once -- turned west, up the
            // pad's ramp and the freighter's, in at the hatch)
            var drive = new System.Text.StringBuilder($"label(0);\npos_point({buggyAt});\nstop();\n\nlabel(1);\n");
            var way = new[] { BuggyAt, (RampFoot.X, RampFoot.Y, BuggyAt.Z), RampFoot, RampTop, HoldHatch };
            for (var s = 1; s < way.Length; s++)
            {
                var (ax, ay, az) = way[s - 1]; var (bx, by, bz) = way[s];
                var steps = Math.Max(1, (int)Math.Ceiling(Math.Sqrt((double)(bx - ax) * (bx - ax) + (double)(bz - az) * (bz - az)) / 110));
                for (var k = 1; k <= steps; k++)
                    drive.Append($"pos_point({Pt((ax + (bx - ax) * k / steps, ay + (by - ay) * k / steps, az + (bz - az) * k / steps))});\nwait_nb_dizieme(1);\n");
            }
            drive.Append($"\nlabel({InHold});\nstop();\n");
            tracks.Add((buggyIndex, drive.ToString()));

            // the dock officer
            var o = officer.Clone();
            (o.X, o.Y, o.Z) = OfficerAt;
            o.Beta = 3072; o.Move = 0;
            var officerIndex = model.Actors.Count;
            adds.Add(IslandBeatsA.Add(model, o, $@"void comportement_0()
{{
    if (0 == var_cube({HangarOffered}) && 1800 > distance(0))
    {{
        set_var_cube({HangarOffered}, 1);
        set_comportement(comportement_1);
    }}
    swif (1 == action())
    {{
        if (1500 > distance(0) && {Driving} != comportement_hero())
        {{
            set_comportement(comportement_1);
        }}
    }}
}}

void comportement_1()
{{
    if ({chapter} != chapter())
    {{
        message_obj({officerIndex}, {say.Closed});
    }}
    else
    {{
        if (0 == var_cube({Boarding}))
        {{
            add_choice({say.Yes});
            add_choice({say.No});
            ask_choice_obj({officerIndex}, {say.Offer});
            if ({say.Yes} == choice())
            {{
                set_var_cube({Boarding}, 1);
                set_track_obj({buggyIndex}, label_1);
                message_obj({officerIndex}, {say.Board});
            }}
            else
            {{
                message_obj({officerIndex}, {say.Later});
            }}
        }}
        else
        {{
            message_obj({officerIndex}, {say.Board});
        }}
    }}
    set_comportement(comportement_0);
}}
"));

            // the hatch: walked into, boarding, Twinsen aboard and the freighter up; at its height, the moon
            model.Zones.Add(new SceneZoneModel { Type = 2, Num = HatchZone, X0 = HoldHatch.X - 500, X1 = RampTop.X - 60, Z0 = ShipZ - 500, Z1 = ShipZ + 500, Y0 = PadY - 100, Y1 = PadY + 1600, Info = new int[8] });
            var keeper = RaceTrackScenes.InvisibleHelper(OfficerAt.X, HallY, OfficerAt.Z);
            adds.Add(IslandBeatsA.Add(model, keeper, $@"void comportement_0()
{{
    if (1 == var_cube({Boarding}) && {HatchZone} == zone_obj(0))
    {{
        set_var_cube({Boarding}, 2);
        set_track_obj({shipIndex}, label_{LiftOff});
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    if ({LiftHigh} == l_track_obj({shipIndex}))
    {{
        message({say.Lift});
        change_cube({destination});
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
}}
"));
            const string heroLife = "void comportement_0()\n{\n    set_comportement(comportement_1);\n}\n\nvoid comportement_1()\n{\n    if (2 == var_cube(41))\n    {\n        set_dir(NO_MOVE);\n        invisible(1);\n        set_comportement(comportement_2);\n    }\n}\n\nvoid comportement_2()\n{\n}\n";
            return scripts =>
            {
                foreach (var add in adds) add(scripts);
                foreach (var (i, track) in tracks) scripts.SetText(i, ScriptKind.Track, track);
                // (gone at once; their tracks kept, and empty behaviours in their places, as the base's other actors' scripts name them)
                var empty = string.Concat(Enumerable.Range(1, 9).Select(k => $"\nvoid comportement_{k}()\n{{\n}}\n"));
                foreach (var i in gone) scripts.SetText(i, ScriptKind.Life, "void comportement_0()\n{\n    invisible(1);\n    suicide();\n}\n" + empty);
                scripts.SetText(0, ScriptKind.Life, heroLife);
            };
        });
    }
}
