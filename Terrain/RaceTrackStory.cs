using System.Buffers.Binary;
using System.IO;
using System.Text;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The race track mod's story, on Citadel Island (the user's StoryNotes.txt, 2026-10-03), told through the game's own storm plot:
//   1. The game opens with Zoe walking up to Twinsen in their house (scene 0, actor 4). She is sick of the rain: find the Weather Wizard
//      and get him to fix it -- her line, and the game's own arrow to his tent (holomap position 21, which his script clears).
//   2. The wizard (scene 21, actor 2) would clear it from the top of the lighthouse, but Raph, its keeper, is too busy at the race track
//      and won't let him in -- in place of the game's "I can't find the keeper", and an arrow to the storm track's start line (222). The
//      game's plot variable 51 goes to 1 as it always did.
//   3. At the start line (the storm track's start scene) stand Raph and Mr. Paul, copies of the game's own (Raph from the Tralu's cave,
//      scene 2; Mr. Paul from his house, scene 7). Raph: beat my time and I'll come to the lighthouse (51 goes to 2, as talking to him
//      in the cave did). Mr. Paul runs the track: no racing gloves, no race -- the race-track mode's gate keeps the start line shut until
//      Twinsen has them (game variable 7: the gloves are found in the attic as before). Raph is in the cave no more (nor is Zoe, who came
//      to fetch Twinsen there after the Tralu was beaten): the race is how he is won over.
//   4. Raph's time is his lap (RaceDriver.CitadelStorm: a driver with no car). A lap under it sets BeatVar (the race-track mode's beat=);
//      Raph says so and goes back to the lighthouse -- the plot's 51 and 56 go to 3, as when he was freed from the Tralu, and the game's
//      own lighthouse scene follows: the wizard waits there, Twinsen joins him, the spell, the storm is over (chapter 2).
//   5. The game then has the aliens land by the tavern (scene 42): Twinsen arrives there in a cutscene that waits for one of them to
//      speak (game variable 70), but the island's actors who played it were taken off the road by the track's build. In their place one
//      alien (a copy of the game's own) stands near where the cutscene puts Twinsen, off the road, and thanks him -- as a thank you
//      they'll build an even better race track, ready tomorrow -- which ends the cutscene. DayVar goes to Tired.
//   6. Tired, everyone Twinsen talks to on the island tells him to go home and have a nap (the race-track mode's tired=). Action at his
//      bed (a zone of its own by it, scene 0) and he sleeps: DayVar Rested, and an arrow to the town circuit's start line (223), which the
//      gate kept shut until now.
//   7. The town circuit races Raph, Zoe, Mr. Paul, the Tralu and the thief (RaceDriver.CitadelTown), three laps. A win (WonVar) is Mr.
//      Paul's prize, a ferry ticket (inventory slot 13), handed over once Twinsen is out of his car.
// The texts are new ones at the end of Citadel's text file, in every language the game has (TEXT.HQR, code page 850 like the game's own).
// At the end, so they have no recorded voice (MESSAGE.CPP Speak: a text past the last sample is shown, not spoken) and every text before
// them keeps its own.
//   - The arrows are holomap positions 222 and 223 (HOLOMAP.HQR entry 12, record 50 + n): positions are scene numbers -- a scene's record
//     is where it lies on the island, used for its arrow and for "Twinsen is here" -- and the game has 222 scenes, so 222-254 are no
//     scene's and no script uses them. Their labels are new texts in the holomap's text file (file 2).
//   - Racing gloves, in the darts' place in Twinsen's attic (scene 1: the darts lie on a shelf, actor 8, and pressing Action there -- zone 5
//     -- gives them). The inventory is 40 fixed slots, all the game's (a save stores exactly 40, and item n's count is game variable n, 40
//     being the Dino-Fly quest), so the gloves take a slot the mod has no use for: 7, the part Baldino gives for Zoe to mend the car -- the
//     mod has the buggy ready from the start. Not the darts' own slot: the darts are sold in the shop and found elsewhere too, and stay.
//     The slot's model (OBJFIX.HQR 7) and its texts (file 2: 7 the found message, 107 the name, 207 the description) become the gloves'; the
//     attic's shelf shows the gloves (a body of its own on the darts' entity) and gives them. Those three texts are spoken by the narrator
//     (EN_GAM.VOX), so each keeps its place in the file under an id nothing asks for -- every text after it keeps its voice -- and the gloves'
//     text goes at the end, where it has none.
// The new story (2026-11-03, docs/LBRA_WALKTHROUGH.md chapters 1 and 2: RaceTrackStory.Chapters.cs) changes this: the gloves are stolen
// (the thief has them, the attic's shelf shows the darts again), the spell at the lighthouse no longer moves the game on, Twinsen sleeps
// the night in his bed and Zoe wakes him to the morning of chapter 2 (the fine weather, CITABAU), when the Esmers land and the Cup opens.
internal static partial class RaceTrackStory
{
    // the holomap: the storm track's start line, the town circuit's, and the game's own arrow to the Weather Wizard's tent
    public const int StartArrow = 222, TownArrow = 223, WizardArrow = 21;
    // the game variables: the gloves (an inventory slot), the ferry ticket's, the story's day (Tired after the aliens' thanks, Rested once
    // Twinsen has slept: the town circuit is open), the town circuit won (1; 2 once the ticket is handed over) and Raph's time beaten
    // (1; 2 once Raph has said so). 200-202 are game variables nothing in the game uses.
    public const int GlovesSlot = 7, FerryTicket = 13;
    // the harbour (scene 43): the ferry, and Twinsen's own script that takes him aboard with the ticket; Citadel Island's weather (1 fine:
    // RACEMOD.CPP RaceMod_CitadelWeather)
    private const int FerryScene = 43, WeatherVar = 206;
    public const int DayVar = 200, Tired = 1, Rested = 2, WonVar = 201, BeatVar = 202;
    // Celebration Island's lava lake: the souvenir seller beaten (1; 2 once he has told what he saw) and his race run (won or not), 203-204
    public const int SellerBeaten = 203, SellerRaced = 204;
    // (205 is Polar Island's dream, 206 Citadel Island's weather: RACEMOD.CPP RaceMod_CitadelWeather.) Twinsen has driven a car on Citadel
    // Island -- he has his car (207, set by every outside scene's controller) -- and Raph has stopped lapping to talk to him (208: his car
    // stands parked by the start line, RACEMOD.CPP parade=)
    public const int HasCar = 207, RaphHere = 208;
    // the race-track mode's: 1 while a gate of the track is shut (RACEMOD.CPP RACE_GATE_VAR) -- the story's race, not one started on its line
    public const int GateShut = 209;
    // the game's own people at the lighthouse (scene 46), kept by the build (RaceTrackIsland.StoryEntities): Raph by its door and Twinsen
    // and Zoe on its top in the spell's last shot (BODY 175) -- his fiancee (Fiancee) no longer, since 2026-10-14: she stood and ran on the
    // town circuit; and the behaviour of Twinsen and Zoe walking together (entity 5)
    public const int LighthouseRaph = 19, Fiancee = 113, CoupleOnTop = 116;
    private const int LighthouseScene = 46, TwinsenAndZoe = 5;
    public const int RaceLaps = 3;
    // the game's storm plot (51: 1 the wizard spoken to, 2 Raph spoken to, 3 Raph freed, 4 the storm over), the lighthouse keeper's
    // (56: 3 back at the lighthouse), the aliens' landing (70) and the behaviour Twinsen drives in (12)
    private const int StormPlot = 51, KeeperPlot = 56, AliensLanded = 70, Driving = 12;
    private const int OpeningScene = 0, Zoe = 4, BedZone = 5;
    private const int WizardTent = 21, Wizard = 2, TraluCave = 2, CaveRaph = 2, CaveZoe = 9, PaulHouse = 7, PaulActor = 2;
    // (the game's landing speech, 384, and greeting, 388: their words are IslandRetell's, the Cup's)
    private const int AliensScene = 42, Alien = 6, AlienGreeting = 388, EsmerGreeting = 384, ArrivalPoint = 20;
    // the gloves: the attic, its shelf actor, and the darts' display entity and its body
    internal const int Attic = 1, Shelf = 8, DartsEntity = 18, DartsBody = 31, CarScene = 49;
    private const int CitadelTexts = 3, HolomapTexts = 2;
    private const byte LabelAttribute = 17;          // the holomap labels' own (text 501 "Downtown Pharmacy.")
    private const int ArrowRecordSize = 32;

