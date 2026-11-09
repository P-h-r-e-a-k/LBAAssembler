using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;
using LBAAssembler.Terrain.Citadel;

namespace LBAAssembler.Terrain;

// The Tavern regulars' secret bar (the user, 2026-11-05: "have the tralu caves as a secret tavern, perhaps with the tralu sitting in a
// corner moaning that his quiet cave has become too noisy"; 2026-11-06: "there's somebody sitting down in mid-air. Let's move the bar
// scene area to scene '19: Citadel Island, Tralu 2nd scene (room #21)', it's supposed to be a secret easter egg for people to find ...
// Remove any references to the Tralu's cave", and "Perhaps some very vague hints about the Tavern owner being nowhere to be seen, and
// perhaps Tim the waiter could be in the new bar area too"). Chapter 1 -- the storm, the Tavern gone for the storm track, and the mayor's
// ban on drink while the island has a race track ("no drinking and driving"):
//   - the hints, and only that Luc is nowhere to be seen: Tim on the Tavern's old doorstep (StormPeople: the ban, nobody has seen Luc all
//     day, nor the Tavern's bottles -- asked a second time while the thief is still out there, once a visit, and from the umbrella given
//     back on), and the mushroom man by the Wizard's tent (scene 45's actor 4, after his first line of the day, once a visit, Once4):
//     nobody has seen Luc since the ban, and half his regulars go missing of an evening and come home singing. No place is named.
//   - the bar: the Back Room, the Tralu's caves' second hall (scene 19), made bigger and furnished by the build (TraluBarRoom: the hall
//     carried west, the massive old table with its benches, barrels for round tables, candlesticks). The Tavern's people copied from it
//     (scene 3) -- Luc behind a counter of the game's dark crates (sprite 107, scene 4's, fixed) against the north wall between the
//     candlesticks; Pat and Fab of Raph's band (the noise) and the visitor from the Tavern ("not from here") sitting on the table's
//     benches, and the drunk rabbibunny at its far end; Tim serving beside it, once his umbrella is back and till Twinsen is tired (the
//     doorstep's Tim isn't there then: TimInBar). Each says their line once Twinsen is near and presses Action (Talker: each far enough
//     from the others that one Action is one of them) -- Pat's, and Fab's after it. The ones who sit sit on the benches: their sitting
//     animations (the Tavern's own, on its stools and chairs) at the benches' height.
//   - the hall's own creatures go -- the two that hunt Twinsen for the keys they carry (actors 3 and 6) and the one that drops from the
//     ceiling by the way to the gate (2) -- and the two barred doors those keys opened between the hall and the way to scene 2 (4 and 5:
//     the regulars come and go that way, RaceTrackStory.TraluCaves), and the spike zones where the hall is now (its stalagmites the build
//     took out).
//   - the Tralu has joined them (the user, 2026-11-06: "Move him into the bar area ... with a large tankard of beer. Keep his lines,
//     maybe adjusted for being in the bar itself"): the game's Tralu (scene 2's actor 3) copied into the bar, standing at the round
//     table by the old room's north wall with his big tankard on it (TraluBarRoom.InstallTankard: a body of the build's, the props'
//     entity), no hunt and beyond harm, grumbling that the whole Tavern has moved into his back room, with drums -- so he may as well
//     have a tankard; in chapter 2 he has the bar and his tankard to himself. His own hall in the caves' first scene (scene 2) is empty:
//     its Tralu is gone in every chapter (one Tralu at a time).
// Chapter 2 (the fine weather): the Esmers have put the Tavern back, the regulars are in it again and gone from the bar (the game's Tavern
// scene keeps its own); the hall keeps its furniture. None of the regulars stands anywhere else in chapter 1: the Tavern's scene is shut
// in the storm (its door went with the storm track's road), Luc is in the yard and the rabbibunny round the Esmers' landing only in
// chapter 2 (TownPeople, Yard), and Tim is on the doorstep or in the bar, never both.
internal static partial class RaceTrackStory
{
    // the game's people of the Tavern copied (scene 3): the visitor "not from here", Fab and Pat of Raph's band (TavernRabbibunny, TavernLuc
    // and TavernTim are Chapters.cs's); the game's Tralu in his cave (actor 3, entity 20) and its crate (scene 4's actor 3: sprite 107)
    private const int TavernVisitor = 3, TavernFab = 7, TavernPat = 8, CaveTralu = 3, TraluEntity = 20, CrateScene = 4, CrateActor = 3;
    // (the crate: SPRITE_3D, fallable, no shadow, a carrier, the game's square box -- and in the way, CHECK_OBJ_COL and CHECK_BRICK_COL;
    // without the game's PUSHABLE and CHECK_ZONE)
    private const uint CrateFlags = 0xDC03;
    // the bar's hall (scene 19) and the game's actors in it that go: the ceiling's creature, the two key-carrying hunters, the two barred doors
    private const int BarHall = 19;
    private static readonly int[] HallGone = { 2, 3, 4, 5, 6 };
    private static readonly (int Entity, int Sprite)[] HallGoneAre = { (64, -1), (65, -1), (-1, 186), (-1, 186), (65, -1) };
    // where each is (TraluBarRoom's cells: the table x 36-37, z 40-51, its benches x 35 and 38): Luc against the north wall (z 34) between
    // the candlesticks, his counter of crates in front of him; Pat and Fab on the west bench, the visitor and the rabbibunny on the east,
    // Tim beside the east bench, facing the table
    private static int Cell(double c) => (int)Math.Round(c * 512);
    private static readonly (int X, int Z) LucAt = (Cell(33.3), Cell(35.7)), PatAt = (Cell(35.5), Cell(42.5)), FabAt = (Cell(35.5), Cell(44.5)),
                                           VisitorAt = (Cell(38.5), Cell(41.5)), RabbibunnyAt = (Cell(38.87), Cell(48.5)), TimAt = (Cell(39.6), Cell(45.0)),
                                           HallMiddle = (Cell(36.5), Cell(45.5));
    private static readonly (int X, int Z)[] Crates = { (Cell(31.85), Cell(37.1)), (Cell(33.3), Cell(37.1)), (Cell(34.75), Cell(37.1)) };
    // how high each sitter stands above the floor to sit on a bench: the Tavern's own height for the visitor (208 over its floor, on his
    // chair), the same for Pat and Fab; the drunk rabbibunny's sitting animation sits higher and further back, so he is 316 up and a
    // third of a cell out onto the bench's back (RabbibunnyAt). (The user, 2026-11-07: "none of the NPC's are sitting correctly on the
    // bench" -- at the bench's top, 512 up, they sat on the table's height in the air; tried out headless with actorpos, 2026-11-08.)
    private const int SitHigh = TraluBarRoom.FloorY + 208, BunnySitHigh = TraluBarRoom.FloorY + 316;
    // the Tralu in the bar: in the corner behind the round table by the old room's north wall (its barrel's cells x 43-44, z 36-37), the
    // wall and the foot of the stairs behind him, facing the camera with the barrel and his tankard on it in front of him (the user,
    // 2026-11-08: "in the corner ... It fits him being moody about the bar being there"; he stood east of the barrel, in front of it)
    private static readonly (int X, int Z) TraluAt = (Cell(42.5), Cell(35.8));
    // how near Twinsen comes to talk: the regulars' (Luc's across his counter), the Tralu's (a big fellow, behind his barrel in his corner)
    private const int RegularReach = 750, LucReach = 1600, TraluReach = 2200;

