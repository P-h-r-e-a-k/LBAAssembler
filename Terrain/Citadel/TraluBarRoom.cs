using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;

namespace LBAAssembler.Terrain.Citadel;

// The Tavern regulars' secret bar, the build's side: scene 19's hall (the Tralu's caves' second scene, room 21) made bigger and furnished
// (the user, 2026-11-06: "Let's move the bar scene area to scene '19: Citadel Island, Tralu 2nd scene (room #21)', it's supposed to be a
// secret easter egg for people to find. Let's expand the scene, perhaps a massive old fashioned wooden table in the centre, some smaller
// round tables around the edge"). The people, their lines and the scene's actors and zones are the story's (RaceTrackStory.TraluTavern).
// All of it out of the hall's own block library (library 1, the Tralu's caves' and the sewers'), cells set in its grid (Lba1GridEdit):
//   - the hall: the room's long back wall (x 39) taken out from z 35 to 53 and the floor carried west to x 28 -- the paving of the room
//     (block 14, 3 x 3 slabs), a hall of 18 x 19 cells with the room's own floor east of the old wall (x 40-45, cleared of its stalagmites);
//     its walls the room's own: the west wall (x 27) the old back wall's cells z for z (its barred windows with it), the north wall (z 34)
//     built as the room's north wall is (its stones, caps and merlons), and the front's rocky parapet (z 54) the room's own (blocks 47,
//     54, 32: four cells repeating) along the new stretch;
//   - the massive old table in its middle: the caves' wooden table (block 100, 2 x 4 cells, three layers -- the one in the cave's first
//     scene, scene 2) three times end to end, x 36-37, z 40-51, a bench (block 101, 1 x 3 cells, two layers) all along either side (x 35
//     and 38), plates, bowls, bottles and an oil lamp on it (blocks 102-110);
//   - the round tables round the edge: the caves' barrels (block 10, 2 x 2 cells, four layers) stood up as tall round tables, with a
//     bottle and a plate or a bowl on each -- by the west wall, at the front and by the old room;
//   - two of the caves' iron candlesticks (block 95) against the north wall, either side of where Luc's counter stands;
//   - the way from the bar to the back gate (RaceTrackStory.TraluCaves): the corridor over the water to scene 2's gate starts on a landing
//     (x 55-57, z 39-43, layer 12) the game reached only from its raised walks, behind a thicket of stalagmites (x 53-54); four of the
//     caves' steps (block 137, rising east) go up to it from the room's floor, x 51-54, z 40-42, the stalagmites in their way taken out.
// Where the build finds scene 19's grid not the game's (the back wall not where it was), the hall is left as it is and said so.
internal static class TraluBarRoom
{
    public const int Scene = 19;
    // the hall: the new floor (x 28-39), the room's floor east of the old wall (x 40-45), z 35-53; the floor's layer (its top at 2304)
    public const int West = 28, OldWall = 39, East = 45, North = 35, South = 53, FloorLayer = 8;
    public const int FloorY = (FloorLayer + 1) * 256;
    // library 1's blocks: the paving, the table, the bench, a barrel, the candlestick, the parapet's rocky face (the front's columns x 41-44)
    private const int Paving = 14, Table = 100, Bench = 101, Barrel = 10, Candlestick = 95;
    private const int ParapetFrom = 41, ParapetPeriod = 4;
    // the massive table: x 36-37, z 40-51 (three tables end to end); its benches at x 35 and 38
    public const int TableX = 36, TableZ0 = 40, TableZ1 = 51, BenchWest = 35, BenchEast = 38;
    // the round tables (barrels): their north-west cells
    public static readonly (int X, int Z)[] Barrels = { (29, 37), (29, 44), (29, 50), (41, 51), (43, 36) };
    // what stands on them and on the table: (x, z, block) -- plates 108 109, bowls 102 104, bottles 105 106 107, the oil lamp 110 (the
    // tall ones on the table's near half, x 37: on the far half they hid the sitters on the far bench from the camera)
    private static readonly (int X, int Z, int Block)[] OnTable =
    {
        (36, 40, 108), (37, 41, 106), (36, 42, 109), (37, 43, 108), (37, 45, 104), (37, 47, 110), (36, 47, 107), (37, 49, 105), (36, 50, 102),
    };
    private static readonly (int X, int Z, int Block)[] OnBarrels =
    {
        (29, 37, 107), (30, 38, 108), (30, 44, 105), (29, 45, 109), (29, 50, 106), (30, 51, 102), (41, 51, 108), (42, 52, 107), (44, 36, 104),
    };
    private static readonly (int X, int Z)[] Candlesticks = { (31, 35), (36, 35) };
    // the steps up to the corridor's landing: x 51 (layer 9) to 54 (layer 12), z 40-42; the stalagmites cleared, x 53-54, z 39-43
    public const int UpStep = 137, UpFrom = 51, UpZ0 = 40, UpZ1 = 42, LandingLayer = 12;
    private static readonly (int X0, int X1, int Z0, int Z1) Thicket = (53, 54, 39, 43);

