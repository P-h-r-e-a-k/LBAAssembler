using System.Buffers.Binary;
using System.IO;
using System.Text;
using LBAAssembler.Terrain.Temple;

namespace LBAAssembler.Terrain;

// The race items' sounds (the user, 2026-10-19: a sound for each item as it comes into Twinsen's item box, and for some as they are used).
// The program carries them -- the repo's sounds\PickUp\<name>.mp3 and sounds\OnUse\<name>.mp3, embedded by LBAAssembler.csproj and LBRA's
// installer under RaceSounds/ (a glob: a file dropped into either folder is in the next build) -- and the race track's build writes each
// into the game folder as the engine plays it: RACESOUNDS\PICKUP_<NAME>.WAV and RACESOUNDS\USE_<NAME>.WAV (the name upper-cased), a 16-bit
// mono 44.1 kHz WAV (the engine's mixer runs at 44.1 kHz and plays a stereo sample's left channel only: LIB386/AIL/SDL/SAMPLE.CPP), its
// silence at either end trimmed and its level brought to the game's own sound effects' (Loud, Peak).
//
// Which item plays which file is the engine's table (RACEMOD.CPP s_puSoundName), by those names: a file named there that isn't in the
// folder is silence, so a sound for an item that has none yet is a file of its name dropped into sounds\OnUse (gazogem, goldgazogem,
// yellowBall, greenBall, redBall, fireBall, fake, jetpack, ghost; penguin and oil are there) -- no code to change. (Without a file the
// magic balls play the game's own throw, SAMPLES.HQR's, which a file of theirs replaces; the super jet-pack hums the game's Proto-Pack
// sample as long as it lasts, a jetpack file played once beside it as it is used. The golden Gazogem without files of its own takes the
// Gazogem's.)
internal static class RaceSounds
{
    public const string Folder = "RACESOUNDS";
    public const string ResourcePrefix = "RaceSounds/";
    private const int Rate = 44100;
    // How loud each sound is: its loudest 100 ms brought to Loud (RMS, of full scale: -8 dB), its loudest sample no more than Peak (-1 dB).
    // The game's own effects, which the engine plays as they are, peak at full scale with their loudest 100 ms at -4 to -12 dB, -7.5 on
    // average (SAMPLES.HQR: the thunder 100 -9.7, the bonus 2 and 3 -7.0 and -6.7, the found object 6 -8.4, the magic ball's 8 -7.1:
    // ScriptRoundTrip racesounds levels); the race's sounds a little under them, with room for the car's engine and the music beside them.
    // (By the loudest 100 ms rather than the peak alone: a short click of a sound -- the oil's splat, the penguin's -- peaks well above its
    // loudness and would be quiet beside the rest.)
    public const double Loud = 0.398, Peak = 0.891;
    private const int LoudWindowMs = 100;
    // (quieter than this -- of full scale, about -50 dB -- counts as silence at the ends; a little is kept before the sound's start and
    // after its end, and its last few milliseconds faded out: no click where the cut is)
    private const double Silence = 0.003;
    private const int KeepBeforeMs = 5, KeepAfterMs = 30, FadeMs = 10;

