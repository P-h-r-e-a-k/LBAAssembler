using LBAAssembler.Scenes;

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
internal static partial class RaceTrackStory
{
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
}
