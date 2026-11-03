using LBAAssembler;
using LBAAssembler.Lba1;
using LBAAssembler.Scenes;

namespace ScriptRoundTrip;

// doorstudy [folder]: the door actors of scene 13 and the block stacks around the two arches (the Rabbibunny house's and the one this
// editor opens), to see whether the new door sits at the same height and screen position relative to its floor as the original.
internal static class DoorStudy
{
    public static int Run(string[] args)
    {
        var folder = args.Length > 1 ? args[1] : @"E:\GOG Games\Little Big Adventure";
        var store = new SceneStore(SceneGame.Lba1, folder);
        var scene = store.Load(13);
        for (var i = 0; i < scene.Actors.Count; i++)
        {
            var a = scene.Actors[i];
            if (a.IsSprite && a.Sprite is 11 or 12)
                Console.WriteLine($"actor {i}: sprite {a.Sprite} flags {a.Flags:X4} at ({a.X},{a.Y},{a.Z}) cell ({a.X / 512.0:0.0},{a.Y / 256.0:0.0},{a.Z / 512.0:0.0}) info {string.Join(",", a.Info)} beta {a.Beta}");
        }
        var grid = store.LoadGrid(13);
        void Stacks(string title, int x0, int x1, int z0, int z1)
        {
            Console.WriteLine($"-- {title}: block (position) per layer y=0..12, cells x {x0}..{x1}, z {z0}..{z1}");
            for (var z = z0; z <= z1; z++)
                for (var x = x0; x <= x1; x++)
                {
                    var cells = Enumerable.Range(0, 13).Select(y => Lba1GridEdit.Get(grid, x, y, z)).ToList();
                    Console.WriteLine($"x{x,2} z{z,2}: " + string.Join(" ", cells.Select(c => c.Block == 0 ? (c.Pos == 0 ? "." : $"c{c.Pos}") : $"{c.Block}")));
                }
        }
        Stacks("Rabbibunny doorway (reference)", 31, 39, 52, 56);
        Stacks("new arch", 45, 54, 11, 15);
        return 0;
    }
}

// blockinfo <folder> <scene> <block>...: each block's extent, and the grid cells of scene 13 that use it with their positions inside the block.
internal static class BlockInfo
{
    public static int Run(string[] args)
    {
        var folder = args[1];
        var scene = int.Parse(args[2]);
        var store = new SceneStore(SceneGame.Lba1, folder);
        var grid = store.LoadGrid(scene);
        var blocks = HqrArchive.Open(Path.Combine(folder, "LBA_BLL.HQR")).Read(scene);
        var cells = Lba1GridCodec.Decode(grid);
        foreach (var arg in args.Skip(3))
        {
            var b = int.Parse(arg);
            var at = (int)BitConverter.ToUInt32(blocks, (b - 1) * 4);
            Console.WriteLine($"block {b}: extent {blocks[at]}x{blocks[at + 1]}x{blocks[at + 2]} (dx,dy,dz)");
            var shown = 0;
            for (var z = 0; z < 64 && shown < 14; z++)
                for (var x = 0; x < 64 && shown < 14; x++)
                    for (var y = 0; y < 25 && shown < 14; y++)
                    {
                        var i = ((z * 64 + x) * 25 + y) * 2;
                        if (cells[i] == b) { Console.WriteLine($"   cell ({x},{y},{z}) pos {cells[i + 1]}"); shown++; }
                    }
        }
        return 0;
    }
}

// doorvariant <folder> <outdir>: candidate edits of grid 13's arch, each rendered closed and open (the sprite lifted 90 px).
internal static class DoorVariant
{
    // The reference doorway (x 33..37, z 52..56, layers 0..9) copied over the new arch's (x 47..51, z 11..15).
    public static List<Lba1GridCell> Reference(byte[] grid)
    {
        var cells = Lba1GridCodec.Decode(grid);
        var edits = new List<Lba1GridCell>();
        for (var dx = 0; dx < 5; dx++)
            for (var dz = 0; dz < 5; dz++)
                for (var y = 0; y < 10; y++)
                {
                    var i = (((52 + dz) * 64 + 33 + dx) * 25 + y) * 2;
                    edits.Add(new Lba1GridCell(47 + dx, y, 11 + dz, cells[i], cells[i + 1]));
                }
        return edits;
    }

    public static int Run(string[] args)
    {
        var folder = args[1];
        var outDir = args[2];
        Directory.CreateDirectory(outDir);
        var pristine = new SceneStore(SceneGame.Lba1, folder).LoadGrid(13);
        // "as installed now" is what the folder holds; the variants start from the same grid
        byte[] Wall(byte[] g, bool back, bool side, int block)
        {
            var edits = new List<Lba1GridCell>();
            for (var y = 1; y <= 6; y++)
            {
                if (back) for (var z = 11; z <= 15; z++) edits.Add(new Lba1GridCell(46, y, z, block, (y - 1) % 2));
                if (side) for (var x = 47; x <= 50; x++) edits.Add(new Lba1GridCell(x, y, 11, block, (y - 1) % 2));
            }
            return Lba1GridEdit.SetCells(g, edits);
        }
        var variants = new (string Name, Func<byte[], byte[]> Edit, int[] Info)[]
        {
            ("v0_now", g => g, new[] { 0, 0, 0, 0 }),
            ("v1_clip", g => g, new[] { 0, 0, 0, 29 }),
            ("v2_reference", g => Lba1GridEdit.SetCells(g, Reference(g)), new[] { 0, 0, 0, 29 }),
            ("v3_wall2", g => Wall(Lba1GridEdit.SetCells(g, Reference(g)), true, true, 2), new[] { 0, 0, 0, 29 }),
            ("v4_wall1", g => Wall(Lba1GridEdit.SetCells(g, Reference(g)), true, true, 1), new[] { 0, 0, 0, 29 }),
        };
        foreach (var v in variants)
            foreach (var lift in new[] { 0, 90 })
            {
                DoorRender.Render(folder, 13, 28, Path.Combine(outDir, $"{v.Name}_{(lift == 0 ? "closed" : "open")}.png"), lift, 4, null, v.Edit, v.Info);
                Console.WriteLine($"{v.Name} {(lift == 0 ? "closed" : "open")}");
            }
        return 0;
    }
}

// surpriseapply <folder>: what Tools > LBA1: Make surprise changes does, on a folder holding (copies of) the game files.
// scenetext <folder> <scene> <actor>: that actor's life script as C text, read from the folder's SCENE.HQR.
internal static class SurpriseCommands
{
    public static int Apply(string[] args)
    {
        var result = Lba1SurpriseChanges.Apply(args[1]);
        Console.WriteLine($"{(result.Changed ? "changed" : "nothing to do")}: {result.Message}");
        return 0;
    }

