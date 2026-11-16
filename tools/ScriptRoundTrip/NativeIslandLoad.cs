using LBAAssembler;

namespace ScriptRoundTrip;

// nativeislandload <game folder> <island name>...: each island loaded by the native renderer as the editor's view loads it (and one frame
// drawn), in turn -- to find an island file that brings the editor down (2026-11-16: the editor vanished after opening OrigDESERT.ILE,
// a copy with no OrigDESERT.OBL beside it). Env LBA2_RENDERER_DLL picks the DLL.
internal static class NativeIslandLoad
{
    public static int Run(string[] args)
    {
        var dll = Environment.GetEnvironmentVariable("LBA2_RENDERER_DLL") ??
                  @"E:\dump\LBAAssembler\native\lba2-classic-community\out\build\windows_ucrt64_static\SOURCES\3DEXT\liblba2_renderer.dll";
        using var lib = new RendererLibraryApi(dll);
        if (!lib.IsLoaded) { Console.WriteLine($"could not load {dll}"); return 2; }
        if (!lib.SetDataRoot(args[1]) || !lib.Initialize()) { Console.WriteLine("native init failed"); return 2; }
        foreach (var name in args.Skip(2))
        {
            Console.Write($"{name}: ");
            Console.Out.Flush();
            var ok = lib.LoadIsland(name);
            Console.WriteLine(ok == 0 ? "LoadIsland failed" : $"loaded, {lib.GetActorCount()} actors");
            Console.Out.Flush();
        }
        return 0;
    }
}
