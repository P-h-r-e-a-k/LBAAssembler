using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The fittings of Twinsen's new bathroom and kitchen (CitadelHouseRooms; the user, 2026-11-16: "a bathroom area, with toilet, sink, shower,
// and a large corner bath" and "a kitchen area" -- LBA1's house as the inspiration, its kitchen's long counter of cupboards with the sink and
// the hob and a tall glass-fronted cabinet at the end, its bath on a rug; the look the LBA2 house's: its brown woods, brass, teal): each built
// round its own place on the floor, its back to -z (a wall) unless it says otherwise. The rooms' cells: floor x 5-14, z 1-8; the north wall's
// face at z 512, the west wall's at x 2,560.
// Round 2 (the user, 2026-11-16 afternoon: "The shower, sink and toilet all look terrible and need improving", "The bathmat in the bathroom is
// too small and can't be walked on", "Let's add taps to the bath so using action on the taps causes the bath to fill with water. We also need
// to improve the bath, it's gold on two sides and not the third", "A table and two chairs... chairs would typically be either side of the
// table and useable"):
//   - the colours kept low enough in their ramps that the light's steps never run them into the next ramp (the porcelain and chrome were the
//     grey ramp's top, 3 * 16 + 14: lit, they ran into the reds -- the sink and toilet came out dark red);
//   - the shower, sink and toilet rounded (rings skinned, the bowls hollow), slimmer chrome;
//   - the corner bath one brass rim all round (the north strip, the curve, the east strip), its basin porcelain, brass taps at its west end;
//     filled by stages, bodies of its own (BathWaterLow/Mid/Full: the same bath, its water at three heights) the taps' Action swaps in;
//   - the bath mat bigger, and walked over: its box under the floor (BathMatBox, as the rugs' tiles);
//   - the kitchen's chairs (KitchenChair): the bedroom's chair a seat's height taller, for Twinsen's sitting (CitadelSpanking.TwinsenSit,
//     made at the Tralu's bench), set either side of the table facing it.
// Round 3 (the user, 2026-11-16 evening): "The bathmat is still too small let's have a large mat taking up most of the centre of the
// bathroom. Let's also add a pedestal mat around the toilet. The colouring of the toilet seat looks like it's supposed to be brown but has
// some white patches"; "This point blocks Twinsen from walking forwards, is the bath in the way even though it visibly isn't";
// "The shower still looks not great either, on one corner it doesn't look like the glass goes up to the wall. Let's change it so that
// there's a hinged door to the shower that's slightly ajar ... a door that swings when Twinsen connects to it"; "The shelf and lamp that
// we've added on the wall stop Twinsen from walking ... Twinsen is also kept a block away from being able to walk to the sink":
//   - every fitting a box of its own (HouseRoomBox): the engine gives a body with none a square round its place, half the width plus
//     the depth over four (FICHE.CPP InitBodyZV) -- the counter's reached 600 past its front, the bath's 1,000 past its curve; the wall's
//     shelf, towel rail and the mats' boxes under the floor (walked by, walked over);
//   - the shower in its corner against both walls (its glass to the wall), its door its own (ShowerDoor: hinged at the west wall, ajar;
//     it swings open as Twinsen comes up to it and back as he goes -- CitadelHouseRooms);
//   - the bath mat over most of the floor's middle; a pedestal mat round the toilet's foot;
//   - the toilet's seat over the bowl's rim, no porcelain under it (the rim's top, sorted over the seat, came through it white).
internal static partial class CitadelProps
{
    public const int Shower = 140, BathSink = 141, Toilet = 142, CornerBath = 143, TowelRail = 144, BathMat = 145;
    public const int KitchenCounter = 146, KitchenRange = 147, Dresser = 148, KitchenTable = 149, PlateShelf = 150;
    public const int KitchenChair = 151, BathWaterLow = 152, BathWaterMid = 153, BathWaterFull = 154, ShowerDoor = 155;
    // (each colour this far or more from its ramp's top: the light runs a colour up to 0.8 x 9 steps)
    private const int HrPorcelain = 3 * 16 + 8, HrPorcelainShade = 3 * 16 + 5, HrChrome = 3 * 16 + 7, HrWood = 1 * 16 + 5, HrWoodDark = 1 * 16 + 3,
        HrWoodLight = 1 * 16 + 8, HrBrass = 6 * 16 + 8, HrTeal = 10 * 16 + 6, HrTealDark = 10 * 16 + 3, HrTile = 3 * 16 + 7, HrIron = 3 * 16 + 2,
        HrCopper = 5 * 16 + 7, HrWater = 10 * 16 + 7, HrMirror = 11 * 16 + 8, HrHot = 4 * 16 + 8, HrCold = 11 * 16 + 6;
    private const int HrNorthFace = 512, HrWestFace = 2560;
    // the kitchen chairs' seat (its top over the floor): Twinsen's sitting drops his hips TwinsenSitDrop -- made for the Tralu's 512 bench
    public const int KitchenSeatTop = 500;
    // the bath's corner (its place's offset: the room's north-east corner), its sizes, its taps (from its place)
    public static readonly Vector3 BathCorner = new(760, 0, -760);
    private const float BathOuter = 1450, BathInner = 1250, BathStrip = 200, BathRim = 520, BathBottom = 140;
    public static readonly Vector3 BathTaps = new(-340, 520, -660);
    // the shower's half width (its tray from its place), its door's width, its glass's height; the door's hinge (at the west wall, the
    // front's glass line) and its turns: the closed door's facing, ajar (a little open) and swung open (ShowerSwing more), its turning speed
    private const float ShowerHalf = 480, ShowerTray = 70, ShowerHigh = 1900;
    public const float ShowerDoorWide = 300;
    public const int ShowerX = HrWestFace + (int)ShowerHalf, ShowerZ = HrNorthFace + (int)ShowerHalf;
    public const int ShowerHingeX = HrWestFace + 8, ShowerHingeZ = ShowerZ + (int)ShowerHalf - 8;
    public const int ShowerDoorShut = 0, ShowerDoorAjar = 4096 - 200, ShowerDoorOpen = 4096 - 900, ShowerDoorTurn = 400;

