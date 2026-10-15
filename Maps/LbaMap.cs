using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LBAAssembler.Maps;

// What a map is: an LBA2 island (its ground and its objects -- ILE and OBL -- and an outside scene for each of its cubes), an LBA2 interior
// (a scene and its grid of blocks, in one of the game's block libraries) or an LBA1 interior (a scene, its grid and its block library).
internal enum MapKind { Lba2Island, Lba2Interior, Lba1Interior }

// One scene of a map: its record as SCENE.HQR holds it, and -- an island's -- the cube it is the outside scene of. Its cube-change and door
// zones to the map's own scenes name them LocalScene + their place in the map, not a game scene number: the numbers a map gets in a game
// folder are only known when it is installed there (MapInstall).
internal sealed record MapScene(byte[] Record, int CubeX = 0, int CubeZ = 0);

// A map in a file of its own, `.lbamap` (the user, 2026-10-15: "Let's also add the ability to load new maps from a file instead of them
// having to be in a HQR file"): a zip of map.json (what it is) and the map's own parts, as the game's files hold them -- its scenes' records,
// its grid and block library or its island's ILE and OBL. It is opened into a copy of the game folder with the map installed in it
// (MapWorkspace), where every editor and Play work on it as on any of the game's own, and saved back out of it.
internal sealed class LbaMap
{
    public const string Extension = ".lbamap";
    public const string FileFilter = "LBA Assembler maps (*.lbamap)|*.lbamap";
    public const int FormatVersion = 1;
    // (a zone's number from LocalScene on: the map's own scene, by its place in Scenes)
    public const int LocalScene = 30000;

    public MapKind Kind { get; set; }
    public string Name { get; set; } = "New map";
    public string Description { get; set; } = "";
    // LBA2: the game's island its scenes carry the number of -- whose sky, palette, music and holomap they borrow (0 Citadel ... 11 the
    // Undergas Island, 12 Polar Island)
    public int IslandByte { get; set; }
    // An LBA1 map: the scene slot it takes in a game folder (LBA1's scenes can't be added to: LBA_GRI.HQR's entries after its 120 grids are
    // grid fragments); an LBA2 interior's: the game scene it was made or exported from, its library and look
    public int? Slot { get; set; }
    // An island: the name of its ILE and OBL in a game folder (eight letters at the most)
    public string? IslandFile { get; set; }
    public List<MapScene> Scenes { get; } = new();
    // An interior's grid (LBA2: LBA_BKG.HQR's entry, its 34-byte header -- library, fragments, used blocks -- and all; LBA1: LBA_GRI.HQR's)
    public byte[]? Grid { get; set; }
    // An LBA1 interior's block library (LBA_BLL.HQR's entry: each LBA1 scene has its own)
    public byte[]? Library { get; set; }
    public byte[]? Ile { get; set; }
    public byte[]? Obl { get; set; }

    public bool IsLba1 => Kind == MapKind.Lba1Interior;
    public string KindName => Kind switch { MapKind.Lba2Island => "LBA2 island", MapKind.Lba2Interior => "LBA2 interior", _ => "LBA1 interior" };

    private sealed class Manifest
    {
        public int Format { get; set; } = FormatVersion;
        [JsonConverter(typeof(JsonStringEnumConverter))] public MapKind Kind { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int IslandByte { get; set; }
        public int? Slot { get; set; }
        public string? IslandFile { get; set; }
        public List<ManifestScene> Scenes { get; set; } = new();
        public string Program { get; set; } = "LBA Assembler";
    }
    private sealed class ManifestScene
    {
        public string File { get; set; } = "";
        public int CubeX { get; set; }
        public int CubeZ { get; set; }
    }

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void Save(string path)
    {
        var manifest = new Manifest { Kind = Kind, Name = Name, Description = Description, IslandByte = IslandByte, Slot = Slot, IslandFile = IslandFile };
        // (written beside it and swapped in: a map file is never left half written)
        var temp = path + ".tmp";
        using (var stream = File.Create(temp))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            void Put(string name, byte[]? bytes)
            {
                if (bytes is null) return;
                using var entry = zip.CreateEntry(name, CompressionLevel.Optimal).Open();
                entry.Write(bytes);
            }
            for (var i = 0; i < Scenes.Count; i++)
            {
                var name = $"scene{i}.bin";
                manifest.Scenes.Add(new ManifestScene { File = name, CubeX = Scenes[i].CubeX, CubeZ = Scenes[i].CubeZ });
                Put(name, Scenes[i].Record);
            }
            Put("grid.bin", Grid);
            Put("library.bin", Library);
            Put("island.ile", Ile);
            Put("island.obl", Obl);
            Put("map.json", JsonSerializer.SerializeToUtf8Bytes(manifest, Json));
        }
        File.Move(temp, path, overwrite: true);
    }

