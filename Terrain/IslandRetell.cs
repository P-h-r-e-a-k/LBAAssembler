using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// An island's people, retold for the race mod's story (the user, 2026-11-03: "Every NPC we leave in the game should have updated text
// lines to suit our new mod"; the story: docs/LBRA_WALKTHROUGH.md). The game's own scripts still run -- who stands where, who answers
// what and when -- but what they say is the mod's: on Citadel a storm with a race track in it where the Tavern was, a thief with
// Twinsen's driving gloves, then the Esmers' circuit and Cup; on the Desert and the Emerald Moon, the Cup come to town.
//
// Each retold line (an island's table, IslandRetell.<Island>.cs: the game's text it replaces, then its words in English, French, German,
// Spanish, Italian, Portuguese) is a new text at the end of the island's text file (island + 3), and every script of the island's
// scenes that says the game's text says the new one instead. New texts, not the old ones rewritten: the CD version speaks a text by its
// place in the file (MESSAGE.CPP Speak), so an old place would speak the old words; at the end they are shown, not spoken. One of the
// game's texts the scripts no longer say keeps its place but loses its words: the file's offsets are 16-bit (64 KB a language), and
// Citadel's German was 36 KB before this.
//
// Untouched: what Twinsen says himself unless it is the story's (on Citadel his question to everyone, the thief's, is), prices, shop
// menus, greetings, "It's locked.", the jukebox and the slot machines, and the game's lines from later chapters, which nothing in the
// mod's story reaches on the island. The race mod's own story texts come after the game's and are left alone. Runs after the island's
// story (RaceTrackStory), so the scripts it changed are retold too.
internal static partial class IslandRetell
{
    // what says a text (the text is the last operand): NpcTexts' list
    private static readonly Regex Says = new(
        @"\b(message|message_obj|message_zoe|add_choice|ask_choice|ask_choice_obj|add_message|add_message_obj|pcx_mess_obj|end_message_obj)\s*\(([^;()]*?)(\d+)\s*\)");

    // The island's people retold: `lines` its table, `name` for the log.
    public static List<string> Apply(string gameDirectory, int island, (int Retail, string[] Words)[] lines, string name)
    {
        var log = new List<string>();
        var texts = island + 3;
        var textPath = Path.Combine(gameDirectory, "TEXT.HQR");
        var text = HqrArchive.Open(textPath);
        var languages = Lba2TextBank.Languages(textPath);
        var first = Enumerable.Range(0, languages).Max(l => Lba2TextBank.Load(text, l, texts).Texts.Select(t => t.Id).DefaultIfEmpty(0).Max()) + 1;
        // (once a build: the island's two tracks -- the Emerald Moon and the old moon share island 3's texts -- or a second story pass)
        var englishWords = Lba2TextBank.Load(text, 0, texts).Texts.Select(t => t.Value).ToHashSet();
        if (lines.Length > 0 && englishWords.Contains(Encoding.Latin1.GetString(Dos.GetBytes(lines[0].Words[0]))))
        {
            log.Add($"{name}'s people are retold already");
            return log;
        }

        // one new text per distinct line (a line several people say the same, the aliens' greeting, is one text)
        var added = new List<(int Retail, string[] Words)>();
        var retold = new Dictionary<int, int>();
        var byWords = new Dictionary<string, int>();
        foreach (var (retail, words) in lines)
        {
            if (!byWords.TryGetValue(words[0], out var id)) { id = first + added.Count; byWords[words[0]] = id; added.Add((retail, words)); }
            retold[retail] = id;
        }

        // the scripts
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var changes = new List<SceneChange>();
        var stillSaid = new HashSet<int>();
        int scenes = 0, scripts = 0, replaced = 0;
        for (var s = 0; s < store.SceneCount; s++)
        {
            if (!store.SceneExists(s)) continue;
            SceneModel model;
            try { model = store.Load(s); } catch (Exception e) when (e is InvalidDataException or IOException) { continue; }
            if (model.Island != island) continue;
            var sceneScripts = SceneScripts.Load(SceneSerializer.Write(model), s);
            var changed = false;
            var said = new HashSet<int>();
            for (var a = 0; a < sceneScripts.ActorCount; a++)
            {
                string life;
                try { life = sceneScripts.GetText(a, ScriptKind.Life); }
                catch (InvalidOperationException) { continue; }
                var choices = new HashSet<int>();
                var now = Says.Replace(life, m =>
                {
                    var id = int.Parse(m.Groups[3].Value);
                    if (!retold.TryGetValue(id, out var to)) { said.Add(id); return m.Value; }
                    replaced++;
                    if (m.Groups[1].Value == "add_choice") choices.Add(id);
                    return $"{m.Groups[1].Value}({m.Groups[2].Value}{to})";
                });
                // (a choice offered under its new number is answered under it too: choice() returns the text chosen, so the script's
                // `N == choice()` and a `switch (choice())`'s `case N:` are retold with it -- the sweep of 2026-11-05 found Citadel's
                // umbrella man offering 1140 and testing 107, his answer never said)
                if (choices.Count > 0) now = RetellChoices(now, choices, retold);
                if (now == life) continue;
                sceneScripts.SetText(a, ScriptKind.Life, now);
                changed = true; scripts++;
            }
            if (!changed) { stillSaid.UnionWith(said); continue; }
            var built = sceneScripts.Build();
            if (!built.Ok)
            {
                log.Add($"scene {s}: the retold scripts would not compile, left as they were: {string.Join("; ", built.Errors.Take(3))}");
                stillSaid.UnionWith(retold.Keys);   // (nothing of the game's is emptied: this scene may still say any of it)
                continue;
            }
            stillSaid.UnionWith(said);
            changes.Add(new SceneChange(s, SceneSerializer.Parse(SceneGame.Lba2, built.Record!), null));
            scenes++;
        }
        store.SaveMany(changes, allowErrors: true);

        // the texts: the new ones, each with its game text's own attribute (how it is shown), and the game's that nothing says now emptied
        var hqr = File.ReadAllBytes(textPath);
        var emptied = retold.Keys.Where(id => !stillSaid.Contains(id)).ToHashSet();
        for (var lang = 0; lang < languages; lang++)
        {
            var bank = Lba2TextBank.Load(text, lang, texts);
            for (var k = 0; k < added.Count; k++)
            {
                var (retail, words) = added[k];
                bank.Texts.Add(new Lba2TextBank.Text
                {
                    Id = first + k,
                    Attribute = bank.Find(retail)?.Attribute ?? Lba2TextBank.NormalAttribute,
                    Bytes = Dos.GetBytes(words[lang < words.Length ? lang : 0]),
                });
            }
            foreach (var t in bank.Texts.Where(t => emptied.Contains(t.Id))) t.Bytes = Array.Empty<byte>();
            hqr = bank.WriteInto(hqr);
        }
        File.WriteAllBytes(textPath, hqr);
        log.Add($"{name}'s people retold: {lines.Length} of the game's lines as {added.Count} new texts ({first}-{first + added.Count - 1}), " +
                $"{replaced} lines in {scripts} scripts of {scenes} scenes; {emptied.Count} of the game's texts no script says any more emptied");
        return log;
    }

