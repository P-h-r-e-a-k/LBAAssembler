using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Otringal's easter eggs (chapter 5; RaceTrackStory.Eggs.cs says what the eggs are; their lines are the last of Otringal's,
// RaceTrackStory.Otringal.cs OtringalLines). The user, 2026-11-04, of the proposal's picks: "Let's make all those changes".
internal static partial class RaceTrackStory
{
    // ---- Of all the race tracks (the proposal: "ask Rick a second time after he's played the race music: "Of all the race tracks in
    // all the towns in all the worlds, you drive into mine." Third time: "Here's looking at you, kid. Now go and win."" -- LBA2's Rick is
    // Casablanca's already): Rick, a copy of the game's (scene 137's actor RickActor, in his office in the Lower City, where the story
    // doesn't go), in the Casino the story sends Twinsen to (scene 135: Rick's Café had its tables too), by its bar -- his first line
    // the walkthrough's ("The Cup? I'll play you something fast for it.", docs/LBRA_STORY_SCRIPT.md), the second and the third and after
    // Casablanca's: what was said to him in a visit counted (var_cube RickTalks). (Not built: the race music he plays -- the line alone.)
    private const int RickScene = 137, RickActor = 7, RickTalks = 71;
    // (where he stands: at the Casino's bar, between two of its stools, facing the room)
    private static readonly (int X, int Y, int Z, int Beta) RickAt = (22600, 512, 12100, 3072);

