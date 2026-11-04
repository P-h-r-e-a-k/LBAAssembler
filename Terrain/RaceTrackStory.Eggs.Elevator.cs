using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The Elevator Platform's easter eggs (the user, 2026-11-04, of the proposal's picks: "Let's make all those changes" -- RaceTrackStory.Eggs.cs):
//   - "How high can you get?": one crate in ten the little monkey monsters throw at the cars is a barrel (Donkey Kong's ape threw barrels
//     down his girders). Caught -- it comes down on Twinsen's car -- it doesn't stop him: "+100" and a kash (the race-track mode's
//     guard_barrel=, this barrel in OBJFIX.HQR as Felix's crab is). Lyra, before the race: "He calls it Monkey Kong. I don't ask."
//   - Beaten by ten seconds (the race-track mode's margin_win=, EggMonster): the monster thumps his chest -- "Blue one FAST." -- and in the
//     final he is in the dock's crowd with a "BLUE ONE" sign (RaceTrackStory.Final's eggs).
// Their lines are the chapter's (ElevatorLine's last), RaceTrackStory.Elevator.cs.
internal static partial class RaceTrackStory
{
    // The barrel: oak staves bellied out, three dark hoops, its ends capped -- upright, about the monkeys' crate's size (units: its foot at
    // the origin, y up). Appended to OBJFIX.HQR; its entry.
    private const int BarrelOak = 20, BarrelOakDark = 18, BarrelHoop = 52, BarrelEnd = 22;
    public static int InstallBarrel(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var header = Body.Read(HqrArchive.Open(bodyPath).Read(RaceTrackBaldinoCar.RacerBody), 2).Header;
        var path = Path.Combine(gameDirectory, "OBJFIX.HQR");
        var index = HqrArchive.CountEntries(path);
        File.WriteAllBytes(path, HqrWriter.AppendEntry(File.ReadAllBytes(path), HqrWriter.StoredEntry(Barrel(header).Write())));
        return index;
    }

    private static Body Barrel(byte[] header)
    {
        var m = new CarMesh("the monkeys' barrel", new[] { -1 });
        const int Sides = 10;
        // (its rings, foot to top: height and radius -- a barrel's belly; the hoops are the bands between the rings marked so)
        var rings = new (float Y, float R, bool Hoop)[] { (0, 190, true), (60, 205, false), (170, 232, true), (210, 236, false), (330, 236, true), (370, 232, false), (480, 205, true), (540, 190, false) };
        int[] Ring(float y, float r) => m.Loop(0, new Vector3(0, y, 0), new Vector3(r, 0, 0), new Vector3(0, 0, r), Sides);
        var inside = new Vector3(0, 270, 0);
        var loops = rings.Select(r => Ring(r.Y, r.R)).ToArray();
        for (var k = 0; k + 1 < loops.Length; k++)
        {
            var hoop = rings[k].Hoop;
            m.Skin(loops[k], loops[k + 1], s => hoop ? BarrelHoop : s % 2 == 0 ? BarrelOak : BarrelOakDark, inside);
        }
        m.Cap(loops[^1], m.P(0, new Vector3(0, 545, 0)), BarrelEnd, inside);
        m.Cap(loops[0], m.P(0, new Vector3(0, -5, 0)), BarrelEnd, inside);
        return m.ToBody(header);
    }

    // ---- the monster's fan sign in the final (EggMonster: beaten by ten seconds on the Elevator Platform) ----------------------------
    // The monkey monster (the game's own, scene 120's actor 2) by the final's crowd with a sign (a sheet of paper, SPRITES.HQR 238, held up
    // beside him): Action -- "BLUE ONE", and his word.
    private static readonly string[][] MonsterFanLines =
    {
        new[]
        {
            "The monkey monster holds up a sign: \"BLUE ONE\". \"Hrrm. Blue one FAST. Blue one WIN.\"",
            "Le monstre singe brandit une pancarte : « BLEU ». « Hrrm. Bleu RAPIDE. Bleu GAGNE. »",
            "Das Affenmonster hält ein Schild hoch: \"BLAUER\". \"Hrrm. Blauer SCHNELL. Blauer GEWINNT.\"",
            "El monstruo mono levanta un cartel: \"AZUL\". \"Hrrm. Azul RÁPIDO. Azul GANA.\"",
            "Il mostro scimmia alza un cartello: \"BLU\". \"Hrrm. Blu VELOCE. Blu VINCE.\"",
            "O monstro macaco ergue um cartaz: \"AZUL\". \"Hrrm. Azul RÁPIDO. Azul GANHA.\"",
        },
    };
    private static List<Action<LbaScript.SceneScripts>> FinalMonsterFan(LateStory s, Scenes.SceneModel model, Func<double, double, (double X, double Z, double Y, int Beta)> at, string here)
    {
        var edits = new List<Action<LbaScript.SceneScripts>>();
        var monster = Copy(s.Originals, PlatformScene, MonsterActor);
        if (monster is null) { s.Log.Add("the monster's fan sign in the final left out: the game's monkey monster (scene 120's actor 2) not found"); return edits; }
        var first = s.Texts(FinalTexts, MonsterFanLines, "the monster's fan sign in the final (an easter egg)");
        var when = $"{here} && 1 == var_game({EggMonster})";
        var place = at(10, -3);
        Place(monster, model, place);
        edits.Add(s.Add(model, monster, Talker(when, $"            message({first});", 2500)));
        // (his sign: beside him, a little higher than his head)
        var sign = RaceTrackScenes.SpriteProp(RaceTrackScenes.CubeLocal(place.X + 0.6, model.CubeX), (int)Math.Round(place.Y) + 1800, RaceTrackScenes.CubeLocal(place.Z, model.CubeY), RaceTrackScenes.PaperSprite, 9);
        edits.Add(s.Add(model, sign, $"void comportement_0()\n{{\n    if ({when})\n    {{\n        set_comportement(comportement_1);\n    }}\n    else\n    {{\n        suicide();\n    }}\n}}\n\nvoid comportement_1()\n{{\n}}\n"));
        s.Log.Add($"the monster's fan sign in the final (an easter egg): the monkey monster and his sign by the crowd while variable {EggMonster} is 1, text {first}");
        return edits;
    }
}
