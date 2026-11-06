using System.IO;
using System.Text.RegularExpressions;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The race mod's new story on Citadel Island (the user, 2026-11-03: "Let's take one island at a time and implement our new game story.
// Let's do both Citadel island files together"), as docs/LBRA_WALKTHROUGH.md tells chapters 1 and 2 (the build notes:
// docs/LBRA_STORY_SCRIPT.md). It goes on from the storm plot RaceTrackStory.Apply builds -- Zoe's opening, the Wizard, Raph and Mr. Paul at
// the storm track, Raph's time, the lighthouse, the town circuit -- and adds:
//   Chapter 1, the storm (CITADEL.ILE; chapter() 1 all chapter long):
//     - the driving gloves stolen from Twinsen's car, left on its seat under the carport (Zoe's opening; Q_THIEF 1). Tim the waiter on the Tavern's old doorstep (the
//       Tavern is gone from the storm's island file) saw the thief run to the harbour with them and a customer's umbrella (Q_THIEF 2);
//       Flick the marshal by her plinth says so too. The thief, the game's umbrella thief copied (scene 48's actor 6), waits on the
//       harbour's quay, below its warehouses (scene 43): "Can't catch me!", and he runs to a sewer hatch on the quay and down (Q_THIEF 3). The hatch is a
//       door of its own into the game's sewers (scene 17), which Twinsen comes into at the foot of the lift from the storm track's start
//       line, as from that lift. The sewers' painter has seen him go through to the far tunnel; the far tunnel's door (into scene 18, a
//       key's in the game) is open for the chase, and in that last room the thief is trapped (Q_THIEF 4). Action beside him: the gloves
//       (item 7), and he drops the umbrella (item 20) as he climbs out, up a ladder that brings Twinsen out on the quay (Q_THIEF 5). The
//       umbrella back to Tim: ten kashes (Q_THIEF 6). (The sewers are the 1996 demo's where the build put
//       them in, Demo96Rooms.Sewers: the hatch lands where the demo starts Twinsen, the thief cornered beside its far halls' lift.)
//     - the museum's blueprints (Q_BLUEPRINT 1, in a display case in scene 15) and the neighbour's design sheet (Q_SHEET 1, scene 9): seen now,
//       copied in chapter 2.
//     - the mayor has banned drink while the island has a race track, and the Tavern's regulars drink in secret in the Back Room, the
//       Tralu's caves' second hall (scene 19, made bigger: Citadel/TraluBarRoom): Tim and the mushroom man by the Wizard's tent say only
//       that nobody has seen Luc; Luc keeps the bar, Pat and Fab drum, the drunk rabbibunny and the visitor from the Tavern sit at the
//       massive table, and Tim serves there once his umbrella is back; Luc tells of the back gate's hidden key, and the Tralu grumbles in
//       his own hall next door (Citadel/RaceTrackStory.TraluTavern.cs, RaceTrackStory.TraluCaves.cs).
//     - the spell at the lighthouse no longer moves the game on to chapter 2 (the game's own script set chapter() there): the Wizard says
//       the sun comes in the morning, Raph goes to bed, Zoe saw lights over the clouds, and Twinsen is tired (DayVar Tired, everyone's
//       tired line; Tim's line of the lights). His bed: the night (DayVar Night, scene 0 started again in the dark).
//   Chapter 2, the town circuit (CITABAU.ILE: chapter() 2 from the morning on):
//     - the morning: Twinsen asleep in his bed, Zoe wakes him, the aliens have landed; inc_chapter, var 70 (the landing done: the game's
//       landing cutscene never runs), DayVar Rested. Out of the door is the fine weather's island.
//     - the Esmers' landing speech (RaceTrackStory.Apply step 5), three islanders round them, Raph outside the Tavern, Tim in it (the
//       umbrella on its hook, or not). If Twinsen goes home first, Zoe's word at the door (Q_LUCCAR 1).
//     - Luc in the yard by the carport, his barrel car's back wheel lost (Q_LUCCAR 2); Bob at the harbour scrap, the wheel in it (3); Zoe
//       fits it and gives the portable radio (item 26; Q_LUCCAR 4, which the town circuit's gate waits for: gate=214 4, RaceCarEngineFile).
//     - at the town circuit's start line the Esmer herald gives the magic slate (item 28), Flick and Mr. Paul want six cars on the grid.
//     - the slate's hidden pages: the blueprints copied onto it (Q_BLUEPRINT 2), the pharmacy's gallic acid for 50 kashes (item 17), the
//       neighbour's sheet brought out with it (Q_SHEET 2). The pages themselves -- their pictures, memo_ardoise -- are for later.
//     - Mr. Paul's prize (Apply step 6) is the ferry ticket and the Cup's purse; Mrs. Brune at the ferry office lets Zoe aboard as crew.
// The game's own lines that the story's people still say are IslandRetell's (it runs after this and repoints every one of the game's text
// ids); the lines here are the story's new ones, texts after RaceTrackStory's own at the end of Citadel's file.
internal static partial class RaceTrackStory
{
    // The story's quest variables (docs/LBRA_STORY_PLAN.md 3.1: 212-229 and 231-239 are no script's and not the engine's).
    // Q_THIEF: 1 Zoe has told, 2 Tim has told, 3 the thief down the hatch, 4 cornered in the sewers, 5 the gloves back (and the umbrella
    // found), 6 the umbrella given back to Tim
    public const int QThief = 213, ThiefTold = 1, TimTold = 2, ThiefDown = 3, ThiefCornered = 4, GlovesBack = 5, UmbrellaBack = 6;
    // Q_LUCCAR: 1 Zoe's word at the door read, 2 Luc has told, 3 the wheel found, 4 the wheel fitted (the town circuit's gate)
    public const int QLucCar = 214, LucWord = 1, LucTold = 2, WheelFound = 3, WheelFitted = 4;
    // DREAM_CAUGHT (1: Twinsen caught FunFrock in the dream -- Polar Island's to set), Q_BLUEPRINT (1 seen, 2 copied onto the slate),
    // Q_SHEET (1 the neighbour has told, 2 brought out with the gallic acid), and the Esmers' landing speech heard (1)
    public const int DreamCaught = 229, QBlueprint = 231, QSheet = 232, EsmersSpoke = 233;
    // DayVar's night: Twinsen asleep in his bed, the morning to come (Tired before it, Rested after)
    public const int Night = 3;
    // the items: the customer's umbrella, the gallic acid, the portable radio, the magic slate; the purse (kashes) and Tim's reward
    private const int Umbrella = 20, GallicAcid = 17, Radio = 26, Slate = 28, Purse = 50, TimReward = 10, GallicPrice = 50;
    // the holomap's places: the harbour's (scene 43's record: the thief, the scrap's wheel)
    private const int HarbourPos = 43;
    // the scenes: Twinsen's house, the Tavern, the ferry office, the neighbour, the museum, the sewers and their far room, the pharmacy, the
    // harbour, the lighthouse, the umbrella thief's (scene 48)
    private const int TavernScene = 3, FerryOffice = 8, Neighbour = 9, Museum = 15, Sewers = 17, FarSewer = 18, Pharmacy = 22, Harbour = 43,
                      UmbrellaScene = 48;
    // the game's people copied: the Tavern's drunk rabbibunny, Luc and Tim (scene 3's actors 4, 5, 6), the umbrella thief (48's 6); the
    // landing crowd's quetch, grobo and Bob (scene 42's by their entities)
    private const int TavernRabbibunny = 4, TavernLuc = 5, TavernTim = 6, UmbrellaThief = 6, CrowdQuetch = 39, CrowdGrobo = 33, CrowdBob = 102;
    // their animations: the thief's run (scene 48's own track)
    private const int ThiefRun = 129;
    // the people of the lighthouse's spell (scene 46): the Weather Wizard and Raph
    private const int LighthouseWizard = 4, LighthouseKeeper = 6;
    // Twinsen's bed in scene 0 (PolarDream's places: on the mattress, out of it, Zoe's step aside) and his animations there (asleep,
    // sitting up, getting out of bed: scene 101's)
    private static readonly (int X, int Y, int Z) BedSpot = (5900, 3072, 2000), OutOfBed = (4352, 2048, 2518), ZoeAside = (2894, 2048, 1902);
    private const int Asleep = 56, SitUp = 57, GetUp = 58;
    // the sewers: where the lift from the storm track's start line (scene 42's door into scene 17) brings Twinsen in -- the lift's top,
    // whose zone the scene's own script lowers him from; the far room's dead end (scene 18), the thief there; a scene's change-cube zones
    // switched on and off by number (their Info4: set_change_cube)
    private static readonly (int X, int Y, int Z) SewerLiftTop = (24576, 3072, 8192), DeadEnd = (1350, 512, 2300);
    // (the 1996 demo's sewers, Demo96Rooms.Sewers: the painter's place and actor, where the demo starts Twinsen in them (the hatch lands
    // there: on the cellar lift's platform the demo's lift ride started, and he fell and drowned), the
    // corner by the barrels beside the far halls' lift)
    // (the hatch a cell in front of the quay's railing: behind it, by the water, Twinsen couldn't come to it -- 2026-11-04)
    private static readonly (int X, int Z) DemoPainter = (17664, 15664);
    private static readonly (int X, int Y, int Z) DemoSewerArrival = (20357, 1536, 15140), DemoDeadEnd = (12800, 3072, 2300);
    private const int DemoPainterActor = 7;
    private const int HatchSwitch = 213, LadderSwitch = 214;
    // the museum's blueprints: under the glass of a display case of the hall's own (the user, 2026-11-04, "in a display case instead of
    // just on the floor"; 2026-11-06, the standard cases, not the tunic's: Citadel/CitadelMuseum, the case and the blueprints in it are
    // the museum's bricks) -- read with Action by it: an unseen helper on the case's top, as the game's own book's case has one (actor 13)
    // (Y: the case's top, layer 7)
    private static readonly (int X, int Y, int Z) BlueprintFile = (Citadel.CitadelMuseum.BlueprintCase.X * 512, 1792, Citadel.CitadelMuseum.BlueprintCase.Z * 512 + 768);
    // the island's places (cells): the harbour's quay below the warehouses (the thief, the hatch further along it -- a straight run on its
    // paving: from behind the warehouses he ran into their walls), the scrap on the quay, the Tavern's doorstep (outside scene 42's door
    // zone into scene 3; the Tavern's front either side of it is a search zone, see Givers), the yard by the carport (RaceTrackIsland's CarHome)
    private static readonly (double X, double Z) ThiefSpot = (478.0, 605.5), Hatch = (470.5, 604.5), Scrap = (472.0, 604.0), TavernDoor = (546.9, 591.6),
                                                   YardSpot = (547.5, 652.5);
    // a per-visit flag of a scene's own (var_cube): no script of the game's uses these (42 is Once3, the LBA1 rooms')
    private const int Once1 = 40, Once2 = 41, Once4 = 43;
    // the text file the pharmacist's price is the game's own in (the Desert's: file 5, text 996, "A flask of gallic costs 50 kashes.")
    private const int DesertTexts = 5, DesertGallicPrice = 996;

