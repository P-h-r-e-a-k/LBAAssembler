using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The race mod's story on the Island of the Francos, chapter 7 (docs/LBRA_WALKTHROUGH.md; the build notes: docs/LBRA_STORY_SCRIPT.md).
// A script-level version: the island is built twice its size, so the story's people stand round the start line (in the walkthrough at
// the refinery's gate, the laboratory and the village), copies of the game's Francos:
//   - Arriving (the lava lake's win), Lyra at her plinth: De La Fontaine's race and refinery (Q_FUEL 1); the radio rings, Baldino's call.
//   - De La Fontaine (a copy of scene 175's actor 2, his family's): who buys his gazogem is his business.
//   - The slick by the start line (Action there): Twinsen's and Zoe's lines, the Can of GazoGem (item 15, Q_FUEL 2).
//   - Mr. Kurtz (a copy of scene 109's actor 8): his glasses -- the nurse has them (Q_FUEL 3); the nurse (scene 109's actor 2): on his
//     head, she'll come (Q_FUEL 4); Mr. Kurtz again: the refinery's best grade, sold by the tanker-load (Q_FUEL 5); De La Fontaine: "So
//     you know" (Q_FUEL 6: the race file's gate=219 6).
//   - Won (win=219 7): out of the car -- Lyra and the Fragment of the Francos (item 31), De La Fontaine's clue (the masked man's tanker
//     sent down the Elevator Platform), and the convoy (inc_chapter: 8; change_cube(120), the Elevator Platform).
// Not built: the nurse following Twinsen back (she says she'll come, and Mr. Kurtz is told she has).
internal static partial class RaceTrackStory
{
    // Q_FUEL: 1 Lyra has told, 2 the can scooped, 3 Mr. Kurtz wants his glasses, 4 the nurse is coming, 5 Mr. Kurtz has read the sample,
    // 6 De La Fontaine has been told (the gate open), 7 the race won, 8 told
    public const int QFuel = 219, FuelCan = 2, FuelGlasses = 3, FuelNurse = 4, FuelRead = 5, FuelOpen = 6, FuelWon = 7, FuelDone = 8;
    private const int GazoGemCan = 15, FragmentFrancos = 31, ElevatorScene = 120, FontaineScene = 175, FontaineActor = 2, VillageScene = 109,
                      KurtzActor = 8, NurseActor = 2;

