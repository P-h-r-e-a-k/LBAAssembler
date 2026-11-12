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
    // the floor (the prison's: layer 3, its top 1,024) and room (cells)
    private const int FloorLayer = 3, Tile = 8, Wall = 57, Size = 64, Height = 25;
    private const int RoomX0 = 8, RoomX1 = 46, RoomZ0 = 4, RoomZ1 = 34;
    public const int FloorY = (FloorLayer + 1) * 256;
    // the lift: its pad outside (scene 90: the platform's middle, ground 3,900) and its foot inside (cells)
    public const int PadX = 22000, PadY = 3900, PadZ = 19800;
    private const int FootX = 12, FootZ = 28;
    private const int LiftZone = 70, ShaftZone = 77, ArrivalZone = 76, LiftSwitch = 7;
    private const int LiftRide = 1, LiftDown = 2, LiftRise = 3, LiftRisen = 4, LiftSpeed = 600, LiftStart = 506, LiftStop = 510;
    private const int CellDown = 1600, CellUp = 1200, ClubDown = 1200, ClubUp = 1800;
    // the animations: the Franco's standing and its fall (entity 297), the Sphero's rolling, the bowlers' throw and swing
    private const int Franco = 56, Sphero = 28, MonkeyMonster = 275, SwordMonkey = 252, EmperorSoldier = 184, DarkMonk = 217;
    private const int StandAnim = 0, FallAnim = 10, RollAnim = 1, WalkAnim = 1, ThrowMonster = 15, SwingSword = 52, PullAnim = 6;

    // (the game of bowling the engine plays -- RACEMOD.CPP bowling_game=: it holds this scene variable, and the lane scripts wait)
    public const int GameVar = 99;

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
        var table = bkg.Read(header.Brk + header.MaxBrk);
        var type = table[Prison * 2];
        var raw = bkg.Read(header.Gri + table[Prison * 2 + 1]);
        var shape = Lba2GridBackend.ToLba1Shape(raw);

        // ---- the grid: the prison's library, a flagstone floor and a wall of its tall stone all round
        var set = new Dictionary<(int, int, int), (int Block, int Pos)>();
        int Mod(int a, int m) => ((a % m) + m) % m;
        bool Floor(int x, int z) => x >= RoomX0 && x <= RoomX1 && z >= RoomZ0 && z <= RoomZ1;
        for (var z = RoomZ0 - 1; z <= RoomZ1 + 1; z++)
            for (var x = RoomX0 - 1; x <= RoomX1 + 1; x++)
                set[(x, FloorLayer, z)] = Floor(x, z) ? (Tile, Mod(x, 3) + 3 * Mod(z, 3)) : (Wall, 0);
        var edits = new List<Lba1GridCell>();
        for (var z = 0; z < Size; z++)
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Size; x++)
        {
            var (block, pos) = set.TryGetValue((x, y, z), out var c) ? c : (0, 0);
            edits.Add(new Lba1GridCell(x, y, z, block, pos));
        }
        var grid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, edits));
        grid[0] = raw[0];
        var gridNumber = header.Grm - header.Gri;
        if (gridNumber > 255) { log.Add("The bowling alley left out: LBA_BKG.HQR has 256 grids already"); return log; }
        bkg.InsertAt(header.Grm, grid);
        header.Grm += 1; header.Bll += 1; header.Brk += 1;
        if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
        var library = bkg.Read(header.Bll + raw[0]);
        var (bytes, usedBricks) = MapBricks.UsedBrickBytes(grid, library, b => b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? BinaryPrimitives.ReadUInt32LittleEndian(bkg.ExtentOf(header.Brk + b)) : 0);
        if (header.MaxBrickCube < (uint)((usedBricks + 1) * 4 + bytes)) header.MaxBrickCube = (uint)((usedBricks + 1) * 4 + bytes);
        if (header.MaxMaskCube < (uint)((usedBricks + 1) * 4 + bytes + usedBricks * 64)) header.MaxMaskCube = (uint)((usedBricks + 1) * 4 + bytes + usedBricks * 64);
        bkg.SetStored(0, header.Bytes);
        table = bkg.Read(header.Brk + header.MaxBrk);
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
        var clubTop = FloorY + ClubDown + 300;
        model.Zones.Add(new SceneZoneModel
        {
            Type = 0, Num = Outside, X0 = clubLift.Item1 - 512, X1 = clubLift.Item1 + 511, Z0 = clubLift.Item3 - 512, Z1 = clubLift.Item3 + 511, Y0 = clubTop, Y1 = clubTop + 3000,
            Info = new[] { padFoot.X - 512, padFoot.Y + CitadelProps.LiftTop - CellUp + 4, padFoot.Z - 512, 0, 0, 0, DontReadjust, 1 },
        });
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = LiftZone, X0 = clubLift.Item1 - 512, X1 = clubLift.Item1 + 511, Z0 = clubLift.Item3 - 512, Z1 = clubLift.Item3 + 511, Y0 = FloorY - 100, Y1 = FloorY + 800, Info = new int[8] });
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = ArrivalZone, X0 = clubLift.Item1 - 512, X1 = clubLift.Item1 + 511, Z0 = clubLift.Item3 - 512, Z1 = clubLift.Item3 + 511, Y0 = FloorY + ClubDown - 200, Y1 = FloorY + ClubDown + 400, Info = new int[8] });
        // the lift's parts (carriers, as Stella Entities'): the plate and its rails, on a track up into the dark
        var clubLow = SceneOps.AddTrackPoint(model, new SceneTrackPoint(clubLift.Item1, FloorY, clubLift.Item3));
        var clubHigh = SceneOps.AddTrackPoint(model, new SceneTrackPoint(clubLift.Item1, FloorY + ClubUp, clubLift.Item3));
        var parts = new[] { CitadelProps.MoonLiftPlatform, CitadelProps.LiftRailBack, CitadelProps.LiftRailLeft, CitadelProps.LiftRailRight }.Select(g =>
        {
            var a = CitadelProps.Actor(g, clubLift.Item1, FloorY + ClubDown, clubLift.Item3);
            a.Flags |= RaceTrackScenes.CarrierFlag; a.Move = 3; a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 };
            return SceneOps.AddActor(model, a);
        }).ToArray();
        var clubTrack = $"label(0);\nspeed({LiftSpeed});\ngoto_point_3d({clubLow});\nsample({LiftStop});\n\nlabel({LiftDown});\nstop();\n\n" +
                        $"label({LiftRide});\nsample({LiftStart});\nspeed({LiftSpeed});\ngoto_point_3d({clubHigh});\nstop();\n";
        foreach (var i in parts) Text(i, Still(), clubTrack);
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
        var lanes = SceneOps.AddActor(model, Furniture(CitadelProps.BowlingLanes, CitadelProps.LaneMiddleX, FloorY, CitadelProps.LaneMiddleZ));
        Text(lanes, Still(), "label(0);\nstop();\n");
        int Mk(int entity, int bodyGeneric, int x, int z, int beta, int anim, string name)
        {
            var a = SceneOps.BlankActor(SceneGame.Lba2, x, FloorY + CitadelProps.LaneTop, z, entity);
            a.Body = bodyGeneric; a.Anim = anim; a.Beta = beta; a.Flags = 0x1000 | RaceTrackScenes.CarrierFlag; a.CoulObj = 9; a.Armor = 51; a.LifePoints = -1; a.Move = 3;
            a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 };
            return SceneOps.AddActor(model, a);
        }
        int Prop(int generic, int x, int y, int z, int beta = 0)
        {
            var a = CitadelProps.Actor(generic, x, y, z, beta);
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
        var bowlers = new (int Lane, int X, int Entity, string Name, int Throw, int Cheer, int Shrug, int Dismay, int Strike, int Some, int Speed)[]
        {
            (0, CitadelProps.LaneX0, MonkeyMonster, "the monkey monster", ThrowMonster, 133, 222, 6, 5, 8, 3200),      // strong: strikes, and wild
            (0, CitadelProps.LaneX0, EmperorSoldier, "the Emperor's soldier", 436, 434, 433, 6, 3, 8, 2500),         // steady: more often some pins
            (1, CitadelProps.LaneX1, SwordMonkey, "the swordsman monkey", SwingSword, 532, 129, 6, 6, 9, 2800),      // precise
            (1, CitadelProps.LaneX1, DarkMonk, "the Dark Monk", 488, 485, 489, 492, 3, 5, 1900),                    // slow and erratic
        };
        foreach (var cx in new[] { CitadelProps.LaneX0, CitadelProps.LaneX1 })
        {
            var onFloor = FloorY + CitadelProps.LaneTop;
            var who = 10 + lane; var outcome = 12 + lane;
            lane++;
            // the giant lever beside the lane keeps the round (an invisible helper's track did not run): a phase a label of its track, its arm
            // thrown down in the third as the bowler pulls it
            var helper = Prop(CitadelProps.LeverUp, cx + 1750, FloorY, CitadelProps.FoulZ + 500);
            Text(helper, "", $"label(0);\nwait_nb_second(2);\nlabel(1);\nwait_nb_second(4);\nlabel(2);\nwait_nb_second(3);\nlabel(3);\nwait_nb_second(2);\nbody({CitadelProps.LeverDown});\nwait_nb_second(1);\nbody({CitadelProps.LeverUp});\nwait_nb_second(1);\nlabel(4);\nwait_nb_second(3);\ngoto(label_0);\n");
            // (and each round the throw's luck is rolled while the bowlers change places, in tenths by whose turn it is)
            var rolls = string.Concat(bowlers.Where(b => b.Lane == lane - 1).Select((b, i) =>
                $"        if ({i} == var_cube({who}))\n        {{\n            if ({b.Strike} > rnd(10)) {{ set_var_cube({outcome}, 0); }}\n            else if ({b.Some} > rnd(10)) {{ set_var_cube({outcome}, 1); }}\n            else {{ set_var_cube({outcome}, 2); }}\n        }}\n"));
            texts[helper] = ($"void comportement_0()\n{{\n    if (0 == l_track_obj({helper}))\n    {{\n{rolls}    }}\n}}\n", texts[helper].Track);
            // the ball: a live Sphero at the lane's foul line; it rolls to the pins, to one side of them, or into the gutter, and is back when the pins are
            var ballStart = SceneOps.AddTrackPoint(model, new SceneTrackPoint(cx, onFloor, CitadelProps.FoulZ + 400));
            var ballEnd = SceneOps.AddTrackPoint(model, new SceneTrackPoint(cx, onFloor, CitadelProps.PinsZ - 500));
            var ballSide = SceneOps.AddTrackPoint(model, new SceneTrackPoint(cx + 520, onFloor, CitadelProps.PinsZ - 500));
            var ballGutter = SceneOps.AddTrackPoint(model, new SceneTrackPoint(cx + CitadelProps.LaneHalf + 130, onFloor, CitadelProps.PinsZ - 500));
            var ball = Mk(Sphero, 0, cx, CitadelProps.FoulZ + 400, 2048, StandAnim, "a Sphero");
            model.Actors[ball].Flags = 0x1000 | RaceTrackScenes.CarrierFlag;
            // (the speed of the ball is the bowler's: set by whoever bowls -- var_cube `who`: first or second of the lane)
            var firstSpeed = bowlers.First(b => b.Lane == lane - 1).Speed; var secondSpeed = bowlers.Last(b => b.Lane == lane - 1).Speed;
            // (labels 1, 9, 8 for the first bowler's speed, 11, 19, 18 for the second's: a track has no ifs)
            string Roll(int label, int to, int speed) =>
                $"label({label});\nanim({RollAnim});\nspeed({speed});\ngoto_point_3d({to});\nanim({StandAnim});\nstop();\n\n";
            var rollLife = $"if (0 == var_cube({who})) {{ {ByOutcome(outcome, 1, 9, 8)} }} else {{ {ByOutcome(outcome, 11, 19, 18)} }}";
            Text(ball, Chain(helper, (1, rollLife), (4, Go(4)), (0, "")),
                "label(0);\nstop();\n\n" + Roll(1, ballEnd, firstSpeed) + Roll(9, ballSide, firstSpeed) + Roll(8, ballGutter, firstSpeed) +
                Roll(11, ballEnd, secondSpeed) + Roll(19, ballSide, secondSpeed) + Roll(18, ballGutter, secondSpeed) + $"label(4);\npos_point({ballStart});\nanim({StandAnim});\nstop();\n");
            // the bowlers
            var home = SceneOps.AddTrackPoint(model, new SceneTrackPoint(cx, onFloor, CitadelProps.FoulZ + 1200));
            var wait = SceneOps.AddTrackPoint(model, new SceneTrackPoint(cx - 1900, onFloor, CitadelProps.FoulZ + 2200));
            var atLever = SceneOps.AddTrackPoint(model, new SceneTrackPoint(cx + 1250, onFloor, CitadelProps.FoulZ + 900));
            var turn = 0;
            var pinActors = new List<int>();
            foreach (var b in bowlers.Where(b => b.Lane == lane - 1))
            {
                var first = turn == 0;
                var actor = Mk(b.Entity, 0, first ? cx : cx - 1900, first ? CitadelProps.FoulZ + 1200 : CitadelProps.FoulZ + 2200, 2048, StandAnim, b.Name);
                string React(int label, int anim) => $"label({label});\nanim({anim});\nwait_anim();\nanim({StandAnim});\nstop();\n\n";
                var track = "label(0);\nstop();\n\n" + $"label(1);\nanim({b.Throw});\nwait_anim();\nanim({StandAnim});\nspeed(900);\ngoto_point_3d({home});\nbeta(2048);\nstop();\n\n" +
                            React(20, b.Cheer) + React(21, b.Shrug) + React(22, b.Dismay) +
                            $"label(3);\nspeed(900);\nanim({StandAnim});\ngoto_point_3d({atLever});\nanim({PullAnim});\nwait_nb_second(1);\nwait_anim();\nanim({StandAnim});\ngoto_point_3d({home});\nbeta(2048);\nstop();\n\n" +
                            $"label(5);\nspeed(900);\ngoto_point_3d({wait});\nbeta(2048);\nstop();\n\nlabel(6);\nspeed(900);\ngoto_point_3d({home});\nbeta(2048);\nstop();\n\nlabel(7);\nstop();\n";
                var react = ByOutcome(outcome, 20, 21, 22);
                var steps = first
                    ? new[] { (1, Go(1)), (2, react), (3, Go(3)), (0, Go(5)), (4, Go(7)), (0, Go(6) + $" set_var_cube({who}, 0);") }
                    : new[] { (4, Go(7)), (0, Go(6) + $" set_var_cube({who}, 1);"), (1, Go(1)), (2, react), (3, Go(3)), (0, Go(5)) };
                Text(actor, Chain(helper, steps), track);
                map.Add($"bowler {bowlerNumber++} {lane - 1} {actor} {b.Throw} {b.Cheer} {b.Shrug} {b.Dismay} {b.Name.Replace(' ', '_')}");
                turn++;
            }
            // the pins: six Franco guards, a helmet and chain over each, in a triangle at the far end; the ones the ball reaches go down in turn,
            // and the chains haul them all up
            var k = 0;
            foreach (var (row, offsets) in new[] { (0, new[] { -700, 0, 700 }), (1, new[] { -350, 350 }), (2, new[] { 0 }) })
                foreach (var off in offsets)
                {
                    var (px, pz) = (cx + off, CitadelProps.PinsZ + row * 650);
                    var stand = SceneOps.AddTrackPoint(model, new SceneTrackPoint(px, onFloor, pz));
                    var up = SceneOps.AddTrackPoint(model, new SceneTrackPoint(px, onFloor + 900, pz));
                    var pin = Mk(Franco, 0, px, pz, 0, StandAnim, "a pin");
                    pinActors.Add(pin);
                    var chain = Prop(CitadelProps.PinChain, px, onFloor, pz);
                    // (a ball to the side of the pins takes the ones from the middle to that side)
                    var falls = off >= 0;
                    Text(pin, Chain(helper, (2, falls ? $"if (2 != var_cube({outcome})) {{ set_track(label_2); }}" : $"if (0 == var_cube({outcome})) {{ set_track(label_2); }}"), (4, Go(4)), (0, "")),
                        $"label(0);\nstop();\n\nlabel(2);\nwait_nb_dizieme({1 + 2 * k});\nanim({FallAnim});\nwait_anim();\nstop();\n\n" +
                        $"label(4);\npos_point({stand});\nanim({StandAnim});\nspeed(600);\ngoto_point_3d({up});\ngoto_point_3d({stand});\nstop();\n");
                    Text(chain, Chain(helper, (4, Go(4)), (0, "")), $"label(0);\nstop();\n\nlabel(4);\nspeed(600);\ngoto_point_3d({up});\ngoto_point_3d({stand});\nstop();\n");
                    k++;
                }
            map.Add($"lane {lane - 1} {cx} {CitadelProps.FoulZ} {CitadelProps.PinsZ} {ball} {string.Join(' ', pinActors)}");
        }
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
        log.Add($"The bowling alley: scene {Scene} (grid {gridNumber}, the prison's library {raw[0]}): {model.Actors.Count} actors, two lanes of six Franco pins on chains, a Sphero for each ball, a giant lever each; its lift's foot at cells ({FootX}-{FootX + 1}, {FootZ}-{FootZ + 1})");
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
        const int DontReadjust = 1, ArriveOver = 40, Under = 60;
        var clubArrival = (X: (FootX + 1) * 512, Y: FloorY + CitadelProps.LiftTop + ClubDown, Z: (FootZ + 1) * 512);
        model.Zones.Add(new SceneZoneModel
        {
            Type = 0, Num = Scene, X0 = PadX - 512, X1 = PadX + 511, Z0 = PadZ - 512, Z1 = PadZ + 511, Y0 = PadY - Under, Y1 = PadY + 700,
            Info = new[] { clubArrival.X - 512, clubArrival.Y - Under + ArriveOver, clubArrival.Z - 512, 0, LiftSwitch, 0, DontReadjust, 0 },
        });
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = LiftZone, X0 = PadX - 512, X1 = PadX + 511, Z0 = PadZ - 512, Z1 = PadZ + 511, Y0 = PadY - 100, Y1 = PadY + 800, Info = new int[8] });
        var life = $@"void comportement_0()
{{
    swif (1 == action())
    {{
        if ({LiftZone} == zone_obj(0))
        {{
            set_dir_obj(0, NO_MOVE);
            set_change_cube({LiftSwitch}, 1);
            set_comportement(comportement_1);
        }}
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
