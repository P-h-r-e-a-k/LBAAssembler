using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// Stella Entities, the club under the Emerald Moon's reactor (StellaEntities; the user, 2026-11-11: "Can we make the room actually round
// instead of with a jagged edge"). A scene's bricks are whole cells, so the round room's edge is made of props over Rick's floor tiles,
// each body round the room's middle (its actor stands there), drawn into the backdrop (OBJ_BACKGROUND: everyone is drawn over it, as
// over bricks) and colliding with nothing (ClubBox: its box far over the room) -- the invisible cells of the scene's grid hold Twinsen in:
//   - the wall: Rick's bar's look -- dark stone to ClubStoneTop, a band of white cloud, sky over it to ClubWallTop -- its inside only, round
//     the half of the room the camera looks at (the other half's inside faces away from it, so it isn't drawn: the room open to the
//     camera, as Rick's own); brass lamps along it;
//   - the curb: a low stone step all round the room's edge, over the floor tiles' stepped edge (the floor's cells reach ClubFloorRadius,
//     each a whole cell, so their edge zigzags inside the step's width), its way into the corridor left open.
internal static partial class CitadelProps
{
    public const int ClubWall = 107, ClubCurb = 108;
    // (cells of 512: the wall's inside, the curb's inside and outside, the floor's cells' middles within; the corridor's way in, degrees)
    public const float ClubWallRadius = 19.5f * 512, ClubCurbIn = 18.2f * 512, ClubCurbOut = 19.8f * 512, ClubFloorRadius = 19.0f * 512;
    public const float ClubDoorFrom = -8, ClubDoorTo = 11;
    public const int ClubStoneTop = 1100, ClubWallTop = 3800, ClubCurbHigh = 200;
    // (the palette: Rick's sky, his clouds' white, the stone's greys, brass and a lamp's glow)
    private const int ClubSky = 205, ClubCloud = 63, ClubStone = 50, ClubCurbStone = 52, ClubBrass = 106, ClubGlow = 245;
    // the arc the camera sees the wall's inside along (the room's middle to the wall: x = cos, z = sin of the angle)
    private const float ClubWallFrom = 128, ClubWallTo = 322;

    // (the actor stands far back in the room's north-west corner, ClubShift from its middle: the engine draws the scene's objects back
    // to front, each piece for the backdrop copied into it as it is drawn -- one drawn after Twinsen took him into the backdrop with it,
    // a second Twinsen left standing where he was as the scene began)
    public const int ClubShiftX = 11264, ClubShiftZ = 15360;
    private static Vector3 ClubAt(float degrees, float radius, float y) =>
        new(ClubShiftX + radius * MathF.Cos(degrees * MathF.PI / 180), y, ClubShiftZ + radius * MathF.Sin(degrees * MathF.PI / 180));
    private static readonly Vector3 ClubMiddle = new(ClubShiftX, 0, ClubShiftZ);

