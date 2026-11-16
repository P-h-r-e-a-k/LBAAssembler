using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Assets;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;
using LBAAssembler.Terrain.Citadel;

namespace LBAAssembler.Terrain;

// The hidden bowling alley's room (BowlingAlley's scene 393), redone (the user, 2026-11-15: "Our bowling area needs a massive redesign, it's
// visibly terrible ... The bowling alley needs to be a dark evil place, it's full of the under belly of society, there's just no walls or
// character to the room currently at all"; "We need to add a bar area and generally just make the whole place dark and seedy"; "Twinsen also
// currently can walk under the alleys, and the alleys themselves can vanish depending upon where Twinsen stands" -- the lanes were one big
// prop actor). The seedy twin of Rick's bar, two floors under Otringal's prison:
//   - Rick's bricks (scene 136's grid and block library), in a copy of his library of its own -- every block the room uses painted dark in
//     place: his sky-blue stucco a dried-blood maroon, its clouds smoke stains, his green vinyl a deep red, his floor tiles grime, the rest
//     at half their light (the lamps' globes still a dim glow) -- the grid naming his block numbers as they are;
//   - the lanes are floor: copies of his floor tile painted as a lane's boards, gutters, foul line, arrows, approach dots, the pin deck and
//     the pit behind it -- walked on, never gone from the picture; Twinsen kept off them past the foul line by invisible walls;
//   - his north and west walls round it (their columns copied, as Stella Entities' are), a low stone wall on the south and east rims;
//   - his raised booths and a slot machine up their stairs along the north wall's east end, and his U of a bar with its stools south of
//     them, the lift's plates in the south-east corner.
internal static class BowlingRoom
{
    public const int RickBar = 136;
    private const int Size = 64, Height = 25;
    public const int FloorLayer = 0;
    // the room's floor (cells) and its walls: the north wall's row and the west wall's column; the low rims south and east
    public const int RoomX0 = 10, RoomX1 = 55, RoomZ0 = 8, RoomZ1 = 47;
    // the lanes: each six cells across -- a gutter, four boards, a gutter -- between dividers (x 11, 18, 25, 32); the pit (z 8-9), the pin deck
    // (10-14), the lane (15-35), the foul line's row (36), the approach (37-41)
    public static readonly int[] LaneGutterLeft = { 12, 19, 26 };
    public const int PitZ0 = 8, DeckZ0 = 10, LaneZ0 = 15, FoulRow = 36, ApproachZ1 = 41, ArrowRow = 30, DotRow = 39;
    public const int LanesX0 = 11, LanesX1 = 32;
    // a lane's middle (world units) by its number
    public static int LaneMiddle(int lane) => (LaneGutterLeft[lane] + 3) * 512;
    // Rick's: his floor tile, his north wall's columns (x 38-40, z 12), his west wall's (x 17, z 36 on, 4 then 3 a repeat from layer 18), the
    // low stone wall (block 19); his booths' platform with its stairs and slot machine (x 18-29, z 23-37) and his bar (x 35-45, z 35-45)
    private const int Tile = 67, LowWall = 19, WestUpper = 18;
    // (the invisible cells over the low rims: up to this layer, well over a jump from the floor)
    private const int RimTop = FloorLayer + 12;
    private static readonly (int X, int Z) North = (38, 12), West = (17, 36);
    // (2026-11-15, second round: his booths, their tables and his fruit machines only -- his raised platform under the western ones, its
    // stairs, its rails and low walls left out, those booths lowered its three layers onto the floor: the stairs led nowhere, the user,
    // "You've also added stairs to nowhere that just causes twinsen to drop down to the floor below"; his wall lamps, on his wall there,
    // left out too -- "These wall lights appear to be floating in the air" -- in the room's middle east; his bar its own blocks only, its
    // back to the north wall -- "push the back of a bar up against a wall", "a corner section of post by the bar that looks out of place")
    private static readonly (int X0, int Z0, int X1, int Z1, int Dx, int Dz) Booths = (18, 23, 29, 37, 18, 3);
    // (his potted plant, 81 and 40, left out: it stood before the poker table; his two fruit machines, 80, too since 2026-11-16 -- the alley's
    // own in their places, their reels turning in them: CitadelProps.BowlSlot)
    private static readonly HashSet<int> BoothBlocks = new() { 32, 33, 35, 36, 39, 205, 206 };
    // the fruit machines (CitadelProps.BowlSlot actors, BowlingAlley): each cabinet's middle (world units), its front south (+z) -- where
    // Rick's two stood, and five side by side along the south strip under the lanes' approach (the user, 2026-11-16: "Let's add in some
    // more fruit machines around the edge in a cluster together")
    public static readonly (int X, int Z)[] FruitMachines =
    {
        (42 * 512, 26 * 512 + 256), (38 * 512, 39 * 512 + 256),
        (14 * 512 + 256, 43 * 512 + 256), (16 * 512 + 256, 43 * 512 + 256), (18 * 512 + 256, 43 * 512 + 256), (20 * 512 + 256, 43 * 512 + 256), (22 * 512 + 256, 43 * 512 + 256),
    };
    // (two more booths, Rick's east one -- cells x 44-47, z 27-32 in the room -- again: east of it, a cell further north (clear of the card
    // table's chair), and south of the poker table by the south rim -- the user, 2026-11-16, "Perhaps add a couple of booth tables, and
    // essentially just fill the space")
    private static readonly (int X0, int Z0, int X1, int Z1) EastBooth = (44, 27, 47, 32);
    private static readonly (int Dx, int Dz)[] MoreBooths = { (5, -1), (-1, 14) };

