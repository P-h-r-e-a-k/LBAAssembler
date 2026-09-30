using LBAAssembler;
using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// Sendell's Well: island number 1 in the engine's island list ("sendell"), an outside island the game was to have and that was cut --
// no SENDELL.ILE ships and no scene has island 1. A proof that the game takes a new one.
//   sendell build <pristine folder> <game folder>            SENDELL.ILE/OBL, island 1's sky, palette and holomap picture, scene 222, and
//                                                            a way there and back from Citadel Island's Sendell's sign (the game folder's
//                                                            SCENE.HQR, RESS.HQR and HOLOMAP.HQR must be the originals)
//   sendell tiles <pristine folder> [ISLAND]                  the ground atlas rectangles an island uses most, and their colours
//   sendell inspect <pristine folder> <cube x> <cube z>     heights, ground classes and decors of one cube of CITADEL.ILE
internal static class SendellIsland
{
    public static int Run(string[] args)
    {
        if (args.Length > 1 && args[1] == "build") return Build(args[2], args[3]);
        if (args.Length > 1 && args[1] == "tiles") return Tiles(args[2], args.Length > 3 ? args[3] : "CITABAU");
        return args.Length > 1 && args[1] == "inspect" ? Inspect(args[2], int.Parse(args[3]), int.Parse(args[4])) : Usage();
    }

    // The atlas rectangles an island's textured ground triangles use most, with their colour and class.
    private static int Tiles(string pristine, string name)
    {
        var island = IslandFile.Load(Path.Combine(pristine, name + ".ILE"));
        var palette = IslandMapRenderer.LoadPalette(pristine, name);
        var counts = new Dictionary<(int X, int Y, int W, int H), (int N, long R, long G, long B)>();
        foreach (var cube in island.Cubes.Values)
            for (var z = 0; z < 64; z++)
            for (var x = 0; x < 64; x++)
            for (var half = 0; half < 2; half++)
            {
                var p = new IslandPolygon(cube.Polygon(x, z, half));
                if (p.TexFlag == 0 || p.CodeJeu != 0) continue;
                var i = p.TextureIndex;
                if (i * 6 + 6 > cube.TextureDefs.Length) continue;
                var us = new[] { cube.TextureDefs[i * 6], cube.TextureDefs[i * 6 + 2], cube.TextureDefs[i * 6 + 4] };
                var vs = new[] { cube.TextureDefs[i * 6 + 1], cube.TextureDefs[i * 6 + 3], cube.TextureDefs[i * 6 + 5] };
                var rect = (us.Min() / 256, vs.Min() / 256, (us.Max() + 128) / 256 - us.Min() / 256, (vs.Max() + 128) / 256 - vs.Min() / 256);
                var c = TriangleColour(island, cube, x, z, half, palette) ?? (0, 0, 0);
                counts.TryGetValue(rect, out var e);
                counts[rect] = (e.N + 1, e.R + c.R, e.G + c.G, e.B + c.B);
            }
        foreach (var (rect, e) in counts.OrderByDescending(p => p.Value.N).Take(40))
        {
            var colour = ((int)(e.R / e.N), (int)(e.G / e.N), (int)(e.B / e.N));
            Console.WriteLine($"{rect.X,3},{rect.Y,3} {rect.W,2}x{rect.H,-2} used {e.N,6}  colour {colour}  {Class(colour)}");
        }
        return 0;
    }

    private static int Usage()
    {
        Console.WriteLine("sendell build <pristine folder> <game folder> | tiles <pristine folder> [ISLAND] | inspect <pristine folder> <cube x> <cube z>");
        return 1;
    }

