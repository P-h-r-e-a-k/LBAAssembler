using System.IO;
using System.Numerics;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The black car's case file (the user, 2026-11-27: "I like the idea of 'The black car's case file' slowly filling up with clues. This would
// be a new in-game object that Twinsen can open from his inventory and inspect"). An inventory object, in the tart's slot (FLAG_TART_LUCI,
// 25: the story never gives the tart -- Wannies Island's Lucy's-tart errand picks it up and hands it over by its own variable, 64, now
// without the slot, TartOff), its model and texts the case file's. Zoe gives it to Twinsen after the Desert race, as she wonders what
// the black car was doing on the track (CaseHandover, said after her oil line). Used from the inventory, in the car too, its pages
// (RACEGAME.CPP RaceGame_CaseFile: the race file's case_file=, the game folder's CASEFILE.TXT): a page a clue, each found once its game
// variable is set -- by the line that tells Twinsen of it, where he hears it.
// The pages so far are the story's own black-car lines up to Otringal, and the Temple of Bú's skeleton witness (311: RaceTrackStory's
// Temple remodel sets it). Clues for the lava lake and the Island of the Francos are drafted for the user, not built.
internal static partial class RaceTrackStory
{
    // (the tart's slot; its model OBJFIX.HQR 25 and texts 25, 125, 225 of file 2 the case file's)
    internal const int CaseSlot = 25;
    // the clues' game variables (310-329 the case file's: reference_game_variables_wide)
    internal const int ClueCitadel = 310, ClueTemple = 311, ClueOil = 312, ClueLaunch = 313, ClueEntry = 314, ClueCoins = 315, ClueCasino = 316, ClueShuttle = 317;
    private const string CaseFileName = "CASEFILE.TXT";
    // Lucy's tart on Wannies Island: picked up (scene 101, Twinsen's life) and handed over (scene 118, actor 2)
    private const int TartPickedUp = 101, TartHandedOver = 118, TartTaker = 2;

    // The race file's line: the slot whose use opens the case file
    internal static string CaseFileLine() => $"case_file={CaseSlot}\n";
    // A clue found: its page's variable set (said after the line that tells it)
    private static string Clue(int var) => $"set_var_game({var}, 1);";

    private sealed record CasePage(int Var, string Island, string Title, string Note, string Where);
    // In the order the story meets them (English, as the scrapbook's cards are)
    private static readonly CasePage[] CasePages =
    {
        new(ClueCitadel, "Citadel Island", "No plates, no driver",
            "Past the town circuit's finish line, a black car with its engine running. No number plates, and no driver anyone could see. It drove off before anyone got near.",
            "Zoe's folder starts with what everyone saw on Citadel."),
        new(ClueOil, "White Leaf Desert", "Oil on the last bend",
            "Moya: oil on the last bend on the morning of the race, and a black car came through in the night, slow, looking for something. Zoe: or putting something on the track?",
            "Ask the racers once the Desert race is over."),
        new(ClueTemple, "White Leaf Desert", "A witness at the Temple of Bú",
            "A skeleton at the Temple of Bú saw a black car tear through towards the Esmers' hangar: \"Went by so fast my skull fell off.\" Its tyre marks lead the same way.",
            "Someone at the Temple of Bú saw something go by."),
        new(ClueLaunch, "The Emerald Moon", "A launch to Zeelich",
            "HAL's log: a launch from the moon the night before the race. One black car, one passenger, to Zeelich. The passenger's name was not recorded.",
            "The moon base's computer keeps a log."),
        new(ClueEntry, "Otringal", "A masked entry",
            "The race marshal: the last place on Otringal's grid went to a black car. No name on its entry, just a mask.",
            "Ask the race marshal at the start line."),
        new(ClueCoins, "Otringal", "Paid in old Sup coins",
            "Lyra: the black car paid its entry in old Sup coins. Nobody has seen its driver's face, not even her.",
            "The Cup's marshal knows how every entry was paid."),
        new(ClueCasino, "Otringal", "A night at the casino",
            "Rick: the black car's driver was in his casino the night before the race. Emptied one of his machines, never said a word, never took the mask off.",
            "Someone at the casino saw the driver."),
        new(ClueShuttle, "Otringal", "The first shuttle to Celebration",
            "Johnny Rocket: a black car came off the moon ship and took the first shuttle to Celebration Island. The souvenir seller there sells pictures of every car he's seen.",
            "Ask at the space port."),
    };

