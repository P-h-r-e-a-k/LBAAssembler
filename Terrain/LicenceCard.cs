using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The racing licence's model in the inventory (OBJFIX.HQR 38, the burgomaster's note's -- RaceTrackStory.Licence.cs): a card, as Twinsen's
// identity card was in the first game (the user, 2026-11-14: "can we create a new object, perhaps with a picture of Twinsen on it,
// similar to his I.D. card in LBA1"). Lying flat as the ferry ticket does (its face up, +y; its top edge -z): cream, a red band along
// its top with a strip of chequers, his photo on the left -- his own body in his racing suit (CitadelOveralls), head and shoulders, pressed flat onto
// a pale blue ground, its textures kept -- his name in block letters on the right, lines of print under it, and a gold seal.
internal static class LicenceCard
{
    public const int Entry = RaceTrackStory.LicenceSlot, PagesEntry = RaceTrackStory.PagesSlot, HeroBody = 0;
    private const float Wide = 3000, Deep = 1900, Thick = 80, Top = Thick;
    private const float BandDeep = 300;
    private const float PhotoX0 = -1380, PhotoX1 = -330, PhotoZ0 = -520, PhotoZ1 = 820;
    // (built lying flat, face up and its top -z -- then stood up as the game's ticket and note stand, face in x-y: its top +y, its face
    // towards Facing -- a turn, not a mirror, so its polygons still face out)
    private const float Facing = 1;
    private static Vector3 Stand(Vector3 v) => Facing > 0 ? new Vector3(v.X, -v.Z, v.Y) : new Vector3(-v.X, -v.Z, -v.Y);
    private static int[] In(CarMesh m, Vector3[] at, int colour, bool unlit)
    {
        var hs = at.Select(v => m.P(0, Stand(v))).ToArray();
        var centre = at.Aggregate(Vector3.Zero, (a, b) => a + b) / at.Length;
        m.In(hs, colour, Stand(centre + new Vector3(0, 100, 0)), unlit);
        return hs;
    }

    // Its bytes, made from the game folder's own (the same every time: the pages' install compares)
    public static byte[] Bytes(string gameDirectory)
    {
        var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
        byte C(int r, int g, int b) => Citadel.CitadelMuseum.Nearest(palette, r, g, b);
        int cream = C(236, 226, 196), edge = C(150, 138, 112), red = C(178, 32, 30), black = C(24, 22, 24), white = C(244, 244, 240),
            sky = C(150, 190, 226), ink = C(38, 36, 46), print = C(112, 110, 120), gold = C(222, 172, 52);
        var objfix = HqrArchive.Open(Path.Combine(gameDirectory, "OBJFIX.HQR"));
        var header = Body.Read(objfix.Read(Entry), 2, allowStatic: true).Header;
        var m = new CarMesh("the racing licence", new[] { -1 });
        m.Light = 0.9f;
        // the card
        {
            float x = Wide / 2, z = Deep / 2, t = Thick - 1;
            m.In(new Vector3[] { new(-x, 0, -z), new(-x, 0, z), new(x, 0, z), new(x, 0, -z) }.Select(v => m.P(0, Stand(v))).ToArray(), edge, Stand(new Vector3(0, -100, 0)), unlit: true);   // (its back)
            var pairs = new[] { (new Vector3(-x, 0, -z), new Vector3(x, 0, -z)), (new Vector3(x, 0, -z), new Vector3(x, 0, z)), (new Vector3(x, 0, z), new Vector3(-x, 0, z)), (new Vector3(-x, 0, z), new Vector3(-x, 0, -z)) };
            foreach (var (a, b) in pairs)
            {
                var hs = new[] { a, b, b + new Vector3(0, t, 0), a + new Vector3(0, t, 0) }.Select(v => m.P(0, Stand(v))).ToArray();
                m.In(hs, edge, Stand((a + b) / 2 * 2), true);
            }
        }
        // (the printed faces unlit: lit, the engine shades a colour within its ramp from the light, and the cream came out olive)
        float Y(float lift) => Top + lift;
        void Patch(float x0, float z0, float x1, float z1, int colour, float lift) =>
            In(m, new Vector3[] { new(x0, Y(lift), z0), new(x1, Y(lift), z0), new(x1, Y(lift), z1), new(x0, Y(lift), z1) }, colour, true);
        Patch(-Wide / 2, -Deep / 2, Wide / 2, Deep / 2, cream, 0);
        // the band and its chequers
        Patch(-Wide / 2, -Deep / 2, Wide / 2, -Deep / 2 + BandDeep, red, 2);
        const int Squares = 10; const float Sq = 120;
        for (var k = 0; k < Squares; k++)
            for (var row = 0; row < 2; row++)
                if ((k + row) % 2 == 0)
                {
                    float x0 = Wide / 2 - 140 - (k + 1) * Sq, z0 = -Deep / 2 + 30 + row * Sq;
                    Patch(x0, z0, x0 + Sq, z0 + Sq, black, 4);
                }
                else
                {
                    float x0 = Wide / 2 - 140 - (k + 1) * Sq, z0 = -Deep / 2 + 30 + row * Sq;
                    Patch(x0, z0, x0 + Sq, z0 + Sq, white, 4);
                }
        // the photo: its ground, and Twinsen
        Patch(PhotoX0 - 20, PhotoZ0 - 20, PhotoX1 + 20, PhotoZ1 + 20, ink, 2);
        Patch(PhotoX0, PhotoZ0, PhotoX1, PhotoZ1, sky, 4);
        // (in his racing suit, as the story dresses him -- CitadelOveralls, from his everyday body as it does; no helmet: his face)
        Photo(m, Citadel.CitadelOveralls.WithBackPatch(Citadel.CitadelOveralls.Overalls(Body.Read(HqrArchive.Open(Path.Combine(gameDirectory, "BODY.HQR")).Read(HeroBody), 2))));
        // his name
        Word(m, "TWINSEN", -170, -500, 170, 310, 44, ink);
        // lines of print, and the seal
        foreach (var (z, len) in new[] { (-60f, 1300f), (110f, 1050f), (280f, 1200f), (450f, 800f) })
            Patch(-170, z, -170 + len, z + 55, print, 3);
        m.Ball(0, Stand(new Vector3(1150, Y(20), 640)), 150, gold);
        return m.ToBody(header).Write();
    }

