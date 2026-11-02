using System.IO;
using System.Numerics;
using LbaBodyStudio;
using LBAAssembler.Terrain.Palace;
using LBAAssembler.Terrain.Snow;

namespace LBAAssembler.Terrain.Harvest;

// The Long Table's things, each a body of flat-coloured faces lit by the cubes' sun (SnowMesh), placed as drawn-only decors: the road is the
// only floor on the island and its rails hold the cars, so nothing here needs a collision box. The table itself is built here (Table), the
// trees and the rest come from the design's lists (HarvestDesign.Things).
internal static class HarvestObjects
{
    public const int MaxPerCube = IslandDecors.MaxPerCubeOwnEngine;
    private const double Cell = IslandFile.CellSize;
    // copies in each cube within this: the table and the mill are seen from across the island
    private const double FarReach = IslandFile.CubeSize;

    public static readonly Rgb Bark = new(72, 48, 30), BarkLight = new(110, 76, 46), MapleRed = new(214, 62, 32), MapleOrange = new(232, 110, 30), MapleGold = new(245, 170, 40),
        Wood = new(150, 110, 70), WoodDark = new(110, 76, 46), Cream = new(244, 232, 200), Cranberry = new(170, 30, 42), Pale = new(250, 208, 92);

    // (what is placed, as it is placed: the harvestclear tool's check of it against the road)
    public sealed record Placed(string What, SnowMesh Mesh, double U, double V, double Y, int Beta);
    public static List<Placed>? Recording;

    private sealed class Placer(IslandFile? island, SnowShading shading)
    {
        public string What = "";
        public readonly List<byte[]> Bodies = new() { RaceTrackPipes.Write(new List<Vector3> { Vector3.Zero }, new List<Face>(), lit: false) };
        public int Decors, Full;
        public SnowMesh Mesh() => new(shading);
        public int Add(SnowMesh mesh, string what) { Bodies.Add(mesh.Write(what)); return Bodies.Count - 1; }
        public void Place(int body, SnowMesh mesh, double u, double v, double y, int beta = 0, double reach = 0)
        {
            Recording?.Add(new Placed(What, mesh, u, v, y, beta));
            if (island is null) return;
            var (lo, hi) = mesh.Bounds();
            double wx = HarvestDesign.WorldX(u), wz = HarvestDesign.WorldZ(v);
            double x0 = lo.X, x1 = hi.X, z0 = lo.Z, z1 = hi.Z;
            if (beta != 0)
            {
                var r = mesh.Points.Max(p => Math.Sqrt(p.X * p.X + p.Z * p.Z));
                x0 = z0 = -r; x1 = z1 = r;
            }
            var (placed, full) = BrickBuilding.AddToCubes(island, body, beta & 0xFFF, wx, (int)Math.Round(y), wz, wx + x0, wx + x1, (int)Math.Floor(y + lo.Y), BrickBuilding.NoBoxTop,
                wz + z0, wz + z1, MaxPerCube, reach);
            Decors += placed; Full += full;
        }
    }

    public sealed record Built(List<byte[]> Bodies, List<string> Log);