    // The inventory object's texts: found, its name, its description
    private static readonly string[][] CaseTexts =
    {
        new[] { "Zoe gives you her old folder: the black car's case file. Open it from the inventory to read what you've found out.", "The black car's case file", "Zoe's old folder, the black car's photo clipped to the front. Everything you've found out about it, a clue a page." },
        new[] { "Zoé te donne son vieux dossier : le dossier de la voiture noire. Ouvre-le depuis l'inventaire pour relire ce que tu as découvert.", "Le dossier de la voiture noire", "Le vieux dossier de Zoé, la photo de la voiture noire agrafée dessus. Tout ce que tu as découvert à son sujet, un indice par page." },
        new[] { "Zoe gibt dir ihre alte Mappe: die Akte des schwarzen Wagens. Öffne sie im Inventar, um nachzulesen, was du herausgefunden hast.", "Die Akte des schwarzen Wagens", "Zoes alte Mappe, vorne das Foto des schwarzen Wagens angeklammert. Alles, was du über ihn herausgefunden hast, ein Hinweis pro Seite." },
        new[] { "Zoe te da su vieja carpeta: el expediente del coche negro. Ábrela desde el inventario para leer lo que has descubierto.", "El expediente del coche negro", "La vieja carpeta de Zoe, con la foto del coche negro sujeta delante. Todo lo que has descubierto sobre él, una pista por página." },
        new[] { "Zoe ti dà la sua vecchia cartellina: il fascicolo dell'auto nera. Aprila dall'inventario per rileggere ciò che hai scoperto.", "Il fascicolo dell'auto nera", "La vecchia cartellina di Zoe, con la foto dell'auto nera fermata sul davanti. Tutto ciò che hai scoperto su di lei, un indizio per pagina." },
        new[] { "A Zoe dá-te a velha pasta dela: o processo do carro preto. Abre-a no inventário para leres o que descobriste.", "O processo do carro preto", "A velha pasta da Zoe, com a fotografia do carro preto presa à frente. Tudo o que descobriste sobre ele, uma pista por página." },
    };

    // Zoe's word as she hands it over (the Desert's text file)
    private enum CaseLine { ZoeFolder }
    private static readonly string[][] CaseLines =
    {
        new[]
        {
            "A black car with no plates, oil on the track... Here, Twinsen, my old folder. Write down everything you find out about that car. Sooner or later it'll all make sense.",
            "Une voiture noire sans plaques, de l'huile sur la piste... Tiens, Twinsen, mon vieux dossier. Note tout ce que tu découvres sur cette voiture. Tôt ou tard, tout s'éclairera.",
            "Ein schwarzer Wagen ohne Kennzeichen, Öl auf der Strecke... Hier, Twinsen, meine alte Mappe. Schreib alles auf, was du über diesen Wagen herausfindest. Früher oder später ergibt alles einen Sinn.",
            "Un coche negro sin matrícula, aceite en la pista... Toma, Twinsen, mi vieja carpeta. Apunta todo lo que averigües sobre ese coche. Tarde o temprano todo tendrá sentido.",
            "Un'auto nera senza targa, olio sulla pista... Tieni, Twinsen, la mia vecchia cartellina. Annota tutto quello che scopri su quell'auto. Prima o poi tutto avrà un senso.",
            "Um carro preto sem matrícula, óleo na pista... Toma, Twinsen, a minha velha pasta. Aponta tudo o que descobrires sobre esse carro. Mais cedo ou mais tarde tudo fará sentido.",
        },
    };

