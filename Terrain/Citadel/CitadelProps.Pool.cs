using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The Imperial Hotel's swimming pool (scene 81; the user, 2026-11-11: "let's also do the Easter egg of Twinsen being able to swim in the
// imperial hotel's pool, let's also give it a diving board too"). In the game the pool is a dry tiled basin with a swimmer actor over its
// floor (entity 103's animation 146): the floor 1,536 up, the deck round it 2,048, the hall's floor 3,072. The build gives it
//   - the water (PoolWater): a see-through blue sheet over the basin at PoolSurface, drawn into the backdrop (OBJ_BACKGROUND, as the club's
//     wall is): its two rectangles' corners in the scene's own units, the body's actor far in the room's NW corner with a speck box;
//   - the diving board (DivingBoard): a stand on the deck's north side and a plank out over the water;
// and the engine does the swimming (RACEMOD.CPP pool=, pool_board=).
internal static partial class CitadelProps
{
    public const int PoolWater = 120, DivingBoard = 121;
    // the basin (scene units): two rectangles -- x0 z0 x1 z1 -- its floor, the water's surface
    public static readonly (int X0, int Z0, int X1, int Z1)[] PoolBasin = { (18500, 7500, 23500, 19500), (15500, 13500, 18500, 19500) };
    public const int PoolFloor = 1536, PoolSurface = 1990, PoolDeck = 2048;
    // the board: its stand's middle on the deck by the basin's north edge, the plank's tip over the water, where a dive comes down
    // (the user, 2026-11-12, on a picture: the green by the wide arm's west edge -- the stand on the deck at BoardX, the plank pointing
    // east, +x, over the water: the actor turned 1024, the body's own +z along it)
    public const int BoardX = 15000, BoardZ = 14600, BoardHigh = 330, BoardTipX = 16800, BoardLandX = 17900, BoardBeta = 1024;
    private const int PoolWaterColour = 192, BoardStand = 50, BoardPlank = 63, BoardGrip = 196;

    public const int PoolMiddleX = 21000, PoolMiddleZ = 13500;
    public static short[]? PoolBox(int generic) => generic == BowlingLanes ? new short[] { -1, 0, -1, 1, 1, 1 } : null;   // (the bowling lanes' boards: no box to be walked round)

    public static Body BuildPoolWater(byte[] header)
    {
        var m = new CarMesh("the hotel pool's water", new[] { -1 });
        m.Light = 1.0f;
        foreach (var (x0, z0, x1, z1) in PoolBasin)
            m.Pane(0, PoolWaterColour, new Vector3(x0 - PoolMiddleX, 0, z0 - PoolMiddleZ), new Vector3(x1 - PoolMiddleX, 0, z0 - PoolMiddleZ),
                new Vector3(x1 - PoolMiddleX, 0, z1 - PoolMiddleZ), new Vector3(x0 - PoolMiddleX, 0, z1 - PoolMiddleZ));
        return m.ToBody(header);
    }

    // The board, round its stand's foot (the deck's floor): the stand a block, the plank from over it out along +z, a grip's stripe on it
    public static Body BuildDivingBoard(byte[] header)
    {
        var m = new CarMesh("the diving board", new[] { -1 });
        m.Light = 0.9f;
        m.Box(0, new Vector3(0, BoardHigh / 2, 0), new Vector3(760, BoardHigh, 640), BoardStand, top: BoardStand + 3);
        var length = BoardTipX - BoardX + 300;
        m.Box(0, new Vector3(0, BoardHigh + 30, length / 2 - 200), new Vector3(560, 60, length), BoardPlank, top: BoardGrip);
        return m.ToBody(header);
    }
}
