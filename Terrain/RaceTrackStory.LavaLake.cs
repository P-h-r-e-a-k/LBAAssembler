using System.IO;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Chapter 6 of the story (docs/LBRA_WALKTHROUGH.md), the lava lake (Celebration Island before the statue, scene 95; its race in scene
// 223), round the souvenir seller's race that RaceTrackStory.ApplyCelebration builds:
//   - arrival: Lyra's word on the seller (Q_STALL 1), then Baldino's radio (the shared ring and answer, GroupB.Arrival);
//   - the seller (ApplyCelebration's script): his stall wrecked by the kangaroo, four statuettes to find (Q_STALL 2); with all four (6) his
//     challenge, "all four!" (7), and the lava lake's race;
//   - the four Dark Monk statuettes beside the lap, found in turn (Q_STALL 3-6), Zoe's word at the fourth;
//   - won (ApplyCelebration's after-race controller): what he saw on Island CX (the game's 171/172, retold: IslandRetell), Zoe, Twinsen,
//     Lyra, inc_chapter (7) and the ship to the Francos (scene 257).
// Group B's islands (chapters 6, 8, 9, 10) share: var 236, the chapter whose radio call has been answered; var 237, a group B race just
// finished (finish=, RaceCarEngineFile); a race won moves the chapter on (win=253, the next chapter).
internal static partial class RaceTrackStory
{
    // Q_STALL: 1 Lyra's word heard, 2 the seller told of his stall, 3-6 the statuettes found (one to four), 7 his "all four"
    public const int QStall = 218, StallTold = 2, AllFound = 6, StallDone = 7;
    // group B's shared variables (docs above)
    public const int RadioAnswered = 236, RaceDoneB = 237;
    private const int LavaChapter = 6, FrancosStart = 257, FrancosDock = 109, ShipSwitch = 236;

    // The lava lake's new lines, after the seller's own (SellerLines) at the end of Celebration Island's texts (file 8)
    internal enum LavaLine { LyraArrival, Ring, Baldino, Stall, Statuette, ZoeFour, AllFour, ZoeGlasses, TwinsenDream, LyraShip, Kangaroo }
    internal static readonly string[][] LavaLines =
    {
        new[]
        {
            "Here we are. The seller here won't even race until his stall is put right. You'll see why.",
            "Nous y voilà. Le vendeur d'ici ne veut même pas courir tant que son étal n'est pas remis en état. Tu vas voir pourquoi.",
            "Da wären wir. Der Verkäufer hier fährt kein Rennen, bevor sein Stand nicht wieder in Ordnung ist. Du wirst schon sehen, warum.",
            "Ya estamos. El vendedor de aquí no correrá hasta que su puesto esté arreglado. Ya verás por qué.",
            "Eccoci. Il venditore di qui non correrà finché la sua bancarella non sarà rimessa a posto. Vedrai perché.",
            "Cá estamos. O vendedor daqui nem corre enquanto a banca dele não estiver arrumada. Vais ver porquê.",
        },
        GroupB.Ring,
        new[]
        {
            "Baldino here! The lava lake: hot enough to melt tyres. Heat shields under the car, fitted. Don't stop on the lava. I mean it.",
            "Ici Baldino ! Le lac de lave : assez chaud pour faire fondre les pneus. Boucliers thermiques sous la voiture, posés. Ne t'arrête pas sur la lave. Je ne plaisante pas.",
            "Hier Baldino! Der Lavasee: heiß genug, um Reifen zu schmelzen. Hitzeschilde unter dem Wagen, montiert. Halt nicht auf der Lava an. Ich meine es ernst.",
            "¡Aquí Baldino! El lago de lava: lo bastante caliente para derretir neumáticos. Escudos térmicos bajo el coche, instalados. No te pares sobre la lava. Lo digo en serio.",
            "Qui Baldino! Il lago di lava: abbastanza caldo da sciogliere le gomme. Scudi termici sotto la macchina, montati. Non fermarti sulla lava. Dico sul serio.",
            "Fala o Baldino! O lago de lava: quente que chegue para derreter pneus. Escudos térmicos debaixo do carro, montados. Não pares em cima da lava. Estou a falar a sério.",
        },
        new[]
        {
            "Look at my stall! That kangaroo bounced into it and my souvenirs went everywhere. Four of them. Find them, and maybe I'll race you. Maybe.",
            "Regardez mon étal ! Ce kangourou a rebondi dedans et mes souvenirs sont partis partout. Quatre. Retrouvez-les, et peut-être que je ferai la course avec vous. Peut-être.",
            "Sieh dir meinen Stand an! Dieses Känguru ist hineingesprungen, und meine Souvenirs sind in alle Richtungen geflogen. Vier Stück. Finde sie, und vielleicht fahre ich ein Rennen mit dir. Vielleicht.",
            "¡Mire mi puesto! Ese canguro saltó encima y mis recuerdos salieron volando. Cuatro. Encuéntrelos, y a lo mejor le echo una carrera. A lo mejor.",
            "Guardi la mia bancarella! Quel canguro ci è saltato dentro e i miei souvenir sono volati dappertutto. Quattro. Li trovi, e forse farò una corsa con lei. Forse.",
            "Olhe para a minha banca! Aquele canguru saltou para cima dela e as minhas lembranças voaram para todo o lado. Quatro. Encontre-as, e talvez eu corra consigo. Talvez.",
        },
        new[]
        {
            "A little statue of the Dark Monk. Ugly, but someone will buy it.",
            "Une petite statue de Dark Monk. Laide, mais quelqu'un l'achètera.",
            "Eine kleine Statue von Dark Monk. Hässlich, aber irgendwer wird sie kaufen.",
            "Una estatuilla de Dark Monk. Fea, pero alguien la comprará.",
            "Una piccola statua di Dark Monk. Brutta, ma qualcuno la comprerà.",
            "Uma pequena estátua de Dark Monk. Feia, mas alguém a vai comprar.",
        },
        new[]
        {
            "Every one of these has the statue's face. Who'd want four?",
            "Elles ont toutes le visage de la statue. Qui en voudrait quatre ?",
            "Jede davon hat das Gesicht der Statue. Wer will denn vier davon?",
            "Todas tienen la cara de la estatua. ¿Quién querría cuatro?",
            "Hanno tutte la faccia della statua. Chi ne vorrebbe quattro?",
            "Todas têm a cara da estátua. Quem é que quer quatro?",
        },
        new[]
        {
            "All four! Now, about that race: beat me, and I'll tell you what I saw on Island CX.",
            "Les quatre ! Bon, pour cette course : battez-moi, et je vous dirai ce que j'ai vu sur l'îlot CX.",
            "Alle vier! Also, zu dem Rennen: Schlag mich, und ich erzähle dir, was ich auf dem Eiland CX gesehen habe.",
            "¡Las cuatro! Bueno, sobre esa carrera: gáneme, y le contaré lo que vi en el islote CX.",
            "Tutte e quattro! Allora, per quella corsa: mi batta, e le dirò cosa ho visto sull'isolotto CX.",
            "As quatro! Bem, quanto a essa corrida: vença-me, e conto-lhe o que vi na Ilhota CX.",
        },
        new[]
        {
            "Glasses... Twinsen, the dream. You told me about the dream.",
            "Des lunettes... Twinsen, le rêve. Tu m'as raconté ton rêve.",
            "Eine Brille... Twinsen, der Traum. Du hast mir von dem Traum erzählt.",
            "Gafas... Twinsen, el sueño. Me hablaste del sueño.",
            "Occhiali... Twinsen, il sogno. Mi hai raccontato del sogno.",
            "Óculos... Twinsen, o sonho. Contaste-me o sonho.",
        },
        new[]
        {
            "It was a dream.",
            "C'était un rêve.",
            "Es war ein Traum.",
            "Fue un sueño.",
            "Era un sogno.",
            "Foi um sonho.",
        },
        new[]
        {
            "The Francos' refinery? That's our next stop anyway. The ship leaves now.",
            "La raffinerie des knartas ? C'est notre prochaine étape, de toute façon. Le bateau part maintenant.",
            "Die Raffinerie der Bifiden? Das ist sowieso unser nächster Halt. Das Schiff legt jetzt ab.",
            "¿La refinería de los purlones? Es nuestra próxima parada de todos modos. El barco sale ahora.",
            "La raffineria dei Knartas? È comunque la nostra prossima tappa. La nave parte adesso.",
            "A refinaria dos knartas? É a nossa próxima paragem de qualquer forma. O barco parte agora.",
        },
        new[]
        {
            "Boing! Sorry about the stall. Boing!",
            "Boing ! Désolé pour l'étal. Boing !",
            "Boing! Tut mir leid wegen des Stands. Boing!",
            "¡Boing! Perdón por lo del puesto. ¡Boing!",
            "Boing! Scusa per la bancarella. Boing!",
            "Boing! Desculpa lá a banca. Boing!",
        },
    };
    // the tourist kangaroo of scene 95 (BODY.HQR 308: entity 214), whose own line (185, retold by IslandRetell) waits for after the race
    private const int KangarooEntity = 214, KangarooText = 185;

    // The seller's answer before he is beaten (ApplyCelebration, scene 95): the stall, then with all four statuettes the race. `lava`: the
    // first of LavaLines' ids, `challenge` his own challenge (a rematch), `raceScene` the lava lake's.
    internal static string LavaSeller(int lava, int challenge, int raceScene) => $@"if ({AllFound} > var_game({QStall}))
                {{
                    message(170);
                    message({lava + (int)LavaLine.Stall});
                    if ({StallTold} > var_game({QStall}))
                    {{
                        set_var_game({QStall}, {StallTold});
                    }}
                }}
                else
                {{
                    if ({AllFound} == var_game({QStall}))
                    {{
                        message({lava + (int)LavaLine.AllFour});
                        set_var_game({QStall}, {StallDone});
                    }}
                    else
                    {{
                        message({challenge});
                    }}
                    set_var_cube(0, 0);
                    change_cube({raceScene});
                }}";

    // ... and after the race won, once the seller has told (ApplyCelebration's after-race controller): Zoe, Twinsen, Lyra, the next chapter
    // and the ship to the Francos -- back to scene 95's start, where a door into the Francos' start (scene 257: past what a change_cube
    // can name) opens (LavaLakeBeats); or, with no scene 257 in the folder when this is built (the Island of the Francos built after the
    // lava lake, or not at all), to the Francos' dock where the game's own taxi from Celebration lands (scene 109)
    internal static string LavaWon(int lava, SceneStore store) => $@"message_zoe({lava + (int)LavaLine.ZoeGlasses});
            message_obj(0, {lava + (int)LavaLine.TwinsenDream});
            message({lava + (int)LavaLine.LyraShip});
            if ({LavaChapter} == chapter())
            {{
                inc_chapter();
            }}
            change_cube({(store.SceneExists(FrancosStart) ? CelebrationScene : FrancosDock)});";

    // Scene 95's beats (after ApplyCelebration has saved the seller): Lyra's word and the radio, and the four statuettes. `lava`: the first
    // of LavaLines' ids.
    internal static List<string> LavaLakeBeats(string gameDirectory, RaceTrackReport? report, int lava)
    {
        var log = new List<string>();
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        int Say(LavaLine l) => lava + (int)l;
        var before = $"0 == var_game({SellerBeaten})";
        GroupB.EditScene(store, CelebrationScene, log, "Lyra's word on arrival (Q_STALL 1) and Baldino's call on the radio (variable 236)", model =>
            GroupB.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 9),
                GroupB.Arrival(before, LavaChapter, $"0 == var_game({QStall})", $"set_var_game({QStall}, 1);", Say(LavaLine.LyraArrival), Say(LavaLine.Ring), Say(LavaLine.Baldino))));
        // the kangaroo, until the seller is beaten: sorry about the stall
        GroupB.EditScene(store, CelebrationScene, log, "the kangaroo is sorry about the stall until the seller is beaten", model =>
        {
            var kangaroo = model.Actors.FindIndex(a => a.Entity == KangarooEntity);
            if (kangaroo < 1) throw new NotTheGames($"no kangaroo (entity {KangarooEntity})");
            return scripts =>
            {
                var life = scripts.GetText(kangaroo, ScriptKind.Life);
                scripts.SetText(kangaroo, ScriptKind.Life, Once(life, $"message({KangarooText});", $@"if ({before})
            {{
                message({Say(LavaLine.Kangaroo)});
            }}
            else
            {{
                message({KangarooText});
            }}"));
            };
        });
        // the ship to the Francos: a change_cube names a scene up to 255 only, so the won race brings Twinsen back to scene 95's start, where
        // a change-cube zone round it into the Francos' start (scene 257, the Island of the Francos' build) is switched on in chapter 7
        if (store.SceneExists(FrancosStart))
        {
            var to = store.Load(FrancosStart).Hero;
            GroupB.EditScene(store, CelebrationScene, log, $"the ship to the Francos: a door round the scene's start into scene {FrancosStart}, open in chapter 7 once the seller has told", model =>
            {
                const int Reach = 1536;
                int x = model.Hero.X, z = model.Hero.Z;
                static int Clamp(int v) => Math.Clamp(v, 0, IslandFile.CubeSize - 1);
                // (from the ground there all the way up: the scene's start is in the air over the dock, Twinsen comes down onto it; he comes
                // out as high over the Francos' start as he stood over the zone's floor)
                var floor = report?.GroundAfter is { } g ? (int)Math.Round(g(model.CubeX * 64 + x / 512.0, model.CubeY * 64 + z / 512.0)) - 300 : 0;
                model.Zones.Add(new SceneZoneModel
                {
                    Type = 0, Num = FrancosStart, X0 = Clamp(x - Reach), X1 = Clamp(x + Reach), Z0 = Clamp(z - Reach), Z1 = Clamp(z + Reach), Y0 = floor, Y1 = 25000,
                    Info = new[] { to.X - (x - Clamp(x - Reach)), to.Y, to.Z - (z - Clamp(z - Reach)), 0, ShipSwitch, 0, 0, 0 },
                });
                return GroupB.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 9), $@"void comportement_0()
{{
    if ({LavaChapter + 1} == chapter() && 2 == var_game({SellerBeaten}))
    {{
        set_change_cube({ShipSwitch}, 1);
    }}
}}
");
            });
        }
        else log.Add($"the ship to the Francos: no scene {FrancosStart} yet (the Island of the Francos isn't built before the lava lake): the won race takes Twinsen to the Francos' dock, scene {FrancosDock}");
        // the statuettes: on the lap's middle at a fifth, two fifths... of the way round (invisible: a car or Twinsen on foot within reach
        // picks it up), each only once the one before is found
        if (report is null || report.LapX.Length < 10) { log.Add("the lava lake's statuettes left out: no lap"); return log; }
        GroupB.EditScene(store, CelebrationScene, log, "four Dark Monk statuettes beside the lap, found in turn (Q_STALL 3-6), Zoe's word at the fourth", model =>
        {
            var edits = new List<Action<SceneScripts>>();
            for (var k = 0; k < 4; k++)
            {
                var i = (int)((k + 1) * report.LapX.Length / 5.0) % report.LapX.Length;
                double x = report.LapX[i], z = report.LapZ[i], y = report.LapY.Length > i ? report.LapY[i] : report.GroundAfter?.Invoke(x, z) ?? 0;
                var spot = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(x, model.CubeX), (int)Math.Round(y), RaceTrackScenes.CubeLocal(z, model.CubeY), 9);
                log.Add($"statuette {k + 1} at cell ({x:0.#}, {z:0.#}), {y:0}");
                edits.Add(GroupB.Add(model, spot, $@"void comportement_0()
{{
    if ({before} && {StallTold + k + 1} > var_game({QStall}))
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
    if ({StallTold + k} == var_game({QStall}) && 1200 > distance(0))
    {{
        message({Say(LavaLine.Statuette)});
        set_var_game({QStall}, {StallTold + k + 1});
{(k == 3 ? $"        message_zoe({Say(LavaLine.ZoeFour)});\n" : "")}        suicide();
    }}
}}
"));
            }
            return scripts => { foreach (var e in edits) e(scripts); };
        });
        return log;
    }

    // ---- what group B's islands share -------------------------------------------------------------------------------------------------
    internal static class GroupB
    {
        public static readonly string[] Ring =
        {
            "The radio is ringing. Answer it from your inventory.",
            "La radio sonne. Réponds depuis ton inventaire.",
            "Das Funksprechgerät klingelt. Nimm den Anruf im Inventar an.",
            "La radio está sonando. Contesta desde tu inventario.",
            "La radio sta squillando. Rispondi dal tuo inventario.",
            "O rádio está a tocar. Atende a partir do inventário.",
        };

        // An island's arrival: while `when` holds as the scene starts, Lyra's line once (`notSaid` true until `setSaid` runs), then -- with
        // the portable radio (item 26) -- "the radio is ringing" once a visit until Twinsen answers it from his inventory: Baldino's call for
        // the chapter (var 236 set to it).
        public static string Arrival(string when, int chapter, string notSaid, string setSaid, int lyra, int ring, int baldino) => $@"void comportement_0()
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
    if ({notSaid} && {Driving} != comportement_hero())
    {{
        {setSaid}
        message({lyra});
    }}
    if (0 < var_game({Radio}) && {chapter} > var_game({RadioAnswered}) && 0 == var_cube(45))
    {{
        set_var_cube(45, 1);
        message({ring});
    }}
    if (1 == use_inventory({Radio}) && {chapter} > var_game({RadioAnswered}))
    {{
        set_var_game({RadioAnswered}, {chapter});
        message({baldino});
    }}
}}
";

        // New lines at the end of a file of TEXT.HQR, in every language (the file's most used attribute). Returns the first id.
        public static int AddLines(string gameDirectory, int file, string[][] lines, List<string> log, string whose)
        {
            var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
            var text = HqrArchive.Open(textPath);
            var languages = Lba2TextBank.Languages(textPath);
            var first = NextId(text, languages, file);
            var hqr = File.ReadAllBytes(textPath);
            for (var lang = 0; lang < languages; lang++)
            {
                var words = lang < lines[0].Length ? lang : 0;
                var bank = Lba2TextBank.Load(text, lang, file);
                var attribute = bank.Texts.GroupBy(t => t.Attribute).OrderByDescending(g => g.Count()).Select(g => g.Key).DefaultIfEmpty(Lba2TextBank.NormalAttribute).First();
                AddTexts(bank, first, attribute, lines, words);
                hqr = bank.WriteInto(hqr);
            }
            File.WriteAllBytes(textPath, hqr);
            log.Add($"{whose} {lines.Length} lines are texts {first}-{first + lines.Length - 1} of text file {file}, in all {languages} languages");
            return first;
        }

        // A scene edited and saved: `edit` changes its actors, then gives what it does to the scripts (read with those changes)
        public static void EditScene(SceneStore store, int scene, List<string> log, string what, Func<SceneModel, Action<SceneScripts>> edit)
        {
            try
            {
                if (!store.SceneExists(scene)) { log.Add($"scene {scene}: {what} left out: no such scene"); return; }
                var model = store.Load(scene);
                var scriptEdit = edit(model);
                var scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
                scriptEdit(scripts);
                var built = scripts.Build();
                if (!built.Ok) { log.Add($"scene {scene}: {what}: the scripts would not compile: {string.Join("; ", built.Errors)}"); return; }
                store.SaveMany(new List<SceneChange> { new(scene, SceneSerializer.Parse(SceneGame.Lba2, built.Record!), null) }, allowErrors: true);
                log.Add($"scene {scene}: {what}");
            }
            catch (Exception e) when (e is NotTheGames or ScriptCompileException or InvalidDataException or ArgumentException or SceneEditException)
            {
                log.Add($"scene {scene}: {what} left out: {e.Message}");
            }
        }

        // An actor added to a scene with a life script of its own (set once the scripts are read)
        public static Action<SceneScripts> Add(SceneModel model, SceneActorModel actor, string life)
        {
            actor.Life = new byte[] { 0 }; actor.Track = new byte[] { 0 };
            var index = SceneOps.AddActor(model, actor);
            return scripts =>
            {
                scripts.SetText(index, ScriptKind.Life, life);
                scripts.SetText(index, ScriptKind.Track, "label(0);\nstop();\n");
            };
        }

        // An island's outside scene whose cube holds an island cell (-1: none)
        public static int OutsideScene(SceneStore store, RaceTrackIsland island, double x, double z)
        {
            for (var s = island.FirstScene; s <= island.LastScene; s++)
            {
                if (!store.SceneExists(s)) continue;
                var m = store.Load(s);
                if (m.CubeMode == 1 && m.CubeX == (int)Math.Floor(x / 64) && m.CubeY == (int)Math.Floor(z / 64)) return s;
            }
            return -1;
        }

        // The flag marshal's place by the start line (RACEMOD.CPP marshal=: island cells and the height), or null
        public static (double X, double Z, double Y)? MarshalAt(RaceTrackReport report) =>
            report.Marshal is { Length: >= 10 } m ? (m[6] / 512.0, m[7] / 512.0, m[8]) : null;

        // Somewhere off the roads near an island cell, on ground (Chapters' OffRoad, with no search zones of Citadel's in the way)
        public static (double X, double Z, double Y)? OffRoadNear(RaceTrackReport report, double x, double z, double clear, double reach)
        {
            givers = new();
            return OffRoad(report, x, z, clear, reach);
        }

        // ... and the island's outside scene it is in, and the way to face (towards `faceX`, `faceZ`); null when there's no such place
        public static (int Scene, (double X, double Z, double Y, int Beta) At)? Spot(SceneStore store, RaceTrackIsland island, RaceTrackReport report,
            double x, double z, double faceX, double faceZ, double clear = 1, double reach = 12)
        {
            if (OffRoadNear(report, x, z, clear, reach) is not { } o) return null;
            var scene = OutsideScene(store, island, o.X, o.Z);
            return scene < 0 ? null : (scene, Facing(o, faceX, faceZ));
        }

        // ... on ground near a height `nearY` (within 700: a person beside a start line on a mountain road, not at the cliff's foot)
        public static (int Scene, (double X, double Z, double Y, int Beta) At)? SpotAt(SceneStore store, RaceTrackIsland island, RaceTrackReport report,
            double x, double z, double nearY, double faceX, double faceZ, double clear = 1, double reach = 12)
        {
            if (report.GroundAfter is not { } ground) return null;
            for (var r = 0.0; r <= reach; r += 0.5)
            {
                var steps = r == 0 ? 1 : (int)Math.Ceiling(2 * Math.PI * r / 0.5);
                for (var k = 0; k < steps; k++)
                {
                    double a = 2 * Math.PI * k / steps, px = x + r * Math.Sin(a), pz = z + r * Math.Cos(a);
                    if (OffRoads(report, px, pz) < clear) continue;
                    // (and not by the cube's edge, where the scene's ground ends)
                    double cx = px - Math.Floor(px / 64) * 64, cz = pz - Math.Floor(pz / 64) * 64;
                    if (cx < 5 || cx > 59 || cz < 5 || cz > 59) continue;
                    var g = ground(px, pz);
                    if (g < 200 || Math.Abs(g - nearY) > 700) continue;
                    var scene = OutsideScene(store, island, px, pz);
                    if (scene >= 0) return (scene, Facing((px, pz, g), faceX, faceZ));
                }
            }
            return null;
        }

        // ... or, with no ground near (a start line on a raised road over the sea), beside a place at its own height `y`, out of the cars' way
        public static (int Scene, (double X, double Z, double Y, int Beta) At)? SpotBeside(SceneStore store, RaceTrackIsland island, RaceTrackReport report,
            double x, double z, double y, double faceX, double faceZ)
        {
            for (var r = 1.0; r <= 3; r += 0.5)
                for (var k = 0; k < 12; k++)
                {
                    double a = Math.PI * 2 * k / 12, px = x + r * Math.Sin(a), pz = z + r * Math.Cos(a);
                    if (report.InTheWay(px, pz, y - 200, y + 1500)) continue;
                    var scene = OutsideScene(store, island, px, pz);
                    if (scene >= 0) return (scene, Facing((px, pz, y), faceX, faceZ));
                }
            return null;
        }

        // An after-race controller (in each of an island's outside scenes): Twinsen out of his car after a race of the island (var 237), and
        // the race won -- its win moved the chapter on -- `won` is said and done
        public static string AfterRace(int chapter, string won) => $@"void comportement_0()
{{
    if ({chapter} == chapter())
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
    if (1 == var_game({RaceDoneB}) && {Driving} != comportement_hero())
    {{
        set_var_game({RaceDoneB}, 0);
        if ({chapter} < chapter())
        {{
{won}
        }}
    }}
}}
";
    }
}