    public static Body BuildClubWall(byte[] header)
    {
        var m = new CarMesh("Stella Entities' wall", new[] { -1 });
        m.Light = 0.85f;
        const int Steps = 30, Arc = 5;
        const float CloudFoot = 1450;
        var step = (ClubWallTo - ClubWallFrom) / Steps;
        Vector3 Inside(float y) => ClubMiddle + new Vector3(0, y, 0);
        // (each column: the stone's foot and top, the clouds' foot and the wall's top)
        var foot = new int[Steps + 1]; var stone = new int[Steps + 1]; var cloud = new int[Steps + 1]; var top = new int[Steps + 1];
        for (var k = 0; k <= Steps; k++)
        {
            var a = ClubWallFrom + k * step;
            foot[k] = m.P(0, ClubAt(a, ClubWallRadius, 0));
            stone[k] = m.P(0, ClubAt(a, ClubWallRadius, ClubStoneTop));
            cloud[k] = m.P(0, ClubAt(a, ClubWallRadius, CloudFoot));
            top[k] = m.P(0, ClubAt(a, ClubWallRadius, ClubWallTop));
        }
        // the half of the wall's chord a puff spans (its width's half), each puff its own height
        var half = ClubWallRadius * MathF.Sin(step * MathF.PI / 360);
        for (var k = 0; k < Steps; k++)
        {
            var a0 = ClubWallFrom + k * step;
            m.In(new[] { foot[k], foot[k + 1], stone[k + 1], stone[k] }, ClubStone, Inside(ClubStoneTop / 2f));
            m.In(new[] { stone[k], stone[k + 1], m.P(0, ClubAt(a0 + step, ClubWallRadius, CloudFoot + 30)), m.P(0, ClubAt(a0, ClubWallRadius, CloudFoot + 30)) }, ClubCloud, Inside(1300), unlit: true);
            // a puff: a half-round of cloud from column to column, a fan from its foot's middle; the sky in strips over it
            var tall = half * (0.9f + 0.7f * (0.5f + 0.5f * MathF.Sin(k * 2.3f)));
            var arc = new int[Arc + 1]; var over = new int[Arc + 1];
            arc[0] = cloud[k]; arc[Arc] = cloud[k + 1]; over[0] = top[k]; over[Arc] = top[k + 1];
            for (var i = 1; i < Arc; i++)
            {
                var t = MathF.PI * i / Arc;
                var along = (1 - MathF.Cos(t)) / 2;
                var a = a0 + along * step;
                arc[i] = m.P(0, ClubAt(a, ClubWallRadius, CloudFoot + tall * MathF.Sin(t)));
                over[i] = m.P(0, ClubAt(a, ClubWallRadius, ClubWallTop));
            }
            var hub = m.P(0, ClubAt(a0 + step / 2, ClubWallRadius, CloudFoot));
            for (var i = 0; i < Arc; i++)
            {
                m.In(new[] { hub, arc[i], arc[i + 1] }, ClubCloud, Inside(1700), unlit: true);
                m.In(new[] { arc[i], arc[i + 1], over[i + 1], over[i] }, ClubSky, Inside(2700), unlit: true);
            }
        }
        // brass lamps on the wall over the stone, every so far along: a bracket and a glowing globe
        for (var a = ClubWallFrom + 14; a < ClubWallTo - 8; a += 30)
        {
            m.Box(0, ClubAt(a, ClubWallRadius - 50, 1250), new Vector3(110, 90, 110), ClubBrass);
            m.Ball(0, ClubAt(a, ClubWallRadius - 170, 1320), 105, ClubGlow);
        }
        return m.ToBody(header);
    }

    public static Body BuildClubCurb(byte[] header)
    {
        var m = new CarMesh("Stella Entities' curb", new[] { -1 });
        m.Light = 0.85f;
        const int Steps = 96;
        var inTop = new int[Steps + 1]; var inFoot = new int[Steps + 1]; var outTop = new int[Steps + 1]; var outFoot = new int[Steps + 1];
        for (var k = 0; k <= Steps; k++)
        {
            var a = k * 360f / Steps;
            inTop[k] = m.P(0, ClubAt(a, ClubCurbIn, ClubCurbHigh));
            inFoot[k] = m.P(0, ClubAt(a, ClubCurbIn, 0));
            outTop[k] = m.P(0, ClubAt(a, ClubCurbOut, ClubCurbHigh));
            outFoot[k] = m.P(0, ClubAt(a, ClubCurbOut, 0));
        }
        for (var k = 0; k < Steps; k++)
        {
            float a0 = k * 360f / Steps, a1 = (k + 1) * 360f / Steps;
            // (the corridor's way in: no step there)
            float Wrap(float a) => a > 180 ? a - 360 : a;
            if (Wrap(a1) > ClubDoorFrom && Wrap(a0) < ClubDoorTo) continue;
            m.Up(new[] { inTop[k], inTop[k + 1], outTop[k + 1], outTop[k] }, ClubCurbStone);
            m.In(new[] { inFoot[k], inFoot[k + 1], inTop[k + 1], inTop[k] }, ClubStone, ClubMiddle + new Vector3(0, ClubCurbHigh / 2f, 0));
            m.Out(new[] { outFoot[k], outFoot[k + 1], outTop[k + 1], outTop[k] }, ClubStone, ClubMiddle + new Vector3(0, ClubCurbHigh / 2f, 0));
        }
        return m.ToBody(header);
    }

