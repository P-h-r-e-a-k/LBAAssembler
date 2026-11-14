using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using LBAAssembler.Terrain.Palace;

namespace LBAAssembler.Terrain;

// The flag marshal of every track but the Temple of Bú's and Twinsun's three (the user, 2026-11-03: "for all the other tracks use the
// female sup from "169: Otringal, Bathroom in the Imperial Hotel (room #115)", but let's remodel her in a skimpy bikini like F1 race track
// girls"): the Imperial Hotel's red-haired Sup (scene 169 actor 2, entity 317, BODY.HQR 440) in a red bikini and red shoes. Her body as the
// game has it is two figures -- hers (bones 0-19) and her reflection in the bathroom's mirror (bones 20-28, 680 behind her) -- and she wears
// a long cream dress: a skirt from her waist to her shins (her legs are only there below its hem), the dress's top over her chest. Remodel:
//   - the reflection gone;
//   - the skirt gone (every polygon of the hips' bone and of the bones that swung its hem: the root's hem point, the thighs', the slit's);
//   - the dress's top skin, but for a band across her bust: the bikini's top;
//   - her midriff and hips made (rings round her middle, on the hips' bone): skin to the bikini's bottom, cut high at the sides;
//   - thighs (on the thighs' bones) and knees (on the shins', down onto the shins as they are) made, skin;
//   - her feet red: shoes.
// Her bones are as they were (bones 0-19), so the Temple of Bú's flags and animations go on her as on the School of Magic's assistant
// (TempleMarshal, its GridGirl rig).
internal static class GridGirl
{
    public const int Entity = 317, Retail = 440;
    // her bones: the root, the hips (the skirt's), the torso, the head; the thighs, shins and feet (left: -x, right: +x); the slit's two
    // bones by her right hip; the chest's little bone
    private const int Root = 0, Hips = 2, Torso = 3, LeftThigh = 11, LeftShin = 12, LeftFoot = 13, RightThigh = 14, RightShin = 15, RightFoot = 16;
    private const int Slit = 17, SlitHem = 18, Chest = 19, Mirror = 20, AllBones = 29;
    // the palette's colours (LBA2's: skin ramp 32.., red ramp 64..): her skin as her body has it; the dress's cream; the bikini's red; the
    // shoes' darker red
    private const int Skin = 36, Dress = 39, Red = 69, Shoes = 66;
    // (the high byte of her polygons' colour as the game gives it: kept on everything made)
    private const int Shade = 0x0A00;

