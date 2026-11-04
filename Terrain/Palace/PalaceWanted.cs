using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Palace;

// The ninth portrait, an easter egg (the user, 2026-11-04: "Let's make all those changes", of the proposal's "Deep in the dungeon, behind
// the iron maiden, a portrait facing the wall. Action turns it round: Twinsen in his LBA1 tunic, captioned 'WANTED. Escaped from the
// asylum, Citadel Island.' Twinsen: 'That's not a good likeness.'"). The picture -- Twinsen in his tunic (BODY.HQR 1), painted as the
// palace's portraits are (PalacePortraits.Paint), on a backdrop of an old poster's brown -- on a page of the island's object textures of its
// own, hung on the dungeon's east wall in the corner behind the iron maiden (PalaceIsland: the north end, the way out's end, where the
// maiden stands a cell out from that wall), looking west into the dungeon. Smaller than the rooms' pictures (theirs are 8 layers high, the
// dungeon 5): PictureWide x PictureHigh, its foot FootOver over the floor, a little out from the wall's stone (OffWall: the wall's faces
// stay -- a picture's place in the rooms has its wall's faces left out instead, a whole cell's, bigger than this one).
// Facing the wall it is the story's (RaceTrackStory.Eggs.Palace: the back of a frame, an actor, hung over it until Action turns it round).
internal static class PalaceWanted
{
    // (world units) its size, its foot over the floor's top, how far out from the wall; (palace cells) its middle along the east wall from
    // the north wall -- the maiden at 0.32 out from the north wall, 1.1 from the east, the shackles from 1.9 along it
    public const double PictureWide = 1280, PictureHigh = 1600, FootOver = 700, OffWall = 24, Along = 0.95;
    public static readonly PalacePortraits.Sitter Twinsen = new("Twinsen, wanted", 1, 0.35, 0.66, (96, 80, 56));

    // Painted and hung: its page added to the island's object pages, its body appended to `newBodies` (numbered from `firstBody` on), a
    // decor nothing touches in the cube it is in. `wallX`: the east wall's face (world x; the dungeon to the west of it), `middleZ` the
    // picture's middle along it, `floorY` the floor's top. Returns a line for the log.
    public static string Hang(string gameDirectory, IslandFile island, Func<(float R, float G, float B), int> nearest, double wallX, double middleZ, double floorY,
        List<byte[]> newBodies, int firstBody, int maxPerCube)
    {
        if (island.ObjectPages.Count + 1 >= IslandFile.MaxObjectPages) return "WARNING: the ninth portrait: no object page left -- left out";
        Body sitter;
        byte[]? texture = null;
        try
        {
            sitter = Body.Read(HqrArchive.Open(Path.Combine(gameDirectory, "BODY.HQR")).Read(Twinsen.Body), 2, allowStatic: true);
            texture = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(6);
        }
        catch (Exception e) when (e is IOException or InvalidDataException or ArgumentException) { return $"WARNING: the ninth portrait: {e.Message} -- left out"; }
        var picture = PalacePortraits.Paint(sitter, texture, nearest, Twinsen);
        var page = new byte[256 * 256];
        for (var row = 0; row < PalacePortraits.High; row++) Array.Copy(picture, row * PalacePortraits.Wide, page, row * 256, PalacePortraits.Wide);
        island.ObjectPages.Add(page);
        var pageNumber = island.ObjectPages.Count;

        // (one textured polygon looking west (-x), as PalacePortraits.Hang makes a picture on a wall looking that way)
        int dx = -1, dz = 0;
        float x = (float)(wallX - OffWall), y0 = (float)(floorY + FootOver), y1 = (float)(floorY + FootOver + PictureHigh);
        float z0 = (float)(middleZ - PictureWide / 2), z1 = (float)(middleZ + PictureWide / 2);
        var quad = new[] { new Vector3(x, y0, z0), new Vector3(x, y0, z1), new Vector3(x, y1, z1), new Vector3(x, y1, z0) };
        var right = new Vector3(dz, 0, -dx);
        float a = quad.Min(q => Vector3.Dot(q, right)), b = quad.Max(q => Vector3.Dot(q, right)), lo = quad.Min(q => q.Y), hi = quad.Max(q => q.Y);
        int U(Vector3 q) => (int)Math.Round((0.5 + (Vector3.Dot(q, right) - a) / (b - a) * (PalacePortraits.Wide - 1)) * 256);
        int V(Vector3 q) => (int)Math.Round((0.5 + (hi - q.Y) / (hi - lo) * (PalacePortraits.High - 1)) * 256);
        var origin = (quad[0] + quad[2]) / 2;
        var points = quad.Select(q => q - origin).ToList();
        var ids = new[] { 0, 1, 2, 3 };
        var n = Vector3.Cross(quad[1] - quad[0], quad[2] - quad[0]);
        if (Vector3.Dot(n, new Vector3(dx, 0, dz)) < 0) Array.Reverse(ids);
        var uv = ids.SelectMany(i => new[] { U(quad[i]), V(quad[i]) }).ToArray();
        var header = new byte[96];
        BinaryPrimitives.WriteInt32LittleEndian(header, 0x10 | BrickBuilding.CloseFlag);
        var body = new Body
        {
            Game = 2, Static = true, Lit = false, Header = header, Vertices = points,
            Bones = new List<Bone> { new(0, points.Count, 0, -1, new byte[8]) },
            Faces = new List<Face> { new(ids, 0, Texture: new FaceTexture(0, uv), Lba2Type: 16) },
            Textures = new[] { PalacePortraits.Entry(0, 0) },
        };
        var number = firstBody + newBodies.Count;
        newBodies.Add(body.Write());
        var lo3 = quad.Aggregate(Vector3.Min); var hi3 = quad.Aggregate(Vector3.Max);
        var placed = BrickBuilding.AddToCubes(island, number | pageNumber << IslandFile.DecorPageShift, 0, origin.X, (int)Math.Round(origin.Y), origin.Z,
            lo3.X, hi3.X, (int)Math.Floor(lo3.Y), BrickBuilding.NoBoxTop, lo3.Z, hi3.Z, maxPerCube, 0).Placed;
        return placed > 0
            ? $"the ninth portrait (an easter egg): {Twinsen.Name}, BODY.HQR {Twinsen.Body}, object page {pageNumber}, on the dungeon's east wall at ({origin.X:0}, {origin.Y:0}, {origin.Z:0}), looking west"
            : "WARNING: the ninth portrait: its cube is full -- not hung";
    }
}
