using System.IO;
using System.Numerics;
using LbaBodyStudio;
using LBAAssembler.Characters;

namespace LBAAssembler.Terrain;

// Stella Entities' seven performers (docs/LBRA_STRIP_JOINT_DESIGN.md; the user, 2026-11-11: "a few female characters of different species
// from both worlds, topless just wearing thongs" -- adult women only -- and "build ORIGINAL performer characters (new models)"). Each is a body
// of her own, hand-built the way PeachCustom is (rings on Twinsen's 19-bone rig, so Twinsen's own animations fit her), from one Spec: her
// skin, hair and thong colours, her build, and what makes her a Grobo, a rabbibunny, a Quetch, a Sup, a Franco or a wizard (ears, trunk, tail,
// crest, hat). A stage costume: the thong a band at the hip with a patch at the front, nothing more drawn.
//
// Installed as bodies of the hero's entity (0, Twinsen's own rig: his walk fits them) -- the generics Body0.. -- with animations of their own
// (Sway: dancing on the spot, arms rolling; Pole: one hand on the pole, leaning out and round it) beside his.
internal static class StellaPerformers
{
    public const int Entity = 0, Body0 = 70, Sway = 480, Pole = 481;

    // What a performer is: colours (ramp starts of the racing palette: see PeachCustom's notes -- each stays inside its ramp when lit), her
    // height and build (hips and shoulders, 1 = Peach's), and her kind's extras
    public enum Kind { Grobo, Rabbibunny, Quetch, Sup, Franco, Wizard }
    public sealed record Spec(string Name, Kind Kind, int Skin, int Hair, int Thong, int Accent, float Height = 1230, float Hips = 1, float Shoulders = 1, float Bust = 1);

    public static readonly Spec[] All =
    {
        new("a Grobo bather", Kind.Grobo, Skin: 54, Hair: 48, Thong: 76, Accent: 100, Height: 1180, Hips: 1.25f, Shoulders: 1.1f, Bust: 1.2f),
        new("a rabbibunny", Kind.Rabbibunny, Skin: 58, Hair: 56, Thong: 70, Accent: 76, Height: 1200, Hips: 1.05f, Shoulders: 0.9f),
        new("a Quetch", Kind.Quetch, Skin: 196, Hair: 197, Thong: 102, Accent: 76, Height: 1300, Hips: 0.9f, Shoulders: 0.9f, Bust: 0.8f),
        new("a bikini Sup", Kind.Sup, Skin: 36, Hair: 69, Thong: 70, Accent: 100),
        new("a stewardess Sup", Kind.Sup, Skin: 35, Hair: 48, Thong: 197, Accent: 102, Height: 1260, Hips: 0.95f),
        new("a Franco lady", Kind.Franco, Skin: 133, Hair: 100, Thong: 102, Accent: 70, Height: 1150, Hips: 1.15f, Shoulders: 1.05f, Bust: 1.1f),
        new("a wizard", Kind.Wizard, Skin: 35, Hair: 58, Thong: 197, Accent: 102, Height: 1250, Hips: 1.0f),
    };

    private static readonly int[] Parents = { -1, 0, 1, 2, 3, 4, 3, 6, 2, 8, 9, 2, 11, 12, 3, 14, 15, 3, 17 };

