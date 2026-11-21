using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// LBA1's hydroglider, the hovercraft of its credits on the beach by the parasols (the user, 2026-11-20: "In LBA1 there's a vehicle called a
// hydroglider body '37: Hovercraft' ... Let's add a hovercraft hire area at Twinsen's current location in-game"; "Twinsen hiring the
// hovercraft should allow Twinsen to drive the hovercraft around in the sea"): LBA1's BODY.HQR entry 104 ("Hovercraft" in BODY1.HQD; 103 is
// it with LBA1's Twinsen in it) taken whole, as the digger is
// (BuildDigger: its colours to the LBA2 palette's nearest ramps, on the racer's hull bone), Twinsen sat in it -- and with nobody in it, parked
// on the beach and while he's out of it. The racer's entity's bodies HovercraftGeneric (parked) and HovercraftDriveGeneric (him in it); the
// race file's hovercraft= draws his car as it while it's hired (RACEMOD.CPP, RaceTrackStory.Hovercraft).
internal static partial class RaceTrackCharacterCars
{
    public const int Lba1HovercraftBody = 104, HovercraftGeneric = 88, HovercraftDriveGeneric = 89;
    public const string HovercraftName = "The hovercraft";

    public static Body BuildHovercraft(Body lba1, byte[] lba1Palette, byte[] lba2Palette, Body twinsen, Body racer, bool driver)
    {
        var colours = Lba1ToLba2Colours(lba1Palette, lba2Palette);
        var m = new CarMesh(HovercraftName);
        Roots(m);
        var world = lba1.World();
        var handle = world.Select(v => m.P(2, v)).ToArray();
        foreach (var f in lba1.Faces)
        {
            var material = f.Material >= 7 ? -1 : f.Material is 5 or 6 ? CarMesh.SeeThrough : 0;
            var hs = f.Points.Select(i => handle[i]).ToArray();
            if (hs.Length is 3 or 4) m.Raw(hs, colours[f.Colour & 255], material);
            else for (var k = 1; k + 1 < hs.Length; k++) m.Raw(new[] { hs[0], hs[k], hs[k + 1] }, colours[f.Colour & 255], material);
        }
        foreach (var l in lba1.Lines) m.SolidLine(handle[l.A], handle[l.B], CarDriver.SolidLineThickness, colours[l.Colour & 255]);
        foreach (var s in lba1.Spheres) m.Sphere(handle[s.Point], s.Radius, colours[s.Colour & 255]);
        // ---- the wheels' bones: a point each (nothing of it turns)
        var parents = new[] { 0, 0, 0, 2, 3, 4, 2, 6, 7, 2, 9, 2, 11 };
        for (var b = 3; b <= 12; b++)
        {
            var at = new Vector3(b is >= 3 and <= 8 ? 300 : -300, 100, b is >= 3 and <= 8 ? 400 : -400);
            m.Pivot(b, m.P(parents[b], at));
            m.P(b, at);
        }
        // ---- Twinsen in its seat (bone 13): over the hull's middle, a little behind it, as the pumpkin's and the digger's drivers sit
        var seat = HovercraftSeat(world);
        m.Pivot(13, m.P(2, seat));
        if (driver)
        {
            m.Light = 1;
            const float Scale = 0.7f;
            var origin = new Vector3(0, 600, 33);
            Vector3 Place(Vector3 p) => (p - origin) * Scale + seat;
            var d = new CarDriver(twinsen);
            var point = d.Seat(m, d.AllBut(new[] { 4, 6, 8, 11 }), Place, seat.Y + 120, Scale, cut: true, solid: true);
            Arms(m, point(twinsen.Bones[4].Pivot), point(twinsen.Bones[6].Pivot));
        }
        else
        {
            m.P(13, seat);
            for (var b = 14; b <= 17; b++) { m.Pivot(b, m.P(b == 16 ? 13 : b - 1, seat)); m.P(b, seat); }
        }
        return WithOwnBox(m.ToBody(racer.Header));
    }