    // (generic, x, z, facing) of each room's fittings
    public static readonly (int Generic, int X, int Z, int Beta)[] BathroomFittings =
    {
        (Shower, ShowerX, ShowerZ, 0),                       // the north-west corner, against both walls: cells 5-6, 1-2
        (ShowerDoor, ShowerHingeX, ShowerHingeZ, ShowerDoorAjar),  // its door, hinged at the west wall, ajar
        (BathSink, 8 * 512 + 256, HrNorthFace + 230, 0),        // against the north wall, its mirror over it
        (Toilet, 10 * 512 + 256, HrNorthFace + 310, 0),
        (CornerBath, 13 * 512 + 256, 2 * 512 + 256, 0),       // the north-east corner: cells 12-14, 1-3
        (TowelRail, HrWestFace + 30, 1950, 1024),             // on the west wall between the shower and the door, facing the room (+x)
        (BathMat, 5120, 2800, 0),                             // most of the floor's middle
    };
    // the kitchen's chairs either side of its table (its middle 5,888 / 3,072), facing it: each one's place behind its seat (ChairAhead)
    public static readonly (int X, int Z, int Beta, int SeatX)[] KitchenChairs =
    {
        (5888 - 800 - ChairAhead, 3072, 1024, 5888 - 800),
        (5888 + 800 + ChairAhead, 3072, 3072, 5888 + 800),
    };
    public static readonly (int Generic, int X, int Z, int Beta)[] KitchenFittings =
    {
        (KitchenCounter, 5 * 512 + 1536, HrNorthFace + 300, 0), // cells 5-10 along the north wall: cupboards, the worktop, the sink
        (KitchenRange, 11 * 512 + 512, HrNorthFace + 330, 0),          // cells 11-12: the range, its hob, a kettle and a pan
        (Dresser, 14 * 512, HrNorthFace + 260, 0),              // cells 13-14: the tall glass-fronted dresser
        (KitchenTable, 11 * 512 + 256, 5 * 512 + 512, 0),     // the round table
        (PlateShelf, HrWestFace + 40, 3 * 512, 1024),           // on the west wall: a shelf of plates
        (KitchenChair, KitchenChairs[0].X, KitchenChairs[0].Z, KitchenChairs[0].Beta),
        (KitchenChair, KitchenChairs[1].X, KitchenChairs[1].Z, KitchenChairs[1].Beta),
    };
    // the bath mat's size; each fitting's box (x0, y0, z0, x1, y1, z1 from its place): its own footprint, as high as it stands -- the mats,
    // the wall's shelf and towel rail and the shower's door under the floor (walked over, walked by, swung through)
    private const int MatHalfX = 1600, MatHalfZ = 900;
    private static short[]? BathMatBox(int generic) => generic switch
    {
        BathMat => new short[] { -MatHalfX, -16, -MatHalfZ, MatHalfX, 0, MatHalfZ },
        Shower => new short[] { -(short)ShowerHalf, 0, -(short)ShowerHalf, (short)ShowerHalf, 2000, (short)ShowerHalf },
        ShowerDoor => new short[] { 0, -16, -40, (short)ShowerDoorWide, 0, 40 },
        BathSink => new short[] { -310, 0, -230, 310, 1000, 250 },
        Toilet => new short[] { -250, 0, -320, 250, 950, 340 },
        // (the corner bath: a box square to the walls short of its curve -- 45 degrees round it the curve is 1,025 from the corner, the box's
        // corner 1,090; its two ends along the walls past the box are solid cells of the room's grid, CitadelHouseRooms)
        CornerBath or BathWaterLow or BathWaterMid or BathWaterFull => new short[] { (short)(BathCorner.X - 1090), 0, (short)BathCorner.Z, (short)BathCorner.X, 620, (short)(BathCorner.Z + 1090) },
        TowelRail => new short[] { -400, -16, -40, 400, 0, 140 },
        PlateShelf => new short[] { -520, -16, -20, 520, 0, 200 },
        KitchenCounter => new short[] { -1556, 0, -300, 1556, 1200, 360 },
        KitchenRange => new short[] { -480, 0, -320, 480, 1100, 330 },
        Dresser => new short[] { -500, 0, -230, 500, 2200, 240 },
        KitchenTable => new short[] { -470, 0, -470, 470, 720, 470 },
        _ => null,
    };

