using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Numerics;
using LBAAssembler.Scenes;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// Chapter 2's props on Citadel Island, made as 3D bodies (the user, 2026-11-06): the old tyre Bob fished out of the harbour for Luc's barrel
// car ("model a round rubber tyre" -- it was a sprite of a wheel that didn't look like one), the customer's umbrella on its hook by the
// Tavern's door ("there's no umbrella there"), and the tin of fishing bait on Mr. Bazoo's counter. Each is a body of the game's static
// prop entity, Prop (entity 17: one body of a single bone, the shop's holomap's, and its animation 0 -- these are single-bone bodies
// too), under a generic number of its own; an actor of that entity with that body shows it (RaceTrackStory.Chapters.cs places them).
internal static partial class CitadelProps
{
    public const int Prop = 17, Tyre = 1, Umbrella = 2, Hook = 3, Bait = 4, Bottle = 5, Plate = 6, Flame = 7, Flame2 = 8, Page = 9, Stool = 10;
    // (the palette's ramps: 48 the greys from black, 56 a steel grey, 18 rust brown, 20 wood, 66 red, 130 green, 76 pink)
    private const int Rubber = 48, Steel = 56, Rust = 18, Wood = 20, Red = 66, Green = 130, Worm = 76, Glass = 164, Cream = 36, FlameOuter = 242, FlameCore = 245, Paper = 44, Ink = 48;
    // (the shop's holomap's body, BODY.HQR 30: its header for the new bodies')
    private const int HeaderBody = 30;

    // The old tyre, lying flat: a round rubber tube (outer radius 240, 160 high) round a rusty steel rim with its hub
    public static Body BuildTyre(byte[] header)
    {
        var m = new CarMesh("the old tyre", new[] { -1 });
        const int Around = 16, Section = 8;
        const float Middle = 180, Across = 62, High = 80;
        m.Light = 0.45f;
        var rings = new int[Around][];
        for (var i = 0; i < Around; i++)
        {
            var a = i * MathF.Tau / Around;
            var (s, c) = (MathF.Sin(a), MathF.Cos(a));
            rings[i] = Enumerable.Range(0, Section).Select(k =>
            {
                var t = k * MathF.Tau / Section;
                var r = Middle + Across * MathF.Cos(t);
                return m.P(0, new Vector3(r * s, High + (High - 4) * MathF.Sin(t), r * c));
            }).ToArray();
        }
        for (var i = 0; i < Around; i++)
        {
            var a = (i + 0.5f) * MathF.Tau / Around;
            var tube = new Vector3(Middle * MathF.Sin(a), High, Middle * MathF.Cos(a));
            var n = (i + 1) % Around;
            for (var k = 0; k < Section; k++)
                m.Out(new[] { rings[i][k], rings[i][(k + 1) % Section], rings[n][(k + 1) % Section], rings[n][k] }, Rubber, tube);
        }
        // the rim: a rusty dish across the hole, a steel hub in its middle
        m.Light = 0.6f;
        var rim = m.Loop(0, new Vector3(0, High + 10, 0), new Vector3(Middle - Across + 8, 0, 0), new Vector3(0, 0, Middle - Across + 8), 12);
        var hub = m.Loop(0, new Vector3(0, High + 30, 0), new Vector3(40, 0, 0), new Vector3(0, 0, 40), 6);
        for (var k = 0; k < 12; k++) m.Both(new[] { rim[k], rim[(k + 1) % 12], m.P(0, new Vector3(0, High + 22, 0)) }, Rust);
        m.Cap(hub, m.P(0, new Vector3(0, High + 50, 0)), Steel, new Vector3(0, High, 0));
        return m.ToBody(header);
    }

    // The hook on the wall: a little wooden plate against it (the wall in the y-z plane, at x 0) and a steel peg out of it along +x
    public static Body BuildHook(byte[] header)
    {
        var m = new CarMesh("the umbrella's hook", new[] { -1 });
        m.Light = 0.7f;
        m.Box(0, new Vector3(8, 0, 0), new Vector3(16, 160, 110), Wood);
        m.Strut(0, new Vector3(10, 30, 0), new Vector3(110, 30, 0), 26, Steel);
        m.Strut(0, new Vector3(110, 30, 0), new Vector3(110, 70, 0), 26, Steel);
        return m.ToBody(header);
    }

