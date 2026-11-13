using System.IO;
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
// Talked to (Action beside her), she gives a hint for the step Twinsen is on where the island's story has given her some (CompanionTalks:
// each island's story puts its lines in its own text file and her script here; Citadel Island's chapter 2, the Desert, the Emerald Moon).
// The pit box in races: RaceTrackPitCrew (2026-11-11).
// Player 2 as Zoe (Story Mode with two players, 2026-11-11 -- RACEMOD.CPP StoryTwo): each game shows the other player by an actor glued to
// their place, so every scene of the story has the actors for it, indoors too -- Zoe indoors (CompanionIndoorArmour; out of sight with one
// player) and the Twinsen double (DoubleArmour, the hero's entity; out of sight with one player). With two players the scripts see
// TwoPlayersVar 1: her walking after Twinsen stops (the engine moves her to player 2), and she is there whatever the chapter.
internal static partial class RaceTrackStory
{
    internal const int CompanionArmour = 117;   // (a signed byte in the scene record: 117, which no one of the game's has)
    // (two players: Zoe indoors, and the Twinsen double -- the hero's entity, normal behaviour -- each by a mark no one of the game's has; the
    // game variable the story's scripts see 1 in with two players: a spare one, RaceTrackStory.Eggs.cs's list)
    // (TwoPlayersVar was 96 until 2026-11-13, the same variable as the night out's Drunk (SecretChamber): the engine sets it every frame, so Drunk
    // never held and Zoe's spanking never came -- 114 is free, RaceTrackStory.Eggs.cs's spare list)
    internal const int CompanionIndoorArmour = 115, DoubleArmour = 116, DoubleEntityId = 0, TwoPlayersVar = 114;
    // (Zoe's entity in the game: scene 0's Zoe's, every Zoe of the story's a copy of her)
    internal const int CompanionEntityId = 15;
    // (her steps: walking once he's this far off, standing again this near)
    private const int CompanionWalkFrom = 1300, CompanionStandAt = 900;

    internal static string CompanionWhen => "2 <= chapter() && 8 != chapter() && 14 > chapter()";
    // (each island's hints for her, the script's lines that say them -- by the island's file, set by its story as it puts its lines in)
    internal static readonly Dictionary<string, string> CompanionTalks = new(StringComparer.OrdinalIgnoreCase);
    private const int CompanionTalkReach = 1100;

    // Her in every outside scene of `island` (its scenes as built). Returns lines for the log.
    internal static List<string> Companion(string gameDirectory, RaceTrackIsland island)
    {
        var log = new List<string>();
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        var zoe = Copy(RaceTrackService.OriginalScenes(store), OpeningScene, Zoe);
        if (zoe is null || zoe.Entity != CompanionEntityId) { log.Add($"Zoe as companion left out: scene 0's Zoe isn't entity {CompanionEntityId}"); return log; }
        var changes = new List<SceneChange>();
        // (the island's outside scenes, and its rooms: every scene whose record names its island -- two players need her and the double
        // indoors too)
        var rooms = new List<int>();
        for (var s = 0; s < store.SceneCount; s++)
        {
            if (!store.SceneExists(s)) continue;
            try { var m = store.Load(s); if (m.Island == island.IslandByte && m.CubeMode != 1) rooms.Add(s); }
            catch (Exception e) when (e is InvalidDataException or ArgumentException) { }
        }
        foreach (var scene in island.Scenes.Concat(rooms).Distinct())
        {
            if (!store.SceneExists(scene)) continue;
            SceneModel model;
            try { model = store.Load(scene); } catch (Exception e) when (e is InvalidDataException or ArgumentException) { continue; }
            if (model.Actors.Skip(1).Any(a => a.Entity == zoe.Entity && (a.Armor == CompanionArmour || a.Armor == CompanionIndoorArmour))) continue;
            // (out of doors she follows him with one player; indoors she is there for two only)
            var her = zoe.Clone();
            (her.X, her.Y, her.Z) = (model.Hero.X, model.Hero.Y, model.Hero.Z);
            her.Armor = model.CubeMode == 1 ? CompanionArmour : CompanionIndoorArmour;
            her.Life = new byte[] { 0 }; her.Track = new byte[] { 0 };
            GiveVoice(her);
            var index = SceneOps.AddActor(model, her);
            // (and the Twinsen double, for player 2's game)
            var twin = zoe.Clone();
            twin.Entity = DoubleEntityId; twin.Body = 0; twin.Anim = 0;
            (twin.X, twin.Y, twin.Z) = (model.Hero.X, model.Hero.Y, model.Hero.Z);
            twin.Armor = DoubleArmour;
            twin.Life = new byte[] { 0 }; twin.Track = new byte[] { 0 };
            var twinIndex = SceneOps.AddActor(model, twin);
            var scripts = SceneScripts.Load(SceneSerializer.Write(model), scene);
            scripts.SetText(index, ScriptKind.Life, CompanionLife(CompanionTalks.GetValueOrDefault(island.IleFile), model.CubeMode != 1));
            scripts.SetText(index, ScriptKind.Track, "label(0);\nstop();\n");
            // (out of sight by itself with one player, wherever the engine's race mode doesn't look after it)
            scripts.SetText(twinIndex, ScriptKind.Life, $"void comportement_0()\n{{\n    set_dir(NO_MOVE);\n    if (0 == var_game({TwoPlayersVar}))\n    {{\n        invisible(1);\n    }}\n}}\n");
            scripts.SetText(twinIndex, ScriptKind.Track, "label(0);\nstop();\n");
            var built = scripts.Build();
            if (!built.Ok) { log.Add($"scene {scene}: Zoe as companion left out: {string.Join("; ", built.Errors.Take(2))}"); continue; }
            changes.Add(new SceneChange(scene, SceneSerializer.Parse(SceneGame.Lba2, built.Record!), null));
        }
        if (changes.Count > 0) store.SaveMany(changes, allowErrors: true);
        log.Add($"Zoe as companion: in {changes.Count} scene(s) of {island.Name} (actor of entity {zoe.Entity}, armour {CompanionArmour} out of doors, {CompanionIndoorArmour} " +
                $"indoors for two players), from chapter 2; and the Twinsen double for two players (entity {DoubleEntityId}, armour {DoubleArmour})");
        return log;
    }

    private static string CompanionLife(string? talk, bool indoor = false) => $@"void comportement_0()
{{
    if ({CompanionWhen} || 1 == var_game({TwoPlayersVar}))
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
    if (1 == var_game({TwoPlayersVar}))
    {{
        set_dir(NO_MOVE);
    }}
    else{(indoor ? $@"
    {{
        set_dir(NO_MOVE);
        invisible(1);
    }}
    if (2 == var_game({TwoPlayersVar}))" : "")}
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
{(talk is null ? "" : $@"    swif (1 == action())
    {{
        if ({CompanionTalkReach} > distance(0) && {Driving} != comportement_hero())
        {{
{talk}
        }}
    }}
")}}}
";
}
