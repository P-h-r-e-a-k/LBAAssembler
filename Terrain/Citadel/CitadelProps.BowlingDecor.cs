using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The bowling alley's dressing, its seedy dark (the user, 2026-11-15: "make the whole place dark and seedy. Feel free to do some research for
// dark seedy places" -- dive bars: dim lamps over the lanes, neon that flickers because nobody pays to fix it, sticky worn floors): a neon
// bowling pin and ball over the lanes, a neon BAR over the bar, dim lamps hung over the lanes, a scoreboard over each played lane's pins,
// a ball return trough for each lane's Sphero to wait in, kegs and crates.
internal static partial class CitadelProps
{
    public const int NeonPin = 35, NeonBar = 36, HangingLamp = 37, Scoreboard = 38, Keg = 39, Crate = 58, BallReturn = 59;
    // (the palette's ramps, 16 shades each: 6 gold-yellow, 4 red, 10 cyan, 3 grey, 1 brown, 7 olive)
    private const int NeonYellow = 6 * 16 + 14, NeonPink = 4 * 16 + 13, NeonCyan = 10 * 16 + 13, PanelBlack = 3 * 16 + 1, DigitRed = 4 * 16 + 12,
        ShadeGreen = 7 * 16 + 4, Bulb = 6 * 16 + 15, KegWood = 1 * 16 + 6, KegBand = 3 * 16 + 6, CrateWood = 1 * 16 + 8, TroughDark = 3 * 16 + 2, Chrome = 3 * 16 + 9;

    // A glowing tube from a to b (unlit: a neon's light its own)
    private static void Tube(CarMesh m, Vector3 a, Vector3 b, float t, int colour)
    {
        var d = Vector3.Normalize(b - a);
        var u = Vector3.Normalize(Vector3.Cross(d, Math.Abs(d.Z) > 0.9f ? Vector3.UnitX : Vector3.UnitZ)) * t / 2; var v = Vector3.Normalize(Vector3.Cross(d, u)) * t / 2;
        var corners = new[] { u + v, u - v, -u - v, -u + v };
        var ea = corners.Select(c => m.P(0, a + c)).ToArray(); var eb = corners.Select(c => m.P(0, b + c)).ToArray();
        for (var k = 0; k < 4; k++) m.Out(new[] { ea[k], ea[(k + 1) % 4], eb[(k + 1) % 4], eb[k] }, colour, (a + b) / 2, unlit: true);
    }
    private static void Polyline(CarMesh m, float t, int colour, params Vector2[] at)
    {
        for (var k = 0; k + 1 < at.Length; k++) Tube(m, new Vector3(at[k], 0), new Vector3(at[k + 1], 0), t, colour);
    }

    // The neon over the lanes, flat on the back wall (facing +z, the room): a bowling pin's outline in yellow, a ball beside it in pink,
    // a broken tube of red under them
    public static Body BuildNeonPin(byte[] header)
    {
        var m = new CarMesh("the alley's neon pin", new[] { -1 });
        m.Light = 1;
        // (a dark backing board, a little off the wall)
        m.Box(0, new Vector3(0, 700, -30), new Vector3(2200, 1500, 30), PanelBlack);
        var half = new[] { new Vector2(0, 0), new Vector2(170, 0), new Vector2(230, 300), new Vector2(200, 560), new Vector2(95, 820), new Vector2(150, 1010), new Vector2(110, 1180), new Vector2(0, 1240) };
        Polyline(m, 34, NeonYellow, half.Select(p => new Vector2(p.X - 450, p.Y + 80)).ToArray());
        Polyline(m, 34, NeonYellow, half.Select(p => new Vector2(-p.X - 450, p.Y + 80)).ToArray());
        var ring = Enumerable.Range(0, 17).Select(k => new Vector2(380 + 300 * MathF.Cos(k * MathF.PI / 8), 420 + 300 * MathF.Sin(k * MathF.PI / 8))).ToArray();
        Polyline(m, 34, NeonPink, ring);
        foreach (var (x, y) in new[] { (300, 470), (460, 520), (390, 330) }) m.Ball(0, new Vector3(x, y, 10), 30, NeonPink);
        Polyline(m, 30, NeonPink, new Vector2(-1000, 30), new Vector2(-300, 30));
        Polyline(m, 30, NeonPink, new Vector2(-180, 30), new Vector2(150, 30));
        Polyline(m, 30, NeonPink, new Vector2(330, 30), new Vector2(1000, 30));
        return m.ToBody(header);
    }