    private const int Size = 64, Height = 25;

    // Whether a game folder's scene 19 is the bar's hall (the massive table's first cell where Install puts it)
    public static bool Built(string gameDirectory)
    {
        var backend = new Lba2GridBackend(gameDirectory);
        if (backend.GridOfScene(Scene) is not { } grid) return false;
        var cells = Lba1GridCodec.Decode(backend.LoadGrid(grid));
        return cells[((TableZ0 * Size + TableX) * Height + FloorLayer + 1) * 2] == Table && cells[((North * Size + OldWall) * Height + 13) * 2] == 0;
    }

    // ---- the back gate's stairs, in the caves' first scene (scene 2) ------------------------------------------------------------------------
    // The gate into scene 19 (its door zone x 7-8, z 17) stands on a ledge (the stone block with the sign of Sendell on its face, cells x
    // 6-9, z 16-20, its top layer 12: 3328) six layers over the Tralu's hall (its floor's top layer 6: 1792), with nothing to climb: come
    // down from 19 into the hall, Twinsen couldn't get back up (the user, 2026-11-06: "In scene 2 there's a soft lock for the top area ...
    // We'd need to either put a ladder or some stairs there"). Stairs now go up along the block's east face, two cells wide (z 20-21): six
    // of the caves' steps (block 74, rising towards -x, as the steps in scene 19 do), x 15 (layer 7) to x 10 (layer 12), onto the ledge at
    // x 9, the cage's bars on that side (x 9, z 19-20, layers 13-17) taken out to let him on. The Sendell sign on the block's front stays
    // in sight.
    public const int GateScene = 2;
    public const int StairsTop = 10, StairsZ0 = 20, StairsZ1 = 21, LedgeX = 9, LedgeLayer = 12, HallLayer = 6;
    private const int Step = 74, CageBars = 28;

    public static bool StairsBuilt(string gameDirectory)
    {
        var backend = new Lba2GridBackend(gameDirectory);
        if (backend.GridOfScene(GateScene) is not { } grid) return false;
        var cells = Lba1GridCodec.Decode(backend.LoadGrid(grid));
        return cells[((StairsZ0 * Size + StairsTop) * Height + LedgeLayer) * 2] == Step;
    }

