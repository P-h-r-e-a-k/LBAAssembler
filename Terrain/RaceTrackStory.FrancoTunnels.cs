using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Francos' tunnels' edits of the island's scenes (the user, 2026-11-12: "Our race track on Franco's island cuts through some buildings so
// they're not accessible, let's build some underground tunnels that join them up"; the scene itself, 394: FrancoTunnels): the road runs over
// the refinery's two doors (scenes 140 and 143) and two village huts' (171 and 172), so their doors on the ground are taken out, each
// building's way out is the tunnels' door at its end instead, and four hatches on the ground clear of the road (Action there: down)
// take Twinsen to the tunnels' four elevators.
internal static partial class RaceTrackStory
{
    // the doors on the ground the road covers: the buildings' scenes, and the outside scenes' zones to them (the way out of each is a zone
    // into the island's scene of its door)
    private static readonly int[] TunnelBuildings = { 140, 143, 171, 172 };
    // where each hatch goes, by the building it is the way to (the island's scene, the place in it, the ground's height there -- all tried with
    // Twinsen dropped there: open ground at least five cells from the road, no rock or pipe or hut over it): the first two the huts', the last
    // two the refinery's
    private static readonly (int Scene, int X, int Y, int Z, string Near)[] TunnelHatches =
    {
        (109, 12800, 3650, 10240, "the west hut (171)"), (261, 25600, 3550, 27648, "the north hut (172)"),
        (108, 4096, 3200, 22528, "the refinery's upper door (143)"), (108, 3584, 3200, 6144, "the refinery's lower door (140)"),
    };

    // Returns the hatches' places (for FrancoTunnels.Install, once the scenes are saved), or null when there's no room for them
    private static List<FrancoTunnels.Pad>? FrancoTunnelEdits(IslandBeatsA b, RaceTrackReport report)
    {
        var pads = new List<FrancoTunnels.Pad>();
        for (var k = 0; k < TunnelHatches.Length; k++)
        {
            var (scene, lx, ly, lz, near) = TunnelHatches[k];
            var kk = k;
            if (!b.Store.SceneExists(scene)) { b.Log.Add($"the Francos' tunnels left out: no scene {scene} for a hatch by {near}"); return null; }
            pads.Add(new FrancoTunnels.Pad(scene, lx, ly, lz));
            b.Edit(scene, $"the hatch down to the tunnels' elevator {k + 1}, by {near}", model => FrancoTunnels.Hatch(model, kk, lx, ly, lz, Driving));
        }
        // the doors on the ground the road covers are taken out
        foreach (var scene in b.OutsideScenes.ToList())
            b.Edit(scene, "the buildings' doors the road covers taken out (the tunnels are the way in now)", model =>
            {
                model.Zones.RemoveAll(zn => zn.Type == 0 && TunnelBuildings.Contains(zn.Num));
                return _ => { };
            });
        // the buildings' own ways out are the tunnels' doors
        foreach (var interior in TunnelBuildings)
        {
            var exit = FrancoTunnels.Exit(interior);
            b.Edit(interior, $"the way out of the building is the tunnels' door (scene {FrancoTunnels.Scene})", model =>
            {
                foreach (var zn in model.Zones.Where(zn => zn.Type == 0 && zn.Num is 108 or 109 or 261))
                {
                    zn.Num = FrancoTunnels.Scene;
                    zn.Info[0] = exit.X; zn.Info[1] = exit.Y; zn.Info[2] = exit.Z; zn.Info[3] = 0;
                }
                return _ => { };
            });
        }
        return pads;
    }
}