    public readonly record struct Built(int GridNumber, byte Type, byte[] Grid, int Library);

    // The room's grid and its library, into LBA_BKG.HQR (`bkg`, its header kept up to date): the grid's number, or null (the log says why)
    public static Built? Install(string gameDirectory, HqrFile bkg, MapBricks.BkgHeader header, List<string> log)
    {
        var table = bkg.Read(header.Brk + header.MaxBrk);
        var type = table[RickBar * 2];
        var raw = bkg.Read(header.Gri + table[RickBar * 2 + 1]);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * Size + x) * Height + y) * 2;
        int Mod(int a, int m) => ((a % m) + m) % m;
        var rickLibrary = bkg.Read(header.Bll + raw[0]);

        var set = new Dictionary<(int, int, int), (int Block, int Pos)>();
        void Put(int x, int y, int z, int block, int pos) => set[(x, y, z)] = (block, pos);
        // (the blocks of his walls' columns: painted as walls -- his stucco and its clouds -- by Dark)
        var walls = new HashSet<int>();
        void Column(int x, int z, Func<int, (int X, int Z)> from)
        {
            for (var y = FloorLayer + 1; y < Height; y++)
            {
                var (sx, sz) = from(y);
                var at = At(sx, y, sz);
                if (cells[at] != 0) { Put(x, y, z, cells[at], cells[at + 1]); walls.Add(cells[at]); }
            }
        }
        // ---- the floor, the walls and the rims
        for (var z = RoomZ0; z <= RoomZ1; z++)
            for (var x = RoomX0; x <= RoomX1; x++) Put(x, FloorLayer, z, Tile, Mod(x, 3) + 3 * Mod(z, 3));
        for (var x = RoomX0 - 1; x <= RoomX1 + 1; x++) { Put(x, FloorLayer, RoomZ0 - 1, Tile, Mod(x, 3)); Column(x, RoomZ0 - 1, y => (North.X + Mod(x - North.X, 3), North.Z)); }
        for (var z = RoomZ0; z <= RoomZ1 + 1; z++) { Put(RoomX0 - 1, FloorLayer, z, Tile, 3 * Mod(z, 3)); Column(RoomX0 - 1, z, y => (West.X, West.Z + Mod(z - West.Z, y >= WestUpper ? 3 : 4))); }
        for (var x = RoomX0; x <= RoomX1 + 1; x++) { Put(x, FloorLayer, RoomZ1 + 1, Tile, Mod(x, 3)); Put(x, FloorLayer + 1, RoomZ1 + 1, LowWall, Mod(x, 2)); }
        for (var z = RoomZ0; z <= RoomZ1; z++) { Put(RoomX1 + 1, FloorLayer, z, Tile, 3 * Mod(z, 3)); Put(RoomX1 + 1, FloorLayer + 1, z, LowWall, Mod(z, 2)); }
        // (over the low rims, invisible solid cells up to RimTop: over the low wall Twinsen climbed and fell out of the room into the dark,
        // to his death -- the user, 2026-11-16, "We need to prevent Twinsen being able to die over the edge of the bowling scene")
        for (var x = RoomX0; x <= RoomX1 + 1; x++) for (var y = FloorLayer + 2; y <= RimTop; y++) Put(x, y, RoomZ1 + 1, 0, 1);
        for (var z = RoomZ0; z <= RoomZ1; z++) for (var y = FloorLayer + 2; y <= RimTop; y++) Put(RoomX1 + 1, y, z, 0, 1);
        // ---- Rick's booths (on the floor) and his bar (its back to the north wall)
        // (his bar's bricks no longer: since 2026-11-16 the bar is props -- BowlingAlley, CitadelProps.BarCounter -- its U open to the north
        // wall, the barman in it; the user: "the U shape of the bar in the bowling alley needs turning around so the gap is against the wall",
        // and Rick's blocks can't be turned)
        foreach (var (r, keep) in new[] { (Booths, BoothBlocks) })
            for (var z = r.Z0; z <= r.Z1; z++)
                for (var x = r.X0; x <= r.X1; x++)
                    for (var y = 1; y < Height; y++)
                    {
                        var at = At(x, y, z);
                        if (cells[at] == 0 || !keep.Contains(cells[at])) continue;
                        // (the booths on his raised platform -- x 18-24 up to z 34 -- down its three layers; his steps, 41 and 42, left out)
                        var ty = r == Booths && x <= 24 && z <= 34 ? y - 3 : y;
                        var (tx, tz) = (x + r.Dx, z + r.Dz);
                        if (ty < 1 || tx < RoomX0 || tx > RoomX1 || tz < RoomZ0 || tz > RoomZ1) continue;
                        Put(tx, ty, tz, cells[at], cells[at + 1]);
                    }

        foreach (var (dx, dz) in MoreBooths)
            foreach (var key in set.Keys.Where(k => k.Item1 >= EastBooth.X0 && k.Item1 <= EastBooth.X1 && k.Item3 >= EastBooth.Z0 && k.Item3 <= EastBooth.Z1 && k.Item2 >= 1).ToList())
                if (BoothBlocks.Contains(set[key].Block)) Put(key.Item1 + dx, key.Item2, key.Item3 + dz, set[key].Block, set[key].Pos);

        // ---- the library: a copy of Rick's, its blocks the room names painted dark in place, the lanes' blocks added
        var library = (byte[])rickLibrary.Clone();
        var used = set.Values.Select(v => v.Block).Where(b => b > 0).ToHashSet();
        // (inserted after the last library: its number; the grid goes in later, before the libraries, which moves them on by one)
        var libraryNumber = header.Brk - header.Bll;
        if (libraryNumber > 255) { log.Add("The bowling alley left out: LBA_BKG.HQR has 256 block libraries already"); return null; }
        bkg.InsertAt(header.Brk, library);
        header.Brk += 1;
        var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
        byte[]? Brick(int b) => b >= 0 && b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? bkg.Read(header.Brk + b) : null;
        var darkened = 0;
        foreach (var block in used.OrderBy(b => b))
        {
            var wall = walls.Contains(block);
            var painted = CitadelMuseum.Repaint(library, block, Brick, (x, y, c) => Dark(c, wall));
            foreach (var (pos, image) in painted)
            {
                var brick = CitadelMuseum.AddBrickPublic(bkg, header, CitadelMuseum.EncodeBrick(image));
                library = GridPaint.SetEntry(library, block, pos, brick);
            }
            if (painted.Count > 0) darkened++;
        }
        // the fruit machines' screens: a hole in Rick's machine's bricks, the floor seen through it (the user, 2026-11-16: "The fruit machines
        // show the floor tiles through the screens") -- filled: dark glass with three lit reels
        {
            var filled = FillScreen(library, FruitBlock, Brick);
            foreach (var (pos, image) in filled)
                library = GridPaint.SetEntry(library, FruitBlock, pos, CitadelMuseum.AddBrickPublic(bkg, header, CitadelMuseum.EncodeBrick(image)));
            if (filled.Count == 0) log.Add("The bowling alley's room: the fruit machine's screen not found (no hole in block 80's picture)");
        }
        // the lanes' floor: the floor tile painted (its nine places alike), a block each kind
        var kinds = new Dictionary<LaneKind, int>();
        var tileEntries = GridPaint.Entries(rickLibrary, Tile).ToList();
        var corner = (U: int.MaxValue, V: int.MaxValue);
        CitadelMuseum.Repaint(rickLibrary, Tile, Brick, (x, y, c) => { corner = (Math.Min(corner.U, x + 2 * y), Math.Min(corner.V, 2 * y - x)); return c; });
        foreach (var kind in Enum.GetValues<LaneKind>())
        {
            var painted = CitadelMuseum.Repaint(rickLibrary, Tile, Brick, (x, y, c) =>
            {
                double u = x + 2 * y - corner.U, v = 2 * y - x - corner.V;
                // (the tile's top only: its edges' sides as they are, darkened)
                if (u < 0 || v < 0 || u >= 144 || v >= 144) return Dark(c, false);
                return LanePaint(palette, kind, u / 48 % 1, v / 48 % 1, (int)(u / 48), (int)(v / 48));
            });
            var number = GridPaint.BlockCount(library) + 1;
            library = GridPaint.AppendBlock(library, 3, 1, 3, tileEntries[0].Brick, tileEntries[0].Shape, tileEntries[0].Sound);
            foreach (var e in tileEntries)
                library = GridPaint.SetEntry(library, number, e.Pos, painted.TryGetValue(e.Pos, out var im) ? CitadelMuseum.AddBrickPublic(bkg, header, CitadelMuseum.EncodeBrick(im)) : e.Brick, e.Shape, e.Sound);
            kinds[kind] = number;
        }
        // the lanes laid
        for (var z = PitZ0; z <= ApproachZ1; z++)
            for (var x = LanesX0; x <= LanesX1; x++)
            {
                var kind = z < DeckZ0 ? LaneKind.Pit : z > FoulRow ? (z == DotRow && OnBoards(x) ? LaneKind.Dots : LaneKind.Approach)
                    : !InLane(x) ? LaneKind.Divider : Gutter(x) ? LaneKind.Gutter
                    : z < LaneZ0 ? LaneKind.Deck : z == FoulRow ? LaneKind.Foul : z == ArrowRow ? LaneKind.Arrow : LaneKind.Board;
                Put(x, FloorLayer, z, kinds[kind], Mod(x, 3) + 3 * Mod(z, 3));
                // (and Twinsen kept off the lanes past the foul line: the ball's and the pins' -- three layers of invisible wall)
                if (z <= FoulRow) for (var y = FloorLayer + 1; y <= FloorLayer + 3; y++) Put(x, y, z, 0, 1);
            }
        bkg.SetStored(header.Bll + libraryNumber, library);
        if (header.MaxBll < (uint)library.Length) header.MaxBll = (uint)library.Length;

        // ---- the grid
        var edits = new List<Lba1GridCell>();
        for (var z = 0; z < Size; z++)
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Size; x++)
        {
            var (block, pos) = set.TryGetValue((x, y, z), out var c) ? c : (0, 0);
            edits.Add(new Lba1GridCell(x, y, z, block, pos));
        }
        var grid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, edits));
        grid[0] = (byte)libraryNumber;
        log.Add($"The bowling alley's room: Rick's bricks in a library of its own ({libraryNumber}), {darkened} of his {used.Count} blocks it uses painted dark, " +
                $"{kinds.Count} lane floor blocks ({string.Join(", ", kinds.Select(k => $"{k.Key} {k.Value}"))})");
        return new Built(-1, type, grid, libraryNumber);
    }

    private const int FruitBlock = 80;
    // A block's picture's holes -- its transparent pixels the outside of the picture can't reach through other transparent ones -- filled, each
    // in the brick drawn last over it: a slot machine's screen, three reels across it (each row's hole split in three, light with a dark edge,
    // a symbol in each's middle row). The positions whose bricks it changed, with their new pictures.
    private static Dictionary<int, GphImage> FillScreen(byte[] library, int block, Func<int, byte[]?> brick)
    {
        var info = GridPaint.Info(library, block)!.Value;
        var entries = GridPaint.Entries(library, block).ToList();
        var images = new Dictionary<int, GphImage>();
        var placed = new List<(int Pos, int Left, int Top)>();
        for (var k = 0; k < info.Dz; k++)
            for (var i = 0; i < info.Dx; i++)
                for (var j = 0; j < info.Dy; j++)
                {
                    var pos = i + info.Dx * (j + info.Dy * k);
                    if (pos >= entries.Count || brick(entries[pos].Brick) is not { Length: >= 4 } data) continue;
                    images[pos] = GphImage.Decode(data);
                    placed.Add((pos, 24 * (i - k) + (sbyte)data[2], 12 * (i + k) - 15 * j + (sbyte)data[3]));
                }
        if (placed.Count == 0) return new();
        int minX = placed.Min(p => p.Left) - 1, minY = placed.Min(p => p.Top) - 1;
        int w = placed.Max(p => p.Left + images[p.Pos].Width) - minX + 1, h = placed.Max(p => p.Top + images[p.Pos].Height) - minY + 1;
        var opaque = new bool[w * h];
        var owner = new (int Pos, int At)[w * h];
        for (var q = 0; q < owner.Length; q++) owner[q] = (-1, 0);
        foreach (var (pos, left, top) in placed)
        {
            var image = images[pos];
            for (var v = 0; v < image.Height; v++)
                for (var u = 0; u < image.Width; u++)
                {
                    var at = (top - minY + v) * w + left - minX + u;
                    owner[at] = (pos, v * image.Width + u);
                    if (image.Opaque[v * image.Width + u]) opaque[at] = true;
                }
        }
        // (the outside: every transparent pixel reached from the picture's edge)
        var outside = new bool[w * h];
        var stack = new Stack<int>();
        for (var x = 0; x < w; x++) { stack.Push(x); stack.Push((h - 1) * w + x); }
        for (var y = 0; y < h; y++) { stack.Push(y * w); stack.Push(y * w + w - 1); }
        while (stack.Count > 0)
        {
            var q = stack.Pop();
            if (outside[q] || opaque[q]) continue;
            outside[q] = true;
            int x = q % w, y = q / w;
            if (x > 0) stack.Push(q - 1);
            if (x < w - 1) stack.Push(q + 1);
            if (y > 0) stack.Push(q - w);
            if (y < h - 1) stack.Push(q + w);
        }
        var hole = Enumerable.Range(0, w * h).Where(q => !opaque[q] && !outside[q] && owner[q].Pos >= 0).ToList();
        if (hole.Count == 0) return new();
        // (the screen's rows: three reels across each, light grey with a dark edge; the middle rows a symbol: red, gold, red)
        const byte Glass = 3 * 16 + 1, Reel = 3 * 16 + 11, Edge = 3 * 16 + 3, Red = 4 * 16 + 7, Gold = 6 * 16 + 10;
        // (split across by columns -- a slot machine's screen leans back, its sides stay upright in the picture -- each column's top and
        // bottom the screen's there, its middle row a symbol's)
        int x0 = hole.Min(q => q % w), x1 = hole.Max(q => q % w), span = x1 - x0 + 1;
        var changed = new HashSet<int>();
        foreach (var column in hole.GroupBy(q => q % w))
        {
            int x = column.Key, y0 = column.Min(q => q / w), y1 = column.Max(q => q / w), middle = (y0 + y1) / 2;
            var third = Math.Min(2, (x - x0) * 3 / span);
            double inThird = (x - x0) * 3.0 / span - third;
            // (every pixel down the column between the screen's top and bottom: the dark streaks drawn on Rick's glass too)
            for (var y = y0; y <= y1; y++)
            {
                var q = y * w + x;
                if (owner[q].Pos < 0) continue;
                byte c = y <= y0 + 1 || y >= y1 - 1 || inThird < 0.1 || inThird > 0.9 ? Glass : Reel;
                if (c == Reel && (y == y0 + 2 || y == y1 - 2)) c = Edge;
                if (c == Reel && Math.Abs(y - middle) <= 2 && Math.Abs(inThird - 0.5) < 0.18) c = third == 1 ? Gold : Red;
                var (pos, at) = owner[q];
                images[pos].Pixels[at] = c;
                images[pos].Opaque[at] = true;
                changed.Add(pos);
            }
        }
        return changed.ToDictionary(p => p, p => images[p]);
    }

    private static bool InLane(int x) => LaneGutterLeft.Any(g => x >= g && x <= g + 5);
    private static bool Gutter(int x) => LaneGutterLeft.Any(g => x == g || x == g + 5);
    private static bool OnBoards(int x) => LaneGutterLeft.Any(g => x > g && x < g + 5);

    private enum LaneKind { Board, Gutter, Foul, Arrow, Dots, Deck, Approach, Divider, Pit }

    // A lane's floor at a place in its cell (fx across it -- the world's x --, fz along it, 0 to 1) and the cell's place in the tile's 3 x 3
    private static byte LanePaint(byte[] palette, LaneKind kind, double fx, double fz, int ci, int ck)
    {
        int C(int ramp, int shade) => ramp * 16 + Math.Clamp(shade, 0, 15);
        // (boards along the lane, four a cell, each its own shade of the varnished wood; the seams between them)
        int Board(int ramp, int baseShade)
        {
            var board = (int)(fx * 4);
            var seam = fx * 4 - board < 0.07;
            if (seam) return C(1, 4);
            var h = (board * 7 + ci * 13) % 3;
            var grain = ((int)((fz + board * 0.31) * 9) % 4 == 0) ? -1 : 0;
            return C(ramp, baseShade + h - 1 + grain);
        }
        switch (kind)
        {
            case LaneKind.Board: return (byte)Board(6, 8);
            case LaneKind.Deck: return (byte)Board(2, 7);
            case LaneKind.Approach:
            {
                var plank = (int)(fx * 2);
                if (fx * 2 - plank < 0.05 || fz < 0.04) return (byte)C(1, 2);
                return (byte)C(1, 8 + (plank + ci) % 2);
            }
            case LaneKind.Dots:
            {
                var d = Math.Sqrt((fx - 0.5) * (fx - 0.5) + (fz - 0.5) * (fz - 0.5));
                if (d < 0.09) return (byte)C(4, 11);
                var plank = (int)(fx * 2);
                if (fx * 2 - plank < 0.05) return (byte)C(1, 2);
                return (byte)C(1, 8 + (plank + ci) % 2);
            }
            case LaneKind.Foul:
                // (the foul line across the lane's near edge, a red that glows in the dark)
                if (fz > 0.86) return (byte)C(4, 12);
                if (fz > 0.80) return (byte)C(4, 7);
                return (byte)Board(6, 8);
            case LaneKind.Arrow:
            {
                // (an arrow pointing down the lane, north: its tip at fz 0.2, its base at 0.62)
                if (fz >= 0.2 && fz <= 0.62 && Math.Abs(fx - 0.5) <= (fz - 0.2) * 0.45) return (byte)C(4, Math.Abs(fx - 0.5) > (fz - 0.2) * 0.32 ? 7 : 10);
                return (byte)Board(6, 8);
            }
            case LaneKind.Gutter:
            {
                // (a rounded steel channel: dark down its middle, a light rim each side)
                var e = Math.Abs(fx - 0.5) * 2;
                if (e > 0.9) return (byte)C(3, 6);
                if (e > 0.75) return (byte)C(3, 4);
                return (byte)C(3, 1 + (int)(e * 3));
            }
            case LaneKind.Divider:
            {
                // (the dark caps between the lanes, a rivet each end)
                var r = Math.Abs(fx - 0.5) < 0.06 && (fz < 0.12 || fz > 0.88);
                return (byte)C(r ? 3 : 14, r ? 6 : 1);
            }
            default: return (byte)C(3, fz < 0.5 ? 0 : 1);   // the pit: black
        }
    }

    // A colour of Rick's at night under the prison: the palette's ramps (16 of 16 shades, dark to light): his sky's blues (ramps 9-12) a
    // dried-blood maroon; his vinyl's greens (7-8) a deep red; on his walls the clouds' whites smoke stains, near black; the rest at half
    // their light -- the brightest whites off the walls (his lamps' globes) left a dim glow
    internal static byte Dark(byte c, bool wall)
    {
        int ramp = c >> 4, shade = c & 15;
        if (ramp == 0 || ramp == 15) return c;
        int Half(double k) => (int)Math.Round(shade * k);
        return ramp switch
        {
            // (2026-11-15, second round: "make the place slightly darker and more menacing feeling" -- every shade lower again)
            >= 9 and <= 12 => (byte)(4 * 16 + Half(wall ? 0.22 : 0.3)),
            7 or 8 => (byte)(4 * 16 + Half(0.42)),
            3 or 13 or 14 when wall => (byte)(14 * 16 + Half(0.12)),
            3 when shade >= 14 => (byte)(3 * 16 + 9),
            3 => (byte)(3 * 16 + Half(0.3)),
            _ => (byte)(ramp * 16 + Half(wall ? 0.26 : 0.38)),
        };
    }
}
