namespace LBAAssembler.Terrain;

// The hidden bowling alley's people, each with a word for Twinsen (the user, 2026-11-16: "We also need to give lines for the NPCs in the
// bowling alley. Every character Twinsen can speak to should have a response line"): each one BowlingAlley lists (Talkers, by who they
// are) answers Action with a line of their own -- the alley's underbelly, seedy and a little sad; and a change machine by the north wall,
// as Stella Entities' (the user, same day: "Let's add another currency conversion machine like the one in the strip joint"): Action --
// change the kashes for zlitos? -- the race file's exchange= (ExchangeVar: two kashes a zlito). Lines in Otringal's bank (the alley is
// Otringal's), after the island's own.
internal static partial class RaceTrackStory
{
    private static readonly (string Who, string[] Line)[] BowlingSays =
    {
        ("BarGloom", new[]
        {
            "The casino upstairs threw me out for counting cards. Down here nobody can count.",
            "Le casino d'en haut m'a jeté pour avoir compté les cartes. Ici, personne ne sait compter.",
            "Das Casino oben hat mich rausgeworfen, weil ich Karten gezählt habe. Hier unten kann keiner zählen.",
            "El casino de arriba me echó por contar cartas. Aquí abajo nadie sabe contar.",
            "Il casinò di sopra mi ha cacciato perché contavo le carte. Quaggiù nessuno sa contare.",
            "O casino lá de cima expulsou-me por contar cartas. Cá em baixo ninguém sabe contar.",
        }),
        ("BarGuard", new[]
        {
            "I'm off duty. If you see my sergeant, I was never here.",
            "Je ne suis pas de service. Si tu vois mon sergent, je n'ai jamais été là.",
            "Ich habe dienstfrei. Wenn du meinen Feldwebel siehst: Ich war nie hier.",
            "Estoy fuera de servicio. Si ves a mi sargento, yo nunca he estado aquí.",
            "Sono fuori servizio. Se vedi il mio sergente, io qui non ci sono mai stato.",
            "Estou de folga. Se vires o meu sargento, eu nunca estive aqui.",
        }),
        ("BarDissident", new[]
        {
            "The Emperor's spies never come down here. Too dark, too sticky, too honest.",
            "Les espions de l'Empereur ne descendent jamais ici. Trop sombre, trop collant, trop honnête.",
            "Die Spione des Kaisers kommen nie hier runter. Zu dunkel, zu klebrig, zu ehrlich.",
            "Los espías del Emperador nunca bajan aquí. Demasiado oscuro, demasiado pegajoso, demasiado honrado.",
            "Le spie dell'Imperatore non scendono mai quaggiù. Troppo buio, troppo appiccicoso, troppo onesto.",
            "Os espiões do Imperador nunca descem cá abaixo. Escuro demais, pegajoso demais, honesto demais.",
        }),
        ("Barman", new[]
        {
            "What'll it be? Don't answer. Everything here tastes of the floor anyway.",
            "Qu'est-ce que ce sera ? Ne réponds pas. Ici, tout a le goût du plancher de toute façon.",
            "Was darf's sein? Sag nichts. Hier schmeckt sowieso alles nach Fußboden.",
            "¿Qué va a ser? No contestes. Aquí todo sabe a suelo de todas formas.",
            "Cosa ti servo? Non rispondere. Qui tutto sa di pavimento comunque.",
            "O que vai ser? Não respondas. Aqui tudo sabe a chão, de qualquer maneira.",
        }),
        ("PoolWannie", new[]
        {
            "This tart is older than I am. Still better than anything at the bar.",
            "Cette tarte est plus vieille que moi. Toujours meilleure que tout ce qu'on sert au bar.",
            "Dieser Kuchen ist älter als ich. Trotzdem besser als alles an der Bar.",
            "Esta tarta es más vieja que yo. Aun así, mejor que cualquier cosa de la barra.",
            "Questa crostata è più vecchia di me. Comunque meglio di qualsiasi cosa al bancone.",
            "Esta tarte é mais velha do que eu. Mesmo assim, melhor do que tudo o que há no bar.",
        }),
        ("LiftGloom", new[]
        {
            "That lift only goes two places: down here, and back up to trouble.",
            "Cet ascenseur ne va qu'à deux endroits : ici en bas, et là-haut, vers les ennuis.",
            "Dieser Aufzug fährt nur an zwei Orte: hier runter und wieder rauf zu den Schwierigkeiten.",
            "Ese ascensor solo va a dos sitios: aquí abajo, y de vuelta arriba a los problemas.",
            "Quell'ascensore va solo in due posti: quaggiù, e di nuovo su verso i guai.",
            "Esse elevador só vai a dois sítios: cá abaixo, e de volta lá acima, aos sarilhos.",
        }),
        ("PoolSailor", new[]
        {
            "Shh! I'm lining up the shot of my life. Third time tonight.",
            "Chut ! Je prépare le coup de ma vie. Pour la troisième fois ce soir.",
            "Psst! Ich ziele gerade auf den Stoß meines Lebens. Zum dritten Mal heute Abend.",
            "¡Chis! Estoy preparando el tiro de mi vida. Por tercera vez esta noche.",
            "Ssst! Sto preparando il tiro della mia vita. Per la terza volta stasera.",
            "Chiu! Estou a preparar a tacada da minha vida. Pela terceira vez esta noite.",
        }),
        ("PokerGloom", new[]
        {
            "I wear the tie so they think I'm a banker. It hasn't worked yet.",
            "Je porte une cravate pour qu'on me prenne pour un banquier. Ça n'a pas encore marché.",
            "Ich trage die Krawatte, damit sie mich für einen Bankier halten. Hat noch nie geklappt.",
            "Llevo corbata para que crean que soy banquero. Todavía no ha funcionado.",
            "Porto la cravatta perché mi credano un banchiere. Non ha ancora funzionato.",
            "Uso gravata para pensarem que sou banqueiro. Ainda não resultou.",
        }),
        ("PokerWannie", new[]
        {
            "Fold? At my age, young man, everything folds.",
            "Me coucher ? À mon âge, jeune homme, tout se couche.",
            "Aussteigen? In meinem Alter, junger Mann, steigt alles aus.",
            "¿Retirarme? A mi edad, jovencito, todo se retira.",
            "Lasciare? Alla mia età, giovanotto, lascia tutto.",
            "Desistir? Na minha idade, meu jovem, tudo desiste.",
        }),
        ("PokerDissident", new[]
        {
            "I bet my pamphlets. Nobody wants them, so I always lose.",
            "Je mise mes tracts. Personne n'en veut, alors je perds toujours.",
            "Ich setze meine Flugblätter. Keiner will sie, also verliere ich immer.",
            "Apuesto mis panfletos. Nadie los quiere, así que siempre pierdo.",
            "Punto i miei volantini. Nessuno li vuole, così perdo sempre.",
            "Aposto os meus panfletos. Ninguém os quer, por isso perco sempre.",
        }),
        ("CardsGloom", new[]
        {
            "Grey is my lucky colour. It's also my only colour.",
            "Le gris est ma couleur porte-bonheur. C'est aussi ma seule couleur.",
            "Grau ist meine Glücksfarbe. Es ist auch meine einzige Farbe.",
            "El gris es mi color de la suerte. También es mi único color.",
            "Il grigio è il mio colore fortunato. È anche il mio unico colore.",
            "O cinzento é a minha cor da sorte. É também a minha única cor.",
        }),
        ("CardsFranco", new[]
        {
            "I came down with the gas station's takings. Now I'm playing for the gas station.",
            "Je suis descendu avec la recette de la station-service. Maintenant je joue la station-service.",
            "Ich kam mit den Einnahmen der Tankstelle runter. Jetzt spiele ich um die Tankstelle.",
            "Bajé con la caja de la gasolinera. Ahora me estoy jugando la gasolinera.",
            "Sono sceso con l'incasso della stazione di servizio. Ora mi gioco la stazione di servizio.",
            "Desci com a receita da bomba de gasolina. Agora estou a jogar a bomba de gasolina.",
        }),
        ("CardsSailor", new[]
        {
            "Pull up a chair, stranger. Bring zlitos. Lots of zlitos.",
            "Prends une chaise, étranger. Apporte des zlitos. Beaucoup de zlitos.",
            "Nimm dir einen Stuhl, Fremder. Bring Zlitos mit. Viele Zlitos.",
            "Acerca una silla, forastero. Trae zlitos. Muchos zlitos.",
            "Prendi una sedia, straniero. Porta zlitos. Tanti zlitos.",
            "Puxa uma cadeira, forasteiro. Traz zlitos. Muitos zlitos.",
        }),
    };
    // (the change machine's: its question, the two answers, done)
    private static readonly string[][] BowlingChange =
    {
        new[] { "A change machine: two kashes for every zlito. Change your kashes?", "Un changeur de monnaie : deux kashes pour chaque zlito. Changer tes kashes ?", "Ein Wechselautomat: zwei Kashes für jeden Zlito. Deine Kashes wechseln?", "Una máquina de cambio: dos kashes por cada zlito. ¿Cambiar tus kashes?", "Un cambiamonete: due kashes per ogni zlito. Cambiare i tuoi kashes?", "Uma máquina de câmbio: dois kashes por cada zlito. Trocar os teus kashes?" },
        new[] { "Change them", "Les changer", "Wechseln", "Cambiarlos", "Cambiarli", "Trocá-los" },
        new[] { "Keep them", "Les garder", "Behalten", "Quedármelos", "Tenerli", "Ficar com eles" },
        new[] { "Clunk, clunk... your kashes are zlitos now.", "Clonk, clonk... tes kashes sont des zlitos maintenant.", "Klonk, klonk... deine Kashes sind jetzt Zlitos.", "Clonc, clonc... tus kashes ya son zlitos.", "Clonk, clonk... i tuoi kashes ora sono zlitos.", "Clonk, clonk... os teus kashes agora são zlitos." },
    };
    // (where the change machine stands: against the north wall west of the bar, its front south)
    private const int BowlChangeX = 43 * 512 + 256, BowlChangeZ = 9 * 512 + 256;

