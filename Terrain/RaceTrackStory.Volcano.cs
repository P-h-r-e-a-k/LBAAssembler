using System.IO;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The race mod's story, chapter 11: the Island under Celebration (docs/LBRA_WALKTHROUGH.md; the build notes docs/LBRA_STORY_SCRIPT.md),
// on Volcano Island's track (RaceTrackIsland.Volcano: SOUSCELB, scenes 130-132, the race from scene 130). Everyone is by the start line
// on the plateau, in the start scene, while chapter() is 11:
//   - Lyra, the marshal (an invisible talker where the race-track mode draws her): on arrival, the hide-out cracking under the lava
//     (Q_SLABS 1); after the race, the Cup on to Island CX.
//   - Baldino's radio call (with the radio, item 26): "the radio is ringing" once a visit until it is answered from the inventory
//     (use_inventory(26)): armour plate, a protection spell in the boot. Answered: var 238 = the chapter (LateStory.Radio).
//   - The dissident (the hide-out's own, scene 129's actor 2 copied): seal the cracks and I'll race you (Q_SLABS 2).
//   - The hide-out's Mosquibee (scene 129's actor 3 copied), at the cracks: three cold slabs by the road (3); with all three, "That'll
//     hold!" (7: the race's gate opens, gate=222 7 -- RaceCarEngineFile).
//   - Three cooled lava slabs beside the road (the volcano's standing stones, shown once the Mosquibee has told; Action there): found one by one (Q_SLABS 4, 5, 6; which ones: var 239,
//     a bit each), Zoe takes the third.
//   - The race won (win=222 8, RaceCarEngineFile), out of the car: Lyra's word, the dissident's reveal (FunFrock), Zoe, Twinsen, Baldino's
//     unasked call (9); and the ferryman (the game's, scene 132's actor 2 copied, standing by the start line from the win on): the
//     Ferryman's Song (item 18), inc_chapter (12), and his boat to Island CX -- a change-cube zone round him (Island CX Mk2's start scene,
//     343, is past a script's change_cube: its byte) switched on (Q_SLABS 10).
// Not built: the slabs are the volcano's standing stones (no slab body of their own yet), the slate's page (memo_ardoise: the pages aren't made), the protection
// spell in the item box at the start (the race file has no start_item= key: the engine's RACEMOD.CPP would need one).
internal static partial class RaceTrackStory
{
    // Q_SLABS (docs/LBRA_STORY_PLAN.md 3.1): 1 Lyra has told, 2 the dissident has asked, 3 the Mosquibee has told, 4-6 slabs found,
    // 7 sealed (the gate), 8 won (the race file's win=), 9 the reveal said, 10 the ferryman's song and boat
    public const int QSlabs = 222, SlabsTold = 1, SlabsAsked = 2, SlabsWhere = 3, SlabsSealed = 7, SlabsWon = 8, SlabsReveal = 9, SlabsFerry = 10;
    // group C's own: the chapter whose radio call has been answered (238), the slabs found (239: 1, 2, 4)
    public const int LateRadioAnswered = 238, LateSlabsFound = 239;
    private const int VolcanoChapter = 11, VolcanoTexts = 3 + 11, FerrymanSong = 18;
    // the game's hide-out (scene 129: the dissident, actor 2; a Mosquibee, actor 3) and the ferryman (scene 132, actor 2)
    // (and its standing stone for the lava, a cooled slab's stand-in: scene 131's actor 7, entity 218)
    private const int StoneScene = 131, StoneActor = 7;
    private const int HideOut = 129, HideOutDissident = 2, HideOutMosquibee = 3, FerryScene132 = 132, Ferryman = 2;
    // the boat's change-cube zone's switch (Info4: set_change_cube)
    private const int FerrySwitch = 215;