    // scenerewrite <folder> <scene> <actor>: an LBA1 scene's actor life bytes as stored and after a Parse/Write of its record, and where they first differ
    public static int Rewrite(string[] args)
    {
        var store = new SceneStore(SceneGame.Lba1, args[1]);
        var scene = int.Parse(args[2]); var actor = int.Parse(args[3]);
        var record = store.LoadRecord(scene);
        var again = LBAAssembler.Scenes.SceneSerializer.Write(LBAAssembler.Scenes.SceneSerializer.Parse(SceneGame.Lba1, record));
        byte[] Life(byte[] r) { var rec = LBAAssembler.LbaScript.SceneRecord.ParseLba1(r); return rec.Life(rec.Actors[actor]).ToArray(); }
        var (a, b) = (Life(record), Life(again));
        Console.WriteLine($"record {record.Length} -> {again.Length} bytes; actor {actor} life {a.Length} -> {b.Length} bytes");
        var at = Enumerable.Range(0, Math.Min(a.Length, b.Length)).FirstOrDefault(i => a[i] != b[i], -1);
        Console.WriteLine(at < 0 && a.Length == b.Length ? "life identical" : $"first difference at {(at < 0 ? Math.Min(a.Length, b.Length) : at)}");
        if (at >= 0) Console.WriteLine($"  was {string.Join(" ", a.Skip(Math.Max(0, at - 4)).Take(16).Select(x => x.ToString("X2")))}\n  now {string.Join(" ", b.Skip(Math.Max(0, at - 4)).Take(16).Select(x => x.ToString("X2")))}");
        return 0;
    }

    // scenelanding <folder> <scene> <destination> <arrival x> <arrival y> <arrival z> <offset x> <offset z>: Lba1Fishermen's landing zone and
    // point added to the scene's record, then every actor's life script decompiled from it with the decompiler's own diagnostics
    public static int Landing(string[] args)
    {
        var store = new SceneStore(SceneGame.Lba1, args[1]);
        var scene = int.Parse(args[2]);
        var n = args.Skip(3).Select(int.Parse).ToArray();
        var model = LBAAssembler.Scenes.SceneSerializer.Parse(SceneGame.Lba1, store.LoadRecord(scene));
        const int sky = 4096, box = 1024;
        LBAAssembler.Scenes.SceneOps.AddZone(model, new LBAAssembler.Scenes.SceneZoneModel
        {
            X0 = box, Y0 = sky, Z0 = box, X1 = 2 * box - 1, Y1 = sky + 1536 - 1, Z1 = 2 * box - 1,
            Type = 0, Info = new[] { n[0], n[1], n[2] - 768, n[3] },
        });
        LBAAssembler.Scenes.SceneOps.AddTrackPoint(model, new LBAAssembler.Scenes.SceneTrackPoint(box + n[4], sky + 768, box + n[5]));
        var record = LBAAssembler.Scenes.SceneSerializer.Write(model);
        var rec = LBAAssembler.LbaScript.SceneRecord.ParseLba1(record);
        var was = LBAAssembler.LbaScript.SceneRecord.ParseLba1(store.LoadRecord(scene));
        var scripts = LBAAssembler.LbaScript.SceneScripts.Load(record, scene, null, lba1: true);
        for (var a = 0; a < rec.Actors.Count; a++)
        {
            var same = rec.Life(rec.Actors[a]).SequenceEqual(was.Life(was.Actors[a]));
            try { scripts.GetText(a, LBAAssembler.LbaScript.ScriptKind.Life); Console.WriteLine($"actor {a}: ok{(same ? "" : " (life bytes changed)")}"); }
            catch (InvalidOperationException e)
            {
                Console.WriteLine($"actor {a}: {e.Message}{(same ? "" : " (life bytes changed)")}");
                using var scope = LBAAssembler.LbaScript.Opcodes.Use(LBAAssembler.LbaScript.Opcodes.Lba1);
                try { LBAAssembler.LbaScript.LifeText.DecompileMapped(rec.Life(rec.Actors[a]).ToArray(), a, LBAAssembler.LbaScript.NoSymbols.Instance, null, d => Console.WriteLine("  " + d)); }
                catch (InvalidOperationException) { }
            }
        }
        return 0;
    }

    public static int Text(string[] args)
    {
        var store = new SceneStore(SceneGame.Lba1, args[1]);
        var scene = int.Parse(args[2]);
        var scripts = LBAAssembler.LbaScript.SceneScripts.Load(store.LoadRecord(scene), scene, null, lba1: true);
        Console.WriteLine(scripts.GetText(int.Parse(args[3]), LBAAssembler.LbaScript.ScriptKind.Life));
        return 0;
    }
}

// doorfloor <folder> <out.png> <extraRows>: the modded arch with the floor row of the doorway copied onto z = 15 (and further), to find what fills the black wedge under the arch's left leg.
internal static class DoorFloor
{
    public static int Run(string[] args)
    {
        var folder = args[1];
        var rows = int.Parse(args[3]);
        byte[] Edit(byte[] g)
        {
            var edits = new List<Lba1GridCell>();
            for (var extra = 0; extra < rows; extra++)
                for (var x = 47; x <= 51; x++)
                {
                    var from = Lba1GridEdit.Get(g, x, 0, 14);
                    edits.Add(new Lba1GridCell(x, 0, 15 + extra, from.Block, from.Pos));
                }
            return Lba1GridEdit.SetCells(g, edits);
        }
        DoorRender.Render(folder, 13, 28, args[2], 0, 3, null, Edit, new[] { 0, 0, 0, 0 });
        return 0;
    }
}

// stacks <folder> <scene> <x0> <x1> <z0> <z1> [yMax]: block per layer for a rectangle of cells ("cN" = collision-only cell with code N).
internal static class StackDump
{
    public static int Run(string[] args)
    {
        var grid = new SceneStore(SceneGame.Lba1, args[1]).LoadGrid(int.Parse(args[2]));
        int x0 = int.Parse(args[3]), x1 = int.Parse(args[4]), z0 = int.Parse(args[5]), z1 = int.Parse(args[6]);
        var top = args.Length > 7 ? int.Parse(args[7]) : 8;
        var cells = Lba1GridCodec.Decode(grid);
        for (var z = z0; z <= z1; z++)
            for (var x = x0; x <= x1; x++)
            {
                var stack = Enumerable.Range(0, top + 1).Select(y => { var i = ((z * 64 + x) * 25 + y) * 2; return cells[i] == 0 ? (cells[i + 1] == 0 ? "." : $"c{cells[i + 1]}") : $"{cells[i]}/{cells[i + 1]}"; });
                Console.WriteLine($"x{x,2} z{z,2}: " + string.Join(" ", stack));
            }
        return 0;
    }
}

