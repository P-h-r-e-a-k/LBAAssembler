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
    // the game's Baldino on Otringal: by his crashed lander (scene 138's actor 2), and his radio calls at the harbour (the test that keeps
    // them, scenes 87's and 209's actor 6) -- he stays on Twinsun
    private const int BaldinoLanding = 138, LandedBaldino = 2;
    private const string BaldinoCalls = "0 == var_game(117) && 2 < var_game(124)";
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
        // (the night at the Imperial Hotel and the coil's fetch quest -- RaceTrackStory.OtringalNight, 2026-11-22)
        MarshalHeld, MarshalNight, ReceptionOffer, RoomClassic, RoomJunior, RoomSuite, ReceptionNoRoom, ReceptionKey, ReceptionShort, ReceptionDay, ReceptionMorning, TwinsenNoKey, RoomLocked, TwinsenSleep, WakeClassic, WakeJunior, WakeSuite, TwinsenAskCoil, SellerMaybe, TwinsenWhatFavour, SellerLetter, SellerWaiting, TwinsenGiveLetter, SingerReply, SingerWaiting, SellerReplyRead, SellerAsk, SellerSold, SellerShort, SellerAfter, RickCoil, JohnnyCoil, LyraCoil, ReceptionCoil,
        // (the singer on the stage: not now -- the letter in the back room, Rick's office: 2026-11-23)
        SingerOnStage,
        // (the hotel's guards in chapter 5: a racer is a guest -- RaceTrackStory.OtringalNight, 2026-11-24)
        GuardGuest,
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
            "Your car's back from the palace, Twinsen -- it's waiting in the pit lane, but without its ignition coil. Somebody had the stewards' garage open in the night. The others are already on the grid.",
            "Ta voiture est revenue du palais, Twinsen -- elle t'attend dans la voie des stands, mais sans sa bobine d'allumage. Quelqu'un a ouvert le garage des commissaires cette nuit. Les autres sont déjà sur la grille.",
            "Dein Auto ist aus dem Palast zurück, Twinsen -- es wartet in der Boxengasse, aber ohne seine Zündspule. Jemand hat in der Nacht die Garage der Kommissare geöffnet. Die anderen stehen schon in der Startaufstellung.",
            "Tu coche ha vuelto del palacio, Twinsen: te espera en la calle de boxes, pero sin su bobina de encendido. Alguien abrió el garaje de los comisarios por la noche. Los demás ya están en la parrilla.",
            "La tua auto è tornata dal palazzo, Twinsen: ti aspetta nella corsia dei box, ma senza la bobina d'accensione. Qualcuno ha aperto il garage dei commissari stanotte. Gli altri sono già in griglia.",
            "O teu carro voltou do palácio, Twinsen -- está à tua espera na via das boxes, mas sem a bobina de ignição. Alguém abriu a garagem dos comissários durante a noite. Os outros já estão na grelha.",
        },
        new[]
        {
            "No ignition coil, no race, Twinsen -- and the race starts in an hour. Ask the gazogem seller in the Lower City; his stall is open all night, he sees everything.",
            "Pas de bobine d'allumage, pas de course, Twinsen -- et la course part dans une heure. Demande au vendeur de gazogem de la Ville Basse : son étal est ouvert toute la nuit, il voit tout.",
            "Keine Zündspule, kein Rennen, Twinsen -- und das Rennen startet in einer Stunde. Frag den Gazogem-Verkäufer in der Unterstadt: Sein Stand hat die ganze Nacht offen, er sieht alles.",
            "Sin bobina de encendido no hay carrera, Twinsen, y la carrera empieza dentro de una hora. Pregunta al vendedor de gazogem de la Ciudad Baja: su puesto abre toda la noche, lo ve todo.",
            "Niente bobina d'accensione, niente gara, Twinsen, e la gara parte tra un'ora. Chiedi al venditore di gazogem della Città Bassa: il suo banco è aperto tutta la notte, vede tutto.",
            "Sem bobina de ignição, não há corrida, Twinsen -- e a corrida começa daqui a uma hora. Pergunta ao vendedor de gazogem da Cidade Baixa: a banca dele está aberta a noite toda, ele vê tudo.",
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
            "Twinsen has his car's ignition coil. Back to the car!",
            "Twinsen a la bobine d'allumage de sa voiture. Retour à la voiture !",
            "Twinsen hat die Zündspule seines Autos. Zurück zum Auto!",
            "¡Twinsen tiene la bobina de encendido de su coche! ¡De vuelta al coche!",
            "Twinsen ha la bobina d'accensione della sua auto. Torna all'auto!",
            "O Twinsen tem a bobina de ignição do carro. De volta ao carro!",
        },
        new[]
        {
            "The coil's back where it belongs. You start from the pit lane, Twinsen -- and watch that black car.",
            "La bobine est de retour à sa place. Tu pars de la voie des stands, Twinsen -- et méfie-toi de cette voiture noire.",
            "Die Zündspule ist wieder, wo sie hingehört. Du startest aus der Boxengasse, Twinsen -- und pass auf das schwarze Auto auf.",
            "La bobina vuelve a estar en su sitio. Sales desde la calle de boxes, Twinsen, y ojo con ese coche negro.",
            "La bobina è tornata al suo posto. Parti dalla corsia dei box, Twinsen, e occhio a quell'auto nera.",
            "A bobina voltou ao seu lugar. Partes da via das boxes, Twinsen -- e cuidado com aquele carro preto.",
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
        new[]
        {
            "A good lap, Twinsen! Every car spends the night in the Emperor's palace, under the stewards' lock and key -- yours too. Get yourself a room at the Imperial Hotel. The race starts in the morning.",
            "Joli tour, Twinsen ! Toutes les voitures passent la nuit au palais de l'Empereur, sous la clé des commissaires -- la tienne aussi. Prends-toi une chambre à l'Hôtel Impérial. La course part demain matin.",
            "Gute Runde, Twinsen! Alle Autos verbringen die Nacht im Palast des Kaisers, unter Verschluss der Rennkommissare -- deins auch. Nimm dir ein Zimmer im Kaiserlichen Hotel. Das Rennen startet morgen früh.",
            "¡Buena vuelta, Twinsen! Todos los coches pasan la noche en el palacio del Emperador, bajo llave de los comisarios: el tuyo también. Búscate una habitación en el Hotel Imperial. La carrera empieza mañana por la mañana.",
            "Bel giro, Twinsen! Tutte le auto passano la notte nel palazzo dell'Imperatore, sotto chiave dei commissari: anche la tua. Prenditi una camera all'Hotel Imperiale. La gara parte domattina.",
            "Boa volta, Twinsen! Todos os carros passam a noite no palácio do Imperador, fechados à chave pelos comissários -- o teu também. Arranja um quarto no Hotel Imperial. A corrida começa amanhã de manhã.",
        },
        new[]
        {
            "Your car's safe in the palace till morning. Go and get some sleep -- the Imperial Hotel has rooms.",
            "Ta voiture est en sécurité au palais jusqu'à demain matin. Va dormir un peu -- l'Hôtel Impérial a des chambres.",
            "Dein Auto ist bis morgen früh sicher im Palast. Geh schlafen -- das Kaiserliche Hotel hat Zimmer.",
            "Tu coche está a salvo en el palacio hasta mañana. Ve a dormir un poco: el Hotel Imperial tiene habitaciones.",
            "La tua auto è al sicuro nel palazzo fino a domattina. Vai a dormire un po': l'Hotel Imperiale ha delle camere.",
            "O teu carro está seguro no palácio até de manhã. Vai dormir um pouco -- o Hotel Imperial tem quartos.",
        },
        new[]
        {
            "Good evening, sir. A room for the night?",
            "Bonsoir, monsieur. Une chambre pour la nuit ?",
            "Guten Abend, der Herr. Ein Zimmer für die Nacht?",
            "Buenas noches, señor. ¿Una habitación para esta noche?",
            "Buonasera, signore. Una camera per la notte?",
            "Boa noite, senhor. Um quarto para esta noite?",
        },
        new[]
        {
            "The Classic Room, 100 zlitos.",
            "La Chambre Classique, 100 zlitos.",
            "Das Klassische Zimmer, 100 Zlitos.",
            "La Habitación Clásica, 100 zlitos.",
            "La Camera Classica, 100 zlitos.",
            "O Quarto Clássico, 100 zlitos.",
        },
        new[]
        {
            "The Junior Suite, 250 zlitos.",
            "La Suite Junior, 250 zlitos.",
            "Die Junior-Suite, 250 Zlitos.",
            "La Suite Júnior, 250 zlitos.",
            "La Junior Suite, 250 zlitos.",
            "A Suite Júnior, 250 zlitos.",
        },
        new[]
        {
            "The Imperial Suite, 500 zlitos.",
            "La Suite Impériale, 500 zlitos.",
            "Die Kaiserliche Suite, 500 Zlitos.",
            "La Suite Imperial, 500 zlitos.",
            "La Suite Imperiale, 500 zlitos.",
            "A Suite Imperial, 500 zlitos.",
        },
        new[]
        {
            "Not tonight, thank you.",
            "Pas ce soir, merci.",
            "Heute nicht, danke.",
            "Esta noche no, gracias.",
            "Stasera no, grazie.",
            "Hoje não, obrigado.",
        },
        new[]
        {
            "Your key, sir. The lift is right beside me -- your room is on the first floor. Sleep well.",
            "Votre clé, monsieur. L'ascenseur est juste à côté de moi -- votre chambre est au premier étage. Dormez bien.",
            "Ihr Schlüssel, der Herr. Der Aufzug ist gleich neben mir -- Ihr Zimmer liegt im ersten Stock. Schlafen Sie gut.",
            "Su llave, señor. El ascensor está justo a mi lado: su habitación está en el primer piso. Que descanse.",
            "La sua chiave, signore. L'ascensore è proprio accanto a me: la sua camera è al primo piano. Dorma bene.",
            "A sua chave, senhor. O elevador está mesmo ao meu lado -- o seu quarto fica no primeiro andar. Durma bem.",
        },
        new[]
        {
            "I'm afraid that's more zlitos than you have, sir. The Casino's machines are just around the corner.",
            "Je crains que ce soit plus de zlitos que vous n'en avez, monsieur. Les machines du Casino sont juste au coin de la rue.",
            "Ich fürchte, das sind mehr Zlitos, als Sie haben, der Herr. Die Automaten des Casinos sind gleich um die Ecke.",
            "Me temo que son más zlitos de los que tiene, señor. Las máquinas del Casino están a la vuelta de la esquina.",
            "Temo che siano più zlitos di quanti ne abbia, signore. Le macchinette del Casinò sono proprio dietro l'angolo.",
            "Receio que sejam mais zlitos do que tem, senhor. As máquinas do Casino ficam mesmo ao virar da esquina.",
        },
        new[]
        {
            "Welcome to the Imperial Hotel, sir. Our rooms are let from the evening.",
            "Bienvenue à l'Hôtel Impérial, monsieur. Nos chambres se louent à partir du soir.",
            "Willkommen im Kaiserlichen Hotel, der Herr. Unsere Zimmer vermieten wir ab dem Abend.",
            "Bienvenido al Hotel Imperial, señor. Nuestras habitaciones se alquilan a partir de la tarde.",
            "Benvenuto all'Hotel Imperiale, signore. Le nostre camere si affittano dalla sera.",
            "Bem-vindo ao Hotel Imperial, senhor. Os nossos quartos alugam-se a partir do fim da tarde.",
        },
        new[]
        {
            "Good morning, sir. I trust you slept well?",
            "Bonjour, monsieur. Vous avez bien dormi, j'espère ?",
            "Guten Morgen, der Herr. Ich hoffe, Sie haben gut geschlafen?",
            "Buenos días, señor. ¿Ha dormido bien?",
            "Buongiorno, signore. Spero abbia dormito bene?",
            "Bom dia, senhor. Espero que tenha dormido bem?",
        },
        new[]
        {
            "The lift's for the hotel's guests. I'd need a room first.",
            "L'ascenseur est réservé aux clients de l'hôtel. Il me faudrait d'abord une chambre.",
            "Der Aufzug ist für die Hotelgäste. Ich bräuchte erst ein Zimmer.",
            "El ascensor es para los huéspedes del hotel. Primero necesitaría una habitación.",
            "L'ascensore è per gli ospiti dell'hotel. Prima mi servirebbe una camera.",
            "O elevador é para os hóspedes do hotel. Primeiro precisava de um quarto.",
        },
        new[]
        {
            "Locked. A \"Do not disturb\" sign hangs on the handle.",
            "Fermé à clé. Un panneau « Ne pas déranger » pend à la poignée.",
            "Abgeschlossen. Ein Schild „Bitte nicht stören“ hängt an der Klinke.",
            "Cerrado con llave. Un cartel de «No molestar» cuelga del pomo.",
            "Chiuso a chiave. Un cartello «Non disturbare» pende dalla maniglia.",
            "Fechado à chave. Um aviso «Não incomodar» está pendurado na maçaneta.",
        },
        new[]
        {
            "Time to get some sleep. Big race tomorrow.",
            "Il est temps de dormir. Grande course demain.",
            "Zeit zu schlafen. Morgen ist das große Rennen.",
            "Hora de dormir. Mañana es la gran carrera.",
            "È ora di dormire. Domani c'è la grande gara.",
            "Hora de dormir. Amanhã é a grande corrida.",
        },
        new[]
        {
            "A narrow bed, but a good one. I slept well enough. Now -- my car.",
            "Un lit étroit, mais un bon lit. J'ai assez bien dormi. Maintenant -- ma voiture.",
            "Ein schmales Bett, aber ein gutes. Ich habe ganz gut geschlafen. Jetzt -- mein Auto.",
            "Una cama estrecha, pero buena. He dormido bastante bien. Y ahora, mi coche.",
            "Un letto stretto, ma buono. Ho dormito abbastanza bene. E ora, la mia auto.",
            "Uma cama estreita, mas boa. Dormi razoavelmente bem. Agora -- o meu carro.",
        },
        new[]
        {
            "Now that's what I call a bed! I slept like a log. Ready for anything.",
            "Ça, c'est un lit ! J'ai dormi comme une souche. Prêt à tout.",
            "Das nenne ich ein Bett! Ich habe geschlafen wie ein Murmeltier. Bereit für alles.",
            "¡Eso sí que es una cama! He dormido como un tronco. Listo para lo que sea.",
            "Questo sì che è un letto! Ho dormito come un ghiro. Pronto a tutto.",
            "Isto é que é uma cama! Dormi como uma pedra. Pronto para tudo.",
        },
        new[]
        {
            "A four-poster bed... I slept like an emperor! The best night's sleep of my life.",
            "Un lit à baldaquin... j'ai dormi comme un empereur ! La meilleure nuit de ma vie.",
            "Ein Himmelbett ... ich habe geschlafen wie ein Kaiser! Die beste Nacht meines Lebens.",
            "Una cama con dosel... ¡he dormido como un emperador! La mejor noche de mi vida.",
            "Un letto a baldacchino... ho dormito come un imperatore! La notte migliore della mia vita.",
            "Uma cama de dossel... dormi como um imperador! A melhor noite da minha vida.",
        },
        new[]
        {
            "Do you know where I can get an ignition coil?",
            "Vous savez où je pourrais trouver une bobine d'allumage ?",
            "Wissen Sie, wo ich eine Zündspule bekomme?",
            "¿Sabe dónde puedo conseguir una bobina de encendido?",
            "Sa dove posso trovare una bobina d'accensione?",
            "Sabe onde posso arranjar uma bobina de ignição?",
        },
        new[]
        {
            "Maybe yes, maybe no, maybe I have one, maybe I could even let you have it, IF you do a small favour for me.",
            "Peut-être oui, peut-être non, peut-être que j'en ai une, peut-être que je pourrais même te la laisser, SI tu me rends un petit service.",
            "Vielleicht ja, vielleicht nein, vielleicht habe ich eine, vielleicht könnte ich sie dir sogar überlassen, WENN du mir einen kleinen Gefallen tust.",
            "Quizá sí, quizá no, quizá tenga una, quizá hasta te la deje, SI me haces un pequeño favor.",
            "Forse sì, forse no, forse ne ho una, forse potrei perfino dartela, SE mi fai un piccolo favore.",
            "Talvez sim, talvez não, talvez tenha uma, talvez até ta possa deixar, SE me fizeres um pequeno favor.",
        },
        new[]
        {
            "What's the favour?",
            "Quel service ?",
            "Was für ein Gefallen?",
            "¿Qué favor?",
            "Che favore?",
            "Que favor?",
        },
        new[]
        {
            "Take this letter to the singer at Rick's bar -- and don't you dare read it! Then bring me back her answer.",
            "Porte cette lettre à la chanteuse du bar de Rick -- et ne t'avise pas de la lire ! Puis rapporte-moi sa réponse.",
            "Bring diesen Brief der Sängerin in Ricks Bar -- und wehe, du liest ihn! Dann bring mir ihre Antwort.",
            "Lleva esta carta a la cantante del bar de Rick, ¡y ni se te ocurra leerla! Luego tráeme su respuesta.",
            "Porta questa lettera alla cantante del bar di Rick, e guai a te se la leggi! Poi riportami la sua risposta.",
            "Leva esta carta à cantora do bar do Rick -- e nem penses em lê-la! Depois traz-me a resposta dela.",
        },
        new[]
        {
            "Has she read it yet? Rick's bar, the singer. Go on -- and no peeking!",
            "Elle l'a lue ? Le bar de Rick, la chanteuse. Vas-y -- et pas de coup d'œil !",
            "Hat sie ihn schon gelesen? Ricks Bar, die Sängerin. Los -- und nicht reinschauen!",
            "¿La ha leído ya? El bar de Rick, la cantante. Venga, ¡y no mires!",
            "L'ha già letta? Il bar di Rick, la cantante. Vai, e non sbirciare!",
            "Ela já a leu? O bar do Rick, a cantora. Vá -- e nada de espreitar!",
        },
        new[]
        {
            "A letter for you -- from the gazogem seller.",
            "Une lettre pour vous -- de la part du vendeur de gazogem.",
            "Ein Brief für Sie -- vom Gazogem-Verkäufer.",
            "Una carta para usted, del vendedor de gazogem.",
            "Una lettera per lei, dal venditore di gazogem.",
            "Uma carta para si -- do vendedor de gazogem.",
        },
        new[]
        {
            "From him? Oh, the sweet man -- he's been writing me poems for a year... Wait, I'll write him an answer. Here, take it to him. And no peeking, Twinsen!",
            "De lui ? Oh, le pauvre chou -- ça fait un an qu'il m'écrit des poèmes... Attends, je vais lui répondre. Tiens, porte-lui ça. Et pas de coup d'œil, Twinsen !",
            "Von ihm? Ach, der Süße -- er schreibt mir seit einem Jahr Gedichte ... Warte, ich schreibe ihm eine Antwort. Hier, bring sie ihm. Und nicht reinschauen, Twinsen!",
            "¿De él? Ay, qué encanto de hombre: lleva un año escribiéndome poemas... Espera, le escribo una respuesta. Toma, llévasela. ¡Y no mires, Twinsen!",
            "Da lui? Oh, che tesoro: è un anno che mi scrive poesie... Aspetta, gli scrivo una risposta. Ecco, portagliela. E non sbirciare, Twinsen!",
            "Dele? Ah, que querido -- há um ano que me escreve poemas... Espera, vou escrever-lhe uma resposta. Toma, leva-lha. E nada de espreitar, Twinsen!",
        },
        new[]
        {
            "Well? Go on, take him my answer!",
            "Eh bien ? Vas-y, porte-lui ma réponse !",
            "Na? Los, bring ihm meine Antwort!",
            "¿Y bien? ¡Venga, llévale mi respuesta!",
            "Allora? Vai, portagli la mia risposta!",
            "Então? Vá, leva-lhe a minha resposta!",
        },
        new[]
        {
            "She answered! She... she'll have dinner with me! Twinsen, you're a true friend. The coil's yours -- for 10 zlitos. Business is business.",
            "Elle a répondu ! Elle... elle veut bien dîner avec moi ! Twinsen, tu es un vrai ami. La bobine est à toi -- pour 10 zlitos. Les affaires sont les affaires.",
            "Sie hat geantwortet! Sie ... sie geht mit mir essen! Twinsen, du bist ein wahrer Freund. Die Spule gehört dir -- für 10 Zlitos. Geschäft ist Geschäft.",
            "¡Ha contestado! Ella... ¡cenará conmigo! Twinsen, eres un amigo de verdad. La bobina es tuya, por 10 zlitos. Los negocios son los negocios.",
            "Ha risposto! Lei... cenerà con me! Twinsen, sei un vero amico. La bobina è tua, per 10 zlitos. Gli affari sono affari.",
            "Ela respondeu! Ela... ela vai jantar comigo! Twinsen, és um verdadeiro amigo. A bobina é tua -- por 10 zlitos. Negócios são negócios.",
        },
        new[]
        {
            "The ignition coil, for 10 zlitos?",
            "La bobine d'allumage, pour 10 zlitos ?",
            "Die Zündspule, für 10 Zlitos?",
            "¿La bobina de encendido, por 10 zlitos?",
            "La bobina d'accensione, per 10 zlitos?",
            "A bobina de ignição, por 10 zlitos?",
        },
        new[]
        {
            "There you are. Funny thing -- somebody dressed all in black and wearing a mask sold it to me yesterday.",
            "Voilà. Drôle de chose -- quelqu'un tout de noir vêtu et masqué me l'a vendue hier.",
            "Bitte schön. Komische Sache -- jemand ganz in Schwarz und mit einer Maske hat sie mir gestern verkauft.",
            "Aquí tienes. Qué curioso: alguien vestido todo de negro y con una máscara me la vendió ayer.",
            "Eccola. Strano, eh: qualcuno vestito tutto di nero e con una maschera me l'ha venduta ieri.",
            "Aqui tens. Coisa curiosa -- alguém todo vestido de preto e de máscara vendeu-ma ontem.",
        },
        new[]
        {
            "10 zlitos, friend. The Casino's machines pay out now and then.",
            "10 zlitos, l'ami. Les machines du Casino paient de temps en temps.",
            "10 Zlitos, mein Freund. Die Automaten im Casino zahlen ab und zu aus.",
            "10 zlitos, amigo. Las máquinas del Casino pagan de vez en cuando.",
            "10 zlitos, amico. Le macchinette del Casinò ogni tanto pagano.",
            "10 zlitos, amigo. As máquinas do Casino pagam de vez em quando.",
        },
        new[]
        {
            "All in black, and a mask... I'd keep an eye open for him if I were you.",
            "Tout en noir, et un masque... à ta place, je garderais l'œil ouvert.",
            "Ganz in Schwarz, mit Maske ... an deiner Stelle würde ich die Augen offen halten.",
            "Todo de negro y con máscara... Yo que tú, estaría atento.",
            "Tutto di nero, con una maschera... Se fossi in te, terrei gli occhi aperti.",
            "Todo de preto, e de máscara... No teu lugar, andava de olho aberto.",
        },
        new[]
        {
            "An ignition coil? Not behind my bar. Try the gazogem seller in the Lower City -- he deals in car parts on the side.",
            "Une bobine d'allumage ? Pas derrière mon bar. Essaie le vendeur de gazogem de la Ville Basse -- il trafique des pièces auto à côté.",
            "Eine Zündspule? Nicht hinter meiner Bar. Versuch's beim Gazogem-Verkäufer in der Unterstadt -- der handelt nebenbei mit Autoteilen.",
            "¿Una bobina de encendido? Detrás de mi barra no. Prueba con el vendedor de gazogem de la Ciudad Baja: trapichea con piezas de coche.",
            "Una bobina d'accensione? Non dietro il mio bancone. Prova dal venditore di gazogem della Città Bassa: traffica in pezzi d'auto.",
            "Uma bobina de ignição? Atrás do meu balcão não. Experimenta o vendedor de gazogem da Cidade Baixa -- ele vende peças de carro por fora.",
        },
        new[]
        {
            "Rocket parts I've got, kid. Car parts, no. Ask the gazogem seller in the Lower City.",
            "Des pièces de fusée, j'en ai, petit. Des pièces auto, non. Demande au vendeur de gazogem de la Ville Basse.",
            "Raketenteile hab ich, Kleiner. Autoteile nicht. Frag den Gazogem-Verkäufer in der Unterstadt.",
            "Piezas de cohete tengo, chaval. De coche, no. Pregunta al vendedor de gazogem de la Ciudad Baja.",
            "Pezzi di razzo ne ho, ragazzo. Di auto no. Chiedi al venditore di gazogem della Città Bassa.",
            "Peças de foguetão tenho, miúdo. De carro, não. Pergunta ao vendedor de gazogem da Cidade Baixa.",
        },
        new[]
        {
            "No coil, no start -- that's the Cup's rule. The gazogem seller in the Lower City might have one. He sees everything from that stall.",
            "Pas de bobine, pas de départ -- c'est la règle de la Coupe. Le vendeur de gazogem de la Ville Basse en a peut-être une. Il voit tout depuis son étal.",
            "Keine Spule, kein Start -- das ist die Regel des Pokals. Der Gazogem-Verkäufer in der Unterstadt hat vielleicht eine. Von seinem Stand aus sieht er alles.",
            "Sin bobina no hay salida: es la norma de la Copa. Puede que el vendedor de gazogem de la Ciudad Baja tenga una. Desde su puesto lo ve todo.",
            "Niente bobina, niente partenza: è la regola della Coppa. Forse il venditore di gazogem della Città Bassa ne ha una. Dal suo banco vede tutto.",
            "Sem bobina, não há partida -- é a regra da Taça. O vendedor de gazogem da Cidade Baixa talvez tenha uma. Daquela banca vê tudo.",
        },
        new[]
        {
            "The hotel can't help with cars, I'm afraid, sir. The gazogem seller in the Lower City, perhaps?",
            "L'hôtel ne peut rien pour les voitures, monsieur, j'en ai peur. Le vendeur de gazogem de la Ville Basse, peut-être ?",
            "Mit Autos kann Ihnen das Hotel leider nicht helfen, der Herr. Vielleicht der Gazogem-Verkäufer in der Unterstadt?",
            "Me temo que el hotel no puede ayudarle con coches, señor. ¿Quizá el vendedor de gazogem de la Ciudad Baja?",
            "Temo che l'hotel non possa aiutarla con le auto, signore. Forse il venditore di gazogem della Città Bassa?",
            "Receio que o hotel não possa ajudar com carros, senhor. Talvez o vendedor de gazogem da Cidade Baixa?",
        },
        new[]
        {
            "Not now, honey, I'm on! Wait for me in the back.",
            "Pas maintenant, mon chou, je suis en scène ! Attends-moi dans l'arrière-salle.",
            "Nicht jetzt, Schätzchen, ich bin dran! Warte hinten auf mich.",
            "Ahora no, cariño, ¡estoy actuando! Espérame en la trastienda.",
            "Non adesso, tesoro, sono in scena! Aspettami nel retro.",
            "Agora não, querido, estou em palco! Espera por mim lá atrás.",
        },
        new[]
        {
            "Welcome to the Imperial Hotel, sir. For a room, the reception's by the lift.",
            "Bienvenue à l'Hôtel Impérial, monsieur. Pour une chambre, la réception est près de l'ascenseur.",
            "Willkommen im Kaiserlichen Hotel, der Herr. Zimmer gibt es an der Rezeption beim Aufzug.",
            "Bienvenido al Hotel Imperial, señor. Para una habitación, la recepción está junto al ascensor.",
            "Benvenuto all'Hotel Imperiale, signore. Per una camera, la reception è vicino all'ascensore.",
            "Bem-vindo ao Hotel Imperial, senhor. Para um quarto, a receção é junto ao elevador.",
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
                // (race day's word on the coil only where the start line is -- "When Twinsen arrives at the race track the following day")
                var atStart = (int)Math.Floor(spot.X / 64) == model.CubeX && (int)Math.Floor(spot.Z / 64) == model.CubeY;
                if (!atStart) spot = (model.CubeX * 64 + 32, model.CubeY * 64 + 32, report.StartLine[0].Y, 0);
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
    if ({DayQualified} == var_game({OtringalDay}) && 0 == var_game({QNight}) && {Driving} != comportement_hero())
    {{
        cinema_mode(1);
        message({Say(OtringalLine.MarshalHeld)});
        set_var_game({QNight}, {NightTold});
        cinema_mode(0);
    }}{(atStart ? $@"
    if ({DayRaceDay} == var_game({OtringalDay}) && 0 == var_game({QCoil}) && 6000 > distance(0) && {Driving} != comportement_hero())
    {{
        cinema_mode(1);
        message({Say(OtringalLine.MarshalCoil)});
        message({Say(OtringalLine.MarshalNoCoil)});
        set_var_game({QCoil}, {CoilTold});
        cinema_mode(0);
    }}" : "")}
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
                    return IslandBeatsA.Add(model, johnny, Talker(otringal, CoilAsked(Say, OtringalLine.JohnnyCoil) + $@"            if ({FeeTold} == var_game({QFee}))
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

        // Baldino stays on Twinsun (the user, 2026-11-19: "we're leaving Baldino behind on Twinsun"): not the game's Baldino by his crashed
        // lander (scene 138's actor 2, from chapter 6: the gazogem errand, the protopack made the jet-pack), nor Twinsen's walk to him as he
        // lands (its hero, from chapter 4: a cut-scene that would wait on him for ever), nor his radio calls at the harbour that he has tuned
        // the car on Otringal (scenes 87's and 209's actor 6). The hoverboard flies here with no help of his -- the gravity's lighter off
        // Twinsun (Citadel/CitadelHoverboard). (His own calls from home, the radio's on arriving, stay.)
        if (b.Store.SceneExists(BaldinoLanding))
            b.Edit(BaldinoLanding, "Baldino stays on Twinsun: not by his lander, nor Twinsen's walk to him", _ => scripts =>
            {
                scripts.SetText(LandedBaldino, ScriptKind.Life, Loose(scripts.GetText(LandedBaldino, ScriptKind.Life),
                    "if (6 > chapter())\n{\nsuicide();", "if (0 <= chapter())\n{\nsuicide();"));
                var hero = Loose(scripts.GetText(0, ScriptKind.Life), "if (3 < chapter() && 11 > var_game(85))", "if (0 > chapter())");
                hero = Loose(hero, "if (0 == var_cube(0) && 1300 > distance_message(2))", "if (0 > chapter())");
                scripts.SetText(0, ScriptKind.Life, hero);
            });
        foreach (var harbour in new[] { 87, 209 })
            if (b.Store.SceneExists(harbour))
                b.Edit(harbour, "Baldino stays on Twinsun: no radio call that he has tuned the car on Otringal", _ => scripts =>
                {
                    var calls = 0;
                    for (var a = 1; a < scripts.ActorCount; a++)
                    {
                        var life = scripts.GetText(a, ScriptKind.Life);
                        if (!life.Contains(BaldinoCalls, StringComparison.Ordinal)) continue;
                        scripts.SetText(a, ScriptKind.Life, life.Replace(BaldinoCalls, "0 > chapter()", StringComparison.Ordinal));
                        calls++;
                    }
                    if (calls != 1) throw new NotTheGames($"Baldino's radio calls in scene {harbour}: {calls} actors, not 1");
                });

        // the eggs: Rick in the Casino, Lyra by the hotel's pool (RaceTrackStory.Eggs.Otringal.cs)
        RickAtTheCasino(b, otringal, Say(OtringalLine.RickCup), Say(OtringalLine.RickTracks), Say(OtringalLine.RickKid), Say(OtringalLine.RickLyra), Say(OtringalLine.RickBlackCar),
            CoilAsked(Say, OtringalLine.RickCoil));
        LyraPoolScore(b, otringal, Say(OtringalLine.LyraNine), Say(OtringalLine.LyraEight), CoilAsked(Say, OtringalLine.LyraCoil) + LyraTalk(Say));
        // (the two days: the witness and the coil -- RaceTrackStory.OtringalDays.cs)
        OtringalDays(b, report, Say, otringal);
        // (the night at the Imperial Hotel, the lift and its upper floor, and the coil's fetch quest: RaceTrackStory.OtringalNight)
        OtringalNight(b, Say, otringal);

        b.Save();
        log.Add($"Otringal's race file: gate={QFee} {FeePaid} (Lyra paid), qualifying the first day (qualify_stop={OtringalDay} 0 {DayQualified}), the race the next once the coil is found (gate_not={OtringalDay} {DayRaceDay}), win={QFee} {FeeWon}");
        return log;
    }
}
