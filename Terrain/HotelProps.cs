using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The Imperial Hotel's new furniture (Otringal; OtringalHotel builds its upper floor: the user, 2026-11-22, "let's add a second floor with
// some bedrooms for Twinsen to spend the night in ... model a lift in the wall the sofa is in front of ... 3 different rooms ... the luxury
// room ... Super large, 4 poster bed, as grand as we can make it"; "Do some research before you model these" -- the grand hotels' rooms,
// corridors and old lifts, E:\dump\TEMP\nov22\research_hotel_skate.md). Each body stands on the floor at its origin, its front towards +z
// (an actor's beta 0 looks along +z); a wall piece's back on z 0. Entity 17's bodies (CitadelProps.InstallMore), generics 229-244:
//   - the lift's front (LiftFront, LiftOpen): a marble surround, two brass doors with a sunburst in relief -- slid apart, the car's red
//     lacquer, its mirror and brass rail between them -- the floor dial over the lintel (a half disc, its arrow), the call button, a lantern
//     each side;
//   - the reception desk (ReceptionDesk): a mahogany counter, its marble top, a brass bell and the register, behind it a board of
//     pigeonholes with the rooms' keys, a lamp;
//   - the corridor's doors (CorridorDoors): five panelled doors in their architraves along a wall, each its brass number 101-105, a sconce
//     between each two, prints in gilt frames; the suite's double doors (SuiteDoors) under a pediment, "Imperial Suite" on a gilt plaque;
//   - the four-poster (FourPoster) on its dais: turned posts, a canopy with a scalloped pelmet, drapes tied back at each post, a quilted
//     spread, bolster and cushions;
//   - a crystal chandelier (Chandelier: its foot at the ceiling's height, hanging down), a damask sofa (Sofa), a coffee table (CoffeeTable),
//     a grand piano (GrandPiano), a table for eight laid with candelabra (DiningTable), a marble fireplace with its overmantel mirror, clock
//     and candlesticks (Fireplace), a patterned rug (HotelRug, walked over), a panelled partition with a doorway (Partition: the basic room's
//     bathroom), a large oil painting in a gilt frame (Painting).
internal static class HotelProps
{
    public const int LiftFront = 229, LiftOpen = 230, ReceptionDesk = 231, CorridorDoors = 232, SuiteDoors = 233, FourPoster = 234, Chandelier = 235,
                     Sofa = 236, CoffeeTable = 237, GrandPiano = 238, DiningTable = 239, Fireplace = 240, HotelRug = 241, Partition = 242, Painting = 243;
    // (each of the corridor's doors a body of its own -- the five in one passed the engine's 550 points: RoomDoor + its number's last digit - 1)
    public const int RoomDoor = 244;

    // (the palette's ramps, lit: a face's light takes it up to nine steps along its ramp -- each base low enough to stay in it)
    private const int Mahogany = 1 * 16 + 3, MahoganyDark = 1 * 16 + 1, Walnut = 1 * 16 + 5, Brass = 6 * 16 + 6, BrassDark = 6 * 16 + 3,
        Marble = 3 * 16 + 9, MarbleVein = 3 * 16 + 5, Velvet = 4 * 16 + 3, VelvetDark = 4 * 16 + 1, Linen = 3 * 16 + 10, Ivory = 3 * 16 + 8,
        Lacquer = 4 * 16 + 4, Teal = 9 * 16 + 5, Ebony = 3 * 16 + 1, Glass = 3 * 16 + 4, Felt = 8 * 16 + 4;
    // (drawn as they are: the dial's face, the lamps' glow, the crystal drops, the plaques' engraving, the fire)
    private const int GlowYellow = 6 * 16 + 15, CrystalWhite = 3 * 16 + 15, Engraving = 6 * 16 + 1, FireOrange = 4 * 16 + 12, FireYellow = 6 * 16 + 13,
        PaintSky = 12 * 16 + 9, PaintGreen = 7 * 16 + 7, PaintDark = 1 * 16 + 1;

    // (the corridor's doors: how far apart, how wide, how high -- OtringalHotel puts the zones before them)
    public const float DoorPitch = 3072, DoorWide = 1100, DoorHigh = 2300;
    public const int FirstRoom = 101, Rooms = 5;
    // (the lift's opening: how wide, how high; its surround's)
    public const float LiftWide = 1250, LiftHigh = 2300;

    public static List<string> Install(string gameDirectory) => Citadel.CitadelProps.InstallMore(gameDirectory, "the Imperial Hotel", new (int, string, Func<byte[], Body>)[]
    {
        (LiftFront, "the hotel's lift, its doors shut", h => BuildLift(h, false)),
        (LiftOpen, "the hotel's lift, its doors open", h => BuildLift(h, true)),
        (ReceptionDesk, "the hotel's reception desk", BuildReceptionDesk),
        (CorridorDoors, "the hotel's corridor, its sconces and prints", BuildCorridorDoors),
        (RoomDoor, "room 101's door", h => BuildRoomDoor(h, 101)), (RoomDoor + 1, "room 102's door", h => BuildRoomDoor(h, 102)),
        (RoomDoor + 2, "room 103's door", h => BuildRoomDoor(h, 103)), (RoomDoor + 3, "room 104's door", h => BuildRoomDoor(h, 104)),
        (RoomDoor + 4, "room 105's door", h => BuildRoomDoor(h, 105)),
        (SuiteDoors, "the Imperial Suite's doors", BuildSuiteDoors),
        (FourPoster, "the Imperial Suite's four-poster bed", BuildFourPoster),
        (Chandelier, "a crystal chandelier", BuildChandelier),
        (Sofa, "a damask sofa", BuildSofa),
        (CoffeeTable, "a coffee table", BuildCoffeeTable),
        (GrandPiano, "a grand piano", BuildGrandPiano),
        (DiningTable, "a table for eight", BuildDiningTable),
        (Fireplace, "a marble fireplace", BuildFireplace),
        (HotelRug, "a hotel rug", BuildRug),
        (Partition, "a panelled partition", BuildPartition),
        (Painting, "an oil painting", BuildPainting),
    });

