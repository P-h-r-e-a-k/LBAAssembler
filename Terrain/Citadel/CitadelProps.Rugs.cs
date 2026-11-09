using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The secret chamber's two picture rugs (the user, 2026-11-09): in the lower room "the rectangular red mat, let's make this larger and
// turn it into a mat with dinofly on it with Twinsen riding him"; in the hall "remove the circular mat and replace it with a large
// rectangular mat that fills almost the entire floor with Sendell on the mat". Each drawn upright below (row 0 its far edge, the back
// wall's side, -z; its columns along +x), a stitch a square of its Stitch units, its colours flat (no light) as the Bayeux hanging's;
// laid on the floor, its top no higher than it (an actor's box stops another's only where they overlap in height, so nobody walks into
// a rug, and the sort draws whoever stands on it after it). Cut into as many parts -- bands of columns, each a body of the static prop
// entity -- as the body's limits want (RugParts): Dinofly's from body DinoFirst, Sendell's from SendellFirst.
internal static partial class CitadelProps
{
    public const int DinoFirst = 34, DinoMost = 6, SendellFirst = 40, SendellMost = 10, DinoStitch = 64, SendellStitch = 80;
    private static readonly Dictionary<char, int> RugColours = new()
    {
        ['.'] = 72, ['R'] = 83, ['r'] = 86, ['y'] = 106, ['Y'] = 108, ['b'] = 196, ['B'] = 194, ['l'] = 204, ['L'] = 207, ['w'] = 95,
        ['g'] = 137, ['G'] = 135, ['k'] = 97, ['s'] = 41, ['c'] = 223, ['o'] = 88, ['n'] = 195, ['p'] = 78,
    };
    public static readonly string[] DinoRows =
    {
        "yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy",
        "ybbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbby",
        "ybYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbYby",
        "ybbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbby",
        "ybbbyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyybbby",
        "ybbby......................................................................ybbby",
        "ybYby......................................................................ybYby",
        "ybbby......................................................................ybbby",
        "ybbby.......................................................g..............ybbby",
        "ybbby........c...ccccc.....................................ggggg...........ybbby",
        "ybYby......cccccccccccc..................................cgggggggg.........ybYby",
        "ybbby.....ccccccccccccc................................ccggggggggcc........ybbby",
        "ybbby.....ccccccccccccc...............................ccggggggggccc........ybbby",
        "ybbby......ccccccccccc................................cggggggggcccc........ybbby",
        "ybYby.......G.ccccc................kk.................ggggggggcccc.........ybYby",
        "ybbby......GGG......................kkks.............ggggggggcc............ybbby",
        "ybbby......GGGG......................sss............ggggggggg..............ybbby",
        "ybbby.....GGGGGGG....................sss...........gggggggkgooo............ybbby",
        "ybYby......GGGGGGG...................sss..........ggggggggggooooo..........ybYby",
        "ybbby.......GGGGGGG.................bbbb.........gggggggggggoo.............ybbby",
        "ybbby........GGGGGGGG...............bbbbbbbs....ggggggggggggg..............ybbby",
        "ybbby.........GGGGGGGG..............bbbb.......gggggggggg..................ybbby",
        "ybYby...........GGGGGGG.............bbbb......ggggggggggggggggggggg........ybYby",
        "ybbby............GGGGGGGG...........bbbb.....ggggggggggggggggggg...........ybbby",
        "ybbby.............GGGGGGGG.....gggggbggbgggggggggggggggggggggg.............ybbby",
        "ybbby..............GGGGGGGG..gggggggbggbggggggggggggggggggg................ybbby",
        "ybYby..............GGGGGGGGGggggggggggggggggggggggggggggg..................ybYby",
        "ybbby.............GGGGGGGGgggggggggggggggggggggggggggg.....................ybbby",
        "ybbby...........GGGGGGGGgggggggggYYYYYYYYYYYgggggggg.......................ybbby",
        "ybbby..........GGGGGGGggggggggYYYYYYYYYYYYYYYYYggg.........................ybbby",
        "ybYby................gggggg.gYYYYYYYYYYYYYYYYYYYg..........................ybYby",
        "ybbby..............gggggg....gYYYYYYYYYYYYYYYYYg...........................ybbby",
        "ybbby............ggggg.........ggYoYYYYYYoYYgg.............................ybbby",
        "ybbby...........gggg..............o...g..o.................................ybbby",
        "ybYby..........ggg................o......o.................................ybYby",
        "ybbby.........g...................o......o.................................ybbby",
        "ybbby......................................................................ybbby",
        "ybbby......................................................c...ccccc.......ybbby",
        "ybYby..........c...ccccc.................................cccccccccccc......ybYby",
        "ybbby........cccccccccccc...............................ccccccccccccc......ybbby",
        "ybbby.......ccccccccccccc...............................ccccccccccccc......ybbby",
        "ybbby.......ccccccccccccc................................ccccccccccc.......ybbby",
        "ybYby........ccccccccccc....................................ccccc..........ybYby",
        "ybbby...........ccccc......................................................ybbby",
        "ybbby......................................................................ybbby",
        "ybbby......................................................................ybbby",
        "ybYby......................................................................ybYby",
        "ybbbyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyybbby",
        "ybbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbby",
        "ybYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbby",
        "ybbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbby",
        "yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy",
    };
    public static readonly string[] SendellRows =
    {
        "yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy",
        "yBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBy",
        "yBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYYBy",
        "yBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBy",
        "yBBByyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnYnnnYnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnYnnnnYnnnYnnnnYnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnYnnnnynnnynnnnYnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnynnnnnnnnnnnnnnnnnnnnnnnnYnnnynnnynnyYnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnyYynnnnnnnnnnnnnnnnnnYYnnnnynnynnnynnynnnnYYnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnynnnnnnnnnnnnnnnnnnnnnYnnnynnnynynnnynnnYnnnnnnnnnnnnnnnnnnnnnynnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnynnnynYYYYYnynnnynnnnnnnnnnnnnnnnnnnnnyYynnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyynYYYYLYYYYnyynnnnnnnnnnnnnnnnnnnnnnnynnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnYYYnnnnnYYYLLLLLYYYnnnnnYYYnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnyyynYYYLLLwLLLYYYnyyynnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyyYYLLwwwwwLLYYyynnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnYYLLwwwwwLLYYnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnYYYyyyyYYYLLwbwbwLLYYYyyyyYYYnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnynnnnnnnnnnnnYLLLwwwwwLLLYnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnyYynnnnnnnnnyyYYLLwwpwwLLYYyynnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnynnnnnnnnyyynYYLLwwwwwLLYYnyyynnnnnnnnynnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnYYYnnnnnYLLLLwLLLLYnnnnnYYYnnnnyYynnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyLLYLLLLLLLLnyynnnnnnnnnnnynnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnynLLllLLLLllLLnnynnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnYnlllllLLLLlllllnnYnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnllllllllllllLLLLllllllllllllnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnlllllllllllllllllllLLLLlllllllllllllllllllnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnwwnlllllllllllllllllllllLLLLLLlllllllllllllllllllllnnwwnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnllllllllllllllllllYlllLLLLLLllllllllllllllllllllllnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnlllllllllllllllnnnLlllLLLLLLlllnnnllllllllllllllllnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnlllllllllllllnnnnnnLlllLLLLLLlllLnnnnnllllllllllllllnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnllllLLLLLLllllnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnynnnnnnnnnnnnnnnnnnnnnnnnnllllLLLLLLllllnnnnnnnnnnnnnnnnnnnnnnnnnnynnnnyBBBy",
        "yBBBynnnnyYynnnnnnnnnnnnnnnnnnnnnnnnllllLLLLLLllllnnnnnnnnnnnnnnnnnnnnnnnnnyYynnnyBBBy",
        "yBBBynnnnnynnnnnnnnnnnnnnnnnnnnnnnnlllllLLLLLLlllllnnnnnnnnnnnnnnnnnnnnnnnnnynnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnllllLLyyyyyLllllnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnllllyyLLLLLyylllnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnlllllyLLLLLLLyllllnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnllllyLLLLLLLLlylllnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnlllllLLLyyyLLlllllnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnlllllLLLyyyLLlllllnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnllllllLLLyyyLLllllllnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnllllllLLLLLLLLllllllnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnlllllLLLLLLLLLLlllllnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnllllllLLLLLLLLLLllllllnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnllllllLLLLLLLLLLllllllnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnynnnnnnnnnllllllLLLLLLLLLLllllllnnnnnnnnnnynnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnyYynnnnnnnlllllllLLLLLLLLLLlllllllnnnnnnnnyYynnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnynnnnnnnnlllllllLLLLLLLLLLlllllllnnnnnnnnnynnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnlllllllLLLLLLLLLLlllllllnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnllllllllLLLLLLLLLLllllllllnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnlllllllLLLLLLLLLLLLlllllllnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnlllllllLLLLLLLLLLLLlllllllnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnllllllllLLLLLLLLLLLLllllllllnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnllllllllLLLLLLLLLLLLllllllllnnnnnnnnnnnnnynnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyYynnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnynnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnyYynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnynnnnnnnnnnnnnYYYnYYYnYYnnYYnnYYYnYnnnYnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnYnnnYnnnYnYnYnYnYnnnYnnnYnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnYYYnYYnnYnYnYnYnYYnnYnnnYnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnYnYnnnYnYnYnYnYnnnYnnnYnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnYYYnYYYnYnYnYYnnYYYnYYYnYYYnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBByyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyBBBy",
        "yBYBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBYBy",
        "yBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBy",
        "yBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBy",
        "yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy",
    };

