using System.Buffers.Binary;
using System.IO;
using System.Text;
using LBAAssembler.Lba1;

namespace LBAAssembler.Terrain.Temple;

// The Temple of Bú's race in LBA1's own temple music (the user, 2026-10-18: "Our temple track currently uses the sound track from LBA2's Bu
// level, let's use the one from LBA1 instead"). LBA1's scenes 8 and 40 -- the temple, the crypt and the long corridor -- play its music 5
// (scene 41, the hall in front, its music 2: TempleScene says which scene's music is the race's). LBA1 plays music 1-9 off its CD's audio
// tracks, music N CD track N + 1 (the first track is the data: native/lba1-classic AMBIANCE.C PlayMusic, PlayCdTrack); the floppy release,
// and anything above 9, plays MIDI (MIDI_MI.HQR / MIDI_SB.HQR: a score, not a recording -- nothing here to play it with).
//
// The race track's build takes the CD track out of the player's own LBA1 -- GOG's install has LBA.DAT, a cue sheet, over the raw disc image
// LBA.GOG; a rip as a cue and BIN, WAV or OGG files does too, and so do loose Track06.wav / .ogg files -- trims the silence at its ends (the
// next track's two-second pregap: the race plays it again behind itself) and writes it into the game folder as a WAV, or copies an OGG, which
// the race file names (RaceTrackService.TrackInfo.Music; the engine's music=, RACEMOD.CPP). The music is never in the repo or an installer:
// it is made from the player's copy when the track is built. Without LBA1's audio (a floppy LBA1, a folder of its data with no disc image
// beside it) the race plays LBA2's Temple of Bù's music, as before (the scenes' own: TempleTrack).
//
// Where it looks: LBA1_MUSIC_DIR (an installer that copies LBA1's data elsewhere names the player's LBA1 folder here), then the LBA1 folder
// the build is made from and the two above it (a disc's data folder inside its install).
internal static class TempleMusic
{
    // the LBA1 scene whose music the race plays (the temple: 8 and 40 share it)
    public const int TempleScene = 8;
    // the file in the game folder (as the race file names it: the game folder's, RACEMOD.CPP music=)
    public const string Folder = "RACEMUSIC";
    public const string Stem = "LBA1_TEMPLE";
    public const string EnvDirectory = "LBA1_MUSIC_DIR";
    private const int Sector = 2352, SectorsPerSecond = 75, Rate = 44100, Channels = 2;
    // (a sample this quiet or quieter counts as silence at the track's ends: dither, the pregap)
    private const int Silence = 24;

    // The race's music, as the race file names it: the file a build put in the game folder (the WAV, or the OGG), or null.
    public static string? Installed(string gameDirectory)
    {
        foreach (var ext in new[] { ".WAV", ".OGG" })
            if (File.Exists(Path.Combine(gameDirectory, Folder, Stem + ext))) return $"{Folder}/{Stem}{ext}";
        return null;
    }

    // LBA1's music number for the temple (its scene's), from the LBA1 folder: 5 on every LBA1 we know of.
    public static int MusicNumber(string lba1Directory)
    {
        try { return Lba1Scene.Parse(TempleScene, HqrArchive.Open(Path.Combine(lba1Directory, "SCENE.HQR")).Read(TempleScene)).Music; }
        catch { return 5; }
    }

