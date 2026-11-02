using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The Long Table's own cars (Terrain/Harvest) --
//
//   the pardoned turkey   a live turkey, fan spread, making its getaway on the silver platter it was meant to be served on, on four small wheels
//   the goose's canoe     a Canada goose paddling a red canoe on wheels, its black neck up, its white chinstrap
//
// Bodies of the racer's entity as Halloween's monsters are (RaceTrackCharacterCars.Monsters: made with the cars' kit, their drivers with them, gone into a game
// folder by InstallMonsters at the very end of a build, with their half-size copies), race cars TurkeyNumber and GooseNumber of RACECARS.JSON.
internal static partial class RaceTrackCharacterCars
{
    public const int TurkeyNumber = 226, GooseNumber = 227, TurkeyGeneric = 70, GooseGeneric = 71;
    public const string LongTable = "The Long Table";
    public static readonly Car TurkeyCar = new("The pardoned turkey's platter", "the pardoned turkey", TwinsenBody, TurkeyGeneric, (_, racer) => BuildTurkeyCar(racer), LongTable);
    public static readonly Car GooseCar = new("The goose's canoe", "a Canada goose", TwinsenBody, GooseGeneric, (_, racer) => BuildGooseCar(racer), LongTable);

    private const int HvBrown = 18, HvBrownDark = 16, HvBrownLight = 22, HvRed = 72, HvRedDark = 68, HvGold = 104, HvWhite = 62, HvBlack = 48, HvSilver = 58, HvSilverDark = 54, HvWood = 24;

    // The racer's arm bones (14-17), which the driving animations turn, for a driver without arms: a point each inside its body.
    private static void HvNoArms(CarMesh m, Vector3 at)
    {
        foreach (var (upper, fore) in new[] { (14, 15), (16, 17) })
        {
            m.Pivot(upper, m.P(13, at));
            m.Pivot(fore, m.P(upper, at));
            m.P(fore, at);
        }
    }

    public static Body BuildTurkeyCar(Body racer)
    {
        var m = new CarMesh(TurkeyCar.Name);
        Roots(m);
        // ---- the platter: an oval dish, its rim turned up
        const int N = 12;
        int[] Oval(float y, float rx, float rz) => m.Loop(2, new Vector3(0, y, 0), new Vector3(0, 0, rz), new Vector3(rx, 0, 0), N);
        m.Light = SoftLight;
        var under = Oval(190, 330, 560); var rim = Oval(262, 440, 730); var dish = Oval(236, 360, 640);
        m.Skin(under, rim, _ => HvSilver, new Vector3(0, 280, 0));
        m.Skin(rim, dish, _ => HvSilver + 2, new Vector3(0, 80, 0));
        m.Cap(dish, m.P(2, new Vector3(0, 232, 0)), HvSilverDark, new Vector3(0, 80, 0));
        m.Cap(under, m.P(2, new Vector3(0, 188, 0)), HvSilverDark, new Vector3(0, 300, 0));
        Wheels(m, new WheelLook(Dark, 22, Black, SoftLight, HvSilver, SoftLight, HvGold),
            new Axle(new Vector3(300, 220, 430), new Vector3(410, 130, 430), 130, 90),
            new Axle(new Vector3(300, 220, -430), new Vector3(425, 150, -430), 150, 100));

        // ---- the turkey, sitting up, head to the front
        m.Light = 1;
        m.Pivot(13, m.P(2, new Vector3(0, 380, -60)));
        m.Light = SoftLight;
        HmStack(m, 13, new[]
        {
            (new Vector3(0, 236, -70), 210f, 250f), (new Vector3(0, 340, -70), 310f, 370f), (new Vector3(0, 470, -60), 320f, 380f),
            (new Vector3(0, 590, -40), 250f, 300f), (new Vector3(0, 670, -10), 130f, 170f),
        }, 8, (band, k) => (band + k) % 2 == 0 ? HvBrown : HvBrownDark);
        foreach (var side in Sides)
            m.Plate(13, HvBrownLight, new Vector3(side * 330, 500, 90), new Vector3(side * 350, 490, -240), new Vector3(side * 300, 300, -300), new Vector3(side * 290, 330, 60));
        // its fan, spread behind it: feathers in rings of red, gold and brown, their tips pale
        const int Feathers = 9;
        for (var k = 0; k < Feathers; k++)
        {
            float a0 = -1.35f + k * 0.3f, a1 = a0 + 0.3f;
            Vector3 F(float a, float r) => new(r * MathF.Sin(a), 480 + r * MathF.Cos(a), -340 - r * 0.15f);
            var colour = k % 3 == 0 ? HvRed : k % 3 == 1 ? HvGold : HvBrownLight;
            m.Plate(13, colour, F(a0, 150), F(a1, 150), F(a1, 640), F(a0, 640));
            m.Plate(13, HvWhite, F(a0, 640), F(a1, 640), F(a1, 720), F(a0, 720));
        }
        // the neck, bare; the head, its beak, its red wattle, its eyes
        var neck = new[] { new Vector3(0, 600, 200), new Vector3(0, 760, 300), new Vector3(0, 900, 330) };
        HmLimb(m, 13, neck, new[] { 86f, 56f, 44f }, 6, _ => HvRedDark + 8);
        m.Ball(13, new Vector3(0, 950, 345), 74, HvRedDark + 8);
        m.Light = 1;
        m.Box(13, new Vector3(0, 945, 430), new Vector3(46, 34, 80), HvGold);
        m.Ball(13, new Vector3(0, 880, 400), 42, HvRed);
        m.Ball(13, new Vector3(30, 835, 400), 30, HvRed);
        foreach (var side in Sides) m.Ball(13, new Vector3(side * 58, 975, 385), 16, HvBlack);
        HvNoArms(m, new Vector3(0, 420, -60));
        return m.ToBody(racer.Header);
    }

