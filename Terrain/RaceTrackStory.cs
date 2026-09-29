using System.Buffers.Binary;
using System.IO;
using System.Text;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The race track mod's story, on Citadel Island: the game opens with Zoe walking up to Twinsen in their house (scene 0, actor 4) to send
// him to the downtown pharmacy -- her line (text 0 of Citadel's texts) and the holomap arrow she switches on (set_holo_pos(22), the
// pharmacy). The mod keeps her walk and has her send him to the race track instead: a line of its own, and an arrow of its own on the
// start line.
//   - The line is a new text at the end of Citadel's text file, in every language the game has (TEXT.HQR, code page 850 like the game's
//     own). At the end, so it has no recorded voice (MESSAGE.CPP Speak: a text past the last sample is shown, not spoken) and every text
//     before it keeps its own.
//   - The arrow is holomap position 222 (HOLOMAP.HQR entry 12, record 50 + 222): positions are scene numbers -- a scene's record is where
//     it lies on the island, used for its arrow and for "Twinsen is here" -- and the game has 222 scenes, so 222-254 are no scene's and no
//     script uses them. Its label is a new text in the holomap's text file (file 2), like the pharmacy's "Downtown Pharmacy.".
//   - Racing gloves, in the darts' place in Twinsen's attic (scene 1: the darts lie on a shelf, actor 8, and pressing Action there -- zone 5
//     -- gives them). The inventory is 40 fixed slots, all the game's (a save stores exactly 40, and item n's count is game variable n, 40
//     being the Dino-Fly quest), so the gloves take a slot the mod has no use for: 7, the part Baldino gives for Zoe to mend the car -- the
//     mod has the buggy ready from the start. Not the darts' own slot: the darts are sold in the shop and found elsewhere too, and stay.
//     The slot's model (OBJFIX.HQR 7) and its texts (file 2: 7 the found message, 107 the name, 207 the description) become the gloves'; the
//     attic's shelf shows the gloves (a body of its own on the darts' entity) and gives them. Those three texts are spoken by the narrator
//     (EN_GAM.VOX), so each keeps its place in the file under an id nothing asks for -- every text after it keeps its voice -- and the gloves'
//     text goes at the end, where it has none.
internal static class RaceTrackStory
{
    public const int StartArrow = 222;
    private const int OpeningScene = 0, Zoe = 4;
    // the gloves: the slot, the attic, its shelf actor and the zone Action gives the darts in, and the darts' display entity and its body
    public const int GlovesSlot = 7;
    private const int Attic = 1, Shelf = 8, DartsEntity = 18, DartsBody = 31, CarScene = 49;
    private const int CitadelTexts = 3, HolomapTexts = 2;
    private const byte LabelAttribute = 17;          // the holomap labels' own (text 501 "Downtown Pharmacy.")
    private const int ArrowRecordSize = 32;

    // English, French, German, Spanish, Italian, Portuguese: TEXT.HQR's own order
    private static readonly string[] ZoeLine =
    {
        "Twinsen, the new race track is finally built, head to the start line behind the house to qualify. Don't forget your racing gloves.",
        "Twinsen, le nouveau circuit est enfin terminé, va à la ligne de départ derrière la maison pour te qualifier. N'oublie pas tes gants de course.",
        "Twinsen, die neue Rennstrecke ist endlich fertig, geh zur Startlinie hinter dem Haus und qualifiziere dich. Vergiss deine Rennhandschuhe nicht.",
        "Twinsen, el nuevo circuito por fin está terminado, ve a la línea de salida detrás de la casa para clasificarte. No olvides tus guantes de carreras.",
        "Twinsen, la nuova pista è finalmente pronta, vai alla linea di partenza dietro casa per qualificarti. Non dimenticare i tuoi guanti da corsa.",
        "Twinsen, a nova pista de corrida finalmente está pronta, vá até a linha de largada atrás da casa para se classificar. Não esqueça suas luvas de corrida.",
    };
    // the gloves' texts: found, name, description
    private static readonly string[][] GlovesTexts =
    {
        new[] { "You have found a pair of racing gloves.", "Racing gloves", "Your racing gloves: a firm grip on the buggy's steering wheel, lap after lap." },
        new[] { "Tu as trouvé une paire de gants de course.", "Gants de course", "Tes gants de course : une bonne prise sur le volant du buggy, tour après tour." },
        new[] { "Du hast ein Paar Rennhandschuhe gefunden.", "Rennhandschuhe", "Deine Rennhandschuhe: fester Griff am Lenkrad des Buggys, Runde für Runde." },
        new[] { "Has encontrado un par de guantes de carreras.", "Guantes de carreras", "Tus guantes de carreras: buen agarre en el volante del buggy, vuelta tras vuelta." },
        new[] { "Hai trovato un paio di guanti da corsa.", "Guanti da corsa", "I tuoi guanti da corsa: una presa salda sul volante del buggy, giro dopo giro." },
        new[] { "Você encontrou um par de luvas de corrida.", "Luvas de corrida", "Suas luvas de corrida: firmeza no volante do buggy, volta após volta." },
    };
    // (where each of those texts' old words stay: ids no script or engine asks for)
    private const int Retired = 60000;
    private static readonly string[] ArrowLabel =
    {
        "Race track start line.",
        "Ligne de départ du circuit.",
        "Startlinie der Rennstrecke.",
        "Línea de salida del circuito.",
        "Linea di partenza della pista.",
        "Linha de largada da pista.",
    };

