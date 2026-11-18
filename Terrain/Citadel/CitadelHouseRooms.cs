using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Assets;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.LbaScript;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain.Citadel;

// Twinsen's house upgraded (the user, 2026-11-16): "The window to the left of the bed we're going to remove and instead put a doorway there
// into a bathroom area, with toilet, sink, shower, and a large corner bath. The other side where the chair is in the corner, we're going to
// remove this chair, and add another entrance into a kitchen area. Twinsen's house from the first game has both a kitchen and bathroom area so
// we could take inspiration from this, but keep the look and feel of his new house in LBA2. Let's make these changes to the daytime house
// first, then after we're happy with it we can merge them into the night house."
// Round 2 (that afternoon, with a picture of the dining room): "The bathroom entrance needs moving a single square to the left, we should also
// add a door, and fill the door frame with wood the same as the retail one. The kitchen entrance is totally in the wrong place, it should be in
// the far right corner near the dining table" -- the corner chair was the dining room's right-hand armchair; "The entrance door to the bathroom
// looks blocky and not smooth ... along with the wall to the left of the door, it doesn't connect to the wall or the door frame"; "The kitchen
// wall and door way need the same work as the bathroom"; "The towel rail is a nice addition but is hidden behind the door frame".
//   - the house (scene 0) sits in its grid's corner -- its north wall on row 0, its west on column 4 -- so there is no room behind either
//     wall: the bathroom and the kitchen are rooms of their own (scenes Bathroom and Kitchen: past 255, reached by doors only);
//   - every doorway the house's own, the bedroom's west one to the storeroom passage (column 4, z 5-8: jamb 127, two cells open under the
//     lintel 171 over a threshold 81, jamb 172; and its door, sprite 100, standing in it): in each room's west wall as it is; in the house's
//     north wall -- the bedroom's (x 5-8: where the window left of the bed was, the opening a cell left of round 1's) and the dining room's
//     (x 24-27: behind its right-hand armchair, by the far right corner) -- mirrored, x for z: the blocks' bricks drawn flipped into new
//     bricks and blocks of the house's library, the door a flipped copy of sprite 100 (a new sprite, its offsets and box in RESS.HQR's);
//   - each room in the house's own bricks (scene 0's block library, its grid's pieces copied): the house's corner, its north wall, its west
//     wall with the doorway and the passage behind it, its floor tiles, its front edge and its east edge (the low walls the camera sees
//     over -- the front edge whole now, no door in it); furnished with the house's own pieces and the new fittings (CitadelProps.HouseRooms):
//     the bath filled from its taps (Action at them), the kitchen's chairs sat on (Action by one; Action gets him up -- RACEMOD.CPP seat_cube=);
//   - made after the house at night (CitadelNightHouse) is made from scene 0's grid: the night's house keeps its window and its armchair
//     till the day's is liked (the user's wish), and has no door to the rooms.
// Round 3 (the user, 2026-11-16 evening, pictures): "The doors we've added into Twinsen's house have a darker background than the rest of
// the walls that they're in" (WestToNorth); "The bathroom door is on a different wall to where Twinsen logically enters the room, so we
// should move it", and the kitchen's "the same as with the bathroom, so it matches the direction Twinsen enters from the house. Let's move
// the lamp from the corner it's at which is where the door needs to move to, into the corner by where the door currently is"; "This corner
// of the bathroom still looks wrong where the two walls meet"; "The door on the right side to the kitchen let's move it a square left";
// "The shelf and lamp that we've added on the wall stop Twinsen from walking even though they're not visibly blocking him":
//   - each room's way in and out a doorway in its front (south) wall -- Twinsen walks north through the house's north wall into it, and
//     south out of it -- two cells open in the low front wall (BathGapX, KitchenGapX: the bathroom's west of the middle, under the bedroom's
//     door; the kitchen's in its east corner, where the floor lamp stood -- the lamp to the west corner); the west wall whole, no door;
//   - the west wall comes down to the front as the house's own partition between the bedroom and the dining room does (column 15,
//     rows 2-5: its curve, a cell of low wall, the low wall's end post at the corner) -- the tall corner stood over a low wall, the
//     corner the user's picture showed; the front from that corner to the doorway the house's storeroom front as high as the game's low
//     walls, past the doorway as low as it was (the east edge too: the bath shows over it) -- the step between the heights the doorway's.
//     (The storeroom's own corner -- a tall post, a tall cell, its curve -- tried first: its tall front hid the kitchen's lamp, and the
//     east edge as high hid the bath);
//   - the kitchen's doorway in the dining room a cell left (x 23-26);
//   - the kitchen's wall lamp gone (2026-11-17: it stopped Twinsen even as a copy with no shape, GhostBlock).
internal static class CitadelHouseRooms
{
    public const int House = 0, Bathroom = 396, Kitchen = 397;
    private const int FloorLayer = 7, Size = 64, Height = 25;
    public const int FloorY = (FloorLayer + 1) * 256;
    // a room's floor: x RoomX0-RoomX1, z RoomZ0-RoomZ1 (cells) -- the house's own corner, walls at column 4 and row 0
    private const int RoomX0 = 5, RoomX1 = 14, RoomZ0 = 1, RoomZ1 = 8;
    // where a room's west wall starts down to its front (the house's partition's curve, two cells, then a cell of low wall)
    private const int WestCurveZ = 6;
    // the house's own doorway (the bedroom's west wall, to the storeroom passage): its four cells along z from DoorZ0 on column 4, the
    // passage's cells behind it on column 3 (and the row after, its side), the door's sprite and where it stands, the passage's zone
    private const int DoorZ0 = 5, DoorSprite = 100;
    private static readonly int[] DoorBlocks = { 127, 171, 172, 81 };
    private const int DoorSpriteX = 2544, DoorSpriteUp = 96, DoorSpriteAlong = 78, DoorSpriteIn = 16;
    // the new doorways in the house's north wall: their four cells from these columns (a jamb, the two open, a jamb) -- the bedroom's, the
    // dining room's; the window's last column after the bedroom's, a wall panel now
    private const int BathDoorX0 = 5, KitchenDoorX0 = 23;
    // each room's doorway in its front wall: its two open cells from these columns; where it brings him in (z, the doorway's inside, walking
    // north) -- the house's north doorway's zone adds how deep he is into it (0-DoorDeep), so its Info's z is this less DoorDeep
    private const int BathGapX = 8, KitchenGapX = 12, RoomArriveZ = 4400;
    // the house's wall lamp (block 90: the bedroom's, column 5 row 2, layers 13-14), the kitchen's copy of it with no shape
    private const int WallLamp = 90;
    // the shower's door: Twinsen this near its hinge, it swings open; this far again, back to ajar
    private const int ShowerSwingNear = 650, ShowerSwingFar = 900;
    // the dining room's right-hand armchair (block 56 over a floor of 81, cells 25-26 by 1-2)
    private static readonly (int X, int Z)[] Armchair = { (25, 1), (26, 1), (25, 2), (26, 2) };
    // a north doorway's zone: its opening's cells and a step into the room, 48 past the wall's face -- the story build's walk-through of
    // round 1 stopped in an opening short of the outer half of its cell
    private const int DoorDeep = 560;
    // where a room's doorway brings him into the house: before its north doorway (the room's zone adds how far he is across its doorway to
    // x, and how deep into its zone to this z -- the engine's GereZoneChangeCube, no turn)
    private const int IntoHouseZ = 700;
    // the scene variables: a seat taken (RaceTrackStory.SeatCube), which chair, the bath's filling count
    private const int WhichChair = 78, Filling = 77;
    private const int ChairZone0 = 80, TapsZone = 82, SitLabel = 100, StandLabel = 120;
    // a door's zone, before it on the room's side: Twinsen in it, it slides down open (the house's door's own way, open_down -- its
    // script without the little key the bedroom's west door wants); away again, it closes. The north doors' zones in scene 0: DoorZone and
    // the one after it
    internal const int DoorZone = 90;
    private const int DoorOpenDown = 1024, DoorSound = 349, DoorFront = 1200;
    // (the night's house keeps these doors and their zones -- CitadelNightHouse: a north doorway's door, the sprite where Install puts it)
    internal static bool IsNorthDoor(SceneActorModel a) => a.IsSprite && a.Entity == -1 && a.Z == 512 - DoorSpriteIn && a.Y == FloorY + DoorSpriteUp;
    internal static bool IsRoomZone(SceneZoneModel z) => (z.Type == 2 && (z.Num == DoorZone || z.Num == DoorZone + 1)) || (z.Type == 0 && (z.Num == Bathroom || z.Num == Kitchen));
    // (`way`: the way it slides open -- the west doors along +z, "down" (the house's own); the north doors, mirrored, along +x, "right":
    // down, one slid 1,024 out into the bedroom and stood there unseen, clipped, in Twinsen's way)
    private static (string Life, string Track) DoorScripts(int zone, string way) => ($@"void comportement_0()
{{
    if ({zone} == zone_obj(0))
    {{
        set_door_{way}({DoorOpenDown});
        set_track(label_100);
        set_comportement(comportement_1);
    }}
    else
    {{
        set_track(label_101);
        set_comportement(comportement_2);
    }}
}}

void comportement_1()
{{
    if ({zone} != zone_obj(0) && {DoorFront} < distance(0))
    {{
        set_track(label_1);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if ({zone} == zone_obj(0))
    {{
        set_track(label_0);
        set_comportement(comportement_1);
    }}
}}
", $@"label(0);
sample({DoorSound});
open_{way}({DoorOpenDown});
wait_door();

label(100);
stop();

label(1);
sample({DoorSound});
close();
wait_door();

label(101);
stop();
");
    // A door's clip (SPRITE_CLIP: what of it is drawn, in the screen's units from where the grid's corner is drawn): its opening's picture,
    // the floor to the lintel (6 layers) -- the bedroom's west door's own, (-71, -75, -30, 32), is its opening's less 1 and 6 at its sides,
    // 3 at its top, 4 at its foot; a north door's mirrored
    private static int[] NorthDoorClip(int openingX, int faceZ)
    {
        int Sx(int x, int z) => (x - z) * 24 / 512;
        int Sy(int x, int y, int z) => (x + z) * 12 / 512 - y * 15 / 256;
        int x0 = Sx(openingX, faceZ), x1 = Sx(openingX + 1024, faceZ);
        int top = Math.Min(Sy(openingX, FloorY + 1536, faceZ), Sy(openingX + 1024, FloorY + 1536, faceZ));
        int foot = Math.Max(Sy(openingX, FloorY, faceZ), Sy(openingX + 1024, FloorY, faceZ));
        return new[] { x0 + 6, top + 3, x1 - 1, foot - 4 };
    }
    // RESS.HQR's sprites' table (8 numbers each: the picture's offset x, y, the box's x0, x1, y0, y1, z0, z1)
    private const int RessSpriteZv = 5;

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var spritesPath = Path.Combine(gameDirectory, "SPRITES.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(House) || !File.Exists(spritesPath) || !File.Exists(ressPath)) { log.Add("The house's bathroom and kitchen left out: no scene 0 (or no SPRITES.HQR, RESS.HQR)"); return log; }
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        foreach (var s in new[] { Bathroom, Kitchen })
            if (s + 1 < scenes.Count && !scenes.IsEmpty(s + 1)) { log.Add($"The house's bathroom and kitchen left out: scene {s} is already another part of the build's"); return log; }
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        var type = table[House * 2];
        var houseEntry = header.Gri + table[House * 2 + 1];
        var raw = bkg.Read(houseEntry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var house = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * Size + x) * Height + y) * 2;
        int Mod(int a, int m) => ((a % m) + m) % m;
        // (the house as the game has it: its window where it was, the armchair -- a second pass leaves it)
        if (house[At(6, 10, 0)] != 89 || house[At(25, 9, 1)] != 56 || house[At(4, 9, DoorZ0)] != DoorBlocks[0]) { log.Add("The house's bathroom and kitchen left out: the bedroom's window, the dining room's armchair or the bedroom's doorway isn't where it was (made already?)"); return log; }
        List<(int Y, int Block, int Pos)> Column(int x, int z) => Shifted(x, z, 0);
        // (a column moved up or down, the house's outermost rock -- block 173, an outer face over the floor's height -- left out)
        List<(int Y, int Block, int Pos)> Shifted(int x, int z, int up)
        {
            var list = new List<(int, int, int)>();
            for (var y = 0; y < Height; y++)
            {
                var at = At(x, y, z);
                if ((house[at] != 0 || house[at + 1] != 0) && house[at] != 173 && y + up >= 0 && y + up < Height) list.Add((y + up, house[at], house[at + 1]));
            }
            return list;
        }

        // ---- the doorway mirrored: its blocks' bricks flipped, new blocks of the house's library (before any grid goes in: that moves it)
        var libraryEntry = header.Bll + raw[0];
        var library = bkg.Read(libraryEntry);
        byte[]? Brick(int b) => b >= 0 && b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? bkg.Read(header.Brk + b) : null;
        var mirrored = new Dictionary<int, int>();
        // (their colours the north wall's light: WestToNorth)
        var shade = WestToNorth(library, Brick, log);
        foreach (var block in DoorBlocks)
        {
            if (GridPaint.Info(library, block) is null) { log.Add($"The house's bathroom and kitchen left out: the house's library has no block {block}"); return log; }
            mirrored[block] = MirrorBlock(bkg, header, ref library, block, Brick, shade);
        }
        bkg.SetStored(libraryEntry, library);
        int MirrorPos(int block, int pos)
        {
            var info = GridPaint.Info(library, block)!.Value;
            int i = pos % info.Dx, j = pos / info.Dx % info.Dy, k = pos / (info.Dx * info.Dy);
            return k + info.Dz * (j + info.Dy * i);
        }
        // the doorway's column along z (its k-th) as a north wall's, its layers FloorLayer..14: x for z, its blocks the mirrored ones
        List<(int Y, int Block, int Pos)> DoorColumn(int k) =>
            Column(4, DoorZ0 + k).Where(c => c.Y >= FloorLayer && c.Y <= 14).Select(c => mirrored.TryGetValue(c.Block, out var m) ? (c.Y, m, MirrorPos(c.Block, c.Pos)) : c).ToList();

        // ---- the door mirrored: sprite 100 flipped, a new sprite after SPRITES.HQR's last, its offsets and box in RESS.HQR's table
        var sprites = HqrFile.Parse(File.ReadAllBytes(spritesPath));
        var ress = HqrFile.Parse(File.ReadAllBytes(ressPath));
        var doorEntry = sprites.Read(DoorSprite);
        var pictureAt = (int)BinaryPrimitives.ReadUInt32LittleEndian(doorEntry);
        var door = GphImage.Decode(doorEntry, pictureAt);
        var picture = CitadelMuseum.EncodeBrick(Flip(door, door.OffsetX));
        var flippedEntry = new byte[8 + picture.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(flippedEntry, 8);
        BinaryPrimitives.WriteUInt32LittleEndian(flippedEntry.AsSpan(4), (uint)picture.Length);
        picture.CopyTo(flippedEntry, 8);
        var mirrorSprite = -1;
        for (var n = sprites.Count - 1; n >= Math.Max(0, sprites.Count - 8) && mirrorSprite < 0; n--)
            if (!sprites.IsEmpty(n) && sprites.Read(n).AsSpan().SequenceEqual(flippedEntry)) mirrorSprite = n;
        if (mirrorSprite < 0)
        {
            mirrorSprite = sprites.Count;
            sprites.Slots.Add(new HqrFile.Slot());
            sprites.SetStored(mirrorSprite, flippedEntry);
        }
        var zv = ress.Read(RessSpriteZv);
        if (zv.Length < (DoorSprite + 1) * 16) { log.Add("The house's bathroom and kitchen left out: RESS.HQR's sprites' table has no door"); return log; }
        if (zv.Length < (mirrorSprite + 1) * 16) Array.Resize(ref zv, (mirrorSprite + 1) * 16);
        short Zv(int n, int i) => BinaryPrimitives.ReadInt16LittleEndian(zv.AsSpan((n * 8 + i) * 2));
        var flippedZv = new[] { (short)(-Zv(DoorSprite, 0) - door.Width + 1), Zv(DoorSprite, 1), Zv(DoorSprite, 6), Zv(DoorSprite, 7), Zv(DoorSprite, 4), Zv(DoorSprite, 5), Zv(DoorSprite, 2), Zv(DoorSprite, 3) };
        for (var i = 0; i < 8; i++) BinaryPrimitives.WriteInt16LittleEndian(zv.AsSpan((mirrorSprite * 8 + i) * 2), flippedZv[i]);
        ress.SetStored(RessSpriteZv, zv);

        // ---- the house's doorways and the armchair
        var houseEdits = new List<Lba1GridCell>();
        void SetColumn(List<Lba1GridCell> edits, int x, int z, List<(int Y, int Block, int Pos)> column)
        {
            for (var y = 0; y < Height; y++)
            {
                var c = column.FirstOrDefault(k => k.Y == y);
                edits.Add(column.Any(k => k.Y == y) ? new Lba1GridCell(x, y, z, c.Block, c.Pos) : new Lba1GridCell(x, y, z, 0, 0));
            }
        }
        // (the wall's own over the doorway kept -- from layer 15: a pilaster's top over a jamb where it was, the panels' top row elsewhere)
        List<(int Y, int Block, int Pos)> Panel(int x, int from) => Enumerable.Range(from, 16 - from).Select(y => (y, 2, Mod(x - 10, 3) + 3 * (y - 8))).ToList();
        List<(int Y, int Block, int Pos)> Above(int x) => Column(x, 0).Where(c => c.Y >= 15 && c.Block != 89).ToList();
        for (var k = 0; k < 4; k++)
        {
            var x = BathDoorX0 + k;
            var top = Above(x);
            if (!top.Any(c => c.Y == 15)) top.AddRange(Panel(x, 15));
            SetColumn(houseEdits, x, 0, DoorColumn(k).Concat(top).ToList());
            SetColumn(houseEdits, KitchenDoorX0 + k, 0, DoorColumn(k).Concat(Above(KitchenDoorX0 + k)).ToList());
        }
        // (the window's last column a panel, its floor's collision as the wall's)
        var afterWindow = BathDoorX0 + 4;
        SetColumn(houseEdits, afterWindow, 0, new List<(int, int, int)> { (FloorLayer, 0, 1) }.Concat(Panel(afterWindow, 8)).Concat(Above(afterWindow)).ToList());
        foreach (var (x, z) in Armchair) SetColumn(houseEdits, x, z, new() { (FloorLayer, 31, Mod(x, 3) + 3 * Mod(z, 3)) });
        var houseGrid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, houseEdits));
        bkg.SetStored(houseEntry, houseGrid);
        if (header.MaxGri < (uint)houseGrid.Length) header.MaxGri = (uint)houseGrid.Length;

        // ---- a room: the house's corner, walls (the west one with the house's doorway and its passage), floor, edges; its furniture's columns
        byte[] Room(Dictionary<(int X, int Z), List<(int Y, int Block, int Pos)>> furniture, int gap)
        {
            var columns = new Dictionary<(int X, int Z), List<(int Y, int Block, int Pos)>>();
            columns[(4, 0)] = Column(4, 0);
            for (var x = RoomX0; x <= RoomX1; x++)
                columns[(x, 0)] = x == RoomX0 ? Column(5, 0) : x == RoomX1 ? Column(13, 0) : Column(10 + Mod(x - 6, 3), 0);
            // (the west wall tall to WestCurveZ, then down as the house's own partition between the bedroom and the dining room comes down
            // (column 15, rows 2-5): its curve over two cells, a cell of low wall, the low wall's end post at the front corner)
            for (var z = RoomZ0; z <= RoomZ1; z++)
                columns[(4, z)] = z == RoomZ0 ? Column(4, 1) : z < WestCurveZ ? Column(4, 2 + Mod(z - 2, 3)) : Column(15, 2 + z - WestCurveZ);
            for (var z = RoomZ0; z <= RoomZ1; z++)
                for (var x = RoomX0; x <= RoomX1; x++)
                    columns[(x, z)] = new() { (FloorLayer, 31, Mod(x, 3) + 3 * Mod(z, 3)) };
            // (the front edge, row RoomZ1 + 1: its west corner the partition's end post (15, 5); from it to the doorway the house's storeroom
            // front as the game has it (8-9, 20), as high as the house's own low walls -- the curve's foot, the end post's top (layer 11) --
            // rock under it to the floor's foot; the doorway, two cells of floor (`gap`); past it the storeroom front two layers down, low
            // (it was all so: the user's picture, the tall corner over it), as the east edge is -- column RoomX1 + 1, the house's plain east
            // edge three layers up -- so the bath and the front's things show over them: the step between the two heights the doorway's;
            // the east's north end the house's; the rows and columns past them, walls one doesn't see)
            var unseen = Enumerable.Range(FloorLayer, 6).Select(y => (y, 0, 1)).ToList();
            var front = RoomZ1 + 1;
            // (both sides of the doorway the same -- the low front, the right's: the left was the storeroom front as high as the house's low
            // walls with a rock foot under it, two layers over the right and another stone -- the user, 2026-11-17, "the wall on the left side
            // of the door is higher than the right side. We need to make sure the wall colour on the outside is consistent also"; the corner's
            // end post down with it)
            columns[(4, front)] = Shifted(15, 5, -2);
            for (var x = RoomX0; x <= RoomX1; x++)
                columns[(x, front)] = x == gap || x == gap + 1
                    ? new() { (FloorLayer, 31, Mod(x, 3) + 3 * Mod(front, 3)) }   // (no rock under: the rock's lower piece alone drew broken, white
                                                                  // through it -- its upper half is the next block's)
                    : Shifted(8 + Mod(x, 2), 20, -2);
            for (var z = RoomZ0; z <= RoomZ1; z++) columns[(RoomX1 + 1, z)] = Shifted(28, 16 + Mod(z, 4), 3);
            columns[(RoomX1 + 1, 0)] = Column(28, 0);
            columns[(RoomX1 + 1, front)] = Shifted(28, 20, 3);
            for (var x = 4; x <= RoomX1 + 2; x++) columns[(x, RoomZ1 + 2)] = unseen;
            for (var z = 0; z <= RoomZ1 + 1; z++) columns[(RoomX1 + 2, z)] = unseen;
            foreach (var (c, col) in furniture) columns[c] = col;
            var edits = new List<Lba1GridCell>();
            for (var z = 0; z < Size; z++)
                for (var x = 0; x < Size; x++)
                    SetColumn(edits, x, z, columns.TryGetValue((x, z), out var col) ? col : new());
            var grid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, edits));
            grid[0] = raw[0];
            return grid;
        }
        // (a piece of the house's furniture where it stands in the house, its floor under it as the room's)
        List<(int, int, int)> Piece(int x, int z, int roomX, int roomZ) =>
            Column(x, z).Where(c => c.Y > FloorLayer).Append((FloorLayer, 31, Mod(roomX, 3) + 3 * Mod(roomZ, 3))).ToList();
        // (the corner bath's two ends along the walls, past its box -- a box is square to the walls, its front a quarter round: solid cells,
        // the bath's rim high, CitadelProps.BathBox)
        List<(int, int, int)> Solid(int x, int z) => new() { (FloorLayer, 31, Mod(x, 3) + 3 * Mod(z, 3)), (FloorLayer + 1, 0, 1), (FloorLayer + 2, 0, 1) };
        var bathFurniture = new Dictionary<(int X, int Z), List<(int Y, int Block, int Pos)>>
        {
            [(14, 8)] = Piece(5, 5, 14, 8),   // a floor lamp in the front corner
            [(12, 1)] = Solid(12, 1),         // the bath's west end
            [(14, 3)] = Solid(14, 3),         // the bath's south end
        };
        var kitchenFurniture = new Dictionary<(int X, int Z), List<(int Y, int Block, int Pos)>>
        {
            // (no wall lamp -- the user, 2026-11-17: "The wall light in the kitchen seems to be sticking out quite far and stopping Twinsen
            // from walking, let's just remove it": its cell plain floor)
            [(5, 8)] = Piece(5, 5, 5, 8),     // the floor lamp in the front's west corner (the doorway in the east one)
        };
        var grids = new[] { (Bathroom, Room(bathFurniture, BathGapX)), (Kitchen, Room(kitchenFurniture, KitchenGapX)) };
        foreach (var (scene, grid) in grids)
        {
            var gridNumber = header.Grm - header.Gri;
            if (gridNumber > 255) { log.Add("The house's bathroom and kitchen left out: LBA_BKG.HQR has 256 grids already"); return log; }
            bkg.InsertAt(header.Grm, grid);
            header.Grm += 1; header.Bll += 1; header.Brk += 1;
            if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
            table = bkg.Read(header.Brk + header.MaxBrk);
            if (table.Length < (scene + 1) * 2) Array.Resize(ref table, (scene + 1) * 2);
            table[scene * 2] = type; table[scene * 2 + 1] = (byte)gridNumber;
            bkg.SetStored(header.Brk + header.MaxBrk, table);
        }
        bkg.SetStored(0, header.Bytes);

