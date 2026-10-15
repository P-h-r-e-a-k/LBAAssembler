using System.IO;

namespace LBAAssembler.Terrain.ControlTower;

// Island CX's race track (the user, 2026-10-15: "Create a track for our CXTower island. The basic concept for this track is a loop around the
// tower, the track enters the underground section through a slope down through one of the doors, which means we'll need to remove the metal
// grate that's currently there, loops around the underground section a few times then back out a door on the other side. Don't forget to
// add a pit lane. Let's have the cannons that normally fire at Twinsen outside randomly firing at any outside car"; then: "In the underground
// section there's some gas monsters that attack the player let's build on this theme for the underground section"). On CXTOWER.ILE -- Island
// CX as it is, the control tower's lower level under its gas (ControlTowerIsland) -- raced as Island CX, island 9, drawn from that file (the
// race-track mode's island_file=), in a copy of Island CX's outside scene (RaceTrackIsland.CxTower: CopiesScene). The build makes the
// island afresh (Created), its room the race track's ring (ControlTowerIsland.RaceRoom): one hall round the control room, roofed just under
// the gas, the roof open where the road comes down through it (RoofHoles). The route: docs/racetrack/cxtower_track_plan.json
// (tools/RaceTrackPlan/cxtower_design.py).
internal static class CxTowerTrack
{
    public const string PlanResource = "RaceTrackPlan.CxTower.json";
    // past the retail game's scenes and the other tracks' (the palace's from 265 on)
    public const int Scene = 300;
    public const int SourceScene = 110;                 // Island CX's outside scene
    public const int IslandByte = 9;
    // Island CX's cannons on its four corner towers (scene 110's actors 3-6): the race-track mode's hunting guns (gunner_hunt=), each burst at
    // a car it picks at random among those outside within Reach (cells) -- RaceTrackScenes keeps them in the scene, their scripts emptied
    public const int CannonEntity = 264;
    public const double CannonReach = 40;
    public const int CannonMuzzle = 520;                // the muzzle over the cannon's own place

    // The island made afresh in a game folder whose files are the originals (the race track's build: Island CX's own files from their
    // backups when it has them -- another build may have changed the folder's).
    public static List<string> Install(string gameDirectory, string backupSuffix)
    {
        var plan = RaceTrackPlan.Built(RaceTrackIsland.CxTower);
        var holes = RoofHoles(plan);
        var source = gameDirectory;
        if (File.Exists(Path.Combine(gameDirectory, ControlTowerIsland.SourceIle + backupSuffix)))
        {
            source = Path.Combine(Path.GetTempPath(), "LBAAssembler-cxtower-" + Environment.ProcessId);
            Directory.CreateDirectory(source);
            foreach (var f in new[] { ControlTowerIsland.SourceIle, ControlTowerIsland.SourceObl })
            {
                var from = Path.Combine(gameDirectory, f + backupSuffix);
                File.Copy(File.Exists(from) ? from : Path.Combine(gameDirectory, f), Path.Combine(source, f), true);
            }
        }
        try { return new() { ControlTowerIsland.Build(gameDirectory, gameDirectory, source, race: new ControlTowerIsland.RaceRoom(holes)).Log }; }
        finally { if (source != gameDirectory) try { Directory.Delete(source, true); } catch (IOException) { } }
    }

    // The room's cells the road comes down through its roof at (just under the gas, ControlTowerIsland.TopLayer + 1): where the road's tunnel
    // (the plan's Tunnels) -- from under its deck to over its roof -- reaches the roof, as wide as the tunnel's walls and a little more. (Not
    // where the road under the roof is out of its tunnel: the hole showed the island's ground from under it, 2026-10-15.)
    public const double TunnelUnder = RaceTrackTunnel.Under + 200, HoleMargin = 0.3;
    public static HashSet<(int X, int Z)> RoofHoles(RaceTrackPlan plan)
    {
        const double roofLo = ControlTowerIsland.GasLevel - ControlTowerIsland.UnderGas, roofHi = roofLo + 128;
        var holes = new HashSet<(int X, int Z)>();
        if (plan.Heights is not { } heights || heights.Length != plan.Points.Length) return holes;
        var n = plan.Points.Length;
        var roofOver = new double?[n];
        foreach (var t in (plan.Tunnels ?? Array.Empty<TunnelRun>()).Where(t => t.Pieces))   // (not one of the camera's alone)
            for (var k = t.From; ; k = (k + 1) % n) { roofOver[k] = t.Roof + RaceTrackTunnel.Slab; if (k == t.To) break; }
        for (var i = 0; i < n; i++)
        {
            var j = (i + 1) % n;
            if (roofOver[i] is not { } over || roofOver[j] is null) continue;
            double y0 = heights[i], y1 = heights[j];
            if (Math.Max(y0, y1) - TunnelUnder > roofHi || Math.Min(y0, y1) + over < roofLo) continue;
            var reach = (plan.RaisedHalfs is { } halfs && i < halfs.Length ? halfs[i] : plan.RaisedHalf ?? 4.5) + RaceTrackTunnel.WallOut + RaceTrackTunnel.Wall / 512 + HoleMargin;
            for (var t = 0.0; t <= 1.0; t += 0.25)
            {
                double x = plan.Points[i][0] + (plan.Points[j][0] - plan.Points[i][0]) * t, z = plan.Points[i][1] + (plan.Points[j][1] - plan.Points[i][1]) * t;
                for (var cz = (int)Math.Floor(z - reach); cz <= (int)Math.Floor(z + reach); cz++)
                    for (var cx = (int)Math.Floor(x - reach); cx <= (int)Math.Floor(x + reach); cx++)
                    {
                        // (a cell whose middle is in reach)
                        if ((cx + 0.5 - x) * (cx + 0.5 - x) + (cz + 0.5 - z) * (cz + 0.5 - z) <= reach * reach && cx is >= 0 and < 64 && cz is >= 0 and < 64) holes.Add((cx, cz));
                    }
            }
        }
        return holes;
    }
}
