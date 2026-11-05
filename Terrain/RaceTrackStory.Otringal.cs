using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The race mod's story on Otringal, chapter 5 (docs/LBRA_WALKTHROUGH.md; the build notes: docs/LBRA_STORY_SCRIPT.md). Otringal's island
// byte (4) is the Emperor's palace's and the Temple of Bú's too: only Otringal's own scenes are edited (its outside scenes, the Casino,
// scene 135).
//   - Arriving by the Esmers' ship (the moon's win: scene 88, the track's start), Lyra at her plinth: the stewards' entry fee, and the
//     Esmers' 10 zlitos (Q_FEE 1); the radio rings, Baldino's call.
//   - The steward beside the start line (a copy of the Casino's doorman, scene 135's actor 8): a Travel Token costs 30 zlitos; short, the
//     Casino round the corner (the game's own slot machines); with 30, "Pay 30 zlitos?" -- yes: the Travel Token (item 24, Q_FEE 2: the
//     race file's gate=217 2).
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
            "The steward by the line sells the Travel Tokens. No token, no grid.",
            "L'intendant près de la ligne vend les Jetons de Voyage. Pas de jeton, pas de grille.",
            "Der Verwalter an der Linie verkauft die Reisemarken. Keine Marke, kein Startplatz.",
            "El administrador junto a la línea vende las Fichas de Viaje. Sin ficha, no hay parrilla.",
            "L'intendente vicino alla linea vende i Gettoni di Viaggio. Niente gettone, niente griglia.",
            "O intendente junto à linha vende as Fichas de Viagem. Sem ficha, não há grelha.",
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
        message_zoe({Say(OtringalLine.ZoeCharming)});
    }}
}}
"),
                };
                return scripts => { foreach (var e in edits) e(scripts); };
            });

        // Lyra at her plinth: the fee and the Esmers' 10 zlitos (Q_FEE 1), the steward, the token. And the translator (item 29), which the
        // control tower's staff and the space port's mechanic answer only to: the game gives it in Baldino's old cell on the moon, which
        // the story doesn't open (the sweep of 2026-11-05)
        MarshalA(b, report, otringal, $@"            if (0 == var_game({QFee}))
            {{
                message({Say(OtringalLine.LyraZeelich)});
                add_gold_pieces({StarterZlitos});
                set_var_game({QFee}, 1);
                if (0 == var_game({Translator}))
                {{
                    message({Say(OtringalLine.LyraTranslator)});
                    found_object({Translator});
                    set_var_game({Translator}, 1);
                    set_used_inventory({Translator});
                }}
            }}
            else if ({FeePaid} > var_game({QFee}))
            {{
                message({Say(OtringalLine.LyraFee)});
            }}
            else if ({FeeWon} > var_game({QFee}))
            {{
                message({Say(OtringalLine.LyraOpen)});
            }}", "Lyra");

        // the steward beside the start line: a copy of the Casino's doorman
        var start = report.StartLine[0];
        // (four cells or more from Lyra, so that Action by one isn't heard by both)
        var lyra = report.Marshal is { Length: >= 10 } mm ? (X: mm[6] / 512.0, Z: mm[7] / 512.0) : (X: double.MaxValue, Z: double.MaxValue);
        (double X, double Z, double Y, int Beta)? StewardSpot()
        {
            // (the nearest ground off the roads, at the road's height, round a point a few cells before or past the line)
            if (report.GroundAfter is { } ground)
                foreach (var back in new[] { 6.0, -6, 10, -10 })
                    for (var r = 0.0; r <= 8; r += 0.5)
                    {
                        double cx = start.X - start.DirX * back, cz = start.Z - start.DirZ * back;
                        var steps = r == 0 ? 1 : (int)Math.Ceiling(2 * Math.PI * r / 0.5);
                        for (var k = 0; k < steps; k++)
                        {
                            double a = 2 * Math.PI * k / steps, px = cx + r * Math.Sin(a), pz = cz + r * Math.Cos(a);
                            if (OffRoads(report, px, pz) < 1 || Math.Sqrt((px - lyra.X) * (px - lyra.X) + (pz - lyra.Z) * (pz - lyra.Z)) <= 4) continue;
                            var g = ground(px, pz);
                            if (Math.Abs(g - start.Y) > 600) continue;
                            return Facing((px, pz, g), start.X, start.Z);
                        }
                    }
            foreach (var back in new[] { 6.0, -6, 10, -10 })
                foreach (var side in new[] { 1, -1 })
                    if (Beside(report, start, back, 1, new[] { side }) is { } s && Math.Sqrt((s.X - lyra.X) * (s.X - lyra.X) + (s.Z - lyra.Z) * (s.Z - lyra.Z)) > 4) return s;
            return OffRoadA(report, start.X - start.DirX * 6, start.Z - start.DirZ * 6, 1) is { } o ? Facing(o, start.X, start.Z) : null;
        }
        if (Copy(b.Originals, CasinoScene, Doorman) is { } steward && StewardSpot() is { } at && b.SceneAt(at.X, at.Z) is var stewardScene and >= 0)
            b.Edit(stewardScene, $"the steward beside the start line at cell ({at.X:0.#}, {at.Z:0.#}): 30 zlitos for a Travel Token (item {TravelToken}, Q_FEE 2)", model =>
            {
                Place(steward, model, at);
                // (no token sold before Lyra has told of the fee, Q_FEE 1: zlitos from selling things on Zeelich would otherwise buy it
                // first, and Lyra's welcome -- the Esmers' 10 zlitos -- would never be said; the price only, until then)
                return IslandBeatsA.Add(model, steward, Talker(otringal, $@"            if ({FeePaid} <= var_game({QFee}) || 0 < var_game({TravelToken}))
            {{
                message({Say(OtringalLine.StewardOnGrid)});
            }}
            else if (0 == var_game({QFee}))
            {{
                message({Say(OtringalLine.StewardFee)});
            }}
            else if ({Fee} > nb_gold_pieces())
            {{
                message({Say(OtringalLine.StewardFee)});
                message({Say(OtringalLine.StewardShort)});
            }}
            else
            {{
                message({Say(OtringalLine.StewardFee)});
                add_choice({Say(OtringalLine.Yes)});
                add_choice({Say(OtringalLine.No)});
                ask_choice({Say(OtringalLine.StewardAsk)});
                if ({Say(OtringalLine.Yes)} == choice())
                {{
                    give_gold_pieces({Fee});
                    message({Say(OtringalLine.StewardToken)});
                    found_object({TravelToken});
                    set_var_game({TravelToken}, 1);
                    set_var_game({QFee}, {FeePaid});
                }}
            }}"));
            });
        else log.Add("the steward left out: no doorman in the Casino (scene 135's actor 8), or no room beside the start line");

        // the Casino: Zoe and Twinsen at the door, the first time a visit (the slot machines are the game's own)
        b.Edit(CasinoScene, "in the Casino, Zoe: \"Baldino said don't\"", model => IslandBeatsA.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 4), $@"void comportement_0()
{{
    if ({otringal} && {FeePaid} > var_game({QFee}))
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
    if (0 == var_cube({Once1}))
    {{
        set_var_cube({Once1}, 1);
        message_zoe({Say(OtringalLine.ZoeDont)});
        message_obj(0, {Say(OtringalLine.TwinsenLittle)});
    }}
}}
"));

        // ... and its slot machines never leave Twinsen too broke for the fee: the pull that spends his last zlito, the Travel Token not yet
        // bought, stops every reel on Money (25 zlitos). The game's odds (a win about one pull in eight, from the Esmers' 10) left one player
        // in four with nothing and the Cup closed to him (the scripted playthrough, 2026-11-05: 35 pulls, one win, 0 zlitos). Each reel's
        // pick is its own scene variable, set after the game's forced key (a pull is paid before the reels spin: hero comportements 10-12).
        b.Edit(CasinoScene, "the Casino's slot machines: the last zlito's pull pays, while the Travel Token is still to buy", _ => scripts =>
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
                b.Edit(johnnyScene, $"Johnny Rocket by the space port at cell ({spot.X:0.#}, {spot.Z:0.#}): the black car's shuttle, and his flight to Celebration Island (inc_chapter, scene {SellerStall})", model =>
                {
                    Place(johnny, model, Facing(spot, port.CubeX * 64 + Pad.X / 512.0, port.CubeY * 64 + Pad.Z / 512.0));
                    return IslandBeatsA.Add(model, johnny, Talker(otringal, $@"            if ({FeeTold} == var_game({QFee}))
            {{
                message({Say(OtringalLine.JohnnyCar)});
                message({Say(OtringalLine.JohnnyFly)});
                set_var_game({QFee}, {FeeDone});
                if (6 > chapter())
                {{
                    inc_chapter();
                }}
                change_cube({SellerStall});
            }}
            else
            {{
                message({Say(OtringalLine.JohnnyBusy)});
            }}"));
                });
            else log.Add("Johnny Rocket left out: no room off the road by the space port's pad");
        }
        else log.Add("Johnny Rocket left out: no Johnny in the hotel (scene 81's actor 3), or no space port scene");

        // the gazogem seller in the Lower City: sold out to a fellow in a black car (IslandRetell's 245), and that's all -- not the game's
        // arrow to the Francos' refinery, nor its variable 123, which puts "The island of the Francos" on the taxis' list (the sweep of
        // 2026-11-05)
        b.Edit(LowerCity, "the gazogem seller: no arrow to the Francos' refinery, no taxi there", _ => scripts =>
            scripts.SetText(GazogemSeller, ScriptKind.Life, Loose(scripts.GetText(GazogemSeller, ScriptKind.Life),
                "message(245);\nset_holo_pos(140);\nset_var_cube(0, 0);\nset_var_game(123, 1);", "message(245);\nset_var_cube(0, 0);")));

        // the eggs: Rick in the Casino, Lyra by the hotel's pool (RaceTrackStory.Eggs.Otringal.cs)
        RickAtTheCasino(b, otringal, Say(OtringalLine.RickCup), Say(OtringalLine.RickTracks), Say(OtringalLine.RickKid));
        LyraPoolScore(b, otringal, Say(OtringalLine.LyraNine), Say(OtringalLine.LyraEight));

        b.Save();
        log.Add($"Otringal's race file: gate={QFee} {FeePaid} (the Travel Token), win={QFee} {FeeWon}");
        return log;
    }
}
