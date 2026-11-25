using System.IO;

namespace LBAAssembler.Terrain;

// The magic slate as the Cup's book of track maps (the story plan, 2026-11-03: "a page per story track added on arrival"; built
// 2026-11-25 -- the user: "make sure the magic slate actually displays collected race tracks the same way as it shows the retail images
// that are collectable for it"): each story track's map drawn in chalk on the slate, as the game's own pages are -- the slate's dark
// board and its grid, the island's shore dotted round, the road's two edges, the start line chequered and an arrow the way the lap runs
// -- into SCREEN.HQR as a page of its own (two entries a page, the picture and its palette, as the game's: INVENT.CPP draws page n from
// entry 2n, the slate's plan box from (78, 61), 480 x 360, its colour 0 see-through). The pages in the story's order from SlateFirstPage:
// the dream's (Polar Island) and the storm's (Citadel 1) the slate's two hidden pages, their quests' (Q_BLUEPRINT, Q_SHEET at 2); the
// others come in with their chapter. The engine puts each in once Twinsen has the slate (RACEMOD.CPP slate_page=).
internal static partial class RaceTrackStory
{
    // the first track page (SCREEN.HQR entry 80: the game's own end at 78), the tracks in the story's order
    public const int SlateFirstPage = 40, SlateTracks = 15;
    // (the slate's own picture: its board and grid under each page's map)
    private const int SlateBoard = 6, SlatePalette = 7;
    private const int BoxX = 78, BoxY = 61, BoxW = 480, BoxH = 360;
    // the chalk's shades (the greys' ramp, RESS.HQR's palette 0: the game's own pages are drawn in them): the board, the faint, the
    // shore, the line, its brightest
    // (as bright as the game's own pages draw theirs: their lines white with a grey edge, the shore white over a dotted grey band)
    private const byte Board = 48, Faint = 52, Shore = 60, Chalk = 63, Edge = 58, Band = 54, Bright = 63;

    // The race file's lines: each track page and what brings it (the chapter, game variable 253; the hidden pages, their quests)
    internal static IEnumerable<string> SlateLines()
    {
        yield return $"slate_page={SlateFirstPage} {QBlueprint} 2";
        yield return $"slate_page={SlateFirstPage + 1} {QSheet} 2";
        for (var k = 2; k < SlateTracks; k++) yield return $"slate_page={SlateFirstPage + k} 253 {k}";
    }

    // A track's place in the story's order (its page: SlateFirstPage + it), or -1. `chapter`: Citadel Island's two (1 the storm, 2 the town)
    private static int SlateIndex(RaceTrackIsland island, int chapter) =>
        island == RaceTrackIsland.Citadel ? (chapter == 2 ? 2 : 1)
        : island == RaceTrackIsland.Polar ? 0
        : island == RaceTrackIsland.Celebration ? 14
        : CardChapter(island);

