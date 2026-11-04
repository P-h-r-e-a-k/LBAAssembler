using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Little Big Postcards (the user, 2026-11-04, of the easter-egg proposal's picks: "Let's make all those changes" -- the proposal's "A.
// The Little Big Postcards"): the souvenir seller of Celebration Island lost his postcards of every island to the Cup's wind, one on each
// island the story races, chapters 1-13 -- a sheet of paper (SPRITES.HQR 238) off the road somewhere a racer wouldn't look, a little over
// half way round the lap. Found -- driven or walked within reach of -- its picture's caption, a joke about the place; in chapter 6, on
// Celebration Island itself, the seller calls out that it's one of his. All thirteen, and in the final (chapter 14) he gives Twinsen a
// car for Track select: the black car in gold (RaceTrackCharacterCars.GoldCar; the race-track mode's unlock_at=, "postcards").
// Each card is there in its chapter only, and only while EggLastCard is below the chapter: found once (EggCards counts them).
internal static partial class RaceTrackStory
{
    public const int PostcardCount = 13, PostcardsRewarded = 99;
    public const string PostcardUnlock = "postcards";
    // (how far round the lap, and how far off the roads)
    private const double CardAlong = 0.58, CardClear = 2.5, CardReach = 16;
    private const int CardReachUnits = 1000, CardSeller = 50;

    // each chapter's caption, in its island's text file
    private static readonly string[][] Captions =
    {
        new[]
        {
            "A postcard: \"Greetings from Citadel Island! It's lovely here when it stops raining. It never stops raining.\"",
            "Une carte postale : « Bons baisers de l'île de la Citadelle ! C'est charmant quand il arrête de pleuvoir. Il n'arrête jamais de pleuvoir. »",
            "Eine Postkarte: \"Grüße von der Zitadelleninsel! Es ist herrlich hier, wenn es aufhört zu regnen. Es hört nie auf zu regnen.\"",
            "Una postal: \"¡Saludos desde la isla de la Ciudadela! Es precioso cuando deja de llover. Nunca deja de llover.\"",
            "Una cartolina: \"Saluti dall'isola della Cittadella! È bellissimo quando smette di piovere. Non smette mai di piovere.\"",
            "Um postal: \"Saudações da ilha da Cidadela! É lindo quando para de chover. Nunca para de chover.\"",
        },
        new[]
        {
            "A postcard: \"Greetings from Citadel Island! Visit our Tavern, exactly as it was. Exactly.\"",
            "Une carte postale : « Bons baisers de l'île de la Citadelle ! Visitez notre Taverne, exactement comme avant. Exactement. »",
            "Eine Postkarte: \"Grüße von der Zitadelleninsel! Besucht unsere Taverne, genau wie früher. Genau.\"",
            "Una postal: \"¡Saludos desde la isla de la Ciudadela! Visite nuestra Taberna, exactamente como era. Exactamente.\"",
            "Una cartolina: \"Saluti dall'isola della Cittadella! Visitate la nostra Taverna, esattamente com'era. Esattamente.\"",
            "Um postal: \"Saudações da ilha da Cidadela! Visite a nossa Taberna, exatamente como era. Exatamente.\"",
        },
        new[]
        {
            "A postcard: \"Greetings from the White Leaf Desert! Sand in the post? Sorry.\"",
            "Une carte postale : « Bons baisers du désert de la Feuille Blanche ! Du sable dans le courrier ? Désolé. »",
            "Eine Postkarte: \"Grüße aus der Weißblattwüste! Sand in der Post? Tut mir leid.\"",
            "Una postal: \"¡Saludos desde el desierto de la Hoja Blanca! ¿Arena en el correo? Lo siento.\"",
            "Una cartolina: \"Saluti dal deserto della Foglia Bianca! Sabbia nella posta? Scusate.\"",
            "Um postal: \"Saudações do deserto da Folha Branca! Areia no correio? Desculpem.\"",
        },
        new[]
        {
            "A postcard: \"Greetings from the Emerald Moon! The view is out of this world. The food isn't.\"",
            "Une carte postale : « Bons baisers de la Lune d'Émeraude ! La vue est hors du commun. La cuisine, non. »",
            "Eine Postkarte: \"Grüße vom Smaragdmond! Die Aussicht ist nicht von dieser Welt. Das Essen schon.\"",
            "Una postal: \"¡Saludos desde la Luna Esmeralda! Las vistas son de otro mundo. La comida no.\"",
            "Una cartolina: \"Saluti dalla Luna di Smeraldo! La vista è fuori dal mondo. Il cibo no.\"",
            "Um postal: \"Saudações da Lua Esmeralda! A vista é de outro mundo. A comida não.\"",
        },
        new[]
        {
            "A postcard: \"Greetings from Otringal! Wish you were richer.\"",
            "Une carte postale : « Bons baisers d'Otringal ! Dommage que tu ne sois pas plus riche. »",
            "Eine Postkarte: \"Grüße aus Otringal! Schade, dass du nicht reicher bist.\"",
            "Una postal: \"¡Saludos desde Otringal! Ojalá fueras más rico.\"",
            "Una cartolina: \"Saluti da Otringal! Peccato che tu non sia più ricco.\"",
            "Um postal: \"Saudações de Otringal! Quem nos dera que fosses mais rico.\"",
        },
        new[]
        {
            "A postcard: \"Greetings from Celebration Island! Mind the lava. And the statue. Mostly the statue.\"",
            "Une carte postale : « Bons baisers de l'île de la Célébration ! Attention à la lave. Et à la statue. Surtout à la statue. »",
            "Eine Postkarte: \"Grüße von der Insel der Feierlichkeiten! Vorsicht vor der Lava. Und vor der Statue. Vor allem vor der Statue.\"",
            "Una postal: \"¡Saludos desde la isla de la Celebración! Cuidado con la lava. Y con la estatua. Sobre todo con la estatua.\"",
            "Una cartolina: \"Saluti dall'isola della Celebrazione! Attenti alla lava. E alla statua. Soprattutto alla statua.\"",
            "Um postal: \"Saudações da ilha da Celebração! Cuidado com a lava. E com a estátua. Sobretudo com a estátua.\"",
        },
        new[]
        {
            "A postcard: \"Greetings from the Island of the Francos! Everything here is twice the size. Even this postcard.\"",
            "Une carte postale : « Bons baisers de l'île des Francos ! Tout est deux fois plus grand ici. Même cette carte. »",
            "Eine Postkarte: \"Grüße von der Insel der Francos! Hier ist alles doppelt so groß. Sogar diese Karte.\"",
            "Una postal: \"¡Saludos desde la isla de los Francos! Aquí todo es el doble de grande. Hasta esta postal.\"",
            "Una cartolina: \"Saluti dall'isola dei Franco! Qui tutto è grande il doppio. Anche questa cartolina.\"",
            "Um postal: \"Saudações da ilha dos Francos! Aqui tudo é o dobro do tamanho. Até este postal.\"",
        },
        new[]
        {
            "A postcard: \"Greetings from the Elevator Platform! It's all downhill from here.\"",
            "Une carte postale : « Bons baisers de la plate-forme de l'ascenseur ! À partir d'ici, ça ne fait que descendre. »",
            "Eine Postkarte: \"Grüße von der Aufzugsplattform! Ab hier geht's nur noch bergab.\"",
            "Una postal: \"¡Saludos desde la plataforma del ascensor! A partir de aquí, todo es cuesta abajo.\"",
            "Una cartolina: \"Saluti dalla piattaforma dell'ascensore! Da qui in poi è tutta discesa.\"",
            "Um postal: \"Saudações da plataforma do elevador! Daqui para a frente, é sempre a descer.\"",
        },
        new[]
        {
            "A postcard: \"Greetings from Wannies Island! Bring a glove. Bring two.\"",
            "Une carte postale : « Bons baisers de l'île des Wannies ! Apportez un gant. Apportez-en deux. »",
            "Eine Postkarte: \"Grüße von der Insel der Wannies! Bring einen Handschuh mit. Bring zwei mit.\"",
            "Una postal: \"¡Saludos desde la isla de los Wannies! Trae un guante. Trae dos.\"",
            "Una cartolina: \"Saluti dall'isola dei Wannie! Portate un guanto. Portatene due.\"",
            "Um postal: \"Saudações da ilha dos Wannies! Tragam uma luva. Tragam duas.\"",
        },
        new[]
        {
            "A postcard: \"Greetings from Mosquibees Island! Bow to the Queen. Trust us.\"",
            "Une carte postale : « Bons baisers de l'île des Mosquibees ! Inclinez-vous devant la Reine. Faites-nous confiance. »",
            "Eine Postkarte: \"Grüße von der Insel der Moskibienos! Verbeugt euch vor der Königin. Glaubt uns.\"",
            "Una postal: \"¡Saludos desde la isla de los Mosquibitas! Haced una reverencia a la Reina. Fiaos de nosotros.\"",
            "Una cartolina: \"Saluti dall'isola dei Moschibei! Fate un inchino alla Regina. Fidatevi.\"",
            "Um postal: \"Saudações da ilha dos Mosquibees! Façam uma vénia à Rainha. Confiem em nós.\"",
        },
        new[]
        {
            "A postcard: \"Greetings from under Celebration Island! It's warm. Very warm.\"",
            "Une carte postale : « Bons baisers de sous l'île de la Célébration ! Il fait chaud. Très chaud. »",
            "Eine Postkarte: \"Grüße von unter der Insel der Feierlichkeiten! Es ist warm. Sehr warm.\"",
            "Una postal: \"¡Saludos desde debajo de la isla de la Celebración! Hace calor. Mucho calor.\"",
            "Una cartolina: \"Saluti da sotto l'isola della Celebrazione! Fa caldo. Molto caldo.\"",
            "Um postal: \"Saudações de debaixo da ilha da Celebração! Está calor. Muito calor.\"",
        },
        new[]
        {
            "A postcard: \"Greetings from Island CX! Not a soul about. Which is the worrying part.\"",
            "Une carte postale : « Bons baisers de l'îlot CX ! Pas âme qui vive. C'est bien ce qui inquiète. »",
            "Eine Postkarte: \"Grüße vom Eiland CX! Keine Menschenseele weit und breit. Genau das ist das Beunruhigende.\"",
            "Una postal: \"¡Saludos desde el islote CX! Ni un alma. Eso es lo preocupante.\"",
            "Una cartolina: \"Saluti dall'isolotto CX! Non c'è anima viva. Ed è proprio questo che preoccupa.\"",
            "Um postal: \"Saudações da Ilhota CX! Nem vivalma. É isso que preocupa.\"",
        },
        new[]
        {
            "A postcard: \"Greetings from the Emperor's palace! Please do not touch the vases.\"",
            "Une carte postale : « Bons baisers du palais de l'Empereur ! Prière de ne pas toucher aux vases. »",
            "Eine Postkarte: \"Grüße aus dem Palast des Kaisers! Bitte die Vasen nicht berühren.\"",
            "Una postal: \"¡Saludos desde el palacio del Emperador! Por favor, no toquen los jarrones.\"",
            "Una cartolina: \"Saluti dal palazzo dell'Imperatore! Si prega di non toccare i vasi.\"",
            "Um postal: \"Saudações do palácio do Imperador! Por favor, não toquem nos vasos.\"",
        },
    };
    // the seller's: in chapter 6, his card found on his own island; in the final, all thirteen found
    private static readonly string[][] SellerCards =
    {
        new[]
        {
            "The souvenir seller: \"That's one of my postcards! The wind took the lot, thirteen of them, one from every island. Bring them all back and I'll make it worth your while.\"",
            "Le vendeur de souvenirs : « C'est une de mes cartes postales ! Le vent les a toutes emportées, treize, une de chaque île. Rapporte-les-moi toutes et tu ne le regretteras pas. »",
            "Der Souvenirverkäufer: \"Das ist eine meiner Postkarten! Der Wind hat alle mitgenommen, dreizehn, eine von jeder Insel. Bring sie mir alle zurück, und es soll dein Schaden nicht sein.\"",
            "El vendedor de recuerdos: \"¡Es una de mis postales! El viento se las llevó todas, trece, una de cada isla. Tráemelas todas y no te arrepentirás.\"",
            "Il venditore di souvenir: \"È una delle mie cartoline! Il vento se le è portate via tutte, tredici, una per isola. Riportamele tutte e non te ne pentirai.\"",
            "O vendedor de lembranças: \"É um dos meus postais! O vento levou-os todos, treze, um de cada ilha. Traz-mos todos e não te vais arrepender.\"",
        },
        new[]
        {
            "The souvenir seller, from the crowd: \"All thirteen postcards! I made a souvenir of that black car too, in gold. Nobody bought it. It's yours: look for it in Track select.\"",
            "Le vendeur de souvenirs, dans la foule : « Les treize cartes postales ! J'avais fait un souvenir de cette voiture noire aussi, en or. Personne ne l'a achetée. Elle est à toi : cherche-la dans le choix des circuits. »",
            "Der Souvenirverkäufer, aus der Menge: \"Alle dreizehn Postkarten! Ich habe auch von diesem schwarzen Wagen ein Souvenir gemacht, in Gold. Keiner hat es gekauft. Es gehört dir: Such es in der Streckenauswahl.\"",
            "El vendedor de recuerdos, entre el público: \"¡Las trece postales! También hice un recuerdo de ese coche negro, en oro. Nadie lo compró. Es tuyo: búscalo en la selección de circuitos.\"",
            "Il venditore di souvenir, dalla folla: \"Tutte e tredici le cartoline! Avevo fatto un souvenir anche di quella macchina nera, in oro. Nessuno l'ha comprato. È tuo: cercalo nella scelta dei circuiti.\"",
            "O vendedor de lembranças, do meio da multidão: \"Os treze postais! Também fiz uma lembrança daquele carro preto, em ouro. Ninguém a comprou. É tua: procura-a na escolha de pistas.\"",
        },
    };

    // The chapter each island's card is found in (its story's chapter), or -1
    private static int CardChapter(RaceTrackIsland island) =>
        island == RaceTrackIsland.Desert ? 3 : island == RaceTrackIsland.Emerald ? 4 : island == RaceTrackIsland.Otringal ? 5
        : island == RaceTrackIsland.CelebrationLava ? 6 : island == RaceTrackIsland.Knartas ? 7 : island == RaceTrackIsland.Elevator ? 8
        : island == RaceTrackIsland.Wannies ? 9 : island == RaceTrackIsland.Mosquibe ? 10 : island == RaceTrackIsland.Volcano ? 11
        : island == RaceTrackIsland.CxTowerMk2 || island == RaceTrackIsland.CxTower ? 12 : island == RaceTrackIsland.Palace ? 13 : -1;

    // An island's postcard (and on Celebration Island's final, the seller's reward): into its text file and an outside scene of its track.
    // `chapter`: Citadel Island's two are chapters 1 and 2 (the storm track's, the town circuit's); the others by the island. Lines for the log.
    internal static List<string> Postcards(string gameDirectory, RaceTrackIsland island, RaceTrackReport report, RaceTrackScenes.Result scenes, int chapter = -1)
    {
        var log = new List<string>();
        if (chapter < 0) chapter = CardChapter(island);
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (island == RaceTrackIsland.Celebration) { log.AddRange(PostcardReward(gameDirectory, store, island, scenes)); return log; }
        if (chapter < 1 || chapter > PostcardCount) return log;
        var lines = chapter == 6 ? new[] { Captions[chapter - 1], SellerCards[0] } : new[] { Captions[chapter - 1] };
        var first = GroupB.AddLines(gameDirectory, 3 + island.IslandByte, lines, log, $"chapter {chapter}'s postcard's");
        // (off the roads a little over half way round the lap; else beside the start line; else where the start scene starts Twinsen)
        (int Scene, int X, int Y, int Z)? at = null;
        int SceneAt(double x, double z) => island.Scenes.FirstOrDefault(s => store.SceneExists(s) && store.Load(s) is var m && m.CubeMode == 1
            && m.CubeX == (int)Math.Floor(x / 64) && m.CubeY == (int)Math.Floor(z / 64), -1);
        foreach (var (along, clear) in new[] { (CardAlong, CardClear), (CardAlong + 0.1, CardClear), (CardAlong - 0.15, 1.5), (0.0, 1.5) })
        {
            if (report.LapX.Length < 10) break;
            var i = (int)(along * report.LapX.Length) % report.LapX.Length;
            if (GroupB.OffRoadNear(report, report.LapX[i], report.LapZ[i], clear, CardReach) is not { } o) continue;
            var scene = SceneAt(o.X, o.Z);
            if (scene < 0) continue;
            var m = store.Load(scene);
            at = (scene, RaceTrackScenes.CubeLocal(o.X, m.CubeX), (int)Math.Round(o.Y), RaceTrackScenes.CubeLocal(o.Z, m.CubeY));
            break;
        }
        if (at is null && scenes.StartScene >= 0 && store.SceneExists(scenes.StartScene))
        {
            var hero = store.Load(scenes.StartScene).Hero;
            at = (scenes.StartScene, hero.X + 1024, hero.Y, hero.Z);
        }
        if (at is not { } a) { log.Add($"WARNING: chapter {chapter}'s postcard left out: nowhere off the roads, and no start scene"); return log; }
        GroupB.EditScene(store, a.Scene, log, $"chapter {chapter}'s postcard (EggCards, EggLastCard) at ({a.X}, {a.Y}, {a.Z})", model =>
        {
            var card = RaceTrackScenes.SpriteProp(Math.Clamp(a.X, 0, IslandFile.CubeSize - 1), a.Y, Math.Clamp(a.Z, 0, IslandFile.CubeSize - 1), RaceTrackScenes.PaperSprite, 9);
            return GroupB.Add(model, card, $@"void comportement_0()
{{
    if ({chapter} == chapter() && {chapter} > var_game({EggLastCard}))
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
    if ({CardReachUnits} > distance(0))
    {{
        set_var_game({EggLastCard}, {chapter});
        add_var_game({EggCards}, 1);
        message({first});
{(chapter == 6 ? $"        message({first + 1});\n" : "")}        suicide();
    }}
}}
");
        });
        return log;
    }

    // The final (Celebration Island, chapter 14): all thirteen found, the seller's word from the crowd as Twinsen is first on foot in any of
    // the island's outside scenes -- EggLastCard to PostcardsRewarded, which the race-track mode's unlock_at= unlocks the gold car by
    private static List<string> PostcardReward(string gameDirectory, SceneStore store, RaceTrackIsland island, RaceTrackScenes.Result scenes)
    {
        var log = new List<string>();
        var first = GroupB.AddLines(gameDirectory, 3 + island.IslandByte, new[] { SellerCards[1] }, log, "the postcards' reward's");
        foreach (var s in island.Scenes.Where(s => store.SceneExists(s) && store.Load(s).CubeMode == 1).ToList())
            GroupB.EditScene(store, s, log, "the postcards' reward: the gold black car (EggCards 13, EggLastCard to 99, unlock_at=)", model =>
                GroupB.Add(model, RaceTrackScenes.InvisibleHelper(IslandFile.CubeSize / 2, 0, IslandFile.CubeSize / 2, 9), $@"void comportement_0()
{{
    if (14 <= chapter() && {PostcardCount} <= var_game({EggCards}) && {PostcardsRewarded} > var_game({EggLastCard}))
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
    if ({Driving} != comportement_hero() && {PostcardsRewarded} > var_game({EggLastCard}))
    {{
        set_var_game({EggLastCard}, {PostcardsRewarded});
        message({first});
    }}
}}
"));
        return log;
    }
}
