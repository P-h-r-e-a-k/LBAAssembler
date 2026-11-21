using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The cycle shop in the baggage claim's first room (scene 5 -- RaceTrackStory.CycleShop.cs; the user, 2026-11-20: "Let's change 'Citadel
// Island, Baggage claim building, 1st scene (room #7)' into a cycle sale and repair shop ... He should also offer bikes for sale that Twinsen
// will never be able to afford 1000, 2000, 5000"; and "Let's add the hoverboard as a used item left behind in a corner of the cycle shop"):
//   - three bikes for sale, Twinsen's and Zoe's bike (BuildBike) in other colours -- the town bike green, the racing bike gold, the
//     champion's teal;
//   - the Hoverboard left behind, standing on its tail and leaning back against the wall behind it (its back, -z, to the wall), its deck
//     to the room: the item's own board (CitadelHoverboard.Faces), the same as Twinsen rides.
internal static partial class CitadelProps
{
    public const int ShopBikeTown = 211, ShopBikeRacing = 212, ShopBikeChampion = 213, LeaningBoard = 214;
    // (since the shop became a room of its own, CitadelCycleShop -- the user, 2026-11-21: "Let's just design an awesome cycle shop": its back
    // doorway through to the baggage claim, the repair stand a bike is clamped on, the wall of wheels for sale)
    public const int ShopDoorway = 215, RepairStand = 216, WheelWall = 217, ShopSign = 218;
    private const int ShopGold = 106, ShopTeal = 150, ShopSteel = 56, ShopRubber = 48, ShopWood = 20, ShopWoodLight = 25, ShopDark = 49;
    // (how far the board leans back from upright, and how far its foot stands out from the wall: its top against it)
    private const float BoardLean = 0.26f;

    private static IEnumerable<(int, string, Func<byte[], Body>)> CycleShopBodies() => new (int, string, Func<byte[], Body>)[]
    {
        (ShopBikeTown, "the cycle shop's town bike", h => BuildBike(h, "the town bike", Green, true)),
        (ShopBikeRacing, "the cycle shop's racing bike", h => BuildBike(h, "the racing bike", ShopGold, false)),
        (ShopBikeChampion, "the cycle shop's champion's bike", h => BuildBike(h, "the champion's bike", ShopTeal, false)),
        (LeaningBoard, "the Hoverboard, left behind in the cycle shop", BuildLeaningBoard),
        (ShopDoorway, "the cycle shop's back doorway", BuildShopDoorway),
        (RepairStand, "the cycle shop's repair stand", BuildRepairStand),
        (WheelWall, "the cycle shop's wall of wheels", BuildWheelWall),
        (ShopSign, "the cycle shop's sign", BuildShopSign),
    };

    // The shop's sign: CYCLES in a 3 x 5 pixel font, its letters gold on a teal board high on the north wall's face (z 0), facing +z, its
    // foot at the floor (the board SignLow up)
    private static readonly string[] SignLetters =
    {
        ".## #.# .## #.. ### .##",
        "#.. #.# #.. #.. #.. #..",
        "#.. .#. #.. #.. ##. .#.",
        "#.. .#. #.. #.. #.. ..#",
        ".## .#. .## ### ### ##.",
    };
    private const float SignLow = 1550, SignPixel = 70;
    private static Body BuildShopSign(byte[] header)
    {
        var m = new CarMesh("the cycle shop's sign", new[] { -1 });
        m.Light = 0.9f;
        var cols = SignLetters[0].Length;
        float w = cols * SignPixel + 2 * SignPixel, h = SignLetters.Length * SignPixel + 2 * SignPixel;
        m.Box(0, new Vector3(0, SignLow + h / 2, 20), new Vector3(w, h, 40), ShopTeal);
        for (var r = 0; r < SignLetters.Length; r++)
            for (var c = 0; c < cols; c++)
                if (SignLetters[r][c] == '#')
                {
                    float x = -w / 2 + SignPixel * (c + 1.5f), y = SignLow + h - SignPixel * (r + 1.5f);
                    m.Box(0, new Vector3(x, y, 50), new Vector3(SignPixel - 6, SignPixel - 6, 20), ShopGold);
                }
        return m.ToBody(header);
    }

