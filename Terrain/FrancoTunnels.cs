using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.LbaScript;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;
using LBAAssembler.Terrain.Citadel;

namespace LBAAssembler.Terrain;

// The Francos' tunnels (the user, 2026-11-12: "Our race track on Franco's island cuts through some buildings so they're not accessible, let's
// build some underground tunnels that join them up so their new entrance will either be tunnel entrance or elevators into and out of the
// tunnels with a couple of elevators in the connecting tunnels to reach the surface again"). The road runs over (and through) four of the
// island's doors -- the refinery's two (scenes 140 and 143) and two village huts (171 and 172) -- so each of those doors is now at the end of
// a tunnel, in one scene (394) of the prison's dark stonework: a long tunnel along its middle, a door at each end (the refinery's two) and at
// the north end of two branches (the huts), and four branches south each ending in an elevator up to a hatch on the island's ground,
// clear of the road (RaceTrackStory.FrancoTunnels.cs picks the spots, and moves the buildings' own way out to come out here).
//   - an elevator: the plate and its rails (as the bowling alley's) at the end of a branch; Action on it takes it up, and into the hatch's
//     scene; the hatch (a sprite on the ground) is an Action pad: Action there switches on a door at its feet that takes Twinsen down,
//     onto the plate, which brings him to the floor.
internal static class FrancoTunnels
{
    public const int Scene = 394, Prison = 84;
    private const int FloorLayer = 3, Tile = 8, Wall = 57, WallLayers = 3, Size = 64, Height = 25;
    public const int FloorY = (FloorLayer + 1) * 256;
    private const int LiftSpeed = 600, LiftStart = 506, LiftStop = 510, ClubDown = 1200, ClubUp = 1800;
    private const int LiftRide = 1, LiftDown = 2, LiftPlace = 5;

    // the floors, in cells (x0, x1, z0, z1): the tunnel along the middle, the two branches north to the huts' doors, the four south to the lifts
    private static readonly (int X0, int X1, int Z0, int Z1)[] Floors =
    {
        (8, 58, 30, 32),
        (14, 16, 10, 29), (28, 30, 10, 29),
        (19, 22, 33, 47), (33, 36, 33, 47), (43, 46, 33, 47), (51, 54, 33, 47),
    };
    // the lifts' middles (the corner between four cells), by the branches' ends: the first two near the huts' doors, the last two the refinery's
    public static readonly (int X, int Z)[] Lifts = { (21, 46), (35, 46), (45, 46), (53, 46) };
    public const int LiftCount = 4;

    // where the doors of each interior come out in the tunnels (T_ZONE Info: the corner of where a walker's offset in the zone is added to):
    // Tunnel (cells) -> interior arrival; the interiors' own exits to the tunnels
    public record Door(int Interior, int X0, int X1, int Z0, int Z1, int InX, int InY, int InZ);
    public static readonly Door[] Doors =
    {
        // the refinery's lower door (scene 140): the tunnel's east end, walked east into it
        new(140, 58 * 512 + 256, 59 * 512 - 1, 30 * 512, 33 * 512 - 1, 12800, 3584, 25088),
        // the refinery's upper door (scene 143): the west end, walked west
        new(143, 8 * 512, 8 * 512 + 255, 30 * 512, 33 * 512 - 1, 31744 - 200, 1536, 25344 - 768),
        // the huts' doors (171 the west branch, 172 the east): walked north
        new(171, 14 * 512, 17 * 512 - 1, 10 * 512, 10 * 512 + 255, 19456 - 768, 768, 19456 - 220),
        new(172, 28 * 512, 31 * 512 - 1, 10 * 512, 10 * 512 + 255, 18432 - 768, 256, 17920 - 220),
    };
    // each interior's way out into the tunnels, as the zone's info (x, y, z of the corner a walker's offset is added to): the arrival
    // three cells in from the door
    public static (int X, int Y, int Z) Exit(int interior) => interior switch
    {
        140 => (55 * 512 - 512, FloorY + 20, 31 * 512 + 256 - 768),
        143 => (11 * 512 + 256, FloorY + 20, 31 * 512 + 256 - 768),
        171 => (15 * 512 + 256 - 512, FloorY + 20, 13 * 512),
        172 => (29 * 512 + 256 - 512, FloorY + 20, 13 * 512),
        _ => throw new ArgumentOutOfRangeException(nameof(interior)),
    };