    // The story's lines (Citadel Island's texts), in English, French, German, Spanish, Italian, Portuguese: TEXT.HQR's own order. The game's
    // own names: the Weather Wizard is the Mage Météo, the Wettermagier, the Mago Meteo, the Mago Metereologo, the Mago do Tempo.
    // (EsmerTrack was the aliens' thanks with the new track ready tomorrow: since 2026-11-03 the middle of their landing speech, the morning
    // after -- RaceTrackStory.Chapters.cs)
    private enum Line { Zoe, Wizard, WizardAfter, Raph, RaphBeaten, PaulGloves, PaulReady, EsmerTrack, Tired, Sleep, PaulTicket, RaphTime, PaulStop, ZoeJoins, RaphYell }
    private static readonly string[][] Lines =
    {
        new[]
        {
            "Twinsen, I am sick of all this rain! Go and find the Weather Wizard and get him to fix it!",
            "Twinsen, j'en ai assez de toute cette pluie ! Va trouver le Mage Météo et demande-lui d'y remédier !",
            "Twinsen, ich habe diesen ganzen Regen satt! Geh zum Wettermagier und sorg dafür, dass er etwas dagegen tut!",
            "¡Twinsen, estoy harta de tanta lluvia! ¡Ve a buscar al Mago Meteo y haz que lo arregle!",
            "Twinsen, sono stufa di tutta questa pioggia! Va' a cercare il Mago Metereologo e fa' che la faccia smettere!",
            "Twinsen, estou farta de toda esta chuva! Vai procurar o Mago do Tempo e faz com que ele resolva isto!",
        },
        new[]
        {
            "I was going to clear the rain, but I need to do it from the top of the lighthouse, and that rascal Raph is too busy at the race track and refuses to let me.",
            "J'allais chasser la pluie, mais je dois le faire du haut du phare, et ce coquin de Raph est trop occupé sur le circuit et refuse de me laisser monter.",
            "Ich wollte den Regen vertreiben, aber das muss ich von der Spitze des Leuchtturms aus tun, und dieser Schlingel Raph ist auf der Rennstrecke zu beschäftigt und lässt mich nicht hinauf.",
            "Iba a despejar la lluvia, pero tengo que hacerlo desde lo alto del faro, y ese granuja de Raph está muy ocupado en el circuito y no me deja subir.",
            "Stavo per far smettere la pioggia, ma devo farlo dalla cima del faro, e quel furfante di Raph è troppo occupato alla pista e non mi lascia salire.",
            "Eu ia acabar com a chuva, mas tenho de o fazer do alto do farol, e aquele maroto do Raph está muito ocupado na pista e não me deixa subir.",
        },
        new[]
        {
            "Raph is back at the lighthouse? Then let's go, Twinsen! I'll meet you there.",
            "Raph est retourné au phare ? Alors allons-y, Twinsen ! Je te retrouve là-bas.",
            "Raph ist wieder am Leuchtturm? Dann los, Twinsen! Wir treffen uns dort.",
            "¿Raph ha vuelto al faro? ¡Entonces vamos, Twinsen! Nos vemos allí.",
            "Raph è tornato al faro? Allora andiamo, Twinsen! Ci vediamo là.",
            "O Raph voltou ao farol? Então vamos, Twinsen! Encontramo-nos lá.",
        },
        new[]
        {
            "I'm having too much fun, it can wait. I'll tell you what: if you can beat my time on the track, then I'll come with you to the lighthouse. Mind you, you'll need a car and some driving gloves to take part.",
            "Je m'amuse trop, ça peut attendre. Je te propose un marché : si tu bats mon temps sur le circuit, je viens avec toi au phare. Mais attention, il te faudra une voiture et des gants de pilote pour participer.",
            "Ich habe gerade viel zu viel Spaß, das kann warten. Ich mache dir einen Vorschlag: Wenn du meine Zeit auf der Strecke schlägst, komme ich mit dir zum Leuchtturm. Aber um mitzumachen, brauchst du ein Auto und Fahrhandschuhe.",
            "Me lo estoy pasando demasiado bien, eso puede esperar. Te propongo una cosa: si consigues batir mi tiempo en el circuito, iré contigo al faro. Eso sí, para participar necesitas un coche y unos guantes de conducir.",
            "Mi sto divertendo troppo, può aspettare. Facciamo così: se riesci a battere il mio tempo sulla pista, verrò con te al faro. Però per partecipare ti servono una macchina e dei guanti da guida.",
            "Estou a divertir-me demasiado, isso pode esperar. Faço-te uma proposta: se conseguires bater o meu tempo na pista, vou contigo ao farol. Mas para participares precisas de um carro e de luvas de condução.",
        },
        new[]
        {
            "You beat my time, Twinsen! A deal is a deal: I'm off to the lighthouse. See you there!",
            "Tu as battu mon temps, Twinsen ! Marché conclu : je file au phare. On se retrouve là-bas !",
            "Du hast meine Zeit geschlagen, Twinsen! Abgemacht ist abgemacht: Ich gehe zum Leuchtturm. Wir sehen uns dort!",
            "¡Has batido mi tiempo, Twinsen! Lo prometido es deuda: me voy al faro. ¡Nos vemos allí!",
            "Hai battuto il mio tempo, Twinsen! Un patto è un patto: vado al faro. Ci vediamo là!",
            "Bateste o meu tempo, Twinsen! O prometido é devido: vou para o farol. Vemo-nos lá!",
        },
        new[]
        {
            "Sorry Twinsen, for safety reasons you'll need some driving gloves to take part.",
            "Désolé Twinsen, pour des raisons de sécurité il te faut des gants de pilote pour participer.",
            "Tut mir leid, Twinsen, aus Sicherheitsgründen brauchst du Fahrhandschuhe, um mitzumachen.",
            "Lo siento, Twinsen, por razones de seguridad necesitas unos guantes de conducir para participar.",
            "Mi dispiace, Twinsen, per motivi di sicurezza ti servono dei guanti da guida per partecipare.",
            "Desculpa, Twinsen, por razões de segurança precisas de luvas de condução para participar.",
        },
        new[]
        {
            "Driving gloves, very good. The track is yours, Twinsen: see if you can beat Raph's time!",
            "Des gants de pilote, très bien. Le circuit est à toi, Twinsen : essaie de battre le temps de Raph !",
            "Fahrhandschuhe, sehr gut. Die Strecke gehört dir, Twinsen: Versuch, Raphs Zeit zu schlagen!",
            "Guantes de conducir, muy bien. El circuito es tuyo, Twinsen: ¡a ver si bates el tiempo de Raph!",
            "Guanti da guida, benissimo. La pista è tua, Twinsen: vedi se riesci a battere il tempo di Raph!",
            "Luvas de condução, muito bem. A pista é tua, Twinsen: vê se consegues bater o tempo do Raph!",
        },
        new[]
        {
            "We saw your little race track in the rain. Brave, but small, and it cost you your Tavern. So last night we built you a bigger one, round your whole town, and we put your Tavern back where it stood, every stone of it.",
            "Nous avons vu votre petit circuit sous la pluie. Courageux, mais petit, et il vous a coûté votre Taverne. Alors cette nuit, nous vous en avons construit un plus grand, tout autour de votre ville, et nous avons remis votre Taverne à sa place, pierre par pierre.",
            "Wir haben Eure kleine Rennstrecke im Regen gesehen. Mutig, aber klein, und sie hat Euch Eure Taverne gekostet. Also haben wir Euch heute Nacht eine größere gebaut, rund um Eure ganze Stadt, und Eure Taverne wieder dorthin gestellt, wo sie stand, Stein für Stein.",
            "Vimos vuestro pequeño circuito bajo la lluvia. Valiente, pero pequeño, y os costó vuestra Taberna. Así que anoche os construimos uno más grande, alrededor de toda vuestra ciudad, y volvimos a poner vuestra Taberna donde estaba, piedra a piedra.",
            "Abbiamo visto la vostra piccola pista sotto la pioggia. Coraggiosa, ma piccola, e vi è costata la vostra Taverna. Così stanotte ve ne abbiamo costruita una più grande, intorno a tutta la vostra città, e abbiamo rimesso la vostra Taverna dov'era, pietra su pietra.",
            "Vimos a vossa pequena pista à chuva. Corajosa, mas pequena, e custou-vos a vossa Taberna. Por isso, esta noite construímos-vos uma maior, à volta de toda a vossa cidade, e pusemos a vossa Taberna onde estava, pedra a pedra.",
        },
        new[]
        {
            "You look tired, Twinsen. You should go home and have a nap.",
            "Tu as l'air fatigué, Twinsen. Tu devrais rentrer chez toi faire une sieste.",
            "Du siehst müde aus, Twinsen. Du solltest nach Hause gehen und ein Nickerchen machen.",
            "Pareces cansado, Twinsen. Deberías irte a casa y echarte una siesta.",
            "Sembri stanco, Twinsen. Dovresti andare a casa e farti un pisolino.",
            "Pareces cansado, Twinsen. Devias ir para casa e dormir uma sesta.",
        },
        new[]
        {
            "What a day! A good night's sleep...",
            "Quelle journée ! Une bonne nuit de sommeil...",
            "Was für ein Tag! Eine Mütze voll Schlaf...",
            "¡Qué día! Una buena noche de sueño...",
            "Che giornata! Una bella dormita...",
            "Que dia! Uma boa noite de sono...",
        },
        new[]
        {
            "Well raced, Twinsen! You've won the prize: a ferry ticket. And the Cup's purse: fifty kashes, from the Esmers.",
            "Belle course, Twinsen ! Tu as gagné le prix : un ticket de ferry. Et la bourse de la Coupe : cinquante kashes, de la part des Esmers.",
            "Gut gefahren, Twinsen! Du hast den Preis gewonnen: eine Fahrkarte für die Fähre. Und das Preisgeld des Pokals: fünfzig Kashes, von den Esmern.",
            "¡Buena carrera, Twinsen! Has ganado el premio: un ticket de ferry. Y la bolsa de la Copa: cincuenta kashes, de parte de los Esmers.",
            "Bella corsa, Twinsen! Hai vinto il premio: un biglietto per il traghetto. E il montepremi della Coppa: cinquanta kashes, da parte degli Esmer.",
            "Boa corrida, Twinsen! Ganhaste o prémio: um bilhete de barca. E a bolsa da Taça: cinquenta kashes, da parte dos Esmers.",
        },
        // (the time is the race-track mode's: its "##" becomes Raph's lap at the car setup's skill, RACEMOD.CPP beat_text=)
        new[]
        {
            "A car and driving gloves: you're all set, Twinsen! My best lap is ## seconds. Beat that, and I'll come with you to the lighthouse.",
            "Une voiture et des gants de pilote : tu es prêt, Twinsen ! Mon meilleur tour est de ## secondes. Bats-le, et je viens avec toi au phare.",
            "Ein Auto und Fahrhandschuhe: Du bist bereit, Twinsen! Meine beste Runde dauert ## Sekunden. Schlag sie, und ich komme mit dir zum Leuchtturm.",
            "Un coche y guantes de conducir: ¡estás listo, Twinsen! Mi mejor vuelta es de ## segundos. Supérala y te acompañaré al faro.",
            "Una macchina e i guanti da guida: sei pronto, Twinsen! Il mio giro migliore è di ## secondi. Battilo, e verrò con te al faro.",
            "Um carro e luvas de condução: estás pronto, Twinsen! A minha melhor volta é de ## segundos. Bate-a, e vou contigo ao farol.",
        },
        new[]
        {
            "Stop right there, Twinsen! Nobody drives on this track without driving gloves. It's for your own safety.",
            "Halte-là, Twinsen ! Personne ne roule sur ce circuit sans gants de pilote. C'est pour ta sécurité.",
            "Halt, Twinsen! Ohne Fahrhandschuhe fährt hier niemand auf der Strecke. Das ist zu deiner eigenen Sicherheit.",
            "¡Alto ahí, Twinsen! Nadie conduce por este circuito sin guantes de conducir. Es por tu propia seguridad.",
            "Fermo lì, Twinsen! Nessuno guida su questa pista senza guanti da guida. È per la tua sicurezza.",
            "Alto aí, Twinsen! Ninguém conduz nesta pista sem luvas de condução. É para tua segurança.",
        },
        new[]
        {
            "Twinsen, you did it! Raph's on his way to the lighthouse, and the Weather Wizard is meeting us there. Come on, let's go together!",
            "Twinsen, tu as réussi ! Raph part au phare, et le Mage Météo nous y attend. Allez, allons-y ensemble !",
            "Twinsen, du hast es geschafft! Raph ist auf dem Weg zum Leuchtturm, und der Wettermagier wartet dort auf uns. Komm, gehen wir zusammen!",
            "¡Twinsen, lo has conseguido! Raph va hacia el faro, y el Mago Meteo nos espera allí. ¡Vamos, vayamos juntos!",
            "Twinsen, ce l'hai fatta! Raph sta andando al faro, e il Mago Metereologo ci aspetta là. Dai, andiamo insieme!",
            "Twinsen, conseguiste! O Raph vai a caminho do farol, e o Mago do Tempo espera-nos lá. Anda, vamos juntos!",
        },
        new[]
        {
            "Hey! Get off the track, Twinsen! Are you trying to get yourself run over?!",
            "Hé ! Sors de la piste, Twinsen ! Tu veux te faire écraser ?!",
            "He! Runter von der Strecke, Twinsen! Willst du dich überfahren lassen?!",
            "¡Eh! ¡Sal de la pista, Twinsen! ¿Quieres que te atropellen?!",
            "Ehi! Esci dalla pista, Twinsen! Vuoi farti investire?!",
            "Ei! Sai da pista, Twinsen! Queres ser atropelado?!",
        },
    };
    // Raph's yell at the townsman who walks across the storm track while he laps (the race file's parade_walker=, RaphWalkerLine)
    private static readonly string[] WalkerYellLine =
    {
        "Hey, you! Get off the track! Are you trying to get yourself run over?!",
        "Hé, toi ! Sors de la piste ! Tu veux te faire écraser ?!",
        "He, du da! Runter von der Strecke! Willst du dich überfahren lassen?!",
        "¡Eh, tú! ¡Sal de la pista! ¿Quieres que te atropellen?!",
        "Ehi, tu! Esci dalla pista! Vuoi farti investire?!",
        "Ei, tu! Sai da pista! Queres ser atropelado?!",
    };
    // (the townsman who walks across the storm track by its start line: the game's own, scene 42's actor 4, a Citadel islander)
    public const int TownsmanEntity = 101;

