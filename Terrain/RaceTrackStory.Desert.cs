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
//     the black car, Zoe, and Baldino taking him to the Esmers' hangar (inc_chapter: 4; change_cube(12)) -- the freighter to the moon there
//     (RaceTrackStory.Hangar.cs; scene 229, the Emerald Moon's start).
internal static partial class RaceTrackStory
{
    // Q_PAGES (docs/LBRA_STORY_PLAN.md 3.1): 1 Flick has told, 2 the Dean has, 3-5 the pages found, 6 the diploma, 7 the race won, 8 told
    public const int QPages = 215, PagesDiploma = 6, PagesWon = 7, PagesDone = 8;
    // the group's two spare game variables: the chapter whose radio call was answered, and the chapter whose ringing was shown
    // (not 235-237: the game keeps which videos have been seen in their bits, PLAYACF.CPP FLAG_ACF -- 2026-11-04)
    public const int RadioAnsweredA = 234, RadioRangA = 240;
    // (rung and not answered: it answers itself after some 20 seconds -- frames counted in this scene, RadioAutoRounds rounds of 250 -- so it
    // never holds him up: docs/LBRA_STORY_SCRIPT.md, "If he hasn't answered after 20 seconds it answers itself")
    private const int RadioWait = 72, RadioRounds = 73, RadioAutoRounds = 3;
    private const int Diploma = 30, DeanScene = 27, DeanActor = 8, BaldinoHouse = 38, BaldinoActor = 2, MoonStart = 229;
    // the game's own: the variable of its Dino-Fly's cure (1 sent to Ker'aooc, 2 his assistant has answered, 3 the School's hall seen)
    // and the holomap's arrow to Ker'aooc's house; Ker'aooc's house, its assistant; the Pearl of Incandescence's cave
    private const int CuredDinoFly = 62, KeraoocArrow = 35, KeraoocHouse = 35, KeraoocAssistant = 2, PearlCave = 113, PearlClam = 5;
    // (where the Dean stands in the School's hall, the game's and the 1996 demo's alike)
    private static readonly (int X, int Z) DeanAt = (15104, 5040);
    // The exam pages as Twinsen carries them (the user, 2026-11-06: "Add the pages to the inventory as one item with a count, 1/2/3 for how
    // many he has found ... gone when handed to the Dean"; and "once the Dean has told Twinsen where his missing papers are, mark each
    // location on the holomap ... cleared as it's found"). The inventory's 40 slots are all the game's (a save keeps exactly 40), so the
    // pages take one the race build has no use for, as the gloves took 7: 6, the pyramid key, which only the neighbour on Citadel Island
    // gave -- the story took that out (Chapters.cs, the neighbour's design sheet). Its game variable is how many pages he has (1-3; 0 once
    // the Dean has them), its box counts them (the race file's inv_count=, RACEMOD.CPP), its model the burgomaster's note's (OBJFIX.HQR 38
    // copied into 6: a scroll of paper), its found message, name and description (file 2's 6, 106 and 206) the pages' -- each moved out of
    // its voiced place, as the gloves' were. The arrows: three holomap positions no scene of the build has (from 224 on), labelled "An exam
    // page", on from the Dean's word (Q_PAGES 2), each off as its page is found.
    public const int PagesSlot = 6;
    // which pages are found, one bit each in one game variable (the user, 2026-11-08: "Can we use a single byte flag for the pages, we
    // could use a single bit for each page"): page 1 adds 1, page 2 adds 2, page 3 adds 4. The scripts have no bit tests, only
    // comparisons, so "page k not found yet" is the values 0-7 without its bit, one equality each. A spare game variable (75:
    // RaceTrackStory.Eggs.cs's list)
    private const int PagesFoundVar = 75;
    // (the pages lie on the road from the Desert chapter's start, picked up in any order before the Dean has told of them too: the user,
    // 2026-11-08, "If they're missing they're missing, we should be able to collect them without speaking to the dean first". Q_PAGES moves
    // with them only once he has: telling of them, he counts those already found -- Q_PAGES 2 and one more each -- and all three found, the
    // diploma there and then)
    private static string PagesBeforeDean() =>
        $@"if (1 == var_game({PagesFoundVar}) || 2 == var_game({PagesFoundVar}) || 4 == var_game({PagesFoundVar}))
                {{
                    set_var_game({QPages}, 3);
                }}
                else if (3 == var_game({PagesFoundVar}) || 5 == var_game({PagesFoundVar}) || 6 == var_game({PagesFoundVar}))
                {{
                    set_var_game({QPages}, 4);
                }}";
    private static string PageNotFound(int k) =>
        string.Join(" || ", Enumerable.Range(0, 8).Where(v => (v & (1 << k)) == 0).Select(v => $"{v} == var_game({PagesFoundVar})"));
    private const int PagesModelLike = 38, FirstFreeArrow = 224, LastArrow = 254;
    private static readonly string[][] PagesItemTexts =
    {
        new[] { "You found one of the Dean's exam pages.", "Exam pages", "The Dean's exam pages, blown along the road by the racers' engines. He wants all three back." },
        new[] { "Tu as trouvé une des pages d'examen du Doyen.", "Pages d'examen", "Les pages d'examen du Doyen, emportées le long de la route par les moteurs des pilotes. Il les veut toutes les trois." },
        new[] { "Du hast eine der Prüfungsseiten des Dekans gefunden.", "Prüfungsseiten", "Die Prüfungsseiten des Dekans, von den Motoren der Rennfahrer die Straße entlang geweht. Er will alle drei zurück." },
        new[] { "Has encontrado una de las páginas de examen del Decano.", "Páginas de examen", "Las páginas de examen del Decano, llevadas por la carretera por los motores de los pilotos. Las quiere de vuelta las tres." },
        new[] { "Hai trovato una delle pagine d'esame del Rettore.", "Pagine d'esame", "Le pagine d'esame del Rettore, portate via lungo la strada dai motori dei piloti. Le rivuole tutte e tre." },
        new[] { "Encontraste uma das páginas de exame do Reitor.", "Páginas de exame", "As páginas de exame do Reitor, levadas pela estrada pelos motores dos pilotos. Ele quer as três de volta." },
    };
    private static readonly string[] PageArrowLabel =
    {
        "An exam page", "Une page d'examen", "Eine Prüfungsseite", "Una página de examen", "Una pagina d'esame", "Uma página de exame",
    };
    // the School's door from the canyon outside (scene 61, and the game's other scene of that cube, 201): its zone, as the game has it, and
    // how far out it reaches now and where in the School's entrance (scene 28) it lands Twinsen (its Info2: his x in the zone is added)
    private const int SchoolOutside = 61, SchoolOutsideToo = 201, SchoolEntrance = 28, SchoolDoorReach = 10240, SchoolDoorLanding = 10752 - 768;
    private static bool IsSchoolDoor(SceneZoneModel z) => z.Type == 0 && z.Num == SchoolEntrance && z.X0 == 9216 && z.X1 == 9728 && z.Info[2] == 10752;
    // (and the entrance's door back out to it, which lands Twinsen at x 9728 plus his z in it: now SchoolOutLanding plus)
    private const int SchoolOutLanding = SchoolDoorReach + 256;
    private static bool IsSchoolWayOut(SceneZoneModel z) => z.Type == 0 && z.Num == SchoolOutside && z.X0 == 26624 && z.Info[0] == 9728;