    // The middle of the room (the design: "a raised round wooden stage with three brass poles round the column; a catwalk from backstage to
    // it", "an arc along the south-west wall: a curved counter with stools, a barman, a shelf of bottles behind", "Rick's round green tables"):
    //   - the stage (body 109, into the backdrop as the wall): a wooden round StageRadius across round the reactor's column, StageHigh up on
    //     a red velvet skirt, footlights round its edge; the catwalk north from it to the curb (the backstage door), CatwalkHalf each side
    //     of the room's middle line -- its cells (and the stage's) the grid's invisible solid cells, one layer;
    //   - the reactor's column (110): the moon's reactor coming down through the room onto the stage, glowing green bands between steel
    //     rings, up out of sight -- drawn in its place among the people (they go round it on the stage);
    //   - a brass pole (111), stage to ceiling;
    //   - the bar: its counter in three pieces (112, 116, 117: CounterArc degrees of the curve each, at CounterAt's angles, each body round
    //     its own middle on the curve at CounterMiddle -- turned there already, its actor at beta 0), the shelf of bottles against the
    //     wall behind it (113, into the backdrop), bar stools (115);
    //   - a table (114): Rick's round green card table on its brass foot, three chairs round it.
    public const int ClubStage = 109, ClubColumn = 110, ClubPole = 111, ClubCounter = 112, ClubShelf = 113, ClubTable = 114, ClubBarStool = 115, ClubCounter2 = 116, ClubCounter3 = 117;
    public static readonly (int Generic, float Angle)[] CounterAt = { (ClubCounter, 115), (ClubCounter2, 135), (ClubCounter3, 155) };
    // (StageHigh: about Twinsen's chest -- three layers, 768; it was one, 256: the user, 2026-11-13, "The stage area needs raising to about
    // the height of Twinsen's chest")
    // (the catwalk right up to the wall, under the backstage door: the user, 2026-11-13, "This stage should also go right to the edge of the room")
    public const float StageRadius = 7.5f * 512, StageHigh = 768, CatwalkHalf = 512, CatwalkTo = ClubWallRadius - 40;
    public const float CounterIn = 15.0f * 512, CounterOut = 16.0f * 512, CounterArc = 20, CounterMiddle = 15.5f * 512, ShelfIn = 17.3f * 512, ShelfOut = 17.9f * 512;
    public const float BarFrom = 105, BarTo = 165;
    public const int PoleHigh = 3900, ColumnHigh = 5200;
    private const int ClubWood = 22, ClubWoodLight = 25, ClubVelvet = 68, ClubReactor = 158, ClubSteel = 56, ClubFelt = 133, ClubLeather = 69;

    public static Body BuildClubStage(byte[] header)
    {
        var m = new CarMesh("Stella Entities' stage", new[] { -1 });
        m.Light = 0.85f;
        const int Steps = 48;
        var hub = m.P(0, ClubAt(0, 0, StageHigh));
        var rimIn = new int[Steps + 1]; var rim = new int[Steps + 1]; var foot = new int[Steps + 1];
        for (var k = 0; k <= Steps; k++)
        {
            var a = k * 360f / Steps;
            rimIn[k] = m.P(0, ClubAt(a, StageRadius - 360, StageHigh));
            rim[k] = m.P(0, ClubAt(a, StageRadius, StageHigh));
            foot[k] = m.P(0, ClubAt(a, StageRadius, 0));
        }
        for (var k = 0; k < Steps; k++)
        {
            m.Up(new[] { hub, rimIn[k], rimIn[k + 1] }, k % 2 == 0 ? ClubWoodLight : ClubWoodLight + 1);
            m.Up(new[] { rimIn[k], rimIn[k + 1], rim[k + 1], rim[k] }, ClubWood);
            m.Out(new[] { foot[k], foot[k + 1], rim[k + 1], rim[k] }, ClubVelvet, ClubMiddle + new Vector3(0, StageHigh / 2, 0));
        }
        // footlights round the edge
        for (var k = 0; k < 16; k++) m.Ball(0, ClubAt(k * 22.5f + 11.25f, StageRadius - 180, StageHigh + 30), 45, ClubGlow);
        // the catwalk: north (-z) from inside the stage to the curb
        var z0 = ClubShiftZ - CatwalkTo; var z1 = ClubShiftZ - StageRadius + 200;
        float x0 = ClubShiftX - CatwalkHalf, x1 = ClubShiftX + CatwalkHalf;
        int P(float x, float y, float z) => m.P(0, new Vector3(x, y, z));
        int a0 = P(x0, StageHigh, z0), a1 = P(x1, StageHigh, z0), a2 = P(x1, StageHigh, z1), a3 = P(x0, StageHigh, z1);
        int b0 = P(x0, 0, z0), b1 = P(x1, 0, z0), b2 = P(x1, 0, z1), b3 = P(x0, 0, z1);
        m.Up(new[] { a0, a1, a2, a3 }, ClubWoodLight);
        var mid = new Vector3(ClubShiftX, StageHigh / 2, (z0 + z1) / 2);
        m.Out(new[] { b1, b2, a2, a1 }, ClubVelvet, mid);
        m.Out(new[] { b3, b0, a0, a3 }, ClubVelvet, mid);
        // (its lights along both edges)
        for (var z = z0 + 400; z < z1 - 200; z += 900)
        {
            m.Ball(0, new Vector3(x0 + 70, StageHigh + 30, z), 40, ClubGlow);
            m.Ball(0, new Vector3(x1 - 70, StageHigh + 30, z), 40, ClubGlow);
        }
        return m.ToBody(header);
    }

