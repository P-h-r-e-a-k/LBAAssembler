using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The Imperial Hotel's reception counter, a traditional front desk (the user, 2026-11-26: "we need to change the imperial hotel so the
// receptionist is behind the counter. Let's remodel the counter to a more traditional look complete with keys and pigeon holes"). Three
// bodies -- the engine's limit of points a body -- each built long along its own x, its front to its own +z, round the counter's middle on
// the floor: all three placed there at beta 1024, their fronts face the room (+x); their boxes in the room's axes (HotelProps.Box).
//   - the counter (ReceptionCounter): a mahogany plinth and body, a front of fielded panels between pilasters, a brass rail along it at
//     foot height, a marble top over the front with a brass edge, its flap at one end on brass hinges, a brass plate on its middle panel;
//     on it the brass service bell, the register open with its ribbon, a pen, a banker's lamp, its shade green;
//   - behind the clerk, the keys' cabinet (ReceptionKeys): a mahogany case on a foot under a stepped cornice, pigeonholes six across and
//     four up, a room's letters in some, a brass hook under each and the free rooms' keys on them with red tassels;
//   - over it, RECEPTION on a brass plaque (ReceptionSign).
internal static partial class HotelProps
{
    public const int ReceptionKeys = 195, ReceptionSign = 194;
    public const float CounterLong = 1150, CounterDeep = 420;
    private const float CounterHigh = 760, CabinetFront = -980, CabinetDeep = 220, CabinetHigh = 2700, CabinetHalf = 1000;

    private static Body BuildReceptionCounter(byte[] header)
    {
        var m = new CarMesh("the reception counter", new[] { -1 });
        m.Light = 0.85f;
        const float L = CounterLong, D = CounterDeep, H = CounterHigh;
        m.Box(0, new Vector3(0, 60, 0), new Vector3(2 * L + 40, 120, 2 * D + 40), MahoganyDark);                        // the plinth
        m.Box(0, new Vector3(0, H / 2 + 60, 0), new Vector3(2 * L, H - 120, 2 * D), Mahogany, Mahogany);                // the body
        // the front: pilasters, and between them fielded panels -- the flap's end plain
        const int Bays = 5;
        var bay = 2 * L / Bays;
        for (var k = 0; k <= Bays; k++) m.Box(0, new Vector3(-L + k * bay, H / 2 + 60, D + 18), new Vector3(90, H - 160, 36), MahoganyDark, Mahogany);
        for (var k = 0; k < Bays; k++)
        {
            var x = -L + (k + 0.5f) * bay;
            if (k == Bays - 1) { m.Box(0, new Vector3(x, H / 2 + 60, D + 12), new Vector3(bay - 110, H - 200, 24), Walnut, Walnut); continue; }
            m.Box(0, new Vector3(x, H / 2 + 60, D + 14), new Vector3(bay - 140, H - 260, 28), Walnut, Walnut);
            m.Box(0, new Vector3(x, H / 2 + 60, D + 32), new Vector3(bay - 260, H - 400, 18), Mahogany, Mahogany);
        }
        // the brass foot rail and its brackets
        m.Strut(0, new Vector3(-L + 60, 200, D + 150), new Vector3(L - 60, 200, D + 150), 36, Brass);
        for (var k = 0; k < 4; k++) { var x = -L + 120 + k * (2 * L - 240) / 3; m.Strut(0, new Vector3(x, 200, D + 30), new Vector3(x, 200, D + 150), 22, BrassDark); }
        // the marble top over the front, bevelled; a brass edge under its lip; the flap's hinges
        HotelKit.BevelBox(m, new(0, H + 40, 40), new(2 * L + 140, 80, 2 * D + 200), 26, new Finish(MarbleVein, 0.85f), new Finish(Marble, 0.9f));
        m.Box(0, new Vector3(0, H - 8, D + 125), new Vector3(2 * L + 100, 16, 14), Brass);
        foreach (var x in new[] { L - bay + 60f, L - 60f }) m.Box(0, new Vector3(x, H + 84, -60), new Vector3(70, 10, 120), Brass);
        // a brass plate on the middle panel
        m.Box(0, new Vector3(-L + 2.5f * bay, H - 210, D + 48), new Vector3(520, 100, 12), BrassDark, Brass);
        // on it: the bell on its stand, the register open, its ribbon, a pen, the banker's lamp
        var topY = H + 80;
        m.Box(0, new Vector3(-L + 260, topY + 10, 160), new Vector3(120, 20, 120), BrassDark, Brass);
        m.Ball(0, new Vector3(-L + 260, topY + 62, 160), 50, Brass);
        m.Ball(0, new Vector3(-L + 260, topY + 118, 160), 12, BrassDark);
        m.Box(0, new Vector3(60, topY + 14, 120), new Vector3(520, 28, 360), VelvetDark, VelvetDark);
        foreach (var s in new[] { -1f, 1f }) m.Box(0, new Vector3(60 + s * 130, topY + 34, 120), new Vector3(240, 12, 320), Linen, Linen);
        m.Box(0, new Vector3(140, topY + 42, 290), new Vector3(14, 4, 120), Velvet);
        m.Strut(0, new Vector3(380, topY + 30, 220), new Vector3(500, topY + 70, 80), 12, Ebony);
        m.Box(0, new Vector3(L - 460, topY + 12, -120), new Vector3(180, 24, 180), BrassDark, Brass);
        m.Strut(0, new Vector3(L - 460, topY + 24, -120), new Vector3(L - 460, topY + 260, -120), 22, BrassDark);
        m.Box(0, new Vector3(L - 460, topY + 300, -120), new Vector3(380, 70, 130), Teal, Teal);
        m.Ball(0, new Vector3(L - 460, topY + 255, -120), 36, GlowYellow);
        return m.ToBody(header);
    }

