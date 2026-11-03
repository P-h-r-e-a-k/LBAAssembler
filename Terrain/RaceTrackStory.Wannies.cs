using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Chapter 9 of the story (docs/LBRA_WALKTHROUGH.md), Wannies Island (PLATFORM, outside scenes 96-99; Twinsen comes down the elevator into
// scene 98):
//   - arrival: Lyra's word (Q_GLOVE 1), then Baldino's call on the radio (GroupB.Arrival, in every outside scene: var 236 9);
//   - the Wannie miner (a Wannie of the game's, scene 96's actor 2, copied) by the start: no glove, no driving (Q_GLOVE 2), Zoe's word;
//   - the old Wannie (scene 96's actor 3, copied) on the ground under the frog: the glove's just inside its mouth;
//   - the glove (Wannie's glove, item 11) at the frog's mouth on the lap: Twinsen, on foot or driving, within reach of it (Q_GLOVE 3).
//     The race-track mode's frog opens and shuts on its own clock, which no script can read: the glove is there whether the mouth is open
//     or not (the engine would need to give the mouth's state to the scripts, a game variable, for "grab it while it's open");
//   - the miner again: his glove back, to the grid (item 11 0; Q_GLOVE 4: the start line's gate, RaceCarEngineFile);
//   - the race (finish= 237, win= chapter 10). Won: Lyra, the Fragment of the Wannies (item 34), the old Wannie's clue, and the tunnel to
//     Mosquibees Island (scene 102).
internal static partial class RaceTrackStory
{
    // Q_GLOVE: 1 Lyra's word heard, 2 the miner has told, 3 the glove found, 4 the glove given back (the start line's gate)
    public const int QGlove = 220, MinerTold = 2, GloveFound = 3, GloveBack = 4;
    private const int WanniesChapter = 9, WanniesTexts = 3 + 6, WannieGlove = 11, WanniesFragment = 34, MosquibeesArrival = 102;
    private const int WannieScene = 96, MinerActor = 2, OldWannieActor = 3;

    internal enum WanniesLine { LyraArrival, Ring, Baldino, Miner, ZoeGloves, OldWannie, MinerThanks, LyraFragment, OldWannieClue }
    internal static readonly string[][] WanniesLines =
    {
        new[]
        {
            "The Wannies' race is off. Their best driver won't drive.",
            "La course des Blafards est annulée. Leur meilleur pilote refuse de conduire.",
            "Das Rennen der Bleichlinge fällt aus. Ihr bester Fahrer will nicht fahren.",
            "La carrera de los Macilentos se ha suspendido. Su mejor piloto no quiere conducir.",
            "La corsa dei Pallidi è annullata. Il loro miglior pilota non vuole guidare.",
            "A corrida dos Blafards foi cancelada. O melhor piloto deles não quer conduzir.",
        },
        GroupB.Ring,
        new[]
        {
            "Baldino here! The frog track! A lighter car for the frog's mouth, and a tighter steering rack for the tower's spiral. Don't get eaten.",
            "Ici Baldino ! Le circuit de la grenouille ! Une voiture plus légère pour la gueule de la grenouille, et une direction plus serrée pour la spirale de la tour. Ne te fais pas manger.",
            "Hier Baldino! Die Froschstrecke! Ein leichterer Wagen für das Froschmaul und eine straffere Lenkung für die Spirale am Turm. Lass dich nicht fressen.",
            "¡Aquí Baldino! ¡El circuito de la rana! Un coche más ligero para la boca de la rana, y una dirección más firme para la espiral de la torre. Que no te coman.",
            "Qui Baldino! La pista della rana! Una macchina più leggera per la bocca della rana, e uno sterzo più stretto per la spirale della torre. Non farti mangiare.",
            "Fala o Baldino! A pista da rã! Um carro mais leve para a boca da rã, e uma direção mais apertada para a espiral da torre. Não te deixes comer.",
        },
        new[]
        {
            "No glove, no driving. Safety first! They say that on Citadel too, I hear. I lost it by the frog. The frog keeps snapping at me.",
            "Pas de gant, pas de conduite. La sécurité d'abord ! C'est ce qu'on dit à la Citadelle aussi, paraît-il. Je l'ai perdu près de la grenouille. Elle n'arrête pas de vouloir me croquer.",
            "Kein Handschuh, kein Fahren. Sicherheit geht vor! Das sagt man auf der Zitadelle auch, habe ich gehört. Ich habe ihn beim Frosch verloren. Der Frosch schnappt dauernd nach mir.",
            "Sin guante, no conduzco. ¡La seguridad es lo primero! Dicen que en la Ciudadela también, he oído. Lo perdí junto a la rana. La rana no para de intentar morderme.",
            "Niente guanto, niente guida. Prima la sicurezza! Dicono così anche alla Cittadella, ho sentito. L'ho perso vicino alla rana. La rana continua a cercare di mordermi.",
            "Sem luva, não conduzo. A segurança primeiro! Também se diz isso na Cidadela, ouvi dizer. Perdi-a ao pé da rã. A rã não para de me tentar abocanhar.",
        },
        new[]
        {
            "Gloves again. Everyone on these islands loses their gloves.",
            "Encore des gants. Tout le monde perd ses gants, sur ces îles.",
            "Schon wieder Handschuhe. Auf diesen Inseln verliert jeder seine Handschuhe.",
            "Guantes otra vez. Todo el mundo pierde los guantes en estas islas.",
            "Ancora guanti. Su queste isole tutti perdono i guanti.",
            "Luvas outra vez. Toda a gente perde as luvas nestas ilhas.",
        },
        new[]
        {
            "The frog opens its mouth every few seconds. Your glove's just inside. Be quick, dear.",
            "La grenouille ouvre la gueule toutes les quelques secondes. Ton gant est juste à l'intérieur. Fais vite, mon petit.",
            "Der Frosch öffnet alle paar Sekunden sein Maul. Dein Handschuh liegt gleich dahinter. Sei flink, mein Lieber.",
            "La rana abre la boca cada pocos segundos. Tu guante está justo dentro. Date prisa, querido.",
            "La rana apre la bocca ogni pochi secondi. Il tuo guanto è appena dentro. Fai presto, caro.",
            "A rã abre a boca de poucos em poucos segundos. A tua luva está mesmo lá dentro. Sê rápido, querido.",
        },
        new[]
        {
            "My glove! Right, you. To the grid.",
            "Mon gant ! Bon, toi. Sur la grille.",
            "Mein Handschuh! Also gut, du. Ab in die Startaufstellung.",
            "¡Mi guante! Muy bien, tú. A la parrilla.",
            "Il mio guanto! Bene, tu. In griglia.",
            "A minha luva! Muito bem, tu. Para a grelha.",
        },
        new[]
        {
            "The Fragment of the Wannies!",
            "Le Fragment des Blafards !",
            "Das Fragment der Bleichlinge!",
            "¡El Fragmento de los Macilentos!",
            "Il Frammento dei Pallidi!",
            "O Fragmento dos Blafards!",
        },
        new[]
        {
            "The black car's driver paid us to dig him a tunnel to Mosquibees Island. Paid in coins with the Dark Monk's face on them. The tunnel's finished: you may as well use it.",
            "Le pilote de la voiture noire nous a payés pour lui creuser un tunnel jusqu'à l'île des Mosquibees. Payés en pièces à l'effigie de Dark Monk. Le tunnel est fini : autant que tu t'en serves.",
            "Der Fahrer des schwarzen Wagens hat uns bezahlt, damit wir ihm einen Tunnel zur Insel der Moskibienos graben. Bezahlt mit Münzen, auf denen Dark Monks Gesicht ist. Der Tunnel ist fertig: Du kannst ihn ruhig benutzen.",
            "El piloto del coche negro nos pagó para que le caváramos un túnel hasta la isla de los Mosquibitas. Nos pagó con monedas con la cara de Dark Monk. El túnel está terminado: ya puedes usarlo.",
            "Il pilota della macchina nera ci ha pagati per scavargli un tunnel fino all'isola dei Moschibei. Pagati con monete con la faccia di Dark Monk. Il tunnel è finito: tanto vale che tu lo usi.",
            "O piloto do carro preto pagou-nos para lhe cavarmos um túnel até à ilha dos Mosquibees. Pagou com moedas com a cara de Dark Monk. O túnel está acabado: mais vale usá-lo.",
        },
    };

    private static List<string> ApplyWannies(string gameDirectory, RaceTrackReport report, RaceTrackScenes.Result scenes)
    {
        var log = new List<string>();
        var first = GroupB.AddLines(gameDirectory, WanniesTexts, WanniesLines, log, "Wannies Island's (chapter 9)");
        int Say(WanniesLine l) => first + (int)l;
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var originals = RaceTrackService.OriginalScenes(store);
        var island = RaceTrackIsland.Wannies;
        var when = $"{WanniesChapter} == chapter()";
        var oldWannie = Copy(originals, WannieScene, OldWannieActor);
        // the arrival and the radio, and after the race, in each outside scene
        var won = $@"            message({Say(WanniesLine.LyraFragment)});
            found_object({WanniesFragment});
            set_var_game({WanniesFragment}, 1);
            message({Say(WanniesLine.OldWannieClue)});
            change_cube({MosquibeesArrival});";
        for (var s = island.FirstScene; s <= island.LastScene; s++)
            GroupB.EditScene(store, s, log, "Lyra's word on arrival (Q_GLOVE 1), Baldino's call on the radio, and after the race won the fragment, the old Wannie's clue and the tunnel", model =>
            {
                var arrival = GroupB.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 9),
                    GroupB.Arrival(when, WanniesChapter, $"0 == var_game({QGlove})", $"set_var_game({QGlove}, 1);", Say(WanniesLine.LyraArrival), Say(WanniesLine.Ring), Say(WanniesLine.Baldino)));
                var after = GroupB.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, oldWannie?.CoulObj ?? 9),
                    GroupB.AfterRace(WanniesChapter, won));
                return scripts => { arrival(scripts); after(scripts); };
            });

        // the miner on the ground by the foot of the elevator, where Twinsen comes down (scene 98's start: the start line is on the raised
        // road over the pier, where no one stands on foot), facing it
        var arrival = originals.Load(WanniesArrival);
        double ax = arrival.CubeX * 64 + arrival.Hero.X / 512.0, az = arrival.CubeY * 64 + arrival.Hero.Z / 512.0;
        var miner = Copy(originals, WannieScene, MinerActor);
        if (miner is not null && GroupB.SpotAt(store, island, report, ax + 2, az + 2, arrival.Hero.Y, ax, az) is { } m)
            GroupB.EditScene(store, m.Scene, log, $"the Wannie miner at cell ({m.At.X:0.#}, {m.At.Z:0.#}): no glove, no driving; his glove back (Q_GLOVE 2, 4)", model =>
            {
                Place(miner, model, m.At);
                return GroupB.Add(model, miner, Talker(when, $@"            if ({GloveFound} == var_game({QGlove}))
            {{
                message({Say(WanniesLine.MinerThanks)});
                set_var_game({WannieGlove}, 0);
                set_var_game({QGlove}, {GloveBack});
            }}
            else if ({GloveBack} > var_game({QGlove}))
            {{
                message({Say(WanniesLine.Miner)});
                if ({MinerTold} > var_game({QGlove}))
                {{
                    set_var_game({QGlove}, {MinerTold});
                    message_zoe({Say(WanniesLine.ZoeGloves)});
                }}
            }}
            else
            {{
                message({Say(WanniesLine.MinerThanks)});
            }}"));
            });
        else log.Add("the Wannie miner left out: no room off the road by the start line");

        // the frog: the glove at its mouth, the old Wannie on the ground under it
        if (report.Frogs.Count == 0) { log.Add("the glove and the old Wannie left out: no frog on the lap"); return log; }
        var frog = report.Frogs[0];
        var frogScene = GroupB.OutsideScene(store, island, frog.X, frog.Z);
        if (frogScene >= 0)
            GroupB.EditScene(store, frogScene, log, $"Wannie's glove (item {WannieGlove}) at the frog's mouth, cell ({frog.X:0.#}, {frog.Z:0.#}), {frog.Y:0} (Q_GLOVE 3)", model =>
            {
                var glove = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(frog.X, model.CubeX), (int)Math.Round(frog.Y), RaceTrackScenes.CubeLocal(frog.Z, model.CubeY), 9);
                return GroupB.Add(model, glove, $@"void comportement_0()
{{
    if ({when} && {GloveFound} > var_game({QGlove}))
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
    if ({MinerTold} == var_game({QGlove}) && 1500 > distance(0))
    {{
        found_object({WannieGlove});
        set_var_game({WannieGlove}, 1);
        set_var_game({QGlove}, {GloveFound});
        suicide();
    }}
}}
");
            });
        else log.Add("Wannie's glove left out: the frog is in no outside scene");
        if (oldWannie is not null && GroupB.Spot(store, island, report, frog.X - frog.Tx * 3, frog.Z - frog.Tz * 3, frog.X, frog.Z, 1.5, 16) is { } w)
            GroupB.EditScene(store, w.Scene, log, $"the old Wannie under the frog at cell ({w.At.X:0.#}, {w.At.Z:0.#}): the glove's just inside its mouth", model =>
            {
                Place(oldWannie, model, w.At);
                return GroupB.Add(model, oldWannie, Talker(when, $"            message({Say(WanniesLine.OldWannie)});"));
            });
        else log.Add("the old Wannie left out: no room off the road under the frog");
        return log;
    }
}
