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

    private static void VelodromeTalk(StoryParts p)
    {
        if (!p.Store.SceneExists(DomeVelodrome.Scene)) return;
        if (DomeVelodrome.Talkers.Count == 0) { p.Log.Add("the velodrome's people: none listed (the room built in another run) -- no lines"); return; }
        p.Edit(DomeVelodrome.Scene, $"the velodrome: a word from each of its people ({DomeVelodrome.Talkers.Count})", _ => scripts =>
        {
            foreach (var (actor, who) in DomeVelodrome.Talkers)
                if (VelodromeSays.TryGetValue(who, out var line))
                    scripts.SetText(actor, ScriptKind.Life, Talker("0 <= chapter()", $"            message({CitadelLaterId(p.Say, line)});",
                        who.StartsWith("Rider", StringComparison.Ordinal) ? NearRider : NearPerson));
        });
    }
}
