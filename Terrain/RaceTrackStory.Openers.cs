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
// name them whatever order the islands are built in. Each goal is said as a question to the one he talks to (the user, 2026-11-23,
// "Twinsen should be using questions, not statements"), in the polite forms a young man uses to a stranger -- vous, Sie, usted, Lei.
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

    // English, French, German, Spanish, Italian, Portuguese -- in OpenerLine's order; each goal a question to whoever he talks to, in the
    // polite forms (vous, Sie, usted, Lei) a young man uses to a stranger (the user, 2026-11-23: "Twinsen should be using questions, not
    // statements i.e. 'Hi, do you know where I can find a car tyre?'")
    internal static readonly string[][] OpenerLines =
    {
        new[] { "Hello!", "Bonjour !", "Hallo!", "¡Hola!", "Salve!", "Olá!" },
        new[] { "Hi! How's it going?", "Salut, ça va ?", "Hallo, wie geht's?", "¡Hola! ¿Qué tal?", "Ciao, come va?", "Olá, como vai?" },
        new[] { "Good day!", "Bien le bonjour !", "Guten Tag!", "¡Buenos días!", "Buongiorno!", "Bom dia!" },
        new[] { "Have you seen FunFrock? I have to catch him before he reaches the Sendells!", "Avez-vous vu FunFrock ? Je dois le rattraper avant qu'il n'atteigne les Sendell !", "Haben Sie FunFrock gesehen? Ich muss ihn einholen, bevor er die Sendells erreicht!", "¿Ha visto a FunFrock? ¡Tengo que alcanzarlo antes de que llegue a los Sendell!", "Ha visto FunFrock? Devo raggiungerlo prima che arrivi dai Sendell!", "Viu o FunFrock? Tenho de o apanhar antes que chegue aos Sendell!" },
        new[] { "Hi, do you know where I can find the Weather Wizard?", "Bonjour, savez-vous où je peux trouver le Mage Météo ?", "Hallo, wissen Sie, wo ich den Wettermagier finde?", "Hola, ¿sabe dónde puedo encontrar al Mago Meteo?", "Salve, sa dove posso trovare il Mago Metereologo?", "Olá, sabe onde posso encontrar o Mago do Tempo?" },
        new[] { "Hi, have you seen Raph? He should be at the race track.", "Bonjour, avez-vous vu Raph ? Il devrait être sur le circuit.", "Hallo, haben Sie Raph gesehen? Er müsste an der Rennstrecke sein.", "Hola, ¿ha visto a Raph? Debería estar en el circuito.", "Salve, ha visto Raph? Dovrebbe essere sul circuito.", "Olá, viu o Raph? Deve estar no circuito." },
        new[] { "Hi, have you seen who took my driving gloves?", "Bonjour, avez-vous vu qui a pris mes gants de conduite ?", "Hallo, haben Sie gesehen, wer meine Fahrhandschuhe genommen hat?", "Hola, ¿ha visto quién se llevó mis guantes de conducir?", "Salve, ha visto chi ha preso i miei guanti da pilota?", "Olá, viu quem levou as minhas luvas de condução?" },
        new[] { "Hi, any tips for beating Raph's lap time on the storm track?", "Bonjour, un conseil pour battre le temps au tour de Raph sur le circuit de la tempête ?", "Hallo, haben Sie einen Tipp, wie ich Raphs Rundenzeit auf der Sturmstrecke schlage?", "Hola, ¿algún consejo para batir el tiempo por vuelta de Raph en el circuito de la tormenta?", "Salve, qualche consiglio per battere il tempo sul giro di Raph sul circuito della tempesta?", "Olá, algum conselho para bater o tempo de volta do Raph no circuito da tempestade?" },
        new[] { "Hi, is this the way to the lighthouse? The Weather Wizard is waiting for me.", "Bonjour, c'est bien par ici, le phare ? Le Mage Météo m'attend.", "Hallo, geht es hier zum Leuchtturm? Der Wettermagier wartet auf mich.", "Hola, ¿se va por aquí al faro? El Mago Meteo me espera.", "Salve, è da questa parte il faro? Il Mago Metereologo mi aspetta.", "Olá, é por aqui o farol? O Mago do Tempo está à minha espera." },
        new[] { "Is it that late already? I'd better get home to bed.", "Il est déjà si tard ? Je ferais mieux de rentrer me coucher.", "Ist es schon so spät? Ich gehe besser nach Hause ins Bett.", "¿Ya es tan tarde? Mejor me voy a casa a dormir.", "È già così tardi? Farei meglio ad andare a casa a dormire.", "Já é assim tão tarde? É melhor ir para casa dormir." },
        new[] { "Hi, do you know where Felix is? I've got bait for his old tyre.", "Bonjour, savez-vous où est Felix ? J'ai de l'appât pour son vieux pneu.", "Hallo, wissen Sie, wo Felix ist? Ich habe Köder für seinen alten Reifen.", "Hola, ¿sabe dónde está Felix? Tengo cebo para su neumático viejo.", "Salve, sa dov'è Felix? Ho l'esca per il suo vecchio pneumatico.", "Olá, sabe onde está o Felix? Tenho isco para o pneu velho dele." },
        new[] { "Hi, do you know where I can find a car tyre?", "Bonjour, savez-vous où je peux trouver un pneu de voiture ?", "Hallo, wissen Sie, wo ich einen Autoreifen finde?", "Hola, ¿sabe dónde puedo encontrar un neumático de coche?", "Salve, sa dove posso trovare uno pneumatico per auto?", "Olá, sabe onde posso encontrar um pneu de carro?" },
        new[] { "Hi, have you seen Luc? I've got a wheel for his barrel car!", "Bonjour, avez-vous vu Luc ? J'ai une roue pour sa voiture-tonneau !", "Hallo, haben Sie Luc gesehen? Ich habe ein Rad für seinen Faßwagen!", "Hola, ¿ha visto a Luc? ¡Tengo una rueda para su coche-barril!", "Salve, ha visto Luc? Ho una ruota per la sua macchina-botte!", "Olá, viu o Luc? Tenho uma roda para o carro-barril dele!" },
        new[] { "Hi, do you know how much the race entry is?", "Bonjour, savez-vous combien coûte l'inscription à la course ?", "Hallo, wissen Sie, was die Startgebühr kostet?", "Hola, ¿sabe cuánto cuesta la inscripción de la carrera?", "Salve, sa quanto costa l'iscrizione alla gara?", "Olá, sabe quanto custa a inscrição na corrida?" },
        new[] { "Hi! When does the race round the town start?", "Bonjour ! Quand commence la course autour de la ville ?", "Hallo! Wann beginnt das Rennen durch die Stadt?", "¡Hola! ¿Cuándo empieza la carrera por la ciudad?", "Salve! Quando comincia la gara in città?", "Olá! Quando começa a corrida pela cidade?" },
        new[] { "Hi, when's the next ferry to the White Leaf Desert?", "Bonjour, quand part le prochain ferry pour le Désert de la Feuille Blanche ?", "Hallo, wann fährt die nächste Fähre zur Weißblattwüste?", "Hola, ¿cuándo sale el próximo ferry al Desierto de la Hoja Blanca?", "Salve, quando parte il prossimo traghetto per il Deserto della Foglia Bianca?", "Olá, quando sai o próximo ferry para o Deserto da Folha Branca?" },
        new[] { "Hi, do you know how I can get the Dean to open the race track again?", "Bonjour, savez-vous comment convaincre le Recteur de rouvrir le circuit ?", "Hallo, wissen Sie, wie ich den Rektor dazu bringe, die Rennstrecke wieder zu öffnen?", "Hola, ¿sabe cómo puedo convencer al rector de que vuelva a abrir el circuito?", "Salve, sa come posso convincere il Rettore a riaprire il circuito?", "Olá, sabe como posso convencer o Reitor a reabrir o circuito?" },
        new[] { "Hi, have you seen any of the Dean's lost exam pages?", "Bonjour, avez-vous vu des feuilles d'examen perdues du Recteur ?", "Hallo, haben Sie irgendwo die verlorenen Prüfungsblätter des Rektors gesehen?", "Hola, ¿ha visto alguna de las hojas de examen perdidas del rector?", "Salve, ha visto qualcuna delle pagine d'esame perdute del Rettore?", "Olá, viu alguma das páginas de exame perdidas do Reitor?" },
        new[] { "Hi, is the Dean about? I've got all three of his pages!", "Bonjour, le Recteur est-il là ? J'ai ses trois feuilles !", "Hallo, ist der Rektor da? Ich habe alle drei Blätter!", "Hola, ¿está el rector? ¡Tengo sus tres hojas!", "Salve, c'è il Rettore? Ho tutte e tre le sue pagine!", "Olá, o Reitor está por aí? Tenho as três páginas dele!" },
        new[] { "Hi, do you know where I can get sand filters for my car's engine?", "Bonjour, savez-vous où trouver des filtres à sable pour le moteur de ma voiture ?", "Hallo, wissen Sie, wo ich Sandfilter für meinen Motor bekomme?", "Hola, ¿sabe dónde puedo conseguir filtros de arena para el motor de mi coche?", "Salve, sa dove posso trovare dei filtri per la sabbia per il motore?", "Olá, sabe onde posso arranjar filtros de areia para o motor do carro?" },
        new[] { "Hi, could you help me fit these sand filters to my car?", "Bonjour, pourriez-vous m'aider à monter ces filtres à sable sur ma voiture ?", "Hallo, könnten Sie mir helfen, diese Sandfilter in meinen Wagen einzubauen?", "Hola, ¿podría ayudarme a montar estos filtros de arena en mi coche?", "Salve, potrebbe aiutarmi a montare questi filtri per la sabbia sulla mia macchina?", "Olá, podia ajudar-me a montar estes filtros de areia no carro?" },
        new[] { "Hi, do you know where I can find a racing helmet?", "Bonjour, savez-vous où je peux trouver un casque de course ?", "Hallo, wissen Sie, wo ich einen Rennhelm finde?", "Hola, ¿sabe dónde puedo encontrar un casco de carreras?", "Salve, sa dove posso trovare un casco da corsa?", "Olá, sabe onde posso encontrar um capacete de corrida?" },
        new[] { "Hi! When does the race against the Desert champion start?", "Bonjour ! Quand commence la course contre le champion du Désert ?", "Hallo! Wann beginnt das Rennen gegen den Wüstenchampion?", "¡Hola! ¿Cuándo empieza la carrera contra el campeón del Desierto?", "Salve! Quando comincia la gara contro il campione del Deserto?", "Olá! Quando começa a corrida contra o campeão do Deserto?" },
        new[] { "Hi! How do I get to the Emerald Moon from here?", "Bonjour ! Comment aller sur la Lune d'Emeraude d'ici ?", "Hallo! Wie komme ich von hier zum Smaragd-Mond?", "¡Hola! ¿Cómo llego desde aquí a la Luna de Esmeralda?", "Salve! Come si arriva da qui alla Luna di Smeraldo?", "Olá! Como chego daqui à Lua de Esmeralda?" },
        new[] { "Hi, do you know how I can get HAL working again?", "Bonjour, savez-vous comment faire remarcher HAL ?", "Hallo, wissen Sie, wie ich HAL wieder zum Laufen bringe?", "Hola, ¿sabe cómo puedo hacer que HAL vuelva a funcionar?", "Salve, sa come posso far funzionare di nuovo HAL?", "Olá, sabe como posso pôr o HAL a funcionar outra vez?" },
        new[] { "Hi! When does the race against HAL start?", "Bonjour ! Quand commence la course contre HAL ?", "Hallo! Wann beginnt das Rennen gegen HAL?", "¡Hola! ¿Cuándo empieza la carrera contra HAL?", "Salve! Quando comincia la gara contro HAL?", "Olá! Quando começa a corrida contra o HAL?" },
        new[] { "Hi! How do I get to Otringal, on Zeelich?", "Bonjour ! Comment aller à Otringal, sur Zeelich ?", "Hallo! Wie komme ich nach Otringal auf Zeelich?", "¡Hola! ¿Cómo llego a Otringal, en Zeelich?", "Salve! Come si arriva a Otringal, su Zeelich?", "Olá! Como chego a Otringal, em Zeelich?" },
        new[] { "Hi, where do I sign up for the Cup race here?", "Bonjour, où puis-je m'inscrire pour la Coupe ici ?", "Hallo, wo kann ich mich hier für den Cup anmelden?", "Hola, ¿dónde me apunto a la Copa aquí?", "Salve, dove posso iscrivermi alla Coppa qui?", "Olá, onde me inscrevo na Taça aqui?" },
        new[] { "Hi, do you know anyone who could get me a place on the grid?", "Bonjour, connaissez-vous quelqu'un qui pourrait me trouver une place sur la grille ?", "Hallo, kennen Sie jemanden, der mir einen Platz in der Startaufstellung besorgen kann?", "Hola, ¿conoce a alguien que pueda conseguirme un sitio en la parrilla?", "Salve, conosce qualcuno che possa trovarmi un posto in griglia?", "Olá, conhece alguém que me possa arranjar um lugar na grelha?" },
        new[] { "Hi, where do I drive my qualifying lap?", "Bonjour, où dois-je faire mon tour de qualification ?", "Hallo, wo fahre ich meine Qualifikationsrunde?", "Hola, ¿dónde tengo que dar mi vuelta de clasificación?", "Salve, dove devo fare il mio giro di qualifica?", "Olá, onde faço a minha volta de qualificação?" },
        new[] { "Hi, do you know where I can find a room for the night?", "Bonjour, savez-vous où je peux trouver une chambre pour la nuit ?", "Hallo, wissen Sie, wo ich ein Zimmer für die Nacht finde?", "Hola, ¿sabe dónde puedo encontrar una habitación para esta noche?", "Salve, sa dove posso trovare una camera per la notte?", "Olá, sabe onde posso arranjar um quarto para esta noite?" },
        new[] { "Is it late already? I'd better get some sleep before race day.", "Il est déjà tard ? Je ferais mieux de dormir avant le jour de la course.", "Ist es schon spät? Ich sollte vor dem Renntag lieber schlafen.", "¿Ya es tarde? Mejor duermo un poco antes del día de la carrera.", "È già tardi? Meglio dormire un po' prima del giorno della gara.", "Já é tarde? É melhor dormir antes do dia da corrida." },
        new[] { "Do you know where I can get an ignition coil?", "Vous savez où je pourrais trouver une bobine d'allumage ?", "Wissen Sie, wo ich eine Zündspule bekomme?", "¿Sabe dónde puedo conseguir una bobina de encendido?", "Sa dove posso trovare una bobina d'accensione?", "Sabe onde posso arranjar uma bobina de ignição?" },
        new[] { "Hi, do you know where I can find the singer at Rick's bar? I've a letter for her.", "Bonjour, savez-vous où trouver la chanteuse du bar de Rick ? J'ai une lettre pour elle.", "Hallo, wissen Sie, wo ich die Sängerin aus Ricks Bar finde? Ich habe einen Brief für sie.", "Hola, ¿sabe dónde puedo encontrar a la cantante del bar de Rick? Tengo una carta para ella.", "Salve, sa dove posso trovare la cantante del bar di Rick? Ho una lettera per lei.", "Olá, sabe onde posso encontrar a cantora do bar do Rick? Tenho uma carta para ela." },
        new[] { "Hi, do you know where the gazogem seller is? I've the singer's reply for him.", "Bonjour, savez-vous où est le vendeur de gazogem ? J'ai la réponse de la chanteuse pour lui.", "Hallo, wissen Sie, wo der Gazogem-Verkäufer ist? Ich habe die Antwort der Sängerin für ihn.", "Hola, ¿sabe dónde está el vendedor de gazogem? Tengo la respuesta de la cantante para él.", "Salve, sa dov'è il venditore di gazogem? Ho la risposta della cantante per lui.", "Olá, sabe onde está o vendedor de gazogem? Tenho a resposta da cantora para ele." },
        new[] { "Hi! When does the Otringal race start?", "Bonjour ! Quand commence la course d'Otringal ?", "Hallo! Wann beginnt das Rennen von Otringal?", "¡Hola! ¿Cuándo empieza la carrera de Otringal?", "Salve! Quando comincia la gara di Otringal?", "Olá! Quando começa a corrida de Otringal?" },
        new[] { "Hi, have you seen a black car come this way?", "Bonjour, avez-vous vu passer une voiture noire ?", "Hallo, haben Sie hier einen schwarzen Wagen vorbeifahren sehen?", "Hola, ¿ha visto pasar por aquí un coche negro?", "Salve, ha visto passare di qui una macchina nera?", "Olá, viu passar por aqui um carro preto?" },
        new[] { "Hi, is it true the souvenir seller has a picture of that black car?", "Bonjour, est-ce vrai que le vendeur de souvenirs a une photo de cette voiture noire ?", "Hallo, stimmt es, dass der Souvenirverkäufer ein Bild von diesem schwarzen Wagen hat?", "Hola, ¿es verdad que el vendedor de recuerdos tiene una foto de ese coche negro?", "Salve, è vero che il venditore di souvenir ha una foto di quella macchina nera?", "Olá, é verdade que o vendedor de lembranças tem uma foto daquele carro preto?" },
        new[] { "Hi, how can I get De La Fontaine to let the race go ahead?", "Bonjour, comment convaincre De la Fontaine de laisser la course avoir lieu ?", "Hallo, wie bringe ich Brunnenschein dazu, das Rennen stattfinden zu lassen?", "Hola, ¿cómo puedo convencer a De la Fontaine de que deje que se corra la carrera?", "Salve, come posso convincere De la Fontaine a lasciar fare la gara?", "Olá, como convenço o De La Fontaine a deixar a corrida avançar?" },
        new[] { "Hi! When does the race against De La Fontaine's tanker start?", "Bonjour ! Quand commence la course contre le camion-citerne de De la Fontaine ?", "Hallo! Wann beginnt das Rennen gegen Brunnenscheins Tankwagen?", "¡Hola! ¿Cuándo empieza la carrera contra el camión cisterna de De la Fontaine?", "Salve! Quando comincia la gara contro l'autocisterna di De la Fontaine?", "Olá! Quando começa a corrida contra o camião-cisterna do De La Fontaine?" },
        new[] { "Hi! When does the race against the monkey monster start?", "Bonjour ! Quand commence la course contre le monstre singe ?", "Hallo! Wann beginnt das Rennen gegen das Affenmonster?", "¡Hola! ¿Cuándo empieza la carrera contra el monstruo mono?", "Salve! Quando comincia la gara contro il mostro scimmia?", "Olá! Quando começa a corrida contra o monstro macaco?" },
        new[] { "Hi, have you seen the miner's lost glove?", "Bonjour, avez-vous vu le gant perdu du mineur ?", "Hallo, haben Sie den verlorenen Handschuh des Bergmanns gesehen?", "Hola, ¿ha visto el guante perdido del minero?", "Salve, ha visto il guanto perduto del minatore?", "Olá, viu a luva perdida do mineiro?" },
        new[] { "Hi, do you know where the miner is? I've found his glove!", "Bonjour, savez-vous où est le mineur ? J'ai trouvé son gant !", "Hallo, wissen Sie, wo der Bergmann ist? Ich habe seinen Handschuh gefunden!", "Hola, ¿sabe dónde está el minero? ¡He encontrado su guante!", "Salve, sa dov'è il minatore? Ho trovato il suo guanto!", "Olá, sabe onde está o mineiro? Encontrei a luva dele!" },
        new[] { "Hi! When does the race against the miner's bulldozer start?", "Bonjour ! Quand commence la course contre le bulldozer du mineur ?", "Hallo! Wann beginnt das Rennen gegen die Planierraupe des Bergmanns?", "¡Hola! ¿Cuándo empieza la carrera contra la excavadora del minero?", "Salve! Quando comincia la gara contro il bulldozer del minatore?", "Olá! Quando começa a corrida contra o buldózer do mineiro?" },
        new[] { "Hi, do you know where I can get a pass for the Queen's race?", "Bonjour, savez-vous où obtenir un laissez-passer pour la course de la Reine ?", "Hallo, wissen Sie, wo ich einen Passierschein für das Rennen der Königin bekomme?", "Hola, ¿sabe dónde puedo conseguir un pase para la carrera de la Reina?", "Salve, sa dove posso avere un lasciapassare per la gara della Regina?", "Olá, sabe onde posso arranjar um passe para a corrida da Rainha?" },
        new[] { "Hi! When does the race against the Queen of the Mosquibees start?", "Bonjour ! Quand commence la course contre la Reine des Mosquibees ?", "Hallo! Wann beginnt das Rennen gegen die Königin der Mosquibees?", "¡Hola! ¿Cuándo empieza la carrera contra la Reina de los Mosquibees?", "Salve! Quando comincia la gara contro la Regina dei Mosquibee?", "Olá! Quando começa a corrida contra a Rainha dos Mosquibees?" },
        new[] { "Hi, do you know how I can seal the cracks in the hide-out before the race?", "Bonjour, savez-vous comment boucher les fissures de la cachette avant la course ?", "Hallo, wissen Sie, wie ich die Risse im Versteck vor dem Rennen abdichte?", "Hola, ¿sabe cómo puedo sellar las grietas del escondite antes de la carrera?", "Salve, sa come posso sigillare le crepe del nascondiglio prima della gara?", "Olá, sabe como posso tapar as fendas do esconderijo antes da corrida?" },
        new[] { "Hi! When does the race against the dissident start?", "Bonjour ! Quand commence la course contre le dissident ?", "Hallo! Wann beginnt das Rennen gegen den Dissidenten?", "¡Hola! ¿Cuándo empieza la carrera contra el disidente?", "Salve! Quando comincia la gara contro il dissidente?", "Olá! Quando começa a corrida contra o dissidente?" },
        new[] { "Hi, do you know how I can get through the door to the halls?", "Bonjour, savez-vous comment passer la porte des salles ?", "Hallo, wissen Sie, wie ich durch die Tür zu den Hallen komme?", "Hola, ¿sabe cómo puedo cruzar la puerta de las salas?", "Salve, sa come posso passare la porta delle sale?", "Olá, sabe como posso passar a porta das salas?" },
        new[] { "Hi! When does the race against the Emperor's soldier start?", "Bonjour ! Quand commence la course contre le soldat de l'Empereur ?", "Hallo! Wann beginnt das Rennen gegen den Soldaten des Kaisers?", "¡Hola! ¿Cuándo empieza la carrera contra el soldado del Emperador?", "Salve! Quando comincia la gara contro il soldato dell'Imperatore?", "Olá! Quando começa a corrida contra o soldado do Imperador?" },
        new[] { "Hi, do you know the way through the palace to Stan?", "Bonjour, connaissez-vous le chemin jusqu'à Stan dans le palais ?", "Hallo, kennen Sie den Weg durch den Palast zu Stan?", "Hola, ¿conoce el camino hasta Stan en el palacio?", "Salve, conosce la strada fino a Stan nel palazzo?", "Olá, conhece o caminho até ao Stan no palácio?" },
        new[] { "Hi! When does my race with Stan to the window start?", "Bonjour ! Quand commence la course avec Stan jusqu'à la fenêtre ?", "Hallo! Wann beginnt das Rennen gegen Stan bis zum Fenster?", "¡Hola! ¿Cuándo empieza la carrera con Stan hasta la ventana?", "Salve! Quando comincia la sfida con Stan fino alla finestra?", "Olá! Quando começa a corrida com o Stan até à janela?" },
        new[] { "Have you seen FunFrock? It's time to beat him once and for all!", "Avez-vous vu FunFrock ? Il est temps de le battre une fois pour toutes !", "Haben Sie FunFrock gesehen? Zeit, ihn ein für alle Mal zu schlagen!", "¿Ha visto a FunFrock? ¡Es hora de vencerlo de una vez por todas!", "Ha visto FunFrock? È ora di batterlo una volta per tutte!", "Viu o FunFrock? Está na hora de o vencer de uma vez por todas!" },
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
        // the storm: the Wizard Zoe sent him to, Raph at the track, the gloves, Raph's time, the lighthouse, bed -- but the gloves first while
        // he's after the thief, from Tim's word to the thief cornered: the quest he's in (2026-11-23, the user: "We need to be more intelligent
        // with Twinsen's opening line for the quest he's in" -- the sewers' painter, who saw the thief go by, was asked for the Wizard)
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenSeekGloves, new[] { Chapter(1), Is(GlovesSlot, 0), In(QThief, TimTold, ThiefCornered) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenSeekWizard, new[] { Chapter(1), In(StormPlot, 0, 1), Is(RaphHere, 0) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenSeekRaph, new[] { Chapter(1), Is(RaphHere, 0) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenSeekGloves, new[] { Chapter(1), Is(GlovesSlot, 0) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenBeatRaph, new[] { Chapter(1), Is(BeatVar, 0) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenToLighthouse, new[] { Chapter(1), In(KeeperPlot, 0, KeeperSpell - 1) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenToBed, new[] { Chapter(1) }),
        // the town circuit: Luc's wheel (Felix's tyre for bait), the entry fee, the race, the ferry
        // (the tyre in hand is the bait traded for it, QBait BaitTraded, whatever Luc's own step -- traded before Luc had told, his step
        // never came to WheelFound and Twinsen went on asking for a tyre with one in hand: the user, 2026-11-23)
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenBaitForFelix, new[] { Chapter(2), In(QLucCar, 0, WheelFitted - 1), Is(QBait, BaitHeld) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenWheelForLuc, new[] { Chapter(2), In(QLucCar, 0, WheelFound - 1), In(QBait, 0, BaitHeld) }),
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenWheelFound, new[] { Chapter(2), In(QLucCar, 0, WheelFitted - 1), Is(QBait, BaitTraded) }),
        // (the entry not yet paid: a hello -- "I need twenty kashes for the race's entry fee" made no sense as an opening line, least of all
        // before Mr. Paul had told him the fee; the user, 2026-11-22, "Let's have him just say hello". TwinsenEntryFee's line kept in the
        // bank, unsaid, so the lines after it keep their ids; the race line only once the entry is paid)
        new(RaceTrackIsland.Citadel.IslandByte, OpenerLine.TwinsenTownRace, new[] { Chapter(2), Is(EntryPaid, 1), Is(WonVar, 0) }),
        // (the race won, the ferry to the Desert next: a hello -- "I've a ferry to catch to the White Leaf Desert" told no one anything; the
        // user, 2026-11-22, "In this instance we can just have Twinsen say hello, as it's clear where he's heading". TwinsenFerry's line kept
        // in the bank, unsaid, so the lines after it keep their ids)
        // the Desert: the Dean, his pages, the filters, the helmet, the race; won (chapter 4 there), the moon
        // (the track shut only once Flick has said so, Q_PAGES 1 -- the user, 2026-11-23: "Twinsen shouldn't tell people he needs the dean
        // to open the race track, until he's been told the dean has shut the race track"; the pages' line with all three in hand, 7)
        new(RaceTrackIsland.Desert.IslandByte, OpenerLine.TwinsenTrackShut, new[] { Chapter(3), Is(QPages, 1), In(PagesFoundVar, 0, 6) }),
        new(RaceTrackIsland.Desert.IslandByte, OpenerLine.TwinsenSeekPages, new[] { Chapter(3), In(QPages, 2, PagesDiploma - 1), In(PagesFoundVar, 0, 6) }),
        new(RaceTrackIsland.Desert.IslandByte, OpenerLine.TwinsenPagesFound, new[] { Chapter(3), In(QPages, 0, PagesDiploma - 1), Is(PagesFoundVar, 7) }),
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

    // (the umbrella thief, the game's scene 48's actor 6, copied for the story's thief: not opened to while he's in the sewers, down the hatch
    // to cornered -- his own lines carry it, "Fine, fine! Here are your gloves" after "I'm looking for the Weather Wizard" made no sense;
    // the town's other people of his entity opened to as before)
    private const int ThiefEntityId = 71;

    // The race file's keys for an island (RaceCarEngineFile.EngineFile): its goals in the story, the hellos, Zoe never greeted and the thief
    // not in the sewers
    internal static string OpenerKeys(RaceTrackIsland island, bool story)
    {
        var text = new System.Text.StringBuilder();
        if (story)
            foreach (var goal in OpenerGoals().Where(g => g.Island == island.IslandByte))
                text.Append($"opener={OpenerBase + (int)goal.Line}{string.Concat(goal.When.Select(w => $" {w.Var} {w.Lo} {w.Hi}"))}\n");
        text.Append($"opener_hello={string.Join(' ', OpenerHellos.Select(h => OpenerBase + (int)h))}\n");
        text.Append($"opener_quiet={CompanionEntityId}\n");
        if (story) text.Append($"opener_quiet={ThiefEntityId} {OpenerChapter} 1 1 {QThief} {ThiefDown} {ThiefCornered}\n");
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