    // A picture's squares as rectangles: each square's colour (at), as few rectangles of one colour as cover the columns [c0, c1) and
    // all the rows, each from its corners (point: a column's and a row's edge -- shared, a point once) facing away from `inside`
    internal static int Stitched(CarMesh m, int c0, int c1, int rows, Func<int, int, char> at, Func<int, int, int> point, IReadOnlyDictionary<char, int> colours, Vector3 inside)
    {
        var done = new bool[c1 - c0, rows];
        var count = 0;
        for (var r = 0; r < rows; r++)
            for (var c = c0; c < c1; c++)
            {
                if (done[c - c0, r]) continue;
                var colour = at(c, r);
                var w = 1;
                while (c + w < c1 && !done[c + w - c0, r] && at(c + w, r) == colour) w++;
                var h = 1;
                while (r + h < rows && Enumerable.Range(c, w).All(i => !done[i - c0, r + h] && at(i, r + h) == colour)) h++;
                for (var j = r; j < r + h; j++) for (var i = c; i < c + w; i++) done[i - c0, j] = true;
                m.Out(new[] { point(c, r), point(c + w, r), point(c + w, r + h), point(c, r + h) }, colours[colour], inside, unlit: true);
                count++;
            }
        return count;
    }

    // A rug's parts: bands of columns, each as wide as keeps its body under the engine's limits (550 points and polygons, kept well under)
    public static List<(int C0, int C1)> RugParts(string[] rows)
    {
        var parts = new List<(int, int)>();
        int width = rows[0].Length, c0 = 0;
        (int Faces, int Points) Count(int a, int b)
        {
            var m = new CarMesh("count", new[] { -1 });
            var pts = new Dictionary<(int, int), int>();
            var faces = Stitched(m, a, b, rows.Length, (c, r) => rows[r][c], (c, r) => pts.TryGetValue((c, r), out var h) ? h : pts[(c, r)] = m.P(0, new Vector3(c, 0, r)),
                                 RugColours, new Vector3(0, -100, 0));
            return (faces, pts.Count);
        }
        while (c0 < width)
        {
            var c1 = c0 + 1;
            while (c1 < width) { var (f, p) = Count(c0, c1 + 1); if (f > 430 || p > 430) break; c1++; }
            parts.Add((c0, c1));
            c0 = c1;
        }
        return parts;
    }
    public static readonly List<(int C0, int C1)> DinoParts = RugParts(DinoRows), SendellParts = RugParts(SendellRows);