    // a cylinder's sides (n facets) between two heights, a top fan if asked
    private static void Cylinder(CarMesh m, Vector3 at, float r, float y0, float y1, int n, int colour, int top = -1)
    {
        var low = m.Loop(0, at + new Vector3(0, y0, 0), new Vector3(r, 0, 0), new Vector3(0, 0, r), n);
        var high = m.Loop(0, at + new Vector3(0, y1, 0), new Vector3(r, 0, 0), new Vector3(0, 0, r), n);
        m.Skin(low, high, _ => colour, at + new Vector3(0, (y0 + y1) / 2, 0));
        if (top >= 0) { var hub = m.P(0, at + new Vector3(0, y1, 0)); for (var k = 0; k < n; k++) m.Up(new[] { hub, high[k], high[(k + 1) % n] }, top); }
    }
    // an ellipse's ring at a height (rx along x, rz along z)
    private static int[] Ellipse(CarMesh m, Vector3 at, float y, float rx, float rz, int n) => m.Loop(0, at + new Vector3(0, y, 0), new Vector3(rx, 0, 0), new Vector3(0, 0, rz), n);
    // the faces between two rings, each turned toward the axis (a bowl's inside)
    private static void Inside(CarMesh m, int[] a, int[] b, int colour, Vector3 axis)
    {
        for (var k = 0; k < a.Length; k++)
        {
            var q = new[] { a[k], a[(k + 1) % a.Length], b[(k + 1) % b.Length], b[k] };
            var c = (m.At(a[k]) + m.At(b[k])) / 2;
            m.In(q, colour, new Vector3(axis.X, c.Y, axis.Z));
        }
    }
    // a flat ring's top between an outer and an inner ring
    private static void RingTop(CarMesh m, int[] outer, int[] inner, int colour)
    {
        for (var k = 0; k < outer.Length; k++) m.Up(new[] { outer[k], outer[(k + 1) % outer.Length], inner[(k + 1) % inner.Length], inner[k] }, colour);
    }
    private static void Fan(CarMesh m, Vector3 hub, int[] ring, int colour)
    {
        var h = m.P(0, hub);
        for (var k = 0; k < ring.Length; k++) m.Up(new[] { h, ring[k], ring[(k + 1) % ring.Length] }, colour);
    }