    // The boxes the scene stands them in: the rug walked over (OtringalHotel gives it the walk-over armour too); the wall pieces only as
    // deep as they stand out from their wall
    public static short[]? Box(int generic) => generic switch
    {
        LiftFront or LiftOpen => new short[] { -900, 0, 0, 900, 2900, 160 },
        // (the counter only: the board of keys behind it is drawn, the clerk stands between them -- its whole extent's box pushed him out)
        ReceptionDesk => new short[] { -1320, 0, -440, 1320, 1150, 440 },
        CorridorDoors => new short[] { 0, 0, 0, (short)(DoorPitch * (Rooms - 1)), 2200, 120 },
        >= RoomDoor and < RoomDoor + Rooms => new short[] { -800, 0, 0, 800, 2600, 120 },
        SuiteDoors => new short[] { -1300, 0, 0, 1300, 3000, 160 },
        HotelRug => new short[] { -10, 0, -10, 10, 4, 10 },
        Chandelier => new short[] { -10, -10, -10, 10, 10, 10 },
        Painting => new short[] { -900, 0, 0, 900, 1400, 60 },
        _ => null,
    };

    private static void Quad(CarMesh m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int colour, Vector3 towards, bool unlit = false) =>
        m.In(new[] { m.P(0, a), m.P(0, b), m.P(0, c), m.P(0, d) }, colour, towards, unlit);

    // ---- a little pixel font: the doors' numbers and the suite's plaque
    private static readonly Dictionary<char, string[]> Font = new()
    {
        ['0'] = new[] { "###", "#.#", "#.#", "#.#", "###" }, ['1'] = new[] { ".#", "##", ".#", ".#", ".#" },
        ['2'] = new[] { "###", "..#", "###", "#..", "###" }, ['3'] = new[] { "###", "..#", ".##", "..#", "###" },
        ['4'] = new[] { "#.#", "#.#", "###", "..#", "..#" }, ['5'] = new[] { "###", "#..", "###", "..#", "###" },
        ['I'] = new[] { "###", ".#.", ".#.", ".#.", "###" }, ['M'] = new[] { "#...#", "##.##", "#.#.#", "#...#", "#...#" },
        ['P'] = new[] { "##.", "#.#", "##.", "#..", "#.." }, ['E'] = new[] { "###", "#..", "##.", "#..", "###" },
        ['R'] = new[] { "##.", "#.#", "##.", "#.#", "#.#" }, ['A'] = new[] { ".#.", "#.#", "###", "#.#", "#.#" },
        ['L'] = new[] { "#..", "#..", "#..", "#..", "###" }, ['S'] = new[] { "###", "#..", "###", "..#", "###" },
        ['U'] = new[] { "#.#", "#.#", "#.#", "#.#", "###" }, ['T'] = new[] { "###", ".#.", ".#.", ".#.", ".#." },
        ['H'] = new[] { "#.#", "#.#", "###", "#.#", "#.#" }, ['O'] = new[] { "###", "#.#", "#.#", "#.#", "###" },
        [' '] = new[] { ".", ".", ".", ".", "." },
    };
    private static int Columns(string text) => text.Sum(ch => Font[ch][0].Length + 1) - 1;
    internal static int TextColumns(string text) => Columns(text);
    // a text's pixels anywhere (the hotel's outside: HotelFacade): `place` takes (column, row) -- 0,0 the text's top left -- to the body's
    // frame; each run of a row's pixels one quad, facing `towards`, unlit
    internal static void TextOn(CarMesh m, string text, float px, Func<float, float, Vector3> place, int colour, Vector3 towards)
    {
        var left = 0;
        foreach (var ch in text)
        {
            var g = Font[ch];
            for (var r = 0; r < g.Length; r++)
                for (var c = 0; c < g[r].Length; c++)
                {
                    if (g[r][c] != '#' || (c > 0 && g[r][c - 1] == '#')) continue;
                    var end = c;
                    while (end + 1 < g[r].Length && g[r][end + 1] == '#') end++;
                    m.In(new[] { m.P(0, place(left + c, r)), m.P(0, place(left + end + 1, r)), m.P(0, place(left + end + 1, r + 1)), m.P(0, place(left + c, r + 1)) }, colour, towards, unlit: true);
                }
            left += g[0].Length + 1;
        }
    }
    // a text's pixels on a plane facing +z at z, its middle at (x, y), each pixel `px` square
    private static void Text(CarMesh m, string text, float x, float y, float z, float px, int colour)
    {
        var left = x - Columns(text) * px / 2;
        var top = y + 5 * px / 2;
        foreach (var ch in text)
        {
            var g = Font[ch];
            // (each run of a row's pixels one quad)
            for (var r = 0; r < g.Length; r++)
                for (var c = 0; c < g[r].Length; c++)
                {
                    if (g[r][c] != '#' || (c > 0 && g[r][c - 1] == '#')) continue;
                    var end = c;
                    while (end + 1 < g[r].Length && g[r][end + 1] == '#') end++;
                    float x0 = left + c * px, x1 = left + (end + 1) * px, y1 = top - r * px;
                    Quad(m, new(x0, y1 - px, z), new(x1, y1 - px, z), new(x1, y1, z), new(x0, y1, z), colour, new((x0 + x1) / 2, y1 - px / 2, z + 100), unlit: true);
                }
            left += (g[0].Length + 1) * px;
        }
    }

