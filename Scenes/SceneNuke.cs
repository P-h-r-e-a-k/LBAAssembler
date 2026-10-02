using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Lba1.Runtime;
using LBAAssembler.Terrain;

namespace LBAAssembler.Scenes;

// Build > Nuke: everything in a scene goes, and a flat empty scene is left. Planned first (what goes, and what else it touches), then
// written as one save and one undo step ("Nuke scene N"): the scene's record with, as the scene has them, its grid (LBA1: LBA_GRI.HQR;
// LBA2 interiors: LBA_BKG.HQR) or its island cube (LBA2 outside scenes: the .ILE's records that change).
// An island cube can be several scenes -- the same place at other points of the story (Desert cube 8,9 is scenes 61 and 201) -- and the
// 3D view draws one of them, not always the one the editor names: they all share the levelled ground, so they are all emptied.
//
// What is left, and why:
//   - Twinsen, where he stood, on the new ground: a scene can't be without its hero. On an island, the engine's Zoe stand-in in slot 1
//     too (entity 14 at 0,0: the engine treats that slot specially -- RaceTrackScenes, SendellWell keep it). The scripts of what is kept
//     are emptied (END): they refer to actors and points that are gone, and the game stuck or blanked on such scripts (the race track
//     build, docs/LBA2_DESERT_RACE_TRACK_BUILD.md).
//   - The exits: the zones that lead to other scenes (type 0). Without them the scene would be a trap the game can't leave, and on an
//     island every edge of the cube an invisible wall.
//   - The ground, flat: a grid scene gets one layer of its own most used floor block under every column it had anything in (so it keeps
//     its size); an island scene's cube has its objects removed and its land levelled to its usual height, eased into the neighbouring
//     cubes over a few cells, painted with its most common flat ground, its water and lava drained, and its light baked again.
internal sealed class SceneNuke
{
    private const int FeatherCells = 4;          // an island cube's level eases into its neighbours over this many cells

    public SceneGame Game { get; }
    public int Scene { get; }
    public IReadOnlyList<int> Scenes => changes.Select(c => c.Scene).ToList();
    public string Where { get; private set; } = "";
    public int Actors { get; private set; }
    public int Zones { get; private set; }
    public int Exits { get; private set; }
    public int TrackPoints { get; private set; }
    public int Columns { get; private set; }     // grid columns that had something in them (now floor)
    public int Decors { get; private set; }      // island objects removed
    public int Level { get; private set; }       // the island cube's new ground height
    public List<string> Warnings { get; } = new();

    private readonly string directory;
    private readonly List<SceneChange> changes = new();
    private readonly List<HqrEntryStore.Edit> edits = new();

    private SceneNuke(SceneGame game, string directory, int scene)
    {
        Game = game; this.directory = directory; Scene = scene;
    }

    // ---- planning -----------------------------------------------------------------------------------------------------------------

    public static SceneNuke ForLba1(string directory, int scene)
    {
        var nuke = new SceneNuke(SceneGame.Lba1, directory, scene);
        var store = new SceneStore(SceneGame.Lba1, directory);
        var old = store.Load(scene);
        var grid = store.LoadGrid(scene);
        var library = store.LoadLibrary(scene);
        var floor = PickFloor(grid, library)
            ?? (Lba1BlankScene.PickFloor(grid, library) is { } single ? new Floor(single.Block, 1, 1, 0) : null)
            ?? throw new SceneEditException($"Scene {scene}'s block library has no plain floor block to leave behind.");
        var model = nuke.Empty(old, keepZoe: false);
        var (flat, floored) = FlatGrid(grid, library, floor);
        nuke.Columns = floored.Count;
        StandOnFloor(model.Hero, floored, floor.Layer);
        nuke.changes.Add(new SceneChange(scene, model, flat));
        nuke.Where = $"LBA1 scene {scene}";
        return nuke;
    }

