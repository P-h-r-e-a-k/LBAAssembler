using System.IO;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The Desert race's entrants from all round Twinsun (the user, 2026-11-16: "Winning the Citadel 2 track allows Twinsen to enter the Desert
// island track with entrants from all around Twinsun"; "let's pull in some characters from LBA1's different islands"): one of each of LBA1's
// islands' people, each in a car made after them -- the user's choices, each with the car they asked for:
//   Principal Island     Bob Vortix the astronomer, a green sphero (LBA1 scene 29's actor 1, the Old Burg's house on the hill): "an
//                        astronomer themed car, perhaps with a giant telescope making up most of the car"
//   Rebellion Island     Lieutenant Grap, a rabbibunny rebel (scene 59's actor 4, down in the trench): "an SUV vehicle in camouflage colours"
//   Hamalayi Mountains   a rabbibunny in green shirt and brown shorts in tatters: "a giant carrot themed car. Perhaps some small carrots
//                        sticking out of the wheels, and a giant carrot leaf at the back"
//   Tippet Island        the hipster rabbibunny: "a disco music inspired car"
//   Brundle Island       the painter rabbibunny in white: "a white paint can inspired car, perhaps with a comedy paint brush at the back"
// and as they were: Baldino Proxima Island's (where LBA1 has him), the Desert track's own racer the White Leaf Desert's (the user).
// Each driver is its LBA1 body (LBA1's BODY.HQR -- the body list's line less two, Assets/FileDesc/BODY1.HQD) sat in by the cast's own steps
// (BuildCast: cut at the waist, made the cockpit's size, the hands on the wheel), its colours each moved to the LBA2 ramp nearest its own
// (Lba1ToLba2Colours, as the digger's); the cars the cast's styles of theirs (Cast.cs: Telescope, Suv, Carrot, Disco, PaintCan). Bodies of
// the racer's entity -- Generic on, their half-size copies beside them -- as the digger is, after every other car (InstallLba1Entrants), and
// race cars Number on in RACECARS.JSON.
internal static partial class RaceTrackCharacterCars
{
    // (Seat: the cast's seat put right by hand -- LBA1's round green sphero came out twice the car's size: the cast's least scale, 0.3, too big for him)
    public sealed record Lba1Entrant(int Number, int Generic, int Lba1Body, string Short, string Name, string Driver, string Island, Kind Kind, Style Style, CastSeat? Seat = null);
    public static readonly Lba1Entrant[] Lba1Entrants =
    {
        new(229, 73, 69, "Bob Vortix", "Bob Vortix's telescope", "Bob Vortix, the astronomer of Principal Island", "Principal Island", Kind.Sphero, Style.Telescope, new CastSeat(Scale: 0.17f)),
        new(230, 74, 66, "Grap", "Grap's SUV", "Lieutenant Grap of Rebellion Island", "Rebellion Island", Kind.Rabbibunny, Style.Suv),
        new(231, 75, 24, "The Hamalayi rabbibunny", "The giant carrot", "a rabbibunny of the Hamalayi Mountains", "Hamalayi Mountains", Kind.Rabbibunny, Style.Carrot),
        new(232, 76, 105, "The Tippet hipster", "The disco car", "the hipster of Tippet Island", "Tippet Island", Kind.Rabbibunny, Style.Disco),
        new(233, 77, 26, "The Brundle painter", "The paint can", "the painter of Brundle Island", "Brundle Island", Kind.Rabbibunny, Style.PaintCan),
    };
    public static Lba1Entrant EntrantOf(string shortName) => Lba1Entrants.First(e => e.Short == shortName);

    // An LBA1 body as a driver: its colours LBA2's (each moved to the nearest ramp's same step), its faces lit or flat as LBA1 had them
    // (LBA1's types 7-10 lit; the rest flat -- the seat keeps a face lit unless its material is 0)
    public static Body Lba1Character(byte[] lba1Body, int[] colours)
    {
        var body = Body.Read(lba1Body, 1);
        for (var i = 0; i < body.Faces.Count; i++)
        {
            var f = body.Faces[i];
            body.Faces[i] = f with { Colour = colours[f.Colour & 255], Material = f.Material >= 7 ? 1 : 0 };
        }
        for (var i = 0; i < body.Lines.Count; i++) body.Lines[i] = body.Lines[i] with { Colour = colours[body.Lines[i].Colour & 255] };
        for (var i = 0; i < body.Spheres.Count; i++) body.Spheres[i] = body.Spheres[i] with { Colour = colours[body.Spheres[i].Colour & 255] };
        return body;
    }

