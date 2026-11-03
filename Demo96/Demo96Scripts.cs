using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LBAAssembler.LbaScript;

namespace LBAAssembler.Demo96;

// The 1996 demo's scripts in the retail game's bytecode. They are decompiled in the demo's dialect (Opcodes.Demo96: the retail language
// but for the operand sizes of a few instructions), the instructions retail doesn't have are rewritten as retail ones, and the text is
// compiled again in the retail dialect -- so SAMPLE's one-byte sample number becomes the retail two bytes, and so on.
//
// The demo's own instructions (Demo96Tables):
//   DEMO_132  the talking actor says its line for the chapter: the hero one of a few picked at random (HeroLines, a table in
//             LBA2DEMO.EXE at 0x80B0A), any other actor its own (the 20 numbers in the demo's actor record). Only below chapter 20.
//             -> switch (chapter()) { case n: message(line) ... }, the hero's lines a switch (rnd(k)) inside.
//   DEMO_70   an island decor object shown or hidden (scene 42 only); DEMO_72 the holomap icon; DEMO_88 extras thrown about an actor;
//             DEMO_99 a palette call -- nothing in the retail game does these: they are left out, a comment says where.
internal static class Demo96Scripts
{
    // LBA2DEMO.EXE 0x80B0A: five text numbers a chapter (-1 = none); the hero's DEMO_132 says one of the first n, n = how many aren't -1
    private static readonly short[][] HeroLines =
    {
        new short[] { 6, 100, 101, 102, -1 },
        new short[] { -1, 105, 106, 107, -1 },
        new short[] { -1, 100, 101, 102, -1 }, new short[] { -1, 100, 101, 102, -1 }, new short[] { -1, 100, 101, 102, -1 },
        new short[] { -1, 100, 101, 102, -1 }, new short[] { -1, 100, 101, 102, -1 }, new short[] { -1, 100, 101, 102, -1 },
    };

    private static readonly Regex DemoCall = new(@"^(?<indent>[ \t]*)demo_(?<op>\d+)\((?<args>[^)]*)\);[ \t]*$", RegexOptions.Multiline);

    // The scene record (retail layout, demo scripts) with its scripts in the retail bytecode; `chapterLines` is each actor's chapter lines
    // (Demo96Scenes.Parse). Throws when a script doesn't translate.
    public static byte[] Translate(byte[] record, int scene, IReadOnlyList<short[]?> chapterLines)
    {
        var scripts = SceneScripts.Load(record, Opcodes.Demo96, scene);
        var result = scripts.Translate(Opcodes.Lba2, (actor, kind, text) => kind == ScriptKind.Life ? Rewrite(text, actor, chapterLines) : text);
        if (!result.Ok) throw new InvalidDataException("The demo's scripts don't translate: " + string.Join("; ", result.Errors.Take(3)));
        return result.Record!;
    }

    private static string Rewrite(string text, int actor, IReadOnlyList<short[]?> chapterLines) =>
        DemoCall.Replace(text, m =>
        {
            var indent = m.Groups["indent"].Value;
            return m.Groups["op"].Value switch
            {
                "132" => ChapterLine(indent, actor == 0 ? null : actor < chapterLines.Count ? chapterLines[actor] : null, actor == 0),
                var op => $"{indent}// (the 1996 demo's instruction {op}({m.Groups["args"].Value}): the retail game has nothing that does it)",
            };
        });

    // DEMO_132 in the retail language
    private static string ChapterLine(string indent, short[]? own, bool hero)
    {
        // chapter -> what is said (one line, or the hero's few to pick from)
        var said = new List<(int Chapter, short[] Lines)>();
        if (hero)
            for (var c = 0; c < HeroLines.Length; c++)
            {
                var n = HeroLines[c].Count(t => t != -1);
                var lines = HeroLines[c].Take(n).Where(t => t != -1).ToArray();
                if (lines.Length > 0) said.Add((c, lines));
            }
        else if (own is not null)
            for (var c = 0; c < own.Length; c++)
                if (own[c] != -1) said.Add((c, new[] { own[c] }));
        if (said.Count == 0) return $"{indent}// (the 1996 demo's chapter line: this actor has none)";

        var s = new StringBuilder();
        var groups = said.GroupBy(x => string.Join(",", x.Lines)).ToList();
        if (groups.Count == 1 && said.Count == (hero ? HeroLines.Length : 20))
            Say(s, indent, said[0].Lines);   // (the same in every chapter)
        else
        {
            s.Append(indent).Append("switch (chapter())\n").Append(indent).Append("{\n");
            foreach (var g in groups)
            {
                foreach (var (c, _) in g) s.Append(indent).Append($"    case {c}:\n");
                Say(s, indent + "        ", g.First().Lines);
                s.Append(indent).Append("        break;\n");
            }
            s.Append(indent).Append("}");
        }
        return s.ToString().TrimEnd('\n');
    }

    private static void Say(StringBuilder s, string indent, short[] lines)
    {
        if (lines.Length == 1) { s.Append(indent).Append($"message({lines[0]});\n"); return; }
        s.Append(indent).Append($"switch (rnd({lines.Length}))\n").Append(indent).Append("{\n");
        for (var k = 0; k < lines.Length; k++)
            s.Append(indent).Append($"    case {k}:\n").Append(indent).Append($"        message({lines[k]});\n").Append(indent).Append("        break;\n");
        s.Append(indent).Append("}\n");
    }
}
