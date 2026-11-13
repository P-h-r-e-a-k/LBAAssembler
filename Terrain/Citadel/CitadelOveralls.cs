using System.IO;
using System.Numerics;
using System.Text;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// Twinsen's racing overalls and his racing helmet (the user, 2026-11-07: "we're replacing Twinsen's tunic with racing overalls"; "along
// with finding his driving gloves, Twinsen must fetch his racing outfit from the museum, where his tunic is now"; a racing helmet,
// "making it a requirement to enter the Desert race ... perhaps reusing the cave that holds the Pearl of Incandescence"):
//   - the overalls take the tunic's place everywhere: item 4 (FLAG_TUNIQUE, its variable and its Magic as before -- the game's tunic
//     stores Magic and lets him throw his ball, and so do they), its model (OBJFIX.HQR 4) and its three texts (file 2's 4, 104, 204),
//     and Twinsen's bodies: his tunic's (BODY.HQR 1, GEN_BODY_TUNIQUE -- the body ChoiceHeroBody gives him once he has item 4) is his
//     everyday body's shape -- shirt and trousers, his legs apart, not the tunic's long skirt -- in racing red, a white belt and white
//     stripes down the outsides of his arms and legs; the tunic's with a weapon (BODY.HQR 2-6: the sabre, the blowpipes, the glove, the
//     laser pistol) have the tunic's blue made the same red. The wizard's robe (BODY.HQR 7, 8) stays the robe;
//   - the helmet is item 5, the game's Incandescent Pearl's slot (the story has no pearl: its Weather Wizard has no ring to make with it,
//     RaceTrackStory.WizardNoRing): its model (OBJFIX.HQR 5) and texts (5, 105, 205); the giant clam in the Pearl's sea cave on the
//     White Leaf Desert (scene 113) gives it as it gave the pearl (RaceTrackStory.Desert.cs HelmetClam).
// Into the game folder after every other body is made (no body moves; the cars' drivers are their own bodies). A second pass makes the
// same again.
internal static class CitadelOveralls
{
    public const int OverallsSlot = 4, HelmetSlot = 5;
    // the overalls with the helmet on: one more body of Twinsen's (the hero's entity, 0) -- the Emerald Moon's outside, where the race
    // file's foot_body= puts it on him in place of the spacesuit (no_suit=: the suit's behaviours his normal ones -- RACEMOD.CPP; F15 of
    // the Nov 7 notes, the user: "Twinsen wears his racing suit and helmet instead of the spacesuit")
    public const int HelmetBody = 18;
    private const int HeroEntity = 0, Head = 14;
    // the hero's sporty, aggressive and discreet behaviours' entities (RESS.HQR 44: each behaviour its own, with the same body numbers)
    private static readonly int[] BehaviourEntities = { 1, 2, 3 };
    // the hero's bodies: his everyday clothes, the tunic's, the tunic's with a weapon
    private const int Everyday = 0, Tunic = 1, FirstArmed = 2, LastArmed = 6;
    // The game's other bodies of Twinsen in his tunic (the user, 2026-11-08: "Twinsen in his racing suit everywhere he's still in the
    // tunic"): his tunic's two others (11, 13), at the wheel (14, 22), with the protopack (16, 21), arm in arm with Zoe (17, 175), the
    // buggy's (18, 19), on the Dino-Fly (166), and 201, 286, 339, 362, 368 -- picked by sight from every body with his eyes (two faces of
    // 197) and the tunic's blue (192): the Wizard at a wheel (15, 23), the rabbibunny, the grobos and the mixed casts keep theirs, and so do
    // the balloons and their basket (247, 248, 275: some balloons are that blue too). Their shapes stay the tunic's; its colour, 192 lit,
    // is the suit's -- patched in the body's own bytes, nothing else rewritten (17, 175 and 362 store more primitives than the writer
    // takes, and play as they are).
    private static readonly int[] TunicBodies = { 9, 10, 11, 13, 14, 16, 17, 18, 19, 21, 22, 166, 175, 201, 286, 339, 362, 368 };
    // (and the race game's small cars with him at the wheel, the build's copies of 14 made before this: his eyes, two faces of 197, and
    // the 66 of the tunic's blue that 14's driver has)
    private const int Eyes = 197, WheelTunicFaces = 66;
    private const int TunicColour = 192;
    // (the buggy's two drivers: BODY.HQR 18 and 19)
    private static readonly int[] BuggyDrivers = { 18, 19 };
    private const int MuseumCase = 105, CaseTunicColour = 162, CaseMedallion = 66;
    // (the racing gloves' colours, RaceTrackGloves: the red lit along its ramp, the white and the dark flat)
    // (the inventory's overalls and helmet, and the gloves' model (RaceTrackGloves), in the suit's colour -- "Red" was the first suit's: the
    // user, 2026-11-12: "The Racing suit in the inventory, and the found icon ... need to match the green")
    private static int Red => SuitLit;
    private const int White = 62, Dark = 49;
    // (on Twinsen: a ramp's start is its darkest -- 64 came out maroon -- so his overalls' red six shades up it, as his jeans' blue is
    // 194; the shirt's polygons are textured, its pattern taken off)
    // The suit's colour, a choice -- green by default (the user, 2026-11-10: "Let's go with green, but let's keep the models, they may be
    // useful for multiplayer at a later date"; and before: "generate a few different colour outfits and add them into LBAAssembler. Let's go
    // for a blue based on Twinsen's tunic, British racing green, a dark mustard yellow"): each a ramp of the palette and how far up it the
    // suit is lit from (the red's six, as above). The build's choice: RT_SUIT (red, blue, green, mustard) if set, else the race car
    // window's (RaceCarSetup.RacingSuit). Everything painted the suit's colour follows it: the overalls, the helmet, the tunic's armed
    // bodies, and the spanking's suit pulled down (CitadelSpanking).
    public enum SuitColour { Red, Blue, Green, Mustard }
    public static readonly (SuitColour Colour, string Name, int Ramp, int Shade)[] Suits =
    {
        (SuitColour.Red, "Red", 64, 6),
        (SuitColour.Blue, "Blue, his tunic's", 192, 6),
        (SuitColour.Green, "British racing green", 128, 4),
        (SuitColour.Mustard, "Dark mustard yellow", 96, 7),
    };
    public static SuitColour Colour { get; set; } = SuitColour.Green;
    private static (SuitColour Colour, string Name, int Ramp, int Shade) Suit => Suits.First(s => s.Colour == Colour);
    private static int Bright => Suit.Ramp + Suit.Shade;
    // the suit's colour, lit, as the build has chosen it (the inventory's models are made before or after Install sets Colour)
    internal static int SuitLit => Suits.First(x => x.Colour == Chosen()) is var c ? c.Ramp + c.Shade : 132;
    // (the build's choice: RT_SUIT, else the race car window's)
    public static SuitColour Chosen()
    {
        var name = Environment.GetEnvironmentVariable("RT_SUIT") is { Length: > 0 } env ? env : EditorSettings.Current.RaceCar.RacingSuit;
        return Enum.TryParse<SuitColour>(name, true, out var c) ? c : SuitColour.Green;
    }
    // the tunic's blue (its ramp), and the everyday clothes': the shirt, the jeans, the belt
    private const int TunicBlue = 192, Shirt = 224, Jeans = 194, Belt = 97;
    // the everyday body's arms and legs (bones), for the stripes
    private static readonly int[] Limbs = { 4, 5, 6, 7, 8, 9, 11, 12 };
    // (the game's inventory objects are about 3,500 across: RaceTrackStory.Errands.cs)
    private const float InventoryExtent = 3200;
    private const int InventoryTexts = 2;

