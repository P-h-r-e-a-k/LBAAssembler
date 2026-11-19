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
    private static string MarshalTalk(Func<OtringalLine, int> say) => $@"            if (0 == var_game({QFee}))
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
        b.Edit(LowerCity, "the gazogem seller on race day: someone in a mask out of the stewards' garage, to the west pier", _ => scripts =>
            scripts.SetText(GazogemSeller, ScriptKind.Life, Loose(scripts.GetText(GazogemSeller, ScriptKind.Life),
                "message(245);\nset_var_cube(0, 0);",
                $"if ({DayRaceDay} == var_game({OtringalDay}))\n{{\nmessage({say(OtringalLine.SellerMask)});\n}}\nelse\n{{\nmessage(245);\n}}\nset_var_cube(0, 0);")));
        // the crate on the west pier, the coil in it
        if (OffRoadA(report, CoilPier.X, CoilPier.Z, 1.0) is { } spot && b.SceneAt(spot.X, spot.Z) is var scene and >= 0)
            b.Edit(scene, $"the crate on the west pier at cell ({spot.X:0.#}, {spot.Z:0.#}): the car's ignition coil, on race day", model =>
            {
                var crate = Citadel.CitadelProps.Actor(Citadel.CitadelProps.Crate, 0, 0, 0);
                Place(crate, model, (spot.X, spot.Z, spot.Y, 0));
                return IslandBeatsA.Add(model, crate, Talker(when, $@"            if ({DayRaceDay} == var_game({OtringalDay}))
            {{
                message({say(OtringalLine.CoilFound)});
                set_var_game({OtringalDay}, {DayCoil});
            }}"));
            });
        else b.Log.Add("the coil's crate left out: no room off the road by the west pier");
    }

    // The race file's: the race full till Lyra's been paid; qualifying the first day -- then no race -- held overnight and while the coil is
    // gone; no qualifying lap on race day
    private static string OtringalRaceKeys =>
        $"gate={QFee} {FeePaid} The marshal: the grid's full -- the race runs tomorrow.\n" +
        $"gate_not={OtringalDay} {DayQualified} The cars are in the stewards' garage till the morning.\n" +
        $"gate_not={OtringalDay} {DayRaceDay} The marshal: no ignition coil, no race!\n" +
        $"qualify_stop={OtringalDay} 0 {DayQualified}\n" +
        $"no_qualify={OtringalDay} {DayRaceDay}\n" +
        $"win={QFee} {FeeWon}\n";
}
