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
// (2026-11-16, redone -- the user: "We also need to do a total redesign of the tunnels on the island of the francos. The tunnels should match
// the building locations for the franco village", and 'Let's use "118: Wannies Island, the city (room #69)" as our inspiration': the mine
// under the Wannies' city, its rock arches, dirt floor, cliffs and fences.) The tunnels laid out as the village lies over them -- the built
// island is the game's twice over (its cubes and places doubled: x' = 2x - 229,376 both ways), and the tunnels' cells are its places at
// a fifth of a cell each, give or take a few cells -- in scene 118's own stonework (its block library), copied stretch by stretch:
//   - north-south tunnels: its north arm's cross-section (x 21-27: the rock wall west, a ledge, four cells of floor, the cliff and its fence
//     east), its rows five at a time; east-west tunnels: its east arm's (z 25-29: the rock wall north, three cells of floor, the cliff and
//     its fence south), its columns three at a time; a tunnel's dead end the other way's wall or edge; where two meet, floor;
//   - the refinery's tunnel north-south, its lower door (140) at its north end and a stub east to its upper door (143); a long tunnel west
//     from its foot to the huts' tunnel, south, a stub west to the west hut (171) and one east to the north hut (172);
//   - the four elevators, on the tunnels' floor near the building whose hatch they go up to: the west hut's at the huts' tunnel's south
//     end, the north hut's halfway up it, the refinery's two up and down its tunnel.
internal static class FrancoTunnels
{
    public const int Scene = 394, Prison = 84, Mine = 118;
    private const int FloorLayer = 6, Size = 64, Height = 25;
    public const int FloorY = (FloorLayer + 1) * 256;
    private const int LiftSpeed = 600, LiftStart = 506, LiftStop = 510, ClubDown = 1200, ClubUp = 1800;
    private const int LiftRide = 1, LiftDown = 2, LiftPlace = 5;

    // the tunnels: along z (north-south) or x, the first cross cell (the wall's), the first and last cells along, a dead end's wall at either
    // end (the door there); NS seven cells across (floor the third to the sixth), EW five (floor the second to the fourth)
    private record Tunnel(bool AlongZ, int Cross, int From, int To, bool CapFrom, bool CapTo);
    private static readonly Tunnel[] Tunnels =
    {
        new(true, 25, 4, 31, true, false),      // the refinery's, its lower door (140) at its north end
        new(false, 15, 31, 37, false, true),    // east from it to the refinery's upper door (143)
        new(false, 28, 14, 30, false, false),   // west from its foot to the huts' tunnel
        new(true, 12, 29, 60, false, true),     // the huts', south
        new(false, 50, 5, 13, true, false),     // west from it to the west hut (171)
        new(false, 40, 18, 24, false, true),    // east from it to the north hut (172)
    };
    // (the corners the tunnels' meeting leaves open: a stack of the east-west tunnel's wall or edge each)
    private static readonly (int X, int Z, bool North)[] Corners = { (12, 28, true), (13, 28, true), (31, 32, false) };
    // the lifts' middles (the corner between four cells): the west hut's, the north hut's, the refinery's upper door's, its lower door's --
    // in the order of the hatches (RaceTrackStory.FrancoTunnels TunnelHatches)
    public static readonly (int X, int Z)[] Lifts = { (16, 58), (16, 47), (29, 22), (29, 10) };
    public const int LiftCount = 4;

    // where the doors of each interior come out in the tunnels (T_ZONE Info: the corner of where a walker's offset in the zone is added to):
    // Tunnel (cells) -> interior arrival; the interiors' own exits to the tunnels
    public record Door(int Interior, int X0, int X1, int Z0, int Z1, int InX, int InY, int InZ);
    public static readonly Door[] Doors =
    {
        // the refinery's lower door (scene 140): the refinery tunnel's north end, walked north into it
        new(140, 27 * 512, 31 * 512 - 1, 5 * 512, 5 * 512 + 255, 12800, 3584, 25088),
        // the refinery's upper door (scene 143): the east stub's end, walked east
        new(143, 36 * 512 + 256, 37 * 512 - 1, 16 * 512, 19 * 512 - 1, 31744 - 200, 1536, 25344 - 768),
        // the huts' doors: the west hut (171) at the west stub's end, walked west; the north hut (172) at the east stub's end, walked east
        new(171, 6 * 512, 6 * 512 + 255, 51 * 512, 54 * 512 - 1, 19456 - 768, 768, 19456 - 220),
        new(172, 23 * 512 + 256, 24 * 512 - 1, 41 * 512, 44 * 512 - 1, 18432 - 768, 256, 17920 - 220),
    };
    // each interior's way out into the tunnels, as the zone's info (x, y, z of the corner a walker's offset is added to): a few cells in
    // from its door
    public static (int X, int Y, int Z) Exit(int interior) => interior switch
    {
        140 => (29 * 512 - 768, FloorY + 20, 7 * 512),
        143 => (34 * 512, FloorY + 20, 17 * 512 + 256 - 768),
        171 => (8 * 512, FloorY + 20, 52 * 512 + 256 - 768),
        172 => (21 * 512, FloorY + 20, 42 * 512 + 256 - 768),
        _ => throw new ArgumentOutOfRangeException(nameof(interior)),
    };

