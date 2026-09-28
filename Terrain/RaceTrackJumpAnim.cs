using System.Buffers.Binary;
using System.IO;

namespace LBAAssembler.Terrain;

// The race track's jump flight: a longer copy of the retail car jump's (ANIM.HQR entry 51, which Twinsen plays as his generic animation 67 in
// the buggy), added to ANIM.HQR and given to his buggy entity (RESS.HQR entry 44, entity 12) under a generic number of its own, so the retail
// jump in scene 62 keeps its own flight. The flight is all in the animation: 18 keyframes whose root steps carry the car 8990 units forward
// and up to 1921 above where it starts, ending 201 below it, with the "master" bit (no gravity) on keyframes 1-17.
internal static class RaceTrackJumpAnim
{
    public const int RetailEntry = 51;
    // The generic animation number the hero's track script plays (ANIM(200)); the retail entities use numbers up to 83.
    public const int Generic = 200;
    // Twinsen's entity while he drives (behaviour C_BUGGY = 12: the engine loads entity n for behaviour n).
    public const int BuggyEntity = 12;
    public const double ForwardScale = 1.2, ClimbScale = 1.2, TimeScale = 1.1;
    // What the copy does, from the retail flight's steps (measured in the game: 17.4-17.6 cells): 21.1 cells, ending 241 below the start.
    public const double Distance = 8990 * ForwardScale / 512;
    public const double EndDrop = -201 * ClimbScale;

    // Adds the flight to the game folder's ANIM.HQR and RESS.HQR (the copies the build starts from are the originals, so this runs once
    // per build). Returns a line for the build's log.
    public static string Install(string gameDirectory)
    {
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var retail = HqrArchive.Open(animPath).Read(RetailEntry);
        var flight = Scale(retail);
        var index = HqrArchive.CountEntries(animPath);
        File.WriteAllBytes(animPath, HqrWriter.AppendEntry(File.ReadAllBytes(animPath), HqrWriter.StoredEntry(flight)));

        var ress = File.ReadAllBytes(ressPath);
        var table = HqrArchive.Open(ressPath).Read(44);
        table = WithAnim(table, BuggyEntity, Generic, index);
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(ress, 44, HqrWriter.StoredEntry(table)));
        return $"jump flight: ANIM.HQR entry {index} (entry {RetailEntry} with the steps forward x{ForwardScale}, the climb x{ClimbScale} and the time x{TimeScale}: " +
               $"{Distance:0.0} cells), played by Twinsen in the buggy as animation {Generic}";
    }

    // The retail flight with its keyframes' root steps and times scaled. Layout (ANIM.HQR): U16 keyframes, U16 bones, U16 loop frame, U16 0;
    // then per keyframe U16 time (ms), S16 step X, Y, Z, and 8 bytes per bone.
    public static byte[] Scale(byte[] anim)
    {
        var copy = (byte[])anim.Clone();
        int frames = BinaryPrimitives.ReadUInt16LittleEndian(copy), bones = BinaryPrimitives.ReadUInt16LittleEndian(copy.AsSpan(2));
        for (var f = 0; f < frames; f++)
        {
            var p = 8 + f * (8 + bones * 8);
            if (p + 8 > copy.Length) throw new InvalidDataException("The jump animation is shorter than its header says.");
            void Put(int at, double value) => BinaryPrimitives.WriteInt16LittleEndian(copy.AsSpan(at), (short)Math.Clamp(Math.Round(value), short.MinValue, short.MaxValue));
            var time = BinaryPrimitives.ReadUInt16LittleEndian(copy.AsSpan(p));
            BinaryPrimitives.WriteUInt16LittleEndian(copy.AsSpan(p), (ushort)Math.Clamp(Math.Round(time * TimeScale), 1, ushort.MaxValue));
            Put(p + 2, BinaryPrimitives.ReadInt16LittleEndian(copy.AsSpan(p + 2)) * ForwardScale);
            Put(p + 4, BinaryPrimitives.ReadInt16LittleEndian(copy.AsSpan(p + 4)) * ClimbScale);
            Put(p + 6, BinaryPrimitives.ReadInt16LittleEndian(copy.AsSpan(p + 6)) * ForwardScale);
        }
        return copy;
    }

    // The entity table (see Lba2EntityTable) with an animation record -- 3, generic number (U16), size 4, ANIM.HQR index (S16), no actions --
    // added to one entity just before its end mark (255), or its index changed when the entity already has that generic number. The entities
    // after it move, so their offsets do too.
    public static byte[] WithAnim(byte[] table, int entity, int generic, int animIndex)
    {
        var count = BinaryPrimitives.ReadInt32LittleEndian(table) / 4 - 1;
        if (entity < 0 || entity >= count) throw new InvalidDataException($"RESS.HQR has no entity {entity}.");
        var start = BinaryPrimitives.ReadInt32LittleEndian(table.AsSpan(entity * 4));
        var end = BinaryPrimitives.ReadInt32LittleEndian(table.AsSpan((entity + 1) * 4));
        var p = start;
        while (p < end && table[p] != 255)
        {
            var command = table[p];
            if (command == 3)
            {
                if ((table[p + 1] | table[p + 2] << 8) == generic)
                {
                    var same = (byte[])table.Clone();
                    BinaryPrimitives.WriteInt16LittleEndian(same.AsSpan(p + 4), (short)animIndex);
                    return same;
                }
                p += 3 + table[p + 3];
            }
            else p += 2 + table[p + 2];
        }
        if (p >= end) throw new InvalidDataException($"Entity {entity}'s records have no end mark.");
        var record = new byte[] { 3, (byte)generic, (byte)(generic >> 8), 4, (byte)animIndex, (byte)(animIndex >> 8), 0 };
        var result = new byte[table.Length + record.Length];
        table.AsSpan(0, p).CopyTo(result);
        record.CopyTo(result.AsSpan(p));
        table.AsSpan(p).CopyTo(result.AsSpan(p + record.Length));
        for (var i = entity + 1; i <= count; i++)
        {
            var at = i * 4;
            if (at + 4 > (count + 1) * 4 || at + 4 > result.Length) break;
            var offset = BinaryPrimitives.ReadInt32LittleEndian(result.AsSpan(at));
            if (offset >= p) BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(at), offset + record.Length);
        }
        return result;
    }
}
