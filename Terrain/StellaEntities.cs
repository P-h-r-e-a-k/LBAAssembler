using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Stella Entities, the club under the Emerald Moon's reactor (docs/LBRA_STRIP_JOINT_DESIGN.md; the user, 2026-11-11: "a hidden elevator
// inside Baldino's cell in the corner immediately to the left of the door, that takes us down to a corridor that then takes us left into
// the area under the giant reactor that's on the moon, as a result the strip joint should be roughly circular"; "Twinsen can access this
// area at any time whilst on Emerald moon"). The build's side, first part: the room itself and the way down.
//   - a new interior scene (Scene: a slot past 255 -- every one under it is the game's or another part of the build's; door zones name
//     it, which the engine takes whole, and saves keep the scene as a long), of Rick's bar's own block library (scene 136, Otringal):
//     the round hall (Radius cells round Centre), the corridor east of it, the lift's foot at its end; Rick's octagon floor tiles (block
//     67) over all of it; walls where the camera looks at them -- the north and west sides -- as Rick's own walls are, column for column
//     (a north wall's cells copied from his north wall, x 38-40 at z 12, a west wall's from his west wall, x 17 at z 36-47 -- each layer
//     at its own repeat: his walls' blocks repeat every 3 or 4 cells); the south and east rims a low stone wall (his walls' foot, block
//     19), which the camera sees over;
//   - Baldino's old cell (scene 54): a door zone in its corner just left of its door, hidden from the camera behind the pillar beside it
//     (cells x 32-33, z 31-32: the corner the user confirmed) -- the hidden lift, down into the new scene's lift foot; and from there a
//     door zone back up, beside where he arrives.
// Still to come (the design's next parts): the stage round the reactor's column, its poles, the catwalk, the bar, the office, backstage,
// the sign; the bouncers and the performers.
internal static class StellaEntities
{
    public const int Scene = 390, Cell = 54, RickBar = 136, MoonIsland = 3;
    // the hall: round, its middle and radius (cells); the corridor (x, z); the lift's foot
    // (the hall's floor: the cells whose middles are within the props' floor radius -- CitadelProps.Club: the round wall and the curb
    // over the floor's stepped edge; inside the curb a ring of invisible solid cells, RingFrom out, ring layers high, holds Twinsen
    // off the curb, open where the corridor comes in)
    private const double CentreX = 24, CentreZ = 32, Radius = Citadel.CitadelProps.ClubFloorRadius / 512.0, RingFrom = 18.6;
    // (the ring of invisible cells inside the curb up to the wall's top, 3,800 -- at 5 (1,536) a jump at the wall landed Twinsen on top of them,
    // hanging in front of the wall in his fall: the user, 2026-11-13, "Jumping towards the edge of the wall leaves twinsen stuck in a falling animation")
    private const int RingTop = Citadel.CitadelProps.ClubWallTop / 256;
    // (the stage's cells: any part within StageCells of the hall's middle -- Twinsen, stopped about half a cell off the cells, then stays just off the round's edge (7.5) all round, never in its skirt; the catwalk's, x
    // CatwalkX0-X1 north of the middle)
    private const double StageCells = 7.0;
    private const int CatwalkX0 = 23, CatwalkX1 = 24;
    // the poles round the column (degrees, the room's middle to them: x = cos, z = sin -- all on the audience's side of it, the camera's:
    // one behind it was drawn over it), the tables', the stools'
    private static readonly float[] PoleAngles = { 10, 60, 110 }, TableAngles = { 20, 60, 200, 235, 305, 335 };
    internal const float PoleCells = 5; private const float TableCells = 11.5f, StoolCells = 14.2f;
    private const int CorridorX0 = 43, CorridorX1 = 56, CorridorZ0 = 30, CorridorZ1 = 34;
    private const int FootX0 = 55, FootX1 = 60, FootZ0 = 26, FootZ1 = 34;
    // the floor's layer (Rick's), its tile (3 x 1 x 3) and where its 3 x 3 repeats from
    private const int FloorLayer = 0, Tile = 67;
    // Rick's walls: his north wall's columns (x 38-40, z 12) and his west wall's (x 17, z 36-47), each layer's repeat; the low wall's block
    private static readonly (int X, int Z) North = (38, 12), West = (17, 36);
    private const int WestUpper = 18, LowWall = 19;
    // The lift (the user, 2026-11-11: "is Twinsen teleporting? I'd like him on an elevator that only activates when he uses action") -- as
    // the house's down to the secret chamber (RaceTrackStory.SecretChamber), a carrier gliding on its track with him on it:
    //   - in the cell, a steel plate (CitadelProps.MoonLiftPlatform) over a shaft cut in its floor (the floor's one layer of bricks, layer
    //     7, taken out under the plate: nothing is under it), flush with the floor. Action on it (a scenario zone, LiftZone, watched by
    //     an invisible helper -- the cell's own scripts left as they are) switches on the door under the floor (off till then: LiftSwitch)
    //     and takes the plate down CellDown with him on it, into the door;
    //   - at the shaft's foot in the club's scene, the same plate on rails (CitadelProps' lift rails), ClubDown over the floor as he comes
    //     out on it, gliding down to the floor; Action on it takes it up ClubUp, into the door up over it, which brings him out on the
    //     cell's plate under the floor (CellUp down the shaft, in ShaftZone), and the plate glides up to the floor with him.
    private const int LiftZone = 70, ShaftZone = 77, ArrivalZone = 76, LiftSwitch = 7;
    private const int LiftRide = 1, LiftDown = 2, LiftRise = 3, LiftRisen = 4, LiftSpeed = 600, LiftStart = 506, LiftStop = 510;
    private const int CellDown = 1600, CellUp = 1200, ClubDown = 1200, ClubUp = 1800;
    // the cell's floor (the top of its layer 7) and the plate's cells: x 32-33, z 31-32, the corner the user confirmed as far into it as
    // Twinsen can go -- the cell's own invisible walls (its grid's block 0 with a collision, behind the pillar) fill x 31 and z 33-34
    // there, which held him at the first try's edge (x 31-32, z 32-33); the plate's middle
    private const int CellFloor = 2048, CellFloorLayer = 7, LiftCellX = 32, LiftCellZ = 31;
    private static readonly (int X, int Y, int Z) CellFoot = ((LiftCellX + 1) * 512, CellFloor - Citadel.CitadelProps.LiftTop, (LiftCellZ + 1) * 512);
    // the club's lift: on the lift's foot's floor, cells x 57-58, z 27-28 (its middle), its open side south, the way into the corridor
    private const int ClubLiftCellX = 57, ClubLiftCellZ = 27;