    public static Body Build(Body donor, Spec s)
    {
        float H = s.Height;
        var vertices = Enumerable.Range(0, 19).Select(_ => new List<Vector3>()).ToArray();
        var faces = new List<(int Bone, int[] Points, int Colour)>();
        int Skin = s.Skin, Hair = s.Hair, Thong = s.Thong, Accent = s.Accent;

        List<Vector3> R(int bone, float cx, float cy, float cz, float rx, float rz, int segments = 8)
        {
            var ring = new List<Vector3>();
            for (var j = 0; j < segments; j++)
            {
                var a = j * 2 * MathF.PI / segments;
                ring.Add(new Vector3(cx + rx * MathF.Cos(a), cy, cz + rz * MathF.Sin(a)));
            }
            vertices[bone].AddRange(ring);
            return ring;
        }
        void Chain(int bone, List<List<Vector3>> rings, int colour, bool capStart, bool capEnd)
        {
            var begin = vertices[bone].Count - rings.Sum(r => r.Count);
            if (begin < 0 || vertices[bone][begin] != rings[0][0]) throw new InvalidOperationException($"Chain(bone {bone}): rings aren't the trailing span.");
            var offsets = new int[rings.Count]; var pos = begin;
            for (var i = 0; i < rings.Count; i++) { offsets[i] = pos; pos += rings[i].Count; }
            for (var r = 0; r < rings.Count - 1; r++)
            {
                var n = rings[r].Count;
                for (var j = 0; j < n; j++)
                    faces.Add((bone, new[] { offsets[r] + j, offsets[r] + (j + 1) % n, offsets[r + 1] + (j + 1) % n, offsets[r + 1] + j }, colour));
            }
            if (capStart) { var n = rings[0].Count; for (var j = 1; j < n - 2; j += 2) faces.Add((bone, new[] { offsets[0], offsets[0] + j + 2, offsets[0] + j + 1, offsets[0] + j }, colour)); }
            if (capEnd) { var n = rings[^1].Count; var b = offsets[^1]; for (var j = 1; j < n - 2; j += 2) faces.Add((bone, new[] { b, b + j, b + j + 1, b + j + 2 }, colour)); }
        }
        // a pointed thing: a ring narrowing to a tip (a cone, an ear, a crest), its tip at `tip`
        void Cone(int bone, float cx, float cy, float cz, float rx, float rz, Vector3 tip, int colour, int segments = 6)
        {
            var ring = R(bone, cx, cy, cz, rx, rz, segments);
            var t = vertices[bone].Count; vertices[bone].Add(tip);
            var b = t - segments;
            for (var j = 0; j < segments; j++) faces.Add((bone, new[] { b + (j + 1) % segments, b + j, t }, colour));
            // (its base, facing down)
            for (var j = 1; j < segments - 2; j += 2) faces.Add((bone, new[] { b, b + j, b + j + 1, b + j + 2 }, colour));
        }

        float hipX = 70 * s.Hips, shX = 145 * s.Shoulders;
        // ---- legs: bare, the foot a bare foot (a dancer's); the Franco's and the Grobo's a little thicker
        float thick = s.Kind is Kind.Grobo or Kind.Franco ? 1.15f : 1f;
        void Leg(int thigh, int shin, int foot, int side)
        {
            float x = side * hipX;
            Chain(thigh, new() { R(thigh, x, H * .455f, 0, 48 * thick, 44 * thick), R(thigh, x, H * .29f, 0, 40 * thick, 38 * thick) }, Skin, true, false);
            var shinTop = R(shin, x, H * .29f, 0, 40 * thick, 38 * thick);
            var ankle = R(shin, x, H * .095f, 0, 32, 30);
            Chain(shin, new() { shinTop, ankle }, Skin, false, false);
            var footTop = R(foot, x, H * .095f, 0, 34, 32);
            var toe = R(foot, x, H * .01f, H * .10f, 38, 34);
            var sole = R(foot, x, H * .0f, H * .06f, 30, 32);
            Chain(foot, new() { footTop, toe, sole }, s.Kind == Kind.Rabbibunny ? Accent : Skin, false, true);
        }
        Leg(8, 9, 10, 1); Leg(11, 12, 13, -1);

        // ---- hips (2): skin, with the thong -- a band round the hips (coloured), and a patch at the front and a string at the back
        {
            var thigh = R(2, 0, H * .38f, 0, 118 * s.Hips, 90 * s.Hips, 10);
            var low = R(2, 0, H * .43f, 0, 150 * s.Hips, 110 * s.Hips, 10);
            Chain(2, new() { thigh, low }, Skin, false, false);
            var bandLow = R(2, 0, H * .47f, 0, 156 * s.Hips, 114 * s.Hips, 10);
            Chain(2, new() { low, bandLow }, Skin, false, false);
            var bandHigh = R(2, 0, H * .485f, 0, 154 * s.Hips, 113 * s.Hips, 10);
            Chain(2, new() { bandLow, bandHigh }, Thong, false, false);
            var waist = R(2, 0, H * .53f, 0, 128, 94, 10);
            Chain(2, new() { bandHigh, waist }, Skin, false, false);
            var top = R(2, 0, H * .58f, 0, 118, 88, 10);
            Chain(2, new() { waist, top }, Skin, false, false);
            // the patch: a triangle at the front, a hand's breadth, from the band down between the legs (standing off the skin a little)
            var p = vertices[2].Count;
            vertices[2].Add(new Vector3(-60 * s.Hips, H * .485f, 118 * s.Hips + 2)); vertices[2].Add(new Vector3(60 * s.Hips, H * .485f, 118 * s.Hips + 2));
            vertices[2].Add(new Vector3(0, H * .385f, 96 * s.Hips + 6));
            faces.Add((2, new[] { p + 1, p, p + 2 }, Thong));
            // (and the string behind: a short strip from the band into the cleft)
            var q = vertices[2].Count;
            vertices[2].Add(new Vector3(-14, H * .485f, -112 * s.Hips - 2)); vertices[2].Add(new Vector3(14, H * .485f, -112 * s.Hips - 2));
            vertices[2].Add(new Vector3(0, H * .40f, -88 * s.Hips - 4));
            faces.Add((2, new[] { q, q + 1, q + 2 }, Thong));
        }

        // ---- the torso (3): skin from the waist to the shoulders, her bust a swelling of the front, the neck
        {
            float bust = s.Bust;
            var bib = R(3, 0, H * .58f, 0, 118, 88, 10);
            var under = R(3, 0, H * .65f, 0, 124, 94 + 10 * bust, 10);
            Chain(3, new() { bib, under }, Skin, false, false);
            var chest = R(3, 0, H * .70f, 0, 128 * s.Shoulders, 100 + 26 * bust, 10);
            Chain(3, new() { under, chest }, Skin, false, false);
            var shoulders = R(3, 0, H * .775f, 0, 100 * s.Shoulders, 80, 10);
            Chain(3, new() { chest, shoulders }, Skin, false, false);
            var neck = R(3, 0, H * .815f, 0, 38, 34, 10);
            Chain(3, new() { shoulders, neck }, Skin, false, false);
        }
        // ---- arms: slender, bare
        void Arm(int upper, int fore, int side)
        {
            float x = side * shX;
            Chain(upper, new() { R(upper, x, H * .775f, 0, 40, 38), R(upper, x, H * .62f, 0, 34, 32) }, Skin, true, false);
            var foreTop = R(fore, x, H * .62f, 0, 34, 32);
            var wrist = R(fore, x, H * .48f, 0, 28, 26);
            Chain(fore, new() { foreTop, wrist }, Skin, false, false);
            var palm = R(fore, x, H * .40f, 0, 32, 30);
            var fist = R(fore, x, H * .345f, 0, 26, 24);
            Chain(fore, new() { wrist, palm, fist }, Skin, false, true);
        }
        Arm(4, 5, 1); Arm(6, 7, -1);

        // ---- the head (14) and what rides it (15, 16 above; 17 and 18 hang from the torso: the tail and the hair behind)
        bool bigHead = s.Kind is Kind.Grobo or Kind.Franco;
        {
            float w = bigHead ? 1.12f : 1f;
            var neck = R(14, 0, H * .815f, 0, 40, 36, 10);
            var jaw = R(14, 0, H * .85f, 0, 56 * w, 52 * w, 10);
            var cheeks = R(14, 0, H * .885f, 0, 62 * w, 58 * w, 10);
            var brow = R(14, 0, H * .925f, 0, 58 * w, 54 * w, 10);
            Chain(14, new() { neck, jaw, cheeks, brow }, Skin, false, false);
            var crown = R(14, 0, H * .965f, 0, 52 * w, 48 * w, 10);
            Chain(14, new() { brow, crown }, s.Kind == Kind.Wizard || s.Kind == Kind.Sup ? Skin : Skin, false, false);
            var top = R(14, 0, H * .985f, 0, 30 * w, 28 * w, 10);
            // (hair over the top of the head: the cap of it, in her hair's colour)
            Chain(14, new() { crown, top }, s.Kind is Kind.Grobo or Kind.Quetch ? Skin : Hair, false, true);
            // the nose, small; the lips, a patch in the thong's colour
            var nb = vertices[14].Count;
            vertices[14].Add(new Vector3(-14, H * .895f, 62 * w)); vertices[14].Add(new Vector3(14, H * .895f, 62 * w));
            vertices[14].Add(new Vector3(12, H * .876f, 58 * w)); vertices[14].Add(new Vector3(-12, H * .876f, 58 * w));
            var nt = vertices[14].Count;
            vertices[14].Add(new Vector3(0, H * .885f, 78 * w));
            faces.Add((14, new[] { nb + 1, nb, nt }, Skin)); faces.Add((14, new[] { nb + 2, nb + 1, nt }, Skin));
            faces.Add((14, new[] { nb + 3, nb + 2, nt }, Skin)); faces.Add((14, new[] { nb, nb + 3, nt }, Skin));
            var lb = vertices[14].Count;
            vertices[14].Add(new Vector3(-15, H * .869f, 54 * w)); vertices[14].Add(new Vector3(15, H * .869f, 54 * w));
            vertices[14].Add(new Vector3(17, H * .853f, 50 * w)); vertices[14].Add(new Vector3(-17, H * .853f, 50 * w));
            faces.Add((14, new[] { lb + 3, lb + 2, lb + 1, lb }, Accent));
            // eyes: two dark patches
            foreach (var side in new[] { -1, 1 })
            {
                var e = vertices[14].Count;
                float ex = side * 26;
                vertices[14].Add(new Vector3(ex - 8, H * .913f, 56 * w)); vertices[14].Add(new Vector3(ex + 8, H * .913f, 56 * w));
                vertices[14].Add(new Vector3(ex + 8, H * .901f, 56 * w)); vertices[14].Add(new Vector3(ex - 8, H * .901f, 56 * w));
                faces.Add((14, new[] { e + 3, e + 2, e + 1, e }, 48));
            }
            float hy = H * .93f;
            switch (s.Kind)
            {
                case Kind.Grobo:
                    // big flat ears out at the sides, and a short trunk at the front hanging to the chin
                    foreach (var side in new[] { -1, 1 })
                    {
                        var e = vertices[14].Count;
                        vertices[14].Add(new Vector3(side * 62, hy + 30, 0)); vertices[14].Add(new Vector3(side * 62, hy - 30, 0));
                        vertices[14].Add(new Vector3(side * 150, hy - 50, -20)); vertices[14].Add(new Vector3(side * 160, hy + 60, -20));
                        faces.Add((14, side > 0 ? new[] { e, e + 1, e + 2, e + 3 } : new[] { e + 3, e + 2, e + 1, e }, Accent));
                        faces.Add((14, side > 0 ? new[] { e + 3, e + 2, e + 1, e } : new[] { e, e + 1, e + 2, e + 3 }, Accent));
                    }
                    Chain(14, new() { R(14, 0, H * .905f, 62, 18, 16, 6), R(14, 0, H * .85f, 90, 15, 14, 6), R(14, 0, H * .81f, 96, 12, 12, 6) }, Skin, true, true);
                    break;
                case Kind.Rabbibunny:
                    // long ears up from the crown, bent back a little; a round white tail behind (on the hips' bone, below)
                    foreach (var side in new[] { -1, 1 })
                    {
                        Cone(14, side * 34, H * .97f, -6, 22, 14, new Vector3(side * 56, H * 1.20f, -52), Hair, 6);
                        Cone(14, side * 34, H * .975f, 2, 12, 8, new Vector3(side * 56, H * 1.17f, -40), Accent, 4);
                    }
                    break;
                case Kind.Quetch:
                    // a tall crest of cartilage from brow to nape
                    Cone(14, 0, H * .98f, 0, 18, 60, new Vector3(0, H * 1.12f, -90), Hair, 6);
                    foreach (var side in new[] { -1, 1 })
                        Cone(14, side * 56, hy, 0, 12, 14, new Vector3(side * 108, hy + 70, -10), Skin, 4);
                    break;
                case Kind.Sup:
                    // long hair falling behind, down her back (a ponytail, or the stewardess's let loose)
                    Chain(14, new() { R(14, 0, H * .97f, -50, 40, 24, 6), R(14, 0, H * .88f, -76, 38, 24, 6), R(14, 0, H * .76f, -86, 36, 22, 6), R(14, 0, H * .66f, -80, 22, 14, 6) }, Hair, true, true);
                    break;
                case Kind.Franco:
                    // pointed ears, a bun of hair
                    foreach (var side in new[] { -1, 1 })
                        Cone(14, side * 58, hy, 0, 14, 14, new Vector3(side * 130, hy + 56, -10), Skin, 4);
                    Chain(14, new() { R(14, 0, H * .99f, -30, 36, 36, 6), R(14, 0, H * 1.04f, -34, 42, 40, 6), R(14, 0, H * 1.08f, -30, 24, 22, 6) }, Hair, true, true);
                    break;
                case Kind.Wizard:
                    // long silver hair behind her; the hat -- a brim and a tall bent cone -- on bone 15 below
                    Chain(14, new() { R(14, 0, H * .97f, -46, 44, 28, 6), R(14, 0, H * .86f, -78, 42, 26, 6), R(14, 0, H * .72f, -90, 40, 24, 6), R(14, 0, H * .58f, -80, 24, 14, 6) }, Hair, true, true);
                    break;
            }
        }
        if (s.Kind == Kind.Wizard)
        {
            Chain(15, new() { R(15, 0, H * .975f, 0, 110, 104, 10), R(15, 0, H * .99f, 0, 96, 90, 10) }, 197, true, false);
            var band = R(15, 0, H * .99f, 0, 54, 50, 10);
            var mid = R(15, 0, H * 1.07f, -10, 40, 38, 10);
            Chain(15, new() { band, mid }, 197, false, false);
            Cone(15, 0, H * 1.07f, -10, 40, 38, new Vector3(0, H * 1.22f, -70), 197, 10);
            // a gold band and a star on the front
            Chain(15, new() { R(15, 0, H * .995f, 0, 57, 53, 10), R(15, 0, H * 1.02f, -2, 52, 48, 10) }, Accent, false, false);
        }
        if (s.Kind == Kind.Rabbibunny)
        {
            // the tail: a round puff behind, on the torso's lower bone (17 hangs from the torso, so it follows her spine; the hips' own 2 keeps it on the thong)
            Chain(2, new() { R(2, 0, H * .46f, -138, 38, 34, 6), R(2, 0, H * .50f, -170, 52, 46, 6), R(2, 0, H * .54f, -138, 34, 30, 6) }, Hair, true, true);
        }
        if (s.Kind == Kind.Wizard)
        {
            // stars in her thong's band: gold patches at the front of the hips
            foreach (var x in new[] { -90f, 0f, 90f })
            {
                var st = vertices[2].Count;
                float zf = 112 * s.Hips + 4 - Math.Abs(x) * 0.25f;
                vertices[2].Add(new Vector3(x - 10, H * .515f, zf)); vertices[2].Add(new Vector3(x + 10, H * .515f, zf));
                vertices[2].Add(new Vector3(x + 10, H * .50f, zf)); vertices[2].Add(new Vector3(x - 10, H * .50f, zf));
                faces.Add((2, new[] { st + 3, st + 2, st + 1, st }, Accent));
            }
        }

        var origins = new Vector3[19];
        origins[0] = Vector3.Zero; origins[1] = new Vector3(0, H * .455f, 0); origins[2] = new Vector3(0, H * .455f, 0);
        origins[3] = new Vector3(0, H * .58f, 0); origins[4] = new Vector3(shX, H * .775f, 0); origins[5] = new Vector3(shX, H * .62f, 0);
        origins[6] = new Vector3(-shX, H * .775f, 0); origins[7] = new Vector3(-shX, H * .62f, 0);
        origins[8] = new Vector3(hipX, H * .455f, 0); origins[9] = new Vector3(hipX, H * .29f, 0); origins[10] = new Vector3(hipX, H * .095f, 0);
        origins[11] = new Vector3(-hipX, H * .455f, 0); origins[12] = new Vector3(-hipX, H * .29f, 0); origins[13] = new Vector3(-hipX, H * .095f, 0);
        origins[14] = new Vector3(0, H * .815f, 0); origins[15] = new Vector3(0, H * .975f, 0);
        origins[16] = new Vector3(0, H * 1.04f, 0); origins[17] = new Vector3(0, H * .58f, 0); origins[18] = new Vector3(0, H * .58f, 0);
        for (var i = 0; i < 19; i++) if (vertices[i].Count == 0) vertices[i].Add(origins[i]);
        var pivots = new int[19];
        for (var i = 1; i < 19; i++) { pivots[i] = vertices[Parents[i]].Count; vertices[Parents[i]].Add(origins[i]); }

        var body = new Body { Game = 2, Header = (byte[])donor.Header.Clone(), Lit = true };
        var starts = new int[19];
        for (var i = 0; i < 19; i++)
        {
            starts[i] = body.Vertices.Count;
            body.Vertices.AddRange(vertices[i]);
            var pivot = i == 0 ? 0 : starts[Parents[i]] + pivots[i];
            body.Bones.Add(new(starts[i], vertices[i].Count, pivot, Parents[i], new byte[38]));
        }
        foreach (var f in faces) body.Faces.Add(new(f.Points.Select(p => p + starts[f.Bone]).ToArray(), f.Colour));
        body.SetWorld(body.Vertices.ToArray());
        body.Validate();
        return body;
    }

