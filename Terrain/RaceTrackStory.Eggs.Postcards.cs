using System.IO;
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
    // (the card's place: the ground under it this far round its middle (cells) level enough; the island's decor far enough off
    // (CardStrict); no floor of these game codes under it -- lava, water, the deadly ones: LavaLake's StatuetteGround's)
    private const double CardFoot = 0.7;
    // (tried strictest first: level within 300 and 500 from the decor; then 450 and 150; then 700 and just not in a decor)
    private static readonly (double Flat, double DecorGap)[] CardStrict = { (300, 500), (450, 150), (700, 0) };
    private static readonly HashSet<int> CardDeadly = new() { 1, 2, 8, 9, 11, 13, 14, 15 };
    // (cells round it with nothing deadly; a road this far over the ground there and this near (cells) has it out of sight; how far from
    // where Twinsen starts, away from the road, when there's no open ground)
    private const int CardDeadlyRound = 2;
    private const double CardUnderRoad = 400, CardRoadOver = 4.5;
    private const int CardFromStart = 1400;

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

    // The scrapbook (the user, 2026-11-24: "let's also add a scrapbook feature that shows every postcard we've gathered, we should probably
    // have this as a per player feature so not all post cards have to be collected in a single playthrough"): the cards as the race game's
    // Scrapbook page draws them, POSTCARDS.TXT in the game folder (RACEGAME.CPP ReadCards) -- a line a card: "card <n> <picture> <sky r g b>
    // <far hills r g b> <ground r g b>|<the place its greeting is from>|<where it was lost>|<its words after the greeting>". The picture: 0 a
    // sun, 1 rain, 2 night (stars, a moon), 3 the lava's glow. Which cards a player has found: the engine keeps them (SCRAPBOOK.TXT in the
    // player's folder, RACEMOD.CPP postcards=).
    public const string ScrapbookFile = "POSTCARDS.TXT";
    private static readonly (int Picture, int[] Sky, int[] Far, int[] Ground, string Where)[] CardPictures =
    {
        (1, new[] { 70, 80, 100 }, new[] { 60, 78, 72 }, new[] { 52, 92, 52 }, "Citadel Island, the storm race"),
        (0, new[] { 110, 170, 230 }, new[] { 90, 140, 110 }, new[] { 70, 150, 70 }, "Citadel Island, the town race"),
        (0, new[] { 150, 200, 240 }, new[] { 200, 170, 110 }, new[] { 230, 200, 140 }, "the White Leaf Desert"),
        (2, new[] { 12, 12, 34 }, new[] { 60, 130, 90 }, new[] { 80, 170, 110 }, "the Emerald Moon"),
        (0, new[] { 170, 150, 200 }, new[] { 120, 110, 140 }, new[] { 100, 140, 100 }, "Otringal"),
        (3, new[] { 200, 120, 80 }, new[] { 90, 60, 50 }, new[] { 230, 90, 30 }, "Celebration Island, by the lava lake"),
        (0, new[] { 160, 190, 220 }, new[] { 100, 120, 90 }, new[] { 110, 140, 80 }, "the Island of the Francos"),
        (0, new[] { 200, 170, 200 }, new[] { 120, 120, 140 }, new[] { 140, 140, 150 }, "the Elevator Platform"),
        (0, new[] { 150, 190, 170 }, new[] { 80, 120, 80 }, new[] { 60, 110, 60 }, "Wannies Island"),
        (0, new[] { 230, 200, 120 }, new[] { 150, 120, 60 }, new[] { 180, 150, 60 }, "Mosquibees Island"),
        (3, new[] { 60, 30, 30 }, new[] { 100, 40, 30 }, new[] { 220, 80, 20 }, "under Celebration Island, the volcano"),
        (2, new[] { 40, 50, 80 }, new[] { 90, 90, 110 }, new[] { 110, 110, 120 }, "Island CX"),
        (0, new[] { 120, 150, 220 }, new[] { 180, 160, 110 }, new[] { 200, 180, 120 }, "the Emperor's palace"),
    };

    // POSTCARDS.TXT into the game folder (the same whichever island's build writes it)
    internal static void WriteScrapbook(string gameDirectory)
    {
        var lines = new List<string> { "# The Little Big Postcards, as the Scrapbook page shows them (LBA Assembler, RaceTrackStory.Eggs.Postcards.cs)" };
        for (var n = 1; n <= PostcardCount; n++)
        {
            var english = Captions[n - 1][0];
            // ("A postcard: "Greetings from <place>! <words>"": the place and the words apart)
            var quoted = english[(english.IndexOf('"') + 1)..].TrimEnd('"');
            var greeting = quoted.IndexOf('!');
            var place = quoted.StartsWith("Greetings from ") && greeting > 15 ? quoted[15..greeting] : CardPictures[n - 1].Where;
            var words = greeting > 0 ? quoted[(greeting + 1)..].Trim() : quoted;
            var (picture, sky, far, ground, where) = CardPictures[n - 1];
            lines.Add($"card {n} {picture} {string.Join(' ', sky)} {string.Join(' ', far)} {string.Join(' ', ground)}|{place}|{where}|{words}");
        }
        File.WriteAllLines(Path.Combine(gameDirectory, ScrapbookFile), lines);
    }

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
        WriteScrapbook(gameDirectory);
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (island == RaceTrackIsland.Celebration) { log.AddRange(PostcardReward(gameDirectory, store, island, scenes)); return log; }
        if (chapter < 1 || chapter > PostcardCount) return log;
        var lines = chapter == 6 ? new[] { Captions[chapter - 1], SellerCards[0] } : new[] { Captions[chapter - 1] };
        var first = GroupB.AddLines(gameDirectory, 3 + island.IslandByte, lines, log, $"chapter {chapter}'s postcard's");
        // (off the roads a little over half way round the lap; else beside the start line; else where the start scene starts Twinsen) --
        // on open, flat, safe ground: clear of the island's decor (a rock, a house, a tree: under one the card was half buried or hidden
        // behind it), the ground under the card level enough, no lava or water under it; standing on the highest of it; its
        // picture to the road's nearest point (the user, 2026-11-24, "some half buried or barely visible")
        (int Scene, int X, int Y, int Z, int Beta)? at = null;
        int SceneAt(double x, double z) => island.Scenes.FirstOrDefault(s => store.SceneExists(s) && store.Load(s) is var m && m.CubeMode == 1
            && m.CubeX == (int)Math.Floor(x / 64) && m.CubeY == (int)Math.Floor(z / 64), -1);
        var ilePath = Path.Combine(gameDirectory, island.IleFile);
        IslandFile? ile = null;
        try { if (File.Exists(ilePath)) ile = IslandFile.Load(ilePath); } catch (Exception) { }
        var (cardFlat, cardDecorGap) = CardStrict[0];
        double? FootHigh(double px, double pz)
        {
            if (report.GroundAfter is not { } ground) return null;
            double lo = double.MaxValue, hi = double.MinValue;
            for (var dz = -CardFoot; dz <= CardFoot; dz += CardFoot)
                for (var dx = -CardFoot; dx <= CardFoot; dx += CardFoot) { var g = ground(px + dx, pz + dz); lo = Math.Min(lo, g); hi = Math.Max(hi, g); }
            return hi - lo <= cardFlat ? hi : null;
        }
        bool Open(double px, double pz)
        {
            if (FootHigh(px, pz) is not { } foot) return false;
            // (no road over it: on the Francos' island the card stood under the raised road, out of sight)
            for (var i = 0; i < report.LapX.Length && i < report.LapY.Length; i++)
                if (report.LapY[i] > foot + CardUnderRoad && Math.Abs(report.LapX[i] - px) < CardRoadOver && Math.Abs(report.LapZ[i] - pz) < CardRoadOver) return false;
            if (ile is null) return true;
            int cx = (int)Math.Floor(px / 64), cz = (int)Math.Floor(pz / 64);
            double lx = (px - cx * 64) * 512, lz = (pz - cz * 64) * 512;
            if (lx < 5 * 512 || lx > 59 * 512 || lz < 5 * 512 || lz > 59 * 512) return false;      // (not by the cube's edge)
            var cube = ile.CubeAt(cx, cz);
            if (cube is null) return true;
            if (cube.Decors.Any(d => d.YMax > d.YMin && lx > d.XMin - cardDecorGap && lx < d.XMax + cardDecorGap && lz > d.ZMin - cardDecorGap && lz < d.ZMax + cardDecorGap)) return false;
            if (cube.HasPolygons)
            {
                // (nothing deadly under it or round it: the volcano's card stood at the lava's edge)
                int gx = (int)Math.Floor(lx / 512), gz = (int)Math.Floor(lz / 512);
                for (var dz = -CardDeadlyRound; dz <= CardDeadlyRound; dz++)
                    for (var dx = -CardDeadlyRound; dx <= CardDeadlyRound; dx++)
                        if (new[] { 0, 1 }.Any(half => CardDeadly.Contains(new IslandPolygon(cube.Polygon(gx + dx, gz + dz, half)).CodeJeu))) return false;
            }
            return true;
        }
        int FaceRoad(double px, double pz)
        {
            var best = double.MaxValue; var (bx, bz) = (px, pz + 1);
            for (var i = 0; i < report.LapX.Length; i++)
            {
                var d = (report.LapX[i] - px) * (report.LapX[i] - px) + (report.LapZ[i] - pz) * (report.LapZ[i] - pz);
                if (d < best) { best = d; (bx, bz) = (report.LapX[i], report.LapZ[i]); }
            }
            return (int)Math.Round(Math.Atan2(bx - px, bz - pz) * 2048 / Math.PI) & 4095;
        }
        var how = "";
        foreach (var strict in CardStrict)
        foreach (var (along, clear) in new[] { (CardAlong, CardClear), (CardAlong + 0.1, CardClear), (CardAlong - 0.15, 1.5), (CardAlong + 0.25, 1.5), (0.0, 1.5) })
        {
            if (report.LapX.Length < 10 || at is not null) break;
            (cardFlat, cardDecorGap) = strict;
            var i = (int)(along * report.LapX.Length) % report.LapX.Length;
            if (GroupB.OffRoadNear(report, report.LapX[i], report.LapZ[i], clear, CardReach, Open) is not { } o) continue;
            var scene = SceneAt(o.X, o.Z);
            if (scene < 0) continue;
            var m = store.Load(scene);
            var y = FootHigh(o.X, o.Z) ?? o.Y;
            at = (scene, RaceTrackScenes.CubeLocal(o.X, m.CubeX), (int)Math.Round(y), RaceTrackScenes.CubeLocal(o.Z, m.CubeY), FaceRoad(o.X, o.Z));
            how = $", {along:0.00} round the lap, level within {cardFlat}, {cardDecorGap} from the decor";
        }
        // (beside the start line, on ground about its height: a raised road's island, the Elevator Platform, Island CX, the palace)
        if (at is null && GroupB.MarshalAt(report) is { } line)
            foreach (var strict in CardStrict)
            {
                (cardFlat, cardDecorGap) = strict;
                if (GroupB.SpotAt(store, island, report, line.X, line.Z, line.Y, line.X, line.Z, 1.5, 12, Open) is not { } s) continue;
                var m = store.Load(s.Scene);
                var y = FootHigh(s.At.X, s.At.Z) ?? s.At.Y;
                at = (s.Scene, RaceTrackScenes.CubeLocal(s.At.X, m.CubeX), (int)Math.Round(y), RaceTrackScenes.CubeLocal(s.At.Z, m.CubeY), FaceRoad(s.At.X, s.At.Z));
                how = $", beside the start line, level within {cardFlat}, {cardDecorGap} from the decor";
                break;
            }
        if (at is null && scenes.StartScene >= 0 && store.SceneExists(scenes.StartScene))
        {
            // (stepped away from the road's nearest point: on the palace's track Twinsen starts on the grid, and the card stood among the cars)
            var start = store.Load(scenes.StartScene);
            var hero = start.Hero;
            double hx = start.CubeX * 64 + hero.X / 512.0, hz = start.CubeY * 64 + hero.Z / 512.0;
            var (ax, az) = (0.0, 1.0);
            if (report.LapX.Length > 0)
            {
                var near = Enumerable.Range(0, report.LapX.Length).MinBy(i => (report.LapX[i] - hx) * (report.LapX[i] - hx) + (report.LapZ[i] - hz) * (report.LapZ[i] - hz));
                var (dx, dz) = (hx - report.LapX[near], hz - report.LapZ[near]);
                var d = Math.Sqrt(dx * dx + dz * dz);
                if (d > 0.01) (ax, az) = (dx / d, dz / d);
            }
            int cx = Math.Clamp(hero.X + (int)Math.Round(ax * CardFromStart), 512, IslandFile.CubeSize - 512), cz = Math.Clamp(hero.Z + (int)Math.Round(az * CardFromStart), 512, IslandFile.CubeSize - 512);
            at = (scenes.StartScene, cx, hero.Y, cz, (int)Math.Round(Math.Atan2(-ax, -az) * 2048 / Math.PI) & 4095);
            how = ", by where the start scene starts Twinsen, away from the road: no open ground off the roads";
        }
        if (at is not { } a) { log.Add($"WARNING: chapter {chapter}'s postcard left out: nowhere off the roads, and no start scene"); return log; }
        GroupB.EditScene(store, a.Scene, log, $"chapter {chapter}'s postcard (EggCards, EggLastCard) at ({a.X}, {a.Y}, {a.Z}) facing {a.Beta}{how}", model =>
        {
            var card = PostcardProp.Actor(gameDirectory, Math.Clamp(a.X, 0, IslandFile.CubeSize - 1), a.Y, Math.Clamp(a.Z, 0, IslandFile.CubeSize - 1), a.Beta);
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
