using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The secret chamber's furnishings (the user, 2026-11-08/09: "the hole in the wall has a sendell seal filling it ... let's restore this";
// "some hanging tapestries around the place"; "a rack with different paddles and whips hanging on it"; "soften it slightly with some
// pictures hanging on the wall"; "one of the chairs from the bedroom down there for Zoe to sit on"). Props of the static prop entity as
// the others (CitadelProps); the wall pieces built flat in the x-y plane, their faces to +z (an actor's beta turns them to face out of
// its wall), their backs against it.
internal static partial class CitadelProps
{
    // (19 and 20 the old plain rugs, gone: the picture rugs, CitadelProps.Rugs, in their place)
    public const int Seal = 11, TapestryRed = 12, TapestryBlue = 13, Picture = 14, Rack = 15, Chair = 16,
        Boulder = 17, Boulder2 = 18, CurtainLeft = 21, CurtainRight = 22;
    // (the palette: the gold ramp, a deep blue, a teal for the chair's cushion, the dark flat, the rock's slate)
    private const int Gold = 106, Blue = 197, Teal = 150, Dark = 49, Sky = 200, Leather = 50, Slate = 180;

    // A flat quad in the x-y plane at depth z, its face to +z (or both sides)
    private static void Panel(CarMesh m, float x0, float y0, float x1, float y1, float z, int colour, bool both = false, bool flat = false)
    {
        var q = new[] { m.P(0, new Vector3(x0, y0, z)), m.P(0, new Vector3(x1, y0, z)), m.P(0, new Vector3(x1, y1, z)), m.P(0, new Vector3(x0, y1, z)) };
        if (both) { m.Both(q, colour, flat ? 0 : -1); return; }
        m.Out(q, colour, new Vector3((x0 + x1) / 2, (y0 + y1) / 2, z - 10), flat);
    }

    // The Sendell seal over the hole in the hall's wall: a round bronze medallion, its raised rim, its inner disc a shade darker, and
    // Sendell's sign on it in dark lines -- an arc over a ball, as LBA1's seal has it
    public static Body BuildSeal(byte[] header)
    {
        var m = new CarMesh("the Sendell seal", new[] { -1 });
        m.Light = 0.7f;
        const int N = 20;
        const float R = 540, Inner = 410, Rim = 50;
        var back = m.Loop(0, new Vector3(0, 0, 0), new Vector3(R, 0, 0), new Vector3(0, R, 0), N);
        var front = m.Loop(0, new Vector3(0, 0, Rim), new Vector3(R, 0, 0), new Vector3(0, R, 0), N);
        var inner = m.Loop(0, new Vector3(0, 0, Rim), new Vector3(Inner, 0, 0), new Vector3(0, Inner, 0), N);
        var face = m.Loop(0, new Vector3(0, 0, Rim - 14), new Vector3(Inner, 0, 0), new Vector3(0, Inner, 0), N);
        var mid = m.P(0, new Vector3(0, 0, Rim - 14));
        for (var i = 0; i < N; i++)
        {
            var n = (i + 1) % N;
            m.Out(new[] { back[i], back[n], front[n], front[i] }, Rust, new Vector3(0, 0, Rim / 2));
            m.Out(new[] { front[i], front[n], inner[n], inner[i] }, Wood, new Vector3(0, 0, 0));
            m.Out(new[] { inner[i], inner[n], face[n], face[i] }, Rust, new Vector3(0, 0, Rim + 50));
            m.Out(new[] { face[i], face[n], mid }, Rust, new Vector3(0, 0, 0));
        }
        // Sendell's sign: an arc (a thick crescent) over a ball, in dark flat lines just proud of the face
        const float Z = Rim - 10;
        const int Arc = 9;
        for (var k = 0; k < Arc; k++)
        {
            float a0 = MathF.PI * (0.15f + 0.7f * k / Arc), a1 = MathF.PI * (0.15f + 0.7f * (k + 1) / Arc);
            Vector3 P(float a, float r) => new(r * MathF.Cos(a), 60 + r * MathF.Sin(a), Z);
            var q = new[] { m.P(0, P(a0, 300)), m.P(0, P(a1, 300)), m.P(0, P(a1, 225)), m.P(0, P(a0, 225)) };
            m.Out(q, Dark, new Vector3(0, 0, 0), unlit: true);
        }
        var ball = m.Loop(0, new Vector3(0, -60, Z), new Vector3(105, 0, 0), new Vector3(0, 105, 0), 10);
        m.Cap(ball, m.P(0, new Vector3(0, -60, Z + 4)), Dark, new Vector3(0, -60, 0), unlit: true);
        return m.ToBody(header);
    }

