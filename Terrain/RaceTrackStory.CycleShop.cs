using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The cycle shop's people and its sale (the room itself: Citadel/CitadelCycleShop, scene 398 behind the street's door that led into the
// baggage claim -- the user, 2026-11-20: "Let's change 'Citadel Island, Baggage claim building, 1st scene (room #7)' into a cycle sale and
// repair shop and have the shop owner mention the racers in the dome of the slate. He should also offer bikes for sale that Twinsen will
// never be able to afford 1000, 2000, 5000 - Twinsen's money stops at 999"; 2026-11-21: "Let's just design an awesome cycle shop and map it
// to the same entrance zone"):
//   - the owner behind the counter: Action by him, his welcome -- the racers at the Dome of the Slate -- and his three bikes, each more than
//     Twinsen can ever carry (kashes stop at 999), or just looking;
//   - the Hoverboard left behind in the corner (the user: "if Twinsen uses action on it, have the shop keeper comment that one of his
//     customers left it there ages ago as he nolonger wanted it, and he hadn't had the heart to throw it away, he then offers to sell it to
//     Twinsen for 50 kashes"): Action by it, the owner's story of it, then his price -- a short question, yes or no; bought, the 50 kashes
//     paid, the board his (item 12, the game's found_object: its holomap arrow off as the crate's was) and gone from the corner;
//   - the game's crate with the Proto-Pack in the baggage claim's hall (scene 5's actor 10, its found_object(12)) gone: the shop's board the
//     one way to it -- the Hoverboard routed into the story.
internal static partial class RaceTrackStory
{
    private const int BaggageHall = 5, ProtoCrate = 10, HoverboardItem = 12, BaggageArrow = 5, BoardPrice = 50;
    private const int ShopOwner = Citadel.CitadelCycleShop.OwnerActor, ShopBoard = Citadel.CitadelCycleShop.BoardActor;

    private static void CycleShop(StoryParts p)
    {
        if (!p.Store.SceneExists(Citadel.CitadelCycleShop.Scene)) { p.Log.Add($"the cycle shop's talk left out: no scene {Citadel.CitadelCycleShop.Scene}"); return; }
        int town = p.Say(ChapterLine.ShopTown), racing = p.Say(ChapterLine.ShopRacing), champion = p.Say(ChapterLine.ShopChampion), looking = p.Say(ChapterLine.ShopJustLooking);
        int buy = p.Say(ChapterLine.BoardBuy), no = p.Say(ChapterLine.BoardNo);
        p.Edit(Citadel.CitadelCycleShop.Scene, "the cycle shop: its owner's welcome and his bikes (more than Twinsen can have), and the Hoverboard left behind for 50 kashes", _ => scripts =>
        {
            scripts.SetText(ShopOwner, ScriptKind.Life, $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if (1800 > distance(0) && {Driving} != comportement_hero())
        {{
            message({p.Say(ChapterLine.ShopWelcome)});
            add_choice({town});
            add_choice({racing});
            add_choice({champion});
            add_choice({looking});
            ask_choice({p.Say(ChapterLine.ShopAsk)});
            if ({looking} == choice())
            {{
                message({p.Say(ChapterLine.ShopBrowse)});
            }}
            else
            {{
                message({p.Say(ChapterLine.ShopTooDear)});
            }}
        }}
    }}
}}
");
            scripts.SetText(ShopBoard, ScriptKind.Life, $@"void comportement_0()
{{
    if (1 == var_game({HoverboardItem}))
    {{
        suicide();
    }}
    else
    {{
        set_comportement(comportement_1);
    }}
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if (1100 > distance(0) && {Driving} != comportement_hero())
        {{
            message_obj({ShopOwner}, {p.Say(ChapterLine.BoardStory)});
            add_choice({buy});
            add_choice({no});
            ask_choice_obj({ShopOwner}, {p.Say(ChapterLine.BoardAsk)});
            if ({buy} == choice())
            {{
                if ({BoardPrice} <= nb_gold_pieces())
                {{
                    give_gold_pieces({BoardPrice});
                    message_obj({ShopOwner}, {p.Say(ChapterLine.BoardSold)});
                    clr_holo_pos({BaggageArrow});
                    found_object({HoverboardItem});
                    set_var_game({HoverboardItem}, 1);
                    suicide();
                }}
                else
                {{
                    message_obj({ShopOwner}, {p.Say(ChapterLine.BoardNoMoney)});
                }}
            }}
            else
            {{
                message_obj({ShopOwner}, {p.Say(ChapterLine.BoardLater)});
            }}
        }}
    }}
}}
");
        });
        // the game's crate with the Proto-Pack gone: the shop's board the one way to the Hoverboard
        p.Edit(BaggageHall, "the baggage claim: no crate with the Proto-Pack -- the Hoverboard is the cycle shop's", _ => scripts =>
            scripts.SetText(ProtoCrate, ScriptKind.Life, Loose(scripts.GetText(ProtoCrate, ScriptKind.Life), "void comportement_0()\n{", "void comportement_0()\n{\n    suicide();")));
    }
}