    // the holomap labels: the storm track's start line, the town circuit's
    private static readonly string[][] Labels =
    {
        new[] { "Race track start line.", "Ligne de départ du circuit.", "Startlinie der Rennstrecke.", "Línea de salida del circuito.", "Linea di partenza della pista.", "Linha de largada da pista." },
        new[] { "The aliens' new race track.", "Le nouveau circuit des extraterrestres.", "Die neue Rennstrecke der Außerirdischen.", "El nuevo circuito de los extraterrestres.", "La nuova pista degli alieni.", "A nova pista dos extraterrestres." },
    };
    // the gloves' texts: found, name, description
    private static readonly string[][] GlovesTexts =
    {
        new[] { "You have found a pair of driving gloves.", "Driving gloves", "Your driving gloves: a firm grip on the buggy's steering wheel, lap after lap." },
        new[] { "Tu as trouvé une paire de gants de pilote.", "Gants de pilote", "Tes gants de pilote : une bonne prise sur le volant du buggy, tour après tour." },
        new[] { "Du hast ein Paar Fahrhandschuhe gefunden.", "Fahrhandschuhe", "Deine Fahrhandschuhe: fester Griff am Lenkrad des Buggys, Runde für Runde." },
        new[] { "Has encontrado un par de guantes de conducir.", "Guantes de conducir", "Tus guantes de conducir: buen agarre en el volante del buggy, vuelta tras vuelta." },
        new[] { "Hai trovato un paio di guanti da guida.", "Guanti da guida", "I tuoi guanti da guida: una presa salda sul volante del buggy, giro dopo giro." },
        new[] { "Você encontrou um par de luvas de condução.", "Luvas de condução", "Suas luvas de condução: firmeza no volante do buggy, volta após volta." },
    };
    // (where each of those texts' old words stay: ids no script or engine asks for)
    private const int Retired = 60000;

    // A script edit that doesn't find the game's own script as it expects: the edit is left out, and the log says so.
    private sealed class NotTheGames(string message) : Exception(message);

    private static string Once(string text, string what, string with)
    {
        var at = text.IndexOf(what, StringComparison.Ordinal);
        if (at < 0) throw new NotTheGames($"no \"{what}\"");
        return text[..at] + with + text[(at + what.Length)..];
    }

    // The gloves' models, and the car part's one use (letting Zoe mend the car, scene 49) no longer taking them away. Since 2026-11-03 the
    // attic is the game's own again -- its shelf shows the darts and gives them -- for the gloves are stolen: the thief gives them back
    // (RaceTrackStory.Chapters.cs, Thief).
    private static List<string> Gloves(string gameDirectory, SceneStore store)
    {
        var log = new List<string> { RaceTrackGloves.InstallInventory(gameDirectory, GlovesSlot) };
        var changes = new List<SceneChange>();

        var car = store.Load(CarScene);
        var carScripts = SceneScripts.Load(SceneSerializer.Write(car), CarScene);
        var carHero = carScripts.GetText(0, ScriptKind.Life);
        var use = $"1 == use_inventory({GlovesSlot})";
        if (carHero.Contains(use))
        {
            // (use_inventory answers 0 or 1: asking for 2 never comes true, and the script keeps its shape)
            carScripts.SetText(0, ScriptKind.Life, carHero.Replace(use, $"2 == use_inventory({GlovesSlot})"));
            var carBuilt = carScripts.Build();
            if (carBuilt.Ok) changes.Add(new(CarScene, SceneSerializer.Parse(SceneGame.Lba2, carBuilt.Record!), null));
            else log.Add("scene 49: the car part's use could not be switched off: " + string.Join("; ", carBuilt.Errors));
        }
        store.SaveMany(changes, allowErrors: true);
        log.Add($"the driving gloves are item {GlovesSlot}; scene {Attic}'s shelf keeps the game's darts (the thief has the gloves)");
        return log;
    }

