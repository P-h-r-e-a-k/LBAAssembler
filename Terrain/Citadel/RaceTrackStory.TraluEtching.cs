using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The darts sums carved into the Tralu caves' stone (scene 20; since 2026-11-18 the lower floor's west wall by the ladder's doorway, between
// its two barred windows -- cells x 43, z 51-52 and 57-58, the barrels under the second; the carving on the wall's face, x 44 cells, over
// z 53-56, 500 up from the floor as its windows are three layers up: the user, 2026-11-17, "it's not where I thought it was ... I was
// expecting it here. I think it's a little more obvious in this location" -- it was on the upper walkway's back wall, x 46, z 10-11):
// Action by it and Twinsen reads them out, then wonders -- the
// user, 2026-11-17: "Let's have Twinsen read this out and exclaim to himself "I wonder if this is some kind of clue"" (CitadelProps
// .TraluEtching, the carving's body). The sums are a darts player's checkouts -- the Tavern's game (RaceTrackStory.DartsTavern).
internal static partial class RaceTrackStory
{
    private const int TraluEtchScene = 20, TraluEtchX = 44 * 512 + 2, TraluEtchZ = 55 * 512, TraluEtchFloor = 1536 + 500, TraluEtchReach = 1300;
    private enum TraluEtchLine { Read, Wonder }
    private static readonly string[][] TraluEtchLines =
    {
        new[]
        {
            "\"T20, T20, T20... 180. T2-something, something-0, something... 151. T20, T20, Bull... 170.\" Half the middle line's worn away.",
            "\"T20, T20, T20... 180. T2-quelque chose, quelque chose-0, quelque chose... 151. T20, T20, Bull... 170.\" La moitié de la ligne du milieu est effacée.",
            "\"T20, T20, T20... 180. T2-irgendwas, irgendwas-0, irgendwas... 151. T20, T20, Bull... 170.\" Die mittlere Zeile ist halb verwittert.",
            "\"T20, T20, T20... 180. T2-algo, algo-0, algo... 151. T20, T20, Bull... 170.\" La mitad de la línea del medio está borrada.",
            "\"T20, T20, T20... 180. T2-qualcosa, qualcosa-0, qualcosa... 151. T20, T20, Bull... 170.\" Metà della riga di mezzo è consumata.",
            "\"T20, T20, T20... 180. T2-qualquer coisa, qualquer coisa-0, qualquer coisa... 151. T20, T20, Bull... 170.\" Metade da linha do meio está apagada.",
        },
        new[]
        {
            "I wonder if this is some kind of clue.",
            "Je me demande si c'est une sorte d'indice.",
            "Ob das wohl eine Art Hinweis ist?",
            "Me pregunto si será algún tipo de pista.",
            "Mi chiedo se sia una specie di indizio.",
            "Pergunto-me se será algum tipo de pista.",
        },
    };

    private static void TraluEtching(StoryParts p)
    {
        if (!p.Store.SceneExists(TraluEtchScene)) { p.Log.Add($"the Tralu caves' carved darts sums left out: no scene {TraluEtchScene}"); return; }
        var first = GroupB.AddLines(p.Store.Directory, CitadelTexts, TraluEtchLines, p.Log, "the Tralu caves' carving's");
        int Say(TraluEtchLine line) => first + (int)line;
        p.Edit(TraluEtchScene, $"the Tralu caves: darts sums carved in the walkway's wall between its windows, read out by Action (texts {Say(TraluEtchLine.Read)}-{Say(TraluEtchLine.Wonder)})", model =>
        {
            // (its three lines, a body each, at the one place: the first reads out on Action, the others stand still)
            var edits = new List<Action<SceneScripts>>();
            for (var k = 0; k < Citadel.CitadelProps.TraluEtchingLines; k++)
            {
                var carving = Citadel.CitadelProps.Actor(Citadel.CitadelProps.TraluEtching + k, TraluEtchX, TraluEtchFloor, TraluEtchZ, 1024);
                carving.Armor = WalkOverArmour;
                edits.Add(p.Add(model, carving, k == 0
                    ? Talker("0 <= chapter()", $"            message_obj(0, {Say(TraluEtchLine.Read)});\n            message_obj(0, {Say(TraluEtchLine.Wonder)});", TraluEtchReach)
                    : "void comportement_0()\n{\n}\n"));
            }
            return scripts => { foreach (var e in edits) e(scripts); };
        });
    }
}