// doorapron <folder> <out.png> <variant>: the modded arch with candidate ground in front of it (a: doorstep slab (block 6) only, b: + paved apron
// x 52..55 z 11..15, c: apron of the doorway's lane only z 12..14 (+ curb kept), d: b + curb strip at x = 52).
internal static class DoorApron
{
    public static int Run(string[] args)
    {
        var folder = args[1];
        var variant = args[3];
        byte[] Edit(byte[] g)
        {
            var edits = new List<Lba1GridCell>();
            if (variant is "a" or "b" or "c" or "d")
                for (var z = 12; z <= 14; z++) edits.Add(new Lba1GridCell(51, 0, z, 6, 0));
            if (variant == "f") for (var z = 12; z <= 13; z++) edits.Add(new Lba1GridCell(51, 0, z, 6, 0));
            if (variant == "g") for (var z = 12; z <= 15; z++) edits.Add(new Lba1GridCell(51, 0, z, 6, 0));
            if (variant == "h") for (var z = 12; z <= 14; z++) edits.Add(new Lba1GridCell(52, 0, z, 6, 0));
            if (variant is "i" or "j") for (var z = 11; z <= 15; z++) edits.Add(new Lba1GridCell(51, 0, z, 6, 0));
            int Pos(int x, int z) => (z % 2 == 1 ? 2 : 0) + (x % 2 == 1 ? 0 : 1);
            if (variant is "b" or "d" or "e" or "f" or "g" or "h" or "i" or "j") for (var x = 52; x <= 55; x++) for (var z = 11; z <= 15; z++) edits.Add(new Lba1GridCell(x, 0, z, 22, Pos(x, z)));
            if (variant is "c") for (var x = 52; x <= 55; x++) for (var z = 12; z <= 14; z++) edits.Add(new Lba1GridCell(x, 0, z, 22, Pos(x, z)));
            if (variant is "d" or "j") for (var z = 11; z <= 15; z++) edits.Add(new Lba1GridCell(52, 0, z, 134, 0));
            return edits.Count == 0 ? g : Lba1GridEdit.SetCells(g, edits);
        }
        DoorRender.Render(folder, 13, 28, args[2], 0, 3, null, Edit, new[] { 0, 0, 0, 0 });
        return 0;
    }
}

// fishtrace <folder> [scene] [fisherman] [boat] [choice]: talks to Proxima City's fisherman (default) in the simulation and prints where he and the boat are while he walks to it.
internal static class FishTrace
{
    public static int Run(string[] args)
    {
        var data = new LBAAssembler.Lba1.Runtime.Lba1RuntimeData(args[1]);
        int scene = args.Length > 2 ? int.Parse(args[2]) : 42, fisherman = args.Length > 3 ? int.Parse(args[3]) : 10, boat = args.Length > 4 ? int.Parse(args[4]) : 9, choice = args.Length > 5 ? int.Parse(args[5]) : 6;
        var rt = new LBAAssembler.Lba1.Runtime.Lba1Runtime(data) { ChoicePolicy = ids => ids.Contains(choice) ? choice : ids[^1] };
        rt.ChangeCube(scene);
        rt.Chapter = 6;
        rt.NbGoldPieces = 100;
        rt.Run(900);
        var f = rt.Objects[fisherman];
        var b = rt.Objects[boat];
        Console.WriteLine($"fisherman srot {f.SRot} beta {f.Beta} box x {f.XMin}..{f.XMax} y {f.YMin}..{f.YMax} z {f.ZMin}..{f.ZMax} at ({f.PosX},{f.PosY},{f.PosZ}) label {f.LabelTrack}; boat at ({b.PosX},{b.PosY},{b.PosZ}) box x {b.XMin}..{b.XMax} y {b.YMin}..{b.YMax} z {b.ZMin}..{b.ZMax}");
        rt.Place(4096, f.PosY, 21728, 768);
        rt.Fire = LBAAssembler.Lba1.Runtime.Lba1Const.FSpace;
        rt.Frame();
        rt.Fire = 0;
        for (var frame = 0; frame < 1500; frame++)
        {
            rt.Frame();
            if (frame == 5 && rt.NbGoldPieces < 100) rt.Place(4300, 768, 20700, 768);   // out of his way
            if (frame % 25 == 0) Console.WriteLine($"  {frame,4}: fisherman ({f.PosX},{f.PosY},{f.PosZ}) label {f.LabelTrack} comportement-ish col {f.ObjCol}; boat ({b.PosX},{b.PosY},{b.PosZ}); gold {rt.NbGoldPieces}");
        }
        foreach (var e in rt.Events.Where(e => !e.Contains("plays sample")).TakeLast(12)) Console.WriteLine("    " + e);
        return 0;
    }
}

// animsteps <folder> <entity> <anim id>...: how far each animation of an entity moves the actor in its own frame (x sideways, z forward), summed over its frames, and how long it lasts.
internal static class AnimSteps
{
    public static int Run(string[] args)
    {
        var data = new LBAAssembler.Lba1.Runtime.Lba1RuntimeData(args[1]);
        var entity = data.Entity(int.Parse(args[2]));
        if (entity is null) { Console.WriteLine("no such entity"); return 1; }
        foreach (var id in args.Skip(3).Select(int.Parse))
        {
            if (!entity.Anims.TryGetValue(id, out var anim)) { Console.WriteLine($"anim {id}: not in the entity"); continue; }
            var a = data.Animation(anim.Hqr);
            if (a is null) { Console.WriteLine($"anim {id}: unreadable"); continue; }
            int sx = a.Frames.Sum(f => f.StepX), sy = a.Frames.Sum(f => f.StepY), sz = a.Frames.Sum(f => f.StepZ), ticks = a.Frames.Sum(f => f.Length);
            Console.WriteLine($"anim {id} (ANIM.HQR {anim.Hqr}): {a.Frames.Count} frames, {ticks} ticks, moves x {sx} y {sy} z {sz}; per frame z: {string.Join(",", a.Frames.Select(f => f.StepZ))}");
        }
        return 0;
    }
}