        // ---- the scenes: scene 0's header (its light, its music), the door back to the house, the fittings
        var houseModel = store.Load(House);
        var doorActor = houseModel.Actors.FirstOrDefault(a => a.IsSprite && a.Sprite == DoorSprite);
        if (doorActor is null) { log.Add("The house's bathroom and kitchen left out: scene 0 has no door (sprite 100)"); return log; }
        var intoHouse = new Dictionary<int, int> { [Bathroom] = (BathDoorX0 + 1) * 512, [Kitchen] = (KitchenDoorX0 + 1) * 512 };
        var gapOf = new Dictionary<int, int> { [Bathroom] = BathGapX, [Kitchen] = KitchenGapX };
        foreach (var scene in new[] { Bathroom, Kitchen })
        {
            var model = houseModel.Clone();
            model.Actors.RemoveRange(1, model.Actors.Count - 1);
            model.Zones.Clear();
            model.TrackPoints.Clear();
            var texts = new Dictionary<int, (string Life, string Track)>();
            var hero = model.Hero;
            (hero.X, hero.Y, hero.Z) = ((gapOf[scene] + 1) * 512, FloorY, RoomArriveZ);
            hero.Beta = 2048;
            hero.Life = new byte[] { 0 }; hero.Track = new byte[] { 0 };
            // (actor 1 is the engine's own, cleared at each load: an unused helper there)
            texts[SceneOps.AddActor(model, RaceTrackScenes.InvisibleHelper(0, FloorY, 0))] = ("void comportement_0()\n{\n}\n", "label(0);\nstop();\n");
            // the way back: the front wall's doorway, walked south out of -- into the house before its north doorway, as far across it,
            // walking on south (no turn: Info3 0)
            var gapX = gapOf[scene] * 512;
            model.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = House, X0 = gapX, X1 = gapX + 1023, Z0 = (RoomZ1 + 1) * 512 + 64, Z1 = (RoomZ1 + 2) * 512 - 1, Y0 = FloorY - 100, Y1 = FloorY + 1500,
                Info = new[] { intoHouse[scene], FloorY, IntoHouseZ, 0, 0, 0, 0, 1 },
            });
            var bath = -1;
            foreach (var (generic, x, z, beta) in scene == Bathroom ? CitadelProps.BathroomFittings : CitadelProps.KitchenFittings)
            {
                var a = CitadelProps.Actor(generic > 127 ? (sbyte)generic : generic, x, FloorY, z, beta);
                a.Life = new byte[] { 0 }; a.Track = new byte[] { 0 };
                // (the shower's door turns on its hinge: its track's angle(), at its turning speed -- the engine turns any actor not the
                // player's to its track's angle, OBJECT.CPP DoDir)
                if (generic == CitadelProps.ShowerDoor) { a.Move = 3; a.SRot = CitadelProps.ShowerDoorTurn; }
                var index = SceneOps.AddActor(model, a);
                if (generic == CitadelProps.CornerBath) bath = index;
                texts[index] = generic == CitadelProps.ShowerDoor
                    // (Twinsen comes up to it: it swings open; he goes: back to ajar -- the user, 2026-11-16, "a hinged door to the shower
                    // that's slightly ajar ... a door that swings when Twinsen connects to it")
                    ? ($@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if ({ShowerSwingNear} > distance(0))
    {{
        set_track(label_1);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if ({ShowerSwingFar} < distance(0))
    {{
        set_track(label_0);
        set_comportement(comportement_1);
    }}
}}
", $"label(0);\nangle({CitadelProps.ShowerDoorAjar});\nstop();\n\nlabel(1);\nangle({CitadelProps.ShowerDoorOpen});\nstop();\n")
                    : ("void comportement_0()\n{\n}\n", "label(0);\nstop();\n");
            }
            var heroTrack = "label(0);\nstop();\n";
            if (scene == Bathroom && bath >= 0)
            {
                // the bath's taps: Action before them, it fills (its bodies with the water higher, a stage a second); again, it empties
                var bathAt = CitadelProps.BathroomFittings.First(f => f.Generic == CitadelProps.CornerBath);
                int tx = bathAt.X + (int)CitadelProps.BathTaps.X, tz = bathAt.Z + (int)CitadelProps.BathTaps.Z;
                model.Zones.Add(new SceneZoneModel { Type = 2, Num = TapsZone, X0 = tx - 920, X1 = tx + 130, Z0 = 512, Z1 = tz + 1080, Y0 = FloorY - 100, Y1 = FloorY + 1200, Info = new int[8] });
                texts[SceneOps.AddActor(model, RaceTrackScenes.InvisibleHelper(tx, FloorY, tz))] = ($@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if ({TapsZone} == zone_obj(0) && 12 != comportement_hero())
        {{
            set_var_cube({Filling}, 0);
            set_comportement(comportement_2);
        }}
    }}
}}

