using System.IO;
using System.Text.RegularExpressions;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The race mod's story on the White Leaf Desert, chapter 3 (docs/LBRA_WALKTHROUGH.md; the build notes: docs/LBRA_STORY_SCRIPT.md), and
// what the story's islands of "group A" share (the Desert, the Emerald Moon, Otringal, the Island of the Francos: IslandBeatsA).
//   - Arriving: on the island's outside scenes the radio rings (once a chapter: "Answer it from your inventory"), and the radio used from
//     the inventory is Baldino's call -- his tip for the track; the car update itself is the race file's best setup. (Chapter 2's town
//     circuit win leaves the game at chapter 2: arriving on the Desert makes it 3.)
//   - Flick, the marshal, at her plinth by the start line: the Dean has shut the track over three exam pages (Q_PAGES 1).
//   - The Dean in the School of Magic (scene 27; the game's own Dean is out of sight in the story, a copy of him talks): bring them back
//     (Q_PAGES 2), Zoe's word on where they blew; the three pages lie on the road at 30, 55 and 80 % of the lap -- driven over or walked
//     to, in lap order (Q_PAGES 3, 4, 5); back to him: the Wizard's Diploma (item 30, Q_PAGES 6), which opens the track (the race file's
//     gate=215 6).
//   - Baldino at his house (scene 38: the game's own out of sight, a copy talks): the sand filters and his tip, and he races too.
//   - Won (the race file's win=215 7): out of the car (by the start line, each speaker's own voice; in another outside scene, the radio's
//     controller says the same lines) -- Flick, the racer, Moya's clue of the oil on the last bend and
//     the black car, Zoe, and Baldino's rocket to the moon (inc_chapter: 4; change_cube(229), the Emerald Moon's start).
internal static partial class RaceTrackStory
{
    // Q_PAGES (docs/LBRA_STORY_PLAN.md 3.1): 1 Flick has told, 2 the Dean has, 3-5 the pages found, 6 the diploma, 7 the race won, 8 told
    public const int QPages = 215, PagesDiploma = 6, PagesWon = 7, PagesDone = 8;
    // the group's two spare game variables: the chapter whose radio call was answered, and the chapter whose ringing was shown
    // (not 235-237: the game keeps which videos have been seen in their bits, PLAYACF.CPP FLAG_ACF -- 2026-11-04)
    public const int RadioAnsweredA = 234, RadioRangA = 240;
    private const int Diploma = 30, DeanScene = 27, DeanActor = 8, BaldinoHouse = 38, BaldinoActor = 2, MoonStart = 229;
    // the game's own: the variable of its Dino-Fly's cure (1 sent to Ker'aooc, 2 his assistant has answered, 3 the School's hall seen)
    // and the holomap's arrow to Ker'aooc's house; Ker'aooc's house, its assistant; the Pearl of Incandescence's cave
    private const int CuredDinoFly = 62, KeraoocArrow = 35, KeraoocHouse = 35, KeraoocAssistant = 2, PearlCave = 113;
    // (where the Dean stands in the School's hall, the game's and the 1996 demo's alike)
    private static readonly (int X, int Z) DeanAt = (15104, 5040);

    // The race file's story keys of the group's islands (RaceCarEngineFile.EngineFile, in the story): their gates and what a win sets.
    internal static string? RaceKeysA(RaceTrackIsland island) =>
        island == RaceTrackIsland.Desert ? $"gate={QPages} 6 Flick: the Dean has shut the track!\nwin={QPages} {PagesWon}\n"
        : island == RaceTrackIsland.Emerald ? $"gate={QHal} {HalMended} Lyra: HAL has locked the start gate!\nwin={QHal} {HalWon}\n"
        : island == RaceTrackIsland.Otringal ? $"gate={QFee} {FeePaid} The steward: a Travel Token first, 30 zlitos!\nwin={QFee} {FeeWon}\n"
        : island == RaceTrackIsland.Knartas ? $"gate={QFuel} {FuelOpen} De La Fontaine: not yet, Twinsen!\nwin={QFuel} {FuelWon}\n"
        : null;

    // The group's islands' story, before their people are retold (RaceTrackService.Story). `report`: the island's track, `scenes`: what its
    // scenes were given.
    internal static List<string> ApplyGroupA(string gameDirectory, RaceTrackReport report, RaceTrackIsland island, RaceTrackScenes.Result scenes)
    {
        try
        {
            if (island == RaceTrackIsland.Desert) return ApplyDesert(gameDirectory, report, scenes);
            if (island == RaceTrackIsland.Emerald) return ApplyEmerald(gameDirectory, report, scenes);
            if (island == RaceTrackIsland.Otringal) return ApplyOtringal(gameDirectory, report, scenes);
            if (island == RaceTrackIsland.Knartas) return ApplyFrancos(gameDirectory, report, scenes);
        }
        catch (Exception e) when (e is NotTheGames or InvalidDataException or IOException or ArgumentException)
        {
            return new List<string> { $"{island.Name}: the story left out: {e.Message}" };
        }
        return new List<string>();
    }

    // What an island's beats share: its scenes (as built, and before any build), each edited once and saved together, the log.
    private sealed class IslandBeatsA(string gameDirectory, RaceTrackIsland island, List<string> log)
    {
        public readonly SceneStore Store = new(SceneGame.Lba2, gameDirectory);
        public SceneStore? originals;
        public SceneStore Originals => originals ??= RaceTrackService.OriginalScenes(Store);
        private readonly Dictionary<int, SceneModel> models = new();
        public readonly List<string> Log = log;
        public readonly RaceTrackIsland Island = island;
        private SceneModel Model(int scene) => models.TryGetValue(scene, out var m) ? m : models[scene] = Store.Load(scene);

        // RaceTrackStory.Apply's Edit: the scene's actors and zones changed, then its scripts, compiled; left out (and logged) otherwise
        public void Edit(int scene, string what, Func<SceneModel, Action<SceneScripts>> edit)
        {
            try
            {
                var model = SceneSerializer.Parse(SceneGame.Lba2, SceneSerializer.Write(Model(scene)));
                var scriptEdit = edit(model);
                var scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
                scriptEdit(scripts);
                var built = scripts.Build();
                if (!built.Ok) { Log.Add($"scene {scene}: {what}: left out, the scripts would not compile: {string.Join("; ", built.Errors)}"); return; }
                models[scene] = SceneSerializer.Parse(SceneGame.Lba2, built.Record!);
                Log.Add($"scene {scene}: {what}");
            }
            catch (Exception e) when (e is NotTheGames or ScriptCompileException or InvalidDataException or ArgumentException or SceneEditException)
            {
                Log.Add($"scene {scene}: {what} left out: {(e is NotTheGames ? "the script isn't the game's own: " : "")}{e.Message}");
            }
        }

