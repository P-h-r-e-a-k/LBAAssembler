using System.IO;
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
    // (since 2026-11-15: the lanes are floor and the chains drawn by the engine -- RACEMOD.CPP BowlChainsDraw, link by link from a body of
    // one link -- so their numbers carry a chain's link and a pin's helmet alone; and the ball a curled-up Sphero of its own)
    public const int ChainLink = BowlingLanes, PinHelmet = PinChain, SpheroBall = 34;
    // the alley's scene units: three lanes along z, the outer two played (their middles), the foul line and the pins' rows
    // (the lanes are the room's floor since 2026-11-15 -- BowlingRoom: lanes 0 and 2 of its three played, the foul line at its row 36's
    // near edge, the pins' back row over its deck's row 10; the lanes' prop below no longer used)
    public const int LaneX0 = 7680, LaneX1 = 14848, LaneMiddleX = 11264, LaneMiddleZ = 12288, LaneHalf = 1024, LaneGutter = 260;
    public const int FoulZ = 18944, PinsZ = 5376, LaneTop = 0;
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

    // A pin's helmet alone, round the pin's feet (the Franco's head about 1,050 up): the steel dome, its brim, and the eye on top its chain
    // hooks on (RACEMOD.CPP BOWL_HELMET_TOP, 1,360) -- turned with the pin as it falls
    public static Body BuildPinHelmet(byte[] header)
    {
        var m = new CarMesh("a pin's helmet", new[] { -1 });
        m.Light = 0.7f;
        m.Ball(0, new Vector3(0, 1150, 0), 190, PinSteel);
        m.Box(0, new Vector3(0, 1010, 0), new Vector3(340, 50, 340), PinSteel);
        m.Box(0, new Vector3(0, 1325, 0), new Vector3(60, 70, 24), ChainColour);
        return m.ToBody(header);
    }

    // A chain's link, a body of its own the engine draws again and again down a chain (RACEMOD.CPP BowlChainsDraw): an upright oval of thick
    // steel bars, 110 long -- the chain's way its y; thicker than the first chains' rods, the user, 2026-11-15: "let's make the chains holding
    // them up slightly thicker so they're more visible"
    public static Body BuildChainLink(byte[] header)
    {
        var m = new CarMesh("a chain's link", new[] { -1 });
        m.Light = 0.75f;
        const float W = 46, H = 62, T = 28;
        foreach (var x in new[] { -W, W }) m.Box(0, new Vector3(x, 0, 0), new Vector3(T, 2 * H, T), ChainColour, top: ChainColour + 3);
        foreach (var y in new[] { -H, H }) m.Box(0, new Vector3(0, y, 0), new Vector3(2 * W + T, T, T), ChainColour, top: ChainColour + 3);
        return m.ToBody(header);
    }

    // The ball: a Sphero curled up (the game's own -- entity 28's body, BODY.HQR 41 -- posed as its hurt animation, ANIM.HQR 301, leaves it at
    // its last frame: legs tucked, a ball), its middle at the body's origin and BOWL_BALL_R (340) round, so the engine rolls it by turning it
    public static Body BuildSpheroBall(byte[] header, string gameDirectory)
    {
        var body = Body.Read(HqrArchive.Open(Path.Combine(gameDirectory, "BODY.HQR")).Read(41), 2);
        var frames = Terrain.Palace.PalaceTorturers.ReadFrames(HqrArchive.Open(Path.Combine(gameDirectory, "ANIM.HQR")).Read(301), body.Bones.Count);
        var (points, _) = Terrain.Palace.PalaceTorturers.Posed(body, frames[^1].Bones);
        var middle = points.Aggregate(Vector3.Zero, (a, b) => a + b) / points.Length;
        var reach = points.Max(q => (q - middle).Length());
        var scale = 340f / Math.Max(1f, reach);
        var m = new CarMesh("a Sphero, curled up", new[] { -1 });
        m.Light = 0.85f;
        var at = points.Select(q => m.P(0, (q - middle) * scale)).ToArray();
        foreach (var f in body.Faces.Where(f => f.Points.Length >= 3)) m.Raw(f.Points.Select(i => at[i]).ToArray(), f.Colour, false);
        foreach (var sp in body.Spheres) m.Sphere(at[sp.Point], Math.Max(8, (int)(sp.Radius * scale)), sp.Colour);
        return m.ToBody(header);
    }

    // The giant lever: a stand and its arm, up or thrown down toward the bowler (+z), a red knob on the end
    private static Body BuildLever(byte[] header, bool down)
    {
        var m = new CarMesh(down ? "the giant lever, pulled" : "the giant lever", new[] { -1 });
        m.Light = 0.3f;
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