    // The race file's story keys of the group's islands (RaceCarEngineFile.EngineFile, in the story): their gates and what a win sets.
    internal static string? RaceKeysA(RaceTrackIsland island) =>
        island == RaceTrackIsland.Desert ? $"gate={QPages} 6 Flick: the Dean has shut the track!\ngate={Citadel.CitadelOveralls.GlovesSlot} {Citadel.CitadelOveralls.GearBoth} Flick: no helmet, no race! The giant clam's sea cave keeps one.\nwin={QPages} {PagesWon}\ninv_count={PagesSlot}\n" + GarageRaceLines()
        : island == RaceTrackIsland.Emerald ? $"gate={QHal} {HalMended} Lyra: HAL won't switch on the start lights!\nwin={QHal} {HalWon}\nno_suit=1\nfoot_body={Citadel.CitadelOveralls.HelmetBody}\n"
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
        if ({chapter} == var_game({RadioRangA}) && {Driving} != comportement_hero())
        {{
            add_var_cube({RadioWait}, 1);
            if (250 < var_cube({RadioWait}))
            {{
                set_var_cube({RadioWait}, 0);
                add_var_cube({RadioRounds}, 1);
            }}
        }}
        if (1 == use_inventory({Radio}) || {RadioAutoRounds} < var_cube({RadioRounds}))
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
        // (the racing helmet, 2026-11-08: Citadel/CitadelOveralls)
        FlickHelmet,
        // (the racers' garage, 2026-11-08: RaceTrackStory.Garage.cs)
        BaldinoFilters, ZoeFitted, ZoeNoFilters, ZoeReady,
        // (Zoe as companion's hints -- RaceTrackStory.Companion.cs, 2026-11-11)
        ZoeHintDean, ZoeHintPages, ZoeHintFilters, ZoeHintHelmet, ZoeHintRace,
        // (the Esmers' hangar, the way to the moon -- RaceTrackStory.Hangar.cs, 2026-11-14)
        HangarOffer, HangarYes, HangarNo, HangarBoard, HangarLater, HangarLift, HangarClosed,
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
            "Hello? Twinsen? That's it, I'm at the racers' garage, the big striped tent past the start line. Come and see me before you race: your car's going to want sand filters.",
            "Allô ? Twinsen ? Ça y est, je suis au garage des pilotes, la grande tente rayée après la ligne de départ. Passe me voir avant la course : ta voiture va avoir besoin de filtres à sable.",
            "Hallo? Twinsen? So, ich bin in der Werkstatt der Fahrer, dem großen gestreiften Zelt hinter der Startlinie. Komm vor dem Rennen bei mir vorbei: Dein Auto wird Sandfilter brauchen.",
            "¿Hola? ¿Twinsen? Ya está, estoy en el taller de los pilotos, la gran carpa a rayas pasada la línea de salida. Ven a verme antes de la carrera: tu coche va a necesitar filtros de arena.",
            "Pronto? Twinsen? Ecco, sono all'officina dei piloti, la grande tenda a strisce dopo la linea di partenza. Passa a trovarmi prima della gara: la tua macchina avrà bisogno di filtri per la sabbia.",
            "Estou? Twinsen? Pronto, estou na oficina dos pilotos, a grande tenda às riscas depois da linha de partida. Passa por cá antes da corrida: o teu carro vai precisar de filtros de areia.",
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
            "Zoe's fitted the filters? Good. And I've given you a longer top gear for the long straights. And a word of advice: the champion brakes late at the Temple bend. Brake later.",
            "Zoé a posé les filtres ? Parfait. Et je t'ai mis une dernière vitesse plus longue pour les grandes lignes droites. Et un conseil : le champion freine tard au virage du Temple. Freine plus tard.",
            "Zoe hat die Filter eingebaut? Gut. Und ich habe dir einen längeren höchsten Gang für die langen Geraden gegeben. Und ein Rat: Der Champion bremst spät in der Tempelkurve. Brems später.",
            "¿Zoe ha puesto los filtros? Bien. Y te he puesto una marcha larga más larga para las rectas. Y un consejo: el campeón frena tarde en la curva del Templo. Frena más tarde.",
            "Zoe ha montato i filtri? Bene. E ti ho dato una marcia alta più lunga per i rettilinei. E un consiglio: il campione frena tardi alla curva del Tempio. Frena più tardi.",
            "A Zoe montou os filtros? Ótimo. E pus-te uma mudança de cima mais longa para as retas. E um conselho: o campeão trava tarde na curva do Templo. Trava mais tarde.",
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
            "You think you're fast? On the moon I've got a lander that would leave your car standing. Come and race me up there. The Esmers' freighter takes cars to the moon from their hangar -- I'll take you there now.",
            "Tu te crois rapide ? Sur la lune, j'ai un atterrisseur qui laisserait ta voiture sur place. Viens me défier là-haut. Le cargo des Esmers emmène les voitures sur la lune depuis leur hangar -- je t'y conduis.",
            "Du hältst dich für schnell? Auf dem Mond habe ich eine Landefähre, die dein Auto stehen ließe. Komm und fahr dort oben gegen mich. Der Frachter der Esmer bringt Autos aus ihrem Hangar zum Mond -- ich bringe dich hin.",
            "¿Te crees rápido? En la luna tengo un módulo de aterrizaje que dejaría tu coche parado. Ven a correr contra mí allí arriba. El carguero de los Esmers lleva coches a la luna desde su hangar: te llevo ahora.",
            "Ti credi veloce? Sulla luna ho un modulo d'atterraggio che lascerebbe la tua macchina ferma. Vieni a sfidarmi lassù. Il cargo degli Esmer porta le auto sulla luna dal loro hangar: ti ci porto subito.",
            "Achas que és rápido? Na lua tenho um módulo de alunagem que deixava o teu carro parado. Vem correr contra mim lá em cima. O cargueiro dos Esmers leva carros para a lua a partir do hangar deles -- levo-te lá agora.",
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
        new[]
        {
            "And no helmet, no race! There's a giant clam in a sea cave in the cliffs that's been sitting on a racing helmet since some racer went diving. I've marked it on your holomap.",
            "Et pas de casque, pas de course ! Il y a un coquillage géant dans une grotte marine au pied des falaises qui couve un casque de course depuis qu'un pilote est allé plonger. Je te l'ai marqué sur ton holomap.",
            "Und ohne Helm kein Rennen! In einer Meeresgrotte in den Klippen sitzt eine Riesenmuschel auf einem Rennhelm, seit irgendein Rennfahrer dort tauchen war. Ich habe sie dir auf dem Holomap markiert.",
            "¡Y sin casco no hay carrera! Hay un molusco gigante en una gruta marina de los acantilados que guarda un casco de carreras desde que un piloto fue a bucear. Te lo he marcado en el holomapa.",
            "E niente casco, niente corsa! C'è una conchiglia gigante in una grotta marina tra le scogliere che cova un casco da corsa da quando un pilota ci è andato a fare immersioni. Te l'ho segnata sull'olomappa.",
            "E sem capacete não há corrida! Há uma concha gigante numa gruta marinha nas falésias que guarda um capacete de corrida desde que um piloto foi lá mergulhar. Marquei-a no teu holomapa.",
        },
        new[]
        {
            "Twinsen! My radio works, then. Here: sand filters for your car. Give them to Zoe, she'll fit them. Without them the sand chokes your engine before you're halfway round.",
            "Twinsen ! Ma radio marche, alors. Tiens : des filtres à sable pour ta voiture. Donne-les à Zoé, elle les posera. Sans eux, le sable étouffe ton moteur avant la moitié du tour.",
            "Twinsen! Mein Funkgerät funktioniert also. Hier: Sandfilter für dein Auto. Gib sie Zoe, sie baut sie ein. Ohne sie erstickt der Sand deinen Motor, bevor du die halbe Runde geschafft hast.",
            "¡Twinsen! Así que mi radio funciona. Toma: filtros de arena para tu coche. Dáselos a Zoe, ella los pondrá. Sin ellos, la arena ahoga el motor antes de media vuelta.",
            "Twinsen! Allora la mia radio funziona. Tieni: filtri per la sabbia per la tua macchina. Dalli a Zoe, li monterà lei. Senza, la sabbia soffoca il motore prima di metà giro.",
            "Twinsen! Então o meu rádio funciona. Toma: filtros de areia para o teu carro. Dá-os à Zoe, ela monta-os. Sem eles, a areia sufoca o motor antes de meia volta.",
        },
        new[]
        {
            "Baldino's sand filters? Give them here... There. Fitted. No more sand in your engine, love.",
            "Les filtres à sable de Baldino ? Donne-les-moi... Voilà. Posés. Plus de sable dans ton moteur, mon chéri.",
            "Baldinos Sandfilter? Gib her... So. Eingebaut. Kein Sand mehr in deinem Motor, Liebling.",
            "¿Los filtros de arena de Baldino? Dámelos... Ya está. Puestos. Se acabó la arena en tu motor, cariño.",
            "I filtri per la sabbia di Baldino? Dammeli... Ecco. Montati. Niente più sabbia nel tuo motore, tesoro.",
            "Os filtros de areia do Baldino? Dá cá... Pronto. Montados. Acabou-se a areia no teu motor, querido.",
        },
        new[]
        {
            "This sand gets everywhere. Your engine won't last a lap without sand filters: Baldino has some, he's here in the tent.",
            "Ce sable se met partout. Ton moteur ne tiendra pas un tour sans filtres à sable : Baldino en a, il est ici, sous la tente.",
            "Dieser Sand kommt überall hin. Ohne Sandfilter hält dein Motor keine Runde durch: Baldino hat welche, er ist hier im Zelt.",
            "Esta arena se mete por todas partes. Tu motor no aguantará una vuelta sin filtros de arena: Baldino tiene, está aquí, en la carpa.",
            "Questa sabbia va dappertutto. Il tuo motore non reggerà un giro senza filtri per la sabbia: Baldino ne ha, è qui nella tenda.",
            "Esta areia mete-se em todo o lado. O teu motor não aguenta uma volta sem filtros de areia: o Baldino tem alguns, está aqui na tenda.",
        },
        new[]
        {
            "Filters in, tyres checked. Your car's ready, Twinsen. Go and win!",
            "Filtres posés, pneus vérifiés. Ta voiture est prête, Twinsen. Va gagner !",
            "Filter drin, Reifen geprüft. Dein Auto ist bereit, Twinsen. Geh und gewinn!",
            "Filtros puestos, neumáticos revisados. Tu coche está listo, Twinsen. ¡Ve y gana!",
            "Filtri montati, gomme controllate. La tua macchina è pronta, Twinsen. Vai e vinci!",
            "Filtros montados, pneus verificados. O teu carro está pronto, Twinsen. Vai e ganha!",
        },
        new[]
        {
            "Flick says the Dean has shut the track. Let's go and see him at the School of Magic.",
            "Flick dit que le Doyen a fermé la piste. Allons le voir à l'École de Magie.",
            "Flick sagt, der Dekan hat die Strecke gesperrt. Besuchen wir ihn in der Zauberschule.",
            "Flick dice que el Decano ha cerrado la pista. Vamos a verlo a la Escuela de Magia.",
            "Flick dice che il Rettore ha chiuso la pista. Andiamo a trovarlo alla Scuola di Magia.",
            "O Flick diz que o Reitor fechou a pista. Vamos vê-lo à Escola de Magia.",
        },
        new[]
        {
            "The Dean's exam pages blew all over the track. Find all three and take them back to him.",
            "Les pages d'examen du Doyen se sont envolées sur la piste. Trouve-les toutes les trois et rapporte-les-lui.",
            "Die Prüfungsblätter des Dekans sind über die ganze Strecke geweht. Finde alle drei und bring sie ihm zurück.",
            "Las hojas de examen del Decano volaron por toda la pista. Encuentra las tres y llévaselas.",
            "Le pagine d'esame del Rettore sono volate su tutta la pista. Trovale tutte e tre e riportagliele.",
            "As folhas de exame do Reitor voaram por toda a pista. Encontra as três e leva-lhas.",
        },
        new[]
        {
            "The sand will choke the engine. Baldino has sand filters in the racers' garage tent.",
            "Le sable va étouffer le moteur. Baldino a des filtres à sable sous la tente des pilotes.",
            "Der Sand erstickt den Motor. Baldino hat Sandfilter im Werkstattzelt der Fahrer.",
            "La arena ahogará el motor. Baldino tiene filtros de arena en la carpa de los pilotos.",
            "La sabbia soffocherà il motore. Baldino ha dei filtri per la sabbia nella tenda dei piloti.",
            "A areia vai sufocar o motor. O Baldino tem filtros de areia na tenda dos pilotos.",
        },
        new[]
        {
            "No helmet, no race. The giant clam's sea cave in the cliffs is the place to look.",
            "Pas de casque, pas de course. La grotte marine du coquillage géant, dans les falaises, voilà où chercher.",
            "Kein Helm, kein Rennen. Die Meeresgrotte der Riesenmuschel in den Klippen, da müssen wir suchen.",
            "Sin casco no hay carrera. La gruta marina del molusco gigante, en los acantilados, es donde hay que buscar.",
            "Niente casco, niente corsa. La grotta marina della conchiglia gigante, nelle scogliere, è il posto dove cercare.",
            "Sem capacete, sem corrida. A gruta marinha da concha gigante, nas falésias, é onde procurar.",
        },
        new[]
        {
            "Everything's ready, Twinsen. Let's go and win the desert race!",
            "Tout est prêt, Twinsen. Allons gagner la course du désert !",
            "Alles ist bereit, Twinsen. Gewinnen wir das Wüstenrennen!",
            "Todo está listo, Twinsen. ¡Vamos a ganar la carrera del desierto!",
            "È tutto pronto, Twinsen. Andiamo a vincere la corsa del deserto!",
            "Está tudo pronto, Twinsen. Vamos ganhar a corrida do deserto!",
        },
        new[]
        {
            "The freighter for the Emerald Moon is ready. Shall we load your car?",
            "Le cargo pour la Lune d'Émeraude est prêt. On charge ta voiture ?",
            "Der Frachter zum Smaragdmond ist bereit. Sollen wir dein Auto einladen?",
            "El carguero a la Luna Esmeralda está listo. ¿Cargamos tu coche?",
            "Il cargo per la Luna di Smeraldo è pronto. Carichiamo la tua auto?",
            "O cargueiro para a Lua Esmeralda está pronto. Carregamos o teu carro?",
        },
        new[]
        {
            "Load it. To the moon!",
            "Chargez-la. En route pour la lune !",
            "Ladet es ein. Auf zum Mond!",
            "Cargadlo. ¡A la luna!",
            "Caricatela. Si va sulla luna!",
            "Carreguem-no. Para a lua!",
        },
        new[]
        {
            "Not yet.",
            "Pas encore.",
            "Noch nicht.",
            "Todavía no.",
            "Non ancora.",
            "Ainda não.",
        },
        new[]
        {
            "Your car's going aboard. Up the pad's ramp and through the hatch -- we lift off as soon as you're in.",
            "Ta voiture monte à bord. Grimpe la rampe de la plate-forme et passe l'écoutille -- on décolle dès que tu es dedans.",
            "Dein Auto kommt an Bord. Die Rampe zur Plattform hinauf und durch die Luke -- wir starten, sobald du drin bist.",
            "Tu coche sube a bordo. Sube la rampa de la plataforma y entra por la escotilla: despegamos en cuanto estés dentro.",
            "La tua auto sale a bordo. Su per la rampa della piattaforma e dentro il portello: decolliamo appena sei dentro.",
            "O teu carro vai a bordo. Sobe a rampa da plataforma e entra pela escotilha -- descolamos assim que estiveres lá dentro.",
        },
        new[]
        {
            "Take your time. The freighter doesn't fly without its passenger.",
            "Prends ton temps. Le cargo ne part pas sans son passager.",
            "Lass dir Zeit. Der Frachter fliegt nicht ohne seinen Passagier.",
            "Tómate tu tiempo. El carguero no despega sin su pasajero.",
            "Fai con calma. Il cargo non parte senza il suo passeggero.",
            "Não tenhas pressa. O cargueiro não parte sem o seu passageiro.",
        },
        new[]
        {
            "The hatch closes. The freighter shudders, rises from its pad and climbs away towards the Emerald Moon.",
            "L'écoutille se ferme. Le cargo tremble, quitte sa plate-forme et s'élève vers la Lune d'Émeraude.",
            "Die Luke schließt sich. Der Frachter bebt, hebt von seiner Plattform ab und steigt dem Smaragdmond entgegen.",
            "La escotilla se cierra. El carguero tiembla, se eleva de su plataforma y asciende hacia la Luna Esmeralda.",
            "Il portello si chiude. Il cargo trema, si stacca dalla piattaforma e sale verso la Luna di Smeraldo.",
            "A escotilha fecha-se. O cargueiro estremece, ergue-se da plataforma e sobe rumo à Lua Esmeralda.",
        },
        new[]
        {
            "No more flights to the moon today, friend.",
            "Plus de vols pour la lune aujourd'hui, l'ami.",
            "Heute keine Flüge mehr zum Mond, Freund.",
            "Hoy no hay más vuelos a la luna, amigo.",
            "Niente più voli per la luna oggi, amico.",
            "Hoje não há mais voos para a lua, amigo.",
        },
    };

    // The exam pages' item and their arrows (PagesSlot): the slot's texts and model, and an arrow at each page's place (island cells; null:
    // none). Returns each page's arrow, -1 where it has none. A second pass over the same files finds the texts there already and only
    // places the arrows again.
    private static int[] PagesInstall(string gameDirectory, SceneStore store, (double X, double Z, double Y)?[] places, List<string> log)
    {
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        var labelWords = System.Text.Encoding.Latin1.GetString(Dos.GetBytes(PageArrowLabel[0]));
        var label = Lba2TextBank.Load(text, 0, HolomapTexts).Texts.FirstOrDefault(t => t.Value == labelWords)?.Id ?? -1;
        if (label < 0)
        {
            label = NextId(text, languages, HolomapTexts);
            var hqr = File.ReadAllBytes(textPath);
            for (var lang = 0; lang < languages; lang++)
            {
                var words = lang < PageArrowLabel.Length ? lang : 0;
                var bank = Lba2TextBank.Load(text, lang, HolomapTexts);
                bank.Texts.Add(new Lba2TextBank.Text { Id = label, Attribute = LabelAttribute, Bytes = Dos.GetBytes(PageArrowLabel[words]) });
                // (the slot's found message, name and description: each of the game's moved to an id nothing asks for, keeping its place --
                // and so its voice, and every later text's -- and the pages' at the end, unvoiced)
                var item = new[] { PagesSlot, 100 + PagesSlot, 200 + PagesSlot };
                for (var k = 0; k < item.Length; k++)
                {
                    var own = bank.Find(item[k]);
                    var attribute = own?.Attribute ?? Lba2TextBank.NormalAttribute;
                    if (own is not null) own.Id = Retired + item[k];
                    bank.Texts.Add(new Lba2TextBank.Text { Id = item[k], Attribute = attribute, Bytes = Dos.GetBytes(PagesItemTexts[words][k]) });
                }
                hqr = bank.WriteInto(hqr);
            }
            File.WriteAllBytes(textPath, hqr);
            log.Add($"the exam pages: item {PagesSlot}'s texts (file {HolomapTexts}: {PagesSlot}, {100 + PagesSlot}, {200 + PagesSlot}) and the arrows' label ({label}), in all {languages} languages");
        }
        var objfix = Path.Combine(gameDirectory, "OBJFIX.HQR");
        HqrWriter.ReplaceInFile(objfix, PagesSlot, HqrArchive.Open(objfix).Read(PagesModelLike));
        log.Add($"the exam pages: OBJFIX.HQR entry {PagesSlot} (the pyramid key's) is entry {PagesModelLike}'s scroll of paper");

        var arrows = Enumerable.Repeat(-1, places.Length).ToArray();
        var free = Enumerable.Range(FirstFreeArrow, LastArrow - FirstFreeArrow + 1).Where(p => p != StartArrow && p != TownArrow && !store.SceneExists(p)).ToList();
        var holoPath = Path.Combine(gameDirectory, RaceTrackHolomap.File);
        var table = HqrArchive.Open(holoPath).Read(12);
        var desert = (50 + SchoolOutside) * ArrowRecordSize;
        for (var k = 0; k < places.Length; k++)
        {
            if (places[k] is not { } at) continue;
            if (k >= free.Count) { log.Add($"the exam pages: no holomap position left for page {k + 1}'s arrow"); continue; }
            var record = (50 + free[k]) * ArrowRecordSize;
            if (table.Length < record + ArrowRecordSize) { log.Add("the exam pages: the holomap's arrow table is shorter than the game's own -- no arrows"); break; }
            void Put(int field, int value) => System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(table.AsSpan(record + field * 4), value);
            Put(0, (int)Math.Round(at.X * 512)); Put(1, (int)Math.Round(at.Y)); Put(2, (int)Math.Round(at.Z * 512));
            for (var f = 3; f < 6; f++) Put(f, System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(table.AsSpan(desert + f * 4)));
            Put(6, label);
            table[record + 28] = 0xFF;                                  // no inventory object
            table[record + 29] = 0;                                     // off until a script puts it on
            table[record + 30] = 0;                                     // planet Twinsun
            table[record + 31] = (byte)RaceTrackIsland.Desert.IslandByte;
            arrows[k] = free[k];
        }
        File.WriteAllBytes(holoPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(holoPath), 12, HqrWriter.StoredEntry(table)));
        log.Add($"the exam pages' holomap arrows: {string.Join(", ", arrows.Select((a, k) => $"page {k + 1} {a}"))}");
        return arrows;
    }

    private static List<string> ApplyDesert(string gameDirectory, RaceTrackReport report, RaceTrackScenes.Result scenes)
    {
        var log = new List<string>();
        if (report.StartLine.Count == 0 || scenes.StartScene < 0) { log.Add("the Desert: no story, its track has no start line"); return log; }
        var first = AddIslandTexts(gameDirectory, RaceTrackIsland.Desert.IslandByte, DesertLines, log, "the Desert");
        if (first < 0) return log;
        int Say(DesertLine line) => first + (int)line;
        var b = new IslandBeatsA(gameDirectory, RaceTrackIsland.Desert, log);
        // (won, Baldino takes him to the Esmers' hangar: the freighter to the moon -- RaceTrackStory.Hangar.cs)
        Hangar(b, Say, MoonStart);
        // (Zoe as companion's hints on the Desert: the Dean, his pages, the sand filters, the helmet, the race -- RaceTrackStory.Companion.cs)
        CompanionTalks[RaceTrackIsland.Desert.IleFile] = $@"            if (2 > var_game({QPages}) && 7 != var_game({PagesFoundVar}))
            {{
                message({Say(DesertLine.ZoeHintDean)});
            }}
            else if ({PagesDiploma} > var_game({QPages}) && 7 != var_game({PagesFoundVar}))
            {{
                message({Say(DesertLine.ZoeHintPages)});
            }}
            else if ({PagesDiploma} > var_game({QPages}))
            {{
                message({Say(DesertLine.ZoeHintDean)});
            }}
            else if ({FiltersFitted} > var_game({QFilters}))
            {{
                message({Say(DesertLine.ZoeHintFilters)});
            }}
            else if ({Citadel.CitadelOveralls.GearBoth} > var_game({Citadel.CitadelOveralls.GlovesSlot}))
            {{
                message({Say(DesertLine.ZoeHintHelmet)});
            }}
            else
            {{
                message({Say(DesertLine.ZoeHintRace)});
            }}";
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
        change_cube({HangarScene});
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
            else if ({PagesWon} > var_game({QPages}) && {Citadel.CitadelOveralls.GearBoth} <= var_game({Citadel.CitadelOveralls.GlovesSlot}))
            {{
                message({Say(DesertLine.FlickOpen)});
            }}
            if ({Citadel.CitadelOveralls.GearBoth} > var_game({Citadel.CitadelOveralls.GlovesSlot}))
            {{
                message({Say(DesertLine.FlickHelmet)});
                set_holo_pos({PearlCave});
            }}", "Flick");

        // the three pages' places on the road, at 30, 55 and 80 % of the lap (each found driving over it or walking to it, in lap order),
        // and the pages as Twinsen carries them: the item and the arrows (PagesInstall)
        var pages = new[] { (0.30, DesertLine.Page1), (0.55, DesertLine.Page2), (0.80, DesertLine.Page3) };
        var pagePlaces = pages.Select(p => OnLap(report, p.Item1)).ToArray();
        var pageArrows = PagesInstall(gameDirectory, b.Store, pagePlaces, log);
        var arrowsOn = string.Concat(pageArrows.Select((a, k) => (a, k)).Where(x => x.a >= 0).Select(x => $"\n                if ({PageNotFound(x.k)})\n                {{\n                    set_holo_pos({x.a});\n                }}"));

        // the Dean in the School of Magic: a copy of the game's own talks (the game's is out of sight on the Desert's chapters). The hall is
        // the 1996 demo's when the build put it in (Demo96Rooms): its Dean, another actor there, is the one copied -- the actor at the
        // Dean's place, as the scene now has it.
        var deanActor = b.Store.SceneExists(DeanScene) && b.Store.Load(DeanScene).Actors.FindIndex(a => a.X == DeanAt.X && a.Z == DeanAt.Z) is >= 0 and var found ? found : DeanActor;
        if (Copy(b.Store, DeanScene, deanActor) is { } dean)
        {
            b.Hide(DeanScene, deanActor, desert, "Dean");
            b.Edit(DeanScene, "the Dean: the three pages (Q_PAGES 2), the Wizard's Diploma (item 30, Q_PAGES 6)", model => IslandBeatsA.Add(model, dean, Talker(desert, DeanEggs(Say) + $@"            if (5 == var_game({QPages}) || (2 > var_game({QPages}) && 7 == var_game({PagesFoundVar})))
            {{
                message({Say(DesertLine.DeanAll)});
                set_var_game({PagesSlot}, 0);
                found_object({Diploma});
                set_var_game({Diploma}, 1);
                set_var_game({QPages}, 6);
            }}
            else if (2 > var_game({QPages}))
            {{
                message({Say(DesertLine.DeanShut)});
                message_zoe({Say(DesertLine.ZoePages)});
                set_var_game({QPages}, 2);{arrowsOn}
                {PagesBeforeDean()}
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
        // The giant clam there keeps the racing helmet now, in the pearl's place (item 5 -- Citadel/CitadelOveralls; the Desert race's gate
        // wants it): opened as the game opens it, Action by it and it's Twinsen's. Not shut for good by what the story has no use for --
        // the Ball of Sendell (variable 3) or the Ring of Lightning (19, the story's errands' slot now: the tin of bait would have shut
        // it) -- and no word to the game's quest after it (83).
        b.Edit(PearlCave, "the Pearl of Incandescence's cave: Twinsen's look round is a text of the game's (337, not 327); the clam gives the racing helmet, whatever the errands", _ => scripts =>
        {
            var hero = Loose(scripts.GetText(0, ScriptKind.Life), "message(327);", "message(337);");
            hero = Loose(hero, "if (21 == l_track_obj(5) && 0 == var_game(169) && 0 == var_game(19) && 0 == var_game(3) && 2500 > distance_message(5))\n{\nset_var_game(169, 1);\nset_var_game(83, 11);\n}",
                "if (21 == l_track_obj(5) && 0 == var_game(169) && 2500 > distance_message(5))\n{\nset_var_game(169, 1);\n}");
            scripts.SetText(0, ScriptKind.Life, hero);
            // (and the helmet into the gloves' slot, its second kind -- Citadel/CitadelOveralls.GearModel: one slot for the racing gear)
            var clam = Loose(scripts.GetText(PearlClam, ScriptKind.Life), "if (1 == var_game(3) || 1 == var_game(19) || 1 == var_game(169))", "if (1 == var_game(169))");
            clam = Loose(clam, "set_var_game(5, 1);\nstate_inventory(5, 0);\nfound_object(5);",
                $"set_var_game({Citadel.CitadelOveralls.GlovesSlot}, {Citadel.CitadelOveralls.GearBoth});\nfound_object({Citadel.CitadelOveralls.GlovesSlot});");
            scripts.SetText(PearlClam, ScriptKind.Life, clam);
        });

        // the three pages on the road, at 30, 55 and 80 % of the lap: driven over or walked to, in any order (the user, 2026-11-08: "can we
        // have these count in any order and go into inventory") -- each a page lying on the road to be seen (CitadelProps.Page), once the
        // Dean has told of them; found, one more in Twinsen's inventory (item PagesSlot's count, and Q_PAGES with it: 3, 4, then 5 -- all
        // three), its own arrow off, and gone. Which pages are found is a bit each in one variable (PagesFoundVar); the lines go by how many are found,
        // not by which. On the road at its height, and never under the ground there ("check that they're all above ground")
        var lines = pages.Select(pg => pg.Item2).ToArray();
        string ByCount() => $@"if (1 == var_game({PagesFoundVar}) || 2 == var_game({PagesFoundVar}) || 4 == var_game({PagesFoundVar}))
        {{
            message({Say(lines[0])});
        }}
        else if (3 == var_game({PagesFoundVar}) || 5 == var_game({PagesFoundVar}) || 6 == var_game({PagesFoundVar}))
        {{
            message({Say(lines[1])});
        }}
        else
        {{
            message({Say(lines[2])});
        }}";
        for (var k = 0; k < pages.Length; k++)
        {
            var fraction = pages[k].Item1;
            var arrowOff = pageArrows[k] >= 0 ? $"\n            clr_holo_pos({pageArrows[k]});" : "";
            if (pagePlaces[k] is not { } at) { log.Add($"page {k + 1} left out: no ground on the lap there"); continue; }
            var scene = b.SceneAt(at.X, at.Z);
            if (scene < 0) { log.Add($"page {k + 1} left out: no scene at cell ({at.X:0.#}, {at.Z:0.#})"); continue; }
            var ground = report.GroundAfter?.Invoke(at.X, at.Z) ?? at.Y;
            var y = Math.Max(at.Y, ground) + 8;
            if (ground > at.Y + 16) log.Add($"page {k + 1}: the lap's line ({at.Y:0}) under the ground there ({ground:0}) -- on the ground");
            var bit = 1 << k;
            b.Edit(scene, $"exam page {k + 1} on the road at cell ({at.X:0.#}, {at.Z:0.#}, height {y:0}) ({fraction:0%} of the lap): found in any order (bit {bit} of variable {PagesFoundVar})", model =>
            {
                var page = Citadel.CitadelProps.Actor(Citadel.CitadelProps.Page, Math.Clamp(RaceTrackScenes.CubeLocal(at.X, model.CubeX), 0, IslandFile.CubeSize - 1), (int)Math.Round(y),
                    Math.Clamp(RaceTrackScenes.CubeLocal(at.Z, model.CubeY), 0, IslandFile.CubeSize - 1), 700 * k);
                page.CoulObj = 9;
                return IslandBeatsA.Add(model, page, $@"void comportement_0()
{{
    if ({desert} && ({PageNotFound(k)}))
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
    if ({PagesDiploma} <= var_game({QPages}))
    {{
        invisible(1);
    }}
    else
    {{
        invisible(0);
        if (1400 > distance(0))
        {{
            add_var_game({PagesFoundVar}, {bit});
            if (2 <= var_game({QPages}))
            {{
                add_var_game({QPages}, 1);
            }}
            add_var_game({PagesSlot}, 1);{arrowOff}
            {ByCount()}
            invisible(1);
            suicide();
        }}
    }}
}}
");
            });
        }

        // Baldino at his house: a copy of the game's own, the car's tune and his tip -- his greeting as his radio call was answered or not
        // (the sweep of 2026-11-05: "My radio works then" whether it was or not)
        // (and at the racers' garage before the race is won -- never in both: RaceTrackStory.Garage.cs)
        DesertGarage(gameDirectory, b, report, Say, desert, Copy(b.Originals, BaldinoHouse, BaldinoActor), scenes.StartScene, log);
        if (Copy(b.Originals, BaldinoHouse, BaldinoActor) is { } baldino)
        {
            b.Hide(BaldinoHouse, BaldinoActor, desert, "Baldino");
            b.Edit(BaldinoHouse, "Baldino at his workbench, once he's not at the racers' garage: his tip, and he races too", model => IslandBeatsA.Add(model, baldino, Talker($"{desert} && {PagesWon} <= var_game({QPages})", $@"            if (3 == var_game({RadioAnsweredA}))
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
        change_cube({HangarScene});
    }}
}}
"),
            };
            return scripts => { foreach (var e in edits) e(scripts); };
        });

        // The School's door from the canyon (the user, 2026-11-06: the gate "can't be walked into"; the road's side of it, the gates, is
        // RaceTrackIsland.GateSill): its zone (x 9216-9728) lies behind the doorway, which stops Twinsen walking in at x 9826 (headless,
        // 2026-11-07, and the playthrough's ch03 06_school_outside, which teleported into it). It reaches a cell out now, in front of the
        // doorway (to x 10240), and lands him 768 further from the entrance's door back out: the zone turns him a quarter round (Info3
        // 1), so his x in the zone (walking in, its far side: 1024) is his z in scene 28 past its Info2 -- with Info2 as it was, z 11776,
        // on that door (z 11264-11776), and he went straight back out. Now z 9984-11008: 11008 walking in, where scene 28 starts him.
        foreach (var scene in new[] { SchoolOutside, SchoolOutsideToo })
            if (b.Store.SceneExists(scene) && b.Store.Load(scene).Zones.Any(IsSchoolDoor))
                b.Edit(scene, $"the School's door reaches out in front of its doorway (to x {SchoolDoorReach}) and lands Twinsen clear of the way back out", model =>
                {
                    foreach (var door in model.Zones.Where(IsSchoolDoor)) { door.X1 = SchoolDoorReach; door.Info[2] = SchoolDoorLanding; }
                    return _ => { };
                });
        // (and the way back out lands him in front of the door's new reach, not in it: x 9728 plus his z in the entrance's door (0-512, turned
        // a quarter round), in the zone as it reaches now, took him straight back in)
        if (b.Store.SceneExists(SchoolEntrance) && b.Store.Load(SchoolEntrance).Zones.Any(IsSchoolWayOut))
            b.Edit(SchoolEntrance, $"the School's way out lands Twinsen in front of its door's reach outside (x {SchoolOutLanding} on)", model =>
            {
                foreach (var door in model.Zones.Where(IsSchoolWayOut)) door.Info[0] = SchoolOutLanding;
                return _ => { };
            });

        // (the easter eggs: the fourth page, Bù's stone -- RaceTrackStory.Eggs.Desert.cs)
        DesertEggs(b, report, Say, desert);
        b.Save();
        log.Add($"the Desert's race file: gate={QPages} 6 (the diploma), win={QPages} {PagesWon}");
        return log;
    }
}
