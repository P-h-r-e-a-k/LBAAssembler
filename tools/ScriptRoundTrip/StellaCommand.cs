using System.Diagnostics;
using LBAAssembler;
using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// stella <game> <out folder> [who...]: Stella Entities' performers (StellaPerformers) made from the game's bodies into copies of its BODY.HQR
// and ANIM.HQR in the out folder, each pictured as she stands from six sides (b<who>.png, BodyPipeline's carviews) and posed by her dance's
// keyframes (a<who>.png, poseviews)
internal static class StellaCommand
{
    public static int Run(string[] args)
    {
        var game = args[1]; var outDir = args[2];
        Directory.CreateDirectory(outDir);
        var who = args.Length > 3 ? args.Skip(3).Select(int.Parse).ToArray() : Enumerable.Range(0, StellaPerformers.All.Length).ToArray();
        var bodies = HqrArchive.Open(Path.Combine(game, "BODY.HQR"));
        var bodyFile = File.ReadAllBytes(Path.Combine(game, "BODY.HQR"));
        var animFile = File.ReadAllBytes(Path.Combine(game, "ANIM.HQR"));
        int firstBody = HqrArchive.CountEntries(Path.Combine(game, "BODY.HQR")), firstAnim = HqrArchive.CountEntries(Path.Combine(game, "ANIM.HQR"));
        foreach (var k in who)
        {
            var s = StellaPerformers.All[k];
            var made = StellaPerformers.Make(s, bodies.Read(s.Source));
            var parts = LBAAssembler.Terrain.Palace.PalaceTorturers.Parts.Read(made);
            // (STELLA_CALIB=1: one move a key, to see which way each goes; STELLA_FROM=n: her routine from its key n -- the pictures show ten)
            List<StellaPerformers.Pose>? routine = null;
            if (Environment.GetEnvironmentVariable("STELLA_CALIB") == "1")
                routine = new()
                {
                    new(), new() { UpA = 90 }, new() { UpB = 90 }, new() { FwdA = 90 }, new() { ElbowA = 90, FwdA = 45 }, new() { KickA = 60 },
                    new() { KneeA = 60 }, new() { Bend = 30 }, new() { Lean = 30 }, new() { Turn = 90 },
                };
            else if (int.TryParse(Environment.GetEnvironmentVariable("STELLA_FROM"), out var from))
                routine = (s.Pole ? StellaPerformers.PoleRoutine() : StellaPerformers.StageRoutine()).Skip(from).ToList();
            var anim = StellaPerformers.Animation(s, made, StellaPerformers.AnimOf(k), routine);
            Console.WriteLine($"{k} {s.Name}: BODY {s.Source} -> {parts.Points.Count} points, {parts.Bones.Count} bones, {parts.Blocks.Sum(b => b.Records.Count)} polygons; dance {anim.Keys.Count} keys, {anim.TotalMilliseconds} ms");
            bodyFile = HqrWriter.AppendEntry(bodyFile, HqrWriter.StoredEntry(made));
            animFile = HqrWriter.AppendEntry(animFile, HqrWriter.StoredEntry(anim.Write(parts.Bones.Count)));
        }
        var bodyHqr = Path.Combine(outDir, "BODY.HQR"); var animHqr = Path.Combine(outDir, "ANIM.HQR");
        File.WriteAllBytes(bodyHqr, bodyFile); File.WriteAllBytes(animHqr, animFile);
        var tool = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\BodyPipeline\bin\Release\net10.0-windows\BodyPipeline.exe"));
        for (var n = 0; n < who.Length; n++)
        {
            foreach (var command in new[]
            {
                $"carviews \"{bodyHqr}\" {firstBody + n} \"{game}\" \"{Path.Combine(outDir, $"b{who[n]}.png")}\"",
                $"poseviews \"{bodyHqr}\" {firstBody + n} \"{animHqr}\" {firstAnim + n} \"{game}\" \"{Path.Combine(outDir, $"a{who[n]}.png")}\"",
            })
            {
                using var run = Process.Start(new ProcessStartInfo(tool, command) { UseShellExecute = false })!;
                run.WaitForExit();
                if (run.ExitCode != 0) return run.ExitCode;
            }
        }
        return 0;
    }
}
