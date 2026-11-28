using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Temple of Bú's remodel (the user, 2026-11-27, asking for "humourous changes like a hoard of small skeletons that Twinsen runs over, or
// anything you think would make it more than just an empty space", then of the ten ideas offered: "Let's implement all 10 of these temple
// of bu suggestions"): the story's drivable temple (TempleOfBuDrive: scene 10, the car's way through it; scene 11 below it, on to the
// Esmers' hangar, 12) as the Cup's departure lounge for the Emerald Moon, staffed by friendly skeletons. Nobody here attacks Twinsen
// (no skeleton swipes: the game's skeleton's blade is left off its bodies, its swipe never played -- TempleRemodelProps), and everyone
// answers Action whenever he's here (chapter 3 on foot, chapter 4 by car, and any visit after).
//   1. Skeleton skittles: ten small skeletons in a bowling triangle across the car's way in the east hall, the corridor's end. The car
//      comes within reach of one and it falls in a heap of bones (the skeleton's own falling apart, 151), lies there a while, pulls itself
//      together (its getting up, 147) and shuffles back to its spot. Nothing stops the car: they are walked and driven through (armour 118,
//      RACEMOD.CPP RaceMod_NoCollide). All ten down at once is a strike: game variable 305 is 1 -- the race file's achieve=strike (the
//      engine's own list of achievements needs "strike" in it to show it: RACEGAME.CPP s_ach) -- and the warden waves his sign; he tells
//      of it when Twinsen next talks to him on foot (in the car on the Desert Twinsen is racing to the engine, which leaves out anything
//      said to him: RACEMOD.CPP RaceMod_Hush).
//   2. The traffic warden by the corridor's end, his lollipop sign (SLOW) held before him: the car coming, he holds it up high (once a
//      visit); talked to, "Reverent speed only" (the first time a visit), his grumble after.
//   3. The tour guide and three tourists in sun hats round the game's own statue of Bú in the east hall.
//   4. A skeleton miner sat in the game's minecart, stranded where the rails were (floored over, 2026-11-23): the cart itself stays where
//      it is now (its life the game's no more -- it rolled off down rails that aren't there as Twinsen came along the corridor).
//   5. Bú, patron of racers, in a racing helmet and a chequered scarf, a steering wheel in his hands, with his luck shrine's bowl: five
//      kashes in it for luck (game variable 306). The luck itself -- the Emerald Moon's race begun with an item in the box -- needs the
//      engine: the race file's start_item= is the same for every race of its file, whatever the story (RACEMOD.CPP s_startItem), so it
//      can't wait for 306 without a new key (said in the report).
//   6. The spaceport's approach: the departure board in the door's hall ("EMERALD MOON / NOW BOARDING", the second line blinking), lane
//      lights along the wooden corridor, an Esmer sign pointing the way; in scene 11, lane lights and a sign to the base's door.
//   7. The black car's tyre marks along the last hall to the stairs down, and a skeleton witness holding his skull ("Went by so fast my
//      skull fell off") -- talked to, game variable 311 is 1: the Desert's clue for the black car's case file.
//   8. The souvenir stall: the Cup's pennants, foam fingers, its stallholder.
//   9. Halloween pieces: fire bowls flickering by the way and by Bú, cobwebs in the corners, flying rats circling under the roof.
//  10. The temple's music: a skeleton playing a ribcage xylophone -- his tune (TempleRemodelProps' waltz) is the room's music now, the
//      scene's own stopped (RACEMOD.CPP quiet_scene=10).
internal static partial class RaceTrackStory
{
    // (305-309 the remodel's -- after the tow's 304; 310-319 the black car's case file's, 311 its Desert clue)
    internal const int StrikeVar = 305, BuLuckVar = 306, CaseFileDesertClue = 311;
    private const int BuOffering = 5;
    // scene 10's variables of the remodel's own (the game's scripts there use 2, 4, 6, 7, 9 and 30): the skittles down, the strike said this
    // visit, the warden's word to the car said this visit
    // (and SkittleTalked: one skittle answers an Action -- the first skittle clears it each frame, the others run after it in that frame)
    private const int SkittlesDown = 50, StrikeSaid = 51, WardenWarned = 52, SkittleTalked = 53;
    private const int RemodelScene = 10, RemodelLower = 11;
    // the floors' heights: scene 10's halls (layer 9, raised), its middle level; scene 11's corridor to the base's door
    private const int TempleHallY = 2560, TempleLowerY = 512;
    // how near on foot Action is answered; how near the car knocks a skittle over; how near it the warden sees it coming
    private const int TempleTalk = 1500, TouristTalk = 1000, SkittleReach = 900, SkittleTalk = 900, WardenSees = 3200;
    // the skittles' facing (to the camera's side, the car's way), how long they lie in a heap, how long the skeleton takes to get up (tenths)
    private const int SkittleBeta = 768, SkittleDownTenths = 80, SkittleGetUpTenths = 63;
    // the game's minecart in scene 10
    private const int MineCart = 2;

