using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The race mod's story, chapter 14: round the statue, the final (docs/LBRA_WALKTHROUGH.md; docs/LBRA_STORY_SCRIPT.md), on Celebration
// Island's statue track (RaceTrackIsland.Celebration: scene 95, the dock, with the statue risen -- var 79). Celebration's text file (8)
// is the lava lake's too: these lines go after whatever is there. In scene 95:
//   - The landing: the palace's sprint won (chapter 13, var 79 1), the teleporter sets Twinsen down on the dock -- FunFrock's voice over
//     the flight (unseen), the final's steps started afresh (var 239 0: the slabs' bits, free since chapter 11) and inc_chapter (14).
//   - FunFrock (the game's, entity 217 with his body 313, unmasked) by the start line: his first line by DREAM_CAUGHT (var 229: 1 Twinsen
//     caught him in the dream) (239 1).
//   - Baldino's last call (LateStory.RadioCall): best setup and a super jet-pack in the boot.
//   - The crowd: Zoe (scene 0's actor 4), Raph (scene 2's 2), Mr. Paul (scene 7's 2), Baldino (entity 93: BODY 139) and Flick (an
//     invisible talker beside them: her body is the race-track mode's marshal's), each with a line.
//   - The race: a loss (finish=224 7: Q_EMPEROR is done with after the palace) -- FunFrock's "again?", and the race again (the scene
//     again: the race-track mode starts a finished race afresh on re-entry). A win (win=239 2): Lyra, FunFrock, and the Esmer's wish
//     (ask_choice): "the statue gone, and FunFrock with it" -- granted, var 79 0, scene 95 again as it was (239 3), and the dock after:
//     Zoe, Raph, Baldino, the Esmer; inc_chapter (15) and the_end(). "Another race" -- FunFrock's "you'll regret that" and the race again
//     (224 8); chosen a second time, his laugh and the_end().
// Not built: the super jet-pack in the item box at the start (the race file has no start_item= key: RACEMOD.CPP would need one); the
// slate's last page; Flick seen.
internal static partial class RaceTrackStory
{
    // the final's steps (var 239, reused): 1 FunFrock has spoken, 2 won (win=), 3 the wish granted
    public const int FinalStep = 239, FinalSpoke = 1, FinalWon = 2, FinalGranted = 3;
    // the race run (finish=224 7) and another race asked for once (224 8)
    public const int FinalRaced = 7, FinalAgain = 8;
    private const int FinalChapter = 14, FinalTexts = 3 + 5, StatueRisen = 79;
    // the game's: FunFrock (entity 217, his body's generic 154, standing 1440), Baldino (entity 93, standing 715)
    private const int FunFrockEntity = 217, FunFrockBody = 154, FunFrockStand = 1440, BaldinoEntity = 93, BaldinoStand = 715;