    // The customer's umbrella, closed, hanging by its crook from the hook's peg (its origin: the peg's end, at the hook's own origin): the
    // crook over the peg (in the y-z plane, the peg through it), the shaft down, the furled red canopy round it, a steel tip
    public static Body BuildUmbrella(byte[] header)
    {
        var m = new CarMesh("the customer's umbrella", new[] { -1 });
        m.Light = 0.6f;
        // (the crook: a half round over the peg, x 90 -- the peg's end -- from the shaft's top at z +45 to its tip at z -45)
        const float X = 90, Peg = 30;
        var crook = Enumerable.Range(0, 7).Select(k =>
        {
            var a = k * MathF.PI / 6;
            return new Vector3(X, Peg + 45 * MathF.Sin(a), 45 * MathF.Cos(a));
        }).ToArray();
        for (var k = 0; k + 1 < crook.Length; k++) m.Strut(0, crook[k], crook[k + 1], 22, Wood);
        m.Strut(0, crook[^1], crook[^1] - new Vector3(0, 50, 0), 22, Wood);
        var top = new Vector3(X, Peg - 120, 45);
        m.Strut(0, crook[0], top, 22, Wood);
        // the canopy: eight folds round the shaft from under the handle to the tip, fuller a third of the way down
        m.Light = 0.5f;
        var levels = new (float Y, float R)[] { (top.Y, 18), (top.Y - 60, 52), (top.Y - 230, 64), (top.Y - 520, 30) };
        var rings = levels.Select(l => Enumerable.Range(0, 8).Select(k =>
        {
            var a = k * MathF.Tau / 8;
            var r = l.R * (k % 2 == 0 ? 1f : 0.72f);
            return m.P(0, new Vector3(X + r * MathF.Sin(a), l.Y, 45 + r * MathF.Cos(a)));
        }).ToArray()).ToArray();
        for (var j = 0; j + 1 < rings.Length; j++) m.Skin(rings[j], rings[j + 1], _ => Red, new Vector3(X, (levels[j].Y + levels[j + 1].Y) / 2, 45));
        m.Cap(rings[0], m.P(0, top + new Vector3(0, 10, 0)), Red, new Vector3(X, top.Y - 30, 45));
        var tip = m.P(0, new Vector3(X, levels[^1].Y - 90, 45));
        m.Cap(rings[^1], tip, Steel, new Vector3(X, levels[^1].Y + 30, 45));
        return m.ToBody(header);
    }

    // The tin of fishing bait: a steel tin with a green label round it, its lid open on a few pink worms. `height` its height as a part of
    // the counter's tin's (the inventory's is squatter: the user, 2026-11-08, "Its inventory model is too tall")
    public static Body BuildBait(byte[] header) => BuildBait(header, 1);
    public static Body BuildBait(byte[] header, float height)
    {
        var m = new CarMesh("the tin of bait", new[] { -1 });
        m.Light = 0.6f;
        var levels = new[] { 0f, 35, 115, 150 }.Select(y => y * height).ToArray();
        var rings = levels.Select(y => m.Loop(0, new Vector3(0, y, 0), new Vector3(95, 0, 0), new Vector3(0, 0, 95), 10)).ToArray();
        for (var j = 0; j + 1 < rings.Length; j++) m.Skin(rings[j], rings[j + 1], _ => j == 1 ? Green : Steel, new Vector3(0, (levels[j] + levels[j + 1]) / 2, 0));
        m.Cap(rings[^1], m.P(0, new Vector3(0, 150 * height - 8, 0)), Rust, new Vector3(0, 80 * height, 0));
        m.Light = 1;
        foreach (var (x, z) in new[] { (-35f, 10f), (20f, -28f), (25f, 32f) }) m.Ball(0, new Vector3(x, 150 * height + 10, z), 22, Worm);
        return m.ToBody(header);
    }

