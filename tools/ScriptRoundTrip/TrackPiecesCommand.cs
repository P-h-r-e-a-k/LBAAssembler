using System.Text.Json;
using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// trackpieces <game> <ILE name> <spec> <out plan.json>: track pieces laid as the 3D view's Build > Race track lays them (TrackPieceSet), then
// made into a plan. The spec: "x,z,lift,degrees|piece|piece|..." -- the first piece at island cell x,z, lift over the ground there, its way
// in degrees (0 along +x, 90 along +z); each next one dropped just past the last one's end, so it snaps on from it (TrackPieceSet.Snapped);
// "piece@x,z,lift,degrees" drops one where it says (a pit lane beside a straight). Prints what the plan says and writes it.
internal static class TrackPiecesCommand
{
    public static int Run(string[] args)
    {
        var (island, _) = TrackSketchCommand.Map(args[1], args[2].ToUpperInvariant(), TrackSketchCommand.MapScale);
        var items = args[3].Split('|');
        double Ground(double x, double z) => Math.Max(0.0, IslandOps.Altitude(island, x * 512, z * 512) ?? 0);
        (double X, double Z, double Lift, double Heading) Spot(string s)
        {
            var v = s.Split(',').Select(t => double.Parse(t, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            return (v[0], v[1], v[2], v[3] * Math.PI / 180);
        }
        var set = new TrackPieceSet();
        var at = Spot(items[0]);
        (double X, double Z, double Y, double Heading)? last = null;
        foreach (var item in items.Skip(1))
        {
            var name = item.Split('@')[0].Trim();
            var section = TrackSection.All.FirstOrDefault(s => s.Name == name) ?? throw new ArgumentException($"no piece \"{name}\": {string.Join(", ", TrackSection.All.Select(s => s.Name))}");
            TrackPiece piece;
            if (item.Contains('@')) { var s = Spot(item.Split('@')[1]); piece = new TrackPiece(section, s.X, s.Z, Ground(s.X, s.Z) + s.Lift, s.Heading); }
            else if (last is { } e) piece = new TrackPiece(section, e.X + Math.Cos(e.Heading) * 0.6, e.Z + Math.Sin(e.Heading) * 0.6, e.Y + 150, e.Heading + 0.05);
            else piece = new TrackPiece(section, at.X, at.Z, Ground(at.X, at.Z) + at.Lift, at.Heading);
            var laid = set.Snapped(piece);
            set.Pieces.Add(laid);
            if (!laid.IsPit) last = laid.End;
            Console.WriteLine($"  {laid.Section.Name,-18} at ({laid.X:0.0}, {laid.Z:0.0}) y {laid.Y:0} way {laid.Heading * 180 / Math.PI:0}  -> end ({laid.End.X:0.0}, {laid.End.Z:0.0}) y {laid.End.Y:0}{(ReferenceEquals(laid, piece) ? " (not snapped)" : "")}");
        }
        var chain = set.Chain(out var closed);
        Console.WriteLine($"{set.Pieces.Count} pieces, {set.OpenEnds().Count()} open end(s), {set.OpenStarts().Count()} open start(s); chain {(chain is null ? "none" : $"{chain.Count} points, {(closed ? "a lap" : "a sprint")}")}");
        var result = set.MakePlan(island);
        foreach (var n in result.Notes) Console.WriteLine("  note: " + n);
        foreach (var p in result.Problems) Console.WriteLine("  PROBLEM: " + p);
        if (result.Plan is not { } plan) return 1;
        var heights = plan.Heights ?? Array.Empty<double>();
        Console.WriteLine($"plan: {plan.Points.Length} points, heights {(heights.Length > 0 ? $"{heights.Min():0}..{heights.Max():0}" : "none")}, raised {(plan.Raised is { } r ? string.Join(",", r) : "no")}, jumps {plan.GapJumps?.Length ?? 0}, loops {plan.Loops?.Length ?? 0}");
        // (the height laid against the plan's, along the way: how near the exact heights came through)
        if (chain is not null && heights.Length == plan.Points.Length)
        {
            var worst = 0.0;
            for (var k = 0; k < plan.Points.Length; k += 5)
            {
                double px = plan.Points[k][0] + plan.OriginCellX, pz = plan.Points[k][1] + plan.OriginCellZ;
                // (the nearest laid point at about that height: stacked decks are told apart by height)
                var near = chain.OrderBy(c => (c.X - px) * (c.X - px) + (c.Z - pz) * (c.Z - pz) + Math.Pow((c.Y - heights[k]) / 512, 2)).First();
                worst = Math.Max(worst, Math.Abs(near.Y - heights[k]));
            }
            Console.WriteLine($"heights: the plan within {worst:0} of the heights laid (every 5th point)");
        }
        var json = JsonSerializer.Serialize(plan, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
        File.WriteAllText(args[4], json);
        Console.WriteLine($"{args[4]} written");
        return 0;
    }
}
