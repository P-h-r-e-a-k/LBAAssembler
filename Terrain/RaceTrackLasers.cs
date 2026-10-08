using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The laser cars' bolts (the user, 2026-10-08: "Create extra car models for Twinsen, one that fires green lasers, and one that fires red"):
// the laser pistol's own bolt (OBJFIX.HQR 63: a thin prism 560 long in the green ramp's colour 137), made bigger to be seen from the race
// camera, once in green and once in red, appended to OBJFIX.HQR. The race-track mode flies them out of the front of the car (RACEMOD.CPP
// laser_models=, drive_laser=).
internal static class RaceTrackLasers
{
    public const int PistolBolt = 63;
    public const int GreenCore = 138, GreenEdge = 135, RedCore = 70, RedEdge = 67;
    public const float Half = 58, Length = 640;

    // A bolt: a prism along z, its core a lighter colour than its edges (flat colours, drawn as they are).
    public static Body Build(int core, int edge)
    {
        var points = new List<Vector3>();
        var faces = new List<Face>();
        int P(Vector3 v) { points.Add(v); return points.Count - 1; }
        int[] Ring(float z, float r) => new[] { P(new(-r, -r, z)), P(new(r, -r, z)), P(new(r, r, z)), P(new(-r, r, z)) };
        var back = Ring(-Length, Half * 0.6f); var middle = Ring(0, Half); var front = Ring(Length, Half * 0.6f);
        var tail = P(new(0, 0, -Length - 90)); var tip = P(new(0, 0, Length + 90));
        for (var k = 0; k < 4; k++)
        {
            var n = (k + 1) % 4;
            faces.Add(new Face(new[] { back[k], back[n], middle[n], middle[k] }, k % 2 == 0 ? core : edge, Material: 0));
            faces.Add(new Face(new[] { middle[k], middle[n], front[n], front[k] }, k % 2 == 0 ? core : edge, Material: 0));
            faces.Add(new Face(new[] { front[k], front[n], tip }, core, Material: 0));
            faces.Add(new Face(new[] { back[n], back[k], tail }, edge, Material: 0));
        }
        // (seen from every side: each face again the other way round)
        foreach (var f in faces.ToList()) faces.Add(new Face(f.Points.Reverse().ToArray(), f.Colour, Material: 0));
        var body = new Body { Game = 2 };
        body.Bones.Add(new Bone(0, points.Count, 0, -1, new byte[8]));
        body.Vertices.AddRange(points);
        body.SetWorld(points.ToArray());
        body.Faces.AddRange(faces);
        body.Validate();
        return body;
    }

    // Into the game folder: the green bolt and the red one appended to OBJFIX.HQR (the pistol bolt's header). Their indices, and a line for
    // the log.
    public static (int Green, int Red, string Log) Install(string gameDirectory)
    {
        var path = Path.Combine(gameDirectory, "OBJFIX.HQR");
        var source = Body.Read(HqrArchive.Open(path).Read(PistolBolt), 2, allowStatic: true);
        var bytes = File.ReadAllBytes(path);
        var green = HqrArchive.CountEntries(path);
        var models = new List<int>();
        foreach (var (core, edge) in new[] { (GreenCore, GreenEdge), (RedCore, RedEdge) })
        {
            var bolt = Build(core, edge);
            bolt.Header = source.Header;
            bolt.Static = source.Static;
            bytes = HqrWriter.AppendEntry(bytes, HqrWriter.StoredEntry(bolt.Write()));
        }
        File.WriteAllBytes(path, bytes);
        return (green, green + 1, $"the laser cars' bolts: OBJFIX.HQR entries {green} (green) and {green + 1} (red)");
    }
}
