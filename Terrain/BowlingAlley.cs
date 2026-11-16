using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;
using LBAAssembler.LbaScript;
using LBAAssembler.Terrain.Citadel;

namespace LBAAssembler.Terrain;

// The hidden bowling alley under Otringal's prison exit (the user, 2026-11-12: "a dark dingy place. The characters are some of the evil
// characters from in game and their bowling balls are live spheros, their pins are live Francos. Let's have the francos have helmets on their
// heads attached to chains so they can be reset into place by the bowlers pulling a giant lever"; the way in: a lift down from the middle of
// the square platform by the prison exit, scene 90 -- an Action lift as Stella Entities' is).
//   - the scene (393): the prison's own dark library (scene 84's), a floor of its flagstones and walls of its tall stone, its dim light; the lift's
//     foot at the south-west corner, three lanes running north (the outer two played), the foul line south, the pins' deck at the north end;
//   - the people: on each outer lane an evil one of the game's -- a monkey monster (entity 275) and a swordsman monkey (252) -- the Sphero (28)
//     at his feet for his ball, six Francos (56, the grey guards) for the pins, each a helmet on a chain over it, and a giant lever
//     at the lane's side;
//   - a round of a lane, sixteen seconds, kept by an invisible helper's track (its label the phase): 1 the bowler throws and the ball rolls,
//     2 the pins go down, 3 the bowler walks to the lever and pulls it, 4 the chains haul the pins up, 0 they stand again. Each actor's life
//     script waits for the phase it has to do something in (l_track_obj of the helper) and starts its own track label.
internal static class BowlingAlley
{
    public const int Scene = 393, Prison = 84, Outside = 90, OtringalIsland = 4;
    // the floor (Rick's bar's bricks, BowlingRoom: layer 0, its top 256)
    public const int FloorY = (BowlingRoom.FloorLayer + 1) * 256;
    // the lift: its pad outside (scene 90: the square platform by the prison's way out, its middle -- ground 3,900 -- where the user looked
    // for it, 2026-11-14: "This was the spot on Otringal that the elevator down to the bowling area was supposed to be. I can't seem to
    // trigger it"; it was 13 cells east, at 22000, 19800) and its foot inside (cells). The middle of the square painted on its ground (cube
    // (7,9)'s cells x 24-30, z 37-43), not of its zone, 2.5 cells off -- the user stood on the square's middle, 2026-11-15: "Twinsen still
    // can't activate it from where he's currently standing" (14314, 21187)
    public const int PadX = 14080, PadY = 3900, PadZ = 20736;
    // (taken by walking onto it, within PadReach of its middle: no scenario zone, the platform's own -- zone 33 -- keeps the creature on it
    // calm while Twinsen stands on it (its script chases him once he is out of it). PadSettle: scene 90's cell variable counting the
    // helper's first frames.)
    // (Action within PadReach of the square's middle: the square, the user, 2026-11-15: "needs to be this square, and only this square")
    private const int PadReach = 500, PadSettle = 20;
    // (the lift's foot: its four plates in the room's south-east corner, BowlingRoom)
    private const int FootX = 52, FootZ = 43;
    private const int LiftZone = 70, ShaftZone = 77, ArrivalZone = 76, LiftSwitch = 7;
    private const int LiftRide = 1, LiftDown = 2, LiftRise = 3, LiftRisen = 4, LiftSpeed = 600, LiftStart = 506, LiftStop = 510;
    private const int CellDown = 1600, CellUp = 1200, ClubDown = 1200, ClubUp = 1800;
    // the animations: the Franco's standing and its fall (entity 297), the Sphero's rolling, the bowlers' throw and swing
    // (the bowlers the user listed, 2026-11-12, by BODY.HQR entry as LBA Assembler lists them: 388 monkey monster = entity 275, 363 monkey
    // monster with sword = 252, 419 sailor = entity 297's body 186, 295 green gloom = entity 204's body 0. The first build had the Emperor's
    // soldier and the Dark Monk for the last two -- the user, 2026-11-15: "two are correct, and two aren't")
    private const int Franco = 56, Sphero = 28, MonkeyMonster = 275, SwordMonkey = 252, Sailor = 297, SailorBody = 186, GreenGloom = 204;
    private const int StandAnim = 0, FallAnim = 10, RollAnim = 1, WalkAnim = 1, ThrowMonster = 15, SwingSword = 52, PullAnim = 6;

