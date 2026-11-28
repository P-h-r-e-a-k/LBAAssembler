using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Hoverboard from Baldino, for a hair dryer's rheostat -- LBA1's errand for his Jet-Pack (LBA1 TEXT.HQR file 6, text 47: "my last test
// flight wreaked a bit of havoc on the garden wall ... I need a rheostat like the one you find in a hair dryer"; 48: "So you found a hair
// dryer? Great! ... let me install the rheostat"). The user, 2026-11-27: "Let's move the hoverboard to Baldino, however before Twinsen can
// get the hoverboard from Baldino he needs to find Baldino a hairdryer as Baldino needs a part from it - this is a direct reference to the
// first game ... and add in a sales person in a grey suit the same as the first game to walk around knocking on doors in the village area
// for Twinsen to approach to buy one"; "Citadel and then Desert, however Twinsen shouldn't be able to buy one before visiting Desert
// Island"; "The hoverboard is needed for the racing helmet, so providing Baldino hands over the hoverboard pre-race then everything will be
// fine"; "The hair dryer won't need its own inventory slot, it can use the hoverboard slot, as Twinsen will never own both at the same time".
//   - Citadel Island, chapters 0-2: LBA1's salesman (HairDryerSalesman, his own body and animations) in the village (scene 48), from door
//     to door -- house 22's and Citadel Cycles' -- knocking at each; talked to, LBA1's own words: he can't sell a single hair dryer here,
//     he'll move on to another island. He sells nothing there.
//   - The White Leaf Desert, chapter 3 on: gone from Citadel, at the Desert village's doors (scene 60: houses 35, 36 and 40) -- "You
//     remember me, I sell hair dryers". Until Baldino has asked for the rheostat Twinsen doesn't want one; after, 30 Kashes (LBA1's price),
//     yes or no.
//   - Baldino at the racers' tent (and at his house, after the race): with the sand filters, his request; Twinsen short of 30 Kashes, he
//     lends them (once: no softlock); the hair dryer brought, the rheostat in -- the slot becomes the Hoverboard.
//   - The hair dryer is the Hoverboard's slot's other kind (FLAG_PROTOPACK, 12): game variable 12 at 2 is the dryer (its model DryerModel,
//     an errand's: RaceTrackStory.Errands), 1 the board (inv_kinds=12 12 DryerModel). The engine doesn't let the dryer fly: the board's
//     use, its J key and its behaviour menu are not made while the slot holds another thing than its own (RACEMOD.CPP RaceMod_InvKindHas).
//   - The cycle shop doesn't sell the board any more (RaceTrackStory.CycleShop).
//   - Both salesmen are out of sight and walked through while a race is on (the engine's game variable RaceOnVar, RACEMOD.CPP).
internal static partial class RaceTrackStory
{
    // Q_DRYER: 0 nothing, 1 Baldino has asked for the rheostat, 2 Twinsen has bought a hair dryer, 3 the Hoverboard his
    public const int QDryer = 301, DryerAsked = 1, DryerBought = 2, DryerGiven = 3, BaldinoLent = 302;
    // the engine's: 1 from a qualifying lap's start or the grid's forming to the finish (RACEMOD.CPP RACE_ON_VAR)
    public const int RaceOnVar = 303;
    // the Hoverboard's slot (FLAG_PROTOPACK): 1 the board, 2 the hair dryer; the dryer's model (OBJFIX.HQR, texts +0/+100/+200 in file 2)
    public const int BoardSlot = 12, BoardInSlot = 1, DryerInSlot = 2, DryerModel = 161, DryerPrice = 30;
    // the villages, the doors he knocks at (each the front of a house's door: where the house's own way out puts Twinsen), and the doorways
    // (their zones' middles, which he faces as he knocks)
    private const int CitadelVillage = 48, DesertVillage = 60;
    private static readonly (int X, int Y, int Z, int DoorX, int DoorZ)[] CitadelDoors =
    {
        (10752, 256, 17408, 10496, 17920),   // house 22
        (11776, 256, 14848, 11520, 15216),   // Citadel Cycles (398)
    };
    private static readonly (int X, int Y, int Z, int DoorX, int DoorZ)[] DesertDoors =
    {
        (18944, 1280, 18432, 18688, 17920),  // house 35
        (18944, 1280, 26624, 18176, 26856),  // house 36
        (30720, 1280, 27648, 30032, 28064),  // house 40
    };
    // (his track's labels: the round, and standing still out of the way during a race)
    private const int SalesmanRaceLabel = 50;
    private const int SalesmanReach = 1500, SalesmanColour = 15;

    private enum DryerCitadelLine { CantSell }
    private static readonly string[][] DryerCitadelLines =
    {
        new[]
        {
            "Can't seem to sell a single hair dryer here. One of my colleagues is already out there trying his best. If this keeps up, I'll have to move on to another island.",
            "Impossible de vendre un seul sèche-cheveux ici. Un de mes collègues est déjà en train de prospecter. Si ça continue, je vais aller essayer sur une autre île.",
            "Unmöglich, hier auch nur einen einzigen Fön loszuwerden. Einer meiner Kollegen ist bereits dabei, Kunden zu werben. Wenn das so weitergeht, versuche ich es auf einer anderen Insel.",
            "Imposible vender un solo secador de pelo aquí. Uno de mis colegas ya está visitando la clientela. Si esto sigue así, iré a probar a otra isla.",
            "Qui è impossibile vendere anche un unico asciugacapelli. Uno dei miei colleghi sta già cercando clienti. Se continua così, proverò su un'altra isola.",
            "Não consigo vender um único secador de cabelo aqui. Um dos meus colegas já anda por aí a tentar. Se isto continua assim, vou ter de ir para outra ilha.",
        },
    };

    private enum DryerLine { Remember, NoNeed, Offer, BuyYes, BuyNo, NoMoney, Later, Thanks, BaldinoAsk, BaldinoRemind, BaldinoLend, BaldinoRheostat, BaldinoDone }
    private static readonly string[][] DryerDesertLines =
    {
        new[]
        {
            "Aah, I see that you too have come to the Desert. You remember me, I sell hair dryers. Lovely hair dryers, door to door.",
            "Tiens, finalement vous aussi vous êtes venu dans le Désert. Vous vous souvenez de moi, je vends des sèche-cheveux. De beaux sèche-cheveux, au porte-à-porte.",
            "Sieh an, schließlich sind Sie auch in die Wüste gekommen. Erinnern Sie sich noch? Ich verkaufe Föne. Schöne Föne, von Haus zu Haus.",
            "Mira, finalmente usted también ha venido al Desierto. ¿Se acuerda de mí? Vendo secadores de pelo. Bonitos secadores, de puerta en puerta.",
            "Oh, anche lei qui nel Deserto. Si ricorda di me? Vendo asciugacapelli. Begli asciugacapelli, porta a porta.",
            "Ora, afinal também veio ao Deserto. Lembra-se de mim? Vendo secadores de cabelo. Belos secadores, de porta em porta.",
        },
        new[]
        {
            "Thanks, but I don't need a hair dryer.",
            "Merci, mais je n'ai pas besoin d'un sèche-cheveux.",
            "Danke, aber ich brauche keinen Fön.",
            "Gracias, pero no necesito un secador de pelo.",
            "Grazie, ma non mi serve un asciugacapelli.",
            "Obrigado, mas não preciso de um secador de cabelo.",
        },
        new[]
        {
            "30 Kashes for a hair dryer, how about that? I normally sell them door to door, but customers are sparse, and you seem nice enough.",
            "Un sèche-cheveux pour 30 Kashes, ça vous dit ? Normalement je n'ai le droit de les vendre qu'au porte-à-porte, mais les clients sont rares et vous m'êtes sympathique.",
            "Wie wär's, ein Fön für 30 Kashes? Normalerweise darf ich sie nur von Haus zu Haus verkaufen, aber ich habe wenig Kunden, und Sie sind mir sympathisch.",
            "Un secador de pelo por 30 Kashes, ¿le parece? Normalmente solo puedo venderlos de puerta en puerta, pero los clientes escasean y usted me es simpático.",
            "30 Kash per un asciugacapelli, che ne dice? Di solito posso venderli solo porta a porta, ma i clienti sono rari e lei mi è simpatico.",
            "Um secador de cabelo por 30 Kashes, que tal? Normalmente só os posso vender de porta em porta, mas os clientes são poucos e o senhor parece simpático.",
        },
        new[] { "Yes, I'll take one.", "Oui, j'en prends un.", "Ja, ich nehme einen.", "Sí, me llevo uno.", "Sì, ne prendo uno.", "Sim, fico com um." },
        new[] { "Not now.", "Pas maintenant.", "Jetzt nicht.", "Ahora no.", "Non ora.", "Agora não." },
        new[]
        {
            "Thirty Kashes, no less. Come back when your pockets are fuller.",
            "Trente Kashes, pas un de moins. Revenez quand vos poches seront plus pleines.",
            "Dreißig Kashes, keinen weniger. Kommen Sie wieder, wenn Ihre Taschen voller sind.",
            "Treinta Kashes, ni uno menos. Vuelva cuando tenga los bolsillos más llenos.",
            "Trenta Kash, non uno di meno. Torni quando avrà le tasche più piene.",
            "Trinta Kashes, nem um a menos. Volte quando tiver os bolsos mais cheios.",
        },
        new[]
        {
            "I'll be knocking on doors round here. You know where to find me.",
            "Je fais du porte-à-porte dans le coin. Vous savez où me trouver.",
            "Ich klopfe hier in der Gegend an die Türen. Sie wissen, wo Sie mich finden.",
            "Estaré llamando a las puertas por aquí. Ya sabe dónde encontrarme.",
            "Busserò alle porte qui intorno. Sa dove trovarmi.",
            "Vou andar a bater às portas por aqui. Já sabe onde me encontrar.",
        },
        new[]
        {
            "A satisfied customer at last! Wait till my colleague on Citadel hears about this.",
            "Enfin un client satisfait ! Attendez que mon collègue de la Citadelle apprenne ça.",
            "Endlich ein zufriedener Kunde! Wenn das mein Kollege auf der Zitadelle hört!",
            "¡Por fin un cliente satisfecho! Ya verá cuando se entere mi colega de la Ciudadela.",
            "Finalmente un cliente soddisfatto! Aspetti che lo sappia il mio collega della Cittadella.",
            "Finalmente um cliente satisfeito! Espere até o meu colega da Cidadela saber disto.",
        },
        new[]
        {
            "One more thing, Twinsen. I've been working on a Hoverboard for days now: it glides you along just above the ground. My problem is that my last test ride wreaked a bit of havoc on the garden wall. It is rather difficult to fine tune the motor's power. I need a rheostat like the one you find in a hair dryer. Do you think you can find one of those for me?",
            "Encore une chose, Twinsen. Je travaille sur un Hoverboard depuis des jours : il te fait glisser juste au-dessus du sol. Le problème, c'est que mon dernier essai a un peu abîmé le mur qui donne sur le jardin. C'est délicat à régler, la puissance d'un moteur, il me faudrait un rhéostat : comme ceux que l'on trouve dans les sèche-cheveux. Tu pourrais m'en trouver un ?",
            "Noch etwas, Twinsen. Ich arbeite seit Tagen an einem Hoverboard: Es lässt dich knapp über dem Boden gleiten. Das Problem dabei ist, dass meine letzte Testfahrt die Wand zum Garten hin ein wenig beschädigt hat. Es ist schwierig, die Leistung eines Motors richtig einzustellen, ich bräuchte einen Rheostat, wie man ihn in Fönen findet. Könntest du mir einen besorgen?",
            "Una cosa más, Twinsen. Trabajo en un Hoverboard desde hace varios días: te hace deslizarte justo por encima del suelo. El problema es que mi último ensayo ha dañado un poco el muro que da al jardín. Es delicado ajustar la potencia de un motor; necesitaría un reostato, como los que hay en los secadores de pelo. ¿Podrías encontrarme uno?",
            "Un'altra cosa, Twinsen. Lavoro da giorni su un Hoverboard: ti fa scivolare appena sopra il suolo. L'unico problema è che l'ultima prova ha un po' sciupato il muro che dà sul giardino... È difficile regolare la potenza di un motore, avrei bisogno di un reostato, come quelli che ci sono negli asciugacapelli. Puoi procurarmene uno?",
            "Mais uma coisa, Twinsen. Ando há dias a trabalhar num Hoverboard: faz-te deslizar mesmo rente ao chão. O problema é que o meu último teste estragou um bocado o muro que dá para o jardim. É difícil afinar a potência de um motor; precisava de um reóstato, como os que há nos secadores de cabelo. Achas que me consegues arranjar um?",
        },
        new[]
        {
            "A rheostat, from a hair dryer. There's a salesman knocking on doors in the village by my house: try him.",
            "Un rhéostat, de sèche-cheveux. Il y a un vendeur qui fait du porte-à-porte dans le village près de chez moi : essaie avec lui.",
            "Ein Rheostat, aus einem Fön. Im Dorf bei meinem Haus klopft ein Vertreter an die Türen: Versuch's bei ihm.",
            "Un reostato, de un secador de pelo. Hay un vendedor llamando a las puertas en el pueblo junto a mi casa: prueba con él.",
            "Un reostato, da un asciugacapelli. C'è un venditore che bussa alle porte nel villaggio vicino a casa mia: prova da lui.",
            "Um reóstato, de um secador de cabelo. Há um vendedor a bater às portas na aldeia ao pé da minha casa: experimenta com ele.",
        },
        new[]
        {
            "Short of Kashes? Here, thirty. Pay me back when you're champion.",
            "À court de Kashes ? Tiens, trente. Tu me les rendras quand tu seras champion.",
            "Knapp bei Kasse? Hier, dreißig Kashes. Zahl sie mir zurück, wenn du Champion bist.",
            "¿Te faltan Kashes? Toma, treinta. Me los devuelves cuando seas campeón.",
            "A corto di Kash? Tieni, trenta. Me li restituirai quando sarai campione.",
            "Faltam-te Kashes? Toma, trinta. Pagas-me quando fores campeão.",
        },
        new[]
        {
            "So you found a hair dryer? Great! I'll be with you in a minute, just let me install the rheostat.",
            "Alors tu as trouvé un sèche-cheveux ? C'est magnifique ! J'en ai pour une minute, le temps d'installer le rhéostat.",
            "Na, hast du einen Fön gefunden? Fantastisch! Sekunde, ich installiere kurz den Rheostat.",
            "¿Has encontrado entonces un secador de pelo? ¡Es magnífico! Me va a tomar solo un minuto instalar el reostato.",
            "Hai trovato un asciugacapelli? Fantastico! Un minuto solo, il tempo di installare il reostato.",
            "Então encontraste um secador de cabelo? Ótimo! É só um minuto, deixa-me instalar o reóstato.",
        },
        new[]
        {
            "There. Your Hoverboard, tuned to a whisker: it won't knock down any more garden walls. Mind how you go!",
            "Voilà. Ton Hoverboard, réglé au poil : il ne démolira plus de murs de jardin. Fais attention à toi !",
            "So. Dein Hoverboard, haargenau eingestellt: Es reißt keine Gartenmauern mehr ein. Pass auf dich auf!",
            "Listo. Tu Hoverboard, ajustado al milímetro: ya no derribará más muros de jardín. ¡Ten cuidado!",
            "Ecco. Il tuo Hoverboard, regolato alla perfezione: non abbatterà più muri del giardino. Stai attento!",
            "Pronto. O teu Hoverboard, afinado ao milímetro: não vai deitar abaixo mais muros de jardim. Tem cuidado!",
        },
    };

    // The hair dryer as an errand (RaceTrackStory.Errands): its found message, name and description, LBA1's own where it has them (file 2,
    // texts 19 and 119). (A property: Errands' own table, a static field of another file of this class, takes it as it starts up.)
    private static string[][] DryerTexts => new[]
    {
        new[] { "You've just bought a hair dryer.", "Hair dryer", "A hair dryer from the salesman in the grey suit. Baldino wants the rheostat out of it, for his Hoverboard." },
        new[] { "Tu viens d'acheter un Sèche-Cheveux.", "Sèche-Cheveux", "Un sèche-cheveux du vendeur au costume gris. Baldino veut son rhéostat, pour son Hoverboard." },
        new[] { "Du hast soeben einen Fön gekauft.", "Fön", "Ein Fön vom Vertreter im grauen Anzug. Baldino will den Rheostat daraus, für sein Hoverboard." },
        new[] { "Acabas de comprar un Secador de Pelo.", "Secador de Pelo", "Un secador de pelo del vendedor del traje gris. Baldino quiere su reostato, para su Hoverboard." },
        new[] { "Hai appena acquistato un Asciugacapelli.", "Asciugacapelli", "Un asciugacapelli del venditore in completo grigio. Baldino vuole il suo reostato, per il suo Hoverboard." },
        new[] { "Acabaste de comprar um secador de cabelo.", "Secador de cabelo", "Um secador de cabelo do vendedor de fato cinzento. O Baldino quer o reóstato dele, para o seu Hoverboard." },
    };

    // The race file's line for the board's slot (story builds: RaceCarEngineFile)
    internal static string DryerSlotLine() => $"inv_kinds={BoardSlot} {BoardSlot} {DryerModel}\n";

    // (the entity a build gave the salesman, -1 with no LBA1 -- then the cycle shop's owner sells the dryers, his body)
    private static int salesmanEntity = -2;
    private static int SalesmanEntity(string gameDirectory, List<string> log)
    {
        if (salesmanEntity != -2) return salesmanEntity;
        var (entity, line) = HairDryerSalesman.Install(gameDirectory);
        log.Add(line);
        return salesmanEntity = entity;
    }

    // Baldino's words for the rheostat: (`handover`) the dryer brought, the board given; (`ask`) his request; (`remind`) asked already --
    // the Kashes lent if Twinsen hasn't 30, once
    internal sealed record DryerTalk(string Handover, string Ask, string Remind);
    private static DryerTalk BaldinoDryer(int first)
    {
        int Say(DryerLine l) => first + (int)l;
        return new(
            $@"                message({Say(DryerLine.BaldinoRheostat)});
                set_var_game({BoardSlot}, {BoardInSlot});
                found_object({BoardSlot});
                message({Say(DryerLine.BaldinoDone)});
                set_var_game({QDryer}, {DryerGiven});",
            $@"                message({Say(DryerLine.BaldinoAsk)});
                set_var_game({QDryer}, {DryerAsked});",
            $@"                if ({DryerPrice} > nb_gold_pieces() && 0 == var_game({BaldinoLent}))
                {{
                    message({Say(DryerLine.BaldinoLend)});
                    add_gold_pieces({DryerPrice});
                    set_var_game({BaldinoLent}, 1);
                }}
                else
                {{
                    message({Say(DryerLine.BaldinoRemind)});
                }}");
    }

    // His walk, door to door: to each door's front, turned to it, his knock, a wait, on to the next, round again -- and (SalesmanRaceLabel)
    // standing still out of the way while a race is on
    private static string SalesmanTrack(int firstPoint, (int X, int Y, int Z, int DoorX, int DoorZ)[] doors)
    {
        var round = string.Concat(doors.Select((d, k) =>
        {
            var beta = (int)Math.Round(Math.Atan2(d.DoorX - d.X, d.DoorZ - d.Z) * 2048 / Math.PI) & 4095;
            return $@"anim({HairDryerSalesman.Walk});
goto_point({firstPoint + k});
beta({beta});
anim({HairDryerSalesman.Knock});
wait_nb_dizieme(15);
anim({HairDryerSalesman.Stand});
wait_nb_dizieme(40);
";
        }));
        return $@"label(0);
{round}goto(label_0);

label({SalesmanRaceLabel});
anim({HairDryerSalesman.Stand});
stop();
";
    }

    // His life: there while `when` holds as the scene starts; out of sight and walked through during a race, back on his round after;
    // Action by him, `talk`
    private static string SalesmanLife(string when, string talk) => $@"void comportement_0()
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
    if (1 == var_game({RaceOnVar}))
    {{
        if ({SalesmanRaceLabel} != l_track())
        {{
            invisible(1);
            obj_col(0);
            set_track(label_{SalesmanRaceLabel});
        }}
    }}
    else if ({SalesmanRaceLabel} == l_track())
    {{
        invisible(0);
        obj_col(1);
        set_track(label_0);
    }}
    swif (1 == action())
    {{
        if ({SalesmanReach} > distance(0) && {Driving} != comportement_hero())
        {{
{talk}
        }}
    }}
}}
";

    // The salesman: LBA1's (or, without LBA1, the cycle shop's owner) at the first door, on his track
    private static SceneActorModel? Salesman(SceneStore originals, string gameDirectory, List<string> log, (int X, int Y, int Z, int DoorX, int DoorZ) at)
    {
        var like = Copy(originals, BaggageHall, Citadel.CitadelCycleShop.ClerkActor);
        if (like is null) { log.Add($"the hair dryer salesman left out: no actor {Citadel.CitadelCycleShop.ClerkActor} in scene {BaggageHall} to make him like"); return null; }
        var entity = SalesmanEntity(gameDirectory, log);
        if (entity >= 0) { like.Entity = entity; like.Body = 0; like.Anim = HairDryerSalesman.Stand; }
        like.X = at.X; like.Y = at.Y; like.Z = at.Z;
        like.Move = 3;          // (MOVE_TRACK: his track walks him)
        like.HitForce = 0;      // (he never hurts anyone: the user, 2026-11-27, "This isn't a world where people hate Twinsen")
        like.CoulObj = SalesmanColour;
        return like;
    }

    // Citadel Island: the salesman in the village in chapters 0-2, selling nothing
    private static void HairDryerCitadel(StoryParts p)
    {
        if (!p.Store.SceneExists(CitadelVillage)) { p.Log.Add($"the hair dryer salesman on Citadel left out: no scene {CitadelVillage}"); return; }
        var first = GroupB.AddLines(p.Store.Directory, CitadelTexts, DryerCitadelLines, p.Log, "the hair dryer salesman's (Citadel)");
        p.Edit(CitadelVillage, $"the hair dryer salesman (LBA1's) knocking at the village's doors -- house 22's, Citadel Cycles' -- in chapters 0-2, selling nothing (text {first})", model =>
        {
            if (Salesman(p.Originals, p.Store.Directory, p.Log, CitadelDoors[0]) is not { } man) return _ => { };
            var firstPoint = model.TrackPoints.Count;
            foreach (var d in CitadelDoors) model.TrackPoints.Add(new SceneTrackPoint(d.X, d.Y, d.Z));
            var life = p.Add(model, man, SalesmanLife("3 > chapter()", $"            message({first + (int)DryerCitadelLine.CantSell});"));
            var index = model.Actors.Count - 1;
            return scripts => { life(scripts); scripts.SetText(index, ScriptKind.Track, SalesmanTrack(firstPoint, CitadelDoors)); };
        });
    }

    // The White Leaf Desert: the salesman in the village from chapter 3, selling once Baldino has asked; `first`: the lines' first text
    private static void HairDryerDesert(IslandBeatsA b, int first)
    {
        if (!b.Store.SceneExists(DesertVillage)) { b.Log.Add($"the hair dryer salesman on the Desert left out: no scene {DesertVillage}"); return; }
        int Say(DryerLine l) => first + (int)l;
        int yes = Say(DryerLine.BuyYes), no = Say(DryerLine.BuyNo);
        b.Edit(DesertVillage, $"the hair dryer salesman (LBA1's) knocking at the village's doors -- houses 35, 36 and 40 -- from chapter 3: a hair dryer for {DryerPrice} Kashes once Baldino wants the rheostat (texts {first}-{first + DryerDesertLines.Length - 1})", model =>
        {
            if (Salesman(b.Originals, b.GameDirectory, b.Log, DesertDoors[0]) is not { } man) return _ => { };
            var firstPoint = model.TrackPoints.Count;
            foreach (var d in DesertDoors) model.TrackPoints.Add(new SceneTrackPoint(d.X, d.Y, d.Z));
            var life = IslandBeatsA.Add(model, man, SalesmanLife("3 <= chapter()", $@"            if ({DryerBought} <= var_game({QDryer}))
            {{
                message({Say(DryerLine.Thanks)});
            }}
            else if ({DryerAsked} == var_game({QDryer}))
            {{
                add_choice({yes});
                add_choice({no});
                ask_choice({Say(DryerLine.Offer)});
                if ({yes} == choice())
                {{
                    if ({DryerPrice} <= nb_gold_pieces())
                    {{
                        give_gold_pieces({DryerPrice});
                        set_var_game({BoardSlot}, {DryerInSlot});
                        found_object({BoardSlot});
                        set_var_game({QDryer}, {DryerBought});
                    }}
                    else
                    {{
                        message({Say(DryerLine.NoMoney)});
                    }}
                }}
                else
                {{
                    message({Say(DryerLine.Later)});
                }}
            }}
            else
            {{
                message({Say(DryerLine.Remember)});
                message_obj(0, {Say(DryerLine.NoNeed)});
            }}"));
            var index = model.Actors.Count - 1;
            return scripts => { life(scripts); scripts.SetText(index, ScriptKind.Track, SalesmanTrack(firstPoint, DesertDoors)); };
        });
    }
}