    private static void RickAtTheCasino(IslandBeatsA b, string when, int cup, int joint, int kid)
    {
        if (Copy(b.Originals, RickScene, RickActor) is not { Entity: >= 0 } rick || !b.Store.SceneExists(CasinoScene))
        {
            b.Log.Add($"Rick left out: no Rick in his office (scene {RickScene}'s actor {RickActor}), or no Casino");
            return;
        }
        b.Edit(CasinoScene, "Rick by the Casino's bar: the Cup, then Casablanca (var_cube " + RickTalks + ")", model =>
        {
            rick.X = RickAt.X; rick.Y = RickAt.Y; rick.Z = RickAt.Z; rick.Beta = RickAt.Beta; rick.Move = 0;
            return IslandBeatsA.Add(model, rick, Talker(when, $@"            add_var_cube({RickTalks}, 1);
            if (1 == var_cube({RickTalks}))
            {{
                message({cup});
            }}
            else if (2 == var_cube({RickTalks}))
            {{
                message({joint});
            }}
            else
            {{
                message({kid});
            }}"));
        });
    }

    // ---- Lyra's pool, Lyra's score (the proposal: "in the Imperial Hotel (scene 81), the running jump on the stairs that dives Twinsen
    // into the pool. During the Cup, Lyra is on a sun lounger by the pool on her day off: "Nine point five. You lose half a point for the
    // splash." Do it again: "Eight. You're showing off."" -- the dive the lava lake's (LavaDive) is made from): Lyra, the Cup's marshal --
    // the race-track mode's (TempleMarshal.GridGirlRig: the Imperial Hotel's own Sup, entity 317, in her bikini, the entity's body 40 and
    // her animations, her flags held like a judge's scorecards) -- standing on the pool's deck in chapter 5, beside where the dive comes
    // down. (The hotel has no sun lounger: she stands.)
    // The dive (the hero's life there: his track to label 10 in the stairs' zone 10, anim(76)) ends in the water, and Twinsen can't swim:
    // the pool has him, a clover is used and the game takes him back to where he last stood (OBJECT.CPP UseOneClover: RestartValidePos,
    // the scene as it was then -- every actor's state and var_cube with it -- but for the clovers, one fewer). So she can't watch the
    // dive itself (what she'd see is undone); she keeps the clovers Twinsen has (var_cube LyraClovers) and, when he has one fewer, he has
    // been in the pool: her score, raising her flags -- the first dive of a visit's, then the other's (var_cube LyraDives, kept: the scene
    // is saved again when Twinsen next leaves the ground). (A clover lost to anything else in the hotel would be a dive to her: nothing
    // else there takes one in chapter 5. With no clover left the game is over, and nobody scores.)
    // (var_cube 74: Twinsen has gone into the pool's water -- the engine's pool_splash=)
    internal const int PoolSplash = 74;
    // The achievements (RACEGAME.CPP's list; the race file's achieve= lines): Zoe's spanking is the chamber's var 96 at Scolded, the postcards the last card
    // variable at its reward, the Dinofly's talk var 194 (PolarDinofly), Stella Entities and the telescope's field the scenes. Not yet: the palace dungeon
    // and the bowling alley (the achievements' ids are in the engine's list already).
    internal static readonly string[] Achievements =
    {
        $"achieve=spanked var {Drunk} {Scolded}",
        $"achieve=postcards var {EggLastCard} {PostcardsRewarded}",
        $"achieve=dinofly var {Terrain.Polar.PolarDinofly.TalkedVar} 1",
        $"achieve=stella scene {StellaEntities.Scene}",
        "achieve=telescope scene 47",
    };
    private const int LyraClovers = 72, LyraDives = 73, LyraEntity = GridGirl.Entity, LyraBody = 40, LyraReady = 660, LyraRaise = 661;
    // (the clovers: the game variable FLAG_CLOVER (COMMON.H), at most MAX_CLOVER_BOX)
    private const int Clovers = 251, MostClovers = 10;
    // (on the deck at the pool's south side, a few cells from where the dive lands -- (18278, 1536, 18432) -- facing the water)
    private static readonly (int X, int Y, int Z, int Beta) LyraAt = (20480, 2048, 20736, 2048);

    // (`talk`: Lyra's at the start line, the same by the pool: Twinsen who met her there first couldn't reach her to talk -- the user,
    // 2026-11-09/11: "the only thing to work on for Otringal is being able to talk to Lyra so we don't have a softlock": the steward sells
    // no Travel Token till Lyra has told of the fee, Q_FEE 1. From the pool's edge: LyraPoolReach)
    private const int LyraPoolReach = 3200;
    private static void LyraPoolScore(IslandBeatsA b, string when, int nine, int eight, string talk)
    {
        if (!b.Store.SceneExists(HotelScene)) { b.Log.Add($"Lyra by the pool left out: no Imperial Hotel (scene {HotelScene})"); return; }
        // (a script compares a value with a number: the clovers kept, and compared, one number at a time)
        var keep = string.Concat(Enumerable.Range(0, MostClovers + 1).Select(k => $@"    if ({k} == var_game({Clovers}))
    {{
        set_var_cube({LyraClovers}, {k});
    }}
"));
        // (Twinsen swims in the pool now -- PoolSwim, RACEMOD.CPP pool_splash= -- and the dive no longer costs a clover: the engine sets var_cube
        // PoolSplash as he goes into the water, and she scores it)
        var fewer = $@"    if (1 == var_cube({PoolSplash}))
    {{
        set_var_cube({PoolSplash}, 0);
        set_comportement(comportement_3);
    }}
";
        b.Edit(HotelScene, $"Lyra by the hotel's pool: her score for the dive, a clover after it (var_cube {LyraClovers}, {LyraDives})", model =>
        {
            var lyra = SceneOps.BlankActor(SceneGame.Lba2, LyraAt.X, LyraAt.Y, LyraAt.Z, LyraEntity);
            lyra.Body = LyraBody; lyra.Anim = LyraReady; lyra.Beta = LyraAt.Beta; lyra.Flags = 0x0803; lyra.CoulObj = 9; lyra.Armor = 51;
            var index = model.Actors.Count;
            var add = IslandBeatsA.Add(model, lyra, $@"void comportement_0()
{{
    if ({when})
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
{keep}    set_comportement(comportement_2);
}}

void comportement_2()
{{
    swif (1 == action())
    {{
        if ({LyraPoolReach} > distance(0))
        {{
{talk}
        }}
    }}
{fewer}}}

void comportement_3()
{{
    add_var_cube({LyraDives}, 1);
    set_track(label_1);
    if (1 == var_cube({LyraDives}))
    {{
        message({nine});
    }}
    else
    {{
        message({eight});
    }}
    set_comportement(comportement_1);
}}
");
            return scripts =>
            {
                add(scripts);
                scripts.SetText(index, ScriptKind.Track, $@"label(0);
anim({LyraReady});
stop();

label(1);
anim({LyraRaise});
wait_anim();
wait_nb_second(3);
anim({LyraReady});

label(2);
stop();
");
            };
        });
    }
}
