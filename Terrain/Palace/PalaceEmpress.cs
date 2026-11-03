using System.IO;
using System.Linq;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Palace;

// The Empress on her feet. The game only ever shows her in bed: every animation of hers (entity 331, BODY.HQR 454: 17 bones, a skeleton
// no other body has, so none of the Emperor's fit) has her hips 452 lower and her pelvis tipped 66 degrees back. Her standing is her own
// talking in bed (generic 28, ANIM.HQR 2023) with the hips back up, the pelvis, waist and legs straight, the chest keeping its breathing,
// and the head and both arms keeping the angles they had to the room -- the bed's lean given back to them -- so she still looks ahead,
// her hands held before her, the one arm talking as it did.
internal static class PalaceEmpress
{
    public const int Entity = 331, Retail = 454, Stand = 642;
    private const int RetailTalk = 28;
    private const int Hips = 1, Pelvis = 2, Waist = 3, Chest = 4, Head = 6, TalkingArm = 7, OtherArm = 10, FirstLeg = 13, Bones = 17;

    // Into the game folder: the animation (ANIM.HQR's end) as the entity's generic Stand (RESS.HQR's entity table). Lines for the build's log.
    public static List<string> Install(string gameDirectory)
    {
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        var entity = Lba2EntityTable.Parse(table).Entities.FirstOrDefault(e => e.Id == Entity);
        if (entity is null || RaceTrackGuards.BodyOf(table, Entity, 0) != Retail || !entity.Anims.Any(a => a.Generic == RetailTalk))
            return new() { $"WARNING: the Empress standing: entity {Entity} isn't the game's Empress (BODY.HQR {Retail}, animation {RetailTalk}) -- not made" };
        if (entity.Anims.Any(a => a.Generic == Stand)) return new();
        var talk = HqrArchive.Open(animPath).Read(entity.Anims.First(a => a.Generic == RetailTalk).Anim);
        var stand = Standing(talk);
        var animIndex = HqrArchive.CountEntries(animPath);
        File.WriteAllBytes(animPath, HqrWriter.AppendEntry(File.ReadAllBytes(animPath), HqrWriter.StoredEntry(stand.Write())));
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(RaceTrackJumpAnim.WithAnim(table, Entity, Stand, animIndex))));
        return new() { $"the Empress standing: ANIM.HQR {animIndex} ({stand.Frames.Count} keyframes, from her talking in bed, ANIM.HQR {entity.Anims.First(a => a.Generic == RetailTalk).Anim}), entity {Entity}'s animation {Stand}" };
    }

    public static Anim Standing(byte[] talk)
    {
        var frames = PalaceTorturers.ReadFrames(talk, Bones);
        if (frames.Count == 0 || frames[0].Bones[Hips].Type != 1) throw new InvalidDataException("The Empress's talking isn't the game's: no hips' translation.");
        var chest0 = frames[0].Bones[Chest].A;
        var anim = new Anim { Game = 2, LoopFrame = 0 };
        foreach (var (time, v) in frames)
        {
            // (how far back the bed tipped her upper body -- pelvis, waist, chest -- beyond the chest's own breathing)
            var lean = v[Pelvis].A + v[Waist].A + chest0;
            var bones = new AnimBone[Bones];
            for (var b = 0; b < Bones; b++)
            {
                var (type, x, y, z) = v[b];
                if (b == Hips) { bones[b] = new AnimBone(1, 0, 0, 0); continue; }
                if (b is Pelvis or Waist || b >= FirstLeg) (x, y, z) = (0, 0, 0);
                else if (b == Chest) x -= chest0;
                else if (b is Head or TalkingArm or OtherArm) x += lean;
                bones[b] = b == 0 ? AnimBone.Neutral : new AnimBone(0, (float)(x / (2 * Math.PI)), (float)(y / (2 * Math.PI)), (float)(z / (2 * Math.PI)));
            }
            anim.Frames.Add(new AnimFrame(time, 0, 0, 0, bones));
        }
        return anim;
    }
}