    private enum FLine
    {
        Flight, Caught, NotCaught, Waiting, Ring, Baldino, Zoe, Raph, Flick, Paul, Again, Won, FunFrockNo, EsmerAsk, Gone, AnotherRace,
        Granted, Regret, Laugh, ZoeHome, RaphWin, BaldinoCar, EsmerNextYear,
    }
    private static readonly string[][] FinalLines =
    {
        new[]
        {
            "FunFrock: Very good, Twinsen. Now come and finish it. I'll be waiting, just like in your dream.",
            "FunFrock : Très bien, Twinsen. Maintenant viens en finir. Je t'attendrai, comme dans ton rêve.",
            "FunFrock: Sehr gut, Twinsen. Jetzt komm und bring es zu Ende. Ich warte auf dich, genau wie in deinem Traum.",
            "FunFrock: Muy bien, Twinsen. Ahora ven a terminarlo. Te esperaré, igual que en tu sueño.",
            "FunFrock: Molto bene, Twinsen. Ora vieni a finirla. Ti aspetterò, proprio come nel tuo sogno.",
            "FunFrock: Muito bem, Twinsen. Agora vem acabar com isto. Vou estar à tua espera, tal como no teu sonho.",
        },
        new[]
        {
            "We raced once, Twinsen, in your dream. You caught me, in your dream. This time it's real, and the wish is mine.",
            "Nous avons fait la course une fois, Twinsen, dans ton rêve. Tu m'as rattrapé, dans ton rêve. Cette fois c'est pour de vrai, et le vœu est à moi.",
            "Wir sind schon einmal gegeneinander gefahren, Twinsen, in deinem Traum. Du hast mich eingeholt, in deinem Traum. Diesmal ist es echt, und der Wunsch gehört mir.",
            "Ya corrimos una vez, Twinsen, en tu sueño. Me alcanzaste, en tu sueño. Esta vez es de verdad, y el deseo es mío.",
            "Abbiamo già corso una volta, Twinsen, nel tuo sogno. Mi hai preso, nel tuo sogno. Questa volta è vero, e il desiderio è mio.",
            "Já corremos uma vez, Twinsen, no teu sonho. Apanhaste-me, no teu sonho. Desta vez é a sério, e o desejo é meu.",
        },
        new[]
        {
            "We raced once, Twinsen, in your dream. You never caught me then, and you won't now. This time it's real, and the wish is mine.",
            "Nous avons fait la course une fois, Twinsen, dans ton rêve. Tu ne m'as jamais rattrapé, et tu ne me rattraperas pas maintenant. Cette fois c'est pour de vrai, et le vœu est à moi.",
            "Wir sind schon einmal gegeneinander gefahren, Twinsen, in deinem Traum. Damals hast du mich nie eingeholt, und jetzt auch nicht. Diesmal ist es echt, und der Wunsch gehört mir.",
            "Ya corrimos una vez, Twinsen, en tu sueño. Entonces nunca me alcanzaste, y ahora tampoco lo harás. Esta vez es de verdad, y el deseo es mío.",
            "Abbiamo già corso una volta, Twinsen, nel tuo sogno. Allora non mi hai mai preso, e non mi prenderai adesso. Questa volta è vero, e il desiderio è mio.",
            "Já corremos uma vez, Twinsen, no teu sonho. Nunca me apanhaste, e não me vais apanhar agora. Desta vez é a sério, e o desejo é meu.",
        },
        new[]
        {
            "The grid is waiting, Twinsen. So am I.",
            "La grille de départ t'attend, Twinsen. Moi aussi.",
            "Die Startaufstellung wartet, Twinsen. Ich auch.",
            "La parrilla te espera, Twinsen. Yo también.",
            "La griglia ti aspetta, Twinsen. Anch'io.",
            "A grelha está à tua espera, Twinsen. Eu também.",
        },
        new[]
        {
            "The radio is ringing. Answer it from your inventory.",
            "La radio sonne. Réponds depuis ton inventaire.",
            "Das Funkgerät klingelt. Antworte über dein Inventar.",
            "La radio está sonando. Contesta desde tu inventario.",
            "La radio sta squillando. Rispondi dal tuo inventario.",
            "O rádio está a tocar. Atende pelo teu inventário.",
        },
        new[]
        {
            "Baldino: Twinsen. Everything I have is on that car. Best setup, and one last thing in the boot: a super jet-pack. Use it on the spiral. And Twinsen... win.",
            "Baldino : Twinsen. Tout ce que j'ai est sur cette voiture. Le meilleur réglage, et une dernière chose dans le coffre : un super jet-pack. Sers-t'en dans la spirale. Et Twinsen... gagne.",
            "Baldino: Twinsen. Alles, was ich habe, steckt in diesem Wagen. Die beste Abstimmung, und eine letzte Sache im Kofferraum: ein Super-Jetpack. Nimm es in der Spirale. Und Twinsen... gewinn.",
            "Baldino: Twinsen. Todo lo que tengo está en ese coche. La mejor puesta a punto, y una última cosa en el maletero: un súper jet-pack. Úsalo en la espiral. Y Twinsen... gana.",
            "Baldino: Twinsen. Tutto quello che ho è su quella macchina. La messa a punto migliore, e un'ultima cosa nel bagagliaio: un super jet-pack. Usalo nella spirale. E Twinsen... vinci.",
            "Baldino: Twinsen. Tudo o que tenho está nesse carro. A melhor afinação, e uma última coisa na mala: um super jet-pack. Usa-o na espiral. E Twinsen... ganha.",
        },
        new[]
        {
            "Go on, Twinsen! For Citadel!",
            "Vas-y, Twinsen ! Pour la Citadelle !",
            "Los, Twinsen! Für die Zitadelle!",
            "¡Vamos, Twinsen! ¡Por la Ciudadela!",
            "Forza, Twinsen! Per la Cittadella!",
            "Vai, Twinsen! Pela Cidadela!",
        },
        new[]
        {
            "Beat his time like you beat mine!",
            "Bats son temps comme tu as battu le mien !",
            "Schlag seine Zeit, wie du meine geschlagen hast!",
            "¡Bate su tiempo como batiste el mío!",
            "Batti il suo tempo come hai battuto il mio!",
            "Bate o tempo dele como bateste o meu!",
        },
        new[]
        {
            "Lyra's flag, my cheering.",
            "Le drapeau de Lyra, mes encouragements.",
            "Lyras Flagge, mein Jubel.",
            "La bandera de Lyra, mis ánimos.",
            "La bandiera di Lyra, il mio tifo.",
            "A bandeira da Lyra, a minha claque.",
        },
        new[]
        {
            "Six cars? Five will do, today. Rules are rules, but this is the final.",
            "Six voitures ? Cinq suffiront, aujourd'hui. Le règlement c'est le règlement, mais c'est la finale.",
            "Sechs Wagen? Fünf reichen heute. Regeln sind Regeln, aber das ist das Finale.",
            "¿Seis coches? Hoy bastan cinco. Las normas son las normas, pero esto es la final.",
            "Sei macchine? Oggi ne bastano cinque. Le regole sono regole, ma questa è la finale.",
            "Seis carros? Hoje chegam cinco. Regras são regras, mas isto é a final.",
        },
        new[]
        {
            "Again? I have all day.",
            "Encore ? J'ai toute la journée.",
            "Noch einmal? Ich habe den ganzen Tag Zeit.",
            "¿Otra vez? Tengo todo el día.",
            "Ancora? Ho tutto il giorno.",
            "Outra vez? Tenho o dia todo.",
        },
        new[]
        {
            "Twinsen of Citadel Island wins the Two Worlds Cup!",
            "Twinsen de l'île de la Citadelle remporte la Coupe des Deux Mondes !",
            "Twinsen von der Zitadelleninsel gewinnt den Pokal der Zwei Welten!",
            "¡Twinsen de la isla de la Ciudadela gana la Copa de los Dos Mundos!",
            "Twinsen dell'isola della Cittadella vince la Coppa dei Due Mondi!",
            "Twinsen da ilha da Cidadela ganha a Taça dos Dois Mundos!",
        },
        new[]
        {
            "No! That wish was mine!",
            "Non ! Ce vœu était à moi !",
            "Nein! Dieser Wunsch gehörte mir!",
            "¡No! ¡Ese deseo era mío!",
            "No! Quel desiderio era mio!",
            "Não! Esse desejo era meu!",
        },
        new[]
        {
            "The Cup is yours, and its wish. What will it be?",
            "La Coupe est à toi, et son vœu aussi. Que souhaites-tu ?",
            "Der Pokal gehört dir, und sein Wunsch auch. Was soll es sein?",
            "La Copa es tuya, y su deseo también. ¿Qué será?",
            "La Coppa è tua, e anche il suo desiderio. Che cosa sarà?",
            "A Taça é tua, e o seu desejo também. O que vai ser?",
        },
        new[]
        {
            "The statue gone, and FunFrock with it.",
            "Que la statue disparaisse, et FunFrock avec.",
            "Die Statue soll verschwinden, und FunFrock mit ihr.",
            "Que desaparezca la estatua, y FunFrock con ella.",
            "Che la statua sparisca, e FunFrock con lei.",
            "Que a estátua desapareça, e o FunFrock com ela.",
        },
        new[]
        {
            "Another race. Right now.",
            "Une autre course. Tout de suite.",
            "Noch ein Rennen. Sofort.",
            "Otra carrera. Ahora mismo.",
            "Un'altra corsa. Subito.",
            "Outra corrida. Já.",
        },
        new[]
        {
            "Granted.",
            "Accordé.",
            "Gewährt.",
            "Concedido.",
            "Concesso.",
            "Concedido.",
        },
        new[]
        {
            "Ha! You'll regret that.",
            "Ha ! Tu vas le regretter.",
            "Ha! Das wirst du bereuen.",
            "¡Ja! Te arrepentirás.",
            "Ah! Te ne pentirai.",
            "Ah! Vais arrepender-te.",
        },
        new[]
        {
            "Ha ha ha ha! Again and again, Twinsen. The statue stays, and so do I!",
            "Ha ha ha ha ! Encore et encore, Twinsen. La statue reste, et moi aussi !",
            "Ha ha ha ha! Immer und immer wieder, Twinsen. Die Statue bleibt, und ich auch!",
            "¡Ja ja ja ja! Una y otra vez, Twinsen. ¡La estatua se queda, y yo también!",
            "Ah ah ah ah! Ancora e ancora, Twinsen. La statua resta, e io pure!",
            "Ah ah ah ah! Outra e outra vez, Twinsen. A estátua fica, e eu também!",
        },
        new[]
        {
            "Home, Twinsen?",
            "On rentre, Twinsen ?",
            "Nach Hause, Twinsen?",
            "¿A casa, Twinsen?",
            "A casa, Twinsen?",
            "Para casa, Twinsen?",
        },
        new[]
        {
            "Only if you let me win sometimes.",
            "Seulement si tu me laisses gagner de temps en temps.",
            "Nur wenn du mich manchmal gewinnen lässt.",
            "Solo si me dejas ganar de vez en cuando.",
            "Solo se ogni tanto mi lasci vincere.",
            "Só se me deixares ganhar de vez em quando.",
        },
        new[]
        {
            "I'm keeping the car. For science.",
            "Je garde la voiture. Pour la science.",
            "Ich behalte den Wagen. Für die Wissenschaft.",
            "Me quedo con el coche. Por la ciencia.",
            "Mi tengo la macchina. Per la scienza.",
            "Eu fico com o carro. Pela ciência.",
        },
        new[]
        {
            "Same time next year? We hear there's a temple, and a very long table...",
            "Même date l'année prochaine ? On nous a parlé d'un temple, et d'une très longue table...",
            "Nächstes Jahr um dieselbe Zeit? Wir hören, es gibt da einen Tempel und einen sehr langen Tisch...",
            "¿El año que viene a la misma hora? Hemos oído que hay un templo, y una mesa muy larga...",
            "Stessa ora l'anno prossimo? Abbiamo sentito che c'è un tempio, e un tavolo molto lungo...",
            "À mesma hora para o ano? Ouvimos dizer que há um templo, e uma mesa muito comprida...",
        },
    };

