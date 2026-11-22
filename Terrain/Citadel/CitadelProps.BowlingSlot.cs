using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The bowling alley's own fruit machines (the user, 2026-11-16: "The fruit machines we've added in the bowling alley, let's either create
// proper wheels like the machines in the casino, or just copy them directly. A new machine specific to the bowling area would be preferable
// though, but we need to replicate the behaviour so it's not just three random things spinning in the middle of the screen, they need to
// rotate in the fruit machine"): a tall cabinet in the alley's dark red, a bowling pin on its top, bulbs along its brow, buttons on a ledge
// across its front, a coin tray -- and in a brass-framed window on its front three reels, each a drum of five faces round its axle (left to right,
// the cabinet's x): 7, BELL, PIN, SKULL, CHERRY, the engine's order (RACEMOD.CPP s_fmName). The cabinet is bone 0, the reels bones 1-3,
// each turned about its axle by the engine (FruitFrame: the actor's own frames, MANUAL_INTER_FRAME) -- spinning while it plays, then stopped
// on its symbol: face k at the front when the reel's bone is turned ReelTurn(k). Its front +z.
// And the six lamps over the lanes in one body (LaneLamps, BowlingAlley): the alley's actors are near the engine's hundred.
internal static partial class CitadelProps
{
    public const int BowlSlot = 160, LaneLamps = 161;
    // (where the reels' axles are: their middles across, how high, how far out from the cabinet's front; each reel's radius and width)
    // (bigger since 2026-11-22, and turned up towards the alley's camera, which looks down on them -- the user: "what they're showing isn't
    // visible in game": the reels 118 wide, 88 round, their symbols 70 across, a few pixels from the camera, white pins and skulls on the
    // reels' light faces. Now 150 wide and 125 round, the symbols half as big again and the pale ones dark, the face shown turned ReelTilt
    // up off the front)
    public static readonly float[] ReelX = { -172, 0, 172 };
    public const float ReelY = 960, ReelZ = 270, ReelRadius = 125, ReelWide = 150, SlotDeep = 500, SlotWide = 620, SlotHigh = 1400;
    public const int ReelFaces = 5;
    private const float ReelTilt = MathF.PI / 6, SymbolScale = 1.45f;
    // (the reel's turn that shows face k at the front -- RACEMOD.CPP FmFace, which turns them)
    public static int ReelTurn(int k) => (int)Math.Round(k * 4096.0 / ReelFaces) & 4095;
    // (the lit ones ramp starts low enough that the game's light -- up to nine steps on -- stays in their ramp: high in the grey one, the
    // reels' faces and the pin ran on into the reds after it)
    private const int CabinetRed = 4 * 16 + 3, CabinetTrim = 3 * 16 + 6, WindowBlack = 3 * 16 + 0, Brass = 6 * 16 + 5, ReelFace = 3 * 16 + 6,
        SymbolRed = 4 * 16 + 11, SymbolGold = 6 * 16 + 12, SymbolWhite = 3 * 16 + 15, SymbolDark = 3 * 16 + 1, SymbolGreen = 7 * 16 + 9,
        BulbYellow = 6 * 16 + 15, PinWhite = 3 * 16 + 6, PinStripe = 4 * 16 + 4, LeverKnob = 4 * 16 + 12;