    // The music into the game folder from the player's LBA1. Returns lines for the build's log.
    public static List<string> Install(string gameDirectory, string lba1Directory)
    {
        var log = new List<string>();
        var music = MusicNumber(lba1Directory);
        if (music is < 1 or > 9)
        {
            log.Add($"The race's music: LBA1's temple plays music {music}, which LBA1 has only as MIDI -- the race plays LBA2's Temple of Bù's music");
            return log;
        }
        var track = music + 1;
        var found = Find(Places(lba1Directory), track);
        var folder = Path.Combine(gameDirectory, Folder);
        if (found is null)
        {
            var kept = Installed(gameDirectory);
            log.Add($"The race's music: LBA1's music {music} (CD track {track}, the temple's) isn't with this LBA1 -- no CD image with its audio tracks, nor its track as a file, " +
                    $"in {string.Join(", ", Places(lba1Directory))} ({EnvDirectory} names another folder to look in): " +
                    (kept is not null ? $"the {kept} an earlier build made plays" : "the race plays LBA2's Temple of Bù's music"));
            return log;
        }
        Directory.CreateDirectory(folder);
        var (source, pcm, ogg, lead) = found.Value;
        string written;
        if (ogg is not null)
        {
            written = Path.Combine(folder, Stem + ".OGG");
            File.Copy(ogg, written, true);
            Delete(Path.Combine(folder, Stem + ".WAV"));
            log.Add($"The race's music: LBA1's music {music} (the temple's, its scenes {TempleScene} and 40), CD track {track} -- {source} -- copied as {Folder}\\{Stem}.OGG");
            return log;
        }
        var (trimmed, note) = Cut(pcm!, lead);
        written = Path.Combine(folder, Stem + ".WAV");
        WriteWav(written, trimmed);
        Delete(Path.Combine(folder, Stem + ".OGG"));
        log.Add($"The race's music: LBA1's music {music} (the temple's, its scenes {TempleScene} and 40), CD track {track} -- {source} -- " +
                $"{Minutes(trimmed.Length / (Channels * 2) / (double)Rate)} ({note}): {Folder}\\{Stem}.WAV ({trimmed.Length / 1048576.0:0.0} MB)");
        return log;
    }

    // Where to look for LBA1's audio, each folder once
    public static List<string> Places(string lba1Directory)
    {
        var places = new List<string>();
        void Add(string? dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return;
            try { dir = Path.GetFullPath(dir).TrimEnd('\\', '/'); } catch { return; }
            if (Directory.Exists(dir) && !places.Contains(dir, StringComparer.OrdinalIgnoreCase)) places.Add(dir);
        }
        Add(Environment.GetEnvironmentVariable(EnvDirectory));
        Add(lba1Directory);
        if (!string.IsNullOrWhiteSpace(lba1Directory) && Directory.Exists(lba1Directory))
        {
            var up = Directory.GetParent(Path.GetFullPath(lba1Directory).TrimEnd('\\', '/'));
            Add(up?.FullName);
            Add(up?.Parent?.FullName);
        }
        return places;
    }

    // CD track `track` in the first of `places` that has it: where it came from, and its PCM (16-bit stereo 44.1 kHz, little-endian) with
    // the `Lead` frames before the cue's start of it (Cut finds the track's own ends in them) -- or an OGG file to copy as it is
    public static (string Source, byte[]? Pcm, string? Ogg, int Lead)? Find(IEnumerable<string> places, int track)
    {
        foreach (var dir in places)
        {
            // a cue sheet: GOG's LBA.DAT, or any *.cue
            foreach (var sheet in Directory.EnumerateFiles(dir).Where(f => Path.GetExtension(f).ToUpperInvariant() is ".CUE" or ".DAT").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                try { if (FromCue(sheet, track) is { } got) return got; }
                catch (Exception error) { DebugLog.Log($"TempleMusic: {sheet}: {error.Message}"); }
            }
            // a track as a file of its own (Track06.wav, track6.ogg ...), beside the data or in a music folder
            foreach (var where in new[] { dir, Path.Combine(dir, "MUSIC") }.Where(Directory.Exists))
                foreach (var name in new[] { $"Track{track:00}", $"Track{track}", $"LBA1_{track:00}" })
                {
                    var wav = Path.Combine(where, name + ".wav");
                    if (File.Exists(wav) && ReadWav(wav) is { } pcm) return (wav, pcm, null, 0);
                    var ogg = Path.Combine(where, name + ".ogg");
                    if (File.Exists(ogg) && IsOgg(ogg)) return (ogg, null, ogg, 0);
                }
        }
        return null;
    }

    private sealed record CueTrack(int Number, bool Audio, string File, string FileType, int Index0, int Index1);

