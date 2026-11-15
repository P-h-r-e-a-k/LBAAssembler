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
    // (2026-11-15, second round: a ball each bowler, his own colour -- the user, "Let's add different coloured spheros as bowling balls": the
    // Sphero's grey-teal shell, ramp 11, in red, blue, green and gold; generics past 127, an actor's body byte signed: (sbyte) in the scene)
    public const int SpheroRed = 130, SpheroBlue = 131, SpheroGreen = 132, SpheroGold = 133;
    public static readonly (int Generic, string Name, int Ramp)[] SpheroColours = { (SpheroRed, "red", 4), (SpheroBlue, "blue", 12), (SpheroGreen, "green", 8), (SpheroGold, "gold", 6) };
    // the poker table (BowlingAlley: a round table, its players sat round it)
    public const int PokerTable = 134;
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
    public static Body BuildSpheroBall(byte[] header, string gameDirectory) => BuildSpheroBall(header, gameDirectory, -1);
    public static Body BuildSpheroBall(byte[] header, string gameDirectory, int ramp)
    {
        // (its shell's ramp, 11, to `ramp`, each shade kept)
        int Paint(int c) => ramp >= 0 && c >> 4 == 11 ? ramp * 16 + (c & 15) : c;
        var body = Body.Read(HqrArchive.Open(Path.Combine(gameDirectory, "BODY.HQR")).Read(41), 2);
        var frames = Terrain.Palace.PalaceTorturers.ReadFrames(HqrArchive.Open(Path.Combine(gameDirectory, "ANIM.HQR")).Read(301), body.Bones.Count);
        var (points, _) = Terrain.Palace.PalaceTorturers.Posed(body, frames[^1].Bones);
        var middle = points.Aggregate(Vector3.Zero, (a, b) => a + b) / points.Length;
        var reach = points.Max(q => (q - middle).Length());
        var scale = 340f / Math.Max(1f, reach);
        var m = new CarMesh("a Sphero, curled up", new[] { -1 });
        m.Light = 0.85f;
        var at = points.Select(q => m.P(0, (q - middle) * scale)).ToArray();
        foreach (var f in body.Faces.Where(f => f.Points.Length >= 3)) m.Raw(f.Points.Select(i => at[i]).ToArray(), Paint(f.Colour), false);
        foreach (var sp in body.Spheres) m.Sphere(at[sp.Point], Math.Max(8, (int)(sp.Radius * scale)), Paint(sp.Colour));
        return m.ToBody(header);
    }

    // The poker table: round, green baize in a dark wooden rim on a thick post and a cross foot; the pot's chips in the middle, a hand of cards
    // face down before each of four places, a bottle -- its top TableTop up, Radius round
    public const int TableTop = 560, TableRadius = 520;
    public static Body BuildPokerTable(byte[] header)
    {
        var m = new CarMesh("the poker table", new[] { -1 });
        m.Light = 0.45f;
        const int Felt = 8 * 16 + 3, Rim = 1 * 16 + 3, Foot = 1 * 16 + 1, CardBack = 4 * 16 + 4, CardFace = 2 * 16 + 12, Chip = 4 * 16 + 6, ChipW = 3 * 16 + 12, ChipG = 6 * 16 + 9;
        const int N = 16;
        // (the top: the felt, the rim round it a little higher, the rim's edge down)
        // (fans of triangles about the middle: no face of more than a few points)
        var felt = m.Loop(0, new Vector3(0, TableTop, 0), new Vector3(TableRadius - 60, 0, 0), new Vector3(0, 0, TableRadius - 60), N);
        var mid = m.P(0, new Vector3(0, TableTop, 0));
        for (var i = 0; i < N; i++) m.Up(new[] { mid, felt[i], felt[(i + 1) % N] }, Felt);
        var rimIn = m.Loop(0, new Vector3(0, TableTop + 14, 0), new Vector3(TableRadius - 60, 0, 0), new Vector3(0, 0, TableRadius - 60), N);
        var rimOut = m.Loop(0, new Vector3(0, TableTop + 14, 0), new Vector3(TableRadius, 0, 0), new Vector3(0, 0, TableRadius), N);
        var rimLow = m.Loop(0, new Vector3(0, TableTop - 60, 0), new Vector3(TableRadius, 0, 0), new Vector3(0, 0, TableRadius), N);
        for (var i = 0; i < N; i++)
        {
            var j = (i + 1) % N;
            m.Up(new[] { rimIn[i], rimIn[j], rimOut[j], rimOut[i] }, Rim);
            m.Out(new[] { rimOut[i], rimOut[j], rimLow[j], rimLow[i] }, Rim, new Vector3(0, TableTop, 0));
            m.In(new[] { rimIn[i], rimIn[j], felt[j], felt[i] }, Rim, new Vector3(0, TableTop, 0));
        }
        var low = m.P(0, new Vector3(0, TableTop - 60, 0));
        for (var i = 0; i < N; i++) m.Out(new[] { low, rimLow[i], rimLow[(i + 1) % N] }, Foot, new Vector3(0, TableTop + 200, 0));
        // (the post and the foot)
        m.Box(0, new Vector3(0, (TableTop - 60) / 2, 0), new Vector3(120, TableTop - 60, 120), Foot);
        m.Box(0, new Vector3(0, 30, 0), new Vector3(620, 60, 110), Foot);
        m.Box(0, new Vector3(0, 30, 0), new Vector3(110, 60, 620), Foot);
        // (the pot: stacks of chips, red, white and gold)
        foreach (var (x, z, h, c) in new[] { (-40, 20, 70, Chip), (30, -30, 50, ChipW), (60, 50, 90, ChipG), (-20, -70, 40, Chip), (-80, -20, 60, ChipG) })
            m.Box(0, new Vector3(x, TableTop + h / 2, z), new Vector3(56, h, 56), c, top: c + 1);
        // (a hand of cards face down before each place, and one turned up; a few chips by each)
        foreach (var (dx, dz) in new[] { (0, 1), (1, 0), (0, -1), (-1, 0) })
        {
            var (cx, cz) = (dx * 300f, dz * 300f);
            var (ux, uz) = (dz * 1f, -dx * 1f);   // across the place
            for (var k = -1; k <= 1; k++)
            {
                var o = new Vector3(cx + ux * k * 46, TableTop + 4 + k + 1, cz + uz * k * 46);
                var a = new Vector3(ux * 30 + dx * 44, 0, uz * 30 + dz * 44);
                var b = new Vector3(ux * 30 - dx * 44, 0, uz * 30 - dz * 44);
                m.Flat(0, k == 1 ? CardFace : CardBack, o + a, o + b, o - a, o - b);
            }
            m.Box(0, new Vector3(cx + ux * 140 - dx * 30, TableTop + 20, cz + uz * 140 - dz * 30), new Vector3(50, 40, 50), Chip, top: Chip + 1);
        }
        // (a bottle by the pot)
        m.Box(0, new Vector3(150, TableTop + 110, -140), new Vector3(60, 220, 60), 7 * 16 + 3, top: 7 * 16 + 5);
        m.Box(0, new Vector3(150, TableTop + 260, -140), new Vector3(26, 80, 26), 7 * 16 + 3);
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
