using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The Imperial Hotel's outside made over (Otringal; the user, 2026-11-22: "As part of adding an upstairs to the imperial hotel, let's
// smarten up and refresh the outside of it. Keep the same basic external footprint, but a new look, and double the height to allow for the
// new floor"; the research -- the Belle Epoque grand hotels, E:\dump\TEMP\nov22\research_hotel_skate.md, part 2): the island's L of six
// pieces (OTRINGAL.OBL bodies 9-14, all placed at one origin, turned 2048) each rebuilt on its own footprint -- the box the build leaves it
// (RaceTrackBuilder's TrimKept may have made one shallower by the road) -- twice as tall: cream stone over a dark plinth, a string course
// between the floors, a cornice and a balustrade at the top; on each face that looks out (not against another piece) round-arched windows
// on the ground floor and tall French windows over little balconies on the first; a slate mansard with dormers over the main block; the
// corner block with the doors (its two canopies are the island's, kept) a dome in copper green with a gilded finial and IMPERIAL HOTEL in
// gold over its south door. Each a body of the build's own (RaceTrackReport.NewBodies), each decor's box as tall as its new body.
internal static class HotelFacade
{
    // (lit faces climb up to nine steps of their ramp: each base low in it -- cream stone, dark stone, white trim, slate, copper green, gold)
    private const int Stone = 2 * 16 + 5, Plinth = 14 * 16 + 2, Trim = 3 * 16 + 8, Slate = 11 * 16 + 2, Copper = 9 * 16 + 4, Gold = 6 * 16 + 5, Iron = 3 * 16 + 1;
    // (drawn as they are: the glass, the doorway's dark, the sign's board and letters)
    private const int Glass = 12 * 16 + 2, Doorway = 1 * 16 + 1, Board = 1 * 16 + 1, Letters = 6 * 16 + 12;
    private const float WindowPitch = 1200, WindowHalf = 260, Offset = 14;

    // The hotel's pieces: the decors of `bodies` placed at (x, z) in cube (cx, cz); the corner block with the doors is the one whose box holds
    // `door` (cube units)
    public sealed record Spec(int CubeX, int CubeZ, int X, int Z, int[] Bodies, (int X, int Z) Door, (int X, int Z) Main);

    public static void Apply(IslandFile island, Spec spec, RaceTrackOptions o, RaceTrackReport report)
    {
        if (island.CubeAt(spec.CubeX, spec.CubeZ) is not { } cube || o.NewBodyBase < 0) { report.Notes.Add("the Imperial Hotel's outside left as it was: no cube or no new bodies' base"); return; }
        var set = spec.Bodies.ToHashSet();
        var pieces = cube.Decors.Where(d => d.X == spec.X && d.Z == spec.Z && IslandFile.ObjectPageOf(d) == 0 && (set.Contains(d.Body & 0xFFFF) || (d.Body & 0xFFFF) >= o.NewBodyBase)).ToList();
        if (pieces.Count == 0) { report.Notes.Add("the Imperial Hotel's outside left as it was: none of its pieces at its place"); return; }
        var boxes = pieces.Select(d => (X0: d.XMin, X1: d.XMax, Z0: d.ZMin, Z1: d.ZMax)).ToList();
        var made = new List<string>();
        foreach (var d in pieces)
        {
            int x0 = d.XMin, x1 = d.XMax, z0 = d.ZMin, z1 = d.ZMax;
            var high = 2 * Math.Max(400, d.YMax - d.Y);
            // the faces that look out: not against another piece over most of their length
            bool Against(char side) => boxes.Any(b => (b.X0, b.X1, b.Z0, b.Z1) != (x0, x1, z0, z1) && side switch
            {
                'W' => Math.Abs(b.X1 - x0) < 40 && Overlap(b.Z0, b.Z1, z0, z1) > 0.6 * (z1 - z0),
                'E' => Math.Abs(b.X0 - x1) < 40 && Overlap(b.Z0, b.Z1, z0, z1) > 0.6 * (z1 - z0),
                'N' => Math.Abs(b.Z1 - z0) < 40 && Overlap(b.X0, b.X1, x0, x1) > 0.6 * (x1 - x0),
                _ => Math.Abs(b.Z0 - z1) < 40 && Overlap(b.X0, b.X1, x0, x1) > 0.6 * (x1 - x0),
            });
            var corner = spec.Door.X >= x0 && spec.Door.X <= x1 && spec.Door.Z >= z0 && spec.Door.Z <= z1;
            var main = spec.Main.X >= x0 && spec.Main.X <= x1 && spec.Main.Z >= z0 && spec.Main.Z <= z1;
            var body = Build(d, high, "WENS".Where(s => !Against(s)).ToArray(), corner, main);
            d.Body = (d.Body & ~0xFFFF) | (o.NewBodyBase + report.NewBodies.Count);
            report.NewBodies.Add(body);
            d.YMax = d.Y + high;
            made.Add($"{x1 - x0}x{z1 - z0} {high} high{(corner ? ", the doors, the dome and the sign" : main ? ", the mansard" : "")}");
        }
        report.Notes.Add($"the Imperial Hotel's outside made over, twice as tall: {string.Join("; ", made)}");
    }

