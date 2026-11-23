using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.LbaScript;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Imperial Hotel's upper floor (Otringal; the user, 2026-11-22: "let's add a second floor with some bedrooms for Twinsen to spend the
// night in. So we can avoid putting stairs in, let's model a lift in the wall the sofa is in front of ... The new floor can largely be a
// couple of corridors with doors off from them. Let's have Twinsen be able to choose the quality of the room he wishes to stay in, and have 3
// different rooms Twinsen can visit ... We can have other numbered hotel rooms too that Twinsen never gets access to"; the prices 100, 250 and
// 500 zlitos). Built at the build's start (before the story, whose scripts walk Twinsen through it: RaceTrackStory.OtringalNight), from the
// hotel's own rooms off its lobby -- the game's scenes 79 and 170 (bedrooms: the four posts of a bed, a wardrobe, armchairs) and 169 (a
// bathroom: the clawfoot bath, its shower ring), all in the hotel's brick library (8):
//   - the corridor (Corridor): room 79's walls and floor carried on to 34 cells long and 5 wide, empty; along its back wall the doors of
//     rooms 101-105 (HotelProps.CorridorDoors) and at its east end the Imperial Suite's double doors; at its west end the lift;
//   - the Classic Room (Classic, 100 zlitos, door 102): room 79 as it is -- its bed, wardrobe, tables -- but its armchairs, which give way to
//     a little bathroom behind a panelled partition in the front corner: a shower, a basin, a toilet;
//   - the Junior Suite (Junior, 250, door 104): room 170, a sofa and coffee table under a chandelier, and through a doorway room 169's
//     bathroom, its clawfoot bath;
//   - the Imperial Suite (Suite, 500, the double doors): room 79 as its salon, its bed gone -- a marble fireplace, sofas facing over a coffee
//     table, a grand piano, a table for eight, chandeliers -- through a doorway room 170 as the bedroom, its bed gone for the four-poster on
//     its dais, and through another room 169's bathroom;
//   - the lobby (the game's scene 81): the sofa against the west wall of its north corridor (block 84, cells 27-28, 5-8) taken out of the
//     grid -- the lift in that wall, a damask sofa at the alcove's edge -- and the reception desk (RaceTrackStory.OtringalNight puts them and
//     the clerk in).
// The rooms' joins between them are front (cut) walls, as the game's rooms' own front walls: a full wall along x would hide the room behind it
// from the camera.
internal static class OtringalHotel
{
    public const int Corridor = 381, Classic = 382, Junior = 383, Suite = 384, LiftCar = 385, Lobby = 81, Island = 4;
    private const int RoomA = 79, RoomB = 170, Bath = 169;
    private const int Size = 64, Height = 25, Library = 8;
    public const int FloorY = 256, LobbyFloorY = 3072;
    // the corridor: its interior x 1-34, z 2-6, the front wall at z 7, the east wall at x 35
    private const int CorridorEnd = 35, CorridorFront = 7;
    // the corridor's doors: room 101's middle at cell 3, the next DoorPitch on (HotelProps); Twinsen's: 102 and 104; the suite's doors' middle
    public static int DoorX(int room) => (int)(1536 + (room - HotelProps.FirstRoom) * HotelProps.DoorPitch);
    public const int ClassicDoor = 102, JuniorDoor = 104, SuiteDoorX = 16640, BackWallFace = 1024;
    // the lifts: on the corridor's west wall's face (x 512) and the lobby's alcove's (x 13824), facing +x; where Twinsen stands before each
    public static readonly (int X, int Z) CorridorLift = (514, 2304), LobbyLift = (13826, 3328);
    public const int LiftFrontOut = 700;
    // (the lift's doorway, where Twinsen walks into the lift and out of it: this far out from its wall -- clear of its box, 130 deep)
    public const int LiftDoorOut = 180;
    // the lift car (2026-11-23, the user: "Let's add an actual scene for the lift that Twinsen can walk inside, have the door close, we'll play
    // some music for 5 seconds, then bing and the doors open again at the different floor"): room A's walls round a floor 4 cells square,
    // x 1-4, z 2-5 -- its doors on its west wall's face (x 512) facing +x as the floors' lifts are, its back wall's panelling at z 1024;
    // where Twinsen stands in it (facing the doors), where he walks out to; its cube-change switches: 1 the first floor, 2 the lobby
    private const int CarEnd = 5, CarFront = 6;
    public static readonly (int X, int Z) CarDoors = (514, 2048), CarStand = (1500, 2048), CarOut = (700, 2048);
    public const int CarToCorridor = 1, CarToLobby = 2, CarDoorsActor = 2;
    // (the rooms' doors, the game's rooms' own: in their front walls -- room 79's at cells 6-7, room 170's at 8-9, room 169's at 6-7 -- in
    // from the corridor there, facing in)
    public static readonly (int X, int Z) RoomArrival = (3584, 5500), JuniorArrival = (4608, 5500);
    private const int DoorA = 6, DoorB = 8, DoorBath = 6;
    // the cube-change switches (zone info 4, set_change_cube): the corridor's doors into the three rooms (off till booked), the lifts' (off
    // till the ride's end)
    public const int ClassicSwitch = 1, JuniorSwitch = 2, SuiteSwitch = 3, LiftSwitch = 4;
    // the lobby's sofa: grid block 84, cells x 27-28, z 5-8, layers 12-13
    private const int SofaBlock = 84;
    // the actors the installer puts in, for the story: each scene's lift (corridor), the rooms' beds' places (scene units: where Twinsen lies)
    public const int CorridorLiftActor = 2;
    public static readonly (int X, int Y, int Z, int Beta) ClassicBed = (1280, 700, 3072, 1024), JuniorBed = (1280, 700, 2048, 1024), SuiteBed = (8448, 900, 2700, 0);

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (new[] { RoomA, RoomB, Bath, Lobby }.Any(s => !store.SceneExists(s))) { log.Add("The Imperial Hotel's upper floor left out: its rooms (scenes 79, 170, 169) or its lobby (81) aren't there"); return log; }
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        foreach (var s in new[] { Corridor, Classic, Junior, Suite, LiftCar })
            if (s + 1 < scenes.Count && !scenes.IsEmpty(s + 1)) { log.Add($"The Imperial Hotel's upper floor left out: scene {s} is already another part of the build's"); return log; }
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        (byte[] Raw, byte[] Shape, byte[] Cells) Grid(int scene)
        {
            var raw = bkg.Read(header.Gri + table[scene * 2 + 1]);
            var shape = Lba2GridBackend.ToLba1Shape(raw);
            return (raw, shape, Lba1GridCodec.Decode(shape));
        }
        var a = Grid(RoomA); var b = Grid(RoomB); var bath = Grid(Bath); var lobby = Grid(Lobby);
        if (new[] { a, b, bath, lobby }.Any(g => g.Raw[0] != Library)) { log.Add("The Imperial Hotel's upper floor left out: its rooms aren't all in the hotel's brick library"); return log; }
        static int At(int x, int y, int z) => ((z * Size + x) * Height + y) * 2;
        static void Column(byte[] to, int tx, int tz, byte[] from, int fx, int fz, int y0 = 0, int y1 = Height - 1)
        {
            for (var y = 0; y < Height; y++)
            {
                var keep = y >= y0 && y <= y1;
                to[At(tx, y, tz)] = keep ? from[At(fx, y, fz)] : (byte)0;
                to[At(tx, y, tz) + 1] = keep ? from[At(fx, y, fz) + 1] : (byte)0;
            }
        }
        static void Clear(byte[] cells, int x0, int x1, int z0, int z1, int y0)
        {
            for (var z = z0; z <= z1; z++) for (var x = x0; x <= x1; x++) for (var y = y0; y < Height; y++) { cells[At(x, y, z)] = 0; cells[At(x, y, z) + 1] = 0; }
        }
        // (a room's door in its front wall, cells 6-7 of its front row: the plain wall's column over it -- the second and third rooms of a suite
        // open into the first, not out of the grid)
        static void ShutDoor(byte[] cells, int x0, int door, int frontZ) { for (var x = x0 + door; x <= x0 + door + 1; x++) Column(cells, x, frontZ, (byte[])cells.Clone(), x0 + 3, frontZ); }
        // (a doorway through a join's cut wall, cells z 6-7: the floor carried through)
        static void Doorway(byte[] cells, int x) { for (var z = 6; z <= 7; z++) Column(cells, x, z, (byte[])cells.Clone(), x - 1, z, 0, 0); }
        // (a room's columns x 1..last+1 (its interior and its east cut wall) copied in at offset dx; the join's own column the cut wall before it)
        static void Join(byte[] to, byte[] from, int dx, int last, int depth)
        {
            for (var x = 1; x <= last + 1; x++)
                for (var z = 0; z <= depth; z++) Column(to, dx + x, z, from, x, z);
        }
        byte[] Shape(byte[] baseShape, byte[] baseCells, byte[] cells)
        {
            var edits = new List<Lba1GridCell>();
            for (var z = 0; z < Size; z++)
                for (var x = 0; x < Size; x++)
                    for (var y = 0; y < Height; y++)
                        if (cells[At(x, y, z)] != baseCells[At(x, y, z)] || cells[At(x, y, z) + 1] != baseCells[At(x, y, z) + 1])
                            edits.Add(new Lba1GridCell(x, y, z, cells[At(x, y, z)], cells[At(x, y, z) + 1]));
            return Lba1GridEdit.SetCells(baseShape, edits);
        }

