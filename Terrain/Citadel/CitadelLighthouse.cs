using System.Buffers.Binary;
using System.IO;
using System.Linq;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The lighthouse's telescope and the Wizard's hop (the user, 2026-11-03: "The lighthouse in Citadel.ile and Citabau.ile let's allow Twinsen
// to enter the door and get to the top. Let's also add in some binoculars like the ones already on Citadel island to the top of the
// lighthouse - the same area Twinsen stands in the scripted segment. If Twinsen uses the telescope let's have a small scripted cut scene of
// the weather wizard standing behind the cow and using Twinsen's hop forward with the 'x' key animation and the cow producing clovers the
// same as it does when Twinsen does this"; "this should only be after Raph has already unlocked it and not before"). The scenes' side is
// RaceTrackStory.Chapters (LighthouseClimb); here: the telescope on the lamp room's balcony, and the Wizard's hop.
internal static class CitadelLighthouse
{
    // ---- the telescope ----------------------------------------------------------------------------------------------------------------
    // On the lighthouse's balcony (cube (7,10): the tower, its decor at (13952, 3250, 11776), its floor up at 7628; the west railing x
    // 12223-12439, its top at 8331), west of where Twinsen stands in the spell's scene (track point 5, (13425, 7628, 11122)) -- he faces it
    // there, his back to the lamp room. A spyglass of the story's on the west railing's top (CitadelProps.RailScope, an actor of scene 46:
    // RaceTrackStory.Chapters' LighthouseClimb). Until 2026-11-09 the island's coin telescope from the park (cube (8,8)) was copied there,
    // post and all: "it's sticking out too far, we could probably mount it on the side of the lighthouse" (the user, 2026-11-08).
    // (on the north railing -- x 12223-13736, z 10317-10479, its top at 8168 -- west of the lamp room, the side towards the ship; aimed across
    // the island at the cow's field, scene 47's cube (8,7), where Rosa and the Wizard are: from the lighthouse, cube (7,10), that is north
    // and a little east -- the user, 2026-11-10: "should be pointing towards the other end of the island near the cow - so it needs moving
    // onto the wall nearest to the ship". Twinsen uses it facing north. Its tube is built along -x (CitadelProps.BuildRailScope): turned
    // by ScopeBeta onto the cow's bearing.)
    public const int ScopeX = 12850, ScopeY = 8168, ScopeZ = 10398;
    private const int CowIslandX = 8 * 32768 + 22600, CowIslandZ = 7 * 32768 + 23552, TopIslandX = 7 * 32768 + 13425, TopIslandZ = 10 * 32768 + 11122;
    public static int ScopeBeta => (RaceTrackMath.Turn(Math.Atan2(CowIslandX - TopIslandX, CowIslandZ - TopIslandZ)) - 3072) & 4095;

    // ---- the Wizard's hop ---------------------------------------------------------------------------------------------------------------
    // Twinsen's hop forward (entity 0's animation 42, the one the cow counts) on the weather wizard (entity 67, BODY.HQR 111: 21 bones to
    // Twinsen's 24 -- his robe's four his own): each bone the Wizard shares with Twinsen turned from the Wizard's own standing as far as
    // Twinsen's turns from his, his hands and robe as he stands. In place: the hop's steps left out, he hops where he stands behind her.
    public const int Wizard = 67, WizardBody = 111, Hop = 643;
    private const int Twinsen = 0, TwinsenHop = 42;
    // (the Wizard's bone slots, each from Twinsen's: -1 his own standing)
    private static readonly int[] FromTwinsen = { -1, 1, 2, 3, 4, 5, -1, 6, 7, -1, 8, 9, 10, 11, 12, 13, 14, -1, -1, -1, -1 };

    // Into the game folder: the animation (ANIM.HQR's end) as the Wizard's Hop (RESS.HQR's entity table). Lines for the build's log.
    public static List<string> Install(string gameDirectory)
    {
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        var entities = Lba2EntityTable.Parse(table).Entities;
        var wizard = entities.FirstOrDefault(e => e.Id == Wizard);
        var twinsen = entities.FirstOrDefault(e => e.Id == Twinsen);
        int AnimOf(Lba2EntityTable.Entity? e, int generic) => e is not null && e.Anims.Any(a => a.Generic == generic) ? e.Anims.First(a => a.Generic == generic).Anim : -1;
        if (wizard is null || RaceTrackGuards.BodyOf(table, Wizard, 0) != WizardBody || AnimOf(wizard, 0) < 0 || AnimOf(twinsen, 0) < 0 || AnimOf(twinsen, TwinsenHop) < 0)
            return new() { $"WARNING: the Wizard's hop: entity {Wizard} isn't the game's weather wizard (BODY.HQR {WizardBody}), or Twinsen's hop isn't there -- not made" };
        if (wizard.Anims.Any(a => a.Generic == Hop)) return new();
        var anims = HqrArchive.Open(animPath);
        var hop = WizardHop(anims.Read(AnimOf(twinsen, TwinsenHop)), anims.Read(AnimOf(twinsen, 0)), anims.Read(AnimOf(wizard, 0)));
        var animIndex = HqrArchive.CountEntries(animPath);
        File.WriteAllBytes(animPath, HqrWriter.AppendEntry(File.ReadAllBytes(animPath), HqrWriter.StoredEntry(hop.Write())));
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(RaceTrackJumpAnim.WithAnim(table, Wizard, Hop, animIndex))));
        return new() { $"the Wizard's hop: ANIM.HQR {animIndex} ({hop.Frames.Count} keyframes, Twinsen's hop forward, ANIM.HQR {AnimOf(twinsen, TwinsenHop)}), entity {Wizard}'s animation {Hop}" };
    }

    public static Anim WizardHop(byte[] hop, byte[] twinsenStand, byte[] wizardStand)
    {
        var frames = Raw(hop);
        var tw = Raw(twinsenStand)[0].Bones;
        var wz = Raw(wizardStand)[0].Bones;
        if (wz.Length < FromTwinsen.Length || tw.Length <= FromTwinsen.Max() || frames[0].Bones.Length <= FromTwinsen.Max())
            throw new InvalidDataException("The Wizard's or Twinsen's animations aren't the game's: too few bones.");
        var anim = new Anim { Game = 2, LoopFrame = BinaryPrimitives.ReadUInt16LittleEndian(hop.AsSpan(4)) };
        foreach (var (time, v) in frames)
        {
            var bones = new AnimBone[FromTwinsen.Length];
            bones[0] = AnimBone.Neutral;
            for (var b = 1; b < bones.Length; b++)
            {
                var own = wz[b];
                var m = FromTwinsen[b];
                var (x, y, z) = (own.X, own.Y, own.Z);
                if (m >= 0 && (v[m].Type & 1) == (own.Type & 1) && (tw[m].Type & 1) == (own.Type & 1))
                    (x, y, z) = (x + v[m].X - tw[m].X, y + v[m].Y - tw[m].Y, z + v[m].Z - tw[m].Z);
                bones[b] = (own.Type & 1) == 1 ? new AnimBone(1, x, y, z) : new AnimBone(0, x / 4096f, y / 4096f, z / 4096f);
            }
            anim.Frames.Add(new AnimFrame(time, 0, 0, 0, bones));
        }
        return anim;
    }

    // An animation's keyframes as the file has them: each bone slot's type and three values (an angle's 4096ths of a turn, a translation's units)
    private static List<(int Time, (int Type, int X, int Y, int Z)[] Bones)> Raw(byte[] d)
    {
        int frames = BinaryPrimitives.ReadUInt16LittleEndian(d), have = BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(2));
        var list = new List<(int, (int, int, int, int)[])>();
        for (var f = 0; f < frames; f++)
        {
            var at = 8 + f * (8 + have * 8);
            var bones = new (int, int, int, int)[have];
            for (var b = 0; b < have; b++)
            {
                var q = at + 8 + b * 8;
                short S(int o) => BinaryPrimitives.ReadInt16LittleEndian(d.AsSpan(q + o));
                var type = S(0) & 1;
                // (an angle as the engine reads it: 12 bits, signed)
                int A(int o) => type == 1 ? S(o) : ((S(o) & 0xFFF) ^ 0x800) - 0x800;
                bones[b] = (type, A(2), A(4), A(6));
            }
            list.Add((BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(at)), bones));
        }
        return list;
    }
}
