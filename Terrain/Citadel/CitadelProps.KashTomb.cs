using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// Kash's tomb in the White Leaf Desert's sea cave (scene 113, where the giant clam kept the Pearl -- RaceTrackStory.Desert, KashTomb): the
// user, 2026-11-18, "The place Twinsen currently discovers the helmet we need to redesign, let's redesign it as a tomb of a famous Twinsun
// racer Kash. Let's make this place sombre and respectful to the dead. Let's have a tomb with Kash's helmet placed on top that Twinsen can
// find". A stone tomb on a plinth, its lid a little wider, KASH in gilt letters along both long sides (the etching's 5 x 7 dots); his
// helmet lying on the lid (an actor of its own: gone once Twinsen has it); tall candlesticks either end, their flames lit.
// The body's own x along the tomb, z across it.
internal static partial class CitadelProps
{
    public const int KashTomb = 203, KashHelmet = 204, TombCandle = 205;
    public const float TombLong = 1800, TombDeep = 800, TombPlinth = 110, TombTop = 820, LidOver = 50, LidThick = 110;
    private const int TombStone = 52, TombLidStone = 50, TombPlinthStone = 49, TombGilt = 106;
    private const float KashDot = 34, KashFoot = 260;
    private static readonly Dictionary<char, string[]> KashFont = new()
    {
        ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
        ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
        ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
    };

    public static Body BuildKashTomb(byte[] header)
    {
        var m = new CarMesh("Kash's tomb", new[] { -1 });
        m.Light = 0.7f;
        // the plinth, the tomb, its lid
        m.Box(0, new Vector3(0, TombPlinth / 2, 0), new Vector3(TombLong + 160, TombPlinth, TombDeep + 160), TombPlinthStone);
        m.Box(0, new Vector3(0, (TombPlinth + TombTop - LidThick) / 2, 0), new Vector3(TombLong, TombTop - LidThick - TombPlinth, TombDeep), TombStone);
        m.Box(0, new Vector3(0, TombTop - LidThick / 2, 0), new Vector3(TombLong + 2 * LidOver, LidThick, TombDeep + 2 * LidOver), TombLidStone);
        // KASH in gilt, along both long sides (each side read left to right from in front of it)
        foreach (var side in new[] { 1, -1 })
        {
            const string text = "KASH";
            var wide = (text.Length * 6 - 1) * KashDot;
            var face = side * (TombDeep / 2 + 4);
            for (var c = 0; c < text.Length; c++)
            {
                var rows = KashFont[text[c]];
                var done = new bool[7, 5];
                for (var r = 0; r < 7; r++)
                    for (var col = 0; col < 5; col++)
                    {
                        if (rows[r][col] != '#' || done[r, col]) continue;
                        var end = col;
                        while (end + 1 < 5 && rows[r][end + 1] == '#' && !done[r, end + 1]) end++;
                        var bottom = r;
                        while (bottom + 1 < 7 && Enumerable.Range(col, end - col + 1).All(i => rows[bottom + 1][i] == '#' && !done[bottom + 1, i])) bottom++;
                        for (var rr = r; rr <= bottom; rr++) for (var i = col; i <= end; i++) done[rr, i] = true;
                        // (from in front of the +z side +x runs left to right; from in front of the -z side, -x does)
                        float a0 = -wide / 2 + (c * 6 + col) * KashDot, a1 = -wide / 2 + (c * 6 + end + 1) * KashDot;
                        float x0 = side * a0, x1 = side * a1;
                        float y1 = KashFoot + 7 * KashDot - r * KashDot, y0 = KashFoot + 7 * KashDot - (bottom + 1) * KashDot;
                        // (one-sided, facing out of the tomb: the far side's letters, double-sided, showed through its lid)
                        m.Out(new[] { m.P(0, new(x0, y0, face)), m.P(0, new(x1, y0, face)), m.P(0, new(x1, y1, face)), m.P(0, new(x0, y1, face)) }, TombGilt, new Vector3((x0 + x1) / 2, (y0 + y1) / 2, 0));
                    }
            }
        }
        return m.ToBody(header);
    }

    // Kash's helmet, lying on the tomb's lid (the body round the tomb's own place: the helmet at its middle, on the lid)
    public const float KashHelmetSize = 190;
    public static Body BuildKashHelmet(byte[] header)
    {
        var m = new CarMesh("Kash's helmet, on his tomb", new[] { -1 });
        m.Light = 0.6f;
        CitadelOveralls.HelmetInto(m, new Vector3(0, TombTop + KashHelmetSize * 0.5f, 0), KashHelmetSize);
        return m.ToBody(header);
    }

    // A tall iron candlestick: a round foot, a stem, a dish, a church candle and its flame
    public const float CandleStem = 900, CandleWax = 260;
    private const int CandleIron = 33, CandleWaxColour = 15;
    public static Body BuildTombCandle(byte[] header)
    {
        var m = new CarMesh("a candlestick by Kash's tomb", new[] { -1 });
        m.Light = 0.7f;
        Cylinder(m, Vector3.Zero, 150, 0, 40, 10, CandleIron, CandleIron);
        Cylinder(m, Vector3.Zero, 26, 40, CandleStem, 8, CandleIron);
        Cylinder(m, Vector3.Zero, 110, CandleStem, CandleStem + 30, 10, CandleIron, CandleIron);
        Cylinder(m, Vector3.Zero, 50, CandleStem + 30, CandleStem + 30 + CandleWax, 10, CandleWaxColour, CandleWaxColour);
        // the flame (the candle's own: BuildFlame's shape, on the wick)
        var at = new Vector3(0, CandleStem + 30 + CandleWax + 6, 0);
        const float tall = 110;
        var low = m.Loop(0, at + new Vector3(0, 4, 0), new Vector3(14, 0, 0), new Vector3(0, 0, 14), 8);
        var mid = m.Loop(0, at + new Vector3(0, tall * 0.35f, 0), new Vector3(24, 0, 0), new Vector3(0, 0, 24), 8);
        var tip = m.P(0, at + new Vector3(0, tall, 0));
        for (var k = 0; k < 8; k++) m.Out(new[] { low[k], low[(k + 1) % 8], mid[(k + 1) % 8], mid[k] }, FlameOuter, at + new Vector3(0, tall * 0.2f, 0), true);
        m.Cap(mid, tip, FlameOuter, at + new Vector3(0, tall * 0.5f, 0), true);
        var core = m.Loop(0, at + new Vector3(0, tall * 0.25f, 0), new Vector3(11, 0, 0), new Vector3(0, 0, 11), 6);
        m.Cap(core, m.P(0, at + new Vector3(0, tall * 0.62f, 0)), FlameCore, at + new Vector3(0, tall * 0.1f, 0), true);
        return m.ToBody(header);
    }
}