    // (the chains' anchors in the dark over the pins: this far over the floor)
    private const int ChainAnchor = 4400;
    // (the poker table's middle -- cells 42, 36: between the booths and the fruit machines -- and its players' distance from it)
    private const int PokerX = 42 * 512, PokerZ = 36 * 512 + 256, PokerSeat = 1060;   // (2026-11-16: the table bigger, its players out)

    // (the game of bowling the engine plays -- RACEMOD.CPP bowling_game=: it holds this scene variable, and the lane scripts wait)
    // (78: the scene variables are 0-79, ListVarCube[80] -- 99 was past them, the engine writing it as the player bowled: 2026-11-16)
    public const int GameVar = 78;

    private static string Still() => "void comportement_0()\n{\n}\n";

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(Prison) || !store.SceneExists(Outside)) { log.Add("The bowling alley left out: no Otringal prison (scene 84) or prison exit (scene 90)"); return log; }
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        if (Scene + 1 < scenes.Count && !scenes.IsEmpty(Scene + 1)) { log.Add($"The bowling alley left out: scene {Scene} is already another part of the build's"); return log; }
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        if (!store.SceneExists(BowlingRoom.RickBar)) { log.Add("The bowling alley left out: no Rick's bar (scene 136) for its bricks"); return log; }

        // ---- the grid: the seedy twin of Rick's bar (BowlingRoom: his bricks painted dark, the lanes as floor, his booths and his bar)
        if (BowlingRoom.Install(gameDirectory, bkg, header, log) is not { } room) return log;
        var (type, grid) = (room.Type, room.Grid);
        var gridNumber = header.Grm - header.Gri;
        if (gridNumber > 255) { log.Add("The bowling alley left out: LBA_BKG.HQR has 256 grids already"); return log; }
        bkg.InsertAt(header.Grm, grid);
        header.Grm += 1; header.Bll += 1; header.Brk += 1;
        if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
        var library = bkg.Read(header.Bll + grid[0]);
        var (bytes, usedBricks) = MapBricks.UsedBrickBytes(grid, library, b => b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? BinaryPrimitives.ReadUInt32LittleEndian(bkg.ExtentOf(header.Brk + b)) : 0);
        if (header.MaxBrickCube < (uint)((usedBricks + 1) * 4 + bytes)) header.MaxBrickCube = (uint)((usedBricks + 1) * 4 + bytes);
        if (header.MaxMaskCube < (uint)((usedBricks + 1) * 4 + bytes + usedBricks * 64)) header.MaxMaskCube = (uint)((usedBricks + 1) * 4 + bytes + usedBricks * 64);
        bkg.SetStored(0, header.Bytes);
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (table.Length < (Scene + 1) * 2) Array.Resize(ref table, (Scene + 1) * 2);
        table[Scene * 2] = type; table[Scene * 2 + 1] = (byte)gridNumber;
        bkg.SetStored(header.Brk + header.MaxBrk, table);

        // ---- the scene: the prison's header (its dim light, its music), Twinsen arriving on the lift
        var model = store.Load(Prison).Clone();
        model.Island = OtringalIsland;
        model.Actors.RemoveRange(1, model.Actors.Count - 1);
        model.Zones.Clear();
        model.TrackPoints.Clear();
        var texts = new Dictionary<int, (string Life, string Track)>();
        void Text(int actor, string life, string track) => texts[actor] = (life, track);
        var clubLift = ((FootX + 1) * 512, FloorY, (FootZ + 1) * 512);
        var clubArrival = (X: clubLift.Item1, Y: FloorY + CitadelProps.LiftTop + ClubDown, Z: clubLift.Item3);
        var hero = model.Hero;
        (hero.X, hero.Y, hero.Z) = clubArrival;
        hero.Life = new byte[] { 0 }; hero.Track = new byte[] { 0 };
        const int DontReadjust = 1;
        var padFoot = (X: PadX, Y: PadY - CitadelProps.LiftTop, Z: PadZ);
        var clubTop = FloorY + ClubUp - 100;
        // (the platform: one plate, the size of the square outside -- four, two by two, were four times it, the user, 2026-11-15: "in the
        // bowling area the elevator seems to be 4 times the size it needs to be"; where Twinsen arrives on it is where he stood in the
        // pad's door, the same size)
        const int Big = 512;
        model.Zones.Add(new SceneZoneModel
        {
            Type = 0, Num = Outside, X0 = clubLift.Item1 - Big, X1 = clubLift.Item1 + Big - 1, Z0 = clubLift.Item3 - Big, Z1 = clubLift.Item3 + Big - 1, Y0 = clubTop, Y1 = clubTop + 3000,
            Info = new[] { PadX - Big, PadY + 40, PadZ - Big, 0, 0, 0, 0, 1 },
        });
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = LiftZone, X0 = clubLift.Item1 - Big, X1 = clubLift.Item1 + Big - 1, Z0 = clubLift.Item3 - Big, Z1 = clubLift.Item3 + Big - 1, Y0 = FloorY - 100, Y1 = FloorY + 800, Info = new int[8] });
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = ArrivalZone, X0 = clubLift.Item1 - Big, X1 = clubLift.Item1 + Big - 1, Z0 = clubLift.Item3 - Big, Z1 = clubLift.Item3 + Big - 1, Y0 = FloorY + ClubDown - 300, Y1 = FloorY + ClubDown + 900, Info = new int[8] });
        // the lift's parts (carriers, as Stella Entities'): the plate and its rails, on a track up into the dark
        // (track point 0 and actor 1 go unused: a goto to point 0 never arrived, and the first plate, as actor 1, was taken for the game's buggy and
        // stayed up where it started, Twinsen waiting on it for ever)
        SceneOps.AddTrackPoint(model, new SceneTrackPoint(0, FloorY, 0));
        Text(SceneOps.AddActor(model, RaceTrackScenes.InvisibleHelper(0, FloorY, 0)), Still(), "label(0);\nstop();\n");
        var parts = new List<int>();
        foreach (var (dx, dz) in new[] { (0, 0) })
        {
            var low = SceneOps.AddTrackPoint(model, new SceneTrackPoint(clubLift.Item1 + dx, FloorY, clubLift.Item3 + dz));
            var high = SceneOps.AddTrackPoint(model, new SceneTrackPoint(clubLift.Item1 + dx, FloorY + ClubUp, clubLift.Item3 + dz));
            var a = CitadelProps.Actor(CitadelProps.MoonLiftPlatform, clubLift.Item1 + dx, FloorY + ClubDown, clubLift.Item3 + dz);
            a.Flags |= RaceTrackScenes.CarrierFlag; a.Move = 3; a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 };
            var plate = SceneOps.AddActor(model, a);
            parts.Add(plate);
            Text(plate, Still(), $"label(0);\nspeed({LiftSpeed});\ngoto_point_3d({low});\nsample({LiftStop});\n\nlabel({LiftDown});\nstop();\n\n" +
                                 $"label({LiftRide});\nsample({LiftStart});\nspeed({LiftSpeed});\ngoto_point_3d({high});\nstop();\n");
        }
        var ride = string.Concat(parts.Select(i => $"            set_track_obj({i}, label_{LiftRide});\n"));
        var heroLife = $@"void comportement_0()
{{
    if ({ArrivalZone} == zone())
    {{
        set_dir(NO_MOVE);
        set_comportement(comportement_2);
    }}
    else
    {{
        set_comportement(comportement_1);
    }}
}}