    // A bottle for the Tralu's bar's round tables (the user, 2026-11-07: the items on the barrels were "more off the top of a barrel than
    // on it" -- one-cell blocks of the grid, each in a corner cell of a barrel two cells across; these stand anywhere on it): a teal
    // glass body, round shoulders, a neck and a cork
    public static Body BuildBottle(byte[] header)
    {
        var m = new CarMesh("a bottle", new[] { -1 });
        m.Light = 0.7f;
        var levels = new (float Y, float R)[] { (0, 62), (170, 66), (215, 46), (240, 24), (300, 22) };
        var rings = levels.Select(l => m.Loop(0, new Vector3(0, l.Y, 0), new Vector3(l.R, 0, 0), new Vector3(0, 0, l.R), 10)).ToArray();
        for (var j = 0; j + 1 < rings.Length; j++) m.Skin(rings[j], rings[j + 1], _ => Glass, new Vector3(0, (levels[j].Y + levels[j + 1].Y) / 2, 0));
        var cork = m.Loop(0, new Vector3(0, 335, 0), new Vector3(20, 0, 0), new Vector3(0, 0, 20), 10);
        m.Skin(rings[^1], cork, _ => Wood, new Vector3(0, 318, 0));
        m.Cap(cork, m.P(0, new Vector3(0, 340, 0)), Wood, new Vector3(0, 300, 0));
        return m.ToBody(header);
    }

    // A plate for the same: a shallow cream dish with a rim
    public static Body BuildPlate(byte[] header)
    {
        var m = new CarMesh("a plate", new[] { -1 });
        m.Light = 0.8f;
        var foot = m.Loop(0, new Vector3(0, 0, 0), new Vector3(70, 0, 0), new Vector3(0, 0, 70), 12);
        var rim = m.Loop(0, new Vector3(0, 30, 0), new Vector3(140, 0, 0), new Vector3(0, 0, 140), 12);
        m.Skin(foot, rim, _ => Cream, new Vector3(0, 15, 0));
        m.Cap(rim, m.P(0, new Vector3(0, 12, 0)), Cream, new Vector3(0, -40, 0));
        return m.ToBody(header);
    }

    // The candle's flame in the house at night (the user, 2026-11-07: "we should also make sure the candle is actually lit"): an orange
    // teardrop round a yellow core, unlit (its own colours whatever the light -- the night's dark_scene= dims the others), two shapes the
    // flame actor flickers between (RaceTrackStory.Evening.cs)
    public static Body BuildFlame(byte[] header) => FlameBody(header, 96, 0);
    public static Body BuildFlame2(byte[] header) => FlameBody(header, 80, 8);
    private static Body FlameBody(byte[] header, float tall, float lean)
    {
        var m = new CarMesh("a candle's flame", new[] { -1 });
        var low = m.Loop(0, new Vector3(0, 4, 0), new Vector3(13, 0, 0), new Vector3(0, 0, 13), 8);
        var mid = m.Loop(0, new Vector3(lean * 0.4f, tall * 0.35f, 0), new Vector3(22, 0, 0), new Vector3(0, 0, 22), 8);
        var tip = m.P(0, new Vector3(lean, tall, 0));
        for (var k = 0; k < 8; k++)
        {
            m.Out(new[] { low[k], low[(k + 1) % 8], mid[(k + 1) % 8], mid[k] }, FlameOuter, new Vector3(0, tall * 0.2f, 0), true);
        }
        m.Cap(mid, tip, FlameOuter, new Vector3(lean * 0.5f, tall * 0.5f, 0), true);
        m.Cap(low, m.P(0, new Vector3(0, -6, 0)), FlameOuter, new Vector3(0, 20, 0), true);
        var core = m.Loop(0, new Vector3(lean * 0.3f, tall * 0.25f, 0), new Vector3(10, 0, 0), new Vector3(0, 0, 10), 6);
        m.Cap(core, m.P(0, new Vector3(lean * 0.6f, tall * 0.62f, 0)), FlameCore, new Vector3(lean * 0.3f, tall * 0.1f, 0), true);
        m.Cap(core, m.P(0, new Vector3(0, 8, 0)), FlameCore, new Vector3(0, tall * 0.4f, 0), true);
        return m.ToBody(header);
    }

