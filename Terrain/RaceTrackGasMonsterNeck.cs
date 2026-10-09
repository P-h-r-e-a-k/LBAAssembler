using System.IO;
using System.Numerics;
using LbaBodyStudio;
using LBAAssembler.Lba1;

namespace LBAAssembler.Terrain;

// The gas monster's neck bent over a raised road's rail (RaceTrackGasMonster.InstallPoses): the user, 2026-10-09, of the monsters leaning
// stiffly from the gas onto the road: "Some of the gas monsters strike too low and appear through the track, let's have them permanently
// visible with their necks above the track and randomly striking down". A straight body leaned from its foot in the gas crosses the rail
// lower than its jaws, so it went through the road's side whenever it bit more than a little inside the rail; this bends the body's neck
// instead, bone by bone, as a serpent arches its neck over a wall.
//
// The body (either of the game's two: shut 320, mouth open 322, whose skeletons differ -- ten neck bones and a separate head, or six and a
// jaw) is a chain from its foot (bone 0, the hole) up to its head: the neck is the chain's bones below the first big one, the head is that
// bone and all under it. A Shape bends the neck in the plane of the body's y (up) and z (where its mouth faces): each neck bone leans
// Lean + (Top - Lean) * along^Gather from upright towards the mouth's side (along: the bone's middle up the neck, 0 to 1), and the head
// Nod past the neck's top; Sway turns the neck sideways, shared among its bones. The engine turns a bone about x by M(Alpha) (+y towards
// +z) in its parent's frame, as Lba1Pose does, so a bone's own value is the difference from its parent's lean.
internal sealed class GasMonsterNeck
{
    internal readonly record struct Shape(double Lean, double Top, double Gather, double Nod);

    private readonly Body body;
    private readonly int[] neck;
    private readonly double[] along;
    private readonly int head;
    public readonly int[] HeadPoints;

    public GasMonsterNeck(Body body)
    {
        this.body = body;
        var rest = body.World();
        // (the chain: from the bone of the highest point down to the foot)
        var top = Enumerable.Range(0, rest.Length).MaxBy(p => rest[p].Y);
        var bone = body.Bones.FindIndex(b => top >= b.Start && top < b.Start + b.Count);
        var chain = new List<int>();
        for (; bone >= 0; bone = body.Bones[bone].Parent) chain.Insert(0, bone);
        var headAt = chain.FindIndex(b => b > 0 && body.Bones[b].Count >= 30);
        if (headAt < 2) throw new InvalidDataException("The gas monster's body has no neck below a head.");
        head = chain[headAt];
        neck = chain.Skip(1).Take(headAt - 1).ToArray();
        double Pivot(int b) => rest[body.Bones[b].Pivot].Y;
        double foot = Pivot(neck[0]), length = Pivot(head) - foot;
        along = neck.Select((b, i) => ((Pivot(b) + (i + 1 < neck.Length ? Pivot(neck[i + 1]) : Pivot(head))) / 2 - foot) / length).ToArray();
        var under = new HashSet<int> { head };
        for (var changed = true; changed;)
        {
            changed = false;
            for (var b = 0; b < body.Bones.Count; b++)
                if (!under.Contains(b) && under.Contains(body.Bones[b].Parent)) { under.Add(b); changed = true; }
        }
        HeadPoints = under.SelectMany(b => Enumerable.Range(body.Bones[b].Start, body.Bones[b].Count)).ToArray();
    }

    public int BoneCount => body.Bones.Count;

    // Each bone's turn (radians about x, about z) for a shape, the sway shared out sideways along the neck.
    private (double Alpha, double Gamma)[] Turns(Shape s, double sway)
    {
        var turns = new (double, double)[body.Bones.Count];
        var lean = 0.0;
        for (var i = 0; i < neck.Length; i++)
        {
            var at = s.Lean + (s.Top - s.Lean) * Math.Pow(along[i], s.Gather);
            turns[neck[i]] = (at - lean, sway / neck.Length);
            lean = at;
        }
        turns[head] = (s.Top + s.Nod - lean, 0);
        return turns;
    }

    // The body's points posed, from its foot.
    public Vector3[] Pose(Shape s, double sway = 0) => Posed(Turns(s, sway));

