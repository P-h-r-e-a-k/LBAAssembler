namespace LBAAssembler.Terrain;

// Moon's retold lines (IslandRetell): the game's text each replaces, then its words in English, French, German, Spanish,
// Italian, Portuguese -- TEXT.HQR's own order. Written for the story of docs/LBRA_WALKTHROUGH.md (2026-11-03).
internal static partial class IslandRetell
{
    public static readonly (int Retail, string[] Words)[] Moon =
    {
        (1, new[]
        {
            "Quickly! The race is about to start!!!",
            "Vite ! La course va commencer !!!",
            "Schnell! Das Rennen fängt gleich an!!!",
            "¡Deprisa! ¡¡¡La carrera va a empezar!!!",
            "Presto! La corsa sta per cominciare!!!",
            "Rápido! A corrida vai começar!!!",
        }),
        (256, new[]
        {
            "Racers check in at the welcoming building, please: no pass, no grid.",
            "Pilotes, merci de vous présenter au bâtiment d'accueil : pas de laissez-passer, pas de grille.",
            "Rennfahrer melden sich bitte im Empfangsgebäude an: ohne Paß kein Platz in der Startaufstellung.",
            "Los pilotos deben registrarse en el edificio de recepción, por favor: sin salvoconducto, no hay parrilla.",
            "I piloti si registrino al servizio accoglienza dell'immobile, per favore: niente lasciapassare, niente griglia.",
            "Pilotos, registrem-se na recepção, por favor: sem passe, sem grid de largada.",
        }),
        (506, new[]
        {
            "Go to the welcoming building and get a racer's pass. Then go to the base's main entrance: the door with the green light. Green means open.",
            "Allez au bâtiment d'accueil et prenez un laissez-passer de pilote. Ensuite, allez à l'entrée principale de la base : la porte à la lumière verte. Vert signifie ouvert.",
            "Gehen Sie in das Empfangsgebäude und holen Sie sich einen Rennfahrerpaß. Nehmen Sie anschließend den Haupteingang des Stützpunkts: die Tür mit dem grünen Licht. Grün bedeutet offen.",
            "Vaya al edificio de recepción y consiga un salvoconducto de piloto. Después, vaya a la entrada principal de la base: la puerta de luz verde. Verde significa abierta.",
            "Andate al servizio accoglienza dell'immobile e prendete un lasciapassare da pilota. Poi andate all'entrata principale della base: la porta con la luce verde. Verde significa aperta.",
            "Vá até a recepção para obter um passe de piloto. Em seguida, vá até a entrada principal da base: a porta com a luz verde. Verde quer dizer aberta.",
        }),
        (508, new[]
        {
            "Only the welcoming building opens the doors, and only HAL works them. If you want in, ask them to open!",
            "Seul le bâtiment d'accueil ouvre les portes, et seul Hal les actionne. Si vous voulez entrer, demandez-leur d'ouvrir !",
            "Nur das Empfangsgebäude öffnet die Türen, und nur HAL bedient sie. Wenn Sie hinein wollen, müssen Sie dort fragen, ob man Ihnen öffnet!",
            "Sólo el edificio de recepción abre las puertas, y sólo HAL las maneja. ¡Si quiere entrar, pida que le abran!",
            "Solo il servizio accoglienza dell'immobile apre le porte, e solo HAL le manovra. Se volete entrare, chiedete che vi aprano!",
            "Apenas a recepção abre as portas, e apenas o HAL as controla. Se quiser entrar, peça que eles abram!",
        }),
        (510, new[]
        {
            "Twinsen!! You made it at last! Welcome to my old cell: the Esmers kept me here once, and now they let me use it as a garage. They've even built the moon a race track!",
            "Twinsen !! Enfin tu es là ! Bienvenue dans mon ancienne cellule : les Esmers m'y ont enfermé autrefois, et maintenant ils me la prêtent comme garage. Ils ont même construit un circuit sur la lune !",
            "Twinsen!! Endlich bist du da! Willkommen in meiner alten Zelle: die Esmerier haben mich hier einmal eingesperrt, und jetzt darf ich sie als Garage benutzen. Sie haben dem Mond sogar eine Rennstrecke gebaut!",
            "¡¡Twinsen!! ¡Por fin has llegado! Bienvenido a mi vieja celda: los esmeres me tuvieron aquí encerrado, y ahora me la dejan usar como garaje. ¡Hasta le han construido un circuito a la luna!",
            "Twinsen!! Sei qui finalmente! Benvenuto nella mia vecchia cella: gli Esmers mi ci tenevano prigioniero, e adesso me la lasciano usare come garage. Hanno perfino costruito una pista sulla luna!",
            "Twinsen!! Finalmente você chegou! Bem-vindo à minha antiga cela: os Esmers me prenderam aqui uma vez, e agora me deixam usá-la como garagem. Eles até construíram uma pista de corrida na lua!",
        }),
        (511, new[]
        {
            "Here, Twinsen, take this translator: you'll understand the marshals on the moon and the people of Zeelich!!",
            "Tiens Twinsen, prends ce traducteur : tu comprendras les commissaires de la lune et les habitants de Zeelich !!",
            "Hier, Twinsen, nimm diesen Kommunikator: damit verstehst du die Streckenposten auf dem Mond und die Zeelicher!!",
            "Toma, Twinsen, coge este traductor: ¡¡entenderás a los comisarios de la luna y a la gente de Zeelich!!",
            "Tieni Twinsen, prendi questo traduttore: capirai i commissari di gara sulla luna e la gente di Zeelich!!",
            "Toma, Twinsen, pegue este tradutor: você vai entender os fiscais da lua e o povo de Zeelich!!",
        }),
        (514, new[]
        {
            "Twinsen! My lander's parked outside the crater, on the grid. Meet you there!",
            "Twinsen ! Mon vaisseau est garé à l'extérieur du cratère, sur la grille. On se retrouve là-bas !",
            "Twinsen! Mein Raumschiff steht außerhalb des Kraters, in der Startaufstellung. Wir sehen uns dort!",
            "¡Twinsen! Mi nave está aparcada fuera del cráter, en la parrilla. ¡Nos vemos allí!",
            "Twinsen! Il mio vascello è parcheggiato fuori dal cratere, sulla griglia. Ci vediamo lì!",
            "Twinsen! Meu módulo de pouso está estacionado fora da cratera, no grid de largada. Encontro você lá!",
        }),
        (516, new[]
        {
            "The door's jammed. HAL locked everything when his voice box broke. We have to find a way to open it.",
            "La porte est bloquée. Hal a tout verrouillé quand son boîtier vocal est tombé en panne. Il faut trouver comment l'ouvrir.",
            "Die Tür ist blockiert. HAL hat alles verriegelt, als sein Sprachmodul kaputtging. Wir müssen sie irgendwie aufbekommen.",
            "La puerta está bloqueada. HAL lo cerró todo cuando se le rompió la caja de voz. Habría que encontrar un medio para abrirla.",
            "La porta è bloccata. HAL ha chiuso tutto quando gli si è rotta la voce. Bisogna trovare il modo di aprirla.",
            "A porta está emperrada. O HAL trancou tudo quando a caixa de voz dele quebrou. Temos que descobrir um jeito de abri-la.",
        }),
        (520, new[]
        {
            "It's true: somebody's been using the moon's launch pad at night. A black car, no number, off to Zeelich. Strange thing for a racer to do.",
            "C'est vrai : quelqu'un utilise la rampe de lancement de la lune la nuit. Une voiture noire, sans numéro, en route pour Zeelich. Drôle d'idée pour un pilote.",
            "Es stimmt: jemand benutzt nachts die Startrampe des Mondes. Ein schwarzer Wagen ohne Startnummer, auf dem Weg nach Zeelich. Seltsam für einen Rennfahrer.",
            "Es cierto: alguien usa de noche la plataforma de lanzamiento de la luna. Un coche negro, sin número, rumbo a Zeelich. Qué cosa más rara para un piloto.",
            "E' vero: qualcuno usa la rampa di lancio della luna di notte. Una macchina nera, senza numero, diretta su Zeelich. Strano, per un pilota.",
            "É verdade: alguém anda usando a plataforma de lançamento da lua à noite. Um carro preto, sem número, rumo a Zeelich. Coisa estranha para um piloto fazer.",
        }),
        (521, new[]
        {
            "Come on, the race is about to start! The Esmers say the winner goes on to Zeelich. I'll see you on the grid!!",
            "Allez, la course va commencer ! Les Esmers disent que le vainqueur ira sur Zeelich. On se retrouve sur la grille !!",
            "Komm, das Rennen fängt gleich an! Die Esmerier sagen, der Sieger darf weiter nach Zeelich. Wir sehen uns in der Startaufstellung!!",
            "Vamos, ¡la carrera va a empezar! Los esmeres dicen que el ganador irá a Zeelich. ¡¡Nos vemos en la parrilla!!",
            "Forza, la corsa sta per cominciare! Gli Esmers dicono che il vincitore andrà su Zeelich. Ci vediamo sulla griglia!!",
            "Vamos, a corrida vai começar! Os Esmers dizem que o vencedor segue para Zeelich. Vejo você no grid de largada!!",
        }),
        (523, new[]
        {
            "We on Zeelich hear the Cup is coming our way next. A race in Otringal! If you win here, Twinsen, that's where you're going!!",
            "Sur Zeelich, on dit que la Coupe viendra chez nous ensuite. Une course à Otringal ! Si tu gagnes ici, Twinsen, c'est là que tu iras !!",
            "Wir auf Zeelich haben gehört, daß der Pokal als nächstes zu uns kommt. Ein Rennen in Otringal! Wenn Ihr hier gewinnt, Twinsen, dann geht es für Euch dorthin!!",
            "En Zeelich se dice que la Copa vendrá a nosotros después. ¡Una carrera en Otringal! Si ganas aquí, Twinsen, ¡¡allí es adonde irás!!",
            "Noi su Zeelich abbiamo sentito che la Coppa arriverà da noi. Una corsa a Otringal! Se vinci qui, Twinsen, è lì che andrai!!",
            "Nós, de Zeelich, ouvimos dizer que a Taça vem para o nosso lado depois. Uma corrida em Otringal! Se vencer aqui, Twinsen, é para lá que você vai!!",
        }),
        (524, new[]
        {
            "Win... the race... Twinsen...",
            "Gagne... la course... Twinsen...",
            "Gewinnt... das Rennen... Twinsen...",
            "Gana... la carrera... Twinsen...",
            "Vinci... la corsa... Twinsen...",
            "Vença... a corrida... Twinsen...",
        }),
        (525, new[]
        {
            "Come on, to the grid!!",
            "Allez, sur la grille !!",
            "Los, zur Startaufstellung!!",
            "¡¡Vamos, a la parrilla!!",
            "Forza, alla griglia!!",
            "Vamos, para o grid de largada!!",
        }),
    };
}