    // ---- the island ----
    // One cube, at (7, 7) of its map, made from the fine-weather Citadel Island's (CITABAU: its texture atlas and palette, so the grass,
    // rock and paving are its own): the cube of the Dome of the Slate -- a round rock in the sea west of the island -- reshaped into a
    // round island with a beach, grassy slopes, a paved rim round a stone well, and water at the bottom of the well.
    public const int CubeX = 7, CubeZ = 7;
    public const int Scene = 222;                   // the first scene number past the retail game's (0..221)
    private const int SourceCubeX = 6, SourceCubeZ = 8, SourceScene = 44;
    // Citadel Island's Sendell's sign: scene 47, cube (8, 7), the circle's middle (cube-local world units)
    private const int SignScene = 47, SignCubeX = 8, SignCubeZ = 7, SignX = 7168, SignZ = 16000;
    private const int SkyEntry = 12, PaletteEntry = 28;           // RESS.HQR: RESS_SKYSEA1 and RESS_XPL1, island 1's own
    private const int CitabauSky = 26, CitabauPalette = 42;       // RESS_SKYSEA00 and RESS_XPL00, the fine-weather Citadel's

    // the atlas tiles (32 x 32 pixels, CITABAU's ground atlas), one to a cell
    private static readonly (int X, int Y) Sand = (0, 32), Grass = (32, 96), Flowers = (192, 0), Rock = (128, 32), Cliff = (160, 32),
        Cobbles = (0, 0), Bricks = (128, 0), Water = (32, 0);

    // The shape, in cells from the cube's middle: the coast (with a little wave to it), the beach, the slope up to the rim, the well.
    private const double Coast = 26, BeachTop = 22.5, Plateau = 11, RimOut = 6.5, RimIn = 4.5, WellBottom = 3.5;
    private const int BeachHeight = 350, PlateauHeight = 1900, RimHeight = 2250, BottomHeight = 120;

    private static double Wobble(double angle) => 1 + 0.07 * Math.Sin(3 * angle + 0.4) + 0.045 * Math.Sin(5 * angle + 1.9) + 0.03 * Math.Sin(8 * angle);

    private static double Smooth(double t) { t = Math.Clamp(t, 0, 1); return t * t * (3 - 2 * t); }

    // The ground's height at a vertex, cells from the middle (the coast and the slope wobble; the rim and the well are round).
    private static double HeightAt(double dx, double dz)
    {
        var r = Math.Sqrt(dx * dx + dz * dz);
        var w = Wobble(Math.Atan2(dz, dx));
        var coast = Coast * w; var beachTop = BeachTop * w;
        if (r >= coast) return 0;
        if (r >= beachTop) return BeachHeight * Smooth((coast - r) / (coast - beachTop));
        if (r >= Plateau) return BeachHeight + (PlateauHeight - BeachHeight) * Smooth((beachTop - r) / (beachTop - Plateau));
        if (r >= RimOut) return PlateauHeight;
        if (r >= RimIn) return RimHeight;
        if (r >= WellBottom) return BottomHeight + (RimHeight - BottomHeight) * Smooth((r - WellBottom) / (RimIn - WellBottom));
        return BottomHeight;
    }

    // What a cell is painted with, from where its middle is and how steep it is.
    private static ((int X, int Y) Tile, int CodeJeu) Paint(double dx, double dz, double slopeDegrees)
    {
        var r = Math.Sqrt(dx * dx + dz * dz);
        var w = Wobble(Math.Atan2(dz, dx));
        if (r < WellBottom) return (Water, 1);
        if (r < RimIn) return (Bricks, 0);
        if (r < RimOut) return (Cobbles, 0);
        if (r < RimOut + 1.5) return (Flowers, 0);
        if (r >= BeachTop * w - 0.5) return (Sand, 0);
        if (slopeDegrees > 38) return (Cliff, 0);
        if (slopeDegrees > 26) return (Rock, 0);
        return (Grass, 0);
    }

    // A region of whole cells (their top-left vertices, weight 1).
    private sealed class CellsRegion(IEnumerable<(int Gx, int Gz)> cells) : IslandRegion
    {
        private readonly List<(int Gx, int Gz)> list = cells.ToList();
        public override IEnumerable<(int Gx, int Gz, double Weight)> Vertices(IslandFile island) => list.Select(c => (c.Gx, c.Gz, 1.0));
        public override (double Gx, double Gz) Center => list.Count == 0 ? (0, 0) : (list.Average(c => c.Gx), list.Average(c => c.Gz));
    }

