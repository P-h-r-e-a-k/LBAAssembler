using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using LBAAssembler.Characters;
using LBAAssembler.Terrain.Palace;

namespace LBAAssembler.Terrain;

// Stella Entities' seven performers (docs/LBRA_STRIP_JOINT_DESIGN.md). The user, 2026-11-13: "None of the models look anything like any
// species from the game. We should be able to take some random characters and reskin and colour them to get the right shape", "We need the
// pole dancers to be doing tricks with the poles, and strutting their stuff on the stage", "Have the performers topless, but wearing thongs
// so they're not fully naked" -- adult women of the game's species, a strip bar's stage show and no more.
//
// Each is one of the game's own bodies (its species' shape, its bones), recoloured: what she wore over her top her skin, a thong at her hips
// (the low band of what she wore there, in a stage colour), her shoes or boots kept; whatever she carried gone (the umbrella). Seven:
//   0 a Grobo (the Hacienda's bather, BODY 122)        stage       4 a Sup (the saucer's stewardess, 144)      stage
//   1 a rabbibunny (the Hacienda's bather, 121)        pole        5 a Franco (the shop lady, 443)               stage
//   2 a Quetch (the umbrella lady, 172)                stage       6 a wizard (the statue's, 189)                pole
//   3 a Sup (the Imperial Hotel's, 440, as the flag marshal's bikini, GridGirl) pole
// Installed as bodies of the hero's entity (0) -- generics Body0.. -- each with an animation of her own (Anim0 + her number) made for her
// bones from one routine (StageRoutine or PoleRoutine, below) by what each bone is (her Rig).
//
// The pole: a pole dancer's body stands Reach beside the actor's own spot (the pole: the actor stands on it), the pole at her left (her
// arm B's side); her bone 1 (the one everything hangs from, its pivot moved onto the pole's line) turns her round the pole, and bone 2 -- a
// shift, in her pole routine -- lifts her up it (a climb), or, with bone 1 tipped, holds her upside down on it (an invert). On the stage,
// bone 1 is the shift instead (her strut along the stage's edge, a dip), and her hips (bone 2 or 3) turn her as she goes.
internal static class StellaPerformers
{
    public const int Entity = 0, Body0 = 70, Anim0 = 480;
    public static int AnimOf(int who) => Anim0 + who;

    // What each of her bones is: her hips (they turn her, sway), her torso (and chest, if she has one: -1), head, arms (+x side A, -x side
    // B: upper, fore) and legs (thigh, shin, foot or -1); bone 1 (Orbit) and 2 (Lift) as above
    public sealed record Rig(int Hips, int Torso, int Chest, int Head, int[] ArmA, int[] ArmB, int[] LegA, int[] LegB, int Orbit = 1, int Lift = 2);
    // A polygon's new colour (To; -1 drops it) when its bone (of its first point) is one of Bones (any: null), its colour (low byte) From
    // (any: -1), its middle within the heights and depths given
    public sealed record Rule(int To, int From = -1, int[]? Bones = null, float YMin = float.MinValue, float YMax = float.MaxValue, float ZMin = float.MinValue, float ZMax = float.MaxValue);
    public sealed record Spec(string Name, int Source, Rig Rig, bool Pole, Rule[] Rules, int DropFrom = -1, bool GridGirl = false);

    private static int[] B(params int[] b) => b;
    private const int Red = 69, Gold = 100, Black = 49, Purple = 210, Teal = 147;
    // (a pole dancer's grip: how high her hand holds the pole, bone 1's pivot -- what she turns and tips about)
    private const float GripHigh = 1500;

