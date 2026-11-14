using System.IO;
using System.Numerics;
using LBAAssembler.Characters;
using LBAAssembler.Terrain.Palace;

namespace LBAAssembler.Terrain;

// Stella Entities' staff (RaceTrackStory.StellaLounge). The user, 2026-11-13: "Let's replace the waitresses in the strip club with '124:
// Female rabbibunny in the School of Magic', let's give her a tray of drinks to be carrying around, and a model just in a thong for if
// Twinsen pays for a lap dance"; "She should replace this character, and also be the one offering the private show. Let's also add a grobo
// model in, similar to the one already dancing on the stage to offer a lap dance too, but with a lesser fee of 50 zilitos".
//   - the rabbibunny (BODY 124, the School of Magic's: a bunny girl's black corset, dark tights, white collar and cuffs, heels) as she is:
//     the hostess offering the private show (Bunny); with a round tray of three cocktails held in front of her on her right hand: the
//     waitresses (BunnyTray); topless in a black thong, her collar, cuffs and heels kept: the private show's dancer (BunnyThong);
//   - the Grobo -- the Hacienda's bather in her suit (BODY 122), the stage's Grobo's own body: the other hostess, offering a lap dance
//     (GroboHost); in the private room the stage's Grobo (StellaPerformers' body 0) dances it.
// Entity 77 (the School's) has no walk: as the performers, they are bodies of the hero's entity (0) with animations of their own made for
// their bones -- a stand, a walk (its keys' steps carry them along a track's way, as the game's walks do), the waitress's two holding the
// tray level, and a lap dance in front of the sofa.
internal static class StellaStaff
{
    public const int Entity = 0;
    public const int Bunny = 77, BunnyTray = 78, BunnyThong = 79, GroboHost = 80;
    public const int BunnyStand = 490, BunnyWalk = 491, TrayStand = 492, TrayWalk = 493, BunnyLap = 494, GroboStand = 495, GroboWalk = 496, GroboLap = 497;
    private const int BunnySource = 124, GroboSource = 122;
    // (a walk's step: how far each key carries her -- the stewardess's walk, ANIM 751, 150-180 a key of 200-260 ms)
    private const float WalkStep = 165;
    private const int WalkKey = 240;

    private static int[] B(params int[] b) => b;
    // her bones: hips 2, torso 3, chest 4, head 5 (her ears 6-9), right arm 10-11 (hand 12), left 13-14 (15), right leg 16-18, left 19-21
    private static readonly StellaPerformers.Rig BunnyRig = new(2, 3, 4, 5, B(10, 11), B(13, 14), B(16, 17, 18), B(19, 20, 21));
    private static StellaPerformers.Rig GroboRig => StellaPerformers.All[0].Rig;
    // her colours: tights 80, corset 224 (and her heels), skin 83, collar and cuffs 237
    private const int Tights = 80, Corset = 224, Fur = 83;
    private static readonly StellaPerformers.Spec ThongSpec = new("a rabbibunny in a thong", BunnySource, BunnyRig, false, new StellaPerformers.Rule[]
    {
        new(Corset, -1, B(2), YMax: 790),              // the hips' low band: a black thong
        new(Fur, -1, B(2)),                            // the rest of the hips: her fur
        new(Fur, Corset, B(3, 4)),                     // the corset: her fur
        new(Fur, Tights, B(3, 16, 17, 19, 20)),        // the tights: bare legs
    });
    private static readonly StellaPerformers.Spec BunnySpec = new("the rabbibunny", BunnySource, BunnyRig, false, Array.Empty<StellaPerformers.Rule>());
    private static readonly StellaPerformers.Spec GroboSpec = new("the Grobo", GroboSource, GroboRig, false, Array.Empty<StellaPerformers.Rule>());

    // ---- the tray: on her chest's bone (her arms hang from it: the hand held under it as she walks), in front of her right side at her
    // forearm's height bent forward, round, a rim; three cocktails on it
    private const int Brass = 106, Glass = 15, Pink = 79, Teal = 147, Cherry = 69;
    private static readonly Vector3 TrayAt = new(170, 925, 235);
    private const float TrayRadius = 165;