    // The story's new lines (Citadel Island's texts, after Lines), in TEXT.HQR's languages: English, French, German, Spanish, Italian,
    // Portuguese. GallicPrice's words are the game's own (the Desert's pharmacist's line, copied in every language).
    private enum ChapterLine
    {
        ZoeSmiling, ZoeShouting, ZoeGloves, FlickStorm, TimCarPark, TimThief, ThiefRuns, ThiefCaught, TimUmbrella, TimLights,
        BlueprintFile, BlueprintDream, BloopOldThing, WizardSpell, RaphBed, ZoeLights,
        ZoeMorning, TwinsenComing, EsmerCup, Quetch, Grobo, Rabbibunny, RaphParked, RaphPolish, TimTavern, TimHook, TimNoHook, ZoeDoor,
        LucWheel, ZoeFitIt, BobWheel, WheelSeen, ZoeGoodAsOld, LucRound, ZoeRadio, ZoePitBox, LucToast, HeraldWelcome, HeraldSlate,
        HeraldLeads, FlickTown, PaulSix, ZoeSlate, CopyOnto, BloopCopied, GallicPrice, GallicThere, GallicComeBack, ZoeTwoTracks, ZoeCrew,
        Brune, PharmacyFerry, MushroomWon, DinoFlySun,
        // (the Tavern regulars' secret bar of chapter 1, in the Tralu's caves' second hall: Citadel/RaceTrackStory.TraluTavern.cs)
        TimBan, MushroomLuc, LucBar, RabbibunnyBar, PatBar, FabBar, StrangerBar, TraluNoisy, TraluQuiet, LucKey, TimBar,
    }
    private static readonly string[][] ChapterLines =
    {
        new[]
        {
            "You were smiling in your sleep. And steering.",
            "Tu souriais en dormant. Et tu tenais un volant.",
            "Du hast im Schlaf gelächelt. Und gelenkt.",
            "Sonreías mientras dormías. Y conducías.",
            "Sorridevi nel sonno. E sterzavi.",
            "Estavas a sorrir a dormir. E a conduzir.",
        },
        new[]
        {
            "You were shouting in your sleep. Something about a big man in a big car.",
            "Tu criais en dormant. Quelque chose à propos d'un grand homme dans une grosse voiture.",
            "Du hast im Schlaf geschrien. Irgendwas von einem großen Mann in einem großen Auto.",
            "Gritabas mientras dormías. Algo sobre un hombre grande en un coche grande.",
            "Gridavi nel sonno. Qualcosa su un uomo grosso in una macchina grossa.",
            "Estavas a gritar a dormir. Qualquer coisa sobre um homem grande num carro grande.",
        },
        new[]
        {
            "Oh, and you left your driving gloves on the seat of your car, under the carport. This morning they were gone! Somebody's been light-fingered again.",
            "Ah, et tu avais laissé tes gants de pilote sur le siège de ta voiture, sous l'abri. Ce matin, ils avaient disparu ! Quelqu'un a encore eu les doigts crochus.",
            "Ach, und du hattest deine Fahrhandschuhe auf dem Sitz deines Wagens liegen lassen, unterm Carport. Heute Morgen waren sie weg! Da hat wieder jemand lange Finger gemacht.",
            "Ah, y te dejaste los guantes de conducir en el asiento del coche, bajo el cobertizo. ¡Esta mañana habían desaparecido! Alguien ha vuelto a tener la mano larga.",
            "Ah, e avevi lasciato i guanti da guida sul sedile della macchina, sotto la tettoia. Stamattina erano spariti! Qualcuno ha di nuovo le mani lunghe.",
            "Ah, e deixaste as tuas luvas de condução no banco do carro, debaixo do telheiro. Esta manhã tinham desaparecido! Alguém voltou a ter mão leve.",
        },
        new[]
        {
            "I'm the School of Magic's assistant, but on race days I'm the marshal. No gloves, no flag, I'm afraid. I saw a fellow with a pair stuffed in his pocket go past where the Tavern used to be.",
            "Je suis l'assistante de l'École de Magie, mais les jours de course, je suis commissaire. Pas de gants, pas de drapeau, désolée. J'ai vu un type passer avec une paire dans la poche, là où était la Taverne.",
            "Ich bin die Assistentin der Zauberschule, aber an Renntagen bin ich die Streckenposten. Keine Handschuhe, keine Flagge, leider. Ich habe einen Kerl mit einem Paar in der Tasche dort vorbeigehen sehen, wo früher die Taverne stand.",
            "Soy la ayudante de la Escuela de Magia, pero los días de carrera soy la comisaria. Sin guantes, no hay bandera, lo siento. Vi pasar a un tipo con un par metido en el bolsillo por donde antes estaba la Taberna.",
            "Sono l'assistente della Scuola di Magia, ma nei giorni di gara faccio da commissaria. Niente guanti, niente bandiera, mi spiace. Ho visto un tizio con un paio in tasca passare dove c'era la Taverna.",
            "Sou a assistente da Escola de Magia, mas nos dias de corrida sou a comissária. Sem luvas, não há bandeira, lamento. Vi um tipo com um par enfiado no bolso passar onde antes ficava a Taberna.",
        },
        new[]
        {
            "Twenty years I served drinks here, Twinsen, and now look: a car park. They needed the room for the track, they said.",
            "Vingt ans que je sers à boire ici, Twinsen, et regarde maintenant : un parking. Il leur fallait la place pour le circuit, qu'ils ont dit.",
            "Zwanzig Jahre habe ich hier Getränke serviert, Twinsen, und jetzt sieh dir das an: ein Parkplatz. Sie brauchten den Platz für die Strecke, haben sie gesagt.",
            "Veinte años sirviendo copas aquí, Twinsen, y mira ahora: un aparcamiento. Necesitaban el sitio para el circuito, dijeron.",
            "Vent'anni ho servito da bere qui, Twinsen, e ora guarda: un parcheggio. Serviva spazio per la pista, hanno detto.",
            "Vinte anos a servir bebidas aqui, Twinsen, e agora olha: um parque de estacionamento. Precisavam do espaço para a pista, disseram.",
        },
        new[]
        {
            "A fellow ran past with gloves in one hand and an umbrella in the other -- the one he snatched at the pharmacy, from one of my regulars. Towards the harbour! If you catch him, bring me that umbrella: I'll keep it for her.",
            "Un type est passé en courant, des gants dans une main et un parapluie dans l'autre -- celui qu'il a chipé à la pharmacie, à une de mes habituées. Vers le port ! Si tu l'attrapes, rapporte-moi ce parapluie : je le lui garderai.",
            "Ein Kerl ist vorbeigerannt, Handschuhe in der einen Hand und einen Regenschirm in der anderen -- den er in der Apotheke einer meiner Stammgäste weggeschnappt hat. Zum Hafen! Wenn du ihn erwischst, bring mir den Schirm: ich bewahre ihn für sie auf.",
            "Un tipo pasó corriendo con unos guantes en una mano y un paraguas en la otra -- el que le quitó en la farmacia a una de mis clientas de siempre. ¡Hacia el puerto! Si lo pillas, tráeme ese paraguas: se lo guardaré.",
            "Un tizio è passato di corsa con dei guanti in una mano e un ombrello nell'altra -- quello che ha soffiato in farmacia a una mia cliente abituale. Verso il porto! Se lo prendi, portami quell'ombrello: glielo terrò da parte.",
            "Um tipo passou a correr com umas luvas numa mão e um guarda-chuva na outra -- o que ele surripiou na farmácia a uma das minhas clientes. Para o porto! Se o apanhares, traz-me esse guarda-chuva: eu guardo-lho.",
        },
        new[]
        {
            "Can't catch me!",
            "Tu ne m'attraperas pas !",
            "Mich kriegst du nicht!",
            "¡No me pillarás!",
            "Non mi prendi!",
            "Não me apanhas!",
        },
        new[]
        {
            "Fine, fine! Here are your gloves. Nice gloves, too. See you on the town circuit, if they ever build one.",
            "Bon, bon ! Les voilà, tes gants. De beaux gants, d'ailleurs. On se reverra sur le circuit de la ville, si jamais ils en construisent un.",
            "Schon gut, schon gut! Hier sind deine Handschuhe. Schöne Handschuhe übrigens. Wir sehen uns auf dem Stadtkurs, falls sie je einen bauen.",
            "¡Vale, vale! Aquí tienes tus guantes. Bonitos guantes, por cierto. Nos vemos en el circuito de la ciudad, si es que alguna vez construyen uno.",
            "Va bene, va bene! Ecco i tuoi guanti. Bei guanti, tra l'altro. Ci vediamo sul circuito cittadino, se mai ne costruiranno uno.",
            "Pronto, pronto! Aqui tens as tuas luvas. Belas luvas, por sinal. Vemo-nos no circuito da cidade, se algum dia construírem um.",
        },
        new[]
        {
            "My regular's umbrella, from the pharmacy! I'll keep it for her. I'd stand you a drink on the house, if I still had a house. Here.",
            "Le parapluie de mon habituée, celui de la pharmacie ! Je le lui garderai. Je t'offrirais bien un verre aux frais de la maison, si j'avais encore une maison. Tiens.",
            "Der Schirm meines Stammgastes, aus der Apotheke! Ich bewahre ihn für sie auf. Ich würde dir ja einen auf Kosten des Hauses ausgeben, wenn ich noch ein Haus hätte. Hier.",
            "¡El paraguas de mi clienta, el de la farmacia! Se lo guardaré. Te invitaría a una copa a cuenta de la casa, si aún tuviera casa. Toma.",
            "L'ombrello della mia cliente, quello della farmacia! Glielo terrò da parte. Ti offrirei da bere a spese della casa, se avessi ancora una casa. Tieni.",
            "O guarda-chuva da minha cliente, o da farmácia! Eu guardo-lho. Pagava-te uma bebida por conta da casa, se ainda tivesse casa. Toma.",
        },
        new[]
        {
            "Lights over the sea, at this hour? First a race track, now this. I'm going home.",
            "Des lumières au-dessus de la mer, à cette heure-ci ? D'abord un circuit, et maintenant ça. Je rentre chez moi.",
            "Lichter über dem Meer, um diese Zeit? Erst eine Rennstrecke, und jetzt das. Ich gehe nach Hause.",
            "¿Luces sobre el mar, a estas horas? Primero un circuito, y ahora esto. Me voy a casa.",
            "Luci sul mare, a quest'ora? Prima una pista, ora questo. Me ne vado a casa.",
            "Luzes sobre o mar, a esta hora? Primeiro uma pista, agora isto. Vou para casa.",
        },
        new[]
        {
            "A file of blueprints: \"ICE ISLAND CIRCUIT. Construction plans. Not to be built.\" @ A road climbing a snowy island, a loop, jumps, and a ramp leaping onto a rocky peak.",
            "Un dossier de plans : \"CIRCUIT DE L'ÎLE DES GLACES. Plans de construction. À ne pas construire.\" @ Une route qui grimpe sur une île enneigée, une boucle, des sauts, et un tremplin qui s'élance sur un pic rocheux.",
            "Eine Mappe mit Bauplänen: \"EISINSEL-KURS. Baupläne. Nicht bauen.\" @ Eine Straße, die eine verschneite Insel hinaufführt, ein Looping, Sprünge und eine Rampe, die auf einen Felsgipfel springt.",
            "Una carpeta de planos: \"CIRCUITO DE LA ISLA DE HIELO. Planos de construcción. No construir.\" @ Una carretera que sube por una isla nevada, un rizo, saltos y una rampa que salta a un pico rocoso.",
            "Una cartella di progetti: \"CIRCUITO DELL'ISOLA DEI GHIACCI. Progetti di costruzione. Da non costruire.\" @ Una strada che sale su un'isola innevata, un giro della morte, dei salti e una rampa che balza su un picco roccioso.",
            "Uma pasta de plantas: \"CIRCUITO DA ILHA DO GELO. Planos de construção. Não construir.\" @ Uma estrada que sobe uma ilha nevada, um looping, saltos e uma rampa que salta para um pico rochoso.",
        },
        new[]
        {
            "That's the track from my dream! Every bend of it. If only I had something to copy it onto.",
            "C'est le circuit de mon rêve ! Chaque virage. Si seulement j'avais de quoi le recopier.",
            "Das ist die Strecke aus meinem Traum! Jede einzelne Kurve. Wenn ich nur etwas hätte, worauf ich sie abzeichnen könnte.",
            "¡Es el circuito de mi sueño! Cada curva. Si al menos tuviera algo donde copiarlo.",
            "È la pista del mio sogno! Ogni curva. Se solo avessi qualcosa su cui ricopiarla.",
            "É a pista do meu sonho! Cada curva. Se ao menos tivesse onde a copiar.",
        },
        new[]
        {
            "That old thing? A man with glasses and a very big car left it here years ago. He said it was worth a fortune one day. It's never been worth anything to me, but I keep it, because I keep everything.",
            "Ce vieux machin ? Un homme à lunettes avec une très grosse voiture l'a laissé ici il y a des années. Il disait que ça vaudrait une fortune un jour. Ça ne m'a jamais rien valu, mais je le garde, parce que je garde tout.",
            "Das alte Ding? Ein Mann mit Brille und einem sehr großen Auto hat es vor Jahren hier gelassen. Er sagte, es sei eines Tages ein Vermögen wert. Mir war es nie etwas wert, aber ich hebe es auf, weil ich alles aufhebe.",
            "¿Esa cosa vieja? Un hombre con gafas y un coche muy grande la dejó aquí hace años. Dijo que algún día valdría una fortuna. Para mí nunca ha valido nada, pero la guardo, porque lo guardo todo.",
            "Quella vecchia roba? Un uomo con gli occhiali e una macchina enorme l'ha lasciata qui anni fa. Diceva che un giorno sarebbe valsa una fortuna. Per me non è mai valsa niente, ma la tengo, perché tengo tutto.",
            "Essa coisa velha? Um homem de óculos com um carro muito grande deixou-a aqui há anos. Disse que um dia valeria uma fortuna. Para mim nunca valeu nada, mas guardo-a, porque guardo tudo.",
        },
        new[]
        {
            "There. A storm this old doesn't clear in an evening, mind you: give it the night. By morning you'll see the sun.",
            "Voilà. Une tempête aussi vieille ne se dissipe pas en une soirée, remarque : laisse-lui la nuit. Demain matin, tu verras le soleil.",
            "So. Ein so alter Sturm verzieht sich nicht an einem Abend, wohlgemerkt: Gib ihm die Nacht. Morgen früh siehst du die Sonne.",
            "Ya está. Una tormenta tan vieja no se despeja en una tarde, ojo: dale la noche. Por la mañana verás el sol.",
            "Ecco. Una tempesta così vecchia non si dissolve in una sera, bada: dalle la notte. Domattina vedrai il sole.",
            "Pronto. Uma tempestade tão velha não se desfaz numa noite, repara: dá-lhe a noite. De manhã verás o sol.",
        },
        new[]
        {
            "Then I'm off to bed. Best race I've lost in years.",
            "Alors je vais me coucher. La plus belle course que j'aie perdue depuis des années.",
            "Dann gehe ich ins Bett. Das beste Rennen, das ich seit Jahren verloren habe.",
            "Entonces me voy a la cama. La mejor carrera que he perdido en años.",
            "Allora vado a letto. La gara più bella che abbia perso da anni.",
            "Então vou para a cama. A melhor corrida que perdi em anos.",
        },
        new[]
        {
            "Something's up there, Twinsen. Above the clouds. Lights. Did you see them?",
            "Il y a quelque chose là-haut, Twinsen. Au-dessus des nuages. Des lumières. Tu les as vues ?",
            "Da oben ist etwas, Twinsen. Über den Wolken. Lichter. Hast du sie gesehen?",
            "Hay algo ahí arriba, Twinsen. Encima de las nubes. Luces. ¿Las has visto?",
            "C'è qualcosa lassù, Twinsen. Sopra le nuvole. Delle luci. Le hai viste?",
            "Há alguma coisa lá em cima, Twinsen. Por cima das nuvens. Luzes. Viste-as?",
        },
        new[]
        {
            "Twinsen! Wake up! The aliens have landed! A big ship, right by the harbour road, and the whole island's out to look!",
            "Twinsen ! Réveille-toi ! Les extraterrestres ont atterri ! Un grand vaisseau, juste à côté de la route du port, et toute l'île est sortie le voir !",
            "Twinsen! Wach auf! Die Außerirdischen sind gelandet! Ein großes Schiff, direkt an der Hafenstraße, und die ganze Insel ist draußen und schaut!",
            "¡Twinsen! ¡Despierta! ¡Los extraterrestres han aterrizado! ¡Una nave enorme, justo al lado de la carretera del puerto, y toda la isla ha salido a verla!",
            "Twinsen! Svegliati! Gli alieni sono atterrati! Una grande nave, proprio vicino alla strada del porto, e tutta l'isola è uscita a guardare!",
            "Twinsen! Acorda! Os extraterrestres aterraram! Uma nave enorme, mesmo ao pé da estrada do porto, e a ilha inteira saiu para ver!",
        },
        new[]
        {
            "Aliens? I was dreaming about... no. Never mind. I'm coming!",
            "Des extraterrestres ? Je rêvais de... non. Laisse tomber. J'arrive !",
            "Außerirdische? Ich habe geträumt von... nein. Egal. Ich komme!",
            "¿Extraterrestres? Estaba soñando con... no. Da igual. ¡Ya voy!",
            "Alieni? Stavo sognando... no. Lascia stare. Arrivo!",
            "Extraterrestres? Estava a sonhar com... não. Esquece. Já vou!",
        },
        new[]
        {
            "And today we open a Cup for both your worlds, Twinsun's and Zeelich's. The first race is here, on your new circuit.",
            "Et aujourd'hui, nous ouvrons une Coupe pour vos deux mondes, celui de Twinsun et celui de Zeelich. La première course a lieu ici, sur votre nouveau circuit.",
            "Und heute eröffnen wir einen Pokal für Eure beiden Welten, Twinsun und Zeelich. Das erste Rennen findet hier statt, auf Eurem neuen Kurs.",
            "Y hoy inauguramos una Copa para vuestros dos mundos, Twinsun y Zeelich. La primera carrera es aquí, en vuestro nuevo circuito.",
            "E oggi apriamo una Coppa per entrambi i vostri mondi, Twinsun e Zeelich. La prima gara è qui, sul vostro nuovo circuito.",
            "E hoje abrimos uma Taça para os vossos dois mundos, Twinsun e Zeelich. A primeira corrida é aqui, no vosso novo circuito.",
        },
        new[]
        {
            "A whole circuit in one night! I went to bed with a puddle outside my door and woke up with a grandstand.",
            "Un circuit entier en une nuit ! Je me suis couché avec une flaque devant ma porte et je me suis réveillé avec une tribune.",
            "Eine ganze Rennstrecke in einer Nacht! Ich bin mit einer Pfütze vor der Tür ins Bett gegangen und mit einer Tribüne aufgewacht.",
            "¡Un circuito entero en una noche! Me acosté con un charco delante de la puerta y me desperté con una tribuna.",
            "Un circuito intero in una notte! Sono andato a letto con una pozzanghera davanti alla porta e mi sono svegliato con una tribuna.",
            "Um circuito inteiro numa noite! Fui deitar-me com uma poça à porta e acordei com uma bancada.",
        },
        new[]
        {
            "Did you see the Tavern? Back like it never left. Even the sign's been repainted.",
            "Tu as vu la Taverne ? Revenue comme si elle n'était jamais partie. Même l'enseigne a été repeinte.",
            "Hast du die Taverne gesehen? Wieder da, als wäre sie nie weg gewesen. Sogar das Schild ist frisch gestrichen.",
            "¿Has visto la Taberna? Ha vuelto como si nunca se hubiera ido. Hasta han repintado el letrero.",
            "Hai visto la Taverna? Tornata come se non se ne fosse mai andata. Hanno perfino ridipinto l'insegna.",
            "Viste a Taberna? Voltou como se nunca tivesse saído. Até a tabuleta foi pintada de novo.",
        },
        new[]
        {
            "They asked me where the bins went. Polite, these aliens.",
            "Ils m'ont demandé où allaient les poubelles. Polis, ces extraterrestres.",
            "Sie haben mich gefragt, wo die Mülltonnen hinkommen. Höflich, diese Außerirdischen.",
            "Me preguntaron dónde iban los cubos de basura. Educados, estos extraterrestres.",
            "Mi hanno chiesto dove andavano i bidoni. Educati, questi alieni.",
            "Perguntaram-me onde ficavam os caixotes do lixo. Bem-educados, estes extraterrestres.",
        },
        new[]
        {
            "I parked on this spot last night. Now there's a Tavern on it. Where's my car?",
            "Je me suis garé ici hier soir. Maintenant il y a une Taverne dessus. Où est ma voiture ?",
            "Ich habe gestern Abend genau hier geparkt. Jetzt steht eine Taverne drauf. Wo ist mein Auto?",
            "Anoche aparqué justo aquí. Ahora hay una Taberna encima. ¿Dónde está mi coche?",
            "Ieri sera ho parcheggiato proprio qui. Adesso c'è una Taverna sopra. Dov'è la mia macchina?",
            "Ontem à noite estacionei aqui mesmo. Agora está aqui uma Taberna. Onde está o meu carro?",
        },
        new[]
        {
            "...in the Esmers' car park, they say. With a wash and a polish. I could get used to aliens.",
            "...dans le parking des Esmers, paraît-il. Lavée et lustrée. Je pourrais m'habituer aux extraterrestres.",
            "...auf dem Parkplatz der Esmer, sagt man. Gewaschen und poliert. An Außerirdische könnte ich mich gewöhnen.",
            "...en el aparcamiento de los Esmers, dicen. Lavado y abrillantado. Podría acostumbrarme a los extraterrestres.",
            "...nel parcheggio degli Esmer, dicono. Lavata e lucidata. Agli alieni potrei abituarmi.",
            "...no parque dos Esmers, dizem. Lavado e encerado. Podia habituar-me aos extraterrestres.",
        },
        new[]
        {
            "Twinsen! Look at it! The same bar, the same stools, the same stain on the ceiling... I asked them to leave that out, but they said 'exactly as it was'. Come in, come in: the first drink in the new old Tavern is on the house.",
            "Twinsen ! Regarde-moi ça ! Le même comptoir, les mêmes tabourets, la même tache au plafond... Je leur ai demandé de l'oublier, mais ils ont dit 'exactement comme avant'. Entre, entre : le premier verre de la nouvelle vieille Taverne est offert par la maison.",
            "Twinsen! Sieh dir das an! Dieselbe Theke, dieselben Hocker, derselbe Fleck an der Decke... Ich habe sie gebeten, den wegzulassen, aber sie sagten 'genau wie vorher'. Komm rein, komm rein: Der erste Drink in der neuen alten Taverne geht aufs Haus.",
            "¡Twinsen! ¡Mira esto! La misma barra, los mismos taburetes, la misma mancha en el techo... Les pedí que la quitaran, pero dijeron 'exactamente como estaba'. Pasa, pasa: la primera copa en la nueva vieja Taberna invita la casa.",
            "Twinsen! Guarda qui! Lo stesso bancone, gli stessi sgabelli, la stessa macchia sul soffitto... Ho chiesto di toglierla, ma hanno detto 'esattamente com'era'. Entra, entra: il primo bicchiere nella nuova vecchia Taverna lo offre la casa.",
            "Twinsen! Olha para isto! O mesmo balcão, os mesmos bancos, a mesma mancha no teto... Pedi-lhes que a deixassem de fora, mas disseram 'exatamente como estava'. Entra, entra: a primeira bebida na nova velha Taberna é por conta da casa.",
        },
        new[]
        {
            "And that umbrella you brought back is on its hook by the door. She never came for it. Exactly as it was.",
            "Et ce parapluie que tu as rapporté est à son crochet près de la porte. Elle n'est jamais venue le chercher. Exactement comme avant.",
            "Und der Schirm, den du zurückgebracht hast, hängt an seinem Haken neben der Tür. Sie hat ihn nie abgeholt. Genau wie vorher.",
            "Y ese paraguas que trajiste está en su gancho junto a la puerta. Ella nunca vino a buscarlo. Exactamente como estaba.",
            "E quell'ombrello che hai riportato è al suo gancio vicino alla porta. Lei non è mai venuta a prenderlo. Esattamente com'era.",
            "E aquele guarda-chuva que trouxeste está no gancho ao pé da porta. Ela nunca o veio buscar. Exatamente como estava.",
        },
        new[]
        {
            "And if you ever find my regular's umbrella, there's a hook by the door waiting for it.",
            "Et si jamais tu retrouves le parapluie de mon habituée, il y a un crochet près de la porte qui l'attend.",
            "Und falls du je den Schirm meines Stammgastes findest: Neben der Tür wartet ein Haken auf ihn.",
            "Y si alguna vez encuentras el paraguas de mi clienta, hay un gancho junto a la puerta esperándolo.",
            "E se mai trovi l'ombrello della mia cliente, c'è un gancio vicino alla porta che lo aspetta.",
            "E se alguma vez encontrares o guarda-chuva da minha cliente, há um gancho ao pé da porta à espera dele.",
        },
        new[]
        {
            "There's a crowd at the new track already! Come on, I want a look... and bring your toolbox. Somebody's going to need a mechanic today.",
            "Il y a déjà foule au nouveau circuit ! Viens, je veux voir ça... et apporte ta boîte à outils. Quelqu'un va avoir besoin d'un mécanicien aujourd'hui.",
            "An der neuen Strecke ist schon eine Menge los! Komm, ich will mir das ansehen... und bring deinen Werkzeugkasten mit. Heute wird jemand einen Mechaniker brauchen.",
            "¡Ya hay una multitud en el nuevo circuito! Vamos, quiero verlo... y trae tu caja de herramientas. Hoy alguien va a necesitar un mecánico.",
            "C'è già una folla alla nuova pista! Dai, voglio dare un'occhiata... e porta la cassetta degli attrezzi. Oggi qualcuno avrà bisogno di un meccanico.",
            "Já há uma multidão na pista nova! Anda, quero ver... e traz a caixa de ferramentas. Hoje alguém vai precisar de um mecânico.",
        },
        new[]
        {
            "They gave me my Tavern back, Twinsen, so I'm racing for the Tavern! Only... my back wheel came off on the way here.",
            "Ils m'ont rendu ma Taverne, Twinsen, alors je cours pour la Taverne ! Seulement... ma roue arrière s'est détachée en venant.",
            "Sie haben mir meine Taverne zurückgegeben, Twinsen, also fahre ich für die Taverne! Nur... mein Hinterrad ist auf dem Weg hierher abgefallen.",
            "Me han devuelto mi Taberna, Twinsen, ¡así que corro por la Taberna! Solo que... se me ha soltado la rueda trasera viniendo hacia aquí.",
            "Mi hanno ridato la mia Taverna, Twinsen, quindi corro per la Taverna! Solo che... la ruota posteriore mi si è staccata venendo qui.",
            "Devolveram-me a minha Taberna, Twinsen, por isso corro pela Taberna! Só que... a roda de trás soltou-se a caminho daqui.",
        },
        new[]
        {
            "I can fit it, if someone finds it. Bob says there's a spare in the scrap at the harbour.",
            "Je peux la remonter, si quelqu'un la trouve. Bob dit qu'il y en a une de rechange dans la ferraille du port.",
            "Ich kann es anbauen, wenn jemand es findet. Bob sagt, im Schrott am Hafen liegt ein Ersatzrad.",
            "Yo puedo ponerla, si alguien la encuentra. Bob dice que hay una de repuesto en la chatarra del puerto.",
            "Posso montarla io, se qualcuno la trova. Bob dice che ce n'è una di scorta tra i rottami del porto.",
            "Eu consigo montá-la, se alguém a encontrar. O Bob diz que há uma sobressalente na sucata do porto.",
        },
        new[]
        {
            "A wheel? Somewhere under that lot. Mind the fish.",
            "Une roue ? Quelque part là-dessous. Attention aux poissons.",
            "Ein Rad? Irgendwo unter dem ganzen Kram. Pass auf die Fische auf.",
            "¿Una rueda? Por ahí debajo de todo eso. Cuidado con el pescado.",
            "Una ruota? Da qualche parte lì sotto. Attento al pesce.",
            "Uma roda? Algures debaixo daquilo tudo. Cuidado com o peixe.",
        },
        new[]
        {
            "A wheel. A bit rusty, but round.",
            "Une roue. Un peu rouillée, mais ronde.",
            "Ein Rad. Etwas rostig, aber rund.",
            "Una rueda. Un poco oxidada, pero redonda.",
            "Una ruota. Un po' arrugginita, ma rotonda.",
            "Uma roda. Um pouco ferrugenta, mas redonda.",
        },
        new[]
        {
            "There. Good as new. Well, good as old.",
            "Voilà. Comme neuve. Enfin, comme vieille.",
            "So. Wie neu. Na ja, wie alt.",
            "Ya está. Como nueva. Bueno, como vieja.",
            "Ecco. Come nuova. Be', come vecchia.",
            "Pronto. Como nova. Bem, como velha.",
        },
        new[]
        {
            "Ha! First round's on me, both of you. After the race.",
            "Ha ! La première tournée est pour moi, tous les deux. Après la course.",
            "Ha! Die erste Runde geht auf mich, für euch beide. Nach dem Rennen.",
            "¡Ja! La primera ronda la pago yo, para los dos. Después de la carrera.",
            "Ah! Il primo giro lo offro io, a tutti e due. Dopo la gara.",
            "Ah! A primeira rodada é por minha conta, para os dois. Depois da corrida.",
        },
        new[]
        {
            "And Twinsen, a parcel came for you on the morning ferry, from Baldino on the Desert. A portable radio! He says he wants to hear how the Cup goes.",
            "Et Twinsen, un colis est arrivé pour toi par le ferry de ce matin, de la part de Baldino, sur le Désert. Une radio portable ! Il dit qu'il veut savoir comment se passe la Coupe.",
            "Und Twinsen, mit der Morgenfähre ist ein Paket für dich gekommen, von Baldino aus der Wüste. Ein tragbares Funkgerät! Er sagt, er will hören, wie der Pokal läuft.",
            "Y Twinsen, ha llegado un paquete para ti en el ferry de la mañana, de Baldino, desde el Desierto. ¡Una radio portátil! Dice que quiere enterarse de cómo va la Copa.",
            "E Twinsen, col traghetto di stamattina è arrivato un pacco per te, da Baldino, dal Deserto. Una radio portatile! Dice che vuole sapere come va la Coppa.",
            "E Twinsen, chegou uma encomenda para ti no ferry da manhã, do Baldino, no Deserto. Um rádio portátil! Diz que quer saber como corre a Taça.",
        },
        new[]
        {
            "You drive, I'll keep the car in one piece. I'll be in the pit box by the start line.",
            "Toi, tu conduis, moi je garde la voiture en un seul morceau. Je serai dans le stand, près de la ligne de départ.",
            "Du fährst, ich halte das Auto in einem Stück. Ich bin in der Box an der Startlinie.",
            "Tú conduces, yo mantengo el coche de una pieza. Estaré en el box, junto a la línea de salida.",
            "Tu guidi, io tengo la macchina tutta intera. Sarò ai box, vicino alla linea di partenza.",
            "Tu conduzes, eu mantenho o carro inteiro. Estarei na boxe, ao pé da linha de partida.",
        },
        new[]
        {
            "First round's on me, as promised! To the Cup!",
            "La première tournée est pour moi, comme promis ! À la Coupe !",
            "Die erste Runde geht auf mich, wie versprochen! Auf den Pokal!",
            "¡La primera ronda la pago yo, como prometí! ¡Por la Copa!",
            "Il primo giro lo offro io, come promesso! Alla Coppa!",
            "A primeira rodada é por minha conta, como prometido! À Taça!",
        },
        new[]
        {
            "Welcome to the Two Worlds Cup! One race on every island of Twinsun and Zeelich, and to whoever wins them all, a wish. Any wish. The first race is here, on Citadel.",
            "Bienvenue à la Coupe des Deux Mondes ! Une course sur chaque île de Twinsun et de Zeelich, et pour qui les gagnera toutes, un voeu. N'importe quel voeu. La première course a lieu ici, sur la Citadelle.",
            "Willkommen zum Pokal der Zwei Welten! Ein Rennen auf jeder Insel von Twinsun und Zeelich, und wer sie alle gewinnt, hat einen Wunsch frei. Jeden Wunsch. Das erste Rennen findet hier statt, auf der Zitadelle.",
            "¡Bienvenido a la Copa de los Dos Mundos! Una carrera en cada isla de Twinsun y de Zeelich, y para quien las gane todas, un deseo. Cualquier deseo. La primera carrera es aquí, en la Ciudadela.",
            "Benvenuto alla Coppa dei Due Mondi! Una gara su ogni isola di Twinsun e di Zeelich, e a chi le vince tutte, un desiderio. Qualunque desiderio. La prima gara è qui, sulla Cittadella.",
            "Bem-vindo à Taça dos Dois Mundos! Uma corrida em cada ilha de Twinsun e de Zeelich, e a quem as ganhar todas, um desejo. Qualquer desejo. A primeira corrida é aqui, na Cidadela.",
        },
        new[]
        {
            "Every racer gets one of these. It shows the circuits of the Cup: a new one appears each time you reach its island.",
            "Chaque pilote en reçoit une. Elle montre les circuits de la Coupe : un nouveau apparaît chaque fois que tu arrives sur son île.",
            "Jeder Fahrer bekommt eine davon. Sie zeigt die Strecken des Pokals: Jedes Mal, wenn du ihre Insel erreichst, erscheint eine neue.",
            "Cada piloto recibe una. Muestra los circuitos de la Copa: aparece uno nuevo cada vez que llegas a su isla.",
            "Ogni pilota ne riceve una. Mostra i circuiti della Coppa: ne compare uno nuovo ogni volta che arrivi sulla sua isola.",
            "Cada piloto recebe uma. Mostra os circuitos da Taça: aparece um novo de cada vez que chegas à sua ilha.",
        },
        new[]
        {
            "Twinsen of Citadel Island leads the Cup! Next: the Desert, the White Leaf Desert.",
            "Twinsen de l'île de la Citadelle mène la Coupe ! Prochaine étape : le Désert, le Désert de la Feuille Blanche.",
            "Twinsen von der Zitadelleninsel führt im Pokal! Als Nächstes: die Wüste, die Weißblattwüste.",
            "¡Twinsen, de la isla de la Ciudadela, lidera la Copa! Próxima parada: el Desierto, el Desierto de la Hoja Blanca.",
            "Twinsen dell'isola della Cittadella guida la Coppa! Prossima tappa: il Deserto, il Deserto della Foglia Bianca.",
            "Twinsen, da ilha da Cidadela, lidera a Taça! A seguir: o Deserto, o Deserto da Folha Branca.",
        },
        new[]
        {
            "Six cars on the grid or Mr. Paul won't start it. That's you, Raph, Mr. Paul, the Tralu, that thief again... and Luc, if he ever gets his barrel back on four wheels.",
            "Six voitures sur la grille, ou M. Paul ne donnera pas le départ. Ça fait toi, Raph, M. Paul, le Tralu, encore ce voleur... et Luc, s'il remet un jour son tonneau sur quatre roues.",
            "Sechs Autos in der Startaufstellung, sonst startet Herr Paul das Rennen nicht. Das sind du, Raph, Herr Paul, der Tral, schon wieder dieser Dieb... und Luc, falls er sein Fass je wieder auf vier Räder bekommt.",
            "Seis coches en la parrilla o el señor Paul no da la salida. Eso sois tú, Raph, el señor Paul, el Tralú, ese ladrón otra vez... y Luc, si alguna vez vuelve a poner su barril sobre cuatro ruedas.",
            "Sei macchine sulla griglia o il signor Paul non dà il via. Cioè tu, Raph, il signor Paul, il Tralù, di nuovo quel ladro... e Luc, se mai rimette la sua botte su quattro ruote.",
            "Seis carros na grelha ou o senhor Paul não dá a partida. Isso és tu, o Raph, o senhor Paul, o Tralu, outra vez aquele ladrão... e o Luc, se algum dia voltar a pôr o barril em quatro rodas.",
        },
        new[]
        {
            "Rules are rules, Twinsen. Six cars.",
            "Le règlement, c'est le règlement, Twinsen. Six voitures.",
            "Regeln sind Regeln, Twinsen. Sechs Autos.",
            "Las normas son las normas, Twinsen. Seis coches.",
            "Le regole sono regole, Twinsen. Sei macchine.",
            "Regras são regras, Twinsen. Seis carros.",
        },
        new[]
        {
            "A book of race tracks... Twinsen, didn't you see a track in the museum? And our neighbour's been squinting at some drawings all week.",
            "Un livre de circuits... Twinsen, tu n'as pas vu un circuit au musée ? Et notre voisin plisse les yeux sur des dessins depuis une semaine.",
            "Ein Buch voller Rennstrecken... Twinsen, hast du nicht im Museum eine Strecke gesehen? Und unser Nachbar starrt seit einer Woche auf irgendwelche Zeichnungen.",
            "Un libro de circuitos... Twinsen, ¿no viste un circuito en el museo? Y nuestro vecino lleva toda la semana forzando la vista con unos dibujos.",
            "Un libro di piste... Twinsen, non avevi visto una pista al museo? E il nostro vicino è tutta la settimana che strizza gli occhi su certi disegni.",
            "Um livro de pistas... Twinsen, não viste uma pista no museu? E o nosso vizinho anda a semana toda a franzir os olhos para uns desenhos.",
        },
        new[]
        {
            "Now I've got something to copy it onto.",
            "Maintenant, j'ai de quoi le recopier.",
            "Jetzt habe ich etwas, worauf ich sie abzeichnen kann.",
            "Ahora tengo algo donde copiarlo.",
            "Adesso ho qualcosa su cui ricopiarla.",
            "Agora tenho onde a copiar.",
        },
        new[]
        {
            "Did that slate just copy my file? The Esmers have very clever toys.",
            "Cette ardoise vient de copier mon dossier ? Les Esmers ont des jouets très malins.",
            "Hat diese Tafel gerade meine Mappe abgezeichnet? Die Esmer haben sehr schlaues Spielzeug.",
            "¿Esa pizarra acaba de copiar mi carpeta? Los Esmers tienen juguetes muy listos.",
            "Quella lavagnetta ha appena copiato la mia cartella? Gli Esmer hanno giocattoli molto furbi.",
            "Aquela ardósia acabou de copiar a minha pasta? Os Esmers têm brinquedos muito espertos.",
        },
        // (the game's own words: the Desert's text 996, put in every language from that file -- AddChapterTexts)
        new[]
        {
            "A flask of gallic costs 50 kashes.",
            "Un flacon d'acide gallique coûte 50 kashes.",
            "Eine Flasche Gallussäure kostet 50 Kashes.",
            "Un frasco de ácido gálico cuesta 50 kashes.",
            "Una boccetta di acido gallico costa 50 kashes.",
            "Um frasco de ácido gálico custa 50 kashes.",
        },
        new[]
        {
            "There. Mind, it's for old documents, not for engines, whatever the Desert racers tell you.",
            "Voilà. Attention, c'est pour les vieux documents, pas pour les moteurs, quoi qu'en disent les pilotes du Désert.",
            "Bitte. Aber Achtung, das ist für alte Dokumente, nicht für Motoren, egal was die Rennfahrer aus der Wüste erzählen.",
            "Aquí tienes. Ojo, es para documentos antiguos, no para motores, digan lo que digan los pilotos del Desierto.",
            "Ecco. Bada, è per i documenti antichi, non per i motori, qualunque cosa dicano i piloti del Deserto.",
            "Aqui tens. Atenção, é para documentos antigos, não para motores, digam o que disserem os pilotos do Deserto.",
        },
        new[]
        {
            "Come back when you've won something.",
            "Reviens quand tu auras gagné quelque chose.",
            "Komm wieder, wenn du etwas gewonnen hast.",
            "Vuelve cuando hayas ganado algo.",
            "Torna quando avrai vinto qualcosa.",
            "Volta quando tiveres ganho alguma coisa.",
        },
        new[]
        {
            "Two tracks we've already raced, and the whole Cup still to come. Your slate's filling up.",
            "Deux circuits qu'on a déjà courus, et toute la Coupe encore à venir. Ton ardoise se remplit.",
            "Zwei Strecken, die wir schon gefahren sind, und der ganze Pokal kommt erst noch. Deine Tafel füllt sich.",
            "Dos circuitos que ya hemos corrido, y toda la Copa por delante. Tu pizarra se va llenando.",
            "Due piste che abbiamo già corso, e tutta la Coppa ancora da correre. La tua lavagnetta si riempie.",
            "Duas pistas que já corremos, e a Taça inteira ainda por vir. A tua ardósia está a encher-se.",
        },
        new[]
        {
            "Every race needs a pit crew. That's me. You drive, I'll fix.",
            "Chaque course a besoin d'une équipe au stand. C'est moi. Toi, tu conduis, moi, je répare.",
            "Jedes Rennen braucht eine Boxencrew. Das bin ich. Du fährst, ich schraube.",
            "Toda carrera necesita un equipo en boxes. Esa soy yo. Tú conduces, yo arreglo.",
            "Ogni gara ha bisogno di una squadra ai box. Sono io. Tu guidi, io aggiusto.",
            "Todas as corridas precisam de uma equipa na boxe. Sou eu. Tu conduzes, eu arranjo.",
        },
        new[]
        {
            "One ticket for the Desert... and the young lady goes free, she says she's crew.",
            "Un billet pour le Désert... et la demoiselle voyage gratuitement, elle dit qu'elle fait partie de l'équipe.",
            "Eine Fahrkarte in die Wüste... und die junge Dame fährt umsonst, sie sagt, sie gehört zur Crew.",
            "Un billete para el Desierto... y la señorita viaja gratis, dice que es del equipo.",
            "Un biglietto per il Deserto... e la signorina viaggia gratis, dice che fa parte della squadra.",
            "Um bilhete para o Deserto... e a menina viaja de graça, diz que é da equipa.",
        },
        // (the game's people's lines put right for the story's days -- RetoldGates: the pharmacist in chapter 2, the mushroom man the
        // evening of the race won, the Dino-Fly in the fine weather)
        new[]
        {
            "The morning ferry brought my deliveries at last. Half the drivers on the island have been in for hand cream.",
            "Le ferry du matin m'a enfin apporté mes livraisons. La moitié des pilotes de l'île sont passés acheter de la crème pour les mains.",
            "Die Morgenfähre hat endlich meine Lieferung gebracht. Die Hälfte der Fahrer auf der Insel war schon hier, wegen Handcreme.",
            "El ferry de la mañana me ha traído por fin los pedidos. La mitad de los pilotos de la isla ha venido a por crema de manos.",
            "Il traghetto del mattino mi ha finalmente portato le consegne. Metà dei piloti dell'isola è passata a prendere crema per le mani.",
            "O ferry da manhã trouxe-me finalmente as encomendas. Metade dos pilotos da ilha já cá veio buscar creme para as mãos.",
        },
        new[]
        {
            "You beat Raph's time? In this rain? The mushrooms and I watched the whole thing from here.",
            "Tu as battu le temps de Raph ? Sous cette pluie ? Mes champignons et moi, on a tout vu d'ici.",
            "Du hast Raphs Zeit geschlagen? Bei diesem Regen? Meine Pilze und ich haben alles von hier aus gesehen.",
            "¿Has batido el tiempo de Raph? ¿Con esta lluvia? Mis setas y yo lo hemos visto todo desde aquí.",
            "Hai battuto il tempo di Raph? Con questa pioggia? Io e i miei funghi abbiamo visto tutto da qui.",
            "Bateste o tempo do Raph? Com esta chuva? Eu e os meus cogumelos vimos tudo daqui.",
        },
        new[]
        {
            "Sunshine at last! But I'm saving my wings for the Desert race.",
            "Enfin du soleil ! Mais je garde mes ailes pour la course du Désert.",
            "Endlich Sonne! Aber meine Flügel spare ich mir für das Rennen in der Wüste auf.",
            "¡Por fin sol! Pero me guardo las alas para la carrera del Desierto.",
            "Finalmente il sole! Ma tengo le ali per la gara del Deserto.",
            "Finalmente sol! Mas guardo as asas para a corrida do Deserto.",
        },
        new[]
        {
            "And don't ask me for a drink, Twinsen: the mayor's banned the stuff while there's a race track on the island. No drinking and driving, he says. Funny, though... nobody's seen Luc all day. Nor the Tavern's bottles. Not my business. I'm only the waiter.",
            "Et ne me demande pas à boire, Twinsen : le maire a interdit l'alcool tant qu'il y a un circuit sur l'île. Pas d'alcool au volant, qu'il dit. C'est drôle, quand même... personne n'a vu Luc de la journée. Ni les bouteilles de la Taverne. Ça ne me regarde pas. Je ne suis que le serveur.",
            "Und frag mich nicht nach einem Drink, Twinsen: Der Bürgermeister hat den Alkohol verboten, solange es auf der Insel eine Rennstrecke gibt. Kein Alkohol am Steuer, sagt er. Komisch ist nur... Luc hat heute den ganzen Tag niemand gesehen. Die Flaschen der Taverne auch nicht. Geht mich nichts an. Ich bin nur der Kellner.",
            "Y no me pidas una copa, Twinsen: el alcalde ha prohibido el alcohol mientras haya un circuito en la isla. Si bebes, no conduzcas, dice. Aunque es curioso... nadie ha visto a Luc en todo el día. Ni las botellas de la Taberna. No es asunto mío. Yo solo soy el camarero.",
            "E non chiedermi da bere, Twinsen: il sindaco ha vietato l'alcol finché c'è una pista sull'isola. Niente alcol al volante, dice. Strano, però... nessuno ha visto Luc per tutto il giorno. Né le bottiglie della Taverna. Non sono affari miei. Io sono solo il cameriere.",
            "E não me peças uma bebida, Twinsen: o presidente da câmara proibiu o álcool enquanto houver uma pista na ilha. Se beber, não conduza, diz ele. Mas é curioso... ninguém viu o Luc o dia todo. Nem as garrafas da Taberna. Não é da minha conta. Sou só o empregado de mesa.",
        },
        new[]
        {
            "Have you seen Luc, from the Tavern? Nor has anyone, since the mayor banned drink. And half his regulars go missing of an evening, and come home singing. Funny, that. The mushrooms hear everything.",
            "Tu as vu Luc, de la Taverne ? Personne ne l'a vu depuis que le maire a interdit l'alcool. Et la moitié de ses habitués disparaissent le soir, et rentrent en chantant. Curieux, non ? Les champignons entendent tout.",
            "Hast du Luc von der Taverne gesehen? Keiner hat ihn gesehen, seit der Bürgermeister den Alkohol verboten hat. Und die Hälfte seiner Stammgäste verschwindet abends und kommt singend nach Hause. Komisch, was? Die Pilze hören alles.",
            "¿Has visto a Luc, el de la Taberna? Nadie lo ha visto desde que el alcalde prohibió el alcohol. Y la mitad de sus clientes desaparecen por las noches y vuelven a casa cantando. Curioso, ¿eh? Las setas lo oyen todo.",
            "Hai visto Luc, quello della Taverna? Nessuno l'ha visto da quando il sindaco ha vietato l'alcol. E metà dei suoi clienti la sera sparisce, e torna a casa cantando. Strano, no? I funghi sentono tutto.",
            "Viste o Luc, da Taberna? Ninguém o viu desde que o presidente da câmara proibiu o álcool. E metade dos clientes dele desaparece à noite e volta para casa a cantar. Curioso, não? Os cogumelos ouvem tudo.",
        },
        new[]
        {
            "Shh! Come in, Twinsen, and keep it down. Welcome to the Back Room: the only bar on Citadel the mayor doesn't know about. A proper table, and the Tavern's last bottles... And if you're racing today, it's lemonade for you.",
            "Chut ! Entre, Twinsen, et pas trop fort. Bienvenue à l'Arrière-Salle : le seul bar de la Citadelle dont le maire n'a jamais entendu parler. Une vraie table, et les dernières bouteilles de la Taverne... Et si tu cours aujourd'hui, ce sera de la limonade pour toi.",
            "Pst! Komm rein, Twinsen, und sei leise. Willkommen im Hinterzimmer: die einzige Bar auf der Zitadelle, von der der Bürgermeister nichts weiß. Ein ordentlicher Tisch und die letzten Flaschen der Taverne... Und wenn du heute fährst, gibt's für dich Limonade.",
            "¡Chis! Pasa, Twinsen, y no hagas ruido. Bienvenido a la Trastienda: el único bar de la Ciudadela del que el alcalde no sabe nada. Una mesa como Dios manda y las últimas botellas de la Taberna... Y si hoy corres, para ti, limonada.",
            "Sst! Entra, Twinsen, e fai piano. Benvenuto nel Retrobottega: l'unico bar della Cittadella di cui il sindaco non sa niente. Un tavolo come si deve e le ultime bottiglie della Taverna... E se oggi corri, per te c'è la limonata.",
            "Chiu! Entra, Twinsen, e fala baixo. Bem-vindo à Sala das Traseiras: o único bar da Cidadela de que o presidente da câmara não sabe nada. Uma mesa a sério e as últimas garrafas da Taberna... E se hoje vais correr, para ti é limonada.",
        },
        new[]
        {
            "The mayor shays no drinking while there'sh a race track. [hic!] Sho I'm not drinking. I'm... sitting. At a table. With a mug. [hic!] Don't tell the mayor.",
            "Le maire dit pas d'alcool tant qu'y a un shircuit. [hips !] Alors je bois pas. Je suis... assis. À une table. Avec une chope. [hips !] Le dites pas au maire.",
            "Der Bürgermeishter shagt, kein Alkohol, sholange esh eine Rennshtrecke gibt. [hick!] Alsho trink ich nich'. Ich... shitze. An 'nem Tish. Mit 'nem Krug. [hick!] Nich' dem Bürgermeishter shagen.",
            "El alcalde dishe que nada de alcohol mientrash haya un circuito. [¡hip!] Ashí que no bebo. Eshtoy... shentado. En una mesa. Con una jarra. [¡hip!] No se lo digash al alcalde.",
            "Il shindaco dice niente alcol finché c'è una pishta. [hic!] Quindi non bevo. Shono... sheduto. A un tavolo. Con un boccale. [hic!] Non ditelo al shindaco.",
            "O presidente da câmara diz que nada de álcool enquanto houver uma pishta. [hic!] Por isho não bebo. Eshtou... shentado. A uma mesa. Com uma caneca. [hic!] Não digam ao presidente.",
        },
        new[]
        {
            "No Tavern, no stage... so we brought the band down here. Listen to that echo! Every beat comes back three times. Raph's meant to be playing with us, but he won't come off that track.",
            "Plus de Taverne, plus de scène... alors on a emmené le groupe ici. Écoute-moi cet écho ! Chaque coup revient trois fois. Raph devait jouer avec nous, mais il ne veut pas lâcher son circuit.",
            "Keine Taverne, keine Bühne... also haben wir die Band hierher verlegt. Hör dir dieses Echo an! Jeder Schlag kommt dreimal zurück. Raph sollte eigentlich mit uns spielen, aber er kommt nicht von seiner Strecke runter.",
            "Sin Taberna no hay escenario... así que nos hemos traído la banda aquí abajo. ¡Escucha qué eco! Cada golpe vuelve tres veces. Raph tenía que tocar con nosotros, pero no hay quien lo saque de ese circuito.",
            "Niente Taverna, niente palco... così abbiamo portato la band quaggiù. Senti che eco! Ogni colpo torna indietro tre volte. Raph doveva suonare con noi, ma non si stacca da quella pista.",
            "Sem Taberna não há palco... por isso trouxemos a banda cá para baixo. Ouve só este eco! Cada batida volta três vezes. O Raph devia estar a tocar connosco, mas não sai daquela pista.",
        },
        new[]
        {
            "And the big fellow next door hasn't come round to complain. Yet.",
            "Et le gros, à côté, n'est pas encore venu se plaindre. Pas encore.",
            "Und der große Kerl nebenan hat sich noch nicht beschwert. Noch nicht.",
            "Y el grandullón de al lado todavía no ha venido a quejarse. Todavía.",
            "E il bestione qui accanto non è ancora venuto a lamentarsi. Non ancora.",
            "E o grandalhão aqui ao lado ainda não veio queixar-se. Ainda.",
        },
        new[]
        {
            "Sorry, I'm not from here. Back home we drink in bars. Here you drink in a cave, in secret, in a storm, at a table fit for a giant. I love this island.",
            "Désolé, je ne suis pas d'ici. Chez moi, on boit dans des bars. Ici, on boit dans une grotte, en cachette, pendant une tempête, à une table de géant. J'adore cette île.",
            "Tut mir leid, ich bin nicht von hier. Bei uns trinkt man in Bars. Hier trinkt man in einer Höhle, heimlich, im Sturm, an einem Tisch für Riesen. Ich liebe diese Insel.",
            "Lo siento, no soy de aquí. En mi tierra bebemos en bares. Aquí se bebe en una cueva, a escondidas, en plena tormenta y en una mesa de gigantes. Me encanta esta isla.",
            "Mi scusi, non sono di qui. Da noi si beve nei bar. Qui si beve in una grotta, di nascosto, durante una tempesta, a un tavolo da giganti. Adoro quest'isola.",
            "Desculpe, não sou daqui. Na minha terra bebe-se em bares. Aqui bebe-se numa gruta, às escondidas, numa tempestade, a uma mesa de gigantes. Adoro esta ilha.",
        },
        new[]
        {
            "Grrrr... A hundred years in this cave, with nothing but the drips for company. Then the rain, then that race track, and now the whole Tavern's moved in next door, with drums! A Tralu can't hear himself growl. ...And stop offering me peanuts.",
            "Grrrr... Cent ans dans cette grotte, avec les gouttes pour seule compagnie. Puis la pluie, puis ce circuit, et maintenant toute la Taverne s'installe à côté, avec des tambours ! Un Tralu ne s'entend même plus grogner. ...Et arrêtez de m'offrir des cacahuètes.",
            "Grrrr... Hundert Jahre in dieser Höhle, nur die Tropfen als Gesellschaft. Dann der Regen, dann diese Rennstrecke, und jetzt ist die ganze Taverne nebenan eingezogen, mit Trommeln! Ein Tralu hört sich nicht mal mehr selbst knurren. ...Und hört auf, mir Erdnüsse anzubieten.",
            "Grrrr... Cien años en esta cueva, sin más compañía que las goteras. Luego la lluvia, luego ese circuito, ¡y ahora toda la Taberna se ha mudado aquí al lado, con tambores! Un Tralu ya no se oye ni gruñir. ...Y dejad de ofrecerme cacahuetes.",
            "Grrrr... Cent'anni in questa grotta, con le gocce come unica compagnia. Poi la pioggia, poi quella pista, e adesso si è trasferita qui accanto tutta la Taverna, con i tamburi! Un Tralu non si sente più nemmeno ringhiare. ...E smettetela di offrirmi noccioline.",
            "Grrrr... Cem anos nesta gruta, só com as gotas por companhia. Depois a chuva, depois aquela pista, e agora a Taberna inteira mudou-se aqui para o lado, com tambores! Um Tralu já nem se ouve rosnar. ...E parem de me oferecer amendoins.",
        },
        new[]
        {
            "Ahhh... quiet at last. The aliens put the Tavern back, and the drums went home with it. Mind you, now they want me in their race. A Tralu in a car! Whatever next?",
            "Ahhh... enfin le calme. Les extraterrestres ont remis la Taverne en place, et les tambours sont rentrés avec elle. Remarque, maintenant ils veulent que je fasse leur course. Un Tralu en voiture ! Et puis quoi encore ?",
            "Ahhh... endlich Ruhe. Die Außerirdischen haben die Taverne zurückgebracht, und die Trommeln sind mit ihr heimgegangen. Allerdings wollen sie mich jetzt in ihrem Rennen haben. Ein Tralu im Auto! Was kommt als Nächstes?",
            "Ahhh... por fin, silencio. Los extraterrestres han devuelto la Taberna, y los tambores se han ido con ella. Eso sí, ahora quieren que corra en su carrera. ¡Un Tralu en coche! ¿Qué será lo próximo?",
            "Ahhh... finalmente un po' di pace. Gli alieni hanno rimesso a posto la Taverna, e i tamburi se ne sono tornati con lei. Però adesso vogliono che faccia la loro gara. Un Tralu in macchina! E poi che altro?",
            "Ahhh... finalmente, sossego. Os extraterrestres puseram a Taberna no lugar, e os tambores foram com ela. Mas agora querem-me na corrida deles. Um Tralu de carro! O que virá a seguir?",
        },
        new[]
        {
            "And between us: regulars don't come the long way round. There's a back gate, up from the old hall next door, and a key to it hidden where Sendell keeps an eye on it. You didn't hear that from me.",
            "Et entre nous : les habitués ne font pas tout le tour. Il y a une porte de derrière, en haut de la vieille salle d'à côté, et sa clé est cachée là où Sendell veille sur elle. Tu ne tiens pas ça de moi.",
            "Und unter uns: Stammgäste nehmen nicht den langen Weg. Es gibt ein Hintertor, oben über der alten Halle nebenan, und der Schlüssel dazu ist versteckt, wo Sendell ein Auge darauf hat. Von mir hast du das nicht.",
            "Y entre nosotros: los clientes de la casa no dan toda la vuelta. Hay una puerta trasera, arriba, en la vieja sala de al lado, y su llave está escondida donde Sendell la vigila. No te lo he dicho yo.",
            "E detto tra noi: i clienti abituali non fanno tutto il giro. C'è un cancello sul retro, in cima alla vecchia sala qui accanto, e la sua chiave è nascosta dove Sendell la tiene d'occhio. Non te l'ho detto io.",
            "E cá entre nós: os clientes da casa não dão a volta toda. Há um portão das traseiras, lá em cima, na velha sala aqui ao lado, e a chave dele está escondida onde Sendell a vigia. Não ouviste isto de mim.",
        },
        new[]
        {
            "There you are, Twinsen! My shift on the doorstep's over, so I'm on duty down here. Old habits: I can't sit in a bar without carrying a tray. Lemonade?",
            "Te voilà, Twinsen ! J'ai fini mon service sur le pas de la porte, alors je suis de service ici. Les vieilles habitudes : je ne sais pas m'asseoir dans un bar sans porter un plateau. Une limonade ?",
            "Da bist du ja, Twinsen! Mein Dienst an der Tür ist vorbei, also bin ich hier unten im Dienst. Alte Gewohnheit: Ich kann in keiner Bar sitzen, ohne ein Tablett zu tragen. Limonade?",
            "¡Ahí estás, Twinsen! Se acabó mi turno en la puerta, así que estoy de servicio aquí abajo. La costumbre: no sé sentarme en un bar sin llevar una bandeja. ¿Una limonada?",
            "Eccoti, Twinsen! Il mio turno sulla soglia è finito, così sono di servizio quaggiù. Vecchie abitudini: non riesco a stare in un bar senza portare un vassoio. Una limonata?",
            "Aí estás tu, Twinsen! Acabou o meu turno à porta, por isso estou de serviço aqui em baixo. Velhos hábitos: não consigo estar num bar sem levar um tabuleiro. Uma limonada?",
        },
    };