    public static Body BuildBowlSlot(byte[] header)
    {
        var m = new CarMesh("the alley's fruit machine", new[] { -1, 0, 0, 0 });
        m.Light = 0.75f;
        float front = SlotDeep / 2;
        // the cabinet: its body, a darker plinth, a chrome cap
        m.Box(0, new Vector3(0, SlotHigh / 2, 0), new Vector3(SlotWide, SlotHigh, SlotDeep), CabinetRed, top: CabinetTrim);
        m.Box(0, new Vector3(0, 60, 10), new Vector3(SlotWide + 40, 120, SlotDeep + 20), WindowBlack, top: CabinetTrim);
        // the reels' window: black behind them, a brass frame round it
        float wx = 258, wy0 = ReelY - 170, wy1 = ReelY + 170;
        m.Box(0, new Vector3(0, ReelY, front + 6), new Vector3(2 * wx, wy1 - wy0, 12), WindowBlack);
        m.Box(0, new Vector3(0, wy1 + 22, front + 18), new Vector3(2 * wx + 88, 44, 36), Brass, top: Brass);
        m.Box(0, new Vector3(0, wy0 - 22, front + 18), new Vector3(2 * wx + 88, 44, 36), Brass, top: Brass);
        foreach (var s in new[] { -1f, 1f }) m.Box(0, new Vector3(s * (wx + 22), ReelY, front + 18), new Vector3(44, wy1 - wy0, 36), Brass, top: Brass);
        // the coin tray, and a slot over it
        m.Box(0, new Vector3(0, 330, front + 40), new Vector3(320, 50, 80), CabinetTrim, top: CabinetTrim);
        m.Box(0, new Vector3(170, 560, front + 4), new Vector3(70, 20, 8), WindowBlack);
        // its buttons on a ledge across its front under the reels -- a big red one to spin, two small lit ones (the user, 2026-11-22: a lever
        // at a machine's side "would be inaccessible in all the other machines given how close they are": they stand side by side, and the
        // game plays one by Action at its front)
        m.Box(0, new Vector3(0, 690, front + 50), new Vector3(SlotWide - 60, 40, 100), CabinetTrim, top: CabinetTrim);
        m.Ball(0, new Vector3(150, 728, front + 62), 40, LeverKnob);
        m.Ball(0, new Vector3(-150, 720, front + 62), 24, BulbYellow);
        m.Ball(0, new Vector3(-60, 720, front + 62), 24, BulbYellow);
        // bulbs along its brow
        for (var bx = -240f; bx <= 241; bx += 80) m.Ball(0, new Vector3(bx, SlotHigh - 60, front + 10), 22, BulbYellow);
        // the bowling pin on its top: white, two red stripes round its neck
        var pinBase = SlotHigh;
        var rings = new (float Y, float R, int Colour)[] { (0, 70, PinWhite), (90, 110, PinWhite), (200, 120, PinWhite), (300, 85, PinWhite), (360, 55, PinStripe), (400, 50, PinWhite), (440, 52, PinStripe), (480, 62, PinWhite), (560, 72, PinWhite), (640, 45, PinWhite) };
        var loops = rings.Select(r => m.Loop(0, new Vector3(0, pinBase + r.Y, 0), new Vector3(r.R, 0, 0), new Vector3(0, 0, r.R), 10)).ToArray();
        for (var k = 0; k + 1 < loops.Length; k++) m.Skin(loops[k], loops[k + 1], _ => rings[k].Colour, new Vector3(0, pinBase + (rings[k].Y + rings[k + 1].Y) / 2, 0));
        m.Cap(loops[^1], m.P(0, new Vector3(0, pinBase + 680, 0)), PinWhite, new Vector3(0, pinBase + 600, 0));
        // the reels
        for (var r = 0; r < 3; r++) Reel(m, 1 + r, new Vector3(ReelX[r], ReelY, front + ReelZ - SlotDeep / 2 + 2));
        return m.ToBody(header);
    }

    // A reel: a drum of ReelFaces faces round its axle (the bone's x), its pivot the axle's middle; face k centred at the angle k turns of
    // a fifth round from the front (+z) upwards -- its symbol on it
    private static void Reel(CarMesh m, int bone, Vector3 at)
    {
        // (its pivot a point of its parent's, the cabinet's: a body's bone turns about one of its parent's points)
        m.Pivot(bone, m.P(0, at));
        float half = ReelWide / 2, step = MathF.Tau / ReelFaces;
        Vector3 Round(float x, float angle, float radius) => at + new Vector3(x, radius * MathF.Sin(angle), radius * MathF.Cos(angle));
        for (var k = 0; k < ReelFaces; k++)
        {
            float a0 = ReelTilt + k * step - step / 2, a1 = ReelTilt + k * step + step / 2;
            var face = new[] { m.P(bone, Round(-half, a0, ReelRadius)), m.P(bone, Round(half, a0, ReelRadius)), m.P(bone, Round(half, a1, ReelRadius)), m.P(bone, Round(-half, a1, ReelRadius)) };
            m.Out(face, ReelFace, at);
            Symbol(m, bone, at, k, ReelTilt + k * step);
        }
        // its ends
        foreach (var s in new[] { -1f, 1f })
        {
            var end = Enumerable.Range(0, ReelFaces).Select(k => m.P(bone, Round(s * half, ReelTilt + k * step - step / 2, ReelRadius))).ToArray();
            var c = m.P(bone, at + new Vector3(s * half, 0, 0));
            for (var k = 0; k < ReelFaces; k++) m.Out(new[] { c, end[k], end[(k + 1) % ReelFaces] }, WindowBlack, at - new Vector3(s * half * 2, 0, 0));
        }
    }

