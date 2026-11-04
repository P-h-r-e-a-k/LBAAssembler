using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Chapter 8 of the story (docs/LBRA_WALKTHROUGH.md), the Elevator Platform (ASCENCE, scene 120): no quest (the user, 2026-11-03: "The
// elevator doesn't need a quest, perhaps some pre-race dialog from the Monkey monster, who I feel should be the only opponent of Twinsen for
// this race"). The race is him alone (RaceTrackIsland.StoryHeadToHead).
//   - arrival: Lyra's word (once a visit), then Baldino's call on the radio (GroupB.Arrival; var 236 8);
//   - the monkey monster on foot where the game has him (scene 120's actor 2, a copy: the build takes the game's own off for his script),
//     out of sight while Twinsen drives: as Twinsen comes near, his four lines and Zoe's; Action again, "Race! Now!"; Twinsen in his car
//     the first time a visit, Zoe off to the lift cage and his "RAAARGH! Go!";
//   - the race (RaceCarEngineFile: finish= sets 246, win= the chapter to 9): lost, his laugh; won, his clue and Lyra's, and down the
//     elevator to Wannies Island (scene 98).
internal static partial class RaceTrackStory
{
    private const int ElevatorChapter = 8, PlatformScene = 120, ElevatorTexts = 3 + 10, MonsterActor = 2, WanniesArrival = 98;

    internal enum ElevatorLine { LyraArrival, Ring, Baldino, Monster1, Monster2, Monster3, ZoeSneaked, Monster4, Again, ZoeCage, Go, Lost, Won1, Won2, LyraDown }
    internal static readonly string[][] ElevatorLines =
    {
        new[]
        {
            "Twinsen! Welcome to the Elevator Platform. The monster won't let anybody down to the undergas without a race. It's just you and him.",
            "Twinsen ! Bienvenue sur la plate-forme de l'ascenseur. Le monstre ne laisse descendre personne sous le gaz sans une course. Ce sera toi contre lui.",
            "Twinsen! Willkommen auf der Aufzugsplattform. Das Monster lässt niemanden ohne Rennen unter das Gas hinunter. Nur du gegen ihn.",
            "¡Twinsen! Bienvenido a la plataforma del ascensor. El monstruo no deja bajar a nadie bajo el gas sin una carrera. Solo tú contra él.",
            "Twinsen! Benvenuto sulla piattaforma dell'ascensore. Il mostro non lascia scendere nessuno sotto il gas senza una corsa. Solo tu contro di lui.",
            "Twinsen! Bem-vindo à plataforma do elevador. O monstro não deixa ninguém descer para debaixo do gás sem uma corrida. Somos só tu e ele.",
        },
        GroupB.Ring,
        new[]
        {
            "Baldino here! That platform's a rollercoaster. Grip, grip and more grip, I've given you all I've got. Hold on to your hat. And your lunch.",
            "Ici Baldino ! Cette plate-forme, c'est des montagnes russes. De l'adhérence, encore de l'adhérence, je t'ai donné tout ce que j'avais. Tiens bien ton chapeau. Et ton déjeuner.",
            "Hier Baldino! Diese Plattform ist eine Achterbahn. Haftung, Haftung und noch mehr Haftung, ich habe dir alles gegeben, was ich habe. Halt deinen Hut fest. Und dein Mittagessen.",
            "¡Aquí Baldino! Esa plataforma es una montaña rusa. Agarre, agarre y más agarre, te he dado todo lo que tengo. Sujeta el sombrero. Y el almuerzo.",
            "Qui Baldino! Quella piattaforma è delle montagne russe. Aderenza, aderenza e ancora aderenza, ti ho dato tutto quello che avevo. Tieniti il cappello. E il pranzo.",
            "Fala o Baldino! Aquela plataforma é uma montanha-russa. Aderência, aderência e mais aderência, dei-te tudo o que tenho. Segura o chapéu. E o almoço.",
        },
        new[]
        {
            "RAAARGH! You! Small blue one! You race me?",
            "RAAARGH ! Toi ! Petit bleu ! Toi faire course avec moi ?",
            "RAAARGH! Du! Kleiner Blauer! Du fahren Rennen gegen mich?",
            "¡RAAARGH! ¡Tú! ¡Pequeño azul! ¿Tú correr contra mí?",
            "RAAARGH! Tu! Piccolo blu! Tu correre contro me?",
            "RAAARGH! Tu! Pequeno azul! Tu correr contra mim?",
        },
        new[]
        {
            "This my platform. My elevator. Nobody go down without race. Nobody beat me down. Nobody! Ever!",
            "Ça, ma plate-forme. Mon ascenseur. Personne descendre sans course. Personne me battre en bas. Personne ! Jamais !",
            "Das meine Plattform. Mein Aufzug. Keiner runter ohne Rennen. Keiner schlagen mich nach unten. Keiner! Nie!",
            "Esta mi plataforma. Mi ascensor. Nadie bajar sin carrera. Nadie ganarme abajo. ¡Nadie! ¡Nunca!",
            "Questa mia piattaforma. Mio ascensore. Nessuno scendere senza corsa. Nessuno battere me giù. Nessuno! Mai!",
            "Esta minha plataforma. Meu elevador. Ninguém descer sem corrida. Ninguém me vencer lá em baixo. Ninguém! Nunca!",
        },
        new[]
        {
            "Black car came. At night. While I sleep! Not fair! Not a race! I wake up, he gone. RAAARGH!",
            "Voiture noire venue. La nuit. Pendant moi dormir ! Pas juste ! Pas une course ! Moi réveillé, lui parti. RAAARGH !",
            "Schwarzes Auto gekommen. In der Nacht. Während ich schlafen! Nicht fair! Kein Rennen! Ich aufwachen, er weg. RAAARGH!",
            "Coche negro vino. De noche. ¡Mientras yo dormir! ¡No justo! ¡No carrera! Yo despertar, él irse. ¡RAAARGH!",
            "Macchina nera venuta. Di notte. Mentre io dormire! Non giusto! Non una corsa! Io svegliare, lui sparito. RAAARGH!",
            "Carro preto veio. De noite. Enquanto eu dormir! Não justo! Não corrida! Eu acordar, ele ido. RAAARGH!",
        },
        new[]
        {
            "So the masked driver sneaked past him. No wonder he's in a mood.",
            "Alors le pilote masqué est passé en douce. Pas étonnant qu'il soit de mauvaise humeur.",
            "Der maskierte Fahrer hat sich also an ihm vorbeigeschlichen. Kein Wunder, dass er schlechte Laune hat.",
            "Así que el piloto enmascarado se le coló. No me extraña que esté de mal humor.",
            "Quindi il pilota mascherato gli è passato di nascosto. Non c'è da stupirsi che sia di cattivo umore.",
            "Então o piloto mascarado passou-lhe às escondidas. Não admira que esteja de mau humor.",
        },
        new[]
        {
            "You. You race fair? Down to bottom, up to top. My little ones throw crates. That is fair too. Ha!",
            "Toi. Toi courir loyal ? En bas, puis en haut. Mes petits jeter caisses. Ça aussi, loyal. Ha !",
            "Du. Du fahren fair? Runter nach unten, hoch nach oben. Meine Kleinen werfen Kisten. Das auch fair. Ha!",
            "Tú. ¿Tú correr limpio? Abajo del todo, arriba del todo. Mis pequeños tirar cajas. Eso también limpio. ¡Ja!",
            "Tu. Tu correre leale? Giù in fondo, su in cima. Miei piccoli lanciare casse. Anche questo leale. Ah!",
            "Tu. Tu correr limpo? Lá em baixo, lá em cima. Meus pequenos atirar caixotes. Isso também limpo. Ah!",
        },
        new[]
        {
            "Hrrm. You still here? Race! Now!",
            "Hrrm. Toi encore là ? Course ! Maintenant !",
            "Hrrm. Du noch hier? Rennen! Jetzt!",
            "Hrrm. ¿Tú todavía aquí? ¡Carrera! ¡Ya!",
            "Hrrm. Tu ancora qui? Corsa! Adesso!",
            "Hrrm. Tu ainda aqui? Corrida! Já!",
        },
        new[]
        {
            "No pit stops on this one. I'll be in the lift cage, watching. Just don't fall off.",
            "Pas d'arrêt aux stands sur celle-ci. Je serai dans la cage de l'ascenseur, à regarder. Ne tombe pas, c'est tout.",
            "Keine Boxenstopps bei diesem hier. Ich bin im Aufzugskäfig und schaue zu. Fall bloß nicht runter.",
            "En esta no hay paradas en boxes. Estaré en la jaula del ascensor, mirando. Tú no te caigas.",
            "Niente pit stop in questa. Sarò nella gabbia dell'ascensore, a guardare. Solo, non cadere.",
            "Nesta não há paragens nas boxes. Vou estar na jaula do elevador, a ver. Só não caias.",
        },
        new[]
        {
            "RAAARGH! Go!",
            "RAAARGH ! Partez !",
            "RAAARGH! Los!",
            "¡RAAARGH! ¡Ya!",
            "RAAARGH! Via!",
            "RAAARGH! Já!",
        },
        new[]
        {
            "Ha! Too slow! Again? Again! RAAARGH!",
            "Ha ! Trop lent ! Encore ? Encore ! RAAARGH !",
            "Ha! Zu langsam! Nochmal? Nochmal! RAAARGH!",
            "¡Ja! ¡Muy lento! ¿Otra vez? ¡Otra vez! ¡RAAARGH!",
            "Ah! Troppo lento! Ancora? Ancora! RAAARGH!",
            "Ah! Muito lento! Outra vez? Outra vez! RAAARGH!",
        },
        new[]
        {
            "Hrrm. Good race. Grr. Real race. Not like black car.",
            "Hrrm. Bonne course. Grr. Vraie course. Pas comme voiture noire.",
            "Hrrm. Gutes Rennen. Grr. Echtes Rennen. Nicht wie schwarzes Auto.",
            "Hrrm. Buena carrera. Grr. Carrera de verdad. No como coche negro.",
            "Hrrm. Bella corsa. Grr. Corsa vera. Non come macchina nera.",
            "Hrrm. Boa corrida. Grr. Corrida a sério. Não como carro preto.",
        },
        new[]
        {
            "Black car went down behind the big tanker. Tanker never came up. Black car never came up. Something down there, under the gas. You go see. You fast. Grr. Good.",
            "Voiture noire descendue derrière gros camion-citerne. Citerne jamais remontée. Voiture noire jamais remontée. Quelque chose en bas, sous le gaz. Toi aller voir. Toi rapide. Grr. Bien.",
            "Schwarzes Auto runter hinter großem Tankwagen. Tankwagen nie wieder hoch. Schwarzes Auto nie wieder hoch. Etwas da unten, unter dem Gas. Du gehen nachsehen. Du schnell. Grr. Gut.",
            "Coche negro bajar detrás de camión cisterna grande. Cisterna nunca subir. Coche negro nunca subir. Algo ahí abajo, bajo el gas. Tú ir a ver. Tú rápido. Grr. Bien.",
            "Macchina nera scesa dietro grande autocisterna. Cisterna mai risalita. Macchina nera mai risalita. Qualcosa laggiù, sotto il gas. Tu andare a vedere. Tu veloce. Grr. Bene.",
            "Carro preto desceu atrás de camião-cisterna grande. Cisterna nunca subiu. Carro preto nunca subiu. Algo lá em baixo, debaixo do gás. Tu ir ver. Tu rápido. Grr. Bom.",
        },
        new[]
        {
            "The elevator is yours. Down we go!",
            "L'ascenseur est à toi. On descend !",
            "Der Aufzug gehört dir. Abwärts!",
            "El ascensor es tuyo. ¡Para abajo!",
            "L'ascensore è tuo. Si scende!",
            "O elevador é teu. Vamos descer!",
        },
    };

    // Group B's islands of chapters 8-10 (RaceTrackService.Story): the island's own beats, before its retelling
    public static List<string> ApplyMiddle(string gameDirectory, RaceTrackReport report, RaceTrackIsland island, RaceTrackScenes.Result scenes)
    {
        if (island == RaceTrackIsland.Elevator) return ApplyElevator(gameDirectory, report);
        if (island == RaceTrackIsland.Wannies) return ApplyWannies(gameDirectory, report, scenes);
        if (island == RaceTrackIsland.Mosquibe) return ApplyMosquibees(gameDirectory, report, scenes);
        return new List<string>();
    }

    // What the race file says for group B's chapters 8-10 in the story (RaceCarEngineFile): a race's end sets 237 (the after-race lines), a
    // win moves the chapter on; Wannies Island's start line waits for the miner's glove, Mosquibees Island's for the ring shown
    public static string StoryRaceKeysB(RaceTrackIsland island)
    {
        var chapter = island == RaceTrackIsland.Elevator ? ElevatorChapter : island == RaceTrackIsland.Wannies ? WanniesChapter : island == RaceTrackIsland.Mosquibe ? MosquibeesChapter : -1;
        if (chapter < 0) return "";
        var keys = $"win=253 {chapter + 1}\nfinish={RaceDoneB} 1\n";
        if (island == RaceTrackIsland.Wannies) keys += $"gate={QGlove} {GloveBack} The miner: no glove, no driving!\n";
        if (island == RaceTrackIsland.Mosquibe) keys += $"gate={QPass} {RingShown} No pass, no grid. The Queen's orders.\n";
        return keys;
    }

    private static List<string> ApplyElevator(string gameDirectory, RaceTrackReport report)
    {
        var log = new List<string>();
        var first = GroupB.AddLines(gameDirectory, ElevatorTexts, ElevatorLines, log, "the Elevator Platform's (chapter 8)");
        int Say(ElevatorLine l) => first + (int)l;
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var originals = RaceTrackService.OriginalScenes(store);
        var monster = originals.SceneExists(PlatformScene) ? originals.Load(PlatformScene).Actors.ElementAtOrDefault(MonsterActor)?.Clone() : null;
        var when = $"{ElevatorChapter} == chapter()";
        GroupB.EditScene(store, PlatformScene, log, "Lyra's word, Baldino's call, the monkey monster on foot before the race and his lines after it", model =>
        {
            var edits = new List<Action<SceneScripts>>();
            edits.Add(GroupB.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 9),
                GroupB.Arrival(when, ElevatorChapter, "0 == var_cube(44)", "set_var_cube(44, 1);", Say(ElevatorLine.LyraArrival), Say(ElevatorLine.Ring), Say(ElevatorLine.Baldino))));
            var monsterIndex = -1;
            if (monster is not null)
            {
                // (where the game has him, or the nearest place a cell or two aside that is out of the cars' way)
                double cx = model.CubeX * 64 + monster.X / 512.0, cz = model.CubeY * 64 + monster.Z / 512.0;
                var placed = false;
                foreach (var (dx, dz) in new[] { (0.0, 0.0), (-1.5, 0.0), (0.0, 1.5), (0.0, -1.5), (-3.0, 0.0), (1.5, 0.0), (-1.5, 1.5), (-1.5, -1.5) })
                    if (!report.InTheWay(cx + dx, cz + dz, monster.Y - 200, monster.Y + 1500))
                    {
                        monster.X = RaceTrackScenes.CubeLocal(cx + dx, model.CubeX); monster.Z = RaceTrackScenes.CubeLocal(cz + dz, model.CubeY);
                        placed = true;
                        log.Add($"the monkey monster on foot at cell ({cx + dx:0.#}, {cz + dz:0.#}), {monster.Y}");
                        break;
                    }
                if (!placed) log.Add("the monkey monster on foot left out: every place by his own is in the cars' way");
                else
                {
                    monsterIndex = model.Actors.Count;
                    edits.Add(GroupB.Add(model, monster, $@"void comportement_0()
{{
    if ({when})
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
    if ({Driving} == comportement_hero())
    {{
        invisible(1);
        if (0 == var_cube(46))
        {{
            set_var_cube(46, 1);
            message_zoe({Say(ElevatorLine.ZoeCage)});
            message({Say(ElevatorLine.Go)});
        }}
    }}
    else
    {{
        invisible(0);
        if (0 == var_cube(47) && 4500 > distance(0))
        {{
            set_var_cube(47, 1);
            message({Say(ElevatorLine.Monster1)});
            message({Say(ElevatorLine.Monster2)});
            message({Say(ElevatorLine.Monster3)});
            message_zoe({Say(ElevatorLine.ZoeSneaked)});
            message({Say(ElevatorLine.Monster4)});
        }}
    }}
    swif (1 == action())
    {{
        if (1500 > distance(0) && {Driving} != comportement_hero() && 1 == var_cube(47))
        {{
            message({Say(ElevatorLine.Again)});
        }}
    }}
}}
"));
                }
            }
            else log.Add("the monkey monster on foot left out: no actor 2 in the game's scene 120");
            // (after the race, Twinsen out of his car: lost, the monster laughs; won -- the race's win made it chapter 9 -- his clue, Lyra's word,
            // and down to Wannies Island)
            var speaker = monsterIndex >= 0 ? $"message_obj({monsterIndex}, " : "message(";
            edits.Add(GroupB.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 9), $@"void comportement_0()
{{
    if ({when})
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
    if (1 == var_game({RaceDoneB}) && {Driving} != comportement_hero())
    {{
        set_var_game({RaceDoneB}, 0);
        if ({ElevatorChapter} < chapter())
        {{
            {speaker}{Say(ElevatorLine.Won1)});
            {speaker}{Say(ElevatorLine.Won2)});
            message({Say(ElevatorLine.LyraDown)});
            change_cube({WanniesArrival});
        }}
        else
        {{
            {speaker}{Say(ElevatorLine.Lost)});
            set_var_cube(46, 0);
        }}
    }}
}}
"));
            return scripts => { foreach (var e in edits) e(scripts); };
        });
        return log;
    }
}