    // The new lines into a language's bank from `first` on (`words`: their column), the pharmacist's price in the game's own words there.
    private static void AddChapterTexts(HqrArchive text, int lang, Lba2TextBank bank, int first, byte attribute, int words)
    {
        AddTexts(bank, first, attribute, ChapterLines, words);
        if (Lba2TextBank.Load(text, lang, DesertTexts).Find(DesertGallicPrice) is { } own && bank.Find(first + (int)ChapterLine.GallicPrice) is { } price)
            price.Bytes = own.Bytes;
    }

    // What the steps share: the folder's scenes (as built, and before any build), the two tracks and what their scenes were given, the
    // log, RaceTrackStory.Apply's Edit and Add, and the lines' text ids.
    private sealed record StoryParts(SceneStore Store, SceneStore Originals, RaceTrackReport Storm, RaceTrackReport Town,
        RaceTrackScenes.Result Scenes, RaceTrackScenes.Result TownScenes, List<string> Log,
        Action<int, string, Func<SceneModel, Action<SceneScripts>>> Edit, Func<SceneModel, SceneActorModel, string, Action<SceneScripts>> Add,
        Func<ChapterLine, int> Say, Func<Line, int> Said);

    // `what` found in a script, whatever the spaces and line breaks between its words, and replaced once
    private static string Loose(string text, string what, string with)
    {
        var pattern = string.Join(@"\s*", what.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(Regex.Escape));
        var m = Regex.Match(text, pattern);
        if (!m.Success) throw new NotTheGames($"no \"{what.Split('\n')[0].Trim()}\"");
        return text[..m.Index] + with + text[(m.Index + m.Length)..];
    }

