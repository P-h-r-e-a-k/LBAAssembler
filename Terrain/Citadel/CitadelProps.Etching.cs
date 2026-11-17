using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The darts sums carved into the Tralu caves' wall (scene 20, the upper walkway's back wall between its two barred windows -- the user,
// 2026-11-17: "let's add the following in the wall, this should be etched into the stone. It's three lines, and the second line is
// partially obscured / T20-T20-T20 = 180 / T2#-##0-## = 151 / T20-T20-Bull - 170"): the three lines in 5 x 7 dots cut into the stone,
// dark against the wall, each '#' a character worn away -- a few scattered chips where it was. The body's own +z out of the wall, its
// lines along +x (the actor turned 1024: along the wall's -z, left to right as the camera sees it); Twinsen reads it out on Action
// (RaceTrackStory.TraluEtching).
internal static partial class CitadelProps
{
    // (a line a body -- the three in one were over the engine's 550 points: TraluEtching, +1, +2, the same place, each line at its height)
    public const int TraluEtching = 174, TraluEtchingLines = 3;
    public static readonly string[] EtchingLines = { "T20-T20-T20 = 180", "T2#-##0-## = 151", "T20-T20-Bull - 170" };
    // a dot's width and height, the text's lowest line's foot over the floor, the gap between lines (world units)
    private const float EtchDotWide = 9, EtchDotTall = 20, EtchFoot = 600, EtchGap = 50;
    private const int EtchGroove = 31, EtchChip = 26;
    private static readonly Dictionary<char, string[]> EtchFont = new()
    {
        ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
        ['2'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" },
        ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
        ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
        ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
        ['5'] = new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
        ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
        ['-'] = new[] { ".....", ".....", ".....", "#####", ".....", ".....", "....." },
        ['='] = new[] { ".....", ".....", "#####", ".....", "#####", ".....", "....." },
        ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
        ['u'] = new[] { ".....", ".....", "#...#", "#...#", "#...#", "#..##", ".##.#" },
        ['l'] = new[] { ".##..", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." },
    };
    // worn away: a few chips, a different few for each
    private static readonly string[][] EtchWorn =
    {
        new[] { ".....", "..#..", ".....", "#....", "...#.", ".....", ".#..." },
        new[] { "#....", ".....", "...#.", ".....", ".#...", "....#", "....." },
        new[] { ".....", "...#.", "#....", ".....", "..#..", ".....", "#...#" },
    };

    public static Body BuildTraluEtching(byte[] header, int line)
    {
        var m = new CarMesh($"the Tralu caves' carved darts sums, line {line + 1}", new[] { -1 });
        m.Light = 0.8f;
        var worn = EtchingLines.Take(line).Sum(t => t.Count(ch => ch == '#'));
        {
            var text = EtchingLines[line];
            var wide = (text.Length * 6 - 1) * EtchDotWide;
            var top = EtchFoot + (EtchingLines.Length - line) * 7 * EtchDotTall + (EtchingLines.Length - 1 - line) * EtchGap;
            for (var c = 0; c < text.Length; c++)
            {
                var rows = text[c] == '#' ? EtchWorn[worn++ % EtchWorn.Length] : EtchFont.TryGetValue(text[c], out var g) ? g : null;
                if (rows is null) continue;
                var colour = text[c] == '#' ? EtchChip : EtchGroove;
                // (each row's runs of dots, a run over runs of the same span below it merged into one rectangle -- the club's sign's way)
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
                        float x0 = -wide / 2 + (c * 6 + col) * EtchDotWide, x1 = -wide / 2 + (c * 6 + end + 1) * EtchDotWide;
                        float y1 = top - r * EtchDotTall, y0 = top - (bottom + 1) * EtchDotTall;
                        m.Plate(0, colour, new(x0, y0, 6), new(x1, y0, 6), new(x1, y1, 6), new(x0, y1, 6));
                    }
            }
        }
        return m.ToBody(header);
    }
}
