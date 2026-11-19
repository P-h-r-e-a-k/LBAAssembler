using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using System.Text;
using LbaBodyStudio;
using LBAAssembler.Terrain.Palace;

namespace LBAAssembler.Terrain.Citadel;

// The hoverboard -- the user, 2026-11-18: "Let's redesign the protopack and jetpack as a hoverboard. On Twinsun the hoverboard should have
// the power of the protopack and everywhere else it should have the power of the jetpack." Its power is the engine's (RACEMOD.CPP
// RaceMod_HoverPack: the protopack item hovers on Twinsun and flies as the super jet-pack everywhere else); its look is this, into the
// story's game folder before the overalls (CitadelOveralls recolours these bodies' tunic, colour 192, which the board doesn't use):
//   - Twinsen with the protopack and with the super jet-pack (BODY.HQR 16, entity 4's body; 21, entity 8's): the pack gone from his back
//     -- its box, its straps, its handlebars round to his front: every polygon of its colours on his chest bone (its points stay, unused)
//     -- and a board under his feet on his hip bone (bone 1: the one the protopack's animations lift as he hovers, which turns with him
//     and never tilts, so the board rises and falls with him and stays level). The same board in both: one Hoverboard, never a super one
//     (the user, 2026-11-19: "We should call the Hoverboard hoverboard everywhere and never super hoverboard. Less gravity will be our
//     explanation for its additional power") -- it flies off Twinsun because the gravity's lighter there, not for a tune-up of Baldino's
//     (he stays on Twinsun: RaceTrackStory.Otringal.cs);
//   - the packs' own animations (ANIM.HQR 104-110, 112, 113, 160-162, 164-166: entities 4's and 8's alone use them, every other entity's
//     checked): his legs under him, the knees a little bent, his feet flat on the board -- the lean his hips had moved up to his chest,
//     so he leans as before -- and his hips lifted: the board hovering over the ground even as he stands still, higher as he goes;
//   - the inventory's model (OBJFIX.HQR 12, the protopack's): the board on its tail, its deck to the front. Not 48, the super jet-pack's:
//     with Baldino's upgrade gone the item never shows it, and the races' super jet-pack power-up is a copy of it (RaceTrackSuperJet: from
//     LBRA 2026.11.19.1723 to 1757 the car turned into a gold board, not the jet-pack);
//   - the texts that named the packs: the item's (file 2's 12, 112, 212, and the upgrade's 48, 148, 248 the same words), the behaviours'
//     names (file 0's 84 and 88), the J key's (file 0's 133), the gazogem can's (file 2's 15 and 215: "your new super jet-pack").
// A second pass leaves a body with its board as it is (its hip bone has more than its one point) and the animations as they are (the
// standing one's hips already lifted).
internal static class CitadelHoverboard
{
    private const int ProtoBody = 16, JetBody = 21, Hip = 1, Pelvis = 2, Chest = 3;
    // (the pack's colours on his chest: its frame and box 208 and 64, its grips 112 and 16, the jet-pack's flames 96 and 224 -- the tunic
    // there is 192, 32 and 80)
    private static readonly HashSet<int> PackColours = new() { 16, 64, 96, 112, 208, 224 };
    // (his back's hole where the pack was, its polygons by the points round it as the body stands -- the same in both bodies: from the
    // waist's right (166, 566, 0) up his right side, over his right shoulder blade, down the right of the strip on his spine, under it,
    // up its left, over his left shoulder blade, down to the waist's left and along the waist's back, 533 up, to the right again)
    private const int TunicColour = 192;
    private static readonly Vector3[][] BackHole =
    {
        new Vector3[] { new(133, 699, -67), new(133, 899, -53), new(33, 899, -60), new(33, 665, -67) },
        new Vector3[] { new(166, 566, 0), new(133, 699, -67), new(33, 665, -67) },
        new Vector3[] { new(166, 566, 0), new(33, 665, -67), new(133, 533, -33) },
        new Vector3[] { new(33, 665, -67), new(0, 533, -100), new(133, 533, -33) },
        new Vector3[] { new(33, 665, -67), new(-33, 665, -67), new(0, 533, -100) },
        new Vector3[] { new(-33, 665, -67), new(-133, 533, -33), new(0, 533, -100) },
        new Vector3[] { new(-166, 566, 0), new(-33, 665, -67), new(-133, 533, -33) },
        new Vector3[] { new(-166, 566, 0), new(-133, 699, -67), new(-33, 665, -67) },
        new Vector3[] { new(-133, 699, -67), new(-133, 899, -53), new(-33, 899, -60), new(-33, 665, -67) },
    };
    // (the board's normals as long as most of Twinsen's: 16,384, the light's whole ramp)
    private const float NormalLength = 16384;
    // (its deck's top under his soles as he stands, in his body's units)
    private const float DeckY = -6;