    public static readonly Spec[] All =
    {
        new("a Grobo", 122, new Rig(2, 3, -1, 4, B(9, 10), B(11, 12), B(5, 6, -1), B(7, 8, -1)), false, new Rule[]
        {
            new(192, 51, B(3, 9, 11)),                     // the bather's suit over her top: her skin
            new(192, 51, B(2), YMin: 300),                 // its upper part at her middle too
            new(Red, 51),                                  // the rest of it -- low at her hips, the legs' edges: a red thong
        }),
        new("a rabbibunny", 121, new Rig(2, 3, -1, 16, B(4, 5), B(7, 8), B(10, 11, 12), B(13, 14, 15)), true, new Rule[]
        {
            new(81, 101, B(3)),                            // the bikini's top: her fur
            new(Black, 101, B(2)),                         // its bottom: a black thong
        }),
        new("a Quetch", 172, new Rig(2, 3, -1, 16, B(4, 5), B(7, 8), B(10, 11, 12), B(13, 14, 15)), false, new Rule[]
        {
            new(22, -1, B(3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 14)),   // her suit: skin, top to toe but for her shoes and hips
            new(Gold, -1, B(2), YMax: 660),                // the hips' low band: a gold thong
            new(22, -1, B(2)),
            new(Gold, 64, B(12, 15)),                      // gold shoes
        }, DropFrom: 21),
        new("a Sup", 440, new Rig(2, 3, -1, 4, B(8, 9), B(5, 6), B(14, 15, 16), B(11, 12, 13)), true, new Rule[]
        {
            new(36, 69, B(3, 17, 18, 19)),                 // the marshal's bikini top: her skin
            new(Black, 69),                                // its bottom and her shoes: black
        }, GridGirl: true),
        new("a Sup stewardess", 144, new Rig(2, 3, 4, 6, B(7, 8), B(10, 11), B(13, 14, 15), B(16, 17, 18)), false, new Rule[]
        {
            new(38, 194, B(13, 16)),                       // her shorts' legs: skin
            new(Teal, 194, B(2)),                          // their seat: a teal thong
        }),
        new("a Franco", 443, new Rig(3, 4, -1, 6, B(10, 11), B(13, 14), B(16, 17, 18), B(20, 21, 22)), false, new Rule[]
        {
            new(Purple, 70, B(3), YMax: 400),              // her shorts' low band: a purple thong
            new(82, 70),                                   // the rest of them: skin
            new(82, 150, B(10, 13)),                       // her sleeves: skin
            new(82, 222),                                  // her socks: skin
        }),
        new("a wizard", 189, new Rig(2, 3, 4, 6, B(14, 15), B(11, 12), B(20, 21, 22), B(17, 18, 19)), true, new Rule[]
        {
            new(32, 160, B(4)),                            // her suit's top: skin
            new(Gold, 64),                                 // its belt: a gold band
            new(Purple, 160),                              // the rest -- at her hips and the boots: purple, a wizard's
        }),
    };

    // ---- the bodies --------------------------------------------------------------------------------------------------------------

