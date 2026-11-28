using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Lba1;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// LBA1's hair dryer salesman, brought over whole for the story's Hoverboard (the user, 2026-11-27: "add in a sales person in a grey suit the
// same as the first game to walk around knocking on doors in the village area for Twinsen to approach to buy one"): LBA1's entity 15,
// "Business quetch" -- its one body (LBA1 BODY.HQR 29) and its animations (ANIM.HQR 160-163) -- as an LBA2 entity of its own:
//   - the body with its bones as LBA1 has them (the two games' bodies and their units match: LBA1's Twinsen walks 240 a frame, so does
//     LBA2's), lit, each colour moved to the LBA2 ramp nearest its own (RaceTrackCharacterCars.Lba1ToLba2Colours, as the digger's), its
//     polygons of more than four corners cut into triangles (LBA2 draws triangles and quads), LBA1's see-through ones see-through still;
//   - the animations with LBA1's angles in LBA2's (a turn is 1024 in LBA1, 4096 in LBA2) and its frame times in LBA2's milliseconds (LBA1
//     counts its 50 Hz timer's ticks: Twinsen's walk frame 9 there, 200 here), under the generic numbers LBA1 gives them (0 standing, 1
//     walking, KnockGeneric his knock at a door).
// A new entity at the entity table's end with these alone (none of another's), or the one this build gave him already, known by his body's
// bytes. Without LBA1's files: -1, and the story's salesman is the cycle shop's owner instead (RaceTrackStory.HairDryer).
internal static class HairDryerSalesman
{
    public const int Lba1Entity = 15, Lba1Body = 29;
    public const int Stand = 0, Walk = 1, Knock = 62, Reels = 6;
    // (LBA1's animation for each generic number: its entity's records -- 160 standing, 161 walking, 162 his stagger back as if hit (6, and
    // LBA1's 22), 163 his knock (LBA1's 62))
    public static readonly (int Lba1Anim, int Generic)[] Anims = { (160, Stand), (161, Walk), (162, Reels), (163, Knock) };
    // (LBA2's body whose header the converted one takes: an animated one's -- the errands' own header donor, RaceTrackStory.Errands)
    private const int HeaderDonor = 30;
    private const int LBA1TicksToMs = 20;

    public static Body Body(string lba1, string game)
    {
        var lba1Body = HqrArchive.Open(Path.Combine(lba1, "BODY.HQR")).Read(Lba1Body);
        var header = LbaBodyStudio.Body.Read(HqrArchive.Open(Path.Combine(game, "BODY.HQR")).Read(HeaderDonor), 2).Header;
        var colours = RaceTrackCharacterCars.Lba1ToLba2Colours(HqrArchive.Open(Path.Combine(lba1, "RESS.HQR")).Read(0), HqrArchive.Open(Path.Combine(game, "RESS.HQR")).Read(0));
        return ToLba2(LbaBodyStudio.Body.Read(lba1Body, 1), header, colours);
    }

    // An LBA1 body as an LBA2 one: its bones and points as they are, its faces lit (LBA1's lit types 7-10) or see-through (5, 6) or flat
    // (the rest), triangles or quads, its colours moved
    public static Body ToLba2(Body lba1, byte[] header, int[] colours)
    {
        var b = new Body { Game = 2, Lit = true, Header = (byte[])header.Clone(), Vertices = lba1.Vertices.ToList(), Bones = lba1.Bones.Select(x => x with { Record = new byte[38] }).ToList() };
        foreach (var f in lba1.Faces)
        {
            var material = f.Material >= 7 ? 1 : f.Material is 5 or 6 ? 2 : 0;
            var colour = colours[f.Colour & 255];
            if (f.Points.Length <= 4) b.Faces.Add(new Face(f.Points, colour, Material: material));
            else for (var k = 1; k + 1 < f.Points.Length; k++) b.Faces.Add(new Face(new[] { f.Points[0], f.Points[k], f.Points[k + 1] }, colour, Material: material));
        }
        b.Lines.AddRange(lba1.Lines.Select(l => l with { Colour = colours[l.Colour & 255] }));
        b.Spheres.AddRange(lba1.Spheres.Select(s => s with { Colour = colours[s.Colour & 255] }));
        return b;
    }

    // An LBA1 animation as an LBA2 one. Slot 0's type is its master flags, not a rotation's or a translation's: its angles (a turn the
    // frame makes) given in LBA2's raw units, as Anim.Write writes an odd type's values as they are.
    public static Anim ToLba2Anim(byte[] lba1)
    {
        var a = Lba1Animation.Parse(lba1) ?? throw new InvalidDataException("not an LBA1 animation");
        var anim = new Anim { Game = 2, LoopFrame = a.LoopFrame };
        foreach (var f in a.Frames)
            anim.Frames.Add(new AnimFrame(f.Length * LBA1TicksToMs, f.StepX, f.StepY, f.StepZ, f.Bones.Select((bone, i) =>
                i == 0 ? ((bone.Type & 1) == 0 ? new AnimBone(bone.Type, bone.X / 1024f, bone.Y / 1024f, bone.Z / 1024f) : new AnimBone(bone.Type, bone.X * 4, bone.Y * 4, bone.Z * 4))
                : (bone.Type & 1) == 0 ? new AnimBone(0, bone.X / 1024f, bone.Y / 1024f, bone.Z / 1024f)
                : new AnimBone(1, bone.X, bone.Y, bone.Z)).ToArray()));
        return anim;
    }

