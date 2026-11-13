using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The Tavern's dartboard (the user, 2026-11-08: "a mini-game, Twinsen to be able to play a game of darts in the tavern"; RACEMOD.CPP's DartFrame
// plays the game): a board hung on the Tavern's wall, turned to the room (the body's own +z faces the player). The standard board: twenty
// numbers round it in the usual order, black and cream singles, the double and treble rings red and green (their colours swapping with the
// number's), a green outer bull and a red bull. Standing off the wall in a thick dark rim, like the target in Twinsen's house (the user,
// 2026-11-12: "The dart board we've added to the bar looks very flat ... Let's make ours slightly bigger and add a border to it also"); beside
// it a blackboard for the scores, and on the floor in front the oche, the line the throwers stand behind.
internal static partial class CitadelProps
{
    public const int Dartboard = 126, DartScoreBoard = 127, DartOche = 96;
    // (the board's radius, how high its middle is over the floor -- lower, the user, 2026-11-12: "compared to Twinsen it's very high" -- and how
    // far its face stands off the wall; where it hangs in the room is the scene's)
    public const int DartRadius = 520, DartMiddleHigh = 1060, DartStandOff = 70;
    // the standard order round from the top, clockwise
    public static readonly int[] DartOrder = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };
    // the rings' edges as fractions of the radius: the bull, the outer bull, the inner single, the treble, the outer single, the double
    public static readonly double[] DartRings = { 0.045, 0.11, 0.58, 0.65, 0.91, 1.0 };
    // (the rim's outer edge, as a fraction of the radius, and how far it stands proud of the board's face)
    private const double DartRimOut = 1.2;
    private const int DartRimProud = 22;
    private const int DartBlack = 48, DartCream = 63, DartRed = 70, DartGreen = 133, DartWood = 22, DartRim = 50, DartChalk = 63, DartSlate = 130;
    // the blackboard: its width and height, how high its bottom is over the floor
    public const int DartSlateWide = 760, DartSlateHigh = 900, DartSlateFrom = 620;
    // the oche: its length across the board's facing and its width
    public const int DartOcheLong = 1100, DartOcheWide = 60;

    public static Body BuildDartboard(byte[] header)
    {
        var m = new CarMesh("the Tavern's dartboard", new[] { -1 });
        m.Light = 0.9f;
        // (the points shared round rings of forty -- every sector's edge and middle -- so the board, its rim and its bulls stay under the engine's
        // 550 points; every face one-sided, toward the room)
        const int n = 40;
        Vector3 At(double r, int k, double z)
        {
            var a = (k - 1) * Math.PI / 20;   // (k even: a sector's edge; odd: its middle -- sector s from k = 2s to 2s + 2)
            return new((float)(r * DartRadius * Math.Sin(a)), (float)(DartMiddleHigh + r * DartRadius * Math.Cos(a)), (float)z);
        }
        int[] Round(double r, double z) => Enumerable.Range(0, n).Select(k => m.P(0, At(r, k, z))).ToArray();
        var front = new Vector3(0, DartMiddleHigh, 10000);
        void Face(int[] hs, int colour) => m.In(hs, colour, front);
        var face = DartRings.Skip(1).Select(r => Round(r, DartStandOff)).ToArray();   // 0.11, 0.58, 0.65, 0.91, 1.0
        int[] lipTop = Round(1.0, DartStandOff + DartRimProud), rimTop = Round(DartRimOut, DartStandOff + DartRimProud), rimBack = Round(DartRimOut, 0);
        for (var k = 0; k < n; k++)
        {
            var k1 = (k + 1) % n;
            // the rim's face, standing proud; its outer side back to the wall; its lip down onto the board
            Face(new[] { lipTop[k], lipTop[k1], rimTop[k1], rimTop[k] }, DartRim);
            m.Out(new[] { rimTop[k], rimTop[k1], rimBack[k1], rimBack[k] }, DartBlack, new Vector3(0, DartMiddleHigh, DartStandOff / 2f));
            m.In(new[] { face[4][k], face[4][k1], lipTop[k1], lipTop[k] }, DartBlack, new Vector3(0, DartMiddleHigh, DartStandOff + DartRimProud));
        }
        // the board's face: each sector's four rings, each slice two quads (its edge to its middle, its middle to its other edge)
        for (var s = 0; s < 20; s++)
        {
            var dark = s % 2 == 0;
            int ka = 2 * s, km = 2 * s + 1, kb = (2 * s + 2) % n;
            void Slice(int ring, int colour)
            {
                Face(new[] { face[ring][ka], face[ring][km], face[ring + 1][km], face[ring + 1][ka] }, colour);
                Face(new[] { face[ring][km], face[ring][kb], face[ring + 1][kb], face[ring + 1][km] }, colour);
            }
            Slice(0, dark ? DartBlack : DartCream);
            Slice(1, dark ? DartRed : DartGreen);
            Slice(2, dark ? DartBlack : DartCream);
            Slice(3, dark ? DartRed : DartGreen);
        }
        // the bulls: the outer a fan of triangles on the 0.11 ring's points, the bull a little fan a hair proud of it
        var middle = m.P(0, new Vector3(0, DartMiddleHigh, DartStandOff));
        for (var k = 0; k < n; k++) Face(new[] { middle, face[0][k], face[0][(k + 1) % n] }, DartGreen);
        var bullMiddle = m.P(0, new Vector3(0, DartMiddleHigh, DartStandOff + 3));
        var bull = Enumerable.Range(0, 10).Select(k => m.P(0, At(DartRings[0], k * 4, DartStandOff + 3))).ToArray();
        for (var k = 0; k < 10; k++) Face(new[] { bullMiddle, bull[k], bull[(k + 1) % 10] }, DartRed);
        return m.ToBody(header);
    }

    // The blackboard beside the board, for the scores (the user, 2026-11-12: "dart scores being written on a blackboard at the side of the dart
    // board"): a dark slate in a wooden frame, hung flat on the wall (its face +z), its two columns ruled in chalk -- the numbers themselves the
    // game's, chalked on the screen beside the board (RACEMOD.CPP DartDraw)
    public static Body BuildDartScoreBoard(byte[] header)
    {
        var m = new CarMesh("the Tavern's darts blackboard", new[] { -1 });
        m.Light = 0.85f;
        float w = DartSlateWide / 2f, y0 = DartSlateFrom, y1 = DartSlateFrom + DartSlateHigh;
        const float frame = 45, z = 30;
        // the frame, standing off the wall
        m.Box(0, new Vector3(0, y1 - frame / 2, z / 2), new Vector3(DartSlateWide, frame, z), DartWood);
        m.Box(0, new Vector3(0, y0 + frame / 2, z / 2), new Vector3(DartSlateWide, frame, z), DartWood);
        m.Box(0, new Vector3(-w + frame / 2, (y0 + y1) / 2, z / 2), new Vector3(frame, DartSlateHigh, z), DartWood);
        m.Box(0, new Vector3(w - frame / 2, (y0 + y1) / 2, z / 2), new Vector3(frame, DartSlateHigh, z), DartWood);
        // the slate, a little back from the frame's face
        m.Plate(0, DartSlate, new(-w + frame, y0 + frame, z - 8), new(w - frame, y0 + frame, z - 8), new(w - frame, y1 - frame, z - 8), new(-w + frame, y1 - frame, z - 8));
        // the chalk: a line under the names, one down the middle, and a few struck-through scores in each column
        void Chalk(float xa, float ya, float xb, float yb) =>
            m.Plate(0, DartChalk, new(xa, ya - 6, z - 6), new(xb, yb - 6, z - 6), new(xb, yb + 6, z - 6), new(xa, ya + 6, z - 6));
        Chalk(-w + frame + 30, y1 - frame - 150, w - frame - 30, y1 - frame - 150);
        m.Plate(0, DartChalk, new(-6, y0 + frame + 40, z - 6), new(6, y0 + frame + 40, z - 6), new(6, y1 - frame - 40, z - 6), new(-6, y1 - frame - 40, z - 6));
        for (var row = 0; row < 3; row++)
        {
            var y = y1 - frame - 230 - row * 110;
            Chalk(-w + frame + 60, y, -60, y);
            Chalk(60, y, w - frame - 60, y);
        }
        return m.ToBody(header);
    }

    // The oche: a white line painted on the floor in front of the board, across its facing (the user, 2026-11-12: "We should also have a line on
    // the floor where the players have to stand behind to throw the darts") -- flat, a hair over the floor, walked over
    public static Body BuildDartOche(byte[] header)
    {
        var m = new CarMesh("the Tavern's oche", new[] { -1 });
        m.Light = 1f;
        float l = DartOcheLong / 2f, h = DartOcheWide / 2f;
        m.Plate(0, DartChalk, new(-l, 4, -h), new(l, 4, -h), new(l, 4, h), new(-l, 4, h));
        return m.ToBody(header);
    }
}
