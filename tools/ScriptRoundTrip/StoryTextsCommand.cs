using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LBAAssembler;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace ScriptRoundTrip;

// storytexts <game> <out folder>: every scene's life scripts as the editor's C text, each text said -- message, message_obj, add_choice,
// ask_choice... -- followed by its English words in a comment, one file a scene's island (island_<n>.c) and a list of every text said and
// where (said.tsv: island, scene, actor, text id, words). For reading a built story through for what no longer fits it (the user,
// 2026-11-05: "let's run a full sweep of the entire game to check for anything else we've missed", after Citadel's pharmacy still asked
// for a cure for the Dino-Fly the story says isn't hurt). The island's text file is island + 3, as IslandRetell has it.
internal static class StoryTextsCommand
{
    private static readonly Regex Says = new(
        @"\b(message|message_obj|message_zoe|add_choice|ask_choice|ask_choice_obj|add_message|add_message_obj|pcx_mess_obj|end_message_obj)\s*\(([^;()]*?)(\d+)\s*\)");

    public static int Run(string[] args)
    {
        var game = args[1];
        var output = args[2];
        Directory.CreateDirectory(output);
        var store = new SceneStore(SceneGame.Lba2, game);
        var text = HqrArchive.Open(Path.Combine(game, "TEXT.HQR"));
        var english = Lba2TextBank.English(text);
        var banks = new Dictionary<int, Dictionary<int, string>>();
        string? Words(int island, int id)
        {
            if (!banks.TryGetValue(island, out var bank))
            {
                bank = new Dictionary<int, string>();
                try { foreach (var t in Lba2TextBank.Load(text, english, island + 3).Texts) bank[t.Id] = t.Value; } catch (Exception e) when (e is InvalidDataException or ArgumentException or IndexOutOfRangeException) { }
                banks[island] = bank;
            }
            return bank.TryGetValue(id, out var w) ? w : null;
        }
        var names = HqdDescriptions.Load("SCENE2.HQD", 0).Names;
        var files = new Dictionary<int, StringBuilder>();
        var said = new StringBuilder("island\tscene\tactor\ttext\twords\n");
        var count = 0;
        for (var scene = 0; scene < store.SceneCount; scene++)
        {
            if (!store.SceneExists(scene)) continue;
            SceneModel model;
            SceneScripts scripts;
            try { model = store.Load(scene); scripts = SceneScripts.Load(store.LoadRecord(scene), scene); }
            catch (Exception e) when (e is InvalidDataException or SceneEditException or ArgumentException or IndexOutOfRangeException) { continue; }
            var island = model.Island;
            if (!files.TryGetValue(island, out var file)) files[island] = file = new StringBuilder();
            var name = scene + 1 < names.Count ? names[scene + 1] : null;
            file.AppendLine($"// ===================== scene {scene}{(name is null ? "" : $": {name}")} (island {island}, {(model.CubeMode == 1 ? "outside" : "inside")}, {model.Actors.Count} actors)");
            for (var actor = 0; actor < scripts.ActorCount; actor++)
            {
                string life;
                try { life = scripts.GetText(actor, ScriptKind.Life); } catch (Exception e) when (e is InvalidDataException or ArgumentException or IndexOutOfRangeException) { continue; }
                if (!Says.IsMatch(life)) continue;
                var a = actor < model.Actors.Count ? model.Actors[actor] : null;
                file.AppendLine($"// ---- scene {scene}, actor {actor}{(actor == 0 ? " (Twinsen)" : "")}{(a is null ? "" : $", entity {a.Entity}, body {a.Body}")}");
                foreach (var line in life.Split('\n'))
                {
                    var m = Says.Match(line);
                    if (!m.Success) { if (line.Trim().Length > 0) file.AppendLine(line.TrimEnd()); continue; }
                    var id = int.Parse(m.Groups[3].Value);
                    var words = Words(island, id) ?? "(no such text)";
                    file.AppendLine($"{line.TrimEnd()}    // \"{words.Replace("\r", " ").Replace("\n", " ")}\"");
                    said.Append($"{island}\t{scene}\t{actor}\t{id}\t{words.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ")}\n");
                    count++;
                }
            }
        }
        foreach (var (island, file) in files) File.WriteAllText(Path.Combine(output, $"island_{island}.c"), file.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(output, "said.tsv"), said.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"storytexts: {count} texts said in {files.Count} islands' scenes, written to {output}");
        return 0;
    }
}