    // ---- the board: its own frame, x across it, y up, z along it, its nose +z, its deck's top at 0 -- rows from its tail to its nose:
    // where along it, its half-width there, how far its deck rises (the kick of its tail and its nose)
    private static readonly (float Z, float Half, float Rise)[] Rows =
        { (-470, 80, 52), (-400, 165, 18), (-270, 190, 0), (250, 190, 0), (420, 165, 22), (520, 80, 62) };
    private const float Thick = 40, StripeHalf = 42, PadHalf = 120;
    private static readonly (float From, float To)[] Pads = { (-250, -100), (100, 240) };

    // its colours (the islands' palettes' rows -- Citadel's, RESS.HQR 27 -- each lit polygon its colour and as many steps more as its light
    // gives, about five): the deck the pack's metal (208, the blue-greys), the rails red (69), the underside dark (224), a grey stripe
    // down the deck (50: facing the light, a colour high in its row runs past it in OpenGL -- the ferry's roof); the glow under it unlit,
    // as it is (a light cyan, 174)
    internal readonly record struct Look(int Deck, int Rail, int Under, int Stripe, int Glow);
    internal static readonly Look Plain = new(208, 69, 224, 50, 174);

    // Its polygons: each quad's corners (wound to face out: the side it faces given -- its middle's way from the board's middle won't do
    // at the kicked ends), its colour, lit or not.
    internal static List<(Vector3[] Corners, int Colour, bool Lit)> Faces(Look look)
    {
        var faces = new List<(Vector3[], int, bool)>();
        void Add(Vector3[] q, int colour, bool lit, Vector3 facing) =>
            faces.Add((Vector3.Dot(PalaceTorturers.Newell(q), facing) < 0 ? q.Reverse().ToArray() : q, colour, lit));
        Vector3 Top(int r, float side) => new(side * Rows[r].Half, Rows[r].Rise, Rows[r].Z);
        Vector3 Under(int r, float side) => Top(r, side) - new Vector3(0, Thick, 0);
        Vector3 Stripe(int r, float side) => new(side * StripeHalf, Rows[r].Rise + 3, Rows[r].Z);
        for (var r = 0; r + 1 < Rows.Length; r++)
        {
            Add(new[] { Top(r, -1), Top(r, 1), Top(r + 1, 1), Top(r + 1, -1) }, look.Deck, true, Vector3.UnitY);
            Add(new[] { Under(r, -1), Under(r, 1), Under(r + 1, 1), Under(r + 1, -1) }, look.Under, true, -Vector3.UnitY);
            foreach (var side in new[] { -1f, 1f })
                Add(new[] { Top(r, side), Top(r + 1, side), Under(r + 1, side), Under(r, side) }, look.Rail, true, new Vector3(side, 0, 0));
        }
        Add(new[] { Top(0, -1), Top(0, 1), Under(0, 1), Under(0, -1) }, look.Rail, true, -Vector3.UnitZ);
        var nose = Rows.Length - 1;
        Add(new[] { Top(nose, -1), Top(nose, 1), Under(nose, 1), Under(nose, -1) }, look.Rail, true, Vector3.UnitZ);
        // (the stripe a hair over the deck, from the tail's kick to the nose's)
        for (var r = 1; r + 2 < Rows.Length; r++)
            Add(new[] { Stripe(r, -1), Stripe(r, 1), Stripe(r + 1, 1), Stripe(r + 1, -1) }, look.Stripe, true, Vector3.UnitY);
        // (the glow: two pads under its flat middle)
        foreach (var (from, to) in Pads)
        {
            var y = -Thick - 3;
            Add(new[] { new Vector3(-PadHalf, y, from), new Vector3(PadHalf, y, from), new Vector3(PadHalf, y, to), new Vector3(-PadHalf, y, to) },
                look.Glow, false, -Vector3.UnitY);
        }
        return faces;
    }

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        // ---- his bodies
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var bodies = HqrArchive.Open(bodyPath);
        foreach (var entry in new[] { ProtoBody, JetBody })
        {
            var made = Rider(bodies.Read(entry), Plain, out var note);
            if (made is not null) HqrWriter.ReplaceInFile(bodyPath, entry, made);
            log.Add($"the hoverboard: BODY.HQR {entry} ({(entry == ProtoBody ? "the protopack's" : "the super jet-pack's")}) {note}");
        }
        // ---- the packs' animations
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var anims = HqrArchive.Open(animPath);
        if (Hips(anims.Read(RiderAnims[0])) == Lift) log.Add("the hoverboard: the packs' animations already the rider's");
        else
        {
            var changed = new List<int>();
            foreach (var a in RiderAnims)
            {
                var bytes = Riding(anims.Read(a));
                if (bytes is null) continue;
                HqrWriter.ReplaceInFile(animPath, a, bytes);
                changed.Add(a);
            }
            log.Add($"the hoverboard: ANIM.HQR {string.Join(", ", changed)} the rider's (his legs under him, his lean in his chest, his hips {Lift} up)");
        }
        // ---- the inventory's model (the protopack's; the super jet-pack's, 48, left the game's: the race's power-up copies it)
        var objfixPath = Path.Combine(gameDirectory, "OBJFIX.HQR");
        var header = Body.Read(bodies.Read(30), 2, allowStatic: true).Header;
        HqrWriter.ReplaceInFile(objfixPath, ProtoSlot, CitadelOveralls.InventorySized(Model(header, Plain)).Write());
        log.Add($"the hoverboard: OBJFIX.HQR {ProtoSlot} (the protopack's model) the board");
        // ---- the texts
        log.Add(InstallTexts(gameDirectory));
        return log;
    }

    // ---- his bodies

    // The game's body with the pack (its bytes) made the rider's: the pack's polygons gone, the board's on his hip bone, its deck under his
    // soles. Null (and why) if the board is on it already.
    internal static byte[]? Rider(byte[] game, Look look, out string note)
    {
        var p = PalaceTorturers.Parts.Read(game);
        if (p.Bones.Count != 19 || p.Points.Count != p.Normals.Count) throw new InvalidDataException("not Twinsen with a pack: 19 bones, a normal a point");
        if (p.Bones[Hip].Count > 1) { note = "has its board already"; return null; }
        var world = p.World();
        int chest = p.StartOf(Chest), chestEnd = chest + p.Bones[Chest].Count;
        static int U(byte[] r, int at) => BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(at));
        static void W(byte[] r, int at, int v) => BinaryPrimitives.WriteUInt16LittleEndian(r.AsSpan(at), (ushort)v);

        // the pack's polygons gone: its colours with a point on his chest -- and its left handlebar's end, which the game's body has in the
        // tunic's colour (every point of it out past his front and his side)
        bool Handlebar(int i) => i >= chest && i < chestEnd && world[i].Z > 140 && Math.Abs(world[i].X) > 150;
        var gone = 0;
        foreach (var (type, records) in p.Blocks)
        {
            var corners = (type & 0x8000) != 0 ? 4 : 3;
            gone += records.RemoveAll(r => PackColours.Contains(r[8]) && Enumerable.Range(0, corners).Any(k => U(r, k * 2) is var i && i >= chest && i < chestEnd)
                                           || Enumerable.Range(0, corners).All(k => Handlebar(U(r, k * 2))));
        }
        if (gone == 0) throw new InvalidDataException("no pack on his chest");

        // his back closed where the pack was -- the game's body has no tunic there: from his shoulder blades to his waist either side of the
        // strip down his spine, and under it -- its polygons on the tunic's own points round the hole, in its colour, lit as it is
        int Back(Vector3 at)
        {
            for (var i = chest; i < chestEnd; i++) if (Vector3.DistanceSquared(world[i], at) < 4) return i;
            throw new InvalidDataException($"his back's hole: no point of his chest at {at}");
        }
        var hole = new List<int[]>();
        foreach (var poly in BackHole) hole.Add(poly.Select(Back).ToArray());
        foreach (var size in new[] { 3, 4 })
        {
            var records = new List<byte[]>();
            foreach (var poly in hole.Where(h => h.Length == size))
            {
                // (facing out of his back: -z)
                var q = PalaceTorturers.Newell(poly.Select(i => world[i]).ToArray()).Z > 0 ? poly.Reverse().ToArray() : poly;
                var r = new byte[12];
                for (var c = 0; c < q.Length; c++) W(r, c * 2, q[c]);
                W(r, 8, TunicColour);
                records.Add(r);
            }
            p.Blocks.Add(((size == 4 ? 0x8000 : 0) | 4, records));
        }

        // the board's points after the hip's own -- each polygon its own corners, so its light is its face's, its edges sharp -- every later
        // point's index (and every light's: the points' and then the faces', ListLights) moved along by as many
        var faces = Faces(look);
        var at = p.StartOf(Hip) + p.Bones[Hip].Count;
        var k = faces.Sum(f => f.Corners.Length);
        int Moved(int i) => i >= at ? i + k : i;
        var origin = world[p.Bones[Hip].Pivot];
        var under = new Vector3(0, DeckY, 0);
        static short S(float v) => checked((short)Math.Round(v));
        var points = new List<(short, short, short, int)>();
        var normals = new List<(short, short, short, int)>();
        var lit = new List<byte[]>();
        var unlit = new List<byte[]>();
        foreach (var (corners, colour, isLit) in faces)
        {
            var n = Vector3.Normalize(PalaceTorturers.Newell(corners)) * NormalLength;
            var r = new byte[12];
            for (var c = 0; c < corners.Length; c++)
            {
                var q = corners[c] + under - origin;
                W(r, c * 2, at + points.Count);
                points.Add((S(q.X), S(q.Y), S(q.Z), Hip));
                normals.Add(isLit ? (S(n.X), S(n.Y), S(n.Z), Hip) : ((short)0, (short)0, (short)0, Hip));
            }
            W(r, 8, colour);
            (isLit ? lit : unlit).Add(r);
        }
        for (var b = 0; b < p.Bones.Count; b++)
        {
            var bone = p.Bones[b];
            p.Bones[b] = bone with { Pivot = Moved(bone.Pivot), Count = b == Hip ? bone.Count + k : bone.Count };
        }
        // (every kind of polygon -- plain, lit, textured, environment-mapped -- its points first, its light at 10: AFF_OBJ.CPP STRUC_POLY*)
        foreach (var (type, records) in p.Blocks)
        {
            var corners = (type & 0x8000) != 0 ? 4 : 3;
            foreach (var r in records)
            {
                for (var c = 0; c < corners; c++) W(r, c * 2, Moved(U(r, c * 2)));
                W(r, 10, Moved(U(r, 10)));
            }
        }
        for (var i = 0; i < p.LineCount; i++) { W(p.Lines, i * 8 + 4, Moved(U(p.Lines, i * 8 + 4))); W(p.Lines, i * 8 + 6, Moved(U(p.Lines, i * 8 + 6))); }
        for (var i = 0; i < p.SphereCount; i++) W(p.Spheres, i * 8 + 4, Moved(U(p.Spheres, i * 8 + 4)));
        p.Points.InsertRange(at, points);
        p.Normals.InsertRange(at, normals);
        p.Blocks.Add((0x8000 | 4, lit));
        p.Blocks.Add((0x8000 | 0, unlit));
        // (its box, as it stands: the board's below and before and behind him)
        var all = p.World();
        int[] box = { (int)Math.Floor(all.Min(v => v.X)), (int)Math.Ceiling(all.Max(v => v.X)), (int)Math.Floor(all.Min(v => v.Y)), (int)Math.Ceiling(all.Max(v => v.Y)),
                      (int)Math.Floor(all.Min(v => v.Z)), (int)Math.Ceiling(all.Max(v => v.Z)) };
        for (var b = 0; b < 6; b++) BinaryPrimitives.WriteInt32LittleEndian(p.Header.AsSpan(8 + b * 4), box[b]);
        note = $"the pack off ({gone} polygons), the board on his hip bone ({faces.Count} polygons, {k} points)";
        return p.Write();
    }

    // ---- the animations

    // the protopack's (entity 4's: standing, forward, back, the turns, the turns going, the fall, the landing) and the super jet-pack's
    // (entity 8's own: flying, its turns, its fall, its landing, its 32nd) -- not the hits, the deaths, the drowning (111, 114-116, 163):
    // he comes off the board
    private static readonly int[] RiderAnims = { 104, 105, 106, 107, 108, 109, 110, 112, 113, 160, 161, 162, 164, 165, 166 };
    private const int Thigh = 8, OtherThigh = 11;
    // (his hips over the ground: the standing's, and a third of what the pack's own lift them as he goes -- a fall's landing all but down)
    private const int Lift = 150;
    private static int Angle(double degrees) => (int)Math.Round(degrees * 4096 / 360);
    // (a rider's legs: the thighs a little forward, the knees as much back, the feet flat; and how far his lean goes to his chest -- the
    // landings fold him at the hips past a right angle)
    private static readonly int StanceThigh = Angle(-14), StanceKnee = Angle(28), StanceFoot = Angle(-14), MostLean = Angle(50);

    private static int Hips(byte[] anim)
    {
        int bones = BinaryPrimitives.ReadUInt16LittleEndian(anim.AsSpan(2));
        var hip = 8 + 8 + Hip * 8;
        return bones > Hip && (BinaryPrimitives.ReadInt16LittleEndian(anim.AsSpan(hip)) & 1) == 1 ? BinaryPrimitives.ReadInt16LittleEndian(anim.AsSpan(hip + 4)) : int.MinValue;
    }

    // An animation of the pack's made the rider's (null if it isn't one of Twinsen's 19 bones)
    internal static byte[]? Riding(byte[] game)
    {
        var a = (byte[])game.Clone();
        int frames = BinaryPrimitives.ReadUInt16LittleEndian(a), bones = BinaryPrimitives.ReadUInt16LittleEndian(a.AsSpan(2));
        if (bones != 19) return null;
        short Get(int at) => BinaryPrimitives.ReadInt16LittleEndian(a.AsSpan(at));
        void Set(int at, int v) => BinaryPrimitives.WriteInt16LittleEndian(a.AsSpan(at), (short)v);
        static int Wrap(int v) => ((v & 0xFFF) ^ 0x800) - 0x800;
        for (var f = 0; f < frames; f++)
        {
            int Bone(int b) => 8 + f * (8 + bones * 8) + 8 + b * 8;
            // the hips: lifted (a turn of them -- the protopack's first turning one has its hips a turn, not a lift -- given to the pelvis)
            var hip = Bone(Hip);
            var turn = 0;
            if ((Get(hip) & 1) == 1) Set(hip + 4, Lift + Get(hip + 4) / 3);
            else { turn = Get(hip + 4); Set(hip, 1); Set(hip + 2, 0); Set(hip + 4, Lift + 15); Set(hip + 6, 0); }
            // the pelvis's lean (forward or back, and to a side) to the chest, its twist kept -- his legs his own from the hips down
            int pelvis = Bone(Pelvis), chest = Bone(Chest);
            if ((Get(pelvis) & 1) == 0 && (Get(chest) & 1) == 0)
            {
                var lean = Math.Clamp(Wrap(Get(pelvis + 2)), -MostLean, MostLean);
                var roll = Math.Clamp(Wrap(Get(pelvis + 6)), -MostLean, MostLean);
                Set(chest + 2, Wrap(Get(chest + 2) + lean));
                Set(chest + 6, Wrap(Get(chest + 6) + roll));
                Set(pelvis + 2, 0);
                Set(pelvis + 4, Wrap(Get(pelvis + 4) + turn));
                Set(pelvis + 6, 0);
            }
            // the legs: a rider's stance
            foreach (var top in new[] { Thigh, OtherThigh })
                for (var j = 0; j < 3; j++)
                {
                    var leg = Bone(top + j);
                    Set(leg, 0);
                    Set(leg + 2, j == 0 ? StanceThigh : j == 1 ? StanceKnee : StanceFoot);
                    Set(leg + 4, 0);
                    Set(leg + 6, 0);
                }
        }
        return a;
    }

    // ---- the inventory's models

    public const int ProtoSlot = 12, SuperSlot = 48;

    // The board on its tail, its deck to the front (+z, the side the inventory shows first), turned to show a rail and leaning back a
    // little, in the light's whole ramp (flat on and at 0.6 its dark deck was a grey shape in a black box)
    internal static Body Model(byte[] header, Look look)
    {
        var m = new CarMesh("the hoverboard", new[] { -1 });
        m.Light = 1f;
        var back = Matrix4x4.CreateRotationY(0.6f) * Matrix4x4.CreateRotationX(-0.3f);
        // (the board's frame to the model's: x to -x, y -- its deck's way -- to +z, z -- its nose -- to +y: a turn, not a mirror)
        Vector3 Stood(Vector3 v) => Vector3.Transform(new Vector3(-v.X, v.Z, v.Y), back);
        foreach (var (corners, colour, lit) in Faces(look))
        {
            var hs = corners.Select(c => m.P(0, Stood(c))).ToArray();
            m.Raw(hs, colour, unlit: !lit);
        }
        return m.ToBody(header);
    }

    // ---- the texts

    private const int SystemTexts = 0, InventoryTexts = 2;
    // (the behaviours' names, 80 + the behaviour: the protopack's 4, the super jet-pack's 8; the J key's, in the keys' list)
    private const int ProtoName = 84, JetName = 88, JetKey = 133, GazogemCan = 15;
    private static readonly string[] Name = { "Hoverboard", "Hoverboard", "Hoverboard", "Hoverboard", "Hoverboard", "Hoverboard" };
    private static readonly string[] Found =
    {
        "You just found the Hoverboard! Step on it and you'll glide a few inches above the ground.",
        "Tu viens de trouver l'Hoverboard ! Monte dessus et tu glisseras à quelques centimètres du sol.",
        "Du hast das Hoverboard gefunden! Stell dich drauf, und du gleitest ein paar Zentimeter über dem Boden.",
        "¡Acabas de encontrar el Hoverboard! Súbete y te deslizarás a unos centímetros del suelo.",
        "Hai appena trovato l'Hoverboard! Salici sopra e scivolerai a pochi centimetri da terra.",
        "Acabaste de encontrar o Hoverboard! Sobe para ele e vais deslizar a uns centímetros do chão.",
    };
    private static readonly string[] About =
    {
        "Designed by Jerome Baldino. On Twinsun it glides just above the ground: to cross water, ride onto it low and level, or you'll sink with your board. Anywhere else the gravity is lighter, and it flies.",
        "Conçu par Jérôme Baldino. Sur Twinsun, il glisse juste au-dessus du sol : pour passer sur l'eau, abordes-la bas et à plat, sinon tu couleras avec ta planche. Partout ailleurs, la gravité est plus faible, et il vole.",
        "Von Jerome Baldino entworfen. Auf Twinsun gleitet es knapp über dem Boden: um über Wasser zu kommen, fahr flach und tief darauf zu, sonst gehst du mit deinem Brett unter. Überall sonst ist die Schwerkraft geringer, und es fliegt.",
        "Diseñado por Jerome Baldino. En Twinsun se desliza justo por encima del suelo: para cruzar el agua, entra en ella bajo y en horizontal, o te hundirás con tu tabla. En cualquier otro sitio la gravedad es menor, y vuela.",
        "Progettato da Jerome Baldino. Su Twinsun scivola appena sopra il suolo: per passare sull'acqua, entraci basso e in piano, o affonderai con la tua tavola. Ovunque altro la gravità è più debole, e vola.",
        "Desenhado por Jerome Baldino. Em Twinsun desliza mesmo rente ao chão: para atravessar a água, entra nela baixo e a direito, ou afundas com a tua prancha. Em qualquer outro lado a gravidade é mais fraca, e voa.",
    };
    private static readonly (int File, int Id, string[] Words)[] Texts =
    {
        (InventoryTexts, ProtoSlot, Found),
        (InventoryTexts, ProtoSlot + 100, Name),
        (InventoryTexts, ProtoSlot + 200, About),
        // (the game's upgrade's -- Baldino's, gone: the same board, the same words)
        (InventoryTexts, SuperSlot, Found),
        (InventoryTexts, SuperSlot + 100, Name),
        (InventoryTexts, SuperSlot + 200, About),
        (SystemTexts, ProtoName, Name),
        (SystemTexts, JetName, Name),
        (SystemTexts, JetKey, new[] { "Hoverboard On/Off", "Hoverboard On/Off", "Hoverboard Ein/Aus", "Hoverboard Encendido/Apagado", "Hoverboard On/Off", "Hoverboard ATIVADO/DESATIVADO" }),
        // (the can of gazogem: the Island of the Francos' slick gives it -- RaceTrackStory.Francos.cs -- and nobody turns the pack into
        // anything with it now)
        (InventoryTexts, GazogemCan, new[]
        {
            "You just found a can of Gazogem, the most powerful fuel on Zeelich.",
            "Tu viens de trouver un bidon de Gazogem, le carburant le plus puissant de Zeelich.",
            "Du hast einen Behälter mit GazoGem gefunden, dem stärksten Treibstoff auf Zeelich.",
            "Acabas de encontrar un bidón de Gazogem, el carburante más potente de Zeelich.",
            "Hai trovato un bidone di gazogem, il carburante più potente di Zeelich.",
            "Você encontrou um galão de gasógeno, o combustível mais potente de Zeelich.",
        }),
        (InventoryTexts, GazogemCan + 200, new[]
        {
            "A can of gazogem, the most powerful fuel on Zeelich. It's refined from the gems of the Wannies' mine.",
            "Un bidon de gazogem, le carburant le plus puissant de Zeelich. On le raffine à partir des gemmes de la mine des Wannies.",
            "Ein Behälter mit GazoGem, dem stärksten Treibstoff auf Zeelich. Er wird aus den Edelsteinen der Mine der Wannies raffiniert.",
            "Un bidón de gazogem, el carburante más potente de Zeelich. Se refina a partir de las gemas de la mina de los Wannies.",
            "Un bidone di gazogem, il carburante più potente di Zeelich. Viene raffinato dalle gemme della miniera dei Wannies.",
            "Um galão de gasógeno, o combustível mais potente de Zeelich. Ele é refinado a partir das gemas da mina dos Wannies.",
        }),
    };

    private static string InstallTexts(string gameDirectory)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var dos = Encoding.GetEncoding(850);
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        var hqr = File.ReadAllBytes(textPath);
        var written = 0;
        for (var lang = 0; lang < languages; lang++)
            foreach (var file in Texts.Select(t => t.File).Distinct())
            {
                var bank = Lba2TextBank.Load(text, lang, file);
                foreach (var (_, id, words) in Texts.Where(t => t.File == file))
                {
                    var wanted = dos.GetBytes(words[lang < words.Length ? lang : 0]);
                    var have = bank.Find(id);
                    if (have is null) bank.Texts.Add(new Lba2TextBank.Text { Id = id, Attribute = Lba2TextBank.NormalAttribute, Bytes = wanted });
                    else if (!have.Bytes.AsSpan().SequenceEqual(wanted)) have.Bytes = wanted;
                    else continue;
                    written++;
                }
                hqr = bank.WriteInto(hqr);
            }
        if (written > 0) File.WriteAllBytes(textPath, hqr);
        return $"the hoverboard's texts: {written} written (file {InventoryTexts}'s {ProtoSlot}, {SuperSlot} and +100, +200, {GazogemCan} and +200; file {SystemTexts}'s {ProtoName}, {JetName}, {JetKey}; in all {languages} languages)";
    }
}
