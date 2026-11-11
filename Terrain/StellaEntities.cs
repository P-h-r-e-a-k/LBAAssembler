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
//     (cells x 31-32, z 32-33: the corner the user confirmed) -- the hidden lift, down into the new scene's lift foot; and from there a
//     door zone back up, beside where he arrives.
// Still to come (the design's next parts): the stage round the reactor's column, its poles, the catwalk, the bar, the office, backstage,
// the sign; the bouncers and the performers.
internal static class StellaEntities
{
    public const int Scene = 390, Cell = 54, RickBar = 136, MoonIsland = 3;
    // the hall: round, its middle and radius (cells); the corridor (x, z); the lift's foot
    private const double CentreX = 24, CentreZ = 32, Radius = 20;
    private const int CorridorX0 = 43, CorridorX1 = 56, CorridorZ0 = 30, CorridorZ1 = 34;
    private const int FootX0 = 55, FootX1 = 60, FootZ0 = 26, FootZ1 = 34;
    // the floor's layer (Rick's), its tile (3 x 1 x 3) and where its 3 x 3 repeats from
    private const int FloorLayer = 0, Tile = 67;
    // Rick's walls: his north wall's columns (x 38-40, z 12) and his west wall's (x 17, z 36-47), each layer's repeat; the low wall's block
    private static readonly (int X, int Z) North = (38, 12), West = (17, 36);
    private const int WestUpper = 18, LowWall = 19;
    // where Twinsen arrives in the new scene from the cell (on the lift's foot), and the door back up beside it (cells)
    private const int ArriveX = 57, ArriveZ = 31, BackX0 = 59, BackX1 = 60, BackZ0 = 27, BackZ1 = 28;
    // the cell's hidden lift (world units: cells x 31-32, z 32-33, on its floor 2048) and where Twinsen comes back up, beside it
    private const int LiftX0 = 31 * 512, LiftX1 = 33 * 512 - 1, LiftZ0 = 32 * 512, LiftZ1 = 34 * 512 - 1, CellFloor = 2048;
    private static readonly (int X, int Y, int Z) BackUp = (33 * 512 + 256, CellFloor, 32 * 512 + 256);

    private const int Size = 64, Height = 25;

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
        int floors = 0, northWalls = 0, westWalls = 0, rims = 0;
        for (var z = 0; z < Size; z++)
        for (var x = 0; x < Size; x++)
        {
            if (Floor(x, z))
            {
                Put(x, FloorLayer, z, Tile, Mod(x - 1, 3) + 3 * Mod(z - 1, 3));
                floors++;
                continue;
            }
            // a wall where the floor's beside it: the north and west sides Rick's walls, the south and east a low wall
            if (Floor(x, z + 1) || (Floor(x + 1, z + 1) && !Floor(x + 1, z)))
            {
                Column(x, z, y => (North.X + Mod(x - North.X, 3), North.Z));
                northWalls++;
            }
            else if (Floor(x + 1, z))
            {
                Column(x, z, y => (West.X, West.Z + Mod(z - West.Z, y >= WestUpper ? 3 : 4)));
                westWalls++;
            }
            else if (Floor(x, z - 1) || Floor(x - 1, z) || Floor(x - 1, z - 1) || Floor(x + 1, z - 1) || Floor(x - 1, z + 1))
            {
                Put(x, FloorLayer + 1, z, LowWall, Mod(x + z, 2));
                rims++;
            }
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
        bkg.SetStored(header.Brk + header.MaxBrk, table);

        // the scene: Rick's bar's header (its light, its music), the Moon's, Twinsen on the lift's foot, the door back up beside it
        var cell = store.Load(Cell);
        var template = cell.Zones.FirstOrDefault(zn => zn.Type == 0);
        if (template is null) { log.Add("Stella Entities left out: Baldino's cell has no door zone to copy"); return log; }
        var model = store.Load(RickBar).Clone();
        model.Island = MoonIsland;
        model.Actors.RemoveRange(2, model.Actors.Count - 2);
        model.Zones.Clear();
        var floorY = (FloorLayer + 1) * 256;
        var hero = model.Hero;
        (hero.X, hero.Y, hero.Z) = (ArriveX * 512 + 256, floorY, ArriveZ * 512 + 256);
        hero.Life = new byte[] { 0 }; hero.Track = new byte[] { 0 };
        var up = template.Clone();
        up.Num = Cell;
        (up.X0, up.X1, up.Z0, up.Z1, up.Y0, up.Y1) = (BackX0 * 512, (BackX1 + 1) * 512 - 1, BackZ0 * 512, (BackZ1 + 1) * 512 - 1, floorY, floorY + 1536);
        (up.Info[0], up.Info[1], up.Info[2]) = BackUp;
        model.Zones.Add(up);
        var entry = Scene + 1;
        while (scenes.Count <= entry) scenes.Slots.Add(new HqrFile.Slot());
        var record = SceneSerializer.Write(model);
        scenes.SetStored(entry, record);
        if (record.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
        { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, record.Length); scenes.SetStored(0, size); }
        // ... and the cell's hidden lift down to it
        var down = template.Clone();
        down.Num = Scene;
        (down.X0, down.X1, down.Z0, down.Z1, down.Y0, down.Y1) = (LiftX0, LiftX1, LiftZ0, LiftZ1, CellFloor, CellFloor + 1536);
        (down.Info[0], down.Info[1], down.Info[2]) = (ArriveX * 512 + 256, floorY, ArriveZ * 512 + 256);
        cell.Zones.Add(down);
        var cellRecord = SceneSerializer.Write(cell);
        scenes.SetStored(Cell + 1, cellRecord);
        if (cellRecord.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
        { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, cellRecord.Length); scenes.SetStored(0, size); }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        log.Add($"Stella Entities: scene {Scene} (grid {gridNumber}, Rick's bar's block library {raw[0]}): a round hall {Radius * 2:0} cells across, the corridor and the " +
                $"lift's foot -- {floors} floor cells, {northWalls} of north wall, {westWalls} of west wall, {rims} of low wall; Baldino's cell (scene {Cell}): the hidden " +
                $"lift in its corner by the door (cells x 31-32, z 32-33) down to it, and the way back up");
        return log;
    }
}
