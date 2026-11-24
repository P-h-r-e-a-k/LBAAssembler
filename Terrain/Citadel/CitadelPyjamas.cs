using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// Twinsen's pyjamas (the user, 2026-11-17, with a picture: "traditional pyjamas complete with a long hat" -- a long light-blue nightgown with
// thin dark stripes, a buttoned placket, and a long floppy nightcap with a white pompom, barefoot): one more body of his, PyjamaBody, in the
// hero's entity and his other behaviours' (each behaviour is an entity of its own, its bodies the same numbers), made from his everyday body
// (BODY.HQR 0) as the racing overalls are:
//   - his shirt, jeans and belt the gown's light blue, every third band across a polygon's middle the stripes' darker blue; a skirt hung from
//     his hips to his shins, in panels of the same; three white buttons down his chest;
//   - his shins and feet bare (the skin of his face);
//   - a nightcap over his head (his hair's tuft tucked in), its band round his brow, its point drooping over to the right and down behind his
//     ear, a white pompom on its end.
// The engine keeps him in it while the story's PyjamaVar says so (RACEMOD.CPP pyjamas=; RaceTrackStory.Pyjamas). And the two things on the
// chair by his bed the morning after (CitadelProps generics): his racing suit folded (FoldedSuit, in the suit's colour), his pyjamas folded
// with the cap on top (FoldedPyjamas).
internal static class CitadelPyjamas
{
    public const int PyjamaBody = 33;
    private const int HeroEntity = 0;
    private static readonly int[] BehaviourEntities = { 1, 2, 3 };
    private const int Shirt = 224, Jeans = 194, Jeans2 = 176, Belt = 97, Shoes = 80, Skin = 32;
    // (the gown low in the blues' ramp, as his own clothes are -- his jeans at its 2: the light climbs the ramp from a polygon's colour, and
    // from 11 it was at the ramp's end in a few steps, every lit side the same light blue -- the user, 2026-11-22: "Twinsen's pyjama model
    // looks a bit flat". The stripes the ramp's darkest)
    internal const int Gown = 12 * 16 + 5, Stripe = 12 * 16 + 2, White = 3 * 16 + 15;
    // (the most of the light a gown point takes: Install, the ramp's end)
    private const float MaxGownLight = 1f;
    private const int Hips = 2, Chest = 3, Head = 14;
    // (the hem down at his ankles since 2026-11-24 -- SIN's picture, GitHub #26, "Twinsen's legs glitch through his nightgown": the skirt is
    // rigid on his hips and, from a hem at his shins, his back leg's bare shin swung out through it as he walked)
    private const float HemY = 80;
    private static readonly int[] Shins = { 9, 12 }, Feet = { 10, 13 };