    // behind the clerk: the keys' cabinet
    private static Body BuildReceptionKeys(byte[] header)
    {
        var m = new CarMesh("the reception's keys", new[] { -1 });
        m.Light = 0.85f;
        float cz = CabinetFront - CabinetDeep / 2;
        m.Box(0, new Vector3(0, 70, cz), new Vector3(2 * CabinetHalf + 40, 140, CabinetDeep + 40), MahoganyDark);
        m.Box(0, new Vector3(0, CabinetHigh / 2, cz), new Vector3(2 * CabinetHalf, CabinetHigh - 200, CabinetDeep), Mahogany, Mahogany);
        // (the cornice in two steps out, a brass bead under it)
        m.Box(0, new Vector3(0, CabinetHigh - 60, cz + 20), new Vector3(2 * CabinetHalf + 120, 100, CabinetDeep + 80), MahoganyDark, Mahogany);
        m.Box(0, new Vector3(0, CabinetHigh + 20, cz + 30), new Vector3(2 * CabinetHalf + 200, 60, CabinetDeep + 110), Mahogany, Mahogany);
        m.Box(0, new Vector3(0, CabinetHigh - 120, CabinetFront + 32), new Vector3(2 * CabinetHalf + 80, 16, 10), Brass);
        // (the pigeonholes: a dark back, the dividers and shelves over it)
        const int Cols = 6, Rows = 4;
        float cellW = (2 * CabinetHalf - 160) / Cols, cellH = 300, gap = 150, rowsTop = CabinetHigh - 300, gridLow = rowsTop - Rows * (cellH + gap) + gap;
        m.Box(0, new Vector3(0, (rowsTop + gridLow) / 2, CabinetFront - 30), new Vector3(2 * CabinetHalf - 160, rowsTop - gridLow, 12), Ebony);
        for (var c = 0; c <= Cols; c++) m.Box(0, new Vector3(-CabinetHalf + 80 + c * cellW, (rowsTop + gridLow) / 2, CabinetFront + 4), new Vector3(30, rowsTop - gridLow, 70), MahoganyDark, Mahogany);
        for (var r = 0; r <= Rows; r++)
        {
            var y = rowsTop - r * (cellH + gap);
            m.Box(0, new Vector3(0, y, CabinetFront + 4), new Vector3(2 * CabinetHalf - 160, 26, 70), MahoganyDark, Mahogany);
            if (r > 0 && r < Rows) m.Box(0, new Vector3(0, y + cellH - gap + 140, CabinetFront + 4), new Vector3(2 * CabinetHalf - 160, 20, 60), MahoganyDark, Mahogany);
        }
        for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Cols; c++)
            {
                float x = -CabinetHalf + 80 + (c + 0.5f) * cellW, y = rowsTop - r * (cellH + gap) - cellH / 2;
                var n = r * Cols + c;
                // a room's letters in some
                if (n % 3 == 0) m.Box(0, new Vector3(x - 10, y - cellH / 2 + 80, CabinetFront - 10), new Vector3(cellW - 120, 110, 30), n % 2 == 0 ? Linen : Ivory, Linen);
                // its hook, and the free room's key on it: a brass fob, a red tassel
                var hy = y - cellH / 2 - 30;
                if (n % 5 == 3) { m.Ball(0, new Vector3(x, hy, CabinetFront + 30), 10, BrassDark); continue; }
                m.Box(0, new Vector3(x, hy - 50, CabinetFront + 34), new Vector3(40, 80, 10), BrassDark, Brass);
                m.Ball(0, new Vector3(x, hy - 112, CabinetFront + 36), 18, Velvet);
            }
        return m.ToBody(header);
    }

    // over the cabinet: RECEPTION on a brass plaque
    private static Body BuildReceptionSign(byte[] header)
    {
        var m = new CarMesh("the reception's sign", new[] { -1 });
        m.Light = 0.9f;
        const float Y = CabinetHigh + 230, Z = CabinetFront + 40;
        m.Box(0, new Vector3(0, Y, Z - 10), new Vector3(1080, 200, 20), BrassDark, Brass);
        m.Box(0, new Vector3(0, Y, Z + 2), new Vector3(1000, 150, 6), Mahogany, Mahogany);
        Text(m, "RECEPTION", 0, Y, Z + 8, 18, Brass);
        return m.ToBody(header);
    }
}
