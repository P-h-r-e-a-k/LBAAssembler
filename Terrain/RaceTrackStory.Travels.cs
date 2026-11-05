using System.IO;
using System.Text.RegularExpressions;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The story's ways from island to island, once every track of a build is built (RaceTrackService.Build, after its tracks; BuildFiles itself
// when it builds one track alone).
//   - Its doors into scenes past 255 (a change_cube names a scene up to 255 only): the lava lake's ship to the Francos (scene 95 into 257,
//     chapter 6 to 7), the volcano ferryman's boat to Island CX (130 into 343, chapter 11 to 12), the soldiers' transport from Island CX to
//     the palace (343 into the palace race's start, chapter 12 to 13). Each island's story asks for its door (PendingTravel) and it is put
//     in here, at the end: the island it goes to may be built after the one it leaves -- the volcano before Island CX Mk2, whose start
//     scene its track's build makes ("no Island CX start scene in the folder: the ferryman's boat goes nowhere", allstory14, 2026-11-05)
//     -- and a later track on the same scene clears doors from its road -- the statue track's, on scene 95 too, took the lava lake's ship
//     as "a door under the road's surface" (in the other weather's file: the lava lake's road isn't the statue's). Where Twinsen comes out
//     is the scene's start as it is then, after its own track has put it on the grid.
//   - Then every travel the story's scenes make is looked at in the finished folder (CheckTravels), and any that goes nowhere is said in
//     the build's log as a WARNING: a change_cube to a scene the folder hasn't got, a set_change_cube(n, 1) with no door n in its scene, a
//     door into a scene that isn't there -- each only where the game's own scene didn't already have it so.
//   - And the game's own ways that would take Twinsen past a chapter, or into a place he can't get out of, shut in the story
//     (RetailTravels): Otringal's prison and harbour taxi, the Francos' taxis, the Desert's ferry office, Johnny Rocket's ring.
internal static partial class RaceTrackStory
{
    // A door a story asks for: in `Scene`, into `Target`, off until the story's script switches it on (set_change_cube(`Switch`, 1)).
    // `Door` makes it from the scene as it is once every track is built (its start may have moved since: the statue track's grid is scene
    // 95's start, where the lava lake's ship waits): the zone's box and, from the target's start, how far Twinsen comes out from it (the
    // zone's corner is that far before the place round which it was put: he comes out where he stood in it, from its corner). `What`: for
    // the log.
    public sealed record PendingTravel(int Scene, int Target, int Switch, string What, Func<SceneModel, (SceneZoneModel Zone, (int X, int Y, int Z) Offset)> Door);

    // A door round a place in a scene (cube-local), `reach` either way, from height `y0` to `y1`, into `target`, Twinsen coming out at
    // the target's start raised by `dy`: the zone (its landing still to add the target's start to) and that offset
    public static (SceneZoneModel Zone, (int X, int Y, int Z) Offset) DoorRound(int x, int z, int reach, int y0, int y1, int target, int zoneSwitch, int dy)
    {
        static int Clamp(int v) => Math.Clamp(v, 0, IslandFile.CubeSize - 1);
        (int X, int Y, int Z) offset = (-(x - Clamp(x - reach)), dy, -(z - Clamp(z - reach)));
        return (new SceneZoneModel
        {
            Type = 0, Num = target, X0 = Clamp(x - reach), X1 = Clamp(x + reach), Z0 = Clamp(z - reach), Z1 = Clamp(z + reach), Y0 = y0, Y1 = y1,
            Info = new[] { offset.X, offset.Y, offset.Z, 0, zoneSwitch, 0, 0, 0 },
        }, offset);
    }