    // A hanging tapestry: a cloth in its colour with a gold border and a gold lozenge in its middle, on a rod with gold ends; its top at 0
    private static Body Tapestry(byte[] header, int cloth, string name)
    {
        var m = new CarMesh(name, new[] { -1 });
        m.Light = 0.7f;
        const float W = 340, H = 1100, B = 40;
        Panel(m, -W, -H, W, 0, 0, cloth, both: true);
        Panel(m, -W, -H, W, -H + B, 4, Gold); Panel(m, -W, -B, W, 0, 4, Gold);
        Panel(m, -W, -H, -W + B, 0, 4, Gold); Panel(m, W - B, -H, W, 0, 4, Gold);
        // (the lozenge)
        var c = new Vector3(0, -H / 2, 6);
        var q = new[] { m.P(0, c + new Vector3(0, 220, 0)), m.P(0, c + new Vector3(160, 0, 0)), m.P(0, c + new Vector3(0, -220, 0)), m.P(0, c + new Vector3(-160, 0, 0)) };
        m.Out(q, Gold, c - new Vector3(0, 0, 10));
        m.Box(0, new Vector3(0, 10, 10), new Vector3(2 * W + 120, 24, 24), Wood);
        foreach (var s in new[] { -1, 1 }) m.Box(0, new Vector3(s * (W + 60), 10, 10), new Vector3(36, 36, 36), Gold);
        return m.ToBody(header);
    }
    public static Body BuildTapestryRed(byte[] header) => Tapestry(header, Red, "a red tapestry");
    public static Body BuildTapestryBlue(byte[] header) => Tapestry(header, Blue, "a blue tapestry");

    // A framed picture: a gold frame round a little landscape -- sky, green hills, a sun; its middle at 0
    public static Body BuildPicture(byte[] header)
    {
        var m = new CarMesh("a picture", new[] { -1 });
        m.Light = 0.7f;
        const float W = 300, H = 220, F = 36;
        m.Box(0, new Vector3(0, H - F / 2, 10), new Vector3(2 * W, F, 20), Gold);
        m.Box(0, new Vector3(0, -H + F / 2, 10), new Vector3(2 * W, F, 20), Gold);
        m.Box(0, new Vector3(-W + F / 2, 0, 10), new Vector3(F, 2 * H, 20), Gold);
        m.Box(0, new Vector3(W - F / 2, 0, 10), new Vector3(F, 2 * H, 20), Gold);
        Panel(m, -W + F, -10, W - F, H - F, 6, Sky);
        Panel(m, -W + F, -H + F, W - F, -10, 6, Green);
        var sun = m.Loop(0, new Vector3(110, 100, 8), new Vector3(40, 0, 0), new Vector3(0, 40, 0), 8);
        m.Cap(sun, m.P(0, new Vector3(110, 100, 10)), FlameCore, new Vector3(110, 100, 0), unlit: true);
        return m.ToBody(header);
    }

