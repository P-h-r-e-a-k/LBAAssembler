using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;
using CycleShopRoom = LBAAssembler.Terrain.Citadel.CitadelCycleShop;

namespace LBAAssembler.Terrain;

// The cycle shop's people and its sale (the room itself: Citadel/CitadelCycleShop, scene 398 behind the street's door that led into the
// baggage claim -- the user, 2026-11-20: "Let's change 'Citadel Island, Baggage claim building, 1st scene (room #7)' into a cycle sale and
// repair shop and have the shop owner mention the racers in the dome of the slate. He should also offer bikes for sale that Twinsen will
// never be able to afford 1000, 2000, 5000 - Twinsen's money stops at 999"; 2026-11-21: "Let's just design an awesome cycle shop and map it
// to the same entrance zone"; that evening: "Instead of speaking to the shop owner to get the price for a bike, Twinsen should be able to walk
// up to a bike, and either get a prompt to buy it if it's for sale, or if it's for decoration get told information about it. Let's call the
// place 'Citadel Cycles'"):
//   - Action in the shop, the hero's own script (the game's distance_message: what he faces, near): before a bike for sale, its price tag
//     read (in quotes, no bubble: read, not said) and his question, buy it or not today -- bought, more kashes than he will ever have (they
//     stopped at 999; since 2026-11-22 they stop at 9,999, and came in with 1,000 or more he finds the tags ten times higher); before the penny-farthing, the tandem, the racer's bike on the repair stand, the fitting bike, the jerseys or the
//     wheels, its plaque read; before the owner, his welcome -- the racers at the Dome of the Slate, the prices on the bikes;
//   - (the Hoverboard left behind in the corner, sold for 50 kashes, 2026-11-21 to 11-27: no more -- the board is Baldino's on the Desert,
//     for a hair dryer's rheostat, RaceTrackStory.HairDryer; the user, 2026-11-27: "Currently on Citadel Island everything is open to
//     Twinsen straight away including the hoverboard, when it's not needed until Desert Island", "Let's move the hoverboard to Baldino")
//     -- its prop gone from the corner;
//   - the owner is the baggage claim's clerk (the user, 2026-11-21: "let's have it staffed by the same NPC who staffed the storage place
//     that it's replacing"): gone from the baggage claim's hall (scene 5's actor 9, unseen there now, his one rule that matters kept: the
//     guard letting Twinsen through to his own parcel, game variable 50, as he leaves by the street door);
//   - the game's crate with the Proto-Pack in the baggage claim's hall (scene 5's actor 10, its found_object(12)) gone: the shop's board the
//     one way to it -- the Hoverboard routed into the story.
internal static partial class RaceTrackStory
{
    private const int BaggageHall = 5, ProtoCrate = 10, HoverboardItem = 12, BaggageArrow = 5, BoardPrice = 50;
    private const int ShopOwner = CycleShopRoom.OwnerActor, ShopBoard = CycleShopRoom.BoardActor;
    // how near Twinsen is to each thing's place (its middle) for Action before it to tell of it -- the long ones and those on a plinth or a
    // wall further; the owner across his counter
    private const int NearBoard = 1100, NearBike = 1350, NearLong = 1600, NearOwner = 2400;
    // Twinsen came in with RichFrom kashes or more (looked at once a visit, RichLooked): the bikes' tags ten times the price this visit
    // (RichPrices) -- the user, 2026-11-22, kashes up to 9,999 now: "if Twinsen enters and he has 1000 kashes or more, then let's make the
    // bikes all 10 times more expensive, so the cheapest bike becomes 10,000". The scene's own variables, cleared each visit.
    private const int RichFrom = 1000, RichLooked = 10, RichPrices = 11;