    // The game's own ways shut, the doors asked for put in, then the check. `story`: the build's islands with the story (none: nothing
    // looked at).
    public static List<string> Travels(string gameDirectory, IReadOnlyList<PendingTravel> pending, IReadOnlyCollection<RaceTrackIsland> story)
    {
        var log = new List<string>();
        if (story.Count == 0 && pending.Count == 0) return log;
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var changes = new Dictionary<int, SceneModel>();
        RetailTravels(gameDirectory, store, changes, story, log);
        SceneModel Model(int scene) => changes.TryGetValue(scene, out var m) ? m : changes[scene] = store.Load(scene);
        foreach (var p in pending)
        {
            if (!store.SceneExists(p.Target)) { log.Add($"WARNING: the story's travel {p.What} (scene {p.Scene} into scene {p.Target}) goes nowhere: no scene {p.Target} in the folder"); continue; }
            if (!store.SceneExists(p.Scene)) { log.Add($"WARNING: the story's travel {p.What}: no scene {p.Scene} to put its door in"); continue; }
            var model = Model(p.Scene);
            var zoneSwitch = p.Switch;
            if (!SetsChangeCube(model, p.Scene, zoneSwitch)) { log.Add($"WARNING: the story's travel {p.What}: nothing in scene {p.Scene} switches its door on (set_change_cube({zoneSwitch}, 1)) -- left out"); continue; }
            var to = store.Load(p.Target).Hero;
            var (zone, offset) = p.Door(model);
            zone.Num = p.Target;
            zone.Info[0] = to.X + offset.X; zone.Info[1] = to.Y + offset.Y; zone.Info[2] = to.Z + offset.Z; zone.Info[4] = zoneSwitch;
            try { SceneOps.AddZone(model, zone); }
            catch (SceneEditException e) { log.Add($"WARNING: the story's travel {p.What}: {e.Message}"); continue; }
            log.Add($"scene {p.Scene}: {p.What}: a door ({zone.X0}..{zone.X1}, {zone.Y0}..{zone.Y1}, {zone.Z0}..{zone.Z1}) into scene {p.Target} (switch {zoneSwitch}), Twinsen out at ({zone.Info[0]},{zone.Info[1]},{zone.Info[2]})");
        }
        if (changes.Count > 0) store.SaveMany(changes.Select(c => new SceneChange(c.Key, c.Value, null)).ToList(), allowErrors: true);
        if (story.Count > 0) log.AddRange(CheckTravels(store));
        return log;
    }

    // ---- The game's own ways, shut in the story ------------------------------------------------------------------------------------
    // (the review of allstory14, 2026-11-05: E:\dump\TEMP\sweep\findings_island2.md #2, findings_island4.md #1, #2, #5, findings_rest.md 8-2)
    //   - Otringal's prison: the race-track mode holds variable 83 at 4 (RaceTrackIsland.Otringal's HeldVars: the space port's guard gone,
    //     the arrest in scene 86 skipped, the halls' doors 10 and 11 shut), and scene 84 starts Twinsen in his cell while it is 4 or under --
    //     its way out needs 5, which the hold undoes: a door into the prison is a door into a cell for good. Scene 90's (the town's) doors
    //     into the prison's entrance (scene 83, switches 1 and 2) and into the prison itself (84) are taken out.
    //   - Otringal's harbour taxi (scene 87, Leontine): 100 zlitos to Celebration Island in chapter 5, past the race, the Fragment and
    //     Johnny Rocket's flight (inc_chapter) -- in chapter 5 and before, the driver won't go (OtringalTaxiLines).
    //   - Johnny Rocket in his hotel room (scene 79): the game's dissidents' quest gives the Dissidents' Ring (item 16) there, which the
    //     Mosquibees' Queen asks for in chapter 10 -- he isn't there before chapter 10 (and Twinsen's arrival in the room, which waits on
    //     him, doesn't play).
    //   - The Francos' taxis (scene 109): 5 zlitos to Otringal or Celebration Island -- islands of chapters 5 and 6, from chapter 7's.
    //     Gone in the story: the Francos are left by the Elevator Platform's way, the race's.
    //   - The Desert's ferry office (scene 40) and the harbour (scene 65): a ticket back to Citadel Island in chapter 3. The clerk sells none
    //     before chapter 4 (DesertFerryLines), and none boards on one.
    //   - Island CX Mk2's copies of the game's Island CX outside (scenes 340-343): its door into scene 179 (walked into from scene 340's
    //     side, x 256, in chapter 12: on to the game's own Island CX, scene 110) taken out, and the Action spot (zone 1) whose ride goes to
    //     scene 176 (on to the Mosquibees' throne room and scene 110) shut from chapter 12 -- both reached in headless runs of the
    //     2026-11-05 build. (Its doors into scene 181 are RetireQuests'.)
    private static readonly string[][] OtringalTaxiLines =
    {
        new[]
        {
            "Celebration? Not today, mac: the Cup's got every cab parked till the race is run.",
            "La Célébration ? Pas aujourd'hui, mon vieux : avec la Coupe, tous les taxis restent au garage tant que la course n'est pas courue.",
            "Zur Insel der Feierlichkeiten? Heute nicht, Kumpel: Wegen des Pokals bleibt jedes Taxi stehen, bis das Rennen gefahren ist.",
            "¿La Celebración? Hoy no, amigo: con la Copa, todos los taxis se quedan aparcados hasta que se corra la carrera.",
            "La Celebrazione? Oggi no, amico: con la Coppa, tutti i taxi restano fermi finché la corsa non è corsa.",
            "A Celebração? Hoje não, amigo: com a Taça, os táxis ficam todos parados até a corrida estar corrida.",
        },
    };
    private static readonly string[][] DesertFerryLines =
    {
        new[]
        {
            "No crossings to Citadel while the Cup's in town, sir. The ferries are all booked for the race crowd.",
            "Pas de traversée pour la Citadelle tant que la Coupe est en ville, monsieur. Les ferries sont tous réservés pour le public de la course.",
            "Keine Überfahrten zur Zitadelleninsel, solange der Pokal in der Stadt ist, mein Herr. Die Fähren sind alle für die Rennzuschauer ausgebucht.",
            "No hay travesías a la Ciudadela mientras la Copa esté en la ciudad, señor. Los ferris están todos reservados para el público de la carrera.",
            "Niente traversate per la Cittadella finché la Coppa è in città, signore. I traghetti sono tutti prenotati per il pubblico della corsa.",
            "Não há travessias para a Cidadela enquanto a Taça estiver na cidade, senhor. Os ferries estão todos reservados para o público da corrida.",
        },
    };
    private const int PrisonEntrance = 83, Prison = 84, OtringalTown = 90, OtringalHarbour = 87, JohnnyRoom = 79, FrancosVillage = 109,
                      DesertFerryOffice = 40, DesertHarbour = 65, CxRetailWay = 179, CxRetailRide = 176;

