using System.IO;
using System.Numerics;
using LBAAssembler.Characters;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The bowlers' own reactions (the user, 2026-11-15: "create fresh animations for our bowlers to celebrate when they do well, or remonstrate when
// they do badly"): for each of the four -- the monkey monster, the swordsman monkey, the sailor, the green gloom -- three animations of the
// build's, made from the first frame of the bowler's own standing animation with some bones turned, and the bowler staying where he stands
// (the game's that stood in for them walked: the swordsman's cheer carried him 2,600 down the lane):
//   - Celebrate (a strike): both arms thrown up, two fist pumps, the head back;
//   - Shrug (some pins): the arms out to the sides, the head tilted, a slow shake;
//   - Remonstrate (the gutter): leaning at the lane, fists shaken one after the other, the head shaking, a stamp.
// Added to ANIM.HQR and to each bowler's entity (RESS.HQR 44) under generic numbers of their own (Celebrate, Shrug, Remonstrate).
internal static class BowlingAnims
{
    public const int Celebrate = 640, Shrug = 641, Remonstrate = 642;
    // (2026-11-15, second round: the throw itself, each bowler's own -- the user, "Let's make the custom animations that you mentioned were
    // still missing to bowl with" -- the ball swung in the hand on the bowler's world east as he faces the pins, his body's left (-x: he faces
    // north, turned half round), let go at 1.0 s as RACEMOD.CPP BOWL_RELEASE_MS has it; and the poker players' sitting at the table)
    public const int Bowl = 643, Poker = 644, Pool = 645;   // (Pool: the sailor's shot at the pool table, 2026-11-16)
    // the swordsman monkey without his sword (the user: "The NPC that bowls whilst holding a sword let's add a new model without the sword"):
    // his entity's body of this number -- his own, the sword's faces in his left hand (bone 14: the blade 52, the hilt 100) left out
    public const int Swordless = 1, SwordEntity = 252, SwordBone = 14;

    // A bowler's rig: its entity, its body (BODY.HQR), its standing animation (ANIM.HQR), and its bones -- each arm's upper and fore bones
    // (the right is +x), the head, the chest -- and which way an arm's upper bone turns up about z (+1 or -1 on the right; the left the other)
    internal sealed record Rig(string Name, int Entity, int Body, int Stand, int Bones, int RUpper, int RFore, int LUpper, int LFore, int Head, int Chest, int RThigh, int LThigh, float UpSign);
    internal static readonly Rig[] Rigs =
    {
        new("the monkey monster", 275, 388, 1710, 25, 19, 20, 22, 23, 6, 4, 13, 16, 1),
        new("the swordsman monkey", 252, 363, 1613, 21, 9, 10, 12, 13, 7, 4, 15, 18, 1),
        new("the sailor", 297, 419, 1855, 25, 7, 8, 12, 13, 5, 3, 17, 21, 1),
        new("the green gloom", 204, 295, 1384, 30, 9, 10, 13, 14, 6, 3, 17, 21, 1),
    };

    // A keyframe: the standing pose with turns added (degrees about x, y, z) to some bones
    private static CharacterAnim.Key Key(CharacterAnim.Key stand, int ms, params (int Bone, float X, float Y, float Z)[] turns)
    {
        var k = stand.Copy();
        k.Milliseconds = ms; k.Step = Vector3.Zero;
        foreach (var (b, x, y, z) in turns)
            if (b > 0 && b < k.Bones.Count && k.Bones[b].Type == 0) k.Bones[b] = (0, k.Bones[b].Value + new Vector3(x, y, z));
        return k;
    }