    // The way through to the back -- the baggage claim behind the shop: a dark opening in a wooden frame, flat on the west wall's face (x 0),
    // facing +x into the shop, its middle at z 0 (as the club's back rooms' doorway, WingDoorway)
    private static Body BuildShopDoorway(byte[] header)
    {
        var m = new CarMesh("the cycle shop's back doorway", new[] { -1 });
        m.Light = 0.85f;
        const float Half = 450, Top = 1800, Rim = 80;
        void Face(float z0, float z1, float y0, float y1, float off, int colour, bool unlit = false) =>
            m.In(new[] { m.P(0, new Vector3(off, y0, z0)), m.P(0, new Vector3(off, y0, z1)), m.P(0, new Vector3(off, y1, z1)), m.P(0, new Vector3(off, y1, z0)) },
                 colour, new Vector3(off + 500, (y0 + y1) / 2, (z0 + z1) / 2), unlit);
        Face(-Half, Half, 0, Top, 20, ShopDark, true);
        Face(-Half - Rim, Half + Rim, Top, Top + 100, 50, ShopWood, true);
        Face(-Half - Rim, -Half, 0, Top, 50, ShopWood, true);
        Face(Half, Half + Rim, 0, Top, 50, ShopWood, true);
        // (a sign over it: STAFF ONLY's plank, lighter wood)
        Face(-300, 300, Top + 140, Top + 320, 45, ShopWoodLight, true);
        return m.ToBody(header);
    }

    // The repair stand: a cross of feet, a post, an arm out over the bike's middle and its clamp (the bike itself a ShopBike* prop lifted onto
    // it). Its arm along x; its foot at 0
    public const int StandHigh = 950;
    private static Body BuildRepairStand(byte[] header)
    {
        var m = new CarMesh("the cycle shop's repair stand", new[] { -1 });
        m.Light = 0.8f;
        m.Box(0, new Vector3(0, 20, 0), new Vector3(700, 40, 60), ShopSteel);
        m.Box(0, new Vector3(0, 20, 0), new Vector3(60, 40, 700), ShopSteel);
        m.Box(0, new Vector3(0, StandHigh / 2f, 0), new Vector3(70, StandHigh, 70), ShopSteel);
        m.Box(0, new Vector3(0, StandHigh, 0), new Vector3(110, 60, 260), Red);
        return m.ToBody(header);
    }

    // The wall of wheels: a pegboard on the north wall's face (z 0), facing +z, four bike wheels hung on it in two rows (six went past the
    // engine's points) (each a rim of short
    // lengths round its hub, as the bikes' are), its foot at 0
    private static Body BuildWheelWall(byte[] header)
    {
        var m = new CarMesh("the cycle shop's wall of wheels", new[] { -1 });
        m.Light = 0.8f;
        const float W = 2400, Low = 500, High = 2300, R = 260, Off = 40;
        m.Box(0, new Vector3(0, (Low + High) / 2, 15), new Vector3(W, High - Low, 30), ShopWoodLight);
        foreach (var row in new[] { Low + 420f, Low + 1250f })
            foreach (var col in new[] { -W / 4, W / 4 })
            {
                const int N = 10;
                for (var k = 0; k < N; k++)
                {
                    float a0 = k * MathF.Tau / N, a1 = (k + 1) * MathF.Tau / N;
                    m.Strut(0, new Vector3(col + R * MathF.Cos(a0), row + R * MathF.Sin(a0), Off + 40), new Vector3(col + R * MathF.Cos(a1), row + R * MathF.Sin(a1), Off + 40), 34, ShopRubber);
                }
                m.Box(0, new Vector3(col, row, Off + 40), new Vector3(60, 60, 50), ShopSteel);
            }
        return m.ToBody(header);
    }

    private static Body BuildLeaningBoard(byte[] header)
    {
        var m = new CarMesh("the Hoverboard, leaning on a wall", new[] { -1 });
        m.Light = 1f;
        // (the board's frame -- x across, y its deck's way, z its nose -- stood on its tail: its nose up, its deck to +z; a turn, not a
        // mirror, as the inventory's model (CitadelHoverboard.Model); then leant back, its top to -z)
        var lean = Matrix4x4.CreateRotationX(-BoardLean);
        Vector3 Stood(Vector3 v) => Vector3.Transform(new Vector3(-v.X, v.Z, v.Y), lean);
        var faces = CitadelHoverboard.Faces(CitadelHoverboard.Plain);
        var all = faces.SelectMany(f => f.Corners).Select(Stood).ToList();
        // (its foot on the floor, its top's back at z 0 -- against the wall behind the actor's place)
        var shift = new Vector3(0, -all.Min(p => p.Y), -all.Min(p => p.Z));
        foreach (var (corners, colour, lit) in faces)
            m.Raw(corners.Select(c => m.P(0, Stood(c) + shift)).ToArray(), colour, unlit: !lit);
        return m.ToBody(header);
    }
}