void comportement_2()
{{
    add_var_cube({Filling}, 1);
    if (50 == var_cube({Filling}))
    {{
        body_obj({bath}, {CitadelProps.BathWaterLow});
    }}
    if (100 == var_cube({Filling}))
    {{
        body_obj({bath}, {CitadelProps.BathWaterMid});
    }}
    if (150 == var_cube({Filling}))
    {{
        body_obj({bath}, {CitadelProps.BathWaterFull});
        set_comportement(comportement_3);
    }}
}}

void comportement_3()
{{
    swif (1 == action())
    {{
        if ({TapsZone} == zone_obj(0) && 12 != comportement_hero())
        {{
            set_var_cube({Filling}, 0);
            set_comportement(comportement_4);
        }}
    }}
}}

void comportement_4()
{{
    add_var_cube({Filling}, 1);
    if (40 == var_cube({Filling}))
    {{
        body_obj({bath}, {CitadelProps.BathWaterMid});
    }}
    if (80 == var_cube({Filling}))
    {{
        body_obj({bath}, {CitadelProps.BathWaterLow});
    }}
    if (120 == var_cube({Filling}))
    {{
        body_obj({bath}, {CitadelProps.CornerBath});
        set_comportement(comportement_1);
    }}
}}
", "label(0);\nstop();\n");
            }
            if (scene == Kitchen)
            {
                // the chairs: Action by one (its zone: the chair and beside it) sits him on it, facing the table (his sitting, the Tralu's
                // bench's -- in Normal, the only behaviour that has it); Action gets him up (the engine's seat_cube=), beside it
                var sit = new System.Text.StringBuilder();
                var stand = new System.Text.StringBuilder();
                for (var k = 0; k < CitadelProps.KitchenChairs.Length; k++)
                {
                    var ch = CitadelProps.KitchenChairs[k];
                    model.Zones.Add(new SceneZoneModel { Type = 2, Num = ChairZone0 + k, X0 = ch.SeatX - 500, X1 = ch.SeatX + 500, Z0 = ch.Z - 450, Z1 = ch.Z + 900, Y0 = FloorY - 100, Y1 = FloorY + 1200, Info = new int[8] });
                    var seat = SceneOps.AddTrackPoint(model, new SceneTrackPoint(ch.SeatX, FloorY + CitadelProps.KitchenSeatTop, ch.Z));
                    var front = SceneOps.AddTrackPoint(model, new SceneTrackPoint(ch.SeatX, FloorY, ch.Z + 650));
                    heroTrack += $"\nlabel({SitLabel + k});\npos_point({seat});\nbeta({ch.Beta});\nanim({CitadelSpanking.TwinsenSit});\nstop();\n\nlabel({StandLabel + k});\npos_point({front});\nbeta({ch.Beta});\nanim(0);\nstop();\n";
                    sit.Append($@"            if ({ChairZone0 + k} == zone_obj(0))
            {{
                comportement_hero(0);
                set_var_cube({RaceTrackStory.SeatCube}, 1);
                set_var_cube({WhichChair}, {k});
                set_dir_obj(0, NO_MOVE);
                set_track_obj(0, label_{SitLabel + k});
                set_comportement(comportement_2);
            }}
");
                    stand.Append($@"        if ({k} == var_cube({WhichChair}))
        {{
            set_track_obj(0, label_{StandLabel + k});
        }}
");
                }
                texts[SceneOps.AddActor(model, RaceTrackScenes.InvisibleHelper(0, FloorY, 0))] = ($@"void comportement_0()
{{
    set_var_cube({RaceTrackStory.SeatCube}, 0);
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if (12 != comportement_hero())
        {{
{sit}        }}
    }}
}}

void comportement_2()
{{
    if (2 == var_cube({RaceTrackStory.SeatCube}))
    {{
{stand}        set_dir_obj(0, MOVE_MANUAL);
        set_var_cube({RaceTrackStory.SeatCube}, 0);
        set_comportement(comportement_1);
    }}
}}
", "label(0);\nstop();\n");
            }
            var built = SceneScripts.Load(SceneSerializer.Write(model), scene);
            built.SetText(0, ScriptKind.Life, "void comportement_0()\n{\n}\n");
            built.SetText(0, ScriptKind.Track, heroTrack);
            foreach (var (actor, (life, track)) in texts) { built.SetText(actor, ScriptKind.Life, life); built.SetText(actor, ScriptKind.Track, track); }
            var result = built.Build();
            if (!result.Ok) { log.AddRange(result.Errors.Select(e => $"The house's room {scene} left out: {e}")); return log; }
            var entry = scene + 1;
            while (scenes.Count <= entry) scenes.Slots.Add(new HqrFile.Slot());
            scenes.SetStored(entry, result.Record!);
            if (result.Record!.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
            { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, result.Record.Length); scenes.SetStored(0, size); }
        }
        // ---- the house's doors to them: walked north into either doorway, through its door (the mirrored sprite, where sprite 100 stands
        // in the bedroom's west doorway, x for z: at the wall's face, DoorSpriteAlong into the opening)
        var houseScripts = new List<(int Actor, (string Life, string Track) Scripts)>();
        foreach (var (scene, x0, zone) in new[] { (Bathroom, BathDoorX0, DoorZone), (Kitchen, KitchenDoorX0, DoorZone + 1) })
        {
            var opening = (x0 + 1) * 512;
            houseModel.Zones.Add(new SceneZoneModel { Type = 2, Num = zone, X0 = opening, X1 = opening + 1023, Z0 = 512, Z1 = 512 + DoorFront, Y0 = FloorY - 100, Y1 = FloorY + 1500, Info = new int[8] });
            houseModel.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = scene, X0 = opening, X1 = opening + 1023, Z0 = 0, Z1 = DoorDeep, Y0 = FloorY - 100, Y1 = FloorY + 1500,
                Info = new[] { gapOf[scene] * 512, FloorY, RoomArriveZ - DoorDeep, 0, 0, 0, 0, 1 },
            });
            var north = doorActor.Clone();
            (north.X, north.Y, north.Z) = (opening + DoorSpriteAlong, FloorY + DoorSpriteUp, 512 - DoorSpriteIn);
            north.Sprite = mirrorSprite;
            north.Info = NorthDoorClip(opening, 512);
            houseScripts.Add((SceneOps.AddActor(houseModel, north), DoorScripts(zone, "right")));
        }
        var houseScenes = SceneScripts.Load(SceneSerializer.Write(houseModel), House);
        foreach (var (actor, (life, track)) in houseScripts) { houseScenes.SetText(actor, ScriptKind.Life, life); houseScenes.SetText(actor, ScriptKind.Track, track); }
        var houseBuilt = houseScenes.Build();
        if (!houseBuilt.Ok) { log.AddRange(houseBuilt.Errors.Select(e => $"The house's doors left out: {e}")); return log; }
        var houseRecord = houseBuilt.Record!;
        scenes.SetStored(House + 1, houseRecord);
        if (houseRecord.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
        { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, houseRecord.Length); scenes.SetStored(0, size); }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Write(spritesPath, sprites.ToBytes()).Write(ressPath, ress.ToBytes()).Commit();
        log.Add($"Twinsen's house: the bedroom's window left of the bed a doorway into the bathroom (scene {Bathroom}, its cells x {BathDoorX0}-{BathDoorX0 + 3}), the dining room's right-hand armchair gone for a doorway into the kitchen (scene {Kitchen}, x {KitchenDoorX0}-{KitchenDoorX0 + 3}): the house's own doorway and door (sprite {DoorSprite}) mirrored -- blocks {string.Join(", ", mirrored.Select(m => $"{m.Key} as {m.Value}"))}, sprite {mirrorSprite}; each room in the house's own bricks, its doorway in its front wall (x {BathGapX}-{BathGapX + 1}, {KitchenGapX}-{KitchenGapX + 1}), furnished ({CitadelProps.BathroomFittings.Length} and {CitadelProps.KitchenFittings.Length} fittings: the bath filled at its taps, the kitchen's chairs sat on)");
        return log;
    }

    // A picture flipped left for right, `offsetX` -- its draw offset -- with it: a brick's offset over its cell (48 wide) mirrored about the
    // cell's middle; a sprite's own offset, kept (its draw offset is RESS.HQR's)
    private static GphImage Flip(GphImage image, int offsetX)
    {
        var flipped = new GphImage(image.Width, image.Height) { OffsetX = (sbyte)offsetX, OffsetY = image.OffsetY };
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++)
            {
                flipped.Pixels[y * image.Width + image.Width - 1 - x] = image.Pixels[y * image.Width + x];
                flipped.Opaque[y * image.Width + image.Width - 1 - x] = image.Opaque[y * image.Width + x];
            }
        return flipped;
    }

    // The house's west wall's colours as its north wall's (the user, 2026-11-16, a picture of the bathroom's door: "The doors we've added into
    // Twinsen's house have a darker background than the rest of the walls that they're in"). A brick has its light drawn in: the doorway's
    // are the west wall's, darker than the north wall's, and mirrored they stay so. Learnt from the house's own wallpaper -- block 1 (the
    // west wall's, along z) against block 2 (the north wall's, along x): their patterns don't line up pixel for pixel (a quarter agreed), so
    // by how much of each colour they use -- in each of the palette's ramps of 16 (a colour's shades) that both use enough of
    // (RampLeast pixels), the west wall's shades matched to the north wall's in order, as far through the ramp's pixels (a quantile map);
    // only the wallpaper's own two ramps, its teal (176-191) and its pattern's (192-207) -- the browns (208-223, the rails) differ between
    // the two walls' drawings and would be moved oddly (213 to 216) -- any other colour kept as it is.
    private const int WestWallpaper = 1, NorthWallpaper = 2, RampLeast = 200;
    private static readonly int[] WallpaperRamps = { 11, 12 };
    public static byte[] WestToNorth(byte[] library, Func<int, byte[]?> brick, List<string>? report = null)
    {
        var map = new byte[256];
        for (var c = 0; c < 256; c++) map[c] = (byte)c;
        int[] Count(int block)
        {
            var count = new int[256];
            foreach (var e in GridPaint.Entries(library, block))
                if (brick(e.Brick) is { Length: >= 4 } d && GphImage.Decode(d) is var im)
                    for (var p = 0; p < im.Pixels.Length; p++) if (im.Opaque[p]) count[im.Pixels[p]]++;
            return count;
        }
        if (GridPaint.Info(library, WestWallpaper) is null || GridPaint.Info(library, NorthWallpaper) is null) return map;
        int[] west = Count(WestWallpaper), north = Count(NorthWallpaper);
        foreach (var ramp in WallpaperRamps)
        {
            int c0 = ramp * 16, westAll = 0, northAll = 0;
            for (var c = c0; c < c0 + 16; c++) { westAll += west[c]; northAll += north[c]; }
            if (westAll < RampLeast || northAll < RampLeast) continue;
            var before = 0;
            var moved = new List<string>();
            for (var c = c0; c < c0 + 16; c++)
            {
                if (west[c] == 0) continue;
                var at = (before + west[c] / 2.0) / westAll;
                before += west[c];
                int d = c0, upTo = 0;
                for (; d < c0 + 15; d++) { upTo += north[d]; if ((double)upTo / northAll >= at) break; }
                map[c] = (byte)d;
                if (d != c) moved.Add($"{c}->{d}");
            }
            report?.Add($"ramp {c0}-{c0 + 15} ({westAll} west, {northAll} north pixels): {(moved.Count == 0 ? "as it is" : string.Join(" ", moved))}");
        }
        return map;
    }

    // A picture's colours through a map (WestToNorth's)
    private static GphImage Recolour(GphImage image, byte[] map)
    {
        var copy = new GphImage(image.Width, image.Height) { OffsetX = image.OffsetX, OffsetY = image.OffsetY };
        for (var p = 0; p < image.Pixels.Length; p++) { copy.Pixels[p] = map[image.Pixels[p]]; copy.Opaque[p] = image.Opaque[p]; }
        return copy;
    }

    // houseshade (ScriptRoundTrip): WestToNorth's map of a game folder's house, to look at
    public static List<string> ShadeReport(string gameDirectory)
    {
        var report = new List<string>();
        var bkg = HqrFile.Parse(File.ReadAllBytes(Path.Combine(gameDirectory, "LBA_BKG.HQR")));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        var raw = bkg.Read(header.Gri + table[House * 2 + 1]);
        var library = bkg.Read(header.Bll + raw[0]);
        byte[]? Brick(int b) => b >= 0 && b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? bkg.Read(header.Brk + b) : null;
        WestToNorth(library, Brick, report);
        // (and each wallpaper's colours, counted)
        foreach (var block in new[] { WestWallpaper, NorthWallpaper })
        {
            var count = new int[256];
            foreach (var e in GridPaint.Entries(library, block))
                if (Brick(e.Brick) is { Length: >= 4 } d && GphImage.Decode(d) is var im)
                    for (var p = 0; p < im.Pixels.Length; p++) if (im.Opaque[p]) count[im.Pixels[p]]++;
            report.Add($"block {block}: " + string.Join(" ", Enumerable.Range(0, 256).Where(c => count[c] > 0).Select(c => $"{c}:{count[c]}")));
        }
        return report;
    }

    // A block as it is but with no shape at any position: drawn, never in anyone's way (the library's own like it if a build made it before)
    private static int GhostBlock(ref byte[] library, int block)
    {
        var info = GridPaint.Info(library, block)!.Value;
        var entries = GridPaint.Entries(library, block).ToList();
        var count = GridPaint.BlockCount(library);
        for (var b = 1; b <= count; b++)
        {
            if (b == block || GridPaint.Info(library, b) is not { } other || (other.Dx, other.Dy, other.Dz) != (info.Dx, info.Dy, info.Dz)) continue;
            var theirs = GridPaint.Entries(library, b).ToList();
            if (theirs.Count == entries.Count && theirs.Zip(entries).All(t => t.First.Shape == 0 && t.First.Brick == t.Second.Brick && t.First.Sound == t.Second.Sound)) return b;
        }
        if (count >= 255) throw new InvalidDataException("The house's block library has 255 blocks already: a grid can't name another.");
        library = GridPaint.AppendBlock(library, info.Dx, info.Dy, info.Dz, 0);
        foreach (var e in entries) library = GridPaint.SetEntry(library, count + 1, e.Pos, e.Brick, 0, e.Sound);
        return count + 1;
    }

    // A block mirrored x for z -- its sizes swapped, each position's brick drawn flipped (a new brick, or the file's own the same), the shapes
    // full ones (the doorway's are) -- the library's own block like it if a build made it before, else a new one after its last
    private static int MirrorBlock(HqrFile bkg, MapBricks.BkgHeader header, ref byte[] library, int block, Func<int, byte[]?> brick, byte[] shade)
    {
        var info = GridPaint.Info(library, block)!.Value;
        var entries = GridPaint.Entries(library, block).ToList();
        int dx = info.Dx, dy = info.Dy, dz = info.Dz, n = dx * dy * dz;
        var bricks = new int[n]; var shapes = new int[n]; var sounds = new int[n];
        foreach (var e in entries)
        {
            int i = e.Pos % dx, j = e.Pos / dx % dy, k = e.Pos / (dx * dy);
            var q = k + dz * (j + dy * i);
            shapes[q] = e.Shape; sounds[q] = e.Sound;
            if (e.Brick >= 0 && brick(e.Brick) is { Length: >= 4 } data)
            {
                var image = GphImage.Decode(data);
                bricks[q] = CitadelMuseum.AddBrickPublic(bkg, header, CitadelMuseum.EncodeBrick(Recolour(Flip(image, 48 - image.OffsetX - image.Width), shade)));
            }
            else bricks[q] = e.Brick;
        }
        var count = GridPaint.BlockCount(library);
        for (var b = 1; b <= count; b++)
        {
            if (GridPaint.Info(library, b) is not { } other || (other.Dx, other.Dy, other.Dz) != (dz, dy, dx)) continue;
            var theirs = GridPaint.Entries(library, b).ToList();
            if (theirs.Count == n && theirs.All(t => t.Shape == shapes[t.Pos] && t.Sound == sounds[t.Pos] && t.Brick == bricks[t.Pos])) return b;
        }
        if (count >= 255) throw new InvalidDataException("The house's block library has 255 blocks already: a grid can't name another.");
        library = GridPaint.AppendBlock(library, dz, dy, dx, 0);
        for (var p = 0; p < n; p++) library = GridPaint.SetEntry(library, count + 1, p, bricks[p], shapes[p], sounds[p]);
        return count + 1;
    }
}
