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
    private const uint NoShadowFlag = 1u << 12;
    // the patrons round the stage (the user, 2026-11-13: "Let's also add some other patrons to the strip joint"): Rick's own regulars from
    // his bar (scene 136) -- the lizard leaning on his table (actor 2) and the dark one on the counter (26), leaning on the stage's rail; the
    // Sup in his coat, a glass in his hand (25), twice, and the Franco in stripes (17) standing at it -- where (degrees round the room, cells
    // out: just outside the rail), facing the stage
    private static readonly (int Actor, float Degrees, float Cells)[] Patrons = { (2, 35, 8.15f), (26, 185, 8.15f), (25, 80, 8.3f), (25, 225, 8.3f), (17, 135, 8.3f), (2, 320, 8.15f) };

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
            // (the guard beside the office's door, not in front of it: the door lets Twinsen in -- the user, 2026-11-13, "Did we add in an
            // office area?")
            Place(guard, StellaEntities.At(Citadel.CitadelProps.OfficeAt - 7, 17.3f), say(MoonLine.ClubNoBoss), 1300);
            // the office's door and the backstage door (the catwalk's end, the stage's height): walked into, into the rooms (StellaRooms) --
            // each zone's corner to the room's place just inside its own door, Twinsen as far into it as into the zone (the engine keeps
            // where in a door's zone he is)
            var (ox, oz) = StellaEntities.At(Citadel.CitadelProps.OfficeAt, 18.1f);
            model.Zones.Add(new SceneZoneModel { Type = 0, Num = StellaRooms.Office, X0 = ox - 450, X1 = ox + 450, Z0 = oz - 450, Z1 = oz + 450, Y0 = StellaEntities.FloorY, Y1 = StellaEntities.FloorY + 1800,
                Info = new[] { StellaRooms.InsideX - 450, StellaRooms.InsideY, StellaRooms.InsideZ - 450, 0, 0, 0, 0, 1 } });
            var stageTop = StellaEntities.FloorY + (int)Citadel.CitadelProps.StageHigh;
            var backZ1 = StellaEntities.CatwalkEndZ + 540;
            model.Zones.Add(new SceneZoneModel { Type = 0, Num = StellaRooms.Backstage, X0 = StellaEntities.CatwalkX0World, X1 = StellaEntities.CatwalkX0World + 1023, Z0 = StellaEntities.CatwalkEndZ, Z1 = backZ1, Y0 = stageTop, Y1 = stageTop + 1800,
                Info = new[] { StellaRooms.InsideX - 512, StellaRooms.InsideY, StellaRooms.InsideZ - 270, 0, 0, 0, 0, 1 } });
            Place(barman, StellaEntities.At(142, 16.6f), say(MoonLine.ClubBarman), 1700, onAction: true);
            // the seven performers (StellaPerformers: the game's own bodies recoloured, bodies of the hero's entity, each her own dance): three
            // on the poles (10, 60 and 110 degrees -- the actor on the pole's spot: her body stands beside it, her dance takes her round,
            // up and over it), four on the stage's other sides, strutting along its edge; no one's blows stopped by them (CHECK_OBJ_COL off).
            // Their boxes take in the room their dances use, far bigger than they are: walked over (WalkOverArmour, RACEMOD.CPP
            // RaceMod_NoCollide), or Twinsen bumped into them and stood on top of them off the stage, and fell from them for ever; and no shadow,
            // which the engine sizes by the box (a big round one beside her, not under her)
            void Perform(int who, float degrees, float cells, bool atPole)
            {
                var a = bouncer.Clone();
                var (x, z) = StellaEntities.At(degrees, cells);
                (a.X, a.Y, a.Z) = (x, StellaEntities.FloorY + (int)Citadel.CitadelProps.StageHigh, z);
                a.Entity = StellaPerformers.Entity;
                a.Body = StellaPerformers.Body0 + who;
                a.Anim = StellaPerformers.AnimOf(who);
                a.Flags &= ~1u;
                a.Flags |= NoShadowFlag;
                a.Armor = WalkOverArmour;
                a.Beta = (StellaEntities.FacingMiddle(x, z) + 2048) & 4095;
                a.Move = 0;
                adds.Add(IslandBeatsA.Add(model, a, "void comportement_0()\n{\n}\n"));
            }
            Perform(3, 10, StellaEntities.PoleCells, true); Perform(1, 60, StellaEntities.PoleCells, true); Perform(6, 110, StellaEntities.PoleCells, true);
            Perform(0, 160, 6.2f, false); Perform(2, 205, 6.2f, false); Perform(4, 300, 6.2f, false); Perform(5, 345, 6.2f, false);
            // the patrons: still, where they are put -- not pushed off the stage's solid cells, nor fallen (CHECK_BRICK_COL, OBJ_FALLABLE off)
            foreach (var (actor, degrees, cells) in Patrons)
            {
                if (Copy(b.Originals, RickBar, actor) is not { } patron) continue;
                var (x, z) = StellaEntities.At(degrees, cells);
                (patron.X, patron.Y, patron.Z) = (x, StellaEntities.FloorY, z);
                patron.Beta = StellaEntities.FacingMiddle(x, z);
                patron.Move = 0;
                patron.Flags &= ~(2u | 0x800u);
                adds.Add(IslandBeatsA.Add(model, patron, "void comportement_0()\n{\n}\n"));
            }
            return scripts => { foreach (var add in adds) add(scripts); };
        });
    }
}