    // The steps up onto the catwalk by the backstage door (body 97, into the backdrop as the stage; the user, 2026-11-13: "Let's add a few steps
    // right by the door"): on the catwalk's east side (+x) from its edge out StepsCells cells, between StepsZ0 and StepsZ1 (world cells, the
    // hall's grid) -- six treads down from the catwalk's top to the floor over the grid's slope cells (StellaEntities: a ramp each cell a
    // layer), each tread at the ramp's height in its middle
    public const int ClubSteps = 97, StepsCellX = 25, StepsCells = 3, StepsZ0 = 14, StepsZ1 = 15;
    public static Body BuildClubSteps(byte[] header)
    {
        var m = new CarMesh("Stella Entities' steps", new[] { -1 });
        m.Light = 0.85f;
        // (the body's frame: the hall's middle at ClubMiddle, as the stage's -- a world place less the actor's, which stands at the hall's
        // middle less ClubShift)
        float worldToBodyX = ClubShiftX - 24 * 512, worldToBodyZ = ClubShiftZ - 32 * 512;
        float x0 = StepsCellX * 512 + worldToBodyX, z0 = StepsZ0 * 512 + worldToBodyZ, z1 = (StepsZ1 + 1) * 512 + worldToBodyZ;
        const int Treads = 6;
        var depth = StepsCells * 512f / Treads;
        int P(float x, float y, float z) => m.P(0, new Vector3(x, y, z));
        for (var j = 0; j < Treads; j++)
        {
            float a = x0 + j * depth, b = a + depth, top = StageHigh - StageHigh * (j + 0.5f) / Treads;
            m.Up(new[] { P(a, top, z0), P(b, top, z0), P(b, top, z1), P(a, top, z1) }, j % 2 == 0 ? ClubWoodLight : ClubWoodLight + 1);
            // (its riser, facing out, down to the next tread; its sides down to the floor)
            var next = j + 1 < Treads ? StageHigh - StageHigh * (j + 1.5f) / Treads : 0;
            m.Out(new[] { P(b, next, z0), P(b, next, z1), P(b, top, z1), P(b, top, z0) }, ClubVelvet, new Vector3(a, top / 2, (z0 + z1) / 2));
            foreach (var (z, outward) in new[] { (z0, -1f), (z1, 1f) })
                m.Out(new[] { P(a, 0, z), P(b, 0, z), P(b, top, z), P(a, top, z) }, ClubVelvet, new Vector3((a + b) / 2, top / 2, z - outward * 100));
        }
        // (lights on every other tread's outer corner)
        for (var j = 0; j < Treads; j += 2)
            m.Ball(0, new Vector3(x0 + (j + 0.5f) * depth, StageHigh - StageHigh * (j + 0.5f) / Treads + 30, z1 - 60), 35, ClubGlow);
        return m.ToBody(header);
    }

