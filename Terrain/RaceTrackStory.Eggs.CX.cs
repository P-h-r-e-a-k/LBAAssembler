using System.IO;
using System.Text.Json;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Island CX's easter eggs (chapter 12, RaceTrackStory.CxTower.cs; the shared variables in RaceTrackStory.Eggs.cs): the egg itself here, its
// lines at the end of CxTower's and a call from ApplyCx.
internal static partial class RaceTrackStory
{
    // ---- Baldino's grey paint works ------------------------------------------------------------------------------------------------
    // The user, 2026-11-04: "Let's make all those changes" -- the proposal's: "finish the CX race without a single cannon hit. The
    // trooper by the cannons: 'Not one hit! I never saw you. Is that grey paint?' Over the radio, Baldino: 'Science!'" (his call on
    // arrival: "I've painted the car grey so the gunners don't see you. That's not a joke, it's science. Mostly."). The race-track mode
    // sets EggGrey to 1 at a finish without a cannon hit (no_hit=); after the race -- once Lyra has said her won line this visit (var_cube
    // Once1, CxTower's), Twinsen on foot -- the chapter's Franco trooper says so, Baldino answers over the radio, and EggGrey is 2 (said
    // once). Without it nothing changes. A helper by the trooper (`at`), its life each frame: the variable is set while the scene is on,
    // at the finish.
    private static Action<SceneScripts> CxGreyPaint(LateStory s, SceneModel model, (double X, double Z, double Y, int Beta) at, int trooper, int trooperLine, int baldinoLine)
    {
        var helper = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(at.X, model.CubeX), (int)Math.Round(at.Y), RaceTrackScenes.CubeLocal(at.Z, model.CubeY), 9);
        return s.Add(model, helper, $@"void comportement_0()
{{
    if ({CxChapter} == chapter())
    {{
        set_comportement(comportement_1);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
    if (1 == var_game({EggGrey}) && {KeyWon} <= var_game({QCxKey}) && 1 == var_cube({Once1}) && {Driving} != comportement_hero())
    {{
        set_var_game({EggGrey}, 2);
        message_obj({trooper}, {trooperLine});
        message({baldinoLine});
    }}
}}
");
    }

    // ---- The staff passage ---------------------------------------------------------------------------------------------------------
    // The user, 2026-11-04: "Let's make all those changes" -- the proposal's: "in the halls, a narrow door: 'STAFF ONLY. TO THE
    // EMPEROR'S PALACE.' Through it (on foot, after the race), a short passage that comes out of the palace dungeon's fireplace, where
    // the race's secret warp already goes" -- LBA2's scene 180, Island CX's secret passage (it runs under a room of the Emperor's): a
    // corridor of cages, a lift at each end, its kashes and its clover box (var 244) as the game has them. Built:
    //   - the door: in the halls, by the road's inner curb on the long straight past the gas monsters, against the control room's south
    //     wall (Island CX Mk2's scene of the cube that holds StaffDoor -- the start scene), one of Island CX's sliding door panels
    //     (SPRITES.HQR 159, the game's scene 181's) standing there, its sign read with Action: before chapter 12's race is won "STAFF
    //     ONLY." and locked; won (Q_CXKEY 4 on, so also once the note is read and after), the sign says where it goes and Twinsen goes
    //     through: change_cube(180), at the scene's start -- on its east lift, at the foot;
    //   - scene 180's two ways up to the game's scene 181 (its lifts' tops, zones of type 0) now its ends: the west one into the palace's
    //     dungeon -- where the race's warp through room 159's fireplace comes out (PALACE.JSON's dungeon "arrive"), its scene the palace
    //     cube's that holds it -- and the east one back out in front of the door in the halls (zones, so scenes past 255 are named);
    //   - and the way back from the dungeon: another of Island CX's door panels against the dungeon's south wall, behind where one
    //     comes in, its sign "STAFF ONLY. TO ISLAND CX." (the palace's texts, Otringal's file), and a zone in front of it into scene 180
    //     at the foot of its west lift.
    // The way back is there so that the palace reached early breaks nothing: through the door between the race won and the note read
    // (Q_CXKEY 4, chapter 12) Twinsen is in the palace a chapter early -- its people only there in chapter 13 (RaceTrackStory.Palace.cs:
    // each a Talker on 13 == chapter(), nothing set before), its race shut until Stan's (gate=224 6) -- and he walks back to read the note.
    // In chapter 13 the same way back takes him to Island CX, where nobody is any more, and back again. Island CX's own track (scene 300)
    // has no halls of this size: no passage there.
    private const int StaffPassage = 180, PassageSprite = 159;
    // the door: island cells (Island CX Mk2's, found in the built game, 2026-11-04: on the straight at z 542 the hall's floor is at -10332,
    // and on foot the road's inner curb is as far as Twinsen goes -- past it, at z 533.2, the island's rock over the halls stands up to
    // the control room's south wall: the door on the grey strip between, up to the wall), and where he comes back out in front of it (on
    // the road)
    private static readonly (double X, double Z) StaffDoor = (524.0, 534.2), StaffDoorOut = (524.0, 537.0);
    private const int HallFloor = -10332;
    // scene 180's ends (the game's): its lifts' tops' zones of type 0, by their corner; and where one comes back in from the dungeon: on
    // the corridor's floor east of the west lift's cage, by its first kashes (put there on the lift's top as the game's way down from
    // scene 181 puts him, the lift wasn't under him: he fell to its foot, hurt -- 2026-11-04)
    private static readonly (int X0, int Z0) PassageWest = (11264, 17920), PassageEast = (25600, 17920);
    private static readonly (int X, int Y, int Z) PassageWestFoot = (13056, 768, 17920);
    private const int DontReadjust = 2;     // (a zone's Info6: ZONE_DONT_REAJUST_POS_TWINSEN)
    // the palace's text file (the palace race's island byte, Otringal's 4: TEXT.HQR 3 + 4) for the dungeon door's sign
    private const int PalaceTextFile = 3 + 4;

    private static readonly string[][] StaffPalaceLines =
    {
        new[]
        {
            "A narrow door. \"STAFF ONLY. TO ISLAND CX.\"",
            "Une porte étroite. « RÉSERVÉ AU PERSONNEL. VERS L'ÎLOT CX. »",
            "Eine schmale Tür. »NUR FÜR PERSONAL. ZUM EILAND CX.«",
            "Una puerta estrecha. «SOLO PERSONAL AUTORIZADO. AL ISLOTE CX.»",
            "Una porta stretta. «RISERVATO AL PERSONALE. ALL'ISOLOTTO CX.»",
            "Uma porta estreita. «SÓ PESSOAL AUTORIZADO. PARA A ILHOTA CX.»",
        },
    };

    // `locked`, `open`: the door's sign, its two texts (Island CX's file). Lines for the log in s.Log.
    private static void CxStaffPassage(LateStory s, int locked, int open)
    {
        if (s.Island != RaceTrackIsland.CxTowerMk2) { s.Log.Add("the staff passage left out: Island CX's own track has no halls for its door (Island CX Mk2's has)"); return; }
        var (palaceScene, arrive, floorTop, southWall) = DungeonLanding(s);
        if (palaceScene < 0) { s.Log.Add("the staff passage left out: no palace dungeon in the folder (PALACE.JSON, its scenes) -- the palace is built before Island CX"); return; }
        if (!s.Store.SceneExists(StaffPassage)) { s.Log.Add($"the staff passage left out: no scene {StaffPassage}"); return; }
        var doorScene = Enumerable.Range(s.Island.FirstScene, 1).Concat(s.Island.MoreScenes).FirstOrDefault(sc => s.Store.SceneExists(sc) && s.Store.Load(sc) is var m && m.CubeMode == 1
            && m.CubeX == (int)Math.Floor(StaffDoor.X / 64) && m.CubeY == (int)Math.Floor(StaffDoor.Z / 64), -1);
        if (doorScene < 0) { s.Log.Add("the staff passage left out: no scene of Island CX Mk2's holds the halls' door"); return; }
        int doorCubeX = (int)Math.Floor(StaffDoor.X / 64), doorCubeZ = (int)Math.Floor(StaffDoor.Z / 64);

        // the door in the halls
        s.Edit(doorScene, "the staff passage: a narrow door in the halls, \"STAFF ONLY.\" -- once the race is won, into scene 180", model =>
        {
            var door = RaceTrackScenes.SpriteProp(RaceTrackScenes.CubeLocal(StaffDoor.X, model.CubeX), HallFloor, RaceTrackScenes.CubeLocal(StaffDoor.Z, model.CubeY), PassageSprite, 9);
            return s.Add(model, door, Talker($"{CxChapter} <= chapter()", $@"            if ({KeyWon} <= var_game({QCxKey}))
            {{
                message({open});
                change_cube({StaffPassage});
            }}
            else
            {{
                message({locked});
            }}", 1100));
        });

        // scene 180's ends: west into the dungeon, east back to the door
        s.Edit(StaffPassage, $"the staff passage: scene {StaffPassage}'s west lift up into the palace's dungeon (scene {palaceScene}), its east lift back to the halls' door (scene {doorScene})", model =>
        {
            var west = model.Zones.FirstOrDefault(z => z.Type == 0 && z.X0 == PassageWest.X0 && z.Z0 == PassageWest.Z0) ?? throw new NotTheGames("no way up at scene 180's west end");
            var east = model.Zones.FirstOrDefault(z => z.Type == 0 && z.X0 == PassageEast.X0 && z.Z0 == PassageEast.Z0) ?? throw new NotTheGames("no way up at scene 180's east end");
            int palaceCubeX = (int)Math.Floor(arrive.X / 64), palaceCubeZ = (int)Math.Floor(arrive.Z / 64);
            // (he comes out where he stood in the zone, from its corner, and as high over the given height as he stood over its foot: so the
            // zone's corner the middle less half the zone's width. Into the dungeon the floor found under him (Info6 0); into the halls just
            // where he is put (Info6 ZONE_DONT_REAJUST_POS_TWINSEN): found there, his place was the island's ground over the halls' roof,
            // 10,000 over their floor -- a fall that killed him, 2026-11-04)
            int Out(double cells, int cube, int zoneFrom, int zoneTo) => RaceTrackScenes.CubeLocal(cells, cube) - (zoneTo - zoneFrom) / 2;
            west.Num = palaceScene;
            west.Info = new[] { Out(arrive.X, palaceCubeX, west.X0, west.X1), floorTop + 100, Out(arrive.Z, palaceCubeZ, west.Z0, west.Z1), 0, 0, 0, 0, 1 };
            east.Num = doorScene;
            east.Info = new[] { Out(StaffDoorOut.X, doorCubeX, east.X0, east.X1), HallFloor + 16, Out(StaffDoorOut.Z, doorCubeZ, east.Z0, east.Z1), 0, 0, 0, DontReadjust, 1 };
            return _ => { };
        });

        // the way back: a door against the dungeon's south wall, behind where one comes in, and a zone in front of it
        var sign = s.Texts(PalaceTextFile, StaffPalaceLines, "the staff passage's door in the palace dungeon");
        s.Edit(palaceScene, $"the staff passage: a door in the dungeon's south wall, \"STAFF ONLY. TO ISLAND CX.\", back into scene {StaffPassage} (a palace scene, edited from Island CX's story)", model =>
        {
            const int Reach = 512;          // (a zone two cells across and one deep, against the wall: the sign is read from further off -- 1,500)
            int x = RaceTrackScenes.CubeLocal(arrive.X, model.CubeX), back = RaceTrackScenes.CubeLocal(southWall, model.CubeY);
            if (back > IslandFile.CubeSize - 1) throw new NotTheGames("the dungeon's south wall isn't in the scene of where one comes in");
            model.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = StaffPassage, X0 = x - Reach, X1 = x + Reach, Z0 = back - Reach, Z1 = back, Y0 = floorTop - 500, Y1 = floorTop + 2500,
                // (on the corridor's floor by the west lift: he stands 500 over the zone's foot, on the dungeon's floor)
                Info = new[] { PassageWestFoot.X, PassageWestFoot.Y - 500, PassageWestFoot.Z, 0, 0, 0, 0, 1 },
            });
            var door = RaceTrackScenes.SpriteProp(x, floorTop, back - 100, PassageSprite, 9);
            return s.Add(model, door, Talker("0 <= chapter()", $"            message({sign});", 1500));
        });
        s.Log.Add($"the staff passage: the door in scene {doorScene}, scene {StaffPassage} (var 244 its clover box, as the game's), out in the palace's dungeon in scene {palaceScene}; " +
                  $"open once Q_CXKEY ({QCxKey}) is {KeyWon} or more; the way back from the dungeon's south wall");
    }

    // The palace dungeon's landing (PALACE.JSON, PalaceIsland's layout: where the race's warp through room 159's fireplace comes out, island
    // cells, its floor's top, its south wall's inside face) and the palace scene of its cube: (-1, ...) when the palace isn't in the folder.
    private static (int Scene, (double X, double Z) Arrive, int FloorTop, double SouthWall) DungeonLanding(LateStory s)
    {
        var path = Path.Combine(s.Game, LBAAssembler.Terrain.Palace.PalaceIsland.LayoutFile);
        if (!File.Exists(path)) return (-1, default, 0, 0);
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            if (!json.RootElement.TryGetProperty("dungeon", out var d) || d.ValueKind != JsonValueKind.Object) return (-1, default, 0, 0);
            var a = d.GetProperty("arrive");
            (double X, double Z) arrive = (a[0].GetDouble(), a[1].GetDouble());
            var scene = LBAAssembler.Terrain.Palace.PalaceTrack.SceneOf((int)Math.Floor(arrive.X / 64), (int)Math.Floor(arrive.Z / 64));
            if (scene < 0 || !s.Store.SceneExists(scene)) return (-1, default, 0, 0);
            return (scene, arrive, (int)Math.Round(d.GetProperty("floorTop").GetDouble()), d.GetProperty("z1").GetDouble());
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            s.Log.Add($"the palace's layout (PALACE.JSON) not read: {e.Message}");
            return (-1, default, 0, 0);
        }
    }
}
