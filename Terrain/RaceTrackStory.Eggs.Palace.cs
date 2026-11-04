using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using System.Text.Json;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;
using LBAAssembler.Terrain.Palace;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The Emperor's palace's easter eggs, in its dungeon (PalaceIsland: under rooms 159 and 163, in through 159's fireplace), and the Brune
// sisters' end in the final (chapter 14). The places are the palace's own: PALACE.JSON's dungeon (its captives, the ninth portrait).
//
// ---- the Brune sisters (the user, 2026-11-04: "Let's make all those changes", of the proposal's "In the palace dungeon, the four
// Rabbibunny captives between the cages look just like her. Action at each cage: 'Are you from Citadel? Tell our sister we're fine. Well.
// Fine-ish.' Talk to all four, and in the final (chapter 14) they're in the dock's crowd, and Mrs. Brune with them: 'Not one postcard!'").
// The four captives hung from the dungeon's ceiling (PalaceFurniture.Captive: drawn only) each have an invisible talker on the floor under
// her, a line of her own. They hang in four cubes -- four scenes, and var_cube starts afresh in each -- so what has been said is kept in the
// game variable EggSisters itself: 100 plus a bit for each sister heard (1, 2, 4, 8) while some are left, then 1 once all four have spoken
// (the final's gate). In the final, Mrs. Brune -- the game's, at Citadel's ferry office (scene 8's actor 3: entity 40, BODY.HQR 62, as
// the story has her: RaceTrackStory.Chapters TownPeople; scene 43's ticket checker is entity 38, Mr. Paul's look) -- and her four sisters (entity 45: Mrs. Brune's body 62, as the captives are, and its other, 68, the blue and white rabbibunny) stand on the dock
// by the crowd while EggSisters is 1.
//
// ---- the ninth portrait (the proposal's "deep in the dungeon, behind the iron maiden, a portrait facing the wall. Action turns it round:
// Twinsen in his LBA1 tunic, captioned 'WANTED. Escaped from the asylum, Citadel Island.' Twinsen: 'That's not a good likeness.'"). The
// picture is the palace's (PalaceWanted: a decor on the east wall behind the maiden, drawn whatever happens); facing the wall it is the
// back of a frame hung in front of it -- an actor, a body of its own (Cover: old wood round a linen back, a cross of stretcher bars;
// entity 16's body CoverGeneric, the game's helpers' entity, which has no animations) -- that Action takes away (invisible) for the
// rest of the visit, the caption said and Twinsen's line after it.
internal static partial class RaceTrackStory
{
    private enum PalaceEggLine { Sister1, Sister2, Sister3, Sister4, Wanted, Likeness }
    private static readonly string[][] PalaceEggLines =
    {
        new[]
        {
            "Are you from Citadel? Tell our sister we're fine. Well. Fine-ish.",
            "Tu viens de la Citadelle ? Dis à notre soeur qu'on va bien. Enfin. Plus ou moins.",
            "Kommst du von der Zitadelle? Sag unserer Schwester, dass es uns gut geht. Na ja. Halbwegs.",
            "¿Vienes de la Ciudadela? Dile a nuestra hermana que estamos bien. Bueno. Más o menos.",
            "Vieni dalla Cittadella? Di' a nostra sorella che stiamo bene. Be'. Più o meno.",
            "Vens da Cidadela? Diz à nossa irmã que estamos bem. Enfim. Mais ou menos.",
        },
        new[]
        {
            "That accent... Citadel! Our sister sells the ferry tickets there. Don't mention the ropes. She worries.",
            "Cet accent... la Citadelle ! Notre soeur vend les billets du ferry, là-bas. Ne lui parle pas des cordes. Elle s'inquiète.",
            "Dieser Akzent... die Zitadelle! Unsere Schwester verkauft dort die Fährkarten. Erzähl ihr nichts von den Seilen. Sie macht sich immer Sorgen.",
            "Ese acento... ¡la Ciudadela! Nuestra hermana vende los billetes del ferry allí. No le hables de las cuerdas. Se preocupa.",
            "Quell'accento... la Cittadella! Nostra sorella vende i biglietti del traghetto, lì. Non dirle delle corde. Si preoccupa.",
            "Esse sotaque... a Cidadela! A nossa irmã vende os bilhetes do ferry lá. Não lhe fales das cordas. Ela preocupa-se.",
        },
        new[]
        {
            "We came to Zeelich on holiday. The brochure said 'see the palace'. It didn't say from the inside.",
            "On est venues à Zeelich en vacances. La brochure disait 'visitez le palais'. Elle ne disait pas de l'intérieur.",
            "Wir sind im Urlaub nach Zeelich gekommen. Im Prospekt stand 'Besuchen Sie den Palast'. Von innen stand da nichts.",
            "Vinimos a Zeelich de vacaciones. El folleto decía 'visite el palacio'. No decía que por dentro.",
            "Siamo venute a Zeelich in vacanza. Il dépliant diceva 'visitate il palazzo'. Non diceva da dentro.",
            "Viemos a Zeelich de férias. O folheto dizia 'visite o palácio'. Não dizia que era por dentro.",
        },
        new[]
        {
            "Four of us on holiday, and not one of us sent her a postcard. Tell her we'll write. Soon.",
            "Quatre en vacances, et pas une de nous ne lui a envoyé de carte postale. Dis-lui qu'on va écrire. Bientôt.",
            "Wir vier im Urlaub, und keine von uns hat ihr eine Postkarte geschickt. Sag ihr, wir schreiben. Bald.",
            "Cuatro de vacaciones, y ninguna le ha mandado una postal. Dile que escribiremos. Pronto.",
            "Quattro in vacanza, e nessuna di noi le ha mandato una cartolina. Dille che scriveremo. Presto.",
            "Quatro de férias, e nenhuma lhe mandou um postal. Diz-lhe que vamos escrever. Em breve.",
        },
        new[]
        {
            "WANTED. Escaped from the asylum, Citadel Island.",
            "RECHERCHÉ. Évadé de l'asile, île de la Citadelle.",
            "GESUCHT. Aus der Anstalt entflohen, Zitadelleninsel.",
            "SE BUSCA. Fugado del asilo, isla de la Ciudadela.",
            "RICERCATO. Evaso dal manicomio, isola della Cittadella.",
            "PROCURADO. Fugido do asilo, ilha da Cidadela.",
        },
        new[]
        {
            "That's not a good likeness.",
            "Ce n'est pas très ressemblant.",
            "Sehr ähnlich sieht mir das nicht.",
            "No se me parece mucho.",
            "Non mi somiglia molto.",
            "Não está muito parecido.",
        },
    };

    private enum BruneLine { NotOnePostcard, WeWrote }
    private static readonly string[][] BruneLines =
    {
        new[]
        {
            "Not one postcard!",
            "Pas une seule carte postale !",
            "Nicht eine einzige Postkarte!",
            "¡Ni una sola postal!",
            "Neanche una cartolina!",
            "Nem um postal!",
        },
        new[]
        {
            "We did write one. There's no post office in a dungeon.",
            "On en a écrit une. Il n'y a pas de bureau de poste dans un cachot.",
            "Wir haben eine geschrieben. In einem Kerker gibt es keine Post.",
            "Escribimos una. En una mazmorra no hay oficina de correos.",
            "Una l'abbiamo scritta. In una segreta non c'è l'ufficio postale.",
            "Escrevemos um. Numa masmorra não há correios.",
        },
    };

    // EggSisters while some are left: SistersHeard plus a bit for each sister heard
    private const int SistersHeard = 100;
    // the cover: entity 16's body CoverGeneric (an actor record's body is a signed byte); its flags: no shadow, nothing collides with it
    private const int CoverEntity = 16, CoverGeneric = 97;
    private const uint CoverFlags = 0x1000;
    // (world units) the cover's thickness, how much bigger than the picture it is each side, and how far in front of the picture
    private const double CoverThick = 50, CoverOver = 30, CoverGap = 8;
    // the game's Mrs. Brune (Citadel's ferry office, scene 8, actor 3) and her sisters' entity: its bodies 0 (BODY.HQR 62, hers) and 43
    // (68), standing (generic animation 0)
    private const int BruneScene = 8, BruneActor = 3, SistersEntity = 45, SisterBody = 0, SisterBlueBody = 43;
    // (the captives in PalaceIsland's order -- two by their wrists, two lying -- have bodies 62, 68, 68, 62: PalaceFurniture.Captive)
    private static readonly int[] SisterBodies = { SisterBody, SisterBlueBody, SisterBlueBody, SisterBody };

    // The dungeon's eggs, into the palace's scenes (`s`: ApplyPalace's): the sisters' talkers and the ninth portrait's cover.
    private static void PalaceDungeonEggs(LateStory s)
    {
        var layout = Path.Combine(s.Game, PalaceIsland.LayoutFile);
        if (!File.Exists(layout)) { s.Log.Add("the palace's easter eggs left out: no PALACE.JSON"); return; }
        double[][] captives;
        double[]? wanted;
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(layout));
            if (!json.RootElement.TryGetProperty("dungeon", out var d) || d.ValueKind != JsonValueKind.Object || !d.TryGetProperty("captives", out var c))
            { s.Log.Add("the palace's easter eggs left out: PALACE.JSON has no dungeon captives (a palace built before them)"); return; }
            captives = c.EnumerateArray().Select(a => a.EnumerateArray().Select(v => v.GetDouble()).ToArray()).ToArray();
            wanted = d.TryGetProperty("wanted", out var w) ? w.EnumerateArray().Select(v => v.GetDouble()).ToArray() : null;
            if (!d.TryGetProperty("floorTop", out var floor)) { s.Log.Add("the palace's easter eggs left out: no dungeon floor in PALACE.JSON"); return; }
            var floorTop = floor.GetDouble();
            var first = s.Texts(PalaceTexts, PalaceEggLines, "the palace's easter eggs (the Brune sisters, the ninth portrait)");
            int Id(PalaceEggLine l) => first + (int)l;

            // the sisters: a talker on the floor under each
            for (var k = 0; k < captives.Length && k < 4; k++)
            {
                var (x, z) = (captives[k][0], captives[k][1]);
                var scene = GroupB.OutsideScene(s.Store, s.Island, x, z);
                if (scene < 0) { s.Log.Add($"the Brune sister {k + 1} left out: her place ({x:0.#}, {z:0.#}) is in no scene of the palace's"); continue; }
                var line = Id(PalaceEggLine.Sister1 + k);
                var bit = 1 << k;
                s.Edit(scene, $"the Brune sister {k + 1} (an easter egg): a talker under the captive at cell ({x:0.#}, {z:0.#}), her line {line}; EggSisters {EggSisters}", model =>
                {
                    var talker = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(x, model.CubeX), (int)Math.Round(floorTop), RaceTrackScenes.CubeLocal(z, model.CubeY), 9);
                    return s.Add(model, talker, SisterTalk(line, bit));
                });
            }

            // the ninth portrait's cover
            if (wanted is not { Length: >= 3 }) { s.Log.Add("the ninth portrait's cover left out: no portrait in PALACE.JSON"); return; }
            double wallX = wanted[0], middleZ = wanted[1], footY = wanted[2];
            var at = (X: wallX - (PalaceWanted.OffWall + CoverGap + CoverThick / 2) / 512, Z: middleZ);
            var coverScene = GroupB.OutsideScene(s.Store, s.Island, at.X, at.Z);
            if (coverScene < 0) { s.Log.Add("the ninth portrait's cover left out: its place is in no scene of the palace's"); return; }
            s.Log.Add(InstallCover(s.Game));
            var floorY = footY - PalaceWanted.FootOver;
            s.Edit(coverScene, $"the ninth portrait (an easter egg): the back of its frame (entity {CoverEntity}'s body {CoverGeneric}) at cell ({at.X:0.#}, {at.Z:0.#}); Action turns it round, lines {Id(PalaceEggLine.Wanted)}-{Id(PalaceEggLine.Likeness)}", model =>
            {
                var cover = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(at.X, model.CubeX), (int)Math.Round(floorY), RaceTrackScenes.CubeLocal(at.Z, model.CubeY), 9);
                cover.Flags = CoverFlags; cover.Entity = CoverEntity; cover.Body = CoverGeneric; cover.Beta = 0;
                return s.Add(model, cover, $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if (1800 > distance(0) && {Driving} != comportement_hero())
        {{
            invisible(1);
            message({Id(PalaceEggLine.Wanted)});
            message_obj(0, {Id(PalaceEggLine.Likeness)});
            set_comportement(comportement_2);
        }}
    }}
}}

