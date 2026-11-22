using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Assets;

namespace LBAAssembler.Terrain.Citadel;

// The museum security room's door with panes of its glass out (the user, 2026-11-17: "Let's take out some panes of glass in the door in the
// museum ... The user needs to throw their magic ball through the missing glass in the door, to hit the broom"): the room's door (scene 15's
// actor 5, sprite 120 -- a wooden door, four panes of glass over its panel, the glass a checker of one colour and see-through pixels) copied
// as a new sprite after SPRITES.HQR's last with its two lower panes' glass pixels see-through too, its box (RESS.HQR's sprites' table) the
// same. RaceTrackStory.Chapters' security-room edit puts it on the door; the race file's ball_through= lets Twinsen's magic ball pass the door.
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

    // The security room's window shut (the user, 2026-11-19: "The elevator into the museum, let's remove this for both Citadel islands
    // citadel.ile and citabau.ile, let's also remove the window from the room it connects to" -- Twinsen gets into the room from inside the
    // museum, the door released by the mop and bucket): in scene 15's grid the window (block WindowBlock, two cells wide and six layers high in the
    // room's north wall, z WallZ) laid over with the wall's own pattern beside it (WallBlock, three cells wide, a row of three positions a
    // layer), and its way out (the zone into scene 48 in front of it, the game's window exit) taken away.
    private const int WallZ = 24, WindowX0 = 24, WindowX1 = 25, WindowY0 = 12, WindowY1 = 17, WindowBlock = 123, WallBlock = 2, WallBase = 10,
                      WallFirstX = 24, Outside = 48, GridSize = 64, GridHeight = 25;
    public static List<string> ShutWindow(string gameDirectory)
    {
        var log = new List<string>();
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        if (!File.Exists(bkgPath)) return log;
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new Maps.MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Scene * 2 + 1 >= table.Length) { log.Add($"The museum's window left: no scene {Scene}"); return log; }
        var gridNumber = table[Scene * 2 + 1];
        var gridEntry = header.Gri + gridNumber;
        var raw = bkg.Read(gridEntry);
        var shape = Grids.Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1.Lba1GridCodec.Decode(shape);
        int BlockAt(int x, int y, int z) => cells[((z * GridSize + x) * GridHeight + y) * 2];
        var edits = new List<Lba1.Lba1GridCell>();
        if (BlockAt(WindowX0, WindowY0, WallZ) == WindowBlock)
        {
            // (the wall's pattern: its column by x, three cells to a repeat starting at WallFirstX, three positions to a layer from WallBase)
            for (var x = WindowX0; x <= WindowX1; x++)
                for (var y = WindowY0; y <= WindowY1; y++)
                    edits.Add(new Lba1.Lba1GridCell(x, y, WallZ, WallBlock, (y - WallBase) * 3 + ((x - WallFirstX) % 3 + 3) % 3));
            shape = Lba1.Lba1GridEdit.SetCells(shape, edits);
            raw = Grids.Lba2GridBackend.FromLba1Shape(raw, shape);
            bkg.SetStored(gridEntry, raw);
            if (header.MaxGri < (uint)raw.Length) header.MaxGri = (uint)raw.Length;
            bkg.SetStored(0, header.Bytes);
            File.WriteAllBytes(bkgPath, bkg.ToBytes());
            log.Add($"The museum's security room (scene {Scene}, grid {gridNumber}): its window (block {WindowBlock}) laid over with its wall ({edits.Count} cells)");
        }
        else log.Add($"The museum's security room (scene {Scene}): its window shut already, or not where it was (block {BlockAt(WindowX0, WindowY0, WallZ)})");
        // (its way out: the zone into scene 48 in front of it -- not the museum's front door, whose zone into 48 stands at the floor's edge)
        var store = new Scenes.SceneStore(Scenes.SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(Scene)) return log;
        var model = store.Load(Scene);
        var gone = model.Zones.RemoveAll(z => z.Type == 0 && z.Num == Outside && z.X0 <= WindowX1 * 512 + 511 && z.X1 >= WindowX0 * 512
                                              && z.Y0 >= WindowY0 * 256 - 1024 && z.Z0 <= (WallZ + 2) * 512 && z.Z1 >= WallZ * 512);
        // (and the ladder zone up the wall to it -- type 6 before the window: the user, 2026-11-21, "When we removed the window in the
        // museum we've left the climbing zone, this needs removing" -- Twinsen climbed the bare wall)
        var ladder = model.Zones.RemoveAll(z => z.Type == 6 && z.X0 <= WindowX1 * 512 + 511 && z.X1 >= WindowX0 * 512
                                                && z.Z0 <= (WallZ + 2) * 512 && z.Z1 >= WallZ * 512);
        if (gone + ladder > 0)
        {
            store.Save(Scene, model, allowErrors: true);
            log.Add($"The museum's security room (scene {Scene}): its window's way out into scene {Outside} ({gone}) and the ladder up to it ({ladder}) taken away");
        }
        return log;
    }
}
