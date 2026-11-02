using LBAAssembler.Terrain.Temple;

namespace LBAAssembler.Terrain.Harvest;

// The Long Table's palette: an autumn written into Otringal's, the way the Temple of Bú's is (TemplePalette: everything drawn on an island is
// drawn by palette index, the cars and the display too, so the island's palette is kept and only the slots nearest these colours move), its
// fog an amber haze; and its sky, late-afternoon gold over a slate lake.
internal static class HarvestPalette
{
    // The colours the island is drawn in, those that matter most first: they take the nearest free slots in that order.
    public static readonly (int R, int G, int B)[] Colours =
    {
        (214, 62, 32), (232, 110, 30), (245, 170, 40), (250, 208, 92), (170, 30, 42), (120, 18, 32),       // maple red, orange, gold, pale gold, cranberry, deep cranberry
        (190, 150, 90), (216, 190, 120), (150, 110, 70), (110, 76, 46), (72, 48, 30), (244, 232, 200),     // straw, pale straw, wood, dark wood, bark, cream
        (130, 120, 60), (110, 124, 62), (72, 132, 62), (54, 104, 50), (120, 104, 78), (92, 84, 70),        // olive, field green, turf, dark turf, stubble, dark stubble
        (240, 150, 40), (200, 112, 44), (102, 60, 30), (160, 100, 50), (204, 152, 90), (240, 236, 226),    // pumpkin, dark pumpkin, gravy, roast, light roast, mash
        (128, 122, 116), (98, 94, 94), (66, 62, 64), (176, 170, 160), (196, 40, 48), (52, 74, 128),         // pavement, road, dark road, stone, flag red, flag blue
    };
    public static readonly (int R, int G, int B) Haze = (214, 170, 120);

    public static TemplePalette.Result Build(byte[] sourceXpl)
    {
        var fake = new byte[768];
        var counts = new long[256];
        for (var c = 0; c < Colours.Length; c++)
        {
            fake[c * 3] = (byte)Colours[c].R; fake[c * 3 + 1] = (byte)Colours[c].G; fake[c * 3 + 2] = (byte)Colours[c].B;
            counts[c] = Colours.Length - c;
        }
        // (the rest of the fake palette: unused, but one bright value keeps PolarTextures.Colours.Rgb from reading it as a 6-bit palette)
        return TemplePalette.Build(sourceXpl, fake, counts, maxShift: 70, fogRgb: Haze);
    }

    // RESS.HQR's SKYSEA entry, 256 x 256, as TemplePalette.Sky lays it out: the sea the left half of the top 128 rows, the sky the right half, each
    // repeating across its 128 x 128 square.
    public static byte[] Sky(byte[] palette)
    {
        var sky = new byte[256 * 256];
        var random = new Random(1578);
        int[] scales = { 4, 8, 16, 32 };
        var lattices = scales.Select(n => Enumerable.Range(0, n * n).Select(_ => random.NextDouble()).ToArray()).ToArray();
        double Noise(int x, int y)
        {
            double sum = 0, weight = 0, amp = 1;
            for (var o = 0; o < scales.Length; o++)
            {
                var n = scales[o];
                double u = x * n / 128.0, v = y * n / 128.0;
                int i0 = (int)Math.Floor(u), j0 = (int)Math.Floor(v);
                double fu = u - i0, fv = v - j0;
                fu = fu * fu * (3 - 2 * fu); fv = fv * fv * (3 - 2 * fv);
                double L(int i, int j) => lattices[o][((j % n + n) % n) * n + (i % n + n) % n];
                sum += ((L(i0, j0) * (1 - fu) + L(i0 + 1, j0) * fu) * (1 - fv) + (L(i0, j0 + 1) * (1 - fu) + L(i0 + 1, j0 + 1) * fu) * fv) * amp;
                weight += amp; amp *= 0.55;
            }
            return sum / weight;
        }
        byte Near(double r, double g, double b) => (byte)Snow.SnowColour.Nearest(palette, r, g, b);
        for (var y = 0; y < 128; y++)
            for (var x = 0; x < 128; x++)
            {
                var cloud = Math.Clamp((Noise(x, y) - 0.46) / 0.3, 0, 1);
                sky[y * 256 + 128 + x] = Near(226 + 26 * cloud, 176 + 56 * cloud, 112 + 86 * cloud);
                var ripple = Noise(x * 2 % 128, y);
                sky[y * 256 + x] = Near(58 + 44 * ripple, 84 + 44 * ripple, 104 + 34 * ripple);
            }
        return sky;
    }
}