    // After the Desert race, Zoe's oil line said: the folder, once (and Citadel's page with it: the car past the town circuit's finish is
    // only there in chapter 2, and the file starts from what everyone saw)
    private static string CaseHandover(int first) => $@"        if (0 == var_game({CaseSlot}))
        {{
            message_zoe({first + (int)CaseLine.ZoeFolder});
            found_object({CaseSlot});
            set_var_game({CaseSlot}, 1);
            {Clue(ClueCitadel)}
        }}
";

    // Into the game folder: CASEFILE.TXT, the folder's model (OBJFIX.HQR 25) and its texts (file 2's 25, 125, 225). Lines for the log.
    internal static List<string> CaseFileInstall(string gameDirectory)
    {
        var log = new List<string>();
        var lines = new List<string> { "# The black car's case file, as its pages show it (LBA Assembler, RaceTrackStory.CaseFile.cs): page <var> <least>|<island>|<title>|<note>|<where>" };
        foreach (var p in CasePages) lines.Add($"page {p.Var} 1|{p.Island}|{p.Title}|{p.Note}|{p.Where}");
        File.WriteAllLines(Path.Combine(gameDirectory, CaseFileName), lines);

        var objfixPath = Path.Combine(gameDirectory, "OBJFIX.HQR");
        var header = Body.Read(HqrArchive.Open(Path.Combine(gameDirectory, "BODY.HQR")).Read(30), 2, allowStatic: true).Header;
        HqrWriter.ReplaceInFile(objfixPath, CaseSlot, InventorySized(BuildCaseFile(header)).Write());

        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        var hqr = File.ReadAllBytes(textPath);
        var written = 0;
        for (var lang = 0; lang < languages; lang++)
        {
            var bank = Lba2TextBank.Load(text, lang, HolomapTexts);
            var words = CaseTexts[lang < CaseTexts.Length ? lang : 0];
            for (var k = 0; k < 3; k++)
            {
                var id = CaseSlot + 100 * k;
                var wanted = Dos.GetBytes(words[k]);
                var have = bank.Find(id);
                if (have is null) bank.Texts.Add(new Lba2TextBank.Text { Id = id, Attribute = Lba2TextBank.NormalAttribute, Bytes = wanted });
                else if (!have.Bytes.AsSpan().SequenceEqual(wanted)) have.Bytes = wanted;
                else continue;
                written++;
            }
            hqr = bank.WriteInto(hqr);
        }
        if (written > 0) File.WriteAllBytes(textPath, hqr);
        log.Add($"the black car's case file: slot {CaseSlot} (the tart's), OBJFIX.HQR {CaseSlot} the folder, texts {CaseSlot}/{CaseSlot + 100}/{CaseSlot + 200} of file {HolomapTexts} ({written} written), {CaseFileName} {CasePages.Length} pages (variables {string.Join(", ", CasePages.Select(p => p.Var))})");
        return log;
    }

    // Wannies Island: Lucy's tart picked up and handed over without the slot (the case file's) -- the errand goes on by its variable, 64
    private static void TartOff(SceneStore store, List<string> log)
    {
        if (store.SceneExists(TartPickedUp))
            GroupB.EditScene(store, TartPickedUp, log, $"Lucy's tart picked up without the slot (item {CaseSlot} is the black car's case file)", _ => scripts =>
                scripts.SetText(0, ScriptKind.Life, Loose(scripts.GetText(0, ScriptKind.Life), $"found_object({CaseSlot});\nset_var_game({CaseSlot}, 1);", "")));
        if (store.SceneExists(TartHandedOver))
            GroupB.EditScene(store, TartHandedOver, log, $"Lucy's tart handed over without taking the slot (item {CaseSlot}: the case file stays)", _ => scripts =>
                scripts.SetText(TartTaker, ScriptKind.Life, Loose(scripts.GetText(TartTaker, ScriptKind.Life), $"set_used_inventory({CaseSlot});\nset_var_game({CaseSlot}, 0);", "")));
    }