    // ---- the lift
    private static Body BuildLift(byte[] header, bool open)
    {
        var m = new CarMesh(open ? "the lift, open" : "the lift, shut", new[] { -1 });
        m.Light = 0.8f;
        float half = LiftWide / 2, jamb = 220;
        // the marble surround: jambs, lintel, a step of marble at its foot
        foreach (var s in new[] { -1f, 1f })
            m.Box(0, new Vector3(s * (half + jamb / 2), LiftHigh / 2 + 100, 70), new Vector3(jamb, LiftHigh + 200, 140), Marble, Marble);
        m.Box(0, new Vector3(0, LiftHigh + 250, 75), new Vector3(LiftWide + 2 * jamb + 80, 300, 150), Marble, Marble);
        m.Box(0, new Vector3(0, LiftHigh + 410, 85), new Vector3(LiftWide + 2 * jamb + 160, 40, 170), BrassDark, Brass);
        m.Box(0, new Vector3(0, 15, 120), new Vector3(LiftWide + 2 * jamb, 30, 240), MarbleVein, Marble);
        // the floor dial over the lintel: a brass half disc, its numerals' ticks, its arrow (at the ground floor shut, between the floors open)
        var dialC = new Vector3(0, LiftHigh + 430, 175);
        const int N = 10; const float R = 330;
        var rim = Enumerable.Range(0, N + 1).Select(k => m.P(0, dialC + new Vector3(-R * MathF.Cos(MathF.PI * k / N), R * MathF.Sin(MathF.PI * k / N), 0))).ToArray();
        var hub = m.P(0, dialC);
        for (var k = 0; k < N; k++) m.In(new[] { hub, rim[k], rim[k + 1] }, Brass, dialC + new Vector3(0, 0, 200));
        foreach (var k in new[] { 1, 5, 9 })
        {
            var a = MathF.PI * k / N;
            m.Box(0, dialC + new Vector3(-0.8f * R * MathF.Cos(a), 0.8f * R * MathF.Sin(a), 12), new Vector3(36, 36, 10), Engraving);
        }
        var arrowAngle = open ? MathF.PI / 2 : MathF.PI * 0.1f;
        m.Strut(0, dialC + new Vector3(0, 0, 20), dialC + new Vector3(-0.72f * R * MathF.Cos(arrowAngle), 0.72f * R * MathF.Sin(arrowAngle), 20), 14, Ebony);
        m.Ball(0, dialC + new Vector3(0, 0, 22), 34, BrassDark);
        // the call button: a brass plate, two buttons
        m.Box(0, new Vector3(half + jamb + 170, 1150, 20), new Vector3(120, 260, 30), Brass, Brass);
        m.Ball(0, new Vector3(half + jamb + 170, 1210, 40), 22, GlowYellow);
        m.Ball(0, new Vector3(half + jamb + 170, 1090, 40), 22, Ivory);
        // a lantern each side
        foreach (var s in new[] { -1f, 1f })
        {
            var x = s * (half + jamb + 330);
            m.Box(0, new Vector3(x, 1800, 40), new Vector3(60, 60, 80), BrassDark);
            m.Box(0, new Vector3(x, 1930, 110), new Vector3(150, 220, 150), Glass, BrassDark);
            m.Ball(0, new Vector3(x, 1930, 110), 50, GlowYellow);
        }
        if (!open)
        {
            // the doors: two brass leaves meeting in the middle, a sunburst in relief over both, the line between them
            foreach (var s in new[] { -1f, 1f })
            {
                m.Box(0, new Vector3(s * half / 2, LiftHigh / 2, 40), new Vector3(half - 10, LiftHigh, 40), BrassDark, Brass);
                // (the leaf's panels: a raised frame)
                m.Box(0, new Vector3(s * half / 2, LiftHigh * 0.32f, 64), new Vector3(half - 160, 40, 10), Brass);
                m.Box(0, new Vector3(s * half / 2, LiftHigh * 0.62f, 64), new Vector3(half - 160, 40, 10), Brass);
            }
            var sun = new Vector3(0, LiftHigh * 0.8f, 66);
            for (var k = 0; k <= 8; k++)
            {
                var a = MathF.PI * k / 8;
                m.Strut(0, sun, sun + new Vector3(-320 * MathF.Cos(a), 260 * MathF.Sin(a), 0), 12, Brass);
            }
            m.Ball(0, sun, 70, Brass);
        }
        else
        {
            // the car between the leaves slid back into the jambs: its red lacquer back and sides, its mirror, its brass rail, its carpet
            m.Box(0, new Vector3(0, LiftHigh / 2, 8), new Vector3(LiftWide - 20, LiftHigh, 16), Lacquer, Lacquer);
            Quad(m, new(-half + 160, 700, 20), new(half - 160, 700, 20), new(half - 160, 1900, 20), new(-half + 160, 1900, 20), Glass, new(0, 1300, 300));
            m.Box(0, new Vector3(0, 900, 40), new Vector3(LiftWide - 120, 30, 30), Brass);
            m.Box(0, new Vector3(0, 6, 60), new Vector3(LiftWide - 20, 12, 100), VelvetDark);
            // (the leaves' edges, just showing at the jambs)
            foreach (var s in new[] { -1f, 1f }) m.Box(0, new Vector3(s * (half - 30), LiftHigh / 2, 50), new Vector3(60, LiftHigh, 40), BrassDark, Brass);
        }
        return m.ToBody(header);
    }

