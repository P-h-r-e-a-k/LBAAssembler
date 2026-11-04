using System.IO;

namespace ScriptRoundTrip;

// discretail <LBA2 folder> <out folder>: the game folder copied, its data files (the LBA2 folder's HQRs and islands on its CD image,
// LBA2.GOG beside them) written again from the disc -- the game as it shipped, whatever an editor saved into the folder since (the user's
// GOG folder has an old BODY.HQR, SCENE.HQR and more: Twinsen drew all in black in every test sandbox copied from it, 2026-11-04). As the
// LBRA installer does (GameInstall.RetailFromDisc). The sandboxes' builds start from this.
internal static class DiscRetailCommand
{
    public static int Run(string[] args)
    {
        string from = args[1], to = args[2];
        var image = Path.Combine(from, "LBA2.GOG");
        if (!File.Exists(image)) { Console.WriteLine($"no CD image: {image}"); return 1; }
        Directory.CreateDirectory(to);
        foreach (var file in Directory.GetFiles(from))
        {
            var name = Path.GetFileName(file);
            if (name.Equals("LBA2.GOG", StringComparison.OrdinalIgnoreCase)) continue;
            File.Copy(file, Path.Combine(to, name), true);
        }
        // (the disc image itself: the engine mounts it for the videos and music)
        var imageTo = Path.Combine(to, "LBA2.GOG");
        if (!File.Exists(imageTo) || new FileInfo(imageTo).Length != new FileInfo(image).Length) File.Copy(image, imageTo, true);
        foreach (var dir in Directory.GetDirectories(from))
            CopyTree(dir, Path.Combine(to, Path.GetFileName(dir)));
        using var disc = new LBAAssembler.Lba1.Runtime.Lba1Iso(image);
        var changed = new List<string>();
        foreach (var f in disc.Files.Where(f => f.Path.StartsWith("/LBA2/", StringComparison.OrdinalIgnoreCase) && f.Path.Count(c => c == '/') == 2))
        {
            var name = Path.GetFileName(f.Path);
            if (Path.GetExtension(name).ToUpperInvariant() is not (".HQR" or ".ILE" or ".OBL")) continue;
            var target = Path.Combine(to, name.ToUpperInvariant());
            var bytes = disc.Read(f);
            if (!File.Exists(target) || !File.ReadAllBytes(target).AsSpan().SequenceEqual(bytes)) { changed.Add(name); File.WriteAllBytes(target, bytes); }
        }
        Console.WriteLine($"{to}: the folder copied, {changed.Count} data files written again from the CD: {string.Join(", ", changed)}");
        return 0;
    }

    private static void CopyTree(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.GetFiles(from)) File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(from)) CopyTree(dir, Path.Combine(to, Path.GetFileName(dir)));
    }
}