    // The rack of paddles and whips: a wooden board with pegs, and hanging from them two paddles (a wooden one, a black leather one), a whip
    // coiled in a loop and a riding crop -- free-standing (the user, 2026-11-09: "Let's make the stand with the spanking implements on it
    // free standing so they can be near the chair"): the board on two uprights with splayed feet, its foot at 0, its front +z
    public static Body BuildRack(byte[] header)
    {
        var m = new CarMesh("a stand of paddles and whips", new[] { -1 });
        m.Light = 0.7f;
        const float W = 520, Lift = 1250;
        // the stand: two uprights to the board's ends and a little over, each on a foot running front to back, a rail low between them
        foreach (var s in new[] { -1, 1 })
        {
            m.Box(0, new Vector3(s * (W + 40), (Lift + 380) / 2, 20), new Vector3(50, Lift + 380, 50), Wood);
            m.Box(0, new Vector3(s * (W + 40), 25, 20), new Vector3(70, 50, 420), Wood);
            m.Box(0, new Vector3(s * (W + 40), Lift + 395, 20), new Vector3(70, 30, 70), Gold);
        }
        m.Box(0, new Vector3(0, 160, 20), new Vector3(2 * W + 80, 40, 36), Wood);
        // the board and its pegs
        m.Box(0, new Vector3(0, Lift + 300, 20), new Vector3(2 * W + 40, 90, 40), Wood);
        var pegs = new[] { -390f, -130f, 130f, 390f };
        foreach (var x in pegs) m.Box(0, new Vector3(x, Lift + 300, 70), new Vector3(24, 24, 70), Steel);
        // a paddle: its handle up to the peg, its broad blade below
        void Paddle(float x, int colour)
        {
            m.Box(0, new Vector3(x, Lift + 180, 60), new Vector3(40, 220, 18), colour);
            m.Box(0, new Vector3(x, Lift - 60, 60), new Vector3(150, 280, 18), colour);
        }
        Paddle(pegs[0], Wood);
        Paddle(pegs[1], Leather);
        // a whip: its handle, then its lash hanging in a long loop of short black lengths
        m.Box(0, new Vector3(pegs[2], Lift + 170, 60), new Vector3(36, 240, 36), Dark);
        var prev = new Vector3(pegs[2], Lift + 50, 60);
        for (var k = 1; k <= 10; k++)
        {
            var a = k / 10f * MathF.PI;
            var next = new Vector3(pegs[2] + 90 * MathF.Sin(a), Lift + 50 - 420 * MathF.Sin(a / 2), 60);
            m.Box(0, (prev + next) / 2, new Vector3(18, Vector3.Distance(prev, next) + 6, 18), Dark);
            prev = next;
        }
        // a riding crop: a thin rod and its leather flap
        m.Box(0, new Vector3(pegs[3], Lift + 40, 60), new Vector3(16, 520, 16), Dark);
        m.Box(0, new Vector3(pegs[3], Lift - 240, 60), new Vector3(60, 70, 10), Leather);
        return m.ToBody(header);
    }