    // One of the Dean's exam pages lying on the Desert's road (the user, 2026-11-08: the pages were unseen places on the road): a sheet of
    // paper, curled up at two corners as the wind left it, with lines of writing across it, both sides showing
    public static Body BuildPage(byte[] header)
    {
        var m = new CarMesh("an exam page", new[] { -1 });
        m.Light = 0.95f;
        const float W = 230, D = 300;
        // (a 3 x 3 grid of the sheet's corners, lifted at the two curled corners)
        float Lift(int i, int j) => (i == 0 && j == 0) ? 40 : (i == 2 && j == 2) ? 26 : (i == 1 && j == 1) ? 4 : 0;
        var p = new int[3, 3];
        for (var i = 0; i < 3; i++)
            for (var j = 0; j < 3; j++)
                p[i, j] = m.P(0, new Vector3(-W + i * W, 6 + Lift(i, j), -D + j * D));
        for (var i = 0; i < 2; i++)
            for (var j = 0; j < 2; j++)
            {
                m.Both(new[] { p[i, j], p[i + 1, j], p[i + 1, j + 1] }, Paper);
                m.Both(new[] { p[i, j], p[i + 1, j + 1], p[i, j + 1] }, Paper);
            }
        // (the writing: thin dark strokes just over the sheet)
        for (var r = 0; r < 5; r++)
        {
            var z = -D + 70 + r * 62f;
            var x0 = -W + 40; var x1 = W - 40 - (r % 2) * 50;
            m.Both(new[] { m.P(0, new Vector3(x0, 12, z)), m.P(0, new Vector3(x1, 12, z)), m.P(0, new Vector3(x1, 12, z + 14)), m.P(0, new Vector3(x0, 12, z + 14)) }, Ink);
        }
        return m.ToBody(header);
    }

