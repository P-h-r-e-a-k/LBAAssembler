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
    // reaches below its own top surface (local Y 0 = the walking surface, -thickness = the underside). railPlusX: the +X side
    // (the outer edge of the deck, for the tiles of the edge columns) is the red curb colour instead of the concrete grey.
    public static byte[] Build(double width, double length, double thickness, bool railPlusX = false)
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
                new(new[] { 0, 3, 7, 4 }, SideColour),
                new(new[] { 2, 1, 5, 6 }, railPlusX ? RailColour : SideColour),
            },
        };
        return body.Write();
    }

    // Appends the two square deck tiles (`tile` units a side) to an island OBL file on disk and returns the index of the first:
    // index = the plain tile, index + 1 = the edge tile (red rail on its +X side; turned 180 degrees for the other edge). The deck
    // is a grid of these small squares rather than a few long slabs because the engine's collision box of a decor is
    // axis-aligned: a long slab laid diagonally gets a box far bigger than itself (an invisible floor reaching cells past the
    // visible deck); a small square's box overhangs by well under a cell. Always appended fresh: RaceTrackService rebuilds the
    // OBL from its own pristine backup every time, so this never piles up unused bodies from an earlier build.
    public static int AppendTo(string oblPath, double tile, double thickness = 150)
    {
        var hqr = File.ReadAllBytes(oblPath);
        var index = HqrArchive.CountEntries(oblPath);   // NOT HqrArchive.Open(...).Count -- that's the table's raw byte length, ~4x too high
        hqr = HqrWriter.AppendEntry(hqr, HqrWriter.StoredEntry(Build(tile, tile, thickness)));
        hqr = HqrWriter.AppendEntry(hqr, HqrWriter.StoredEntry(Build(tile, tile, thickness, railPlusX: true)));
        File.WriteAllBytes(oblPath, hqr);
        return index;
    }
}
