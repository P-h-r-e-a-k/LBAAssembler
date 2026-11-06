using System.IO;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Polar;

// The grobo who gives Twinsen the digger's ignition key (the user, 2026-10-27: "We also need to pull across the grobo character that gives
// Twinsen the key to the tractor into Polar Island near the start line. LBA2 already has grobo's so we could base it on one of those and
// change the clothing"): LBA1's scene 109 actor 6 (entity 11, BODY.HQR 17: "Grobo: Green vest"); without his key Twinsen says "Can't start
// this machine without an ignition key!" (LBA1's id 10). Here: LBA2's baggage-claim grobo (entity 33, BODY.HQR 54) in a green vest -- his teal
// top LBA2's green ramp, its red trim the green's dark end -- as a body of his entity's own (generic GroboGeneric), standing on the dock beside
// Twinsen's start. Spoken to, he says the story's line (PolarDream.ChaseGroboText: LBA1's two lines of his are gone, the user, 2026-11-02) and
// sets game variable KeyVar; the car's script on the island (every scene's copy of it) lets Twinsen in only once it is set, else he says
// that third line.
internal static class PolarGrobo
{
    public const int Entity = 33, BaseBody = 54, Generic = 1, KeyVar = 230;
    public const int NoKeyText = 22;
    // (LBA2's ramps: 10 the teal top, 4 its red trim -> 8 the greens)
    private const int TealBank = 10, RedBank = 4, GreenBank = 8;

    // Twinsen's line (LBA1's TEXT.HQR, the island's bank, id 10), in the island text file's languages: English, French, German, Spanish,
    // Italian -- LBA1's own -- and Portuguese
    private static readonly string[] NoKey =
    {
        "Can't start this machine without an ignition key!",
        "Impossible de faire démarrer cet engin sans clef de contact !",
        "Es ist unmöglich dieses Gerät ohne Zündschlüssel zu starten !",
        "¡Imposible hacer arrancar esta máquina sin la llave de contacto!",
        "Impossibile far partire questo veicolo senza chiave!",
        "Não consigo ligar esta máquina sem a chave da ignição!",
    };

    public static IEnumerable<(int Id, string Text)> IslandTexts(int language)
    {
        yield return (NoKeyText, NoKey[language < NoKey.Length ? language : 0]);
    }

    // His body: the baggage-claim grobo's, recoloured
    public static byte[] BuildBody(byte[] baseBody)
    {
        // (written lit, each polygon its own type as the retail body has it -- Gouraud: written as read, the body came out unlit, every
        // polygon flat at its ramp's dark end, his grey skin black, 2026-10-27)
        var body = Body.Read(baseBody, 2);
        body.Lit = true;
        for (var i = 0; i < body.Faces.Count; i++)
        {
            var f = body.Faces[i] with { Lba2Type = body.Faces[i].Material };
            var bank = f.Colour >> 4;
            body.Faces[i] = bank == TealBank || bank == RedBank ? f with { Colour = GreenBank * 16 + (f.Colour & 15) } : f;
        }
        for (var i = 0; i < body.Lines.Count; i++)
        {
            var l = body.Lines[i];
            if (l.Colour >> 4 is TealBank or RedBank) body.Lines[i] = l with { Colour = GreenBank * 16 + (l.Colour & 15) };
        }
        return body.Write();
    }

    // His body into the folder: at the end of BODY.HQR -- once: where his entity already has a body GroboGeneric that is this, it stays --
    // and his entity's GroboGeneric in RESS.HQR's entity table (a file the island's install backs up and its removal puts back).
    public static List<string> InstallBody(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var bodies = HqrArchive.Open(bodyPath);
        var bytes = BuildBody(bodies.Read(BaseBody));
        var table = HqrArchive.Open(ressPath).Read(44);
        var entity = Lba2EntityTable.Load(gameDirectory)?.Entities.FirstOrDefault(e => e.Id == Entity);
        var have = entity?.Bodies.Where(b => b.Generic == Generic).Select(b => (int?)b.Body).FirstOrDefault();
        var count = HqrArchive.CountEntries(bodyPath);
        int index;
        if (have is { } h && h < count && bodies.Read(h).AsSpan().SequenceEqual(bytes)) index = h;
        else
        {
            File.WriteAllBytes(bodyPath, HqrWriter.AppendEntry(File.ReadAllBytes(bodyPath), HqrWriter.StoredEntry(bytes)));
            index = count;
        }
        table = RaceTrackBaldinoCar.WithBody(table, Entity, Generic, index);
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return new() { $"the digger's grobo (LBA1's green-vest grobo): BODY.HQR entry {index}, entity {Entity}'s body {Generic} (the baggage-claim grobo, {BaseBody}, in a green vest)" };
    }

    // He stands on the dock: an actor of his entity in `model` at (x, y, z) facing `beta`, his script compiled
    public static void AddActor(SceneModel model, int x, int y, int z, int beta)
    {
        var actor = SceneOps.BlankActor(model.Game, x, y, z, Entity);
        actor.Body = Generic;
        actor.Beta = beta;
        SceneOps.AddActor(model, actor);
        var index = model.Actors.Count - 1;
        // (once: spoken to before the key is his -- the race game's story opening gives it, and says the line, unless Twinsen talked to
        // him first: RACEMOD.CPP ChaseOpening. Not again after: Action at the car, a cell from him, said it once more, the user, 2026-11-06)
        var life = $@"void comportement_0()
{{
    if (1 == action() && 1500 > distance(0) && 0 == var_game({KeyVar}))
    {{
        message({PolarDream.ChaseGroboText});
        set_var_game({KeyVar}, 1);
    }}
}}
";
        actor.Life = Compile(model, index, life);
    }

    // The car's script (the Desert buggy's, every scene's copy) with getting in asking for the key: without it Twinsen says NoKeyText.
    // The script as the build has it (comportement_1's two ways in: Action beside the car, or being carried onto it), changed in place.
    public static bool GateCar(SceneModel model, int index)
    {
        var scripts = SceneScripts.Load(SceneSerializer.Write(model), -1);
        var text = scripts.GetText(index, ScriptKind.Life);
        const string In = "        if (768 > angle_obj(0) && 1500 > distance(0))\n        {\n            set_var_cube(30, 1);";
        if (!text.Contains(In)) return false;
        text = text.Replace(In, $"        if (768 > angle_obj(0) && 1500 > distance(0) && 0 == var_game({KeyVar}))\n        {{\n            message_obj(0, {NoKeyText});\n        }}\n" +
                                "        else if (768 > angle_obj(0) && 1500 > distance(0))\n        {\n            set_var_cube(30, 1);");
        text = text.Replace("    if (6 == carry_obj_by(0))", $"    if (6 == carry_obj_by(0) && 0 != var_game({KeyVar}))");
        model.Actors[index].Life = Compile(model, index, text);
        return true;
    }

    private static byte[] Compile(SceneModel scene, int index, string life)
    {
        var scratch = scene.Clone();
        scratch.Actors[index].Life = new byte[] { 0 };
        var scripts = SceneScripts.Load(SceneSerializer.Write(scratch), -1);
        scripts.SetText(index, ScriptKind.Life, life);
        var built = scripts.Build();
        if (!built.Ok) throw new InvalidDataException("The grobo's scripts didn't compile: " + string.Join("; ", built.Errors));
        return SceneSerializer.Parse(scene.Game, built.Record!).Actors[index].Life;
    }
}
