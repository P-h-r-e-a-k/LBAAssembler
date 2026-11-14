using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// Stella Entities' posters of its performers, for its office's walls (StellaRooms) -- the user, 2026-11-13: "let's also replace the
// picture on the back wall ... Let's have the new pictures be of some of our strippers". Each a prop (entity 17's body): a dark board in a
// brass frame, a pink neon edge, and on it the performers themselves -- their own bodies (StellaPerformers.Make), posed as they dance
// (a key of their routine), pressed flat against the board, as a pin-up's print. The board's face +z, its foot at 0, its middle across at 0.
internal static class StellaPosters
{
    public const int PosterBack = 91, PosterSide = 90;
    private const int Frame = 106;
    // (the board a deep magenta, its edge neon pink: the palette's nearest)
    private static int Board = 211, Neon = 79;
    // (the performers on each, StellaPerformers.All's, with the key of their stage routine: 0 hands on hips, 22 arms up)
    private static readonly (int Who, int Key)[] BackCast = { (0, 0), (2, 22) };
    private static readonly (int Who, int Key)[] SideCast = { (5, 22) };
    public const float BackWide = 1300, SideWide = 800, High = 1000;

    public static List<string> Install(string gameDirectory)
    {
        var bodies = HqrArchive.Open(Path.Combine(gameDirectory, "BODY.HQR"));
        var made = StellaPerformers.All.Select(s => StellaPerformers.Make(s, bodies.Read(s.Source))).ToArray();
        var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
        Board = Citadel.CitadelMuseum.Nearest(palette, 70, 18, 64);
        Neon = Citadel.CitadelMuseum.Nearest(palette, 255, 90, 200);
        return Citadel.CitadelProps.InstallMore(gameDirectory, "Stella Entities' posters", new (int, string, Func<byte[], Body>)[]
        {
            (PosterBack, "a poster of the Grobo and the Quetch", h => Poster(h, made, BackCast, BackWide)),
            (PosterSide, "a poster of the Franco", h => Poster(h, made, SideCast, SideWide)),
        });
    }

    private static Body Poster(byte[] header, byte[][] made, (int Who, int Key)[] cast, float wide)
    {
        var m = new CarMesh("a poster of Stella Entities' performers", new[] { -1 });
        m.Light = 0.8f;
        // the board, its neon edge just in front of it, the frame round it
        m.Flat(0, Board, new Vector3(-wide / 2, 0, 0), new Vector3(wide / 2, 0, 0), new Vector3(wide / 2, High, 0), new Vector3(-wide / 2, High, 0));
        const float Edge = 26, Bar = 50;
        foreach (var (a, b) in new[] { (new Vector2(-wide / 2 + Bar, Bar), new Vector2(wide / 2 - Bar, Bar + Edge)), (new Vector2(-wide / 2 + Bar, High - Bar - Edge), new Vector2(wide / 2 - Bar, High - Bar)),
                                       (new Vector2(-wide / 2 + Bar, Bar), new Vector2(-wide / 2 + Bar + Edge, High - Bar)), (new Vector2(wide / 2 - Bar - Edge, Bar), new Vector2(wide / 2 - Bar, High - Bar)) })
            m.Flat(0, Neon, new Vector3(a.X, a.Y, 4), new Vector3(b.X, a.Y, 4), new Vector3(b.X, b.Y, 4), new Vector3(a.X, b.Y, 4));
        m.Box(0, new Vector3(0, Bar / 2, 10), new Vector3(wide, Bar, 30), Frame);
        m.Box(0, new Vector3(0, High - Bar / 2, 10), new Vector3(wide, Bar, 30), Frame);
        foreach (var s in new[] { -1, 1 }) m.Box(0, new Vector3(s * (wide / 2 - Bar / 2), High / 2, 10), new Vector3(Bar, High, 30), Frame);
        // the performers, side by side, each scaled to the board's height inside the frame
        var slot = (wide - 2 * Bar) / cast.Length;
        for (var c = 0; c < cast.Length; c++)
        {
            var (who, key) = cast[c];
            var spec = StellaPerformers.All[who];
            var body = Body.Read(made[who], 2, allowStatic: true);
            var anim = StellaPerformers.Animation(spec, made[who], 0, StellaPerformers.StageRoutine());
            var world = Lba1.Lba1Pose.World(body, anim.PoseOf(Math.Min(key, anim.Keys.Count - 1)));
            float minX = world.Min(v => v.X), maxX = world.Max(v => v.X), minY = world.Min(v => v.Y), maxY = world.Max(v => v.Y);
            var scale = Math.Min((High - 2 * Bar - 80) / (maxY - minY), (slot - 40) / (maxX - minX));
            var middle = -wide / 2 + Bar + slot * (c + 0.5f);
            var handles = world.Select(v => m.P(0, new Vector3(middle + (v.X - (minX + maxX) / 2) * scale, Bar + 40 + (v.Y - minY) * scale, 20 + v.Z * 0.03f))).ToArray();
            foreach (var f in body.Faces) m.Raw(f.Points.Select(i => handles[i]).ToArray(), f.Colour & 0xFF, unlit: false);
            foreach (var l in body.Lines) m.Line(handles[l.A], handles[l.B], l.Colour & 0xFF);
            foreach (var s in body.Spheres) m.Sphere(handles[s.Point], Math.Max(4, (int)(s.Radius * scale)), s.Colour & 0xFF);
        }
        return m.ToBody(header);
    }
}
