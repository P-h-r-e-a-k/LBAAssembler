using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;
using LBAAssembler.Terrain.Palace;

namespace LBAAssembler.Terrain;

// The race mod's story, chapter 13: the Emperor's palace (docs/LBRA_WALKTHROUGH.md; docs/LBRA_STORY_SCRIPT.md), on the palace race's own
// scenes (RaceTrackIsland.Palace: OwnScenes 265 on, island byte 4 -- Otringal's and the Temple of Bú's too: only the race's start scene is
// edited, and its texts go at the end of Otringal's file, 7). Everyone is in the start scene, by the start, while chapter() is 13:
//   - Lyra: FunFrock has taken the palace, the gate sealed with the Emperor's seal (Q_EMPEROR 1); with the four Fragments (items 31-34)
//     it knows them, and opens (2).
//   - Baldino's call (LateStory.RadioCall): marble floors.
//   - The Empress (the game's, entity 331: BODY 454): her husband locked in his bedroom, the lock answers to a song (3).
//   - The Emperor's bedroom door (an invisible talker): with the Ferryman's Song (item 18) Twinsen sings, Zoe's word, it opens (4).
//   - The Emperor (the game's, entity 330: BODY 453), there from then on: his teleporter past the round window, the Emperor's Sword
//     (item 10; 5).
//   - Stan (the game's, scene 158's actor 6: BODY 439): race me to the window (6: the race's gate, gate=224 6 -- RaceCarEngineFile).
//   - The race: the palace's sprint (built); a win sets var 79 (the statue risen, built) and the teleporter takes the car to Celebration
//     Island, where the final's arrival says FunFrock's voice over the flight and moves the chapter on (RaceTrackStory.Final.cs).
// Not built: the rooms (the music room, the bedroom, the library: everyone stands by the start, in the south-east room), the pighead
// guards' walk-ons (the race's own guards are), the slate's page.
internal static partial class RaceTrackStory
{
    // Q_EMPEROR: 1 Lyra has told, 2 the gate open, 3 the Empress has told, 4 the bedroom open, 5 the sword given, 6 Stan's race (the gate)
    public const int QEmperor = 224, GateTold = 1, GateOpen = 2, EmpressTold = 3, BedroomOpen = 4, SwordGiven = 5, StanReady = 6;
    private const int PalaceChapter = 13, PalaceTexts = 3 + 4, Sword = 10;
    // the game's: Stan (Otringal's scene 158, actor 6: entity 316), the Emperor and the Empress (entities 330 and 331, generic body 0; their
    // standing animations' generic numbers -- an actor's animation is its entity's generic, not an ANIM.HQR entry: the Emperor's 0, the
    // Empress's made for her, PalaceEmpress)
    private const int StanScene = 158, StanActor = 6, EmperorEntity = 330, EmperorStand = 0, EmpressEntity = PalaceEmpress.Entity, EmpressStand = PalaceEmpress.Stand;