        // ---- the corridor: room A's walls carried on
        var corridor = new byte[a.Cells.Length];
        for (var x = 0; x <= CorridorEnd; x++)
        {
            var fx = x == 0 ? 0 : x == CorridorEnd ? 12 : 1 + (x - 1) % 11;
            for (var z = 0; z <= CorridorFront; z++)
            {
                if (z <= 1 || x == 0 || x == CorridorEnd) Column(corridor, x, z, a.Cells, fx, z == CorridorFront ? 12 : Math.Min(z, 11));
                else if (z < CorridorFront) Column(corridor, x, z, a.Cells, fx, 2 + (z - 2) % 10, 0, 0);
                // (the front wall: room A's front row, one plain column of it -- its door's frame (cells 5-8) stood up in the corridor as pillars)
                else Column(corridor, x, z, a.Cells, 2, 12);
            }
        }
        // ---- the lift car: room A's walls round its 4 x 4 floor, as the corridor's are round its own
        var car = new byte[a.Cells.Length];
        for (var x = 0; x <= CarEnd; x++)
        {
            var fx = x == 0 ? 0 : x == CarEnd ? 12 : 1 + (x - 1) % 11;
            for (var z = 0; z <= CarFront; z++)
            {
                if (z <= 1 || x == 0 || x == CarEnd) Column(car, x, z, a.Cells, fx, z == CarFront ? 12 : Math.Min(z, 11));
                else if (z < CarFront) Column(car, x, z, a.Cells, fx, 2 + (z - 2) % 10, 0, 0);
                else Column(car, x, z, a.Cells, 2, 12);
            }
        }
        // ---- the Classic Room: room A, its armchairs (cells 10-11, 8-9) gone for the bathroom
        var classic = (byte[])a.Cells.Clone();
        Clear(classic, 10, 11, 8, 9, 1);
        // (2026-11-23, the user: "The bathroom needs to be a separate room within the hotel room, even if it's actually all the same room":
        // the game's own low cut walls -- its way between two rooms, as the suites' joins -- round the bathroom in the front corner: along
        // z 7 (x 8-11, the front wall's plain column) and along x 8 (z 10-11, the east wall's), their corner the front walls' corner
        // piece; its doorway z 8-9 by the bedroom; its floor and doorway room 169's bathroom tiles)
        for (var x = 9; x <= 11; x++) Column(classic, x, 7, a.Cells, 2, 12);
        Column(classic, 8, 7, a.Cells, 12, 12);
        for (var z = 10; z <= 11; z++) Column(classic, 8, z, a.Cells, 12, 5);
        for (var x = 8; x <= 11; x++)
            for (var z = 8; z <= 11; z++)
                if (x > 8 || z < 10) Column(classic, x, z, bath.Cells, 2 + (x - 8) % 4, 3 + (z - 8) % 2, 0, 0);
        // ("the lamp is a little close" to the bed: block 59 one cell further from it, (1, 8) to (1, 9); "the plant is too big for the room":
        // the potted tree, block 79 at x 1-2, z 2-3, gone)
        for (var y = 1; y < Height; y++)
        {
            classic[At(1, y, 9)] = a.Cells[At(1, y, 8)]; classic[At(1, y, 9) + 1] = a.Cells[At(1, y, 8) + 1];
            classic[At(1, y, 8)] = 0; classic[At(1, y, 8) + 1] = 0;
        }
        Clear(classic, 1, 2, 2, 3, 1);
        // ---- the Junior Suite: room B, room 169's bathroom east of it through a doorway
        var junior = (byte[])b.Cells.Clone();
        Join(junior, bath.Cells, 12, 8, 11);
        for (var z = 0; z <= 12; z++) Column(junior, 12, z, b.Cells, 12, z);
        Doorway(junior, 12);
        ShutDoor(junior, 12, DoorBath, 11);
        // ---- the Imperial Suite: room A (the salon, its bed and the plant by it gone), room B (the bedroom, its bed gone), room 169's bathroom
        var suite = (byte[])a.Cells.Clone();
        Clear(suite, 1, 4, 5, 7, 1);
        Join(suite, b.Cells, 12, 11, 12);
        for (var z = 0; z <= 12; z++) Column(suite, 12, z, a.Cells, 12, z);
        Clear(suite, 13, 16, 3, 5, 1);
        Join(suite, bath.Cells, 24, 8, 11);
        for (var z = 0; z <= 12; z++) Column(suite, 24, z, b.Cells, 12, z);
        Doorway(suite, 12);
        Doorway(suite, 24);
        ShutDoor(suite, 12, DoorB, 12);
        ShutDoor(suite, 24, DoorBath, 11);
        // ---- the lobby: its sofa out of the wall's way
        var lobbyCells = (byte[])lobby.Cells.Clone();
        var sofaCells = 0;
        for (var z = 5; z <= 8; z++)
            for (var x = 27; x <= 28; x++)
                for (var y = 12; y <= 13; y++)
                    if (lobbyCells[At(x, y, z)] == SofaBlock) { lobbyCells[At(x, y, z)] = 0; lobbyCells[At(x, y, z) + 1] = 0; sofaCells++; }
        if (sofaCells == 0) { log.Add("The Imperial Hotel's upper floor left out: the lobby's sofa (block 84) isn't where the game has it"); return log; }

