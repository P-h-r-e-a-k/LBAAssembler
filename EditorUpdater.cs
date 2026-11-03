using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace LBAAssembler;

// LBA Assembler's updates from GitHub (the user, 2026-11-03: "Did we add auto update for LBAAssembler yet? If not then let's get this updating from GitHub"), as LBRA's are
// (LittleBigRacingAdventure's Installer/Updater.cs) but for one exe: the latest release's LBAAssembler.exe, known by the SHA-256 GitHub gives each asset. Another than the one
// running is downloaded beside it as LBAAssembler.new.exe and checked -- its digest, and a FileVersion newer than this one's (publishing stamps the time it was built,
// Properties/PublishProfiles/SingleFile.pubxml, so a build of the source is never "updated" to an older release) -- then, on the user's word, swapped in as the editor closes:
// the running exe renamed LBAAssembler.old.exe (Windows lets a running exe be renamed, not overwritten), the new one given its name and started. The old one goes at the next
// start. Only the single-file exe updates itself: a build run from the source is a DLL and a small launcher, with nothing to swap.
internal static class EditorUpdater
{
    public const string Api = "https://api.github.com/repos/P-h-r-e-a-k/LBAAssembler/releases/latest";
    public const string Asset = "LBAAssembler.exe";
    private const string NewName = "LBAAssembler.new.exe", PartName = "LBAAssembler.download", OldName = "LBAAssembler.old.exe";
    // (tests: a stand-in for GitHub's API, on this PC only -- anything else is ignored)
    private const string TestVariable = "LBA_ASSEMBLER_UPDATE_URL";

    // A newer release, downloaded: its tag, its page, its version, the file beside the editor, its SHA-256. NewFile null: there is one, but it couldn't be put beside the editor
    // (a folder the user can't write to) -- the page is all there is to offer.
    public sealed record Release(string Tag, string Page, Version Version, string? NewFile, string Digest);

#pragma warning disable IL3000 // (empty in a single-file app: that is the test)
    public static bool CanUpdate => string.IsNullOrEmpty(typeof(EditorUpdater).Assembly.Location) && Environment.ProcessPath is { } path && File.Exists(path);
#pragma warning restore IL3000

    public static Version Own => Environment.ProcessPath is { } path && File.Exists(path) && Parse(FileVersionInfo.GetVersionInfo(path).FileVersion) is { } v ? v : new Version(0, 0);

    private static Version? Parse(string? text) => Version.TryParse(text, out var v) ? v : null;

    private static string Folder => Path.GetDirectoryName(Environment.ProcessPath!)!;

    private static string Url() =>
        Environment.GetEnvironmentVariable(TestVariable) is { Length: > 0 } test && Uri.TryCreate(test, UriKind.Absolute, out var uri) && uri.IsLoopback ? test : Api;

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string? ownDigest;
    private static string OwnDigest() => ownDigest ??= Sha256(Environment.ProcessPath!);

    // At the start: what the last update left -- the editor it replaced, a download cut short.
    public static void CleanUp()
    {
        if (!CanUpdate) return;
        foreach (var name in new[] { OldName, PartName })
            try { if (File.Exists(Path.Combine(Folder, name))) { File.Delete(Path.Combine(Folder, name)); DebugLog.Log($"[update] {name} removed"); } }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    // GitHub's latest release against this editor: a newer one downloaded (Found), or why not (Said). `skipped`: a release the user said to skip (its digest), not offered again
    // unless asked (`manual`).
    public static async Task<(Release? Found, string Said)> Check(string skipped, bool manual)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("LBAAssembler-Updater/1");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        var url = Url();
        using var json = JsonDocument.Parse(await http.GetStringAsync(url));
        var root = json.RootElement;
        var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
        var page = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? "" : "";
        string? download = null, digest = null;
        if (root.TryGetProperty("assets", out var assets))
            foreach (var a in assets.EnumerateArray())
                if (a.TryGetProperty("name", out var n) && n.GetString() == Asset)
                {
                    download = a.TryGetProperty("browser_download_url", out var d) ? d.GetString() : null;
                    digest = a.TryGetProperty("digest", out var g) && g.GetString() is { } s && s.StartsWith("sha256:") ? s[7..].ToLowerInvariant() : null;
                }
        DebugLog.Log($"[update] {url}: release {tag}, {Asset} {(download is null ? "missing" : digest is null ? "with no digest" : digest[..12])}; this editor {Own}, {OwnDigest()[..12]}");
        if (download is null) return (null, $"The latest release ({tag}) has no {Asset}.");
        if (digest == OwnDigest()) return (null, $"LBA Assembler is up to date ({tag}).");
        if (!manual && digest is not null && digest == skipped) return (null, $"The latest release ({tag}) is one you chose to skip.");

        // (beside the editor, where the swap renames it: a folder the user can't write to has only the page to offer)
        var newFile = Path.Combine(Folder, NewName);
        var part = Path.Combine(Folder, PartName);
        if (!(File.Exists(newFile) && digest is not null && Sha256(newFile) == digest))
        {
            try
            {
                using (var response = await http.GetAsync(download, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    await using var file = File.Create(part);
                    await response.Content.CopyToAsync(file);
                }
                var got = Sha256(part);
                if (digest is not null && got != digest) { File.Delete(part); return (null, $"The download of {tag} didn't match its checksum: not used."); }
                if (got == OwnDigest()) { File.Delete(part); return (null, $"LBA Assembler is up to date ({tag})."); }
                File.Move(part, newFile, true);
                digest ??= got;
            }
            catch (UnauthorizedAccessException)
            {
                DebugLog.Log($"[update] {Folder} can't be written to: the release page offered instead");
                return (new Release(tag, page, new Version(0, 0), null, digest ?? ""), "");
            }
        }
        var version = Parse(FileVersionInfo.GetVersionInfo(newFile).FileVersion);
        if (version is null || version <= Own)
        {
            try { File.Delete(newFile); } catch (IOException) { }
            DebugLog.Log($"[update] {tag} is {version?.ToString() ?? "unversioned"}, not newer than {Own}: not offered");
            return (null, $"The latest release ({tag}) isn't newer than this LBA Assembler ({Own}).");
        }
        DebugLog.Log($"[update] {tag} ({version}) downloaded to {NewName}");
        return (new Release(tag, page, version, newFile, digest!), "");
    }

    // The swap, as the editor closes: null when the new one is in place, else what went wrong (the editor left as it was).
    public static string? Swap(Release release)
    {
        if (release.NewFile is null || !File.Exists(release.NewFile)) return "The downloaded update is gone.";
        var exe = Environment.ProcessPath!;
        var old = Path.Combine(Folder, OldName);
        try
        {
            if (File.Exists(old)) File.Delete(old);
            File.Move(exe, old);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return $"LBA Assembler couldn't be moved aside for the update: {e.Message}"; }
        try { File.Move(release.NewFile, exe); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            try { File.Move(old, exe); } catch (Exception) { }
            return $"The update couldn't take LBA Assembler's place: {e.Message}";
        }
        DebugLog.Log($"[update] {release.Tag} ({release.Version}) in place of {Own}; starting it");
        return null;
    }

    public static void Start() =>
        Process.Start(new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true, WorkingDirectory = Folder });

    public static void OpenPage(Release release)
    {
        try { Process.Start(new ProcessStartInfo(release.Page.Length > 0 ? release.Page : "https://github.com/P-h-r-e-a-k/LBAAssembler/releases/latest") { UseShellExecute = true }); }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { DebugLog.Log($"[update] the page didn't open: {e.Message}"); }
    }
}
