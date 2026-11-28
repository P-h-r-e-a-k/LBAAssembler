using System.IO;
using System.Numerics;
using LBAAssembler.Characters;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The Temple of Bú's remodel (the user, 2026-11-27: "Let's implement all 10 of these temple of bu suggestions" -- the Temple as the Cup's
// departure lounge for the Emerald Moon, staffed by friendly skeletons; the story's beats: RaceTrackStory.TempleRemodel). What it is made
// of, modelled round and bevelled, every face lit at its own angle (the user, 2026-11-23: "make sure you use lots of normals"):
//   - the props, on an entity of their own (CitadelProps.InstallOwn): Bú's statue in a racing helmet and a chequered scarf, holding a
//     steering wheel, with his luck shrine's bowl (empty, and with kashes in it); a fire bowl in two flickers; a cobweb for a corner, its
//     spider on a thread; the black car's tyre marks; the departure board ("EMERALD MOON / NOW BOARDING", its second line lit and dimmed
//     in turn); a row of lane lights; an Esmer sign pointing the way; the souvenir stall (its awning, the Cup's pennants, foam fingers);
//     the ribcage xylophone;
//   - the temple's skeletons: the game's rabbibunny skeleton (BODY.HQR 35, its entity's own) dressed for their jobs, as more of that
//     entity's bodies -- the skittles (small), the traffic warden (a hi-vis tabard, a lollipop sign: SLOW), the tour guide (a cap, a
//     flag on a pole), the tourists (sun hats, a camera), the witness (holding his own skull), the xylophonist (two mallets), the
//     stranded miner (a miner's helmet, its lamp), the stallholder (a cap) -- and three animations of its own: sitting, playing the
//     xylophone, waving the lollipop sign up. Never its swipe (generic 150): nobody attacks Twinsen.
//   - the xylophone's tune: a skeleton's waltz on bones, at SAMPLES.HQR's end (the temple's music now: the scene's own is stopped,
//     RACEMOD.CPP quiet_scene=).
internal static class TempleRemodelProps
{
    // ---- the props' bodies (their own entity's numbers)
    public const int BuStatue = 1, BuStatueCoins = 2, FireBowl = 3, FireBowl2 = 4, Cobweb = 5, TyreMarks = 6, Board = 7, BoardDim = 8,
        LaneLights = 9, EsmerSign = 10, Stall = 11, Xylophone = 12, LaneLightsShort = 13;
    private const string What = "the Temple of Bú's remodel";

    // ---- the skeletons: the rabbibunny skeleton's entity's new bodies, and its new animations (generic numbers)
    public const int SkeletonBody = 35;
    public const int Skittle = 60, Warden = 61, Guide = 62, TouristA = 63, TouristB = 64, TouristC = 65, Witness = 66, Musician = 67, Miner = 68, Vendor = 69;
    // (the skeleton entity's generic animations used: standing, walking, getting up, falling apart -- not 129, a run, nor 5, 6, 22, hops back,
    // nor 150, its swipe)
    public const int AnimStand = 0, AnimWalk = 1, AnimGetUp = 147, AnimHeap = 151;
    public const int AnimSit = 690, AnimPlay = 691, AnimSignUp = 692, AnimHold = 693;
    // the skittles' size, of the game's skeleton
    public const float SkittleScale = 0.55f;
    // the flying rat's bigger, shaded body (its entity's)
    public const int BigRat = 60;
    // the game's skeleton's bones: ribcage, head, the +x arm (upper, fore, hand), the -x arm
    private const int Ribs = 3, Head = 4, RUpper = 5, RFore = 6, RHand = 7, LUpper = 8, LFore = 9, LHand = 10;

    // ---- colours: low on their ramps (the light runs them up), the unlit ones as they are
    private static readonly Finish StoneF = new(11 * 16 + 4, 0.8f), StoneDarkF = new(11 * 16 + 2, 0.8f), HelmetF = new(4 * 16 + 4, 0.7f),
        HelmetWhiteF = new(3 * 16 + 9, 0.5f), VisorF = new(3 * 16 + 1, 0.3f), BronzeF = new(6 * 16 + 3, 0.8f), GoldF = new(6 * 16 + 7, 0.6f),
        CoalF = new(4 * 16 + 1, 0.5f), WoodF = new(1 * 16 + 5, 0.8f), WoodLightF = new(1 * 16 + 7, 0.7f), WoodDarkF = new(1 * 16 + 3, 0.8f),
        BoneF = new(3 * 16 + 7, 0.6f), BoardF = new(3 * 16 + 3, 0.7f), HousingF = new(11 * 16 + 3, 0.7f), PanelF = new(12 * 16 + 3, 0.6f),
        PostF = new(11 * 16 + 4, 0.7f), StrawF = new(6 * 16 + 5, 0.7f), HiVisF = new(6 * 16 + 8, 0.6f), PoleF = new(3 * 16 + 10, 0.5f),
        DiscF = new(3 * 16 + 11, 0.35f), RimF = new(4 * 16 + 5, 0.6f), MinerF = new(6 * 16 + 6, 0.6f), CameraF = new(3 * 16 + 1, 0.4f);
    private const int ScarfBlack = 3 * 16 + 1, ScarfWhite = 3 * 16 + 11, FlameOuter = 5 * 16 + 8, FlameInner = 6 * 16 + 12, FlameTip = 4 * 16 + 8,
        WebGrey = 3 * 16 + 12, Spider = 3 * 16 + 0, Tyre = 3 * 16 + 1, BoardFace = 3 * 16 + 0, Amber = 6 * 16 + 13, AmberDim = 6 * 16 + 4,
        Lens = 10 * 16 + 14, Glyph = 10 * 16 + 13, Arrow = 6 * 16 + 13, AwningRed = 4 * 16 + 5, AwningWhite = 3 * 16 + 10, Ink = 3 * 16 + 0,
        Stripe = 3 * 16 + 13, LampLens = 6 * 16 + 14;
    private static readonly int[] PennantColours = { 4 * 16 + 6, 12 * 16 + 6, 6 * 16 + 9 };

    // ---- the props, onto their own entity (once a build; asked again, only the entity table is read)
    internal static IEnumerable<(int Generic, string Name, Func<byte[], Body> Build, short[]? Box)> Props => new (int, string, Func<byte[], Body>, short[]?)[]
    {
        (BuStatue, "Bú, patron of racers, his luck shrine's bowl empty", h => BuildBu(h, false), BuBox),
        (BuStatueCoins, "Bú, patron of racers, kashes in his bowl", h => BuildBu(h, true), BuBox),
        (FireBowl, "a fire bowl", h => BuildFireBowl(h, 0), FireBowlBox),
        (FireBowl2, "a fire bowl, flickering", h => BuildFireBowl(h, 1), FireBowlBox),
        (Cobweb, "a cobweb in a corner, its spider", BuildCobweb, null),
        (TyreMarks, "the black car's tyre marks", BuildTyreMarks, null),
        (Board, "the departure board: Emerald Moon, now boarding", h => BuildBoard(h, true), null),
        (BoardDim, "the departure board, its second line dimmed", h => BuildBoard(h, false), null),
        (LaneLights, "a row of lane lights", h => BuildLaneLights(h, 8), null),
        (LaneLightsShort, "a short row of lane lights", h => BuildLaneLights(h, 4), null),
        (EsmerSign, "an Esmer sign", BuildEsmerSign, null),
        (Stall, "the souvenir stall", BuildStall, StallBox),
        (Xylophone, "the ribcage xylophone", BuildXylophone, null),
    };
    private static readonly short[] FireBowlBox = { -180, 0, -180, 180, 800, 180 };
    // (Bú: his plinth and the shrine's pedestal before him, facing +z -- placed at beta 0)
    private static readonly short[] BuBox = { -520, 0, -520, 520, 2300, 950 };
    private static readonly short[] StallBox = { -950, 0, -420, 950, 1000, 420 };

    public static List<string> Install(string gameDirectory) => new() { Citadel.CitadelProps.InstallOwn(gameDirectory, What, Props).Log };
    public static int Entity(string gameDirectory) => Citadel.CitadelProps.InstallOwn(gameDirectory, What, Props).Entity;