    private enum FrancosLine
    {
        Ringing, BaldinoRadio, LyraArrive, LyraWait, LyraOpen, FontaineBusiness, SlickTwinsen, SlickZoe, KurtzSample, KurtzWait, NurseGlasses,
        KurtzHead, FontaineKnow, FontaineRace, LyraFragment, FontaineMasked, FontaineConvoy, SlickGone,
        // (the easter egg's: RaceTrackStory.Eggs.Francos.cs)
        KurtzHorror, KurtzCooking,
    }
    private static readonly string[][] FrancosLines =
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
            "The Francos' island is twice the size it looks, the race people made it so. Top speed up, bigger tank. And Twinsen, find out who's buying that gazogem.",
            "L'île des Francos est deux fois plus grande qu'elle n'en a l'air, les gens de la course l'ont faite ainsi. Vitesse de pointe en hausse, réservoir plus gros. Et Twinsen, découvre qui achète ce gazogem.",
            "Die Insel der Francos ist doppelt so groß, wie sie aussieht, die Rennleute haben sie so gemacht. Mehr Höchstgeschwindigkeit, größerer Tank. Und Twinsen, finde heraus, wer dieses Gazogem kauft.",
            "La isla de los Francos es el doble de grande de lo que parece, los de la carrera la hicieron así. Más velocidad punta, depósito más grande. Y Twinsen, averigua quién compra ese gazogem.",
            "L'isola dei Franco è grande il doppio di quanto sembri, l'hanno fatta così quelli della gara. Velocità massima aumentata, serbatoio più grande. E Twinsen, scopri chi compra quel gazogem.",
            "A ilha dos Francos é o dobro do tamanho que parece, o pessoal da corrida fê-la assim. Velocidade máxima maior, depósito maior. E Twinsen, descobre quem anda a comprar aquele gazogem.",
        },
        new[]
        {
            "The Francos' race is De La Fontaine's, and so is the refinery. Ask him about that fuel, if you dare.",
            "La course des Francos appartient à De La Fontaine, et la raffinerie aussi. Interroge-le sur ce carburant, si tu l'oses.",
            "Das Rennen der Francos gehört De La Fontaine, und die Raffinerie auch. Frag ihn nach diesem Treibstoff, wenn du dich traust.",
            "La carrera de los Francos es de De La Fontaine, y la refinería también. Pregúntale por ese combustible, si te atreves.",
            "La gara dei Franco è di De La Fontaine, e anche la raffineria. Chiedigli di quel carburante, se hai il coraggio.",
            "A corrida dos Francos é do De La Fontaine, e a refinaria também. Pergunta-lhe por esse combustível, se tiveres coragem.",
        },
        new[]
        {
            "De La Fontaine won't drop the flag until he's ready. And he isn't.",
            "De La Fontaine ne baissera pas le drapeau avant d'être prêt. Et il ne l'est pas.",
            "De La Fontaine senkt die Flagge erst, wenn er so weit ist. Und das ist er nicht.",
            "De La Fontaine no bajará la bandera hasta que esté listo. Y no lo está.",
            "De La Fontaine non abbasserà la bandiera finché non sarà pronto. E non lo è.",
            "O De La Fontaine não baixa a bandeira enquanto não estiver pronto. E não está.",
        },
        new[]
        {
            "He's ready for you now. Line up whenever you like.",
            "Il est prêt maintenant. Mets-toi en place quand tu veux.",
            "Jetzt ist er bereit für dich. Stell dich auf, wann du willst.",
            "Ahora está listo. Colócate cuando quieras.",
            "Adesso è pronto. Mettiti in griglia quando vuoi.",
            "Agora está pronto. Alinha quando quiseres.",
        },
        new[]
        {
            "Who buys my gazogem is my business. Race me, if you want to talk.",
            "Qui achète mon gazogem, ça ne regarde que moi. Fais-moi la course, si tu veux parler.",
            "Wer mein Gazogem kauft, ist meine Sache. Fahr gegen mich, wenn du reden willst.",
            "Quién compra mi gazogem es asunto mío. Corre contra mí, si quieres hablar.",
            "Chi compra il mio gazogem sono affari miei. Sfidami, se vuoi parlare.",
            "Quem compra o meu gazogem é assunto meu. Corre contra mim, se quiseres conversar.",
        },
        new[]
        {
            "Fresh oil, still warm.",
            "De l'huile fraîche, encore tiède.",
            "Frisches Öl, noch warm.",
            "Aceite fresco, todavía caliente.",
            "Olio fresco, ancora caldo.",
            "Óleo fresco, ainda quente.",
        },
        new[]
        {
            "I've got a can in the toolbox. Scoop some up.",
            "J'ai un bidon dans la caisse à outils. Ramasses-en un peu.",
            "Ich habe einen Kanister im Werkzeugkasten. Schöpf etwas davon auf.",
            "Tengo una lata en la caja de herramientas. Recoge un poco.",
            "Ho una tanica nella cassetta degli attrezzi. Raccoglinene un po'.",
            "Tenho uma lata na caixa de ferramentas. Apanha um bocado.",
        },
        new[]
        {
            "A sample? Splendid! But my nurse has my glasses, and she's out with the pram. Bring her here and I'll tell you everything that's in it.",
            "Un échantillon ? Splendide ! Mais mon infirmière a mes lunettes, et elle est sortie avec le landau. Amène-la ici et je te dirai tout ce qu'il contient.",
            "Eine Probe? Großartig! Aber meine Krankenschwester hat meine Brille, und sie ist mit dem Kinderwagen unterwegs. Bring sie her, und ich sage dir alles, was darin ist.",
            "¿Una muestra? ¡Espléndido! Pero mi enfermera tiene mis gafas, y ha salido con el cochecito. Tráela aquí y te diré todo lo que contiene.",
            "Un campione? Splendido! Ma la mia infermiera ha i miei occhiali, ed è fuori con la carrozzina. Portala qui e ti dirò tutto quello che contiene.",
            "Uma amostra? Esplêndido! Mas a minha enfermeira tem os meus óculos, e saiu com o carrinho de bebé. Trá-la cá e digo-te tudo o que tem lá dentro.",
        },
        new[]
        {
            "Without my glasses I can't tell gazogem from gravy. Find my nurse!",
            "Sans mes lunettes, je ne distingue pas le gazogem de la sauce. Trouve mon infirmière !",
            "Ohne meine Brille kann ich Gazogem nicht von Bratensoße unterscheiden. Finde meine Krankenschwester!",
            "Sin mis gafas no distingo el gazogem de la salsa. ¡Encuentra a mi enfermera!",
            "Senza occhiali non distinguo il gazogem dal sugo. Trova la mia infermiera!",
            "Sem os óculos não distingo gazogem de molho. Encontra a minha enfermeira!",
        },
        new[]
        {
            "His glasses? On his head, as always. I'll come and show him.",
            "Ses lunettes ? Sur sa tête, comme toujours. Je viens les lui montrer.",
            "Seine Brille? Auf seinem Kopf, wie immer. Ich komme und zeige sie ihm.",
            "¿Sus gafas? En la cabeza, como siempre. Voy a enseñárselas.",
            "I suoi occhiali? Sulla testa, come sempre. Vengo a mostrarglieli.",
            "Os óculos dele? Na cabeça, como sempre. Eu vou lá mostrar-lhos.",
        },
        new[]
        {
            "On my head! Now then... this is the refinery's own gazogem, the best grade, sold only by the tanker-load. Keep the can: it's evidence.",
            "Sur ma tête ! Voyons voir... c'est le gazogem de la raffinerie elle-même, la meilleure qualité, vendue seulement par citerne entière. Garde le bidon : c'est une preuve.",
            "Auf meinem Kopf! Nun denn... das ist das Gazogem der Raffinerie selbst, die beste Sorte, nur tankwagenweise verkauft. Behalte den Kanister: Er ist ein Beweisstück.",
            "¡En mi cabeza! Veamos... es el gazogem de la propia refinería, la mejor calidad, que solo se vende por camiones cisterna. Guarda la lata: es una prueba.",
            "Sulla mia testa! Vediamo... è il gazogem della raffineria stessa, la qualità migliore, venduto solo a cisterne intere. Tieni la tanica: è una prova.",
            "Na minha cabeça! Ora vejamos... é o gazogem da própria refinaria, da melhor qualidade, vendido só ao camião-cisterna. Guarda a lata: é uma prova.",
        },
        new[]
        {
            "...So you know. Beat me on the track and I'll tell you who I sold it to.",
            "...Alors tu sais. Bats-moi sur le circuit et je te dirai à qui je l'ai vendu.",
            "...Du weißt es also. Schlag mich auf der Strecke, und ich sage dir, wem ich es verkauft habe.",
            "...Así que lo sabes. Gáname en el circuito y te diré a quién se lo vendí.",
            "...Quindi lo sai. Battimi in pista e ti dirò a chi l'ho venduto.",
            "...Então já sabes. Vence-me na pista e digo-te a quem o vendi.",
        },
        new[]
        {
            "The flag is down whenever you are ready. My tanker is.",
            "Le drapeau tombe dès que tu es prêt. Ma citerne l'est.",
            "Die Flagge fällt, sobald du bereit bist. Mein Tankwagen ist es.",
            "La bandera baja en cuanto estés listo. Mi cisterna lo está.",
            "La bandiera si abbassa quando sei pronto. La mia cisterna lo è.",
            "A bandeira baixa quando estiveres pronto. A minha cisterna está.",
        },
        new[]
        {
            "The Fragment of the Francos is yours!",
            "Le Fragment des Francos est à toi !",
            "Das Fragment der Francos gehört dir!",
            "¡El Fragmento de los Francos es tuyo!",
            "Il Frammento dei Franco è tuo!",
            "O Fragmento dos Francos é teu!",
        },
        new[]
        {
            "A masked man paid for a whole tanker. Cash, Dark Monk coins. I sent it down the Elevator Platform to the undergas, where he said he'd collect it.",
            "Un homme masqué a payé une citerne entière. En liquide, en pièces du Moine Noir. Je l'ai envoyée par la Plate-forme élévatrice jusqu'au sous-gaz, où il a dit qu'il viendrait la chercher.",
            "Ein maskierter Mann hat einen ganzen Tankwagen bezahlt. Bar, in Münzen des Dunklen Mönchs. Ich habe ihn über die Aufzugsplattform ins Untergas geschickt, wo er ihn abholen wollte.",
            "Un hombre enmascarado pagó una cisterna entera. En efectivo, en monedas del Monje Oscuro. La mandé por la Plataforma elevadora al subgás, donde dijo que la recogería.",
            "Un uomo mascherato ha pagato un'intera cisterna. In contanti, monete del Monaco Nero. L'ho mandata giù dalla Piattaforma elevatrice fino al sottogas, dove ha detto che l'avrebbe ritirata.",
            "Um homem mascarado pagou uma cisterna inteira. Em dinheiro, moedas do Monge Negro. Mandei-a pela Plataforma elevatória até ao subgás, onde ele disse que a ia buscar.",
        },
        new[]
        {
            "Take the next convoy down and see for yourself.",
            "Prends le prochain convoi pour descendre et vois par toi-même.",
            "Nimm den nächsten Konvoi nach unten und sieh selbst nach.",
            "Coge el próximo convoy hacia abajo y compruébalo tú mismo.",
            "Prendi il prossimo convoglio e vai a vedere di persona.",
            "Apanha o próximo comboio para baixo e vê com os teus olhos.",
        },
        new[]
        {
            "Just a dark stain on the ground now.",
            "Plus qu'une tache sombre sur le sol.",
            "Jetzt nur noch ein dunkler Fleck auf dem Boden.",
            "Ahora solo es una mancha oscura en el suelo.",
            "Ormai solo una macchia scura per terra.",
            "Agora é só uma mancha escura no chão.",
        },
        new[]
        {
            "The horror... the horror...",
            "L'horreur... l'horreur...",
            "Das Grauen... das Grauen...",
            "El horror... el horror...",
            "L'orrore... l'orrore...",
            "O horror... o horror...",
        },
        new[]
        {
            "No, wait, that's the nurse's cooking. The fuel's fine.",
            "Non, attends, ça, c'est la cuisine de l'infirmière. Le carburant n'a rien.",
            "Nein, warte, das ist die Küche der Krankenschwester. Mit dem Treibstoff ist alles in Ordnung.",
            "No, espera, eso es la comida de la enfermera. El combustible está bien.",
            "No, aspetta, questa è la cucina dell'infermiera. Il carburante va bene.",
            "Não, espera, isso é a comida da enfermeira. O combustível está bom.",
        },
    };

    private static List<string> ApplyFrancos(string gameDirectory, RaceTrackReport report, RaceTrackScenes.Result scenes)
    {
        var log = new List<string>();
        if (report.StartLine.Count == 0 || scenes.StartScene < 0) { log.Add("the Island of the Francos: no story, its track has no start line"); return log; }
        var first = AddIslandTexts(gameDirectory, RaceTrackIsland.Knartas.IslandByte, FrancosLines, log, "the Island of the Francos");
        if (first < 0) return log;
        int Say(FrancosLine line) => first + (int)line;
        var b = new IslandBeatsA(gameDirectory, RaceTrackIsland.Knartas, log);
        const string francos = "7 == chapter()";
        var start = report.StartLine[0];

        // the radio, and after a win Lyra (the fragment) and De La Fontaine's clue -- on every outside scene
        var fontaineColour = Copy(b.Originals, FontaineScene, FontaineActor)?.CoulObj ?? 4;
        foreach (var scene in b.OutsideScenes.ToList())
            b.Edit(scene, "the radio rings in chapter 7; after a win Lyra (the Fragment of the Francos), De La Fontaine's clue and the convoy (inc_chapter, scene 120)", model =>
            {
                var spot = AfterRaceSpot(report);
                if ((int)Math.Floor(spot.X / 64) != model.CubeX || (int)Math.Floor(spot.Z / 64) != model.CubeY) spot = (model.CubeX * 64 + 32, model.CubeY * 64 + 32, start.Y, 0);
                var fontaine = model.Actors.Count;
                var edits = new List<Action<SceneScripts>>
                {
                    IslandBeatsA.Add(model, Voice(model, spot, fontaineColour), "void comportement_0()\n{\n}\n"),
                    IslandBeatsA.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 4),
                        RadioController(7, Say(FrancosLine.Ringing), new[] { Say(FrancosLine.BaldinoRadio) })),
                    IslandBeatsA.Add(model, Voice(model, spot, 9), $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if ({FuelWon} == var_game({QFuel}) && {Driving} != comportement_hero())
    {{
        set_var_game({QFuel}, {FuelDone});
        message({Say(FrancosLine.LyraFragment)});
        found_object({FragmentFrancos});
        set_var_game({FragmentFrancos}, 1);
        message_obj({fontaine}, {Say(FrancosLine.FontaineMasked)});
        message_obj({fontaine}, {Say(FrancosLine.FontaineConvoy)});
        if (8 > chapter())
        {{
            inc_chapter();
        }}
        change_cube({ElevatorScene});
    }}
}}
"),
                };
                return scripts => { foreach (var e in edits) e(scripts); };
            });

        // Lyra at her plinth
        MarshalA(b, report, francos, $@"            if (0 == var_game({QFuel}))
            {{
                message({Say(FrancosLine.LyraArrive)});
                set_var_game({QFuel}, 1);
            }}
            else if ({FuelOpen} > var_game({QFuel}))
            {{
                message({Say(FrancosLine.LyraWait)});
            }}
            else if ({FuelWon} > var_game({QFuel}))
            {{
                message({Say(FrancosLine.LyraOpen)});
            }}", "Lyra");

        // the people round the start line: the start is a raised deck over the dock (the ground below it is out of Twinsen's reach, where
        // he gets out of his car), so each stands on the deck at its edge -- `back` cells before the line, the edge to the `side` (1 or
        // -1) of the road's middle, half a cell in from it; the cars drive through people
        (double X, double Z, double Y)? DeckEdge(double back, int side)
        {
            double cx = start.X - start.DirX * back, cz = start.Z - start.DirZ * back;
            (double X, double Z, double Y)? last = null;
            for (var d = 0.0; d <= 16; d += 0.25)
            {
                double px = cx - start.DirZ * side * d, pz = cz + start.DirX * side * d;
                if (OffRoads(report, px, pz) > -0.5) break;
                last = (px, pz, start.Y);
            }
            return last;
        }
        void Person(string what, SceneActorModel? who, double back, int side, string talk, int reach = 1500)
        {
            if (who is null) { log.Add($"{what} left out: the game's actor isn't there"); return; }
            if (DeckEdge(back, side) is not { } at) { log.Add($"{what} left out: no deck there"); return; }
            // (not one to fall: the deck is the race-track mode's floor, not the island's, and someone who can fall drops through it)
            who.Flags &= ~0x800u;
            var scene = b.SceneAt(at.X, at.Z);
            if (scene < 0) { log.Add($"{what} left out: no scene at cell ({at.X:0.#}, {at.Z:0.#})"); return; }
            b.Edit(scene, $"{what} at cell ({at.X:0.#}, {at.Z:0.#})", model =>
            {
                Place(who, model, Facing(at, start.X, start.Z));
                return IslandBeatsA.Add(model, who, Talker(francos, talk, reach));
            });
        }
        Person("De La Fontaine: his gazogem is his business; with Mr. Kurtz's word, \"So you know\" (Q_FUEL 6)", Copy(b.Originals, FontaineScene, FontaineActor), 4, -1, $@"            if ({FuelRead} == var_game({QFuel}))
            {{
                message({Say(FrancosLine.FontaineKnow)});
                set_var_game({QFuel}, {FuelOpen});
            }}
            else if ({FuelOpen} <= var_game({QFuel}))
            {{
                message({Say(FrancosLine.FontaineRace)});
            }}
            else
            {{
                message({Say(FrancosLine.FontaineBusiness)});
            }}");
        // (the slick: an invisible spot Action is pressed at, on the deck's edge a few cells past the line)
        var slickAt = DeckEdge(-3, -1);
        if (slickAt is { } sl && b.SceneAt(sl.X, sl.Z) is var slickScene and >= 0)
            b.Edit(slickScene, $"the fresh gazogem slick at cell ({sl.X:0.#}, {sl.Z:0.#}): the Can of GazoGem (item {GazoGemCan}, Q_FUEL 2)", model =>
                IslandBeatsA.Add(model, Voice(model, (sl.X, sl.Z, sl.Y, 0), 4), Talker(francos, $@"            if ({FuelCan} > var_game({QFuel}))
            {{
                message_obj(0, {Say(FrancosLine.SlickTwinsen)});
                message_zoe({Say(FrancosLine.SlickZoe)});
                found_object({GazoGemCan});
                set_var_game({GazoGemCan}, 1);
                set_var_game({QFuel}, {FuelCan});
            }}
            else
            {{
                message_obj(0, {Say(FrancosLine.SlickGone)});
            }}", 1200)));
        else log.Add("the slick left out: no room off the road past the start line");
        Person("Mr. Kurtz: the sample, his glasses (Q_FUEL 3), read with the nurse's word (5)", Copy(b.Originals, VillageScene, KurtzActor), 8, 1, $@"            if ({FuelNurse} == var_game({QFuel}))
            {{
{KurtzHorror(Say)}                message({Say(FrancosLine.KurtzHead)});
                set_var_game({QFuel}, {FuelRead});
            }}
            else if ({FuelCan} == var_game({QFuel}))
            {{
                message({Say(FrancosLine.KurtzSample)});
                set_var_game({QFuel}, {FuelGlasses});
            }}
            else if ({FuelGlasses} == var_game({QFuel}))
            {{
                message({Say(FrancosLine.KurtzWait)});
            }}
            else if ({FuelRead} <= var_game({QFuel}))
            {{
                message({Say(FrancosLine.KurtzHead)});
            }}
            else
            {{
                message({Say(FrancosLine.KurtzWait)});
            }}");
        Person("the nurse: Mr. Kurtz's glasses are on his head (Q_FUEL 4)", Copy(b.Originals, VillageScene, NurseActor), 9, -1, $@"            if ({FuelGlasses} == var_game({QFuel}))
            {{
                message({Say(FrancosLine.NurseGlasses)});
                set_var_game({QFuel}, {FuelNurse});
            }}
            else
            {{
                message({Say(FrancosLine.NurseGlasses)});
            }}");

        // De La Fontaine's kin in scene 175 (the game's: "Roger de la Fontaine, he's my brother"): Twinsen's "But I beat him, you know,
        // your brother! On his own track!!!" only once he has (the race won, Q_FUEL 7) -- the game's said it with the Francos' Fragment or
        // the four joined held, and the game's own dig in the village hands that Fragment over before the race: the sweep of 2026-11-05
        b.Edit(FontaineScene, "De La Fontaine's brother's line: \"But I beat him\" only once the race is won", _ => scripts =>
            scripts.SetText(FontaineActor, ScriptKind.Life, Loose(scripts.GetText(FontaineActor, ScriptKind.Life),
                "if (0 == var_game(55) && 0 == var_game(31))", $"if ({FuelWon} > var_game({QFuel}) || (0 == var_game(55) && 0 == var_game({FragmentFrancos})))")));

        b.Save();
        log.Add($"the Island of the Francos' race file: gate={QFuel} {FuelOpen} (De La Fontaine told), win={QFuel} {FuelWon}");
        return log;
    }
}
