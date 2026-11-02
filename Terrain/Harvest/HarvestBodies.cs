using System.IO;
using System.Numerics;
using LBAAssembler.Terrain.Snow;

namespace LBAAssembler.Terrain.Harvest;

// The Long Table's things, each made of flat-coloured faces (SnowMesh), every one its own model from nothing: from the fair (tents, a Ferris
// wheel, hay, prize pumpkins), the regatta (pumpkin boats), the feast (the horn of plenty, tarts, pies, the turkey, Mash Mountain), the fort, the
// harbour, the avenue, the stadium and the maze. A body holds 550 points at most: Fits checks.
internal static class HarvestBodies
{
    public const int MaxPoints = 550;

    private static readonly Rgb Bark = new(72, 48, 30), BarkLight = new(110, 76, 46), Wood = new(150, 110, 70), WoodDark = new(110, 76, 46), Cream = new(244, 232, 200),
        Straw = new(216, 190, 120), StrawDark = new(190, 150, 90), Pumpkin = new(240, 150, 40), PumpkinDark = new(200, 112, 44), Stem = new(110, 124, 62), Crust = new(204, 152, 90),
        Roast = new(160, 100, 50), RoastLight = new(204, 152, 90), Gravy = new(102, 60, 30), Mash = new(240, 236, 226), Cranberry = new(170, 30, 42), CranberryDeep = new(120, 18, 32),
        Gold = new(245, 170, 40), PaleGold = new(250, 208, 92), Stone = new(176, 170, 160), StoneDark = new(128, 122, 116), Road = new(98, 94, 94), Dark = new(66, 62, 64),
        Red = new(196, 40, 48), Blue = new(52, 74, 128), White = new(240, 236, 226), Green = new(72, 132, 62), GreenDark = new(54, 104, 50), Silver = new(176, 170, 160), Brick = new(160, 80, 50), BrickDark = new(120, 66, 46);

    public static SnowMesh? Make(string what, SnowShading s)
    {
        var m = new SnowMesh(s);
        switch (what)
        {
            case "tent": Tent(m, Red); break;
            case "tent2": Tent(m, new Rgb(52, 74, 128)); break;
            case "ferris": Ferris(m); break;
            case "stall": Stall(m); break;
            case "hay": m.Cylinder(new Vector3(-800, 1000, 0), new Vector3(800, 1000, 0), 1050, 1050, 8, StrawDark, Straw); break;
            case "bales": Bales(m); break;
            case "pumpkin": Gourd(m, 900, 760, Pumpkin, PumpkinDark, 0); break;
            case "prize": Gourd(m, 1900, 1500, Pumpkin, PumpkinDark, 0); break;
            case "boat": Boat(m); break;
            case "buoy": m.Cylinder(new Vector3(0, -80, 0), new Vector3(0, 380, 0), 260, 170, 6, Red, White); break;
            case "tart": Tart(m); break;
            case "pie": Pie(m, 3400, Pumpkin, PumpkinDark, true); break;
            case "pecan": Pie(m, 3100, Gravy, Roast, false); break;
            case "sweet": Sweet(m); break;
            case "turkey": Turkey(m); break;
            case "mash": MashMountain(m); break;
            case "cran": Cran(m); break;
            case "gravyboat": GravyBoat(m); break;
            case "candle": Candle(m); break;
            case "fork": Fork(m); break;
            case "knife": Knife(m); break;
            case "cob": Cob(m); break;
            case "apple": Gourd(m, 620, 600, Red, CranberryDeep, 2); break;
            case "squash": Gourd(m, 860, 620, PaleGold, Gold, 2); break;
            case "chair": Chair(m); break;
            case "palisade": Palisade(m); break;
            case "bastion": Bastion(m); break;
            case "hall": Hall(m, 9000, 3600); break;
            case "shed": Hall(m, 5000, 2800); break;
            case "ship": Ship(m); break;
            case "crates": Crates(m); break;
            case "tower": Tower(m); break;
            case "bldg0": Building(m, 5600, 7200, Brick, BrickDark); break;
            case "bldg1": Building(m, 5600, 11000, Stone, StoneDark); break;
            case "bldg2": Building(m, 5600, 9000, new Rgb(132, 104, 80), new Rgb(92, 84, 70)); break;
            case "bldg3": Building(m, 5600, 13500, new Rgb(120, 104, 78), Dark); break;
            case "review": Review(m); break;
            case "stand": Stand(m); break;
            case "goal": Goal(m); break;
            case "board": Board(m); break;
            case "lines": FieldLines(m); break;
            case "corn": Corn(m); break;
            case "flagCA": FlagCanada(m); break;
            case "flagUS": FlagStates(m); break;
            case "arch": Arch(m); break;
            case "post": m.Cylinder(new Vector3(0, -60, 0), new Vector3(0, 1100, 0), 280, 210, 5, Stone, StoneDark); break;
            case "mill": Mill(m); break;
            default: return null;
        }
        if (m.Points.Count > MaxPoints) throw new InvalidDataException($"The Long Table's {what}: {m.Points.Count} points, more than a body's {MaxPoints}.");
        return m;
    }