    public static Built Build(IslandFile? island, SnowShading shading, HarvestDesign design)
    {
        var at = new Placer(island, shading);
        var log = new List<string>();
        foreach (var (what, make) in new (string, Func<Placer, SnowMesh>)[]
        {
            ("maple0", p => Maple(p, MapleRed)), ("maple1", p => Maple(p, MapleOrange)), ("maple2", p => Maple(p, MapleGold)), ("oak", Oak),
        })
        {
            if (!design.Things.TryGetValue(what, out var list)) continue;
            var mesh = make(at);
            var body = at.Add(mesh, what);
            at.What = what;
            foreach (var t in list) at.Place(body, mesh, t.X, t.Z, t.Y, t.Turn);
            log.Add($"{list.Count} {what}");
        }
        // the rest of the design's lists, each model once; the big ones seen from further
        foreach (var (what, list) in design.Things.Where(t => !t.Key.StartsWith("maple") && t.Key is not "oak" and not "horn"))
        {
            var mesh = HarvestBodies.Make(what, shading) ?? throw new InvalidDataException($"The Long Table has no model for its {what}.");
            var body = at.Add(mesh, what);
            var reach = what is "ferris" or "mill" or "chair" or "board" or "ship" or "turkey" or "mash" ? FarReach : 0;
            at.What = what;
            foreach (var t in list) at.Place(body, mesh, t.X, t.Z, t.Y, t.Turn, reach);
        }
        at.What = "the table";
        Table(at);
        at.What = "the horn";
        if (design.Things.TryGetValue("horn", out var horn) && horn.Count > 0) Horn(at, horn[0]);
        log.Add($"the things: {at.Bodies.Count} bodies, {at.Decors} decors{(at.Full > 0 ? $", {at.Full} left out, their cubes full" : "")}");
        return new Built(at.Bodies, log);
    }

    private static SnowMesh Maple(Placer at, Rgb colour)
    {
        var m = at.Mesh();
        m.Cylinder(new Vector3(0, -100, 0), new Vector3(0, 1500, 0), 240, 150, 5, Bark);
        m.Ellipsoid(new Vector3(0, 2500, 0), 1400, 1500, 1400, 4, 7, colour, (i, k) => 1 + 0.12 * Math.Sin(i * 2.1 + k * 1.7));
        return m;
    }

    private static SnowMesh Oak(Placer at)
    {
        var m = at.Mesh();
        m.Cylinder(new Vector3(0, -100, 0), new Vector3(0, 1800, 0), 260, 140, 5, Bark);
        for (var k = 0; k < 5; k++)
        {
            var a = k * 1.256; var tip = new Vector3((float)(Math.Cos(a) * 1100), 3000 + 300 * (k % 2), (float)(Math.Sin(a) * 1100));
            m.Cylinder(new Vector3(0, 1500, 0), tip, 110, 30, 3, BarkLight);
        }
        return m;
    }

    // The horn of plenty: a woven cone over the road, its tip at the top of the Maple Run's cloth, widening to its mouth on the table -- the car goes in at
    // the narrow end and comes out onto the feast. Rings of two-sided faces over the road's width and over it, none under it (the road is its floor);
    // `t.Scale`: how far the road climbs from the tip to the mouth.
    private static void Horn(Placer at, HarvestDesign.Thing t)
    {
        const int Rings = 11, Sides = 9; const double Half = 17, Tip = 8.0, Mouth = 15.5;
        var m = at.Mesh();
        (Vector3 Middle, double R) Ring(int i)
        {
            var f = i / (double)(Rings - 1);
            var r = (Tip + (Mouth - Tip) * f * f) * Cell;
            var y = -t.Scale * (1 - f) * (1 - f) * 0 - t.Scale * (1 - f) + r * 0.46;
            return (new Vector3((float)((Half - f * 2 * Half) * Cell), (float)y, 0), r);
        }
        Vector3 P(int i, int k)
        {
            var (o, r) = Ring(i);
            var a = -0.28 + (Math.PI + 0.56) * k / Sides;
            return o + new Vector3(0, (float)(r * Math.Sin(a)), (float)(r * Math.Cos(a)));
        }
        Rgb Light = new(216, 190, 120), Band = new(190, 150, 90), Dark = new(150, 110, 70);
        for (var i = 0; i + 1 < Rings; i++)
            for (var k = 0; k < Sides; k++)
                m.TwoSided(i % 3 == 2 ? Dark : (i + k) % 2 == 0 ? Light : Band, P(i, k), P(i, k + 1), P(i + 1, k + 1), P(i + 1, k));
        at.Place(at.Add(m, "the horn of plenty"), m, t.X, t.Z, t.Y, reach: FarReach);
    }

