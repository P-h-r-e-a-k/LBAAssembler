using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Tralu's cave as the regulars' secret tavern (the user, 2026-11-05: "The tralu caves no longer form part of the story for Citadel
// island, given that in Citadel 1 there's no travel let's add a story detail that the mayor has banned alcohol and have the tralu caves as
// a secret tavern, perhaps with the tralu sitting in a corner moaning that his quiet cave has become too noisy or similar"). In the game
// the cave (scene 2, its mouth in the cliff by the Weather Wizard's tent, scene 45) is where the Tralu holds Raph; in the story Raph is at
// the race track and the cave had nothing left in it but the Tralu himself, still the game's boss on the hunt. Now, in chapter 1 -- the
// storm, the Tavern gone for the storm track, and the mayor's ban on drink while the island has a race track ("no drinking and driving"):
//   - Tim on the Tavern's old doorstep (StormPeople) tells of the ban, and of Luc gone with the Tavern's bottles and drumming in the Tralu's
//     cave, asked a second time while the thief is still out there (once a visit) and from the umbrella given back on;
//   - the mushroom man by the Wizard's tent (scene 45's actor 4, his first line of the day said) hears the drums under the cliff: half the
//     Tavern creeps in there of an evening (once a visit, Once4);
//   - in the cave's first hall (the low floor inside its mouth, layer 0: 256), the Tavern's people copied from it (scene 3): Luc behind a
//     bar of two crates (the game's dark crate, sprite 107, scene 4's -- fixed, not pushed, not a zone's) where the tunnel comes in, the
//     drunk rabbibunny propped against a stalagmite in the middle, Pat and Fab (Raph's band: the noise) by the west wall, the visitor from
//     the Tavern ("not from here") across from them; each with a line of their own, said once Twinsen is near and presses Action (Talker:
//     each far enough from the others that one Action is one of them) -- Pat's, and Fab's after it;
//   - the Tralu (the game's own, actor 3: its life and track the story's now, no hunt and beyond harm) in the hall's far corner, standing
//     in the game's own idle animation, grumbles: his quiet cave too noisy.
// Chapter 2 (the fine weather): the Esmers have put the Tavern back, the regulars and their crates are in it again and gone from the cave
// (the game's Tavern scene keeps its own), and the Tralu is glad of the quiet -- though the aliens want him in their race (Flick's six cars).
// None of the regulars stands anywhere else in chapter 1: the Tavern's scene is shut in the storm (its door went with the storm track's
// road), Luc is in the yard and the rabbibunny round the Esmers' landing only in chapter 2 (TownPeople, Yard), and Tim stays on the doorstep.
internal static partial class RaceTrackStory
{
    // the game's people of the Tavern copied (scene 3): the visitor "not from here", Fab and Pat of Raph's band (TavernRabbibunny and
    // TavernLuc are Chapters.cs's); the game's Tralu in his cave (actor 3, entity 20) and its crate (scene 4's actor 3: sprite 107)
    private const int TavernVisitor = 3, TavernFab = 7, TavernPat = 8, CaveTralu = 3, TraluEntity = 20, CrateScene = 4, CrateActor = 3;
    // (the crate: SPRITE_3D, fallable, no shadow, a carrier, the game's square box -- and in the way, CHECK_OBJ_COL and CHECK_BRICK_COL;
    // without the game's PUSHABLE and CHECK_ZONE)
    private const uint CrateFlags = 0xDC03;
    // the hall inside the cave's mouth (cells x 43-55, z 47-62 of its grid; the mouth's corridor comes in from the east along z 44-50):
    // its floor (layer 0's top) and where each stands -- Luc against its north wall (z 46) where the corridor comes in, his crates a bar in
    // front of him, the rabbibunny by the stalagmite at (49, 51), Pat and Fab by the stalagmites along the west wall (x 42), the visitor
    // across from them, and the Tralu in the north-west corner, facing them all: the hall's back corner as the camera sees it (one by its
    // south or east walls is behind them, the walls nearest the camera)
    private const int CaveFloor = 256;
    private static readonly (int X, int Z) LucAt = (26675, 24320), RabbibunnyAt = (24883, 25702), PatAt = (23040, 27136), FabAt = (23650, 27545),
                                           VisitorAt = (25090, 27900), TraluAt = (22477, 24525), HallMiddle = (24576, 27136);
    private static readonly (int X, int Z)[] Crates = { (26300, 24830), (27050, 24830) };
    // how near Twinsen comes to talk: the regulars' (Luc's across his bar), the Tralu's (a big fellow)
    private const int RegularReach = 750, LucReach = 1400, TraluReach = 1500;

    private static int Face((int X, int Z) from, (int X, int Z) to) => RaceTrackMath.Turn(Math.Atan2(to.X - from.X, to.Z - from.Z));

    private static void TraluTavern(StoryParts p)
    {
        p.Edit(TraluCave, "the Tralu's cave, the regulars' secret tavern in the storm: Luc's bar of crates, the band, the drunk rabbibunny, the visitor, and the Tralu grumbling in his corner (glad of the quiet in chapter 2)", model =>
        {
            if (model.Actors.ElementAtOrDefault(CaveTralu) is not { Entity: TraluEntity } tralu) throw new NotTheGames("the cave's Tralu");
            var luc = Copy(p.Originals, TavernScene, TavernLuc);
            var bunny = Copy(p.Originals, TavernScene, TavernRabbibunny);
            var pat = Copy(p.Originals, TavernScene, TavernPat);
            var fab = Copy(p.Originals, TavernScene, TavernFab);
            var visitor = Copy(p.Originals, TavernScene, TavernVisitor);
            if (luc is null || bunny is null || pat is null || fab is null || visitor is null) throw new NotTheGames("the Tavern's people");

            // the Tralu, in his corner facing the hall: no longer the game's hunter (his life and track the story's below), beyond harm
            (tralu.X, tralu.Y, tralu.Z, tralu.Beta) = (TraluAt.X, CaveFloor, TraluAt.Z, Face(TraluAt, HallMiddle));
            tralu.Armor = 51;
            void Stand(SceneActorModel who, (int X, int Z) at, (int X, int Z) facing) => (who.X, who.Y, who.Z, who.Beta) = (at.X, CaveFloor, at.Z, Face(at, facing));
            Stand(luc, LucAt, HallMiddle);
            Stand(bunny, RabbibunnyAt, LucAt);
            Stand(pat, PatAt, VisitorAt);
            Stand(fab, FabAt, VisitorAt);
            Stand(visitor, VisitorAt, FabAt);

            var edits = new List<Action<SceneScripts>>();
            const string Storm = "2 > chapter()";
            edits.Add(p.Add(model, luc, Talker(Storm, $"            message({p.Say(ChapterLine.LucCave)});", LucReach)));
            edits.Add(p.Add(model, bunny, Talker(Storm, $"            message({p.Say(ChapterLine.RabbibunnyCave)});", RegularReach)));
            var fabIndex = model.Actors.Count + 1;
            edits.Add(p.Add(model, pat, Talker(Storm, $"            message({p.Say(ChapterLine.PatCave)});\n            message_obj({fabIndex}, {p.Say(ChapterLine.FabCave)});", RegularReach)));
            edits.Add(p.Add(model, fab, OnlyWhile(Storm)));
            edits.Add(p.Add(model, visitor, Talker(Storm, $"            message({p.Say(ChapterLine.StrangerCave)});", RegularReach)));
            foreach (var at in Crates)
                if (Copy(p.Originals, CrateScene, CrateActor) is { } crate)
                {
                    (crate.X, crate.Y, crate.Z, crate.Beta) = (at.X, CaveFloor, at.Z, 0);
                    crate.Flags = CrateFlags;
                    edits.Add(p.Add(model, crate, OnlyWhile(Storm)));
                }
            return scripts =>
            {
                foreach (var e in edits) e(scripts);
                scripts.SetText(CaveTralu, ScriptKind.Life, Talker("0 < chapter()", $@"            if (2 > chapter())
            {{
                message({p.Say(ChapterLine.TraluNoisy)});
            }}
            else
            {{
                message({p.Say(ChapterLine.TraluQuiet)});
            }}", TraluReach));
                scripts.SetText(CaveTralu, ScriptKind.Track, "label(0);\nstop();\n");
            };
        });

        // the mushroom man by the Wizard's tent, before the race is won (the game's StormPlot under 3: his line of the rain and the
        // mushrooms), the drums under the cliff once a visit
        p.Edit(WizardTentOutside, "the mushroom man hears drums in the Tralu's cave (once a visit, before the race is won)", _ => scripts =>
            scripts.SetText(MushroomMan, ScriptKind.Life, Loose(scripts.GetText(MushroomMan, ScriptKind.Life), "case 0:\nmessage(378);", $@"case 0:
                message(378);
                if (0 == var_cube({Once4}))
                {{
                    set_var_cube({Once4}, 1);
                    message({p.Say(ChapterLine.MushroomDrums)});
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