    // The nearest place to an island cell clear of every road of a track by `clear` cells, on its ground (the sea's out), and out of the
    // island's search zones (Givers): searched outward ring by ring, half a cell at a time, up to `reach` cells. Null when there's none.
    private static (double X, double Z, double Y)? OffRoad(RaceTrackReport report, double x, double z, double clear, double reach = 10)
    {
        if (report.GroundAfter is not { } ground) return null;
        for (var r = 0.0; r <= reach; r += 0.5)
        {
            var steps = r == 0 ? 1 : (int)Math.Ceiling(2 * Math.PI * r / 0.5);
            for (var k = 0; k < steps; k++)
            {
                double a = 2 * Math.PI * k / steps, px = x + r * Math.Sin(a), pz = z + r * Math.Cos(a);
                if (OffRoads(report, px, pz) < clear) continue;
                if (givers.Any(b => px > b.X0 - GiverGap && px < b.X1 + GiverGap && pz > b.Z0 - GiverGap && pz < b.Z1 + GiverGap)) continue;
                var g = ground(px, pz);
                if (g < 200) continue;
                return (px, pz, g);
            }
        }
        return null;
    }

    // The island's search zones (its outside scenes' zones of type 4: a bin, a barrel, a vase -- Action there is Twinsen searching it, and
    // no one standing in one can be talked to: the Tavern's front is one), in island cells; and how far a person stands from them
    private static List<(double X0, double Z0, double X1, double Z1)> givers = new();
    private const double GiverGap = 0.75;
    private static List<(double X0, double Z0, double X1, double Z1)> Givers(SceneStore store)
    {
        var boxes = new List<(double, double, double, double)>();
        for (var s = RaceTrackIsland.Citadel.FirstScene; s <= RaceTrackIsland.Citadel.LastScene; s++)
        {
            if (!store.SceneExists(s)) continue;
            var m = store.Load(s);
            if (m.CubeMode != 1) continue;
            foreach (var zn in m.Zones.Where(zn => zn.Type == 4))
                boxes.Add((m.CubeX * 64 + zn.X0 / 512.0, m.CubeY * 64 + zn.Z0 / 512.0, m.CubeX * 64 + zn.X1 / 512.0, m.CubeY * 64 + zn.Z1 / 512.0));
        }
        return boxes;
    }