    // The bedroom's chair (the house's own, as LBA2 draws it in Twinsen's bedroom -- the user, 2026-11-09: "The chair prop looks nothing like
    // the one I asked you to copy"): a ladder-back of mid-brown wood -- square legs, a deep apron round the seat, a thick teal cushion
    // over the whole seat, two square back posts leaning a little back with three broad slats across them and a top rail -- its seat's
    // top at CitadelSpanking.StoolTop (Zoe sits on it) and its seat's middle ChairAhead in front of its own place (+z): the actor stands
    // back behind it, so its box -- the engine's sort -- keeps it behind Zoe, who sits on it
    public const int ChairAhead = 500;
    public static Body BuildChair(byte[] header)
    {
        var m = new CarMesh("the bedroom's chair", new[] { -1 });
        m.Light = 0.7f;
        const float Top = CitadelSpanking.StoolTop, Half = 200, Cushion = 70, Apron = 80, Leg = 46, Z = ChairAhead;
        const float SeatUnder = Top - Cushion;
        // the cushion, its top a shade lighter, and the apron under it all round
        m.Box(0, new Vector3(0, Top - Cushion / 2, Z + 8), new Vector3(2 * Half - 10, Cushion, 2 * Half - 26), Teal, Teal + 1);
        m.Box(0, new Vector3(0, SeatUnder - Apron / 2, Z + Half - 12), new Vector3(2 * Half, Apron, 24), Wood);
        m.Box(0, new Vector3(0, SeatUnder - Apron / 2, Z - Half + 12), new Vector3(2 * Half, Apron, 24), Wood);
        foreach (var s in new[] { -1, 1 })
            m.Box(0, new Vector3(s * (Half - 12), SeatUnder - Apron / 2, Z), new Vector3(24, Apron, 2 * Half), Wood);
        // the front legs, and the low stretchers between all four
        foreach (var s in new[] { -1, 1 })
            m.Box(0, new Vector3(s * (Half - Leg / 2), SeatUnder / 2, Z + Half - Leg / 2), new Vector3(Leg, SeatUnder, Leg), Wood);
        foreach (var s in new[] { -1, 1 })
            m.Box(0, new Vector3(s * (Half - Leg / 2), 70, Z), new Vector3(20, 22, 2 * Half - Leg), Wood);
        m.Box(0, new Vector3(0, 90, Z + Half - Leg / 2), new Vector3(2 * Half - Leg, 22, 20), Wood);
        // the back: its posts from the floor up past the seat, leaning back as they rise; the slats and the top rail across them
        const float PostTop = Top + 560, Lean = 90;
        float Back(float y) => Z - Half + Leg / 2 - Lean * MathF.Max(0, y - SeatUnder) / (PostTop - SeatUnder);
        foreach (var s in new[] { -1, 1 })
        {
            var x = s * (Half - Leg / 2);
            m.Strut(0, new Vector3(x, 0, Z - Half + Leg / 2), new Vector3(x, SeatUnder, Back(SeatUnder)), Leg, Wood);
            m.Strut(0, new Vector3(x, SeatUnder, Back(SeatUnder)), new Vector3(x, PostTop, Back(PostTop)), Leg, Wood);
        }
        foreach (var y in new[] { Top + 150, Top + 290, Top + 430 })
            m.Box(0, new Vector3(0, y, Back(y)), new Vector3(2 * Half - Leg, 70, 22), Wood);
        m.Box(0, new Vector3(0, PostTop - 30, Back(PostTop - 30)), new Vector3(2 * Half + 10, 60, 34), Wood);
        return m.ToBody(header);
    }

