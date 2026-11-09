using System.IO;
using System.Numerics;
using LBAAssembler.Terrain.Palace;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The secret chamber's scene, home from the Tralu's bar (the user, 2026-11-07: Zoe "takes him down on the elevator and spanks him"; "Zoe
// should be sat down with Twinsen over her lap, spanking his bare ass and turning it red"). The build's side; the story's -- where each
// is, what is said, when -- is RaceTrackStory.SecretChamber:
//   - Zoe's two new animations (her entity, 15): sitting (ZoeSit) -- her hips down onto a stool, her thighs out level in front of her, her
//     shins down, her left hand on Twinsen's back -- and the spanking (ZoeSpank), her right hand up and down onto him, a loop;
//   - Twinsen's two (the hero's entity, 0): across her lap (TwinsenLap) -- tipped forward to lie face down, his head and arms hanging
//     down on her left, his legs on her right -- and the same with his legs kicking (TwinsenKick);
//   - his body with his trousers' seat bare (TwinsenBare), and red (TwinsenRed): his everyday body (BODY.HQR 0) with the back faces of
//     its hips' bone the skin's colour, then the red's;
//   - the stool she sits on (CitadelProps.Stool) and the slap's sound (SAMPLES.HQR's end: a short crack and a thump, made here).
// The poses are aimed bone by bone from the entity's own standing (its animation 0's first keyframe), as the palace's torturers and the
// flag marshal are (PalaceTorturers.Posed and Turn).
internal static class CitadelSpanking
{
    public const int ZoeEntity = 15, ZoeSit = 100, ZoeSpank = 101;
    public const int HeroEntity = 0, TwinsenLap = 240, TwinsenKick = 241, TwinsenBare = 30, TwinsenRed = 31, TwinsenPink = 32;
    // how far Zoe's hips come down to sit (her hips' bone at 633 standing, her thighs' tops 60 under it): the stool's top
    public const int SitDrop = 280, StoolTop = 633 - SitDrop - 60;
    // Twinsen's hips over her lap: her thighs out SitDrop under her standing hips, his hips' bone (533 standing) this far down
    // (raised, 2026-11-09: "his bare ass up in the air" -- his hips higher, his chest and legs further down either side of her lap)
    public const int LapDown = 55;
    private const int Skin = 32, Red = 66, Pink = 76;
    // his everyday clothes' trousers (the racing suit's lower half once CitadelOveralls has painted them: what he pulls down)
    private const int Trousers = 194;
    // the heads' bones (the back is aimed at its head)
    private const int ZoeHead = 12, TwinsenHead = 14;

    // Into the game folder: the bodies, the animations, the entities' tables, the slap. Returns the slap's SAMPLES.HQR number (-1 none).
    public static (List<string> Log, int Slap) Install(string gameDirectory)
    {
        var log = new List<string>();
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        var entities = Lba2EntityTable.Parse(table).Entities;
        var zoe = entities.FirstOrDefault(e => e.Id == ZoeEntity);
        var hero = entities.FirstOrDefault(e => e.Id == HeroEntity);
        if (zoe is null || hero is null) return (new() { "WARNING: the spanking: no Zoe or no Twinsen in the entity table -- none made" }, -1);
        var slap = Slap(gameDirectory);
        if (zoe.Anims.Any(a => a.Generic == ZoeSit)) return (new() { $"the spanking: made already (Zoe's animation {ZoeSit}); the slap SAMPLES.HQR {slap}" }, slap);

        var bodies = HqrArchive.Open(bodyPath);
        var anims = HqrArchive.Open(animPath);
        var zoeBody = Body.Read(bodies.Read(RaceTrackGuards.BodyOf(table, ZoeEntity, 0) ?? 26), 2);
        var twinsenBody = Body.Read(bodies.Read(RaceTrackGuards.BodyOf(table, HeroEntity, 0) ?? 0), 2);
        var zoeBasis = PalaceTorturers.ReadFrames(anims.Read(zoe.Anims.First(a => a.Generic == 0).Anim), zoeBody.Bones.Count)[0].Bones;
        var heroBasis = PalaceTorturers.ReadFrames(anims.Read(hero.Anims.First(a => a.Generic == 0).Anim), twinsenBody.Bones.Count)[0].Bones;

        // ---- Zoe: sitting, and the spanking
        static Vector3 N(float x, float y, float z) => Vector3.Normalize(new Vector3(x, y, z));
        var zoeSeat = new Dictionary<int, Vector3>
        {
            [3] = N(0, 1, 0.12f),                                   // her back, upright, leant a little over him
            [8] = N(0.12f, -0.05f, 1), [9] = N(0.05f, -1, 0.1f),    // her right thigh out level, the shin down
            [10] = N(-0.12f, -0.05f, 1), [11] = N(-0.05f, -1, 0.1f),
            [6] = N(-0.15f, -0.75f, 0.65f), [7] = N(0.45f, -0.35f, 0.82f),   // her left hand on his back
        };
        Dictionary<int, Vector3> With(Dictionary<int, Vector3> pose, params (int Bone, Vector3 Way)[] more)
        {
            var d = new Dictionary<int, Vector3>(pose);
            foreach (var (b, w) in more) d[b] = w;
            return d;
        }
        var restArm = With(zoeSeat, (4, N(0.2f, -0.85f, 0.45f)), (5, N(-0.2f, -0.6f, 0.75f)));
        var armUp = With(zoeSeat, (4, N(0.35f, 0.55f, 0.6f)), (5, N(0.05f, 0.95f, 0.3f)));
        var armDown = With(zoeSeat, (4, N(0.1f, -0.7f, 0.7f)), (5, N(-0.35f, -0.55f, 0.75f)));
        var sit = new Anim { Game = 2, LoopFrame = 0 };
        sit.Frames.Add(Frame(zoeBody, 500, zoeBasis, restArm, -SitDrop, ZoeHead));
        sit.Frames.Add(Frame(zoeBody, 900, zoeBasis, With(restArm, (3, N(0, 1, 0.16f))), -SitDrop, ZoeHead));
        var spank = new Anim { Game = 2, LoopFrame = 0 };
        spank.Frames.Add(Frame(zoeBody, 180, zoeBasis, armUp, -SitDrop, ZoeHead));
        spank.Frames.Add(Frame(zoeBody, 110, zoeBasis, armDown, -SitDrop, ZoeHead));

        // ---- Twinsen: across her lap, and kicking
        var lap = new Dictionary<int, Vector3>
        {
            [2] = N(0, -0.05f, 1),                                  // his hips tipped forward: lying face down along his own forward
            [3] = N(0, -0.8f, 0.6f),                                // his chest and head hanging well down on her left
            [8] = N(0, -0.62f, -0.78f), [9] = N(0, -1, -0.08f),     // his legs down over her right, the shins hanging: his bottom up
            [11] = N(0, -0.62f, -0.78f), [12] = N(0, -1, -0.08f),
            [4] = N(0.1f, -1, 0.15f), [6] = N(-0.1f, -1, 0.15f),    // his arms hanging
        };
        var kickUp = new Dictionary<int, Vector3>(lap) { [9] = N(0, -0.2f, -0.98f), [12] = N(0, -1, -0.1f) };
        var kickDown = new Dictionary<int, Vector3>(lap) { [9] = N(0, -1, -0.1f), [12] = N(0, -0.2f, -0.98f) };
        var lie = new Anim { Game = 2, LoopFrame = 0 };
        lie.Frames.Add(Frame(twinsenBody, 600, heroBasis, lap, -LapDown, TwinsenHead));
        lie.Frames.Add(Frame(twinsenBody, 800, heroBasis, new Dictionary<int, Vector3>(lap) { [3] = N(0, -0.75f, 0.66f) }, -LapDown, TwinsenHead));
        var kick = new Anim { Game = 2, LoopFrame = 0 };
        kick.Frames.Add(Frame(twinsenBody, 150, heroBasis, kickUp, -LapDown, TwinsenHead));
        kick.Frames.Add(Frame(twinsenBody, 150, heroBasis, kickDown, -LapDown, TwinsenHead));

        // ---- his racing suit pulled down to his knees (the user, 2026-11-09: "with his trousers down and his bare ass up in the air which
        // turns red as she spanks him"; the racing suit only -- "the suit is required ... to access the Tralu bar area"): his everyday
        // body painted as the suit (Citadel/CitadelOveralls), then its lower half's polygons -- the trousers' -- bare: his thighs and his
        // hips skin, his bottom (their backs) `colour`; the shins keep the suit, bunched at the knees
        Body Seat(int colour)
        {
            var b = Body.Read(bodies.Read(RaceTrackGuards.BodyOf(table, HeroEntity, 0) ?? 0), 2);
            var trousers = b.Faces.Select(f => f.Colour == Trousers).ToArray();
            CitadelOveralls.Overalls(b);
            var world = b.World();
            int BoneOf(int p) => b.Bones.FindIndex(x => p >= x.Start && p < x.Start + x.Count);
            for (var i = 0; i < b.Faces.Count; i++)
            {
                var f = b.Faces[i];
                if (!trousers[i] || f.Points.Length == 0) continue;
                var bone = BoneOf(f.Points[0]);
                if (bone is not (2 or 3 or 8 or 11)) continue;
                var middle = f.Points.Aggregate(Vector3.Zero, (s, p) => s + world[p]) / f.Points.Length;
                var bottom = bone is 2 or 3 && middle.Z < 20;
                b.Faces[i] = f with { Colour = bottom ? colour : Skin, Material = -1, Texture = null };
            }
            return b;
        }

        Frames.Clear();
        foreach (var (name, b, an) in new[] { ("sit", zoeBody, sit), ("spank", zoeBody, spank), ("lap", twinsenBody, lie), ("kick", twinsenBody, kick) })
            for (var k = 0; k < an.Frames.Count; k++) Frames.Add(($"{name}{k}", b, an.Frames[k]));
        var bodyIndex = HqrArchive.CountEntries(bodyPath);
        var file = File.ReadAllBytes(bodyPath);
        file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(Seat(Skin).Write()));
        file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(Seat(Red).Write()));
        file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(Seat(Pink).Write()));
        File.WriteAllBytes(bodyPath, file);
        var animIndex = HqrArchive.CountEntries(animPath);
        var animFile = File.ReadAllBytes(animPath);
        foreach (var a in new[] { sit, spank, lie, kick }) animFile = HqrWriter.AppendEntry(animFile, HqrWriter.StoredEntry(a.Write()));
        File.WriteAllBytes(animPath, animFile);
        table = RaceTrackBaldinoCar.WithBody(table, HeroEntity, TwinsenBare, bodyIndex);
        table = RaceTrackBaldinoCar.WithBody(table, HeroEntity, TwinsenRed, bodyIndex + 1);
        table = RaceTrackBaldinoCar.WithBody(table, HeroEntity, TwinsenPink, bodyIndex + 2);
        table = RaceTrackJumpAnim.WithAnim(table, ZoeEntity, ZoeSit, animIndex);
        table = RaceTrackJumpAnim.WithAnim(table, ZoeEntity, ZoeSpank, animIndex + 1);
        table = RaceTrackJumpAnim.WithAnim(table, HeroEntity, TwinsenLap, animIndex + 2);
        table = RaceTrackJumpAnim.WithAnim(table, HeroEntity, TwinsenKick, animIndex + 3);
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        log.Add($"the spanking: Zoe sitting and spanking (entity {ZoeEntity}'s animations {ZoeSit}, {ZoeSpank}: ANIM.HQR {animIndex}, {animIndex + 1}), " +
                $"Twinsen across her lap and kicking ({TwinsenLap}, {TwinsenKick}: {animIndex + 2}, {animIndex + 3}), his seat bare and red (bodies {TwinsenBare}, " +
                $"{TwinsenRed}: BODY.HQR {bodyIndex}, {bodyIndex + 1}); the slap SAMPLES.HQR {slap}");
        return (log, slap);
    }

    // (the poses made by the last Install, for looking at outside the game: the tool's spankposes)
    internal static readonly List<(string Name, Body Body, AnimFrame Frame)> Frames = new();

    // A keyframe: the standing's values, the hips' move down by `drop`, then each bone in `aims` turned to point its own way (from its
    // pivot to its first child's) along the given direction -- parents before children
    private static AnimFrame Frame(Body body, int time, (int Type, double A, double B, double C)[] basis, Dictionary<int, Vector3> aims, int drop, int head)
    {
        var v = basis.ToArray();
        v[1] = (1, 0, drop, 0);
        var rest = body.World();
        int Depth(int b) { var d = 0; while (b > 0) { b = body.Bones[b].Parent; d++; } return d; }
        foreach (var bone in aims.Keys.OrderBy(Depth))
        {
            // (the back is aimed at the head, not at its first child, an upper arm)
            var child = bone == 3 ? head : Enumerable.Range(0, body.Bones.Count).FirstOrDefault(c => c > 0 && body.Bones[c].Parent == bone);
            if (child <= 0) continue;
            var from = rest[body.Bones[child].Pivot] - rest[body.Bones[bone].Pivot];
            if (from.LengthSquared() < 1) continue;
            var (_, matrices) = PalaceTorturers.Posed(body, v);
            v[bone] = PalaceTorturers.Turn(matrices[body.Bones[bone].Parent], from, aims[bone]);
        }
        return new AnimFrame(time, 0, 0, 0, v.Select((b, i) => i == 0 ? AnimBone.Neutral
            : b.Type == 1 ? new AnimBone(1, (float)b.A, (float)b.B, (float)b.C)
            : new AnimBone(0, (float)(b.A / (2 * Math.PI)), (float)(b.B / (2 * Math.PI)), (float)(b.C / (2 * Math.PI)))).ToArray());
    }

    // The slap: a crack of noise falling away in a few hundredths of a second over a low thump -- 8-bit mono WAV at 22,050 Hz, put at
    // SAMPLES.HQR's end (where it is already, there)
    private static int Slap(string gameDirectory)
    {
        const int Rate = 22050;
        var n = Rate * 18 / 100;
        var rnd = new Random(7);
        var pcm = new byte[n];
        for (var i = 0; i < n; i++)
        {
            var t = i / (double)Rate;
            var crack = (rnd.NextDouble() * 2 - 1) * Math.Exp(-t / 0.012);
            var thump = Math.Sin(2 * Math.PI * 170 * t) * Math.Exp(-t / 0.035) * 0.6;
            var s = Math.Clamp((crack * 0.85 + thump) * 0.9, -1, 1);
            pcm[i] = (byte)Math.Round(128 + s * 127);
        }
        var wav = new MemoryStream();
        var w = new BinaryWriter(wav);
        w.Write("RIFF"u8); w.Write(36 + n); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate); w.Write((short)1); w.Write((short)8);
        w.Write("data"u8); w.Write(n); w.Write(pcm);
        var bytes = wav.ToArray();
        var path = Path.Combine(gameDirectory, "SAMPLES.HQR");
        var count = HqrArchive.CountEntries(path);
        var samples = HqrArchive.Open(path);
        for (var k = Math.Max(0, count - 4); k < count; k++)
            if (samples.Read(k).AsSpan().SequenceEqual(bytes)) return k;
        HqrWriter.AppendToFile(path, bytes);
        return count;
    }
}