    // The three animations of a rig, from its standing animation's first frame
    public static (CharacterAnim Celebrate, CharacterAnim Shrug, CharacterAnim Remonstrate) Make(Rig r, byte[] standAnim)
    {
        var stand = CharacterAnim.Read(standAnim, r.Bones, 0, "stand").Keys[0];
        var u = r.UpSign;
        // (an arm raised: its upper bone turned up about z, outward each side; bent at the elbow about x)
        (int, float, float, float) RUp(float deg) => (r.RUpper, 0, 0, u * deg);
        (int, float, float, float) LUp(float deg) => (r.LUpper, 0, 0, -u * deg);
        (int, float, float, float) RBend(float deg) => (r.RFore, -deg, 0, 0);
        (int, float, float, float) LBend(float deg) => (r.LFore, -deg, 0, 0);
        (int, float, float, float) RFwd(float deg) => (r.RUpper, -deg, 0, 0);
        (int, float, float, float) LFwd(float deg) => (r.LUpper, -deg, 0, 0);
        (int, float, float, float) Head(float x, float y, float z) => (r.Head, x, y, z);
        (int, float, float, float) Chest(float x, float y, float z) => (r.Chest, x, y, z);
        (int, float, float, float) RLeg(float deg) => (r.RThigh, -deg, 0, 0);
        (int, float, float, float) LLeg(float deg) => (r.LThigh, -deg, 0, 0);

        var celebrate = new CharacterAnim { Generic = Celebrate, Name = "celebrate", LoopFrame = 0 };
        celebrate.Keys.Add(Key(stand, 1));
        celebrate.Keys.Add(Key(stand, 260, RUp(150), LUp(150), RBend(20), LBend(20), Head(-20, 0, 0), Chest(-10, 0, 0)));
        celebrate.Keys.Add(Key(stand, 200, RUp(120), LUp(120), RBend(80), LBend(80), Head(-10, 0, 0), Chest(-5, 0, 0), RLeg(20)));
        celebrate.Keys.Add(Key(stand, 200, RUp(160), LUp(160), RBend(10), LBend(10), Head(-25, 0, 0), Chest(-12, 0, 0)));
        celebrate.Keys.Add(Key(stand, 200, RUp(120), LUp(120), RBend(80), LBend(80), Head(-10, 0, 0), Chest(-5, 0, 0), LLeg(20)));
        celebrate.Keys.Add(Key(stand, 220, RUp(160), LUp(160), RBend(10), LBend(10), Head(-25, 0, 0), Chest(-12, 0, 0)));
        celebrate.Keys.Add(Key(stand, 400, RUp(40), LUp(40), RBend(30), LBend(30), Head(-5, 0, 0)));
        celebrate.Keys.Add(Key(stand, 300));

        var shrug = new CharacterAnim { Generic = Shrug, Name = "shrug", LoopFrame = 0 };
        shrug.Keys.Add(Key(stand, 1));
        shrug.Keys.Add(Key(stand, 350, RUp(60), LUp(60), RBend(70), LBend(70), Head(0, 0, 15), Chest(0, 0, 5)));
        shrug.Keys.Add(Key(stand, 300, RUp(65), LUp(65), RBend(75), LBend(75), Head(0, 20, -10), Chest(0, 0, -3)));
        shrug.Keys.Add(Key(stand, 300, RUp(60), LUp(60), RBend(70), LBend(70), Head(0, -20, 12), Chest(0, 0, 3)));
        shrug.Keys.Add(Key(stand, 400, RUp(10), LUp(10), RBend(20), LBend(20), Head(10, 0, 0)));
        shrug.Keys.Add(Key(stand, 300));

        var remonstrate = new CharacterAnim { Generic = Remonstrate, Name = "remonstrate", LoopFrame = 0 };
        remonstrate.Keys.Add(Key(stand, 1));
        remonstrate.Keys.Add(Key(stand, 250, RFwd(80), RBend(90), LFwd(30), LBend(60), Head(15, 0, 0), Chest(20, 0, 0)));
        remonstrate.Keys.Add(Key(stand, 180, RFwd(110), RBend(60), LFwd(30), LBend(70), Head(15, 25, 0), Chest(22, 0, 0)));
        remonstrate.Keys.Add(Key(stand, 180, RFwd(40), RBend(80), LFwd(110), LBend(60), Head(15, -25, 0), Chest(22, 0, 0), RLeg(35)));
        remonstrate.Keys.Add(Key(stand, 160, RFwd(110), RBend(60), LFwd(40), LBend(80), Head(15, 25, 0), Chest(22, 0, 0)));
        remonstrate.Keys.Add(Key(stand, 160, RFwd(40), RBend(80), LFwd(110), LBend(60), Head(15, -25, 0), Chest(22, 0, 0), LLeg(35)));
        remonstrate.Keys.Add(Key(stand, 260, RUp(30), LUp(30), RBend(110), LBend(110), Head(-15, 0, 0), Chest(-8, 0, 0)));
        remonstrate.Keys.Add(Key(stand, 260, RUp(30), LUp(30), RBend(110), LBend(110), Head(25, 0, 0), Chest(25, 0, 0)));
        remonstrate.Keys.Add(Key(stand, 400));
        return (celebrate, shrug, remonstrate);
    }

