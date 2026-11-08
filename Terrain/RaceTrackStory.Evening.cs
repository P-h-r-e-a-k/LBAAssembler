using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Chapter 1's evening on Citadel Island, from Raph's time beaten to bed (the user's fix list of 2026-11-06, items 1 and 10-16, 21 -- one
// flow, no teleports in it: docs/LBRA_WALKTHROUGH.md chapter 1, 10-12):
//   - the storm track (RaceTrackStory.Apply step 4): Zoe watches the practice by the start line from the moment she leaves home (the
//     yard's Zoe walks off up the path, YardZoeLeaves); Raph's time beaten, he gets out of his car -- the car out of sight while he is out
//     of it: one Raph at a time -- and walks over to her; Twinsen has to talk to him: "I'm off to the lighthouse", and he runs off down the
//     road; Zoe takes Twinsen's arm (the game's behaviour of the two of them walking together, 5) and they walk there, slowly, together.
//     The yard on the way (scene 49) no longer puts them back at its north edge (the game's own walk to the lighthouse came in there).
//   - the lighthouse (Chapters.cs Lighthouse, and here): the spell as it was, then no change of scene -- Twinsen and Zoe are left on its
//     top together, each their own again; Raph walks off down the hill towards the storm track (back to his car there: its line), the Weather
//     Wizard goes home to his tent his own way (a flash, as he came), and on a later visit neither is there. Going down, Zoe asks Twinsen
//     if he's coming home: "Yes, let's go home" -- arm in arm again, down and home together; "I'll come home soon" -- she goes on ahead.
//   - the Tavern's doorstep: Tim's lights over the sea, "I'm going home" -- and he goes, along the road and out of sight, not to come back
//     that night.
//   - home (Citadel/CitadelNightHouse: the house at night, scene NightHouse -- the day's house sends Twinsen there while he is tired):
//     Zoe is there either way (come in with him, or waiting); his bed, from any of its three open sides (the day's house's bed zone was
//     beside the bed, out in the room: the user couldn't sleep, 2026-11-06): "What a day!", into bed, Zoe's goodnight, his, the candle
//     out -- the dark -- and the morning in the day's house (Morning), in whatever he wore to bed: the spell's cutscene no longer
//     dresses him in his everyday clothes after taking his body (body(1), the game's own way after a cutscene: his tunic once he has it).
//   - the alien with his two dogs by the Tavern is the game's for after the rain: there from chapter 2, not from the spell on (the game
//     moved on to chapter 2 at the spell, the story at the morning).
internal static partial class RaceTrackStory
{
    // Q_EVENING: 1 Zoe has left the lighthouse's top (her question answered), 2 Tim has gone home from the Tavern's doorstep (in that order:
    // Twinsen comes down from the top first). Game variable 58: a spare one (RaceTrackStory.Eggs.cs's list) -- no script, race file or engine
    // code uses it (235-237 are the videos seen, PLAYACF.CPP)
    public const int QEvening = 58, ZoeLeftTop = 1, TimGoneHome = 2;
    public const int NightHouse = Citadel.CitadelNightHouse.Scene;
    // DayVar away from Tired while one of the evening's own lines is said: the race-track mode's tired= says its line in place of anyone's
    // while Twinsen is tired (Tim's lights over the sea are said so too)
    private const int TiredAside = 4;
    // a per-visit flag of a scene's own (var_cube): Zoe beside Twinsen on the lighthouse's top / come home with him
    private const int Once5 = 44;
    // the game's walk of the two of them in the yard: its scene start put them on the path from the storm track (its point 15)
    private const int YardWalkIn = 15;

    private enum EveningLine
    {
        ZoeWatching, ZoeWatchingReady, FlickReady, FlickWon, ZoeComingHome, YesHome, HomeSoon, ZoeAhead, WizardHome, ZoeAtHome, ZoeGoodnight,
        TwinsenGoodnight,
    }
    private static readonly string[][] EveningLines =
    {
        new[]
        {
            "Raph's been lapping since breakfast, rain or no rain. Get your gloves back, Twinsen, and show him how it's done!",
            "Raph enchaîne les tours depuis le petit déjeuner, pluie ou pas. Récupère tes gants, Twinsen, et montre-lui comment on fait !",
            "Raph dreht seit dem Frühstück seine Runden, Regen hin oder her. Hol dir deine Handschuhe zurück, Twinsen, und zeig ihm, wie man fährt!",
            "Raph lleva dando vueltas desde el desayuno, llueva o no. ¡Recupera tus guantes, Twinsen, y enséñale cómo se hace!",
            "Raph gira in pista dalla colazione, pioggia o non pioggia. Riprenditi i guanti, Twinsen, e fagli vedere come si fa!",
            "O Raph anda às voltas desde o pequeno-almoço, com chuva ou sem ela. Recupera as tuas luvas, Twinsen, e mostra-lhe como se faz!",
        },
        new[]
        {
            "Gloves on? Then go and beat his time! I'll be watching from right here.",
            "Tu as tes gants ? Alors va battre son temps ! Je regarde d'ici.",
            "Handschuhe an? Dann los, schlag seine Zeit! Ich schaue von hier aus zu.",
            "¿Ya tienes los guantes? ¡Pues ve a batir su tiempo! Yo miro desde aquí.",
            "Hai i guanti? Allora va' a battere il suo tempo! Io guardo da qui.",
            "Já tens as luvas? Então vai bater o tempo dele! Eu fico a ver daqui.",
        },
        new[]
        {
            "Gloves on, I see. Drive up to the line whenever you're ready, and I'll drop the flag for you.",
            "Tu as tes gants, je vois. Avance jusqu'à la ligne quand tu veux, et j'abaisserai le drapeau pour toi.",
            "Handschuhe an, wie ich sehe. Fahr an die Linie, wenn du so weit bist, und ich senke die Flagge für dich.",
            "Ya llevas los guantes, ya veo. Acércate a la línea cuando quieras y bajaré la bandera para ti.",
            "Vedo che hai i guanti. Porta la macchina sulla linea quando vuoi, e abbasserò la bandiera per te.",
            "Já tens as luvas, estou a ver. Avança até à linha quando quiseres, e eu baixo a bandeira para ti.",
        },
        new[]
        {
            "Raph's lap record, beaten in the pouring rain! I'll chalk your time on my plinth, before the rain washes it off.",
            "Le record de Raph, battu sous une pluie battante ! J'écris ton temps à la craie sur mon socle, avant que la pluie ne l'efface.",
            "Raphs Rundenrekord, geschlagen im strömenden Regen! Ich schreibe deine Zeit mit Kreide auf meinen Sockel, bevor der Regen sie wegwäscht.",
            "¡El récord de vuelta de Raph, batido bajo un aguacero! Apuntaré tu tiempo con tiza en mi pedestal, antes de que la lluvia lo borre.",
            "Il record sul giro di Raph, battuto sotto la pioggia battente! Scrivo il tuo tempo col gesso sul mio piedistallo, prima che la pioggia lo cancelli.",
            "O recorde de volta do Raph, batido debaixo de chuva torrencial! Vou escrever o teu tempo a giz no meu pedestal, antes que a chuva o apague.",
        },
        new[]
        {
            "Are you coming home, Twinsen?",
            "Tu rentres à la maison, Twinsen ?",
            "Kommst du mit nach Hause, Twinsen?",
            "¿Vienes a casa, Twinsen?",
            "Vieni a casa, Twinsen?",
            "Vens para casa, Twinsen?",
        },
        new[] { "Yes, let's go home.", "Oui, rentrons.", "Ja, gehen wir nach Hause.", "Sí, vamos a casa.", "Sì, andiamo a casa.", "Sim, vamos para casa." },
        new[] { "I'll come home soon.", "Je rentre bientôt.", "Ich komme bald nach.", "Iré a casa pronto.", "Torno a casa presto.", "Vou para casa daqui a pouco." },
        new[]
        {
            "Don't stay up here too long, then. I'll light the candle by the bed for you.",
            "Ne reste pas trop longtemps là-haut, alors. J'allumerai la bougie près du lit pour toi.",
            "Bleib nicht zu lange hier oben. Ich zünde dir die Kerze am Bett an.",
            "Entonces no te quedes aquí arriba mucho rato. Te encenderé la vela junto a la cama.",
            "Allora non restare quassù troppo a lungo. Ti accendo la candela accanto al letto.",
            "Então não fiques aqui em cima muito tempo. Acendo-te a vela ao pé da cama.",
        },
        new[]
        {
            "And now, home to my tent. Mind the puddles on your way down, Twinsen!",
            "Et maintenant, je rentre sous ma tente. Attention aux flaques en descendant, Twinsen !",
            "Und nun nach Hause in mein Zelt. Pass beim Hinuntergehen auf die Pfützen auf, Twinsen!",
            "Y ahora, a casa, a mi tienda. ¡Cuidado con los charcos al bajar, Twinsen!",
            "E ora, a casa nella mia tenda. Attento alle pozzanghere scendendo, Twinsen!",
            "E agora, para casa, para a minha tenda. Cuidado com as poças ao descer, Twinsen!",
        },
        new[]
        {
            "Bed, Twinsen. The Wizard said by morning, and morning comes quicker when you're asleep.",
            "Au lit, Twinsen. Le Mage a dit demain matin, et le matin arrive plus vite quand on dort.",
            "Ab ins Bett, Twinsen. Der Magier hat gesagt, bis morgen früh, und der Morgen kommt schneller, wenn man schläft.",
            "A la cama, Twinsen. El Mago dijo que por la mañana, y la mañana llega antes cuando duermes.",
            "A letto, Twinsen. Il Mago ha detto domattina, e la mattina arriva prima quando si dorme.",
            "Para a cama, Twinsen. O Mago disse de manhã, e a manhã chega mais depressa quando se dorme.",
        },
        new[]
        {
            "Goodnight, Twinsen. Don't forget the candle.",
            "Bonne nuit, Twinsen. N'oublie pas la bougie.",
            "Gute Nacht, Twinsen. Vergiss die Kerze nicht.",
            "Buenas noches, Twinsen. No te olvides de la vela.",
            "Buonanotte, Twinsen. Non dimenticare la candela.",
            "Boa noite, Twinsen. Não te esqueças da vela.",
        },
        new[] { "Goodnight, Zoe.", "Bonne nuit, Zoé.", "Gute Nacht, Zoe.", "Buenas noches, Zoe.", "Buonanotte, Zoe.", "Boa noite, Zoe." },
    };
    // (after the chapters' lines in Citadel Island's texts)
    private static int EveningId(Func<ChapterLine, int> say, EveningLine line) => say(ChapterLine.ZoeSmiling) + ChapterLines.Length + (int)line;

    // A line said as the one saying it would, while Twinsen is tired (not the tired line in its place)
    private static string Aside(string said) => $@"set_var_game({DayVar}, {TiredAside});
            {said}
            set_var_game({DayVar}, {Tired});";

    // Points along a track's road from the place on it nearest (x, z) -- island cells -- the way along it whose end comes nearer (toX, toZ),
    // every `step` cells, up to `count`, on the road (its height), inside the scene's cube; its raised deck, bridges and jumps end them.
    // Scene-local track points (none when there's no road there).
    private static List<SceneTrackPoint> AlongRoad(RaceTrackReport report, SceneModel model, double x, double z, double toX, double toZ, double step, int count)
    {
        (int R, int K, double D) best = (-1, -1, double.MaxValue);
        for (var r = 0; r < report.Roads.Count; r++)
            for (var k = 0; k < report.Roads[r].X.Length; k++)
            {
                var d = (report.Roads[r].X[k] - x) * (report.Roads[r].X[k] - x) + (report.Roads[r].Z[k] - z) * (report.Roads[r].Z[k] - z);
                if (d < best.D) best = (r, k, d);
            }
        if (best.R < 0) return new();
        var road = report.Roads[best.R];
        bool Flagged(bool[] flags, int k) => k < flags.Length && flags[k];
        List<(double X, double Z, double Y)> Walk(int dir)
        {
            var points = new List<(double X, double Z, double Y)>();
            int n = road.X.Length, k = best.K;
            double gone = 0, next = step;
            for (var guard = 0; guard < n && points.Count < count; guard++)
            {
                var j = k + dir;
                if (j < 0 || j >= n) { if (!road.Closed) break; j = (j + n) % n; }
                gone += Math.Sqrt((road.X[j] - road.X[k]) * (road.X[j] - road.X[k]) + (road.Z[j] - road.Z[k]) * (road.Z[j] - road.Z[k]));
                k = j;
                if (Flagged(road.Deck, k) || Flagged(road.Bridge, k) || Flagged(road.Jump, k) || Flagged(road.Gap, k) || Flagged(road.Void, k)) break;
                if (road.X[k] < model.CubeX * 64 + 1 || road.X[k] > model.CubeX * 64 + 63 || road.Z[k] < model.CubeY * 64 + 1 || road.Z[k] > model.CubeY * 64 + 63) break;
                if (gone >= next) { points.Add((road.X[k], road.Z[k], k < road.H.Length ? road.H[k] : 0)); next += step; }
            }
            return points;
        }
        double Far(List<(double X, double Z, double Y)> p) => p.Count == 0 ? double.MaxValue : (p[^1].X - toX) * (p[^1].X - toX) + (p[^1].Z - toZ) * (p[^1].Z - toZ);
        var ahead = Walk(1); var back = Walk(-1);
        var way = Far(ahead) <= Far(back) ? ahead : back;
        return way.Select(p => new SceneTrackPoint(RaceTrackScenes.CubeLocal(p.X, model.CubeX), (int)Math.Round(p.Y), RaceTrackScenes.CubeLocal(p.Z, model.CubeY))).ToList();
    }

    // Raph's labels by the storm track's start line (his track there is the story's own): out of his car and over to Zoe, off to the
    // lighthouse, gone
    private const int RaphOutLabel = 10, RaphRunsLabel = 20, RaphGoneLabel = 21;
    // the lighthouse's door, in island cells (scene 46's cube: 7, 10) -- where Raph runs off to, and Tim doesn't
    private static readonly (double X, double Z) LighthouseCells = (7 * 64 + 16600 / 512.0, 10 * 64 + 11150 / 512.0);

    private static void Evening(StoryParts p)
    {
        AliensAfterRain(p);
        YardWayIn(p);
        LighthouseEvening(p);
        NightHouseStory(p);
    }

    // ---- the alien with his two dogs (scene 42) -------------------------------------------------------------------------------------
    // The game's Esmer who walks two dogs by the Tavern (scene 42's actors of entities 30 and 32: he and his dogs) is there from Raph freed
    // on (var 51 at 3) -- in the game the spell came next and moved the game on to chapter 2, the rain gone; the story's Raph is won over
    // at the race, and the rain stays the night (the user, 2026-11-06: "After the lighthouse scene, while it is still raining, he is
    // already there with his two dogs"). Now from chapter 2 on (as before, not chapter 4).
    private const int DogWalker = 30, Dog = 32;
    private static void AliensAfterRain(StoryParts p) => p.Edit(AliensScene, "the alien with his two dogs by the Tavern once the rain has cleared (chapter 2), not from Raph won over in the storm", model => scripts =>
    {
        const string gate = "4 == chapter() || 3 > var_game(51)";
        var n = 0;
        for (var i = 1; i < model.Actors.Count; i++)
        {
            if (model.Actors[i].Entity is not (DogWalker or Dog)) continue;
            var life = scripts.GetText(i, ScriptKind.Life);
            if (!life.Contains(gate)) continue;
            scripts.SetText(i, ScriptKind.Life, Once(life, gate, "4 == chapter() || 2 > chapter()"));
            n++;
        }
        if (n == 0) throw new NotTheGames("the alien and his dogs");
    });

    // ---- the yard on the way to the lighthouse (scene 49) -----------------------------------------------------------------------------
    // The game's walk of Twinsen and Zoe to the lighthouse came into the yard by a cut, put on the path down from the storm track (its
    // point 15) -- the story's two come down that path themselves, where they please: they are left where they come in (the game's
    // short walk on, and its way out west to the lighthouse, as they were).
    private static void YardWayIn(StoryParts p) => p.Edit(CarScene, "Twinsen and Zoe walking to the lighthouse come into the yard where they walk in, not put on its path", _ => scripts =>
    {
        var hero = scripts.GetText(0, ScriptKind.Life);
        hero = Loose(hero, $"set_camera(2, 1);\npos_point({YardWalkIn});", "set_camera(2, 1);");
        scripts.SetText(0, ScriptKind.Life, hero);
    });

    // ---- the lighthouse (scene 46): after the spell ------------------------------------------------------------------------------------
    // (Zoe on the top: beside Twinsen at its point 5, on the walkway round the lamp room; the lamp room's door, where the way down is --
    // Twinsen faces it, east, to go down (LighthouseClimb); Raph's way off down the hill from the door, towards the storm track: out of the
    // door's recess onto the ledge, along it and down past the bend in the path, where he is out of sight)
    private static readonly (int X, int Y, int Z) ZoeOnTop = (13425, 7629, 10830), LampRoomDoor = (13470, 7629, 10990);
    private static readonly (int X, int Y, int Z)[] RaphDownHill = { (17800, 2950, 11250), (18300, 2500, 12500), (17550, 1700, 13850), (16400, 1500, 13950) };
    // (Twinsen's own, Zoe's and Raph's labels and behaviours there: new)
    internal const int RaphHome = 170, RaphHomeDone = 171, RaphGoes = 2, WizardGoes = 5, WizardGone = 12, WizardHomeLabel = 11;
    private const int ZoeDown = 190, ZoeGoneDown = 191;
    private static void LighthouseEvening(StoryParts p)
    {
        var zoe = Copy(p.Originals, OpeningScene, Zoe);
        if (zoe is null) { p.Log.Add("the lighthouse's evening left out: no Zoe (scene 0's actor 4)"); return; }
        p.Edit(LighthouseScene, "after the spell: Twinsen and Zoe on the top together, Raph down the hill, the Wizard home; going down, Zoe's \"coming home?\"", model =>
        {
            var first = model.TrackPoints.Count;
            model.TrackPoints.Add(new SceneTrackPoint(LampRoomDoor.X, LampRoomDoor.Y, LampRoomDoor.Z));
            foreach (var (x, y, z) in RaphDownHill) model.TrackPoints.Add(new SceneTrackPoint(x, y, z));
            int door = first, downHill = first + 1;
            (zoe.X, zoe.Y, zoe.Z) = ZoeOnTop;
            zoe.Beta = 0;
            var zoeIndex = model.Actors.Count;
            int ask = EveningId(p.Say, EveningLine.ZoeComingHome), yes = EveningId(p.Say, EveningLine.YesHome), soon = EveningId(p.Say, EveningLine.HomeSoon),
                ahead = EveningId(p.Say, EveningLine.ZoeAhead);
            // (her question: from her own script when Twinsen talks to her, from the climb's helper when he goes to go down -- "Yes" and they
            // are arm in arm, her own body gone into theirs, and down; "soon" and she goes down ahead of him)
            string Question(bool self, string down) => $@"set_var_game({DayVar}, {TiredAside});
            add_choice({yes});
            add_choice({soon});
            ask_choice_obj({zoeIndex}, {ask});
            set_var_game({QEvening}, {ZoeLeftTop});
            if ({yes} == choice())
            {{
                set_var_game({DayVar}, {Tired});
                comportement_hero({TwinsenAndZoe});
                {down}
                {(self ? "invisible(1);\n                suicide();" : $"set_comportement_obj({zoeIndex}, comportement_4);")}
            }}
            else
            {{
                message_obj({zoeIndex}, {ahead});
                set_var_game({DayVar}, {Tired});
                {(self ? $"set_track(label_{ZoeDown});\n                set_comportement(comportement_3);" : $"set_track_obj({zoeIndex}, label_{ZoeDown});\n                set_comportement_obj({zoeIndex}, comportement_3);")}
            }}";
            // (her own Action's "Yes": down together at once, as the helper takes Twinsen down -- its index known once the scripts are read)
            string ZoeLife(string down) => $@"void comportement_0()
{{
    if (2 > chapter() && 3 == var_game({KeeperPlot}))
    {{
        body(18);
        invisible(1);
        set_comportement(comportement_1);
    }}
    else if (2 > chapter() && {Tired} == var_game({DayVar}) && 0 == var_game({QEvening}))
    {{
        body(18);
        set_comportement(comportement_2);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
    if (1 == var_cube({Once5}))
    {{
        invisible(0);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    swif (1 == action())
    {{
        if (900 > distance(0) && {Driving} != comportement_hero() && {TwinsenAndZoe} != comportement_hero() && 0 == var_game({QEvening}))
        {{
            {Question(true, down)}
        }}
    }}
}}

void comportement_3()
{{
    if ({ZoeGoneDown} == l_track())
    {{
        invisible(1);
        suicide();
    }}
}}

void comportement_4()
{{
    invisible(1);
    suicide();
}}
";
            var zoeLife = p.Add(model, zoe, ZoeLife(""));
            return scripts =>
            {
                zoeLife(scripts);
                scripts.SetText(zoeIndex, ScriptKind.Track, $@"label(0);
anim(88);
stop();

label({ZoeDown});
anim(89);
goto_point({door});

label({ZoeGoneDown});
stop();
");
                // the spell's end (Chapters.cs Lighthouse: Twinsen himself again on the top, the ways out open): the two of them in one body
                // there (actor 12) each their own again -- Zoe beside him -- Raph off down the hill, the Wizard home
                var hero = scripts.GetText(0, ScriptKind.Life);
                hero = Loose(hero, "set_change_cube(1, 1);", $@"set_change_cube(1, 1);
        kill_obj(12);
        set_var_cube({Once5}, 1);
        set_track_obj({LighthouseKeeper}, label_{RaphHome});
        set_comportement_obj({LighthouseKeeper}, comportement_{RaphGoes});
        set_comportement_obj({LighthouseWizard}, comportement_{WizardGoes});");
                scripts.SetText(0, ScriptKind.Life, hero);
                // Raph (actor 5): at the door only while the spell is to come (on a later visit he's gone back to his car); after it, off down
                // the hill, out of sight past the bend
                var raph = scripts.GetText(LighthouseKeeper, ScriptKind.Life);
                if (!raph.Contains("if (3 > var_game(56))")) throw new NotTheGames("the lighthouse's Raph");
                raph = Loose(raph, "if (3 > var_game(56))", "if (3 != var_game(56))");
                scripts.SetText(LighthouseKeeper, ScriptKind.Life, raph.TrimEnd() + $@"

void comportement_{RaphGoes}()
{{
    if ({RaphHomeDone} == l_track())
    {{
        invisible(1);
        suicide();
    }}
}}
");
                scripts.SetText(LighthouseKeeper, ScriptKind.Track, scripts.GetText(LighthouseKeeper, ScriptKind.Track).TrimEnd() + $@"

label({RaphHome});
anim(1);
{string.Join("\n", Enumerable.Range(downHill, RaphDownHill.Length).Select(k => $"goto_point({k});"))}

label({RaphHomeDone});
stop();
");
                // the Wizard (actor 4): there for the spell only, not after it in the storm; after it, his word and home in a flash
                var wizard = scripts.GetText(LighthouseWizard, ScriptKind.Life);
                wizard = Loose(wizard, "else if (2 > chapter() && 2 < var_game(56))", "else if (2 > chapter() && 3 == var_game(56))");
                scripts.SetText(LighthouseWizard, ScriptKind.Life, wizard.TrimEnd() + $@"

void comportement_{WizardGoes}()
{{
    {Aside($"message({EveningId(p.Say, EveningLine.WizardHome)});")}
    set_track(label_{WizardHomeLabel});
    set_comportement(comportement_{WizardGoes + 1});
}}

void comportement_{WizardGoes + 1}()
{{
    if ({WizardGone} == l_track())
    {{
        eclair(7);
        invisible(1);
        suicide();
    }}
}}
");
                scripts.SetText(LighthouseWizard, ScriptKind.Track, scripts.GetText(LighthouseWizard, ScriptKind.Track).TrimEnd() + $@"

label({WizardHomeLabel});
anim(266);
wait_anim();
anim(0);

label({WizardGone});
stop();
");
                // the climb's helper (LighthouseClimb): Twinsen going down from the top, Zoe there -- her question first
                var helper = Enumerable.Range(1, scripts.ActorCount - 1).FirstOrDefault(i => scripts.GetText(i, ScriptKind.Life).Contains($"set_track_obj(0, label_{Down});"));
                if (helper <= 0) throw new NotTheGames("the climb's helper");
                scripts.SetText(zoeIndex, ScriptKind.Life, ZoeLife($"set_dir_obj(0, NO_MOVE);\n                set_track_obj(0, label_{Down});\n                set_comportement_obj({helper}, comportement_2);"));
                var down = $"set_dir_obj(0, NO_MOVE);\n            set_track_obj(0, label_{Down});\n            set_comportement(comportement_2);";
                var text = scripts.GetText(helper, ScriptKind.Life);
                text = Loose(text, $"else if (512 < beta_obj(0) && 1536 > beta_obj(0))\n{{\nset_dir_obj(0, NO_MOVE);\nset_track_obj(0, label_{Down});\nset_comportement(comportement_2);\n}}",
                    $@"else if (512 < beta_obj(0) && 1536 > beta_obj(0))
                {{
                    if (2 > chapter() && {Tired} == var_game({DayVar}) && 0 == var_game({QEvening}))
                    {{
                        {Question(false, down)}
                    }}
                    else
                    {{
                        {down}
                    }}
                }}");
                scripts.SetText(helper, ScriptKind.Life, text);
            };
        });
    }

    // ---- home at night (scene NightHouse; scene 0) -----------------------------------------------------------------------------------------
    // The day's house sends Twinsen to the night's while he is tired (whichever way he comes in: the front door, the attic's stairs, a game
    // loaded there). The night's: Twinsen come in with Zoe arm in arm is himself again and she beside him by the door; or she is waiting by
    // the dining room's chairs. His bed (zone 5 round its three open sides: the left, the foot, the right below the bedside table -- and on
    // it, should he jump up): "What a day!", the screen dark a moment, he is in bed; Zoe's goodnight and his, the candle out (the screen
    // goes black), and it is the night (DayVar Night): the day's house, his bed, the morning (Morning). His body stays what it was.
    private static readonly (int X, int Y, int Z) ZoeWaiting = (10240, 2048, 3584), ZoeByTheDoor = (12032, 768, 9728);
    // the bed's three open sides, a cell and a half deep (round the bed's cells x 9-11, z 1-4: x 7.5-12 and z 1-6.5; the right side only
    // below the bedside table, z 2 on, to x 13.5), on the upper floor (layer 7, 2048) up to standing on the bed (3072) and a little over
    internal static readonly (int X0, int X1, int Z0, int Z1)[] BedSides = { (3840, 6143, 512, 3327), (6144, 6911, 1024, 3327) };
    internal const int BedY0 = 2048, BedY1 = 3839;
    private const int IntoBed = 200, InBed = 201, Goodnight = 202, CandleOut = 203;
    // the candle's flame on the bedside table (Citadel/CitadelNightHouse's candle: cell 12, layer 12, z 1 -- its wick's top), flickering
    // between the props' two flames a tenth of a second at a time, and out at the goodnights (the user, 2026-11-07: "make sure the
    // candle is actually lit")
    private static readonly (int X, int Y, int Z) Wick = (12 * 512 + 276, 12 * 256 + 220, 1 * 512 + 236);
    private static void NightHouseStory(StoryParts p)
    {
        if (!p.Store.SceneExists(NightHouse)) { p.Log.Add($"home at night left out: no scene {NightHouse} (Citadel/CitadelNightHouse)"); return; }
        var zoe = Copy(p.Originals, OpeningScene, Zoe);
        if (zoe is null) { p.Log.Add("home at night left out: no Zoe (scene 0's actor 4)"); return; }
        p.Edit(OpeningScene, $"the day's house sends Twinsen to the night's (scene {NightHouse}) while he is tired", _ => scripts =>
        {
            var hero = scripts.GetText(0, ScriptKind.Life);
            hero = Loose(hero, "void comportement_0()\n{", $@"void comportement_0()
{{
    if ({Tired} == var_game({DayVar}))
    {{
        change_cube({NightHouse});
    }}
    else");
            scripts.SetText(0, ScriptKind.Life, hero);
        });
        p.Edit(NightHouse, "the house at night: Zoe home, the bed from its three sides, into bed, the candle out, the night", model =>
        {
            foreach (var (x0, x1, z0, z1) in BedSides)
                model.Zones.Add(new SceneZoneModel { Type = 2, Num = BedZone, Info = new[] { 0, 0, 0, 0, 0, 0, 0, 1 }, X0 = x0, X1 = x1, Z0 = z0, Z1 = z1, Y0 = BedY0, Y1 = BedY1 });
            var first = model.TrackPoints.Count;
            model.TrackPoints.Add(new SceneTrackPoint(BedSpot.X, BedSpot.Y, BedSpot.Z));
            model.TrackPoints.Add(new SceneTrackPoint(ZoeByTheDoor.X, ZoeByTheDoor.Y, ZoeByTheDoor.Z));
            int bed = first, byDoor = first + 1;
            (zoe.X, zoe.Y, zoe.Z) = ZoeWaiting;
            zoe.Beta = RaceTrackMath.Turn(Math.Atan2(BedSpot.X - ZoeWaiting.X, BedSpot.Z - ZoeWaiting.Z));
            var flame = Citadel.CitadelProps.Actor(Citadel.CitadelProps.Flame, Wick.X, Wick.Y, Wick.Z);
            var flameIndex = model.Actors.Count;
            var flameLife = p.Add(model, flame, $@"void comportement_0()
{{
    if ({Tired} != var_game({DayVar}))
    {{
        suicide();
    }}
}}
");
            var zoeIndex = model.Actors.Count;
            var zoeLife = p.Add(model, zoe, $@"void comportement_0()
{{
    if (2 > chapter() && {Tired} == var_game({DayVar}))
    {{
        body(0);
        if (1 == var_cube({Once5}) || {TwinsenAndZoe} == comportement_hero())
        {{
            pos_point({byDoor});
            beta(3072);
        }}
        set_comportement(comportement_1);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if (1200 > distance(0) && {Driving} != comportement_hero() && {Tired} == var_game({DayVar}))
        {{
            {Aside($"message({EveningId(p.Say, EveningLine.ZoeAtHome)});")}
        }}
    }}
}}
");
            return scripts =>
            {
                flameLife(scripts);
                scripts.SetText(flameIndex, ScriptKind.Track, $@"label(0);
body({Citadel.CitadelProps.Flame});
wait_nb_dizieme(1);
body({Citadel.CitadelProps.Flame2});
wait_nb_dizieme(1);
goto(label_0);
");
                zoeLife(scripts);
                scripts.SetText(0, ScriptKind.Life, $@"void comportement_0()
{{
    if ({TwinsenAndZoe} == comportement_hero())
    {{
        set_var_cube({Once5}, 1);
        comportement_hero(0);
    }}
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if ({BedZone} == zone() && {Driving} != comportement_hero() && {Tired} == var_game({DayVar}))
        {{
            set_dir(NO_MOVE);
            cinema_mode(1);
            message({p.Said(Line.Sleep)});
            fade_to_pal(3);
            shadow_obj(0, 0);
            pos_point({bed});
            beta(0);
            anim({Asleep});
            set_track(label_{IntoBed});
            set_comportement(comportement_2);
        }}
    }}
}}

void comportement_2()
{{
    if ({InBed} == l_track())
    {{
        fade_to_pal(1);
        set_track(label_{Goodnight});
        set_comportement(comportement_3);
    }}
}}

void comportement_3()
{{
    if ({CandleOut} == l_track())
    {{
        {Aside($"message_obj({zoeIndex}, {EveningId(p.Say, EveningLine.ZoeGoodnight)});")}
        message({EveningId(p.Say, EveningLine.TwinsenGoodnight)});
        kill_obj({flameIndex});
        fade_to_pal(3);
        set_var_game({DayVar}, {Night});
        change_cube({OpeningScene});
    }}
}}
");
                scripts.SetText(0, ScriptKind.Track, $@"label(0);
stop();

label({IntoBed});
anim({Asleep});
wait_nb_dizieme(3);

label({InBed});
stop();

label({Goodnight});
anim({Asleep});
wait_nb_dizieme(15);

label({CandleOut});
stop();
");
                scripts.SetText(zoeIndex, ScriptKind.Track, "label(0);\nanim(0);\nstop();\n");
            };
        });
    }
}
