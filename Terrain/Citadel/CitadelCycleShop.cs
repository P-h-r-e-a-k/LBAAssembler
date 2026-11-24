using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.LbaScript;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain.Citadel;

// The cycle shop, a room of its own (the user, 2026-11-21, after a first try dressed the baggage claim's hall: "Your cycle shop is looking
// pretty bad because you're designing it for the existing scene. Let's just design an awesome cycle shop and map it to the same entrance
// zone"). Scene Scene, built at the build's start (before the story, which puts its owner's talk and the Hoverboard's sale in:
// RaceTrackStory.CycleShop):
//   - its room the town shop's (scene 14: its walls, its tiled floor, its arched door in the east wall, its U of a counter with the till)
//     made 12 cells longer -- the north wall and the low front wall carried on, the arched east wall moved to the new end -- and emptied of
//     the shop's goods: its wall tables with their vases and toys, the plant, the pots and the crate by the door. The counter stays;
//   - the way in: the street's door that led into the baggage claim (scene 48's zone 11) leads in here, through the arch; the arch leads back
//     out to the street where the baggage claim's door did;
//   - a back door on the west wall (ShopDoorway), shut and locked (2026-11-22: it was a doorway through to the baggage claim's hall, scene 5,
//     whose way out came back in here -- the user, "we definitely don't want it to lead into the original shop"); the hall's way out the
//     game's again, to the street;
//   - the shop, Citadel Cycles (redesigned 2026-11-21 evening, after how high-end racing bike shops look -- CitadelProps.CycleShop): its
//     owner behind the counter, the baggage claim's own clerk (the user: "let's have it staffed by the same NPC who staffed the storage
//     place that it's replacing" -- scene 5's actor 9, a grobo, entity 33; he has left the hall: RaceTrackStory.CycleShop), the signed
//     jerseys on the wall behind him; on the north wall the Hoverboard left behind leaning there, the shop's name (the wall only 2,304
//     high: under it nothing taller than the tandem), the tandem, the racing bike hung up; the wall of wheels on the west wall by the
//     corner; on the floor the champion's bike on its plinth under a lamp, the town bike on its stand by the door, the penny-farthing on
//     its plinth along the front, the fitting bike on its trainer with a stand of saddles, the coffee bar and its stools at its end; the
//     workshop by the back doorway -- the tool wall and bench, the repair stand with a racer's bike on it. Each bike and the jerseys and
//     wheels: Action before it, its price tag or its plaque (RaceTrackStory.CycleShop, the hero's script);
//   - outside, over the baggage claim's old sign on its door (CITADEL.OBL body 33's, scene 48), the shop's banner (the user: "a temporary
//     shop banner across the old logo, so the old logo is still visible behind it").
internal static class CitadelCycleShop
{
    public const int Scene = 398, Island = 0;
    private const int Shop = 14, Street = 48, Hall = 5, Clerk = 33;
    public const int ClerkActor = 9;
    private const int Size = 64, Height = 25, FloorLayer = 3, Grow = 12, OldEast = 15, Front = 11;
    // the town shop's library: the floor's tile block and its plain tile; the north and west walls' blocks and their shaded copies (layers
    // 4-5, behind the wall tables: the north's in the same places, 2 x 2; the west's two columns at 0-1 and 2-3 against the wall's 0-1, 4-5)
    private const int FloorTile = 3, PlainTile = 6, NorthWall = 1, NorthShade = 57, WestWall = 2, WestShade = 58;
    public const int East = OldEast + Grow, FloorY = (FloorLayer + 1) * 256;
    // the counter and its till (kept where the shop has them): cells x 4-8, z 4-8
    private const int CounterX0 = 4, CounterX1 = 8, CounterZ0 = 4, CounterZ1 = 8;
    // the doors: the street's into the baggage claim (scene 48's zone 11) and the hall's back out to the street (scene 5's zone 8) -- each
    // its arrival as the game has it, kept
    private const int StreetDoor = 11, HallDoor = 8;
    // the back door: how near Twinsen's Action reaches it, and what he says (the game's "It's locked.", Citadel's text 110)
    private const int LockedReach = 900, LockedLine = 110;
    private static readonly int[] StreetArrival = { 11776, 256, 14848 }, HallArrival = { 30720, 3584, 18944 };
    // inside: in from the street just inside the arch (its opening z 5-6), facing west; the back doorway's middle on the west wall's face,
    // and in from the hall before it, facing east
    public const int ArchZ = 6 * 512, InFromStreetX = (East - 2) * 512, BackDoorX = 512, BackDoorZ = 5 * 512 + 256, InFromHallX = BackDoorX + 1100;
    // who stands where (scene units, the floor's; the walls' faces at x 512 and z 512): the owner behind the counter's till; the Hoverboard
    // against the north wall past the counter, its back on the wall (in the north-west corner it stood over the owner's head)
    public static readonly (int X, int Z, int Beta) OwnerAt = (2688, 2384, 1024);
    public static readonly (int X, int Z) BoardAt = (9 * 512 + 256, 512 + 16);
    // the north wall: the jerseys behind the counter, the tandem (turned along the wall) under the shop's name -- its middle at SignX, as
    // high as SignLowest or under the wall's top -- and the racing bike hung up past it; the wall of wheels on the west wall's face by the
    // corner (turned to face +x)
    public static readonly (int X, int Z) JerseysAt = (1742, 512), WheelsAt = (512, 1450);
    public static readonly (int X, int Z) HungAt = (11500, 512 + (int)CitadelProps.HungOut), TandemAt = (8200, 512 + 330);
    public const int SignX = 8200, SignLowest = 1600;
    // the floor: the champion's bike on its plinth under a lamp, the town bike on its stand by the door, the penny-farthing along the
    // front, the fitting bike and its saddles in the front corner by the door, the coffee bar and two stools at its east end, the workshop
    // by the back doorway (the tool wall on the west wall's face, the repair stand before it with a racer's bike)
    public static readonly (int X, int Z) PlinthAt = (8200, 2950), TownAt = (11776, 2304), PennyAt = (9472, 5000), FitAt = (12032, 4864), SaddlesAt = (13300, 4500);
    public static readonly (int X, int Z) CafeAt = (5632, 4880), ToolsAt = (512, 4352), StandAt = (1800, 4400);
    public static readonly (int X, int Z)[] StoolsAt = { (6850, 4650), (6850, 5150) };
    public const int LampHigh = 1500;
    // (the shop's goods on the counter: its two stands of glass bottles, blocks 76 and 90 -- the till, 91, stays)
    private static readonly HashSet<int> CounterGoods = new() { 76, 90 };
    // the actors' numbers in the scene, for the story's scripts: the owner, the Hoverboard, and what Action before it tells of
    public const int OwnerActor = 2, BoardActor = 3, TownActor = 4, RacingActor = 5, ChampionActor = 6, PennyActor = 7, TandemActor = 8,
                     RepairActor = 9, FitActor = 10, JerseysActor = 11, WheelsActor = 12;
    // outside: the banner over the baggage claim's sign (CITADEL.OBL body 33's: x 11332, z 14892-15492, y 1150-1650 -- the building's
    // front at x 11282, the ground at 250)
    public static readonly (int X, int Y, int Z) BannerAt = (11282, 250, 15192);

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(Shop) || !store.SceneExists(Street) || !store.SceneExists(Hall)) { log.Add("The cycle shop left out: no town shop (scene 14), street (48) or baggage claim (5)"); return log; }
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        if (Scene + 1 < scenes.Count && !scenes.IsEmpty(Scene + 1)) { log.Add($"The cycle shop left out: scene {Scene} is already another part of the build's"); return log; }
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        var type = table[Shop * 2];
        var raw = bkg.Read(header.Gri + table[Shop * 2 + 1]);
        var shape = Lba2GridBackend.ToLba1Shape(raw);
        var cells = Lba1GridCodec.Decode(shape);
        int At(int x, int y, int z) => ((z * Size + x) * Height + y) * 2;
        // (the town shop as the game has it: its arch in the east wall, its counter)
        if (cells[At(OldEast, 4, 4)] != 39 || cells[At(7, 6, 4)] != 60) { log.Add("The cycle shop left out: the town shop's grid isn't the game's (its arch or its counter moved)"); return log; }

        // ---- the room: columns as the town shop's, moved or copied
        var room = (byte[])cells.Clone();
        void Copy(int fx, int fz, int tx, int tz) { for (var y = 0; y < Height; y++) { room[At(tx, y, tz)] = cells[At(fx, y, fz)]; room[At(tx, y, tz) + 1] = cells[At(fx, y, fz) + 1]; } }
        // the arched east wall to the new end, the room carried on to it from 12 cells back (the walls' and the tiles' patterns repeat in 12)
        for (var z = 0; z <= Front; z++) Copy(OldEast, z, East, z);
        for (var x = OldEast; x < East; x++)
            for (var z = 0; z <= Front; z++) Copy(x - Grow, z, x, z);
        // the goods out: every cell over the floor inside the walls but the counter's, empty and open; the floor under the wall tables (the
        // shaded tile, block 32) the floor's own tile
        for (var z = 1; z < Front; z++)
            for (var x = 1; x < East; x++)
            {
                if (room[At(x, FloorLayer, z)] == 32) { room[At(x, FloorLayer, z)] = 3; room[At(x, FloorLayer, z) + 1] = 6; }
                var counter = x >= CounterX0 && x <= CounterX1 && z >= CounterZ0 && z <= CounterZ1;
                if (counter)
                {
                    for (var y = FloorLayer + 1; y < Height; y++)
                        if (CounterGoods.Contains(room[At(x, y, z)])) { room[At(x, y, z)] = 0; room[At(x, y, z) + 1] = 0; }
                    continue;
                }
                for (var y = FloorLayer + 1; y < Height; y++) { room[At(x, y, z)] = 0; room[At(x, y, z) + 1] = 0; }
            }
        // (and the shade the goods left: the floor's dark tiles where the wall tables' feet and the old east wall stood -- block 3's tiles
        // but its plain one, 6 -- and the walls' shaded bricks behind the tables, blocks 57 (the north wall's) and 58 (the west's), their own
        // walls' bricks in their places, blocks 1 and 2. The user, 2026-11-24: "lots of dark patches on the wall that are the remains of a
        // source shadow. There's also intermittent black blocks on the floor")
        for (var z = 0; z <= Front; z++)
            for (var x = 0; x <= East; x++)
                for (var y = FloorLayer; y < Height; y++)
                {
                    var i = At(x, y, z);
                    if (y == FloorLayer && room[i] == FloorTile && room[i + 1] != PlainTile && x >= 1 && x < East && z >= 1 && z < Front) room[i + 1] = PlainTile;
                    else if (room[i] == NorthShade) room[i] = NorthWall;
                    else if (room[i] == WestShade) { room[i] = WestWall; room[i + 1] = (byte)(room[i + 1] < 2 ? room[i + 1] : room[i + 1] + 2); }
                }
        var edits = new List<Lba1GridCell>();
        for (var z = 0; z <= Front; z++)
            for (var x = 0; x <= East; x++)
                for (var y = 0; y < Height; y++)
                    if (room[At(x, y, z)] != cells[At(x, y, z)] || room[At(x, y, z) + 1] != cells[At(x, y, z) + 1])
                        edits.Add(new Lba1GridCell(x, y, z, room[At(x, y, z)], room[At(x, y, z) + 1]));
        var grid = Lba2GridBackend.FromLba1Shape(raw, Lba1GridEdit.SetCells(shape, edits));
        grid[0] = raw[0];
        var gridNumber = header.Grm - header.Gri;
        if (gridNumber > 255) { log.Add("The cycle shop left out: LBA_BKG.HQR has 256 grids already"); return log; }
        bkg.InsertAt(header.Grm, grid);
        header.Grm += 1; header.Bll += 1; header.Brk += 1;
        if (header.MaxGri < (uint)grid.Length) header.MaxGri = (uint)grid.Length;
        table = bkg.Read(header.Brk + header.MaxBrk);
        if (table.Length < (Scene + 1) * 2) Array.Resize(ref table, (Scene + 1) * 2);
        table[Scene * 2] = type; table[Scene * 2 + 1] = (byte)gridNumber;
        bkg.SetStored(header.Brk + header.MaxBrk, table);
        bkg.SetStored(0, header.Bytes);

        // ---- the scene: the town shop's header (its light, its music), the doors, the shop's things
        var model = store.Load(Shop).Clone();
        model.Island = Island;
        model.Actors.RemoveRange(1, model.Actors.Count - 1);
        model.Zones.Clear();
        model.TrackPoints.Clear();
        var hero = model.Hero;
        (hero.X, hero.Y, hero.Z, hero.Beta) = (InFromStreetX, FloorY, ArchZ, 3072);
        hero.Life = new byte[] { 0 }; hero.Track = new byte[] { 0 };
        var texts = new Dictionary<int, (string Life, string Track)>();
        const string Still = "void comportement_0()\n{\n}\n", Stop = "label(0);\nstop();\n";
        // (actor 1 is the engine's own, cleared at each load: an unused helper there)
        texts[SceneOps.AddActor(model, RaceTrackScenes.InvisibleHelper(0, FloorY, 0))] = (Still, Stop);
        // the owner (his talk the story's), behind the till: the baggage claim's clerk
        var hallActors = store.Load(Hall).Actors;
        var owner = (hallActors.ElementAtOrDefault(ClerkActor) is { Entity: Clerk } clerk ? clerk : hallActors.Skip(1).FirstOrDefault(a => a.Entity == Clerk))?.Clone();
        if (owner is null) { log.Add($"The cycle shop left out: no clerk (entity {Clerk}) in the baggage claim (scene {Hall}) to keep it"); return log; }
        (owner.X, owner.Y, owner.Z, owner.Beta) = (OwnerAt.X, FloorY, OwnerAt.Z, OwnerAt.Beta);
        owner.Move = 0; owner.Life = new byte[] { 0 }; owner.Track = new byte[] { 0 };
        if (SceneOps.AddActor(model, owner) != OwnerActor) { log.Add("The cycle shop left out: its owner isn't actor 2"); return log; }
        texts[OwnerActor] = (Still, Stop);
        // the Hoverboard in the corner (its sale the story's)
        if (SceneOps.AddActor(model, CitadelProps.Actor(CitadelProps.LeaningBoard, BoardAt.X, FloorY, BoardAt.Z, 0)) != BoardActor) { log.Add("The cycle shop left out: the Hoverboard isn't actor 3"); return log; }
        texts[BoardActor] = (Still, Stop);
        // what Action before it tells of, in the story's order (TownActor on): the three bikes for sale, the penny-farthing, the tandem, the
        // racer's bike on the repair stand, the fitting bike, the jerseys, the wheels
        var told = new (int Generic, int X, int Y, int Z, int Beta)[]
        {
            (CitadelProps.ShopBikeTown, TownAt.X, FloorY, TownAt.Z, 1024 + 200),
            (CitadelProps.ShopBikeRacing, HungAt.X, FloorY, HungAt.Z, 1024),
            (CitadelProps.ShopBikeChampion, PlinthAt.X, FloorY + (int)CitadelProps.PlinthTop, PlinthAt.Z, 1024 + 320),
            (CitadelProps.PennyFarthing, PennyAt.X, FloorY, PennyAt.Z, 1024),
            (CitadelProps.Tandem, TandemAt.X, FloorY, TandemAt.Z, 1024),
            (CitadelProps.RepairBike, StandAt.X, FloorY + CitadelProps.StandHigh - 520, StandAt.Z, 0),
            (CitadelProps.FitBike, FitAt.X, FloorY, FitAt.Z, 1024),
            (CitadelProps.JerseyWall, JerseysAt.X, FloorY, JerseysAt.Z, 0),
            (CitadelProps.WheelWall, WheelsAt.X, FloorY, WheelsAt.Z, 1024),
        };
        for (var k = 0; k < told.Length; k++)
        {
            var (generic, x, y, z, beta) = told[k];
            if (SceneOps.AddActor(model, CitadelProps.Actor(generic, x, y, z, beta)) != TownActor + k) { log.Add($"The cycle shop left out: its things to look at aren't actors {TownActor}-{WheelsActor}"); return log; }
            texts[TownActor + k] = (Still, Stop);
        }
        // the shop's name on the north wall, as high as SignLowest or under the wall's top (its column's highest brick)
        var wallTop = FloorLayer;
        for (var y = Height - 1; y > FloorLayer; y--) if (room[At(SignX / 512, y, 0)] != 0) { wallTop = y; break; }
        var signLow = Math.Max(0, Math.Min(SignLowest, (wallTop + 1) * 256 - FloorY - (int)CitadelProps.SignHeight - 120));
        texts[SceneOps.AddActor(model, CitadelProps.Actor(CitadelProps.ShopSign, SignX, FloorY + signLow, 512, 0))] = (Still, Stop);
        // the rest, nothing to say: the champion's plinth and its lamp, the saddles, the coffee bar and its stools, the workshop
        foreach (var (generic, x, y, z, beta) in new (int, int, int, int, int)[]
                 {
                     (CitadelProps.ShowPlinth, PlinthAt.X, FloorY, PlinthAt.Z, 0), (CitadelProps.HangingLamp, PlinthAt.X, FloorY + LampHigh, PlinthAt.Z, 0),
                     (CitadelProps.SaddleStand, SaddlesAt.X, FloorY, SaddlesAt.Z, 0), (CitadelProps.CafeBar, CafeAt.X, FloorY, CafeAt.Z, 0),
                     (CitadelProps.ClubBarStool, StoolsAt[0].X, FloorY, StoolsAt[0].Z, 0), (CitadelProps.ClubBarStool, StoolsAt[1].X, FloorY, StoolsAt[1].Z, 0),
                     (CitadelProps.ToolWall, ToolsAt.X, FloorY, ToolsAt.Z, 0), (CitadelProps.RepairStand, StandAt.X, FloorY, StandAt.Z, 1024),
                 })
            texts[SceneOps.AddActor(model, CitadelProps.Actor(generic, x, y, z, beta))] = (Still, Stop);
        // the back door, shut and locked: Action at it, Twinsen's "It's locked." (the game's own line, 110) -- no way through to the baggage claim's
        // hall (the user, 2026-11-22, "The cycle shop, let's add a door at the back and have it locked. We may add something there later, but we
        // definitely don't want it to lead into the original shop"; it was a doorway, and the hall's way out came back in through it)
        var doorway = CitadelProps.Actor(CitadelProps.ShopDoorway, BackDoorX, FloorY, BackDoorZ, 0);
        doorway.Armor = RaceTrackStory.WalkOverArmour;
        texts[SceneOps.AddActor(model, doorway)] = ($@"void comportement_0()
{{
    swif (1 == action())
    {{
        if ({LockedReach} > distance(0))
        {{
            message_obj(0, {LockedLine});
        }}
    }}
}}
", Stop);
        // the door out: through the arch to the street, where the baggage claim's door put him
        model.Zones.Add(new SceneZoneModel
        {
            Type = 0, Num = Street, X0 = East * 512, X1 = (East + 1) * 512 - 1, Y0 = FloorY, Y1 = FloorY + 1792, Z0 = 5 * 512, Z1 = 7 * 512 - 1,
            Info = new[] { StreetArrival[0], StreetArrival[1], StreetArrival[2], 0, 0, 0, 0, 1 },
        });
        var built = SceneScripts.Load(SceneSerializer.Write(model), Scene);
        built.SetText(0, ScriptKind.Life, Still);
        built.SetText(0, ScriptKind.Track, Stop);
        foreach (var (actor, (life, track)) in texts) { built.SetText(actor, ScriptKind.Life, life); built.SetText(actor, ScriptKind.Track, track); }
        var result = built.Build();
        if (!result.Ok) { log.AddRange(result.Errors.Select(e => $"The cycle shop left out: {e}")); return log; }
        void Store(int scene, byte[] record)
        {
            while (scenes.Count <= scene + 1) scenes.Slots.Add(new HqrFile.Slot());
            scenes.SetStored(scene + 1, record);
            if (record.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
            { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, record.Length); scenes.SetStored(0, size); }
        }
        Store(Scene, result.Record!);

        // ---- the street's door, to the shop (its scripts as they are); the hall's way out left the game's, out to the street -- the shop's
        // back door is locked
        foreach (var (scene, zone, x, z) in new[] { (Street, StreetDoor, InFromStreetX, ArchZ) })
        {
            var m = store.Load(scene);
            // (the game's zone by its number, else the one door to the other: the race build moves an outside scene's zones about)
            var to = scene == Street ? Hall : Street;
            var door = m.Zones.ElementAtOrDefault(zone) is { Type: 0 } z0 && z0.Num == to ? z0 : m.Zones.Where(z => z.Type == 0 && z.Num == to).ToList() is { Count: 1 } one ? one[0] : null;
            if (door is null) { log.Add($"The cycle shop: scene {scene}'s door to {to} isn't the game's -- not to the shop"); continue; }
            door.Num = Scene;
            door.Info = new[] { x, FloorY, z, 0, 0, 0, 0, 1 };
            // (the street: the shop's banner over the baggage claim's sign -- walked past, not into)
            var banner = -1;
            if (scene == Street)
            {
                var b = CitadelProps.Actor(CitadelProps.ShopBanner, BannerAt.X, BannerAt.Y, BannerAt.Z, 0);
                b.Armor = RaceTrackStory.WalkOverArmour;
                banner = SceneOps.AddActor(m, b);
            }
            var streetScripts = SceneScripts.Load(SceneSerializer.Write(m), scene);
            if (banner >= 0) { streetScripts.SetText(banner, ScriptKind.Life, Still); streetScripts.SetText(banner, ScriptKind.Track, Stop); }
            var again = streetScripts.Build();
            if (!again.Ok) { log.AddRange(again.Errors.Select(e => $"The cycle shop: scene {scene}'s door not to it: {e}")); continue; }
            Store(scene, again.Record!);
        }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        log.Add($"the cycle shop: scene {Scene}, the town shop's room 12 cells longer (grid {gridNumber}), emptied but its counter; the street's door (scene {Street}) " +
                $"leads into it, its arch out to the street, its back door locked (the baggage claim, scene {Hall}, not through it); Citadel Cycles: " +
                $"its owner (the baggage claim's clerk, entity {Clerk}), three bikes for sale, the penny-farthing, the tandem, a racer's bike on the repair stand, " +
                $"the fitting bike, the jerseys, the wheels, the coffee bar, the workshop, the Hoverboard; its name on the north wall {signLow} over the floor " +
                $"(the wall's top {(wallTop + 1) * 256 - FloorY}); its banner outside over the baggage claim's sign");
        return log;
    }
}
