using System.IO;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The scene side of a race track (the Desert island's or Citadel Island's): every exterior scene of the island loses its actors except Twinsen and the buggy,
// and the scene the start line lies in starts Twinsen and the buggy on the line.
internal static class RaceTrackScenes
{
    public const int BuggyEntity = 152;
    public const int ZoeEntity = 14;

    // Opponent: for each scene, the index of its copy of the racer's car (the race-track mode drives whichever the player's scene has);
    // Baldino: the same for Baldino's car; StartScene: the scene the start line is in; Grid: the grid spots, pole first, and Pits: the
    // spots in the pit lane the opponents wait on while the player qualifies, each [cube x, cube z, x, y, z, turn] in its cube's world units.
    public sealed record Result(List<string> Log, int ScenesChanged, int ActorsRemoved, Dictionary<int, int> Opponent, int StartScene, Dictionary<int, int> Baldino,
        List<int[]> Grid, List<int[]> Pits);

    // The buggy's own script removes it until the quest that mends it is done (game variable 74 >= 3). The compare is
    //   IF VAR_GAME(74) >= 3   =   0C 0F 4A 03 03 00 ..
    // and reads >= 0 with the 3 zeroed, so the buggy is there from the start of any game. True when it was that script.
    private static bool BuggyAlwaysThere(SceneActorModel buggy)
    {
        if (buggy.Life.Length <= 6 || buggy.Life[0] != 0x0C || buggy.Life[1] != 0x0F || buggy.Life[2] != 0x4A || buggy.Life[3] != 0x03 || buggy.Life[4] != 0x03 || buggy.Life[5] != 0x00) return false;
        buggy.Life[4] = 0x00;
        return true;
    }

    // ---- cube edges ----
    // An island's outside is one scene per cube, and the engine holds the hero at a cube's edge (EXTFUNC: Nxw clamped to the cube, with
    // FlagHeroOut set) unless a cube-change zone (type 0) to the next cube's scene covers the spot, at his height, on that edge
    // (GereZoneChangeCube: the arrival value 512 / 31744 names the edge). The retail zones only cover the edges where the retail paths
    // cross them -- scene 42's south edge has none for 14 cells where there was a cliff -- so wherever the lap crosses an edge the road
    // is checked, both ways, and given a zone of its own where the island's don't reach all of it: an invisible wall otherwise.
    private sealed record EdgeCrossing(int FromScene, int ToScene, int CubeX, int CubeZ, char Side, double Along, double Height);

    // the half width of road a crossing zone covers either side of where the lap's middle crosses (the road and its curbs), and how far
    // below and above the road it reaches
    private const double EdgeHalfCells = 8;
    private const int EdgeBelow = 2048, EdgeAbove = 4096;

