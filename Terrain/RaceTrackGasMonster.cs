using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// Gas monsters along a stretch of the lap over Zeelich's gas (RaceTrackPlan.GasMonsters; RACEMOD.CPP gasmonster=): Otringal's, at its docks
// and round the islets where Baldino's plane lies crashed among the rocks -- the user, 2026-10-09: "let's have gas monsters coming out of
// the gas and attempting to bite cars, any bitten cars should be stunned for a couple of seconds as though they've been hit then be able to
// continue"; then, of the serpents first made for them: "The gas monsters we've added are fake ... let's just reuse the retail version";
// then: "Some of the gas monsters strike too low and appear through the track, let's have them permanently visible with their necks above
// the track and randomly striking down. Let's have each gas monster always attack the same point in the track, so once their positions are
// learnt they're avoidable". The game's own gas monster (entity 222, body 320, its mouth open 322; it rises out of the gas in Celebration
// Island's and the Wannies' scenes), made bigger -- twice or three times its own size (Scales), a size's bodies the entity's BodyOf and
// OpenBodyOf -- standing out of the gas beside the road for good, its neck arched over the rail (GasMonsterNeck), its head poised over the
// road's near half, swaying; now and then it rears back and strikes its head down onto its own place on the road, its jaws snapping shut
// there. Each monster's moves are its own animations, its neck bent bone by bone to fit the road beside it (InstallPoses). One actor each.
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
// (RaceTrackGasMonster.Scales); how far towards its bite the road's rail is (cells); once its moves are made (InstallPoses), its foot's
// height (under the gas) and the first of its animations.
internal sealed record GasMonsterSpot(double X, double Z, double BiteX, double BiteZ, double BiteY, int Size, double Edge, double FootY = 0, int FirstAnim = -1);

internal static class RaceTrackGasMonster
{
    // the game's gas monster: its entity, its bodies (shut, mouth open), its height; how much bigger it is made, and the entity's numbers
    // for each size's bodies (shut, open), each size's 4 on from the last's
    public const int Entity = 222, RetailBody = 320, RetailOpen = 322;
    private const double RetailHeight = 2554;
    public static readonly double[] Scales = { 2.0, 3.0 };
    public static int BodyOf(int size) => 120 + 4 * size;
    public static int OpenBodyOf(int size) => 122 + 4 * size;
    // each monster's animations (the entity's, from FirstPoseAnim on, 4 apart): swaying poised, rearing and striking (its mouth open), and
    // biting and back (shut); their times (ms, RACEMOD.CPP's RACE_MONSTER_*): into the strike's first pose, rearing, the strike, the bite
    // held, back to poised, and a sway either way; how far it sways (radians)
    private const int FirstPoseAnim = 610;
    public const int PoiseMs = 150, RearMs = 650, LungeMs = 220, HoldMs = 450, BackMs = 700, SwayMs = 700;
    private const double Sway = 0.12;
    // how far across from the road's middle towards the monster it bites, as a share of the road's half width
    private const double BiteAcross = 0.45;
    private const double GasLevel = 60;
    // the least distance between two monsters (cells)
    private const double Apart = 9;
    // how far its head stands over the road where it bites, at the least: a twice-size one (5108 tall) by Otringal's docks, the road at 4450
    // there, was hidden from the chase camera by the road's own deck
    private const double OverRoad = 1800;