    // The gloves' models and the attic: the shelf shows and gives the gloves instead of the darts; and the car part's one use (letting Zoe
    // mend the car, scene 49) no longer takes them away.
    private static List<string> Gloves(string gameDirectory, SceneStore store)
    {
        var log = new List<string> { RaceTrackGloves.InstallInventory(gameDirectory, GlovesSlot) };
        var (generic, bodyLog) = RaceTrackGloves.InstallInRoom(gameDirectory, DartsEntity, DartsBody);
        log.Add(bodyLog);

        var attic = store.Load(Attic);
        if (attic.Actors.Count <= Shelf || attic.Actors[Shelf].Entity != DartsEntity) { log.Add($"no gloves: scene {Attic}'s shelf isn't the darts'"); return log; }
        attic.Actors[Shelf].Body = generic;
        var scripts = SceneScripts.Load(SceneSerializer.Write(attic), Attic);
        var hero = scripts.GetText(0, ScriptKind.Life);
        var shelf = scripts.GetText(Shelf, ScriptKind.Life);
        if (!hero.Contains($"kill_obj({Shelf});") || !hero.Contains("found_object(2);") || !shelf.Contains("if (3 == var_game(2))"))
        {
            log.Add($"no gloves: scene {Attic}'s scripts aren't the game's own");
            return log;
        }
        // (the one found_object(2) in the attic is the shelf's; its other finds are the holomap's and the magic ball's)
        scripts.SetText(0, ScriptKind.Life, hero.Replace("found_object(2);", $"found_object({GlovesSlot});\n                    set_var_game({GlovesSlot}, 1);"));
        scripts.SetText(Shelf, ScriptKind.Life, shelf.Replace("if (3 == var_game(2))", $"if (0 < var_game({GlovesSlot}))"));
        var built = scripts.Build();
        if (!built.Ok) { log.Add("no gloves: the attic's scripts would not compile: " + string.Join("; ", built.Errors)); return log; }
        var changes = new List<SceneChange> { new(Attic, SceneSerializer.Parse(SceneGame.Lba2, built.Record!), null) };

        var car = store.Load(CarScene);
        var carScripts = SceneScripts.Load(SceneSerializer.Write(car), CarScene);
        var carHero = carScripts.GetText(0, ScriptKind.Life);
        var use = $"1 == use_inventory({GlovesSlot})";
        if (carHero.Contains(use))
        {
            // (use_inventory answers 0 or 1: asking for 2 never comes true, and the script keeps its shape)
            carScripts.SetText(0, ScriptKind.Life, carHero.Replace(use, $"2 == use_inventory({GlovesSlot})"));
            var carBuilt = carScripts.Build();
            if (carBuilt.Ok) changes.Add(new(CarScene, SceneSerializer.Parse(SceneGame.Lba2, carBuilt.Record!), null));
            else log.Add("scene 49: the car part's use could not be switched off: " + string.Join("; ", carBuilt.Errors));
        }
        store.SaveMany(changes, allowErrors: true);
        log.Add($"scene {Attic}: the attic's shelf shows the racing gloves (entity {DartsEntity} body {generic}) and Action there gives them (item {GlovesSlot}); the darts are still in the shop");
        return log;
    }

