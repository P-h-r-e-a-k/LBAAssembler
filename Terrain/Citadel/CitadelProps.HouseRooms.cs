using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The fittings of Twinsen's new bathroom and kitchen (CitadelHouseRooms; the user, 2026-11-16: "a bathroom area, with toilet, sink, shower,
// and a large corner bath" and "a kitchen area" -- LBA1's house as the inspiration, its kitchen's long counter of cupboards with the sink and
// the hob and a tall glass-fronted cabinet at the end, its bath on a rug; the look the LBA2 house's: its brown woods, brass, teal): each built
// round its own place on the floor, its back to -z (a wall) unless it says otherwise. The rooms' cells: floor x 5-14, z 1-8; the north wall's
// face at z 512, the west wall's at x 2,560.
internal static partial class CitadelProps
{
    public const int Shower = 140, BathSink = 141, Toilet = 142, CornerBath = 143, TowelRail = 144, BathMat = 145;
    public const int KitchenCounter = 146, KitchenRange = 147, Dresser = 148, KitchenTable = 149, PlateShelf = 150;
    private const int HrPorcelain = 3 * 16 + 14, HrPorcelainShade = 3 * 16 + 11, HrChrome = 3 * 16 + 10, HrWood = 1 * 16 + 5, HrWoodDark = 1 * 16 + 3,
        HrWoodLight = 1 * 16 + 8, HrBrass = 6 * 16 + 8, HrTeal = 10 * 16 + 6, HrTealDark = 10 * 16 + 3, HrTile = 3 * 16 + 12, HrIron = 3 * 16 + 2, HrCopper = 5 * 16 + 7;
    private const int HrNorthFace = 512, HrWestFace = 2560;

    // (generic, x, z, facing) of each room's fittings
    public static readonly (int Generic, int X, int Z, int Beta)[] BathroomFittings =
    {
        (Shower, 6 * 512, 2 * 512, 0),                       // the north-west corner: cells 5-6, 1-2
        (BathSink, 8 * 512 + 256, HrNorthFace + 230, 0),        // against the north wall, its mirror over it
        (Toilet, 10 * 512 + 256, HrNorthFace + 280, 0),
        (CornerBath, 13 * 512 + 256, 2 * 512 + 256, 0),       // the north-east corner: cells 12-14, 1-3
        (TowelRail, HrWestFace + 30, 7 * 512, 1024),            // on the west wall, facing the room (+x)
        (BathMat, 11 * 512 + 256, 4 * 512 + 128, 0),          // before the bath
    };
    public static readonly (int Generic, int X, int Z, int Beta)[] KitchenFittings =
    {
        (KitchenCounter, 5 * 512 + 1536, HrNorthFace + 300, 0), // cells 5-10 along the north wall: cupboards, the worktop, the sink
        (KitchenRange, 11 * 512 + 512, HrNorthFace + 330, 0),          // cells 11-12: the range, its hob, a kettle and a pan
        (Dresser, 14 * 512, HrNorthFace + 260, 0),              // cells 13-14: the tall glass-fronted dresser
        (KitchenTable, 11 * 512 + 256, 5 * 512 + 512, 0),     // the table, the house's two chairs east of it
        (PlateShelf, HrWestFace + 40, 3 * 512, 1024),           // on the west wall: a shelf of plates
    };

    // a box's six faces seen from out; a cylinder's sides (n facets) between two heights
    private static void Cylinder(CarMesh m, Vector3 at, float r, float y0, float y1, int n, int colour, int top = -1)
    {
        var low = m.Loop(0, at + new Vector3(0, y0, 0), new Vector3(r, 0, 0), new Vector3(0, 0, r), n);
        var high = m.Loop(0, at + new Vector3(0, y1, 0), new Vector3(r, 0, 0), new Vector3(0, 0, r), n);
        m.Skin(low, high, _ => colour, at + new Vector3(0, (y0 + y1) / 2, 0));
        if (top >= 0) { var hub = m.P(0, at + new Vector3(0, y1, 0)); for (var k = 0; k < n; k++) m.Up(new[] { hub, high[k], high[(k + 1) % n] }, top); }
    }

