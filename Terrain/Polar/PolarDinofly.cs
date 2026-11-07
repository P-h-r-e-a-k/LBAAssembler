using System.IO;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain.Polar;

// The Dino-Fly on the Polar dream's island (the user, 2026-11-03: "For the Polar dream let's add Dinofly in, in the same location as he'd be
// in LBA1. If Twinsen talks to him, let's have him ask twinsen "Where do you want to go?" The same as the line is for Fortress Island in
// LBA1, and give Twinsen the list of options Brundle Island, Tippet Island, and if Twinsen selects either dinofly should respond "This is not
// the game for that Twinsen""; 2026-11-04: "In LBA1 Fortress island connects to polar island, we've ported both in already combined into
// our current polar island"). LBA1 has him on Fortress Island's docks (scene 100, PolarLayout.FortressDocks: actor 2, landed by his
// track on the jetty -- Lba1X); here the game's own Dino-Fly (entity 109, his body without Twinsen on his back) stands there, on the
// jetty's planks (their decor's top), facing as LBA1's does.
// Spoken to: "Where do you want to go?", Brundle Island or Tippet Island, and whichever -- "This is not the game for that, Twinsen."
internal static class PolarDinofly
{
    public const int Entity = 109, Body = 37;
    // LBA1's place for him: Fortress Island's docks (scene 100), in LBA1's world units (PolarScenes.OnIsland). LBA1's actor 2 starts out
    // over the open sea at cell (17, layer 4, 36.5), his body hovering; his track flies him from there to its point 1 and lands him (its
    // label 101: his standing animation, FALLABLE on), so where the player meets him is point 1, (21184, 512, 15168): on the jetty's
    // planks (LBA1's block 142, layer 1), a cell from its end, facing the way he flew in. (c171ac9 took the actor's own start, over the
    // water, and stood him on the land nearest it: well off -- the user, 2026-11-06, "put him exactly where LBA1 has him".)
    public const int Lba1X = 21184, Lba1Y = 512, Lba1Z = 15168;
    private const int Lba1FromX = 17 * 512, Lba1FromZ = 18704;
    public static int Beta => (int)Math.Round(Math.Atan2(Lba1X - Lba1FromX, Lba1Z - Lba1FromZ) * 4096 / (2 * Math.PI)) & 4095;
    public const int AskText = 26, BrundleText = 27, TippetText = 28, NotThisGameText = 29;

    private static readonly string[] Ask =
    {
        "Where do you want to go?", "Où veux-tu aller ?", "Wohin willst du?", "¿Adónde quieres ir?", "Dove vuoi andare?", "Para onde queres ir?",
    };
    private static readonly string[] Brundle =
    {
        "Brundle Island", "Île de Brundle", "Brundle-Insel", "Isla Brundle", "Isola di Brundle", "Ilha de Brundle",
    };
    private static readonly string[] Tippet =
    {
        "Tippet Island", "Île de Tippet", "Tippet-Insel", "Isla Tippet", "Isola di Tippet", "Ilha de Tippet",
    };
    private static readonly string[] NotThisGame =
    {
        "This is not the game for that, Twinsen.",
        "Ce n'est pas le jeu pour ça, Twinsen.",
        "Das ist nicht das Spiel dafür, Twinsen.",
        "Este no es el juego para eso, Twinsen.",
        "Questo non è il gioco giusto, Twinsen.",
        "Este não é o jogo para isso, Twinsen.",
    };

    public static IEnumerable<(int Id, string Text)> IslandTexts(int language)
    {
        var words = language < Ask.Length ? language : 0;
        yield return (AskText, Ask[words]);
        yield return (BrundleText, Brundle[words]);
        yield return (TippetText, Tippet[words]);
        yield return (NotThisGameText, NotThisGame[words]);
    }

    // He stands at (x, y, z) in `model`, facing `beta`, his script compiled
    public static void AddActor(SceneModel model, int x, int y, int z, int beta)
    {
        var actor = SceneOps.BlankActor(model.Game, x, y, z, Entity);
        actor.Body = Body;
        actor.Beta = beta;
        SceneOps.AddActor(model, actor);
        var index = model.Actors.Count - 1;
        var life = $@"void comportement_0()
{{
    if (1 == action() && 1500 > distance(0))
    {{
        add_choice({BrundleText});
        add_choice({TippetText});
        ask_choice_obj({index}, {AskText});
        message_obj({index}, {NotThisGameText});
    }}
}}
";
        var scratch = model.Clone();
        scratch.Actors[index].Life = new byte[] { 0 };
        var scripts = SceneScripts.Load(SceneSerializer.Write(scratch), -1);
        scripts.SetText(index, ScriptKind.Life, life);
        var built = scripts.Build();
        if (!built.Ok) throw new InvalidDataException("The Dino-Fly's script didn't compile: " + string.Join("; ", built.Errors));
        actor.Life = SceneSerializer.Parse(model.Game, built.Record!).Actors[index].Life;
    }
}
