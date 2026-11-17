using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// Stella Entities' dressing room behind the catwalk's door (StellaRooms.Backstage -- the user, 2026-11-16: "The doorway at the end of the
// catwalk needs to go into an area where the performers get changed, it currently connects to the office. It doesn't appear that we've
// created a back stage changing area yet so we'll have to do so"): its furniture, each body standing on the floor at its origin, its front
// towards +z (an actor's beta 0 looks along +z):
//   - a dressing table (DressingTable): a long top on its legs, three tall mirrors along its back each ringed with bulbs, make-up and
//     bottles before them;
//   - a costume rail (CostumeRail): two uprights on feet, a bar, the performers' costumes hanging from it, sequins and feathers;
//   - a folding screen (FoldingScreen): three panels zig-zag, red velvet in gilt frames, a garment thrown over its top;
//   - a row of lockers (Lockers): four steel doors, vents and handles.
internal static partial class CitadelProps
{
    public const int DressingTable = 162, CostumeRail = 163, FoldingScreen = 164, Lockers = 165;
    private const int DressWood = 1 * 16 + 4, DressWoodDark = 1 * 16 + 1, MirrorGlass = 3 * 16 + 3, Frame = 6 * 16 + 6, MirrorBulb = 6 * 16 + 15,
        Velvet = 4 * 16 + 4, LockerSteel = 3 * 16 + 8, LockerDark = 3 * 16 + 3;
    // (the costumes' colours: a red, a pink, the gold, a teal, a purple, a white)
    private static readonly int[] CostumeColours = { 4 * 16 + 8, 4 * 16 + 13, 6 * 16 + 10, 10 * 16 + 8, 13 * 16 + 6, 0 * 16 + 14 };

    // (its half length; and its box the scene's way round, the actor turned 1024 against the west wall -- the body's own square box, the
    // average of its length and depth, reached a cell out into the room)
    public const float DressTableHalf = 1700;
    // (and the room's other furniture the same: each one's own footprint, the scene's way round as each stands -- the costume rail along x
    // (beta 0), the folding screen's panels along z (3072), the lockers along x (0). The bodies' square boxes, as deep as they were long,
    // took in the performer standing before the rail: the sort drew its upright over her legs -- the user, 2026-11-17, "the latest stripper
    // is still partly transparent")
    private static short[]? DressingBox(int generic) => generic switch
    {
        DressingTable => new short[] { -300, 0, -(short)(DressTableHalf + 20), 300, 1600, (short)(DressTableHalf + 20) },
        CostumeRail => new short[] { -690, 0, -220, 690, 1550, 220 },
        FoldingScreen => new short[] { -200, 0, -800, 200, 1600, 800 },
        Lockers => new short[] { -820, 0, -250, 820, 1800, 250 },
        _ => null,
    };

    public static Body BuildDressingTable(byte[] header)
    {
        var m = new CarMesh("the dressing table", new[] { -1 });
        m.Light = 0.75f;
        const float W = DressTableHalf, D = 280, Top = 720, Slab = 50;
        // the top and its legs
        m.Box(0, new Vector3(0, Top - Slab / 2, 0), new Vector3(2 * W, Slab, 2 * D), DressWood, DressWood + 2);
        foreach (var sx in new[] { -1, 1 })
            foreach (var sz in new[] { -1, 1 })
                m.Box(0, new Vector3(sx * (W - 50), (Top - Slab) / 2, sz * (D - 50)), new Vector3(60, Top - Slab, 60), DressWoodDark);
        // (a drawer front under the top, its handle)
        m.Box(0, new Vector3(0, Top - Slab - 70, D - 20), new Vector3(2 * W - 200, 140, 30), DressWoodDark, DressWoodDark);
        // the four mirrors along its back: a frame, the glass, bulbs round the glass's top and sides (four since 2026-11-17, the table longer --
        // the user: "We can probably expend the table and have a fourth mirror")
        const float MirrorW = 330, MirrorH = 820, Back = -D + 60;
        foreach (var mx in new[] { -3 * W / 4, -W / 4, W / 4, 3 * W / 4 })
        {
            m.Box(0, new Vector3(mx, Top + MirrorH / 2, Back - 10), new Vector3(2 * MirrorW + 60, MirrorH + 60, 30), Frame, Frame);
            // (the glass facing forward, +z: away from a point behind it -- Up turned a standing quad either way)
            m.Out(new[] { m.P(0, new Vector3(mx - MirrorW, Top + 30, Back + 8)), m.P(0, new Vector3(mx + MirrorW, Top + 30, Back + 8)),
                          m.P(0, new Vector3(mx + MirrorW, Top + MirrorH - 30, Back + 8)), m.P(0, new Vector3(mx - MirrorW, Top + MirrorH - 30, Back + 8)) },
                  MirrorGlass, new Vector3(mx, Top + MirrorH / 2, Back - 200));
            for (var k = 0; k <= 4; k++)
            {
                var y = Top + 80 + k * (MirrorH - 160) / 4f;
                m.Ball(0, new Vector3(mx - MirrorW - 10, y, Back + 30), 30, MirrorBulb);
                m.Ball(0, new Vector3(mx + MirrorW + 10, y, Back + 30), 30, MirrorBulb);
            }
            for (var k = 0; k <= 4; k++)
                m.Ball(0, new Vector3(mx - MirrorW + k * MirrorW / 2, Top + MirrorH + 10, Back + 30), 30, MirrorBulb);
            // before each mirror: a powder box, a lipstick, a bottle of scent
            m.Box(0, new Vector3(mx - 160, Top + 30, 40), new Vector3(150, 60, 120), CostumeColours[1], CostumeColours[1] + 2);
            m.Box(0, new Vector3(mx + 40, Top + 50, 80), new Vector3(30, 100, 30), CostumeColours[0]);
            m.Box(0, new Vector3(mx + 180, Top + 70, 20), new Vector3(70, 140, 70), MirrorGlass, MirrorGlass);
            m.Ball(0, new Vector3(mx + 180, Top + 160, 20), 28, Frame);
        }
        return m.ToBody(header);
    }

