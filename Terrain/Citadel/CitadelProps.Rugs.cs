using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The secret chamber's pictures in squares of flat colour (the user, 2026-11-09): in the lower room "the rectangular red mat, let's make
// this larger and turn it into a mat with dinofly on it with Twinsen riding him"; in the hall "a large rectangular mat that fills almost
// the entire floor with Sendell on the mat"; on the lower room's back wall "another bayeaux tapestry with different elements of LBA1 than
// we already have shown, this time the right way up so it's viewable"; and a Jolly Roger. Each drawn upright below (a rug's row 0 its far
// edge, the back wall's side, -z; its columns along +x). A rug lies on the floor, its top no higher than it (an actor's box stops
// another's only where they overlap in height, so nobody walks into a rug, and the sort draws whoever stands on it after it), cut into
// tiles -- bands of rows, each band into as many column ranges as the body's limits want -- each tile's actor at its own near corner (its
// least x and z): the engine redraws over an actor only the bricks from its own cell on, so a tile's bricks standing on or past it -- the
// hall's barrel, the stairs' posts -- are drawn over it again. Each square a rectangle grown a hair (Grow) into its neighbours: shared
// corners left the software filler's hairline cracks along the joins ("there's lines on it that shouldn't be there").
internal static partial class CitadelProps
{
    public const int DinoFirst = 64, DinoMost = 12, SendellFirst = 40, SendellMost = 18, DinoStitch = 64, SendellStitch = 80, RugBand = 13;
    public const float Grow = 0.06f;
    private static readonly Dictionary<char, int> RugColours = new()
    {
        ['.'] = 72, ['R'] = 83, ['r'] = 86, ['y'] = 106, ['Y'] = 108, ['b'] = 196, ['B'] = 194, ['l'] = 204, ['L'] = 207, ['w'] = 95,
        ['g'] = 137, ['G'] = 135, ['k'] = 97, ['s'] = 41, ['c'] = 223, ['o'] = 88, ['n'] = 195, ['p'] = 78,
        ['C'] = 199, ['q'] = 172, ['Q'] = 175, ['h'] = 198, ['H'] = 202, ['K'] = 48, ['W'] = 62, ['S'] = 203,
    };
    public static readonly string[] DinoRows =
    {
        "yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy",
        "ybbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbby",
        "ybYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbYby",
        "ybbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbkkbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbby",
        "ybbbyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyykkkyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyybbby",
        "ybbbySSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSkkSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSybbby",
        "ybYbySSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSoooooSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSybYby",
        "ybbbySSSSSSSSSSSSSSSScSSScccccSSSSSSSSSsssSSSSSSSSSSSSSSSSSSSSSSSSGGGGSSSSSybbby",
        "ybbbySSSSSSSSSSSSSSccccccccccccSSSSSSSsssssSSSSSSSSSSSSSSSSGGGGGGGGGGSSSSSSybbby",
        "ybbbySSSSSSSSSSSSScGccccGccccccSSSSSSSsbsbsSSSSSsSSSSSSSGGGGGGGGGGGGccccSSSybbby",
        "ybYbySSSSSSSSSSSSScGcccGGccccccSSSSSSSsssssSSSSSSSSSSSSGGGGGGGGGGGGccccccSSybYby",
        "ybbbySSSSSSSSSSSSSSGGccGGcccccSSSSSSSSSsssSSSSLLSSSSSSGGGGGGGGGGGGcccccccSSybbby",
        "ybbbySSSSSSSSSSSSSSGGGcGGGcSSSSSSSSSSLLLLLLSSLLSSSSSSGGGGGGGGGGGGGGccccccSSybbby",
        "ybbbySSSSSSSSSSSSSSSSSgSSSSSSSSSSSSSSLLLLLLSLLSSSSSSGGGGGGGGGGGGGGGGGGccSSSybbby",
        "ybYbySSSSSSSSSSSSSSrgwwwggSSSSSSSSSSSLLLLLLSLSSSSSSSGGGGGGGGGGGGGGGGGGGSSSSybYby",
        "ybbbySSSSSSSSSSSSSgrwwwwwggSSSSSSSSSSLLLYLLSSSSSSSSGGGGGGGGGGGGGGGGGSSSSSSSybbby",
        "ybbbySSSSSSSSSSSSggrwbbwwgggSSSSSLLLLLLLyLLSSSSSSSGGGGGGGGGGGGGGGSSSSSSSSSSybbby",
        "ybbbySSSSSSSSSgSSggrwbbwwgggSSSSsrSSLLLLLLLLSSSSSGGGGGGGGGGGGGGGSSSSSSSSSSSybbby",
        "ybYbySSSSSgggggggggrgwwwrrgrgrrSSSSSLLLLLLLLSSSSGGGGGGGGGGGGGGGGGSSSSSSSSSSybYby",
        "ybbbySSSSgggrrrrrrrrgrrgggggSSSSSSSSLLLLLLLLSSSSGGGGGGGGGGGGGGGGGGSSSSSSSSSybbby",
        "ybbbySSSgKgggggggggrggggggggSSSSSSSSLLLLLLLLSSSGGGGGGGGGGGGGGGGGGGGSSSSSSSSybbby",
        "ybbbySSSggggggggggggggggggggSSSSSSSSLLLLLLLLSSGGGGGGGGGGGGGGGGGGGGGSSSSSSSSybbby",
        "ybYbySSSgggggggggggggggggggggSSSSSSoooLLLLLLLoGGGGGGGGGGGGGGGGGGSSSSSSSSSSSybYby",
        "ybbbySSSSGGGGGGGggggSSgSSgggggSSSSooooLLLLLLLooGGGGGGGGGGGGGGGSSSSSSSSSSSSSybbby",
        "ybbbySSSSSgggggggggSSSSSSSgggggSSSSoooooooooooKKKGGGGGGGGGGGSSSSSSSSSSSSSSSybbby",
        "ybbbySSSSSSSSSgSSSSSSSSSSSSgggggSSSYYYoooooYYYYYYYGGGGGGGSSSSSSSSSSSSSSSSSSybbby",
        "ybYbySSSSSSSSSSSSSSSSSSSSSSSgggggSYYYYYYYYYYYYYYYYYSGGGSSSSSSSSSSSSSSSSSSSSybYby",
        "ybbbySSSSSSSSSSSSSSSSSSSSSSSSgggSKKKKKKKKKKKKKKKKKKKSSSSSSSSSSSSSSSSSSSSSSSybbby",
        "ybbbySSSSSSSSSSSSSSSSSSSSSSSSSggYYYYYYYYYYYYYYYYYYYYYSSSSSSSSSSSSSSSSSSSSSSybbby",
        "ybbbySSSSSSSSSSSSSSSSSSSSSSSSSSSYYYYYYYYYYYYYYYYYYYYYSSSSSSSSSSSSSSSSSSSSSSybbby",
        "ybYbySSSSSSSSSSSSSSSSSSSSSSSSSSSKKKKKKKKKKKKKKKKKKKKKSSSSSSSSSSSSSSSSSSSSSSybYby",
        "ybbbySSSSSSSSSSSSSSSSSSSSSSSSSSYYYYYYYYYYYYYYYYYYYYYYYSSSSSSSSSSSSSSSSSSSSSybbby",
        "ybbbySSSSSSSSSSSSSSSSSSSSSSSSSSSYYYYYYYYYYYYYYYYYYYYggSSSSSSSSSSSSSSSSSSSSSybbby",
        "ybbbySSSSSSSSSSSSSSSSSSSSSSSSSSSKKKKKKKKKKKKKKKKKKKKgggggggSSSSSSSSSSSSSSSSybbby",
        "ybYbySSSSSSSSSSSSSSSSSSSSSSSSSSSYYYYYYYYYYYYYYYYYYYYggggggggggSSSSSSSSSSSSSybYby",
        "ybbbySSSSSSSSSSSSSSSSSSSSSSSSSSggYYYYYYYYYYYYYYYYYYYgggggggggggSSSSSSSSSSSSybbby",
        "ybbbySSSSSSSSSSSSSSSSSSSSSSSSgggggKKKKKKKKKKKKKKKKKSSSSSggggggggSSSSSSSSSSSybbby",
        "ybbbySSSSSScSSScccccSSSSSSSggggggggYYYYYYYYYYYYYYYSSSSSSSSSSSSSggSSSSSSSSSSybbby",
        "ybYbySSSSccccccccccccSSSSSggggggggggYgggYYYYYYgggSSSSSSSSSSSSSSSSSSSSSSSSSSybYby",
        "ybbbySSScccccccccccccSSSggggggggggggggggKKKKKKgggSSSSSSSSSSSScSSScccccSSSSSybbby",
        "ybbbySSScccccccccccccSSgggggggggggggggggSSSSSSgggSSSSSSSSSSccccccccccccSSSSybbby",
        "ybbbySSSScccccccccccSgggggggggggggggggggSSSSSSgggSSSSSSSSScccccccccccccSSSSybbby",
        "ybYbySSSSSSScccccSSgggggggggggggggggwSwSwSSSSwSwSwSSSSSSSScccccccccccccSSSSybYby",
        "ybbbySSSSSSSSSSSSSgggggggggggggggggSSSSSSSSSSSSSSSSSSSSSSSScccccccccccSSSSSybbby",
        "ybbbySSSSSSSSSSSggggggggggggggggggSSSSSSSSSSSSSSSSSSSSSSSSSSSScccccSSSSSSSSybbby",
        "ybbbySSSSSSSSSgggggggggggggggggggSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSybbby",
        "ybYbySSSSSSSSgggSSSgggggggSSggggSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSybYby",
        "ybbbyyyyyyyyyyyyyygggggyyyyyyggyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyybbby",
        "ybbbbbbbbbbbbbbbbgggbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbby",
        "ybYbbbYbbbYbbbYbgbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbYbbbby",
        "ybbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbby",
        "yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy",
    };
    public static readonly string[] SendellRows =
    {
        "yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy",
        "yBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBy",
        "yBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYYBy",
        "yBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBy",
        "yBBByyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnhhhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnhhhhhHHHhhhhhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnhhHHHHHHCHHHHHHhhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnhHHHqqqqqqqqqqCHHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnynnnnnnnnnnnnnnnnnnnnhHHqqqqqqqqqqqqqqCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnyYynnnnnnnnnnnnnnnnnnhHHqQQQqqqqqqqqqqqqCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnynnnnnnnnnnnnnnnnnnhHHQQQQQQQqqqqqqqqqQqCHHhnnnnnnnnnnnnnnnnnnynnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnhHHqQQQQQQQqqqqqqqqqqqqCHHhnnnnnnnnnnnnnnnnyYynnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnhHqqQQQQQQQqqqqqqqqqqqqqCHhnnnnnnnnnnnnnnnnnynnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnhHHqqqqQQQqqqqqqqqqqqqqqqCHHhnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnhHqqqQqqqqqqqqqqqqqqqqqqqqCHhnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnhhHqqqqqqqqqqqqqqqqqqqqqqqqCHhhnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnhHHHqqqqqqqqqqqqqqqqqqqqqqqqCHHHhnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnhHHqqqqqqqqqKqqqqqqqqqKqqqqqqqqCHHhnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnynhHqqqqqqqqqKKKqqqqqqqKKKqqqqqqqqCHhnnnynnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnyYyhHqqqqqqqqKwKKKqqqqqKwKKKqqqqqqqCHhnnyYynnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnynhHCqqqqqqqKKKKKqqqqqKKKKKqCqqqqCCHhnnnynnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnhHHCCqqqqqKKKKKqqqqqKKKKKqqCqCCCHHhnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnhHHHCqqqqqKKKqqqqqqqKKKqqCCCCHHHhnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnhhHHqqqqqqKqqqqqqqqqKqqqCCCHHhhnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnhHCqqqqqqqqqqqqqqqqqqqCCCHhnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnhHHCqqqqqqqqqqqqqqqqqCCCHHhnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnhHHCqqqqqqqqqqqqqqCCCCHHhnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnhHHCqqqqqqCCCCCCCCCCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnhHHCCqqqqqqqqqqCCCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnhHHHCCCCCCCCCCCHHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnhhHHHHHHCHHHHHHhhnnnnnnnnnnnnnnnnnnnnnnnnynnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnhhhHHqqqqCHHhnnnnnnnnnnnnnnnnnnnnnnnnnyYynnnyBYBy",
        "yBBBynnnnnynnnnnnnnnnnnnnnnnnnnnnnnnnnhHHqqqqqqCHHhnnnnnnnnnnnnnnnnnnnnnnnnnynnnnyBBBy",
        "yBBBynnnnyYynnnnnnnnnnnnnnnnnnnnnnnnnhHHqqqqqqqqCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnynnnnnnnnnnnnnnnnnnnnnnnnnhhHqqqqqqqqqqCHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnhHHHqqqqqqqqqqCHHhhhnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnhHHqqqqqqqqqqqqqCHHHHhhhnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnhhHHqqCCqqqqqqqqCCqqqCHHHHhhnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnhHHHqCCCHqqQqqqqqCHCCqqqqCHHHhhhnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnhhHHqqCCHHHqQQQqqqqCHHHCCCqqqCHHHHhhnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnhHHHqCCCHHhHqQQQqqqCCHhHHHHCCCqqqCHHHhhnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnhhHHqqCCHHHhhHqQQQqqqCHHhhhhHHHHCCCqqCHHHhhhnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnhHHHqCCCHHhhnhHqQQQCqqCHhnnnnhhhHHHHCCqqCHHHHhnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnhhHHqqCCHHHhnnnhHqQQCCCCCHhnnnnnnnhhhHHHCCqqqCHhnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnhhhHHHqqCCHHhhnnnhHHqqCCCCCHHhnnnnnnnnnnhhHHHCqqCHhnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnhHHHHqqCCCHHhnnnnnhHqqqqCCCCHhnnnnnnnnnnnnnhhHHCCCHhnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnhHqqqCCCHHHhnnnnnnhHqqqqqCCHHhnnnnnnnnnnnnnnnhHHHHHhnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnhHqqCCHHHhhnnnnnnnhHqqqqqCCHhnnnnnnnnnnnnnnnnnhhhhhnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnhHCCCHHhhnnnnnnnnnhHqqqqqCHHhnnnnnnnnnnnnnnnnnnynnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnhHHHHHhnnnnnnnnnnhHHqqqqCCHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnhhhhhnnnnnnnnnnnhHqqqqCCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnhHqqqqCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnhHHqqqCCHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnnhHqqqCCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnhHHqqCCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnnnhHqqqCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnynnnnnnnnnnnnnnnnhHHqqCCHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnynnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnyYynnnnnnnnnnnnnnnhHqqCCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnyYynnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnynnnnnnnnnnnnnnnhHHqCCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnynnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnnnhHCCCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnhHHCHHhnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnYYYhYYYhYYnnYYnnYYYnYnnnYnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnYnnnYhhnYnYnYnYnYnnnYnnnYnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBYBynnnnnnnnnnnnnnnnnnnnnnnnnYYYnYYnnYnYnYnYnYYnnYnnnYnnnnnnnnnnnnnnnnnnnnnnnnnnyBYBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnnnYnYnnnYnYnYnYnYnnnYnnnYnnnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBBynnnnnnnnnnnnnnnnnnnnnnnnnYYYnYYYnYnYnYYnnYYYnYYYnYYYnnnnnnnnnnnnnnnnnnnnnnnnyBBBy",
        "yBBByyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyBBBy",
        "yBYBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBYBy",
        "yBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBBYBBy",
        "yBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBy",
        "yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy",
    };
    // (a Jolly Roger for the lower room's wall, in place of LBA1's pirate flag -- its black came out brown in LBA2's colours, and the
    // user, 2026-11-09, "I've no idea what this decoration is supposed to be")
    public static readonly string[] FlagRows =
    {
        "KKKKKKKKKKKKKKKKKKKKKKKK",
        "KKKKKKKKKKKKKKKKKKKKKKKK",
        "KKKKKKKKKKWWWWKKKKKKKKKK",
        "KKKKKKKKWWWWWWWWKKKKKKKK",
        "KKKKKKKWWWWWWWWWWKKKKKKK",
        "KKKKKKKWWKKWWKKWWKKKKKKK",
        "KKKKKKKWWKKWWKKWWKKKKKKK",
        "KKKKKKKWWWWKKWWWWKKKKKKK",
        "KKKKKKKKWWWWWWWWKKKKKKKK",
        "KKKKKKKKKWKWKWKWKKKKKKKK",
        "KKKWWKKKKKWWWWKKKKKWWKKK",
        "KKKKWWWKKKKKKKKKWWWKKKKK",
        "KKKKKKWWWWKKKKWWWWKKKKKK",
        "KKKKKKKKKWWWWWWKKKKKKKKK",
        "KKKKKKWWWWKKKKWWWWKKKKKK",
        "KKKKWWWKKKKKKKKKKWWWKKKK",
    };
    // (the second Bayeux hanging, the right way up on the lower room's back wall: Zoe carried off by the clones, the lighthouse, Twinsen's
    // magic ball, the snowboard down the Hamalayi, the Book of Bu -- its letters BayeuxColours' and skin and gold)
    public static readonly string[] Bayeux2Rows =
    {
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
        "..r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....g.....r.k.k.y.....",
        ".r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g.....r...k.y.....g",
        "r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.",
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
        "............................................................................................................................................",
        "...........bbb.bbb.bbb...........bb..b.b..b..bb..b.b.bbb.........bb..bbb.b....b................bb..bbb.b.b.................bb..b.b..........",
        ".............b.b.b.b.............b.b.b.b.b.b.b.b.b.b.b...........b.b..b..b...b.b...............b.b..b..b.b.................b.b.b.b..........",
        "............b..b.b.bb............bb..bbb.bbb.bb..b.b.bbb.........bb...b..b...bbb...............b.b..b...b..................bb..b.b..........",
        "...........b...b.b.b.............b...b.b.b.b.b.b.b.b...b.........b....b..b...b.b...............b.b..b..b.b.................b.b.b.b..........",
        "...........bbb.bbb.bbb...........b...b.b.b.b.b.b.bbb.bbb.........b...bbb.bbb.b.b...............b.b.bbb.b.b.................bb..bbb..........",
        "..............yy...................................y.y......................................................................................",
        ".....llll....wwwy......llll.............kkkkkk...y..................................................................kkkkkkkkkkkkkkkkk.......",
        ".....llll....www.......llll..............yyyy.yyyyyyyy.......kkw.....y.y.........................kkw................kwwwwwwwkwwwwwwwk.......",
        ".....lklll.w.rrrr.w...lllkl......................y..........kwww...y.....y......................kwww................kwrwrwwwkwwrwrwwk.......",
        ".....lllllk.rrrrrr...klllll...............ww.......y.y.....k.www...b..................lll......k.www................kwwwwwwwkwwwwwwwk.......",
        "....yyyyyyk..rrrr....kyyyyyy.............rrrr...............bbbbbbb........Y..........llllll....bbbbb...............kwrrwrwwkwrwwrrwk.......",
        "....yyyyyyk..rrrr....kyyyyyy.............rrrr...............bbbbb............yy.......llllllwll.bbbbb...............kwwwwwwwkwwwwwwwk.......",
        "....yyyyyyk..w..w....kyyyyyy.............wwww...............bbbbb............yy.......llllwlllllbbbbb...............kwwrwrrwkwrrwrwwk.......",
        "....yyyyyyk..........kyyyyyy............wwwwww..............bbbbb.............ll......llllllllyyyybbb...............kkkkkkkkkkkkkkkkk.......",
        "....kkkkkkk..........kkkkkkk............rrrrrr..............bbbbb.............llyy....lllllwllllbbyyyyll....................kk..............",
        ".....k..k.k..........k.k..k.............rrrrrr...............k.k...............yyy....lllwlllllkllllklyylll.................kk..............",
        ".....k..k.k..........k.k..k............kkkkkkkk..............k.k................kk....lllllllllklllllkllllllll..............kk..............",
        ".....k..k..............k..k...........kkkkkkkkkk.............k.k......................llllllllllklllllklllllllll.........kkkkkkkk...........",
        "gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg",
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
        "...r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y.....g.....r.....y....",
        "..r.....y...yyy.....r.....y.....g...yyy.....y.....g.....r...yyy.....g.....r.....y...yyy.....r.....y.....g...yyy.....y.....g.....r...yyy.....",
        ".r.....y....y.y....r.....y.....g....y.y....y.....g.....r....y.y....g.....r.....y....y.y....r.....y.....g....y.y....y.....g.....r....y.y....g",
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
    };