    private static readonly (int Id, string[] Words)[] Texts =
    {
        (OverallsSlot, new[]
        {
            "You have your racing overalls back! Sendell's Medallion is still in the pocket: you can store Magic again, and bounce your ball at your enemies.",
            "Tu as récupéré ta combinaison de course ! Le Médaillon de Sendell est toujours dans la poche : tu peux de nouveau emmagasiner de la Magie et lancer ta balle sur tes ennemis.",
            "Du hast deinen Rennoverall zurück! Sendells Medaillon steckt noch in der Tasche: du kannst wieder Magie speichern und deinen Ball auf deine Feinde werfen.",
            "¡Has recuperado tu mono de carreras! El Medallón de Sendell sigue en el bolsillo: puedes volver a almacenar Magia y lanzar tu bola contra tus enemigos.",
            "Hai ritrovato la tua tuta da corsa! Il Medaglione di Sendell è ancora in tasca: puoi di nuovo accumulare Magia e lanciare la tua sfera contro i nemici.",
            "Você recuperou seu macacão de corrida! O Medalhão de Sendell continua no bolso: você pode voltar a armazenar Magia e lançar sua bola contra os inimigos.",
        }),
        (OverallsSlot + 100, new[] { "Racing overalls", "Combinaison de course", "Rennoverall", "Mono de carreras", "Tuta da corsa", "Macacão de corrida" }),
        (OverallsSlot + 200, new[]
        {
            "Your racing overalls, with Sendell's Medallion in the pocket. They store Magic power: some is used each time you use a magical object, such as your ball. To use them, you must find enough Magic and carry it with you.",
            "Ta combinaison de course, avec le Médaillon de Sendell dans la poche. Elle emmagasine la puissance Magique : chaque objet magique utilisé, comme ta balle, en consomme un peu. Pour t'en servir, tu dois trouver assez de Magie et la garder sur toi.",
            "Dein Rennoverall, mit Sendells Medaillon in der Tasche. Er speichert magische Kraft: jedes magische Objekt, das du benutzt, wie dein Ball, verbraucht etwas davon. Um sie zu nutzen, mußt du genug Magie finden und bei dir tragen.",
            "Tu mono de carreras, con el Medallón de Sendell en el bolsillo. Almacena el poder Mágico: cada objeto mágico que usas, como tu bola, consume un poco. Para usarlos, debes encontrar suficiente Magia y llevarla contigo.",
            "La tua tuta da corsa, con il Medaglione di Sendell in tasca. Accumula il potere Magico: ogni oggetto magico che usi, come la tua sfera, ne consuma un po'. Per usarli, devi trovare abbastanza Magia e portarla con te.",
            "Seu macacão de corrida, com o Medalhão de Sendell no bolso. Ele armazena o poder Mágico: cada objeto mágico que você usa, como sua bola, gasta um pouco. Para usá-los, você precisa encontrar Magia suficiente e levá-la consigo.",
        }),
        (HelmetSlot, new[]
        {
            "Inside the giant clam: a racing helmet! Some racer must have lost it diving. Now you can race in the desert.",
            "Dans le coquillage géant : un casque de course ! Un pilote a dû le perdre en plongeant. Maintenant, tu peux courir dans le désert.",
            "In der Riesenmuschel: ein Rennhelm! Irgendein Rennfahrer muß ihn beim Tauchen verloren haben. Jetzt kannst du in der Wüste fahren.",
            "Dentro del molusco gigante: ¡un casco de carreras! Algún piloto debió de perderlo buceando. Ahora puedes correr en el desierto.",
            "Dentro la conchiglia gigante: un casco da corsa! Qualche pilota deve averlo perso facendo immersioni. Ora puoi correre nel deserto.",
            "Dentro da concha gigante: um capacete de corrida! Algum piloto deve tê-lo perdido mergulhando. Agora você pode correr no deserto.",
        }),
        (HelmetSlot + 100, new[] { "Racing gloves and helmet", "Gants et casque de course", "Rennhandschuhe und Rennhelm", "Guantes y casco de carreras", "Guanti e casco da corsa", "Luvas e capacete de corrida" }),
        (HelmetSlot + 200, new[]
        {
            "Your driving gloves, and a racing helmet from the giant clam's sea cave. No helmet, no race: Flick lets nobody onto the Desert track without one.",
            "Tes gants de pilote, et un casque de course trouvé dans la grotte marine du coquillage géant. Pas de casque, pas de course : Flick ne laisse personne sur la piste du Désert sans casque.",
            "Deine Fahrhandschuhe und ein Rennhelm aus der Meeresgrotte der Riesenmuschel. Ohne Helm kein Rennen: Flick läßt niemanden ohne Helm auf die Wüstenstrecke.",
            "Tus guantes de conducir, y un casco de carreras de la gruta marina del molusco gigante. Sin casco no hay carrera: Flick no deja a nadie entrar en la pista del Desierto sin él.",
            "I tuoi guanti da guida, e un casco da corsa dalla grotta marina della conchiglia gigante. Niente casco, niente corsa: Flick non lascia nessuno sulla pista del Deserto senza.",
            "As tuas luvas de condução, e um capacete de corrida da gruta marinha da concha gigante. Sem capacete, sem corrida: o Flick não deixa ninguém entrar na pista do Deserto sem um.",
        }),
    };

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        Colour = Chosen();
        // ---- Twinsen's bodies
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var bodies = HqrArchive.Open(bodyPath);
        var overalls = WithBackPatch(Overalls(Body.Read(bodies.Read(Everyday), 2)));
        var armed = new List<(int Entry, Body Body, int Faces)>();
        for (var b = FirstArmed; b <= LastArmed; b++)
        {
            var body = Body.Read(bodies.Read(b), 2);
            var n = 0;
            for (var i = 0; i < body.Faces.Count; i++)
                if (body.Faces[i].Colour is >= TunicBlue and < TunicBlue + 16) { body.Faces[i] = body.Faces[i] with { Colour = Math.Min(Suit.Ramp + 15, Bright + body.Faces[i].Colour - TunicBlue), Texture = null }; n++; }
            // (his shape the suit's, not the tunic's robe: the weapon and what hangs on him -- the bones past his own -- carried over)
            body = WithExtras(WithBackPatch(Overalls(Body.Read(bodies.Read(Everyday), 2))), body);
            armed.Add((b, body, n));
        }
        var helmeted = WithBackPatch(WithHelmet(Overalls(Body.Read(bodies.Read(Everyday), 2))));
        // (and the rest of him in his tunic: the suit's colour where it was the tunic's)
        var others = new List<(int Entry, int Faces)>();
        byte[]? Entry(int i) { try { return bodies.Read(i); } catch (ArgumentOutOfRangeException) { return null; } catch (InvalidDataException) { return null; } }
        var copies = Enumerable.Range(0, bodies.Count).Where(i => !TunicBodies.Contains(i) && Entry(i) is { } e && Counts(e) is (2, WheelTunicFaces)).ToList();
        // (Twinsen arm in arm with Zoe, entity 5's body: his part the suit's shape, not only its colour -- CoupleInSuit)
        var coupled = new HashSet<int>();
        foreach (var c in CoupleBodies)
        {
            if (c >= bodies.Count || Entry(c) is not { } coupleBytes) continue;
            try
            {
                // (no LBRA patch on his back here: its 150 points and polygons took the pair over the engine's 550)
                var couple = CoupleInSuit(Body.Read(coupleBytes, 2), Overalls(Body.Read(bodies.Read(Everyday), 2)));
                log.Add($"Twinsen arm in arm with Zoe (BODY.HQR {c}): {couple.Vertices.Count} points, {couple.Faces.Count} polygons, {couple.Lines.Count} lines, {couple.Spheres.Count} spheres, {couple.Bones.Count} bones");
                HqrWriter.ReplaceInFile(bodyPath, c, couple.Write());
                coupled.Add(c);
                log.Add($"Twinsen arm in arm with Zoe (BODY.HQR {c}): his part the racing suit's ({couple.Vertices.Count} points, {couple.Faces.Count} polygons)");
            }
            catch (Exception e) when (e is InvalidDataException or InvalidOperationException or ArgumentException or IndexOutOfRangeException or KeyNotFoundException)
            { log.Add($"Twinsen arm in arm with Zoe (BODY.HQR {c}) left in the tunic's shape, in the suit's colour: {e.Message}"); }
        }
        foreach (var t in TunicBodies.Concat(copies).Where(t => !coupled.Contains(t)).ToList())
        {
            if (t >= bodies.Count) continue;
            if (Entry(t) is not { } bytes) continue;
            var n = RecolourPolygons(bytes, TunicColour, Bright);
            if (n <= 0) continue;
            HqrWriter.ReplaceInFile(bodyPath, t, bytes);
            others.Add((t, n));
        }
        // (and the museum's case, the overalls on show until Twinsen takes them: entity 62's body, BODY.HQR 105 -- 106 the case empty --
        // whose tunic is the palette's 162, its blue a little lighter behind the glass. LBRA issue #4, the user: "The container in the
        // museum that contains Twinsen's racing outfit appears as his magic tunic until he collects it")
        // The tunic's shape is the overalls' (OverallsInCase); where the writer can't take the case so, its tunic only in the suit's colour.
        if (Entry(MuseumCase) is { } caseBytes)
        {
            byte[]? shaped = null;
            try { shaped = OverallsInCase(caseBytes, Body.Read(bodies.Read(Everyday), 2)); }
            catch (Exception e) when (e is InvalidDataException or InvalidOperationException or ArgumentException) { log.Add($"the museum's case: the overalls' shape left out ({e.Message}), the tunic in the suit's colour"); }
            if (shaped is not null) { HqrWriter.ReplaceInFile(bodyPath, MuseumCase, shaped); log.Add($"the museum's case (BODY.HQR {MuseumCase}): the overalls on its stand in place of the tunic"); }
            else if (RecolourPolygons(caseBytes, CaseTunicColour, Math.Min(Suit.Ramp + 15, Bright + 2)) is > 0 and var caseFaces)
            {
                HqrWriter.ReplaceInFile(bodyPath, MuseumCase, caseBytes);
                others.Add((MuseumCase, caseFaces));
            }
        }
        // (the buggy's drivers, the laser cars' Twinsen at the wheel: the racing suit and the helmet, his strap and horn kept)
        foreach (var d in BuggyDrivers)
        {
            if (d >= bodies.Count || Entry(d) is not { } driverBytes) continue;
            try
            {
                var driver = WithExtras(WithBackPatch(WithHelmet(Overalls(Body.Read(bodies.Read(Everyday), 2)), 8, 4)), Body.Read(driverBytes, 2));
                log.Add($"the buggy's driver (BODY.HQR {d}): {driver.Vertices.Count} points, {driver.Faces.Count} polygons, {driver.Lines.Count} lines, {driver.Spheres.Count} spheres, {driver.Bones.Count} bones");
                HqrWriter.ReplaceInFile(bodyPath, d, driver.Write());
                log.Add($"the buggy's driver (BODY.HQR {d}): the racing suit and helmet, {driver.Vertices.Count} points, {driver.Bones.Count} bones");
            }
            catch (Exception e) when (e is InvalidDataException or InvalidOperationException or ArgumentException)
            { log.Add($"the buggy's driver (BODY.HQR {d}) left in the tunic's shape: {e.Message}"); }
        }
        HqrWriter.ReplaceInFile(bodyPath, Tunic, overalls.Write());
        foreach (var (entry, body, _) in armed) HqrWriter.ReplaceInFile(bodyPath, entry, body.Write());
        // (the helmeted one: its entry again on a second pass, else one more at the file's end, and the hero's entity told of it)
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        int helmetEntry;
        if (RaceTrackGuards.BodyOf(table, HeroEntity, HelmetBody) is { } had) { HqrWriter.ReplaceInFile(bodyPath, had, helmeted.Write()); helmetEntry = had; }
        else
        {
            helmetEntry = HqrArchive.CountEntries(bodyPath);
            HqrWriter.AppendToFile(bodyPath, helmeted.Write());
            table = RaceTrackBaldinoCar.WithBody(table, HeroEntity, HelmetBody, helmetEntry);
            HqrWriter.ReplaceInFile(ressPath, 44, table);
        }
        // (and his other behaviours' entities -- sporty, aggressive, discreet: each behaviour is an entity of its own, its bodies the
        // same numbers -- told of it too: the race file's foot_body= asks for body 18 in whichever he's in, and found none outside the
        // normal one, so in the others the helmet came off -- the user, 2026-11-09, the Emerald Moon, "it only keeps him in the racing
        // outfit in normal mode")
        foreach (var e in BehaviourEntities)
            if (RaceTrackGuards.BodyOf(table, e, HelmetBody) is null)
            {
                table = RaceTrackBaldinoCar.WithBody(table, e, HelmetBody, helmetEntry);
                HqrWriter.ReplaceInFile(ressPath, 44, table);
            }
        log.Add($"Twinsen's other bodies in his tunic in the suit's colour: BODY.HQR {string.Join(", ", others.Select(o => $"{o.Entry} ({o.Faces})"))}");
        log.Add($"Twinsen's racing overalls with the helmet on: the hero's body {HelmetBody} ({helmeted.Vertices.Count} points, {helmeted.Faces.Count} polygons)");
        log.Add($"Twinsen's racing overalls ({Suit.Name}, the palette's {Bright}): BODY.HQR {Tunic} (the tunic's) his everyday body in the suit's colour with a white belt and stripes " +
                $"and an LBRA patch on his back; {FirstArmed}-{LastArmed} (the tunic's with a weapon) the tunic's blue made the suit's ({string.Join(", ", armed.Select(a => a.Faces))} polygons)");

