using System.IO;

namespace LBAAssembler.Terrain;

// Twinsen's opening line (the user, 2026-11-22: "We need Twinsen's opening line goal led everywhere. If there's times he isn't explicitly
// goal led then he can just say hello and have the NPCs respond"; before, of the coil: "Twinsen's opening line follows his objective ('Do
// you know where I can get an ignition coil?') and NPCs respond accordingly; generalise to all objectives"). Whoever Twinsen talks to --
// the story's people and the game's own alike -- he speaks first: the race file's opener= lines (RACEMOD.CPP RaceMod_Opener), the first
// whose game variables all hold, his goal on that island in that chapter; none holding, a hello (opener_hello=, in turn). The person then
// says their own lines. Once a scene visit for each person and line: talked to again for the same goal, he doesn't say it again. Zoe is
// never greeted: she's at his side, and her hints answer his goal already (opener_quiet=, the companion's entity).
// The lines are at fixed ids in each island's text bank, past every story line (OpenerBase: NextId leaves them out), so the race file can
// name them whatever order the islands are built in. Each goal is said as Twinsen's statement of it, not a question: it reads the same
// to anyone, in every language (no "tu" or "vous" to choose).
internal static partial class RaceTrackStory
{
    public const int OpenerBase = 3000;
    private const int OpenerChapter = 253, OpenerAny = 32767, StatueVar = 79;

    internal enum OpenerLine
    {
        TwinsenHello, TwinsenHowsItGoing, TwinsenGoodDay,
        TwinsenDreamFunFrock,
        TwinsenSeekWizard, TwinsenSeekRaph, TwinsenSeekGloves, TwinsenBeatRaph, TwinsenToLighthouse, TwinsenToBed,
        TwinsenBaitForFelix, TwinsenWheelForLuc, TwinsenWheelFound, TwinsenEntryFee, TwinsenTownRace, TwinsenFerry,
        TwinsenTrackShut, TwinsenSeekPages, TwinsenPagesFound, TwinsenSandFilters, TwinsenFitFilters, TwinsenSeekHelmet, TwinsenDesertRace, TwinsenToMoon,
        TwinsenFixHal, TwinsenRaceHal, TwinsenToOtringal,
        TwinsenCupRace, TwinsenGridFull, TwinsenQualify, TwinsenNeedRoom, TwinsenSleep, TwinsenAskCoil, TwinsenLetter, TwinsenReply, TwinsenOtringalRace, TwinsenBlackCar,
        TwinsenSellerPicture, TwinsenFontaine, TwinsenFrancosRace, TwinsenRaceMonster, TwinsenSeekGlove, TwinsenGloveFound, TwinsenRaceMiner,
        TwinsenNeedPass, TwinsenRaceQueen, TwinsenSealCracks, TwinsenRaceDissident, TwinsenHallsDoor, TwinsenRaceSoldier, TwinsenSeekStan, TwinsenRaceStan,
        TwinsenBeatFunFrock,
    }