    private const int Size = 64, Height = 25;
    // the hall's floor (the top of its floor layer); a place round the hall's middle (degrees: x = cos, z = sin; cells out); the beta that
    // faces the middle from a place (an actor's beta 0 looks along +z, 1024 along +x)
    public const int FloorY = (FloorLayer + 1) * 256;
    public static (int X, int Z) At(float degrees, float cells) =>
        ((int)Math.Round(CentreX * 512 + cells * 512 * Math.Cos(degrees * Math.PI / 180)), (int)Math.Round(CentreZ * 512 + cells * 512 * Math.Sin(degrees * Math.PI / 180)));
    public static int FacingMiddle(int x, int z) =>
        ((int)Math.Round(Math.Atan2(CentreX * 512 - x, CentreZ * 512 - z) * 2048 / Math.PI) + 4096) & 4095;

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(RickBar) || !store.SceneExists(Cell)) { log.Add("Stella Entities left out: no Rick's bar (scene 136) or Baldino's cell (scene 54)"); return log; }
        if (Scene + 1 < scenes.Count && !scenes.IsEmpty(Scene + 1)) { log.Add($"Stella Entities left out: scene {Scene} is already another part of the build's"); return log; }
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        var type = table[RickBar * 2];
        var raw = bkg.Read(header.Gri + table[RickBar * 2 + 1]);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * Size + x) * Height + y) * 2;

        // where the floor is: the hall, the corridor, the lift's foot
        bool Hall(int x, int z) => (x + 0.5 - CentreX) * (x + 0.5 - CentreX) + (z + 0.5 - CentreZ) * (z + 0.5 - CentreZ) < Radius * Radius;
        // (the corridor's and the lift foot's floor: their walls are Rick's bricks; the hall's are the props)
        bool Passage(int x, int z) => x is >= 0 and < Size && z is >= 0 and < Size && !Hall(x, z) &&
            ((x >= CorridorX0 && x <= CorridorX1 && z >= CorridorZ0 && z <= CorridorZ1) || (x >= FootX0 && x <= FootX1 && z >= FootZ0 && z <= FootZ1));
        bool Floor(int x, int z) => x is >= 0 and < Size && z is >= 0 and < Size &&
            (Hall(x, z) || (x >= CorridorX0 && x <= CorridorX1 && z >= CorridorZ0 && z <= CorridorZ1) || (x >= FootX0 && x <= FootX1 && z >= FootZ0 && z <= FootZ1));
        var edits = new List<Lba1GridCell>();
        var set = new Dictionary<(int, int, int), (int Block, int Pos)>();
        void Put(int x, int y, int z, int block, int pos) => set[(x, y, z)] = (block, pos);
        // (a column of Rick's, copied whole into (x, z))
        void Column(int x, int z, Func<int, (int X, int Z)> from)
        {
            for (var y = FloorLayer + 1; y < Height; y++)
            {
                var (sx, sz) = from(y);
                var at = At(sx, y, sz);
                if (cells[at] != 0) Put(x, y, z, cells[at], cells[at + 1]);
            }
        }
        int Mod(int a, int m) => ((a % m) + m) % m;
        int floors = 0, northWalls = 0, westWalls = 0, rims = 0, rings = 0, stageCells = 0, counterCells = 0, barEnds = 0;
        for (var z = 0; z < Size; z++)
        for (var x = 0; x < Size; x++)
        {
            if (Floor(x, z))
            {
                Put(x, FloorLayer, z, Tile, Mod(x - 1, 3) + 3 * Mod(z - 1, 3));
                floors++;
                // (the hall's ring of invisible solid cells inside the curb: block 0, collision 1 -- not where the corridor comes in)
                double dx = x + 0.5 - CentreX, dz = z + 0.5 - CentreZ;
                var angle = Math.Atan2(dz, dx) * 180 / Math.PI;
                // (the stage and its catwalk: invisible solid cells under the props, up to its top -- the dancers stand on it. Every cell the
                // stage's round covers, not only those whose middles it does: with those, Twinsen walked into its skirt where the round reached
                // past a cell -- the user, 2026-11-13, "it should be solid and high all the way around")
                var nearX = Math.Clamp(CentreX, x, x + 1.0) - CentreX; var nearZ = Math.Clamp(CentreZ, z, z + 1.0) - CentreZ;
                if (Hall(x, z) && (Math.Sqrt(nearX * nearX + nearZ * nearZ) < StageCells || (x >= CatwalkX0 && x <= CatwalkX1 && dz < 0 && Math.Sqrt(dx * dx + dz * dz) < RingFrom)))
                {
                    for (var y = FloorLayer + 1; y <= FloorLayer + (int)(Citadel.CitadelProps.StageHigh / 256); y++) Put(x, y, z, 0, 1);
                    stageCells++;
                }
                // (the bar's counter: invisible solid cells along its curve, two layers)
                var r = Math.Sqrt(dx * dx + dz * dz);
                if (Hall(x, z) && r >= Citadel.CitadelProps.CounterIn / 512 - 0.15 && r <= Citadel.CitadelProps.CounterOut / 512 + 0.1 &&
                    angle >= Citadel.CitadelProps.BarFrom - 3 && angle <= Citadel.CitadelProps.BarTo + 3)
                {
                    for (var y = FloorLayer + 1; y <= FloorLayer + 2; y++) Put(x, y, z, 0, 1);
                    counterCells++;
                }
                // (behind the bar, the barman's side: shut at both ends of the counter, up to the wall's top as the ring -- Twinsen walked in
                // between the counter and the bottles, and the pieces drawn by their boxes put him under them: the user, 2026-11-13, "Twinsen
                // disappears under the bar")
                var behind = r > Citadel.CitadelProps.CounterOut / 512 - 0.2 && r < RingFrom + 0.5;
                if (Hall(x, z) && behind && ((angle >= Citadel.CitadelProps.BarFrom - 4 && angle <= Citadel.CitadelProps.BarFrom + 1) ||
                                             (angle >= Citadel.CitadelProps.BarTo - 1 && angle <= Citadel.CitadelProps.BarTo + 4)))
                {
                    for (var y = FloorLayer + 1; y <= RingTop; y++) Put(x, y, z, 0, 1);
                    barEnds++;
                }
                if (Hall(x, z) && Math.Sqrt(dx * dx + dz * dz) >= RingFrom && !(angle > Citadel.CitadelProps.ClubDoorFrom && angle < Citadel.CitadelProps.ClubDoorTo))
                {
                    for (var y = FloorLayer + 1; y <= RingTop; y++) Put(x, y, z, 0, 1);
                    rings++;
                }
                continue;
            }
            // a wall where the floor's beside it: the north and west sides Rick's walls, the south and east a low wall
            // (the corridor's walls low -- Rick's stone foot, as round the hall's open side: tall, they stood in front of the hall's
            // north-east from the camera; the lift's foot keeps his tall walls)
            if (x < FootX0 - 1 && (Passage(x, z + 1) || Passage(x + 1, z) || Passage(x + 1, z + 1)))
            {
                Put(x, FloorLayer + 1, z, LowWall, Mod(x + z, 2));
                rims++;
            }
            else if (Passage(x, z + 1) || (Passage(x + 1, z + 1) && !Floor(x + 1, z)))
            {
                Column(x, z, y => (North.X + Mod(x - North.X, 3), North.Z));
                northWalls++;
            }
            else if (Passage(x + 1, z))
            {
                Column(x, z, y => (West.X, West.Z + Mod(z - West.Z, y >= WestUpper ? 3 : 4)));
                westWalls++;
            }
            else if (Passage(x, z - 1) || Passage(x - 1, z) || Passage(x - 1, z - 1) || Passage(x + 1, z - 1) || Passage(x - 1, z + 1))
            {
                Put(x, FloorLayer + 1, z, LowWall, Mod(x + z, 2));
                rims++;
            }
        }
        // the steps up onto the catwalk by the backstage door (CitadelProps.ClubSteps): a ramp down east from the catwalk's edge, a layer a
        // cell -- each column solid up to its slope (collision 5: full height at its west side, nothing at its east: OBJECT.CPP ReajustPos)
        const int SlopeDownEast = 5;
        var stageLayers = (int)(Citadel.CitadelProps.StageHigh / 256);
        for (var k = 0; k < Citadel.CitadelProps.StepsCells; k++)
            for (var z = Citadel.CitadelProps.StepsZ0; z <= Citadel.CitadelProps.StepsZ1; z++)
            {
                var x = Citadel.CitadelProps.StepsCellX + k;
                var top = FloorLayer + stageLayers - k;
                for (var y = FloorLayer + 1; y < top; y++) Put(x, y, z, 0, 1);
                Put(x, top, z, 0, SlopeDownEast);
            }
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
        if (gridNumber > 255) { log.Add("Stella Entities left out: LBA_BKG.HQR has 256 grids already"); return log; }
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
        // (the cell keeps its floor whole: no shaft, nothing in it to show -- the lift is hidden, the user, 2026-11-13: "the elevator down can be
        // seen inside Baldino's cell, it shouldn't be visible at all")
        bkg.SetStored(header.Brk + header.MaxBrk, table);

        // the scene: Rick's bar's header (its light, its music), the Moon's, Twinsen arriving on the lift
        var cell = store.Load(Cell);
        var model = store.Load(RickBar).Clone();
        model.Island = MoonIsland;
        model.Actors.RemoveRange(2, model.Actors.Count - 2);
        model.Zones.Clear();
        model.TrackPoints.Clear();
        var floorY = (FloorLayer + 1) * 256;
        var clubLift = ((ClubLiftCellX + 1) * 512, floorY, (ClubLiftCellZ + 1) * 512);
        var clubArrival = (X: clubLift.Item1, Y: floorY + Citadel.CitadelProps.LiftTop + ClubDown, Z: clubLift.Item3);
        var hero = model.Hero;
        (hero.X, hero.Y, hero.Z) = clubArrival;
        hero.Life = new byte[] { 0 }; hero.Track = new byte[] { 0 };
        // (the cell's way down: a door over the spot at Twinsen's feet, off till Action there (LiftSwitch), which takes him onto the club's
        // lift high in its shaft -- where on the club's platform is where he stood on the spot; tall, as the floor's a step lower at its edge)
        const int Tall = 700, Big = 1024, DontReadjust = 1;
        cell.Zones.RemoveAll(zn => zn.Type == 0 && zn.Num == Scene);
        cell.Zones.Add(new SceneZoneModel
        {
            Type = 0, Num = Scene, X0 = CellFoot.X - 512, X1 = CellFoot.X + 511, Z0 = CellFoot.Z - 512, Z1 = CellFoot.Z + 511, Y0 = CellFloor - Tall, Y1 = CellFloor + Tall,
            Info = new[] { clubArrival.X - 512, clubArrival.Y + 100 - Tall, clubArrival.Z - 512, 0, LiftSwitch, 0, DontReadjust, 0 },
        });
        cell.Zones.Add(new SceneZoneModel { Type = 2, Num = LiftZone, X0 = CellFoot.X - 512, X1 = CellFoot.X + 511, Z0 = CellFoot.Z - 512, Z1 = CellFoot.Z + 511, Y0 = CellFloor - Tall, Y1 = CellFloor + Tall, Info = new int[8] });
        // (the club's way up: over its lift, higher than he comes in on it, on from the start; out on the cell's plate down its shaft)
        var clubTop = floorY + ClubUp - 100;
        model.Zones.Add(new SceneZoneModel
        {
            Type = 0, Num = Cell, X0 = clubLift.Item1 - Big, X1 = clubLift.Item1 + Big - 1, Z0 = clubLift.Item3 - Big, Z1 = clubLift.Item3 + Big - 1, Y0 = clubTop, Y1 = clubTop + 3000,
            Info = new[] { CellFoot.X - Big, CellFloor + 30, CellFoot.Z - Big, 0, 0, 0, 0, 1 },
        });
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = LiftZone, X0 = clubLift.Item1 - Big, X1 = clubLift.Item1 + Big - 1, Z0 = clubLift.Item3 - Big, Z1 = clubLift.Item3 + Big - 1, Y0 = floorY - 100, Y1 = floorY + 800, Info = new int[8] });
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = ArrivalZone, X0 = clubLift.Item1 - Big, X1 = clubLift.Item1 + Big - 1, Z0 = clubLift.Item3 - Big, Z1 = clubLift.Item3 + Big - 1, Y0 = floorY + ClubDown - 300, Y1 = floorY + ClubDown + 900, Info = new int[8] });

        // the lifts' actors: the cell's helper (Action on the spot); the club's platform, four plates two by two
        var cellHelper = SceneOps.AddActor(cell, RaceTrackScenes.InvisibleHelper(CellFoot.X, CellFloor, CellFoot.Z));
        var cellLife = $@"void comportement_0()
{{
    set_change_cube({LiftSwitch}, 0);
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if ({LiftZone} == zone_obj(0))
        {{
            set_change_cube({LiftSwitch}, 1);
            set_comportement(comportement_3);
        }}
    }}
}}

