using System.Numerics;
using LbaBodyStudio;

namespace ScriptRoundTrip;

// bodynormals <BODY.HQR> <entry>...: a lit body's smooth normals (Body.VertexNormals, what Write stores) against each polygon's own -- a
// polygon whose corners' normals point away from it is drawn dark (black where they point straight back), the racing suit's black
// patches, 2026-11-24. Per entry: points with no normal (their polygons' normals cancel), polygons with a corner turned away (dot < 0),
// by colour, and the worst ones' corners.
internal static class BodyNormalsCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 3) { Console.WriteLine("bodynormals <BODY.HQR> <entry>..."); return 1; }
        var hqr = LBAAssembler.HqrArchive.Open(args[1]);
        foreach (var entry in args.Skip(2).Select(int.Parse))
        {
            var body = Body.Read(hqr.Read(entry), 2);
            var normals = body.VertexNormals();
            var world = body.World();
            var zero = normals.Count(n => n == Vector3.Zero);
            var away = new List<(int Face, int Colour, float Worst)>();
            for (var i = 0; i < body.Faces.Count; i++)
            {
                var f = body.Faces[i];
                var n = Vector3.Zero;
                for (var k = 0; k < f.Points.Length; k++)
                {
                    var a = world[f.Points[k]]; var b = world[f.Points[(k + 1) % f.Points.Length]];
                    n += new Vector3((a.Y - b.Y) * (a.Z + b.Z), (a.Z - b.Z) * (a.X + b.X), (a.X - b.X) * (a.Y + b.Y));
                }
                if (n.LengthSquared() < 1e-9f) continue;
                n = Vector3.Normalize(n);
                var worst = f.Points.Min(p => normals[p] == Vector3.Zero ? -2f : Vector3.Dot(normals[p], n));
                if (worst < 0) away.Add((i, f.Colour, worst));
            }
            Console.WriteLine($"entry {entry}: {body.Vertices.Count} points, {body.Faces.Count} polygons, lit {body.Lit}; {zero} points with no normal; {away.Count} polygons with a corner turned away");
            foreach (var g in away.GroupBy(a => a.Colour).OrderByDescending(g => g.Count()))
                Console.WriteLine($"  colour {g.Key}: {g.Count()} polygons (worst {g.Min(x => x.Worst):0.00})");
        }
        return 0;
    }
}
