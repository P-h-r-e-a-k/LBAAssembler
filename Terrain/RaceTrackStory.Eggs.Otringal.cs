using System.IO;
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

    // (lyra, black: while the race is full -- Q_FEE 1, RaceTrackStory.OtringalDays -- where to find Lyra, and what he saw of the black
    // car's driver, 2026-11-19, the user: "somebody who knows somebody")
    private static void RickAtTheCasino(IslandBeatsA b, string when, int cup, int joint, int kid, int lyra = -1, int black = -1, string first = "")
    {
        if (Copy(b.Originals, RickScene, RickActor) is not { Entity: >= 0 } rick || !b.Store.SceneExists(CasinoScene))
        {
            b.Log.Add($"Rick left out: no Rick in his office (scene {RickScene}'s actor {RickActor}), or no Casino");
            return;
        }
        b.Edit(CasinoScene, "Rick by the Casino's bar: the Cup, then Casablanca (var_cube " + RickTalks + ")", model =>
        {
            rick.X = RickAt.X; rick.Y = RickAt.Y; rick.Z = RickAt.Z; rick.Beta = RickAt.Beta; rick.Move = 0;
            var full = lyra < 0 ? "" : $@"            if ({FeeFull} == var_game({QFee}))
            {{
                message({lyra});
                message({black});
            }}
            else
            {{
";
            return IslandBeatsA.Add(model, rick, Talker(when, first + full + $@"            add_var_cube({RickTalks}, 1);
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
            }}" + (lyra < 0 ? "" : $@"
            }}")));
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
        // (the Wizard and the cow as the telescope shows them -- its view on, ScopeVar -- not the cow's field walked into: the user, 2026-11-12,
        // "should fire when we watch the wizard with the cow through the telescope, not when we're close to the cow")
        $"achieve=telescope var {ScopeVar} 1",
        $"achieve=bowling scene {BowlingAlley.Scene}",
        // (the palace's dungeon, five layers under its rooms 159 and 163, its floor at 6,800: Twinsen in either, under 7,400)
        "achieve=dungeon below 159 7400",
        "achieve=dungeon below 163 7400",
    };
    private const int LyraClovers = 72, LyraDives = 73, LyraEntity = GridGirl.Entity, LyraBody = 40 + Temple.TempleMarshal.NoFlags, LyraReady = 660, LyraRaise = 661;
    // (the clovers: the game variable FLAG_CLOVER (COMMON.H), at most MAX_CLOVER_BOX)
    private const int Clovers = 251, MostClovers = 10;
    // (on the deck at the pool's south side, a few cells from where the dive lands -- (18278, 1536, 18432) -- facing the water)
    // (since 2026-11-19 lying on one of the loungers by the pool, on her back -- the user, "Lyra by the pool in the Imperial hotel seems to
    // have a massive shadow and this is the closest Twinsen can get to her. Let's reposition her to one of the loungers by the pool
    // instead": the game's own second deck chair (Citadel/CitadelProps.Pool: DeckChairs -- the first is Johnny Rocket's), her head to its
    // backrest, -z, her feet to +z, facing +z (TempleMarshal.Lie: the turned bone's pivot 468 up, her back 400 over her origin, her head
    // 1,054 along from it and her feet 468), the chair's middle 293 head-ward of her origin; no shadow -- hers was the massive one)
    private const int LyraLyingBack = 400, LyraLyingMiddle = 293;
    private static readonly (int X, int Y, int Z, int Beta) LyraAt = (Citadel.CitadelProps.DeckChairs[1].X,
        Citadel.CitadelProps.PoolDeck + Citadel.CitadelProps.DeckChairSeat - LyraLyingBack, Citadel.CitadelProps.DeckChairs[1].Z + LyraLyingMiddle, 0);
    private const uint LyraNoShadow = 1u << 12;

    // (`talk`: Lyra's at the start line, the same by the pool: Twinsen who met her there first couldn't reach her to talk -- the user,
    // 2026-11-09/11: "the only thing to work on for Otringal is being able to talk to Lyra so we don't have a softlock": the steward sells
    // no Travel Token till Lyra has told of the fee, Q_FEE 1. From the pool's edge: LyraPoolReach)
    private const int LyraPoolReach = 3200;
    // A speaker's bubble put at a place of its own, from the actor's (BUBBLES.TXT in the game folder, a line a speaker: scene, actor, the
    // place's x, y and z from the actor's -- RACEMOD.CPP RaceMod_BulleAt): the line for that scene's actor written again, the others kept
    internal static void SpeechBubbleAt(string gameDirectory, int scene, int actor, (int X, int Y, int Z) at)
    {
        var path = Path.Combine(gameDirectory, "BUBBLES.TXT");
        var lines = File.Exists(path) ? File.ReadAllLines(path).Where(l => !l.StartsWith($"{scene} {actor} ", StringComparison.Ordinal)).ToList() : new List<string>();
        lines.Add($"{scene} {actor} {at.X} {at.Y} {at.Z}");
        File.WriteAllLines(path, lines);
    }

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
            // (not shoved by what she lies on: CHECK_OBJ_COL off -- with it the engine pushed her out of the lounger's box and along the deck to
            // the ladder; and no brick collision nor falling -- on the game's deck chair, its bricks, the engine lifted her onto their top and
            // stood her up)
            lyra.Body = LyraBody; lyra.Anim = Temple.TempleMarshal.Lie; lyra.Beta = LyraAt.Beta; lyra.Flags = LyraNoShadow;
            // (walked past, not into: her box is the flag marshal's rig's, standing, its flags out -- about 3,400 round her -- and lying on the
            // lounger it walled off the deck all about the pool and the stairs up, invisible: the user, 2026-11-21, "There seems to be an
            // invisible wall here in the imperial hotel blocking Twinsen right in front of him" (three places; Twinsen's obj_col her, actor 20).
            // The lounger's own bricks stop him at it. Her colour and armour were in the comment above, never set: her armour 1)
            lyra.CoulObj = 9; lyra.Armor = RaceTrackStory.WalkOverArmour;
            var index = model.Actors.Count;
            // (her speech bubble at her head as she lies -- the game puts it over her feet at her box's top, standing: well above and
            // beside her, the user, 2026-11-24: "Lyra's speech bubble is a little far from her head")
            SpeechBubbleAt(b.GameDirectory, HotelScene, index, Temple.TempleMarshal.LieHeadAt);
            // (her talking animation her lying one: the engine plays a speaker's set_anim_dial animation while she speaks -- by default the
            // talking one, standing, and with none of her own there the standing one -- then puts her back: talked to, she stood up through
            // the lounger and slid back, the user, 2026-11-21: "causes her to move and rubber band back ... causing her to stand through the
            // chair", MESSAGE.CPP InitWhoSpeak)
            var add = IslandBeatsA.Add(model, lyra, $@"void comportement_0()
{{
    set_anim_dial({Temple.TempleMarshal.Lie});
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
anim({Temple.TempleMarshal.Lie});
stop();

label(1);
anim({LyraRaise});
wait_anim();
wait_nb_second(3);
anim({Temple.TempleMarshal.Lie});

label(2);
stop();
");
            };
        });
    }
}