    // A bicycle, parked on its stand and leaning a little onto it: two wheels -- black tyres, a hub and its spokes -- a diamond frame in
    // `colour`, the saddle, the handlebars and their grips, the pedals; with a basket on the front for Zoe's. Along z (its front +z), its
    // wheels on the floor at 0
    public const int BikeTwinsen = 32, BikeZoe = 33;
    private static Body BuildBike(byte[] header, string name, int colour, bool basket)
    {
        var m = new CarMesh(name, new[] { -1 });
        m.Light = 0.75f;
        const float R = 270, Base = 520, Tilt = 0.12f;
        // (the lean: x moves over as it rises)
        Vector3 L(float x, float y, float z) => new(x - y * Tilt, y, z);
        int P(float x, float y, float z) => m.P(0, L(x, y, z));
        void Bar(Vector3 a, Vector3 b, float t, int c) => m.Strut(0, L(a.X, a.Y, a.Z), L(b.X, b.Y, b.Z), t, c);
        foreach (var hz in new[] { -Base, Base })
        {
            // the tyre: a ring of short dark lengths round the hub, the spokes as lines
            const int N = 14;
            for (var k = 0; k < N; k++)
            {
                float a0 = k * MathF.Tau / N, a1 = (k + 1) * MathF.Tau / N;
                Bar(new Vector3(0, R + R * MathF.Sin(a0), hz + R * MathF.Cos(a0)), new Vector3(0, R + R * MathF.Sin(a1), hz + R * MathF.Cos(a1)), 34, Dark);
            }
            var hub = P(0, R, hz);
            for (var k = 0; k < 8; k++) m.Line(hub, P(0, R + (R - 20) * MathF.Sin(k * MathF.Tau / 8), hz + (R - 20) * MathF.Cos(k * MathF.Tau / 8)), Steel);
        }
        Vector3 bracket = new(0, R + 10, -30), seat = new(0, 760, -170), head = new(0, 770, 400), headLow = new(0, 640, 420);
        Vector3 rear = new(0, R, -Base), front = new(0, R, Base);
        Bar(bracket, seat, 34, colour);
        Bar(seat, head, 32, colour);
        Bar(bracket, headLow, 36, colour);
        Bar(bracket, rear, 24, colour);
        Bar(seat, rear, 22, colour);
        Bar(headLow, front, 26, colour);
        Bar(head, headLow, 34, colour);
        // the saddle, the stem and the handlebars with their grips
        Bar(seat, seat + new Vector3(0, 70, 0), 22, Steel);
        m.Box(0, L(0, 845, -175), new Vector3(110, 40, 230), Dark);
        Bar(head, head + new Vector3(0, 110, -30), 22, Steel);
        Bar(new Vector3(-230, 880, 370), new Vector3(230, 880, 370), 22, Steel);
        foreach (var s in new[] { -1, 1 }) Bar(new Vector3(s * 230, 880, 370), new Vector3(s * 290, 880, 370), 30, Dark);
        // the pedals, the stand
        Bar(new Vector3(-90, R - 60, -30), new Vector3(90, R + 80, -30), 20, Steel);
        foreach (var (x, y) in new[] { (-100f, R - 60f), (100f, R + 80f) }) m.Box(0, L(x, y, -30), new Vector3(70, 18, 90), Dark);
        m.Strut(0, L(-20, R, -120), new Vector3(-160, 0, -200), 18, Steel);
        if (basket)
        {
            // a wicker basket on the front, over the wheel
            foreach (var (x, z) in new[] { (-140f, 470f), (140f, 470f), (-140f, 700f), (140f, 700f) }) Bar(new Vector3(x, 620, z), new Vector3(x, 820, z), 16, Wood);
            m.Box(0, L(0, 620, 585), new Vector3(300, 20, 250), Wood);
            Bar(new Vector3(-140, 820, 470), new Vector3(140, 820, 470), 16, Wood);
            Bar(new Vector3(-140, 820, 700), new Vector3(140, 820, 700), 16, Wood);
            Bar(new Vector3(-140, 820, 470), new Vector3(-140, 820, 700), 16, Wood);
            Bar(new Vector3(140, 820, 470), new Vector3(140, 820, 700), 16, Wood);
        }
        return m.ToBody(header);
    }
    public static Body BuildBikeTwinsen(byte[] header) => BuildBike(header, "Twinsen's bike", Blue, false);
    public static Body BuildBikeZoe(byte[] header) => BuildBike(header, "Zoe's bike", Red, true);

    // ---- round 2 (the user, 2026-11-09): "let's add the stairs back, but add in some giant bolders to show it's not useable"; "let's add
    // some rugs on the floors, some curtains around the edges ... it's now part of Twinsen's home so it needs to feel homely"