    // English, French, German, Spanish, Italian, Portuguese -- in OpenerLine's order
    internal static readonly string[][] OpenerLines =
    {
        new[] { "Hello!", "Bonjour !", "Hallo!", "¡Hola!", "Salve!", "Olá!" },
        new[] { "Hi! How's it going?", "Salut, ça va ?", "Hallo, wie geht's?", "¡Hola! ¿Qué tal?", "Ciao, come va?", "Olá, como vai?" },
        new[] { "Good day!", "Bien le bonjour !", "Guten Tag!", "¡Buenos días!", "Buongiorno!", "Bom dia!" },
        new[]
        {
            "I have to catch FunFrock before he reaches the Sendells!", "Je dois rattraper FunFrock avant qu'il n'atteigne les Sendell !",
            "Ich muss FunFrock einholen, bevor er die Sendells erreicht!", "¡Tengo que alcanzar a FunFrock antes de que llegue a los Sendell!",
            "Devo raggiungere FunFrock prima che arrivi dai Sendell!", "Tenho de apanhar o FunFrock antes que chegue aos Sendell!",
        },
        // ---- Citadel Island, the storm (chapter 1)
        new[]
        {
            "I'm looking for the Weather Wizard.", "Je cherche le Mage Météo.", "Ich suche den Wettermagier.", "Busco al Mago Meteo.",
            "Sto cercando il Mago Metereologo.", "Procuro o Mago do Tempo.",
        },
        new[]
        {
            "I need to find Raph at the race track.", "Je dois trouver Raph sur le circuit.", "Ich muss Raph an der Rennstrecke finden.",
            "Tengo que encontrar a Raph en el circuito.", "Devo trovare Raph sul circuito.", "Tenho de encontrar o Raph no circuito.",
        },
        new[]
        {
            "I'm looking for whoever took my driving gloves.", "Je cherche celui qui a pris mes gants de pilote.",
            "Ich suche den, der meine Fahrhandschuhe genommen hat.", "Busco a quien se llevó mis guantes de conducir.",
            "Cerco chi ha preso i miei guanti da pilota.", "Procuro quem levou as minhas luvas de condução.",
        },
        new[]
        {
            "I have to beat Raph's lap time on the storm track.", "Je dois battre le temps au tour de Raph sur le circuit de la tempête.",
            "Ich muss Raphs Rundenzeit auf der Sturmstrecke schlagen.", "Tengo que batir el tiempo por vuelta de Raph en el circuito de la tormenta.",
            "Devo battere il tempo sul giro di Raph sul circuito della tempesta.", "Tenho de bater o tempo de volta do Raph no circuito da tempestade.",
        },
        new[]
        {
            "The Weather Wizard is waiting for me at the lighthouse.", "Le Mage Météo m'attend au phare.", "Der Wettermagier wartet am Leuchtturm auf mich.",
            "El Mago Meteo me espera en el faro.", "Il Mago Metereologo mi aspetta al faro.", "O Mago do Tempo está à minha espera no farol.",
        },
        new[]
        {
            "What a day! I'm off home to bed.", "Quelle journée ! Je rentre me coucher.", "Was für ein Tag! Ich gehe nach Hause ins Bett.",
            "¡Menudo día! Me voy a casa a dormir.", "Che giornata! Vado a casa a dormire.", "Que dia! Vou para casa dormir.",
        },
        // ---- Citadel Island, the town circuit (chapter 2)
        new[]
        {
            "I've got bait for Felix's old tyre.", "J'ai de l'appât pour le vieux pneu de Felix.", "Ich habe Köder für Felix' alten Reifen.",
            "Tengo cebo para el neumático viejo de Felix.", "Ho l'esca per il vecchio pneumatico di Felix.", "Tenho isco para o pneu velho do Felix.",
        },
        new[]
        {
            "Luc needs a new wheel for his barrel car.", "Luc a besoin d'une roue pour sa voiture-tonneau.", "Luc braucht ein neues Rad für seinen Faßwagen.",
            "Luc necesita una rueda para su coche-barril.", "Luc ha bisogno di una ruota per la sua macchina-botte.", "O Luc precisa de uma roda para o carro-barril dele.",
        },
        new[]
        {
            "I've found a wheel for Luc's car!", "J'ai trouvé une roue pour la voiture de Luc !", "Ich habe ein Rad für Lucs Wagen gefunden!",
            "¡He encontrado una rueda para el coche de Luc!", "Ho trovato una ruota per la macchina di Luc!", "Encontrei uma roda para o carro do Luc!",
        },
        new[]
        {
            "I need twenty kashes for the race's entry fee.", "Il me faut vingt kashes pour l'inscription à la course.", "Ich brauche zwanzig Kashes für die Startgebühr.",
            "Necesito veinte kashes para la inscripción de la carrera.", "Mi servono venti kash per l'iscrizione alla gara.", "Preciso de vinte kashes para a inscrição na corrida.",
        },
        new[]
        {
            "Time to win the race round the town!", "C'est l'heure de gagner la course autour de la ville !", "Zeit, das Rennen durch die Stadt zu gewinnen!",
            "¡Es hora de ganar la carrera por la ciudad!", "È ora di vincere la gara in città!", "Está na hora de ganhar a corrida pela cidade!",
        },
        new[]
        {
            "I've a ferry to catch to the White Leaf Desert.", "J'ai un ferry à prendre pour le Désert de la Feuille Blanche.", "Ich muss die Fähre zur Weißblattwüste erwischen.",
            "Tengo que coger el ferry al Desierto de la Hoja Blanca.", "Devo prendere il traghetto per il Deserto della Foglia Bianca.", "Tenho de apanhar o ferry para o Deserto da Folha Branca.",
        },
        // ---- the White Leaf Desert (chapter 3)
        new[]
        {
            "I need the Dean to open the race track again.", "Il faut que le Recteur rouvre le circuit.", "Der Rektor muss die Rennstrecke wieder öffnen.",
            "Necesito que el rector vuelva a abrir el circuito.", "Il Rettore deve riaprire il circuito.", "Preciso que o Reitor volte a abrir o circuito.",
        },
        new[]
        {
            "I'm looking for the Dean's lost exam pages.", "Je cherche les feuilles d'examen perdues du Recteur.", "Ich suche die verlorenen Prüfungsblätter des Rektors.",
            "Busco las hojas de examen perdidas del rector.", "Cerco le pagine d'esame perdute del Rettore.", "Procuro as páginas de exame perdidas do Reitor.",
        },
        new[]
        {
            "I've got all three of the Dean's pages!", "J'ai les trois feuilles du Recteur !", "Ich habe alle drei Blätter des Rektors!",
            "¡Tengo las tres hojas del rector!", "Ho tutte e tre le pagine del Rettore!", "Tenho as três páginas do Reitor!",
        },
        new[]
        {
            "I need sand filters for my car's engine.", "Il me faut des filtres à sable pour le moteur de ma voiture.", "Ich brauche Sandfilter für meinen Motor.",
            "Necesito filtros de arena para el motor de mi coche.", "Mi servono dei filtri per la sabbia per il motore.", "Preciso de filtros de areia para o motor do carro.",
        },
        new[]
        {
            "These sand filters need fitting to my car.", "Ces filtres à sable doivent être montés sur ma voiture.", "Diese Sandfilter müssen noch in meinen Wagen.",
            "Hay que montar estos filtros de arena en mi coche.", "Questi filtri per la sabbia vanno montati sulla mia macchina.", "Estes filtros de areia têm de ser montados no carro.",
        },
        new[]
        {
            "I'm looking for a racing helmet.", "Je cherche un casque de course.", "Ich suche einen Rennhelm.", "Busco un casco de carreras.",
            "Cerco un casco da corsa.", "Procuro um capacete de corrida.",
        },
        new[]
        {
            "Time to beat the Desert champion!", "C'est l'heure de battre le champion du Désert !", "Zeit, den Wüstenchampion zu schlagen!",
            "¡Es hora de vencer al campeón del Desierto!", "È ora di battere il campione del Deserto!", "Está na hora de vencer o campeão do Deserto!",
        },
        new[]
        {
            "Next stop: the Emerald Moon!", "Prochaine étape : la Lune d'Emeraude !", "Nächster Halt: der Smaragd-Mond!",
            "¡Próxima parada: la Luna de Esmeralda!", "Prossima tappa: la Luna di Smeraldo!", "Próxima paragem: a Lua de Esmeralda!",
        },
        // ---- the Emerald Moon (chapter 4)
        new[]
        {
            "I need to get HAL working again.", "Il faut que je fasse remarcher HAL.", "Ich muss HAL wieder zum Laufen bringen.",
            "Tengo que hacer que HAL vuelva a funcionar.", "Devo far funzionare di nuovo HAL.", "Tenho de pôr o HAL a funcionar outra vez.",
        },
        new[]
        {
            "Time to race HAL!", "C'est l'heure d'affronter HAL !", "Zeit, gegen HAL anzutreten!", "¡Es hora de correr contra HAL!",
            "È ora di sfidare HAL!", "Está na hora de correr contra o HAL!",
        },
        new[]
        {
            "Next stop: Otringal, on Zeelich!", "Prochaine étape : Otringal, sur Zeelich !", "Nächster Halt: Otringal auf Zeelich!",
            "¡Próxima parada: Otringal, en Zeelich!", "Prossima tappa: Otringal, su Zeelich!", "Próxima paragem: Otringal, em Zeelich!",
        },
        // ---- Otringal (chapter 5)
        new[]
        {
            "I've come to race in the Cup here.", "Je suis venu courir la Coupe ici.", "Ich bin hier, um im Cup zu fahren.",
            "He venido a correr la Copa aquí.", "Sono venuto a correre la Coppa qui.", "Vim correr a Taça aqui.",
        },
        new[]
        {
            "The race is full. I need someone to get me on the grid.", "La course est complète. Il me faut quelqu'un pour me faire entrer sur la grille.",
            "Das Rennen ist voll. Ich brauche jemanden, der mich in die Startaufstellung bringt.", "La carrera está completa. Necesito que alguien me meta en la parrilla.",
            "La gara è al completo. Mi serve qualcuno che mi metta in griglia.", "A corrida está cheia. Preciso de alguém que me ponha na grelha.",
        },
        new[]
        {
            "I've a qualifying lap to drive today.", "J'ai un tour de qualification à faire aujourd'hui.", "Ich muss heute eine Qualifikationsrunde fahren.",
            "Hoy tengo que dar una vuelta de clasificación.", "Oggi devo fare un giro di qualifica.", "Hoje tenho uma volta de qualificação para fazer.",
        },
        new[]
        {
            "I need a room for the night.", "Il me faut une chambre pour la nuit.", "Ich brauche ein Zimmer für die Nacht.",
            "Necesito una habitación para esta noche.", "Mi serve una camera per la notte.", "Preciso de um quarto para esta noite.",
        },
        new[]
        {
            "Time I got some sleep before race day.", "Il est temps que je dorme avant le jour de la course.", "Zeit, vor dem Renntag zu schlafen.",
            "Ya es hora de dormir antes del día de la carrera.", "È ora di dormire prima del giorno della gara.", "Está na hora de dormir antes do dia da corrida.",
        },
        new[]
        {
            "Do you know where I can get an ignition coil?", "Vous savez où je pourrais trouver une bobine d'allumage ?", "Wissen Sie, wo ich eine Zündspule bekomme?",
            "¿Sabe dónde puedo conseguir una bobina de encendido?", "Sa dove posso trovare una bobina d'accensione?", "Sabe onde posso arranjar uma bobina de ignição?",
        },
        new[]
        {
            "I've a love letter for the singer at Rick's bar.", "J'ai une lettre d'amour pour la chanteuse du bar de Rick.", "Ich habe einen Liebesbrief für die Sängerin in Ricks Bar.",
            "Tengo una carta de amor para la cantante del bar de Rick.", "Ho una lettera d'amore per la cantante del bar di Rick.", "Tenho uma carta de amor para a cantora do bar do Rick.",
        },
        new[]
        {
            "I'm taking the singer's reply back to the gazogem seller.", "Je rapporte la réponse de la chanteuse au vendeur de gazogem.",
            "Ich bringe die Antwort der Sängerin zum Gazogem-Verkäufer.", "Llevo la respuesta de la cantante al vendedor de gazogem.",
            "Porto la risposta della cantante al venditore di gazogem.", "Levo a resposta da cantora ao vendedor de gazogem.",
        },
        new[]
        {
            "Time to win the Otringal race!", "C'est l'heure de gagner la course d'Otringal !", "Zeit, das Rennen von Otringal zu gewinnen!",
            "¡Es hora de ganar la carrera de Otringal!", "È ora di vincere la gara di Otringal!", "Está na hora de ganhar a corrida de Otringal!",
        },
        new[]
        {
            "I'm on the trail of that black car.", "Je suis la piste de cette voiture noire.", "Ich bin diesem schwarzen Wagen auf der Spur.",
            "Sigo la pista de ese coche negro.", "Sono sulle tracce di quella macchina nera.", "Ando atrás daquele carro preto.",
        },
        // ---- the later islands (chapters 6-14)
        new[]
        {
            "They say the souvenir seller has a picture of that black car.", "On dit que le vendeur de souvenirs a une photo de cette voiture noire.",
            "Der Souvenirverkäufer soll ein Bild von diesem schwarzen Wagen haben.", "Dicen que el vendedor de recuerdos tiene una foto de ese coche negro.",
            "Dicono che il venditore di souvenir abbia una foto di quella macchina nera.", "Dizem que o vendedor de lembranças tem uma foto daquele carro preto.",
        },
        new[]
        {
            "I need De La Fontaine to let the race go ahead.", "Il faut que De la Fontaine laisse la course avoir lieu.", "Brunnenschein muss das Rennen stattfinden lassen.",
            "Necesito que De la Fontaine deje que se corra la carrera.", "De la Fontaine deve lasciare che la gara si faccia.", "Preciso que o De La Fontaine deixe a corrida avançar.",
        },
        new[]
        {
            "Time to beat De La Fontaine's tanker!", "C'est l'heure de battre le camion-citerne de De la Fontaine !", "Zeit, Brunnenscheins Tankwagen zu schlagen!",
            "¡Es hora de vencer al camión cisterna de De la Fontaine!", "È ora di battere l'autocisterna di De la Fontaine!", "Está na hora de vencer o camião-cisterna do De La Fontaine!",
        },
        new[]
        {
            "Time to race the monkey monster!", "C'est l'heure d'affronter le monstre singe !", "Zeit, gegen das Affenmonster anzutreten!",
            "¡Es hora de correr contra el monstruo mono!", "È ora di sfidare il mostro scimmia!", "Está na hora de correr contra o monstro macaco!",
        },
        new[]
        {
            "I'm looking for the miner's lost glove.", "Je cherche le gant perdu du mineur.", "Ich suche den verlorenen Handschuh des Bergmanns.",
            "Busco el guante perdido del minero.", "Cerco il guanto perduto del minatore.", "Procuro a luva perdida do mineiro.",
        },
        new[]
        {
            "I've found the miner's glove!", "J'ai trouvé le gant du mineur !", "Ich habe den Handschuh des Bergmanns gefunden!",
            "¡He encontrado el guante del minero!", "Ho trovato il guanto del minatore!", "Encontrei a luva do mineiro!",
        },
        new[]
        {
            "Time to beat the miner's bulldozer!", "C'est l'heure de battre le bulldozer du mineur !", "Zeit, die Planierraupe des Bergmanns zu schlagen!",
            "¡Es hora de vencer a la excavadora del minero!", "È ora di battere il bulldozer del minatore!", "Está na hora de vencer o buldózer do mineiro!",
        },
        new[]
        {
            "I need a pass for the Queen's race.", "Il me faut un laissez-passer pour la course de la Reine.", "Ich brauche einen Passierschein für das Rennen der Königin.",
            "Necesito un pase para la carrera de la Reina.", "Mi serve un lasciapassare per la gara della Regina.", "Preciso de um passe para a corrida da Rainha.",
        },
        new[]
        {
            "Time to race the Queen of the Mosquibees!", "C'est l'heure d'affronter la Reine des Mosquibees !", "Zeit, gegen die Königin der Mosquibees anzutreten!",
            "¡Es hora de correr contra la Reina de los Mosquibees!", "È ora di sfidare la Regina dei Mosquibee!", "Está na hora de correr contra a Rainha dos Mosquibees!",
        },
        new[]
        {
            "The cracks in the hide-out need sealing before the race.", "Il faut boucher les fissures de la cachette avant la course.",
            "Die Risse im Versteck müssen vor dem Rennen abgedichtet werden.", "Hay que sellar las grietas del escondite antes de la carrera.",
            "Bisogna sigillare le crepe del nascondiglio prima della gara.", "É preciso tapar as fendas do esconderijo antes da corrida.",
        },
        new[]
        {
            "Time to race the dissident!", "C'est l'heure d'affronter le dissident !", "Zeit, gegen den Dissidenten anzutreten!",
            "¡Es hora de correr contra el disidente!", "È ora di sfidare il dissidente!", "Está na hora de correr contra o dissidente!",
        },
        new[]
        {
            "I need a way through the door to the halls.", "Il me faut un moyen de passer la porte des salles.", "Ich muss durch die Tür zu den Hallen kommen.",
            "Necesito una forma de cruzar la puerta de las salas.", "Mi serve un modo per passare la porta delle sale.", "Preciso de passar a porta das salas.",
        },
        new[]
        {
            "Time to beat the Emperor's soldier!", "C'est l'heure de battre le soldat de l'Empereur !", "Zeit, den Soldaten des Kaisers zu schlagen!",
            "¡Es hora de vencer al soldado del Emperador!", "È ora di battere il soldato dell'Imperatore!", "Está na hora de vencer o soldado do Imperador!",
        },
        new[]
        {
            "I need to find a way through the palace to Stan.", "Il me faut trouver un chemin jusqu'à Stan dans le palais.", "Ich muss durch den Palast zu Stan kommen.",
            "Tengo que encontrar el camino hasta Stan en el palacio.", "Devo trovare la strada fino a Stan nel palazzo.", "Tenho de encontrar o caminho até ao Stan no palácio.",
        },
        new[]
        {
            "Time to race Stan to the window!", "C'est l'heure de faire la course avec Stan jusqu'à la fenêtre !", "Zeit für das Rennen gegen Stan bis zum Fenster!",
            "¡Es hora de echar una carrera a Stan hasta la ventana!", "È ora di sfidare Stan fino alla finestra!", "Está na hora de correr contra o Stan até à janela!",
        },
        new[]
        {
            "Time to beat FunFrock, once and for all!", "C'est l'heure de battre FunFrock, une fois pour toutes !", "Zeit, FunFrock ein für alle Mal zu schlagen!",
            "¡Es hora de vencer a FunFrock de una vez por todas!", "È ora di battere FunFrock, una volta per tutte!", "Está na hora de vencer o FunFrock de uma vez por todas!",
        },
    };

