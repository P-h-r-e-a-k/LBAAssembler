using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Zoe as Twinsen's companion (docs/LBRA_STORY_SCRIPT.md, "Twinsen and Zoe"; the user, 2026-11-06/08: "Zoe as companion", built
// 2026-11-11): from chapter 2 she goes with him -- every outside scene of every island the story is built on has her, a copy of the
// house's Zoe marked by her armour (CompanionArmour), and the race file's companion= has the engine put her beside him as each scene
// starts and as he gets out of his car, and keep her out of sight while he drives or while the scene has a Zoe of its own in sight
// (RACEMOD.CPP Companion). Her script follows him: turned to him, walking when he's more than a few steps off, standing by him
// otherwise. Not in the dream (chapter 0) nor the storm (chapter 1), when she's at home, nor on the Elevator Platform (chapter 8) or at
// the final (14), as the design has it.
// Still to come (the design): her hint for the step Twinsen is on when he talks to her, and the pit box in races.
internal static partial class RaceTrackStory
{
    internal const int CompanionArmour = 117;   // (a signed byte in the scene record: 117, which no one of the game's has)
    // (Zoe's entity in the game: scene 0's Zoe's, every Zoe of the story's a copy of her)
    internal const int CompanionEntityId = 15;
    // (her steps: walking once he's this far off, standing again this near)
    private const int CompanionWalkFrom = 1300, CompanionStandAt = 900;

    internal static string CompanionWhen => "2 <= chapter() && 8 != chapter() && 14 > chapter()";

    // Her in every outside scene of `island` (its scenes as built). Returns lines for the log.
    internal static List<string> Companion(string gameDirectory, RaceTrackIsland island)
    {
        var log = new List<string>();
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var zoe = Copy(RaceTrackService.OriginalScenes(store), OpeningScene, Zoe);
        if (zoe is null || zoe.Entity != CompanionEntityId) { log.Add($"Zoe as companion left out: scene 0's Zoe isn't entity {CompanionEntityId}"); return log; }
        var changes = new List<SceneChange>();
        foreach (var scene in island.Scenes.Distinct())
        {
            if (!store.SceneExists(scene)) continue;
            SceneModel model;
            try { model = store.Load(scene); } catch (Exception e) when (e is InvalidDataException or ArgumentException) { continue; }
            if (model.CubeMode != 1 || model.Actors.Skip(1).Any(a => a.Entity == zoe.Entity && a.Armor == CompanionArmour)) continue;
            var her = zoe.Clone();
            (her.X, her.Y, her.Z) = (model.Hero.X, model.Hero.Y, model.Hero.Z);
            her.Armor = CompanionArmour;
            her.Life = new byte[] { 0 }; her.Track = new byte[] { 0 };
            GiveVoice(her);
            var index = SceneOps.AddActor(model, her);
            var scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
            scripts.SetText(index, ScriptKind.Life, CompanionLife);
            scripts.SetText(index, ScriptKind.Track, "label(0);\nstop();\n");
            var built = scripts.Build();
            if (!built.Ok) { log.Add($"scene {scene}: Zoe as companion left out: {string.Join("; ", built.Errors.Take(2))}"); continue; }
            changes.Add(new SceneChange(scene, SceneSerializer.Parse(SceneGame.Lba2, built.Record!), null));
        }
        if (changes.Count > 0) store.SaveMany(changes, allowErrors: true);
        log.Add($"Zoe as companion: in {changes.Count} outside scene(s) of {island.Name} (actor of entity {zoe.Entity}, armour {CompanionArmour}), from chapter 2");
        return log;
    }

    private static string CompanionLife => $@"void comportement_0()
{{
    if ({CompanionWhen})
    {{
        set_comportement(comportement_1);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
    set_dir(MOVE_FOLLOW, 0);
    if ({CompanionWalkFrom} < distance(0))
    {{
        anim(1);
    }}
    else if ({CompanionStandAt} > distance(0))
    {{
        anim(0);
    }}
}}
";
}
