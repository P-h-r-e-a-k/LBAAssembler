using System.IO;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// A simple flat rectangular deck (a box: top, bottom, four sides), for the race track's road-over-road bridge -- the same
// technique the retail game itself uses for a walkable span (Citadel Island's rope bridge at "the Cliffs of the Woodbridge":
// a flat plank decor with a ZV box whose top is the walking surface; ReajustPosDecors, EXTFUNC.CPP, lifts anything over the
// decor's footprint to that top). One body, reused for every tile of every deck: only its ZV box differs per placement.
internal static class RaceTrackDeckBody
{
    // Palette indices of the Desert island's own palette (RESS.HQR entry 29) nearest the ground's own asphalt grey, a darker
    // concrete grey for the underside and ends, and the red curb colour for a rail down each long edge -- so the deck reads as
    // the same road continuing, not a foreign object.
    public const int TopColour = 53, SideColour = 212, RailColour = 75;

    // width: across the road (the deck's local X). length: along the road (the deck's local Z). thickness: how deep the slab
    // reaches below its own top surface (local Y 0 = the walking surface, -thickness = the underside).
    public static byte[] Build(double width, double length, double thickness)
    {
        var hw = (float)(width / 2); var hl = (float)(length / 2); var t = (float)thickness;
        var v = new System.Numerics.Vector3[]
        {
            new(-hw, 0, -hl), new(hw, 0, -hl), new(hw, 0, hl), new(-hw, 0, hl),           // 0-3: top
            new(-hw, -t, -hl), new(hw, -t, -hl), new(hw, -t, hl), new(-hw, -t, hl),        // 4-7: bottom
        };
        var body = new Body
        {
            Game = 2, Static = true, Lit = false, Header = new byte[96],
            Vertices = new List<System.Numerics.Vector3>(v),
            Bones = new List<Bone> { new(0, v.Length, 0, -1, new byte[8]) },
            Faces = new List<Face>
            {
                new(new[] { 0, 1, 2, 3 }, TopColour),
                new(new[] { 7, 6, 5, 4 }, SideColour),
                new(new[] { 4, 5, 1, 0 }, SideColour),
                new(new[] { 3, 2, 6, 7 }, SideColour),
                new(new[] { 0, 3, 7, 4 }, RailColour),
                new(new[] { 2, 1, 5, 6 }, RailColour),
            },
        };
        return body.Write();
    }

    // Appends a deck body (sized for the given road width, a modest length per tile) to an island OBL file on disk and
    // returns its new index. Always appended fresh: RaceTrackService rebuilds the OBL from its own pristine backup every
    // time, so this never piles up unused bodies from an earlier build.
    public static int AppendTo(string oblPath, double width, double tileLength, double thickness = 150)
    {
        var hqr = File.ReadAllBytes(oblPath);
        var index = HqrArchive.CountEntries(oblPath);   // NOT HqrArchive.Open(...).Count -- that's the table's raw byte length, ~4x too high
        var entry = HqrWriter.StoredEntry(Build(width, tileLength, thickness));
        File.WriteAllBytes(oblPath, HqrWriter.AppendEntry(hqr, entry));
        return index;
    }
}
