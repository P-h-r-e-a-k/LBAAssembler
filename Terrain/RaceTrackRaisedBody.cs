using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The pieces of a raised road (RaceTrackPlan.Raised): a road that stands in the air on piers, following the plan's heights at any grade --
// Celebration Island's lap winds up round the statue and comes back down over itself, which the engine's ground, one height map, cannot do.
// Each piece is a decor body made for its own place: a few cells of road surface between cross-sections of the lap (so bends and grades
// are in the mesh itself and the decor needs no turn), and the piers that carry it. Unlike the flat bridge deck (RaceTrackDeckBody), whose
// collision box is its floor, these carry nothing themselves: a decor's box is level and axis-aligned, and a sloping road made of them
// would be a staircase the engine drops a car down step by step. The engine's race-track mode has the road as a floor of its own
// (RACEMOD.CPP, from RACETRACK.JSON's Raised).
internal static class RaceTrackRaisedBody
{
    // the deck's colours: the same greys, red and white as the flat deck's (RaceTrackDeckBody; the island palettes share them)
    public const int Asphalt = RaceTrackDeckBody.TopColour, Red = RaceTrackDeckBody.RailColour, White = RaceTrackDeckBody.WhiteColour;
    public const int RailTop = 58, RailSide = 56, Side = 51, Under = 50, PierLight = 56, PierDark = 53, PierCap = 55;
    // the arrows: the orange of the ground's own (the island palettes' fifth ramp)
    public const int Arrow = 89;
    // the slab's thickness and the rail's height (world units)
    public const double Thickness = 200, Rail = 220;

    private sealed class Mesh
    {
        public readonly List<Vector3> Points = new();
        public readonly List<Face> Faces = new();
        public int P(Vector3 v) { Points.Add(v); return Points.Count - 1; }

        // a quad whose right-hand normal points the way `outward` does (the engine culls faces seen from behind)
        public void Quad(int a, int b, int c, int d, int colour, Vector3 outward)
        {
            var n = Vector3.Cross(Points[b] - Points[a], Points[c] - Points[a]);
            Faces.Add(Vector3.Dot(n, outward) >= 0 ? new Face(new[] { a, b, c, d }, colour) : new Face(new[] { d, c, b, a }, colour));
        }

        public void Tri(int a, int b, int c, int colour, Vector3 outward)
        {
            var n = Vector3.Cross(Points[b] - Points[a], Points[c] - Points[a]);
            Faces.Add(Vector3.Dot(n, outward) >= 0 ? new Face(new[] { a, b, c }, colour) : new Face(new[] { c, b, a }, colour));
        }

        public byte[] Write() => new Body
        {
            Game = 2, Static = true, Lit = false, Header = new byte[96],
            Vertices = Points,
            Bones = new List<Bone> { new(0, Points.Count, 0, -1, new byte[8]) },
            Faces = Faces,
        }.Write();
    }