    // A boulder: a lumpy rock, its flat underside at 0 -- rings of points round it, each pushed in or out a little (a seeded roll, the same
    // every build), skinned ring to ring
    private static Body BuildRock(byte[] header, string name, float radius, int seed)
    {
        var m = new CarMesh(name, new[] { -1 });
        m.Light = 0.75f;
        var roll = new Random(seed);
        const int Around = 9, Rings = 5;
        var rings = new List<int[]>();
        for (var k = 0; k < Rings; k++)
        {
            // from the underside (a flattened ring) up towards the crown
            var t = (k + 1f) / (Rings + 1) * MathF.PI;
            var r = radius * (k == 0 ? 0.75f : MathF.Sin(t));
            var y = k == 0 ? 0 : radius * 0.85f * (1 - MathF.Cos(t));
            var ring = new int[Around];
            for (var i = 0; i < Around; i++)
            {
                var a = (i + 0.5f * (k % 2)) / Around * 2 * MathF.PI;
                var s = 0.8f + 0.4f * (float)roll.NextDouble();
                var lift = k == 0 ? 0 : radius * 0.12f * ((float)roll.NextDouble() - 0.5f);
                ring[i] = m.P(0, new Vector3(r * s * MathF.Cos(a), y + lift, r * s * MathF.Sin(a)));
            }
            rings.Add(ring);
        }
        var middle = new Vector3(0, radius * 0.8f, 0);
        for (var k = 0; k + 1 < Rings; k++) m.Skin(rings[k], rings[k + 1], _ => Slate, middle);
        m.Cap(rings[Rings - 1], m.P(0, new Vector3(radius * 0.1f, radius * 1.65f, -radius * 0.05f)), Slate, middle);
        m.Cap(rings[0], m.P(0, new Vector3(0, 0, 0)), Slate, middle);
        return m.ToBody(header);
    }
    public static Body BuildBoulder(byte[] header) => BuildRock(header, "a boulder", 640, 1996);
    public static Body BuildBoulder2(byte[] header) => BuildRock(header, "a smaller boulder", 480, 1997);

    // A small boulder, for the scatter round the blocked way out (the user, 2026-11-09: "add some more small boulders by the blocked exit")
    public const int SmallBoulder = 61;
    public static Body BuildSmallBoulder(byte[] header) => BuildRock(header, "a small boulder", 260, 1998);

    // The lift (the user, 2026-11-09: "we also need to add in the elevator for Zoe and Twinsen to leave the basement area"): a platform of
    // dark planks in an iron frame, LiftSize square, its top LiftTop over its foot -- no chains ("the other in-game elevators don't have
    // chains so we just accept an elevator can just go up and down") -- and its rail on three sides, each side a body of its own with a box
    // of its own (CitadelProps.BoxOf), so the rail stops Twinsen as the platform carries him ("they have no colission, so we need to add
    // this"). All of them carriers riding the same track (RaceTrackStory.SecretChamber.LiftRig).
    public const int LiftPlatform = 62, LiftRailBack = 100, LiftRailLeft = 101, LiftRailRight = 102, LiftSize = 1024, LiftTop = 40, RailHigh = 460;
    public static Body BuildLiftPlatform(byte[] header)
    {
        var m = new CarMesh("the lift", new[] { -1 });
        m.Light = 0.7f;
        const float H = LiftSize / 2f, Rim = 40;
        for (var k = 0; k < 8; k++)
        {
            var z0 = -H + Rim + k * (LiftSize - 2 * Rim) / 8f;
            m.Box(0, new Vector3(0, LiftTop - 20, z0 + (LiftSize - 2 * Rim) / 16f), new Vector3(LiftSize - 2 * Rim, 40, (LiftSize - 2 * Rim) / 8f - 4), k % 2 == 0 ? Wood : Rust, k % 2 == 0 ? Wood : Rust);
        }
        foreach (var s in new[] { -1, 1 })
        {
            m.Box(0, new Vector3(s * (H - Rim / 2), LiftTop - 20, 0), new Vector3(Rim, 44, LiftSize), Steel);
            m.Box(0, new Vector3(0, LiftTop - 20, s * (H - Rim / 2)), new Vector3(LiftSize, 44, Rim), Steel);
        }
        return m.ToBody(header);
    }
    // a side of the rail: posts at its ends and middle, a bar along their tops -- the back (-z), the left (-x) or the right (+x)
    private static Body BuildLiftRail(byte[] header, int side)
    {
        var m = new CarMesh("the lift's rail", new[] { -1 });
        m.Light = 0.7f;
        const float H = LiftSize / 2f - 20;
        Vector3 At(float along) => side == 0 ? new Vector3(along, 0, -H) : new Vector3(side * H, 0, along);
        foreach (var along in new[] { -H, 0, H })
            m.Box(0, At(along) + new Vector3(0, LiftTop + RailHigh / 2f, 0), new Vector3(30, RailHigh, 30), Steel);
        var size = side == 0 ? new Vector3(2 * H + 30, 30, 30) : new Vector3(30, 30, 2 * H + 30);
        m.Box(0, At(0) + new Vector3(0, LiftTop + RailHigh, 0), size, Steel);
        m.Box(0, At(0) + new Vector3(0, LiftTop + RailHigh / 2f, 0), size with { Y = 20 }, Steel);
        return m.ToBody(header);
    }
    public static Body BuildLiftRailBack(byte[] header) => BuildLiftRail(header, 0);
    public static Body BuildLiftRailLeft(byte[] header) => BuildLiftRail(header, -1);
    public static Body BuildLiftRailRight(byte[] header) => BuildLiftRail(header, 1);
    // (the rail's sides' boxes: thin, as high as the rail)
    public static short[]? LiftBox(int generic) => generic switch
    {
        LiftPlatform => new short[] { -512, 0, -512, 511, LiftTop, 511 },
        LiftRailBack => new short[] { -512, 0, -512, 511, LiftTop + RailHigh + 20, -460 },
        LiftRailLeft => new short[] { -512, 0, -512, -460, LiftTop + RailHigh + 20, 511 },
        LiftRailRight => new short[] { 460, 0, -512, 511, LiftTop + RailHigh + 20, 511 },
        _ => null,
    };

