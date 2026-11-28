using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// A racer towed out of the sand (the user, 2026-11-27: "I like the idea of a racer getting stuck in the sand on Desert island and us
// having to tow them out, the problem with this may be that there's not a lot of spare space left on the island"; then "yes add towing to
// tomorrows list with the trackside sand trap as the plan"). Chapter 3, before the race: one of the Desert grid's entrants -- the Hamalayi
// rabbibunny, LBA1's snow racer, in his giant carrot (RaceTrackCharacterCars.Lba1Entrants) -- braked late at the Temple bend, as Baldino
// says the champion does, and sits beached in the sand beside it: the lap's own corner, no new ground, on the far side of the road from
// the Temple (Bù's stone is on the Temple's). Flick won't drop the flag while a car is stuck on the course (the race file's fourth gate,
// QTow). On foot, Action: he asks for a tow. Twinsen in his car pulls up beside him: hooked on, pulled out onto the road, his thanks, and
// away along the lap towards the grid -- no tow physics, no rope. Gone from the bend from then on; on the grid as ever.
// (No cut-scene bars: the engine plays no cinema while Twinsen is in his car on a track with a start line -- RACEMOD.CPP RaceMod_NoCinema --
// and the lines, said while he sits in his car, hold the game as Mr. Paul's at the car ferry do.)
internal static partial class RaceTrackStory
{
    // (304: free -- after the hair dryer's 301 and 302 and the engine's RACE_ON_VAR 303)
    internal const int QTow = 304;
    private const int TowAsked = 1, Towed = 2;
    // how near Twinsen comes in his car for the tow (world units from the stuck car's middle: about two car lengths) and on foot to talk
    private const int TowReach = 2200, TowTalkReach = 1600;
    private const int TowOutSpeed = 700, TowAwaySpeed = 3000;
    // (cells: how far to the side of the lap's middle the sand is looked for -- the road and its verge are about 6 across each side -- and
    // how far along the lap he drives off, within the scene's cube)
    private const double TowSide = 8, TowReachCells = 5, TowRun = 16;
    private const string TowDriver = "The Hamalayi rabbibunny";
    private const int TowRoadLabel = 1, TowOnRoadLabel = 2, TowAwayLabel = 3, TowGoneLabel = 4;

    private enum TowLine { Stuck, Ask, Hooked, Out }
    private static readonly string[][] TowLines =
    {
        new[]
        {
            "Snow, I can drive on. Ice, I can drive on. Sand? Nobody told me about sand! I braked late at the Temple bend, like the champion does, and look at me now.",
            "La neige, je sais y rouler. La glace, je sais y rouler. Le sable ? Personne ne m'a parlé du sable ! J'ai freiné tard au virage du Temple, comme le champion, et regardez-moi maintenant.",
            "Auf Schnee kann ich fahren. Auf Eis kann ich fahren. Sand? Von Sand hat mir keiner was gesagt! Ich habe in der Tempelkurve spät gebremst, wie der Champion, und jetzt sehen Sie mich an.",
            "En la nieve sé conducir. En el hielo sé conducir. ¿En la arena? ¡Nadie me habló de la arena! Frené tarde en la curva del Templo, como el campeón, y míreme ahora.",
            "Sulla neve so guidare. Sul ghiaccio so guidare. Sulla sabbia? Nessuno mi ha parlato della sabbia! Ho frenato tardi alla curva del Tempio, come il campione, e guardi adesso.",
            "Na neve sei conduzir. No gelo sei conduzir. Na areia? Ninguém me falou da areia! Travei tarde na curva do Templo, como o campeão, e veja agora.",
        },
        new[]
        {
            "Flick won't start the race with a car stuck in his sand trap. Bring your car round and tow me out, would you?",
            "Flick ne donnera pas le départ avec une voiture coincée dans son bac à sable. Amenez votre voiture et sortez-moi de là, vous voulez bien ?",
            "Flick startet das Rennen nicht, solange ein Wagen in seinem Sandbett feststeckt. Holen Sie Ihren Wagen und schleppen Sie mich raus, ja?",
            "Flick no dará la salida con un coche atascado en su trampa de arena. Traiga su coche y sáqueme de aquí, ¿quiere?",
            "Flick non darà il via con una macchina bloccata nella sua trappola di sabbia. Porti qui la sua macchina e mi tiri fuori, per favore.",
            "O Flick não dá a partida com um carro preso na caixa de areia. Traga o seu carro e reboque-me daqui, pode ser?",
        },
        new[]
        {
            "A tow! Rope on... Gently now... Gently...",
            "Un remorquage ! La corde est attachée... Doucement... Doucement...",
            "Abschleppen! Das Seil ist dran... Ganz sachte... Ganz sachte...",
            "¡Un remolque! La cuerda, puesta... Despacio... Despacio...",
            "Un traino! La corda è legata... Piano... Piano...",
            "Um reboque! A corda está presa... Devagar... Devagar...",
        },
        new[]
        {
            "Out! Thanks, Twinsen. See you on the grid. And from now on I brake early at this bend.",
            "Sorti ! Merci, Twinsen. On se voit sur la grille. Et désormais, je freine tôt dans ce virage.",
            "Draußen! Danke, Twinsen. Wir sehen uns in der Startaufstellung. Und ab jetzt bremse ich in dieser Kurve früh.",
            "¡Fuera! Gracias, Twinsen. Nos vemos en la parrilla. Y a partir de ahora freno pronto en esta curva.",
            "Fuori! Grazie, Twinsen. Ci vediamo sulla griglia. E d'ora in poi a questa curva frenerò presto.",
            "Saí! Obrigado, Twinsen. Vemo-nos na grelha. E a partir de agora travo cedo nesta curva.",
        },
    };