    // The shower: a white tray with its recess and drain, frameless glass on its open sides in a slim chrome frame (its door's gap at the
    // front's west end: ShowerDoor), a slim riser in the corner up to an arm and a round rain head, the mixer on the back wall, a corner
    // shelf. Against both walls (its place ShowerX, ShowerZ): the east glass from the north wall's face
    public static Body BuildShower(byte[] header)
    {
        var m = new CarMesh("the shower", new[] { -1 });
        m.Light = 0.8f;
        const float H = ShowerHalf, Tray = ShowerTray, High = ShowerHigh;
        m.Box(0, new Vector3(0, Tray / 2, 0), new Vector3(2 * H, Tray, 2 * H), HrPorcelain, top: HrPorcelain);
        m.Box(0, new Vector3(0, Tray + 1, 0), new Vector3(2 * H - 110, 2, 2 * H - 110), HrPorcelainShade, top: HrPorcelainShade);
        Cylinder(m, new Vector3(0, 0, 0), 38, Tray, Tray + 4, 10, HrChrome, HrIron);
        // (the glass: the front (+z) from the door's gap, the east side (+x) whole, wall to front)
        const float Door = -H + 8 + ShowerDoorWide;
        m.Pane(0, HrTeal + 8, new Vector3(Door, Tray, H - 8), new Vector3(H - 8, Tray, H - 8), new Vector3(H - 8, High, H - 8), new Vector3(Door, High, H - 8));
        m.Pane(0, HrTeal + 8, new Vector3(H - 8, Tray, -H), new Vector3(H - 8, Tray, H - 8), new Vector3(H - 8, High, H - 8), new Vector3(H - 8, High, -H));
        foreach (var (a, b) in new[] { (new Vector3(Door, Tray, H - 8), new Vector3(Door, High, H - 8)), (new Vector3(H - 8, Tray, H - 8), new Vector3(H - 8, High, H - 8)),
                                       (new Vector3(Door, High, H - 8), new Vector3(H - 8, High, H - 8)), (new Vector3(H - 8, High, H - 8), new Vector3(H - 8, High, -H)) })
            m.Strut(0, a, b, 14, HrChrome);
        // (the riser up the corner, its arm, the rain head)
        var riser = new Vector3(-H + 70, 0, -H + 70);
        Cylinder(m, riser, 15, 950, 2000, 8, HrChrome);
        m.Strut(0, riser + new Vector3(0, 1990, 0), riser + new Vector3(230, 1990, 230), 16, HrChrome);
        Cylinder(m, riser + new Vector3(250, 0, 250), 130, 1950, 1975, 14, HrChrome, HrChrome);
        // (the mixer on the back wall: its plate, its lever)
        m.Box(0, new Vector3(-H + 330, 1150, -H + 8), new Vector3(170, 170, 14), HrChrome);
        Cylinder(m, new Vector3(-H + 330, 0, -H + 30), 30, 1120, 1180, 8, HrChrome, HrChrome);
        m.Strut(0, new Vector3(-H + 330, 1180, -H + 40), new Vector3(-H + 400, 1220, -H + 70), 14, HrChrome);
        m.Ball(0, new Vector3(-H + 290, 1225, -H + 20), 10, HrHot);
        m.Ball(0, new Vector3(-H + 370, 1225, -H + 20), 10, HrCold);
        // (the corner shelf and a bottle on it)
        m.Up(new[] { m.P(0, new Vector3(-H + 8, 1350, -H + 8)), m.P(0, new Vector3(-H + 8, 1350, -H + 200)), m.P(0, new Vector3(-H + 200, 1350, -H + 8)) }, HrChrome);
        Cylinder(m, new Vector3(-H + 60, 0, -H + 60), 22, 1352, 1460, 6, HrTeal, HrTealDark);
        return m.ToBody(header);
    }

    // The shower's door: a glass pane in a slim chrome frame, from its hinge (its place) along +x, ShowerDoorWide; a chrome handle near its
    // free end, both sides. Turned about its hinge (ShowerDoorAjar, ShowerDoorOpen)
    public static Body BuildShowerDoor(byte[] header)
    {
        var m = new CarMesh("the shower's door", new[] { -1 });
        m.Light = 0.8f;
        const float W = ShowerDoorWide, Low = ShowerTray, High = ShowerHigh;
        m.Pane(0, HrTeal + 8, new Vector3(0, Low, 0), new Vector3(W, Low, 0), new Vector3(W, High, 0), new Vector3(0, High, 0));
        foreach (var (a, b) in new[] { (new Vector3(0, Low, 0), new Vector3(0, High, 0)), (new Vector3(W, Low, 0), new Vector3(W, High, 0)),
                                       (new Vector3(0, High, 0), new Vector3(W, High, 0)), (new Vector3(0, Low, 0), new Vector3(W, Low, 0)) })
            m.Strut(0, a, b, 14, HrChrome);
        foreach (var z in new[] { -14f, 14f }) m.Strut(0, new Vector3(W - 40, 1000, z), new Vector3(W - 40, 1250, z), 12, HrChrome);
        return m.ToBody(header);
    }

