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
internal static class CitadelProps
{
    public const int Prop = 17, Tyre = 1, Umbrella = 2, Hook = 3, Bait = 4;
    // (the palette's ramps: 48 the greys from black, 56 a steel grey, 18 rust brown, 20 wood, 66 red, 130 green, 76 pink)
    private const int Rubber = 48, Steel = 56, Rust = 18, Wood = 20, Red = 66, Green = 130, Worm = 76;
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

    // The tin of fishing bait: a steel tin with a green label round it, its lid open on a few pink worms
    public static Body BuildBait(byte[] header)
    {
        var m = new CarMesh("the tin of bait", new[] { -1 });
        m.Light = 0.6f;
        var levels = new[] { 0f, 35, 115, 150 };
        var rings = levels.Select(y => m.Loop(0, new Vector3(0, y, 0), new Vector3(95, 0, 0), new Vector3(0, 0, 95), 10)).ToArray();
        for (var j = 0; j + 1 < rings.Length; j++) m.Skin(rings[j], rings[j + 1], _ => j == 1 ? Green : Steel, new Vector3(0, (levels[j] + levels[j + 1]) / 2, 0));
        m.Cap(rings[^1], m.P(0, new Vector3(0, 142, 0)), Rust, new Vector3(0, 80, 0));
        m.Light = 1;
        foreach (var (x, z) in new[] { (-35f, 10f), (20f, -28f), (25f, 32f) }) m.Ball(0, new Vector3(x, 160, z), 22, Worm);
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
                 { (Tyre, "the old tyre", BuildTyre), (Umbrella, "the umbrella", BuildUmbrella), (Hook, "its hook", BuildHook), (Bait, "the tin of bait", BuildBait) })
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
            table = RaceTrackBaldinoCar.WithBody(table, Prop, generic, index);
            made.Add($"{name} BODY.HQR {index} (body {generic})");
        }
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return new() { $"chapter 2's props, entity {Prop}'s bodies: {string.Join(", ", made)}" };
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