    private static Encoding Dos
    {
        get
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(850);
        }
    }

    // Returns lines for the log. The start line must have been placed (report.StartLine).
    public static List<string> Apply(string gameDirectory, RaceTrackReport report)
    {
        var log = new List<string>();
        if (report.StartLine.Count == 0) { log.Add("no story: the track has no start line"); return log; }

        // the texts
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        int NewId(int file) => Enumerable.Range(0, languages).Max(l => Lba2TextBank.Load(text, l, file).Texts.Select(t => t.Id).DefaultIfEmpty(0).Max()) + 1;
        var lineId = NewId(CitadelTexts); var labelId = NewId(HolomapTexts);
        var hqr = File.ReadAllBytes(textPath);
        for (var lang = 0; lang < languages; lang++)
        {
            var words = lang < ZoeLine.Length ? lang : 0;       // (a language this doesn't know gets the English)
            var line = Lba2TextBank.Load(text, lang, CitadelTexts);
            var zoeOwn = line.Find(0);
            line.Texts.Add(new Lba2TextBank.Text { Id = lineId, Attribute = zoeOwn?.Attribute ?? Lba2TextBank.NormalAttribute, Bytes = Dos.GetBytes(ZoeLine[words]) });
            hqr = line.WriteInto(hqr);
            var labels = Lba2TextBank.Load(text, lang, HolomapTexts);
            labels.Texts.Add(new Lba2TextBank.Text { Id = labelId, Attribute = LabelAttribute, Bytes = Dos.GetBytes(ArrowLabel[words]) });
            // (file 2 is also the inventory's: the gloves' found message, name and description, each moved out of its voiced place)
            var item = new[] { GlovesSlot, 100 + GlovesSlot, 200 + GlovesSlot };
            for (var k = 0; k < item.Length; k++)
            {
                var own = labels.Find(item[k]);
                var attribute = own?.Attribute ?? Lba2TextBank.NormalAttribute;
                if (own is not null) own.Id = Retired + item[k];
                labels.Texts.Add(new Lba2TextBank.Text { Id = item[k], Attribute = attribute, Bytes = Dos.GetBytes(GlovesTexts[words][k]) });
            }
            hqr = labels.WriteInto(hqr);
        }
        File.WriteAllBytes(textPath, hqr);
        log.Add($"Zoe's opening line is text {lineId} and the start line's holomap label text {labelId}, in all {languages} languages (shown, not spoken)");

        // the arrow: on the start line, on Citadel Island, off until Zoe switches it on
        var holoPath = Path.Combine(gameDirectory, RaceTrackHolomap.File);
        var holo = File.ReadAllBytes(holoPath);
        var arrows = HqrArchive.Open(holoPath).Read(12);
        var at = (50 + StartArrow) * ArrowRecordSize;
        if (arrows.Length < at + ArrowRecordSize) throw new InvalidDataException("The holomap's arrow table is shorter than the game's own.");
        var (x, z, y, _, _) = report.StartLine[0];
        var citadel = 50 + 49;          // (scene 49's record: Citadel Island's own place on the globe)
        void Put(int field, int value) => BinaryPrimitives.WriteInt32LittleEndian(arrows.AsSpan(at + field * 4), value);
        Put(0, (int)Math.Round(x * 512)); Put(1, (int)Math.Round(y)); Put(2, (int)Math.Round(z * 512));
        for (var f = 3; f < 6; f++) Put(f, BinaryPrimitives.ReadInt32LittleEndian(arrows.AsSpan(citadel * ArrowRecordSize + f * 4)));
        Put(6, labelId);
        arrows[at + 28] = 0xFF;         // no inventory object
        arrows[at + 29] = 0;            // off, never asked
        arrows[at + 30] = 0;            // planet Twinsun
        arrows[at + 31] = 0;            // Citadel Island
        holo = HqrWriter.ReplaceEntry(holo, 12, HqrWriter.StoredEntry(arrows));
        File.WriteAllBytes(holoPath, holo);
        log.Add($"holomap arrow {StartArrow} on the start line, cell ({x:0.0}, {z:0.0})");

        // Zoe
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var model = store.Load(OpeningScene);
        var scripts = SceneScripts.Load(SceneSerializer.Write(model), OpeningScene);
        var life = scripts.GetText(Zoe, ScriptKind.Life);
        if (!life.Contains("message(0);") || !life.Contains("set_holo_pos(22);"))
        {
            log.Add($"no story: Zoe's script in scene {OpeningScene} isn't the game's own");
            return log;
        }
        // (only the opening's arrow: her later reminder -- "did you find something to cure the Dino-Fly?" -- keeps pointing at the pharmacy)
        var said = life.IndexOf("message(0);", StringComparison.Ordinal);
        var arrow = life.IndexOf("set_holo_pos(22);", said, StringComparison.Ordinal);
        life = life[..arrow] + $"set_holo_pos({StartArrow});" + life[(arrow + "set_holo_pos(22);".Length)..];
        life = life.Replace("message(0);", $"message({lineId});");
        scripts.SetText(Zoe, ScriptKind.Life, life);
        var built = scripts.Build();
        if (!built.Ok) { log.Add("no story: Zoe's script would not compile: " + string.Join("; ", built.Errors)); return log; }
        store.SaveMany(new[] { new SceneChange(OpeningScene, SceneSerializer.Parse(SceneGame.Lba2, built.Record!), null) }, allowErrors: true);
        log.Add($"scene {OpeningScene}: Zoe walks up to Twinsen as ever and sends him to the start line (text {lineId}, arrow {StartArrow}) instead of the pharmacy, and reminds him of his racing gloves");
        log.AddRange(Gloves(gameDirectory, store));
        return log;
    }
}