    // The table: a top of gingham squares of 4 cells, thick, whose cloth hangs over its long sides, and hangs to the ground at its east end, the
    // Maple Run's ramp under it; legs at its corners and two more at its middle; chairs down both sides, the road between a chair's legs.
    public const double TableX0 = 118, TableZ0 = 142, TableX1 = 280, TableZ1 = 178, TableTop = 6200 - 70, TableDepth = 520;

    private static void Table(Placer at)
    {
        const int Across = 4;
        var pieces = (int)Math.Ceiling((TableX1 - TableX0) / 40);
        for (var p = 0; p < pieces; p++)
        {
            var m = at.Mesh();
            double u0 = TableX0 + p * 40, u1 = Math.Min(TableX1, u0 + 40), mid = (u0 + u1) / 2, vm = (TableZ0 + TableZ1) / 2;
            float W(double u) => (float)((u - mid) * Cell);
            float D(double v) => (float)((v - vm) * Cell);
            for (var iz = 0; TableZ0 + iz * Across < TableZ1; iz++)
                for (var ix = 0; u0 + ix * Across < u1; ix++)
                {
                    double a = u0 + ix * Across, b = Math.Min(u1, a + Across), c = TableZ0 + iz * Across, d = Math.Min(TableZ1, c + Across);
                    var colour = (ix + iz + p * 10) % 2 == 0 ? Cream : new Rgb(206, 72, 58);
                    m.Poly(colour, Vector3.UnitY, new Vector3(W(a), 0, D(c)), new Vector3(W(a), 0, D(d)), new Vector3(W(b), 0, D(d)), new Vector3(W(b), 0, D(c)));
                }
            // (its underside, dark, and the cloth's hem down each long side)
            m.Poly(WoodDark, -Vector3.UnitY, new Vector3(W(u0), -(float)TableDepth, D(TableZ0)), new Vector3(W(u1), -(float)TableDepth, D(TableZ0)), new Vector3(W(u1), -(float)TableDepth, D(TableZ1)), new Vector3(W(u0), -(float)TableDepth, D(TableZ1)));
            // (in the cloth's squares: all one cream it read as a grey concrete beam from under the table)
            for (var ix = 0; u0 + ix * Across < u1; ix++)
            {
                double a = u0 + ix * Across, b = Math.Min(u1, a + Across);
                var hem = (ix + p * 10) % 2 == 0 ? new Rgb(206, 72, 58) : Cream;
                m.Poly(hem, -Vector3.UnitZ, new Vector3(W(a), 0, D(TableZ0)), new Vector3(W(b), 0, D(TableZ0)), new Vector3(W(b), -1400, D(TableZ0)), new Vector3(W(a), -1400, D(TableZ0)));
                m.Poly(hem, Vector3.UnitZ, new Vector3(W(b), 0, D(TableZ1)), new Vector3(W(a), 0, D(TableZ1)), new Vector3(W(a), -1400, D(TableZ1)), new Vector3(W(b), -1400, D(TableZ1)));
            }
            at.Place(at.Add(m, "the table"), m, mid, vm, TableTop, reach: FarReach);
        }
        // the legs: turned, each drawn from its foot
        var leg = at.Mesh();
        var legTop = (float)(TableTop - TableDepth - 700);
        leg.Cylinder(new Vector3(0, 0, 0), new Vector3(0, legTop * 0.2f, 0), 1100, 800, 8, WoodDark);
        leg.Cylinder(new Vector3(0, legTop * 0.2f, 0), new Vector3(0, legTop * 0.3f, 0), 800, 1300, 8, Wood);
        leg.Cylinder(new Vector3(0, legTop * 0.3f, 0), new Vector3(0, legTop, 0), 1000, 1100, 8, WoodDark);
        var legBody = at.Add(leg, "a table leg");
        foreach (var (u, v) in new[] { (124.0, 148.0), (124.0, 172.0), (274.0, 148.0), (274.0, 172.0), (160.0, 160.0), (232.0, 160.0) })
            at.Place(legBody, leg, u, v, 700, reach: FarReach);
    }
}