    public static List<string> ApplyFinal(string gameDirectory, RaceTrackReport report, RaceTrackIsland island, RaceTrackScenes.Result scenes)
    {
        var s = new LateStory(gameDirectory, report, island, scenes.StartScene);
        if (!s.Ready("the final")) return s.Log;
        var first = s.Texts(FinalTexts, FinalLines, "the final (Celebration Island)");
        int Id(FLine line) => first + (int)line;
        var when = $"{FinalChapter} == chapter()";
        // (and at the landing, the scene loaded in chapter 13 before the landing moves it on)
        var here = $"({FinalChapter} == chapter() || (13 == chapter() && 1 == var_game({StatueRisen})))";
        var zoe = Copy(s.Originals, OpeningScene, Zoe);
        var raph = Copy(s.Originals, TraluCave, CaveRaph);
        var paul = Copy(s.Originals, PaulHouse, PaulActor);
        var esmer = Copy(s.Originals, AliensScene, Alien);
        if (zoe is null || raph is null || paul is null || esmer is null) { s.Log.Add("chapter 14 left out: the game's Zoe, Raph, Mr. Paul or Esmer not found"); return s.Log; }
        SceneActorModel Person(int entity, int body, int anim)
        {
            var a = zoe.Clone();
            a.Entity = entity; a.Body = body; a.Anim = anim;
            return a;
        }
        // (his body's generic number is past an actor record's signed byte: put on by his script, body())
        var funfrock = Person(FunFrockEntity, 0, FunFrockStand);
        var baldino = Person(BaldinoEntity, 0, BaldinoStand);

        s.Edit(scenes.StartScene, "chapter 14: the landing, FunFrock, the last call, the crowd, the race's end, the Esmer's wish, the dock after and the end", model =>
        {
            var edits = new List<Action<SceneScripts>>();
            // (on the dock, by the start line: ground near its height -- the statue's hand and book are off the road too, high over it)
            (double X, double Z, double Y, int Beta) At(double back, double aside) => s.Near(model, back, aside, 600) ?? s.ByHero(model, back, aside);
            var lyra = s.MarshalIn(model);
            var crowd = new (SceneActorModel Who, FLine Line, double Back, double Aside)[]
            {
                (zoe, FLine.Zoe, 6, 4), (raph, FLine.Raph, 7, 5.5), (paul, FLine.Paul, 8, 7), (baldino, FLine.Baldino, 6, 7),
            };
            Place(funfrock, model, At(3, 3));
            Place(esmer, model, At(4, 6));
            var flickAt = At(9, 5);
            var flick = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(flickAt.X, model.CubeX), (int)Math.Round(flickAt.Y), RaceTrackScenes.CubeLocal(flickAt.Z, model.CubeY), 9);
            // (their indices, as they are added: FunFrock, the Esmer, Lyra, Zoe, Raph, Mr. Paul, Baldino, Flick, then the controllers)
            int ff = model.Actors.Count, es = ff + 1, ly = ff + 2, zo = ff + 3, ra = ff + 4, ba = ff + 6;
            edits.Add(s.Add(model, funfrock, $@"void comportement_0()
{{
    if ({here})
    {{
        body({FunFrockBody});
        set_comportement(comportement_1);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
    if ({when} && {FinalGranted} != var_game({FinalStep}))
    {{
        invisible(0);
    }}
    else
    {{
        invisible(1);
    }}
    if ({when} && 0 == var_game({FinalStep}) && {Driving} != comportement_hero())
    {{
        set_var_game({FinalStep}, {FinalSpoke});
        if (1 == var_game({DreamCaught}))
        {{
            message({Id(FLine.Caught)});
        }}
        else
        {{
            message({Id(FLine.NotCaught)});
        }}
    }}
    swif (1 == action())
    {{
        if (1500 > distance(0) && {Driving} != comportement_hero() && {when} && {FinalWon} > var_game({FinalStep}))
        {{
            message({Id(FLine.Waiting)});
        }}
    }}
}}
"));
            edits.Add(s.Add(model, esmer, Talker(here, $@"            if ({FinalGranted} == var_game({FinalStep}))
            {{
                message({Id(FLine.EsmerNextYear)});
            }}
            else if ({FinalWon} == var_game({FinalStep}))
            {{
                message({Id(FLine.EsmerAsk)});
            }}")));
            edits.Add(s.Add(model, lyra, Talker(here, $@"            if ({FinalWon} <= var_game({FinalStep}))
            {{
                message({Id(FLine.Won)});
            }}")));
            foreach (var (who, line, back, aside) in crowd)
            {
                Place(who, model, At(back, aside));
                edits.Add(s.Add(model, who, Talker(here, $"            message({Id(line)});")));
            }
            edits.Add(s.Add(model, flick, Talker(here, $"            message({Id(FLine.Flick)});")));
            // the landing, the race's ends and the wish
            edits.Add(s.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 9), $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if (13 == chapter() && 1 == var_game({StatueRisen}) && {Driving} != comportement_hero())
    {{
        message({Id(FLine.Flight)});
        set_var_game({FinalStep}, 0);
        set_var_game({QEmperor}, {StanReady});
        inc_chapter();
    }}
    if ({when} && {FinalWon} != var_game({FinalStep}) && {FinalRaced} == var_game({QEmperor}) && {Driving} != comportement_hero())
    {{
        set_var_game({QEmperor}, {StanReady});
        message_obj({ff}, {Id(FLine.Again)});
        change_cube({CelebrationScene});
    }}
    if ({when} && {FinalWon} == var_game({FinalStep}) && {Driving} != comportement_hero())
    {{
        message_obj({ly}, {Id(FLine.Won)});
        message_obj({ff}, {Id(FLine.FunFrockNo)});
        add_choice({Id(FLine.Gone)});
        add_choice({Id(FLine.AnotherRace)});
        ask_choice_obj({es}, {Id(FLine.EsmerAsk)});
        if ({Id(FLine.AnotherRace)} == choice())
        {{
            if ({FinalAgain} == var_game({QEmperor}))
            {{
                message_obj({ff}, {Id(FLine.Laugh)});
                set_var_game({FinalStep}, {FinalSpoke});
                the_end();
            }}
            else
            {{
                message_obj({ff}, {Id(FLine.Regret)});
                set_var_game({QEmperor}, {FinalAgain});
                set_var_game({FinalStep}, {FinalSpoke});
                change_cube({CelebrationScene});
            }}
        }}
        else
        {{
            message_obj({es}, {Id(FLine.Granted)});
            set_var_game({StatueRisen}, 0);
            set_var_game({FinalStep}, {FinalGranted});
            change_cube({CelebrationScene});
        }}
    }}
    if ({when} && {FinalGranted} == var_game({FinalStep}) && 0 == var_cube({Once1}) && {Driving} != comportement_hero())
    {{
        set_var_cube({Once1}, 1);
        message_obj({zo}, {Id(FLine.ZoeHome)});
        message_obj({ra}, {Id(FLine.RaphWin)});
        message_obj({ba}, {Id(FLine.BaldinoCar)});
        message_obj({es}, {Id(FLine.EsmerNextYear)});
        inc_chapter();
        the_end();
    }}
}}
"));
            edits.Add(s.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 9), LateStory.RadioCall(FinalChapter, Id(FLine.Ring), Id(FLine.Baldino), here)));
            return scripts => { foreach (var e in edits) e(scripts); };
        });
        s.Save();
        s.Log.Add($"chapter 14: the final's steps are variable {FinalStep} (a win {FinalWon}), a race run sets {QEmperor} to {FinalRaced}; the wish granted takes the statue away (variable {StatueRisen} 0)");
        return s.Log;
    }
}