    private enum VLine
    {
        Arrive, Ring, Baldino, Ask, Slabs, Slab, ZoeTakes, Sealed, Won, Reveal, ZoeDream, TwinsenKnows, BaldinoFunFrock, Ferry, FerryAfter, NotYet,
        // (the easter egg's: RaceTrackStory.Eggs.Volcano.cs)
        RingZoe, RingTwinsen,
    }
    private static readonly string[][] VolcanoLines =
    {
        new[]
        {
            "The dissidents' hide-out is cracking under the lava. Nobody's racing until it holds.",
            "La cachette des dissidents se fissure sous la lave. Personne ne court tant qu'elle ne tient pas.",
            "Das Versteck der Rebellen bekommt Risse unter der Lava. Niemand fährt, bevor es hält.",
            "El escondite de los disidentes se agrieta bajo la lava. Nadie corre hasta que aguante.",
            "Il nascondiglio dei dissidenti si crepa sotto la lava. Nessuno corre finché non regge.",
            "O esconderijo dos dissidentes está a rachar com a lava. Ninguém corre até que aguente.",
        },
        new[]
        {
            "The radio is ringing. Answer it from your inventory.",
            "La radio sonne. Réponds depuis ton inventaire.",
            "Das Funkgerät klingelt. Antworte über dein Inventar.",
            "La radio está sonando. Contesta desde tu inventario.",
            "La radio sta squillando. Rispondi dal tuo inventario.",
            "O rádio está a tocar. Atende pelo teu inventário.",
        },
        new[]
        {
            "Baldino: Lava balls?! Armour plate on the roof. And a protection spell in the boot for the start, you'll want it.",
            "Baldino : Des boules de lave ?! Une plaque de blindage sur le toit. Et un sort de protection dans le coffre pour le départ, tu en auras besoin.",
            "Baldino: Lavakugeln?! Panzerplatte aufs Dach. Und ein Schutzzauber im Kofferraum für den Start, den wirst du brauchen.",
            "Baldino: ¡¿Bolas de lava?! Una plancha de blindaje en el techo. Y un hechizo de protección en el maletero para la salida, lo vas a necesitar.",
            "Baldino: Palle di lava?! Una piastra corazzata sul tetto. E un incantesimo di protezione nel bagagliaio per la partenza, ti servirà.",
            "Baldino: Bolas de lava?! Uma placa de blindagem no tejadilho. E um feitiço de proteção na mala para a partida, vais precisar dele.",
        },
        new[]
        {
            "Help us seal the cracks, and I'll race you. I'll even tell you who's under the mask.",
            "Aide-nous à colmater les fissures, et je ferai la course contre toi. Je te dirai même qui se cache sous le masque.",
            "Hilf uns, die Risse abzudichten, dann fahre ich gegen dich. Ich verrate dir sogar, wer unter der Maske steckt.",
            "Ayúdanos a sellar las grietas y correré contra ti. Hasta te diré quién está bajo la máscara.",
            "Aiutaci a sigillare le crepe, e correrò contro di te. Ti dirò perfino chi c'è sotto la maschera.",
            "Ajuda-nos a vedar as fendas, e eu corro contra ti. Até te digo quem está por baixo da máscara.",
        },
        new[]
        {
            "Cold slabs of lava rock, three of them. They've cooled by the road.",
            "Des dalles de lave froide, trois. Elles ont refroidi au bord de la route.",
            "Kalte Lavaplatten, drei Stück. Sie sind am Straßenrand abgekühlt.",
            "Losas frías de roca de lava, tres. Se han enfriado junto a la carretera.",
            "Lastre fredde di roccia lavica, tre. Si sono raffreddate lungo la strada.",
            "Lajes frias de rocha de lava, três. Arrefeceram à beira da estrada.",
        },
        new[]
        {
            "A slab of cooled lava. Heavy!",
            "Une dalle de lave refroidie. Lourde !",
            "Eine Platte aus abgekühlter Lava. Schwer!",
            "Una losa de lava enfriada. ¡Pesa!",
            "Una lastra di lava raffreddata. Pesante!",
            "Uma laje de lava arrefecida. Pesada!",
        },
        new[]
        {
            "Let me take that one.",
            "Laisse-moi porter celle-là.",
            "Lass mich die tragen.",
            "Déjame llevar esa.",
            "Lascia che porti io questa.",
            "Deixa-me levar essa.",
        },
        new[]
        {
            "That'll hold!",
            "Ça va tenir !",
            "Das hält!",
            "¡Esto aguantará!",
            "Questo reggerà!",
            "Isto vai aguentar!",
        },
        new[]
        {
            "Well raced! The Cup goes on to Island CX.",
            "Belle course ! La Coupe continue sur l'îlot CX.",
            "Gut gefahren! Der Pokal geht weiter zum Eiland CX.",
            "¡Buena carrera! La Copa sigue en el islote CX.",
            "Bella corsa! La Coppa prosegue sull'isolotto CX.",
            "Boa corrida! A Taça segue para a Ilhota CX.",
        },
        new[]
        {
            "I promised. I've seen him without his mask: FunFrock. Yes, that FunFrock. He builds his cars on Island CX and buys his soldiers from the Emperor's palace. He wants the Cup's wish, to bring back the Dark Monk.",
            "J'ai promis. Je l'ai vu sans son masque : FunFrock. Oui, ce FunFrock-là. Il construit ses voitures sur l'îlot CX et achète ses soldats au palais de l'Empereur. Il veut le vœu de la Coupe, pour ramener le Moine Noir.",
            "Versprochen ist versprochen. Ich habe ihn ohne Maske gesehen: FunFrock. Ja, genau der FunFrock. Er baut seine Wagen auf dem Eiland CX und kauft seine Soldaten im Palast des Kaisers. Er will den Wunsch des Pokals, um den Schwarzen Mönch zurückzuholen.",
            "Lo prometí. Lo he visto sin su máscara: FunFrock. Sí, ese FunFrock. Construye sus coches en el islote CX y compra sus soldados en el palacio del Emperador. Quiere el deseo de la Copa, para traer de vuelta al Monje Negro.",
            "L'ho promesso. L'ho visto senza maschera: FunFrock. Sì, proprio quel FunFrock. Costruisce le sue auto sull'isolotto CX e compra i suoi soldati al palazzo dell'Imperatore. Vuole il desiderio della Coppa, per riportare indietro il Monaco Nero.",
            "Prometi. Vi-o sem a máscara: FunFrock. Sim, esse FunFrock. Constrói os carros na Ilhota CX e compra os soldados no palácio do Imperador. Quer o desejo da Taça, para trazer de volta o Monge Negro.",
        },
        new[]
        {
            "Twinsen... the dream.",
            "Twinsen... le rêve.",
            "Twinsen... der Traum.",
            "Twinsen... el sueño.",
            "Twinsen... il sogno.",
            "Twinsen... o sonho.",
        },
        new[]
        {
            "I know.",
            "Je sais.",
            "Ich weiß.",
            "Lo sé.",
            "Lo so.",
            "Eu sei.",
        },
        new[]
        {
            "Baldino: Twinsen? I heard that on the Esmers' channel. FunFrock! I'll fit everything I have on the car before the end. Everything.",
            "Baldino : Twinsen ? J'ai entendu ça sur la fréquence des Esmers. FunFrock ! Je monterai tout ce que j'ai sur la voiture avant la fin. Tout.",
            "Baldino: Twinsen? Ich habe das auf dem Kanal der Esmer gehört. FunFrock! Ich baue alles, was ich habe, vor dem Ende in den Wagen ein. Alles.",
            "Baldino: ¿Twinsen? Lo he oído por el canal de los Esmers. ¡FunFrock! Montaré en el coche todo lo que tengo antes del final. Todo.",
            "Baldino: Twinsen? L'ho sentito sul canale degli Esmer. FunFrock! Monterò sulla macchina tutto quello che ho prima della fine. Tutto.",
            "Baldino: Twinsen? Ouvi isso no canal dos Esmers. FunFrock! Vou montar no carro tudo o que tenho antes do fim. Tudo.",
        },
        new[]
        {
            "Island CX? I'll take you, for a song. The song. Here, I'll teach you.",
            "L'îlot CX ? Je t'y emmène, pour une chanson. LA chanson. Tiens, je vais te l'apprendre.",
            "Das Eiland CX? Ich bringe dich hin, für ein Lied. DAS Lied. Hier, ich bringe es dir bei.",
            "¿El islote CX? Te llevo, por una canción. LA canción. Toma, te la enseñaré.",
            "L'isolotto CX? Ti ci porto, per una canzone. LA canzone. Ecco, te la insegno.",
            "A Ilhota CX? Levo-te lá, por uma canção. A canção. Toma, eu ensino-ta.",
        },
        new[]
        {
            "...beautiful. Well, nearly. In you get.",
            "... magnifique. Enfin, presque. Monte.",
            "... wunderschön. Na ja, fast. Steig ein.",
            "... precioso. Bueno, casi. Sube.",
            "... bellissimo. Be', quasi. Sali.",
            "... lindo. Bem, quase. Entra.",
        },
        new[]
        {
            "Island CX? After the race, lad. Win it first.",
            "L'îlot CX ? Après la course, mon garçon. Gagne-la d'abord.",
            "Das Eiland CX? Nach dem Rennen, Junge. Gewinn es zuerst.",
            "¿El islote CX? Después de la carrera, chico. Gánala primero.",
            "L'isolotto CX? Dopo la corsa, ragazzo. Prima vincila.",
            "A Ilhota CX? Depois da corrida, rapaz. Ganha-a primeiro.",
        },
        new[]
        {
            "Twinsen. We still need that.",
            "Twinsen. On en a encore besoin.",
            "Twinsen. Den brauchen wir noch.",
            "Twinsen. Todavía lo necesitamos.",
            "Twinsen. Ci serve ancora.",
            "Twinsen. Ainda precisamos disso.",
        },
        new[]
        {
            "...Right. Sorry. It felt like the thing to do.",
            "...C'est vrai. Pardon. Ça m'a semblé être la chose à faire.",
            "...Stimmt. Entschuldige. Es kam mir irgendwie richtig vor.",
            "...Cierto. Perdona. Me pareció lo que había que hacer.",
            "...Giusto. Scusa. Mi sembrava la cosa da fare.",
            "...Pois é. Desculpa. Pareceu-me a coisa certa a fazer.",
        },
    };