    // ---- the text: a 5 x 5 pixel font of the capitals the boards need, each glyph's lit pixels merged into as few rectangles as they go
    private static readonly Dictionary<char, string[]> Font = new()
    {
        ['A'] = new[] { ".###.", "#...#", "#####", "#...#", "#...#" },
        ['B'] = new[] { "####.", "#...#", "####.", "#...#", "####." },
        ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "####." },
        ['E'] = new[] { "#####", "#....", "####.", "#....", "#####" },
        ['G'] = new[] { ".####", "#....", "#..##", "#...#", ".###." },
        ['I'] = new[] { "###", ".#.", ".#.", ".#.", "###" },
        ['L'] = new[] { "#....", "#....", "#....", "#....", "#####" },
        ['M'] = new[] { "#...#", "##.##", "#.#.#", "#...#", "#...#" },
        ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#" },
        ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", ".###." },
        ['R'] = new[] { "####.", "#...#", "####.", "#..#.", "#...#" },
        ['S'] = new[] { ".####", "#....", ".###.", "....#", "####." },
        ['W'] = new[] { "#...#", "#...#", "#.#.#", "##.##", "#...#" },
        [' '] = new[] { "..", "..", "..", "..", ".." },
    };
    private const int FontRows = 5;

    // a text's lit pixels as rectangles (column, row of its top left; column, row of its bottom right), and its width in columns
    private static (List<(int C0, int R0, int C1, int R1)> Rects, int Columns) TextRects(string text)
    {
        var columns = text.Sum(c => Font[c][0].Length + 1) - 1;
        var lit = new bool[columns, FontRows];
        var x = 0;
        foreach (var ch in text)
        {
            var g = Font[ch];
            for (var r = 0; r < FontRows; r++) for (var c = 0; c < g[r].Length; c++) lit[x + c, r] = g[r][c] == '#';
            x += g[0].Length + 1;
        }
        var used = new bool[columns, FontRows];
        var rects = new List<(int, int, int, int)>();
        for (var r = 0; r < FontRows; r++)
            for (var c = 0; c < columns; c++)
            {
                if (!lit[c, r] || used[c, r]) continue;
                var c1 = c;
                while (c1 + 1 < columns && lit[c1 + 1, r] && !used[c1 + 1, r]) c1++;
                var r1 = r;
                bool RowFits(int rr) { for (var k = c; k <= c1; k++) if (!lit[k, rr] || used[k, rr]) return false; return true; }
                while (r1 + 1 < FontRows && RowFits(r1 + 1)) r1++;
                for (var rr = r; rr <= r1; rr++) for (var k = c; k <= c1; k++) used[k, rr] = true;
                rects.Add((c, r, c1, r1));
            }
        return (rects, columns);
    }

    // a text's quads, unlit, facing `towards`: `place` takes (column, row) -- 0, 0 its top left -- into the body
    private static void Text(CarMesh m, string text, Func<float, float, Vector3> place, int colour, Vector3 towards)
    {
        foreach (var (c0, r0, c1, r1) in TextRects(text).Rects)
            m.In(new[] { m.P(0, place(c0, r0)), m.P(0, place(c1 + 1, r0)), m.P(0, place(c1 + 1, r1 + 1)), m.P(0, place(c0, r1 + 1)) }, colour, towards, unlit: true);
    }

    // a flat quad of new points, unlit, facing `towards`
    private static void Flat(CarMesh m, int colour, Vector3 towards, params Vector3[] at) => m.In(at.Select(v => m.P(0, v)).ToArray(), colour, towards, unlit: true);

    // a ring of tube round a circle (centre, its plane's u and v, radius R, the tube's radius r): its points shared, lit smoothly
    private static void Torus(CarMesh m, Vector3 centre, Vector3 u, Vector3 v, float R, float r, Finish f, int n = 12, int k = 5)
    {
        m.Light = f.Light;
        var axis = Vector3.Normalize(Vector3.Cross(u, v));
        var rings = Enumerable.Range(0, n).Select(i =>
        {
            var a = i * MathF.Tau / n;
            var radial = u * MathF.Cos(a) + v * MathF.Sin(a);
            var c = centre + radial * R;
            return m.Loop(0, c, radial * r, axis * r, k);
        }).ToArray();
        for (var i = 0; i < n; i++)
        {
            var a = (i + 0.5f) * MathF.Tau / n;
            m.Skin(rings[i], rings[(i + 1) % n], _ => f.Colour, centre + (u * MathF.Cos(a) + v * MathF.Sin(a)) * R);
        }
    }

    // an unlit cone of `n` sides (a flame's tongue): its foot round `foot` (radius r), its tip at `tip`
    private static void Tongue(CarMesh m, Vector3 foot, float r, Vector3 tip, int colour, int n = 5)
    {
        var ring = m.Loop(0, foot, new Vector3(r, 0, 0), new Vector3(0, 0, r), n);
        m.Cap(ring, m.P(0, tip), colour, foot + (tip - foot) * 0.3f, unlit: true);
        m.Cap(ring, m.P(0, foot - new Vector3(0, r * 0.4f, 0)), colour, foot + (tip - foot) * 0.3f, unlit: true);
    }

    // ---- Bú, patron of racers: a robed rabbibunny figure on a stepped plinth, his long ears up through a red racing helmet (a white stripe
    // over its top, a dark visor), a chequered scarf at his neck, its end blowing back, a steering wheel in his hands; before him, his luck
    // shrine: a bronze bowl on a stone pedestal (kashes in it once Twinsen has left some). Faces +z. About 2,300 high to his ears' tips.
    public const float BowlZ = 760;
    private static Body BuildBu(byte[] header, bool coins)
    {
        var m = new CarMesh(coins ? "Bú, kashes in his bowl" : "Bú, patron of racers", new[] { -1 }) { Limit = 1100 };
        // the plinth
        HotelKit.BevelBox(m, new(0, 140, 0), new(1000, 280, 1000), 40, StoneDarkF);
        HotelKit.BevelBox(m, new(0, 320, 0), new(820, 80, 820), 24, StoneF);
        // the robe, flared at its hem, to his shoulders
        HotelKit.Turned(m, new(0, 360, 0), new (float, float)[] { (0, 0), (320, 0), (300, 80), (250, 400), (215, 700), (190, 860), (130, 930), (0, 930) }, 10, StoneF);
        // his arms forward to the wheel, his hands on it
        foreach (var s in new[] { -1f, 1f })
        {
            HotelKit.Rod(m, new(s * 185, 1210, 10), new(s * 120, 1040, 230), 55, StoneF);
            HotelKit.Orb(m, new(s * 115, 1045, 245), 52, StoneF, 8, 4);
        }
        // the steering wheel: a rim of tube, three spokes to its hub
        var wheelC = new Vector3(0, 1070, 300);
        var wu = new Vector3(1, 0, 0);
        var wv = Vector3.Normalize(new Vector3(0, 0.94f, -0.34f));
        Torus(m, wheelC, wu, wv, 150, 18, VisorF, 12, 5);
        m.Light = VisorF.Light;
        foreach (var a in new[] { 90f, 210f, 330f })
        {
            var r = a * MathF.PI / 180;
            m.Strut(0, wheelC, wheelC + (wu * MathF.Cos(r) + wv * MathF.Sin(r)) * 140, 16, VisorF.Colour);
        }
        HotelKit.Orb(m, wheelC + new Vector3(0, 0, 8), 34, HelmetF, 8, 3);
        // his head, his muzzle
        HotelKit.Orb(m, new(0, 1460, 30), 160, StoneF, 10, 5, 1.05f);
        HotelKit.Orb(m, new(0, 1395, 175), 72, StoneF, 8, 4, 0.8f);
        // his ears, long and leaning back a little, up through the helmet
        foreach (var s in new[] { -1f, 1f })
        {
            var foot = new Vector3(s * 70, 1540, -10);
            var tip = new Vector3(s * 150, 2230, -90);
            var axis = Vector3.Normalize(tip - foot);
            var u = Vector3.Normalize(Vector3.Cross(axis, Vector3.UnitZ));
            var v = Vector3.Cross(axis, u);
            HotelKit.Lathe(m, foot, axis, u * 1.0f, v * 0.6f, new (float, float)[] { (0, 0), (52, 0), (62, 160), (52, 420), (30, 600), (0, (tip - foot).Length()) }, 8, _ => StoneF);
        }
        // the racing helmet: a shell over his head, sixteen panels round -- the stripe's over the top, front to back, white; the band at the
        // front under the brow the visor, dark
        {
            var c = new Vector3(0, 1470, 30);
            var rad = new Vector3(196, 192, 206);
            const int N = 16;
            var lats = new[] { 80f, 58f, 36f, 14f, -8f, -24f };
            Vector3 On(float lat, float a) => c + new Vector3(rad.X * MathF.Cos(lat) * MathF.Sin(a), rad.Y * MathF.Sin(lat), rad.Z * MathF.Cos(lat) * MathF.Cos(a));
            m.Light = HelmetF.Light;
            var crown = m.P(0, c + new Vector3(0, rad.Y, 0));
            var rings = lats.Select(l => Enumerable.Range(0, N).Select(i => m.P(0, On(l * MathF.PI / 180, (i - 0.5f) * MathF.Tau / N))).ToArray()).ToArray();
            bool StripeAt(int i) => i is 0 or 8;
            bool VisorAt(int i) => i is 14 or 15 or 0 or 1 or 2;
            for (var i = 0; i < N; i++)
            {
                var n = (i + 1) % N;
                m.Out(new[] { crown, rings[0][i], rings[0][n] }, StripeAt(i) ? HelmetWhiteF.Colour : HelmetF.Colour, c);
                for (var k = 0; k + 1 < lats.Length; k++)
                {
                    var colour = k == 3 && VisorAt(i) ? VisorF.Colour : k == 4 && VisorAt(i) ? HelmetWhiteF.Colour : k < 3 && StripeAt(i) ? HelmetWhiteF.Colour : HelmetF.Colour;
                    if (k == 4 && VisorAt(i)) continue;   // (open under the visor: his muzzle)
                    m.Out(new[] { rings[k][i], rings[k][n], rings[k + 1][n], rings[k + 1][i] }, colour, c);
                }
            }
        }
        // the chequered scarf: a collar of squares round his neck, black and white, its end blowing back over his left shoulder
        {
            const int N = 12;
            m.Light = 0.5f;
            var lo = m.Loop(0, new(0, 1250, 20), new(0, 0, 190), new(182, 0, 0), N);
            var mid = m.Loop(0, new(0, 1300, 20), new(0, 0, 178), new(170, 0, 0), N);
            var hi = m.Loop(0, new(0, 1350, 18), new(0, 0, 160), new(152, 0, 0), N);
            for (var i = 0; i < N; i++)
            {
                var n = (i + 1) % N;
                m.Out(new[] { lo[i], lo[n], mid[n], mid[i] }, i % 2 == 0 ? ScarfBlack : ScarfWhite, new(0, 1300, 20));
                m.Out(new[] { mid[i], mid[n], hi[n], hi[i] }, i % 2 == 1 ? ScarfBlack : ScarfWhite, new(0, 1300, 20));
            }
            // its end: two squares wide, four long, from behind his left shoulder down and back, rippling
            var tail = new int[5, 3];
            for (var r = 0; r <= 4; r++)
                for (var col = 0; col <= 2; col++)
                {
                    var t = r / 4f;
                    tail[r, col] = m.P(0, new Vector3(-90 - 60 * t + col * 70 - 70, 1290 - 330 * t, -160 - 260 * t + 40 * MathF.Sin(t * 6 + col)));
                }
            for (var r = 0; r < 4; r++)
                for (var col = 0; col < 2; col++)
                    m.Both(new[] { tail[r, col], tail[r, col + 1], tail[r + 1, col + 1], tail[r + 1, col] }, (r + col) % 2 == 0 ? ScarfBlack : ScarfWhite);
        }
        // the shrine: a stone pedestal before him, a bronze bowl on it -- and, once left, five kashes in it
        HotelKit.Turned(m, new(0, 0, BowlZ), new (float, float)[] { (0, 0), (170, 0), (160, 60), (85, 110), (70, 520), (125, 560), (0, 560) }, 8, StoneDarkF);
        HotelKit.Turned(m, new(0, 560, BowlZ), new (float, float)[] { (0, 0), (70, 0), (180, 60), (225, 150), (205, 155), (0, 60) }, 10, BronzeF);
        if (coins)
            foreach (var (dx, dz, dy) in new[] { (-60f, -30f, 75f), (50f, -50f, 78f), (10f, 55f, 80f), (-40f, 50f, 92f), (30f, 5f, 98f) })
                HotelKit.Turned(m, new(dx, 560 + dy, BowlZ + dz), new (float, float)[] { (0, 0), (46, 0), (46, 14), (0, 14) }, 8, GoldF);
        return m.ToBody(header);
    }

    // ---- a fire bowl: a turned bronze stand and bowl, glowing coals, flames (the second frame the flames' other flicker). Its foot at 0.
    private static Body BuildFireBowl(byte[] header, int frame)
    {
        var m = new CarMesh("a fire bowl", new[] { -1 }) { Limit = 1100 };
        HotelKit.Turned(m, Vector3.Zero, new (float, float)[]
        {
            (0, 0), (170, 0), (160, 40), (70, 90), (45, 200), (45, 540), (70, 580), (90, 600), (200, 690), (255, 790), (238, 805), (0, 730),
        }, 10, BronzeF);
        HotelKit.Orb(m, new(0, 760, 0), 190, CoalF, 10, 3, 0.25f);
        var tongues = frame == 0
            ? new (float X, float Z, float R, float H, float Lean)[] { (0, 0, 120, 520, 0), (110, 40, 75, 330, 40), (-100, 60, 70, 290, -30), (40, -110, 70, 360, 20), (-60, -80, 65, 270, -20) }
            : new (float X, float Z, float R, float H, float Lean)[] { (10, 10, 115, 450, -30), (100, 30, 80, 390, 60), (-110, 50, 70, 250, 10), (50, -100, 75, 300, -40), (-70, -70, 70, 340, 30) };
        foreach (var (x, z, r, h, lean) in tongues)
        {
            Tongue(m, new(x, 780, z), r, new(x + lean, 780 + h, z + lean * 0.5f), FlameOuter);
            Tongue(m, new(x, 790, z + 4), r * 0.55f, new(x + lean * 0.8f, 790 + h * 0.62f, z + lean * 0.4f + 4), FlameInner, 4);
        }
        Tongue(m, new(0, 785, 0), 50, new(frame == 0 ? 30 : -30, 785 + (frame == 0 ? 640 : 560), 0), FlameTip, 4);
        return m.ToBody(header);
    }

    // ---- a cobweb across a corner: the corner's two walls behind it (-x and -z of its origin), its top at 0 (the actor at the walls' top),
    // threads out from its hub to the walls and down the corner, three rings round, a spider hanging on a thread below it
    private static Body BuildCobweb(byte[] header)
    {
        var m = new CarMesh("a cobweb", new[] { -1 }) { Limit = 1100 };
        var hub = new Vector3(230, -330, 230);
        var anchors = new[]
        {
            new Vector3(0, 0, 0), new Vector3(760, -20, 0), new Vector3(820, -520, 0), new Vector3(520, -900, 0),
            new Vector3(0, -1050, 0), new Vector3(0, -900, 520), new Vector3(0, -520, 820), new Vector3(0, -20, 760),
        };
        void Thread(Vector3 a, Vector3 b, float w = 9)
        {
            var d = Vector3.Normalize(b - a);
            var side = Vector3.Normalize(Vector3.Cross(d, Vector3.Normalize(new Vector3(1, 0, 1)))) * w;
            m.Both(new[] { m.P(0, a - side), m.P(0, b - side), m.P(0, b + side), m.P(0, a + side) }, WebGrey, 0);
        }
        foreach (var a in anchors) Thread(hub, a);
        foreach (var f in new[] { 0.3f, 0.55f, 0.8f })
            for (var k = 0; k < anchors.Length; k++)
            {
                var a = Vector3.Lerp(hub, anchors[k], f);
                var b = Vector3.Lerp(hub, anchors[(k + 1) % anchors.Length], f);
                // (sagging a little between its threads)
                var sag = (a + b) / 2 + new Vector3(0, -25 * f, 0);
                Thread(a, sag, 7); Thread(sag, b, 7);
            }
        // the spider: on a thread down from the hub, a round body and its head, eight legs
        var spider = hub + new Vector3(60, -620, 60);
        Thread(hub, spider + new Vector3(0, 60, 0), 4);
        HotelKit.Orb(m, spider, 48, new Finish(Spider, 0.4f), 8, 3);
        HotelKit.Orb(m, spider + new Vector3(0, -45, 40), 26, new Finish(Spider, 0.4f), 6, 3);
        m.Light = 0.3f;
        for (var k = 0; k < 4; k++)
            foreach (var s in new[] { -1f, 1f })
            {
                var knee = spider + new Vector3(s * 90, 30, -50 + k * 35);
                m.Strut(0, spider + new Vector3(s * 30, 0, -30 + k * 20), knee, 9, Spider);
                m.Strut(0, knee, knee + new Vector3(s * 50, -80, k * 12 - 18), 8, Spider);
            }
        return m.ToBody(header);
    }

    // ---- the black car's tyre marks: two dark streaks a car's track apart, along +z from 0 for 3,000, wavering as it slid
    public const float TyreMarksLong = 3000;
    private static Body BuildTyreMarks(byte[] header)
    {
        var m = new CarMesh("the black car's tyre marks", new[] { -1 });
        const int Segments = 12;
        foreach (var side in new[] { -420f, 420f })
        {
            int[]? last = null;
            for (var k = 0; k <= Segments; k++)
            {
                var z = TyreMarksLong * k / Segments;
                var x = side + 45 * MathF.Sin(k * 0.9f) + (k < 3 ? (3 - k) * 30 : 0);
                var w = 60 + 12 * MathF.Sin(k * 1.7f + side);
                var pair = new[] { m.P(0, new Vector3(x - w, 3, z)), m.P(0, new Vector3(x + w, 3, z)) };
                if (last is not null) m.In(new[] { last[0], last[1], pair[1], pair[0] }, Tyre, new Vector3(x, 1000, z), unlit: true);
                last = pair;
            }
        }
        return m.ToBody(header);
    }

    // ---- the departure board: a dark board in a bevelled frame, its back to the wall (z 0), its face +z, its foot at 0; two lines in amber
    // lights -- EMERALD MOON, and NOW BOARDING lit (`lit`) or dimmed (the board's second body: the line blinks)
    public const float BoardPixel = 34, BoardHigh = 700;
    private static readonly string[] BoardLines = { "EMERALD MOON", "NOW BOARDING" };
    private static Body BuildBoard(byte[] header, bool lit)
    {
        var m = new CarMesh("the departure board", new[] { -1 }) { Limit = 1100 };
        var cols = BoardLines.Max(l => TextRects(l).Columns);
        float w = (cols + 6) * BoardPixel, h = BoardHigh, d = 110;
        HotelKit.BevelBox(m, new(0, h / 2, d / 2), new(w, h, d), 26, BoardF);
        var front = new Vector3(0, h / 2, 2000);
        Flat(m, BoardFace, front, new(-w / 2 + 50, 50, d + 1), new(w / 2 - 50, 50, d + 1), new(w / 2 - 50, h - 50, d + 1), new(-w / 2 + 50, h - 50, d + 1));
        for (var line = 0; line < BoardLines.Length; line++)
        {
            var width = TextRects(BoardLines[line]).Columns * BoardPixel;
            float left = -width / 2, top = h - 120 - line * (FontRows + 3) * BoardPixel;
            Text(m, BoardLines[line], (c, r) => new Vector3(left + c * BoardPixel, top - r * BoardPixel, d + 3), line == 1 && !lit ? AmberDim : Amber, front);
        }
        // the hangers it is fixed to the wall by, top corners
        foreach (var s in new[] { -1f, 1f }) HotelKit.BevelBox(m, new(s * (w / 2 - 120), h + 40, 40), new(80, 80, 80), 10, BoardF);
        return m.ToBody(header);
    }

    // ---- a row of lane lights in the floor along +x: eight, 640 apart from 0, each a bevelled housing with its lens lit
    public const float LaneSpacing = 640;
    private static Body BuildLaneLights(byte[] header, int lights)
    {
        var m = new CarMesh("a row of lane lights", new[] { -1 });
        for (var k = 0; k < lights; k++)
        {
            var x = k * LaneSpacing;
            HotelKit.HardBox(m, new(x, 15, 0), new(200, 30, 110), HousingF);
            Flat(m, Lens, new(x, 1000, 0), new(x - 80, 31, -38), new(x + 80, 31, -38), new(x + 80, 31, 38), new(x - 80, 31, 38));
        }
        return m.ToBody(header);
    }

    // ---- an Esmer sign: a turned post, a bevelled panel on it facing +z (and -z), on both faces an arrow pointing +x and four of the Esmers'
    // letters lit
    private static Body BuildEsmerSign(byte[] header)
    {
        var m = new CarMesh("an Esmer sign", new[] { -1 }) { Limit = 1100 };
        HotelKit.Turned(m, Vector3.Zero, new (float, float)[] { (0, 0), (110, 0), (100, 40), (45, 90), (38, 1300), (60, 1340), (0, 1360) }, 8, PostF);
        HotelKit.BevelBox(m, new(0, 1560, 0), new(950, 420, 70), 22, PanelF);
        foreach (var face in new[] { 1f, -1f })
        {
            var z = face * 37;
            var towards = new Vector3(0, 1560, face * 2000);
            // the arrow: a shaft and its head, pointing +x
            Flat(m, Arrow, towards, new(40, 1530, z), new(300, 1530, z), new(300, 1590, z), new(40, 1590, z));
            Flat(m, Arrow, towards, new(290, 1480, z), new(420, 1560, z), new(420, 1560, z), new(290, 1640, z));
            // the Esmers' letters: a ring, a bar crossed, a hook, a triangle
            float gx = -380;
            for (var k = 0; k < 8; k++)
            {
                var a0 = k * MathF.Tau / 8; var a1 = (k + 1) * MathF.Tau / 8;
                Vector3 R(float a, float r) => new(gx + r * MathF.Cos(a), 1600 + r * MathF.Sin(a), z);
                Flat(m, Glyph, towards, R(a0, 45), R(a1, 45), R(a1, 28), R(a0, 28));
            }
            Flat(m, Glyph, towards, new(-280, 1480, z), new(-260, 1480, z), new(-260, 1660, z), new(-280, 1660, z));
            Flat(m, Glyph, towards, new(-320, 1590, z), new(-220, 1590, z), new(-220, 1610, z), new(-320, 1610, z));
            Flat(m, Glyph, towards, new(-180, 1640, z), new(-100, 1640, z), new(-100, 1620, z), new(-180, 1620, z));
            Flat(m, Glyph, towards, new(-120, 1620, z), new(-100, 1620, z), new(-100, 1490, z), new(-120, 1490, z));
            Flat(m, Glyph, towards, new(-60, 1490, z), new(40, 1490, z), new(-10, 1640, z), new(-10, 1640, z));
        }
        return m.ToBody(header);
    }

    // ---- the souvenir stall: a wooden counter, posts at its corners, a striped awning sloping out over it (its hem scalloped), a string of
    // the Cup's pennants along its front, three foam fingers stood on the counter. Faces +z, its foot at 0.
    private static Body BuildStall(byte[] header)
    {
        var m = new CarMesh("the souvenir stall", new[] { -1 }) { Limit = 1100 };
        HotelKit.BevelBox(m, new(0, 440, 0), new(1800, 880, 700), 30, WoodF);
        HotelKit.BevelBox(m, new(0, 910, 0), new(1900, 60, 790), 16, WoodLightF);
        // the panels' boards: three darker strips down its front
        foreach (var x in new[] { -600f, 0f, 600f }) HotelKit.HardBox(m, new(x, 430, 356), new(60, 760, 14), WoodDarkF);
        foreach (var (x, z, top) in new[] { (-880f, -330f, 2150f), (880f, -330f, 2150f), (-880f, 340f, 1900f), (880f, 340f, 1900f) })
            HotelKit.Rod(m, new(x, 0, z), new(x, top, z), 42, WoodDarkF, 6);
        // the awning: eight stripes, red and white, from the back's top out over the front
        const int Stripes = 8;
        float x0 = -980, x1 = 980;
        Vector3 Back(float x) => new(x, 2180, -400);
        Vector3 Front(float x) => new(x, 1880, 470);
        m.Light = 0.6f;
        for (var k = 0; k < Stripes; k++)
        {
            float a = x0 + (x1 - x0) * k / Stripes, b = x0 + (x1 - x0) * (k + 1) / Stripes;
            var colour = k % 2 == 0 ? AwningRed : AwningWhite;
            m.Both(new[] { m.P(0, Back(a)), m.P(0, Back(b)), m.P(0, Front(b)), m.P(0, Front(a)) }, colour);
            // the hem: a scallop hanging under each stripe's front
            m.Both(new[] { m.P(0, Front(a)), m.P(0, Front(b)), m.P(0, Front((a + b) / 2) + new Vector3(0, -170, 10)) }, colour);
        }
        // the pennants: a cord along the front, seven triangles on it in the Cup's colours
        m.Light = 0.6f;
        m.Strut(0, new(-900, 1700, 500), new(900, 1700, 500), 10, WoodDarkF.Colour);
        for (var k = 0; k < 7; k++)
        {
            var x = -770 + k * 256f;
            m.Both(new[] { m.P(0, new(x - 90, 1700, 500)), m.P(0, new(x + 90, 1700, 500)), m.P(0, new(x + 6, 1440, 520)) }, PennantColours[k % 3]);
        }
        // the foam fingers on the counter: a cuff, a mitt of a hand, its first finger pointing up -- "We're number one"
        foreach (var (x, colour) in new[] { (-560f, 6 * 16 + 8), (0f, 4 * 16 + 5), (560f, 12 * 16 + 5) })
        {
            var f = new Finish(colour, 0.55f);
            HotelKit.Turned(m, new(x, 940, 120), new (float, float)[] { (0, 0), (80, 0), (82, 120), (70, 150), (0, 150) }, 8, f);
            HotelKit.Cushion(m, new(x, 1190, 120), new(125, 120, 60), 3, f, 8, 4);
            HotelKit.Cushion(m, new(x - 55, 1450, 120), new(38, 150, 38), 3, f, 8, 4);
            HotelKit.Cushion(m, new(x + 120, 1240, 120), new(70, 30, 36), 3, f, 6, 3);
        }
        return m.ToBody(header);
    }

    // ---- the ribcage xylophone: a dark wooden frame on four legs, and on it a giant's ribcage on its back -- the spine along x, nine ribs
    // across it from the longest (the low notes, -x) to the shortest -- and at its low end the skull. Faces +z (the player stands at -z).
    private static Body BuildXylophone(byte[] header)
    {
        var m = new CarMesh("the ribcage xylophone", new[] { -1 }) { Limit = 1100, Rounded = true };
        foreach (var x in new[] { -760f, 760f })
            foreach (var z in new[] { -230f, 230f })
                HotelKit.Rod(m, new(x, 0, z), new(x * 0.96f, 460, z * 0.9f), 28, WoodDarkF, 6);
        foreach (var z in new[] { -210f, 210f }) HotelKit.Rod(m, new(-800, 460, z), new(800, 460, z), 34, WoodDarkF, 6);
        HotelKit.Rod(m, new(-760, 500, 0), new(760, 500, 0), 38, BoneF, 6);
        m.Light = BoneF.Light;
        for (var k = 0; k < 9; k++)
        {
            var x = -640 + k * 160f;
            var half = 330 - k * 22f;
            var top = new Vector3(x, 560, 0);
            m.Strut(0, new(x, 485, -half), top + new Vector3(0, 0, -half * 0.35f), 34, BoneF.Colour);
            m.Strut(0, top + new Vector3(0, 0, -half * 0.35f), top + new Vector3(0, 0, half * 0.35f), 34, BoneF.Colour);
            m.Strut(0, top + new Vector3(0, 0, half * 0.35f), new(x, 485, half), 34, BoneF.Colour);
        }
        HotelKit.Orb(m, new(-900, 560, 0), 130, BoneF, 10, 5, 0.9f);
        HotelKit.Orb(m, new(-1010, 520, 0), 60, BoneF, 8, 3, 0.8f);
        foreach (var s in new[] { -1f, 1f }) HotelKit.Orb(m, new(-930, 600, s * 70), 34, new Finish(3 * 16 + 0, 0.2f), 6, 3);
        return m.ToBody(header);
    }

    // ================= the skeletons =================

    // The entity whose own body the game's rabbibunny skeleton is (-1: none)
    public static int SkeletonEntity(string gameDirectory) => Lba2EntityTable.Load(gameDirectory)?.KindForBody(SkeletonBody)?.Entity ?? -1;

    // Each skeleton's body: the game's, `dress` adding to its rest pose (the body as it stands: World), what it is
    private static IEnumerable<(int Generic, string Name, Func<Body, Body> Make)> Dressed => new (int, string, Func<Body, Body>)[]
    {
        (Skittle, "a skittle (small)", b => Scaled(b, SkittleScale)),
        (Warden, "the traffic warden: tabard, lollipop sign", b => Dress(Dress(b, Ribs, Tabard), RHand, Lollipop)),
        (Guide, "the tour guide: cap, flag", b => Dress(Dress(b, Head, (m, w) => Cap(m, w, 12 * 16 + 4)), RHand, GuideFlag)),
        (TouristA, "a tourist: straw hat, camera", b => Dress(Dress(b, Head, (m, w) => SunHat(m, w, StrawF.Colour, 4 * 16 + 5)), Ribs, CameraOnStrap)),
        (TouristB, "a tourist: white sun hat", b => Dress(b, Head, (m, w) => SunHat(m, w, 3 * 16 + 9, 12 * 16 + 5))),
        (TouristC, "a tourist: pink sun hat", b => Dress(b, Head, (m, w) => SunHat(m, w, 5 * 16 + 7, 8 * 16 + 5))),
        (Witness, "the witness: his skull in his hand", SkullInHand),
        (Musician, "the xylophonist: two mallets", b => Dress(Dress(b, RHand, (m, w) => Mallet(m, w, RHand)), LHand, (m, w) => Mallet(m, w, LHand))),
        (Miner, "the miner: helmet and lamp", b => Dress(b, Head, MinerHelmet)),
        (Vendor, "the stallholder: cap", b => Dress(b, Head, (m, w) => Cap(m, w, 4 * 16 + 4))),
    };

    // The game's skeleton without the blade in its hand (bone 17, the -x hand's: a spike forward, what its swipe cuts with) -- the user,
    // 2026-11-27: "where we have an NPC with a body without a gun, and one with one, preference the one without"
    private const int Blade = 17;
    private static Body Unarmed(Body b)
    {
        if (b.Bones.Count <= Blade) return b;
        var blade = b.Bones[Blade];
        bool On(int p) => p >= blade.Start && p < blade.Start + blade.Count;
        b.Faces.RemoveAll(f => f.Points.Any(On));
        b.Lines.RemoveAll(l => On(l.A) || On(l.B));
        b.Spheres.RemoveAll(s => On(s.Point));
        return b;
    }

    // The game's skeleton's bones lighter: its greys (49 lit, the start of the ramp) from BoneGrey, its black polygons (colour 0: its arms,
    // its ribs' undersides) a dark grey -- in the temple's dark halls the game's own was a black shape on a dark floor, its sun hat floating
    // over nothing (the first look, 2026-11-28)
    private const int BoneGrey = 3 * 16 + 6, BoneDark = 3 * 16 + 3;
    private static Body Lighter(Body b)
    {
        b.Faces = b.Faces.Select(f => f.Colour is >= 48 and < 64 ? f with { Colour = Math.Max(f.Colour, BoneGrey) } : f.Colour == 0 ? f with { Colour = BoneDark } : f).ToList();
        b.Lines = b.Lines.Select(l => l.Colour is 0 or (>= 48 and < 64) ? l with { Colour = BoneGrey + 2 } : l).ToList();
        b.Spheres = b.Spheres.Select(s => s.Colour is 0 or (>= 48 and < 64) ? s with { Colour = BoneGrey } : s).ToList();
        return b;
    }

    // A body's points scaled about its root (its bones' pivots with them), its spheres too
    private static Body Scaled(Body b, float k)
    {
        b.Vertices = b.Vertices.Select(v => v * k).ToList();
        b.Spheres = b.Spheres.Select(s => s with { Radius = Math.Max(1, (int)MathF.Round(s.Radius * k)) }).ToList();
        return b;
    }

    // What `make` builds -- a CarMesh in the body's rest pose (World) -- added to bone `bone`, after its own points (every later point moved
    // along), its faces as made (lit or not), the light its points take as the mesh's
    private static Body Dress(Body body, int bone, Action<CarMesh, Vector3[]> make)
    {
        var world = body.World();
        var m = new CarMesh("a skeleton's dress", new[] { -1 }) { Limit = 1100 };
        make(m, world);
        var add = m.ToBody(body.Header);
        var addWorld = add.World();
        var owner = body.Bones[bone];
        var at = owner.Start + owner.Count;
        var k = addWorld.Length;
        int Moved(int p) => p >= at ? p + k : p;
        var all = world.ToList();
        all.InsertRange(at, addWorld);
        for (var b = 0; b < body.Bones.Count; b++)
        {
            var bn = body.Bones[b];
            if (b == bone) body.Bones[b] = bn with { Count = bn.Count + k, Pivot = Moved(bn.Pivot) };
            else if (bn.Start >= at) body.Bones[b] = bn with { Start = bn.Start + k, Pivot = Moved(bn.Pivot) };
            else body.Bones[b] = bn with { Pivot = Moved(bn.Pivot) };
        }
        for (var i = 0; i < body.Faces.Count; i++) body.Faces[i] = body.Faces[i] with { Points = body.Faces[i].Points.Select(Moved).ToArray(), PointNormals = null };
        for (var i = 0; i < body.Lines.Count; i++) body.Lines[i] = body.Lines[i] with { A = Moved(body.Lines[i].A), B = Moved(body.Lines[i].B) };
        for (var i = 0; i < body.Spheres.Count; i++) body.Spheres[i] = body.Spheres[i] with { Point = Moved(body.Spheres[i].Point) };
        foreach (var f in add.Faces) body.Faces.Add(new Face(f.Points.Select(p => p + at).ToArray(), f.Colour, Material: f.Material));
        var scales = body.LightScale ?? Enumerable.Repeat(1f, world.Length).ToArray();
        var addScales = add.LightScale ?? Enumerable.Repeat(1f, k).ToArray();
        body.LightScale = scales.Take(at).Concat(addScales).Concat(scales.Skip(at)).ToArray();
        body.Vertices = Enumerable.Repeat(Vector3.Zero, all.Count).ToList();
        body.SetWorld(all.ToArray());
        body.LimitOverride = 1100;
        return body;
    }

    // the middle of a bone's points (rest pose), and their highest
    private static Vector3 Middle(Body b, Vector3[] w, int bone) { var bn = b.Bones[bone]; return w.Skip(bn.Start).Take(bn.Count).Aggregate(Vector3.Zero, (s, p) => s + p) / Math.Max(1, bn.Count); }

    // (the dress-makers below are each handed the mesh and the body's rest pose; `headTop` etc. read off the game's skeleton: its head
    // y 1,156-1,331, x -70..105, z -105..210 -- the muzzle forward -- its ribcage y 806-1,156, its hands at y ~500 by x +-210)
    private static readonly Vector3 HeadTop = new(17, 1318, 10), RHandAt = new(217, 508, -10), LHandAt = new(-182, 526, -10);

    // the hi-vis tabard over the ribcage: a box round it, yellow, two silvery stripes round it
    private static void Tabard(CarMesh m, Vector3[] _)
    {
        HotelKit.HardBox(m, new(17, 975, 25), new(330, 320, 190), HiVisF);
        foreach (var y in new[] { 900f, 1010f })
        {
            Flat(m, Stripe, new(17, y, 2000), new(-148, y - 18, 121), new(182, y - 18, 121), new(182, y + 18, 121), new(-148, y + 18, 121));
            Flat(m, Stripe, new(17, y, -2000), new(-148, y - 18, -71), new(182, y - 18, -71), new(182, y + 18, -71), new(-148, y + 18, -71));
        }
    }

    // the lollipop sign in his hand: a white pole up past his head, a disc on it facing +z -- white, a red rim, SLOW on both faces
    private static void Lollipop(CarMesh m, Vector3[] _)
    {
        var x = RHandAt.X + 12;
        HotelKit.Rod(m, new(x, RHandAt.Y - 120, RHandAt.Z + 25), new(x, 1660, RHandAt.Z + 25), 17, PoleF, 6);
        var c = new Vector3(x, 1830, RHandAt.Z + 25);
        const int N = 16;
        const float R = 235, Inner = 195, Half = 14;
        foreach (var face in new[] { 1f, -1f })
        {
            var z = c.Z + face * Half;
            var towards = new Vector3(c.X, c.Y, c.Z + face * 2000);
            m.Light = DiscF.Light;
            var centre = m.P(0, new(c.X, c.Y, z));
            var inner = Enumerable.Range(0, N).Select(i => m.P(0, new(c.X + Inner * MathF.Cos(i * MathF.Tau / N), c.Y + Inner * MathF.Sin(i * MathF.Tau / N), z))).ToArray();
            m.Light = RimF.Light;
            var outer = Enumerable.Range(0, N).Select(i => m.P(0, new(c.X + R * MathF.Cos(i * MathF.Tau / N), c.Y + R * MathF.Sin(i * MathF.Tau / N), z))).ToArray();
            for (var i = 0; i < N; i++)
            {
                var n = (i + 1) % N;
                m.In(new[] { centre, inner[i], inner[n] }, DiscF.Colour, towards);
                m.In(new[] { inner[i], outer[i], outer[n], inner[n] }, RimF.Colour, towards);
            }
            // SLOW, read from that side (the back's mirrored, so it reads from behind too)
            const float Px = 13;
            var width = TextRects("SLOW").Columns * Px;
            Text(m, "SLOW", (col, row) => new Vector3(c.X + face * (-width / 2 + col * Px), c.Y + 2.5f * Px - row * Px, z + face * 2), Ink, towards);
        }
        // its edge: a red band round the disc
        m.Light = RimF.Light;
        var front = Enumerable.Range(0, N).Select(i => m.P(0, new(c.X + R * MathF.Cos(i * MathF.Tau / N), c.Y + R * MathF.Sin(i * MathF.Tau / N), c.Z + Half))).ToArray();
        var back = Enumerable.Range(0, N).Select(i => m.P(0, new(c.X + R * MathF.Cos(i * MathF.Tau / N), c.Y + R * MathF.Sin(i * MathF.Tau / N), c.Z - Half))).ToArray();
        m.Skin(front, back, _ => RimF.Colour, c);
    }

    // a peaked cap on his skull
    private static void Cap(CarMesh m, Vector3[] _, int colour)
    {
        var f = new Finish(colour, 0.6f);
        var at = HeadTop + new Vector3(0, -40, -20);
        HotelKit.Turned(m, at, new (float, float)[] { (0, 0), (128, 0), (126, 55), (100, 115), (55, 145), (0, 152) }, 10, f);
        // the peak, out over his muzzle, bevelled
        m.Light = f.Light;
        var a = m.P(0, at + new Vector3(-105, 8, 95)); var b = m.P(0, at + new Vector3(105, 8, 95));
        var c = m.P(0, at + new Vector3(85, -6, 280)); var d = m.P(0, at + new Vector3(-85, -6, 280));
        m.Up(new[] { a, b, c, d }, colour);
        m.In(new[] { a, b, c, d }, colour, at + new Vector3(0, -1000, 180));
    }

    // a sun hat: a wide brim, a round crown, a band round its foot
    private static void SunHat(CarMesh m, Vector3[] _, int colour, int band)
    {
        var at = HeadTop + new Vector3(0, -35, -15);
        var f = new Finish(colour, 0.6f);
        HotelKit.Lathe(m, at, Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ, new (float, float)[]
        {
            (0, 20), (118, 20), (262, -45), (272, -32), (126, 32), (124, 80), (119, 125), (92, 165), (0, 178),
        }, 14, k => k == 4 ? new Finish(band, 0.6f) : f);
    }

    // a camera on a strap round his neck, at his chest
    private static void CameraOnStrap(CarMesh m, Vector3[] _)
    {
        HotelKit.BevelBox(m, new(17, 960, 140), new(190, 120, 70), 12, CameraF);
        HotelKit.Lathe(m, new(17, 960, 175), Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, new (float, float)[] { (0, 0), (40, 0), (42, 50), (30, 70), (0, 70) }, 8, _ => CameraF);
        Flat(m, Lens, new(17, 960, 2000), new(-3, 945, 246), new(37, 945, 246), new(37, 975, 246), new(-3, 975, 246));
        m.Light = 0.4f;
        m.Strut(0, new(-60, 1000, 120), new(-40, 1150, 40), 10, 3 * 16 + 2);
        m.Strut(0, new(94, 1000, 120), new(74, 1150, 40), 10, 3 * 16 + 2);
    }

    // the tour guide's flag: a pole up from his hand, a red and yellow pennant at its top
    private static void GuideFlag(CarMesh m, Vector3[] _)
    {
        var x = RHandAt.X + 12;
        HotelKit.Rod(m, new(x, RHandAt.Y - 100, RHandAt.Z + 25), new(x, 1960, RHandAt.Z + 25), 15, PoleF, 6);
        HotelKit.Orb(m, new(x, 1975, RHandAt.Z + 25), 28, GoldF, 6, 3);
        m.Light = 0.6f;
        float z = RHandAt.Z + 25;
        m.Both(new[] { m.P(0, new(x, 1930, z)), m.P(0, new(x + 420, 1850, z + 30)), m.P(0, new(x, 1760, z)) }, 4 * 16 + 6);
        m.Both(new[] { m.P(0, new(x, 1880, z + 3)), m.P(0, new(x + 250, 1846, z + 21)), m.P(0, new(x, 1812, z + 3)) }, 6 * 16 + 9);
    }

    // a mallet in a hand: a stick out of the fist, a round head -- angled so that his playing (AnimPlay: the forearm brought up and forward,
    // a quarter turn in all) has it pointing forward and down at the bars
    private static void Mallet(CarMesh m, Vector3[] _, int hand)
    {
        var at = hand == RHand ? RHandAt : LHandAt;
        var dir = Vector3.Normalize(new Vector3(0, -0.82f, -0.57f));
        var end = at + dir * 340;
        HotelKit.Rod(m, at - dir * 30, end, 13, WoodLightF, 5);
        HotelKit.Orb(m, end + dir * 30, 46, new Finish(4 * 16 + 6, 0.6f), 8, 3);
    }

    // the miner's helmet: a yellow dome with a brim all round, its lamp at the front
    private static void MinerHelmet(CarMesh m, Vector3[] _)
    {
        var at = HeadTop + new Vector3(0, -45, -10);
        HotelKit.Lathe(m, at, Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ, new (float, float)[]
        {
            (0, -2), (190, -2), (196, 14), (150, 22), (148, 60), (128, 120), (88, 165), (0, 182),
        }, 12, _ => MinerF);
        HotelKit.Lathe(m, at + new Vector3(0, 95, 130), Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY, new (float, float)[] { (0, 0), (42, 0), (46, 40), (40, 52), (0, 52) }, 8, _ => new Finish(3 * 16 + 3, 0.6f));
        Flat(m, LampLens, new(at.X, at.Y + 95, 2000), at + new Vector3(-30, 70, 184), at + new Vector3(30, 70, 184), at + new Vector3(30, 120, 184), at + new Vector3(-30, 120, 184));
    }

    // The witness: the game's skeleton with his skull off his neck and in his hand (the -x hand, at his side) -- the head bone's polygons
    // left out, a copy of them added to the hand, turned to face forward a little up
    private static Body SkullInHand(Body body)
    {
        var world = body.World();
        var head = body.Bones[Head];
        bool OnHead(int p) => p >= head.Start && p < head.Start + head.Count;
        var skullFaces = body.Faces.Where(f => f.Points.All(OnHead)).ToList();
        body.Faces.RemoveAll(f => f.Points.All(OnHead));
        body.Lines.RemoveAll(l => OnHead(l.A) && OnHead(l.B));
        body.Spheres.RemoveAll(s => OnHead(s.Point));
        var centre = Middle(body, world, Head);
        var to = LHandAt + new Vector3(-40, -60, 90);
        var turn = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -0.35f);
        return Dress(body, LHand, (m, w) =>
        {
            var map = new Dictionary<int, int>();
            int P(int p) => map.TryGetValue(p, out var h) ? h : map[p] = m.P(0, to + Vector3.Transform(w[p] - centre, turn));
            m.Light = 1;
            foreach (var f in skullFaces) m.Raw(f.Points.Select(P).ToArray(), f.Colour, f.Material);
        });
    }

    // Into the game folder: each dressed skeleton as a body at BODY.HQR's end and the skeleton's entity's body of its number, and the three
    // animations at ANIM.HQR's end, the entity's of theirs -- once (an entity that has them keeps them, where they are this build's)
    public static List<string> InstallSkeletons(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        if (!File.Exists(bodyPath) || !File.Exists(animPath) || !File.Exists(ressPath)) return new() { "WARNING: the Temple of Bú's skeletons: no BODY.HQR, ANIM.HQR or RESS.HQR -- not made" };
        var table = HqrArchive.Open(ressPath).Read(44);
        var parsed = Lba2EntityTable.Parse(table);
        if (parsed.KindForBody(SkeletonBody) is not { } kind) return new() { $"WARNING: the Temple of Bú's skeletons: no entity has the rabbibunny skeleton (BODY.HQR {SkeletonBody}) -- not made" };
        var entity = kind.Entity;
        var ent = parsed.Entities[entity];
        var log = new List<string>();
        var bodies = HqrArchive.Open(bodyPath);
        var source = bodies.Read(SkeletonBody);
        var count = HqrArchive.CountEntries(bodyPath);
        var file = File.ReadAllBytes(bodyPath);
        var made = new List<string>();
        foreach (var (generic, name, make) in Dressed)
        {
            var bytes = make(Lighter(Unarmed(Body.Read(source, 2)))).Write();
            var have = ent.Bodies.Where(b => b.Generic == generic).Select(b => (int?)b.Body).FirstOrDefault();
            if (have is { } h && h < count && bodies.Read(h).AsSpan().SequenceEqual(bytes)) continue;
            file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(bytes));
            table = RaceTrackBaldinoCar.WithBody(table, entity, generic, count);
            made.Add($"{name} BODY.HQR {count} (body {generic})");
            count++;
        }
        if (made.Count > 0) File.WriteAllBytes(bodyPath, file);
        // the animations: from the standing's first frame (its bone 1's shift as it has it), or the getting up's sitting frame
        var standIndex = ent.Anims.Where(a => a.Generic == AnimStand).Select(a => (int?)a.Anim).FirstOrDefault() ?? -1;
        var getUpIndex = ent.Anims.Where(a => a.Generic == AnimGetUp).Select(a => (int?)a.Anim).FirstOrDefault() ?? -1;
        if (standIndex < 0 || getUpIndex < 0) log.Add($"WARNING: the Temple of Bú's skeletons: entity {entity} has no standing ({AnimStand}) or getting up ({AnimGetUp}) -- no animations of their own");
        else
        {
            var anims = HqrArchive.Open(animPath);
            var bones = Body.Read(source, 2).Bones.Count;
            var stand = CharacterAnim.Read(anims.Read(standIndex), bones, AnimStand, "stand").Keys[0];
            var getUp = CharacterAnim.Read(anims.Read(getUpIndex), bones, AnimGetUp, "get up");
            foreach (var (generic, anim, name) in new[]
            {
                (AnimSit, MakeSit(getUp), "sitting"),
                (AnimPlay, MakePlay(stand), "playing the xylophone"),
                (AnimSignUp, MakeSignUp(stand), "holding the lollipop sign up"),
                (AnimHold, MakeHold(stand), "holding the lollipop sign"),
            })
            {
                var bytes = anim.Write(bones);
                var have = ent.Anims.Where(a => a.Generic == generic).Select(a => (int?)a.Anim).FirstOrDefault();
                if (have is { } h && anims.IsValid(h) && anims.Read(h).AsSpan().SequenceEqual(bytes)) continue;
                var at = HqrArchive.CountEntries(animPath);
                HqrWriter.AppendToFile(animPath, bytes);
                table = RaceTrackJumpAnim.WithAnim(table, entity, generic, at);
                made.Add($"{name} ANIM.HQR {at} (animation {generic})");
            }
        }
        // the flying rat twice the game's size, its faces shaded in its own brown-greys -- the game's is a flat near-black speck in the
        // temple's dark (the Halloween temple's, TempleHalloween.RatBody): its entity's body BigRat, its own flapping
        if (Lba2EntityTable.Parse(table).KindForBody(Temple.TempleHalloween.FlyingRatBody) is { } rat && Temple.TempleHalloween.RatBody(gameDirectory) is { } ratBytes)
        {
            var have = Lba2EntityTable.Parse(table).Entities[rat.Entity].Bodies.Where(b => b.Generic == BigRat).Select(b => (int?)b.Body).FirstOrDefault();
            var n = HqrArchive.CountEntries(bodyPath);
            if (have is not { } h || h >= n || !HqrArchive.Open(bodyPath).Read(h).AsSpan().SequenceEqual(ratBytes))
            {
                HqrWriter.AppendToFile(bodyPath, ratBytes);
                table = RaceTrackBaldinoCar.WithBody(table, rat.Entity, BigRat, n);
                made.Add($"the flying rat, bigger and shaded BODY.HQR {n} (entity {rat.Entity}'s body {BigRat})");
            }
        }
        HqrWriter.ReplaceInFile(ressPath, 44, table);
        log.Add(made.Count == 0 ? $"the Temple of Bú's skeletons: entity {entity}'s already" : $"the Temple of Bú's skeletons, entity {entity}'s: {string.Join(", ", made)}");
        return log;
    }

    // a keyframe: `from` with turns (degrees about x, y, z) added to some bones
    private static CharacterAnim.Key Key(CharacterAnim.Key from, int ms, params (int Bone, float X, float Y, float Z)[] turns)
    {
        var k = from.Copy();
        k.Milliseconds = ms; k.Step = Vector3.Zero;
        foreach (var (b, x, y, z) in turns)
            if (b > 0 && b < k.Bones.Count && k.Bones[b].Type == 0) k.Bones[b] = (0, k.Bones[b].Value + new Vector3(x, y, z));
        return k;
    }

    // sitting: the getting up's frame where it sits up, its legs out before it, held
    private const int SitFrame = 5;
    private static CharacterAnim MakeSit(CharacterAnim getUp)
    {
        var k = getUp.Keys[Math.Min(SitFrame, getUp.Keys.Count - 1)].Copy();
        k.Milliseconds = 800; k.Step = Vector3.Zero;
        var sit = new CharacterAnim { Generic = AnimSit, Name = "sit", LoopFrame = 0 };
        sit.Keys.Add(k);
        sit.Keys.Add(Key(k, 900, (Head, 0, 10, 0)));
        sit.Keys.Add(Key(k, 900, (Head, 0, -10, 0)));
        return sit;
    }

    // playing the xylophone: both upper arms forward, the forearms raised to strike, each in turn brought down onto a bar
    private static CharacterAnim MakePlay(CharacterAnim.Key stand)
    {
        var play = new CharacterAnim { Generic = AnimPlay, Name = "play", LoopFrame = 0 };
        (int, float, float, float) Up(int bone, float deg) => (bone, deg, 0, 0);
        play.Keys.Add(Key(stand, 160, Up(RUpper, 35), Up(LUpper, 35), Up(RFore, 70), Up(LFore, 40), (Head, 12, 0, 0)));
        play.Keys.Add(Key(stand, 160, Up(RUpper, 35), Up(LUpper, 35), Up(RFore, 40), Up(LFore, 70), (Head, 12, 6, 0)));
        play.Keys.Add(Key(stand, 160, Up(RUpper, 35), Up(LUpper, 35), Up(RFore, 70), Up(LFore, 40), (Head, 14, 0, 0)));
        play.Keys.Add(Key(stand, 160, Up(RUpper, 35), Up(LUpper, 35), Up(RFore, 40), Up(LFore, 70), (Head, 12, -6, 0)));
        return play;
    }

    // a keyframe: `from` with some bones' turns set outright (degrees about x, y, z)
    private static CharacterAnim.Key Pose(CharacterAnim.Key from, int ms, params (int Bone, float X, float Y, float Z)[] set)
    {
        var k = from.Copy();
        k.Milliseconds = ms; k.Step = Vector3.Zero;
        foreach (var (b, x, y, z) in set)
            if (b > 0 && b < k.Bones.Count && k.Bones[b].Type == 0) k.Bones[b] = (0, new Vector3(x, y, z));
        return k;
    }
    // (the standing skeleton's ribcage leans forward this much: an arm's turns are from it -- the sign's pole upright when the arm's, the
    // forearm's and the hand's turns and this come to nothing)
    private const float RibLean = 32.3f;

    // the lollipop sign held up high, waved a little: the sign's arm raised forward, the hand turned so the pole stays upright, the other
    // hand up, palm out
    private static CharacterAnim MakeSignUp(CharacterAnim.Key stand)
    {
        var up = new CharacterAnim { Generic = AnimSignUp, Name = "sign up", LoopFrame = 1 };
        (int, float, float, float)[] Arm(float upper, float fore, float left) => new (int, float, float, float)[]
        {
            (RUpper, upper, 0, 0), (RFore, fore, 0, 0), (RHand, -(RibLean + upper + fore), 0, 0), (LUpper, left, -20, 0), (LFore, 70, 0, 0),
        };
        up.Keys.Add(Pose(stand, 250, Arm(70, 20, 40)));
        up.Keys.Add(Pose(stand, 300, Arm(118, -8, 80)));
        up.Keys.Add(Pose(stand, 300, Arm(105, 8, 95)));
        return up;
    }

    // the lollipop sign held before him, its pole upright: his forearm level, the hand at his chest
    private static CharacterAnim MakeHold(CharacterAnim.Key stand)
    {
        var hold = new CharacterAnim { Generic = AnimHold, Name = "hold", LoopFrame = 0 };
        (int, float, float, float)[] Arm(float upper, float fore, float head) => new (int, float, float, float)[]
        {
            (RUpper, upper, 0, 0), (RFore, fore, 0, 0), (RHand, -(RibLean + upper + fore), 0, 0), (Head, -19.7f, head, 0),
        };
        hold.Keys.Add(Pose(stand, 900, Arm(0, 58, 0)));
        hold.Keys.Add(Pose(stand, 1100, Arm(2, 55, 14)));
        hold.Keys.Add(Pose(stand, 900, Arm(0, 58, 0)));
        hold.Keys.Add(Pose(stand, 1100, Arm(-2, 60, -14)));
        return hold;
    }

    // ================= the xylophone's tune =================

    // A skeleton's waltz on a xylophone of bones, in D minor, 3/4 at 168 to the minute: the tune on the bars over a low bony "tock" on the
    // beat and two chords after it, eight bars (two phrases), looped by its player's track. 8-bit mono at 22,050 Hz, as the game's own samples.
    private const int Rate = 22050;
    public const double TuneBeat = 60.0 / 168, TuneBars = 8;
    public static double TuneSeconds => TuneBeat * 3 * TuneBars;

    public static int InstallTune(string gameDirectory)
    {
        var path = Path.Combine(gameDirectory, "SAMPLES.HQR");
        if (!File.Exists(path)) return -1;
        var bytes = Tune();
        var count = HqrArchive.CountEntries(path);
        var samples = HqrArchive.Open(path);
        for (var k = Math.Max(0, count - 32); k < count; k++)
            if (samples.IsValid(k) && samples.Read(k).AsSpan().SequenceEqual(bytes)) return k;
        HqrWriter.AppendToFile(path, bytes);
        return count;
    }

    private static double Hz(int midi) => 440.0 * Math.Pow(2, (midi - 69) / 12.0);

    private static byte[] Tune()
    {
        var n = (int)(Rate * TuneSeconds);
        var s = new double[n];
        void Note(double at, double hz, double level, Func<double, double, double> voice, double length = 1.2)
        {
            var i0 = (int)(at * Rate);
            var count = (int)(length * Rate);
            for (var k = 0; k < count; k++)
            {
                // (wrapped round to the start: the loop rings on across its own seam)
                var i = (i0 + k) % n;
                s[i] += level * voice(k / (double)Rate, hz);
            }
        }
        // a xylophone's bar: its note and the bar's own overtones (about 3 and 6.3 times), struck hard, dying fast -- a dry, woody, bony knock
        static double Xylo(double t, double hz) =>
            (Math.Sin(2 * Math.PI * hz * t) * Math.Exp(-7 * t) + 0.45 * Math.Sin(2 * Math.PI * hz * 3.0 * t) * Math.Exp(-16 * t)
             + 0.2 * Math.Sin(2 * Math.PI * hz * 6.3 * t) * Math.Exp(-30 * t)) * Math.Min(1, t / 0.0015)
            + 0.25 * Math.Exp(-180 * t) * Math.Sin(2 * Math.PI * 2400 * t);
        // a low bone "tock": a short dull knock with a little pitch
        static double Tock(double t, double hz) => (Math.Sin(2 * Math.PI * hz * t * (1 + 0.6 * Math.Exp(-40 * t))) * Math.Exp(-14 * t)) * Math.Min(1, t / 0.002);
        // the chords on beats two and three: a soft marimba-like dyad
        static double Mallet(double t, double hz) => (Math.Sin(2 * Math.PI * hz * t) + 0.3 * Math.Sin(2 * Math.PI * hz * 4 * t) * Math.Exp(-12 * t)) * Math.Exp(-5 * t) * Math.Min(1, t / 0.003);

        // the harmony a bar: Dm, Dm, Gm, A7 | Dm, Bb, A7, Dm (roots under, the chord's two upper notes on beats two and three)
        var bars = new (int Root, int A, int B)[]
        {
            (38, 62, 65), (38, 62, 65), (43, 62, 67), (45, 61, 67),
            (38, 62, 65), (46, 62, 65), (45, 61, 64), (38, 62, 65),
        };
        for (var b = 0; b < bars.Length; b++)
        {
            var at = b * 3 * TuneBeat;
            Note(at, Hz(bars[b].Root), 0.55, Tock, 0.6);
            foreach (var beat in new[] { 1, 2 })
            {
                Note(at + beat * TuneBeat, Hz(bars[b].A), 0.18, Mallet, 0.5);
                Note(at + beat * TuneBeat, Hz(bars[b].B), 0.18, Mallet, 0.5);
            }
        }
        // the tune (bar, beat, note): a bony little danse macabre
        var tune = new (double Bar, double Beat, int Note)[]
        {
            (0, 0, 74), (0, 1, 77), (0, 2, 81),
            (1, 0, 80), (1, 1.5, 81), (1, 2, 77),
            (2, 0, 79), (2, 1, 82), (2, 2, 86),
            (3, 0, 85), (3, 1, 81), (3, 1.5, 79), (3, 2, 76),
            (4, 0, 74), (4, 1, 77), (4, 2, 81),
            (5, 0, 82), (5, 1, 81), (5, 1.5, 79), (5, 2, 77),
            (6, 0, 76), (6, 0.5, 77), (6, 1, 79), (6, 2, 73),
            (7, 0, 74), (7, 1, 69), (7, 2, 62),
        };
        foreach (var (bar, beat, note) in tune)
        {
            var at = (bar * 3 + beat) * TuneBeat;
            Note(at, Hz(note), 0.42, Xylo, 1.0);
            // (each bar struck with both mallets close together now and then: a rattle of bones on the bar's first beat)
            if (beat == 0) Note(at + 0.045, Hz(note), 0.18, Xylo, 0.6);
        }
        var peak = s.Max(Math.Abs);
        if (peak > 0) for (var i = 0; i < n; i++) s[i] *= 0.85 / peak;
        return Wav(s);
    }

    private static byte[] Wav(double[] s)
    {
        var pcm = s.Select(v => (byte)Math.Round(128 + Math.Clamp(v, -1, 1) * 127)).ToArray();
        var wav = new MemoryStream();
        var w = new BinaryWriter(wav);
        w.Write("RIFF"u8); w.Write(36 + pcm.Length); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate); w.Write((short)1); w.Write((short)8);
        w.Write("data"u8); w.Write(pcm.Length); w.Write(pcm);
        return wav.ToArray();
    }
}