    // ---- animations ------------------------------------------------------------------------------------------------------------

    // Bone slots of the rig: 2 hips, 3 torso, 4/5 her right arm (upper, fore), 6/7 left, 8-10 right leg, 11-13 left leg, 14 head
    // Dancing on the spot: the hips rolling side to side, the torso answering, the arms swaying over her head
    public static CharacterAnim SwayAnim()
    {
        var a = CharacterAnim.Still(19, Sway, "stella sway");
        a.Keys.Clear();
        (float Hip, float Lean, float Arms, float Knee)[] beats = { (14, -8, 40, 6), (0, 0, 80, 0), (-14, 8, 40, 6), (0, 0, 80, 0) };
        foreach (var (hip, lean, arms, knee) in beats)
        {
            var k = new CharacterAnim.Key { Milliseconds = 450, Bones = Enumerable.Range(0, 19).Select(_ => (0, Vector3.Zero)).ToList() };
            void Set(int bone, float x, float y, float z) => k.Bones[bone] = (0, new Vector3(x, y, z));
            Set(2, 0, hip, hip * 0.35f); Set(3, 0, -hip * 0.6f, lean);
            Set(4, 0, 0, 150 - arms * 0.2f); Set(6, 0, 0, -150 + arms * 0.2f);
            Set(5, 0, 0, 20); Set(7, 0, 0, -20);
            Set(14, 0, hip * 0.5f, -lean * 0.5f);
            Set(9, knee, 0, 0); Set(12, knee, 0, 0);
            a.Keys.Add(k);
        }
        a.LoopFrame = 0;
        return a;
    }