    // ---- the reception desk: its front +z, the clerk behind it (at -z)
    private static Body BuildReceptionDesk(byte[] header)
    {
        var m = new CarMesh("the reception desk", new[] { -1 });
        m.Light = 0.8f;
        const float W = 1200, D = 350, H = 1050;
        // the counter: its front's raised panels, a plinth, the marble top overhanging
        m.Box(0, new Vector3(0, H / 2, 0), new Vector3(2 * W, H, 2 * D), Mahogany, Mahogany);
        m.Box(0, new Vector3(0, 40, 10), new Vector3(2 * W + 40, 80, 2 * D + 40), MahoganyDark);
        for (var k = -2; k <= 2; k++) m.Box(0, new Vector3(k * 450, H / 2, D + 12), new Vector3(360, H - 300, 24), Walnut, Walnut);
        m.Box(0, new Vector3(0, H + 30, 20), new Vector3(2 * W + 120, 60, 2 * D + 160), Marble, Marble);
        m.Box(0, new Vector3(0, H - 60, D + 30), new Vector3(2 * W + 40, 40, 30), Brass);
        // the bell and the register on the top, a lamp at its end
        m.Box(0, new Vector3(-500, H + 75, 120), new Vector3(70, 30, 70), BrassDark);
        m.Ball(0, new Vector3(-500, H + 120, 120), 48, Brass);
        m.Box(0, new Vector3(300, H + 90, 40), new Vector3(520, 60, 360), VelvetDark, Linen);
        m.Box(0, new Vector3(W - 160, H + 200, -60), new Vector3(30, 340, 30), Brass);
        m.Box(0, new Vector3(W - 160, H + 420, -60), new Vector3(260, 160, 260), GlowYellow);
        // behind the clerk: the board of the rooms' keys, its pigeonholes and their brass tags
        const float Bz = -D - 900;
        m.Box(0, new Vector3(0, 1500, Bz), new Vector3(1800, 1400, 120), Mahogany, Mahogany);
        for (var r = 0; r < 3; r++)
            for (var c = 0; c < 5; c++)
            {
                float x = -640 + c * 320, y = 1150 + r * 360;
                m.Box(0, new Vector3(x, y, Bz + 70), new Vector3(220, 220, 20), MahoganyDark);
                if ((r * 5 + c) % 3 != 1) m.Box(0, new Vector3(x, y - 40, Bz + 90), new Vector3(40, 90, 10), Brass);
            }
        return m.ToBody(header);
    }

    // ---- a panelled door in its architrave, a brass number plate (its back on z 0, its middle at x)
    private static void Door(CarMesh m, float x, float wide, float high, string number, int leaf)
    {
        // the architrave
        foreach (var s in new[] { -1f, 1f }) m.Box(0, new Vector3(x + s * (wide / 2 + 70), high / 2 + 60, 30), new Vector3(140, high + 120, 60), Ivory, Ivory);
        m.Box(0, new Vector3(x, high + 150, 40), new Vector3(wide + 360, 120, 80), Ivory, Ivory);
        m.Box(0, new Vector3(x, high + 225, 50), new Vector3(wide + 420, 30, 100), Brass);
        // the leaf: four raised panels, its knob
        m.Box(0, new Vector3(x, high / 2, 20), new Vector3(wide, high, 40), leaf, leaf);
        foreach (var (px, py) in new[] { (-1, 0.27f), (1, 0.27f), (-1, 0.7f), (1, 0.7f) })
            m.Box(0, new Vector3(x + px * wide / 4, high * py, 44), new Vector3(wide / 2 - 120, high * (py < 0.5f ? 0.36f : 0.42f), 10), leaf + 2);
        m.Ball(0, new Vector3(x + wide / 2 - 110, high * 0.48f, 70), 32, Brass);
        // its number on a brass plate at eye height
        if (number.Length > 0)
        {
            m.Box(0, new Vector3(x, high * 0.86f, 48), new Vector3(Columns(number) * 34 + 80, 5 * 34 + 70, 10), Brass, Brass);
            Text(m, number, x, high * 0.86f, 55, 34, Engraving);
        }
    }

    // ---- a room's door, its number
    private static Body BuildRoomDoor(byte[] header, int room)
    {
        var m = new CarMesh($"room {room}'s door", new[] { -1 });
        m.Light = 0.8f;
        Door(m, 0, DoorWide, DoorHigh, room.ToString(), Mahogany);
        return m.ToBody(header);
    }

    // ---- the corridor's wall between its rooms' doors (the doors DoorPitch apart from x 0 along +x): a sconce between each two, a print in a
    // gilt frame under it
    private static Body BuildCorridorDoors(byte[] header)
    {
        var m = new CarMesh("the corridor's sconces", new[] { -1 });
        m.Light = 0.8f;
        for (var k = 0; k + 1 < Rooms; k++)
        {
            var x = (k + 0.5f) * DoorPitch;
            // a two-armed sconce, its shades
            m.Box(0, new Vector3(x, 1750, 30), new Vector3(80, 160, 60), Brass);
            foreach (var s in new[] { -1f, 1f })
            {
                m.Strut(0, new Vector3(x, 1740, 60), new Vector3(x + s * 170, 1820, 120), 14, Brass);
                m.Box(0, new Vector3(x + s * 170, 1900, 120), new Vector3(110, 110, 110), GlowYellow);
            }
            // a print in a gilt frame under it
            m.Box(0, new Vector3(x, 1150, 15), new Vector3(620, 460, 30), Brass, Brass);
            Quad(m, new(x - 260, 950, 34), new(x + 260, 950, 34), new(x + 260, 1350, 34), new(x - 260, 1350, 34), k % 2 == 0 ? PaintSky : PaintGreen, new(x, 1150, 200), unlit: true);
        }
        return m.ToBody(header);
    }