    // Her body remodelled (the game's, `retail`), its bytes: no flags, her bones as they were (20 of them)
    public static byte[] Remodel(byte[] retail)
    {
        var p = PalaceTorturers.Parts.Read(retail);
        if (p.Bones.Count != AllBones || p.Bones[Mirror].Parent != Root || p.Bones[LeftShin].Count != 5 || p.Bones[RightShin].Count != 5)
            throw new InvalidDataException($"BODY.HQR {Retail} isn't the Imperial Hotel's Sup and her reflection.");
        var e = new Edit(p);
        e.DropBonesFrom(Mirror);
        // (every point of a bone, by the bone: as the body is now)
        HashSet<int> PointsOf(params int[] bones) => bones.SelectMany(b => Enumerable.Range(p.StartOf(b), p.Bones[b].Count)).ToHashSet();
        var skirt = PointsOf(Root, Hips, LeftThigh, RightThigh, Slit, SlitHem);
        var hipsPoints = PointsOf(Hips);
        var chest = PointsOf(Torso, Chest);
        var feet = PointsOf(LeftFoot, RightFoot);
        var world = p.World();
        e.EachPolygon((points, colour) =>
        {
            // (the skirt: a polygon of the hips' bone, or one using a point of the bones that swung it -- but not the torso's own, which
            // comes down onto the hips' top: its belly)
            var first = points[0];
            if (hipsPoints.Contains(first) || (points.Any(skirt.Contains) && !chest.Contains(first))) return null;
            if (feet.Contains(first)) return Shades(colour, Shoes);
            if ((colour & 0xFF) != Dress || !chest.Contains(first)) return colour;
            var c = points.Select(i => world[i]).Aggregate(Vector3.Zero, (a, b) => a + b) / points.Length;
            return c.Y >= 800 && c.Y <= 890 && c.Z >= 150 ? Shades(colour, Red) : Shades(colour, Skin);
        });
        // ---- her midriff and hips: rings round her middle (y, half across, front, back), on the hips' bone
        var rings = new (float Y, float Across, float Front, float Back)[]
        {
            (800, 118, 135, -55),   // inside the torso's lower edge (her back's is at 807: it shows below it)
            (640, 112, 150, -66),   // the waist, behind the belly's front
            (565, 132, 142, -84),   // the hips: the bikini's top edge
            (470, 118, 108, -82),   // where the thighs leave
            (418, 46, 62, -34),     // between her legs
        };
        const int Around = 8;
        var ring = rings.Select(r => e.Add(Hips, Enumerable.Range(0, Around).Select(k =>
        {
            var a = k * 2 * Math.PI / Around;
            float zc = (r.Front + r.Back) / 2, rz = (r.Front - r.Back) / 2;
            return new Vector3((float)(r.Across * Math.Sin(a)), r.Y, (float)(zc + rz * Math.Cos(a)));
        }).ToArray())).ToArray();
        // (a side of the ring: its middle's way round from the front -- 0 front, 4 back)
        static bool FrontOrBack(int k) { var m = (k + 0.5) % 8; return m < 1 || m > 7 || (m > 3 && m < 5); }
        for (var r = 0; r + 1 < rings.Length; r++)
            for (var k = 0; k < Around; k++)
            {
                var colour = r < 2 ? Skin : r == 2 ? (FrontOrBack(k) ? Red : Skin) : Red;
                e.Quad(ring[r][k], ring[r][(k + 1) % Around], ring[r + 1][(k + 1) % Around], ring[r + 1][k], Shade | colour, outward: true);
            }
        var bottom = e.Add(Hips, new[] { new Vector3(0, 405, 14) })[0];
        for (var k = 0; k < Around; k++) e.Tri(ring[^1][k], ring[^1][(k + 1) % Around], bottom, Shade | Red, outward: true);
        // ---- thighs, from inside the hips down to the knee, and the knee, down onto the shin's top (its four points at 188)
        foreach (var (thigh, shin, side) in new[] { (LeftThigh, LeftShin, -1), (RightThigh, RightShin, 1) })
        {
            var x = side * 64f;
            var top = Hexagon(e, thigh, new Vector3(x, 520, 8), 64, 58);
            var knee = Hexagon(e, thigh, new Vector3(side * 62f, 268, 10), 44, 40);
            e.Strip(top, knee, Shade | Skin);
            // (the shin's top: its four points at the hem's height, in order round it)
            var shinTop = Enumerable.Range(p.StartOf(shin), p.Bones[shin].Count).Where(i => Math.Abs(e.World(i).Y - 188) < 2).ToArray();
            if (shinTop.Length != 4) throw new InvalidDataException("Her shin's top isn't four points at 188.");
            var middle = shinTop.Select(e.World).Aggregate(Vector3.Zero, (a, b) => a + b) / 4;
            shinTop = shinTop.OrderBy(i => Math.Atan2(e.World(i).X - middle.X, e.World(i).Z - middle.Z)).ToArray();
            var upper = e.Add(shin, shinTop.Select(i => e.World(i) + new Vector3((e.World(i).X - middle.X) * 0.25f, 112, (e.World(i).Z - middle.Z) * 0.2f)).ToArray());
            for (var k = 0; k < 4; k++) e.Quad(upper[k], upper[(k + 1) % 4], shinTop[(k + 1) % 4], shinTop[k], Shade | Skin, outward: true);
            e.Cap(upper, Shade | Skin, up: true);
        }
        // ---- the back of her head: the game only ever showed her face on in the bathroom (her reflection showed her back), so her head is
        // a face, a neck's front and a shell of hair, open under the hair's back edge (1045 high, 136 behind) down to her shoulders: from
        // behind, the picture went through to the sky (the user, 2026-11-03: "The back of her head in some angles is transparent"). A
        // core of hair inside her head, on its bone, from her shoulders up into the hair -- behind her face and the front of her neck
        // (its front 50 forward, the neck's 51 and more), within the hair's sides (161 and 195 across) -- fills it.
        var hair = HairColour(p, Head);
        var core = new[] { 1010f, 1300f }.Select(y => e.Add(Head, Enumerable.Range(0, Around).Select(k =>
        {
            var a = k * 2 * Math.PI / Around;
            return new Vector3((float)(120 * Math.Sin(a)), y, (float)(-30 + 80 * Math.Cos(a)));
        }).ToArray())).ToArray();
        for (var k = 0; k < Around; k++)
            e.Quad(core[0][k], core[0][(k + 1) % Around], core[1][(k + 1) % Around], core[1][k], hair, outward: true);
        // (its bottom: a fan to its middle, facing down -- a polygon has four points at most)
        var underneath = e.Add(Head, new[] { new Vector3(0, 1010, -30) })[0];
        for (var k = 0; k < Around; k++) e.Tri(core[0][k], core[0][(k + 1) % Around], underneath, hair | 2 << 20, outward: false);
        // (and its top, facing up: the hair's shell is open behind there too, and showed the core's dark inside)
        var above = e.Add(Head, new[] { new Vector3(0, 1300, -30) })[0];
        for (var k = 0; k < Around; k++) e.Tri(core[1][k], core[1][(k + 1) % Around], above, hair | 1 << 20, outward: false);
        return e.Finish();
    }

