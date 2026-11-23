using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The Imperial Hotel's rooms' furniture, remade (2026-11-23, the user, of the Imperial Suite: "The new objects you've added all look flat
// and need more normals adding. The new furniture clashes with the existing furniture you've copied from the hotel's downstairs room.
// They need to look sleek, uniform, upmarket, and modelled to a high degree. Anything in the room needs to be visible and clear what it
// is"). So each piece is in the game's own hotel style -- its rooms 79, 170 and 169: the wardrobes' dark marble between gunmetal posts
// turned in beads, the armchairs' peach leather, the beds' white linen and slate teal, the lamps' tiered white glass -- and modelled round
// (HotelKit: hard faces with corners of their own, bevelled edges, turned and upholstered shapes lit smoothly). Bodies of up to 1,100
// points, for LBA Racing Adventure's own engine.
internal static partial class HotelProps
{
    // (new bodies of entity 17: the Classic Room's bathroom, in the hotel's porcelain and gunmetal)
    public const int HotelBath = 253, HotelToilet = 254, HotelBasin = 197, SideChair = 198, GiltToilet = 199;

    // the finishes: the palette's ramp 3 (greys, the gunmetal and the whites), 13 (violet greys, the wardrobes' marble), 2 (peaches, the
    // armchairs' leather), 11 (slate teals, the walls' and the beds'), 6 (golds), 1 (browns)
    private static readonly Finish GunMetal = new(3 * 16 + 1, 0.95f), DarkStone = new(13 * 16 + 0, 0.6f), StoneTop = new(13 * 16 + 1, 0.7f),
        PeachLeather = new(2 * 16 + 2, 0.9f), WhiteLinen = new(3 * 16 + 6, 0.8f), TealCloth = new(11 * 16 + 2, 0.85f), DeepTeal = new(11 * 16 + 1, 0.75f),
        BlackLacquer = new(3 * 16 + 0, 0.7f), LacquerUnder = new(3 * 16 + 1, 0.6f), KeyIvory = new(3 * 16 + 8, 0.65f), GiltLeaf = new(6 * 16 + 3, 0.9f),
        WhitePorcelain = new(3 * 16 + 7, 0.85f), WhiteGlass = new(3 * 16 + 7, 0.85f), HarpGold = new(6 * 16 + 2, 0.7f), SeatIvory = new(3 * 16 + 8, 0.75f), SeatGrey = new(3 * 16 + 6, 0.75f), LogWood = new(1 * 16 + 2, 0.6f), Soundboard = new(1 * 16 + 7, 0.8f);
    private static readonly Vector3 Up = Vector3.UnitY, Ahead = Vector3.UnitZ, Right = Vector3.UnitX;

    private static CarMesh RoomMesh(string name) => new(name, new[] { -1 }) { Limit = 1100 };

    // ---- a leather sofa: the game's own armchairs' peach leather, three seats -- a plinth, a rolled back and arms, three seat cushions and
    // three back cushions, all upholstered round -- on gunmetal bun feet; its back at -z
    private static Body BuildSofa(byte[] header)
    {
        var m = RoomMesh("a leather sofa");
        const float W = 990, D = 460;
        HotelKit.Cushion(m, new(0, 250, 0), new(W - 40, 140, D - 20), 5, PeachLeather);
        HotelKit.Cushion(m, new(0, 660, -D + 120), new(W - 150, 290, 120), 4, PeachLeather);
        foreach (var s in new[] { -1f, 1f }) HotelKit.Cushion(m, new(s * (W - 140), 450, 0), new(140, 280, D), 4, PeachLeather);
        for (var k = -1; k <= 1; k++)
        {
            HotelKit.Cushion(m, new(k * 475, 455, 60), new(232, 75, D - 100), 4, PeachLeather);
            HotelKit.Cushion(m, new(k * 475, 720, -D + 270), new(226, 200, 80), 3.5f, PeachLeather);
        }
        foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            HotelKit.Turned(m, new(sx * (W - 120), 0, sz * (D - 110)), new[] { (0f, 0f), (46f, 0f), (58f, 40f), (52f, 90f), (40f, 135f), (32f, 165f), (0f, 168f) }, 6, GunMetal);
        return m.ToBody(header);
    }

    // ---- a coffee table: a bevelled top of the dark marble on a gunmetal apron and four turned legs, a shelf of the marble low between
    // them; on it a white porcelain vase, a teal band round it, a spray of flowers
    private static Body BuildCoffeeTable(byte[] header)
    {
        var m = RoomMesh("a coffee table");
        const float W = 620, D = 360, H = 420;
        HotelKit.BevelBox(m, new(0, H - 30, 0), new(2 * W, 60, 2 * D), 22, DarkStone, StoneTop, marbled: (7, 0), seed: 3);
        HotelKit.HardBox(m, new(0, H - 85, 0), new(2 * W - 140, 50, 2 * D - 140), GunMetal);
        foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            HotelKit.Turned(m, new(sx * (W - 90), 0, sz * (D - 90)), new[]
            {
                (0f, 0f), (40f, 0f), (50f, 28f), (36f, 56f), (24f, 90f), (24f, 150f), (42f, 180f), (24f, 210f), (24f, H - 140), (38f, H - 120), (38f, H - 60), (0f, H - 60),
            }, 6, GunMetal);
        HotelKit.HardBox(m, new(0, 120, 0), new(2 * W - 230, 30, 2 * D - 230), DarkStone, StoneTop);
        HotelKit.Lathe(m, new(0, H, 0), Up, Right, Ahead, new[]
        {
            (0f, 0f), (62f, 0f), (88f, 50f), (98f, 120f), (98f, 150f), (82f, 200f), (52f, 245f), (46f, 275f), (68f, 300f), (0f, 296f),
        }, 8, k => k is 3 or 4 ? TealCloth : WhitePorcelain);
        foreach (var (x, y, z, r, f) in new[]
        {
            (0f, 370f, 0f, 58f, WhiteLinen), (-80f, 340f, 45f, 46f, PeachLeather), (70f, 350f, -45f, 46f, TealCloth), (25f, 400f, 70f, 40f, WhiteLinen),
            (-45f, 395f, -65f, 40f, PeachLeather),
        })
            HotelKit.Orb(m, new(x, H + y, z), r, f, 6, 3);
        return m.ToBody(header);
    }

    // ---- the four-poster, its head at -z: a base of the dark marble, the mattress, the teal spread turned down at its head, two pillows;
    // a marble headboard upholstered in teal, a footboard; four gunmetal posts turned as the game's own beds' are, gunmetal rails between
    // their heads, a pleated, scalloped teal pelmet round them -- the canopy open above, so the bed shows from the camera -- and a deep teal
    // curtain at each post, tied back
    private static Body BuildFourPoster(byte[] header)
    {
        var m = RoomMesh("the four-poster");
        const float W = 900, L = 1050, Post = 2300;
        HotelKit.HardBox(m, new(0, 240, 0), new(2 * W - 140, 300, 2 * L - 140), DarkStone, StoneTop);
        HotelKit.Cushion(m, new(0, 500, 0), new(W - 90, 115, L - 90), 7, WhiteLinen, 10, 4);
        HotelKit.Cushion(m, new(0, 515, 210), new(W - 70, 115, L - 270), 8, TealCloth, 12, 5);
        HotelKit.Cushion(m, new(0, 630, -L + 490), new(W - 75, 14, 70), 4, WhiteLinen, 8, 3);
        foreach (var s in new[] { -1f, 1f }) HotelKit.Cushion(m, new(s * (W / 2 - 50), 690, -L + 260), new(W / 2 - 110, 75, 170), 3, WhiteLinen, 12, 4);
        HotelKit.BevelBox(m, new(0, 1080, -L + 10), new(2 * W - 200, 1000, 70), 25, DarkStone);
        HotelKit.Cushion(m, new(0, 1100, -L + 52), new(W - 260, 360, 18), 6, TealCloth, 12, 4);
        HotelKit.BevelBox(m, new(0, 520, L - 20), new(2 * W - 200, 360, 60), 20, DarkStone);
        foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            HotelKit.Post(m, new(sx * (W - 40), 0, sz * (L - 40)), Post, 42, 2, GunMetal);
        const float RailY = Post - 150;
        foreach (var sz in new[] { -1f, 1f }) HotelKit.Rod(m, new(-(W - 40), RailY, sz * (L - 40)), new(W - 40, RailY, sz * (L - 40)), 30, GunMetal);
        foreach (var sx in new[] { -1f, 1f }) HotelKit.Rod(m, new(sx * (W - 40), RailY, -(L - 40)), new(sx * (W - 40), RailY, L - 40), 30, GunMetal);
        // (each side's pelmet faces the camera's way, +x or +z: the foot's and the right's their outsides, the head's and the left's their insides)
        const float PelmetY = RailY + 70;
        HotelKit.Drape(m, new(-(W - 40), PelmetY, L), new(W - 40, PelmetY, L), 330, 7, 35, Ahead, TealCloth, scallop: 90);
        HotelKit.Drape(m, new(-(W - 40), PelmetY, -(L - 80)), new(W - 40, PelmetY, -(L - 80)), 330, 7, 35, Ahead, TealCloth, scallop: 90);
        HotelKit.Drape(m, new(W, PelmetY, -(L - 40)), new(W, PelmetY, L - 40), 330, 8, 35, Right, TealCloth, scallop: 90);
        HotelKit.Drape(m, new(-(W - 80), PelmetY, -(L - 40)), new(-(W - 80), PelmetY, L - 40), 330, 8, 35, Right, TealCloth, scallop: 90);
        foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
        {
            var top = new Vector3(sx * (W + 10), RailY - 20, sz * (L - 40));
            HotelKit.Drape(m, top, top - new Vector3(0, 0, sz * 520), 2050, 3, 30, Right, DeepTeal, new[] { 1f, 0.3f, 0.55f });
        }
        return m.ToBody(header);
    }

    // ---- a fireplace against its wall (its back on z 0): jambs and a frieze of the dark marble, bevelled, a turned gunmetal colonnette on
    // each jamb, the mantel shelf; the fire on its logs behind a gunmetal fender, the hearth stone; on the mantel a gunmetal clock with its
    // white face and a lamp at each end, the game's own lamps' tiered white glass; over it a mirror in a gunmetal frame, streaked as the
    // game's own wardrobe's mirror is
    private static Body BuildFireplace(byte[] header)
    {
        var m = RoomMesh("a marble fireplace");
        const float W = 900, H = 1150;
        foreach (var s in new[] { -1f, 1f }) HotelKit.BevelBox(m, new(s * (W - 150), H / 2, 160), new(300, H, 320), 30, DarkStone, marbled: (0, 5), seed: s > 0 ? 11 : 17);
        HotelKit.BevelBox(m, new(0, H - 140, 150), new(2 * W - 600, 280, 300), 20, DarkStone, marbled: (0, 4), seed: 23);
        HotelKit.BevelBox(m, new(0, H + 45, 190), new(2 * W + 180, 90, 380), 30, DarkStone, StoneTop, marbled: (8, 6), seed: 29);
        foreach (var s in new[] { -1f, 1f }) HotelKit.Post(m, new(s * (W - 150), 0, 345), H - 80, 26, 2, GunMetal, 5);
        // the firebox's sooty back and floor, the hearth stone before it
        HotelKit.Unlit(m, 3 * 16 + 0, Ahead, new(-W + 300, 60, 20), new(W - 300, 60, 20), new(W - 300, H - 280, 20), new(-W + 300, H - 280, 20));
        HotelKit.Unlit(m, 3 * 16 + 1, Up, new(-W + 300, 62, 20), new(W - 300, 62, 20), new(W - 300, 62, 320), new(-W + 300, 62, 320));
        HotelKit.HardBox(m, new(0, 30, 460), new(2 * W + 120, 60, 280), DarkStone, StoneTop);
        HotelKit.Rod(m, new(-330, 120, 150), new(330, 120, 190), 55, LogWood);
        HotelKit.Rod(m, new(-260, 200, 210), new(280, 190, 140), 48, LogWood);
        foreach (var (x, h, w, z) in new[] { (-200f, 360f, 150f, 175f), (0f, 470f, 190f, 165f), (190f, 330f, 140f, 180f) })
        {
            HotelKit.Unlit(m, 15 * 16 + 2, Ahead, new(x - w / 2, 160, z), new(x + w / 2, 160, z), new(x + w * 0.1f, 160 + h, z));
            HotelKit.Unlit(m, 15 * 16 + 4, Ahead, new(x - w / 4, 160, z + 30), new(x + w / 4, 160, z + 30), new(x, 160 + h * 0.6f, z + 30));
        }
        HotelKit.Rod(m, new(-W + 330, 95, 380), new(W - 330, 95, 380), 20, GunMetal);
        foreach (var s in new[] { -1f, 1f }) HotelKit.Orb(m, new(s * (W - 330), 115, 380), 42, GunMetal, 6, 3);
        // the mantel's clock and lamps
        const float Shelf = H + 90;
        HotelKit.HardBox(m, new(0, Shelf + 40, 190), new(300, 80, 170), GunMetal);
        HotelKit.Orb(m, new(0, Shelf + 230, 190), 140, GunMetal, 8, 4);
        HotelKit.Lathe(m, new(0, Shelf + 230, 332), Ahead, Right, Up, new[] { (100f, 0f), (0f, 0f) }, 10, _ => KeyIvory);
        m.Light = 0.5f;
        m.Strut(0, new(0, Shelf + 230, 338), new(0, Shelf + 305, 338), 12, 3 * 16 + 0);
        m.Strut(0, new(0, Shelf + 230, 338), new(50, Shelf + 210, 338), 12, 3 * 16 + 0);
        foreach (var s in new[] { -1f, 1f })
        {
            var at = new Vector3(s * 760, Shelf, 200);
            HotelKit.Turned(m, at, new[] { (0f, 0f), (70f, 0f), (28f, 50f), (18f, 120f), (26f, 150f), (18f, 180f), (18f, 260f), (34f, 280f), (0f, 290f) }, 6, GunMetal);
            HotelKit.Turned(m, at + new Vector3(0, 280, 0), new[] { (0f, 0f), (55f, 8f), (95f, 45f), (78f, 75f), (100f, 105f), (80f, 140f), (92f, 168f), (55f, 200f), (0f, 212f) }, 6, WhiteGlass);
        }
        const float MirrorX = 640, MirrorY0 = Shelf + 140, MirrorY1 = Shelf + 1300;
        foreach (var s in new[] { -1f, 1f }) HotelKit.HardBox(m, new(s * (MirrorX + 40), (MirrorY0 + MirrorY1) / 2, 35), new(80, MirrorY1 - MirrorY0 + 160, 70), GunMetal);
        HotelKit.HardBox(m, new(0, MirrorY1 + 40, 35), new(2 * MirrorX, 80, 70), GunMetal);
        HotelKit.HardBox(m, new(0, MirrorY0 - 40, 35), new(2 * MirrorX, 80, 70), GunMetal);
        Mirror(m, -MirrorX, MirrorX, MirrorY0, MirrorY1, 30);
        HotelKit.Orb(m, new(0, MirrorY1 + 150, 35), 62, GunMetal, 6, 3);
        return m.ToBody(header);
    }

    // a mirror's glass at depth z facing +z: a grid of triangles streaked along its diagonals in the blues, violets and greys of the game's
    // own wardrobe's mirror, drawn as they are
    private static void Mirror(CarMesh m, float x0, float x1, float y0, float y1, float z, int nx = 6, int ny = 8)
    {
        int[] streaks = { 11 * 16 + 12, 11 * 16 + 12, 13 * 16 + 12, 11 * 16 + 12, 11 * 16 + 13, 11 * 16 + 12, 11 * 16 + 11, 3 * 16 + 11, 11 * 16 + 12, 13 * 16 + 13 };
        var g = new int[nx + 1, ny + 1];
        for (var i = 0; i <= nx; i++)
            for (var j = 0; j <= ny; j++) g[i, j] = m.P(0, new(x0 + (x1 - x0) * i / nx, y0 + (y1 - y0) * j / ny, z));
        int Streak(int s) => streaks[((s % streaks.Length) + streaks.Length) % streaks.Length];
        var towards = new Vector3((x0 + x1) / 2, (y0 + y1) / 2, z + 1000);
        for (var i = 0; i < nx; i++)
            for (var j = 0; j < ny; j++)
            {
                m.In(new[] { g[i, j], g[i + 1, j], g[i + 1, j + 1] }, Streak(i - j), towards, unlit: true);
                m.In(new[] { g[i, j], g[i + 1, j + 1], g[i, j + 1] }, Streak(i - j - 1), towards, unlit: true);
            }
    }

    // ---- a grand piano, its keyboard at +z: the black lacquer case round its bent side to the tail, its lid propped open on the bent side
    // over the gilt harp and the strings; the keys (ivory, the black keys in their twos and threes), the music desk with its music, three
    // turned legs on gunmetal castors, the pedals' lyre; the bench before it in the peach leather
    private static Body BuildGrandPiano(byte[] header)
    {
        var m = RoomMesh("a grand piano");
        const float Top = 980, Bottom = 640, LidAngle = 0.74f;
        var outline = new Vector2[]
        {
            new(-700, 330), new(700, 330), new(700, -200), new(660, -470), new(560, -720), new(430, -920), new(330, -1110), new(270, -1330),
            new(180, -1560), new(30, -1720), new(-200, -1790), new(-450, -1760), new(-620, -1660), new(-700, -1500),
        };
        var n = outline.Length;
        var area = 0f;
        for (var k = 0; k < n; k++) area += outline[k].X * outline[(k + 1) % n].Y - outline[(k + 1) % n].X * outline[k].Y;
        Vector3 OutOf(Vector2 a, Vector2 b) { var d = b - a; var o = area > 0 ? new Vector2(d.Y, -d.X) : new Vector2(-d.Y, d.X); return Vector3.Normalize(new Vector3(o.X, 0, o.Y)); }
        Vector3 At(Vector2 p, float y) => new(p.X, y, p.Y);
        var centroid = outline.Aggregate(Vector2.Zero, (s, p) => s + p) / n;
        // the case's sides: the front, the bent side to the tail (lit smoothly round its curve), the straight side -- each its own points
        void Strip(int from, int to)
        {
            m.Light = BlackLacquer.Light;
            var low = new List<int>(); var high = new List<int>();
            for (var k = from; k <= to; k++) { low.Add(m.P(0, At(outline[k % n], Bottom))); high.Add(m.P(0, At(outline[k % n], Top))); }
            for (var k = 0; k + 1 < low.Count; k++)
            {
                var mid = (At(outline[(from + k) % n], (Top + Bottom) / 2) + At(outline[(from + k + 1) % n], (Top + Bottom) / 2)) / 2;
                m.In(new[] { low[k], low[k + 1], high[k + 1], high[k] }, BlackLacquer.Colour, mid + OutOf(outline[(from + k) % n], outline[(from + k + 1) % n]) * 1000);
            }
        }
        Strip(0, 1); Strip(1, n - 1); Strip(n - 1, n);
        // the rim's top, the inside of the rim, the soundboard, the gilt harp over it and its strings
        Vector2 Inset(int k, float by)
        {
            var a = OutOf(outline[(k - 1 + n) % n], outline[k]); var b = OutOf(outline[k], outline[(k + 1) % n]);
            var o = Vector3.Normalize(a + b);
            return outline[k] - new Vector2(o.X, o.Z) * by;
        }
        var inner = Enumerable.Range(0, n).Select(k => Inset(k, 50)).ToArray();
        m.Light = BlackLacquer.Light;
        var rimOut = outline.Select(p => m.P(0, At(p, Top))).ToArray(); var rimIn = inner.Select(p => m.P(0, At(p, Top))).ToArray();
        for (var k = 0; k < n; k++) m.In(new[] { rimOut[k], rimOut[(k + 1) % n], rimIn[(k + 1) % n], rimIn[k] }, BlackLacquer.Colour, At(outline[k], Top + 1000));
        var wallTop = inner.Select(p => m.P(0, At(p, Top))).ToArray(); var wallLow = inner.Select(p => m.P(0, At(p, Top - 110))).ToArray();
        for (var k = 0; k < n; k++) m.In(new[] { wallTop[k], wallTop[(k + 1) % n], wallLow[(k + 1) % n], wallLow[k] }, BlackLacquer.Colour, At(centroid, Top - 50));
        m.Light = Soundboard.Light;
        var board = inner.Select(p => m.P(0, At(p, Top - 110))).ToArray(); var boardMid = m.P(0, At(centroid, Top - 110));
        for (var k = 0; k < n; k++) m.In(new[] { boardMid, board[k], board[(k + 1) % n] }, Soundboard.Colour, At(centroid, Top + 1000));
        m.Light = HarpGold.Light;
        var plate = inner.Select(p => m.P(0, At(centroid + (p - centroid) * 0.82f, Top - 80))).ToArray(); var plateMid = m.P(0, At(centroid, Top - 80));
        for (var k = 0; k < n; k++) m.In(new[] { plateMid, plate[k], plate[(k + 1) % n] }, HarpGold.Colour, At(centroid, Top + 1000));
        float FarZ(float x)
        {
            // (where the plate's edge is at x, along the bent side and the tail)
            var best = 0f;
            for (var k = 1; k < n - 1; k++)
            {
                Vector2 a = centroid + (inner[k] - centroid) * 0.82f, b = centroid + (inner[k + 1] - centroid) * 0.82f;
                if ((a.X - x) * (b.X - x) <= 0 && a.X != b.X) best = Math.Min(best, a.Y + (b.Y - a.Y) * (x - a.X) / (b.X - a.X));
            }
            return best;
        }
        for (var k = 0; k < 12; k++)
        {
            var x = -500 + k * 90f;
            HotelKit.Unlit(m, 3 * 16 + 12, Up, new(x - 5, Top - 70, 220), new(x + 5, Top - 70, 220), new(x + 5, Top - 70, FarZ(x) + 90), new(x - 5, Top - 70, FarZ(x) + 90));
        }
        // the lid, hinged along the straight side and propped up over the bent one: a slab, its top black, its underside catching the light
        float ca = MathF.Cos(LidAngle), sa = MathF.Sin(LidAngle);
        var lidNormal = new Vector3(-sa, ca, 0);
        Vector3 Lid(Vector2 p) => new(-700 + (p.X + 700) * ca, Top + 25 + (p.X + 700) * sa, p.Y);
        var lidMid = Lid(centroid);
        foreach (var (side, f) in new[] { (1f, BlackLacquer), (-1f, LacquerUnder) })
        {
            m.Light = f.Light;
            var ring = outline.Select(p => m.P(0, Lid(p) + lidNormal * 15 * side)).ToArray(); var mid = m.P(0, lidMid + lidNormal * 15 * side);
            for (var k = 0; k < n; k++) m.In(new[] { mid, ring[k], ring[(k + 1) % n] }, f.Colour, lidMid + lidNormal * 1000 * side);
        }
        m.Light = BlackLacquer.Light;
        var edgeTop = outline.Select(p => m.P(0, Lid(p) + lidNormal * 15)).ToArray(); var edgeLow = outline.Select(p => m.P(0, Lid(p) - lidNormal * 15)).ToArray();
        for (var k = 0; k < n; k++)
        {
            var o = OutOf(outline[k], outline[(k + 1) % n]);
            var oLid = Vector3.Normalize(new Vector3(o.X * ca, o.X * sa, o.Z));
            m.In(new[] { edgeTop[k], edgeTop[(k + 1) % n], edgeLow[(k + 1) % n], edgeLow[k] }, BlackLacquer.Colour, (Lid(outline[k]) + Lid(outline[(k + 1) % n])) / 2 + oLid * 1000);
        }
        m.Strut(0, new(600, Top, -330), Lid(new(640, -330)) - lidNormal * 15, 22, BlackLacquer.Colour);
        // the keyboard: the key bed, its cheeks, the white keys and the gaps between them, the black keys
        HotelKit.HardBox(m, new(0, 690, 455), new(1360, 110, 250), BlackLacquer);
        foreach (var s in new[] { -1f, 1f }) HotelKit.HardBox(m, new(s * 655, 770, 455), new(70, 170, 250), BlackLacquer);
        HotelKit.BevelBox(m, new(0, 765, 470), new(1240, 40, 220), 8, KeyIvory);
        const int Whites = 21;
        const float KeyWide = 1240f / Whites, KeysLeft = -620;
        m.Light = 1;
        for (var k = 1; k < Whites; k++) m.Line(m.P(0, new(KeysLeft + k * KeyWide, 786, 365)), m.P(0, new(KeysLeft + k * KeyWide, 786, 578)), 3 * 16 + 4);
        for (var octave = 0; octave < 3; octave++)
            foreach (var w in new[] { 1, 2, 4, 5, 6 })
            {
                var x = KeysLeft + (octave * 7 + w) * KeyWide;
                HotelKit.Unlit(m, 3 * 16 + 1, Up, new(x - 17, 802, 362), new(x + 17, 802, 362), new(x + 17, 802, 495), new(x - 17, 802, 495));
            }
        // the music desk on the case's front, the music on it
        HotelKit.HardBox(m, new(0, Top + 130, 300), new(900, 260, 40), BlackLacquer);
        HotelKit.Unlit(m, 3 * 16 + 13, Ahead, new(-230, Top + 40, 322), new(230, Top + 40, 322), new(230, Top + 235, 322), new(-230, Top + 235, 322));
        m.Light = 1;
        for (var k = 0; k < 4; k++) m.Line(m.P(0, new(-200, Top + 80 + k * 40, 324)), m.P(0, new(200, Top + 80 + k * 40, 324)), 3 * 16 + 4);
        // three turned legs on gunmetal castors, the lyre and its gilt pedals
        foreach (var (x, z) in new[] { (-600f, 230f), (600f, 230f), (-330f, -1530f) })
            HotelKit.Lathe(m, new(x, 0, z), Up, Right, Ahead, new[]
            {
                (0f, 0f), (32f, 0f), (40f, 32f), (30f, 62f), (52f, 78f), (82f, 125f), (70f, 210f), (55f, 340f), (62f, 480f), (86f, 560f), (100f, Bottom), (0f, Bottom),
            }, 6, k => k < 3 ? GunMetal : BlackLacquer);
        HotelKit.HardBox(m, new(0, (Bottom + 110) / 2, 170), new(160, Bottom - 110, 40), BlackLacquer);
        HotelKit.HardBox(m, new(0, 80, 190), new(280, 60, 140), BlackLacquer);
        foreach (var x in new[] { -80f, 0f, 80f }) HotelKit.Face(m, GiltLeaf, Up, new(x - 19, 100, 235), new(x + 19, 100, 235), new(x + 19, 100, 345), new(x - 19, 100, 345));
        // the bench
        HotelKit.Cushion(m, new(0, 500, 920), new(400, 50, 170), 5, PeachLeather);
        HotelKit.HardBox(m, new(0, 420, 920), new(820, 70, 340), BlackLacquer);
        foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            HotelKit.Turned(m, new(sx * 360, 0, 920 + sz * 130), new[] { (0f, 0f), (34f, 0f), (26f, 40f), (20f, 200f), (28f, 330f), (34f, 385f), (0f, 385f) }, 6, BlackLacquer);
        return m.ToBody(header);
    }

    // ---- a rug (flat, walked over): 24 x 17 knots of the room's own colours, drawn as they are -- the darkest slate edge, a cream guard
    // stripe, the slate teal border with its cream motif, a cream line, the deep teal field and its weave, a medallion of white, light teal,
    // cream and the marble's violet grey, cream spandrels in its corners -- its edges lit, cream fringes at both ends
    private static Body BuildRug(byte[] header)
    {
        var m = RoomMesh("a rug");
        m.Light = 0.75f;
        const int NX = 24, NZ = 17;
        const float W = 1200, L = 850, Y = 14;
        var grid = new int[NX + 1, NZ + 1];
        for (var i = 0; i <= NX; i++)
            for (var j = 0; j <= NZ; j++) grid[i, j] = m.P(0, new(-W + 2 * W * i / NX, Y, -L + 2 * L * j / NZ));
        for (var i = 0; i < NX; i++)
            for (var j = 0; j < NZ; j++)
                m.Up(new[] { grid[i, j], grid[i + 1, j], grid[i + 1, j + 1], grid[i, j + 1] }, RugKnot(i, j, NX, NZ), unlit: true);
        var outwards = new[] { -Ahead, Right, Ahead, -Right };
        var corners = new[] { new Vector3(-W, 0, -L), new Vector3(W, 0, -L), new Vector3(W, 0, L), new Vector3(-W, 0, L) };
        for (var e = 0; e < 4; e++)
        {
            Vector3 a = corners[e], b = corners[(e + 1) % 4];
            HotelKit.Face(m, new(11 * 16 + 1, 0.6f), outwards[e], a, b, b + new Vector3(0, Y, 0), a + new Vector3(0, Y, 0));
        }
        for (var k = 0; k < 14; k++)
            foreach (var sx in new[] { -1f, 1f })
            {
                var z = -L + 2 * L * (k + 0.5f) / 14;
                HotelKit.Unlit(m, 3 * 16 + 12, Up, new(sx * W, 5, z - 13), new(sx * (W + 105), 5, z - 9), new(sx * (W + 105), 5, z + 9), new(sx * W, 5, z + 13));
            }
        return m.ToBody(header);
    }

    private static int RugKnot(int i, int j, int nx, int nz)
    {
        var ring = Math.Min(Math.Min(i, nx - 1 - i), Math.Min(j, nz - 1 - j));
        if (ring == 0) return 11 * 16 + 1;
        if (ring == 1) return (i + j) % 2 == 0 ? 3 * 16 + 12 : 3 * 16 + 9;
        if (ring == 2) return (i + j) % 4 == 0 ? 3 * 16 + 12 : 11 * 16 + 8;
        if (ring == 3) return 3 * 16 + 11;
        double cx = (nx - 1) / 2.0, cz = (nz - 1) / 2.0;
        var d = Math.Abs(i - cx) / (cx - 3.5) + Math.Abs(j - cz) / (cz - 3.5);
        if (d <= 0.22) return 3 * 16 + 13;
        if (d <= 0.42) return 11 * 16 + 10;
        if (d <= 0.62) return 3 * 16 + 11;
        if (d <= 0.74) return 13 * 16 + 6;
        double ci = Math.Min(i - 4, nx - 5 - i), cj = Math.Min(j - 4, nz - 5 - j);
        if (ci + cj <= 1.6) return ci + cj <= 0.6 ? 3 * 16 + 12 : 11 * 16 + 9;
        return (i + 2 * j) % 5 == 0 ? 11 * 16 + 6 : 11 * 16 + 5;
    }

    // ---- an oil painting on its wall (back on z 0): Otringal's bay at sunset in the palette's own colours (PaintingColour),
    // in a gunmetal frame -- a gilt slip round the canvas, then the frame's moulding stepped out and back, each step lit at its own angle --
    // gunmetal beads at its corners and over its top
    private static Body BuildPainting(byte[] header)
    {
        var m = RoomMesh("a painting");
        const int MaxRows = 48, Samples = 200, CanvasPoints = 820;
        const float W = 800, H = 600, Y = 700, F = 150, Z = 52;
        var profile = new (float Out, float Depth, Finish Finish)[]
        {
            (0, Z, GiltLeaf), (22, Z + 10, GiltLeaf), (34, Z + 18, GunMetal), (52, Z + 28, GunMetal), (66, Z + 22, GunMetal), (88, Z + 38, GunMetal),
            (110, Z + 54, GunMetal), (126, Z + 60, GunMetal), (140, Z + 54, GunMetal), (156, Z + 36, GunMetal), (F, 6, GunMetal),
        };
        var outward = new[] { new Vector3(0, -1, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(-1, 0, 0) };
        for (var k = 0; k + 1 < profile.Length; k++)
        {
            var f = profile[k].Finish;
            var rise = profile[k + 1].Depth - profile[k].Depth;
            for (var side = 0; side < 4; side++)
            {
                // (each step's four sides their own corners: mitred, each side lit as itself)
                Vector3 C(int corner, int step) => new(
                    (corner is 1 or 2 ? 1 : -1) * (W + profile[step].Out), Y + (corner is 2 or 3 ? 1 : -1) * (H + profile[step].Out), profile[step].Depth);
                int a = side, b = (side + 1) % 4;
                var towards = outward[side] * (rise >= 0 ? -0.15f : 0.4f) + Ahead;
                HotelKit.Face(m, f, towards, C(a, k), C(b, k), C(b, k + 1), C(a, k + 1));
            }
        }
        foreach (var sx in new[] { -1f, 1f })
            foreach (var sy in new[] { -1f, 1f }) HotelKit.Orb(m, new(sx * (W + 116), Y + sy * (H + 116), Z + 66), 30, GunMetal, 6, 3);
        HotelKit.Orb(m, new(0, Y + H + F + 20, Z + 30), 55, GunMetal, 8, 4);
        // the canvas: its rows, each cut where the picture's colour changes along it (a run of a sample or two folded into its
        // neighbour), each run one unlit quad -- every shape's edge as fine as the rows are close
        List<(int From, int Colour)> Runs(int r, int rows)
        {
            var v = (r + 0.5) / rows;
            var runs = new List<(int From, int Colour)>();
            for (var k = 0; k < Samples; k++)
            {
                var c = PaintingColour((k + 0.5) / Samples, v);
                if (runs.Count == 0 || runs[^1].Colour != c) runs.Add((k, c));
            }
            for (var k = runs.Count - 1; k >= 1; k--)
                if ((k + 1 < runs.Count ? runs[k + 1].From : Samples) - runs[k].From < 2) runs.RemoveAt(k);
            for (var k = runs.Count - 1; k >= 1; k--)
                if (runs[k].Colour == runs[k - 1].Colour) runs.RemoveAt(k);
            return runs;
        }
        // (as many rows as the body's points allow, after the frame's)
        var rows = MaxRows;
        while (Enumerable.Range(0, rows).Sum(r => (Runs(r, rows).Count + 1) * 2) > CanvasPoints) rows -= 2;
        for (var r = 0; r < rows; r++)
        {
            var runs = Runs(r, rows);
            float y0 = Y + H - 2 * H * r / rows, y1 = Y + H - 2 * H * (r + 1) / rows;
            float Xs(int k) => -W + 2 * W * (k < runs.Count ? runs[k].From : Samples) / Samples;
            var tops = Enumerable.Range(0, runs.Count + 1).Select(k => m.P(0, new(Xs(k), y0, Z))).ToArray();
            var lows = Enumerable.Range(0, runs.Count + 1).Select(k => m.P(0, new(Xs(k), y1, Z))).ToArray();
            for (var k = 0; k < runs.Count; k++)
                m.In(new[] { lows[k], lows[k + 1], tops[k + 1], tops[k] }, runs[k].Colour, new(0, Y, 400), unlit: true);
        }
        return m.ToBody(header);
    }

    // The painting's colour at (u across, v down the canvas, 0..1): Otringal's bay at sunset -- the sky blue overhead warming to peach at
    // the horizon, three clouds lit from below, the sun low over the sea with its glow and its path glinting on the water, the mountains
    // blue-violet on the left with light on their ridges, a white palace with red domes and a gilt spire on the green shore, a sail on the
    // bay, dark cypresses and a pine in the foreground lit from the sun's side. (Ramp * 16 + shade, the palette's ramps: 12 blues, 5
    // peaches, 14 rose greys, 15 the sun's yellows, 13 violet greys, 10 sea blues, 6 golds, 7 and 8 greens, 3 whites, 4 reds, 1 browns,
    // 2 creams.)
    private static int PaintingColour(double u, double v)
    {
        const double Horizon = 0.56;
        double Ellipse(double cu, double cv, double ru, double rv) => Math.Pow((u - cu) / ru, 2) + Math.Pow((v - cv) / rv, 2);
        foreach (var (cu, cv, ru, rv) in new[] { (0.045, 0.74, 0.042, 0.24), (0.118, 0.83, 0.032, 0.15), (0.36, 0.84, 0.055, 0.085) })
            if (Ellipse(cu, cv, ru, rv) <= 1) return u > cu + ru * 0.25 ? 8 * 16 + 6 : 8 * 16 + 3;
        if (u > 0.354 && u < 0.366 && v > 0.84) return 1 * 16 + 4;
        // the boat: its hull, its mast, its sail
        if (v > 0.645 && v < 0.668 && u > 0.53 + (v - 0.645) * 0.8 && u < 0.605 - (v - 0.645) * 0.8) return 1 * 16 + 4;
        if (u > 0.553 && u < 0.558 && v > 0.555 && v <= 0.645) return 1 * 16 + 2;
        if (v > 0.565 && v <= 0.64 && u >= 0.558 && u < 0.558 + (v - 0.565) * 0.6) return 2 * 16 + 14;
        // the palace on the shore: its towers and their domes, the great dome and its spire, the walls, their windows and the gate
        if (u > 0.1 && u < 0.31 && v > 0.37 && v < 0.63)
        {
            foreach (var tu in new[] { 0.1325, 0.2775 })
            {
                if (Ellipse(tu, 0.445, 0.02, 0.03) <= 1 && v <= 0.445) return u < tu ? 4 * 16 + 9 : 4 * 16 + 7;
                if (Math.Abs(u - tu) < 0.0175 && v > 0.445)
                    return Math.Abs(u - tu) < 0.004 && v > 0.47 && v < 0.5 ? 3 * 16 + 7 : u < tu ? 3 * 16 + 13 : 3 * 16 + 11;
            }
            if (u > 0.203 && u < 0.208 && v > 0.4 && v < 0.44) return 6 * 16 + 12;
            if (Ellipse(0.205, 0.505, 0.052, 0.07) <= 1 && v <= 0.505) return u < 0.19 ? 4 * 16 + 9 : 4 * 16 + 7;
            if (v > 0.505 && u > 0.15 && u < 0.26)
            {
                if (u > 0.196 && u < 0.214 && v > 0.585) return 3 * 16 + 6;
                foreach (var (wv0, wv1) in new[] { (0.525, 0.548), (0.565, 0.585) })
                    if (v > wv0 && v < wv1 && ((u - 0.152) % 0.022) > 0.01) return 3 * 16 + 8;
                return 3 * 16 + 13;
            }
        }
        // the shore, a strip of sand along it
        var shoreline = 0.38 + (v - Horizon) * 0.9;
        if (v > 0.585 && u < shoreline) return u > shoreline - 0.014 ? 2 * 16 + 9 : v > 0.82 ? 7 * 16 + 5 : 7 * 16 + 8;
        if (v >= Horizon)
        {
            var path = Math.Abs(u - 0.72) < 0.018 + (v - Horizon) * 0.14;
            if (path && Math.Sin(v * 150) > 0.55) return 6 * 16 + 12;
            return v < 0.6 ? 10 * 16 + 11 : v < 0.68 ? 10 * 16 + 9 : v < 0.8 ? 10 * 16 + 7 : 10 * 16 + 5;
        }
        // the sun, its glow
        var sun = Math.Pow((u - 0.72) * 1600, 2) + Math.Pow((v - 0.485) * 1200, 2);
        if (sun < 62 * 62) return 15 * 16 + 5;
        if (sun < 84 * 84) return 15 * 16 + 4;
        // the mountains, light along their ridges
        var ridge = Horizon - 0.2 * Math.Max(0, Math.Sin(Math.PI * Math.Min(1, u / 0.55))) - 0.045 * Math.Sin(u * 23) - 0.015 * Math.Sin(u * 61);
        if (u < 0.62 && v > ridge) return v < ridge + 0.03 ? 13 * 16 + 11 : v > ridge + 0.09 ? 13 * 16 + 6 : 13 * 16 + 8;
        // the clouds, lit from below
        foreach (var (cu, cv, ru, rv) in new[] { (0.25, 0.19, 0.17, 0.03), (0.7, 0.31, 0.2, 0.024), (0.5, 0.1, 0.11, 0.02) })
            if (Ellipse(cu, cv, ru, rv) <= 1) return v > cv ? 14 * 16 + 13 : 14 * 16 + 11;
        if (v > 0.47) return 5 * 16 + 12;
        if (v > 0.41) return 5 * 16 + 13;
        if (v > 0.35) return 5 * 16 + 14;
        return v < 0.12 ? 12 * 16 + 9 : v < 0.2 ? 12 * 16 + 10 : v < 0.28 ? 12 * 16 + 11 : 12 * 16 + 13;
    }

    // ---- the Classic Room's bath: a roll-top bath in white porcelain, as the hotel's own (room 169's) is -- an oval turned and hollowed,
    // its rim rolled over -- on four turned gunmetal feet, a gunmetal mixer at its head (-z), its spout and cross handles. Low, so it
    // hides nothing behind it (a shower's glass cage, 2,100 high, stood over the little bathroom and the room behind it)
    private static Body BuildBath(byte[] header)
    {
        var m = RoomMesh("a bath");
        HotelKit.Lathe(m, Vector3.Zero, Up, new Vector3(0.5f, 0, 0), Ahead, new[]
        {
            (0f, 160f), (470f, 168f), (560f, 230f), (610f, 380f), (636f, 540f), (650f, 610f), (640f, 640f), (608f, 646f), (590f, 620f),
            (560f, 470f), (500f, 330f), (380f, 270f), (0f, 262f),
        }, 16, _ => WhitePorcelain);
        foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            HotelKit.Turned(m, new(sx * 200, 0, sz * 430), new[] { (0f, 0f), (52f, 0f), (58f, 30f), (40f, 62f), (30f, 110f), (44f, 150f), (60f, 205f), (0f, 205f) }, 6, GunMetal);
        HotelKit.Turned(m, new(0, 620, -575), new[] { (0f, 0f), (34f, 0f), (28f, 40f), (24f, 110f), (34f, 130f), (0f, 136f) }, 6, GunMetal);
        HotelKit.Rod(m, new(0, 735, -575), new(0, 760, -455), 18, GunMetal);
        m.Light = GunMetal.Light;
        foreach (var sx in new[] { -1f, 1f }) m.Strut(0, new(sx * 30, 700, -575), new(sx * 110, 700, -575), 14, GunMetal.Colour);
        return m.ToBody(header);
    }

    // ---- a toilet, its back on z 0 (its wall), its front +z: a white porcelain cistern and lid, bevelled, its lever; the pedestal and bowl
    // turned round an oval; the seat, its lid raised against the cistern. Gilt: the suites' bathrooms' (the game's room 169: its basin's
    // and its bath's gold taps, rail and claw feet -- 2026-11-23, the user: "let's model one that matches the sink and bath style") -- gold
    // fittings, a gold band round the lid, an ivory seat; else the Classic Room's, its fittings gunmetal and its seat black
    private static Body BuildToilet(byte[] header, bool gilt)
    {
        var m = RoomMesh(gilt ? "a toilet, gilt" : "a toilet");
        var fittings = gilt ? GiltLeaf : GunMetal;
        var seat = gilt ? SeatIvory : BlackLacquer;
        HotelKit.BevelBox(m, new(0, 830, 120), new(460, 320, 200), 30, WhitePorcelain);
        HotelKit.BevelBox(m, new(0, 1005, 120), new(500, 30, 230), 10, WhitePorcelain);
        if (gilt) HotelKit.HardBox(m, new(0, 978, 120), new(470, 24, 210), GiltLeaf);
        HotelKit.Orb(m, new(-170, 930, 222), 24, fittings, 6, 3);
        m.Light = fittings.Light;
        m.Strut(0, new(-170, 930, 230), new(-270, 925, 250), 16, fittings.Colour);
        HotelKit.Turned(m, new(0, 520, 150), new[] { (0f, 0f), (32f, 0f), (30f, 160f), (0f, 160f) }, 6, fittings);
        var oval = new Vector3(0, 0, 1.25f);
        HotelKit.Lathe(m, new(0, 0, 420), Up, Right, oval, new[]
        {
            (0f, 0f), (150f, 0f), (150f, 30f), (120f, 62f), (108f, 200f), (140f, 280f), (198f, 340f), (228f, 380f), (234f, 400f), (192f, 404f), (120f, 352f), (0f, 330f),
        }, 10, _ => WhitePorcelain);
        HotelKit.Lathe(m, new(0, 0, 420), Up, Right, oval, new[]
        {
            (196f, 408f), (240f, 410f), (252f, 422f), (244f, 438f), (220f, 444f), (190f, 440f), (178f, 426f), (196f, 408f),
        }, 10, _ => seat);
        HotelKit.Cushion(m, new(0, 690, 245), new(215, 250, 20), 3, seat, 10, 4);
        if (gilt) foreach (var sx in new[] { -1f, 1f }) HotelKit.Orb(m, new(sx * 120, 452, 230), 22, GiltLeaf, 6, 3);
        return m.ToBody(header);
    }

    // ---- a side chair in the game's own hotel style (its rooms' chairs: gunmetal posts turned in beads, a round grey seat): four turned
    // legs, the seat, two beaded back posts with a rail and a row of beads between them; its back at -z
    private static Body BuildSideChair(byte[] header)
    {
        var m = RoomMesh("a side chair");
        foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            HotelKit.Turned(m, new(sx * 150, 0, sz * 150), new[] { (0f, 0f), (42f, 0f), (30f, 30f), (20f, 90f), (32f, 150f), (20f, 210f), (26f, 330f), (0f, 330f) }, 6, GunMetal);
        HotelKit.Lathe(m, new(0, 330, 0), Up, Right, Ahead, new[] { (0f, 0f), (210f, 0f), (222f, 22f), (210f, 46f), (0f, 52f) }, 12, k => k == 0 ? GunMetal : SeatGrey);
        foreach (var sx in new[] { -1f, 1f }) HotelKit.Post(m, new(sx * 165, 330, -175), 640, 22, 3, GunMetal);
        HotelKit.Rod(m, new(-165, 330 + 560, -175), new(165, 330 + 560, -175), 16, GunMetal);
        foreach (var x in new[] { -80f, 0f, 80f }) HotelKit.Orb(m, new(x, 330 + 470, -175), 30, GunMetal, 6, 3);
        HotelKit.Rod(m, new(-165, 330 + 390, -175), new(165, 330 + 390, -175), 14, GunMetal);
        return m.ToBody(header);
    }

    // ---- a pedestal basin, its back on z 0 (its wall), its front +z: the pedestal turned in white porcelain, the bowl round an oval and
    // hollowed, a splashback, two gunmetal taps with their cross handles, a spout
    private static Body BuildBasin(byte[] header)
    {
        var m = RoomMesh("a basin");
        HotelKit.Turned(m, new(0, 0, 250), new[] { (0f, 0f), (150f, 0f), (150f, 25f), (115f, 52f), (92f, 120f), (84f, 450f), (100f, 600f), (140f, 700f), (0f, 700f) }, 10, WhitePorcelain);
        HotelKit.Lathe(m, new(0, 0, 270), Up, new Vector3(1.25f, 0, 0), Ahead, new[]
        {
            (0f, 690f), (180f, 700f), (262f, 760f), (298f, 820f), (304f, 852f), (276f, 860f), (240f, 842f), (170f, 792f), (0f, 776f),
        }, 10, _ => WhitePorcelain);
        HotelKit.BevelBox(m, new(0, 900, 22), new(720, 170, 44), 12, WhitePorcelain);
        foreach (var s in new[] { -1f, 1f })
        {
            HotelKit.Turned(m, new(s * 140, 852, 70), new[] { (0f, 0f), (32f, 0f), (26f, 30f), (18f, 70f), (0f, 72f) }, 6, GunMetal);
            m.Light = GunMetal.Light;
            m.Strut(0, new(s * 140 - 50, 935, 70), new(s * 140 + 50, 935, 70), 14, GunMetal.Colour);
            m.Strut(0, new(s * 140, 935, 20), new(s * 140, 935, 120), 14, GunMetal.Colour);
        }
        HotelKit.Rod(m, new(0, 880, 40), new(0, 900, 150), 22, GunMetal);
        HotelKit.Turned(m, new(0, 840, 150), new[] { (0f, 0f), (20f, 0f), (22f, 60f), (0f, 64f) }, 6, GunMetal);
        return m.ToBody(header);
    }
}