    // Chapters 11-14's stories, by the island built (RaceTrackService.Story, before the island's retelling). Lines for the log.
    public static List<string> ApplyLate(string gameDirectory, RaceTrackReport report, RaceTrackIsland island, RaceTrackScenes.Result scenes)
    {
        if (island == RaceTrackIsland.Volcano) return ApplyVolcano(gameDirectory, report, island, scenes);
        if (island == RaceTrackIsland.CxTowerMk2 || island == RaceTrackIsland.CxTower) return ApplyCx(gameDirectory, report, island, scenes);
        if (island == RaceTrackIsland.Palace) return ApplyPalace(gameDirectory, report, island, scenes);
        if (island == RaceTrackIsland.Celebration) return ApplyFinal(gameDirectory, report, island, scenes);
        return new List<string>();
    }

    // The story's race file keys for chapters 11-14 (RaceCarEngineFile, story sets only): the gate that waits for each chapter's quest
    // and what its win sets.
    public static IEnumerable<string> LateRaceKeys(RaceTrackIsland island)
    {
        if (island == RaceTrackIsland.Volcano)
        {
            yield return $"gate={QSlabs} {SlabsSealed} The hide-out's cracks first!";
            yield return $"win={QSlabs} {SlabsWon}";
            // (Baldino's car update: a protection spell in the item box for the start, against the lava balls -- RACEMOD.CPP start_item=)
            yield return $"start_item={StartProtection}";
        }
        if (island == RaceTrackIsland.CxTowerMk2 || island == RaceTrackIsland.CxTower)
        {
            yield return $"gate={QCxKey} {KeyTraded} The door to the halls is locked!";
            yield return $"win={QCxKey} {KeyWon}";
        }
        // (the palace's win is the sprint's own, win=79 1: the statue risen -- RaceCarEngineFile's dream)
        if (island == RaceTrackIsland.Palace) yield return $"gate={QEmperor} {StanReady} Stan waits in the library!";
        // (the lava lake: after the dive into the lava, the clovers filled -- RACEMOD.CPP clovers_fill=, LavaDive)
        if (island == RaceTrackIsland.CelebrationLava) yield return $"clovers_fill={LavaDived}";
        if (island == RaceTrackIsland.Celebration)
        {
            yield return $"win={FinalStep} {FinalWon}";
            yield return $"finish={QEmperor} {FinalRaced}";
            // (Baldino's last call: "one last thing in the boot: a super jet-pack")
            yield return $"start_item={StartSuperJet}";
        }
    }