    // A track's slate page into SCREEN.HQR (the entries before it made as the file needs them, empty)
    internal static List<string> SlatePage(string gameDirectory, RaceTrackIsland island, RaceTrackReport report, int chapter = -1)
    {
        var log = new List<string>();
        var index = SlateIndex(island, chapter);
        if (index < 0 || index >= SlateTracks) return log;
        var path = Path.Combine(gameDirectory, "SCREEN.HQR");
        if (!File.Exists(path)) { log.Add("the slate's track page left out: no SCREEN.HQR"); return log; }
        var lap = report.Roads.FirstOrDefault();
        if (lap is null || lap.X.Length < 8) { log.Add($"the slate's page of {island.Name} left out: no lap"); return log; }
        var screen = HqrArchive.Open(path);
        if (screen.Count <= SlatePalette) { log.Add("the slate's track page left out: SCREEN.HQR has no slate"); return log; }
        var board = screen.Read(SlateBoard);
        if (board.Length < 640 * 480) { log.Add("the slate's track page left out: the slate's picture isn't 640 x 480"); return log; }
        var palette = screen.Read(SlatePalette);
        var picture = Draw(board, lap, report);
        var bytes = File.ReadAllBytes(path);
        var page = SlateFirstPage + index;
        while (HqrArchive.CountEntries(path) < 2 * page + 2)
        {
            bytes = HqrWriter.AppendEntry(bytes, HqrWriter.StoredEntry(new byte[] { 0 }));
            File.WriteAllBytes(path, bytes);
        }
        bytes = HqrWriter.ReplaceEntry(bytes, 2 * page, HqrWriter.CompressedEntry(picture));
        bytes = HqrWriter.ReplaceEntry(bytes, 2 * page + 1, HqrWriter.StoredEntry(palette));
        File.WriteAllBytes(path, bytes);
        log.Add($"the magic slate's page of {island.Name}{(chapter > 0 ? $" (chapter {chapter})" : "")}: page {page}, SCREEN.HQR entries {2 * page} and {2 * page + 1} ({lap.X.Length} points of the lap drawn)");
        return log;
    }

