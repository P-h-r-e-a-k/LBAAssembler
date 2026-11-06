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
    // The island's own coin telescope (cube (8,8), its decor at (24512, 4500, 6016): CITADEL's body 40, CITABAU's 48) copied onto the
    // lighthouse's balcony (cube (7,10): the tower, its decor at (13952, 3250, 11776), up to 7628), west of where Twinsen stands in the
    // spell's scene (track point 5, (13425, 7628, 11122)) -- he faces it there, his back to the lamp room.
    private const int ParkCubeX = 8, ParkCubeZ = 8, ParkX = 24512, ParkY = 4500, ParkZ = 6016;
    private const int TowerCubeX = 7, TowerCubeZ = 10, TowerX = 13952, TowerY = 3250, TowerZ = 11776, TowerTop = 7628;
    public const int ScopeX = 12880, ScopeY = TowerTop, ScopeZ = 11122;

    // The island's own telescope as it was before the build (a copy: the road may clear it), null on an island without one
    public static IslandDecor? Telescope(IslandFile island) =>
        island.CubeAt(ParkCubeX, ParkCubeZ)?.Decors.FirstOrDefault(d => d.X == ParkX && d.Y == ParkY && d.Z == ParkZ)?.Clone();

    public static void AddTelescope(IslandFile island, IslandDecor? scope, RaceTrackReport report)
    {
        if (island.CubeAt(TowerCubeX, TowerCubeZ) is not { } top
            || top.Decors.FirstOrDefault(d => d.X == TowerX && d.Y == TowerY && d.Z == TowerZ && d.YMax == TowerTop) is null) return;
        if (scope is null)
        {
            report.Notes.Add("WARNING: the lighthouse's telescope left out: the island's own telescope (cube (8,8)) isn't there");
            return;
        }
        if (top.Decors.Any(d => d.X == ScopeX && d.Y == ScopeY && d.Z == ScopeZ)) return;
        var copy = scope.Clone();
        copy.MoveTo(ScopeX, ScopeY, ScopeZ);
        // (turned half round from the park's: as copied (turn 1024) it faced the wrong way -- the user, 2026-11-05: "we should turn it
        // around 180 degrees". Its box turned with it: mirrored through its origin, x and z. Twinsen's place to use it, east of it facing
        // west (track point 5), is beside its post either way: the tube runs north-south, z -717..410 of the post now, 410..-717 before.)
        copy.Beta = (copy.Beta & ~0xFFFF) | (((copy.Beta & 0xFFFF) + 2048) & 4095);
        (copy.XMin, copy.XMax) = (2 * copy.X - copy.XMax, 2 * copy.X - copy.XMin);
        (copy.ZMin, copy.ZMax) = (2 * copy.Z - copy.ZMax, 2 * copy.Z - copy.ZMin);
        top.Decors.Add(copy);
        report.Notes.Add($"the island's telescope (body {scope.Body & 0xFFFF}) on the lighthouse's balcony, cube (7,10) at ({ScopeX}, {ScopeY}, {ScopeZ})");
    }

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