    // The shower: a white tray a step up, two glass screens on its open sides in chrome frames, the chrome riser and its head in the corner
    public static Body BuildShower(byte[] header)
    {
        var m = new CarMesh("the shower", new[] { -1 });
        m.Light = 0.8f;
        const float H = 480, High = 1750;
        m.Box(0, new Vector3(0, 40, 0), new Vector3(2 * H, 80, 2 * H), HrPorcelain, top: HrPorcelainShade);
        m.Box(0, new Vector3(-H + 120, 84, -H + 120), new Vector3(60, 6, 60), HrIron);   // the drain
        // (the screens: the front (+z) and the east (+x) sides, a door's gap at the front's west end)
        m.Pane(0, HrTeal + 8, new Vector3(-H + 300, 80, H), new Vector3(H, 80, H), new Vector3(H, High, H), new Vector3(-H + 300, High, H));
        m.Pane(0, HrTeal + 8, new Vector3(H, 80, -H), new Vector3(H, 80, H), new Vector3(H, High, H), new Vector3(H, High, -H));
        foreach (var (a, b) in new[] { (new Vector3(-H + 300, 80, H), new Vector3(-H + 300, High, H)), (new Vector3(H, 80, H), new Vector3(H, High, H)), (new Vector3(H, 80, -H), new Vector3(H, High, -H)),
                                       (new Vector3(-H + 300, High, H), new Vector3(H, High, H)), (new Vector3(H, High, H), new Vector3(H, High, -H)) })
            m.Strut(0, a, b, 26, HrChrome);
        // (the riser up the corner, the head on its arm, the taps)
        m.Strut(0, new Vector3(-H + 70, 900, -H + 70), new Vector3(-H + 70, 1850, -H + 70), 30, HrChrome);
        m.Strut(0, new Vector3(-H + 70, 1850, -H + 70), new Vector3(-H + 300, 1850, -H + 300), 26, HrChrome);
        Cylinder(m, new Vector3(-H + 320, 0, -H + 320), 110, 1780, 1820, 8, HrChrome, HrChrome + 2);
        foreach (var dx in new[] { -60f, 60f }) m.Ball(0, new Vector3(-H + 70 + dx, 1000, -H + 30), 28, dx < 0 ? 4 * 16 + 8 : 12 * 16 + 8);
        return m.ToBody(header);
    }

    // The sink: a white basin on its pedestal, chrome taps, and over it on the wall a mirror in a brass frame, a little shelf under it
    public static Body BuildBathSink(byte[] header)
    {
        var m = new CarMesh("the bathroom's sink", new[] { -1 });
        m.Light = 0.8f;
        Cylinder(m, new Vector3(0, 0, 0), 110, 0, 620, 8, HrPorcelain);
        m.Box(0, new Vector3(0, 700, 0), new Vector3(600, 160, 420), HrPorcelain, top: HrPorcelainShade);
        m.Box(0, new Vector3(0, 781, 20), new Vector3(420, 4, 280), HrTeal + 4);   // (the water in its bowl)
        m.Strut(0, new Vector3(0, 780, -170), new Vector3(0, 900, -170), 30, HrChrome);
        m.Strut(0, new Vector3(0, 900, -170), new Vector3(0, 880, -60), 24, HrChrome);
        foreach (var dx in new[] { -110f, 110f }) m.Ball(0, new Vector3(dx, 810, -170), 30, HrChrome);
        // (the mirror: on the wall's face, -z of the basin's middle by its depth)
        const float Wall = -225;
        m.Box(0, new Vector3(0, 1350, Wall + 15), new Vector3(560, 560, 30), HrBrass);
        m.Box(0, new Vector3(0, 1350, Wall + 32), new Vector3(470, 470, 6), HrTeal + 9, top: HrTeal + 9);
        m.Box(0, new Vector3(0, 1030, Wall + 60), new Vector3(520, 30, 120), HrWoodLight);
        m.Box(0, new Vector3(-150, 1080, Wall + 60), new Vector3(50, 70, 50), 12 * 16 + 9);   // a tooth mug
        return m.ToBody(header);
    }

