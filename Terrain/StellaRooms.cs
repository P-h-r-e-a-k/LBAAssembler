using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Stella Entities' office (scene 391) and backstage (scene 392): built in full behind their doors, though the bouncers keep Twinsen out for
// now (the user, 2026-11-11 late: "even if we can't access them initially I'm sure I'll add something later to allow us to") -- so the
// club's scene has no way in yet; each room's own door leads back out to the hall, at the door it belongs to (CitadelProps.OfficeAt,
// BackstageAt), so a way in can be added later by a zone there.
//   - the office: Rick's office (scene 137), its grid, its light and music and its furniture as the game has them (the desk and the lamps),
//     without Rick, his guards and their scripts;
//   - backstage: the same room (the grid is shared), furnished for the performers -- a rack, chairs, mirrors -- instead.
// Since 2026-11-14 the rooms' grid is their own, a copy of Rick's (the user, 2026-11-13: "The office we've added is a direct copy of the one
// from the bar on Otringal, we need to change it. The hole in the wall needs to not have a transition zone, and let's add a picture here,
// let's also replace the picture on the back wall, and change the colours of the wall to pull our space theme through. Let's have the new
// pictures be of some of our strippers"): every block of his teal stucco walls painted the night, dark blue flecked with stars; the air
// vent in his west wall (his escape, its door zone gone) and his painting on the north wall walled over, a poster of the club's
// performers on each (StellaPosters) in the office.
internal static class StellaRooms
{
    // (since 2026-11-17 the catwalk's door leads into the dressing room, 391, and the hall's office door into the office, 392 -- the user,
    // 2026-11-16: "The doorway at the end of the catwalk needs to go into an area where the performers get changed, it currently connects to
    // the office" and "let's keep 392 and lose the other one": 392, the office, to be dressed anew once its door is settled)
    public const int Office = 392, Backstage = 391, RickOffice = 137;
    // the private room (scene 395: 393 and 394 are the bowling alley's and the Francos' tunnels'): the same room again, curtained, a low
    // sofa where Twinsen sits for a private show (RaceTrackStory.StellaEntities: the hostess's offer in the hall, the show there) -- the
    // user, 2026-11-13, "Let's have the curtained booths in a back area that a dancer can lead him off into"
    public const int Private = 395;
    // (since 2026-11-15 the back rooms' own grid, StellaEntities.BuildWing: Twinsen in at the corridor's south end, EntryZ; the chair in the
    // room at its far end, SofaX/SofaZ, the dancer before it at ShowZ -- he faces her, south)
    public const int SofaX = 16384, SofaZ = 7680, ShowZ = 9216, EntryZ = 23040;
    // (the doorway from the hall since 2026-11-19, CitadelProps.WingDoorway: on the corridor's west wall's face, between its cells z 45 and
    // 46 -- south of the first booth; Twinsen in from the hall just inside it, facing east)
    public const int WingDoorX = StellaEntities.WingCorrX0 * 512, WingDoorZ = 46 * 512, InFromHallX = WingDoorX + 700;
    // (where Twinsen comes in from the club, either room: just inside the door of Rick's office's that leads out, x 5120-6144 at z 9216)
    public const int InsideX = 5632, InsideY = 256, InsideZ = 8500;
    // (the booths since 2026-11-17, StellaEntities.WingRooms: the chair half a cell off the back wall, the curtain across the way in, the
    // middle of its cell x 29 -- the booth's mouth; she dances three cells before the chair, as far as she did in the old end room, he sits
    // facing her, east)
    public const int BoothChairX = StellaEntities.WingWestX0 * 512 + 256, BoothDrapeX = (StellaEntities.WingWestX1 + 1) * 512 + 256, BoothShowX = BoothChairX + 1536;
    public static int BoothMid(int z0, int z1) => (z0 + z1 + 1) * 256;
    // (a scene actor's body: the props past 127 kept as the engine reads them back, a signed byte -- CitadelProps.Actor)
    public static int BodyOf(int generic) => generic > 127 ? (sbyte)generic : generic;
    // the booth's curtain among the private room's actors: the one across booth k's mouth
    public static int BoothDrapeIndex(SceneModel model, int k)
    {
        var (z0, z1) = StellaEntities.WingRooms[k];
        return model.Actors.FindIndex(a => a.Body == BodyOf(Citadel.CitadelProps.BoothDrape) && a.Z == BoothMid(z0, z1));
    }
    private const int DoorwayZ = 9216;   // (Rick's doorway: his door's sprite stands past it, in the door zone)
    private const int Prop = Citadel.CitadelProps.Chair;
    // Rick's walls: the west wall's block (its pattern: z mod 5 -- z 12 its positions 35-39 up from layer 6, z 13 its 5-9) and the vent in it
    // (x 4, z 12-13, layers 6-9); the north wall's (x 5-14, z 4: its column x mod 4 -- 20-23, 4 more a layer up) and the painting in it
    // (block 195, x 11-13, layers 6-9)
    // (Rick's furniture, bricks: his armchairs, his desk and its ends, what stands on it -- gone from the private room's grid: the dancer
    // stood in an armchair)
    private static readonly HashSet<int> Furniture = new() { 78, 82, 143, 144, 145, 62, 205 };
    // (and the dressing room's: Rick's sofa, x 5-6 z 11-14; the potted plant, x 14-15 z 5-6; the wall lamp, x 16 z 10, the east wall's
    // block 196 there instead)
    private const int DressGone1 = 84, DressGone2 = 40, WallLamp = 207, EastWallBlock = 196;
    // (the office's: the plant's cells, x 14-15 z 5-6, moved this far south -- the corner right of the doorway; the curtains' colour ramp,
    // orange, made the purple's)
    private const int PlantX = 14, PlantZ = 5, PlantMoveZ = 11, OrangeRamp = 5, CurtainRamp = 13;
    // (the floor's tile, and its darker ones -- the shadows his desk and armchairs left on it, cells x 9-15 z 7-14)
    private const int FloorTile = 24, FloorShadow = 26, FloorShadow2 = 136, DeskShadowX0 = 9, DeskShadowX1 = 15, DeskShadowZ0 = 7, DeskShadowZ1 = 14;
    private const int WestWall = 23, NorthWall = 28, Painting = 195, WallX = 4, VentZ = 12, NorthZ = 4, PaintingX = 11;
    // the posters' places: on the west wall over the vent, on the north wall over the painting (the walls' inner faces, a little off them)
    // (the west wall's poster north of the dressing table's mirrors, which since 2026-11-17 stand along the wall's south end)
    private const int SidePosterX = (WallX + 1) * 512 + 24, SidePosterZ = 7 * 512 + 256, BackPosterX = PaintingX * 512 + 768, BackPosterZ = (NorthZ + 1) * 512 + 24, PosterY = 1530;

