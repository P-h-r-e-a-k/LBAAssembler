namespace LBAAssembler.Terrain;

// People in the cars' way (RaceTrackPlan.Guards; RACEMOD.CPP guard=): the Emperor's palace's own -- the user, 2026-10-13: "The retail game has
// enemies inside the palace, 3 pigs, Stan from Time commando, and lots of soldiers with swords, let's add some of these enemies inside as
// obstacles, perhaps the could chase and attack the cars, or throw random things at them". Each stands at its post on the tiles by a room's
// carpets, an actor of every scene it can be seen from (the engine draws a scene's actors only while it is the one about), its own scripts
// gone: the race-track mode moves and animates it (RACEMOD.CPP Guards) -- a soldier or Stan steps out at a car coming and strikes at it, a
// pighead throws things at it.
internal static class RaceTrackGuards
{
    public const int Soldier = 0, Pighead = 1, Stan = 2;
    public const double Seen = 40;          // (cells: an actor in every scene whose cube comes this near the post)

    // A kind's actor and animations, the game's own (the palace scenes' scripts, 151-166 and 80): its entity, its body (the entity's own
    // number for it), standing at its post, walking, knocked back, and its strikes (a pighead's: its throw).
    public sealed record Look(int Entity, int Body, int Idle, int Walk, int Hit, int[] Strikes);

    public static Look Of(int kind, int which) => kind switch
    {
        // (the Emperor's soldier, entity 184, his sword drawn -- body 2, BODY.HQR 261 -- in his fighting stance (434), walking with it (437);
        // his four strikes as his scripts choose among them at random within 900 of Twinsen)
        Soldier => new(184, 2, 434, 437, 6, new[] { 142, 277, 438, 439 }),
        // (the pigheads, entities 313-315: the red one with the hammer, the brown with the garden tool, the yellow with the broom -- at their
        // work as they are found (231, 585, 584), walking, swinging their tool -- here, throwing)
        Pighead => which switch
        {
            0 => new(313, 0, 231, 1, 6, new[] { 142 }),
            1 => new(314, 0, 585, 1, 6, new[] { 142 }),
            _ => new(315, 0, 584, 1, 6, new[] { 142 }),
        },
        // (Stan, come over from Time Commando, entity 316: standing, walking, his three blows)
        _ => new(316, 0, 0, 1, 6, new[] { 142, 277, 587 }),
    };

    public static string KindName(int kind, int count) => kind switch
    {
        Soldier => count == 1 ? "soldier" : "soldiers",
        Pighead => count == 1 ? "pighead" : "pigheads",
        _ => "Stan",
    };
}
