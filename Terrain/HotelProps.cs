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
//   - a crystal chandelier (Chandelier: its foot at the ceiling's height, hanging down -- the corridor's), a table for eight laid with
//     candelabra (DiningTable), a panelled partition with a doorway (Partition) -- neither in the rooms now;
//   - the rooms' furniture -- the four-poster, a leather sofa, a coffee table, a grand piano, a fireplace, a rug, a painting, the Classic
//     Room's bath, toilet and basin -- remade in the game's own hotel style on 2026-11-23: HotelProps.Rooms.cs.
internal static partial class HotelProps
{
    // the four-poster's mattress's top over the floor (its teal cover's: HotelProps.Rooms BuildFourPoster)
    public const short BedTop = 630;
    public const int LiftFront = 229, LiftOpen = 230, ReceptionDesk = 231, CorridorDoors = 232, SuiteDoors = 233, FourPoster = 234, Chandelier = 235,
                     Sofa = 236, CoffeeTable = 237, GrandPiano = 238, DiningTable = 239, Fireplace = 240, HotelRug = 241, Partition = 242, Painting = 243;
    // (each of the corridor's doors a body of its own -- the five in one passed the engine's 550 points: RoomDoor + its number's last digit - 1)
    public const int RoomDoor = 244;
    // (the lift car's own: its doors seen from inside, shut and open; its back wall's lacquer, mirror and rail -- 2026-11-23, the user: "Let's add
    // an actual scene for the lift that Twinsen can walk inside": OtringalHotel.LiftCar)
    public const int LiftInside = 249, LiftInsideOpen = 250, LiftCarBack = 251;

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

    public static List<string> Install(string gameDirectory) => Citadel.CitadelProps.InstallMore(gameDirectory, "the Imperial Hotel", Bodies);

