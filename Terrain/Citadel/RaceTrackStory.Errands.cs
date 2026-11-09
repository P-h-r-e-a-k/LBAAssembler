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
    public const int ErrandSlot = 19, ErrandBait = 1, ErrandTyre = 2;
    public const int BaitModel = 154, TyreModel = 155;
    // (the inventory's tin less than half the counter's height: the user, 2026-11-08, "Its inventory model is too tall: at least half the
    // height"; scaled to the inventory's size by its width then)
    private const float InventoryBaitHeight = 0.45f;
    // (the game's inventory objects are about 3,500 across, centred on their middle: OBJFIX.HQR 20, the customer's umbrella, -1739..1739)
    private const float InventoryExtent = 3200;

    private static readonly (int Model, string[][] Texts)[] ErrandItems =
    {
        (BaitModel, new[]
        {
            new[] { "You bought a tin of fishing bait. Bob at the harbour will want it.", "Tin of fishing bait", "A tin of lively worms from Mr. Bazoo's shop. Bob at the harbour will trade for it." },
            new[] { "Tu as acheté une boîte d'appâts. Bob, au port, en voudra.", "Boîte d'appâts", "Une boîte de vers bien vivants de la boutique de M. Bazoo. Bob, au port, l'échangera volontiers." },
            new[] { "Du hast eine Dose Angelköder gekauft. Bob am Hafen wird sie haben wollen.", "Dose Angelköder", "Eine Dose munterer Würmer aus Herrn Bazoos Laden. Bob am Hafen tauscht gern dafür." },
            new[] { "Has comprado una lata de cebo. Bob, en el puerto, la querrá.", "Lata de cebo", "Una lata de gusanos bien vivos de la tienda del señor Bazoo. Bob, en el puerto, la cambiará con gusto." },
            new[] { "Hai comprato una scatola di esche. Bob, al porto, la vorrà.", "Scatola di esche", "Una scatola di vermi vivaci dal negozio del signor Bazoo. Bob, al porto, la scambierà volentieri." },
            new[] { "Compraste uma lata de isco. O Bob, no porto, vai querê-la.", "Lata de isco", "Uma lata de minhocas bem vivas da loja do senhor Bazoo. O Bob, no porto, troca-a de bom grado." },
        }),
        (TyreModel, new[]
        {
            new[] { "Bob gave you the old tyre he fished out of the harbour.", "Old tyre", "The tyre Bob fished out of the harbour. Zoe can fit it to Luc's barrel car." },
            new[] { "Bob t'a donné le vieux pneu qu'il a repêché dans le port.", "Vieux pneu", "Le pneu que Bob a repêché dans le port. Zoé peut le monter sur le tonneau sur roues de Luc." },
            new[] { "Bob hat dir den alten Reifen gegeben, den er aus dem Hafen gefischt hat.", "Alter Reifen", "Der Reifen, den Bob aus dem Hafen gefischt hat. Zoe kann ihn an Lucs Fass auf Rädern montieren." },
            new[] { "Bob te ha dado el neumático viejo que pescó en el puerto.", "Neumático viejo", "El neumático que Bob pescó en el puerto. Zoe puede ponérselo al barril con ruedas de Luc." },
            new[] { "Bob ti ha dato il vecchio pneumatico che ha ripescato nel porto.", "Vecchio pneumatico", "Il pneumatico che Bob ha ripescato nel porto. Zoe può montarlo sulla botte su ruote di Luc." },
            new[] { "O Bob deu-te o pneu velho que pescou no porto.", "Pneu velho", "O pneu que o Bob pescou no porto. A Zoe pode montá-lo no barril com rodas do Luc." },
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
        var models = new (int Model, Func<byte[], Body> Build)[] { (BaitModel, h => Citadel.CitadelProps.BuildBait(h, InventoryBaitHeight)), (TyreModel, Citadel.CitadelProps.BuildTyre) };
        var bytes = File.ReadAllBytes(objfixPath);
        var count = HqrArchive.CountEntries(objfixPath);
        // (the entries up to the models: each empty one a copy of the game's first, never shown)
        var filler = HqrWriter.StoredEntry(objfix.Read(0));
        while (count < models.Max(m => m.Model) + 1) { bytes = HqrWriter.AppendEntry(bytes, filler); count++; }
        foreach (var (model, build) in models)
            bytes = HqrWriter.ReplaceEntry(bytes, model, HqrWriter.StoredEntry(InventorySized(build(bodyHeader)).Write()));
        File.WriteAllBytes(objfixPath, bytes);
        log.Add($"the errands' slot {ErrandSlot}: OBJFIX.HQR {BaitModel} the tin of bait, {TyreModel} the old tyre (the props' bodies at an inventory object's size)");

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
        log.Add($"the errands' texts: {written} written ({BaitModel}, {TyreModel} and +100, +200, in all {languages} languages)");
        return log;
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