        // ---- the grids in: four new ones, the lobby's own changed
        var grids = new (int Scene, byte[] Raw, byte[] Shape)[]
        {
            (Corridor, a.Raw, Shape(a.Shape, a.Cells, corridor)), (Classic, a.Raw, Shape(a.Shape, a.Cells, classic)),
            (Junior, b.Raw, Shape(b.Shape, b.Cells, junior)), (Suite, a.Raw, Shape(a.Shape, a.Cells, suite)),
            (LiftCar, a.Raw, Shape(a.Shape, a.Cells, car)),
        };
        var type = table[RoomA * 2];
        foreach (var (scene, raw, shape) in grids)
        {
            var grid = Lba2GridBackend.FromLba1Shape(raw, shape);
            var gridNumber = header.Grm - header.Gri;
            if (gridNumber > 255) { log.Add("The Imperial Hotel's upper floor left out: LBA_BKG.HQR has 256 grids already"); return log; }
            bkg.InsertAt(header.Grm, grid);
            header.Grm += 1; header.Bll += 1; header.Brk += 1;
            if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
            table = bkg.Read(header.Brk + header.MaxBrk);
            if (table.Length < (scene + 1) * 2) Array.Resize(ref table, (scene + 1) * 2);
            table[scene * 2] = type; table[scene * 2 + 1] = (byte)gridNumber;
            bkg.SetStored(header.Brk + header.MaxBrk, table);
        }
        var lobbyGrid = Lba2GridBackend.FromLba1Shape(lobby.Raw, Shape(lobby.Shape, lobby.Cells, lobbyCells));
        bkg.SetStored(header.Gri + table[Lobby * 2 + 1], lobbyGrid);
        if (header.MaxGri < (uint)lobbyGrid.Length) header.MaxGri = (uint)lobbyGrid.Length;
        bkg.SetStored(0, header.Bytes);