    public static Body BuildCostumeRail(byte[] header)
    {
        var m = new CarMesh("the costume rail", new[] { -1 });
        m.Light = 0.8f;
        const float W = 650, High = 1500, Bar = 40;
        // the uprights on their feet, the bar along their tops
        foreach (var sx in new[] { -1, 1 })
        {
            m.Box(0, new Vector3(sx * W, High / 2, 0), new Vector3(Bar, High, Bar), LockerSteel);
            m.Box(0, new Vector3(sx * W, 20, 0), new Vector3(Bar, 40, 420), LockerSteel);
        }
        m.Box(0, new Vector3(0, High, 0), new Vector3(2 * W + Bar, Bar, Bar), LockerSteel, LockerSteel);
        // the costumes: each a short dress from a hanger, its skirt flaring, a gold trim at the hem -- side by side, each turned a little
        var colours = CostumeColours;
        for (var k = 0; k < 6; k++)
        {
            var x = -W + 170 + k * (2 * W - 340) / 5f;
            var z = (k % 2 == 0 ? 1 : -1) * 30;
            var c = colours[k];
            float neck = High - 120, waist = High - 470, hem = High - 900;
            m.Rod(0, new Vector3(x, High, z), new Vector3(x, neck + 40, z), Frame);
            m.Rod(0, new Vector3(x - 110, neck, z), new Vector3(x + 110, neck, z), Frame);
            // bodice and skirt, both sides
            m.Plate(0, c, new Vector3(x - 100, neck, z), new Vector3(x + 100, neck, z), new Vector3(x + 80, waist, z), new Vector3(x - 80, waist, z));
            m.Plate(0, c + 1, new Vector3(x - 80, waist, z), new Vector3(x + 80, waist, z), new Vector3(x + 170, hem, z), new Vector3(x - 170, hem, z));
            m.Plate(0, Frame, new Vector3(x - 170, hem, z + 1), new Vector3(x + 170, hem, z + 1), new Vector3(x + 175, hem + 40, z + 1), new Vector3(x - 175, hem + 40, z + 1));
        }
        // a feather boa thrown over the bar
        for (var k = 0; k < 8; k++) m.Ball(0, new Vector3(-W + 100 + k * 90, High - 30 - (k % 2) * 30, 50), 45, colours[1] + 3);
        return m.ToBody(header);
    }

    public static Body BuildFoldingScreen(byte[] header)
    {
        var m = new CarMesh("the folding screen", new[] { -1 });
        m.Light = 0.8f;
        const float Panel = 520, High = 1600, Zig = 160;
        // three panels zig-zag along x: each a gilt frame round red velvet, both faces
        var xs = new[] { -1.5f * Panel, -0.5f * Panel, 0.5f * Panel, 1.5f * Panel };
        var zs = new[] { 0f, Zig, 0f, Zig };
        for (var k = 0; k < 3; k++)
        {
            Vector3 A(float y) => new(xs[k], y, zs[k]); Vector3 B(float y) => new(xs[k + 1], y, zs[k + 1]);
            m.Plate(0, Velvet, A(80), B(80), B(High - 60), A(High - 60));
            m.Strut(0, A(0), A(High), 40, Frame);
            m.Rod(0, A(High), B(High), Frame);
            m.Rod(0, A(60), B(60), Frame);
        }
        m.Strut(0, new Vector3(xs[3], 0, zs[3]), new Vector3(xs[3], High, zs[3]), 40, Frame);
        // a stocking and a slip thrown over its top
        m.Plate(0, CostumeColours[5], new Vector3(xs[1] - 60, High, zs[1]), new Vector3(xs[1] + 140, High, zs[1] + 20), new Vector3(xs[1] + 120, High - 380, zs[1] + 30), new Vector3(xs[1] - 40, High - 420, zs[1] + 10));
        m.Plate(0, CostumeColours[0], new Vector3(xs[2] - 120, High, zs[2]), new Vector3(xs[2] + 40, High, zs[2] - 10), new Vector3(xs[2] + 20, High - 300, zs[2] - 20), new Vector3(xs[2] - 100, High - 280, zs[2]));
        return m.ToBody(header);
    }

    public static Body BuildLockers(byte[] header)
    {
        var m = new CarMesh("the lockers", new[] { -1 });
        m.Light = 0.75f;
        const float Each = 400, High = 1800, D = 220;
        var w = 2 * Each;
        // the cabinet, its top a shade lighter
        m.Box(0, new Vector3(0, High / 2, 0), new Vector3(2 * w, High, 2 * D), LockerDark, LockerSteel + 2);
        // four doors on its front, each with vents and a handle, a name plate
        for (var k = 0; k < 4; k++)
        {
            var x = -w + Each / 2 + k * Each;
            m.Box(0, new Vector3(x, High / 2, D + 6), new Vector3(Each - 30, High - 60, 12), LockerSteel, LockerSteel);
            for (var v = 0; v < 4; v++) m.Box(0, new Vector3(x, High - 220 - v * 50, D + 14), new Vector3(Each - 140, 14, 6), LockerDark, LockerDark);
            m.Box(0, new Vector3(x + Each / 2 - 70, High / 2, D + 20), new Vector3(24, 110, 16), Frame, Frame);
            m.Box(0, new Vector3(x, High - 110, D + 14), new Vector3(150, 50, 6), CostumeColours[5], CostumeColours[5]);
        }
        return m.ToBody(header);
    }
}