    private static List<CueTrack>? ParseCue(string sheet)
    {
        if (new FileInfo(sheet).Length > 65536) return null;
        var text = File.ReadAllText(sheet, Encoding.Latin1);
        if (!text.Contains("TRACK", StringComparison.OrdinalIgnoreCase) || !text.Contains("FILE", StringComparison.OrdinalIgnoreCase)) return null;
        var tracks = new List<CueTrack>();
        string file = "", type = "";
        CueTrack? current = null;
        static int Frames(string mmssff)
        {
            var p = mmssff.Split(':');
            return p.Length == 3 && int.TryParse(p[0], out var m) && int.TryParse(p[1], out var s) && int.TryParse(p[2], out var f) ? (m * 60 + s) * SectorsPerSecond + f : -1;
        }
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("FILE ", StringComparison.OrdinalIgnoreCase))
            {
                if (current is not null) { tracks.Add(current); current = null; }
                var q0 = line.IndexOf('"'); var q1 = q0 >= 0 ? line.IndexOf('"', q0 + 1) : -1;
                if (q0 >= 0 && q1 > q0) { file = line[(q0 + 1)..q1]; type = line[(q1 + 1)..].Trim().ToUpperInvariant(); }
                else { var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries); file = parts.Length > 1 ? parts[1] : ""; type = parts.Length > 2 ? parts[^1].ToUpperInvariant() : ""; }
            }
            else if (line.StartsWith("TRACK ", StringComparison.OrdinalIgnoreCase))
            {
                if (current is not null) tracks.Add(current);
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                current = new CueTrack(parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : -1, parts.Length > 2 && parts[2].Equals("AUDIO", StringComparison.OrdinalIgnoreCase), file, type, -1, -1);
            }
            else if (line.StartsWith("INDEX ", StringComparison.OrdinalIgnoreCase) && current is not null)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && int.TryParse(parts[1], out var which))
                    current = which == 0 ? current with { Index0 = Frames(parts[2]) } : which == 1 ? current with { Index1 = Frames(parts[2]) } : current;
            }
        }
        if (current is not null) tracks.Add(current);
        return tracks.Count > 0 ? tracks : null;
    }

    private static (string, byte[]?, string?, int)? FromCue(string sheet, int number)
    {
        var tracks = ParseCue(sheet);
        var t = tracks?.FirstOrDefault(x => x.Number == number);
        if (tracks is null || t is null || !t.Audio || t.Index1 < 0) return null;
        var dir = Path.GetDirectoryName(sheet)!;
        var path = Path.Combine(dir, t.File);
        if (!File.Exists(path)) return null;
        // (it ends where the next track in its file starts: its pregap, else its first sector; or at the file's end)
        var next = tracks.SkipWhile(x => x != t).Skip(1).FirstOrDefault(x => x.File == t.File);
        var end = next is null ? -1 : next.Index0 >= 0 ? next.Index0 : next.Index1;
        // ... and it is read from a little before the cue says it starts, when the track before it in its file is music too: GOG's LBA.DAT
        // lists no pregaps, and every track of its image starts 2 s before its INDEX 01 (Cut finds where)
        var previous = tracks.TakeWhile(x => x != t).LastOrDefault(x => x.File == t.File);
        var start = previous is { Audio: true, Index1: >= 0 } ? Math.Max(previous.Index1, Math.Max(0, t.Index1 - LeadSeconds * SectorsPerSecond)) : t.Index1;
        var source = $"{Path.GetFileName(sheet)} over {Path.GetFileName(path)}";
        if (t.FileType == "BINARY")
        {
            using var f = File.OpenRead(path);
            var sectors = (int)(f.Length / Sector);
            var last = end >= 0 ? Math.Min(end, sectors) : sectors;
            if (last <= t.Index1) return null;
            var pcm = new byte[(long)(last - start) * Sector];
            f.Position = (long)start * Sector;
            f.ReadExactly(pcm);
            // (Red Book's samples are big-endian on the disc and a ripper is meant to swap them; not every one does: the order is measured)
            if (BigEndian(pcm)) Swap(pcm);
            return (source, pcm, null, (t.Index1 - start) * Sector / 4);
        }
        if (IsWav(path) && ReadWav(path) is { } whole)
        {
            var from = Math.Min(whole.Length, (long)start * Sector);
            var to = end >= 0 ? Math.Min(whole.Length, (long)end * Sector) : whole.Length;
            return to > from ? (source, whole[(int)from..(int)to], null, (int)(Math.Min(whole.Length, (long)t.Index1 * Sector) - from) / 4) : null;
        }
        // (an OGG -- GOG's cue sheets call theirs MP3 -- only when the track is the whole file)
        if (IsOgg(path) && t.Index1 == 0 && next is null) return (source, null, path, 0);
        return null;
    }

    // (how far before a track's INDEX 01 it is read from; a run of silence this long or longer is a gap between tracks)
    private const int LeadSeconds = 4;
    private const double GapSeconds = 1.0;

    // A track read with `lead` frames before its cue's start: from the end of the last gap in those (its pregap: the music starts after
    // it) to the start of the last gap in its last LeadSeconds (the next track's pregap, with the next track's start after it when the cue
    // is off), its silent ends trimmed. Returns the samples and what was cut, said.
    public static (byte[] Pcm, string Note) Cut(byte[] pcm, int lead)
    {
        var frames = pcm.Length / 4;
        var gap = (int)(GapSeconds * Rate);
        bool Quiet(int frame) => Math.Abs((short)(pcm[4 * frame] | pcm[4 * frame + 1] << 8)) <= Silence && Math.Abs((short)(pcm[4 * frame + 2] | pcm[4 * frame + 3] << 8)) <= Silence;
        // the runs of silence (first frame, frames) in [from, to)
        List<(int At, int Length)> Gaps(int from, int to)
        {
            var runs = new List<(int, int)>();
            var run = 0;
            for (var f = Math.Max(0, from); f < Math.Min(frames, to); f++)
            {
                if (Quiet(f)) { run++; continue; }
                if (run >= gap) runs.Add((f - run, run));
                run = 0;
            }
            if (run >= gap) runs.Add((Math.Min(frames, to) - run, run));
            return runs;
        }
        var first = lead;
        var head = Gaps(0, lead + 1).LastOrDefault();
        if (head.Length > 0) first = head.At + head.Length;
        var tailFrom = Math.Max(first, frames - LeadSeconds * Rate);
        var last = frames;
        var tail = Gaps(tailFrom, frames).LastOrDefault();
        if (tail.Length > 0) last = tail.At;
        var cut = Trim(pcm[(first * 4)..(last * 4)]);
        var before = (lead - first) / (double)Rate;
        var note = $"{(before > 0.01 ? $"its music starts {before:0.00} s before the cue's INDEX 01 -- its pregap isn't in the cue sheet; " : before < -0.01 ? $"{-before:0.00} s of silence after the cue's INDEX 01 left out; " : "")}" +
                   $"{(last < frames ? $"{(frames - last) / (double)Rate:0.00} s after its end left out (the gap{(tail.At + tail.Length < frames ? " and the next track's start" : "")}); " : "")}" +
                   $"silent ends trimmed: {(pcm.Length - cut.Length) / 4 / (double)Rate:0.00} s off in all";
        return (cut, note);
    }

    private static bool IsOgg(string path)
    {
        using var f = File.OpenRead(path);
        var magic = new byte[4];
        return f.Read(magic, 0, 4) == 4 && magic[0] == 'O' && magic[1] == 'g' && magic[2] == 'g' && magic[3] == 'S';
    }

    private static bool IsWav(string path)
    {
        using var f = File.OpenRead(path);
        var magic = new byte[12];
        return f.Read(magic, 0, 12) == 12 && Encoding.ASCII.GetString(magic, 0, 4) == "RIFF" && Encoding.ASCII.GetString(magic, 8, 4) == "WAVE";
    }

    // A WAV file's samples, when it is 16-bit stereo 44.1 kHz PCM (the CD's own format), else null
    private static byte[]? ReadWav(string path)
    {
        var d = File.ReadAllBytes(path);
        if (d.Length < 12 || Encoding.ASCII.GetString(d, 0, 4) != "RIFF" || Encoding.ASCII.GetString(d, 8, 4) != "WAVE") return null;
        var p = 12;
        var ok = false;
        while (p + 8 <= d.Length)
        {
            var id = Encoding.ASCII.GetString(d, p, 4);
            var size = BinaryPrimitives.ReadInt32LittleEndian(d.AsSpan(p + 4));
            if (size < 0) return null;
            if (id == "fmt " && p + 8 + 16 <= d.Length)
            {
                var span = d.AsSpan(p + 8);
                ok = BinaryPrimitives.ReadUInt16LittleEndian(span) == 1 && BinaryPrimitives.ReadUInt16LittleEndian(span[2..]) == Channels &&
                     BinaryPrimitives.ReadInt32LittleEndian(span[4..]) == Rate && BinaryPrimitives.ReadUInt16LittleEndian(span[14..]) == 16;
            }
            else if (id == "data") return ok ? d.AsSpan(p + 8, Math.Min(size, d.Length - p - 8) & ~3).ToArray() : null;
            p += 8 + size + (size & 1);
        }
        return null;
    }

    // Whether 16-bit samples read smoother big-endian than little-endian (music changes little from one sample to the next; the wrong
    // order makes noise of it)
    private static bool BigEndian(byte[] pcm)
    {
        long le = 0, be = 0;
        var n = Math.Min(pcm.Length / 2, 2_000_000);
        for (var i = 2; i < n; i++)
        {
            int a = (short)(pcm[2 * i] | pcm[2 * i + 1] << 8), b = (short)(pcm[2 * i - 4] | pcm[2 * i - 3] << 8);
            int c = (short)(pcm[2 * i] << 8 | pcm[2 * i + 1]), e = (short)(pcm[2 * i - 4] << 8 | pcm[2 * i - 3]);
            le += Math.Abs(a - b); be += Math.Abs(c - e);
        }
        return be < le;
    }

    private static void Swap(byte[] pcm)
    {
        for (var i = 0; i + 1 < pcm.Length; i += 2) (pcm[i], pcm[i + 1]) = (pcm[i + 1], pcm[i]);
    }

    // The samples without the silence at their ends (whole stereo frames)
    public static byte[] Trim(byte[] pcm)
    {
        var frames = pcm.Length / 4;
        bool Loud(int frame) => Math.Abs((short)(pcm[4 * frame] | pcm[4 * frame + 1] << 8)) > Silence || Math.Abs((short)(pcm[4 * frame + 2] | pcm[4 * frame + 3] << 8)) > Silence;
        var first = 0;
        while (first < frames && !Loud(first)) first++;
        var last = frames - 1;
        while (last > first && !Loud(last)) last--;
        return last <= first ? pcm : pcm[(first * 4)..((last + 1) * 4)];
    }

    public static void WriteWav(string path, byte[] pcm)
    {
        var header = new byte[44];
        void Text(int at, string s) => Encoding.ASCII.GetBytes(s).CopyTo(header, at);
        Text(0, "RIFF"); BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4), 36 + pcm.Length); Text(8, "WAVE");
        Text(12, "fmt "); BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(16), 16);
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(20), 1); BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(22), Channels);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(24), Rate); BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(28), Rate * Channels * 2);
        BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(32), Channels * 2); BinaryPrimitives.WriteInt16LittleEndian(header.AsSpan(34), 16);
        Text(36, "data"); BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(40), pcm.Length);
        using var f = File.Create(path);
        f.Write(header);
        f.Write(pcm);
    }

    private static void Delete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static string Minutes(double seconds) => $"{(int)(seconds / 60)}:{seconds % 60:00.0}";
}