    private static void CycleShop(StoryParts p)
    {
        if (!p.Store.SceneExists(CycleShopRoom.Scene)) { p.Log.Add($"the cycle shop's talk left out: no scene {CycleShopRoom.Scene}"); return; }
        int Later(CitadelLater line) => CitadelLaterId(p.Say, line);
        int buyIt = Later(CitadelLater.ShopBuyIt), notToday = Later(CitadelLater.ShopNotToday);
        // a bike for sale: its tag read -- ten times the price if he came in with 1,000 kashes or more -- his question, his sigh if he would
        // buy it; a plaque: read
        string Sale(CitadelLater tag, CitadelLater rich) => $@"bulle(0);
                if (1 == var_cube({RichPrices}))
                {{
                    message({Later(rich)});
                }}
                else
                {{
                    message({Later(tag)});
                }}
                bulle(1);
                add_choice({buyIt});
                add_choice({notToday});
                ask_choice({Later(CitadelLater.TwinsenBuyAsk)});
                if ({buyIt} == choice())
                {{
                    message({Later(CitadelLater.TwinsenNeverAfford)});
                }}";
        string Plaque(CitadelLater plaque) => $@"bulle(0);
                message({Later(plaque)});
                bulle(1);";
        var things = new (int Actor, int Near, string Says)[]
        {
            (CycleShopRoom.TownActor, NearBike, Sale(CitadelLater.ShopTagTown, CitadelLater.ShopTagTownRich)),
            (CycleShopRoom.RacingActor, NearBike, Sale(CitadelLater.ShopTagRacing, CitadelLater.ShopTagRacingRich)),
            (CycleShopRoom.ChampionActor, NearLong, Sale(CitadelLater.ShopTagChampion, CitadelLater.ShopTagChampionRich)),
            (CycleShopRoom.PennyActor, NearLong, Plaque(CitadelLater.ShopPennyFarthing)),
            (CycleShopRoom.TandemActor, NearLong, Plaque(CitadelLater.ShopTandem)),
            (CycleShopRoom.RepairActor, NearBike, Plaque(CitadelLater.ShopRepair)),
            (CycleShopRoom.FitActor, NearBike, Plaque(CitadelLater.ShopFitting)),
            (CycleShopRoom.JerseysActor, NearLong, Plaque(CitadelLater.ShopJerseys)),
            (CycleShopRoom.WheelsActor, NearLong, Plaque(CitadelLater.ShopWheels)),
        };
        var told = string.Concat(things.Select(t => $@"
            else if ({t.Near} > distance_message({t.Actor}))
            {{
                {t.Says}
            }}"));
        p.Edit(CycleShopRoom.Scene, "Citadel Cycles: Action before a bike, its price tag or its plaque; before the owner, his welcome; the Hoverboard left behind for 50 kashes", _ => scripts =>
        {
            scripts.SetText(0, ScriptKind.Life, $@"void comportement_0()
{{
    if (0 == var_cube({RichLooked}))
    {{
        set_var_cube({RichLooked}, 1);
        if ({RichFrom} <= nb_gold_pieces())
        {{
            set_var_cube({RichPrices}, 1);
        }}
    }}
    swif (1 == action())
    {{
        if ({Driving} != comportement_hero())
        {{
            {told.TrimStart()["else ".Length..]}
            else if ({NearOwner} > distance_message({ShopOwner}))
            {{
                message_obj({ShopOwner}, {p.Say(ChapterLine.ShopWelcome)});
            }}
        }}
    }}
}}
");
            // (the board not in the shop: Baldino's on the Desert, for a hair dryer's rheostat -- RaceTrackStory.HairDryer)
            scripts.SetText(ShopBoard, ScriptKind.Life, "void comportement_0()\n{\n    suicide();\n}\n");
        });
        // the baggage claim's hall: no crate with the Proto-Pack; its clerk gone to run the shop -- unseen, walked through, silent, his rule for
        // game variable 50 kept (the guard lets Twinsen through to his own parcel)
        p.Edit(BaggageHall, "the baggage claim: no crate with the Proto-Pack -- the Hoverboard is the cycle shop's; its clerk runs the shop now", _ => scripts =>
        {
            scripts.SetText(ProtoCrate, ScriptKind.Life, Loose(scripts.GetText(ProtoCrate, ScriptKind.Life), "void comportement_0()\n{", "void comportement_0()\n{\n    suicide();"));
            var clerk = scripts.GetText(CycleShopRoom.ClerkActor, ScriptKind.Life);
            if (!clerk.Contains("set_var_game(50, 2);")) throw new NotTheGames($"scene {BaggageHall}'s actor {CycleShopRoom.ClerkActor} isn't the game's clerk");
            scripts.SetText(CycleShopRoom.ClerkActor, ScriptKind.Life, @"void comportement_0()
{
    invisible(1);
    obj_col(0);
    set_comportement(comportement_1);
}

void comportement_1()
{
    if (6 == zone_obj(0) && 1 == var_cube(5))
    {
        set_var_game(50, 2);
    }
}
");
        });
    }
}