    // An LBA2 scene: an interior (its grid) or a scene of an island (`islandFile`, the .ILE the scene's island is drawn from).
    public static SceneNuke ForLba2(string directory, int scene, string? islandFile)
    {
        var nuke = new SceneNuke(SceneGame.Lba2, directory, scene);
        var store = new SceneStore(SceneGame.Lba2, directory);
        var old = store.Load(scene);
        if (old.CubeMode == 0) nuke.PlanInterior(old, new Lba2GridBackend(directory));
        else nuke.PlanIsland(store, old, islandFile ?? throw new SceneEditException($"Scene {scene} is on an island, and no island file is open for it."));
        return nuke;
    }

    private void PlanInterior(SceneModel old, Lba2GridBackend backend)
    {
        var gridId = backend.GridOfScene(Scene) ?? throw new SceneEditException($"Scene {Scene} has no interior grid.");
        var grid = backend.LoadGrid(gridId);
        var library = backend.LoadLibrary(gridId);
        var floor = PickFloor(grid, library)
            ?? (Lba2BlankScene.PickFloor(grid, library) is { } single ? new Floor(single.Block, 1, 1, single.Layer) : null)
            ?? throw new SceneEditException($"The block library of scene {Scene}'s interior has no solid floor block to leave behind.");
        var model = Empty(old, keepZoe: false);
        var (flat, floored) = FlatGrid(grid, library, floor);
        Columns = floored.Count;
        StandOnFloor(model.Hero, floored, floor.Layer);
        changes.Add(new SceneChange(Scene, model));
        edits.Add(new HqrEntryStore.Edit("LBA_BKG.HQR", backend.GridEntry(gridId), Lba2GridBackend.FromLba1Shape(backend.RawGrid(gridId), flat)));
        var store = new SceneStore(SceneGame.Lba2, directory);
        var sharing = backend.ScenesOfGrid(gridId).Where(s => s != Scene && IsInterior(store, s)).ToList();
        if (sharing.Count > 0)
            Warnings.Add($"Its interior map (grid {gridId}) is also scene{(sharing.Count > 1 ? "s" : "")} {string.Join(", ", sharing)}'s: {(sharing.Count > 1 ? "they lose" : "it loses")} its buildings and furniture too.");
        Where = $"LBA2 interior scene {Scene} (grid {gridId})";
    }

    private static bool IsInterior(SceneStore store, int scene) => TryLoad(store, scene) is { CubeMode: 0 };

    private static SceneModel? TryLoad(SceneStore store, int scene)
    {
        // (the table runs on past the scenes the game has: a number with no scene, or no readable one, isn't one)
        try { return store.Load(scene); }
        catch (Exception e) when (e is InvalidDataException or IOException or SceneFormatException or ArgumentException or OverflowException or IndexOutOfRangeException) { return null; }
    }

