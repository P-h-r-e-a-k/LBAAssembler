using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// Stella Entities' office (scene 392) dressed anew (the user, 2026-11-16/17: "we need to refresh the decor to give it a new look. Let's put a
// large animal skin rug on the floor. It doesn't even need to be an animal that exists. Let's move the desk from being straight in front of
// Twinsen when he enters the room to being by the left side wall ... and put a large bed into the room, with some black silk sheets perhaps
// with a moon and stars on them"):
//   - OfficeDesk: a writing desk in dark wood, a green leather top, a pedestal of drawers at each end with brass knobs, a brass lamp with a
//     white globe at its left end, papers and an inkpot -- its front +z, its length along x (stood against the west wall, turned to face
//     the room);
//   - MoonBed: a big bed, its head -z against a wall -- a tall wooden headboard with a gold crescent moon on it, a black silk cover hanging
//     down its sides, printed with gold crescent moons and pale stars, turned back under two black silk pillows;
//   - the beast's hide (BeastFirst on, tiles of a picture as the chamber's rugs are, CitadelProps.Rugs): a creature that never was -- a
//     purple hide with lilac bands, four splayed legs with white claws, a tufted tail, a horned head with yellow eyes, its head +x.
internal static partial class CitadelProps
{
    public const int OfficeDesk = 185, MoonBed = 186, BeastFirst = 189, BeastMost = 10, BeastStitch = 52;
    public const float DeskHalf = 750, DeskDepth = 320, DeskTop = 740;
    public const float BedHalf = 800, BedLength = 1100, BedTop = 540;
    private const int DeskWood = 1 * 16 + 4, DeskWoodDark = 1 * 16 + 2, DeskLeather = 8 * 16 + 3, DeskBrass = 6 * 16 + 9, DeskGlobe = 3 * 16 + 15, DeskPaper = 3 * 16 + 14,
        SilkBlack = 13 * 16 + 0, SilkShade = 3 * 16 + 0, SilkSheen = 13 * 16 + 2, HeadVelvet = 13 * 16 + 3, HeadButton = 13 * 16 + 1, MoonGold = 6 * 16 + 12, StarPale = 6 * 16 + 15, BedWood = 1 * 16 + 2;

    public static Body BuildOfficeDesk(byte[] header)
    {
        var m = new CarMesh("the office's desk", new[] { -1 });
        m.Light = 0.8f;
        const float W = DeskHalf, D = DeskDepth, Top = DeskTop, Slab = 50, PedW = 380;
        // the top: wood, a leather inlay set into it
        m.Box(0, new Vector3(0, Top - Slab / 2, 0), new Vector3(2 * W, Slab, 2 * D), DeskWood, DeskWood + 1);
        m.Up(new[] { m.P(0, new Vector3(-W + 70, Top + 2, -D + 60)), m.P(0, new Vector3(W - 70, Top + 2, -D + 60)), m.P(0, new Vector3(W - 70, Top + 2, D - 60)), m.P(0, new Vector3(-W + 70, Top + 2, D - 60)) }, DeskLeather);
        // a pedestal of drawers at each end, three drawer fronts on each and a brass knob on each; the back panel between them
        foreach (var sx in new[] { -1, 1 })
        {
            var cx = sx * (W - PedW / 2 - 20);
            m.Box(0, new Vector3(cx, (Top - Slab) / 2, 0), new Vector3(PedW, Top - Slab, 2 * D - 40), DeskWoodDark, DeskWood);
            for (var k = 0; k < 3; k++)
            {
                var y = 110 + k * 200;
                m.Box(0, new Vector3(cx, y, D - 15), new Vector3(PedW - 60, 170, 20), DeskWood, DeskWood);
                m.Ball(0, new Vector3(cx, y, D + 8), 18, DeskBrass);
            }
        }
        m.Box(0, new Vector3(0, Top - Slab - 220, -D + 40), new Vector3(2 * W - 2 * PedW, 420, 30), DeskWoodDark, DeskWoodDark);
        // a drawer under the top between the pedestals
        m.Box(0, new Vector3(0, Top - Slab - 60, D - 25), new Vector3(2 * W - 2 * PedW - 80, 100, 20), DeskWood, DeskWood);
        // the lamp at its left end, at the back: a brass foot and stem, a white globe
        var lx = -W + 230;
        m.Box(0, new Vector3(lx, Top + 20, -D + 140), new Vector3(170, 40, 170), DeskBrass, DeskBrass);
        m.Box(0, new Vector3(lx, Top + 170, -D + 140), new Vector3(40, 260, 40), DeskBrass, DeskBrass);
        m.Ball(0, new Vector3(lx, Top + 400, -D + 140), 120, DeskGlobe);
        // papers, an inkpot and its quill
        m.Up(new[] { m.P(0, new Vector3(-80, Top + 6, -60)), m.P(0, new Vector3(180, Top + 6, -90)), m.P(0, new Vector3(220, Top + 6, 120)), m.P(0, new Vector3(-40, Top + 6, 150)) }, DeskPaper, unlit: true);
        m.Up(new[] { m.P(0, new Vector3(-150, Top + 9, -20)), m.P(0, new Vector3(60, Top + 9, -80)), m.P(0, new Vector3(110, Top + 9, 90)), m.P(0, new Vector3(-100, Top + 9, 150)) }, DeskPaper - 2, unlit: true);
        m.Box(0, new Vector3(W - 260, Top + 45, -D + 120), new Vector3(70, 90, 70), SilkShade, SilkShade);
        m.Rod(0, new Vector3(W - 260, Top + 80, -D + 120), new Vector3(W - 330, Top + 260, -D + 60), DeskPaper);
        return m.ToBody(header);
    }