    internal static Body Build(Body body)
    {
        var world = body.World().ToList();
        int BoneOf(int p) => body.Bones.FindIndex(b => p >= b.Start && p < b.Start + b.Count);
        var skinMaterial = body.Faces.FirstOrDefault(f => f.Colour == Skin && BoneOf(f.Points[0]) == Head)?.Material ?? -1;
        for (var i = 0; i < body.Faces.Count; i++)
        {
            var f = body.Faces[i];
            var bone = BoneOf(f.Points[0]);
            if (Shins.Contains(bone) || Feet.Contains(bone))
            {
                // (his feet bare; his shins the gown's, whole: what of a leg swings out through the skirt as he strides is its cloth, not skin
                // -- GitHub #26)
                if (f.Colour is Jeans or Jeans2 or Shoes)
                    body.Faces[i] = Shins.Contains(bone) ? f with { Colour = Gown, Material = -1, Texture = null } : f with { Colour = Skin, Material = skinMaterial, Texture = null };
                continue;
            }
            if (f.Colour is not (Shirt or Jeans or Jeans2 or Belt)) continue;
            var pts = f.Points.Select(p => world[p]).ToArray();
            var n = LBAAssembler.Terrain.Palace.PalaceTorturers.Newell(pts);
            var middle = pts.Aggregate(Vector3.Zero, (s, p) => s + p) / pts.Length;
            var across = MathF.Abs(n.X) > MathF.Abs(n.Z) ? middle.Z : middle.X;
            var stripe = ((int)MathF.Floor((across + 4000) / 30f)) % 3 == 0;
            body.Faces[i] = f with { Colour = stripe ? Stripe : Gown, Material = -1, Texture = null };
        }
        // (his hair's tuft -- spheres at the back of his head -- tucked into the cap)
        var head = body.Bones[Head];
        bool UnderHead(int b) { while (b > Head) b = body.Bones[b].Parent; return b == Head; }
        body.Spheres.RemoveAll(sp => BoneOf(sp.Point) is var bp && bp >= Head && UnderHead(bp));

        // the skirt: three rings round his hips, the waist's, the knee's, the hem's at his shins; panels of the gown, every third a stripe
        var skirt = new List<Vector3>();
        var skirtFaces = new List<(int[] Points, int Colour)>();
        const int Around = 16;
        var hipPts = world.Skip(body.Bones[Hips].Start).Take(body.Bones[Hips].Count).ToArray();
        var cz = (hipPts.Min(p => p.Z) + hipPts.Max(p => p.Z)) / 2;
        var rings = new[] { (Y: 600f, Rx: 140f, Rz: 118f), (Y: 330f, Rx: 172f, Rz: 152f), (Y: HemY, Rx: 186f, Rz: 172f) }
            .Select(r => Enumerable.Range(0, Around).Select(k =>
            {
                var a = (k + 0.5f) * MathF.Tau / Around;
                skirt.Add(new Vector3(r.Rx * MathF.Sin(a), r.Y, cz + r.Rz * MathF.Cos(a)));
                return skirt.Count - 1;
            }).ToArray()).ToArray();
        var skirtMid = new Vector3(0, 380, cz);
        for (var r = 0; r + 1 < rings.Length; r++)
            for (var k = 0; k < Around; k++)
                skirtFaces.Add((Outward(skirt, new[] { rings[r][k], rings[r][(k + 1) % Around], rings[r + 1][(k + 1) % Around], rings[r + 1][k] }, skirtMid), k % 3 == 0 ? Stripe : Gown));
        body = AddToBone(body, world, Hips, skirt, skirtFaces);
        world = body.World().ToList();

        // three white buttons down his chest's front
        var chestPts = world.Skip(body.Bones[Chest].Start).Take(body.Bones[Chest].Count).ToArray();
        var front = chestPts.Max(p => p.Z);
        var buttons = new List<Vector3> { new(0, 960, front - 4), new(0, 905, front - 2), new(0, 850, front) };
        var firstButton = body.Bones[Chest].Start + body.Bones[Chest].Count;
        body = AddToBone(body, world, Chest, buttons, new List<(int[] Points, int Colour)>());
        for (var k = 0; k < buttons.Count; k++) body.Spheres.Add(new BodySphere(firstButton + k, 9, White));
        world = body.World().ToList();

        // the nightcap: its band round his brow (lower behind), then rings along a curve up over his crown and drooping off to his right and
        // down behind his ear, narrowing to the pompom
        head = body.Bones[Head];
        var own = world.Skip(head.Start).Take(head.Count).ToArray();
        var lo = new Vector3(own.Min(p => p.X), own.Min(p => p.Y), own.Min(p => p.Z));
        var hi = new Vector3(own.Max(p => p.X), own.Max(p => p.Y), own.Max(p => p.Z));
        float hx = (lo.X + hi.X) / 2, hz = lo.Z + 108, rx = (hi.X - lo.X) / 2 + 16, rz = 116;
        var cap = new List<Vector3>();
        var capFaces = new List<(int[] Points, int Colour)>();
        const int Round = 10, Steps = 8;
        Vector3 P0 = new(hx, hi.Y - 75, hz), P1 = new(hx + 10, hi.Y + 210, hz - 30), P2 = new(hx + 200, hi.Y - 170, hz - 150);
        Vector3 Curve(float t) => (1 - t) * (1 - t) * P0 + 2 * (1 - t) * t * P1 + t * t * P2;
        Vector3 Tangent(float t) => Vector3.Normalize(2 * (1 - t) * (P1 - P0) + 2 * t * (P2 - P1));
        var capRings = new int[Steps][];
        for (var s = 0; s < Steps; s++)
        {
            var t = s / (float)Steps;
            var c = Curve(t);
            var scale = 1 - 0.85f * t;
            capRings[s] = new int[Round];
            for (var k = 0; k < Round; k++)
            {
                var a = k * MathF.Tau / Round;
                Vector3 p;
                if (s == 0) p = c + new Vector3(rx * MathF.Sin(a), 20 * MathF.Cos(a), rz * MathF.Cos(a));
                else
                {
                    var tan = Tangent(t);
                    var u = Vector3.Normalize(Vector3.Cross(tan, Vector3.UnitZ));
                    var v = Vector3.Cross(u, tan);
                    p = c + u * (rx * scale * MathF.Sin(a)) + v * (rz * scale * MathF.Cos(a));
                }
                cap.Add(p);
                capRings[s][k] = cap.Count - 1;
            }
        }
        var tip = cap.Count; cap.Add(Curve(1));
        for (var s = 0; s + 1 < Steps; s++)
        {
            var mid = (Curve(s / (float)Steps) + Curve((s + 1) / (float)Steps)) / 2;
            for (var k = 0; k < Round; k++)
                capFaces.Add((Outward(cap, new[] { capRings[s][k], capRings[s][(k + 1) % Round], capRings[s + 1][(k + 1) % Round], capRings[s + 1][k] }, mid), k % 3 == 0 ? Stripe : Gown));
        }
        for (var k = 0; k < Round; k++)
            capFaces.Add((Outward(cap, new[] { capRings[Steps - 1][k], capRings[Steps - 1][(k + 1) % Round], tip }, Curve((Steps - 0.5f) / Steps)), Gown));
        var capStart = head.Start + head.Count;
        body = AddToBone(body, world, Head, cap, capFaces);
        body.Spheres.Add(new BodySphere(capStart + tip, 34, White));
        body.LimitOverride = 1100;
        return body;
    }