    // Her body from the game's (`source`, BODY.HQR's entry): recoloured by her rules, what she carried dropped, set beside the pole if she
    // dances on one; its box round all the room her routine takes
    public static byte[] Make(Spec s, byte[] source)
    {
        if (s.GridGirl) source = GridGirl.Remodel(source);
        var p = PalaceTorturers.Parts.Read(source);
        if (s.DropFrom > 0)
        {
            var e = new GridGirl.Edit(p);
            e.DropBonesFrom(s.DropFrom);
            p = PalaceTorturers.Parts.Read(e.Finish());
        }
        // (each polygon by the first rule it meets; one with a picture on it -- the wizard's suit -- made a plain one, gouraud-lit, if a rule
        // gives it a colour: its first 12 bytes are a plain polygon's, its points, colour and light)
        var world = p.World();
        var plain = new Dictionary<int, List<byte[]>>();
        for (var b = 0; b < p.Blocks.Count; b++)
        {
            var (type, records) = p.Blocks[b];
            var n = (type & 0x8000) != 0 ? 4 : 3;
            var textured = (type & 0xFF) > 7;
            var kept = new List<byte[]>();
            foreach (var record in records)
            {
                var points = Enumerable.Range(0, n).Select(k => (int)BinaryPrimitives.ReadUInt16LittleEndian(record.AsSpan(k * 2))).ToArray();
                var colour = BinaryPrimitives.ReadUInt16LittleEndian(record.AsSpan(8));
                var bone = p.Points[points[0]].Bone;
                var c = points.Select(i => world[i]).Aggregate(Vector3.Zero, (a, v) => a + v) / n;
                var rule = s.Rules.FirstOrDefault(r => (r.From < 0 || (colour & 0xFF) == r.From) && (r.Bones is null || r.Bones.Contains(bone))
                                                       && c.Y >= r.YMin && c.Y <= r.YMax && c.Z >= r.ZMin && c.Z <= r.ZMax);
                if (rule is null) { kept.Add(record); continue; }
                if (rule.To < 0) continue;
                if (!textured)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(8), (ushort)((colour & 0xFF00) | rule.To));
                    kept.Add(record);
                    continue;
                }
                var r12 = record[..12];
                BinaryPrimitives.WriteUInt16LittleEndian(r12.AsSpan(8), (ushort)(0x1000 | rule.To));
                var to = (type & 0x8000) | 4;
                if (!plain.TryGetValue(to, out var list)) plain[to] = list = new();
                list.Add(r12);
            }
            p.Blocks[b] = (type, kept);
        }
        foreach (var (type, list) in plain) p.Blocks.Add((type, list));
        byte[] bytes;
        var reach = Reach(s, p);
        if (s.Pole)
        {
            // (bone 1's pivot onto the pole's line -- the actor's spot -- at her grip's height, and her, from it, Reach out to her right: the
            // pole at her left; her points kept where they are from the pivot front to back, so she stands on the line; tipped about that
            // pivot she goes over her hand on the pole, upside down above it)
            var pivot = p.Bones[s.Rig.Orbit].Pivot;
            var below = GripHigh - p.Points[pivot].Y;
            p.Points[pivot] = p.Points[pivot] with { X = 0, Y = (short)GripHigh, Z = 0 };
            var start = p.StartOf(s.Rig.Orbit);
            for (var i = start; i < start + p.Bones[s.Rig.Orbit].Count; i++)
                p.Points[i] = p.Points[i] with { X = (short)(p.Points[i].X + reach), Y = (short)(p.Points[i].Y - below) };
        }
        bytes = p.Write();
        var w = p.World();
        var height = w.Max(v => v.Y);
        var box = s.Pole ? new float[] { -reach - 900, reach + 900, 0, height + 2200, -reach - 900, reach + 900 }
                         : new float[] { -1000, 1000, 0, height + 300, -450, 450 };
        for (var k = 0; k < 6; k++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8 + k * 4), (int)box[k]);
        return bytes;
    }

    // How far beside the pole she stands: her B shoulder's way out, and a little more (her hand, up, on the pole)
    private static float Reach(Spec s, PalaceTorturers.Parts p)
    {
        var w = p.World();
        return Math.Abs(w[p.Bones[s.Rig.ArmB[0]].Pivot].X) + 20;
    }

    // ---- the routines ------------------------------------------------------------------------------------------------------------

    // A moment of a routine, in degrees: her whole self round the pole (Orbit) and tipped over it (Invert); her pelvis's place (Pelvis: on
    // the pole, its height over her standing one and its way out from the pole's line -- or, on the stage, the shift: along the stage's
    // edge x, up y); her hips' turn (Turn: 90 faces her left), sway, tip; torso, chest, head; arms (Up: raised sideways, Fwd: forwards,
    // Elbow: the forearm in, Twist); legs (Kick: forwards, Out: sideways, Knee: bent)
    public sealed record Pose
    {
        public int Ms = 500;
        public float Orbit, Invert;
        public Vector3 Pelvis;
        public float Turn, Sway, Tip;
        public float Bend, Lean, Twist, ChestBend, ChestTwist;
        public float Nod, Look, Tilt;
        public float UpA, FwdA, ElbowA, InA, UpB, FwdB, ElbowB, InB;
        public float KickA, OutA, KneeA, KickB, OutB, KneeB;
    }

    // The stage: facing the room, hands on hips, popping them; a strut to her left along the edge, a pose to the crowd, a dip and a body roll
    // up from it; a strut right, past where she began, arms up, a shimmy; back to the middle
    public static List<Pose> StageRoutine()
    {
        var r = new List<Pose>();
        Pose HandsOnHips(float sway) => new() { Ms = 650, Sway = sway, UpA = 45, ElbowA = 15, InA = 105, UpB = 45, ElbowB = 15, InB = 105, KneeA = sway > 0 ? 18 : 0, KneeB = sway < 0 ? 18 : 0, Tilt = -sway * 0.5f, Lean = -sway * 0.4f };
        r.Add(HandsOnHips(12)); r.Add(HandsOnHips(-12)); r.Add(HandsOnHips(12));
        void Strut(float from, float to, float facing, int steps)
        {
            for (var k = 1; k <= steps; k++)
            {
                var a = k % 2 == 0 ? 1 : -1;
                r.Add(new()
                {
                    Ms = 420, Pelvis = new Vector3(from + (to - from) * k / steps, 0, 0), Turn = facing, Sway = 10 * a, Lean = -4 * a,
                    KickA = a > 0 ? -28 : 14, KneeA = a > 0 ? 8 : 30, KickB = a > 0 ? 14 : -28, KneeB = a > 0 ? 30 : 8, OutA = -6, OutB = -6,
                    FwdA = a > 0 ? 18 : -14, FwdB = a > 0 ? -14 : 18, UpA = 12, UpB = 12, ElbowA = 20, ElbowB = 20, Look = -facing * 0.6f,
                });
            }
        }
        r.Add(new() { Ms = 300, Turn = 90, UpA = 12, UpB = 12, Look = -40 });
        Strut(0, -700, 90, 4);
        r.Add(new() { Ms = 450, Pelvis = new(-700, 0, 0), Sway = 14, UpA = 160, ElbowA = 30, UpB = 45, ElbowB = 15, InB = 105, Bend = -10, Nod = -10, KneeB = 20 });
        r.Add(new() { Ms = 800, Pelvis = new(-700, 0, 0), Sway = -14, UpA = 165, ElbowA = 40, UpB = 45, ElbowB = 15, InB = 105, Bend = -12, Nod = -14, KneeA = 20, Tilt = 8 });
        // the dip: down, knees out, hands on her knees; and up through a body roll
        r.Add(new() { Ms = 600, Pelvis = new(-700, -330, 0), KickA = -75, OutA = 30, KneeA = 110, KickB = -75, OutB = 30, KneeB = 110, Bend = 20, FwdA = -40, FwdB = -40, UpA = 20, UpB = 20, ElbowA = 10, ElbowB = 10 });
        r.Add(new() { Ms = 500, Pelvis = new(-700, -150, 0), KickA = -35, OutA = 15, KneeA = 55, KickB = -35, OutB = 15, KneeB = 55, Tip = 12, Bend = 28, ChestBend = 10, Nod = 15, UpA = 30, UpB = 30, ElbowA = 60, ElbowB = 60 });
        r.Add(new() { Ms = 450, Pelvis = new(-700, -40, 0), KneeA = 15, KneeB = 15, Tip = -10, Bend = -8, ChestBend = -14, Nod = -20, UpA = 30, UpB = 30, ElbowA = 40, ElbowB = 40 });
        r.Add(new() { Ms = 300, Pelvis = new(-700, 0, 0), Turn = -90, UpA = 12, UpB = 12, Look = 40 });
        Strut(-700, 700, -90, 8);
        // arms up, her hair thrown back; a shimmy
        r.Add(new() { Ms = 500, Pelvis = new(700, 0, 0), UpA = 170, UpB = 170, ElbowA = 30, ElbowB = 30, Nod = -25, Bend = -10, Sway = 10 });
        foreach (var t in new[] { 14f, -14, 14, -14, 14, -14 })
            r.Add(new() { Ms = 170, Pelvis = new(700, 0, 0), Twist = t, ChestTwist = t * 0.6f, UpA = 50, UpB = 50, ElbowA = 80, ElbowB = 80, KneeA = 10, KneeB = 10, Sway = t * 0.3f });
        r.Add(new() { Ms = 300, Pelvis = new(700, 0, 0), Turn = 90, UpA = 12, UpB = 12, Look = -40 });
        Strut(700, 0, 90, 4);
        r.Add(new() { Ms = 300, Turn = 0, UpA = 45, ElbowA = 15, InA = 105, UpB = 45, ElbowB = 15, InB = 105 });
        return r;
    }

    // The pole: a hand up on it, leaning out; walking round it; a spin, her legs tucked then one out; down; climbing it, legs wrapped round;
    // upside down on it, legs opening and closing; back down; sliding down it to a squat, and up through a body roll
    public static List<Pose> PoleRoutine()
    {
        var r = new List<Pose>();
        const float Hold = 168, Grip = 25;    // her B hand up on the pole
        r.Add(new() { Ms = 800, UpB = Hold, ElbowB = Grip, Lean = -14, UpA = 70, ElbowA = 20, Sway = 8, OutA = 10, Tilt = -6 });
        r.Add(new() { Ms = 700, UpB = Hold, ElbowB = Grip, Lean = -20, UpA = 95, ElbowA = 10, Sway = -6, KneeA = 25, Nod = -10 });
        // walking round it
        for (var k = 1; k <= 4; k++)
        {
            var a = k % 2 == 0 ? 1 : -1;
            r.Add(new()
            {
                Ms = 480, Orbit = 60 * k, UpB = Hold, ElbowB = Grip, Lean = -10, UpA = 55, ElbowA = 15, Sway = 8 * a,
                KickA = a > 0 ? -30 : 12, KneeA = a > 0 ? 6 : 34, KickB = a > 0 ? 12 : -30, KneeB = a > 0 ? 34 : 6, Look = -15,
            });
        }
        // the spin: off her feet, round
        r.Add(new() { Ms = 380, Orbit = 330, Pelvis = new(0, 160, 0), UpB = Hold, ElbowB = 10, Lean = -18, UpA = 80, KickA = -70, KneeA = 95, KickB = -60, KneeB = 105 });
        r.Add(new() { Ms = 380, Orbit = 90, Pelvis = new(0, 240, 0), UpB = Hold, ElbowB = 10, Lean = -24, UpA = 100, FwdA = -20, KickA = -80, KneeA = 0, KickB = -50, KneeB = 110, Nod = -15 });
        r.Add(new() { Ms = 380, Orbit = 210, Pelvis = new(0, 180, 0), UpB = Hold, ElbowB = 10, Lean = -20, UpA = 100, KickA = -75, KneeA = 10, KickB = -40, KneeB = 100, Nod = -20 });
        r.Add(new() { Ms = 420, Orbit = 330, Pelvis = new(0, 40, 0), UpB = Hold, ElbowB = 15, Lean = -12, UpA = 70, KickA = -20, KneeA = 20, KickB = -10, KneeB = 30 });
        r.Add(new() { Ms = 400, Orbit = 360, UpB = Hold, ElbowB = Grip, Lean = -14, UpA = 60, Sway = 8 });
        // climbing: both hands on it, legs round it
        Pose Climb(float y, int ms, float legs) => new()
        {
            Ms = ms, Orbit = 360, Pelvis = new(0, y, 0), UpB = Hold, ElbowB = 10, UpA = 165, ElbowA = 35, Lean = -6,
            KickA = -45 * legs, OutA = -20 * legs, KneeA = 70 * legs, KickB = -20 * legs, OutB = 15 * legs, KneeB = 50 * legs,
        };
        r.Add(Climb(250, 500, 1)); r.Add(Climb(450, 450, 0.4f)); r.Add(Climb(600, 500, 1)); r.Add(Climb(650, 450, 0.6f));
        // tipping over: upside down on it, held there, legs in a V and closing
        Pose Invert(float angle, float height, int ms, float v) => new()
        {
            Ms = ms, Orbit = 360, Invert = angle, Pelvis = new(0, height, 0), UpB = Hold, ElbowB = 10, UpA = 150, ElbowA = 30, Lean = -6,
            KickA = -10, OutA = 40 * v, KneeA = 5, KickB = -10, OutB = 40 * v, KneeB = 5, Nod = -10,
        };
        r.Add(Invert(60, 1000, 600, 0.2f)); r.Add(Invert(120, 1050, 600, 0.6f)); r.Add(Invert(170, 1050, 600, 1));
        r.Add(Invert(170, 1050, 700, 0.15f)); r.Add(Invert(170, 1050, 700, 1)); r.Add(Invert(170, 1050, 700, 0.2f));
        r.Add(Invert(110, 1000, 600, 0.4f)); r.Add(Invert(50, 900, 600, 0.2f));
        // sliding down to a squat, then up through a body roll
        r.Add(Climb(600, 500, 0.8f) with { Invert = 0 });
        r.Add(new() { Ms = 700, Orbit = 360, Pelvis = new(0, -300, 0), UpB = 150, ElbowB = 30, UpA = 30, ElbowA = 60, KickA = -80, OutA = 30, KneeA = 115, KickB = -80, OutB = 30, KneeB = 115, Bend = 15 });
        r.Add(new() { Ms = 500, Orbit = 360, Pelvis = new(0, -130, 0), UpB = 160, ElbowB = 25, UpA = 50, ElbowA = 40, KickA = -35, KneeA = 50, KickB = -35, KneeB = 50, Tip = 12, Bend = 26, ChestBend = 10, Nod = 15 });
        r.Add(new() { Ms = 450, Orbit = 360, Pelvis = new(0, -30, 0), UpB = Hold, ElbowB = Grip, UpA = 60, KneeA = 12, KneeB = 12, Tip = -10, Bend = -8, ChestBend = -14, Nod = -20 });
        r.Add(new() { Ms = 400, Orbit = 360, UpB = Hold, ElbowB = Grip, Lean = -14, UpA = 70, ElbowA = 20, Sway = 8, OutA = 10, Tilt = -6 });
        return r;
    }

    // A routine as her animation: her bones' turns (and the one shift) key by key
    public static CharacterAnim Animation(Spec s, byte[] body, int generic, List<Pose>? routineGiven = null)
    {
        var p = PalaceTorturers.Parts.Read(body);
        var world = p.World();
        int bones = p.Bones.Count;
        var rig = s.Rig;
        var routine = routineGiven ?? (s.Pole ? PoleRoutine() : StageRoutine());
        var a = new CharacterAnim { Generic = generic, Name = $"stella {s.Name}" };
        foreach (var q in routine)
        {
            var key = new CharacterAnim.Key { Milliseconds = q.Ms, Bones = Enumerable.Range(0, bones).Select(_ => (0, Vector3.Zero)).ToList() };
            void Turn(int bone, float x, float y, float z) { if (bone > 0 && bone < bones && key.Bones[bone].Item1 == 0) key.Bones[bone] = (0, key.Bones[bone].Value + new Vector3(x, y, z)); }
            void Shift(int bone, Vector3 by) { if (bone > 0 && bone < bones) key.Bones[bone] = (1, by); }
            if (s.Pole)
            {
                // round the pole and tipped over her grip on it (bone 1, its pivot there); the lift (bone 2, a shift): up the pole by
                // Pelvis.Y -- upright only (once tipped, the tip alone holds her up over her hand)
                Turn(rig.Orbit, 0, q.Orbit, q.Invert);
                Shift(rig.Lift, q.Invert == 0 ? new Vector3(0, q.Pelvis.Y, 0) : Vector3.Zero);
            }
            else
                Shift(rig.Orbit, q.Pelvis);
            // (hips that are the lift, on the pole, don't turn: nor then do her legs make up for them)
            var hipsTurn = key.Bones[rig.Hips].Item1 == 0;
            var sway = hipsTurn ? q.Sway : 0;
            var tip = hipsTurn ? q.Tip : 0;
            Turn(rig.Hips, q.Tip, q.Turn, q.Sway);
            Turn(rig.Torso, q.Bend, q.Twist, q.Lean);
            if (rig.Chest >= 0) Turn(rig.Chest, q.ChestBend, q.ChestTwist, 0); else Turn(rig.Torso, q.ChestBend, q.ChestTwist, 0);
            Turn(rig.Head, q.Nod, q.Look, q.Tilt);
            // arms: A at +x (up: +z), B mirrored (up: -z); forward: -x; the forearm bent towards her front (Elbow) or in to her side (In)
            Turn(rig.ArmA[0], -q.FwdA, 0, q.UpA); Turn(rig.ArmA[1], -q.ElbowA, 0, -q.InA);
            Turn(rig.ArmB[0], -q.FwdB, 0, -q.UpB); Turn(rig.ArmB[1], -q.ElbowB, 0, q.InB);
            // legs: kick forward -x, out sideways (A +z, B -z), knee back +x; each thigh against her hips' sway, so her feet stay under her
            Turn(rig.LegA[0], q.KickA - tip, 0, q.OutA - sway); Turn(rig.LegA[1], q.KneeA, 0, 0);
            Turn(rig.LegB[0], q.KickB - tip, 0, -q.OutB - sway); Turn(rig.LegB[1], q.KneeB, 0, 0);
            if (rig.LegA.Length > 2 && rig.LegA[2] >= 0) Turn(rig.LegA[2], -q.KneeA * 0.4f - q.KickA * 0.3f, 0, 0);
            if (rig.LegB.Length > 2 && rig.LegB[2] >= 0) Turn(rig.LegB[2], -q.KneeB * 0.4f - q.KickB * 0.3f, 0, 0);
            a.Keys.Add(key);
        }
        a.LoopFrame = 0;
        return a;
    }

    // ---- installing --------------------------------------------------------------------------------------------------------------

    // Into the game folder: the seven bodies and their animations at the ends of BODY.HQR and ANIM.HQR, given to entity 0 as bodies
    // Body0..Body0+6 and animations Anim0..Anim0+6. Lines for the build's log.
    public static List<string> Install(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        var hero = Lba2EntityTable.Parse(table).Entities.FirstOrDefault(e => e.Id == Entity);
        if (hero is null)
            return new() { "WARNING: Stella Entities' performers: no hero entity -- none made" };
        if (hero.Bodies.Any(b => b.Generic >= Body0 && b.Generic < Body0 + All.Length) || hero.Anims.Any(a => a.Generic >= Anim0 && a.Generic < Anim0 + All.Length))
            return new() { $"WARNING: Stella Entities' performers: entity {Entity} already has bodies {Body0}-{Body0 + All.Length - 1} or animations {Anim0}-{Anim0 + All.Length - 1} -- none made" };
        var bodies = HqrArchive.Open(bodyPath);
        var made = All.Select(s => Make(s, bodies.Read(s.Source))).ToList();
        var file = File.ReadAllBytes(bodyPath);
        var firstBody = HqrArchive.CountEntries(bodyPath);
        var anims = File.ReadAllBytes(animPath);
        var firstAnim = HqrArchive.CountEntries(animPath);
        for (var k = 0; k < All.Length; k++)
        {
            file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(made[k]));
            table = RaceTrackBaldinoCar.WithBody(table, Entity, Body0 + k, firstBody + k);
            var bones = PalaceTorturers.Parts.Read(made[k]).Bones.Count;
            anims = HqrWriter.AppendEntry(anims, HqrWriter.StoredEntry(Animation(All[k], made[k], AnimOf(k)).Write(bones)));
            table = RaceTrackJumpAnim.WithAnim(table, Entity, AnimOf(k), firstAnim + k);
        }
        File.WriteAllBytes(bodyPath, file);
        File.WriteAllBytes(animPath, anims);
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return new() { $"Stella Entities' performers: {string.Join(", ", All.Select(s => s.Name))} (the game's own bodies, recoloured) -- BODY.HQR {firstBody}-{firstBody + All.Length - 1}, entity {Entity}'s bodies {Body0}-{Body0 + All.Length - 1}; their dances ANIM.HQR {firstAnim}-{firstAnim + All.Length - 1}, its animations {Anim0}-{Anim0 + All.Length - 1}" };
    }
}
