using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// Gas monsters along a stretch of the lap over Zeelich's gas (RaceTrackPlan.GasMonsters; RACEMOD.CPP gasmonster=): Otringal's, at its docks
// and round the islets where Baldino's plane lies crashed among the rocks -- the user, 2026-10-09: "let's have gas monsters coming out of
// the gas and attempting to bite cars, any bitten cars should be stunned for a couple of seconds as though they've been hit then be able to
// continue"; then, of the serpents first made for them: "The gas monsters we've added are fake ... let's just reuse the retail version". The
// game's own gas monster (entity 222, body 320, its mouth open 322; it rises out of the gas in Celebration Island's and the Wannies' scenes),
// made bigger -- its bodies and the animations it rises, sways and strikes with, their moves that much longer, their steps along the ground
// left out (the race-track mode stands it) -- in two sizes, twice and three times its own (Scales), a size's bodies and animations the
// entity's BodyOf, OpenBodyOf and IdleOf (and the three after: strike, after striking, rise). Each monster the smallest that towers over the
// road where it bites (a deck higher than its head hid it from the chase camera). One actor each, out of sight until the race-track mode
// raises it beside the road, leans it over the road's near half and snaps its jaws there.
internal sealed class GasMonsterRun
{
    public int From { get; set; }
    public int To { get; set; }
    // one every so many cells along the stretch, where there is gas beside the road; how far out past the rail it rises (cells)
    public double Every { get; set; } = 9;
    public double Out { get; set; } = 3;
    public int Most { get; set; } = 5;
}

// A monster as placed: where it rises out of the gas, and where on the road it bites (island cells, the bite's height the road's); its size
// (RaceTrackGasMonster.Scales).
internal sealed record GasMonsterSpot(double X, double Z, double BiteX, double BiteZ, double BiteY, int Size);

internal static class RaceTrackGasMonster
{
    // the game's gas monster: its entity, its bodies (shut, mouth open) and the animations it sways (0), strikes (142), rears after
    // striking (211) and rises (499) with, its height; how much bigger it is made, and the entity's numbers for each size's bodies (shut,
    // open) and animations (sway, strike, after, rise: IdleOf and the three after it), each size's 4 on from the last's
    public const int Entity = 222, RetailBody = 320, RetailOpen = 322;
    public static readonly int[] RetailAnims = { 1481, 1485, 1482, 1480 };
    private const double RetailHeight = 2554;
    public static readonly double[] Scales = { 2.0, 3.0 };
    public static int BodyOf(int size) => 120 + 4 * size;
    public static int OpenBodyOf(int size) => 122 + 4 * size;
    public static int IdleOf(int size) => 600 + 4 * size;
    // how far across from the road's middle towards the monster it bites, as a share of the road's half width
    private const double BiteAcross = 0.45;
    private const double GasLevel = 60;
    // the least distance between two monsters (cells)
    private const double Apart = 9;
    // how far its head stands over the road where it bites, at the least: a twice-size one (5108 tall) by Otringal's docks, the road at 4450
    // there, was hidden from the chase camera by the road's own deck
    private const double OverRoad = 1800;

    // An animation `k` times as big: its bones' moves (translations) that much longer, its steps along the ground (which move the actor)
    // left out. Its header: frames, bones, loop frame; each frame its time and step, then each bone's type and three values.
    public static byte[] ScaledAnim(byte[] anim, double k)
    {
        var a = (byte[])anim.Clone();
        int U(int at) => a[at] | a[at + 1] << 8;
        short S(int at) => (short)U(at);
        void W(int at, int v) { v = Math.Clamp(v, short.MinValue, short.MaxValue); a[at] = (byte)v; a[at + 1] = (byte)(v >> 8); }
        int frames = U(0), bones = U(2);
        for (var f = 0; f < frames; f++)
        {
            var at = 8 + f * (8 + bones * 8);
            if (at + 8 + bones * 8 > a.Length) break;
            W(at + 2, 0); W(at + 4, 0); W(at + 6, 0);
            for (var b = 1; b < bones; b++)
            {
                var q = at + 8 + b * 8;
                if ((S(q) & 1) == 0) continue;
                for (var v = 1; v <= 3; v++) W(q + v * 2, (int)Math.Round(S(q + v * 2) * k));
            }
        }
        return a;
    }

