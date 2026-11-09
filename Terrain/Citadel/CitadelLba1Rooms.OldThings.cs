using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The secret chamber's lower room as a store of old things (the user, 2026-11-09: "The lower area, let's fill it with some random clutter
// such as Twinsen and Zoe's bikes, an old snowboard (we can copy the one from LBA1), and a few other old items from LBA1"): LBA1's own
// inventory objects (its INVOBJ.HQR, drawn in the inventory some ten times life size) at life size -- the snowboard he rode down the
// Hamalayi slopes and the space guitar leant against the wall, the protopack, the meca-penguin, the old holomap and Gawley's horn on the
// floor, the pirate flag hung on the wall -- each the mushroom's entity's body OldThingFirst on, as the seal's parts are. Their colours
// each moved to the LBA2 ramp nearest their own (RaceTrackCharacterCars.Lba1ToLba2Colours, as the digger's); LBA1's lit faces lit.
internal static partial class CitadelLba1Rooms
{
    public const int OldThingFirst = 44;
    // (the things: INVOBJ.HQR's entry, its name, its size -- its longest side, in scene units -- and how it stands: on the floor as it
    // is, leant against a wall at its back (z 0) -- the snowboard stood up on its tail, its deck out -- or hung flat on one)
    public enum OldStand { Floor, Lean, StoodUp, Hung }
    public static readonly (int Entry, string Name, float Size, OldStand Stand)[] OldThings =
    {
        (13, "LBA1's snowboard", 1400, OldStand.StoodUp),
        (18, "LBA1's space guitar", 950, OldStand.Lean),
        (12, "LBA1's protopack", 700, OldStand.Floor),
        (14, "LBA1's meca-penguin", 600, OldStand.Floor),
        (0, "LBA1's holomap", 620, OldStand.Floor),
        (3, "Gawley's horn", 460, OldStand.Floor),
        (16, "LBA1's pirate flag", 900, OldStand.Hung),
    };

    private static List<(int Generic, string Name, Body Body)> OldThingBodies(string lba1Directory, byte[] lba2Palette, byte[] header)
    {
        var list = new List<(int, string, Body)>();
        var path = Path.Combine(lba1Directory, "INVOBJ.HQR");
        if (!File.Exists(path)) return list;
        var hqr = HqrArchive.Open(path);
        var colours = RaceTrackCharacterCars.Lba1ToLba2Colours(HqrArchive.Open(Path.Combine(lba1Directory, "RESS.HQR")).Read(0), lba2Palette);
        for (var k = 0; k < OldThings.Length; k++)
        {
            var (entry, name, size, stand) = OldThings[k];
            Body lba1;
            try { lba1 = Body.Read(hqr.Read(entry), 1); } catch { continue; }
            var world = lba1.World();
            if (world.Length == 0) continue;
            // to life size, then stood as it stands
            Vector3 min = new(world.Min(v => v.X), world.Min(v => v.Y), world.Min(v => v.Z)), max = new(world.Max(v => v.X), world.Max(v => v.Y), world.Max(v => v.Z));
            var scale = size / MathF.Max(max.X - min.X, MathF.Max(max.Y - min.Y, max.Z - min.Z));
            var centre = (min + max) / 2;
            var placed = world.Select(v =>
            {
                var p = (v - centre) * scale;
                // (the snowboard: its length up, its deck facing out, +z -- turned a quarter about x)
                if (stand == OldStand.StoodUp) p = new Vector3(p.X, -p.Z, p.Y);
                return p;
            }).ToArray();
            var lo = new Vector3(placed.Min(v => v.X), placed.Min(v => v.Y), placed.Min(v => v.Z));
            var top = placed.Max(v => v.Y) - lo.Y;
            for (var i = 0; i < placed.Length; i++)
            {
                var p = placed[i] - new Vector3(0, lo.Y, stand is OldStand.Lean or OldStand.StoodUp or OldStand.Hung ? lo.Z : 0);
                // (leant: its top back against the wall, its foot out from it)
                if (stand is OldStand.Lean or OldStand.StoodUp) p.Z += (top - p.Y) * 0.18f;
                if (stand == OldStand.Hung) p.Z += 6;
                placed[i] = p;
            }
            var m = new CarMesh(name, new[] { -1 });
            var handle = placed.Select(v => m.P(0, v)).ToArray();
            foreach (var f in lba1.Faces)
            {
                var material = f.Material >= 7 ? -1 : f.Material is 5 or 6 ? CarMesh.SeeThrough : 0;
                var hs = f.Points.Select(i => handle[i]).ToArray();
                var c = colours[f.Colour & 255];
                void Add(int[] q) { m.Raw(q, c, material); if (stand == OldStand.Hung) m.Raw(q.Reverse().ToArray(), c, material); }
                if (hs.Length is 3 or 4) Add(hs);
                else for (var j = 1; j + 1 < hs.Length; j++) Add(new[] { hs[0], hs[j], hs[j + 1] });
            }
            foreach (var l in lba1.Lines) m.Line(handle[l.A], handle[l.B], colours[l.Colour & 255]);
            foreach (var s in lba1.Spheres) m.Sphere(handle[s.Point], (int)MathF.Max(1, s.Radius * scale), colours[s.Colour & 255]);
            list.Add((OldThingFirst + k, name, m.ToBody(header)));
        }
        return list;
    }
}