    // A curtain hung on a wall, gathered to one side by a gold tie-back (`side` -1 its left, +1 its right): pleats of red cloth from its
    // pelmet (its top at 0) to the floor CurtainDrop under it, drawn in to the tie-back's side two-thirds of the way down. Built facing
    // +z, as the other wall pieces.
    public const int CurtainDrop = 2000;
    private static Body BuildCurtain(byte[] header, int side, string name)
    {
        var m = new CarMesh(name, new[] { -1 });
        m.Light = 0.7f;
        const float W = 300, Depth = 50;
        const int Pleats = 6;
        const float Tie = -CurtainDrop * 0.62f;
        // the rows of points: under the pelmet, at the tie-back, at the hem
        (float Y, float Left, float Right)[] rows =
        {
            (-90, -W, W),
            (Tie, side < 0 ? -W : W - 160, side < 0 ? -W + 160 : W),
            (-CurtainDrop, side < 0 ? -W : W - 380, side < 0 ? -W + 380 : W),
        };
        var points = rows.Select(r => Enumerable.Range(0, 2 * Pleats + 1).Select(i =>
            m.P(0, new Vector3(r.Left + (r.Right - r.Left) * i / (2 * Pleats), r.Y, i % 2 == 0 ? Depth : Depth * 0.2f))).ToArray()).ToArray();
        for (var k = 0; k + 1 < points.Length; k++)
            for (var i = 0; i < 2 * Pleats; i++)
            {
                // (each pleat facing out from the wall, +z: by its own normal -- the pleats' slant fools a test against a point behind)
                var q = new[] { points[k][i], points[k][i + 1], points[k + 1][i + 1], points[k + 1][i] };
                m.In(q, i % 2 == 0 ? Red : Red - 1, m.At(q[0]) + new Vector3(0, 0, 100000));
            }
        m.Box(0, new Vector3(0, -45, 50), new Vector3(2 * W + 80, 110, 70), Red - 1);
        m.Box(0, new Vector3(0, -95, 88), new Vector3(2 * W + 80, 18, 8), Gold);
        var tx = side < 0 ? -W + 80 : W - 80;
        m.Box(0, new Vector3(tx, Tie, 50), new Vector3(200, 50, 90), Gold);
        return m.ToBody(header);
    }
    public static Body BuildCurtainLeft(byte[] header) => BuildCurtain(header, -1, "a curtain tied to its left");
    public static Body BuildCurtainRight(byte[] header) => BuildCurtain(header, 1, "a curtain tied to its right");
}