    private static double Overlap(int a0, int a1, int b0, int b1) => Math.Max(0, Math.Min(a1, b1) - Math.Max(a0, b0));

    // A piece: its world box (cube units) and height, the faces that look out, whether it has the doors or the mansard -- its points the
    // decor's own (the decor turned 2048: local = origin - world, both ways)
    private static byte[] Build(IslandDecor d, int high, char[] outward, bool corner, bool main)
    {
        var m = new CarMesh("the Imperial Hotel", new[] { -1 });
        m.Light = 0.85f;
        Vector3 L(float wx, float y, float wz) => new(d.X - wx, y, d.Z - wz);
        int x0 = d.XMin, x1 = d.XMax, z0 = d.ZMin, z1 = d.ZMax;
        var mid = L((x0 + x1) / 2f, high / 2f, (z0 + z1) / 2f);
        float floor2 = high * 0.5f;
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 e, int colour, bool unlit = false) => m.Out(new[] { m.P(0, a), m.P(0, b), m.P(0, c), m.P(0, e) }, colour, mid, unlit);
        // a face's frame: along it from `s0` to `s1` (world x or z), its plane at `at`, out by `o` from it
        foreach (var side in "WENS")
        {
            var alongX = side is 'N' or 'S';
            float at = side switch { 'W' => x0, 'E' => x1, 'N' => z0, _ => z1 };
            float s0 = alongX ? x0 : z0, s1 = alongX ? x1 : z1;
            float sign = side is 'W' or 'N' ? -1 : 1;
            Vector3 P(float s, float y, float o) => alongX ? L(s, y, at + sign * o) : L(at + sign * o, y, s);
            void Face(float a0, float a1, float y0, float y1, float o, int colour, bool unlit = false) => Quad(P(a0, y0, o), P(a1, y0, o), P(a1, y1, o), P(a0, y1, o), colour, unlit);
            var looks = outward.Contains(side);
            // the wall (every side: the inner ones keep the L closed where the pieces' tops differ)
            Face(s0, s1, 0, high, 0, Stone);
            if (!looks) continue;
            // the plinth, the string course, the cornice and its top, the balustrade's rail
            Face(s0, s1, 0, 380, Offset, Plinth);
            Face(s0, s1, floor2 - 60, floor2 + 60, Offset * 3, Trim);
            Face(s0, s1, high - 260, high, Offset * 4, Trim);
            Quad(P(s0, high, 0), P(s1, high, 0), P(s1, high, Offset * 4), P(s0, high, Offset * 4), Trim);
            Face(s0, s1, high + 260, high + 330, Offset * 2, Trim);
            for (var s = s0 + 200; s < s1 - 100; s += 400) Face(s - 30, s + 30, high, high + 260, Offset * 2, Trim);
            // the windows, evenly along it; the corner block's ground floor its doors' arches instead
            var n = Math.Max(1, (int)((s1 - s0 - 500) / WindowPitch));
            for (var k = 0; k < n; k++)
            {
                var c = s0 + (s1 - s0) * (k + 0.5f) / n;
                if (corner)
                {
                    // (the doorway: dark under a gilded arch, wide as the canopy over it)
                    float w = Math.Min(700, (s1 - s0) / (2f * n) - 80);
                    Face(c - w, c + w, 0, floor2 - 450, Offset, Doorway, unlit: true);
                    m.Out(new[] { m.P(0, P(c - w - 80, floor2 - 450, Offset * 2)), m.P(0, P(c + w + 80, floor2 - 450, Offset * 2)), m.P(0, P(c, floor2 - 120, Offset * 2)) }, Gold, mid);
                }
                else
                {
                    // (round-arched: the glass, its stone surround, the arch's point)
                    Face(c - WindowHalf - 70, c + WindowHalf + 70, 520, floor2 - 300, Offset, Trim);
                    Face(c - WindowHalf, c + WindowHalf, 580, floor2 - 420, Offset * 2, Glass, unlit: true);
                    m.Out(new[] { m.P(0, P(c - WindowHalf - 70, floor2 - 300, Offset)), m.P(0, P(c + WindowHalf + 70, floor2 - 300, Offset)), m.P(0, P(c, floor2 - 120, Offset)) }, Trim, mid);
                }
                // the first floor: a French window, its surround, a balcony's slab and rail
                Face(c - WindowHalf - 70, c + WindowHalf + 70, floor2 + 200, high - 420, Offset, Trim);
                Face(c - WindowHalf, c + WindowHalf, floor2 + 240, high - 500, Offset * 2, Glass, unlit: true);
                Quad(P(c - WindowHalf - 140, floor2 + 200, 0), P(c + WindowHalf + 140, floor2 + 200, 0), P(c + WindowHalf + 140, floor2 + 200, 260), P(c - WindowHalf - 140, floor2 + 200, 260), Trim);
                Face(c - WindowHalf - 140, c + WindowHalf + 140, floor2 + 380, floor2 + 440, 260, Iron);
            }
        }
        // the roof: the main block's mansard (slate, sloped in from the cornice, its dormers), else flat zinc behind the balustrade
        if (main)
        {
            float inset = Math.Min(700, Math.Min(x1 - x0, z1 - z0) / 4f), top = high + 1300;
            var low = new[] { L(x0, high, z0), L(x1, high, z0), L(x1, high, z1), L(x0, high, z1) };
            var up = new[] { L(x0 + inset, top, z0 + inset), L(x1 - inset, top, z0 + inset), L(x1 - inset, top, z1 - inset), L(x0 + inset, top, z1 - inset) };
            for (var k = 0; k < 4; k++) Quad(low[k], low[(k + 1) % 4], up[(k + 1) % 4], up[k], Slate);
            m.Up(new[] { m.P(0, up[0]), m.P(0, up[1]), m.P(0, up[2]), m.P(0, up[3]) }, Slate);
            // (dormers on the long sides: a little window box out of the slope)
            foreach (var (s, alongX) in new[] { (z1, true), (z0, true) })
                for (var k = 1; k <= 3; k++)
                {
                    var c = x0 + (x1 - x0) * k / 4f;
                    var inZ = s == z1 ? -inset / 2 : inset / 2;
                    m.Box(0, L(c, high + 450, s + inZ), new Vector3(420, 600, 260), Trim, Slate);
                }
        }
        else m.Up(new[] { m.P(0, L(x0, high - 5, z0)), m.P(0, L(x1, high - 5, z0)), m.P(0, L(x1, high - 5, z1)), m.P(0, L(x0, high - 5, z1)) }, Slate);
        if (corner)
        {
            // the dome: a drum, the copper bell in rings, the gilded finial
            var c = L((x0 + x1) / 2f, high, (z0 + z1) / 2f);
            var r = Math.Min(x1 - x0, z1 - z0) * 0.36f;
            var rings = new (float Y, float R)[] { (0, r), (300, r), (500, r * 0.98f), (900, r * 0.85f), (1250, r * 0.55f), (1450, r * 0.2f) };
            var loops = rings.Select(q => m.Loop(0, c + new Vector3(0, q.Y, 0), new Vector3(q.R, 0, 0), new Vector3(0, 0, q.R), 8)).ToArray();
            for (var k = 0; k + 1 < loops.Length; k++) m.Skin(loops[k], loops[k + 1], _ => k == 0 ? Trim : Copper, c + new Vector3(0, (rings[k].Y + rings[k + 1].Y) / 2, 0));
            m.Cap(loops[^1], m.P(0, c + new Vector3(0, 1550, 0)), Copper, c + new Vector3(0, 1300, 0));
            m.Strut(0, c + new Vector3(0, 1500, 0), c + new Vector3(0, 2000, 0), 30, Gold);
            m.Ball(0, c + new Vector3(0, 2050, 0), 70, Gold);
            // IMPERIAL HOTEL in gold on a dark board over the south door (its canopy the island's, at 1480-1920 up)
            if (outward.Contains('S'))
            {
                const float Px = 34;
                float w = HotelProps.TextColumns("IMPERIAL HOTEL") * Px / 2 + 80, cx = (x0 + x1) / 2f, y = Math.Min(floor2 - 150, 2600);
                Quad(L(cx - w, y - 180, z1 + 40), L(cx + w, y - 180, z1 + 40), L(cx + w, y + 180, z1 + 40), L(cx - w, y + 180, z1 + 40), Board, unlit: true);
                HotelProps.TextOn(m, "IMPERIAL HOTEL", Px, (col, row) => L(cx - w + 80 + col * Px, y + 85 - row * Px, z1 + 50), Letters, L(cx, y, z1 + 600));
            }
        }
        var built = m.ToBody(new byte[96]);
        built.Static = true;
        return built.Write();
    }
}
