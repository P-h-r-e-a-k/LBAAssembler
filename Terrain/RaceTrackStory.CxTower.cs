using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The race mod's story, chapter 12: Island CX (docs/LBRA_WALKTHROUGH.md; docs/LBRA_STORY_SCRIPT.md), on Island CX Mk2's track
// (RaceTrackIsland.CxTowerMk2: scenes 340-343, the race from 343; or Island CX's own, scene 300, if that is the one built). Everyone is by
// the start line, in the start scene, while chapter() is 12:
//   - Lyra, the marshal: on arrival the door to the halls is locked and the soldiers lost the key (Q_CXKEY 1).
//   - Baldino's call (LateStory.RadioCall): the car painted grey.
//   - A Franco trooper (the Emerald Moon's Franco guard, scene 23's entity 57, copied): the survivor on the raft has it (Q_CXKEY 2).
//   - The survivor (the Franco who came back from Island CX: the souvenir seller of Celebration Island, scene 95's entity 213, copied):
//     the key is his; with the Can of GazoGem (item 15, from the Francos' island) he trades: the can gone, the Key for the passage to CX
//     (item 35) found, Zoe's word (Q_CXKEY 3: the race's gate opens, gate=223 3 -- RaceCarEngineFile).
//   - The race won (win=223 4): Lyra's word, then the note on the bench beside her (an invisible pickup): FunFrock's note, Lyra's "the
//     palace!", inc_chapter (13) and the soldiers' transport to the palace -- a change-cube zone round the note into the palace race's
//     start scene (past 255: LateStory.Travel), switched on (Q_CXKEY 5).
// Not built: the survivor's raft (he stands on the shore), the note in the halls' workshop (it lies by the start line), Zoe's hint when
// Twinsen has no can, the slate's page.
internal static partial class RaceTrackStory
{
    // Q_CXKEY: 1 Lyra has told, 2 the trooper has told, 3 the key traded (the gate), 4 won, 5 the note read
    public const int QCxKey = 223, KeyTold = 1, KeyTrooper = 2, KeyTraded = 3, KeyWon = 4, KeyNote = 5;
    private const int CxChapter = 12, CxTexts = 3 + 9, GazoGem = 15, PassageKey = 35, TransportSwitch = 216;
    // the game's Franco guard (the Emerald Moon's scene 23, entity 57) and the survivor (Celebration Island's scene 95, entity 213)
    private const int MoonGuards = 23, FrancoGuard = 57;