    // Into a game folder: the stairs up to the back gate's ledge in scene 2's grid. A line for the build's log.
    public static string InstallStairs(string gameDirectory)
    {
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (GateScene * 2 + 1 >= table.Length) return "the back gate's stairs left out: no scene 2";
        var entry = header.Gri + table[GateScene * 2 + 1];
        var raw = bkg.Read(entry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * Size + x) * Height + y) * 2;
        var steps = LedgeLayer - HallLayer;
        // (the game's: the ledge's top at x 9, z 19-20, its bars over it; the hall's floor under where the stairs go, nothing over it)
        bool Game() => raw[0] == 1
            && Enumerable.Range(StairsZ0 - 1, 2).All(z => cells[At(LedgeX, LedgeLayer, z)] != 0 && cells[At(LedgeX, LedgeLayer + 1, z)] == CageBars)
            && Enumerable.Range(StairsTop, steps).All(x => Enumerable.Range(StairsZ0, StairsZ1 - StairsZ0 + 1).All(z =>
                cells[At(x, HallLayer, z)] != 0 && Enumerable.Range(HallLayer + 1, Height - HallLayer - 1).All(y => cells[At(x, y, z)] == 0)));
        if (!Game()) return "the back gate's stairs left out: scene 2's grid isn't the game's there (the ledge at x 9, the hall's floor x 10-15, z 20-21)";
        var edits = new List<Lba1GridCell>();
        for (var k = 0; k < steps; k++)
            for (var z = StairsZ0; z <= StairsZ1; z++)
                edits.Add(new Lba1GridCell(StairsTop + k, LedgeLayer - k, z, Step, (z - StairsZ0) % 2));
        for (var z = StairsZ0 - 1; z <= StairsZ0; z++)
            for (var y = LedgeLayer + 1; y <= LedgeLayer + 5; y++) edits.Add(new Lba1GridCell(LedgeX, y, z, 0, 0));
        var stored = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, edits));
        bkg.SetStored(entry, stored);
        if (header.MaxGri < (uint)stored.Length) { header.MaxGri = (uint)stored.Length; bkg.SetStored(0, header.Bytes); }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Commit();
        return $"the back gate's stairs: scene 2, {steps} steps up the ledge's east face (x {StairsTop + steps - 1} to {StairsTop}, z {StairsZ0}-{StairsZ1}), the cage's bars at x {LedgeX}, z {StairsZ0 - 1}-{StairsZ0} taken out";
    }

    // Into a game folder: scene 19's grid made the bar's. A line for the build's log.
    public static string Install(string gameDirectory)
    {
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Scene * 2 + 1 >= table.Length) return "the secret bar's hall left out: no scene 19";
        var entry = header.Gri + table[Scene * 2 + 1];
        var raw = bkg.Read(entry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * Size + x) * Height + y) * 2;
        (int Block, int Pos)[] Column(int x, int z) => Enumerable.Range(0, Height).Select(y => ((int)cells[At(x, y, z)], (int)cells[At(x, y, z) + 1])).ToArray();
        // (the game's: the back wall from layer 9 to 20 all along the stretch, nothing west of it, the floor east of it)
        bool Wall(int z) => Enumerable.Range(9, 12).All(y => cells[At(OldWall, y, z)] != 0);
        if (raw[0] != 1 || !Enumerable.Range(North, South - North + 1).All(Wall)
            || Enumerable.Range(West - 1, OldWall - West + 1).Any(x => Column(x, 40).Any(c => c.Block != 0))
            || cells[At(OldWall + 1, FloorLayer, 40)] == 0)
            return "the secret bar's hall left out: scene 19's grid isn't the game's (its back wall at x 39, z 35-53)";

        var edits = new Dictionary<(int X, int Y, int Z), (int Block, int Pos)>();
        void Set(int x, int y, int z, int block, int pos) => edits[(x, y, z)] = (block, pos);
        void Put(int x, int z, (int Block, int Pos)[] column) { for (var y = 0; y < Height; y++) Set(x, y, z, column[y].Block, column[y].Pos); }
        var oldWall = Enumerable.Range(0, Size).ToDictionary(z => z, z => Column(OldWall, z));
        var front = Enumerable.Range(ParapetFrom, ParapetPeriod).ToDictionary(x => x, x => Column(x, South + 1));

        // the hall's floor: the paving from x 28 to the old wall, nothing over it; the room's floor east of it cleared of its stalagmites
        for (var z = North; z <= South; z++)
        {
            for (var x = West; x <= OldWall; x++)
            {
                for (var y = 0; y < Height; y++) Set(x, y, z, 0, 0);
                Set(x, FloorLayer, z, Paving, (x - West) % 3 + 3 * ((z - North) % 3));
            }
            for (var x = OldWall + 1; x <= East; x++)
                for (var y = FloorLayer + 1; y < Height; y++) Set(x, y, z, 0, 0);
        }
        // its walls: west (x 27) the old wall z for z, north (z 34) the old wall's plain stretch again, the corners the old wall's own;
        // the front (z 54) the room's parapet, four cells repeating, its south-west corner the old wall's
        for (var z = North; z <= South; z++) Put(West - 1, z, oldWall[z]);
        for (var x = West; x < OldWall; x++) Put(x, North - 1, NorthWall(x));
        Put(West - 1, North - 1, oldWall[North - 1]);
        for (var x = West; x <= OldWall; x++) Put(x, South + 1, front[ParapetFrom + ((x - ParapetFrom) % ParapetPeriod + ParapetPeriod) % ParapetPeriod]);
        Put(West - 1, South + 1, oldWall[South + 1]);

        // the massive table, its benches, what is on it
        for (var t = 0; t * 4 + TableZ0 <= TableZ1; t++)
            for (var dz = 0; dz < 4; dz++)
                for (var dy = 0; dy < 3; dy++)
                    for (var dx = 0; dx < 2; dx++)
                        Set(TableX + dx, FloorLayer + 1 + dy, TableZ0 + t * 4 + dz, Table, dx + 2 * dy + 6 * dz);
        foreach (var x in new[] { BenchWest, BenchEast })
            for (var b = 0; b * 3 + TableZ0 <= TableZ1; b++)
                for (var dz = 0; dz < 3; dz++)
                    for (var dy = 0; dy < 2; dy++)
                        Set(x, FloorLayer + 1 + dy, TableZ0 + b * 3 + dz, Bench, dy + 2 * dz);
        foreach (var (x, z, block) in OnTable) Item(x, FloorLayer + 4, z, block);
        // the round tables (barrels) and what is on them
        foreach (var (bx, bz) in Barrels)
            for (var dz = 0; dz < 2; dz++)
                for (var dy = 0; dy < 4; dy++)
                    for (var dx = 0; dx < 2; dx++)
                        Set(bx + dx, FloorLayer + 1 + dy, bz + dz, Barrel, dx + 2 * dy + 8 * dz);
        foreach (var (x, z, block) in OnBarrels) Item(x, FloorLayer + 5, z, block);
        foreach (var (x, z) in Candlesticks)
            for (var dy = 0; dy < 6; dy++) Set(x, FloorLayer + 1 + dy, z, Candlestick, dy);
        // the steps up to the corridor's landing, through the stalagmites
        for (var x = Thicket.X0; x <= Thicket.X1; x++)
            for (var z = Thicket.Z0; z <= Thicket.Z1; z++)
                for (var y = FloorLayer + 1; y <= LandingLayer; y++) Set(x, y, z, 0, 0);
        for (var k = 0; k < LandingLayer - FloorLayer; k++)
            for (var z = UpZ0; z <= UpZ1; z++) Set(UpFrom + k, FloorLayer + 1 + k, z, UpStep, (z - UpZ0) % 2);

        // (a column of the north wall, built as the room's own north wall (z 6) is -- the old back wall's columns are capped for a wall
        // along z, their caps two cells along z, and showed their stones' tops along x: its foot (layers 9-12, block 57, as the back wall's),
        // the stones (blocks 1-4 in turn, eight layers), its cap (block 18, two cells along x) and every sixth column a merlon (block 56))
        (int Block, int Pos)[] NorthWall(int x)
        {
            var column = new (int Block, int Pos)[Height];
            column[FloorLayer] = (0, 1);
            for (var y = FloorLayer + 1; y <= 12; y++) column[y] = (57, (y - FloorLayer - 1) % 2);
            for (var y = 13; y <= 20; y++) column[y] = (1 + (x - West) % 4, y - 13);
            if ((x - West) % 6 == 4) { column[21] = (56, 0); column[22] = (56, 1); }
            else column[21] = (18, (x - West) % 2);
            return column;
        }

        // (an item: a block one cell across, standing from layer y up -- the oil lamp three layers, a bottle 105 two)
        void Item(int x, int y, int z, int block)
        {
            var tall = block switch { 110 => 3, 105 => 2, _ => 1 };
            for (var dy = 0; dy < tall; dy++) Set(x, y + dy, z, block, dy);
        }

        var edited = Lba1GridEdit.SetCells(shape, edits.Select(e => new Lba1GridCell(e.Key.X, e.Key.Y, e.Key.Z, e.Value.Block, e.Value.Pos)).ToList());
        var stored = Lba2GridBackend.FromLba1Shape(raw, edited);
        bkg.SetStored(entry, stored);
        if (header.MaxGri < (uint)stored.Length) { header.MaxGri = (uint)stored.Length; bkg.SetStored(0, header.Bytes); }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Commit();
        return $"the secret bar: scene 19's hall carried west to x {West} (z {North}-{South}), the massive table at x {TableX}-{TableX + 1}, z {TableZ0}-{TableZ1} with its benches, " +
               $"{Barrels.Length} barrels for round tables, two candlesticks ({edits.Count} cells); " + InstallTankard(gameDirectory);
    }

    // ---- the Tralu's tankard (the user, 2026-11-06: "Move him into the bar area ... with a large tankard of beer") ---------------------------
    // A body of the build's: a big wooden tankard, two iron hoops, a handle, a head of foam over its brim -- one more body of the mushroom's
    // entity (the props' entity: CitadelLba1Rooms.PropEntity), at BODY.HQR's end. It stands on the round table (barrel) by the old room's
    // north wall, its north-west cell (43, 36), at its front cell on the camera's side (43, 37: its bottle taken off for it), the Tralu on
    // its far side (RaceTrackStory.TraluTavern), half as big again as a mug for Twinsen would be.
    public const int TankardGeneric = 47;
    public static readonly (int X, int Y, int Z) TankardAt = (43 * 512 + 256, (FloorLayer + 5) * 256, 37 * 512 + 256);

    private static string InstallTankard(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var like = LbaBodyStudio.Body.Read(HqrArchive.Open(bodyPath).Read(CitadelLba1Rooms.MushroomBody), 2, allowStatic: true).Header;
        var palette = HqrArchive.Open(ressPath).Read(0);
        var m = new Snow.SnowMesh(new Snow.SnowShading(palette, 225, 50));
        var wood = new Snow.Rgb(126, 80, 42);
        var iron = new Snow.Rgb(96, 96, 104);
        var foam = new Snow.Rgb(238, 228, 196);
        const int Sides = 12;
        m.Transform = System.Numerics.Matrix4x4.CreateScale(1.5f);
        // (the staves, a little narrower at the top; the hoops round them; the foam heaped over the brim and down its lip)
        m.Cylinder(new(0, 0, 0), new(0, 300, 0), 120, 108, Sides, wood, capB: false);
        m.Cylinder(new(0, 34, 0), new(0, 70, 0), 127, 125, Sides, iron, capA: false, capB: false);
        m.Cylinder(new(0, 226, 0), new(0, 262, 0), 117, 115, Sides, iron, capA: false, capB: false);
        m.Cylinder(new(0, 300, 0), new(0, 318, 0), 112, 116, Sides, foam, capA: false, capB: false);
        m.Ellipsoid(new(0, 318, 0), 116, 54, 116, 6, Sides, foam, below: false);
        // (the handle: out from the side, down, and back in)
        m.Beam(new(108, 250, 0), new(190, 240, 0), 34, 30, wood);
        m.Beam(new(190, 248, 0), new(190, 72, 0), 34, 30, wood);
        m.Beam(new(190, 80, 0), new(112, 70, 0), 34, 30, wood);
        var body = new LbaBodyStudio.Body { Game = 2, Lit = false, Static = true, Header = like };
        body.Bones.Add(new LbaBodyStudio.Bone(0, m.Points.Count, 0, -1, new byte[8]));
        body.Vertices.AddRange(m.Points);
        body.SetWorld(m.Points.ToArray());
        body.Faces.AddRange(m.Faces);
        body.Validate();
        var index = HqrArchive.CountEntries(bodyPath);
        HqrWriter.AppendToFile(bodyPath, body.Write());
        HqrWriter.ReplaceInFile(ressPath, 44, RaceTrackBaldinoCar.WithBody(HqrArchive.Open(ressPath).Read(44), CitadelLba1Rooms.PropEntity, TankardGeneric, index));
        return $"the Tralu's tankard: BODY.HQR entry {index} ({body.Faces.Count} faces), the props' entity ({CitadelLba1Rooms.PropEntity}) body {TankardGeneric}";
    }
}