    // A wooden stool for the secret chamber (Zoe sits on it: CitadelSpanking.StoolTop high), a square seat on four legs
    public static Body BuildStool(byte[] header)
    {
        var m = new CarMesh("a stool", new[] { -1 });
        m.Light = 0.7f;
        const float Top = CitadelSpanking.StoolTop, Half = 150, Seat = 40, Leg = 22;
        m.Box(0, new Vector3(0, Top - Seat / 2, 0), new Vector3(2 * Half, Seat, 2 * Half), Wood);
        foreach (var (x, z) in new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })
            m.Box(0, new Vector3(x * (Half - Leg), (Top - Seat) / 2, z * (Half - Leg)), new Vector3(2 * Leg, Top - Seat, 2 * Leg), Rust);
        return m.ToBody(header);
    }

    // Into the game folder: the four bodies at the end of BODY.HQR, each Prop's generic body of its number in RESS.HQR's entity table -- once:
    // where the entity already has that body and it is this, it stays. Lines for the build's log.
    public static List<string> Install(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var bodies = HqrArchive.Open(bodyPath);
        var header = Body.Read(bodies.Read(HeaderBody), 2, allowStatic: true).Header;
        var entity = Lba2EntityTable.Load(gameDirectory)?.Entities.FirstOrDefault(e => e.Id == Prop);
        if (entity is null || !entity.Bodies.Any(b => b.Generic == 0 && b.Body == HeaderBody))
            return new() { $"WARNING: the chapter 2 props: entity {Prop} isn't the game's static prop (BODY.HQR {HeaderBody}) -- not made" };
        var table = HqrArchive.Open(ressPath).Read(44);
        var made = new List<string>();
        foreach (var (generic, name, build) in new (int, string, Func<byte[], Body>)[]
                 { (Tyre, "the old tyre", BuildTyre), (Umbrella, "the umbrella", BuildUmbrella), (Hook, "its hook", BuildHook), (Bait, "the tin of bait", BuildBait), (Bottle, "a bottle", BuildBottle), (Plate, "a plate", BuildPlate), (Flame, "a candle's flame", BuildFlame), (Flame2, "its flicker", BuildFlame2), (Page, "an exam page", BuildPage), (Stool, "a stool", BuildStool),
                   (Seal, "the Sendell seal", BuildSeal), (TapestryRed, "a red tapestry", BuildTapestryRed), (TapestryBlue, "a blue tapestry", BuildTapestryBlue),
                   (Picture, "a picture", BuildPicture), (Rack, "a stand of paddles and whips", BuildRack), (Chair, "a chair", BuildChair),
                   (Boulder, "a boulder", BuildBoulder), (Boulder2, "a smaller boulder", BuildBoulder2), (BikeTwinsen, "Twinsen's bike", BuildBikeTwinsen), (BikeZoe, "Zoe's bike", BuildBikeZoe), (JollyRoger, "a Jolly Roger", BuildJollyRoger), (SmallBoulder, "a small boulder", BuildSmallBoulder), (LiftPlatform, "the lift", BuildLiftPlatform), (HouseLiftPlatform, "the house's lift", BuildHouseLiftPlatform), (MoonLiftPlatform, "the moon's lift", BuildMoonLiftPlatform), (ClubWall, "Stella Entities' wall", BuildClubWall), (ClubCurb, "Stella Entities' curb", BuildClubCurb), (ClubStage, "Stella Entities' stage", BuildClubStage), (ClubColumn, "the moon's reactor column", BuildClubColumn), (ClubPole, "a dancer's pole", BuildClubPole), (ClubCounter, "Stella Entities' bar", h => BuildClubCounter(h, 115)), (ClubCounter2, "Stella Entities' bar", h => BuildClubCounter(h, 135)), (ClubCounter3, "Stella Entities' bar", h => BuildClubCounter(h, 155)), (ClubShelf, "Stella Entities' bottles", BuildClubShelf), (ClubTable, "Rick's card table", BuildClubTable), (ClubBarStool, "a bar stool", BuildClubBarStool), (ClubFront, "Stella Entities' name and doors", BuildClubFront), (ClubBoard, "Stella Entities' name's board", BuildClubBoard), (ClubSteps, "Stella Entities' steps", BuildClubSteps), (ClubBooth, "a booth's bench", BuildClubBooth), (ChangeMachine, "the change machine", BuildChangeMachine), (WannieGloveProp, "the Wannie's glove", BuildWannieGlove), (CarLift, "a car lift", BuildCarLift), (ClubStar, "a twinkling star", BuildClubStar), (ClubRail, "Stella Entities' stage rail", BuildClubRail), (ClubStarDim, "a twinkling star, dim", BuildClubStarDim), (Dartboard, "the Tavern's dartboard", BuildDartboard), (DartScoreBoard, "the Tavern's darts blackboard", BuildDartScoreBoard), (DartOche, "the Tavern's oche", BuildDartOche), (BowlingLanes, "the bowling lanes", BuildBowlingLanes), (PinChain, "a pin's helmet and chain", BuildPinChain), (LeverUp, "the giant lever", BuildLeverUp), (LeverDown, "the giant lever, pulled", BuildLeverDown), (PoolWater, "the hotel pool's water", BuildPoolWater), (DivingBoard, "the diving board", BuildDivingBoard), (RailScope, "the lighthouse's telescope", BuildRailScope), (Footprints, "the thief's painted footprints", BuildFootprints), (LiftRailBack, "the lift's rail", BuildLiftRailBack), (LiftRailLeft, "the lift's rail", BuildLiftRailLeft), (LiftRailRight, "the lift's rail", BuildLiftRailRight),
                   (CurtainLeft, "a curtain", BuildCurtainLeft), (CurtainRight, "a curtain", BuildCurtainRight) }
                 .Concat(PictureBodies())
                 .Concat(Enumerable.Range(0, BayeuxParts).Select(k => (Bayeux + k, $"the Bayeux hanging's part {k + 1}", (Func<byte[], Body>)(h => BuildBayeux(h, k))))))
        {
            var bytes = build(header).Write();
            var count = HqrArchive.CountEntries(bodyPath);
            var have = entity.Bodies.Where(b => b.Generic == generic).Select(b => (int?)b.Body).FirstOrDefault();
            int index;
            if (have is { } h && h < count && HqrArchive.Open(bodyPath).Read(h).AsSpan().SequenceEqual(bytes)) index = h;
            else
            {
                File.WriteAllBytes(bodyPath, HqrWriter.AppendEntry(File.ReadAllBytes(bodyPath), HqrWriter.StoredEntry(bytes)));
                index = count;
            }
            table = BoxOf(generic) is { } box ? WithBodyBox(table, Prop, generic, index, box) : RaceTrackBaldinoCar.WithBody(table, Prop, generic, index);
            made.Add($"{name} BODY.HQR {index} (body {generic})");
        }
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return new() { $"chapter 2's props, entity {Prop}'s bodies: {string.Join(", ", made)}" };
    }

    // More props made elsewhere (from the game folder's own bodies -- StellaPosters): appended to BODY.HQR and given to entity 17 as above
    public static List<string> InstallMore(string gameDirectory, string what, IEnumerable<(int Generic, string Name, Func<byte[], Body> Build)> more)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var header = Body.Read(HqrArchive.Open(bodyPath).Read(HeaderBody), 2, allowStatic: true).Header;
        if (Lba2EntityTable.Load(gameDirectory)?.Entities.FirstOrDefault(e => e.Id == Prop) is null)
            return new() { $"WARNING: {what}: no prop entity {Prop} -- not made" };
        var table = HqrArchive.Open(ressPath).Read(44);
        var made = new List<string>();
        foreach (var (generic, name, build) in more)
        {
            var count = HqrArchive.CountEntries(bodyPath);
            File.WriteAllBytes(bodyPath, HqrWriter.AppendEntry(File.ReadAllBytes(bodyPath), HqrWriter.StoredEntry(build(header).Write())));
            table = BoxOf(generic) is { } box ? WithBodyBox(table, Prop, generic, count, box) : RaceTrackBaldinoCar.WithBody(table, Prop, generic, count);
            made.Add($"{name} BODY.HQR {count} (body {generic})");
        }
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return new() { $"{what}, entity {Prop}'s bodies: {string.Join(", ", made)}" };
    }

    // The props whose box isn't the one their body's extent makes: the lift's platform (not its rails), the rugs' tiles (CitadelProps.Rugs)
    private static short[]? BoxOf(int generic) =>
        LiftBox(generic) ?? RugTileBox(generic) ?? ClubBox(generic) ?? PoolBox(generic) ?? HangarProps.Box(generic);

    // An entity's body record with its own box (the engine's ZV: F_BODY, the generic, the record's size, the body's number, "has
    // actions", F_ZV and the box's six values) in place of the one the body's own extent makes -- the chamber's lift: its rail and chains
    // over it, its box only the platform, so Twinsen stands on it, not in it, pushed off
    private static byte[] WithBodyBox(byte[] table, int entity, int generic, int bodyIndex, short[] box)
    {
        table = RaceTrackBaldinoCar.WithBody(table, entity, generic, bodyIndex);
        var start = BinaryPrimitives.ReadInt32LittleEndian(table.AsSpan(entity * 4));
        var end = BinaryPrimitives.ReadInt32LittleEndian(table.AsSpan((entity + 1) * 4));
        for (var p = start; p < end && table[p] != 255; p += table[p] == 3 ? 3 + table[p + 3] : 2 + table[p + 2])
        {
            if (table[p] != 1 || table[p + 1] != generic) continue;
            var record = new byte[19];
            record[0] = 1; record[1] = (byte)generic; record[2] = 17; record[3] = (byte)bodyIndex; record[4] = (byte)(bodyIndex >> 8);
            record[5] = 1; record[6] = 14;
            for (var k = 0; k < 6; k++) BinaryPrimitives.WriteInt16LittleEndian(record.AsSpan(7 + k * 2), box[k]);
            return Lba2EntityTable.Splice(table, entity, p, 2 + table[p + 2], record);
        }
        return table;
    }

    // A prop of these in a scene at (x, y, z) (scene units) turned `beta`: no collisions with anyone, no shadow (as the shop's holomap on its
    // counter), no scripts of its own yet; `settles`: it falls onto whatever is under it (the bricks' and the ground's: CHECK_BRICK_COL |
    // OBJ_FALLABLE) -- placed a little above where it lies, as the island's ground under the harbour's quay isn't the quay's paving
    public static SceneActorModel Actor(int generic, int x, int y, int z, int beta = 0, bool settles = false)
    {
        var a = SceneOps.BlankActor(SceneGame.Lba2, x, y, z, Prop);
        a.Body = generic; a.Beta = beta; a.Flags = settles ? 0x1802u : 0x1000u; a.Armor = 51; a.LifePoints = -1; a.CoulObj = RaceTrackStory.DefaultVoice;
        return a;
    }
}