    // a polygon's points turned to face away from `inside`
    private static int[] Outward(List<Vector3> points, int[] q, Vector3 inside)
    {
        var at = q.Select(p => points[p]).ToArray();
        var n = LBAAssembler.Terrain.Palace.PalaceTorturers.Newell(at);
        var middle = at.Aggregate(Vector3.Zero, (s, p) => s + p) / at.Length;
        return Vector3.Dot(n, middle - inside) < 0 ? q.Reverse().ToArray() : q;
    }

    // `points` (world) into bone `bone` after its own, every later index moved along by as many, and `faces` on them (their indexes
    // `points`'), lit -- as the overalls' helmet is put on (CitadelOveralls.AddToBone)
    private static Body AddToBone(Body body, List<Vector3> world, int bone, List<Vector3> points, List<(int[] Points, int Colour)> faces)
    {
        var owner = body.Bones[bone];
        var at = owner.Start + owner.Count;
        var k2 = points.Count;
        int Moved(int p) => p >= at ? p + k2 : p;
        world.InsertRange(at, points);
        for (var b = 0; b < body.Bones.Count; b++)
        {
            var bn = body.Bones[b];
            if (b == bone) body.Bones[b] = bn with { Count = bn.Count + k2 };
            else if (bn.Start >= at) body.Bones[b] = bn with { Start = bn.Start + k2, Pivot = Moved(bn.Pivot) };
            else body.Bones[b] = bn with { Pivot = Moved(bn.Pivot) };
        }
        for (var i = 0; i < body.Faces.Count; i++) body.Faces[i] = body.Faces[i] with { Points = body.Faces[i].Points.Select(Moved).ToArray(), PointNormals = null };
        for (var i = 0; i < body.Lines.Count; i++) body.Lines[i] = body.Lines[i] with { A = Moved(body.Lines[i].A), B = Moved(body.Lines[i].B) };
        for (var i = 0; i < body.Spheres.Count; i++) body.Spheres[i] = body.Spheres[i] with { Point = Moved(body.Spheres[i].Point) };
        foreach (var (q, colour) in faces) body.Faces.Add(new Face(q.Select(p => at + p).ToArray(), colour, Material: -1));
        // (the new points as much of the light as the bone's own take -- the most of them: 1.6 on most of him, the whole ramp; at 1 the skirt
        // and the cap climbed only some nine steps of it and looked flat beside him)
        if (body.LightScale is { } scales && scales.Length > at)
        {
            var own = scales.Skip(owner.Start).Take(owner.Count).DefaultIfEmpty(1f).Max();
            body.LightScale = scales.Take(at).Concat(Enumerable.Repeat(own, k2)).Concat(scales.Skip(at)).ToArray();
        }
        body.Vertices = Enumerable.Repeat(Vector3.Zero, world.Count).ToList();
        body.SetWorld(world.ToArray());
        return body;
    }

