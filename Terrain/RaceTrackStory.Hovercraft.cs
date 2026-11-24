using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The hovercraft hire on the Desert's beach (the user, 2026-11-20: "Let's add a hovercraft hire area at Twinsen's current location in-game.
// The hovercraft's will be at the shore edge near him. We should have an NPC there for Twinsen to hire from, let's charge 10 kashes";
// "Twinsen hiring the hovercraft should allow Twinsen to drive the hovercraft around in the sea. Twinsen should be able to use it to reach
// both the island across from the hacienda and the area where we've put the racers tomb"): scene 70 (cube (10,8)), on the sand between the
// track and the sea where the user stood (4746, 11896):
//   - the hire man (the Desert's beach man, scene 71's entity 177), and two hovercraft parked at the sand's edge (LBA1's hydroglider:
//     RaceTrackCharacterCars.InstallHovercraft, the racer's entity's body HovercraftGeneric);
//   - Action by him: "Fancy a hovercraft?" -- ten kashes, yes or no; paid, HireVar 1: the engine (RACEMOD.CPP hovercraft=) puts Twinsen in
//     his car at the water's edge (HireAt), the car the hovercraft, held on the sea and not drowned, his own car kept; given back on foot
//     within HireReach of there (or off the island), his own car where it was, and HireVar 0. The sea all round: the island across from the
//     hacienda are driven to and walked onto, and Kash's sea cave driven into -- on its water and up a ramp onto its ledge (SeaCaveRamp).
internal static partial class RaceTrackStory
{
    public const int HireVar = 275;
    private const int HireScene = 70, HireManFrom = 71, HireManEntity = 177, HirePrice = 10, HireReach = 2500, HireSeaY = 0;
    private static readonly (int X, int Y, int Z, int Beta) HireManAt = (5770, 170, 9848, 3584);
    // (the two along the shore's edge, well apart and clear of the hire point -- they stood 800 apart, one in the other: the user,
    // 2026-11-22, "the two hydroglyders at the hire point appear on top of each other, let's have them separated")
    private static readonly (int X, int Y, int Z, int Beta)[] ParkedHovercraft = { (6600, 90, 8000, 1536), (8800, 150, 10500, 1400) };
    // Kash's sea cave, behind the door on the sea north of the hire: driven into (drive_in=), the game's own way in -- its turtle raft,
    // Twinsen aboard ("on the raft", game variable 143) -- never called, so the raft goes as the game's own script has it
    private const int SeaCave = 113;
    // (and in the cave, the hovercraft drawn by a stand-in of its own entity, marked to be walked over -- where he drives it or where it's
    // parked: RACEMOD.CPP HoverRaftFrame. Placed at the raft's own place, actor 2's, till the engine moves it)
    private const int CaveRaft = 2;
    // (the room's own car: the island's car actor -- scene 64's, entity 152 -- on the ledge at the ramp's head, facing it; its life the game's
    // car's, less its getting in by standing on it: in by Action, out by Action, and the engine's init_buggy to keep it there)
    private const int CarFrom = 64, CarEntity = 152;
    private static readonly (int X, int Y, int Z, int Beta) CaveCarAt = (4864, 768, 11776, 0);
    private const string CaveCarLife = @"void comportement_0()
{
    init_buggy(0);
    if (12 == comportement_hero())
    {
        set_comportement(comportement_2);
    }
    else
    {
        set_comportement(comportement_1);
    }
}

void comportement_1()
{
    if (12 == comportement_hero())
    {
        set_var_cube(30, 1);
        set_comportement(comportement_2);
    }
    if (1 == action())
    {
        if (768 > angle_obj(0) && 1500 > distance(0))
        {
            set_var_cube(30, 1);
            set_dir_obj(0, MOVE_BUGGY_MANUAL);
            set_comportement(comportement_2);
        }
    }
}

void comportement_2()
{
    if (0 != var_cube(30))
    {
        if (0 == action())
        {
            set_var_cube(30, 0);
        }
    }
    else if (2 == action())
    {
        set_dir_obj(0, MOVE_MANUAL);
        set_comportement(comportement_1);
    }
    if (12 != comportement_hero())
    {
        set_dir_obj(0, MOVE_MANUAL);
        set_comportement(comportement_1);
    }
}
";
    // The hire scene's cube, (10, 8): its people and things seen from the cubes round it in world units (RACEMOD.CPP far_actor=)
    private const int HireCubeX = 10 * 32768, HireCubeZ = 8 * 32768;
    private static readonly (int X, int Z, int Beta) HireAt = (8074, 8824, 1536);

    // The story file's line (RACEMOD.CPP hovercraft=): the variable, the Desert (island 2), the racer's entity's bodies -- him in it, nobody in
    // it -- and its driving animation (generic 1), the sea's height, where the hire starts and how near it he gives it back
    internal static string HovercraftLine =>
        $"hovercraft={HireVar} {RaceTrackIsland.Desert.IslandByte} {RaceTrackBaldinoCar.RacerEntity} {RaceTrackCharacterCars.HovercraftDriveGeneric} {RaceTrackCharacterCars.HovercraftGeneric} 1 " +
        $"{HireSeaY} {HireScene} {HireAt.X} {HireAt.Z} {HireAt.Beta} {HireReach}\n" +
        // (the hire man standing, and the two hovercraft, from round the hire scene -- the user, 2026-11-22: "We need to make sure the
        // hydroglyder hire scene the actors are visible from neighbouring squares")
        $"far_actor={RaceTrackIsland.Desert.IslandByte} {HireScene} {HireManEntity} 0 0 {HireCubeX + HireManAt.X} {HireManAt.Y} {HireCubeZ + HireManAt.Z} {HireManAt.Beta}\n" +
        string.Concat(ParkedHovercraft.Select(h =>
            $"far_actor={RaceTrackIsland.Desert.IslandByte} {HireScene} {RaceTrackBaldinoCar.RacerEntity} {RaceTrackCharacterCars.HovercraftGeneric} -1 {HireCubeX + h.X} {h.Y} {HireCubeZ + h.Z} {h.Beta}\n"));

