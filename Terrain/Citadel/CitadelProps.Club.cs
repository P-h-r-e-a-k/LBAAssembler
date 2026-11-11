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

    // The club's round pieces: no box Twinsen meets (the room's invisible cells hold him in) -- a speck on the floor at the actor, far back
    // in the room's corner outside it: the engine sorts what it draws by the boxes, back to front, so they are drawn first (a box high
    // over the room was "in front" of Twinsen, and he went into the backdrop with them)
    public static short[]? ClubBox(int generic) => generic is ClubWall or ClubCurb ? new short[] { -1, 0, -1, 1, 1, 1 } : null;
}