    private enum CLine { Arrive, Ring, Baldino, Trooper, Survivor, Traded, ZoeEvidence, Won, Note, Palace }
    private static readonly string[][] CxLines =
    {
        new[]
        {
            "Island CX. The race goes down through that door into the halls, and the door is locked. The soldiers lost the key.",
            "L'îlot CX. La course descend par cette porte dans les salles, et la porte est fermée à clé. Les soldats ont perdu la clé.",
            "Das Eiland CX. Das Rennen führt durch diese Tür hinunter in die Hallen, und die Tür ist verschlossen. Die Soldaten haben den Schlüssel verloren.",
            "El islote CX. La carrera baja por esa puerta hasta las salas, y la puerta está cerrada con llave. Los soldados perdieron la llave.",
            "L'isolotto CX. La corsa scende da quella porta nelle sale, e la porta è chiusa a chiave. I soldati hanno perso la chiave.",
            "A Ilhota CX. A corrida desce por aquela porta até às salas, e a porta está trancada. Os soldados perderam a chave.",
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
            "Baldino: Cannons and gas monsters! I've painted the car grey so the gunners don't see you. That's not a joke, it's science. Mostly.",
            "Baldino : Des canons et des monstres à gaz ! J'ai peint la voiture en gris pour que les artilleurs ne te voient pas. Ce n'est pas une blague, c'est de la science. En gros.",
            "Baldino: Kanonen und Gasmonster! Ich habe den Wagen grau lackiert, damit die Kanoniere dich nicht sehen. Das ist kein Witz, das ist Wissenschaft. Größtenteils.",
            "Baldino: ¡Cañones y monstruos de gas! He pintado el coche de gris para que los artilleros no te vean. No es una broma, es ciencia. Más o menos.",
            "Baldino: Cannoni e mostri di gas! Ho dipinto la macchina di grigio così gli artiglieri non ti vedono. Non è uno scherzo, è scienza. Più o meno.",
            "Baldino: Canhões e monstros de gás! Pintei o carro de cinzento para os artilheiros não te verem. Não é piada, é ciência. Quase.",
        },
        new[]
        {
            "Lost it? The survivor on the raft has it. Won't give it back, either. Says his motor's out of fuel.",
            "Perdue ? C'est le rescapé du radeau qui l'a. Et il ne veut pas la rendre. Il dit que son moteur n'a plus de carburant.",
            "Verloren? Der Überlebende auf dem Floß hat ihn. Und er gibt ihn nicht zurück. Er sagt, sein Motor hat keinen Treibstoff mehr.",
            "¿Perdida? La tiene el superviviente de la balsa. Y no quiere devolverla. Dice que su motor se ha quedado sin combustible.",
            "Persa? Ce l'ha il superstite sulla zattera. E non vuole restituirla. Dice che il suo motore è senza carburante.",
            "Perdida? Quem a tem é o sobrevivente da jangada. E não a quer devolver. Diz que o motor dele ficou sem combustível.",
        },
        new[]
        {
            "The Key for the passage? Mine. Fair and square. But if you had some fuel for my raft...",
            "La clé du passage ? À moi. Trouvée à la régulière. Mais si tu avais du carburant pour mon radeau...",
            "Der Schlüssel für den Durchgang? Meiner. Ehrlich gefunden. Aber wenn du Treibstoff für mein Floß hättest...",
            "¿La llave del pasaje? Mía. Limpiamente. Pero si tuvieras algo de combustible para mi balsa...",
            "La chiave del passaggio? Mia. Trovata lealmente. Ma se avessi del carburante per la mia zattera...",
            "A chave da passagem? É minha. Com toda a justiça. Mas se tivesses combustível para a minha jangada...",
        },
        new[]
        {
            "Gazogem! The good stuff! Here, take the key.",
            "Du Gazogem ! Du bon ! Tiens, prends la clé.",
            "Gazogem! Das gute Zeug! Hier, nimm den Schlüssel.",
            "¡Gazogem! ¡Del bueno! Toma, llévate la llave.",
            "Gazogem! Roba buona! Ecco, prendi la chiave.",
            "Gazogem! Do bom! Toma, leva a chave.",
        },
        new[]
        {
            "So much for evidence.",
            "Adieu la pièce à conviction.",
            "So viel zum Beweisstück.",
            "Adiós a la prueba.",
            "Addio prova.",
            "Lá se foi a prova.",
        },
        new[]
        {
            "Island CX is yours! And look, there's a note on the bench there.",
            "L'îlot CX est à toi ! Et regarde, il y a un mot sur le banc là-bas.",
            "Das Eiland CX gehört dir! Und sieh mal, da liegt ein Zettel auf der Bank.",
            "¡El islote CX es tuyo! Y mira, hay una nota en el banco de ahí.",
            "L'isolotto CX è tuo! E guarda, c'è un biglietto sulla panca laggiù.",
            "A Ilhota CX é tua! E olha, há um bilhete no banco ali.",
        },
        new[]
        {
            "\"To the palace. The Emperor will lend me his teleporter, or he will lose his throne. F.\"",
            "« Au palais. L'Empereur me prêtera son téléporteur, ou il perdra son trône. F. »",
            "„Zum Palast. Der Kaiser leiht mir seinen Teleporter, oder er verliert seinen Thron. F.“",
            "«Al palacio. El Emperador me prestará su teletransportador, o perderá su trono. F.»",
            "«Al palazzo. L'Imperatore mi presterà il suo teletrasporto, o perderà il trono. F.»",
            "«Para o palácio. O Imperador empresta-me o teletransportador, ou perde o trono. F.»",
        },
        new[]
        {
            "The palace! The soldiers' transport leaves now, and we're on it.",
            "Le palais ! Le transport des soldats part maintenant, et nous sommes dedans.",
            "Der Palast! Der Transporter der Soldaten fährt jetzt ab, und wir sind dabei.",
            "¡El palacio! El transporte de los soldados sale ahora, y vamos en él.",
            "Il palazzo! Il trasporto dei soldati parte adesso, e noi ci siamo sopra.",
            "O palácio! O transporte dos soldados parte agora, e nós vamos nele.",
        },
    };

