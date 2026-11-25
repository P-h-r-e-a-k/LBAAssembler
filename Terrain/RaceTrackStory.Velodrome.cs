using LBAAssembler.LbaScript;

namespace LBAAssembler.Terrain;

// The Dome of the Slate's velodrome's people, each with a word for Twinsen (DomeVelodrome.People puts them in the room and lists them,
// Talkers -- the user, 2026-11-24: "We need to add some NPCs to the velodrome to bring it to life, perhaps some people riding laps, and
// their support team, and generally just people around"; every character answers Action): the riders as they pass, near; the others
// when he is by them. Their lines are Citadel Island's (CitadelLater.Velo...).
internal static partial class RaceTrackStory
{
    // how near Twinsen is for Action to have a word from them: a rider going by, the others
    private const int NearRider = 1200, NearPerson = 1500;

    private static readonly Dictionary<string, CitadelLater> VelodromeSays = new()
    {
        ["RiderComets"] = CitadelLater.VeloRiderComets, ["RiderComets2"] = CitadelLater.VeloRiderComets2,
        ["RiderExpress"] = CitadelLater.VeloRiderExpress, ["RiderExpress2"] = CitadelLater.VeloRiderExpress2,
        ["CometsCoach"] = CitadelLater.VeloCometsCoach, ["ExpressCoach"] = CitadelLater.VeloExpressCoach,
        ["Mechanic"] = CitadelLater.VeloMechanic, ["Soigneur"] = CitadelLater.VeloSoigneur,
        ["FanFlag"] = CitadelLater.VeloFanFlag, ["FanGrobo"] = CitadelLater.VeloFanGrobo, ["FanGirl"] = CitadelLater.VeloFanGirl,
        ["FanChild"] = CitadelLater.VeloFanChild, ["FanTeacher"] = CitadelLater.VeloFanTeacher, ["FanSphero"] = CitadelLater.VeloFanSphero,
    };

    // The Comets' coach (the user, 2026-11-24: "let's do some work to allow Twinsen to cycle as this would fit in with the velodrome"):
    // his word, then his spare bike offered -- yes, and Twinsen is on it (TwinsenBike.BikeVar 1: RACEMOD.CPP bike= puts him on the bike,
    // C_VELO); asked again while he rides it, he takes it back (0). Out of the velodrome the bike stays with the team (the engine's).
    private static string CoachLendsBike(StoryParts p)
    {
        int Line(CitadelLater line) => CitadelLaterId(p.Say, line);
        int yes = Line(CitadelLater.VeloRideYes), no = Line(CitadelLater.VeloRideNo), back = Line(CitadelLater.VeloGiveBack), more = Line(CitadelLater.VeloOneMoreLap);
        var bike = Citadel.TwinsenBike.BikeVar;
        return $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if ({NearPerson} > distance(0) && {Driving} != comportement_hero())
        {{
            if (1 == var_game({bike}))
            {{
                add_choice({back});
                add_choice({more});
                ask_choice({Line(CitadelLater.VeloCoachBackAsk)});
                if ({back} == choice())
                {{
                    set_var_game({bike}, 0);
                    message({Line(CitadelLater.VeloCoachThanks)});
                }}
            }}
            else
            {{
                message({Line(CitadelLater.VeloCometsCoach)});
                add_choice({yes});
                add_choice({no});
                ask_choice({Line(CitadelLater.VeloCoachAsk)});
                if ({yes} == choice())
                {{
                    message({Line(CitadelLater.VeloCoachLent)});
                    set_var_game({bike}, 1);
                }}
            }}
        }}
    }}
}}
";
    }

    private static void VelodromeTalk(StoryParts p)
    {
        if (!p.Store.SceneExists(DomeVelodrome.Scene)) return;
        if (DomeVelodrome.Talkers.Count == 0) { p.Log.Add("the velodrome's people: none listed (the room built in another run) -- no lines"); return; }
        p.Edit(DomeVelodrome.Scene, $"the velodrome: a word from each of its people ({DomeVelodrome.Talkers.Count})", _ => scripts =>
        {
            foreach (var (actor, who) in DomeVelodrome.Talkers)
                if (who == "CometsCoach") scripts.SetText(actor, ScriptKind.Life, CoachLendsBike(p));
                else if (VelodromeSays.TryGetValue(who, out var line))
                    scripts.SetText(actor, ScriptKind.Life, Talker("0 <= chapter()", $"            message({CitadelLaterId(p.Say, line)});",
                        who.StartsWith("Rider", StringComparison.Ordinal) ? NearRider : NearPerson));
        });
    }
}
