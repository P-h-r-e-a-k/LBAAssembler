using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The dartboards (the user, 2026-11-08, and 2026-11-12: "implementing a darts mini game for the citadel tavern ... for Twinsen to play
// against a random character from the car or in two player mode with Zoe then to play against Zoe"): a board on the Tavern's wall
// from chapter 2, when the Esmers have put the Tavern back; and one in the Tralu's bar under the sewers, on its wall between two of its
// barred windows, against the regulars at the table -- the user, 2026-11-15: "Let's add a dart board to the tralu bar area too between
// these two windows on the wall. Let's make the opponent one of the people sat at the table". The game is the engine's (RACEMOD.CPP
// DartFrame, the race file's darts= and dart_foe= lines, a board's after its own darts= line): Action by a board starts a match of 501
// against one of its foes, chosen at random -- or against Zoe, played by player 2, in a game with two players.
internal static partial class RaceTrackStory
{
    // a foe: its body and standing animation (BODY.HQR, ANIM.HQR: shown beside the board), its name, how steady its hand is (the tenths of
    // the board's radius its darts scatter by: smaller is better)
    // (and, for a regular sat in the scene, its entity and its walk -- ANIM.HQR: he gets up and walks over to ask the wager, the user, 2026-11-16,
    // "If Twinsen triggers a dart game, one of the NPCs at the table stands up, walks over to him, and asks the question"; -1: none)
    internal sealed record DartBoard(int Scene, int X, int Z, int Beta, int Floor, int FromChapter, (int Body, int Anim, string Name, int Scatter, int Entity, int Walk)[] Foes,
        DartPlayer? Player = null);
    // the one playing darts at the board on his own (the user, 2026-11-18: "Let's add a character playing darts in both the tavern and the
    // tralu bar area, when Twinsen walks up to him he asks Twinsen if he wants a game, if Twinsen says yes then they discuss stakes before
    // playing"): his entity and body (the entity's generic), the throw he plays over and over (ANIM.HQR -- RACEMOD.CPP dart_practice=), his
    // place behind the thrower's spot (that much further out: on the line he throws from -- beside it, 650 along the wall, the Tavern's
    // bench took him up onto it), his lines (DartsTalk: Ask, Stakes, Low, Deal,
    // Later, in that order from `Lines`) and his foe among the board's (the one of his entity: the match is against him). His script: walked
    // up to, he asks -- yes or no, then the stakes -- and sets DartsAsk; the engine starts the match.
    internal sealed record DartPlayer(int Entity, int GenBody, int Throw, int Back, DartsTalk Lines);
    // (ANIM.HQR 228: Raph's own gesture, the arm up and forward and back -- his entity's generic 131 -- drawn right on the rabbibunnies' bodies
    // only, the drunk rabbibunny's and the blue-and-white one's among them: the throw)
    private const int RaphEntity = 19, DartThrow = 228, BennyEntity = 45, BennyBody = 43;
    // the game variable the dialogue sets (RACEMOD.CPP dart_ask=): 1 a friendly game, 2-4 the stakes (25, 50, 100); -1 while a match is on
    internal const int DartsAsk = 267;
    private const int DartsAskNear = 1800, DartsAskAgain = 3000;

    // the Tavern's (scene 3): on the window wall in the corner at the end of the bar where the potted plant stood (the user, 2026-11-12:
    // "remove plant from the corner at the end of the bar and put the dart board against that wall instead"; the plant is
    // Citadel/TavernPlant's), facing the floor (beta 1024: toward +x) -- Luc behind his bar and Baldino best, Pat the worst
    // the Tralu's bar's (scene 19, the hall): on its west wall (its face x 14,336, cell 28) between the two barred windows nearest the
    // corner, over the floor (2,304), facing the room -- the regulars at the massive table: Pat, Fab and the drunk rabbibunny (the
    // Tavern's own, their game bodies: entities 29, 28, 25)
    internal static readonly DartBoard[] DartBoards =
    {
        new(TavernScene, 584, 7930, 1024, 1024, 2, new[]
        {
            (39, 282, "Luc", 9, -1, -1), (139, 715, "Baldino", 11, -1, -1), (32, 224, "Raph", 14, RaphEntity, 225), (40, 289, "Tim", 16, -1, -1), (59, 383, "Mr. Paul", 18, -1, -1), (44, 302, "Pat", 22, -1, -1),
        }, new DartPlayer(RaphEntity, 0, DartThrow, 200, DartsTalk.RaphAsk)),
        new(TraluBarHall, 14408, 24560, 1024, 2304, 1, new[]
        {
            // (Pat and Fab walk -- entities 29 and 28's walks; the drunk rabbibunny, entity 25, has none: he lurches over, his sway)
            (44, 302, "Pat", 20, 29, 304), (41, 297, "Fab", 15, 28, 299), (38, 277, "Bunny", 24, 25, 279),
            // (Benny, the one at the board: a rabbibunny in blue and white -- entity 45's other body, BODY.HQR 68, its stand and walk)
            (68, 393, "Benny", 17, BennyEntity, 397),
        }, new DartPlayer(BennyEntity, BennyBody, DartThrow, 200, DartsTalk.BennyAsk)),
    };
    private const int TraluBarHall = 19;
    internal const int DartsReach = 1500;
    // The darts players' lines (Citadel Island's texts, after the evening's: DartsTalkId): Twinsen's answers, then each player's own --
    // Ask, Stakes, Low, Deal, Later in that order (DartPlayer.Lines: the first of his). (A question -- Ask, Stakes -- fits the choice box's
    // one page: Benny's first, 108 letters, wanted a second and the question waited there for a key, 2026-11-19.)
    internal enum DartsTalk { Yes, No, Fun, K25, K50, K100, RaphAsk, RaphStakes, RaphLow, RaphDeal, RaphLater, BennyAsk, BennyStakes, BennyLow, BennyDeal, BennyLater }
    private static int DartsTalkId(Func<ChapterLine, int> say, DartsTalk line) => say(ChapterLine.ZoeSmiling) + ChapterLines.Length + EveningLines.Length + (int)line;
    internal static readonly string[][] DartsTalkLines =
    {
        new[] { "You're on!", "Je relève le défi !", "Na klar!", "¡Hecho!", "Ci sto!", "Alinho!" },
        new[] { "Not right now.", "Pas maintenant.", "Gerade nicht.", "Ahora no.", "Non adesso.", "Agora não." },
        new[] { "Just for fun.", "Juste pour le plaisir.", "Nur zum Spaß.", "Solo por diversión.", "Solo per divertirci.", "Só por diversão." },
        new[] { "25 kashes.", "25 kashes.", "25 Kashes.", "25 kashes.", "25 kash.", "25 kashes." },
        new[] { "50 kashes.", "50 kashes.", "50 Kashes.", "50 kashes.", "50 kash.", "50 kashes." },
        new[] { "100 kashes.", "100 kashes.", "100 Kashes.", "100 kashes.", "100 kash.", "100 kashes." },
        // Raph, in the Tavern
        new[]
        {
            "Fancy a game of darts, Twinsen? Five-oh-one, finish on a double.",
            "Une partie de fléchettes, Twinsen ? Cinq cent un, on finit sur un double.",
            "Lust auf eine Runde Darts, Twinsen? Fünfhunderteins, Abschluss auf ein Doppel.",
            "¿Te apetece una partida de dardos, Twinsen? Quinientos uno, se cierra con un doble.",
            "Ti va una partita a freccette, Twinsen? Cinquecentouno, si chiude su un doppio.",
            "Apetece-te uma partida de dardos, Twinsen? Quinhentos e um, a fechar num duplo.",
        },
        new[]
        {
            "What are we playing for?",
            "On joue pour combien ?",
            "Worum spielen wir?",
            "¿Qué nos jugamos?",
            "Su cosa giochiamo?",
            "O que é que apostamos?",
        },
        new[]
        {
            "You seem a bit low on money, let's just have a friendly game.",
            "Tu m'as l'air un peu à court d'argent, faisons juste une partie amicale.",
            "Du scheinst etwas knapp bei Kasse zu sein, spielen wir einfach eine Freundschaftsrunde.",
            "Pareces ir algo corto de dinero, juguemos solo una partida amistosa.",
            "Mi sembri un po' a corto di soldi, facciamo solo una partita amichevole.",
            "Pareces estar um pouco curto de dinheiro, vamos só jogar uma partida amigável.",
        },
        new[]
        {
            "You're on. You throw first -- I'll be right behind you.",
            "Ça marche. Tu lances en premier -- je suis juste derrière toi.",
            "Abgemacht. Du wirfst zuerst -- ich stehe direkt hinter dir.",
            "Trato hecho. Lanzas tú primero, yo estaré justo detrás de ti.",
            "Affare fatto. Tiri per primo tu, io sono proprio dietro di te.",
            "Combinado. Lanças tu primeiro -- eu fico mesmo atrás de ti.",
        },
        new[]
        {
            "Suit yourself. I'll be here, practising.",
            "Comme tu veux. Je serai là, à m'entraîner.",
            "Wie du willst. Ich bin hier und übe.",
            "Como quieras. Estaré aquí, practicando.",
            "Come vuoi. Io sono qui ad allenarmi.",
            "Como queiras. Eu fico aqui a treinar.",
        },
        // Benny, in the Tralu's bar
        new[]
        {
            "Hey, you! Up for a round of arrows? Five-oh-one, double out.",
            "Hé, toi ! Une partie de fléchettes ? Cinq cent un, sortie sur un double.",
            "He, du! Eine Runde Pfeile? Fünfhunderteins, Doppel zum Schluss.",
            "¡Eh, tú! ¿Una ronda de dardos? Quinientos uno, cierre con doble.",
            "Ehi, tu! Un giro di freccette? Cinquecentouno, chiusura su un doppio.",
            "Ei, tu! Uma ronda de dardos? Quinhentos e um, a fechar num duplo.",
        },
        new[]
        {
            "And the stakes, friend?",
            "Et la mise, l'ami ?",
            "Und der Einsatz, Freund?",
            "¿Y la apuesta, amigo?",
            "E la posta, amico?",
            "E a aposta, amigo?",
        },
        new[]
        {
            "Your purse looks a bit light for that, friend. A friendly game, then.",
            "Ta bourse m'a l'air un peu légère pour ça, l'ami. Une partie amicale, alors.",
            "Dein Geldbeutel sieht dafür etwas leicht aus, Freund. Dann eben eine Freundschaftsrunde.",
            "Tu bolsa parece algo ligera para eso, amigo. Una partida amistosa, entonces.",
            "La tua borsa mi sembra un po' leggera per quello, amico. Una partita amichevole, allora.",
            "A tua bolsa parece um bocado leve para isso, amigo. Uma partida amigável, então.",
        },
        new[]
        {
            "Done. Step up to the oche -- you throw first.",
            "Marché conclu. Avance jusqu'à la ligne -- tu lances en premier.",
            "Abgemacht. Tritt an die Linie -- du wirfst zuerst.",
            "Hecho. Acércate a la línea: lanzas tú primero.",
            "Fatto. Vai alla linea: tiri per primo tu.",
            "Feito. Chega-te à linha -- lanças tu primeiro.",
        },
        new[]
        {
            "Your loss. More board for me.",
            "Tant pis pour toi. Plus de cible pour moi.",
            "Dein Pech. Mehr Scheibe für mich.",
            "Tú te lo pierdes. Más diana para mí.",
            "Peggio per te. Più bersaglio per me.",
            "Azar o teu. Mais alvo para mim.",
        },
    };

    // Zoe, for the game with two players: her body and animation, and her own steadiness
    internal const int ZoeDartBody = 26, ZoeDartAnim = 207;

    // (how far out from the wall the thrower stands, the oche just in front of him; the one waiting: further back and to the side, facing the board
    // -- the user, 2026-11-12: "Twinsen's opponent let's have them standing behind him, and watch them alternate positions"; the blackboard: along
    // the wall beside the board, its middle that far from the board's)
    internal const int DartsSpotOut = 1700, DartsOcheOut = 1500, DartsWaitOut = 2600, DartsWaitAside = 700, DartsSlateAside = 1000;

    // a point out from a board's wall (along its facing) and aside along the wall (+: toward larger z, with the board facing +x)
    private static (int X, int Z) DartsAt(DartBoard b, int outward, int aside)
    {
        var a = b.Beta * 2 * Math.PI / 4096;
        double fx = Math.Sin(a), fz = Math.Cos(a);
        return (b.X + (int)Math.Round(fx * outward - fz * aside), b.Z + (int)Math.Round(fz * outward + fx * aside));
    }

    private static void DartsTavern(StoryParts p)
    {
        foreach (var b in DartBoards)
        {
            if (!p.Store.SceneExists(b.Scene)) { p.Log.Add($"the dartboard of scene {b.Scene} left out: no such scene"); continue; }
            var from = b.FromChapter > 0 ? $"void comportement_0()\n{{\n    if ({b.FromChapter} > chapter())\n    {{\n        suicide();\n    }}\n}}\n" : "void comportement_0()\n{\n}\n";
            p.Edit(b.Scene, $"the dartboard on the wall (from chapter {b.FromChapter}): Action by it plays darts -- RACEMOD.CPP DartFrame", model =>
            {
                var board = Citadel.CitadelProps.Actor(Citadel.CitadelProps.Dartboard, b.X, b.Floor, b.Z, b.Beta);
                return p.Add(model, board, from);
            });
            p.Edit(b.Scene, "the darts' blackboard beside the board, for the scores", model =>
            {
                var (x, z) = DartsAt(b, 0, DartsSlateAside);
                var slate = Citadel.CitadelProps.Actor(Citadel.CitadelProps.DartScoreBoard, x, b.Floor, z, b.Beta);
                return p.Add(model, slate, from);
            });
            p.Edit(b.Scene, "the oche, the line the darts' throwers stand behind (walked over)", model =>
            {
                var (x, z) = DartsAt(b, DartsOcheOut, 0);
                var oche = Citadel.CitadelProps.Actor(Citadel.CitadelProps.DartOche, x, b.Floor, z, b.Beta);
                oche.Armor = WalkOverArmour;
                return p.Add(model, oche, from);
            });
            // (the one played against, standing where the waiting thrower stands: the engine gives it the foe's body and moves it -- hidden but in
            // a match, and walked through)
            p.Edit(b.Scene, "the darts' opponent's place (its body the foe's in a match: RACEMOD.CPP DartFrame)", model =>
            {
                var (x, z) = DartsAt(b, DartsWaitOut, DartsWaitAside);
                var stand = Citadel.CitadelProps.Actor(Citadel.CitadelProps.DartOche, x, b.Floor, z, (b.Beta + 2048) & 4095);
                stand.Armor = WalkOverArmour;
                return p.Add(model, stand, from);
            });
            if (b.Player is { } player) DartsPlayer(p, b, player);
        }
    }

    // The one playing darts at a board on his own: beside the oche, facing the board (his throw the engine's, dart_practice=), from the
    // board's chapter. Walked up to (DartsAskNear), he asks once -- again once Twinsen has gone DartsAskAgain away, or by Action -- and not
    // while a match is on (DartsAsk -1). Yes: the stakes, and DartsAsk set (one Twinsen hasn't the kashes for: a friendly game, said so).
    private static void DartsPlayer(StoryParts p, DartBoard b, DartPlayer player)
    {
        var who = Copy(p.Originals, TraluCave, CaveRaph);
        if (who is null) { p.Log.Add($"the darts player of scene {b.Scene} left out: no Raph in scene {TraluCave} to make him from"); return; }
        int Id(DartsTalk line) => DartsTalkId(p.Say, line);
        int Said(int k) => Id(player.Lines + k);
        var ask = $@"            add_choice({Id(DartsTalk.Yes)});
            add_choice({Id(DartsTalk.No)});
            ask_choice({Said(0)});
            if ({Id(DartsTalk.Yes)} == choice())
            {{
                add_choice({Id(DartsTalk.Fun)});
                add_choice({Id(DartsTalk.K25)});
                add_choice({Id(DartsTalk.K50)});
                add_choice({Id(DartsTalk.K100)});
                ask_choice({Said(1)});
                if ({Id(DartsTalk.K100)} == choice() && 100 > nb_gold_pieces())
                {{
                    message({Said(2)});
                    set_var_game({DartsAsk}, 1);
                }}
                else if ({Id(DartsTalk.K50)} == choice() && 50 > nb_gold_pieces())
                {{
                    message({Said(2)});
                    set_var_game({DartsAsk}, 1);
                }}
                else if ({Id(DartsTalk.K25)} == choice() && 25 > nb_gold_pieces())
                {{
                    message({Said(2)});
                    set_var_game({DartsAsk}, 1);
                }}
                else
                {{
                    message({Said(3)});
                    if ({Id(DartsTalk.K100)} == choice())
                    {{
                        set_var_game({DartsAsk}, 4);
                    }}
                    else if ({Id(DartsTalk.K50)} == choice())
                    {{
                        set_var_game({DartsAsk}, 3);
                    }}
                    else if ({Id(DartsTalk.K25)} == choice())
                    {{
                        set_var_game({DartsAsk}, 2);
                    }}
                    else
                    {{
                        set_var_game({DartsAsk}, 1);
                    }}
                }}
            }}
            else
            {{
                message({Said(4)});
            }}";
        var near = $"{Driving} != comportement_hero() && 0 == var_game({DartsAsk})";
        var life = $@"void comportement_0()
{{
    if ({b.FromChapter} > chapter())
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
    if ({DartsAskNear} > distance(0) && {near})
    {{
        set_comportement(comportement_2);
{ask}
    }}
}}

void comportement_2()
{{
    if ({DartsAskAgain} < distance(0))
    {{
        set_comportement(comportement_1);
    }}
    swif (1 == action())
    {{
        if (1500 > distance(0) && {near})
        {{
{ask}
        }}
    }}
}}
";
        p.Edit(b.Scene, $"the darts player at the board (entity {player.Entity}): walked up to, he asks for a game -- yes, then the stakes (DartsAsk {DartsAsk}) -- RACEMOD.CPP dart_ask=", model =>
        {
            var (x, z) = DartsAt(b, DartsSpotOut + player.Back, 0);
            who.Entity = player.Entity; who.Body = player.GenBody; who.Anim = 0;
            who.X = x; who.Y = b.Floor; who.Z = z; who.Beta = (b.Beta + 2048) & 4095;
            return p.Add(model, who, life);
        });
    }

    // The race file's lines for the game: each board's darts= line, then its face's and its foes'
    internal static IEnumerable<string> DartsLines()
    {
        foreach (var b in DartBoards)
        {
            // (the board's middle: where the post stands, the board's height up; the spot: three cells out in front of it, along its facing;
            // the chapter it's played from)
            var (spotX, spotZ) = DartsAt(b, DartsSpotOut, 0);
            yield return $"darts={b.Scene} {spotX} {spotZ} {DartsReach} {b.X} {b.Z} {Citadel.CitadelProps.DartMiddleHigh + b.Floor} {b.FromChapter}";
            // (the board's face and size, for the darts stuck in it: its face's x, z out from the wall, its radius, its facing; the waiting place)
            var (faceX, faceZ) = DartsAt(b, Citadel.CitadelProps.DartStandOff, 0);
            var (waitX, waitZ) = DartsAt(b, DartsWaitOut, DartsWaitAside);
            yield return $"dart_board={faceX} {faceZ} {Citadel.CitadelProps.DartRadius} {b.Beta} {waitX} {waitZ}";
            foreach (var f in b.Foes) yield return $"dart_foe={f.Body} {f.Anim} {f.Scatter} {f.Name.Replace(' ', '_')}" + (f.Entity >= 0 ? $" {f.Entity} {f.Walk}" : "");
            if (b.Player is { } player) yield return $"dart_practice={player.Entity} {player.Throw}";
        }
        yield return $"dart_ask={DartsAsk}";
        yield return $"dart_zoe={ZoeDartBody} {ZoeDartAnim} 12 Zoe";
    }
}