    internal enum TempleLine
    {
        SkittleTalk, SkittleDown, WardenSlow, WardenTalk, WardenStrike, GuideTalk, TouristA, TouristB, TouristC, MinerTalk,
        BuAsk, BuYes, BuNo, BuThanks, BuPoor, BuDone, WitnessTalk, WitnessMore, VendorTalk, VendorMore, XyloTalk,
    }
    internal static readonly string[][] TempleLines =
    {
        new[]
        {
            "We're the temple skittles! Go on, drive through us. We always pull ourselves together.",
            "Nous sommes les quilles du temple ! Vas-y, fonce dans le tas. On finit toujours par se remettre d'aplomb.",
            "Wir sind die Tempelkegel! Los, fahr durch uns durch. Wir kriegen uns immer wieder zusammen.",
            "¡Somos los bolos del templo! Venga, atraviésanos con el coche. Siempre volvemos a recomponernos.",
            "Siamo i birilli del tempio! Dai, passaci in mezzo con la macchina. Ci rimettiamo sempre in sesto.",
            "Somos os pinos do templo! Vá, passa por cima de nós. Recompomo-nos sempre.",
        },
        new[]
        {
            "Give us a minute... Has anyone seen my shin bone? This one's somebody else's.",
            "Une minute... Quelqu'un a vu mon tibia ? Celui-là, c'est celui d'un autre.",
            "Einen Moment... Hat jemand mein Schienbein gesehen? Das hier gehört einem anderen.",
            "Un momento... ¿Alguien ha visto mi tibia? Esta es de otro.",
            "Un momento... Qualcuno ha visto la mia tibia? Questa è di un altro.",
            "Um momento... Alguém viu a minha tíbia? Esta é de outro.",
        },
        new[]
        {
            "Reverent speed only, please! This is a temple, not a racetrack!",
            "Vitesse respectueuse, s'il te plaît ! C'est un temple ici, pas un circuit !",
            "Nur andächtiges Tempo, bitte! Das hier ist ein Tempel, keine Rennstrecke!",
            "¡Solo a velocidad reverente, por favor! ¡Esto es un templo, no un circuito!",
            "Solo velocità reverente, per favore! Questo è un tempio, non una pista!",
            "Só a velocidade reverente, por favor! Isto é um templo, não uma pista!",
        },
        new[]
        {
            "Reverent speed only. I hold up my sign, and every racer drives straight through the skittles anyway. Every single one.",
            "Vitesse respectueuse uniquement. Je lève mon panneau, et chaque pilote fonce quand même droit dans les quilles. Tous, sans exception.",
            "Nur andächtiges Tempo. Ich halte mein Schild hoch, und trotzdem fährt jeder Rennfahrer mitten durch die Kegel. Jeder einzelne.",
            "Solo velocidad reverente. Levanto mi señal, y aun así todos los pilotos atraviesan los bolos. Todos y cada uno.",
            "Solo velocità reverente. Alzo il mio cartello, e ogni pilota passa comunque dritto in mezzo ai birilli. Nessuno escluso.",
            "Só velocidade reverente. Levanto o meu sinal e, mesmo assim, todos os pilotos passam a direito pelos pinos. Todos, sem exceção.",
        },
        new[]
        {
            "STRIKE! All ten at once! That'll take them a while to sort out whose bones are whose.",
            "STRIKE ! Les dix d'un coup ! Ils vont mettre un moment à retrouver à qui sont quels os.",
            "STRIKE! Alle zehn auf einmal! Das dauert, bis die sortiert haben, welche Knochen wem gehören.",
            "¡PLENO! ¡Los diez de una vez! Les va a costar un rato saber de quién es cada hueso.",
            "STRIKE! Tutti e dieci in un colpo! Ci metteranno un po' a capire di chi sono le ossa.",
            "STRIKE! Os dez de uma vez! Vão demorar a perceber de quem são os ossos.",
        },
        new[]
        {
            "...and here we have the Temple of Bú, now the Cup's departure lounge for the Emerald Moon. Stay together, please, and mind the racing cars.",
            "... et voici le Temple de Bù, désormais la salle d'embarquement de la Coupe pour la Lune d'Émeraude. Restez groupés, s'il vous plaît, et attention aux voitures de course.",
            "... und hier sehen Sie den Tempel von Bù, heute die Abflughalle des Pokals zum Smaragdmond. Bitte zusammenbleiben und auf die Rennwagen achten.",
            "... y aquí tenemos el Templo de Bú, ahora la sala de embarque de la Copa hacia la Luna Esmeralda. No se separen, por favor, y cuidado con los coches de carreras.",
            "... ed ecco il Tempio di Bù, ora la sala d'imbarco della Coppa per la Luna di Smeraldo. Restate uniti, per favore, e attenti alle auto da corsa.",
            "... e aqui temos o Templo de Bú, agora a sala de embarque da Taça para a Lua Esmeralda. Fiquem juntos, por favor, e cuidado com os carros de corrida.",
        },
        new[]
        {
            "We came for the sun. Nobody told us the temple was indoors.",
            "On est venus pour le soleil. Personne ne nous avait dit que le temple était à l'intérieur.",
            "Wir sind wegen der Sonne gekommen. Keiner hat uns gesagt, dass der Tempel drinnen ist.",
            "Vinimos por el sol. Nadie nos dijo que el templo estaba bajo techo.",
            "Siamo venuti per il sole. Nessuno ci aveva detto che il tempio era al chiuso.",
            "Viemos pelo sol. Ninguém nos disse que o templo era coberto.",
        },
        new[]
        {
            "Would you take our picture? ...Oh, don't bother. We never come out in photos. Just the hats.",
            "Tu veux bien nous prendre en photo ? ... Oh, laisse tomber. On n'apparaît jamais sur les photos. Seulement les chapeaux.",
            "Machst du ein Foto von uns? ... Ach, lass nur. Wir sind auf Fotos nie zu sehen. Nur die Hüte.",
            "¿Nos haces una foto? ... Oh, déjalo. Nunca salimos en las fotos. Solo los sombreros.",
            "Ci fai una foto? ... Oh, lascia stare. Nelle foto non veniamo mai. Solo i cappelli.",
            "Tiras-nos uma fotografia? ... Oh, deixa. Nunca aparecemos nas fotografias. Só os chapéus.",
        },
        new[]
        {
            "Is it true you weigh less on the Emerald Moon? I'll have to hold on to my hat. And my head.",
            "C'est vrai qu'on est plus léger sur la Lune d'Émeraude ? Il va falloir que je tienne mon chapeau. Et ma tête.",
            "Stimmt es, dass man auf dem Smaragdmond leichter ist? Da muss ich meinen Hut festhalten. Und meinen Kopf.",
            "¿Es verdad que en la Luna Esmeralda se pesa menos? Tendré que sujetarme el sombrero. Y la cabeza.",
            "È vero che sulla Luna di Smeraldo si pesa di meno? Dovrò tenermi il cappello. E la testa.",
            "É verdade que na Lua Esmeralda se pesa menos? Vou ter de segurar o chapéu. E a cabeça.",
        },
        new[]
        {
            "I'm waiting for the rails to come back. They floored them over, lovely and smooth for the racing cars... I've been sat in this cart since Tuesday.",
            "J'attends que les rails reviennent. Ils les ont recouverts d'un beau sol bien lisse pour les voitures de course... Je suis assis dans ce wagonnet depuis mardi.",
            "Ich warte darauf, dass die Schienen zurückkommen. Sie haben sie zugepflastert, schön glatt für die Rennwagen... Ich sitze seit Dienstag in dieser Lore.",
            "Estoy esperando a que vuelvan los raíles. Los cubrieron con un suelo bien liso para los coches de carreras... Llevo sentado en esta vagoneta desde el martes.",
            "Sto aspettando che tornino i binari. Li hanno coperti con un bel pavimento liscio per le auto da corsa... Sono seduto in questo carrello da martedì.",
            "Estou à espera que os carris voltem. Cobriram-nos com um chão bem liso para os carros de corrida... Estou sentado neste vagão desde terça-feira.",
        },
        new[]
        {
            "BÚ, PATRON OF RACERS. Leave five Kashes in his bowl, and Bú will smile on your next race.",
            "BÙ, PROTECTEUR DES PILOTES. Laisse cinq Kashes dans son bol, et Bù sourira à ta prochaine course.",
            "BÙ, SCHUTZPATRON DER RENNFAHRER. Lass fünf Kashes in seiner Schale, und Bù wird deinem nächsten Rennen zulächeln.",
            "BÚ, PATRÓN DE LOS PILOTOS. Deja cinco Kashes en su cuenco y Bú sonreirá en tu próxima carrera.",
            "BÙ, PATRONO DEI PILOTI. Lascia cinque Kash nella sua ciotola, e Bù sorriderà alla tua prossima gara.",
            "BÚ, PADROEIRO DOS PILOTOS. Deixa cinco Kashes na taça dele, e Bú vai sorrir à tua próxima corrida.",
        },
        new[] { "Leave five Kashes", "Laisser cinq Kashes", "Fünf Kashes dalassen", "Dejar cinco Kashes", "Lasciare cinque Kash", "Deixar cinco Kashes" },
        new[] { "Not now", "Pas maintenant", "Jetzt nicht", "Ahora no", "Non ora", "Agora não" },
        new[]
        {
            "The coins clink into the bowl. Under that helmet, Bú seems to smile.",
            "Les pièces tintent dans le bol. Sous son casque, Bù semble sourire.",
            "Die Münzen klimpern in die Schale. Unter seinem Helm scheint Bù zu lächeln.",
            "Las monedas tintinean en el cuenco. Bajo su casco, Bú parece sonreír.",
            "Le monete tintinnano nella ciotola. Sotto il casco, Bù sembra sorridere.",
            "As moedas tilintam na taça. Debaixo do capacete, Bú parece sorrir.",
        },
        new[]
        {
            "You haven't got five Kashes. Bú doesn't mind: he's made of stone.",
            "Tu n'as pas cinq Kashes. Bù ne t'en veut pas : il est en pierre.",
            "Du hast keine fünf Kashes. Bù nimmt es dir nicht übel: Er ist aus Stein.",
            "No tienes cinco Kashes. A Bú no le importa: es de piedra.",
            "Non hai cinque Kash. Bù non se la prende: è di pietra.",
            "Não tens cinco Kashes. O Bú não se importa: é de pedra.",
        },
        new[]
        {
            "Your Kashes are in Bú's bowl. He's smiling on your next race already.",
            "Tes Kashes sont dans le bol de Bù. Il sourit déjà à ta prochaine course.",
            "Deine Kashes liegen in Bùs Schale. Er lächelt deinem nächsten Rennen schon zu.",
            "Tus Kashes están en el cuenco de Bú. Ya sonríe a tu próxima carrera.",
            "I tuoi Kash sono nella ciotola di Bù. Sorride già alla tua prossima gara.",
            "Os teus Kashes estão na taça do Bú. Ele já sorri à tua próxima corrida.",
        },
        new[]
        {
            "A black car came through here! Went by so fast my skull fell off. It was heading down the stairs, towards the Esmers' hangar.",
            "Une voiture noire est passée par ici ! Elle allait si vite que mon crâne est tombé. Elle filait vers l'escalier, en direction du hangar des Esmers.",
            "Hier ist ein schwarzes Auto durchgekommen! Es war so schnell, dass mir der Schädel runtergefallen ist. Es fuhr die Treppe hinunter, zum Hangar der Esmer.",
            "¡Por aquí pasó un coche negro! Iba tan rápido que se me cayó el cráneo. Bajó por las escaleras, hacia el hangar de los Esmers.",
            "È passata di qui una macchina nera! Andava così veloce che mi è caduto il teschio. È scesa per le scale, verso l'hangar degli Esmer.",
            "Passou por aqui um carro preto! Ia tão depressa que me caiu o crânio. Desceu pelas escadas, em direção ao hangar dos Esmers.",
        },
        new[]
        {
            "Who was driving? No idea. My eyes were on the floor with the rest of my head.",
            "Qui conduisait ? Aucune idée. Mes yeux étaient par terre, avec le reste de ma tête.",
            "Wer am Steuer saß? Keine Ahnung. Meine Augen lagen mit dem Rest meines Kopfes auf dem Boden.",
            "¿Quién conducía? Ni idea. Mis ojos estaban en el suelo con el resto de mi cabeza.",
            "Chi guidava? Non ne ho idea. I miei occhi erano per terra con il resto della testa.",
            "Quem conduzia? Não faço ideia. Os meus olhos estavam no chão com o resto da cabeça.",
        },
        new[]
        {
            "Cup pennants! Foam fingers! Get your souvenirs before your flight to the Emerald Moon!",
            "Fanions de la Coupe ! Mains en mousse ! Vos souvenirs avant votre vol pour la Lune d'Émeraude !",
            "Pokal-Wimpel! Schaumstoffhände! Holt euch eure Andenken vor dem Flug zum Smaragdmond!",
            "¡Banderines de la Copa! ¡Manos de espuma! ¡Llévense un recuerdo antes de su vuelo a la Luna Esmeralda!",
            "Gagliardetti della Coppa! Manone di gommapiuma! Prendete i vostri souvenir prima del volo per la Luna di Smeraldo!",
            "Galhardetes da Taça! Mãos de espuma! Levem as vossas lembranças antes do voo para a Lua Esmeralda!",
        },
        new[]
        {
            "Business is slow. Everyone who comes through here drives straight past at a reverent speed... well, at a speed.",
            "Les affaires sont calmes. Tout le monde passe devant en voiture, à une vitesse respectueuse... enfin, à une vitesse.",
            "Das Geschäft läuft schleppend. Alle fahren hier einfach vorbei, mit andächtigem Tempo... na ja, mit Tempo.",
            "El negocio va lento. Todo el que pasa por aquí sigue de largo a velocidad reverente... bueno, a cierta velocidad.",
            "Gli affari vanno a rilento. Chiunque passi di qui tira dritto a velocità reverente... be', a una certa velocità.",
            "O negócio está fraco. Toda a gente passa por aqui a direito, a velocidade reverente... enfim, a uma velocidade.",
        },
        new[]
        {
            "Like the tune? I play it on a ribcage. Not mine: I'm still using mine. This one belonged to a very large gentleman.",
            "Tu aimes l'air ? Je le joue sur une cage thoracique. Pas la mienne : je m'en sers encore. Celle-ci appartenait à un très grand monsieur.",
            "Gefällt dir die Melodie? Ich spiele sie auf einem Brustkorb. Nicht auf meinem: Den brauche ich noch. Dieser gehörte einem sehr großen Herrn.",
            "¿Te gusta la melodía? La toco en una caja torácica. No la mía: esa la sigo usando. Esta era de un señor muy grande.",
            "Ti piace la melodia? La suono su una cassa toracica. Non la mia: quella la uso ancora. Questa era di un signore molto grosso.",
            "Gostas da música? Toco-a numa caixa torácica. Não a minha: essa ainda a uso. Esta pertencia a um senhor muito grande.",
        },
    };

