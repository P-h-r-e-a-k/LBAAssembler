using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// Twinsen on a bike (the user, 2026-11-24: "Let's keep bikes unpurchasable for now, but let's do some work to allow Twinsen to cycle as
// this would fit in with the velodrome we've developed, we may look at allowing a bike to be purchased in the future"): a behaviour of
// the hero's of its own in the engine (C_VELO, COMMON.H), whose entity this makes -- his own entity's (0) records, each of his bodies
// with a track bike under him, and animations of his on it:
//   - a body: his own (each of entity 0's: his everyday clothes, his tunic, the racing suit, the pyjamas ...) with two more bones, the
//     bike's frame (from his root) and its cranks (from the frame, turning at the bottom bracket) -- the bike sized to his legs: its
//     saddle under his hips as he stands, so nothing of him but his limbs moves;
//   - the animations: pedalling (his walk's place, GEN_ANIM_MARCHE: the cranks a turn a cycle, each leg reaching its pedal -- thigh,
//     shin and foot turned about x alone, a two-bone reach -- the torso down over the bars, his arms to them, his head up, and the bike
//     going forward); backing it up slowly (GEN_ANIM_RECULE); still on it, the right pedal forward (everything else).
// One set of animations for all his bodies: their legs are the same bones and near enough the same shape (BODY.HQR 0's thigh and shin
// 200 + 266, 1's 166 + 220, 0.75 each) that the same angles reach each one's pedals, the bike being his legs' size.
// The engine (RACEMOD.CPP bike=) puts him on it when the story's BikeVar is 1 and off it at 0 (RaceTrackStory.Velodrome: the Citadel
// Comets' spare bike, lent by their coach in the Dome of the Slate).
internal static class TwinsenBike
{
    // (a story variable of its own: 280-292 are the reruns' disqualified cars, 293 the demo's -- reference_game_variables_wide)
    public const int BikeVar = 294;
    public const string FileName = "TWINBIKE.TXT";
    private const int HeroEntity = 0;
    // his bones (19, the same in each of his bodies), and the bike's two after them
    private const int Root = 0, Hips = 2, Chest = 3, RightArm = 4, LeftArm = 6, RightThigh = 8, RightShin = 9, RightFoot = 10,
                      LeftThigh = 11, LeftShin = 12, LeftFoot = 13, Head = 14, HisBones = 19, Frame = 19, Cranks = 20;
    // the pose: the torso's lean forward from his hips (turns about x, + the top forward), the head's back up, the arms forward and down
    // from straight down (world), the cranks' length and the bracket's place as shares of his leg's length, the pedalling's keyframes
    private const float Lean = 0.32f, HeadBack = 0.26f, ArmForward = 0.95f;
    private const float CrankShare = 0.2f, BracketDown = 0.72f, BracketForward = 0.2f;
    private const int Steps = 8;
    // the reference leg (BODY.HQR 1, the tunic's): the angles worked out for it
    private const int ReferenceBody = 1;
    // the bike's colours (ramp starts): the Citadel Comets' red frame, white bands, dark tyres and saddle, steel
    private const int FrameRed = 4 * 16, White = 3 * 16 + 14, Tyre = 48, Steel = 56, Dark = 49;
    // its speed: his run's (2,965 units a second) and a half again -- the pedalling's cycle, the distance a cycle
    private const int CycleMs = 640;
    private const float CycleDistance = 2.9f;