    private static int Face((int X, int Z) from, (int X, int Z) to) => RaceTrackMath.Turn(Math.Atan2(to.X - from.X, to.Z - from.Z));

    private static void TraluTavern(StoryParts p)
    {
        if (!TraluBarRoom.Built(p.Store.Directory)) p.Log.Add("the secret bar left out: the build didn't make scene 19's hall (TraluBarRoom)");
        else p.Edit(BarHall, "the Tavern regulars' secret bar in the storm: Luc behind his counter, Pat and Fab, the visitor and the drunk rabbibunny at the massive table, Tim serving once his umbrella is back; the hall's creatures and barred doors gone", model =>
        {
            for (var k = 0; k < HallGone.Length; k++)
                if (model.Actors.ElementAtOrDefault(HallGone[k]) is not { } a || a.Entity != HallGoneAre[k].Entity || (HallGoneAre[k].Sprite >= 0 && a.Sprite != HallGoneAre[k].Sprite))
                    throw new NotTheGames($"scene 19's actor {HallGone[k]}");
            var luc = Copy(p.Originals, TavernScene, TavernLuc);
            var bunny = Copy(p.Originals, TavernScene, TavernRabbibunny);
            var pat = Copy(p.Originals, TavernScene, TavernPat);
            var fab = Copy(p.Originals, TavernScene, TavernFab);
            var visitor = Copy(p.Originals, TavernScene, TavernVisitor);
            var tim = Copy(p.Originals, TavernScene, TavernTim);
            if (luc is null || bunny is null || pat is null || fab is null || visitor is null || tim is null) throw new NotTheGames("the Tavern's people");
            void Place(SceneActorModel who, (int X, int Z) at, int y, (int X, int Z) facing) => (who.X, who.Y, who.Z, who.Beta) = (at.X, y, at.Z, Face(at, facing));
            Place(luc, LucAt, TraluBarRoom.FloorY, HallMiddle);
            Place(tim, TimAt, TraluBarRoom.FloorY, (Cell(36.5), TimAt.Z));
            // (the sitters face across the table)
            Place(pat, PatAt, SitHigh, (Cell(38.5), PatAt.Z));
            Place(fab, FabAt, SitHigh, (Cell(38.5), FabAt.Z));
            Place(visitor, VisitorAt, SitHigh, (Cell(35.5), VisitorAt.Z));
            Place(bunny, RabbibunnyAt, BunnySitHigh, (Cell(35.5), RabbibunnyAt.Z));
            // (the spike zones where the hall is now, and on the steps up to the corridor's landing -- the stalagmites the build took out)
            model.Zones.RemoveAll(z => z.Type == 8 && z.X0 < (TraluBarRoom.East + 1) * 512 && z.X1 > TraluBarRoom.West * 512
                                       && z.Z0 < (TraluBarRoom.South + 1) * 512 && z.Z1 > TraluBarRoom.North * 512);
            model.Zones.RemoveAll(z => z.Type == 8 && z.X0 < (TraluBarRoom.UpFrom + 4) * 512 && z.X1 > TraluBarRoom.UpFrom * 512
                                       && z.Z0 < 44 * 512 && z.Z1 > 39 * 512);

            var edits = new List<Action<SceneScripts>>();
            const string Storm = "2 > chapter()";
            // (Luc: his welcome and the back gate's key -- told once, BackGateVar to 1: RaceTrackStory.TraluCaves -- then the key again till
            // the back gate is open, his welcome after that. The cave's own gate opens as Twinsen comes in.)
            edits.Add(p.Add(model, luc, Talker(Storm, $@"            if (0 == var_game({BackGateVar}))
            {{
                message({p.Say(ChapterLine.LucBar)});
                message({p.Say(ChapterLine.LucKey)});
                set_var_game({BackGateVar}, {BackKeyTold});
            }}
            else if ({BackGateOpen} > var_game({BackGateVar}))
            {{
                message({p.Say(ChapterLine.LucKey)});
            }}
            else
            {{
                message({p.Say(ChapterLine.LucBar)});
            }}", LucReach)));
            edits.Add(p.Add(model, bunny, Talker(Storm, $"            message({p.Say(ChapterLine.RabbibunnyBar)});", RegularReach)));
            var fabIndex = model.Actors.Count + 1;
            edits.Add(p.Add(model, pat, Talker(Storm, $"            message({p.Say(ChapterLine.PatBar)});\n            message_obj({fabIndex}, {p.Say(ChapterLine.FabBar)});", RegularReach)));
            edits.Add(p.Add(model, fab, OnlyWhile(Storm)));
            edits.Add(p.Add(model, visitor, Talker(Storm, $"            message({p.Say(ChapterLine.StrangerBar)});", RegularReach)));
            edits.Add(p.Add(model, tim, Talker(TimInBar, $"            message({p.Say(ChapterLine.TimBar)});", RegularReach)));
            foreach (var at in Crates)
                if (Copy(p.Originals, CrateScene, CrateActor) is { } crate)
                {
                    (crate.X, crate.Y, crate.Z, crate.Beta) = (at.X, TraluBarRoom.FloorY, at.Z, 0);
                    crate.Flags = CrateFlags;
                    edits.Add(p.Add(model, crate, OnlyWhile(Storm)));
                }
            // (the Tralu at his round table, his tankard on it -- every chapter: his back room, noisy in the storm, his own in chapter 2)
            if (Copy(p.Originals, TraluCave, CaveTralu) is not { Entity: TraluEntity } tralu) throw new NotTheGames("the cave's Tralu");
            // (turned to the camera, his face to whoever comes to talk -- the user, 2026-11-07: "have the Tralu angled so we can see his
            // face to talk to him" -- his tankard at his side)
            Place(tralu, TraluAt, TraluBarRoom.FloorY, (TraluAt.X + 512, TraluAt.Z + 512));
            tralu.Armor = 51;
            // (the evening after the lighthouse -- Tired, when everyone tells Twinsen to go home and have a nap (tired=): his moan first, a
            // line tired= leaves as it is (RaceTrackStory.TiredKeep), and then he says it too -- the user, 2026-11-08: "First he should moan
            // about people being in his bar ... then tell him to go home")
            edits.Add(p.Add(model, tralu, Talker("0 < chapter()", $@"            if (2 > chapter() && {Tired} == var_game({DayVar}))
            {{
                message({EveningId(p.Say, EveningLine.TraluEvening)});
                message({p.Said(Line.Tired)});
            }}
            else if (2 > chapter())
            {{
                message({p.Say(ChapterLine.TraluNoisy)});
            }}
            else
            {{
                message({p.Say(ChapterLine.TraluQuiet)});
            }}", TraluReach)));
            edits.Add(p.Add(model, Prop(TraluBarRoom.TankardAt.X, TraluBarRoom.TankardAt.Y, TraluBarRoom.TankardAt.Z, TraluBarRoom.TankardGeneric, 3072), OnlyWhile("0 < chapter()")));
            // (a bottle and a plate round the middle of every other barrel's top, the round tables' -- the tankard's barrel is the Tralu's)
            foreach (var barrel in TraluBarRoom.Barrels)
            {
                var (bx, by, bz) = TraluBarRoom.BarrelTop(barrel);
                if (bx == TraluBarRoom.TankardAt.X && bz == TraluBarRoom.TankardAt.Z) continue;
                SceneOps.AddActor(model, CitadelProps.Actor(CitadelProps.Bottle, bx + 100, by, bz - 110));
                SceneOps.AddActor(model, CitadelProps.Actor(CitadelProps.Plate, bx - 90, by, bz + 80, 600));
            }
            return scripts =>
            {
                foreach (var gone in HallGone)
                {
                    scripts.SetText(gone, ScriptKind.Life, "void comportement_0()\n{\n    suicide();\n}\n");
                    scripts.SetText(gone, ScriptKind.Track, "label(0);\nstop();\n");
                }
                foreach (var e in edits) e(scripts);
            };
        });

        // the Tralu gone from his own hall: he has joined the secret bar next door (above), with his tankard. (The game's barred door,
        // actor 8 -- open once the bar is found, RaceTrackStory.TraluCaves -- takes the Tralu gone for the Tralu killed: var 60 to 2, the
        // hall's pots broken; that is left out of its script)
        p.Edit(TraluCave, "the Tralu gone from his own hall: he has joined the secret bar next door, with a tankard", model =>
        {
            if (model.Actors.ElementAtOrDefault(CaveTralu) is not { Entity: TraluEntity }) throw new NotTheGames("the cave's Tralu");
            return scripts =>
            {
                scripts.SetText(CaveTralu, ScriptKind.Life, "void comportement_0()\n{\n    suicide();\n}\n");
                scripts.SetText(CaveTralu, ScriptKind.Track, "label(0);\nstop();\n");
                scripts.SetText(CaveDoor, ScriptKind.Life, Loose(scripts.GetText(CaveDoor, ScriptKind.Life), @"oneif (1 > life_point_obj(3))
    {
        if (1 == var_game(60))
        {
            set_var_game(60, 2);
            set_var_cube(10, 2);
            set_var_cube(11, 2);
            set_var_cube(12, 2);
            set_var_cube(13, 2);
            set_var_cube(14, 2);
            hit_obj(12, 1);
            hit_obj(13, 1);
            hit_obj(14, 1);
            hit_obj(15, 1);
            hit_obj(16, 1);
        }
    }", ""));
            };
        });

        // the mushroom man by the Wizard's tent, before the race is won (the game's StormPlot under 3: his line of the rain and the
        // mushrooms): nobody has seen Luc, once a visit
        p.Edit(WizardTentOutside, "the mushroom man: nobody has seen Luc since the ban (once a visit, before the race is won)", _ => scripts =>
            scripts.SetText(MushroomMan, ScriptKind.Life, Loose(scripts.GetText(MushroomMan, ScriptKind.Life), "case 0:\nmessage(378);", $@"case 0:
                message(378);
                if (0 == var_cube({Once4}))
                {{
                    set_var_cube({Once4}, 1);
                    message({p.Say(ChapterLine.MushroomLuc)});
                }}")));
    }

    // someone there only while `when` holds when the scene starts, with nothing to say
    private static string OnlyWhile(string when) => $@"void comportement_0()
{{
    if ({when})
    {{
        end_life();
    }}
    else
    {{
        suicide();
    }}
}}
";
}
