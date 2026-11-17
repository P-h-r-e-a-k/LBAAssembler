using LBAAssembler.Scenes;
using LBAAssembler.LbaScript;

namespace LBAAssembler.Terrain;

// Stella Entities' people (StellaEntities, the club under the Emerald Moon's reactor; docs/LBRA_STRIP_JOINT_DESIGN.md: "Bouncers: Rick's,
// entity 297 (two at the entrance, one at backstage) and 302 (the office)"; "the bouncers at the entrance let him through and only keep
// him out of backstage and the office"). Copies of Otringal's own -- Rick's bouncer (scene 136, entity 297), his barman (294) and the
// guard at his office's door (scene 137, entity 302) -- and, Rick's bar having none, an Esmer of the moon base's (its mechanic, scene 23's
// actor 14) behind the counter as the barman -- with scripts of their own (Rick's pick fights): standing at their places facing
// the room, each with a word for Twinsen as he comes near (once a visit, again once he has gone away and come back), the barman's on
// Action at his counter.
internal static partial class RaceTrackStory
{
    private const int RickBar = 136, RickOffice = 137, BouncerEntity = 297, BossGuardEntity = 302, MoonBase = 23, BaseMechanic = 14;
    // (the club's scene variables: whether each has had his word this time he came near)
    private const int ClubSaidFirst = 10;
    private const uint NoShadowFlag = 1u << 12;
    // the patrons round the stage (the user, 2026-11-13: "Let's also add some other patrons to the strip joint"): Rick's own regulars from
    // his bar (scene 136) -- the lizard leaning on his table (actor 2) and the dark one on the counter (26), leaning on the stage's rail; the
    // Sup in his coat, a glass in his hand (25), twice, and the Franco in stripes (17) standing at it -- where (degrees round the room, cells
    // out: just outside the rail), facing the stage
    private static readonly (int Actor, float Degrees, float Cells)[] Patrons = { (2, 35, 8.15f), (26, 185, 8.15f), (25, 80, 8.3f), (25, 225, 8.3f), (17, 135, 8.3f), (2, 320, 8.15f) };