    public static Body BuildMoonBed(byte[] header)
    {
        var m = new CarMesh("the office's bed", new[] { -1 });
        m.Light = 0.8f;
        // (the silk hangs only a little over the bed's wooden sides: hanging to the floor it read as a grey side to a brown bed -- the user,
        // 2026-11-17, "the side of the bed is grey but the rest of it is brown, let's make it all brown")
        const float W = BedHalf, L = BedLength, Top = BedTop, Hang = 70;
        // the frame: four posts, the side boards and the footboard, all wood
        foreach (var sx in new[] { -1, 1 })
            foreach (var sz in new[] { -1, 1 })
                m.Box(0, new Vector3(sx * (W - 30), (sz < 0 ? 1290 : 650) / 2f, sz * (L - 30)), new Vector3(70, sz < 0 ? 1290 : 650, 70), BedWood, BedWood + 2);
        // (each side board only its outer face and its top edge, in pieces along it: as a box, the left board's inner face -- a long panel the
        // engine's sort gave the depth of its middle -- was drawn over the cover, a brown triangle across it; nothing inside the bed shows)
        const int BoardPieces = 4;
        foreach (var sx in new[] { -1, 1 })
            for (var j = 0; j < BoardPieces; j++)
            {
                float z0 = -L + 60 + (2 * L - 120) * j / BoardPieces, z1 = -L + 60 + (2 * L - 120) * (j + 1) / BoardPieces, xo = sx * (W - 7), xi = sx * (W - 53);
                m.Out(new[] { m.P(0, new Vector3(xo, 150, z0)), m.P(0, new Vector3(xo, 150, z1)), m.P(0, new Vector3(xo, Top - 50, z1)), m.P(0, new Vector3(xo, Top - 50, z0)) },
                      BedWood, new Vector3(0, (150 + Top - 50) / 2f, (z0 + z1) / 2));
                m.Up(new[] { m.P(0, new Vector3(xi, Top - 50, z0)), m.P(0, new Vector3(xo, Top - 50, z0)), m.P(0, new Vector3(xo, Top - 50, z1)), m.P(0, new Vector3(xi, Top - 50, z1)) }, BedWood + 2);
            }
        m.Box(0, new Vector3(0, 420, L - 30), new Vector3(2 * W - 60, 300, 40), BedWood, BedWood + 2);
        // the headboard upholstered (the user, same evening: "Let's make the headboard uppolstered"): plum velvet deep-buttoned in diamonds,
        // in a wooden frame -- its bottom rail at the mattress, its top rail over the padding -- the gold crescent moon on the top rail
        const float PadLow = Top - 20, PadHigh = 1170, PadFront = -L + 30 + 34;
        m.Box(0, new Vector3(0, (PadLow + PadHigh) / 2, -L + 30), new Vector3(2 * W - 60, PadHigh - PadLow, 68), HeadVelvet, HeadVelvet + 1);
        m.Box(0, new Vector3(0, PadHigh + 35, -L + 30), new Vector3(2 * W - 40, 70, 80), BedWood, BedWood + 2);
        m.Box(0, new Vector3(0, PadLow - 30, -L + 30), new Vector3(2 * W - 40, 60, 80), BedWood, BedWood + 2);
        var buttons = new List<(float X, float Y)>();
        for (var row = 0; row < 3; row++)
            for (var x = -W + 160 + (row % 2) * 140f; x <= W - 150; x += 280)
                buttons.Add((x, PadLow + 130 + row * 190));
        foreach (var (x, y) in buttons)
        {
            m.Ball(0, new Vector3(x, y, PadFront + 6), 16, HeadButton);
            // (the creases to the buttons diagonally above it: the diamonds)
            foreach (var (ox, oy) in buttons.Where(o => MathF.Abs(o.Y - y - 190) < 1 && MathF.Abs(MathF.Abs(o.X - x) - 140) < 1))
                m.Rod(0, new Vector3(x, y, PadFront + 3), new Vector3(ox, oy, PadFront + 3), HeadButton);
        }
        // (small, set into the top rail's front: higher up, it stood against the poster's frame on the wall behind)
        Crescent(m, new Vector3(0, PadHigh + 35, -L + 30 + 42), 32, false, MoonGold);
        // the cover: black silk over the mattress, a little over its sides and its foot
        float y0 = Top - Hang;
        var c = new[] { new Vector3(-W + 40, Top, -L + 60), new Vector3(W - 40, Top, -L + 60), new Vector3(W - 40, Top, L - 60), new Vector3(-W + 40, Top, L - 60) };
        m.Up(c.Select(v => m.P(0, v)).ToArray(), SilkBlack);
        var mid = new Vector3(0, Top - 150, 0);
        foreach (var (a, b) in new[] { (0, 3), (1, 2), (3, 2) })
        {
            var low = new[] { c[a] with { Y = y0 }, c[b] with { Y = y0 } };
            var outward = (c[a] + c[b]) / 2 - new Vector3(0, Top, 0);
            var push = Vector3.Normalize(outward with { Y = 0 }) * 25;
            m.Out(new[] { m.P(0, c[a]), m.P(0, c[b]), m.P(0, low[1] + push), m.P(0, low[0] + push) }, SilkShade, mid);
        }
        // turned back at the head, its sheen, and the two pillows on it
        m.Up(new[] { m.P(0, new Vector3(-W + 40, Top + 12, -L + 520)), m.P(0, new Vector3(W - 40, Top + 12, -L + 520)), m.P(0, new Vector3(W - 40, Top + 12, -L + 700)), m.P(0, new Vector3(-W + 40, Top + 12, -L + 700)) }, SilkSheen);
        foreach (var sx in new[] { -1, 1 })
            m.Box(0, new Vector3(sx * W / 2, Top + 70, -L + 260), new Vector3(W - 140, 120, 330), SilkBlack, SilkSheen);
        // the print: gold crescent moons and pale stars over the cover's top
        foreach (var (x, z, r) in new[] { (-420f, 0f, 120f), (300f, 300f, 140f), (-150f, 640f, 110f), (460f, 820f, 100f) })
            Crescent(m, new Vector3(x, Top + 5, z), r, true, MoonGold);
        foreach (var (x, z) in new[] { (-560f, 330f), (60f, 60f), (560f, -60f), (160f, 520f), (-470f, 850f), (-60f, 920f), (600f, 520f), (-260f, -150f), (330f, 860f) })
            Star(m, new Vector3(x, Top + 6, z), 55, StarPale);
        return m.ToBody(header);
    }