    private static void BowlingTalk(IslandBeatsA b, string gameDirectory, List<string> log)
    {
        if (!b.Store.SceneExists(BowlingAlley.Scene)) return;
        if (BowlingAlley.Talkers.Count == 0) { log.Add("the bowling alley's people: none listed (the alley built in another run) -- no lines"); return; }
        var lines = BowlingSays.Select(s => s.Line).Concat(BowlingChange).ToArray();
        var first = AddIslandTexts(gameDirectory, RaceTrackIsland.Otringal.IslandByte, lines, log, "the bowling alley");
        if (first < 0) return;
        var said = BowlingSays.Select((s, k) => (s.Who, Id: first + k)).ToDictionary(p => p.Who, p => p.Id);
        int ask = first + BowlingSays.Length, yes = ask + 1, no = ask + 2, done = ask + 3;
        b.Edit(BowlingAlley.Scene, $"the bowling alley: a word from each of its people ({BowlingAlley.Talkers.Count}), and a change machine", model =>
        {
            var machine = Citadel.CitadelProps.Actor(Citadel.CitadelProps.ChangeMachine, BowlChangeX, BowlingAlley.FloorY, BowlChangeZ, 0);
            var machineIndex = model.Actors.Count;
            var addMachine = IslandBeatsA.Add(model, machine, $@"void comportement_0()
{{
    swif (1 == action())
    {{
        if (1100 > distance(0) && {Driving} != comportement_hero())
        {{
            add_choice({yes});
            add_choice({no});
            ask_choice_obj({machineIndex}, {ask});
            if ({yes} == choice())
            {{
                set_var_game({ExchangeVar}, 1);
                message_obj({machineIndex}, {done});
            }}
        }}
    }}
}}
");
            return scripts =>
            {
                addMachine(scripts);
                foreach (var (actor, who) in BowlingAlley.Talkers)
                    if (said.TryGetValue(who, out var id))
                        scripts.SetText(actor, LbaScript.ScriptKind.Life, Talker("0 <= chapter()", $"            message({id});", 1300));
            };
        });
        log.Add($"the bowling alley: {BowlingAlley.Talkers.Count(t => said.ContainsKey(t.Who))} of its people answer Action with a word (texts {first} on), the change machine by the north wall");
    }
}