    // The toilet: a white bowl and pedestal, a wooden seat and lid (up), the cistern against the wall, its chrome flush handle
    public static Body BuildToilet(byte[] header)
    {
        var m = new CarMesh("the toilet", new[] { -1 });
        m.Light = 0.8f;
        m.Box(0, new Vector3(0, 160, 40), new Vector3(240, 320, 320), HrPorcelain);
        Cylinder(m, new Vector3(0, 0, 80), 190, 300, 400, 10, HrPorcelain, HrTeal + 3);
        // (the seat: a wooden ring on the rim; the lid up against the cistern)
        var outer = m.Loop(0, new Vector3(0, 410, 80), new Vector3(200, 0, 0), new Vector3(0, 0, 200), 10);
        var inner = m.Loop(0, new Vector3(0, 410, 80), new Vector3(120, 0, 0), new Vector3(0, 0, 120), 10);
        for (var k = 0; k < 10; k++) m.Up(new[] { outer[k], outer[(k + 1) % 10], inner[(k + 1) % 10], inner[k] }, HrWood);
        m.Box(0, new Vector3(0, 620, -150), new Vector3(400, 420, 40), HrWood);
        // (the cistern)
        m.Box(0, new Vector3(0, 650, -230), new Vector3(460, 500, 160), HrPorcelain, top: HrPorcelainShade);
        m.Strut(0, new Vector3(180, 840, -150), new Vector3(240, 840, -150), 24, HrChrome);
        return m.ToBody(header);
    }

    // The corner bath: big, its two backs against the walls (-x is open: the room's middle; the north-east corner is +x, -z), the front a
    // quarter round, white in a panel of the house's wood, a brass rim; the water; brass taps in the corner
    public static Body BuildCornerBath(byte[] header)
    {
        var m = new CarMesh("the corner bath", new[] { -1 });
        m.Light = 0.8f;
        const float R = 1450, Rim = 520, Inner = 1250;
        const int N = 12;
        var corner = new Vector3(760, 0, -760);
        // (round the corner: from the west wall's way (-x) to the room's (+z), a quarter circle about the corner)
        Vector3 Arc(float r, float y, int k) { var a = k * MathF.PI / 2 / N; return corner + new Vector3(-r * MathF.Cos(a), y, r * MathF.Sin(a)); }
        int[] Ring(float r, float y) => Enumerable.Range(0, N + 1).Select(k => m.P(0, Arc(r, y, k))).ToArray();
        var footOut = Ring(R, 0); var rimOut = Ring(R, Rim);
        var rimIn = Ring(Inner, Rim); var bottom = Ring(Inner, 120);
        for (var k = 0; k < N; k++)
        {
            m.Out(new[] { footOut[k], footOut[k + 1], rimOut[k + 1], rimOut[k] }, k % 2 == 0 ? HrWood : HrWoodDark, corner + new Vector3(0, Rim / 2, 0));
            m.Up(new[] { rimOut[k], rimOut[k + 1], rimIn[k + 1], rimIn[k] }, HrBrass);
            m.In(new[] { rimIn[k], rimIn[k + 1], bottom[k + 1], bottom[k] }, HrPorcelain, corner + new Vector3(0, Rim, 0));
        }
        // (the water, the bath's floor under it at the corner's fan; the two straight backs' tops along the walls)
        var hub = m.P(0, corner + new Vector3(0, 380, 0));
        var water = Ring(Inner, 380);
        for (var k = 0; k < N; k++) m.Up(new[] { hub, water[k], water[k + 1] }, HrTeal + 7);
        m.Box(0, new Vector3(corner.X - R / 2, Rim / 2, corner.Z + 40), new Vector3(R, Rim, 80), HrWood, top: HrBrass);
        m.Box(0, new Vector3(corner.X - 40, Rim / 2, corner.Z + R / 2), new Vector3(80, Rim, R), HrWood, top: HrBrass);
        foreach (var d in new[] { 150f, 300f }) m.Ball(0, corner + new Vector3(-d, Rim + 60, 140), 34, HrBrass);
        m.Strut(0, corner + new Vector3(-220, Rim + 20, 120), corner + new Vector3(-220, Rim + 120, 260), 30, HrBrass);
        return m.ToBody(header);
    }

    // The towel rail: two brass brackets off the wall (-z), the bar, a folded towel over it in the house's teal
    public static Body BuildTowelRail(byte[] header)
    {
        var m = new CarMesh("the towel rail", new[] { -1 });
        m.Light = 0.8f;
        foreach (var x in new[] { -350f, 350f }) m.Strut(0, new Vector3(x, 1100, -20), new Vector3(x, 1100, 90), 30, HrBrass);
        m.Strut(0, new Vector3(-380, 1100, 90), new Vector3(380, 1100, 90), 28, HrBrass);
        m.Box(0, new Vector3(0, 900, 90), new Vector3(520, 420, 40), HrTeal);
        m.Box(0, new Vector3(0, 900, 112), new Vector3(520, 30, 6), HrTealDark);
        return m.ToBody(header);
    }