void comportement_2()
{{
    swif (1 == action())
    {{
        if (1800 > distance(0) && {Driving} != comportement_hero())
        {{
            message({Id(PalaceEggLine.Wanted)});
        }}
    }}
}}
");
            });
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or IOException or InvalidDataException or KeyNotFoundException)
        {
            s.Log.Add($"the palace's easter eggs left out: {e.Message}");
        }
    }

    // A sister's talker: her line; EggSisters 0, or SistersHeard plus the sisters heard, gains her `bit` -- 1 once all four are heard
    private static string SisterTalk(int line, int bit)
    {
        var steps = new System.Text.StringBuilder();
        steps.Append($@"            if (0 == var_game({EggSisters}))
            {{
                set_var_game({EggSisters}, {SistersHeard + bit});
            }}");
        for (var heard = 1; heard < 15; heard++)
        {
            if ((heard & bit) != 0) continue;
            var now = heard | bit;
            steps.Append($@"
            else if ({SistersHeard + heard} == var_game({EggSisters}))
            {{
                set_var_game({EggSisters}, {(now == 15 ? 1 : SistersHeard + now)});
            }}");
        }
        return $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if (1600 > distance(0) && {Driving} != comportement_hero())
        {{
            message({line});
{steps}
        }}
    }}
}}
";
    }

    // The back of the frame: a body into BODY.HQR (once: an entity 16 body CoverGeneric that is this already stays) and entity 16's body
    // CoverGeneric in RESS.HQR's entity table. In the island's palette (Otringal's, the palace's), drawn as it is (unlit), its own frame:
    // x across its thickness, y up from the floor (its foot FootOver less CoverOver over it), z along the wall -- the actor turned 0.
    private static string InstallCover(string gameDirectory)
    {
        var palette = IslandMapRenderer.LoadPaletteEntry(gameDirectory, PalaceIsland.PaletteEntry);
        var matched = new Dictionary<(float, float, float), int>();
        int Colour((float R, float G, float B) c) => BrickBuilding.Nearest(palette, c, matched);
        int wood = Colour((88, 62, 38)), dark = Colour((58, 40, 24)), linen = Colour((150, 132, 104)), edge = Colour((120, 90, 46));
        var pts = new List<Vector3>();
        var faces = new List<Face>();
        // (a box, each face wound so that the plain cross product looks out of it, as the decors' are; `side` its four sides' colour)
        void Box(Vector3 lo, Vector3 hi, int front, int side)
        {
            var c = new[]
            {
                new Vector3(lo.X, lo.Y, lo.Z), new Vector3(hi.X, lo.Y, lo.Z), new Vector3(hi.X, lo.Y, hi.Z), new Vector3(lo.X, lo.Y, hi.Z),
                new Vector3(lo.X, hi.Y, lo.Z), new Vector3(hi.X, hi.Y, lo.Z), new Vector3(hi.X, hi.Y, hi.Z), new Vector3(lo.X, hi.Y, hi.Z),
            };
            var middle = (lo + hi) / 2;
            void Quad(int a, int b, int d, int e, int colour)
            {
                var first = pts.Count;
                var q = new[] { c[a], c[b], c[d], c[e] };
                var ids = new[] { 0, 1, 2, 3 };
                var n = Vector3.Cross(q[1] - q[0], q[2] - q[0]);
                if (Vector3.Dot(n, (q[0] + q[2]) / 2 - middle) < 0) Array.Reverse(ids);
                pts.AddRange(ids.Select(i => q[i]));
                faces.Add(new Face(new[] { first, first + 1, first + 2, first + 3 }, colour, Material: 0));
            }
            Quad(0, 3, 7, 4, front); Quad(1, 2, 6, 5, front);       // (the faces across x: the back the room sees, and the one to the wall)
            Quad(0, 1, 5, 4, side); Quad(3, 2, 6, 7, side);         // (its ends along z)
            Quad(4, 5, 6, 7, side); Quad(0, 1, 2, 3, side);         // (its top and foot)
        }
        float t = (float)(CoverThick / 2), w = (float)(PalaceWanted.PictureWide / 2 + CoverOver);
        float y0 = (float)(PalaceWanted.FootOver - CoverOver), y1 = (float)(PalaceWanted.FootOver + PalaceWanted.PictureHigh + CoverOver);
        const float Rim = 90, Bar = 60;
        // (the linen, a little back from the frame; the frame round it; the stretchers' cross, standing out of the linen)
        Box(new Vector3(-t + 10, y0 + Rim, -w + Rim), new Vector3(t - 10, y1 - Rim, w - Rim), linen, linen);
        Box(new Vector3(-t, y0, -w), new Vector3(t, y0 + Rim, w), wood, edge);
        Box(new Vector3(-t, y1 - Rim, -w), new Vector3(t, y1, w), wood, edge);
        Box(new Vector3(-t, y0 + Rim, -w), new Vector3(t, y1 - Rim, -w + Rim), wood, edge);
        Box(new Vector3(-t, y0 + Rim, w - Rim), new Vector3(t, y1 - Rim, w), wood, edge);
        float my = (y0 + y1) / 2;
        Box(new Vector3(-t + 4, my - Bar / 2, -w + Rim), new Vector3(t - 4, my + Bar / 2, w - Rim), dark, dark);
        Box(new Vector3(-t + 4, y0 + Rim, -Bar / 2), new Vector3(t - 4, y1 - Rim, Bar / 2), dark, dark);
        var bytes = RaceTrackPipes.Write(pts, faces, lit: false);

        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var bodies = HqrArchive.Open(bodyPath);
        var have = Lba2EntityTable.Load(gameDirectory)?.Entities.FirstOrDefault(e => e.Id == CoverEntity)?.Bodies.Where(b => b.Generic == CoverGeneric).Select(b => (int?)b.Body).FirstOrDefault();
        var count = HqrArchive.CountEntries(bodyPath);
        int index;
        if (have is { } h && h < count && bodies.Read(h).AsSpan().SequenceEqual(bytes)) index = h;
        else
        {
            File.WriteAllBytes(bodyPath, HqrWriter.AppendEntry(File.ReadAllBytes(bodyPath), HqrWriter.StoredEntry(bytes)));
            index = count;
        }
        // (its record with its box -- as entity 16's own bodies have theirs: 1, generic, 17, the body, 1, 14, x0 y0 z0 x1 y1 z1 -- the box the
        // engine tests against the depth of what is drawn already: without one only the foot of it showed in front of the picture)
        var record = new byte[19];
        record[0] = 1; record[1] = CoverGeneric; record[2] = 17; record[5] = 1; record[6] = 14;
        BinaryPrimitives.WriteInt16LittleEndian(record.AsSpan(3), (short)index);
        var box = new[] { -(int)t - 8, 0, -(int)w - 8, (int)t + 8, (int)y1 + 8, (int)w + 8 };
        for (var i = 0; i < 6; i++) BinaryPrimitives.WriteInt16LittleEndian(record.AsSpan(7 + 2 * i), (short)box[i]);
        var table = HqrArchive.Open(ressPath).Read(44);
        int at = -1, gone = 0;
        foreach (var r in Lba2EntityTable.Records(table, CoverEntity))
            if (r.Command == 1 && r.Generic == CoverGeneric) { at = r.At; gone = r.Size; break; }
        if (at < 0 && (at = Lba2EntityTable.EndOf(table, CoverEntity)) < 0) throw new InvalidDataException($"Entity {CoverEntity}'s records have no end mark.");
        table = Lba2EntityTable.Splice(table, CoverEntity, at, gone, record);
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return $"the ninth portrait's cover (the back of a frame): BODY.HQR {index}, entity {CoverEntity}'s body {CoverGeneric} ({faces.Count} faces)";
    }

    // The final's crowd (`s`, `model`, `at`, `here`: ApplyFinal's): Mrs. Brune and her four sisters, by the crowd, while EggSisters is 1 --
    // her line, and the sisters' answer. Their places `at` (back, aside) as the crowd's.
    private static List<Action<SceneScripts>> FinalBruneSisters(LateStory s, SceneModel model, Func<double, double, (double X, double Z, double Y, int Beta)> at, string here)
    {
        var edits = new List<Action<SceneScripts>>();
        var brune = Copy(s.Originals, BruneScene, BruneActor);
        if (brune is null) { s.Log.Add("the Brune sisters in the final left out: the game's Mrs. Brune (scene 8) not found"); return edits; }
        var first = s.Texts(FinalTexts, BruneLines, "the Brune sisters in the final (an easter egg)");
        var when = $"{here} && 1 == var_game({EggSisters})";
        Place(brune, model, at(10, 2.5));
        edits.Add(s.Add(model, brune, Talker(when, $"            message({first + (int)BruneLine.NotOnePostcard});")));
        var places = new[] { (11.0, 1.5), (11.0, 3.5), (12.0, 2.0), (12.0, 4.0) };
        for (var k = 0; k < 4; k++)
        {
            var sister = brune.Clone();
            sister.Entity = SistersEntity; sister.Body = SisterBodies[k]; sister.Anim = 0;
            Place(sister, model, at(places[k].Item1, places[k].Item2));
            edits.Add(s.Add(model, sister, Talker(when, $"            message({first + (int)BruneLine.WeWrote});")));
        }
        s.Log.Add($"the Brune sisters in the final (an easter egg): Mrs. Brune and her four sisters by the crowd while variable {EggSisters} is 1, texts {first}-{first + 1}");
        return edits;
    }
}