// hqrcmp <a.hqr> <b.hqr>: the entries (decoded) that differ between two archives, with their sizes.
internal static class HqrCompare
{
    public static int Run(string[] args)
    {
        var a = LBAAssembler.HqrFile.Parse(File.ReadAllBytes(args[1]));
        var b = LBAAssembler.HqrFile.Parse(File.ReadAllBytes(args[2]));
        Console.WriteLine($"{a.Count} vs {b.Count} entries");
        for (var i = 0; i < Math.Min(a.Count, b.Count); i++)
        {
            if (a.IsEmpty(i) != b.IsEmpty(i)) { Console.WriteLine($"  {i}: one is empty"); continue; }
            if (a.IsEmpty(i)) continue;
            var x = a.Read(i); var y = b.Read(i);
            if (!x.AsSpan().SequenceEqual(y)) Console.WriteLine($"  {i}: differs ({x.Length} vs {y.Length} bytes)");
        }
        return 0;
    }
}

// doorlanes: walk into the bedroom's zone (scene 13) at every z of the recess, and see where Twinsen arrives in the bedroom and whether the doorway zone throws him out again.
internal static class DoorLanes
{
    public static int Run(string[] args)
    {
        var data = new LBAAssembler.Lba1.Runtime.Lba1RuntimeData(args.Length > 1 ? args[1] : @"E:\GOG Games\Little Big Adventure");
        for (var z = 5376; z <= 7935; z += 128)
        {
            var rt = new LBAAssembler.Lba1.Runtime.Lba1Runtime(data);
            rt.ChangeCube(13);
            rt.NbLittleKeys = 1;
            rt.FlagGame[220] = 1;
            rt.Run(5);
            rt.Place(25600, 256, z, 768);
            var history = new List<string>();
            var last = rt.NumCube;
            for (var f = 0; f < 120; f++)
            {
                rt.Frame();
                if (rt.NumCube != last) { history.Add($"f{f}: scene {rt.NumCube} at ({rt.Hero.PosX},{rt.Hero.PosY},{rt.Hero.PosZ})"); last = rt.NumCube; }
            }
            Console.WriteLine($"z {z} (cell {z / 512.0:0.0}): {string.Join("; ", history)}; ends in scene {rt.NumCube}");
        }
        return 0;
    }
}

// doorwalk: walk west from the street into the bedroom door along several z lines, with a key, and follow the scenes.
internal static class DoorWalk
{
    public static int Run(string[] args)
    {
        var data = new LBAAssembler.Lba1.Runtime.Lba1RuntimeData(args.Length > 1 ? args[1] : @"E:\GOG Games\Little Big Adventure");
        for (var z = 5376; z <= 8192; z += 256)
        {
            var rt = new LBAAssembler.Lba1.Runtime.Lba1Runtime(data);
            rt.ChangeCube(13);
            rt.NbLittleKeys = 1;
            rt.Run(5);
            rt.Place(54 * 512, 256, z, 768);
            rt.Joy = LBAAssembler.Lba1.Runtime.Lba1Const.JUp;
            var history = new List<string>();
            var last = rt.NumCube;
            for (var f = 0; f < 500; f++)
            {
                rt.Frame();
                if (rt.NumCube != last) { history.Add($"f{f}: scene {rt.NumCube} at ({rt.Hero.PosX},{rt.Hero.PosY},{rt.Hero.PosZ})"); last = rt.NumCube; }
            }
            Console.WriteLine($"z {z} (cell {z / 512.0:0.0}): {string.Join("; ", history)}; ends in scene {rt.NumCube} at ({rt.Hero.PosX},{rt.Hero.PosY},{rt.Hero.PosZ})");
        }
        return 0;
    }
}

// scenezones <folder> <scene>: every zone of a scene (an LBA2 folder's when it has LBA2.HQR).
internal static class SceneZoneDump
{
    public static int Run(string[] args)
    {
        var game = File.Exists(Path.Combine(args[1], "LBA2.HQR")) ? LBAAssembler.Scenes.SceneGame.Lba2 : LBAAssembler.Scenes.SceneGame.Lba1;
        var store = new LBAAssembler.Scenes.SceneStore(game, args[1]);
        var scene = store.Load(int.Parse(args[2]));
        for (var i = 0; i < scene.Zones.Count; i++)
        {
            var z = scene.Zones[i];
            Console.WriteLine($"zone {i}: type {z.Type} num {z.Num} x {z.X0}..{z.X1} y {z.Y0}..{z.Y1} z {z.Z0}..{z.Z1} info {string.Join(",", z.Info)}");
        }
        return 0;
    }
}

// doortrace <z>: the hero's position each frame near the zone when walking west along z.
internal static class DoorTrace
{
    public static int Run(string[] args)
    {
        var data = new LBAAssembler.Lba1.Runtime.Lba1RuntimeData(@"E:\GOG Games\Little Big Adventure");
        var z = int.Parse(args[1]);
        var rt = new LBAAssembler.Lba1.Runtime.Lba1Runtime(data);
        rt.ChangeCube(13);
        rt.NbLittleKeys = 1;
        rt.Run(5);
        rt.Place(54 * 512, 256, z, 768);
        rt.Joy = LBAAssembler.Lba1.Runtime.Lba1Const.JUp;
        for (var f = 0; f < 100; f++)
        {
            rt.Frame();
            if (f >= 60) Console.WriteLine($"f{f}: scene {rt.NumCube} hero ({rt.Hero.PosX},{rt.Hero.PosY},{rt.Hero.PosZ}) beta {rt.Hero.Beta} anim {rt.Hero.GenAnim}; {string.Join(" | ", rt.Events.TakeLast(2))}");
            if (rt.NumCube != 13) break;
        }
        return 0;
    }
}

// scriptgrep <folder> <text>: every life script (all scenes) containing the text, as scene/actor.
// scriptgrep2 <LBA2 folder> <text>: every LBA2 life and track script line (all scenes) containing the text, as scene/actor.
internal static class ScriptGrep2
{
    public static int Run(string[] args)
    {
        var store = new LBAAssembler.Scenes.SceneStore(LBAAssembler.Scenes.SceneGame.Lba2, args[1]);
        for (var s = 0; s < store.SceneCount; s++)
        {
            if (!store.SceneExists(s)) continue;
            LBAAssembler.LbaScript.SceneScripts scripts; LBAAssembler.Scenes.SceneModel model;
            try { var record = store.LoadRecord(s); scripts = LBAAssembler.LbaScript.SceneScripts.Load(record, s); model = store.Load(s); } catch { continue; }
            for (var a = 0; a < model.Actors.Count; a++)
                foreach (var kind in new[] { LBAAssembler.LbaScript.ScriptKind.Life, LBAAssembler.LbaScript.ScriptKind.Track })
                {
                    string text;
                    try { text = scripts.GetText(a, kind); }
                    catch (Exception e) { Console.WriteLine($"scene {s} actor {a} {kind}: UNDECODED {e.Message}"); continue; }
                    foreach (var line in text.Split('\n').Where(l => l.Contains(args[2])))
                        Console.WriteLine($"scene {s} actor {a} {kind}: {line.Trim()}");
                }
        }
        return 0;
    }
}