    private static Encoding Dos
    {
        get
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(850);
        }
    }

    // A holomap arrow (`position`) on a start line, on Citadel Island, labelled with text `label` of the holomap's file, off until a script
    // switches it on.
    private static byte[] Arrow(byte[] arrows, int position, (double X, double Z, double Y, double DirX, double DirZ) line, int label)
    {
        var at = (50 + position) * ArrowRecordSize;
        if (arrows.Length < at + ArrowRecordSize) throw new InvalidDataException("The holomap's arrow table is shorter than the game's own.");
        var citadel = 50 + 49;          // (scene 49's record: Citadel Island's own place on the globe)
        void Put(int field, int value) => BinaryPrimitives.WriteInt32LittleEndian(arrows.AsSpan(at + field * 4), value);
        Put(0, (int)Math.Round(line.X * 512)); Put(1, (int)Math.Round(line.Y)); Put(2, (int)Math.Round(line.Z * 512));
        for (var f = 3; f < 6; f++) Put(f, BinaryPrimitives.ReadInt32LittleEndian(arrows.AsSpan(citadel * ArrowRecordSize + f * 4)));
        Put(6, label);
        arrows[at + 28] = 0xFF;         // no inventory object
        arrows[at + 29] = 0;            // off, never asked
        arrows[at + 30] = 0;            // planet Twinsun
        arrows[at + 31] = 0;            // Citadel Island
        return arrows;
    }

    // How far a place is outside every road of a track -- the lap and its pit lane -- past its verge (cells; less than 0 on one).
    private static double OffRoads(RaceTrackReport report, double x, double z)
    {
        if (report.Roads.Count == 0) return report.DistanceToRoad(x, z) - 5;
        var off = double.MaxValue;
        foreach (var r in report.Roads)
        {
            var nearest = double.MaxValue;
            for (var i = 0; i < r.X.Length; i++) nearest = Math.Min(nearest, (r.X[i] - x) * (r.X[i] - x) + (r.Z[i] - z) * (r.Z[i] - z));
            off = Math.Min(off, Math.Sqrt(nearest) - r.VergeHalf);
        }
        return off;
    }

    // Where a person stands by a start line: `back` cells before it along the lap, off the roads to one side (the first of `sides` with
    // ground there near the road's height, `clear` cells past every road's verge), facing the line. Null when neither side has room.
    // (cells between Raph's parked car, which he talks from, and anyone else who talks by the storm track's start line: his reach and
    // theirs end to end -- 1500 each, Flick's 1800 -- and a little more)
    private const double RaphApart = 6.5;

    private static (double X, double Z, double Y, int Beta)? Beside(RaceTrackReport report, (double X, double Z, double Y, double DirX, double DirZ) s, double back, double clear, int[] sides)
    {
        foreach (var side in sides)
            for (var d = 3.0; d <= 24; d += 0.5)
            {
                double x = s.X - s.DirX * back - s.DirZ * side * d, z = s.Z - s.DirZ * back + s.DirX * side * d;
                if (OffRoads(report, x, z) < clear) continue;
                if (report.GroundAfter?.Invoke(x, z) is not { } g || g < 200 || Math.Abs(g - s.Y) > 500) continue;
                return (x, z, g, RaceTrackMath.Turn(Math.Atan2(s.X - x, s.Z - z)));
            }
        return null;
    }

    // Where the Esmer of the landing stands in scene 42: off the road (the fine weather's: the town circuit's) beside where the game's
    // landing cutscene puts Twinsen (its track point 20), on its ground.
    private static (double X, double Z, double Y, int Beta) Landing(SceneModel model, RaceTrackReport town)
    {
        if (ArrivalPoint >= model.TrackPoints.Count) throw new NotTheGames($"no track point {ArrivalPoint}");
        var arrival = model.TrackPoints[ArrivalPoint];
        var from = (model.CubeX * 64 + arrival.X / 512.0, model.CubeY * 64 + arrival.Z / 512.0, (double)arrival.Y, 0.0, 1.0);
        return Beside(town, from, -1, 1, new[] { 1, -1 }) ?? throw new NotTheGames("no room by Twinsen's arrival");
    }

    // ---- Celebration Island: the souvenir seller -------------------------------------------------------------------------------------
    // The user's (2026-10-03, later): Twinsen has to beat the souvenir seller to get the information he needs. In the game the seller (scene
    // 95, actor 5: the Franco who came back from Island CX, BODY.HQR 307) tells it when Twinsen walks up to him: what he saw there (171),
    // and to "How can I get there?" Rick's gang, at the bar by Otringal's harbour (172, with its holomap arrow, 136; game variable 124 goes
    // to 1). Now, until Twinsen has beaten him, he answers with a challenge instead and takes him to the lava lake's race (its scene, 223:
    // the lava lake is the island before the statue rises, as it is when Twinsen meets him). There he races as the one to beat, three laps.
    // Out of his car after the race, the seller tells him what he saw if he won, or laughs at him if not, and he is back by the dock (scene
    // 95's start); a race lost, the challenge stands.
    private enum Seller { Challenge, Beaten, Rematch }
    private static readonly string[][] SellerLines =
    {
        new[]
        {
            "What I saw over there? That'll cost you more than a statuette, mister. Race me round the lava lake, and if you beat me I'll tell you everything. Come on, the cars are waiting!",
            "Ce que j'ai vu là-bas ? Ça vous coûtera plus qu'une statuette, monsieur. Faites-moi la course autour du lac de lave, et si vous me battez je vous dirai tout. Venez, les voitures attendent !",
            "Was ich dort gesehen habe? Das kostet dich mehr als eine Statuette, mein Freund. Fahr mit mir ein Rennen um den Lavasee, und wenn du mich schlägst, erzähle ich dir alles. Komm, die Wagen warten!",
            "¿Lo que vi allí? Eso te costará más que una estatuilla, amigo. Échame una carrera alrededor del lago de lava, y si me ganas te lo contaré todo. ¡Vamos, los coches esperan!",
            "Quello che ho visto laggiù? Ti costerà più di una statuetta, amico. Sfidami in una corsa intorno al lago di lava, e se mi batti ti racconterò tutto. Vieni, le auto aspettano!",
            "O que eu vi lá? Isso vai custar-te mais do que uma estatueta, amigo. Corre contra mim à volta do lago de lava, e se me venceres conto-te tudo. Anda, os carros estão à espera!",
        },
        new[]
        {
            "You beat me fair and square, mister! A deal's a deal: here's what I saw on Island CX.",
            "Vous m'avez battu à la régulière, monsieur ! Marché conclu : voici ce que j'ai vu sur l'île CX.",
            "Du hast mich ehrlich geschlagen, mein Freund! Abgemacht ist abgemacht: Das habe ich auf der Insel CX gesehen.",
            "¡Me has ganado limpiamente, amigo! Lo prometido es deuda: esto es lo que vi en la isla CX.",
            "Mi hai battuto lealmente, amico! Un patto è un patto: ecco cosa ho visto sull'isola CX.",
            "Venceste-me com justiça, amigo! O prometido é devido: eis o que vi na ilha CX.",
        },
        new[]
        {
            "Ha! Not fast enough, mister. Come and find me at my stall when you want a rematch.",
            "Ha ! Pas assez rapide, monsieur. Revenez me voir à mon étal si vous voulez une revanche.",
            "Ha! Nicht schnell genug, mein Freund. Komm zu meinem Stand, wenn du eine Revanche willst.",
            "¡Ja! No eres lo bastante rápido, amigo. Ven a buscarme a mi puesto si quieres la revancha.",
            "Ah! Non abbastanza veloce, amico. Vieni a trovarmi alla mia bancarella se vuoi la rivincita.",
            "Ah! Não foste rápido que chegue, amigo. Vem ter comigo à minha banca se quiseres a desforra.",
        },
    };
    // (the seller is the game's actor 5 of scene 95, entity 213; a track's build that took the others off the road renumbered him)
    public const int SellerEntity = 213;
    private const int CelebrationScene = 95, CelebrationTexts = 3 + 5;
    private const int Told = 124, RickArrow = 136;

    // The first id free in a file of TEXT.HQR, in every language (one past the highest any of them has) ...
    private static int NextId(HqrArchive text, int languages, int file) =>
        Enumerable.Range(0, languages).Max(l => Lba2TextBank.Load(text, l, file).Texts.Select(t => t.Id).DefaultIfEmpty(0).Max()) + 1;

    // Raph's yell at the townsman on the storm track (the user, 2026-11-06: "This guy still walks on the track whilst Raph is driving" --
    // their choice: Raph stops and yells at him as he yells at Twinsen, then drives on). Its text goes in last of all Citadel Island's
    // (RaceTrackService.Story: after the island's retelling and its postcards), so no text before it moves; the story's info gets its id
    // and the townsman's entity for the race file's parade_walker= (RACEMOD.CPP, ParadeStep). The info as it was if the townsman isn't
    // in the storm track's start scene.
    public static (List<string> Log, RaceTrackService.StoryInfo? Info) RaphWalkerLine(string gameDirectory, RaceTrackService.StoryInfo? info, RaceTrackScenes.Result scenes)
    {
        var log = new List<string>();
        if (info is null || info.RaphYell < 0) return (log, info);
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (scenes.StartScene < 0 || !store.SceneExists(scenes.StartScene) || store.Load(scenes.StartScene).Actors.All(a => a.Entity != TownsmanEntity))
        {
            log.Add($"Raph's yell at the townsman on the track left out: no townsman (entity {TownsmanEntity}) in the storm track's start scene");
            return (log, info);
        }
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        var id = NextId(text, languages, CitadelTexts);
        var hqr = File.ReadAllBytes(textPath);
        for (var lang = 0; lang < languages; lang++)
        {
            var bank = Lba2TextBank.Load(text, lang, CitadelTexts);
            var words = lang < WalkerYellLine.Length ? lang : 0;
            bank.Texts.Add(new Lba2TextBank.Text { Id = id, Attribute = bank.Find(0)?.Attribute ?? Lba2TextBank.NormalAttribute, Bytes = Dos.GetBytes(WalkerYellLine[words]) });
            hqr = bank.WriteInto(hqr);
        }
        File.WriteAllBytes(textPath, hqr);
        log.Add($"Raph's yell at the townsman (entity {TownsmanEntity}) who walks across the storm track while he laps: text {id} of Citadel Island's");
        return (log, info with { WalkerYell = id, Walkers = new[] { TownsmanEntity } });
    }

    // ... and texts added to a language's bank from it on: each line's words in that language (`words`, its column of `lines`)
    private static void AddTexts(Lba2TextBank bank, int first, byte attribute, string[][] lines, int words)
    {
        for (var k = 0; k < lines.Length; k++)
            bank.Texts.Add(new Lba2TextBank.Text { Id = first + k, Attribute = attribute, Bytes = Dos.GetBytes(lines[k][words]) });
    }

    // A person the story adds speaks in a colour of their own. Dialogue colour 0 is the palette's first row, which the typewriter cycles
    // through as a rainbow while the line types out (MESSAGE.CPP TestCoulDial): the game gives it to people who never speak -- the landing
    // crowd's quetch and grobo copied for the Esmers' landing said their lines in it (the user, 2026-11-06). Such a person is given the
    // colour the game gives the same entity where it does speak (the commonest, in the scenes as they were before any build; Zoe's, 4,
    // which the radio's picture goes by, last), or DefaultVoice. `store`: the build's scenes; without them, DefaultVoice.
    internal const int DefaultVoice = 9, ZoeVoice = 4;
    private static readonly Dictionary<string, Dictionary<int, int>> voices = new(StringComparer.OrdinalIgnoreCase);
    internal static void GiveVoice(SceneActorModel actor, SceneStore? store = null)
    {
        if (actor.CoulObj is > 0 and < 16) return;
        actor.CoulObj = store is not null && Voices(store).TryGetValue(actor.Entity, out var own) ? own : DefaultVoice;
    }
    private static Dictionary<int, int> Voices(SceneStore store)
    {
        if (voices.TryGetValue(store.Directory, out var known)) return known;
        var originals = RaceTrackService.OriginalScenes(store);
        var counts = new Dictionary<int, Dictionary<int, int>>();
        for (var s = 0; s < 256; s++)
        {
            SceneModel m;
            try { if (!originals.SceneExists(s)) continue; m = originals.Load(s); }
            catch (Exception e) when (e is InvalidDataException or ArgumentException or IOException) { continue; }
            foreach (var a in m.Actors.Skip(1).Where(a => a.CoulObj is > 0 and < 16))
            {
                if (!counts.TryGetValue(a.Entity, out var c)) counts[a.Entity] = c = new Dictionary<int, int>();
                c[a.CoulObj] = c.GetValueOrDefault(a.CoulObj) + 1;
            }
        }
        return voices[store.Directory] = counts.ToDictionary(e => e.Key, e => e.Value.OrderByDescending(c => c.Value).ThenBy(c => c.Key == ZoeVoice ? 1 : 0).ThenBy(c => c.Key).First().Key);
    }

    // An actor stood at a place in island cells, in the scene's cube, with its height and facing.
    private static void Place(SceneActorModel actor, SceneModel model, (double X, double Z, double Y, int Beta) at)
    {
        actor.X = RaceTrackScenes.CubeLocal(at.X, model.CubeX); actor.Z = RaceTrackScenes.CubeLocal(at.Z, model.CubeY);
        actor.Y = (int)Math.Round(at.Y); actor.Beta = at.Beta;
    }

    // `scenes`: what the lava lake's scenes were given (its start scene, 223, is its race's). Returns lines for the log.
    // (`report`: the lava lake's track, for chapter 6's statuettes beside it -- RaceTrackStory.LavaLake.cs)
    // (`travels`: the build's doors past scene 255, put in once every track is built -- RaceTrackStory.Travels.cs)
    public static List<string> ApplyCelebration(string gameDirectory, RaceTrackScenes.Result scenes, RaceTrackReport? report = null, List<PendingTravel>? travels = null)
    {
        var log = new List<string>();
        if (scenes.StartScene < 0) { log.Add("no story: the lava lake's track has no start scene"); return log; }
        // the lines, at the end of Celebration Island's texts
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        var first = NextId(text, languages, CelebrationTexts);
        int Id(Seller line) => first + (int)line;
        var hqr = File.ReadAllBytes(textPath);
        for (var lang = 0; lang < languages; lang++)
        {
            var words = lang < SellerLines[0].Length ? lang : 0;
            var lines = Lba2TextBank.Load(text, lang, CelebrationTexts);
            AddTexts(lines, first, lines.Find(170)?.Attribute ?? Lba2TextBank.NormalAttribute, SellerLines, words);
            // (chapter 6's lines after them: RaceTrackStory.LavaLake.cs)
            AddTexts(lines, first + SellerLines.Length, lines.Find(170)?.Attribute ?? Lba2TextBank.NormalAttribute, LavaLines, words);
            hqr = lines.WriteInto(hqr);
        }
        File.WriteAllBytes(textPath, hqr);
        var lava = first + SellerLines.Length;
        log.Add($"the souvenir seller's {SellerLines.Length} lines are texts {first}-{first + SellerLines.Length - 1} of Celebration Island's, chapter 6's {LavaLines.Length} texts {lava}-{lava + LavaLines.Length - 1}, in all {languages} languages");

        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        // (the dive into the lava past the jump: RaceTrackStory.LavaLake.cs)
        log.AddRange(LavaDive(store, report, lava));
        var changes = new List<SceneChange>();
        // the seller: a challenge before he is beaten, what he knows after
        try
        {
            var model = store.Load(CelebrationScene);
            var sellerActor = model.Actors.FindIndex(a => a.Entity == SellerEntity);
            if (sellerActor < 1) throw new NotTheGames($"no souvenir seller (entity {SellerEntity})");
            var scripts = SceneScripts.Load(SceneSerializer.Write(model), CelebrationScene);
            var seller = scripts.GetText(sellerActor, ScriptKind.Life);
            int from = seller.IndexOf($"set_var_game({Told}, 1);", StringComparison.Ordinal), to = seller.IndexOf($"set_holo_pos({RickArrow});", StringComparison.Ordinal);
            if (from < 0 || to < from) throw new NotTheGames("the seller's lines");
            to += $"set_holo_pos({RickArrow});".Length;
            // (chapter 6: his stall and the four statuettes first -- RaceTrackStory.LavaLake.cs)
            seller = seller[..from] + $@"if (0 == var_game({SellerBeaten}))
            {{
                {LavaSeller(lava, Id(Seller.Challenge), scenes.StartScene)}
            }}
            else
            {{
                set_var_game({Told}, 1);
                message(170);
                message_obj(0, 6);
                message(171);
                message_obj(0, 7);
                message(172);
                set_holo_pos({RickArrow});
            }}" + seller[to..];
            scripts.SetText(sellerActor, ScriptKind.Life, seller);
            var built = scripts.Build();
            if (!built.Ok) throw new NotTheGames(string.Join("; ", built.Errors));
            changes.Add(new(CelebrationScene, SceneSerializer.Parse(SceneGame.Lba2, built.Record!), null));
            log.Add($"scene {CelebrationScene}: the souvenir seller challenges Twinsen to the lava lake's race (scene {scenes.StartScene}) until he is beaten (variable {SellerBeaten})");
        }
        catch (Exception e) when (e is NotTheGames or ScriptCompileException or InvalidDataException or ArgumentException)
        {
            log.Add($"scene {CelebrationScene}: the souvenir seller left as he was: {e.Message}");
            return log;
        }
        // the race's scene: once Twinsen is out of his car after the race, what the seller says, and back to the dock
        try
        {
            var model = store.Load(scenes.StartScene);
            var colour = store.Load(CelebrationScene).Actors.FirstOrDefault(a => a.Entity == SellerEntity)?.CoulObj ?? 4;
            var index = SceneOps.AddActor(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, colour));
            var scripts = SceneScripts.Load(SceneSerializer.Write(model), scenes.StartScene);
            scripts.SetText(index, ScriptKind.Life, $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if (1 == var_game({SellerRaced}) && {Driving} != comportement_hero())
    {{
        set_var_game({SellerRaced}, 0);
        if (1 == var_game({SellerBeaten}))
        {{
            message({Id(Seller.Beaten)});
            message(171);
            message_obj(0, 7);
            message(172);
            set_var_game({Told}, 1);
            set_var_game({SellerBeaten}, 2);
            {LavaWon(lava, store)}
        }}
        else
        {{
            message({Id(Seller.Rematch)});
            change_cube({CelebrationScene});
        }}
    }}
}}
");
            scripts.SetText(index, ScriptKind.Track, "label(0);\nstop();\n");
            var built = scripts.Build();
            if (!built.Ok) throw new NotTheGames(string.Join("; ", built.Errors));
            changes.Add(new(scenes.StartScene, SceneSerializer.Parse(SceneGame.Lba2, built.Record!), null));
            log.Add($"scene {scenes.StartScene}: after the race the seller tells Twinsen what he saw if he won (variable {SellerBeaten}), or offers a rematch, and Twinsen is back at scene {CelebrationScene}");
        }
        catch (Exception e) when (e is NotTheGames or ScriptCompileException or InvalidDataException or ArgumentException)
        {
            log.Add($"scene {scenes.StartScene}: the race's end left out: {e.Message}");
        }
        store.SaveMany(changes, allowErrors: true);
        // (chapter 6's arrival, radio and statuettes in scene 95)
        log.AddRange(LavaLakeBeats(gameDirectory, report, lava, travels));
        // (the fifth statuette, an easter egg: RaceTrackStory.Eggs.Celebration.cs)
        log.AddRange(FifthStatuette(gameDirectory, report, lava));
        return log;
    }

    // Returns lines for the log and what the race-track mode needs (the tired line's text). `storm`: the storm track's build (CITADEL),
    // `town`: the town circuit's (CITABAU); `scenes`, `townScenes`: what the scenes were given for each. Their start lines must have been placed.
    public static (List<string> Log, RaceTrackService.StoryInfo? Info) Apply(string gameDirectory, RaceTrackReport storm, RaceTrackReport town,
        RaceTrackScenes.Result scenes, RaceTrackScenes.Result townScenes)
    {
        var log = new List<string>();
        if (storm.StartLine.Count == 0 || town.StartLine.Count == 0) { log.Add("no story: a track has no start line"); return (log, null); }

        // the texts
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        var first = NextId(text, languages, CitadelTexts); var labelFirst = NextId(text, languages, HolomapTexts);
        int Id(Line line) => first + (int)line;
        // (the new story's lines after these: RaceTrackStory.Chapters.cs)
        int Say(ChapterLine line) => first + Lines.Length + (int)line;
        var hqr = File.ReadAllBytes(textPath);
        for (var lang = 0; lang < languages; lang++)
        {
            var words = lang < Lines[0].Length ? lang : 0;       // (a language this doesn't know gets the English)
            var lines = Lba2TextBank.Load(text, lang, CitadelTexts);
            AddTexts(lines, first, lines.Find(0)?.Attribute ?? Lba2TextBank.NormalAttribute, Lines, words);
            AddChapterTexts(text, lang, lines, first + Lines.Length, lines.Find(0)?.Attribute ?? Lba2TextBank.NormalAttribute, words);
            hqr = lines.WriteInto(hqr);
            var labels = Lba2TextBank.Load(text, lang, HolomapTexts);
            AddTexts(labels, labelFirst, LabelAttribute, Labels, words);
            // (file 2 is also the inventory's: the gloves' found message, name and description, each moved out of its voiced place)
            var item = new[] { GlovesSlot, 100 + GlovesSlot, 200 + GlovesSlot };
            for (var k = 0; k < item.Length; k++)
            {
                var own = labels.Find(item[k]);
                var itemAttribute = own?.Attribute ?? Lba2TextBank.NormalAttribute;
                if (own is not null) own.Id = Retired + item[k];
                labels.Texts.Add(new Lba2TextBank.Text { Id = item[k], Attribute = itemAttribute, Bytes = Dos.GetBytes(GlovesTexts[words][k]) });
            }
            hqr = labels.WriteInto(hqr);
        }
        File.WriteAllBytes(textPath, hqr);
        log.Add($"the story's {Lines.Length + ChapterLines.Length} lines are texts {first}-{first + Lines.Length + ChapterLines.Length - 1} of Citadel Island's, the start lines' holomap labels texts {labelFirst}-{labelFirst + 1}, in all {languages} languages (shown, not spoken)");

        // the arrows: the storm track's start line, the town circuit's
        var holoPath = Path.Combine(gameDirectory, RaceTrackHolomap.File);
        var arrows = HqrArchive.Open(holoPath).Read(12);
        arrows = Arrow(arrows, StartArrow, storm.StartLine[0], labelFirst);
        arrows = Arrow(arrows, TownArrow, town.StartLine[0], labelFirst + 1);
        File.WriteAllBytes(holoPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(holoPath), 12, HqrWriter.StoredEntry(arrows)));
        log.Add($"holomap arrows {StartArrow} (the storm track's start line, cell {storm.StartLine[0].X:0.0}, {storm.StartLine[0].Z:0.0}) and {TownArrow} (the town circuit's, {town.StartLine[0].X:0.0}, {town.StartLine[0].Z:0.0})");

        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        log.AddRange(Gloves(gameDirectory, store));
        log.AddRange(Citadel.CitadelLighthouse.Install(gameDirectory));
        // (the people copied are the game's own, from the scenes as they were before any track's build)
        var originals = RaceTrackService.OriginalScenes(store);

        // every scene edited, once each: loaded, then each step's edits of its actors and scripts in turn, saved together at the end
        var models = new Dictionary<int, SceneModel>();
        SceneModel Model(int scene) => models.TryGetValue(scene, out var m) ? m : models[scene] = store.Load(scene);
        // (`edit` changes the scene's actors and zones, then gives what it does to the scripts, which are read with those changes)
        void Edit(int scene, string what, Func<SceneModel, Action<SceneScripts>> edit)
        {
            try
            {
                var model = SceneSerializer.Parse(SceneGame.Lba2, SceneSerializer.Write(Model(scene)));
                var scriptEdit = edit(model);
                var scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
                scriptEdit(scripts);
                var built = scripts.Build();
                if (!built.Ok) { log.Add($"scene {scene}: {what}: the scripts would not compile: {string.Join("; ", built.Errors)}"); return; }
                models[scene] = SceneSerializer.Parse(SceneGame.Lba2, built.Record!);
                log.Add($"scene {scene}: {what}");
            }
            catch (Exception e) when (e is NotTheGames or ScriptCompileException or InvalidDataException or ArgumentException or SceneEditException)
            {
                log.Add($"scene {scene}: {what} left out: {(e is NotTheGames ? "the script isn't the game's own: " : "")}{e.Message}");
            }
        }
        // a person (or anyone) added to a scene, with a life script of its own (set once the scripts are read)
        Action<SceneScripts> Add(SceneModel model, SceneActorModel actor, string life)
        {
            actor.Life = new byte[] { 0 }; actor.Track = new byte[] { 0 };
            GiveVoice(actor, store);
            var index = SceneOps.AddActor(model, actor);
            return scripts =>
            {
                scripts.SetText(index, ScriptKind.Life, life);
                scripts.SetText(index, ScriptKind.Track, "label(0);\nstop();\n");
            };
        }

        // 1. Zoe sends Twinsen to the Weather Wizard and tells him his gloves are gone; and his bed, where he sleeps once the spell is cast
        // -- the night: the screen fades to black and the scene starts again with Twinsen asleep in it, for the morning (Chapters.cs, Morning)
        Edit(OpeningScene, "Zoe sends Twinsen to the Weather Wizard (his tent's arrow) and tells of the stolen gloves; Action at Twinsen's bed sleeps the night once he is tired", model =>
        {
            // the bed: a zone of its own round it (the hero starts the game at its foot), and Action there
            model.Zones.Add(new SceneZoneModel { Type = 2, Num = BedZone, Info = new[] { 0, 0, 0, 0, 0, 0, 0, 1 }, X0 = 7168, Y0 = 2048, Z0 = 3072, X1 = 10751, Y1 = 3327, Z1 = 5631 });
            return scripts =>
        {
            var zoe = scripts.GetText(Zoe, ScriptKind.Life);
            if (!zoe.Contains("message(0);") || !zoe.Contains("set_holo_pos(22);")) throw new NotTheGames("Zoe's opening");
            // (only the opening's arrow: her later reminder -- "found your gloves yet?" -- loses its arrow to the pharmacy in Chapters.cs, RetoldGates)
            var said = zoe.IndexOf("message(0);", StringComparison.Ordinal);
            var arrow = zoe.IndexOf("set_holo_pos(22);", said, StringComparison.Ordinal);
            zoe = zoe[..arrow] + $"set_holo_pos({WizardArrow});" + zoe[(arrow + "set_holo_pos(22);".Length)..];
            // (woken from the dream -- the dream's variable set -- first what she saw of it: caught FunFrock, DREAM_CAUGHT 1, or not; then her
            // opening, and the gloves stolen: Q_THIEF 1)
            scripts.SetText(Zoe, ScriptKind.Life, zoe.Replace("message(0);", $@"if (0 < var_game({LBAAssembler.Terrain.Polar.PolarDream.DreamVar}))
                {{
                    if (1 == var_game({DreamCaught}))
                    {{
                        message({Say(ChapterLine.ZoeSmiling)});
                    }}
                    else
                    {{
                        message({Say(ChapterLine.ZoeShouting)});
                    }}
                }}
                message({Id(Line.Zoe)});
                message({Say(ChapterLine.ZoeGloves)});
                if (0 == var_game({QThief}))
                {{
                    set_var_game({QThief}, {ThiefTold});
                }}"));
            var hero = scripts.GetText(0, ScriptKind.Life);
            hero = Once(hero, "case 4:", $@"case {BedZone}:
                    if ({Tired} == var_game({DayVar}))
                    {{
                        message({Id(Line.Sleep)});
                        set_var_game({DayVar}, {Night});
                        fade_to_pal(3);
                        change_cube({OpeningScene});
                    }}
                    break;
                case 4:");
            scripts.SetText(0, ScriptKind.Life, hero);
        };
        });

        // 2. Raph isn't held in the Tralu's cave (nor does Zoe come to fetch Twinsen there): he is at the race track
        Edit(TraluCave, "Raph and Zoe are no longer in the Tralu's cave (Raph is at the race track)", _ => scripts =>
        {
            scripts.SetText(CaveRaph, ScriptKind.Life, Once(scripts.GetText(CaveRaph, ScriptKind.Life), $"if (1 < var_game({KeeperPlot}))", $"if (0 <= var_game({KeeperPlot}))"));
            scripts.SetText(CaveZoe, ScriptKind.Life, Once(scripts.GetText(CaveZoe, ScriptKind.Life), $"if (2 < var_game({StormPlot}))", $"if (0 <= var_game({StormPlot}))"));
        });

        // 3. the Weather Wizard: Raph won't let him up the lighthouse; the arrow to the start line
        Edit(WizardTent, "the Weather Wizard tells of Raph at the race track (the start line's arrow), and once Raph is back at the lighthouse to meet him there", _ => scripts =>
        {
            var wizard = scripts.GetText(Wizard, ScriptKind.Life);
            wizard = Once(wizard, "message(26);", $@"if (3 > var_game({StormPlot}))
                {{
                    message({Id(Line.Wizard)});
                    set_holo_pos({StartArrow});
                }}
                else
                {{
                    message({Id(Line.WizardAfter)});
                }}");
            // (the game's next line was Twinsen's: he had seen the keeper held in the cave)
            wizard = Once(wizard, "message_obj(0, 306);", $"set_var_game({StormPlot}, 2);");
            scripts.SetText(Wizard, ScriptKind.Life, wizard);
        });

        // 4. Raph and Mr. Paul at the storm track's start line, in the storm
        var raph = originals.Load(TraluCave).Actors.ElementAtOrDefault(CaveRaph)?.Clone();
        var paul = originals.Load(PaulHouse).Actors.ElementAtOrDefault(PaulActor)?.Clone();
        var start = storm.StartLine[0];
        int[]? raphPark = null;
        if (scenes.StartScene >= 0 && raph is not null && paul is not null)
            Edit(scenes.StartScene, "Raph and Mr. Paul stand by the storm track's start line", model =>
            {
                var sides = new[] { 1, -1 };
                if (Beside(storm, start, 1, 1, sides) is not { } p || Beside(storm, start, 3, 1, sides) is not { } r) throw new NotTheGames("no room beside the start line");
                Place(raph, model, r); Place(paul, model, p);
                // (the start line, the lap's and the pit lane's, two cells either way: a scenaric zone of its own -- the scene's next
                // number -- where Mr. Paul stops a car driven without gloves; he and Raph stand at its end, off the roads)
                var startZone = model.Zones.Where(z => z.Type == 2).Select(z => z.Num).DefaultIfEmpty(0).Max() + 1;
                var across = (storm.StartCurbs is { } curbs ? Math.Max(curbs.Left, curbs.Right) : 12) + 1;
                var corners = new[] { (-across, -2.0), (-across, 2.0), (across, -2.0), (across, 2.0) }
                    .Select(c => (X: start.X - start.DirZ * c.Item1 + start.DirX * c.Item2, Z: start.Z + start.DirX * c.Item1 + start.DirZ * c.Item2)).ToList();
                static int Local(double cell, int cube) => Math.Clamp(RaceTrackScenes.CubeLocal(cell, cube), 0, IslandFile.CubeSize - 1);
                model.Zones.Add(new SceneZoneModel
                {
                    Type = 2, Num = startZone, Info = new[] { 0, 0, 0, 0, 0, 0, 0, 1 },
                    X0 = Local(corners.Min(c => c.X), model.CubeX), X1 = Local(corners.Max(c => c.X), model.CubeX),
                    Z0 = Local(corners.Min(c => c.Z), model.CubeY), Z1 = Local(corners.Max(c => c.Z), model.CubeY),
                    Y0 = (int)Math.Round(start.Y) - 500, Y1 = (int)Math.Round(start.Y) + 1500,
                });
                // (his car's place once he has stopped lapping: beside the road by the start line, along it -- and out of reach of the others
                // who talk there (Tim on the Tavern's doorstep, Mr. Paul, Flick at her plinth): Raph talks from his car, unseen, to anyone
                // within 1500 of it, so beside Tim -- where five cells past the line put it, three steps from him -- Action at Tim had Raph
                // say "beat my time ... I'll come with you to the lighthouse" too, and it read as the waiter's (the user, 2026-11-06). The
                // first place, past the line or before it, either side of the road, in the start line's scene, RaphApart or more from each
                // of them: no one within reach of two of them)
                var others = new List<(double X, double Z)> { (p.X, p.Z) };
                if (OffRoad(storm, TavernDoor.X, TavernDoor.Z, 1) is { } timAt) others.Add((timAt.X, timAt.Z));
                if (storm.Marshal is { Length: >= 10 } flag) others.Add((flag[6] / 512.0, flag[7] / 512.0));
                // (and clear of the scene's doors by three cells: east of the line, past it, the car stood in the mouth of the sewers' lift)
                bool InScene((double X, double Z, double Y, int Beta) at)
                {
                    if (at.X < model.CubeX * 64 + 1 || at.X > model.CubeX * 64 + 63 || at.Z < model.CubeY * 64 + 1 || at.Z > model.CubeY * 64 + 63) return false;
                    double lx = (at.X - model.CubeX * 64) * 512, lz = (at.Z - model.CubeY * 64) * 512;
                    return model.Zones.Where(z => z.Type == 0).All(z =>
                        Math.Max(Math.Max(z.X0 - lx, lx - z.X1), 0) is var ox && Math.Max(Math.Max(z.Z0 - lz, lz - z.Z1), 0) is var oz && Math.Sqrt(ox * ox + oz * oz) >= 3 * 512);
                }
                (double X, double Z, double Y, int Beta)? parkAt = null;
                foreach (var back in new[] { -5.0, -7, -9, -11, 5, 7, 9 })
                {
                    foreach (var side in sides)
                        if (Beside(storm, start, back, 1.5, new[] { side }) is { } at && InScene(at)
                            && others.All(o => Math.Sqrt((at.X - o.X) * (at.X - o.X) + (at.Z - o.Z) * (at.Z - o.Z)) >= RaphApart))
                        {
                            parkAt = at;
                            break;
                        }
                    if (parkAt is not null) break;
                }
                parkAt ??= Beside(storm, start, -5, 1.5, sides);
                if (parkAt is { } park) log.Add($"scene {scenes.StartScene}: Raph's car parks at cell ({park.X:0.0}, {park.Z:0.0}), {others.Min(o => Math.Sqrt((park.X - o.X) * (park.X - o.X) + (park.Z - o.Z) * (park.Z - o.Z))):0.0} cells from the nearest who talks there");
                if (parkAt is { } c)
                {
                    raphPark = new[] { scenes.StartScene, RaceTrackScenes.CubeLocal(c.X, model.CubeX), (int)Math.Round(c.Y), RaceTrackScenes.CubeLocal(c.Z, model.CubeY),
                                       RaceTrackMath.Turn(Math.Atan2(start.DirX, start.DirZ)) };
                    // (his car has him at its wheel: he talks from it -- the game's own Raph, out of sight, where it parks)
                    raph.X = raphPark[1]; raph.Y = raphPark[2]; raph.Z = raphPark[3]; raph.Beta = raphPark[4];
                }
                var shown = raphPark is null ? 0 : 1;
                // Raph laps the track in his car (the race-track mode's parade=, RaphHere 0), until Twinsen stands on the start line on foot
                // (its zone; the user, 2026-10-27: "let's have him keep driving laps until Twinsen is standing on the start/finish line when he
                // drives by, then have him pulling up next to Twinsen with a screech of the tyres and yelling at Twinsen to get off the track":
                // parade_stop=, RaphYell) -- then he pulls up beside him, yells, and parks by the start line, and he talks from it (the game's own
                // Raph is the one who speaks, out of sight where the car stands; with no room for the car, he stands by the start line, seen
                // only once he has stopped). With a car and driving gloves (HasCar, the gloves' slot) he says his time; without, what it takes. Raph's time beaten (BeatVar 1, the race-track mode's beat=) and Twinsen out of his car: he is
                // off to the lighthouse, and Zoe joins Twinsen -- the two of them walking together, as when the game's Raph was freed from
                // the Tralu (behaviour 5) -- and they are there, the game's own lighthouse (scene 46: 51 and 56 at 3, the wizard at its door).
                var raphScript = Add(model, raph, $@"void comportement_0()
{{
    if (1 < chapter() || 2 < var_game({KeeperPlot}))
    {{
        suicide();
    }}
    else
    {{
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    if (0 == var_game({RaphHere}))
    {{
        invisible(1);
        if ({Driving} != comportement_hero() && {startZone} == zone_obj(0))
        {{
            set_var_game({RaphHere}, 1);
            invisible({shown});
            set_comportement(comportement_2);
        }}
    }}
    else
    {{
        invisible({shown});
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if (0 == var_game({RaphHere}))
    {{
        set_comportement(comportement_1);
    }}
    if (1 == var_game({BeatVar}) && {Driving} != comportement_hero())
    {{
        message({Id(Line.RaphBeaten)});
        set_var_game({StormPlot}, 3);
        set_var_game({KeeperPlot}, 3);
        set_var_game({BeatVar}, 2);
        comportement_hero({TwinsenAndZoe});
        message_zoe({Id(Line.ZoeJoins)});
        change_cube({LighthouseScene});
    }}
    swif (1 == action())
    {{
        if (1500 > distance(0) && {Driving} != comportement_hero())
        {{
            if (0 < var_game({GlovesSlot}) && 0 < var_game({HasCar}))
            {{
                message({Id(Line.RaphTime)});
            }}
            else
            {{
                message({Id(Line.Raph)});
            }}
            if (2 > var_game({StormPlot}))
            {{
                set_var_game({StormPlot}, 2);
            }}
        }}
    }}
}}
");
                var paulScript = Add(model, paul, $@"void comportement_0()
{{
    if (1 < chapter())
    {{
        suicide();
    }}
    else
    {{
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    if ({Driving} == comportement_hero() && 0 == var_game({GlovesSlot}) && 1 == var_game({GateShut}) && {startZone} == zone_obj(0))
    {{
        set_dir_obj(0, MOVE_MANUAL);
        message({Id(Line.PaulStop)});
    }}
    swif (1 == action())
    {{
        if (1500 > distance(0) && {Driving} != comportement_hero())
        {{
            if (0 == var_game({GlovesSlot}))
            {{
                message({Id(Line.PaulGloves)});
            }}
            else
            {{
                message({Id(Line.PaulReady)});
            }}
        }}
    }}
}}
");
                return scripts => { raphScript(scripts); paulScript(scripts); };
            });
        else log.Add("Raph and Mr. Paul left out: no start scene, or the game's own Raph (scene 2) or Mr. Paul (scene 7) not found");

        // 5. the Esmers' landing, the morning after the spell (chapter 2: the game's landing cutscene, which waits for var 70, never runs -- the
        // morning has set it): one of them, beside where the cutscene put Twinsen, speaks to him as he comes near the first time -- the game's
        // greeting (384, retold: their thanks for the rain cleared) and the new track and the Cup (EsmersSpoke 1) -- and greets him after
        var alien = originals.Load(AliensScene).Actors.ElementAtOrDefault(Alien)?.Clone();
        if (alien is not null)
            Edit(AliensScene, "an Esmer by the town circuit speaks to Twinsen the morning they land: the rain cleared, the new circuit and the Tavern put back, the Cup", model =>
            {
                Place(alien, model, Landing(model, town));
                var alienScript = Add(model, alien, $@"void comportement_0()
{{
    if (2 != chapter())
    {{
        suicide();
    }}
    else
    {{
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    if (0 == var_game({EsmersSpoke}) && 2500 > distance(0) && {Driving} != comportement_hero())
    {{
        set_var_game({EsmersSpoke}, 1);
        message({EsmerGreeting});
        message({Id(Line.EsmerTrack)});
        message({Say(ChapterLine.EsmerCup)});
    }}
    swif (1 == action())
    {{
        if (1500 > distance(0) && {Driving} != comportement_hero())
        {{
            message({AlienGreeting});
        }}
    }}
}}
");
                return alienScript;
            });

        // 6. the town circuit won: Mr. Paul's prize, once Twinsen is out of his car -- in whichever of the island's outside scenes that is: the
        // ferry ticket and the Cup's purse, and the herald's word that Twinsen leads the Cup
        var paulColour = paul?.CoulObj ?? 4;
        for (var scene = RaceTrackIsland.Citadel.FirstScene; scene <= RaceTrackIsland.Citadel.LastScene; scene++)
        {
            if (!store.SceneExists(scene)) continue;
            Edit(scene, "Mr. Paul hands over his prize, a ferry ticket, after a win on the town circuit; Twinsen driving here means he has his car", model =>
            {
                var prize = RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, paulColour);
                return Add(model, prize, $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if ({Driving} == comportement_hero() && 0 == var_game({HasCar}))
    {{
        set_var_game({HasCar}, 1);
    }}
    if (1 == var_game({WonVar}) && {Driving} != comportement_hero())
    {{
        message({Id(Line.PaulTicket)});
        found_object({FerryTicket});
        set_var_game({FerryTicket}, 1);
        add_gold_pieces({Purse});
        message({Say(ChapterLine.HeraldLeads)});
        set_var_game({WonVar}, 2);
    }}
}}
");
            });
        }

        // 7. the ferry: not there in the storm, nor before the town circuit is won (the user, 2026-10-27: "Citadel island has a bug that lets
        // Twinsen take the ferry in the rain by selecting his ferry ticket from his inventory, let's add a fix so this doesn't happen, and
        // keep the ferry invisible until after the Citabau track is beaten") -- the ferry hidden, and the ticket shown to the ferryman or
        // used at the quay doing nothing, until then
        Edit(FerryScene, "the ferry is there, and the ticket takes Twinsen aboard, only in the fine weather once the town circuit is won", _ => scripts =>
        {
            var hero = scripts.GetText(0, ScriptKind.Life);
            var board = System.Text.RegularExpressions.Regex.Match(hero, $@"if \(1 == var_game\({FerryTicket}\) && \(0 == l_track_obj\((\d+)\) \|\| 1 == use_inventory\({FerryTicket}\)\)\)");
            if (!board.Success) throw new NotTheGames("the ferry's boarding");
            var ferry = int.Parse(board.Groups[1].Value);
            var open = $"0 < var_game({WonVar}) && 1 == var_game({WeatherVar})";
            hero = Once(hero, $"if (1 == use_inventory({FerryTicket}))", $"if (1 == use_inventory({FerryTicket}) && {open})");
            hero = Once(hero, board.Value, $"if (1 == var_game({FerryTicket}) && (0 == l_track_obj({ferry}) || 1 == use_inventory({FerryTicket})) && {open})");
            scripts.SetText(0, ScriptKind.Life, hero);
            if (scripts.GetText(ferry, ScriptKind.Life).Contains("comportement_1")) throw new NotTheGames("the ferry's own script");
            scripts.SetText(ferry, ScriptKind.Life, $@"void comportement_0()
{{
    if ({open})
    {{
        invisible(0);
    }}
    else
    {{
        invisible(1);
    }}
}}
");
        });

        // 8. the new story's beats, chapters 1 and 2 (RaceTrackStory.Chapters.cs)
        var parts = new StoryParts(store, originals, storm, town, scenes, townScenes, log, Edit, Add, Say, Id);
        Chapters(parts);
        // 9. the island's easter eggs (RaceTrackStory.Eggs.Citadel.cs)
        CitadelEggs(parts, gameDirectory);

        store.SaveMany(models.Select(m => new SceneChange(m.Key, m.Value, null)).ToList(), allowErrors: true);
        log.Add($"the race-track mode: Mr. Paul's gloves gate the storm track (variable {GlovesSlot}), Raph's time beaten sets {BeatVar}; the town circuit opens once {DayVar} is {Rested}, " +
                $"{RaceLaps} laps, a win sets {WonVar}; while {DayVar} is {Tired} everyone says text {Id(Line.Tired)}");
        log.Add($"Raph laps the storm track in his car until Twinsen comes to the start line on foot (variable {RaphHere}){(raphPark is null ? " -- no room for his car by the start line: it goes out of sight" : "")}; he tells his time (text {Id(Line.RaphTime)}) once Twinsen has a car (variable {HasCar}) and driving gloves; Mr. Paul puts Twinsen out of his car on the start line without them");
        return (log, new RaceTrackService.StoryInfo(Id(Line.Tired), TownArrow, RaphTime: Id(Line.RaphTime), RaphPark: raphPark, RaphYell: Id(Line.RaphYell), RaphEntity: raph?.Entity ?? -1));
    }
}
