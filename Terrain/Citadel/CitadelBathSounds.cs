using System.IO;

namespace LBAAssembler.Terrain.Citadel;

// The bathroom's sounds (the user, 2026-11-20: "Let's make all the bathroom assets work in Twinsen's bathroom, the toilet to have a flushing
// trigger ... Let's have the shower turn on/off as well as the sink"), made here as the spanking's slap is (CitadelSpanking.Slap) and put at
// SAMPLES.HQR's end -- where one is already, that one: the toilet's flush, played once; the sink's tap, the shower and the bath's taps
// running, each a loop (a script's sample_always: the engine plays it over and over till its sample_stop -- GERELIFE.CPP LM_SAMPLE_ALWAYS --
// so its end runs into its start with no seam). 8-bit mono WAV at 22,050 Hz, as the game's own.
internal static class CitadelBathSounds
{
    private const int Rate = 22050;

    public sealed record Numbers(int Flush, int Tap, int Shower, int BathTap);

    public static Numbers Install(string gameDirectory) =>
        new(Put(gameDirectory, Flush()), Put(gameDirectory, Running(seed: 11, seconds: 1.6, low: 0.10, high: 0.55, level: 0.55)),
            Put(gameDirectory, Running(seed: 23, seconds: 1.8, low: 0.22, high: 0.85, level: 0.62, flutter: 0.18)),
            Put(gameDirectory, Running(seed: 37, seconds: 1.7, low: 0.07, high: 0.45, level: 0.6, flutter: 0.1)));

    // The flush: the cistern's rush -- a roar of water, low and full, rising at once and falling away over two seconds -- the bowl's gurgle
    // under its end, and the cistern filling again, a quiet hiss
    private static byte[] Flush()
    {
        var n = (int)(Rate * 3.2);
        var rnd = new Random(5);
        var s = new double[n];
        double lp1 = 0, lp2 = 0, slow = 0;
        for (var i = 0; i < n; i++)
        {
            var t = i / (double)Rate;
            var w = rnd.NextDouble() * 2 - 1;
            lp1 += 0.18 * (w - lp1);       // (a low pass: the water's roar)
            lp2 += 0.04 * (w - lp2);       // (a lower one: its body)
            slow += 0.002 * (w - slow);
            var rush = t < 0.08 ? t / 0.08 : t < 1.1 ? 1 : Math.Max(0, 1 - (t - 1.1) / 1.2);
            var roar = (lp1 * 1.6 + lp2 * 3.0) * rush;
            // (the gurgle: a low note, wobbling, in the rush's tail)
            var gurgle = t is > 1.0 and < 2.4 ? Math.Sin(2 * Math.PI * (150 + 60 * Math.Sin(2 * Math.PI * 7 * t)) * t) * 0.35 * Math.Sin(Math.PI * (t - 1.0) / 1.4) * (0.6 + 0.4 * Math.Sin(2 * Math.PI * 9 * t)) : 0;
            // (the cistern filling: a thin hiss, coming in as the rush goes)
            var refill = t > 1.6 ? (w - lp1) * 0.12 * Math.Min(1, (t - 1.6) / 0.4) * Math.Max(0, 1 - (t - 2.8) / 0.4) : 0;
            s[i] = roar + gurgle + refill + slow * 0.5;
        }
        return Wav(Normalised(s, 0.92));
    }

    // Water running from a tap or a shower's head: noise between two filters (`low`, `high`: the one-pole filters' steps -- higher, brighter),
    // a slow `flutter` in its loudness; the loop's last fifth faded into its first so it runs on with no seam
    private static byte[] Running(int seed, double seconds, double low, double high, double level, double flutter = 0.06)
    {
        var n = (int)(Rate * seconds);
        var fade = n / 5;
        var total = n + fade;
        var rnd = new Random(seed);
        var raw = new double[total];
        double a = 0, b = 0;
        for (var i = 0; i < total; i++)
        {
            var w = rnd.NextDouble() * 2 - 1;
            a += high * (w - a);           // (under the bright edge)
            b += low * (w - b);            // (less the low rumble: what's left, the water's hiss and splash)
            var t = i / (double)Rate;
            raw[i] = (a - b * 0.6) * (1 + flutter * Math.Sin(2 * Math.PI * 3.1 * t) + flutter * 0.6 * Math.Sin(2 * Math.PI * 5.3 * t + 1));
        }
        // (the loop: its first `fade` samples crossfaded with the run past its end)
        var s = new double[n];
        for (var i = 0; i < n; i++)
        {
            if (i < fade) { var k = i / (double)fade; s[i] = raw[i] * k + raw[n + i] * (1 - k); }
            else s[i] = raw[i];
        }
        return Wav(Normalised(s, level));
    }

    private static double[] Normalised(double[] s, double peak)
    {
        var max = s.Max(v => Math.Abs(v));
        if (max < 1e-9) return s;
        return s.Select(v => v / max * peak).ToArray();
    }

    private static byte[] Wav(double[] s)
    {
        var pcm = s.Select(v => (byte)Math.Round(128 + Math.Clamp(v, -1, 1) * 127)).ToArray();
        var wav = new MemoryStream();
        var w = new BinaryWriter(wav);
        w.Write("RIFF"u8); w.Write(36 + pcm.Length); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate); w.Write((short)1); w.Write((short)8);
        w.Write("data"u8); w.Write(pcm.Length); w.Write(pcm);
        return wav.ToArray();
    }

    // SAMPLES.HQR's number for `bytes`: one of its last entries already it, else appended
    private static int Put(string gameDirectory, byte[] bytes)
    {
        var path = Path.Combine(gameDirectory, "SAMPLES.HQR");
        var count = HqrArchive.CountEntries(path);
        var samples = HqrArchive.Open(path);
        // (the retail archive has empty slots among its last: passed over)
        for (var k = Math.Max(0, count - 32); k < count; k++)
            if (samples.IsValid(k) && samples.Read(k).AsSpan().SequenceEqual(bytes)) return k;
        HqrWriter.AppendToFile(path, bytes);
        return count;
    }
}