    // Into the game folder after the overalls (RaceTrackService): the body appended (its entry again on a second pass), and the hero's
    // entities -- each behaviour's, and the arm-in-arm one's (the pair's body under every number his own can be at) -- told of it
    public static List<string> Install(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var body = Build(Body.Read(HqrArchive.Open(bodyPath).Read(0), 2));
        // (written lit, as the overalls are: with the points' normals and Gouraud polygons -- read, a body isn't, and written so it had no
        // normals at all, every polygon flat in its one colour; the user, 2026-11-22: "Twinsen's pyjama model looks a bit flat, apparently it
        // needs more normals")
        body.Lit = true;
        // (the light a point adds to its polygon's colour (AFF_OBJ.CPP: colour + light >> 8, nothing holding it in the colour's ramp) is
        // 15,360 x its normal's length / 2^24 at most -- 9 at the 10,240 of a light scale of 1, 15 at Twinsen's own 1.6. The game's own bodies
        // start their ramps at 0 for it; the gown, at the ramp's 5, went 6 past the blues' end into the next ramp's dark where the light was
        // full on -- black patches on his chest and shoulders that came and went as he turned (the user, 2026-11-22, "Some parts of Twinsen's
        // new pyjama body look like they have black patches that come and go as he rotates"). The gown's and the stripes' points at no more
        // than 1: the gown 5-14, the stripes 2-11, in the ramp)
        if (body.LightScale is { } scales)
        {
            var gown = body.Faces.Where(f => f.Colour is Gown or Stripe).SelectMany(f => f.Points).ToHashSet();
            for (var p = 0; p < scales.Length; p++)
                if (gown.Contains(p) && scales[p] > MaxGownLight) scales[p] = MaxGownLight;
        }
        var bytes = body.Write();
        var table = HqrArchive.Open(ressPath).Read(44);
        int entry;
        if (RaceTrackGuards.BodyOf(table, HeroEntity, PyjamaBody) is { } had) { HqrWriter.ReplaceInFile(bodyPath, had, bytes); entry = had; }
        else { entry = HqrArchive.CountEntries(bodyPath); HqrWriter.AppendToFile(bodyPath, bytes); }
        // (each with his everyday body's collision box -- 1,240 tall -- not the pyjamas' own bounds: the nightcap made him too tall for the
        // storeroom's doorway, the user, 2026-11-19, "Twinsen is unable to enter the back room of his house when wearing pyjamas")
        foreach (var e in BehaviourEntities.Prepend(HeroEntity))
            table = RaceTrackBaldinoCar.WithBodyLike(table, e, PyjamaBody, entry, 0);
        if (RaceTrackGuards.BodyOf(table, CitadelOveralls.CoupleEntity, 0) is { } pair)
            table = RaceTrackBaldinoCar.WithBodyLike(table, CitadelOveralls.CoupleEntity, PyjamaBody, pair, 0);
        HqrWriter.ReplaceInFile(ressPath, 44, table);
        return new() { $"Twinsen's pyjamas: the hero's body {PyjamaBody} (BODY.HQR {entry}: {body.Vertices.Count} points, {body.Faces.Count} polygons, {body.Spheres.Count} spheres) -- a striped light-blue nightgown and nightcap, barefoot" };
    }
}
