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
//   - The back gate (the user, 2026-11-06: "In scene 2 there's a soft lock for the top area, where Twinsen in the original game fights and
//     kills the Tralu to get a key, with the tralu moves there's no longer a way to get a key. Perhaps once Twinsen finds the bar area this
//     gate could be unlocked as a quicker way for him to get back to the bar. We'd need to either put a ladder or some stairs there ...
//     Twinsen gets told that there's a key secretly hidden to allow him to open the door. The secret key may work best."). In the game the
//     way from the cave's first hall to the Tralu's hall is a barred door (actor 8) the Tralu's key opens -- Twinsen's own script: a key and
//     touching it, `set_track_obj(8, label_0)`, `use_one_little_key()`, var 60 to 3, and the door is gone for good -- and the Tralu's hall
//     is reached from above, down off the ledge of the gate from scene 19, with no way back up. Now:
//       - the stairs up to the ledge (TraluBarRoom.InstallStairs): from the Tralu's hall back up to the gate, always;
//       - the barred door is the gate's: the game's actor 8 moved onto the ledge across the gate's arch (x 7-8, z 18.5: between the gate's
//         door zone, z 17, and where scene 19's door lands Twinsen, z 19-20), drawn and boxed as scene 19's barred doors were (sprite 186,
//         its clip box moved with it) and sliding aside as they did (`open_right(1024)`); the game's own key script opens it, and it is
//         gone on every visit after (var 60 3). The way from the first hall into the Tralu's hall is open (the door is no longer there).
//         Come from the bar, Twinsen lands on the near side of it, and goes down the stairs: one way, until he has the key;
//       - the key: Luc tells of it in the bar, once he has welcomed Twinsen (var 60 to 1: "a key to it hidden where Sendell keeps an eye
//         on it"), and it is under the sign of Sendell on the ledge's face (the game's text zone there: "Aha! a sign from Sendell"): Action
//         in front of it, once told, while the gate is shut and Twinsen has no key, and a key jumps out (an unseen helper with a key for
//         its bonus, give_bonus). A key used elsewhere is found there again.
//     So the bar's quick way back: the cave's mouth, its first hall, the Tralu's hall, the stairs, the gate -- and scene 19's corridor
//     (its barred doors gone with the bar: RaceTrackStory.TraluTavern).
internal static partial class RaceTrackStory
{
    // the cave's game variable (60: 1 Twinsen told of the key -- the game's own 1, the ledge reached, and 2, the Tralu dead, never come in
    // the story; 3 the gate opened) and its barred door (actor 8), scene 19's barred door copied for its look (actor 4)
    private const int CaveVar = 60, CaveKeyTold = 1, CaveGateOpen = 3, CaveDoor = 8, HallDoor = 4;
    // the gate's place: the arch's left edge, across its middle; the key's: in front of the sign of Sendell
    private static readonly (int X, int Y, int Z) GateAt = (3584, 3328, 9472), KeyAt = (4096, 1792, 11008);
    private const int KeyReach = 900;

    private static void TraluBackGate(StoryParts p)
    {
        if (!TraluBarRoom.StairsBuilt(p.Store.Directory)) { p.Log.Add("the back gate left out: the build didn't make its stairs (TraluBarRoom.InstallStairs)"); return; }
        p.Edit(TraluCave, "the back gate: the game's barred door (actor 8) on the ledge across the gate to the secret bar, the game's key script opens it; its key under the sign of Sendell once Luc has told", model =>
        {
            if (model.Actors.ElementAtOrDefault(CaveDoor) is not { Entity: -1 } door) throw new NotTheGames("the cave's barred door");
            if (Copy(p.Originals, BarHall, HallDoor) is not { Entity: -1, Sprite: 186 } look) throw new NotTheGames("scene 19's barred door");
            // (its clip box is the screen's, from the world's origin: moved by the door's move, as the projection does it -- 24 across and
            // 12 down a cell of x, 24 back and 12 down a cell of z, 15 up a layer)
            int dx = GateAt.X - look.X, dy = GateAt.Y - look.Y, dz = GateAt.Z - look.Z;
            int sx = (dx - dz) * 24 / 512, sy = (dx + dz) * 12 / 512 - dy * 15 / 256;
            door.Sprite = look.Sprite;
            door.Info = new[] { look.Info[0] + sx, look.Info[1] + sy, look.Info[2] + sx, look.Info[3] + sy };
            (door.X, door.Y, door.Z, door.Beta) = (GateAt.X, GateAt.Y, GateAt.Z, look.Beta);
            var key = RaceTrackScenes.InvisibleHelper(KeyAt.X, KeyAt.Y, KeyAt.Z);
            key.OptionFlags = 0x80; key.NbBonus = 1;
            var addKey = p.Add(model, key, $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    swif (1 == action())
    {{
        if ({KeyReach} > distance(0) && {CaveKeyTold} <= var_game({CaveVar}) && {CaveGateOpen} > var_game({CaveVar}) && 0 == nb_little_keys())
        {{
            give_bonus(0);
        }}
    }}
}}
");
            return scripts =>
            {
                // (the door slides aside as scene 19's did, not down into the floor; its life -- gone once var 60 is past 2, gone once open
                // -- and the rest of its track are the game's)
                scripts.SetText(CaveDoor, ScriptKind.Track, Loose(scripts.GetText(CaveDoor, ScriptKind.Track), "open_down(1024);", "open_right(1024);"));
                addKey(scripts);
            };
        });
    }
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