    private static Vector3 V(double x, double y, double z) => new((float)x, (float)y, (float)z);
    private static Rgb Colour(int k) => new[] { Red, Blue, Gold, Cream, Green, Pumpkin, new Rgb(120, 104, 78), White }[k & 7];

    // ---- the fair
    private static void Tent(SnowMesh m, Rgb stripe)
    {
        const int Sides = 8; const double R = 2800, Wall = 1500, Top = 3700;
        Vector3 Rim(int k, double y) => V(R * Math.Cos(k * Math.Tau / Sides), y, R * Math.Sin(k * Math.Tau / Sides));
        for (var k = 0; k < Sides; k++)
        {
            var c = k % 2 == 0 ? stripe : Cream;
            var side = Rim(k, 0) + Rim(k + 1, 0);
            m.Poly(c, side, Rim(k, -60), Rim(k + 1, -60), Rim(k + 1, Wall), Rim(k, Wall));
            m.Poly(c, side + V(0, 3000, 0), Rim(k, Wall), Rim(k + 1, Wall), V(0, Top, 0));
        }
        m.Cylinder(V(0, Top, 0), V(0, Top + 900, 0), 30, 30, 3, Bark);
        m.TwoSided(Gold, V(0, Top + 900, 0), V(0, Top + 560, 0), V(620, Top + 730, 0));
    }

    private static void Ferris(SnowMesh m)
    {
        const double R = 6600, Hub = 7600, Gap = 600; const int Sides = 16;
        Vector3 P(int k, double z, double r = R) => V(r * Math.Cos(k * Math.Tau / Sides), Hub + r * Math.Sin(k * Math.Tau / Sides), z);
        for (var k = 0; k < Sides; k++)
        {
            m.Beam(P(k, -Gap), P(k + 1, -Gap), 110, 110, k % 2 == 0 ? Red : Gold);
            m.Beam(P(k, Gap), P(k + 1, Gap), 110, 110, k % 2 == 0 ? Red : Gold);
            if (k % 2 == 0)
            {
                m.Beam(V(0, Hub, -Gap), P(k, -Gap), 70, 70, Silver);
                m.Beam(V(0, Hub, Gap), P(k, Gap), 70, 70, Silver);
                var c = P(k, 0);
                m.Box(c + V(-420, -900, -420), c + V(420, -300, 420), Colour(k / 2), bottom: true);
            }
        }
        foreach (var z in new[] { -1000.0, 1000.0 })
        {
            m.Beam(V(-3600, -50, z), V(0, Hub, z * 0.7), 180, 180, Dark);
            m.Beam(V(3600, -50, z), V(0, Hub, z * 0.7), 180, 180, Dark);
        }
    }

    private static void Stall(SnowMesh m)
    {
        m.Box(V(-1500, -60, -900), V(1500, 900, 900), Wood, Straw);
        foreach (var x in new[] { -1400.0, 1400.0 })
            m.Beam(V(x, 900, 800), V(x, 2200, 800), 90, 90, BarkLight);
        for (var k = 0; k < 6; k++)
            m.Poly(k % 2 == 0 ? Red : Cream, Vector3.UnitY, V(-1650 + k * 550, 2200, -1100), V(-1100 + k * 550, 2200, -1100), V(-1100 + k * 550, 2500, 1100), V(-1650 + k * 550, 2500, 1100));
    }

    private static void Bales(SnowMesh m)
    {
        for (var k = 0; k < 3; k++)
            m.Box(V(-1600 + k * 1100, -40 + 0, -560), V(-540 + k * 1100, 700, 560), StrawDark, Straw);
        m.Box(V(-1000, 700, -560), V(60, 1400, 560), StrawDark, Straw);
    }