    private void PlanIsland(SceneStore store, SceneModel old, string islandFile)
    {
        var path = Path.Combine(directory, islandFile);
        var island = IslandFile.Load(path);
        int cubeX = old.CubeX, cubeZ = old.CubeY;
        var cube = island.CubeAt(cubeX, cubeZ) ?? throw new SceneEditException($"{islandFile} has no cube ({cubeX}, {cubeZ}) for scene {Scene}.");
        // every scene of this cube: this one first, then the same place at the story's other points
        var models = new List<(int Scene, SceneModel Model)> { (Scene, Empty(old, keepZoe: true)) };
        for (var s = 0; s < store.SceneCount; s++)
            if (s != Scene && TryLoad(store, s) is { CubeMode: not 0 } other && other.Island == old.Island && other.CubeX == cubeX && other.CubeY == cubeZ)
                models.Add((s, Empty(other, keepZoe: true)));
        var also = models.Skip(1).Select(m => m.Scene).ToList();
        var name = Path.GetFileNameWithoutExtension(islandFile);
        Where = also.Count == 0 ? $"LBA2 scene {Scene} ({name}, cube {cubeX},{cubeZ})" : $"LBA2 scenes {Scene} and {string.Join(", ", also)} ({name}, cube {cubeX},{cubeZ})";
        if (also.Count > 0)
            Warnings.Add($"The cube is also scene{(also.Count > 1 ? "s" : "")} {string.Join(", ", also)} (the same place at another point of the story): emptied too, since {(also.Count > 1 ? "they share" : "it shares")} the ground.");
        var shown = island.CellsOf(cube.Id);
        if (shown.Count > 1)
            Warnings.Add($"The island shows this cube in {shown.Count} places: they all change.");

        Decors = cube.Decors.Count;
        cube.Decors.Clear();

        int gx0 = cubeX * IslandCube.Cells, gz0 = cubeZ * IslandCube.Cells;
        // the sea: ground at sea level that the open water reaches -- from the cube's edge, through ground no higher. (Ground as low inside
        // the land -- a pit, a well, the sewers' way in -- is land: Citadel's sewer hole was left as a dark square on the new plain.)
        const int V = IslandCube.Vertices;
        var sea = new bool[V * V];
        var queue = new Queue<(int X, int Z)>();
        for (var i = 0; i < V; i++)
            foreach (var (x, z) in new[] { (i, 0), (i, V - 1), (0, i), (V - 1, i) })
                if (cube.Height(x, z) <= 0 && !sea[z * V + x]) { sea[z * V + x] = true; queue.Enqueue((x, z)); }
        while (queue.Count > 0)
        {
            var (x, z) = queue.Dequeue();
            foreach (var (nx, nz) in new[] { (x + 1, z), (x - 1, z), (x, z + 1), (x, z - 1) })
                if (nx >= 0 && nz >= 0 && nx < V && nz < V && !sea[nz * V + nx] && cube.Height(nx, nz) <= 0) { sea[nz * V + nx] = true; queue.Enqueue((nx, nz)); }
        }
        bool Land(int x, int z) => !sea[z * V + x];
        // the land: the cube's cells with a corner on land, and the level it is brought to (the middle of its heights)
        var land = new List<(int Gx, int Gz)>();
        for (var z = 0; z < IslandCube.Cells; z++)
        for (var x = 0; x < IslandCube.Cells; x++)
            if (Land(x, z) || Land(x + 1, z) || Land(x, z + 1) || Land(x + 1, z + 1))
                land.Add((gx0 + x, gz0 + z));
        var heights = new List<short>();
        for (var z = 0; z < V; z++)
        for (var x = 0; x < V; x++)
            if (Land(x, z) && cube.Height(x, z) > 0) heights.Add(cube.Height(x, z));
        if (land.Count == 0 || heights.Count == 0)
        {
            Warnings.Add("The cube is all sea: its objects go, the sea stays.");
            Level = 0;
        }
        else
        {
            heights.Sort();
            Level = heights[heights.Count / 2];
            var ground = MostCommonFlatGround(island, cube, land);
            // levelled: the cube's land, then the neighbours' land eased towards it over FeatherCells
            var touched = new List<(int, int)>();
            for (var gz = Math.Max(0, gz0 - FeatherCells); gz <= Math.Min(IslandFile.GridSize, gz0 + IslandCube.Cells + FeatherCells); gz++)
            for (var gx = Math.Max(0, gx0 - FeatherCells); gx <= Math.Min(IslandFile.GridSize, gx0 + IslandCube.Cells + FeatherCells); gx++)
            {
                if (island.HeightAt(gx, gz) is not { } h) continue;
                var outside = Math.Max(Math.Max(gx0 - gx, gx - (gx0 + IslandCube.Cells)), Math.Max(gz0 - gz, gz - (gz0 + IslandCube.Cells)));
                // (in the cube: its land; beyond it, the neighbours' ground above the sea)
                if (outside <= 0 ? !Land(gx - gx0, gz - gz0) : h <= 0) continue;
                var weight = outside <= 0 ? 1 : 1 - (double)outside / (FeatherCells + 1);
                weight = weight * weight * (3 - 2 * weight);
                var to = (int)Math.Round(h + (Level - h) * weight);
                if (to != h) island.SetHeight(gx, gz, to);
                touched.Add((gx, gz));
            }
            // the ground: one plain walkable ground everywhere on the land, no water, lava or blocking rock left
            var region = new CellsRegion(land);
            if (ground is not null) IslandGround.Paint(island, region, ground, PolygonFields.Texture);
            IslandGround.PaintGameCode(island, region, 0);
            foreach (var (gx, gz) in land)
                for (var half = 0; half < 2; half++)
                    cube.SetPolygon(gx - gx0, gz - gz0, half, new IslandPolygon(cube.Polygon(gx - gx0, gz - gz0, half)).With(col: false).Raw);
            // the light, as the cube's own light falls on flat ground with nothing standing on it
            var bake = BakeOptions.For(cube);
            bake.TerrainShadows = true;
            IslandBake.Bake(island, new CellsRegion(touched), bake);

            // Twinsen on the new ground (or, where he stood in the sea, in the middle of the land)
            foreach (var (_, model) in models)
            {
                var hero = model.Hero;
                double wx = cubeX * (double)IslandFile.CubeSize + hero.X, wz = cubeZ * (double)IslandFile.CubeSize + hero.Z;
                if ((IslandOps.Altitude(island, wx, wz) ?? 0) <= 0)
                {
                    var (mx, mz) = land.OrderBy(c => Math.Abs(c.Gx - (gx0 + 32)) + Math.Abs(c.Gz - (gz0 + 32))).First();
                    hero.X = (mx - gx0) * IslandFile.CellSize + IslandFile.CellSize / 2; hero.Z = (mz - gz0) * IslandFile.CellSize + IslandFile.CellSize / 2;
                    wx = cubeX * (double)IslandFile.CubeSize + hero.X; wz = cubeZ * (double)IslandFile.CubeSize + hero.Z;
                }
                hero.Y = (int)Math.Round(IslandOps.Altitude(island, wx, wz) ?? Level);
            }
        }
        changes.AddRange(models.Select(m => new SceneChange(m.Scene, m.Model)));

        // the island's records that changed, as edits beside the scene's
        var before = HqrFile.Parse(File.ReadAllBytes(path));
        var after = HqrFile.Parse(island.ToBytes());
        for (var i = 0; i < after.Count; i++)
        {
            var a = i < before.Count && !before.IsEmpty(i) ? before.Read(i) : null;
            var b = after.IsEmpty(i) ? null : after.Read(i);
            if (b is null || a is not null && a.AsSpan().SequenceEqual(b)) continue;
            edits.Add(new HqrEntryStore.Edit(islandFile, i, b));
        }
    }