    // a crescent moon, a strip of quads round half a circle thickest at its middle: lying flat (on y) or standing facing +z
    private static void Crescent(CarMesh m, Vector3 at, float r, bool flat, int colour)
    {
        const int N = 7;
        Vector3 On(float a, float k) { var p = new Vector3(MathF.Cos(a) * r * k, 0, MathF.Sin(a) * r * k); return flat ? at + p : at + new Vector3(p.X, -p.Z, 0); }
        for (var i = 0; i < N; i++)
        {
            float t0 = i / (float)N, t1 = (i + 1) / (float)N;
            float a0 = MathF.PI / 2 + t0 * MathF.PI, a1 = MathF.PI / 2 + t1 * MathF.PI;
            float k0 = 1 - 0.6f * MathF.Sin(t0 * MathF.PI), k1 = 1 - 0.6f * MathF.Sin(t1 * MathF.PI);
            var q = new[] { m.P(0, On(a0, 1)), m.P(0, On(a1, 1)), m.P(0, On(a1, k1)), m.P(0, On(a0, k0)) };
            if (flat) m.Up(q, colour, unlit: true);
            else m.In(q, colour, at + new Vector3(0, 0, 400), unlit: true);
        }
    }

    // a four-pointed star lying flat: two thin diamonds across each other
    private static void Star(CarMesh m, Vector3 at, float r, int colour)
    {
        m.Up(new[] { m.P(0, at + new Vector3(-r, 0, 0)), m.P(0, at + new Vector3(0, 0, -r * 0.3f)), m.P(0, at + new Vector3(r, 0, 0)), m.P(0, at + new Vector3(0, 0, r * 0.3f)) }, colour, unlit: true);
        m.Up(new[] { m.P(0, at + new Vector3(0, 1, -r)), m.P(0, at + new Vector3(r * 0.3f, 1, 0)), m.P(0, at + new Vector3(0, 1, r)), m.P(0, at + new Vector3(-r * 0.3f, 1, 0)) }, colour, unlit: true);
    }