    // a round thing of lobes, standing on its underside (a pumpkin, a gourd, an apple)
    private static void Gourd(SnowMesh m, double r, double h, Rgb light, Rgb dark, int lobes)
    {
        var n = lobes > 0 ? 8 : 12;
        m.Ellipsoid(V(0, h * 0.82, 0), r, h, r, 5, n, light, (i, j) => 1 + (lobes == 0 ? 0.07 * Math.Cos(j * Math.PI) : 0), colourAt: (i, j) => j % 2 == 0 ? light : dark);
        m.Cylinder(V(0, h * 1.7, 0), V(r * 0.08, h * 2.05, 0), r * 0.14, r * 0.08, 4, Stem);
    }

    private static void Boat(SnowMesh m)
    {
        // a giant pumpkin, hollowed: its top cut off level, a paddle over the side
        m.Ellipsoid(V(0, 500, 0), 2300, 1700, 2300, 6, 12, Pumpkin, colourAt: (i, j) => j % 2 == 0 ? Pumpkin : PumpkinDark, below: true);
        m.Cylinder(V(0, 1890, 0), V(0, 1905, 0), 1500, 1500, 10, Bark, Bark);
        m.Beam(V(-400, 2200, 900), V(-1800, 300, 2400), 90, 90, Wood);
        m.Box(V(-2200, 240, 2250), V(-1500, 280, 2600), Wood);
    }

    // ---- the feast
    private static void Tart(SnowMesh m)
    {
        m.Cylinder(V(0, 0, 0), V(0, 520, 0), 1250, 1450, 14, Crust, Crust, capA: false, capB: false);
        m.Cylinder(V(0, 520, 0), V(0, 560, 0), 1450, 1250, 14, RoastLight, Roast, capA: false);
        m.Ellipsoid(V(-300, 590, 250), 240, 80, 200, 2, 5, Gravy);
        m.Ellipsoid(V(320, 590, -150), 240, 80, 200, 2, 5, Gravy);
    }

    private static void Pie(SnowMesh m, double r, Rgb fill, Rgb edge, bool cream)
    {
        m.Cylinder(V(0, 0, 0), V(0, 620, 0), r * 0.92, r, 16, Crust, capA: false, capB: false);
        m.Cylinder(V(0, 620, 0), V(0, 700, 0), r * 1.03, r * 0.9, 16, RoastLight, capA: false, capB: false);
        m.Cylinder(V(0, 640, 0), V(0, 660, 0), r * 0.9, r * 0.9, 16, fill, fill, capA: false);
        if (cream)
            for (var k = 0; k < 6; k++)
                m.Cylinder(V(r * 0.6 * Math.Cos(k * Math.Tau / 6), 660, r * 0.6 * Math.Sin(k * Math.Tau / 6)), V(r * 0.6 * Math.Cos(k * Math.Tau / 6), 1120, r * 0.6 * Math.Sin(k * Math.Tau / 6)), 330, 0, 6, Cream);
        else
            for (var k = 0; k < 9; k++)
                m.Ellipsoid(V(r * 0.28 * (k / 3 + 0.6) * Math.Cos(k * 2.4), 700, r * 0.28 * (k / 3 + 0.6) * Math.Sin(k * 2.4)), 300, 90, 180, 2, 4, edge);
    }

    private static void Sweet(SnowMesh m)
    {
        m.Box(V(-3000, 0, -2100), V(3000, 800, 2100), Cream, Pumpkin);
        for (var x = 0; x < 5; x++)
            for (var z = 0; z < 3; z++)
                m.Box(V(-2500 + x * 1100, 800, -1500 + z * 1100), V(-1950 + x * 1100, 1120, -950 + z * 1100), (x + z) % 2 == 0 ? White : PaleGold, Cream);
    }

