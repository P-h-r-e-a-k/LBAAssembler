using LBAAssembler.Assets;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Cup's racing licence (the user, 2026-11-14: "a racing licence that Twinsen has to show to enter the temple of Bu area after the race
// with his car then script the car progression the rest of the way. Let's add the racing licence as a prize for the winner of the
// Citadel 2 track").
//   - The item: inventory slot 38, the game's burgomaster's note (Zeelich's, which the story never reaches): its found message, name and
//     description (file 2's 38, 138, 238) the licence's, each of the game's moved aside (Retired), as the exam pages' were; its model the
//     note's own paper; used from the inventory, the engine shows file 2's text 800 (PERSO.CPP FLAG_NOTE_BOURGMESTRE) -- the licence card.
//   - The prize: the town circuit won (chapter 2), Mr. Paul hands it over with the ferry ticket (RaceTrackStory.cs, the prize's helper).
//   - The Temple of Bú's gate (the Desert's start scene, where its door down to the Esmers' base is): the Desert won (chapter 4), an Esmer
//     at the roadside by the temple checks it -- Twinsen in his car with the licence: waved on, and the car driven by itself along the road
//     to the temple (DriveMarks markers: RACEMOD.CPP drive_marks=), stopped there, Twinsen out, and down through the temple to the hangar
//     (scene 12: the freighter -- RaceTrackStory.Hangar.cs).
internal static partial class RaceTrackStory
{
    public const int LicenceSlot = 38, LicenceCard = 800;
    // drive_marks='s variable: the first marker's index while the drive is asked for, 250 under way, 251 Twinsen out at the temple, 252 done
    public const int DriveMarksVar = 248, DriveMarks = 3, MarksDriving = 250, MarksArrived = 251, MarksDone = 252;
    // the gate's cube variables: what the guard has said (a car wanted, no licence) so he says it once a visit
    private const int GateSaidCar = 43, GateSaidNoLicence = 44;
    // the door down into the temple in the Desert's start scene (its zone's scene), and how far before it along the road the guard and
    // the markers are (cells)
    private const int TempleScene = 10;
    private const double GuardBefore = 13, GuardOut = 4;
    private static readonly double[] MarksBefore = { 9, 5, 1 };

    // found, name, description, the card -- in English, French, German, Spanish, Italian, Portuguese
    private static readonly string[][] LicenceItemTexts =
    {
        new[] { "You've got the Cup's racing licence!", "Racing licence", "The Cup's racing licence, made out to Twinsen. The race gates open for whoever holds it.",
                "CUP RACING LICENCE@Holder: Twinsen, of Citadel Island.@Licensed to race in the Cup and to drive through its gates.@Signed: Mr. Paul, for the Cup." },
        new[] { "Tu as obtenu la licence de course de la Coupe !", "Licence de course", "La licence de course de la Coupe, au nom de Twinsen. Les barrières de course s'ouvrent pour qui la porte.",
                "LICENCE DE COURSE DE LA COUPE@Titulaire : Twinsen, de l'île de la Citadelle.@Autorisé à courir dans la Coupe et à passer ses barrières.@Signé : M. Paul, pour la Coupe." },
        new[] { "Du hast die Rennlizenz des Pokals bekommen!", "Rennlizenz", "Die Rennlizenz des Pokals, auf Twinsen ausgestellt. Die Renntore öffnen sich für ihren Inhaber.",
                "RENNLIZENZ DES POKALS@Inhaber: Twinsen, von der Zitadelleninsel.@Berechtigt, im Pokal zu fahren und seine Tore zu passieren.@Gezeichnet: Herr Paul, für den Pokal." },
        new[] { "¡Has conseguido la licencia de carreras de la Copa!", "Licencia de carreras", "La licencia de carreras de la Copa, a nombre de Twinsen. Las barreras de carrera se abren para quien la lleve.",
                "LICENCIA DE CARRERAS DE LA COPA@Titular: Twinsen, de la isla de la Ciudadela.@Autorizado a correr en la Copa y a cruzar sus barreras.@Firmado: Sr. Paul, por la Copa." },
        new[] { "Hai ottenuto la licenza di corsa della Coppa!", "Licenza di corsa", "La licenza di corsa della Coppa, intestata a Twinsen. I cancelli di gara si aprono per chi la possiede.",
                "LICENZA DI CORSA DELLA COPPA@Titolare: Twinsen, dell'isola della Cittadella.@Autorizzato a correre nella Coppa e a passarne i cancelli.@Firmato: il signor Paul, per la Coppa." },
        new[] { "Conseguiste a licença de corrida da Taça!", "Licença de corrida", "A licença de corrida da Taça, em nome de Twinsen. As barreiras de corrida abrem-se para quem a tiver.",
                "LICENÇA DE CORRIDA DA TAÇA@Titular: Twinsen, da ilha da Cidadela.@Autorizado a correr na Taça e a passar as suas barreiras.@Assinado: Sr. Paul, pela Taça." },
    };

    // The licence's texts into file 2's bank (one language's): each of the game's moved aside, the licence's in its place
    private static void LicenceTexts(Lba2TextBank bank, int words)
    {
        var ids = new[] { LicenceSlot, 100 + LicenceSlot, 200 + LicenceSlot, LicenceCard };
        for (var k = 0; k < ids.Length; k++)
        {
            var own = bank.Find(ids[k]);
            var attribute = own?.Attribute ?? Lba2TextBank.NormalAttribute;
            if (own is not null) own.Id = Retired + ids[k];
            var words2 = LicenceItemTexts[words < LicenceItemTexts.Length ? words : 0][k];
            bank.Texts.Add(new Lba2TextBank.Text { Id = ids[k], Attribute = attribute, Bytes = Dos.GetBytes(words2) });
        }
    }