    public static byte[] WithTray(byte[] source)
    {
        var p = PalaceTorturers.Parts.Read(source);
        var e = new GridGirl.Edit(p);
        const int Chest = 4, Shade = 0x0A00, Sides = 10;
        Vector3[] Ring(Vector3 c, float r, int n) => Enumerable.Range(0, n).Select(k => c + new Vector3(r * MathF.Cos(k * 2 * MathF.PI / n), 0, r * MathF.Sin(k * 2 * MathF.PI / n))).ToArray();
        // the tray: its top and underside (fans to their middles), its rim
        var top = e.Add(Chest, Ring(TrayAt, TrayRadius, Sides));
        var under = e.Add(Chest, Ring(TrayAt - new Vector3(0, 18, 0), TrayRadius, Sides));
        var rim = e.Add(Chest, Ring(TrayAt + new Vector3(0, 22, 0), TrayRadius + 6, Sides));
        var topMiddle = e.Add(Chest, new[] { TrayAt })[0];
        var underMiddle = e.Add(Chest, new[] { TrayAt - new Vector3(0, 18, 0) })[0];
        for (var k = 0; k < Sides; k++)
        {
            var n = (k + 1) % Sides;
            e.Face(new[] { topMiddle, top[n], top[k] }, Shade | Brass, Vector3.UnitY);
            e.Face(new[] { underMiddle, under[k], under[n] }, Shade | Brass, -Vector3.UnitY);
            var out_ = Vector3.Normalize(e.World(top[k]) + e.World(top[n]) - 2 * TrayAt);
            e.Face(new[] { under[k], under[n], rim[n], rim[k] }, Shade | Brass, out_);
            e.Face(new[] { top[k], top[n], rim[n], rim[k] }, Shade | Brass, -out_);
        }
        // the cocktails: a glass's stem and bowl, the drink's top in it, a cherry on one
        foreach (var (at, drink) in new[] { (new Vector3(110, 0, 190), Pink), (new Vector3(235, 0, 215), Teal), (new Vector3(165, 0, 330), Pink) })
        {
            const int GlassSides = 6;
            var baseC = new Vector3(at.X, TrayAt.Y + 2, at.Z);
            var ringFoot = e.Add(Chest, Ring(baseC, 26, GlassSides));
            var ringStem = e.Add(Chest, Ring(baseC + new Vector3(0, 45, 0), 7, GlassSides));
            var ringBowl = e.Add(Chest, Ring(baseC + new Vector3(0, 105, 0), 42, GlassSides));
            var drinkTop = e.Add(Chest, Ring(baseC + new Vector3(0, 95, 0), 36, GlassSides));
            var drinkMiddle = e.Add(Chest, new[] { baseC + new Vector3(0, 95, 0) })[0];
            for (var k = 0; k < GlassSides; k++)
            {
                var n = (k + 1) % GlassSides;
                var out_ = Vector3.Normalize(e.World(ringFoot[k]) + e.World(ringFoot[n]) - 2 * baseC);
                e.Face(new[] { ringFoot[k], ringFoot[n], ringStem[n], ringStem[k] }, Shade | Glass, out_ + Vector3.UnitY);
                e.Face(new[] { ringStem[k], ringStem[n], ringBowl[n], ringBowl[k] }, Shade | drink, out_ - Vector3.UnitY * 0.3f);
                e.Face(new[] { drinkMiddle, drinkTop[n], drinkTop[k] }, Shade | drink, Vector3.UnitY);
            }
            if (at.Z > 300) e.AddSphere(Chest, baseC + new Vector3(0, 112, 0), 16, Cherry);
        }
        return e.Finish();
    }

