using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The story's errands in Twinsen's inventory (the user, 2026-11-07: "Collectible items like fishing bait need to go into Twinsen's
// inventory and thus need an object modelling for them"; 2026-11-08: "Let's reuse inventory slots as much as possible ... things like the
// bait and tyre we never have at the same time so they only need a single slot"):
//   - one slot for the errands of chapter 2, ErrandSlot: 19, the game's Ring of Lightning -- which only the Weather Wizard gave, for the
//     Incandescent Pearl, in his tent (scene 21): the story's Wizard has no such trade (WizardNoRing), and nothing else gives or asks for
//     it. Its game variable is what it holds: 1 the tin of fishing bait (bought at Mr. Bazoo's), 2 Bob's old tyre (traded for the bait),
//     0 nothing (the tyre fitted to Luc's barrel car);
//   - each thing a model of its own: OBJFIX.HQR's entries BaitModel and TyreModel (the props' bodies, CitadelProps, scaled up to the
//     game's inventory objects' size), its found message, name and description the texts of that number, +100 and +200 in the
//     inventory's text file -- numbers past the game's whose three texts are all free -- and the race file's inv_kinds= tells the engine
//     which the slot shows by its variable (RACEMOD.CPP).
internal static partial class RaceTrackStory
{
    // (3: the island winner's trophy, Citadel Island's, won on the town circuit -- the errands done by then, the slot free -- shown to Flick
    // at the Desert's start line to race there: the user, 2026-11-16, "For winning Citadel 2 he gets an island winners trophy that he has
    // to present to be allowed to enter the desert race")
    public const int ErrandSlot = 19, ErrandBait = 1, ErrandTyre = 2, ErrandTrophy = 3;
    public const int BaitModel = 154, TyreModel = 155, TrophyModel = 157;   // (156 the sand filters': RaceTrackStory.Garage FiltersModel)
    // (Otringal's letters, 2026-11-23 -- the user: "The letter from and to the gazogem seller need to be an inventory item, perhaps a different
    // colour for each to differentiate them": in the sand filters' slot, free once they're fitted at the Desert -- 2 the seller's love
    // letter, a pink envelope sealed in red wax; 3 the singer's reply, pale blue, a lipstick kiss on its flap; 0 handed over)
    public const int LoveLetterModel = 158, ReplyModel = 159, LetterSlot = FiltersSlot, LetterHeld = 2, ReplyHeld = 3;
    // (the inventory's tin less than half the counter's height: the user, 2026-11-08, "Its inventory model is too tall: at least half the
    // height"; scaled to the inventory's size by its width then)
    private const float InventoryBaitHeight = 0.45f;
    // (the game's inventory objects are about 3,500 across, centred on their middle: OBJFIX.HQR 20, the customer's umbrella, -1739..1739)
    private const float InventoryExtent = 3200;

    private static readonly (int Model, string[][] Texts)[] ErrandItems =
    {
        (BaitModel, new[]
        {
            // (who wants it left for Twinsen to find out -- 2026-11-23, the user: "'You bought a tin of fishing bait. Felix at the harbour
            // will want it.' - this hint is too explicit and doesn't need to be there"; his own word as he buys it, "Somebody at the harbour
            // will be glad of this", is the hint)
            new[] { "You bought a tin of fishing bait.", "Tin of fishing bait", "A tin of lively worms from Mr. Bazoo's shop." },
            new[] { "Tu as acheté une boîte d'appâts.", "Boîte d'appâts", "Une boîte de vers bien vivants de la boutique de M. Bazoo." },
            new[] { "Du hast eine Dose Angelköder gekauft.", "Dose Angelköder", "Eine Dose munterer Würmer aus Herrn Bazoos Laden." },
            new[] { "Has comprado una lata de cebo.", "Lata de cebo", "Una lata de gusanos bien vivos de la tienda del señor Bazoo." },
            new[] { "Hai comprato una scatola di esche.", "Scatola di esche", "Una scatola di vermi vivaci dal negozio del signor Bazoo." },
            new[] { "Compraste uma lata de isco.", "Lata de isco", "Uma lata de minhocas bem vivas da loja do senhor Bazoo." },
        }),
        (TrophyModel, new[]
        {
            new[] { "Mr. Paul gives you Citadel Island's winner's trophy. The Desert's race lets in only the islands' winners: show it there.", "Island winner's trophy", "Citadel Island's winner's trophy, won on the town circuit. Show it at the Desert to race Twinsun's best." },
            new[] { "M. Paul te remet le trophée du vainqueur de l'île de la Citadelle. La course du Désert n'accepte que les vainqueurs des îles : montre-le là-bas.", "Trophée du vainqueur de l'île", "Le trophée du vainqueur de l'île de la Citadelle, gagné sur le circuit de la ville. Montre-le au Désert pour courir contre les meilleurs de Twinsun." },
            new[] { "Herr Paul überreicht dir den Siegerpokal der Zitadelleninsel. Zum Rennen in der Wüste kommen nur die Sieger der Inseln: Zeig ihn dort vor.", "Siegerpokal der Insel", "Der Siegerpokal der Zitadelleninsel, gewonnen auf dem Stadtkurs. Zeig ihn in der Wüste, um gegen die Besten von Twinsun zu fahren." },
            new[] { "El señor Paul te entrega el trofeo del ganador de la isla de la Ciudadela. La carrera del Desierto solo admite a los ganadores de las islas: muéstralo allí.", "Trofeo del ganador de la isla", "El trofeo del ganador de la isla de la Ciudadela, ganado en el circuito de la ciudad. Muéstralo en el Desierto para correr contra los mejores de Twinsun." },
            new[] { "Il signor Paul ti consegna il trofeo del vincitore dell'isola della Cittadella. La corsa del Deserto ammette solo i vincitori delle isole: mostralo là.", "Trofeo del vincitore dell'isola", "Il trofeo del vincitore dell'isola della Cittadella, vinto sul circuito della città. Mostralo nel Deserto per correre contro i migliori di Twinsun." },
            new[] { "O senhor Paul entrega-te o troféu do vencedor da ilha da Cidadela. A corrida do Deserto só aceita os vencedores das ilhas: mostra-o lá.", "Troféu do vencedor da ilha", "O troféu do vencedor da ilha da Cidadela, ganho no circuito da cidade. Mostra-o no Deserto para correr contra os melhores de Twinsun." },
        }),
        (LoveLetterModel, new[]
        {
            new[] { "The gazogem seller gave you his love letter for the singer at Rick's bar.", "The seller's love letter", "A pink envelope, scented, sealed with red wax. For the singer at Rick's bar -- and not to be read!" },
            new[] { "Le vendeur de gazogem t'a confié sa lettre d'amour pour la chanteuse du bar de Rick.", "La lettre d'amour du vendeur", "Une enveloppe rose, parfumée, scellée de cire rouge. Pour la chanteuse du bar de Rick -- et pas question de la lire !" },
            new[] { "Der Gazogem-Verkäufer hat dir seinen Liebesbrief für die Sängerin in Ricks Bar gegeben.", "Der Liebesbrief des Verkäufers", "Ein rosa Umschlag, parfümiert, mit rotem Wachs versiegelt. Für die Sängerin in Ricks Bar -- und nicht zum Lesen!" },
            new[] { "El vendedor de gazogem te ha dado su carta de amor para la cantante del bar de Rick.", "La carta de amor del vendedor", "Un sobre rosa, perfumado, sellado con lacre rojo. Para la cantante del bar de Rick... ¡y no se lee!" },
            new[] { "Il venditore di gazogem ti ha dato la sua lettera d'amore per la cantante del bar di Rick.", "La lettera d'amore del venditore", "Una busta rosa, profumata, sigillata con ceralacca rossa. Per la cantante del bar di Rick -- e guai a leggerla!" },
            new[] { "O vendedor de gazogem deu-te a carta de amor dele para a cantora do bar do Rick.", "A carta de amor do vendedor", "Um envelope cor-de-rosa, perfumado, selado com lacre vermelho. Para a cantora do bar do Rick -- e não é para ler!" },
        }),
        (ReplyModel, new[]
        {
            new[] { "The singer gave you her answer for the gazogem seller.", "The singer's reply", "A pale blue envelope with a kiss of lipstick on its flap. For the gazogem seller in the Lower City." },
            new[] { "La chanteuse t'a donné sa réponse pour le vendeur de gazogem.", "La réponse de la chanteuse", "Une enveloppe bleu pâle, un baiser de rouge à lèvres sur le rabat. Pour le vendeur de gazogem de la Ville Basse." },
            new[] { "Die Sängerin hat dir ihre Antwort für den Gazogem-Verkäufer gegeben.", "Die Antwort der Sängerin", "Ein hellblauer Umschlag mit einem Lippenstiftkuss auf der Lasche. Für den Gazogem-Verkäufer in der Unterstadt." },
            new[] { "La cantante te ha dado su respuesta para el vendedor de gazogem.", "La respuesta de la cantante", "Un sobre azul pálido con un beso de carmín en la solapa. Para el vendedor de gazogem de la Ciudad Baja." },
            new[] { "La cantante ti ha dato la sua risposta per il venditore di gazogem.", "La risposta della cantante", "Una busta azzurra con un bacio di rossetto sul lembo. Per il venditore di gazogem della Città Bassa." },
            new[] { "A cantora deu-te a resposta dela para o vendedor de gazogem.", "A resposta da cantora", "Um envelope azul-claro com um beijo de batom na aba. Para o vendedor de gazogem da Cidade Baixa." },
        }),
        (TyreModel, new[]
        {
            new[] { "Felix gave you the old tyre he fished out of the harbour.", "Old tyre", "The tyre Felix fished out of the harbour. Zoe can fit it to Luc's barrel car." },
            new[] { "Felix t'a donné le vieux pneu qu'il a repêché dans le port.", "Vieux pneu", "Le pneu que Felix a repêché dans le port. Zoé peut le monter sur le tonneau sur roues de Luc." },
            new[] { "Felix hat dir den alten Reifen gegeben, den er aus dem Hafen gefischt hat.", "Alter Reifen", "Der Reifen, den Felix aus dem Hafen gefischt hat. Zoe kann ihn an Lucs Fass auf Rädern montieren." },
            new[] { "Felix te ha dado el neumático viejo que pescó en el puerto.", "Neumático viejo", "El neumático que Felix pescó en el puerto. Zoe puede ponérselo al barril con ruedas de Luc." },
            new[] { "Felix ti ha dato il vecchio pneumatico che ha ripescato nel porto.", "Vecchio pneumatico", "Il pneumatico che Felix ha ripescato nel porto. Zoe può montarlo sulla botte su ruote di Luc." },
            new[] { "O Felix deu-te o pneu velho que pescou no porto.", "Pneu velho", "O pneu que o Felix pescou no porto. A Zoe pode montá-lo no barril com rodas do Luc." },
        }),
    };

    // Into the game folder: the models and the texts. Lines for the build's log. A second pass finds them there and leaves them.
    internal static List<string> ErrandsInstall(string gameDirectory)
    {
        var log = new List<string>();
        var objfixPath = Path.Combine(gameDirectory, "OBJFIX.HQR");
        var objfix = HqrArchive.Open(objfixPath);
        var header = Body.Read(objfix.Read(20), 2, allowStatic: true).Header;
        var bodyHeader = Body.Read(HqrArchive.Open(Path.Combine(gameDirectory, "BODY.HQR")).Read(30), 2, allowStatic: true).Header;
        var models = new (int Model, Func<byte[], Body> Build)[] { (LoveLetterModel, h => BuildEnvelope(h, reply: false)), (ReplyModel, h => BuildEnvelope(h, reply: true)), (BaitModel, h => Citadel.CitadelProps.BuildBait(h, InventoryBaitHeight)), (TyreModel, Citadel.CitadelProps.BuildTyre), (TrophyModel, Citadel.CitadelProps.BuildTrophy) };
        var bytes = File.ReadAllBytes(objfixPath);
        var count = HqrArchive.CountEntries(objfixPath);
        // (the entries up to the models: each empty one a copy of the game's first, never shown)
        var filler = HqrWriter.StoredEntry(objfix.Read(0));
        while (count < models.Max(m => m.Model) + 1) { bytes = HqrWriter.AppendEntry(bytes, filler); count++; }
        foreach (var (model, build) in models)
            bytes = HqrWriter.ReplaceEntry(bytes, model, HqrWriter.StoredEntry(InventorySized(build(bodyHeader)).Write()));
        File.WriteAllBytes(objfixPath, bytes);
        log.Add($"the errands' slot {ErrandSlot}: OBJFIX.HQR {BaitModel} the tin of bait, {TyreModel} the old tyre, {TrophyModel} the island winner's trophy (the props' bodies at an inventory object's size)");

        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        var hqr = File.ReadAllBytes(textPath);
        var written = 0;
        for (var lang = 0; lang < languages; lang++)
        {
            var bank = Lba2TextBank.Load(text, lang, HolomapTexts);
            foreach (var (model, texts) in ErrandItems)
            {
                var words = texts[lang < texts.Length ? lang : 0];
                for (var k = 0; k < 3; k++)
                {
                    var id = model + 100 * k;
                    var have = bank.Find(id);
                    var wanted = Dos.GetBytes(words[k]);
                    if (have is not null && have.Bytes.AsSpan().SequenceEqual(wanted)) continue;
                    if (have is not null) { log.Add($"WARNING: the errands: text {id} of file {HolomapTexts} is something else's -- left as it is"); continue; }
                    bank.Texts.Add(new Lba2TextBank.Text { Id = id, Attribute = Lba2TextBank.NormalAttribute, Bytes = wanted });
                    written++;
                }
            }
            hqr = bank.WriteInto(hqr);
        }
        if (written > 0) File.WriteAllBytes(textPath, hqr);
        log.Add($"the errands' texts: {written} written ({BaitModel}, {TyreModel}, {TrophyModel} and +100, +200, in all {languages} languages)");
        return log;
    }

    // An envelope (the inventory's: InventorySized after): a body with depth -- its back, its edges' sides, the front's two side flaps and
    // the bottom flap sloping in to the middle, the top flap folded over them, a wax seal (the love letter's) or a lipstick kiss (the
    // reply's) on its point; every face at its own angle and lit, each colour low on its ramp so the light stays in it
    private static Body BuildEnvelope(byte[] header, bool reply)
    {
        // (lying flat, face up -- the inventory looks down on its things: stood up, it showed edge-on, a sliver)
        var m = new CarMesh(reply ? "the singer's reply" : "the seller's love letter", new[] { -1 });
        m.Light = 0.7f;
        int paper = reply ? 12 * 16 + 7 : 4 * 16 + 8, fold = reply ? 12 * 16 + 6 : 4 * 16 + 7, edge = reply ? 12 * 16 + 4 : 4 * 16 + 5;
        const float W = 900, D = 580, T = 50, Rise = 26;
        // the back and the edges' sides: a slab
        m.Box(0, new Vector3(0, T / 2, 0), new Vector3(2 * W, T, 2 * D), paper, edge);
        // the front's flaps: triangles from the corners in to the middle, its point lifted a little so they slope
        var c = new Vector3(0, T + Rise, 40);
        Vector3 nl = new(-W, T, -D), nr = new(W, T, -D), fr = new(W, T, D), fl = new(-W, T, D);
        m.In(new[] { m.P(0, fl), m.P(0, fr), m.P(0, c) }, fold, new Vector3(0, 300, D));
        m.In(new[] { m.P(0, fr), m.P(0, nr), m.P(0, c + new Vector3(60, 0, 0)) }, paper, new Vector3(W, 300, 0));
        m.In(new[] { m.P(0, nl), m.P(0, fl), m.P(0, c - new Vector3(60, 0, 0)) }, paper, new Vector3(-W, 300, 0));
        // the top flap folded over them, its point past the middle
        var tip = new Vector3(0, T + Rise + 14, 150);
        m.In(new[] { m.P(0, nl + new Vector3(0, 4, 0)), m.P(0, nr + new Vector3(0, 4, 0)), m.P(0, tip) }, fold, new Vector3(0, 300, -D));
        if (!reply)
        {
            // the wax seal: a red disc, raised, and its stamp
            m.Ball(0, tip + new Vector3(0, 18, 0), 120, 4 * 16 + 3);
            m.Box(0, tip + new Vector3(0, 70, 0), new Vector3(90, 20, 70), 4 * 16 + 1, 4 * 16 + 2);
        }
        else
        {
            // a kiss of lipstick on the flap: two little red lips
            m.Box(0, tip + new Vector3(0, 10, -90), new Vector3(170, 12, 40), 4 * 16 + 5, 4 * 16 + 4);
            m.Box(0, tip + new Vector3(0, 10, -40), new Vector3(150, 12, 36), 4 * 16 + 5, 4 * 16 + 4);
        }
        return m.ToBody(header);
    }

    // A prop's body at the inventory's size: centred on its middle and scaled so its widest side is InventoryExtent
    private static Body InventorySized(Body body)
    {
        var v = body.Vertices;
        if (v.Count == 0) return body;
        var lo = new Vector3(v.Min(p => p.X), v.Min(p => p.Y), v.Min(p => p.Z));
        var hi = new Vector3(v.Max(p => p.X), v.Max(p => p.Y), v.Max(p => p.Z));
        var middle = (lo + hi) / 2;
        var size = MathF.Max(hi.X - lo.X, MathF.Max(hi.Y - lo.Y, hi.Z - lo.Z));
        var k = size > 0 ? InventoryExtent / size : 1;
        for (var i = 0; i < v.Count; i++) v[i] = (v[i] - middle) * k;
        for (var i = 0; i < body.Spheres.Count; i++) body.Spheres[i] = body.Spheres[i] with { Radius = (int)Math.Round(body.Spheres[i].Radius * k) };
        return body;
    }
}