    // The sink: an oval basin, rounded, hollowed into a bowl, on a slim tapering pedestal; a chrome mixer at its back, its spout over the bowl;
    // over it on the wall a mirror in a brass frame, a little glass shelf under it, a mug and a toothbrush
    public static Body BuildBathSink(byte[] header)
    {
        var m = new CarMesh("the bathroom's sink", new[] { -1 });
        m.Light = 0.8f;
        const int N = 14;
        var o = Vector3.Zero;
        // (the pedestal, its foot a little wider)
        var foot = Ellipse(m, o, 0, 150, 125, N);
        var waist = Ellipse(m, o, 520, 95, 80, N);
        var neck = Ellipse(m, o, 620, 150, 115, N);
        m.Skin(foot, waist, _ => HrPorcelain, o + new Vector3(0, 260, 0));
        m.Skin(waist, neck, _ => HrPorcelain, o + new Vector3(0, 570, 0));
        // (the basin: its belly, its rim, its bowl)
        var belly = Ellipse(m, o, 700, 300, 215, N);
        var lip = Ellipse(m, o, 790, 310, 225, N);
        m.Skin(neck, belly, _ => HrPorcelain, o + new Vector3(0, 660, 0));
        m.Skin(belly, lip, _ => HrPorcelain, o + new Vector3(0, 745, 0));
        var rimIn = Ellipse(m, o + new Vector3(0, 0, 25), 790, 245, 165, N);
        RingTop(m, lip, rimIn, HrPorcelain);
        var bowl = Ellipse(m, o + new Vector3(0, 0, 30), 700, 150, 100, N);
        Inside(m, rimIn, bowl, HrPorcelainShade, o + new Vector3(0, 0, 25));
        Fan(m, o + new Vector3(0, 695, 30), bowl, HrPorcelainShade);
        Cylinder(m, o + new Vector3(0, 0, 30), 20, 696, 699, 8, HrChrome, HrIron);
        // (the mixer: its pillar at the back of the rim, the spout reaching over the bowl, the lever)
        Cylinder(m, o + new Vector3(0, 0, -170), 28, 790, 930, 10, HrChrome, HrChrome);
        m.Strut(0, o + new Vector3(0, 915, -165), o + new Vector3(0, 935, -60), 18, HrChrome);
        m.Strut(0, o + new Vector3(0, 935, -60), o + new Vector3(0, 890, -20), 16, HrChrome);
        m.Strut(0, o + new Vector3(0, 935, -175), o + new Vector3(0, 990, -210), 12, HrChrome);
        // (the mirror on the wall's face, its brass frame; the glass shelf under it; a mug, a toothbrush)
        const float Wall = -225;
        m.Box(0, new Vector3(0, 1400, Wall + 12), new Vector3(560, 600, 24), HrBrass);
        m.Box(0, new Vector3(0, 1400, Wall + 26), new Vector3(480, 520, 6), HrMirror, top: HrMirror);
        m.Box(0, new Vector3(0, 1060, Wall + 55), new Vector3(500, 14, 100), HrTeal + 4, top: HrTeal + 4);
        Cylinder(m, new Vector3(-160, 0, Wall + 55), 32, 1067, 1170, 8, HrTeal, HrTealDark);
        m.Strut(0, new Vector3(-150, 1100, Wall + 50), new Vector3(-130, 1230, Wall + 62), 8, HrHot);
        return m.ToBody(header);
    }

    // The toilet: a rounded bowl on its pedestal, hollow, the water in it; a wooden seat on the rim and its lid up against the cistern; the
    // cistern against the wall, its lid, the chrome flush button; the pipe between them
    public static Body BuildToilet(byte[] header)
    {
        var m = new CarMesh("the toilet", new[] { -1 });
        m.Light = 0.8f;
        const int N = 14;
        var o = new Vector3(0, 0, 70);
        var foot = Ellipse(m, o + new Vector3(0, 0, -30), 0, 150, 185, N);
        var waist = Ellipse(m, o + new Vector3(0, 0, -10), 230, 125, 160, N);
        var belly = Ellipse(m, o, 340, 205, 255, N);
        var lip = Ellipse(m, o, 400, 210, 262, N);
        m.Skin(foot, waist, _ => HrPorcelain, o + new Vector3(0, 115, -20));
        m.Skin(waist, belly, _ => HrPorcelain, o + new Vector3(0, 285, -5));
        m.Skin(belly, lip, _ => HrPorcelain, o + new Vector3(0, 370, 0));
        var rimIn = Ellipse(m, o + new Vector3(0, 0, 10), 400, 145, 190, N);
        // (no porcelain top on the rim: the seat covers it -- sorted over the seat, it came through it in white patches)
        var deep = Ellipse(m, o + new Vector3(0, 0, -10), 300, 85, 110, N);
        Inside(m, rimIn, deep, HrPorcelainShade, o + new Vector3(0, 0, 10));
        Fan(m, o + new Vector3(0, 300, -10), deep, HrWater);
        // (the seat: a wooden ring a little over the rim, its edge; the lid up, a flat oval against the cistern)
        var seatOut = Ellipse(m, o, 420, 215, 266, N);
        var seatIn = Ellipse(m, o + new Vector3(0, 0, 10), 420, 140, 185, N);
        var seatEdge = Ellipse(m, o, 402, 215, 266, N);
        RingTop(m, seatOut, seatIn, HrWood);
        m.Skin(seatEdge, seatOut, _ => HrWoodDark, o + new Vector3(0, 410, 0));
        var lid = m.Loop(0, new Vector3(0, 640, -175), new Vector3(205, 0, 0), new Vector3(0, 230, -25), N);
        var lidHub = m.P(0, new Vector3(0, 640, -175));
        for (var k = 0; k < N; k++) m.Both(new[] { lidHub, lid[k], lid[(k + 1) % N] }, HrWood);
        // (the cistern, its lid, the button; the pipe down to the bowl)
        m.Box(0, new Vector3(0, 640, -235), new Vector3(470, 460, 150), HrPorcelain, top: HrPorcelain);
        m.Box(0, new Vector3(0, 880, -235), new Vector3(490, 30, 170), HrPorcelain, top: HrPorcelainShade);
        Cylinder(m, new Vector3(0, 0, -235), 40, 895, 905, 10, HrChrome, HrChrome);
        m.Strut(0, new Vector3(0, 420, -200), new Vector3(0, 340, -110), 40, HrPorcelain);
        // (the pedestal mat: a rounded rug round the foot, its cut-out the foot's, open at the back to the wall -- a darker border)
        const int M = 20;
        var matAt = o + new Vector3(0, 0, 60);
        var border = Ellipse(m, matAt, 6, 420, 380, M);
        var fill = Ellipse(m, matAt, 9, 385, 345, M);
        var cut = Ellipse(m, o + new Vector3(0, 0, -30), 9, 180, 215, M);
        var cutLow = Ellipse(m, o + new Vector3(0, 0, -30), 6, 180, 215, M);
        for (var k = 0; k < M; k++)
        {
            // (the back's segments left out: the ones whose outer points lie behind the foot's middle, toward the wall)
            if (m.At(border[k]).Z < o.Z - 60 || m.At(border[(k + 1) % M]).Z < o.Z - 60) continue;
            m.Up(new[] { border[k], border[(k + 1) % M], fill[(k + 1) % M], fill[k] }, HrTealDark);
            m.Up(new[] { fill[k], fill[(k + 1) % M], cut[(k + 1) % M], cut[k] }, HrTeal);
            m.Out(new[] { cutLow[k], cutLow[(k + 1) % M], cut[(k + 1) % M], cut[k] }, HrTealDark, o + new Vector3(0, 8, -30));
        }
        return m.ToBody(header);
    }