    // the dressing room's furniture (generic, x, z, beta): see Install
    // (Rick's office's cells: its floor x 5-15, z 5-17; along its west wall his corner piece, lamps and sofa -- the sofa, x 5-6 z 11-14,
    // the room's couch -- and on its north wall a curtain at x 8: the dressing table free-standing in front of the west wall's things, its
    // mirrors facing the room; the costume rail in the middle, facing the camera; the lockers on the north wall right of the curtain; the
    // folding screen by the east wall)
    internal const int DressTableX = 5 * 512 + 290, DressTableZ = 17 * 512 - 1720;
    internal static readonly (int Generic, int X, int Z, int Beta)[] DressingRoom =
    {
        // (the table flush to the west wall's face, x 5 cells, at its south end by the door -- where Rick's sofa was; four mirrors, a stool
        // before each -- the user, 2026-11-17: "have the table with the mirrors moved directly to the left of the door - same orientation as it
        // is currently, and pushed back flush to the wall")
        (Citadel.CitadelProps.DressingTable, DressTableX, DressTableZ, 1024),
        (Citadel.CitadelProps.ClubBarStool, DressTableX + 850, DressTableZ - 1275, 0), (Citadel.CitadelProps.ClubBarStool, DressTableX + 850, DressTableZ - 425, 0),
        (Citadel.CitadelProps.ClubBarStool, DressTableX + 850, DressTableZ + 425, 0), (Citadel.CitadelProps.ClubBarStool, DressTableX + 850, DressTableZ + 1275, 0),
        (Citadel.CitadelProps.CostumeRail, 5888, 4608, 0),
        (Citadel.CitadelProps.Lockers, 7150, 2800, 0),
        (Citadel.CitadelProps.FoldingScreen, 7700, 5000, 3072),
    };

    // the office's furniture (generic, x, z, beta): the desk its back to the west wall's face (x 5 cells), from z 8 to 11 -- between the
    // basin's alcove and Rick's sofa -- its chair before it facing it; the bed its head to the north wall's face (z 5 cells) under the
    // poster; the paddles' stand beside the plant's new corner, facing into the room. The hide's middle (RugX, RugZ): before the desk, short
    // of the doorway's way in
    internal const int OfficeDeskX = 5 * 512 + (int)Citadel.CitadelProps.DeskDepth + 30, OfficeDeskZ = 9 * 512 + 256;
    internal const int RugX = 10 * 512 + 256, RugZ = 12 * 512 + 150;
    internal static readonly (int Generic, int X, int Z, int Beta)[] OfficeFurniture =
    {
        (Citadel.CitadelProps.OfficeDesk, OfficeDeskX, OfficeDeskZ, 1024),
        (Citadel.CitadelProps.Chair, OfficeDeskX + (int)Citadel.CitadelProps.DeskDepth + 330, OfficeDeskZ, 3072),
        (Citadel.CitadelProps.MoonBed, 12 * 512 + 256, 5 * 512 + (int)Citadel.CitadelProps.BedLength + 40, 0),
        (Citadel.CitadelProps.Rack, 13 * 512 + 256, 17 * 512 + 200, 2048),
    };