void comportement_3()
{{
}}
";
        var clubParts = new List<int>();
        var clubTracks = new List<(int Low, int High)>();
        foreach (var (dx, dz) in new[] { (-512, -512), (512, -512), (-512, 512), (512, 512) })
        {
            var low = SceneOps.AddTrackPoint(model, new SceneTrackPoint(clubLift.Item1 + dx, floorY, clubLift.Item3 + dz));
            var high = SceneOps.AddTrackPoint(model, new SceneTrackPoint(clubLift.Item1 + dx, floorY + ClubUp, clubLift.Item3 + dz));
            clubParts.Add(SceneOps.AddActor(model, LiftActor(Citadel.CitadelProps.MoonLiftPlatform, clubLift.Item1 + dx, floorY + ClubDown, clubLift.Item3 + dz)));
            clubTracks.Add((low, high));
        }
        var clubPlate = clubParts[0];
        // the hall's round wall and curb (CitadelProps.Club), round its middle, drawn into the backdrop
        var round = new[] { Citadel.CitadelProps.ClubWall, Citadel.CitadelProps.ClubBoard, Citadel.CitadelProps.ClubFront, Citadel.CitadelProps.ClubCurb, Citadel.CitadelProps.ClubStage, Citadel.CitadelProps.ClubSteps, Citadel.CitadelProps.ClubShelf }
            .Select(g => SceneOps.AddActor(model, Backdrop(g, (int)(CentreX * 512) - Citadel.CitadelProps.ClubShiftX, floorY, (int)(CentreZ * 512) - Citadel.CitadelProps.ClubShiftZ))).ToList();
        // ... and the room's furniture in its place among the people: the reactor's column on the stage, the poles round it, the bar's
        // counter, its stools, the tables
        (int X, int Z) Round(float degrees, float cells) => ((int)Math.Round(CentreX * 512 + cells * 512 * Math.Cos(degrees * Math.PI / 180)), (int)Math.Round(CentreZ * 512 + cells * 512 * Math.Sin(degrees * Math.PI / 180)));
        var stageTop = floorY + (int)Citadel.CitadelProps.StageHigh;
        void Furnish(int generic, int x, int y, int z) => round.Add(SceneOps.AddActor(model, Furniture(generic, x, y, z)));
        Furnish(Citadel.CitadelProps.ClubColumn, (int)(CentreX * 512), stageTop, (int)(CentreZ * 512));
        foreach (var a in PoleAngles) { var (x, z) = Round(a, PoleCells); Furnish(Citadel.CitadelProps.ClubPole, x, stageTop, z); }
        foreach (var (generic, a) in Citadel.CitadelProps.CounterAt) { var (x, z) = Round(a, Citadel.CitadelProps.CounterMiddle / 512); Furnish(generic, x, floorY, z); }
        for (var a = Citadel.CitadelProps.BarFrom + 7; a < Citadel.CitadelProps.BarTo - 3; a += 10) { var (x, z) = Round(a, StoolCells); Furnish(Citadel.CitadelProps.ClubBarStool, x, floorY, z); }
        foreach (var a in TableAngles) { var (x, z) = Round(a, TableCells); Furnish(Citadel.CitadelProps.ClubTable, x, floorY, z); }
        string ClubTrack((int Low, int High) t) => $"label(0);\nspeed({LiftSpeed});\ngoto_point_3d({t.Low});\nsample({LiftStop});\n\nlabel({LiftDown});\nstop();\n\n" +
                        $"label({LiftRide});\nsample({LiftStart});\nspeed({LiftSpeed});\ngoto_point_3d({t.High});\nstop();\n";
        var ride = string.Concat(clubParts.Select(i => $"        set_track_obj({i}, label_{LiftRide});\n"));
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
    if ({LiftDown} == l_track_obj({clubPlate}))
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
        static string Still() => "void comportement_0()\n{\n}\n";
        var cellScripts = LbaScript.SceneScripts.Load(SceneSerializer.Write(cell), Cell);
        cellScripts.SetText(cellHelper, LbaScript.ScriptKind.Life, cellLife);
        cellScripts.SetText(cellHelper, LbaScript.ScriptKind.Track, "label(0);\nstop();\n");
        var cellBuilt = cellScripts.Build();
        if (!cellBuilt.Ok) { log.AddRange(cellBuilt.Errors.Select(e => $"Stella Entities left out: the cell's lift script: {e}")); return log; }
        var clubScripts = LbaScript.SceneScripts.Load(SceneSerializer.Write(model), Scene);
        clubScripts.SetText(0, LbaScript.ScriptKind.Life, heroLife);
        for (var q = 0; q < clubParts.Count; q++) { clubScripts.SetText(clubParts[q], LbaScript.ScriptKind.Life, Still()); clubScripts.SetText(clubParts[q], LbaScript.ScriptKind.Track, ClubTrack(clubTracks[q])); }
        foreach (var i in round) { clubScripts.SetText(i, LbaScript.ScriptKind.Life, Still()); clubScripts.SetText(i, LbaScript.ScriptKind.Track, "label(0);\nstop();\n"); }
        var clubBuilt = clubScripts.Build();
        if (!clubBuilt.Ok) { log.AddRange(clubBuilt.Errors.Select(e => $"Stella Entities left out: the club's lift script: {e}")); return log; }

        var entry = Scene + 1;
        while (scenes.Count <= entry) scenes.Slots.Add(new HqrFile.Slot());
        foreach (var (slot, record) in new[] { (entry, clubBuilt.Record!), (Cell + 1, cellBuilt.Record!) })
        {
            scenes.SetStored(slot, record);
            if (record.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
            { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, record.Length); scenes.SetStored(0, size); }
        }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        log.Add($"Stella Entities: scene {Scene} (grid {gridNumber}, Rick's bar's block library {raw[0]}): a round hall {Radius * 2:0} cells across, the corridor and the " +
                $"lift's foot -- {floors} floor cells, {northWalls} of north wall, {westWalls} of west wall, {rims} of low wall, {rings} invisible cells round the hall's edge, {stageCells} under the stage and catwalk, {counterCells} along the bar, {barEnds} shutting its ends; Baldino's cell (scene {Cell}, its floor whole): " +
                $"the hidden lift in its corner by the door (cells x 32-33, z 31-32): Action on the spot and down onto the club's platform, and back up onto the spot");
        return log;
    }

    // A piece of the room drawn into the backdrop as bricks are (OBJ_BACKGROUND, and NO_PRE_CLIP: a big body whose middle may be off the
    // screen while it is on it), no shadow
    private const uint ObjBackground = 1u << 13, NoPreClip = 1u << 19;
    private static SceneActorModel Backdrop(int generic, int x, int y, int z)
    {
        var a = Citadel.CitadelProps.Actor(generic, x, y, z);
        a.Flags |= ObjBackground | NoPreClip;
        a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 };
        return a;
    }

    // A piece of furniture, drawn among the people
    private static SceneActorModel Furniture(int generic, int x, int y, int z)
    {
        var a = Citadel.CitadelProps.Actor(generic, x, y, z);
        a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 };
        return a;
    }

    // A lift's part: a carrier on its own track, run from the scene's start (an actor's track starts by itself only when it moves by it)
    private static SceneActorModel LiftActor(int generic, int x, int y, int z)
    {
        var a = Citadel.CitadelProps.Actor(generic, x, y, z);
        a.Flags |= RaceTrackScenes.CarrierFlag;
        a.Move = 3;
        a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 };
        return a;
    }
}
