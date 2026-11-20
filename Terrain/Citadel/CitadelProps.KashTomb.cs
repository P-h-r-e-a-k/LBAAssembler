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
    public const int KashTomb = 203, KashHelmet = 204, TombCandle = 205, RecordSlate = 207;
    public const float TombLong = 1800, TombDeep = 800, TombPlinth = 110, TombTop = 820, LidOver = 50, LidThick = 110;
    private const int TombStone = 52, TombLidStone = 50, TombPlinthStone = 49, TombGilt = 106;
    private const float KashDot = 34, KashFoot = 260;
    // (the name's middle along the lid, and the helmet's place past it at the other end)
    private const float KashTextAt = -180, KashHelmetAt = 560;
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
        // KASH in gilt on the lid's top (the user, 2026-11-19: "Let's put the name Kash on the top of the tomb instead of the side"): the
        // letters along the tomb, +x to -x in the tomb's own frame, their tops towards +z -- turned half round as it stands (KashTombBeta),
        // read left to right from the cave's camera; the helmet lies past them at the +x end (BuildKashHelmet)
        {
            const string text = "KASH";
            var wide = (text.Length * 6 - 1) * KashDot;
            var top = TombTop + 3;
            for (var c = 0; c < text.Length; c++)
            {
                var rows = KashFont[text[c]];
                var done = new bool[7, 5];
                for (var r = 0; r < 7; r++)
                    for (var col = 0; col < 5; col++)
                    {
                        if (rows[r][col] != '#' || done[r, col]) continue;
                        var end2 = col;
                        while (end2 + 1 < 5 && rows[r][end2 + 1] == '#' && !done[r, end2 + 1]) end2++;
                        var bottom = r;
                        while (bottom + 1 < 7 && Enumerable.Range(col, end2 - col + 1).All(i => rows[bottom + 1][i] == '#' && !done[bottom + 1, i])) bottom++;
                        for (var rr = r; rr <= bottom; rr++) for (var i = col; i <= end2; i++) done[rr, i] = true;
                        // (the first letter at +x, row 0 -- the letter's top -- furthest towards +z: the tomb stands a half turn round, so this
                        // is the way up the cave's camera reads)
                        float x1 = KashTextAt + wide / 2 - (c * 6 + col) * KashDot, x0 = KashTextAt + wide / 2 - (c * 6 + end2 + 1) * KashDot;
                        float z1 = 7 * KashDot / 2f - r * KashDot, z0 = 7 * KashDot / 2f - (bottom + 1) * KashDot;
                        m.Out(new[] { m.P(0, new(x0, top, z0)), m.P(0, new(x1, top, z0)), m.P(0, new(x1, top, z1)), m.P(0, new(x0, top, z1)) }, TombGilt, new Vector3((x0 + x1) / 2, top - 50, (z0 + z1) / 2));
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
        CitadelOveralls.HelmetInto(m, new Vector3(KashHelmetAt, TombTop + KashHelmetSize * 0.5f, 0), KashHelmetSize);
        return m.ToBody(header);
    }

    // A slate of the speedrun records, on the cave's wall either side of the tomb (the user, 2026-11-19: "On the wall either side of the tomb
    // let's add a slate showing best times for runners from LBA1 ... The other side will be for LBA2"): a dark slate board in a wooden frame,
    // its face to +z, chalk rows on it -- its list shown on the screen when Twinsen reads it (RACEMOD.CPP plaque=). Hung on the wall: its
    // box under the floor (KashBox), walked by.
    public const float SlateWide = 520, SlateHigh = 400, SlateLow = 1050;
    private const int SlateStone = 3 * 16 + 1, SlateFrame = 1 * 16 + 5, SlateChalk = 3 * 16 + 11;
    public static Body BuildRecordSlate(byte[] header)
    {
        var m = new CarMesh("a slate of the speedrun records", new[] { -1 });
        m.Light = 0.7f;
        const float Mid = SlateLow + SlateHigh / 2;
        m.Box(0, new Vector3(0, Mid, 18), new Vector3(SlateWide + 60, SlateHigh + 60, 24), SlateFrame);
        m.Box(0, new Vector3(0, Mid, 32), new Vector3(SlateWide, SlateHigh, 6), SlateStone);
        // (the chalk: a title row and the list's rows, a name and a time each)
        m.Box(0, new Vector3(0, SlateLow + SlateHigh - 50, 36), new Vector3(SlateWide * 0.6f, 14, 2), SlateChalk);
        for (var r = 0; r < 6; r++)
        {
            var y = SlateLow + SlateHigh - 105 - r * 44;
            m.Box(0, new Vector3(-SlateWide * 0.17f, y, 36), new Vector3(SlateWide * 0.45f, 9, 2), SlateChalk);
            m.Box(0, new Vector3(SlateWide * 0.3f, y, 36), new Vector3(SlateWide * 0.2f, 9, 2), SlateChalk);
        }
        return m.ToBody(header);
    }
    private static short[]? KashBox(int generic) => generic == RecordSlate ? new short[] { -300, -16, -20, 300, 0, 80 } : null;

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