    // At the pole: one hand up on it, the body leaning out and round, the hips swinging, the other hand trailing
    public static CharacterAnim PoleAnim()
    {
        var a = CharacterAnim.Still(19, Pole, "stella pole");
        a.Keys.Clear();
        (float Turn, float Lean, float Hip)[] beats = { (0, 14, 10), (35, 22, 0), (0, 12, -10), (-35, 22, 0) };
        foreach (var (turn, lean, hip) in beats)
        {
            var k = new CharacterAnim.Key { Milliseconds = 520, Bones = Enumerable.Range(0, 19).Select(_ => (0, Vector3.Zero)).ToList() };
            void Set(int bone, float x, float y, float z) => k.Bones[bone] = (0, new Vector3(x, y, z));
            Set(2, 0, hip, hip * 0.4f); Set(3, -lean, turn * 0.5f, 0);
            Set(4, 0, 0, 165); Set(5, 0, 0, 10);        // the right hand up on the pole
            Set(6, 20, 0, -40 - turn * 0.4f); Set(7, 0, 0, -25);  // the left hand trailing
            Set(14, 6, turn * 0.4f, 0);
            Set(8, 0, 0, hip * 0.5f); Set(9, 18, 0, 0); Set(11, -10, 0, -hip * 0.5f); Set(12, 6, 0, 0);
            a.Keys.Add(k);
        }
        a.LoopFrame = 0;
        return a;
    }