    // ---- the Imperial Suite's double doors under a pediment, the plaque
    private static Body BuildSuiteDoors(byte[] header)
    {
        var m = new CarMesh("the suite's doors", new[] { -1 });
        m.Light = 0.8f;
        const float W = 1700, H = 2500;
        // two leaves in one architrave
        Door(m, -W / 4, W / 2 - 10, H, "", Walnut);
        Door(m, W / 4, W / 2 - 10, H, "", Walnut);
        // pilasters either side, the pediment over them
        foreach (var s in new[] { -1f, 1f }) m.Box(0, new Vector3(s * (W / 2 + 300), H / 2 + 150, 60), new Vector3(200, H + 300, 120), Marble, Marble);
        var top = H + 300;
        m.Box(0, new Vector3(0, top, 70), new Vector3(W + 900, 120, 140), Marble, Marble);
        var l = m.P(0, new Vector3(-W / 2 - 450, top + 60, 90)); var r = m.P(0, new Vector3(W / 2 + 450, top + 60, 90)); var apex = m.P(0, new Vector3(0, top + 520, 90));
        m.In(new[] { l, r, apex }, Marble, new Vector3(0, top + 200, 400));
        m.Box(0, new Vector3(0, top + 70, 145), new Vector3(W + 920, 30, 20), Brass);
        // the plaque on the pediment's face
        m.Box(0, new Vector3(0, top + 230, 100), new Vector3(Columns("IMPERIAL SUITE") * 30 + 120, 5 * 30 + 80, 12), Brass, Brass);
        Text(m, "IMPERIAL SUITE", 0, top + 230, 108, 30, Engraving);
        return m.ToBody(header);
    }

    // ---- the four-poster bed on its dais, its head at -z, its foot towards +z
    private static Body BuildFourPoster(byte[] header)
    {
        var m = new CarMesh("the four-poster", new[] { -1 });
        m.Light = 0.8f;
        const float W = 1150, L = 1300, Dais = 160, Mat = 750, Post = 2900;
        // the dais, its gilt edge
        m.Box(0, new Vector3(0, Dais / 2, 0), new Vector3(2 * W + 600, Dais, 2 * L + 600), Velvet, Velvet);
        m.Box(0, new Vector3(0, Dais - 10, 0), new Vector3(2 * W + 640, 20, 2 * L + 640), Brass);
        // the bed's frame, the mattress, the quilted spread (a pattern of buttons), the bolster and the cushions against the headboard
        m.Box(0, new Vector3(0, Dais + 220, 0), new Vector3(2 * W, 260, 2 * L), Walnut, Walnut);
        m.Box(0, new Vector3(0, Dais + Mat - 120, 40), new Vector3(2 * W - 60, 240, 2 * L - 80), Linen, Linen);
        m.Box(0, new Vector3(0, Dais + Mat + 10, 200), new Vector3(2 * W - 20, 40, 2 * L - 380), Velvet, Velvet);
        for (var i = -1; i <= 1; i++) for (var j = -1; j <= 1; j++) m.Ball(0, new Vector3(i * 560, Dais + Mat + 40, 200 + j * 600), 18, Brass);
        m.Box(0, new Vector3(0, Dais + Mat + 60, -L + 220), new Vector3(2 * W - 140, 180, 220), Linen, Linen);
        for (var k = -1; k <= 1; k++) m.Box(0, new Vector3(k * 600, Dais + Mat + 200, -L + 130), new Vector3(520, 300, 120), k == 0 ? Ivory : Velvet, Ivory);
        // the headboard: a tall carved panel, gilt crest
        m.Box(0, new Vector3(0, Dais + 1100, -L - 40), new Vector3(2 * W, 1500, 80), Walnut, Walnut);
        m.Box(0, new Vector3(0, Dais + 1100, -L + 10), new Vector3(2 * W - 300, 1100, 20), Velvet);
        m.Ball(0, new Vector3(0, Dais + 1900, -L), 90, Brass);
        // the four posts, turned (a stack of rings), their finials
        foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
        {
            float x = sx * (W - 40), z = sz * (L - 40);
            var rings = new (float Y, float R)[] { (Dais, 90), (Dais + 700, 95), (Dais + 800, 60), (Dais + Post * 0.55f, 80), (Dais + Post, 50) };
            var loops = rings.Select(r => m.Loop(0, new Vector3(x, r.Y, z), new Vector3(r.R, 0, 0), new Vector3(0, 0, r.R), 6)).ToArray();
            for (var k = 0; k + 1 < loops.Length; k++) m.Skin(loops[k], loops[k + 1], _ => Walnut, new Vector3(x, (rings[k].Y + rings[k + 1].Y) / 2, z));
            m.Ball(0, new Vector3(x, Dais + Post + 80, z), 85, Brass);
        }
        // the canopy: a frame between the posts' tops, a scalloped pelmet round it, the roof of cloth
        var ty = Dais + Post;
        m.Box(0, new Vector3(0, ty, 0), new Vector3(2 * W + 80, 60, 2 * L + 80), Walnut, Velvet);
        foreach (var (len, alongX, at) in new[] { (2 * W, true, L + 40f), (2 * W, true, -L - 40f), (2 * L, false, W + 40f), (2 * L, false, -W - 40f) })
            for (var k = 0; k < 4; k++)
            {
                var t = -len / 2 + (k + 0.5f) * len / 4;
                var c = alongX ? new Vector3(t, ty - 160, at) : new Vector3(at, ty - 160, t);
                m.Box(0, c, alongX ? new Vector3(len / 4 - 20, 300, 30) : new Vector3(30, 300, len / 4 - 20), k % 2 == 0 ? Velvet : VelvetDark, Velvet);
            }
        // the drapes: a fall of cloth at each post, tied back with a gold tassel
        foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
        {
            float x = sx * (W + 60), z = sz * (L - 160);
            m.Box(0, new Vector3(x, Dais + 1900, z), new Vector3(120, 1600, 360), Velvet, Velvet);
            m.Box(0, new Vector3(x, Dais + 600, z + sz * 80), new Vector3(140, 1100, 200), Velvet, Velvet);
            m.Ball(0, new Vector3(x, Dais + 1150, z), 55, Brass);
        }
        return m.ToBody(header);
    }

