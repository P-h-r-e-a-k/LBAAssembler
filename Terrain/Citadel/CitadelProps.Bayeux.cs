using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The secret chamber's Bayeux hanging (the user, 2026-11-09: "The long wall section at the bottom of the screen where the original entrance
// was ... let's add a bayeaux tapestry style hanging based on the first game, let's have this draping over the top of the wall, so the
// side closest to the screen will actually be upside down, and we won't be able to see the part facing into the room"). LBA1's story in
// embroidery on linen -- Twinsen's house, the clones taking him, his flight, the boat, the Dinofly, Funfrock on his throne with his
// clones, Sendell, the victory -- each scene's word over it, between borders of slanted bars, birds and beasts, as the real one has. The
// rows below are the cloth drawn upright (BayeuxColours: what each letter stitches); the game hangs it over the front wall's top
// (CitadelLba1Rooms' z 63, its top at layer 8), so the camera sees it turned right round: its first row at the bottom of the drop and
// its first column at the right. Flat colours, no light (as the Sendell seal's picture), a stitch BayeuxStitch units square; in
// BayeuxParts bodies of BayeuxColumns columns each (the body's limits), each square of one colour as few rectangles as cover it.
internal static partial class CitadelProps
{
    public const int Bayeux = 23, BayeuxParts = 8, BayeuxColumns = 32, BayeuxStitch = 36;
    // (where the cloth goes, in each part's own frame: the part stands on the wall's top, BayeuxBack in from the wall's outside face -- the
    // wall cell's middle, where the engine puts an actor standing further out anyway -- and the cloth over the top reaches BayeuxFold in
    // from that face: short of the room's side, so that the part's box (the engine's MINI_ZV square: half the body's least extent, here
    // its depth) stays clear of anyone in the room, and the sort draws it after them)
    public const int BayeuxBack = 256, BayeuxFold = 384;
    private static readonly Dictionary<char, int> BayeuxColours = new()
    {
        ['.'] = 43, ['b'] = 196, ['r'] = 86, ['g'] = 121, ['y'] = 105, ['l'] = 187, ['k'] = 97, ['w'] = 45,
    };
    private static readonly string[] BayeuxRows =
    {
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
        "..r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.",
        ".r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r..",
        "r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r...",
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
        "................................................................................................................................................................................................................................................................",
        "....bbb.b.b.bbb.bb..bbb.bbb.bb...........bbb.b...bbb.bb..bbb.............bbb.b.b.bbb..b................bb...b..b.b.bbb.bbb..........bb..bbb.bb..bbb.bbb.b...b.b....bbb.b.b.bb..bbb.bb..bbb.bbb.b.b.bbb.bbb.bb..bb..bbb.b...b.....b.b.bbb.bbb.bbb.bbb.bb..bbb..b.",
        ".....b..b.b..b..b.b.b...b...b.b..........b...b...b.b.b.b..b..............b...b.b.b...b.b...............b.b.b.b.b.b..b..b............b.b..b..b.b.b.b.b...b...b.b....b...b.b.b.b.b...b.b.b.b.b...b.b.b...b...b.b.b.b.b...b...b.....b.b..b..b....b..b.b.b.b..b..b.b",
        ".....b..bbb..b..b.b.bbb.bb..b.b..........b...b...b.b.b.b..b..............bb..b.b.b.b.bbb...............b.b.bbb.b.b..b..bbb..........b.b..b..b.b.b.b.bb..b....b.....bb..b.b.b.b.bb..bb..b.b.b...bb..bbb.bb..b.b.b.b.bb..b...b.....b.b..b..b....b..b.b.bb...b..bbb",
        ".....b..bbb..b..b.b...b.b...b.b..........b...b...b.b.b.b..b..............b...b.b.b.b.b.b...............b.b.b.b.b.b..b....b..........b.b..b..b.b.b.b.b...b....b.....b...b.b.b.b.b...b.b.b.b.b...b.b...b.b...b.b.b.b.b...b...b.....b.b..b..b....b..b.b.b.b..b..b.b",
        ".....b..b.b.bbb.b.b.bbb.bbb.b.b..........bbb.bbb.bbb.b.b.bbb.............b...bbb.bbb.b.b...............b.b.b.b..b..bbb.bbb..........bb..bbb.b.b.bbb.b...bbb..b.....b...bbb.b.b.b...b.b.bbb.bbb.b.b.bbb.bbb.b.b.bb..bbb.bbb.bbb....b..bbb.bbb..b..bbb.b.b.bbb.b.b",
        "................................................................................................................................................................................................................................................................",
        ".....rrrrrr..........................llll..................llll................................................k...............................www................kk.yyy.........llll.....llll..............y.....y.............................................",
        "....rrrrrrrr......kkw....yyyy........llll.......kkw........llll..............kkw...........................kkw.kyyy............................www................kk.www...y.....llll.....llll...........y....yyy....y.................kkw..y.....yyyy..........",
        "...rrrrrrrrrr....kwww....ywww........lklll.....kwww.......lllkl.............kwww..........................kwww.kyyyy...........................bbb................kk.www...y.....lklll....lklll.............yyyyyyy...................kwww..b.....ywww..........",
        "..rrrrrrrrrrrr..k.www....ywww........lllllk...k.www......klllll............k.www...b.....................k.www.kyyyyy................gg........bbb................kk.kkk..r......lllllk...lllllk...........yyyyyyyyy.................k.www..b.....ywww..........",
        "...llllllllll....bbbbb...rrrrr......yyyyyyk....bbbbb.....kyyyyyy............bbbbbbb.......................bbbbbkyyyy..................ggg.............gg..........kkrrrrrr......yyyyyyk..yyyyyyk........y..yyyywyyyy..y...............bbbbbb......rrrrr.........",
        "...lyyllllyyl....bbbbb...rrrrr......yyyyyyk....bbbbb.....kyyyyyy............bbbbb.........................bbbbbkyyy....................ggggg......ggggg...........kkrrrrr.......yyyyyyk..yyyyyyk...........yyyyyyyyy..................bbbbb.......rrrrr.........",
        "...lyyllllyyl....bbbbb...rrrrr......yyyyyyk....bbbbb.....kyyyyyy............bbbbb.........................bbbbbk........................gggggg..gggggg............kkrrrrr.......yyyyyyk..yyyyyyk............yyyyyyy...................bbbbb.......rrrrr.........",
        "...llllllllll....bbbbb...rrrrr......yyyyyyk....bbbbb.....kyyyyyy............bbbbb......................r..bbbbb.......r..................ggggggggggggg..ww........kkkkkrrrk.....yyyyyyk..yyyyyyk.........y....yyy....y................bbbbb.......rrrrr.........",
        "...llllkkllll....bbbbb...rrrrr......kkkkkkk....bbbbb.....kkkkkkk............bbbbb......................rr.bbbbb......rr...................gggggggggggggggw........kkkkkkrrk.....kkkkkkk..kkkkkkk............y.....y...................bbbbb.......rrrrr.........",
        "...llllkkllll.....k.k.....k.k........k..k.k.....k.k......k.k..k............k....k......................rrrrkrkrrrrrrrrr....................gggggggggg.............kk....rrk......k..k.k...k..k.k...............k.......................k.k.........k.k..........",
        "...llllkkllll.....k.k.....k.k........k..k.k.....k.k......k.k..k............k.....k......................rrrkrkrrrrrrrr......................g..g...g..............kk....rrk......k..k.k...k..k.k...............k.......................k.k.........k.k..........",
        "...llllkkllll.....k.k.....k.k........k..k.......k.k........k..k.............k.....k......................yykykyyyyyyy......................gg.gg..gg..............kk.....kk......k..k.....k..k...............kkkkk.....................k.k.........k.k..........",
        "gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggblgblgblgblgblgblgblgblgggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg",
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
        "...r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r",
        "..r.....y...yyy.....r.....y.....g...yyy.....y.....g.....r...yyy.....g.....r.....y...yyy.....r.....y.....g...yyy.....y.....g.....r...yyy.....g.....r.....y...yyy.....r.....y.....g...yyy.....y.....g.....r...yyy.....g.....r.....y...yyy.....r.....y.....g...yyy.",
        ".r.....y....y.y....r.....y.....g....y.y....y.....g.....r....y.y....g.....r.....y....y.y....r.....y.....g....y.y....y.....g.....r....y.y....g.....r.....y....y.y....r.....y.....g....y.y....y.....g.....r....y.y....g.....r.....y....y.y....r.....y.....g....y.y.",
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
    };