// scriptdump <LBA2 folder> <out folder> [scene,scene...|all] [demo96]: every actor's life and track text, one file per scene
// (sceneNNN.txt); demo96 = the scripts are the 1996 demo's (Opcodes.Demo96).
internal static class ScriptDump
{
    public static int Run(string[] args)
    {
        var store = new LBAAssembler.Scenes.SceneStore(LBAAssembler.Scenes.SceneGame.Lba2, args[1]);
        Directory.CreateDirectory(args[2]);
        var only = args.Length > 3 && args[3] != "all" ? args[3].Split(',').Select(int.Parse).ToHashSet() : null;
        var dialect = args.Length > 4 && args[4] == "demo96" ? LBAAssembler.LbaScript.Opcodes.Demo96 : LBAAssembler.LbaScript.Opcodes.Lba2;
        for (var s = 0; s < store.SceneCount; s++)
        {
            if (!store.SceneExists(s) || only != null && !only.Contains(s)) continue;
            LBAAssembler.LbaScript.SceneScripts scripts; LBAAssembler.Scenes.SceneModel model;
            try { var record = store.LoadRecord(s); scripts = LBAAssembler.LbaScript.SceneScripts.Load(record, dialect, s); model = store.Load(s); } catch (Exception e) { Console.WriteLine($"scene {s}: {e.Message}"); continue; }
            var text = new System.Text.StringBuilder();
            for (var a = 0; a < model.Actors.Count; a++)
                foreach (var kind in new[] { LBAAssembler.LbaScript.ScriptKind.Life, LBAAssembler.LbaScript.ScriptKind.Track })
                {
                    text.AppendLine($"// ===== actor {a} {kind}");
                    try { text.AppendLine(scripts.GetText(a, kind)); }
                    catch (Exception e) { text.AppendLine($"// UNDECODED {e.Message}"); }
                }
            File.WriteAllText(Path.Combine(args[2], $"scene{s:D3}.txt"), text.ToString());
        }
        return 0;
    }
}

// scriptbytes <LBA2 folder> <scene>: every actor's raw life and track bytes as hex.
internal static class ScriptBytes
{
    public static int Run(string[] args)
    {
        var store = new LBAAssembler.Scenes.SceneStore(LBAAssembler.Scenes.SceneGame.Lba2, args[1]);
        var record = LBAAssembler.LbaScript.SceneRecord.Parse(store.LoadRecord(int.Parse(args[2])));
        for (var a = 0; a < record.Actors.Count; a++)
        {
            Console.WriteLine($"actor {a} life {Convert.ToHexString(record.Life(record.Actors[a]).ToArray())}");
            Console.WriteLine($"actor {a} track {Convert.ToHexString(record.Track(record.Actors[a]).ToArray())}");
        }
        return 0;
    }
}