    public static LbaMap Load(string path)
    {
        try
        {
            using var zip = ZipFile.OpenRead(path);
            byte[]? Get(string name)
            {
                if (zip.GetEntry(name) is not { } entry) return null;
                using var stream = entry.Open();
                using var copy = new MemoryStream();
                stream.CopyTo(copy);
                return copy.ToArray();
            }
            var manifest = JsonSerializer.Deserialize<Manifest>(Get("map.json") ?? throw new InvalidDataException("It has no map.json: it isn't an LBA Assembler map."), Json)
                ?? throw new InvalidDataException("Its map.json is empty.");
            if (manifest.Format > FormatVersion) throw new InvalidDataException($"It is a newer kind of map (format {manifest.Format}) than this version of the program reads ({FormatVersion}).");
            var map = new LbaMap
            {
                Kind = manifest.Kind, Name = manifest.Name, Description = manifest.Description, IslandByte = manifest.IslandByte, Slot = manifest.Slot,
                IslandFile = manifest.IslandFile, Grid = Get("grid.bin"), Library = Get("library.bin"), Ile = Get("island.ile"), Obl = Get("island.obl"),
            };
            foreach (var s in manifest.Scenes)
                map.Scenes.Add(new MapScene(Get(s.File) ?? throw new InvalidDataException($"Its scene {s.File} is missing."), s.CubeX, s.CubeZ));
            map.Check();
            return map;
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or IOException)
        {
            throw new InvalidDataException($"{Path.GetFileName(path)} can't be read as a map: {error.Message}", error);
        }
    }

    // What a map of its kind must have.
    public void Check()
    {
        if (Scenes.Count == 0) throw new InvalidDataException("It has no scene.");
        switch (Kind)
        {
            case MapKind.Lba2Island:
                if (Ile is null || Obl is null) throw new InvalidDataException("An island map needs its ILE and its OBL.");
                if (!ValidIslandFile(IslandFile)) throw new InvalidDataException($"An island map's file name \"{IslandFile}\" isn't a name the game can load (eight letters or digits at the most).");
                break;
            case MapKind.Lba2Interior:
                if (Grid is null) throw new InvalidDataException("An interior map needs its grid.");
                break;
            case MapKind.Lba1Interior:
                if (Grid is null || Library is null) throw new InvalidDataException("An LBA1 map needs its grid and its block library.");
                if (Slot is null) throw new InvalidDataException("An LBA1 map needs the scene slot it takes.");
                break;
        }
    }

    // An island's file name the game can load: one to eight letters, digits or underscores, not one of the game's own islands' (ILE files a
    // game folder has stay its own).
    public static bool ValidIslandFile(string? name) =>
        name is { Length: >= 1 and <= 8 } && name.All(c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_') && !name.StartsWith('_');

    // A file name from the map's name (for a new island's ILE, or a map file's name).
    public static string FileNameFrom(string name, int most)
    {
        var letters = new string(name.ToUpperInvariant().Where(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9').ToArray());
        if (letters.Length == 0) letters = "NEWMAP";
        return letters.Length > most ? letters[..most] : letters;
    }
}