    // The beast's hide, a picture made from its shape: its body an oval, four splayed legs ending in clawed paws, a long tufted tail, a horned
    // head to the right with yellow eyes -- purple, banded lilac, its edge darker
    private static readonly Dictionary<char, int> BeastColours = new()
    {
        ['p'] = 13 * 16 + 3, ['P'] = 13 * 16 + 7, ['l'] = 13 * 16 + 12,['w'] = 3 * 16 + 15, ['y'] = 6 * 16 + 12, ['k'] = 3 * 16 + 0, ['h'] = 6 * 16 + 14, ['r'] = 4 * 16 + 3,
    };
    public static readonly string[] BeastRows = BeastPicture(48, 32);
    private static string[] BeastPicture(int width, int height)
    {
        float Seg(float u, float v, float ax, float az, float bx, float bz)
        {
            float dx = bx - ax, dz = bz - az, t = Math.Clamp(((u - ax) * dx + (v - az) * dz) / (dx * dx + dz * dz), 0, 1);
            float ex = u - (ax + t * dx), ez = v - (az + t * dz);
            return MathF.Sqrt(ex * ex + ez * ez);
        }
        char Fill(float u, float v)
        {
            // the eyes and the nose, the claws first
            foreach (var s in new[] { -1f, 1f })
            {
                if ((u - 16.5f) * (u - 16.5f) + (v - s * 2.2f) * (v - s * 2.2f) < 1.1f) return 'y';
                if ((u - 21.6f) * (u - 21.6f) + (v - s * 0.9f) * (v - s * 0.9f) < 0.5f) return 'k';
                // horns: back from the head's top, curving out
                if (Seg(u, v, 15, s * 4.2f, 12.5f, s * 8.6f) < 1.0f || Seg(u, v, 12.5f, s * 8.6f, 10.2f, s * 9.6f) < 0.7f) return 'h';
                foreach (var (ax, az, bx, bz) in new[] { (6f, 6f, 10.5f, 12.5f), (-9f, 6f, -13.5f, 12.5f) })
                {
                    // a paw's claws: three points past its end
                    float dx = bx - ax, dz = s * (bz - az), n = MathF.Sqrt(dx * dx + dz * dz);
                    var (fx, fz) = (dx / n, dz / n);
                    for (var k = -1; k <= 1; k++)
                    {
                        float cx = bx + fx * 2.9f + -fz * k * 1.3f, cz = s * bz + fz * 2.9f + fx * k * 1.3f;
                        if ((u - cx) * (u - cx) + (v - cz) * (v - cz) < 0.45f) return 'w';
                    }
                }
            }
            if ((u - 20.5f) * (u - 20.5f) / 4f + v * v / 1.2f < 0.35f && u > 21.2f) return 'r';
            return '\0';
        }
        bool Inside(float u, float v)
        {
            if ((u + 2) * (u + 2) / (13.5f * 13.5f) + v * v / (8.5f * 8.5f) < 1) return true;                       // the body
            if ((u - 15) * (u - 15) + v * v < 5.2f * 5.2f) return true;                                                // the head
            if ((u - 19.5f) * (u - 19.5f) / (2.8f * 2.8f) + v * v / (3f * 3f) < 1) return true;                       // the snout
            foreach (var s in new[] { -1f, 1f })
            {
                if (Seg(u, v, 6, s * 6, 10.5f, s * 12.5f) < 2.3f || (u - 10.5f) * (u - 10.5f) + (v - s * 12.5f) * (v - s * 12.5f) < 2.6f * 2.6f) return true;
                if (Seg(u, v, -9, s * 6, -13.5f, s * 12.5f) < 2.3f || (u + 13.5f) * (u + 13.5f) + (v - s * 12.5f) * (v - s * 12.5f) < 2.6f * 2.6f) return true;
            }
            if (Seg(u, v, -15, 0, -19.5f, 1.8f) < 1.3f || Seg(u, v, -19.5f, 1.8f, -22.5f, 0.6f) < 1.1f) return true;   // the tail
            return false;
        }
        var rows = new char[height][];
        for (var r = 0; r < height; r++)
        {
            rows[r] = new char[width];
            for (var c = 0; c < width; c++)
            {
                float u = c - width / 2f + 0.5f, v = r - height / 2f + 0.5f;
                var mark = Fill(u, v);
                if (mark != '\0') { rows[r][c] = mark; continue; }
                if (!Inside(u, v)) { rows[r][c] = ' '; continue; }
                var edge = !Inside(u + 1, v) || !Inside(u - 1, v) || !Inside(u, v + 1) || !Inside(u, v - 1);
                if (edge) { rows[r][c] = 'p'; continue; }
                // (the tail's tuft lilac; bands across the back, curving forward from the spine)
                if (u < -21) { rows[r][c] = 'l'; continue; }
                var band = (u + 0.45f * MathF.Abs(v) + 40) % 5f;
                rows[r][c] = band < 1.3f && u < 12 ? 'l' : 'P';
            }
        }
        return rows.Select(r => new string(r)).ToArray();
    }
    public static readonly List<(int C0, int C1, int R0, int R1)> BeastTiles = PictureTiles(BeastRows, 8, BeastColours);
    private static Body BuildBeastTile(byte[] header, (int C0, int C1, int R0, int R1) t)
    {
        var m = new CarMesh("the beast's hide", new[] { -1 });
        Stitched(m, t.C0, t.C1, t.R0, t.R1, (c, r) => BeastRows[r][c], Corners(m, (c, r) => new Vector3((c - t.C0) * BeastStitch, 0, (r - t.R0) * BeastStitch)),
                 BeastColours, new Vector3(0, -100, 0), Grow);
        return m.ToBody(header);
    }
    private static short[]? BeastTileBox(int generic)
    {
        if (generic < BeastFirst || generic >= BeastFirst + Math.Min(BeastMost, BeastTiles.Count)) return null;
        var k = BeastTiles[generic - BeastFirst];
        return new short[] { 0, -16, 0, (short)((k.C1 - k.C0) * BeastStitch), 0, (short)((k.R1 - k.R0) * BeastStitch) };
    }
    public static IEnumerable<(int Generic, string Name, Func<byte[], Body> Build)> OfficeBodies() =>
        new (int, string, Func<byte[], Body>)[] { (OfficeDesk, "the office's desk", BuildOfficeDesk), (MoonBed, "the office's bed", BuildMoonBed) }
        .Concat(BeastTiles.Take(BeastMost).Select((t, k) => (BeastFirst + k, $"the beast's hide, tile {k + 1}", (Func<byte[], Body>)(h => BuildBeastTile(h, t)))));
}