    private static void Turkey(SnowMesh m)
    {
        m.Cylinder(V(0, 0, 0), V(0, 240, 0), 5200, 5700, 16, Cream, Cream, capA: false);
        var body = (Func<int, int, double>)((i, j) => 1 + 0.05 * Math.Sin(i * 1.8 + j));
        m.Ellipsoid(V(0, 2500, 0), 4300, 2500, 3300, 6, 12, Roast, body, colourAt: (i, j) => (i + j) % 3 == 0 ? RoastLight : Roast);
        foreach (var side in new[] { -1.0, 1.0 })
        {
            // drumsticks, off the tail end; wings folded along the sides
            m.Ellipsoid(V(3000, 2500, side * 2500), 2000, 1100, 1100, 4, 8, Roast, colourAt: (i, j) => j % 2 == 0 ? RoastLight : Roast);
            m.Cylinder(V(4700, 2900, side * 2700), V(5900, 3200, side * 2500), 340, 240, 6, Cream, Mash);
            m.Ellipsoid(V(-600, 2400, side * 3300), 1900, 900, 500, 4, 8, RoastLight);
        }
        for (var k = 0; k < 10; k++)
            m.Ellipsoid(V(4500 * Math.Cos(k * 0.64 + 0.2), 470, 4500 * Math.Sin(k * 0.64 + 0.2)), 420, 300, 420, 2, 5, k % 3 == 0 ? Green : Cranberry);
    }

    private static void MashMountain(SnowMesh m)
    {
        // lumps heaped up to a crater, the gravy down its sides
        double Bump(int i, int j) => 1 + 0.09 * Math.Sin(i * 2.3 + j * 1.9) + 0.05 * Math.Cos(j * 3.1);
        const int Rings = 7, Segs = 14;
        Vector3 At(int i, int j)
        {
            var f = (double)i / Rings;
            var r = (4700 * (1 - f) * (1 - f) + 900) * Bump(i, j);
            return V(r * Math.Cos(j * Math.Tau / Segs), 4700 * Math.Sqrt(f), r * Math.Sin(j * Math.Tau / Segs));
        }
        for (var i = 0; i < Rings; i++)
            for (var j = 0; j < Segs; j++)
            {
                var p = new[] { At(i, j), At(i, j + 1), At(i + 1, j + 1), At(i + 1, j) };
                var c = j % 4 == 1 && i >= Rings - 5 || i == Rings - 1 ? Gravy : (i + j) % 2 == 0 ? Mash : new Rgb(232, 222, 200);
                m.Poly(c, (p[0] + p[2]) / 2 - V(0, 2000, 0), p);
            }
        m.Cylinder(V(0, 4600, 0), V(0, 4660, 0), 900, 900, 10, Gravy, Gravy, capA: false);
    }

    private static void Cran(SnowMesh m)
    {
        m.Cylinder(V(0, 0, 0), V(0, 160, 0), 2300, 2500, 12, Cream, Cream, capA: false);
        for (var k = 0; k < 6; k++)
            m.Cylinder(V(0, 160 + k * 440, 0), V(0, 600 + k * 440, 0), k % 2 == 0 ? 1500 : 1400, k % 2 == 0 ? 1400 : 1500, 12, k % 2 == 0 ? Cranberry : CranberryDeep, Cranberry, capA: false, capB: k == 5);
    }

    private static void GravyBoat(SnowMesh m)
    {
        m.Cylinder(V(0, 0, 0), V(0, 200, 0), 2300, 2800, 12, Cream, Cream, capA: false);
        m.Ellipsoid(V(0, 1300, 0), 3300, 1250, 1700, 5, 12, Mash, (i, j) => 1 + (j % 12 < 2 || j % 12 == 11 ? 0.18 : 0), below: true);
        m.Cylinder(V(-200, 2470, 0), V(-200, 2500, 0), 1200, 1200, 10, Gravy, Gravy, capA: false);
        m.Beam(V(-3200, 1900, 0), V(-4300, 2100, 0), 180, 260, Mash);
        m.Beam(V(-4300, 2100, 0), V(-3500, 700, 0), 180, 260, Mash);
    }

    private static void Candle(SnowMesh m)
    {
        m.Cylinder(V(0, 0, 0), V(0, 240, 0), 1300, 400, 8, Gold, capA: false);
        m.Cylinder(V(0, 240, 0), V(0, 5200, 0), 420, 380, 8, Cream);
        m.Cylinder(V(0, 5200, 0), V(0, 6000, 0), 210, 0, 6, Gold);
    }

    private static void Fork(SnowMesh m)
    {
        m.Box(V(-5200, 30, -350), V(800, 140, 350), Silver);
        m.Box(V(800, 30, -900), V(2100, 140, 900), Silver);
        for (var k = 0; k < 4; k++) m.Box(V(2100, 30, -900 + k * 500), V(5000, 120, -600 + k * 500), Silver);
    }