    // The power-ups a car update puts in the item box for the start (RACEMOD.CPP's PU_* numbers)
    private const int StartProtection = 1, StartSuperJet = 4;

    // `report`: the Volcano track's build; `island`: its RaceTrackIsland; `scenes`: what its scenes were given. Lines for the log.
    public static List<string> ApplyVolcano(string gameDirectory, RaceTrackReport report, RaceTrackIsland island, RaceTrackScenes.Result scenes)
    {
        var s = new LateStory(gameDirectory, report, island, scenes.StartScene);
        if (!s.Ready("the Island under Celebration")) return s.Log;
        var first = s.Texts(VolcanoTexts, VolcanoLines, "the Island under Celebration");
        int Id(VLine line) => first + (int)line;
        var when = $"{VolcanoChapter} == chapter()";
        var start = report.StartLine[0];
        var dissident = Copy(s.Originals, HideOut, HideOutDissident);
        var mosquibee = Copy(s.Originals, HideOut, HideOutMosquibee);
        var ferryman = Copy(s.Originals, FerryScene132, Ferryman);
        if (dissident is null || mosquibee is null || ferryman is null) { s.Log.Add("chapter 11 left out: the game's dissident, Mosquibee (scene 129) or ferryman (scene 132) not found"); return s.Log; }
        // (the CX track's start scene and Twinsen's place on its grid: the boat's landing)
        var (cxScene, cxAt) = s.Landing(RaceTrackIsland.CxTowerMk2.FirstScene + RaceTrackIsland.CxTowerMk2.MoreScenes.Length);

        s.Edit(scenes.StartScene, "chapter 11: Lyra, the radio, the dissident, the hide-out's Mosquibee, three lava slabs, the reveal and the ferryman's boat to Island CX", model =>
        {
            var edits = new List<Action<SceneScripts>>();
            var lyra = s.MarshalIn(model);
            var dissidentAt = s.Near(model, 4, 0) ?? throw new NotTheGames("no room by the start line for the dissident");
            var mosquibeeAt = s.Near(model, 7, 2.5) ?? throw new NotTheGames("no room by the start line for the Mosquibee");
            var ferryAt = s.Near(model, 10, 5) ?? throw new NotTheGames("no room by the start line for the ferryman");
            var slabs = s.Spots(model, 3, 14, 22, 30, 38, 46, 54, -14, -22, -30, -40);
            if (slabs.Count < 3) throw new NotTheGames("no room for three slabs beside the road in the start scene");
            Place(dissident, model, dissidentAt); Place(mosquibee, model, mosquibeeAt); Place(ferryman, model, ferryAt);
            var dIndex = model.Actors.Count; var mIndex = dIndex + 1;
            edits.Add(s.Add(model, dissident, Talker(when, $@"            if ({SlabsWon} <= var_game({QSlabs}))
            {{
                message({Id(VLine.Reveal)});
            }}
            else
            {{
                message({Id(VLine.Ask)});
                if ({SlabsAsked} > var_game({QSlabs}))
                {{
                    set_var_game({QSlabs}, {SlabsAsked});
                }}
            }}")));
            edits.Add(s.Add(model, mosquibee, Talker(when, $@"            if (6 == var_game({QSlabs}))
            {{
                message({Id(VLine.Sealed)});
                set_var_game({QSlabs}, {SlabsSealed});
            }}
            else if ({SlabsSealed} <= var_game({QSlabs}))
            {{
                message({Id(VLine.Sealed)});
            }}
            else
            {{
                message({Id(VLine.Slabs)});
                if ({SlabsAsked} <= var_game({QSlabs}) && {SlabsWhere} > var_game({QSlabs}))
                {{
                    set_var_game({QSlabs}, {SlabsWhere});
                }}
            }}")));
            // the slabs, a bit each in var 239: taken when its bit is in it
            var bits = new[] { 1, 2, 4 };
            for (var k = 0; k < 3; k++)
            {
                var bit = bits[k];
                var without = string.Join(" || ", Enumerable.Range(0, 8).Where(v => (v & bit) == 0).Select(v => $"{v} == var_game({LateSlabsFound})"));
                // (seen as the volcano's own standing stone, entity 218's body 0 -- BODY 316 -- once the Mosquibee has told; gone once found)
                var stone = Copy(s.Originals, StoneScene, StoneActor);
                var slab = stone ?? RaceTrackScenes.InvisibleHelper(0, 0, 0, 9);
                if (stone is not null) slab.Body = 0;
                Place(slab, model, (slabs[k].X, slabs[k].Z, slabs[k].Y, 0));
                edits.Add(s.Add(model, slab, Talker(when, $@"            if ({SlabsWhere} <= var_game({QSlabs}) && {SlabsSealed} > var_game({QSlabs}) && ({without}))
            {{
                message_obj(0, {Id(VLine.Slab)});
                add_var_game({LateSlabsFound}, {bit});
                add_var_game({QSlabs}, 1);
                if (6 == var_game({QSlabs}))
                {{
                    message_zoe({Id(VLine.ZoeTakes)});
                }}
            }}", 1100, $@"    if ({SlabsWhere} <= var_game({QSlabs}) && {SlabsSealed} > var_game({QSlabs}) && ({without}))
    {{
        invisible(0);
    }}
    else
    {{
        invisible(1);
    }}
")));
            }
            // the ferryman: from the win on, by the start line; the song and the boat
            edits.Add(s.Add(model, ferryman, $@"void comportement_0()
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
    if ({SlabsWon} > var_game({QSlabs}))
    {{
        invisible(1);
    }}
    else
    {{
        invisible(0);
    }}
    swif (1 == action())
    {{
        if (1500 > distance(0) && {Driving} != comportement_hero() && {SlabsWon} <= var_game({QSlabs}))
        {{
            if ({SlabsReveal} == var_game({QSlabs}))
            {{
                message({Id(VLine.Ferry)});
                found_object({FerrymanSong});
                set_var_game({FerrymanSong}, 1);
                message({Id(VLine.FerryAfter)});
                set_var_game({QSlabs}, {SlabsFerry});
                inc_chapter();
                set_change_cube({FerrySwitch}, 1);
            }}
            else
            {{
                message({Id(VLine.NotYet)});
            }}
        }}
    }}
}}
"));
            // (the boat: a change-cube zone round the ferryman into Island CX's start scene, off until he takes Twinsen)
            if (cxScene >= 0) s.Travel(model, ferryAt, cxScene, cxAt, FerrySwitch);
            else s.Log.Add("chapter 11: no Island CX start scene in the folder: the ferryman's boat goes nowhere");
            // Lyra, the radio and the race's end
            edits.Add(s.Add(model, lyra, Talker(when, $@"            if ({SlabsWon} <= var_game({QSlabs}))
            {{
                message({Id(VLine.Won)});
            }}
            else
            {{
                message({Id(VLine.Arrive)});
            }}", 1800, $@"    if (0 == var_game({QSlabs}) && {Driving} != comportement_hero())
    {{
        set_var_game({QSlabs}, {SlabsTold});
        message({Id(VLine.Arrive)});
    }}
    if ({SlabsWon} == var_game({QSlabs}) && {Driving} != comportement_hero())
    {{
        set_var_game({QSlabs}, {SlabsReveal});
        message({Id(VLine.Won)});
        message_obj({dIndex}, {Id(VLine.Reveal)});
        message_zoe({Id(VLine.ZoeDream)});
        message_obj(0, {Id(VLine.TwinsenKnows)});
        message({Id(VLine.BaldinoFunFrock)});
    }}
")));
            edits.Add(s.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 9), LateStory.RadioCall(VolcanoChapter, Id(VLine.Ring), Id(VLine.Baldino))));
            // the easter egg: the Dissidents' Ring used at the lava's edge (RaceTrackStory.Eggs.Volcano.cs)
            edits.Add(NotThatRing(s, model, Id(VLine.RingZoe), Id(VLine.RingTwinsen)));
            _ = mIndex;
            return scripts => { foreach (var e in edits) e(scripts); };
        });
        // the easter egg: the statue's feet over a crack in the cave's roof (RaceTrackStory.Eggs.Volcano.cs)
        StatueFeet(s);
        s.Save();
        s.Log.Add($"chapter 11: Q_SLABS is variable {QSlabs} (the race's gate at {SlabsSealed}, a win {SlabsWon}), the slabs found {LateSlabsFound}, the radio answered {LateRadioAnswered}");
        return s.Log;
    }

    // ---- group C's shared parts (chapters 11-14) -----------------------------------------------------------------------------------
    // One island's story: its texts, its scenes edited once each and saved together, and where its people stand by its start line.
    private sealed class LateStory
    {
        public readonly string Game; public readonly List<string> Log = new();
        public readonly SceneStore Store, Originals;
        public readonly RaceTrackReport Report; public readonly RaceTrackIsland Island; public readonly int StartScene;
        private readonly Dictionary<int, SceneModel> models = new();

        public LateStory(string game, RaceTrackReport report, RaceTrackIsland island, int startScene)
        {
            Game = game; Report = report; Island = island; StartScene = startScene;
            Store = new SceneStore(SceneGame.Lba2, game);
            Originals = RaceTrackService.OriginalScenes(Store);
            givers = new();         // (Citadel Island's search zones: not this island's)
        }

        public bool Ready(string name)
        {
            if (StartScene >= 0 && Report.StartLine.Count > 0) return true;
            Log.Add($"no story on {name}: its track has no start scene or start line");
            return false;
        }

        // the lines at the end of a text file of TEXT.HQR, in every language: the first one's id
        public int Texts(int file, string[][] lines, string name)
        {
            var textPath = Path.Combine(Game, "TEXT.HQR");
            var text = HqrArchive.Open(textPath);
            var languages = Lba2TextBank.Languages(textPath);
            var first = NextId(text, languages, file);
            var hqr = File.ReadAllBytes(textPath);
            for (var lang = 0; lang < languages; lang++)
            {
                var bank = Lba2TextBank.Load(text, lang, file);
                var attribute = bank.Texts.Count > 0 ? bank.Texts[0].Attribute : Lba2TextBank.NormalAttribute;
                AddTexts(bank, first, attribute, lines, lang < lines[0].Length ? lang : 0);
                hqr = bank.WriteInto(hqr);
            }
            File.WriteAllBytes(textPath, hqr);
            Log.Add($"{name}: the story's {lines.Length} lines are texts {first}-{first + lines.Length - 1} of file {file}, in all {languages} languages");
            return first;
        }

        public void Edit(int scene, string what, Func<SceneModel, Action<SceneScripts>> edit)
        {
            try
            {
                var current = models.TryGetValue(scene, out var m) ? m : Store.Load(scene);
                var model = SceneSerializer.Parse(SceneGame.Lba2, SceneSerializer.Write(current));
                var scriptEdit = edit(model);
                var scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
                scriptEdit(scripts);
                var built = scripts.Build();
                if (!built.Ok) { Log.Add($"scene {scene}: {what}: the scripts would not compile: {string.Join("; ", built.Errors)}"); return; }
                models[scene] = SceneSerializer.Parse(SceneGame.Lba2, built.Record!);
                Log.Add($"scene {scene}: {what}");
            }
            catch (Exception e) when (e is NotTheGames or ScriptCompileException or InvalidDataException or ArgumentException or SceneEditException)
            {
                Log.Add($"scene {scene}: {what} left out: {e.Message}");
            }
        }

        public Action<SceneScripts> Add(SceneModel model, SceneActorModel actor, string life)
        {
            actor.Life = new byte[] { 0 }; actor.Track = new byte[] { 0 };
            var index = SceneOps.AddActor(model, actor);
            return scripts =>
            {
                scripts.SetText(index, ScriptKind.Life, life);
                scripts.SetText(index, ScriptKind.Track, "label(0);\nstop();\n");
            };
        }

        public void Save() => Store.SaveMany(models.Select(m => new SceneChange(m.Key, m.Value, null)).ToList(), allowErrors: true);

        private static bool InCube(SceneModel model, double x, double z) => (int)Math.Floor(x / 64) == model.CubeX && (int)Math.Floor(z / 64) == model.CubeY;

        // beside the start line, `back` cells before it, either side, or else the nearest ground off the roads -- in the scene's cube
        // (`level`: only ground within that height of the start line's -- not a statue's hand over it)
        public (double X, double Z, double Y, int Beta)? Near(SceneModel model, double back, double aside, double level = double.MaxValue)
        {
            var s = Report.StartLine[0];
            foreach (var side in new[] { 1, -1 })
                if (Beside(Report, s, back, 1 + Math.Abs(aside) * 0.5, new[] { side }) is { } b && InCube(model, b.X, b.Z)) return b;
            double x = s.X - s.DirX * back - s.DirZ * aside, z = s.Z - s.DirZ * back + s.DirX * aside;
            return OffRoad(Report, x, z, 1, 24) is { } o && Math.Abs(o.Y - s.Y) <= level && Facing(o, s.X, s.Z) is var f && InCube(model, f.X, f.Z) ? f : null;
        }

        // up to `count` places beside the road, `backs` cells before the start line along it (after it, less than 0), in the scene's cube
        public List<(double X, double Z, double Y)> Spots(SceneModel model, int count, params double[] backs)
        {
            var spots = new List<(double X, double Z, double Y)>();
            var s = Report.StartLine[0];
            foreach (var back in backs)
            {
                if (spots.Count == count) break;
                foreach (var side in new[] { -1, 1 })
                    if (Beside(Report, s, back, 1.5, new[] { side }) is { } b && InCube(model, b.X, b.Z) && spots.All(p => Math.Abs(p.X - b.X) + Math.Abs(p.Z - b.Z) > 6))
                    { spots.Add((b.X, b.Z, b.Y)); break; }
            }
            // (no room right beside the road -- a raised one -- the nearest ground off it, either side, in the scene's cube)
            foreach (var back in backs)
                foreach (var aside in new[] { 8.0, -8.0, 14.0, -14.0 })
                {
                    if (spots.Count == count) return spots;
                    double x = s.X - s.DirX * back - s.DirZ * aside, z = s.Z - s.DirZ * back + s.DirX * aside;
                    if (OffRoad(Report, x, z, 1.5, 6) is { } o && InCube(model, o.X, o.Z) && spots.All(p => Math.Abs(p.X - o.X) + Math.Abs(p.Z - o.Z) > 6)) spots.Add(o);
                }
            return spots;
        }

        // the marshal's place (the race-track mode draws her there: marshal=) as an invisible talker, or beside the start line
        public SceneActorModel MarshalIn(SceneModel model)
        {
            if (Report.Marshal is { Length: >= 10 } m && InCube(model, m[6] / 512.0, m[7] / 512.0))
                return RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(m[6] / 512.0, model.CubeX), m[8], RaceTrackScenes.CubeLocal(m[7] / 512.0, model.CubeY), 9);
            var at = Near(model, 2, 2) ?? throw new NotTheGames("no room by the start line for the marshal");
            return RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(at.X, model.CubeX), (int)Math.Round(at.Y), RaceTrackScenes.CubeLocal(at.Z, model.CubeY), 9);
        }

        // a place `back` cells before Twinsen's start in the scene (along the start line's way) and `aside` to its side, on his height
        public (double X, double Z, double Y, int Beta) ByHero(SceneModel model, double back, double aside)
        {
            var s = Report.StartLine[0]; var h = model.Hero;
            double hx = model.CubeX * 64 + h.X / 512.0, hz = model.CubeY * 64 + h.Z / 512.0;
            double x = Math.Clamp(hx - s.DirX * back - s.DirZ * aside, model.CubeX * 64 + 1, model.CubeX * 64 + 63);
            double z = Math.Clamp(hz - s.DirZ * back + s.DirX * aside, model.CubeY * 64 + 1, model.CubeY * 64 + 63);
            return (x, z, h.Y, RaceTrackMath.Turn(Math.Atan2(hx - x, hz - z)));
        }

        // a scene's start (Twinsen's place in it) in this folder: (-1, ...) when it isn't there
        public (int Scene, (int X, int Y, int Z) At) Landing(int scene)
        {
            if (!Store.SceneExists(scene)) return (-1, default);
            var hero = Store.Load(scene).Hero;
            return (scene, (hero.X, hero.Y, hero.Z));
        }

        // A change-cube zone round a place (island cells) into `scene` at `at`, off until set_change_cube(`zoneSwitch`, 1): the way the
        // story travels to a scene past 255, which a script's change_cube (a byte) can't name.
        public void Travel(SceneModel model, (double X, double Z, double Y, int Beta) round, int scene, (int X, int Y, int Z) at, int zoneSwitch)
        {
            const int Reach = 1536;
            int x = RaceTrackScenes.CubeLocal(round.X, model.CubeX), z = RaceTrackScenes.CubeLocal(round.Z, model.CubeY), y = (int)Math.Round(round.Y);
            static int Clamp(int v) => Math.Clamp(v, 0, IslandFile.CubeSize - 1);
            model.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = scene, X0 = Clamp(x - Reach), X1 = Clamp(x + Reach), Z0 = Clamp(z - Reach), Z1 = Clamp(z + Reach), Y0 = y - 1000, Y1 = y + 3000,
                // (Twinsen comes out where he stood in the zone, from its corner: so the zone's corner is put that far before the place)
                Info = new[] { at.X - (x - Clamp(x - Reach)), at.Y + 200, at.Z - (z - Clamp(z - Reach)), 0, zoneSwitch, 0, 0, 0 },
            });
        }

        // Baldino's call on arrival: with the radio, "the radio is ringing" once a visit until it is answered from the inventory; then his
        // line (`call`), once a chapter (var 238 the chapter answered)
        public static string RadioCall(int chapter, int ring, int call, string? here = null) => RadioScript(here ?? $"{chapter} == chapter()", chapter, ring, call);

        private static string RadioScript(string here, int chapter, int ring, int call) => $@"void comportement_0()
{{
    if ({here} && 0 < var_game({Radio}))
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
    if ({chapter} > var_game({LateRadioAnswered}) && 0 == var_cube({Once2}) && {Driving} != comportement_hero())
    {{
        set_var_cube({Once2}, 1);
        message({ring});
    }}
    if ({chapter} > var_game({LateRadioAnswered}) && 1 == use_inventory({Radio}))
    {{
        set_var_game({LateRadioAnswered}, {chapter});
        message({call});
    }}
}}
";
    }
}