    // The Temple of Bú's gate in the Desert's start scene: the guard by the road before the temple, the markers on the road to it
    private static void TempleGate(IslandBeatsA b, RaceTrackReport report, int startScene, Func<DesertLine, int> say)
    {
        var start = b.Store.Load(startScene);
        var door = start.Zones.FirstOrDefault(z => z.Type == 0 && z.Num == TempleScene);
        var road = report.Roads.FirstOrDefault(r => r.Closed) ?? report.Roads.FirstOrDefault();
        if (door is null || road is null || road.X.Length < 4) { b.Log.Add($"the Temple of Bú's gate left out: no door into scene {TempleScene} in scene {startScene}, or no road"); return; }
        // (the door's place, island cells; the road's nearest point to it in the same cube)
        double doorX = start.CubeX * 64 + (door.X0 + door.X1) / 2.0 / 512, doorZ = start.CubeY * 64 + (door.Z0 + door.Z1) / 2.0 / 512;
        bool InCube(double x, double z) => (int)Math.Floor(x / 64) == start.CubeX && (int)Math.Floor(z / 64) == start.CubeY;
        var near = Enumerable.Range(0, road.X.Length).Where(i => InCube(road.X[i], road.Z[i]))
            .OrderBy(i => (road.X[i] - doorX) * (road.X[i] - doorX) + (road.Z[i] - doorZ) * (road.Z[i] - doorZ)).DefaultIfEmpty(-1).First();
        if (near < 0) { b.Log.Add("the Temple of Bú's gate left out: the road doesn't pass through the temple's cube"); return; }
        // (a point the given way back along the road from the nearest, by its length)
        (double X, double Z, int I) Back(double cells)
        {
            var i = near;
            while (i > 0 && road.S[near] - road.S[i] < cells) i--;
            return (road.X[i], road.Z[i], i);
        }
        var marks = MarksBefore.Select(Back).ToList();
        var g = Back(GuardBefore);
        if (!InCube(g.X, g.Z) || marks.Any(m => !InCube(m.X, m.Z))) { b.Log.Add("the Temple of Bú's gate left out: its road runs out of the temple's cube"); return; }
        // the guard off the road on the temple's side
        double tx = road.Tx[g.I], tz = road.Tz[g.I];
        double nx = -tz, nz = tx;
        if (nx * (doorX - g.X) + nz * (doorZ - g.Z) < 0) { nx = -nx; nz = -nz; }
        var guardAt = (X: g.X + nx * GuardOut, Z: g.Z + nz * GuardOut);
        var officer = Copy(b.Originals, MoonBase, BaseMechanic);
        if (officer is null) { b.Log.Add("the Temple of Bú's gate left out: no Esmer mechanic in the moon base (scene 23) to copy"); return; }
        var y = (int)Math.Round(report.StartLine[0].Y);
        int Local(double cells, int cube) => Math.Clamp(RaceTrackScenes.CubeLocal(cells, cube), 0, IslandFile.CubeSize - 1);
        b.Edit(startScene, $"the Temple of Bú's gate: an Esmer checks the racing licence, the car driven to the temple (cells {guardAt.X:0.#}, {guardAt.Z:0.#}), down to the hangar (chapter 4)", model =>
        {
            var adds = new List<Action<LbaScript.SceneScripts>>();
            var firstMark = model.Actors.Count;
            foreach (var m in marks)
                adds.Add(IslandBeatsA.Add(model, RaceTrackScenes.InvisibleHelper(Local(m.X, model.CubeX), y, Local(m.Z, model.CubeY)), "void comportement_0()\n{\n}\n"));
            var guard = officer.Clone();
            (guard.X, guard.Y, guard.Z) = (Local(guardAt.X, model.CubeX), y, Local(guardAt.Z, model.CubeY));
            guard.Beta = RaceTrackMath.Turn(Math.Atan2(-nx, -nz));
            guard.Move = 0;
            var gi = model.Actors.Count;
            adds.Add(IslandBeatsA.Add(model, guard, $@"void comportement_0()
{{
    if (4 == chapter() && {PagesDone} == var_game({QPages}) && 0 == var_game({DriveMarksVar}) && 2600 > distance(0))
    {{
        if ({Driving} == comportement_hero())
        {{
            if (0 < var_game({LicenceSlot}))
            {{
                message_obj({gi}, {say(DesertLine.GateLicence)});
                set_var_game({DriveMarksVar}, {firstMark});
                set_comportement(comportement_1);
            }}
            else
            {{
                if (0 == var_cube({GateSaidNoLicence}))
                {{
                    message_obj({gi}, {say(DesertLine.GateNoLicence)});
                    set_var_cube({GateSaidNoLicence}, 1);
                }}
            }}
        }}
        else
        {{
            if (0 == var_cube({GateSaidCar}))
            {{
                message_obj({gi}, {say(DesertLine.GateInCar)});
                set_var_cube({GateSaidCar}, 1);
            }}
        }}
    }}
}}

void comportement_1()
{{
    if ({MarksArrived} == var_game({DriveMarksVar}) && {Driving} != comportement_hero())
    {{
        set_var_game({DriveMarksVar}, {MarksDone});
        message({say(DesertLine.TempleThrough)});
        change_cube({HangarScene});
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
}}
"));
            return scripts => { foreach (var add in adds) add(scripts); };
        });
    }
}
