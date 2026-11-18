using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LBAAssembler.Terrain;

// The race mod's voiced lines, put into a built game (2026-11-18, the user: "embed finished voice lines once all are successfully
// generated"). The engine plays a text's own clip, VOICES\<EN|FR|DE|SP|IT|PO>_<GAM|000..012>_<text id>.ogg, before its VOX entry
// (MESSAGE.CPP SpeakClip). A new text's id is not known before the game is built -- the build numbers the mod's texts in order, so a
// line put in before others moves them, and LBRA's setup (with its own islands and tracks) numbers them otherwise than a story test
// build -- so the clips come keyed by what they say: voices.zip (LBRA's tools\pack_voices.py, from NewLines' Chatterbox recordings)
// holds <EN_000>/<key>.ogg, the key a hash of the line's words in that language (Key). Here every text of the built TEXT.HQR that is
// the mod's -- no retail text has that id in that file, or the retail one there says other words (so a retail line keeps its own
// recording even where a new line says the same words) -- takes the clip of its words, written under its id.
internal static class VoiceClips
{
    private static readonly string[] Prefixes = { "EN_", "FR_", "DE_", "SP_", "IT_", "PO_" };   // MESSAGE.CPP ListLanguage
    private const int FirstFile = 2, LastFile = 15;                                             // GAM, the twelve islands, Polar Island

    // MESSAGE.CPP ListFileText: the VOX file's name of a text file
    public static string Vox(int file) => file == 2 ? "GAM" : (file - 3).ToString("000");

    // A line's words as the game shows them -- code page 850, '@' a new paragraph -- as NewLines' .txt files have them
    // (ScriptRoundTrip NewLinesCommand.Words)
    public static string Words(byte[] bytes)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Regex.Replace(Encoding.GetEncoding(850).GetString(bytes), @"\s*@\s*", "\n").Trim();
    }

    // The words' key: the first 16 hex digits of the SHA-1 of their UTF-8, every space, line break and '@' left out (pack_voices.py
    // makes the same)
    public static string Key(string words)
    {
        var bare = Regex.Replace(words, @"[ \t\n\r\f\v @]+", "");
        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(bare)), 0, 8).ToLowerInvariant();
    }

    // The game folder's VOICES\ made again from the clips (its .ogg files emptied first: a clip of a line no longer in the game, or of
    // one whose id moved, must not stay to say the wrong words). retailText: TEXT.HQR as the CD has it. Returns the clips written.
    public static int Install(string game, byte[] retailText, Stream voicesZip, Action<string> log)
    {
        var dir = Path.Combine(game, "VOICES");
        if (Directory.Exists(dir))
            foreach (var old in Directory.GetFiles(dir, "*.ogg"))
                try { File.Delete(old); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { log($"{old} not removed: {e.Message}"); }

        using var zip = new ZipArchive(voicesZip, ZipArchiveMode.Read);
        var clips = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        foreach (var e in zip.Entries)
            if (e.Name.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)) clips[e.FullName] = e;
        if (clips.Count == 0) { log("voices: none in the archive"); return 0; }
        Directory.CreateDirectory(dir);

        var built = HqrArchive.Open(Path.Combine(game, "TEXT.HQR"));
        var retail = HqrArchive.FromBytes(retailText);
        int written = 0, mods = 0;
        var used = new HashSet<string>(StringComparer.Ordinal);
        for (var lang = 0; lang < Prefixes.Length; lang++)
            for (var file = FirstFile; file <= LastFile; file++)
            {
                var name = Prefixes[lang] + Vox(file);
                var old = Bank(retail, lang, file).GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.First().Bytes);
                foreach (var (id, bytes) in Bank(built, lang, file))
                {
                    if (bytes.Length == 0 || old.TryGetValue(id, out var was) && was.AsSpan().SequenceEqual(bytes)) continue;
                    mods++;
                    if (!clips.TryGetValue($"{name}/{Key(Words(bytes))}.ogg", out var clip)) continue;
                    using (var from = clip.Open())
                    using (var to = File.Create(Path.Combine(dir, $"{name}_{id}.ogg")))
                        from.CopyTo(to);
                    used.Add(clip.FullName);
                    written++;
                }
            }
        log($"voices: {written} of the mod's {mods} texts spoken (VOICES\\), {clips.Count - used.Count} of the archive's {clips.Count} clips not used");
        return written;
    }

    // A text file of TEXT.HQR, (id, characters) in its order (island 12's file, the engine's 15, is in entries after the retail ones)
    private static List<(int Id, byte[] Bytes)> Bank(HqrArchive text, int lang, int file)
    {
        var order = file < Lba2TextBank.FilesPerLanguage ? Lba2TextBank.IdsEntry(lang, file) : Polar.PolarScenes.TextEntry(lang);
        var bank = new List<(int, byte[])>();
        if (!text.IsValid(order) || !text.IsValid(order + 1)) return bank;
        var ids = text.Read(order);
        var data = text.Read(order + 1);
        for (var i = 0; i < ids.Length / 2 && i * 2 + 4 <= data.Length; i++)
        {
            int off0 = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(i * 2));
            int off1 = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(i * 2 + 2));
            var bytes = Array.Empty<byte>();
            if (off1 > off0 && off1 <= data.Length)
            {
                var end = off1;
                while (end > off0 + 1 && data[end - 1] == 0) end--;
                bytes = data[(off0 + 1)..end];
            }
            bank.Add((BinaryPrimitives.ReadUInt16LittleEndian(ids.AsSpan(i * 2)), bytes));
        }
        return bank;
    }
}