    // (an actor onto a feature's own entity, when it has one -- CitadelProps.InstallOwn)
    private static SceneActorModel Own(SceneActorModel a, int entity) { if (entity >= 0) a.Entity = entity; return a; }

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(RickOffice) || !store.SceneExists(StellaEntities.Scene)) { log.Add("Stella Entities' office and backstage left out: no Rick's office (scene 137) or no club scene"); return log; }
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        foreach (var mine in new[] { Office, Backstage })
            if (mine + 1 < scenes.Count && !scenes.IsEmpty(mine + 1)) { log.Add($"Stella Entities' rooms left out: scene {mine} is already another part of the build's"); return log; }
        if (Private + 1 < scenes.Count && !scenes.IsEmpty(Private + 1)) { log.Add($"Stella Entities' rooms left out: scene {Private} is already another part of the build's"); return log; }
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        var type = table[RickOffice * 2];
        var (grid, _, privateGrid) = NightGrid(gameDirectory, bkg, header, table[RickOffice * 2 + 1], log);
        if (grid < 0 || privateGrid < 0) return log;
        table = bkg.Read(header.Brk + header.MaxBrk);
        if (table.Length < (Private + 1) * 2) Array.Resize(ref table, (Private + 1) * 2);
        // (the dressing room on the grid without Rick's furniture, his desk and armchairs -- bricks -- as the private room's was)
        foreach (var s in new[] { Office, Backstage, Private }) { table[s * 2] = type; table[s * 2 + 1] = (byte)(s == Office ? grid : privateGrid); }
        // (the private room: the back rooms' grid when the club's build made it -- Rick's bar's library, so its type)
        if (StellaEntities.WingGrid >= 0) { table[Private * 2] = table[StellaEntities.RickBar * 2]; table[Private * 2 + 1] = (byte)StellaEntities.WingGrid; }
        bkg.SetStored(header.Brk + header.MaxBrk, table);

        var original = store.Load(RickOffice);
        var (outX, outZ) = StellaEntities.At(Citadel.CitadelProps.OfficeAt, 14.5f);
        // (back out of backstage: onto the catwalk by its door, at the stage's height -- not on the floor at 14.5 cells, inside the
        // catwalk's cells; the engine adds where in the room's door zone he was, 0-1024 along x and 0-512 along z)
        // (past the backstage door's zone, which reaches BackstageZoneDeep back along the catwalk: at 600 a way out from the near half of the
        // room's door landed in it, and Twinsen went straight back in -- 392, 390, 392 -- found 2026-11-16 after the zone was made deeper)
        var (backX, backZ) = (StellaEntities.CatwalkX0World, StellaEntities.CatwalkEndZ + StellaEntities.BackstageZoneDeep + 160);
        static string Still() => "void comportement_0()\n{\n}\n";
        var records = new List<(int Scene, byte[] Record)>();
        var (privX, privZ) = StellaEntities.At(RaceTrackStory.PrivateBack, 15.5f);
        foreach (var (number, name, outAt) in new[] { (Office, "office", (outX, outZ)), (Backstage, "backstage", (backX, backZ)), (Private, "private room", (privX, privZ)) })
        {
            var model = original.Clone();
            model.Island = StellaEntities.MoonIsland;
            model.TrackPoints.Clear();
            // the furniture: the game's own entity 16's with a body (the desk and what stands round it) and its lamps (sprites) -- the office's;
            // backstage has none of them
            // (actor 1 kept as it is in every room: the engine's mechanical penguin's place -- whatever stands there is killed as each scene
            // loads, OBJECT.CPP RestartMecaPingouin; the private room's sofa and backstage's rack were, there)
            var keep = new List<SceneActorModel> { model.Actors[0], model.Actors[1] };
            // (backstage too, since 2026-11-14: the user, there, "we need the desk and chair back, along with the posters on the wall")
            // (not his door, a sprite in the doorway at z 9472: its own scripts open it, these rooms' are still -- shut for ever, Twinsen
            // kept in: the doorway open instead, 2026-11-14)
            // (the dressing room none of them: his lamps stood on his desk, and on the floor without it)
            // (not, since 2026-11-17, what stood on his desk -- the desk gone from the grid, a green triangle of it was left on the floor)
            if (number == Office) keep.AddRange(model.Actors.Skip(2).Where(a => ((a.Entity == 16 && a.Body >= 0) || a.IsSprite) && !(a.IsSprite && a.Z >= DoorwayZ)
                && !(!a.IsSprite && a.X / 512 >= DeskShadowX0 && a.X / 512 <= DeskShadowX1 && a.Z / 512 >= DeskShadowZ0 && a.Z / 512 <= DeskShadowZ1)));
            model.Actors.Clear(); model.Actors.AddRange(keep);
            // (his door, not the vent's way out: a door zone up the west wall, from layer 3)
            model.Zones.RemoveAll(z => z.Type != 0 || z.Y0 >= 512);
            foreach (var z in model.Zones.Where(z => z.Type == 0))
            {
                z.Num = StellaEntities.Scene;
                z.Info = new[] { outAt.Item1, number == Backstage ? StellaEntities.FloorY + (int)Citadel.CitadelProps.StageHigh : StellaEntities.FloorY, outAt.Item2, 0, 0, 0, 0, 1 };   // (the last: the door on)
            }
            if (number is Office or Backstage)
            {
                // (the office's over the vent walled up, as it was before the dressing room's moved north of its mirrors: there it hid
                // behind the basin's curtain, the vent's grille bare -- 2026-11-17)
                var posters = StellaPosters.Entity(gameDirectory);
                SceneOps.AddActor(model, Own(Citadel.CitadelProps.Actor(StellaPosters.PosterSide, SidePosterX, PosterY, number == Office ? (VentZ + 1) * 512 : SidePosterZ, 1024), posters));
                // (the office's higher: the bed's headboard stands under it now, and at the same height the poster was drawn over its top)
                SceneOps.AddActor(model, Own(Citadel.CitadelProps.Actor(StellaPosters.PosterBack, BackPosterX, number == Office ? PosterY + 350 : PosterY, BackPosterZ, 0), posters));
            }
            if (number == Private && StellaEntities.WingGrid >= 0)
            {
                // the back rooms: the doorway back to the hall on the corridor's west wall at its south end (since 2026-11-19, WingDoorway --
                // before, an unseen zone across the corridor's end: "there's currently no doorway into the scene"); Twinsen in just inside it,
                // facing into the corridor
                model.Zones.Clear();
                model.Zones.Add(new SceneZoneModel { Type = 0, Num = StellaEntities.Scene, X0 = WingDoorX, X1 = WingDoorX + 300, Z0 = WingDoorZ - 420, Z1 = WingDoorZ + 420, Y0 = 0, Y1 = 2000,
                    Info = new[] { privX - 512, StellaEntities.FloorY, privZ - 256, 0, 0, 0, 1, 1 } });
                (model.Actors[0].X, model.Actors[0].Y, model.Actors[0].Z, model.Actors[0].Beta) = (InFromHallX, 256, WingDoorZ, 1024);
                SceneOps.AddActor(model, Citadel.CitadelProps.Actor(Citadel.CitadelProps.WingDoorway, WingDoorX, 256, WingDoorZ, 0));
                // the booths, all on the corridor's west side (the user, 2026-11-17: "Some smaller booths, perhaps just one side, with curtains
                // drawn that we can't see into" + "a small waste paper bin that's overflowing with tissue to each booth"): in each a chair against
                // its back wall facing out (east), a tissue bin in its corner, and its curtain drawn across its way in -- the one Twinsen is
                // led into drawn back by the private show's script (RaceTrackStory.StellaLounge, BoothDrapeIndex)
                foreach (var (z0, z1) in StellaEntities.WingRooms)
                {
                    var mid = BoothMid(z0, z1);
                    var chair = Citadel.CitadelProps.Actor(Prop, BoothChairX, 256, mid, 1024);
                    chair.Armor = RaceTrackStory.WalkOverArmour;
                    SceneOps.AddActor(model, chair);
                    SceneOps.AddActor(model, Citadel.CitadelProps.Actor(Citadel.CitadelProps.TissueBin, StellaEntities.WingWestX0 * 512 + 230, 256, z0 * 512 + 230, 0));
                    var drape = Citadel.CitadelProps.Actor(Citadel.CitadelProps.BoothDrape, BoothDrapeX, 256, mid, 1024);
                    drape.Armor = RaceTrackStory.WalkOverArmour;
                    SceneOps.AddActor(model, drape);
                }
            }
            else if (number == Private)
            {
                // (no back rooms built: the old curtained room, its sofa)
                var sofa = Citadel.CitadelProps.Actor(Citadel.CitadelProps.ClubBooth, 6256, 256, 3100, 0);
                sofa.Armor = RaceTrackStory.WalkOverArmour;
                SceneOps.AddActor(model, sofa);
            }
            if (number == Office)
            {
                // the office's new look (the user, item 10 of 2026-11-17's list -- "Left"/"right" as he sees it coming in at the south wall's
                // doorway, walking north): the desk against the left (west) wall facing into the room, its chair before it; the big bed, its
                // head against the north wall under the poster; the beast's hide on the floor between them and the door; the stand of paddles
                // beside the plant in the corner right of the door (the plant moved there in the grid, NightGrid)
                foreach (var (generic, x, z, beta) in OfficeFurniture)
                    SceneOps.AddActor(model, Citadel.CitadelProps.Actor(generic, x, 256, z, beta));
                foreach (var (t, k) in Citadel.CitadelProps.BeastTiles.Take(Citadel.CitadelProps.BeastMost).Select((t, k) => (t, k)))
                {
                    var tile = Citadel.CitadelProps.Actor(Citadel.CitadelProps.BeastFirst + k, RugX + (int)MathF.Round((t.C0 - Citadel.CitadelProps.BeastRows[0].Length / 2f) * Citadel.CitadelProps.BeastStitch), 256,
                        RugZ + (int)MathF.Round((t.R0 - Citadel.CitadelProps.BeastRows.Length / 2f) * Citadel.CitadelProps.BeastStitch), 0);
                    tile.Armor = RaceTrackStory.WalkOverArmour;
                    SceneOps.AddActor(model, tile);
                }
                // (and come in at the doorway, not where Rick's scene starts its hero -- in the bed now)
                (model.Actors[0].X, model.Actors[0].Y, model.Actors[0].Z, model.Actors[0].Beta) = (InsideX, InsideY, InsideZ, 2048);
            }
            if (number == Backstage)
            {
                // the dressing room (CitadelProps.Dressing): the dressing table along the west wall under the poster, its mirrors facing into the
                // room, three stools before them; the costume rail along the north wall left of the sink's alcove, the lockers right of it; the
                // folding screen by the east wall. Where the camera looks: the north and west walls
                foreach (var (generic, x, z, beta) in DressingRoom)
                    SceneOps.AddActor(model, Citadel.CitadelProps.Actor(generic, x, 256, z, beta));
            }
            var scripts = LbaScript.SceneScripts.Load(SceneSerializer.Write(model), number);
            for (var i = 0; i < model.Actors.Count; i++)
            {
                scripts.SetText(i, LbaScript.ScriptKind.Life, Still());
                scripts.SetText(i, LbaScript.ScriptKind.Track, "label(0);\nstop();\n");
            }
            var built = scripts.Build();
            if (!built.Ok) { log.AddRange(built.Errors.Select(e => $"Stella Entities' {name} left out: {e}")); return log; }
            records.Add((number, built.Record!));
        }
        foreach (var (number, record) in records)
        {
            var entry = number + 1;
            while (scenes.Count <= entry) scenes.Slots.Add(new HqrFile.Slot());
            scenes.SetStored(entry, record);
            if (record.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
            { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, record.Length); scenes.SetStored(0, size); }
        }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        log.Add($"Stella Entities' rooms: the office (scene {Office}, grid {grid}), the dressing room behind the catwalk (scene {Backstage}, grid {privateGrid}: no desk or armchairs) and the private room (scene {Private}) -- Rick's office's walls the night -- each with its door back to the hall");
        return log;
    }

    // Blocks of a library painted the night (copies of them, added to it): those mostly blue -- a third of their picture or more in blue ramps
    // (a ramp's middle shade bluer than red) -- every ramp of a fiftieth or more of them dark blue, a star here and there. The new block of each.
    internal static Dictionary<int, int> NightBlocks(string gameDirectory, HqrFile bkg, MapBricks.BkgHeader header, ref byte[] library, IEnumerable<int> blocks)
    {
        byte[]? Brick(int b) => b >= 0 && b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? bkg.Read(header.Brk + b) : null;
        var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
        bool Blue(int r) { int k = (r * 16 + 8) * 3; return palette[k + 2] > palette[k] + 30; }
        var night = Enumerable.Range(0, 16).Select(s => Citadel.CitadelMuseum.Nearest(palette, 10 + s, 12 + s, 32 + s * 2)).ToArray();
        byte white = Citadel.CitadelMuseum.Nearest(palette, 235, 235, 250), yellow = Citadel.CitadelMuseum.Nearest(palette, 250, 236, 150);
        var made = new Dictionary<int, int>();
        foreach (var block in blocks.Distinct().OrderBy(b => b))
        {
            var counts = new int[16];
            Citadel.CitadelMuseum.Repaint(library, block, Brick, (x, y, c) => { counts[c >> 4]++; return c; });
            var all = counts.Sum();
            if (all == 0 || Enumerable.Range(0, 16).Where(Blue).Sum(r => counts[r]) * 3 < all) continue;
            var ramps = Enumerable.Range(0, 16).Where(r => counts[r] * 50 >= all).ToHashSet();   // (a fiftieth: the clouds' outlines too)
            var b0 = block;
            var painted = Citadel.CitadelMuseum.Repaint(library, block, Brick, (x, y, c) =>
            {
                if (!ramps.Contains(c >> 4)) return c;
                var h = unchecked(((uint)x * 73856093u) ^ ((uint)y * 19349663u) ^ ((uint)b0 * 83492791u)) * 2654435761u;
                return (h >> 7) % 97 == 0 ? white : (h >> 7) % 173 == 1 ? yellow : night[c & 15];
            });
            if (painted.Count == 0) continue;
            try { made[block] = Citadel.CitadelMuseum.AddBlock(bkg, header, ref library, block, painted); }
            catch (InvalidDataException) { }
        }
        return made;
    }

    // Rick's office's grid copied (`rick`), his stucco's blocks painted the night (copies of them, in his block library), the vent and the
    // painting walled over: the new grid's number (or -1, the log says why). The file's header kept up to date; the bricks' table read
    // again after (bricks added before it).
    private static (int Rooms, int Backstage, int Private) NightGrid(string gameDirectory, HqrFile bkg, MapBricks.BkgHeader header, int rick, List<string> log)
    {
        var raw = bkg.Read(header.Gri + rick);
        var libraryEntry = header.Bll + raw[0];
        var library = bkg.Read(libraryEntry);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        byte[]? Brick(int b) => b >= 0 && b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? bkg.Read(header.Brk + b) : null;
        var palette = HqrArchive.Open(Path.Combine(gameDirectory, "RESS.HQR")).Read(0);
        const int Size = 64, Height = 25;
        var used = new HashSet<int>();
        for (var z = 0; z < Size; z++) for (var y = 0; y < Height; y++) for (var x = 0; x < Size; x++)
        { var c = Lba1GridEdit.Get(shape, x, y, z); if (c.Block != 0) used.Add(c.Block); }
        // the stucco: the west wall's commonest colour ramp
        var counts = new int[16];
        Citadel.CitadelMuseum.Repaint(library, WestWall, Brick, (x, y, c) => { counts[c >> 4]++; return c; });
        var stucco = Array.IndexOf(counts, counts.Max());
        // (and its flecks: every ramp a tenth of it or more -- painted in the stucco's blocks only, those mostly of its own ramp: not his
        // stone's greys, his basin's, his lamps')
        var ramps = Enumerable.Range(0, 16).Where(r => r == stucco || counts[r] * 10 >= counts.Sum()).ToHashSet();
        // its shades the night's: dark blues; a star here and there, white or pale yellow
        var night = Enumerable.Range(0, 16).Select(s => Citadel.CitadelMuseum.Nearest(palette, 10 + s, 12 + s, 32 + s * 2)).ToArray();
        byte white = Citadel.CitadelMuseum.Nearest(palette, 235, 235, 250), yellow = Citadel.CitadelMuseum.Nearest(palette, 250, 236, 150);
        // (one shade of the night for the whole wall, the stucco's commonest shade's: its other shades -- the wallpaper's waves -- showed through
        // as darker waves, the user, 2026-11-14: "let's have them uniform")
        var shades = new int[16];
        Citadel.CitadelMuseum.Repaint(library, WestWall, Brick, (x, y, c) => { if (c >> 4 == stucco) shades[c & 15]++; return c; });
        var flat = night[Array.IndexOf(shades, shades.Max())];
        var swap = new Dictionary<int, int>();
        foreach (var block in used.OrderBy(b => b))
        {
            if (block == Painting) continue;   // (walled over)
            int all = 0, teal = 0;
            var blockCounts = new int[16];
            Citadel.CitadelMuseum.Repaint(library, block, Brick, (x, y, c) => { all++; blockCounts[c >> 4]++; if (c >> 4 == stucco) teal++; return c; });
            if (all == 0 || teal * 100 < all * 35) continue;
            // (in a stucco block, every ramp a fiftieth of it or more as well: the wallpaper's darker waves' outlines were left showing through
            // the night -- the user, 2026-11-14, "on the wall ... is still the outline of the waves from the original walls, let's have them
            // uniform")
            var blockRamps = new HashSet<int>(ramps);
            for (var r = 0; r < 16; r++) if (blockCounts[r] * 50 >= all) blockRamps.Add(r);
            log.Add($"Stella Entities' rooms: block {block}'s ramps painted the night: {string.Join(", ", blockRamps.OrderBy(r => r))} (its ramps: {string.Join(", ", Enumerable.Range(0, 16).Where(r => blockCounts[r] > 0).Select(r => $"{r} x{blockCounts[r]}"))})");
            var painted = Citadel.CitadelMuseum.Repaint(library, block, Brick, (x, y, c) =>
            {
                if (!blockRamps.Contains(c >> 4)) return c;
                var h = unchecked(((uint)x * 73856093u) ^ ((uint)y * 19349663u) ^ ((uint)block * 83492791u)) * 2654435761u;
                return (h >> 7) % 97 == 0 ? white : (h >> 7) % 173 == 1 ? yellow : flat;
            });
            if (painted.Count == 0) continue;
            try { swap[block] = Citadel.CitadelMuseum.AddBlock(bkg, header, ref library, block, painted); }
            catch (InvalidDataException e) { log.Add($"Stella Entities' rooms: block {block} left as it was -- {e.Message}"); }
        }
        int B(int b) => swap.TryGetValue(b, out var n) ? n : b;
        var set = new Dictionary<(int, int, int), (int Block, int Pos)>();
        for (var z = 0; z < Size; z++) for (var y = 0; y < Height; y++) for (var x = 0; x < Size; x++)
        { var c = Lba1GridEdit.Get(shape, x, y, z); if (swap.ContainsKey(c.Block)) set[(x, y, z)] = (swap[c.Block], c.Pos); }
        for (var y = 6; y <= 9; y++)
        {
            set[(WallX, y, VentZ)] = (B(WestWall), 35 + y - 6);
            set[(WallX, y, VentZ + 1)] = (B(WestWall), 5 + y - 6);
        }
        // (the painting, layers 4-9: the north wall's columns 1-3 there, as on the wall's layer 3 under it -- position column + 4 a layer up
        // from layer 1)
        for (var y = 4; y <= 9; y++)
            for (var k = 0; k < 3; k++)
                set[(PaintingX + k, y, NorthZ)] = (B(NorthWall), 1 + k + 4 * (y - 1));
        // the office's own (the user, 2026-11-16/17, item 10 of the 17th's list): Rick's desk, its armchairs and what stood on it gone, and the
        // invisible cells left over them -- a desk of the room's own stands against the west wall (CitadelProps.OfficeDesk, Install); his
        // plant moved, all its cells, to the corner right of the doorway (x 14-15, z 16-17); the curtains round his basin -- blocks mostly
        // of the orange ramp -- that ramp the purple's
        var office = new Dictionary<(int, int, int), (int Block, int Pos)>(set);
        var deskGone = new HashSet<(int, int)>();
        for (var z = 0; z < Size; z++) for (var y = 0; y < Height; y++) for (var x = 0; x < Size; x++)
            if (Furniture.Contains(Lba1GridEdit.Get(shape, x, y, z).Block)) { office[(x, y, z)] = (0, 0); deskGone.Add((x, z)); }
        for (var z = 0; z < Size; z++) for (var y = 0; y < Height; y++) for (var x = 0; x < Size; x++)
        { var c = Lba1GridEdit.Get(shape, x, y, z); if (c.Block == 0 && c.Pos != 0 && deskGone.Contains((x, z))) office[(x, y, z)] = (0, 0); }
        // (his wall lamp on the east wall by the bed's foot gone, the wall's own block there -- as in the dressing room; the user, 2026-11-17,
        // "let's remove this wall light")
        for (var z = 0; z < Size; z++) for (var y = 0; y < Height; y++) for (var x = 0; x < Size; x++)
            if (Lba1GridEdit.Get(shape, x, y, z).Block == WallLamp) office[(x, y, z)] = (B(EastWallBlock), 0);
        // (and the floor's shadows his desk and armchairs cast, darker tiles: the plain floor's tile there, its four by four pattern)
        for (var x = DeskShadowX0; x <= DeskShadowX1; x++) for (var z = DeskShadowZ0; z <= DeskShadowZ1; z++)
            if (Lba1GridEdit.Get(shape, x, 0, z).Block is FloorShadow or FloorShadow2) office[(x, 0, z)] = (B(FloorTile), x % 4 + 4 * (z % 4));
        var plantMoved = 0;
        for (var x = PlantX; x <= PlantX + 1; x++) for (var z = PlantZ; z <= PlantZ + 1; z++) for (var y = 1; y < Height; y++)
        {
            var c = Lba1GridEdit.Get(shape, x, y, z);
            if (c.Block != DressGone2 && !(c.Block == 0 && c.Pos != 0)) continue;
            office[(x, y, z)] = (0, 0);
            office[(x, y, z + PlantMoveZ)] = (B(c.Block), c.Pos);
            plantMoved++;
        }
        var curtains = new Dictionary<int, int>();
        foreach (var block in used.Where(b => !swap.ContainsKey(b) && b != Painting).OrderBy(b => b))
        {
            int all = 0, orange = 0;
            Citadel.CitadelMuseum.Repaint(library, block, Brick, (x, y, c) => { all++; if (c >> 4 == OrangeRamp) orange++; return c; });
            if (all == 0 || orange * 100 < all * 40) continue;
            var painted = Citadel.CitadelMuseum.Repaint(library, block, Brick, (x, y, c) => c >> 4 == OrangeRamp ? (byte)(CurtainRamp * 16 + (c & 15)) : c);
            if (painted.Count == 0) continue;
            try { curtains[block] = Citadel.CitadelMuseum.AddBlock(bkg, header, ref library, block, painted); }
            catch (InvalidDataException e) { log.Add($"Stella Entities' office: block {block} left orange -- {e.Message}"); }
        }
        for (var z = 0; z < Size; z++) for (var y = 0; y < Height; y++) for (var x = 0; x < Size; x++)
        { var c = Lba1GridEdit.Get(shape, x, y, z); if (curtains.TryGetValue(c.Block, out var to) && !office.ContainsKey((x, y, z))) office[(x, y, z)] = (to, c.Pos); }
        bkg.SetStored(libraryEntry, library);
        if (header.MaxBll < (uint)library.Length) header.MaxBll = (uint)library.Length;
        int Insert(Dictionary<(int, int, int), (int Block, int Pos)> cells, string what)
        {
            var grid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, cells.Select(kv => new Lba1GridCell(kv.Key.Item1, kv.Key.Item2, kv.Key.Item3, kv.Value.Block, kv.Value.Pos)).ToList()));
            grid[0] = raw[0];
            var number = header.Grm - header.Gri;
            if (number > 255) { log.Add("Stella Entities' rooms left out: LBA_BKG.HQR has 256 grids already"); return -1; }
            bkg.InsertAt(header.Grm, grid);
            header.Grm += 1; header.Bll += 1; header.Brk += 1;
            if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
            var (bytes, n) = MapBricks.UsedBrickBytes(grid, library, b => b < header.MaxBrk && !bkg.IsEmpty(header.Brk + b) ? BinaryPrimitives.ReadUInt32LittleEndian(bkg.ExtentOf(header.Brk + b)) : 0);
            if (header.MaxBrickCube < (uint)((n + 1) * 4 + bytes)) header.MaxBrickCube = (uint)((n + 1) * 4 + bytes);
            if (header.MaxMaskCube < (uint)((n + 1) * 4 + bytes + n * 64)) header.MaxMaskCube = (uint)((n + 1) * 4 + bytes + n * 64);
            bkg.SetStored(0, header.Bytes);
            log.Add($"Stella Entities' rooms: grid {number}, {what}");
            return number;
        }
        var rooms = Insert(office, $"Rick's office's with its stucco (colour ramps {string.Join(", ", ramps)}) the night in {swap.Count} blocks ({string.Join(", ", swap.Select(kv => $"{kv.Key} as {kv.Value}"))}), the vent and the painting walled over; " +
            $"the office's: his desk and armchairs gone ({deskGone.Count} columns), his plant to the corner right of the door ({plantMoved} cells), the curtains purple (blocks {string.Join(", ", curtains.Select(kv => $"{kv.Key} as {kv.Value}"))})");
        if (rooms < 0) return (-1, -1, -1);
        // the private room's: the same without Rick's furniture
        var bare = new Dictionary<(int, int, int), (int Block, int Pos)>(set);
        var cleared = new HashSet<(int, int)>();
        for (var z = 0; z < Size; z++) for (var y = 0; y < Height; y++) for (var x = 0; x < Size; x++)
            if (Furniture.Contains(Lba1GridEdit.Get(shape, x, y, z).Block)) { bare[(x, y, z)] = (0, 0); cleared.Add((x, z)); }
        // (the dressing room's too, the user, 2026-11-17: Rick's sofa, the plant by the lockers, the wall lamp the folding screen stood
        // against -- the wall's own block there instead; and the invisible solid cells left over his furniture, "an invisible barrier where
        // the desk previously was")
        for (var z = 0; z < Size; z++) for (var y = 0; y < Height; y++) for (var x = 0; x < Size; x++)
        {
            var cell = Lba1GridEdit.Get(shape, x, y, z);
            if (cell.Block is DressGone1 or DressGone2) bare[(x, y, z)] = (0, 0);
            else if (cell.Block == WallLamp) bare[(x, y, z)] = (EastWallBlock, 0);
            else if (cell.Block == 0 && cell.Pos != 0 && cleared.Contains((x, z))) bare[(x, y, z)] = (0, 0);
        }
        // (backstage on the office's grid, Rick's desk and armchairs kept: the user, 2026-11-14)
        var backstage = rooms;
        // (and the private room's: under the sofa's seat, solid invisible cells up to it -- as under the hall's booths: Twinsen sits on them)
        var seated = new Dictionary<(int, int, int), (int Block, int Pos)>(bare);
        for (var x = (SofaX - 900) / 512; x <= (SofaX + 900) / 512; x++)
            for (var z = (SofaZ - 480) / 512; z <= (SofaZ + 80) / 512; z++)
                for (var y = 1; y <= Citadel.CitadelProps.BoothSeat / 256; y++) seated[(x, y, z)] = (0, 1);
        return (rooms, backstage, Insert(seated, "the same, the private room's: the sofa's seat solid"));
    }

}
