using System.Buffers.Binary;
using System.IO;
using LBAAssembler.Maps;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Esmers' hangar on the Emerald Moon (the user, 2026-11-13: "a way of Twinsen getting to Emerald Moon and onto Otringal without just
// teleporting"): the Desert's hangar (scene 12, the Esmer base's room, RaceTrackStory.Hangar) again, as the moon base's -- its grid shared,
// its scene copied into 208, the Moon's, which nothing in the build leads into (the retail game's second copy of the moon's outside cube
// (7, 8), where the saucer lands; the story's moon is the track's copies, 229-232). The moon won, the freighter takes Twinsen and his car
// on to Otringal from here (RaceTrackStory.Hangar, chapter 5). A scene under 256: change_cube reaches it.
internal static class MoonHangar
{
    public const int Scene = 208, Source = 12;

    public static List<string> Install(string gameDirectory)
    {
        var log = new List<string>();
        var scenePath = Path.Combine(gameDirectory, "SCENE.HQR");
        var bkgPath = Path.Combine(gameDirectory, "LBA_BKG.HQR");
        var store = new SceneStore(SceneGame.Lba2, gameDirectory);
        if (!store.SceneExists(Source)) { log.Add($"the moon's hangar left out: no scene {Source}"); return log; }
        var scenes = HqrFile.Parse(File.ReadAllBytes(scenePath));
        var bkg = HqrFile.Parse(File.ReadAllBytes(bkgPath));
        var header = new MapBricks.BkgHeader(bkg.Read(0));
        var table = bkg.Read(header.Brk + header.MaxBrk);
        if (table.Length < (Scene + 1) * 2) Array.Resize(ref table, (Scene + 1) * 2);
        table[Scene * 2] = table[Source * 2]; table[Scene * 2 + 1] = table[Source * 2 + 1];
        bkg.SetStored(header.Brk + header.MaxBrk, table);
        // the scene: the Desert's hangar as the game has it, on the moon (its texts the moon's)
        var model = store.Load(Source).Clone();
        model.Island = StellaEntities.MoonIsland;
        var record = LbaScript.SceneScripts.Load(SceneSerializer.Write(model), Scene).Build();
        if (!record.Ok) { log.AddRange(record.Errors.Select(e => $"the moon's hangar left out: {e}")); return log; }
        scenes.SetStored(Scene + 1, record.Record!);
        if (record.Record!.Length > BinaryPrimitives.ReadInt32LittleEndian(scenes.Read(0)))
        { var size = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(size, record.Record!.Length); scenes.SetStored(0, size); }
        new FileTransaction().Write(bkgPath, bkg.ToBytes()).Write(scenePath, scenes.ToBytes()).Commit();
        log.Add($"the moon's hangar: scene {Scene} the Desert's hangar (scene {Source}, its grid) on the Emerald Moon");
        return log;
    }
}