    // The throw: the ball picked up, swung back, a stride on the other foot, let go low at 1.0 s, the arm following through up
    public static CharacterAnim MakeBowl(Rig r, byte[] standAnim)
    {
        var stand = CharacterAnim.Read(standAnim, r.Bones, 0, "stand").Keys[0];
        (int, float, float, float) Fwd(float deg) => (r.LUpper, -deg, 0, 0);
        (int, float, float, float) Bend(float deg) => (r.LFore, -deg, 0, 0);
        (int, float, float, float) Other(float deg) => (r.RUpper, -deg, 0, 0);
        (int, float, float, float) Chest(float x) => (r.Chest, x, 0, 0);
        (int, float, float, float) Step(float deg) => (r.RThigh, -deg, 0, 0);
        (int, float, float, float) Back(float deg) => (r.LThigh, -deg, 0, 0);
        var bowl = new CharacterAnim { Generic = Bowl, Name = "bowl", LoopFrame = 0 };
        bowl.Keys.Add(Key(stand, 1));
        bowl.Keys.Add(Key(stand, 300, Fwd(25), Bend(35), Chest(10), Other(10)));
        bowl.Keys.Add(Key(stand, 350, Fwd(-75), Bend(5), Chest(25), Other(35), Step(25)));
        bowl.Keys.Add(Key(stand, 350, Fwd(25), Bend(0), Chest(35), Other(-20), Step(45), Back(-25)));
        bowl.Keys.Add(Key(stand, 300, Fwd(115), Bend(20), Chest(22), Other(-10), Step(35), Back(-15)));
        bowl.Keys.Add(Key(stand, 500));
        return bowl;
    }

    // The poker players: their entity, the body generic they sit as, its arms' and legs' bones (the right +x; each shin the thigh's next), and
    // how far their hips come down to sit (thighs level at a chair's seat, about 330 up)
    internal sealed record Sitter(string Name, int Entity, int Generic, int RUpper, int RFore, int LUpper, int LFore, int RThigh, int LThigh, int Drop);
    internal static readonly Sitter[] Sitters =
    {
        new("the sailor", 297, 0, 7, 8, 12, 13, 17, 21, 430),
        new("the blue sailor", 295, 0, 12, 13, 7, 8, 21, 17, 234),
        new("a Franco dissident", 56, 93, 6, 7, 9, 10, 12, 16, 0),
    };