    // One entrant's car: its driver from LBA1's BODY.HQR and palette, the racer's car (its header)
    public static Body BuildLba1Entrant(Lba1Entrant e, byte[] lba1Body, byte[] lba1Palette, byte[] lba2Palette, Body racer) =>
        BuildCast(new CastMember(-1, e.Short, e.Driver, e.Island, e.Kind, e.Style, e.Seat), e.Number, Lba1Character(lba1Body, Lba1ToLba2Colours(lba1Palette, lba2Palette)), racer);

    // Adds the entrants' cars to a game folder whose race tracks are built (after the digger: RaceTrackService.Build): each car's body and its
    // half-size copy at the end of BODY.HQR, the racer's entity's bodies Generic and its half-size one, its line in RACECARS.JSON.
    // `lba1Directory`: LBA1's game folder (its BODY.HQR and RESS.HQR); none there, none made. Lines for the build's log.
    public static List<string> InstallLba1Entrants(string gameDirectory, string? lba1Directory)
    {
        if (string.IsNullOrEmpty(lba1Directory) || !File.Exists(Path.Combine(lba1Directory, "BODY.HQR")) || !File.Exists(Path.Combine(lba1Directory, "RESS.HQR")))
            return new() { "the Desert race's entrants from LBA1's islands: not made -- no LBA1 folder to take them from" };
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var racer = Body.Read(HqrArchive.Open(bodyPath).Read(RaceTrackBaldinoCar.RacerBody), 2);
        var lba1Bodies = HqrArchive.Open(Path.Combine(lba1Directory, "BODY.HQR"));
        var lba1Palette = HqrArchive.Open(Path.Combine(lba1Directory, "RESS.HQR")).Read(0);
        var lba2Palette = HqrArchive.Open(ressPath).Read(0);
        var file = File.ReadAllBytes(bodyPath);
        var next = HqrArchive.CountEntries(bodyPath);
        var table = HqrArchive.Open(ressPath).Read(44);
        var catalogue = Catalogue(gameDirectory).Where(c => Lba1Entrants.All(e => e.Number != c.Number)).ToList();
        var log = new List<string>();
        foreach (var e in Lba1Entrants)
        {
            Body car;
            try { car = BuildLba1Entrant(e, lba1Bodies.Read(e.Lba1Body), lba1Palette, lba2Palette, racer); }
            catch (Exception x) when (x is InvalidDataException or InvalidOperationException or ArgumentException or IOException) { log.Add($"{e.Name} (race car {e.Number}): not made -- {x.Message}"); continue; }
            var bytes = car.Write();
            file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(bytes));
            var own = next++;
            file = HqrWriter.AppendEntry(file, HqrWriter.StoredEntry(RaceTrackSmallCars.Halved(bytes)));
            var small = next++;
            table = RaceTrackBaldinoCar.WithBody(table, RaceTrackBaldinoCar.RacerEntity, e.Generic, own);
            table = RaceTrackBaldinoCar.WithBody(table, RaceTrackBaldinoCar.RacerEntity, RaceTrackSmallCars.SmallOf(e.Generic), small);
            catalogue.Add(new CatalogueEntry(e.Number, e.Name, e.Driver, e.Island, RaceTrackBaldinoCar.RacerEntity, e.Generic, own, small));
            log.Add($"{e.Name} (race car {e.Number}, {e.Island}: LBA1's body {e.Lba1Body}): BODY.HQR {own} ({car.Faces.Count} polygons, {car.Vertices.Count} points), half-size {small}; the racer's entity's bodies {e.Generic} and {RaceTrackSmallCars.SmallOf(e.Generic)}");
        }
        File.WriteAllBytes(bodyPath, file);
        HqrWriter.ReplaceInFile(ressPath, 44, table);
        SaveCatalogue(gameDirectory, catalogue.OrderBy(c => c.Number).ToList());
        return log;
    }
}