    // A symbol on face k of a reel (its middle at `angle` round the axle): drawn in the face's plane a little out from it, (u, v) its own
    // across (the axle's x) and up (round the drum)
    private static void Symbol(CarMesh m, int bone, Vector3 at, int k, float angle)
    {
        var apothem = ReelRadius * MathF.Cos(MathF.PI / ReelFaces) + 2;
        var normal = new Vector3(0, MathF.Sin(angle), MathF.Cos(angle));
        var up = new Vector3(0, MathF.Cos(angle), -MathF.Sin(angle));
        Vector3 On(float u, float v) => at + normal * apothem + new Vector3(u * SymbolScale, 0, 0) + up * (v * SymbolScale);
        void Quad(float u0, float v0, float u1, float v1, int colour) => m.Out(new[] { m.P(bone, On(u0, v0)), m.P(bone, On(u1, v0)), m.P(bone, On(u1, v1)), m.P(bone, On(u0, v1)) }, colour, at, unlit: true);
        // (a polygon is a triangle or a quad: a wider one -- convex -- a fan of triangles from its first point)
        void Poly(int colour, params (float U, float V)[] pts)
        {
            var hs = pts.Select(p => m.P(bone, On(p.U, p.V))).ToArray();
            if (hs.Length <= 4) { m.Out(hs, colour, at, unlit: true); return; }
            for (var k = 1; k + 1 < hs.Length; k++) m.Out(new[] { hs[0], hs[k], hs[k + 1] }, colour, at, unlit: true);
        }
        switch (k)
        {
            case 0: // a red 7: its bar and its stroke down to the left
                Quad(-34, 26, 34, 40, SymbolRed);
                Poly(SymbolRed, (20, 26), (34, 26), (-6, -40), (-20, -40));
                break;
            case 1: // a gold bell: its crown, its flared skirt, its clapper
                Quad(-8, 34, 8, 42, SymbolGold);
                Poly(SymbolGold, (-16, 34), (16, 34), (26, -20), (-26, -20));
                Quad(-36, -30, 36, -20, SymbolGold);
                Quad(-7, -40, 7, -30, SymbolDark);
                break;
            case 2: // a dark bowling pin (dark on the light reel, 2026-11-22): its head, neck (a red stripe) and belly
                Poly(SymbolDark, (-10, 30), (10, 30), (12, 42), (-12, 42));
                Quad(-8, 18, 8, 30, SymbolRed);
                Poly(SymbolDark, (-8, 18), (8, 18), (20, -10), (14, -40), (-14, -40), (-20, -10));
                break;
            case 3: // a dark skull: its dome and jaw, two light eyes, a light nose
                Poly(SymbolDark, (-26, 0), (-20, 30), (0, 40), (20, 30), (26, 0), (16, -18), (-16, -18));
                Quad(-12, -36, 12, -18, SymbolDark);
                Quad(-18, 2, -6, 14, SymbolWhite);
                Quad(6, 2, 18, 14, SymbolWhite);
                Poly(SymbolWhite, (-4, -10), (4, -10), (0, -2));
                break;
            default: // two red cherries on green stalks
                Poly(SymbolRed, (-30, -14), (-22, -6), (-12, -14), (-12, -26), (-22, -34), (-30, -26));
                Poly(SymbolRed, (12, -20), (20, -12), (30, -20), (30, -32), (20, -40), (12, -32));
                Poly(SymbolGreen, (-22, -6), (-18, -6), (4, 36), (0, 38));
                Poly(SymbolGreen, (20, -12), (24, -12), (6, 36), (2, 36));
                break;
        }
    }

    // The lamps over the lanes, in one body (the alley's actors near the engine's hundred): six hanging lamps, three across (the lanes'
    // left, middle and right: LaneX0, LaneMiddleX, LaneX1) and two along (z 9216, 14336), round the body's origin at (LaneMiddleX, 11776)
    public static readonly (float X, float Z)[] LaneLampSpots = { (LaneX0 - LaneMiddleX, -2560), (0, -2560), (LaneX1 - LaneMiddleX, -2560), (LaneX0 - LaneMiddleX, 2560), (0, 2560), (LaneX1 - LaneMiddleX, 2560) };
    public static Body BuildLaneLamps(byte[] header)
    {
        var m = new CarMesh("the lamps over the lanes", new[] { -1 });
        m.Light = 0.6f;
        foreach (var (x, z) in LaneLampSpots) HangingLampAt(m, new Vector3(x, 0, z));
        return m.ToBody(header);
    }
}