    // A picture's squares as rectangles: each square's colour (at), as few rectangles of one colour as cover columns [c0, c1) and rows
    // [r0, r1), each from its corners (point: a column's and a row's edge, in squares -- grown by `grow` squares into its neighbours),
    // facing away from `inside`. The rectangles made.
    internal static int Stitched(CarMesh m, int c0, int c1, int r0, int r1, Func<int, int, char> at, Func<float, float, int> point,
                                 IReadOnlyDictionary<char, int> colours, Vector3 inside, float grow = 0)
    {
        var done = new bool[c1 - c0, r1 - r0];
        var count = 0;
        for (var r = r0; r < r1; r++)
            for (var c = c0; c < c1; c++)
            {
                if (done[c - c0, r - r0]) continue;
                var colour = at(c, r);
                var w = 1;
                while (c + w < c1 && !done[c + w - c0, r - r0] && at(c + w, r) == colour) w++;
                var h = 1;
                while (r + h < r1 && Enumerable.Range(c, w).All(i => !done[i - c0, r + h - r0] && at(i, r + h) == colour)) h++;
                for (var j = r; j < r + h; j++) for (var i = c; i < c + w; i++) done[i - c0, j - r0] = true;
                // (grown only where a neighbour lies: the picture's own edges stay where they are)
                float gl = c > 0 ? grow : 0, gt = r > 0 ? grow : 0, gr = grow, gb = grow;
                m.Out(new[] { point(c - gl, r - gt), point(c + w + gr, r - gt), point(c + w + gr, r + h + gb), point(c - gl, r + h + gb) }, colours[colour], inside, unlit: true);
                count++;
            }
        return count;
    }
    // (a shared corner, where nothing is grown; a corner of its own where it is)
    private static Func<float, float, int> Corners(CarMesh m, Func<float, float, Vector3> at)
    {
        var shared = new Dictionary<(float, float), int>();
        return (c, r) => shared.TryGetValue((c, r), out var h) ? h : shared[(c, r)] = m.P(0, at(c, r));
    }