    private static void Knife(SnowMesh m)
    {
        m.Box(V(-5200, 30, -330), V(-900, 140, 330), Silver);
        m.Box(V(-900, 30, -560), V(4600, 110, 620), Silver);
    }

    private static void Cob(SnowMesh m)
    {
        m.Ellipsoid(V(0, 520, 0), 1900, 520, 520, 4, 8, Gold, colourAt: (i, j) => (i + j) % 2 == 0 ? Gold : PaleGold);
        m.TwoSided(Green, V(-2500, 300, -600), V(-1000, 900, -100), V(-900, 200, -700));
        m.TwoSided(GreenDark, V(-2500, 300, 600), V(-1000, 900, 100), V(-900, 200, 700));
    }

    private static void Chair(SnowMesh m)
    {
        // the seat high enough to drive under, 24 cells across its legs (the road goes between them), its back away from the table: -z
        const double W = 6200, D = 5400, Seat = 3900, Back = 10400; const double T = 680;
        m.Box(V(-W, Seat, -D), V(W, Seat + 520, D), WoodDark, Wood, bottom: true);
        foreach (var (x, z) in new[] { (-W + 300, -D + 300), (W - 300, -D + 300), (-W + 300, D - 300), (W - 300, D - 300) })
            m.Box(V(x - T / 2, -50, z - T / 2), V(x + T / 2, Seat, z + T / 2), WoodDark, bottom: false);
        foreach (var x in new[] { -W + 300, W - 300 })
            m.Box(V(x - T / 2, Seat, -D), V(x + T / 2, Back, -D + T), Wood);
        for (var k = 0; k < 3; k++)
            m.Box(V(-W, Seat + 2000 + k * 1600, -D), V(W, Seat + 2700 + k * 1600, -D + 360), Wood, WoodDark, bottom: true);
    }

    // ---- the fort
    private static void Palisade(SnowMesh m)
    {
        for (var k = -3; k <= 3; k++)
        {
            var x = k * 680.0; var h = 2300 + 160 * ((k * 7 + 9) % 3);
            m.Cylinder(V(x, -80, 0), V(x, h, 0), 350, 350, 5, k % 2 == 0 ? Wood : WoodDark, capB: false, phase: 0.2);
            m.Cylinder(V(x, h, 0), V(x, h + 420, 0), 350, 0, 5, BarkLight, phase: 0.2);
        }
    }

    private static void Bastion(SnowMesh m)
    {
        m.Box(V(-1900, -80, -1900), V(1900, 3300, 1900), WoodDark, Wood);
        m.Cylinder(V(0, 3300, 0), V(0, 5200, 0), 2800, 0, 4, BarkLight, phase: 0.5);
    }

    private static void Hall(SnowMesh m, double length, double width)
    {
        double W = width / 2, L = length / 2, H = 2100, R = H + width * 0.62;
        m.Box(V(-L, -80, -W), V(L, H, W), Wood, WoodDark);
        Rgb Roof = new(92, 62, 40);
        foreach (var s in new[] { -1.0, 1.0 })
            m.Poly(Roof, V(0, 1, s), V(-L - 180, H - 100, s * (W + 260)), V(L + 180, H - 100, s * (W + 260)), V(L + 180, R, 0), V(-L - 180, R, 0));
        foreach (var s in new[] { -1.0, 1.0 })
            m.Poly(WoodDark, V(s, 0, 0), V(s * L, H, -W), V(s * L, H, W), V(s * L, R, 0));
        m.Box(V(L * 0.4, H, -420), V(L * 0.4 + 840, R + 900, 420), StoneDark, Dark);
        for (var k = -2; k <= 2; k++)
            m.Poly(PaleGold, Vector3.UnitZ, V(k * L * 0.36 - 300, 900, W + 12), V(k * L * 0.36 + 300, 900, W + 12), V(k * L * 0.36 + 300, 1600, W + 12), V(k * L * 0.36 - 300, 1600, W + 12));
    }

