namespace LBAAssembler.Terrain;

// Wannies's retold lines (IslandRetell): the game's text each replaces, then its words in English, French, German, Spanish,
// Italian, Portuguese -- TEXT.HQR's own order. Written for the story of docs/LBRA_WALKTHROUGH.md (2026-11-03).
internal static partial class IslandRetell
{
    public static readonly (int Retail, string[] Words)[] Wannies =
    {
        (10, new[]
        {
            "Do you know where the black car went? It came down the elevator before us.",
            "Vous savez où est passée la voiture noire ? Elle est descendue par l'ascenseur avant nous.",
            "Wissen Sie, wohin der schwarze Wagen gefahren ist? Er ist vor uns mit dem Aufzug heruntergekommen.",
            "¿Sabe usted adónde fue el coche negro? Bajó por el ascensor antes que nosotros.",
            "Sa dove è andata la macchina nera? E' scesa con l'ascensore prima di noi.",
            "Você sabe para onde foi o carro preto? Ele desceu pelo elevador antes de nós.",
        }),
        (11, new[]
        {
            "Have you seen a black car with a masked driver?",
            "Vous avez vu une voiture noire avec un pilote masqué ?",
            "Haben Sie einen schwarzen Wagen mit einem maskierten Fahrer gesehen?",
            "¿Ha visto un coche negro con un piloto enmascarado?",
            "Ha visto una macchina nera con un pilota mascherato?",
            "Você viu um carro preto com um piloto mascarado?",
        }),
        (12, new[]
        {
            "I asked Mr. Monk how to follow the Cup across the sea, and he told me to ask you about the ferryman.",
            "J'ai demandé à Monsieur le Moine comment suivre la Coupe de l'autre côté de la mer, et il m'a dit de vous demander à propos du passeur.",
            "Ich habe den Mönch gefragt, wie ich dem Pokal über das Meer folgen kann, und er hat gesagt, ich soll bei Ihnen nach dem Fährmann fragen.",
            "He preguntado al señor Monje cómo seguir a la Copa a través del mar y me dijo que le preguntara a usted por el barquero.",
            "Ho chiesto al Prete come fare per seguire la Coppa oltre il mare, e mi ha detto di chiederle qualcosa sul traghettatore.",
            "Perguntei ao senhor Frade como seguir a Taça através do mar, e ele me disse para te perguntar sobre o barqueiro.",
        }),
        (13, new[]
        {
            "I'm racing the Cup, and I'm after a black car with a masked driver. Can you help me out?",
            "Je cours la Coupe, et je cherche une voiture noire avec un pilote masqué. Pourriez-vous m'aider ?",
            "Ich fahre im Pokal mit, und ich bin hinter einem schwarzen Wagen mit einem maskierten Fahrer her. Können Sie mir helfen?",
            "Corro en la Copa y busco un coche negro con un piloto enmascarado. ¿Podría ayudarme?",
            "Corro la Coppa, e do la caccia a una macchina nera con un pilota mascherato. Potrebbe aiutarmi?",
            "Estou correndo a Taça, e procuro um carro preto com um piloto mascarado. Você pode me ajudar?",
        }),
        (14, new[]
        {
            "But, I am only here to race...",
            "Mais, je suis juste venu pour courir...",
            "Aber ich will doch nur Rennen fahren ...",
            "Pero si sólo he venido a correr...",
            "Ma io sono qui solo per correre...",
            "Mas, eu só estou aqui para correr...",
        }),
        (17, new[]
        {
            "Hey! Do you know where the miner lost his glove?",
            "Dis, sais-tu où le mineur a perdu son gant ?",
            "He! Weißt du, wo der Bergmann seinen Handschuh verloren hat?",
            "¡Oye! ¿Sabes dónde perdió el minero su guante?",
            "Ehi! Sai dove il minatore ha perso il suo guanto?",
            "Ei! Você sabe onde o mineiro perdeu a luva dele?",
        }),
        (30, new[]
        {
            "I went to watch the race practice. Be back soon. Signed: your housemaid.",
            "Je suis allée voir les essais de la course, je reviens très bientôt. Signé : votre employée de maison.",
            "Gehe nur kurz beim Renntraining zuschauen, bin gleich zurück. Unterschrift: Ihr Hausmädchen.",
            "He ido a ver los entrenamientos de la carrera. Vuelvo enseguida. Firmado: la asistenta",
            "Sono andata a vedere le prove della corsa, torno presto. Firmato: la sua donna di servizio.",
            "Fui assistir ao treino da corrida. Volto logo. Assinado: sua empregada doméstica.",
        }),
        (35, new[]
        {
            "Your Majesty? What brings you to the Wannies? I'm racing the Cup, and I'm after a black car with a masked driver. Has it been to your island?",
            "Majesté ? Que faites-vous chez les blafards ? Je cours la Coupe, et je cherche une voiture noire avec un pilote masqué. Est-elle passée sur votre île ?",
            "Majestät? Was führt Euch zu den Bleichlingen? Ich fahre im Pokal mit, und ich bin hinter einem schwarzen Wagen mit einem maskierten Fahrer her. War er auf Eurer Insel?",
            "¿Majestad? ¿Qué os trae a la tierra de los macilentos? Corro en la Copa y busco un coche negro con un piloto enmascarado. ¿Ha pasado por vuestra isla?",
            "Maestà? Cosa la porta dai Pallidi? Corro la Coppa, e do la caccia a una macchina nera con un pilota mascherato. E' passata dalla sua isola?",
            "Majestade? O que a traz à ilha dos blafards? Estou correndo a Taça, e procuro um carro preto com um piloto mascarado. Ele passou pela sua ilha?",
        }),
        (300, new[]
        {
            "The Monk threw our buddy Johnny out of the choir 'cause he hummed the race song in the hymn. Wasn't even true.",
            "Monsieur le Moine il a mis not copain Jeannot dehors de la chorale passe qu'il avait fredonné la chanson de la course pendant le cantique. C'était même pas vrai.",
            "Der Mönch hat unseren Freund Jan aus dem Chor geworfen, weil er in der Hymne das Rennlied gesummt hat. Das hat gar nich' gestimmt.",
            "El señor Monje ha echado del coro a nuestro compañero Juanito porque tarareó la canción de la carrera en el himno. Y ni siquiera era verdad.",
            "Quel Prete ha cacciato dal coro il nostro compagno Jeannot perché ha canticchiato la canzone della corsa durante l'inno. E non era neanche vero.",
            "O senhor Frade expulsou nosso amigo Jonas do coral porque ele cantarolou a música da corrida no hino. Nem é verdade.",
        }),
        (301, new[]
        {
            "Let's get this over with, so we can go and see the race.",
            "Oh ben vivement qu'on ait fini, qu'on puisse aller voir la course.",
            "Los, bringen wir's hinter uns, dann können wir uns das Rennen anschauen.",
            "Venga, vamos a acabar pronto con esto para ir a ver la carrera.",
            "Finiamola in fretta, così andiamo a vedere la corsa.",
            "Vamos acabar logo com isso, para podermos ir ver a corrida.",
        }),
        (305, new[]
        {
            "As for me, I'm real glad the Monk punished this Johnny. He had it coming, he was always making fun of my go-kart.",
            "Moi j'suis bien content que M'sieur le Moine il l'ait puni, le Jeannot. Il l'avait bien cherché, il arrêtait pas de se moquer de mon kart.",
            "Ich find' das ganz gut, daß der Mönch den Jan bestraft hat. Das hatte er verdient, er hat sich immer über mein Gokart lustig gemacht.",
            "Yo me alegro de que el señor Monje haya castigado al Juanito. Se lo tenía merecido, no paraba de burlarse de mi kart.",
            "Io sono proprio contento che il Prete lo abbia punito, quel Jeannot. Se l'è meritato, non faceva altro che prendere in giro il mio go-kart.",
            "Eu estou muito contente que o senhor Frade tenha punido esse Jonas. Ele mereceu, vivia debochando do meu kart.",
        }),
        (306, new[]
        {
            "I sure hope the frog swallowed this Johnny. Or, better yet, maybe they fed him to the ferryman!!",
            "J'espère que la grenouille l'a avalé, le Jeannot. Ou mieux, qu'ils l'ont donné à manger au passeur !!",
            "Hoffentlich hat der Frosch den Jan verschluckt. Oder noch besser, sie haben ihn dem Fährmann zum Fressen vorgeworfen!!",
            "Ojalá la rana se haya tragado al Juanito. ¡¡O mejor aún, que se lo hayan echado al barquero para que se lo comiera!!",
            "Spero proprio che la rana se lo sia ingoiato, quel Jeannot. O, meglio ancora, che l'abbiano dato da mangiare al traghettatore!!",
            "Espero que o sapo tenha engolido esse Jonas. Ou, melhor ainda, que o tenham dado como comida ao barqueiro!!",
        }),
        (310, new[]
        {
            "Look, this job is too much drudgery for us to want to talk about it! Ask us after the race!",
            "Ecoutez, ici le boulot est bien trop pénible pour qu'on ait envie d'en parler ! Revenez après la course !",
            "Hören Sie mal, die Arbeit hier ist viel zu mühsam, als daß wir darüber reden wollten! Fragen Sie uns nach dem Rennen!",
            "¡Mire, aquí el curro es tan pesado que no tenemos ni ganas de hablar! ¡Pregúntenos después de la carrera!",
            "Senta un po', qui il lavoro è troppo faticoso per avere voglia di parlarne! Ce lo chieda dopo la corsa!",
            "Escute, o trabalho aqui é tão pesado que não temos vontade de falar dele! Pergunte depois da corrida!",
        }),
        (315, new[]
        {
            "Here, there's no time for racing. All we do is dig. That's all.",
            "Ici, pas le temps de faire la course. On ne fait que creuser. C'est tout.",
            "Hier gibt es keine Zeit für Rennen. Wir graben nur. Das ist alles.",
            "Aquí no hay tiempo para carreras. No hacemos más que cavar. Sólo eso.",
            "Qui non c'è tempo per le corse. Non si fa altro che scavare. E' tutto.",
            "Aqui não há tempo para corridas. Só fazemos cavar. Nada mais.",
        }),
        (325, new[]
        {
            "Welcome to the Building Company, sir. If you're here to order a tunnel, we can take care of it right away. Cash only, any coin.",
            "Bienvenue à la Building Company monsieur. Si vous venez commander un tunnel, nous pouvons nous en occuper tout de suite. En liquide uniquement, toutes monnaies acceptées.",
            "Willkommen bei der Building Company, mein Herr. Wenn Sie einen Tunnel bestellen wollen, kümmern wir uns sofort darum. Nur gegen bar, jede Münze.",
            "Bienvenido a la Building Company, señor. Si viene a encargar un túnel, podemos atenderlo inmediatamente. Sólo al contado, cualquier moneda.",
            "Benvenuto alla Building Company, signore. Se viene per ordinare un tunnel, possiamo occuparci di lei immediatamente. Solo contanti, qualunque moneta.",
            "Bem-vindo à Building Company, senhor. Se o senhor está aqui para encomendar um túnel, podemos tratar disso imediatamente. Só em dinheiro, qualquer moeda.",
        }),
        (330, new[]
        {
            "Er... good day... sir... I'm the manager of this institution. You were let in without an appointment? I take it then you're here to order an obscenely long tunnel?",
            "Euh... bonjour... monsieur... Je suis le directeur de cet établissement. On vous a laissé entrer sans rendez-vous ? Peut-être venez-vous commander un tunnel d'une longueur indécente ?",
            "Äh... guten Tag ... mein Herr ... ich bin der Direktor dieses Unternehmens. Sie sind ohne Termin hereingekommen? Dann wollen Sie wohl einen unanständig langen Tunnel bestellen?",
            "Eh... buenos días... señor... Soy el director de este establecimiento. ¿Le han dejado entrar sin cita? ¿Acaso viene a encargar un túnel escandalosamente largo?",
            "Ehm... Buongiorno... signore... Sono il direttore di questa società. L'hanno fatta entrare senza appuntamento? Forse è venuto qui per ordinare un tunnel spropositatamente lungo?",
            "Eeé... bom dia... senhor... sou o diretor desse estabelecimento. Deixaram você entrar sem hora marcada? Então suponho que você veio encomendar um túnel obscenamente comprido?",
        }),
        (335, new[]
        {
            "Please leave now and close the door! We've got a tunnel to finish before the race. Here, as we say, \"Idle hands make the Devil's workshop!!!\"",
            "Sortez maintenant et fermez bien la porte ! Nous avons un tunnel à finir avant la course. Ici, comme on dit, \"L'oisiveté est la mère de tous les vices !!!\"",
            "Gehen Sie jetzt bitte, und schließen Sie die Tür! Wir müssen vor dem Rennen noch einen Tunnel fertigstellen. Hier sagen wir: \"Müßiggang ist aller Laster Anfang!!!\"",
            "¡Váyase ya y cierre la puerta, por favor! Tenemos que acabar un túnel antes de la carrera. ¡¡¡Aquí, como decimos, \"la ociosidad es la madre de todos los vicios\"!!!",
            "Ora se ne vada e chiuda bene la porta! Abbiamo un tunnel da finire prima della corsa. Qui, come si dice, \"L'ozio è il padre dei vizi!!!\"",
            "Saia agora e feche a porta, por favor! Temos um túnel para terminar antes da corrida. Aqui, como dizemos, \"Mente vazia, oficina do diabo!!!\"",
        }),
        (502, new[]
        {
            "Hello, racer. Won't you come in and share our meal? Nobody drives well on an empty stomach.",
            "Bonjour, pilote. Entrez donc et venez partager notre repas. Personne ne conduit bien le ventre vide.",
            "Guten Tag, Rennfahrer. Kommen Sie doch herein, und teilen Sie unser Mahl mit uns. Mit leerem Magen fährt keiner gut.",
            "Buenos días, piloto. ¿No quiere entrar y compartir nuestra comida? Nadie conduce bien con el estómago vacío.",
            "Buongiorno, pilota. Entri e venga a dividere con noi il nostro pranzo. Nessuno guida bene a stomaco vuoto.",
            "Olá, piloto. Entre e coma conosco. Ninguém dirige bem de barriga vazia.",
        }),
        (503, new[]
        {
            "So? How do you like my firefly tart? I race in one, you know.",
            "Alors ? Que pensez-vous de ma tarte aux lucioles ? J'en pilote une, vous savez.",
            "Und? Wie finden Sie meinen Glühwürmchenkuchen? Ich fahre selbst einen im Rennen, wissen Sie.",
            "¿Y? ¿Qué opina de mi tarta de luciérnagas? Yo corro en una, ¿sabe?",
            "Allora? Cosa ne pensa della mia torta di lucciole? Sa, io gareggio a bordo di una.",
            "E então? O que você achou da minha torta de pirilampos? Eu corro numa, sabia?",
        }),
        (505, new[]
        {
            "Feeling better? Well, you're the first racer I find who does not like my tart!! Wait till you see the one I drive!",
            "Ça va mieux ? Et bien, vous êtes le premier pilote que je vois qui n'aime pas ma tarte !! Attendez de voir celle que je conduis !",
            "Geht es Ihnen besser? Also, Sie sind der erste Rennfahrer, dem mein Kuchen nicht schmeckt!! Warten Sie erst, bis Sie den sehen, den ich fahre!",
            "¿Se encuentra mejor? ¡¡Pues es usted el primer piloto al que no le gusta mi tarta!! ¡Espere a ver la que conduzco!",
            "Va meglio? Ebbene, è il primo pilota che non ama la mia torta!! Aspetti di vedere quella che guido!",
            "Está melhor? Bem, você é o primeiro piloto que encontro que não gosta da minha torta!! Espere só até ver a que eu dirijo!",
        }),
        (506, new[]
        {
            "I hope those race people are not going to wreak havoc here. Since they built their track past the frog, I've had a tough time finding fireflies for my tarts.",
            "J'espère que ces gens de la course ne vont pas mettre du grabuge ici. Depuis qu'ils ont construit leur circuit au-delà de la grenouille, j'ai du mal à trouver des lucioles pour mes tartes.",
            "Ich hoffe, diese Rennleute richten hier kein Chaos an. Seit sie ihre Strecke am Frosch vorbei gebaut haben, ist es sehr schwer, Glühwürmchen für meinen Kuchen zu finden.",
            "Espero que la gente de la carrera no venga a armar gresca por aquí. Desde que construyeron su circuito más allá de la rana, me cuesta mucho trabajo encontrar luciérnagas para las tartas.",
            "Spero che quelli della corsa non vengano a mettere scompiglio qui. Da quando hanno costruito la loro pista oltre la rana, non riesco più a trovare delle lucciole per le mie torte.",
            "Espero que esse pessoal da corrida não venha tumultuar isso aqui. Desde que construíram a pista deles para lá do sapo, tenho dificuldades em encontrar pirilampos para minhas tortas.",
        }),
        (520, new[]
        {
            "Hey, pal! Say, if you bring me back a slice of tart, I'll tell you a pretty wacky secret. About a black car.",
            "Salut mon chtiot gars ! Dis voir, si tu me rapportes un morceau de tarte, j'te confierais un secret qu'est drôlement rigolo. A propos d'une voiture noire.",
            "Hallo, Junge! Hör mal, wenn du mir ein Stück Kuchen bringst, verrat' ich dir ein ganz verrücktes Geheimnis. Über einen schwarzen Wagen.",
            "¡Hola, mozuelo! Mira lo que te digo, si me traes un pedazo de tarta, te cuento un secreto que es la monda. Sobre un coche negro.",
            "Salve ragazzo mio! Dimmi un po', se mi porti un pezzo di torta, ti racconterò un segreto proprio strampalato. Su una macchina nera.",
            "Olá, amizade! Olha, se você me trouxer um pedaço de torta, te direi um segredo muito engraçado. Sobre um carro preto.",
        }),
        (521, new[]
        {
            "Oh, thanks a bunch, pal! Listen, them boys in the mine ain't digging for gems no more, they're digging a tunnel for some fella in a black car. And this kid, he give me the key to the chapel. Here, take it. I've had enough fun with it.",
            "Ho ben merci mon gars ! Ecoute, les gars d'la mine, y creusent plus pour les gemmes, y creusent un tunnel pour un type en voiture noire. Et pis un gamin m'a donné la clé de la chapelle. Tiens je t'la donne, maintenant je m'suis assez amusé.",
            "Oho, danke, mein Kleiner! Hör zu, die Jungs in der Mine graben nicht mehr nach Juwelen, die graben einen Tunnel für so 'nen Kerl in einem schwarzen Wagen. Und irgend so ein Junge hat mir den Schlüssel für die Kapelle gegeben. Hier, nimm ihn. Ich hab' genug Spaß damit gehabt.",
            "¡Gracias, gracias, mozuelo! Oye, los chicos de la mina ya no cavan buscando gemas, están cavando un túnel para un tipo de un coche negro. Y un chaval me dio la llave de la capilla. Mira, te la doy, ya me he divertido bastante.",
            "Oh, grazie mille, ragazzo mio! Ascolta, quei ragazzi della miniera non scavano più per le gemme, scavano un tunnel per un tizio con una macchina nera. E un ragazzino mi ha dato la chiave della cappella. Tieni, te la do, adesso mi sono divertito abbastanza.",
            "Ho, obrigado, amizade! Escuta, os rapazes da mina não tão mais cavando atrás de pedra preciosa, tão cavando um túnel pra um sujeito num carro preto. E um moleque me deu a chave da capela. Toma, fique com ela. Já me diverti bastante.",
        }),
        (522, new[]
        {
            "That was one hummer of a tart! My boy, 'e sez that I shouldn't eat none before the race 'cause it could make me sick. But that's all a bunch of hogwash. Phooey, I sez.",
            "Ha dis donc, elle était drôlement bonne cette tarte là ! Mon fiston, y dis que j'dois pas en manger avant la course passe que ça pourrait me rende malade. Mais tout ça c'est des balivernes. Pfff, que j'dis.",
            "Au Mann, der war ja phantastisch, der Kuchen! Mein Söhnchen sagt, ich darf vor dem Rennen nichts davon essen, weil ich davon krank werden könnte. Aber das ist alles nur Geschwätz. Pah, sag' ich.",
            "Lo que yo te diga, estaba de chuparse los dedos esa tarta. Mi hijito dice que no debo comerla antes de la carrera por aquello de que puedo ponerme malo. Pero eso son pamplinas. ¡Bah!, digo yo.",
            "Ah, dimmi un po', era una torta coi fiocchi! Mio figlio dice che non dovrei mangiarne prima della corsa perché mi potrebbe far male. Ma sono tutte sciocchezze. Bah, dico io.",
            "Olha, ela estava boa demais essa torta! Meu filho, ele diz que eu não devo comer antes da corrida porque posso ficar doente. Mas isso tudo é besteira. Ora bolas, digo eu.",
        }),
        (523, new[]
        {
            "Hi, pal! Hey, is that you who brought on all these racing cars? They don't look all that fast, if you ask me. In my day, it was different. We raced mine carts on firefly tarts, that really built us up!!!",
            "Salut mon gars ! Ben dis voir, c'est toi qu'a ramené toutes ces voitures de course ? Y ont pas l'air bien rapides, si tu veux mon avis. A mon époque, c'était pas pareil. On faisait la course en wagonnets, nourris à la tarte aux lucioles, alors forcément, on était baraqués !!!",
            "Tag, Junge! Sag mal, hast du die ganzen Rennwagen hierher gebracht? Die sehen mir nicht besonders schnell aus, wenn du mich fragst. Früher war das anders. Da fuhren wir Lorenrennen mit Glühwürmchenkuchen im Bauch, davon wurde man groß und kräftig!!!",
            "Hola, chaval. Dime una cosa, ¿has sido tú el que ha traído todos esos coches de carreras? Pues no me parecen muy rápidos, la verdad. En mis tiempos las cosas eran diferentes. ¡¡¡Corríamos en vagonetas a base de tarta de luciérnagas, y claro, menuda planta teníamos!!!",
            "Ciao ragazzo mio! Dimmi un po', sei stato tu a portare tutte queste macchine da corsa? Non hanno l'aria di essere molto veloci, se vuoi saperlo. Ai miei tempi, era diverso. Correvamo coi carrelli della miniera a torte di lucciole, quelle sì che ci facevano crescere forti!!!",
            "Olá, rapaz! Olha, foi você que trouxe todos esses carros de corrida? Eles não parecem lá muito rápidos, se quer saber. Na minha época, era diferente. A gente corria de vagonete à base de torta de pirilampos, isso sim deixava a gente robusto!!!",
        }),
        (524, new[]
        {
            "Say, I've heard that them Mosquibees won't let nobody onto their grid without a pass. Pretty sure the ones who'd give you one are hiding down the road from it.",
            "Dis donc, j'ai entendu dire que les mosquibees, y laissent personne monter sur leur grille sans laissez-passer. Moi chui sûr que ceux qui pourraient t'en donner un se cachent un peu plus loin que la grille, sur la route.",
            "Sag mal, ich hab' gehört, die Moskibienos lassen keinen ohne Passierschein in ihre Startaufstellung. Die, die dir einen geben würden, verstecken sich bestimmt ein Stück weiter die Straße runter.",
            "Oye una cosa, he oído que los mosquibitas no dejan entrar a nadie en su parrilla sin un pase. Me apuesto la cabeza a que los que te lo darían se esconden un poco más allá, siguiendo el camino.",
            "Dimmi un po', ho sentito dire che i Moschibei non fanno salire nessuno sulla loro griglia senza un lasciapassare. Sono quasi sicuro che quelli che te ne darebbero uno si nascondono lungo la strada, poco lontano da lì.",
            "Olha, ouvi dizer que os mosquibees não deixam ninguém entrar no grid de largada deles sem um passe. Tenho certeza de que quem pode te dar um está escondido mais adiante, na estrada que sai de lá.",
        }),
        (525, new[]
        {
            "My mommy, she's helping Mr. Monk get ready for the Cup's last race. When it's won, Dark Monk's gonna come back, Mr. Monk says, and heads are gonna roll! It'll be so awesome!! My mommy's strong!!",
            "Ma maman, et ben elle aide Monsieur le Moine à préparer la dernière course de la Coupe. Quand elle sera gagnée, Dark Monk va revenir, qu'il dit Monsieur le Moine, et des têtes vont tomber ! Ça sera vraiment trop bien !! Elle est drôlement forte ma maman !!",
            "Meine Mama, die hilft dem Mönch, das letzte Rennen des Pokals vorzubereiten. Wenn es gewonnen ist, kommt Dark Monk zurück, sagt der Mönch, und dann rollen Köpfe! Das wird so toll!! Meine Mama ist wahnsinnig stark!!",
            "Mi mamá, pues lo que hace es ayudar al señor Monje a preparar la última carrera de la Copa. Cuando se gane, Dark Monk volverá, dice el señor Monje, ¡y van a rodar cabezas! ¡¡Será genial!! ¡¡Mi mamá es muy muy fuerte!!",
            "La ma... mamma, beh lei aiuta il Prete a prepararsi per l'ultima corsa della Coppa. Quando sarà vinta, Dark Monk tornerà, dice il Prete, e salteranno delle teste! Sarà fortissimo!! E' veramente forte, la mia mamma!!",
            "Minha mamãe, ela está ajudando o senhor Frade a se preparar para a última corrida da Taça. Quando ela for vencida, o Dark Monk vai voltar, diz o senhor Frade, e cabeças vão rolar! Vai ser demais!! Minha mamãe é muito forte!!",
        }),
        (530, new[]
        {
            "A black car? Oh, my, that's far beyond me. You see, none of us have ever left our island or our beloved old mine. Though the men have been digging something new at night...",
            "Une voiture noire ? Hou la, c'est bien trop loin pour moi. Vous savez, ici aucun de nous n'a jamais quitté notre île et notre bonne vieille mine. Pourtant les hommes creusent quelque chose de nouveau la nuit...",
            "Ein schwarzer Wagen? Oh, das ist mir viel zu hoch. Wissen Sie, von uns hat noch keiner unsere Insel und die gute alte Mine verlassen. Aber nachts graben die Männer an etwas Neuem ...",
            "¿Un coche negro? ¡Uhhh, eso me queda muy lejos! No, mire, aquí ninguno de nosotros ha salido de esta isla ni de nuestra querida vieja mina. Aunque los hombres cavan algo nuevo por las noches...",
            "Una macchina nera? Oh, è una cosa troppo lontana per me. Sa, qui nessuno di noi ha mai lasciato l'isola e la nostra cara vecchia miniera. Però di notte gli uomini scavano qualcosa di nuovo...",
            "Um carro preto? Ai, isso é longe demais para mim. Sabe, nenhum de nós nunca saiu da nossa ilha nem da nossa querida velha mina. Mas os homens andam cavando uma coisa nova à noite...",
        }),
        (535, new[]
        {
            "A black car? Ah, yes, right. It went through before the race people came, sir, and it didn't stop for the frog. It's not on this island any more, they say. I've never actually seen it go, but this rumour has an air of truth. Ask the old Wannie after the race, she sees everything.",
            "Une voiture noire ? Ah oui, c'est vrai. Elle est passée avant les gens de la course, monsieur, et elle ne s'est pas arrêtée pour la grenouille. Elle ne serait plus sur cette île, à ce qu'on dit. Je ne l'ai jamais vue partir, mais cette rumeur a l'air vraie. Demandez à la vieille blafarde après la course, elle voit tout.",
            "Ein schwarzer Wagen? Ach ja, richtig. Er ist durchgefahren, bevor die Rennleute kamen, mein Herr, und er hat am Frosch nicht angehalten. Er ist nicht mehr auf dieser Insel, sagt man. Ich habe ihn zwar nie wegfahren sehen, aber an dem Gerücht ist wohl etwas Wahres dran. Fragen Sie nach dem Rennen die alte Bleichlingsfrau, sie sieht alles.",
            "¿Un coche negro? Ah, sí, claro. Pasó antes de que llegara la gente de la carrera, señor, y no se paró ante la rana. Dicen que ya no está en esta isla. Yo nunca lo he visto irse, pero el rumor parece cierto. Pregúntele a la vieja macilenta después de la carrera, ella lo ve todo.",
            "Una macchina nera? Ah, sì, giusto. E' passata prima che arrivasse la gente della corsa, signore, e non si è fermata per la rana. Dicono che non sia più su quest'isola. Non l'ho mai vista partire, ma la voce pare fondata. Chieda alla vecchia Pallida dopo la corsa, lei vede tutto.",
            "Um carro preto? Ah, sim, isso mesmo. Ele passou antes de chegar o pessoal da corrida, senhor, e não parou no sapo. Dizem que ele não está mais nessa ilha. Eu nunca o vi partir, mas esse boato parece verdadeiro. Pergunte à velha blafard depois da corrida, ela vê tudo.",
        }),
        (536, new[]
        {
            "I'm in a bind, I've lost my glove by the frog. With it, I can catch the pieces of lava and the balls of fire, I can even pitch them back. No glove, no driving: safety first!",
            "Je suis bien embêté, j'ai perdu mon gant près de la grenouille. Avec lui je peux attraper les morceaux de lave et les boules de feu, je peux même les renvoyer. Pas de gant, pas de course : la sécurité d'abord !",
            "So ein Mist, ich habe meinen Handschuh beim Frosch verloren. Damit kann man Lava- und Feuerkugeln fangen und sogar zurückwerfen. Kein Handschuh, kein Rennen: Sicherheit geht vor!",
            "¡Qué lata, he perdido mi guante junto a la rana! Con él puedo coger trozos de lava y bolas de fuego, incluso puedo devolverlas. Sin guante no hay conducción: ¡la seguridad, lo primero!",
            "Che rabbia, ho perso il mio guanto vicino alla rana. Mi serve per prendere i pezzi di lava e le palle di fuoco, posso anche rilanciarle. Senza guanto non si guida: prima la sicurezza!",
            "Eu estou muito chateado, perdi a minha luva perto do sapo. Com ela posso pegar pedaços de lava e bolas de fogo, posso até jogá-las de volta. Sem luva, nada de dirigir: segurança em primeiro lugar!",
        }),
        (540, new[]
        {
            "The Monk, he threw me out of his choir. It's not fair, I only hummed the race song. So I hid his hymn book in the mine cart! It's racing today, ha!",
            "Monsieur le Moine il m'a mis dehors de sa chorale. C'est pas juste, j'avais juste fredonné la chanson de la course. Alors j'ai caché son livre de cantiques dans le wagonnet de la mine ! Il fait la course aujourd'hui, ha !",
            "Der Mönch hat mich aus dem Chor geworfen. Das ist nicht fair, ich hab' doch nur das Rennlied gesummt. Also habe ich sein Gesangbuch in der Lore versteckt! Die fährt heute Rennen, haha!",
            "El señor Monje me ha echado del coro. No es justo, sólo había tarareado la canción de la carrera. ¡Así que escondí su libro de himnos en la vagoneta de la mina! ¡Hoy le toca correr, ja!",
            "Il Prete mi ha buttato fuori dal suo coro. Non è giusto, avevo solo canticchiato la canzone della corsa. Allora ho nascosto il suo libro degli inni nel carrello della miniera. Oggi fa la corsa anche lui, ah!",
            "O senhor Frade me expulsou do coral. Não é justo, eu só cantarolei a música da corrida. Então escondi o livro de hinos dele no vagonete da mina! Hoje ele está correndo, ha!",
        }),
        (541, new[]
        {
            "I knew the Mosquibees live away from the rest, but now their Queen won't even let racers onto her island without a pass. It's being whispered she's hiding somebody from the Emperor's stewards!!",
            "Je savais que les mosquibees vivaient à l'écart des autres, mais maintenant leur Reine ne laisse même plus les pilotes venir sur son île sans laissez-passer. On murmure qu'elle cache quelqu'un aux commissaires de l'Empereur !!",
            "Ich wußte ja, daß die Moskibienos von allen anderen getrennt leben, aber jetzt läßt ihre Königin nicht einmal mehr Rennfahrer ohne Passierschein auf ihre Insel. Man munkelt, sie versteckt jemanden vor den Kommissaren des Kaisers!!",
            "Yo ya sabía que los mosquibitas vivían al margen de los demás, pero ahora su Reina ni siquiera deja entrar a los pilotos en su isla sin un pase. ¡¡Se rumorea que esconde a alguien de los comisarios del emperador!!",
            "Sapevo che i Moschibei vivevano in disparte, lontano dagli altri, ma adesso la loro Regina non fa nemmeno salire i piloti sulla sua isola senza un lasciapassare. Si sussurra che nasconda qualcuno ai commissari dell'Imperatore!!",
            "Eu sabia que os mosquibees viviam separados dos outros, mas agora a Rainha deles não deixa nem os pilotos entrarem na ilha sem um passe. Dizem à boca pequena que ela esconde alguém dos comissários do Imperador!!",
        }),
        (542, new[]
        {
            "I fish fireflies for the old Wannie's tarts. She even races in one, you know, a firefly tart.",
            "Moi et ben je chasse des lucioles pour les tartes de la vieille blafarde. Elle fait même la course dans une, vous savez, une tarte aux lucioles.",
            "Ich angle Glühwürmchen für den Kuchen der alten Bleichlingsfrau. Sie fährt sogar in einem Rennen mit, wissen Sie, in einem Glühwürmchenkuchen.",
            "Pues yo pesco luciérnagas para las tartas de la vieja macilenta. Hasta corre en una, ¿sabe?, en una tarta de luciérnagas.",
            "Io pesco delle lucciole per le torte della vecchia Pallida. Sa, lei corre perfino su una torta di lucciole.",
            "Eu, eu pesco pirilampos para as tortas da velha blafard. Ela até corre numa, sabia, uma torta de pirilampos.",
        }),
        (545, new[]
        {
            "I can always look for gems around here, there are none. Nothing doing, they can only be found in the mine. And yet, I was told the racers find them lying on the tracks on the other islands.",
            "J'ai beau chercher des gemmes par ici, il n'y en a pas. Décidément, on n'en trouve que dans la mine. Pourtant on m'a raconté que les pilotes en trouvaient sur les circuits des autres îles.",
            "Hier kann ich lange nach Juwelen suchen, hier gibt es keine. Da ist nichts zu machen, die gibt's nur in der Mine. Aber angeblich finden die Rennfahrer sie auf den anderen Inseln einfach so auf den Strecken.",
            "Por más que busco gemas no encuentro ninguna por aquí. Definitivamente sólo se encuentran en la mina. Sin embargo me dijeron que los pilotos se las encuentran tiradas en los circuitos de las otras islas.",
            "Mi affanno inutilmente a cercare delle gemme da queste parti, non ce ne sono. Niente da fare, si trovano solo nella miniera. Eppure mi hanno raccontato che i piloti le trovano per terra sulle piste delle altre isole.",
            "Eu já procurei pedras preciosas por aqui, não tem. Decididamente, elas só são encontradas na mina. Mas me disseram que os pilotos as encontram jogadas nas pistas das outras ilhas.",
        }),
        (550, new[]
        {
            "Of course, it's fairly simple. The ferryman takes racers too, if you pay him in gems. But first you have to call him. To do that, you have to stand at the end of the ferryman's pier and call him with this song. Listen to me and don't forget.",
            "Ah mais bien sûr, c'est très simple. Le passeur emmène aussi les pilotes, si vous le payez en gemmes. Mais d'abord, il vous faudra l'appeler. Pour ça, il faut vous placer au bout du quai du passeur, et l'appeler avec cette chanson. Ecoutez-moi et retenez bien.",
            "Aber natürlich, das ist ganz einfach. Der Fährmann nimmt auch Rennfahrer mit, wenn man ihn mit Juwelen bezahlt. Aber zuerst muß man ihn rufen. Dazu stellen Sie sich ans Ende des Landestegs des Fährmanns und rufen ihn mit diesem Lied. Hören Sie gut zu, und merken Sie es sich.",
            "Ah, pues claro, es muy sencillo. El barquero también lleva a los pilotos, si se le paga con gemas. Pero antes tendrá que llamarlo. Y para llamarlo tiene que ponerse al final del embarcadero del barquero y llamarlo con esta canción. Escúcheme y recuérdelo.",
            "Ah ma certo, è molto semplice. Il traghettatore porta anche i piloti, se lo si paga con delle gemme. Ma prima dovrà chiamarlo. Per poterlo fare, dovrà mettersi in fondo al molo del traghettatore e chiamarlo con questa canzone. Mi ascolti e non la dimentichi.",
            "Ah, mas claro, é bem simples. O barqueiro também leva pilotos, se você pagar com pedras preciosas. Mas primeiro, você tem que chamá-lo. Para isso, posicione-se no fim do cais do barqueiro e chame-o com esta canção. Me escute e lembre bem.",
        }),
        (552, new[]
        {
            "The Monk impatiently awaits the Cup's last race, round the statue on Celebration. It's a major event for our entire population. He says the winner's wish will end the curse which lies heavily upon Zeelich. We shall at last resume the course of our lives on a hospitable planet, not in this forlorn place.",
            "Monsieur le Moine attend impatiemment la dernière course de la Coupe, autour de la statue de la Célébration. C'est un événement capital pour toute notre population. Il dit que le voeu du vainqueur mettra fin à la malédiction qui pèse sur Zeelich. On va enfin reprendre le cours de nos vies sur une planète hospitalière, pas dans cet endroit désolé.",
            "Der Mönch wartet schon ganz ungeduldig auf das letzte Rennen des Pokals, rund um die Statue auf dem Eiland der Feste. Das wird ein furchtbar wichtiges Ereignis für unser ganzes Volk. Er sagt, der Wunsch des Siegers wird den Fluch beenden, der auf Zeelich lastet. Endlich können wir unser normales Leben wieder aufnehmen, auf einem freundlichen Planeten statt an diesem trostlosen Ort.",
            "El señor Monje espera con impaciencia la última carrera de la Copa, alrededor de la estatua de la Celebración. Es un acontecimiento capital para toda nuestra población. Dice que el deseo del ganador acabará con la maldición que pesa sobre Zeelich. Por fin podremos retomar el curso de nuestras vidas en un planeta acogedor y no en este inhóspito lugar.",
            "Il Prete aspetta con impazienza l'ultima corsa della Coppa, intorno alla statua della Celebrazione. E' un evento capitale per tutta la nostra popolazione. Dice che il desiderio del vincitore porrà fine alla maledizione che pesa su Zeelich. Potremo finalmente riprendere il corso delle nostre vite su un pianeta ospitale, invece che in questa terra di desolazione.",
            "O senhor Frade espera impacientemente a última corrida da Taça, em volta da estátua da Celebração. É um evento da maior importância para toda nossa população. Ele diz que o desejo do vencedor acabará com a maldição que pesa sobre Zeelich. Finalmente vamos retomar o curso de nossas vidas num planeta hospitaleiro, não nesse lugar devastado.",
        }),
        (555, new[]
        {
            "Very well. But first, being that you're a stranger and a racer, I must warn you.",
            "Très bien. Mais je dois d'abord te mettre en garde, car tu es étranger, et pilote.",
            "Na gut. Aber zunächst muß ich dich warnen, weil du hier fremd bist und ein Rennfahrer.",
            "Muy bien. Pero antes debo advertirte, porque eres extranjero y piloto.",
            "Va bene. Ma prima devo metterti in guardia, visto che sei straniero e pilota.",
            "Muito bem. Mas antes, como você é estrangeiro e piloto, tenho que te prevenir.",
        }),
        (556, new[]
        {
            "Once, long ago, our god, Dark Monk, condemned us, the people of Zeelich, to live forever on our planet which he then turned into hell so as to make it seem more of a real condemnation. Each of the four races received a fragment of a key. Now the Cup has made them its prizes, one for each island's race, and at the last race they will be reunited. And the winner's wish, say the Dark Monk's faithful, will bring him back to rid us of the curse and give Zeelich back its original appearance. Twinsen, do not dare interfere with the religious practice of our people. Or with that race.",
            "Il y a longtemps de cela, notre Dieu Dark Monk nous condamna, peuple de Zeelich, à vivre indéfiniment sur notre planète qu'il transforma en enfer pour rendre la condamnation plus réelle encore. Chacune des quatre races reçut un fragment de clé. Aujourd'hui la Coupe en a fait ses trophées, un pour la course de chaque île, et lors de la dernière course ils seront réunis. Et le voeu du vainqueur, disent les fidèles de Dark Monk, le fera revenir pour nous libérer de la malédiction et rendre à Zeelich son aspect originel. Twinsen, ne te mêle surtout pas des affaires religieuses de notre peuple. Ni de cette course.",
            "Vor langer Zeit verurteilte unser Gott Dark Monk uns, die Völker von Zeelich, dazu, ewig auf unserem Planeten zu leben, den er dann in eine Hölle verwandelte, damit die Strafe noch echter wirkte. Jedes der vier Völker erhielt ein Fragment eines Schlüssels. Nun hat der Pokal sie zu seinen Preisen gemacht, eines für das Rennen jeder Insel, und beim letzten Rennen werden sie wieder zusammengefügt. Und der Wunsch des Siegers, sagen die Gläubigen von Dark Monk, wird ihn zurückbringen, damit er uns von dem Fluch erlöst und Zeelich sein ursprüngliches Gesicht zurückgibt. Twinsen, wage es nicht, dich in die religiösen Angelegenheiten unseres Volkes einzumischen. Oder in dieses Rennen.",
            "Hace mucho tiempo, nuestro Dios Dark Monk nos condenó, pueblo de Zeelich, a vivir indefinidamente en nuestro planeta, que luego transformó en un infierno para que la condena pareciera más auténtica. Cada una de las cuatro razas recibió un fragmento de llave. Ahora la Copa los ha convertido en sus premios, uno por la carrera de cada isla, y en la última carrera se reunirán. Y el deseo del ganador, dicen los fieles de Dark Monk, lo hará volver para librarnos de la maldición y devolver a Zeelich su aspecto original. Twinsen, no te atrevas a entrometerte en los asuntos religiosos de nuestro pueblo. Ni en esa carrera.",
            "Successe molto tempo fa... il nostro Dio Dark Monk ci condannò, popolo di Zeelich, a vivere all'infinito sul nostro pianeta, che poi trasformò in inferno perché la condanna sembrasse più vera. Ognuna delle quattro razze ricevette un frammento di chiave. Ora la Coppa ne ha fatto i suoi premi, uno per la corsa di ogni isola, e all'ultima corsa saranno riuniti. E il desiderio del vincitore, dicono i fedeli di Dark Monk, lo farà tornare per liberarci dalla maledizione e restituire a Zeelich il suo aspetto originale. Twinsen, non osare immischiarti negli affari religiosi del nostro popolo. Né in quella corsa.",
            "Faz muito tempo, nosso deus, Dark Monk, condenou a nós, o povo de Zeelich, a viver para sempre nesse planeta que ele então transformou num inferno, para que a condenação parecesse ainda mais real. Cada uma das quatro raças recebeu uma parte da chave. Agora a Taça fez delas os seus prêmios, uma para a corrida de cada ilha, e na última corrida elas serão reunidas. E o desejo do vencedor, dizem os fiéis de Dark Monk, o trará de volta para nos livrar da maldição e devolver a Zeelich a sua aparência original. Twinsen, não ouse se envolver com as práticas religiosas do nosso povo. Nem com aquela corrida.",
        }),
        (557, new[]
        {
            "Then race, and go. The Cup will move on soon enough. The ferryman provides the only communication between the islands and the under-gas. Go see my housemaid. She shall tell you how to call him.",
            "Alors cours, et va-t'en. La Coupe repartira bien assez tôt. Le passeur assure la seule communication entre les îles du sous-gaz. Va trouver mon employée de maison, elle te dira comment l'appeler.",
            "Dann fahr dein Rennen, und geh. Der Pokal zieht schon bald weiter. Der Fährmann ist die einzige Verbindung zwischen den Inseln unter der Gaswolke. Geh zu meinem Hausmädchen. Sie wird dir sagen, wie du ihn rufen kannst.",
            "Entonces corre, y vete. La Copa seguirá su camino muy pronto. El barquero es el único que comunica las islas del sub-gas. Vete a buscar a mi asistenta. Ella te dirá cómo llamarlo.",
            "Allora corri, e vai. La Coppa ripartirà presto. Solo il traghettatore assicura la comunicazione tra le isole e il sotto-gas. Vai a trovare la mia donna di servizio e ti dirà come chiamarlo.",
            "Então corra, e vá embora. A Taça logo seguirá adiante. O barqueiro garante a única comunicação entre as ilhas e o sub-gás. Vá encontrar minha empregada doméstica. Ela te dirá como chamá-lo.",
        }),
        (558, new[]
        {
            "So it's you who won our fragment. You don't realise what you have done. Listen carefully.",
            "Alors c'est toi qui as gagné notre fragment. Tu ne te rends pas compte de ce que tu as fait. Ecoute moi bien.",
            "Du bist es also, der unser Fragment gewonnen hat. Du hast ja keine Ahnung, was du da getan hast. Hör mir gut zu.",
            "Así que has sido tú quien ha ganado nuestro fragmento. No te das cuenta de lo que has hecho. Escúchame bien.",
            "Allora sei tu che hai vinto il nostro frammento. Non ti rendi conto di quello che hai fatto. Ascoltami bene.",
            "Então foi você que ganhou a nossa parte da chave. Você não percebe o que fez. Me escute bem.",
        }),
        (559, new[]
        {
            "The Cup's last race is near. Dark Monk in person will chase away the monsters of the gas cloud when the four fragments are reunited at its finish line.",
            "La dernière course de la Coupe approche. Dark Monk en personne chassera les monstres du nuage de gaz lorsque les quatre fragments seront réunis sur sa ligne d'arrivée.",
            "Das letzte Rennen des Pokals naht. Dark Monk wird persönlich die Gasmonster verjagen, wenn die vier Fragmente an seiner Ziellinie vereint sind.",
            "La última carrera de la Copa se acerca. Dark Monk en persona expulsará a los monstruos de la nube de gas cuando se reúnan los cuatro fragmentos en su línea de meta.",
            "L'ultima corsa della Coppa si avvicina. Dark Monk in persona caccerà i mostri della nube di gas quando i quattro frammenti saranno riuniti al suo traguardo.",
            "A última corrida da Taça está chegando. Dark Monk em pessoa espantará os monstros da nuvem de gás quando as quatro partes da chave forem reunidas na sua linha de chegada.",
        }),
        (560, new[]
        {
            "Hmmf! Lost my glove somewhere in the mine. Now I can't reach into the lava to grab up the gems. Our champion lost his too, by the frog: it's catching!",
            "Pfff ! J'ai perdu mon gant quelque part dans la mine. Sans lui, je ne peux pas plonger mes mains dans la lave pour ramasser des gemmes. Notre champion a perdu le sien aussi, près de la grenouille : c'est contagieux !",
            "Hmpf! Ich habe meinen Handschuh irgendwo in der Mine verloren. Jetzt kann ich nicht mehr in die Lava fassen und Juwelen sammeln. Unser Champion hat seinen auch verloren, beim Frosch: das ist ansteckend!",
            "¡Humpf! He perdido mi guante en algún lugar de la mina. Sin él no voy a poder meter la mano en la lava para recoger gemas. Nuestro campeón también perdió el suyo, junto a la rana: ¡esto se contagia!",
            "Uff! Ho perso il mio guanto da qualche parte, nella miniera. Adesso non posso infilare le mani nella lava per raccogliere le gemme. Anche il nostro campione ha perso il suo, vicino alla rana: è contagioso!",
            "Hmmf! Perdi minha luva em algum lugar na mina. Agora não posso mergulhar as mãos na lava para catar pedras preciosas. Nosso campeão também perdeu a dele, perto do sapo: é contagioso!",
        }),
        (562, new[]
        {
            "The Cup's last race is near. Now that you have won our fragment, if the right driver doesn't win, Dark Monk will not chase the monsters of the gas or the cloud which blankets Zeelich!",
            "La dernière course de la Coupe approche. Maintenant que tu as gagné notre fragment, si le bon pilote ne gagne pas, Dark Monk ne chassera pas les monstres du gaz ni le nuage qui recouvre Zeelich !",
            "Das letzte Rennen des Pokals naht. Jetzt, wo du unser Fragment gewonnen hast: wenn nicht der richtige Fahrer gewinnt, wird Dark Monk weder die Gasmonster noch die Gaswolke verjagen, die Zeelich bedeckt!",
            "La última carrera de la Copa se acerca. ¡Ahora que has ganado tú nuestro fragmento, si no gana el piloto adecuado, Dark Monk no expulsará a los monstruos del gas ni la nube que recubre Zeelich!",
            "L'ultima corsa della Coppa si avvicina. Adesso che hai vinto il nostro frammento, se non vince il pilota giusto, Dark Monk non scaccerà più i mostri del gas né la nuvola che copre Zeelich!",
            "A última corrida da Taça está chegando. Agora que você ganhou nossa parte da chave, se o piloto certo não vencer, Dark Monk não espantará os monstros do gás nem a nuvem que cobre Zeelich!",
        }),
        (583, new[]
        {
            "Here, as a token of my gratitude, take my spare glove. It protects us from fire and lets us get the gems out of the molten lava without burning up. It's not the miner's, mind: he'll only drive in his own.",
            "Tiens, en signe de gratitude, prends mon gant de rechange. Il nous protège du feu et nous permet de sortir des gemmes de la lave en fusion sans nous brûler. Ce n'est pas celui du mineur, attention : il ne conduit qu'avec le sien.",
            "Hier, als Dank möchte ich dir meinen Ersatzhandschuh schenken. Er schützt uns gegen Feuer und ermöglicht es uns, die Juwelen aus der flüssigen Lava zu holen, ohne zu verbrennen. Es ist aber nicht der des Bergmanns: der fährt nur mit seinem eigenen.",
            "Toma, en agradecimiento, te doy mi guante de repuesto. Nos protege del fuego y nos permite sacar gemas de la lava fundida sin quemarnos. Pero no es el del minero, ojo: él sólo conduce con el suyo.",
            "Tieni, per ringraziarti, prendi il mio guanto di riserva. Protegge dal fuoco e permette di estrarre le gemme dalla lava bollente senza bruciarsi. Non è quello del minatore, attento: lui guida solo col suo.",
            "Olha, como prova da minha gratidão, pegue a minha luva reserva. Ela nos protege do fogo e com ela podemos extrair pedras preciosas da lava fervendo sem nos queimar. Não é a do mineiro, veja bem: ele só dirige com a dele.",
        }),
        (584, new[]
        {
            "Here, to thank you, take this gem. For luck in the race.",
            "Tiens, pour te remercier, prends cette gemme. Pour la chance pendant la course.",
            "Hier, nimm diesen Edelstein als Dank. Viel Glück beim Rennen.",
            "Toma, en agradecimiento, coge esta gema. Para que tengas suerte en la carrera.",
            "Tieni, per ringraziarti, prendi questa gemma. Porta fortuna in corsa.",
            "Toma, para te agradecer, pegue esta pedra preciosa. Para dar sorte na corrida.",
        }),
        (586, new[]
        {
            "Ah, thank you, thank you! I'd fallen on the track and couldn't get up, with the race people out testing the frog. What can I do to thank you?",
            "Ah merci, merci ! J'avais fait une chute sur le circuit et je n'arrivais plus à me relever, avec les gens de la course qui testaient la grenouille. Comment pourrais-je te remercier ?",
            "Oh, vielen Dank, vielen Dank! Ich war auf der Strecke gestürzt und konnte nicht mehr aufstehen, während die Rennleute den Frosch getestet haben. Wie kann ich dir meinen Dank zeigen?",
            "¡Ah, gracias, gracias! Me había caído en el circuito y no conseguía ponerme de pie, con la gente de la carrera probando la rana. ¿Cómo puedo agradecértelo?",
            "Ah grazie, grazie! Ero caduto sulla pista e non riuscivo più a rialzarmi, con quelli della corsa che provavano la rana. Come potrei ringraziarti?",
            "Ah, obrigado, obrigado! Eu tinha caído na pista e não conseguia me levantar, com o pessoal da corrida testando o sapo. O que posso fazer para te agradecer?",
        }),
        (587, new[]
        {
            "He lost it by the frog, at the foot of the tower. The frog opens its mouth every few seconds, and the glove's just inside. Wait for the mouth to open, then run in and out before it shuts.",
            "Il l'a perdu près de la grenouille, au pied de la tour. La grenouille ouvre la bouche toutes les quelques secondes, et le gant est juste à l'intérieur. Attends que la bouche s'ouvre, puis entre et ressors avant qu'elle se referme.",
            "Er hat ihn beim Frosch verloren, am Fuß des Turms. Der Frosch öffnet alle paar Sekunden das Maul, und der Handschuh liegt gleich dahinter. Warte, bis das Maul aufgeht, dann lauf hinein und wieder heraus, bevor es zuschnappt.",
            "Lo perdió junto a la rana, al pie de la torre. La rana abre la boca cada pocos segundos, y el guante está justo dentro. Espera a que abra la boca, y entra y sal corriendo antes de que la cierre.",
            "L'ha perso vicino alla rana, ai piedi della torre. La rana apre la bocca ogni pochi secondi, e il guanto è proprio lì dentro. Aspetta che la bocca si apra, poi entra ed esci prima che si chiuda.",
            "Ele a perdeu perto do sapo, ao pé da torre. O sapo abre a boca a cada poucos segundos, e a luva está logo ali dentro. Espere a boca abrir, então entre e saia correndo antes que ela feche.",
        }),
        (588, new[]
        {
            "Be careful Twinsen, that frog is hungry, and it doesn't care who's racing.",
            "Fais attention Twinsen, cette grenouille a faim, et elle se moque bien de savoir qui fait la course.",
            "Paß auf, Twinsen, dieser Frosch ist hungrig, und es ist ihm egal, wer gerade Rennen fährt.",
            "Ten cuidado, Twinsen, esa rana tiene hambre, y le da igual quién corra.",
            "Stai attento Twinsen, quella rana ha fame, e non le importa chi corre.",
            "Cuidado Twinsen, esse sapo está com fome, e ele não liga para quem está correndo.",
        }),
        (590, new[]
        {
            "Racers have no business here, get lost!!!",
            "Les pilotes n'ont rien à faire ici, dégagez !!!",
            "Weg da, Rennfahrer haben hier nichts zu suchen!!!",
            "¡¡Aquí los pilotos no pintan nada, lárguese!!!",
            "I piloti non hanno niente da fare qui, via!!!",
            "Pilotos não têm nada para fazer aqui, vão embora!!!",
        }),
        (600, new[]
        {
            "Easy, Twinsen, be careful. It's not the time to break the Cup's precious fragment!!",
            "Allons Twinsen, fais un peu attention. Ce n'est pas le moment de casser le précieux fragment de la Coupe !!",
            "Aber Twinsen, paß doch ein bißchen auf. Jetzt ist wirklich nicht der richtige Augenblick, das kostbare Fragment des Pokals kaputt zu machen!!",
            "Ojo, Twinsen, ten cuidado. ¡¡No es momento para romper el valioso fragmento de la Copa!!",
            "Piano, Twinsen, fai attenzione. Non è certo il momento di rompere il prezioso frammento della Coppa!!",
            "Calma, Twinsen, preste atenção. Não está na hora de quebrar a preciosa parte da chave da Taça!!",
        }),
        (608, new[]
        {
            "You're very brave, to speak to a Queen without an appointment. Somebody has dug a tunnel into my island, and this company won't tell me who paid for it. So I suppose I'll just stay here till they do. Here! Take the key to the tunnel! Go and win your race!!",
            "Tu es bien courageux de parler à une Reine sans rendez-vous. Quelqu'un a creusé un tunnel jusque dans mon île, et cette compagnie refuse de me dire qui l'a payé. Alors je resterai ici jusqu'à ce qu'elle me le dise. Tiens ! Prends la clé du tunnel ! Va gagner ta course !!",
            "Du bist sehr mutig, ohne Termin mit einer Königin zu sprechen. Jemand hat einen Tunnel in meine Insel gegraben, und dieses Unternehmen will mir nicht sagen, wer ihn bezahlt hat. Also bleibe ich wohl einfach hier, bis sie es tun. Hier! Nimm den Schlüssel für den Tunnel! Geh und gewinn dein Rennen!!",
            "Eres muy valiente para hablar con una Reina sin cita. Alguien ha cavado un túnel hasta mi isla, y esa compañía no quiere decirme quién lo pagó. Así que supongo que me quedaré aquí hasta que lo digan. ¡Toma! ¡Coge la llave del túnel! ¡¡Ve a ganar tu carrera!!",
            "Sei un prode, a parlare a una Regina senza appuntamento. Qualcuno ha scavato un tunnel fino alla mia isola, e questa società non vuole dirmi chi l'ha pagato. Quindi resterò qui finché non lo diranno. Tieni! Prendi la chiave del tunnel! Vai a vincere la tua corsa!!",
            "Você é muito corajoso, para falar com uma Rainha sem hora marcada. Alguém cavou um túnel até a minha ilha, e essa companhia não quer me dizer quem pagou por ele. Então acho que vou ficar aqui até que digam. Tome! Pegue a chave do túnel! Vá ganhar a sua corrida!!",
        }),
        (609, new[]
        {
            "Hurry, Twinsen!! The tunnel to my island starts behind the Wannies' village! But win their race first! Quickly!",
            "Dépêche-toi Twinsen !! Le tunnel qui mène à mon île part de derrière le village des blafards ! Mais gagne d'abord leur course ! Vite !",
            "Beeil dich, Twinsen!! Der Tunnel zu meiner Insel beginnt hinter dem Dorf der Bleichlinge! Aber gewinn zuerst ihr Rennen! Schnell!",
            "¡¡Date prisa, Twinsen!! ¡El túnel hacia mi isla empieza detrás del pueblo de los macilentos! ¡Pero gana antes su carrera! ¡Deprisa!",
            "Sbrigati Twinsen!! Il tunnel per la mia isola parte dietro il paese dei Pallidi! Ma prima vinci la loro corsa! Presto!",
            "Rápido, Twinsen!! O túnel para a minha ilha começa atrás da vila dos blafards! Mas ganhe a corrida deles primeiro! Depressa!",
        }),
        (616, new[]
        {
            "Only you can beat that masked driver.",
            "Il n'y a que toi pour battre ce pilote masqué.",
            "Nur du kannst diesen maskierten Fahrer noch schlagen.",
            "Sólo tú puedes vencer a ese piloto enmascarado.",
            "Solo tu puoi battere quel pilota mascherato.",
            "Só você pode vencer aquele piloto mascarado.",
        }),
        (617, new[]
        {
            "Don't stay here! Go and race while there's still a Cup to win!",
            "Ne reste pas ici ! Va courir tant qu'il reste une Coupe à gagner !",
            "Bleib' nicht hier! Fahr dein Rennen, solange es noch einen Pokal zu gewinnen gibt!",
            "¡No te quedes aquí! ¡Ve a correr mientras todavía quede una Copa que ganar!",
            "Non restare qui! Vai a correre finché c'è ancora una Coppa da vincere!",
            "Não fique aqui! Vá correr enquanto ainda há uma Taça para ganhar!",
        }),
    };
}