    // The bath mat: a flat teal rug with a darker border
    public static Body BuildBathMat(byte[] header)
    {
        var m = new CarMesh("the bath mat", new[] { -1 });
        m.Light = 0.75f;
        m.Box(0, new Vector3(0, 4, 0), new Vector3(900, 8, 560), HrTealDark, top: HrTealDark);
        m.Box(0, new Vector3(0, 9, 0), new Vector3(760, 4, 420), HrTeal, top: HrTeal);
        return m.ToBody(header);
    }

    // The kitchen's counter: six cells of cupboards in the house's wood, a light stone worktop over them, a white sink with its drainer and
    // a chrome tap near its west end, a row of tiles up the wall behind
    public static Body BuildKitchenCounter(byte[] header)
    {
        var m = new CarMesh("the kitchen's counter", new[] { -1 });
        m.Light = 0.8f;
        const float L = 1536, D = 300, High = 820;
        m.Box(0, new Vector3(0, High / 2, 0), new Vector3(2 * L, High, 2 * D), HrWoodDark);
        for (var k = 0; k < 6; k++)
        {
            var x = -L + 256 + k * 512;
            m.Box(0, new Vector3(x, 400, D + 6), new Vector3(440, 600, 12), HrWood);
            m.Box(0, new Vector3(x + 150, 600, D + 14), new Vector3(30, 60, 10), HrBrass);
        }
        m.Box(0, new Vector3(0, High + 25, 10), new Vector3(2 * L + 40, 50, 2 * D + 40), HrTile, top: HrTile + 1);
        // (the sink, the drainer's ribs, the tap)
        m.Box(0, new Vector3(-L + 700, High + 52, 0), new Vector3(520, 8, 380), HrPorcelain, top: HrPorcelainShade);
        m.Box(0, new Vector3(-L + 700, High + 54, 0), new Vector3(420, 4, 280), HrTeal + 4, top: HrTeal + 4);
        for (var k = 0; k < 6; k++) m.Box(0, new Vector3(-L + 1050 + k * 50, High + 54, 0), new Vector3(20, 6, 340), HrChrome);
        m.Strut(0, new Vector3(-L + 700, High + 50, -230), new Vector3(-L + 700, High + 280, -230), 26, HrChrome);
        m.Strut(0, new Vector3(-L + 700, High + 280, -230), new Vector3(-L + 700, High + 240, -100), 22, HrChrome);
        // (things on it: a bread board and loaf, two jars)
        m.Box(0, new Vector3(L - 500, High + 60, 0), new Vector3(360, 20, 240), HrWoodLight);
        m.Box(0, new Vector3(L - 500, High + 120, 0), new Vector3(240, 110, 140), 6 * 16 + 7, top: 6 * 16 + 9);
        foreach (var x in new[] { L - 180, L - 90 }) Cylinder(m, new Vector3(x, 0, -150), 45, High + 50, High + 220, 6, 3 * 16 + 12, 4 * 16 + 6);
        // (tiles up the wall)
        // (the house's teal: 3 * 16 + 13 came out dark red in Citadel's palette -- the story build's picture, 2026-11-16)
        m.Box(0, new Vector3(0, High + 330, -D + 8), new Vector3(2 * L, 560, 16), HrTeal - 1);
        return m.ToBody(header);
    }

    // The range: a black iron cooker, its oven door and brass rail, the hob's rings, a copper pan and a chrome kettle on them, its flue up the wall
    public static Body BuildKitchenRange(byte[] header)
    {
        var m = new CarMesh("the range", new[] { -1 });
        m.Light = 0.8f;
        const float W = 480, D = 320, High = 860;
        m.Box(0, new Vector3(0, High / 2, 0), new Vector3(2 * W, High, 2 * D), HrIron, top: HrIron + 1);
        m.Box(0, new Vector3(-180, 420, D + 8), new Vector3(400, 380, 16), HrIron + 2);
        m.Strut(0, new Vector3(-W + 40, 680, D + 50), new Vector3(W - 40, 680, D + 50), 22, HrBrass);
        foreach (var (x, z) in new[] { (-230f, -120f), (230f, -120f), (-230f, 150f), (230f, 150f) }) Cylinder(m, new Vector3(x, 0, z), 110, High, High + 12, 8, HrIron + 4);
        Cylinder(m, new Vector3(-230, 0, 150), 140, High + 12, High + 150, 8, HrCopper, HrCopper - 2);
        m.Strut(0, new Vector3(-370, High + 140, 150), new Vector3(-620, High + 150, 150), 30, HrWoodDark);
        Cylinder(m, new Vector3(230, 0, -120), 120, High + 12, High + 230, 8, HrChrome, HrChrome + 2);
        m.Strut(0, new Vector3(350, High + 160, -120), new Vector3(450, High + 260, -120), 24, HrChrome);
        m.Box(0, new Vector3(0, 1600, -D + 120), new Vector3(240, 1500, 240), HrIron);
        return m.ToBody(header);
    }

