using System.Numerics;

namespace LBAAssembler.Terrain.Temple;

// The Long Table's moving things, flown by the race-track mode as the Temple of Bú's witches are (spook=, BODY.HQR): Canada geese in their Vs, the
// pumpkin boats on the regatta's lane, and the parade's giant balloons, a turkey, a pumpkin and a goose. Each is built facing +z, the way it goes.
internal static partial class TempleHalloween
{
    private static readonly Paint GooseBrown = new(new[] { 21, 26, 30 }), GooseBlack = new(new[] { 52, 55 }), GoosePale = new(new[] { 236, 46 }),
        HarvestOrange = new(new[] { 106, 88, 88, 108 }), HarvestRoast = new(new[] { 26, 30, 31 }), HarvestRed = new(new[] { 83, 73, 72, 74 }), HarvestGold = new(new[] { 106, 108 }),
        HarvestStem = new(new[] { 134, 136 }), HarvestDark = new(new[] { 21 });

    private static Model? HarvestSpook(string kind) => kind switch
    {
        "goose" => Goose(1),
        "balloonGoose" => Goose(3.4f),
        "balloonTurkey" => TurkeyBalloon(),
        "balloonPumpkin" => PumpkinBalloon(),
        "pboat" => PumpkinBoat(),
        _ => null,
    };

    // a lathe's turned shape laid along +z
    private static void Laid(Model m, (double R, double Y)[] profile, int sides, Paint paint, Vector3 at)
    {
        var first = m.Points.Count;
        m.Lathe(profile, sides, _ => paint, false, false);
        m.Move(first, p => new Vector3(p.X, p.Z, p.Y) + at);
    }

    private static Model Goose(float k)
    {
        var m = new Model();
        Laid(m, new (double, double)[] { (0, -620 * k), (170 * k, -420 * k), (290 * k, -60 * k), (250 * k, 260 * k), (120 * k, 480 * k), (0, 560 * k) }, 7, GooseBrown, Vector3.Zero);
        // the long black neck, its white chinstrap, the head
        m.Bar(new Vector3(0, 90 * k, 420 * k), new Vector3(0, 240 * k, 1040 * k), 90 * k, GooseBlack);
        m.Box(new Vector3(-90 * k, 200 * k, 1020 * k), new Vector3(90 * k, 330 * k, 1260 * k), GoosePale);
        m.Bar(new Vector3(0, 260 * k, 1260 * k), new Vector3(0, 240 * k, 1400 * k), 44 * k, GooseBlack);
        foreach (var s in new[] { -1f, 1f })
        {
            m.Facing(new[] { new Vector3(s * 180 * k, 90 * k, 340 * k), new Vector3(s * 180 * k, 90 * k, -220 * k), new Vector3(s * 1250 * k, 200 * k, -360 * k), new Vector3(s * 1250 * k, 200 * k, -20 * k) }, GooseBrown, Vector3.UnitY, both: true);
            m.Facing(new[] { new Vector3(s * 1250 * k, 200 * k, -20 * k), new Vector3(s * 1250 * k, 200 * k, -360 * k), new Vector3(s * 1820 * k, 360 * k, -500 * k) }, GooseBlack, Vector3.UnitY, both: true);
        }
        return m;
    }

    private static Model PumpkinBoat()
    {
        var m = new Model();
        m.Lathe(new (double, double)[] { (0, 0), (900, 60), (1800, 500), (2200, 1200), (2050, 1800), (1500, 2050) }, 12, i => i % 2 == 0 ? HarvestOrange : HarvestGold, false, false);
        m.Lathe(new (double, double)[] { (1500, 2050), (1300, 2000), (0, 2000) }, 8, _ => HarvestDark, false, false);
        m.Bar(new Vector3(-500, 2500, 400), new Vector3(-2000, 300, 1500), 70, HarvestRoast);
        return m;
    }

    private static Model TurkeyBalloon()
    {
        var m = new Model();
        m.Lathe(new (double, double)[] { (0, -1500), (1300, -1250), (2100, -300), (2100, 500), (1400, 1350), (0, 1600) }, 10, _ => HarvestRoast, false, false, at: new Vector3(0, 0, 0));
        // the head and wattle, the fan of its tail
        m.Lathe(new (double, double)[] { (0, -520), (420, -320), (520, 60), (380, 420), (0, 560) }, 8, _ => HarvestRoast, false, false, at: new Vector3(0, 1200, 2100));
        m.Bar(new Vector3(0, 0, 1500), new Vector3(0, 1000, 2100), 420, HarvestRoast);
        m.Bar(new Vector3(0, 1120, 2600), new Vector3(0, 900, 3000), 180, HarvestGold);
        m.Bar(new Vector3(120, 1000, 2500), new Vector3(120, 500, 2700), 110, HarvestRed);
        Paint[] fan = { HarvestRed, HarvestOrange, HarvestGold, HarvestRed, HarvestOrange, HarvestGold, HarvestRed };
        for (var k = 0; k < fan.Length; k++)
        {
            double a0 = -1.4 + k * 0.4, a1 = a0 + 0.4;
            Vector3 P(double a, double r) => new((float)(r * Math.Sin(a)), (float)(r * Math.Cos(a)) + 300, -1500);
            m.Facing(new[] { P(a0, 900), P(a1, 900), P(a1, 3400), P(a0, 3400) }, fan[k], -Vector3.UnitZ, both: true);
        }
        return m;
    }

    private static Model PumpkinBalloon()
    {
        var m = new Model();
        m.Lathe(new (double, double)[] { (0, -1900), (1500, -1650), (2600, -700), (2800, 200), (2300, 1300), (1200, 1800), (0, 1900) }, 12, i => i % 2 == 0 ? HarvestOrange : HarvestGold, false, false);
        m.Bar(new Vector3(0, 1800, 0), new Vector3(180, 2600, 0), 300, HarvestStem);
        return m;
    }
}