    // The corset's white trim (lines, on her hips', torso's and chest's bones, under her collar) gone with it
    private static byte[] WithoutCorsetTrim(byte[] body)
    {
        var p = PalaceTorturers.Parts.Read(body);
        var world = p.World();
        bool Trim(byte[] r)
        {
            var a = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(4));
            var b = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(6));
            return new[] { a, b }.All(i => i < p.Points.Count && p.Points[i].Bone is >= 2 and <= 4) && Math.Min(world[a].Y, world[b].Y) < CollarFoot;
        }
        p.Lines = p.Lines.Chunk(8).Where(r => !Trim(r)).SelectMany(r => r).ToArray();
        p.LineCount = p.Lines.Length / 8;
        return p.Write();
    }
    private const float CollarFoot = 1190;

    // ---- the animations

    private static StellaPerformers.Pose Stand(bool tray, float sway = 0) => tray
        ? new() { Ms = 900, FwdA = 8, ElbowA = 88, InA = 0, UpA = 4, UpB = 8, ElbowB = 10, Sway = sway, KneeB = sway < 0 ? 12 : 0, KneeA = sway > 0 ? 12 : 0 }
        : new() { Ms = 900, UpA = 45, ElbowA = 15, InA = 105, UpB = 10, ElbowB = 12, Sway = sway, KneeB = sway < 0 ? 14 : 0, KneeA = sway > 0 ? 14 : 0, Tilt = -sway * 0.4f };

    // standing: weight from one hip to the other
    private static List<StellaPerformers.Pose> StandRoutine(bool tray) => new() { Stand(tray, 8), Stand(tray, -8) };

    // walking: four keys, a stride and the passing step, each foot in turn; her hips sway; the tray arm still
    private static List<StellaPerformers.Pose> WalkRoutine(bool tray)
    {
        var r = new List<StellaPerformers.Pose>();
        foreach (var a in new[] { 1, -1 })
        {
            var stride = new StellaPerformers.Pose
            {
                Ms = WalkKey, Sway = 9 * a, Lean = -3 * a,
                KickA = a > 0 ? -26 : 16, KneeA = a > 0 ? 6 : 26, KickB = a > 0 ? 16 : -26, KneeB = a > 0 ? 26 : 6,
                FwdB = a > 0 ? 16 : -12, UpB = 8, ElbowB = 14,
            };
            var passing = new StellaPerformers.Pose
            {
                Ms = WalkKey, Sway = 4 * a, KickA = a > 0 ? -6 : -10, KneeA = a > 0 ? 10 : 40, KickB = a > 0 ? -10 : -6, KneeB = a > 0 ? 40 : 10,
                FwdB = 2, UpB = 8, ElbowB = 14,
            };
            foreach (var q in new[] { stride, passing })
            {
                if (tray) { q.FwdA = 8; q.ElbowA = 88; q.UpA = 4; }
                else { q.FwdA = -q.FwdB; q.UpA = 8; q.ElbowA = 14; }
                r.Add(q);
            }
        }
        return r;
    }

    // the lap dance, in front of the sofa (she faces it): hands on her hips, popping them; turning her back to him, a sway, a dip and a body
    // roll up from it; facing him again, arms up, a shimmy; hands on her knees, leaning in
    public static List<StellaPerformers.Pose> LapRoutine()
    {
        var r = new List<StellaPerformers.Pose>();
        StellaPerformers.Pose Hips(float sway, float turn = 0) => new() { Ms = 600, Turn = turn, Sway = sway, UpA = 45, ElbowA = 15, InA = 105, UpB = 45, ElbowB = 15, InB = 105, KneeA = sway > 0 ? 18 : 0, KneeB = sway < 0 ? 18 : 0, Tilt = -sway * 0.5f, Lean = -sway * 0.4f };
        r.Add(Hips(12)); r.Add(Hips(-12)); r.Add(Hips(12)); r.Add(Hips(-12));
        r.Add(Hips(0, 90) with { Ms = 400 });
        r.Add(Hips(12, 180)); r.Add(Hips(-12, 180)); r.Add(Hips(12, 180));
        r.Add(new() { Ms = 600, Turn = 180, Pelvis = new(0, -300, 0), KickA = -75, OutA = 30, KneeA = 110, KickB = -75, OutB = 30, KneeB = 110, Bend = 20, FwdA = -40, FwdB = -40, UpA = 20, UpB = 20, ElbowA = 10, ElbowB = 10 });
        r.Add(new() { Ms = 500, Turn = 180, Pelvis = new(0, -140, 0), KickA = -35, OutA = 15, KneeA = 55, KickB = -35, OutB = 15, KneeB = 55, Tip = 12, Bend = 28, ChestBend = 10, Nod = 15, UpA = 30, UpB = 30, ElbowA = 60, ElbowB = 60 });
        r.Add(new() { Ms = 450, Turn = 180, Pelvis = new(0, -40, 0), KneeA = 15, KneeB = 15, Tip = -10, Bend = -8, ChestBend = -14, Nod = -20, UpA = 30, UpB = 30, ElbowA = 40, ElbowB = 40 });
        r.Add(Hips(0, 270) with { Ms = 400 });
        r.Add(new() { Ms = 500, Turn = 360, UpA = 170, UpB = 170, ElbowA = 30, ElbowB = 30, Nod = -20, Bend = -8, Sway = 10 });
        foreach (var t in new[] { 14f, -14, 14, -14, 14, -14 })
            r.Add(new() { Ms = 170, Turn = 360, Twist = t, ChestTwist = t * 0.6f, UpA = 50, UpB = 50, ElbowA = 80, ElbowB = 80, KneeA = 10, KneeB = 10, Sway = t * 0.3f });
        r.Add(new() { Ms = 600, Turn = 360, Pelvis = new(0, -120, 0), Bend = 30, ChestBend = 8, Nod = -25, KneeA = 40, KneeB = 40, KickA = -20, KickB = -20, OutA = 14, OutB = 14, FwdA = 40, FwdB = 40, UpA = 10, UpB = 10 });
        r.Add(new() { Ms = 600, Turn = 360, Pelvis = new(0, -120, 0), Bend = 30, ChestBend = 8, Nod = -25, KneeA = 40, KneeB = 40, KickA = -20, KickB = -20, OutA = 14, OutB = 14, FwdA = 40, FwdB = 40, UpA = 10, UpB = 10, Sway = 10 });
        r.Add(Hips(0, 360) with { Ms = 500 });
        // (Turn 360 is where 0 is: the loop's first key carries on from it)
        foreach (var q in r) q.Turn %= 360;
        return r;
    }

    private static CharacterAnim Made(StellaPerformers.Spec s, byte[] body, int generic, List<StellaPerformers.Pose> routine, bool walk)
    {
        var a = StellaPerformers.Animation(s, body, generic, routine);
        a.Name = $"stella staff {generic}";
        if (walk) foreach (var k in a.Keys) k.Step = new Vector3(0, 0, WalkStep);
        return a;
    }

    // ---- installing: the two new bodies at BODY.HQR's end, the two the game's (124, 122) as they are; the animations at ANIM.HQR's end;
    // all given to entity 0
    public static List<string> Install(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        var hero = Lba2EntityTable.Parse(table).Entities.FirstOrDefault(e => e.Id == Entity);
        if (hero is null) return new() { "WARNING: Stella Entities' staff: no hero entity -- none made" };
        if (hero.Bodies.Any(b => b.Generic >= Bunny && b.Generic <= GroboHost) || hero.Anims.Any(a => a.Generic >= BunnyStand && a.Generic <= GroboLap))
            return new() { $"WARNING: Stella Entities' staff: entity {Entity} already has bodies {Bunny}-{GroboHost} or animations {BunnyStand}-{GroboLap} -- none made" };
        var bodies = HqrArchive.Open(bodyPath);
        var bunny = bodies.Read(BunnySource);
        var grobo = bodies.Read(GroboSource);
        var tray = WithTray(bunny);
        var thong = WithoutCorsetTrim(StellaPerformers.Make(ThongSpec, bunny));
        var stageGrobo = StellaPerformers.Make(StellaPerformers.All[0], grobo);
        var file = File.ReadAllBytes(bodyPath);
        var first = HqrArchive.CountEntries(bodyPath);
        file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(tray));
        file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(thong));
        table = RaceTrackBaldinoCar.WithBody(table, Entity, Bunny, BunnySource);
        table = RaceTrackBaldinoCar.WithBody(table, Entity, BunnyTray, first);
        table = RaceTrackBaldinoCar.WithBody(table, Entity, BunnyThong, first + 1);
        table = RaceTrackBaldinoCar.WithBody(table, Entity, GroboHost, GroboSource);
        var anims = new[]
        {
            (Made(BunnySpec, bunny, BunnyStand, StandRoutine(false), false), bunny),
            (Made(BunnySpec, bunny, BunnyWalk, WalkRoutine(false), true), bunny),
            (Made(BunnySpec, tray, TrayStand, StandRoutine(true), false), tray),
            (Made(BunnySpec, tray, TrayWalk, WalkRoutine(true), true), tray),
            (Made(BunnySpec, thong, BunnyLap, LapRoutine(), false), thong),
            (Made(GroboSpec, grobo, GroboStand, StandRoutine(false), false), grobo),
            (Made(GroboSpec, grobo, GroboWalk, WalkRoutine(false), true), grobo),
            (Made(StellaPerformers.All[0], stageGrobo, GroboLap, LapRoutine(), false), stageGrobo),
        };
        var animFile = File.ReadAllBytes(animPath);
        var firstAnim = HqrArchive.CountEntries(animPath);
        for (var k = 0; k < anims.Length; k++)
        {
            var (a, body) = anims[k];
            animFile = HqrWriter.AppendEntry(animFile, HqrWriter.StoredEntry(a.Write(PalaceTorturers.Parts.Read(body).Bones.Count)));
            table = RaceTrackJumpAnim.WithAnim(table, Entity, a.Generic, firstAnim + k);
        }
        File.WriteAllBytes(bodyPath, file);
        File.WriteAllBytes(animPath, animFile);
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return new() { $"Stella Entities' staff: the School of Magic's rabbibunny (BODY {BunnySource}) as the hostess, with a tray of cocktails (BODY {first}) the waitresses, in a thong (BODY {first + 1}) the private dancer; the Grobo bather (BODY {GroboSource}) the other hostess -- entity {Entity}'s bodies {Bunny}-{GroboHost}; their stands, walks and lap dances ANIM.HQR {firstAnim}-{firstAnim + anims.Length - 1}, its animations {BunnyStand}-{GroboLap}" };
    }
}