    // Twinsen's head and shoulders (his body's polygons above his chest), seen from the front, laid onto the photo: his height down the card
    // (-z up), his front (+z in his body) up off it, his x across -- a turn, not a mirror, so his polygons face up (his -z up showed the
    // back of his head, the user, 2026-11-14)
    private static void Photo(CarMesh m, Body body)
    {
        var world = body.World();
        var top = world.Max(v => v.Y);
        var cut = top * 0.56f;
        var faces = body.Faces.Where(f => f.Points.All(p => world[p].Y >= cut)).ToList();
        float minX = faces.SelectMany(f => f.Points).Min(p => world[p].X), maxX = faces.SelectMany(f => f.Points).Max(p => world[p].X);
        var scale = Math.Min((PhotoZ1 - PhotoZ0 - 60) / (top - cut), (PhotoX1 - PhotoX0 - 60) / (maxX - minX));
        var middle = (PhotoX0 + PhotoX1) / 2;
        if (faces.Any(f => f.Texture is not null)) m.Textures = body.Textures;
        var handles = new Dictionary<int, int>();
        int H(int p)
        {
            if (handles.TryGetValue(p, out var h)) return h;
            var v = world[p];
            return handles[p] = m.P(0, Stand(new Vector3(middle + (v.X - (minX + maxX) / 2) * scale, Top + 12 + (v.Z + 300) * 0.04f, PhotoZ1 - 20 - (v.Y - cut) * scale)));
        }
        foreach (var f in faces) m.Raw(f.Points.Select(H).ToArray(), f.Colour, f.Material == 0 ? 0 : -1, f.Texture);
        foreach (var s in body.Spheres.Where(s => world[s.Point].Y >= cut)) m.Sphere(H(s.Point), Math.Max(6, (int)(s.Radius * scale)), s.Colour);
    }

    // Block letters: strokes on a 4 x 6 grid (y down), each a thin bar lying on the card
    private static readonly Dictionary<char, (float, float, float, float)[]> Strokes = new()
    {
        ['T'] = new[] { (0f, 0f, 4f, 0f), (2f, 0f, 2f, 6f) },
        ['W'] = new[] { (0f, 0f, 1f, 6f), (1f, 6f, 2f, 2f), (2f, 2f, 3f, 6f), (3f, 6f, 4f, 0f) },
        ['I'] = new[] { (2f, 0f, 2f, 6f), (1f, 0f, 3f, 0f), (1f, 6f, 3f, 6f) },
        ['N'] = new[] { (0f, 6f, 0f, 0f), (0f, 0f, 4f, 6f), (4f, 6f, 4f, 0f) },
        ['S'] = new[] { (4f, 0f, 0f, 0f), (0f, 0f, 0f, 3f), (0f, 3f, 4f, 3f), (4f, 3f, 4f, 6f), (4f, 6f, 0f, 6f) },
        ['E'] = new[] { (4f, 0f, 0f, 0f), (0f, 0f, 0f, 6f), (0f, 6f, 4f, 6f), (0f, 3f, 3f, 3f) },
    };
    private static void Word(CarMesh m, string word, float x, float z, float letterWide, float letterHigh, float stroke, int colour)
    {
        float u = letterWide / 4, v = letterHigh / 6;
        foreach (var ch in word)
        {
            if (Strokes.TryGetValue(ch, out var strokes))
                foreach (var (a, b, c, d) in strokes)
                {
                    var p0 = new Vector2(x + a * u, z + b * v); var p1 = new Vector2(x + c * u, z + d * v);
                    var along = Vector2.Normalize(p1 - p0); var side = new Vector2(-along.Y, along.X) * (stroke / 2);
                    var ext = along * (stroke / 2);
                    var q = new[] { p0 - ext - side, p1 + ext - side, p1 + ext + side, p0 - ext + side };
                    In(m, q.Select(w => new Vector3(w.X, Top + 6, w.Y)).ToArray(), colour, true);
                }
            x += letterWide * 1.35f;
        }
    }

    // Into OBJFIX.HQR: the note's model kept for the exam pages first (their install copies it from 38 -- and leaves it once the card is
    // there), then the card in its place
    public static List<string> Install(string gameDirectory)
    {
        var path = Path.Combine(gameDirectory, "OBJFIX.HQR");
        var card = Bytes(gameDirectory);
        var now = HqrArchive.Open(path).Read(Entry);
        if (!now.AsSpan().SequenceEqual(card))
        {
            HqrWriter.ReplaceInFile(path, PagesEntry, now);
            HqrWriter.ReplaceInFile(path, Entry, card);
        }
        return new() { $"the racing licence: OBJFIX.HQR {Entry} a card with Twinsen's photo (the note's model kept in {PagesEntry}, the exam pages')" };
    }
}
