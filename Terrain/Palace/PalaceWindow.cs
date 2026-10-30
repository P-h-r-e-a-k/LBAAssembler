using System.IO;
using System.Numerics;
using System.Text.Json;

namespace LBAAssembler.Terrain.Palace;

// The round window at the end of the palace's race (the user, 2026-10-30: "We need to tweak the end of the palace track so the exit to
// outside carries a large version of the window that's featured in game and let's have the car smash it into pieces when it exits through
// it"). The hall's round window was taken out of its wall for the cars' way out (PalaceIsland); its picture, sampled finer than the rooms'
// (PalaceFurniture.Pane, into PALACE.JSON's windowPane), stands in the opening as a model of its own. The engine draws it until the first
// car reaches it and throws its pieces (RACEMOD.CPP smash_window=), so the wall's decors don't change and it is whole again every race.
internal static class PalaceWindow
{
    private const float ShardSize = 620;
    private const double InFront = 600, Size = 0.97;    // how far into the hall it stands, and its size against the opening's
    private const double RoadHeight = 14540;            // its foot: just under the road to the window (tools/RaceTrackPlan/palace_design.py, 14,600)
    private const int MostPoints = 500;                 // (the engine's body holds 550: Body.Limit)

    // Its models appended to OBJFIX.HQR, in a folder with the palace built: the window and three shards in the window's commonest colours.
    // What smash_window= says -- the tiles' first model and how many, the three shards, the window's middle (island units), its half width
    // and height, the way out x and z -- and a
    // line for the log. (null, null: no palace in this folder.)
    public static (int[]? Smash, string? Log) Install(string gameDirectory)
    {
        var layout = Path.Combine(gameDirectory, PalaceIsland.LayoutFile);
        if (!File.Exists(layout)) return (null, null);
        using var json = JsonDocument.Parse(File.ReadAllText(layout));
        var root = json.RootElement;
        if (!root.TryGetProperty("window", out var window) || window.ValueKind != JsonValueKind.Object) return (null, null);
        if (!root.TryGetProperty("windowPane", out var pane) || pane.ValueKind != JsonValueKind.Object) return (null, "the round window: no picture of it in the palace's layout");
        int w = pane.GetProperty("w").GetInt32(), h = pane.GetProperty("h").GetInt32();
        var colours = pane.GetProperty("colours").EnumerateArray().Select(c => c.GetInt32()).ToArray();
        double x0 = window.GetProperty("x0").GetDouble() * 512, x1 = window.GetProperty("x1").GetDouble() * 512, z0 = window.GetProperty("z0").GetDouble() * 512, z1 = window.GetProperty("z1").GetDouble() * 512;
        double sill = window.GetProperty("sill").GetDouble(), top = window.GetProperty("top").GetDouble();
        int outX = (int)window.GetProperty("outX").GetDouble(), outZ = (int)window.GetProperty("outZ").GetDouble();
        // (standing on the road, in front of the hall's end wall: the road comes up to the opening half way up it, and in the opening only
        // the window's top half showed over the ramp. As big as the opening, a little smaller to clear the upstairs' floor over it)
        double x = outX != 0 ? (outX > 0 ? x0 - InFront : x1 + InFront) : (x0 + x1) / 2, z = outZ != 0 ? (outZ > 0 ? z0 - InFront : z1 + InFront) : (z0 + z1) / 2;
        var wide = (float)((outX != 0 ? z1 - z0 : x1 - x0) * Size);
        var high = (float)((top - sill) * Size);

        // (cut into tiles: a model holds MostPoints at the most, the window's detail takes a dozen of them; all drawn at its middle)
        var models = new List<byte[]>();
        var faces = 0;
        void Tile(int u0, int v0, int u1, int v1)
        {
            var mesh = Mesh(colours, w, h, wide, high, u0, v0, u1, v1);
            if (mesh.Points.Count <= MostPoints && mesh.Faces.Count <= MostPoints) { models.Add(mesh.Write()); faces += mesh.Faces.Count; return; }
            if (u1 - u0 >= v1 - v0) { var m = (u0 + u1) / 2; Tile(u0, v0, m, v1); Tile(m, v0, u1, v1); }
            else { var m = (v0 + v1) / 2; Tile(u0, v0, u1, m); Tile(u0, m, u1, v1); }
        }
        Tile(0, 0, w, h);
        var tiles = models.Count;
        var common = colours.GroupBy(c => c).OrderByDescending(g => g.Count()).Select(g => g.Key).Take(3).ToArray();
        for (var k = 0; k < 3; k++) models.Add(Shard(common[k % common.Length], k));

        var path = Path.Combine(gameDirectory, "OBJFIX.HQR");
        var first = HqrArchive.CountEntries(path);
        var bytes = File.ReadAllBytes(path);
        foreach (var m in models) bytes = HqrWriter.AppendEntry(bytes, HqrWriter.StoredEntry(m));
        File.WriteAllBytes(path, bytes);
        var shards = first + tiles;
        var smash = new[] { first, tiles, shards, shards + 1, shards + 2, (int)x, (int)(RoadHeight + high / 2), (int)z, (int)(wide / 2), (int)(high / 2), outX, outZ };
        return (smash, $"the round window: OBJFIX.HQR {first}.. ({tiles} tiles, {faces} faces, {w} x {h} samples), its shards {shards}-{shards + 2}, at ({(int)x},{(int)z})");
    }

    // The samples u0..u1, v0..v1 of the window, as merged rectangles of a colour, their corners shared.
    private static RaceTrackRaisedBody.Mesh Mesh(int[] colours, int w, int h, float wide, float high, int u0, int v0, int u1, int v1)
    {
        var grid = new Dictionary<(int U, int V), int>();
        for (var v = v0; v < v1; v++)
            for (var u = u0; u < u1; u++)
                grid[(u, v)] = colours[v * w + u];
        var mesh = new RaceTrackRaisedBody.Mesh();
        var corners = new Dictionary<(int, int), int>();
        int Corner(int u, int v)
        {
            if (corners.TryGetValue((u, v), out var known)) return known;
            return corners[(u, v)] = mesh.P(new(wide * (u / (float)w - 0.5f), high * (v / (float)h - 0.5f), 0));
        }
        foreach (var (a0, b0, a1, b1, c) in BrickBuilding.Rectangles(grid))
            mesh.Quad(Corner(a0, b0), Corner(a1 + 1, b0), Corner(a1 + 1, b1 + 1), Corner(a0, b1 + 1), c, Vector3.UnitZ);
        return mesh;
    }

    // A shard of glass: a thin jagged triangle, seen from both sides (the engine culls faces from behind).
    private static byte[] Shard(int colour, int kind)
    {
        var m = new RaceTrackRaisedBody.Mesh();
        var s = ShardSize * (1 - kind * 0.2f);
        var a = m.P(new(-s * 0.5f, -s * 0.4f, 0)); var b = m.P(new(s * (0.45f + kind * 0.1f), -s * 0.55f, 20)); var c = m.P(new(s * (0.1f - kind * 0.2f), s * 0.7f, -20));
        m.Tri(a, b, c, colour, Vector3.UnitZ);
        m.Tri(a, b, c, colour, -Vector3.UnitZ);
        return m.Write();
    }
}
