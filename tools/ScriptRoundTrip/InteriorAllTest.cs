using System.IO;
using System.Runtime.InteropServices;
using LBAAssembler;

namespace ScriptRoundTrip;

// interiorall <sandbox> [first] [last] [rounds]: loads every scene through the native renderer the way the editor's scene box does (load, one frame,
// the stitched render) and prints each number before it starts, so a native crash names its scene (set LBA2_RENDERER_CRASHLOG=<file> for the faulting
// addresses). Sandbox folder only (hard links of the game files: the renderer reads them).
internal static class InteriorAllTest
{
    public static int Run(string[] args)
    {
        var sandbox = args[1];
        var first = args.Length > 2 ? int.Parse(args[2]) : 0;
        var last = args.Length > 3 ? int.Parse(args[3]) : 230;
        var rounds = args.Length > 4 ? int.Parse(args[4]) : 1;
        var dll = Environment.GetEnvironmentVariable("LBA2_RENDERER_DLL") ??
                  @"E:\dump\LBAAssembler\native\lba2-classic-community\out\build\windows_ucrt64_static\SOURCES\3DEXT\liblba2_renderer.dll";
        using var lib = new RendererLibraryApi(dll);
        if (!lib.IsLoaded) { Console.WriteLine($"could not load {dll}"); return 2; }
        if (!lib.SetDataRoot(sandbox) || !lib.Initialize()) { Console.WriteLine("native init failed"); return 2; }
        var canvas = new byte[3712 * 2400];
        // (the editor always has an island loaded and drawn before an interior is picked: the renderer needs a first exterior frame)
        // (INTERIORALL_ISLAND=<name>: that island instead of the Desert; INTERIORALL_DOWN=1: scenes last..first, the way a mouse wheel
        // over the scene box steps back through them)
        var islandName = Environment.GetEnvironmentVariable("INTERIORALL_ISLAND") is { Length: > 0 } isl ? isl : "desert";
        var down = Environment.GetEnvironmentVariable("INTERIORALL_DOWN") == "1";
        if (lib.LoadIsland(islandName) == 0) { Console.WriteLine("LoadIsland failed"); return 2; }
        lib.SetViewTarget(8 * 32768 + 16384, 10000, 9 * 32768 + 16384);
        lib.SetCamera(240, -256, 0, 30000);
        lib.RenderFrameWide(2);
        // (INTERIORALL_SWAPFROM=<folder>: once the renderer has started and drawn the island, every file of the sandbox that folder also has
        // and that differs replaced by that folder's (written beside and renamed over, as a race track build writes them) -- the editor's case of
        // 2026-11-14: a build made from the race track window while the renderer was up, then an Otringal inside scene picked)
        if (Environment.GetEnvironmentVariable("INTERIORALL_SWAPFROM") is { Length: > 0 } swapFrom)
        {
            var swapped = 0;
            foreach (var source in Directory.EnumerateFiles(swapFrom))
            {
                var target = System.IO.Path.Combine(sandbox, System.IO.Path.GetFileName(source));
                if (!File.Exists(target) || File.ReadAllBytes(target).AsSpan().SequenceEqual(File.ReadAllBytes(source))) continue;
                File.Copy(source, target + ".swap", true);
                File.Move(target + ".swap", target, true);
                swapped++;
            }
            Console.WriteLine($"swapped {swapped} files from {swapFrom}");
            // (INTERIORALL_RESTART=1: then the renderer shut down and started again, as the editor does after a build -- no island drawn
            // before the first inside scene, as when the build was made with an inside scene on screen)
            if (Environment.GetEnvironmentVariable("INTERIORALL_RESTART") == "1")
            {
                lib.Shutdown();
                if (!lib.SetDataRoot(sandbox) || !lib.Initialize()) { Console.WriteLine("native init failed after the restart"); return 2; }
                Console.WriteLine("renderer restarted");
            }
        }
        var archive = HqrArchive.Open(System.IO.Path.Combine(sandbox, "SCENE.HQR"));
        int loaded = 0, skipped = 0;
        for (var round = 0; round < rounds; round++)
        {
            // (INTERIORALL_REBUILD=<island name>: after the first round the island's track built again into the folder, the renderer
            // still loaded -- the editor's own case, 2026-11-04: a track rebuilt from the race track window, then an interior picked, and
            // a native crash in LoadInteriorScene; the build writes SCENE.HQR, BODY.HQR, ANIM.HQR and RESS.HQR under the renderer)
            if (round == 1 && Environment.GetEnvironmentVariable("INTERIORALL_REBUILD") is { Length: > 0 } rebuild)
            {
                var island = LBAAssembler.Terrain.RaceTrackIsland.BuiltIn.First(i => i.Name == rebuild);
                var built = LBAAssembler.Terrain.RaceTrackService.Build(sandbox, new List<LBAAssembler.Terrain.RaceTrackService.TrackBuild>
                    { new(LBAAssembler.Terrain.RaceTrackPlan.Built(island), LBAAssembler.Terrain.RaceTrackOptions.For(island)) });
                Console.WriteLine($"\nrebuilt {rebuild}: {built.Ok}");
                archive = HqrArchive.Open(System.IO.Path.Combine(sandbox, "SCENE.HQR"));
            }
            for (var step = first; step <= last; step++)
            {
                var scene = down ? last - (step - first) : step;
                // (the scene box only hands interiors to the interior renderer: the scene record's sixth byte, CubeMode, is 0)
                byte[]? bytes = null;
                try { if (scene + 1 < archive.Count && archive.IsValid(scene + 1)) bytes = archive.Read(scene + 1); } catch (InvalidDataException) { }
                if (bytes is not { Length: > 5 } || bytes[5] != 0) { skipped++; continue; }
                Console.Write($"{scene} "); Console.Out.Flush();
                var result = lib.LoadInteriorSceneResult(scene);
                // (-1: LBA_BKG.HQR changed since the renderer started -- the editor starts it again and loads once more: CommunityRendererBackend)
                if (result == -1)
                {
                    Console.Write("[restart] ");
                    lib.Shutdown();
                    if (!lib.SetDataRoot(sandbox) || !lib.Initialize()) { Console.WriteLine("native init failed after the restart"); return 2; }
                    result = lib.LoadInteriorSceneResult(scene);
                }
                if (result != 1) { skipped++; continue; }
                lib.GetInteriorCameraPosition(out _, out _, out _);
                lib.RenderInteriorFrame();
                var handle = GCHandle.Alloc(canvas, GCHandleType.Pinned);
                try { lib.RenderInteriorFull(handle.AddrOfPinnedObject(), 3712, 2400); }
                finally { handle.Free(); }
                loaded++;
            }
        }
        Console.WriteLine($"\ninteriorall: {loaded} scenes loaded and drawn, {skipped} not interiors, in {rounds} round(s)");
        return 0;
    }
}
