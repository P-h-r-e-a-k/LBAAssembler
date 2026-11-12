using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The Tavern's dartboard (the user, 2026-11-08: "a mini-game, Twinsen to be able to play a game of darts in the tavern"; RACEMOD.CPP's DartFrame
// plays the game): a board on a wooden post standing on the Tavern's floor, the board turned to the room (the body's own +z faces the
// player). The standard board: twenty numbers round it in the usual order, black and cream singles, the double and treble rings red and green
// (their colours swapping with the number's), a green outer bull and a red bull.
internal static partial class CitadelProps
{
    public const int Dartboard = 126;
    // (the board's radius and how high its middle is over the floor; where the post stands in the room is the scene's)
    public const int DartRadius = 460, DartMiddleHigh = 1400;
    // the standard order round from the top, clockwise
    public static readonly int[] DartOrder = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };
    // the rings' edges as fractions of the radius: the bull, the outer bull, the inner single, the treble, the outer single, the double
    public static readonly double[] DartRings = { 0.045, 0.11, 0.58, 0.65, 0.91, 1.0 };
    private const int DartBlack = 48, DartCream = 63, DartRed = 70, DartGreen = 133, DartWood = 22;

    public static Body BuildDartboard(byte[] header)
    {
        var m = new CarMesh("the Tavern's dartboard", new[] { -1 });
        m.Light = 0.9f;
        // (hung on a wall: a round wooden backing behind it, no post)
        // the backing, a little wider than the board
        Vector3 At(double r, double a) => new((float)(r * DartRadius * Math.Sin(a)), (float)(DartMiddleHigh + r * DartRadius * Math.Cos(a)), 0);
        m.Plate(0, DartBlack, Enumerable.Range(0, 24).Select(k => At(1.12, k * 2 * Math.PI / 24)).ToArray()[..4]);
        for (var s = 0; s < 20; s++)
        {
            double a0 = (s - 0.5) * Math.PI / 10, a1 = (s + 0.5) * Math.PI / 10;
            var dark = s % 2 == 0;
            // (a ring's slice: from r0 to r1 across the sector, in the colour given)
            void Slice(double r0, double r1, int colour) => m.Plate(0, colour, At(r0, a0), At(r0, a1), At(r1, a1), At(r1, a0));
            Slice(DartRings[1], DartRings[2], dark ? DartBlack : DartCream);
            Slice(DartRings[2], DartRings[3], dark ? DartRed : DartGreen);
            Slice(DartRings[3], DartRings[4], dark ? DartBlack : DartCream);
            Slice(DartRings[4], DartRings[5], dark ? DartRed : DartGreen);
        }
        // the bulls: the outer, a ring of twelve; the bull in its middle
        void Disc(double r, int colour)
        {
            const int n = 12;
            for (var k = 0; k < n; k += 2)
            {
                double a0 = k * 2 * Math.PI / n, a1 = (k + 1) * 2 * Math.PI / n, a2 = (k + 2) * 2 * Math.PI / n;
                m.Plate(0, colour, At(0, 0), At(r, a0), At(r, a1), At(r, a2));
            }
        }
        Disc(DartRings[1], DartGreen);
        Disc(DartRings[0], DartRed);
        return m.ToBody(header);
    }
}