// bkgcompare <retail folder> <other folder> <scene,scene...>: for each of the other folder's scenes, its interior grid and block
// library, whether the retail LBA_BKG.HQR has that library (byte for byte, at which number), and for the bricks its blocks draw: how
// many are the retail brick of the same number, how many are some other retail brick (bytes equal), and how many the retail game lacks.
internal static class BkgCompare
{
    public static int Run(string[] args)
    {
        var retail = new LBAAssembler.Grids.Lba2GridBackend(args[1]);
        var other = new LBAAssembler.Grids.Lba2GridBackend(args[2]);
        var retailFile = LBAAssembler.HqrFile.Parse(File.ReadAllBytes(Path.Combine(args[1], "LBA_BKG.HQR")));
        var otherFile = LBAAssembler.HqrFile.Parse(File.ReadAllBytes(Path.Combine(args[2], "LBA_BKG.HQR")));
        int U16(byte[] b, int o) => b[o] | b[o + 1] << 8;
        var rh = retailFile.Read(0); var oh = otherFile.Read(0);
        int rBll = U16(rh, 4), rBrk = U16(rh, 6), rMax = U16(rh, 8), rGrm = U16(rh, 2);
        int oBll = U16(oh, 4), oBrk = U16(oh, 6), oMax = U16(oh, 8);
        var retailBricks = new Dictionary<string, int>();
        for (var b = 0; b < rMax; b++) if (!retailFile.IsEmpty(rBrk + b)) retailBricks.TryAdd(Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(retailFile.Read(rBrk + b))), b);
        var retailLibs = new Dictionary<string, int>();
        for (var l = 0; rBll + l < rBrk; l++) if (!retailFile.IsEmpty(rBll + l)) retailLibs.TryAdd(Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(retailFile.Read(rBll + l))), l);
        var allUsed = new SortedSet<int>();
        foreach (var s in args[3].Split(',').Select(int.Parse))
        {
            var grid = other.GridOfScene(s);
            if (grid is null) { Console.WriteLine($"scene {s}: no interior grid"); continue; }
            var raw = other.RawGrid(grid.Value);
            var lib = otherFile.Read(oBll + raw[0]);
            var libHash = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(lib));
            Console.WriteLine($"scene {s}: grid {grid} ({raw.Length} bytes), library {raw[0]} ({lib.Length} bytes), fragment set {raw[1]}; retail has that library: " +
                              (retailLibs.TryGetValue(libHash, out var rl) ? $"yes, library {rl}" : "no"));
            // the bricks the library's blocks draw (LBA2 library: U32 offsets to each block, block = dx dy dz, then per cell 4 bytes: shape, sound, brick U16)
            var used = new SortedSet<int>();
            var first = BitConverter.ToInt32(lib, 0); var blocks = first / 4;
            for (var k = 0; k < blocks; k++)
            {
                var o = BitConverter.ToInt32(lib, k * 4); var end = k + 1 < blocks ? BitConverter.ToInt32(lib, (k + 1) * 4) : lib.Length;
                int dx = lib[o], dy = lib[o + 1], dz = lib[o + 2];
                for (var c = 0; c < dx * dy * dz && o + 3 + c * 4 + 3 < end + 1; c++) { var brick = U16(lib, o + 3 + c * 4 + 2); if (brick > 0) used.Add(brick - 1); }
            }
            int same = 0, moved = 0, missing = 0;
            foreach (var b in used)
            {
                if (b >= oMax || otherFile.IsEmpty(oBrk + b)) { missing++; continue; }
                var bytes = otherFile.Read(oBrk + b);
                var h = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(bytes));
                if (b < rMax && !retailFile.IsEmpty(rBrk + b) && retailFile.Read(rBrk + b).AsSpan().SequenceEqual(bytes)) same++;
                else if (retailBricks.ContainsKey(h)) moved++;
                else missing++;
            }
            Console.WriteLine($"  bricks drawn: {used.Count}; the retail brick of the same number {same}, another retail brick {moved}, not in the retail game {missing}");

            // the blocks the grid uses, and whether retail library `args[4]` has each (same size, the same cells with the bricks renumbered)
            if (args.Length > 4)
            {
                var rlib = retailFile.Read(rBll + int.Parse(args[4]));
                int Brick(int b) => b < 0 ? -1 : !otherFile.IsEmpty(oBrk + b) && retailBricks.TryGetValue(Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(otherFile.Read(oBrk + b))), out var r) ? r : -2;
                string Sig(byte[] l, int k, Func<int, int> map)
                {
                    var n = BitConverter.ToInt32(l, 0) / 4; var o = BitConverter.ToInt32(l, k * 4);
                    int dx = l[o], dy = l[o + 1], dz = l[o + 2];
                    var sb = new System.Text.StringBuilder($"{dx}x{dy}x{dz}");
                    for (var c = 0; c < dx * dy * dz; c++) sb.Append($";{l[o + 3 + c * 4]},{l[o + 4 + c * 4]},{map(U16(l, o + 5 + c * 4) - 1)}");
                    return sb.ToString();
                }
                var retailSigs = new Dictionary<string, int>();
                for (var k = 0; k < BitConverter.ToInt32(rlib, 0) / 4; k++) retailSigs.TryAdd(Sig(rlib, k, b => b), k);
                var cube = new LBAAssembler.Lba1.Runtime.Lba1Cube(other.LoadGrid(grid.Value), lib);
                var usedBlocks = new SortedSet<int>();
                for (var y = 0; y < 25; y++) for (var z = 0; z < 64; z++) for (var x = 0; x < 64; x++) { var (bl, _) = cube.Cell(x, y, z); if (bl > 0) usedBlocks.Add(bl); }
                int found = 0, lacking = 0, lackBricks = 0;
                foreach (var bl in usedBlocks)
                {
                    var sig = Sig(lib, bl - 1, Brick);
                    if (retailSigs.ContainsKey(sig)) found++; else { lacking++; if (sig.Contains(",-2")) lackBricks++; }
                }
                Console.WriteLine($"  blocks used: {usedBlocks.Count}; in retail library {args[4]} as they are (bricks renumbered) {found}; not {lacking} ({lackBricks} of them draw a brick the retail game lacks)");
                allUsed.UnionWith(usedBlocks);
                Console.WriteLine($"  retail library {args[4]}: {BitConverter.ToInt32(rlib, 0) / 4} blocks; this library: {BitConverter.ToInt32(lib, 0) / 4}; blocks used by the scenes so far: {allUsed.Count}");
            }
        }
        Console.WriteLine($"retail: grids from {U16(rh, 0)}, libraries from {rBll} ({rBrk - rBll}), bricks from {rBrk} ({rMax}); fragments from {rGrm}");
        return 0;
    }
}

// hqrmatch <a.HQR> <b.HQR> <entry,entry...>: for each of a's entries, the b entries with the same bytes (unpacked).
internal static class HqrMatch
{
    public static int Run(string[] args)
    {
        var a = LBAAssembler.HqrFile.Parse(File.ReadAllBytes(args[1]));
        var b = LBAAssembler.HqrFile.Parse(File.ReadAllBytes(args[2]));
        var byHash = new Dictionary<string, List<int>>();
        for (var i = 0; i < b.Count; i++)
        {
            if (b.IsEmpty(i)) continue;
            var h = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(b.Read(i)));
            if (!byHash.TryGetValue(h, out var l)) byHash[h] = l = new();
            l.Add(i);
        }
        foreach (var i in args[3].Split(',').Select(int.Parse))
        {
            if (i >= a.Count || a.IsEmpty(i)) { Console.WriteLine($"{i}: empty"); continue; }
            var bytes = a.Read(i);
            var h = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(bytes));
            Console.WriteLine($"{i} ({bytes.Length} bytes): " + (byHash.TryGetValue(h, out var l) ? string.Join(",", l) : "none"));
        }
        return 0;
    }
}

// npctexts <LBA2 folder> <island> [scene,scene...]: every scene of the island (by its header's island byte), each actor's entity,
// body, place and the texts its life script says (message, message_obj, add_choice, ask_choice...), with the text in English. The text
// file is island + 3 (Citadel 3, Desert 5).
internal static class NpcTexts
{
    private static readonly System.Text.RegularExpressions.Regex Says = new(
        @"\b(message|message_obj|message_zoe|add_choice|ask_choice|ask_choice_obj|add_message|add_message_obj|pcx_mess_obj|end_message_obj)\s*\(([^;]*?)\)\s*;");

    public static int Run(string[] args)
    {
        var store = new LBAAssembler.Scenes.SceneStore(LBAAssembler.Scenes.SceneGame.Lba2, args[1]);
        var island = int.Parse(args[2]);
        var only = args.Length > 3 ? args[3].Split(',').Select(int.Parse).ToHashSet() : null;
        var hqr = LBAAssembler.HqrArchive.Open(Path.Combine(args[1], "TEXT.HQR"));
        var english = LBAAssembler.Lba2TextBank.English(hqr);
        var bank = LBAAssembler.Lba2TextBank.Load(hqr, english, island + 3);
        for (var s = 0; s < store.SceneCount; s++)
        {
            if (!store.SceneExists(s) || only != null && !only.Contains(s)) continue;
            LBAAssembler.Scenes.SceneModel model; LBAAssembler.LbaScript.SceneScripts scripts;
            try { model = store.Load(s); if (model.Island != island) continue; scripts = LBAAssembler.LbaScript.SceneScripts.Load(store.LoadRecord(s), s); }
            catch (Exception e) { Console.WriteLine($"scene {s}: {e.Message}"); continue; }
            Console.WriteLine($"== scene {s} ({(model.CubeMode != 0 ? "outside" : "inside")}), {model.Actors.Count} actors");
            for (var a = 0; a < model.Actors.Count; a++)
            {
                var actor = model.Actors[a];
                string life;
                try { life = scripts.GetText(a, LBAAssembler.LbaScript.ScriptKind.Life); } catch (Exception e) { life = ""; Console.WriteLine($"  actor {a}: life undecoded ({e.Message})"); }
                var said = Says.Matches(life).Select(m => (Op: m.Groups[1].Value, Args: m.Groups[2].Value)).ToList();
                var flags = (actor.Flags & 0x200) != 0 ? " invisible" : "";
                Console.WriteLine($"  actor {a}: entity {actor.Entity} body {actor.Body} at ({actor.X},{actor.Y},{actor.Z}){flags}; {said.Count} lines");
                foreach (var (op, argText) in said)
                {
                    var last = argText.Split(',').Last().Trim();
                    var text = int.TryParse(last, out var id) ? bank.Find(id)?.Value ?? "(no such text)" : "";
                    Console.WriteLine($"    {op}({argText}): {text}");
                }
            }
        }
        return 0;
    }
}