    // The folder lying flat, face up (the inventory looks down on its things): a manila card, its edges bevelled, a tab at the back; the
    // black car's photo clipped to it -- a white print, dusk sky over sand, the car black on it -- a silver paperclip, a red stamp
    private static Body BuildCaseFile(byte[] header)
    {
        var m = new CarMesh("the black car's case file", new[] { -1 });
        m.Light = 0.7f;
        // (lit, a polygon's ramp is what counts -- the light picks the shade along it)
        const int Manila = 1 * 16 + 9, ManilaEdge = 1 * 16 + 6, Print = 3 * 16 + 9, Sky = 12 * 16 + 7, Sand = 1 * 16 + 7, CarBlack = 3 * 16 + 0,
            Window = 3 * 16 + 3, Clip = 3 * 16 + 8, Stamp = 4 * 16 + 4;
        const float Card = 80;
        Slab(m, new Vector3(0, 0, 0), 1500, 1100, Card, 24, Manila, ManilaEdge);
        Slab(m, new Vector3(-430, 0, -620), 480, 150, Card, 18, Manila, ManilaEdge);
        const float OnCard = Card, Pic = OnCard + 16;
        Slab(m, new Vector3(150, OnCard, -40), 780, 580, 16, 6, Print, Print);
        Slab(m, new Vector3(150, Pic, -130), 690, 320, 8, 3, Sky, Sky);
        Slab(m, new Vector3(150, Pic, 115), 690, 170, 8, 3, Sand, Sand);
        // (the car: its body, its cabin and window, its wheels)
        Slab(m, new Vector3(150, Pic + 8, 40), 440, 120, 40, 12, CarBlack, CarBlack);
        Slab(m, new Vector3(170, Pic + 8, -50), 230, 100, 50, 12, CarBlack, CarBlack);
        Slab(m, new Vector3(170, Pic + 58, -50), 170, 60, 6, 2, Window, Window);
        foreach (var wx in new[] { -10f, 310f })
            Slab(m, new Vector3(wx, Pic + 2, 100), 90, 50, 44, 14, CarBlack, CarBlack);
        // (the paperclip over the print's back edge, the stamp at the folder's front corner)
        Slab(m, new Vector3(440, OnCard + 16, -330), 70, 280, 22, 8, Clip, Clip);
        Slab(m, new Vector3(-470, OnCard, 380), 400, 150, 12, 4, Stamp, Stamp);
        return m.ToBody(header);
    }

    // A slab lying flat from c.Y up `h`, `w` across by `d` deep about (c.X, c.Z), its top edges bevelled by `b`: the top inset, four
    // chamfers, four sides and the bottom, each facing out (the points shared: lit smoothly round the bevel)
    private static void Slab(CarMesh m, Vector3 c, float w, float d, float h, float b, int colour, int side)
    {
        float x0 = c.X - w / 2, x1 = c.X + w / 2, z0 = c.Z - d / 2, z1 = c.Z + d / 2, y0 = c.Y, y1 = c.Y + h - b, y2 = c.Y + h;
        int P(float x, float y, float z) => m.P(0, new Vector3(x, y, z));
        var bottom = new[] { P(x0, y0, z0), P(x1, y0, z0), P(x1, y0, z1), P(x0, y0, z1) };
        var ring = new[] { P(x0, y1, z0), P(x1, y1, z0), P(x1, y1, z1), P(x0, y1, z1) };
        var top = new[] { P(x0 + b, y2, z0 + b), P(x1 - b, y2, z0 + b), P(x1 - b, y2, z1 - b), P(x0 + b, y2, z1 - b) };
        var inside = new Vector3(c.X, c.Y + h / 2, c.Z);
        m.Out(top, colour, inside);
        for (var i = 0; i < 4; i++)
        {
            var j = (i + 1) % 4;
            m.Out(new[] { ring[i], ring[j], top[j], top[i] }, colour, inside);
            m.Out(new[] { bottom[i], bottom[j], ring[j], ring[i] }, side, inside);
        }
        m.Out(bottom, side, inside);
    }
}
