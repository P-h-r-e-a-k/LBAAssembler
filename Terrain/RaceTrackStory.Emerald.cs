using System.IO;
using System.Text.RegularExpressions;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The race mod's story on the Emerald Moon, chapter 4 (docs/LBRA_WALKTHROUGH.md; the build notes: docs/LBRA_STORY_SCRIPT.md):
//   - Arriving by Baldino's rocket (the Desert's win: scene 229, the track's start), the radio rings and Baldino calls from his lander.
//   - Lyra, the marshal, at her plinth: the base's computer won't switch on the start lights (Q_HAL 1).
//   - In the moon base (scene 23, by Baldino's old cell), HAL: "HAL!! ZX81!!" (the game's own 546 and 545) and Zoe's word; his mechanics
//     there: his voice box is broken, any radio would do -- Twinsen hands over the portable radio (item 26 taken, Q_HAL 3); HAL again,
//     mended: the start lights on, and the radio given back, improved (item 26, Q_HAL 4: the race file's gate=216 4).
//   - Won (win=216 5): out of the car, in any of the track's outside scenes --Baldino, Lyra, HAL's log of a black car launched to Zeelich, Zoe -- and the
//     Esmers' ship to Otringal (inc_chapter: 5; change_cube(88), Otringal's start).
// HAL and his mechanics talk through invisible actors of their own beside the game's (HAL is a sprite over the base's bricks, and the
// game's scripts there -- HAL's sparks, the mechanics' tracks -- are left as they are).
internal static partial class RaceTrackStory
{
    // Q_HAL: 1 Lyra has told, 2 HAL heard, 3 the radio handed over, 4 HAL mended (the start lights on), 5 the race won, 6 told
    public const int QHal = 216, HalHeard = 2, RadioLent = 3, HalMended = 4, HalWon = 5, HalDone = 6;
    // the moon base: HAL's place (scene 23's actor 4, its sprite, at the floor of the mechanics' room) and the mechanics' (actor 14's)
    private const int BaseScene = 23, HalScreen = 4, OtringalStart = 88, HalCry = 546, HalZx = 545;
    // (a scene variable of the base: HAL's talker is there this visit -- the eggs' HalTalks is 70, the base's own scripts use 0-2)
    private const int HalHere = 71;
    private static readonly (int X, int Y, int Z) HalSpot = (23152, 2816, 3904), MechanicSpot = (19344, 2816, 6400);
    private const int HalVoice = 10;
    // the moon's scenes whose scripts put the spacesuit on: the X building's airlock, the outside by the circle and the square entrances,
    // the Esmers' shuttle
    private static readonly int[] MoonSuitScenes = { 53, 75, 77, 115 };

    // A story driver who races only while the story has him at the track (the race file's opponentN_when=<var> <least> <below>, RACEMOD.CPP):
    // HAL, from his mending to the race's end -- in the moon base before and after (ApplyEmerald)
    internal static string? OpponentWhen(RaceTrackIsland island, string driver) =>
        island == RaceTrackIsland.Emerald && driver == "HAL" ? $"{QHal} {HalMended} {HalDone}" : null;