    private const string Idle = "label(0);\nstop();\n";
    private static string Still() => "void comportement_0()\n{\n}\n";

    // The surface pads' places (scene, local x, ground y, local z), one for each lift
    public record Pad(int Scene, int X, int Y, int Z);

    public static List<string> Install(string gameDirectory, IReadOnlyList<Pad> pads)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(Prison)) { log.Add("The Francos' tunnels left out: no Otringal prison (scene 84)"); return log; }
        if (pads.Count != LiftCount) { log.Add("The Francos' tunnels left out: no room for the elevators' hatches"); return log; }
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        if (Scene + 1 < scenes.Count && !scenes.IsEmpty(Scene + 1)) { log.Add($"The Francos' tunnels left out: scene {Scene} is already another part of the build's"); return log; }
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        var type = table[Prison * 2];
        var raw = bkg.Read(header.Gri + table[Prison * 2 + 1]);
        var shape = Lba2GridBackend.ToLba1Shape(raw);

        // ---- the grid: flagstones along the tunnels, the prison's tall stone round them
        var set = new Dictionary<(int, int, int), (int Block, int Pos)>();
        int Mod(int a, int m) => ((a % m) + m) % m;
        var floor = new HashSet<(int X, int Z)>();
        foreach (var (x0, x1, z0, z1) in Floors)
            for (var z = z0; z <= z1; z++)
                for (var x = x0; x <= x1; x++) floor.Add((x, z));
        foreach (var (x, z) in floor)
            for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                    if (!floor.Contains((x + dx, z + dz)))
                        for (var y = FloorLayer; y < FloorLayer + WallLayers; y++) set[(x + dx, y, z + dz)] = (Wall, 0);
        foreach (var (x, z) in floor) set[(x, FloorLayer, z)] = (Tile, Mod(x, 3) + 3 * Mod(z, 3));
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
        if (gridNumber > 255) { log.Add("The Francos' tunnels left out: LBA_BKG.HQR has 256 grids already"); return log; }
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

        // ---- the scene: the prison's header (its dim light, its music), Twinsen arriving on the first lift
        var model = store.Load(Prison).Clone();
        model.Island = RaceTrackIsland.Knartas.IslandByte;
        model.Actors.RemoveRange(1, model.Actors.Count - 1);
        model.Zones.Clear();
        model.TrackPoints.Clear();
        var texts = new Dictionary<int, (string Life, string Track)>();
        void Text(int actor, string life, string track) => texts[actor] = (life, track);
        var hero = model.Hero;
        (hero.X, hero.Y, hero.Z) = (Lifts[0].X * 512, FloorY + CitadelProps.LiftTop + ClubDown, Lifts[0].Z * 512);
        hero.Life = new byte[] { 0 }; hero.Track = new byte[] { 0 };
        const int DontReadjust = 1;
        var clubTop = FloorY + ClubUp - 100;
        // (track point 0 goes unused: a goto to point 0 never arrived, the first plate stuck up where it started)
        SceneOps.AddTrackPoint(model, new SceneTrackPoint(0, FloorY, 0));
        // (actor 1 is the game's buggy's: the first plate there was treated as the buggy, and never came down -- an unused helper takes the place)
        Text(SceneOps.AddActor(model, RaceTrackScenes.InvisibleHelper(0, FloorY, 0)), Still(), Idle);
        var arrivals = new List<string>();
        var waits = new List<string>();
        var rides = new List<string>();
        for (var k = 0; k < LiftCount; k++)
        {
            var (cx, cz) = (Lifts[k].X * 512, Lifts[k].Z * 512);
            int liftZone = 70 + 2 * k, arrivalZone = 71 + 2 * k;
            var pad = pads[k];
            const int Big = 1024;   // (the platform is four plates, two by two: where on it Twinsen arrives is where he stood in the hatch's door)
            // the elevator's way up: a door over the platform's top, out at the hatch
            model.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = pad.Scene, X0 = cx - Big, X1 = cx + Big - 1, Z0 = cz - Big, Z1 = cz + Big - 1, Y0 = clubTop, Y1 = clubTop + 3000,
                Info = new[] { pad.X - Big, pad.Y + 30, pad.Z - Big, 0, 0, 0, 0, 1 },
            });
            model.Zones.Add(new SceneZoneModel { Type = 2, Num = liftZone, X0 = cx - Big, X1 = cx + Big - 1, Z0 = cz - Big, Z1 = cz + Big - 1, Y0 = FloorY - 100, Y1 = FloorY + 800, Info = new int[8] });
            model.Zones.Add(new SceneZoneModel { Type = 2, Num = arrivalZone, X0 = cx - Big, X1 = cx + Big - 1, Z0 = cz - Big, Z1 = cz + Big - 1, Y0 = FloorY + ClubDown - 300, Y1 = FloorY + ClubDown + 900, Info = new int[8] });
            var parts = new List<int>();
            var plateTracks = new List<(int Low, int High)>();
            foreach (var (dx, dz) in new[] { (-512, -512), (512, -512), (-512, 512), (512, 512) })
            {
                var low = SceneOps.AddTrackPoint(model, new SceneTrackPoint(cx + dx, FloorY, cz + dz));
                var high = SceneOps.AddTrackPoint(model, new SceneTrackPoint(cx + dx, FloorY + ClubUp, cz + dz));
                var a = CitadelProps.Actor(CitadelProps.MoonLiftPlatform, cx + dx, FloorY + ClubDown, cz + dz);
                a.Flags |= RaceTrackScenes.CarrierFlag; a.Move = 3; a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 };
                parts.Add(SceneOps.AddActor(model, a));
                plateTracks.Add((low, high));
            }
            string TrackOf((int Low, int High) t) => $"label(0);\nspeed({LiftSpeed});\ngoto_point_3d({t.Low});\nsample({LiftStop});\n\nlabel({LiftDown});\nstop();\n\n" +
                        $"label({LiftRide});\nsample({LiftStart});\nspeed({LiftSpeed});\ngoto_point_3d({t.High});\nstop();\n\n" +
                        $"label({LiftPlace});\npos_point({t.Low});\ngoto(label_{LiftDown});\n";
            // (every plate starts up and comes down at the scene's start: where Twinsen arrives on one it takes him to the floor)
            var life = Still();
            for (var q = 0; q < parts.Count; q++) Text(parts[q], life, TrackOf(plateTracks[q]));
            arrivals.Add($"    {(k == 0 ? "if" : "else if")} ({arrivalZone} == zone())\n    {{\n        set_dir(NO_MOVE);\n        set_comportement(comportement_{4 + k});\n    }}\n");
            waits.Add($"void comportement_{4 + k}()\n{{\n    if ({LiftDown} == l_track_obj({parts[0]}))\n    {{\n        set_dir(MOVE_MANUAL);\n        set_comportement(comportement_1);\n    }}\n}}\n");
            rides.Add($"        if ({liftZone} == zone())\n        {{\n            set_dir(NO_MOVE);\n{string.Concat(parts.Select(i => $"            set_track_obj({i}, label_{LiftRide});\n"))}            set_comportement(comportement_3);\n        }}\n");
        }
        // the doors into the buildings
        foreach (var d in Doors)
            model.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = d.Interior, X0 = d.X0, X1 = d.X1, Z0 = d.Z0, Z1 = d.Z1, Y0 = FloorY - 100, Y1 = FloorY + 1500,
                Info = new[] { d.InX, d.InY - 70, d.InZ, 0, 0, 0, 0, 1 },
            });
        var heroLife = $@"void comportement_0()
{{
{string.Concat(arrivals)}    else
    {{
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    swif (1 == action())
    {{
{string.Concat(rides)}    }}
}}

void comportement_3()
{{
}}

{string.Concat(waits)}";

        // ---- the scripts
        {
            var built = SceneScripts.Load(SceneSerializer.Write(model), Scene);
            built.SetText(0, ScriptKind.Life, heroLife);
            foreach (var (actor, (life, track)) in texts) { built.SetText(actor, ScriptKind.Life, life); built.SetText(actor, ScriptKind.Track, track); }
            var result = built.Build();
            if (!result.Ok) { log.AddRange(result.Errors.Select(e => $"The Francos' tunnels left out: {e}")); return log; }
            var entry = Scene + 1;
            while (scenes.Count <= entry) scenes.Slots.Add(new HqrFile.Slot());
            scenes.SetStored(entry, result.Record!);
            if (result.Record!.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
            { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, result.Record.Length); scenes.SetStored(0, size); }
        }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        log.Add($"The Francos' tunnels: scene {Scene} (grid {gridNumber}, the prison's library {raw[0]}): {floor.Count} cells of tunnel, {Doors.Length} doors into the refinery and the huts, {LiftCount} elevators to the surface");
        return log;
    }

    // The hatch on the ground (scene edit): Action by it (in the zone round it) switches on a small door at the hatch's middle, which Twinsen walks
    // into (a shut door keeps him to its edge) and which takes him down to the tunnel scene, onto lift `k`'s plate -- the door's size is small
    // beside the plate's, so he arrives near its middle wherever he came from; the ground there slopes, so the zones
    // are tall and he falls onto the plate from what height he arrives at
    public static Action<SceneScripts> Hatch(SceneModel model, int k, int x, int y, int z, int driving)
    {
        int liftZone = 80 + k, doorSwitch = 20 + k;
        var helper = SceneOps.AddActor(model, RaceTrackScenes.InvisibleHelper(x, y, z));
        var sprite = SceneOps.AddActor(model, RaceTrackScenes.SpriteProp(x, y, z, RaceTrackScenes.HatchSprite, 9));
        const int Tall = 700, Half = 1000;
        var top = FloorY + CitadelProps.LiftTop + ClubDown;
        model.Zones.Add(new SceneZoneModel
        {
            Type = 0, Num = Scene, X0 = x - Half, X1 = x + Half - 1, Z0 = z - Half, Z1 = z + Half - 1, Y0 = y - Tall, Y1 = y + Tall,
            Info = new[] { Lifts[k].X * 512 - Half, top - Tall + 100, Lifts[k].Z * 512 - Half, 0, doorSwitch, 0, 1, 0 },
        });
        model.Zones.Add(new SceneZoneModel { Type = 2, Num = liftZone, X0 = x - Half, X1 = x + Half - 1, Z0 = z - Half, Z1 = z + Half - 1, Y0 = y - Tall, Y1 = y + Tall, Info = new int[8] });
        var life = $@"void comportement_0()
{{
    set_change_cube({doorSwitch}, 0);
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if ({liftZone} == zone_obj(0) && {driving} != comportement_hero())
        {{
            set_change_cube({doorSwitch}, 1);
            set_comportement(comportement_2);
        }}
    }}
}}

void comportement_2()
{{
}}
";
        return scripts =>
        {
            scripts.SetText(helper, ScriptKind.Life, life);
            scripts.SetText(helper, ScriptKind.Track, Idle);
            scripts.SetText(sprite, ScriptKind.Life, Still());
            scripts.SetText(sprite, ScriptKind.Track, Idle);
        };
    }
}