// texts <TEXT.HQR> <language> <file> [id,id...|size]: the texts of one text file (all, or the ids given), or its count and size.
internal static class Texts
{
    public static int Run(string[] args)
    {
        var bank = LBAAssembler.Lba2TextBank.Load(LBAAssembler.HqrArchive.Open(args[1]), int.Parse(args[2]), int.Parse(args[3]));
        if (args.Length > 4 && args[4] == "size") { Console.WriteLine($"{bank.Texts.Count} texts, {bank.Encode().Data.Length} bytes"); return 0; }
        var only = args.Length > 4 ? args[4].Split(',').Select(int.Parse).ToHashSet() : null;
        // (the game's code page, 850: Latin-1 would show its accented letters as others)
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var dos = System.Text.Encoding.GetEncoding(850);
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        foreach (var t in bank.Texts.Where(t => only == null || only.Contains(t.Id)))
            Console.WriteLine($"{t.Id} [{t.Attribute}] {dos.GetString(t.Bytes)}");
        return 0;
    }
}

internal static class ScriptGrep
{
    public static int Run(string[] args)
    {
        var store = new LBAAssembler.Scenes.SceneStore(LBAAssembler.Scenes.SceneGame.Lba1, args[1]);
        for (var s = 0; s < 120; s++)
        {
            byte[] record;
            try { record = store.LoadRecord(s); } catch { continue; }
            var scripts = LBAAssembler.LbaScript.SceneScripts.Load(record, s, null, lba1: true);
            var model = LBAAssembler.Scenes.SceneSerializer.Parse(LBAAssembler.Scenes.SceneGame.Lba1, record);
            for (var a = 0; a < model.Actors.Count; a++)
            {
                var text = scripts.GetText(a, LBAAssembler.LbaScript.ScriptKind.Life);
                foreach (var line in text.Split('\n').Where(l => l.Contains(args[2]))) Console.WriteLine($"scene {s} actor {a}: {line.Trim()}");
            }
        }
        return 0;
    }
}

// actordump <folder> <scene> <actor>: the fields of one LBA1 actor.
internal static class ActorDump
{
    public static int Run(string[] args)
    {
        var game = args.Length > 4 && args[4] == "lba2" ? LBAAssembler.Scenes.SceneGame.Lba2 : LBAAssembler.Scenes.SceneGame.Lba1;
        var store = new LBAAssembler.Scenes.SceneStore(game, args[1]);
        var a = store.Load(int.Parse(args[2])).Actors[int.Parse(args[3])];
        Console.WriteLine($"flags 0x{a.Flags:X} sprite {a.IsSprite} entity {a.Entity} body {a.Body} anim {a.Anim} sprite {a.Sprite} pos ({a.X},{a.Y},{a.Z}) beta {a.Beta} srot {a.SRot} move {a.Move} hitforce {a.HitForce} option 0x{a.OptionFlags:X} info [{string.Join(",", a.Info)}] nbbonus {a.NbBonus} coul {a.CoulObj} armor {a.Armor} life {a.LifePoints}");
        return 0;
    }
}

// dummybody: what Assets/DummyBody.lm2 (the editor's placeholder actor body) is made of.
internal static class DummyBodyStudy
{
    public static int Run(string[] args)
    {
        var bytes = File.ReadAllBytes(Path.Combine("Assets", "DummyBody.lm2"));
        var body = LbaBodyStudio.Body.Read(bytes, 2, allowStatic: true);
        var xs = body.Vertices.Select(v => v.X).ToList(); var ys = body.Vertices.Select(v => v.Y).ToList(); var zs = body.Vertices.Select(v => v.Z).ToList();
        Console.WriteLine($"{bytes.Length} bytes; static {body.Static}; {body.Vertices.Count} points, {body.Bones.Count} bones, {body.Faces.Count} faces, {body.Lines.Count} lines, {body.Spheres.Count} spheres, textures {body.Textures.Length}");
        Console.WriteLine($"x {xs.Min()}..{xs.Max()} y {ys.Min()}..{ys.Max()} z {zs.Min()}..{zs.Max()}");
        foreach (var g in body.Faces.GroupBy(f => (f.Points.Length, f.Colour, f.Texture is not null)).OrderByDescending(g => g.Count()).Take(12)) Console.WriteLine($"  faces: {g.Key.Item1} corners colour {g.Key.Colour} textured {g.Key.Item3}: {g.Count()}");
        foreach (var l in body.Lines.Take(10)) Console.WriteLine($"  line {l.A}-{l.B} colour {l.Colour}: ({body.Vertices[l.A]}) -> ({body.Vertices[l.B]})");
        foreach (var s in body.Spheres.Take(10)) Console.WriteLine($"  sphere at {body.Vertices[s.Point]} radius {s.Radius} colour {s.Colour}");
        return 0;
    }
}