    // Into the game folder: the gas monster in each of its sizes -- its bodies appended to BODY.HQR and its animations to ANIM.HQR, as its
    // entity's bodies BodyOf and OpenBodyOf and animations IdleOf and the three after. A line for the log.
    public static string Install(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var bodies = HqrArchive.Open(bodyPath);
        var clipped = 0;
        var firstBody = HqrArchive.CountEntries(bodyPath);
        var bytes = File.ReadAllBytes(bodyPath);
        foreach (var scale in Scales)
            foreach (var index in new[] { RetailBody, RetailOpen })
                bytes = HqrWriter.AppendEntry(bytes, HqrWriter.StoredEntry(IslandScaler.ScaledBody(bodies.Read(index), scale, ref clipped)));
        File.WriteAllBytes(bodyPath, bytes);
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var anims = HqrArchive.Open(animPath);
        var firstAnim = HqrArchive.CountEntries(animPath);
        var animBytes = File.ReadAllBytes(animPath);
        foreach (var scale in Scales)
            foreach (var index in RetailAnims)
                animBytes = HqrWriter.AppendEntry(animBytes, HqrWriter.StoredEntry(ScaledAnim(anims.Read(index), scale)));
        File.WriteAllBytes(animPath, animBytes);
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        for (var size = 0; size < Scales.Length; size++)
        {
            table = RaceTrackBaldinoCar.WithBody(table, Entity, BodyOf(size), firstBody + 2 * size);
            table = RaceTrackBaldinoCar.WithBody(table, Entity, OpenBodyOf(size), firstBody + 2 * size + 1);
            for (var i = 0; i < RetailAnims.Length; i++) table = RaceTrackJumpAnim.WithAnim(table, Entity, IdleOf(size) + i, firstAnim + RetailAnims.Length * size + i);
        }
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return $"the gas monster (entity {Entity}) {string.Join(" and ", Scales)} times its size: BODY.HQR entries {firstBody}-{firstBody + 2 * Scales.Length - 1} " +
               $"(its bodies {string.Join(", ", Enumerable.Range(0, Scales.Length).Select(z => $"{BodyOf(z)}/{OpenBodyOf(z)}"))}{(clipped > 0 ? $", {clipped} values clipped" : "")}), " +
               $"ANIM.HQR {firstAnim}-{firstAnim + RetailAnims.Length * Scales.Length - 1} (its animations from {string.Join(", ", Enumerable.Range(0, Scales.Length).Select(IdleOf))})";
    }

    // Where along the stretch From..To (the lap's points) monsters rise: every Every cells, out past the rail over the gas on one side or the
    // other (each side in turn where both have it), clear of the lap elsewhere and of what stands there, where the road is low enough for
    // it to tower over; at most Most.
    public static List<GasMonsterSpot> Place(IslandFile island, TrackRoad r, int from, int to, GasMonsterRun run, RaceTrackOptions o, RaceTrackReport report)
    {
        var placed = report.GasMonsters;
        var n = r.Count;
        var span = ((to - from) % n + n) % n + 1;
        var per = Math.Max(1, (int)Math.Round(run.Every / o.Spacing));
        var spots = new List<GasMonsterSpot>();
        var boxes = IslandOps.CubeCells(island).SelectMany(c => c.Item3.Decors.Select(d =>
            ((c.Item1 * (double)IslandFile.CubeSize + d.XMin) / 512, (c.Item2 * (double)IslandFile.CubeSize + d.ZMin) / 512,
             (c.Item1 * (double)IslandFile.CubeSize + d.XMax) / 512, (c.Item2 * (double)IslandFile.CubeSize + d.ZMax) / 512))).ToList();
        bool Gas(double x, double z)
        {
            for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                    if ((IslandOps.Altitude(island, (x + dx * 0.8) * 512, (z + dz * 0.8) * 512) ?? 0) > GasLevel) return false;
            return !boxes.Any(b => x > b.Item1 - 1 && x < b.Item3 + 1 && z > b.Item2 - 1 && z < b.Item4 + 1);
        }
        bool ClearOfLap(double x, double z, int k)
        {
            for (var i = 0; i < n; i++)
            {
                var apart = Math.Abs(i - k); apart = Math.Min(apart, n - apart);
                if (apart * o.Spacing < 6) continue;
                var half = r.RaisedHalfs?[i] ?? o.RaisedHalfWidth;
                if ((r.X[i] - x) * (r.X[i] - x) + (r.Z[i] - z) * (r.Z[i] - z) < (half + 2.5) * (half + 2.5)) return false;
            }
            return true;
        }
        var prefer = 1;
        for (var j = per / 2; j < span && spots.Count < run.Most; j += per)
        {
            var k = (from + j) % n;
            if (r.Gap[k]) continue;
            // (the smallest that towers over the road here)
            var size = Enumerable.Range(0, Scales.Length).FirstOrDefault(z => r.H[k] + OverRoad <= RetailHeight * Scales[z], -1);
            if (size < 0) continue;
            double tx = r.Tx[k], tz = r.Tz[k], ax = -tz, az = tx;
            var half = r.RaisedHalfs?[k] ?? o.RaisedHalfWidth;
            foreach (var side in new[] { prefer, -prefer })
            {
                double sx = r.X[k] + ax * side * (half + run.Out), sz = r.Z[k] + az * side * (half + run.Out);
                if (!Gas(sx, sz) || !ClearOfLap(sx, sz, k)) continue;
                // (and not near another: two side by side bit a car one after the other)
                if (spots.Concat(placed).Any(o => (o.X - sx) * (o.X - sx) + (o.Z - sz) * (o.Z - sz) < Apart * Apart)) continue;
                spots.Add(new GasMonsterSpot(sx, sz, r.X[k] + ax * side * half * BiteAcross, r.Z[k] + az * side * half * BiteAcross, r.H[k], size));
                prefer = -side;
                break;
            }
        }
        report.Notes.Add($"gas monsters along the lap's points {from}-{to}: {spots.Count} ({string.Join(", ", spots.Select(s => $"({s.X:0.0}, {s.Z:0.0}) {Scales[s.Size]}x"))})");
        return spots;
    }
}