    public static Body BuildGooseCar(Body racer)
    {
        var m = new CarMesh(GooseCar.Name);
        Roots(m);
        // ---- the canoe: rings along it, pointed at both ends, a red hull under a pale gunwale, its ends turned up
        m.Light = SoftLight;
        var stations = new (float Z, float Half, float Top)[] { (800, 12, 470), (600, 150, 400), (300, 300, 370), (0, 340, 360), (-300, 300, 370), (-600, 150, 400), (-800, 12, 470) };
        const int N = 8;
        int[] Hull((float Z, float Half, float Top) s) => Enumerable.Range(0, N + 1).Select(k =>
        {
            var a = MathF.PI * k / N;
            return m.P(2, new Vector3(s.Half * MathF.Cos(a), s.Top - (s.Top - 190) * MathF.Sin(a), s.Z));
        }).ToArray();
        var rings = stations.Select(Hull).ToArray();
        for (var i = 0; i + 1 < rings.Length; i++)
            for (var k = 0; k < N; k++)
                m.Out(new[] { rings[i][k], rings[i][k + 1], rings[i + 1][k + 1], rings[i + 1][k] }, HvRed, new Vector3(0, 420, (stations[i].Z + stations[i + 1].Z) / 2));
        // (the deck inside, dark wood, level with the gunwales' foot; the gunwales pale)
        for (var i = 0; i + 1 < rings.Length; i++)
        {
            m.Up(new[] { rings[i][0], rings[i][N], rings[i + 1][N], rings[i + 1][0] }, HvWood);
            m.Line(rings[i][0], rings[i + 1][0], HvWhite);
            m.Line(rings[i][N], rings[i + 1][N], HvWhite);
        }
        Wheels(m, new WheelLook(Dark, 22, Black, SoftLight, HvWhite, SoftLight, HvRed),
            new Axle(new Vector3(250, 240, 420), new Vector3(380, 130, 420), 130, 90),
            new Axle(new Vector3(250, 240, -420), new Vector3(390, 145, -420), 145, 100));

        // ---- the goose, sitting in it
        m.Light = 1;
        m.Pivot(13, m.P(2, new Vector3(0, 380, -60)));
        m.Light = SoftLight;
        HmStack(m, 13, new[]
        {
            (new Vector3(0, 370, -90), 170f, 300f), (new Vector3(0, 470, -90), 240f, 360f), (new Vector3(0, 570, -70), 210f, 320f), (new Vector3(0, 640, -40), 110f, 190f),
        }, 8, (band, k) => band == 0 ? HvWhite : HvBrown);
        foreach (var side in Sides)
            m.Plate(13, HvBrownDark, new Vector3(side * 240, 590, 120), new Vector3(side * 260, 540, -300), new Vector3(side * 200, 420, -440), new Vector3(side * 230, 420, 80));
        m.Plate(13, HvBlack, new Vector3(-90, 540, -380), new Vector3(90, 540, -380), new Vector3(40, 610, -560), new Vector3(-40, 610, -560));
        // the long black neck, the head, the white chinstrap, the bill
        HmLimb(m, 13, new[] { new Vector3(0, 590, 130), new Vector3(0, 800, 240), new Vector3(0, 1020, 280) }, new[] { 74f, 50f, 44f }, 6, _ => HvBlack + 2);
        m.Ball(13, new Vector3(0, 1070, 300), 68, HvBlack + 2);
        m.Light = 1;
        foreach (var side in Sides) m.Ball(13, new Vector3(side * 52, 1035, 300), 34, HvWhite);
        m.Box(13, new Vector3(0, 1062, 385), new Vector3(46, 30, 90), HvBlack);
        // the paddle, across the canoe
        m.Rod(13, new Vector3(-420, 640, 180), new Vector3(420, 500, 60), HvWood);
        m.Plate(13, HvWood, new Vector3(420, 500, 20), new Vector3(420, 500, 100), new Vector3(580, 300, 100), new Vector3(580, 300, 20));
        HvNoArms(m, new Vector3(0, 420, -60));
        return m.ToBody(racer.Header);
    }
}
