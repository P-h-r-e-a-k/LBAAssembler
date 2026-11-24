namespace LBAAssembler.Terrain;

// A story race lost (the user, 2026-11-24: "I just failed to win the citadel 2 race but was still awarded the cup for winning, if Twinsen
// doesn't win let's have him told by Mr. Paul that there were some irregularities during the race and it's going to be re-run later, after
// Twinsen drives to a different scene and comes back let's have the race start again. We should do this every time Twinsen doesn't win to
// allow him to eventually win it. Every time he doesn't win let's remove a car from the grid as "disqualified", if all of his opponents
// get disqualified then Twinsen should win the race by default. Let's implement this for all tracks Twinsen races"). The race file's
// rerun= and rerun_text= (RACEMOD.CPP RerunLost, RerunSay): a story race is won in first place only; lost, the car that came in first is
// disqualified -- a bit of the race's own game variable, saved -- and the official says so: Mr. Paul on Citadel Island's town circuit
// (his entity, 38: the nearest one in the scene), the flag marshal everywhere else. The race runs again once Twinsen has left the scene and
// come back; every car disqualified, it is his by default. Not the storm track (Raph's time to beat) nor Polar Island's dream (a dream).
internal static partial class RaceTrackStory
{
    // the lines, in every island's bank at fixed ids past the openers' (OpenerTexts puts them in with them)
    public const int RerunBase = OpenerBase + 900;
    private const int PaulEntity = 38;

    internal enum RerunLine { PaulIrregular, PaulDefault, FlickIrregular, FlickDefault }

    // English, French, German, Spanish, Italian, Portuguese -- Mr. Paul and the flag marshal call Twinsen tu, as the island's people do
    internal static readonly string[][] RerunLines =
    {
        new[]
        {
            "Hold on, Twinsen! There were some irregularities during that race. The winner's been disqualified, and the race will be run again later. Come back to the start line when you're ready.",
            "Attends, Twinsen ! Il y a eu des irrégularités pendant cette course. Le vainqueur a été disqualifié, et la course sera courue à nouveau plus tard. Reviens à la ligne de départ quand tu seras prêt.",
            "Moment, Twinsen! Bei diesem Rennen gab es einige Unregelmäßigkeiten. Der Sieger wurde disqualifiziert, und das Rennen wird später wiederholt. Komm zur Startlinie zurück, wenn du so weit bist.",
            "¡Espera, Twinsen! Ha habido algunas irregularidades en esta carrera. El ganador ha sido descalificado y la carrera se volverá a correr más tarde. Vuelve a la línea de salida cuando estés listo.",
            "Aspetta, Twinsen! Ci sono state delle irregolarità in questa gara. Il vincitore è stato squalificato e la gara verrà ripetuta più tardi. Torna alla linea di partenza quando sei pronto.",
            "Espera, Twinsen! Houve algumas irregularidades nesta corrida. O vencedor foi desclassificado e a corrida vai ser repetida mais tarde. Volta à linha de partida quando estiveres pronto.",
        },
        new[]
        {
            "Twinsen! More irregularities, I'm afraid -- and that's every other driver disqualified now. The race is yours by default. Congratulations!",
            "Twinsen ! Encore des irrégularités, j'en ai peur -- et voilà tous les autres pilotes disqualifiés. La course est à toi par forfait. Félicitations !",
            "Twinsen! Leider wieder Unregelmäßigkeiten -- damit sind jetzt alle anderen Fahrer disqualifiziert. Das Rennen geht kampflos an dich. Herzlichen Glückwunsch!",
            "¡Twinsen! Me temo que ha habido más irregularidades, y ya están descalificados todos los demás pilotos. La carrera es tuya por incomparecencia. ¡Enhorabuena!",
            "Twinsen! Altre irregolarità, temo -- e ora tutti gli altri piloti sono squalificati. La gara è tua a tavolino. Congratulazioni!",
            "Twinsen! Mais irregularidades, receio -- e agora todos os outros pilotos estão desclassificados. A corrida é tua por falta de adversários. Parabéns!",
        },
        new[]
        {
            "Twinsen, wait! The stewards found irregularities in that race. The winning car is disqualified, and the race will be re-run later. Come back to the line when you're ready.",
            "Twinsen, attends ! Les commissaires ont relevé des irrégularités dans cette course. La voiture gagnante est disqualifiée, et la course sera recourue plus tard. Reviens sur la ligne quand tu seras prêt.",
            "Twinsen, warte! Die Rennkommissare haben bei diesem Rennen Unregelmäßigkeiten festgestellt. Der Siegerwagen ist disqualifiziert, und das Rennen wird später neu gestartet. Komm zur Linie zurück, wenn du so weit bist.",
            "¡Twinsen, espera! Los comisarios han encontrado irregularidades en esta carrera. El coche ganador queda descalificado y la carrera se repetirá más tarde. Vuelve a la línea cuando estés listo.",
            "Twinsen, aspetta! I commissari hanno trovato delle irregolarità in questa gara. L'auto vincitrice è squalificata e la gara sarà ripetuta più tardi. Torna sulla linea quando sei pronto.",
            "Twinsen, espera! Os comissários encontraram irregularidades nesta corrida. O carro vencedor foi desclassificado e a corrida vai ser repetida mais tarde. Volta à linha quando estiveres pronto.",
        },
        new[]
        {
            "Twinsen! The stewards found irregularities again, and that's every other car disqualified now. You win the race by default!",
            "Twinsen ! Les commissaires ont encore relevé des irrégularités, et voilà toutes les autres voitures disqualifiées. Tu gagnes la course par forfait !",
            "Twinsen! Die Rennkommissare haben wieder Unregelmäßigkeiten festgestellt, damit sind jetzt alle anderen Wagen disqualifiziert. Du gewinnst das Rennen kampflos!",
            "¡Twinsen! Los comisarios han vuelto a encontrar irregularidades, y ya están descalificados todos los demás coches. ¡Ganas la carrera por incomparecencia!",
            "Twinsen! I commissari hanno trovato di nuovo delle irregolarità, e ora tutte le altre auto sono squalificate. Vinci la gara a tavolino!",
            "Twinsen! Os comissários voltaram a encontrar irregularidades, e agora todos os outros carros estão desclassificados. Ganhas a corrida por falta de adversários!",
        },
    };