        // ---- the inventory's models
        var objfixPath = Path.Combine(gameDirectory, "OBJFIX.HQR");
        var header = Body.Read(bodies.Read(30), 2, allowStatic: true).Header;
        HqrWriter.ReplaceInFile(objfixPath, OverallsSlot, InventorySized(OverallsModel(header)).Write());
        HqrWriter.ReplaceInFile(objfixPath, HelmetSlot, InventorySized(GearModel(header)).Write());
        log.Add($"OBJFIX.HQR {OverallsSlot}: the racing overalls (the tunic's model); {HelmetSlot}: the racing gloves and helmet together (the pearl's) -- the gloves' slot's second kind");

        // ---- their texts: found, name, description
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var dos = Encoding.GetEncoding(850);
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        var hqr = File.ReadAllBytes(textPath);
        var written = 0;
        for (var lang = 0; lang < languages; lang++)
        {
            var bank = Lba2TextBank.Load(text, lang, InventoryTexts);
            foreach (var (id, words) in Texts)
            {
                var wanted = dos.GetBytes(words[lang < words.Length ? lang : 0]);
                var have = bank.Find(id);
                if (have is null) bank.Texts.Add(new Lba2TextBank.Text { Id = id, Attribute = Lba2TextBank.NormalAttribute, Bytes = wanted });
                else if (!have.Bytes.AsSpan().SequenceEqual(wanted)) have.Bytes = wanted;
                else continue;
                written++;
            }
            hqr = bank.WriteInto(hqr);
        }
        if (written > 0) File.WriteAllBytes(textPath, hqr);
        log.Add($"the overalls' and the helmet's texts: {written} written ({OverallsSlot}, {HelmetSlot} and +100, +200, in all {languages} languages)");
        return log;
    }

    // Twinsen's everyday body as his racing overalls: the shirt and the jeans one red, the belt white, and a white stripe down the outside
    // of each arm and leg (the limbs' polygons that face out sideways, away from his middle)
    internal static Body Overalls(Body body)
    {
        var world = body.World();
        int BoneOf(int p) => body.Bones.FindIndex(b => p >= b.Start && p < b.Start + b.Count);
        for (var i = 0; i < body.Faces.Count; i++)
        {
            var f = body.Faces[i];
            if (f.Colour != Shirt && f.Colour != Jeans && f.Colour != Belt) continue;
            if (f.Colour == Belt) { body.Faces[i] = f with { Colour = White, Material = 0, Texture = null }; continue; }
            var pts = f.Points.Select(p => world[p]).ToArray();
            var n = Vector3.Normalize(LBAAssembler.Terrain.Palace.PalaceTorturers.Newell(pts));
            var middle = pts.Aggregate(Vector3.Zero, (s, p) => s + p) / pts.Length;
            var stripe = Limbs.Contains(BoneOf(f.Points[0])) && MathF.Abs(n.X) > 0.85f && MathF.Sign(n.X) == MathF.Sign(middle.X);
            body.Faces[i] = stripe ? f with { Colour = White, Material = 0, Texture = null } : f with { Colour = Bright, Material = -1, Texture = null };
        }
        return body;
    }

    // The museum's case with the overalls in it: the tunic's polygons (the palette's 162) out, and in their place the overalls -- the
    // shirt's, the jeans' and the belt's polygons of Twinsen's everyday body made overalls, standing as he stands with no pose -- scaled
    // to the tunic's height (no wider than it), on the same feet and middle, in the bone the tunic was in. Its little bits of the 66
    // inside the tunic's box (its medallion) go with it. Behind the case's glass all of it is the glass's cyan, but the shape is the suit's.
    // Null where the case has no tunic, or its polygons don't read in their blocks' order.
    // (Written lit, every polygon of the case's as the type it had -- the glass see-through, the frames shaded: Body.Read keeps no types,
    // and written as read the whole case came out flat and dark, its glass opaque -- the overalls' Gouraud, their white flat, as on him)
    internal static byte[]? OverallsInCase(byte[] caseBytes, Body everyday)
    {
        var caseBody = Body.Read(caseBytes, 2);
        var types = PolygonTypes(caseBytes);
        if (types is null || types.Count != caseBody.Faces.Count || types.Where((t, i) => t.Colour != caseBody.Faces[i].Colour).Any()) return null;
        if (types.Any(t => t.Type > 7)) return null;
        // (its type 1 -- lit flat, from the face normals, which the writer doesn't keep: the frame's uprights came out dark -- lit from its
        // points' normals instead, type 4, the game's own normals put back below)
        for (var i = 0; i < caseBody.Faces.Count; i++) caseBody.Faces[i] = caseBody.Faces[i] with { Lba2Type = types[i].Type == 1 ? 4 : types[i].Type };
        caseBody.Lit = true;
        var clothes = Enumerable.Range(0, everyday.Faces.Count).Where(i => everyday.Faces[i].Colour is Shirt or Jeans or Belt).ToList();
        var suit = Overalls(everyday);
        var tunic = caseBody.Faces.Where(f => f.Colour == CaseTunicColour).ToList();
        if (tunic.Count == 0 || clothes.Count == 0) return null;
        var world = caseBody.World().ToList();
        static (Vector3 Min, Vector3 Max) Box(IEnumerable<Vector3> ps) => (ps.Aggregate(new Vector3(float.MaxValue), Vector3.Min), ps.Aggregate(new Vector3(float.MinValue), Vector3.Max));
        var (tMin, tMax) = Box(tunic.SelectMany(f => f.Points).Distinct().Select(p => world[p]).ToList());
        var sw = suit.World();
        var used = clothes.SelectMany(i => suit.Faces[i].Points).Distinct().ToList();
        var (sMin, sMax) = Box(used.Select(p => sw[p]).ToList());
        var size = sMax - sMin; var room = tMax - tMin;
        var scale = MathF.Min(room.Y / MathF.Max(1, size.Y), MathF.Min(room.X / MathF.Max(1, size.X), room.Z / MathF.Max(1, size.Z)) * 1.15f);
        var sMid = (sMin + sMax) / 2; var tMid = (tMin + tMax) / 2;
        Vector3 Place(Vector3 p) => new(tMid.X + (p.X - sMid.X) * scale, tMin.Y + (p.Y - sMin.Y) * scale, tMid.Z + (p.Z - sMid.Z) * scale);
        var local = used.Select((p, k) => (p, k)).ToDictionary(x => x.p, x => x.k);
        var points = used.Select(p => Place(sw[p])).ToList();
        var faces = clothes.Select(i => (suit.Faces[i].Points.Select(p => local[p]).ToArray(), suit.Faces[i].Colour)).ToList();
        var bone = caseBody.Bones.FindIndex(b => tunic[0].Points[0] >= b.Start && tunic[0].Points[0] < b.Start + b.Count);
        if (bone < 0) return null;
        bool Inside(Vector3 p) => p.X >= tMin.X - 1 && p.X <= tMax.X + 1 && p.Y >= tMin.Y - 1 && p.Y <= tMax.Y + 1 && p.Z >= tMin.Z - 1 && p.Z <= tMax.Z + 1;
        caseBody.Faces.RemoveAll(f => f.Colour == CaseTunicColour || f.Colour == CaseMedallion && f.Points.All(p => Inside(world[p])));
        var at = caseBody.Bones[bone].Start + caseBody.Bones[bone].Count;
        var shaped = AddToBone(caseBody, world, bone, points, faces);
        for (var i = 0; i < shaped.Faces.Count; i++)
            if (shaped.Faces[i].Lba2Type < 0) shaped.Faces[i] = shaped.Faces[i] with { Lba2Type = shaped.Faces[i].Colour == Bright ? 4 : 0 };
        var bytes = shaped.Write();
        // (the case's own points their own light: the normals the game's body had -- the writer's, worked out from the polygons, left
        // its gold frames dark -- the overalls' points the writer's; a normal 8 bytes, in the points' order, the header's count at 48 and
        // place at 52)
        int oldCount = BitConverter.ToInt32(caseBytes, 48), oldAt = BitConverter.ToInt32(caseBytes, 52), newAt = BitConverter.ToInt32(bytes, 52);
        if (BitConverter.ToInt32(bytes, 48) != oldCount + points.Count) return null;
        for (var i = 0; i < oldCount; i++)
            Array.Copy(caseBytes, oldAt + i * 8, bytes, newAt + (i < at ? i : i + points.Count) * 8, 8);
        return bytes;
    }

    // An LBA2 body's polygons' types and colours, in their blocks' order (RecolourPolygons' walk); null if it doesn't read so
    private static List<(int Type, int Colour)>? PolygonTypes(byte[] b)
    {
        if (b.Length < 96) return null;
        int p = BitConverter.ToInt32(b, 68), end = BitConverter.ToInt32(b, 76);
        if (p < 96 || end > b.Length || p > end) return null;
        var list = new List<(int, int)>();
        while (p < end)
        {
            if (p + 8 > end) return null;
            int type = BitConverter.ToUInt16(b, p), count = BitConverter.ToUInt16(b, p + 2);
            p += 8;
            bool quad = (type & 32768) != 0, env = (type & 16384) != 0, texture = (type & 255) > 7;
            var stride = env ? 16 : texture ? (quad ? 32 : 24) : 12;
            if ((type & 255) > 23 || p + count * stride > end) return null;
            for (var i = 0; i < count; i++, p += stride) list.Add((type & 255, b[p + 8]));
        }
        return list;
    }

    // The helmet on his head: a shell round the head bone's points -- red, a white stripe over the top, a dark visor across the face, the
    // padding dark inside the rim -- its points the head's (added after the head's own, every later bone's moved along)
    internal static Body WithHelmet(Body body, int around = 12, int down = 6)
    {
        var world = body.World().ToList();
        var head = body.Bones[Head];
        // (his hair's tuft -- a brown sphere at the back of his head -- tucked into the helmet: the head's spheres and its children's off)
        bool UnderHead(int b) { while (b > Head) b = body.Bones[b].Parent; return b == Head; }
        int BoneOfPoint(int p) => body.Bones.FindIndex(x => p >= x.Start && p < x.Start + x.Count);
        body.Spheres.RemoveAll(sp => BoneOfPoint(sp.Point) is var bp && bp >= Head && UnderHead(bp));
        var own = world.Skip(head.Start).Take(head.Count).ToArray();
        var lo = new Vector3(own.Min(p => p.X), own.Min(p => p.Y), own.Min(p => p.Z));
        var hi = new Vector3(own.Max(p => p.X), own.Max(p => p.Y), own.Max(p => p.Z));
        var centre = new Vector3((lo.X + hi.X) / 2, (lo.Y + hi.Y) / 2 + 10, (lo.Z + hi.Z) / 2);
        var radius = new Vector3((hi.X - lo.X) / 2 + 30, (hi.Y - lo.Y) / 2 + 28, (hi.Z - lo.Z) / 2 + 22);
        int Around = around, Down = down;
        float Lat(int k) => (75 - k * 110f / (Down - 1)) * MathF.PI / 180;   // 75 down to -35
        var points = new List<Vector3>();
        var faces = new List<(int[] Points, int Colour)>();
        Vector3 On(float lat, float a, float shrink = 1) =>
            centre + new Vector3(radius.X * shrink * MathF.Cos(lat) * MathF.Sin(a), radius.Y * MathF.Sin(lat), radius.Z * shrink * MathF.Cos(lat) * MathF.Cos(a));
        var crown = points.Count; points.Add(centre + new Vector3(0, radius.Y, 0));
        var rings = new int[Down][];
        for (var k = 0; k < Down; k++)
        {
            rings[k] = new int[Around];
            for (var i = 0; i < Around; i++) { rings[k][i] = points.Count; points.Add(On(Lat(k), i * MathF.Tau / Around)); }
        }
        int Colour(float around, float lat)
        {
            var fromFront = MathF.Abs(MathF.IEEERemainder(around, MathF.Tau));
            if (lat is > -0.5f and < 0.35f && fromFront < 1.1f) return Dark;
            var fromTop = MathF.Min(fromFront, MathF.PI - fromFront);
            return lat > 0.2f && fromTop < 0.3f ? White : Bright;
        }
        void Out(int[] q, int colour)
        {
            var at = q.Select(p => points[p]).ToArray();
            var n = LBAAssembler.Terrain.Palace.PalaceTorturers.Newell(at);
            var middle = at.Aggregate(Vector3.Zero, (s, p) => s + p) / at.Length;
            faces.Add((Vector3.Dot(n, middle - centre) < 0 ? q.Reverse().ToArray() : q, colour));
        }
        for (var i = 0; i < Around; i++)
        {
            var n = (i + 1) % Around;
            var mid = (i + 0.5f) * MathF.Tau / Around;
            Out(new[] { crown, rings[0][i], rings[0][n] }, Colour(mid, 1.4f) == White ? White : Bright);
            for (var k = 0; k + 1 < Down; k++) Out(new[] { rings[k][i], rings[k][n], rings[k + 1][n], rings[k + 1][i] }, Colour(mid, (Lat(k) + Lat(k + 1)) / 2));
        }
        // (the padding: a dark band inside the rim, both its sides drawn)
        var inner = new int[Around];
        for (var i = 0; i < Around; i++) { inner[i] = points.Count; points.Add(On(Lat(Down - 1), i * MathF.Tau / Around, 0.82f) + new Vector3(0, 40, 0)); }
        for (var i = 0; i < Around; i++)
        {
            var n = (i + 1) % Around;
            var q = new[] { rings[Down - 1][i], rings[Down - 1][n], inner[n], inner[i] };
            faces.Add((q, Dark)); faces.Add((q.Reverse().ToArray(), Dark));
        }

        return AddToBone(body, world, Head, points, faces);
    }

    // Twinsen arm in arm with Zoe (entity 5's body, BODY.HQR 17: the user, 2026-11-12, "Twinsen with Zoe is still Twinsen in his tunic rather
    // than his racing outfit"): his bones there are the tunic body's 0-16, the whole of him 100 to his right (x -100), the pelvis's bone (2)
    // shared with Zoe's middle. His polygons out -- every one on his bones 3-16, and the tunic's (192) and his trousers' (80) on bone 2 --
    // and the suit's of the same bones in, moved as far; Zoe's (bone 2's others, her bones 17 on) as they were.
    private static readonly int[] CoupleBodies = { 17 };
    private static readonly Vector3 CoupleShift = new(-100, 0, 0);
    internal static Body CoupleInSuit(Body couple, Body suit)
    {
        const int Shared = 2, FirstHis = 3, LastHis = 16;
        if (couple.Bones.Count <= LastHis + 1 || suit.Bones.Count <= LastHis) throw new InvalidDataException("not the couple's bones");
        var tWorld = couple.World(); var dWorld = suit.World();
        int TBone(int p) => couple.Bones.FindIndex(b => p >= b.Start && p < b.Start + b.Count);
        int DBone(int p) => suit.Bones.FindIndex(b => p >= b.Start && p < b.Start + b.Count);
        bool His(int bone) => bone >= FirstHis && bone <= LastHis;
        bool HisOnShared(Face f) => f.Points.All(p => TBone(p) == Shared) && (f.Colour == TunicColour || f.Colour == 80);
        // (what of the couple stays: no point on his bones, nor his on the pelvis)
        var keptFaces = couple.Faces.Where(f => !f.Points.Any(p => His(TBone(p))) && !HisOnShared(f)).ToList();
        var keptLines = couple.Lines.Where(l => !His(TBone(l.A)) && !His(TBone(l.B))).ToList();
        var keptSpheres = couple.Spheres.Where(sp => !His(TBone(sp.Point))).ToList();
        // (and of the suit's: on his bones 0-16 only -- the medallion's, 17 on, left out)
        // (nor those facing straight down -- his soles, under his chin and hands -- never seen from the game's camera above: the pair within
        // the engine's 550 primitives)
        bool Down(Face f) => Vector3.Normalize(LBAAssembler.Terrain.Palace.PalaceTorturers.Newell(f.Points.Select(p => dWorld[p]).ToArray())).Y < -0.8f;
        var donorFaces = suit.Faces.Where(f => f.Points.All(p => DBone(p) <= LastHis) && !Down(f)).ToList();
        var donorLines = suit.Lines.Where(l => DBone(l.A) <= LastHis && DBone(l.B) <= LastHis).ToList();
        var donorSpheres = suit.Spheres.Where(sp => DBone(sp.Point) <= LastHis).ToList();
        var usedShared = new HashSet<int>(keptFaces.SelectMany(f => f.Points).Concat(keptLines.SelectMany(l => new[] { l.A, l.B })).Concat(keptSpheres.Select(sp => sp.Point))
            .Concat(couple.Bones.Where((b, i) => !His(i) && i != Shared).Select(b => b.Pivot)).Where(p => TBone(p) == Shared));
        var world = new List<Vector3>(); var light = new List<float>();
        var tMap = new Dictionary<int, int>(); var dMap = new Dictionary<int, int>();
        var bones = new List<Bone>();
        for (var b = 0; b < couple.Bones.Count; b++)
        {
            var start = world.Count;
            void Take(int p) { tMap[p] = world.Count; world.Add(tWorld[p]); light.Add(couple.LightScale is { } ls && p < ls.Length ? ls[p] : 1f); }
            void Give(int p) { dMap[p] = world.Count; world.Add(dWorld[p] + CoupleShift); light.Add(suit.LightScale is { } ls && p < ls.Length ? ls[p] : 1f); }
            var tb = couple.Bones[b];
            if (His(b)) { var db = suit.Bones[b]; for (var p = db.Start; p < db.Start + db.Count; p++) Give(p); }
            else if (b == Shared)
            {
                for (var p = tb.Start; p < tb.Start + tb.Count; p++) if (usedShared.Contains(p)) Take(p);
                var db = suit.Bones[b]; for (var p = db.Start; p < db.Start + db.Count; p++) Give(p);
            }
            else
            {
                for (var p = tb.Start; p < tb.Start + tb.Count; p++) Take(p);
                // (his root bones, 0 and 1: the same points in both -- the suit's taken as the couple's)
                if (b < FirstHis && b < suit.Bones.Count) { var db = suit.Bones[b]; for (var k = 0; k < db.Count && k < tb.Count; k++) dMap[db.Start + k] = tMap[tb.Start + k]; }
            }
            bones.Add(tb with { Start = start, Count = world.Count - start });
        }
        // (pivots: his bones the suit's, moved; the rest the couple's -- one hanging from his (the umbrella from his hand): the suit's point of
        // that bone nearest where it was)
        int Pivot(int b)
        {
            if (His(b)) return dMap[suit.Bones[b].Pivot];
            var p = couple.Bones[b].Pivot;
            if (tMap.TryGetValue(p, out var kept)) return kept;
            var db = suit.Bones[TBone(p)];
            var best = db.Start;
            for (var q = db.Start; q < db.Start + db.Count; q++)
                if (Vector3.DistanceSquared(dWorld[q] + CoupleShift, tWorld[p]) < Vector3.DistanceSquared(dWorld[best] + CoupleShift, tWorld[p])) best = q;
            return dMap[best];
        }
        for (var b = 0; b < bones.Count; b++)
            if (couple.Bones[b].Parent >= 0) bones[b] = bones[b] with { Pivot = Pivot(b) };
        var result = couple;
        result.Bones = bones;
        result.Faces = keptFaces.Select(f => f with { Points = f.Points.Select(p => tMap[p]).ToArray(), PointNormals = null })
            .Concat(donorFaces.Select(f => f with { Points = f.Points.Select(p => dMap[p]).ToArray(), PointNormals = null })).ToList();
        result.Lines = keptLines.Select(l => l with { A = tMap[l.A], B = tMap[l.B] }).Concat(donorLines.Select(l => l with { A = dMap[l.A], B = dMap[l.B] })).ToList();
        result.Spheres = keptSpheres.Select(sp => sp with { Point = tMap[sp.Point] }).Concat(donorSpheres.Select(sp => sp with { Point = dMap[sp.Point] })).ToList();
        result.LightScale = light.Any(l => l != 1f) ? light.ToArray() : null;
        result.Vertices = Enumerable.Repeat(Vector3.Zero, world.Count).ToList();
        result.SetWorld(world.ToArray());
        return result;
    }

    // Twinsen in his tunic holding or wearing something (the blowpipes, the laser pistol, the buggy's strap and horn: a tunic body's
    // bones past his own 19) made the suit's: `suit` (his racing overalls, his own 19 bones) with the tunic body's extra bones, their points
    // and polygons added after -- the user, 2026-11-12: "Twinsen's buggy with the green and red lasers we need to recreate a version of Twinsen
    // in his racing outfit". The extra bones hang from the same bones; a pivot or a line's end on one of his own is the suit's nearest point
    // of that bone (the rest pose is the same).
    internal static Body WithExtras(Body suit, Body tunic)
    {
        const int Own = 19;
        if (tunic.Bones.Count <= Own || suit.Bones.Count != Own) return suit;
        var extraStart = tunic.Bones[Own].Start;
        var tunicWorld = tunic.World();
        var suitWorld = suit.World().ToList();
        var suitCount = suitWorld.Count;
        int Nearest(int tunicPoint)
        {
            var bone = tunic.Bones.FindIndex(x => tunicPoint >= x.Start && tunicPoint < x.Start + x.Count);
            var b = suit.Bones[Math.Min(bone, Own - 1)];
            var best = b.Start; var bestD = float.MaxValue;
            for (var i = b.Start; i < b.Start + b.Count; i++)
            {
                var d = Vector3.DistanceSquared(suitWorld[i], tunicWorld[tunicPoint]);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }
        int Map(int p) => p >= extraStart ? p - extraStart + suitCount : Nearest(p);
        suitWorld.AddRange(tunicWorld.Skip(extraStart));
        for (var b = Own; b < tunic.Bones.Count; b++)
        {
            var t = tunic.Bones[b];
            suit.Bones.Add(t with { Start = t.Start - extraStart + suitCount, Pivot = Map(t.Pivot), Parent = t.Parent });
        }
        foreach (var f in tunic.Faces)
            if (f.Points.All(p => p >= extraStart)) suit.Faces.Add(f with { Points = f.Points.Select(Map).ToArray(), PointNormals = null });
        foreach (var l in tunic.Lines) suit.Lines.Add(l with { A = Map(l.A), B = Map(l.B) });
        foreach (var s in tunic.Spheres) if (s.Point >= extraStart) suit.Spheres.Add(s with { Point = Map(s.Point) });
        if (suit.LightScale is { } scales) suit.LightScale = scales.Take(suitCount).Concat(Enumerable.Repeat(1f, suitWorld.Count - suitCount)).ToArray();
        suit.Vertices = Enumerable.Repeat(Vector3.Zero, suitWorld.Count).ToList();
        suit.SetWorld(suitWorld.ToArray());
        return suit;
    }

    // `points` (world) into bone `bone` after its own, every later index moved along by as many, and `faces` on them (their indexes
    // `points`'): the suit's colour lit, the rest flat
    private static Body AddToBone(Body body, List<Vector3> world, int bone, List<Vector3> points, List<(int[] Points, int Colour)> faces)
    {
        var owner = body.Bones[bone];
        var at = owner.Start + owner.Count;
        var k2 = points.Count;
        int Moved(int p) => p >= at ? p + k2 : p;
        world.InsertRange(at, points);
        for (var b = 0; b < body.Bones.Count; b++)
        {
            var bn = body.Bones[b];
            if (b == bone) body.Bones[b] = bn with { Count = bn.Count + k2 };
            else if (bn.Start >= at) body.Bones[b] = bn with { Start = bn.Start + k2, Pivot = Moved(bn.Pivot) };
            else body.Bones[b] = bn with { Pivot = Moved(bn.Pivot) };
        }
        for (var i = 0; i < body.Faces.Count; i++) body.Faces[i] = body.Faces[i] with { Points = body.Faces[i].Points.Select(Moved).ToArray(), PointNormals = null };
        for (var i = 0; i < body.Lines.Count; i++) body.Lines[i] = body.Lines[i] with { A = Moved(body.Lines[i].A), B = Moved(body.Lines[i].B) };
        for (var i = 0; i < body.Spheres.Count; i++) body.Spheres[i] = body.Spheres[i] with { Point = Moved(body.Spheres[i].Point) };
        foreach (var (q, colour) in faces) body.Faces.Add(new Face(q.Select(p => at + p).ToArray(), colour, Material: colour == Bright ? -1 : 0));
        // (the body's own light scales, a point each, moved along with their points; the new ones their own, 1)
        if (body.LightScale is { } scales && scales.Length > at)
            body.LightScale = scales.Take(at).Concat(Enumerable.Repeat(1f, k2)).Concat(scales.Skip(at)).ToArray();
        body.Vertices = Enumerable.Repeat(Vector3.Zero, world.Count).ToList();
        body.SetWorld(world.ToArray());
        return body;
    }

    // How many polygons of a body are Twinsen's eyes' colour and the tunic's (-1, -1 for what isn't a body)
    private static (int Eyes, int Tunic) Counts(byte[] b)
    {
        var copy = (byte[])b.Clone();
        var eyes = RecolourPolygons(copy, Eyes, Eyes);
        return eyes < 0 ? (-1, -1) : (eyes, RecolourPolygons(copy, TunicColour, TunicColour));
    }

    // An LBA2 body's polygons of colour `from` made `to`, in its own bytes (its polygon blocks as Body.Read walks them: a type, a count,
    // then each polygon's record with its colour at 8): how many, -1 if it isn't a body that reads so
    private static int RecolourPolygons(byte[] b, int from, int to)
    {
        if (b.Length < 96) return -1;
        int p = BitConverter.ToInt32(b, 68), end = BitConverter.ToInt32(b, 76), n = 0;
        if (p < 96 || end > b.Length || p > end) return -1;
        while (p < end)
        {
            if (p + 8 > end) return -1;
            int type = BitConverter.ToUInt16(b, p), count = BitConverter.ToUInt16(b, p + 2);
            p += 8;
            bool quad = (type & 32768) != 0, env = (type & 16384) != 0, texture = (type & 255) > 7;
            var stride = env ? 16 : texture ? (quad ? 32 : 24) : 12;
            if ((type & 255) > 23 || p + count * stride > end) return -1;
            for (var i = 0; i < count; i++, p += stride)
                if (b[p + 8] == from) { b[p + 8] = (byte)to; n++; }
        }
        return n;
    }

    // The patch on his back (the user, 2026-11-10: "I'm not sure how well it'll show on screen, but let's add a patch to the back of the suit
    // saying LBRA"): a white panel across his shoulder blades -- the chest bone's back, a hair behind it -- and on it LBRA in dark letters
    // of a 3 x 5 pixel font, each pixel PatchPixel square, reading the right way round from behind him
    private const int Chest = 3, PatchPixel = 9;
    private static readonly string[] PatchLetters =
    {
        "#.. ##. ##. .#.",
        "#.. #.# #.# #.#",
        "#.. ##. ##. ###",
        "#.. #.# #.# #.#",
        "### ##. #.# #.#",
    };
    internal static Body WithBackPatch(Body body)
    {
        if (body.Bones.Count <= Chest) return body;
        var world = body.World().ToList();
        var chest = body.Bones[Chest];
        var own = world.Skip(chest.Start).Take(chest.Count).ToArray();
        if (own.Length == 0) return body;
        var lo = new Vector3(own.Min(p => p.X), own.Min(p => p.Y), own.Min(p => p.Z));
        var hi = new Vector3(own.Max(p => p.X), own.Max(p => p.Y), own.Max(p => p.Z));
        int columns = PatchLetters[0].Length, rows = PatchLetters.Length;
        float w = (columns + 2) * PatchPixel, h = (rows + 2) * PatchPixel;
        // (he faces +z: his back is the chest's least z; the panel's middle a little above the chest's)
        var centre = new Vector3((lo.X + hi.X) / 2, lo.Y + (hi.Y - lo.Y) * 0.62f, lo.Z - 3);
        var points = new List<Vector3>();
        var faces = new List<(int[] Points, int Colour)>();
        // (a quad facing -z, out of his back: x across his back -- from behind, his left is the viewer's left, -x reads left to right
        // if +x is his left: the letters' columns run from +x to -x)
        void Quad(float x0, float y0, float x1, float y1, float z, int colour)
        {
            var q = new[] { new Vector3(x0, y0, z), new Vector3(x1, y0, z), new Vector3(x1, y1, z), new Vector3(x0, y1, z) };
            var n = LBAAssembler.Terrain.Palace.PalaceTorturers.Newell(q);
            if (n.Z > 0) q = q.Reverse().ToArray();
            var first = points.Count;
            points.AddRange(q);
            faces.Add((new[] { first, first + 1, first + 2, first + 3 }, colour));
        }
        Quad(centre.X - w / 2, centre.Y - h / 2, centre.X + w / 2, centre.Y + h / 2, centre.Z, White);
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < columns; c++)
            {
                if (PatchLetters[r][c] != '#') continue;
                var x = centre.X + w / 2 - (c + 1) * PatchPixel;
                var y = centre.Y + h / 2 - (r + 1) * PatchPixel;
                Quad(x - PatchPixel, y - PatchPixel, x, y, centre.Z - 2, Dark);
            }
        return AddToBone(body, world, Chest, points, faces);
    }

    // ---- the inventory's models: single-bone bodies, upright, their fronts to +z (the side the inventory shows first)
    private static void Block(CarMesh m, Vector3 centre, Vector3 size, int colour)
    {
        var h = size / 2;
        var ids = new int[8];
        for (var i = 0; i < 8; i++)
            ids[i] = m.P(0, centre + new Vector3((i & 1) == 0 ? -h.X : h.X, (i & 2) == 0 ? -h.Y : h.Y, (i & 4) == 0 ? -h.Z : h.Z));
        var unlit = colour != Red;
        foreach (var q in new[] { new[] { 2, 3, 7, 6 }, new[] { 0, 1, 5, 4 }, new[] { 0, 2, 6, 4 }, new[] { 1, 3, 7, 5 }, new[] { 0, 1, 3, 2 }, new[] { 4, 5, 7, 6 } })
            m.Out(q.Select(k => ids[k]).ToArray(), colour, centre, unlit);
    }

    // The overalls laid out as on a hanger: the body, the legs and the arms, red; the collar, the belt and the stripes white; the zip and
    // the badge on the chest
    internal static Body OverallsModel(byte[] header)
    {
        var m = new CarMesh("the racing overalls", new[] { -1 });
        m.Light = 0.6f;
        Block(m, new Vector3(0, 1300, 0), new Vector3(900, 1000, 420), Red);
        Block(m, new Vector3(0, 1830, 0), new Vector3(520, 70, 320), White);
        Block(m, new Vector3(0, 820, 0), new Vector3(920, 80, 440), White);
        Block(m, new Vector3(0, 1300, 214), new Vector3(36, 880, 8), Dark);
        Block(m, new Vector3(250, 1560, 214), new Vector3(220, 160, 8), White);
        foreach (var side in new[] { -1, 1 })
        {
            Block(m, new Vector3(side * 230, 390, 0), new Vector3(380, 780, 380), Red);
            Block(m, new Vector3(side * 424, 390, 0), new Vector3(12, 760, 110), White);
            Block(m, new Vector3(side * 580, 1290, 0), new Vector3(240, 900, 260), Red);
            Block(m, new Vector3(side * 704, 1290, 0), new Vector3(10, 880, 90), White);
        }
        return m.ToBody(header);
    }

    // The helmet: a round red shell, a white stripe over its top from front to back, a dark visor across its front, open underneath
    internal static Body HelmetModel(byte[] header)
    {
        var m = new CarMesh("the racing helmet", new[] { -1 });
        m.Light = 0.6f;
        const int Around = 16, Down = 7;
        const float R = 1000;
        // rings from the crown (latitude 80) down to the rim (latitude -40); the crown a point
        var crown = m.P(0, new Vector3(0, R, 0));
        var rings = new int[Down][];
        float Lat(int k) => (80 - k * 20) * MathF.PI / 180;
        for (var k = 0; k < Down; k++)
            rings[k] = Enumerable.Range(0, Around).Select(i =>
            {
                var a = i * MathF.Tau / Around;
                return m.P(0, new Vector3(R * MathF.Cos(Lat(k)) * MathF.Sin(a), R * MathF.Sin(Lat(k)), R * 1.08f * MathF.Cos(Lat(k)) * MathF.Cos(a)));
            }).ToArray();
        // (which colour a polygon is by where its middle is: its angle round from the front, +z, and its height)
        int Colour(float around, float lat)
        {
            var fromFront = MathF.Abs(MathF.IEEERemainder(around, MathF.Tau));
            if (lat is > -0.45f and < 0.3f && fromFront < 1.05f) return Dark;
            var fromTop = MathF.Min(fromFront, MathF.PI - fromFront);
            if (lat > 0.1f && fromTop < 0.22f) return White;
            return Red;
        }
        for (var i = 0; i < Around; i++)
        {
            var n = (i + 1) % Around;
            var mid = (i + 0.5f) * MathF.Tau / Around;
            var top = Colour(mid, Lat(0) + 0.1f) == White ? White : Red;
            m.Out(new[] { crown, rings[0][n], rings[0][i] }, top, Vector3.Zero, top != Red);
            for (var k = 0; k + 1 < Down; k++)
            {
                var c = Colour(mid, (Lat(k) + Lat(k + 1)) / 2);
                m.Out(new[] { rings[k][i], rings[k][n], rings[k + 1][n], rings[k + 1][i] }, c, Vector3.Zero, c != Red);
            }
        }
        // the padding inside the rim, dark, seen from underneath
        var inner = Enumerable.Range(0, Around).Select(i =>
        {
            var a = i * MathF.Tau / Around;
            var r = R * 0.8f * MathF.Cos(Lat(Down - 1));
            return m.P(0, new Vector3(r * MathF.Sin(a), R * MathF.Sin(Lat(Down - 1)) + 60, r * 1.08f * MathF.Cos(a)));
        }).ToArray();
        for (var i = 0; i < Around; i++)
        {
            var n = (i + 1) % Around;
            m.Both(new[] { rings[Down - 1][i], rings[Down - 1][n], inner[n], inner[i] }, Dark, 0);
        }
        return m.ToBody(header);
    }

    // A model at the inventory's size: centred on its middle and scaled so its widest side is InventoryExtent (RaceTrackStory.Errands.cs)
    // The racing gear in one inventory slot (the user, 2026-11-08: "the gloves and helmet should share one slot"): the gloves' slot (7) is
    // 1 with the gloves, 2 with the gloves and the helmet -- the race file's inv_kinds=7 7 5 shows OBJFIX 7 (the gloves) or this, 5 (the
    // pearl's, the helmet's till then), with 5's texts. The gloves and the helmet side by side, the helmet as tall as the gloves.
    internal static Body GearModel(byte[] header)
    {
        var gloves = RaceTrackGloves.Build(RaceTrackGloves.InventoryScale, upright: true);
        var helmet = HelmetModel(header);
        Vector3[] gw = gloves.World(), hw = helmet.World();
        static (Vector3 Lo, Vector3 Hi) Box(Vector3[] w) => (w.Aggregate(new Vector3(float.MaxValue), Vector3.Min), w.Aggregate(new Vector3(float.MinValue), Vector3.Max));
        var (gl, gh) = Box(gw); var (hl, hh) = Box(hw);
        var k = (gh.Y - gl.Y) / MathF.Max(1, hh.Y - hl.Y);
        var gap = (gh.X - gl.X) * 0.12f;
        var gMid = (gl + gh) / 2; var hMid = (hl + hh) / 2;
        var points = gw.Select(p => p - gMid - new Vector3((gh.X - gl.X) / 2 + gap / 2, 0, 0))
            .Concat(hw.Select(p => (p - hMid) * k + new Vector3((hh.X - hl.X) * k / 2 + gap / 2, 0, 0))).ToArray();
        var body = new Body { Game = 2, Lit = true, Static = true, Header = header };
        body.Bones.Add(new Bone(0, points.Length, 0, -1, new byte[8]));
        body.Vertices.AddRange(points);
        body.SetWorld(points);
        foreach (var f in gloves.Faces) body.Faces.Add(f with { PointNormals = null, FaceNormal = -1 });
        foreach (var f in helmet.Faces) body.Faces.Add(f with { Points = f.Points.Select(i => i + gw.Length).ToArray(), PointNormals = null, FaceNormal = -1 });
        foreach (var sp in helmet.Spheres) body.Spheres.Add(sp with { Point = sp.Point + gw.Length, Radius = (int)Math.Round(sp.Radius * k) });
        foreach (var ln in helmet.Lines) body.Lines.Add(ln with { A = ln.A + gw.Length, B = ln.B + gw.Length });
        body.Validate();
        return body;
    }
    // the gloves' slot and its value with the helmet too
    public const int GlovesSlot = 7, GearBoth = 2;

    private static Body InventorySized(Body body)
    {
        var v = body.Vertices;
        if (v.Count == 0) return body;
        var lo = new Vector3(v.Min(p => p.X), v.Min(p => p.Y), v.Min(p => p.Z));
        var hi = new Vector3(v.Max(p => p.X), v.Max(p => p.Y), v.Max(p => p.Z));
        var middle = (lo + hi) / 2;
        var size = MathF.Max(hi.X - lo.X, MathF.Max(hi.Y - lo.Y, hi.Z - lo.Z));
        var k = size > 0 ? InventoryExtent / size : 1;
        for (var i = 0; i < v.Count; i++) v[i] = (v[i] - middle) * k;
        for (var i = 0; i < body.Spheres.Count; i++) body.Spheres[i] = body.Spheres[i] with { Radius = (int)Math.Round(body.Spheres[i].Radius * k) };
        return body;
    }
}
