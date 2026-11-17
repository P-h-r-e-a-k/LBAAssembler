using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Assets;

namespace LBAAssembler.Terrain.Citadel;

// The museum security room's door with panes of its glass out (the user, 2026-11-17: "Let's take out some panes of glass in the door in the
// museum ... The user needs to throw their magic ball through the missing glass in the door, to hit the broom"): the room's door (scene 15's
// actor 5, sprite 120 -- a wooden door, four panes of glass over its panel, the glass a checker of one colour and see-through pixels) copied
// as a new sprite after SPRITES.HQR's last with its two lower panes' glass pixels see-through too, its box (RESS.HQR's sprites' table) the
// same. RaceTrackStory.Chapters' MuseumBroom puts it on the door; the race file's ball_through= lets Twinsen's magic ball pass the door.
internal static class CitadelMuseumDoor
{
    public const int Scene = 15, DoorActor = 5, DoorSprite = 120;
    private const int RessSpriteZv = 5;
    // the new sprite's number (-1: not made)
    public static int GapSprite { get; private set; } = -1;

    public static string Install(string gameDirectory)
    {
        GapSprite = -1;
        var spritesPath = Path.Combine(gameDirectory, "SPRITES.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        if (!File.Exists(spritesPath) || !File.Exists(ressPath)) return "The museum's door with panes out left out: no SPRITES.HQR or RESS.HQR";
        var sprites = HqrFile.Parse(File.ReadAllBytes(spritesPath));
        var ress = HqrFile.Parse(File.ReadAllBytes(ressPath));
        var entry = sprites.Read(DoorSprite);
        var door = GphImage.Decode(entry, (int)BinaryPrimitives.ReadUInt32LittleEndian(entry));
        int w = door.Width, h = door.Height;
        // the glass: the commonest colour among the pixels with a see-through pixel on each side (the checker)
        var counts = new Dictionary<byte, int>();
        for (var y = 0; y < h; y++)
            for (var x = 1; x + 1 < w; x++)
            {
                var i = y * w + x;
                if (door.Opaque[i] && !door.Opaque[i - 1] && !door.Opaque[i + 1]) counts[door.Pixels[i]] = counts.GetValueOrDefault(door.Pixels[i]) + 1;
            }
        if (counts.Count == 0) return $"The museum's door with panes out left out: sprite {DoorSprite} has no glass";
        var glass = counts.MaxBy(c => c.Value).Key;
        // its panes: the glass pixels in patches (diagonal neighbours and one see-through pixel between count as joined), the lower two
        var patch = new int[w * h];
        var patches = new List<List<int>>();
        for (var start = 0; start < w * h; start++)
        {
            if (!door.Opaque[start] || door.Pixels[start] != glass || patch[start] != 0) continue;
            var list = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(start); patch[start] = patches.Count + 1;
            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                list.Add(i);
                int x = i % w, y = i / w;
                for (var dy = -2; dy <= 2; dy++)
                    for (var dx = -2; dx <= 2; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h || Math.Abs(dx) + Math.Abs(dy) > 2) continue;
                        var n = ny * w + nx;
                        if (patch[n] != 0 || !door.Opaque[n] || door.Pixels[n] != glass) continue;
                        // (two apart: only across a see-through pixel, not across a bar of the frame)
                        if (Math.Abs(dx) + Math.Abs(dy) == 2 && (dx == 0 || dy == 0) && door.Opaque[(y + dy / 2) * w + x + dx / 2]) continue;
                        patch[n] = patches.Count + 1;
                        queue.Enqueue(n);
                    }
            }
            patches.Add(list);
        }
        var panes = patches.Where(p => p.Count >= 20).OrderByDescending(p => p.Average(i => i / w)).ToList();
        if (panes.Count < 4) return $"The museum's door with panes out left out: sprite {DoorSprite}'s glass makes {panes.Count} panes, not 4";
        var gap = new GphImage(w, h) { OffsetX = door.OffsetX, OffsetY = door.OffsetY };
        Array.Copy(door.Pixels, gap.Pixels, w * h);
        Array.Copy(door.Opaque, gap.Opaque, w * h);
        var cleared = 0;
        foreach (var pane in panes.Take(2))
            foreach (var i in pane) { gap.Opaque[i] = false; gap.Pixels[i] = 0; cleared++; }
        var picture = CitadelMuseum.EncodeBrick(gap);
        var made = new byte[8 + picture.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(made, 8);
        BinaryPrimitives.WriteUInt32LittleEndian(made.AsSpan(4), (uint)picture.Length);
        picture.CopyTo(made, 8);
        // (a build's second run finds it already there)
        var number = -1;
        for (var n = sprites.Count - 1; n >= Math.Max(0, sprites.Count - 8) && number < 0; n--)
            if (!sprites.IsEmpty(n) && sprites.Read(n).AsSpan().SequenceEqual(made)) number = n;
        if (number < 0)
        {
            number = sprites.Count;
            sprites.Slots.Add(new HqrFile.Slot());
            sprites.SetStored(number, made);
        }
        var zv = ress.Read(RessSpriteZv);
        if (zv.Length < (DoorSprite + 1) * 16) return "The museum's door with panes out left out: RESS.HQR's sprites' table has no door";
        if (zv.Length < (number + 1) * 16) Array.Resize(ref zv, (number + 1) * 16);
        Array.Copy(zv, DoorSprite * 16, zv, number * 16, 16);
        ress.SetStored(RessSpriteZv, zv);
        new FileTransaction().Write(spritesPath, sprites.ToBytes()).Write(ressPath, ress.ToBytes()).Commit();
        GapSprite = number;
        return $"The museum's security room door with its lower two panes of glass out: sprite {DoorSprite} copied as sprite {number} ({cleared} pixels of glass, colour {glass}, see-through)";
    }
}
