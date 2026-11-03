using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Chapter 10 of the story (docs/LBRA_WALKTHROUGH.md), Mosquibees Island (MOSQUIBE, outside scenes 102, 103, 105; Twinsen comes out of the
// Wannies' tunnel into scene 102):
//   - arrival: Lyra's word (Q_PASS 1), then Baldino's call on the radio (GroupB.Arrival, in every outside scene: var 236 10);
//   - the Queen's ventilation guy (scene 104's actor 5, copied) by the start line: no pass, no grid -- and quietly, try down the road
//     (Q_PASS 2); with the ring, off you go (Q_PASS 4: the start line's gate, RaceCarEngineFile);
//   - the honey-pot Mosquibee (scene 103's actor 6, copied) by the start: the Queen never loses at home;
//   - the hide-out's Mosquibee (scene 105's actor 8, copied) hiding beside the road a way down the lap: the Dissidents' Ring (item 16;
//     Q_PASS 3);
//   - the race (finish= 237, win= chapter 11). Won: the Queen's "Hmph.", Lyra, the Fragment of the Mosquibees (item 33), the dissidents'
//     message, and to the Island under Celebration (scene 130).
internal static partial class RaceTrackStory
{
    // Q_PASS: 1 Lyra's word heard, 2 the ventilation guy has told, 3 the ring found, 4 the ring shown (the start line's gate)
    public const int QPass = 221, VentTold = 2, RingFound = 3, RingShown = 4;
    private const int MosquibeesChapter = 10, MosquibeesTexts = 3 + 7, DissidentsRing = 16, MosquibeesFragment = 33, VolcanoArrival = 130;
    private const int ThroneScene = 104, VentActor = 5, HoneyScene = 103, HoneyActor = 6, HideScene = 105, HideActor = 8;

    internal enum MosquibeesLine { LyraArrival, Ring, Baldino, VentNo, VentQuiet, HoneyPot, HideRing, VentRing, QueenHmph, LyraFragment, HideMessage }
    internal static readonly string[][] MosquibeesLines =
    {
        new[]
        {
            "The Queen races only those she's allowed onto her island. We're... not sure you are.",
            "La Reine ne fait la course qu'avec ceux qu'elle autorise sur son île. Nous ne sommes... pas sûrs que tu le sois.",
            "Die Königin fährt nur gegen die, die sie auf ihre Insel lässt. Wir sind uns... nicht sicher, ob du dazugehörst.",
            "La Reina solo corre con quienes deja entrar en su isla. No estamos... seguros de que tú puedas.",
            "La Regina corre solo con chi ammette sulla sua isola. Non siamo... sicuri che tu lo sia.",
            "A Rainha só corre com quem ela deixa entrar na ilha dela. Não temos... a certeza de que sejas um deles.",
        },
        GroupB.Ring,
        new[]
        {
            "Baldino here! Mountain gearing for the Queen's climbs. Torque, torque, torque. And bow. Queens like a bow.",
            "Ici Baldino ! Une boîte de montagne pour les côtes de la Reine. Du couple, du couple, du couple. Et incline-toi. Les reines aiment qu'on s'incline.",
            "Hier Baldino! Berggetriebe für die Steigungen der Königin. Drehmoment, Drehmoment, Drehmoment. Und verbeug dich. Königinnen mögen Verbeugungen.",
            "¡Aquí Baldino! Marchas de montaña para las subidas de la Reina. Par, par y más par. Y haz una reverencia. A las reinas les gustan las reverencias.",
            "Qui Baldino! Rapporti da montagna per le salite della Regina. Coppia, coppia, coppia. E fai un inchino. Alle regine piacciono gli inchini.",
            "Fala o Baldino! Mudanças de montanha para as subidas da Rainha. Binário, binário, binário. E faz uma vénia. As rainhas gostam de vénias.",
        },
        new[]
        {
            "No pass, no grid. The Queen's orders.",
            "Pas de laissez-passer, pas de grille. Ordre de la Reine.",
            "Kein Passierschein, keine Startaufstellung. Befehl der Königin.",
            "Sin pase, no hay parrilla. Órdenes de la Reina.",
            "Niente lasciapassare, niente griglia. Ordini della Regina.",
            "Sem passe, não há grelha. Ordens da Rainha.",
        },
        new[]
        {
            "...unless you know the right people. Try down the road.",
            "...à moins de connaître les bonnes personnes. Essaie plus bas sur la route.",
            "...es sei denn, du kennst die richtigen Leute. Versuch's weiter unten an der Straße.",
            "...a menos que conozcas a la gente adecuada. Prueba carretera abajo.",
            "...a meno che tu non conosca le persone giuste. Prova giù per la strada.",
            "...a não ser que conheças as pessoas certas. Experimenta mais abaixo na estrada.",
        },
        new[]
        {
            "The Queen's never lost a race on her own island. Never!",
            "La Reine n'a jamais perdu une course sur sa propre île. Jamais !",
            "Die Königin hat auf ihrer eigenen Insel noch nie ein Rennen verloren. Nie!",
            "¡La Reina nunca ha perdido una carrera en su propia isla! ¡Nunca!",
            "La Regina non ha mai perso una corsa sulla sua isola. Mai!",
            "A Rainha nunca perdeu uma corrida na sua própria ilha. Nunca!",
        },
        new[]
        {
            "You're the racer who's after the black car? So are we. Take this ring and show it at the grid: our friend there will let you through. And after the race, come and find me. We've got a message for you.",
            "C'est toi, le pilote qui poursuit la voiture noire ? Nous aussi. Prends cette bague et montre-la à la grille : notre ami là-bas te laissera passer. Et après la course, viens me trouver. Nous avons un message pour toi.",
            "Du bist der Fahrer, der hinter dem schwarzen Wagen her ist? Wir auch. Nimm diesen Ring und zeig ihn an der Startaufstellung: Unser Freund dort lässt dich durch. Und nach dem Rennen such mich. Wir haben eine Nachricht für dich.",
            "¿Eres el piloto que persigue al coche negro? Nosotros también. Toma este anillo y enséñalo en la parrilla: nuestro amigo de allí te dejará pasar. Y después de la carrera, ven a buscarme. Tenemos un mensaje para ti.",
            "Sei tu il pilota che dà la caccia alla macchina nera? Anche noi. Prendi questo anello e mostralo alla griglia: il nostro amico lì ti farà passare. E dopo la corsa, vieni a cercarmi. Abbiamo un messaggio per te.",
            "És tu o piloto que anda atrás do carro preto? Nós também. Leva este anel e mostra-o na grelha: o nosso amigo lá deixa-te passar. E depois da corrida, vem procurar-me. Temos uma mensagem para ti.",
        },
        new[]
        {
            "Ah. That ring. Off you go, then. I never saw it.",
            "Ah. Cette bague. Vas-y, alors. Je ne l'ai jamais vue.",
            "Ah. Dieser Ring. Dann nur zu. Ich habe ihn nie gesehen.",
            "Ah. Ese anillo. Adelante, entonces. Yo nunca lo he visto.",
            "Ah. Quell'anello. Vai pure, allora. Io non l'ho mai visto.",
            "Ah. Esse anel. Podes ir, então. Eu nunca o vi.",
        },
        new[]
        {
            "Hmph.",
            "Hmph.",
            "Hmpf.",
            "Hmph.",
            "Hmph.",
            "Hmph.",
        },
        new[]
        {
            "You beat the Queen! The Fragment of the Mosquibees!",
            "Tu as battu la Reine ! Le Fragment des Mosquibees !",
            "Du hast die Königin geschlagen! Das Fragment der Moskibienos!",
            "¡Has ganado a la Reina! ¡El Fragmento de los Mosquibitas!",
            "Hai battuto la Regina! Il Frammento dei Moschibei!",
            "Venceste a Rainha! O Fragmento dos Mosquibees!",
        },
        new[]
        {
            "Here's our message: a map. The black car's workshop is on Island CX, but its driver hides under Celebration Island, in the volcano. Our hide-out is there. We'll take you.",
            "Voici notre message : une carte. L'atelier de la voiture noire est sur l'îlot CX, mais son pilote se cache sous l'île du volcan, dans le volcan. Notre repaire est là-bas. Nous allons t'y emmener.",
            "Hier ist unsere Nachricht: eine Karte. Die Werkstatt des schwarzen Wagens ist auf dem Eiland CX, aber sein Fahrer versteckt sich unter der Insel des Vulkans, im Vulkan. Unser Versteck ist dort. Wir bringen dich hin.",
            "Este es nuestro mensaje: un mapa. El taller del coche negro está en el islote CX, pero su piloto se esconde bajo la isla del volcán, dentro del volcán. Nuestro escondite está allí. Te llevaremos.",
            "Ecco il nostro messaggio: una mappa. L'officina della macchina nera è sull'isolotto CX, ma il suo pilota si nasconde sotto l'isola del vulcano, nel vulcano. Il nostro rifugio è lì. Ti ci porteremo.",
            "Eis a nossa mensagem: um mapa. A oficina do carro preto fica na Ilhota CX, mas o piloto esconde-se debaixo da ilha do vulcão, dentro do vulcão. O nosso esconderijo é lá. Nós levamos-te.",
        },
    };

    private static List<string> ApplyMosquibees(string gameDirectory, RaceTrackReport report, RaceTrackScenes.Result scenes)
    {
        var log = new List<string>();
        var first = GroupB.AddLines(gameDirectory, MosquibeesTexts, MosquibeesLines, log, "Mosquibees Island's (chapter 10)");
        int Say(MosquibeesLine l) => first + (int)l;
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var originals = RaceTrackService.OriginalScenes(store);
        var island = RaceTrackIsland.Mosquibe;
        var when = $"{MosquibeesChapter} == chapter()";
        var hiding = Copy(originals, HideScene, HideActor);
        // the arrival and the radio, and after the race, in each outside scene
        var won = $@"            message({Say(MosquibeesLine.QueenHmph)});
            message({Say(MosquibeesLine.LyraFragment)});
            found_object({MosquibeesFragment});
            set_var_game({MosquibeesFragment}, 1);
            message({Say(MosquibeesLine.HideMessage)});
            change_cube({VolcanoArrival});";
        for (var s = island.FirstScene; s <= island.LastScene; s++)
        {
            if (s == ThroneScene) continue;
            GroupB.EditScene(store, s, log, "Lyra's word on arrival (Q_PASS 1), Baldino's call on the radio, and after the race won the Queen's word, the fragment, the dissidents' message and the way to the volcano", model =>
            {
                var arrival = GroupB.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 9),
                    GroupB.Arrival(when, MosquibeesChapter, $"0 == var_game({QPass})", $"set_var_game({QPass}, 1);", Say(MosquibeesLine.LyraArrival), Say(MosquibeesLine.Ring), Say(MosquibeesLine.Baldino)));
                var after = GroupB.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, hiding?.CoulObj ?? 9),
                    GroupB.AfterRace(MosquibeesChapter, won));
                return scripts => { arrival(scripts); after(scripts); };
            });
        }

        // the ventilation guy and the honey-pot Mosquibee by the start, facing where Twinsen comes in
        // (round where Twinsen comes into the start scene -- out of the Wannies' tunnel -- on his own ground: the marshal's plinth is on the
        // mountain's edge, where a person put beside it stood over the drop)
        var by = (X: 0.0, Z: 0.0, Y: 0.0);
        if (scenes.StartScene >= 0 && store.SceneExists(scenes.StartScene) && store.Load(scenes.StartScene) is { } start)
            by = (start.CubeX * 64 + start.Hero.X / 512.0, start.CubeY * 64 + start.Hero.Z / 512.0, start.Hero.Y);
        else if (GroupB.MarshalAt(report) is { } marshal) by = marshal;
        var vent = Copy(originals, ThroneScene, VentActor);
        var ventAt = GroupB.SpotAt(store, island, report, by.X + 1, by.Z + 1, by.Y, by.X, by.Z, 1, 24);
        if (vent is not null && ventAt is { } v)
            GroupB.EditScene(store, v.Scene, log, $"the ventilation guy at cell ({v.At.X:0.#}, {v.At.Z:0.#}): no pass, no grid; with the ring, through (Q_PASS 2, 4)", model =>
            {
                Place(vent, model, v.At);
                return GroupB.Add(model, vent, Talker(when, $@"            if ({RingFound} <= var_game({QPass}))
            {{
                message({Say(MosquibeesLine.VentRing)});
                set_var_game({QPass}, {RingShown});
            }}
            else
            {{
                message({Say(MosquibeesLine.VentNo)});
                message({Say(MosquibeesLine.VentQuiet)});
                if ({VentTold} > var_game({QPass}))
                {{
                    set_var_game({QPass}, {VentTold});
                }}
            }}"));
            });
        else log.Add("the ventilation guy left out: no room off the road by the start line");
        var honey = Copy(originals, HoneyScene, HoneyActor);
        // (two cells and a half from the ventilation guy)
        var honeyFrom = ventAt is { } va ? (X: va.At.X - 2.5, Z: va.At.Z + 1) : (X: by.X - 2.5, Z: by.Z - 2.5);
        if (honey is not null && GroupB.SpotAt(store, island, report, honeyFrom.X, honeyFrom.Z, by.Y, by.X, by.Z, 1, 10) is { } h)
            GroupB.EditScene(store, h.Scene, log, $"the honey-pot Mosquibee at cell ({h.At.X:0.#}, {h.At.Z:0.#})", model =>
            {
                Place(honey, model, h.At);
                return GroupB.Add(model, honey, Talker(when, $"            message({Say(MosquibeesLine.HoneyPot)});"));
            });
        else log.Add("the honey-pot Mosquibee left out: no room off the road by the start line");

        // the hide-out's Mosquibee, hiding beside the road a seventh of the lap on from the start
        if (hiding is not null && report.LapX.Length > 10)
        {
            var i = report.LapX.Length / 7;
            if (GroupB.Spot(store, island, report, report.LapX[i], report.LapZ[i], report.LapX[i], report.LapZ[i], 1.5, 12) is { } d)
                GroupB.EditScene(store, d.Scene, log, $"the hide-out's Mosquibee hiding at cell ({d.At.X:0.#}, {d.At.Z:0.#}): the Dissidents' Ring (item {DissidentsRing}, Q_PASS 3)", model =>
                {
                    Place(hiding, model, d.At);
                    return GroupB.Add(model, hiding, Talker(when, $@"            if ({RingFound} > var_game({QPass}))
            {{
                message({Say(MosquibeesLine.HideRing)});
                found_object({DissidentsRing});
                set_var_game({DissidentsRing}, 1);
                set_var_game({QPass}, {RingFound});
            }}"));
                });
            else log.Add("the hide-out's Mosquibee left out: no room off the road down the lap");
        }
        return log;
    }
}