    // The ground most of the cube's flat land wears (a triangle to paint with), or null when it has no flat land to go by.
    private static IslandGround.Sample? MostCommonFlatGround(IslandFile island, IslandCube cube, List<(int Gx, int Gz)> land)
    {
        var counts = new Dictionary<string, (IslandGround.Sample Sample, int Count)>();
        foreach (var (gx, gz) in land)
        {
            int x = gx % IslandCube.Cells, z = gz % IslandCube.Cells;
            var hs = new[] { cube.Height(x, z), cube.Height(x + 1, z), cube.Height(x, z + 1), cube.Height(x + 1, z + 1) };
            if (hs.Max() - hs.Min() > 160) continue;                                        // (flat: under about 17 degrees)
            if (IslandGround.Pick(island, gx, gz, 0) is not { Texture: not null } sample) continue;
            var p = sample.Polygon;
            if (p.CodeJeu != 0 || p.Col || p.TexFlag == 0 && p.PolyFlag == 0) continue;
            var key = $"{p.TexFlag}/{p.PolyFlag}/{p.Bank}/{p.SampleStep}/{string.Join(",", sample.Texture!)}";
            counts[key] = counts.TryGetValue(key, out var c) ? (c.Sample, c.Count + 1) : (sample, 1);
        }
        return counts.Count == 0 ? null : counts.Values.MaxBy(c => c.Count).Sample;
    }

    // The scene with everything gone but the hero (and on an island the Zoe stand-in), scripts emptied, and the exits.
    private SceneModel Empty(SceneModel old, bool keepZoe)
    {
        var scene = old.Clone();
        var keep = keepZoe && scene.Actors.Count > 1 && scene.Actors[1].Entity == 14 && scene.Actors[1].X == 0 && scene.Actors[1].Z == 0 ? 2 : 1;
        Actors += scene.Actors.Count - keep;
        // (all at once: one by one would trip over the scripts that point at each other -- Lba2BlankScene)
        scene.Actors.RemoveRange(keep, scene.Actors.Count - keep);
        foreach (var actor in scene.Actors) { actor.Life = new byte[] { 0 }; actor.Track = new byte[] { 0 }; }
        var exits = scene.Zones.Where(z => z.Type == 0).ToList();
        Zones += scene.Zones.Count - exits.Count;
        Exits += exits.Count;
        scene.Zones.Clear();
        scene.Zones.AddRange(exits);
        TrackPoints += scene.TrackPoints.Count;
        scene.TrackPoints.Clear();
        return scene;
    }