    private static void RetailTravels(string gameDirectory, SceneStore store, Dictionary<int, SceneModel> changes, IReadOnlyCollection<RaceTrackIsland> story, List<string> log)
    {
        void Edit(int scene, string what, Func<SceneModel, Action<SceneScripts>> edit)
        {
            try
            {
                if (!store.SceneExists(scene)) throw new NotTheGames($"no scene {scene}");
                var model = SceneSerializer.Parse(SceneGame.Lba2, SceneSerializer.Write(changes.TryGetValue(scene, out var had) ? had : store.Load(scene)));
                var scriptEdit = edit(model);
                var scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
                scriptEdit(scripts);
                var built = scripts.Build();
                if (!built.Ok) { log.Add($"WARNING: scene {scene}: {what}: left out, the scripts would not compile: {string.Join("; ", built.Errors)}"); return; }
                changes[scene] = SceneSerializer.Parse(SceneGame.Lba2, built.Record!);
                log.Add($"scene {scene}: {what}");
            }
            catch (Exception e) when (e is NotTheGames or ScriptCompileException or InvalidDataException or ArgumentException or SceneEditException)
            {
                log.Add($"WARNING: scene {scene}: {what} left out: {(e is NotTheGames ? "the script isn't the game's own: " : "")}{e.Message}");
            }
        }
        // (the actors whose life scripts have `sign` in them: the build may have taken actors out before them, so not by their numbers; and
        // not by what they say -- IslandRetell gives a retold line a new text)
        static List<int> ActorsWith(SceneScripts scripts, string sign, int count = 1)
        {
            var found = Enumerable.Range(1, Math.Max(0, scripts.ActorCount - 1)).Where(a => scripts.GetText(a, ScriptKind.Life).Contains(sign, StringComparison.Ordinal)).ToList();
            if (found.Count != count) throw new NotTheGames($"{found.Count} actors whose scripts have \"{sign}\", not {count}");
            return found;
        }
        static int ActorWith(SceneScripts scripts, string sign) => ActorsWith(scripts, sign)[0];

        if (story.Contains(RaceTrackIsland.Otringal))
        {
            Edit(OtringalTown, $"the doors into Otringal's prison (scenes {PrisonEntrance} and {Prison}) taken out: the race-track mode holds variable 83 at 4, where the prison starts Twinsen in his cell for good", model =>
            {
                var shut = 0;
                for (var z = model.Zones.Count - 1; z >= 0; z--)
                    if (model.Zones[z].Type == 0 && model.Zones[z].Num is PrisonEntrance or Prison) { SceneOps.DeleteZone(model, z); shut++; }
                if (shut == 0) throw new NotTheGames("no door into the prison");
                return _ => { };
            });
            var taxi = AddIslandTexts(gameDirectory, RaceTrackIsland.Otringal.IslandByte, OtringalTaxiLines, log, "Otringal's harbour taxi");
            if (taxi >= 0)
                Edit(OtringalHarbour, "the harbour taxi won't fly to Celebration Island in chapter 5 (or before): the Cup's cabs stay parked till the race is run", _ => scripts =>
                {
                    var driver = ActorWith(scripts, "set_var_cube(2, 95)");
                    var life = scripts.GetText(driver, ScriptKind.Life);
                    scripts.SetText(driver, ScriptKind.Life, AtStart(life, "comportement_3", $@"    if (21 == l_track() && 6 > chapter())
    {{
        message({taxi});
        restore_l_track();
        set_var_cube(0, 0);
        set_comportement(comportement_1);
        return;
    }}
"));
                });
            Edit(JohnnyRoom, "Johnny Rocket's room: no Dissidents' Ring (item 16) before chapter 10 -- he isn't there, and Twinsen's arrival that waits on him doesn't play", _ => scripts =>
            {
                var johnny = ActorWith(scripts, "found_object(16)");
                scripts.SetText(johnny, ScriptKind.Life, AtStart(scripts.GetText(johnny, ScriptKind.Life), "comportement_0", @"    if (10 > chapter())
    {
        suicide();
    }
"));
                scripts.SetText(0, ScriptKind.Life, Once(scripts.GetText(0, ScriptKind.Life), "if (3 == var_game(124))", "if (3 == var_game(124) && 9 < chapter())"));
            });
        }
        if (story.Contains(RaceTrackIsland.CxTowerMk2))
            foreach (var scene in RaceTrackIsland.CxTowerMk2.MoreScenes.Prepend(RaceTrackIsland.CxTowerMk2.FirstScene))
                Edit(scene, $"Island CX Mk2: no door into scene {CxRetailWay} (on to the game's Island CX, scene 110), and the Action spot's ride to scene {CxRetailRide} (the Mosquibees' throne room and scene 110) shut", model =>
                {
                    for (var z = model.Zones.Count - 1; z >= 0; z--)
                        if (model.Zones[z].Type == 0 && model.Zones[z].Num == CxRetailWay) SceneOps.DeleteZone(model, z);
                    return scripts =>
                    {
                        var hero = scripts.GetText(0, ScriptKind.Life);
                        if (!hero.Contains($"change_cube({CxRetailRide});", StringComparison.Ordinal)) return;
                        var ride = new Regex(@"if \(1 == zone\(\)\)(\s*\{\s*set_dir\(NO_MOVE\);)");
                        if (!ride.IsMatch(hero)) throw new NotTheGames($"no Action spot (zone 1) before the ride to scene {CxRetailRide}");
                        scripts.SetText(0, ScriptKind.Life, ride.Replace(hero, $"if (1 == zone() && {CxChapter} > chapter())$1"));
                    };
                });
        if (story.Contains(RaceTrackIsland.Knartas))
            Edit(FrancosVillage, "the Francos' two taxis (to Otringal and Celebration Island) gone: the Francos are left by the Elevator Platform's way", _ => scripts =>
            {
                foreach (var taxi in ActorsWith(scripts, "set_var_cube(2, 87)", 2))
                    scripts.SetText(taxi, ScriptKind.Life, AtStart(scripts.GetText(taxi, ScriptKind.Life), "comportement_0", "    suicide();\n"));
            });
        if (story.Contains(RaceTrackIsland.Desert))
        {
            var no = AddIslandTexts(gameDirectory, RaceTrackIsland.Desert.IslandByte, DesertFerryLines, log, "the Desert's ferry office");
            if (no >= 0)
                Edit(DesertFerryOffice, "the ferry office sells no ticket back to Citadel Island before chapter 4", _ => scripts =>
                {
                    var clerk = ActorWith(scripts, "found_object(13)");
                    scripts.SetText(clerk, ScriptKind.Life, Once(scripts.GetText(clerk, ScriptKind.Life), "if (0 == var_game(13))", $@"if (4 > chapter())
        {{
            message({no});
        }}
        else if (0 == var_game(13))"));
                });
            Edit(DesertHarbour, "the ferry to Citadel Island boards no one before chapter 4", _ => scripts =>
            {
                var hero = scripts.GetText(0, ScriptKind.Life);
                hero = Once(hero, "if (1 == use_inventory(13))", "if (1 == use_inventory(13) && 3 < chapter())");
                hero = Once(hero, "if (1 == var_game(13) && (1 == var_cube(8) || 0 == l_track_obj(13) || 1 == use_inventory(13)))",
                    "if (1 == var_game(13) && (1 == var_cube(8) || 0 == l_track_obj(13) || 1 == use_inventory(13)) && 3 < chapter())");
                scripts.SetText(0, ScriptKind.Life, hero);
            });
        }
    }

    // `text` put at the start of a script's comportement (`name`), before what it does
    private static string AtStart(string life, string name, string text)
    {
        var m = Regex.Match(life, $@"void {name}\(\)\s*\{{\r?\n");
        if (!m.Success) throw new NotTheGames($"no {name}");
        return life[..(m.Index + m.Length)] + text + life[(m.Index + m.Length)..];
    }

    // Whether a script of the scene switches door `zoneSwitch` on
    private static bool SetsChangeCube(SceneModel model, int scene, int zoneSwitch)
    {
        var on = new Regex($@"\bset_change_cube\({zoneSwitch},\s*1\)");
        return LifeTexts(model, scene).Any(t => on.IsMatch(t));
    }

    // (each actor's life script, decompiled; `each` hears which actor's comes next)
    private static IEnumerable<string> LifeTexts(SceneModel model, int scene, Action<int, string>? each = null)
    {
        SceneScripts scripts;
        try { scripts = SceneScripts.Load(SceneSerializer.Write(model), scene); }
        catch (Exception e) when (e is InvalidDataException or ArgumentException or IndexOutOfRangeException or InvalidOperationException or SceneFormatException) { yield break; }
        for (var a = 0; a < scripts.ActorCount; a++)
        {
            string text;
            try { text = scripts.GetText(a, ScriptKind.Life); }
            catch (Exception e) when (e is InvalidDataException or ArgumentException or IndexOutOfRangeException or InvalidOperationException) { continue; }
            each?.Invoke(a, text);
            yield return text;
        }
    }

    private static readonly Regex ChangeCubeCall = new(@"(?<!set_)\bchange_cube\((\d+)\)"), DoorOn = new(@"\bset_change_cube\((\d+),\s*1\)");

    // Every way out of a scene the build changed that goes nowhere -- a change_cube to a scene the folder hasn't got, a door switched on
    // (set_change_cube(n, 1)) that the scene hasn't got, a door into a scene that isn't there -- where the game's own scene hadn't it so
    // already. Lines for the log, each a WARNING; one line when all is well.
    private static List<string> CheckTravels(SceneStore store)
    {
        var log = new List<string>();
        var originals = RaceTrackService.OriginalScenes(store);
        if (ReferenceEquals(originals, store)) { log.Add("the story's travels not looked at: no original SCENE.HQR kept beside the folder's to tell the build's changes by"); return log; }
        var count = store.SceneCount;
        int looked = 0, bad = 0;
        // (the game's own scripts, as its scenes have them: a door one of them switches on that the build took off a road -- the race
        // track's road clearing's, RaceTrackScenes -- or that the story shut is said, not warned of; so are those of a scene the build made
        // as a copy of one of the game's, Island CX Mk2's)
        HashSet<string>? retail = null;
        HashSet<string> RetailLives()
        {
            if (retail is not null) return retail;
            retail = new HashSet<string>();
            for (var s = 0; s < originals.SceneCount; s++)
                if (originals.SceneExists(s))
                    foreach (var a in originals.Load(s).Actors) retail.Add(Convert.ToBase64String(a.Life));
            return retail;
        }
        for (var scene = 0; scene < count; scene++)
        {
            if (!store.SceneExists(scene)) continue;
            byte[] record;
            try { record = store.LoadRecord(scene); }
            catch (InvalidDataException e) { log.Add($"WARNING: the story's travels: scene {scene} can't be read ({e.Message})"); continue; }
            byte[]? had;
            try { had = originals.SceneExists(scene) ? originals.LoadRecord(scene) : null; }
            catch (InvalidDataException e) { log.Add($"WARNING: the story's travels: the original scene {scene} can't be read ({e.Message})"); had = null; }
            if (had is not null && had.AsSpan().SequenceEqual(record)) continue;
            looked++;
            var model = store.Load(scene);
            var now = Dangling(model, scene, store.SceneExists);
            // (a scene the build made: the game's scene it is a copy of, where it is one -- RaceTrackIsland.CopiesScene, CopiesScenes)
            var from = had is not null ? scene : CopiedFrom(scene);
            var old = from >= 0 && originals.SceneExists(from) ? originals.Load(from) : null;
            var before = old is null ? new HashSet<string>() : Dangling(old, from, originals.SceneExists).Select(d => d.Key).ToHashSet();
            var gameLines = old is null ? new HashSet<string>() : LifeTexts(old, from).SelectMany(t => ChangeCubeCall.Matches(t).Concat(DoorOn.Matches(t))).Select(m => m.Value).ToHashSet();
            foreach (var d in now.Where(d => !before.Contains(d.Key)))
            {
                if (d.Actor >= 0 && (gameLines.Contains(d.Line) || RetailLives().Contains(Convert.ToBase64String(model.Actors[d.Actor].Life))))
                {
                    log.Add($"the story's travels: scene {scene}: the game's own {d.What} (its door taken out by the build)");
                    continue;
                }
                log.Add($"WARNING: the story's travels: scene {scene}: {d.What}");
                bad++;
            }
        }
        log.Add(bad == 0 ? $"the story's travels: every change_cube and door of the {looked} scenes the build changed goes to a scene in the folder"
                         : $"WARNING: the story's travels: {bad} way{(bad == 1 ? "" : "s")} out of the {looked} scenes the build changed go{(bad == 1 ? "es" : "")} nowhere (above)");
        return log;
    }

    // The game's scene a scene the build made is a copy of (-1: none): the lava lake's 223 of 95, the old moon's of the Emerald Moon's,
    // Island CX Mk2's four of Island CX's outside
    private static int CopiedFrom(int scene)
    {
        foreach (var island in RaceTrackIsland.All)
        {
            if (island.CopiesScene is { } one && (scene == island.FirstScene || island.MoreScenes.Contains(scene))) return one;
            if (island.CopiesScenes is { } many && scene >= island.FirstScene && scene < island.FirstScene + many.Count) return many.First + scene - island.FirstScene;
        }
        return -1;
    }

    // A scene's ways out that go nowhere: what (for the log), the script's words (empty for a zone), the actor whose script it is (-1: a
    // zone) and what it is without the actor (the build may have taken actors out before it)
    private static List<(string What, string Line, int Actor, string Key)> Dangling(SceneModel model, int scene, Func<int, bool> exists)
    {
        var found = new List<(string What, string Line, int Actor, string Key)>();
        var doors = model.Zones.Where(z => z.Type == 0).ToList();
        var actor = 0;
        foreach (var text in LifeTexts(model, scene, (a, _) => actor = a))
        {
            foreach (Match m in ChangeCubeCall.Matches(text))
                if (int.Parse(m.Groups[1].Value) is var to && !exists(to)) found.Add(($"change_cube({to}) (actor {actor}): no scene {to}", m.Value, actor, m.Value));
            foreach (Match m in DoorOn.Matches(text))
                if (int.Parse(m.Groups[1].Value) is var n && !doors.Any(z => z.Info.Length > 4 && z.Info[4] == n)) found.Add(($"set_change_cube({n}, 1) (actor {actor}): the scene has no door {n}", m.Value, actor, m.Value));
        }
        foreach (var z in doors.Where(z => z.Num >= 0 && !exists(z.Num)))
        {
            var what = $"a door (switch {(z.Info.Length > 4 ? z.Info[4] : 0)}) into scene {z.Num}, which isn't there";
            found.Add((what, "", -1, what));
        }
        return found.DistinctBy(f => f.What).ToList();
    }
}