    // (the seat: the hull's middle across, a fifth of its length behind its middle, its deck's height -- a third of the way up it; trials
    // through HovercraftSeatAt)
    public static Vector3? HovercraftSeatAt = null;
    public static string HovercraftShape = "";
    private static Vector3 HovercraftSeat(Vector3[] world)
    {
        if (HovercraftSeatAt is { } at) return at;
        float x0 = world.Min(v => v.X), x1 = world.Max(v => v.X), y0 = world.Min(v => v.Y), y1 = world.Max(v => v.Y), z0 = world.Min(v => v.Z), z1 = world.Max(v => v.Z);
        HovercraftShape = $"x {x0:0}..{x1:0}, y {y0:0}..{y1:0}, z {z0:0}..{z1:0}";
        return new Vector3((x0 + x1) / 2, y0 + (y1 - y0) * 0.35f, (z0 + z1) / 2 - (z1 - z0) * 0.2f);
    }

    // The hovercraft's bodies into a game folder whose race tracks are built (after the digger): him in it and nobody in it at the end of
    // BODY.HQR, the racer's entity's HovercraftDriveGeneric and HovercraftGeneric. Lines for the build's log.
    public static List<string> InstallHovercraft(string gameDirectory, string? lba1Directory)
    {
        if (string.IsNullOrEmpty(lba1Directory) || !File.Exists(Path.Combine(lba1Directory, "BODY.HQR")) || !File.Exists(Path.Combine(lba1Directory, "RESS.HQR")))
            return new() { $"{HovercraftName}: not made -- no LBA1 folder to take it from" };
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        if (RaceTrackGuards.BodyOf(table, RaceTrackBaldinoCar.RacerEntity, HovercraftGeneric) is not null)
            return new() { $"{HovercraftName}: the racer's entity has a body {HovercraftGeneric} already -- left as it is" };
        var bodies = HqrArchive.Open(bodyPath);
        byte[] driving, parked;
        try
        {
            var lba1 = Body.Read(HqrArchive.Open(Path.Combine(lba1Directory, "BODY.HQR")).Read(Lba1HovercraftBody), 1);
            var pal1 = HqrArchive.Open(Path.Combine(lba1Directory, "RESS.HQR")).Read(0);
            var pal2 = HqrArchive.Open(ressPath).Read(0);
            var twinsen = Body.Read(bodies.Read(TwinsenBody), 2);
            var racer = Body.Read(bodies.Read(RaceTrackBaldinoCar.RacerBody), 2);
            driving = BuildHovercraft(lba1, pal1, pal2, twinsen, racer, driver: true).Write();
            parked = BuildHovercraft(lba1, pal1, pal2, twinsen, racer, driver: false).Write();
        }
        catch (Exception e) when (e is InvalidDataException or InvalidOperationException or ArgumentException or IOException) { return new() { $"{HovercraftName}: not made -- {e.Message}" }; }
        var file = File.ReadAllBytes(bodyPath);
        var own = HqrArchive.CountEntries(bodyPath);
        file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(driving));
        file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(parked));
        File.WriteAllBytes(bodyPath, file);
        table = RaceTrackBaldinoCar.WithBody(table, RaceTrackBaldinoCar.RacerEntity, HovercraftDriveGeneric, own);
        table = RaceTrackBaldinoCar.WithBody(table, RaceTrackBaldinoCar.RacerEntity, HovercraftGeneric, own + 1);
        HqrWriter.ReplaceInFile(ressPath, 44, table);
        return new() { $"{HovercraftName} (LBA1's hydroglider, body {Lba1HovercraftBody}: {HovercraftShape}): BODY.HQR {own} (Twinsen in it), {own + 1} (nobody in it); the racer's entity's bodies {HovercraftDriveGeneric}, {HovercraftGeneric}" };
    }
}
