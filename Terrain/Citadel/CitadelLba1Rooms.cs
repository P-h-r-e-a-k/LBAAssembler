using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// Two of LBA1's rooms on Citadel Island, back as easter eggs (the user, 2026-11-04: "Let's make all those changes", of the easter eggs
// proposal's picks): "The LBA1 secret room in the sewers ... one stretch of wall has a brick a different colour. A magic ball on it (or
// Action in aggressive mode) and it slides back: a small room in LBA1's colours with three old signs leaning on the wall ... and a clover
// box", and "The Tavern's cellar, 'exactly as it was' ... behind Tim's bar, a hatch that was never there in LBA2. Down it: LBA1's Tavern
// cellar, rebuilt by the Esmers along with everything else". The build's side is here, the story's (who stands where, what is said, the
// clover boxes' variables) RaceTrackStory.Lba1Rooms (Terrain/Citadel/RaceTrackStory.Lba1Rooms.cs):
//   - the rooms: LBA1's own -- the secret sewer (LBA1's scene 55, off its first sewer, 34) and the Tavern's cellar (LBA1's scene 33, under
//     its Tavern, 14) -- ported as LBA2 interiors (Maps/Lba1InteriorPort: LBA1's grid, blocks and bricks, each brick recoloured to the LBA2
//     colour nearest LBA1's, so the rooms keep LBA1's colours as near as the one palette an interior is drawn in allows), each with a
//     block library and bricks of its own in LBA_BKG.HQR (MapBricks.Install, as a map's interior goes in) and a grid after the game's;
//     each a new scene (SecretRoom, Cellar: slots no build uses, under 255 so that change_cube() can name them, and the holomap's tables
//     hold them), its header the scene the way in is from (the sewers', the Tavern's: light, music, ambience), Twinsen alone in it with
//     the way back out (a change-cube zone where LBA1 had its own door);
//   - the loose stretch of wall: in the 1996 demo's sewers (Demo96Rooms.Sewers, scene 17), the landing at the foot of the stairs from the
//     far halls' door, beside the "LBA" plaque -- its back wall's cell there emptied from the landing's floor up (a doorway into the dark
//     behind it), for a slab of bricks of another colour (a body of the build's: SlabGeneric) to stand in and slide back from;
//   - the props' bodies: the slab, the three old signs (wooden boards leaning on the wall, painted -- what each says is the story's
//     line), and the cellar's dusty bottle -- bodies of the small brown mushroom's entity (112, a fixed object, as RaceTrackOil's slicks
//     are), generic numbers no other build gives it.
internal static class CitadelLba1Rooms
{
    // the scenes: the secret room, the cellar (and the scenes they are entered from: the demo's sewers, the Tavern)
    public const int SecretRoom = 226, Cellar = 227, Sewers = 17, Tavern = 3;
    // LBA1's scenes they are
    public const int Lba1SecretSewer = 55, Lba1Cellar = 33;

    // ---- the secret room (scene 226: LBA1's 55, its walls round x 49-63, z 48-63 of the grid as LBA1 has them) -----------------------
    // Twinsen comes in from the sewers walking west, where LBA1's own way in from its first sewer was (the east wall's doorway, x 63, z 54-57,
    // layers 9-16 open over the floor), a few cells in -- clear of that tall wall, which hides him from the camera -- and goes back out
    // there: LBA1's own zone (from x 32000) across the doorway (less Twinsen's width at its edges: z 27904-29439, as wide as the sewers'
    // landing he comes out on, his place in the zone added -- RoomExit, SewerReturn)
    public static readonly (int X, int Y, int Z) RoomArrival = (29184, 2304, 28160);
    public static readonly (int X0, int X1, int Z0, int Z1, int Y0, int Y1) RoomExit = (32000, 32767, 27904, 29439, 2304, 3839);
    // ---- the loose stretch of wall in the demo's sewers (scene 17) -------------------------------------------------------------------------
    // its cell: the back wall (x 0) at the foot of the stairs down from the far halls' door, beside the "LBA" plaque (the wall's cells of z 25
    // and 26, layers 5-7). Of the landing there only cells 1-2 of z 24-26 are floor to stand on (sound 04): the rest is the demo's water's
    // edge, its bricks' high half 0xF -- the engine's CJ_ANIMATED_WATER: Twinsen drowns there. Layers 2-7 of the wall emptied (512 to
    // 2047) and the landing's floor (its cell in front, layer 1) carried into the doorway, over the wall's invisible block there.
    public const int HoleX = 0, HoleZ = 24, HoleFirst = 2, HoleLast = 7;
    public static int HoleMiddleZ => HoleZ * 512 + 256;
    // where Twinsen comes back out: on the landing in front of it, two cells out from the wall (the zone's corner: his place in the room's
    // door zone added -- x 0-50 or so of it, z 0-1535 -- he lands on the landing's floor, x 1024-1080, z 12288-13823)
    public static readonly (int X, int Y, int Z) SewerReturn = (1024, 512, 24 * 512);

