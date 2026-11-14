using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The Esmers' hangar's props (RaceTrackStory.Hangar): the freighter -- the game's Esmer saucer (BODY 71, entity 47's), as one still body,
// a cargo hatch in its east side and a ramp down from it to the pad -- and Twinsen's buggy (BODY 219, entity 152's) as a prop the story can
// drive up into it (the game's buggy actor is the engine's own outside: here a body like any other). Each the game's polygons where its
// bones put them standing, on one bone (as StellaPosters' performers).
internal static class HangarProps
{
    public const int Freighter = 88, FreighterBuggy = 87;
    private const int SaucerBody = 71, BuggyBody = 219;
    // the hatch: its foot HatchY over the pad, HatchX out east from the saucer's middle (just outside its widest, 2000); the ramp down from it
    // to the pad, RampX out
    // (the saucer made 1.2 times the game's: 4800 across, 2900 high -- the freighter)
    public const float Scale = 1.2f;
    public const float HatchX = 2010 * Scale, HatchY = 420 * Scale, HatchHigh = 760 * Scale, HatchHalf = 400 * Scale, RampX = 2900;
    private const int RampEdge = 106;
    // (the ramp's metal grey and the doorway's dark: the palette's nearest)
    private static int Ramp = 7, HatchDark = 1;

    public static List<string> Install(string gameDirectory)
    {
        var bodies = HqrArchive.Open(Path.Combine(gameDirectory, "BODY.HQR"));
        var saucer = bodies.Read(SaucerBody); var buggy = bodies.Read(BuggyBody);
        var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
        Ramp = Citadel.CitadelMuseum.Nearest(palette, 112, 114, 122);
        HatchDark = Citadel.CitadelMuseum.Nearest(palette, 16, 16, 22);
        return Citadel.CitadelProps.InstallMore(gameDirectory, "the Esmers' hangar", new (int, string, Func<byte[], Body>)[]
        {
            (Freighter, "the Esmers' freighter", h => BuildFreighter(h, saucer)),
            (FreighterBuggy, "Twinsen's buggy, to load", h => Still(h, "Twinsen's buggy", buggy, 1).ToBody(h)),
        });
    }

    // A game body's polygons, lines and spheres as it stands, on one bone
    private static CarMesh Still(byte[] header, string name, byte[] source, float scale)
    {
        var body = Body.Read(source, 2, allowStatic: true);
        var world = body.World();
        var m = new CarMesh(name, new[] { -1 });
        m.Light = 0.85f;
        var handles = world.Select(v => m.P(0, v * scale)).ToArray();
        foreach (var f in body.Faces) m.Raw(f.Points.Select(i => handles[i]).ToArray(), f.Colour & 0xFF, unlit: false);
        foreach (var l in body.Lines) m.Line(handles[l.A], handles[l.B], l.Colour & 0xFF);
        foreach (var s in body.Spheres) m.Sphere(handles[s.Point], (int)(s.Radius * scale), s.Colour & 0xFF);
        return m;
    }

    private static Body BuildFreighter(byte[] header, byte[] saucer)
    {
        var m = Still(header, "the Esmers' freighter", saucer, Scale);
        // the hatch: a dark doorway on its east side, framed
        var out_ = new Vector3(4000, HatchY + HatchHigh / 2, 0);
        m.In(new[] { m.P(0, new(HatchX, HatchY, -HatchHalf)), m.P(0, new(HatchX, HatchY, HatchHalf)), m.P(0, new(HatchX, HatchY + HatchHigh, HatchHalf)), m.P(0, new(HatchX, HatchY + HatchHigh, -HatchHalf)) }, HatchDark, out_, unlit: true);
        m.Box(0, new Vector3(HatchX + 10, HatchY + HatchHigh + 25, 0), new Vector3(40, 50, 2 * HatchHalf + 100), RampEdge);
        foreach (var s in new[] { -1, 1 }) m.Box(0, new Vector3(HatchX + 10, HatchY + HatchHigh / 2, s * (HatchHalf + 25)), new Vector3(40, HatchHigh, 50), RampEdge);
        // the ramp from its foot down to the pad, its edges
        var a = m.P(0, new(HatchX, HatchY, -HatchHalf)); var b = m.P(0, new(HatchX, HatchY, HatchHalf));
        var c = m.P(0, new(RampX, 0, HatchHalf)); var d = m.P(0, new(RampX, 0, -HatchHalf));
        m.Up(new[] { a, b, c, d }, Ramp);
        m.Up(new[] { d, c, b, a }, Ramp);
        foreach (var s in new[] { -1, 1 })
            m.Plate(0, RampEdge, new(HatchX, HatchY + 60, s * HatchHalf), new(RampX, 60, s * HatchHalf), new(RampX, 0, s * HatchHalf), new(HatchX, HatchY, s * HatchHalf));
        return m.ToBody(header);
    }

    // (its box: the saucer's own, not out over the ramp -- Twinsen walks up the ramp to the hatch)
    public static short[]? Box(int generic) => generic == Freighter ? new short[] { -2400, 0, -2400, (short)(HatchX - 450), 2900, 2400 } : null;
}