    // A sitter's poker: sitting, his thighs level and shins down, his hands on the table; looking at his cards, pushing chips in -- each at his
    // own pace (the three not in step)
    public static Anim MakePoker(Sitter p, Body body, (int Type, double A, double B, double C)[] basis, int k)
    {
        static Vector3 N(float x, float y, float z) => Vector3.Normalize(new Vector3(x, y, z));
        Dictionary<int, Vector3> Pose(Vector3 rUp, Vector3 rFore, Vector3 lUp, Vector3 lFore) => new()
        {
            [p.RThigh] = N(0.12f, -0.05f, 1), [p.RThigh + 1] = N(0.03f, -1, 0.12f),
            [p.LThigh] = N(-0.12f, -0.05f, 1), [p.LThigh + 1] = N(-0.03f, -1, 0.12f),
            [p.RUpper] = rUp, [p.RFore] = rFore, [p.LUpper] = lUp, [p.LFore] = lFore,
        };
        var rest = Pose(N(0.2f, -0.7f, 0.68f), N(-0.35f, 0.1f, 0.93f), N(-0.2f, -0.7f, 0.68f), N(0.35f, 0.1f, 0.93f));
        var cards = Pose(N(0.2f, -0.6f, 0.75f), N(-0.3f, 0.45f, 0.84f), N(-0.2f, -0.6f, 0.75f), N(0.3f, 0.45f, 0.84f));
        var chips = Pose(N(0.12f, -0.4f, 0.9f), N(-0.05f, -0.3f, 0.95f), N(-0.2f, -0.7f, 0.68f), N(0.35f, 0.1f, 0.93f));
        var a = new Anim { Game = 2, LoopFrame = 0 };
        AnimFrame F(int ms, Dictionary<int, Vector3> aims) => Citadel.CitadelSpanking.Frame(body, ms, basis, aims, -p.Drop, -1);
        a.Frames.Add(F(900 + 170 * k, rest));
        a.Frames.Add(F(700 + 90 * k, cards));
        a.Frames.Add(F(1300 + 210 * k, cards));
        a.Frames.Add(F(600, rest));
        a.Frames.Add(F(500 + 120 * k, chips));
        a.Frames.Add(F(700, rest));
        return a;
    }

    // The swordsman's body without the sword: faces, lines and spheres in the sword's colours on the hand's bone left out
    public static Body SwordlessBody(byte[] bytes)
    {
        var body = Body.Read(bytes, 2);
        var bone = body.Bones[SwordBone];
        bool Sword(int point, int colour) => point >= bone.Start && point < bone.Start + bone.Count && colour is 52 or 100;
        body.Faces.RemoveAll(f => f.Points.Any(q => Sword(q, f.Colour)));
        body.Lines.RemoveAll(l => Sword(l.A, l.Colour) || Sword(l.B, l.Colour));
        body.Spheres.RemoveAll(q => Sword(q.Point, q.Colour));
        return body;
    }

