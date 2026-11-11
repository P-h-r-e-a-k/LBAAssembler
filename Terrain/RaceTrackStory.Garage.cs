using System.IO;
using System.Numerics;
using System.Text;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The White Leaf Desert's racers' garage and the sand filters (item 6 of the Nov 7 notes; the user, 2026-11-07: "a massive tent with
// the racers inside working on their cars"; "Twinsen collects the sand filters from Baldino and takes them to Zoe, who installs them";
// Baldino "can be in the garage/tent instead" of his house, "never in two places at once"; "without the filters Twinsen may still
// start the race, but the car breaks down part-way through ... smoke coming out of the car ... then he must race a second time"):
//   - the tent is the island's (RaceTrackBuilder.GarageTent, report.Garage); in its scene: three of the racers' cars along its back
//     wall with a mechanic at work by two of them, Baldino by the right-hand end and Zoe by the left;
//   - Baldino is there in chapter 3 till the race is won, out of his house (he's never in both: his house's talker only when he isn't
//     at the garage); talked to, he gives the sand filters (item FiltersSlot -- the game's Protection spell's, which the story never
//     gives -- and Q_FILTERS 1), and once Zoe has fitted them his tune and his tip (the house's old lines);
//   - Zoe, talked to with the filters, fits them (the item gone, Q_FILTERS 2); before, she sends Twinsen to Baldino; after, the car's
//     ready;
//   - the race file's breakdown= (RACEMOD.CPP): without the filters fitted, past the lap's second checkpoint, the car slows to a crawl,
//     smoking, to the race's end -- lost; raced again once they're in. Its inv_kinds= shows the filters' own model in the slot, and the
//     game's spell for that slot is never cast (PERSO.CPP).
internal static partial class RaceTrackStory
{
    // Q_FILTERS: 0 nothing, 1 Baldino has given the filters, 2 Zoe has fitted them (a spare game variable: RaceTrackStory.Eggs.cs's list)
    public const int QFilters = 106, FiltersGiven = 1, FiltersFitted = 2;
    public const int FiltersSlot = 39, FiltersModel = 156;
    // the breakdown: after which checkpoint of the first lap, and how fast the car goes then (units a second: about 13 km/h)
    private const int BreakdownCheckpoint = 2, BreakdownSpeed = 1800;
    private const string BreakdownText = "Sand in the engine! Zoe needs Baldino's sand filters: back to the garage.";

    // The Desert race file's lines for the filters
    internal static string GarageRaceLines() =>
        $"inv_kinds={FiltersSlot} {FiltersModel}\nbreakdown={QFilters} {FiltersFitted} {BreakdownCheckpoint} {BreakdownSpeed} {BreakdownText}\n" +
        // (and Chedil Amiradoo on his magic carpet, round the island: on the ground as the game had him, but never under the track's raised
        // ground nor stopped by its decors, and well over the road wherever he comes near it -- F14, the user, 2026-11-08: "raise his
        // actors path above ground", "check that during a race he won't be in the way of the track at any point": RACEMOD.CPP hover=)
        $"hover={CarpetPeddler} 0 {CarpetOverRoad} {CarpetRoadReach}\n";
    // (1200 over the road -- a car clears under him -- within half the reach of the lap's line, easing down to his own height over the
    // ground at 3200: the user, 2026-11-08, "Our wizard fix has left him high in the air" -- 1600 anywhere within 5000, over the beach too)
    private const int CarpetPeddler = 169, CarpetOverRoad = 1200, CarpetRoadReach = 3200;

    private static readonly string[][] FiltersTexts =
    {
        new[] { "Baldino gave you a pair of sand filters. Zoe will fit them to your car.", "Sand filters", "Baldino's sand filters, to keep the desert's sand out of your engine. Zoe will fit them." },
        new[] { "Baldino t'a donné une paire de filtres à sable. Zoé les posera sur ta voiture.", "Filtres à sable", "Les filtres à sable de Baldino, pour garder le sable du désert hors de ton moteur. Zoé les posera." },
        new[] { "Baldino hat dir ein Paar Sandfilter gegeben. Zoe baut sie in dein Auto ein.", "Sandfilter", "Baldinos Sandfilter, damit der Wüstensand nicht in deinen Motor kommt. Zoe baut sie ein." },
        new[] { "Baldino te ha dado un par de filtros de arena. Zoe los pondrá en tu coche.", "Filtros de arena", "Los filtros de arena de Baldino, para que la arena del desierto no entre en tu motor. Zoe los pondrá." },
        new[] { "Baldino ti ha dato un paio di filtri per la sabbia. Zoe li monterà sulla tua macchina.", "Filtri per la sabbia", "I filtri per la sabbia di Baldino, per tenere la sabbia del deserto fuori dal motore. Li monterà Zoe." },
        new[] { "O Baldino deu-te um par de filtros de areia. A Zoe monta-os no teu carro.", "Filtros de areia", "Os filtros de areia do Baldino, para manter a areia do deserto fora do motor. A Zoe monta-os." },
    };

    // The filters into the game folder: their inventory model and texts
    private static void FiltersInstall(string gameDirectory, List<string> log)
    {
        var objfixPath = Path.Combine(gameDirectory, "OBJFIX.HQR");
        var objfix = HqrArchive.Open(objfixPath);
        var header = Body.Read(HqrArchive.Open(Path.Combine(gameDirectory, "BODY.HQR")).Read(30), 2, allowStatic: true).Header;
        var bytes = File.ReadAllBytes(objfixPath);
        var count = HqrArchive.CountEntries(objfixPath);
        var filler = HqrWriter.StoredEntry(objfix.Read(0));
        while (count < FiltersModel + 1) { bytes = HqrWriter.AppendEntry(bytes, filler); count++; }
        bytes = HqrWriter.ReplaceEntry(bytes, FiltersModel, HqrWriter.StoredEntry(InventorySized(FiltersBody(header)).Write()));
        File.WriteAllBytes(objfixPath, bytes);
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        var hqr = File.ReadAllBytes(textPath);
        for (var lang = 0; lang < languages; lang++)
        {
            var bank = Lba2TextBank.Load(text, lang, HolomapTexts);
            var words = FiltersTexts[lang < FiltersTexts.Length ? lang : 0];
            for (var k = 0; k < 3; k++)
            {
                var id = FiltersModel + 100 * k;
                var wanted = Dos.GetBytes(words[k]);
                if (bank.Find(id) is { } have) have.Bytes = wanted;
                else bank.Texts.Add(new Lba2TextBank.Text { Id = id, Attribute = Lba2TextBank.NormalAttribute, Bytes = wanted });
            }
            hqr = bank.WriteInto(hqr);
        }
        File.WriteAllBytes(textPath, hqr);
        log.Add($"the sand filters: item {FiltersSlot}, OBJFIX.HQR {FiltersModel}, texts {FiltersModel}/+100/+200");
    }

    // A pair of round air filters, one on the other: pleated paper rings (ochre, in folds) between dark caps, a steel rim round each
    private static Body FiltersBody(byte[] header)
    {
        var m = new CarMesh("the sand filters", new[] { -1 });
        m.Light = 0.6f;
        const int Around = 16;
        const float R = 900, Inner = 380, High = 520;
        const int Paper = 98, PaperDark = 96, Cap = 49, Rim = 56;
        foreach (var y0 in new[] { 0f, High + 60 })
        {
            var lo = m.Loop(0, new Vector3(0, y0, 0), new Vector3(R, 0, 0), new Vector3(0, 0, R), Around);
            var hi = m.Loop(0, new Vector3(0, y0 + High, 0), new Vector3(R, 0, 0), new Vector3(0, 0, R), Around);
            var loIn = m.Loop(0, new Vector3(0, y0, 0), new Vector3(Inner, 0, 0), new Vector3(0, 0, Inner), Around);
            var hiIn = m.Loop(0, new Vector3(0, y0 + High, 0), new Vector3(Inner, 0, 0), new Vector3(0, 0, Inner), Around);
            var middle = new Vector3(0, y0 + High / 2, 0);
            for (var i = 0; i < Around; i++)
            {
                var n = (i + 1) % Around;
                m.Out(new[] { lo[i], lo[n], hi[n], hi[i] }, i % 2 == 0 ? Paper : PaperDark, middle);
                m.Both(new[] { hi[i], hi[n], hiIn[n], hiIn[i] }, Cap, 0);
                m.Both(new[] { lo[i], lo[n], loIn[n], loIn[i] }, Cap, 0);
            }
            var rim = m.Loop(0, new Vector3(0, y0 + High + 20, 0), new Vector3(R + 40, 0, 0), new Vector3(0, 0, R + 40), Around);
            for (var i = 0; i < Around; i++) m.Both(new[] { hi[i], hi[(i + 1) % Around], rim[(i + 1) % Around], rim[i] }, Rim, -1);
        }
        return m.ToBody(header);
    }

    // The garage's people and cars, each in the scene of the cube it stands in (the tent may reach over a cube's edge: the Desert's does);
    // and Baldino's house without him while he's there. `desert`: the island's chapters (the story's); `baldino`: a copy of the game's
    // Baldino (null: none); `startScene`: the start line's (its grid cars, whose bodies the tent's cars are)
    private static void DesertGarage(string gameDirectory, IslandBeatsA b, RaceTrackReport report, Func<DesertLine, int> say, string desert, SceneActorModel? baldino, int startScene, List<string> log)
    {
        if (report.Garage is not { } g) { log.Add("the racers' garage left out: the island has no tent (RaceTrackBuilder.GarageTent)"); return; }
        FiltersInstall(gameDirectory, log);
        var garageTime = $"{desert} && {PagesWon} > var_game({QPages})";
        var facingOut = RaceTrackMath.Turn(Math.Atan2(g.OutX, g.OutZ));
        var stand = "void comportement_0()\n{\n}\n";
        // who stands where: `u` along the tent, `v` from its middle towards its open front (world units)
        var people = new List<(SceneActorModel Actor, double U, double V, int Beta, string Life, string Who)>();
        var start = b.Store.Load(startScene);
        var grid = start.Actors.Where(a => a.Entity == RaceTrackScenes.RacerEntity && a.Body >= 0).Take(5).ToList();
        var mechanic = start.Actors.FirstOrDefault(a => a.Entity == 30 && a.Body >= 0);
        var size = report.GarageSize;
        // where Baldino and Zoe stand: the tent's ends, or (with its bays) Zoe in Twinsen's bay and Baldino in the aisle before the bays
        double baldinoU = 2050, baldinoV = 500, zoeU = -2050, zoeV = 500;
        if (size.Bays > 0)
        {
            // A bay for each car (the user, 2026-11-11: "A specific garage space for every car with room around them to be worked on"):
            // the tent's back, size.Bays of them side by side, each car in the middle of its own nose out, a mechanic at work beside it.
            // The first bay, the end nearest the start line, is Twinsen's: his car is out on the road, Zoe waits in it with the tools.
            double hl = size.Long * 256, hd = size.Deep * 256, w = 2 * hl / size.Bays, bayV = -hd + size.BayDeep * 512 / 2;
            for (var k = 1; k < size.Bays && k - 1 < grid.Count; k++)
            {
                var u = -hl + w * (k + 0.5);
                var car = grid[k - 1].Clone();
                car.Flags &= ~1u;
                people.Add((car, u, bayV, facingOut, stand, "a car in its bay"));
                if (mechanic is not null)
                {
                    // (by the car's side, a little towards the front, turned to it -- the car's side alternating bay by bay)
                    var side = k % 2 == 0 ? 1 : -1;
                    var mu = u + side * w * 0.3;
                    var beta = RaceTrackMath.Turn(Math.Atan2(-side * g.AlongX, -side * g.AlongZ));
                    people.Add((mechanic.Clone(), mu, bayV + 400, beta, stand, "a mechanic"));
                }
            }
            zoeU = -hl + w * 0.5; zoeV = bayV + 600;
            baldinoU = 0; baldinoV = -hd + size.BayDeep * 512 + 900;
        }
        else
        {
            // three of the racers' cars along the back, nose out (the start line's grid cars' bodies), a mechanic at work by two of them
            foreach (var (u, k) in new[] { (-1700.0, 0), (0.0, 2), (1700.0, 4) })
                if (k < grid.Count)
                {
                    var car = grid[k].Clone();
                    car.Flags &= ~1u;
                    people.Add((car, u, -700, facingOut, stand, "a car"));
                }
            if (mechanic is not null)
                foreach (var u in new[] { -1150.0, 550.0 }) people.Add((mechanic.Clone(), u, -350, (facingOut + 1024) % 4096, stand, "a mechanic"));
        }
        // Baldino: the filters, then (fitted) his tune and tip -- while it's garage time
        if (baldino is not null)
            people.Add((baldino.Clone(), baldinoU, baldinoV, facingOut, Talker(garageTime, $@"            if (0 == var_game({QFilters}))
            {{
                message({say(DesertLine.BaldinoFilters)});
                set_var_game({FiltersSlot}, 1);
                found_object({FiltersSlot});
                set_var_game({QFilters}, {FiltersGiven});
            }}
            else if ({FiltersFitted} > var_game({QFilters}))
            {{
                message({say(DesertLine.BaldinoFilters)});
            }}
            else
            {{
                message({say(DesertLine.BaldinoTuned)});
                message({say(DesertLine.BaldinoRacing)});
            }}"), "Baldino"));
        // Zoe: she fits the filters
        if (Copy(b.Originals, OpeningScene, Zoe) is { } zoe)
            people.Add((zoe, zoeU, zoeV, facingOut, Talker(desert, $@"            if (1 == var_game({FiltersSlot}))
            {{
                message({say(DesertLine.ZoeFitted)});
                set_var_game({FiltersSlot}, 0);
                set_var_game({QFilters}, {FiltersFitted});
            }}
            else if ({FiltersFitted} > var_game({QFilters}))
            {{
                message({say(DesertLine.ZoeNoFilters)});
            }}
            else
            {{
                message({say(DesertLine.ZoeReady)});
            }}"), "Zoe"));
        // each into its cube's scene
        var scenes = b.OutsideScenes.ToDictionary(s => (b.Store.Load(s).CubeX, b.Store.Load(s).CubeY));
        foreach (var group in people.GroupBy(p =>
                 {
                     double wx = g.X * 512 + g.AlongX * p.U + g.OutX * p.V, wz = g.Z * 512 + g.AlongZ * p.U + g.OutZ * p.V;
                     return ((int)Math.Floor(wx / IslandFile.CubeSize), (int)Math.Floor(wz / IslandFile.CubeSize));
                 }))
        {
            if (!scenes.TryGetValue(group.Key, out var scene)) { log.Add($"the racers' garage: no outside scene for cube {group.Key} -- {string.Join(", ", group.Select(p => p.Who))} left out"); continue; }
            b.Edit(scene, $"the racers' garage: {string.Join(", ", group.Select(p => p.Who))}", model =>
            {
                var edits = new List<Action<SceneScripts>>();
                foreach (var (actor, u, v, beta, life, _) in group)
                {
                    double wx = g.X * 512 + g.AlongX * u + g.OutX * v, wz = g.Z * 512 + g.AlongZ * u + g.OutZ * v;
                    actor.X = (int)Math.Round(wx - group.Key.Item1 * IslandFile.CubeSize);
                    actor.Z = (int)Math.Round(wz - group.Key.Item2 * IslandFile.CubeSize);
                    actor.Y = (int)Math.Round(g.Y);
                    actor.Beta = beta;
                    edits.Add(IslandBeatsA.Add(model, actor, life));
                }
                return scripts => { foreach (var e in edits) e(scripts); };
            });
        }
        log.Add($"the racers' garage: {people.Count} placed (Baldino {(baldino is null ? "left out" : "there")} while {garageTime})");
    }
}