    public static List<string> ApplyCx(string gameDirectory, RaceTrackReport report, RaceTrackIsland island, RaceTrackScenes.Result scenes)
    {
        var s = new LateStory(gameDirectory, report, island, scenes.StartScene);
        if (!s.Ready("Island CX")) return s.Log;
        var first = s.Texts(CxTexts, CxLines, "Island CX");
        int Id(CLine line) => first + (int)line;
        var when = $"{CxChapter} == chapter()";
        var trooper = CopyOf(s.Originals, MoonGuards, FrancoGuard);
        var survivor = CopyOf(s.Originals, CelebrationScene, SellerEntity);
        if (trooper is null || survivor is null) { s.Log.Add("chapter 12 left out: the game's Franco guard (scene 23) or the survivor (scene 95) not found"); return s.Log; }
        var (palaceScene, palaceAt) = s.Landing(LBAAssembler.Terrain.Palace.PalaceTrack.FirstScene + StartCubeOfPalace());

        s.Edit(scenes.StartScene, "chapter 12: Lyra, the radio, the trooper, the survivor's key for the gazogem, the note and the transport to the palace", model =>
        {
            var edits = new List<Action<SceneScripts>>();
            var lyra = s.MarshalIn(model);
            var trooperAt = s.Near(model, 4, 0) ?? throw new NotTheGames("no room by the start line for the trooper");
            var survivorAt = s.Near(model, 8, 3) ?? throw new NotTheGames("no room by the start line for the survivor");
            var noteAt = s.Near(model, 2, 4) ?? throw new NotTheGames("no room by the start line for the note");
            Place(trooper, model, trooperAt); Place(survivor, model, survivorAt);
            edits.Add(s.Add(model, trooper, Talker(when, $@"            message({Id(CLine.Trooper)});
            if ({KeyTrooper} > var_game({QCxKey}))
            {{
                set_var_game({QCxKey}, {KeyTrooper});
            }}")));
            edits.Add(s.Add(model, survivor, Talker(when, $@"            if ({KeyTraded} <= var_game({QCxKey}))
            {{
                message({Id(CLine.Traded)});
            }}
            else
            {{
                message({Id(CLine.Survivor)});
                if (0 < var_game({GazoGem}))
                {{
                    message({Id(CLine.Traded)});
                    message_zoe({Id(CLine.ZoeEvidence)});
                    set_var_game({GazoGem}, 0);
                    found_object({PassageKey});
                    set_var_game({PassageKey}, 1);
                    set_var_game({QCxKey}, {KeyTraded});
                }}
            }}")));
            var note = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(noteAt.X, model.CubeX), (int)Math.Round(noteAt.Y), RaceTrackScenes.CubeLocal(noteAt.Z, model.CubeY), 9);
            var lyraIndex = model.Actors.Count + 1;      // (the note first, then Lyra)
            edits.Add(s.Add(model, note, Talker(when, $@"            if ({KeyWon} <= var_game({QCxKey}))
            {{
                message_obj(0, {Id(CLine.Note)});
                message_obj({lyraIndex}, {Id(CLine.Palace)});
                set_var_game({QCxKey}, {KeyNote});
                inc_chapter();
                set_change_cube({TransportSwitch}, 1);
            }}", 1100)));
            if (palaceScene >= 0) s.Travel(model, noteAt, palaceScene, palaceAt, TransportSwitch);
            else s.Log.Add("chapter 12: no palace race scene in the folder: the soldiers' transport goes nowhere");
            edits.Add(s.Add(model, lyra, Talker(when, $@"            if ({KeyWon} <= var_game({QCxKey}))
            {{
                message({Id(CLine.Won)});
            }}
            else
            {{
                message({Id(CLine.Arrive)});
            }}", 1800, $@"    if (0 == var_game({QCxKey}) && {Driving} != comportement_hero())
    {{
        set_var_game({QCxKey}, {KeyTold});
        message({Id(CLine.Arrive)});
    }}
    if ({KeyWon} == var_game({QCxKey}) && 0 == var_cube({Once1}) && {Driving} != comportement_hero())
    {{
        set_var_cube({Once1}, 1);
        message({Id(CLine.Won)});
    }}
")));
            edits.Add(s.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 9), LateStory.RadioCall(CxChapter, Id(CLine.Ring), Id(CLine.Baldino))));
            return scripts => { foreach (var e in edits) e(scripts); };
        });
        s.Save();
        s.Log.Add($"chapter 12: Q_CXKEY is variable {QCxKey} (the race's gate at {KeyTraded}, a win {KeyWon}); the gazogem (item {GazoGem}) traded for the key (item {PassageKey})");
        return s.Log;
    }

    // (the palace race's start scene: the folder's built track says, else the start cube's place in its scene list -- PalaceTrack)
    private static int StartCubeOfPalace() => Math.Max(0, Array.IndexOf(LBAAssembler.Terrain.Palace.PalaceTrack.SceneCubes, (8, 11)));
}