    // A rug's part: its columns [c0, c1), its own middle at 0 (the band's middle across, the rug's middle along), lying at 0
    private static Body BuildRugPart(byte[] header, string name, string[] rows, int stitch, (int C0, int C1) part)
    {
        var m = new CarMesh(name, new[] { -1 });
        var mid = (part.C0 + part.C1) / 2f;
        var pts = new Dictionary<(int, int), int>();
        Stitched(m, part.C0, part.C1, rows.Length, (c, r) => rows[r][c],
                 (c, r) => pts.TryGetValue((c, r), out var h) ? h : pts[(c, r)] = m.P(0, new Vector3((c - mid) * stitch, 0, (r - rows.Length / 2f) * stitch)),
                 RugColours, new Vector3(0, -100, 0));
        return m.ToBody(header);
    }
    // where a part's actor stands, from the rug's middle (x along it)
    public static int RugPartX(string[] rows, int stitch, (int C0, int C1) part) => (int)MathF.Round(((part.C0 + part.C1) / 2f - rows[0].Length / 2f) * stitch);
    public static IEnumerable<(int Generic, string Name, Func<byte[], Body> Build)> RugBodies() =>
        DinoParts.Take(DinoMost).Select((p, k) => (DinoFirst + k, $"the Dinofly rug, part {k + 1}", (Func<byte[], Body>)(h => BuildRugPart(h, "the Dinofly rug", DinoRows, DinoStitch, p))))
        .Concat(SendellParts.Take(SendellMost).Select((p, k) => (SendellFirst + k, $"the Sendell rug, part {k + 1}", (Func<byte[], Body>)(h => BuildRugPart(h, "the Sendell rug", SendellRows, SendellStitch, p)))));
}