        // ---- the scenes: each its room's header (light, music), the hero where he comes in, the furniture
        const string Still = "void comportement_0()\n{\n}\n", Stop = "label(0);\nstop();\n";
        void Store(int scene, byte[] record)
        {
            while (scenes.Count <= scene + 1) scenes.Slots.Add(new HqrFile.Slot());
            scenes.SetStored(scene + 1, record);
            if (record.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
            { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, record.Length); scenes.SetStored(0, size); }
        }
        SceneModel Room(int from, (int X, int Z, int Beta) start)
        {
            var model = store.Load(from).Clone();
            model.Island = Island;
            model.Actors.RemoveRange(1, model.Actors.Count - 1);
            model.Zones.Clear();
            model.TrackPoints.Clear();
            (model.Hero.X, model.Hero.Y, model.Hero.Z, model.Hero.Beta) = (start.X, FloorY, start.Z, start.Beta);
            model.Hero.Life = new byte[] { 0 }; model.Hero.Track = new byte[] { 0 };
            // (actor 1 is the engine's own, cleared at each load: an unused helper there)
            SceneOps.AddActor(model, RaceTrackScenes.InvisibleHelper(0, FloorY, 0));
            return model;
        }
        int Put(SceneModel model, int generic, int x, int y, int z, int beta, bool walkOver = false)
        {
            var actor = Citadel.CitadelProps.Actor(generic, x, y, z, beta);
            if (walkOver) actor.Armor = RaceTrackStory.WalkOverArmour;
            return SceneOps.AddActor(model, actor);
        }
        SceneZoneModel Door(int num, int x0, int x1, int z0, int z1, int y, (int X, int Y, int Z) to, int sw, bool on) => new()
        {
            Type = 0, Num = num, X0 = x0, X1 = x1, Y0 = y, Y1 = y + 2000, Z0 = z0, Z1 = z1,
            Info = new[] { to.X, to.Y, to.Z, 0, sw, 0, 0, on ? 1 : 0 },
        };
        var built = new List<string>();
        bool Save(int scene, SceneModel model, string what)
        {
            var scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
            for (var i = 0; i < model.Actors.Count; i++) { scripts.SetText(i, ScriptKind.Life, Still); scripts.SetText(i, ScriptKind.Track, Stop); }
            var result = scripts.Build();
            if (!result.Ok) { log.AddRange(result.Errors.Select(e => $"The Imperial Hotel: {what} left out: {e}")); return false; }
            Store(scene, result.Record!);
            built.Add(what);
            return true;
        }
        // the lift car: its doors (actor 2: the story's ride turns on them), its back wall's panelling; the two ways out by its doors, off
        // till the ride's end -- to the first floor's lift, to the lobby's, Twinsen in each lift's doorway
        var lc = Room(RoomA, (CarStand.X, CarStand.Z, 3072));
        lc.Music = -1;      // (none of its own: the lift's tune, the floor's music stopped as it loads -- RACEMOD.CPP quiet_scene=)
        if (Put(lc, HotelProps.LiftInsideOpen, CarDoors.X, FloorY, CarDoors.Z, 1024) != CarDoorsActor) { log.Add("The Imperial Hotel: the lift car's doors aren't actor 2"); return log; }
        Put(lc, HotelProps.LiftCarBack, 1536, FloorY, 1034, 0);
        lc.Zones.Add(Door(Corridor, 512, 1100, CarDoors.Z - 600, CarDoors.Z + 600, FloorY, (CorridorLift.X + LiftDoorOut, FloorY, CorridorLift.Z), CarToCorridor, false));
        lc.Zones.Add(Door(Lobby, 512, 1100, CarDoors.Z - 600, CarDoors.Z + 600, FloorY, (LobbyLift.X + LiftDoorOut, LobbyFloorY, LobbyLift.Z), CarToLobby, false));
        if (!Save(LiftCar, lc, "the lift car")) return log;
        // the corridor: out of the lift at its west end, facing +x
        var c = Room(RoomA, (CorridorLift.X + LiftFrontOut, CorridorLift.Z, 1024));
        if (Put(c, HotelProps.LiftFront, CorridorLift.X, FloorY, CorridorLift.Z, 1024) != CorridorLiftActor) { log.Add("The Imperial Hotel: the corridor's lift isn't actor 2"); return log; }
        Put(c, HotelProps.CorridorDoors, DoorX(HotelProps.FirstRoom), FloorY, BackWallFace + 4, 0);
        for (var k = 0; k < HotelProps.Rooms; k++) Put(c, HotelProps.RoomDoor + k, DoorX(HotelProps.FirstRoom + k), FloorY, BackWallFace + 4, 0);
        Put(c, HotelProps.SuiteDoors, SuiteDoorX, FloorY, BackWallFace + 4, 0);
        foreach (var x in new[] { 3500, 9500, 15500 }) Put(c, HotelProps.Chandelier, x, FloorY + 2650, 2560, 0);
        c.Zones.Add(Door(Classic, DoorX(ClassicDoor) - 450, DoorX(ClassicDoor) + 450, BackWallFace, BackWallFace + 600, FloorY, (RoomArrival.X, FloorY, RoomArrival.Z), ClassicSwitch, false));
        c.Zones.Add(Door(Junior, DoorX(JuniorDoor) - 450, DoorX(JuniorDoor) + 450, BackWallFace, BackWallFace + 600, FloorY, (JuniorArrival.X, FloorY, JuniorArrival.Z), JuniorSwitch, false));
        c.Zones.Add(Door(Suite, SuiteDoorX - 800, SuiteDoorX + 800, BackWallFace, BackWallFace + 600, FloorY, (RoomArrival.X, FloorY, RoomArrival.Z), SuiteSwitch, false));
        c.Zones.Add(Door(LiftCar, CorridorLift.X, CorridorLift.X + 900, CorridorLift.Z - 500, CorridorLift.Z + 500, FloorY, (CarStand.X, FloorY, CarStand.Z), LiftSwitch, false));
        if (!Save(Corridor, c, "the corridor")) return log;
        // (back out to the corridor from a room, before its door there)
        SceneZoneModel Out(int doorX, int doorCell) => Door(Corridor, doorCell * 512, (doorCell + 2) * 512, 6144, 6656, FloorY, (doorX, FloorY, BackWallFace + 900), 0, true);
        // the Classic Room: the bathroom in the front corner behind its partition
        var r1 = Room(RoomA, (RoomArrival.X, RoomArrival.Z, 2048));
        // (the bathroom's walls are bricks now, its doorway at their north-west; the shower in its back corner, against the north and east
        // walls, the toilet on the east wall by the front, the basin against the west wall's inside)
        Put(r1, Citadel.CitadelProps.Shower, 5664, FloorY, 4576, 1024);
        Put(r1, Citadel.CitadelProps.Toilet, 5850, FloorY, 5650, 3072);
        Put(r1, Citadel.CitadelProps.BathSink, 4760, FloorY, 5450, 1024);
        Put(r1, HotelProps.Painting, 2600, FloorY + 600, 1034, 0);
        // (the rug -- 2400 x 1700 -- on the open floor between the bed, the wardrobe and the bathroom: it stood under the wardrobe and was
        // drawn over it, the user: "drawn over what is presumably a cupboard standing in the corner")
        Put(r1, HotelProps.HotelRug, 3840, FloorY, 2300, 0, walkOver: true);
        r1.Zones.Add(Out(DoorX(ClassicDoor), DoorA));
        if (!Save(Classic, r1, "the Classic Room")) return log;
        // the Junior Suite: its lounge, its chandelier, a painting; the bath through the doorway
        var r2 = Room(RoomB, (JuniorArrival.X, JuniorArrival.Z, 2048));
        Put(r2, HotelProps.Sofa, 3800, FloorY, 3500, 0);
        Put(r2, HotelProps.CoffeeTable, 3800, FloorY, 4600, 0);
        Put(r2, HotelProps.Chandelier, 3800, FloorY + 2650, 3900, 0);
        Put(r2, HotelProps.HotelRug, 3800, FloorY, 4300, 0, walkOver: true);
        // (no painting: on room 170's back wall it stood behind the wardrobe, and there is no width of wall clear of the furniture for it)
        r2.Zones.Add(Out(DoorX(JuniorDoor), DoorB));
        if (!Save(Junior, r2, "the Junior Suite")) return log;
        // the Imperial Suite: the salon (the fireplace on its west wall, sofas facing over the coffee table, the piano, the table for eight,
        // two chandeliers), the bedroom (the four-poster, a chandelier, a rug), the bathroom as room 169 has it
        var r3 = Room(RoomA, (RoomArrival.X, RoomArrival.Z, 2048));
        Put(r3, HotelProps.Fireplace, 522, FloorY, 3200, 1024);
        Put(r3, HotelProps.Sofa, 2100, FloorY, 2000, 0);
        Put(r3, HotelProps.Sofa, 2100, FloorY, 4400, 2048);
        Put(r3, HotelProps.CoffeeTable, 2100, FloorY, 3200, 1024);
        Put(r3, HotelProps.HotelRug, 2100, FloorY, 3200, 1024, walkOver: true);
        Put(r3, HotelProps.GrandPiano, 4700, FloorY, 2300, 3072 - 300);
        // (2026-11-23: the table for eight gone -- the salon crowded, its chairs in the sofas -- and its chandelier; the painting over the
        // piano, on the salon's back wall clear of the furniture; the bedroom's chandelier, inside the four-poster's canopy, gone; its rug
        // before the bed, clear of the front wall)
        Put(r3, HotelProps.Chandelier, 2100, FloorY + 2650, 3200, 0);
        Put(r3, HotelProps.Painting, 3840, FloorY + 600, 1034, 0);
        Put(r3, HotelProps.FourPoster, SuiteBed.X, FloorY, SuiteBed.Z, 0);
        Put(r3, HotelProps.HotelRug, 8448, FloorY, 4900, 0, walkOver: true);
        r3.Zones.Add(Out(SuiteDoorX, DoorA));
        if (!Save(Suite, r3, "the Imperial Suite")) return log;

        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        log.Add($"the Imperial Hotel's upper floor: scenes {Corridor}-{LiftCar} ({string.Join(", ", built)}), from the hotel's rooms 79, 170 and 169; " +
                $"the lobby's sofa out of its alcove's west wall ({sofaCells} cells) for the lift");
        return log;
    }
}