    // The race file's gate: no race while he's stuck (RaceKeysA)
    private static string TowGate => $"gate={QTow} {Towed} Flick: a car's stuck in the sand at the Temple bend! Tow it out first.\n";

    // The stuck car's life: there in chapter 3 until towed; Action on foot, his ask; Twinsen in his car beside him, the tow
    private static string TowLife(Func<TowLine, int> say) => $@"void comportement_0()
{{
    if (3 == chapter() && {Towed} != var_game({QTow}))
    {{
        set_comportement(comportement_1);
    }}
    else
    {{
        invisible(1);
        obj_col(0);
        suicide();
    }}
}}

void comportement_1()
{{
    if ({Driving} == comportement_hero() && {TowReach} > distance(0))
    {{
        message({say(TowLine.Hooked)});
        set_var_game({QTow}, {Towed});
        set_track(label_{TowRoadLabel});
        set_comportement(comportement_2);
    }}
    swif (1 == action())
    {{
        if ({TowTalkReach} > distance(0) && {Driving} != comportement_hero())
        {{
            if (0 == var_game({QTow}))
            {{
                message({say(TowLine.Stuck)});
                set_var_game({QTow}, {TowAsked});
            }}
            message({say(TowLine.Ask)});
        }}
    }}
}}

void comportement_2()
{{
    if ({TowOnRoadLabel} == l_track())
    {{
        message({say(TowLine.Out)});
        set_track(label_{TowAwayLabel});
        set_comportement(comportement_3);
    }}
}}

void comportement_3()
{{
    if ({TowGoneLabel} == l_track())
    {{
        invisible(1);
        obj_col(0);
        suicide();
    }}
}}
";