    // The corner bath: big, its back two sides against the walls, its front a quarter round about the corner (BathCorner) in the house's wood;
    // a brass rim all round its top -- the north strip, the curve, the east strip -- its basin porcelain, inset BathStrip from both walls;
    // brass taps at its west end, their spout over the basin. `water`: how high it stands (0 empty), BuildCornerBathWater's three
    private static Body CornerBathBody(byte[] header, string name, float water)
    {
        var m = new CarMesh(name, new[] { -1 });
        m.Light = 0.8f;
        const int N = 16;
        var c = BathCorner;
        Vector3 Arc(float r, float y, float a) => c + new Vector3(-r * MathF.Cos(a), y, r * MathF.Sin(a));
        float a0 = MathF.Asin(BathStrip / BathInner), a1 = MathF.PI / 2 - a0;
        // the outline: outer -- the corner, its west end W on the north wall, the curve, its south end E on the east wall; inner -- the
        // basin's corner c', A0 on the north strip's edge, its curve, A1 on the east strip's edge
        var inset = c + new Vector3(-BathStrip, 0, BathStrip);
        Vector3 Inner(float y, int k) => Arc(BathInner, y, a0 + (a1 - a0) * k / N);
        Vector3 Outer(float y, int k) => Arc(BathOuter, y, MathF.PI / 2 * k / N);
        // (the front: the curve's panel, wood, a darker plinth)
        for (var k = 0; k < N; k++)
        {
            m.Out(new[] { m.P(0, Outer(60, k)), m.P(0, Outer(60, k + 1)), m.P(0, Outer(BathRim, k + 1)), m.P(0, Outer(BathRim, k)) }, HrWood, c + new Vector3(0, BathRim / 2, 0));
            m.Out(new[] { m.P(0, Outer(0, k)), m.P(0, Outer(0, k + 1)), m.P(0, Outer(60, k + 1)), m.P(0, Outer(60, k)) }, HrWoodDark, c + new Vector3(0, 30, 0));
        }
        // (its east side, seen over the room's low east edge: a darker wood -- facing the room's light, HrWood ran up its ramp into grey, the
        // user's "it has some grey on the edge", 2026-11-16 -- and the plinth along its foot as the front's)
        m.Out(new[] { m.P(0, c + new Vector3(0, 60, 0)), m.P(0, Outer(60, N)), m.P(0, Outer(BathRim, N)), m.P(0, c + new Vector3(0, BathRim, 0)) }, HrWoodDark, c + new Vector3(-400, BathRim / 2, 400));
        m.Out(new[] { m.P(0, c), m.P(0, Outer(0, N)), m.P(0, Outer(60, N)), m.P(0, c + new Vector3(0, 60, 0)) }, HrWoodDark - 1, c + new Vector3(-400, 30, 400));
        // (the rim: the north strip, the curve's band, the east strip -- one brass top)
        m.Up(new[] { m.P(0, c + new Vector3(0, BathRim, 0)), m.P(0, Outer(BathRim, 0)), m.P(0, Inner(BathRim, 0)), m.P(0, inset + new Vector3(0, BathRim, 0)) }, HrBrass);
        for (var k = 0; k < N; k++)
            m.Up(new[] { m.P(0, Outer(BathRim, k)), m.P(0, Outer(BathRim, k + 1)), m.P(0, Inner(BathRim, k + 1)), m.P(0, Inner(BathRim, k)) }, HrBrass);
        m.Up(new[] { m.P(0, Outer(BathRim, N)), m.P(0, c + new Vector3(0, BathRim, 0)), m.P(0, inset + new Vector3(0, BathRim, 0)), m.P(0, Inner(BathRim, N)) }, HrBrass);
        // (the basin: its walls inward, porcelain; its floor)
        var middle = c + new Vector3(-700, 0, 700);
        void Wall(Vector3 a, Vector3 b) => m.In(new[] { m.P(0, a + new Vector3(0, BathRim, 0)), m.P(0, b + new Vector3(0, BathRim, 0)), m.P(0, b + new Vector3(0, BathBottom, 0)), m.P(0, a + new Vector3(0, BathBottom, 0)) },
            HrPorcelain, new Vector3(middle.X, (BathRim + BathBottom) / 2, middle.Z));
        Wall(inset, Inner(0, 0));
        for (var k = 0; k < N; k++) Wall(Inner(0, k), Inner(0, k + 1));
        Wall(Inner(0, N), inset);
        void Surface(float y, int colour)
        {
            var hub = m.P(0, inset + new Vector3(0, y, 0));
            for (var k = 0; k < N; k++) m.Up(new[] { hub, m.P(0, Inner(y, k)), m.P(0, Inner(y, k + 1)) }, colour);
        }
        Surface(BathBottom, HrPorcelainShade);
        if (water > 0) Surface(water, HrWater);
        // (the taps at the west end: the mixer's pillar, its spout over the basin, the hot and the cold)
        var t = BathTaps;
        Cylinder(m, new Vector3(t.X, 0, t.Z), 45, BathRim, BathRim + 100, 10, HrBrass, HrBrass);
        m.Strut(0, t + new Vector3(0, 90, 0), t + new Vector3(0, 150, 120), 26, HrBrass);
        m.Strut(0, t + new Vector3(0, 150, 120), t + new Vector3(0, 110, 210), 22, HrBrass);
        foreach (var (dx, dot) in new[] { (-110f, HrHot), (110f, HrCold) })
        {
            Cylinder(m, new Vector3(t.X + dx, 0, t.Z), 22, BathRim, BathRim + 70, 8, HrBrass, HrBrass);
            m.Strut(0, t + new Vector3(dx - 40, 80, 0), t + new Vector3(dx + 40, 80, 0), 16, HrBrass);
            m.Ball(0, t + new Vector3(dx, 92, 0), 12, dot);
        }
        return m.ToBody(header);
    }
    public static Body BuildCornerBath(byte[] header) => CornerBathBody(header, "the corner bath", 0);
    public static Body BuildCornerBathLow(byte[] header) => CornerBathBody(header, "the corner bath, filling", BathBottom + 120);
    public static Body BuildCornerBathMid(byte[] header) => CornerBathBody(header, "the corner bath, half full", BathBottom + 230);
    public static Body BuildCornerBathFull(byte[] header) => CornerBathBody(header, "the corner bath, full", BathRim - 70);