    // ---- the cellar (scene 227: LBA1's 33) -----------------------------------------------------------------------------------------------
    // Twinsen comes down onto the top landing where LBA1's way down from the Tavern landed him (cell 0.5, layer 14, 11.5 there; a little
    // further from its door here), and goes back up through that door: LBA1's own zone (x 256-1791, z 6400-6911, from layer 14)
    public static readonly (int X, int Y, int Z) CellarArrival = (1280, 3584, 5376);
    public static readonly (int X0, int X1, int Z0, int Z1, int Y0, int Y1) CellarExit = (256, 1791, 6400, 6911, 3584, 5631);
    // behind Tim's bar in the Tavern (scene 3: the counter along x 5, z 18-27; behind it the floor, layer 3, cells 2-4 -- cell 1 is the
    // shelves at z 21-23): the hatch, far enough back from the counter for the camera to see it over the counter, clear of the bar's
    // search zone (x to 1488, z 11248-12784; Action in it is Twinsen searching) and of Luc's place (1824, 12352); and where the cellar's
    // door brings Twinsen up -- the zone's corner, his place in it added (the zone is x 256-1791, z 6400-6911, and he reaches it from the
    // landing, x 760-1800, z 6400 or so: 520 + that puts him behind the bar, x 1030-2070, z 10900)
    public static readonly (int X, int Y, int Z) Hatch = (1280, 1024, 10900);
    public static readonly (int X, int Y, int Z) TavernReturnCorner = (520, 1024, 10900);

    // ---- the props' bodies -----------------------------------------------------------------------------------------------------------------
    public const int PropEntity = 112, MushroomBody = 171;
    public const int SlabGeneric = 30, SignCheckpoint = 31, SignOrder = 32, SignWanted = 33, BottleGeneric = 34;
    // the slab: 448 units square (a cell less a little), as tall as the doorway
    public const int SlabHalf = 224, SlabHeight = (HoleLast - HoleFirst + 1) * 256;

    private sealed record Room(int Slot, int Lba1Scene, int BaseScene, string Name, (int X, int Y, int Z) Arrival,
        (int X0, int X1, int Z0, int Z1, int Y0, int Y1) Exit, int ExitTo, (int X, int Y, int Z) ExitLands);

    private static readonly Room[] Rooms =
    {
        new(SecretRoom, Lba1SecretSewer, Sewers, "LBA1's secret sewer room", RoomArrival, RoomExit, Sewers, SewerReturn),
        new(Cellar, Lba1Cellar, Tavern, "LBA1's Tavern cellar", CellarArrival, CellarExit, Tavern, TavernReturnCorner),
    };

    // Into a game folder (after the 1996 demo's sewers, Demo96Rooms, whose wall the slab's doorway is cut into): the rooms, their scenes,
    // the doorway, the props' bodies. Lines for the build's log.
    public static List<string> Install(string gameDirectory, string lba1Directory)
    {
        var log = new List<string>();
        if (!Lba1Game.IsInstalled(lba1Directory)) { log.Add("LBA1's secret sewer room and Tavern cellar left out: no LBA1 folder (File > Settings)"); return log; }
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);

        // ---- the doorway in the demo's sewers' wall
        log.Add(Doorway(bkg));