    // Into the Desert's scenes (`b`); `first`: the lines' first text
    private static void DesertTow(IslandBeatsA b, RaceTrackReport report, int first)
    {
        int Say(TowLine l) => first + (int)l;
        var n = report.LapX.Length;
        if (n < 8 || report.LapY.Length != n) { b.Log.Add("the tow left out: no lap"); return; }
        var entrant = RaceTrackCharacterCars.Lba1Entrants.FirstOrDefault(e => e.Short == TowDriver);
        if (entrant is null) { b.Log.Add($"the tow left out: no {TowDriver} among LBA1's entrants"); return; }
        // the Temple bend: where the lap passes nearest the Temple of Bù's door (as Bù's stone finds it)
        (double X, double Z)? door = null;
        foreach (var s in b.OutsideScenes)
        {
            var m = b.Store.Load(s);
            if (m.Zones.FirstOrDefault(zn => zn.Type == 0 && zn.Num == TempleOfBu) is { } zone)
            { door = (m.CubeX * 64 + (zone.X0 + zone.X1) / 1024.0, m.CubeY * 64 + (zone.Z0 + zone.Z1) / 1024.0); break; }
        }
        if (door is not { } d) { b.Log.Add($"the tow left out: no door into the Temple of Bù (scene {TempleOfBu})"); return; }
        var near = Enumerable.Range(0, n).MinBy(i => Math.Pow(report.LapX[i] - d.X, 2) + Math.Pow(report.LapZ[i] - d.Z, 2));
        double lx = report.LapX[near], lz = report.LapZ[near], ly = report.LapY[near];
        double ax = report.LapX[(near + 1) % n] - report.LapX[(near + n - 1) % n], az = report.LapZ[(near + 1) % n] - report.LapZ[(near + n - 1) % n];
        var len = Math.Sqrt(ax * ax + az * az);
        if (len < 1e-6) { b.Log.Add("the tow left out: the lap stands still at the Temple bend"); return; }
        ax /= len; az /= len;
        // (the side of the road away from the Temple's door: its normal (-az, ax) or the other way)
        double nx = -az, nz = ax;
        if ((d.X - lx) * nx + (d.Z - lz) * nz > 0) { nx = -nx; nz = -nz; }
        var cube = ((int)Math.Floor(lx / 64), (int)Math.Floor(lz / 64));
        bool SameCube(double x, double z) => ((int)Math.Floor(x / 64), (int)Math.Floor(z / 64)) == cube;
        if (GroupB.SpotAt(b.Store, b.Island, report, lx + nx * TowSide, lz + nz * TowSide, ly, lx, lz, 1, TowReachCells, SameCube) is not { } spot)
        { b.Log.Add($"the tow left out: no sand off the road by the Temple bend (cell {lx:0.#}, {lz:0.#})"); return; }
        // his way off: along the lap from the bend, as far as TowRun cells or the cube's edge
        int away = near;
        for (double run = 0; run < TowRun; )
        {
            var next = (away + 1) % n;
            if (!SameCube(report.LapX[next], report.LapZ[next])) break;
            run += Math.Sqrt(Math.Pow(report.LapX[next] - report.LapX[away], 2) + Math.Pow(report.LapZ[next] - report.LapZ[away], 2));
            away = next;
            if (away == near) break;
        }
        var (scene, at) = spot;
        b.Edit(scene, $"the tow: {TowDriver}'s car beached in the sand by the Temple bend at cell ({at.X:0.#}, {at.Z:0.#}), {Math.Sqrt(Math.Pow(at.X - lx, 2) + Math.Pow(at.Z - lz, 2)):0.#} cells off the lap's middle, chapter 3 until towed (variable {QTow}; texts {first}-{first + TowLines.Length - 1})", model =>
        {
            int Local(double cells, int cubeAt) => Math.Clamp(RaceTrackScenes.CubeLocal(cells, cubeAt), 0, IslandFile.CubeSize - 1);
            var x = Local(at.X, model.CubeX); var z = Local(at.Z, model.CubeY);
            var car = SceneOps.BlankActor(SceneGame.Lba2, x, (int)Math.Round(at.Y), z, RaceTrackBaldinoCar.RacerEntity);
            car.Body = entrant.Generic;
            car.Beta = at.Beta;   // (spun round: his nose to the road)
            car.Flags = 0x1000u | RaceTrackScenes.CarrierFlag; car.Move = 3; car.LifePoints = -1; car.Armor = WalkOverArmour;
            car.HitForce = 0;
            var from = SceneOps.AddTrackPoint(model, new SceneTrackPoint(x, car.Y, z));
            var road = SceneOps.AddTrackPoint(model, new SceneTrackPoint(Local(lx, model.CubeX), (int)Math.Round(ly), Local(lz, model.CubeY)));
            var off = SceneOps.AddTrackPoint(model, new SceneTrackPoint(Local(report.LapX[away], model.CubeX), (int)Math.Round(report.LapY[away]), Local(report.LapZ[away], model.CubeY)));
            var index = model.Actors.Count;
            var add = IslandBeatsA.Add(model, car, TowLife(Say));
            return scripts =>
            {
                add(scripts);
                scripts.SetText(index, ScriptKind.Track, $@"label(0);
pos_point({from});
stop();

label({TowRoadLabel});
speed({TowOutSpeed});
goto_point_3d({road});

label({TowOnRoadLabel});
stop();

label({TowAwayLabel});
speed({TowAwaySpeed});
goto_point_3d({off});

label({TowGoneLabel});
stop();
");
            };
        });
    }
}