    // ... and part way (t) from one shape to another as the engine moves between keyframes: each bone's turn by itself.
    public Vector3[] Blend(Shape a, Shape b, double t)
    {
        var (ta, tb) = (Turns(a, 0), Turns(b, 0));
        return Posed(ta.Select((x, i) => (x.Alpha + (tb[i].Alpha - x.Alpha) * t, x.Gamma + (tb[i].Gamma - x.Gamma) * t)).ToArray());
    }

    private Vector3[] Posed((double Alpha, double Gamma)[] turns)
    {
        const double Units = 1024 / (2 * Math.PI);
        return Lba1Pose.World(body, turns.Select(t => (0, t.Alpha * Units, 0d, t.Gamma * Units)).ToArray());
    }

    // An animation keyframe of the shape: slot 0 the root's (no turn), each bone's turn (in turns).
    public AnimFrame Frame(Shape s, int time, double sway = 0)
    {
        var turns = Turns(s, sway);
        return new AnimFrame(time, 0, 0, 0, turns.Select((t, i) => i == 0 ? AnimBone.Neutral
            : new AnimBone(0, (float)(t.Alpha / (2 * Math.PI)), 0, (float)(t.Gamma / (2 * Math.PI)))).ToArray());
    }
}

// The monster's poses at one place: where its foot stands (under the gas) and the neck's shapes ready (its head poised over the rail),
// reared back and struck (its head down on the road at its bite), found by search against the road beside it. In the monster's own frame:
// its foot at the place it rises from, z towards its bite D away, the rail E away, the road's deck Road over the gas.
internal static class GasMonsterPoser
{
    internal sealed record Poses(double FootY, GasMonsterNeck.Shape Ready, GasMonsterNeck.Shape Rear, GasMonsterNeck.Shape Struck,
        double StruckMiss, double ReadyHeadOver, double LeastOverRail, double LeastOverDeck);

    // how far over the rail (220 over the deck) and over the deck the body keeps, struck and when not
    private const double RailHeight = RaceTrackRaisedBody.Rail, RailOver = 120, DeckOver = 60, PoisedOver = 450;

