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
    // the hero's bodies: his everyday clothes, the tunic's, the tunic's with a weapon
    private const int Everyday = 0, Tunic = 1, FirstArmed = 2, LastArmed = 6;
    // (the racing gloves' colours, RaceTrackGloves: the red lit along its ramp, the white and the dark flat)
    private const int Red = 64, White = 62, Dark = 49;
    // (on Twinsen: a ramp's start is its darkest -- 64 came out maroon -- so his overalls' red six shades up it, as his jeans' blue is
    // 194; the shirt's polygons are textured, its pattern taken off)
    private const int Bright = 70;
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
        (HelmetSlot + 100, new[] { "Racing helmet", "Casque de course", "Rennhelm", "Casco de carreras", "Casco da corsa", "Capacete de corrida" }),
        (HelmetSlot + 200, new[]
        {
            "A racing helmet, from the giant clam's sea cave. No helmet, no race: Flick lets nobody onto the Desert track without one.",
            "Un casque de course, trouvé dans la grotte marine du coquillage géant. Pas de casque, pas de course : Flick ne laisse personne sur la piste du Désert sans casque.",
            "Ein Rennhelm aus der Meeresgrotte der Riesenmuschel. Ohne Helm kein Rennen: Flick läßt niemanden ohne Helm auf die Wüstenstrecke.",
            "Un casco de carreras, de la gruta marina del molusco gigante. Sin casco no hay carrera: Flick no deja a nadie entrar en la pista del Desierto sin él.",
            "Un casco da corsa, dalla grotta marina della conchiglia gigante. Niente casco, niente corsa: Flick non lascia nessuno sulla pista del Deserto senza.",
            "Um capacete de corrida, da gruta marinha da concha gigante. Sem capacete, sem corrida: o Flick não deixa ninguém entrar na pista do Deserto sem um.",
        }),
    };

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        // ---- Twinsen's bodies
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var bodies = HqrArchive.Open(bodyPath);
        var overalls = Overalls(Body.Read(bodies.Read(Everyday), 2));
        var armed = new List<(int Entry, Body Body, int Faces)>();
        for (var b = FirstArmed; b <= LastArmed; b++)
        {
            var body = Body.Read(bodies.Read(b), 2);
            var n = 0;
            for (var i = 0; i < body.Faces.Count; i++)
                if (body.Faces[i].Colour is >= TunicBlue and < TunicBlue + 16) { body.Faces[i] = body.Faces[i] with { Colour = Bright + body.Faces[i].Colour - TunicBlue, Texture = null }; n++; }
            armed.Add((b, body, n));
        }
        var helmeted = WithHelmet(Overalls(Body.Read(bodies.Read(Everyday), 2)));
        HqrWriter.ReplaceInFile(bodyPath, Tunic, overalls.Write());
        foreach (var (entry, body, _) in armed) HqrWriter.ReplaceInFile(bodyPath, entry, body.Write());
        // (the helmeted one: its entry again on a second pass, else one more at the file's end, and the hero's entity told of it)
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        if (RaceTrackGuards.BodyOf(table, HeroEntity, HelmetBody) is { } had) HqrWriter.ReplaceInFile(bodyPath, had, helmeted.Write());
        else
        {
            var index = HqrArchive.CountEntries(bodyPath);
            HqrWriter.AppendToFile(bodyPath, helmeted.Write());
            HqrWriter.ReplaceInFile(ressPath, 44, RaceTrackBaldinoCar.WithBody(table, HeroEntity, HelmetBody, index));
        }
        log.Add($"Twinsen's racing overalls with the helmet on: the hero's body {HelmetBody} ({helmeted.Vertices.Count} points, {helmeted.Faces.Count} polygons)");
        log.Add($"Twinsen's racing overalls: BODY.HQR {Tunic} (the tunic's) his everyday body in racing red with a white belt and stripes; " +
                $"{FirstArmed}-{LastArmed} (the tunic's with a weapon) the tunic's blue made red ({string.Join(", ", armed.Select(a => a.Faces))} polygons)");

        // ---- the inventory's models
        var objfixPath = Path.Combine(gameDirectory, "OBJFIX.HQR");
        var header = Body.Read(bodies.Read(30), 2, allowStatic: true).Header;
        HqrWriter.ReplaceInFile(objfixPath, OverallsSlot, InventorySized(OverallsModel(header)).Write());
        HqrWriter.ReplaceInFile(objfixPath, HelmetSlot, InventorySized(HelmetModel(header)).Write());
        log.Add($"OBJFIX.HQR {OverallsSlot}: the racing overalls (the tunic's model); {HelmetSlot}: the racing helmet (the pearl's)");

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

    // The helmet on his head: a shell round the head bone's points -- red, a white stripe over the top, a dark visor across the face, the
    // padding dark inside the rim -- its points the head's (added after the head's own, every later bone's moved along)
    internal static Body WithHelmet(Body body)
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
        const int Around = 12, Down = 6;
        float Lat(int k) => (75 - k * 22) * MathF.PI / 180;   // 75 down to -35
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

        // into the body: the points after the head's own, every later index moved along by as many
        var at = head.Start + head.Count;
        var k2 = points.Count;
        int Moved(int p) => p >= at ? p + k2 : p;
        world.InsertRange(at, points);
        for (var b = 0; b < body.Bones.Count; b++)
        {
            var bone = body.Bones[b];
            if (b == Head) body.Bones[b] = bone with { Count = bone.Count + k2 };
            else if (b > Head) body.Bones[b] = bone with { Start = bone.Start + k2, Pivot = Moved(bone.Pivot) };
        }
        for (var i = 0; i < body.Faces.Count; i++) body.Faces[i] = body.Faces[i] with { Points = body.Faces[i].Points.Select(Moved).ToArray(), PointNormals = null };
        for (var i = 0; i < body.Lines.Count; i++) body.Lines[i] = body.Lines[i] with { A = Moved(body.Lines[i].A), B = Moved(body.Lines[i].B) };
        for (var i = 0; i < body.Spheres.Count; i++) body.Spheres[i] = body.Spheres[i] with { Point = Moved(body.Spheres[i].Point) };
        foreach (var (q, colour) in faces) body.Faces.Add(new Face(q.Select(p => at + p).ToArray(), colour, Material: colour == Bright ? -1 : 0));
        // (the body's own light scales, a point each, moved along with their points; the helmet's its own, 1)
        if (body.LightScale is { } scales && scales.Length > at)
            body.LightScale = scales.Take(at).Concat(Enumerable.Repeat(1f, k2)).Concat(scales.Skip(at)).ToArray();
        body.Vertices = Enumerable.Repeat(Vector3.Zero, world.Count).ToList();
        body.SetWorld(world.ToArray());
        return body;
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