    // ---- where everything stands (cells: 512 scene units), scene 10 unless said
    // the skittles' triangle across the car's way just out of the corridor into the east hall, its point to the corridor: rows a little
    // over three quarters of a cell apart, a row's skittles 0.72 apart -- the back row's ends 1.08 cells (553) off the way's middle (z 57.5):
    // driven down the middle, every one within the car's reach (SkittleReach) -- a strike; along an edge, a few left standing
    private static IEnumerable<(double X, double Z)> SkittleSpots()
    {
        for (var row = 0; row < 4; row++)
            for (var k = 0; k <= row; k++)
                yield return (43.0 + 0.78 * row, 57.5 + (k - row / 2.0) * 0.72);
    }
    private static readonly (double X, double Z, int Beta) WardenAt = (38.4, 60.3, 3328);
    private static readonly (double X, double Z, int Beta) GuideAt = (53.9, 50.2, 2651);
    private static readonly (double X, double Z, int Generic, TempleLine Line)[] Tourists =
    {
        (49.8, 48.2, TempleRemodelProps.TouristA, TempleLine.TouristA),
        (51.0, 47.8, TempleRemodelProps.TouristB, TempleLine.TouristB),
        (52.2, 48.2, TempleRemodelProps.TouristC, TempleLine.TouristC),
    };
    // the miner, sat in the game's minecart: his origin this far over the floor -- sitting (the getting up's frame) his hips are some 600 over it,
    // and in the cart (400 high) they sit 170 over its floor, his ribs and head over its side
    private const int CartDeck = -430;
    private static readonly (double X, double Z) BuAt = (27.0, 13.5);
    private static readonly (double X, double Z)[] ShrineBowls = { (25.2, 14.8), (28.8, 14.8) };
    private static readonly (double X, double Z)[] LaneBowls = { (41.0, 55.25), (41.0, 59.75) };
    private static readonly (double X, double Z) StallAt = (24.5, 9.0);
    private static readonly (double X, double Z, int Beta) VendorAt = (26.9, 9.7, 512);
    private static readonly (double X, double Z) XylophoneAt = (26.5, 20.6);
    private static readonly (double X, double Z) MusicianAt = (26.5, 19.75);
    private static readonly (double X, double Z, int Beta) WitnessAt = (24.8, 26.3, 512);
    // the black car's tyre marks: from the middle level's incline west along the hall, round north up the last hall to the stairs
    private static readonly (double X, double Z, int Beta)[] TyreMarksAt = { (34.0, 37.6, 3072), (28.1, 37.6, 3072), (21.5, 37.3, 2048), (21.5, 31.4, 2048), (21.5, 25.5, 2048) };
    // the cobwebs, in corners whose walls are on their west and north (y their top)
    private static readonly (double X, double Z, int Y)[] Cobwebs = { (12.06, 5.06, 3700), (2.06, 52.06, 3700) };
    // the flying rats' rounds under the roof: the east hall's, the last hall's
    private static readonly (double X, double Z, int Y)[][] RatRounds =
    {
        new[] { (46.0, 49.0, 4400), (53.0, 47.0, 4700), (56.0, 53.0, 4300), (51.0, 58.5, 4600), (45.0, 55.0, 4200) },
        new[] { (23.0, 11.0, 4300), (28.0, 10.0, 4500), (28.5, 18.0, 4200), (26.0, 25.0, 4600), (23.5, 18.0, 4400) },
    };
    // the departure board on the door's hall's north wall (its back against it, y its foot); the Esmer sign before the corridor
    private static readonly (double X, double Z, int Y) BoardAt = (7.5, 52.0, TempleHallY + 1250);
    private static readonly (double X, double Z) SignAt = (13.0, 54.3);
    // the lane lights along the wooden corridor's edges (its planks z 56-58), each row eight from its x on
    private static readonly (double X, double Z)[] LaneRows = { (17.0, 56.13), (27.0, 56.13), (17.0, 58.87), (27.0, 58.87) };
    // scene 11: the corridor to the Esmers' base's door (x 22-25, z 29-32, layer 1) -- its lane lights, four a side, and a sign by it
    private static readonly (double X, double Z)[] LowerLaneRows = { (21.9, 29.12), (21.9, 31.88) };
    private static readonly (double X, double Z) LowerSignAt = (23.6, 29.1);

