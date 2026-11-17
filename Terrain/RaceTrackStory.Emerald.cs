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
    private static readonly (int Scene, int Actor)[] MoonAirlocks = { (31, 12), (52, 9), (53, 6) };
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
                var addShip = IslandBeatsA.Add(model, ship, @"void comportement_0()
{
}
");
                // (and aboard as he comes up to the shuttle's door, the moon won: in the track's copy of the cube the platform's foot can't be
                // walked onto, its door out of reach)
                var door = model.Zones.First(z => z.Type == 0 && z.Num == ShuttleInside);
                var helper = RaceTrackScenes.InvisibleHelper((door.X0 + door.X1) / 2, Math.Min(door.Y0, door.Y1), (door.Z0 + door.Z1) / 2);
                var addHelper = IslandBeatsA.Add(model, helper, $@"void comportement_0()
{{
    if (5 <= chapter() && {HalDone} == var_game({QHal}) && {BoardReach} > distance(0) && {Driving} != comportement_hero())
    {{
        set_dir_obj(0, NO_MOVE);
        change_cube({ShuttleInside});
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
}}
");
                return scripts => { addShip(scripts); addHelper(scripts); };
            });
        if (!b.Store.SceneExists(ShuttleInside)) { b.Log.Add($"the saucer to Otringal left out: no scene {ShuttleInside}"); return; }
        b.Edit(ShuttleInside, $"aboard the Esmers' saucer once the moon is won: the lift-off, on to Otringal (scene {OtringalStart})", model =>
        {
            var helper = RaceTrackScenes.InvisibleHelper(model.Hero.X, model.Hero.Y, model.Hero.Z);
            return IslandBeatsA.Add(model, helper, $@"void comportement_0()
{{
    if (5 <= chapter() && {HalDone} == var_game({QHal}))
    {{
        set_dir_obj(0, NO_MOVE);
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
        change_cube({OtringalStart});
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
    // door, cells 46-47, 39-40, and a second wheel on it (actor 3's sprite and its turning, the same height over its stand): Action by it
    // works the door as the first does.
    private const int XDoorVar = 93, XWheel = 3, XStandBlock = 151, XStandFloor = 60, XStandX = 44, XStandZ = 35, XWheelFrom = 47 * 512, XWheelFromZ = 42 * 512;
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
    private const int SaucerWait = 20, BoardReach = 1800;

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
                message_obj(0, {Say(MoonLine.TwinsenNo)});
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
                message_obj(0, {Say(MoonLine.TwinsenOffer)});
                message({Say(MoonLine.MechanicThanks)});
                message_obj(0, {Say(MoonLine.TwinsenBack)});
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
    if ({HalWon} == var_game({QHal}) && {Driving} != comportement_hero())
    {{
        set_var_game({QHal}, {HalDone});
        message_obj({guard}, {Say(MoonLine.GuardBeaten)});
        message({Say(MoonLine.LyraZeelich)});
        message_obj({hal}, {Say(MoonLine.HalNumber)});
        message({Say(MoonLine.HangarGo)});
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

        b.Save();
        log.Add($"the Emerald Moon's race file: gate={QHal} {HalMended} (HAL mended), win={QHal} {HalWon}");
        return log;
    }
}