    // ---- a crystal chandelier: its foot (the origin) at the ceiling, hanging down from it
    private static Body BuildChandelier(byte[] header)
    {
        var m = new CarMesh("a chandelier", new[] { -1 });
        m.Light = 0.85f;
        m.Strut(0, new Vector3(0, 0, 0), new Vector3(0, -450, 0), 18, Brass);
        m.Ball(0, new Vector3(0, -500, 0), 90, Brass);
        // two tiers of arms, a candle and a drop on each, strings of crystals between
        foreach (var (y, r, n) in new[] { (-620f, 520f, 8), (-420f, 330f, 6) })
            for (var k = 0; k < n; k++)
            {
                var a = MathF.Tau * k / n;
                var tip = new Vector3(r * MathF.Cos(a), y, r * MathF.Sin(a));
                m.Strut(0, new Vector3(0, -540, 0), tip, 12, Brass);
                m.Box(0, tip + new Vector3(0, 60, 0), new Vector3(36, 120, 36), Ivory);
                m.Ball(0, tip + new Vector3(0, 150, 0), 26, GlowYellow);
                m.Ball(0, tip + new Vector3(0, -80, 0), 22, CrystalWhite);
                var mid = new Vector3(0.6f * r * MathF.Cos(a + MathF.PI / n), y - 120, 0.6f * r * MathF.Sin(a + MathF.PI / n));
                m.Ball(0, mid, 18, CrystalWhite);
            }
        m.Ball(0, new Vector3(0, -760, 0), 60, CrystalWhite);
        m.Ball(0, new Vector3(0, -860, 0), 34, CrystalWhite);
        return m.ToBody(header);
    }

    // ---- a damask sofa: three seats, rolled arms, gilt feet, its back at -z
    private static Body BuildSofa(byte[] header)
    {
        var m = new CarMesh("a sofa", new[] { -1 });
        m.Light = 0.8f;
        const float W = 1050, D = 420, Seat = 430;
        m.Box(0, new Vector3(0, Seat - 120, 0), new Vector3(2 * W, 200, 2 * D), Teal, Teal);
        for (var k = -1; k <= 1; k++) m.Box(0, new Vector3(k * 2 * W / 3, Seat + 30, 40), new Vector3(2 * W / 3 - 30, 100, 2 * D - 140), Teal, Teal + 2);
        m.Box(0, new Vector3(0, Seat + 330, -D + 70), new Vector3(2 * W, 560, 140), Teal, Teal);
        m.Box(0, new Vector3(0, Seat + 625, -D + 70), new Vector3(2 * W + 40, 40, 160), Mahogany, Brass);
        foreach (var s in new[] { -1f, 1f })
        {
            m.Box(0, new Vector3(s * (W - 70), Seat + 120, 0), new Vector3(140, 300, 2 * D), Teal, Teal);
            m.Ball(0, new Vector3(s * (W - 70), Seat + 270, D - 40), 70, Teal + 1);
        }
        foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            m.Box(0, new Vector3(sx * (W - 60), 100, sz * (D - 60)), new Vector3(60, 200, 60), Brass);
        return m.ToBody(header);
    }

    // ---- a coffee table: a marble top on gilt legs, a vase of flowers
    private static Body BuildCoffeeTable(byte[] header)
    {
        var m = new CarMesh("a coffee table", new[] { -1 });
        m.Light = 0.8f;
        const float W = 600, D = 380, H = 420;
        m.Box(0, new Vector3(0, H - 20, 0), new Vector3(2 * W, 40, 2 * D), Marble, Marble);
        m.Box(0, new Vector3(0, H - 60, 0), new Vector3(2 * W - 60, 40, 2 * D - 60), Brass);
        foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
            m.Strut(0, new Vector3(sx * (W - 60), H - 60, sz * (D - 60)), new Vector3(sx * (W - 20), 0, sz * (D - 20)), 22, Brass);
        m.Box(0, new Vector3(0, H + 110, 0), new Vector3(140, 180, 140), Glass, Glass);
        foreach (var (fx, fz, c) in new[] { (0, 0, Velvet + 4), (-70, 40, FireYellow), (60, -50, Velvet + 2), (40, 70, Ivory + 4) })
            m.Ball(0, new Vector3(fx, H + 280, fz), 60, c);
        return m.ToBody(header);
    }

