using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;
using LBAAssembler.Terrain.Citadel;

namespace LBAAssembler.Terrain;

// The Tralu's caves on Citadel Island (scenes 2, 19 and 20; the cave's mouth in the cliff below the Weather Wizard's tent, scene 45).
//   - The mouth (the user, 2026-11-06: "The entrance into the cave on the way back from visiting the Tralu's cave doesn't seem to allow
//     Twinsen back in, this should be fixed"). Scene 45's door into the cave (x 29696-30208, z 11776-12800) lies behind the cliff's face:
//     walking west to it, Twinsen is stopped at x 30209, one unit short of the zone, everywhere but in the opening's middle (headless,
//     2026-11-06: in from x 30500 at z 11800 and 12288; stopped at x 30209 at z 11300, 11550, 12800, 13050, 13300). Out of the cave he
//     lands at x 30720 and the z he left scene 2's door at (z 11776-12800 for 25088-26112): come out near the corridor's side, he walks
//     back into the cliff beside the opening. And the zone's way in landed him in scene 2 at 31744 plus his x in the zone -- up to 32256,
//     the cave's own door back out (x 32256 on): in and straight back out again. Now the zone reaches 256 out in front of the face (to x
//     30464: still 256 short of where the cave's door out lands him, x 30720 on), the whole width of it, and lands him 512 further in
//     (31232 plus his x in the zone: 31232-32000, 256 short of the door out at the most).
//   - The cave's gate (the user, 2026-11-06: "In scene 2 there's a soft lock for the top area, where Twinsen in the original game fights and
//     kills the Tralu to get a key, with the tralu moves there's no longer a way to get a key. Perhaps once Twinsen finds the bar area this
//     gate could be unlocked as a quicker way for him to get back to the bar. We'd need to either put a ladder or some stairs there"; and
//     2026-11-07: "the entrance I've put a pink circle around normally has a gate here until Twinsen unlocks it with the key ... let's keep
//     the gate there until Twinsen has discovered the bar area"). In the game the way between the cave's first hall and the Tralu's hall
//     is a barred door (actor 8) that the Tralu's key opens: var 60 to 3 -- and the door's own life takes it away for good once var 60 is
//     past 2. The Tralu's hall is reached from above, down off the ledge from scene 19, with no way back up. Now:
//       - the stairs up to the ledge (TraluBarRoom.InstallStairs): from the Tralu's hall back up to the ledge and the bar, always;
//       - the gate stays where the game has it, shut, until Twinsen first walks into the bar (scene 19): an unseen helper there sets
//         var 60 to 3 as the scene starts, and the door is gone on every later visit to the caves. (The game's own 1 -- the ledge's zone
//         reached, set only while var 60 is 0 -- comes before or after; either way the bar makes it 3.)
//     So the bar's quick way back, once found: the cave's mouth, its first hall, the gate's doorway, the Tralu's hall, the stairs, the
//     ledge -- and scene 19's corridor (its barred doors gone with the bar: RaceTrackStory.TraluTavern).
//     (2026-11-07 an earlier build moved the gate itself onto the ledge as a "back gate" with a hidden key under the sign of Sendell; that
//     left the game's doorway open from the start. Undone: the gate is the game's again.)
internal static partial class RaceTrackStory
{
    // the cave's game variable (60: 1 the ledge reached, the game's own; 2, the Tralu dead, never comes in the story; 3 the gate open)
    // and its barred door (actor 8)
    private const int CaveVar = 60, CaveGateOpen = 3, CaveDoor = 8;

