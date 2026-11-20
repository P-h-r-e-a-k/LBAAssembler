using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The museum security room's mop and bucket (the user, 2026-11-17: "Let's take out some panes of glass in the door in the museum, let's add a
// broom ... The user needs to throw their magic ball through the missing glass in the door, to hit the broom, the broom then falls in a short
// cut scene and activates the red button which causes the door to open"; 2026-11-19: "Let's replace the broom with a mop and bucket, and
// position the mop and bucket in the middle of the room between the door and the button to make it more obvious. Where it currently is the ball
// doesn't actually reach it unless Twinsen has the suit"): a mop standing in a bucket out on the floor, on the line from the room's door to the
// desk's red button. Hit, the two tip over together about the bucket's foot on the button's side until the handle's end lies on the button.
// An actor turns only about the upright, so the fall is five bodies, the pair at each tilt, swapped one after another (RaceTrackStory.Chapters,
// MuseumMop): each with its origin at the bucket's foot (its middle, on the floor). MopButton is the button's place less that foot's (scene 15):
// as far from the button as a mop's length lets the handle still reach it.
internal static partial class CitadelProps
{
    public const int MopUp = 166;
    public static readonly int[] MopFrames = { 166, 167, 168, 169, 170 };
    // (how far through its fall each body is: slow off the upright, quick at the end)
    private static readonly float[] MopTilt = { 0f, 0.25f, 0.5f, 0.78f, 1f };
    public static readonly Vector3 MopButton = new(-1460, 784, -523);
    private const float BucketLow = 190, BucketTop = 235, BucketHigh = 330, WaterAt = 230, MopFoot = 60, MopLean = 8;
    // (greys in the middle of their ramp, lit only a little: the light runs a colour up and down its ramp, and past either end into the
    // reds either side)
    private const int BucketSide = 3 * 16 + 7, BucketInside = 3 * 16 + 5, BucketRim = 3 * 16 + 10, Water = 3 * 16 + 4, MopStrand = 3 * 16 + 11, MopHandle = 1 * 16 + 7;
    private const float BucketLight = 0.45f, StrandLight = 0.3f, HandleLight = 0.8f;

    // the standing pair's box (x, y, z min, then max): round it and the floor before it, towards the room's door -- the side away from the
    // button (see BuildMop)
    private static short[]? MopBox(int generic) => generic == MopUp ? new short[] { -450, 0, -450, 1300, 1600, 750 } : null;

    public static Body BuildMop(byte[] header, int frame)
    {
        var m = new CarMesh("a mop in a bucket", new[] { -1 });
        m.Light = BucketLight;
        // the plane of the fall: `u` along the floor towards the button, the tip about the bucket's foot on that side (`edge`)
        var u = Vector3.Normalize(new Vector3(MopButton.X, 0, MopButton.Z));
        var edge = u * BucketLow;
        var axis = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, u));
        // the handle: from the mop's head on the bucket's floor, leaning a little towards the button, as long as takes its end -- turned
        // about the edge -- onto the button
        var lean = MopLean * MathF.PI / 180;
        var d0 = Vector3.Normalize(u * MathF.Sin(lean) + Vector3.UnitY * MathF.Cos(lean));
        var foot0 = new Vector3(0, MopFoot, 0);
        float b = Vector3.Dot(d0, foot0 - edge), c = (foot0 - edge).LengthSquared() - (MopButton - edge).LengthSquared();
        var length = -b + MathF.Sqrt(b * b - c);
        var top0 = foot0 + d0 * length;
        float Angle(Vector3 v) => MathF.Atan2(v.Y, Vector3.Dot(v, u));
        var fall = Angle(top0 - edge) - Angle(MopButton - edge);
        var turn = Quaternion.CreateFromAxisAngle(axis, fall * MopTilt[frame]);
        Vector3 At(Vector3 v) => edge + Vector3.Transform(v - edge, turn);
        Vector3 Dir(Vector3 v) => Vector3.Transform(v, turn);
        // (the rings across the bucket's own upright)
        var across1 = Dir(u);
        var across2 = Dir(Vector3.Normalize(Vector3.Cross(u, Vector3.UnitY)));
        int[] Ring(float y, float r) => m.Loop(0, At(new Vector3(0, y, 0)), across1 * r, across2 * r, 10);
        var mid = At(new Vector3(0, BucketHigh / 2, 0));
        // the bucket: its tapered side, out and in (seen into from above), a rim, its floor; the water in it
        var low = Ring(0, BucketLow);
        var high = Ring(BucketHigh, BucketTop);
        m.Skin(low, high, _ => BucketSide, mid);
        var lowIn = Ring(4, BucketLow - 4);
        var highIn = Ring(BucketHigh, BucketTop - 6);
        for (var k = 0; k < 10; k++)
            m.In(new[] { lowIn[k], lowIn[(k + 1) % 10], highIn[(k + 1) % 10], highIn[k] }, BucketInside, mid);
        var rimHigh = Ring(BucketHigh + 22, BucketTop + 10);
        m.Skin(high, rimHigh, _ => BucketRim, mid);
        m.Cap(low, m.P(0, At(Vector3.Zero)), BucketSide, mid);
        m.Cap(Ring(WaterAt, BucketLow + (BucketTop - BucketLow) * WaterAt / BucketHigh - 6), m.P(0, At(new Vector3(0, WaterAt, 0))), Water, At(new Vector3(0, -100, 0)));
        // the mop's strands: a skirt from the handle out to the water, a little above the rim
        m.Light = StrandLight;
        var strandsTop = foot0 + d0 * (BucketHigh + 90);
        var skirtTop = m.Loop(0, At(strandsTop), across1 * 45, across2 * 45, 8);
        var skirtLow = m.Loop(0, At(foot0 + d0 * (WaterAt - MopFoot)), across1 * 170, across2 * 170, 8);
        m.Skin(skirtTop, skirtLow, _ => MopStrand, At(foot0 + d0 * (WaterAt - MopFoot)));
        // the handle
        m.Light = HandleLight;
        m.Strut(0, At(strandsTop - d0 * 30), At(top0), 36, MopHandle);
        // (standing: two points no face uses, at the corners of a box round it and the floor before it, towards the room's door -- its box,
        // what a magic ball hits. Down, its own box)
        if (frame == 0)
        {
            m.P(0, new Vector3(-450, 0, -450));
            m.P(0, new Vector3(1300, 1600, 750));
        }
        return m.ToBody(header);
    }
}
