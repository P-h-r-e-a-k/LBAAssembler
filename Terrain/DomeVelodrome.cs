using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;
using LBAAssembler.Assets;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.LbaScript;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Dome of the Slate made a velodrome (the user, 2026-11-24: "See if you can model a velodrome for the inside of the dome of the slate.
// The dome of the slate on the outside is round, so this would fit a velodrome ... Let's make sure all new 3D assets are rendered to a high
// quality, lots of normals so they are up to the quality of the retail game assets instead of being flat"). The game's room inside the Dome
// (scene 26: a starry void crossed on invisible paths) becomes a round hall with a banked wooden track sunk into its floor.
//
// Made as the game's own rooms were made: a 3D model rendered from the room's camera and cut into its bricks. The model is a smooth one
// (a distance field: every surface's normal its own, the banking, the rails, the seats and the columns lit pixel by pixel with soft shadows
// and occlusion), drawn in the room's palette along its colour ramps, and each pixel given to the brick of the cell its surface lies in --
// so Twinsen goes behind what is in front of him and in front of the rest, as with any brick. Under it the cells' collision, fitted to the
// model's floor from the heights at the cells' corners: the game's slopes where they fit (the straights' banking climbs a layer each
// diagonal step, walked up and down), steps where it is steeper (the turns, banked to 42 degrees, are for bikes, not for walking).
//
// The hall, round the oval's spine (its long axis across the screen, so that every part of the banking faces the camera): the infield, low
// (the top of layer 0), with a winners' podium and the Dome's name painted on it; a safety band, the blue cote d'azur, the boards with their
// black, red and blue lines, the finish line; the fence on the top edge with a gate in the middle of each straight; the floor round it at
// the straights' top edges (layer 5's top), slate tiles; stands behind the far straight, SLATE in yellow seats; the dome's wall on the far
// side, columns and a starry sky between them, its two doors (the way in from outside, and the game's back door to the slate's garden).
internal static class DomeVelodrome
{
    public const int Scene = 26, Outside = 44;
    private const int Size = 64, Height = 25;
    private const double Cell = 512, Lay = 256, IRt2 = 0.70710678118654752, HalfPi = Math.PI / 2;
    private const double Xc = 32 * Cell, Zc = 32 * Cell;
    // the floors: the infield's (layer 0's top) and the hall's round the bowl (layer 5's top -- the straights' top edges)
    private const double FIn = 256, FOut = 1536;
    // the oval: its straights' half length; from its spine the infield, the safety band, the cote d'azur, the boards (R2 and the boards'
    // width whole diagonal half-steps of the grid, so that a straight's banking -- a layer a diagonal step -- meets the cells' corners)
    private const double Lu = 14.5 * Cell;
    private const double R0 = 4.0 * Cell, R1 = 5.4 * Cell, R2 = 9 * Cell * IRt2, Wd = 10 * Cell * IRt2, R3 = R2 + Wd, Ft = 128;
    // the banking: the straights' 19.5 degrees (a layer a diagonal step), the turns' 42 at their apex, the turns' boards curving up a little
    private const double TanS = 0.35355339059327373, TanT = 0.9004040442978399, Kc = 0.15, Fillet = 110;
    // the fence on the top edge: a kickboard, posts, two rails; a gate in the middle of each straight
    private const double KickH = 260, Rail1 = 560, Rail2 = 960, RailR = 28, PostH = 1000, PostHalf = 30, PostGap = 880, GateHalf = 1.4 * Cell;
    // the stands behind the far straight: a walkway (wide: the fence's invisible wall takes most of a cell either side of it, its cells
    // across the grid's diagonal), then the rows
    private const double Walkway = 3 * Cell, RowD = 640, RowR = 320, StandHalf = 13.5 * Cell, SeatPitch = 460, SeatHalf = 180;
    private const int Rows = 8;
    // the dome's wall: round the hall, full height on the far side, a low kerb on the near (cut away, as the game's rooms are)
    private const double Rw = 30.5 * Cell, Tw = Cell, Hw = 6300, Stub = 110, FarCos = 0.22, ColR = 175, ColAt = Rw - 70, ColStep = 9 * Math.PI / 180;
    private const double Ycap = FOut + 3900, BaseTop = FOut + 640;
    // the doors: the way in from outside (the wall's -z point) and the back door to the slate's garden (its -x point)
    private static readonly double[] DoorAngles = { -HalfPi, Math.PI };
    private const double DoorHalf = 512, DoorRise = 1700, Recess = Tw - 120;
    // the podium in the infield's middle (cells): first 2 x 2 at (31, 31) two layers high, second at (31, 33) and third at (33, 31) one layer
    private static readonly (int X, int Z, int Layers)[] Podium = { (31, 31, 2), (31, 33, 1), (33, 31, 1) };
    // the finish line across the far straight, the 200 m line across the near
    private const double FinishA = 3.5 * Cell, LineA200 = -3.5 * Cell;

    // where Twinsen comes in (from outside: inside the main door) and the actors' places
    public static readonly (int X, int Y, int Z) Arrival = ((int)Xc, (int)FOut, (int)(Zc - Rw + 1100));
    // (the outside door's own: the engine takes off where in its zone he stepped, up to a cell each way -- GereZoneChangeCube --
    // so half a cell across and a cell back, never in the way out's zone, 700 deep)
    private static readonly (int X, int Y, int Z) ArrivalInfo = (Arrival.X + 256, Arrival.Y, Arrival.Z + 512);
    private static (int X, int Y, int Z) AtOval(double a, double b, double y) => ((int)Math.Round(Xc + (a + b) * IRt2), (int)y, (int)Math.Round(Zc + (b - a) * IRt2));

    // ================================================================================================= the model
    private enum M : byte { None, Bowl, Floor, Kick, Post, Rail, Stand, Seat, Wall, Column, Capital, Entab, Frame, Podium }

    private static double Bank(double psi) { var c = Math.Cos(psi); return c <= 0 ? 0 : c * Math.Sqrt(c); }

    // the boards' height at a distance from the spine (and its slope across), where the banking is `w` of the way to the turns' apex
    private static double Deck(double d, double w, out double slope)
    {
        double tan = TanS + (TanT - TanS) * w, k = Kc * w;
        double s = Math.Min(d - R2, Wd), f, fp;
        if (s <= -Fillet) { f = 0; fp = 0; }
        else if (s < Fillet) { f = (s + Fillet) * (s + Fillet) / (4 * Fillet); fp = (s + Fillet) / (2 * Fillet); }
        else { f = s; fp = 1; }
        if (d - R2 > Wd) fp = 0;
        slope = tan * fp * (1 + 2 * k * f / Wd);
        return FIn + tan * (f + k * f * f / Wd);
    }
    private static double TopEdge(double w) => Deck(R3, w, out _);

    private static void Oval(double x, double z, out double a, out double b, out double d, out double psi, out double ea)
    {
        double ax = x - Xc, az = z - Zc;
        a = (ax - az) * IRt2; b = (ax + az) * IRt2;
        ea = Math.Abs(a) - Lu;
        if (ea <= 0) { d = Math.Abs(b); psi = b >= 0 ? HalfPi : -HalfPi; }
        else { d = Math.Sqrt(ea * ea + b * b); psi = Math.Atan2(b, ea); }
    }

    private static double Box2(double px, double py, double hx, double hy, double r)
    {
        double qx = Math.Abs(px) - hx + r, qy = Math.Abs(py) - hy + r;
        return Math.Sqrt(Math.Max(qx, 0) * Math.Max(qx, 0) + Math.Max(qy, 0) * Math.Max(qy, 0)) + Math.Min(Math.Max(qx, qy), 0) - r;
    }
    private static double Box3(double px, double py, double pz, double hx, double hy, double hz, double r)
    {
        double qx = Math.Abs(px) - hx + r, qy = Math.Abs(py) - hy + r, qz = Math.Abs(pz) - hz + r;
        double ox = Math.Max(qx, 0), oy = Math.Max(qy, 0), oz = Math.Max(qz, 0);
        return Math.Sqrt(ox * ox + oy * oy + oz * oz) + Math.Min(Math.Max(qx, Math.Max(qy, qz)), 0) - r;
    }
    private static void Take(ref double best, ref M mat, double v, M m) { if (v < best) { best = v; mat = m; } }

    // the door a place is in front of: its angle's index, the place's offset across the door and out from the middle (or -1)
    private static int DoorOf(double ax, double az, out double across, out double outward)
    {
        across = 0; outward = 0;
        for (var i = 0; i < DoorAngles.Length; i++)
        {
            double cx = Math.Cos(DoorAngles[i]), cz = Math.Sin(DoorAngles[i]);
            var o = ax * cx + az * cz;
            if (o < Rw - 900) continue;
            var t = -ax * cz + az * cx;
            if (Math.Abs(t) > DoorHalf + 400) continue;
            across = t; outward = o;
            return i;
        }
        return -1;
    }
    // the door's opening (its arch), across and up from its sill
    private static double Arch(double across, double up) =>
        up < DoorRise ? Box2(across, up - DoorRise / 2 + 20, DoorHalf, DoorRise / 2 + 20, 0) : Math.Sqrt(across * across + (up - DoorRise) * (up - DoorRise)) - DoorHalf;

    // the far side of the hall: how far into it (negative) or out of it a place on the floor is, round the middle
    private static double FarSector(double ax, double az, double rho)
    {
        var c = (ax + az) * IRt2 / Math.Max(rho, 1);
        var beta = Math.Acos(Math.Clamp(c, -1, 1));
        var b0 = Math.Acos(FarCos);
        return beta > b0 ? -rho * Math.Sin(Math.Min(beta - b0, HalfPi)) : rho * Math.Sin(Math.Min(b0 - beta, HalfPi));
    }

    private static double Dist(double x, double y, double z) => Map(x, y, z, out _);

    private static double Map(double x, double y, double z, out M mat)
    {
        double best = 1e9; var m = M.None;
        double ax = x - Xc, az = z - Zc, rho = Math.Sqrt(ax * ax + az * az);
        Oval(x, z, out var a, out var b, out var d, out var psi, out var ea);
        var w = Bank(psi);

        // ---- the bowl: the infield, the bands, the boards, up to the fence's outer side
        {
            var h = Deck(d, w, out var slope);
            var s = Math.Max(Math.Max((y - h) / Math.Sqrt(1 + slope * slope), d - (R3 + Ft)), -y);
            Take(ref best, ref m, s, M.Bowl);
        }
        // ---- the hall's floor round it
        Take(ref best, ref m, Math.Max(Math.Max(y - FOut, R3 + Ft - d), Math.Max(rho - (Rw + Tw), -y)), M.Floor);

        // ---- the fence: kickboard, posts, rails (a gate in each straight's middle). (Every part everywhere, exactly: a part left out
        // away from it lets a step jump into it, and a stand-in distance shows in the shadows.)
        var dc = R3 + Ft / 2;
        var top = TopEdge(w);
        {
            var gate = ea <= 0 ? GateHalf - Math.Abs(a) : -1e9;
            Take(ref best, ref m, Math.Max(Box2(d - dc, y - (top + KickH / 2 - 30), Ft / 2, KickH / 2 + 30, 10), gate), M.Kick);
            var rail = Math.Min(Math.Sqrt((d - dc) * (d - dc) + (y - top - Rail1) * (y - top - Rail1)), Math.Sqrt((d - dc) * (d - dc) + (y - top - Rail2) * (y - top - Rail2))) - RailR;
            Take(ref best, ref m, Math.Max(rail, gate), M.Rail);
            double lt;
            if (ea <= 0) lt = a - PostGap * Math.Round(a / PostGap);
            else
            {
                var n = Math.Max(1, Math.Round(Math.PI * dc / PostGap));
                var step = Math.PI / n;
                var al = HalfPi - psi;
                lt = d * Math.Sin(al - step * Math.Round(al / step));
            }
            Take(ref best, ref m, Math.Max(Box3(lt, y - (top + PostH / 2), d - dc, PostHalf, PostH / 2, PostHalf, 10), gate), M.Post);
            // (the gates' own posts, either side of each, a ball on each)
            var gt = ea <= 0 ? Math.Abs(a) - (GateHalf + 45) : Math.Sqrt(ea * ea) + Lu - (GateHalf + 45);
            Take(ref best, ref m, Box3(gt, y - (top + PostH / 2), d - dc, 40, PostH / 2, 40, 12), M.Post);
            var by = y - (top + PostH + 40);
            Take(ref best, ref m, Math.Sqrt(gt * gt + by * by + (d - dc) * (d - dc)) - 58, M.Rail);
        }

        // ---- the stands behind the far straight, and their seats
        {
            var q = -b - (R3 + Ft + Walkway);
            var k = (int)Math.Clamp(Math.Floor(q / RowD), 0, Rows - 1);
            var stand = 1e9;
            for (var j = Math.Max(0, k - 1); j <= Math.Min(Rows - 1, k + 1); j++)
            {
                double dq = -(q - j * RowD), dy = y - (FOut + (j + 1) * RowR);
                var outside = Math.Sqrt(Math.Max(dq, 0) * Math.Max(dq, 0) + Math.Max(dy, 0) * Math.Max(dy, 0));
                stand = Math.Min(stand, outside + Math.Min(Math.Max(dq, dy), 0));
            }
            Take(ref best, ref m, Math.Max(Math.Max(stand, Math.Abs(a) - StandHalf), -y), M.Stand);
            var last = Math.Floor((StandHalf - 150 - SeatHalf) / SeatPitch);
            var la = a - SeatPitch * Math.Clamp(Math.Round(a / SeatPitch), -last, last);
            var seat = 1e9;
            for (var j = Math.Max(0, k - 1); j <= Math.Min(Rows - 1, k + 1); j++)
            {
                double yj = FOut + (j + 1) * RowR, q0 = j * RowD;
                var pan = Box3(la, y - (yj + 215), q - (q0 + 0.40 * RowD), SeatHalf, 24, 0.17 * RowD, 14);
                var sb = y - (yj + 390);
                var back = Box3(la, sb, q - (q0 + 0.66 * RowD) - sb * 0.18, SeatHalf - 10, 160, 26, 16) * 0.95;
                var leg = Box3(la, y - (yj + 100), q - (q0 + 0.40 * RowD), 40, 100, 34, 8);
                seat = Math.Min(seat, Math.Min(pan, Math.Min(back, leg)));
            }
            Take(ref best, ref m, seat, M.Seat);
        }

        // ---- the podium
        foreach (var (px, pz, layers) in Podium)
            Take(ref best, ref m, Box3(x - (px + 1) * Cell, y - (FIn + layers * Lay / 2), z - (pz + 1) * Cell, Cell - 4, layers * Lay / 2, Cell - 4, 18), M.Podium);

        // ---- the dome's wall, its doors, its entablature, its columns
        {
            var sector = FarSector(ax, az, rho);
            var shell = Math.Max(Rw - rho, rho - (Rw + Tw));
            var wall = Math.Min(Math.Max(Math.Max(shell, sector), Math.Max(y - Hw, -y)), Math.Max(shell, Math.Max(y - (FOut + Stub), -y)));
            for (var i = 0; i < DoorAngles.Length; i++)
            {
                double cx = Math.Cos(DoorAngles[i]), cz = Math.Sin(DoorAngles[i]);
                double outward = ax * cx + az * cz, across = -ax * cz + az * cx;
                var open = Arch(across, y - FOut);
                wall = Math.Max(wall, -Math.Max(open, Math.Max(Rw - 200 - outward, outward - (Rw + Recess))));
                // (a gilt moulding round the arch)
                Take(ref best, ref m, Math.Max(Math.Abs(open + 45) - 45, Math.Max(Rw - 70 - outward, outward - Rw - 10)), M.Frame);
            }
            Take(ref best, ref m, wall, M.Wall);
            // the entablature: a band round the far side over the columns
            var ent = Math.Max(Math.Max(Rw - 230 - rho, rho - Rw - 10), Math.Abs(y - (Ycap + 470)) - 160);
            Take(ref best, ref m, Math.Max(ent, sector), M.Entab);
            // the columns, every 9 degrees round the far side but at the doors (the nearest three looked at)
            var phi = Math.Atan2(az, ax);
            var k0 = Math.Round(phi / ColStep);
            for (var k = k0 - 1; k <= k0 + 1; k++)
            {
                var at = k * ColStep;
                if ((Math.Cos(at) + Math.Sin(at)) * IRt2 >= FarCos - 0.12) continue;
                var nearDoor = false;
                for (var i = 0; i < DoorAngles.Length; i++) nearDoor |= Math.Abs(Math.IEEERemainder(at - DoorAngles[i], 2 * Math.PI)) < 0.06;
                if (nearDoor) continue;
                var pl = phi - at;
                double lt = rho * Math.Sin(pl), lr = rho * Math.Cos(pl) - ColAt;
                var r2 = Math.Sqrt(lt * lt + lr * lr);
                var shaft = Math.Max(r2 - ColR, Math.Abs(y - (FOut + 150 + (Ycap - FOut - 150) / 2)) - (Ycap - FOut - 150) / 2);
                var plinth = Box3(lt, y - (FOut + 75), lr, 245, 75, 245, 14);
                var torus = Math.Sqrt((r2 - ColR - 10) * (r2 - ColR - 10) + (y - (FOut + 185)) * (y - (FOut + 185))) - 38;
                Take(ref best, ref m, Math.Min(Math.Min(shaft, plinth), torus), M.Column);
                var t = Math.Clamp((y - Ycap) / 260, 0, 1);
                var bell = Math.Max(r2 - (ColR + 110 * t * t + 10), Math.Abs(y - (Ycap + 130)) - 130) * 0.85;
                var abacus = Box3(lt, y - (Ycap + 285), lr, 280, 30, 280, 8);
                Take(ref best, ref m, Math.Min(bell, abacus), M.Capital);
            }
        }
        mat = m;
        return best;
    }

    // ================================================================================================= the look
    // a colour: a ramp of the palette (16 shades, dark to light), the shade it has lit as the floor is, and its shine
    private readonly record struct Paint(int Ramp, double Shade, double Shine = 0, double Sharp = 30);

    private static uint Hash(int a, int b = 0, int c = 0)
    {
        unchecked
        {
            var h = (uint)a * 0x8DA6B343u ^ (uint)b * 0xD8163841u ^ (uint)c * 0xCB1AB31Fu;
            h ^= h >> 15; h *= 0x2C1B3C6Du; h ^= h >> 12; h *= 0x297A2D39u; h ^= h >> 15;
            return h;
        }
    }
    private static double Rand(int a, int b = 0, int c = 0) => (Hash(a, b, c) & 0xFFFFFF) / (double)0x1000000;

    // the 5 x 7 letters the floor's and the seats' words are made of
    private static readonly Dictionary<char, string[]> Font = new()
    {
        ['D'] = new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" },
        ['O'] = new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" },
        ['M'] = new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" },
        ['E'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" },
        ['F'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" },
        ['T'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" },
        ['H'] = new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" },
        ['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" },
        ['L'] = new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" },
        ['A'] = new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" },
        ['1'] = new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" },
        ['2'] = new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" },
        ['3'] = new[] { "11110", "00001", "00001", "01110", "00001", "00001", "11110" },
    };
    // a word's lit pixel: column `c` (0 at the left) and row `r` (0 at the top) of the letters, one column between them
    private static bool Lettered(string word, int c, int r)
    {
        if (r < 0 || r > 6 || c < 0) return false;
        var i = c / 6; var cc = c % 6;
        if (i >= word.Length || cc == 5 || !Font.TryGetValue(word[i], out var g)) return false;
        return g[r][cc] == '1';
    }

    private static Paint Look(M mat, double x, double y, double z, double nx, double ny, double nz)
    {
        double ax = x - Xc, az = z - Zc, rho = Math.Sqrt(ax * ax + az * az);
        Oval(x, z, out var a, out var b, out var d, out var psi, out var ea);
        switch (mat)
        {
            case M.Bowl:
            {
                if (d > R3 + Ft - 6 && Math.Abs(ny) < 0.6) return Cladding(Sigma(a, b, d, psi, ea), y);
                if (d < R0)
                {
                    // the infield: a green sports floor, its edge and centre lines, the Dome's name
                    if (R0 - d < 50 || (Math.Abs(b) < 26 && Math.Abs(a) < Lu - 300)) return new Paint(3, 14);
                    const double px = 143;
                    const string name = "DOME OF THE SLATE";
                    var width = name.Length * 6 - 1;
                    int c = (int)Math.Floor((a + width * px / 2) / px), r = (int)Math.Floor((b + 1900) / px);
                    if (Lettered(name, c, r)) return new Paint(3, 14);
                    return new Paint(8, 6 + 0.4 * (Rand((int)Math.Floor(x / 37), (int)Math.Floor(z / 37)) - 0.5));
                }
                if (d < R1) return d - R0 < 40 ? new Paint(3, 3) : new Paint(3, 10 + 0.3 * (Rand((int)(x / 41), (int)(z / 41)) - 0.5));
                if (d < R2 - 10) return new Paint(12, 13 + 0.25 * (Rand((int)(x / 29), (int)(z / 29)) - 0.5), 0.1);
                return Boards(a, b, d, psi, ea);
            }
            case M.Floor:
                if (rho > Rw + Tw - 8 && Math.Abs(ny) < 0.5) return Cut(y);
                return Slate(x, z);
            case M.Kick:
                // (white boards facing the track, a red band along their top)
                return y > TopEdge(Bank(psi)) + KickH - 70 ? new Paint(4, 9, 0.2) : new Paint(3, 14, 0.15);
            case M.Post: return new Paint(3, 9, 0.5, 40);
            case M.Rail: return new Paint(3, 12, 0.6, 50);
            case M.Stand:
                return ny > 0.7 ? new Paint(3, 8 + 0.4 * (Rand((int)(x / 53), (int)(z / 53)) - 0.5)) : new Paint(3, 6);
            case M.Seat:
            {
                // the seats: blue, SLATE across rows 1 to 7 in yellow
                var q = -b - (R3 + Ft + Walkway);
                var j = (int)Math.Floor(q / RowD);
                var ci = (int)Math.Round(a / SeatPitch) + 14;
                if (Lettered("SLATE", ci, 7 - j)) return new Paint(6, 12, 0.35, 25);
                return new Paint(12, 8, 0.35, 25);
            }
            case M.Podium:
            {
                // (white tops, gold, silver and bronze sides with their numbers)
                if (ny > 0.7) return new Paint(3, 13, 0.2);
                var which = Array.FindIndex(Podium, p => x > p.X * Cell - 30 && x < (p.X + 2) * Cell + 30 && z > p.Z * Cell - 30 && z < (p.Z + 2) * Cell + 30 && y < FIn + p.Layers * Lay + 30);
                if (which < 0) which = 0;
                var side = which switch { 0 => new Paint(6, 11, 0.4), 1 => new Paint(3, 10, 0.5), _ => new Paint(1, 13, 0.4) };
                // (its number on the two faces toward the camera, +x and +z: on screen a +x face's left is its +z end, a +z face's left its
                // -x end; the first's high on its faces, over the second and third in front of them)
                var p0 = Podium[which];
                double across = nx > 0.7 ? (p0.Z + 1) * Cell - z : nz > 0.7 ? x - (p0.X + 1) * Cell : double.NaN;
                if (double.IsNaN(across)) return side;
                var gp = p0.Layers == 1 ? 30.0 : 34.0;
                var middle = which == 0 ? 1.5 * Lay : p0.Layers * Lay / 2;
                int gc = (int)Math.Floor(across / gp + 2.5), gr = (int)Math.Floor((middle + 3.5 * gp - (y - FIn)) / gp);
                if (gc >= 0 && gc < 5 && Lettered(which == 0 ? "1" : which == 1 ? "2" : "3", gc, gr)) return new Paint(3, 2);
                return side;
            }
            case M.Wall:
            {
                var door = DoorOf(ax, az, out var across, out var outward);
                if (door >= 0 && outward > Rw + 40 && Arch(across, y - FOut) < 10) return new Paint(12, 0.5);
                if (rho > Rw + Tw - 8 && Math.Abs(ny) < 0.5) return Cut(y);
                if (ny > 0.7) return new Paint(3, 9);
                if (y < BaseTop - 40) return new Paint(14, 9 + 0.5 * (Rand((int)(x / 61), (int)(y / 61), (int)(z / 61)) - 0.5));
                if (y < BaseTop) return new Paint(14, 12, 0.2);
                return Sky(x, y, z, rho);
            }
            case M.Column: return new Paint(3, 11 + 0.3 * (Rand((int)(y / 47), (int)(x / 47), (int)(z / 47)) - 0.5), 0.25, 20);
            case M.Capital: case M.Frame: return new Paint(6, 11, 0.6, 35);
            case M.Entab: return Math.Abs(y - (Ycap + 470)) < 25 ? new Paint(6, 11, 0.5) : new Paint(3, 10);
        }
        return new Paint(3, 8);
    }

    // a place's distance along the track, round from the near straight's left end (for the boards' joints and the panels)
    private static double Sigma(double a, double b, double d, double psi, double ea)
    {
        if (ea <= 0) return b >= 0 ? a : 2 * Lu + Math.PI * d + Lu - a;
        return a > 0 ? Lu + (HalfPi - psi) * d : 3 * Lu + Math.PI * d + (psi + HalfPi) * d;
    }

    // the boards: varnished pine along the track, each its own shade, their joints; the black, red and blue lines; the finish line across
    // the far straight, the 200 m line across the near, the pursuit lines' red halves in both straights' middles
    private static Paint Boards(double a, double b, double d, double psi, double ea)
    {
        var s = d - R2;
        if (s >= 170 && s < 250) return new Paint(3, 1, 0.3);
        if (s >= 620 && s < 700) return new Paint(4, 9, 0.3);
        if (s >= 1250 && s < 1330) return new Paint(12, 7, 0.3);
        if (ea <= 0)
        {
            if (b < 0 && Math.Abs(a - FinishA) < 150) return Math.Abs(a - FinishA) < 36 ? new Paint(3, 1, 0.3) : new Paint(3, 14, 0.3);
            if (b > 0 && Math.Abs(a - LineA200) < 28) return new Paint(3, 14, 0.3);
            if (Math.Abs(a) < 34 && s < Wd / 2) return new Paint(4, 9, 0.3);
        }
        var sigma = Sigma(a, b, d, psi, ea);
        const double board = 52;
        var bi = (int)Math.Floor(s / board);
        var off = Rand(bi, 7) * 2100;
        var along = (sigma + off) % 2100;
        if (along < 0) along += 2100;
        if (along < 12) return new Paint(6, 7, 0.3);
        if (s - bi * board < 5) return new Paint(6, 8.6, 0.3);
        var grain = 0.35 * Math.Sin(sigma * 0.011 + Rand(bi, 3) * 6.28) + 0.25 * (Rand(bi, (int)Math.Floor(sigma / 90)) - 0.5);
        return new Paint(6, 10.2 + 0.9 * (Rand(bi, 11) - 0.5) + grain, 0.35, 28);
    }

    private static readonly Paint[] Panels = { new(12, 6, 0.3), new(4, 7, 0.3), new(6, 11, 0.3), new(11, 5, 0.3) };
    // the bowl's outer face below the fence: sponsors' panels, a white band across them
    private static Paint Cladding(double sigma, double y)
    {
        var pi = (int)Math.Floor(sigma / 1800);
        var at = sigma - pi * 1800;
        if (at < 30) return new Paint(3, 2);
        var band = Math.Abs(y - (FOut + 500));
        var c = Panels[((pi % 4) + 4) % 4];
        if (band < 90 && y < FOut + 1800) return new Paint(3, 14, 0.3);
        return c;
    }

    // the hall's floor: slate tiles two cells square, each its own shade, grout between
    private static Paint Slate(double x, double z)
    {
        int tx = (int)Math.Floor(x / 1024), tz = (int)Math.Floor(z / 1024);
        double fx = x - tx * 1024, fz = z - tz * 1024;
        if (fx < 18 || fz < 18) return new Paint(11, 3);
        return new Paint(11, 8 + 1.2 * (Rand(tx, tz, 5) - 0.5) + 0.3 * (Rand((int)(x / 43), (int)(z / 43)) - 0.5), 0.25, 40);
    }

    // where the hall is cut away on the near side: stone in courses
    private static Paint Cut(double y) => (y % 256) < 18 ? new Paint(14, 3) : new Paint(14, 5);

    // the dome's sky between the columns: deep blue, darker up, with stars
    private static Paint Sky(double x, double y, double z, double rho)
    {
        var phi = Math.Atan2(z - Zc, x - Xc);
        int u = (int)Math.Floor(phi * Rw / 64), v = (int)Math.Floor(y / 64);
        var r = Rand(u, v, 9);
        if (r < 0.05)
        {
            double cu = (Rand(u, v, 1) - 0.5) * 30, cv = (Rand(u, v, 2) - 0.5) * 30;
            double du = phi * Rw - (u * 64 + 32 + cu), dv = y - (v * 64 + 32 + cv);
            if (du * du + dv * dv < (r < 0.012 ? 400 : 160)) return r < 0.02 ? new Paint(10, 15) : new Paint(3, 15);
        }
        var up = Math.Clamp((y - BaseTop) / (Hw - BaseTop), 0, 1);
        return new Paint(12, 3.4 - 2.6 * up + 0.4 * (r - 0.5));
    }

    // ================================================================================================= the picture
    // the camera: a point's place on the screen (pixels), x = 24 (X - Z) / 512, y = 12 (X + Z) / 512 - 15 Y / 256 -- the grid's own
    private const double DirX = -1 / 1.6248076809271921, DirY = -0.8 / 1.6248076809271921, DirZ = -1 / 1.6248076809271921;
    private static readonly (double X, double Y, double Z) Light = Norm(0.45, 1.0, -0.2), Half = Norm(0.45 / 1.1147 + 0.6154, 1.0 / 1.1147 + 0.4923, -0.2 / 1.1147 + 0.6154);
    private static (double, double, double) Norm(double x, double y, double z) { var l = Math.Sqrt(x * x + y * y + z * z); return (x / l, y / l, z / l); }

    private readonly record struct Sample(bool Hit, byte Ramp, float Shade, int Cell, M Mat, float Depth, float Nx, float Ny, float Nz);

    private static Sample Trace(double sx, double sy, bool lit)
    {
        const double y0 = 6500;
        double xmz = sx * 512 / 24, xpz = (sy + 15 * y0 / 256) * 512 / 12;
        double ox = (xpz + xmz) / 2, oz = (xpz - xmz) / 2, oy = y0;
        // (into the hall's cylinder)
        double px = ox - Xc, pz = oz - Zc, R = Rw + Tw + 20;
        double qa = DirX * DirX + DirZ * DirZ, qb = 2 * (px * DirX + pz * DirZ), qc = px * px + pz * pz - R * R;
        var disc = qb * qb - 4 * qa * qc;
        if (disc < 0) return default;
        double t0 = Math.Max(0, (-qb - Math.Sqrt(disc)) / (2 * qa)), t1 = Math.Min((-qb + Math.Sqrt(disc)) / (2 * qa), (oy + 60) / -DirY);
        var t = t0;
        for (var i = 0; i < 400 && t < t1; i++)
        {
            double x = ox + DirX * t, y = oy + DirY * t, z = oz + DirZ * t;
            var dist = Map(x, y, z, out var mat);
            if (dist < 0.6)
            {
                // the normal: the field's gradient
                const double e = 2.5;
                double nx = Dist(x + e, y, z) - Dist(x - e, y, z), ny = Dist(x, y + e, z) - Dist(x, y - e, z), nz = Dist(x, y, z + e) - Dist(x, y, z - e);
                var nl = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (nl < 1e-9) { nx = 0; ny = 1; nz = 0; } else { nx /= nl; ny /= nl; nz /= nl; }
                var paint = Look(mat, x, y, z, nx, ny, nz);
                // the cell its surface is in (just inside it)
                double ix = x - nx * 4, iy = y - ny * 4, iz = z - nz * 4;
                int cx = (int)Math.Floor(ix / Cell), cy = (int)Math.Floor(iy / Lay), cz = (int)Math.Floor(iz / Cell);
                var cell = cx < 0 || cz < 0 || cx >= Size || cz >= Size || cy < 0 || cy >= Height ? -1 : cx | cy << 6 | cz << 11;
                var light = 1.0;
                double shine = 0;
                if (lit)
                {
                    var ndl = nx * Light.X + ny * Light.Y + nz * Light.Z;
                    var shadow = ndl > 0 ? Shadow(x + nx * 6, y + ny * 6, z + nz * 6) : 0;
                    var ao = Occlusion(x, y, z, nx, ny, nz);
                    light = 0.42 * ao * (0.75 + 0.25 * ny) + 0.66 * Math.Max(0, ndl) * shadow;
                    var ndh = Math.Max(0, nx * Half.X + ny * Half.Y + nz * Half.Z);
                    shine = paint.Shine * Math.Pow(ndh, paint.Sharp) * shadow;
                }
                return new Sample(true, (byte)paint.Ramp, (float)Lit(paint.Ramp, paint.Shade, light, shine), cell, mat, (float)t, (float)nx, (float)ny, (float)nz);
            }
            t += Math.Max(dist * 0.8, 0.8);
        }
        return default;
    }

    private static double Shadow(double x, double y, double z)
    {
        double res = 1, t = 20;
        for (var i = 0; i < 56 && t < 7000; i++)
        {
            var h = Dist(x + Light.X * t, y + Light.Y * t, z + Light.Z * t);
            if (h < 0.5) return 0;
            res = Math.Min(res, 9 * h / t);
            t += Math.Clamp(h, 12, 450);
        }
        return Math.Clamp(res, 0, 1);
    }

    private static readonly double[] OccSteps = { 35, 90, 170, 280, 420 };
    private static double Occlusion(double x, double y, double z, double nx, double ny, double nz)
    {
        double occ = 0;
        for (var i = 0; i < OccSteps.Length; i++)
        {
            var h = OccSteps[i];
            occ += Math.Max(0, (h - Dist(x + nx * h, y + ny * h, z + nz * h)) / h) / (i + 1);
        }
        return Math.Clamp(1 - 0.6 * occ, 0.25, 1);
    }

    // the palette's ramps' brightness, for lighting a shade along its ramp
    private static double[,] lum = new double[16, 16];
    private static byte[] pal = new byte[768];
    private static void UsePalette(byte[] palette)
    {
        pal = palette;
        for (var r = 0; r < 16; r++)
            for (var s = 0; s < 16; s++)
            {
                var i = (r * 16 + s) * 3;
                lum[r, s] = (0.3 * palette[i] + 0.59 * palette[i + 1] + 0.11 * palette[i + 2]) / 255.0;
            }
    }
    private static double LumAt(int r, double f)
    {
        f = Math.Clamp(f, 0, 15);
        var i = (int)Math.Floor(f);
        if (i >= 15) return lum[r, 15];
        return lum[r, i] + (lum[r, i + 1] - lum[r, i]) * (f - i);
    }
    // the shade whose brightness is the lit one (shade `f` under light `light`, plus a shine)
    private static double Lit(int r, double f, double light, double shine) => ShadeOf(r, LumAt(r, f) * light + shine * 0.55);
    // the place along a ramp of a brightness
    private static double ShadeOf(int r, double target)
    {
        if (target <= lum[r, 0]) return 0;
        for (var s = 0; s < 15; s++)
            if (target <= lum[r, s + 1])
            {
                var span = lum[r, s + 1] - lum[r, s];
                return s + (span > 1e-6 ? (target - lum[r, s]) / span : 0);
            }
        return 15;
    }
    private static (double R, double G, double B) Rgb(int r, double f)
    {
        f = Math.Clamp(f, 0, 15);
        var i = (int)Math.Floor(f); var j = Math.Min(15, i + 1); var t = f - i;
        int a = (r * 16 + i) * 3, c = (r * 16 + j) * 3;
        return (pal[a] + (pal[c] - pal[a]) * t, pal[a + 1] + (pal[c + 1] - pal[a + 1]) * t, pal[a + 2] + (pal[c + 2] - pal[a + 2]) * t);
    }

    private static readonly int[] Bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

    // The hall as the room's camera sees it: each pixel's colour (palette index) and the cell it belongs to (-1: none). Pixel (0, 0) is the
    // screen's point (X0, Y0).
    internal sealed class Picture
    {
        public const int X0 = -1090, Y0 = -170, W = 2180, H = 1490;
        public readonly byte[] Index = new byte[W * H];
        public readonly int[] Cell = new int[W * H];
        public readonly byte[] Bgra = new byte[W * H * 4];
    }

    internal static Picture Render(byte[] palette)
    {
        UsePalette(palette);
        var pic = new Picture();
        const int W = Picture.W, H = Picture.H;
        var first = new Sample[W * H];
        // a sample at every pixel's middle
        Parallel.For(0, H, row =>
        {
            for (var col = 0; col < W; col++)
                first[row * W + col] = Trace(Picture.X0 + col + 0.5, Picture.Y0 + row + 0.5, true);
        });
        // four more where the picture changes (edges, lines, corners), the pixel the colour they make
        Parallel.For(0, H, row =>
        {
            for (var col = 0; col < W; col++)
            {
                var i = row * W + col;
                var s = first[i];
                var edge = false;
                for (var k = 0; k < 4 && !edge; k++)
                {
                    int r2 = row + (k == 0 ? -1 : k == 1 ? 1 : 0), c2 = col + (k == 2 ? -1 : k == 3 ? 1 : 0);
                    if (r2 < 0 || r2 >= H || c2 < 0 || c2 >= W) continue;
                    var o = first[r2 * W + c2];
                    if (o.Hit != s.Hit) { edge = true; break; }
                    if (!s.Hit) continue;
                    if (o.Ramp != s.Ramp || o.Mat != s.Mat || Math.Abs(o.Shade - s.Shade) > 2.5 || Math.Abs(o.Depth - s.Depth) > 80 || o.Nx * s.Nx + o.Ny * s.Ny + o.Nz * s.Nz < 0.9) edge = true;
                }
                var samples = edge
                    ? new[] { s, Trace(Picture.X0 + col + 0.25, Picture.Y0 + row + 0.25, true), Trace(Picture.X0 + col + 0.75, Picture.Y0 + row + 0.25, true),
                              Trace(Picture.X0 + col + 0.25, Picture.Y0 + row + 0.75, true), Trace(Picture.X0 + col + 0.75, Picture.Y0 + row + 0.75, true) }
                    : new[] { s };
                var hits = samples.Where(h => h.Hit && h.Cell >= 0).ToList();
                if (hits.Count * 2 <= samples.Length) { pic.Cell[i] = -1; continue; }
                // (the cell most of them are in, the nearest first when it's even)
                var cell = hits.GroupBy(h => h.Cell).OrderByDescending(g => g.Count()).ThenBy(g => g.Min(h => h.Depth)).First().Key;
                double r = 0, g = 0, bl = 0;
                foreach (var h in hits) { var c = Rgb(h.Ramp, h.Shade); r += c.R; g += c.G; bl += c.B; }
                r /= hits.Count; g /= hits.Count; bl /= hits.Count;
                // (the ramp among theirs whose shade comes nearest the mix, dithered between its two nearest shades)
                var bestErr = double.MaxValue; int bestRamp = hits[0].Ramp; double bestF = hits[0].Shade;
                foreach (var ramp in hits.Select(h => h.Ramp).Distinct())
                {
                    var f = hits.Any(h => h.Ramp != ramp) ? ShadeOf(ramp, (0.3 * r + 0.59 * g + 0.11 * bl) / 255.0) : hits.Average(h => (double)h.Shade);
                    var cc = Rgb(ramp, f);
                    var err = (cc.R - r) * (cc.R - r) + (cc.G - g) * (cc.G - g) + (cc.B - bl) * (cc.B - bl);
                    if (err < bestErr) { bestErr = err; bestRamp = ramp; bestF = f; }
                }
                int gx = Picture.X0 + col, gy = Picture.Y0 + row;
                var threshold = (Bayer[(gy & 3) * 4 + (gx & 3)] + 0.5) / 16;
                var shade = (int)Math.Floor(bestF) + (bestF - Math.Floor(bestF) > threshold ? 1 : 0);
                var index = (byte)(bestRamp * 16 + Math.Clamp(shade, 0, 15));
                pic.Index[i] = index;
                pic.Cell[i] = cell;
                pic.Bgra[i * 4] = palette[index * 3 + 2]; pic.Bgra[i * 4 + 1] = palette[index * 3 + 1]; pic.Bgra[i * 4 + 2] = palette[index * 3]; pic.Bgra[i * 4 + 3] = 255;
            }
        });
        return pic;
    }

    // ================================================================================================= the cells
    // The top of what Twinsen stands on (units): the bowl, the floor, the stands' rows, the podium; the wall and beyond it solid to the top
    // (but in the doors)
    private static double Ground(double x, double z)
    {
        double ax = x - Xc, az = z - Zc, rho = Math.Sqrt(ax * ax + az * az);
        var door = DoorOf(ax, az, out var across, out var outward);
        var inDoor = door >= 0 && Math.Abs(across) < DoorHalf - 40 && outward < Rw + Recess - 20;
        if (rho > Rw - 100 && !inDoor) return Height * Lay;
        Oval(x, z, out var a, out var b, out var d, out var psi, out _);
        var h = d <= R3 + Ft ? Deck(d, Bank(psi), out _) : FOut;
        var q = -b - (R3 + Ft + Walkway);
        if (q >= 0 && Math.Abs(a) <= StandHalf) h = Math.Max(h, FOut + (Math.Min(Math.Floor(q / RowD), Rows - 1) + 1) * RowR);
        foreach (var (px, pz, layers) in Podium)
            if (x >= px * Cell && x <= (px + 2) * Cell && z >= pz * Cell && z <= (pz + 2) * Cell) h = Math.Max(h, FIn + layers * Lay);
        return h;
    }

    // a cell's slope from which of its corners are a layer higher (bit 0 its (0, 0), 1 (1, 0), 2 (0, 1), 3 (1, 1) -- x, z): the game's
    // shapes 2-13 (OBJECT.CPP ReajustPos), 0 where none fits (the two saddles)
    private static readonly int[] ShapeOfCorners = { 0, 11, 12, 4, 13, 5, 0, 7, 10, 0, 2, 8, 3, 9, 6, 0 };

    // Each column's collision: the layer of its top cell and that cell's shape (the cells under it solid), and how high the fence's invisible
    // wall reaches in it (-1: none)
    private static (int Top, int Shape, int Fence)[,] Collision()
    {
        var corner = new int[Size + 1, Size + 1];
        for (var x = 0; x <= Size; x++)
            for (var z = 0; z <= Size; z++)
                corner[x, z] = (int)Math.Clamp(Math.Floor(Ground(x * Cell, z * Cell) / Lay + 0.5 + 1e-9), 0, Height);
        var cols = new (int, int, int)[Size, Size];
        for (var x = 0; x < Size; x++)
            for (var z = 0; z < Size; z++)
            {
                int h00 = corner[x, z], h10 = corner[x + 1, z], h01 = corner[x, z + 1], h11 = corner[x + 1, z + 1];
                int lo = Math.Min(Math.Min(h00, h10), Math.Min(h01, h11)), hi = Math.Max(Math.Max(h00, h10), Math.Max(h01, h11));
                int top, shape;
                var pattern = (h00 > lo ? 1 : 0) | (h10 > lo ? 2 : 0) | (h01 > lo ? 4 : 0) | (h11 > lo ? 8 : 0);
                if (hi == lo) { top = lo - 1; shape = 1; }
                else if (hi - lo == 1 && ShapeOfCorners[pattern] != 0) { top = lo; shape = ShapeOfCorners[pattern]; }
                else
                {
                    var mid = (int)Math.Clamp(Math.Floor(Ground((x + 0.5) * Cell, (z + 0.5) * Cell) / Lay + 0.5), 0, Height);
                    top = mid - 1; shape = 1;
                }
                // the fence across the cell (not in a gate): solid to over the rails
                var fence = -1;
                double cxw = (x + 0.5) * Cell, czw = (z + 0.5) * Cell;
                Oval(cxw, czw, out var a, out _, out var dmid, out var psi, out var ea);
                double dmin = double.MaxValue, dmax = 0;
                foreach (var (ux, uz) in new[] { (0, 0), (1, 0), (0, 1), (1, 1) })
                {
                    Oval((x + ux) * Cell, (z + uz) * Cell, out _, out _, out var dd, out _, out _);
                    dmin = Math.Min(dmin, dd); dmax = Math.Max(dmax, dd);
                }
                var fenceLine = R3 + Ft / 2;
                if (dmin <= fenceLine && dmax >= fenceLine && !(ea <= 0 && Math.Abs(a) < GateHalf + 200))
                    fence = Math.Min(Height - 1, (int)Math.Ceiling((TopEdge(Bank(psi)) + PostH + 300) / Lay) - 1);
                cols[x, z] = (top, shape, fence);
            }
        return cols;
    }

    // ================================================================================================= into the game
    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        if (!File.Exists(scenePath) || !File.Exists(bkgPath) || !File.Exists(ressPath)) return log;
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(Scene) || !store.SceneExists(Outside)) { log.Add($"The Dome's velodrome left out: no scene {Scene} or {Outside}"); return log; }
        var room = store.Load(Scene);
        if (room.Hero.X == Arrival.X && room.Hero.Y == Arrival.Y && room.Hero.Z == Arrival.Z) { log.Add("The Dome's velodrome: in already"); return log; }
        if (room.Actors.Count != 6 || room.Zones.Count(z => z.Type == 0 && z.Num == Outside) != 2) { log.Add($"The Dome's velodrome left out: scene {Scene} isn't the game's Dome of the Slate"); return log; }

        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Scene * 2 + 1 >= table.Length) { log.Add($"The Dome's velodrome left out: no grid for scene {Scene}"); return log; }
        var type = table[Scene * 2];
        var raw = bkg.Read(header.Gri + table[Scene * 2 + 1]);
        var palette = HqrArchive.Open(ressPath).Read(0);

        // ---- the picture, cut into its cells' bricks
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var pic = Render(palette);
        var images = new Dictionary<int, GphImage>();
        const int W = Picture.W, H = Picture.H;
        for (var row = 0; row < H; row++)
            for (var col = 0; col < W; col++)
            {
                var cell = pic.Cell[row * W + col];
                if (cell < 0) continue;
                int cx = cell & 63, cy = cell >> 6 & 31, cz = cell >> 11;
                int gx = Picture.X0 + col, gy = Picture.Y0 + row;
                // (the cell's brick: 48 x 38, its top corner the cell's top-back corner; a pixel past its bottom row goes to the cell under
                // it, or the one in front)
                foreach (var (dx, dy, dz) in new[] { (0, 0, 0), (0, -1, 0), (1, 0, 1), (1, -1, 1) })
                {
                    int x = cx + dx, y = cy + dy, z = cz + dz;
                    if (x >= Size || z >= Size || y < 0) continue;
                    int i = gx - 24 * (x - z) + 24, j = gy - 12 * (x + z) + 15 * (y + 1);
                    if (i < 0 || i >= 48 || j < 0 || j >= 38) continue;
                    var key = x | y << 6 | z << 11;
                    if (!images.TryGetValue(key, out var image)) images[key] = image = new GphImage(48, 38);
                    image.Set(i, j, pic.Index[row * W + col]);
                    break;
                }
            }
        var renderTime = clock.Elapsed.TotalSeconds;

        // ---- the cells: the collision under the picture, the bricks where it shows
        var cols = Collision();
        var bricks = new Dictionary<string, int>();
        var entries = new List<(int Brick, int Shape, int Sound)>();
        var entryOf = new Dictionary<(int, int, int), int>();
        var newBricks = 0;
        int BrickNumber(GphImage image)
        {
            var bytes = Citadel.CitadelMuseum.EncodeBrick(image);
            var key = Convert.ToBase64String(bytes);
            if (bricks.TryGetValue(key, out var n)) return n;
            if (header.MaxBrk + 1 > MapBricks.RaceEngineMaxBricks) throw new InvalidDataException("LBA_BKG.HQR holds as many bricks as the engine can");
            bkg.InsertAt(header.Brk + header.MaxBrk, bytes);
            header.MaxBrk += 1;
            newBricks++;
            return bricks[key] = header.MaxBrk - 1;
        }
        var set = new Dictionary<(int, int, int), (int Block, int Pos)>();
        for (var x = 0; x < Size; x++)
            for (var z = 0; z < Size; z++)
            {
                var (top, shape, fence) = cols[x, z];
                Oval((x + 0.5) * Cell, (z + 0.5) * Cell, out _, out _, out var dmid, out _, out _);
                // (the cells' footsteps: wood on the boards, stone elsewhere -- the low half of the byte; its high half the floor's game
                // code, 0: the game's own Dome floor's 1 kills, its starry void)
                var sound = dmid >= R2 && dmid <= R3 ? 0x01 : 0x04;
                for (var y = 0; y < Height; y++)
                {
                    var s = y < top ? 1 : y == top ? shape : 0;
                    if (y <= fence && y > top) s = 1;
                    if (images.TryGetValue(x | y << 6 | z << 11, out var image))
                    {
                        var e = (BrickNumber(image), s, sound);
                        if (!entryOf.TryGetValue(e, out var at)) { at = entries.Count; entries.Add(e); entryOf[e] = at; }
                        set[(x, y, z)] = (1 + at / 256, at % 256);
                    }
                    else if (s != 0) set[(x, y, z)] = (0, s);
                }
            }
        var blocks = (entries.Count + 255) / 256;
        if (blocks > 255) { log.Add($"The Dome's velodrome left out: {entries.Count} bricks' places need {blocks} blocks (255 the most)"); return log; }
        // the library: blocks of 8 x 4 x 8 places, each place one of the cells' bricks
        var library = new List<byte>();
        for (var i = 0; i < blocks; i++) library.AddRange(new byte[4]);
        for (var i = 0; i < blocks; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(library).Slice(i * 4, 4), (uint)library.Count);
            library.Add(8); library.Add(4); library.Add(8);
            for (var p = 0; p < 256; p++)
            {
                var at = i * 256 + p;
                var (brick, shape, sound) = at < entries.Count ? entries[at] : entries[0];
                library.Add((byte)shape); library.Add((byte)sound);
                library.Add((byte)((brick + 1) & 255)); library.Add((byte)((brick + 1) >> 8));
            }
        }
        var libraryBytes = library.ToArray();
        var libraryNumber = header.Brk - header.Bll;
        if (libraryNumber > 255) { log.Add("The Dome's velodrome left out: LBA_BKG.HQR has 256 block libraries already"); return log; }
        bkg.InsertAt(header.Brk, libraryBytes);
        header.Brk += 1;
        if (header.MaxBll < (uint)libraryBytes.Length) header.MaxBll = (uint)libraryBytes.Length;

        // ---- the grid (scene 26's, every cell anew)
        var shapeGrid = Lba2GridBackend.ToLba1Shape(raw);
        var edits = new List<Lba1GridCell>(Size * Size * Height);
        for (var z = 0; z < Size; z++)
            for (var y = 0; y < Height; y++)
                for (var x = 0; x < Size; x++)
                {
                    var (block, pos) = set.TryGetValue((x, y, z), out var c) ? c : (0, 0);
                    edits.Add(new Lba1GridCell(x, y, z, block, pos));
                }
        var cleared = (byte[])shapeGrid.Clone();
        Array.Clear(cleared, cleared.Length - 32, 32);
        byte[] grid;
        try { grid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(cleared, edits)); }
        catch (InvalidOperationException e) { log.Add($"The Dome's velodrome left out: {e.Message}"); return log; }
        grid[0] = (byte)libraryNumber;
        var gridNumber = header.Grm - header.Gri;
        if (gridNumber > 255) { log.Add("The Dome's velodrome left out: LBA_BKG.HQR has 256 grids already"); return log; }
        bkg.InsertAt(header.Grm, grid);
        header.Grm += 1; header.Bll += 1; header.Brk += 1;
        if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
        var (usedBytes, usedBricks) = MapBricks.UsedBrickBytes(grid, libraryBytes, n => n < header.MaxBrk && !bkg.IsEmpty(header.Brk + n) ? BinaryPrimitives.ReadUInt32LittleEndian(bkg.ExtentOf(header.Brk + n)) : 0);
        if (header.MaxBrickCube < (uint)((usedBricks + 1) * 4 + usedBytes)) header.MaxBrickCube = (uint)((usedBricks + 1) * 4 + usedBytes);
        if (header.MaxMaskCube < (uint)((usedBricks + 1) * 4 + usedBytes + usedBricks * 64)) header.MaxMaskCube = (uint)((usedBricks + 1) * 4 + usedBytes + usedBricks * 64);
        bkg.SetStored(0, header.Bytes);
        table = bkg.Read(header.Brk + header.MaxBrk);
        table[Scene * 2] = type; table[Scene * 2 + 1] = (byte)gridNumber;
        bkg.SetStored(header.Brk + header.MaxBrk, table);

        // ---- the scenes: the room's doors in its new wall, its actors on its new floor; outside, the way in arriving inside the door
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        var errors = new List<string>();
        byte[]? Rebuilt(int scene, SceneModel model)
        {
            var built = SceneScripts.Load(SceneSerializer.Write(model), scene).Build();
            if (!built.Ok) { errors.AddRange(built.Errors.Select(e => $"scene {scene}: {e}")); return null; }
            return built.Record;
        }
        var exits = room.Zones.Where(z => z.Type == 0 && z.Num == Outside).ToList();
        // (the game's: the first its back door, to the slate's garden; the second the way out by the way in)
        var backInfo = exits[0].Info; var mainInfo = exits[1].Info;
        room.Zones.RemoveAll(z => z.Type == 0 && z.Num == Outside);
        for (var i = 0; i < DoorAngles.Length; i++)
        {
            double cx = Math.Cos(DoorAngles[i]), cz = Math.Sin(DoorAngles[i]);
            // (the doorway: from in front of the wall to its back, across the door less a little -- walked up to, not into: the cells by
            // the wall hold its arch's and its moulding's pixels, and the engine stops a walker at any brick within his height, whatever
            // its shape, GRILLE.CPP WorldColBrickFull; the way in arrives 400 short of it)
            var corners = new List<(double X, double Z)>();
            foreach (var o in new[] { Rw - 700, Rw + Recess - 10 })
                foreach (var t in new[] { -(DoorHalf - 80), DoorHalf - 80 })
                    corners.Add((Xc + cx * o - cz * t, Zc + cz * o + cx * t));
            room.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = Outside,
                X0 = (int)corners.Min(c => c.X), X1 = (int)corners.Max(c => c.X), Z0 = (int)corners.Min(c => c.Z), Z1 = (int)corners.Max(c => c.Z),
                Y0 = (int)FOut - 100, Y1 = (int)FOut + 2000,
                Info = (int[])(i == 0 ? mainInfo : backInfo).Clone(),
            });
        }
        (room.Hero.X, room.Hero.Y, room.Hero.Z) = Arrival;
        // (the clover box on the winners' step; the three pots: either end of the infield and on the walkway behind the far straight)
        void Place(int actor, (int X, int Y, int Z) at) { var a = room.Actors[actor]; (a.X, a.Y, a.Z) = at; }
        Place(2, ((Podium[0].X + 1) * (int)Cell, (int)FIn + Podium[0].Layers * (int)Lay, (Podium[0].Z + 1) * (int)Cell));
        Place(3, AtOval(-Lu - 1000, 0, FIn));
        Place(4, AtOval(Lu + 1000, 0, FIn));
        Place(5, AtOval(1800, -(R3 + Ft + Walkway / 2), FOut));
        var roomRecord = Rebuilt(Scene, room);
        var outside = store.Load(Outside);
        var door = outside.Zones.Where(z => z.Type == 0 && z.Num == Scene).ToList();
        if (door.Count == 1) { door[0].Info[0] = ArrivalInfo.X; door[0].Info[1] = ArrivalInfo.Y; door[0].Info[2] = ArrivalInfo.Z; }
        else errors.Add($"scene {Outside}: {door.Count} doors into the Dome, not one");
        var outsideRecord = Rebuilt(Outside, outside);
        if (roomRecord is null || outsideRecord is null || errors.Count > 0) { log.Add($"The Dome's velodrome left out: {string.Join("; ", errors)}"); return log; }
        scenes.SetStored(Scene + 1, roomRecord);
        scenes.SetStored(Outside + 1, outsideRecord);
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        log.Add($"The Dome of the Slate's velodrome (scene {Scene}): rendered in {renderTime:0.0} s, {images.Count} cells drawn, {newBricks} bricks, " +
                $"{entries.Count} places in {blocks} blocks (library {libraryNumber}), grid {gridNumber} ({grid.Length} bytes); the way in from scene {Outside} " +
                $"arrives at ({ArrivalInfo.X}, {ArrivalInfo.Y}, {ArrivalInfo.Z}) less where in its door he was");
        return log;
    }

    internal static string DebugPixel(byte[] palette, double sx, double sy)
    {
        UsePalette(palette);
        var smp = Trace(sx, sy, true);
        const double y0 = 6500;
        double xmz = sx * 512 / 24, xpz = (sy + 15 * y0 / 256) * 512 / 12;
        double ox = (xpz + xmz) / 2, oz = (xpz - xmz) / 2;
        var t = smp.Depth;
        double x = ox + DirX * t, y = y0 + DirY * t, z = oz + DirZ * t;
        var ndl = smp.Nx * Light.X + smp.Ny * Light.Y + smp.Nz * Light.Z;
        return $"hit {smp.Hit} mat {smp.Mat} at ({x:0},{y:0},{z:0}) n ({smp.Nx:0.00},{smp.Ny:0.00},{smp.Nz:0.00}) ramp {smp.Ramp} shade {smp.Shade:0.00} cell {smp.Cell} ndl {ndl:0.00} " +
               $"shadow {Shadow(x + smp.Nx * 6, y + smp.Ny * 6, z + smp.Nz * 6):0.00} ao {Occlusion(x, y, z, smp.Nx, smp.Ny, smp.Nz):0.00}";
    }

    // the hall's picture as the model draws it, before it is cut into bricks (ScriptRoundTrip velodromepicture)
    public static (byte[] Bgra, int Width, int Height) Preview(byte[] palette)
    {
        var pic = Render(palette);
        return (pic.Bgra, Picture.W, Picture.H);
    }
}
