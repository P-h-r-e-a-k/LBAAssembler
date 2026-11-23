using LBAAssembler.LbaScript;

namespace LBAAssembler.Terrain;

// Otringal's two days (the user, 2026-11-18/19: "Lyra charges Twinsen for a 'Travel token' ... Let's tweak the story line so the race is
// already full, as Twinsen took too long to arrive, because they stopped off on the Emerald moon, but if he finds and speaks to Lyra she
// takes a bribe to get him into the race"; "when Twinsen arrives at the start line and tries to speak to the race marshall she tells him that
// he's too late and the race is full, and it's due to run 'Tomorrow'. This could trigger Twinsen to ask people ... ultimately leading him to
// find somebody"; and, of the ideas: "let's drop a few extra details about the black car, let's also have Twinsen have to bribe his way into
// the race. Qualifying is day one, cars are held and inspected in a secure garage over night, when Twinsen gets his car back on race day
// it's missing a part that he has to find"):
//   - the race marshal at the start line: too late, the grid's full, the race is tomorrow -- the last place a black car's, a mask and no
//     name; the Esmers' 10 zlitos (Q_FEE 1). Asked again: ask around town.
//   - Rick at the Casino's bar: Lyra runs the Cup's paperwork, her day off by the Imperial Hotel's pool, and she likes zlitos; the black
//     car's driver at his machines last night, the mask never off (RaceTrackStory.Eggs.Otringal: RickAtTheCasino).
//   - Lyra on her sun lounger: a place found for 30 zlitos (the Casino's slot machines for them) -- on the list (Q_FEE 2): qualifying is
//     today, the cars in the stewards' garage overnight; the black car paid in old Sup coins, its driver's face seen by nobody.
//   - Qualifying (the race file's gate=217 2): the lap, then no race -- qualify_stop=: the day's variable 1, the car stopped and Twinsen out;
//     the marshal: a good lap, the car in the garage tonight -- the night passes -- race day (2): the car back, its ignition coil not.
//   - The race held while the coil is gone (gate_not=); the Lower City's gazogem seller saw someone in a mask leave the garage in the night
//     with something under his arm, down to the west pier, into a crate; the crate there: the coil (3).
//   - The race (no qualifying lap: no_qualify=), won as before (win=217 3): the stewards, the Fragment of the Sups, the priest; Johnny Rocket.
internal static partial class RaceTrackStory
{
    // the west pier's yard (island cells), where the coil was thrown into a crate
    private static readonly (double X, double Z) CoilPier = (413, 548);

    // The race marshal at the start line (MarshalA)
    private static string MarshalTalk(Func<OtringalLine, int> say) => $@"            if ({FeePaid} == var_game({QFee}) && {DayQualified} == var_game({OtringalDay}))
            {{
                message({say(OtringalLine.MarshalNight)});
            }}
            else if (0 == var_game({QFee}))
            {{
                message({say(OtringalLine.MarshalFull)});
                message({say(OtringalLine.EsmerMoney)});
                add_gold_pieces({StarterZlitos});
                set_var_game({QFee}, {FeeFull});
            }}
            else if ({FeeFull} == var_game({QFee}))
            {{
                message({say(OtringalLine.MarshalStillFull)});
            }}
            else if ({FeePaid} == var_game({QFee}) && {DayRaceDay} == var_game({OtringalDay}))
            {{
                message_obj(0, {say(OtringalLine.TwinsenAskCoil)});
                message({say(OtringalLine.MarshalNoCoil)});
            }}
            else if ({FeePaid} == var_game({QFee}) && {DayCoil} == var_game({OtringalDay}))
            {{
                message({say(OtringalLine.MarshalReady)});
            }}
            else if ({FeePaid} == var_game({QFee}))
            {{
                message({say(OtringalLine.MarshalSetTime)});
            }}";

    // Lyra on her sun lounger by the hotel's pool (LyraPoolScore)
    private static string LyraTalk(Func<OtringalLine, int> say) => $@"            if ({FeeFull} > var_game({QFee}))
            {{
                message({say(OtringalLine.LyraOffDuty)});
            }}
            else if ({FeeFull} == var_game({QFee}))
            {{
                message({say(OtringalLine.LyraBribe)});
                if ({Fee} > nb_gold_pieces())
                {{
                    message({say(OtringalLine.LyraShort)});
                }}
                else
                {{
                    add_choice({say(OtringalLine.Yes)});
                    add_choice({say(OtringalLine.No)});
                    ask_choice({say(OtringalLine.LyraAsk)});
                    if ({say(OtringalLine.Yes)} == choice())
                    {{
                        give_gold_pieces({Fee});
                        message({say(OtringalLine.LyraIn)});
                        message({say(OtringalLine.LyraBlackCar)});
                        set_var_game({QFee}, {FeePaid});
                    }}
                }}
            }}
            else
            {{
                message({say(OtringalLine.LyraBlackCar)});
            }}";