        // someone added to a scene with a life script of their own
        public static Action<SceneScripts> Add(SceneModel model, SceneActorModel actor, string life)
        {
            actor.Life = new byte[] { 0 }; actor.Track = new byte[] { 0 };
            GiveVoice(actor);
            var index = SceneOps.AddActor(model, actor);
            return scripts =>
            {
                scripts.SetText(index, ScriptKind.Life, life);
                scripts.SetText(index, ScriptKind.Track, "label(0);\nstop();\n");
            };
        }

        // the island's outside scene whose cube holds an island cell (-1: none)
        public int SceneAt(double x, double z)
        {
            foreach (var s in Island.Scenes)
            {
                if (!Store.SceneExists(s)) continue;
                var m = Store.Load(s);
                if (m.CubeMode == 1 && m.CubeX == (int)Math.Floor(x / 64) && m.CubeY == (int)Math.Floor(z / 64)) return s;
            }
            return -1;
        }

        public IEnumerable<int> OutsideScenes => Island.Scenes.Where(s => Store.SceneExists(s) && Store.Load(s).CubeMode == 1);

        // A game person's script out of sight while `when` holds when the scene starts (a copy of them talks in the story)
        public void Hide(int scene, int actor, string when, string who) => Edit(scene, $"the game's {who} out of sight while {when} (a copy talks for the story)", _ => scripts =>
        {
            var life = scripts.GetText(actor, ScriptKind.Life);
            scripts.SetText(actor, ScriptKind.Life, Loose(life, "void comportement_0()\n{", $@"void comportement_0()
{{
    if ({when})
    {{
        suicide();
    }}
    else"));
        });

        public void Save() => Store.SaveMany(models.Select(m => new SceneChange(m.Key, m.Value, null)).ToList(), allowErrors: true);
    }

    // An island's new lines at the end of its text file (island + 3), in every language; the first one's id, or -1 when they're there
    // already (a second pass over the same files)
    private static int AddIslandTexts(string gameDirectory, int island, string[][] lines, List<string> log, string name)
    {
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        var file = island + 3;
        var english = Lba2TextBank.Load(text, 0, file);
        var firstWords = System.Text.Encoding.Latin1.GetString(Dos.GetBytes(lines[0][0]));
        if (english.Texts.Any(t => t.Value == firstWords)) { log.Add($"{name}: the story's lines are there already"); return -1; }
        var first = NextId(text, languages, file);
        var hqr = File.ReadAllBytes(textPath);
        for (var lang = 0; lang < languages; lang++)
        {
            var bank = Lba2TextBank.Load(text, lang, file);
            AddTexts(bank, first, Lba2TextBank.NormalAttribute, lines, lang < lines[0].Length ? lang : 0);
            hqr = bank.WriteInto(hqr);
        }
        File.WriteAllBytes(textPath, hqr);
        log.Add($"{name}: the story's {lines.Length} lines are texts {first}-{first + lines.Length - 1} of file {file}, in all {languages} languages");
        return first;
    }

    // The radio, on every outside scene of an island in its chapter (`chapter`): it rings once (the line), and used from the inventory it
    // is Baldino's call (`call`, his lines). `arrive`: what the first scene of the island does on arrival, before (the chapter put right);
    // `extra`: more of its script each frame (the Desert's after-race lines, away from the start scene).
    private static string RadioController(int chapter, int ringing, int[] call, string arrive = "", string extra = "") => $@"void comportement_0()
{{
{arrive}    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if ({chapter} == chapter() && 0 < var_game({Radio}) && {chapter} != var_game({RadioAnsweredA}))
    {{
        if ({chapter} != var_game({RadioRangA}) && {Driving} != comportement_hero())
        {{
            set_var_game({RadioRangA}, {chapter});
            message({ringing});
        }}
        if (1 == use_inventory({Radio}))
        {{
            set_var_game({RadioAnsweredA}, {chapter});
{string.Concat(call.Select(c => $"            message({c});\n"))}        }}
    }}
{extra}}}
";

    // Where the race's people stand to speak after it: beside the start line, a few cells past it, off the roads (or on it, at worst)
    private static (double X, double Z, double Y, int Beta) AfterRaceSpot(RaceTrackReport report)
    {
        var s = report.StartLine[0];
        return Beside(report, s, -4, 1, new[] { 1, -1 }) ?? (s.X, s.Z, s.Y, 0);
    }

    // An invisible voice (a dialog's colour: who is speaking) at a place in the scene
    private static SceneActorModel Voice(SceneModel model, (double X, double Z, double Y, int Beta) at, int colour) =>
        RaceTrackScenes.InvisibleHelper(Math.Clamp(RaceTrackScenes.CubeLocal(at.X, model.CubeX), 0, IslandFile.CubeSize - 1), (int)Math.Round(at.Y),
            Math.Clamp(RaceTrackScenes.CubeLocal(at.Z, model.CubeY), 0, IslandFile.CubeSize - 1), colour);

    // A talker at the flag marshal's place by a track's start line (Chapters.cs Marshal, for any island)
    private static void MarshalA(IslandBeatsA b, RaceTrackReport track, string when, string talk, string who)
    {
        if (track.Marshal is not { Length: >= 10 } m) { b.Log.Add($"{who} at the start line left out: no flag marshal there"); return; }
        double x = m[6] / 512.0, z = m[7] / 512.0;
        var scene = b.SceneAt(x, z);
        if (scene < 0) { b.Log.Add($"{who} at the start line left out: no scene at cell ({x:0.#}, {z:0.#})"); return; }
        var y = track.GroundAfter?.Invoke(x, z) ?? m[8];
        b.Edit(scene, $"{who}, the marshal, talks at the start line (cell {x:0.#}, {z:0.#})", model =>
        {
            var marshal = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(x, model.CubeX), (int)Math.Round(Math.Max(y, m[8])), RaceTrackScenes.CubeLocal(z, model.CubeY), 9);
            return IslandBeatsA.Add(model, marshal, Talker(when, talk, 1800));
        });
    }

    // A place on the lap's road at a fraction of it (from the start line), on the ground: not on a raised road nor in a jump's gap
    private static (double X, double Z, double Y)? OnLap(RaceTrackReport report, double fraction)
    {
        var n = report.LapX.Length;
        if (n == 0 || report.LapY.Length != n) return null;
        for (var k = 0; k < n / 10; k++)
        {
            var i = ((int)(fraction * n) + k) % n;
            if (report.LapRaised?[i] == true || report.LapArc?[i] == true) continue;
            return (report.LapX[i], report.LapZ[i], report.LapY[i]);
        }
        return null;
    }

    // ---- the White Leaf Desert --------------------------------------------------------------------------------------------------------
    private enum DesertLine
    {
        Ringing, BaldinoRadio, FlickShut, FlickPages, FlickOpen, DeanShut, ZoePages, ZoeWhere, Page1, Page2, Page3, DeanAll,
        BaldinoTuned, BaldinoRacing, FlickWon, RacerNext, MoyaOil, ZoeOil, BaldinoRocket,
        // (the easter eggs: RaceTrackStory.Eggs.Desert.cs)
        PageEgg, DeanNotMine, FlickNoIdea, FlickBox, RunesUnread, RunesRecord, RunesExcept, DeanBu,
        // (the sweep of 2026-11-05)
        HallWelcome, BaldinoRadioWorks, BaldinoNoCall,
    }
    private static readonly string[][] DesertLines =
    {
        new[]
        {
            "The radio is ringing. Answer it from your inventory.",
            "La radio sonne. Réponds depuis ton inventaire.",
            "Das Funkgerät klingelt. Nimm den Anruf in deinem Inventar an.",
            "La radio está sonando. Contesta desde tu inventario.",
            "La radio sta squillando. Rispondi dal tuo inventario.",
            "O rádio está a tocar. Atende a partir do teu inventário.",
        },
        new[]
        {
            "Hello? Twinsen? That's it, I'm at my house on the Desert. Come and see me before you race: your car's going to want sand filters.",
            "Allô ? Twinsen ? Ça y est, je suis chez moi, sur le Désert. Passe me voir avant la course : ta voiture va avoir besoin de filtres à sable.",
            "Hallo? Twinsen? So, ich bin in meinem Haus in der Wüste. Komm vor dem Rennen bei mir vorbei: Dein Auto wird Sandfilter brauchen.",
            "¿Hola? ¿Twinsen? Ya está, estoy en mi casa del Desierto. Ven a verme antes de la carrera: tu coche va a necesitar filtros de arena.",
            "Pronto? Twinsen? Ecco, sono a casa mia, nel Deserto. Passa a trovarmi prima della gara: la tua macchina avrà bisogno di filtri per la sabbia.",
            "Estou? Twinsen? Pronto, estou em minha casa, no Deserto. Passa por cá antes da corrida: o teu carro vai precisar de filtros de areia.",
        },
        new[]
        {
            "Twinsen! Good to see you again. Bad news: my Dean has shut the track. The engines blew his students' exam papers all over the island. Three pages. He's at the School, and he's in a mood.",
            "Twinsen ! Content de te revoir. Mauvaise nouvelle : mon Doyen a fermé le circuit. Les moteurs ont éparpillé les copies d'examen de ses élèves dans toute l'île. Trois pages. Il est à l'École, et il est de mauvaise humeur.",
            "Twinsen! Schön, dich wiederzusehen. Schlechte Nachrichten: Mein Dekan hat die Strecke gesperrt. Die Motoren haben die Prüfungsblätter seiner Schüler über die ganze Insel geweht. Drei Seiten. Er ist in der Schule, und er hat schlechte Laune.",
            "¡Twinsen! Me alegro de verte. Malas noticias: mi Decano ha cerrado el circuito. Los motores han esparcido los exámenes de sus alumnos por toda la isla. Tres páginas. Está en la Escuela, y está de mal humor.",
            "Twinsen! Che piacere rivederti. Brutte notizie: il mio Rettore ha chiuso la pista. I motori hanno sparso i compiti d'esame dei suoi studenti per tutta l'isola. Tre pagine. È alla Scuola, ed è di cattivo umore.",
            "Twinsen! Que bom ver-te outra vez. Más notícias: o meu Reitor fechou a pista. Os motores espalharam os exames dos alunos dele pela ilha toda. Três páginas. Está na Escola, e está de mau humor.",
        },
        new[]
        {
            "Twinsen! No flag yet, I'm afraid: the track stays shut till the Dean at the School of Magic has his three pages back. He means it.",
            "Twinsen ! Pas encore de drapeau, désolée : le circuit reste fermé tant que le Doyen de l'École de Magie n'a pas récupéré ses trois pages. Il ne plaisante pas.",
            "Twinsen! Noch keine Flagge, leider: Die Strecke bleibt gesperrt, bis der Dekan der Zauberschule seine drei Seiten zurückhat. Er meint es ernst.",
            "¡Twinsen! Todavía no hay bandera, lo siento: el circuito sigue cerrado hasta que el Decano de la Escuela de Magia recupere sus tres páginas. Va en serio.",
            "Twinsen! Ancora niente bandiera, mi spiace: la pista resta chiusa finché il Rettore della Scuola di Magia non riavrà le sue tre pagine. Fa sul serio.",
            "Twinsen! Ainda não há bandeira, lamento: a pista fica fechada até o Reitor da Escola de Magia ter as três páginas de volta. Ele não está a brincar.",
        },
        new[]
        {
            "The Dean's diploma! Then the track is open. Line up whenever you're ready.",
            "Le diplôme du Doyen ! Alors le circuit est ouvert. Mets-toi en place quand tu veux.",
            "Das Diplom des Dekans! Dann ist die Strecke offen. Stell dich auf, wann immer du bereit bist.",
            "¡El diploma del Decano! Entonces el circuito está abierto. Colócate cuando quieras.",
            "Il diploma del Rettore! Allora la pista è aperta. Mettiti in griglia quando vuoi.",
            "O diploma do Reitor! Então a pista está aberta. Alinha quando quiseres.",
        },
        new[]
        {
            "Shut it is! Three pages, gone with the wind of your engines. Bring them back, and I'll think about it.",
            "Fermé, et bien fermé ! Trois pages, envolées avec le vent de vos moteurs. Rapportez-les, et j'y réfléchirai.",
            "Gesperrt ist gesperrt! Drei Seiten, vom Wind eurer Motoren davongeweht. Bring sie zurück, dann denke ich darüber nach.",
            "¡Cerrado está! Tres páginas, volando con el viento de vuestros motores. Tráelas de vuelta y me lo pensaré.",
            "Chiusa e basta! Tre pagine, volate via col vento dei vostri motori. Riportamele, e ci penserò.",
            "Fechada está! Três páginas, levadas pelo vento dos vossos motores. Traz-mas de volta, e eu penso nisso.",
        },
        new[]
        {
            "The pages blew along the road. If we drive the course we'll find them, and learn the bends while we're at it.",
            "Les pages se sont envolées le long de la route. Si on fait le tour du circuit, on les trouvera, et on apprendra les virages en même temps.",
            "Die Seiten sind die Straße entlang geweht. Wenn wir die Strecke abfahren, finden wir sie und lernen nebenbei die Kurven.",
            "Las páginas volaron a lo largo de la carretera. Si recorremos el circuito las encontraremos, y de paso aprenderemos las curvas.",
            "Le pagine sono volate lungo la strada. Se facciamo il giro del circuito le troveremo, e intanto impareremo le curve.",
            "As páginas voaram ao longo da estrada. Se dermos a volta ao circuito encontramo-las, e aprendemos as curvas pelo caminho.",
        },
        new[]
        {
            "One by the first dunes, one past the Temple, one near the last bend. Drive the lap the way the race goes.",
            "Une près des premières dunes, une après le Temple, une près du dernier virage. Fais le tour dans le sens de la course.",
            "Eine bei den ersten Dünen, eine hinter dem Tempel, eine nahe der letzten Kurve. Fahr die Runde in Rennrichtung.",
            "Una junto a las primeras dunas, otra pasado el Templo, otra cerca de la última curva. Recorre la vuelta en el sentido de la carrera.",
            "Una vicino alle prime dune, una dopo il Tempio, una vicino all'ultima curva. Fai il giro nel senso della gara.",
            "Uma junto às primeiras dunas, outra depois do Templo, outra perto da última curva. Dá a volta no sentido da corrida.",
        },
        new[]
        {
            "A page: Chapter 1, Levitation for Beginners.",
            "Une page : Chapitre 1, la Lévitation pour les débutants.",
            "Eine Seite: Kapitel 1, Schweben für Anfänger.",
            "Una página: Capítulo 1, Levitación para principiantes.",
            "Una pagina: Capitolo 1, Levitazione per principianti.",
            "Uma página: Capítulo 1, Levitação para principiantes.",
        },
        new[]
        {
            "Another page: Turning Sand into Slightly Different Sand.",
            "Une autre page : Transformer le sable en un sable légèrement différent.",
            "Noch eine Seite: Wie man Sand in etwas anderen Sand verwandelt.",
            "Otra página: Convertir arena en una arena ligeramente distinta.",
            "Un'altra pagina: Trasformare la sabbia in una sabbia leggermente diversa.",
            "Outra página: Transformar areia numa areia ligeiramente diferente.",
        },
        new[]
        {
            "The last page: Why Wizards Should Not Drive.",
            "La dernière page : Pourquoi les mages ne devraient pas conduire.",
            "Die letzte Seite: Warum Zauberer nicht Auto fahren sollten.",
            "La última página: Por qué los magos no deberían conducir.",
            "L'ultima pagina: Perché i maghi non dovrebbero guidare.",
            "A última página: Porque é que os feiticeiros não devem conduzir.",
        },
        new[]
        {
            "All three! Very well. Here: a diploma. It says you may use the School's grounds, which include the track. Don't let it go to your head.",
            "Les trois ! Très bien. Tenez : un diplôme. Il dit que vous pouvez utiliser les terrains de l'École, circuit compris. Que ça ne vous monte pas à la tête.",
            "Alle drei! Na gut. Hier: ein Diplom. Darauf steht, dass du das Gelände der Schule benutzen darfst, und dazu gehört die Strecke. Lass es dir nicht zu Kopf steigen.",
            "¡Las tres! Muy bien. Toma: un diploma. Dice que puedes usar los terrenos de la Escuela, circuito incluido. Que no se te suba a la cabeza.",
            "Tutte e tre! Molto bene. Ecco: un diploma. Dice che puoi usare i terreni della Scuola, pista compresa. Che non ti monti la testa.",
            "As três! Muito bem. Toma: um diploma. Diz que podes usar os terrenos da Escola, pista incluída. Não te subas à cabeça.",
        },
        new[]
        {
            "Sand filters, fitted. A longer top gear for the long straights. And a word of advice: the champion brakes late at the Temple bend. Brake later.",
            "Filtres à sable, posés. Une dernière vitesse plus longue pour les grandes lignes droites. Et un conseil : le champion freine tard au virage du Temple. Freine plus tard.",
            "Sandfilter, eingebaut. Ein längerer höchster Gang für die langen Geraden. Und ein Rat: Der Champion bremst spät in der Tempelkurve. Brems später.",
            "Filtros de arena, puestos. Una marcha larga más larga para las rectas. Y un consejo: el campeón frena tarde en la curva del Templo. Frena más tarde.",
            "Filtri per la sabbia, montati. Una marcia alta più lunga per i rettilinei. E un consiglio: il campione frena tardi alla curva del Tempio. Frena più tardi.",
            "Filtros de areia, montados. Uma mudança de cima mais longa para as retas. E um conselho: o campeão trava tarde na curva do Templo. Trava mais tarde.",
        },
        new[]
        {
            "I'm racing too, of course. I need to see how my work goes against my work.",
            "Je cours aussi, bien sûr. Il faut que je voie ce que donne mon travail contre mon travail.",
            "Ich fahre natürlich auch mit. Ich muss sehen, wie sich meine Arbeit gegen meine Arbeit schlägt.",
            "Yo también corro, claro. Tengo que ver qué tal va mi trabajo contra mi trabajo.",
            "Corro anch'io, naturalmente. Devo vedere come se la cava il mio lavoro contro il mio lavoro.",
            "Eu também corro, claro. Tenho de ver como o meu trabalho se porta contra o meu trabalho.",
        },
        new[]
        {
            "The Desert champion, beaten by a ferry-ticket driver! The Cup goes on, to the Emerald Moon.",
            "Le champion du Désert, battu par un pilote venu en ferry ! La Coupe continue, vers la Lune d'Émeraude.",
            "Der Wüstenchampion, geschlagen von einem Fahrer mit Fährticket! Der Pokal geht weiter, zum Smaragdmond.",
            "¡El campeón del Desierto, vencido por un piloto con billete de ferry! La Copa sigue, hacia la Luna Esmeralda.",
            "Il campione del Deserto, battuto da un pilota col biglietto del traghetto! La Coppa continua, verso la Luna di Smeraldo.",
            "O campeão do Deserto, vencido por um piloto de bilhete de barca! A Taça continua, rumo à Lua Esmeralda.",
        },
        new[]
        {
            "Next year, Twinsen.",
            "L'année prochaine, Twinsen.",
            "Nächstes Jahr, Twinsen.",
            "El año que viene, Twinsen.",
            "L'anno prossimo, Twinsen.",
            "Para o ano, Twinsen.",
        },
        new[]
        {
            "There was oil on the last bend this morning. That wasn't there yesterday. A black car came through at night, slow, like it was looking for something.",
            "Il y avait de l'huile dans le dernier virage ce matin. Elle n'y était pas hier. Une voiture noire est passée cette nuit, lentement, comme si elle cherchait quelque chose.",
            "Heute Morgen war Öl in der letzten Kurve. Gestern war es noch nicht da. Ein schwarzes Auto kam nachts vorbei, langsam, als würde es etwas suchen.",
            "Esta mañana había aceite en la última curva. Ayer no estaba. Un coche negro pasó de noche, despacio, como si buscara algo.",
            "Stamattina c'era dell'olio sull'ultima curva. Ieri non c'era. Una macchina nera è passata di notte, piano, come se cercasse qualcosa.",
            "Esta manhã havia óleo na última curva. Ontem não estava lá. Um carro preto passou de noite, devagar, como se andasse à procura de alguma coisa.",
        },
        new[]
        {
            "Looking for something on a race track... or putting something on one.",
            "Chercher quelque chose sur un circuit... ou y mettre quelque chose.",
            "Etwas auf einer Rennstrecke suchen... oder etwas auf eine legen.",
            "Buscar algo en un circuito... o poner algo en él.",
            "Cercare qualcosa su una pista... o metterci qualcosa.",
            "Procurar alguma coisa numa pista... ou pôr lá alguma coisa.",
        },
        new[]
        {
            "You think you're fast? On the moon I've got a lander that would leave your car standing. Come and race me up there. My rocket leaves now, room for three.",
            "Tu te crois rapide ? Sur la lune, j'ai un atterrisseur qui laisserait ta voiture sur place. Viens me défier là-haut. Ma fusée part maintenant, il y a de la place pour trois.",
            "Du hältst dich für schnell? Auf dem Mond habe ich eine Landefähre, die dein Auto stehen ließe. Komm und fahr dort oben gegen mich. Meine Rakete startet jetzt, Platz für drei.",
            "¿Te crees rápido? En la luna tengo un módulo de aterrizaje que dejaría tu coche parado. Ven a correr contra mí allí arriba. Mi cohete sale ahora, hay sitio para tres.",
            "Ti credi veloce? Sulla luna ho un modulo d'atterraggio che lascerebbe la tua macchina ferma. Vieni a sfidarmi lassù. Il mio razzo parte adesso, c'è posto per tre.",
            "Achas que és rápido? Na lua tenho um módulo de alunagem que deixava o teu carro parado. Vem correr contra mim lá em cima. O meu foguetão parte agora, há lugar para três.",
        },
        // (the easter eggs, from PageEgg on: RaceTrackStory.Eggs.Desert.cs)
        new[]
        {
            "A page: How to Pass Your Exams Without Studying. Step 1: let racing cars blow everyone else's papers away.",
            "Une page : Comment réussir ses examens sans réviser. Étape 1 : laisser les voitures de course emporter les copies des autres.",
            "Eine Seite: Wie man Prüfungen besteht, ohne zu lernen. Schritt 1: Rennwagen die Blätter aller anderen wegwehen lassen.",
            "Una página: Cómo aprobar los exámenes sin estudiar. Paso 1: dejar que los coches de carreras se lleven los papeles de los demás.",
            "Una pagina: Come superare gli esami senza studiare. Passo 1: lasciare che le auto da corsa portino via i fogli di tutti gli altri.",
            "Uma página: Como passar nos exames sem estudar. Passo 1: deixar os carros de corrida levar os papéis de todos os outros.",
        },
        new[]
        {
            "That's not one of mine. ...Whose is it? Flick!",
            "Ce n'est pas une des miennes. ...À qui est-elle ? Flick !",
            "Das ist keine von meinen. ...Wem gehört die? Flick!",
            "Esa no es mía. ...¿De quién es? ¡Flick!",
            "Questa non è mia. ...Di chi è? Flick!",
            "Essa não é minha. ...De quem é? Flick!",
        },
        new[]
        {
            "I have no idea what you mean.",
            "Je ne vois pas du tout de quoi tu parles.",
            "Ich habe keine Ahnung, wovon du sprichst.",
            "No tengo ni idea de qué me hablas.",
            "Non ho idea di cosa tu stia parlando.",
            "Não faço ideia do que estás a falar.",
        },
        new[]
        {
            "...Here. A clover box. For nothing at all. Nothing happened.",
            "...Tiens. Une boîte à trèfle. Pour rien du tout. Il ne s'est rien passé.",
            "...Hier. Eine Kleeblattbox. Einfach so. Es ist nichts passiert.",
            "...Toma. Una caja de trébol. Por nada en especial. Aquí no ha pasado nada.",
            "...Tieni. Una scatola per quadrifogli. Per niente. Non è successo niente.",
            "...Toma. Uma caixa de trevo. Por nada. Não aconteceu nada.",
        },
        new[]
        {
            "Twinsen can't read the runes. If only he still had the Book of Bù.",
            "Twinsen ne sait pas lire les runes. S'il avait encore le Livre de Bù...",
            "Twinsen kann die Runen nicht lesen. Hätte er nur noch das Buch von Bù.",
            "Twinsen no sabe leer las runas. Si aún tuviera el Libro de Bù...",
            "Twinsen non sa leggere le rune. Se solo avesse ancora il Libro di Bù...",
            "O Twinsen não consegue ler as runas. Se ao menos ainda tivesse o Livro de Bù...",
        },
        new[]
        {
            "Here raced the chariot of Bù. Lap record: 1 minute 2 seconds. Nobody will ever beat it.",
            "Ici courut le char de Bù. Record du tour : 1 minute 2 secondes. Nul ne le battra jamais.",
            "Hier fuhr der Streitwagen von Bù. Rundenrekord: 1 Minute 2 Sekunden. Niemand wird ihn je schlagen.",
            "Aquí corrió el carro de Bù. Récord de vuelta: 1 minuto 2 segundos. Nadie lo batirá jamás.",
            "Qui corse il carro di Bù. Record sul giro: 1 minuto e 2 secondi. Nessuno lo batterà mai.",
            "Aqui correu o carro de Bù. Recorde da volta: 1 minuto e 2 segundos. Ninguém o vai bater.",
        },
        new[]
        {
            "...except Twinsen.",
            "...sauf Twinsen.",
            "...außer Twinsen.",
            "...excepto Twinsen.",
            "...tranne Twinsen.",
            "...exceto o Twinsen.",
        },
        new[]
        {
            "A lap in under a minute and two seconds? Bù's own record! The School owes you 50 kashes.",
            "Un tour en moins d'une minute et deux secondes ? Le record de Bù lui-même ! L'École vous doit 50 kashes.",
            "Eine Runde in weniger als einer Minute und zwei Sekunden? Bùs eigener Rekord! Die Schule schuldet dir 50 Kashes.",
            "¿Una vuelta en menos de un minuto y dos segundos? ¡El récord del mismísimo Bù! La Escuela te debe 50 kashes.",
            "Un giro in meno di un minuto e due secondi? Il record di Bù in persona! La Scuola ti deve 50 kashes.",
            "Uma volta em menos de um minuto e dois segundos? O recorde do próprio Bù! A Escola deve-te 50 kashes.",
        },
        // (the sweep of 2026-11-05: the 1996 demo's School hall greets Twinsen with a text the game's Desert file hasn't (11); Baldino's
        // greeting, as the radio call was answered or not -- his tune, BaldinoTuned, after it)
        new[]
        {
            "Welcome to the School of Magic! Mind your step: we're still picking up after the Dean's exam papers.",
            "Bienvenue à l'École de Magie ! Attention où vous mettez les pieds : on ramasse encore les copies d'examen du Doyen.",
            "Willkommen in der Zauberschule! Pass auf, wo du hintrittst: Wir sammeln immer noch die Prüfungsblätter des Dekans auf.",
            "¡Bienvenido a la Escuela de Magia! Cuidado dónde pisas: todavía estamos recogiendo los exámenes del Decano.",
            "Benvenuto alla Scuola di Magia! Attento a dove metti i piedi: stiamo ancora raccogliendo i compiti d'esame del Rettore.",
            "Bem-vindo à Escola de Magia! Cuidado onde pões os pés: ainda estamos a apanhar os exames do Reitor.",
        },
        new[]
        {
            "Twinsen! My radio works then.",
            "Twinsen ! Ma radio marche, alors.",
            "Twinsen! Mein Funkgerät funktioniert also.",
            "¡Twinsen! Así que mi radio funciona.",
            "Twinsen! Allora la mia radio funziona.",
            "Twinsen! Então o meu rádio funciona.",
        },
        new[]
        {
            "Twinsen! You never answered my call. Never mind: you found the house.",
            "Twinsen ! Tu n'as jamais répondu à mon appel. Peu importe : tu as trouvé la maison.",
            "Twinsen! Du bist nie an mein Funkgerät gegangen. Egal: Du hast das Haus gefunden.",
            "¡Twinsen! Nunca contestaste a mi llamada. No importa: has encontrado la casa.",
            "Twinsen! Non hai mai risposto alla mia chiamata. Non importa: hai trovato la casa.",
            "Twinsen! Nunca atendeste a minha chamada. Não faz mal: encontraste a casa.",
        },
    };

    private static List<string> ApplyDesert(string gameDirectory, RaceTrackReport report, RaceTrackScenes.Result scenes)
    {
        var log = new List<string>();
        if (report.StartLine.Count == 0 || scenes.StartScene < 0) { log.Add("the Desert: no story, its track has no start line"); return log; }
        var first = AddIslandTexts(gameDirectory, RaceTrackIsland.Desert.IslandByte, DesertLines, log, "the Desert");
        if (first < 0) return log;
        int Say(DesertLine line) => first + (int)line;
        var b = new IslandBeatsA(gameDirectory, RaceTrackIsland.Desert, log);
        const string desert = "4 > chapter()";

        // the radio, on every outside scene: arriving from Citadel Island (the game still at chapter 2, the town circuit's) is chapter 3.
        // (And the game's variable 62 back to 0, its arrow to Ker'aooc's house cleared: on Citadel the game's people sent Twinsen to the
        // healing wizard for the Dino-Fly -- the pharmacist's customer, the umbrella's owner, Mr. Paul's missus -- setting it to 1, and at
        // 1 the people of Port-Ludo's plaza (scene 60) talk of nothing else. The sweep of 2026-11-05.)
        foreach (var scene in b.OutsideScenes.ToList())
            b.Edit(scene, scene == scenes.StartScene ? "the radio rings in chapter 3: Baldino's call" : "the radio rings in chapter 3: Baldino's call; after a win out of the car here, the after-race lines and the rocket",
                model => IslandBeatsA.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 4),
                RadioController(3, Say(DesertLine.Ringing), new[] { Say(DesertLine.BaldinoRadio) }, $"    if (2 == chapter())\n    {{\n        inc_chapter();\n        if (1 == var_game({CuredDinoFly}))\n        {{\n            set_var_game({CuredDinoFly}, 0);\n            clr_holo_pos({KeraoocArrow});\n        }}\n    }}\n",
                    // (out of the car after a win away from the start scene: the same lines, said without their speakers' voices)
                    scene == scenes.StartScene ? "" : $@"    if ({PagesWon} == var_game({QPages}) && {Driving} != comportement_hero())
    {{
        set_var_game({QPages}, {PagesDone});
        message({Say(DesertLine.FlickWon)});
        message({Say(DesertLine.RacerNext)});
        message({Say(DesertLine.MoyaOil)});
        message_zoe({Say(DesertLine.ZoeOil)});
        message({Say(DesertLine.BaldinoRocket)});
{BuAfterRace(Say)}        if (4 > chapter())
        {{
            inc_chapter();
        }}
        change_cube({MoonStart});
    }}
")));

        // Flick at her plinth: the track shut (Q_PAGES 1), still shut, open
        MarshalA(b, report, desert, FlickEggs(Say) + $@"            if (0 == var_game({QPages}))
            {{
                message({Say(DesertLine.FlickShut)});
                set_var_game({QPages}, 1);
            }}
            else if (6 > var_game({QPages}))
            {{
                message({Say(DesertLine.FlickPages)});
            }}
            else if ({PagesWon} > var_game({QPages}))
            {{
                message({Say(DesertLine.FlickOpen)});
            }}", "Flick");

        // the Dean in the School of Magic: a copy of the game's own talks (the game's is out of sight on the Desert's chapters). The hall is
        // the 1996 demo's when the build put it in (Demo96Rooms): its Dean, another actor there, is the one copied -- the actor at the
        // Dean's place, as the scene now has it.
        var deanActor = b.Store.SceneExists(DeanScene) && b.Store.Load(DeanScene).Actors.FindIndex(a => a.X == DeanAt.X && a.Z == DeanAt.Z) is >= 0 and var found ? found : DeanActor;
        if (Copy(b.Store, DeanScene, deanActor) is { } dean)
        {
            b.Hide(DeanScene, deanActor, desert, "Dean");
            b.Edit(DeanScene, "the Dean: the three pages (Q_PAGES 2), the Wizard's Diploma (item 30, Q_PAGES 6)", model => IslandBeatsA.Add(model, dean, Talker(desert, DeanEggs(Say) + $@"            if (5 == var_game({QPages}))
            {{
                message({Say(DesertLine.DeanAll)});
                found_object({Diploma});
                set_var_game({Diploma}, 1);
                set_var_game({QPages}, 6);
            }}
            else if (2 > var_game({QPages}))
            {{
                message({Say(DesertLine.DeanShut)});
                message_zoe({Say(DesertLine.ZoePages)});
                set_var_game({QPages}, 2);
            }}
            else if (5 > var_game({QPages}))
            {{
                message({Say(DesertLine.DeanShut)});
                message_zoe({Say(DesertLine.ZoeWhere)});
            }}
            else
            {{
                message({Say(DesertLine.DeanAll)});
            }}")));
        }
        else log.Add($"the Dean left out: no actor {deanActor} in scene {DeanScene}");

        // The School hall's welcome the first time Twinsen comes in (the walkthrough's). The 1996 demo's greeter (actor 10 of the hall) said it
        // only once the switch at the far wall (actor 7: the chest Twinsen opens with Action in zone 5, facing it) had been worked, and in
        // the same breath opened the gate (actor 8) up to the Dean's floor and set the game's variable 61 to 1 (which unlocks the entrance
        // hall's upper door too) -- the welcome waited for the chest, not for Twinsen coming in (the sweep of 2026-11-05, ch03
        // 08_school_hall). Now it is said 0.7 s after coming in while 61 is 0, and that first visit is done there (61 = 1: the gate and the
        // greeter gone on the next one, the upper door open, as the demo has them after the chest), with the game's variable 62 at 3 (the
        // Dino-Fly's errand: the School found, which the greeting set too); the chest still opens the gate this time.
        b.Edit(DeanScene, "the School's hall: the welcome as Twinsen first comes in, the hall's switch still opening the gate to the Dean", _ => scripts =>
        {
            var greeter = Enumerable.Range(0, scripts.ActorCount).Where(a =>
            {
                try { var t = scripts.GetText(a, ScriptKind.Life); return t.Contains("l_track_obj(7)") && t.Contains("message(11);"); }
                catch (InvalidOperationException) { return false; }
            }).ToList();
            if (greeter.Count != 1) throw new NotTheGames($"the demo's greeter: {greeter.Count} actors say the welcome when the switch is worked");
            var life = scripts.GetText(greeter[0], ScriptKind.Life);
            life = Loose(life, @"if (100 == l_track_obj(7))
        {
            set_track(label_0);
            set_comportement(comportement_1);
        }", @"set_track(label_0);
        set_comportement(comportement_1);");
            life = Loose(life, @"if (1 == l_track())
    {
        if (3 > var_game(62))
        {
            set_var_game(62, 3);
        }
        set_var_game(61, 1);
        set_track_obj(8, label_0);
        message(11);
        suicide();
    }", $@"if (1 == l_track())
    {{
        if (3 > var_game(62))
        {{
            set_var_game(62, 3);
        }}
        set_var_game(61, 1);
        message({Say(DesertLine.HallWelcome)});
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if (100 == l_track_obj(7))
    {{
        set_track_obj(8, label_0);
        suicide();
    }}");
            scripts.SetText(greeter[0], ScriptKind.Life, life);
        });

        // The game's people of the School and the town, put right (the sweep of 2026-11-05): in the School's hall, the 1996 demo's greeting
        // on Twinsen's first visit (its text 11, which the game's Desert file hasn't: the story's welcome) and Twinsen's third "hello" (102,
        // none either: the game's 100), and the student's "he's in a terrible mood about his exam papers" (69) no longer once the diploma is
        // given (the game's variable 61 stays at 1 after the first visit); in Ker'aooc's house, the umbrella shown to his assistant is not
        // hers (IslandRetell's 286) and stays with Twinsen; in the Pearl of Incandescence's cave, Twinsen's second look round is the game's
        // 337 (it said 327, which is no text: the game's own slip).
        b.Edit(DeanScene, "the School's hall: the demo's greeting and Twinsen's hello in the game's texts, the student quiet about the exam papers once the diploma is given", _ => scripts =>
        {
            for (var a = 0; a < scripts.ActorCount; a++)
            {
                string life;
                try { life = scripts.GetText(a, ScriptKind.Life); } catch (InvalidOperationException) { continue; }
                var now = life.Replace("message(11);", $"message({Say(DesertLine.HallWelcome)});").Replace("message(102);", "message(100);");
                now = Regex.Replace(now, @"if \(2 > var_game\(61\)\)(\s*\{\s*message\(69\);)", $"if (2 > var_game(61) && {PagesDiploma} > var_game({QPages}))$1");
                if (now != life) scripts.SetText(a, ScriptKind.Life, now);
            }
        });
        b.Edit(KeraoocHouse, "Ker'aooc's assistant: the umbrella isn't hers, Twinsen keeps it", _ => scripts =>
            scripts.SetText(KeraoocAssistant, ScriptKind.Life, Loose(scripts.GetText(KeraoocAssistant, ScriptKind.Life),
                "oneif (1 == use_inventory(20))\n{\nset_var_game(20, 0);\nmessage(286);", "oneif (1 == use_inventory(20))\n{\nmessage(286);")));
        b.Edit(PearlCave, "the Pearl of Incandescence's cave: Twinsen's look round is a text of the game's (337, not 327)", _ => scripts =>
            scripts.SetText(0, ScriptKind.Life, Loose(scripts.GetText(0, ScriptKind.Life), "message(327);", "message(337);")));

        // the three pages on the road, at 30, 55 and 80 % of the lap: driven over or walked to, in lap order
        var pages = new[] { (0.30, DesertLine.Page1), (0.55, DesertLine.Page2), (0.80, DesertLine.Page3) };
        for (var k = 0; k < pages.Length; k++)
        {
            var (fraction, line) = pages[k];
            var step = 2 + k;
            if (OnLap(report, fraction) is not { } at) { log.Add($"page {k + 1} left out: no ground on the lap there"); continue; }
            var scene = b.SceneAt(at.X, at.Z);
            if (scene < 0) { log.Add($"page {k + 1} left out: no scene at cell ({at.X:0.#}, {at.Z:0.#})"); continue; }
            b.Edit(scene, $"exam page {k + 1} on the road at cell ({at.X:0.#}, {at.Z:0.#}) ({fraction:0%} of the lap): Q_PAGES {step} -> {step + 1}", model =>
                IslandBeatsA.Add(model, Voice(model, (at.X, at.Z, at.Y, 0), 9), $@"void comportement_0()
{{
    if ({desert})
    {{
        set_comportement(comportement_1);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
    if ({step} == var_game({QPages}) && 1400 > distance(0))
    {{
        set_var_game({QPages}, {step + 1});
        message({Say(line)});
    }}
}}
"));
        }

        // Baldino at his house: a copy of the game's own, the car's tune and his tip -- his greeting as his radio call was answered or not
        // (the sweep of 2026-11-05: "My radio works then" whether it was or not)
        if (Copy(b.Originals, BaldinoHouse, BaldinoActor) is { } baldino)
        {
            b.Hide(BaldinoHouse, BaldinoActor, desert, "Baldino");
            b.Edit(BaldinoHouse, "Baldino at his workbench: the sand filters, his tip, and he races too", model => IslandBeatsA.Add(model, baldino, Talker(desert, $@"            if (3 == var_game({RadioAnsweredA}))
            {{
                message({Say(DesertLine.BaldinoRadioWorks)});
            }}
            else
            {{
                message({Say(DesertLine.BaldinoNoCall)});
            }}
            message({Say(DesertLine.BaldinoTuned)});
            message({Say(DesertLine.BaldinoRacing)});")));
        }
        else log.Add($"Baldino left out: no actor {BaldinoActor} in scene {BaldinoHouse}");

        // won: out of the car by the start line -- Flick, the racer, Moya's clue, Zoe, and Baldino's rocket to the moon
        var racerColour = Copy(b.Originals, RaceTrackScenes.RacerScene, 4)?.CoulObj ?? 2;
        var baldinoColour = Copy(b.Originals, BaldinoHouse, BaldinoActor)?.CoulObj ?? 4;
        b.Edit(scenes.StartScene, "after a win: Flick, the racer, Moya's clue (the oil, the black car), Zoe, and Baldino's rocket to the Emerald Moon (inc_chapter, scene 229)", model =>
        {
            var spot = AfterRaceSpot(report);
            if ((int)Math.Floor(spot.X / 64) != model.CubeX || (int)Math.Floor(spot.Z / 64) != model.CubeY) spot = (model.CubeX * 64 + 32, model.CubeY * 64 + 32, report.StartLine[0].Y, 0);
            var racer = model.Actors.Count; var moya = racer + 1; var bal = racer + 2;
            var edits = new List<Action<SceneScripts>>
            {
                IslandBeatsA.Add(model, Voice(model, spot, racerColour), "void comportement_0()\n{\n}\n"),
                IslandBeatsA.Add(model, Voice(model, spot, 2), "void comportement_0()\n{\n}\n"),
                IslandBeatsA.Add(model, Voice(model, spot, baldinoColour), "void comportement_0()\n{\n}\n"),
                IslandBeatsA.Add(model, Voice(model, spot, 9), $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if ({PagesWon} == var_game({QPages}) && {Driving} != comportement_hero())
    {{
        set_var_game({QPages}, {PagesDone});
        message({Say(DesertLine.FlickWon)});
        message_obj({racer}, {Say(DesertLine.RacerNext)});
        message_obj({moya}, {Say(DesertLine.MoyaOil)});
        message_zoe({Say(DesertLine.ZoeOil)});
        message_obj({bal}, {Say(DesertLine.BaldinoRocket)});
{BuAfterRace(Say)}        if (4 > chapter())
        {{
            inc_chapter();
        }}
        change_cube({MoonStart});
    }}
}}
"),
            };
            return scripts => { foreach (var e in edits) e(scripts); };
        });

        // (the easter eggs: the fourth page, Bù's stone -- RaceTrackStory.Eggs.Desert.cs)
        DesertEggs(b, report, Say, desert);
        b.Save();
        log.Add($"the Desert's race file: gate={QPages} 6 (the diploma), win={QPages} {PagesWon}");
        return log;
    }
}