    private enum MoonLine
    {
        Ringing, BaldinoRadio, LyraWelcome, LyraGate, LyraOpen, ZoeNo, MechanicSpare, ZoeRadio, TwinsenMind, ZoeWillMind, MechanicLeave,
        MechanicWorking, HalThanks, HalRadio, BaldinoBeaten, LyraZeelich, HalLog, ZoeLaugh,
        // (the eggs': RaceTrackStory.Eggs.Emerald.cs)
        HalDaisy, HalDiagnostic, BowlerUp, BowlerTuesday,
        // (one HAL at a time, 2026-11-07: a mechanic's word while he is out at the track)
        MechanicHalOut,
    }
    private static readonly string[][] MoonLines =
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
            "Twinsen? Baldino, from the pits. Low gravity up here: I've softened your springs and put weight in the nose, so you won't flip on the loops. And I've put an air filter in your racing helmet: you can breathe out here, no spacesuit needed. Don't touch HAL, he hates me.",
            "Twinsen ? Baldino, depuis les stands. Faible gravité ici : j'ai assoupli tes ressorts et lesté l'avant, pour que tu ne te retournes pas dans les loopings. Et j'ai mis un filtre à air dans ton casque de course : tu peux respirer dehors, pas besoin de scaphandre. Ne touche pas à HAL, il me déteste.",
            "Twinsen? Baldino, aus der Box. Hier oben ist wenig Schwerkraft: Ich habe deine Federn weicher gemacht und Gewicht in die Nase gepackt, damit du dich in den Loopings nicht überschlägst. Und in deinen Rennhelm habe ich einen Luftfilter eingebaut: Du kannst hier draußen atmen, ganz ohne Raumanzug. Fass HAL nicht an, er hasst mich.",
            "¿Twinsen? Baldino, desde boxes. Aquí arriba hay poca gravedad: te he ablandado los muelles y he puesto peso en el morro, para que no vuelques en los rizos. Y le he puesto un filtro de aire a tu casco de carreras: puedes respirar aquí fuera, sin escafandra. No toques a HAL, me odia.",
            "Twinsen? Baldino, dai box. Quassù c'è poca gravità: ho ammorbidito le molle e messo peso sul muso, così non ti ribalti nei giri della morte. E ho messo un filtro dell'aria nel tuo casco da corsa: puoi respirare qui fuori, niente scafandro. Non toccare HAL, mi odia.",
            "Twinsen? Baldino, da boxe. Aqui em cima há pouca gravidade: amaciei-te as molas e pus peso no nariz, para não capotares nos loopings. E pus um filtro de ar no teu capacete de corrida: podes respirar cá fora, sem escafandro. Não toques no HAL, ele odeia-me.",
        },
        new[]
        {
            "Welcome to the moon, racers. I'm Lyra, the Cup's marshal from here to the end. Bad news: the base's computer won't switch on the start lights, and nobody can get it to talk sense.",
            "Bienvenue sur la lune, pilotes. Je suis Lyra, commissaire de la Coupe d'ici à la fin. Mauvaise nouvelle : l'ordinateur de la base refuse d'allumer les feux de départ, et personne n'arrive à le raisonner.",
            "Willkommen auf dem Mond, Rennfahrer. Ich bin Lyra, die Rennleiterin des Pokals von hier bis zum Ende. Schlechte Nachrichten: Der Computer der Basis schaltet die Startampel nicht ein, und niemand bringt ihn zur Vernunft.",
            "Bienvenidos a la luna, pilotos. Soy Lyra, la comisaria de la Copa de aquí al final. Malas noticias: el ordenador de la base no quiere encender el semáforo de salida, y nadie consigue hacerle entrar en razón.",
            "Benvenuti sulla luna, piloti. Sono Lyra, la commissaria della Coppa da qui alla fine. Brutte notizie: il computer della base non vuole accendere il semaforo di partenza, e nessuno riesce a farlo ragionare.",
            "Bem-vindos à lua, pilotos. Sou a Lyra, a comissária da Taça daqui até ao fim. Más notícias: o computador da base não quer acender as luzes da partida, e ninguém o consegue pôr a falar com juízo.",
        },
        new[]
        {
            "The start lights are still dark. HAL is in the base, by Baldino's old cell. Good luck getting a word out of him.",
            "Les feux de départ sont toujours éteints. HAL est dans la base, près de l'ancienne cellule de Baldino. Bonne chance pour en tirer un mot.",
            "Die Startampel ist immer noch aus. HAL steht in der Basis, bei Baldinos alter Zelle. Viel Glück dabei, ein Wort aus ihm herauszubekommen.",
            "El semáforo de salida sigue apagado. HAL está en la base, junto a la antigua celda de Baldino. Suerte sacándole una palabra.",
            "Il semaforo di partenza è ancora spento. HAL è nella base, vicino alla vecchia cella di Baldino. Buona fortuna a cavargli una parola.",
            "As luzes da partida continuam apagadas. O HAL está na base, junto à antiga cela do Baldino. Boa sorte a arrancar-lhe uma palavra.",
        },
        new[]
        {
            "The start lights are on! HAL must be feeling better. Line up whenever you're ready.",
            "Les feux de départ sont allumés ! HAL doit aller mieux. Mets-toi en place quand tu veux.",
            "Die Startampel ist an! HAL muss es besser gehen. Stell dich auf, wann immer du bereit bist.",
            "¡El semáforo de salida está encendido! HAL debe de encontrarse mejor. Colócate cuando quieras.",
            "Il semaforo di partenza è acceso! HAL deve stare meglio. Mettiti in griglia quando vuoi.",
            "As luzes da partida estão acesas! O HAL deve estar melhor. Alinha quando quiseres.",
        },
        new[]
        {
            "I think that means no.",
            "Je crois que ça veut dire non.",
            "Ich glaube, das heißt nein.",
            "Creo que eso significa que no.",
            "Credo che voglia dire no.",
            "Acho que isso quer dizer que não.",
        },
        new[]
        {
            "It shouted at us all night. Its voice box is broken. There's a spare in... no, wait, we used it. Any radio would do.",
            "Il nous a crié dessus toute la nuit. Son boîtier vocal est cassé. Il y en a un de rechange dans... non, attends, on l'a utilisé. N'importe quelle radio ferait l'affaire.",
            "Er hat uns die ganze Nacht angeschrien. Sein Sprachmodul ist kaputt. Es gibt ein Ersatzteil im... nein, warte, das haben wir verbraucht. Jedes Funkgerät würde gehen.",
            "Nos ha estado gritando toda la noche. Tiene la caja de voz rota. Hay una de repuesto en... no, espera, la usamos. Cualquier radio serviría.",
            "Ci ha urlato contro tutta la notte. Ha il modulo vocale rotto. Ce n'è uno di ricambio nel... no, aspetta, l'abbiamo usato. Andrebbe bene qualsiasi radio.",
            "Esteve a gritar connosco a noite toda. Tem a caixa de voz avariada. Há uma de reserva no... não, espera, usámo-la. Qualquer rádio servia.",
        },
        new[]
        {
            "Twinsen... your radio.",
            "Twinsen... ta radio.",
            "Twinsen... dein Funkgerät.",
            "Twinsen... tu radio.",
            "Twinsen... la tua radio.",
            "Twinsen... o teu rádio.",
        },
        new[]
        {
            "Baldino won't mind.",
            "Baldino ne m'en voudra pas.",
            "Baldino wird nichts dagegen haben.",
            "A Baldino no le importará.",
            "A Baldino non dispiacerà.",
            "O Baldino não se vai importar.",
        },
        new[]
        {
            "Baldino will mind.",
            "Baldino t'en voudra.",
            "Baldino wird etwas dagegen haben.",
            "A Baldino sí le importará.",
            "A Baldino dispiacerà eccome.",
            "O Baldino vai importar-se.",
        },
        new[]
        {
            "Leave it to us.",
            "Laisse-nous faire.",
            "Überlass das uns.",
            "Déjalo de nuestra cuenta.",
            "Lascia fare a noi.",
            "Deixa connosco.",
        },
        new[]
        {
            "Nearly done. Go and say hello to him, he's dying to talk.",
            "C'est presque fini. Va lui dire bonjour, il meurt d'envie de parler.",
            "Fast fertig. Geh und sag ihm Hallo, er brennt darauf, zu reden.",
            "Casi está. Ve a saludarle, se muere por hablar.",
            "Quasi fatto. Vai a salutarlo, non vede l'ora di parlare.",
            "Está quase. Vai cumprimentá-lo, está mortinho por falar.",
        },
        new[]
        {
            "Thank you, Twinsen. The start lights are on. I have calculated your chances against Baldino: they are poor. I will race too, to see if I am right.",
            "Merci, Twinsen. Les feux de départ sont allumés. J'ai calculé tes chances contre Baldino : elles sont faibles. Je cours aussi, pour voir si j'ai raison.",
            "Danke, Twinsen. Die Startampel ist an. Ich habe deine Chancen gegen Baldino berechnet: Sie sind gering. Ich fahre auch mit, um zu sehen, ob ich recht habe.",
            "Gracias, Twinsen. El semáforo de salida está encendido. He calculado tus posibilidades contra Baldino: son escasas. Yo también correré, para ver si tengo razón.",
            "Grazie, Twinsen. Il semaforo di partenza è acceso. Ho calcolato le tue probabilità contro Baldino: sono scarse. Correrò anch'io, per vedere se ho ragione.",
            "Obrigado, Twinsen. As luzes da partida estão acesas. Calculei as tuas hipóteses contra o Baldino: são fracas. Também vou correr, para ver se tenho razão.",
        },
        new[]
        {
            "Your radio is returned. I have improved it: it now receives the Esmers' channel.",
            "Je te rends ta radio. Je l'ai améliorée : elle capte maintenant la fréquence des Esmers.",
            "Hier ist dein Funkgerät zurück. Ich habe es verbessert: Es empfängt jetzt den Kanal der Esmer.",
            "Te devuelvo tu radio. La he mejorado: ahora recibe el canal de los Esmers.",
            "Ti restituisco la radio. L'ho migliorata: ora riceve il canale degli Esmer.",
            "Devolvo-te o rádio. Melhorei-o: agora apanha o canal dos Esmers.",
        },
        new[]
        {
            "Beaten in my own lander! I'll have to tune the other one.",
            "Battu dans mon propre atterrisseur ! Il va falloir que je règle l'autre.",
            "Geschlagen in meiner eigenen Landefähre! Ich werde die andere tunen müssen.",
            "¡Vencido en mi propio módulo! Tendré que ajustar el otro.",
            "Battuto nel mio stesso modulo! Dovrò mettere a punto l'altro.",
            "Vencido no meu próprio módulo! Vou ter de afinar o outro.",
        },
        new[]
        {
            "Two worlds, two races... and now Zeelich. The Esmers' ship leaves for Otringal.",
            "Deux mondes, deux courses... et maintenant Zeelich. Le vaisseau des Esmers part pour Otringal.",
            "Zwei Welten, zwei Rennen... und jetzt Zeelich. Das Schiff der Esmer fliegt nach Otringal.",
            "Dos mundos, dos carreras... y ahora Zeelich. La nave de los Esmers sale hacia Otringal.",
            "Due mondi, due gare... e ora Zeelich. La nave degli Esmer parte per Otringal.",
            "Dois mundos, duas corridas... e agora Zeelich. A nave dos Esmers parte para Otringal.",
        },
        new[]
        {
            "My log shows a launch from this moon last night: one black car, one passenger, to Zeelich. The passenger's name was not given. His laugh was on file.",
            "Mon journal indique un lancement depuis cette lune la nuit dernière : une voiture noire, un passager, pour Zeelich. Le nom du passager n'a pas été donné. Son rire était dans mes fichiers.",
            "Mein Protokoll zeigt einen Start von diesem Mond letzte Nacht: ein schwarzes Auto, ein Passagier, nach Zeelich. Der Name des Passagiers wurde nicht genannt. Sein Lachen war gespeichert.",
            "Mi registro muestra un lanzamiento desde esta luna anoche: un coche negro, un pasajero, rumbo a Zeelich. No se dio el nombre del pasajero. Su risa estaba en el archivo.",
            "Il mio registro mostra un lancio da questa luna la notte scorsa: una macchina nera, un passeggero, per Zeelich. Il nome del passeggero non è stato dato. La sua risata era in archivio.",
            "O meu registo mostra um lançamento desta lua ontem à noite: um carro preto, um passageiro, para Zeelich. O nome do passageiro não foi dado. A gargalhada dele estava em arquivo.",
        },
        new[]
        {
            "Who leaves a laugh on file?",
            "Qui laisse son rire dans un fichier ?",
            "Wer hinterlässt denn sein Lachen in einer Datei?",
            "¿Quién deja su risa en un archivo?",
            "Chi lascia la propria risata in archivio?",
            "Quem é que deixa a gargalhada em arquivo?",
        },
        // (HAL's song: Daisy Bell, as 2001 has him sing it -- and as its dubs did: Au clair de la lune, Hänschen klein, Giro giro tondo)
        new[]
        {
            "Daisy... Daisy... give me your answer do... I'm... half... crazy...",
            "Au clair... de la lune... mon ami... Pierrot... prête-moi... ta plume...",
            "Hänschen klein... ging allein... in die weite... Welt... hinein...",
            "Daisy... Daisy... dame tu respuesta... estoy... medio... loco...",
            "Giro giro... tondo... casca... il mondo... casca... la terra...",
            "Daisy... Daisy... dá-me a tua resposta... estou... meio... louco...",
        },
        new[]
        {
            "Excuse me. A diagnostic.",
            "Excuse-moi. Un diagnostic.",
            "Entschuldige. Eine Diagnose.",
            "Perdona. Un diagnóstico.",
            "Scusami. Una diagnostica.",
            "Desculpa. Um diagnóstico.",
        },
        new[]
        {
            "In this gravity my boules never come down. I threw one on Tuesday.",
            "Avec cette gravité, mes boules ne retombent jamais. J'en ai lancé une mardi.",
            "Bei dieser Schwerkraft kommen meine Kugeln nie wieder runter. Eine habe ich am Dienstag geworfen.",
            "Con esta gravedad mis bolas nunca bajan. Lancé una el martes.",
            "Con questa gravità le mie bocce non scendono mai. Ne ho lanciata una martedì.",
            "Com esta gravidade as minhas bolas nunca caem. Lancei uma na terça-feira.",
        },
        new[]
        {
            "Tuesday's.",
            "Celle de mardi.",
            "Die vom Dienstag.",
            "La del martes.",
            "Quella di martedì.",
            "A de terça-feira.",
        },
        new[]
        {
            "HAL's loaded himself into a racing car and gone out to the track. That cabinet's just a box till he's back.",
            "HAL s'est chargé dans une voiture de course et il est parti sur le circuit. Cette armoire n'est qu'une boîte jusqu'à son retour.",
            "HAL hat sich in einen Rennwagen geladen und ist zur Strecke gefahren. Der Schrank da ist nur eine Kiste, bis er zurück ist.",
            "HAL se ha cargado en un coche de carreras y se ha ido al circuito. Ese armario no es más que una caja hasta que vuelva.",
            "HAL si è caricato in un'auto da corsa ed è andato in pista. Quell'armadio è solo una scatola finché non torna.",
            "O HAL carregou-se num carro de corrida e foi para a pista. Aquele armário é só uma caixa até ele voltar.",
        },
    };

    private static List<string> ApplyEmerald(string gameDirectory, RaceTrackReport report, RaceTrackScenes.Result scenes)
    {
        var log = new List<string>();
        if (report.StartLine.Count == 0 || scenes.StartScene < 0) { log.Add("the Emerald Moon: no story, its track has no start line"); return log; }
        var first = AddIslandTexts(gameDirectory, RaceTrackIsland.Emerald.IslandByte, MoonLines, log, "the Emerald Moon");
        if (first < 0) return log;
        int Say(MoonLine line) => first + (int)line;
        var b = new IslandBeatsA(gameDirectory, RaceTrackIsland.Emerald, log);
        const string moon = "4 == chapter()";

        // the radio, on the track's outside scenes (the copies of the moon's: 229 on)
        foreach (var scene in b.OutsideScenes.ToList())
            b.Edit(scene, "the radio rings in chapter 4: Baldino's call from his lander", model => IslandBeatsA.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 4),
                RadioController(4, Say(MoonLine.Ringing), new[] { Say(MoonLine.BaldinoRadio) })));

        // Lyra at her plinth: the start lights dark (Q_HAL 1), still dark, on
        MarshalA(b, report, moon, $@"            if (0 == var_game({QHal}))
            {{
                message({Say(MoonLine.LyraWelcome)});
                set_var_game({QHal}, 1);
            }}
            else if ({HalMended} > var_game({QHal}))
            {{
                message({Say(MoonLine.LyraGate)});
            }}
            else if ({HalWon} > var_game({QHal}))
            {{
                message({Say(MoonLine.LyraOpen)});
            }}", "Lyra");

        // the moon base's doors out: to the track's copies of the moon's outside scenes (the game's own, 74-77, have no track), so that
        // Twinsen walks back out of the base onto the track
        var copies = RaceTrackIsland.Emerald.AddedScenes.ToDictionary(c => c.From, c => c.To);
        for (var s = 0; s < b.Store.SceneCount; s++)
        {
            if (!b.Store.SceneExists(s) || copies.ContainsKey(s) || copies.ContainsValue(s)) continue;
            SceneModel m;
            try { m = b.Store.Load(s); } catch (Exception e) when (e is InvalidDataException or System.IO.IOException) { continue; }
            if (m.Island != RaceTrackIsland.Emerald.IslandByte || m.CubeMode == 1 || !m.Zones.Any(z => z.Type == 0 && copies.ContainsKey(z.Num))) continue;
            b.Edit(s, "its doors out onto the moon lead to the track's copies of the moon's outside scenes", model =>
            {
                foreach (var z in model.Zones.Where(z => z.Type == 0 && copies.ContainsKey(z.Num))) z.Num = copies[z.Num];
                return _ => { };
            });
        }

        // No spacesuit (F15 of the Nov 7 notes, the user: outside on the Emerald Moon "Twinsen wears his racing suit and helmet instead of
        // the spacesuit"; "going back out through the airlock must not force Twinsen into the spacesuit again" -- the suit can't climb the
        // track's ladders): the moon's scripts that put it on (comportement_hero(): the airlocks', the shuttle's, the outside's -- 7 and 10
        // the suit's normal behaviours, 9 and 11 its sporty ones) put on his own normal or sporty one instead. Outside, the race file's
        // foot_body= has his helmet on (Citadel/CitadelOveralls; Baldino's call tells of its air filter), and no_suit= turns any suit's
        // behaviour that still comes his way into his own (RACEMOD.CPP)
        foreach (var scene in MoonSuitScenes.Where(b.Store.SceneExists))
            b.Edit(scene, "no spacesuit: the scripts' suit behaviours his own, normal or sporty", _ => scripts =>
            {
                for (var a = 0; a < scripts.ActorCount; a++)
                {
                    string life;
                    try { life = scripts.GetText(a, ScriptKind.Life); } catch (InvalidOperationException) { continue; }
                    var now = Regex.Replace(life, @"comportement_hero\((7|10)\);", "comportement_hero(0);");
                    now = Regex.Replace(now, @"comportement_hero\((9|11)\);", "comportement_hero(1);");
                    if (now != life) scripts.SetText(a, ScriptKind.Life, now);
                }
            });

        // the moon base: HAL and his mechanics. One HAL at a time (the user, 2026-11-06: "HAL waits as a racer in the pit lane, and HAL is
        // also inside the moon base ... Only one should exist at a time"): in the base until he is mended, then out at the track racing
        // -- from the next visit his talker gone from the base and his screen (the game's actor 4: the glow of his coils and his hum) off,
        // a mechanic saying he has loaded himself into a racing car (his cabinet is the room's bricks: it stays, a box without him), and
        // his car in the pit lane from then on (the race file's opponent2_when=, OpponentWhen) -- and back in the base once the race is
        // over. Mended, he talks on for the rest of that visit (his song, RaceTrackStory.Eggs.Emerald.cs; var_cube HalHere marks it).
        if (b.Store.SceneExists(BaseScene))
        {
            b.Edit(BaseScene, $"HAL's screen (actor {HalScreen}) out of the base while he races (Q_HAL {HalMended}-{HalWon})", _ => scripts =>
            {
                var life = scripts.GetText(HalScreen, ScriptKind.Life);
                scripts.SetText(HalScreen, ScriptKind.Life, Loose(life, "void comportement_0()\n{", $@"void comportement_0()
{{
    if ({HalMended} <= var_game({QHal}) && {HalDone} > var_game({QHal}))
    {{
        suicide();
    }}
    else"));
            });
            b.Edit(BaseScene, "HAL's voice box: HAL's cries (Q_HAL 2), the mechanics take the radio (3), HAL mended gives it back (4)", model =>
            {
                // (HAL speaks in colour 10, the palette's cyan: the base's palette has no ramp in row 15 -- it ends blue, green, cyan, red,
                // magenta -- so a line in 15 typed out in a rainbow, the user, 2026-11-07)
                var hal = IslandBeatsA.Add(model, RaceTrackScenes.InvisibleHelper(HalSpot.X, HalSpot.Y, HalSpot.Z, HalVoice), Talker($"{moon} && {HalMended} > var_game({QHal})", $@"            if ({RadioLent} == var_game({QHal}))
            {{
                message({Say(MoonLine.HalThanks)});
                message({Say(MoonLine.HalRadio)});
                found_object({Radio});
                set_var_game({Radio}, 1);
                set_var_game({QHal}, {HalMended});
            }}
            else if ({RadioLent} > var_game({QHal}))
            {{
                message({HalCry});
                message({HalZx});
                message({HalCry});
                message_zoe({Say(MoonLine.ZoeNo)});
                set_var_game({QHal}, {HalHeard});
            }}
            else if ({HalWon} == var_game({QHal}))
            {{
                message({Say(MoonLine.HalLog)});
            }}
            else
            {{
{HalDaisy(Say(MoonLine.HalThanks), Say(MoonLine.HalDaisy), Say(MoonLine.HalDiagnostic))}
            }}", 2500, $"    set_var_cube({HalHere}, 1);\n"));
                var mechanics = IslandBeatsA.Add(model, RaceTrackScenes.InvisibleHelper(MechanicSpot.X, MechanicSpot.Y, MechanicSpot.Z, 4), Talker(moon, $@"            if ({RadioLent} > var_game({QHal}) && 0 < var_game({Radio}))
            {{
                message({Say(MoonLine.MechanicSpare)});
                message_zoe({Say(MoonLine.ZoeRadio)});
                message_obj(0, {Say(MoonLine.TwinsenMind)});
                message_zoe({Say(MoonLine.ZoeWillMind)});
                set_var_game({Radio}, 0);
                message({Say(MoonLine.MechanicLeave)});
                set_var_game({QHal}, {RadioLent});
            }}
            else if ({RadioLent} > var_game({QHal}))
            {{
                message({Say(MoonLine.MechanicSpare)});
            }}
            else if ({HalMended} <= var_game({QHal}) && 0 == var_cube({HalHere}))
            {{
                message({Say(MoonLine.MechanicHalOut)});
            }}
            else
            {{
                message({Say(MoonLine.MechanicWorking)});
            }}", 2500));
                return scripts => { hal(scripts); mechanics(scripts); };
            });
        }
        else log.Add($"HAL and the mechanics left out: no scene {BaseScene}");

        // won: out of the car by the start line -- Baldino, Lyra, HAL's log, Zoe -- and the Esmers' ship to Otringal
        foreach (var after in b.OutsideScenes.ToList())
        b.Edit(after, "after a win: Baldino, Lyra, HAL's log of the black car, Zoe, and the Esmers' ship to Otringal (inc_chapter, scene 88)", model =>
        {
            var spot = AfterRaceSpot(report);
            if ((int)Math.Floor(spot.X / 64) != model.CubeX || (int)Math.Floor(spot.Z / 64) != model.CubeY) spot = (model.CubeX * 64 + 32, model.CubeY * 64 + 32, report.StartLine[0].Y, 0);
            var baldino = model.Actors.Count; var hal = baldino + 1;
            var edits = new List<Action<SceneScripts>>
            {
                IslandBeatsA.Add(model, Voice(model, spot, 4), "void comportement_0()\n{\n}\n"),
                // (HAL in his base's voice, HalVoice: 15 is the moon palette's row of odd colours -- his log typed out in a rainbow outside
                // after the win, the user, 2026-11-09, as it was in the base till ede56f9)
                IslandBeatsA.Add(model, Voice(model, spot, HalVoice), "void comportement_0()\n{\n}\n"),
                IslandBeatsA.Add(model, Voice(model, spot, 9), $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if ({HalWon} == var_game({QHal}) && {Driving} != comportement_hero())
    {{
        set_var_game({QHal}, {HalDone});
        message_obj({baldino}, {Say(MoonLine.BaldinoBeaten)});
        message({Say(MoonLine.LyraZeelich)});
        message_obj({hal}, {Say(MoonLine.HalLog)});
        message_zoe({Say(MoonLine.ZoeLaugh)});
        if (5 > chapter())
        {{
            inc_chapter();
        }}
        change_cube({OtringalStart});
    }}
}}
"),
            };
            return scripts => { foreach (var e in edits) e(scripts); };
        });

        // the eggs: petanque in low gravity (RaceTrackStory.Eggs.Emerald.cs)
        MoonPetanque(b, report, Say(MoonLine.BowlerUp), Say(MoonLine.BowlerTuesday));

        b.Save();
        log.Add($"the Emerald Moon's race file: gate={QHal} {HalMended} (HAL mended), win={QHal} {HalWon}");
        return log;
    }
}
