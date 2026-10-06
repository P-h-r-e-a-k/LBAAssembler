using System.IO;
using System.Text;

namespace LBAAssembler.Terrain.Polar;

// Polar Island's dream race (the user's, 2026-10-06): the race track picks up from the end of the first game. Twinsen is dreaming: he is
// back on Polar Island and FunFrock has escaped, racing him to Sendell to get to her first. A sprint from the dock to the top of the
// rocky peak (RaceTrackIsland.Polar, docs/racetrack/polar_track_plan.json). Won, Twinsen is shaken awake by Zoe at home, in his bed in
// the second game's first scene (scene 0); lost, the race is run again. The race-track mode does it (RACEMOD.CPP sprint=, wake=):
//   - the intro, said by Twinsen as the grid forms, and the loss's line: texts 1 and 2 of the island's own text file (island 12's,
//     PolarScenes.WriteTexts), next to its name;
//   - the wake line, said by Zoe (scene 0's actor 4) a moment after Twinsen wakes up: a text at the end of Citadel Island's file, where
//     scene 0's texts are (no recorded voice: shown, not spoken), before her own opening line.
// The texts are in every language the game has, in its own order (English, French, German, Spanish, Italian, Portuguese) and code page.
internal static class PolarDream
{
    public const int IntroText = 1, LoseText = 2;
    // Twinsen wakes up in the game's first scene, at its own start (the foot of his bed), and Zoe says the wake line
    public const int WakeScene = 0, WakeActor = 4;
    // the car's top speed for the race (km/h): the race car setup's gears scaled to it (its normal top is 80)
    public const int TopKmh = 140;
    private const int CitadelTexts = 3;

    private static readonly string[] Intro =
    {
        "Polar Island? FunFrock has escaped, and he's racing to Sendell! If he gets to her first, the whole planet is lost. I have to beat him to the top of the rocky peak!",
        "L'île Polaire ? FunFrock s'est échappé et il file vers Sendell ! S'il l'atteint le premier, toute la planète est perdue. Je dois arriver avant lui au sommet du pic rocheux !",
        "Die Polarinsel? FunFrock ist entkommen und rast zu Sendell! Wenn er sie zuerst erreicht, ist der ganze Planet verloren. Ich muss vor ihm oben auf dem Felsgipfel sein!",
        "¿La Isla Polar? ¡FunFrock ha escapado y corre hacia Sendell! Si llega a ella primero, todo el planeta estará perdido. ¡Tengo que llegar antes que él a la cima del pico rocoso!",
        "L'Isola Polare? FunFrock è fuggito e corre verso Sendell! Se arriva da lei per primo, l'intero pianeta è perduto. Devo arrivare in cima al picco roccioso prima di lui!",
        "A Ilha Polar? O FunFrock fugiu e corre para Sendell! Se chegar primeiro, o planeta inteiro está perdido. Tenho de chegar ao cimo do pico rochoso antes dele!",
    };

    private static readonly string[] Lose =
    {
        "No! FunFrock got to Sendell first... It can't end like this. Again!",
        "Non ! FunFrock a atteint Sendell le premier... Ça ne peut pas finir comme ça. Encore !",
        "Nein! FunFrock hat Sendell zuerst erreicht... So darf es nicht enden. Noch einmal!",
        "¡No! FunFrock ha llegado antes a Sendell... No puede terminar así. ¡Otra vez!",
        "No! FunFrock è arrivato da Sendell per primo... Non può finire così. Ancora!",
        "Não! O FunFrock chegou primeiro a Sendell... Não pode acabar assim. Outra vez!",
    };

    private static readonly string[] Wake =
    {
        "Twinsen! Twinsen, wake up! You were tossing and turning all night. Were you dreaming about FunFrock again?",
        "Twinsen ! Twinsen, réveille-toi ! Tu t'es agité toute la nuit. Tu rêvais encore de FunFrock ?",
        "Twinsen! Twinsen, wach auf! Du hast dich die ganze Nacht hin und her gewälzt. Hast du wieder von FunFrock geträumt?",
        "¡Twinsen! ¡Twinsen, despierta! Te has pasado la noche dando vueltas. ¿Estabas soñando otra vez con FunFrock?",
        "Twinsen! Twinsen, svegliati! Ti sei agitato tutta la notte. Stavi di nuovo sognando FunFrock?",
        "Twinsen! Twinsen, acorda! Andaste às voltas a noite toda. Estavas outra vez a sonhar com o FunFrock?",
    };

    // The island's own texts after its name, in a language (a language this doesn't know: the English).
    public static IEnumerable<(int Id, string Text)> IslandTexts(int language)
    {
        var words = language < Intro.Length ? language : 0;
        yield return (IntroText, Intro[words]);
        yield return (LoseText, Lose[words]);
    }

    // The race's texts into a game folder: the island's (its text file written again with them) and the wake line at the end of Citadel
    // Island's texts. Returns lines for the log and the wake line's text.
    public static (List<string> Log, int WakeText) Apply(string gameDirectory)
    {
        var log = new List<string> { PolarScenes.WriteTexts(gameDirectory) };
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Math.Min(Lba2TextBank.Languages(textPath), PolarScenes.Languages);
        var id = Enumerable.Range(0, languages).Max(l => Lba2TextBank.Load(text, l, CitadelTexts).Texts.Select(t => t.Id).DefaultIfEmpty(0).Max()) + 1;
        var hqr = File.ReadAllBytes(textPath);
        for (var lang = 0; lang < languages; lang++)
        {
            var lines = Lba2TextBank.Load(text, lang, CitadelTexts);
            var attribute = lines.Find(0)?.Attribute ?? Lba2TextBank.NormalAttribute;
            lines.Texts.Add(new Lba2TextBank.Text { Id = id, Attribute = attribute, Bytes = Dos.GetBytes(Wake[lang < Wake.Length ? lang : 0]) });
            hqr = lines.WriteInto(hqr);
        }
        File.WriteAllBytes(textPath, hqr);
        log.Add($"the dream's end: Zoe's wake line is text {id} of Citadel Island's, in all {languages} languages; Twinsen wakes up in scene {WakeScene}");
        return (log, id);
    }

    internal static Encoding Dos
    {
        get
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(850);
        }
    }
}