    // a ring of n points round the body's own foot, radius r at height y
    private static int[] ClubRing(CarMesh m, float r, float y, int n) =>
        Enumerable.Range(0, n).Select(k => m.P(0, new Vector3(r * MathF.Cos(k * 2 * MathF.PI / n), y, r * MathF.Sin(k * 2 * MathF.PI / n)))).ToArray();
    private static void ClubSides(CarMesh m, int[] low, int[] high, int colour, bool unlit = false)
    {
        for (var k = 0; k < low.Length; k++)
        {
            var j = (k + 1) % low.Length;
            var c = (m.At(low[k]) + m.At(high[j])) / 2;
            m.Out(new[] { low[k], low[j], high[j], high[k] }, colour, new Vector3(0, c.Y, 0), unlit);
        }
    }

    public static Body BuildClubColumn(byte[] header)
    {
        var m = new CarMesh("the moon's reactor column", new[] { -1 });
        m.Light = 0.9f;
        const int Sides = 16;
        // a steel collar on the stage, then glowing bands between steel rings, up out of sight
        var collarLow = ClubRing(m, 1000, 0, Sides); var collarHigh = ClubRing(m, 1000, 260, Sides);
        ClubSides(m, collarLow, collarHigh, ClubSteel);
        var collarTop = ClubRing(m, 760, 260, Sides);
        for (var k = 0; k < Sides; k++) m.Up(new[] { collarHigh[k], collarHigh[(k + 1) % Sides], collarTop[(k + 1) % Sides], collarTop[k] }, ClubSteel);
        var y = 260f;
        for (var band = 0; y < ColumnHigh; band++)
        {
            var glow = band % 2 == 0;
            var h = glow ? 520f : 140f;
            var r = glow ? 700f : 780f;
            ClubSides(m, ClubRing(m, r, y, Sides), ClubRing(m, r, y + h, Sides), glow ? ClubReactor : ClubSteel, glow);
            y += h;
        }
        return m.ToBody(header);
    }

    public static Body BuildClubPole(byte[] header)
    {
        var m = new CarMesh("a dancer's pole", new[] { -1 });
        m.Light = 0.95f;
        ClubSides(m, ClubRing(m, 150, 0, 10), ClubRing(m, 150, 40, 10), ClubBrass);
        var capTop = ClubRing(m, 150, 40, 10); var capIn = ClubRing(m, 45, 40, 10);
        for (var k = 0; k < 10; k++) m.Up(new[] { capTop[k], capTop[(k + 1) % 10], capIn[(k + 1) % 10], capIn[k] }, ClubBrass);
        ClubSides(m, ClubRing(m, 45, 40, 8), ClubRing(m, 45, PoleHigh, 8), ClubBrass);
        return m.ToBody(header);
    }

    // a piece of the bar's counter: CounterArc degrees of the curve round the room's middle at `middle` degrees, round its own middle on
    // the curve at CounterMiddle
    private static float CounterMiddleAngle;
    private static Vector3 Piece(float a, float r, float y)
    {
        float am = CounterMiddleAngle * MathF.PI / 180, ar = (CounterMiddleAngle + a) * MathF.PI / 180;
        return new(r * MathF.Cos(ar) - CounterMiddle * MathF.Cos(am), y, r * MathF.Sin(ar) - CounterMiddle * MathF.Sin(am));
    }
    public static Body BuildClubCounter(byte[] header, float middle)
    {
        CounterMiddleAngle = middle;
        var m = new CarMesh("Stella Entities' bar", new[] { -1 });
        m.Light = 0.85f;
        const int Steps = 6;
        const float High = 560, Slab = 40, Over = 60;
        var inner = new Vector3(-CounterMiddle * MathF.Cos(middle * MathF.PI / 180), 0, -CounterMiddle * MathF.Sin(middle * MathF.PI / 180));
        for (var k = 0; k < Steps; k++)
        {
            float a0 = -CounterArc / 2 + k * CounterArc / Steps, a1 = a0 + CounterArc / Steps;
            int P(float a, float r, float y) => m.P(0, Piece(a, r, y));
            // the front (to the room), the back (the barman's side), the top slab overhanging both, its brass edge, a brass foot rail
            m.In(new[] { P(a0, CounterIn, 0), P(a1, CounterIn, 0), P(a1, CounterIn, High), P(a0, CounterIn, High) }, ClubWood, inner + new Vector3(0, High / 2, 0));
            m.Out(new[] { P(a0, CounterOut, 0), P(a1, CounterOut, 0), P(a1, CounterOut, High), P(a0, CounterOut, High) }, ClubWood, inner + new Vector3(0, High / 2, 0));
            m.Up(new[] { P(a0, CounterIn - Over, High + Slab), P(a1, CounterIn - Over, High + Slab), P(a1, CounterOut + Over, High + Slab), P(a0, CounterOut + Over, High + Slab) }, ClubWoodLight);
            m.In(new[] { P(a0, CounterIn - Over, High), P(a1, CounterIn - Over, High), P(a1, CounterIn - Over, High + Slab), P(a0, CounterIn - Over, High + Slab) }, ClubBrass, inner + new Vector3(0, High, 0));
            m.In(new[] { P(a0, CounterIn - 70, 110), P(a1, CounterIn - 70, 110), P(a1, CounterIn - 70, 150), P(a0, CounterIn - 70, 150) }, ClubBrass, inner + new Vector3(0, 130, 0));
        }
        foreach (var a in new[] { -CounterArc / 2, CounterArc / 2 })
            m.Plate(0, ClubWood, Piece(a, CounterIn, 0), Piece(a, CounterOut, 0), Piece(a, CounterOut, High), Piece(a, CounterIn, High));
        return m.ToBody(header);
    }