    // The dresser: tall, in the house's wood, its top half glass-fronted with plates and cups behind, its bottom cupboards, an arch over it
    public static Body BuildDresser(byte[] header)
    {
        var m = new CarMesh("the dresser", new[] { -1 });
        m.Light = 0.8f;
        const float W = 480, D = 230;
        m.Box(0, new Vector3(0, 420, 0), new Vector3(2 * W, 840, 2 * D), HrWoodDark, top: HrWood);
        foreach (var x in new[] { -240f, 240f }) m.Box(0, new Vector3(x, 420, D + 6), new Vector3(420, 700, 12), HrWood);
        m.Box(0, new Vector3(0, 1500, -60), new Vector3(2 * W - 60, 1280, 2 * D - 120), HrWoodDark, top: HrWood);
        foreach (var y in new[] { 1180f, 1500f, 1820f })
        {
            m.Box(0, new Vector3(0, y, 20), new Vector3(2 * W - 120, 20, 200), HrWoodLight);
            for (var k = -3; k <= 3; k += 2) m.Box(0, new Vector3(k * 110, y + 110, 10), new Vector3(180, 200, 16), y < 1400 ? 12 * 16 + 9 : HrPorcelain);
        }
        m.Pane(0, HrTeal + 9, new Vector3(-W + 40, 900, D - 60), new Vector3(W - 40, 900, D - 60), new Vector3(W - 40, 2100, D - 60), new Vector3(-W + 40, 2100, D - 60));
        m.Box(0, new Vector3(0, 2190, 0), new Vector3(2 * W + 40, 100, 2 * D), HrWood, top: HrBrass);
        return m.ToBody(header);
    }

    // The kitchen's table: round, the house's wood, a white cloth runner, two plates and a jug
    public static Body BuildKitchenTable(byte[] header)
    {
        var m = new CarMesh("the kitchen's table", new[] { -1 });
        m.Light = 0.8f;
        Cylinder(m, Vector3.Zero, 520, 640, 700, 14, HrWoodDark, HrWood);
        Cylinder(m, Vector3.Zero, 70, 0, 640, 6, HrWoodDark);
        m.Box(0, new Vector3(0, 30, 0), new Vector3(560, 60, 120), HrWoodDark);
        m.Box(0, new Vector3(0, 30, 0), new Vector3(120, 60, 560), HrWoodDark);
        m.Box(0, new Vector3(0, 703, 0), new Vector3(900, 4, 260), HrPorcelain, top: HrPorcelain);
        foreach (var x in new[] { -260f, 260f }) Cylinder(m, new Vector3(x, 0, 0), 110, 705, 720, 8, HrPorcelain, HrPorcelainShade);
        Cylinder(m, new Vector3(0, 0, 0), 70, 705, 900, 8, HrTeal, HrTealDark);
        return m.ToBody(header);
    }

    // The plate shelf on the west wall (facing +z of its own: turned to face the room): two shelves, plates stood up along them
    public static Body BuildPlateShelf(byte[] header)
    {
        var m = new CarMesh("the plate shelf", new[] { -1 });
        m.Light = 0.8f;
        foreach (var y in new[] { 1200f, 1550f })
        {
            m.Box(0, new Vector3(0, y, 90), new Vector3(1000, 30, 180), HrWood);
            for (var k = -2; k <= 2; k++) Cylinder(m, new Vector3(k * 180, 0, 60), 85, y + 15, y + 30, 8, k % 2 == 0 ? HrPorcelain : HrTeal, k % 2 == 0 ? HrPorcelainShade : HrTealDark);
            for (var k = -2; k <= 2; k++) m.Box(0, new Vector3(k * 180, y + 110, 40), new Vector3(170, 170, 12), k % 2 == 0 ? 12 * 16 + 9 : HrPorcelain);
        }
        foreach (var x in new[] { -480f, 480f }) m.Box(0, new Vector3(x, 1380, 90), new Vector3(30, 420, 180), HrWoodDark);
        return m.ToBody(header);
    }
}