    // The sounds the program carries: (kind, name, resource), kind "PICKUP" or "USE", name as its file has it
    public static List<(string Kind, string Name, string Resource)> Carried()
    {
        var list = new List<(string, string, string)>();
        foreach (var res in typeof(RaceSounds).Assembly.GetManifestResourceNames().Where(r => r.StartsWith(ResourcePrefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal))
        {
            var rel = res[ResourcePrefix.Length..].Replace('\\', '/');
            var slash = rel.IndexOf('/');
            if (slash < 0) continue;
            var kind = rel[..slash].ToLowerInvariant() switch { "pickup" => "PICKUP", "onuse" => "USE", _ => null };
            if (kind is null) continue;
            list.Add((kind, Path.GetFileNameWithoutExtension(rel[(slash + 1)..]), res));
        }
        return list;
    }

    // The file a sound is written to, as the engine looks for it (relative to the game folder)
    public static string FileName(string kind, string name) => $"{kind}_{name.ToUpperInvariant()}.WAV";

    // The sounds into the game folder (RACESOUNDS, made afresh: one the program no longer carries goes). Lines for the build's log.
    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var folder = Path.Combine(gameDirectory, Folder);
        if (Directory.Exists(folder)) foreach (var old in Directory.GetFiles(folder, "*.WAV")) File.Delete(old);
        var carried = Carried();
        if (carried.Count == 0) return log;
        Directory.CreateDirectory(folder);
        var made = new List<string>();
        foreach (var (kind, name, res) in carried)
        {
            try
            {
                var pcm = Read(res);
                if (pcm is null) { log.Add($"The race items' sounds: {res} isn't an MP3 or a WAV -- left out"); continue; }
                var (mono, note) = Prepare(pcm);
                if (mono.Length == 0) { log.Add($"The race items' sounds: {res} is silent -- left out"); continue; }
                WriteWav(Path.Combine(folder, FileName(kind, name)), mono);
                made.Add($"{FileName(kind, name)} ({mono.Length / 2 / (double)Rate:0.00} s{note})");
            }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or IndexOutOfRangeException or ArgumentException)
            {
                log.Add($"The race items' sounds: {res}: {error.Message} -- left out");
            }
        }
        log.Add($"The race items' sounds: {made.Count} into {Folder}\\ -- {string.Join(", ", made)}");
        return log;
    }

    // A carried sound's samples, as TempleMusic reads audio (16-bit stereo 44.1 kHz), or null
    private static byte[]? Read(string resource)
    {
        using var s = typeof(RaceSounds).Assembly.GetManifestResourceStream(resource)!;
        var bytes = new MemoryStream();
        s.CopyTo(bytes);
        bytes.Position = 0;
        var head = bytes.GetBuffer();
        if (bytes.Length >= 12 && Encoding.ASCII.GetString(head, 0, 4) == "RIFF" && Encoding.ASCII.GetString(head, 8, 4) == "WAVE") return TempleMusic.ReadWav(bytes);
        if (resource.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) return TempleMusic.DecodeMp3(bytes, resource);
        return null;
    }

    // 16-bit stereo samples as the engine's: mono (the two channels' mean), the silence at the ends trimmed, brought to Loud (or Peak). A
    // note for the log (how much it was raised or lowered)
    public static (byte[] Mono, string Note) Prepare(byte[] stereo)
    {
        var frames = stereo.Length / 4;
        var v = new double[frames];
        for (var i = 0; i < frames; i++)
            v[i] = (BinaryPrimitives.ReadInt16LittleEndian(stereo.AsSpan(i * 4)) + BinaryPrimitives.ReadInt16LittleEndian(stereo.AsSpan(i * 4 + 2))) / 65536.0;
        var peak = 0.0;
        foreach (var x in v) peak = Math.Max(peak, Math.Abs(x));
        if (peak <= 0) return (Array.Empty<byte>(), "");
        var floor = Math.Max(Silence, peak * 0.01);
        int first = 0, last = frames - 1;
        while (first < frames && Math.Abs(v[first]) < floor) first++;
        while (last > first && Math.Abs(v[last]) < floor) last--;
        if (first >= frames) return (Array.Empty<byte>(), "");
        first = Math.Max(0, first - KeepBeforeMs * Rate / 1000);
        last = Math.Min(frames - 1, last + KeepAfterMs * Rate / 1000);
        var count = last - first + 1;
        // (the loudest LoudWindowMs's RMS: its loudness as heard)
        var win = Math.Min(count, LoudWindowMs * Rate / 1000);
        double acc = 0, loudest = 0;
        for (var i = 0; i < count; i++)
        {
            acc += v[first + i] * v[first + i];
            if (i >= win) acc -= v[first + i - win] * v[first + i - win];
            if (i >= win - 1) loudest = Math.Max(loudest, acc / win);
        }
        loudest = Math.Sqrt(Math.Max(loudest, 0));
        var gain = Math.Min(Peak / peak, loudest > 0 ? Loud / loudest : double.MaxValue);
        var fade = Math.Min(count, FadeMs * Rate / 1000);
        var mono = new byte[count * 2];
        for (var i = 0; i < count; i++)
        {
            var g = gain * (i >= count - fade ? (count - i) / (double)fade : 1.0);
            BinaryPrimitives.WriteInt16LittleEndian(mono.AsSpan(i * 2), (short)Math.Clamp((int)Math.Round(v[first + i] * g * 32767), -32768, 32767));
        }
        var db = 20 * Math.Log10(gain);
        return (mono, $", {(db >= 0 ? "+" : "")}{db:0.0} dB");
    }

    // A 16-bit mono 44.1 kHz WAV
    public static void WriteWav(string path, byte[] mono)
    {
        using var f = File.Create(path);
        using var w = new BinaryWriter(f);
        w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + mono.Length); w.Write(Encoding.ASCII.GetBytes("WAVE"));
        w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
        w.Write(Encoding.ASCII.GetBytes("data")); w.Write(mono.Length); w.Write(mono);
    }
}