    public static Body BuildClubShelf(byte[] header)
    {
        var m = new CarMesh("Stella Entities' bottles", new[] { -1 });
        m.Light = 0.85f;
        const int Steps = 8;
        var step = (BarTo - BarFrom) / Steps;
        int[] bottleColours = { 130, 66, 164, 106, 36, 196 };
        for (var k = 0; k < Steps; k++)
        {
            float a0 = BarFrom + k * step, a1 = a0 + step;
            // the back board, three shelves, a bottle on each
            m.In(new[] { m.P(0, ClubAt(a0, ShelfOut, 0)), m.P(0, ClubAt(a1, ShelfOut, 0)), m.P(0, ClubAt(a1, ShelfOut, 1600)), m.P(0, ClubAt(a0, ShelfOut, 1600)) }, ClubWood, ClubMiddle + new Vector3(0, 800, 0));
            foreach (var (y, j) in new[] { (500f, 0), (900f, 1), (1300f, 2) })
            {
                m.Up(new[] { m.P(0, ClubAt(a0, ShelfIn, y)), m.P(0, ClubAt(a1, ShelfIn, y)), m.P(0, ClubAt(a1, ShelfOut, y)), m.P(0, ClubAt(a0, ShelfOut, y)) }, ClubWoodLight);
                var a = a0 + step * (0.3f + 0.4f * ((k + j) % 2));
                m.Box(0, ClubAt(a, (ShelfIn + ShelfOut) / 2, y + 110), new Vector3(70, 220, 70), bottleColours[(k * 3 + j) % bottleColours.Length]);
            }
        }
        return m.ToBody(header);
    }

