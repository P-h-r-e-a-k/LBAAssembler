using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The race mod's story on Otringal, chapter 5 (docs/LBRA_WALKTHROUGH.md; the build notes: docs/LBRA_STORY_SCRIPT.md). Otringal's island
// byte (4) is the Emperor's palace's and the Temple of Bú's too: only Otringal's own scenes are edited (its outside scenes, the Casino,
// scene 135).
//   - Arriving by the Esmers' ship (the moon's win: scene 88, the track's start), Lyra at her plinth: the stewards' entry fee, and the
//     Esmers' 10 zlitos (Q_FEE 1); the radio rings, Baldino's call.
//   - Lyra herself takes the stewards' fee, 30 zlitos (Q_FEE 2: the race file's gate=217 2) -- short, the Casino round the corner (the
//     game's own slot machines). (The steward by the line, a copy of the Casino's doorman, and his Travel Token: gone, 2026-11-17.)
//   - In the Casino, the first time a visit: Zoe "Baldino said don't", Twinsen "I'm going in a little".
//   - Won (win=217 3): out of the car -- Lyra and the Fragment of the Sups (item 32), a Dark Monk priest's word, Zoe (Q_FEE 4); Johnny
//     Rocket at the space port (a copy of the game's, scene 81's actor 3, by the shuttles' pads in scene 92): the black car took the
//     shuttle to Celebration Island, and he flies them there (inc_chapter: 6; change_cube(95), the souvenir seller's stall on Celebration Island, where the lava lake's chapter begins).
// Rick (optional in the walkthrough) is by the Casino's bar, with the eggs (RaceTrackStory.Eggs.Otringal.cs); not at a piano.
// Not built: the priest standing in the crowd at the palace bend (his line is said with the others after the race).
internal static partial class RaceTrackStory
{
    // Q_FEE: 1 Lyra has told (the 10 zlitos given), 2 the Travel Token bought, 3 the race won, 4 Lyra and the priest have spoken, 5 Johnny
    // has flown them on
    public const int QFee = 217, FeePaid = 2, FeeWon = 3, FeeTold = 4, FeeDone = 5;
    // (the two days, 2026-11-19 -- RaceTrackStory.OtringalDays.cs: Q_FEE 1 told the race is full, 2 Lyra bribed (on the list: qualifying);
    // and the days' own variable (a wide one: Q_FEE's 3 on are the win's, and the race goes quiet once its win= holds -- once_won), 0 day
    // one, 1 qualified -- the cars in the stewards' garage overnight, 2 race day -- the car's ignition coil gone, 3 the coil found)
    public const int FeeFull = 1, OtringalDay = 266, DayQualified = 1, DayRaceDay = 2, DayCoil = 3;
    // (the Imperial Hotel's scene, for the race file's pool=)
    internal const int HotelSceneNumber = 81;
    private const int TravelToken = 24, FragmentSups = 32, Fee = 30, StarterZlitos = 10, CasinoScene = 135, Doorman = 8, HotelScene = 81,
                      JohnnyActor = 3, SpacePort = 92, SellerStall = 95;
    // the translator (the game's item), and the Lower City's gazogem seller (scene 89's actor 3)
    private const int Translator = 29, LowerCity = 89, GazogemSeller = 3;
    // the space port's pad (scene 92's own spaceship, actor 2, at its pos_point: RaceTrackIsland.Otringal's Shuttles)
    private static readonly (int X, int Z) Pad = (19200, 9472);