    private static List<EdgeCrossing> EdgeCrossings(SceneStore store, RaceTrackReport report, RaceTrackOptions options, int island)
    {
        var result = new List<EdgeCrossing>();
        if (report.LapX.Length < 2 || report.GroundAfter is not { } ground) return result;
        var sceneOf = new Dictionary<(int, int), int>();
        for (var scene = options.Island.FirstScene; scene <= options.Island.LastScene; scene++)
        {
            if (!store.SceneExists(scene)) continue;
            try
            {
                var m = store.Load(scene);
                if (m.Island == island && m.CubeMode == 1) sceneOf.TryAdd((m.CubeX, m.CubeY), scene);
            }
            catch (Exception e) when (e is InvalidDataException or ArgumentException or IOException) { }
        }
        var n = report.LapX.Length;
        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            double x0 = report.LapX[i], z0 = report.LapZ[i], x1 = report.LapX[j], z1 = report.LapZ[j];
            int ax = (int)Math.Floor(x0 / 64), az = (int)Math.Floor(z0 / 64), bx = (int)Math.Floor(x1 / 64), bz = (int)Math.Floor(z1 / 64);
            if (ax == bx && az == bz) continue;
            if (!sceneOf.TryGetValue((ax, az), out var from) || !sceneOf.TryGetValue((bx, bz), out var to)) continue;
            // (where the segment meets the edge: an x edge, a z edge, or -- through a corner -- both, each taken at its own point)
            if (ax != bx)
            {
                var ex = Math.Max(ax, bx) * 64.0; var t = (ex - x0) / (x1 - x0); var z = z0 + (z1 - z0) * t;
                var h = ground(ex, z);
                result.Add(new EdgeCrossing(from, to, ax, az, bx > ax ? 'E' : 'W', z - az * 64.0, h));
                result.Add(new EdgeCrossing(to, from, bx, bz, bx > ax ? 'W' : 'E', z - bz * 64.0, h));
            }
            if (az != bz)
            {
                var ez = Math.Max(az, bz) * 64.0; var t = (ez - z0) / (z1 - z0); var x = x0 + (x1 - x0) * t;
                var h = ground(x, ez);
                result.Add(new EdgeCrossing(from, to, ax, az, bz > az ? 'S' : 'N', x - ax * 64.0, h));
                result.Add(new EdgeCrossing(to, from, bx, bz, bz > az ? 'N' : 'S', x - bx * 64.0, h));
            }
        }
        return result;
    }

    // Gives `model` a crossing zone for each place the lap leaves its cube that no zone of its own to the next scene covers (the whole
    // road's width, at the road's height). Its box and arrival are the retail edge zones' own: the last cell before the edge, the arrival
    // naming the edge (512 into the next cube's near side, 32768 - 1024 its far side), the other coordinate and the height carried over.
    private static int CoverEdges(SceneModel model, int scene, List<EdgeCrossing> edges, List<string> log)
    {
        var added = 0;
        const int Cube = IslandFile.CubeSize, Cell = 512, Near = 512, Far = Cube - 1024;
        foreach (var e in edges.Where(e => e.FromScene == scene))
        {
            int lo = (int)Math.Round((e.Along - EdgeHalfCells) * Cell), hi = (int)Math.Round((e.Along + EdgeHalfCells) * Cell);
            int y0 = Math.Max(0, (int)Math.Round(e.Height) - EdgeBelow), y1 = (int)Math.Round(e.Height) + EdgeAbove;
            var road = (int)Math.Round(e.Height);
            bool Covers(SceneZoneModel z)
            {
                if (z.Type != 0 || z.Num != e.ToScene || z.Info.Length < 3) return false;
                if (road < Math.Min(z.Y0, z.Y1) || road > Math.Max(z.Y0, z.Y1)) return false;
                int a0, a1; bool edge;
                switch (e.Side)
                {
                    case 'S': edge = z.Info[2] == Near && Math.Max(z.Z0, z.Z1) >= Cube - Cell; a0 = Math.Min(z.X0, z.X1); a1 = Math.Max(z.X0, z.X1); break;
                    case 'N': edge = z.Info[2] == Far && Math.Min(z.Z0, z.Z1) <= Cell; a0 = Math.Min(z.X0, z.X1); a1 = Math.Max(z.X0, z.X1); break;
                    case 'E': edge = z.Info[0] == Near && Math.Max(z.X0, z.X1) >= Cube - Cell; a0 = Math.Min(z.Z0, z.Z1); a1 = Math.Max(z.Z0, z.Z1); break;
                    default: edge = z.Info[0] == Far && Math.Min(z.X0, z.X1) <= Cell; a0 = Math.Min(z.Z0, z.Z1); a1 = Math.Max(z.Z0, z.Z1); break;
                }
                return edge && a0 <= lo && a1 >= hi;
            }
            if (model.Zones.Any(Covers)) continue;
            var zone = new SceneZoneModel { Type = 0, Num = e.ToScene, Info = new int[8], Y0 = y0, Y1 = y1 };
            zone.Info[1] = y0; zone.Info[7] = 1;
            switch (e.Side)
            {
                case 'S': zone.X0 = lo; zone.X1 = hi; zone.Z0 = Cube - Cell; zone.Z1 = Cube; zone.Info[0] = lo; zone.Info[2] = Near; break;
                case 'N': zone.X0 = lo; zone.X1 = hi; zone.Z0 = 0; zone.Z1 = Cell; zone.Info[0] = lo; zone.Info[2] = Far; break;
                case 'E': zone.Z0 = lo; zone.Z1 = hi; zone.X0 = Cube - Cell; zone.X1 = Cube; zone.Info[2] = lo; zone.Info[0] = Near; break;
                default: zone.Z0 = lo; zone.Z1 = hi; zone.X0 = 0; zone.X1 = Cell; zone.Info[2] = lo; zone.Info[0] = Far; break;
            }
            SceneOps.AddZone(model, zone);
            added++;
            log.Add($"scene {scene}: the road leaves the cube over its {e.Side} edge where no crossing zone reached it -- one added to scene {e.ToScene} ({EdgeHalfCells * 2:0} cells of edge, height {y0}..{y1})");
        }
        return added;
    }

    // The Desert island's own buggy (scene 67), for an island whose scenes have none: the same actor, so its script and flags are the
    // game's own. Null when that scene cannot be read.
    private static SceneActorModel? BuggyTemplate(SceneStore store, List<string> log)
    {
        try
        {
            var buggy = store.Load(BuggyScene).Actors.Skip(1).FirstOrDefault(a => a.Entity == BuggyEntity)?.Clone();
            if (buggy is null) log.Add($"no buggy: scene {BuggyScene} has none");
            else log.Add($"the buggy: a copy of the one in scene {BuggyScene} (this island has none of its own)");
            return buggy;
        }
        catch (Exception e) when (e is InvalidDataException or ArgumentException or IOException)
        {
            log.Add($"no buggy: scene {BuggyScene} could not be read ({e.Message})");
            return null;
        }
    }

    // The retail track's racer (scene 57, actor 4: "Car with racer", which drives the retail lap by its route points), copied into every
    // outside scene as the opponent the race-track mode drives round the lap. The copies neither collide, fall, check zones nor react to
    // hits (NO_CHOC), and are drawn with the depth buffer like the car (OBJ_ZBUFFER, NO_PRE_CLIP); their scripts are a single END. Each
    // waits 20000 below the ground, except the one on the grid beside the player's car: without the race-track mode (the retail engine)
    // that is all there is, a parked car.
    public const int RacerEntity = 157, RacerScene = 57;
    // how many cells behind the start line Baldino starts (the second row); his line's grid point (a point a cell)
    public const int BaldinoGridBack = 8;

    // The grid: spots staggered either side of the road's middle, pole GridFirst cells behind the start line and each GridStep behind the one
    // before, GridSide cells to its side -- so two cars side by side are 2 x GridSide apart across (the cars are 2.6 cells wide: 0.9 cells
    // between them) and a car is 2 x GridStep behind the one on its own side. The race-track mode puts the cars on them in the order the
    // qualifying decides; the build puts Twinsen's buggy on pole, the racer on the second spot and Baldino on the third.
    public const double GridFirst = 3, GridStep = 3.5, GridSide = 1.75;
    public const int GridSpots = 5;
    public static (double Back, double Side) GridSpot(int k) => (GridFirst + GridStep * k, k % 2 == 0 ? -GridSide : GridSide);
    private const uint OpponentFlags = 0x1A0000;

    public const int DesertIsland = 2;
    // The buggy's own scene on the Desert island: an island with no buggy of its own (Citadel) gets a copy of that actor on the grid.
    public const int BuggyScene = 67;

    // Edits the outside scenes of the Desert island as the options say, from what the build of the island found (start line, jump, road).
    public static Result Apply(string gameDirectory, RaceTrackReport report, RaceTrackOptions options)
    {
        var island = options.Island.IslandByte;
        var demoFrom = 190;
        (double X, double Z, double Y, double DirX, double DirZ)? start = options.StartAtLine && report.StartLine.Count > 0 ? report.StartLine[0] : null;
        var distanceToRoad = report.DistanceToRoad; var roadReach = 6.5; var jump = report.Jump;
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var log = new List<string>();
        var changes = new List<SceneChange>();
        var removed = 0; var zonesRemoved = 0; var camerasRemoved = 0;
        var opponent = new Dictionary<int, int>(); var baldino = new Dictionary<int, int>(); var startScene = -1;
        var gridSpots = new List<int[]>(); var pitSpots = new List<int[]>();
        SceneActorModel? racer = null;
        if (options.AddOpponent)
            try { racer = store.Load(RacerScene).Actors.Skip(1).FirstOrDefault(a => a.Entity == RacerEntity)?.Clone(); }
            catch (Exception e) when (e is InvalidDataException or ArgumentException or IOException) { log.Add($"no opponent: scene {RacerScene} could not be read ({e.Message})"); }
        var edges = EdgeCrossings(store, report, options, island);
        var edgeZonesAdded = 0;
        for (var scene = 0; scene < store.SceneCount; scene++)
        {
            if (!store.SceneExists(scene)) continue;
            SceneModel model;
            try { model = store.Load(scene); } catch (Exception e) when (e is InvalidDataException or ArgumentException or IOException) { continue; }
            if (model.Island != island || model.CubeMode != 1) continue;
            if (scene < options.Island.FirstScene || scene > options.Island.LastScene)
            {
                // the demo scenes are copies the game plays as films; they are left as they are
                log.Add($"scene {scene}: {(scene >= demoFrom ? "demo scene" : "not one of the island's own outside scenes")}, left alone");
                continue;
            }
            var count = 0;
            (int X, int Z, int Beta)? grid = null, baldinoGrid = null;
            if (options.RemoveActors)
            {
                // slot 1 of every scene is the engine's own placeholder for Zoe (entity 14, no body, parked at 0,0,0); the engine treats that slot
                // specially -- a buggy that took its place came up with no life -- so it stays
                var needed = CutsceneActors(model);
                var doomed = Enumerable.Range(1, model.Actors.Count - 1)
                    .Where(i => model.Actors[i].Entity != BuggyEntity && !(i == 1 && model.Actors[i].Entity == ZoeEntity && model.Actors[i].X == 0 && model.Actors[i].Z == 0))
                    .Where(i => !needed.Contains(i))
                    .ToList();
                if (needed.Count > 0) log.Add($"scene {scene}: actors {string.Join(", ", needed.Order())} kept -- Twinsen's own script waits on them in its travel cutscenes (the ferry, the Dino-Fly), which would never end without them");
                var standIn = AddStandIn(model, doomed);
                foreach (var i in doomed.OrderByDescending(i => i))
                {
                    SceneOps.DeleteActor(model, i, retarget: standIn is { } stand ? stand - count : 0);
                    count++;
                }
                if (standIn is { } si)
                {
                    TidyStandIn(model, si - count);
                    log.Add($"scene {scene}: the scripts that stay referred to removed actors; those references now go to a stand-in (actor {si - count})");
                }
            }
            removed += count;
            if (report.GroundBefore is { } groundBefore && report.GroundAfter is { } groundAfter && report.WasGround is { } wasGround)
                Reseat(model, groundBefore, groundAfter, wasGround, scene, log);
            // The buggy's own script removes it until the quest that mends it is done (game variable 74 >= 3). The compare is
            //   IF VAR_GAME(74) >= 3   =   0C 0F 4A 03 03 00 ..
            // and reads >= 0 with the 3 zeroed, so the buggy is there from the start of any game.
            if (options.BuggyAlways)
                foreach (var actor in model.Actors.Skip(1).Where(a => a.Entity == BuggyEntity))
                    if (BuggyAlwaysThere(actor)) log.Add($"scene {model.CubeX},{model.CubeY}: the buggy no longer waits for the car quest");
            log.Add($"scene {scene} (cube {model.CubeX},{model.CubeY}): {count} actors removed, {model.Actors.Count - 1} left");

            if (start is { } s)
            {
                var wx = s.X * 512; var wz = s.Z * 512;
                var cx = (int)Math.Floor(wx / IslandFile.CubeSize); var cz = (int)Math.Floor(wz / IslandFile.CubeSize);
                if (cx == model.CubeX && cz == model.CubeY)
                {
                    var lx = (int)Math.Round(wx - cx * (double)IslandFile.CubeSize); var lz = (int)Math.Round(wz - cz * (double)IslandFile.CubeSize);
                    var beta = (int)Math.Round(Math.Atan2(s.DirX, s.DirZ) / (2 * Math.PI) * 4096); beta = ((beta % 4096) + 4096) % 4096;
                    var y = (int)Math.Round(s.Y);
                    // the buggy stands a few cells before the line, Twinsen beside it; each on the ground at its own spot (the road climbs there)
                    int At(double back, double side, bool z) => (int)Math.Round((z ? lz : lx) + (z ? (-s.DirZ * back + s.DirX * side) : (-s.DirX * back - s.DirZ * side)) * 512);
                    int Ground(int x, int z) => report.GroundAfter is { } g ? (int)Math.Round(g(cx * 64 + x / 512.0, cz * 64 + z / 512.0)) : y;
                    var buggy = model.Actors.Skip(1).FirstOrDefault(a => a.Entity == BuggyEntity);
                    // an island with no buggy of its own (Citadel): a copy of the Desert island's own buggy actor, on the grid
                    if (buggy is null && BuggyTemplate(store, log) is { } spare)
                    {
                        // (the quest patch above ran before this copy was here)
                        if (options.BuggyAlways) BuggyAlwaysThere(spare);
                        buggy = spare;
                        SceneOps.AddActor(model, buggy);
                    }
                    var buggyIndex = buggy is null ? -1 : model.Actors.IndexOf(buggy);
                    for (var k = 0; k < GridSpots; k++)
                    {
                        var (back, side) = GridSpot(k);
                        int gx = At(back, side, false), gz = At(back, side, true);
                        gridSpots.Add(new[] { cx, cz, gx, Ground(gx, gz), gz, beta });
                    }
                    // the pits: where the opponents' cars wait while the player qualifies (RaceTrackBuilder.PlacePits, in the pit lane
                    // beside the start line). The build parks them there; the race-track mode puts them on the grid for the race.
                    foreach (var p in report.Pits)
                    {
                        var wpx = p.X * 512; var wpz = p.Z * 512;
                        if ((int)Math.Floor(wpx / IslandFile.CubeSize) != cx || (int)Math.Floor(wpz / IslandFile.CubeSize) != cz) continue;
                        int px = (int)Math.Round(wpx - cx * (double)IslandFile.CubeSize), pz = (int)Math.Round(wpz - cz * (double)IslandFile.CubeSize);
                        var pbeta = (int)Math.Round(Math.Atan2(p.DirX, p.DirZ) / (2 * Math.PI) * 4096); pbeta = ((pbeta % 4096) + 4096) % 4096;
                        pitSpots.Add(new[] { cx, cz, px, Ground(px, pz), pz, pbeta });
                    }
                    var pole = GridSpot(0);
                    if (buggy is not null) { buggy.X = At(pole.Back, pole.Side, false); buggy.Z = At(pole.Back, pole.Side, true); buggy.Y = Ground(buggy.X, buggy.Z); buggy.Beta = beta; }
                    // Twinsen right behind his car: he comes into the scene facing the way the lap runs (the scene's start keeps no
                    // facing), so he faces the car and the action key gets him in. (Beside it, 1.3 cells from its middle, the car's box
                    // pushed him off and he faced away from it.) The next car is on the other side, clear of him.
                    model.Hero.X = At(pole.Back + 1.8, pole.Side, false); model.Hero.Z = At(pole.Back + 1.8, pole.Side, true); model.Hero.Y = Ground(model.Hero.X, model.Hero.Z) + 100;
                    model.Hero.Beta = beta;
                    startScene = scene;
                    // the opponents wait in the pit lane (the race-track mode puts them on the grid when the race is about to start); with
                    // no pit lane they stand on the grid spots behind the player
                    grid = Waiting(0); baldinoGrid = Waiting(1);
                    (int X, int Z, int Beta)? Waiting(int k) =>
                        pitSpots.Count > k ? (pitSpots[k][2], pitSpots[k][4], pitSpots[k][5])
                        : gridSpots.Count > k + 1 ? (gridSpots[k + 1][2], gridSpots[k + 1][4], beta) : null;
                    log.Add($"scene {scene}: Twinsen at ({model.Hero.X},{model.Hero.Y},{model.Hero.Z}) turn {beta}, buggy at ({buggy?.X},{buggy?.Y},{buggy?.Z})");
                    if (buggyIndex > 0 && StartBuggyScript(model, scene, buggyIndex, log) is { } withBuggy) model = withBuggy;
                }
            }
            // zones that would act on a car driving along the road: doors into buildings (cube changes to scenes that are not part of the island's
            // outside), hit, ladder, escalator, grid and rail zones; and, with RemoveTrackCameras, the fixed cameras (type 1) the car would
            // drive into. The cube-edge changes, scenario, giver and message zones stay.
            if (options.RemoveRoadZones || options.RemoveTrackCameras)
                for (var z = model.Zones.Count - 1; z >= 0; z--)
                {
                    var zone = model.Zones[z];
                    var kind = zone.Type;
                    // (a change to another of the island's own outside scenes is the way across a cube edge: the engine holds the hero at the
                    // edge unless such a zone takes him over, so removing one walls the road off -- which is what happened on Citadel Island
                    // while this was the Desert island's scene numbers)
                    var door = kind == 0 && (zone.Num < options.Island.FirstScene || zone.Num > options.Island.LastScene);
                    // (only cameras that are on from the start: one that starts off is switched on only by a cutscene's script -- the
                    // ferry's arrival, a call of the car -- which then needs it, and the car never meets it)
                    var camera = kind == 1 && options.RemoveTrackCameras && zone.Info.Length > 7 && (zone.Info[7] & 1) != 0;
                    if (!(camera || options.RemoveRoadZones && (door || kind is 3 or 6 or 7 or 8 or 9))) continue;
                    // (tested at the centres of the cells the box covers: half a cell's diagonal more makes it the box itself that counts)
                    var reach = camera ? roadReach + options.CameraMargin + Math.Sqrt(0.5) : roadReach;
                    var ox = model.CubeX * (double)IslandFile.CubeSize; var oz = model.CubeY * (double)IslandFile.CubeSize;
                    var hit = false;
                    for (var cz = (int)Math.Floor((oz + zone.Z0) / 512); cz <= (int)Math.Floor((oz + zone.Z1) / 512) && !hit; cz++)
                    for (var cx = (int)Math.Floor((ox + zone.X0) / 512); cx <= (int)Math.Floor((ox + zone.X1) / 512) && !hit; cx++)
                        if (distanceToRoad(cx + 0.5, cz + 0.5) <= reach) hit = true;
                    if (!hit) continue;
                    log.Add(camera ? $"scene {scene}: fixed camera zone {z} (number {zone.Num}) reaches the track and is removed"
                                   : $"scene {scene}: zone {z} (type {kind}, number {zone.Num}) lies on the road and is removed");
                    SceneOps.DeleteZone(model, z);
                    if (camera) camerasRemoved++; else zonesRemoved++;
                }
            if (racer is not null)
            {
                SceneActorModel Car(int body, (int X, int Z, int Beta)? at)
                {
                    var car = racer.Clone();
                    car.Body = body;
                    car.Flags = OpponentFlags; car.Move = 0; car.Anim = 0; car.Life = new byte[] { 0 }; car.Track = new byte[] { 0 };
                    car.X = IslandFile.CubeSize / 2; car.Z = IslandFile.CubeSize / 2; car.Y = -20000; car.Beta = 0;
                    if (at is { } g) { car.X = g.X; car.Z = g.Z; car.Beta = g.Beta; car.Y = report.GroundAfter is { } ga ? (int)Math.Round(ga(model.CubeX * 64 + g.X / 512.0, model.CubeY * 64 + g.Z / 512.0)) : 0; }
                    return car;
                }
                opponent[scene] = SceneOps.AddActor(model, Car(0, grid));
                // Baldino's car: the racer's entity with its body 1 (RaceTrackBaldinoCar), so the racer's animations drive it
                if (options.AddBaldino) baldino[scene] = SceneOps.AddActor(model, Car(RaceTrackBaldinoCar.Generic, baldinoGrid));
                grid = null; baldinoGrid = null;
            }
            if (jump is not null && model.CubeX == jump.CubeX && model.CubeY == jump.CubeZ)
            {
                var jumped = AddJump(model, scene, jump, log);
                if (jumped is not null) model = jumped;
            }
            edgeZonesAdded += CoverEdges(model, scene, edges, log);
            changes.Add(new SceneChange(scene, model, null));
        }
        if (edges.Count > 0) log.Add($"the lap crosses {edges.Count / 2} cube edges; {edgeZonesAdded} crossing zones added where the island's own did not cover the road");
        if (changes.Count > 0) store.SaveMany(changes, allowErrors: true);
        log.Add($"{zonesRemoved} zones on the road removed");
        if (options.RemoveTrackCameras) log.Add($"{camerasRemoved} fixed camera zones along the track removed");
        if (opponent.Count > 0) log.Add($"the opponent: a copy of the retail track's racer in {opponent.Count} scenes");
        if (baldino.Count > 0) log.Add($"Baldino: a copy of his car in {baldino.Count} scenes");
        if (gridSpots.Count > 0) log.Add($"the grid: {gridSpots.Count} spots, pole {GridFirst} cells behind the start line, each {GridStep} behind the last, {GridSide} either side of the middle");
        if (pitSpots.Count > 0) log.Add($"the pits: {pitSpots.Count} spots in the pit lane, where the opponents wait while the player qualifies");
        return new Result(log, changes.Count, removed, opponent, startScene, baldino, gridSpots, pitSpots);
    }

    // Where the scripts that stay (Twinsen's own life script, mostly) refer to actors about to be removed -- "if Twinsen is near the
    // shopkeeper and presses Action, talk, then send the shopkeeper's track to @45" -- those references need somewhere harmless to go.
    // Pointing them at Twinsen (actor 0) made his own scripts act on him: in scene 67 every press of Action (which is also how he gets
    // into the car) found him at distance 0 from "the shopkeeper", made him speak and jumped his own track script to a foreign
    // offset. The stand-in is an invisible actor with no body and no shadow, 20000 below the ground (the engine's distance() and
    // distance_message() give 32000, "far away", when the heights differ by 1500 or more), whose life and track scripts are a single
    // END (TidyStandIn sends every jump into them to that END). Added at the end of the list; returns its index before the
    // removals, or null when nothing that stays refers to a removed actor.
    private static int? AddStandIn(SceneModel model, List<int> doomed)
    {
        if (doomed.Count == 0) return null;
        var gone = doomed.ToHashSet();
        var referred = doomed.Any(i => SceneOps.ReferencesTo(model, ArgRole.Obj, i).Any(r => !gone.Contains(r.Actor)));
        if (!referred || model.Actors.Count >= SceneValidator.MaxObjects) return null;
        // where it stands across the ground: where the removed actor Twinsen's script turns him towards (set_dir(follow, n) -- the car
        // he calls, the bell he rings, the telescope) stood, so he still faces the right way
        var (sx, sz) = (512, 512);
        using (Opcodes.Use(Opcodes.Lba2))
        {
            var code = Bytecode.DecodeLife(model.Hero.Life, out var failure);
            if (failure is null)
                foreach (var ins in code)
                {
                    var def = Opcodes.Life(ins.Op);
                    if (def?.Form != LifeForm.Dir || ins.A.Length <= def.Args.Length || ins.A[def.Args.Length - 1] != 2) continue;
                    var followed = (int)ins.A[def.Args.Length];
                    if (!gone.Contains(followed)) continue;
                    (sx, sz) = (model.Actors[followed].X, model.Actors[followed].Z);
                    break;
                }
        }
        var standIn = SceneOps.BlankActor(SceneGame.Lba2, sx, -20000, sz, entity: 16);
        standIn.Life = new byte[] { 0 }; standIn.Track = new byte[] { 0 };
        standIn.Flags = InvisibleNoShadow; standIn.Body = -1; standIn.Armor = 51; standIn.LifePoints = -1; standIn.CoulObj = 4;
        return SceneOps.AddActor(model, standIn);
    }

    // INVISIBLE | NO_SHADOW, as the retail invisible helper actors ("Dots") have: a script's BODY_OBJ can give the stand-in a body.
    private const uint InvisibleNoShadow = 0x1200;
    // OBJ_FALLABLE: the actor falls onto the ground below it.
    private const uint Fallable = 0x0800;

    // After the removals: every set_track_obj / set_comportement_obj the kept scripts aim at the stand-in goes to offset 0 (its one
    // END), and a camera told to follow it follows Twinsen instead -- it stands 20000 below the ground, and a cutscene's
    // cam_follow blanked the screen. (Its scripts could be ENDs as long as the removed ones' instead, but the editor can't show
    // such a script as text.)
    private static void TidyStandIn(SceneModel model, int standIn)
    {
        using var scope = Opcodes.Use(Opcodes.Lba2);
        for (var n = 0; n < model.Actors.Count; n++)
        {
            var actor = model.Actors[n];
            if (n == standIn || actor.Life.Length == 0) continue;
            var code = Bytecode.DecodeLife(actor.Life, out var failure);
            if (failure is not null) continue;
            var changed = false;
            foreach (var ins in code)
            {
                if (ins.A.Length < 1 || ins.A[0] != standIn) continue;
                var name = Opcodes.Life(ins.Op)?.Name;
                if (name is "SET_TRACK_OBJ" or "SET_COMPORTEMENT_OBJ" && ins.A.Length > 1 && ins.A[1] != 0) { ins.A[1] = 0; changed = true; }
                else if (name == "CAM_FOLLOW") { ins.A[0] = 0; changed = true; }
            }
            if (changed) actor.Life = Bytecode.EncodeLife(code);
        }
    }

    // The actors a normal game can't do without: the ones Twinsen's own life script waits on (a test of their track's label,
    // l_track_obj(n) == k) or points the camera at (cam_follow(n)) -- the ferry, the Dino-Fly and their helpers, in the cutscenes
    // of arriving on and leaving the island. With them removed, arriving on the island by ferry or Dino-Fly, or leaving it, left
    // the player stuck for good. Not the actors that drive them: in scene 60 that would keep the game ending's whole cast -- the
    // Temple Park guard waits for its hidden director and the bowl players, who play petanque beside the road in normal play --
    // so the ending (played in scene 60) doesn't finish on a race track build; putting the original files back undoes it.
    private static HashSet<int> CutsceneActors(SceneModel model)
    {
        var needed = new HashSet<int>();
        var count = model.Actors.Count;
        using (Opcodes.Use(Opcodes.Lba2))
        {
            var code = Bytecode.DecodeLife(model.Hero.Life, out var failure);
            if (failure is not null) return needed;
            foreach (var ins in code)
            {
                var def = Opcodes.Life(ins.Op);
                if (def is null) continue;
                if (def.Name == "CAM_FOLLOW" && ins.A.Length > 0 && ins.A[0] > 0 && ins.A[0] < count) needed.Add((int)ins.A[0]);
                if (def.Form is LifeForm.Cond or LifeForm.Switch && Opcodes.Cond(ins.Func)?.Name == "L_TRACK_OBJ" && ins.FuncArg > 0 && ins.FuncArg < count
                    && (def.Form == LifeForm.Switch || ins.Test == 0))
                    needed.Add(ins.FuncArg);
            }
        }
        return needed;
    }

    // Track points, actors and Twinsen's start that stood on ground the build reshaped go with the ground (keeping their height
    // above it): left where the ground used to be they end up buried -- scene 57's buggy recovery point (42) was 911 under the new
    // road, and a buggy put there by its script showed only as a shadow. Only what stood on drawn ground, and only actors that
    // fall onto the ground (OBJ_FALLABLE): the sea is height 0 too, and the harbour ferry and its route, which the water
    // bridge's causeway now crosses, were lifted 1300 above the water.
    private static void Reseat(SceneModel model, Func<double, double, double> before, Func<double, double, double> after, Func<double, double, bool> wasGround, int scene, List<string> log)
    {
        var ox = model.CubeX * 64.0; var oz = model.CubeY * 64.0;
        int Rise(int x, int y, int z)
        {
            var cx = ox + x / 512.0; var cz = oz + z / 512.0;
            if (!wasGround(cx, cz)) return 0;
            var b = before(cx, cz); var a = after(cx, cz);
            return Math.Abs(a - b) > 100 && Math.Abs(y - b) < 400 ? (int)Math.Round(a - b) : 0;
        }
        var moved = 0;
        for (var i = 0; i < model.TrackPoints.Count; i++)
        {
            var p = model.TrackPoints[i]; var d = Rise(p.X, p.Y, p.Z);
            if (d != 0) { model.TrackPoints[i] = p with { Y = p.Y + d }; moved++; }
        }
        foreach (var a in model.Actors)
        {
            if ((a.Flags & Fallable) == 0 && a != model.Hero) continue;
            var d = Rise(a.X, a.Y, a.Z);
            if (d != 0) { a.Y += d; moved++; }
        }
        if (moved > 0) log.Add($"scene {scene}: {moved} track points and actors moved up or down with the reshaped ground");
    }

    // Scene 67, the start: the buggy stands on the start line whenever the scene starts on foot (INIT_BUGGY 2 puts the game's one
    // buggy at the actor's own place; the island's scenes run INIT_BUGGY 0, which only shows it where it already is) -- but not
    // when Twinsen drives back into the scene on the next lap: forcing it then parked a second, solid buggy on the start line
    // for the car to crash into.
    private static SceneModel? StartBuggyScript(SceneModel model, int scene, int buggy, List<string> log)
    {
        try
        {
            var scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
            var text = scripts.GetText(buggy, ScriptKind.Life);
            var pattern = new System.Text.RegularExpressions.Regex(@"init_buggy\(0\);\s*if \(12 == comportement_hero\(\)\)\s*\{\s*set_comportement\(comportement_2\);\s*\}\s*else\s*\{\s*set_comportement\(comportement_1\);\s*\}");
            if (!pattern.IsMatch(text)) { log.Add($"scene {scene}: the buggy's script isn't the expected one; it is left to show the buggy where it is"); return null; }
            text = pattern.Replace(text, "if (12 == comportement_hero())\n        {\n            init_buggy(0);\n            set_comportement(comportement_2);\n        }\n        else\n        {\n            init_buggy(2);\n            set_comportement(comportement_1);\n        }", 1);
            scripts.SetText(buggy, ScriptKind.Life, text);
            var built = scripts.Build();
            if (!built.Ok) { foreach (var e in built.Errors) log.Add($"scene {scene}: buggy script: {e}"); return null; }
            log.Add($"scene {scene}: the buggy is put on the start line whenever the scene starts on foot (INIT_BUGGY 2), not when Twinsen drives in");
            return SceneSerializer.Parse(SceneGame.Lba2, built.Record!);
        }
        catch (Exception error) when (error is ScriptCompileException or InvalidDataException or ArgumentException or InvalidOperationException)
        {
            log.Add($"scene {scene}: the buggy's start could not be set: {error.Message}");
            return null;
        }
    }

    // The retail "car jump" (scene 62 of the Desert island) is not physics -- the ground is one height map and a falling object does not move
    // sideways -- but a scripted flight: the hero's own life script watches for the buggy (behaviour 12) in a scenario zone while heading
    // roughly the right way, then sets the hero's movement to 12 (MOVE_BUGGY, moved by its animation alone) and starts a track that plays
    // animation 67 (ANIM.HQR 51: about 14 cells forward, up and down), and when the track reaches its last label gives the controls back
    // (movement 13). Here a small actor does what the retail hero script does, so no scene's own hero script has to be edited; the hero's
    // track script only gets the two labels.
    private static SceneModel? AddJump(SceneModel model, int scene, JumpInfo jump, List<string> log)
    {
        var ox = jump.CubeX * 64.0; var oz = jump.CubeZ * 64.0;
        var y = (int)Math.Round(jump.Height);
        var template = model.Zones.FirstOrDefault(z => z.Type == 2);
        foreach (var b in jump.Boxes)
        {
            var zone = template?.Clone() ?? new SceneZoneModel { Info = new int[8] };
            zone.Type = 2; zone.Num = jump.Zone; zone.Info = new int[8]; zone.Info[7] = 1;
            zone.X0 = (int)((b.X0 - ox) * 512); zone.X1 = (int)((b.X1 - ox) * 512) - 1;
            zone.Z0 = (int)((b.Z0 - oz) * 512); zone.Z1 = (int)((b.Z1 - oz) * 512) - 1;
            zone.Y0 = y - 300; zone.Y1 = y + 900;
            model.Zones.Add(zone);
        }
        var controller = SceneOps.BlankActor(SceneGame.Lba2, (int)((jump.StartX - ox) * 512), y, (int)((jump.StartZ - oz) * 512), entity: 16);
        controller.Life = new byte[] { 0 }; controller.Track = new byte[] { 0 };
        controller.Flags = InvisibleNoShadow; controller.Body = -1; controller.Armor = 51; controller.LifePoints = -1; controller.CoulObj = 4;
        var index = SceneOps.AddActor(model, controller);

        // the turn window: 640 units (56 degrees) either side of the road's heading
        var lo = ((jump.Beta - 640) % 4096 + 4096) % 4096; var hi = (jump.Beta + 640) % 4096;
        var window = lo < hi ? $"{lo} < beta_obj(0) && {hi} > beta_obj(0)" : $"{lo} < beta_obj(0) || {hi} > beta_obj(0)";
        const int startLabel = 90, endLabel = 91;
        var life = $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if (12 == comportement_hero() && {jump.Zone} == zone_obj(0))
    {{
        if ({window})
        {{
            set_dir_obj(0, 12);
            set_track_obj(0, label_{startLabel});
            set_comportement(comportement_2);
        }}
    }}
}}

