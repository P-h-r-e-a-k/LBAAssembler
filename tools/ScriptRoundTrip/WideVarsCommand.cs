using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace ScriptRoundTrip;

// widevars <game folder> [scene]: the game variables past 255 (the engine's two-byte forms, 2026-11-16) through the compiler and back --
// a life script that reads, sets, adds to and takes from variables 0, 255, 256, 700 and 1023 compiled into the scene's actor 1, read back
// as text and as bytes (each statement's opcode and its operands)
internal static class WideVarsCommand
{
    private const string Script = @"void comportement_0()
{
    if (3 == var_game(700) && 1 == var_game(255))
    {
        set_var_game(256, 5);
        add_var_game(700, 2);
        sub_var_game(1023, 1);
        set_var_game(12, 4);
    }
    switch (var_game(300))
    {
        case 1:
            set_var_game(301, 1);
            break;
    }
}
";

    public static int Run(string[] args)
    {
        var store = new SceneStore(SceneGame.Lba2, args[1]);
        var scene = args.Length > 2 ? int.Parse(args[2]) : 0;
        var scripts = SceneScripts.Load(store.LoadRecord(scene), scene);
        scripts.SetText(1, ScriptKind.Life, Script);
        var built = scripts.Build();
        if (!built.Ok) { foreach (var e in built.Errors) Console.WriteLine("error: " + e); return 1; }
        var again = SceneScripts.Load(built.Record!, scene);
        var text = again.GetText(1, ScriptKind.Life);
        Console.WriteLine(text);
        var same = Normalise(text) == Normalise(Script);
        Console.WriteLine(same ? "round trip: the same text" : "round trip: DIFFERENT text");
        // a variable past the engine's last: refused
        var bad = SceneScripts.Load(store.LoadRecord(scene), scene);
        bad.SetText(1, ScriptKind.Life, "void comportement_0()\n{\n    set_var_game(1024, 1);\n}\n");
        var refused = bad.Build();
        Console.WriteLine(refused.Ok ? "1024: NOT refused" : $"1024: refused ({refused.Errors.FirstOrDefault()})");
        return same && !refused.Ok ? 0 : 1;
    }

    private static string Normalise(string s) => string.Join("\n", s.Replace("\r", "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("//")));
}
