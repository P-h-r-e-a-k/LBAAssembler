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
    // Each island's jump has a flight of its own, sized to its gap, so several tracks can be built into one folder: 200 plus the island's
    // number (a scene's island byte), and 12 more for the track of its other-weather file (Citadel Island's town circuit) -- 200 to 223, the
    // numbers the engine's race-track mode flies at the car's speed (RACEMOD.CPP RACE_JUMP_ANIM_FIRST..LAST).
    public const int TwinOffset = 12, Last = Generic + 2 * TwinOffset - 1;
    public static int GenericFor(RaceTrackIsland island, bool twin = false) => Generic + island.IslandByte + (twin ? TwinOffset : 0);
    // Twinsen's entity while he drives (behaviour C_BUGGY = 12: the engine loads entity n for behaviour n).
    public const int BuggyEntity = 12;
    // The retail flight's steps (measured in the game: 17.4-17.6 cells): 8990 units forward, ending 201 below where it starts.
    public const double RetailForward = 8990, RetailEndDrop = -201;

    // A copy `forward` times as long climbs a little less than that much higher and takes a little longer, so the arc keeps its look:
    // x1.3 forward is x1.25 up and x1.15 the time.
    public static double ClimbScale(double forward) => 1 + (forward - 1) * 0.85;
    public static double TimeScale(double forward) => 1 + (forward - 1) * 0.5;
    public static double Distance(double forward) => RetailForward * forward / 512;
    public static double EndDrop(double forward) => RetailEndDrop * ClimbScale(forward);
    // The retail flight's keyframes: time (ms), the root's step forward and up (ANIM.HQR 51).
    private static readonly (int Ms, int Forward, int Up)[] RetailSteps =
    {
        (100, 440, 0), (100, 564, 526), (100, 564, 131), (100, 564, 131), (100, 564, 263), (100, 564, 190), (100, 564, 190), (80, 450, 190), (100, 564, 190),
        (100, 564, 55), (100, 564, 55), (100, 564, -244), (100, 564, -122), (100, 564, -122), (100, 333, -332), (100, 333, -506), (100, 333, -615), (100, 333, -181),
    };

    // How long a flight `forward` times the retail one takes, and how high above its start it is `cells` along (the steps are covered
    // evenly over each keyframe).
    public static double Seconds(double forward) => RetailSteps.Sum(f => f.Ms) * TimeScale(forward) / 1000;

    public static double Climb(double forward, double cells)
    {
        var at = cells * 512 / forward; double gone = 0, up = 0;
        foreach (var (_, f, u) in RetailSteps)
        {
            if (gone + f >= at) return (up + u * (at - gone) / f) * ClimbScale(forward);
            gone += f; up += u;
        }
        return up * ClimbScale(forward);
    }

    // The scale that flies `cells` (never shorter than the retail flight).
    public static double ForwardFor(double cells) => Math.Max(1, Math.Ceiling(cells * 512 / RetailForward * 100) / 100);

    // Adds a flight `forward` times the retail one to the game folder's ANIM.HQR and RESS.HQR, as Twinsen's generic animation `generic` in
    // the buggy (the copies the build starts from are the originals, so this runs once per jump a build has). Returns a line for the build's log.
    public static string Install(string gameDirectory, double forward, int generic = Generic)
    {
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var retail = HqrArchive.Open(animPath).Read(RetailEntry);
        var flight = Scale(retail, forward);
        var index = HqrArchive.CountEntries(animPath);
        File.WriteAllBytes(animPath, HqrWriter.AppendEntry(File.ReadAllBytes(animPath), HqrWriter.StoredEntry(flight)));

        var ress = File.ReadAllBytes(ressPath);
        var table = HqrArchive.Open(ressPath).Read(44);
        table = WithAnim(table, BuggyEntity, generic, index);
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(ress, 44, HqrWriter.StoredEntry(table)));
        return $"jump flight: ANIM.HQR entry {index} (entry {RetailEntry} with the steps forward x{forward:0.00}, the climb x{ClimbScale(forward):0.00} and the time x{TimeScale(forward):0.00}: " +
               $"{Distance(forward):0.0} cells), played by Twinsen in the buggy as animation {generic}";
    }

    // The retail flight with its keyframes' root steps and times scaled. Layout (ANIM.HQR): U16 keyframes, U16 bones, U16 loop frame, U16 0;
    // then per keyframe U16 time (ms), S16 step X, Y, Z, and 8 bytes per bone.
    public static byte[] Scale(byte[] anim, double forward)
    {
        double climb = ClimbScale(forward), time = TimeScale(forward);
        var copy = (byte[])anim.Clone();
        int frames = BinaryPrimitives.ReadUInt16LittleEndian(copy), bones = BinaryPrimitives.ReadUInt16LittleEndian(copy.AsSpan(2));
        for (var f = 0; f < frames; f++)
        {
            var p = 8 + f * (8 + bones * 8);
            if (p + 8 > copy.Length) throw new InvalidDataException("The jump animation is shorter than its header says.");
            void Put(int at, double value) => BinaryPrimitives.WriteInt16LittleEndian(copy.AsSpan(at), (short)Math.Clamp(Math.Round(value), short.MinValue, short.MaxValue));
            BinaryPrimitives.WriteUInt16LittleEndian(copy.AsSpan(p), (ushort)Math.Clamp(Math.Round(BinaryPrimitives.ReadUInt16LittleEndian(copy.AsSpan(p)) * time), 1, ushort.MaxValue));
            Put(p + 2, BinaryPrimitives.ReadInt16LittleEndian(copy.AsSpan(p + 2)) * forward);
            Put(p + 4, BinaryPrimitives.ReadInt16LittleEndian(copy.AsSpan(p + 4)) * climb);
            Put(p + 6, BinaryPrimitives.ReadInt16LittleEndian(copy.AsSpan(p + 6)) * forward);
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
