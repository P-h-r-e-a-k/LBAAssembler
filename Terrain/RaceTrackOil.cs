using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The oil slick a car drops (the power-ups' oil, RACEMOD.CPP): a puddle flat on the road -- an irregular black blob two cells across, an
// oily blue and violet sheen on it -- as body 1 of the small brown mushroom's entity (112, whose body 0 is the mushroom: a fixed object of
// one bone, as this is), so a slick is a copy of the mushroom's actor with this body (RaceTrackScenes: a few in each scene, out of sight
// until oil is dropped).
internal static class RaceTrackOil
{
    public const int Entity = 112, Generic = 1, MushroomBody = 171;
    public const double Radius = 520;
    // the colours, drawn as they are (no light): near-black, and the sheen's dark blue and violet
    private const int Black = 48, Sheen = 193, Violet = 226;

    public static Body Build()
    {
        var points = new List<Vector3>();
        var faces = new List<Face>();
        int P(Vector3 v) { points.Add(v); return points.Count - 1; }
        // a blob: a ring of points round a middle, the radius wobbling round it; its triangles facing up
        void Blob(Vector3 middle, double radius, int n, double phase, int colour)
        {
            var centre = P(middle);
            var ring = new int[n];
            for (var k = 0; k < n; k++)
            {
                var a = 2 * Math.PI * k / n;
                var r = radius * (1 + 0.18 * Math.Sin(3 * a + phase) + 0.1 * Math.Cos(5 * a + 2 * phase));
                ring[k] = P(middle + new Vector3((float)(Math.Sin(a) * r), 0, (float)(Math.Cos(a) * r)));
            }
            // (a ring point's x is sin, z cos: going round, the triangle centre -> k -> k+1 is the one facing up -- the other way round, it
            // faced down and the engine left it out, seen from above)
            for (var k = 0; k < n; k++) faces.Add(new Face(new[] { centre, ring[k], ring[(k + 1) % n] }, colour, Material: 0));
        }
        Blob(new Vector3(0, 4, 0), Radius, 16, 0.4, Black);
        Blob(new Vector3(90, 7, -70), Radius * 0.42, 9, 1.7, Sheen);
        Blob(new Vector3(-150, 9, 120), Radius * 0.22, 7, 2.9, Violet);
        Blob(new Vector3(110, 10, -40), Radius * 0.12, 6, 0.9, Sheen + 3);
        var body = new Body { Game = 2, Lit = false };
        body.Bones.Add(new Bone(0, points.Count, 0, -1, new byte[8]));
        body.Vertices.AddRange(points);
        body.SetWorld(points.ToArray());
        body.Faces.AddRange(faces);
        body.Validate();
        return body;
    }

    // Into the game folder: the slick appended to BODY.HQR as the mushroom's entity's body 1 (the mushroom's own header: a fixed object).
    // Returns a line for the log.
    public static string Install(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var slick = Build();
        slick.Header = Body.Read(HqrArchive.Open(bodyPath).Read(MushroomBody), 2, allowStatic: true).Header;
        slick.Static = true;
        var index = HqrArchive.CountEntries(bodyPath);
        File.WriteAllBytes(bodyPath, HqrWriter.AppendEntry(File.ReadAllBytes(bodyPath), HqrWriter.StoredEntry(slick.Write())));
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = RaceTrackBaldinoCar.WithBody(HqrArchive.Open(ressPath).Read(44), Entity, Generic, index);
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return $"the oil slick: BODY.HQR entry {index}, the mushroom's entity ({Entity}) body {Generic}";
    }
}