    // Into the game folder: the gas monster in each of its sizes -- its bodies appended to BODY.HQR, as its entity's bodies BodyOf and
    // OpenBodyOf. A line for the log.
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
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        for (var size = 0; size < Scales.Length; size++)
        {
            table = RaceTrackBaldinoCar.WithBody(table, Entity, BodyOf(size), firstBody + 2 * size);
            table = RaceTrackBaldinoCar.WithBody(table, Entity, OpenBodyOf(size), firstBody + 2 * size + 1);
        }
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return $"the gas monster (entity {Entity}) {string.Join(" and ", Scales)} times its size: BODY.HQR entries {firstBody}-{firstBody + 2 * Scales.Length - 1} " +
               $"(its bodies {string.Join(", ", Enumerable.Range(0, Scales.Length).Select(z => $"{BodyOf(z)}/{OpenBodyOf(z)}"))}{(clipped > 0 ? $", {clipped} values clipped" : "")})";
    }

    // Into the game folder: each monster's moves (report.GasMonsters, which get their size, foot and first animation) -- its poses against
    // the road beside it (GasMonsterPoser: the smallest size whose struck head comes down on its bite with its neck over the rail and its
    // poised head well over the road), as three animations appended to ANIM.HQR and given to the entity from FirstPoseAnim on: swaying
    // poised (mouth open), rearing and striking (open), biting and back to poised (shut). Lines for the log.
    public static List<string> InstallPoses(string gameDirectory, RaceTrackReport report)
    {
        var log = new List<string>();
        if (report.GasMonsters.Count == 0) return log;
        var bodies = HqrArchive.Open(Path.Combine(gameDirectory, "BODY.HQR"));
        var clipped = 0;
        GasMonsterNeck Neck(int retail, double scale) => new(Body.Read(IslandScaler.ScaledBody(bodies.Read(retail), scale, ref clipped), 2, allowStatic: true));
        var necks = Scales.Select(scale => (Shut: Neck(RetailBody, scale), Open: Neck(RetailOpen, scale))).ToArray();
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var anims = File.ReadAllBytes(animPath);
        var index = HqrArchive.CountEntries(animPath);
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        var gen = FirstFreeAnim(table, Entity, FirstPoseAnim);
        for (var i = 0; i < report.GasMonsters.Count; i++)
        {
            var g = report.GasMonsters[i];
            double d = Math.Sqrt((g.BiteX - g.X) * (g.BiteX - g.X) + (g.BiteZ - g.Z) * (g.BiteZ - g.Z)) * 512, e = g.Edge * 512;
            GasMonsterPoser.Poses poses = null!;
            var size = g.Size;
            for (; size < Scales.Length; size++)
            {
                var k = Scales[size] / Scales[0];
                poses = GasMonsterPoser.Solve(necks[size].Shut, necks[size].Open, d, e, g.BiteY, k);
                if (poses.StruckMiss < 200 && poses.ReadyHeadOver >= 600 * k && poses.LeastOverRail >= 0 && poses.LeastOverDeck >= 0) break;
            }
            var fits = size < Scales.Length;
            size = Math.Min(size, Scales.Length - 1);
            var (shut, open) = necks[size];
            var (ready, rear, struck) = (poses.Ready, poses.Rear, poses.Struck);
            var idle = new Anim { Game = 2, LoopFrame = 0 };
            idle.Frames.AddRange(new[] { open.Frame(ready, SwayMs), open.Frame(ready with { Nod = ready.Nod + 0.1 }, SwayMs, Sway),
                                         open.Frame(ready, SwayMs), open.Frame(ready with { Nod = ready.Nod - 0.06 }, SwayMs, -Sway) });
            var strike = new Anim { Game = 2, LoopFrame = 2 };
            strike.Frames.AddRange(new[] { open.Frame(ready, PoiseMs), open.Frame(rear, RearMs), open.Frame(struck, LungeMs) });
            var bite = new Anim { Game = 2, LoopFrame = 2 };
            bite.Frames.AddRange(new[] { shut.Frame(struck, 20), shut.Frame(struck, HoldMs), shut.Frame(ready, BackMs) });
            foreach (var (anim, n) in new[] { (idle, 0), (strike, 1), (bite, 2) })
            {
                anims = HqrWriter.AppendEntry(anims, HqrWriter.StoredEntry(anim.Write()));
                table = RaceTrackJumpAnim.WithAnim(table, Entity, gen + n, index++);
            }
            // (for a look from the side: each pose's points from the foot, and the road beside it -- RT_MONSTER_POSES=<folder>)
            if (Environment.GetEnvironmentVariable("RT_MONSTER_POSES") is { Length: > 0 } dump)
            {
                Directory.CreateDirectory(dump);
                using var w = new StreamWriter(Path.Combine(dump, $"monster{i}.csv"));
                w.WriteLine($"# foot {poses.FootY:0} road {g.BiteY:0} rail {e:0} bite {d:0}");
                w.WriteLine("pose,z,y");
                foreach (var (name, shape, neck) in new[] { ("ready", ready, open), ("rear", rear, open), ("struck", struck, open), ("bit", struck, shut) })
                    foreach (var q in neck.Pose(shape)) w.WriteLine($"{name},{q.Z:0},{poses.FootY + q.Y:0}");
            }
            report.GasMonsters[i] = g with { Size = size, FootY = poses.FootY, FirstAnim = gen };
            log.Add($"{(fits ? "" : "WARNING: ")}gas monster at ({g.X:0.0}, {g.Z:0.0}): {Scales[size]} times the size, its foot {poses.FootY:0} " +
                    $"(the road {g.BiteY:0}, its bite {d / 512:0.0} cells out, the rail {g.Edge:0.0}); struck its head {poses.StruckMiss:0} from the bite, " +
                    $"poised {poses.ReadyHeadOver:0} over the road, at the least {poses.LeastOverRail:0} over the rail and {poses.LeastOverDeck:0} over the deck; " +
                    $"animations {gen}-{gen + 2}");
            gen += 4;
        }
        File.WriteAllBytes(animPath, anims);
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return log;
    }

    // The first of 4 animation numbers in a row the entity doesn't use yet, from `from` on, in steps of 4.
    private static int FirstFreeAnim(byte[] table, int entity, int from)
    {
        int start = BitConverter.ToInt32(table, entity * 4), end = BitConverter.ToInt32(table, (entity + 1) * 4);
        var used = new HashSet<int>();
        for (var p = start; p < end && table[p] != 255;)
            if (table[p] == 3) { used.Add(table[p + 1] | table[p + 2] << 8); p += 3 + table[p + 3]; }
            else p += 2 + table[p + 2];
        var gen = from;
        while (Enumerable.Range(gen, 4).Any(used.Contains)) gen += 4;
        return gen;
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
                spots.Add(new GasMonsterSpot(sx, sz, r.X[k] + ax * side * half * BiteAcross, r.Z[k] + az * side * half * BiteAcross, r.H[k], size, run.Out));
                prefer = -side;
                break;
            }
        }
        report.Notes.Add($"gas monsters along the lap's points {from}-{to}: {spots.Count} ({string.Join(", ", spots.Select(s => $"({s.X:0.0}, {s.Z:0.0}) {Scales[s.Size]}x"))})");
        return spots;
    }
}