    // scene 118's stretches: the north arm's cross-section (its columns, its rows' repeat and floor layer) and the east arm's
    private const int NsX = 21, NsZ = 15, NsRepeat = 5, NsFloor = 14, EwX = 39, EwZ = 25, EwRepeat = 3, EwFloor = 6;
    private const int FloorBlock = 12;

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
        if (!store.SceneExists(Mine)) { log.Add("The Francos' tunnels left out: no Wannies' mine (scene 118)"); return log; }
        if (pads.Count != LiftCount) { log.Add("The Francos' tunnels left out: no room for the elevators' hatches"); return log; }
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        if (Scene + 1 < scenes.Count && !scenes.IsEmpty(Scene + 1)) { log.Add($"The Francos' tunnels left out: scene {Scene} is already another part of the build's"); return log; }
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (!store.SceneExists(Mine) || table.Length < (Mine + 1) * 2) { log.Add("The Francos' tunnels left out: no Wannies' mine (scene 118)"); return log; }
        var type = table[Mine * 2];
        var raw = bkg.Read(header.Gri + table[Mine * 2 + 1]);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var mine = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * Size + x) * Height + y) * 2;
        int Mod(int a, int m) => ((a % m) + m) % m;
        // a column of the mine's, its layers moved by `shift`: (layer, block, place) of every cell with something (an empty cell with collision too)
        List<(int Y, int Block, int Pos)> Column(int x, int z, int shift)
        {
            var list = new List<(int, int, int)>();
            for (var y = 0; y < Height; y++)
            {
                var at = At(x, y, z);
                if (mine[at] == 0 && mine[at + 1] == 0) continue;
                if (y + shift >= 0 && y + shift < Height) list.Add((y + shift, mine[at], mine[at + 1]));
            }
            return list;
        }
        var ns = FloorLayer - NsFloor;
        List<(int, int, int)> NsColumn(int d, int z) => Column(NsX + d, NsZ + Mod(z, NsRepeat), ns);
        List<(int, int, int)> EwColumn(int x, int d) => Column(EwX + Mod(x, EwRepeat), EwZ + d, 0);

        // ---- the tunnels' columns, the floor's cells
        var columns = new Dictionary<(int X, int Z), List<(int Y, int Block, int Pos)>>();
        var floor = new HashSet<(int X, int Z)>();
        var floorColumn = new HashSet<(int X, int Z)>();
        foreach (var t in Tunnels)
            for (var a = t.From; a <= t.To; a++)
            {
                var capped = (a == t.From && t.CapFrom) || (a == t.To && t.CapTo);
                for (var d = 0; d < (t.AlongZ ? 7 : 5); d++)
                {
                    var (x, z) = t.AlongZ ? (t.Cross + d, a) : (a, t.Cross + d);
                    var isFloor = !capped && (t.AlongZ ? d is >= 2 and <= 5 : d is >= 1 and <= 3);
                    columns[(x, z)] = capped
                        ? (t.AlongZ ? EwColumn(x, a == t.From ? 0 : 4) : NsColumn(a == t.From ? 0 : 6, z))
                        : (t.AlongZ ? NsColumn(d, z) : EwColumn(x, d));
                    if (isFloor) { floor.Add((x, z)); floorColumn.Add((x, z)); }
                    else floorColumn.Remove((x, z));
                }
            }
        foreach (var (x, z, north) in Corners) columns[(x, z)] = EwColumn(x, north ? 0 : 4);
        // (where tunnels meet, a wall's or an edge's column on another's floor is floor: the mine's own tile)
        foreach (var c in floor.Where(c => !floorColumn.Contains(c)))
            columns[c] = new List<(int, int, int)> { (FloorLayer, FloorBlock, Mod(c.X + 1, 4) + 4 * Mod(c.Z, 4)) };
        var edits = new List<Lba1GridCell>();
        for (var z = 0; z < Size; z++)
        for (var x = 0; x < Size; x++)
        {
            var stack = columns.TryGetValue((x, z), out var col) ? col : null;
            for (var y = 0; y < Height; y++)
            {
                var cell = stack?.FirstOrDefault(c => c.Y == y);
                edits.Add(cell is { } k && (k.Block != 0 || k.Pos != 0) ? new Lba1GridCell(x, y, z, k.Block, k.Pos) : new Lba1GridCell(x, y, z, 0, 0));
            }
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
        var model = store.Load(Mine).Clone();
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
            string TrackOf((int Low, int High) t) => $"label(0);\nspeed({LiftSpeed});\ngoto_point_3d({t.Low});\nsample({LiftStop});\n\nlabel({LiftDown});\nwait_nb_dizieme(50);\ngoto(label_{LiftDown});\n\n" +
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
        log.Add($"The Francos' tunnels: scene {Scene} (grid {gridNumber}, the Wannies' mine's library {raw[0]}): {floor.Count} cells of tunnel floor, {Doors.Length} doors into the refinery and the huts, {LiftCount} elevators to the surface");
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