    // LBA1's hair dryer in the inventory (INVOBJ.HQR 19: its item 19's, "Hair dryer", text 119) as an LBA2 body, in the header given (the
    // errands' models': RaceTrackStory.Errands sizes it); null without LBA1's files
    public const int Lba1DryerObject = 19;
    public static Body? Dryer(byte[] header, string game)
    {
        var lba1 = RaceTrackService.Lba1Folder();
        if (string.IsNullOrEmpty(lba1) || !File.Exists(Path.Combine(lba1, "INVOBJ.HQR")) || !File.Exists(Path.Combine(lba1, "RESS.HQR"))) return null;
        var colours = RaceTrackCharacterCars.Lba1ToLba2Colours(HqrArchive.Open(Path.Combine(lba1, "RESS.HQR")).Read(0), HqrArchive.Open(Path.Combine(game, "RESS.HQR")).Read(0));
        return ToLba2(LbaBodyStudio.Body.Read(HqrArchive.Open(Path.Combine(lba1, "INVOBJ.HQR")).Read(Lba1DryerObject), 1), header, colours);
    }

    public static List<(int Generic, Anim Anim)> Animations(string lba1)
    {
        var archive = HqrArchive.Open(Path.Combine(lba1, "ANIM.HQR"));
        return Anims.Select(a => (a.Generic, ToLba2Anim(archive.Read(a.Lba1Anim)))).ToList();
    }

    // Into the game folder: his body, his animations and his entity -- its number (-1: no LBA1 here) and a line for the build's log
    public static (int Entity, string Log) Install(string gameDirectory)
    {
        var lba1 = RaceTrackService.Lba1Folder();
        if (string.IsNullOrEmpty(lba1) || !File.Exists(Path.Combine(lba1, "BODY.HQR")) || !File.Exists(Path.Combine(lba1, "ANIM.HQR")))
            return (-1, "the hair dryer salesman: no LBA1 folder (LBA1_DIR, or the editor's) -- the cycle shop's owner sells the dryers instead");
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var bodyBytes = Body(lba1, gameDirectory).Write();
        var table = HqrArchive.Open(ressPath).Read(44);
        var bodies = HqrArchive.Open(bodyPath);
        var bodyCount = HqrArchive.CountEntries(bodyPath);
        foreach (var e in Lba2EntityTable.Parse(table).Entities)
            if (e.Bodies.FirstOrDefault(b => b.Generic == 0) is { } first && first.Body < bodyCount && bodies.Read(first.Body).AsSpan().SequenceEqual(bodyBytes))
                return (e.Id, $"the hair dryer salesman: entity {e.Id} (this build's already)");
        (table, var entity) = WithEntityEmpty(table);
        File.WriteAllBytes(bodyPath, HqrWriter.AppendEntry(File.ReadAllBytes(bodyPath), HqrWriter.StoredEntry(bodyBytes)));
        table = RaceTrackBaldinoCar.WithBody(table, entity, 0, bodyCount);
        var animBytes = File.ReadAllBytes(animPath);
        var animIndex = HqrArchive.CountEntries(animPath);
        var anims = Animations(lba1);
        for (var k = 0; k < anims.Count; k++)
        {
            animBytes = HqrWriter.AppendEntry(animBytes, HqrWriter.StoredEntry(anims[k].Anim.Write()));
            table = RaceTrackJumpAnim.WithAnim(table, entity, anims[k].Generic, animIndex + k);
        }
        File.WriteAllBytes(animPath, animBytes);
        HqrWriter.ReplaceInFile(ressPath, 44, table);
        return (entity, $"the hair dryer salesman: LBA1's entity {Lba1Entity} as entity {entity} (BODY.HQR {bodyCount}, ANIM.HQR {animIndex}-{animIndex + anims.Count - 1}: generics {string.Join(", ", anims.Select(a => a.Generic))})");
    }

    // The entity table with a new entity at its end, with no records yet (RaceTrackCharacterCars.WithEntityLike, none of another's)
    private static (byte[] Table, int Entity) WithEntityEmpty(byte[] table)
    {
        var count = BinaryPrimitives.ReadInt32LittleEndian(table) / 4 - 1;
        var offsets = Enumerable.Range(0, count + 1).Select(i => BinaryPrimitives.ReadInt32LittleEndian(table.AsSpan(i * 4))).ToArray();
        var data = table.AsSpan(offsets[0], offsets[count] - offsets[0]).ToArray();
        var result = new byte[(count + 2) * 4 + data.Length + 1];
        for (var i = 0; i <= count; i++) BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(i * 4), offsets[i] + 4);
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan((count + 1) * 4), offsets[count] + 4 + 1);
        data.CopyTo(result.AsSpan((count + 2) * 4));
        result[^1] = 255;
        return (result, count);
    }
}