    // A script's tests of the choices it offers, retold as the add_choice lines were: `N == choice()`, `choice() == N`, and each
    // `case N:` at the top level of a `switch (choice())` block (only there: another switch's cases are numbers of something else).
    private static readonly Regex ChoiceTest = new(@"\b(\d+)(\s*==\s*choice\(\))|(choice\(\)\s*==\s*)(\d+)\b");
    private static readonly Regex ChoiceSwitch = new(@"switch\s*\(\s*choice\(\)\s*\)\s*\{");
    private static readonly Regex CaseLabel = new(@"\bcase\s+(\d+)\s*:");

    internal static string RetellChoices(string life, HashSet<int> offered, IReadOnlyDictionary<int, int> retold)
    {
        int To(int id) => offered.Contains(id) && retold.TryGetValue(id, out var to) ? to : id;
        life = ChoiceTest.Replace(life, m => m.Groups[1].Success
            ? $"{To(int.Parse(m.Groups[1].Value))}{m.Groups[2].Value}"
            : $"{m.Groups[3].Value}{To(int.Parse(m.Groups[4].Value))}");
        var text = new StringBuilder(life);
        foreach (Match sw in ChoiceSwitch.Matches(life).Cast<Match>().Reverse())
        {
            // (the switch's block: from its brace to the brace that closes it; its own cases at depth 1)
            int open = sw.Index + sw.Length - 1, depth = 0, close = -1;
            for (var i = open; i < life.Length; i++)
            {
                if (life[i] == '{') depth++;
                else if (life[i] == '}' && --depth == 0) { close = i; break; }
            }
            if (close < 0) continue;
            var labels = new List<Match>();
            depth = 0;
            for (var i = open; i < close; i++)
            {
                if (life[i] == '{') depth++;
                else if (life[i] == '}') depth--;
                else if (depth == 1 && CaseLabel.Match(life, i) is { Success: true } c && c.Index == i) labels.Add(c);
            }
            foreach (var c in Enumerable.Reverse(labels))
            {
                var id = int.Parse(c.Groups[1].Value);
                if (To(id) == id) continue;
                text.Remove(c.Groups[1].Index, c.Groups[1].Length).Insert(c.Groups[1].Index, To(id).ToString());
            }
        }
        return text.ToString();
    }

    private static Encoding Dos
    {
        get
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(850);
        }
    }
}
