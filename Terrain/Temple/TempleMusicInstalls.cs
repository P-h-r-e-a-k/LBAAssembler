using System.IO;
using System.Text.RegularExpressions;

namespace LBAAssembler.Terrain.Temple;

// The LBA1s installed on this PC by GOG and Steam -- where the race's temple music comes from when the LBA1 folder a build is given is a
// copy of LBA1's data files alone (the CD's music is its audio tracks, not files), and where LBRA's installer looks for LBA1 (its window's
// guess, its music's fallback). And the LBA2s, for that window and LBA2's menu music.
internal static partial class TempleMusic
{
    // The folders a store installs games to, on every fixed drive (GOG's own default, GOG Galaxy's, Steam's and a Steam library's)
    private static readonly string[] LibraryFolders =
    {
        "GOG Games",
        Path.Combine("GOG Galaxy", "Games"),
        Path.Combine("Program Files (x86)", "GOG Galaxy", "Games"),
        Path.Combine("Program Files", "GOG Galaxy", "Games"),
        Path.Combine("Program Files (x86)", "GOG.com"),
        "Games",
        Path.Combine("Program Files (x86)", "Steam", "steamapps", "common"),
        Path.Combine("Program Files", "Steam", "steamapps", "common"),
        Path.Combine("Steam", "steamapps", "common"),
        Path.Combine("SteamLibrary", "steamapps", "common"),
        Path.Combine("Games", "Steam", "steamapps", "common"),
        Path.Combine("Games", "SteamLibrary", "steamapps", "common"),
    };

    // The names the stores give LBA1's folder, looked in first: GOG's DOS release ("Little Big Adventure"; "Little Big AdventureLBAWin", its
    // Windows-launcher variant), Steam's and GOG's 2022 Classic ("Twinsen's Little Big Adventure Classic"). Every other folder of a library
    // is looked in too, whatever it's called (GOG's newest LBA2 installs as tlba2-classic, the user, 2026-11-01: not found by name): what's
    // in it says which game it is.
    public static readonly string[] Lba1FolderNames = { "Little Big Adventure", "Little Big AdventureLBAWin", "Twinsen's Little Big Adventure Classic" };
    public static readonly string[] Lba2FolderNames = { "Little Big Adventure 2", "Twinsen's Little Big Adventure 2 Classic", "tlba2-classic" };

    // Every LBA1 installed on this PC, as the folder with its data (SCENE.HQR, LBA_GRI.HQR ...: the install folder for GOG's DOS release,
    // its Common folder for the Classic): GOG's (the registry's GOG.com\Games paths, its usual folders), Steam's (its libraries, from
    // libraryfolders.vdf, and the usual folders). Each once, in that order.
    public static List<string> InstalledLba1Folders() => Installed(Lba1FolderNames, new[] { "", "Common", "CommonClassic", Path.Combine("Speedrun", "Windows"), "LBA", "GAME" },
        data => File.Exists(Path.Combine(data, "SCENE.HQR")) && File.Exists(Path.Combine(data, "LBA_GRI.HQR")) && File.Exists(Path.Combine(data, "LBA_BRK.HQR")));

    // ... and every LBA2 the same way (LBA1 has a RESS, a SCENE and a BODY too: its LBA_GRI says it isn't)
    public static List<string> InstalledLba2Folders() => Installed(Lba2FolderNames, new[] { "", "CommonClassic", "Common", "LBA2", "LBA" },
        data => File.Exists(Path.Combine(data, "RESS.HQR")) && File.Exists(Path.Combine(data, "SCENE.HQR")) && File.Exists(Path.Combine(data, "BODY.HQR")) && !File.Exists(Path.Combine(data, "LBA_GRI.HQR")));

    private static List<string> Installed(string[] names, string[] subs, Func<string, bool> isData)
    {
        var found = new List<string>();
        void Install(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return;
                foreach (var sub in subs)
                {
                    var data = Path.GetFullPath(Path.Combine(dir, sub)).TrimEnd('\\', '/');
                    if (!isData(data)) continue;
                    if (!found.Contains(data, StringComparer.OrdinalIgnoreCase)) found.Add(data);
                    return;
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { }
        }
        void Library(string library)
        {
            try
            {
                if (!Directory.Exists(library)) return;
                foreach (var name in names) Install(Path.Combine(library, name));
                foreach (var dir in Directory.EnumerateDirectories(library).OrderBy(d => d, StringComparer.OrdinalIgnoreCase)) Install(dir);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) { }
        }
        foreach (var dir in GogInstalls()) Install(dir);
        var roots = new List<string>();
        try { roots.AddRange(DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady).Select(d => d.RootDirectory.FullName)); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        foreach (var root in roots)
            foreach (var where in LibraryFolders.Where(w => !w.Contains("steamapps")))
                Library(Path.Combine(root, where));
        foreach (var library in SteamLibraries()) Library(Path.Combine(library, "steamapps", "common"));
        foreach (var root in roots)
            foreach (var where in LibraryFolders.Where(w => w.Contains("steamapps")))
                Library(Path.Combine(root, where));
        return found;
    }

    // GOG's installed games, from the registry (GOG.com\Games\<id>: path)
    private static IEnumerable<string> GogInstalls()
    {
        var dirs = new List<string>();
        if (!OperatingSystem.IsWindows()) return dirs;
        try
        {
            foreach (var key in new[] { @"SOFTWARE\WOW6432Node\GOG.com\Games", @"SOFTWARE\GOG.com\Games" })
            {
                using var games = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(key);
                if (games is null) continue;
                foreach (var id in games.GetSubKeyNames())
                {
                    using var game = games.OpenSubKey(id);
                    if (game?.GetValue("path") is string path && !string.IsNullOrWhiteSpace(path)) dirs.Add(path);
                }
            }
        }
        catch (Exception error) when (error is System.Security.SecurityException or UnauthorizedAccessException or IOException) { }
        return dirs;
    }

    // Steam's libraries (its own folder, and each "path" in steamapps\libraryfolders.vdf), Steam's folder from the registry
    private static IEnumerable<string> SteamLibraries()
    {
        var steam = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            try
            {
                if (Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")?.GetValue("SteamPath") is string user) steam.Add(user.Replace('/', '\\'));
                if (Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")?.GetValue("InstallPath") is string machine) steam.Add(machine);
            }
            catch (Exception error) when (error is System.Security.SecurityException or UnauthorizedAccessException or IOException) { }
        }
        // (the libraries' own spelling first: the registry's SteamPath is lower case)
        var libraries = new List<string>();
        foreach (var dir in steam.Where(Directory.Exists))
        {
            try
            {
                var vdf = Path.Combine(dir, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf)) continue;
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                    libraries.Add(m.Groups[1].Value.Replace("\\\\", "\\"));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
        libraries.AddRange(steam.Where(Directory.Exists));
        return libraries.Select(l => l.TrimEnd('\\', '/')).Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