    // Into the game folder: the twelve animations at ANIM.HQR's end, each bowler's entity given its three (once: an entity that has them keeps them)
    public static List<string> Install(string gameDirectory)
    {
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        var entities = Lba2EntityTable.Parse(table).Entities;
        var log = new List<string>();
        foreach (var r in Rigs)
        {
            if (entities.FirstOrDefault(e => e.Id == r.Entity) is not { } ent) { log.Add($"the bowling alley's reactions: no entity {r.Entity} ({r.Name})"); continue; }
            if (!ent.Anims.Any(a => a.Generic == Bowl))
            {
                var at2 = HqrArchive.CountEntries(animPath);
                HqrWriter.AppendToFile(animPath, MakeBowl(r, HqrArchive.Open(animPath).Read(r.Stand)).Write(r.Bones));
                table = RaceTrackJumpAnim.WithAnim(table, r.Entity, Bowl, at2);
                log.Add($"the bowling alley's throw: {r.Name} (entity {r.Entity}) bowls as ANIM.HQR {at2} (animation {Bowl})");
            }
            if (ent.Anims.Any(a => a.Generic == Celebrate)) continue;
            var (c, s, m) = Make(r, HqrArchive.Open(animPath).Read(r.Stand));
            var at = HqrArchive.CountEntries(animPath);
            var file = File.ReadAllBytes(animPath);
            foreach (var a in new[] { c, s, m }) file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(a.Write(r.Bones)));
            File.WriteAllBytes(animPath, file);
            table = RaceTrackJumpAnim.WithAnim(table, r.Entity, Celebrate, at);
            table = RaceTrackJumpAnim.WithAnim(table, r.Entity, Shrug, at + 1);
            table = RaceTrackJumpAnim.WithAnim(table, r.Entity, Remonstrate, at + 2);
            log.Add($"the bowling alley's reactions: {r.Name} (entity {r.Entity}) celebrates, shrugs and remonstrates as ANIM.HQR {at}-{at + 2} (animations {Celebrate}-{Remonstrate})");
        }
        // the poker players' sitting (once each)
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        for (var k = 0; k < Sitters.Length; k++)
        {
            var p = Sitters[k];
            if (entities.FirstOrDefault(e => e.Id == p.Entity) is not { } ent) { log.Add($"the bowling alley's poker: no entity {p.Entity} ({p.Name})"); continue; }
            if (ent.Anims.Any(a => a.Generic == Poker)) continue;
            if (RaceTrackGuards.BodyOf(table, p.Entity, p.Generic) is not { } bodyIndex || !ent.Anims.Any(a => a.Generic == 0))
            { log.Add($"the bowling alley's poker: {p.Name} (entity {p.Entity}) has no body {p.Generic} or no standing"); continue; }
            var body = Body.Read(HqrArchive.Open(bodyPath).Read(bodyIndex), 2);
            var basis = Palace.PalaceTorturers.ReadFrames(HqrArchive.Open(animPath).Read(ent.Anims.First(a => a.Generic == 0).Anim), body.Bones.Count)[0].Bones;
            var at = HqrArchive.CountEntries(animPath);
            HqrWriter.AppendToFile(animPath, MakePoker(p, body, basis, k).Write());
            table = RaceTrackJumpAnim.WithAnim(table, p.Entity, Poker, at);
            log.Add($"the bowling alley's poker: {p.Name} (entity {p.Entity}) sits and plays as ANIM.HQR {at} (animation {Poker})");
        }
        // the sailor's pool shot (CitadelProps.PoolAims: bent over the table, the cue drawn back twice, through, held)
        if (entities.FirstOrDefault(e => e.Id == 297) is { } sailor && !sailor.Anims.Any(a => a.Generic == Pool) && RaceTrackGuards.BodyOf(table, 297, 0) is { } sailorBody
            && sailor.Anims.FirstOrDefault(a => a.Generic == 0).Anim is var sailorStand)
        {
            var body = Body.Read(HqrArchive.Open(bodyPath).Read(sailorBody), 2);
            var basis = Palace.PalaceTorturers.ReadFrames(HqrArchive.Open(animPath).Read(sailorStand), body.Bones.Count)[0].Bones;
            var pool = new Anim { Game = 2, LoopFrame = 0 };
            foreach (var (ms, stroke) in new[] { (1400, 1), (500, 0), (400, 1), (600, 0), (250, 2), (1800, 2), (3000, 1) })
                pool.Frames.Add(Citadel.CitadelSpanking.Frame(body, ms, basis, Citadel.CitadelProps.PoolAims(stroke), -Citadel.CitadelProps.PoolDrop, -1));
            var at = HqrArchive.CountEntries(animPath);
            HqrWriter.AppendToFile(animPath, pool.Write());
            table = RaceTrackJumpAnim.WithAnim(table, 297, Pool, at);
            log.Add($"the bowling alley's pool: the sailor (entity 297) plays a shot as ANIM.HQR {at} (animation {Pool})");
        }
        // the swordsman without his sword
        if (entities.FirstOrDefault(e => e.Id == SwordEntity) is { } sword && !sword.Bodies.Any(b => b.Generic == Swordless) && RaceTrackGuards.BodyOf(table, SwordEntity, 0) is { } own)
        {
            var at = HqrArchive.CountEntries(bodyPath);
            HqrWriter.AppendToFile(bodyPath, SwordlessBody(HqrArchive.Open(bodyPath).Read(own)).Write());
            table = RaceTrackBaldinoCar.WithBody(table, SwordEntity, Swordless, at);
            log.Add($"the bowling alley: the swordsman monkey without his sword, BODY.HQR {at} (entity {SwordEntity}'s body {Swordless})");
        }
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return log;
    }
}