    private const int Head = 4;

    // The colour of her hair: that of the head's polygon furthest behind (the hair's back), as the game gives it
    private static int HairColour(PalaceTorturers.Parts p, int head)
    {
        var world = p.World();
        int first = p.StartOf(head), last = first + p.Bones[head].Count;
        var best = (Z: float.MaxValue, Colour: Shade | Red);
        foreach (var (type, records) in p.Blocks)
        {
            var n = (type & 0x8000) != 0 ? 4 : 3;
            foreach (var r in records)
            {
                var points = Enumerable.Range(0, n).Select(k => (int)BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(k * 2))).ToArray();
                if (!points.All(i => i >= first && i < last)) continue;
                var z = points.Average(i => world[i].Z);
                if (z < best.Z) best = ((float)z, BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(8)));
            }
        }
        return best.Colour;
    }

    private static int Shades(int colour, int to) => (colour & 0xFF00) | to;

    // Six points round a leg (half across, front to back), on `bone`, going round as Edit.Strip wants them
    private static int[] Hexagon(Edit e, int bone, Vector3 centre, float across, float deep) =>
        e.Add(bone, Enumerable.Range(0, 6).Select(k => centre + new Vector3((float)(across * Math.Sin(k * Math.PI / 3)), 0, (float)(deep * Math.Cos(k * Math.PI / 3)))).ToArray());

    // A body's points added to the bones they belong to (each after the bone's own, every index after them moved on), its polygons kept,
    // recoloured or dropped, and new ones made; the new points' normals from the lit polygons round them, as long as its own skin's are.
    internal sealed class Edit
    {
        private readonly PalaceTorturers.Parts p;
        private readonly List<int[]> handed = new();           // the indices given out, moved on with the rest
        private readonly HashSet<int> made = new();
        private readonly List<(int[] Points, int Colour, bool Outward)> polys = new();
        // (the polygons made by Face: each facing the way given, not out from the body's middle)
        private readonly Dictionary<int[], Vector3> facing = new(ReferenceEqualityComparer.Instance);
        private Vector3[] world;

        public Edit(PalaceTorturers.Parts parts) { p = parts; world = p.World(); }
        public Vector3 World(int i) => world[i];

        public void DropBonesFrom(int bone)
        {
            var cut = p.StartOf(bone);
            var faces = p.Bones.Take(bone).Sum(b => b.FaceNormals);
            var removed = p.Points.Count - cut;
            p.Bones.RemoveRange(bone, p.Bones.Count - bone);
            p.Points.RemoveRange(cut, p.Points.Count - cut);
            p.Normals.RemoveRange(cut, p.Normals.Count - cut);
            p.FaceNormals = p.FaceNormals[..(faces * 8)];
            var faceFirst = cut + removed;
            p.FaceNormalCount = faces;
            for (var b = 0; b < p.Blocks.Count; b++)
            {
                var (type, records) = p.Blocks[b];
                var n = (type & 0x8000) != 0 ? 4 : 3;
                var kept = records.Where(r => Enumerable.Range(0, n).All(k => BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(k * 2)) < cut)).ToList();
                foreach (var r in kept)
                {
                    // (a light past every point -- a face normal's -- comes back by the points taken away)
                    var light = BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(10));
                    if (light >= faceFirst) BinaryPrimitives.WriteUInt16LittleEndian(r.AsSpan(10), (ushort)(light - removed));
                }
                p.Blocks[b] = (type, kept);
            }
            // (lines and spheres -- 8 bytes each, a line's points at 4 and 6, a sphere's point at 4 and its size at 6 -- on the bones taken
            // away: gone too)
            byte[] Keep(byte[] records, bool line) => records.Chunk(8).Where(r =>
                BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(4)) < cut && (!line || BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(6)) < cut)).SelectMany(r => r).ToArray();
            p.Lines = Keep(p.Lines, true); p.LineCount = p.Lines.Length / 8;
            p.Spheres = Keep(p.Spheres, false); p.SphereCount = p.Spheres.Length / 8;
            world = p.World();
        }

        // Each polygon's colour as `recolour` has it (given its points and colour), or dropped (null)
        public void EachPolygon(Func<int[], int, int?> recolour)
        {
            for (var b = 0; b < p.Blocks.Count; b++)
            {
                var (type, records) = p.Blocks[b];
                var n = (type & 0x8000) != 0 ? 4 : 3;
                var kept = new List<byte[]>();
                foreach (var r in records)
                {
                    var points = Enumerable.Range(0, n).Select(k => (int)BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(k * 2))).ToArray();
                    if (recolour(points, BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(8))) is not { } colour) continue;
                    BinaryPrimitives.WriteUInt16LittleEndian(r.AsSpan(8), (ushort)colour);
                    kept.Add(r);
                }
                p.Blocks[b] = (type, kept);
            }
        }

        // Points (as the body stands) added to `bone`: their indices, kept up to date as more are added
        public int[] Add(int bone, Vector3[] at)
        {
            var origin = bone == 0 ? Vector3.Zero : world[p.Bones[bone].Pivot];
            var insert = p.StartOf(bone) + p.Bones[bone].Count;
            int Move(int i) => i >= insert ? i + at.Length : i;
            // (every index at or past where they go: polygons' points and lights, bones' pivots, the indices handed out, the points made)
            foreach (var (type, records) in p.Blocks)
            {
                var n = (type & 0x8000) != 0 ? 4 : 3;
                foreach (var r in records)
                {
                    for (var k = 0; k < n; k++) BinaryPrimitives.WriteUInt16LittleEndian(r.AsSpan(k * 2), (ushort)Move(BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(k * 2))));
                    BinaryPrimitives.WriteUInt16LittleEndian(r.AsSpan(10), (ushort)Move(BinaryPrimitives.ReadUInt16LittleEndian(r.AsSpan(10))));
                }
            }
            for (var b = 0; b < p.Bones.Count; b++) p.Bones[b] = p.Bones[b] with { Pivot = Move(p.Bones[b].Pivot) };
            // (lines' two points, at 4 and 6, and spheres' one, at 4: the School of Magic's rabbibunny's whiskers and tail)
            for (var r = 0; r + 8 <= p.Lines.Length; r += 8)
                foreach (var at2 in new[] { 4, 6 }) BinaryPrimitives.WriteUInt16LittleEndian(p.Lines.AsSpan(r + at2), (ushort)Move(BinaryPrimitives.ReadUInt16LittleEndian(p.Lines.AsSpan(r + at2))));
            for (var r = 0; r + 8 <= p.Spheres.Length; r += 8)
                BinaryPrimitives.WriteUInt16LittleEndian(p.Spheres.AsSpan(r + 4), (ushort)Move(BinaryPrimitives.ReadUInt16LittleEndian(p.Spheres.AsSpan(r + 4))));
            foreach (var list in handed) for (var k = 0; k < list.Length; k++) list[k] = Move(list[k]);
            foreach (var poly in polys) for (var k = 0; k < poly.Points.Length; k++) poly.Points[k] = Move(poly.Points[k]);
            var movedMade = made.Select(Move).ToList(); made.Clear(); foreach (var m in movedMade) made.Add(m);
            for (var k = 0; k < at.Length; k++)
            {
                var local = at[k] - origin;
                p.Points.Insert(insert + k, ((short)Math.Round(local.X), (short)Math.Round(local.Y), (short)Math.Round(local.Z), bone));
                p.Normals.Insert(insert + k, (0, 0, 0, bone));
                made.Add(insert + k);
            }
            p.Bones[bone] = p.Bones[bone] with { Count = p.Bones[bone].Count + at.Length };
            world = p.World();
            var given = Enumerable.Range(insert, at.Length).ToArray();
            handed.Add(given);
            return given;
        }

        // A polygon, wound to face away from the body's middle line (`outward`) or as given
        public void Quad(int a, int b, int c, int d, int colour, bool outward) => polys.Add((new[] { a, b, c, d }, colour, outward));
        public void Tri(int a, int b, int c, int colour, bool outward) => polys.Add((new[] { a, b, c }, colour, outward));
        // A polygon (three or four points) wound to face `want`
        public void Face(int[] points, int colour, Vector3 want)
        {
            var copy = points.ToArray();
            polys.Add((copy, colour, false));
            facing[copy] = want;
        }
        // A sphere on `bone` at a place (as the body stands): its colour, its radius
        public void AddSphere(int bone, Vector3 at, int radius, int colour)
        {
            var point = Add(bone, new[] { at })[0];
            var record = new byte[8];
            BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(2), (ushort)colour);
            BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(4), (ushort)point);
            BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(6), (ushort)radius);
            p.Spheres = p.Spheres.Concat(record).ToArray();
            p.SphereCount = p.Spheres.Length / 8;
        }

        // The sides between two rings of as many points, going round the same way
        public void Strip(int[] upper, int[] lower, int colour)
        {
            for (var k = 0; k < upper.Length; k++) Quad(upper[k], upper[(k + 1) % upper.Length], lower[(k + 1) % lower.Length], lower[k], colour, outward: true);
        }

        public void Cap(int[] ring, int colour, bool up) => polys.Add((ring.ToArray(), colour | (up ? 1 << 20 : 2 << 20), false));

        public byte[] Finish()
        {
            // (each new polygon wound to face out: from the middle of the leg or body it is part of -- the middle of its points' ring, level with
            // it -- or, a cap, up or down)
            Vector3 Outward(int[] points, int colour)
            {
                if ((colour >> 20) == 1) return Vector3.UnitY;
                if ((colour >> 20) == 2) return -Vector3.UnitY;
                var c = points.Select(i => world[i]).Aggregate(Vector3.Zero, (a, b) => a + b) / points.Length;
                // (the middle line of whatever it's round: a leg's centre (x of its bone's pivot), the body's middle otherwise)
                var bone = p.Points[points[0]].Bone;
                var axis = bone == 0 ? Vector3.Zero : world[p.Bones[bone].Pivot];
                var centreX = Math.Abs(axis.X) > 20 ? axis.X : 0;
                return new Vector3(c.X - centreX, 0, c.Z - 20);
            }
            var written = new List<(int[] Points, int Colour)>();
            foreach (var (points, colour, _) in polys)
            {
                var n = PalaceTorturers.Newell(points.Select(i => world[i]).ToArray());
                var want = facing.TryGetValue(points, out var given) ? given : Outward(points, colour);
                written.Add((Vector3.Dot(n, want) >= 0 ? points : points.Reverse().ToArray(), colour & 0xFFFF));
            }
            // (the new points' normals: from the polygons round them, as long as her skin's own are)
            var skinLength = p.Normals.Where((n, i) => !made.Contains(i) && (n.X != 0 || n.Y != 0 || n.Z != 0)).Select(n => new Vector3(n.X, n.Y, n.Z).Length()).DefaultIfEmpty(10240).Average();
            var sum = new Dictionary<int, Vector3>();
            foreach (var (points, _) in written)
            {
                var n = PalaceTorturers.Newell(points.Select(i => world[i]).ToArray());
                foreach (var i in points.Where(made.Contains)) sum[i] = sum.GetValueOrDefault(i) + n;
            }
            foreach (var i in made)
            {
                var n = sum.TryGetValue(i, out var v) && v.LengthSquared() > 1e-9f ? Vector3.Normalize(v) * (float)skinLength : Vector3.Zero;
                p.Normals[i] = ((short)Math.Round(n.X), (short)Math.Round(n.Y), (short)Math.Round(n.Z), p.Normals[i].Bone);
            }
            foreach (var group in written.GroupBy(q => q.Points.Length == 4))
            {
                var type = (group.Key ? 0x8000 : 0) | 4;
                p.Blocks.Add((type, group.Select(q =>
                {
                    var r = new byte[12];
                    for (var k = 0; k < q.Points.Length; k++) BinaryPrimitives.WriteUInt16LittleEndian(r.AsSpan(k * 2), (ushort)q.Points[k]);
                    BinaryPrimitives.WriteUInt16LittleEndian(r.AsSpan(8), (ushort)q.Colour);
                    BinaryPrimitives.WriteUInt16LittleEndian(r.AsSpan(10), (ushort)q.Points[0]);
                    return r;
                }).ToList()));
            }
            var bytes = p.Write();
            // (her box: round her as she stands now -- no reflection behind her)
            var w = p.World();
            var box = new[] { w.Min(v => v.X), w.Max(v => v.X), w.Min(v => v.Y), w.Max(v => v.Y), w.Min(v => v.Z), w.Max(v => v.Z) };
            for (var k = 0; k < 6; k++) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8 + k * 4), (int)Math.Round(k % 2 == 0 ? Math.Floor(box[k]) : Math.Ceiling(box[k])));
            return bytes;
        }
    }
}