void comportement_2()
{{
    if ({LiftDown} == l_track_obj({parts[0]}))
    {{
        set_dir(MOVE_MANUAL);
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if ({LiftZone} == zone())
        {{
            set_dir(NO_MOVE);
{ride}            set_comportement(comportement_3);
        }}
    }}
}}

void comportement_3()
{{
}}
";

        // ---- the room: the lanes, then each outer lane's people and their round
        int Mk(int entity, int bodyGeneric, int x, int z, int beta, int anim, string name)
        {
            var a = SceneOps.BlankActor(SceneGame.Lba2, x, FloorY + CitadelProps.LaneTop, z, entity);
            // (the record's body a signed byte: 186 is -70, as the game's own scenes have it)
            a.Body = (sbyte)bodyGeneric; a.Anim = anim; a.Beta = beta; a.Flags = 0x1000 | RaceTrackScenes.CarrierFlag; a.CoulObj = 9; a.Armor = 51; a.LifePoints = -1; a.Move = 3;
            a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 };
            return SceneOps.AddActor(model, a);
        }
        int Prop(int generic, int x, int y, int z, int beta = 0)
        {
            var a = CitadelProps.Actor(generic > 127 ? (sbyte)generic : generic, x, y, z, beta);
            a.Flags |= RaceTrackScenes.CarrierFlag; a.Move = 3; a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 };
            return SceneOps.AddActor(model, a);
        }
        // a life that waits for the lane's phases in turn -- the lever's track's labels -- and at each does its piece of script; after the last, round again
        static string Chain(int helper, params (int Phase, string Do)[] steps)
        {
            var sb = new System.Text.StringBuilder();
            for (var k = 0; k <= steps.Length; k++)
            {
                sb.Append($"void comportement_{k}()\n{{\n");
                if (k < steps.Length)
                    sb.Append($"    if ({steps[k].Phase} == l_track_obj({helper}) && 0 == var_cube({GameVar}))\n    {{\n        {steps[k].Do}\n        set_comportement(comportement_{k + 1});\n    }}\n");
                else
                    sb.Append("    set_comportement(comportement_0);\n");
                sb.Append("}\n\n");
            }
            return sb.ToString();
        }
        static string Go(int label) => $"set_track(label_{label});";
        // by how the throw went (var_cube `outcome`: 0 a strike, 1 some pins, 2 the gutter): one of three labels
        static string ByOutcome(int outcome, int strike, int some, int gutter) =>
            $"if (2 == var_cube({outcome})) {{ set_track(label_{gutter}); }} else if (1 == var_cube({outcome})) {{ set_track(label_{some}); }} else {{ set_track(label_{strike}); }}";
        var lane = 0;
        var map = new List<string> { $"scene {Scene} {GameVar} {FloorY + CitadelProps.LaneTop} {CitadelProps.LaneHalf}" };
        var bowlerNumber = 0;
        // the four bowlers, two to a lane taking turns: what they throw, how they cheer a strike, shrug at some pins, and curse a gutter ball; the
        // chances of each (tenths: a strike under `Strike`, some pins under `Some`, else the gutter) and how fast the ball goes
        // (the sailor's and the gloom's animations picked from a sheet of their keyframes, ScriptRoundTrip animsheet: the sailor's 579 an
        // underarm swing, 578 arms up and a bow, 28 a pointing gesture, 6 a recoil; the gloom's 481 a lunge along the floor, 480 arms raised,
        // 28 its talking gesture, 482 a slump)
        var bowlers = new (int Lane, int X, int Entity, int Body, string Name, int Throw, int Cheer, int Shrug, int Dismay, int Strike, int Some, int Speed)[]
        {
            // (the speeds the ball rolls at, units a second -- RACEMOD.CPP rolls it: some 2 to 3 seconds down the lane)
            // (their reactions the build's own -- BowlingAnims: a celebration for a strike, a shrug for some pins, a remonstration at the
            // gutter; the game's that stood in for them walked them off down the lane)
            // (2026-11-15, second round: each his own throw -- BowlingAnims.Bowl -- the swordsman without his sword, a ball each in his own
            // colour, and strikes rarer -- "we can't have our players bowling a strike every time"; the engine's pins fall less surely too)
            (0, CitadelProps.LaneX0, MonkeyMonster, 0, "the monkey monster", BowlingAnims.Bowl, BowlingAnims.Celebrate, BowlingAnims.Shrug, BowlingAnims.Remonstrate, 3, 8, 6000),   // strong: strikes, and wild
            (0, CitadelProps.LaneX0, Sailor, SailorBody, "the sailor", BowlingAnims.Bowl, BowlingAnims.Celebrate, BowlingAnims.Shrug, BowlingAnims.Remonstrate, 2, 8, 4800),            // steady: more often some pins
            (1, CitadelProps.LaneX1, SwordMonkey, BowlingAnims.Swordless, "the swordsman monkey", BowlingAnims.Bowl, BowlingAnims.Celebrate, BowlingAnims.Shrug, BowlingAnims.Remonstrate, 4, 9, 5400),   // precise
            (1, CitadelProps.LaneX1, GreenGloom, 0, "the green gloom", BowlingAnims.Bowl, BowlingAnims.Celebrate, BowlingAnims.Shrug, BowlingAnims.Remonstrate, 2, 5, 4200),           // heavy and erratic
        };
        // (a chain's link, for the engine to draw the chains with -- RACEMOD.CPP BowlChainsDraw: an actor out of sight carrying its body)
        var link = Prop(CitadelProps.ChainLink, CitadelProps.LaneMiddleX, FloorY, 2000);
        model.Actors[link].Y = -20000;
        Text(link, Still(), "label(0);\nstop();\n");
        foreach (var cx in new[] { CitadelProps.LaneX0, CitadelProps.LaneX1 })
        {
            var onFloor = FloorY + CitadelProps.LaneTop;
            var who = 10 + lane; var outcome = 12 + lane;
            lane++;
            // the giant lever beside the lane keeps the round (an invisible helper's track did not run): a phase a label of its track, its arm
            // thrown down in the third as the bowler pulls it
            var helper = Prop(CitadelProps.LeverUp, cx + 1750, FloorY, CitadelProps.FoulZ + 500);
            // (the second lane's round starts 8 s after the first's, about half a round: the two never in step -- the user, 2026-11-15, "we
            // need our two lanes out of sync")
            var late = lane == 2 ? "label(9);\nwait_nb_second(8);\n" : "";
            Text(helper, "", late + $"label(0);\nwait_nb_second(2);\nlabel(1);\nwait_nb_second(5);\nlabel(2);\nwait_nb_second(3);\nlabel(3);\nwait_nb_second(2);\nbody({CitadelProps.LeverDown});\nwait_nb_second(1);\nbody({CitadelProps.LeverUp});\nwait_nb_second(1);\nlabel(4);\nwait_nb_second(3);\ngoto(label_0);\n");
            // (and each round the throw's luck is rolled while the bowlers change places, in tenths by whose turn it is)
            var rolls = string.Concat(bowlers.Where(b => b.Lane == lane - 1).Select((b, i) =>
                $"        if ({i} == var_cube({who}))\n        {{\n            if ({b.Strike} > rnd(10)) {{ set_var_cube({outcome}, 0); }}\n            else if ({b.Some} > rnd(10)) {{ set_var_cube({outcome}, 1); }}\n            else {{ set_var_cube({outcome}, 2); }}\n        }}\n"));
            texts[helper] = ($"void comportement_0()\n{{\n    if (0 == l_track_obj({helper}))\n    {{\n{rolls}    }}\n}}\n", texts[helper].Track);
            // the ball: a Sphero curled up, waiting at the lane's ball return on the divider beside its approach; the engine has it picked up,
            // thrown, rolled and spun down the lane and back by the round's phase (RACEMOD.CPP BowlLaneFrame)
            var (rackX, rackZ) = (cx + 1700, CitadelProps.FoulZ + 1500);
            var colours = CitadelProps.SpheroColours;
            var ball = Prop(colours[2 * (lane - 1)].Generic, rackX, onFloor + 340, rackZ);
            Text(ball, Still(), "label(0);\nstop();\n");
            var ball2 = Prop(colours[2 * (lane - 1) + 1].Generic, rackX, onFloor + 340, rackZ + 2 * 340 + 40);
            Text(ball2, Still(), "label(0);\nstop();\n");
            // the bowlers
            var home = SceneOps.AddTrackPoint(model, new SceneTrackPoint(cx, onFloor, CitadelProps.FoulZ + 1200));
            var wait = SceneOps.AddTrackPoint(model, new SceneTrackPoint(cx - 1900, onFloor, CitadelProps.FoulZ + 2200));
            var atLever = SceneOps.AddTrackPoint(model, new SceneTrackPoint(cx + 1250, onFloor, CitadelProps.FoulZ + 900));
            var turn = 0;
            var pinActors = new List<int>();
            var helmets = new List<int>();
            var bowlerActors = new List<int>();
            foreach (var b in bowlers.Where(b => b.Lane == lane - 1))
            {
                var first = turn == 0;
                var actor = Mk(b.Entity, b.Body, first ? cx : cx - 1900, first ? CitadelProps.FoulZ + 1200 : CitadelProps.FoulZ + 2200, 2048, StandAnim, b.Name);
                string React(int label, int anim) => $"label({label});\nanim({anim});\nwait_anim();\nanim({StandAnim});\nstop();\n\n";
                var track = "label(0);\nstop();\n\n" + $"label(1);\nanim({b.Throw});\nwait_anim();\nanim({StandAnim});\nspeed(1600);\ngoto_point_3d({home});\nbeta(2048);\nstop();\n\n" +
                            React(20, b.Cheer) + React(21, b.Shrug) + React(22, b.Dismay) +
                            $"label(3);\nspeed(1600);\nanim({StandAnim});\ngoto_point_3d({atLever});\nanim({PullAnim});\nwait_nb_second(1);\nwait_anim();\nanim({StandAnim});\ngoto_point_3d({home});\nbeta(2048);\nstop();\n\n" +
                            $"label(5);\nspeed(1600);\ngoto_point_3d({wait});\nbeta(2048);\nstop();\n\nlabel(6);\nspeed(1600);\ngoto_point_3d({home});\nbeta(2048);\nstop();\n\nlabel(7);\nstop();\n";
                var react = ByOutcome(outcome, 20, 21, 22);
                var steps = first
                    ? new[] { (1, Go(1)), (2, react), (3, Go(3)), (0, Go(5)), (4, Go(7)), (0, Go(6) + $" set_var_cube({who}, 0);") }
                    : new[] { (4, Go(7)), (0, Go(6) + $" set_var_cube({who}, 1);"), (1, Go(1)), (2, react), (3, Go(3)), (0, Go(5)) };
                Text(actor, Chain(helper, steps), track);
                map.Add($"bowler {bowlerNumber++} {lane - 1} {actor} {b.Throw} {b.Cheer} {b.Shrug} {b.Dismay} {b.Name.Replace(' ', '_')}");
                bowlerActors.Add(actor);
                turn++;
            }
            // the pins: six Franco guards in a triangle at the far end, a helmet on each (an actor turned with its pin); the engine topples the
            // ones the ball and the falling pins reach, draws each one's chain up into the dark, and hauls them up and sets them down again
            foreach (var (row, offsets) in new[] { (0, new[] { -700, 0, 700 }), (1, new[] { -350, 350 }), (2, new[] { 0 }) })
                foreach (var off in offsets)
                {
                    var (px, pz) = (cx + off, CitadelProps.PinsZ + row * 650);
                    var pin = Mk(Franco, 0, px, pz, 0, StandAnim, "a pin");
                    pinActors.Add(pin);
                    Text(pin, Still(), "label(0);\nstop();\n");
                    var helmet = Prop(CitadelProps.PinHelmet, px, onFloor, pz);
                    Text(helmet, Still(), "label(0);\nstop();\n");
                    helmets.Add(helmet);
                }
            map.Add($"lane {lane - 1} {cx} {CitadelProps.FoulZ} {CitadelProps.PinsZ} {ball} {string.Join(' ', pinActors)}");
            var speeds = bowlers.Where(b => b.Lane == lane - 1).Select(b => b.Speed).ToList();
            map.Add($"anim {lane - 1} {helper} {who} {outcome} {speeds[0]} {speeds[1]} {bowlerActors[0]} {bowlerActors[1]} {link} {FloorY + ChainAnchor} {rackX} {rackZ} {string.Join(' ', helmets)} {ball2}");
        }
        // ---- the dressing (CitadelProps.BowlingDecor): the neon pin on the back wall over the lanes, the neon BAR hung over the bar, dim lamps
        // over the lanes, a scoreboard over each played lane's pins, the ball returns the Spheros wait in, kegs and crates
        void Decor(int generic, int x, int y, int z, int beta = 0) { var d = Prop(generic, x, y, z, beta); Text(d, Still(), "label(0);\nstop();\n"); }
        Decor(CitadelProps.NeonPin, CitadelProps.LaneMiddleX, FloorY + 1700, (BowlingRoom.RoomZ0 * 512) + 40);
        // ---- the bar (since 2026-11-16 props -- CitadelProps.BowlingBar -- its U's gap against the north wall, the user: "the U shape of the
        // bar in the bowling alley needs turning around so the gap is against the wall"): its front run facing the room, its arms back to the
        // wall, the bottles' shelf on the wall in the U, the barman between, stools along the front; its neon on the wall over it
        const int barWall = BowlingRoom.RoomZ0 * 512, barX0 = 45 * 512, barX1 = 53 * 512, barFront = 15 * 512;
        const int barMiddle = (barX0 + barX1) / 2;
        Decor(CitadelProps.BarFront, barMiddle, FloorY, barFront - CitadelProps.BarDepth / 2);
        Decor(CitadelProps.BarWest, barX0 + CitadelProps.BarDepth / 2, FloorY, barWall + CitadelProps.BarArmLength / 2);
        Decor(CitadelProps.BarEast, barX1 - CitadelProps.BarDepth / 2, FloorY, barWall + CitadelProps.BarArmLength / 2);
        Decor(CitadelProps.BarShelf, barMiddle, FloorY, barWall + 10);
        foreach (var sx in new[] { barMiddle - 1500, barMiddle - 500, barMiddle + 500, barMiddle + 1500 }) Decor(CitadelProps.ClubBarStool, sx, FloorY, barFront + 380);
        Decor(CitadelProps.NeonBar, barMiddle, FloorY + 2100, barWall + 60);
        Decor(CitadelProps.HangingLamp, barMiddle, FloorY + 2300, barFront - 1200);
        // ---- the pool table (the user, 2026-11-16: "perhaps a pool table for a couple of NPCs to be playing on to help fill up the space"):
        // between the bar and the booths, along x; the sailor bent over it at its west end, cue in hand (three poses of him, his track
        // changing them: drawn back, at the ball, through), a Franco dissident waiting his turn by its south side, a cue against its east end
        const int poolX = 48 * 512 + 256, poolZ = 21 * 512;
        Decor(CitadelProps.PoolTable, poolX, FloorY, poolZ);
        Decor(CitadelProps.HangingLamp, poolX, FloorY + 2200, poolZ);
        Decor(CitadelProps.PoolCue, poolX + CitadelProps.PoolLength / 2 + 90, FloorY, poolZ - 300);
        var (shotX, shotZ) = (poolX - CitadelProps.PoolLength / 2 - CitadelProps.PoolStand, poolZ);
        Decor(CitadelProps.HangingLamp, PokerX, FloorY + 2300, PokerZ);
        foreach (var lx in new[] { CitadelProps.LaneX0, CitadelProps.LaneMiddleX, CitadelProps.LaneX1 })
            foreach (var lz in new[] { 9216, 14336 }) Decor(CitadelProps.HangingLamp, lx, FloorY + 2300, lz);
        foreach (var lx in new[] { CitadelProps.LaneX0, CitadelProps.LaneX1 })
        {
            Decor(CitadelProps.Scoreboard, lx, FloorY + 2500, 4700);
            Decor(CitadelProps.BallReturn, lx + 1700, FloorY, CitadelProps.FoulZ + 1500);
        }
        // (no kegs: they read as two posts -- the user, 2026-11-15: "I have no idea what these two posts are")
        foreach (var (cx2, cz2) in new[] { (5500, 23300), (5500, 23900), (6080, 23600) }) Decor(CitadelProps.Crate, cx2, FloorY, cz2);
        // ---- the regulars, the underbelly at the bar (Otringal's own, as Rick's bar has them): a casino gloom, a sailor and a Franco dissident
        // at its stools, a brown-faced sailor behind it, the big grey gloom minding the lift, another dissident at the slot machine
        // (2026-11-15, second round: the bar against the north wall -- the gloom and the sailor at its west side, the dissident at its east,
        // the barman in its U; another dissident by the booths; the big grey Franco by the lift)
        void Regular(int entity, int body, int x, int z, int beta, int anim = 0)
        {
            var r = SceneOps.BlankActor(SceneGame.Lba2, x, FloorY, z, entity);
            r.Body = (sbyte)body; r.Anim = anim; r.Beta = beta; r.Flags = 0x1000; r.CoulObj = 9; r.Armor = 51; r.LifePoints = -1; r.Move = 0;
            r.Life = new byte[] { 0 }; r.Track = new byte[] { 0 };
            Text(SceneOps.AddActor(model, r), Still(), "label(0);\nstop();\n");
        }
        foreach (var (entity, body, x, z, beta) in new[] { (294, 0, barMiddle - 1000, barFront + 520, 2048), (295, 0, barMiddle + 1000, barFront + 520, 2048),
                                                         (56, 93, barX1 + 420, barWall + 1900, 3072), (297, 187, barMiddle, barWall + 1500, 0),
                                                         (56, 93, poolX + 500, poolZ + CitadelProps.PoolWidth / 2 + 380, 2048), (56, 93, 25856, 22272, 2048) })
            Regular(entity, body, x, z, beta);
        Regular(297, 0, shotX, shotZ, 1024, BowlingAnims.Pool);
        // ---- more tables (the user, 2026-11-16: "We should add more tables"): a second big round table east of the booths, a sailor and a
        // dissident at a hand of cards there, two chairs free; and one of Rick's card tables between the lanes' end and the booths, empty
        // (Rick's is too small for the sailors to sit at: the poker table's size, the user's same day)
        Decor(CitadelProps.ClubTable, 39 * 512, FloorY, 22 * 512);
        const int cardsX = 52 * 512, cardsZ = 35 * 512 + 256;
        Decor(CitadelProps.PokerTable, cardsX, FloorY, cardsZ);
        Decor(CitadelProps.HangingLamp, cardsX, FloorY + 2300, cardsZ);
        foreach (var (entity, body, dx, dz, beta) in new[] { (297, 0, -1, 0, 1024), (56, 93, 0, -1, 0), (-1, 0, 1, 0, 3072), (-1, 0, 0, 1, 2048) })
        {
            var (sx, sz) = (cardsX + dx * PokerSeat, cardsZ + dz * PokerSeat);
            Decor(CitadelProps.Chair, sx + dx * CitadelProps.ChairAhead, FloorY, sz + dz * CitadelProps.ChairAhead, beta);
            if (entity >= 0) Regular(entity, body, sx, sz, beta, BowlingAnims.Poker);
        }
        // ---- the poker table (the user, 2026-11-15: "add a round table with people sitting playing poker"): round, among the booths and the
        // fruit machines, a sailor, a blue sailor and a Franco dissident sat at it (BowlingAnims.Poker, each at his own pace), the fourth chair
        // empty, toward the room. Each sits PokerSeat from its middle facing it, his chair's seat under him (a chair's seat ChairAhead in front
        // of its own place)
        Decor(CitadelProps.PokerTable, PokerX, FloorY, PokerZ);
        foreach (var (entity, body, dx, dz, beta) in new[] { (56, 93, 0, -1, 0), (297, 0, -1, 0, 1024), (295, 0, 1, 0, 3072), (-1, 0, 0, 1, 2048) })
        {
            var (sx, sz) = (PokerX + dx * PokerSeat, PokerZ + dz * PokerSeat);
            Decor(CitadelProps.Chair, sx + dx * CitadelProps.ChairAhead, FloorY, sz + dz * CitadelProps.ChairAhead, beta);
            if (entity >= 0) Regular(entity, body, sx, sz, beta, BowlingAnims.Poker);
        }
        // the fruit machines (BowlingRoom: Rick's two, where they stand), for the engine
        foreach (var (fx, fz) in BowlingRoom.FruitMachines) map.Add($"fruit {fx} {fz}");
        File.WriteAllLines(Path.Combine(gameDirectory, "BOWLMAP.TXT"), map);

        // ---- the scripts
        var built = LbaScript.SceneScripts.Load(SceneSerializer.Write(model), Scene);
        built.SetText(0, ScriptKind.Life, heroLife);
        foreach (var (actor, (life, track)) in texts) { built.SetText(actor, ScriptKind.Life, life); built.SetText(actor, ScriptKind.Track, track); }
        var result = built.Build();
        if (!result.Ok) { log.AddRange(result.Errors.Select(e => $"The bowling alley left out: {e}")); return log; }
        var entry = Scene + 1;
        while (scenes.Count <= entry) scenes.Slots.Add(new HqrFile.Slot());
        scenes.SetStored(entry, result.Record!);
        if (result.Record!.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
        { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, result.Record.Length); scenes.SetStored(0, size); }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        log.Add($"The bowling alley: scene {Scene} (grid {gridNumber}, library {grid[0]}, Rick's bricks painted dark): {model.Actors.Count} actors, two lanes of six Franco pins on chains, a Sphero for each ball, a giant lever each; its lift's foot at cells ({FootX}-{FootX + 1}, {FootZ}-{FootZ + 1})");
        return log;
    }

    private static SceneActorModel Furniture(int generic, int x, int y, int z)
    {
        var a = CitadelProps.Actor(generic, x, y, z);
        a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 };
        return a;
    }

    // The way in, outside: the pad in the platform's middle (a hatch of the ground -- no shaft to cut in the island's ground, so no plate there), Action
    // on it and a door at ground level switches on under his feet and takes him down; the ride is in the alley's own lift, which brings him
    // out on its plate. The helper's life waits for the Action; the door is off till then (LiftSwitch).
    public static Action<SceneScripts> OutsideLift(SceneModel model)
    {
        var helper = SceneOps.AddActor(model, RaceTrackScenes.InvisibleHelper(PadX, PadY, PadZ));
        const int DontReadjust = 1, Big = 512, Tall = 700;   // (the ground slopes: tall zones; arriving he falls onto the plate from a little over it)
        var clubArrival = (X: (FootX + 1) * 512, Y: FloorY + CitadelProps.LiftTop + ClubDown, Z: (FootZ + 1) * 512);
        model.Zones.Add(new SceneZoneModel
        {
            Type = 0, Num = Scene, X0 = PadX - Big, X1 = PadX + Big - 1, Z0 = PadZ - Big, Z1 = PadZ + Big - 1, Y0 = PadY - Tall, Y1 = PadY + Tall,
            Info = new[] { clubArrival.X - Big, clubArrival.Y - Tall + 100, clubArrival.Z - Big, 0, LiftSwitch, 0, DontReadjust, 0 },
        });
        // (Action on it, standing on the square -- the door's zone is wider, so where he stands in it is where he comes out on the alley's
        // plate. Walked onto, it took him down whenever he crossed the square: the user, 2026-11-15, "it needs to only be triggered when
        // Twinsen uses action on it")
        var life = $@"void comportement_0()
{{
    set_change_cube({LiftSwitch}, 0);
    set_comportement(comportement_4);
}}

void comportement_4()
{{
    if ({PadReach} > distance(0) && 1 == action())
    {{
        set_dir_obj(0, NO_MOVE);
        set_change_cube({LiftSwitch}, 1);
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
}}
";
        return scripts =>
        {
            scripts.SetText(helper, ScriptKind.Life, life);
            scripts.SetText(helper, ScriptKind.Track, "label(0);\nstop();\n");
        };
    }
}
