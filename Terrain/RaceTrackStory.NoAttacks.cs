using System.IO;
using System.Text.RegularExpressions;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Nobody in the story attacks Twinsen (the user, 2026-11-27: "This isn't a world where people hate Twinsen, the only mystery is the black
// car" -- every actor's scripting that sets them on Twinsen taken out; where a kept person's entity has an armed and an unarmed body, the
// unarmed one). The audit of 2026-11-28 read every actor of the built story game that can hurt anyone (E:\dump\TEMP\nov28\audit: review_A
// to review_F, island by island) and settled on:
//   - the armed occupations with no part in the story gone, every chapter -- the Esmers' on the Desert, the Emerald Moon's spike guards,
//     the Wannies', the Island of the Francos' and Island CX's guards, troopers and dogs: RaceTrackStory.Retired.cs, SoldierEntities;
//   - the other attackers Twinsen can reach, one by one below: gone where they are nothing but an attacker (a guard, a sentry, a turret);
//     kept where they are someone (a townsman, a miner, a bather, the ride Mosquibee), with only the branch that went for him taken out --
//     whatever sends them at him (seen, near, bumped into, hit, a mate hit) never true -- and everything else they do and say left as it
//     was; the animals of the caves and beaches (rats, snakes, crabs, the spiders) left to their rounds, no bite, no strike;
//   - those who only strike back once hit (Mr. Paul, the museum's clerk, Bob, Otringal's policemen and sailors, the Wannie miners, ...)
//     kept, every line of theirs too, the strike back gone;
//   - turrets and traps that turn to Twinsen on foot (Island CX's rocket turrets, the patrol cars' guns) gone; a trap that only turned to
//     him (the Tralu cave's fire-spitters, the volcano's launchers) shoots straight ahead now, as the rest of the game's traps do;
//   - the fireflies' sting (entity 225: every animation carries a 1-point blow) and the pick-axe Wannie's swing (entity 233's digging,
//     anim 503: a 5-point blow to whoever stands in front of the pick) without their force, in the entity table (RESS.HQR 44);
//   - the doors whose little key a guard taken out used to drop (the keys go with each change of scene: the guard's key was the room's)
//     opening as Twinsen comes up against them, no key, as the Desert's base doors do (RaceTrackStory.Retired.cs).
// Kept as they are: the race's own hazards (RACEMOD: gunner=, guard=, gasmonster=, lavaball=, steam=, the frog -- a car in a race only),
// the Stella Entities bouncer (he carries Twinsen out, no blow), the lava lake's death (scene 223's actor 29), and the game's traps that
// fire along their own line (mines, wall shooters, falling rocks).
// Run with the island's retirements (RetireQuests: once the island's story is built, before its people are retold -- a script's texts
// still the game's own numbers), in the story's builds only. Each person is found where the built game of 2026-11-28 has them (scene,
// actor, entity -- and the script's own words where an index could move); each change is made once (an island's second track finds it
// made and leaves it). What isn't as expected is left out and logged.
internal static partial class RaceTrackStory
{
    // One person disarmed: in `scene`, the actor at `actor` (else the one actor) of entity `entity` (Any: whatever) whose life script matches
    // `who` (empty: any); `life` / `track` applied to its scripts, `body` its header body; `why` for the log
    private sealed record Disarm(int Scene, int Actor, int Entity, string Why, Func<string, string>? Life = null, Func<string, string>? Track = null,
        int? Body = null, string Who = "");
    // Every actor of `entities` in `scenes` gone, every chapter
    private sealed record Crowd(int[] Scenes, int[] Entities, string Why);
    private const int Any = int.MinValue, Sprite = -1;
    // the moon base's light gray Franco guards (entity 57): body 0 their gun (BODY.HQR 97), body 94 a tool, a wrench, in the same hand (98 --
    // the base's own scientists wear it; every animation of the entity fits it, drawn 2026-11-28). The story's copies of its mechanic (the
    // hangar's dock officer, Stella Entities' barman) wear it too.
    internal const int MechanicWithTool = 94;

    // a condition never true, and one always true
    private const string Off = "0 > chapter()", On = "0 <= chapter()";

    internal static List<string> NoAttacks(string gameDirectory, RaceTrackIsland island)
    {
        var log = new List<string>();
        var (people, crowds, doors) = island.IslandByte switch
        {
            0 => (CitadelDisarmed(), new List<Crowd>(), new List<ClosedDoor>()),
            2 => (DesertDisarmed(), new List<Crowd>(), new List<ClosedDoor>()),
            3 => (MoonDisarmed(), new List<Crowd>(), new List<ClosedDoor>()),
            4 => (OtringalDisarmed(), new List<Crowd> { PalaceGuards }, new List<ClosedDoor>()),
            // (the statue's inside -- FunFrock's base, his grobos, missiles and sword -- through the lava lake's copy of the dock's door
            // too: CelebrationRetired shuts the dock's own, scene 95's; the copy's hangs 6600 up in the air, but shut all the same)
            5 => (new List<Disarm>(), new List<Crowd>(), new List<ClosedDoor> { new(223, 185, "the lava lake's copy of the door up into the statue (scene 185: FunFrock's base) taken out") }),
            6 => (WanniesDisarmed(), new List<Crowd>(), new List<ClosedDoor>()),
            7 => (MosquibeesDisarmed(), new List<Crowd>(), new List<ClosedDoor>()),
            8 => (FrancosDisarmed(), new List<Crowd>(), new List<ClosedDoor>()),
            9 => (CxDisarmed(), new List<Crowd>(), new List<ClosedDoor>()),
            11 => (VolcanoDisarmed(), new List<Crowd>(), new List<ClosedDoor>()),
            _ => (new List<Disarm>(), new List<Crowd>(), new List<ClosedDoor>()),
        };
        if (island.IslandByte == 6) log.AddRange(StingsOff(gameDirectory));
        if (people.Count == 0 && crowds.Count == 0 && doors.Count == 0) return log;

        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var changes = new List<SceneChange>();
        for (var scene = 0; scene < store.SceneCount; scene++)
        {
            var mine = people.Where(p => p.Scene == scene).ToList();
            var gangs = crowds.Where(c => c.Scenes.Contains(scene)).ToList();
            var shut = doors.Where(d => d.Scene == scene).ToList();
            if (mine.Count == 0 && gangs.Count == 0 && shut.Count == 0) continue;
            SceneModel model;
            try
            {
                if (!store.SceneExists(scene)) { log.Add($"scene {scene}: no such scene, {mine.Count + gangs.Count + shut.Count} disarming left out"); continue; }
                model = store.Load(scene);
            }
            catch (Exception e) when (e is InvalidDataException or IOException) { log.Add($"scene {scene}: unreadable, its disarming left out: {e.Message}"); continue; }
            if (model.Island != island.IslandByte)
            {
                if (mine.Count + shut.Count > 0) log.Add($"scene {scene}: not {island.Name}'s (island {model.Island}), its disarming left out");
                continue;
            }
            var said = new List<string>();
            var edited = false;
            foreach (var d in shut)
            {
                var gone = model.Zones.RemoveAll(z => z.Type == 0 && z.Num == d.Into);
                if (gone > 0) { edited = true; said.Add($"{d.Why} ({gone} zone{(gone > 1 ? "s" : "")})"); }
            }
            // (the header bodies first: the scripts are read from the model as it is then)
            var found = new List<(Disarm P, int Actor, bool Bodied)>();
            var scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
            foreach (var p in mine)
            {
                var actor = Whose(model, scripts, p);
                if (actor < 0) { log.Add($"scene {scene}: {p.Why} left out: no actor {p.Actor} of entity {p.Entity}{(p.Who.Length > 0 ? $" doing /{p.Who}/" : "")}, nor one other"); continue; }
                var bodied = p.Body is { } body && model.Actors[actor].Body != body;
                if (bodied) { model.Actors[actor].Body = p.Body!.Value; edited = true; }
                found.Add((p, actor, bodied));
            }
            if (edited) scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
            var scripted = false;
            foreach (var (p, actor, bodied) in found)
            {
                try
                {
                    var changed = false;
                    foreach (var (kind, change) in new[] { (ScriptKind.Life, p.Life), (ScriptKind.Track, p.Track) })
                    {
                        if (change is null) continue;
                        var text = scripts.GetText(actor, kind);
                        var now = change(text);
                        if (now == text) continue;
                        scripts.SetText(actor, kind, now);
                        changed = true;
                    }
                    if (changed || bodied) said.Add($"actor {actor}: {p.Why}{(actor != p.Actor ? $" (actor {actor}, not {p.Actor})" : "")}");
                    else log.Add($"scene {scene} actor {actor}: {p.Why} -- there already");
                    scripted |= changed;
                }
                catch (NotTheGames e) { log.Add($"scene {scene} actor {actor}: {p.Why} left out: the script isn't the game's own: {e.Message}"); }
            }
            foreach (var c in gangs)
                for (var a = 1; a < model.Actors.Count; a++)
                {
                    if (!c.Entities.Contains(model.Actors[a].Entity)) continue;
                    try
                    {
                        var life = scripts.GetText(a, ScriptKind.Life);
                        if (!life.Contains("comportement_0", StringComparison.Ordinal)) continue;   // (no script: taken off by the race build)
                        // (gone already, read back in the decompiler's IF / label form -- a script it can't put in blocks)
                        if (Regex.IsMatch(life, @"void comportement_0\(\)\s*\{\s*IF\(127 > chapter\(\), L\d+\);\s*suicide\(\);")) continue;
                        var now = Gone("127 > chapter()")(life);
                        if (now == life) continue;
                        scripts.SetText(a, ScriptKind.Life, now);
                        scripted = true;
                        said.Add($"actor {a}: {c.Why} (entity {model.Actors[a].Entity})");
                    }
                    catch (NotTheGames e) { log.Add($"scene {scene} actor {a}: {c.Why} left out: {e.Message}"); }
                }
            if (!edited && !scripted) continue;
            var built = scripts.Build();
            if (!built.Ok) { log.Add($"scene {scene}: the disarming would not compile, left as it was: {string.Join("; ", built.Errors.Take(3))}"); continue; }
            changes.Add(new SceneChange(scene, SceneSerializer.Parse(SceneGame.Lba2, built.Record!), null));
            foreach (var s in said) log.Add($"scene {scene}: {s}");
        }
        store.SaveMany(changes, allowErrors: true);
        log.Add($"{island.Name}: nobody attacks Twinsen -- disarmed in {changes.Count} scene{(changes.Count == 1 ? "" : "s")}");
        return log;
    }

    // the actor a disarming is for: at its place if it is of that entity and does what it says, else the one actor that is and does (-1:
    // none, or more than one)
    private static int Whose(SceneModel model, SceneScripts scripts, Disarm p)
    {
        var rx = p.Who.Length > 0 ? new Regex(p.Who, RegexOptions.Singleline) : null;
        bool Fits(int a)
        {
            if (a < 0 || a >= model.Actors.Count || a >= scripts.ActorCount) return false;
            if (p.Entity != Any && (a == 0 ? 0 : model.Actors[a].Entity) != p.Entity) return false;
            if (rx is null) return true;
            try { return rx.IsMatch(scripts.GetText(a, ScriptKind.Life)); } catch (InvalidOperationException) { return false; }
        }
        if (Fits(p.Actor)) return p.Actor;
        if (p.Actor == 0) return -1;
        var all = Enumerable.Range(1, model.Actors.Count - 1).Where(Fits).ToList();
        return all.Count == 1 ? all[0] : -1;
    }

    // ---- the changes ---------------------------------------------------------------------------------------------------------------------
    // `text` as a pattern (LoosePattern's), not inside a longer word: "if (" isn't the end of "swif (" or "oneif ("
    private static string WordPattern(string text) => (char.IsLetter(text[0]) ? @"(?<![\w])" : "") + LoosePattern(text);

    // `what` (the game's) made `with`, the first or every one; left alone if `what` isn't there but `with` is -- or one of `made`, `with`
    // as the scripts read back (made already)
    private static Func<string, string> Change(string what, string with, bool all = false, params string[] made) => text =>
    {
        var rx = new Regex(WordPattern(what));
        if (rx.IsMatch(text)) return all ? rx.Replace(text, with.Replace("$", "$$")) : rx.Replace(text, with.Replace("$", "$$"), 1);
        if (made.Prepend(with).Any(m => Regex.IsMatch(text, WordPattern(m)))) return text;
        throw new NotTheGames($"no \"{what.Split('\n')[0].Trim()}\"");
    };

    // `head (cond)` never taken: `head (0 > chapter() && (cond))` -- the game's condition kept to read (read back without the brackets
    // round a lone condition)
    private static Func<string, string> Never(string head, string cond) => Change($"{head} ({cond})", $"{head} ({Off} && ({cond}))", false, $"{head} ({Off} && {cond})");

    // `head (cond)` always taken: `head (0 <= chapter() || (cond))`
    private static Func<string, string> Always(string head, string cond) => Change($"{head} ({cond})", $"{head} ({On} || ({cond}))", false, $"{head} ({On} || {cond})");

    // no strike back: "hit by Twinsen" -- himself (0 == hit_by()) or one of his mates (0 == hit_obj_by(n)) -- never true
    private static string Unprovoked(string life) => Regex.Replace(life, @"(?<![\w])0\s*==\s*hit_(?:by\(\)|obj_by\(\d+\))", Off);

    // no blow straight from the script: every hit_obj(0, n) taken out
    private static string NoHits(string life) => Regex.Replace(life, @"[ \t]*(?<![\w])hit_obj\(0,\s*\d+\);[ \t]*\r?\n?", "");

    // a door that took a little key opens without one, as he comes up against it
    private static string NoKey(string life) => Regex.Replace(Regex.Replace(life, @"0\s*<\s*nb_little_keys\(\)\s*&&\s*", ""), @"[ \t]*(?<![\w])use_one_little_key\(\);[ \t]*\r?\n?", "");

    // every `what` made `with`, wherever it is (nothing if it isn't: made already) -- an armed body's switch in a track
    private static Func<string, string> Wherever(string what, string with) => text => Regex.Replace(text, WordPattern(what), with.Replace("$", "$$"));

    // the many creatures of the caves and beaches: a snake that went for Twinsen within 1200 keeps to its round
    private static readonly Func<string, string> Snake = All(Always("if", "1200 < distance(0)"), NoHits);

    // ---- each island's ------------------------------------------------------------------------------------------------------------------
    private static List<Disarm> CitadelDisarmed()
    {
        // the rats' and crabs' bites, the snakes' strikes: Tralu's caves, the Spider Cave, the beaches (chapters 1-3)
        var crab43 = All(Change("swif (500 > distance(0))\n{\nset_track(label_20);\n}", "swif (500 > distance(0))\n{\nanim(1);\n}"), NoHits);
        var crab = All(Change("swif (0 == col())\n{\nset_track(label_20);\n}", "swif (0 == col())\n{\nset_track(label_15);\n}"), NoHits);
        // the Sups in light green: no gun drawn (their entity's body 46), whatever comes
        var unarmed = Wherever("body(46);", "body(0);");
        return new List<Disarm>
        {
            new(2, 17, 23, "the Tralu cave's flying rat bites no more (it keeps its loop)", NoHits),
            new(2, 18, 23, "the Tralu cave's second flying rat bites no more", NoHits),
            new(51, 7, 23, "the Spider Cave's flying rat bites no more", NoHits),
            new(51, 2, 21, "the Spider Cave's snake keeps to its round, no strike", Snake),
            new(51, 3, 21, "the Spider Cave's second snake keeps to its round, no strike", Snake),
            new(51, 9, 148, "the Spider Cave's giant spider stays at its post (no chase on sight, no bite)", Never("if", "0 == hit_by() || 5000 > cone_view(0)")),
            new(47, 8, 21, "the snake by Sendell's sign keeps to its round, no strike", Snake),
            new(47, 9, 21, "the second snake by Sendell's sign keeps to its round", Snake),
            new(47, 10, 21, "the third snake by Sendell's sign keeps to its round", Snake),
            new(43, 6, 107, "the harbour's crab (Felix's catch) scuttles after Twinsen, no pinch", crab43),
            new(43, 7, 107, "the harbour's second crab, no pinch", crab43),
            new(43, 8, 107, "the harbour's third crab, no pinch", crab43),
            new(43, 9, 107, "the harbour's fourth crab, no pinch", crab43),
            new(46, 7, 107, "the lighthouse beach's crab, no pinch", crab),
            new(49, 5, 107, "the crab by the Dino-Fly, no pinch", crab),
            new(49, 10, 107, "the second crab by the Dino-Fly, no pinch", crab),
            // Mr. Paul: hit, he takes offence as the game has him (var_cube 0), and no blow back
            new(7, 2, 38, "Mr. Paul doesn't strike back when hit (his offence and his words kept)", Drop("set_comportement(comportement_3);\nset_track(label_50);")),
            new(15, 2, 59, "the museum's clerk doesn't strike back when hit (his ticket talk kept)", Unprovoked),
            new(42, 6, 30, "the Sup near the Tavern doesn't draw his gun when he or his dogs are hit", Unprovoked, unarmed),
            new(43, 16, 30, "the Sup with the newspaper near the harbour doesn't draw his gun when hit", Unprovoked, unarmed),
            new(48, 20, 30, "the Sup downstairs the sewer grid doesn't draw his gun when hit", Unprovoked, unarmed),
            new(45, 6, 30, "the Sup by the Weather Wizard's tent doesn't go for Twinsen as he comes near, nor when hit",
                All(Never("if", "1 == var_game(109)"), Never("else if", "0 == hit_by() || 4000 > distance(0)")), unarmed),
            new(47, 14, 30, "the Sup by Sendell's sign doesn't go for Twinsen as he comes near, nor when hit",
                Never("if", "0 == hit_by() || (0 == var_game(109) && 4000 > distance(0))"), unarmed),
            new(49, 12, 30, "the Sup by the Dino-Fly doesn't go for Twinsen as he comes near, nor when hit",
                All(Never("if", "1 == var_game(109)"), Never("else if", "0 == hit_by() || (5 != zone_obj(0) && 4500 > distance(0))")), unarmed),
            new(47, 15, 30, "the armed Sup by Sendell's sign (the game's, var 83 6) gone", Gone("127 > chapter()")),
            // Bob: his punch's track label (20) passed over to the stop after it (120), so his flow -- the storm's, scene 37's game -- goes on
            new(48, 3, 102, "Bob doesn't punch (bumped into or hit): straight to the stop after the punch", Change("set_track(label_20);", "set_track(label_120);", all: true)),
            new(48, 9, 121, "the Quetch in black doesn't punch: straight to the stop after it", Change("set_track(label_10);", "set_track(label_110);", all: true)),
            new(48, 5, 120, "the Quetch child on the pharmacy's roof throws nothing at Twinsen (nor lands on him)", All(Never("else if", "1 == zone_obj(0)"), NoHits)),
            // the 1996 demo's sewers and halls (the thief's chase runs through them)
            new(17, 14, 350, "the demo sewers' creature follows Twinsen, no strike", All(Never("if", "1 == l_track() || 100 == l_track()"), NoHits)),
            new(17, 0, 0, "the demo sewers' gaping creature (actor 15) doesn't bite: Twinsen's own script took the bite", Drop("set_track(label_4);\nhit_obj(0, 2);"), Who: @"450 > distance\(15\)"),
            new(18, 2, 350, "the demo halls' creature keeps to its round, no strike", All(Never("if", "1000 > distance(0)"), NoHits)),
            new(18, 4, 354, "the demo halls' thrower throws nothing as Twinsen comes into its zone", Change("if (10 == zone_obj(0))\n{\nset_track(label_0);", "if (10 == zone_obj(0))\n{\nset_track(label_1);")),
            new(18, 5, 355, "the demo halls' biter keeps to its round, no chase", Never("if", "2 == zone_obj(0)")),
            new(18, 6, 356, "the demo halls' wanderer gone (its very walk carries a blow)", Gone("127 > chapter()")),
            new(20, 4, 359, "the demo's third hall's biter bites no more", NoHits),
            new(20, 5, 358, "the demo's third hall's snake keeps to its round, no strike", All(Never("if", "1000 > distance(0)"), NoHits)),
            new(20, 6, 357, "the demo's third hall's fire-spitter spits straight ahead, not turned to Twinsen", Change("set_dir(MOVE_FOLLOW, 0);", "set_dir(NO_MOVE);")),
            new(20, 7, 357, "the demo's third hall's second fire-spitter spits straight ahead", Change("set_dir(MOVE_FOLLOW, 0);", "set_dir(NO_MOVE);")),
        };
    }

    private static List<Disarm> DesertDisarmed() => new()
    {
        new(11, 10, 21, "the Temple of Bú's snake keeps to its round, no strike", Snake),
        new(11, 11, 21, "the Temple of Bú's second snake keeps to its round", Snake),
        new(11, 12, 21, "the Temple of Bú's third snake keeps to its round", Snake),
        new(11, 13, 21, "the Temple of Bú's fourth snake keeps to its round", Snake),
        new(11, 14, 21, "the Temple of Bú's fifth snake keeps to its round", Snake),
        new(27, 12, 349, "the School of Magic's student bumps into Twinsen, no blow", NoHits),
        new(27, 13, 349, "the School of Magic's second student, no blow", NoHits),
        new(27, 14, 349, "the School of Magic's third student, no blow", NoHits),
        new(27, 15, 349, "the School of Magic's fourth student, no blow", NoHits),
        // (her slap, track label 2, her own idle: her line and the rest as the game has them)
        new(25, 3, 74, "the women's bath's bather doesn't slap Twinsen (her line kept)", Track: Change("label(2);\nanim(198);", "label(2);\nanim(281);")),
        new(29, 7, 72, "the Hacienda's nomad grobo doesn't fight back when hit (his line kept)", All(Unprovoked, Never("else if", "2 == var_game(41)"))),
        new(32, 3, 49, "the secret passage's guard keeps his patrol, no chase", Never("if", "0 == hit_by() || (3 == zone_obj(0) && 3000 > cone_view(0))")),
        // (the park's policeman: his warning and his walk up to Twinsen kept, the blow -- anim 162, track labels 40 and 60 -- not)
        new(67, 3, 172, "the Temple Park's policeman warns Twinsen off the lawn, no blow", Track: Change("anim(162);", "anim(0);", all: true)),
        new(61, 16, 23, "the School of Magic's flying rat bites no more", NoHits),
        new(61, 17, 23, "the School of Magic's second flying rat bites no more", NoHits),
        new(72, 6, 179, "the old rabbibunny's chest doesn't lash out when touched (it opens when hit, as the game has it)", Never("else if", "6 == col_obj(0) || 0 == col()")),
        new(72, 7, 21, "the chest's snake slithers off, no strike", Never("if", "1200 > distance(0)")),
    };

    private static List<Disarm> MoonDisarmed() => new()
    {
        // (the base's spike guards, entity 49: SoldierEntities; the guards with guns, entity 57, here -- its own scientists wear its body 94,
        // the "tool": the one in HAL's room too)
        new(13, 15, 57, "the corridor's guard with a gun gone", Gone($"{MoonChapter - 1} < chapter()")),
        new(23, 19, 57, "the base's guard with a gun gone", Gone($"{MoonChapter - 1} < chapter()")),
        new(23, 20, 57, "the base's second guard with a gun gone", Gone($"{MoonChapter - 1} < chapter()")),
        new(23, 13, 57, "the guard in HAL's room with a tool, not a gun (entity 57's body 94)", Body: MechanicWithTool),
    };

    private static List<Disarm> OtringalDisarmed()
    {
        var gone = Gone($"{OtringalChapter - 1} < chapter()");
        // the Franco policemen who only strike back: no stick drawn (their entity's body 86)
        var noStick = Wherever("body(86);", "body(0);");
        return new List<Disarm>
        {
            // the palace gate's guards: no charge as Twinsen comes near the gate (the hero's script set them on him), sword sheathed (192's body 0)
            new(88, 0, 0, "Twinsen near the palace gate doesn't set its guards on him", Never("if", "1 == var_cube(3) || 3000 > distance(2) || 3000 > distance(3)"), Who: @"3000 > distance\(2\)"),
            new(88, 2, 192, "the palace gate's guard keeps his patrol, his sword sheathed", Never("if", "1 == zone_obj(0)"), Body: 0),
            new(88, 3, 192, "the palace gate's second guard keeps his patrol, his sword sheathed", Never("if", "1 == zone_obj(0)"), Body: 0),
            new(88, 6, 199, "the policeman on the patrol car doesn't fire at Twinsen", Never("if", "3000 > distance(0) || (3 != comportement_hero() && 6000 > distance(0))")),
            new(88, 11, 51, "the Upper City's policeman with a gun (the Fragment's hunt) gone", gone),
            new(89, 17, 51, "the Lower City's policeman with a gun (the Fragment's hunt) gone", gone),
            new(90, 7, 105, "the prison exit's robot guard gone", gone),
            new(90, 5, 49, "the prison exit's spike guard gone from chapter 5", Change("if (6 <= chapter())", "if (4 < chapter())")),
            new(90, 6, 49, "the prison exit's second spike guard gone from chapter 5", Change("if (6 <= chapter())", "if (4 < chapter())")),
            new(90, 4, 32, "the prison exit's dog doesn't chase Twinsen, bumped into or hit", Change("0 == hit_by() || 0 == col()", Off, all: true)),
            new(90, 8, 32, "the prison exit's second dog doesn't chase Twinsen", Change("0 == hit_by() || 0 == col()", Off, all: true)),
            new(90, 22, 32, "the prison exit's hidden dog (woken by Action) gone", gone),
            new(90, 2, 209, "the prison exit's patrol car gone (it runs into him; its gun goes with it)", gone),
            new(90, 3, 210, "the prison exit's patrol car's gun gone (it turned to Twinsen and fired)", gone),
            new(82, 2, 49, "the elevators' spike guard gone from chapter 5", Change("if (6 <= chapter())", "if (4 < chapter())")),
            new(82, 3, 49, "the elevators' second spike guard gone from chapter 5", Change("if (6 <= chapter())", "if (4 < chapter())")),
            new(82, 4, 49, "the elevators' third spike guard gone from chapter 5", Change("if (6 <= chapter())", "if (4 < chapter())")),
            new(82, 5, 49, "the elevators' fourth spike guard gone from chapter 5", Change("if (6 <= chapter())", "if (4 < chapter())")),
            new(83, 6, 188, "the prison entrance's policeman (no gate of his own) gone", gone),
            new(84, 2, 188, "the prison's policeman gone", gone),
            new(84, 13, 188, "the prison's patrolling policeman gone", gone),
            new(84, 14, 188, "the prison's policeman in zone 23 gone", gone),
            new(84, 21, 188, "the prison's policeman in zone 24 gone", gone),
            new(85, 3, 49, "the control tower entrance's spike guard gone", gone),
            new(85, 9, 49, "the control tower entrance's second spike guard gone", gone),
            new(85, 2, 49, "the control tower entrance's talking guard doesn't fight when he or his mates are hit (his words kept)", Unprovoked),
            new(85, 11, 57, "the control tower entrance's mechanic doesn't fire when hit (his words kept), a tool not a gun", Unprovoked, Body: MechanicWithTool),
            new(87, 9, 188, "the harbour's policeman doesn't strike back", Unprovoked, noStick),
            new(89, 10, 188, "the Lower City's policeman doesn't strike back", Unprovoked, noStick),
            new(139, 9, 188, "the lowest elevators' policeman doesn't strike back", Unprovoked, noStick),
            new(150, 5, 188, "the Upper City elevator's policeman doesn't strike back", Unprovoked, noStick),
            new(150, 6, 188, "the Upper City elevator's second policeman doesn't strike back", Unprovoked, noStick),
            // (the jackpot room: Twinsen's Action at the jackpot set its two glooms on him -- a fight he couldn't lose, his life held at
            // 10, for the game's 10000 kashes; no fight, no jackpot)
            new(133, 3, 206, "the jackpot room's gray gloom doesn't fight back", Unprovoked),
            new(133, 0, 0, "the jackpot room's jackpot doesn't set its two gray glooms on Twinsen", Never("if", "0 == var_cube(5)"), Who: @"set_comportement_obj\(6, comportement_1\)"),
            new(136, 2, 294, "Rick's bar's green gloom doesn't fight back, nor shove", All(Unprovoked, NoHits)),
            new(136, 17, 295, "Rick's bar's sailor doesn't draw his gun when hit", Unprovoked, Wherever("body(185);", "body(0);")),
            new(136, 19, 297, "Rick's bar's sailor doesn't fight back (nor the punch his mates hold Twinsen for)", Unprovoked),
            new(136, 20, 297, "Rick's bar's second sailor doesn't fight back", Unprovoked),
            new(136, 21, 297, "Rick's bar's third sailor doesn't fight back", Unprovoked),
            // (Rick's office: the hidden sailor -- the others watch his life points, so he stays -- doesn't spring out of the door at Twinsen)
            new(137, 5, 297, "Rick's office's hidden sailor doesn't spring his ambush (nor the door open for it)",
                All(Never("swif", "21 == zone_obj(0)"), Never("else swif", "1 == l_track()"), Never("if", "1 == l_track_obj(2)"))),
            new(78, 8, 49, "the palace departure room's alarm guard gone", gone),
            new(170, 2, 30, "the hotel's second room's Sup with the newspaper (and his gun) gone", gone),
            new(170, 3, 30, "the hotel's second room's other Sup gone", gone),
        };
    }

    // the palace's insides (only through the gate's little key or the race's lap): its soldiers, dogs, policemen, pigheads, Stan and the
    // last room's monster
    private static readonly Crowd PalaceGuards = new(new[] { 80 }.Concat(Enumerable.Range(151, 16)).ToArray(), new[] { 32, 51, 183, 184, 313, 314, 315, 316 },
        "the palace's guard gone");

    private static List<Disarm> WanniesDisarmed() => new()
    {
        // (the guards, policemen, troopers and dogs: SoldierEntities)
        new(98, 2, 209, "the deck's patrol car gone (it runs into him; its gun goes with it)", Gone("8 < chapter()")),
        new(98, 3, 210, "the deck's patrol car's gun gone (it turned to Twinsen and fired)", Gone("8 < chapter()")),
        // the elevator foot's guard behind the counter: the levers' man, no shot -- not when hit, not in his zone, not as the room starts (his
        // track's first label faced Twinsen and fired); a tool, not a gun
        new(123, 6, 57, "the elevator foot's guard works the levers, fires at no one (a tool, not a gun)",
            All(Unprovoked, Never("if", "2 == zone_obj(0)")), Change("anim(173);", "anim(0);", all: true), Body: MechanicWithTool),
        new(112, 4, 265, "the Wannie guarding the temple door stands his ground, no blow (it began his track)", Track: Change("anim(142);", "anim(0);", all: true)),
        new(100, 2, 223, "the mine's Wannie miner doesn't fight back (his words kept)", All(Never("if", "1 == var_game(55) || 1 == var_game(34)"), Unprovoked)),
        new(100, 3, 223, "the mine's second miner doesn't fight back", All(Never("if", "1 == var_game(55) || 1 == var_game(34)"), Unprovoked)),
        new(100, 4, 223, "the mine's third miner doesn't fight back", All(Never("if", "1 == var_game(55) || 1 == var_game(34)"), Unprovoked)),
        new(100, 8, 223, "the mine's fourth miner doesn't fight back", All(Never("if", "1 == var_game(55) || 1 == var_game(34)"), Unprovoked)),
        new(100, 7, 233, "the mine's Wannie with the pick-axe doesn't fight back (his words kept)", All(Never("if", "1 == var_game(55) || 1 == var_game(34)"), Unprovoked)),
        new(111, 2, 223, "the mine's third room's miner doesn't fight back", All(Never("if", "1 == var_game(55) || 1 == var_game(34)"), Unprovoked)),
        new(111, 3, 233, "the mine's third room's Wannie with the pick-axe doesn't fight back", Never("if", "3000 > distance(0) && (0 == hit_by() || 1 == var_game(55) || 1 == var_game(34))")),
        new(124, 2, 223, "the box transport building's miner doesn't fight back", Unprovoked),
    };

    private static List<Disarm> MosquibeesDisarmed() => new()
    {
        new(103, 3, 243, "the mountain top's gas monster gone", Gone("9 < chapter()")),
        new(105, 6, 132, "the ride Mosquibee doesn't sting when hit: his own idle", Change("anim(376);", "anim(289);")),
        new(149, 7, 312, "the passage's small spider stops when bumped into, no bite", Change("set_track(label_20);", "set_track(label_21);")),
    };

    private static List<Disarm> FrancosDisarmed() => new()
    {
        // (the refinery's guards, policemen and dogs: SoldierEntities -- with them, the 3rd and 4th rooms' little keys)
        new(142, 8, Sprite, "the refinery's 3rd room's door opens without a little key (its guard and his key gone)", NoKey, Who: @"set_track_obj\(9, label_10\)"),
        new(142, 10, Sprite, "the refinery's 3rd room's second door opens without a little key", NoKey, Who: @"set_track_obj\(11, label_10\)"),
        new(143, 7, Sprite, "the refinery's 4th room's door opens without a little key (its guards and their keys gone)", NoKey, Who: @"6 == col_obj\(0\) \|\| 7 == col_obj\(0\)"),
        new(143, 9, Sprite, "the refinery's 4th room's second door opens without a little key", NoKey, Who: @"9 == col_obj\(0\) \|\| 8 == col_obj\(0\)"),
        new(143, 15, Sprite, "the refinery's 4th room's third door opens without a little key", NoKey, Who: @"15 == col_obj\(0\) \|\| 16 == col_obj\(0\)"),
        new(143, 19, Sprite, "the refinery's 4th room's fourth door opens without a little key", NoKey, Who: @"20 == col_obj\(0\) \|\| 19 == col_obj\(0\)"),
    };

    private static List<Disarm> CxDisarmed() => new()
    {
        // (the troopers and sword soldiers: SoldierEntities; trooper 11 shut the control tower's door, 3, as the scene began -- gone first,
        // the door stays open. The rocket turrets at the island's four corners: entity 264, as the Mk2 race's own gunners are, so here
        // by scene only)
        new(110, 2, 264, "a rocket turret at the island's corner gone (it turned to Twinsen and fired)", Gone("11 < chapter()")),
        new(110, 3, 264, "the second rocket turret gone", Gone("11 < chapter()")),
        new(110, 4, 264, "the third rocket turret gone", Gone("11 < chapter()")),
        new(110, 5, 264, "the fourth rocket turret gone", Gone("11 < chapter()")),
        new(178, 4, Sprite, "the control tower's door opens without a little key (the sword soldier and his key gone)", NoKey, Who: @"4 == col_obj\(0\)"),
    };

    private static List<Disarm> VolcanoDisarmed() => new()
    {
        new(132, 8, 16, "the lava's edge's launcher throws straight ahead, not turned to Twinsen", Change("set_dir(MOVE_FOLLOW, 0);", "set_dir(NO_MOVE);")),
        new(132, 9, 16, "the lava's edge's second launcher throws straight ahead", Change("set_dir(MOVE_FOLLOW, 0);", "set_dir(NO_MOVE);")),
    };

    // ---- the entity table -------------------------------------------------------------------------------------------------------------------
    // (an animation's action and the bytes that follow its code: FICHE.CPP GereAnimAction)
    private static readonly Dictionary<int, int> ActionSizes = new()
    {
        [5] = 2, [6] = 3, [7] = 5, [8] = 12, [9] = 8, [10] = 10, [11] = 8, [12] = 12, [13] = 3, [14] = 13, [15] = 1, [16] = 1, [17] = 1, [18] = 16,
        [19] = 16, [20] = 12, [21] = 12, [22] = 10, [23] = 17, [26] = 8, [27] = 8, [28] = 2, [29] = 1, [30] = 8, [31] = 1, [32] = 2, [33] = 1, [34] = 1,
        [35] = 0, [36] = 4, [37] = 1, [38] = 1, [39] = 8, [40] = 9, [41] = 9, [42] = 1,
    };
    private const int ActionHit = 5;
    // the blows no one aims: the fireflies' sting (every animation of entity 225) and the pick-axe Wannie's digging swing (entity 233's 503)
    private static readonly (int Entity, int[]? Anims, string What)[] Stings = { (225, null, "the fireflies' sting"), (233, new[] { 503 }, "the pick-axe Wannie's digging swing") };

    private static List<string> StingsOff(string gameDirectory)
    {
        var log = new List<string>();
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        byte[] table;
        try { table = HqrArchive.Open(ressPath).Read(44); }
        catch (Exception e) when (e is IOException or InvalidDataException or ArgumentException) { log.Add($"the stings left out: no entity table ({e.Message})"); return log; }
        var changed = false;
        foreach (var (entity, anims, what) in Stings)
        {
            var zeroed = new List<int>();
            foreach (var (at, command, generic, size) in Lba2EntityTable.Records(table, entity).ToList())
            {
                if (command != 3 || anims is not null && !anims.Contains(generic) || size < 7) continue;
                // the record: 3, generic (2), size, ANIM.HQR (2), the number of actions, the actions -- walked to its end, else left
                var hits = new List<int>();
                int p = at + 7, end = at + size, count = table[at + 6];
                for (var i = 0; i < count && p < end; i++)
                {
                    if (!ActionSizes.TryGetValue(table[p], out var bytes)) { p = -1; break; }
                    if (table[p] == ActionHit) hits.Add(p + 2);
                    p += 1 + bytes;
                }
                if (p != end) { log.Add($"{what}: entity {entity}'s animation {generic} left as it is (its actions don't end where its record does)"); continue; }
                var any = false;
                foreach (var force in hits.Where(f => table[f] != 0)) { table[force] = 0; any = true; }
                if (any) zeroed.Add(generic);
            }
            if (zeroed.Count > 0) { changed = true; log.Add($"{what} without its force: entity {entity}'s animation{(zeroed.Count > 1 ? "s" : "")} {string.Join(", ", zeroed)}"); }
            else log.Add($"{what}: no force left in entity {entity}'s animations (made already)");
        }
        if (changed) HqrWriter.ReplaceInFile(ressPath, 44, table);
        return log;
    }
}