    private static void TraluBarOpensGate(StoryParts p) =>
        p.Edit(BarHall, "the bar found opens the cave's gate: var 60 to 3 as Twinsen first comes into the bar (the gate in scene 2 gone for good)", model =>
            p.Add(model, RaceTrackScenes.InvisibleHelper(model.Actors[0].X, model.Actors[0].Y, model.Actors[0].Z), $@"void comportement_0()
{{
    if ({CaveGateOpen} > var_game({CaveVar}))
    {{
        set_var_game({CaveVar}, {CaveGateOpen});
    }}
    suicide();
}}
"));

    private const int MouthScene = 45;
    private static readonly (int X0, int X1, int Z0) MouthZone = (29696, 30208, 11776);
    private const int MouthReach = 30464, MouthLanding = 31744 - 512;

    private static void TraluCaveMouth(StoryParts p) =>
        p.Edit(MouthScene, $"the Tralu's cave's mouth: its door reaches out in front of the cliff's face (to x {MouthReach}) and lands Twinsen clear of the cave's door back out", model =>
        {
            var mouth = model.Zones.FirstOrDefault(z => z.Type == 0 && z.Num == TraluCave && z.X0 == MouthZone.X0 && z.X1 == MouthZone.X1 && z.Z0 == MouthZone.Z0)
                        ?? throw new NotTheGames("scene 45's door into the Tralu's cave");
            mouth.X1 = MouthReach;
            mouth.Info[0] = MouthLanding;
            return _ => { };
        });

    // The spider cave's lower mouth (the user, 2026-11-06: "A cave on the way back from the Tralu area won't let Twinsen in ... It's the
    // cave that has a large spider in it"): scene 47's door into the spider cave (scene 51), x 29392-30560, z 6656-7168, in the cliff
    // south of the sign of Sendell. The town circuit's road (the fine weather's file) runs through the cliff there, so the build took the
    // zone out as a door under a road's surface -- in the storm too, where the cliff and the mouth are the game's: no way in at all. The
    // build now shuts it only while that file is shown (RaceTrackScenes, door_off). And, as at the Tralu's cave's mouth, the zone lay
    // behind the cliff's face: walking north at it, Twinsen was stopped at z 6648, 8 short of the zone, anywhere but in the opening's
    // middle (headless, 2026-11-07: in at x 29700, 29952 and 30200; stopped at z 6648 coming from x 29300, 29500 and 30400). Out of the
    // cave he landed at x 29184-30208 (29184 plus how far along the cave's door, 1024 wide, he went into it), mostly against the cliff
    // west of the opening, and walking back north he stopped there. Now the zone reaches 256 out in front of the face (from z 6400; the
    // town file's door_off moved with it) and the way out lands him in front of the opening, x 29440-30464 round its middle (29952), z
    // 6144: 256 clear of the zone.
    private const int SpiderOutside = 47, SpiderCave = 51;
    private static readonly (int X0, int Z0, int X1, int Z1) SpiderMouth = (29392, 6656, 30560, 7168);
    private const int SpiderReach = 6400, SpiderLanding = 29952 - 512, SpiderOldLanding = 29184;

    private static void SpiderCaveMouth(StoryParts p)
    {
        p.Edit(SpiderOutside, $"the spider cave's lower mouth: its door reaches out in front of the cliff's face (from z {SpiderReach})", model =>
        {
            var mouth = model.Zones.FirstOrDefault(z => z.Type == 0 && z.Num == SpiderCave && (z.X0, z.Z0, z.X1, z.Z1) == SpiderMouth)
                        ?? throw new NotTheGames("scene 47's lower door into the spider cave");
            mouth.Z0 = SpiderReach;
            // (shut in the town circuit's file still: its door_off has the zone's box)
            var was = $" {SpiderOutside} {SpiderMouth.X0} {SpiderMouth.Z0} {SpiderMouth.X1} {SpiderMouth.Z1}";
            if (p.Scenes.DoorsOff is { } off)
                for (var i = 0; i < off.Count; i++)
                    if (off[i].EndsWith(was)) off[i] = off[i][..^was.Length] + $" {SpiderOutside} {SpiderMouth.X0} {SpiderReach} {SpiderMouth.X1} {SpiderMouth.Z1}";
            return _ => { };
        });
        p.Edit(SpiderCave, $"the spider cave's lower way out: Twinsen out in front of the mouth's opening (x {SpiderLanding}-{SpiderLanding + 1024})", model =>
        {
            var back = model.Zones.FirstOrDefault(z => z.Type == 0 && z.Num == SpiderOutside && z.Info.Length > 2 && z.Info[0] == SpiderOldLanding)
                       ?? throw new NotTheGames("scene 51's lower way out");
            back.Info[0] = SpiderLanding;
            return _ => { };
        });
    }
}