    // The neon over the bar, hung from the dark: BAR in cyan block letters and a cocktail glass in pink, facing +z
    public static Body BuildNeonBar(byte[] header)
    {
        var m = new CarMesh("the neon BAR", new[] { -1 });
        m.Light = 1;
        m.Box(0, new Vector3(150, 300, -30), new Vector3(1800, 760, 30), PanelBlack);
        foreach (var x in new[] { -400f, 700f }) Tube(m, new Vector3(x, 680, -30), new Vector3(x, 2400, -30), 14, PanelBlack);
        const float W = 280, H = 460, T = 38;
        // B
        var b = -620f;
        Polyline(m, T, NeonCyan, new Vector2(b, 70), new Vector2(b, 70 + H), new Vector2(b + W * 0.8f, 70 + H), new Vector2(b + W, 70 + H * 0.8f), new Vector2(b + W * 0.8f, 70 + H * 0.52f), new Vector2(b, 70 + H * 0.52f));
        Polyline(m, T, NeonCyan, new Vector2(b + W * 0.8f, 70 + H * 0.52f), new Vector2(b + W, 70 + H * 0.26f), new Vector2(b + W * 0.8f, 70), new Vector2(b, 70));
        // A
        var a = -250f;
        Polyline(m, T, NeonCyan, new Vector2(a, 70), new Vector2(a + W / 2, 70 + H), new Vector2(a + W, 70));
        Polyline(m, T, NeonCyan, new Vector2(a + W * 0.22f, 70 + H * 0.4f), new Vector2(a + W * 0.78f, 70 + H * 0.4f));
        // R
        var r = 120f;
        Polyline(m, T, NeonCyan, new Vector2(r, 70), new Vector2(r, 70 + H), new Vector2(r + W * 0.8f, 70 + H), new Vector2(r + W, 70 + H * 0.78f), new Vector2(r + W * 0.8f, 70 + H * 0.52f), new Vector2(r, 70 + H * 0.52f));
        Polyline(m, T, NeonCyan, new Vector2(r + W * 0.45f, 70 + H * 0.52f), new Vector2(r + W, 70));
        // the glass: a V on a stem, an olive
        Polyline(m, 32, NeonPink, new Vector2(560, 560), new Vector2(720, 330), new Vector2(880, 560), new Vector2(560, 560));
        Polyline(m, 32, NeonPink, new Vector2(720, 330), new Vector2(720, 120), new Vector2(620, 90), new Vector2(820, 90));
        m.Ball(0, new Vector3(760, 500, 10), 34, NeonYellow);
        return m.ToBody(header);
    }

    // A lamp hung over a lane: its cord up into the dark, a dented shade, a dim bulb under it
    public static Body BuildHangingLamp(byte[] header)
    {
        var m = new CarMesh("a hanging lamp", new[] { -1 });
        m.Light = 0.6f;
        HangingLampAt(m, Vector3.Zero);
        return m.ToBody(header);
    }
    private static void HangingLampAt(CarMesh m, Vector3 at)
    {
        Tube(m, at + new Vector3(0, 220, 0), at + new Vector3(0, 2600, 0), 16, PanelBlack);
        var top = m.Loop(0, at + new Vector3(0, 220, 0), new Vector3(70, 0, 0), new Vector3(0, 0, 70), 8);
        var rim = m.Loop(0, at + new Vector3(0, 40, 0), new Vector3(260, 0, 0), new Vector3(0, 0, 260), 8);
        m.Skin(top, rim, _ => ShadeGreen, at + new Vector3(0, -400, 0));
        m.Skin(rim, top, _ => ShadeGreen, at + new Vector3(0, 600, 0));
        m.Ball(0, at + new Vector3(0, 30, 0), 80, Bulb);
    }