    private enum PLine { Arrive, Fragments, NoFragments, Ring, Baldino, Empress, Sing, ZoeWorse, Locked, Emperor, Stan }
    private static readonly string[][] PalaceLines =
    {
        new[]
        {
            "FunFrock has taken the palace! The gate's sealed with the Emperor's seal...",
            "FunFrock a pris le palais ! La porte est scellée du sceau de l'Empereur...",
            "FunFrock hat den Palast eingenommen! Das Tor ist mit dem Siegel des Kaisers versiegelt...",
            "¡FunFrock ha tomado el palacio! La puerta está sellada con el sello del Emperador...",
            "FunFrock ha preso il palazzo! Il portone è sigillato con il sigillo dell'Imperatore...",
            "O FunFrock tomou o palácio! O portão está selado com o selo do Imperador...",
        },
        new[]
        {
            "...and it knows the Fragments of the four peoples when it sees them.",
            "... et il reconnaît les Fragments des quatre peuples quand il les voit.",
            "... und es erkennt die Fragmente der vier Völker, wenn es sie sieht.",
            "... y reconoce los Fragmentos de los cuatro pueblos cuando los ve.",
            "... e riconosce i Frammenti dei quattro popoli quando li vede.",
            "... e reconhece os Fragmentos dos quatro povos quando os vê.",
        },
        new[]
        {
            "Only the Fragments of the four peoples will open it: the Sups', the Francos', the Wannies' and the Mosquibees'.",
            "Seuls les Fragments des quatre peuples l'ouvriront : celui des Sups, des Francos, des Wannies et des Mosquibees.",
            "Nur die Fragmente der vier Völker öffnen es: das der Sups, der Francos, der Wannies und der Mosquibees.",
            "Solo los Fragmentos de los cuatro pueblos la abrirán: el de los Sups, el de los Francos, el de los Wannies y el de los Mosquibees.",
            "Solo i Frammenti dei quattro popoli lo apriranno: quello dei Sup, dei Franco, dei Wannie e dei Mosquibee.",
            "Só os Fragmentos dos quatro povos o abrem: o dos Sups, o dos Francos, o dos Wannies e o dos Mosquibees.",
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
            "Baldino: Indoors! Marble floors, so soft tyres. And Twinsen, don't break anything, those vases are older than me.",
            "Baldino : À l'intérieur ! Des sols en marbre, donc des pneus tendres. Et Twinsen, ne casse rien, ces vases sont plus vieux que moi.",
            "Baldino: Drinnen! Marmorböden, also weiche Reifen. Und Twinsen, mach nichts kaputt, diese Vasen sind älter als ich.",
            "Baldino: ¡Bajo techo! Suelos de mármol, así que neumáticos blandos. Y Twinsen, no rompas nada, esos jarrones son más viejos que yo.",
            "Baldino: Al chiuso! Pavimenti di marmo, quindi gomme morbide. E Twinsen, non rompere niente, quei vasi sono più vecchi di me.",
            "Baldino: Lá dentro! Chão de mármore, por isso pneus macios. E Twinsen, não partas nada, esses vasos são mais velhos do que eu.",
        },
        new[]
        {
            "My husband! FunFrock's men have locked him in his bedroom. The lock answers to a song, but I can't sing.",
            "Mon mari ! Les hommes de FunFrock l'ont enfermé dans sa chambre. La serrure obéit à une chanson, mais je ne sais pas chanter.",
            "Mein Gemahl! FunFrocks Männer haben ihn in seinem Schlafgemach eingesperrt. Das Schloss gehorcht einem Lied, aber ich kann nicht singen.",
            "¡Mi esposo! Los hombres de FunFrock lo han encerrado en su dormitorio. La cerradura responde a una canción, pero yo no sé cantar.",
            "Mio marito! Gli uomini di FunFrock lo hanno chiuso nella sua camera. La serratura risponde a una canzone, ma io non so cantare.",
            "O meu marido! Os homens do FunFrock fecharam-no no quarto. A fechadura obedece a uma canção, mas eu não sei cantar.",
        },
        new[]
        {
            "Twinsen sings the Ferryman's Song. It's... not good. The door opens anyway.",
            "Twinsen chante la Chanson du passeur. C'est... pas terrible. La porte s'ouvre quand même.",
            "Twinsen singt das Lied des Fährmanns. Es ist... nicht gut. Die Tür geht trotzdem auf.",
            "Twinsen canta la Canción del barquero. Es... no muy buena. La puerta se abre de todos modos.",
            "Twinsen canta la Canzone del traghettatore. È... non granché. La porta si apre lo stesso.",
            "Twinsen canta a Canção do barqueiro. É... não muito boa. A porta abre-se na mesma.",
        },
        new[]
        {
            "I've heard worse. Not much worse.",
            "J'ai entendu pire. Pas beaucoup pire.",
            "Ich habe Schlimmeres gehört. Nicht viel Schlimmeres.",
            "He oído cosas peores. No mucho peores.",
            "Ho sentito di peggio. Non molto peggio.",
            "Já ouvi pior. Não muito pior.",
        },
        new[]
        {
            "The Emperor's bedroom. Locked, and no keyhole: the lock answers to a song.",
            "La chambre de l'Empereur. Fermée, et pas de trou de serrure : la serrure obéit à une chanson.",
            "Das Schlafgemach des Kaisers. Verschlossen, und kein Schlüsselloch: Das Schloss gehorcht einem Lied.",
            "El dormitorio del Emperador. Cerrado, y sin cerradura a la vista: responde a una canción.",
            "La camera dell'Imperatore. Chiusa, e senza buco della serratura: la serratura risponde a una canzone.",
            "O quarto do Imperador. Fechado, e sem buraco de fechadura: a fechadura obedece a uma canção.",
        },
        new[]
        {
            "Twinsen of Twinsun! FunFrock has my teleporter, past the round window at the end of the long hall. If we get there first, it's ours. Take this: you've earned it.",
            "Twinsen de Twinsun ! FunFrock a mon téléporteur, derrière la fenêtre ronde au bout de la grande salle. Si nous y arrivons les premiers, il est à nous. Prends ceci : tu l'as mérité.",
            "Twinsen von Twinsun! FunFrock hat meinen Teleporter, hinter dem runden Fenster am Ende der langen Halle. Wenn wir zuerst dort sind, gehört er uns. Nimm das: Du hast es dir verdient.",
            "¡Twinsen de Twinsun! FunFrock tiene mi teletransportador, tras la ventana redonda al final del gran salón. Si llegamos primero, es nuestro. Toma esto: te lo has ganado.",
            "Twinsen di Twinsun! FunFrock ha il mio teletrasporto, oltre la finestra rotonda in fondo alla lunga sala. Se arriviamo prima noi, è nostro. Prendi questa: te la sei meritata.",
            "Twinsen de Twinsun! O FunFrock tem o meu teletransportador, para lá da janela redonda ao fundo do grande salão. Se lá chegarmos primeiro, é nosso. Toma isto: mereceste-o.",
        },
        new[]
        {
            "The window? End of the long hall. I've timed that run from this desk more times than I've read these books. Race me there.",
            "La fenêtre ? Au bout de la grande salle. J'ai chronométré ce trajet depuis ce bureau plus souvent que je n'ai lu ces livres. Fais-moi la course.",
            "Das Fenster? Am Ende der langen Halle. Ich habe diese Strecke von diesem Schreibtisch aus öfter gestoppt, als ich diese Bücher gelesen habe. Fahr gegen mich.",
            "¿La ventana? Al final del gran salón. He cronometrado ese recorrido desde este escritorio más veces de las que he leído estos libros. Échame una carrera.",
            "La finestra? In fondo alla lunga sala. Ho cronometrato quel percorso da questa scrivania più volte di quante abbia letto questi libri. Sfidami.",
            "A janela? Ao fundo do grande salão. Já cronometrei esse percurso desta secretária mais vezes do que li estes livros. Corre contra mim.",
        },
    };