    private static int Build(string pristine, string game)
    {
        var log = new List<string>();
        // the island file: CITABAU's, its map down to the one cube
        var island = IslandFile.Load(Path.Combine(pristine, "CITABAU.ILE"));
        var cube = island.CubeAt(SourceCubeX, SourceCubeZ) ?? throw new InvalidDataException("CITABAU.ILE has no cube (6, 8)");
        var flag = island.Map[SourceCubeZ * IslandFile.MapSize + SourceCubeX] & 0x80;
        Array.Clear(island.Map);
        island.Map[CubeZ * IslandFile.MapSize + CubeX] = (byte)(cube.Id | flag);
        cube.Decors.Clear();

        // the ground's heights
        const int mid = 32;
        for (var z = 0; z <= 64; z++)
        for (var x = 0; x <= 64; x++)
            cube.Heights[z * IslandCube.Vertices + x] = (short)Math.Round(HeightAt(x - mid, z - mid));

        // the cells: land (and the well's water) painted, the sea left as it was
        var gx0 = CubeX * IslandCube.Cells; var gz0 = CubeZ * IslandCube.Cells;
        var land = new List<(int, int)>();
        for (var z = 0; z < 64; z++)
        for (var x = 0; x < 64; x++)
        {
            var hs = new[] { cube.Height(x, z), cube.Height(x + 1, z), cube.Height(x, z + 1), cube.Height(x + 1, z + 1) };
            if (hs.All(h => h == 0)) continue;
            land.Add((gx0 + x, gz0 + z));
        }
        var all = new CellsRegion(land);
        IslandGround.OptimiseDiagonals(island, all);
        // the sea: the rest of the cube, as its own corner cell is (the dome's rock reached past the new coast in places)
        var seaCells = new List<(int, int)>();
        for (var z = 0; z < 64; z++) for (var x = 0; x < 64; x++) if (!land.Contains((gx0 + x, gz0 + z))) seaCells.Add((gx0 + x, gz0 + z));
        if (IslandGround.Pick(island, gx0, gz0, 0) is { } sea) IslandGround.Paint(island, new CellsRegion(seaCells), sea, PolygonFields.All);
        var groups = new Dictionary<((int, int) Tile, int CodeJeu), List<(int, int)>>();
        foreach (var (gx, gz) in land)
        {
            var x = gx - gx0; var z = gz - gz0;
            var hs = new[] { cube.Height(x, z), cube.Height(x + 1, z), cube.Height(x, z + 1), cube.Height(x + 1, z + 1) };
            var rise = Math.Max(Math.Abs(hs[0] - hs[3]), Math.Abs(hs[1] - hs[2])) / (512.0 * Math.Sqrt(2));
            var slope = Math.Atan(rise) * 180 / Math.PI;
            var key = Paint(x + 0.5 - mid, z + 0.5 - mid, slope);
            if (!groups.TryGetValue(key, out var list)) groups[key] = list = new();
            list.Add((gx, gz));
        }
        foreach (var ((tile, code), cells) in groups)
        {
            var region = new CellsRegion(cells);
            IslandGround.PaintTile(island, region, tile.Item1, tile.Item2, 32, 32);
            IslandGround.PaintGameCode(island, region, code);
            log.Add($"{cells.Count,5} cells: tile ({tile.Item1},{tile.Item2}){(code != 0 ? $", game code {code}" : "")}");
        }
        // the light, from the cube's own light direction, with the terrain's shadows (the well's walls shade its water)
        var bake = BakeOptions.For(cube);
        bake.TerrainShadows = true;
        var vertices = new List<(int, int)>();
        for (var z = 0; z <= 64; z++) for (var x = 0; x <= 64; x++) vertices.Add((gx0 + x, gz0 + z));
        IslandBake.Bake(island, new CellsRegion(vertices), bake);
        island.Save(Path.Combine(game, "SENDELL.ILE"));
        RaceTrackService.CopyWritable(Path.Combine(pristine, "CITABAU.OBL"), Path.Combine(game, "SENDELL.OBL"));
        log.Add($"SENDELL.ILE: CITABAU's atlas, one cube (id {cube.Id}) at ({CubeX},{CubeZ}), no decors; SENDELL.OBL a copy of CITABAU.OBL");

        // island 1's sky and palette in RESS.HQR: the sky slot is empty in the retail file (and the engine would load nothing for it),
        // the palette slot holds a copy of the Desert island's; both the fine-weather Citadel's now, whose atlas the island is drawn with
        var ressPath = Path.Combine(game, "RESS.HQR");
        var ress = HqrArchive.Open(ressPath);
        var bytes = File.ReadAllBytes(ressPath);
        // (the retail slots have no data of their own: FillEntry gives them some; a folder built before has, and it is replaced)
        byte[] Set(byte[] hqr, int slot, byte[] data)
        {
            try { return HqrWriter.FillEntry(hqr, slot, HqrWriter.StoredEntry(data)); }
            catch (InvalidDataException) { return HqrWriter.ReplaceEntry(hqr, slot, HqrWriter.StoredEntry(data)); }
        }
        bytes = Set(bytes, SkyEntry, ress.Read(CitabauSky));
        bytes = Set(bytes, PaletteEntry, ress.Read(CitabauPalette));
        File.WriteAllBytes(ressPath, bytes);
        log.Add($"RESS.HQR: entry {SkyEntry} (island 1's sky) = entry {CitabauSky}, entry {PaletteEntry} (island 1's palette) = entry {CitabauPalette}");

        // island 1's holomap picture and camera (HOLOMAP.HQR 20 and 21, empty in the retail file: the holomap already names the Well
        // of Sendell and knows where on the planet it is, but zoomed in it showed nothing): the island's ground drawn through Francos
        // Island's camera (also one cube at (7, 7)) over the sea of the fine-weather Citadel Island's picture
        var holoPath = Path.Combine(game, RaceTrackHolomap.File);
        var holo = HqrArchive.Open(holoPath);
        var ress0 = HqrArchive.Open(ressPath).Read(0);
        var camera = holo.Read(RaceTrackHolomap.FirstMap + 2 * 9 + 1);
        var seaPicture = HolomapPicture.SeaBackground(holo.Read(RaceTrackHolomap.FirstMap + 2 * 12), HqrArchive.Open(ressPath).Read(0), 300, 435, 639, 479);
        var picture = HolomapPicture.Draw(IslandFile.Load(Path.Combine(game, "SENDELL.ILE")), IslandMapRenderer.LoadPalette(game, "SENDELL"), ress0[..768], camera, seaPicture);
        var holoBytes = File.ReadAllBytes(holoPath);
        holoBytes = Set(holoBytes, RaceTrackHolomap.FirstMap + 2 * 1, picture);
        holoBytes = Set(holoBytes, RaceTrackHolomap.FirstMap + 2 * 1 + 1, camera);
        File.WriteAllBytes(holoPath, holoBytes);
        log.Add($"HOLOMAP.HQR: entries {RaceTrackHolomap.FirstMap + 2} and {RaceTrackHolomap.FirstMap + 3} (island 1's picture and camera): the island drawn through Francos Island's camera");

        // the scene: scene 44's (the Dome of the Slate's) with island 1, the new cube, Twinsen alone on the rim of the well
        var scenePath = Path.Combine(game, "SCENE.HQR");
        var scenes = HqrArchive.Open(scenePath);
        var count = HqrArchive.CountEntries(scenePath);
        if (count != Scene + 1) throw new InvalidDataException($"SCENE.HQR has {count - 1} scenes, not the {Scene} of the retail game: build into a folder with the original SCENE.HQR");
        var model = LBAAssembler.Scenes.SceneSerializer.Parse(LBAAssembler.Scenes.SceneGame.Lba2, scenes.Read(SourceScene + 1));
        model.Island = 1; model.CubeX = CubeX; model.CubeY = CubeZ;
        var hero = model.Hero;
        var keepZoe = model.Actors.Count > 1 && model.Actors[1].Entity == 14 && model.Actors[1].X == 0 && model.Actors[1].Z == 0;
        model.Actors.RemoveRange(keepZoe ? 2 : 1, model.Actors.Count - (keepZoe ? 2 : 1));
        model.Zones.Clear(); model.TrackPoints.Clear();
        hero.Life = new byte[] { 0 }; hero.Track = new byte[] { 0 };
        foreach (var a in model.Actors.Skip(1)) { a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 }; }
        // (on the paving, south of the well, facing it)
        hero.X = (mid + 0) * 512 + 256; hero.Z = (int)((mid + (RimOut + 2.5)) * 512); hero.Y = PlateauHeight; hero.Beta = 2048;
        // A way there and back for the test: the Sendell's sign on Citadel Island (scene 47, a circle of flowers with an S in it) sends
        // Twinsen to the rim of the well, and a patch of the new island's south shore sends him back beside the sign. Cube-change zones:
        // the engine carries his place in the zone over (OBJECT.CPP GereZoneChangeCube: arrival = Info0..2 + where he is in the zone).
        var citadel = IslandFile.Load(Path.Combine(pristine, "CITADEL.ILE"));
        int CitadelHeight(double lx, double lz) => (int)Math.Round(IslandOps.Altitude(citadel, SignCubeX * 32768.0 + lx, SignCubeZ * 32768.0 + lz) ?? 0);
        LBAAssembler.Scenes.SceneZoneModel Portal(double cx, double cz, int ground, int toScene, double ax, double ay, double az)
        {
            var zone = new LBAAssembler.Scenes.SceneZoneModel
            {
                Type = 0, Num = toScene, Info = new int[8],
                X0 = (int)cx - 512, X1 = (int)cx + 511, Z0 = (int)cz - 512, Z1 = (int)cz + 511, Y0 = ground - 256, Y1 = ground + 2048,
            };
            zone.Info[0] = (int)ax - 512; zone.Info[1] = (int)ay - 256; zone.Info[2] = (int)az - 512; zone.Info[7] = 1;
            return zone;
        }
        var sign = LBAAssembler.Scenes.SceneSerializer.Parse(LBAAssembler.Scenes.SceneGame.Lba2, scenes.Read(SignScene + 1));
        var signGround = CitadelHeight(SignX, SignZ);
        LBAAssembler.Scenes.SceneOps.AddZone(sign, Portal(SignX, SignZ, signGround, Scene, hero.X, hero.Y, hero.Z));
        // (the way back: south of the rim, on the slope above the beach; arriving east of the sign's circle)
        double backX = mid * 512, backZ = (mid + 20) * 512;
        var backGround = (int)Math.Round(HeightAt(0, 20));
        double arriveX = SignX + 8.5 * 512, arriveZ = SignZ;
        model.Zones.Add(Portal(backX, backZ, backGround, SignScene, arriveX, CitadelHeight(arriveX, arriveZ), arriveZ));
        log.Add($"the way there: a cube-change zone to scene {Scene} on the Sendell's sign (scene {SignScene}, at {SignX},{signGround},{SignZ}); the way back: one to scene {SignScene} on the island's south slope (at {backX},{backGround},{backZ}), arriving east of the sign");
        var record = LBAAssembler.Scenes.SceneSerializer.Write(model);
        var sceneBytes = File.ReadAllBytes(scenePath);
        sceneBytes = HqrWriter.ReplaceEntry(sceneBytes, SignScene + 1, HqrWriter.StoredEntry(LBAAssembler.Scenes.SceneSerializer.Write(sign)));
        sceneBytes = HqrWriter.AppendEntry(sceneBytes, HqrWriter.StoredEntry(record));
        // (entry 0: the largest scene's size, which the engine sizes its scene buffer from)
        var largest = BitConverter.ToInt32(scenes.Read(0), 0);
        if (record.Length > largest) sceneBytes = HqrWriter.ReplaceEntry(sceneBytes, 0, HqrWriter.StoredEntry(BitConverter.GetBytes(record.Length)));
        File.WriteAllBytes(scenePath, sceneBytes);
        log.Add($"SCENE.HQR: scene {Scene} (island 1, cube {CubeX},{CubeZ}): scene {SourceScene}'s with {model.Actors.Count} actor(s) left{(keepZoe ? " (Twinsen and the engine's Zoe placeholder)" : "")}, its zones only the way back, Twinsen at ({hero.X},{hero.Y},{hero.Z}); {record.Length} bytes");
        foreach (var l in log) Console.WriteLine(l);
        return 0;
    }

    // The colour a ground triangle shows: the mean of the atlas pixels its texture corners span, in the island's palette.
    internal static (int R, int G, int B)? TriangleColour(IslandFile island, IslandCube cube, int x, int z, int half, byte[] palette)
    {
        var polygon = new IslandPolygon(cube.Polygon(x, z, half));
        if (polygon.TexFlag == 0) return null;
        var i = polygon.TextureIndex;
        if (i * 6 + 6 > cube.TextureDefs.Length) return null;
        var u = new double[3]; var v = new double[3];
        for (var k = 0; k < 3; k++) { u[k] = cube.TextureDefs[i * 6 + k * 2] / 256.0; v[k] = cube.TextureDefs[i * 6 + k * 2 + 1] / 256.0; }
        long r = 0, g = 0, b = 0; var n = 0;
        // (sample the triangle on a small barycentric grid)
        for (var a = 0; a <= 6; a++)
        for (var c = 0; c <= 6 - a; c++)
        {
            double wa = a / 6.0, wc = c / 6.0, wb = 1 - wa - wc;
            var px = (int)Math.Clamp(u[0] * wa + u[1] * wb + u[2] * wc, 0, 255);
            var py = (int)Math.Clamp(v[0] * wa + v[1] * wb + v[2] * wc, 0, 255);
            var index = island.GroundTexture[py * 256 + px];
            r += palette[index * 3]; g += palette[index * 3 + 1]; b += palette[index * 3 + 2]; n++;
        }
        return ((int)(r / n), (int)(g / n), (int)(b / n));
    }

    // grass, rock, sand, earth or water, from a colour
    internal static char Class((int R, int G, int B)? colour)
    {
        if (colour is not { } c) return '.';
        var (r, g, b) = c;
        if (b > r + 20 && b > g) return '~';
        if (g > r + 8 && g > b + 8) return 'g';
        if (r > 150 && g > 120 && b < 110 && r - b > 50) return 's';
        if (r > g + 15 && g > b) return 'e';
        return 'r';
    }

    private static int Inspect(string pristine, int cubeX, int cubeZ)
    {
        var island = IslandFile.Load(Path.Combine(pristine, "CITADEL.ILE"));
        var palette = IslandMapRenderer.LoadPalette(pristine, "CITADEL");
        var cube = island.CubeAt(cubeX, cubeZ) ?? throw new InvalidDataException("no cube there");
        Console.WriteLine($"cube ({cubeX},{cubeZ}) id {cube.Id}: {cube.Decors.Count} decors, {cube.TextureDefs.Length / 6} texture definitions, heights {cube.Heights.Min()}..{cube.Heights.Max()}");
        foreach (var d in cube.Decors) Console.WriteLine($"  decor body {d.Body} at ({d.X},{d.Y},{d.Z}) box x {d.XMin}..{d.XMax} y {d.YMin}..{d.YMax} z {d.ZMin}..{d.ZMax}");
        Console.WriteLine("heights / 100, every other vertex:");
        for (var z = 0; z <= 64; z += 2)
        {
            var line = "";
            for (var x = 0; x <= 64; x += 2) line += Math.Clamp(cube.Height(x, z) / 100, -9, 99).ToString().PadLeft(3);
            Console.WriteLine(line);
        }
        Console.WriteLine("ground classes (g grass, r rock, s sand, e earth, ~ water, . untextured), code jeu where not 0 as a digit:");
        for (var z = 0; z < 64; z++)
        {
            var line = "";
            for (var x = 0; x < 64; x++)
            {
                var p = new IslandPolygon(cube.Polygon(x, z, 0));
                line += p.CodeJeu != 0 ? (char)('0' + Math.Min(9, p.CodeJeu)) : Class(TriangleColour(island, cube, x, z, 0, palette));
            }
            Console.WriteLine(line);
        }
        return 0;
    }
}
