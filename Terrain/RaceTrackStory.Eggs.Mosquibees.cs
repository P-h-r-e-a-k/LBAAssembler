using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Mosquibees Island's easter eggs (RaceTrackStory.Eggs.cs; the user, 2026-11-04: "Let's make all those changes", of the proposal's
// picks), with chapter 10's story (RaceTrackStory.Mosquibees.cs):
//   - Bow to the Queen. The proposal: "Baldino said 'Queens like a bow.' Approach the Queen in her throne room (scene 104) in discreet
//     behaviour (Twinsen tiptoes, which is close enough): 'At last, a racer with manners.' She gives royal jelly: full life and magic."
//     A helper at the Queen's place (her own script is the game's, scene 104's entity 246, there while var 69 is 0 -- the story never
//     sets it): Twinsen within reach of her, discreet (comportement_hero() 3, C_DISCRET), on his feet and seen -- not the throne room's
//     arrival, where the game carries him in unseen (no_body: body_obj(0) -1) past her -- her two lines and full_point() (life and
//     magic full, GERELIFE.CPP LM_FULL_POINT). Once a visit (var_cube EggVisit): no game variable is spare for it (Eggs.cs keeps 155 as
//     the last spare), and the jelly is only a refill, so a second visit's bow earning it again harms nothing.
//   - The waggle dance. The proposal: "the honey-pot Mosquibee by the start does a figure-of-eight dance on the spot. Ask her: 'That's the
//     way to the honey. Turn left at the deck, and if you're brave, don't.' Zoe: 'Twinsen, she's showing us a short cut.'" Real bees'
//     waggle dance tells the hive the way to the flowers. Her track loops a dance on the spot: a turn round one way, a waggle, a turn
//     round the other way, a waggle -- her entity (219) has no walk to go round a real figure of eight with, only her hover (0), so the
//     eight is in her turning. Her first word is her own (HoneyPot); asked again in the visit (var_cube EggVisit), the hint and Zoe's
//     line. The hint only: no new road branch.
internal static partial class RaceTrackStory
{
    // the game's Queen (scene 104's entity), how near Twinsen bows to her, the discreet behaviour; a per-visit flag of the eggs' own
    // (var_cube: none of the island's scenes' scripts use it -- 0-3, 30 and 45 are theirs)
    private const int QueenEntity = 246, BowReach = 1600, Discreet = 3, EggVisit = 46, QueenAway = 69;

    // The honey-pot Mosquibee added to her scene (GroupB.Add) with her talk and her dance; `when`: chapter 10's condition
    private static Action<SceneScripts> HoneyDance(SceneModel model, SceneActorModel honey, string when, Func<MosquibeesLine, int> say)
    {
        // (her turns at her own speed of rotation; the game's actors standing still may have none)
        if (honey.SRot <= 0) honey.SRot = 600;
        var talk = Talker(when, $@"            if (0 == var_cube({EggVisit}))
            {{
                message({say(MosquibeesLine.HoneyPot)});
                set_var_cube({EggVisit}, 1);
            }}
            else
            {{
                message({say(MosquibeesLine.HoneyWaggle)});
                message_zoe({say(MosquibeesLine.ZoeShortCut)});
            }}");
        // (an actor's track runs only once its life has set it going)
        var add = GroupB.Add(model, honey, Loose(talk, "set_comportement(comportement_1);", "set_track(label_0);\n        set_comportement(comportement_1);"));
        var index = model.Actors.Count - 1;
        return scripts =>
        {
            add(scripts);
            // (round to the right a quarter at a time, a waggle -- a quick shake either side -- round to the left, a waggle, and again)
            scripts.SetText(index, ScriptKind.Track, @"label(0);
anim(0);
angle(1024);
angle(2048);
angle(3072);
angle(0);
angle(160);
angle(3936);
angle(160);
angle(3936);
angle(0);
angle(3072);
angle(2048);
angle(1024);
angle(0);
angle(3936);
angle(160);
angle(3936);
angle(160);
angle(0);
wait_nb_dizieme(5);
goto(label_0);
");
        };
    }

    // Bow to the Queen, in her throne room (scene 104). Adds to the log.
    private static void QueenBow(SceneStore store, List<string> log, Func<MosquibeesLine, int> say) =>
        GroupB.EditScene(store, ThroneScene, log, $"the Queen's bow: Twinsen discreet near her, her royal jelly (full life and magic), once a visit (var_cube {EggVisit})", model =>
        {
            var queen = model.Actors.FindIndex(a => a.Entity == QueenEntity);
            if (queen < 1) throw new NotTheGames($"no Queen (entity {QueenEntity})");
            var at = model.Actors[queen];
            return GroupB.Add(model, RaceTrackScenes.InvisibleHelper(at.X, at.Y, at.Z, at.CoulObj), $@"void comportement_0()
{{
    if ({MosquibeesChapter} <= chapter() && 0 == var_game({QueenAway}))
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
    if (0 == var_cube({EggVisit}) && {Discreet} == comportement_hero() && {BowReach} > distance(0) && -1 < body_obj(0))
    {{
        set_var_cube({EggVisit}, 1);
        message_obj({queen}, {say(MosquibeesLine.QueenManners)});
        message_obj({queen}, {say(MosquibeesLine.QueenJelly)});
        full_point();
    }}
}}
");
        });
}