    // (every body the hotel adds, its generic and name: Install's, and a preview's -- ScriptRoundTrip hotelpreview)
    internal static readonly (int Generic, string Name, Func<byte[], Body> Build)[] Bodies =
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
        (Sofa, "a leather sofa", BuildSofa),
        (CoffeeTable, "a coffee table", BuildCoffeeTable),
        (GrandPiano, "a grand piano", BuildGrandPiano),
        (DiningTable, "a table for eight", BuildDiningTable),
        (Fireplace, "a marble fireplace", BuildFireplace),
        (HotelRug, "a hotel rug", BuildRug),
        (Partition, "a panelled partition", BuildPartition),
        (Painting, "an oil painting", BuildPainting),
        (LiftInside, "the lift car's doors, shut", h => BuildLiftInside(h, false)),
        (LiftInsideOpen, "the lift car's doors, open", h => BuildLiftInside(h, true)),
        (LiftCarBack, "the lift car's back wall", BuildLiftCarBack),
        (HotelBath, "a bath", BuildBath),
        (HotelToilet, "a toilet", h => BuildToilet(h, false)),
        (GiltToilet, "a toilet, its fittings gilt", h => BuildToilet(h, true)),
        (SideChair, "a side chair", BuildSideChair),
        (HotelBasin, "a basin", BuildBasin),
    };

    // The boxes the scene stands them in: the rug walked over (OtringalHotel gives it the walk-over armour too); the wall pieces only as
    // deep as they stand out from their wall
    public static short[]? Box(int generic) => generic switch
    {
        // (the lift stands on a wall along z facing +x, and the engine never turns an actor's box with its beta: the box in the room's own
        // axes -- x out from the wall, z along it. Its x along the wall before 2026-11-23 reached 900 out into the room and the doors drew
        // over Twinsen walking by them, the user: "need to be flush in the wall and not drawn over Twinsen")
        LiftFront or LiftOpen or LiftInside or LiftInsideOpen => new short[] { 0, 0, -1260, 130, 3100, 1260 },
        LiftCarBack => new short[] { -1000, 0, 0, 1000, 2600, 60 },
        // (the counter only: the board of keys behind it is drawn, the clerk stands between them -- its whole extent's box pushed him out)
        ReceptionDesk => new short[] { -1320, 0, -440, 1320, 1150, 440 },
        CorridorDoors => new short[] { 0, 0, 0, (short)(DoorPitch * (Rooms - 1)), 2200, 120 },
        >= RoomDoor and < RoomDoor + Rooms => new short[] { -800, 0, 0, 800, 2600, 120 },
        SuiteDoors => new short[] { -1300, 0, 0, 1300, 3000, 160 },
        HotelRug => new short[] { -10, 0, -10, 10, 4, 10 },
        Chandelier => new short[] { -10, -10, -10, 10, 10, 10 },
        Painting => new short[] { -960, -60, 0, 960, 1560, 130 },
        // (the rooms' furniture, 2026-11-23: each its own footprint -- without one the engine gives a body a square box about its origin,
        // half the average of its width and depth (FICHE.CPP InitBodyZV): the piano's reached 1,080 out every way, past its keyboard's end
        // into the way to the suite's bedroom, and Twinsen couldn't pass. In the room's axes, as each is placed: the fireplace at beta 1024
        // on its west wall, the Classic Room's toilet at 1024 on its bathroom's west wall; the gilt toilet, the bath, the basin, the piano,
        // the four-poster and the coffee table at 0; the sofa and the chair the same at 0 and 2048)
        GrandPiano => new short[] { -720, 0, -1800, 720, 1960, 1100 },
        // (the four-poster's to its mattress's top, BedTop: Twinsen laid on it to sleep is carried there -- boxed to its canopy he was put
        // inside its box, pushed out of it and slept on the floor, the user, 2026-11-24)
        FourPoster => new short[] { -960, 0, -1100, 960, BedTop, 1100 },
        Sofa => new short[] { -990, 0, -470, 990, 960, 470 },
        CoffeeTable => new short[] { -620, 0, -360, 620, 860, 360 },
        Fireplace => new short[] { 0, 0, -990, 600, 2760, 990 },
        HotelBath => new short[] { -330, 0, -650, 330, 800, 650 },
        HotelBasin => new short[] { -380, 0, 0, 380, 1000, 580 },
        HotelToilet => new short[] { 0, 0, -260, 715, 1030, 260 },
        GiltToilet => new short[] { -260, 0, 0, 260, 1030, 715 },
        SideChair => new short[] { -230, 0, -230, 230, 1000, 230 },
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
        // (flush in its wall, 2026-11-23: the surround a relief 50 deep, the doors set back in it, nothing standing out at the foot)
        foreach (var s in new[] { -1f, 1f })
            m.Box(0, new Vector3(s * (half + jamb / 2), LiftHigh / 2 + 100, 25), new Vector3(jamb, LiftHigh + 200, 50), Marble, Marble);
        m.Box(0, new Vector3(0, LiftHigh + 250, 27), new Vector3(LiftWide + 2 * jamb + 80, 300, 54), Marble, Marble);
        m.Box(0, new Vector3(0, LiftHigh + 410, 32), new Vector3(LiftWide + 2 * jamb + 160, 40, 64), BrassDark, Brass);
        m.Box(0, new Vector3(0, 8, 14), new Vector3(LiftWide + 2 * jamb, 16, 28), MarbleVein, Marble);
        // the floor dial over the lintel: a brass half disc, its numerals' ticks, its arrow (at the ground floor shut, between the floors open)
        var dialC = new Vector3(0, LiftHigh + 430, 60);
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
        m.Box(0, new Vector3(half + jamb + 170, 1150, 10), new Vector3(120, 260, 20), Brass, Brass);
        m.Ball(0, new Vector3(half + jamb + 170, 1210, 24), 18, GlowYellow);
        m.Ball(0, new Vector3(half + jamb + 170, 1090, 24), 18, Ivory);
        // a lantern each side
        foreach (var s in new[] { -1f, 1f })
        {
            var x = s * (half + jamb + 330);
            m.Box(0, new Vector3(x, 1800, 20), new Vector3(60, 60, 40), BrassDark);
            m.Box(0, new Vector3(x, 1930, 70), new Vector3(140, 220, 120), Glass, BrassDark);
            m.Ball(0, new Vector3(x, 1930, 70), 46, GlowYellow);
        }
        if (!open)
        {
            // the doors: two brass leaves meeting in the middle, a sunburst in relief over both, the line between them
            foreach (var s in new[] { -1f, 1f })
            {
                m.Box(0, new Vector3(s * half / 2, LiftHigh / 2, 12), new Vector3(half - 10, LiftHigh, 16), BrassDark, Brass);
                // (the leaf's panels: a raised frame)
                m.Box(0, new Vector3(s * half / 2, LiftHigh * 0.32f, 24), new Vector3(half - 160, 40, 8), Brass);
                m.Box(0, new Vector3(s * half / 2, LiftHigh * 0.62f, 24), new Vector3(half - 160, 40, 8), Brass);
            }
            var sun = new Vector3(0, LiftHigh * 0.8f, 26);
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
            m.Box(0, new Vector3(0, LiftHigh / 2, 4), new Vector3(LiftWide - 20, LiftHigh, 8), Lacquer, Lacquer);
            Quad(m, new(-half + 160, 700, 10), new(half - 160, 700, 10), new(half - 160, 1900, 10), new(-half + 160, 1900, 10), Glass, new(0, 1300, 300));
            m.Box(0, new Vector3(0, 900, 16), new Vector3(LiftWide - 120, 24, 16), Brass);
            m.Box(0, new Vector3(0, 4, 20), new Vector3(LiftWide - 20, 8, 40), VelvetDark);
            // (the leaves' edges, just showing at the jambs)
            foreach (var s in new[] { -1f, 1f }) m.Box(0, new Vector3(s * (half - 30), LiftHigh / 2, 18), new Vector3(60, LiftHigh, 24), BrassDark, Brass);
        }
        return m.ToBody(header);
    }

    // ---- the lift car's doors from inside (its west wall, facing +x as the floors' lifts are): two brushed brass leaves meeting in the
    // middle, a red lacquer surround, the button panel beside them (the two floors' buttons, the one lit), the floor dial over them; open,
    // the leaves slid into the surround and the landing beyond a dim gold
    private static Body BuildLiftInside(byte[] header, bool open)
    {
        var m = new CarMesh(open ? "the lift car's doors, open" : "the lift car's doors, shut", new[] { -1 });
        m.Light = 0.8f;
        float half = LiftWide / 2, side = 260;
        foreach (var s in new[] { -1f, 1f })
            m.Box(0, new Vector3(s * (half + side / 2), LiftHigh / 2 + 150, 15), new Vector3(side, LiftHigh + 300, 30), Lacquer, Lacquer);
        m.Box(0, new Vector3(0, LiftHigh + 200, 15), new Vector3(LiftWide + 2 * side, 200, 30), Lacquer, Lacquer);
        // (a brass bead round the opening)
        m.Box(0, new Vector3(0, LiftHigh + 20, 32), new Vector3(LiftWide + 40, 40, 8), Brass);
        foreach (var s in new[] { -1f, 1f }) m.Box(0, new Vector3(s * (half + 20), LiftHigh / 2, 32), new Vector3(40, LiftHigh, 8), Brass);
        // the floor dial, small, over the doors
        var dialC = new Vector3(0, LiftHigh + 200, 34);
        const int N = 8; const float R = 120;
        var rim = Enumerable.Range(0, N + 1).Select(k => m.P(0, dialC + new Vector3(-R * MathF.Cos(MathF.PI * k / N), R * MathF.Sin(MathF.PI * k / N) - 60, 0))).ToArray();
        var hub = m.P(0, dialC + new Vector3(0, -60, 0));
        for (var k = 0; k < N; k++) m.In(new[] { hub, rim[k], rim[k + 1] }, Brass, dialC + new Vector3(0, 0, 200));
        m.Strut(0, dialC + new Vector3(0, -60, 6), dialC + new Vector3(-80, 10, 6), 10, Ebony);
        // the button panel, right of the doors: a brass plate, the ground floor's and the first's buttons, an alarm bell's red one
        var px = half + side / 2;
        m.Box(0, new Vector3(px, 1150, 36), new Vector3(150, 420, 12), Brass, Brass);
        m.Ball(0, new Vector3(px, 1270, 46), 26, GlowYellow);
        m.Ball(0, new Vector3(px, 1150, 46), 26, Ivory);
        m.Ball(0, new Vector3(px, 1010, 46), 22, FireOrange);
        if (!open)
        {
            // the leaves: brushed brass, the seam between them, a kick plate at their foot
            foreach (var s in new[] { -1f, 1f })
            {
                m.Box(0, new Vector3(s * half / 2, LiftHigh / 2, 10), new Vector3(half - 8, LiftHigh, 12), Brass, BrassDark);
                m.Box(0, new Vector3(s * half / 2, 160, 18), new Vector3(half - 60, 300, 6), BrassDark);
                m.Box(0, new Vector3(s * half / 2, LiftHigh * 0.55f, 18), new Vector3(half - 160, 24, 6), BrassDark);
            }
            m.Box(0, new Vector3(0, LiftHigh / 2, 18), new Vector3(10, LiftHigh, 6), Ebony);
        }
        else
        {
            // the landing beyond: its dim gold light, the leaves' edges showing at the jambs
            m.Box(0, new Vector3(0, LiftHigh / 2, 2), new Vector3(LiftWide - 20, LiftHigh, 4), BrassDark, BrassDark);
            foreach (var s in new[] { -1f, 1f }) m.Box(0, new Vector3(s * (half - 30), LiftHigh / 2, 12), new Vector3(60, LiftHigh, 16), Brass, BrassDark);
        }
        return m.ToBody(header);
    }

    // ---- the lift car's back wall (on a wall along x, facing +z): red lacquer panelling, a tall mirror in a brass frame, the brass hand rail
    // at waist height, a little velvet bench under it
    private static Body BuildLiftCarBack(byte[] header)
    {
        var m = new CarMesh("the lift car's back wall", new[] { -1 });
        m.Light = 0.8f;
        const float W = 980, H = 2500;
        m.Box(0, new Vector3(0, H / 2, 10), new Vector3(2 * W, H, 20), Lacquer, Lacquer);
        foreach (var x in new[] { -W + 40, W - 40 }) m.Box(0, new Vector3(x, H / 2, 24), new Vector3(30, H, 10), Brass);
        m.Box(0, new Vector3(0, H - 20, 24), new Vector3(2 * W, 40, 10), Brass);
        // the mirror: brass frame, the glass
        m.Box(0, new Vector3(0, 1550, 26), new Vector3(1120, 1060, 12), Brass, BrassDark);
        Quad(m, new(-500, 1060, 34), new(500, 1060, 34), new(500, 2040, 34), new(-500, 2040, 34), Glass, new(0, 1550, 300));
        // the hand rail on its brackets
        m.Box(0, new Vector3(0, 900, 70), new Vector3(2 * W - 120, 36, 36), Brass, BrassDark);
        foreach (var x in new[] { -W + 200, 0f, W - 200 }) m.Box(0, new Vector3(x, 900, 44), new Vector3(24, 24, 50), BrassDark);
        // the bench: velvet on a brass frame
        m.Box(0, new Vector3(0, 380, 140), new Vector3(1000, 70, 260), Velvet, VelvetDark);
        foreach (var x in new[] { -460f, 460f }) m.Box(0, new Vector3(x, 175, 140), new Vector3(30, 350, 240), BrassDark);
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
}
