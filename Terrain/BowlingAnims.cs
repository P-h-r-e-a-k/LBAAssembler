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
    // (2026-11-18, the user: "Our pool players in the bowling alley don't appear to actually be playing" -- the shot was one looping
    // animation of him swinging at nothing, and nothing on the cloth ever moved. It is three now, and his TRACK plays them in turn, so the
    // strike falls on the frame the ball is struck: PoolStroke drawn back and through to the contact, then PoolFollow the follow-through
    // back to his stance, with Pool itself his stance between shots. A track can set an animation and wait_anim() for it -- that is what
    // keeps the balls, the sound and his arms in step, where a separate looping track would have drifted apart from the animation.)
    public const int PoolStroke = 646, PoolFollow = 647, PokerWin = 648;
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
        // (each table its own players -- the user, 2026-11-16: "We have two poker tables in the bowling area, both of them with two identical
        // NPCs, this needs work, they need to add interest and have differences": the glooms in a tie and in grey (entity 204's bodies 150,
        // 151, one rig), an old Wannie, the Franco from the gas station)
        new("a gloom in a tie", 204, 150, 9, 10, 13, 14, 17, 21, 250),
        new("an old Wannie", 274, 0, 7, 8, 12, 13, 17, 21, 185),
        new("the gas station's Franco", 203, 0, 6, 7, 9, 10, 12, 16, 0),
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
        // (2026-11-18, the user: "Our poker players are very static also". They had three poses and barely moved out of one of them. These
        // are bigger and there are more of them: his cards up to his face to study them, sitting back from the table with his hands off it,
        // a finger tapping the felt, a card laid down -- and the loop is rotated by player, so no two are doing the same thing at once.)
        var look = Pose(N(0.3f, -0.3f, 0.6f), N(-0.25f, 0.82f, 0.5f), N(-0.3f, -0.3f, 0.6f), N(0.25f, 0.82f, 0.5f));
        var sitBack = Pose(N(0.42f, -0.78f, 0.3f), N(-0.45f, -0.15f, 0.78f), N(-0.42f, -0.78f, 0.3f), N(0.45f, -0.15f, 0.78f));
        var tap = Pose(N(0.18f, -0.5f, 0.82f), N(-0.22f, 0.5f, 0.84f), N(-0.2f, -0.7f, 0.68f), N(0.35f, 0.1f, 0.93f));
        var lay = Pose(N(0.1f, -0.52f, 0.85f), N(-0.08f, -0.1f, 0.99f), N(-0.2f, -0.7f, 0.68f), N(0.35f, 0.1f, 0.93f));
        var a = new Anim { Game = 2, LoopFrame = 0 };
        AnimFrame F(int ms, Dictionary<int, Vector3> aims) => Citadel.CitadelSpanking.Frame(body, ms, basis, aims, -p.Drop, -1);
        var beats = new (Dictionary<int, Vector3> Pose, int Ms)[]
        {
            (rest, 900), (look, 650), (look, 1400), (rest, 600), (chips, 500), (rest, 800),
            (sitBack, 700), (sitBack, 1500), (rest, 650), (tap, 260), (rest, 240), (tap, 260), (rest, 700),
            (lay, 450), (rest, 900), (look, 700), (rest, 1100),
        };
        // (each man's own pace, and his own place in the round: the pot is not studied, pushed and tapped by three men in step)
        for (var i = 0; i < beats.Length; i++)
        {
            var (pose, ms) = beats[(i + 5 * k) % beats.Length];
            a.Frames.Add(F(ms + 40 * k, pose));
        }
        return a;
    }

    // Winning the pot, sitting (the user, same evening: "someone winning the pot (a cheer/groan) every so often"). NOT the bowlers'
    // Celebrate: that one is built from a standing pose, so a seated player would have straightened his legs and risen through the table.
    // This keeps the poker thighs exactly as they are and throws only the arms up, twice, then gathers the pot in.
    public static Anim MakePokerWin(Sitter p, Body body, (int Type, double A, double B, double C)[] basis, int k)
    {
        static Vector3 N(float x, float y, float z) => Vector3.Normalize(new Vector3(x, y, z));
        Dictionary<int, Vector3> Pose(Vector3 rUp, Vector3 rFore, Vector3 lUp, Vector3 lFore) => new()
        {
            [p.RThigh] = N(0.12f, -0.05f, 1), [p.RThigh + 1] = N(0.03f, -1, 0.12f),
            [p.LThigh] = N(-0.12f, -0.05f, 1), [p.LThigh + 1] = N(-0.03f, -1, 0.12f),
            [p.RUpper] = rUp, [p.RFore] = rFore, [p.LUpper] = lUp, [p.LFore] = lFore,
        };
        var rest = Pose(N(0.2f, -0.7f, 0.68f), N(-0.35f, 0.1f, 0.93f), N(-0.2f, -0.7f, 0.68f), N(0.35f, 0.1f, 0.93f));
        var up = Pose(N(0.45f, 0.86f, 0.25f), N(0.1f, 0.99f, 0.05f), N(-0.45f, 0.86f, 0.25f), N(-0.1f, 0.99f, 0.05f));
        var half = Pose(N(0.4f, 0.4f, 0.5f), N(0, 0.9f, 0.4f), N(-0.4f, 0.4f, 0.5f), N(0, 0.9f, 0.4f));
        // (and gathering it in: both arms out to the pot and back to him)
        var gather = Pose(N(0.2f, -0.45f, 0.87f), N(-0.1f, -0.25f, 0.96f), N(-0.2f, -0.45f, 0.87f), N(0.1f, -0.25f, 0.96f));
        var a = new Anim { Game = 2 };
        AnimFrame F(int ms, Dictionary<int, Vector3> aims) => Citadel.CitadelSpanking.Frame(body, ms, basis, aims, -p.Drop, -1);
        foreach (var (ms, pose) in new[] { (260, up), (240, half), (220, up), (320, half), (420, gather), (520, gather), (480, rest) })
            a.Frames.Add(F(ms + 20 * k, pose));
        // (played once by his track, which wait_anim()s for it: it holds him at rest at the end rather than throwing his arms up again)
        a.LoopFrame = a.Frames.Count - 1;
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
            if (ent.Anims.Any(a => a.Generic == PokerWin)) continue;
            if (RaceTrackGuards.BodyOf(table, p.Entity, p.Generic) is not { } bodyIndex || !ent.Anims.Any(a => a.Generic == 0))
            { log.Add($"the bowling alley's poker: {p.Name} (entity {p.Entity}) has no body {p.Generic} or no standing"); continue; }
            var body = Body.Read(HqrArchive.Open(bodyPath).Read(bodyIndex), 2);
            var basis = Palace.PalaceTorturers.ReadFrames(HqrArchive.Open(animPath).Read(ent.Anims.First(a => a.Generic == 0).Anim), body.Bones.Count)[0].Bones;
            var at = HqrArchive.CountEntries(animPath);
            HqrWriter.AppendToFile(animPath, MakePoker(p, body, basis, k).Write());
            table = RaceTrackJumpAnim.WithAnim(table, p.Entity, Poker, at);
            log.Add($"the bowling alley's poker: {p.Name} (entity {p.Entity}) sits and plays as ANIM.HQR {at} (animation {Poker})");
            // (and his cheer when the pot is his: seated, so he does not rise through the table -- MakePokerWin)
            var won = HqrArchive.CountEntries(animPath);
            HqrWriter.AppendToFile(animPath, MakePokerWin(p, body, basis, k).Write());
            table = RaceTrackJumpAnim.WithAnim(table, p.Entity, PokerWin, won);
            log.Add($"the bowling alley's poker: {p.Name} (entity {p.Entity}) wins the pot as ANIM.HQR {won} (animation {PokerWin})");
        }
        // the sailor's pool shot (CitadelProps.PoolAims: bent over the table, the cue drawn back, through, held) -- his stance, his stroke to
        // the contact and his follow-through, three animations his track plays in turn (BowlingAlley: the white rolls on the stroke's end)
        if (entities.FirstOrDefault(e => e.Id == 297) is { } sailor && !sailor.Anims.Any(a => a.Generic == PoolFollow) && RaceTrackGuards.BodyOf(table, 297, 0) is { } sailorBody
            && sailor.Anims.FirstOrDefault(a => a.Generic == 0).Anim is var sailorStand)
        {
            var body = Body.Read(HqrArchive.Open(bodyPath).Read(sailorBody), 2);
            var basis = Palace.PalaceTorturers.ReadFrames(HqrArchive.Open(animPath).Read(sailorStand), body.Bones.Count)[0].Bones;
            // (`once`: the clip holds its last pose instead of looping -- LoopFrame on the final keyframe, the way AnimGenerator's landings
            // do it. The stroke and the follow-through are played once each by his track, which wait_anim()s for them; a clip that looped
            // round to its first pose would snap him from the contact back to drawn-back, and wait_anim() has no end to wait for.)
            Anim Shot(bool once, params (int Ms, int Stroke)[] frames)
            {
                var a = new Anim { Game = 2 };
                foreach (var (ms, stroke) in frames)
                    a.Frames.Add(Citadel.CitadelSpanking.Frame(body, ms, basis, Citadel.CitadelProps.PoolAims(stroke), -Citadel.CitadelProps.PoolDrop, -1));
                a.LoopFrame = once ? a.Frames.Count - 1 : 0;
                return a;
            }
            // his stance over the table, sighting the shot (stroke 3 is at the ball a touch back); the stroke drawn back and through to the
            // contact, which is where it ends -- the track strikes the ball there; then the follow-through and back to his stance
            var shots = new (int Generic, Anim Anim, string What)[]
            {
                (Pool, Shot(false, (900, 1), (700, 3), (900, 1)), "stands over the table"),
                (PoolStroke, Shot(true, (400, 0), (150, 1)), "draws back and strikes"),
                (PoolFollow, Shot(true, (200, 2), (900, 2), (800, 1)), "follows through"),
            };
            foreach (var (generic, anim, what) in shots)
            {
                if (sailor.Anims.Any(a => a.Generic == generic) && generic != Pool) continue;
                var at = HqrArchive.CountEntries(animPath);
                HqrWriter.AppendToFile(animPath, anim.Write());
                table = RaceTrackJumpAnim.WithAnim(table, 297, generic, at);
                log.Add($"the bowling alley's pool: the sailor (entity 297) {what} as ANIM.HQR {at} (animation {generic})");
            }
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