// buttonjump <folder> <scene> <actor>...: drop Twinsen onto each of the given (sprite) actors from above and say what the engine does: is the actor "hit by" him, does its scene variable get set?
internal static class ButtonJump
{
    public static int Run(string[] args)
    {
        var data = new LBAAssembler.Lba1.Runtime.Lba1RuntimeData(args[1]);
        var scene = int.Parse(args[2]);
        foreach (var a in args.Skip(3).Select(int.Parse))
        {
            var rt = new LBAAssembler.Lba1.Runtime.Lba1Runtime(data);
            rt.ChangeCube(scene);
            rt.Run(30);
            var b = rt.Objects[a];
            var before = string.Join("", Enumerable.Range(0, 8).Select(i => rt.FlagCube[i]));
            Console.WriteLine($"actor {a}: at ({b.PosX},{b.PosY},{b.PosZ}) box x {b.XMin}..{b.XMax} y {b.YMin}..{b.YMax} z {b.ZMin}..{b.ZMax} flags 0x{b.Flags:X} life {b.LifePoint}; var_cube before {before}");
            rt.Place(b.PosX, b.PosY + b.YMax + 700, b.PosZ, 0);
            var log = new List<string>();
            for (var f = 0; f < 120; f++)
            {
                rt.Frame();
                if (f % 10 == 9) log.Add($"y{rt.Hero.PosY}");
            }
            var after = string.Join("", Enumerable.Range(0, 8).Select(i => rt.FlagCube[i]));
            Console.WriteLine($"   after the drop: hero y trace {string.Join(" ", log.Take(6))}; hero at ({rt.Hero.PosX},{rt.Hero.PosY},{rt.Hero.PosZ}); HitBy {b.HitBy}; var_cube {after}; button dead {b.IsDead} invisible {(b.Flags & LBAAssembler.Lba1.Runtime.Lba1Const.Invisible) != 0}");
        }
        return 0;
    }
}

// floorat <folder> <scene> <x> <z>: where Twinsen comes to rest when dropped at (x, z) from high up, and the hero's own box.
internal static class FloorAt
{
    public static int Run(string[] args)
    {
        var data = new LBAAssembler.Lba1.Runtime.Lba1RuntimeData(args[1]);
        var rt = new LBAAssembler.Lba1.Runtime.Lba1Runtime(data);
        rt.ChangeCube(int.Parse(args[2]));
        rt.Run(30);
        rt.Place(int.Parse(args[3]), 6000, int.Parse(args[4]), 0);
        rt.Run(200);
        Console.WriteLine($"rests at ({rt.Hero.PosX},{rt.Hero.PosY},{rt.Hero.PosZ}); hero box x {rt.Hero.XMin}..{rt.Hero.XMax} y {rt.Hero.YMin}..{rt.Hero.YMax}");
        return 0;
    }
}

// buttonjump2 <folder> <actor> [nocarrier]: in scene 99 Twinsen (jumping, sportive) runs at button <actor> from the room side and jumps; with `nocarrier` the button's carrier flag is cleared first
// (an in-memory change only). Prints what the engine's rules do.
internal static class ButtonJump2
{
    public static int Run(string[] args)
    {
        var data = new LBAAssembler.Lba1.Runtime.Lba1RuntimeData(args[1]);
        var a = int.Parse(args[2]);
        var noCarrier = args.Contains("nocarrier");
        var rt = new LBAAssembler.Lba1.Runtime.Lba1Runtime(data);
        rt.ChangeCube(99);
        rt.Run(30);
        rt.SetBehaviour(LBAAssembler.Lba1.Runtime.Lba1Const.CSportif);
        var b = rt.Objects[a];
        if (noCarrier) b.Flags &= ~LBAAssembler.Lba1.Runtime.Lba1Const.ObjCarrier;
        rt.Place(b.PosX + 900, b.PosY, b.PosZ, 768);      // east of it, facing west, on the floor
        rt.Run(10);
        var maxY = 0;
        for (var f = 0; f < 200; f++)
        {
            rt.Joy = f < 60 ? LBAAssembler.Lba1.Runtime.Lba1Const.JUp : 0;
            rt.Fire = f == 8 || f == 30 || f == 50 ? LBAAssembler.Lba1.Runtime.Lba1Const.FSpace : 0;
            rt.Frame();
            maxY = Math.Max(maxY, rt.Hero.PosY);
            if (f % 15 == 0) Console.WriteLine($"  f{f}: hero ({rt.Hero.PosX},{rt.Hero.PosY},{rt.Hero.PosZ}) anim {rt.Hero.GenAnim}; button HitBy {b.HitBy}, var_cube({a}) {rt.FlagCube[a]}");
        }
        Console.WriteLine($"button {a}{(noCarrier ? " (not a carrier)" : "")}: highest the hero got {maxY} (floor 2304, button top {b.PosY + b.YMax}); HitBy {b.HitBy}; var_cube({a}) = {rt.FlagCube[a]}; button visible {(b.Flags & LBAAssembler.Lba1.Runtime.Lba1Const.Invisible) == 0}");
        return 0;
    }
}

// jumpheight <folder> <scene> <x> <z>: how high Twinsen's feet get in a standing jump and a running jump (sportive) from the floor at (x, z).
internal static class JumpHeight
{
    public static int Run(string[] args)
    {
        var data = new LBAAssembler.Lba1.Runtime.Lba1RuntimeData(args[1]);
        foreach (var running in new[] { false, true })
        {
            var rt = new LBAAssembler.Lba1.Runtime.Lba1Runtime(data);
            rt.ChangeCube(int.Parse(args[2]));
            rt.Run(30);
            rt.SetBehaviour(LBAAssembler.Lba1.Runtime.Lba1Const.CSportif);
            rt.Place(int.Parse(args[3]), 6000, int.Parse(args[4]), 768);
            rt.Run(150);
            var floor = rt.Hero.PosY;
            var max = floor;
            for (var f = 0; f < 120; f++)
            {
                rt.Joy = running ? LBAAssembler.Lba1.Runtime.Lba1Const.JUp : 0;
                rt.Fire = f == 5 ? LBAAssembler.Lba1.Runtime.Lba1Const.FSpace : 0;
                rt.Frame();
                max = Math.Max(max, rt.Hero.PosY);
            }
            Console.WriteLine($"{(running ? "running" : "standing")} jump from floor {floor}: feet reach {max} (+{max - floor})");
        }
        return 0;
    }
}

// lba2actordump <folder> <scene> <indexInScene>: one LBA2 actor's fields as SceneStore reads them (indexInScene excludes the hero, i.e. 0 is the scene's first real actor).
internal static class Lba2ActorDump
{
    public static int Run(string[] args)
    {
        var store = new LBAAssembler.Scenes.SceneStore(LBAAssembler.Scenes.SceneGame.Lba2, args[1]);
        var model = store.Load(int.Parse(args[2]));
        var a = model.Actors[int.Parse(args[3]) + 1];
        Console.WriteLine($"entity {a.Entity} body {a.Body} anim {a.Anim} pos ({a.X},{a.Y},{a.Z}) beta {a.Beta} flags 0x{a.Flags:X} life {a.LifePoints} armor {a.Armor} hit {a.HitForce} move {a.Move}");
        return 0;
    }
}