    // The catwalk's bouncer (the user, 2026-11-16, above): on the catwalk at the stage's height, z CatwalkPost (between the steps' top, z 14-15,
    // and the stage, from z 24.5), facing the steps, "staff only" as Twinsen comes near. On the stage (StageZone: the stage's square and the
    // catwalk south of him, at the stage's height -- the floor under it is far below the zones), the casino's throw-out (scene 136: the
    // guard's grab and throw, his animations 579 and 142 -- Rick's bouncers are the same entity, 297 -- and Twinsen's thrown, 60): Twinsen held
    // still, the bouncer walks to him (MOVE_FOLLOW, or a moment at most -- the reactor's column may be between them), grabs him, and carries him
    // (RACEMOD.CPP carry_hero=: Twinsen held up in front of him) walking the way off the stage -- along the catwalk, down its steps, round
    // the hall clear of the booths and tables, out of its door and along the corridor -- to the club's lift, and throws him onto its plate;
    // the controls his again, the bouncer back at his post.
    internal const int CarryCube = 30, CarryUp = 520, CarryAhead = 420;
    // (StageZone the stage's north half and the catwalk south of him, StageSouth its south half: carried from there, round the column's east)
    // (the office's and the backstage door's switches: off while Twinsen is carried -- held up in front of the bouncer he went through the
    // backstage door's zone as the bouncer turned towards the steps)
    private const int BackstageSwitch = 9, OfficeSwitch = 10;
    private const int StageZone = 90, StageSouth = 91, CarryWait = 31, CarrySide = 32, CatwalkPost = 18 * 512, ThrowOutWait = 150, CarriedAnim = 60;
    private const int BGuard = 0, BWalk = 1, BWalkSouth = 11, BAtRim = 9, BAtSteps = 12, BGrab = 2, BGrabbed = 3, BCarry = 4, BCarrySouth = 10, BAtLift = 5, BThrow = 6, BThrown = 7, BHome = 8, HThrown = 150, HLanded = 151;
    private static void CatwalkBouncer(SceneModel model, SceneActorModel bouncer, int staffOnly, List<Action<SceneScripts>> adds)
    {
        var stageTop = StellaEntities.FloorY + (int)Citadel.CitadelProps.StageHigh;
        var (cx, cz) = StellaEntities.At(0, 0);
        int x0 = StellaEntities.CatwalkX0World, x1 = x0 + 1024;
        var a = bouncer.Clone();
        (a.X, a.Y, a.Z) = (x0 + 512, stageTop, CatwalkPost);
        a.Beta = 2048;
        a.Move = 3;
        // (walking his way -- goto_point, his walk's own steps: gliding on goto_point_3d, his walk carried him the other way as fast; never
        // falling -- walking to Twinsen along the catwalk he stepped off its edge to the floor -- and no collisions of his own as he moves,
        // through the reactor's column and the dancers: Twinsen's own still stop him against the bouncer)
        a.Flags &= ~(1u | (1u << 11));
        var index = model.Actors.Count;
        // the stage's square and the catwalk south of him, at the stage's height
        const int R = (int)(Citadel.CitadelProps.StageRadius);
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = StageZone, X0 = cx - R, X1 = cx + R, Z0 = cz - R, Z1 = cz - 1, Y0 = stageTop - 150, Y1 = stageTop + 2000, Info = new int[8] });
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = StageSouth, X0 = cx - R, X1 = cx + R, Z0 = cz, Z1 = cz + R, Y0 = stageTop - 150, Y1 = stageTop + 2000, Info = new int[8] });
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = StageZone, X0 = x0, X1 = x1 - 1, Z0 = CatwalkPost + 500, Z1 = cz - R, Y0 = stageTop - 150, Y1 = stageTop + 2000, Info = new int[8] });
        int Point(int x, int y, int z) => SceneOps.AddTrackPoint(model, new SceneTrackPoint(x, y, z));
        int At(float deg, float cells) { var (x, z) = StellaEntities.At(deg, cells); return Point(x, StellaEntities.FloorY, z); }
        // the way out: the stage's north edge by the catwalk, the catwalk by the steps, the steps' top and foot (cells x 25-27 down east, z
        // 14-15), round the hall's north-east at 14.5 cells (the tables at 11.5, the booths and the office's door from 17), the door
        // (angle 0, the corridor's mouth), the corridor, and the lift's foot before its plates (cells 57-58, 27-28)
        // (by the steps along their south side, clear of the backstage door's zone, z up to CatwalkEndZ + BackstageZoneDeep: the bouncer
        // walking up the catwalk with Twinsen held CarryAhead in front of him took him into it, into the backstage room)
        var stepsZ = (Citadel.CitadelProps.StepsZ1 + 1) * 512 - 200;
        const int StepsTop = 2;
        var catwalkZ = StellaEntities.CatwalkEndZ + StellaEntities.BackstageZoneDeep + CarryAhead + 100;
        // (the stage's north rim, where the catwalk meets it, and its east rim, round the column from the south)
        var northRim = Point(x0 + 512, stageTop, cz - R + 100);
        var eastRim = Point(cx + R - 600, stageTop, cz);
        var way = new List<int>
        {
            northRim,
            Point(x0 + 512, stageTop, catwalkZ),
            Point(Citadel.CitadelProps.StepsCellX * 512 + 100, stageTop, stepsZ),
            Point((Citadel.CitadelProps.StepsCellX + Citadel.CitadelProps.StepsCells) * 512 + 300, StellaEntities.FloorY, stepsZ),
            At(300, 14.5f), At(330, 14.5f), At(357, 16f),
            Point(44 * 512, StellaEntities.FloorY, cz), Point(54 * 512, StellaEntities.FloorY, cz),
            Point(56 * 512, StellaEntities.FloorY, 30 * 512),
        };
        var post = Point(a.X, a.Y, a.Z);
        // (onto the lift's plates: their middle, cells (58, 28), their tops LiftTop over the floor)
        var plate = Point(58 * 512, StellaEntities.FloorY + Citadel.CitadelProps.LiftTop, 28 * 512);
        var life = $@"void comportement_0()
{{
    set_var_cube({CarryCube}, 0);
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if (1300 > distance(0))
    {{
        if (0 == var_cube({CarryWait}))
        {{
            message({staffOnly});
            set_var_cube({CarryWait}, 1);
        }}
    }}
    else if (2600 < distance(0))
    {{
        set_var_cube({CarryWait}, 0);
    }}
    if ({StageZone} == zone_obj(0) || {StageSouth} == zone_obj(0))
    {{
        set_change_cube({BackstageSwitch}, 0);
        set_change_cube({OfficeSwitch}, 0);
        cinema_mode(1);
        set_dir_obj(0, NO_MOVE);
        anim_obj(0, 0);
        set_var_cube({CarryWait}, 0);
        set_var_cube({CarrySide}, 0);
        set_track(label_{BWalk});
        if ({StageSouth} == zone_obj(0))
        {{
            set_var_cube({CarrySide}, 1);
            set_track(label_{BWalkSouth});
        }}
        set_comportement(comportement_6);
    }}
}}

