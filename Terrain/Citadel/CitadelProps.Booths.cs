using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// Stella Entities' private booths (the user, 2026-11-17: "Some smaller booths, perhaps just one side, with curtains drawn that we can't see
// into" + "we should add a small waste paper bin that's overflowing with tissue to each booth"):
//   - BoothDrape: a curtain drawn right across a booth's way in -- deep red velvet in pleats from a brass rail to the floor, 1,536 across
//     (the three cells of a booth's opening); its own +z out of the booth, its width along x (the actor turned to face the corridor)
//   - TissueBin: a small round bin, crumpled tissues heaped over its rim and a couple dropped by it
internal static partial class CitadelProps
{
    public const int TissueBin = 183, BoothDrape = 184;
    public const float DrapeHalf = 768, DrapeHigh = 1900;

    public static Body BuildBoothDrape(byte[] header)
    {
        var m = new CarMesh("a booth's drawn curtain", new[] { -1 });
        m.Light = 0.7f;
        const int Pleats = 12;
        const float Deep = 45;
        // (pleats: the cloth zig-zags in and out along its width, each fold a pair of panels, alternately lit and shaded)
        for (var k = 0; k < Pleats; k++)
        {
            float x0 = -DrapeHalf + 2 * DrapeHalf * k / Pleats, x1 = x0 + 2 * DrapeHalf / Pleats, xm = (x0 + x1) / 2;
            m.Plate(0, ClubVelvet, new(x0, 0, 0), new(xm, 0, Deep), new(xm, DrapeHigh, Deep), new(x0, DrapeHigh, 0));
            m.Plate(0, ClubVelvet + 1, new(xm, 0, Deep), new(x1, 0, 0), new(x1, DrapeHigh, 0), new(xm, DrapeHigh, Deep));
        }
        // the rail along its top, brass
        m.Box(0, new Vector3(0, DrapeHigh + 25, Deep / 2), new Vector3(2 * DrapeHalf + 60, 40, 40), ClubBrass, ClubBrass);
        return m.ToBody(header);
    }

    public static Body BuildTissueBin(byte[] header)
    {
        var m = new CarMesh("a waste paper bin, overflowing", new[] { -1 });
        m.Light = 0.85f;
        const float R = 150, High = 300;
        const int Tissue = 47, TissueShade = 45;
        // the bin: an eight-sided tub, dark steel
        var low = m.Loop(0, new Vector3(0, 2, 0), new Vector3(R * 0.85f, 0, 0), new Vector3(0, 0, R * 0.85f), 8);
        var top = m.Loop(0, new Vector3(0, High, 0), new Vector3(R, 0, 0), new Vector3(0, 0, R), 8);
        for (var k = 0; k < 8; k++)
            m.Out(new[] { low[k], low[(k + 1) % 8], top[(k + 1) % 8], top[k] }, ClubSteel, new Vector3(0, High / 2, 0), true);
        // the tissues: crumpled balls heaped over the rim, and two on the floor beside it
        var rnd = new Random(17);
        (float X, float Y, float Z, float S)[] balls =
        {
            (0, High + 40, 0, 70), (-70, High + 20, 40, 60), (60, High + 25, -50, 62), (40, High + 70, 50, 55), (-50, High + 75, -40, 50),
            (0, High + 110, 10, 48), (90, High + 10, 60, 50), (-95, High + 5, -30, 52), (240, 30, 90, 45), (-180, 28, 210, 42),
        };
        foreach (var (x, y, z, s) in balls)
            m.Ball(0, new Vector3(x, y, z), (int)s, rnd.Next(3) == 0 ? TissueShade : Tissue);
        return m.ToBody(header);
    }
}