    // A picture's pieces: bands of `band` rows (all of them: 0), each into column ranges as wide as keeps a body under the engine's limits
    // (550 points and polygons, kept well under)
    public static List<(int C0, int C1, int R0, int R1)> PictureTiles(string[] rows, int band)
    {
        var tiles = new List<(int, int, int, int)>();
        int width = rows[0].Length, height = rows.Length;
        if (band <= 0) band = height;
        for (var r0 = 0; r0 < height; r0 += band)
        {
            var r1 = Math.Min(height, r0 + band);
            (int Faces, int Points) Count(int a, int b)
            {
                var m = new CarMesh("count", new[] { -1 });
                var faces = Stitched(m, a, b, r0, r1, (c, r) => rows[r][c], Corners(m, (c, r) => new Vector3(c, 0, r)), RugColours, new Vector3(0, -100, 0), Grow);
                return (faces, m.ToBody(new byte[0]).Vertices.Count);
            }
            var c0 = 0;
            while (c0 < width)
            {
                var c1 = c0 + 1;
                while (c1 < width) { var (f, p) = Count(c0, c1 + 1); if (f > 420 || p > 430) break; c1++; }
                tiles.Add((c0, c1, r0, r1));
                c0 = c1;
            }
        }
        return tiles;
    }
    public static readonly List<(int C0, int C1, int R0, int R1)> DinoTiles = PictureTiles(DinoRows, RugBand), SendellTiles = PictureTiles(SendellRows, RugBand),
        Bayeux2Parts = PictureTiles(Bayeux2Rows, 0);