    private static void HovercraftHire(IslandBeatsA b, Func<DesertLine, int> say, string desert)
    {
        if (!b.Store.SceneExists(HireScene)) { b.Log.Add($"the hovercraft hire left out: no scene {HireScene}"); return; }
        var man = CopyOf(b.Originals, HireManFrom, HireManEntity);
        var racer = b.Store.Load(RaceTrackScenes.RacerScene).Actors.Skip(1).FirstOrDefault(a => a.Entity == RaceTrackScenes.RacerEntity && a.Body >= 0)
                    ?? b.Originals.Load(RaceTrackScenes.RacerScene).Actors.Skip(1).FirstOrDefault(a => a.Entity == RaceTrackScenes.RacerEntity);
        if (man is null) { b.Log.Add($"the hovercraft hire left out: no beach man (entity {HireManEntity}) in scene {HireManFrom}"); return; }
        int yes = say(DesertLine.HireYes), no = say(DesertLine.HireNo);
        b.Edit(HireScene, $"the hovercraft hire on the beach: its man, two hovercraft at the sand's edge, ten kashes a hire (variable {HireVar})", model =>
        {
            var edits = new List<Action<SceneScripts>>();
            (man.X, man.Y, man.Z, man.Beta) = HireManAt;
            man.Move = 0;
            edits.Add(IslandBeatsA.Add(model, man, $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if (1500 > distance(0) && {Driving} != comportement_hero())
        {{
            if (2 == var_game({HireVar}))
            {{
                message({say(DesertLine.HireOut)});
            }}
            else
            {{
                message({say(DesertLine.HireAsk)});
                add_choice({yes});
                add_choice({no});
                ask_choice({say(DesertLine.HirePrice)});
                if ({yes} == choice())
                {{
                    if ({HirePrice} <= nb_gold_pieces())
                    {{
                        give_gold_pieces({HirePrice});
                        message({say(DesertLine.HireGo)});
                        set_var_game({HireVar}, 1);
                    }}
                    else
                    {{
                        message({say(DesertLine.HireNoMoney)});
                    }}
                }}
                else
                {{
                    message({say(DesertLine.HireLater)});
                }}
            }}
        }}
    }}
}}
"));
            if (racer is not null)
                foreach (var (x, y, z, beta) in ParkedHovercraft)
                {
                    var boat = racer.Clone();
                    (boat.X, boat.Y, boat.Z, boat.Beta) = (x, y, z, beta);
                    boat.Body = RaceTrackCharacterCars.HovercraftGeneric;
                    boat.Flags &= ~1u; boat.Move = 0;
                    edits.Add(IslandBeatsA.Add(model, boat, "void comportement_0()\n{\n}\n"));
                }
            return scripts => { foreach (var e in edits) e(scripts); };
        });
        // the cave driven into (the user, 2026-11-24: "We need to stop the scripting, have Twinsen back in the hydroglyder, and let's add a
        // small slope that Twinsen can use to park on land"): the hovercraft through its door and on its water (drive_in=, RACEMOD.CPP), up
        // the ramp onto the ledge (SeaCaveRamp) -- the turtle raft never called (no hover_door_var=; it goes, OnTheRaft 0, as the game's own
        // script has it). The hovercraft's stand-in, drawn where he drives it or where it's parked (RACEMOD.CPP HoverRaftFrame), and the
        // room's own car -- the island's car actor's (scene 64's, entity 152), parked on the ledge -- so that out of it there it stays,
        // and in again by it (BUGGY.CPP LeaveBuggy, InitBuggy)
        var islandCar = b.Store.SceneExists(CarFrom) ? b.Store.Load(CarFrom).Actors.FirstOrDefault(a => a.Entity == CarEntity) : null;
        if (racer is not null && b.Store.SceneExists(SeaCave))
            b.Edit(SeaCave, "Kash's sea cave: the hovercraft driven in, its stand-in and the room's own car on the ledge (RACEMOD.CPP drive_in=, HoverRaftFrame)", model =>
            {
                var raft = model.Actors.ElementAtOrDefault(CaveRaft);
                if (raft is null) { b.Log.Add($"the sea cave's hovercraft left out: no raft (actor {CaveRaft}) in scene {SeaCave}"); return _ => { }; }
                var boat = racer.Clone();
                (boat.X, boat.Y, boat.Z, boat.Beta) = (raft.X, raft.Y, raft.Z, raft.Beta);
                boat.Body = RaceTrackCharacterCars.HovercraftGeneric;
                boat.Flags = 0x1000 | 0x200;   // (no shadow; hidden -- the engine shows it)
                boat.Move = 0;
                boat.Armor = WalkOverArmour;   // (walked over, nothing stopped by it: RACEMOD.CPP RaceMod_NoCollide)
                var edits = new List<Action<SceneScripts>> { IslandBeatsA.Add(model, boat, "void comportement_0()\n{\n}\n") };
                if (islandCar is null) b.Log.Add($"the sea cave's own car left out: no car (entity {CarEntity}) in scene {CarFrom}");
                else
                {
                    var car = islandCar.Clone();
                    (car.X, car.Y, car.Z, car.Beta) = CaveCarAt;
                    edits.Add(IslandBeatsA.Add(model, car, CaveCarLife));
                }
                return scripts => { foreach (var e in edits) e(scripts); };
            });
    }
}
