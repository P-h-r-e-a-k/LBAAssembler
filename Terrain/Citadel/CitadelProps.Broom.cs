using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The museum security room's broom (the user, 2026-11-17: "Let's take out some panes of glass in the door in the museum, let's add a broom
// between the cupboard and the blue security machine. The user needs to throw their magic ball through the missing glass in the door, to hit
// the broom, the broom then falls in a short cut scene and activates the red button which causes the door to open"): leaning against the
// room's west wall, its bristles on the floor; hit, it tips over onto the desk's red button. An actor turns only about the upright, so the
// fall is five bodies, the broom at each tilt, swapped one after another (RaceTrackStory.Chapters, MuseumBroom): each with its origin at the
// broom's foot, the handle along a direction from Lean (against the wall, -x) to Fall (onto the button: its place less the foot's, scene 15).
internal static partial class CitadelProps
{
    public const int BroomUp = 166;
    public static readonly int[] BroomFrames = { 166, 167, 168, 169, 170 };
    // (how far through its fall each body is: slow off the wall, quick at the end)
    private static readonly float[] BroomTilt = { 0f, 0.25f, 0.5f, 0.78f, 1f };
    private const float BroomLength = 1160, BroomHead = 260;
    private const int Bristle = 6 * 16 + 9, BroomBand = 3 * 16 + 4, Handle = 1 * 16 + 7;
    public static readonly Vector3 BroomLean = Vector3.Normalize(new Vector3(-270, 1170, 0)), BroomFall = Vector3.Normalize(new Vector3(168, 784, -801));

    // the standing broom's box (x, y, z min, then max): round it and the floor before it, towards the room's door (see BuildBroom)
    private static short[]? BroomBox(int generic) => generic == BroomUp ? new short[] { -350, 0, -400, 1700, 1250, 400 } : null;

    public static Body BuildBroom(byte[] header, int frame)
    {
        var m = new CarMesh("a broom", new[] { -1 });
        m.Light = 0.8f;
        var t = BroomTilt[frame];
        // (the handle's way: turned from Lean towards Fall about the axis between them, the head's breadth along that axis throughout)
        var axis = Vector3.Normalize(Vector3.Cross(BroomLean, BroomFall));
        var angle = MathF.Acos(Math.Clamp(Vector3.Dot(BroomLean, BroomFall), -1, 1)) * t;
        var d = Vector3.Transform(BroomLean, Quaternion.CreateFromAxisAngle(axis, angle));
        var thick = Vector3.Normalize(Vector3.Cross(axis, d));
        // the head: a block of bristles from the floor up, broad along the axis, a band round its top
        void Block(float s0, float s1, float broad, float deep, int colour)
        {
            var c = new[] { s0, s1 }.SelectMany(s => new[] { -1, 1 }.SelectMany(a => new[] { -1, 1 }.Select(b => d * s + axis * (a * broad) + thick * (b * deep)))).ToArray();
            var mid = d * ((s0 + s1) / 2);
            int[] H(params int[] k) => k.Select(i => m.P(0, c[i])).ToArray();
            // (corners: [s][a][b] = s*4 + a*2 + b)
            m.Out(H(0, 1, 3, 2), colour, mid); m.Out(H(4, 5, 7, 6), colour, mid);
            m.Out(H(0, 1, 5, 4), colour, mid); m.Out(H(2, 3, 7, 6), colour, mid);
            m.Out(H(0, 2, 6, 4), colour, mid); m.Out(H(1, 3, 7, 5), colour, mid);
        }
        Block(0, BroomHead - 50, 170, 55, Bristle);
        Block(BroomHead - 50, BroomHead, 150, 45, BroomBand);
        // the handle
        m.Strut(0, d * BroomHead, d * BroomLength, 34, Handle);
        // (standing: two points no face uses, at the corners of a box round it and the floor before it, towards the room's door -- its box,
        // what a magic ball hits. A ball of the lowest magic level, thrown from the doorway, goes straight, drops to the floor two cells short
        // of the wall and comes back (the higher levels' bounce on to it): it still knocks the broom over, 2026-11-17's test. Down, its own box)
        if (frame == 0)
        {
            m.P(0, new Vector3(-350, 0, -400));
            m.P(0, new Vector3(1700, 1250, 400));
        }
        return m.ToBody(header);
    }
}
