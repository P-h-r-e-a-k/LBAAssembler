using System.IO;
using System.Text.RegularExpressions;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The original game's quests kept out of the story's reach (docs/LBRA_STORY_PLAN.md: "The original game's quests are ignored", and the
// "Travel" note -- nobody walks or sails through LBA2's open world between chapters). The review sweep of 2026-11-05 (the fourteen chapters
// built, every person's lines read in order) found the game's own quests still live in the story's chapters: people who give the game's
// objects, set its plot variables or change cube, reachable from where the story puts Twinsen. Each is retired here, island by island, in
// the story's builds only (RaceTrackService.Story: after the island's own story, before its people are retold, so a script's texts are
// still the game's own numbers):
//   - Citadel Island (chapters 1, 2): the pharmacy's umbrella snatch stays (the story's thief is that man, and his chase starts there), but
//     the game's second half of it -- the umbrella man on the harbour (scene 48's actor 6) handing the umbrella (item 20) to his customer
//     (actor 7), who sends Twinsen to the Desert's healing wizard (the holomap's arrow, var 62) -- is gone: the story's umbrella goes back
//     to Tim. The ferry office (scene 8) sells no Desert ticket before the town circuit's prize is one (no ten kashes taken in the storm
//     for a crossing that can't sail); asked in the storm, the clerk's own "No ferries in this storm". The Esmer of the game's landing
//     (scene 42) isn't on the quay the night before they land.
//   - The White Leaf Desert (chapter 3): the Wizard's Diploma shown to a peddler (Twinsen's use_inventory(30) in the market's scenes)
//     buys nothing: the game's tunic (item 4, var 109) is what invites Twinsen to the Hacienda and the game's kidnapping to Otringal
//     (scene 29), gated off too. Baldino's radio in the plaza (scene 60's actor 26) and the game's Zoe off shopping (scene 65's actor 6),
//     both waiting on var 74 (2, then 3 -- Citadel's yard sets 2 only for the gloves used on the game's Zoe, a use RaceTrackStory.Gloves
//     switches off: gone all the same).
//   - The Emerald Moon (chapter 4): the moon base's guards who chase on sight (scenes 13, 23, 54), and the cell's prisoners -- Baldino's
//     old cell, the translator, the escape that ends in the game's crash and its chapter 6 (scene 53) -- gone; the cell door's little key
//     opens nothing (Twinsen would be held still for an escape that no longer comes: scene 54's hero waits for var 85 8).
//   - Otringal (chapter 5): the hotel's receptionist points at Johnny Rocket's armchair only while he is in it (var 124 3), and Twinsen asks
//     for him only then; the chest in scene 80 isn't opened with a little key (its Fragment, see below). The steward's token: RaceTrackStory
//     .Otringal.cs (not before Lyra has told of the fee).
//   - Celebration Island (chapters 6, 7, 13, 14): the game's FunFrock on the dock (scene 95's actor 12, while var 79 is 1: the statue risen,
//     as the story's landing has it too) and his "Adieu!!!" that sets var 79 2 under the story's own FunFrock, with Twinsen's own hold
//     for that cutscene (his scene start on the rocks at the statue's foot, NO_MOVE, while var 79 is 1); the two armed guards on
//     the dock (actors 8, 9, the game's own till chapter 9); and the statue's inside -- the door up in the statue into scene 185, the game's
//     ending without the final's race -- taken out (its change-cube zone).
//   - Wannies Island (chapter 9): the end of the game's chain to the Ferryman's Song (chapter 11's prize) -- the housemaid who gives it (scene
//     116's actor 4) and the pier's ferryman who answers it (scene 97's actor 2) -- and the free glove by the miner who says "No glove, no
//     driving" (scene 101's actor 10, item 11: the story's glove is in the frog's mouth).
//   - Mosquibees Island (chapter 10): the Queen's audience. Since the retold choices were answered under their new numbers (commit
//     782612f) the Mosquibee in scene 105 ("I've come to race your Queen!") flies Twinsen to her throne room (scene 104): the game's arrival
//     there holds him unseen while she is carried in, then her audience; her two ways back out (into 106 and 128) stay shut until var 69
//     changes, which only the Dissidents' Ring shown to her does -- the story's ring, from the hide-out -- and that is the game's test of
//     truth (scene 128), its blowtron and its soldiers' raid. So: no ride (that choice no longer offered), the maze's door into the throne room (scene 106)
//     taken out, the Queen's audience not given (she stays, for the bow of RaceTrackStory.Eggs.Mosquibees.cs, should Twinsen come by
//     another way) and the ring shown to her doing nothing, her adviser (actor 5) gone.
//   - The Fragments: the game's four (items 31-34) merge into one when the fourth is found (var 55 1, items 32-34 0). The story gives each
//     as a race's prize, after-race; its palace gate (RaceTrackStory.Palace.cs) also takes the merged one now, but the game's own pickups --
//     the chest in Otringal's scene 80, the Island of the Francos' dig (zone 1 of its village scenes, with the pick-ax: and the shop's
//     pick-ax itself, scene 171), the Mosquibees' chest (scene 103), the Wannies' temple (scene 117) -- are shut in the story: a Fragment
//     before its race, or the merge, isn't the story's.
//   - The Island under Celebration (chapter 11): the game's ferryman at the lava's edge (scene 132's actor 2), who answers the Ferryman's
//     Song with a ferry to the Mosquibees or the Wannies for four gems -- the story's ferryman (RaceTrackStory.Volcano.cs) is the one.
//   - Island CX (chapter 12): the doors into the game's scene 181 (its boss and Sendell's vision, set_holo_pos(93)) taken out -- on the Mk2
//     island the game's door closers aren't in its scenes, so the doors are open from the start: scene 343's, from the ground beside it
//     (y 4190 by the door's 1280-4544), took Twinsen into 181 on the build of 2026-11-05 -- and scene 179's way in is always open.
// A person is found by what their script does (the race build takes people off the roads and renumbers the rest, and the scene numbers
// below are where the built game of 2026-11-05 has them, checked first); each change is made once (a second pass over the same files --
// an island's second track -- finds it there and leaves it). What isn't as expected is left out and logged.
internal static partial class RaceTrackStory
{
    // One person's script changed: in `scene` (-1: every scene of the island whose actor `actor` matches), the actor whose life script
    // matches `who` (looked for at `actor` first), `change` applied; `why` for the log
    private sealed record Retirement(int Scene, int Actor, string Who, string Why, Func<string, string> Change);
    // A change-cube zone taken out: in `scene` (-1: every scene of the island), every zone into `into`
    private sealed record ClosedDoor(int Scene, int Into, string Why);

    // the story's chapters on each island: the game's own pickups gated on `N > chapter()` are never there in the story
    // (the lava lake's, 6: RaceTrackStory.LavaLake.cs)
    private const int DesertChapter = 3, MoonChapter = 4, OtringalChapter = 5, FrancosChapter = 7;

    // An island's retirements (RaceTrackService.Story: once the island's story is built, before its people are retold). Adds to nothing
    // else; what it did, the log.
    internal static List<string> RetireQuests(string gameDirectory, RaceTrackIsland island)
    {
        var log = new List<string>();
        var (people, doors) = island.IslandByte switch
        {
            0 => CitadelRetired(),
            2 => DesertRetired(),
            3 => MoonRetired(),
            4 => OtringalRetired(),
            5 => CelebrationRetired(),
            6 => WanniesRetired(),
            7 => MosquibeesRetired(),
            8 => FrancosRetired(),
            9 => (new List<Retirement>(), new List<ClosedDoor> { new(-1, 181, "the doors into the game's scene 181 (its boss, Sendell's vision): not in the story") }),
            11 => (new List<Retirement> { new(132, 2, @"change_cube\(105\)", "the game's ferryman at the lava's edge (the Ferryman's Song, four gems) gone from chapter 11: the story's ferryman is the one", Gone("10 < chapter()")) }, new List<ClosedDoor>()),
            _ => (new List<Retirement>(), new List<ClosedDoor>()),
        };
        if (people.Count == 0 && doors.Count == 0) return log;

        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var islandScenes = new List<int>();
        for (var s = 0; s < store.SceneCount; s++)
        {
            if (!store.SceneExists(s)) continue;
            try { if (store.Load(s).Island == island.IslandByte) islandScenes.Add(s); }
            catch (Exception e) when (e is InvalidDataException or IOException) { }
        }
        var changes = new List<SceneChange>();
        foreach (var scene in islandScenes)
        {
            var mine = people.Where(p => p.Scene == scene || p.Scene < 0).ToList();
            var shut = doors.Where(d => d.Scene == scene || d.Scene < 0).ToList();
            if (mine.Count == 0 && shut.Count == 0) continue;
            SceneModel model;
            try { model = store.Load(scene); } catch (Exception e) when (e is InvalidDataException or IOException) { continue; }
            var said = new List<string>();
            var zones = false;
            foreach (var d in shut)
            {
                var gone = model.Zones.RemoveAll(z => z.Type == 0 && z.Num == d.Into);
                if (gone > 0) { zones = true; said.Add($"{d.Why} ({gone} zone{(gone > 1 ? "s" : "")})"); }
                else if (d.Scene >= 0) log.Add($"scene {scene}: {d.Why}: no zone into {d.Into} (taken out already, or not there)");
            }
            var scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
            var edited = false;
            foreach (var p in mine)
            {
                var actor = Who(scripts, p);
                if (actor < 0)
                {
                    if (p.Scene >= 0) log.Add($"scene {scene}: {p.Why} left out: no actor whose script does /{p.Who}/");
                    continue;
                }
                try
                {
                    var life = scripts.GetText(actor, ScriptKind.Life);
                    var now = p.Change(life);
                    if (now == life) { if (p.Scene >= 0) log.Add($"scene {scene} actor {actor}: {p.Why} -- there already"); continue; }
                    scripts.SetText(actor, ScriptKind.Life, now);
                    edited = true;
                    said.Add($"actor {actor}: {p.Why}{(p.Scene >= 0 && actor != p.Actor ? $" (actor {actor}, not {p.Actor})" : "")}");
                }
                catch (NotTheGames e) { log.Add($"scene {scene} actor {actor}: {p.Why} left out: the script isn't the game's own: {e.Message}"); }
            }
            if (!edited && !zones) continue;
            byte[] record;
            if (edited)
            {
                var built = scripts.Build();
                if (!built.Ok) { log.Add($"scene {scene}: the game's quests' retirement would not compile, left as it was: {string.Join("; ", built.Errors.Take(3))}"); continue; }
                record = built.Record!;
            }
            else record = SceneSerializer.Write(model);
            changes.Add(new SceneChange(scene, SceneSerializer.Parse(SceneGame.Lba2, record), null));
            foreach (var s in said) log.Add($"scene {scene}: {s}");
        }
        store.SaveMany(changes, allowErrors: true);
        log.Add($"{island.Name}: the game's quests retired in {changes.Count} scene{(changes.Count == 1 ? "" : "s")}");
        return log;
    }

    // the actor a retirement is for: at its place if that script does what it says, else the one actor that does (-1: none, or more than one)
    private static int Who(SceneScripts scripts, Retirement p)
    {
        var rx = new Regex(p.Who, RegexOptions.Singleline);
        bool Does(int a)
        {
            if (a < 0 || a >= scripts.ActorCount) return false;
            try { return rx.IsMatch(scripts.GetText(a, ScriptKind.Life)); } catch (InvalidOperationException) { return false; }
        }
        if (Does(p.Actor)) return p.Actor;
        if (p.Scene < 0) return -1;
        var found = Enumerable.Range(0, scripts.ActorCount).Where(Does).ToList();
        return found.Count == 1 ? found[0] : -1;
    }

    // ---- the changes ---------------------------------------------------------------------------------------------------------------------
    // `text` as a pattern that any spaces and line breaks between its words match (Loose's)
    private static string LoosePattern(string text) => string.Join(@"\s*", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(Regex.Escape));

    // gone (suicide) as the scene starts while `when` holds; before whatever the game's script does first
    private static Func<string, string> Gone(string when)
    {
        var gate = $"void comportement_0()\n{{\n    if ({when})\n    {{\n        suicide();\n    }}\n";
        return life => Regex.IsMatch(life, LoosePattern(gate)) ? life : Loose(life, "void comportement_0()\n{", gate);
    }

    // `what` (the game's) made `with`, once or everywhere it is (`all`); left alone if `with` is there already
    private static Func<string, string> Swap(string what, string with, bool all = false) => life =>
    {
        if (Regex.IsMatch(life, LoosePattern(with))) return life;
        if (!all) return Loose(life, what, with);
        var rx = new Regex(LoosePattern(what));
        if (!rx.IsMatch(life)) throw new NotTheGames($"no \"{what.Split('\n')[0].Trim()}\"");
        return rx.Replace(life, with.Replace("$", "$$"));
    };

    // `what` taken out where it is (nothing if it isn't: taken out already)
    private static Func<string, string> Drop(string what) => life =>
    {
        var m = Regex.Match(life, LoosePattern(what));
        return m.Success ? life[..m.Index] + life[(m.Index + m.Length)..] : life;
    };

    // a behaviour's (comportement's) whole body run only while `when` holds, `otherwise` else
    private static Func<string, string> Guard(string comportement, string when, string otherwise) => life =>
    {
        var head = Regex.Match(life, $@"void\s+{comportement}\(\)\s*\{{");
        if (!head.Success) throw new NotTheGames($"no {comportement}");
        int open = head.Index + head.Length - 1, depth = 0, close = -1;
        for (var i = open; i < life.Length; i++)
        {
            if (life[i] == '{') depth++;
            else if (life[i] == '}' && --depth == 0) { close = i; break; }
        }
        if (close < 0) throw new NotTheGames($"{comportement} has no end");
        var body = life[(open + 1)..close];
        if (Regex.IsMatch(body, @"^\s*if\s*\(" + LoosePattern(when) + @"\)")) return life;
        return life[..(open + 1)] + $"\n    if ({when})\n    {{{body}    }}\n    else\n    {{\n        {otherwise}\n    }}\n" + life[close..];
    };

    // several changes in turn
    private static Func<string, string> All(params Func<string, string>[] changes) => life => changes.Aggregate(life, (l, c) => c(l));

    // ---- each island's ------------------------------------------------------------------------------------------------------------------
    private static (List<Retirement>, List<ClosedDoor>) CitadelRetired() => (new List<Retirement>
    {
        // (the umbrella man and his customer: the game's own gates are chapter 1 and var 57 1 or 2 -- the pharmacy's snatch sets it 1)
        new(48, 6, @"found_object\(20\)", "the game's umbrella man on the harbour (the umbrella, item 20, for his customer) gone: the story's umbrella goes back to Tim", Gone("0 < chapter()")),
        new(48, 7, @"set_anim_dial\(86\)", "the umbrella man's customer (the umbrella given her: the Desert's healing wizard, var 62, the holomap's arrow) gone", Gone("0 < chapter()")),
        // (the clerk's sale: from the game's var 111 or 62 on, with no ticket -- the story's ticket is the town circuit's prize, after which
        // Twinsen has one; in the storm, her own line that there are no ferries)
        new(8, 2, @"set_var_game\(59, 1\)", "the ferry office sells no Desert ticket before the town circuit is won (the ticket is its prize); in the storm, \"No ferries in this storm\"", life =>
        {
            var storm = Regex.Match(life, @"if \(2 > chapter\(\)\)\s*\{\s*message\((\d+)\);\s*\}");
            if (!storm.Success) throw new NotTheGames("no storm line");
            return All(
                Swap("if (0 < var_game(111) || 0 < var_game(62))", $"if (0 < var_game({WonVar}) && (0 < var_game(111) || 0 < var_game(62)))"),
                Swap("message(314);\nmessage_obj(0, 311);\nmessage(310);",
                    $"if (2 > chapter())\n{{\nmessage({storm.Groups[1].Value});\n}}\nelse\n{{\nmessage(314);\nmessage_obj(0, 311);\nmessage(310);\n}}"))(life);
        }),
        new(42, 5, @"set_var_game\(70, 1\)", "the Esmer of the game's landing not on the quay the night before they land (chapter 1)", Gone("2 > chapter()")),
        // (the Tavern's own Luc and drunk rabbibunny: in chapter 2 the story has Luc in the yard by his barrel car and the rabbibunny round
        // the Esmers' landing -- RaceTrackStory.Chapters.cs, Yard and TownPeople -- so not in the Tavern too; back behind the bar after.
        // Tim's MOVE_FOLLOW 4 and the regular's message_obj(4, 71) are the game's own with the rabbibunny gone, as on var 51)
        new(3, 4, @"1 == var_game\(51\) \|\| 4 == chapter\(\)", "the Tavern's drunk rabbibunny not in the Tavern in chapter 2 (he's round the Esmers' landing)", Gone("2 == chapter()")),
        new(3, 5, @"set_dir_obj\(6, MOVE_FOLLOW, 5\)", "Luc not behind the Tavern's bar in chapter 2 (he's in the yard by his barrel car)", Gone("2 == chapter()")),
        // (the game's Zoe in the yard: from chapter 2 the story's own Zoe is there and follows Twinsen; the game's ran up to him with the
        // healing wizard's helper (951, var 62 and the holomap's arrow) -- the scripted playthrough of 2026-11-05)
        new(49, 4, @"0 < var_game\(42\) \|\| 3 == var_game\(74\)", "the game's Zoe in the yard gone from chapter 2 (the story's Zoe is there; the game's told of Ker'aooc's helper, var 62)", Gone("1 < chapter()")),
        // (the game's Zoe on the lighthouse's top, where its chapter 2 starts her after the spell -- the story's Zoe goes with Twinsen from
        // chapter 2, and that is the very place the lighthouse's door puts him up (track point 5): she stood in it and pushed him out of
        // the top's zone, where neither the telescope nor the way down answered his Action -- the check of 2026-11-06)
        new(46, 5, @"2 < chapter\(\) \|\| 4 == var_game\(56\)", "the game's Zoe on the lighthouse's top gone from chapter 2 (the story's Zoe goes with Twinsen; she stood where the climb puts him)", Gone("1 < chapter()")),
        // (Twinsen at the carport: his "where are my driving gloves?" (retail 32, retold) while he hasn't the gloves (item 7), not while
        // the game's var 74 is under 2 -- the story never sets it, so he asked for the gloves he was wearing)
        new(49, 0, @"2 > var_game\(74\) && 4 == zone\(\)", "Twinsen asks for his driving gloves at the carport only without them (item 7)",
            Swap("2 > var_game(74) && 4 == zone()", $"0 == var_game({GlovesSlot}) && 4 == zone()")),
    }, new List<ClosedDoor>());

    private static (List<Retirement>, List<ClosedDoor>) DesertRetired() => (new List<Retirement>
    {
        new(-1, 0, @"1 == use_inventory\(30\)", "the Wizard's Diploma shown to a peddler buys no tunic (the game's: item 4, var 109, the Hacienda's invitation)",
            Swap("1 == use_inventory(30)", $"{DesertChapter} > chapter() && 1 == use_inventory(30)", all: true)),
        new(29, 0, @"1 == var_game\(109\)", "the Hacienda's invitation (the tunic, var 109: the game's kidnapping to Otringal) not in the story",
            Swap("1 == var_game(109)", $"{DesertChapter} > chapter() && 1 == var_game(109)", all: true)),
        new(60, 26, @"2 == var_game\(74\) && 1 == var_game\(26\)", "Baldino's radio in the plaza (the game's \"I fixed the car\", var 74) gone in the story", Gone($"{DesertChapter - 1} < chapter()")),
        new(65, 6, @"4 == var_game\(74\) \|\| 3 == var_game\(74\)", "the game's Zoe off shopping by the harbour (var 74 3 or 4) gone in the story", Gone($"{DesertChapter - 1} < chapter()")),
    }, new List<ClosedDoor>());

    private static (List<Retirement>, List<ClosedDoor>) MoonRetired()
    {
        var moon = $"{MoonChapter - 1} < chapter()";
        return (new List<Retirement>
        {
            new(54, 0, @"set_var_game\(85, 7\)", "the cell door's little key opens nothing (the game's escape with Baldino: var 85 7-10, its crash, chapter 6)",
                Swap("if (4 == col())", $"if ({MoonChapter} > chapter() && 4 == col())")),
            new(54, 5, @"suicide\(\);\s*\}\s*set_track\(label_0\);\s*set_comportement\(comportement_1\);.*use_inventory\(29\)", "the cell's guard who chases on sight gone", Gone(moon)),
            new(54, 6, @"set_dir\(MOVE_FOLLOW, 8\)", "Baldino in his old cell (the game's escape) gone", Gone(moon)),
            new(54, 7, @"found_object\(29\)", "the cell's prisoner (the translator, item 29, and the escape) gone", Gone(moon)),
            new(54, 8, @"if \(7 == var_game\(85\)\)", "the cell's second guard gone", Gone(moon)),
            new(23, 11, @"3 == zone_obj\(0\) && 1 == var_game\(93\)", "the base's guard who chases on sight gone", Gone(moon)),
            new(23, 14, @"pos_point\(12\);\s*if \(5 == zone_obj\(0\)\)", "the base's guard who calls \"Halt!!\" and chases gone", Gone(moon)),
            new(13, 6, @"7 < var_game\(85\) \|\| 1 == var_game\(93\)", "the corridor's guard who chases on sight gone", Gone(moon)),
        }, new List<ClosedDoor>());
    }

    private static (List<Retirement>, List<ClosedDoor>) OtringalRetired() => (new List<Retirement>
    {
        // (Johnny and his manicurist are in the hotel only while var 124 is 3: the game's later chapter)
        new(81, 0, @"0 != var_cube\(5\) \|\| 0 != var_game\(132\)", "Twinsen asks the receptionist for Johnny Rocket only while he's in the hotel (var 124 3); else a hello",
            Swap("if (0 != var_cube(5) || 0 != var_game(132))", "if (0 != var_cube(5) || 0 != var_game(132) || 3 != var_game(124))")),
        new(81, 4, @"1 == var_cube\(5\) \|\| 1 == var_game\(132\)", "the receptionist points at Johnny Rocket's armchair only while he's in it (var 124 3); else her own line",
            Swap("if (1 == var_cube(5) || 1 == var_game(132))", "if (1 == var_cube(5) || 1 == var_game(132) || 3 != var_game(124))")),
        new(80, 3, @"set_var_game\(148, 3\)", "the chest's Fragment (item 32, and the four Fragments' merge) not with a little key in the story",
            Swap("if (0 < nb_little_keys())", $"if ({OtringalChapter} > chapter() && 0 < nb_little_keys())")),
    }, new List<ClosedDoor>());

    private static (List<Retirement>, List<ClosedDoor>) CelebrationRetired() => (new List<Retirement>
    {
        new(95, 12, @"set_var_game\(79, 2\)", "the game's FunFrock on the dock (\"At last we meet again\", \"Adieu!!!\", var 79 2) gone once the statue rises in the story (chapter 13)", Gone("12 < chapter()")),
        // (and Twinsen's own part of that cutscene: with var 79 1 the game's scene start held him for it -- save_hero, NO_MOVE, pos_point(10)
        // on the rocks at the statue's foot, for FunFrock's "Adieu!!!" to let go (restore_hero, 79 2). With that FunFrock gone the story's
        // landing left him there all the final, unable to walk or press Action (the scripted playthrough, 2026-11-05). In the story he lands
        // where the scene starts, behind his car on pole, the cameras as the game has them once the statue has risen (its 79 2))
        new(95, 0, @"1 == var_game\(79\)\)\s*\{\s*save_hero\(\);\s*set_dir\(NO_MOVE\);\s*pos_point\(10\)", "Twinsen not held on the rocks at the statue's foot for the game's statue cutscene (var 79 1): in the story he lands free, by his car",
            All(Swap("if (1 == var_game(79))", "if (13 > chapter() && 1 == var_game(79))"),
                Swap("if (1 < var_game(79))", "if (0 < var_game(79))"))),
        new(95, 8, @"set_var_game\(112, 1\)", "the dock's armed guard (\"After him!!\") gone from the lava lake's chapter", Gone($"{LavaChapter - 1} < chapter()")),
        new(95, 9, @"1 == var_cube\(1\) \|\| 2000 > cone_view\(0\)", "the dock's second armed guard (\"Halt!!\") gone from the lava lake's chapter", Gone($"{LavaChapter - 1} < chapter()")),
    }, new List<ClosedDoor>
    {
        new(95, 185, "the door up into the statue (scene 185: the Sendells, FunFrock, the game's ending) taken out"),
    });

    private static (List<Retirement>, List<ClosedDoor>) WanniesRetired() => (new List<Retirement>
    {
        new(116, 4, @"found_object\(18\)", "the housemaid who gives the Ferryman's Song (item 18, chapter 11's prize) gone", Gone("8 < chapter()")),
        new(97, 2, @"use_inventory\(18\)", "the pier's ferryman (the Song, four gems: the Mosquibees, the volcano) gone", Gone("8 < chapter()")),
        new(101, 10, @"if \(0 < var_game\(11\)\)\s*\{\s*suicide\(\);\s*\}\s*else\s*\{\s*end_life\(\);", "the free glove by the miner (item 11: the story's is in the frog's mouth) gone", Gone("8 < chapter()")),
        new(117, 0, @"play_acf\(""FRAGMENT""\)", "the temple's Fragment (item 34, and the four Fragments' merge) not taken in the story: it is the race's prize",
            Swap("if (1500 > distance_message(6))\n{\nplay_acf(\"FRAGMENT\");", "if (9 > chapter() && 1500 > distance_message(6))\n{\nplay_acf(\"FRAGMENT\");")),
    }, new List<ClosedDoor>());

    private static (List<Retirement>, List<ClosedDoor>) MosquibeesRetired() => (new List<Retirement>
    {
        new(105, 6, @"set_dir_obj\(0, MOVE_FOLLOW, 6\)", "no ride to the Queen's throne room (that choice not offered): the game's audience and test of truth aren't the story's",
            All(Drop("add_choice(26);"), Swap("if (26 == choice())", "if (10 > chapter() && 26 == choice())"))),
        new(104, 2, @"change_cube\(128\)", "the Queen gives no audience in the story, and the ring shown her does nothing (the game's test of truth, scene 128); she stays for the bow",
            All(Guard("comportement_2", "10 > chapter()", "set_comportement(comportement_1);"), Swap("if (1 == use_inventory(16))", "if (10 > chapter() && 1 == use_inventory(16))"))),
        new(104, 5, @"if \(2 == var_cube\(0\)\)", "the Queen's adviser (\"Don't you have a pass?\") gone from the throne room", Gone("9 < chapter()")),
        new(103, 0, @"use_one_little_key\(\);\s*set_track_obj\(5, label_10\)", "the chest's Fragment (item 33, and the four Fragments' merge) not with a little key in the story: it is the race's prize",
            Swap("if (0 == var_game(55) && 0 == var_game(33) && 0 < nb_little_keys()", "if (10 > chapter() && 0 == var_game(55) && 0 == var_game(33) && 0 < nb_little_keys()")),
    }, new List<ClosedDoor>
    {
        new(106, 104, "the maze's door into the Queen's throne room taken out"),
    });

    private static (List<Retirement>, List<ClosedDoor>) FrancosRetired() => (new List<Retirement>
    {
        new(171, 0, @"found_object\(36\)", "the shop sells no pick-ax (item 36: the game's dig for the Francos' Fragment) in the story",
            Swap("else if (1200 > distance_message(3))", $"else if ({FrancosChapter} > chapter() && 1200 > distance_message(3))")),
        new(-1, 0, @"0 == var_game\(31\) && 0 == var_game\(55\) && 1 == zone\(\) && 1 == use_inventory\(36\)", "the village's dig (the Francos' Fragment, item 31, and the four Fragments' merge) not in the story: it is the race's prize",
            Swap("1 == use_inventory(36)", $"{FrancosChapter} > chapter() && 1 == use_inventory(36)", all: true)),
    }, new List<ClosedDoor>());
}