        // ---- the rooms
        foreach (var room in Rooms)
        {
            if (room.Slot + 1 < scenes.Count && !scenes.IsEmpty(room.Slot + 1)) { log.Add($"{room.Name} left out: scene {room.Slot} is taken"); continue; }
            var header = new MapBricks.BkgHeader(bkg.Read(0));
            var table = bkg.Read(header.Brk + header.MaxBrk);
            var type = room.BaseScene * 2 + 1 < table.Length ? table[room.BaseScene * 2] : (byte)1;
            var map = Lba1InteriorPort.Port(lba1Directory, room.Lba1Scene, palette, room.Name);
            var (library, fragments) = MapBricks.Install(map, bkg);
            var grid = (byte[])map.Grid!.Clone();
            grid[0] = (byte)library; grid[1] = (byte)fragments;
            // (the grid after the game's last, what follows it one entry on; its number in the scene-to-grid table)
            header = new MapBricks.BkgHeader(bkg.Read(0));
            var number = header.Grm - header.Gri;
            if (number > 255) { log.Add($"{room.Name} left out: LBA_BKG.HQR has 256 grids already"); continue; }
            bkg.InsertAt(header.Grm, grid);
            header.Grm += 1; header.Bll += 1; header.Brk += 1;
            if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
            bkg.SetStored(0, header.Bytes);
            table = bkg.Read(header.Brk + header.MaxBrk);
            if (table.Length < (room.Slot + 1) * 2) Array.Resize(ref table, (room.Slot + 1) * 2);
            table[room.Slot * 2] = type; table[room.Slot * 2 + 1] = (byte)number;
            bkg.SetStored(header.Brk + header.MaxBrk, table);

            // the scene: the way in's header, Twinsen alone (and the Meca-penguin's slot, actor 1), the way back out
            var model = store.Load(room.BaseScene).Clone();
            model.Actors.RemoveRange(2, model.Actors.Count - 2);
            model.Zones.Clear(); model.TrackPoints.Clear();
            var hero = model.Hero;
            (hero.X, hero.Y, hero.Z) = room.Arrival;
            hero.Life = new byte[] { 0 }; hero.Track = new byte[] { 0 };
            var e = room.Exit;
            model.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = room.ExitTo, X0 = e.X0, X1 = e.X1, Z0 = e.Z0, Z1 = e.Z1, Y0 = e.Y0, Y1 = e.Y1,
                // (Twinsen's height over the zone's foot is added to Info1: the floor he lands on)
                Info = new[] { room.ExitLands.X, room.ExitLands.Y, room.ExitLands.Z, 0, 0, 0, 0, 1 },
            });
            var entry = room.Slot + 1;
            while (scenes.Count <= entry) scenes.Slots.Add(new HqrFile.Slot());
            var record = SceneSerializer.Write(model);
            scenes.SetStored(entry, record);
            if (record.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
            { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, record.Length); scenes.SetStored(0, size); }
            log.Add($"{room.Name}: scene {room.Slot}, LBA1's scene {room.Lba1Scene} in grid {number} (block library {library}, {HqrFile.Parse(map.Bricks!).Count} bricks recoloured to LBA2's palette), " +
                    $"the header scene {room.BaseScene}'s, its door back to scene {room.ExitTo}");
        }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();

        // ---- the props' bodies
        log.Add(Bodies(gameDirectory));
        return log;
    }

    // The demo's sewers' back wall at the slab's cell emptied from layer HoleFirst to HoleLast and the landing's floor carried in under it,
    // when it is the demo's (the wall from layer 2 to 12 over an invisible block, the landing's floor in front of it at layer 1 and nothing
    // over that). A line for the log.
    private static string Doorway(HqrFile bkg)
    {
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (Sewers * 2 + 1 >= table.Length) return "the secret room's doorway left out: no scene 17";
        var gridEntry = header.Gri + table[Sewers * 2 + 1];
        var raw = bkg.Read(gridEntry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * 64 + x) * 25 + y) * 2;
        bool Filled(int x, int y, int z) => cells[At(x, y, z)] != 0;
        if (!Enumerable.Range(HoleFirst, 11).All(y => Filled(HoleX, y, HoleZ)) || !Filled(HoleX + 1, 1, HoleZ) || Enumerable.Range(2, 10).Any(y => Filled(HoleX + 1, y, HoleZ)))
            return "the secret room's doorway left out: scene 17's wall there isn't the 1996 demo's sewers'";
        var edits = Enumerable.Range(HoleFirst, HoleLast - HoleFirst + 1).Select(y => new Lba1GridCell(HoleX, y, HoleZ, 0, 0)).ToList();
        var floor = At(HoleX + 1, 1, HoleZ);
        edits.Add(new Lba1GridCell(HoleX, 1, HoleZ, cells[floor], cells[floor + 1]));
        var entry = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, edits));
        bkg.SetStored(gridEntry, entry);
        if (header.MaxGri < (uint)entry.Length) { header.MaxGri = (uint)entry.Length; bkg.SetStored(0, header.Bytes); }
        return $"the secret room's doorway: the demo's sewers' back wall (scene 17, cell {HoleX}, {HoleZ}) emptied from layer {HoleFirst} to {HoleLast}";
    }

    // Whether the doorway is there (the story's slab stands in it only then)
    public static bool HasDoorway(string gameDirectory)
    {
        var backend = new Lba2GridBackend(gameDirectory);
        if (backend.GridOfScene(Sewers) is not { } grid) return false;
        var cells = Lba1GridCodec.Decode(backend.LoadGrid(grid));
        bool Filled(int y) => cells[((HoleZ * 64 + HoleX) * 25 + y) * 2] != 0;
        return Filled(HoleFirst - 1) && Enumerable.Range(HoleFirst, HoleLast - HoleFirst + 1).All(y => !Filled(y)) && Filled(HoleLast + 1);
    }

    // ---- the props' bodies: BODY.HQR's end, the mushroom's entity's generic numbers --------------------------------------------------------
    private static string Bodies(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var header = Body.Read(HqrArchive.Open(bodyPath).Read(MushroomBody), 2, allowStatic: true).Header;
        var palette = HqrArchive.Open(ressPath).Read(0);
        int C(int r, int g, int b) => Nearest(palette, r, g, b);
        var table = HqrArchive.Open(ressPath).Read(44);
        var first = HqrArchive.CountEntries(bodyPath);
        var index = first;
        foreach (var (generic, body) in new[]
                 {
                     (SlabGeneric, Slab(C)), (SignCheckpoint, Sign(C, 0)), (SignOrder, Sign(C, 1)), (SignWanted, Sign(C, 2)), (BottleGeneric, Bottle(C)),
                 })
        {
            body.Header = header;
            body.Static = true;
            HqrWriter.AppendToFile(bodyPath, body.Write());
            table = RaceTrackBaldinoCar.WithBody(table, PropEntity, generic, index++);
        }
        HqrWriter.ReplaceInFile(ressPath, 44, table);
        return $"the LBA1 rooms' props: BODY.HQR entries {first}-{index - 1}, the mushroom's entity ({PropEntity}) bodies {SlabGeneric} (the sewers' loose slab), " +
               $"{SignCheckpoint}-{SignWanted} (the old signs), {BottleGeneric} (the cellar's dusty bottle)";
    }

    // The palette's colour nearest an RGB (redmean)
    private static int Nearest(byte[] palette, int r, int g, int b)
    {
        var best = 0; var bestD = double.MaxValue;
        for (var j = 1; j < 256; j++)
        {
            double pr = palette[j * 3], pg = palette[j * 3 + 1], pb = palette[j * 3 + 2];
            var rm = (r + pr) / 2;
            var d = (2 + rm / 256) * (r - pr) * (r - pr) + 4 * (g - pg) * (g - pg) + (2 + (255 - rm) / 256) * (b - pb) * (b - pb);
            if (d < bestD) { bestD = d; best = j; }
        }
        return best;
    }

    // A body being made: points and flat faces (drawn as they are, no light), one bone. A face's points go round so that (b - a) x (c - a)
    // points out of it (RaceTrackOil's winding: the engine leaves out a face seen from behind).
    private sealed class Mesh
    {
        public readonly List<Vector3> Points = new();
        public readonly List<Face> Faces = new();
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int colour)
        {
            var n = Points.Count;
            Points.AddRange(new[] { a, b, c, d });
            Faces.Add(new Face(new[] { n, n + 1, n + 2, n + 3 }, colour, Material: 0));
        }
        public void Poly(IReadOnlyList<Vector3> ring, int colour)
        {
            var n = Points.Count;
            Points.AddRange(ring);
            Faces.Add(new Face(Enumerable.Range(n, ring.Count).ToArray(), colour, Material: 0));
        }
        public Body Body()
        {
            var body = new Body { Game = 2, Lit = false };
            body.Bones.Add(new Bone(0, Points.Count, 0, -1, new byte[8]));
            body.Vertices.AddRange(Points);
            body.SetWorld(Points.ToArray());
            body.Faces.AddRange(Faces);
            body.Validate();
            return body;
        }
    }

    // A rectangle of a box's side: `o` its corner, `u` along it, `v` up it; seen from `outward` (u x v is made to point that way)
    private static void Side(Mesh m, Vector3 o, Vector3 u, Vector3 v, Vector3 outward, int colour)
    {
        var flip = Vector3.Dot(Vector3.Cross(u, v), outward) < 0;
        if (flip) m.Quad(o, o + v, o + u + v, o + u, colour);
        else m.Quad(o, o + u, o + u + v, o + v, colour);
    }

    // The slab: a block of bricks of LBA1's red-brown, 448 square and as tall as the doorway, its four sides coursed in bricks (staggered,
    // a little proud of the mortar) -- the same from every side, so whichever way the engine turns it, the wall shows it.
    private static Body Slab(Func<int, int, int, int> c)
    {
        var m = new Mesh();
        var mortar = c(70, 56, 48);
        int[] bricks = { c(156, 74, 52), c(170, 86, 60), c(140, 64, 46), c(182, 98, 66), c(150, 80, 58) };
        const float H = SlabHalf, Tall = SlabHeight, Proud = 6, Gap = 10;
        const int Courses = 8;
        var sides = new[]
        {
            (O: new Vector3(H, 0, -H), U: new Vector3(0, 0, 2 * H), N: Vector3.UnitX),
            (O: new Vector3(-H, 0, H), U: new Vector3(0, 0, -2 * H), N: -Vector3.UnitX),
            (O: new Vector3(H, 0, H), U: new Vector3(-2 * H, 0, 0), N: Vector3.UnitZ),
            (O: new Vector3(-H, 0, -H), U: new Vector3(2 * H, 0, 0), N: -Vector3.UnitZ),
        };
        var k = 0;
        foreach (var (o, u, n) in sides)
        {
            Side(m, o, u, new Vector3(0, Tall, 0), n, mortar);
            var along = Vector3.Normalize(u);
            for (var course = 0; course < Courses; course++)
            {
                var y0 = course * Tall / Courses + Gap / 2; var y1 = (course + 1) * Tall / Courses - Gap / 2;
                // (two bricks a course, every other course a half brick at each end)
                var cuts = course % 2 == 0 ? new[] { 0f, 0.5f, 1f } : new[] { 0f, 0.25f, 0.75f, 1f };
                for (var b = 0; b + 1 < cuts.Length; b++)
                {
                    var a0 = cuts[b] * 2 * H + Gap / 2; var a1 = cuts[b + 1] * 2 * H - Gap / 2;
                    var corner = o + along * a0 + new Vector3(0, y0, 0) + n * Proud;
                    Side(m, corner, along * (a1 - a0), new Vector3(0, y1 - y0, 0), n, bricks[k++ % bricks.Length]);
                }
            }
        }
        Side(m, new Vector3(-H, Tall, -H), new Vector3(2 * H, 0, 0), new Vector3(0, 0, 2 * H), Vector3.UnitY, mortar);
        return m.Body();
    }

    // An old sign leaning on a wall behind it (towards -z): a board 600 wide and 520 tall, its foot 150 out from the wall, its top against
    // it; a dark wooden frame round a painted panel, lines of lettering on it. `which`: 0 the clone checkpoint's (red, white lettering),
    // 1 FunFrock's order (dark blue, gold lettering, his emblem's ring), 2 the wanted poster (yellowed paper, a portrait over the lettering).
    private static Body Sign(Func<int, int, int, int> c, int which)
    {
        var m = new Mesh();
        const float W = 300, Tall = 520, Out = 150, Thick = 30;
        // the board's plane: its foot at z = Out, its top at z = 0; up the board, and out of its face
        var up = Vector3.Normalize(new Vector3(0, Tall, -Out));
        var face = Vector3.Normalize(Vector3.Cross(Vector3.UnitX, up));
        if (face.Z < 0) face = -face;
        var length = new Vector3(0, Tall, -Out).Length();
        var foot = new Vector3(-W, 0, Out);
        Vector3 At(float x, float t, float lift = 0) => foot + new Vector3(x, 0, 0) + up * t + face * lift;
        var wood = c(92, 62, 38); var woodDark = c(64, 42, 26);
        // the board's back and edges, then its face: the frame, the panel, its lettering
        Side(m, At(0, 0, -Thick), new Vector3(2 * W, 0, 0), up * length, -face, woodDark);
        Side(m, At(0, length, -Thick), new Vector3(2 * W, 0, 0), face * Thick, Vector3.UnitY, wood);
        Side(m, At(0, 0, -Thick), up * length, face * Thick, -Vector3.UnitX, woodDark);
        Side(m, At(2 * W, 0, -Thick), up * length, face * Thick, Vector3.UnitX, woodDark);
        Side(m, At(0, 0), new Vector3(2 * W, 0, 0), up * length, face, wood);
        var (panel, ink) = which switch
        {
            0 => (c(168, 40, 36), c(232, 226, 210)),
            1 => (c(36, 48, 96), c(212, 172, 64)),
            _ => (c(214, 196, 150), c(60, 44, 34)),
        };
        const float Frame = 40;
        void Rect(float x0, float t0, float x1, float t1, float lift, int colour) =>
            Side(m, At(x0, t0, lift), new Vector3(x1 - x0, 0, 0), up * (t1 - t0), face, colour);
        Rect(Frame, Frame, 2 * W - Frame, length - Frame, 4, panel);
        if (which == 2)
        {
            // the poster: "WANTED" across its top, the portrait under it (a face, a ponytail), the lines of small print at its foot
            Rect(90, length - 120, 2 * W - 90, length - 75, 8, ink);
            Rect(200, length - 330, 400, length - 145, 8, c(120, 104, 86));
            Rect(250, length - 300, 350, length - 190, 12, c(222, 178, 136));
            Rect(350, length - 260, 390, length - 200, 12, c(196, 150, 70));
            for (var line = 0; line < 3; line++) Rect(100, 70 + line * 40, 2 * W - 100 - line * 60, 88 + line * 40, 8, ink);
        }
        else
        {
            // three lines of lettering, a bolder one on top; FunFrock's order with his emblem's ring under them
            Rect(90, length - 140, 2 * W - 90, length - 95, 8, ink);
            for (var line = 0; line < 2; line++) Rect(110, length - 200 - line * 55, 2 * W - 110 - line * 80, length - 175 - line * 55, 8, ink);
            if (which == 1) { Rect(250, 70, 350, 90, 8, ink); Rect(250, 150, 350, 170, 8, ink); Rect(250, 90, 270, 150, 8, ink); Rect(330, 90, 350, 150, 8, ink); }
            else for (var s = 0; s < 4; s++) Rect(70 + s * 120, 60, 120 + s * 120, 120, 8, ink);
        }
        return m.Body();
    }

    // The dusty bottle: dark green glass, eight-sided, its shoulders and the top of its cork grey with dust
    private static Body Bottle(Func<int, int, int, int> c)
    {
        var m = new Mesh();
        const int Sides = 8;
        var glass = new[] { c(30, 70, 40), c(40, 90, 52), c(24, 56, 32) };
        var dust = c(150, 146, 136); var cork = c(140, 100, 60);
        Vector3 Ring(float r, float y, int i) { var a = i * MathF.PI * 2 / Sides; return new Vector3(r * MathF.Cos(a), y, r * MathF.Sin(a)); }
        void Band(float r0, float y0, float r1, float y1, Func<int, int> colour)
        {
            for (var i = 0; i < Sides; i++)
            {
                Vector3 a = Ring(r0, y0, i), b = Ring(r0, y0, i + 1), d = Ring(r1, y1, i), e = Ring(r1, y1, i + 1);
                var mid = (a + b + d + e) / 4; var outward = new Vector3(mid.X, 0, mid.Z);
                if (Vector3.Dot(Vector3.Cross(b - a, e - a), outward) >= 0) m.Quad(a, b, e, d, colour(i));
                else m.Quad(a, d, e, b, colour(i));
            }
        }
        Band(75, 0, 75, 230, i => glass[i % 3]);
        Band(75, 230, 30, 300, i => i % 2 == 0 ? dust : glass[1]);
        Band(30, 300, 30, 380, i => glass[(i + 1) % 3]);
        Band(32, 380, 32, 420, _ => cork);
        // (the cork's top: triangles round its middle -- LBA2's polygons have three or four points)
        var middle = new Vector3(0, 420, 0);
        for (var i = 0; i < Sides; i++)
        {
            Vector3 a = Ring(32, 420, i), b = Ring(32, 420, i + 1);
            m.Poly(Vector3.Cross(a - middle, b - middle).Y >= 0 ? new[] { middle, a, b } : new[] { middle, b, a }, dust);
        }
        return m.Body();
    }
}