    // One piece of road: `sections` are cross-sections along the lap, each its middle (from the body's origin, in world axes, y up) and the
    // unit vector across the road; `firstBlock` numbers the curb's red and white blocks so they run on from piece to piece. Widths in world
    // units from the middle: the asphalt's edge, the curb's outer edge (where the rail begins), the road's edge. `arrow`: a piece of
    // four cells with an arrow on its asphalt, pointing the way the sections run -- its head two cells long, cut into the asphalt (the
    // cells before and after it are cut to meet its corners, so no face's edge ends part-way along another's: the engine leaves a
    // seam of missing pixels there).
    public static byte[] Tile(IReadOnlyList<(Vector3 Mid, Vector3 Across)> sections, int firstBlock, double asphalt, double curb, double edge, bool arrow = false)
    {
        var m = new Mesh();
        // across each section, from one edge to the other: the offset and the height over the road's surface
        var profile = new (double U, double Y)[]
        {
            (-edge, -Thickness), (-edge, Rail), (-curb, Rail), (-curb, 0), (-asphalt, 0),
            (asphalt, 0), (curb, 0), (curb, Rail), (edge, Rail), (edge, -Thickness),
        };
        var at = new int[sections.Count, profile.Length];
        for (var j = 0; j < sections.Count; j++)
            for (var k = 0; k < profile.Length; k++)
                at[j, k] = m.P(sections[j].Mid + sections[j].Across * (float)profile[k].U + new Vector3(0, (float)profile[k].Y, 0));
        var up = Vector3.UnitY;
        for (var j = 0; j + 1 < sections.Count; j++)
        {
            var across = Vector3.Normalize(sections[j].Across + sections[j + 1].Across);
            var block = (firstBlock + j) % 2 == 0 ? Red : White;
            void Strip(int k, int colour, Vector3 outward) => m.Quad(at[j, k], at[j, k + 1], at[j + 1, k + 1], at[j + 1, k], colour, outward);
            Strip(0, Side, -across);          // the outer side, slab and rail
            Strip(1, RailTop, up);
            Strip(2, RailSide, across);       // the rail's inner face
            Strip(3, block, up);              // the curb
            if (!(arrow && sections.Count == 5)) Strip(4, Asphalt, up);
            Strip(5, block, up);
            Strip(6, RailSide, -across);
            Strip(7, RailTop, up);
            Strip(8, Side, across);
            m.Quad(at[j, 9], at[j, 0], at[j + 1, 0], at[j + 1, 9], Under, -up);   // the underside
        }
        if (arrow && sections.Count == 5)
        {
            int L(int j) => at[j, 4]; int R(int j) => at[j, 5];
            int On(int j, double u) => m.P(sections[j].Mid + sections[j].Across * (float)u);
            var h = asphalt * 0.5;
            int a1 = On(1, -h), b1 = On(1, h), a2 = On(2, -h / 2), b2 = On(2, h / 2), tip = On(3, 0);
            // the cell before the arrow, meeting the corners of its base
            m.Quad(L(0), R(0), b1, a1, Asphalt, up); m.Tri(L(0), a1, L(1), Asphalt, up); m.Tri(R(0), R(1), b1, Asphalt, up);
            // the head: its wide half, then its point
            m.Quad(a1, b1, b2, a2, Arrow, up); m.Quad(L(1), a1, a2, L(2), Asphalt, up); m.Quad(b1, R(1), R(2), b2, Asphalt, up);
            m.Tri(a2, b2, tip, Arrow, up); m.Quad(L(2), a2, tip, L(3), Asphalt, up); m.Quad(b2, R(2), R(3), tip, Asphalt, up);
            // the cell after it, meeting its point
            m.Tri(L(3), tip, L(4), Asphalt, up); m.Tri(tip, R(3), R(4), Asphalt, up); m.Tri(tip, R(4), L(4), Asphalt, up);
        }
        return m.Write();
    }

    // A pier: a square column `half` wide each way from the ground (the body's origin, y 0) up to `height`, and on it a beam `beam` long
    // each way across the road and `thick` high, along `across`. The column's sides are in two greys, so it reads as a solid from any side.
    public static byte[] Pier(double height, double half, Vector3 across, double beam, double thick, double beamHalf)
    {
        var m = new Mesh();
        var h = (float)Math.Max(thick + 50, height); var t = (float)thick; var a = (float)half;
        var along = new Vector3(-across.Z, 0, across.X);
        void Box(Vector3 centre, Vector3 u, Vector3 v, float y0, float y1, int light, int dark, int top)
        {
            var c = new[] { centre - u - v, centre + u - v, centre + u + v, centre - u + v };
            var lo = c.Select(p => m.P(p + new Vector3(0, y0, 0))).ToArray(); var hi = c.Select(p => m.P(p + new Vector3(0, y1, 0))).ToArray();
            for (var k = 0; k < 4; k++)
            {
                var n = (k + 1) % 4;
                var outward = (c[k] + c[n]) / 2 - centre;
                m.Quad(lo[k], lo[n], hi[n], hi[k], k % 2 == 0 ? light : dark, outward);
            }
            m.Quad(hi[0], hi[1], hi[2], hi[3], top, Vector3.UnitY);
            m.Quad(lo[0], lo[1], lo[2], lo[3], dark, -Vector3.UnitY);
        }
        Box(Vector3.Zero, across * a, along * a, 0, h - t, PierLight, PierDark, PierDark);
        Box(Vector3.Zero, across * (float)beam, along * (float)beamHalf, h - t, h, PierCap, PierDark, PierCap);
        return m.Write();
    }
}