    // A scoreboard over a played lane's pins: a black panel, its frame, the last frames in red (an X, a 9 and a spare's slash)
    public static Body BuildScoreboard(byte[] header)
    {
        var m = new CarMesh("a lane's scoreboard", new[] { -1 });
        m.Light = 0.7f;
        m.Box(0, new Vector3(0, 0, 0), new Vector3(1500, 640, 80), PanelBlack, top: Chrome);
        foreach (var x in new[] { -600f, 600f }) Tube(m, new Vector3(x, 320, 0), new Vector3(x, 2200, 0), 18, PanelBlack);
        // X
        Polyline(m, 34, DigitRed, new Vector2(-560, 180), new Vector2(-300, -180));
        Polyline(m, 34, DigitRed, new Vector2(-560, -180), new Vector2(-300, 180));
        // 9
        Polyline(m, 34, DigitRed, new Vector2(80, -180), new Vector2(80, 180), new Vector2(-120, 180), new Vector2(-120, 20), new Vector2(80, 20));
        // /
        Polyline(m, 34, DigitRed, new Vector2(300, -180), new Vector2(520, 180));
        // (its tubes on the face toward the room: brought forward)
        return m.ToBody(header);
    }

    // A keg: a squat drum of staves, two iron bands
    public static Body BuildKeg(byte[] header)
    {
        var m = new CarMesh("a keg", new[] { -1 });
        m.Light = 0.6f;
        var low = m.Loop(0, new Vector3(0, 0, 0), new Vector3(190, 0, 0), new Vector3(0, 0, 190), 10);
        var mid = m.Loop(0, new Vector3(0, 260, 0), new Vector3(230, 0, 0), new Vector3(0, 0, 230), 10);
        var high = m.Loop(0, new Vector3(0, 520, 0), new Vector3(190, 0, 0), new Vector3(0, 0, 190), 10);
        m.Skin(low, mid, k => k % 3 == 0 ? KegBand : KegWood, new Vector3(0, 260, 0));
        m.Skin(mid, high, k => k % 3 == 0 ? KegBand : KegWood, new Vector3(0, 260, 0));
        m.Cap(high, m.P(0, new Vector3(0, 520, 0)), KegWood, new Vector3(0, 0, 0));
        return m.ToBody(header);
    }

    // A crate of empties: a slatted box
    public static Body BuildCrate(byte[] header)
    {
        var m = new CarMesh("a crate", new[] { -1 });
        m.Light = 0.6f;
        m.Box(0, new Vector3(0, 220, 0), new Vector3(560, 440, 560), CrateWood, top: CrateWood - 3);
        foreach (var y in new[] { 120f, 320f }) m.Box(0, new Vector3(0, y, 0), new Vector3(580, 40, 580), CrateWood - 4);
        return m.ToBody(header);
    }

    // A lane's ball return: a low dark trough with a chrome lip, the Sphero waiting in it (RACEMOD.CPP's rack)
    public static Body BuildBallReturn(byte[] header)
    {
        var m = new CarMesh("a ball return", new[] { -1 });
        m.Light = 0.22f;
        m.Box(0, new Vector3(0, 60, 0), new Vector3(700, 120, 1100), TroughDark, top: TroughDark);
        foreach (var s in new[] { -1, 1 }) m.Box(0, new Vector3(s * 330, 150, 0), new Vector3(40, 180, 1100), Chrome);
        m.Box(0, new Vector3(0, 150, -540), new Vector3(700, 180, 40), Chrome);
        return m.ToBody(header);
    }
}