    private enum OtringalLine
    {
        Ringing, BaldinoRadio, LyraZeelich, LyraFee, LyraOpen, StewardFee, StewardShort, StewardAsk, Yes, No, StewardToken, StewardOnGrid,
        ZoeDont, TwinsenLittle, LyraWin, Priest, ZoeCharming, JohnnyCar, JohnnyFly, JohnnyBusy,
        // (the eggs': RaceTrackStory.Eggs.Otringal.cs)
        RickCup, RickTracks, RickKid, LyraNine, LyraEight,
        // (the sweep of 2026-11-05)
        LyraTranslator,
        // (the two days, 2026-11-19: RaceTrackStory.OtringalDays.cs)
        MarshalFull, EsmerMoney, MarshalStillFull, RickLyra, RickBlackCar, LyraOffDuty, LyraBribe, LyraShort, LyraAsk, LyraIn, LyraBlackCar,
        MarshalSetTime, MarshalQualified, NightPasses, MarshalCoil, MarshalNoCoil, SellerMask, CoilFound, MarshalReady,
        // (Johnny's pointer to the harbour taxi, his flight's place -- 2026-11-19)
        JohnnyTaxi,
    }
    private static readonly string[][] OtringalLines =
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
            "Otringal! Tunnels and town streets: I've given you short gears and a louder horn, people walk in the road there. Oh, and don't go in the Casino. I lost a lander in that Casino.",
            "Otringal ! Des tunnels et des rues : je t'ai mis des vitesses courtes et un klaxon plus fort, les gens marchent sur la route là-bas. Ah, et ne va pas au Casino. J'y ai perdu un atterrisseur.",
            "Otringal! Tunnel und Stadtstraßen: Ich habe dir kurze Gänge und eine lautere Hupe eingebaut, die Leute laufen dort auf der Straße. Ach, und geh nicht ins Casino. In dem Casino habe ich eine Landefähre verloren.",
            "¡Otringal! Túneles y calles: te he puesto marchas cortas y una bocina más fuerte, allí la gente camina por la calzada. Ah, y no entres en el Casino. En ese Casino perdí un módulo.",
            "Otringal! Gallerie e strade di città: ti ho messo marce corte e un clacson più forte, lì la gente cammina in mezzo alla strada. Ah, e non entrare nel Casinò. In quel Casinò ho perso un modulo.",
            "Otringal! Túneis e ruas da cidade: pus-te mudanças curtas e uma buzina mais forte, lá as pessoas andam pela estrada. Ah, e não entres no Casino. Perdi um módulo naquele Casino.",
        },
        new[]
        {
            "Zeelich! Here the Cup's run by the Emperor's stewards, and they want an entry fee. The Esmers left you a little Zeelich money to start you off.",
            "Zeelich ! Ici, la Coupe est organisée par les intendants de l'Empereur, et ils veulent un droit d'inscription. Les Esmers t'ont laissé un peu d'argent de Zeelich pour commencer.",
            "Zeelich! Hier wird der Pokal von den Verwaltern des Kaisers ausgerichtet, und sie wollen eine Startgebühr. Die Esmer haben dir ein wenig Geld von Zeelich dagelassen, für den Anfang.",
            "¡Zeelich! Aquí la Copa la llevan los administradores del Emperador, y quieren una cuota de inscripción. Los Esmers te han dejado un poco de dinero de Zeelich para empezar.",
            "Zeelich! Qui la Coppa la gestiscono gli intendenti dell'Imperatore, e vogliono una quota d'iscrizione. Gli Esmer ti hanno lasciato un po' di denaro di Zeelich per cominciare.",
            "Zeelich! Aqui a Taça é organizada pelos intendentes do Imperador, e eles querem uma taxa de inscrição. Os Esmers deixaram-te um pouco de dinheiro de Zeelich para começares.",
        },
        new[]
        {
            "The stewards want a Travel Token from every racer: 30 zlitos. I sell them for the Cup. No token, no grid.",
            "Les intendants exigent un Jeton de Voyage de chaque pilote : 30 zlitos. Je les vends pour la Coupe. Pas de jeton, pas de grille.",
            "Die Verwalter wollen von jedem Fahrer eine Reisemarke: 30 Zlitos. Ich verkaufe sie für den Pokal. Keine Marke, kein Startplatz.",
            "Los administradores exigen una Ficha de Viaje a cada piloto: 30 zlitos. Yo las vendo para la Copa. Sin ficha, no hay parrilla.",
            "Gli intendenti vogliono un Gettone di Viaggio da ogni pilota: 30 zlitos. Li vendo io per la Coppa. Niente gettone, niente griglia.",
            "Os intendentes exigem uma Ficha de Viagem a cada piloto: 30 zlitos. Sou eu que as vendo para a Taça. Sem ficha, não há grelha.",
        },
        new[]
        {
            "You've got your token! Line up whenever you're ready.",
            "Tu as ton jeton ! Mets-toi en place quand tu veux.",
            "Du hast deine Marke! Stell dich auf, wann immer du bereit bist.",
            "¡Ya tienes tu ficha! Colócate cuando quieras.",
            "Hai il tuo gettone! Mettiti in griglia quando vuoi.",
            "Já tens a tua ficha! Alinha quando quiseres.",
        },
        new[]
        {
            "A Travel Token for the Cup costs 30 zlitos. Twenty-five is a nice number too, but it's not 30.",
            "Un Jeton de Voyage pour la Coupe coûte 30 zlitos. Vingt-cinq, c'est un joli nombre aussi, mais ce n'est pas 30.",
            "Eine Reisemarke für den Pokal kostet 30 Zlitos. Fünfundzwanzig ist auch eine schöne Zahl, aber es ist nicht 30.",
            "Una Ficha de Viaje para la Copa cuesta 30 zlitos. Veinticinco también es un número bonito, pero no es 30.",
            "Un Gettone di Viaggio per la Coppa costa 30 zlitos. Anche venticinque è un bel numero, ma non è 30.",
            "Uma Ficha de Viagem para a Taça custa 30 zlitos. Vinte e cinco também é um número bonito, mas não é 30.",
        },
        new[]
        {
            "Short? There's a Casino round the corner. Some people leave it richer.",
            "Il te manque des sous ? Il y a un Casino au coin de la rue. Certains en ressortent plus riches.",
            "Zu wenig? Gleich um die Ecke ist ein Casino. Manche kommen reicher heraus.",
            "¿Te falta? Hay un Casino a la vuelta de la esquina. Hay quien sale de él más rico.",
            "Ti mancano? C'è un Casinò dietro l'angolo. Qualcuno ne esce più ricco.",
            "Falta-te dinheiro? Há um Casino ao virar da esquina. Há quem saia de lá mais rico.",
        },
        new[]
        {
            "Pay 30 zlitos?",
            "Payer 30 zlitos ?",
            "30 Zlitos bezahlen?",
            "¿Pagar 30 zlitos?",
            "Pagare 30 zlitos?",
            "Pagar 30 zlitos?",
        },
        new[] { "Yes.", "Oui.", "Ja.", "Sí.", "Sì.", "Sim." },
        new[] { "No.", "Non.", "Nein.", "No.", "No.", "Não." },
        new[]
        {
            "One Travel Token. You're on the grid.",
            "Un Jeton de Voyage. Tu es sur la grille.",
            "Eine Reisemarke. Du stehst am Start.",
            "Una Ficha de Viaje. Estás en la parrilla.",
            "Un Gettone di Viaggio. Sei in griglia.",
            "Uma Ficha de Viagem. Estás na grelha.",
        },
        new[]
        {
            "Token's in order. Off you go, racer.",
            "Jeton en règle. Vas-y, pilote.",
            "Die Marke ist in Ordnung. Los, Rennfahrer.",
            "La ficha está en regla. Adelante, piloto.",
            "Il gettone è in regola. Vai pure, pilota.",
            "A ficha está em ordem. Vai lá, piloto.",
        },
        new[]
        {
            "Baldino said don't.",
            "Baldino a dit de ne pas y aller.",
            "Baldino hat gesagt, lieber nicht.",
            "Baldino dijo que no.",
            "Baldino ha detto di no.",
            "O Baldino disse para não vir.",
        },
        new[]
        {
            "Baldino said don't go in. I'm going in a little.",
            "Baldino a dit de ne pas entrer. J'entre juste un peu.",
            "Baldino hat gesagt, ich soll nicht reingehen. Ich gehe nur ein bisschen rein.",
            "Baldino dijo que no entrara. Solo entro un poco.",
            "Baldino ha detto di non entrare. Entro solo un pochino.",
            "O Baldino disse para não entrar. Só entro um bocadinho.",
        },
        new[]
        {
            "A win on Zeelich! The stewards give you this: the Fragment of the Sups. Win the fragments of all of Zeelich's peoples, they say, and the Emperor's own gate will open to you.",
            "Une victoire sur Zeelich ! Les intendants te remettent ceci : le Fragment des Sups. Gagne les fragments de tous les peuples de Zeelich, disent-ils, et la porte de l'Empereur lui-même s'ouvrira pour toi.",
            "Ein Sieg auf Zeelich! Die Verwalter geben dir das hier: das Fragment der Sups. Gewinne die Fragmente aller Völker von Zeelich, sagen sie, und das Tor des Kaisers selbst wird sich dir öffnen.",
            "¡Una victoria en Zeelich! Los administradores te dan esto: el Fragmento de los Sups. Gana los fragmentos de todos los pueblos de Zeelich, dicen, y la puerta del propio Emperador se abrirá para ti.",
            "Una vittoria su Zeelich! Gli intendenti ti danno questo: il Frammento dei Sup. Vinci i frammenti di tutti i popoli di Zeelich, dicono, e il cancello dell'Imperatore in persona si aprirà per te.",
            "Uma vitória em Zeelich! Os intendentes dão-te isto: o Fragmento dos Sups. Ganha os fragmentos de todos os povos de Zeelich, dizem eles, e o próprio portão do Imperador abrir-se-á para ti.",
        },
        new[]
        {
            "Enjoy it while you can, little racer. The Cup's last race is already won.",
            "Profites-en tant que tu peux, petit pilote. La dernière course de la Coupe est déjà gagnée.",
            "Genieß es, solange du kannst, kleiner Rennfahrer. Das letzte Rennen des Pokals ist schon gewonnen.",
            "Disfrútalo mientras puedas, pequeño piloto. La última carrera de la Copa ya está ganada.",
            "Goditela finché puoi, piccolo pilota. L'ultima gara della Coppa è già vinta.",
            "Aproveita enquanto podes, pequeno piloto. A última corrida da Taça já está ganha.",
        },
        new[] { "Charming.", "Charmant.", "Reizend.", "Encantador.", "Simpatico.", "Encantador." },
        new[]
        {
            "A black car? Came off the moon ship last night and took the first shuttle to Celebration Island. The souvenir seller there sells pictures of every car he's seen. Ask him.",
            "Une voiture noire ? Elle est descendue du vaisseau de la lune hier soir et a pris la première navette pour l'île de la Célébration. Le vendeur de souvenirs là-bas vend des photos de toutes les voitures qu'il a vues. Demande-lui.",
            "Ein schwarzes Auto? Kam letzte Nacht vom Mondschiff und nahm die erste Fähre zur Insel der Feierlichkeiten. Der Souvenirverkäufer dort verkauft Bilder von jedem Auto, das er gesehen hat. Frag ihn.",
            "¿Un coche negro? Bajó anoche de la nave de la luna y tomó la primera lanzadera a la isla de la Celebración. El vendedor de recuerdos de allí vende fotos de todos los coches que ha visto. Pregúntale.",
            "Una macchina nera? È scesa dalla nave della luna ieri sera e ha preso la prima navetta per l'isola della Celebrazione. Il venditore di souvenir laggiù vende foto di ogni macchina che ha visto. Chiedi a lui.",
            "Um carro preto? Saiu da nave da lua ontem à noite e apanhou o primeiro vaivém para a ilha da Celebração. O vendedor de recordações de lá vende fotografias de todos os carros que viu. Pergunta-lhe.",
        },
        new[]
        {
            "I'll fly you there myself, if you like.",
            "Je vous y emmène moi-même, si tu veux.",
            "Ich fliege euch selbst hin, wenn du willst.",
            "Os llevo yo mismo, si quieres.",
            "Vi ci porto io stesso, se vuoi.",
            "Levo-vos lá eu mesmo, se quiseres.",
        },
        new[]
        {
            "Johnny Rocket, at your service. Win your race first, kid, then we'll talk.",
            "Johnny Rocket, pour vous servir. Gagne d'abord ta course, petit, on parlera ensuite.",
            "Johnny Rocket, zu Diensten. Gewinn erst dein Rennen, Kleiner, dann reden wir.",
            "Johnny Rocket, a tu servicio. Gana primero tu carrera, chaval, y luego hablamos.",
            "Johnny Rocket, al tuo servizio. Vinci prima la tua gara, ragazzo, poi parliamo.",
            "Johnny Rocket, ao teu dispor. Ganha primeiro a tua corrida, miúdo, depois falamos.",
        },
        new[]
        {
            "The Cup? I'll play you something fast for it.",
            "La Coupe ? Je vais te jouer quelque chose de rapide pour l'occasion.",
            "Der Pokal? Dafür spiele ich dir etwas Schnelles.",
            "¿La Copa? Te tocaré algo rápido para la ocasión.",
            "La Coppa? Ti suono qualcosa di veloce per l'occasione.",
            "A Taça? Vou tocar-te uma coisa rápida para a ocasião.",
        },
        new[]
        {
            "Of all the race tracks in all the towns in all the worlds, you drive into mine.",
            "De tous les circuits de toutes les villes de tous les mondes, c'est dans le mien que tu débarques.",
            "Von allen Rennstrecken in allen Städten auf allen Welten fährst du ausgerechnet in meine.",
            "De todos los circuitos de todas las ciudades de todos los mundos, tenías que entrar en el mío.",
            "Di tutte le piste di tutte le città di tutti i mondi, proprio nella mia dovevi entrare.",
            "De todas as pistas de todas as cidades de todos os mundos, logo na minha é que entras.",
        },
        new[]
        {
            "Here's looking at you, kid. Now go and win.",
            "À la tienne, petit. Maintenant, va gagner.",
            "Ich schau dir in die Augen, Kleiner. Und jetzt geh und gewinn.",
            "Brindo por ti, chaval. Y ahora ve y gana.",
            "Alla tua salute, ragazzo. Ora va' e vinci.",
            "À tua, miúdo. Agora vai e ganha.",
        },
        new[]
        {
            "Nine point five. You lose half a point for the splash.",
            "Neuf et demi. Tu perds un demi-point pour les éclaboussures.",
            "Neun Komma fünf. Einen halben Punkt Abzug für das Spritzen.",
            "Nueve coma cinco. Pierdes medio punto por la salpicadura.",
            "Nove e mezzo. Perdi mezzo punto per gli schizzi.",
            "Nove vírgula cinco. Perdes meio ponto pelo salpico.",
        },
        new[]
        {
            "Eight. You're showing off.",
            "Huit. Tu frimes.",
            "Acht. Du gibst an.",
            "Ocho. Te estás luciendo.",
            "Otto. Ti stai mettendo in mostra.",
            "Oito. Estás a exibir-te.",
        },
        // (the sweep of 2026-11-05: the translator, with Lyra's welcome)
        new[]
        {
            "And this, from the Esmers too: a translator. The people here don't all speak Twinsunian.",
            "Et ceci, de la part des Esmers aussi : un traducteur. Les gens d'ici ne parlent pas tous le twinsunien.",
            "Und das hier, auch von den Esmeriern: ein Übersetzer. Hier sprechen nicht alle Twinsunisch.",
            "Y esto, también de parte de los Esmers: un traductor. Aquí no todo el mundo habla twinsuniano.",
            "E questo, sempre da parte degli Esmers: un traduttore. Qui non tutti parlano il twinsuniano.",
            "E isto, também da parte dos Esmers: um tradutor. Aqui nem toda a gente fala twinsuniano.",
        },
        // (the two days, 2026-11-19 -- RaceTrackStory.OtringalDays.cs: the race full, Lyra's bribe, qualifying, the garage overnight, the
        // coil gone, the black car's details)
        new[]
        {
            "Twinsen of Citadel Island? You're too late -- the grid filled up while you were off on the Emerald Moon. The race runs tomorrow. The last place went to a black car: no name on its entry, just a mask.",
            "Twinsen, de l'île de la Citadelle ? Trop tard -- la grille s'est remplie pendant que tu étais sur la Lune d'Émeraude. La course a lieu demain. La dernière place est allée à une voiture noire : pas de nom sur l'inscription, juste un masque.",
            "Twinsen von der Zitadelleninsel? Zu spät -- das Starterfeld wurde voll, während du auf dem Smaragdmond warst. Das Rennen ist morgen. Den letzten Platz hat ein schwarzes Auto bekommen: kein Name auf der Meldung, nur eine Maske.",
            "¿Twinsen, de la isla de la Ciudadela? Llegas tarde: la parrilla se llenó mientras estabas en la Luna Esmeralda. La carrera es mañana. La última plaza fue para un coche negro: ningún nombre en la inscripción, solo una máscara.",
            "Twinsen dell'isola della Cittadella? Troppo tardi: la griglia si è riempita mentre eri sulla Luna di Smeraldo. La gara è domani. L'ultimo posto è andato a un'auto nera: nessun nome sull'iscrizione, solo una maschera.",
            "Twinsen, da ilha da Cidadela? Chegaste tarde: a grelha encheu-se enquanto estavas na Lua Esmeralda. A corrida é amanhã. O último lugar foi para um carro preto: nenhum nome na inscrição, só uma máscara.",
        },
        new[]
        {
            "The Esmers left a little Zeelich money for you: 10 zlitos.",
            "Les Esmers t'ont laissé un peu d'argent de Zeelich : 10 zlitos.",
            "Die Esmerier haben dir ein bisschen Zeelich-Geld dagelassen: 10 Zlitos.",
            "Los Esmers te han dejado algo de dinero de Zeelich: 10 zlitos.",
            "Gli Esmers ti hanno lasciato un po' di denaro di Zeelich: 10 zlitos.",
            "Os Esmers deixaram-te algum dinheiro de Zeelich: 10 zlitos.",
        },
        new[]
        {
            "Still full, I'm afraid. Ask around town -- somebody always knows somebody.",
            "Toujours complet, désolée. Renseigne-toi en ville -- il y a toujours quelqu'un qui connaît quelqu'un.",
            "Immer noch voll, tut mir leid. Hör dich in der Stadt um -- irgendwer kennt immer irgendwen.",
            "Sigue llena, lo siento. Pregunta por la ciudad: siempre hay alguien que conoce a alguien.",
            "Ancora al completo, mi spiace. Chiedi in giro per la città: c'è sempre qualcuno che conosce qualcuno.",
            "Continua cheia, lamento. Pergunta pela cidade -- há sempre alguém que conhece alguém.",
        },
        new[]
        {
            "A place on the grid? Talk to Lyra, she runs the Cup's paperwork. It's her day off, so try the Imperial Hotel's pool. And bring zlitos -- Lyra likes zlitos.",
            "Une place sur la grille ? Va voir Lyra, c'est elle qui s'occupe des papiers de la Coupe. C'est son jour de repos, essaie la piscine de l'Hôtel Impérial. Et prends des zlitos -- Lyra aime les zlitos.",
            "Ein Startplatz? Sprich mit Lyra, sie macht den Papierkram für den Pokal. Heute hat sie frei, versuch es am Pool des Kaiserlichen Hotels. Und bring Zlitos mit -- Lyra mag Zlitos.",
            "¿Una plaza en la parrilla? Habla con Lyra, ella lleva el papeleo de la Copa. Hoy libra, prueba en la piscina del Hotel Imperial. Y lleva zlitos: a Lyra le gustan los zlitos.",
            "Un posto in griglia? Parla con Lyra, è lei che si occupa delle carte della Coppa. Oggi è il suo giorno libero, prova alla piscina dell'Hotel Imperiale. E porta degli zlitos: a Lyra piacciono gli zlitos.",
            "Um lugar na grelha? Fala com a Lyra, é ela que trata da papelada da Taça. Hoje está de folga, experimenta a piscina do Hotel Imperial. E leva zlitos -- a Lyra gosta de zlitos.",
        },
        new[]
        {
            "That black car's driver was in here last night. Emptied one of my machines, never said a word, never took the mask off.",
            "Le pilote de la voiture noire est passé ici hier soir. Il a vidé une de mes machines sans dire un mot, sans jamais ôter son masque.",
            "Der Fahrer des schwarzen Autos war gestern Abend hier. Hat einen meiner Automaten geleert, kein Wort gesagt und die Maske nie abgenommen.",
            "El piloto del coche negro estuvo aquí anoche. Vació una de mis máquinas sin decir palabra y sin quitarse la máscara.",
            "Il pilota dell'auto nera è stato qui ieri sera. Ha svuotato una delle mie macchinette senza dire una parola, senza mai togliersi la maschera.",
            "O piloto do carro preto esteve cá ontem à noite. Esvaziou uma das minhas máquinas sem dizer uma palavra, sem nunca tirar a máscara.",
        },
        new[]
        {
            "The Cup? I'm off duty, Twinsen. Ask the marshal at the start line.",
            "La Coupe ? Je ne suis pas de service, Twinsen. Demande à la commissaire sur la ligne de départ.",
            "Der Pokal? Ich habe heute frei, Twinsen. Frag die Rennkommissarin an der Startlinie.",
            "¿La Copa? Hoy no estoy de servicio, Twinsen. Pregunta a la comisaria de la línea de salida.",
            "La Coppa? Oggi non sono in servizio, Twinsen. Chiedi alla commissaria sulla linea di partenza.",
            "A Taça? Estou de folga, Twinsen. Pergunta à comissária na linha de partida.",
        },
        new[]
        {
            "Full? Well... a grid has a way of growing a place. Thirty zlitos and I'll find you one.",
            "Complet ? Eh bien... une grille, ça se trouve toujours une place. Trente zlitos et je t'en trouve une.",
            "Voll? Na ja ... in so einem Starterfeld findet sich immer ein Platz. Dreißig Zlitos, und ich finde dir einen.",
            "¿Llena? Bueno... en una parrilla siempre aparece un hueco. Treinta zlitos y te lo encuentro.",
            "Al completo? Be'... in una griglia un posto si trova sempre. Trenta zlitos e te lo trovo io.",
            "Cheia? Bem... numa grelha arranja-se sempre um lugar. Trinta zlitos e eu encontro-te um.",
        },
        new[]
        {
            "Thirty zlitos, Twinsen, not a zlito less. The Casino's round the corner.",
            "Trente zlitos, Twinsen, pas un de moins. Le Casino est au coin de la rue.",
            "Dreißig Zlitos, Twinsen, keinen weniger. Das Casino ist gleich um die Ecke.",
            "Treinta zlitos, Twinsen, ni uno menos. El Casino está a la vuelta de la esquina.",
            "Trenta zlitos, Twinsen, non uno di meno. Il Casinò è dietro l'angolo.",
            "Trinta zlitos, Twinsen, nem um a menos. O Casino é logo ali à esquina.",
        },
        new[]
        {
            "Give Lyra 30 zlitos?",
            "Donner 30 zlitos à Lyra ?",
            "Lyra 30 Zlitos geben?",
            "¿Dar 30 zlitos a Lyra?",
            "Dare 30 zlitos a Lyra?",
            "Dar 30 zlitos à Lyra?",
        },
        new[]
        {
            "There -- you're on the list. Qualifying is today: set your time at the start line. Then the cars go into the stewards' garage overnight, for inspection.",
            "Voilà, tu es sur la liste. Les qualifications, c'est aujourd'hui : va faire ton temps sur la ligne de départ. Ensuite les voitures passent la nuit au garage des commissaires, pour l'inspection.",
            "So -- du stehst auf der Liste. Das Qualifying ist heute: Fahr deine Zeit an der Startlinie. Danach kommen die Autos über Nacht zur Inspektion in die Garage der Kommissare.",
            "Listo, estás en la lista. La clasificación es hoy: haz tu tiempo en la línea de salida. Después los coches pasan la noche en el garaje de los comisarios, para la inspección.",
            "Ecco, sei in lista. Le qualifiche sono oggi: fai il tuo tempo sulla linea di partenza. Poi le auto passano la notte nel garage dei commissari, per l'ispezione.",
            "Pronto, estás na lista. A qualificação é hoje: faz o teu tempo na linha de partida. Depois os carros passam a noite na garagem dos comissários, para a inspeção.",
        },
        new[]
        {
            "The black car? Paid its entry in old Sup coins. Nobody's seen its driver's face -- not even me.",
            "La voiture noire ? Payée en vieilles pièces des Sups. Personne n'a vu le visage de son pilote -- pas même moi.",
            "Das schwarze Auto? Hat die Startgebühr in alten Sup-Münzen bezahlt. Niemand hat das Gesicht des Fahrers gesehen -- nicht einmal ich.",
            "¿El coche negro? Pagó la inscripción con viejas monedas de los Sups. Nadie le ha visto la cara al piloto, ni siquiera yo.",
            "L'auto nera? Ha pagato l'iscrizione in vecchie monete dei Sup. Nessuno ha visto la faccia del pilota, nemmeno io.",
            "O carro preto? Pagou a inscrição com moedas antigas dos Sups. Ninguém viu a cara do piloto -- nem eu.",
        },
        new[]
        {
            "Lyra's put you on the list, I see. Set your qualifying time whenever you're ready.",
            "Lyra t'a inscrit, à ce que je vois. Fais ton temps de qualification quand tu veux.",
            "Lyra hat dich auf die Liste gesetzt, wie ich sehe. Fahr deine Qualifikationszeit, wann immer du so weit bist.",
            "Ya veo que Lyra te ha apuntado. Haz tu tiempo de clasificación cuando quieras.",
            "Vedo che Lyra ti ha messo in lista. Fai il tuo tempo di qualifica quando vuoi.",
            "Vejo que a Lyra te pôs na lista. Faz o teu tempo de qualificação quando quiseres.",
        },
        new[]
        {
            "A good lap. Your car stays with us tonight -- the stewards' garage, under lock and key. Get some rest: the race starts in the morning.",
            "Joli tour. Ta voiture reste avec nous cette nuit -- au garage des commissaires, sous clé. Repose-toi : la course part demain matin.",
            "Gute Runde. Dein Auto bleibt heute Nacht bei uns -- in der Garage der Kommissare, hinter Schloss und Riegel. Ruh dich aus: Das Rennen startet morgen früh.",
            "Buena vuelta. Tu coche se queda con nosotros esta noche, en el garaje de los comisarios, bajo llave. Descansa: la carrera empieza por la mañana.",
            "Bel giro. La tua auto resta con noi stanotte, nel garage dei commissari, sotto chiave. Riposati: la gara parte domattina.",
            "Boa volta. O teu carro fica connosco esta noite -- na garagem dos comissários, fechada à chave. Descansa: a corrida começa de manhã.",
        },
        new[]
        {
            "The cars are wheeled into the stewards' garage... The night passes. Morning on Otringal.",
            "Les voitures sont poussées dans le garage des commissaires... La nuit passe. Le matin se lève sur Otringal.",
            "Die Autos werden in die Garage der Kommissare geschoben ... Die Nacht vergeht. Morgen auf Otringal.",
            "Los coches entran en el garaje de los comisarios... Pasa la noche. Amanece en Otringal.",
            "Le auto vengono spinte nel garage dei commissari... La notte passa. È mattina su Otringal.",
            "Os carros são levados para a garagem dos comissários... A noite passa. Amanhece em Otringal.",
        },
        new[]
        {
            "Your car's back from inspection, Twinsen -- but its ignition coil isn't. Somebody had the garage open in the night. No coil, no race.",
            "Ta voiture est revenue de l'inspection, Twinsen -- mais pas sa bobine d'allumage. Quelqu'un a ouvert le garage cette nuit. Pas de bobine, pas de course.",
            "Dein Auto ist von der Inspektion zurück, Twinsen -- aber seine Zündspule nicht. Jemand war heute Nacht in der Garage. Keine Zündspule, kein Rennen.",
            "Tu coche ha vuelto de la inspección, Twinsen, pero su bobina de encendido no. Alguien abrió el garaje por la noche. Sin bobina, no hay carrera.",
            "La tua auto è tornata dall'ispezione, Twinsen, ma la bobina d'accensione no. Qualcuno ha aperto il garage stanotte. Niente bobina, niente gara.",
            "O teu carro voltou da inspeção, Twinsen -- mas a bobina de ignição não. Alguém abriu a garagem durante a noite. Sem bobina, não há corrida.",
        },
        new[]
        {
            "No ignition coil, no race, Twinsen. Ask around -- somebody must have seen something last night.",
            "Pas de bobine, pas de course, Twinsen. Renseigne-toi -- quelqu'un a bien dû voir quelque chose cette nuit.",
            "Keine Zündspule, kein Rennen, Twinsen. Hör dich um -- irgendwer muss heute Nacht etwas gesehen haben.",
            "Sin bobina no hay carrera, Twinsen. Pregunta por ahí: alguien tuvo que ver algo anoche.",
            "Niente bobina, niente gara, Twinsen. Chiedi in giro: qualcuno deve aver visto qualcosa stanotte.",
            "Sem bobina não há corrida, Twinsen. Pergunta por aí -- alguém deve ter visto alguma coisa esta noite.",
        },
        new[]
        {
            "Last night? Someone in a mask came out of the stewards' garage with something under his arm. He went down to the west pier and threw it in a crate.",
            "Cette nuit ? Quelqu'un de masqué est sorti du garage des commissaires avec quelque chose sous le bras. Il est descendu jusqu'à la jetée ouest et l'a jeté dans une caisse.",
            "Heute Nacht? Jemand mit Maske kam mit etwas unter dem Arm aus der Garage der Kommissare. Er ging zum Westpier hinunter und warf es in eine Kiste.",
            "¿Anoche? Alguien con máscara salió del garaje de los comisarios con algo bajo el brazo. Bajó al muelle oeste y lo tiró en una caja.",
            "Stanotte? Uno con la maschera è uscito dal garage dei commissari con qualcosa sotto il braccio. È sceso al molo ovest e l'ha buttato in una cassa.",
            "Esta noite? Alguém de máscara saiu da garagem dos comissários com uma coisa debaixo do braço. Desceu até ao cais oeste e atirou-a para dentro de um caixote.",
        },
        new[]
        {
            "Twinsen finds his car's ignition coil in the crate!",
            "Twinsen retrouve la bobine d'allumage de sa voiture dans la caisse !",
            "Twinsen findet die Zündspule seines Autos in der Kiste!",
            "¡Twinsen encuentra la bobina de encendido de su coche en la caja!",
            "Twinsen trova la bobina d'accensione della sua auto nella cassa!",
            "O Twinsen encontra a bobina de ignição do carro dentro do caixote!",
        },
        new[]
        {
            "The coil's back where it belongs. To the grid, Twinsen -- and watch that black car.",
            "La bobine est de retour à sa place. En grille, Twinsen -- et méfie-toi de cette voiture noire.",
            "Die Zündspule ist wieder, wo sie hingehört. Ab in die Startaufstellung, Twinsen -- und pass auf das schwarze Auto auf.",
            "La bobina vuelve a estar en su sitio. A la parrilla, Twinsen, y ojo con ese coche negro.",
            "La bobina è tornata al suo posto. In griglia, Twinsen, e occhio a quell'auto nera.",
            "A bobina voltou ao seu lugar. Para a grelha, Twinsen -- e cuidado com aquele carro preto.",
        },
        new[]
        {
            "My rocket's got nowhere to land on Celebration, kid. Take a taxi from the harbour -- tell the driver you won the Cup.",
            "Ma fusée n'a nulle part où se poser sur la Célébration, petit. Prends un taxi au port -- dis au chauffeur que tu as gagné la Coupe.",
            "Meine Rakete kann auf der Insel der Feierlichkeiten nirgends landen, Kleiner. Nimm ein Taxi am Hafen -- sag dem Fahrer, dass du den Pokal gewonnen hast.",
            "Mi cohete no tiene dónde aterrizar en la Celebración, chico. Coge un taxi en el puerto y dile al conductor que ganaste la Copa.",
            "Il mio razzo non ha dove atterrare sulla Celebrazione, ragazzo. Prendi un taxi al porto: di' all'autista che hai vinto la Coppa.",
            "O meu foguetão não tem onde aterrar na Celebração, miúdo. Apanha um táxi no porto -- diz ao condutor que ganhaste a Taça.",
        },
    };

    // The nearest place to an island cell clear of every road of a track by `clear` cells, on its ground: searched outward ring by ring
    private static (double X, double Z, double Y)? OffRoadA(RaceTrackReport report, double x, double z, double clear, double reach = 12)
    {
        if (report.GroundAfter is not { } ground) return null;
        for (var r = 0.0; r <= reach; r += 0.5)
        {
            var steps = r == 0 ? 1 : (int)Math.Ceiling(2 * Math.PI * r / 0.5);
            for (var k = 0; k < steps; k++)
            {
                double a = 2 * Math.PI * k / steps, px = x + r * Math.Sin(a), pz = z + r * Math.Cos(a);
                if (OffRoads(report, px, pz) < clear) continue;
                var g = ground(px, pz);
                if (g < 200) continue;
                return (px, pz, g);
            }
        }
        return null;
    }

    private static List<string> ApplyOtringal(string gameDirectory, RaceTrackReport report, RaceTrackScenes.Result scenes)
    {
        var log = new List<string>();
        if (report.StartLine.Count == 0 || scenes.StartScene < 0) { log.Add("Otringal: no story, its track has no start line"); return log; }
        var first = AddIslandTexts(gameDirectory, RaceTrackIsland.Otringal.IslandByte, OtringalLines, log, "Otringal");
        if (first < 0) return log;
        int Say(OtringalLine line) => first + (int)line;
        var b = new IslandBeatsA(gameDirectory, RaceTrackIsland.Otringal, log);
        const string otringal = "5 == chapter()";

        // the radio, and after a win Lyra, the fragment, the priest and Zoe -- on every outside scene (wherever Twinsen gets out of his car)
        foreach (var scene in b.OutsideScenes.ToList())
            b.Edit(scene, "the radio rings in chapter 5; after a win Lyra (the Fragment of the Sups), the priest, Zoe", model =>
            {
                var spot = AfterRaceSpot(report);
                if ((int)Math.Floor(spot.X / 64) != model.CubeX || (int)Math.Floor(spot.Z / 64) != model.CubeY) spot = (model.CubeX * 64 + 32, model.CubeY * 64 + 32, report.StartLine[0].Y, 0);
                var priest = model.Actors.Count;
                var edits = new List<Action<SceneScripts>>
                {
                    IslandBeatsA.Add(model, Voice(model, spot, 1), "void comportement_0()\n{\n}\n"),
                    IslandBeatsA.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 4),
                        RadioController(5, Say(OtringalLine.Ringing), new[] { Say(OtringalLine.BaldinoRadio) })),
                    IslandBeatsA.Add(model, Voice(model, spot, 9), $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if ({FeeWon} == var_game({QFee}) && {Driving} != comportement_hero())
    {{
        set_var_game({QFee}, {FeeTold});
        message({Say(OtringalLine.LyraWin)});
        found_object({FragmentSups});
        set_var_game({FragmentSups}, 1);
        message_obj({priest}, {Say(OtringalLine.Priest)});
    }}
    if ({DayQualified} == var_game({OtringalDay}) && {Driving} != comportement_hero())
    {{
        cinema_mode(1);
        message({Say(OtringalLine.MarshalQualified)});
        message({Say(OtringalLine.NightPasses)});
        set_var_game({OtringalDay}, {DayRaceDay});
        message({Say(OtringalLine.MarshalCoil)});
        cinema_mode(0);
    }}
}}
"),
                };
                return scripts => { foreach (var e in edits) e(scripts); };
            });

        // Lyra (at her plinth, and by the hotel's pool): the Esmers' 10 zlitos (Q_FEE 1), then the fee -- 30 zlitos, taken by her (Q_FEE 2).
        // (The steward by the line who sold a Travel Token is gone, and the token with him: the user, 2026-11-17, "The dog by the race track
        // currently sells us the travel token, but we have to talk to Lyra inside the Imperial hotel first, this makes no sense. Let's remove
        // the dog" -- and the token was inventory slot 24, the game's memory viewer, "an optional object and can be purchased by him on
        // otringal, so we don't need it given to him".) The translator (item 29), which the control tower's staff and the space port's
        // mechanic answer only to, is the Desert race's prize now, with the licence (RaceTrackStory.Desert: "The translator let's have this
        // given to Twinsen when he wins the race on Desert Island as this makes most sense")
        // (since 2026-11-19 the start line's is the race marshal's -- the user, "when Twinsen arrives at the start line and tries to speak to
        // the race marshall she tells him that he's too late and the race is full, and it's due to run 'Tomorrow'" -- and Lyra, by the
        // hotel's pool, takes a bribe for a place: RaceTrackStory.OtringalDays.cs)
        var marshalTalk = MarshalTalk(Say);
        MarshalA(b, report, otringal, marshalTalk, "the race marshal");
        // (Zoe stays on Twinsun now -- the user, 2026-11-19, "she's no longer coming with us": her "Baldino said don't" at the Casino's door
        // gone, and her word after the win)
        // ... and its slot machines never leave Twinsen too broke for the fee: the pull that spends his last zlito, the Travel Token not yet
        // bought, stops every reel on Money (25 zlitos). The game's odds (a win about one pull in eight, from the Esmers' 10) left one player
        // in four with nothing and the Cup closed to him (the scripted playthrough, 2026-11-05: 35 pulls, one win, 0 zlitos). Each reel's
        // pick is its own scene variable, set after the game's forced key (a pull is paid before the reels spin: hero comportements 10-12).
        b.Edit(CasinoScene, "the Casino's slot machines: the last zlito's pull pays, while Lyra is still to be paid", _ => scripts =>
        {
            var reel = new System.Text.RegularExpressions.Regex(@"if \(0 == var_cube\(37\) && \(20 < var_cube\(38\) \|\| 4 <= var_cube\(13\)\)\)\s*\{\s*set_var_cube\((\d+), 3\);\s*\}");
            var reels = 0;
            for (var a = 0; a < scripts.ActorCount; a++)
            {
                var life = scripts.GetText(a, ScriptKind.Life);
                if (!life.Contains("switch (rnd(27))") || reel.Match(life) is not { Success: true } m) continue;
                scripts.SetText(a, ScriptKind.Life, life[..(m.Index + m.Length)] + $@"
    if ({otringal} && 0 == nb_gold_pieces() && {FeePaid} > var_game({QFee}))
    {{
        set_var_cube({m.Groups[1].Value}, 4);
    }}" + life[(m.Index + m.Length)..]);
                reels++;
            }
            if (reels == 0) throw new NotTheGames("the Casino's slot machine reels");
            log.Add($"the Casino: {reels} slot machine reels pay the last zlito's pull");
        });

        // Johnny Rocket by the space port's pad: the black car's shuttle, and he flies them to Celebration Island
        if (Copy(b.Originals, HotelScene, JohnnyActor) is { } johnny && b.Store.SceneExists(SpacePort))
        {
            var port = b.Store.Load(SpacePort);
            if (OffRoadA(report, port.CubeX * 64 + Pad.X / 512.0, port.CubeY * 64 + Pad.Z / 512.0, 1.5) is { } spot && b.SceneAt(spot.X, spot.Z) is var johnnyScene and >= 0)
                // (no longer his flight -- his spaceship has nowhere to land on Celebration Island: the harbour taxi takes the Cup's winner,
                // RaceTrackStory.Travels -- the user, 2026-11-19, "we'll redesign one of the Taxi's that already take Twinsen from Otringal to
                // Celebration Island". He tells where the black car went, and points him to the taxi)
                b.Edit(johnnyScene, $"Johnny Rocket by the space port at cell ({spot.X:0.#}, {spot.Z:0.#}): the black car's shuttle, and the harbour taxi to Celebration Island", model =>
                {
                    Place(johnny, model, Facing(spot, port.CubeX * 64 + Pad.X / 512.0, port.CubeY * 64 + Pad.Z / 512.0));
                    return IslandBeatsA.Add(model, johnny, Talker(otringal, $@"            if ({FeeTold} == var_game({QFee}))
            {{
                message({Say(OtringalLine.JohnnyCar)});
                message({Say(OtringalLine.JohnnyTaxi)});
            }}
            else
            {{
                message({Say(OtringalLine.JohnnyBusy)});
            }}"));
                });
            else log.Add("Johnny Rocket left out: no room off the road by the space port's pad");
        }
        else log.Add("Johnny Rocket left out: no Johnny in the hotel (scene 81's actor 3), or no space port scene");

        // the Imperial Hotel's pool (the user, 2026-11-11: swimming in it, and a diving board): the water over the basin, drawn into the
        // backdrop, and the board on the deck by it -- the engine does the swimming and the dive (pool=, pool_board=: RaceCarEngineFile)
        if (b.Store.SceneExists(HotelScene))
            b.Edit(HotelScene, "the hotel pool's water and a diving board (Twinsen swims in it: RACEMOD.CPP pool=)", model =>
            {
                var water = Citadel.CitadelProps.Actor(Citadel.CitadelProps.PoolWater, Citadel.CitadelProps.PoolMiddleX, Citadel.CitadelProps.PoolSurface, Citadel.CitadelProps.PoolMiddleZ);
                // (the water walked through, not a solid sheet: falling through it off the diving board, Twinsen was pushed out of its box --
                // both basins' -- to its far corner, or out of the room altogether, the user, 2026-11-14: "After respawning after using the
                // diving board in the imperial hotel twinsen respawns out of bounds")
                water.Armor = WalkOverArmour;
                var board = Citadel.CitadelProps.Actor(Citadel.CitadelProps.DivingBoard, Citadel.CitadelProps.BoardX, Citadel.CitadelProps.PoolDeck, Citadel.CitadelProps.BoardZ, Citadel.CitadelProps.BoardBeta);
                const string Still = "void comportement_0()\n{\n}\n";
                var addWater = IslandBeatsA.Add(model, water, Still);
                var addBoard = IslandBeatsA.Add(model, board, Still);
                var ladder = Citadel.CitadelProps.Actor(Citadel.CitadelProps.PoolLadder, Citadel.CitadelProps.LadderX, Citadel.CitadelProps.PoolDeck, Citadel.CitadelProps.LadderZ, Citadel.CitadelProps.LadderBeta);
                ladder.Armor = WalkOverArmour;
                var addLadder = IslandBeatsA.Add(model, ladder, Still);
                // (the board walked on, up a step behind its stand: both carry Twinsen -- OBJ_CARRIER)
                board.Flags |= RaceTrackScenes.CarrierFlag;
                var step = Citadel.CitadelProps.Actor(Citadel.CitadelProps.BoardStep, Citadel.CitadelProps.BoardStepX, Citadel.CitadelProps.PoolDeck, Citadel.CitadelProps.BoardZ, Citadel.CitadelProps.BoardBeta);
                step.Flags |= RaceTrackScenes.CarrierFlag;
                var addStep = IslandBeatsA.Add(model, step, Still);
                // (the ladder's zone, the game's own -- type 6, on: in the water by the wall from the pool's floor up to the deck -- not on the
                // deck: walking forward in a ladder's zone climbs, and there he stuck at the edge. He backs over it into the zone to climb down)
                model.Zones.Add(new SceneZoneModel
                {
                    Type = 6, Num = 0, X0 = Citadel.CitadelProps.PoolWall, X1 = Citadel.CitadelProps.PoolWall + 600,
                    Y0 = Citadel.CitadelProps.PoolFloor - 256, Y1 = Citadel.CitadelProps.PoolDeck,
                    Z0 = Citadel.CitadelProps.LadderZ - 300, Z1 = Citadel.CitadelProps.LadderZ + 300, Info = new[] { 1, 1, 1, 0, 0, 0, 0, 1 },
                });
                return scripts => { addWater(scripts); addBoard(scripts); addLadder(scripts); addStep(scripts); };
            });

        // the bowling alley's people's words and its change machine (RaceTrackStory.BowlingTalk)
        BowlingTalk(b, gameDirectory, log);

        // the bowling alley's lift in the prison exit's platform (BowlingAlley: scene 393 below it)
        if (b.Store.SceneExists(BowlingAlley.Outside) && b.Store.SceneExists(BowlingAlley.Scene))
            b.Edit(BowlingAlley.Outside, "the hidden bowling alley's lift in the prison exit's platform", BowlingAlley.OutsideLift);

        // the gazogem seller in the Lower City: sold out to a fellow in a black car (IslandRetell's 245), and that's all -- not the game's
        // arrow to the Francos' refinery, nor its variable 123, which puts "The island of the Francos" on the taxis' list (the sweep of
        // 2026-11-05)
        b.Edit(LowerCity, "the gazogem seller: no arrow to the Francos' refinery, no taxi there", _ => scripts =>
            scripts.SetText(GazogemSeller, ScriptKind.Life, Loose(scripts.GetText(GazogemSeller, ScriptKind.Life),
                "message(245);\nset_holo_pos(140);\nset_var_cube(0, 0);\nset_var_game(123, 1);", "message(245);\nset_var_cube(0, 0);")));

        // the eggs: Rick in the Casino, Lyra by the hotel's pool (RaceTrackStory.Eggs.Otringal.cs)
        RickAtTheCasino(b, otringal, Say(OtringalLine.RickCup), Say(OtringalLine.RickTracks), Say(OtringalLine.RickKid), Say(OtringalLine.RickLyra), Say(OtringalLine.RickBlackCar));
        LyraPoolScore(b, otringal, Say(OtringalLine.LyraNine), Say(OtringalLine.LyraEight), LyraTalk(Say));
        // (the two days: the witness and the coil -- RaceTrackStory.OtringalDays.cs)
        OtringalDays(b, report, Say, otringal);

        b.Save();
        log.Add($"Otringal's race file: gate={QFee} {FeePaid} (Lyra paid), qualifying the first day (qualify_stop={OtringalDay} 0 {DayQualified}), the race the next once the coil is found (gate_not={OtringalDay} {DayRaceDay}), win={QFee} {FeeWon}");
        return log;
    }
}