    // Story Mode's Play Demo (the user, 2026-11-24: "Let's push a demo build, that is only Citadel Island, we can gate this by preventing
    // people being able to buy a ferry ticket. Let's keep the one install, but add a 'Play Demo' to the 'Story Mode' menu"): the game
    // variable the demo is marked by (RACEMOD.CPP demo_var=, set as the demo starts; kept in its save), and the ferry office's word in
    // its place of the ticket -- Citadel Island's bank alone, past the officials' (OpenerTexts). The Dino-Fly doesn't fly in it either
    // (DinoFlyFlights).
    public const int DemoVar = 293, DemoFerryText = RerunBase + 10;
    internal static readonly string[] DemoFerryLine =
    {
        "I'm terribly sorry, Mr. Twinsen, every crossing to White Leaf Desert is fully booked. That's as far as the demo goes -- thank you for playing!",
        "Je suis navré, monsieur Twinsen, toutes les traversées pour le Désert de la Feuille Blanche sont complètes. La démo s'arrête ici -- merci d'avoir joué !",
        "Es tut mir schrecklich leid, Herr Twinsen, alle Überfahrten zur Weißblattwüste sind ausgebucht. Hier endet die Demo -- danke fürs Spielen!",
        "Lo siento muchísimo, señor Twinsen, todas las travesías al Desierto de la Hoja Blanca están completas. La demo termina aquí: ¡gracias por jugar!",
        "Mi dispiace tanto, signor Twinsen, tutte le traversate per il Deserto della Foglia Bianca sono al completo. La demo finisce qui: grazie per aver giocato!",
        "Lamento imenso, senhor Twinsen, todas as travessias para o Deserto da Folha Branca estão esgotadas. A demo termina aqui -- obrigado por jogar!",
    };

    // Each story race's disqualified cars: a game variable of its own (bit n: the race file's opponent n + 1). 280-292, spare until now
    // (reference_game_variables_wide: next free 280).
    internal static int? RerunVar(RaceTrackIsland island, bool town, bool lava) =>
        lava || island == RaceTrackIsland.CelebrationLava ? 285
        : town ? 280
        : island == RaceTrackIsland.Desert ? 281
        : island == RaceTrackIsland.Emerald ? 282
        : island == RaceTrackIsland.Otringal ? 283
        : island == RaceTrackIsland.Knartas ? 284
        : island == RaceTrackIsland.Celebration ? 286
        : island == RaceTrackIsland.Elevator ? 287
        : island == RaceTrackIsland.Wannies ? 288
        : island == RaceTrackIsland.Mosquibe ? 289
        : island == RaceTrackIsland.Volcano ? 290
        : island == RaceTrackIsland.CxTower || island == RaceTrackIsland.CxTowerMk2 ? 291
        : island == RaceTrackIsland.Palace ? 292
        : null;

    // The race file's keys for a story race against cars (RaceCarEngineFile): its variable, the official (Mr. Paul's entity on the town
    // circuit; none -- the flag marshal -- elsewhere) and the two lines
    internal static string RerunKeys(RaceTrackIsland island, bool town, bool lava)
    {
        if (RerunVar(island, town, lava) is not int var) return "";
        var paul = town;
        var irregular = RerunBase + (int)(paul ? RerunLine.PaulIrregular : RerunLine.FlickIrregular);
        var byDefault = RerunBase + (int)(paul ? RerunLine.PaulDefault : RerunLine.FlickDefault);
        return $"# a story race lost: the winner disqualified (variable {var}), told by {(paul ? "Mr. Paul" : "the flag marshal")}, the race again later; every car disqualified, won by default\n" +
               $"rerun={var}{(paul ? $" {PaulEntity}" : "")}\nrerun_text={irregular} {byDefault}\n";
    }
}