    // ---- the harbour
    private static void Ship(SnowMesh m)
    {
        m.Ellipsoid(V(0, 500, 0), 9000, 1900, 2300, 5, 12, Bark, colourAt: (i, j) => i % 2 == 0 ? Bark : BarkLight);
        m.Box(V(-8200, 1400, -1700), V(-4300, 3700, 1700), BarkLight, Wood);
        m.Box(V(-4300, 1800, -2000), V(6500, 2000, 2000), Wood, Straw);
        foreach (var (x, h, w) in new[] { (-5200.0, 10000.0, 2800.0), (-200.0, 14000.0, 3600.0), (4700.0, 12000.0, 3000.0) })
        {
            m.Cylinder(V(x, 1800, 0), V(x, h, 0), 170, 90, 5, BarkLight);
            for (var k = 0; k < 2; k++)
            {
                var lo = 3800 + (h - 4600) * k / 2.0; var hi = lo + (h - 5000) / 2.0; var a = w * (1 - k * 0.2); var b = a * 0.85;
                m.TwoSided(Cream, V(x + 140, lo, -a), V(x + 140, lo, a), V(x + 340, hi, b), V(x + 340, hi, -b));
                m.Beam(V(x + 340, hi, -b), V(x + 340, hi, b), 70, 70, Bark);
            }
        }
    }

    private static void Crates(SnowMesh m)
    {
        m.Box(V(-1200, -50, -800), V(0, 1000, 400), WoodDark, Wood);
        m.Box(V(100, -50, -500), V(1300, 900, 700), Wood, Straw);
        m.Box(V(-800, 1000, -600), V(250, 1800, 300), Wood, Straw);
        m.Cylinder(V(1900, -50, 900), V(1900, 1100, 900), 520, 520, 8, BarkLight, WoodDark);
    }

    private static void Tower(SnowMesh m)
    {
        m.Box(V(-1300, -600, -1300), V(1300, 4300, 1300), StoneDark, Stone);
        for (var k = 0; k < 4; k++)
            m.Box(V(-1500 + k * 1000, 4300, -1500), V(-1000 + k * 1000, 4900, 1500), Stone);
    }

