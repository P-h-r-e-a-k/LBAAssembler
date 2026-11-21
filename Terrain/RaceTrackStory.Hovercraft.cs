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
//     hacienda and Kash's sea cave are driven to and walked into.
internal static partial class RaceTrackStory
{
    public const int HireVar = 275;
    private const int HireScene = 70, HireManFrom = 71, HireManEntity = 177, HirePrice = 10, HireReach = 2500, HireSeaY = 0;
    private static readonly (int X, int Y, int Z, int Beta) HireManAt = (5770, 170, 9848, 3584);
    private static readonly (int X, int Y, int Z, int Beta)[] ParkedHovercraft = { (6794, 125, 8568, 1536), (7050, 125, 9336, 1400) };
    private static readonly (int X, int Z, int Beta) HireAt = (8074, 8824, 1536);

    // The story file's line (RACEMOD.CPP hovercraft=): the variable, the Desert (island 2), the racer's entity's bodies -- him in it, nobody in
    // it -- and its driving animation (generic 1), the sea's height, where the hire starts and how near it he gives it back
    internal static string HovercraftLine =>
        $"hovercraft={HireVar} {RaceTrackIsland.Desert.IslandByte} {RaceTrackBaldinoCar.RacerEntity} {RaceTrackCharacterCars.HovercraftDriveGeneric} {RaceTrackCharacterCars.HovercraftGeneric} 1 " +
        $"{HireSeaY} {HireScene} {HireAt.X} {HireAt.Z} {HireAt.Beta} {HireReach}\n";

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
    }
}
