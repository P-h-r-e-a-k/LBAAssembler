using LBAAssembler.Scenes;
using LBAAssembler.LbaScript;

namespace LBAAssembler.Terrain;

// Stella Entities' people (StellaEntities, the club under the Emerald Moon's reactor; docs/LBRA_STRIP_JOINT_DESIGN.md: "Bouncers: Rick's,
// entity 297 (two at the entrance, one at backstage) and 302 (the office)"; "the bouncers at the entrance let him through and only keep
// him out of backstage and the office"). Copies of Otringal's own -- Rick's bouncer (scene 136, entity 297), his barman (294) and the
// guard at his office's door (scene 137, entity 302) -- and, Rick's bar having none, an Esmer of the moon base's (its mechanic, scene 23's
// actor 14) behind the counter as the barman -- with scripts of their own (Rick's pick fights): standing at their places facing
// the room, each with a word for Twinsen as he comes near (once a visit, again once he has gone away and come back), the barman's on
// Action at his counter.
internal static partial class RaceTrackStory
{
    private const int RickBar = 136, RickOffice = 137, BouncerEntity = 297, BossGuardEntity = 302, MoonBase = 23, BaseMechanic = 14;
    // (the club's scene variables: whether each has had his word this time he came near)
    private const int ClubSaidFirst = 10;

    private static void StellaPeople(IslandBeatsA b, Func<MoonLine, int> say)
    {
        if (!b.Store.SceneExists(StellaEntities.Scene)) { b.Log.Add($"Stella Entities' people left out: no scene {StellaEntities.Scene}"); return; }
        var bouncer = CopyOf(b.Originals, RickBar, BouncerEntity);
        var barman = Copy(b.Originals, MoonBase, BaseMechanic);
        var guard = CopyOf(b.Originals, RickOffice, BossGuardEntity);
        if (bouncer is null || barman is null || guard is null) { b.Log.Add("Stella Entities' people left out: Rick's bouncer or guard isn't in Otringal's scenes, or the moon base's mechanic isn't in scene 23"); return; }
        b.Edit(StellaEntities.Scene, "Stella Entities' people: Rick's bouncers at the way in and the catwalk, his guard at the office's door, an Esmer barman", model =>
        {
            var adds = new List<Action<SceneScripts>>();
            var said = ClubSaidFirst;
            // someone at a place round the room's middle (degrees, cells), facing it, a word as Twinsen comes within `near`
            void Place(SceneActorModel who, (int X, int Z) at, int line, int near, bool onAction = false)
            {
                var a = who.Clone();
                (a.X, a.Y, a.Z) = (at.X, StellaEntities.FloorY, at.Z);
                a.Beta = StellaEntities.FacingMiddle(at.X, at.Z);
                a.Move = 0;
                var v = said++;
                var life = onAction
                    ? $@"void comportement_0()
{{
    swif (1 == action())
    {{
        if ({near} > distance(0))
        {{
            message({line});
        }}
    }}
}}
"
                    : $@"void comportement_0()
{{
    if ({near} > distance(0))
    {{
        if (0 == var_cube({v}))
        {{
            message({line});
            set_var_cube({v}, 1);
        }}
    }}
    else if ({near * 2} < distance(0))
    {{
        set_var_cube({v}, 0);
    }}
}}
";
                adds.Add(IslandBeatsA.Add(model, a, life));
            }
            // the way in: either side of the corridor's mouth; the catwalk's foot, beside it; the office's door; the barman behind his counter
            Place(bouncer, StellaEntities.At(-12, 16.6f), say(MoonLine.ClubWelcome), 1600);
            Place(bouncer, StellaEntities.At(15, 16.6f), say(MoonLine.ClubWelcome), 0);
            Place(bouncer, StellaEntities.At(258, 9.6f), say(MoonLine.ClubStaffOnly), 1300);
            Place(guard, StellaEntities.At(Citadel.CitadelProps.OfficeAt, 17.3f), say(MoonLine.ClubNoBoss), 1300);
            Place(barman, StellaEntities.At(142, 16.6f), say(MoonLine.ClubBarman), 1700, onAction: true);
            // the seven performers (StellaPerformers: the game's own bodies recoloured, bodies of the hero's entity, each her own dance): three
            // on the poles (10, 60 and 110 degrees -- the actor on the pole's spot: her body stands beside it, her dance takes her round,
            // up and over it), four on the stage's other sides, strutting along its edge; no one's blows stopped by them (CHECK_OBJ_COL off)
            void Perform(int who, float degrees, float cells, bool atPole)
            {
                var a = bouncer.Clone();
                var (x, z) = StellaEntities.At(degrees, cells);
                (a.X, a.Y, a.Z) = (x, StellaEntities.FloorY + (int)Citadel.CitadelProps.StageHigh, z);
                a.Entity = StellaPerformers.Entity;
                a.Body = StellaPerformers.Body0 + who;
                a.Anim = StellaPerformers.AnimOf(who);
                a.Flags &= ~1u;
                a.Beta = (StellaEntities.FacingMiddle(x, z) + 2048) & 4095;
                a.Move = 0;
                adds.Add(IslandBeatsA.Add(model, a, "void comportement_0()\n{\n}\n"));
            }
            Perform(3, 10, StellaEntities.PoleCells, true); Perform(1, 60, StellaEntities.PoleCells, true); Perform(6, 110, StellaEntities.PoleCells, true);
            Perform(0, 160, 6.2f, false); Perform(2, 205, 6.2f, false); Perform(4, 300, 6.2f, false); Perform(5, 345, 6.2f, false);
            return scripts => { foreach (var add in adds) add(scripts); };
        });
    }
}