    private static readonly OpenerLine[] OpenerHellos = { OpenerLine.TwinsenHello, OpenerLine.TwinsenHowsItGoing, OpenerLine.TwinsenGoodDay };

    // A goal: the island's text bank (its island byte), Twinsen's line, and the game variables it wants, each within its bounds
    private readonly record struct OpenerGoal(int Island, OpenerLine Line, (int Var, int Lo, int Hi)[] When);
    private static (int, int, int) Is(int var, int value) => (var, value, value);
    private static (int, int, int) In(int var, int lo, int hi) => (var, lo, hi);
    private static (int, int, int) Chapter(int chapter) => (OpenerChapter, chapter, chapter);

    // In each island's order: the first that holds is said
    private static OpenerGoal[] OpenerGoals() => new OpenerGoal[]
    {
        // the dream
        new(RaceTrackIsland.Polar.IslandByte, OpenerLine.TwinsenDreamFunFrock, new[] { Chapter(0) }),
        // the storm: the Wizard Zoe sent him to, Raph at the track, the gloves, Raph's time, the lighthouse, bed
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenSeekWizard, new[] { Chapter(1), In(StormPlot, 0, 1), Is(RaphHere, 0) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenSeekRaph, new[] { Chapter(1), Is(RaphHere, 0) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenSeekGloves, new[] { Chapter(1), Is(GlovesSlot, 0) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenBeatRaph, new[] { Chapter(1), Is(BeatVar, 0) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenToLighthouse, new[] { Chapter(1), In(KeeperPlot, 0, KeeperSpell - 1) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenToBed, new[] { Chapter(1) }),
        // the town circuit: Luc's wheel (Felix's tyre for bait), the entry fee, the race, the ferry
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenBaitForFelix, new[] { Chapter(2), In(QLucCar, 0, WheelFound - 1), Is(QBait, BaitHeld) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenWheelForLuc, new[] { Chapter(2), In(QLucCar, 0, WheelFound - 1) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenWheelFound, new[] { Chapter(2), Is(QLucCar, WheelFound) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenEntryFee, new[] { Chapter(2), Is(EntryPaid, 0), Is(WonVar, 0) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenTownRace, new[] { Chapter(2), Is(WonVar, 0) }),
        // (the race won, the ferry to the Desert next: a hello -- "I've a ferry to catch to the White Leaf Desert" told no one anything; the
        // user, 2026-11-22, "In this instance we can just have Twinsen say hello, as it's clear where he's heading". TwinsenFerry's line kept
        // in the bank, unsaid, so the lines after it keep their ids)
        // the Desert: the Dean, his pages, the filters, the helmet, the race; won (chapter 4 there), the moon
        new(RaceTrackIsland.Desert.IslandByte, OpenerLine.TwinsenTrackShut, new[] { Chapter(3), In(QPages, 0, 1), In(PagesFoundVar, 0, 6) }),
        new(RaceTrackIsland.Desert.IslandByte, OpenerLine.TwinsenSeekPages, new[] { Chapter(3), In(QPages, 2, PagesDiploma - 1), In(PagesFoundVar, 0, 6) }),
        new(RaceTrackIsland.Desert.IslandByte, OpenerLine.TwinsenPagesFound, new[] { Chapter(3), In(QPages, 0, PagesDiploma - 1) }),
        new(RaceTrackIsland.Desert.IslandByte, OpenerLine.TwinsenSandFilters, new[] { Chapter(3), Is(QFilters, 0) }),
        new(RaceTrackIsland.Desert.IslandByte, OpenerLine.TwinsenFitFilters, new[] { Chapter(3), Is(QFilters, FiltersGiven) }),
        new(RaceTrackIsland.Desert.IslandByte, OpenerLine.TwinsenSeekHelmet, new[] { Chapter(3), In(Citadel.CitadelOveralls.GlovesSlot, 0, Citadel.CitadelOveralls.GearBoth - 1) }),
        new(RaceTrackIsland.Desert.IslandByte, OpenerLine.TwinsenDesertRace, new[] { Chapter(3), In(QPages, 0, PagesWon - 1) }),
        new(RaceTrackIsland.Desert.IslandByte, OpenerLine.TwinsenToMoon, new[] { In(OpenerChapter, 3, 4) }),
        // the Emerald Moon: HAL, his race, then Otringal
        new(RaceTrackIsland.Emerald.IslandByte, OpenerLine.TwinsenFixHal, new[] { Chapter(4), In(QHal, 0, HalMended - 1) }),
        new(RaceTrackIsland.Emerald.IslandByte, OpenerLine.TwinsenRaceHal, new[] { Chapter(4), Is(QHal, HalMended) }),
        new(RaceTrackIsland.Emerald.IslandByte, OpenerLine.TwinsenToOtringal, new[] { Chapter(4) }),
        // Otringal: the Cup's race, full, the bribe, qualifying, the night, the coil, the race; won, the black car
        new(RaceTrackIsland.Otringal.IslandByte, OpenerLine.TwinsenCupRace, new[] { Chapter(5), Is(QFee, 0) }),
        new(RaceTrackIsland.Otringal.IslandByte, OpenerLine.TwinsenGridFull, new[] { Chapter(5), Is(QFee, FeeFull) }),
        new(RaceTrackIsland.Otringal.IslandByte, OpenerLine.TwinsenQualify, new[] { Chapter(5), Is(QFee, FeePaid), Is(OtringalDay, 0) }),
        new(RaceTrackIsland.Otringal.IslandByte, OpenerLine.TwinsenNeedRoom, new[] { Chapter(5), Is(QFee, FeePaid), Is(OtringalDay, DayQualified), In(QNight, 0, NightClassic - 1) }),
        new(RaceTrackIsland.Otringal.IslandByte, OpenerLine.TwinsenSleep, new[] { Chapter(5), Is(QFee, FeePaid), Is(OtringalDay, DayQualified) }),
        new(RaceTrackIsland.Otringal.IslandByte, OpenerLine.TwinsenAskCoil, new[] { Chapter(5), Is(QFee, FeePaid), Is(OtringalDay, DayRaceDay), In(QCoil, 0, CoilTold) }),
        new(RaceTrackIsland.Otringal.IslandByte, OpenerLine.TwinsenLetter, new[] { Chapter(5), Is(QFee, FeePaid), Is(OtringalDay, DayRaceDay), Is(QCoil, CoilLetter) }),
        new(RaceTrackIsland.Otringal.IslandByte, OpenerLine.TwinsenReply, new[] { Chapter(5), Is(QFee, FeePaid), Is(OtringalDay, DayRaceDay), Is(QCoil, CoilAnswer) }),
        new(RaceTrackIsland.Otringal.IslandByte, OpenerLine.TwinsenOtringalRace, new[] { Chapter(5), Is(QFee, FeePaid) }),
        new(RaceTrackIsland.Otringal.IslandByte, OpenerLine.TwinsenBlackCar, new[] { Chapter(5) }),
        // the lava lake (Celebration Island), the black car still
        new(RaceTrackIsland.CelebrationLava.IslandByte, OpenerLine.TwinsenSellerPicture, new[] { Chapter(6), Is(SellerBeaten, 0) }),
        new(RaceTrackIsland.CelebrationLava.IslandByte, OpenerLine.TwinsenBlackCar, new[] { Chapter(6) }),
        // the Island of the Francos: De La Fontaine's fuel
        new(RaceTrackIsland.Knartas.IslandByte, OpenerLine.TwinsenFontaine, new[] { Chapter(7), In(QFuel, 0, FuelOpen - 1) }),
        new(RaceTrackIsland.Knartas.IslandByte, OpenerLine.TwinsenFrancosRace, new[] { Chapter(7), Is(QFuel, FuelOpen) }),
        new(RaceTrackIsland.Knartas.IslandByte, OpenerLine.TwinsenBlackCar, new[] { Chapter(7) }),
        // the Elevator Platform (a win moves the chapter on)
        new(RaceTrackIsland.Elevator.IslandByte, OpenerLine.TwinsenRaceMonster, new[] { Chapter(8) }),
        // Wannies Island: the miner's glove
        new(RaceTrackIsland.Wannies.IslandByte, OpenerLine.TwinsenSeekGlove, new[] { Chapter(9), In(QGlove, 0, GloveFound - 1) }),
        new(RaceTrackIsland.Wannies.IslandByte, OpenerLine.TwinsenGloveFound, new[] { Chapter(9), Is(QGlove, GloveFound) }),
        new(RaceTrackIsland.Wannies.IslandByte, OpenerLine.TwinsenRaceMiner, new[] { Chapter(9) }),
        // Mosquibees Island: the pass, the Queen
        new(RaceTrackIsland.Mosquibe.IslandByte, OpenerLine.TwinsenNeedPass, new[] { Chapter(10), In(QPass, 0, RingShown - 1) }),
        new(RaceTrackIsland.Mosquibe.IslandByte, OpenerLine.TwinsenRaceQueen, new[] { Chapter(10) }),
        // the Island under Celebration: the cracks, the dissident
        new(RaceTrackIsland.Volcano.IslandByte, OpenerLine.TwinsenSealCracks, new[] { Chapter(11), In(QSlabs, 0, SlabsSealed - 1) }),
        new(RaceTrackIsland.Volcano.IslandByte, OpenerLine.TwinsenRaceDissident, new[] { Chapter(11), Is(QSlabs, SlabsSealed) }),
        // Island CX: the door to the halls, the Emperor's soldier
        new(RaceTrackIsland.CxTower.IslandByte, OpenerLine.TwinsenHallsDoor, new[] { Chapter(12), In(QCxKey, 0, KeyTraded - 1) }),
        new(RaceTrackIsland.CxTower.IslandByte, OpenerLine.TwinsenRaceSoldier, new[] { Chapter(12), Is(QCxKey, KeyTraded) }),
        // the Emperor's palace (island 4, Otringal's bank): Stan, his sprint
        new(RaceTrackIsland.Palace.IslandByte, OpenerLine.TwinsenSeekStan, new[] { Chapter(13), In(QEmperor, 0, StanReady - 1) }),
        new(RaceTrackIsland.Palace.IslandByte, OpenerLine.TwinsenRaceStan, new[] { Chapter(13), Is(StatueVar, 0) }),
        // the final, round the statue (Celebration Island's bank)
        new(RaceTrackIsland.Celebration.IslandByte, OpenerLine.TwinsenBeatFunFrock, new[] { Chapter(14), In(FinalStep, 0, FinalWon - 1) }),
    };

    // The race file's keys for an island (RaceCarEngineFile.EngineFile): its goals in the story, the hellos, and Zoe never greeted
    internal static string OpenerKeys(RaceTrackIsland island, bool story)
    {
        var text = new System.Text.StringBuilder();
        if (story)
            foreach (var goal in OpenerGoals().Where(g => g.Island == island.IslandByte))
                text.Append($"opener={OpenerBase + (int)goal.Line}{string.Concat(goal.When.Select(w => $" {w.Var} {w.Lo} {w.Hi}"))}\n");
        text.Append($"opener_hello={string.Join(' ', OpenerHellos.Select(h => OpenerBase + (int)h))}\n");
        text.Append($"opener_quiet={CompanionEntityId}\n");
        return text.ToString();
    }

    // The lines into the island's text bank, every language (RaceTrackService.Story, each island's track): the hellos and the island's goals
    // at OpenerBase + their number -- the bank's earlier ones there first taken out, so a build made again puts the same in
    internal static List<string> OpenerTexts(string gameDirectory, RaceTrackIsland island)
    {
        var log = new List<string>();
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        if (!File.Exists(textPath)) return log;
        var lines = OpenerHellos.Concat(OpenerGoals().Where(g => g.Island == island.IslandByte).Select(g => g.Line)).Distinct().OrderBy(l => (int)l).ToList();
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        var file = island.IslandByte + 3;
        var hqr = File.ReadAllBytes(textPath);
        for (var lang = 0; lang < languages; lang++)
        {
            var bank = Lba2TextBank.Load(text, lang, file);
            bank.Texts.RemoveAll(t => t.Id >= OpenerBase);
            foreach (var line in lines)
                bank.Texts.Add(new Lba2TextBank.Text { Id = OpenerBase + (int)line, Attribute = Lba2TextBank.NormalAttribute, Bytes = Dos.GetBytes(OpenerLines[(int)line][lang < OpenerLines[(int)line].Length ? lang : 0]) });
            hqr = bank.WriteInto(hqr);
        }
        File.WriteAllBytes(textPath, hqr);
        log.Add($"Twinsen's opening lines: {lines.Count} in file {file} ({island.Name}), texts {OpenerBase}+, in all {languages} languages");
        return log;
    }
}