    // ---- a grand piano: its keyboard towards +z, the lid propped up
    private static Body BuildGrandPiano(byte[] header)
    {
        var m = new CarMesh("a grand piano", new[] { -1 });
        m.Light = 0.85f;
        const float Top = 950, Case = 330;
        // the case: a curved outline (its curve on the right), black lacquer
        var outline = new List<Vector3> { new(-720, 0, 300), new(720, 0, 300) };
        for (var k = 0; k <= 8; k++)
        {
            var a = MathF.PI / 2 * k / 8;
            outline.Add(new Vector3(720 - 300 * (1 - MathF.Cos(a)), 0, 300 - 900 * MathF.Sin(a)));
        }
        outline.Add(new Vector3(-100, 0, -1700));
        outline.Add(new Vector3(-720, 0, -1500));
        var bottom = outline.Select(v => m.P(0, v + new Vector3(0, Top - Case, 0))).ToArray();
        var top = outline.Select(v => m.P(0, v + new Vector3(0, Top, 0))).ToArray();
        var centre = new Vector3(0, Top - Case / 2, -600);
        for (var k = 0; k < outline.Count; k++) m.Out(new[] { bottom[k], bottom[(k + 1) % outline.Count], top[(k + 1) % outline.Count], top[k] }, Ebony, centre);
        var tc = m.P(0, new Vector3(0, Top, -600)); var bc = m.P(0, new Vector3(0, Top - Case, -600));
        for (var k = 0; k < outline.Count; k++) { m.Out(new[] { tc, top[k], top[(k + 1) % outline.Count] }, Ebony, centre); m.Out(new[] { bc, bottom[(k + 1) % outline.Count], bottom[k] }, Ebony, centre); }
        // the lid propped up on its stick, the keyboard (ivory, the black keys over it), the music desk, three legs, the pedals
        m.Strut(0, new Vector3(650, Top, -400), new Vector3(650, Top + 800, -600), 14, Ebony);
        var lid = new[] { m.P(0, new Vector3(-720, Top, -1500)), m.P(0, new Vector3(720, Top, -200)), m.P(0, new Vector3(700, Top + 820, -620)), m.P(0, new Vector3(-100, Top + 500, -1700)) };
        m.Both(lid, Ebony);
        m.Box(0, new Vector3(0, Top - 120, 400), new Vector3(1420, 60, 200), Ivory, Linen);
        for (var k = -10; k <= 10; k++) if (k % 7 != 0 && k % 7 != 3) m.Box(0, new Vector3(k * 64, Top - 60, 360), new Vector3(30, 30, 110), Ebony);
        m.Box(0, new Vector3(0, Top + 200, 150), new Vector3(800, 300, 30), Ebony, Ebony);
        foreach (var (x, z) in new[] { (-640f, 220f), (640f, 220f), (-100f, -1500f) }) m.Box(0, new Vector3(x, (Top - Case) / 2, z), new Vector3(110, Top - Case, 110), Ebony, Brass);
        m.Box(0, new Vector3(0, 80, 260), new Vector3(240, 40, 80), Brass);
        return m.ToBody(header);
    }