    public static Poses Solve(GasMonsterNeck shut, GasMonsterNeck open, double d, double e, double road, double k)
    {
        // (how a pose sits against the road: the least height over the rail and over the deck of the points out over them, foot at footY --
        // nothing on the road's side of the rail lower than the deck, not even under it; its hole in the gas, beside the road, is clear)
        (double Rail, double Deck) Clear(Vector3[] p, double footY)
        {
            double rail = double.MaxValue, deck = double.MaxValue;
            foreach (var q in p)
            {
                if (q.Z < e - 100) continue;
                var y = footY + q.Y;
                if (q.Z <= e + 150) rail = Math.Min(rail, y - road - RailHeight);
                deck = Math.Min(deck, y - road);
            }
            return (rail, deck);
        }
        // (the head's lowest point and where it is)
        (double Y, double Z) Low(GasMonsterNeck neck, Vector3[] p, double footY)
        {
            var low = neck.HeadPoints.MinBy(i => p[i].Y);
            return (footY + p[low].Y, p[low].Z);
        }
        double Cost(GasMonsterNeck.Shape s, double footY, double wantY, double wantZ, double over, bool struck)
        {
            var cost = 0.0;
            foreach (var neck in new[] { shut, open })
            {
                var p = neck.Pose(s);
                var (lowY, lowZ) = Low(neck, p, footY);
                var (rail, deck) = Clear(p, footY);
                cost += (lowY - wantY) * (lowY - wantY) + (lowZ - wantZ) * (lowZ - wantZ);
                var railNeed = RailOver + (struck ? 0 : over);
                var deckNeed = struck ? DeckOver : over;
                if (rail < railNeed) cost += 400 * (railNeed - rail) * (railNeed - rail) + 1e6;
                if (deck < deckNeed) cost += 400 * (deckNeed - deck) * (deckNeed - deck) + 1e6;
            }
            // (a gentle bend: no bone turning back on itself)
            if (s.Top < s.Lean - 0.3) cost += 1e5 * (s.Lean - 0.3 - s.Top);
            return cost;
        }
        GasMonsterNeck.Shape Search(double footY, double wantY, double wantZ, double over, bool struck, out double best)
        {
            var bestShape = new GasMonsterNeck.Shape(0, 0, 1, 0);
            best = double.MaxValue;
            foreach (var lean in Steps(-0.4, 1.2, 9))
            foreach (var top in Steps(0, 3.0, 13))
            foreach (var gather in new[] { 0.6, 1.0, 1.6, 2.5, 4.0 })
            foreach (var nod in Steps(-0.8, 1.2, 9))
            {
                var s = new GasMonsterNeck.Shape(lean, top, gather, nod);
                var c = Cost(s, footY, wantY, wantZ, over, struck);
                if (c < best) { best = c; bestShape = s; }
            }
            // (then nearer, each value in turn, the steps halving)
            for (var step = 0.1; step > 0.004; step /= 2)
                for (var improved = true; improved;)
                {
                    improved = false;
                    for (var axis = 0; axis < 4; axis++)
                        foreach (var sign in new[] { -1, 1 })
                        {
                            var s = bestShape;
                            s = axis switch
                            {
                                0 => s with { Lean = s.Lean + sign * step },
                                1 => s with { Top = s.Top + sign * step },
                                2 => s with { Gather = Math.Max(0.3, s.Gather + sign * step * 4) },
                                _ => s with { Nod = s.Nod + sign * step },
                            };
                            var c = Cost(s, footY, wantY, wantZ, over, struck);
                            if (c < best - 1e-6) { best = c; bestShape = s; improved = true; }
                        }
                }
            return bestShape;
        }

        // struck: the head's lowest point just over the deck at the bite, the foot as high as lets it (at the gas or under it)
        var length = shut.Pose(new GasMonsterNeck.Shape(0, 0, 1, 0)).Max(q => q.Y);
        Poses? found = null;
        var bestCost = double.MaxValue;
        foreach (var footY in Steps(0, -0.45 * length, 6))
        {
            var struck = Search(footY, road + DeckOver + 40, d, 0, true, out var cost);
            cost += Math.Abs(footY) * 2;
            if (cost >= bestCost) continue;
            bestCost = cost;
            found = new Poses(footY, struck, struck, struck, 0, 0, 0, 0);
        }
        var foot = found!.FootY;
        // poised: its head over the rail, well up; reared: back over the gas and higher
        var ready = Search(foot, road + 1300 * k, e + 0.25 * (d - e), PoisedOver, false, out _);
        var rear = Search(foot, road + 1900 * k, e - 600 * k, PoisedOver, false, out _);
        // (how well it came out: the struck head's miss, the poised head's height over the deck, the least clearances)
        var sp = shut.Pose(found.Struck);
        var (sy, sz) = Low(shut, sp, foot);
        var miss = Math.Sqrt((sy - (road + DeckOver + 40)) * (sy - (road + DeckOver + 40)) + (sz - d) * (sz - d));
        var (ry, _) = Low(shut, shut.Pose(ready), foot);
        double leastRail = double.MaxValue, leastDeck = double.MaxValue;
        foreach (var s in new[] { ready, rear, found.Struck })
            foreach (var neck in new[] { shut, open })
            {
                var (rail, deck) = Clear(neck.Pose(s), foot);
                leastRail = Math.Min(leastRail, rail); leastDeck = Math.Min(leastDeck, deck);
            }
        // (and the moves between them, as the engine blends them: bone by bone)
        foreach (var (a, b) in new[] { (ready, rear), (rear, found.Struck), (found.Struck, ready) })
            for (var t = 0.25; t < 1; t += 0.25)
            {
                var (rail, deck) = Clear(shut.Blend(a, b, t), foot);
                leastRail = Math.Min(leastRail, rail); leastDeck = Math.Min(leastDeck, deck);
            }
        return found with { Ready = ready, Rear = rear, StruckMiss = miss, ReadyHeadOver = ry - road, LeastOverRail = leastRail, LeastOverDeck = leastDeck };
    }

    private static IEnumerable<double> Steps(double from, double to, int n) => Enumerable.Range(0, n).Select(i => from + (to - from) * i / (n - 1));
}
