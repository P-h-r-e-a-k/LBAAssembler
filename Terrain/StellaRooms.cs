using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Stella Entities' office (scene 391) and backstage (scene 392): built in full behind their doors, though the bouncers keep Twinsen out for
// now (the user, 2026-11-11 late: "even if we can't access them initially I'm sure I'll add something later to allow us to") -- so the
// club's scene has no way in yet; each room's own door leads back out to the hall, at the door it belongs to (CitadelProps.OfficeAt,
// BackstageAt), so a way in can be added later by a zone there.
//   - the office: Rick's office (scene 137), its grid, its light and music and its furniture as the game has them (the desk and the lamps),
//     without Rick, his guards and their scripts;
//   - backstage: the same room (the grid is shared), furnished for the performers -- a rack, chairs, mirrors -- instead.
// Since 2026-11-14 the rooms' grid is their own, a copy of Rick's (the user, 2026-11-13: "The office we've added is a direct copy of the one
// from the bar on Otringal, we need to change it. The hole in the wall needs to not have a transition zone, and let's add a picture here,
// let's also replace the picture on the back wall, and change the colours of the wall to pull our space theme through. Let's have the new
// pictures be of some of our strippers"): every block of his teal stucco walls painted the night, dark blue flecked with stars; the air
// vent in his west wall (his escape, its door zone gone) and his painting on the north wall walled over, a poster of the club's
// performers on each (StellaPosters) in the office.
internal static class StellaRooms
{
    public const int Office = 391, Backstage = 392, RickOffice = 137;
    // the private room (scene 395: 393 and 394 are the bowling alley's and the Francos' tunnels'): the same room again, curtained, a low
    // sofa where Twinsen sits for a private show (RaceTrackStory.StellaEntities: the hostess's offer in the hall, the show there) -- the
    // user, 2026-11-13, "Let's have the curtained booths in a back area that a dancer can lead him off into"
    public const int Private = 395;
    public const int SofaX = 6256, SofaZ = 3300, ShowZ = 4400;
    // (where Twinsen comes in from the club, either room: just inside the door of Rick's office's that leads out, x 5120-6144 at z 9216)
    public const int InsideX = 5632, InsideY = 256, InsideZ = 8500;
    private const int DoorwayZ = 9216;   // (Rick's doorway: his door's sprite stands past it, in the door zone)
    private const int Prop = Citadel.CitadelProps.Chair;
    // Rick's walls: the west wall's block (its pattern: z mod 5 -- z 12 its positions 35-39 up from layer 6, z 13 its 5-9) and the vent in it
    // (x 4, z 12-13, layers 6-9); the north wall's (x 5-14, z 4: its column x mod 4 -- 20-23, 4 more a layer up) and the painting in it
    // (block 195, x 11-13, layers 6-9)
    // (Rick's furniture, bricks: his armchairs, his desk and its ends, what stands on it -- gone from the private room's grid: the dancer
    // stood in an armchair)
    private static readonly HashSet<int> Furniture = new() { 78, 82, 143, 144, 145, 62, 205 };
    private const int WestWall = 23, NorthWall = 28, Painting = 195, WallX = 4, VentZ = 12, NorthZ = 4, PaintingX = 11;
    // the posters' places: on the west wall over the vent, on the north wall over the painting (the walls' inner faces, a little off them)
    private const int SidePosterX = (WallX + 1) * 512 + 24, SidePosterZ = (VentZ + 1) * 512, BackPosterX = PaintingX * 512 + 768, BackPosterZ = (NorthZ + 1) * 512 + 24, PosterY = 1530;

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(RickOffice) || !store.SceneExists(StellaEntities.Scene)) { log.Add("Stella Entities' office and backstage left out: no Rick's office (scene 137) or no club scene"); return log; }
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        if (Backstage + 1 < scenes.Count && !scenes.IsEmpty(Backstage + 1)) { log.Add($"Stella Entities' rooms left out: scene {Backstage} is already another part of the build's"); return log; }
        if (Private + 1 < scenes.Count && !scenes.IsEmpty(Private + 1)) { log.Add($"Stella Entities' rooms left out: scene {Private} is already another part of the build's"); return log; }
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        var type = table[RickOffice * 2];
        var (grid, _, privateGrid) = NightGrid(gameDirectory, bkg, header, table[RickOffice * 2 + 1], log);
        if (grid < 0 || privateGrid < 0) return log;
        table = bkg.Read(header.Brk + header.MaxBrk);
        if (table.Length < (Private + 1) * 2) Array.Resize(ref table, (Private + 1) * 2);
        foreach (var s in new[] { Office, Backstage, Private }) { table[s * 2] = type; table[s * 2 + 1] = (byte)(s == Private ? privateGrid : grid); }
        bkg.SetStored(header.Brk + header.MaxBrk, table);

        var original = store.Load(RickOffice);
        var (outX, outZ) = StellaEntities.At(Citadel.CitadelProps.OfficeAt, 14.5f);
        // (back out of backstage: onto the catwalk by its door, at the stage's height -- not on the floor at 14.5 cells, inside the
        // catwalk's cells; the engine adds where in the room's door zone he was, 0-1024 along x and 0-512 along z)
        var (backX, backZ) = (StellaEntities.CatwalkX0World, StellaEntities.CatwalkEndZ + 600);
        static string Still() => "void comportement_0()\n{\n}\n";
        var records = new List<(int Scene, byte[] Record)>();
        var (privX, privZ) = StellaEntities.At(215, 15.5f);
        foreach (var (number, name, outAt) in new[] { (Office, "office", (outX, outZ)), (Backstage, "backstage", (backX, backZ)), (Private, "private room", (privX, privZ)) })
        {
            var model = original.Clone();
            model.Island = StellaEntities.MoonIsland;
            model.TrackPoints.Clear();
            // the furniture: the game's own entity 16's with a body (the desk and what stands round it) and its lamps (sprites) -- the office's;
            // backstage has none of them
            // (actor 1 kept as it is in every room: the engine's mechanical penguin's place -- whatever stands there is killed as each scene
            // loads, OBJECT.CPP RestartMecaPingouin; the private room's sofa and backstage's rack were, there)
            var keep = new List<SceneActorModel> { model.Actors[0], model.Actors[1] };
            // (backstage too, since 2026-11-14: the user, there, "we need the desk and chair back, along with the posters on the wall")
            // (not his door, a sprite in the doorway at z 9472: its own scripts open it, these rooms' are still -- shut for ever, Twinsen
            // kept in: the doorway open instead, 2026-11-14)
            if (number is Office or Backstage) keep.AddRange(model.Actors.Skip(2).Where(a => ((a.Entity == 16 && a.Body >= 0) || a.IsSprite) && !(a.IsSprite && a.Z >= DoorwayZ)));
            model.Actors.Clear(); model.Actors.AddRange(keep);
            // (his door, not the vent's way out: a door zone up the west wall, from layer 3)
            model.Zones.RemoveAll(z => z.Type != 0 || z.Y0 >= 512);
            foreach (var z in model.Zones.Where(z => z.Type == 0))
            {
                z.Num = StellaEntities.Scene;
                z.Info = new[] { outAt.Item1, number == Backstage ? StellaEntities.FloorY + (int)Citadel.CitadelProps.StageHigh : StellaEntities.FloorY, outAt.Item2, 0, 0, 0, 0, 1 };   // (the last: the door on)
            }
            if (number is Office or Backstage)
            {
                SceneOps.AddActor(model, Citadel.CitadelProps.Actor(StellaPosters.PosterSide, SidePosterX, PosterY, SidePosterZ, 1024));
                SceneOps.AddActor(model, Citadel.CitadelProps.Actor(StellaPosters.PosterBack, BackPosterX, PosterY, BackPosterZ, 0));
            }
            if (number == Private)
            {
                // the sofa (a booth's bench, on the floor: its seat on the private grid's solid cells) and the curtains round it; Twinsen on
                // it as he comes in
                var sofa = Citadel.CitadelProps.Actor(Citadel.CitadelProps.ClubBooth, SofaX, 256, SofaZ - 200, 0);
                sofa.Armor = RaceTrackStory.WalkOverArmour;
                SceneOps.AddActor(model, sofa);
                foreach (var (generic, x, z, beta) in new[] { (Citadel.CitadelProps.CurtainLeft, SofaX - 1500, SofaZ + 300, 1024), (Citadel.CitadelProps.CurtainRight, SofaX + 1500, SofaZ + 300, 3072), (Citadel.CitadelProps.CurtainLeft, SofaX - 1500, ShowZ + 900, 1024), (Citadel.CitadelProps.CurtainRight, SofaX + 1500, ShowZ + 900, 3072) })
                    SceneOps.AddActor(model, Citadel.CitadelProps.Actor(generic, x, 256, z, beta));
                (model.Actors[0].X, model.Actors[0].Y, model.Actors[0].Z, model.Actors[0].Beta) = (SofaX, 256 + Citadel.CitadelProps.BoothSeat, SofaZ, 0);
            }
            if (number == Backstage)
            {
                // the dressing room: a rack of the performers' things at the back, chairs at the mirrors, mirrors (pictures) on the wall
                foreach (var (generic, x, z, beta) in new[] { (Citadel.CitadelProps.Rack, 7600, 2900, 0) })   // (its chairs gone with Rick's desk and armchairs back: they stood in them, 2026-11-14)
                    SceneOps.AddActor(model, Citadel.CitadelProps.Actor(generic, x, 256, z, beta));
            }
            var scripts = LbaScript.SceneScripts.Load(SceneSerializer.Write(model), number);
            for (var i = 0; i < model.Actors.Count; i++)
            {
                scripts.SetText(i, LbaScript.ScriptKind.Life, Still());
                scripts.SetText(i, LbaScript.ScriptKind.Track, "label(0);\nstop();\n");
            }
            var built = scripts.Build();
            if (!built.Ok) { log.AddRange(built.Errors.Select(e => $"Stella Entities' {name} left out: {e}")); return log; }
            records.Add((number, built.Record!));
        }
        foreach (var (number, record) in records)
        {
            var entry = number + 1;
            while (scenes.Count <= entry) scenes.Slots.Add(new HqrFile.Slot());
            scenes.SetStored(entry, record);
            if (record.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
            { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, record.Length); scenes.SetStored(0, size); }
        }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        log.Add($"Stella Entities' rooms: the office (scene {Office}), backstage (scene {Backstage}) and the private room (scene {Private}) on grid {grid} (Rick's office's, its walls the night), each with its door back to the hall");
        return log;
    }

    // Blocks of a library painted the night (copies of them, added to it): those mostly blue -- a third of their picture or more in blue ramps
    // (a ramp's middle shade bluer than red) -- every ramp of a fiftieth or more of them dark blue, a star here and there. The new block of each.
    internal static Dictionary<int, int> NightBlocks(string gameDirectory, HqrFile bkg, MapBricks.BkgHeader header, ref byte[] library, IEnumerable<int> blocks)
    {
        byte[]? Brick(int b) => b >= 0 && b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? bkg.Read(header.Brk + b) : null;
        var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
        bool Blue(int r) { int k = (r * 16 + 8) * 3; return palette[k + 2] > palette[k] + 30; }
        var night = Enumerable.Range(0, 16).Select(s => Citadel.CitadelMuseum.Nearest(palette, 10 + s, 12 + s, 32 + s * 2)).ToArray();
        byte white = Citadel.CitadelMuseum.Nearest(palette, 235, 235, 250), yellow = Citadel.CitadelMuseum.Nearest(palette, 250, 236, 150);
        var made = new Dictionary<int, int>();
        foreach (var block in blocks.Distinct().OrderBy(b => b))
        {
            var counts = new int[16];
            Citadel.CitadelMuseum.Repaint(library, block, Brick, (x, y, c) => { counts[c >> 4]++; return c; });
            var all = counts.Sum();
            if (all == 0 || Enumerable.Range(0, 16).Where(Blue).Sum(r => counts[r]) * 3 < all) continue;
            var ramps = Enumerable.Range(0, 16).Where(r => counts[r] * 50 >= all).ToHashSet();   // (a fiftieth: the clouds' outlines too)
            var b0 = block;
            var painted = Citadel.CitadelMuseum.Repaint(library, block, Brick, (x, y, c) =>
            {
                if (!ramps.Contains(c >> 4)) return c;
                var h = unchecked(((uint)x * 73856093u) ^ ((uint)y * 19349663u) ^ ((uint)b0 * 83492791u)) * 2654435761u;
                return (h >> 7) % 97 == 0 ? white : (h >> 7) % 173 == 1 ? yellow : night[c & 15];
            });
            if (painted.Count == 0) continue;
            try { made[block] = Citadel.CitadelMuseum.AddBlock(bkg, header, ref library, block, painted); }
            catch (InvalidDataException) { }
        }
        return made;
    }

    // Rick's office's grid copied (`rick`), his stucco's blocks painted the night (copies of them, in his block library), the vent and the
    // painting walled over: the new grid's number (or -1, the log says why). The file's header kept up to date; the bricks' table read
    // again after (bricks added before it).
    private static (int Rooms, int Backstage, int Private) NightGrid(string gameDirectory, HqrFile bkg, MapBricks.BkgHeader header, int rick, List<string> log)
    {
        var raw = bkg.Read(header.Gri + rick);
        var libraryEntry = header.Bll + raw[0];
        var library = bkg.Read(libraryEntry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        byte[]? Brick(int b) => b >= 0 && b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? bkg.Read(header.Brk + b) : null;
        var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
        const int Size = 64, Height = 25;
        var used = new HashSet<int>();
        for (var z = 0; z < Size; z++) for (var y = 0; y < Height; y++) for (var x = 0; x < Size; x++)
        { var c = Lba1GridEdit.Get(shape, x, y, z); if (c.Block != 0) used.Add(c.Block); }
        // the stucco: the west wall's commonest colour ramp
        var counts = new int[16];
        Citadel.CitadelMuseum.Repaint(library, WestWall, Brick, (x, y, c) => { counts[c >> 4]++; return c; });
        var stucco = Array.IndexOf(counts, counts.Max());
        // (and its flecks: every ramp a tenth of it or more -- painted in the stucco's blocks only, those mostly of its own ramp: not his
        // stone's greys, his basin's, his lamps')
        var ramps = Enumerable.Range(0, 16).Where(r => r == stucco || counts[r] * 10 >= counts.Sum()).ToHashSet();
        // its shades the night's: dark blues; a star here and there, white or pale yellow
        var night = Enumerable.Range(0, 16).Select(s => Citadel.CitadelMuseum.Nearest(palette, 10 + s, 12 + s, 32 + s * 2)).ToArray();
        byte white = Citadel.CitadelMuseum.Nearest(palette, 235, 235, 250), yellow = Citadel.CitadelMuseum.Nearest(palette, 250, 236, 150);
        // (one shade of the night for the whole wall, the stucco's commonest shade's: its other shades -- the wallpaper's waves -- showed through
        // as darker waves, the user, 2026-11-14: "let's have them uniform")
        var shades = new int[16];
        Citadel.CitadelMuseum.Repaint(library, WestWall, Brick, (x, y, c) => { if (c >> 4 == stucco) shades[c & 15]++; return c; });
        var flat = night[Array.IndexOf(shades, shades.Max())];
        var swap = new Dictionary<int, int>();
        foreach (var block in used.OrderBy(b => b))
        {
            if (block == Painting) continue;   // (walled over)
            int all = 0, teal = 0;
            var blockCounts = new int[16];
            Citadel.CitadelMuseum.Repaint(library, block, Brick, (x, y, c) => { all++; blockCounts[c >> 4]++; if (c >> 4 == stucco) teal++; return c; });
            if (all == 0 || teal * 100 < all * 35) continue;
            // (in a stucco block, every ramp a fiftieth of it or more as well: the wallpaper's darker waves' outlines were left showing through
            // the night -- the user, 2026-11-14, "on the wall ... is still the outline of the waves from the original walls, let's have them
            // uniform")
            var blockRamps = new HashSet<int>(ramps);
            for (var r = 0; r < 16; r++) if (blockCounts[r] * 50 >= all) blockRamps.Add(r);
            log.Add($"Stella Entities' rooms: block {block}'s ramps painted the night: {string.Join(", ", blockRamps.OrderBy(r => r))} (its ramps: {string.Join(", ", Enumerable.Range(0, 16).Where(r => blockCounts[r] > 0).Select(r => $"{r} x{blockCounts[r]}"))})");
            var painted = Citadel.CitadelMuseum.Repaint(library, block, Brick, (x, y, c) =>
            {
                if (!blockRamps.Contains(c >> 4)) return c;
                var h = unchecked(((uint)x * 73856093u) ^ ((uint)y * 19349663u) ^ ((uint)block * 83492791u)) * 2654435761u;
                return (h >> 7) % 97 == 0 ? white : (h >> 7) % 173 == 1 ? yellow : flat;
            });
            if (painted.Count == 0) continue;
            try { swap[block] = Citadel.CitadelMuseum.AddBlock(bkg, header, ref library, block, painted); }
            catch (InvalidDataException e) { log.Add($"Stella Entities' rooms: block {block} left as it was -- {e.Message}"); }
        }
        int B(int b) => swap.TryGetValue(b, out var n) ? n : b;
        var set = new Dictionary<(int, int, int), (int Block, int Pos)>();
        for (var z = 0; z < Size; z++) for (var y = 0; y < Height; y++) for (var x = 0; x < Size; x++)
        { var c = Lba1GridEdit.Get(shape, x, y, z); if (swap.ContainsKey(c.Block)) set[(x, y, z)] = (swap[c.Block], c.Pos); }
        for (var y = 6; y <= 9; y++)
        {
            set[(WallX, y, VentZ)] = (B(WestWall), 35 + y - 6);
            set[(WallX, y, VentZ + 1)] = (B(WestWall), 5 + y - 6);
        }
        // (the painting, layers 4-9: the north wall's columns 1-3 there, as on the wall's layer 3 under it -- position column + 4 a layer up
        // from layer 1)
        for (var y = 4; y <= 9; y++)
            for (var k = 0; k < 3; k++)
                set[(PaintingX + k, y, NorthZ)] = (B(NorthWall), 1 + k + 4 * (y - 1));
        bkg.SetStored(libraryEntry, library);
        if (header.MaxBll < (uint)library.Length) header.MaxBll = (uint)library.Length;
        int Insert(Dictionary<(int, int, int), (int Block, int Pos)> cells, string what)
        {
            var grid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, cells.Select(kv => new Lba1GridCell(kv.Key.Item1, kv.Key.Item2, kv.Key.Item3, kv.Value.Block, kv.Value.Pos)).ToList()));
            grid[0] = raw[0];
            var number = header.Grm - header.Gri;
            if (number > 255) { log.Add("Stella Entities' rooms left out: LBA_BKG.HQR has 256 grids already"); return -1; }
            bkg.InsertAt(header.Grm, grid);
            header.Grm += 1; header.Bll += 1; header.Brk += 1;
            if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
            var (bytes, n) = MapBricks.UsedBrickBytes(grid, library, b => b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? BinaryPrimitives.ReadUInt32LittleEndian(bkg.ExtentOf(header.Brk + b)) : 0);
            if (header.MaxBrickCube < (uint)((n + 1) * 4 + bytes)) header.MaxBrickCube = (uint)((n + 1) * 4 + bytes);
            if (header.MaxMaskCube < (uint)((n + 1) * 4 + bytes + n * 64)) header.MaxMaskCube = (uint)((n + 1) * 4 + bytes + n * 64);
            bkg.SetStored(0, header.Bytes);
            log.Add($"Stella Entities' rooms: grid {number}, {what}");
            return number;
        }
        var rooms = Insert(set, $"Rick's office's with its stucco (colour ramps {string.Join(", ", ramps)}) the night in {swap.Count} blocks ({string.Join(", ", swap.Select(kv => $"{kv.Key} as {kv.Value}"))}), the vent and the painting walled over");
        if (rooms < 0) return (-1, -1, -1);
        // the private room's: the same without Rick's furniture
        var bare = new Dictionary<(int, int, int), (int Block, int Pos)>(set);
        for (var z = 0; z < Size; z++) for (var y = 0; y < Height; y++) for (var x = 0; x < Size; x++)
            if (Furniture.Contains(Lba1GridEdit.Get(shape, x, y, z).Block)) bare[(x, y, z)] = (0, 0);
        // (backstage on the office's grid, Rick's desk and armchairs kept: the user, 2026-11-14)
        var backstage = rooms;
        // (and the private room's: under the sofa's seat, solid invisible cells up to it -- as under the hall's booths: Twinsen sits on them)
        var seated = new Dictionary<(int, int, int), (int Block, int Pos)>(bare);
        for (var x = (SofaX - 900) / 512; x <= (SofaX + 900) / 512; x++)
            for (var z = (SofaZ - 480) / 512; z <= (SofaZ + 80) / 512; z++)
                for (var y = 1; y <= Citadel.CitadelProps.BoothSeat / 256; y++) seated[(x, y, z)] = (0, 1);
        return (rooms, backstage, Insert(seated, "the same, the private room's: the sofa's seat solid"));
    }

}
