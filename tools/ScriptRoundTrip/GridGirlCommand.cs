using System.Diagnostics;
using LBAAssembler;
using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// gridgirl <game> <out folder> [flags]: the Imperial Hotel's Sup remodelled in her bikini (GridGirl.Remodel) -- with "flags", as the
// flag marshal (her flags in her hands, TempleMarshal's GridGirl rig) -- into a copy of the game's BODY.HQR in the out folder, and
// pictured from six sides by BodyPipeline's carviews (views.png).
internal static class GridGirlCommand
{
    public static int Run(string[] args)
    {
        var game = args[1]; var outDir = args[2];
        Directory.CreateDirectory(outDir);
        var retail = HqrArchive.Open(Path.Combine(game, "BODY.HQR")).Read(GridGirl.Retail);
        var bytes = args.Length > 3 && args[3] == "flags"
            ? LBAAssembler.Terrain.Temple.TempleMarshal.Make(retail, out _, LBAAssembler.Terrain.Temple.TempleMarshal.GridGirlRig)
            : GridGirl.Remodel(retail);
        var body = LbaBodyStudio.Body.Read(bytes, 2, allowStatic: true);
        Console.WriteLine($"her body: {body.Vertices.Count} points, {body.Bones.Count} bones, {body.Faces.Count} polygons");
        var hqr = Path.Combine(outDir, "BODY.HQR");
        File.WriteAllBytes(hqr, HqrWriter.AppendEntry(File.ReadAllBytes(Path.Combine(game, "BODY.HQR")), HqrWriter.StoredEntry(bytes)));
        var index = HqrArchive.CountEntries(hqr) - 1;
        var tool = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\BodyPipeline\bin\Release\net10.0-windows\BodyPipeline.exe"));
        using var run = Process.Start(new ProcessStartInfo(tool, $"carviews \"{hqr}\" {index} \"{game}\" \"{Path.Combine(outDir, "views.png")}\"") { UseShellExecute = false })!;
        run.WaitForExit();
        return run.ExitCode;
    }
}