    // ---- the avenue
    private static void Building(SnowMesh m, double half, double height, Rgb wall, Rgb trim)
    {
        const double Deep = 3000;
        m.Box(V(-half, -80, -Deep), V(half, height, Deep), wall, trim);
        m.Box(V(-half - 120, height, -Deep - 120), V(half + 120, height + 280, Deep + 120), trim);
        var rows = (int)(height / 1700);
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < 5; c++)
            {
                double x = -half * 0.8 + c * half * 0.4, y = 900 + r * 1600;
                foreach (var s in new[] { -1.0, 1.0 })
                    m.Poly((r * 3 + c * 5) % 7 == 0 ? PaleGold : Dark, V(0, 0, s), V(x - 420, y, s * (Deep + 14)), V(x + 420, y, s * (Deep + 14)), V(x + 420, y + 800, s * (Deep + 14)), V(x - 420, y + 800, s * (Deep + 14)));
            }
    }

    private static void Review(SnowMesh m)
    {
        // the reviewing stand: tiers of benches under a striped awning
        for (var k = 0; k < 4; k++)
            m.Box(V(-8000, -60 + k * 640, -k * 1000 - 1000), V(8000, 640 + k * 640, 1000 - k * 1000), k % 2 == 0 ? Blue : Cream, Wood);
        for (var k = 0; k < 8; k++)
        {
            m.Poly(k % 2 == 0 ? Red : Cream, Vector3.UnitY, V(-8000 + k * 2000, 6300, -4400), V(-6000 + k * 2000, 6300, -4400), V(-6000 + k * 2000, 5300, 1800), V(-8000 + k * 2000, 5300, 1800));
            m.Poly(k % 2 == 0 ? Red : Cream, -Vector3.UnitY, V(-8000 + k * 2000, 6280, -4400), V(-6000 + k * 2000, 6280, -4400), V(-6000 + k * 2000, 5280, 1800), V(-8000 + k * 2000, 5280, 1800));
        }
        foreach (var x in new[] { -7900.0, 7900.0 }) m.Beam(V(x, 0, 1700), V(x, 5300, 1700), 180, 180, Dark);
    }

    // ---- the stadium
    private static void Stand(SnowMesh m)
    {
        // tiers of crowd (coloured dots of faces) rising away from +z
        const double Half = 7200, Tread = 900, Rise = 800; const int Tiers = 6, Across = 9;
        var random = new Random(1934);
        for (var t = 0; t < Tiers; t++)
        {
            double z0 = -t * Tread, z1 = z0 + Tread, y = t * Rise + 200;
            m.Poly(StoneDark, Vector3.UnitZ, V(-Half, y - Rise, z1), V(Half, y - Rise, z1), V(Half, y, z1), V(-Half, y, z1));
            for (var c = 0; c < Across; c++)
            {
                double x0 = -Half + c * 2 * Half / Across, x1 = x0 + 2 * Half / Across;
                m.Poly(Colour(random.Next(8)), Vector3.UnitY, V(x0, y, z0), V(x1, y, z0), V(x1, y, z1), V(x0, y, z1));
            }
        }
        var back = -Tiers * Tread + Tread;
        m.Poly(Brick, -Vector3.UnitZ, V(-Half, -80, back), V(Half, -80, back), V(Half, Tiers * Rise + 200, back), V(-Half, Tiers * Rise + 200, back));
        foreach (var s in new[] { -1.0, 1.0 })
            m.Poly(BrickDark, V(s, 0, 0), V(s * Half, -80, Tread), V(s * Half, -80, back), V(s * Half, Tiers * Rise + 200, back));
    }

    private static void Goal(SnowMesh m)
    {
        m.Beam(V(0, -40, 0), V(0, 2300, 0), 220, 220, Gold);
        m.Beam(V(-2900, 2300, 0), V(2900, 2300, 0), 180, 180, Gold);
        foreach (var x in new[] { -2900.0, 2900.0 }) m.Beam(V(x, 2300, 0), V(x, 7000, 0), 160, 160, Gold);
    }

    private static void Board(SnowMesh m)
    {
        foreach (var x in new[] { -5400.0, 5400.0 }) m.Beam(V(x, -60, 0), V(x, 6000, 0), 400, 400, Dark);
        m.Box(V(-6800, 6000, -300), V(6800, 10400, 300), Dark, Dark, bottom: true);
        for (var c = 0; c < 8; c++)
            for (var r = 0; r < 3; r++)
                m.Poly((c + r) % 3 == 0 ? Gold : (c * r) % 2 == 0 ? Red : White, Vector3.UnitZ, V(-6000 + c * 1600, 6700 + r * 1200, 320), V(-4800 + c * 1600, 6700 + r * 1200, 320), V(-4800 + c * 1600, 7500 + r * 1200, 320), V(-6000 + c * 1600, 7500 + r * 1200, 320));
    }

    // The field, from 110 yards' worth of Canada's and a shorter end of the States': yard lines, an end zone in each country's colours.
    private static void FieldLines(SnowMesh m)
    {
        const double W = 11200, L = 14300;
        for (var k = 0; k <= 10; k++)
        {
            var z = -L * 0.8 + k * L * 0.16;
            m.Poly(White, Vector3.UnitY, V(-W, 20, z - 70), V(W, 20, z - 70), V(W, 20, z + 70), V(-W, 20, z + 70));
        }
        m.Poly(Red, Vector3.UnitY, V(-W, 18, -L), V(W, 18, -L), V(W, 18, -L * 0.8), V(-W, 18, -L * 0.8));
        m.Poly(Blue, Vector3.UnitY, V(-W, 18, L * 0.8), V(W, 18, L * 0.8), V(W, 18, L), V(-W, 18, L));
    }

    // ---- the maze
    private static void Corn(SnowMesh m)
    {
        // a stretch of stalks, 6 cells long, along x, higher than a car: seen from either side, tasselled
        const double Half = 1560, H = 2500;
        for (var k = 0; k < 6; k++)
        {
            double x0 = -Half + k * Half / 3, x1 = x0 + Half / 3, top = H + 240 * ((k * 5) % 3);
            var c = k % 2 == 0 ? Green : new Rgb(110, 124, 62);
            m.TwoSided(c, V(x0, -40, 0), V(x1, -40, 0), V(x1, top, 120), V(x0, top, 120));
            m.TwoSided(k % 3 == 0 ? PaleGold : Straw, V(x0, top, 120), V(x1, top, 120), V((x0 + x1) / 2, top + 560, 0));
            m.TwoSided(GreenDark, V(x0, 1000, 0), V(x1, 900, 0), V((x0 + x1) / 2, 1300, 420));
        }
    }

    // ---- the border
    private static void Pole(SnowMesh m) => m.Cylinder(V(0, -80, 0), V(0, 9200, 0), 170, 90, 5, Silver);

    private static void FlagCanada(SnowMesh m)
    {
        Pole(m);
        const double X0 = 120, Y0 = 6400, H = 2700; var W = H * 2;
        void Bar(double a, double b, Rgb c) => m.TwoSided(c, V(X0 + W * a, Y0, 0), V(X0 + W * b, Y0, 0), V(X0 + W * b, Y0 + H, 0), V(X0 + W * a, Y0 + H, 0));
        Bar(0, 0.25, Red); Bar(0.25, 0.75, White); Bar(0.75, 1, Red);
        // the leaf: a shape of a few faces, a hair in front of the white
        var cx = X0 + W / 2; var cy = Y0 + H / 2; var u = H / 8;
        foreach (var s in new[] { -1.0, 1.0 })
        {
            foreach (var z in new[] { -6.0, 6.0 })
            {
                var o = V(0, 0, z);
                m.TwoSided(Red, V(cx, cy + 3.4 * u, 0) + o, V(cx + s * 1.3 * u, cy + 0.4 * u, 0) + o, V(cx, cy - 1.5 * u, 0) + o);
                m.TwoSided(Red, V(cx, cy + 0.6 * u, 0) + o, V(cx + s * 3.3 * u, cy + 1.2 * u, 0) + o, V(cx + s * 2.2 * u, cy - 1.5 * u, 0) + o, V(cx, cy - 1.5 * u, 0) + o);
                m.TwoSided(Red, V(cx - 0.2 * u, cy - 1.5 * u, 0) + o, V(cx + 0.2 * u, cy - 1.5 * u, 0) + o, V(cx + 0.2 * u, cy - 3 * u, 0) + o, V(cx - 0.2 * u, cy - 3 * u, 0) + o);
            }
        }
    }

    private static void FlagStates(SnowMesh m)
    {
        Pole(m);
        const double X0 = 120, Y0 = 6400, H = 2700; var W = H * 1.9;
        for (var k = 0; k < 13; k++)
        {
            double y0 = Y0 + H - (k + 1) * H / 13, y1 = y0 + H / 13, x0 = k < 7 ? X0 + W * 0.4 : X0;
            m.TwoSided(k % 2 == 0 ? Red : White, V(x0, y0, 0), V(X0 + W, y0, 0), V(X0 + W, y1, 0), V(x0, y1, 0));
        }
        var cantonY = Y0 + H * 6 / 13.0;
        m.TwoSided(Blue, V(X0, cantonY, 0), V(X0 + W * 0.4, cantonY, 0), V(X0 + W * 0.4, Y0 + H, 0), V(X0, Y0 + H, 0));
        for (var r = 0; r < 4; r++)
            for (var c = 0; c < 5; c++)
            {
                double x = X0 + W * 0.04 + c * W * 0.08, y = cantonY + H * 0.05 + r * H * 0.1;
                foreach (var z in new[] { -6.0, 6.0 }) m.TwoSided(White, V(x, y, z), V(x + 70, y, z), V(x + 70, y + 70, z), V(x, y + 70, z));
            }
    }

    // The border over the road on the table: its two halves red and blue, a flag at each end.
    private static void Arch(SnowMesh m)
    {
        const double Half = 3500, H = 4600;
        foreach (var (s, c) in new[] { (-1.0, Red), (1.0, Blue) })
        {
            m.Box(V(s * Half - 280, 0, -280), V(s * Half + 280, H, 280), c, Cream);
            m.Box(V(Math.Min(0, s * (Half + 300)), H, -160), V(Math.Max(0, s * (Half + 300)), H + 900, 160), c, Cream, bottom: true);
        }
    }

    private static void Mill(SnowMesh m)
    {
        (double Y, double R)[] outline = { (0, 3800), (600, 3700), (1200, 2900), (2600, 2600), (3400, 3100), (4300, 2500), (6800, 2000), (7700, 2500), (8500, 2500), (9400, 3100), (10600, 2800), (11300, 1300), (11900, 900), (12300, 1100) };
        for (var k = 0; k + 1 < outline.Length; k++)
            m.Cylinder(V(0, outline[k].Y, 0), V(0, outline[k + 1].Y, 0), outline[k].R, outline[k + 1].R, 10, k % 3 == 1 ? BarkLight : Bark, capA: false, capB: k + 2 == outline.Length);
        m.Ellipsoid(V(0, 12700, 0), 900, 800, 900, 3, 8, Gold);
    }
}
