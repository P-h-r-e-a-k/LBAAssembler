using System.IO;
using System.Numerics;
using LBAAssembler.Characters;

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
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return log;
    }
}