    // Into the game folder after his other bodies (the overalls, the pyjamas: RaceTrackService) -- the entity's number in TWINBIKE.TXT
    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        var hero = Lba2EntityTable.Parse(table).Entities.FirstOrDefault(e => e.Id == HeroEntity);
        if (hero is null) { log.Add("Twinsen's bike left out: no hero entity"); return log; }
        var bodies = HqrArchive.Open(bodyPath);
        var reference = Body.Read(bodies.Read(ReferenceBody), 2);
        if (!Fits(reference)) { log.Add("Twinsen's bike left out: his tunic's body isn't the game's"); return log; }
        var pose = Pose.Of(reference);
        // the animations: appended to ANIM.HQR
        var anims = new[] { ("still", Still(pose)), ("pedalling", Pedalling(pose)), ("backing", Backing(pose)) };
        var animIndex = new int[anims.Length];
        var animBytes = File.ReadAllBytes(animPath);
        for (var k = 0; k < anims.Length; k++)
        {
            animIndex[k] = CountOf(animBytes);
            animBytes = HqrWriter.AppendEntry(animBytes, HqrWriter.StoredEntry(anims[k].Item2.Write()));
        }
        File.WriteAllBytes(animPath, animBytes);
        // the bodies: each of his a bike's, once (his generic numbers share bodies)
        var bodyBytes = File.ReadAllBytes(bodyPath);
        var made = new Dictionary<int, int>();
        foreach (var (_, index) in hero.Bodies)
        {
            if (made.ContainsKey(index)) continue;
            Body his;
            try { his = Body.Read(bodies.Read(index), 2); } catch (Exception bad) when (bad is InvalidDataException or IndexOutOfRangeException) { continue; }
            if (!Fits(his)) continue;
            var riding = OnBike(his, pose);
            made[index] = CountOf(bodyBytes);
            bodyBytes = HqrWriter.AppendEntry(bodyBytes, HqrWriter.StoredEntry(riding.Write()));
        }
        File.WriteAllBytes(bodyPath, bodyBytes);
        if (made.Count == 0) { log.Add("Twinsen's bike left out: none of his bodies is the game's shape"); return log; }
        var fallback = made.TryGetValue(ReferenceBody, out var r) ? r : made.Values.First();
        // the entity: entity 0's records -- each body record his bike's body, each animation still but the pedalling and the backing
        var e = BinaryPrimitives.ReadInt32LittleEndian(table) / 4 - 1;
        var records = new List<byte>();
        foreach (var rec in Lba2EntityTable.Records(table, HeroEntity))
        {
            var bytes = table.AsSpan(rec.At, rec.Size).ToArray();
            if (rec.Command == 1)
            {
                var index = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(3));
                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(3), (short)(made.TryGetValue(index, out var b) ? b : fallback));
                records.AddRange(bytes);
            }
            else if (rec.Command == 3)
            {
                var anim = rec.Generic == 1 ? animIndex[1] : rec.Generic == 2 ? animIndex[2] : animIndex[0];
                // (no footsteps: the record's own, its sounds, left behind)
                records.AddRange(new byte[] { 3, (byte)rec.Generic, (byte)(rec.Generic >> 8), 4, (byte)anim, (byte)(anim >> 8), 0 });
            }
            else records.AddRange(bytes);
        }
        records.Add(255);
        table = AppendEntity(table, records.ToArray());
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        File.WriteAllText(Path.Combine(gameDirectory, FileName), $"# Twinsen on a bike: his riding behaviour's entity (LBA Assembler, Citadel/TwinsenBike.cs)\nentity {e}\n");
        log.Add($"Twinsen's bike: entity {e} (his own entity's records), {made.Count} bodies of his on it ({string.Join(", ", made.Select(m => $"{m.Key} as {m.Value}"))}), animations {string.Join(", ", anims.Select((a, k) => $"{a.Item1} {animIndex[k]}"))}");
        return log;
    }

    // an HQR's entries (its offsets' table less its end's: HqrArchive.CountEntries)
    private static int CountOf(byte[] hqr) => hqr.Length < 4 ? 0 : Math.Max(0, (int)(BinaryPrimitives.ReadUInt32LittleEndian(hqr) / 4) - 1);

    // The entity's number TWINBIKE.TXT keeps (-1: none built)
    public static int EntityOf(string gameDirectory)
    {
        var path = Path.Combine(gameDirectory, FileName);
        if (!File.Exists(path)) return -1;
        foreach (var line in File.ReadAllLines(path))
            if (line.StartsWith("entity ") && int.TryParse(line[7..].Trim(), out var e)) return e;
        return -1;
    }

    // RESS.HQR 44 with one more entity, its records these
    private static byte[] AppendEntity(byte[] table, byte[] records)
    {
        var count = BinaryPrimitives.ReadInt32LittleEndian(table) / 4 - 1;
        var offsets = Enumerable.Range(0, count + 1).Select(i => BinaryPrimitives.ReadInt32LittleEndian(table.AsSpan(i * 4))).ToArray();
        var data = table.AsSpan(offsets[0], offsets[count] - offsets[0]).ToArray();
        var result = new byte[(count + 2) * 4 + data.Length + records.Length];
        for (var i = 0; i <= count; i++) BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(i * 4), offsets[i] + 4);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan((count + 1) * 4), offsets[count] + 4 + records.Length);
        data.CopyTo(result.AsSpan((count + 2) * 4));
        records.CopyTo(result.AsSpan((count + 2) * 4 + data.Length));
        return result;
    }

    // His bones as the game's: 19, the legs and arms where they are
    private static bool Fits(Body b) =>
        b.Bones.Count == HisBones && b.Bones[RightThigh].Parent == Hips && b.Bones[RightShin].Parent == RightThigh && b.Bones[RightFoot].Parent == RightShin
        && b.Bones[LeftThigh].Parent == Hips && b.Bones[LeftShin].Parent == LeftThigh && b.Bones[LeftFoot].Parent == LeftShin
        && b.Bones[Chest].Parent == Hips && b.Bones[RightArm].Parent == Chest && b.Bones[LeftArm].Parent == Chest && b.Bones[Head].Parent == Chest;

    // The pose's numbers, from a body: his hips, legs and arms as he stands, and the bike's places from them
    private sealed record Pose(Vector3 Hip, float Thigh, float Shin, float RestThigh, float RestShin, Vector3 Bracket, float Crank, float AnkleUp,
                               float Leg, Vector3 ChestPivot, Vector3 Shoulder, Vector3 Hand)
    {
        public static Pose Of(Body b)
        {
            var w = b.World();
            Vector3 Piv(int bone) => w[b.Bones[bone].Pivot];
            Vector3 hip = Piv(RightThigh), knee = Piv(RightShin), ankle = Piv(RightFoot);
            float thigh = Vector3.Distance(hip, knee), shin = Vector3.Distance(knee, ankle), leg = thigh + shin;
            var foot = Enumerable.Range(b.Bones[RightFoot].Start, b.Bones[RightFoot].Count).Select(i => w[i]).ToList();
            var sole = foot.Count > 0 ? foot.Min(v => v.Y) : 0;
            var bracket = new Vector3(0, hip.Y - BracketDown * leg, hip.Z + BracketForward * leg);
            var hand = Enumerable.Range(b.Bones[RightArm + 1].Start, b.Bones[RightArm + 1].Count).Select(i => w[i]).Aggregate(Vector3.Zero, (s, v) => s + v) / Math.Max(1, b.Bones[RightArm + 1].Count);
            return new Pose(hip, thigh, shin, Phi(knee - hip), Phi(ankle - knee), bracket, CrankShare * leg, ankle.Y - sole, leg, Piv(Chest), Piv(RightArm), hand);
        }
    }

    // the angle of a direction in (y, z) from +y towards +z: a turn about x of a adds a to it (the engine's: TempleMarshal's LiePitch)
    private static float Phi(Vector3 v) => MathF.Atan2(v.Z, v.Y);
    private static float Turns(float radians) => radians / MathF.Tau;

    // the pedal's place at a step of the cranks' turn, a side's (the right forward at step 0, going forward over the top -- the cranks
    // turning back about x as the wheels do going forward)
    private static float CrankAngle(int step) => -step * MathF.Tau / Steps;
    private static Vector3 Pedal(Pose p, float angle, float side) =>
        p.Bracket + new Vector3(0, p.Crank * MathF.Sin(angle + (side > 0 ? 0 : MathF.PI)), p.Crank * MathF.Cos(angle + (side > 0 ? 0 : MathF.PI)));

    // A leg's turns reaching a pedal: the thigh's (from his hips), the shin's (from the thigh), the foot's (kept level)
    private static (float Thigh, float Shin, float Foot) Reach(Pose p, Vector3 pedal)
    {
        // (the ankle over the pedal by the foot's height, the hip in its own plane: y and z alone)
        var target = new Vector2(pedal.Z - p.Hip.Z, pedal.Y + p.AnkleUp - p.Hip.Y);
        var d = Math.Clamp(target.Length(), Math.Abs(p.Thigh - p.Shin) + 1, p.Thigh + p.Shin - 1);
        var u = Vector2.Normalize(target);
        var along = (p.Thigh * p.Thigh - p.Shin * p.Shin + d * d) / (2 * d);
        var h = MathF.Sqrt(MathF.Max(0, p.Thigh * p.Thigh - along * along));
        // (the knee forward: the perpendicular towards +z)
        var perp = new Vector2(-u.Y, u.X);
        if (perp.X < 0) perp = -perp;
        var knee = u * along + perp * h;
        var ankle = u * d;
        float thighPhi = MathF.Atan2(knee.X, knee.Y), shinPhi = MathF.Atan2(ankle.X - knee.X, ankle.Y - knee.Y);
        float dThigh = thighPhi - p.RestThigh, dShin = shinPhi - p.RestShin;
        return (Turns(dThigh), Turns(dShin - dThigh), Turns(-dShin));
    }

    // A keyframe's bones: the legs to the pedals at a crank angle, the rest of the pose
    private static AnimBone[] Bones(Pose p, float crank)
    {
        var bones = Enumerable.Range(0, HisBones + 2).Select(_ => AnimBone.Neutral).ToArray();
        var right = Reach(p, Pedal(p, crank, 1));
        var left = Reach(p, Pedal(p, crank, -1));
        bones[RightThigh] = new AnimBone(0, right.Thigh, 0, 0);
        bones[RightShin] = new AnimBone(0, right.Shin, 0, 0);
        bones[RightFoot] = new AnimBone(0, right.Foot, 0, 0);
        bones[LeftThigh] = new AnimBone(0, left.Thigh, 0, 0);
        bones[LeftShin] = new AnimBone(0, left.Shin, 0, 0);
        bones[LeftFoot] = new AnimBone(0, left.Foot, 0, 0);
        bones[Chest] = new AnimBone(0, Turns(Lean), 0, 0);
        bones[Head] = new AnimBone(0, Turns(-Lean * HeadBack / 0.26f * 0.8f), 0, 0);
        // (the arms from hanging straight down to forward and down: their turn from the chest's -- ArmForward from down, world)
        var arm = Turns(-ArmForward - Lean);
        bones[RightArm] = new AnimBone(0, arm, 0, 0);
        bones[LeftArm] = new AnimBone(0, arm, 0, 0);
        // (the cranks: a turn about x from where the body has them -- the right pedal forward)
        bones[Cranks] = new AnimBone(0, Turns(crank), 0, 0);
        return bones;
    }

    private static Anim Pedalling(Pose p)
    {
        var anim = new Anim { Game = 2, LoopFrame = 0 };
        var step = CycleDistance * CycleMs / Steps;
        for (var k = 0; k < Steps; k++) anim.Frames.Add(new AnimFrame(CycleMs / Steps, 0, 0, step, Bones(p, CrankAngle(k))));
        return anim;
    }

    private static Anim Backing(Pose p)
    {
        // (walked back slowly, the pedals still, the feet on them)
        var anim = new Anim { Game = 2, LoopFrame = 0 };
        for (var k = 0; k < 4; k++) anim.Frames.Add(new AnimFrame(200, 0, 0, -160, Bones(p, 0)));
        return anim;
    }

    private static Anim Still(Pose p)
    {
        var anim = new Anim { Game = 2, LoopFrame = 0 };
        anim.Frames.Add(new AnimFrame(500, 0, 0, 0, Bones(p, 0)));
        anim.Frames.Add(new AnimFrame(500, 0, 0, 0, Bones(p, 0)));
        return anim;
    }

    // ---- the body: his, the bike's frame and cranks added as bones of their own -------------------------------------------------------

    private static Body OnBike(Body his, Pose reference)
    {
        var own = Pose.Of(his);
        // (the bike to his legs: the reference's numbers scaled by his leg against the reference's, from his own hips)
        var k = own.Leg / reference.Leg;
        var bike = BuildBike(his.Header, own, reference, k);
        return Merge(his, bike);
    }

    // The bike, its frame bone 0 and its cranks bone 1 (turning at the bracket, its pivot), in his body's coordinates
    private static Body BuildBike(byte[] header, Pose own, Pose reference, float k)
    {
        var m = new CarMesh("Twinsen's track bike", new[] { -1, 0 }) { Rounded = true, Limit = 1100 };
        m.Light = 1f;
        var hip = own.Hip with { X = 0 };
        Vector3 bracket = own.Bracket;
        float r = 0.5f * own.Leg, crank = own.Crank;
        Vector3 rear = new(0, r, hip.Z - 0.8f * own.Leg), front = new(0, r, hip.Z + 1.12f * own.Leg);
        // (the bars where his hands come to: the arm turned forward from hanging, after the lean -- worked out as the animation does)
        var hand = HandAt(own);
        var bars = hand with { X = 0 };
        Vector3 seat = new(0, hip.Y - 0.13f * own.Leg, hip.Z - 0.12f * own.Leg), head = new(0, bars.Y - 0.05f * own.Leg, bars.Z - 0.18f * own.Leg);
        Vector3 headLow = new(0, r + 0.32f * own.Leg, head.Z + 0.04f * own.Leg);
        void Bar(Vector3 a, Vector3 b, float t, int c) => m.Strut(0, a, b, t * k, c);
        var pivot = m.P(0, bracket);
        // the wheels: a tyre each, a deep rim, spokes as lines
        foreach (var hub in new[] { rear, front })
        {
            const int N = 14;
            var outer = m.Loop(0, hub, new Vector3(0, r, 0), new Vector3(0, 0, r), N);
            var inner = m.Loop(0, hub, new Vector3(0, r - 22 * k, 0), new Vector3(0, 0, r - 22 * k), N);
            var outerL = m.Loop(0, hub + new Vector3(14 * k, 0, 0), new Vector3(0, r - 8 * k, 0), new Vector3(0, 0, r - 8 * k), N);
            var outerR = m.Loop(0, hub - new Vector3(14 * k, 0, 0), new Vector3(0, r - 8 * k, 0), new Vector3(0, 0, r - 8 * k), N);
            for (var i = 0; i < N; i++)
            {
                var j = (i + 1) % N;
                m.Out(new[] { outer[i], outer[j], outerL[j], outerL[i] }, Tyre, hub);
                m.Out(new[] { outer[j], outer[i], outerR[i], outerR[j] }, Tyre, hub);
                m.Both(new[] { outerL[i], outerL[j], inner[j], inner[i] }, White);
            }
            var h = m.P(0, hub);
            for (var i = 0; i < N; i += 2) m.Line(h, inner[i], Steel);
        }
        // the frame
        Bar(bracket, seat, 30, FrameRed);
        Bar(seat, head, 28, FrameRed);
        Bar(bracket, headLow, 32, FrameRed);
        Bar(bracket, rear, 20, FrameRed);
        Bar(seat, rear, 18, FrameRed);
        Bar(headLow, front, 22, White);
        Bar(head, headLow, 30, FrameRed);
        // the saddle on its post, the stem, the bars across with their grips
        var saddleTop = new Vector3(0, hip.Y - 0.035f * own.Leg, hip.Z - 0.05f * own.Leg);
        Bar(seat, saddleTop - new Vector3(0, 12 * k, 0), 18, Steel);
        m.Box(0, saddleTop - new Vector3(0, 14 * k, 0), new Vector3(70 * k, 26 * k, 190 * k), Dark);
        Bar(head, bars - new Vector3(0, 10 * k, 0), 20, Steel);
        var half = MathF.Abs(hand.X) + 30 * k;
        Bar(new Vector3(-half, bars.Y, bars.Z), new Vector3(half, bars.Y, bars.Z), 18, Steel);
        foreach (var s in new[] { -1f, 1f }) Bar(new Vector3(s * (half - 60 * k), bars.Y, bars.Z), new Vector3(s * half, bars.Y, bars.Z), 26, Dark);
        // the cranks (bone 1, turning at the bracket): the chainring on the right, each crank with its pedal -- the right forward
        m.Pivot(1, pivot);
        m.Light = 0.8f;
        var ring = m.Loop(1, bracket + new Vector3(60 * k, 0, 0), new Vector3(0, 70 * k, 0), new Vector3(0, 0, 70 * k), 10);
        var mid = m.P(1, bracket + new Vector3(64 * k, 0, 0));
        m.Cap(ring, mid, Steel, bracket);
        m.Light = 1f;
        foreach (var s in new[] { 1f, -1f })
        {
            var pedal = bracket + new Vector3(s * 85 * k, 0, s * crank);
            m.Strut(1, bracket + new Vector3(s * 70 * k, 0, 0), pedal - new Vector3(s * 8 * k, 0, 0), 16 * k, Steel);
            m.Box(1, pedal + new Vector3(s * 18 * k, 0, 0), new Vector3(46 * k, 14 * k, 64 * k), Dark);
        }
        return m.ToBody(header);
    }

    // where his right hand is with the pose's lean and arm (the animation's own turns, worked forward)
    private static Vector3 HandAt(Pose p)
    {
        Vector3 TurnX(Vector3 v, Vector3 about, float a)
        {
            var d = v - about;
            float c = MathF.Cos(a), s = MathF.Sin(a);
            return about + new Vector3(d.X, d.Y * c - d.Z * s, d.Y * s + d.Z * c);
        }
        var shoulder = TurnX(p.Shoulder, p.ChestPivot, Lean);
        var hand = TurnX(p.Hand, p.ChestPivot, Lean);
        return TurnX(hand, shoulder, -ArmForward - Lean);
    }

    // His body with the bike's bones after his: the frame's from his root (its pivot his root's own point), the cranks' from the frame
    private static Body Merge(Body his, Body bike)
    {
        var world = his.World().ToList();
        var bikeWorld = bike.World();
        var start = world.Count;
        var result = new Body
        {
            Game = 2, Lit = true, Header = (byte[])his.Header.Clone(), LimitOverride = 1100, Textures = his.Textures, TexturePage = his.TexturePage,
        };
        result.Bones.AddRange(his.Bones);
        result.Bones.Add(new Bone(start + bike.Bones[0].Start, bike.Bones[0].Count, his.Bones[Root].Pivot, Root, new byte[38]));
        result.Bones.Add(new Bone(start + bike.Bones[1].Start, bike.Bones[1].Count, start + bike.Bones[1].Pivot, Frame, new byte[38]));
        world.AddRange(bikeWorld);
        result.Faces.AddRange(his.Faces);
        result.Faces.AddRange(bike.Faces.Select(f => f with { Points = f.Points.Select(i => i + start).ToArray(), PointNormals = null }));
        result.Lines.AddRange(his.Lines);
        result.Lines.AddRange(bike.Lines.Select(l => l with { A = l.A + start, B = l.B + start }));
        result.Spheres.AddRange(his.Spheres);
        result.Spheres.AddRange(bike.Spheres.Select(s => s with { Point = s.Point + start }));
        var hisLight = his.LightScale ?? Enumerable.Repeat(1f, start).ToArray();
        var bikeLight = bike.LightScale ?? Enumerable.Repeat(1f, bikeWorld.Length).ToArray();
        result.LightScale = hisLight.Concat(bikeLight).ToArray();
        result.Vertices = Enumerable.Repeat(Vector3.Zero, world.Count).ToList();
        result.SetWorld(world.ToArray());
        result.Validate();
        return result;
    }
}