    // The page's picture: colour 0 (see-through) but the plan box, where the slate's board is copied and the map drawn on it
    private static byte[] Draw(byte[] board, TrackRoad lap, RaceTrackReport report)
    {
        var pic = new byte[640 * 480];
        for (var y = BoxY; y < BoxY + BoxH; y++) Array.Copy(board, y * 640 + BoxX, pic, y * 640 + BoxX, BoxW);
        // the map's frame: the lap's extent and a margin, fitted to the box less its own margin
        double x0 = lap.X.Min(), x1 = lap.X.Max(), z0 = lap.Z.Min(), z1 = lap.Z.Max();
        const double Margin = 8;
        x0 -= Margin; x1 += Margin; z0 -= Margin; z1 += Margin;
        const int Inner = 24;
        double scale = Math.Min((BoxW - 2 * Inner) / (x1 - x0), (BoxH - 2 * Inner) / (z1 - z0));
        double ox = BoxX + BoxW / 2.0 - (x0 + x1) / 2 * scale, oy = BoxY + BoxH / 2.0 - (z0 + z1) / 2 * scale;
        (double X, double Y) At(double x, double z) => (ox + x * scale, oy + z * scale);
        (double X, double Z) Cell(int px, int py) => ((px - ox) / scale, (py - oy) / scale);
        bool Inside(int x, int y) => x >= BoxX + 2 && x < BoxX + BoxW - 2 && y >= BoxY + 2 && y < BoxY + BoxH - 2;
        var rnd = new Random(lap.X.Length * 7919 + (int)(x0 * 31));
        void Dot(int x, int y, byte c) { if (Inside(x, y)) pic[y * 640 + x] = c; }
        // (a chalk stroke: its pixels the line's shade, a few dropped, a fainter one beside some -- the chalk's grain)
        void Stroke(double ax, double ay, double bx, double by, byte c)
        {
            var n = (int)Math.Ceiling(Math.Max(Math.Abs(bx - ax), Math.Abs(by - ay))) + 1;
            for (var i = 0; i <= n; i++)
            {
                double t = (double)i / n;
                int x = (int)Math.Round(ax + (bx - ax) * t), y = (int)Math.Round(ay + (by - ay) * t);
                var r = rnd.Next(24);
                if (r == 0) continue;
                Dot(x, y, r == 1 ? Edge : c);
            }
        }
        // the shore: land a cell's worth of ground over the sea, its edge dotted and a faint dotted band off it (as the game's own map of
        // the island under the slate's dome draws its shore)
        if (report.GroundAfter is { } ground)
        {
            var land = new bool[BoxW, BoxH];
            for (var py = 0; py < BoxH; py++)
                for (var px = 0; px < BoxW; px++)
                {
                    var (cx, cz) = Cell(BoxX + px, BoxY + py);
                    land[px, py] = ground(cx, cz) >= 200;
                }
            for (var py = 1; py < BoxH - 1; py++)
                for (var px = 1; px < BoxW - 1; px++)
                {
                    if (!land[px, py]) continue;
                    var edge = !land[px - 1, py] || !land[px + 1, py] || !land[px, py - 1] || !land[px, py + 1];
                    if (edge && rnd.Next(9) != 0) Dot(BoxX + px, BoxY + py, Shore);
                }
            for (var py = 3; py < BoxH - 3; py++)
                for (var px = 3; px < BoxW - 3; px++)
                {
                    if (land[px, py] || ((px + py) & 1) != 0) continue;
                    var near = land[px - 3, py] || land[px + 3, py] || land[px, py - 3] || land[px, py + 3] || land[px - 2, py - 2] || land[px + 2, py + 2];
                    if (near) Dot(BoxX + px, BoxY + py, Band);
                }
        }
        // the road: its two edges, a little apart, along the lap
        var half = Math.Clamp(1.8 * scale, 2.5, 5);
        var count = lap.X.Length;
        int Next(int i) => lap.Closed ? (i + 1) % count : Math.Min(i + 1, count - 1);
        for (var i = 0; i < count - (lap.Closed ? 0 : 1); i++)
        {
            var j = Next(i);
            var (ax, ay) = At(lap.X[i], lap.Z[i]);
            var (bx, by) = At(lap.X[j], lap.Z[j]);
            double dx = bx - ax, dy = by - ay, len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) continue;
            double nx = -dy / len * half, ny = dx / len * half, ex = -dy / len, ey = dx / len;
            Stroke(ax + nx, ay + ny, bx + nx, by + ny, Chalk);
            Stroke(ax - nx, ay - ny, bx - nx, by - ny, Chalk);
            // (each edge two pixels: the white and a grey one outside it)
            Stroke(ax + nx + ex, ay + ny + ey, bx + nx + ex, by + ny + ey, Edge);
            Stroke(ax - nx - ex, ay - ny - ey, bx - nx - ex, by - ny - ey, Edge);
        }
        // the start line: across the road, chequered
        if (report.StartLine.Count > 0)
        {
            var s = report.StartLine[0];
            var (sx, sy) = At(s.X, s.Z);
            double dl = Math.Sqrt(s.DirX * s.DirX + s.DirZ * s.DirZ);
            if (dl > 1e-6)
            {
                double ux = s.DirX / dl, uy = s.DirZ / dl, vx = -uy, vy = ux;
                var w = half + 2;
                for (var a = -w; a <= w; a += 1)
                    for (var b = -3.0; b <= 3.0; b += 1)
                    {
                        var check = (((int)Math.Floor((a + w) / 3) + (int)Math.Floor((b + 3) / 3)) & 1) == 0;
                        Dot((int)Math.Round(sx + vx * a + ux * b), (int)Math.Round(sy + vy * a + uy * b), check ? Bright : Board);
                    }
                // the way the lap runs: an arrowhead a little after the line
                var k = Math.Min(count - 1, Math.Max(1, count / 14));
                var (ax, ay) = At(lap.X[k], lap.Z[k]);
                var (bx, by) = At(lap.X[Next(k)], lap.Z[Next(k)]);
                double tx = bx - ax, ty = by - ay, tl = Math.Sqrt(tx * tx + ty * ty);
                if (tl > 1e-6)
                {
                    tx /= tl; ty /= tl;
                    double px = ax + ty * (half + 9), py = ay - tx * (half + 9);
                    const double L = 9;
                    Stroke(px - tx * L, py - ty * L, px, py, Bright);
                    Stroke(px, py, px - tx * 5 + ty * 4, py - ty * 5 - tx * 4, Bright);
                    Stroke(px, py, px - tx * 5 - ty * 4, py - ty * 5 + tx * 4, Bright);
                }
            }
        }
        return pic;
    }
}