    // The towel rail: two brass brackets off the wall (-z), the bar, a folded towel over it in the house's teal
    public static Body BuildTowelRail(byte[] header)
    {
        var m = new CarMesh("the towel rail", new[] { -1 });
        m.Light = 0.8f;
        foreach (var x in new[] { -350f, 350f }) m.Strut(0, new Vector3(x, 1100, -20), new Vector3(x, 1100, 90), 26, HrBrass);
        m.Strut(0, new Vector3(-380, 1100, 90), new Vector3(380, 1100, 90), 24, HrBrass);
        m.Box(0, new Vector3(0, 880, 90), new Vector3(520, 440, 40), HrTeal);
        m.Box(0, new Vector3(0, 860, 112), new Vector3(520, 30, 6), HrTealDark);
        m.Box(0, new Vector3(0, 700, 112), new Vector3(520, 30, 6), HrTealDark);
        return m.ToBody(header);
    }

    // The bath mat: a wide teal rug with a darker border and two stripes, flat on the floor (walked over: BathMatBox)
    public static Body BuildBathMat(byte[] header)
    {
        var m = new CarMesh("the bath mat", new[] { -1 });
        m.Light = 0.75f;
        m.Box(0, new Vector3(0, 4, 0), new Vector3(2 * MatHalfX, 8, 2 * MatHalfZ), HrTealDark, top: HrTealDark);
        m.Box(0, new Vector3(0, 9, 0), new Vector3(2 * MatHalfX - 160, 4, 2 * MatHalfZ - 160), HrTeal, top: HrTeal);
        foreach (var z in new[] { -MatHalfZ + 260f, MatHalfZ - 260f }) m.Box(0, new Vector3(0, 12, z), new Vector3(2 * MatHalfX - 160, 2, 60), HrTealDark, top: HrTealDark);
        foreach (var x in new[] { -MatHalfX + 260f, MatHalfX - 260f }) m.Box(0, new Vector3(x, 12, 0), new Vector3(60, 2, 2 * MatHalfZ - 160), HrTealDark, top: HrTealDark);
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
        m.Strut(0, new Vector3(-L + 700, High + 50, -230), new Vector3(-L + 700, High + 280, -230), 22, HrChrome);
        m.Strut(0, new Vector3(-L + 700, High + 280, -230), new Vector3(-L + 700, High + 240, -100), 18, HrChrome);
        // (things on it: a bread board and loaf, two jars)
        m.Box(0, new Vector3(L - 500, High + 60, 0), new Vector3(360, 20, 240), HrWoodLight);
        m.Box(0, new Vector3(L - 500, High + 120, 0), new Vector3(240, 110, 140), 6 * 16 + 6, top: 6 * 16 + 7);
        foreach (var x in new[] { L - 180, L - 90 }) Cylinder(m, new Vector3(x, 0, -150), 45, High + 50, High + 220, 6, 3 * 16 + 7, 4 * 16 + 6);
        // (tiles up the wall, the house's teal -- 3 * 16 + 13 came out dark red in Citadel's palette: the story build's picture, 2026-11-16)
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
        Cylinder(m, new Vector3(-230, 0, 150), 140, High + 12, High + 150, 10, HrCopper, HrCopper - 2);
        m.Strut(0, new Vector3(-370, High + 140, 150), new Vector3(-620, High + 150, 150), 30, HrWoodDark);
        Cylinder(m, new Vector3(230, 0, -120), 120, High + 12, High + 230, 10, HrChrome, HrChrome);
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
            for (var k = -3; k <= 3; k += 2) m.Box(0, new Vector3(k * 110, y + 110, 10), new Vector3(180, 200, 16), y < 1400 ? 12 * 16 + 7 : HrPorcelain);
        }
        m.Pane(0, HrTeal + 8, new Vector3(-W + 40, 900, D - 60), new Vector3(W - 40, 900, D - 60), new Vector3(W - 40, 2100, D - 60), new Vector3(-W + 40, 2100, D - 60));
        m.Box(0, new Vector3(0, 2190, 0), new Vector3(2 * W + 40, 100, 2 * D), HrWood, top: HrBrass);
        return m.ToBody(header);
    }

    // The kitchen's table: round, the house's wood, a white cloth runner, two plates and a jug
    public static Body BuildKitchenTable(byte[] header)
    {
        var m = new CarMesh("the kitchen's table", new[] { -1 });
        m.Light = 0.8f;
        Cylinder(m, Vector3.Zero, 520, 640, 700, 16, HrWoodDark, HrWood);
        Cylinder(m, Vector3.Zero, 70, 0, 640, 8, HrWoodDark);
        m.Box(0, new Vector3(0, 30, 0), new Vector3(560, 60, 120), HrWoodDark);
        m.Box(0, new Vector3(0, 30, 0), new Vector3(120, 60, 560), HrWoodDark);
        m.Box(0, new Vector3(0, 703, 0), new Vector3(900, 4, 260), HrPorcelain, top: HrPorcelain);
        foreach (var x in new[] { -280f, 280f }) Cylinder(m, new Vector3(x, 0, 0), 110, 705, 720, 10, HrPorcelain, HrPorcelainShade);
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
            for (var k = -2; k <= 2; k++) m.Box(0, new Vector3(k * 180, y + 110, 40), new Vector3(170, 170, 12), k % 2 == 0 ? 12 * 16 + 7 : HrPorcelain);
        }
        foreach (var x in new[] { -480f, 480f }) m.Box(0, new Vector3(x, 1380, 90), new Vector3(30, 420, 180), HrWoodDark);
        return m.ToBody(header);
    }

    // The kitchen's chair: the bedroom's (BuildChair), its seat KitchenSeatTop over the floor -- Twinsen sits on it
    public static Body BuildKitchenChair(byte[] header) => ChairBody(header, "the kitchen's chair", KitchenSeatTop);
}
