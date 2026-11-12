using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The hidden bowling alley's props (BowlingAlley; the user, 2026-11-12: "a dark dingy place ... live spheros for balls, live Francos for pins ...
// helmets on their heads attached to chains so they can be reset into place by the bowlers pulling a giant lever"):
//   - the lanes (BowlingLanes): three boards between gutters, drawn in the scene's own units round LaneMiddle;
//   - a pin's helmet and chain (PinChain): over a pin's foot -- a steel helmet at the Franco's head, its chain up to the ceiling's dark;
//   - the giant lever, up and down (LeverUp, LeverDown): a stand and an arm with a red knob, standing at the bowler's side.
internal static partial class CitadelProps
{
    public const int BowlingLanes = 122, PinChain = 123, LeverUp = 124, LeverDown = 125;
    // the alley's scene units: three lanes along z, the outer two played (their middles), the foul line and the pins' rows
    public const int LaneX0 = 9216, LaneX1 = 18432, LaneMiddleX = 13824, LaneMiddleZ = 9600, LaneHalf = 1000, LaneGutter = 260;
    public const int FoulZ = 14000, PinsZ = 4800, LaneTop = 40;
    private const int Board = 21, BoardLine = 63, Gutter = 48, PinSteel = 52, ChainColour = 50, LeverSteel = 56, Knob = 70;

    public static Body BuildBowlingLanes(byte[] header)
    {
        var m = new CarMesh("the bowling lanes", new[] { -1 });
        m.Light = 0.55f;
        foreach (var cx in new[] { LaneX0, LaneMiddleX, LaneX1 })
        {
            // (the lane, its two gutters, a foul line across it)
            m.Box(0, new Vector3(cx - LaneMiddleX, LaneTop / 2, (FoulZ + PinsZ) / 2 - LaneMiddleZ), new Vector3(2 * LaneHalf, LaneTop, FoulZ - PinsZ + 600), Board, top: Board + 1);
            foreach (var s in new[] { -1, 1 })
                m.Box(0, new Vector3(cx - LaneMiddleX + s * (LaneHalf + LaneGutter / 2), 10, (FoulZ + PinsZ) / 2 - LaneMiddleZ), new Vector3(LaneGutter, 20, FoulZ - PinsZ + 600), Gutter);
            m.Plate(0, BoardLine, new Vector3(cx - LaneMiddleX - LaneHalf, LaneTop + 3, FoulZ - LaneMiddleZ), new Vector3(cx - LaneMiddleX + LaneHalf, LaneTop + 3, FoulZ - LaneMiddleZ),
                new Vector3(cx - LaneMiddleX + LaneHalf, LaneTop + 3, FoulZ + 60 - LaneMiddleZ), new Vector3(cx - LaneMiddleX - LaneHalf, LaneTop + 3, FoulZ + 60 - LaneMiddleZ));
        }
        return m.ToBody(header);
    }

    // The helmet and chain, round the pin's foot: the helmet on its head (a Franco's head about 1,050 up), the chain up to the dark
    public static Body BuildPinChain(byte[] header)
    {
        var m = new CarMesh("a pin's helmet and chain", new[] { -1 });
        m.Light = 0.7f;
        m.Ball(0, new Vector3(0, 1150, 0), 190, PinSteel);
        m.Box(0, new Vector3(0, 1010, 0), new Vector3(330, 50, 330), PinSteel);
        for (var y = 1340; y < 3900; y += 120) m.Rod(0, new Vector3(0, y, 0), new Vector3(0, y + 90, 0), ChainColour);
        return m.ToBody(header);
    }

    // The giant lever: a stand and its arm, up or thrown down toward the bowler (+z), a red knob on the end
    private static Body BuildLever(byte[] header, bool down)
    {
        var m = new CarMesh(down ? "the giant lever, pulled" : "the giant lever", new[] { -1 });
        m.Light = 0.8f;
        m.Box(0, new Vector3(0, 250, 0), new Vector3(700, 500, 500), LeverSteel, top: LeverSteel + 2);
        var tip = down ? new Vector3(0, 650, 1250) : new Vector3(0, 1900, -250);
        m.Rod(0, new Vector3(0, 500, 0), tip, LeverSteel);
        m.Rod(0, new Vector3(30, 500, 0), tip + new Vector3(30, 0, 0), LeverSteel);
        m.Rod(0, new Vector3(-30, 500, 0), tip + new Vector3(-30, 0, 0), LeverSteel);
        m.Ball(0, tip, 110, Knob);
        return m.ToBody(header);
    }
    public static Body BuildLeverUp(byte[] header) => BuildLever(header, false);
    public static Body BuildLeverDown(byte[] header) => BuildLever(header, true);
}
