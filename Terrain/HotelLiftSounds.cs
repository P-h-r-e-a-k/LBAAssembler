using System.IO;

namespace LBAAssembler.Terrain;

// The Imperial Hotel's lift (2026-11-23, the user: "Let's add an actual scene for the lift that Twinsen can walk inside, have the door close,
// we'll play some music for 5 seconds, then bing and the doors open again at the different floor"): its tune and its bell, made here as the
// bathroom's sounds are (Citadel/CitadelBathSounds) and put at SAMPLES.HQR's end -- where one is already, that one. 8-bit mono WAV at
// 22,050 Hz, as the game's own.
//   - the tune: five seconds of lift music -- a soft bossa nova, a Rhodes-like electric piano comping Cmaj7, A7, Dm7, G7 and home to
//     Cmaj7 on the bossa's off-beats, a walking bass under it, a vibraphone's melody over it, fading out on its last chord;
//   - the bell: the car's arrival -- one bright ding, a bell's partials dying away over a second and a half.
internal static class HotelLiftSounds
{
    private const int Rate = 22050;
    public const double TuneSeconds = 5.0;

    public sealed record Numbers(int Tune, int Bell);

    public static Numbers Install(string gameDirectory) => new(Put(gameDirectory, Tune()), Put(gameDirectory, Bell()));

    private static double Hz(int midi) => 440.0 * Math.Pow(2, (midi - 69) / 12.0);

    private static byte[] Tune()
    {
        const double Beat = 0.5;                 // (120 to the minute: ten beats, two bars and a half)
        var n = (int)(Rate * (TuneSeconds + 0.35));
        var s = new double[n];
        void Note(double at, double length, double hz, double level, Func<double, double, double> voice)
        {
            var i0 = (int)(at * Rate);
            var count = (int)((length + 0.6) * Rate);
            for (var k = 0; k < count && i0 + k < n; k++)
            {
                var t = k / (double)Rate;
                // (held for its length, then let go over 0.12 s)
                var release = t < length ? 1 : Math.Max(0, 1 - (t - length) / 0.12);
                if (release <= 0) break;
                s[i0 + k] += level * release * voice(t, hz);
            }
        }
        // a Rhodes: a sine and its octave, the tine's bell at the strike dying fast, the body slowly
        static double Rhodes(double t, double hz) =>
            (Math.Sin(2 * Math.PI * hz * t) + 0.25 * Math.Sin(4 * Math.PI * hz * t) * Math.Exp(-6 * t) + 0.15 * Math.Sin(2 * Math.PI * hz * 7 * t) * Math.Exp(-25 * t))
            * Math.Exp(-1.6 * t) * Math.Min(1, t / 0.004);
        // a vibraphone: a pure tone with its fourth partial, the motor's tremolo
        static double Vibes(double t, double hz) =>
            (Math.Sin(2 * Math.PI * hz * t) + 0.18 * Math.Sin(2 * Math.PI * hz * 4 * t) * Math.Exp(-8 * t)) * Math.Exp(-1.1 * t) * (0.8 + 0.2 * Math.Sin(2 * Math.PI * 5.5 * t)) * Math.Min(1, t / 0.003);
        // a soft upright bass
        static double Bass(double t, double hz) => (Math.Sin(2 * Math.PI * hz * t) + 0.3 * Math.Sin(4 * Math.PI * hz * t)) * Math.Exp(-2.2 * t) * Math.Min(1, t / 0.01);

        // the chords, two beats each: Cmaj7 A7 Dm7 G7 Cmaj7 (C4 = 60)
        var chords = new[]
        {
            (Root: 48, Notes: new[] { 64, 67, 71, 74 }),   // Cmaj7 (E G B D)
            (Root: 45, Notes: new[] { 61, 64, 67, 71 }),   // A7 (C# E G B)
            (Root: 50, Notes: new[] { 60, 65, 69, 72 }),   // Dm7 (C F A C)
            (Root: 43, Notes: new[] { 59, 62, 65, 69 }),   // G7 (B D F A)
            (Root: 48, Notes: new[] { 64, 67, 71, 76 }),   // Cmaj7
        };
        for (var c = 0; c < chords.Length; c++)
        {
            var at = c * 2 * Beat;
            var last = c == chords.Length - 1;
            // (the bossa's comp, in beats: on the beat, its "a", the "and" of two -- the last chord rung once and let ring; seconds held)
            foreach (var (off, len) in last ? new[] { (0.0, 1.4) } : new[] { (0.0, 0.3), (0.75, 0.2), (1.5, 0.45) })
                foreach (var note in chords[c].Notes) Note(at + off * Beat, len, Hz(note), 0.06, Rhodes);
            // (the bass: the root, its fifth)
            Note(at, 0.45, Hz(chords[c].Root), 0.22, Bass);
            if (!last) Note(at + Beat, 0.45, Hz(chords[c].Root + 7), 0.18, Bass);
        }
        // the melody over it (beat, length in beats, note)
        var tune = new (double At, double Len, int Note)[]
        {
            (0.0, 0.75, 76), (0.75, 0.25, 74), (1.0, 1.0, 72),
            (2.0, 0.75, 73), (2.75, 0.25, 76), (3.0, 1.0, 79),
            (4.0, 0.5, 77), (4.5, 0.5, 76), (5.0, 1.0, 74),
            (6.0, 0.5, 71), (6.5, 0.5, 74), (7.0, 1.0, 77),
            (8.0, 2.0, 76),
        };
        foreach (var (at, len, note) in tune) Note(at * Beat, len * Beat, Hz(note), 0.16, Vibes);

        // (the whole: faded out over its last 0.6 s, a little room in it, levelled)
        var echo = (int)(0.11 * Rate);
        for (var i = echo; i < n; i++) s[i] += 0.22 * s[i - echo];
        for (var i = 0; i < n; i++)
        {
            var t = i / (double)Rate;
            if (t > TuneSeconds - 0.6) s[i] *= Math.Max(0, (TuneSeconds + 0.3 - t) / 0.9);
        }
        var peak = s.Max(Math.Abs);
        if (peak > 0) for (var i = 0; i < n; i++) s[i] *= 0.85 / peak;
        return Wav(s);
    }

    // The bell: one ding at E6, a struck bell's partials (1, 2, 2.4, 3, 4.2 times its note), each dying at its own rate
    private static byte[] Bell()
    {
        var n = (int)(Rate * 1.6);
        var s = new double[n];
        var f = Hz(88);
        var partials = new (double Ratio, double Level, double Decay)[] { (1, 1, 2.4), (2, 0.45, 3.5), (2.4, 0.3, 5), (3, 0.22, 6), (4.2, 0.14, 9) };
        for (var i = 0; i < n; i++)
        {
            var t = i / (double)Rate;
            var v = 0.0;
            foreach (var (ratio, level, decay) in partials) v += level * Math.Sin(2 * Math.PI * f * ratio * t) * Math.Exp(-decay * t);
            s[i] = v * Math.Min(1, t / 0.002);
        }
        var peak = s.Max(Math.Abs);
        for (var i = 0; i < n; i++) s[i] *= 0.8 / peak;
        return Wav(s);
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
        for (var k = Math.Max(0, count - 32); k < count; k++)
            if (samples.IsValid(k) && samples.Read(k).AsSpan().SequenceEqual(bytes)) return k;
        HqrWriter.AppendToFile(path, bytes);
        return count;
    }
}