void comportement_2()
{{
    if ({endLabel} == l_track_obj(0))
    {{
        set_dir_obj(0, 13);
        set_comportement(comportement_1);
    }}
}}
";
        try
        {
            var scripts = LBAAssembler.LbaScript.SceneScripts.Load(SceneSerializer.Write(model), scene);
            var heroTrack = scripts.GetText(0, LBAAssembler.LbaScript.ScriptKind.Track).TrimEnd();
            if (heroTrack.Contains($"label({startLabel})") || heroTrack.Contains($"label({endLabel})")) { log.Add($"scene {scene}: the hero's track script already uses labels {startLabel}/{endLabel}; no jump"); return null; }
            heroTrack += $@"

label({startLabel});
beta({jump.Beta});
anim({jump.Anim});
wait_anim();
anim(0);

label({endLabel});
stop();
";
            scripts.SetText(0, LBAAssembler.LbaScript.ScriptKind.Track, heroTrack);
            scripts.SetText(index, LBAAssembler.LbaScript.ScriptKind.Life, life);
            var built = scripts.Build();
            if (!built.Ok) { foreach (var e in built.Errors) log.Add($"scene {scene}: jump script: {e}"); return null; }
            log.Add($"scene {scene}: jump added -- {jump.Boxes.Count} zone boxes numbered {jump.Zone}, controller actor {index}, hero track labels {startLabel}/{endLabel}, heading turn {jump.Beta}");
            return SceneSerializer.Parse(SceneGame.Lba2, built.Record!);
        }
        catch (Exception error) when (error is LBAAssembler.LbaScript.ScriptCompileException or InvalidDataException or ArgumentException)
        {
            log.Add($"scene {scene}: the jump could not be built: {error.Message}");
            return null;
        }
    }
}
