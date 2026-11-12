using System.IO;
using System.Numerics;
using LBAAssembler.Characters;

namespace LBAAssembler.Terrain;

// Twinsen's swimming (the Imperial Hotel's pool, Citadel/CitadelProps.Pool; RACEMOD.CPP pool=): the game has no swimming for him, so his
// animations for it are the build's -- Swim, a front crawl lying along the water (the whole body turned forward about his feet, the arms
// reaching over one after the other, the legs kicking) and Tread, upright, the arms sculling and the legs pedalling -- added to ANIM.HQR and
// to every one of his entities (the hero's 0 and his sporty, aggressive and discreet ones 1-3: the engine plays them whichever he is in).
// The engine moves him itself in the water (his keys taken, as the lifts take them) and plays one of the two.
internal static class PoolSwim
{
    public const int Swim = 485, Tread = 486;
    // Twinsen's own: the sporty one's dive (generic 76, ANIM.HQR 76: the hotel's dive) and the dodge back, the backflip (43, ANIM.HQR 53) --
    // given to the entities that lack them
    public const int Dive = 76, DiveEntry = 76, Flip = 43, FlipEntry = 53;
    private static readonly int[] Entities = { 0, 1, 2, 3 };

    // Bone slots of Twinsen's rig: 3 torso, 4/5 his right arm, 6/7 left, 8/9/10 right leg, 11/12/13 left, 14 head
    private static CharacterAnim.Key Key(int ms, Action<Action<int, float, float, float>> pose, Vector3? root = null)
    {
        var k = new CharacterAnim.Key { Milliseconds = ms, Bones = Enumerable.Range(0, 19).Select(_ => (0, Vector3.Zero)).ToList() };
        if (root is { } r) k.Bones[0] = (0, r);
        pose((bone, x, y, z) => k.Bones[bone] = (0, new Vector3(x, y, z)));
        return k;
    }

    public static CharacterAnim SwimAnim()
    {
        var a = CharacterAnim.Still(19, Swim, "swim");
        a.Keys.Clear();
        // the whole body lying forward (the root's turn about x: a quarter turn, 1024 of 4096), the head up to look ahead
        const float Lie = 85;   // (bone 2 turned forward about x: the hips' bone and all that hangs from it, about his hips)
        // the stroke: one arm reaches forward over the head while the other pulls back by the hip; the legs kick the other way
        (float Reach, float Pull, float KickA, float KickB, float Roll)[] beats =
        {
            (-170, -15, 22, -22, 8), (-100, -60, 0, 0, 0), (-15, -170, -22, 22, -8), (-60, -100, 0, 0, 0),
        };
        foreach (var (reach, pull, kickA, kickB, roll) in beats)
            a.Keys.Add(Key(200, set =>
            {
                set(2, Lie, 0, 0);
                set(3, 0, roll, 0);
                set(4, reach, 0, 0); set(5, -15, 0, 0);
                set(6, pull, 0, 0); set(7, -15, 0, 0);
                set(8, kickA, 0, 0); set(9, kickA > 0 ? 25 : 5, 0, 0);
                set(11, kickB, 0, 0); set(12, kickB > 0 ? 25 : 5, 0, 0);
                set(14, -70, -roll, 0);
            }));
        a.LoopFrame = 0;
        return a;
    }

    public static CharacterAnim TreadAnim()
    {
        var a = CharacterAnim.Still(19, Tread, "tread water");
        a.Keys.Clear();
        (float Arm, float Leg)[] beats = { (30, 30), (55, -10), (30, -30), (5, 10) };
        foreach (var (arm, leg) in beats)
            a.Keys.Add(Key(280, set =>
            {
                // (the arms out at the sides, sculling in and out; the legs pedalling)
                set(4, 0, 0, arm + 20); set(5, 0, 0, 15);
                set(6, 0, 0, -arm - 20); set(7, 0, 0, -15);
                set(8, leg, 0, 0); set(9, leg > 0 ? 0 : 35, 0, 0);
                set(11, -leg, 0, 0); set(12, leg > 0 ? 35 : 0, 0, 0);
            }));
        a.LoopFrame = 0;
        return a;
    }

    // Into the game folder: the two animations at the end of ANIM.HQR, given to the hero's entities. Once: where the entity has them already
    // they stay. Lines for the build's log.
    public static List<string> Install(string gameDirectory)
    {
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        var entities = Lba2EntityTable.Parse(table).Entities;
        // (his dive and backflip, wherever an entity of his lacks one)
        foreach (var e in Entities)
            if (entities.FirstOrDefault(x => x.Id == e) is { } ent)
            {
                if (!ent.Anims.Any(a => a.Generic == Dive)) table = RaceTrackJumpAnim.WithAnim(table, e, Dive, DiveEntry);
                if (!ent.Anims.Any(a => a.Generic == Flip)) table = RaceTrackJumpAnim.WithAnim(table, e, Flip, FlipEntry);
            }
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        var todo = Entities.Where(e => entities.Any(x => x.Id == e && !x.Anims.Any(a => a.Generic is Swim or Tread))).ToList();
        if (todo.Count == 0) return new() { "Twinsen's swimming: the entities have it already" };
        var animIndex = HqrArchive.CountEntries(animPath);
        var file = HqrWriter.AppendEntry(File.ReadAllBytes(animPath), HqrWriter.StoredEntry(SwimAnim().Write(19)));
        File.WriteAllBytes(animPath, HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(TreadAnim().Write(19))));
        foreach (var e in todo)
        {
            table = RaceTrackJumpAnim.WithAnim(table, e, Swim, animIndex);
            table = RaceTrackJumpAnim.WithAnim(table, e, Tread, animIndex + 1);
        }
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return new() { $"Twinsen's swimming: ANIM.HQR {animIndex} (the crawl, animation {Swim}) and {animIndex + 1} (treading water, {Tread}), given to entities {string.Join(", ", todo)}" };
    }
}
