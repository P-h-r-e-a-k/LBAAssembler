using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// Clothes folded on the chair by Twinsen's bed (the user, 2026-11-17: "Let's have his racing suit folded neatly on the chair to the left of the
// bed. Using action on the racing suit swaps Twinsen back into it and his pyjamas take their place on the chair"): a neat stack lying on the
// seat, its foot at 0 --
//   - FoldedSuit: the racing suit in the suit's colour (CitadelOveralls.SuitLit), its white stripes and belt showing along the folds;
//   - FoldedPyjamas: the striped light-blue nightgown, the nightcap folded on top with its white pompom.
internal static partial class CitadelProps
{
    public const int FoldedSuit = 187, FoldedPyjamas = 188;
    // (each body drawn this far back from its actor's place, -x and -z: none -- the actor on the seat, in the chair's own cell, so that
    // Twinsen standing in front of the chair is drawn over the clothes (the user, 2026-11-19); the chair's bricks are kept from being
    // redrawn over them by the race file's no_recover=, RaceCarEngineFile. It stood a cell out, 280 and 300, before: drawn over him)
    public const int FoldedShiftX = 0, FoldedShiftZ = 0;
    private static readonly Vector3 Shift = new(-FoldedShiftX, 0, -FoldedShiftZ);

    private static void FoldedStack(CarMesh m, int cloth, int edge)
    {
        // three layers, each a little smaller than the one under it, the edge colour along each fold's front
        const float W = 190, D = 150, H = 34;
        for (var k = 0; k < 3; k++)
        {
            float w = W - k * 14, d = D - k * 10, y = H / 2 + k * H;
            m.Box(0, Shift + new Vector3(0, y, 0), new Vector3(2 * w, H - 4, 2 * d), cloth, cloth);
            m.Box(0, Shift + new Vector3(0, y, d + 2), new Vector3(2 * w - 20, H - 12, 6), edge, edge);
        }
    }

    public static Body BuildFoldedSuit(byte[] header)
    {
        var m = new CarMesh("the racing suit, folded", new[] { -1 });
        m.Light = 0.85f;
        FoldedStack(m, CitadelOveralls.SuitLit, CitadelPyjamas.White);
        // (its belt's buckle on top)
        m.Box(0, Shift + new Vector3(0, 3 * 34 + 4, 0), new Vector3(220, 8, 40), CitadelPyjamas.White, CitadelPyjamas.White);
        return m.ToBody(header);
    }

    public static Body BuildFoldedPyjamas(byte[] header)
    {
        var m = new CarMesh("the pyjamas, folded", new[] { -1 });
        m.Light = 0.85f;
        FoldedStack(m, CitadelPyjamas.Gown, CitadelPyjamas.Stripe);
        // (the nightcap on top: a long cone lying along the stack, its pompom off the end)
        var top = 3 * 34f;
        var ring = m.Loop(0, Shift + new Vector3(-110, top + 40, 0), new Vector3(0, 40, 0), new Vector3(0, 0, 60), 8);
        var tip = m.P(0, Shift + new Vector3(150, top + 18, 30));
        m.Cap(ring, tip, CitadelPyjamas.Gown, Shift + new Vector3(0, top + 40, 0));
        m.Cap(ring, m.P(0, Shift + new Vector3(-130, top + 40, 0)), CitadelPyjamas.Stripe, Shift + new Vector3(0, top + 40, 0));
        m.Ball(0, Shift + new Vector3(175, top + 18, 34), 30, CitadelPyjamas.White);
        return m.ToBody(header);
    }

    // FoldedCasual: his everyday clothes for the game's first morning (the user, 2026-11-18, "his casual clothes on the chair for him to
    // change into"): the jeans folded twice, the shirt over them, the belt rolled on top -- the everyday body's own colours (BODY.HQR 0:
    // CitadelPyjamas' Shirt 224, Jeans 194, Belt 97)
    public const int FoldedCasual = 200;
    private const int CasualShirt = 224 + 2, CasualJeans = 194, CasualJeansSeam = 176, CasualBelt = 97;
    public static Body BuildFoldedCasual(byte[] header)
    {
        var m = new CarMesh("his everyday clothes, folded", new[] { -1 });
        m.Light = 0.85f;
        const float W = 190, D = 150, H = 34;
        for (var k = 0; k < 3; k++)
        {
            float w = W - k * 14, d = D - k * 10, y = H / 2 + k * H;
            var (cloth, edge) = k < 2 ? (CasualJeans, CasualJeansSeam) : (CasualShirt, CitadelPyjamas.White);
            m.Box(0, Shift + new Vector3(0, y, 0), new Vector3(2 * w, H - 4, 2 * d), cloth, cloth);
            m.Box(0, Shift + new Vector3(0, y, d + 2), new Vector3(2 * w - 20, H - 12, 6), edge, edge);
        }
        m.Box(0, Shift + new Vector3(60, 3 * H + 10, 20), new Vector3(90, 20, 90), CasualBelt, CasualBelt);
        return m.ToBody(header);
    }

    public static IEnumerable<(int Generic, string Name, Func<byte[], Body> Build)> FoldedBodies() =>
        new (int, string, Func<byte[], Body>)[] { (FoldedSuit, "the racing suit, folded", BuildFoldedSuit), (FoldedPyjamas, "the pyjamas, folded", BuildFoldedPyjamas),
            (FoldedCasual, "his everyday clothes, folded", BuildFoldedCasual) };
}