    // A floor block: Dx x 1 x Dz cells, laid at Layer.
    private sealed record Floor(int Block, int Dx, int Dz, int Layer);

    // The floor the scene is walked on most: of the blocks one layer tall, solid in every cell and plain ground (no water or other special
    // code), the one with the most cells that have nothing on them, in the lowest layers -- whatever its size: a room's tiles are often a
    // block of 2 x 2 or more (the Desert bar's are), and the blocks of one cell are then the stones under them, not what is seen.
    private static Floor? PickFloor(byte[] grid, byte[] library)
    {
        var cube = new Lba1Cube(grid, library);
        var counts = new Dictionary<(int Block, int Layer), int>();
        for (var y = 0; y < 4; y++)
        for (var z = 0; z < Lba1Cube.SizeZ; z++)
        for (var x = 0; x < Lba1Cube.SizeX; x++)
        {
            var (block, _) = cube.Cell(x, y, z);
            if (block == 0 || cube.Cell(x, y + 1, z).Block != 0) continue;
            counts[(block, y)] = counts.GetValueOrDefault((block, y)) + 1;
        }
        foreach (var ((block, layer), _) in counts.OrderByDescending(c => c.Value))
        {
            if (GridPaint.Info(library, block) is not { Dy: 1, Dx: > 0 and <= 8, Dz: > 0 and <= 8 } info) continue;
            if (Enumerable.Range(0, cube.ExtentOf(block)).Any(p => cube.ShapeOf(block, p) != 1)) continue;
            var code = cube.GameCodeOf(block);
            if (code != 0xF0 && (code & 0xF0) == 0xF0) continue;          // (0xF1..0xFF: water and other special ground -- WorldCodeBrick)
            return new Floor(block, info.Dx, info.Dz, layer);
        }
        return null;
    }

