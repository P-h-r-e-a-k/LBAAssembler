using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Grids;
using LBAAssembler.Lba1;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Stella Entities' office (scene 391) and backstage (scene 392): built in full behind their doors, though the bouncers keep Twinsen out for
// now (the user, 2026-11-11 late: "even if we can't access them initially I'm sure I'll add something later to allow us to") -- so the
// club's scene has no way in yet; each room's own door leads back out to the hall, at the door it belongs to (CitadelProps.OfficeAt,
// BackstageAt), so a way in can be added later by a zone there.
//   - the office: Rick's office (scene 137), its grid, its light and music and its furniture as the game has them (the desk and the lamps),
//     without Rick, his guards and their scripts;
//   - backstage: the same room (the grid is shared), furnished for the performers -- a rack, chairs, mirrors -- instead.
internal static class StellaRooms
{
    public const int Office = 391, Backstage = 392, RickOffice = 137;
    private const int Prop = Citadel.CitadelProps.Chair;

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(RickOffice) || !store.SceneExists(StellaEntities.Scene)) { log.Add("Stella Entities' office and backstage left out: no Rick's office (scene 137) or no club scene"); return log; }
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        if (Backstage + 1 < scenes.Count && !scenes.IsEmpty(Backstage + 1)) { log.Add($"Stella Entities' rooms left out: scene {Backstage} is already another part of the build's"); return log; }
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        // (Rick's office's grid, shared: the rooms don't change it)
        var type = table[RickOffice * 2]; var grid = table[RickOffice * 2 + 1];
        if (table.Length < (Backstage + 1) * 2) Array.Resize(ref table, (Backstage + 1) * 2);
        foreach (var s in new[] { Office, Backstage }) { table[s * 2] = type; table[s * 2 + 1] = grid; }
        bkg.SetStored(header.Brk + header.MaxBrk, table);

        var original = store.Load(RickOffice);
        var (outX, outZ) = StellaEntities.At(Citadel.CitadelProps.OfficeAt, 14.5f);
        var (backX, backZ) = StellaEntities.At(Citadel.CitadelProps.BackstageAt, 14.5f);
        static string Still() => "void comportement_0()\n{\n}\n";
        var records = new List<(int Scene, byte[] Record)>();
        foreach (var (number, name, outAt) in new[] { (Office, "office", (outX, outZ)), (Backstage, "backstage", (backX, backZ)) })
        {
            var model = original.Clone();
            model.Island = StellaEntities.MoonIsland;
            model.TrackPoints.Clear();
            // the furniture: the game's own entity 16's with a body (the desk and what stands round it) and its lamps (sprites) -- the office's;
            // backstage has none of them
            var keep = new List<SceneActorModel> { model.Actors[0] };
            if (number == Office) keep.AddRange(model.Actors.Skip(1).Where(a => (a.Entity == 16 && a.Body >= 0) || a.IsSprite));
            model.Actors.Clear(); model.Actors.AddRange(keep);
            model.Zones.RemoveAll(z => z.Type != 0);
            foreach (var z in model.Zones.Where(z => z.Type == 0))
            {
                z.Num = StellaEntities.Scene;
                z.Info = new[] { outAt.Item1, StellaEntities.FloorY, outAt.Item2, 0, 0, 0, 0, 0 };
            }
            if (number == Backstage)
            {
                // the dressing room: a rack of the performers' things at the back, chairs at the mirrors, mirrors (pictures) on the wall
                foreach (var (generic, x, z, beta) in new[] { (Citadel.CitadelProps.Rack, 7600, 2900, 0), (Citadel.CitadelProps.Chair, 3800, 4400, 1024), (Citadel.CitadelProps.Chair, 5200, 4400, 1024), (Citadel.CitadelProps.Chair, 6600, 4400, 1024) })
                    SceneOps.AddActor(model, Citadel.CitadelProps.Actor(generic, x, 256, z, beta));
            }
            var scripts = LbaScript.SceneScripts.Load(SceneSerializer.Write(model), number);
            for (var i = 0; i < model.Actors.Count; i++)
            {
                scripts.SetText(i, LbaScript.ScriptKind.Life, Still());
                scripts.SetText(i, LbaScript.ScriptKind.Track, "label(0);\nstop();\n");
            }
            var built = scripts.Build();
            if (!built.Ok) { log.AddRange(built.Errors.Select(e => $"Stella Entities' {name} left out: {e}")); return log; }
            records.Add((number, built.Record!));
        }
        foreach (var (number, record) in records)
        {
            var entry = number + 1;
            while (scenes.Count <= entry) scenes.Slots.Add(new HqrFile.Slot());
            scenes.SetStored(entry, record);
            if (record.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
            { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, record.Length); scenes.SetStored(0, size); }
        }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        log.Add($"Stella Entities' rooms: the office (scene {Office}) and backstage (scene {Backstage}) on Rick's office's grid {grid}, each with its door back to the hall; no way in from the hall yet");
        return log;
    }
}