    // Part `part` of the hanging: its columns (as the camera sees them, left to right: the cloth's from its last) down the wall's outside,
    // and the cloth over the wall's top behind them
    public static Body BuildBayeux(byte[] header, int part)
    {
        var m = new CarMesh($"the Bayeux hanging, part {part + 1}", new[] { -1 });
        int width = BayeuxRows[0].Length, height = BayeuxRows.Length;
        const float Face = BayeuxBack + 4;
        // the stitch the camera sees at column c (from the left, in this part) and row r (from the top of the drop)
        char At(int c, int r) => BayeuxRows[height - 1 - r][width - 1 - (part * BayeuxColumns + c)];
        var points = new Dictionary<(int, int), int>();
        int Point(int c, int r) => points.TryGetValue((c, r), out var h) ? h
            : points[(c, r)] = m.P(0, new Vector3((c - BayeuxColumns / 2f) * BayeuxStitch, -r * BayeuxStitch, Face));
        Stitched(m, 0, BayeuxColumns, height, At, Point, BayeuxColours, new Vector3(0, 0, -100));
        // the cloth over the wall's top, plain linen, from the outside face most of the way in
        float x0 = -BayeuxColumns / 2f * BayeuxStitch, x1 = BayeuxColumns / 2f * BayeuxStitch;
        m.Out(new[] { Point(0, 0), Point(BayeuxColumns, 0), m.P(0, new Vector3(x1, 0, Face - BayeuxFold)), m.P(0, new Vector3(x0, 0, Face - BayeuxFold)) },
              BayeuxColours['.'], new Vector3(0, -100, 0), unlit: true);
        return m.ToBody(header);
    }
}