void comportement_6()
{{
    if ({BAtRim} == l_track())
    {{
        set_dir(MOVE_FOLLOW, 0);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    add_var_cube({CarryWait}, 1);
    if (700 > distance(0) || {ThrowOutWait} < var_cube({CarryWait}))
    {{
        set_dir(MOVE_TRACK);
        set_track(label_{BGrab});
        set_comportement(comportement_3);
    }}
}}

void comportement_3()
{{
    if ({BGrabbed} == l_track())
    {{
        set_var_cube({CarryCube}, {index});
        anim_obj(0, {CarriedAnim});
        if (1 == var_cube({CarrySide}))
        {{
            set_track(label_{BCarrySouth});
        }}
        else
        {{
            set_track(label_{BCarry});
        }}
        set_comportement(comportement_4);
    }}
}}

void comportement_4()
{{
    if ({BAtSteps} == l_track())
    {{
        fallable(1);
    }}
    if ({BAtLift} == l_track())
    {{
        set_var_cube({CarryCube}, 0);
        set_track_obj(0, label_{HThrown});
        set_track(label_{BThrow});
        set_comportement(comportement_5);
    }}
}}

void comportement_5()
{{
    if ({HLanded} == l_track_obj(0))
    {{
        set_dir_obj(0, MOVE_MANUAL);
        cinema_mode(0);
        set_change_cube({BackstageSwitch}, 1);
        set_change_cube({OfficeSwitch}, 1);
        set_var_cube({CarryWait}, 0);
        fallable(0);
        set_track(label_{BHome});
        set_comportement(comportement_1);
    }}
}}
";
        // (falling again from the steps' top on -- down them and on the hall's floor; never before, on the catwalk and the stage: BAtSteps)
        var carry = string.Concat(way.Select((w, k) => $"goto_point({w});\n" + (k == StepsTop ? $"\nlabel({BAtSteps});\n" : "")));
        var track = $@"label({BGuard});
anim(0);
stop();

label({BWalkSouth});
anim(0);
angle(0);
anim(1);
goto_point({northRim});
goto_point({eastRim});
anim(0);
goto(label_{BAtRim});

label({BWalk});
anim(0);
angle(0);
anim(1);
goto_point({northRim});
anim(0);

label({BAtRim});
anim(1);
stop();

label({BGrab});
anim(579);
wait_anim();
anim(0);

label({BGrabbed});
stop();

label({BCarrySouth});
anim(1);
goto_point({eastRim});

label({BCarry});
anim(1);
{carry}beta(1536);
anim(0);

label({BAtLift});
stop();

label({BThrow});
anim(142);
wait_anim();
anim(0);

label({BThrown});
stop();

label({BHome});
pos_point({post});
beta(2048);
anim(0);
stop();
";
        var hero = $@"
label({HThrown});
pos_point({plate});
beta(1536);
anim({CarriedAnim});
wait_anim();
anim(0);

label({HLanded});
stop();
";
        var add = IslandBeatsA.Add(model, a, life);
        adds.Add(scripts =>
        {
            add(scripts);
            scripts.SetText(index, ScriptKind.Track, track);
            var heroTrack = scripts.GetText(0, ScriptKind.Track).TrimEnd();
            if (!heroTrack.Contains("label(")) heroTrack += "\nlabel(0);\nstop();";
            scripts.SetText(0, ScriptKind.Track, heroTrack + "\n" + hero);
        });
    }

    private static void StellaPeople(IslandBeatsA b, Func<MoonLine, int> say)
    {
        if (!b.Store.SceneExists(StellaEntities.Scene)) { b.Log.Add($"Stella Entities' people left out: no scene {StellaEntities.Scene}"); return; }
        var bouncer = CopyOf(b.Originals, RickBar, BouncerEntity);
        var barman = Copy(b.Originals, MoonBase, BaseMechanic);
        var guard = CopyOf(b.Originals, RickOffice, BossGuardEntity);
        if (bouncer is null || barman is null || guard is null) { b.Log.Add("Stella Entities' people left out: Rick's bouncer or guard isn't in Otringal's scenes, or the moon base's mechanic isn't in scene 23"); return; }
        b.Edit(StellaEntities.Scene, "Stella Entities' people: Rick's bouncers at the way in and the catwalk, his guard at the office's door, an Esmer barman", model =>
        {
            var adds = new List<Action<SceneScripts>>();
            var said = ClubSaidFirst;
            // someone at a place round the room's middle (degrees, cells), facing it, a word as Twinsen comes within `near`
            void Place(SceneActorModel who, (int X, int Z) at, int line, int near, bool onAction = false)
            {
                var a = who.Clone();
                (a.X, a.Y, a.Z) = (at.X, StellaEntities.FloorY, at.Z);
                a.Beta = StellaEntities.FacingMiddle(at.X, at.Z);
                a.Move = 0;
                var v = said++;
                var life = onAction
                    ? $@"void comportement_0()
{{
    swif (1 == action())
    {{
        if ({near} > distance(0))
        {{
            message({line});
        }}
    }}
}}
"
                    : $@"void comportement_0()
{{
    if ({near} > distance(0))
    {{
        if (0 == var_cube({v}))
        {{
            message({line});
            set_var_cube({v}, 1);
        }}
    }}
    else if ({near * 2} < distance(0))
    {{
        set_var_cube({v}, 0);
    }}
}}
";
                adds.Add(IslandBeatsA.Add(model, a, life));
            }
            // the way in: either side of the corridor's mouth; the catwalk's foot, beside it; the office's door; the barman behind his counter
            Place(bouncer, StellaEntities.At(-12, 16.6f), say(MoonLine.ClubWelcome), 1600);
            Place(bouncer, StellaEntities.At(15, 16.6f), say(MoonLine.ClubWelcome), 0);
            // (the catwalk's: up ON the catwalk, between the steps' top and the stage, facing the steps -- the user, 2026-11-16, "Let's have one
            // of the bouncers stand on the catwalk to prevent Twinsen getting onto the stage. If Twinsen manages to get onto the stage, have a
            // bouncer pick him out, carry him over to the exit platform and throw him onto it, similar to how he can get thrown out of the
            // casino": CatwalkBouncer)
            CatwalkBouncer(model, bouncer, say(MoonLine.ClubStaffOnly), adds);
            // (the guard beside the office's door, not in front of it: the door lets Twinsen in -- the user, 2026-11-13, "Did we add in an
            // office area?")
            Place(guard, StellaEntities.At(Citadel.CitadelProps.OfficeAt - 7, 17.3f), say(MoonLine.ClubNoBoss), 1300);
            // the office's door and the backstage door (the catwalk's end, the stage's height): walked into, into the rooms (StellaRooms) --
            // each zone's corner to the room's place just inside its own door, Twinsen as far into it as into the zone (the engine keeps
            // where in a door's zone he is)
            var (ox, oz) = StellaEntities.At(Citadel.CitadelProps.OfficeAt, 18.1f);
            model.Zones.Add(new SceneZoneModel { Type = 0, Num = StellaRooms.Office, X0 = ox - 450, X1 = ox + 450, Z0 = oz - 450, Z1 = oz + 450, Y0 = StellaEntities.FloorY, Y1 = StellaEntities.FloorY + 1800,
                Info = new[] { StellaRooms.InsideX - 450, StellaRooms.InsideY, StellaRooms.InsideZ - 450, 0, OfficeSwitch, 0, 0, 1 } });
            var stageTop = StellaEntities.FloorY + (int)Citadel.CitadelProps.StageHigh;
            // (back to the catwalk's last cell's edge, z 15: its end pinches between the walls either side of the doorway, and walking up
            // its middle Twinsen stopped short of a shallower zone -- the user, 2026-11-16, "Twinsen is still getting blocked by an invisible
            // wall just here"; the room's place moved back as far, so he comes in where he did)
            const int BackDeep = StellaEntities.BackstageZoneDeep, BackDeepWas = 540;
            var backZ1 = StellaEntities.CatwalkEndZ + BackDeep;
            model.Zones.Add(new SceneZoneModel { Type = 0, Num = StellaRooms.Backstage, X0 = StellaEntities.CatwalkX0World, X1 = StellaEntities.CatwalkX0World + 1023, Z0 = StellaEntities.CatwalkEndZ, Z1 = backZ1, Y0 = stageTop, Y1 = stageTop + 1800,
                Info = new[] { StellaRooms.InsideX - 512, StellaRooms.InsideY, StellaRooms.InsideZ - 270 - (BackDeep - BackDeepWas), 0, BackstageSwitch, 0, 0, 1 } });
            // (behind the counter's last third, clear of the reactor's column in front of its middle from the camera)
            Place(barman, StellaEntities.At(237, 16.6f), say(MoonLine.ClubBarman), 1700, onAction: true);
            // the seven performers (StellaPerformers: the game's own bodies recoloured, bodies of the hero's entity, each her own dance): three
            // on the poles (10, 60 and 110 degrees -- the actor on the pole's spot: her body stands beside it, her dance takes her round,
            // up and over it), four on the stage's other sides, strutting along its edge; no one's blows stopped by them (CHECK_OBJ_COL off).
            // Their boxes take in the room their dances use, far bigger than they are: walked over (WalkOverArmour, RACEMOD.CPP
            // RaceMod_NoCollide), or Twinsen bumped into them and stood on top of them off the stage, and fell from them for ever; and no shadow,
            // which the engine sizes by the box (a big round one beside her, not under her)
            void Perform(int who, float degrees, float cells, bool atPole)
            {
                var a = bouncer.Clone();
                var (x, z) = StellaEntities.At(degrees, cells);
                (a.X, a.Y, a.Z) = (x, StellaEntities.FloorY + (int)Citadel.CitadelProps.StageHigh, z);
                a.Entity = StellaPerformers.Entity;
                a.Body = StellaPerformers.Body0 + who;
                a.Anim = StellaPerformers.AnimOf(who);
                a.Flags &= ~1u;
                a.Flags |= NoShadowFlag;
                a.Armor = WalkOverArmour;
                a.Beta = (StellaEntities.FacingMiddle(x, z) + 2048) & 4095;
                a.Move = 0;
                adds.Add(IslandBeatsA.Add(model, a, "void comportement_0()\n{\n}\n"));
            }
            Perform(3, 10, StellaEntities.PoleCells, true); Perform(1, 60, StellaEntities.PoleCells, true); Perform(6, 110, StellaEntities.PoleCells, true);
            Perform(0, 160, 6.2f, false); Perform(2, 205, 6.2f, false); Perform(4, 300, 6.2f, false); Perform(5, 345, 6.2f, false);
            // the patrons: still, where they are put -- not pushed off the stage's solid cells, nor fallen (CHECK_BRICK_COL, OBJ_FALLABLE off)
            foreach (var (actor, degrees, cells) in Patrons)
            {
                if (Copy(b.Originals, RickBar, actor) is not { } patron) continue;
                var (x, z) = StellaEntities.At(degrees, cells);
                (patron.X, patron.Y, patron.Z) = (x, StellaEntities.FloorY, z);
                patron.Beta = StellaEntities.FacingMiddle(x, z);
                patron.Move = 0;
                patron.Flags &= ~(2u | 0x800u);
                // (walked through, as the dancers are: their boxes, wider than they look, leaning on the rail, walled the way round the
                // stage -- the user, 2026-11-17, "There's a weird section here preventing Twinsen from walking forwards, I'm not sure why",
                // the patron at 320 degrees, 8.15 cells out)
                patron.Armor = WalkOverArmour;
                adds.Add(IslandBeatsA.Add(model, patron, "void comportement_0()\n{\n}\n"));
            }
            return scripts => { foreach (var add in adds) add(scripts); };
        });
        DressingPeople(b, bouncer, say);
    }

    // The dressing room's performers (StellaRooms.Backstage -- the user, 2026-11-16: "an area where the performers get changed"): three of
    // the dancers getting ready -- at the mirrors, at the costume rail, by the folding screen -- each a line of her own when Twinsen talks
    // to her. Standing (the hero's entity's own stand), walked through as the hall's are (their boxes take in their dances: WalkOverArmour,
    // no shadow)
    private static readonly (int Who, int X, int Z, int Beta, MoonLine Line)[] Dressing =
    {
        (1, StellaRooms.DressTableX + 1250, StellaRooms.DressTableZ, 3072, MoonLine.DressMirror), (4, 5888, 5250, 2048, MoonLine.DressRail), (6, 7000, 5000, 3376, MoonLine.DressScreen),
    };
    private static void DressingPeople(IslandBeatsA b, SceneActorModel bouncer, Func<MoonLine, int> say)
    {
        if (!b.Store.SceneExists(StellaRooms.Backstage)) { b.Log.Add($"Stella Entities' dressing room's performers left out: no scene {StellaRooms.Backstage}"); return; }
        b.Edit(StellaRooms.Backstage, "Stella Entities' dressing room: three performers getting ready, a word each", model =>
        {
            var adds = new List<Action<SceneScripts>>();
            foreach (var (who, x, z, beta, line) in Dressing)
            {
                var a = bouncer.Clone();
                (a.X, a.Y, a.Z, a.Beta) = (x, StellaEntities.FloorY, z, beta);
                a.Entity = StellaPerformers.Entity;
                a.Body = StellaPerformers.Body0 + who;
                a.Anim = 0;
                a.Flags &= ~1u;
                a.Flags |= NoShadowFlag;
                a.Armor = WalkOverArmour;
                a.Move = 0;
                adds.Add(IslandBeatsA.Add(model, a, Talker($"1 == var_game({StellaEntities.AdultVar})", $"            message({say(line)});", 1500)));
            }
            return scripts => { foreach (var add in adds) add(scripts); };
        });
    }
}
