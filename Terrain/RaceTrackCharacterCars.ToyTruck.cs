using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using System.Text.Json;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The toy dump truck (the user, 2026-10-17, with a photo of a toddler's toy truck and one of a man: "Create a special car as pictured with
// the person in the second picture, either driving with their head out of the window smoking, or riding in the back of the truck"): the
// chunky toy at race-car size --
//
//   the body     glossy yellow, one rounded tub from the cab's bluff front to the dump bed's back: the cab tall and round-roofed, a notch
//                behind it, the bed's flat top with a round hole in it, as the toy's (its maker's name on the side left off)
//   the windows  big and teal: the windscreen over the front, a window each side of the cab -- the driver's open, dark inside, its glass
//                wound down (a teal line along the sill)
//   the lamps    two round red headlights low on the front
//   the wheels   four oversized glossy purple balls on white hubs, standing out beside the body
//   the driver   made here, not taken from a body of the game's: a fond caricature in the cars' low-poly style -- thick wavy
//                salt-and-pepper hair, rectangular glasses, a dark moustache over a cheeky grin, a white T-shirt with a dark print --
//                leaning his head out of the driver's window, his elbow out over the door, a cigarette in the corner of his mouth, its tip
//                glowing, its smoke streaming back in the wind
//
// A body of the racer's entity (157: its driving animation bounces it, as it does the other cars) made with the cars' kit (CarMesh), but
// neither one of the characters' cars (All) nor the race game's: like the pumpkin F1 it goes into a game folder at the very end of every
// build (InstallToyTruck, after InstallPumpkin: no body before it moves), with its half-size copy, as race car ToyTruckNumber of
// RACECARS.JSON. Twinsen drives it on any track by picking it in the Race car window's list (RaceCarSetup.DriveAs); it is drawn in his
// buggy's place (RACEMOD.CPP drive_as=), so it handles as his buggy does. No track has it as its own car.
internal static partial class RaceTrackCharacterCars
{
    // race car 221 (RACECARS.JSON: after the pumpkin F1's 220), the racer entity's body 65 (its half-size copy 165)
    public const int ToyTruckNumber = 221, ToyTruckGeneric = 65;
    public const string ToyTruckName = "Toy dump truck", ToyTruckDriver = "the toy truck's driver";

    // The truck's colours: the body in the golds' top (96 on: the yellowest ramp every island's palette has) and the windows in the teals,
    // both taking a part of the light so they stay bright; the balls in the purples (208 on), more of the light (dark underneath, lavender
    // on top), a lavender glint; red lamps, white hubs.
    private const int TtYellow = 105, TtWindow = 153, TtGlass = 156, TtInside = 178, TtPurple = 211, TtShine = 221, TtLamp = 73, TtHub = WhiteFlat;
    private const float TtBodyShare = 0.42f, TtBallShare = 0.65f;
    // The driver's: skin (the ramp Twinsen's is in), hair and moustache greys, the grin's dark red and its teeth, the ears', nose's and hand's
    // skin drawn flat, the T-shirt's white (a part of the light) and its print, the glasses' black frames, the cigarette, its ember and smoke.
    private const int TtSkin = 34, TtHair = 49, TtMoustache = 48, TtMouth = 67, TtTeeth = 62, TtSkinFlat = 38, TtShirt = 60, TtPrint = 49, TtPrintLine = 58,
        TtFrame = 48, TtBrow = 49, TtCigarette = 62, TtEmber = 242;
    private const float TtHeadShare = 0.7f;
    // the side he sits on: -x, the side the game draws as the car's right -- a right-hand drive (seen from the chase camera behind the car,
    // +x is on the left; BodyPipeline's pictures show it the other way round)
    private const float TtDriverSide = -1;

    // A ring across the body at z, from the five points of its +x half ((x, y), from the top's edge round to the bottom's) and the -x half
    // mirrored: ten points, from the top's edge down the +x side, across the bottom and up the -x side -- so its panels (each between point
    // k and k + 1) are 0-3 the +x side's shoulder, window, door and sill, 4 the bottom, 5-8 the -x side's sill, door, window and shoulder,
    // and 9 the top.
    private static int[] TtRing(CarMesh m, float z, (float X, float Y)[] half)
        => half.Select(p => new Vector3(p.X, p.Y, z)).Concat(half.Reverse().Select(p => new Vector3(-p.X, p.Y, z))).Select(v => m.P(2, v)).ToArray();

    // A ball on a wheel's bone: its poles on the axle (x), `rings` rings of `meridians` points between them, faced out.
    private static void TtBall(CarMesh m, int bone, Vector3 centre, float radius, int meridians, int rings, int colour)
    {
        var lat = Enumerable.Range(1, rings).Select(j =>
        {
            var th = j * MathF.PI / (rings + 1);
            return Enumerable.Range(0, meridians).Select(k =>
            {
                var ph = (k + 0.5f) * MathF.Tau / meridians;
                return m.P(bone, centre + new Vector3(radius * MathF.Cos(th), radius * MathF.Sin(th) * MathF.Cos(ph), radius * MathF.Sin(th) * MathF.Sin(ph)));
            }).ToArray();
        }).ToArray();
        m.Cap(lat[0], m.P(bone, centre + new Vector3(radius, 0, 0)), colour, centre);
        for (var j = 0; j + 1 < lat.Length; j++) m.Skin(lat[j], lat[j + 1], _ => colour, centre);
        m.Cap(lat[^1], m.P(bone, centre - new Vector3(radius, 0, 0)), colour, centre);
    }

    // A wheel: the strut (from inside the body, where it turns; the white hub on the ball's inner side), the hub (a front wheel's, which
    // steers about the ball's middle) and the ball itself; a lavender glint high on its outer side (on a bone that doesn't roll it round).
    private static void TtWheel(CarMesh m, (float Side, int Strut, int Hub, int Wheel) bones, Vector3 centre, float radius)
    {
        var (side, strutBone, hubBone, wheelBone) = bones;
        centre = centre with { X = side * centre.X };
        m.Light = 1;
        m.Pivot(strutBone, m.P(2, new Vector3(side * 230, centre.Y + 40, centre.Z)));
        var hub = m.P(strutBone, centre - new Vector3(side * radius * 0.94f, 0, 0));
        m.Sphere(hub, (int)(radius * 0.25f), TtHub);
        var middle = m.P(strutBone, centre);
        var still = strutBone;
        if (hubBone >= 0)
        {
            var hubMiddle = m.P(hubBone, centre);
            m.Pivot(hubBone, middle);
            m.Pivot(wheelBone, hubMiddle);
            still = hubBone;
        }
        else m.Pivot(wheelBone, middle);
        m.Light = TtBallShare;
        TtBall(m, wheelBone, centre, radius, 10, 4, TtPurple);
        m.Light = 1;
        m.Ball(still, centre + Vector3.Normalize(new Vector3(side * 0.7f, 0.65f, 0.1f)) * radius * 1.02f, (int)(radius * 0.12f), TtShine);
    }

    // The toy dump truck.
    public static Body BuildToyTruck(Body racer)
    {
        var m = new CarMesh(ToyTruckName);
        Roots(m);

        // ---- the body: rings from the front back -- the front's rim, the windscreen's top, the cab's side windows' back, the cab's back
        // edge, the notch behind it, the bed's front, middle and back, the tail's rounding
        var profile = new (float Z, (float X, float Y)[] Half)[]
        {
            (700, new[] { (115f, 470f), (210f, 455f), (250f, 395f), (258f, 300f), (205f, 178f) }),
            (545, new[] { (150f, 768f), (268f, 738f), (304f, 530f), (311f, 330f), (250f, 168f) }),
            (280, new[] { (150f, 775f), (270f, 742f), (306f, 530f), (311f, 330f), (250f, 168f) }),
            (175, new[] { (145f, 738f), (262f, 712f), (305f, 560f), (311f, 330f), (250f, 168f) }),
            (135, new[] { (150f, 690f), (270f, 676f), (304f, 560f), (311f, 330f), (250f, 168f) }),
            (100, new[] { (170f, 728f), (300f, 708f), (320f, 560f), (322f, 330f), (255f, 168f) }),
            (-210, new[] { (170f, 728f), (300f, 708f), (320f, 560f), (322f, 330f), (255f, 168f) }),
            (-470, new[] { (168f, 724f), (296f, 704f), (318f, 560f), (320f, 330f), (252f, 170f) }),
            (-535, new[] { (130f, 680f), (240f, 666f), (270f, 560f), (276f, 330f), (220f, 190f) }),
        };
        const int Windscreen = 0, Cab = 1, BedFront = 5, BedBack = 6;
        m.Light = TtBodyShare;
        var rings = profile.Select(r => TtRing(m, r.Z, r.Half)).ToArray();
        // (the side windows' panels: 1 on +x, 7 on -x -- the driver's open, the other teal)
        int driverWindow = TtDriverSide > 0 ? 1 : 7, otherWindow = 8 - driverWindow;
        for (var i = 0; i + 1 < rings.Length; i++)
        {
            float Middle(int r) => (profile[r].Half[0].Y + profile[r].Half[4].Y) / 2;
            var inside = new Vector3(0, (Middle(i) + Middle(i + 1)) / 2, (profile[i].Z + profile[i + 1].Z) / 2);
            for (var k = 0; k < 10; k++)
            {
                // (the bed's top: its lid with the hole, below)
                if (k == 9 && i is BedFront or BedBack) continue;
                var quad = new[] { rings[i][k], rings[i][(k + 1) % 10], rings[i + 1][(k + 1) % 10], rings[i + 1][k] };
                if (i == Cab && k == driverWindow) m.Out(quad, TtInside, inside, unlit: true);
                else m.Out(quad, (i == Windscreen && k is 8 or 9 or 0) || (i == Cab && k == otherWindow) ? TtWindow : TtYellow, inside);
            }
        }
        // the front and the back closed: the front's face (its lamps on it, below) and the tail
        m.Cap(rings[0], m.P(2, new(0, 320, 716)), TtYellow, new Vector3(0, 320, 640));
        m.Cap(rings[^1], m.P(2, new(0, 440, -553)), TtYellow, new Vector3(0, 440, -485));
        // the bed's lid: from the bed's top edges in to the hole's rim, round the hole (dark) -- one surface, so nothing on it is drawn over
        // anything else of it
        {
            var outer = new[] { rings[BedFront][0], rings[BedBack][0], rings[BedBack + 1][0], rings[BedBack + 1][9], rings[BedBack][9], rings[BedFront][9] };
            var centre = new Vector3(0, 727, -185);
            var rim = outer.Select(o =>
            {
                var d = m.At(o) - centre; d.Y = 0;
                var a = MathF.Atan2(d.X, d.Z);
                return m.P(2, centre + new Vector3(118 * MathF.Sin(a), 0, 132 * MathF.Cos(a)));
            }).ToArray();
            for (var k = 0; k < 6; k++) m.Up(new[] { outer[k], outer[(k + 1) % 6], rim[(k + 1) % 6], rim[k] }, TtYellow);
            m.Light = 1;
            m.Up(new[] { rim[0], rim[1], rim[2], rim[3] }, HoleDark, unlit: true);
            m.Up(new[] { rim[3], rim[4], rim[5], rim[0] }, HoleDark, unlit: true);
        }
        m.Light = 1;
        // the driver's window's glass, wound down: its top edge along the sill
        m.Line(rings[Cab][driverWindow == 1 ? 2 : 7], rings[Cab + 1][driverWindow == 1 ? 2 : 7], TtGlass);
        // the headlights: round and red, low on the front
        foreach (var side in Sides) m.Ball(2, new Vector3(side * 160, 262, 713), 50, TtLamp);
        // (and small ones at the back, for the cars behind)
        foreach (var side in Sides) m.Ball(2, new Vector3(side * 190, 300, -552), 30, TtLamp);

        // ---- the wheels: four big balls
        var front = new Vector3(480, 200, 400); var rear = new Vector3(480, 200, -270);
        const float Radius = 200;
        TtWheel(m, (1, 3, 4, 5), front, Radius);
        TtWheel(m, (-1, 6, 7, 8), front, Radius);
        TtWheel(m, (-1, 9, -1, 10), rear, Radius);
        TtWheel(m, (1, 11, -1, 12), rear, Radius);

        // ---- the driver (bone 13, turning at the window's sill)
        m.Pivot(13, m.P(2, new Vector3(TtDriverSide * 304, 530, 410)));
        var (shoulder, neck) = TtDriver(m);
        Arms(m, shoulder, neck);

        var body = m.ToBody(racer.Header);
        // (its box its own: the balls and the driver's head stand out of the racer's car's)
        var world = body.World();
        var box = new[] { world.Min(v => v.X), world.Max(v => v.X), world.Min(v => v.Y), world.Max(v => v.Y), world.Min(v => v.Z), world.Max(v => v.Z) };
        for (var i = 0; i < 6; i++) BinaryPrimitives.WriteInt32LittleEndian(body.Header.AsSpan(8 + i * 4), (int)MathF.Round(box[i]));
        return body;
    }

    // The driver (bone 13), in the car's coordinates (his side's x as TtDriverSide has it): his shoulders and chest in the window, his head
    // out of it, his arm out over the door, the cigarette and its smoke. Returns two of his points for the racer's arms' bones (Arms).
    private static (int Shoulder, int Neck) TtDriver(CarMesh m)
    {
        Vector3 S(Vector3 v) => v with { X = TtDriverSide * v.X };
        // a ring of `n` points round the way `d` goes, `r` across, its first point towards +z (so rings of a bent arm line up)
        int[] Round(Vector3 centre, Vector3 d, float r, int n)
        {
            d = Vector3.Normalize(d);
            var u = Vector3.Normalize(Vector3.UnitZ - d * Vector3.Dot(Vector3.UnitZ, d));
            return m.Loop(13, centre, u * r, Vector3.Cross(d, u) * r, n);
        }
        // a tube from a to b, `ra` across at a and `rb` at b; returns both rings
        (int[] A, int[] B) Tube(Vector3 a, Vector3 b, float ra, float rb, int n, Func<int, int> colour)
        {
            var ringA = Round(a, b - a, ra, n); var ringB = Round(b, b - a, rb, n);
            m.Skin(ringA, ringB, colour, (a + b) / 2);
            return (ringA, ringB);
        }

        // ---- his shoulders and the top of his chest: a white yoke across the window, its outer end his shoulder out of it; on its front
        // his T-shirt's dark print, a pale scrawl across it
        m.Light = SoftShare;
        var inner = S(new Vector3(215, 602, 392)); var outer = S(new Vector3(352, 612, 382));
        var yokeUp = Vector3.UnitY * 46; var yokeFore = Vector3.UnitZ * 66;
        int[] Hexagon(Vector3 c) => Enumerable.Range(0, 6).Select(k => m.P(13, c + yokeUp * MathF.Cos(k * MathF.Tau / 6) + yokeFore * MathF.Sin(k * MathF.Tau / 6))).ToArray();
        var yokeIn = Hexagon(inner); var yokeOut = Hexagon(outer);
        // (panel k between point k and k + 1: 0 the top's front, 1 the chest -- the print --, 2 the front's bottom, 3-5 underneath and the back)
        m.Skin(yokeIn, yokeOut, k => k == 1 ? TtPrint : TtShirt, (inner + outer) / 2);
        m.Out(new[] { yokeOut[0], yokeOut[1], yokeOut[2], yokeOut[3] }, TtShirt, inner);
        m.Out(new[] { yokeOut[3], yokeOut[4], yokeOut[5], yokeOut[0] }, TtShirt, inner);
        // (the scrawl on the part of the chest out of the window)
        m.Line(m.P(13, Vector3.Lerp(m.At(yokeIn[1]), m.At(yokeOut[1]), 0.66f)), m.P(13, Vector3.Lerp(m.At(yokeIn[2]), m.At(yokeOut[2]), 0.92f)), TtPrintLine);
        m.Line(m.P(13, Vector3.Lerp(m.At(yokeIn[1]), m.At(yokeOut[1]), 0.9f)), m.P(13, Vector3.Lerp(m.At(yokeIn[2]), m.At(yokeOut[2]), 0.7f)), TtPrintLine);

        // ---- his arm out over the door: the sleeve from the shoulder, the bare arm out to the elbow, the forearm hanging down the door, his
        // hand
        var shoulderAt = outer + S(new Vector3(-8, -8, -10));
        var elbow = S(new Vector3(445, 590, 340)); var wrist = S(new Vector3(436, 462, 312)); var hand = S(new Vector3(431, 428, 304));
        var sleeveEnd = Vector3.Lerp(shoulderAt, elbow, 0.5f);
        m.Light = SoftShare;
        Tube(shoulderAt, sleeveEnd, 42, 38, 5, _ => TtShirt);
        m.Light = TtHeadShare;
        var upper = Tube(sleeveEnd, elbow, 27, 25, 5, _ => TtSkin);
        m.Skin(upper.B, Round(wrist, wrist - elbow, 20, 5), _ => TtSkin, (elbow + wrist) / 2);
        m.Light = 1;
        m.Ball(13, hand, 28, TtSkinFlat);

        // ---- his head, out of the window, turned a little out and tilted
        // (a little bigger than life: he is the car's character)
        const float HeadScale = 1.2f;
        var chin = S(new Vector3(400, 680, 452));
        var turn = Quaternion.CreateFromAxisAngle(Vector3.UnitY, TtDriverSide * 15 * MathF.PI / 180) * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -TtDriverSide * 8 * MathF.PI / 180);
        Vector3 Head(Vector3 local) => chin + Vector3.Transform(local with { X = TtDriverSide * local.X } * HeadScale, turn);
        var (neckTop, mouthCorner) = TtHead(m, Head, HeadScale);

        // ---- his neck, from the yoke up into his head
        var neckFoot = S(new Vector3(318, 648, 400));
        m.Light = TtHeadShare;
        var neckRings = Tube(neckFoot, neckTop, 40, 34, 5, _ => TtSkin);

        // ---- the cigarette in the corner of his mouth, its ember, and the smoke streaming back
        m.Light = 1;
        var tip = Head(new Vector3(62, 40, 110));
        m.Line(m.P(13, mouthCorner), m.P(13, tip), TtCigarette);
        m.Ball(13, tip, 10, TtEmber);
        foreach (var (offset, radius, colour) in new[] { (new Vector3(10, 20, -26), 11, 58), (new Vector3(24, 40, -128), 16, 59), (new Vector3(36, 58, -250), 22, 60), (new Vector3(46, 72, -390), 29, 61) })
            m.Ball(13, tip + S(offset), radius, colour);
        return (yokeOut[0], neckRings.A[0]);
    }

    // His head, from rings up it -- chin, jaw, the grin's top, the moustache's top, the eyes, the brow, the hairline -- and a crown, in its
    // own coordinates (the chin's middle at 0, up y, the face towards +z, his right +x) placed by `place`: the face's features in its panels
    // (the grin, the moustache), the hair over the back, the sides and the crown, wavy tufts round it; his nose, ears and eyes, the glasses'
    // frames and his eyebrows. Returns where his neck goes into it and his mouth's corner (the cigarette's), placed.
    private static (Vector3 Neck, Vector3 Mouth) TtHead(CarMesh m, Func<Vector3, Vector3> place, float scale)
    {
        int P(Vector3 local) => m.P(13, place(local));
        // (the rings' points round from the face's right: panel k between point k and k + 1 -- 0 the right cheek, 1 the right side, 2 the right
        // of the back, 3 the back, 4 its left, 5 the left side, 6 the left cheek, 7 the face's middle)
        var angles = new[] { 28f, 75, 120, 160, 200, 240, 285, 332 };
        var rings = new (float Y, float Rx, float Front, float Back, float Zc)[]
        {
            (0, 30, 28, 18, 26), (36, 58, 64, 50, 12), (58, 66, 74, 64, 8), (80, 71, 80, 74, 4), (118, 75, 80, 84, 0), (152, 75, 76, 88, -4), (185, 64, 58, 78, -8),
        };
        Vector3 On(int r, float degrees)
        {
            var (y, rx, front, back, zc) = rings[r];
            var a = degrees * MathF.PI / 180;
            return new Vector3(rx * MathF.Sin(a), y, zc + (MathF.Cos(a) >= 0 ? front : back) * MathF.Cos(a));
        }
        m.Light = TtHeadShare;
        var points = Enumerable.Range(0, rings.Length).Select(r => angles.Select(a => P(On(r, a))).ToArray()).ToArray();
        const int Face = 7;
        int Colour(int band, int k) => band switch
        {
            1 => k == Face ? -1 : TtSkin,
            2 => k == Face ? TtMoustache : k is 2 or 3 or 4 ? TtHair : TtSkin,
            3 => k is 2 or 3 or 4 ? TtHair : TtSkin,
            4 => k is 1 or 2 or 3 or 4 or 5 ? TtHair : TtSkin,
            5 => k == Face ? TtSkin : TtHair,
            _ => TtSkin,
        };
        var middle = new Vector3(0, 100, 0);
        for (var b = 0; b + 1 < rings.Length; b++)
            for (var k = 0; k < 8; k++)
            {
                var quad = new[] { points[b][k], points[b][(k + 1) % 8], points[b + 1][(k + 1) % 8], points[b + 1][k] };
                var colour = Colour(b, k);
                if (colour >= 0) { m.Out(quad, colour, place(middle)); continue; }
                // (the grin: his teeth along its top, open and dark red under them -- unlit)
                int l = P(Vector3.Lerp(On(1, angles[Face]), On(2, angles[Face]), 0.45f)), r = P(Vector3.Lerp(On(1, angles[0]), On(2, angles[0]), 0.45f));
                m.Out(new[] { l, r, points[b + 1][0], points[b + 1][Face] }, TtTeeth, place(middle), unlit: true);
                m.Out(new[] { points[b][Face], points[b][0], r, l }, TtMouth, place(middle), unlit: true);
            }
        m.Cap(points[0], P(new Vector3(0, -6, 30)), TtSkin, place(middle));
        m.Cap(points[^1], P(new Vector3(0, 212, -12)), TtHair, place(middle));
        m.Light = 1;

        // his nose, ears and eyes; the glasses' frames -- rectangles, a bridge, the arms back to his ears -- and his eyebrows over them
        m.Ball(13, place(new Vector3(0, 90, 94)), (int)(20 * scale), TtSkinFlat);
        foreach (var side in Sides)
        {
            m.Ball(13, place(new Vector3(side * 78, 100, 4)), (int)(17 * scale), TtSkinFlat);
            m.Ball(13, place(new Vector3(side * 27, 120, 80)), (int)(8 * scale), TtFrame);
        }
        int[] Lens(float side) => new[] { P(new(side * 8, 134, 85)), P(new(side * 50, 134, 66)), P(new(side * 50, 106, 66)), P(new(side * 8, 106, 85)) };
        var right = Lens(1); var left = Lens(-1);
        foreach (var lens in new[] { right, left }) for (var k = 0; k < 4; k++) m.Line(lens[k], lens[(k + 1) % 4], TtFrame);
        m.Line(right[0], left[0], TtFrame);
        m.Line(right[1], points[4][1], TtFrame); m.Line(left[1], points[4][6], TtFrame);
        foreach (var side in Sides) m.Line(P(new(side * 9, 143, 85)), P(new(side * 52, 149, 64)), TtBrow);

        // his hair's waves: tufts over the crown, the forelock and the sides, salt and pepper
        foreach (var (at, radius, colour) in new[]
        {
            (new Vector3(4, 206, 34), 32, 52), (new Vector3(-40, 200, 8), 30, 55), (new Vector3(42, 198, 4), 30, 57), (new Vector3(0, 212, -36), 34, 53),
            (new Vector3(-62, 172, -30), 27, 56), (new Vector3(64, 170, -32), 27, 52), (new Vector3(0, 186, -72), 30, 54), (new Vector3(30, 172, 58), 22, 57),
        })
            m.Ball(13, place(at), (int)(radius * scale), colour);
        return (place(new Vector3(0, 30, -16)), place(Vector3.Lerp(On(1, 28), On(2, 28), 0.5f) + new Vector3(0, 0, -4)));
    }

    // Adds the toy dump truck to a game folder whose race tracks are built (at the very end of the build: RaceTrackService.Build): its body and
    // its half-size copy at the end of BODY.HQR, the racer's entity's bodies ToyTruckGeneric and its half-size one, and its line in
    // RACECARS.JSON. Returns lines for the build's log.
    public static List<string> InstallToyTruck(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var racer = Body.Read(HqrArchive.Open(bodyPath).Read(RaceTrackBaldinoCar.RacerBody), 2);
        Body car;
        try { car = BuildToyTruck(racer); }
        catch (Exception e) when (e is InvalidDataException or InvalidOperationException or ArgumentException) { return new() { $"{ToyTruckName}: not made -- {e.Message}" }; }
        var bytes = car.Write();
        var file = File.ReadAllBytes(bodyPath);
        var own = HqrArchive.CountEntries(bodyPath);
        file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(bytes));
        file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(RaceTrackSmallCars.Halved(bytes)));
        var small = own + 1;
        File.WriteAllBytes(bodyPath, file);
        var table = HqrArchive.Open(ressPath).Read(44);
        table = RaceTrackBaldinoCar.WithBody(table, RaceTrackBaldinoCar.RacerEntity, ToyTruckGeneric, own);
        table = RaceTrackBaldinoCar.WithBody(table, RaceTrackBaldinoCar.RacerEntity, RaceTrackSmallCars.SmallOf(ToyTruckGeneric), small);
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        // (its line after the others': the folder's cars, as InstallCast and InstallPumpkin wrote them)
        var catalogue = Catalogue(gameDirectory).Where(e => e.Number != ToyTruckNumber).ToList();
        catalogue.Add(new CatalogueEntry(ToyTruckNumber, ToyTruckName, ToyTruckDriver, "", RaceTrackBaldinoCar.RacerEntity, ToyTruckGeneric, own, small));
        File.WriteAllText(Path.Combine(gameDirectory, CatalogueFile), JsonSerializer.Serialize(catalogue, new JsonSerializerOptions { WriteIndented = true }));
        return new()
        {
            $"{ToyTruckName} (race car {ToyTruckNumber}, anyone's to drive): BODY.HQR entry {own} ({car.Faces.Count} polygons, {car.Lines.Count} lines, {car.Spheres.Count} spheres, " +
            $"{car.Vertices.Count} points), its half-size copy {small}; the racer's entity ({RaceTrackBaldinoCar.RacerEntity}) bodies {ToyTruckGeneric} and {RaceTrackSmallCars.SmallOf(ToyTruckGeneric)}",
        };
    }
}
