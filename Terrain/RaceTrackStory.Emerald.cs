using System.IO;
using System.Text.RegularExpressions;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.LbaScript;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The race mod's story on the Emerald Moon, chapter 4 (docs/LBRA_WALKTHROUGH.md; the build notes: docs/LBRA_STORY_SCRIPT.md):
//   - Arriving at the track's start (scene 229). (No call from Baldino any more, nor Baldino on the moon at all -- the user, 2026-11-16:
//     "Baldino is no longer going to be on Emerald Moon, let's make it just Twinsen and HAL, and a random guard in the moon spacesuit".
//     The radio is HAL's, RACEMOD.CPP HalCall, once he's mended.)
//   - Lyra, the marshal, at her plinth: the base's computer won't switch on the start lights (Q_HAL 1).
//   - In the moon base (scene 23, by Baldino's old cell), HAL: "HAL!! ZX81!!" (the game's own 546 and 545) and Twinsen's word; his
//     mechanics there: his voice box is broken, any radio would do -- Twinsen hands over the portable radio (item 26 taken, Q_HAL 3); HAL
//     again, mended: the start lights on, and the radio given back, improved (item 26, Q_HAL 4: the race file's gate=216 4).
//   - Won (win=216 5): out of the car, in any of the track's outside scenes -- the moon guard he beat, Lyra, HAL's log of a black car
//     launched to Zeelich, Twinsen -- and the Esmers' ship to Otringal (inc_chapter: 5; change_cube(88), Otringal's start).
// (Zoe isn't on the moon: her lines there -- MESSAGE_ZOE, her colour at Twinsen -- read as nobody's, the user, 2026-11-16: "I'm not sure
// who's talking when inside the Emerald moon next to HAL, especially delivering the lines "Baldino won't mind" and "Baldino will mind"";
// Twinsen and the mechanics say them now, each by his own mouth.)
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
    // (who answers Action in HAL's room: the one Twinsen faces, near -- the hero's own script sets var_cube Faced, distance_message; the
    // talkers and the scientists answer only to it. HAL's talker answered for anyone within 2,500 of him, and the scientists by his console
    // all stand within that: whoever Twinsen went to, he heard HAL's cry and his own "I think that means no" -- the user, 2026-11-21: "for
    // the first 30 seconds or so that Twinsen tries to talk to them they all say the same thing 'I think that means no' - we need to give
    // them all informative story related lines". The head scientist is the one at HAL's console, actor 12: the game's own, actor 14, is
    // gone after chapter 3, and the mechanics' talker spoke from his empty place)
    private const int Faced = 73, FacedHal = 1, HeadScientist = 12, ScientistWalker = 13, ScientistBench = 15;
    private const int NearScientist = 1200, NearHead = 1300, NearHal = 2500;
    // (HAL mended while Twinsen waits after lending the radio -- the user, 2026-11-17: "After Twinsen talks to the scientist near HAL and
    // hands over his radio, let's have Twinsen held in a cut scene until HAL is fixed, at which point have a scientist return to him and hand
    // the radio back, along with telling him to talk to HAL": the mechanics' cut scene's frames, counted in a cell variable)
    private const int MendWait = 72, MendFrames = 120;
    // (the race won: out of his car, held, Twinsen sees HAL's car come up the road and stop beside him -- RACEMOD.CPP win_pull=, HAL the
    // first opponent -- and the words after the win wait for it (game variable HalPullVar 1); the user, 2026-11-17: "After the race HAL
    // currently talks to us off screen, let's have Twinsen held in a cut scene and have HAL pull up next to him a couple of seconds after
    // Twinsen wins the race, and then HAL talks to him from his car")
    public const int HalPullVar = 264, HalPullDelay = 1500;
    private const int HalVoice = 10;
    // the moon's scenes whose scripts put the spacesuit on: the X building's airlock, the outside by the circle and the square entrances,
    // the Esmers' shuttle
    private static readonly int[] MoonSuitScenes = { 53, 75, 77, 115 };
    // The moon's airlocks and each one's hook (the actor the game hung the empty space suit on -- entity 83, no_body() while Twinsen wears
    // it, body(0) once it's hung): the base's (31), the circle entrance's (52), the X building's (53). The hook shows his helmet on its
    // stand now (CitadelProps.HangingHelmet), and the race file's helmet_hook= keeps the helmet on him indoors there till it's hung --
    // he walks in in it, the airlock cycles, he comes out without it, and takes it again on the way out (the user, 2026-11-16: "When
    // Twinsen enters the emerald moon, let's have him enter wearing his helmet, go into the airlock, and emerge not wearing his helmet,
    // this mirrors behaviour in the retail game"; "We should show Twinsen's helmet left in the airlocks instead of the spacesuit")
    // (and the triangle's and the square's, 167 and 168 -- the user, 2026-11-17: "we need to make sure the helmet works correctly for the 4
    // airlocks in the main building also. The square, triangle, circle, and X entrances")
    private static readonly (int Scene, int Actor)[] MoonAirlocks = { (31, 12), (52, 9), (53, 6), (167, 9), (168, 9) };
    private const int EmptySuitEntity = 83;
    internal static string MoonHelmetHooks() => string.Concat(MoonAirlocks.Select(a => $"helmet_hook={a.Scene} {a.Actor}\n"));

    // A story driver who races only while the story has him at the track (the race file's opponentN_when=<var> <least> <below>, RACEMOD.CPP):
    // HAL, from his mending to the race's end -- in the moon base before and after (ApplyEmerald)
    // (and Luc on Citadel Island's town circuit: his barrel in Twinsen's carport till Zoe has fitted Bob's tyre, not in the pit lane --
    // the user, 2026-11-17, "This stops him being in two places at once": RaceTrackStory.Chapters, Yard)
    internal static string? OpponentWhen(RaceTrackIsland island, string driver) =>
        island == RaceTrackIsland.Emerald && driver == "HAL" ? $"{QHal} {HalMended} {HalDone}"
        : island == RaceTrackIsland.Citadel && driver == "Luc" ? $"{QLucCar} {WheelFitted}"
        : null;

    private enum MoonLine
    {
        // (Ringing and BaldinoRadio: Baldino's call from his lander, gone with him -- kept, unsaid, so the lines after keep their numbers)
        Ringing, BaldinoRadio, LyraWelcome, LyraGate, LyraOpen, TwinsenNo, MechanicSpare, TwinsenOffer, MechanicThanks, TwinsenBack, MechanicLeave,
        MechanicWorking, HalThanks, HalRadio, GuardBeaten, LyraZeelich, HalLog, TwinsenLaugh,
        // (the eggs': RaceTrackStory.Eggs.Emerald.cs)
        HalDaisy, HalDiagnostic, BowlerUp, BowlerTuesday,
        // (one HAL at a time, 2026-11-07: a mechanic's word while he is out at the track)
        MechanicHalOut,
        // (Zoe as companion's hints -- RaceTrackStory.Companion.cs, 2026-11-11)
        ZoeHintHal, ZoeHintRace,
        // (Stella Entities' people -- RaceTrackStory.StellaEntities.cs, 2026-11-11)
        ClubWelcome, ClubStaffOnly, ClubNoBoss, ClubBarman,
        // (the lounge -- RaceTrackStory.StellaLounge.cs, 2026-11-13)
        ClubOffer, ClubShowYes, ClubShowNo, ClubFollow, ClubBroke, ClubLater, ClubThanks, ClubMachineAsk, ClubMachineYes, ClubMachineNo, ClubMachineDone,
        // (the Grobo hostess's lap dance -- StellaStaff, 2026-11-14)
        ClubLapOffer, ClubLapBroke,
        // (the moon's hangar, the freighter on to Otringal -- RaceTrackStory.Hangar.cs, MoonHangar, 2026-11-14)
        HangarOffer, HangarYes, HangarNo, HangarBoard, HangarLater, HangarLift, HangarClosed, HangarGo,
        // (HAL beaten: his number on Twinsen's radio -- the user, 2026-11-16)
        HalNumber,
        // (the performers in the dressing room behind the catwalk -- RaceTrackStory.StellaEntities, DressingPeople, 2026-11-17)
        DressMirror, DressRail, DressScreen,
        // (HAL mended while Twinsen waits, his radio brought back -- the user, 2026-11-17)
        MechanicMended,
        // (the private rooms' bouncer, to Twinsen without a dancer -- RaceTrackStory.StellaLounge, 2026-11-17)
        ClubPrivateOnly,
        // (HAL's scientists, each his own word on HAL as the story goes -- the user, 2026-11-21; Faced)
        WalkerBroken, BenchBroken, BenchRadioSeen, WalkerMended, BenchMended, WalkerOut, BenchOut, WalkerBeaten, BenchBeaten, TwinsenAskScientists,
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
            "Would my radio do? Here, take it.",
            "Ma radio ferait l'affaire ? Tenez, prenez-la.",
            "Würde mein Funkgerät gehen? Hier, nehmt es.",
            "¿Serviría mi radio? Tomad, cogedla.",
            "Andrebbe bene la mia radio? Ecco, prendetela.",
            "O meu rádio serve? Tomem, fiquem com ele.",
        },
        new[]
        {
            "A portable! Perfect. He'll have his voice back in no time.",
            "Une portative ! Parfait. Il aura retrouvé sa voix en un rien de temps.",
            "Ein tragbares! Perfekt. Er hat seine Stimme im Nu wieder.",
            "¡Una portátil! Perfecto. Recuperará la voz en un momento.",
            "Una portatile! Perfetto. Riavrà la voce in un attimo.",
            "Um portátil! Perfeito. Vai recuperar a voz num instante.",
        },
        new[]
        {
            "I'll want it back before the race.",
            "Je la veux de retour avant la course.",
            "Ich will es vor dem Rennen zurück.",
            "La quiero de vuelta antes de la carrera.",
            "La rivoglio prima della corsa.",
            "Quero-o de volta antes da corrida.",
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
            "Thank you, Twinsen. The start lights are on. I have calculated your chances against me: they are poor. I will race you, to see if I am right.",
            "Merci, Twinsen. Les feux de départ sont allumés. J'ai calculé tes chances contre moi : elles sont faibles. Je cours contre toi, pour voir si j'ai raison.",
            "Danke, Twinsen. Die Startampel ist an. Ich habe deine Chancen gegen mich berechnet: Sie sind gering. Ich fahre gegen dich, um zu sehen, ob ich recht habe.",
            "Gracias, Twinsen. El semáforo de salida está encendido. He calculado tus posibilidades contra mí: son escasas. Correré contra ti, para ver si tengo razón.",
            "Grazie, Twinsen. Il semaforo di partenza è acceso. Ho calcolato le tue probabilità contro di me: sono scarse. Correrò contro di te, per vedere se ho ragione.",
            "Obrigado, Twinsen. As luzes da partida estão acesas. Calculei as tuas hipóteses contra mim: são fracas. Vou correr contra ti, para ver se tenho razão.",
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
            "Beaten by a fellow without a spacesuit! The base will never let me hear the end of it.",
            "Battu par un type sans scaphandre ! À la base, on ne va pas me laisser l'oublier.",
            "Geschlagen von einem Kerl ohne Raumanzug! Das wird man mir in der Basis ewig vorhalten.",
            "¡Vencido por un tipo sin escafandra! En la base no me dejarán olvidarlo nunca.",
            "Battuto da uno senza tuta spaziale! Alla base non me lo faranno dimenticare mai.",
            "Vencido por um tipo sem escafandro! Na base nunca mais me deixam esquecer isto.",
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
        new[]
        {
            "The base's computer, HAL, has locked the start gate. Let's have a look inside the base.",
            "L'ordinateur de la base, HAL, a verrouillé la grille de départ. Allons voir à l'intérieur de la base.",
            "Der Computer der Basis, HAL, hat das Starttor verriegelt. Schauen wir uns in der Basis um.",
            "El ordenador de la base, HAL, ha bloqueado la puerta de salida. Echemos un vistazo dentro de la base.",
            "Il computer della base, HAL, ha bloccato il cancello di partenza. Diamo un'occhiata dentro la base.",
            "O computador da base, o HAL, trancou o portão de partida. Vamos ver dentro da base.",
        },
        new[]
        {
            "The start lights are on. Let's race!",
            "Les feux de départ sont allumés. En course !",
            "Die Startampel ist an. Auf zum Rennen!",
            "Las luces de salida están encendidas. ¡A correr!",
            "Le luci di partenza sono accese. Si corre!",
            "As luzes de partida estão acesas. Vamos correr!",
        },
        new[]
        {
            "Welcome to Stella Entities. No charge, HAL's added your name to the list for one night only. Look all you like -- no touching the dancers.",
            "Bienvenue au Stella Entities. C'est gratuit, HAL a mis ton nom sur la liste pour une nuit seulement. Regarde tant que tu veux -- on ne touche pas les danseuses.",
            "Willkommen im Stella Entities. Kostet nichts, HAL hat deinen Namen für eine Nacht auf die Liste gesetzt. Schau, so viel du willst -- Anfassen verboten.",
            "Bienvenido a Stella Entities. Gratis, HAL ha puesto tu nombre en la lista solo por una noche. Mira todo lo que quieras -- prohibido tocar a las bailarinas.",
            "Benvenuto allo Stella Entities. Gratis, HAL ha messo il tuo nome in lista solo per una notte. Guarda quanto vuoi -- vietato toccare le ballerine.",
            "Bem-vindo ao Stella Entities. É grátis, o HAL pôs o teu nome na lista só por uma noite. Olha à vontade -- não se tocam as bailarinas.",
        },
        new[]
        {
            "Staff only past here. The dancers get changed back there.",
            "Réservé au personnel. Les danseuses se changent là-derrière.",
            "Nur für Personal. Dahinten ziehen sich die Tänzerinnen um.",
            "Solo personal a partir de aquí. Las bailarinas se cambian ahí detrás.",
            "Solo personale da qui in poi. Le ballerine si cambiano là dietro.",
            "Só pessoal daqui para a frente. As bailarinas mudam de roupa lá atrás.",
        },
        new[]
        {
            "No touch the Boss!",
            "Pas toucher le Patron !",
            "Nicht Boss anfassen!",
            "¡No tocar al Jefe!",
            "Non toccare il Capo!",
            "Não tocar no Chefe!",
        },
        new[]
        {
            "What'll it be? The Moon Fizz is on the house tonight.",
            "Qu'est-ce que je te sers ? Le Moon Fizz est offert ce soir.",
            "Was darf's sein? Der Moon Fizz geht heute aufs Haus.",
            "¿Qué te pongo? El Moon Fizz invita la casa esta noche.",
            "Cosa ti servo? Il Moon Fizz stasera lo offre la casa.",
            "O que vai ser? O Moon Fizz é oferta da casa esta noite.",
        },
        new[]
        {
            "Hey there, handsome. Fancy a private show in the back? Just you and me -- 250 zlitos.",
            "Salut, beau gosse. Un spectacle privé au fond, ça te dit ? Rien que toi et moi -- 250 zlitos.",
            "Hallo, Hübscher. Lust auf eine Privatshow hinten? Nur du und ich -- 250 Zlitos.",
            "Hola, guapo. ¿Te apetece un espectáculo privado al fondo? Solo tú y yo -- 250 zlitos.",
            "Ciao, bello. Ti va uno spettacolo privato sul retro? Solo io e te -- 250 zlitos.",
            "Olá, bonitão. Apetece-te um espetáculo privado lá atrás? Só tu e eu -- 250 zlitos.",
        },
        new[]
        {
            "Yes, let's go.",
            "Oui, allons-y.",
            "Ja, gehen wir.",
            "Sí, vamos.",
            "Sì, andiamo.",
            "Sim, vamos.",
        },
        new[]
        {
            "Not tonight, thanks.",
            "Pas ce soir, merci.",
            "Heute nicht, danke.",
            "Esta noche no, gracias.",
            "Non stasera, grazie.",
            "Hoje não, obrigado.",
        },
        new[]
        {
            "This way, sugar. Follow me.",
            "Par ici, mon chou. Suis-moi.",
            "Hier entlang, Süßer. Folge mir.",
            "Por aquí, cariño. Sígueme.",
            "Da questa parte, tesoro. Seguimi.",
            "Por aqui, querido. Segue-me.",
        },
        new[]
        {
            "You haven't got 250 zlitos, sugar. The change machine by the bar takes kashes.",
            "Tu n'as pas 250 zlitos, mon chou. Le changeur près du bar prend les kashes.",
            "Du hast keine 250 Zlitos, Süßer. Der Wechselautomat an der Bar nimmt Kashes.",
            "No tienes 250 zlitos, cariño. La máquina de cambio junto a la barra acepta kashes.",
            "Non hai 250 zlitos, tesoro. La macchina del cambio vicino al bar prende i kashes.",
            "Não tens 250 zlitos, querido. A máquina de câmbio ao pé do bar aceita kashes.",
        },
        new[]
        {
            "Suit yourself. I'll be around.",
            "Comme tu veux. Je ne suis pas loin.",
            "Wie du willst. Ich bin in der Nähe.",
            "Como quieras. Estaré por aquí.",
            "Come vuoi. Sono qui intorno.",
            "Como quiseres. Ando por aqui.",
        },
        new[]
        {
            "That's your show, handsome. Come back and see us.",
            "C'est la fin du spectacle, beau gosse. Reviens nous voir.",
            "Das war deine Show, Hübscher. Komm wieder vorbei.",
            "Ese fue tu espectáculo, guapo. Vuelve a vernos.",
            "Lo spettacolo è finito, bello. Torna a trovarci.",
            "Foi o teu espetáculo, bonitão. Volta a visitar-nos.",
        },
        new[]
        {
            "A change machine: one zlito for every 2 kashes. Change your kashes?",
            "Un changeur : un zlito pour 2 kashes. Changer tes kashes ?",
            "Ein Wechselautomat: ein Zlito für je 2 Kashes. Kashes wechseln?",
            "Una máquina de cambio: un zlito por cada 2 kashes. ¿Cambiar tus kashes?",
            "Una macchina del cambio: uno zlito ogni 2 kashes. Cambiare i tuoi kashes?",
            "Uma máquina de câmbio: um zlito por cada 2 kashes. Trocar os teus kashes?",
        },
        new[]
        {
            "Change them.",
            "Changer.",
            "Wechseln.",
            "Cambiarlos.",
            "Cambiali.",
            "Trocar.",
        },
        new[]
        {
            "Leave it.",
            "Laisser.",
            "Lieber nicht.",
            "Dejarlo.",
            "Lascia stare.",
            "Deixar.",
        },
        new[]
        {
            "The machine rattles and counts out your zlitos.",
            "La machine cliquette et compte tes zlitos.",
            "Der Automat rattert und zählt deine Zlitos ab.",
            "La máquina traquetea y cuenta tus zlitos.",
            "La macchina sferraglia e conta i tuoi zlitos.",
            "A máquina chocalha e conta os teus zlitos.",
        },
        new[]
        {
            "Hi there, sweetie. How about a lap dance? Only 50 zlitos -- I'm the bargain of the house.",
            "Coucou, mon sucre. Une petite danse rien que pour toi ? Seulement 50 zlitos -- la bonne affaire de la maison.",
            "Hallo, Zuckerstück. Wie wär's mit einem Lapdance? Nur 50 Zlitos -- das Schnäppchen des Hauses.",
            "Hola, cielo. ¿Qué tal un baile privado? Solo 50 zlitos -- la ganga de la casa.",
            "Ciao, dolcezza. Che ne dici di una lap dance? Solo 50 zlitos -- l'affare della casa.",
            "Olá, docinho. Que tal uma lap dance? Só 50 zlitos -- a pechincha da casa.",
        },
        new[]
        {
            "You haven't got 50 zlitos, sweetie. The change machine by the bar takes kashes.",
            "Tu n'as pas 50 zlitos, mon sucre. Le changeur près du bar prend les kashes.",
            "Du hast keine 50 Zlitos, Zuckerstück. Der Wechselautomat an der Bar nimmt Kashes.",
            "No tienes 50 zlitos, cielo. La máquina de cambio junto a la barra acepta kashes.",
            "Non hai 50 zlitos, dolcezza. La macchina del cambio vicino al bar prende i kashes.",
            "Não tens 50 zlitos, docinho. A máquina de câmbio ao pé do bar aceita kashes.",
        },
        new[]
        {
            "The freighter for Otringal is ready. Shall we load your car?",
            "Le cargo pour Otringal est prêt. On charge ta voiture ?",
            "Der Frachter nach Otringal ist bereit. Sollen wir dein Auto einladen?",
            "El carguero a Otringal está listo. ¿Cargamos tu coche?",
            "Il cargo per Otringal è pronto. Carichiamo la tua auto?",
            "O cargueiro para Otringal está pronto. Carregamos o teu carro?",
        },
        new[]
        {
            "Load it. To Otringal!",
            "Chargez-la. En route pour Otringal !",
            "Ladet es ein. Auf nach Otringal!",
            "Cargadlo. ¡A Otringal!",
            "Caricatela. Si va a Otringal!",
            "Carreguem-no. Para Otringal!",
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
            "The hatch closes. The saucer hums, lifts off the landing pad and climbs away towards Otringal.",
            "L'écoutille se ferme. La soucoupe vrombit, quitte l'aire d'atterrissage et s'élève en direction d'Otringal.",
            "Die Luke schließt sich. Die Untertasse summt, hebt vom Landeplatz ab und steigt in Richtung Otringal.",
            "La escotilla se cierra. El platillo zumba, despega de la plataforma de aterrizaje y asciende rumbo a Otringal.",
            "Il portello si chiude. Il disco ronza, si stacca dalla piattaforma di atterraggio e sale verso Otringal.",
            "A escotilha fecha-se. O disco zumbe, ergue-se da plataforma de aterragem e sobe rumo a Otringal.",
        },
        new[]
        {
            "No more flights to Otringal today, friend.",
            "Plus de vols pour Otringal aujourd'hui, l'ami.",
            "Heute keine Flüge mehr nach Otringal, Freund.",
            "Hoy no hay más vuelos a Otringal, amigo.",
            "Niente più voli per Otringal oggi, amico.",
            "Hoje não há mais voos para Otringal, amigo.",
        },
        new[]
        {
            "An Esmer saucer is waiting on the landing pad to take you and your car on to Otringal. Off you go, racer.",
            "Une soucoupe des Esmers t'attend sur l'aire d'atterrissage pour t'emmener à Otringal avec ta voiture. En route, pilote.",
            "Eine Untertasse der Esmer wartet auf dem Landeplatz, um dich und dein Auto nach Otringal zu bringen. Los geht's, Rennfahrer.",
            "Un platillo de los Esmers te espera en la plataforma de aterrizaje para llevarte a Otringal con tu coche. En marcha, piloto.",
            "Un disco degli Esmer ti aspetta sulla piattaforma di atterraggio per portare te e la tua auto a Otringal. Vai, pilota.",
            "Um disco dos Esmers espera-te na plataforma de aterragem para te levar a Otringal com o teu carro. Vai, piloto.",
        },
        new[]
        {
            "Congratulations for beating me. I've added my number to your phone, if you ever need any help.",
            "Félicitations, tu m'as battu. J'ai ajouté mon numéro à ta radio, si jamais tu as besoin d'aide.",
            "Glückwunsch, du hast mich geschlagen. Ich habe meine Nummer in dein Funkgerät eingespeichert, falls du je Hilfe brauchst.",
            "Enhorabuena por ganarme. He añadido mi número a tu radio, por si alguna vez necesitas ayuda.",
            "Congratulazioni per avermi battuto. Ho aggiunto il mio numero alla tua radio, se mai avessi bisogno di aiuto.",
            "Parabéns por me venceres. Adicionei o meu número ao teu rádio, se alguma vez precisares de ajuda.",
        },
        // (the performers in the dressing room -- RaceTrackStory.StellaEntities, DressingPeople, 2026-11-17)
        new[]
        {
            "Out, sweetie, dancers only back here! ... Oh, all right, you can stay. Pass me that eyelash -- no, the other one.",
            "Dehors, mon chou, c'est réservé aux danseuses ici ! ... Bon, d'accord, tu peux rester. Passe-moi ce cil -- non, l'autre.",
            "Raus, Schätzchen, hier hinten nur Tänzerinnen! ... Na gut, du darfst bleiben. Gib mir mal die Wimper -- nein, die andere.",
            "¡Fuera, cielo, aquí detrás solo bailarinas! ... Bueno, vale, puedes quedarte. Pásame esa pestaña... no, la otra.",
            "Fuori, tesoro, qui dietro solo ballerine! ... E va bene, puoi restare. Passami quella ciglia... no, l'altra.",
            "Fora, querido, aqui atrás só bailarinas! ... Pronto, está bem, podes ficar. Passa-me essa pestana -- não, a outra.",
        },
        new[]
        {
            "Has anyone seen my sequinned number? The blue one? It was right here five minutes ago, and I'm on next!",
            "Quelqu'un a vu ma tenue à paillettes ? La bleue ? Elle était juste là il y a cinq minutes, et c'est mon tour après !",
            "Hat jemand mein Paillettenkleid gesehen? Das blaue? Vor fünf Minuten hing es noch genau hier, und ich bin als Nächste dran!",
            "¿Alguien ha visto mi conjunto de lentejuelas? ¿El azul? Estaba aquí mismo hace cinco minutos, ¡y salgo la siguiente!",
            "Qualcuno ha visto il mio vestito di paillettes? Quello blu? Era proprio qui cinque minuti fa, e tocca a me tra poco!",
            "Alguém viu o meu fato de lantejoulas? O azul? Estava mesmo aqui há cinco minutos, e sou a próxima a entrar!",
        },
        new[]
        {
            "Twinsen? The one from the news? Well I never. Mind the screen, darling -- there's a girl changing behind it.",
            "Twinsen ? Celui des infos ? Ça alors. Attention au paravent, mon chéri -- il y a une fille qui se change derrière.",
            "Twinsen? Der aus den Nachrichten? Na so was. Vorsicht mit dem Paravent, Schätzchen -- dahinter zieht sich gerade eine um.",
            "¿Twinsen? ¿El de las noticias? Vaya, vaya. Cuidado con el biombo, cariño: hay una chica cambiándose detrás.",
            "Twinsen? Quello del telegiornale? Ma guarda un po'. Attento al paravento, tesoro: dietro c'è una ragazza che si cambia.",
            "Twinsen? O das notícias? Ora vejam só. Cuidado com o biombo, querido -- está uma rapariga a mudar-se atrás dele.",
        },
        new[]
        {
            "There! HAL's back on his circuits. And here's your radio, good as new -- thanks for the loan. Go and talk to him.",
            "Voilà ! HAL est de nouveau sur ses circuits. Et voici ta radio, comme neuve -- merci pour le prêt. Va lui parler.",
            "So! HAL läuft wieder. Und hier ist dein Funkgerät, wie neu -- danke fürs Leihen. Geh und sprich mit ihm.",
            "¡Listo! HAL vuelve a funcionar. Y aquí tienes tu radio, como nueva: gracias por prestárnosla. Ve a hablar con él.",
            "Ecco fatto! HAL è di nuovo in funzione. Ed ecco la tua radio, come nuova -- grazie del prestito. Vai a parlargli.",
            "Pronto! O HAL voltou a funcionar. E aqui está o teu rádio, como novo -- obrigado pelo empréstimo. Vai falar com ele.",
        },
        new[]
        {
            "Private shows only through here, pal. Pay one of the girls and she'll bring you in.",
            "Par ici, c'est réservé aux spectacles privés, mon gars. Paie une des filles et elle t'emmènera.",
            "Hier geht's nur zu den Privatshows, Kumpel. Bezahl eins der Mädchen, dann bringt sie dich rein.",
            "Por aquí solo se pasa a los espectáculos privados, amigo. Paga a una de las chicas y ella te llevará.",
            "Di qui si passa solo per gli spettacoli privati, amico. Paga una delle ragazze e ti porterà lei.",
            "Por aqui só se passa para os espetáculos privados, amigo. Paga a uma das raparigas e ela leva-te lá dentro.",
        },
        new[]
        {
            "HAL's gone mad, if you ask me -- \"ZX81! ZX81!\" all night long. Talk to our head scientist at HAL's console. He knows what's wrong.",
            "HAL est devenu fou, si tu veux mon avis -- « ZX81 ! ZX81 ! » toute la nuit. Parle à notre chef scientifique, à la console de HAL. Il sait ce qui ne va pas.",
            "HAL ist durchgedreht, wenn du mich fragst -- „ZX81! ZX81!“ die ganze Nacht. Sprich mit unserem Chefwissenschaftler an HALs Konsole. Er weiß, was los ist.",
            "HAL se ha vuelto loco, si me preguntas: «¡ZX81! ¡ZX81!» toda la noche. Habla con nuestro jefe científico, en la consola de HAL. Él sabe qué le pasa.",
            "HAL è impazzito, se vuoi saperlo: «ZX81! ZX81!» tutta la notte. Parla con il nostro capo scienziato, alla console di HAL. Lui sa cos'ha che non va.",
            "O HAL enlouqueceu, se queres saber -- «ZX81! ZX81!» a noite toda. Fala com o nosso cientista-chefe, na consola do HAL. Ele sabe o que se passa.",
        },
        new[]
        {
            "HAL's broken. His voice box is burnt out, and he won't switch on the start lights till it's mended. A part from a radio would do it, but we haven't got one up here. Ask the head scientist at the console.",
            "HAL est en panne. Son boîtier vocal a grillé, et il n'allumera pas les feux de départ tant qu'on ne l'aura pas réparé. Une pièce de radio ferait l'affaire, mais on n'en a pas ici. Demande au chef scientifique, à la console.",
            "HAL ist kaputt. Sein Sprachmodul ist durchgebrannt, und die Startampel schaltet er erst ein, wenn es repariert ist. Ein Teil aus einem Funkgerät würde reichen, aber wir haben hier oben keins. Frag den Chefwissenschaftler an der Konsole.",
            "HAL está averiado. Se le ha quemado la caja de voz, y no encenderá el semáforo de salida hasta que se la arreglen. Una pieza de una radio serviría, pero aquí arriba no tenemos ninguna. Pregúntale al jefe científico, en la consola.",
            "HAL è guasto. Gli si è bruciato il modulo vocale, e non accenderà il semaforo di partenza finché non glielo ripariamo. Basterebbe un pezzo di una radio, ma quassù non ne abbiamo. Chiedi al capo scienziato, alla console.",
            "O HAL está avariado. A caixa de voz queimou-se, e ele não acende as luzes da partida enquanto não a repararem. Uma peça de um rádio resolvia, mas aqui em cima não temos nenhum. Pergunta ao cientista-chefe, na consola.",
        },
        new[]
        {
            "Is that a radio you've got there? That's just what HAL needs! Show it to the head scientist at the console.",
            "C'est une radio que tu as là ? C'est exactement ce qu'il faut à HAL ! Montre-la au chef scientifique, à la console.",
            "Ist das ein Funkgerät, das du da hast? Genau das braucht HAL! Zeig es dem Chefwissenschaftler an der Konsole.",
            "¿Eso que llevas es una radio? ¡Es justo lo que necesita HAL! Enséñasela al jefe científico, en la consola.",
            "Quella che hai lì è una radio? È proprio quello che serve a HAL! Mostrala al capo scienziato, alla console.",
            "Isso aí é um rádio? É mesmo do que o HAL precisa! Mostra-o ao cientista-chefe, na consola.",
        },
        new[]
        {
            "Your radio did it. HAL's back to himself... more or less. Go on, talk to him.",
            "C'est ta radio qui a fait ça. HAL est redevenu lui-même... plus ou moins. Vas-y, parle-lui.",
            "Dein Funkgerät hat's geschafft. HAL ist wieder er selbst ... mehr oder weniger. Na los, sprich mit ihm.",
            "Lo ha conseguido tu radio. HAL vuelve a ser él mismo... más o menos. Venga, habla con él.",
            "Ce l'ha fatta la tua radio. HAL è tornato quello di prima... più o meno. Dai, parlagli.",
            "Foi o teu rádio. O HAL voltou a ser o que era... mais ou menos. Vá, fala com ele.",
        },
        new[]
        {
            "HAL says he wants to race you himself. We couldn't talk him out of it.",
            "HAL dit qu'il veut faire la course contre toi en personne. On n'a pas réussi à l'en dissuader.",
            "HAL sagt, er will selbst gegen dich fahren. Wir konnten es ihm nicht ausreden.",
            "HAL dice que quiere correr contra ti en persona. No hemos podido quitárselo de la cabeza.",
            "HAL dice che vuole sfidarti lui in persona. Non siamo riusciti a fargli cambiare idea.",
            "O HAL diz que quer correr contra ti ele próprio. Não conseguimos tirar-lhe a ideia da cabeça.",
        },
        new[]
        {
            "HAL's waiting for you in the pit lane. The start lights are on -- go and race.",
            "HAL t'attend dans la voie des stands. Les feux de départ sont allumés -- va courir.",
            "HAL wartet in der Boxengasse auf dich. Die Startampel ist an -- los, fahr.",
            "HAL te espera en el carril de boxes. El semáforo de salida está encendido: ve a correr.",
            "HAL ti aspetta nella corsia dei box. Il semaforo di partenza è acceso: vai a correre.",
            "O HAL está à tua espera na via das boxes. As luzes da partida estão acesas -- vai correr.",
        },
        new[]
        {
            "It's so quiet in here without HAL shouting. He's out at the track, waiting for you.",
            "C'est si calme ici sans HAL qui crie. Il est sur le circuit, il t'attend.",
            "Ohne HALs Geschrei ist es so still hier. Er ist draußen an der Strecke und wartet auf dich.",
            "Qué silencio hay aquí sin los gritos de HAL. Está en el circuito, esperándote.",
            "Che silenzio qui senza HAL che urla. È in pista, ti aspetta.",
            "Está tudo tão calado sem os gritos do HAL. Está na pista, à tua espera.",
        },
        new[]
        {
            "You beat HAL! He hasn't stopped sulking since.",
            "Tu as battu HAL ! Il n'a pas arrêté de bouder depuis.",
            "Du hast HAL geschlagen! Seitdem schmollt er ununterbrochen.",
            "¡Le has ganado a HAL! Desde entonces no ha parado de enfurruñarse.",
            "Hai battuto HAL! Da allora non ha smesso di tenere il broncio.",
            "Ganhaste ao HAL! Desde então não parou de amuar.",
        },
        new[]
        {
            "Thanks for the loan of the radio, Twinsen. HAL's never sounded better.",
            "Merci pour le prêt de la radio, Twinsen. HAL n'a jamais eu une aussi belle voix.",
            "Danke, dass du uns dein Funkgerät geliehen hast, Twinsen. HAL hat noch nie so gut geklungen.",
            "Gracias por prestarnos la radio, Twinsen. HAL nunca había sonado tan bien.",
            "Grazie per averci prestato la radio, Twinsen. HAL non ha mai avuto una voce così bella.",
            "Obrigado por nos emprestares o rádio, Twinsen. O HAL nunca soou tão bem.",
        },
        new[]
        {
            "Maybe one of his scientists knows what's wrong with him.",
            "Ses scientifiques savent peut-être ce qui ne va pas chez lui.",
            "Vielleicht weiß einer seiner Wissenschaftler, was mit ihm los ist.",
            "Quizá alguno de sus científicos sepa qué le pasa.",
            "Forse uno dei suoi scienziati sa cos'ha che non va.",
            "Talvez um dos cientistas dele saiba o que se passa com ele.",
        },
    };

    // The moon's landing pad (the outside scene with the game's door into its Esmer shuttle, scene 115 -- the platform's west face): the
    // hangar's saucer (the game's, scene 12's actor 6) parked on the platform; the moon won (chapter 5, HAL's log told), Twinsen walked in
    // through the shuttle's door is aboard -- held, the lift-off said -- and on to Otringal's start
    private const int ShuttleInside = 115;
    private static readonly (int X, int Y, int Z) PadTop = (21248, 700, 17390);
    private static void MoonSaucer(IslandBeatsA b, int liftLine)
    {
        var saucer = Copy(b.Originals, HangarScene, HangarSaucer);
        foreach (var scene in b.OutsideScenes.ToList())
            b.Edit(scene, "the landing pad: the Esmers' saucer parked on its platform (the shuttle's door the way aboard)", model =>
            {
                if (saucer is null || !model.Zones.Any(z => z.Type == 0 && z.Num == ShuttleInside)) return _ => { };
                var ship = saucer.Clone();
                (ship.X, ship.Y, ship.Z) = PadTop;
                ship.Beta = 0;
                // (walked through: its box, as wide as the saucer, kept Twinsen from the shuttle's door at the platform's foot)
                ship.Armor = WalkOverArmour;
                // (and not moved by what walks into it: with the game's CHECK_OBJ_COL kept, its own collision put it out of Twinsen's way --
                // he pushed it about the pad, the user, 2026-11-17: "We need to prevent the spaceship actor on Emerald moon being able to be
                // pushed by Twinsen")
                ship.Flags &= ~0x1u;
                var addShip = IslandBeatsA.Add(model, ship, @"void comportement_0()
{
}
");
                // (and aboard as he comes up to the shuttle's door, the moon won: in the track's copy of the cube the platform's foot can't be
                // walked onto, its door out of reach)
                // (on foot or in the car -- set_dir_obj(0, NO_MOVE) takes him out of it, and the saucer's arrival on Otringal puts him back in,
                // in_car= -- the user, 2026-11-21: "we need to improve the trigger zone though and make it so Twinsen can also drive aboard")
                var door = model.Zones.First(z => z.Type == 0 && z.Num == ShuttleInside);
                string Aboard(string near) => $@"void comportement_0()
{{
    if (5 <= chapter() && {HalDone} == var_game({QHal}) && {near})
    {{
        set_dir_obj(0, NO_MOVE);
        change_cube({ShuttleInside});
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
}}
";
                var helper = RaceTrackScenes.InvisibleHelper((door.X0 + door.X1) / 2, Math.Min(door.Y0, door.Y1), (door.Z0 + door.Z1) / 2);
                var addHelper = IslandBeatsA.Add(model, helper, Aboard($"{BoardReach} > distance(0)"));
                // (and all round the saucer itself, where he comes up to it on the pad -- in 3D, so the road passing over the pad doesn't)
                var round = RaceTrackScenes.InvisibleHelper(PadTop.X, PadTop.Y, PadTop.Z);
                var addRound = IslandBeatsA.Add(model, round, Aboard($"{SaucerReach} > distance_3d(0)"));
                return scripts => { addShip(scripts); addHelper(scripts); addRound(scripts); };
            });
        if (!b.Store.SceneExists(ShuttleInside)) { b.Log.Add($"the saucer to Otringal left out: no scene {ShuttleInside}"); return; }
        b.Edit(ShuttleInside, $"aboard the Esmers' saucer once the moon is won: the lift-off, on to Otringal's space port (scene {SpaceportScene}, in his car)", model =>
        {
            var helper = RaceTrackScenes.InvisibleHelper(model.Hero.X, model.Hero.Y, model.Hero.Z);
            return IslandBeatsA.Add(model, helper, $@"void comportement_0()
{{
    if (5 <= chapter() && {HalDone} == var_game({QHal}))
    {{
        set_dir_obj(0, NO_MOVE);
        cinema_mode(1); // (the lift-off watched: the cut-scene bars, off before Otringal -- the user, 2026-11-18)
        set_var_cube({SaucerWait}, 0);
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    add_var_cube({SaucerWait}, 1);
    if (20 < var_cube({SaucerWait}))
    {{
        message({liftLine});
        set_var_game({OtringalInCar}, 1);
        cinema_mode(0);
        change_cube({SpaceportScene});
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
}}
");
        });
    }
    // The moon base's X door (scene 23's actor 7, its X-striped door down into the floor while var_game 93 is 1) has its wheel (actor 3, on
    // its striped stand, block 151 at cells 47-48, 42-43) only on the far side of the glass: come in from the X building (scene 53), on the
    // corridor's side, Twinsen was shut in -- the user, 2026-11-16: "If Twinsen enters the Emerald Moon base area by the "x" entrance, where
    // in the retail game he escapes with Baldino he gets trapped at this door as the controls are on the other side of the door. Let's add
    // an extra control for it the other side of the barrier to prevent this". A second stand (the same block) in the corridor beside the
    // door, and a second wheel on it (actor 3's sprite and its turning, the same height over its stand): Action by it works the door as
    // the first does. Its stand in the corridor's corner by the first's, across the wall from it, cells 44-45, 41-42 (the user, 2026-11-17:
    // "move it into the corner that's exactly next to where the second switch is on the other side of the door" -- it stood in front of the
    // doorway, at 44-45, 35-36)
    private const int XDoorVar = 93, XWheel = 3, XStandBlock = 151, XStandFloor = 60, XStandX = 44, XStandZ = 41, XWheelFrom = 47 * 512, XWheelFromZ = 42 * 512;
    private static void XDoorWheel(string gameDirectory, IslandBeatsA b, List<string> log)
    {
        if (!b.Store.SceneExists(BaseScene)) return;
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (BaseScene * 2 + 1 >= table.Length) { log.Add("the X door's second wheel left out: no grid for scene 23"); return; }
        var gridEntry = header.Gri + table[BaseScene * 2 + 1];
        var raw = bkg.Read(gridEntry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * 64 + x) * 25 + y) * 2;
        // (the first stand's bricks, each of its four columns layers 2-4, to the same places of the second)
        var edits = new List<Lba1GridCell>();
        for (var dz = 0; dz < 2; dz++)
        for (var dx = 0; dx < 2; dx++)
        {
            int fx = XWheelFrom / 512 + dx, fz = XWheelFromZ / 512 + dz, tx = XStandX + dx, tz = XStandZ + dz;
            if (cells[At(tx, 1, tz)] != XStandFloor || Enumerable.Range(2, 3).Any(y => cells[At(tx, y, tz)] != 0)) { log.Add($"the X door's second wheel left out: cell ({tx}, {tz}) isn't the corridor's bare floor"); return; }
            for (var y = 2; y <= 4; y++)
            {
                if (cells[At(fx, y, fz)] != XStandBlock) { log.Add($"the X door's second wheel left out: the first stand isn't block {XStandBlock} at ({fx}, {y}, {fz})"); return; }
                edits.Add(new Lba1GridCell(tx, y, tz, XStandBlock, cells[At(fx, y, fz) + 1]));
            }
        }
        var entry = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, edits));
        bkg.SetStored(gridEntry, entry);
        if (header.MaxGri < (uint)entry.Length) { header.MaxGri = (uint)entry.Length; bkg.SetStored(0, header.Bytes); }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Commit();
        b.Edit(BaseScene, "the X door's second wheel, in the corridor (Action: the door, var_game 93)", model =>
        {
            var first = model.Actors[XWheel];
            var wheel = first.Clone();
            wheel.X = first.X - (XWheelFrom - XStandX * 512); wheel.Z = first.Z - (XWheelFromZ - XStandZ * 512);
            wheel.Life = new byte[] { 0 }; wheel.Track = new byte[] { 0 };
            var index = SceneOps.AddActor(model, wheel);
            return scripts =>
            {
                scripts.SetText(index, ScriptKind.Track, scripts.GetText(XWheel, ScriptKind.Track));
                scripts.SetText(index, ScriptKind.Life, $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if (1500 > distance(0))
        {{
            if (0 == var_game({XDoorVar}))
            {{
                set_var_game({XDoorVar}, 1);
                set_track(label_0);
            }}
            else
            {{
                set_var_game({XDoorVar}, 0);
                set_track(label_1);
            }}
        }}
    }}
}}
");
            };
        });
        log.Add($"the X door's second wheel: block {XStandBlock} at cells {XStandX}-{XStandX + 1}, {XStandZ}-{XStandZ + 1} in scene {BaseScene}'s grid, a wheel on it working var_game {XDoorVar}");
    }

    // (scene 115's cell variable counting the moments before the lift-off -- its own scripts use 0-3)
    private const int SaucerWait = 20, BoardReach = 1800, SaucerReach = 3000;

    // The moon's arrival (the user, 2026-11-17: "when we first land on the moon let's still be in the spaceship with our car in there too.
    // Twinsen drives the car out, and there's a short section of extra track that can take him up to the race track. There's already a
    // landing zone that we're using to fly to Otringal, so we can use this for our Emerald Moon stop off"): the Desert's freighter brings
    // him to the saucer's inside, scene 115 (RaceTrackStory.Desert's MoonStart), in his car on its lower deck facing the door. A moment, and
    // it rolls out through the doorway and down the steps, and he is outside on the landing pad in his car (the story file's in_car=,
    // RACEMOD.CPP InCarFrame: MoonInCar 1, then 2 -- the arrival done), facing north to the ramp up onto the pit lane (the plan's spur:
    // RaceTrackBuilder, a branch with no way back). Chapter 4, the first time in only. (He sits in it from the start, hidden: the lower deck
    // is behind the hull's front from the room's camera -- a walk to the car there was never seen, 2026-11-17's test.)
    public const int MoonInCar = 261;
    // the car on the lower deck (cells 9-15 x 9-14 at 1536, the doorway x 8-12): its middle, facing the door (+z), as far forward as the
    // deck goes; the way out -- the deck, the doorway's sill (1792, cells z 14-17), the three steps down to the door (z 18-20)
    private static readonly (int X, int Y, int Z) ArriveCar = (5376, 1536, 5888);
    private static readonly (int X, int Y, int Z)[] CarWayOut = { (5376, 1536, 7040), (5376, 1792, 7424), (5376, 1792, 8960), (5376, 1536, 9472), (5376, 1280, 9984), (5376, 1024, 10560) };
    // outside: the shuttle's door's own landing (scene 115's way out, its zone's info), facing north
    internal static readonly (int X, int Y, int Z, int Beta) PadArrival = (17920, 512, 15872, 2048);
    // the car's track labels, the helper's moment counter (a cell variable; the scene's own scripts use 0-3, the lift-off's 20)
    private const int CarOut = 1, CarDone = 2, ArriveWait = 21;
    internal static string MoonInCarLine() => $"in_car={MoonInCar} {PadArrival.Beta} {PadArrival.X} {PadArrival.Y} {PadArrival.Z} 230\n" +
        $"in_car={OtringalInCar} {SpaceportArrival.Beta} {SpaceportArrival.X} {SpaceportArrival.Y} {SpaceportArrival.Z} {SpaceportScene}\n";
    // On to Otringal: the Esmers' saucer lands him at the space port (scene 92) in his car, beside the ship parked there (RaceTrackIsland
    // Otringal's Parked, actor 3, cells 28, 27.5) at the foot of the spur up onto the east leg, facing it -- the user, 2026-11-17: "When
    // Twinsen lands in Ortingal let's have him arrive at the spaceport via the spaceship on the left of this picture" (OtringalInCar 1 at the
    // lift-off, 2 once he is in it there)
    public const int OtringalInCar = 263, SpaceportScene = 92;
    internal static readonly (int X, int Y, int Z, int Beta) SpaceportArrival = (30 * 512, 3400, 33 * 512, 1024);

    private static void MoonArrival(IslandBeatsA b)
    {
        if (!b.Store.SceneExists(ShuttleInside)) return;
        b.Edit(ShuttleInside, "the moon's arrival: Twinsen in his car on the saucer's lower deck, driven out down its steps onto the landing pad (chapter 4, once)", model =>
        {
            // (Twinsen in the car: the scene's start at its middle, he hidden there)
            (model.Hero.X, model.Hero.Y, model.Hero.Z) = ArriveCar;
            model.Hero.Beta = 0;
            int Pt((int X, int Y, int Z) p) => SceneOps.AddTrackPoint(model, new SceneTrackPoint(p.X, p.Y, p.Z));
            var car = Citadel.CitadelProps.Actor(HangarProps.FreighterBuggy, ArriveCar.X, ArriveCar.Y, ArriveCar.Z, 0);
            if (HangarProps.Entity(b.GameDirectory) is var hangarEntity && hangarEntity >= 0) car.Entity = hangarEntity;
            car.Move = 3; car.Armor = WalkOverArmour;
            var carIndex = model.Actors.Count;
            var addCar = IslandBeatsA.Add(model, car, $@"void comportement_0()
{{
    if (4 != chapter() || 0 != var_game({MoonInCar}))
    {{
        invisible(1);
        suicide();
    }}
}}
");
            // (rolled a step a tenth of a second at a time: a prop has no walk, and goto_point_3d set it there at once)
            var drive = new System.Text.StringBuilder($"label(0);\nstop();\n\nlabel({CarOut});\n");
            var from = ArriveCar;
            foreach (var to in CarWayOut)
            {
                var steps = Math.Max(1, (int)Math.Ceiling(Math.Sqrt((double)(to.X - from.X) * (to.X - from.X) + (double)(to.Z - from.Z) * (to.Z - from.Z)) / 110));
                for (var k = 1; k <= steps; k++)
                    drive.Append($"pos_point({Pt((from.X + (to.X - from.X) * k / steps, from.Y + (to.Y - from.Y) * k / steps, from.Z + (to.Z - from.Z) * k / steps))});\nwait_nb_dizieme(1);\n");
                from = to;
            }
            drive.Append($"\nlabel({CarDone});\nstop();\n");
            var helper = RaceTrackScenes.InvisibleHelper(ArriveCar.X, ArriveCar.Y, ArriveCar.Z);
            var addHelper = IslandBeatsA.Add(model, helper, $@"void comportement_0()
{{
    if (4 != chapter() || 0 != var_game({MoonInCar}))
    {{
        suicide();
    }}
    else
    {{
        cinema_mode(1);
        set_dir_obj(0, NO_MOVE);
        set_var_cube({ArriveWait}, 0);
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    add_var_cube({ArriveWait}, 1);
    if (30 < var_cube({ArriveWait}))
    {{
        set_track_obj({carIndex}, label_{CarOut});
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if ({CarDone} == l_track_obj({carIndex}))
    {{
        cinema_mode(0);
        set_var_game({MoonInCar}, 1);
        change_cube(230);
        set_comportement(comportement_3);
    }}
}}

void comportement_3()
{{
}}
");
            return scripts =>
            {
                addCar(scripts); addHelper(scripts);
                scripts.SetText(carIndex, ScriptKind.Track, drive.ToString());
                // Twinsen in the car, hidden, held -- before his own scripts, the first time in in chapter 4
                var life = scripts.GetText(0, ScriptKind.Life);
                var next = Regex.Matches(life, @"void comportement_(\d+)\(\)").Select(m => int.Parse(m.Groups[1].Value)).DefaultIfEmpty(0).Max() + 1;
                life = Loose(life, "void comportement_0()\n{", $"void comportement_0()\n{{\n    if (4 == chapter() && 0 == var_game({MoonInCar}))\n    {{\n        invisible(1);\n        set_dir(NO_MOVE);\n        set_comportement(comportement_{next});\n        return;\n    }}");
                scripts.SetText(0, ScriptKind.Life, life.TrimEnd() + $"\n\nvoid comportement_{next}()\n{{\n}}\n");
            };
        });
    }

    // A talker of HAL's room answering only when the hero's script has Twinsen facing it (Faced): its "within reach" made "faced"
    private static string FacedTalker(int who, string talker)
    {
        const string near = "2500 > distance(0)";
        if (!talker.Contains(near)) throw new InvalidOperationException("HAL's room: a talker without its reach");
        return talker.Replace(near, $"{who} == var_cube({Faced})");
    }

    // The hero's script in HAL's room (the moon base, scene 23): Action -- whom Twinsen faces, near (distance_message, his own facing): the
    // scientist who walks the room or the one at the bench, each his own word on HAL as the story goes (broken; mended, here or out at the
    // track; beaten), the head scientist at the console (the mechanics' talker), or HAL (his talker). Not while his radio is being fitted
    // (Q_HAL 3: the cut scene).
    private static void FacingInHalsRoom(SceneScripts scripts, int halTalker, string moon, Func<MoonLine, int> say)
    {
        string Says(int who, MoonLine broken, MoonLine mended, MoonLine outAtTrack, MoonLine beaten, MoonLine? radioSeen) => $@"                set_var_cube({Faced}, {who});
                {(radioSeen is { } seen ? $@"if ({RadioLent} > var_game({QHal}) && 0 < var_game({Radio}))
                {{
                    message_obj({who}, {say(seen)});
                }}
                else " : "")}if ({RadioLent} > var_game({QHal}))
                {{
                    message_obj({who}, {say(broken)});
                }}
                else if ({HalWon} <= var_game({QHal}))
                {{
                    message_obj({who}, {say(beaten)});
                }}
                else if (0 == var_cube({HalHere}))
                {{
                    message_obj({who}, {say(outAtTrack)});
                }}
                else
                {{
                    message_obj({who}, {say(mended)});
                }}";
        var block = $@"
    swif (1 == action())
    {{
        set_var_cube({Faced}, 0);
        if ({moon} && {Driving} != comportement_hero() && {RadioLent} != var_game({QHal}))
        {{
            if ({NearScientist} > distance_message({ScientistWalker}))
            {{
{Says(ScientistWalker, MoonLine.WalkerBroken, MoonLine.WalkerMended, MoonLine.WalkerOut, MoonLine.WalkerBeaten, null)}
            }}
            else if ({NearScientist} > distance_message({ScientistBench}))
            {{
{Says(ScientistBench, MoonLine.BenchBroken, MoonLine.BenchMended, MoonLine.BenchOut, MoonLine.BenchBeaten, MoonLine.BenchRadioSeen)}
            }}
            else if ({NearHead} > distance_message({HeadScientist}))
            {{
                set_var_cube({Faced}, {HeadScientist});
            }}
            else if ({NearHal} > distance_message({halTalker}))
            {{
                set_var_cube({Faced}, {FacedHal});
            }}
        }}
    }}";
        var hero = scripts.GetText(0, ScriptKind.Life);
        scripts.SetText(0, ScriptKind.Life, Loose(hero, "void comportement_1()\n{", "void comportement_1()\n{" + block));
    }

    private const uint PushableFlag = 1u << 4, CheckObjColFlag = 1u; // (COMMON.H PUSHABLE, CHECK_OBJ_COL)
    private static List<string> ApplyEmerald(string gameDirectory, RaceTrackReport report, RaceTrackScenes.Result scenes)
    {
        var log = new List<string>();
        if (report.StartLine.Count == 0 || scenes.StartScene < 0) { log.Add("the Emerald Moon: no story, its track has no start line"); return log; }
        var first = AddIslandTexts(gameDirectory, RaceTrackIsland.Emerald.IslandByte, MoonLines, log, "the Emerald Moon");
        if (first < 0) return log;
        int Say(MoonLine line) => first + (int)line;
        var b = new IslandBeatsA(gameDirectory, RaceTrackIsland.Emerald, log);
        // (won, an Esmer saucer on to Otringal from the moon's own landing pad -- the user, 2026-11-14: "We need to have a different spaceship
        // take us from Emerald Moon to Otringal. Emerald Moon already has a place spaceships take off and land, let's use this area for
        // Twinsen to board a spaceship from the moon to otringal": MoonSaucer. The base's hangar, scene 208, no longer the way)
        MoonSaucer(b, Say(MoonLine.HangarLift));
        // (and the arrival: in the saucer with his car, driven out onto the landing pad, the ramp up onto the pit lane -- MoonArrival)
        MoonArrival(b);
        // (Zoe as companion's hints on the Emerald Moon: HAL, then the race -- RaceTrackStory.Companion.cs)
        CompanionTalks[RaceTrackIsland.Emerald.IleFile] = $@"            if ({HalMended} > var_game({QHal}))
            {{
                message({Say(MoonLine.ZoeHintHal)});
            }}
            else
            {{
                message({Say(MoonLine.ZoeHintRace)});
            }}";
        const string moon = "4 == chapter()";

        // (no radio call on the moon: Baldino's from his lander went with him -- the user, 2026-11-16, "Baldino is no longer going to be on
        // Emerald Moon"; his call and HAL's both came of one use of the radio, "using the radio on the emerald moon triggers both Baldino and
        // HAL" -- RACEMOD.CPP's HalFrame mended too)

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

        // the airlocks' hooks: Twinsen's helmet on its stand where the empty suit hung (MoonAirlocks)
        foreach (var (scene, actor) in MoonAirlocks.Where(a => b.Store.SceneExists(a.Scene)))
            b.Edit(scene, $"the airlock's hook (actor {actor}): Twinsen's helmet on its stand, not the empty suit", model =>
            {
                if (actor >= model.Actors.Count || model.Actors[actor].Entity != EmptySuitEntity) { log.Add($"the airlock's hook: scene {scene}'s actor {actor} isn't the empty suit -- left"); return _ => { }; }
                model.Actors[actor].Entity = Citadel.CitadelProps.Prop;
                model.Actors[actor].Body = unchecked((sbyte)Citadel.CitadelProps.HangingHelmet);
                return scripts =>
                {
                    var life = scripts.GetText(actor, ScriptKind.Life);
                    var now = life.Replace("body(0);", $"body({Citadel.CitadelProps.HangingHelmet});");
                    if (now != life) scripts.SetText(actor, ScriptKind.Life, now);
                };
            });

        // the base's X door's second wheel, inside (XDoorWheel)
        XDoorWheel(gameDirectory, b, log);

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
                var halTalker = model.Actors.Count;
                var hal = IslandBeatsA.Add(model, RaceTrackScenes.InvisibleHelper(HalSpot.X, HalSpot.Y, HalSpot.Z, HalVoice), FacedTalker(FacedHal, Talker($"{moon} && {HalMended} > var_game({QHal})", $@"            if ({RadioLent} == var_game({QHal}))
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
                message_obj(0, {Say(MoonLine.TwinsenNo)});
                message_obj(0, {Say(MoonLine.TwinsenAskScientists)});
                set_var_game({QHal}, {HalHeard});
            }}
            else if ({HalWon} == var_game({QHal}))
            {{
                message({Say(MoonLine.HalLog)});
                {Clue(ClueLaunch)}
            }}
            else
            {{
{HalDaisy(Say(MoonLine.HalThanks), Say(MoonLine.HalDaisy), Say(MoonLine.HalDiagnostic))}
            }}", 2500, $"    set_var_cube({HalHere}, 1);\n")));
                // (the head scientist at HAL's console says the mechanics' lines, from his own head)
                var mechanics = IslandBeatsA.Add(model, RaceTrackScenes.InvisibleHelper(MechanicSpot.X, MechanicSpot.Y, MechanicSpot.Z, 4), FacedTalker(HeadScientist, Talker(moon, $@"            if ({RadioLent} > var_game({QHal}) && 0 < var_game({Radio}))
            {{
                message_obj({HeadScientist}, {Say(MoonLine.MechanicSpare)});
                message_obj(0, {Say(MoonLine.TwinsenOffer)});
                message_obj({HeadScientist}, {Say(MoonLine.MechanicThanks)});
                message_obj(0, {Say(MoonLine.TwinsenBack)});
                set_var_game({Radio}, 0);
                message_obj({HeadScientist}, {Say(MoonLine.MechanicLeave)});
                set_var_game({QHal}, {RadioLent});
                set_var_cube({MendWait}, 0);
                cinema_mode(1);
                set_dir_obj(0, NO_MOVE);
                set_comportement(comportement_2);
            }}
            else if ({RadioLent} > var_game({QHal}))
            {{
                message_obj({HeadScientist}, {Say(MoonLine.MechanicSpare)});
            }}
            else if ({HalMended} <= var_game({QHal}) && 0 == var_cube({HalHere}))
            {{
                message_obj({HeadScientist}, {Say(MoonLine.MechanicHalOut)});
            }}
            else
            {{
                message_obj({HeadScientist}, {Say(MoonLine.MechanicWorking)});
            }}", 2500)) + $@"
// (HAL mended while Twinsen waits, held in a cut scene; the radio brought back, and the word to go and talk to HAL)
void comportement_2()
{{
    add_var_cube({MendWait}, 1);
    if ({MendFrames} < var_cube({MendWait}))
    {{
        message_obj({HeadScientist}, {Say(MoonLine.MechanicMended)});
        found_object({Radio});
        set_var_game({Radio}, 1);
        set_var_game({QHal}, {HalMended});
        cinema_mode(0);
        set_dir_obj(0, MOVE_MANUAL);
        set_comportement(comportement_1);
    }}
}}
");
                return scripts => { hal(scripts); mechanics(scripts); FacingInHalsRoom(scripts, halTalker, moon, Say); };
            });
        }
        else log.Add($"HAL and the mechanics left out: no scene {BaseScene}");

        // won: out of the car by the start line -- the moon guard, Lyra, HAL's number on Twinsen's radio -- and the Esmers' ship to Otringal.
        // (HAL's log of the black car, and Twinsen's word on it, no longer said unasked here: the user, 2026-11-16, "Twinsen seems to be
        // getting an automatic hint from HAL, let's stop this. Let's have HAL say at the end of the Emerald Moon race "Congratulations for
        // beating me, I've added my number to your phone if you ever need any help" - then leave it. This should be player initiated" --
        // the log is still his in the base, talked to; the radio calls him from now on: RACEMOD.CPP HalCall, the race file's hal_mended=)
        foreach (var after in b.OutsideScenes.ToList())
        b.Edit(after, "after a win: the moon guard, Lyra, HAL's log of the black car, Twinsen, and the Esmers' ship to Otringal (inc_chapter, scene 88)", model =>
        {
            var spot = AfterRaceSpot(report);
            if ((int)Math.Floor(spot.X / 64) != model.CubeX || (int)Math.Floor(spot.Z / 64) != model.CubeY) spot = (model.CubeX * 64 + 32, model.CubeY * 64 + 32, report.StartLine[0].Y, 0);
            var guard = model.Actors.Count; var hal = guard + 1;
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
    if ({HalWon} == var_game({QHal}) && 1 == var_game({HalPullVar}) && {Driving} != comportement_hero())
    {{
        // (a cut scene: the bars, Twinsen held, the camera on HAL's car beside him till it's over -- RACEMOD.CPP win_pull='s camera, back on
        // Twinsen as HalPullVar moves on at the end; the user, 2026-11-23, HAL on screen as he talks, Twinsen held as in a cut scene)
        cinema_mode(1);
        set_dir_obj(0, NO_MOVE);
        message_obj({guard}, {Say(MoonLine.GuardBeaten)});
        message({Say(MoonLine.LyraZeelich)});
        message_obj({hal}, {Say(MoonLine.HalNumber)});
        message({Say(MoonLine.HangarGo)});
        set_var_game({HalPullVar}, 2);
        set_var_game({QHal}, {HalDone});
        set_dir_obj(0, MOVE_MANUAL);
        cinema_mode(0);
        if (5 > chapter())
        {{
            inc_chapter();
        }}
    }}
}}
"),
            };
            return scripts => { foreach (var e in edits) e(scripts); };
        });

        // the eggs: petanque in low gravity (RaceTrackStory.Eggs.Emerald.cs)
        MoonPetanque(b, report, Say(MoonLine.BowlerUp), Say(MoonLine.BowlerTuesday));
        // Stella Entities' people: Rick's bouncers, his guard and his barman (RaceTrackStory.StellaEntities.cs)
        StellaPeople(b, Say);
        StellaLounge(b, Say);
        // (and nobody in the club pushed about -- the user, 2026-11-18, "Twinsen seems to be able to push a lot of the NPCs around in the strip
        // club, we should fix the actor settings so they can't be pushed": none of them is PUSHABLE, but a person checking his own collisions,
        // CHECK_OBJ_COL, is put out of Twinsen's box whenever an animation or a track step moves him -- shoved along as Twinsen walks into
        // him. So the club's people check none -- as the lounge's hostess, waitresses and dancer already didn't: Twinsen is stopped by them,
        // and they stay where they are. Props are left as they are.)
        foreach (var scene in new[] { StellaEntities.Scene, StellaRooms.Backstage, StellaRooms.Office, StellaRooms.Private })
            if (b.Store.SceneExists(scene))
                b.Edit(scene, "nobody in Stella Entities pushed about: no person checks his own collisions (CHECK_OBJ_COL), none PUSHABLE", model =>
                {
                    var cleared = 0;
                    foreach (var a in model.Actors.Skip(1).Where(a => a.Entity >= 0 && a.Entity != Citadel.CitadelProps.Prop))
                        if ((a.Flags & (PushableFlag | CheckObjColFlag)) != 0) { a.Flags &= ~(PushableFlag | CheckObjColFlag); cleared++; }
                    log.Add($"scene {scene}: {cleared} people no longer shoved about");
                    return _ => { };
                });

        b.Save();
        log.Add($"the Emerald Moon's race file: gate={QHal} {HalMended} (HAL mended), win={QHal} {HalWon}");
        return log;
    }
}