    // ... facing another place
    private static (double X, double Z, double Y, int Beta) Facing((double X, double Z, double Y) at, double x, double z) =>
        (at.X, at.Z, at.Y, RaceTrackMath.Turn(Math.Atan2(x - at.X, z - at.Z)));

    // The island's outside scene whose cube holds an island cell (-1: none)
    private static int OutsideScene(SceneStore store, double x, double z)
    {
        for (var s = RaceTrackIsland.Citadel.FirstScene; s <= RaceTrackIsland.Citadel.LastScene; s++)
        {
            if (!store.SceneExists(s)) continue;
            var m = store.Load(s);
            if (m.CubeMode == 1 && m.CubeX == (int)Math.Floor(x / 64) && m.CubeY == (int)Math.Floor(z / 64)) return s;
        }
        return -1;
    }

    // A talker: someone who says what `talk` says when Twinsen, on foot, presses Action within `reach` of them -- while `when` holds when
    // the scene starts (and gone otherwise). `extra`: more of the life script, each frame.
    private static string Talker(string when, string talk, int reach = 1500, string extra = "") => $@"void comportement_0()
{{
    if ({when})
    {{
        set_comportement(comportement_1);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
{extra}    swif (1 == action())
    {{
        if ({reach} > distance(0) && {Driving} != comportement_hero())
        {{
{talk}
        }}
    }}
}}
";

    // An actor of the game's, copied (null when the scene or actor isn't there)
    private static SceneActorModel? Copy(SceneStore originals, int scene, int actor) =>
        originals.SceneExists(scene) ? originals.Load(scene).Actors.ElementAtOrDefault(actor)?.Clone() : null;
    private static SceneActorModel? CopyOf(SceneStore originals, int scene, int entity) =>
        originals.SceneExists(scene) ? originals.Load(scene).Actors.Skip(1).FirstOrDefault(a => a.Entity == entity)?.Clone() : null;

    private static void Chapters(StoryParts p)
    {
        givers = Givers(p.Store);
        Morning(p);
        Thief(p);
        StormPeople(p);
        // (the Tralu's cave as the regulars' secret tavern in the storm -- Citadel/RaceTrackStory.TraluTavern.cs)
        TraluTavern(p);
        // (the cave's mouth: in from anywhere in front of it -- Citadel/RaceTrackStory.TraluCaves.cs)
        TraluCaveMouth(p);
        Lighthouse(p);
        LighthouseClimb(p);
        HiddenPages(p);
        TownPeople(p);
        Yard(p);
        YardZoeLeaves(p);
        StartLine(p);
        PackageLevers(p);
        RetoldGates(p);
        PharmacyHold(p);
        // (the easter eggs' LBA1 rooms: the sewers' secret room, the Tavern's cellar -- Citadel/RaceTrackStory.Lba1Rooms.cs)
        Lba1Rooms(p);
    }

    // ---- the game's own lines put right for the story's days (the sweep of 2026-11-05) -------------------------------------------------
    // IslandRetell gives the game's people the story's words, text by text; where a line is right one day and wrong the next, or comes
    // with a holomap arrow or a variable of the game's own quests, it is put right here, in the scripts (each its own edit: one the game's
    // script doesn't have is logged and left out, the others stand):
    //   - the pharmacy (scene 22) in chapter 2: Twinsen no longer asks for something for his hands (the gloves were found in the storm),
    //     and the pharmacist's chapter 2 is the morning ferry's delivery -- not "try again after the storm" and her customer's healing
    //     wizard on the Desert, with the Desert's arrow and the game's variable 62, which sets Ker'aooc's people talking of the Dino-Fly;
    //   - Mr. Paul on the quay (43): no more word from his missus about Ker'aooc curing the Dino-Fly (the same arrow, the same variable);
    //   - the attic's transmitter (1): Baldino's call in chapter 2 only, the morning the radio comes on the ferry (IslandRetell's text 31),
    //     and no arrow to his house on the Desert (the game's said it the evening of the race won too, the island still in the storm);
    //   - Zoe at home, the morning of the storm (0): no arrow to the pharmacy after "found your gloves yet?" -- the Wizard's, from her
    //     opening, stays;
    //   - the mushroom man by the Wizard's tent (45), the race won: the race, not the Esmers, who land the next morning;
    //   - the Dino-Fly in the yard (49): the storm in chapter 1, the sunshine in chapter 2.
    // (the attic is RaceTrackStory.cs's Attic, the yard by the carport its CarScene)
    private const int Transmitter = 5, Pharmacist = 3, QuayPaul = 4, WizardTentOutside = 45, MushroomMan = 4, DinoFlyActor = 3;
    private static void RetoldGates(StoryParts p)
    {
        void Gate(int scene, int actor, string what, string from, string to) => p.Edit(scene, what, _ => scripts =>
            scripts.SetText(actor, ScriptKind.Life, Loose(scripts.GetText(actor, ScriptKind.Life), from, to)));

        Gate(Pharmacy, 0, "the pharmacy in chapter 2: Twinsen no longer asks for something for his hands",
            "if (0 == var_game(42))\n{\nmessage(252);\n}", "");
        Gate(Pharmacy, Pharmacist, "the pharmacist in chapter 2: the morning ferry's delivery (no healing wizard, no arrow to the Desert)",
            "if (0 == var_game(42))\n{\nmessage(246);\nclr_holo_pos(22);\nif (0 == var_game(62))\n{\nmessage(18);\nset_holo_pos(35);\nset_var_game(62, 1);\n}\n}",
            $"message({p.Say(ChapterLine.PharmacyFerry)});\nclr_holo_pos(22);");
        Gate(Harbour, QuayPaul, "Mr. Paul on the quay: no word of Ker'aooc and the Dino-Fly (no arrow to the Desert)",
            "if (0 == var_game(62))\n{\nmessage(950);\nset_holo_pos(35);\nset_var_game(62, 1);\n}", "");
        Gate(Attic, Transmitter, "the attic's transmitter: Baldino's call in chapter 2 only, no arrow to his house",
            "if (3 > chapter() && 0 == var_game(26) && 2 < var_game(51))\n{\nmessage(31);\nset_holo_pos(38);",
            "if (2 == chapter() && 0 == var_game(26) && 2 < var_game(51))\n{\nmessage(31);");
        Gate(OpeningScene, Zoe, "Zoe's \"found your gloves yet?\": no arrow to the pharmacy",
            "message_obj(0, 303);\nset_holo_pos(22);", "message_obj(0, 303);");
        Gate(WizardTentOutside, MushroomMan, "the mushroom man, the race won: the race, not the Esmers",
            "default:\nmessage(343);", $"default:\nmessage({p.Say(ChapterLine.MushroomWon)});");
        Gate(CarScene, DinoFlyActor, "the Dino-Fly: the storm in chapter 1, the sunshine in chapter 2",
            "else if (0 == var_game(42))\n{\nset_anim_dial(337);\nmessage(216);\n}", $@"else if (0 == var_game(42))
        {{
            set_anim_dial(337);
            if (2 > chapter())
            {{
                message(216);
            }}
            else
            {{
                message({p.Say(ChapterLine.DinoFlySun)});
            }}
        }}");
    }

    // ---- the pharmacy's umbrella snatch (scene 22): Twinsen held till the customer is out -------------------------------------------
    // The user, 2026-11-05: in the retail game "Twinsen can't move while the thief comes in, steals the umbrella, leaves and the customer
    // follows him outside"; in the story he could. The game's scripts are the story's (only the texts retold), and headless, holding a
    // movement key all through, the story build and the retail game move him the same, frame for frame: the game's hold (Twinsen's
    // comportement_1, `oneif (40 == l_track_obj(6))`: MOVE_FOLLOW on the customer, cinema mode, until her label 52) starts only at
    // the customer's "if you do me a favour" (her label 40) -- the thief is already walking in from the door then, since her label 30
    // (the pharmacist's answer) -- and ends on her way to the door, before she is out. So he walked about while the thief came in and
    // while the customer followed him out. Now the hold starts with her label 30, the moment the thief comes in, and ends once she is
    // out of the door (her label 105: var_cube 3 back to 0 as she goes) -- or if she stops on the way to ask him for the umbrella
    // (her label 20, then 102: the game's, when he stands within 1350 of her path; held, he could never answer her or walk off).
    private static void PharmacyHold(StoryParts p) => p.Edit(Pharmacy, "the pharmacy's umbrella snatch: Twinsen held from the thief coming in till the customer has followed him out", _ => scripts =>
    {
        var hero = scripts.GetText(0, ScriptKind.Life);
        hero = Loose(hero, "oneif (40 == l_track_obj(6))\n{\nset_dir(MOVE_FOLLOW, 6);", "oneif (30 == l_track_obj(6))\n    {\n        set_dir(MOVE_FOLLOW, 6);");
        hero = Loose(hero, "if (52 == l_track_obj(6))\n{\nrestore_hero();", "if (0 == var_cube(3) || 105 == l_track_obj(6) || 102 == l_track_obj(6))\n    {\n        restore_hero();");
        scripts.SetText(0, ScriptKind.Life, hero);
    });

    // ---- the package centre's hidden room (scene 16): its three levers ------------------------------------------------------------
    // The user, 2026-11-03: "Inside the package centre on Citadel Island there's a hidden room with three levers. Currently if Twinsen
    // pulls the third lever he gets rewarded with a box containing 102 kashes. Let's change the levers he has to pull so in order to get a
    // reward he has to pull the middle lever, followed by the right lever - this is a call back to the first game." The game's levers
    // (actors 4, 5, 6 -- the right, the middle, the left as the picture shows them) each send the pusher (actor 3) to its place, and the
    // box (actor 2) down that way: the right's (the third from the left) to the floor and Twinsen, the others up onto the racks and round
    // back to the start, to be tried again. Now the middle lever is pulled down and remembered (var_cube LeversSet), the box left where
    // it is; the right lever after it sends the box the rewarding way, and without it the left's way, round and back; the left lever
    // forgets the middle.
    private const int LeversScene = 16, LeversSet = 5;
    private static void PackageLevers(StoryParts p) => p.Edit(LeversScene, "the package centre's levers: the middle, then the right (LBA1's call back)", _ => scripts =>
    {
        var hero = scripts.GetText(0, ScriptKind.Life);
        hero = Loose(hero, "set_track_obj(3, label_3);", $@"if (1 == var_cube({LeversSet}))
        {{
            set_track_obj(3, label_3);
        }}
        else
        {{
            set_track_obj(3, label_1);
        }}
        set_var_cube({LeversSet}, 0);");
        hero = Loose(hero, "set_var_cube(0, 1); set_track_obj(5, label_0); set_track_obj(3, label_2);", $@"set_track_obj(5, label_0);
        set_var_cube({LeversSet}, 1);");
        hero = Loose(hero, "set_track_obj(3, label_1); set_comportement(comportement_0);", $@"set_track_obj(3, label_1);
        set_var_cube({LeversSet}, 0);
        set_comportement(comportement_0);");
        scripts.SetText(0, ScriptKind.Life, hero);
    });

    // ---- the night and the morning (scene 0) -------------------------------------------------------------------------------------
    // The bed's Action (Apply step 1) made it the night and started the scene again: Twinsen lies asleep in his bed (Polar's places and
    // scene 101's animations, as the dream's waking), Zoe beside it waits a moment and wakes him with the news of the landing, he answers,
    // sits up and gets out of bed -- then it is chapter 2 (inc_chapter: the fine weather's island from the door on), the landing is done
    // (var 70: the game's landing cutscene never runs) and the day has begun (DayVar Rested: the town circuit's gate open). Later in the
    // day, coming home before Luc has told of his wheel, Zoe's word at the door (Q_LUCCAR 1). The hero's labels 170-172 and Zoe's 180-183
    // are new (PolarDream's are 150, 151 and 160-164); its scene-start branch goes before the game's own, which PolarDream's own then
    // finds as it always did.
    private static void Morning(StoryParts p) => p.Edit(OpeningScene, "the night: Twinsen asleep in his bed, Zoe wakes him to the morning the aliens land (chapter 2); later Zoe's word at the door", model =>
    {
        var first = model.TrackPoints.Count;
        model.TrackPoints.Add(new SceneTrackPoint(BedSpot.X, BedSpot.Y, BedSpot.Z));
        model.TrackPoints.Add(new SceneTrackPoint(OutOfBed.X, OutOfBed.Y, OutOfBed.Z));
        model.TrackPoints.Add(new SceneTrackPoint(ZoeAside.X, ZoeAside.Y, ZoeAside.Z));
        int bed = first, up = first + 1, aside = first + 2;
        return scripts =>
        {
            var hero = scripts.GetText(0, ScriptKind.Life);
            hero = Loose(hero, "void comportement_0()\n{", $@"void comportement_0()
{{
    if ({Night} == var_game({DayVar}))
    {{
        cinema_mode(1);
        pos_point({bed});
        beta(0);
        shadow_obj(0, 0);
        set_dir(NO_MOVE);
        anim({Asleep});
        set_track(label_170);
        set_comportement(comportement_70);
    }}
    else");
            hero = Loose(hero, "void comportement_1()\n{", $@"void comportement_1()
{{
    if (2 == chapter() && 0 == var_game({QLucCar}) && {Rested} == var_game({DayVar}) && 0 == var_cube({Once1}))
    {{
        set_var_game({QLucCar}, {LucWord});
        message_zoe({p.Say(ChapterLine.ZoeDoor)});
    }}");
            hero = hero.TrimEnd() + $@"

void comportement_70()
{{
    if (1 == var_cube({Once1}))
    {{
        shadow_obj(0, 1);
        set_dir(MOVE_MANUAL);
        comportement_hero(0);
        cinema_mode(0);
        set_comportement(comportement_1);
    }}
}}
";
            scripts.SetText(0, ScriptKind.Life, hero);
            scripts.SetText(0, ScriptKind.Track, scripts.GetText(0, ScriptKind.Track).TrimEnd() + $@"

label(170);
anim({Asleep});
stop();

label(171);
anim({SitUp});
wait_anim();
anim({GetUp});
wait_anim();
pos_point({up});
anim(0);
angle(0);

label(172);
stop();
");
            var zoe = scripts.GetText(Zoe, ScriptKind.Life);
            zoe = Loose(zoe, "void comportement_0()\n{", $@"void comportement_0()
{{
    if ({Night} == var_game({DayVar}))
    {{
        set_track(label_180);
        set_comportement(comportement_70);
    }}
    else");
            zoe = zoe.TrimEnd() + $@"

void comportement_70()
{{
    if (181 == l_track())
    {{
        message({p.Say(ChapterLine.ZoeMorning)});
        message_obj(0, {p.Say(ChapterLine.TwinsenComing)});
        set_track(label_182);
        set_track_obj(0, label_171);
        set_comportement(comportement_71);
    }}
}}

void comportement_71()
{{
    if (172 == l_track_obj(0))
    {{
        set_var_game({DayVar}, {Rested});
        if (2 > chapter())
        {{
            inc_chapter();
        }}
        set_var_game({AliensLanded}, 1);
        set_holo_pos({TownArrow});
        set_var_cube({Once1}, 1);
        set_comportement(comportement_72);
    }}
}}

void comportement_72()
{{
    if (0 == var_cube({Once1}))
    {{
        set_var_cube({Once1}, 1);
    }}
}}
";
            scripts.SetText(Zoe, ScriptKind.Life, zoe);
            scripts.SetText(Zoe, ScriptKind.Track, scripts.GetText(Zoe, ScriptKind.Track).TrimEnd() + $@"

label(180);
anim(0);
face_twinsen(-1);
wait_nb_second(2);

label(181);
stop();

label(182);
anim(1);
goto_point({aside});
anim(0);
face_twinsen(-1);

label(183);
stop();
");
        };
    });

    // ---- the thief (scenes 43, 17, 18) -----------------------------------------------------------------------------------------------
    private static void Thief(StoryParts p)
    {
        // (the sewers the 1996 demo's when the build put them in -- Demo96Rooms.Sewers: the painter at his place in the demo's -- the hatch
        // landing on its cellar lift's platform, the thief cornered by the barrels beside the far halls' lift, no locked door between)
        var demoSewers = p.Store.SceneExists(Sewers) && p.Store.Load(Sewers).Actors.Any(a => a.X == DemoPainter.X && a.Z == DemoPainter.Z);
        var liftTop = demoSewers ? DemoSewerArrival : SewerLiftTop;
        var deadEnd = demoSewers ? DemoDeadEnd : DeadEnd;
        // the harbour: the game's umbrella thief on the quay below the warehouses (with the umbrella: his body 100, as scene 48 shows him before he
        // gives it back), from Zoe's word on, until he is down the hatch; within three cells of Twinsen on foot he says so, and runs to the
        // hatch on the quay and down it. The hatch is a door of its own into the sewers (scene 17) at the lift from the storm track's start
        // line -- as if from that lift -- switched on in the storm once he is down (HatchSwitch); the far room's ladder comes out beside it.
        var thief = Copy(p.Originals, UmbrellaScene, UmbrellaThief);
        var behind = OffRoad(p.Storm, ThiefSpot.X, ThiefSpot.Z, 1);
        var hatch = OffRoad(p.Storm, Hatch.X, Hatch.Z, 1);
        (double X, double Z, double Y)? quay = null;
        if (hatch is { } h)
            for (var d = 1.5; d <= 4 && quay is null; d += 0.5)
                foreach (var (dx, dz) in new[] { (1.0, 0.0), (0.0, 1.0), (-1.0, 0.0), (0.0, -1.0) })
                    if (OffRoad(p.Storm, h.X + dx * d, h.Z + dz * d, 1, 0) is { } q) { quay = q; break; }
        if (thief is null || behind is not { } b || hatch is not { } ht || quay is not { } qy)
        {
            p.Log.Add($"the thief left out: {(thief is null ? $"no umbrella thief in scene {UmbrellaScene}" : "no room off the storm track's roads at the harbour")}");
            return;
        }
        p.Edit(Harbour, $"the thief on the harbour's quay at cell ({b.X:0.#}, {b.Z:0.#}) runs from Twinsen to a sewer hatch at ({ht.X:0.#}, {ht.Z:0.#}) and down it (variable {QThief} {ThiefDown})", model =>
        {
            if (model.CubeX != (int)Math.Floor(b.X / 64) || model.CubeY != (int)Math.Floor(b.Z / 64) || model.CubeX != (int)Math.Floor(ht.X / 64) || model.CubeY != (int)Math.Floor(ht.Z / 64))
                throw new NotTheGames("the thief's place and the hatch aren't in the harbour's cube");
            Place(thief, model, Facing(b, ht.X, ht.Z));
            var run = model.TrackPoints.Count;
            model.TrackPoints.Add(new SceneTrackPoint(RaceTrackScenes.CubeLocal(ht.X, model.CubeX), (int)Math.Round(ht.Y), RaceTrackScenes.CubeLocal(ht.Z, model.CubeY)));
            int hx = RaceTrackScenes.CubeLocal(ht.X, model.CubeX), hz = RaceTrackScenes.CubeLocal(ht.Z, model.CubeY), hy = (int)Math.Round(ht.Y);
            model.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = Sewers, X0 = hx - 256, X1 = hx + 255, Z0 = hz - 256, Z1 = hz + 255, Y0 = hy - 200, Y1 = hy + 1500,
                // (into the sewers at the lift's top -- the engine adds Twinsen's height over the zone's foot, 200 here -- switched off
                // until the thief is down)
                Info = new[] { liftTop.X, liftTop.Y - 200, liftTop.Z, 0, HatchSwitch, 0, 1, 0 },
            });
            // (the hatch itself, on the quay over the zone -- before the thief, who must stay the scene's last actor)
            // (one Twinsen stands on, as the game's hatches are -- OBJ_CARRIER: a sprite is solid, and the hatch pushed him off its own door)
            var hatchProp = RaceTrackScenes.SpriteProp(hx, hy, hz, RaceTrackScenes.HatchSprite);
            hatchProp.Flags |= RaceTrackScenes.CarrierFlag;
            SceneOps.AddActor(model, hatchProp);
            var life = p.Add(model, thief, $@"void comportement_0()
{{
    if (2 > chapter() && {ThiefDown} <= var_game({QThief}))
    {{
        set_change_cube({HatchSwitch}, 1);
    }}
    if (2 > chapter() && 0 < var_game({QThief}) && {ThiefDown} > var_game({QThief}))
    {{
        body(100);
        set_track(label_0);
        set_comportement(comportement_1);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
    if (1536 > distance(0) && {Driving} != comportement_hero())
    {{
        message({p.Say(ChapterLine.ThiefRuns)});
        set_var_game({QThief}, {ThiefDown});
        set_change_cube({HatchSwitch}, 1);
        set_track(label_10);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if (11 == l_track() || 6000 < distance(0))
    {{
        invisible(1);
        suicide();
    }}
}}
");
            return scripts =>
            {
                life(scripts);
                var index = model.Actors.Count - 1;
                scripts.SetText(index, ScriptKind.Track, $@"label(0);
anim(0);
stop();

label(10);
anim({ThiefRun});
goto_point({run});

label(11);
stop();
");
            };
        });

        // the sewers: the painter (scene 17's actor 12, the game's sewer worker) says his own line first, then -- asked again in the same
        // visit, while the thief is somewhere ahead -- that he went through to the far tunnel (the game's 237, retold so); and the far
        // tunnel's door (actor 17, a key's in the game) opens for the chase
        if (demoSewers)
            p.Edit(Sewers, "the demo's sewers' painter: his own line, then -- asked again while the thief is ahead -- the thief gone through to the far halls", _ => scripts =>
            {
                // the demo's: `if (1 == chapter()) { message(thief); } message(own);` -- both at once, the thief's first. The walkthrough
                // (and the game's own painter below): his own line first, the thief's when Twinsen asks again (var_cube Once1).
                var painter = scripts.GetText(DemoPainterActor, ScriptKind.Life);
                var m = Regex.Match(painter, @"if\s*\(1 == chapter\(\)\)\s*\{\s*(message\(\d+\);)\s*\}\s*(message\(\d+\);)");
                if (!m.Success) throw new NotTheGames("the demo painter's two lines");
                painter = painter[..m.Index] + $@"if (1 == var_cube({Once1}) && {ThiefDown} == var_game({QThief}))
            {{
                {m.Groups[1].Value}
            }}
            else
            {{
                {m.Groups[2].Value}
                set_var_cube({Once1}, 1);
            }}" + painter[(m.Index + m.Length)..];
                scripts.SetText(DemoPainterActor, ScriptKind.Life, painter);
            });
        else
        p.Edit(Sewers, "the sewers' painter: his own line, then the thief gone through to the far tunnel; the far tunnel's door opens once the thief is down", _ => scripts =>
        {
            var painter = scripts.GetText(12, ScriptKind.Life);
            painter = Loose(painter, "if (1 == var_game(51))", $"if (1 == var_cube({Once1}) && {ThiefDown} == var_game({QThief}))");
            painter = Loose(painter, "set_var_cube(1, 0);", $"set_var_cube({Once1}, 1);\n        set_var_cube(1, 0);");
            scripts.SetText(12, ScriptKind.Life, painter);
            var door = scripts.GetText(17, ScriptKind.Life);
            if (!door.Contains("use_one_little_key();")) throw new NotTheGames("the far tunnel's door");
            scripts.SetText(17, ScriptKind.Life, Loose(door, "void comportement_1()\n{", $@"void comportement_1()
{{
    if ({ThiefDown} <= var_game({QThief}) && 17 == col_obj(0))
    {{
        set_track(label_0);
        end_life();
    }}"));
        });

        // the far room (scene 18), a dead end: the thief there from the hatch on (cornered: Q_THIEF 4); Action beside him -- the gloves,
        // and he is off up the ladder, the umbrella left behind (Q_THIEF 5). The ladder: a door of its own up onto the quay beside the
        // hatch, open from then on (LadderSwitch).
        var cornered = Copy(p.Originals, UmbrellaScene, UmbrellaThief);
        if (cornered is null) return;
        p.Edit(FarSewer, $"the thief cornered in the sewers' far room gives back the driving gloves (item {GlovesSlot}) and leaves the umbrella (item {Umbrella}); a ladder up to the quay", model =>
        {
            cornered.X = deadEnd.X; cornered.Y = deadEnd.Y; cornered.Z = deadEnd.Z; cornered.Beta = 1024;
            int qx = RaceTrackScenes.CubeLocal(qy.X, (int)Math.Floor(qy.X / 64)), qz = RaceTrackScenes.CubeLocal(qy.Z, (int)Math.Floor(qy.Z / 64));
            model.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = Harbour, X0 = deadEnd.X - 256, X1 = deadEnd.X + 255, Z0 = deadEnd.Z - 256, Z1 = deadEnd.Z + 255, Y0 = deadEnd.Y, Y1 = deadEnd.Y + 1536,
                // (out on the quay beside the hatch, at its ground, readjusted onto it; switched off until the thief has gone up)
                Info = new[] { qx, (int)Math.Round(qy.Y) + 100, qz, 0, LadderSwitch, 0, 0, 0 },
            });
            return p.Add(model, cornered, $@"void comportement_0()
{{
    if ({GlovesBack} <= var_game({QThief}))
    {{
        set_change_cube({LadderSwitch}, 1);
    }}
    if (2 > chapter() && {ThiefDown} <= var_game({QThief}) && {GlovesBack} > var_game({QThief}))
    {{
        set_var_game({QThief}, {ThiefCornered});
        body(100);
        set_comportement(comportement_1);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if (1500 > distance(0))
        {{
            message({p.Say(ChapterLine.ThiefCaught)});
            found_object({GlovesSlot});
            set_var_game({GlovesSlot}, 1);
            body(0);
            invisible(1);
            found_object({Umbrella});
            set_var_game({Umbrella}, 1);
            set_var_game({QThief}, {GlovesBack});
            set_change_cube({LadderSwitch}, 1);
            clr_holo_pos({HarbourPos});
            suicide();
        }}
    }}
}}
");
        });
    }

    // ---- chapter 1's people by the storm track (scene 42 in the storm) -------------------------------------------------------------
    // Tim the waiter on the Tavern's old doorstep: the car park, the thief (Q_THIEF 2, the harbour's arrow), the umbrella back for ten
    // kashes (6); tired, the lights over the sea -- said as himself, not as the tired line everyone else says (DayVar is the tired line's
    // variable, RACEMOD.CPP tired=: away from Tired just while he says it -- the message is said there and then). Asked again while the
    // thief is still out there, the mayor's ban on drink and nobody having seen Luc all day (once a visit, Once4; then the thief again),
    // and that is his word once the umbrella is back: the secret bar's hint (Citadel/RaceTrackStory.TraluTavern.cs). He is there while
    // the umbrella isn't back, and again once Twinsen is tired (the lights); from the umbrella back till then he is off duty, serving in
    // the secret bar (TimInBar) -- never both (the 2026-11-06 report: "Tim the waiter could be in the new bar area too"). And Flick the
    // marshal, at her plinth by the start line (the marshal herself is the race-track mode's, marshal=): no gloves, no flag.
    private static string TimOnDoorstep => $"2 > chapter() && ({UmbrellaBack} != var_game({QThief}) || {Tired} == var_game({DayVar}))";
    private static string TimInBar => $"2 > chapter() && {UmbrellaBack} == var_game({QThief}) && {Tired} != var_game({DayVar})";
    private static void StormPeople(StoryParts p)
    {
        var tim = Copy(p.Originals, TavernScene, TavernTim);
        if (tim is not null && OffRoad(p.Storm, TavernDoor.X, TavernDoor.Z, 1) is { } step && OutsideScene(p.Store, step.X, step.Z) is var scene and >= 0)
            p.Edit(scene, $"Tim on the Tavern's old doorstep at cell ({step.X:0.#}, {step.Z:0.#}) in the storm: the thief, the umbrella", model =>
            {
                Place(tim, model, Facing(step, p.Storm.StartLine[0].X, p.Storm.StartLine[0].Z));
                return p.Add(model, tim, Talker(TimOnDoorstep, $@"            if ({Tired} == var_game({DayVar}))
            {{
                set_var_game({DayVar}, 4);
                message({p.Say(ChapterLine.TimLights)});
                set_var_game({DayVar}, {Tired});
            }}
            else if ({GlovesBack} == var_game({QThief}) && 0 < var_game({Umbrella}))
            {{
                message({p.Say(ChapterLine.TimUmbrella)});
                add_gold_pieces({TimReward});
                set_var_game({Umbrella}, 0);
                set_var_game({QThief}, {UmbrellaBack});
            }}
            else if ({TimTold} > var_game({QThief}))
            {{
                message({p.Say(ChapterLine.TimCarPark)});
                message({p.Say(ChapterLine.TimThief)});
                set_var_game({QThief}, {TimTold});
                set_holo_pos({HarbourPos});
            }}
            else if ({GlovesBack} > var_game({QThief}))
            {{
                if (0 == var_cube({Once4}))
                {{
                    set_var_cube({Once4}, 1);
                    message({p.Say(ChapterLine.TimBan)});
                }}
                else
                {{
                    message({p.Say(ChapterLine.TimThief)});
                }}
            }}
            else
            {{
                message({p.Say(ChapterLine.TimBan)});
            }}", extra:$@"    if ({Tired} == var_game({DayVar}) && 0 == var_cube({Once2}) && 2000 > distance(0) && {Driving} != comportement_hero())
    {{
        set_var_cube({Once2}, 1);
        set_var_game({DayVar}, 4);
        message({p.Say(ChapterLine.TimLights)});
        set_var_game({DayVar}, {Tired});
    }}
"));
            });
        else p.Log.Add("Tim on the Tavern's doorstep left out: no room off the storm track there, or no Tim in scene 3");
        Marshal(p, p.Storm, "2 > chapter()", $@"            if (0 == var_game({GlovesSlot}))
            {{
                message({p.Say(ChapterLine.FlickStorm)});
            }}", "the storm track's");
    }

    // A talker at the flag marshal's place by a track's start line (the race-track mode draws her there: RACEMOD.CPP marshal=)
    private static void Marshal(StoryParts p, RaceTrackReport track, string when, string talk, string which)
    {
        if (track.Marshal is not { Length: >= 10 } m) { p.Log.Add($"Flick at {which} start line left out: no flag marshal there"); return; }
        double x = m[6] / 512.0, z = m[7] / 512.0;
        var scene = OutsideScene(p.Store, x, z);
        if (scene < 0) { p.Log.Add($"Flick at {which} start line left out: no scene at cell ({x:0.#}, {z:0.#})"); return; }
        var y = track.GroundAfter?.Invoke(x, z) ?? m[8];
        p.Edit(scene, $"Flick, the marshal, talks at {which} start line (cell {x:0.#}, {z:0.#})", model =>
        {
            var flick = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(x, model.CubeX), (int)Math.Round(Math.Max(y, m[8])), RaceTrackScenes.CubeLocal(z, model.CubeY), 9);
            return p.Add(model, flick, Talker(when, talk, 1800));
        });
    }

    // ---- the lighthouse (scene 46) -----------------------------------------------------------------------------------------------------
    // The spell is cast as the game casts it, but it doesn't move the game on (its script set chapter() to 2, the fine weather): the storm
    // stays the night. The Wizard (actor 4) says the sun comes in the morning, Raph (actor 6) that he's off to bed, Zoe -- with Twinsen,
    // walking together -- that she saw lights over the clouds; then Twinsen is tired, and back where the game's spell leaves him (scene 42).
    private static void Lighthouse(StoryParts p) => p.Edit(LighthouseScene, "the spell no longer moves the game on to chapter 2: the Wizard's, Raph's and Zoe's lines, and Twinsen is tired", _ => scripts =>
    {
        var hero = scripts.GetText(0, ScriptKind.Life);
        hero = Loose(hero, "set_var_game(253, 2);", "");
        hero = Loose(hero, $"set_var_game({KeeperPlot}, 5);", $@"set_var_game({KeeperPlot}, 5);
        message_obj({LighthouseWizard}, {p.Say(ChapterLine.WizardSpell)});
        message_obj({LighthouseKeeper}, {p.Say(ChapterLine.RaphBed)});
        message_zoe({p.Say(ChapterLine.ZoeLights)});
        set_var_game({DayVar}, {Tired});");
        // (his body back as he leaves: the spell's cutscene took it -- no_body, comportement_4 -- and the game gave it back in scene 42's
        // landing cutscene, restore_hero, which chapter 2 starts; the chapter stays 1 now, so in 42 he stood bodiless, no walking, no one
        // answering him: the scripted playthrough, chapter 1, 2026-11-05). And the cutscene's cinema mode off: his "We are ready, Master"
        // turned it on (comportement_1) and the game turned it off in that same landing cutscene, so in 42 it stayed on the rest of the
        // chapter -- the black bars, and Enter no longer recentring the camera on him (PERSO.CPP: not in cinema mode), nor the holomap
        // opening (the user, 2026-11-05: "After the lighthouse scene the camera seems to be broken, enter no longer centres the camera").
        hero = Loose(hero, "comportement_hero(0);\nchange_cube(42);", "body(0);\n        comportement_hero(0);\n        cinema_mode(0);\n        cam_follow(0);\n        change_cube(42);");
        scripts.SetText(0, ScriptKind.Life, hero);
    });

    // ---- the lighthouse's door, its top and the telescope there; the Wizard and the cow (scenes 46, 47) --------------------------------
    // The user, 2026-11-03: "let's allow Twinsen to enter the door and get to the top ... add in some binoculars like the ones already on
    // Citadel island to the top of the lighthouse - the same area Twinsen stands in the scripted segment. If Twinsen uses the telescope let's
    // have a small scripted cut scene of the weather wizard standing behind the cow and using Twinsen's hop forward with the 'x' key
    // animation and the cow producing clovers the same as it does when Twinsen does this"; "this should only be after Raph has already
    // unlocked it and not before". Once the spell is cast (KeeperPlot 5: Raph unlocked the door and they went up), Action at the door (its
    // zone, sceneric 0, facing it: the game's "It's locked." until then) takes Twinsen up to where the spell's scene has him (track point
    // 5), and Action there facing the lamp room back down to the door (point 6). The island's telescope stands beside him there
    // (CitadelLighthouse.AddTelescope); Action facing it: the game's "For 3 Kashes, do you want to use the telescope?" -- paid, the cow's
    // field (scene 47): Twinsen out of sight, the camera on the Wizard behind Rosa (her zone 11, facing her) hopping as Twinsen does
    // (CitadelLighthouse.Hop) -- the fourth hop and she gives her clover (her own script, var_cube 11 past 3) -- and back up the lighthouse.
    // (ScopeVar: 1 at the cow's, 2 on the way back -- the cow's cinema mode off again there, and the camera on Twinsen, not on the
    // Wizard's number in the cow's scene: neither goes with a change of scene, so Enter never recentred the camera on him again.
    // Twinsen's track labels 160-163 in scene 46 are new.) In chapter 1 -- the night after the
    // spell -- the telescope shows the Esmers' ship coming down over the sea instead (an easter egg: RaceTrackStory.Eggs.Citadel.cs, StormLook).
    public const int ScopeVar = 227;
    private const int CowScene = 47, LighthouseTop = 60, ScopeAsk = 396, ScopeNo = 17, ScopeYes = 397, NoKashes = 115;
    private const int TopPoint = 5, DoorPoint = 6, Up = 160, UpDone = 161, Down = 162, DownDone = 163;
    private const int Hops = 40, HopLabel = 10, Hopped = 11, Watch = 12, Watched = 13, HeroWatching = 20;
    private static readonly (int X, int Y, int Z) WizardAtCow = (22600, 8700, 23552), DoorStep = (16600, 3300, 11152);
    private const int TopClear = 80, CowCamera = 70, RosaEntity = 119;
    // (where the game has Rosa, and so the Wizard's place behind her, WizardAtCow, wherever she is)
    private static readonly (int X, int Y, int Z) RosaAt = (21984, 8700, 23440);
    // (the shot of them: a camera zone round the Wizard, off until his scene -- on the two of them, nearer than the island's 10500)
    private static readonly int[] CowShot = { 44, 34, 46, 300, 2048, 0, 5000, 8 };
    private static void LighthouseClimb(StoryParts p)
    {
        p.Edit(LighthouseScene, "the lighthouse's door once the spell is cast: up to its top and down, and the telescope there", model =>
        {
            if (model.TrackPoints.Count <= Math.Max(TopPoint, DoorPoint)) throw new NotTheGames("the lighthouse's track points");
            var top = model.TrackPoints[TopPoint];
            // (where he comes up and goes down to: the spell's places, each off the tower's collision -- point 5 on its top, 7628, and point 6
            // in the door's recess inside its box -- or the engine puts him back on whichever side he came from)
            var arrive = model.TrackPoints.Count;
            model.TrackPoints.Add(new SceneTrackPoint(top.X, top.Y + TopClear, top.Z));
            model.TrackPoints.Add(new SceneTrackPoint(DoorStep.X, DoorStep.Y, DoorStep.Z));
            int upTo = arrive, downTo = arrive + 1;
            model.Zones.Add(new SceneZoneModel { Type = 2, Num = LighthouseTop, X0 = top.X - 300, X1 = top.X + 400, Z0 = top.Z - 350, Z1 = top.Z + 400, Y0 = top.Y - 200, Y1 = top.Y + 1200, Info = new int[8] });
            var scope = RaceTrackScenes.InvisibleHelper(Citadel.CitadelLighthouse.ScopeX, Citadel.CitadelLighthouse.ScopeY + 900, Citadel.CitadelLighthouse.ScopeZ, 9);
            var index = model.Actors.Count;
            var life = p.Add(model, scope, $@"void comportement_0()
{{
    if (2 == var_game({ScopeVar}))
    {{
        set_var_game({ScopeVar}, 0);
        cinema_mode(0);
        cam_follow(0);
        set_dir_obj(0, NO_MOVE);
        set_track_obj(0, label_{Up});
        set_comportement(comportement_2);
    }}
    else
    {{
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if (4 < var_game({KeeperPlot}) && {Driving} != comportement_hero())
        {{
            if (0 == zone_obj(0) && 2560 < beta_obj(0) && 3584 > beta_obj(0))
            {{
                set_dir_obj(0, NO_MOVE);
                set_track_obj(0, label_{Up});
                set_comportement(comportement_2);
            }}
            else if ({LighthouseTop} == zone_obj(0))
            {{
                if (2560 < beta_obj(0) && 3584 > beta_obj(0))
                {{
                    add_choice({ScopeNo});
                    add_choice({ScopeYes});
                    ask_choice_obj({index}, {ScopeAsk});
                    if ({ScopeYes} == choice())
                    {{
                        if (2 < nb_gold_pieces())
                        {{
                            give_gold_pieces(3);
                            if (1 == chapter())
                            {{
                                set_var_cube({StormLook}, 1);
                            }}
                            else
                            {{
                                set_var_game({ScopeVar}, 1);
                                change_cube({CowScene});
                            }}
                        }}
                        else
                        {{
                            message_obj(0, {NoKashes});
                        }}
                    }}
                }}
                else if (512 < beta_obj(0) && 1536 > beta_obj(0))
                {{
                    set_dir_obj(0, NO_MOVE);
                    set_track_obj(0, label_{Down});
                    set_comportement(comportement_2);
                }}
            }}
        }}
    }}
}}

void comportement_2()
{{
    if ({UpDone} == l_track_obj(0) || {DownDone} == l_track_obj(0))
    {{
        set_dir_obj(0, MOVE_MANUAL);
        set_comportement(comportement_1);
    }}
}}
");
            return scripts =>
            {
                life(scripts);
                scripts.SetText(0, ScriptKind.Track, scripts.GetText(0, ScriptKind.Track).TrimEnd() + $@"

label({Up});
pos_point({upTo});
beta(3072);

label({UpDone});
stop();

label({Down});
pos_point({downTo});
beta(1024);

label({DownDone});
stop();
");
            };
        });

        var wizard = Copy(p.Originals, LighthouseScene, LighthouseWizard);
        if (wizard is null || wizard.Entity != Citadel.CitadelLighthouse.Wizard) { p.Log.Add("the Wizard at the cow's left out: the lighthouse's Wizard (scene 46) not found"); return; }
        p.Edit(CowScene, "through the lighthouse's telescope: the Wizard hops behind Rosa, and she gives her clover", model =>
        {
            // (Rosa where the game has her -- and, in the weather whose road runs through her pasture, where the build put her aside
            // (RaceTrackScenes' ActorsAt, RACEMOD.CPP actor_at=: the town circuit's, CITABAU) -- the Wizard as far behind her in each)
            var cow = model.Actors.FindIndex(a => a.Entity == RosaEntity);
            if (cow < 1) throw new NotTheGames("Rosa the cow");
            var rosa = model.Actors[cow];
            (int X, int Y, int Z) behind = (WizardAtCow.X - RosaAt.X, WizardAtCow.Y - RosaAt.Y, WizardAtCow.Z - RosaAt.Z);
            (int X, int Y, int Z) here = (rosa.X + behind.X, rosa.Y + behind.Y, rosa.Z + behind.Z);
            var aside = (p.Scenes.ActorsAt ?? p.TownScenes.ActorsAt ?? new List<string>())
                .Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .FirstOrDefault(f => f.Length == 6 && f[1] == CowScene.ToString() && f[2] == cow.ToString());
            (int X, int Y, int Z)? there = aside is null ? null : (int.Parse(aside[3]) + behind.X, int.Parse(aside[4]) + behind.Y, int.Parse(aside[5]) + behind.Z);
            // (the weather the put-aside place is for: WeatherVar 1 while CITABAU is shown)
            var asideWeather = aside is not null && aside[0].Equals("citabau", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            void Shot(int num, (int X, int Y, int Z) at, (int X, int Y, int Z) her)
            {
                var info = (int[])CowShot.Clone();
                (info[0], info[1], info[2]) = ((at.X + her.X) / 2 / 512, her.Y / 256, (at.Z + her.Z) / 2 / 512);
                model.Zones.Add(new SceneZoneModel { Type = 1, Num = num, X0 = at.X - 400, X1 = at.X + 400, Y0 = at.Y - 1500, Y1 = at.Y + 1500, Z0 = at.Z - 500, Z1 = at.Z + 500, Info = info });
            }
            Shot(CowCamera, here, (rosa.X, rosa.Y, rosa.Z));
            var therePoint = -1;
            if (there is { } t)
            {
                Shot(CowCamera + 1, t, (int.Parse(aside![3]), int.Parse(aside[4]), int.Parse(aside[5])));
                therePoint = model.TrackPoints.Count;
                model.TrackPoints.Add(new SceneTrackPoint(t.X, t.Y, t.Z));
            }
            wizard.X = here.X; wizard.Y = here.Y; wizard.Z = here.Z; wizard.Beta = rosa.Beta + 72;
            wizard.Anim = 0;
            var moveThere = therePoint < 0 ? "" : $@"        if ({asideWeather} == var_game({WeatherVar}))
        {{
            pos_point({therePoint});
        }}
";
            var cameraOn = therePoint < 0 ? $"set_camera({CowCamera}, 1);" : $@"if ({asideWeather} == var_game({WeatherVar}))
        {{
            set_camera({CowCamera + 1}, 1);
        }}
        else
        {{
            set_camera({CowCamera}, 1);
        }}";
            var life = p.Add(model, wizard, $@"void comportement_0()
{{
    if (1 == var_game({ScopeVar}))
    {{
{moveThere}        set_var_cube({Hops}, 0);
        set_track(label_{HopLabel});
        set_comportement(comportement_1);
    }}
    else
    {{
        invisible(1);
        suicide();
    }}
}}

void comportement_1()
{{
    if ({Hopped} == l_track())
    {{
        add_var_cube(11, 1);
        add_var_cube({Hops}, 1);
        if (3 < var_cube({Hops}))
        {{
            set_track(label_{Watch});
            set_comportement(comportement_2);
        }}
        else
        {{
            set_track(label_{HopLabel});
        }}
    }}
}}

void comportement_2()
{{
    if ({Watched} == l_track())
    {{
        set_var_game({ScopeVar}, 2);
        change_cube({LighthouseScene});
    }}
}}
");
            var w = model.Actors.Count - 1;
            return scripts =>
            {
                life(scripts);
                scripts.SetText(w, ScriptKind.Track, $@"label(0);
anim(0);
stop();

label({HopLabel});
anim({Citadel.CitadelLighthouse.Hop});
wait_anim();
anim(0);
wait_nb_dizieme(4);

label({Hopped});
stop();

label({Watch});
anim(0);
wait_nb_second(4);

label({Watched});
stop();
");
                var hero = scripts.GetText(0, ScriptKind.Life);
                hero = Loose(hero, "if (6 == var_game(83))", $@"if (1 == var_game({ScopeVar}))
    {{
        invisible(1);
        set_dir(NO_MOVE);
        cinema_mode(1);
        {cameraOn}
        cam_follow({w});
        set_comportement(comportement_{HeroWatching});
    }}
    else if (6 == var_game(83))");
                scripts.SetText(0, ScriptKind.Life, hero.TrimEnd() + $@"

void comportement_{HeroWatching}()
{{
    if (0 == var_game({ScopeVar}))
    {{
        set_comportement(comportement_1);
    }}
}}
");
            };
        });
    }

    // ---- the slate's hidden pages (scenes 15, 9, 22) ---------------------------------------------------------------------------------
    private static void HiddenPages(StoryParts p)
    {
        // the museum: the blueprints in a display case (Action by it): seen (Q_BLUEPRINT 1) -- Miss Bloop (actor 9), asked, tells whose they
        // were -- and with the slate copied onto it (2), Miss Bloop's word on it
        p.Edit(Museum, "the blueprints of the dream's track in a display case: seen, and copied onto the slate; Miss Bloop tells whose they were", model =>
        {
            // (a sprite, unseen: a sprite that speaks has the picture held as it is while it speaks -- MESSAGE.CPP InitWhoSpeak, 5bae8ba --
            // where a helper of a 3D entity is "animated" and the dialogue redraws round it from the bare background)
            var file = RaceTrackScenes.SpriteProp(BlueprintFile.X, BlueprintFile.Y, BlueprintFile.Z, RaceTrackScenes.PaperSprite, 9);
            file.Flags |= RaceTrackScenes.InvisibleFlag;
            var life = p.Add(model, file, Talker("4 > chapter()", $@"            if (2 > var_game({QBlueprint}) && 0 < var_game({Slate}))
            {{
                if (0 == var_game({QBlueprint}))
                {{
                    message({p.Say(ChapterLine.BlueprintFile)});
                    message_obj(0, {p.Say(ChapterLine.BlueprintDream)});
                }}
                message_obj(0, {p.Say(ChapterLine.CopyOnto)});
                set_var_game({QBlueprint}, 2);
                message_obj(9, {p.Say(ChapterLine.BloopCopied)});
            }}
            else
            {{
                message({p.Say(ChapterLine.BlueprintFile)});
                if (2 > var_game({QBlueprint}))
                {{
                    message_obj(0, {p.Say(ChapterLine.BlueprintDream)});
                    set_var_game({QBlueprint}, 1);
                }}
            }}"));
            return scripts =>
            {
                life(scripts);
                var bloop = scripts.GetText(9, ScriptKind.Life);
                if (!bloop.Contains("message(8);")) throw new NotTheGames("Miss Bloop's");
                scripts.SetText(9, ScriptKind.Life, Loose(bloop, "else if (1 == var_cube(1))\n    {", $@"else if (1 == var_cube(1) && 1 == var_game({QBlueprint}))
    {{
        message({p.Say(ChapterLine.BloopOldThing)});
        set_var_cube(1, 0);
    }}
    else if (1 == var_cube(1))
    {{"));
            };
        });

        // the neighbour: his sheet of drawings (the game's sewer map, retold the storm track's sheet: IslandRetell) -- told of, Q_SHEET 1;
        // brought out with the gallic acid, 2, with Zoe's word on the slate filling up -- and nothing of the game's after it: no pyramid
        // (item 6), the picture of the map and its slate page left out (the sheet's page is for later)
        p.Edit(Neighbour, "the neighbour's design sheet: told of (variable 232 1), brought out with the gallic acid (2); no pyramid, no sewer map", _ => scripts =>
        {
            var hero = scripts.GetText(0, ScriptKind.Life);
            hero = Loose(hero, "pcx(4, 0);", "");
            hero = Loose(hero, "oneif (1 == var_game(28))\n{\nmemo_ardoise(5);\n}", "");
            scripts.SetText(0, ScriptKind.Life, hero);
            var man = scripts.GetText(2, ScriptKind.Life);
            man = Loose(man, "set_var_game(54, 1);", $"set_var_game(54, 1);\n        if (0 == var_game({QSheet}))\n        {{\n            set_var_game({QSheet}, 1);\n        }}");
            man = man.Replace("message(406);", $"message(406);\n    set_var_game({QSheet}, 2);\n    message_zoe({p.Say(ChapterLine.ZoeTwoTracks)});");
            man = Loose(man, "found_object(6);\nset_var_game(6, 1);", "");
            man = Loose(man, "set_var_game(6, 1);\nfound_object(6);", "");
            scripts.SetText(2, ScriptKind.Life, man);
        });

        // the pharmacy: from chapter 2 (the morning ferry's delivery), the pharmacist (actor 3) sells the gallic acid for 50 kashes, until
        // the sheet is read -- "come back when you've won something" without them
        p.Edit(Pharmacy, $"the pharmacist sells the gallic acid (item {GallicAcid}) for {GallicPrice} kashes in chapter 2", _ => scripts =>
        {
            var hero = scripts.GetText(0, ScriptKind.Life);
            scripts.SetText(0, ScriptKind.Life, Loose(hero, "if (1250 > distance_message(3))\n{\nswitch (chapter())", $@"if (1250 > distance_message(3) && 2 == chapter() && 0 == var_game({GallicAcid}) && 2 > var_game({QSheet}))
            {{
                add_choice(16);
                add_choice(17);
                ask_choice_obj(3, {p.Say(ChapterLine.GallicPrice)});
                if (16 == choice())
                {{
                    if ({GallicPrice} <= nb_gold_pieces())
                    {{
                        give_gold_pieces({GallicPrice});
                        message_obj(3, {p.Say(ChapterLine.GallicThere)});
                        found_object({GallicAcid});
                        set_var_game({GallicAcid}, 1);
                    }}
                    else
                    {{
                        message_obj(3, {p.Say(ChapterLine.GallicComeBack)});
                    }}
                }}
                set_var_cube(0, 1);
            }}
            else if (1250 > distance_message(3))
            {{
                switch (chapter())"));
        });
    }

    // ---- chapter 2's people in town (scenes 42, 3, 43, 8) ---------------------------------------------------------------------------
    private static void TownPeople(StoryParts p)
    {
        // round the Esmer of the landing: a quetch, a grobo and a rabbibunny of the game's (the landing crowd's, the Tavern's), each with
        // a word on the night's work; and Raph outside the Tavern, staring at it
        var crowd = new (SceneActorModel? Who, ChapterLine Line, double Side)[]
        {
            (CopyOf(p.Originals, AliensScene, CrowdQuetch), ChapterLine.Quetch, 2.5),
            (CopyOf(p.Originals, AliensScene, CrowdGrobo), ChapterLine.Grobo, -2.5),
            (Copy(p.Originals, TavernScene, TavernRabbibunny), ChapterLine.Rabbibunny, 4.5),
        };
        p.Edit(AliensScene, "three islanders round the Esmers' landing, each with a word on the night's work", model =>
        {
            var esmer = Landing(model, p.Town);
            var edits = new List<Action<SceneScripts>>();
            foreach (var (who, line, side) in crowd)
            {
                if (who is null || OffRoad(p.Town, esmer.X + side, esmer.Z + 1.5, 1, 4) is not { } at) continue;
                if ((int)Math.Floor(at.X / 64) != model.CubeX || (int)Math.Floor(at.Z / 64) != model.CubeY) continue;
                Place(who, model, Facing(at, esmer.X, esmer.Z));
                edits.Add(p.Add(model, who, Talker("2 == chapter()", $"            message({p.Say(line)});")));
            }
            if (edits.Count == 0) throw new NotTheGames("no room round the landing");
            return scripts => { foreach (var e in edits) e(scripts); };
        });
        var raph = Copy(p.Originals, TraluCave, CaveRaph);
        // (between the Tavern's front and the circuit's kerb, two cells and a half: on the ground just off the verge)
        if (raph is not null && OffRoad(p.Town, TavernDoor.X - 4.5, TavernDoor.Z - 0.6, 0.3) is { } spot && OutsideScene(p.Store, spot.X, spot.Z) is var raphScene and >= 0)
            p.Edit(raphScene, $"Raph outside the Tavern at cell ({spot.X:0.#}, {spot.Z:0.#}), staring at it", model =>
            {
                Place(raph, model, Facing(spot, TavernDoor.X, TavernDoor.Z - 2));
                return p.Add(model, raph, Talker("2 == chapter()", $"            message({p.Say(ChapterLine.RaphParked)});\n            message({p.Say(ChapterLine.RaphPolish)});"));
            });
        else p.Log.Add("Raph outside the Tavern left out: no room off the town circuit there");

        // in the Tavern (scene 3), Tim (actor 6) the first time he's talked to in a visit: the Tavern exactly as it was, and the umbrella
        // on its hook by the door (Q_THIEF 6) or the hook waiting for it; then his own lines (the game's, retold)
        p.Edit(TavernScene, "Tim in the Tavern put back: exactly as it was, and the umbrella's hook", _ => scripts =>
        {
            var tim = scripts.GetText(TavernTim, ScriptKind.Life);
            scripts.SetText(TavernTim, ScriptKind.Life, Loose(tim, "if (0 == var_cube(1))\n{\nif (1 == var_game(51))\n{\nmessage(43);", $@"if (0 == var_cube(1) && 2 == chapter() && 0 == var_cube({Once1}))
        {{
            set_var_cube({Once1}, 1);
            message({p.Say(ChapterLine.TimTavern)});
            if ({UmbrellaBack} == var_game({QThief}))
            {{
                message({p.Say(ChapterLine.TimHook)});
            }}
            else
            {{
                message({p.Say(ChapterLine.TimNoHook)});
            }}
        }}
        else if (0 == var_cube(1))
        {{
            if (1 == var_game(51))
            {{
                message(43);"));
        });

        // the harbour in the fine weather: Bob by the scrap (the landing crowd's), and the wheel in it beside him (Action there: Q_LUCCAR 3)
        var bob = CopyOf(p.Originals, AliensScene, CrowdBob);
        if (bob is not null && OffRoad(p.Town, Scrap.X, Scrap.Z, 1) is { } bobAt && OffRoad(p.Town, bobAt.X + 1.5, bobAt.Z, 1, 3) is { } wheelAt
            && OutsideScene(p.Store, bobAt.X, bobAt.Z) == Harbour && OutsideScene(p.Store, wheelAt.X, wheelAt.Z) == Harbour)
            p.Edit(Harbour, $"Bob by the harbour's scrap at cell ({bobAt.X:0.#}, {bobAt.Z:0.#}), the wheel for Luc's car in it beside him", model =>
            {
                Place(bob, model, Facing(bobAt, wheelAt.X, wheelAt.Z));
                var bobLife = p.Add(model, bob, Talker("2 == chapter()", $@"            if ({WheelFound} > var_game({QLucCar}))
            {{
                message({p.Say(ChapterLine.BobWheel)});
            }}"));
                var wheel = RaceTrackScenes.SpriteProp(RaceTrackScenes.CubeLocal(wheelAt.X, model.CubeX), (int)Math.Round(wheelAt.Y), RaceTrackScenes.CubeLocal(wheelAt.Z, model.CubeY), RaceTrackScenes.WheelSprite, 9);
                // (in the scrap until it is found)
                var wheelLife = p.Add(model, wheel, Talker($"2 == chapter() && {WheelFound} > var_game({QLucCar})", $@"            if ({LucTold} == var_game({QLucCar}))
            {{
                message_obj(0, {p.Say(ChapterLine.WheelSeen)});
                set_var_game({QLucCar}, {WheelFound});
            }}", 1100, $@"    if ({WheelFound} <= var_game({QLucCar}))
    {{
        suicide();
    }}
"));
                return scripts => { bobLife(scripts); wheelLife(scripts); };
            });
        else p.Log.Add("Bob and the wheel at the harbour left out: no room off the town circuit by the scrap");

        // the ferry office (scene 8): Mrs. Brune (actor 3), once Twinsen has the ticket in chapter 2, and Zoe coming too as crew
        p.Edit(FerryOffice, "Mrs. Brune at the ferry office: the ticket for the Desert, and Zoe goes free as crew", _ => scripts =>
        {
            var brune = scripts.GetText(3, ScriptKind.Life);
            scripts.SetText(3, ScriptKind.Life, Loose(brune, "message(316);", $@"if (2 == chapter() && 0 < var_game({FerryTicket}))
                {{
                    message_zoe({p.Say(ChapterLine.ZoeCrew)});
                    message({p.Say(ChapterLine.Brune)});
                }}
                else
                {{
                    message(316);
                }}"));
        });
    }

    // ---- the yard by the carport (scene 49) -------------------------------------------------------------------------------------------
    // Luc, the Tavern's boss, by his barrel car and its lost back wheel, and Zoe with her spanner: Luc tells of the wheel and Zoe of Bob's
    // scrap (Q_LUCCAR 2, the harbour's arrow); with the wheel (3) Zoe fits it, Luc stands them a round after the race, and Zoe gives the
    // portable radio from Baldino (item 26; Q_LUCCAR 4: the town circuit's gate opens). Zoe asked later: the slate's hidden pages, or the
    // two of them found. After the race, Luc's round.
    private static void Yard(StoryParts p)
    {
        var luc = Copy(p.Originals, TavernScene, TavernLuc);
        var zoe = Copy(p.Originals, OpeningScene, Zoe);
        if (luc is null || zoe is null || OffRoad(p.Town, YardSpot.X, YardSpot.Z, 1) is not { } lucAt || OffRoad(p.Town, lucAt.X + 1.5, lucAt.Z - 1, 1, 3) is not { } zoeAt
            || OutsideScene(p.Store, lucAt.X, lucAt.Z) is not (var scene and >= 0) || OutsideScene(p.Store, zoeAt.X, zoeAt.Z) != scene)
        {
            p.Log.Add("Luc and Zoe in the yard left out: no room off the town circuit by the carport");
            return;
        }
        p.Edit(scene, $"Luc and Zoe in the yard at cell ({lucAt.X:0.#}, {lucAt.Z:0.#}): his barrel car's wheel, Bob's scrap, the radio", model =>
        {
            Place(luc, model, Facing(lucAt, zoeAt.X, zoeAt.Z));
            Place(zoe, model, Facing(zoeAt, lucAt.X, lucAt.Z));
            var lucIndex = model.Actors.Count;
            var zoeIndex = lucIndex + 1;
            var fitted = $@"                message_obj({zoeIndex}, {p.Say(ChapterLine.ZoeGoodAsOld)});
                message_obj({lucIndex}, {p.Say(ChapterLine.LucRound)});
                message_obj({zoeIndex}, {p.Say(ChapterLine.ZoeRadio)});
                found_object({Radio});
                set_var_game({Radio}, 1);
                message_obj({zoeIndex}, {p.Say(ChapterLine.ZoePitBox)});
                set_var_game({QLucCar}, {WheelFitted});
                clr_holo_pos({HarbourPos});";
            var told = $@"                message_obj({lucIndex}, {p.Say(ChapterLine.LucWheel)});
                message_obj({zoeIndex}, {p.Say(ChapterLine.ZoeFitIt)});
                set_var_game({QLucCar}, {LucTold});
                set_holo_pos({HarbourPos});";
            var lucLife = p.Add(model, luc, Talker("2 == chapter()", $@"            if ({WheelFound} == var_game({QLucCar}))
            {{
{fitted}
            }}
            else if ({WheelFitted} > var_game({QLucCar}))
            {{
{told}
            }}
            else if (0 < var_game({WonVar}))
            {{
                message({p.Say(ChapterLine.LucToast)});
            }}
            else
            {{
                message({p.Say(ChapterLine.LucRound)});
            }}", 1200));
            var zoeLife = p.Add(model, zoe, Talker("2 == chapter()", $@"            if ({WheelFound} == var_game({QLucCar}))
            {{
{fitted}
            }}
            else if ({WheelFitted} > var_game({QLucCar}))
            {{
{told}
            }}
            else if (2 == var_game({QSheet}))
            {{
                message({p.Say(ChapterLine.ZoeTwoTracks)});
            }}
            else if (0 < var_game({Slate}) && (2 > var_game({QBlueprint}) || 2 > var_game({QSheet})))
            {{
                message({p.Say(ChapterLine.ZoeSlate)});
            }}
            else
            {{
                message({p.Say(ChapterLine.ZoePitBox)});
            }}", 1000));
            return scripts => { lucLife(scripts); zoeLife(scripts); };
        });
    }

    // ---- the game's Zoe in the yard, the morning of the storm (scene 49) ---------------------------------------------------------------
    // The user, 2026-11-06: "Zoe still tells Twinsen that she's off to watch the race practice, but then goes to the dinofly." The game's
    // Zoe (actor 4) comes out of the door after Twinsen (var 40 2: her place by the door, then comportement_9) with "I'm off to watch the
    // race practice..." (the game's 212, retold), or out of the door once he is away from it (var 40 1: comportement_5), and walks the
    // game's way to the Dino-Fly (track labels 2-13: var 40 3-13, 14 there), where she stays the chapter. In chapter 1 she goes the way
    // she says: north up the path out of the yard, towards the storm track (two points of her own: past the door, and the yard's north
    // edge, where the path goes into the storm track's scene), and is gone there; var 40 is 14 from her first step, as the game's
    // walk ends, so the house and the island's other scenes go on as they did, and the yard has no Zoe in chapter 1 after it (var 40
    // 3-14). Chapter 2's yard has the story's own Zoe (Yard; the game's gone there, RaceTrackStory.Retired.cs).
    private static readonly (int X, int Y, int Z) YardPath = (18816, 250, 4400), YardNorthEdge = (18816, 250, 400);
    private const int YardZoe = 4, ZoeOff = 40, ZoeGone = 41, ZoeOffComportement = 11;
    private static void YardZoeLeaves(StoryParts p) => p.Edit(CarScene, "the game's Zoe in the yard in chapter 1: off up the path to the race practice she speaks of, not to the Dino-Fly", model =>
    {
        var first = model.TrackPoints.Count;
        model.TrackPoints.Add(new SceneTrackPoint(YardPath.X, YardPath.Y, YardPath.Z));
        model.TrackPoints.Add(new SceneTrackPoint(YardNorthEdge.X, YardNorthEdge.Y, YardNorthEdge.Z));
        return scripts =>
        {
            var life = scripts.GetText(YardZoe, ScriptKind.Life);
            if (!life.Contains("set_track(label_51);") || !life.Contains("message(212);")) throw new NotTheGames("the yard's Zoe");
            // (gone as the yard starts once she has left: before the game's own start, which the retirements' gate finds as it was)
            life = Loose(life, "void comportement_0()\n{", $@"void comportement_0()
{{
    if (2 > chapter() && 2 < var_game(40) && 15 > var_game(40))
    {{
        suicide();
    }}");
            // (by the door, her line said: off north; the game's walk to the Dino-Fly from chapter 2 on -- where it never comes, she's
            // gone from the yard then)
            life = Loose(life, "set_comportement(comportement_1);\nset_dir_obj(0, MOVE_MANUAL);\nset_track(label_2);", $@"set_dir_obj(0, MOVE_MANUAL);
        if (2 > chapter())
        {{
            set_var_game(40, 14);
            set_track(label_{ZoeOff});
            set_comportement(comportement_{ZoeOffComportement});
        }}
        else
        {{
            set_comportement(comportement_1);
            set_track(label_2);
        }}");
            // (out of the door after him, once he is away: the same way)
            life = Loose(life, "set_var_game(40, 3);\nset_track(label_2);\nset_comportement(comportement_1);", $@"if (2 > chapter())
            {{
                set_var_game(40, 14);
                set_track(label_{ZoeOff});
                set_comportement(comportement_{ZoeOffComportement});
            }}
            else
            {{
                set_var_game(40, 3);
                set_track(label_2);
                set_comportement(comportement_1);
            }}");
            life = life.TrimEnd() + $@"

void comportement_{ZoeOffComportement}()
{{
    if ({ZoeGone} == l_track())
    {{
        suicide();
    }}
}}
";
            scripts.SetText(YardZoe, ScriptKind.Life, life);
            scripts.SetText(YardZoe, ScriptKind.Track, scripts.GetText(YardZoe, ScriptKind.Track).TrimEnd() + $@"

label({ZoeOff});
anim(89);
goto_point({first});
goto_point({first + 1});

label({ZoeGone});
stop();
");
        };
    });

    // ---- the town circuit's start line (its start scene) ------------------------------------------------------------------------------
    // The Esmer herald beside the line: the Two Worlds Cup, and the magic slate (item 28); Mr. Paul: six cars; and Flick at her place.
    private static void StartLine(StoryParts p)
    {
        Marshal(p, p.Town, "2 == chapter()", $@"            if ({WheelFitted} > var_game({QLucCar}))
            {{
                message({p.Say(ChapterLine.FlickTown)});
            }}", "the town circuit's");
        if (p.TownScenes.StartScene < 0 || p.Town.StartLine.Count == 0) { p.Log.Add("the herald and Mr. Paul at the town circuit's start line left out: no start scene"); return; }
        var herald = Copy(p.Originals, AliensScene, Alien);
        var paul = Copy(p.Originals, PaulHouse, PaulActor);
        var start = p.Town.StartLine[0];
        p.Edit(p.TownScenes.StartScene, "the Esmer herald (the magic slate) and Mr. Paul (six cars) by the town circuit's start line", model =>
        {
            var sides = new[] { -1, 1 };
            var edits = new List<Action<SceneScripts>>();
            // (beside the line, or -- the town circuit's line is on its bridge, over the sea -- on the nearest ground off the roads, in the
            // start scene's cube)
            bool InCube((double X, double Z, double Y, int Beta) at) => (int)Math.Floor(at.X / 64) == model.CubeX && (int)Math.Floor(at.Z / 64) == model.CubeY;
            (double X, double Z, double Y, int Beta)? Near(double back, double aside)
            {
                if (Beside(p.Town, start, back, 1, sides) is { } b && InCube(b)) return b;
                double x = start.X - start.DirX * back - start.DirZ * aside, z = start.Z - start.DirZ * back + start.DirX * aside;
                return OffRoad(p.Town, x, z, 1, 24) is { } o && Facing(o, start.X, start.Z) is var f && InCube(f) ? f : null;
            }
            if (herald is not null && Near(3, 0) is { } h)
            {
                Place(herald, model, h);
                p.Log.Add($"the herald at cell ({h.X:0.#}, {h.Z:0.#}), {h.Y:0}");
                edits.Add(p.Add(model, herald, Talker("2 == chapter()", $@"            if (0 == var_game({Slate}))
            {{
                message({p.Say(ChapterLine.HeraldWelcome)});
                message({p.Say(ChapterLine.HeraldSlate)});
                found_object({Slate});
                set_var_game({Slate}, 1);
            }}
            else if (0 < var_game({WonVar}))
            {{
                message({p.Say(ChapterLine.HeraldLeads)});
            }}
            else
            {{
                message({p.Say(ChapterLine.HeraldWelcome)});
            }}")));
            }
            // (Mr. Paul beside the herald, a cell and a half from him)
            (double X, double Z, double Y, int Beta)? Beside2((double X, double Z, double Y, int Beta) by)
            {
                foreach (var (dx, dz) in new[] { (1.5, 0.0), (-1.5, 0.0), (0.0, 1.5), (0.0, -1.5), (1.1, 1.1), (-1.1, -1.1) })
                    if (OffRoad(p.Town, by.X + dx, by.Z + dz, 1, 0) is { } o && Facing(o, start.X, start.Z) is var f && InCube(f)) return f;
                return null;
            }
            var heraldAt = herald is null ? null : Near(3, 0);
            if (paul is not null && (heraldAt is { } nextTo ? Beside2(nextTo) : Near(3, 2.5)) is { } pa)
            {
                Place(paul, model, pa);
                p.Log.Add($"Mr. Paul at cell ({pa.X:0.#}, {pa.Z:0.#}), {pa.Y:0}");
                edits.Add(p.Add(model, paul, Talker("2 == chapter()", $@"            if ({WheelFitted} > var_game({QLucCar}))
            {{
                message({p.Say(ChapterLine.PaulSix)});
            }}")));
            }
            if (edits.Count == 0) throw new NotTheGames("no room by the start line");
            return scripts => { foreach (var e in edits) e(scripts); };
        });
    }
}
