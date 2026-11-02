using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LBAAssembler;

// Help's Report a bug and Request a feature (the user, 2026-11-02: "bug reporting for LBAAssembler also, along with feature request features ... submitted to github in the same way
// we're logging bugs"): an issue on GitHub, as LBRA's Installer/BugReport.cs sends the game's, a bug with the editor's log, scrubbed: the page is public. The key is the game's, Issues
// on the LBRA repo at least (the build masks it in, MaskReportKey.ps1, from a file outside the repos): it goes to this repo if the key reaches it, else to the game's, marked as ours.
// For tests LBRA_REPORT_URL points the reports at http://localhost/... (nothing else is taken).
internal static class IssueReport
{
    private const string Own = "https://api.github.com/repos/P-h-r-e-a-k/LBAAssembler", Game = "https://api.github.com/repos/P-h-r-e-a-k/LittleBigRacingAdventure";
    private const string BugLabel = "auto-report", FeatureLabel = "feature-request", OursLabel = "lba-assembler", UrlVariable = "LBRA_REPORT_URL", SentFile = "reports_sent.txt";
    // (GitHub's limit is 65,536 characters a body)
    private const int ChunkMax = 60000, LogPieces = 3, DayMax = 3, TitleMax = 90;
    private static readonly byte[] Mask = Encoding.ASCII.GetBytes("LBRA-reports");

    private static string? Key()
    {
        var hex = typeof(IssueReport).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "ReportKey")?.Value;
        if (string.IsNullOrEmpty(hex)) return null;
        var bytes = Convert.FromHexString(hex);
        for (var i = 0; i < bytes.Length; i++) bytes[i] ^= Mask[i % Mask.Length];
        return Encoding.ASCII.GetString(bytes);
    }

    private static string? TestUrl()
    {
        var url = Environment.GetEnvironmentVariable(UrlVariable);
        return url is not null && url.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) ? url.TrimEnd('/') : null;
    }

    public static bool CanSend => Key() is not null || TestUrl() is not null;

    // Nothing in it that says who or where the user is: this goes on a public page
    public static string Scrub(string text)
    {
        text = Regex.Replace(text, @"(?<![A-Za-z])[A-Za-z]:[\\/](?:[^\\/\r\n:*?""<>|]+[\\/])*", "<folder>/");
        text = Regex.Replace(text, @"\\\\[^\\/\r\n]+[\\/]", "<folder>/");
        foreach (var name in new[] { Environment.UserName, Environment.MachineName }.Where(n => n.Length >= 2).OrderByDescending(n => n.Length))
            text = Regex.Replace(text, @"(?<![A-Za-z0-9])" + Regex.Escape(name) + @"(?![A-Za-z0-9])", "<name>", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?<![\d.])(?:\d{1,3}\.){3}\d{1,3}(?![\d.])", "<address>");
        return Regex.Replace(text, @"(?<![0-9A-Fa-f:])[0-9A-Fa-f]{1,4}(?::[0-9A-Fa-f]{0,4}){4,7}(?![0-9A-Fa-f:])", "<address>");
    }

    private static string Read(string path)
    {
        try
        {
            using var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var r = new StreamReader(f);
            return r.ReadToEnd();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return ""; }
    }

    // A log in pieces that fit a body or a comment, its end if it is longer than those: what happened last is what was asked about
    private static List<string> Pieces(string name, string text)
    {
        var room = ChunkMax * LogPieces;
        if (text.Length > room) text = "[... the start of the log left out ...]\n\n" + text[^(room - 60)..];
        var pieces = new List<string>();
        for (var at = 0; at < text.Length; at += ChunkMax) pieces.Add(text.Substring(at, Math.Min(ChunkMax, text.Length - at)));
        return pieces.Select((p, i) => $"<details><summary>{(pieces.Count > 1 ? $"{name}, part {i + 1} of {pieces.Count}" : name)}</summary>\n\n```text\n{p.Replace("```", "'''")}\n```\n</details>\n").ToList();
    }

    private static string SentPath => Path.Combine(Path.GetDirectoryName(DebugLog.LogFile) ?? AppContext.BaseDirectory, SentFile);

    private static bool TooMany()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return Read(SentPath).Split('\n').Count(l => l.Trim() == today) >= DayMax;
    }

    private static async Task<(HttpStatusCode Status, long Number)> Post(HttpClient http, string url, object body)
    {
        using var reply = await http.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        if (!reply.IsSuccessStatusCode) return (reply.StatusCode, 0);
        using var made = JsonDocument.Parse(await reply.Content.ReadAsStringAsync());
        return (reply.StatusCode, made.RootElement.TryGetProperty("number", out var n) ? n.GetInt64() : 0);
    }

    // What it says of how it went, for the window; null: it was sent
    public static async Task<string?> Send(bool feature, string words)
    {
        var key = Key();
        var test = TestUrl();
        if (key is null && test is null) return "This build of LBA Assembler can't send reports.";
        if (TooMany()) return $"That's {DayMax} from this PC today, which is as many as we take in a day. Thank you, and please try again tomorrow.";
        try
        {
            var said = Scrub(words.Trim());
            var line = said.Split('\n')[0].Trim();
            var title = (feature ? "[feature] " : "[bug] ") + (line.Length > TitleMax ? line[..TitleMax] : line);
            var built = Environment.ProcessPath is { } exe && File.Exists(exe) ? File.GetLastWriteTimeUtc(exe).ToString("yyyy-MM-dd HH:mm") : "unknown";
            var body = $"**LBA Assembler** built {built}  \n**Windows** {Environment.OSVersion.Version}\n\n> {said.Replace("\n", "\n> ")}\n\n";
            var pieces = feature ? new List<string>() : Pieces(Path.GetFileName(DebugLog.LogFile), Scrub(Read(DebugLog.LogFile)));
            if (!feature && Environment.GetEnvironmentVariable("LBA2_RENDERER_CRASHLOG") is { Length: > 0 } crash && Read(crash) is { Length: > 0 } fault)
                pieces.AddRange(Pieces("the native crash log", Scrub(fault)));
            var label = feature ? FeatureLabel : BugLabel;
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(40) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("LBAAssembler-Reports/1");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            if (key is not null) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
            var first = body + (pieces.Count > 0 ? pieces[0] : "");
            var api = test ?? Own;
            var (status, number) = await Post(http, $"{api}/issues", new { title, body = first, labels = new[] { label } });
            // (the key doesn't reach this repo: the game's, marked as ours)
            if (test is null && status is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            {
                api = Game;
                (status, number) = await Post(http, $"{api}/issues", new { title = "[LBA Assembler] " + title, body = first, labels = new[] { label, OursLabel } });
            }
            if ((int)status < 200 || (int)status >= 300) { DebugLog.Log($"IssueReport: GitHub said {(int)status}"); return $"GitHub didn't take it ({(int)status}). Please try again later."; }
            foreach (var piece in pieces.Skip(1))
                await Post(http, $"{api}/issues/{number}/comments", new { body = piece });
            DebugLog.Log($"IssueReport: sent as issue {number}");
            File.AppendAllText(SentPath, DateTime.UtcNow.ToString("yyyy-MM-dd") + "\n");
            return null;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException or JsonException)
        {
            DebugLog.Log($"IssueReport: {e.Message}");
            return "It couldn't be sent: is this PC online? Please try again later.";
        }
    }
}