    // ---- installing --------------------------------------------------------------------------------------------------------------

    // Into the game folder: the seven bodies and two animations at the ends of BODY.HQR and ANIM.HQR, given to entity 0 as bodies Body0..Body0+6
    // and animations Sway and Pole. Lines for the build's log.
    public static List<string> Install(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        var hero = Lba2EntityTable.Parse(table).Entities.FirstOrDefault(e => e.Id == Entity);
        if (hero is null || RaceTrackGuards.BodyOf(table, Entity, 0) is not { } twinsen)
            return new() { "WARNING: Stella Entities' performers: no hero entity -- none made" };
        if (hero.Bodies.Any(b => b.Generic >= Body0 && b.Generic < Body0 + All.Length) || hero.Anims.Any(a => a.Generic is Sway or Pole))
            return new() { $"WARNING: Stella Entities' performers: entity {Entity} already has bodies {Body0}-{Body0 + All.Length - 1} or animations {Sway}/{Pole} -- none made" };
        var donor = Body.Read(HqrArchive.Open(bodyPath).Read(twinsen), 2);
        var file = File.ReadAllBytes(bodyPath);
        var firstBody = HqrArchive.CountEntries(bodyPath);
        for (var k = 0; k < All.Length; k++)
        {
            file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(Build(donor, All[k]).Write()));
            table = RaceTrackBaldinoCar.WithBody(table, Entity, Body0 + k, firstBody + k);
        }
        File.WriteAllBytes(bodyPath, file);
        var animIndex = HqrArchive.CountEntries(animPath);
        var anims = HqrWriter.AppendEntry(File.ReadAllBytes(animPath), HqrWriter.StoredEntry(SwayAnim().Write(19)));
        File.WriteAllBytes(animPath, HqrWriter.AppendEntry(anims, HqrWriter.StoredEntry(PoleAnim().Write(19))));
        table = RaceTrackJumpAnim.WithAnim(table, Entity, Sway, animIndex);
        table = RaceTrackJumpAnim.WithAnim(table, Entity, Pole, animIndex + 1);
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return new() { $"Stella Entities' performers: {string.Join(", ", All.Select(s => s.Name))} -- BODY.HQR {firstBody}-{firstBody + All.Length - 1}, entity {Entity}'s bodies {Body0}-{Body0 + All.Length - 1}; sway and pole ANIM.HQR {animIndex}, {animIndex + 1}, its animations {Sway}, {Pole}" };
    }
}