    // ---- a table for eight: a long top, its chairs, two candelabra
    private static Body BuildDiningTable(byte[] header)
    {
        var m = new CarMesh("a table for eight", new[] { -1 });
        m.Light = 0.8f;
        const float W = 1500, D = 520, H = 720;
        m.Box(0, new Vector3(0, H - 25, 0), new Vector3(2 * W, 50, 2 * D), Mahogany, Mahogany);
        m.Box(0, new Vector3(0, H - 22, 0), new Vector3(2 * W - 200, 8, 2 * D - 400), Linen, Linen);
        foreach (var (sx, sz) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) }) m.Box(0, new Vector3(sx * (W - 100), (H - 50) / 2, sz * (D - 100)), new Vector3(90, H - 50, 90), MahoganyDark);
        // the chairs: three a side, one at each end
        void Chair(Vector3 at, Vector3 back)
        {
            m.Box(0, at + new Vector3(0, 450, 0), new Vector3(420, 70, 420), Velvet, Velvet);
            m.Box(0, at + back * 190 + new Vector3(0, 820, 0), new Vector3(back.X != 0 ? 60 : 420, 700, back.Z != 0 ? 60 : 420), Mahogany, Mahogany);
            // (two legs' boxes, front and back, each the chair's width: the eight chairs' thirty-two legs took the table past 550 points)
            foreach (var s in new[] { -1, 1 }) m.Box(0, at + (back.X != 0 ? new Vector3(s * 170, 210, 0) : new Vector3(0, 210, s * 170)), back.X != 0 ? new Vector3(50, 420, 390) : new Vector3(390, 420, 50), MahoganyDark);
        }
        for (var k = -1; k <= 1; k++) { Chair(new Vector3(k * 900, 0, D + 300), Vector3.UnitZ); Chair(new Vector3(k * 900, 0, -D - 300), -Vector3.UnitZ); }
        Chair(new Vector3(W + 300, 0, 0), Vector3.UnitX); Chair(new Vector3(-W - 300, 0, 0), -Vector3.UnitX);
        // the candelabra, the places laid
        foreach (var x in new[] { -600f, 600f })
        {
            m.Box(0, new Vector3(x, H + 200, 0), new Vector3(40, 400, 40), Brass);
            m.Strut(0, new Vector3(x - 180, H + 380, 0), new Vector3(x + 180, H + 380, 0), 14, Brass);
            foreach (var cx in new[] { -180f, 0f, 180f }) { m.Box(0, new Vector3(x + cx, H + 470, 0), new Vector3(34, 160, 34), Ivory); m.Ball(0, new Vector3(x + cx, H + 580, 0), 24, GlowYellow); }
        }
        return m.ToBody(header);
    }

    // ---- a marble fireplace against its wall (its back on z 0), the fire in it, the mantel's clock and candlesticks, the mirror over it
    private static Body BuildFireplace(byte[] header)
    {
        var m = new CarMesh("a marble fireplace", new[] { -1 });
        m.Light = 0.8f;
        const float W = 900, H = 1150, D = 300;
        foreach (var s in new[] { -1f, 1f }) m.Box(0, new Vector3(s * (W - 140), H / 2, D / 2), new Vector3(280, H, D), Marble, Marble);
        m.Box(0, new Vector3(0, H - 120, D / 2), new Vector3(2 * W, 240, D), Marble, Marble);
        m.Box(0, new Vector3(0, H + 30, D / 2 + 30), new Vector3(2 * W + 160, 60, D + 80), MarbleVein, Marble);
        // the hearth's dark mouth, the fire, the logs
        Quad(m, new(-W + 280, 60, 40), new(W - 280, 60, 40), new(W - 280, H - 240, 40), new(-W + 280, H - 240, 40), Ebony, new(0, 400, 400), unlit: true);
        m.Box(0, new Vector3(0, 30, D / 2 + 100), new Vector3(2 * W, 60, D + 200), MarbleVein, Marble);
        m.Box(0, new Vector3(0, 120, 140), new Vector3(700, 90, 90), MahoganyDark);
        foreach (var (x, h, c) in new[] { (-150f, 300f, FireOrange), (0f, 420f, FireYellow), (160f, 280f, FireOrange) }) m.Ball(0, new Vector3(x, 160 + h / 2, 150), (int)(h / 2.4f), c);
        // the clock and two candlesticks on the mantel
        m.Box(0, new Vector3(0, H + 220, D / 2), new Vector3(300, 320, 160), Brass, Brass);
        m.Ball(0, new Vector3(0, H + 240, D / 2 + 90), 90, Ivory + 6);
        foreach (var s in new[] { -1f, 1f }) { m.Box(0, new Vector3(s * 650, H + 200, D / 2), new Vector3(50, 300, 50), Brass); m.Ball(0, new Vector3(s * 650, H + 380, D / 2), 26, GlowYellow); }
        // the overmantel mirror in its gilt frame
        m.Box(0, new Vector3(0, H + 1050, 20), new Vector3(1500, 1300, 40), Brass, Brass);
        Quad(m, new(-650, H + 470, 44), new(650, H + 470, 44), new(650, H + 1630, 44), new(-650, H + 1630, 44), Glass, new(0, H + 1050, 400));
        m.Ball(0, new Vector3(0, H + 1730, 40), 70, Brass);
        return m.ToBody(header);
    }

    // ---- a patterned rug (flat): a red field, a gold border, a medallion
    private static Body BuildRug(byte[] header)
    {
        var m = new CarMesh("a rug", new[] { -1 });
        m.Light = 0.75f;
        const float W = 1800, L = 1300, Y = 6;
        void Flat(float x0, float z0, float x1, float z1, float y, int c) => m.Up(new[] { m.P(0, new(x0, y, z0)), m.P(0, new(x1, y, z0)), m.P(0, new(x1, y, z1)), m.P(0, new(x0, y, z1)) }, c);
        Flat(-W, -L, W, L, Y, Brass);
        Flat(-W + 140, -L + 140, W - 140, L - 140, Y + 2, Velvet);
        Flat(-W + 260, -L + 260, W - 260, L - 260, Y + 3, VelvetDark);
        // the medallion, a lozenge of gold and blue
        var c0 = m.P(0, new(0, Y + 5, -L + 420)); var c1 = m.P(0, new(W - 520, Y + 5, 0)); var c2 = m.P(0, new(0, Y + 5, L - 420)); var c3 = m.P(0, new(-W + 520, Y + 5, 0));
        m.Up(new[] { c0, c1, c2, c3 }, Brass);
        var d0 = m.P(0, new(0, Y + 7, -L + 640)); var d1 = m.P(0, new(W - 820, Y + 7, 0)); var d2 = m.P(0, new(0, Y + 7, L - 640)); var d3 = m.P(0, new(-W + 820, Y + 7, 0));
        m.Up(new[] { d0, d1, d2, d3 }, 12 * 16 + 4);
        return m.ToBody(header);
    }

    // ---- a panelled partition with a doorway (its length along x, centred; the doorway at its +x end): the basic room's bathroom wall
    private static Body BuildPartition(byte[] header)
    {
        var m = new CarMesh("a partition", new[] { -1 });
        m.Light = 0.8f;
        const float L = 1000, H = 2300, T = 80, Gap = 800;
        // the wall from -L to L - Gap, the lintel over the doorway, the panels' mouldings both sides
        float x1 = L - Gap;
        m.Box(0, new Vector3((-L + x1) / 2, H / 2, 0), new Vector3(x1 + L, H, T), Teal, Ivory);
        m.Box(0, new Vector3((x1 + L) / 2, H - 150, 0), new Vector3(Gap, 300, T), Teal, Ivory);
        foreach (var s in new[] { -1f, 1f })
        {
            m.Box(0, new Vector3((-L + x1) / 2, 900, s * (T / 2 + 6)), new Vector3(x1 + L - 160, 20, 12), Ivory);
            m.Box(0, new Vector3((-L + x1) / 2, H - 40, s * (T / 2 + 6)), new Vector3(x1 + L, 50, 14), Brass);
        }
        foreach (var x in new[] { x1, L }) m.Box(0, new Vector3(x, H / 2, 0), new Vector3(60, H, T + 40), Mahogany, Mahogany);
        return m.ToBody(header);
    }

    // ---- an oil painting in a heavy gilt frame, on its wall (back on z 0): a landscape, sky, hills, a palace
    private static Body BuildPainting(byte[] header)
    {
        var m = new CarMesh("a painting", new[] { -1 });
        m.Light = 0.8f;
        const float W = 800, H = 600, Y = 700;
        m.Box(0, new Vector3(0, Y, 25), new Vector3(2 * W + 180, 2 * H + 180, 50), Brass, Brass);
        Quad(m, new(-W, Y, 52), new(W, Y, 52), new(W, Y + H, 52), new(-W, Y + H, 52), PaintSky, new(0, Y, 300), unlit: true);
        Quad(m, new(-W, Y - H, 52), new(W, Y - H, 52), new(W, Y, 52), new(-W, Y, 52), PaintGreen, new(0, Y, 300), unlit: true);
        var h0 = m.P(0, new(-W, Y - 60, 54)); var h1 = m.P(0, new(-200, Y + 220, 54)); var h2 = m.P(0, new(300, Y - 20, 54));
        m.In(new[] { h0, h1, h2 }, PaintGreen - 3, new(0, Y, 300), unlit: true);
        m.Box(0, new Vector3(380, Y + 80, 56), new Vector3(260, 200, 8), Ivory + 4);
        m.Box(0, new Vector3(380, Y + 230, 58), new Vector3(90, 120, 8), PaintDark);
        return m.ToBody(header);
    }
}