    // The witness and the coil
    private static void OtringalDays(IslandBeatsA b, RaceTrackReport report, Func<OtringalLine, int> say, string when)
    {
        // the gazogem seller: on race day, what he saw in the night (his own line, the black car's fellow, the rest of the time)
        // (since 2026-11-22 the coil's fetch quest -- the user: "Maybe yes, maybe no, maybe I have one ... IF you do a small favour for me" -- a
        // love letter to the singer at Rick's bar and her answer back, then the coil for 10 zlitos and the masked man in black who sold it him)
        // (and he stays at his stall in chapter 5: the game takes him away once the dissidents' plot is far on and Twinsen has the gazogem can
        // -- the Island of the Francos gives it him before Otringal -- and the coil's quest is his)
        b.Edit(LowerCity, "the gazogem seller on race day: the coil for a love letter carried to the singer at Rick's bar, and 10 zlitos", _ => scripts =>
            scripts.SetText(GazogemSeller, ScriptKind.Life, Loose(Loose(scripts.GetText(GazogemSeller, ScriptKind.Life),
                "if (7 < chapter() || (0 < var_game(123)", "if (7 < chapter() || (5 != chapter() && 0 < var_game(123)"),
                "message(245);\nset_var_cube(0, 0);",
                $@"if ({DayRaceDay} == var_game({OtringalDay}) && {CoilLetter} > var_game({QCoil}))
{{
message_obj(0, {say(OtringalLine.TwinsenAskCoil)});
message({say(OtringalLine.SellerMaybe)});
message_obj(0, {say(OtringalLine.TwinsenWhatFavour)});
message({say(OtringalLine.SellerLetter)});
set_var_game({QCoil}, {CoilLetter});
}}
else if ({DayRaceDay} == var_game({OtringalDay}) && {CoilLetter} == var_game({QCoil}))
{{
message({say(OtringalLine.SellerWaiting)});
}}
else if ({DayRaceDay} == var_game({OtringalDay}) && {CoilAnswer} == var_game({QCoil}))
{{
message({say(OtringalLine.SellerReplyRead)});
add_choice({say(OtringalLine.Yes)});
add_choice({say(OtringalLine.No)});
ask_choice({say(OtringalLine.SellerAsk)});
if ({say(OtringalLine.Yes)} == choice())
{{
if ({CoilPrice} > nb_gold_pieces())
{{
message({say(OtringalLine.SellerShort)});
}}
else
{{
give_gold_pieces({CoilPrice});
message({say(OtringalLine.SellerSold)});
set_var_game({QCoil}, {CoilBought});
set_var_game({OtringalDay}, {DayCoil});
bulle(0);
message({say(OtringalLine.CoilFound)});
bulle(1);
}}
}}
}}
else if ({CoilBought} == var_game({QCoil}))
{{
message({say(OtringalLine.SellerAfter)});
}}
else
{{
message(245);
}}
set_var_cube(0, 0);")));
        // (the crate on the west pier with the coil in it: gone since 2026-11-22 -- the seller has the coil now, RaceTrackStory.OtringalNight)
    }

    // The race file's: the race full till Lyra's been paid; qualifying the first day -- then no race -- held overnight and while the coil is
    // gone; no qualifying lap on race day
    private static string OtringalRaceKeys =>
        $"gate={QFee} {FeePaid} The marshal: the grid's full -- the race runs tomorrow.\n" +
        $"gate_not={OtringalDay} {DayQualified} The cars are in the stewards' garage till the morning.\n" +
        $"gate_not={OtringalDay} {DayRaceDay} The marshal: no ignition coil, no race!\n" +
        $"qualify_stop={OtringalDay} 0 {DayQualified}\n" +
        // (the others out of sight while he qualifies: they qualified already, their cars kept in the palace overnight -- the user, 2026-11-23,
        // "For qualifying we don't need the other cars on the pitlane"; on race day they come out onto the grid)
        "pit_hidden=1\n" +
        // (the night: the car in the palace's keeping; race day: its coil gone, it won't start -- RaceTrackStory.OtringalNight)
        $"car_held={OtringalDay} {DayQualified} {PalaceRoom}\n" +
        $"car_dead={OtringalDay} {DayRaceDay} The engine won't start -- the ignition coil is missing!\n" +
        $"no_qualify={OtringalDay} {DayRaceDay}\n" +
        $"win={QFee} {FeeWon}\n";
}