    private static int TCell(double cells) => (int)Math.Round(cells * 512);

    // A talker's life: on foot, Action within `reach`, `talk` -- every visit, every chapter; `extra` more of comportement_1 each frame,
    // `start` comportement_0's before it goes on
    private static string TempleTalker(string talk, int reach = TempleTalk, string extra = "", string start = "") => $@"void comportement_0()
{{
{start}    set_comportement(comportement_1);
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

    // The remodel into the Desert's scenes 10 and 11 (`b`); `first`: its lines' first text
    private static void TempleRemodel(IslandBeatsA b, int first)
    {
        if (!b.Store.SceneExists(RemodelScene)) { b.Log.Add($"the Temple of Bú's remodel left out: no scene {RemodelScene}"); return; }
        int Say(TempleLine l) => first + (int)l;
        b.Log.AddRange(TempleRemodelProps.InstallSkeletons(b.GameDirectory));
        var (props, propsLine) = Citadel.CitadelProps.InstallOwn(b.GameDirectory, "the Temple of Bú's remodel", TempleRemodelProps.Props);
        b.Log.Add(propsLine);
        var skeleton = TempleRemodelProps.SkeletonEntity(b.GameDirectory);
        if (props < 0 || skeleton < 0) { b.Log.Add($"the Temple of Bú's remodel left out: no props' entity ({props}) or no skeleton's ({skeleton})"); return; }
        var tune = TempleRemodelProps.InstallTune(b.GameDirectory);
        var bats = Temple.TempleHalloween.FlyingRatBody;
        var ratEntity = Lba2EntityTable.Load(b.GameDirectory)?.KindForBody(bats)?.Entity ?? -1;
        b.Log.Add($"the Temple of Bú's remodel: the xylophone's tune SAMPLES.HQR {tune} ({TempleRemodelProps.TuneSeconds:0.00} s), the flying rat's entity {ratEntity}");

        SceneActorModel Skeleton(int generic, double x, double z, int y, int beta, bool walkOver = false)
        {
            var a = SceneOps.BlankActor(SceneGame.Lba2, TCell(x), y, TCell(z), skeleton);
            a.Body = generic; a.Anim = 0; a.Beta = beta; a.Move = 3; a.HitForce = 0; a.LifePoints = -1;
            a.Flags = walkOver ? 0x0802u : 0x0803u;
            a.Armor = walkOver ? WalkOverArmour : 51;
            return a;
        }
        SceneActorModel Prop(int generic, double x, double z, int y, int beta, bool walkOver)
        {
            var a = Citadel.CitadelProps.Actor(generic, TCell(x), y, TCell(z), beta);
            a.Entity = props; a.Move = 3; a.HitForce = 0;
            if (walkOver) a.Armor = WalkOverArmour;
            return a;
        }
        const string Still = "void comportement_0()\n{\n}\n";

        b.Edit(RemodelScene, $"the Temple of Bú's remodel: the skittles, the warden, the tour, the miner, Bú's shrine, the departure board and lane lights, the tyre marks and the witness, the stall, the fire bowls, cobwebs and rats, the xylophonist (variables {StrikeVar}, {BuLuckVar}, {CaseFileDesertClue}; texts {first}-{first + TempleLines.Length - 1})", model =>
        {
            var adds = new List<Action<SceneScripts>>();
            void Track(int index, string track) => adds.Add(s => s.SetText(index, ScriptKind.Track, track));
            int Add(SceneActorModel actor, string life, string? track = null)
            {
                var index = model.Actors.Count;
                adds.Add(IslandBeatsA.Add(model, actor, life));
                if (track is not null) Track(index, track);
                return index;
            }
            int Point(double x, double z, int y) => SceneOps.AddTrackPoint(model, new SceneTrackPoint(TCell(x), y, TCell(z)));
            // (the temple's music: none of the scene's own, the xylophone's instead -- RACEMOD.CPP quiet_scene=)
            model.Music = -1;

            // 1. the skittles -- the first one starts the count of those down afresh as the scene starts
            var spots = SkittleSpots().ToList();
            for (var k = 0; k < spots.Count; k++)
            {
                var (x, z) = spots[k];
                var home = Point(x, z, TempleHallY);
                var reset = k == 0 ? $"    set_var_cube({SkittleTalked}, 0);\n" : "";
                var talk = $"            set_var_cube({SkittleTalked}, 1);\n            if (2 == l_track() || 1 == l_track())\n            {{\n                message({Say(TempleLine.SkittleDown)});\n            }}\n            else\n            {{\n                message({Say(TempleLine.SkittleTalk)});\n            }}";
                Add(Skeleton(TempleRemodelProps.Skittle, x, z, TempleHallY, SkittleBeta, walkOver: true), $@"void comportement_0()
{{
{(k == 0 ? $"    set_var_cube({SkittlesDown}, 0);\n" : "")}    set_comportement(comportement_1);
}}

void comportement_1()
{{
{reset}    if ({Driving} == comportement_hero() && {SkittleReach} > distance(0))
    {{
        add_var_cube({SkittlesDown}, 1);
        set_track(label_1);
        set_comportement(comportement_2);
    }}
    swif (1 == action())
    {{
        if (0 == var_cube({SkittleTalked}) && {SkittleTalk} > distance(0) && {Driving} != comportement_hero())
        {{
{talk}
        }}
    }}
}}

void comportement_2()
{{
{reset}    if (2 == l_track())
    {{
        sub_var_cube({SkittlesDown}, 1);
        set_comportement(comportement_3);
    }}
    swif (1 == action())
    {{
        if (0 == var_cube({SkittleTalked}) && {SkittleTalk} > distance(0) && {Driving} != comportement_hero())
        {{
{talk}
        }}
    }}
}}

void comportement_3()
{{
{reset}    if (3 == l_track())
    {{
        set_comportement(comportement_1);
    }}
    swif (1 == action())
    {{
        if (0 == var_cube({SkittleTalked}) && {SkittleTalk} > distance(0) && {Driving} != comportement_hero())
        {{
{talk}
        }}
    }}
}}
", $@"label(0);
anim({TempleRemodelProps.AnimStand});
stop();

label(1);
anim({TempleRemodelProps.AnimHeap});
wait_nb_dizieme({SkittleDownTenths});

label(2);
anim({TempleRemodelProps.AnimGetUp});
wait_nb_dizieme({SkittleGetUpTenths});
anim({TempleRemodelProps.AnimWalk});
goto_point({home});
beta({SkittleBeta});
anim({TempleRemodelProps.AnimStand});

label(3);
stop();
");
            }

            // 2. the warden: the car coming, his sign held up high (once a visit); all ten skittles down, his sign waved and game variable 305
            // set. His words wait for Twinsen on foot -- in his car on the Desert he is racing to the engine, which leaves out what anyone says
            // to him then (RACEMOD.CPP RaceMod_Hush): talked to, the strike first if there was one (StrikeSaid 1, told 2), "Reverent speed
            // only" the first time a visit, his grumble after
            Add(Skeleton(TempleRemodelProps.Warden, WardenAt.X, WardenAt.Z, TempleHallY, WardenAt.Beta), TempleTalker($@"            if (1 == var_cube({StrikeSaid}))
            {{
                message({Say(TempleLine.WardenStrike)});
                set_var_cube({StrikeSaid}, 2);
            }}
            else if (0 == var_cube({WardenWarned}))
            {{
                message({Say(TempleLine.WardenSlow)});
                set_var_cube({WardenWarned}, 1);
            }}
            else
            {{
                message({Say(TempleLine.WardenTalk)});
            }}",
                extra: $@"    if ({Driving} == comportement_hero() && {WardenSees} > distance(0) && 0 == var_cube({WardenWarned}))
    {{
        set_var_cube({WardenWarned}, 1);
        set_track(label_1);
    }}
    if (10 <= var_cube({SkittlesDown}))
    {{
        set_var_game({StrikeVar}, 1);
        if (0 == var_cube({StrikeSaid}))
        {{
            set_var_cube({StrikeSaid}, 1);
            set_track(label_1);
        }}
    }}
"),$"label(0);\nanim({TempleRemodelProps.AnimHold});\nstop();\n\nlabel(1);\nanim({TempleRemodelProps.AnimSignUp});\nwait_nb_dizieme(40);\nanim({TempleRemodelProps.AnimHold});\nstop();\n");

            // 3. the tour: the guide, three tourists in sun hats round the game's statue of Bú
            Add(Skeleton(TempleRemodelProps.Guide, GuideAt.X, GuideAt.Z, TempleHallY, GuideAt.Beta), TempleTalker($"            message({Say(TempleLine.GuideTalk)});"));
            foreach (var (x, z, generic, line) in Tourists)
                Add(Skeleton(generic, x, z, TempleHallY, 0), TempleTalker($"            message({Say(line)});", TouristTalk),
                    $"label(0);\nanim({TempleRemodelProps.AnimStand});\nstop();\n");

            // 4. the miner in the game's minecart, the cart stranded with him
            if (model.Actors.Count > MineCart && model.Actors[MineCart].Move == 8)
            {
                var cart = model.Actors[MineCart];
                adds.Add(s => s.SetText(MineCart, ScriptKind.Life, Still));
                var miner = Skeleton(TempleRemodelProps.Miner, cart.X / 512.0, cart.Z / 512.0, cart.Y + CartDeck, cart.Beta);
                miner.Flags = 0;      // (still where he sits: the cart's box round him, nothing to push him out of it)
                Add(miner, TempleTalker($"            message({Say(TempleLine.MinerTalk)});"), $"label(0);\nanim({TempleRemodelProps.AnimSit});\nstop();\n");
            }
            else b.Log.Add($"the Temple of Bú's miner left out: actor {MineCart} of scene {RemodelScene} isn't the game's minecart");

            // 5. Bú and his luck shrine
            Add(Prop(TempleRemodelProps.BuStatue, BuAt.X, BuAt.Z, TempleHallY, 0, false), TempleTalker($@"            bulle(0);
            if (1 <= var_game({BuLuckVar}))
            {{
                message({Say(TempleLine.BuDone)});
            }}
            else
            {{
                add_choice({Say(TempleLine.BuYes)});
                add_choice({Say(TempleLine.BuNo)});
                ask_choice({Say(TempleLine.BuAsk)});
                if ({Say(TempleLine.BuYes)} == choice())
                {{
                    if ({BuOffering} <= nb_gold_pieces())
                    {{
                        give_gold_pieces({BuOffering});
                        set_var_game({BuLuckVar}, 1);
                        body({TempleRemodelProps.BuStatueCoins});
                        message({Say(TempleLine.BuThanks)});
                    }}
                    else
                    {{
                        message({Say(TempleLine.BuPoor)});
                    }}
                }}
            }}
            bulle(1);", TempleTalk, start: $"    if (1 <= var_game({BuLuckVar}))\n    {{\n        body({TempleRemodelProps.BuStatueCoins});\n    }}\n"));

            // 6. the departure board, its second line blinking; the Esmer sign; the lane lights along the corridor
            Add(Prop(TempleRemodelProps.Board, BoardAt.X, BoardAt.Z, BoardAt.Y, 0, true), Still,
                $"label(0);\nbody({TempleRemodelProps.Board});\nwait_nb_dizieme(9);\nbody({TempleRemodelProps.BoardDim});\nwait_nb_dizieme(5);\ngoto(label_0);\n");
            Add(Prop(TempleRemodelProps.EsmerSign, SignAt.X, SignAt.Z, TempleHallY, 0, true), Still);
            foreach (var (x, z) in LaneRows) Add(Prop(TempleRemodelProps.LaneLights, x, z, TempleHallY, 0, true), Still);

            // 7. the tyre marks to the stairs, the witness
            foreach (var (x, z, beta) in TyreMarksAt) Add(Prop(TempleRemodelProps.TyreMarks, x, z, TempleHallY, beta, true), Still);
            Add(Skeleton(TempleRemodelProps.Witness, WitnessAt.X, WitnessAt.Z, TempleHallY, WitnessAt.Beta), TempleTalker($@"            message({Say(TempleLine.WitnessTalk)});
            message({Say(TempleLine.WitnessMore)});
            set_var_game({CaseFileDesertClue}, 1);"));

            // 8. the souvenir stall and its stallholder
            Add(Prop(TempleRemodelProps.Stall, StallAt.X, StallAt.Z, TempleHallY, 0, false), Still);
            Add(Skeleton(TempleRemodelProps.Vendor, VendorAt.X, VendorAt.Z, TempleHallY, VendorAt.Beta), TempleTalker($"            message({Say(TempleLine.VendorTalk)});\n            message({Say(TempleLine.VendorMore)});"),
                $"label(0);\nanim({TempleRemodelProps.AnimStand});\nstop();\n");

            // 9. fire bowls flickering (by the car's way, walked through; by Bú, solid), cobwebs, the flying rats' rounds
            const string Flicker = "label(0);\nbody({0});\nwait_nb_dizieme(2);\nbody({1});\nwait_nb_dizieme(3);\ngoto(label_0);\n";
            foreach (var (x, z) in LaneBowls) Add(Prop(TempleRemodelProps.FireBowl, x, z, TempleHallY, 0, true), Still, string.Format(Flicker, TempleRemodelProps.FireBowl, TempleRemodelProps.FireBowl2));
            foreach (var (x, z) in ShrineBowls) Add(Prop(TempleRemodelProps.FireBowl, x, z, TempleHallY, 1024, false), Still, string.Format(Flicker, TempleRemodelProps.FireBowl2, TempleRemodelProps.FireBowl));
            foreach (var (x, z, y) in Cobwebs) Add(Prop(TempleRemodelProps.Cobweb, x, z, y, 0, true), Still);
            if (ratEntity >= 0)
                foreach (var round in RatRounds)
                {
                    var pts = round.Select(p => Point(p.X, p.Z, p.Y)).ToList();
                    var rat = SceneOps.BlankActor(SceneGame.Lba2, TCell(round[0].X), round[0].Y, TCell(round[0].Z), ratEntity);
                    // (a carrier, though it carries nothing: a body's goto_point_3d glides only a carrier -- anything else's passes at once, and
                    // a round of them never waited a frame: the engine hung in DoTrack -- GERETRAK.CPP TM_GOTO_POINT_3D; turned to each leg first)
                    rat.Body = TempleRemodelProps.BigRat; rat.Anim = 0; rat.Move = 3; rat.Flags = 0x1000u | 0x4000u; rat.Armor = WalkOverArmour; rat.LifePoints = -1; rat.HitForce = 0;
                    var legs = string.Concat(round.Select((p, k) =>
                    {
                        var from = round[(k + round.Length - 1) % round.Length];
                        var beta = (int)Math.Round(Math.Atan2(p.X - from.X, p.Z - from.Z) * 2048 / Math.PI) & 4095;
                        return $"beta({beta});\ngoto_point_3d({pts[k]});\n";
                    }));
                    Add(rat, Still, $"label(0);\nspeed(1100);\n{legs}goto(label_0);\n");
                }
            else b.Log.Add($"the Temple of Bú's flying rats left out: no entity has BODY.HQR {bats}");

            // 10. the xylophone and its player, his tune the room's music
            Add(Prop(TempleRemodelProps.Xylophone, XylophoneAt.X, XylophoneAt.Z, TempleHallY, 0, false), Still);
            var tenths = (int)Math.Ceiling(TempleRemodelProps.TuneSeconds * 10);
            Add(Skeleton(TempleRemodelProps.Musician, MusicianAt.X, MusicianAt.Z, TempleHallY, 0), TempleTalker($"            message({Say(TempleLine.XyloTalk)});"),
                $"label(0);\nanim({TempleRemodelProps.AnimPlay});\n{(tune >= 0 ? $"\nlabel(1);\nsimple_sample({tune});\nwait_nb_dizieme({tenths});\ngoto(label_1);\n" : "stop();\n")}");

            b.Log.Add($"the Temple of Bú's remodel: scene {RemodelScene} now {model.Actors.Count} actors, {model.TrackPoints.Count} points");
            return scripts => { foreach (var add in adds) add(scripts); };
        });

        // scene 11: the corridor to the base's door -- lane lights, a sign
        if (b.Store.SceneExists(RemodelLower))
            b.Edit(RemodelLower, "the Temple of Bú's remodel: the corridor to the Esmers' base, lane lights and a sign", model =>
            {
                var adds = new List<Action<SceneScripts>>();
                foreach (var (x, z) in LowerLaneRows)
                {
                    var a = Prop(TempleRemodelProps.LaneLights, x, z, TempleLowerY, 0, true);
                    a.Body = TempleRemodelProps.LaneLightsShort;
                    adds.Add(IslandBeatsA.Add(model, a, Still));
                }
                adds.Add(IslandBeatsA.Add(model, Prop(TempleRemodelProps.EsmerSign, LowerSignAt.X, LowerSignAt.Z, TempleLowerY, 0, true), Still));
                return scripts => { foreach (var add in adds) add(scripts); };
            });
    }
}