    // An empty grid with one layer of the floor under every column the old grid had anything in, and the holes those enclose (a pit, a
    // well: a hole in a flat plain is a trap) -- or, when it had nothing, the blank scenes' 32 x 32 in the middle -- its cells numbered as
    // if blocks were laid side by side from the grid's corner. Returns the grid and the floored columns.
    private static (byte[] Grid, List<(int X, int Z)> Floored) FlatGrid(byte[] grid, byte[] library, Floor floor)
    {
        const int N = Lba1Cube.SizeX;
        var cube = new Lba1Cube(grid, library);
        var used = new bool[N * N];
        for (var z = 0; z < N; z++)
        for (var x = 0; x < N; x++)
            for (var y = 0; y < Lba1Cube.SizeY; y++)
                if (cube.Cell(x, y, z).Block != 0) { used[z * N + x] = true; break; }
        // the outside: the empty columns the grid's edge reaches through empty columns; every other column is floor
        var outside = new bool[N * N];
        var queue = new Queue<int>();
        for (var i = 0; i < N; i++)
            foreach (var c in new[] { i, (N - 1) * N + i, i * N, i * N + N - 1 })
                if (!used[c] && !outside[c]) { outside[c] = true; queue.Enqueue(c); }
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            int x = c % N, z = c / N;
            foreach (var (nx, nz) in new[] { (x + 1, z), (x - 1, z), (x, z + 1), (x, z - 1) })
                if (nx >= 0 && nz >= 0 && nx < N && nz < N && !used[nz * N + nx] && !outside[nz * N + nx]) { outside[nz * N + nx] = true; queue.Enqueue(nz * N + nx); }
        }
        var floored = new List<(int X, int Z)>();
        if (used.Any(u => u))
            for (var z = 0; z < N; z++)
            for (var x = 0; x < N; x++)
                if (!outside[z * N + x]) floored.Add((x, z));
        if (floored.Count == 0)
            for (var z = Lba1BlankScene.FloorFirst; z <= Lba1BlankScene.FloorLast; z++)
            for (var x = Lba1BlankScene.FloorFirst; x <= Lba1BlankScene.FloorLast; x++)
                floored.Add((x, z));
        var cells = floored.Select(c => new Lba1GridCell(c.X, floor.Layer, c.Z, floor.Block, c.X % floor.Dx + floor.Dx * (c.Z % floor.Dz))).ToList();
        return (Lba1GridEdit.SetCells(Lba1BlankScene.EmptyGrid(), cells), floored);
    }

    // The hero on the floor: where he stood when there is floor there, else on the nearest floored cell (with floor all round it, when
    // there is such a cell, so that he isn't on the brink). The engines find the cell under a point as (x + 256) / 512 (GRILLE.C,
    // GRILLE_A.CPP): cell N is centred on N * 512, not N * 512 + 256 -- placed by the other rule, Twinsen fell through LBA1 scene 3.
    private static void StandOnFloor(SceneActorModel hero, List<(int X, int Z)> floored, int layer)
    {
        int cx = (hero.X + 256) / 512, cz = (hero.Z + 256) / 512;
        var set = floored.ToHashSet();
        if (!set.Contains((cx, cz)))
        {
            bool Inner((int X, int Z) c) => set.Contains((c.X + 1, c.Z)) && set.Contains((c.X - 1, c.Z)) && set.Contains((c.X, c.Z + 1)) && set.Contains((c.X, c.Z - 1));
            var choices = floored.Where(Inner).ToList();
            var (fx, fz) = (choices.Count > 0 ? choices : floored).MinBy(c => (c.X - cx) * (c.X - cx) + (c.Z - cz) * (c.Z - cz));
            hero.X = fx * 512; hero.Z = fz * 512;
        }
        hero.Y = (layer + 1) * 256;
    }

    // ---- writing ------------------------------------------------------------------------------------------------------------------

    // The nuke in a sentence or two, for the question before it and the status after.
    public string Summary
    {
        get
        {
            var parts = new List<string>();
            if (Actors > 0) parts.Add($"{Actors} actor{(Actors == 1 ? "" : "s")}");
            if (Zones > 0) parts.Add($"{Zones} zone{(Zones == 1 ? "" : "s")}");
            if (TrackPoints > 0) parts.Add($"{TrackPoints} track point{(TrackPoints == 1 ? "" : "s")}");
            if (Columns > 0) parts.Add("every building, wall and piece of furniture");
            if (Decors > 0) parts.Add($"{Decors} building{(Decors == 1 ? "" : "s")} and object{(Decors == 1 ? "" : "s")}");
            var gone = parts.Count == 0 ? "nothing but the ground" : string.Join(", ", parts.Take(parts.Count - 1)) + (parts.Count > 1 ? " and " : "") + parts[^1];
            return $"{gone}";
        }
    }

    // Writes it: one transaction, one undo step.
    public void Commit()
    {
        var store = new SceneStore(Game, directory);
        var scenes = Scenes;
        var description = scenes.Count == 1 ? $"Nuke scene {Scene}" : $"Nuke scenes {string.Join(", ", scenes)}";
        store.SaveMany(changes, allowErrors: false, description: description, extraEdits: edits);
    }

    // Whether this nuke wrote island records (the views of the island are to be read again).
    public bool ChangesIsland => edits.Any(e => e.RelativePath.EndsWith(".ILE", StringComparison.OrdinalIgnoreCase));

    // A region of whole cells, or vertices (their top-left vertex, weight 1).
    private sealed class CellsRegion(IEnumerable<(int Gx, int Gz)> cells) : IslandRegion
    {
        private readonly List<(int Gx, int Gz)> list = cells.ToList();
        public override IEnumerable<(int Gx, int Gz, double Weight)> Vertices(IslandFile island) => list.Select(c => (c.Gx, c.Gz, 1.0));
        public override (double Gx, double Gz) Center => list.Count == 0 ? (0, 0) : (list.Average(c => c.Gx), list.Average(c => c.Gz));
    }
}