    // A rug's tile: its squares, its near corner (its least column and row) at 0, lying at 0
    private static Body BuildRugTile(byte[] header, string name, string[] rows, int stitch, (int C0, int C1, int R0, int R1) t)
    {
        var m = new CarMesh(name, new[] { -1 });
        Stitched(m, t.C0, t.C1, t.R0, t.R1, (c, r) => rows[r][c], Corners(m, (c, r) => new Vector3((c - t.C0) * stitch, 0, (r - t.R0) * stitch)),
                 RugColours, new Vector3(0, -100, 0), Grow);
        return m.ToBody(header);
    }
    // where a tile's actor stands, from the rug's middle
    public static (int X, int Z) RugTileAt(string[] rows, int stitch, (int C0, int C1, int R0, int R1) t) =>
        ((int)MathF.Round((t.C0 - rows[0].Length / 2f) * stitch), (int)MathF.Round((t.R0 - rows.Length / 2f) * stitch));

    // The Jolly Roger hung on a wall: its cloth (FlagRows, a stitch FlagStitch square, flat colours) from its top at 0 down, its face +z, on
    // a dark rod with knobbed ends
    public const int JollyRoger = 60, FlagStitch = 36;
    public static Body BuildJollyRoger(byte[] header)
    {
        var m = new CarMesh("a Jolly Roger", new[] { -1 });
        int w = FlagRows[0].Length, h = FlagRows.Length;
        Stitched(m, 0, w, 0, h, (c, r) => FlagRows[r][c], Corners(m, (c, r) => new Vector3((c - w / 2f) * FlagStitch, -r * FlagStitch, 8)),
                 RugColours, new Vector3(0, 0, -100), Grow);
        m.Light = 0.7f;
        m.Box(0, new Vector3(0, 10, 14), new Vector3(w * FlagStitch + 100, 22, 22), Dark);
        foreach (var s in new[] { -1, 1 }) m.Box(0, new Vector3(s * (w * FlagStitch / 2f + 60), 10, 14), new Vector3(36, 36, 36), Gold);
        return m.ToBody(header);
    }