    public static Body BuildClubTable(byte[] header)
    {
        var m = new CarMesh("Rick's card table", new[] { -1 });
        m.Light = 0.85f;
        const int Sides = 12;
        // the green top with its brass edge, the foot
        var edgeLow = ClubRing(m, 420, 440, Sides); var edgeHigh = ClubRing(m, 420, 480, Sides);
        ClubSides(m, edgeLow, edgeHigh, ClubBrass);
        var hub = m.P(0, new Vector3(0, 480, 0));
        for (var k = 0; k < Sides; k++) m.Up(new[] { hub, edgeHigh[k], edgeHigh[(k + 1) % Sides] }, ClubFelt);
        ClubSides(m, ClubRing(m, 50, 0, 6), ClubRing(m, 50, 440, 6), ClubBrass);
        ClubSides(m, ClubRing(m, 200, 0, 8), ClubRing(m, 200, 30, 8), ClubBrass);
        // three chairs round it, their backs out
        for (var c = 0; c < 3; c++)
        {
            var a = c * 2 * MathF.PI / 3 + 0.5f;
            var dir = new Vector3(MathF.Cos(a), 0, MathF.Sin(a));
            var seat = dir * 640;
            m.Box(0, seat + new Vector3(0, 280, 0), new Vector3(240, 40, 240), ClubLeather);
            foreach (var (u, w) in new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })
                m.Box(0, seat + new Vector3(u * 95, 130, w * 95), new Vector3(24, 260, 24), ClubWood);
            var back = seat + dir * 115 + new Vector3(0, 450, 0);
            var side = new Vector3(-dir.Z, 0, dir.X) * 120;
            m.Plate(0, ClubLeather, back - side - new Vector3(0, 150, 0), back + side - new Vector3(0, 150, 0), back + side + new Vector3(0, 150, 0), back - side + new Vector3(0, 150, 0));
        }
        return m.ToBody(header);
    }

    public static Body BuildClubBarStool(byte[] header)
    {
        var m = new CarMesh("a bar stool", new[] { -1 });
        m.Light = 0.9f;
        ClubSides(m, ClubRing(m, 170, 0, 8), ClubRing(m, 170, 25, 8), ClubBrass);
        ClubSides(m, ClubRing(m, 35, 25, 6), ClubRing(m, 35, 560, 6), ClubBrass);
        ClubSides(m, ClubRing(m, 130, 180, 8), ClubRing(m, 130, 200, 8), ClubBrass);
        var low = ClubRing(m, 170, 560, 10); var high = ClubRing(m, 170, 640, 10);
        ClubSides(m, low, high, ClubLeather);
        var hub = m.P(0, new Vector3(0, 650, 0));
        for (var k = 0; k < 10; k++) m.Up(new[] { hub, high[k], high[(k + 1) % 10] }, ClubLeather);
        return m.ToBody(header);
    }

    // The back wall's front (body 118, into the backdrop with the wall): the club's name over the catwalk -- "Stella Entities" (the user,
    // 2026-11-11), pink neon letters on a dark board along the wall -- the backstage door at the catwalk's end (a dark doorway between red
    // velvet curtains, a brass frame) and the office's door further round (Rick's office's teal, a brass handle).
    // (the name's board a body of its own, 119, drawn before it: in one body the renderer's polygon sort put pieces of the board over
    // letters -- STELLA's E lost its bars)
    public const int ClubFront = 118, ClubBoard = 119;
    public const float BackstageAt = 270, OfficeAt = 304;
    private const int ClubNeon = 79, ClubBoardColour = 49, ClubDoorway = 49, ClubTeal = 150;
    private const string ClubName = "STELLA ENTITIES";
    // a 5 x 7 letter's rows, top first ('#' lit)
    private static readonly Dictionary<char, string[]> ClubFont = new()
    {
        ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
        ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
        ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
        ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
        ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
        ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
    };

    public static Body BuildClubFront(byte[] header) => BuildClubFront(header, false);
    public static Body BuildClubBoard(byte[] header) => BuildClubFront(header, true);
    private static Body BuildClubFront(byte[] header, bool board)
    {
        var m = new CarMesh(board ? "Stella Entities' name's board" : "Stella Entities' name and doors", new[] { -1 });
        m.Light = 0.85f;
        Vector3 Inside(float y) => ClubMiddle + new Vector3(0, y, 0);
        // a patch of the wall's inside: from angle a0 to a1, height y0 to y1, `off` in from the wall
        void Patch(float a0, float a1, float y0, float y1, float off, int colour, bool unlit = false) =>
            m.In(new[] { m.P(0, ClubAt(a0, ClubWallRadius - off, y0)), m.P(0, ClubAt(a1, ClubWallRadius - off, y0)), m.P(0, ClubAt(a1, ClubWallRadius - off, y1)), m.P(0, ClubAt(a0, ClubWallRadius - off, y1)) },
                 colour, Inside((y0 + y1) / 2), unlit);
        // the name: Pixel units a dot, along the wall from its middle at BackstageAt, Line high
        const float Pixel = 66, Line = 2250;
        var degreesPerUnit = 180 / (MathF.PI * (ClubWallRadius - 40));
        var width = ClubName.Length * 6 * Pixel - Pixel;
        float A(float along) => BackstageAt + (along - width / 2) * degreesPerUnit;
        // (its board, in a few curved strips behind it)
        const int Strips = 6;
        if (board)
        {
            for (var k = 0; k < Strips; k++)
                Patch(A(-2 * Pixel + k * (width + 4 * Pixel) / Strips), A(-2 * Pixel + (k + 1) * (width + 4 * Pixel) / Strips), Line - 2 * Pixel, Line + 9 * Pixel, 20, ClubBoardColour, true);
            return m.ToBody(header);
        }
        for (var c = 0; c < ClubName.Length; c++)
        {
            if (!ClubFont.TryGetValue(ClubName[c], out var rows)) continue;
            // each row's runs of lit dots, a run over runs of the same span below it merged into one rectangle
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
                    var x0 = c * 6 * Pixel + col * Pixel; var x1 = c * 6 * Pixel + (end + 1) * Pixel;
                    Patch(A(x0), A(x1), Line + (6 - bottom) * Pixel, Line + (7 - r) * Pixel, 40, ClubNeon, true);
                }
        }
        // the backstage door: a dark doorway the catwalk's width, red curtains drawn back either side of it, a brass frame
        const float DoorTop = 1900;
        var half = (CatwalkHalf + 60) * 180 / (MathF.PI * ClubWallRadius);
        Patch(BackstageAt - half, BackstageAt + half, StageHigh, DoorTop, 20, ClubDoorway, true);
        var curtain = 260 * 180 / (MathF.PI * ClubWallRadius);
        for (var k = 0; k < 3; k++)
        {
            Patch(BackstageAt - half + k * curtain / 3, BackstageAt - half + (k + 1) * curtain / 3, StageHigh, DoorTop, 30 + 25 * (k % 2), ClubVelvet + (k % 2));
            Patch(BackstageAt + half - (k + 1) * curtain / 3, BackstageAt + half - k * curtain / 3, StageHigh, DoorTop, 30 + 25 * (k % 2), ClubVelvet + (k % 2));
        }
        var rim = 70 * 180 / (MathF.PI * ClubWallRadius);
        Patch(BackstageAt - half - rim, BackstageAt + half + rim, DoorTop, DoorTop + 90, 25, ClubBrass);
        Patch(BackstageAt - half - rim, BackstageAt - half, StageHigh, DoorTop, 25, ClubBrass);
        Patch(BackstageAt + half, BackstageAt + half + rim, StageHigh, DoorTop, 25, ClubBrass);
        // the office's door: teal panels in a brass frame, a brass handle
        var door = 420 * 180 / (MathF.PI * ClubWallRadius);
        const float OfficeTop = 1700;
        Patch(OfficeAt - door, OfficeAt + door, 0, OfficeTop, 20, ClubTeal);
        Patch(OfficeAt - door * 0.8f, OfficeAt - door * 0.05f, 250, OfficeTop - 150, 30, ClubTeal + 2);
        Patch(OfficeAt + door * 0.05f, OfficeAt + door * 0.8f, 250, OfficeTop - 150, 30, ClubTeal + 2);
        Patch(OfficeAt - door - rim, OfficeAt + door + rim, OfficeTop, OfficeTop + 90, 25, ClubBrass);
        Patch(OfficeAt - door - rim, OfficeAt - door, 0, OfficeTop, 25, ClubBrass);
        Patch(OfficeAt + door, OfficeAt + door + rim, 0, OfficeTop, 25, ClubBrass);
        m.Ball(0, ClubAt(OfficeAt + door * 0.6f, ClubWallRadius - 80, 850), 45, ClubBrass);
        return m.ToBody(header);
    }

    // The club's round pieces: no box Twinsen meets (the room's invisible cells hold him in) -- a speck on the floor at the actor, far back
    // in the room's corner outside it: the engine sorts what it draws by the boxes, back to front, so they are drawn first (a box high
    // over the room was "in front" of Twinsen, and he went into the backdrop with them)
    // (and the bar's counter pieces, a speck at their middles: a curved piece's own box was a square round it, the stools and the floor
    // in front of the bar inside it -- Twinsen pushed away from the bar; the grid's invisible cells along the curve are the counter's)
    public static short[]? ClubBox(int generic) => generic is ClubWall or ClubCurb or ClubStage or ClubShelf or ClubFront or ClubBoard or ClubCounter or ClubCounter2 or ClubCounter3 or ClubSteps
        ? new short[] { -1, 0, -1, 1, 1, 1 } : null;
}