    // (the Empress's own animations all have her in bed: she stands with PalaceEmpress's, put on again by her script)
    private static string StandUp(string life) => life.Replace("        set_comportement(comportement_1);", $"        anim({EmpressStand});\n        set_comportement(comportement_1);");

    public static List<string> ApplyPalace(string gameDirectory, RaceTrackReport report, RaceTrackIsland island, RaceTrackScenes.Result scenes)
    {
        var s = new LateStory(gameDirectory, report, island, scenes.StartScene);
        if (!s.Ready("the Emperor's palace")) return s.Log;
        s.Log.AddRange(PalaceEmpress.Install(gameDirectory));
        var first = s.Texts(PalaceTexts, PalaceLines, "the Emperor's palace");
        int Id(PLine line) => first + (int)line;
        var when = $"{PalaceChapter} == chapter()";
        var stan = Copy(s.Originals, StanScene, StanActor);
        if (stan is null) { s.Log.Add("chapter 13 left out: the game's Stan (scene 158) not found"); return s.Log; }
        SceneActorModel Person(int entity, int anim)
        {
            var a = stan.Clone();
            a.Entity = entity; a.Body = 0; a.Anim = anim;
            return a;
        }
        var empress = Person(EmpressEntity, EmpressStand);
        var emperor = Person(EmperorEntity, EmperorStand);
        var fragments = string.Join(" && ", Enumerable.Range(31, 4).Select(f => $"0 < var_game({f})"));

        s.Edit(scenes.StartScene, "chapter 13: Lyra and the sealed gate, the radio, the Empress, the bedroom's song, the Emperor's sword, Stan", model =>
        {
            var edits = new List<Action<SceneScripts>>();
            var lyra = s.MarshalIn(model);
            // (the rooms' floor is the palace's decor, not the island's ground: placed from Twinsen's start, on its floor, off the grid to the side)
            (double X, double Z, double Y, int Beta) At(double back, double aside) => s.ByHero(model, back, aside);
            Place(empress, model, At(2, -8)); Place(stan, model, At(-3, -8));
            var door = At(6, -11);
            Place(emperor, model, At(8, -11));
            edits.Add(s.Add(model, empress, StandUp(Talker(when, $@"            message({Id(PLine.Empress)});
            if ({GateOpen} <= var_game({QEmperor}) && {EmpressTold} > var_game({QEmperor}))
            {{
                set_var_game({QEmperor}, {EmpressTold});
            }}"))));
            var bedroom = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(door.X, model.CubeX), (int)Math.Round(door.Y), RaceTrackScenes.CubeLocal(door.Z, model.CubeY), 9);
            edits.Add(s.Add(model, bedroom, Talker(when, $@"            if ({EmpressTold} == var_game({QEmperor}) && 0 < var_game({FerrymanSong}))
            {{
                message_obj(0, {Id(PLine.Sing)});
                message_zoe({Id(PLine.ZoeWorse)});
                set_var_game({QEmperor}, {BedroomOpen});
            }}
            else if ({BedroomOpen} > var_game({QEmperor}))
            {{
                message_obj(0, {Id(PLine.Locked)});
            }}", 1100)));
            edits.Add(s.Add(model, emperor, $@"void comportement_0()
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
    if ({BedroomOpen} > var_game({QEmperor}))
    {{
        invisible(1);
    }}
    else
    {{
        invisible(0);
    }}
    swif (1 == action())
    {{
        if (1500 > distance(0) && {Driving} != comportement_hero() && {BedroomOpen} <= var_game({QEmperor}))
        {{
            message({Id(PLine.Emperor)});
            if ({SwordGiven} > var_game({QEmperor}))
            {{
                found_object({Sword});
                set_var_game({Sword}, 1);
                set_var_game({QEmperor}, {SwordGiven});
            }}
        }}
    }}
}}
"));
            edits.Add(s.Add(model, stan, Talker(when, $@"            message({Id(PLine.Stan)});
            if ({SwordGiven} == var_game({QEmperor}))
            {{
                set_var_game({QEmperor}, {StanReady});
            }}")));
            edits.Add(s.Add(model, lyra, Talker(when, $@"            if ({GateOpen} > var_game({QEmperor}))
            {{
                message({Id(PLine.Arrive)});
                if ({fragments})
                {{
                    message({Id(PLine.Fragments)});
                    set_var_game({QEmperor}, {GateOpen});
                }}
                else
                {{
                    message({Id(PLine.NoFragments)});
                }}
            }}
            else
            {{
                message({Id(PLine.Fragments)});
            }}", 1800, $@"    if (0 == var_game({QEmperor}) && {Driving} != comportement_hero())
    {{
        set_var_game({QEmperor}, {GateTold});
        message({Id(PLine.Arrive)});
        if ({fragments})
        {{
            message({Id(PLine.Fragments)});
            set_var_game({QEmperor}, {GateOpen});
        }}
        else
        {{
            message({Id(PLine.NoFragments)});
        }}
    }}
")));
            edits.Add(s.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 9), LateStory.RadioCall(PalaceChapter, Id(PLine.Ring), Id(PLine.Baldino))));
            return scripts => { foreach (var e in edits) e(scripts); };
        });
        // (the dungeon's easter eggs: the Brune sisters and the ninth portrait -- RaceTrackStory.Eggs.Palace)
        PalaceDungeonEggs(s);
        s.Save();
        s.Log.Add($"chapter 13: Q_EMPEROR is variable {QEmperor} (the race's gate at {StanReady}); the four Fragments (items 31-34) open the gate, the Ferryman's Song (item {FerrymanSong}) the bedroom, the Emperor's Sword is item {Sword}");
        return s.Log;
    }
}