    // The second Bayeux hanging, upright on a wall: a piece of its columns, its top at 0, its middle across at 0, its face +z; a dark rod
    // along its top
    public const int Bayeux2First = 80, Bayeux2Most = 10, Bayeux2Stitch = 28;
    private static Body BuildBayeux2(byte[] header, (int C0, int C1, int R0, int R1) t)
    {
        var m = new CarMesh("the second Bayeux hanging", new[] { -1 });
        var mid = (t.C0 + t.C1) / 2f;
        var colours = new Dictionary<char, int>(BayeuxColours) { ['s'] = 41, ['Y'] = 108 };
        Stitched(m, t.C0, t.C1, t.R0, t.R1, (c, r) => Bayeux2Rows[r][c], Corners(m, (c, r) => new Vector3((c - mid) * Bayeux2Stitch, -r * Bayeux2Stitch, 8)),
                 colours, new Vector3(0, 0, -100), Grow);
        m.Light = 0.7f;
        m.Box(0, new Vector3(0, 8, 14), new Vector3((t.C1 - t.C0) * Bayeux2Stitch + 4, 20, 20), Wood);
        return m.ToBody(header);
    }
    // where a piece's actor stands along the wall, from the hanging's middle
    public static int Bayeux2X((int C0, int C1, int R0, int R1) t) => (int)MathF.Round(((t.C0 + t.C1) / 2f - Bayeux2Rows[0].Length / 2f) * Bayeux2Stitch);

    public static IEnumerable<(int Generic, string Name, Func<byte[], Body> Build)> PictureBodies() =>
        DinoTiles.Take(DinoMost).Select((t, k) => (DinoFirst + k, $"the Dinofly rug, tile {k + 1}", (Func<byte[], Body>)(h => BuildRugTile(h, "the Dinofly rug", DinoRows, DinoStitch, t))))
        .Concat(SendellTiles.Take(SendellMost).Select((t, k) => (SendellFirst + k, $"the Sendell rug, tile {k + 1}", (Func<byte[], Body>)(h => BuildRugTile(h, "the Sendell rug", SendellRows, SendellStitch, t)))))
        .Concat(Bayeux2Parts.Take(Bayeux2Most).Select((t, k) => (Bayeux2First + k, $"the second Bayeux hanging, part {k + 1}", (Func<byte[], Body>)(h => BuildBayeux2(h, t)))));
}
