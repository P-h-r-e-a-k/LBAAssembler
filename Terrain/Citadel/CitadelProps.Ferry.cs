using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The car ferry between Citadel Island and the White Leaf Desert (the user, 2026-11-18: "We need to redesign the ferry between Citadel
// and Desert Island into one that Twinsen can drive onto and off again"): in the game's hovercraft's place -- its actor still the one its
// tracks move (RACEMOD.CPP car_ferry=: the hovercraft hidden, this body kept where it is) -- a red hull, an open deck at the quays'
// height, a ramp out along each long side (the quays meet it side on), rails at the ends and a wheelhouse over the bow. The body's own z
// along the ferry, x across it; its origin at the waterline, as the hovercraft's.
internal static partial class CitadelProps
{
    public const int CarFerry = 195, CarFerryTall = 196;
    // (the deck's top over the waterline -- Citadel's pier's own height, 746: 6 under it the pier's side was a wall to the car coming off, 4 over it a step the car on could not take -- its half width and half length; the wheelhouse's end; the ramps'
    // reach past the hull, down to the quays' edge)
    // (the Desert's quay stands 1,360 up, its ferry docked at the waterline (0): the same ferry with a deeper hull, CarFerryTall, its deck that much higher)
    public const int FerryDeckTall = 1360;
    // (... and docked a wider gap from its quay: the ramp on its quay's side, +x, that much longer -- the car fell in at the 700 one's end)
    public const int FerryRampTall = 2100;
    public const int FerryDeck = 746, FerryHalfWide = 1300, FerryHalfLong = 2400, FerryHouseFrom = 1300, FerryRamp = 700;
    // (the rails and the wheelhouse light greys, 54 and 55 -- the greys' row, 48-63, of Citadel's palette (RESS.HQR 27), with room for the
    // light's steps: 14 and 15, the palette's first row, aren't a ramp -- lit, Software drew the wheelhouse pink and OpenGL purple; at 48
    // both drew it black, 2026-11-19; the roof unlit, 58)
    private const int FerryHull = 4 * 16 + 9, FerryHullDark = 4 * 16 + 12, FerryDeckColour = 52, FerryRail = 54, FerryHouse = 55, FerryRoof = 58, FerryGlass = 158, FerryRampColour = 50;

    // (no box: the car drives onto its deck -- the deck is the car's floor, RACEMOD.CPP car_ferry= -- not into an actor's box)
    public static short[]? FerryBox(int generic) => generic is CarFerry or CarFerryTall ? new short[] { -1, 0, -1, 1, 1, 1 } : null;

    public static Body BuildCarFerry(byte[] header) => FerryBody(header, "the car ferry", FerryDeck, FerryRamp);
    public static Body BuildCarFerryTall(byte[] header) => FerryBody(header, "the car ferry, at the Desert's quay", FerryDeckTall, FerryRampTall);
    private static Body FerryBody(byte[] header, string name, int FerryDeck, int rampPlus)
    {
        var m = new CarMesh(name, new[] { -1 });
        m.Light = 0.8f;
        // the hull: from under the water up to the deck, narrowing a little at the bow and the stern
        m.Box(0, new Vector3(0, (FerryDeck - 260) / 2f - 130, 0), new Vector3(2 * FerryHalfWide, FerryDeck - 260 + 260, 2 * FerryHalfLong), FerryHull);
        m.Box(0, new Vector3(0, -60, 0), new Vector3(2 * FerryHalfWide - 120, 120, 2 * FerryHalfLong - 120), FerryHullDark);
        // the deck plate, its edges a band of the hull's darker red
        m.Box(0, new Vector3(0, FerryDeck - 15, 0), new Vector3(2 * FerryHalfWide - 40, 30, 2 * FerryHalfLong - 40), FerryDeckColour);
        // the ramps: a plate out from each long side at the deck's height, the quays' edge under its far end
        foreach (var side in new[] { 1, -1 })
        {
            var ramp = side > 0 ? rampPlus : FerryRamp;
            m.Box(0, new Vector3(side * (FerryHalfWide + ramp / 2f), FerryDeck - 20, -300), new Vector3(ramp, 30, 1800), FerryRampColour);
        }
        // the stern's rail (the car driven on across the deck, not off its end)
        m.Box(0, new Vector3(0, FerryDeck + 160, -FerryHalfLong + 40), new Vector3(2 * FerryHalfWide - 80, 40, 40), FerryRail);
        foreach (var x in new[] { -FerryHalfWide + 60, 0, FerryHalfWide - 60 })
            m.Box(0, new Vector3(x, FerryDeck + 80, -FerryHalfLong + 40), new Vector3(40, 160, 40), FerryRail);
        // the wheelhouse over the bow: walls, windows all round, a flat roof
        float h0 = FerryDeck, h1 = FerryDeck + 900, zc = (FerryHouseFrom + FerryHalfLong) / 2f, zl = FerryHalfLong - FerryHouseFrom;
        // (its walls lit, its roof a light grey as it is, unlit: lit, the light the roof takes ran any grey of the row past its end -- dark
        // red in OpenGL, at 48 nearly black in both -- and a lit top under an unlit one still showed in OpenGL, which draws a body's lit
        // polygons after its unlit ones)
        {
            float hx = FerryHalfWide - 150, z0 = zc - zl / 2f, z1 = zc + zl / 2f;
            int C(float x, float y, float z) => m.P(0, new Vector3(x, y, z));
            var middle = new Vector3(0, (h0 + h1) / 2, zc);
            m.Out(new[] { C(-hx, h0, z0), C(hx, h0, z0), C(hx, h1, z0), C(-hx, h1, z0) }, FerryHouse, middle);
            m.Out(new[] { C(-hx, h0, z1), C(hx, h0, z1), C(hx, h1, z1), C(-hx, h1, z1) }, FerryHouse, middle);
            m.Out(new[] { C(-hx, h0, z0), C(-hx, h0, z1), C(-hx, h1, z1), C(-hx, h1, z0) }, FerryHouse, middle);
            m.Out(new[] { C(hx, h0, z0), C(hx, h0, z1), C(hx, h1, z1), C(hx, h1, z0) }, FerryHouse, middle);
            m.Up(new[] { C(-hx, h1, z0), C(hx, h1, z0), C(hx, h1, z1), C(-hx, h1, z1) }, FerryRoof, unlit: true);
        }
        m.Box(0, new Vector3(0, h0 + 620, FerryHouseFrom - 2), new Vector3(2 * FerryHalfWide - 500, 220, 6), FerryGlass);
        foreach (var side in new[] { 1, -1 })
            m.Box(0, new Vector3(side * (FerryHalfWide - 150 + 2), h0 + 620, zc), new Vector3(6, 220, zl - 200), FerryGlass);
        m.Box(0, new Vector3(0, h1 + 30, zc), new Vector3(2 * FerryHalfWide - 200, 60, zl + 100), FerryHullDark);
        return m.ToBody(header);
    }
}
