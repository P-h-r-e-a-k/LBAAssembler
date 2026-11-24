using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The cycle shop (CitadelCycleShop, scene 398; the user, 2026-11-20: "Let's change 'Citadel Island, Baggage claim building, 1st scene
// (room #7)' into a cycle sale and repair shop ... He should also offer bikes for sale that Twinsen will never be able to afford 1000, 2000,
// 5000"; and "Let's add the hoverboard as a used item left behind in a corner of the cycle shop").
// Redesigned 2026-11-21 evening, after looking at how high-end racing bike shops are fitted out (the user: "Let's redesign the cycle shop
// based on your new research ... Let's call the place 'Citadel Cycles' Let's not put the text in all capitals as this is ugly and hard to
// read"): few bikes, each shown like a piece in a gallery -- the champion's on a lit plinth under a lamp, the racing bike hung on the wall,
// the town bike on a floor stand, each with its price tag; the shop's racing past on the walls -- a penny-farthing, signed jerseys; the
// workshop on show, its tool wall and bench by the repair stand; a fitting bike on its trainer and a stand of saddles; a little coffee bar
// for the racers; and the shop's name over it all, white on black. Outside, over the baggage claim's old sign on its door, a banner.
//   - the bikes: Twinsen's and Zoe's bike's shape (BuildBike), upright now (AddBike), the racing ones with drop bars;
//   - the Hoverboard left behind, standing on its tail and leaning back against the wall behind it (its back, -z, to the wall), its deck
//     to the room: the item's own board (CitadelHoverboard.Faces), the same as Twinsen rides.
internal static partial class CitadelProps
{
    public const int ShopBikeTown = 211, ShopBikeRacing = 212, ShopBikeChampion = 213, LeaningBoard = 214;
    // (since the shop became a room of its own, CitadelCycleShop -- the user, 2026-11-21: "Let's just design an awesome cycle shop": its back
    // doorway through to the baggage claim, the repair stand a bike is clamped on, the wall of wheels for sale)
    public const int ShopDoorway = 215, RepairStand = 216, WheelWall = 217, ShopSign = 218;
    // (the redesign, 2026-11-21 evening)
    public const int PennyFarthing = 219, Tandem = 220, FitBike = 221, SaddleStand = 222, JerseyWall = 223, ToolWall = 224, CafeBar = 225,
                     ShopBanner = 226, ShowPlinth = 227, RepairBike = 228;
    private const int ShopGold = 106, ShopTeal = 150, ShopSteel = 56, ShopRubber = 48, ShopWood = 20, ShopWoodLight = 25, ShopDark = 49;
    private const int ShopRed = 66, ShopCream = 36, ShopWhite = 62, ShopYellow = 6 * 16 + 13, ShopBlue = 197;
    // (unlit, as drawn: the sign's and the banner's -- white on near black, a red rule; the price tags white)
    private const int InkWhite = 63, InkBlack = 49, InkRed = 70, TagWhite = 62;
    // (how far the board leans back from upright, and how far its foot stands out from the wall: its top against it)
    private const float BoardLean = 0.26f;
    // a bike's wheel's radius, its hubs either side of the bottom bracket, its seat's height (AddBike)
    public const float BikeWheel = 270, BikeBase = 520, BikeSeat = 760;
    // the racing bike's height on the wall (its wheels' foot), the plinth's top
    public const float HungLow = 560, PlinthTop = 220;

    private static IEnumerable<(int, string, Func<byte[], Body>)> CycleShopBodies() => new (int, string, Func<byte[], Body>)[]
    {
        (ShopBikeTown, "the cycle shop's town bike, on its stand", BuildShopTownBike),
        (ShopBikeRacing, "the cycle shop's racing bike, hung on the wall", BuildShopHungBike),
        (ShopBikeChampion, "the cycle shop's champion's bike", BuildShopChampionBike),
        (LeaningBoard, "the Hoverboard, left behind in the cycle shop", BuildLeaningBoard),
        (ShopDoorway, "the cycle shop's back door, shut", BuildShopDoorway),
        (RepairStand, "the cycle shop's repair stand", BuildRepairStand),
        (WheelWall, "the cycle shop's wall of wheels", BuildWheelWall),
        (ShopSign, "the cycle shop's sign: Citadel Cycles", BuildShopSign),
        (PennyFarthing, "the cycle shop's penny-farthing", BuildPennyFarthing),
        (Tandem, "the cycle shop's tandem", BuildTandem),
        (FitBike, "the cycle shop's fitting bike, on its trainer", BuildFitBike),
        (SaddleStand, "the cycle shop's stand of saddles", BuildSaddleStand),
        (JerseyWall, "the cycle shop's signed jerseys", BuildJerseyWall),
        (ToolWall, "the cycle shop's tool wall and bench", BuildToolWall),
        (CafeBar, "the cycle shop's coffee bar", BuildCafeBar),
        (ShopBanner, "Citadel Cycles' banner, over the baggage claim's sign", BuildShopBanner),
        (ShowPlinth, "the cycle shop's plinth", BuildShowPlinth),
        (RepairBike, "a racer's bike, in for repair", BuildRepairBike),
    };

    // ---- the lettering: a pixel font of the letters the shop's name needs, capitals and small letters (rows 0-6 the letters', 7-8 a
    // descender's), each run of a row's pixels one quad
    private static readonly Dictionary<char, string[]> ShopGlyphs = new()
    {
        ['C'] = new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###.", ".....", "....." },
        ['i'] = new[] { "#", ".", "#", "#", "#", "#", "#", ".", "." },
        ['t'] = new[] { ".#.", ".#.", "###", ".#.", ".#.", ".#.", "..#", "...", "..." },
        ['a'] = new[] { "....", "....", ".##.", "...#", ".###", "#..#", ".###", "....", "...." },
        ['d'] = new[] { "...#", "...#", ".###", "#..#", "#..#", "#..#", ".###", "....", "...." },
        ['e'] = new[] { "....", "....", ".##.", "#..#", "####", "#...", ".##.", "....", "...." },
        ['l'] = new[] { "#.", "#.", "#.", "#.", "#.", "#.", ".#", "..", ".." },
        ['y'] = new[] { "....", "....", "#..#", "#..#", "#..#", "#..#", ".###", "...#", ".##." },
        ['c'] = new[] { "....", "....", ".###", "#...", "#...", "#...", ".###", "....", "...." },
        ['s'] = new[] { "....", "....", ".###", "#...", ".##.", "...#", "###.", "....", "...." },
        [' '] = new[] { "..", "..", "..", "..", "..", "..", "..", "..", ".." },
    };
    public const int GlyphRows = 9;
    public const string ShopName = "Citadel Cycles";

    // the runs of a text's pixels: (row, first column, last column), and how many columns it is wide
    public static (List<(int Row, int C0, int C1)> Runs, int Columns) TextRuns(string text)
    {
        var runs = new List<(int, int, int)>();
        var x = 0;
        foreach (var ch in text)
        {
            var g = ShopGlyphs[ch];
            for (var r = 0; r < GlyphRows; r++)
                for (var c = 0; c < g[r].Length; c++)
                {
                    if (g[r][c] != '#' || (c > 0 && g[r][c - 1] == '#')) continue;
                    var end = c;
                    while (end + 1 < g[r].Length && g[r][end + 1] == '#') end++;
                    runs.Add((r, x + c, x + end));
                }
            x += g[0].Length + 1;
        }
        return (runs, x - 1);
    }

    // a text's quads: `place` takes (column, row) -- 0,0 the text's top left -- to the body's frame; facing `towards`, unlit
    private static void TextQuads(CarMesh m, string text, Func<float, float, Vector3> place, int colour, Vector3 towards)
    {
        foreach (var (r, c0, c1) in TextRuns(text).Runs)
            m.In(new[] { m.P(0, place(c0, r)), m.P(0, place(c1 + 1, r)), m.P(0, place(c1 + 1, r + 1)), m.P(0, place(c0, r + 1)) }, colour, towards, unlit: true);
    }

    // a flat quad in a plane, unlit, facing `towards`
    private static void FlatQuad(CarMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int colour, Vector3 towards, bool unlit = true)
        => m.In(new[] { m.P(0, a), m.P(0, b), m.P(0, c), m.P(0, d) }, colour, towards, unlit);

    // The shop's sign: Citadel Cycles, white on a near-black board with a red rule round it, on the north wall's face (z 0), facing +z; its
    // foot at 0 (the actor's height puts it on the wall); its letters a pixel SignPixel square
    public const float SignPixel = 75, SignMargin = 1.4f;
    public static float SignWidth => (TextRuns(ShopName).Columns + 2 * SignMargin) * SignPixel;
    public static float SignHeight => (GlyphRows + 2 * SignMargin) * SignPixel;
    private static Body BuildShopSign(byte[] header)
    {
        var m = new CarMesh("the cycle shop's sign: Citadel Cycles", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        float w = SignWidth, h = SignHeight, rule = SignPixel * 0.45f;
        var front = new Vector3(0, h / 2, 1000);
        // the board: a box for its depth, its face unlit black, the red rule round its edge, the letters
        m.Box(0, new Vector3(0, h / 2, 20), new Vector3(w, h, 40), ShopDark);
        FlatQuad(m, new(-w / 2, 0, 42), new(w / 2, 0, 42), new(w / 2, h, 42), new(-w / 2, h, 42), InkBlack, front);
        foreach (var (x0, y0, x1, y1) in new[] { (-w / 2, h - rule, w / 2, h), (-w / 2, 0, w / 2, rule), (-w / 2, 0, -w / 2 + rule, h), (w / 2 - rule, 0, w / 2, h) })
            FlatQuad(m, new(x0, y0, 44), new(x1, y0, 44), new(x1, y1, 44), new(x0, y1, 44), InkRed, front);
        float left = -w / 2 + SignMargin * SignPixel, top = h - SignMargin * SignPixel;
        TextQuads(m, ShopName, (c, r) => new Vector3(left + c * SignPixel, top - r * SignPixel, 46), InkWhite, front);
        return m.ToBody(header);
    }

    // The banner outside: Citadel Cycles in red on a white cloth, two lines, put up on the door itself over the baggage claim's old sign
    // (CITADEL.OBL body 33's: 600 wide, 500 high) -- the shop has only just moved in. Facing +x (the building's front, its wall at x 0), its
    // middle at y BannerMiddle and z 0, just out from the old sign; its text read along -z (left to right as one faces the wall). (It was
    // slung crooked across the doorway, wider than the door, tied back to the wall past it -- as if the way in were barred: the user,
    // 2026-11-22, "When I said a "Citadel Cycles" banner across the door... I meant on the door its self, not one that would prevent people
    // from entering")
    public const float BannerPixel = 19, BannerOut = 60, BannerMiddle = 1150;
    private static readonly string[] BannerLines = { "Citadel", "Cycles" };
    private static Body BuildShopBanner(byte[] header)
    {
        var m = new CarMesh("Citadel Cycles' banner", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        var cols = BannerLines.Max(l => TextRuns(l).Columns);
        float w = (cols + 2f) * BannerPixel, h = (BannerLines.Length * GlyphRows + 2f) * BannerPixel;
        var front = new Vector3(2000, BannerMiddle, 0);
        // (u along the text from its left, v down from its top, both from the cloth's middle)
        Vector3 At(float u, float v, float x) => new(x, BannerMiddle - v, -u);
        FlatQuad(m, At(-w / 2, -h / 2, BannerOut), At(w / 2, -h / 2, BannerOut), At(w / 2, h / 2, BannerOut), At(-w / 2, h / 2, BannerOut), TagWhite, front);
        // a red hem along its top and bottom
        foreach (var v0 in new[] { -h / 2, h / 2 - BannerPixel * 0.5f })
            FlatQuad(m, At(-w / 2, v0, BannerOut + 2), At(w / 2, v0, BannerOut + 2), At(w / 2, v0 + BannerPixel * 0.5f, BannerOut + 2), At(-w / 2, v0 + BannerPixel * 0.5f, BannerOut + 2), InkRed, front);
        // each line in the middle of the cloth's width, a glyph's rows apart
        for (var line = 0; line < BannerLines.Length; line++)
        {
            float left = -TextRuns(BannerLines[line]).Columns * BannerPixel / 2, top = -h / 2 + BannerPixel * (1 + line * GlyphRows);
            TextQuads(m, BannerLines[line], (c, r) => At(left + c * BannerPixel, top + r * BannerPixel, BannerOut + 4), InkRed, front);
        }
        return m.ToBody(header);
    }

    // ---- the bikes: along z (its front +z), upright, its wheels on y = o.Y; the racing ones with drop bars and thinner tyres; a basket;
    // a price tag (its colour, -1 none) hung from the bars' right end
    private static void AddBike(CarMesh m, Vector3 o, int colour, bool racing, bool basket = false, int tag = -1)
    {
        const float R = BikeWheel, Base = BikeBase;
        int P(Vector3 v) => m.P(0, o + v);
        void Bar(Vector3 a, Vector3 b, float t, int c) => m.Strut(0, o + a, o + b, t, c);
        var tyre = racing ? 24f : 34f;
        foreach (var hz in new[] { -Base, Base })
        {
            // the tyre: one smooth ring round the hub, the spokes as lines
            ShopTyre(m, o + new Vector3(0, R, hz), R, tyre, 16);
            var hub = P(new Vector3(0, R, hz));
            for (var k = 0; k < 8; k++) m.Line(hub, P(new Vector3(0, R + (R - 20) * MathF.Sin(k * MathF.Tau / 8), hz + (R - 20) * MathF.Cos(k * MathF.Tau / 8))), ShopSteel);
        }
        Vector3 bracket = new(0, R + 10, -30), seat = new(0, BikeSeat, -170), head = new(0, 770, 400), headLow = new(0, 640, 420);
        Vector3 rear = new(0, R, -Base), front = new(0, R, Base);
        Bar(bracket, seat, 34, colour);
        Bar(seat, head, 32, colour);
        Bar(bracket, headLow, 36, colour);
        Bar(bracket, rear, 24, colour);
        Bar(seat, rear, 22, colour);
        Bar(headLow, front, 26, colour);
        Bar(head, headLow, 34, colour);
        // the saddle, the stem, the bars
        Bar(seat, seat + new Vector3(0, 70, 0), 22, ShopSteel);
        m.Box(0, o + new Vector3(0, 845, -175), new Vector3(110, 40, 230), ShopDark);
        Bar(head, head + new Vector3(0, 110, -30), 22, ShopSteel);
        if (racing)
        {
            Bar(new Vector3(-210, 880, 370), new Vector3(210, 880, 370), 22, ShopSteel);
            foreach (var s in new[] { -1, 1 })
            {
                Bar(new Vector3(s * 210, 880, 370), new Vector3(s * 210, 880, 450), 26, ShopDark);
                Bar(new Vector3(s * 210, 880, 450), new Vector3(s * 210, 790, 480), 26, ShopDark);
                Bar(new Vector3(s * 210, 790, 480), new Vector3(s * 210, 760, 410), 26, ShopDark);
            }
        }
        else
        {
            Bar(new Vector3(-230, 880, 370), new Vector3(230, 880, 370), 22, ShopSteel);
            foreach (var s in new[] { -1, 1 }) Bar(new Vector3(s * 230, 880, 370), new Vector3(s * 290, 880, 370), 30, ShopDark);
        }
        // the pedals
        Bar(new Vector3(-90, R - 60, -30), new Vector3(90, R + 80, -30), 20, ShopSteel);
        foreach (var (x, y) in new[] { (-100f, R - 60f), (100f, R + 80f) }) m.Box(0, o + new Vector3(x, y, -30), new Vector3(70, 18, 90), ShopDark);
        if (basket)
        {
            // a wicker basket on the front, over the wheel
            foreach (var (x, z) in new[] { (-140f, 470f), (140f, 470f), (-140f, 700f), (140f, 700f) }) Bar(new Vector3(x, 620, z), new Vector3(x, 820, z), 16, ShopWood);
            m.Box(0, o + new Vector3(0, 620, 585), new Vector3(300, 20, 250), ShopWood);
            Bar(new Vector3(-140, 820, 470), new Vector3(140, 820, 470), 16, ShopWood);
            Bar(new Vector3(-140, 820, 700), new Vector3(140, 820, 700), 16, ShopWood);
            Bar(new Vector3(-140, 820, 470), new Vector3(-140, 820, 700), 16, ShopWood);
            Bar(new Vector3(140, 820, 470), new Vector3(140, 820, 700), 16, ShopWood);
        }
        if (tag >= 0)
        {
            // the price tag: a card on a string from the bars, seen from both sides, a red line across it
            var end = racing ? new Vector3(210, 880, 410) : new Vector3(260, 880, 370);
            float tx = end.X + 30;
            m.Line(P(end), P(new Vector3(tx, 780, end.Z)), ShopWood);
            m.Flat(0, tag, o + new Vector3(tx, 640, end.Z - 50), o + new Vector3(tx, 640, end.Z + 50), o + new Vector3(tx, 780, end.Z + 50), o + new Vector3(tx, 780, end.Z - 50));
            m.Flat(0, InkRed, o + new Vector3(tx + 3, 700, end.Z - 40), o + new Vector3(tx + 3, 700, end.Z + 40), o + new Vector3(tx + 3, 715, end.Z + 40), o + new Vector3(tx + 3, 715, end.Z - 40));
        }
    }

    // the town bike (1,000 kashes) on a floor stand that holds its front wheel; its price tag
    private static Body BuildShopTownBike(byte[] header)
    {
        var m = new CarMesh("the cycle shop's town bike", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        AddBike(m, Vector3.Zero, Green, racing: false, basket: true, tag: TagWhite);
        m.Box(0, new Vector3(0, 15, BikeBase), new Vector3(200, 30, 420), ShopSteel);
        foreach (var s in new[] { -1, 1 })
            foreach (var dz in new[] { -170f, 170f })
                m.Strut(0, new Vector3(s * 55, 30, BikeBase + dz), new Vector3(s * 45, 230, BikeBase + dz * 0.6f), 22, ShopSteel);
        return m.ToBody(header);
    }

    // the racing bike (2,000 kashes) hung side on on the north wall: lifted HungLow, two hooks from the wall (local +x, the actor turned
    // 1024 so the bike runs along the wall) under its top tube; its price tag
    public const float HungOut = 280;
    private static Body BuildShopHungBike(byte[] header)
    {
        var m = new CarMesh("the cycle shop's racing bike, on the wall", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        AddBike(m, new Vector3(0, HungLow, 0), ShopGold, racing: true, tag: TagWhite);
        foreach (var z in new[] { -120f, 250f })
        {
            var y = HungLow + 770 - 30 - (z + 120) * 0.02f;
            m.Strut(0, new Vector3(HungOut, y + 60, z), new Vector3(0, y - 30, z), 26, ShopSteel);
            m.Strut(0, new Vector3(0, y - 30, z), new Vector3(-50, y + 20, z), 22, ShopSteel);
            m.Box(0, new Vector3(HungOut - 15, y + 60, z), new Vector3(30, 120, 80), ShopSteel);
        }
        return m.ToBody(header);
    }

    // the champion's bike (5,000 kashes), as raced at the Dome of the Slate: teal, drop bars, on a little stand under its back hub (it stands
    // on the plinth, ShowPlinth); its price tag
    private static Body BuildShopChampionBike(byte[] header)
    {
        var m = new CarMesh("the cycle shop's champion's bike", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        AddBike(m, Vector3.Zero, ShopTeal, racing: true, tag: TagWhite);
        foreach (var s in new[] { -1, 1 })
            m.Strut(0, new Vector3(s * 160, 0, -BikeBase - 60), new Vector3(s * 30, BikeWheel, -BikeBase), 20, ShopGold);
        return m.ToBody(header);
    }

    // a racer's bike in for repair (on the repair stand): red, drop bars, a red tag
    private static Body BuildRepairBike(byte[] header)
    {
        var m = new CarMesh("a racer's bike, in for repair", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        AddBike(m, Vector3.Zero, ShopRed, racing: true, tag: InkRed);
        return m.ToBody(header);
    }

    // the plinth the champion's bike stands on: a round dais, dark, a strip of light round its top edge
    private static Body BuildShowPlinth(byte[] header)
    {
        var m = new CarMesh("the cycle shop's plinth", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        const int N = 20;
        const float R = 720;
        var bottom = m.Loop(0, Vector3.Zero, new Vector3(R, 0, 0), new Vector3(0, 0, R), N);
        var top = m.Loop(0, new Vector3(0, PlinthTop - 30, 0), new Vector3(R, 0, 0), new Vector3(0, 0, R), N);
        var lip = m.Loop(0, new Vector3(0, PlinthTop, 0), new Vector3(R - 10, 0, 0), new Vector3(0, 0, R - 10), N);
        var middle = new Vector3(0, PlinthTop / 2, 0);
        m.Skin(bottom, top, _ => ShopDark, middle);
        for (var k = 0; k < N; k++) m.Out(new[] { top[k], top[(k + 1) % N], lip[(k + 1) % N], lip[k] }, InkWhite, middle, unlit: true);
        m.Cap(lip, m.P(0, new Vector3(0, PlinthTop, 0)), ShopDark, new Vector3(0, -1000, 0));
        return m.ToBody(header);
    }

    // A tyre across y and z (its bike along z) round `hub`: a ring of radius r, its tube t thick, n segments round and five round its tube,
    // every point shared -- lit smoothly all round, as a tyre is (it was a ring of square bars, flat)
    private static void ShopTyre(CarMesh m, Vector3 hub, float r, float t, int n)
    {
        const int Round = 5;
        var g = new int[n, Round];
        m.Light = 1f;
        for (var k = 0; k < n; k++)
            for (var j = 0; j < Round; j++)
            {
                float a = k * MathF.Tau / n, b = j * MathF.Tau / Round;
                var radial = new Vector3(0, MathF.Sin(a), MathF.Cos(a));
                g[k, j] = m.P(0, hub + radial * (r + t / 2 * MathF.Cos(b)) + Vector3.UnitX * (t / 2 * MathF.Sin(b)));
            }
        for (var k = 0; k < n; k++)
            for (var j = 0; j < Round; j++)
            {
                int k1 = (k + 1) % n, j1 = (j + 1) % Round;
                float a = (k + 0.5f) * MathF.Tau / n;
                var ring = hub + new Vector3(0, MathF.Sin(a), MathF.Cos(a)) * r;
                m.Out(new[] { g[k, j], g[k1, j], g[k1, j1], g[k, j1] }, ShopRubber, ring);
            }
    }

    // a wheel across y and z (its bike along z) at `hub`: a tyre of n segments, `spokes` lines to the hub
    private static void ShopWheel(CarMesh m, Vector3 hub, float r, int n, float t, int spokes)
    {
        ShopTyre(m, hub, r, t, Math.Max(12, n));
        var h = m.P(0, hub);
        for (var k = 0; k < spokes; k++)
        {
            var a = k * MathF.Tau / spokes;
            m.Line(h, m.P(0, hub + new Vector3(0, (r - t / 2) * MathF.Sin(a), (r - t / 2) * MathF.Cos(a))), ShopSteel);
        }
    }

    // The penny-farthing: a great front wheel and a little one behind, a curved backbone between, the saddle high over the front wheel --
    // on a low wooden plinth, a brass plaque on each long side. Along z (its front +z); its foot at 0
    private static Body BuildPennyFarthing(byte[] header)
    {
        var m = new CarMesh("the cycle shop's penny-farthing", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        const float Lift = 110, Big = 640, Small = 170, BigZ = 300, SmallZ = -780;
        m.Box(0, new Vector3(0, Lift / 2, -220), new Vector3(560, Lift, 2050), ShopWood);
        foreach (var s in new[] { -1f, 1f })
            FlatQuad(m, new(s * 282, 25, -480), new(s * 282, 25, 40), new(s * 282, 85, 40), new(s * 282, 85, -480), ShopGold, new(s * 2000, 55, -220), unlit: false);
        ShopWheel(m, new Vector3(0, Lift + Big, BigZ), Big, 22, 44, 12);
        ShopWheel(m, new Vector3(0, Lift + Small, SmallZ), Small, 10, 34, 6);
        var head = new Vector3(0, Lift + 2 * Big + 50, BigZ - 20);
        Vector3[] spine =
        {
            head, new(0, Lift + 2 * Big + 10, BigZ - 230), new(0, Lift + 2 * Big - 130, BigZ - 520), new(0, Lift + Big + 110, BigZ - 800),
            new(0, Lift + Big - 230, BigZ - 1000), new(0, Lift + Small + 60, SmallZ + 30),
        };
        for (var k = 0; k + 1 < spine.Length; k++) m.Strut(0, spine[k], spine[k + 1], 44, ShopDark);
        foreach (var s in new[] { -1f, 1f })
        {
            m.Strut(0, head + new Vector3(s * 70, 0, 0), new Vector3(s * 70, Lift + Big, BigZ), 30, ShopDark);
            m.Strut(0, spine[^1] + new Vector3(s * 40, 0, 0), new Vector3(s * 40, Lift + Small, SmallZ), 20, ShopDark);
        }
        m.Box(0, new Vector3(0, Lift + 2 * Big + 80, BigZ - 250), new Vector3(150, 50, 260), ShopWood);
        m.Strut(0, head, head + new Vector3(0, 90, 30), 30, ShopSteel);
        m.Strut(0, new Vector3(-330, head.Y + 90, head.Z + 30), new Vector3(330, head.Y + 90, head.Z + 30), 26, ShopSteel);
        foreach (var s in new[] { -1f, 1f })
        {
            m.Strut(0, new Vector3(s * 330, head.Y + 90, head.Z + 30), new Vector3(s * 360, head.Y + 20, head.Z - 40), 34, ShopWood);
            // the cranks and pedals at the great wheel's hub
            m.Strut(0, new Vector3(s * 90, Lift + Big, BigZ), new Vector3(s * 90, Lift + Big - s * 150, BigZ + s * 60), 26, ShopSteel);
            m.Box(0, new Vector3(s * 140, Lift + Big - s * 150, BigZ + s * 60), new Vector3(90, 24, 110), ShopDark);
        }
        return m.ToBody(header);
    }

    // The tandem: a bike for two -- two saddles, two pairs of bars (the back one on the front seat's post), its wheels 2,000 apart. Red and
    // cream. Along z (its front +z); its foot at 0
    private static Body BuildTandem(byte[] header)
    {
        var m = new CarMesh("the cycle shop's tandem", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        const float R = BikeWheel, Front = 1000, Back = -1000;
        ShopWheel(m, new Vector3(0, R, Front), R, 14, 34, 8);
        ShopWheel(m, new Vector3(0, R, Back), R, 14, 34, 8);
        Vector3 frontBracket = new(0, R + 10, 250), backBracket = new(0, R + 10, -500);
        Vector3 captain = new(0, BikeSeat, 120), stoker = new(0, BikeSeat, -640), head = new(0, 770, Front - 120), headLow = new(0, 640, Front - 100);
        void Bar(Vector3 a, Vector3 b, float t, int c) => m.Strut(0, a, b, t, c);
        Bar(frontBracket, captain, 34, ShopRed);
        Bar(backBracket, stoker, 34, ShopRed);
        Bar(stoker, captain, 30, ShopCream);
        Bar(captain, head, 30, ShopCream);
        Bar(frontBracket, headLow, 34, ShopRed);
        Bar(backBracket, frontBracket, 34, ShopRed);
        Bar(backBracket, new Vector3(0, R, Back), 24, ShopRed);
        Bar(stoker, new Vector3(0, R, Back), 22, ShopRed);
        Bar(headLow, new Vector3(0, R, Front), 26, ShopRed);
        Bar(head, headLow, 34, ShopRed);
        foreach (var s in new[] { captain, stoker })
        {
            Bar(s, s + new Vector3(0, 70, 0), 22, ShopSteel);
            m.Box(0, s + new Vector3(0, 85, -5), new Vector3(110, 40, 230), ShopDark);
        }
        // the captain's bars on the stem; the stoker's on the captain's seat post
        Bar(head, head + new Vector3(0, 110, -30), 22, ShopSteel);
        Bar(new Vector3(-230, 880, Front - 150), new Vector3(230, 880, Front - 150), 22, ShopSteel);
        Bar(captain + new Vector3(0, 40, 0), new Vector3(0, 860, captain.Z - 140), 22, ShopSteel);
        Bar(new Vector3(-200, 860, captain.Z - 140), new Vector3(200, 860, captain.Z - 140), 22, ShopSteel);
        foreach (var s in new[] { -1, 1 })
        {
            Bar(new Vector3(s * 230, 880, Front - 150), new Vector3(s * 290, 880, Front - 150), 30, ShopDark);
            Bar(new Vector3(s * 200, 860, captain.Z - 140), new Vector3(s * 250, 860, captain.Z - 140), 30, ShopDark);
        }
        foreach (var b in new[] { frontBracket, backBracket })
        {
            Bar(b + new Vector3(-90, -70, 0), b + new Vector3(90, 70, 0), 20, ShopSteel);
            m.Box(0, b + new Vector3(-100, -70, 0), new Vector3(70, 18, 90), ShopDark);
            m.Box(0, b + new Vector3(100, 70, 0), new Vector3(70, 18, 90), ShopDark);
        }
        return m.ToBody(header);
    }

    // The fitting bike: a racing bike, steel grey, its back wheel held in a trainer on a roller, its front on a riser block. Along z; foot at 0
    private static Body BuildFitBike(byte[] header)
    {
        var m = new CarMesh("the cycle shop's fitting bike", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        const float Up = 70;
        AddBike(m, new Vector3(0, Up, 0), ShopSteel, racing: true);
        m.Box(0, new Vector3(0, Up / 2, -BikeBase), new Vector3(300, Up, 220), ShopDark);
        m.Box(0, new Vector3(0, Up / 2, BikeBase), new Vector3(280, Up, 400), ShopDark);
        foreach (var s in new[] { -1f, 1f })
            foreach (var dz in new[] { -260f, 260f })
                m.Strut(0, new Vector3(s * 300, 0, -BikeBase + dz), new Vector3(s * 60, Up + BikeWheel, -BikeBase), 26, ShopSteel);
        return m.ToBody(header);
    }

    // A stand of saddles: a post on a foot, arms out either side (along z) at three heights, a saddle on each. Foot at 0
    private static Body BuildSaddleStand(byte[] header)
    {
        var m = new CarMesh("the cycle shop's stand of saddles", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        m.Box(0, new Vector3(0, 20, 0), new Vector3(500, 40, 500), ShopDark);
        m.Strut(0, new Vector3(0, 40, 0), new Vector3(0, 1550, 0), 50, ShopSteel);
        var colours = new[] { ShopDark, ShopRed, ShopWhite, ShopWood, ShopDark, ShopTeal };
        var k = 0;
        foreach (var y in new[] { 750f, 1050f, 1350f })
            foreach (var s in new[] { -1f, 1f })
            {
                m.Strut(0, new Vector3(0, y, 0), new Vector3(0, y, s * 330), 24, ShopSteel);
                m.Box(0, new Vector3(0, y + 30, s * 360), new Vector3(120, 40, 240), colours[k++]);
            }
        return m.ToBody(header);
    }

    // The signed jerseys: three framed racing jerseys in a row on the north wall's face (z 0), facing +z, a shelf with a cup under the
    // middle one. Foot at 0
    private static Body BuildJerseyWall(byte[] header)
    {
        var m = new CarMesh("the cycle shop's signed jerseys", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        const float W = 700, H = 820, Low = 1080, Gap = 780, Rim = 50;
        var front = new Vector3(0, Low + H / 2, 2000);
        var jerseys = new[] { (ShopYellow, ShopDark), (ShopRed, ShopWhite), (ShopBlue, ShopWhite) };
        for (var k = 0; k < 3; k++)
        {
            var cx = (k - 1) * Gap;
            m.Box(0, new Vector3(cx, Low + H - Rim / 2, 25), new Vector3(W, Rim, 50), ShopWood);
            m.Box(0, new Vector3(cx, Low + Rim / 2, 25), new Vector3(W, Rim, 50), ShopWood);
            m.Box(0, new Vector3(cx - W / 2 + Rim / 2, Low + H / 2, 25), new Vector3(Rim, H - 2 * Rim, 50), ShopWood);
            m.Box(0, new Vector3(cx + W / 2 - Rim / 2, Low + H / 2, 25), new Vector3(Rim, H - 2 * Rim, 50), ShopWood);
            FlatQuad(m, new(cx - W / 2 + Rim, Low + Rim, 20), new(cx + W / 2 - Rim, Low + Rim, 20), new(cx + W / 2 - Rim, Low + H - Rim, 20), new(cx - W / 2 + Rim, Low + H - Rim, 20), ShopCream, front, unlit: false);
            var (body, band) = jerseys[k];
            float jl = cx - 140, jr = cx + 140, jb = Low + 150, jt = Low + H - 200, z = 30;
            // the body, its sleeves out from the shoulders, a band across it
            FlatQuad(m, new(jl, jb, z), new(jr, jb, z), new(jr, jt, z), new(jl, jt, z), body, front, unlit: false);
            FlatQuad(m, new(jl, jt, z), new(jl - 150, jt - 170, z), new(jl - 80, jt - 230, z), new(jl, jt - 110, z), body, front, unlit: false);
            FlatQuad(m, new(jr, jt, z), new(jr + 150, jt - 170, z), new(jr + 80, jt - 230, z), new(jr, jt - 110, z), body, front, unlit: false);
            FlatQuad(m, new(jl, jt - 260, z + 2), new(jr, jt - 260, z + 2), new(jr, jt - 190, z + 2), new(jl, jt - 190, z + 2), band, front, unlit: false);
            // a signature scrawled across it
            m.Line(m.P(0, new Vector3(jl + 30, jb + 90, z + 4)), m.P(0, new Vector3(jl + 120, jb + 150, z + 4)), InkBlack);
            m.Line(m.P(0, new Vector3(jl + 120, jb + 150, z + 4)), m.P(0, new Vector3(jr - 40, jb + 80, z + 4)), InkBlack);
        }
        // the shelf under the middle frame and its cup
        m.Box(0, new Vector3(0, Low - 120, 110), new Vector3(760, 40, 220), ShopWood);
        m.Box(0, new Vector3(0, Low - 70, 110), new Vector3(150, 60, 150), ShopDark);
        m.Strut(0, new Vector3(0, Low - 40, 110), new Vector3(0, Low + 40, 110), 40, ShopGold);
        var low = m.Loop(0, new Vector3(0, Low + 40, 110), new Vector3(50, 0, 0), new Vector3(0, 0, 50), 8);
        var high = m.Loop(0, new Vector3(0, Low + 200, 110), new Vector3(120, 0, 0), new Vector3(0, 0, 120), 8);
        m.Skin(low, high, _ => ShopGold, new Vector3(0, Low + 120, 110));
        m.Skin(high, low, _ => ShopGold, new Vector3(0, Low + 2000, 110));
        return m.ToBody(header);
    }

    // The workshop's wall: a pegboard of tools on the west wall's face (x 0), facing +x, a workbench under it with a vice, its middle at z 0,
    // foot at 0
    private static Body BuildToolWall(byte[] header)
    {
        var m = new CarMesh("the cycle shop's tool wall and bench", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        var front = new Vector3(2000, 1300, 0);
        m.Box(0, new Vector3(15, 1380, 0), new Vector3(30, 900, 1700), ShopWoodLight);
        void Tool(float z0, float z1, float y0, float y1, int c) => FlatQuad(m, new(34, y0, z0), new(34, y0, z1), new(34, y1, z1), new(34, y1, z0), c, front);
        // spanners in a row, longer to the right; a hammer; screwdrivers with red handles; a saw
        for (var k = 0; k < 5; k++)
        {
            var z = -700 + k * 85f;
            Tool(z, z + 34, 1720 - 260 - k * 40, 1720, ShopSteel);
            Tool(z - 14, z + 48, 1660, 1720, ShopSteel);
        }
        Tool(-200, -160, 1150, 1520, ShopWood);
        Tool(-270, -90, 1520, 1600, ShopSteel);
        foreach (var z in new[] { 30f, 110f, 190f })
        {
            Tool(z, z + 22, 1420, 1650, ShopSteel);
            Tool(z - 10, z + 32, 1280, 1420, InkRed);
        }
        FlatQuad(m, new(34, 1180, 330), new(34, 1180, 700), new(34, 1420, 700), new(34, 1300, 330), ShopSteel, front);
        Tool(260, 340, 1260, 1420, ShopWood);
        // the bench
        m.Box(0, new Vector3(330, 860, 0), new Vector3(620, 60, 1800), ShopWood);
        foreach (var (x, z) in new[] { (60f, -840f), (600f, -840f), (60f, 840f), (600f, 840f) })
            m.Box(0, new Vector3(x, 415, z), new Vector3(60, 830, 60), ShopSteel);
        m.Box(0, new Vector3(330, 260, 0), new Vector3(580, 30, 1700), ShopWood);
        m.Box(0, new Vector3(470, 960, 640), new Vector3(200, 140, 170), ShopDark);
        m.Box(0, new Vector3(470, 1050, 640), new Vector3(240, 40, 60), ShopSteel);
        return m.ToBody(header);
    }

    // The coffee bar: a counter along x, red, a wooden top, an espresso machine and cups on it -- the racers' coffee after training. Foot at
    // 0, its middle at x 0, z 0
    private static Body BuildCafeBar(byte[] header)
    {
        var m = new CarMesh("the cycle shop's coffee bar", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        const float Long = 1800, Deep = 560, Top = 1000;
        // the counter (its front -z, to the room): a dark plinth, the red body, its front in raised panels, a wooden top with a rounded
        // front edge, a brass foot rail on brackets (2026-11-24: one red box before, flat -- the user, "increase the quality of all of the
        // models in the cycle shop, they look very flat and one dimensional")
        m.Box(0, new Vector3(0, 60, 20), new Vector3(Long - 160, 120, Deep - 120), ShopDark);
        m.Box(0, new Vector3(0, 120 + (Top - 170) / 2, 0), new Vector3(Long - 100, Top - 170, Deep - 60), ShopRed);
        const int Panels = 4;
        var panelW = (Long - 100) / Panels;
        for (var k = 0; k < Panels; k++)
        {
            var px = -(Long - 100) / 2 + panelW * (k + 0.5f);
            m.Box(0, new Vector3(px, 120 + (Top - 170) / 2, -(Deep - 60) / 2 - 14), new Vector3(panelW - 90, Top - 330, 28), ShopRed);
        }
        m.Box(0, new Vector3(0, Top - 25, 0), new Vector3(Long, 50, Deep), ShopWood);
        HotelKit.Rod(m, new Vector3(-Long / 2, Top - 30, -Deep / 2), new Vector3(Long / 2, Top - 30, -Deep / 2), 30, new Finish(ShopWood, 1), 8);
        HotelKit.Rod(m, new Vector3(-Long / 2 + 80, 210, -Deep / 2 - 130), new Vector3(Long / 2 - 80, 210, -Deep / 2 - 130), 22, new Finish(ShopGold, 1), 8);
        foreach (var x in new[] { -Long / 2 + 120, 0f, Long / 2 - 120 })
            HotelKit.Rod(m, new Vector3(x, 210, -Deep / 2 - 130), new Vector3(x, 210, -Deep / 2 + 20), 14, new Finish(ShopGold, 1), 6);
        // the espresso machine: a steel box with a red band, two round group heads and a cup under each, a gauge on its front
        m.Box(0, new Vector3(-480, Top + 170, 0), new Vector3(460, 300, 320), ShopSteel);
        m.Box(0, new Vector3(-480, Top + 330, 0), new Vector3(470, 26, 330), InkRed);
        HotelKit.Orb(m, new Vector3(-480, Top + 240, -168), 44, new Finish(ShopWhite, 1), 8, 4);
        foreach (var x in new[] { -580f, -380f })
        {
            HotelKit.Turned(m, new Vector3(x, Top + 70, -190), new[] { (0f, 0f), (38f, 0f), (40f, 40f), (28f, 60f), (0f, 60f) }, 8, new Finish(ShopDark, 1));
            HotelKit.Turned(m, new Vector3(x, Top, -200), new[] { (0f, 0f), (26f, 0f), (32f, 50f), (34f, 60f), (0f, 60f) }, 8, new Finish(ShopWhite, 1));
        }
        // three cups and their saucers along the counter
        foreach (var x in new[] { 150f, 260f, 370f })
        {
            HotelKit.Turned(m, new Vector3(x, Top, 60), new[] { (0f, 0f), (55f, 0f), (58f, 8f), (0f, 8f) }, 10, new Finish(ShopWhite, 1));
            HotelKit.Turned(m, new Vector3(x, Top + 8, 60), new[] { (0f, 0f), (30f, 0f), (38f, 55f), (40f, 62f), (0f, 62f) }, 10, new Finish(ShopWhite, 1));
        }
        return m.ToBody(header);
    }

    // The back door: shut, in a wooden frame, flat on the west wall's face (x 0), facing +x into the shop, its middle at z 0 (as the club's
    // back rooms' doorway, WingDoorway) -- a door of dark planks, two sunk panels, a gold knob. Locked: Action at it, "It's locked." (the
    // user, 2026-11-22, "The cycle shop, let's add a door at the back and have it locked. We may add something there later, but we definitely
    // don't want it to lead into the original shop" -- it was an open doorway through to the baggage claim's hall)
    private static Body BuildShopDoorway(byte[] header)
    {
        var m = new CarMesh("the cycle shop's back door", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        const float Half = 450, Top = 1800, Rim = 80;
        void Face(float z0, float z1, float y0, float y1, float off, int colour, bool unlit = false) =>
            m.In(new[] { m.P(0, new Vector3(off, y0, z0)), m.P(0, new Vector3(off, y0, z1)), m.P(0, new Vector3(off, y1, z1)), m.P(0, new Vector3(off, y1, z0)) },
                 colour, new Vector3(off + 500, (y0 + y1) / 2, (z0 + z1) / 2), unlit);
        // the door: its leaf, two panels sunk in it (darker), and its knob on the far side from the hinges
        Face(-Half, Half, 0, Top, 20, ShopWood);
        m.Box(0, new Vector3(34, (Top * 0.55f + Top - 110) / 2, 0), new Vector3(24, Top * 0.45f - 110, 2 * Half - 180), ShopWood);
        m.Box(0, new Vector3(34, (110 + Top * 0.45f) / 2, 0), new Vector3(24, Top * 0.45f - 110, 2 * Half - 180), ShopWood);
        m.Box(0, new Vector3(60, 950, Half - 110), new Vector3(60, 70, 70), ShopGold);
        m.Box(0, new Vector3(30, Top + 50, 0), new Vector3(60, 100, 2 * (Half + Rim)), ShopWood);
        m.Box(0, new Vector3(30, Top / 2, -Half - Rim / 2), new Vector3(60, Top, Rim), ShopWood);
        m.Box(0, new Vector3(30, Top / 2, Half + Rim / 2), new Vector3(60, Top, Rim), ShopWood);
        // (a sign over it: STAFF ONLY's plank, lighter wood)
        m.Box(0, new Vector3(28, Top + 230, 0), new Vector3(30, 180, 600), ShopWoodLight);
        return m.ToBody(header);
    }

    // The repair stand: a cross of feet, a post, an arm out over the bike's middle and its clamp (the bike itself a RepairBike prop lifted
    // onto it). Its arm along x; its foot at 0
    public const int StandHigh = 950;
    private static Body BuildRepairStand(byte[] header)
    {
        var m = new CarMesh("the cycle shop's repair stand", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        m.Box(0, new Vector3(0, 20, 0), new Vector3(700, 40, 60), ShopSteel);
        m.Box(0, new Vector3(0, 20, 0), new Vector3(60, 40, 700), ShopSteel);
        m.Box(0, new Vector3(0, StandHigh / 2f, 0), new Vector3(70, StandHigh, 70), ShopSteel);
        m.Box(0, new Vector3(0, StandHigh, 0), new Vector3(110, 60, 260), Red);
        return m.ToBody(header);
    }

    // The wall of wheels: a pegboard on the north wall's face (z 0), facing +z, four bike wheels hung on it in two rows (six went past the
    // engine's points) (each a rim of short lengths round its hub, as the bikes' are), its foot at 0
    private static Body BuildWheelWall(byte[] header)
    {
        var m = new CarMesh("the cycle shop's wall of wheels", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        const float W = 1700, Low = 420, High = 1820, R = 260, Off = 40;
        m.Box(0, new Vector3(0, (Low + High) / 2, 15), new Vector3(W, High - Low, 30), ShopWoodLight);
        foreach (var row in new[] { Low + 380f, Low + 1040f })
            foreach (var col in new[] { -W / 4, W / 4 })
            {
                const int N = 10;
                for (var k = 0; k < N; k++)
                {
                    float a0 = k * MathF.Tau / N, a1 = (k + 1) * MathF.Tau / N;
                    m.Strut(0, new Vector3(col + R * MathF.Cos(a0), row + R * MathF.Sin(a0), Off + 40), new Vector3(col + R * MathF.Cos(a1), row + R * MathF.Sin(a1), Off + 40), 34, ShopRubber);
                }
                m.Box(0, new Vector3(col, row, Off + 40), new Vector3(60, 60, 50), ShopSteel);
            }
        return m.ToBody(header);
    }

    private static Body BuildLeaningBoard(byte[] header)
    {
        var m = new CarMesh("the Hoverboard, leaning on a wall", new[] { -1 });
        m.Rounded = true; m.Limit = 1100;
        m.Light = 1f;
        // (the board's frame -- x across, y its deck's way, z its nose -- stood on its tail: its nose up, its deck to +z; a turn, not a
        // mirror, as the inventory's model (CitadelHoverboard.Model); then leant back, its top to -z)
        var lean = Matrix4x4.CreateRotationX(-BoardLean);
        Vector3 Stood(Vector3 v) => Vector3.Transform(new Vector3(-v.X, v.Z, v.Y), lean);
        var faces = CitadelHoverboard.Faces(CitadelHoverboard.Plain);
        var all = faces.SelectMany(f => f.Corners).Select(Stood).ToList();
        // (its foot on the floor, its top's back at z 0 -- against the wall behind the actor's place)
        var shift = new Vector3(0, -all.Min(p => p.Y), -all.Min(p => p.Z));
        foreach (var (corners, colour, lit) in faces)
            m.Raw(corners.Select(c => m.P(0, Stood(c) + shift)).ToArray(), colour, unlit: !lit);
        return m.ToBody(header);
    }
}
