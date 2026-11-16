using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.LbaScript;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain.Citadel;

// Twinsen's house upgraded (the user, 2026-11-16): "The window to the left of the bed we're going to remove and instead put a doorway there
// into a bathroom area, with toilet, sink, shower, and a large corner bath. The other side where the chair is in the corner, we're going to
// remove this chair, and add another entrance into a kitchen area. Twinsen's house from the first game has both a kitchen and bathroom area so
// we could take inspiration from this, but keep the look and feel of his new house in LBA2. Let's make these changes to the daytime house
// first, then after we're happy with it we can merge them into the night house."
//   - the house (scene 0) sits in its grid's corner -- its north wall on row 0, its west on column 4 -- so there is no room behind either
//     wall: the bathroom and the kitchen are rooms of their own (scenes Bathroom and Kitchen: past 255, reached by doors only), through new
//     doorways in the bedroom's walls -- the window left of the bed (north wall, x 6-9) a doorway between the wall's own pilasters (block 3)
//     under the wallpaper kept as a lintel; the chair in the corner (cell 5, 3) gone and the west wall's archway (as its doorway at z 5-8:
//     jamb 127, arch 171 over a threshold, jamb 172) at z 1-4 for the kitchen, the wall lamp over the chair with it;
//   - each room in the house's own bricks (scene 0's block library, its grid's pieces copied): the house's corner, its north wall (its
//     wallpaper's three strips, a pilaster at each end), its west wall (an end piece, then its three strips), its floor tiles, its front
//     edge and its east edge (the low wall the camera sees over), its front door's frame in the front edge as the way back; furnished with
//     the house's own pieces (a floor lamp, a wall lamp, the sideboard, the chairs) and the new fittings (CitadelProps.HouseRooms);
//   - made after the house at night (CitadelNightHouse) is made from scene 0's grid: the night's house keeps its window and its chair till
//     the day's is liked (the user's wish), and has no door to the rooms.
internal static class CitadelHouseRooms
{
    public const int House = 0, Bathroom = 396, Kitchen = 397;
    private const int FloorLayer = 7, Size = 64, Height = 25;
    public const int FloorY = (FloorLayer + 1) * 256;
    // a room's floor: x RoomX0-RoomX1, z RoomZ0-RoomZ1 (cells) -- the house's own corner, walls at column 4 and row 0
    private const int RoomX0 = 5, RoomX1 = 14, RoomZ0 = 1, RoomZ1 = 8;
    // the doorways in the house: the bathroom's in the north wall (its opening x 7-8, the pilasters at 6 and 9), the kitchen's in the west
    // wall (its opening z 2-3, the jambs at 1 and 4)
    private const int BathDoorX0 = 7, BathDoorX1 = 8, KitchenDoorZ0 = 2, KitchenDoorZ1 = 3;
    private const int DoorDeep = 560;
    // each room's door back: the house's front door's frame (its cells x 22-25 on its front edge, row 20 -- a post, the two cells of its
    // opening, a post) at x 7-10 of the room's front edge, five layers up (the house's front door is at its lower floor's level, layer 2)
    private const int FrontDoorFrom = 22, FrontDoorTo = 7, FrontDoorUp = 5, DoorOpening = FrontDoorTo + 1;

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(House)) { log.Add("The house's bathroom and kitchen left out: no scene 0"); return log; }
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        foreach (var s in new[] { Bathroom, Kitchen })
            if (s + 1 < scenes.Count && !scenes.IsEmpty(s + 1)) { log.Add($"The house's bathroom and kitchen left out: scene {s} is already another part of the build's"); return log; }
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        var type = table[House * 2];
        var houseEntry = header.Gri + table[House * 2 + 1];
        var raw = bkg.Read(houseEntry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var house = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * Size + x) * Height + y) * 2;
        int Mod(int a, int m) => ((a % m) + m) % m;
        // (the house as the game has it: its window where it was, its chair -- a second pass leaves it)
        if (house[At(6, 10, 0)] != 89 || house[At(5, 9, 3)] != 48) { log.Add("The house's bathroom and kitchen left out: the bedroom's window or its chair isn't where it was (made already?)"); return log; }
        List<(int Y, int Block, int Pos)> Column(int x, int z) => Shifted(x, z, 0);
        // (a column moved up or down, the house's outermost rock -- block 173, an outer face over the floor's height -- left out)
        List<(int Y, int Block, int Pos)> Shifted(int x, int z, int up)
        {
            var list = new List<(int, int, int)>();
            for (var y = 0; y < Height; y++)
            {
                var at = At(x, y, z);
                if ((house[at] != 0 || house[at + 1] != 0) && house[at] != 173 && y + up >= 0 && y + up < Height) list.Add((y + up, house[at], house[at + 1]));
            }
            return list;
        }

        // ---- the house's doorways
        var houseEdits = new List<Lba1GridCell>();
        void SetColumn(List<Lba1GridCell> edits, int x, int z, List<(int Y, int Block, int Pos)> column)
        {
            for (var y = 0; y < Height; y++)
            {
                var c = column.FirstOrDefault(k => k.Y == y);
                edits.Add(column.Any(k => k.Y == y) ? new Lba1GridCell(x, y, z, c.Block, c.Pos) : new Lba1GridCell(x, y, z, 0, 0));
            }
        }
        // (the north wall: pilasters at 6 and 9 -- its own at 5 and 13 -- the opening's wall kept from layer 14 up, a threshold at its foot)
        SetColumn(houseEdits, 6, 0, Column(5, 0));
        SetColumn(houseEdits, 9, 0, Column(13, 0));
        foreach (var x in new[] { BathDoorX0, BathDoorX1 })
        {
            var wall = Column(10 + Mod(x - 10, 3), 0).Where(c => c.Y >= 14).ToList();
            wall.Add((FloorLayer, 81, 4));
            SetColumn(houseEdits, x, 0, wall);
        }
        // (the west wall: its doorway's archway at z 1-4, the chair's cell and the wall lamp's floor again)
        SetColumn(houseEdits, 4, 1, Column(4, 5));
        SetColumn(houseEdits, 4, KitchenDoorZ0, Column(4, 6));
        SetColumn(houseEdits, 4, KitchenDoorZ1, Column(4, 7));
        SetColumn(houseEdits, 4, 4, Column(4, 8));
        SetColumn(houseEdits, 5, 3, new() { (FloorLayer, 31, Mod(5, 3) + 3 * Mod(3, 3)) });
        SetColumn(houseEdits, 5, 2, Column(5, 2).Where(c => c.Y == FloorLayer).ToList());
        var houseGrid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, houseEdits));
        bkg.SetStored(houseEntry, houseGrid);
        if (header.MaxGri < (uint)houseGrid.Length) header.MaxGri = (uint)houseGrid.Length;

        // ---- a room: the house's corner, walls, floor, edges and front door frame, then its furniture's columns
        byte[] Room(Dictionary<(int X, int Z), List<(int Y, int Block, int Pos)>> furniture)
        {
            var columns = new Dictionary<(int X, int Z), List<(int Y, int Block, int Pos)>>();
            columns[(4, 0)] = Column(4, 0);
            for (var x = RoomX0; x <= RoomX1; x++)
                columns[(x, 0)] = x == RoomX0 ? Column(5, 0) : x == RoomX1 ? Column(13, 0) : Column(10 + Mod(x - 6, 3), 0);
            for (var z = RoomZ0; z <= RoomZ1; z++)
                columns[(4, z)] = z == RoomZ0 ? Column(4, 1) : Column(4, 2 + Mod(z - 2, 3));
            for (var z = RoomZ0; z <= RoomZ1; z++)
                for (var x = RoomX0; x <= RoomX1; x++)
                    columns[(x, z)] = new() { (FloorLayer, 31, Mod(x, 3) + 3 * Mod(z, 3)) };
            // (the front edge, row RoomZ1 + 1 -- the house's row 20 -- and the row past it, 21; the east edge, column RoomX1 + 1 -- the house's
            // 28 -- and the column past it, 29; their corners)
            // (the front edge, row RoomZ1 + 1: the storeroom's front -- its rock, its wooden cap -- two layers down, to stand as a low wall at
            // the room's floor; the east edge, column RoomX1 + 1: the house's plain east edge three layers up; the front's west corner the
            // storeroom's, tall, as the west wall is; the east's north end the house's; the rows and columns past them, walls one doesn't see)
            var unseen = Enumerable.Range(FloorLayer, 6).Select(y => (y, 0, 1)).ToList();
            columns[(4, RoomZ1 + 1)] = Column(4, 20);
            for (var x = RoomX0; x <= RoomX1; x++) columns[(x, RoomZ1 + 1)] = Shifted(8 + Mod(x, 2), 20, -2);
            for (var z = RoomZ0; z <= RoomZ1; z++) columns[(RoomX1 + 1, z)] = Shifted(28, 16 + Mod(z, 4), 3);
            columns[(RoomX1 + 1, 0)] = Column(28, 0);
            columns[(RoomX1 + 1, RoomZ1 + 1)] = Shifted(28, 20, 3);
            for (var x = 4; x <= RoomX1 + 2; x++) columns[(x, RoomZ1 + 2)] = unseen;
            for (var z = 0; z <= RoomZ1 + 1; z++) columns[(RoomX1 + 2, z)] = unseen;
            // (the way back: the house's front door's frame)
            for (var k = 0; k < 4; k++) columns[(FrontDoorTo + k, RoomZ1 + 1)] = Shifted(FrontDoorFrom + k, 20, FrontDoorUp);
            foreach (var (c, col) in furniture) columns[c] = col;
            var edits = new List<Lba1GridCell>();
            for (var z = 0; z < Size; z++)
                for (var x = 0; x < Size; x++)
                    SetColumn(edits, x, z, columns.TryGetValue((x, z), out var col) ? col : new());
            var grid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, edits));
            grid[0] = raw[0];
            return grid;
        }
        // (a piece of the house's furniture where it stands in the house, its floor under it as the room's)
        List<(int, int, int)> Piece(int x, int z, int roomX, int roomZ) =>
            Column(x, z).Where(c => c.Y > FloorLayer).Append((FloorLayer, 31, Mod(roomX, 3) + 3 * Mod(roomZ, 3))).ToList();
        var bathFurniture = new Dictionary<(int X, int Z), List<(int Y, int Block, int Pos)>>
        {
            [(5, 5)] = Piece(5, 2, 5, 5),     // the house's wall lamp on the west wall
            [(14, 8)] = Piece(5, 5, 14, 8),   // a floor lamp in the front corner
        };
        var kitchenFurniture = new Dictionary<(int X, int Z), List<(int Y, int Block, int Pos)>>
        {
            [(5, 4)] = Piece(5, 2, 5, 4),     // the wall lamp
            [(5, 7)] = Piece(5, 5, 5, 7),     // the floor lamp
            [(13, 5)] = Column(5, 3), [(13, 6)] = Column(5, 3),   // two of the house's chairs by the table (their base with them)
        };
        var grids = new[] { (Bathroom, Room(bathFurniture)), (Kitchen, Room(kitchenFurniture)) };
        foreach (var (scene, grid) in grids)
        {
            var gridNumber = header.Grm - header.Gri;
            if (gridNumber > 255) { log.Add("The house's bathroom and kitchen left out: LBA_BKG.HQR has 256 grids already"); return log; }
            bkg.InsertAt(header.Grm, grid);
            header.Grm += 1; header.Bll += 1; header.Brk += 1;
            if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
            table = bkg.Read(header.Brk + header.MaxBrk);
            if (table.Length < (scene + 1) * 2) Array.Resize(ref table, (scene + 1) * 2);
            table[scene * 2] = type; table[scene * 2 + 1] = (byte)gridNumber;
            bkg.SetStored(header.Brk + header.MaxBrk, table);
        }
        bkg.SetStored(0, header.Bytes);

        // ---- the scenes: scene 0's header (its light, its music), Twinsen in from the front door, the door back to the house, the fittings
        var houseModel = store.Load(House);
        // (where each room's door brings him into the house: before the doorway, facing into the bedroom)
        var intoHouse = new Dictionary<int, (int X, int Y, int Z)>
        {
            [Bathroom] = (BathDoorX0 * 512, FloorY, 1 * 512 + 100),
            [Kitchen] = (5 * 512 + 100, FloorY, KitchenDoorZ0 * 512),
        };
        const int DoorWide = 1024;
        foreach (var scene in new[] { Bathroom, Kitchen })
        {
            var model = houseModel.Clone();
            model.Actors.RemoveRange(1, model.Actors.Count - 1);
            model.Zones.Clear();
            model.TrackPoints.Clear();
            var texts = new Dictionary<int, (string Life, string Track)>();
            var hero = model.Hero;
            (hero.X, hero.Y, hero.Z) = (DoorOpening * 512 + 512, FloorY, RoomZ1 * 512);
            hero.Beta = 2048;
            hero.Life = new byte[] { 0 }; hero.Track = new byte[] { 0 };
            // (actor 1 is the engine's own, cleared at each load: an unused helper there)
            texts[SceneOps.AddActor(model, RaceTrackScenes.InvisibleHelper(0, FloorY, 0))] = ("void comportement_0()\n{\n}\n", "label(0);\nstop();\n");
            // the door back: the frame's opening (its cells 2-3), walked out of at the front edge
            var back = intoHouse[scene];
            model.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = House, X0 = DoorOpening * 512, X1 = DoorOpening * 512 + DoorWide - 1, Z0 = (RoomZ1 + 1) * 512, Z1 = (RoomZ1 + 2) * 512 - 1, Y0 = 0, Y1 = FloorY + 1500,
                Info = new[] { back.X, back.Y, back.Z, 0, 0, 0, 0, 1 },
            });
            foreach (var (generic, x, z, beta) in scene == Bathroom ? CitadelProps.BathroomFittings : CitadelProps.KitchenFittings)
            {
                var a = CitadelProps.Actor(generic > 127 ? (sbyte)generic : generic, x, FloorY, z, beta);
                a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 };
                texts[SceneOps.AddActor(model, a)] = ("void comportement_0()\n{\n}\n", "label(0);\nstop();\n");
            }
            var built = SceneScripts.Load(SceneSerializer.Write(model), scene);
            built.SetText(0, ScriptKind.Life, "void comportement_0()\n{\n}\n");
            built.SetText(0, ScriptKind.Track, "label(0);\nstop();\n");
            foreach (var (actor, (life, track)) in texts) { built.SetText(actor, ScriptKind.Life, life); built.SetText(actor, ScriptKind.Track, track); }
            var result = built.Build();
            if (!result.Ok) { log.AddRange(result.Errors.Select(e => $"The house's room {scene} left out: {e}")); return log; }
            var entry = scene + 1;
            while (scenes.Count <= entry) scenes.Slots.Add(new HqrFile.Slot());
            scenes.SetStored(entry, result.Record!);
            if (result.Record!.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
            { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, result.Record.Length); scenes.SetStored(0, size); }
        }
        // ---- the house's doors to them: the bathroom's walked north into the opening, the kitchen's west into the archway
        // (DoorDeep: the opening's threshold cell and a step into the room, 48 past the wall's face -- the threshold's outer half only, he
        // stopped in the opening at 2,390 and 494, short of it: the story build's walk-through of 2026-11-16; along the wall his middle is
        // 150 further off it)
        houseModel.Zones.Add(new SceneZoneModel
        {
            Type = 0, Num = Bathroom, X0 = BathDoorX0 * 512, X1 = (BathDoorX1 + 1) * 512 - 1, Z0 = 0, Z1 = DoorDeep, Y0 = FloorY - 100, Y1 = FloorY + 1500,
            Info = new[] { DoorOpening * 512, FloorY, RoomZ1 * 512 - 400, 0, 0, 0, 0, 1 },
        });
        houseModel.Zones.Add(new SceneZoneModel
        {
            Type = 0, Num = Kitchen, X0 = 4 * 512, X1 = 4 * 512 + DoorDeep, Z0 = KitchenDoorZ0 * 512, Z1 = (KitchenDoorZ1 + 1) * 512 - 1, Y0 = FloorY - 100, Y1 = FloorY + 1500,
            // (his place along the archway, 1,024, added to z: from 2,000 short of the room's front, clear of its door back)
            Info = new[] { DoorOpening * 512 + 256, FloorY, RoomZ1 * 512 - 200 - DoorWide, 0, 0, 0, 0, 1 },
        });
        var houseRecord = SceneSerializer.Write(houseModel);
        scenes.SetStored(House + 1, houseRecord);
        if (houseRecord.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
        { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, houseRecord.Length); scenes.SetStored(0, size); }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        log.Add($"Twinsen's house: a doorway in the bedroom's north wall (the window gone) into the bathroom (scene {Bathroom}), the west wall's corner chair gone for an archway into the kitchen (scene {Kitchen}); each room in the house's own bricks, furnished ({CitadelProps.BathroomFittings.Length} and {CitadelProps.KitchenFittings.Length} fittings)");
        return log;
    }
}
