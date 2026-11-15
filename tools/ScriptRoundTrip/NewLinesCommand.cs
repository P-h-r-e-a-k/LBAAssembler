using System.Buffers.Binary;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using LBAAssembler;
using LBAAssembler.Lba1.Runtime;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;
using LBAAssembler.Terrain;

namespace ScriptRoundTrip;

// newlines [--built <LBRA folder>] [--disc <LBA2.GOG>] [--out <folder>] [--work <folder>] [--seconds <n>] [--scan]
//
// The race mod's new lines, laid out for recording voices for them (the user, 2026-11-15: "For each character we've added new lines for,
// create a sub-folder ... named after the character, with each new text line spoken in a single txt file, named with the text bank number
// they're from. Also combine all of the characters' existing audio speech into a single wav file named sample" -- one set per language,
// "as we need to generate new audio for all languages"):
//   <out>\<Language>\<Character>\<bank>_<id>.txt   one line's words in that language (UTF-8), e.g. Spanish\Twinsen\citadel_1203.txt
//   <out>\<Language>\<Character>\sample.wav        the character's own voice in the retail game, from that language's VOX files where the
//                                                  game is voiced in it (English, French, German), else the English one (the language
//                                                  folder's README.txt says so); none for a character the game never voiced (README.txt)
//   <out>\index.tsv                                every line: who, its bank, id and place in the bank's list (= the VOX entry a recording
//                                                  goes in), whether it is new at the end or replaced in place, where it is said
//   <out>\left_out.tsv                             the new texts not exported, and why (signs, labels, things described)
// What is new: the built game's TEXT.HQR (the LBRA install, read only) against the retail one on the CD image -- a text whose words the
// retail file has neither at its place nor under its id is the mod's (one emptied, a retail text nothing says any more, isn't a line).
// Who says it, the first of: a text rule (TextRules: the lines the race mode says from its race files, radios, voices off, and the things
// described, left out); the line's name in the story's tables (RaceTrackStory's enums: DesertLine.FlickShut is Flick's); the built scenes'
// life scripts -- message / add_message (the script's own actor), message_obj / ask_choice_obj / pcx_mess_obj (the actor named),
// message_zoe (Zoe), add_choice (Twinsen: the chosen answer is spoken by him, GAMEMENU.CPP GameAskChoice) -- the actor named by its body
// (the entity table's body for its generic one, BODY2.HQD's name, BodyRules); for a line the island's retelling made (IslandRetell's
// tables), whoever says the game's text it retells; the game file's found messages and descriptions are the narrator's. Each character's
// voice: the retail scenes' lines of the same character (Twinsen's: his answers), their samples the VOX entries at their places
// (MESSAGE.CPP Speak: a text's sample is the entry at its place in the list -- so a text added at the end has none, it is shown, not
// spoken, until its VOX gets an entry there); a character the retail game never voiced, a stand-in (VoiceOf, or the body they wear), or
// for an unseen one the recordings of the game's lines theirs retell.
// Re-runnable: <out> is wiped and written again (only a folder this wrote: it has index.tsv). Nothing is written to the built folder or the
// disc image; the retail data files it needs are copied from the disc into <work>\retail.
internal static class NewLinesCommand
{
    private static readonly string[] LanguageNames = { "English", "French", "German", "Spanish", "Italian", "Portuguese" };
    private static readonly string[] VoxPrefixes = { "EN_", "FR_", "DE_", "SP_", "IT_", "PO_" };   // MESSAGE.CPP ListLanguage
    private const int RetailFiles = 15, GameFile = 2;
    private const int Rate = 22050;                                  // sample.wav: 16-bit mono at this rate
    private const double Gap = 0.4, Shortest = 0.8;                  // seconds of silence between lines; lines shorter than this skipped
    private const string Unknown = "_Unknown", Narrator = "Narrator";
    private const int HelperEntity = 16;                             // the race mod's unseen talkers' and helpers' entity (RaceTrackScenes.InvisibleHelper)

    // MESSAGE.CPP ListFileText: the VOX of a text file
    private static string VoxName(int lang, int file) => VoxPrefixes[lang] + (file switch { 0 => "SYS", 1 => "CRE", 2 => "GAM", _ => (file - 3).ToString("000") }) + ".VOX";

    // the banks' names in the files' names (file 3 + island; island 12, Polar Island, is the engine's file 15)
    private static string BankName(int file) => file switch
    {
        2 => "game", 3 => "citadel", 4 => "island1", 5 => "desert", 6 => "emerald", 7 => "otringal", 8 => "celebration", 9 => "wannies",
        10 => "mosquibees", 11 => "francos", 12 => "cx", 13 => "elevator", 14 => "volcano", 15 => "polar", _ => $"file{file}",
    };

    // ---- who a body is. A rule's character null: a thing (a page, a stone, a sign, a radio set): what it says is described, not voiced,
    // unless a text rule names who speaks through it. Bodies no rule names are people under their BODY2.HQD name (its "(...)" and ": body
    // N of its kind" left off).
    private static readonly (Regex Body, string? Character)[] BodyRules =
    {
        (new(@"^Twinsen"), "Twinsen"),
        (new(@"^Zo(e|é|\W|$)"), "Zoe"),
        (new(@"Baldino"), "Baldino"),
        (new(@"^Raph, the lighthouse keeper"), "Raph"),
        (new(@"^Raph's fianc"), "Raph's Fiancee"),
        (new(@"^Johnny Rocket"), "Johnny Rocket"),
        (new(@"^Bob with"), "Bob"),
        (new(@"^Felix with"), "Felix"),
        (new(@"^Zed\b"), "Zed"),
        (new(@"^Dino-Fly"), "Dino-Fly"),
        (new(@"^Dean of the School of Magic"), "The Dean"),
        (new(@"^Weather wizard"), "The Weather Wizard"),
        (new(@"^The Citadel Island thie"), "The Thief"),
        (new(@"^Mr\. Paul"), "Mr Paul"),
        (new(@"^Luc, the boss"), "Luc"),
        (new(@"^Tim, waiter"), "Tim"),
        (new(@"^The Tralu$"), "The Tralu"),
        (new(@"^Queen of the Mosquibees"), "The Mosquibee Queen"),
        (new(@"^The emperor's wife"), "The Empress"),
        (new(@"^The emperor$"), "The Emperor"),
        (new(@"^Mrs\. Bloop$"), "Mrs Bloop"),
        (new(@"^Mrs\. Brune"), "Mrs Brune"),
        (new(@"^Mr\. Bazoo"), "Mr Bazoo"),
        (new(@"^Moya|^Silhuette of the turtle"), "Moya"),
        (new(@"^Dark Monk|FunFrock"), "FunFrock"),                   // (LBA2's Dark Monk is FunFrock hidden: the mod's FunFrock)
        (new(@"^Brother of francos' burgermaster"), "De La Fontaine"),
        (new(@"^Female rabbibunny in the School of Magic"), "Flick"),  // (the Dean's assistant: in the mod, Flick, the School's marshal)
        (new(@"^Sup in dark green dressing"), "Esmer"),             // (LBA2's Esmers, Johnny Rocket's clones: the mod's Esmers)
        (new(@"^Shy sup girl in Imperial Hotel: body \d+ of its kind"), "Lyra"),   // (the flag marshal the build makes of her: Lyra)
        (new(@"^(Holomap|Mushroom|Standing stone|Piece of flying saucer|Three darts|Notes|Memory viewer|Container|Trunk|Flask|Hourglass|Blowgun test|Light from|Elevator \(|Ferry \(|Flying saucer|Royal saucer|Small flying saucer|Boat is|Trash can|Propeller|Crate|Cart in|Arrow in|Race car|Oil slick|Racing gloves)"), null),
    };

    // characters whose voice no retail one stands in for (a voice off: the sample would be whoever's script says it)
    private static readonly HashSet<string> NoStandIn = new() { "Unseen voice", "HAL" };
    // entities that are one character wherever they stand (the game's and the build's): the souvenir seller of Celebration Island
    private static readonly Dictionary<int, string> EntityCharacters = new() { [RaceTrackStory.SellerEntity] = "Souvenir seller" };

    // the 1996 demo's characters the build adds as entities of their own (Demo96Rooms: setup.log's "the demo's characters D -> entity E")
    private static readonly Dictionary<int, string> DemoCharacters = new()
    {
        [62] = "The Dean", [63] = "Flick", [45] = "Painter from Citadel Island sewers", [11] = "School of Magic greeter",
    };

    // ---- the story's line tables (RaceTrackStory and the rest: string[][] XLines, enum XLine naming each line): a line's name says who
    // speaks it when it starts with one of these. The mod's own characters (Story) win over the body of the actor who says the line (an
    // unseen talker, a copy in another's body); the rest are taken only when no visible actor says the line.
    private static readonly (string Prefix, string Character, bool Story)[] NamePrefixes =
    {
        ("Zoe", "Zoe", true), ("Twinsen", "Twinsen", true), ("Baldino", "Baldino", true), ("Flick", "Flick", true), ("Dean", "The Dean", true),
        ("Moya", "Moya", true), ("Lyra", "Lyra", true), ("Raph", "Raph", true), ("Paul", "Mr Paul", true), ("Wizard", "The Weather Wizard", true),
        ("Hal", "HAL", true), ("Empress", "The Empress", true), ("Emperor", "The Emperor", true), ("Queen", "The Mosquibee Queen", true),
        ("Stan", "Stan", true), ("FunFrock", "FunFrock", true), ("Seller", "Souvenir seller", true), ("Tim", "Tim", true), ("Luc", "Luc", true),
        ("Bob", "Bob", true), ("Thief", "The Thief", true), ("Bloop", "Mrs Bloop", true), ("Brune", "Mrs Brune", true), ("Tralu", "The Tralu", true),
        ("Fiancee", "Raph's Fiancee", true), ("Johnny", "Johnny Rocket", true), ("Rick", "Rick the sup", true), ("Fontaine", "De La Fontaine", true),
        ("Herald", "Esmer", false), ("Esmer", "Esmer", false), ("Steward", "Steward", false), ("Slick", "Slick", false), ("Kurtz", "Kurtz", false),
        ("Nurse", "Nurse", false), ("Mechanic", "Mechanic", false), ("Bowler", "Moon bowler", true), ("Monster", "Monkey monster", false),
        ("OldWannie", "Old wannie", false), ("Miner", "Wannie miner", false), ("Vent", "Mosquibee ventilation guy", false), ("Pat", "Pat", false),
        ("Fab", "Fab", false), ("Kangaroo", "Kangaroo #1", false), ("Priest", "Priest", false), ("Customer", "Customer", false),
        ("Racer", "Desert champion", true), ("Sister", "Brune sisters", true), ("Hide", "Mosquibee dissident", false),
    };
    // ... and lines whose name says nothing of who, said by an unseen talker that stands for a character (LateStory's Talker for Lyra, the
    // flag marshal the race mode draws)
    private static readonly Dictionary<string, string?> NamedLines = new()
    {
        ["CLine.Arrive"] = "Lyra", ["CLine.Won"] = "Lyra", ["CLine.Palace"] = "Lyra", ["PLine.Arrive"] = "Lyra", ["PLine.Fragments"] = "Lyra",
        ["PLine.NoFragments"] = "Lyra", ["VLine.Arrive"] = "Lyra", ["VLine.Won"] = "Lyra", ["FLine.Won"] = "Lyra",
        ["MoonLine.HangarGo"] = "Lyra",
    };
    // a character the retail game never voiced, sampled from the retail character whose body they wear in the game
    private static readonly Dictionary<string, string> VoiceOf = new()
    {
        ["Lyra"] = "Female sup in the flying saucer", ["Priest"] = "Sup priest", ["Brune sisters"] = "Mrs Brune", ["Moon bowler"] = "Quetch bowl player", ["Mechanic"] = "Mechanic owl",
    };
    // ... and why that one
    private static readonly Dictionary<string, string> VoiceWhy = new()
    {
        ["Lyra"] = "a Sup woman, as Lyra is (the Imperial Hotel's Sup girl, whose body the flag marshal is made from, has one short line in the retail game)",
        ["Priest"] = "the Dark Monk's priests are Sups",
        ["Brune sisters"] = "their sister's voice, Mrs. Brune of the Citadel ferry",
        ["Moon bowler"] = "one of the Desert's bowlers, as the moon's bowler is one (in a space suit)",
        ["Mechanic"] = "the retail game's only mechanic line; a short one",
    };

    // ---- lines no script names a speaker for (the race mode says them from its race files: gates, hints, chases; radios; voices off) or
    // whose script's speaker is a thing, by their English words: a character, or null and why they are left out (no one's voice)
    private static readonly (Regex English, string? Character, string? Why)[] TextRules =
    {
        // things described, signs, postcards, labels: no one's voice
        (new(@"^(A|An|Another|The last) (postcard|narrow door|old sign|sign|poster|dusty bottle|page|portrait|file of blueprints|little statue of the Dark Monk\.|change machine)"), null, "a thing described"),
        (new(@"^(A fifth Dark Monk|Twinsen picked|Twinsen can't read|Here raced the chariot|\.\.\.except Twinsen\.|The machine rattles|WANTED\. )"), null, "a thing described"),
        (new("^\"[^\"]*\"$"), null, "a sign or a notice"),
        (new(@"^The radio is ringing\.|^The hatch closes\. The freighter shudders|^Twinsen sings |^The Emperor's bedroom\. Locked"), null, "narration"),
        (new("^\"[A-Z][A-Z' ]{3,}[:.!]|^OFFICIAL NOTICE|\\bSigned: "), null, "a sign or a notice"),
        (new(@"^Twinsen, the car's ready\. All it needs now is a driver with gloves"), "Zoe", null),
        // the race mode's own lines (race files: RACEMOD.CPP gate=, hints, chases, the walker's yell) and the voices off
        (new(@"^Stop right there, Twinsen! Nobody drives on this track without driving gloves"), "Mr Paul", null),
        (new(@"^Hey! Get off the track, Twinsen!|^Hey, you! Get off the track!"), "Raph", null),
        (new(@"^My regular's umbrella, from the pharmacy|^And that umbrella you brought back|^And if you ever find my regular's umbrella"), "Luc", null),
        (new(@"^There's a crowd at the new track already|^Luc's barrel car is a wheel short|^The town circuit's open, Twinsen|^We've won here! Mr\. Paul's ticket|^Twinsen, the Dino-Fly says the sky's too stormy"), "Zoe", null),
        (new(@"^Flick says the Dean has shut the track|^The Dean's exam pages blew all over|^The sand will choke the engine|^No helmet, no race\. The giant clam|^Everything's ready, Twinsen\. Let's go and win the desert"), "Zoe", null),
        (new(@"^The base's computer, HAL, has locked|^The start lights are on\. Let's race"), "Zoe", null),
        (new(@"^Her umbrella\. After yesterday"), "Sphero customer of pharmacy", null),
        (new(@"^FunFrock has the Emperor's teleporter|^Too slow\.\.\. FunFrock got to the teleporter|^Celebration Island\.\.\. the teleporter brought me|^Polar Island\? FunFrock has escaped|^No! FunFrock got to Sendell first|^No, FunFrock, come back!"), "Twinsen", null),
        (new(@"^You'll never catch me!|^FunFrock's voice"), "FunFrock", null),
        (new(@"^The souvenir seller[:,] |^You beat me fair and square, mister!|^Ha! Not fast enough, mister\."), "Souvenir seller", null),
        (new(@"^FunFrock: "), "FunFrock", null),
        (new(@"^Baldino(, on the radio)?: "), "Baldino", null),
        (new(@"^De La Fontaine trundles past"), "De La Fontaine", null),
        (new(@"^Twinsen, did you get the Desert track open|^Well, Twinsen, have you found the Dean's pages|^By the way, could you ask Baldino for those sand filters|^I'm worried, Twinsen\. These space visitors"), "Zoe", null),
        (new(@"^Welcome to Zeelich, the planet of a thousand wonders|^In our workshops, our engineers|^If you don't meet with him, you might at least|^At the palace bend, you will get a look|^After the race, you will try your luck|^By no means!\s+You shouldn't leave Zeelich"), "Zeelich tour guide", null),
        (new(@"^Twinsen, the statue is gone and the Cup is yours|^It is I who must thank you, you won it for us|^Well, FunFrock's wish was much too powerful|it's me, Sendell"), "Sendell", null),
        (new(@"^(Another voice, unseen|A voice, fading)"), "Unseen voice", null),
        (new(@"Baldino here|^Twinsen\? Baldino\.|^Twinsen! My radio works|It's me, Baldino!"), "Baldino", null),
    };

    private sealed record Text(int Id, int Place, byte[] Bytes);
    private sealed record Who(string? Person, string? Thing, string Detail, string? Voice = null);
    private sealed record Said(int Scene, int Actor, string How, Who Who);
    private sealed class NewLine
    {
        public int File, Id, Place;
        public bool Replaced;
        public Dictionary<int, string> Words = new();
        public List<Said> Said = new();
        public string Character = "";      // "" = left out
        public string Why = "";
        public string? Name;               // its name in the story's line tables (DesertLine.FlickShut), if it has one
        public List<int> Retells = new();
        public List<string> OtherPlaces = new();   // a language whose list has the line at another place (its VOX entry there)  // the game's texts it retells (IslandRetell), whose recordings are its speaker's own voice
        public string English => Words.TryGetValue(0, out var w) ? w : Words.Values.FirstOrDefault() ?? "";
    }

    private static readonly Regex Says = new(
        @"\b(message|message_obj|message_zoe|add_choice|ask_choice|ask_choice_obj|add_message|add_message_obj|pcx_mess_obj)\s*\(([^;()]*?)(\d+)\s*\)");

    private static Encoding Dos
    {
        get { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); return Encoding.GetEncoding(850); }
    }

    public static int Run(string[] args)
    {
        string Arg(string name, string fallback) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback; }
        var built = Arg("--built", @"E:\dump\Test\LBRA");
        var discPath = Arg("--disc", @"E:\GOG Games\Little Big Adventure 2\LBA2.GOG");
        var output = Arg("--out", @"E:\dump\LittleBigRacingAdventure\NewLines");
        var work = Arg("--work", @"E:\dump\TEMP\nov15\newlines");
        var seconds = double.Parse(Arg("--seconds", "75"), System.Globalization.CultureInfo.InvariantCulture);
        var scan = args.Contains("--scan");

        // ---- the retail game, from the disc (its data files into the work folder, for SceneStore and the entity table)
        using var disc = new Lba1Iso(discPath);
        var retail = Path.Combine(work, "retail");
        Directory.CreateDirectory(retail);
        foreach (var name in new[] { "TEXT.HQR", "SCENE.HQR", "RESS.HQR", "BODY.HQR" })
        {
            var entry = disc.Files.FirstOrDefault(f => f.Path.Equals("/LBA2/" + name, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException($"{discPath} has no LBA2\\{name}: not the LBA2 CD image?");
            var to = Path.Combine(retail, name);
            if (!File.Exists(to) || new FileInfo(to).Length != entry.Size) File.WriteAllBytes(to, disc.Read(entry));
        }
        var voxOnDisc = disc.Files.Where(f => f.Path.StartsWith("/LBA2/VOX/", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(f => Path.GetFileName(f.Path).ToUpperInvariant(), f => f);
        var languages = Math.Min(LanguageNames.Length, Lba2TextBank.Languages(Path.Combine(built, "TEXT.HQR")));
        var voiced = Enumerable.Range(0, languages).Where(l => voxOnDisc.ContainsKey(VoxName(l, 3))).ToList();
        if (!voiced.Contains(0)) throw new InvalidDataException("The disc has no English voices (VOX\\EN_000.VOX).");

        var retailText = HqrArchive.Open(Path.Combine(retail, "TEXT.HQR"));
        var builtText = HqrArchive.Open(Path.Combine(built, "TEXT.HQR"));
        var retailBanks = new Dictionary<(int Lang, int File), List<Text>>();
        var builtBanks = new Dictionary<(int Lang, int File), List<Text>>();
        for (var file = GameFile; file <= RetailFiles; file++)
            for (var lang = 0; lang < languages; lang++)
            {
                retailBanks[(lang, file)] = LoadBank(retailText, lang, file);
                builtBanks[(lang, file)] = LoadBank(builtText, lang, file);
            }

        // ---- 1. the new texts: in any language, a built text the retail file has neither at its place nor under its id
        var lines = new Dictionary<(int File, int Id), NewLine>();
        for (var file = GameFile; file <= RetailFiles; file++)
            for (var lang = 0; lang < languages; lang++)
            {
                var before = retailBanks[(lang, file)];
                var beforeById = before.GroupBy(t => t.Id).ToDictionary(g => g.Key, g => g.First());
                foreach (var t in builtBanks[(lang, file)])
                {
                    if ((t.Place < before.Count && before[t.Place].Bytes.AsSpan().SequenceEqual(t.Bytes))
                        || (beforeById.TryGetValue(t.Id, out var old) && old.Bytes.AsSpan().SequenceEqual(t.Bytes))) continue;
                    lines.TryAdd((file, t.Id), new NewLine { File = file, Id = t.Id });
                }
            }
        foreach (var line in lines.Values)
        {
            for (var lang = 0; lang < languages; lang++)
                if (builtBanks[(lang, line.File)].FirstOrDefault(t => t.Id == line.Id) is { } t) line.Words[lang] = Words(t.Bytes);
            var english = builtBanks[(0, line.File)].FirstOrDefault(t => t.Id == line.Id);
            line.Place = english?.Place ?? -1;
            line.Replaced = english is not null && english.Place < retailBanks[(0, line.File)].Count;
            // (the VOX entry is the place in each language's own list: the same in all of them, as a rule)
            for (var lang = 1; lang < languages; lang++)
                if (builtBanks[(lang, line.File)].FirstOrDefault(t => t.Id == line.Id) is { } t && t.Place != line.Place) line.OtherPlaces.Add($"{VoxPrefixes[lang]}{t.Place}");
        }
        // (a retail text emptied -- nothing says it now -- is no line)
        foreach (var key in lines.Where(p => p.Value.Words.Values.All(w => w.Length == 0)).Select(p => p.Key).ToList()) lines.Remove(key);

        // ---- 2. who says them: the built scenes' scripts
        var builtWho = new Bodies(built, Demo(built));
        foreach (var (file, scene, actor, how, id, model, scriptBody) in SaidTexts(built))
            if (lines.TryGetValue((file, id), out var line))
                line.Said.Add(new Said(scene, actor, how, builtWho.Of(model, actor, how, scriptBody)));

        // ---- the retail scenes' lines: who says each text, and each character's
        var retailWho = new Bodies(retail, new Dictionary<int, string>());
        var retailSaid = new Dictionary<(int File, int Id), List<string>>();
        var answers = new HashSet<(int File, int Id)>();
        foreach (var (file, _, actor, how, id, model, scriptBody) in SaidTexts(retail))
            if (retailWho.Of(model, actor, how, scriptBody).Person is { } person)
            {
                // (a line to Twinsen in his own script is a voice he hears, not his)
                if (person == "Twinsen" && how != "add_choice" && retailBanks[(0, file)].FirstOrDefault(t => t.Id == id) is { } t && ToTwinsen.IsMatch(Words(t.Bytes))) continue;
                if (!retailSaid.TryGetValue((file, id), out var who)) retailSaid[(file, id)] = who = new List<string>();
                who.Add(person);
                if (person == "Twinsen" && how == "add_choice") answers.Add((file, id));
            }
        var ownLines = new Dictionary<string, List<(int File, int Id)>>();
        foreach (var ((file, id), who) in retailSaid.OrderBy(p => p.Key.File).ThenBy(p => p.Key.Id))
        {
            var person = who.GroupBy(w => w).OrderByDescending(g => g.Count()).First().Key;
            // (Twinsen's own voice for certain: his answers -- what his script says is now and then a sign read or someone else's)
            if (person == "Twinsen" && !answers.Contains((file, id))) continue;
            if (!ownLines.TryGetValue(person, out var own)) ownLines[person] = own = new List<(int, int)>();
            own.Add((file, id));
        }
        // (the narrator: what the game says of the things Twinsen finds and of his inventory, EN_GAM.VOX)
        ownLines[Narrator] = retailBanks[(0, GameFile)].Where(t => t.Bytes.Length > 0 && (t.Id < 40 || t.Id is >= 200 and < 240)).Select(t => (GameFile, t.Id)).ToList();

        // ---- 3. each line's character
        var retold = Retold();
        var names = LineNames();
        foreach (var line in lines.Values)
        {
            names.TryGetValue(Key(line.English), out line.Name);
            Decide(line, retold, retailSaid);
        }

        if (scan)
        {
            var dump = new StringBuilder("file\tid\tplace\treplaced\tcharacter\twhy\tsaid\tenglish\n");
            foreach (var l in lines.Values.OrderBy(l => l.File).ThenBy(l => l.Id))
                dump.Append($"{l.File}\t{l.Id}\t{l.Place}\t{l.Replaced}\t{l.Character}\t{l.Why}{(l.Name is null ? "" : $" [{l.Name}]")}\t{string.Join(" | ", l.Said.Select(s => $"s{s.Scene} a{s.Actor} {s.How} -> {s.Who.Person ?? (s.Who.Thing is null ? "?" : "thing " + s.Who.Thing)} [{s.Who.Detail}]"))}\t{l.English.Replace('\n', ' ')}\n");
            Directory.CreateDirectory(work);
            File.WriteAllText(Path.Combine(work, "scan.tsv"), dump.ToString(), new UTF8Encoding(false));
            var retailDump = new StringBuilder("character\tlines\n");
            foreach (var (person, own) in ownLines.OrderBy(p => p.Key)) retailDump.Append($"{person}\t{own.Count}\n");
            File.WriteAllText(Path.Combine(work, "retail_characters.tsv"), retailDump.ToString(), new UTF8Encoding(false));
            Console.WriteLine($"newlines --scan: {lines.Count} new texts, written to {Path.Combine(work, "scan.tsv")}; the retail game's characters in retail_characters.tsv");
            return 0;
        }

        // ---- 4. the folder, written again
        if (Directory.Exists(output))
        {
            var ours = File.Exists(Path.Combine(output, "index.tsv")) || !Directory.EnumerateFileSystemEntries(output).Any();
            if (!ours) { Console.WriteLine($"{output} has files this didn't write (no index.tsv): not wiped. Empty it or pick another --out."); return 1; }
            Directory.Delete(output, true);
        }
        Directory.CreateDirectory(output);
        var utf8 = new UTF8Encoding(false);
        var exported = lines.Values.Where(l => l.Character.Length > 0).OrderBy(l => l.Character).ThenBy(l => l.File).ThenBy(l => l.Id).ToList();
        var characters = exported.Select(l => l.Character).Distinct().OrderBy(c => c).ToList();
        for (var lang = 0; lang < languages; lang++)
            foreach (var line in exported)
            {
                var folder = Path.Combine(output, LanguageNames[lang], Safe(line.Character));
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, FileName(line)), line.Words.TryGetValue(lang, out var w) ? w : line.English, utf8);
            }

        // the samples: in a voiced language its own VOX, else the English sample
        var vox = new Dictionary<string, HqrArchive?>();
        HqrArchive? Vox(string name)
        {
            if (vox.TryGetValue(name, out var a)) return a;
            return vox[name] = voxOnDisc.TryGetValue(name, out var f) ? HqrArchive.FromBytes(disc.Read(f)) : null;
        }
        var report = new StringBuilder();
        var englishSamples = new Dictionary<string, byte[]>();
        // each character's voice: their own lines in the retail game; else the retail character whose body they wear (VoiceOf, or the body
        // their lines are said in); else the game's texts their lines retell (an unseen voice's own recordings)
        var voices = new Dictionary<string, (List<(int File, int Id)> Lines, string Whose)>();
        foreach (var c in characters.Where(c => c != Unknown))
        {
            if (ownLines.TryGetValue(c, out var own)) { voices[c] = (own, c); continue; }
            if (NoStandIn.Contains(c)) continue;
            var stand = VoiceOf.TryGetValue(c, out var v) && ownLines.ContainsKey(v) ? v : null;
            // (the body a line is said in, but not where a text rule had to name the speaker: there the script's actor is someone else)
            stand ??= exported.Where(l => l.Character == c && l.Why != "text rule").SelectMany(l => l.Said).Select(x => x.Who.Voice ?? x.Who.Person)
                .Where(x => x is not null && x != c && ownLines.ContainsKey(x)).GroupBy(x => x!).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
            if (stand is not null) { voices[c] = (ownLines[stand], stand); continue; }
            var retells = exported.Where(l => l.Character == c).SelectMany(l => l.Retells.Select(r => (l.File, r))).Distinct().ToList();
            if (retells.Count > 0) voices[c] = (retells, "");
        }
        foreach (var lang in Enumerable.Range(0, languages).OrderBy(l => voiced.Contains(l) ? 0 : 1))
        {
            foreach (var character in characters)
            {
                var folder = Path.Combine(output, LanguageNames[lang], Safe(character));
                var source = voiced.Contains(lang) ? lang : 0;
                byte[]? wav = null;
                string from = "";
                var hasVoice = voices.TryGetValue(character, out var voice);
                if (source == lang && hasVoice)
                {
                    (wav, from) = Sample(voice.Lines, lang, retailBanks, Vox, seconds);
                    if (lang == 0 && wav is not null) englishSamples[character] = wav;
                }
                else if (source != lang && englishSamples.TryGetValue(character, out var english)) { wav = english; from = "the English sample"; }
                // (a voiced language whose recordings of them are missing or too short: the English sample too)
                if (wav is null && lang != 0 && englishSamples.TryGetValue(character, out var fallback)) { wav = fallback; from = $"the English sample (no {LanguageNames[lang]} recording of theirs long enough)"; }
                if (wav is not null)
                {
                    File.WriteAllBytes(Path.Combine(folder, "sample.wav"), wav);
                    report.Append($"{LanguageNames[lang]}\t{character}\t{(voice.Whose == character ? "" : voice.Whose.Length == 0 ? "(the lines theirs retell) " : $"({voice.Whose}) ")}{from}\n");
                    if (voice.Whose != character)
                        File.WriteAllText(Path.Combine(folder, "README.txt"), voice.Whose.Length == 0
                            ? $"{character} is unseen in the game; sample.wav is the retail game's own recordings of the lines theirs retell -- the same voice.\r\n"
                            : $"{character} has no lines of their own in the retail game; sample.wav is {voice.Whose}'s voice: " +
                              $"{(VoiceWhy.TryGetValue(character, out var why) ? why : $"the body {character} wears in the game is theirs")}. Pick another voice if it doesn't suit.\r\n", utf8);
                }
                else if (character != Unknown)
                    File.WriteAllText(Path.Combine(folder, "README.txt"),
                        $"{character} has no voice in the retail game{(voiced.Contains(lang) && lang != 0 && ownLines.ContainsKey(character) ? $" in {LanguageNames[lang]}" : "")}: no sample.wav. " +
                        $"A stand-in for the voice: {StandIn(character)}.\r\n", utf8);
            }
            if (!voiced.Contains(lang))
                File.WriteAllText(Path.Combine(output, LanguageNames[lang], "README.txt"),
                    $"LBA2 has no {LanguageNames[lang]} voices (its CD is voiced in {string.Join(", ", voiced.Select(l => LanguageNames[l]))} only), so each sample.wav in this folder is the character's English sample: " +
                    $"the voice to match, in {LanguageNames[lang]}. The .txt files are the {LanguageNames[lang]} words the game shows.\r\n", utf8);
        }

        // the index and what was left out
        var index = new StringBuilder("character\tfile\tbank\ttext_file\tid\tvox_entry\tvox_file\tstatus\tsaid_in\tenglish\n");
        foreach (var l in exported)
            index.Append($"{l.Character}\t{FileName(l)}\t{BankName(l.File)}\t{l.File}\t{l.Id}\t{l.Place}\t??_{VoxName(0, l.File)[3..]}\t" +
                         $"{(l.Replaced ? "replaced in place (the old recording plays under the new words)" : "added at the end (no recording: shown, not spoken)")}" +
                         $"{(l.OtherPlaces.Count == 0 ? "" : $"; another entry in {string.Join(", ", l.OtherPlaces)}")}\t" +
                         $"{string.Join(" ", l.Said.Select(s => $"scene {s.Scene} actor {s.Actor}").Distinct())}\t{l.English.Replace('\n', ' ')}\n");
        File.WriteAllText(Path.Combine(output, "index.tsv"), index.ToString(), utf8);
        var left = new StringBuilder("bank\ttext_file\tid\twhy\tenglish\n");
        foreach (var l in lines.Values.Where(l => l.Character.Length == 0).OrderBy(l => l.File).ThenBy(l => l.Id))
            left.Append($"{BankName(l.File)}\t{l.File}\t{l.Id}\t{l.Why}\t{l.English.Replace('\n', ' ')}\n");
        File.WriteAllText(Path.Combine(output, "left_out.tsv"), left.ToString(), utf8);
        File.WriteAllText(Path.Combine(output, "README.txt"), Readme(languages, voiced, characters, exported, lines.Count - exported.Count), utf8);
        File.WriteAllText(Path.Combine(output, "samples.tsv"), "language\tcharacter\tsample (whose voice, when not their own: length, the VOX files and lines it is from)\n" + report, utf8);

        Console.WriteLine($"newlines: {exported.Count} lines of {characters.Count} characters in {languages} languages written to {output} ({lines.Count - exported.Count} new texts left out: left_out.tsv)");
        foreach (var c in characters)
            Console.WriteLine($"  {c}: {exported.Count(l => l.Character == c)} lines{(c == Unknown ? "" : !voices.TryGetValue(c, out var v) ? " (no voice to sample)" : v.Whose == c ? "" : $" (sample: {(v.Whose.Length == 0 ? "the retail recordings of the lines theirs retell" : v.Whose + "'s voice")})")}");
        Console.WriteLine($"samples: {Path.Combine(output, "samples.tsv")}");
        return 0;
    }

    private static string FileName(NewLine l) => $"{BankName(l.File)}_{l.Id}.txt";

    private static string Safe(string name)
    {
        var s = new string(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
        return s.Length == 0 ? Unknown : s;
    }

    private static string StandIn(string character) => character switch
    {
        "FunFrock" => "the Dark Monk's voice (the retail game's FunFrock in disguise), or LBA1's FunFrock",
        "Unseen voice" => "Sendell's or the narrator's voice -- a voice off, not a person on screen",
        "School of Magic greeter" => "someone of the School of Magic: Flick's sample (the Dean's assistant) or Sad grobo student in the School of Magic's",
        "Souvenir seller" => "a Celebration Island islander's voice",
        "HAL" => "a calm, flat computer voice (LBA2's machines don't speak; HAL is 2001's), processed to sound synthetic",
        "The Tralu" => "a deep, rumbling voice -- the Tralu only roars in LBA2",
        "Stan" => "Time Commando's Stanley Opar (Adeline's other game; he never speaks in LBA2) -- a young man's voice",
        "Desert champion" => "a Desert racer's voice, e.g. Nomad grobo from Desert Island's sample",
        "The reptile-thing in the casino" => "Green gloom #1 in the casino's sample, a casino regular of Zeelich",
        _ when character.StartsWith("Stella", StringComparison.OrdinalIgnoreCase) => "a performer's voice close to the retail game's Sup girls",
        _ => "a retail character of the same kind (the body's people on the same island)",
    };

    // ---- the line's character: a text rule, else the script's speaker, else the retold game text's, else a thing's (left out), else none
    private static readonly Regex ToTwinsen = new(@"^(\W*|Hello\?\s*|Hey!?,?\s*|Hi,?\s*|Oh,?\s*)Twinsen\s*[,!?.]");

    private static void Decide(NewLine line, Dictionary<(int File, string English), List<int>> retold, Dictionary<(int File, int Id), List<string>> retailSaid)
    {
        var english = line.English;
        if (retold.TryGetValue((line.File, Key(english)), out var retells)) line.Retells = retells;
        // (a line to Twinsen in his own script -- "Twinsen, ..." -- is a voice he hears: the radio, a voice off)
        if (ToTwinsen.IsMatch(english)) line.Said.RemoveAll(s => s.Who.Person == "Twinsen" && s.How is "message" or "add_message");
        foreach (var (rule, character, why) in TextRules)
            if (rule.IsMatch(english))
            {
                if (character is null) { line.Character = ""; line.Why = why ?? "left out"; }
                else { line.Character = character; line.Why = "text rule"; }
                return;
            }
        // (its name in the story's tables: a story character's, or a line an unseen talker says for one)
        string? named = null;
        var story = false;
        if (line.Name is { } name)
        {
            if (NamedLines.TryGetValue(name, out var n)) { named = n; story = true; }
            else
            {
                var member = name[(name.IndexOf('.') + 1)..];
                if (NamePrefixes.FirstOrDefault(p => member.StartsWith(p.Prefix, StringComparison.Ordinal) &&
                                                      (member.Length == p.Prefix.Length || char.IsUpper(member[p.Prefix.Length]) || char.IsDigit(member[p.Prefix.Length]))) is { Prefix: not null } hit)
                    (named, story) = (hit.Character, hit.Story);
            }
        }
        if (named is not null && story) { line.Character = named; line.Why = $"its name ({line.Name})"; return; }
        var people = line.Said.Where(s => s.Who.Person is not null).GroupBy(s => s.Who.Person!).OrderByDescending(g => g.Count()).ToList();
        if (people.Count > 0)
        {
            line.Character = people[0].Key;
            line.Why = people.Count > 1 ? "said most by: " + string.Join(", ", people.Select(g => $"{g.Key} x{g.Count()}")) : "script";
            return;
        }
        if (named is not null) { line.Character = named; line.Why = $"its name ({line.Name}), said by no one seen"; return; }
        if (line.Retells is { Count: > 0 } games)
        {
            var who = games.Where(g => retailSaid.ContainsKey((line.File, g))).SelectMany(g => retailSaid[(line.File, g)]).GroupBy(w => w).OrderByDescending(g => g.Count()).FirstOrDefault();
            if (who is not null) { line.Character = who.Key; line.Why = $"retells the game's text {string.Join(", ", games)}"; return; }
        }
        if (line.Said.FirstOrDefault(s => s.Who.Thing is not null) is { } thing) { line.Character = ""; line.Why = $"said by a thing ({thing.Who.Thing})"; return; }
        if (line.File == GameFile)
        {
            // the game file: what is found and the inventory's descriptions are spoken (the narrator), names and labels shown
            var sentence = english.Length > 40 || Regex.IsMatch(english, @"[.!?]$");
            line.Character = sentence ? Narrator : "";
            line.Why = sentence ? "the game file's found message or description" : "a name or a label (shown)";
            return;
        }
        if (english.Length < 30 && !Regex.IsMatch(english, @"[.!?""]")) { line.Character = ""; line.Why = "a name or a label (shown)"; return; }
        line.Character = Unknown;
        line.Why = line.Said.Count == 0 ? "said by no script" : "said by an actor with no body";
    }

    // A line's words for matching the tables against the built texts (code page 850 loses some characters: letters and digits only)
    private static string Key(string words)
    {
        var k = Regex.Replace(words.ToLowerInvariant(), @"[^\p{L}\p{N}]", "");
        return k.Length > 120 ? k[..120] : k;
    }

    // The story's line tables, by their English words: each line's name ("DesertLine.FlickShut"). A table XLines is named by the enum XLine
    // of its class (CxLines, PalaceLines, VolcanoLines, FinalLines by CLine, PLine, VLine, FLine), its lines in the enum's order.
    private static Dictionary<string, string> LineNames()
    {
        var result = new Dictionary<string, string>();
        foreach (var type in typeof(RaceTrackStory).Assembly.GetTypes().Where(t => t.Namespace?.StartsWith("LBAAssembler.Terrain", StringComparison.Ordinal) == true))
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (field.FieldType != typeof(string[][]) || field.IsLiteral || field.GetValue(null) is not string[][] table) continue;
                var enumName = field.Name switch { "CxLines" => "CLine", "PalaceLines" => "PLine", "VolcanoLines" => "VLine", "FinalLines" => "FLine", var f when f.EndsWith('s') => f[..^1], var f => f };
                var e = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(n => n.IsEnum && n.Name == enumName);
                if (e is null) continue;
                for (var i = 0; i < table.Length; i++)
                    if (table[i].Length > 0 && Enum.GetName(e, i) is { } member) result.TryAdd(Key(Regex.Replace(table[i][0], @"\s*@\s*", "\n").Trim()), $"{e.Name}.{member}");
            }
        return result;
    }

    // IslandRetell's tables: each island's retold lines, by their English words, and the game's texts each retells
    private static Dictionary<(int File, string English), List<int>> Retold()
    {
        var files = new Dictionary<string, int>
        {
            ["Citadel"] = 3, ["Desert"] = 5, ["Moon"] = 6, ["Otringal"] = 7, ["Celebration"] = 8, ["Wannies"] = 9, ["Mosquibees"] = 10,
            ["Francos"] = 11, ["IslandCX"] = 12, ["Volcano"] = 14,
        };
        var result = new Dictionary<(int, string), List<int>>();
        foreach (var field in typeof(IslandRetell).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (field.GetValue(null) is not (int Retail, string[] Words)[] table || !files.TryGetValue(field.Name, out var file)) continue;
            foreach (var (game, words) in table)
            {
                var key = (file, Key(Regex.Replace(words[0], @"\s*@\s*", "\n").Trim()));
                if (!result.TryGetValue(key, out var list)) result[key] = list = new List<int>();
                list.Add(game);
            }
        }
        return result;
    }

    // setup.log's "the demo's characters 11 -> entity 345, 34 -> entity 346, ...": the built entities that are the 1996 demo's characters
    private static Dictionary<int, string> Demo(string built)
    {
        var result = new Dictionary<int, string>();
        var log = Path.Combine(built, "setup.log");
        if (!File.Exists(log)) return result;
        foreach (Match m in Regex.Matches(File.ReadAllText(log), @"the demo's characters ([^:\r\n]*):"))
            foreach (Match p in Regex.Matches(m.Groups[1].Value, @"(\d+) -> entity (\d+)"))
                if (DemoCharacters.TryGetValue(int.Parse(p.Groups[1].Value), out var name)) result[int.Parse(p.Groups[2].Value)] = name;
        return result;
    }

    // ---- who an actor is, in a game folder: its body's name (BODY2.HQD, the folder's race cars ...) through BodyRules
    private sealed class Bodies(string game, Dictionary<int, string> demo)
    {
        private readonly Lba2EntityTable? table = Lba2EntityTable.Load(game);
        private readonly IReadOnlyList<string?> names = Lba2BodyNames.For(game).Names;

        public Who Of(SceneModel model, int actor, string how, int[] scriptBody)
        {
            if (how == "message_zoe") return new Who("Zoe", null, "message_zoe");
            if (actor == 0) return new Who("Twinsen", null, how == "add_choice" ? "an answer" : "actor 0");
            if (actor >= model.Actors.Count) return new Who(null, null, $"actor {actor} past the scene's {model.Actors.Count}");
            var a = model.Actors[actor];
            if (a.Entity < 0 || a.IsSprite) return new Who(null, "a sprite", $"sprite {a.Sprite}");
            if (a.Entity == HelperEntity) return new Who(null, null, $"entity {a.Entity}: an unseen talker");
            if (demo.TryGetValue(a.Entity, out var demoName)) return new Who(demoName, null, $"entity {a.Entity}: the 1996 demo's");
            if (EntityCharacters.TryGetValue(a.Entity, out var entityName)) return new Who(entityName, null, $"entity {a.Entity}");
            // (Stella Entities' performers and staff: bodies of the hero's entity, each made from one of the game's -- StellaPerformers, StellaStaff)
            if (a.Entity == StellaPerformers.Entity)
            {
                var g = a.Body < -1 ? a.Body & 0xFF : a.Body;
                var k = g - StellaPerformers.Body0;
                if (k >= 0 && k < StellaPerformers.All.Length)
                {
                    var spec = StellaPerformers.All[k];
                    return new Who($"Stella performer ({Regex.Replace(spec.Name, @"^an? ", "")})", null, $"entity 0 body {g}: Stella Entities' performer {k}", Character(names, spec.Source));
                }
                if (g is StellaStaff.Bunny or StellaStaff.BunnyTray or StellaStaff.BunnyThong)
                    return new Who("Stella hostess (rabbibunny)", null, $"entity 0 body {g}: Stella Entities' staff", Character(names, 124));
                if (g == StellaStaff.GroboHost)
                    return new Who("Stella hostess (grobo)", null, $"entity 0 body {g}: Stella Entities' staff", Character(names, 122));
            }
            var e = table?.Entities.FirstOrDefault(x => x.Id == a.Entity);
            var generic = a.Body < -1 ? a.Body & 0xFF : a.Body;     // (a generic body past 127, read as a signed byte)
            if (generic < 0 && a.Entity != HelperEntity && actor < scriptBody.Length) generic = scriptBody[actor];
            var match = e?.Bodies.Where(b => b.Generic == generic).ToList();
            if (generic < 0 || match is null || match.Count == 0) return new Who(null, null, $"entity {a.Entity} body {a.Body}: no body (unseen)");
            var body = match[0].Body;
            var name = body >= 0 && body < names.Count ? names[body] : null;
            var detail = $"entity {a.Entity} body {a.Body} = {body}: {name ?? "?"}";
            if (name is null || Regex.IsMatch(name, @"^Body \d+ of entity")) return new Who(null, null, detail);
            var (person, thing) = Named(name);
            return new Who(person, thing, detail);
        }

        // a body's character (BODY.HQR's index), as its name gives it
        private static string? Character(IReadOnlyList<string?> names, int body) => body >= 0 && body < names.Count && names[body] is { } n ? Named(n).Person : null;

        private static (string? Person, string? Thing) Named(string name)
        {
            foreach (var (rule, character) in BodyRules)
                if (rule.IsMatch(name)) return character is null ? (null, name) : (character, null);
            var clean = Regex.Replace(name, @": body \d+ of its kind of actor \(entity \d+\)$", "");
            clean = Regex.Replace(clean, @"\s*\([^)]*\)", "").Trim();
            foreach (var (rule, character) in BodyRules)
                if (rule.IsMatch(clean)) return character is null ? (null, clean) : (character, null);
            return (clean, null);
        }
    }

    // ---- a character's sample: their retail lines' recordings in this language, one after the other, up to `seconds`
    private static (byte[]? Wav, string From) Sample(List<(int File, int Id)> own, int lang, Dictionary<(int Lang, int File), List<Text>> retailBanks,
        Func<string, HqrArchive?> vox, double seconds)
    {
        var pcm = new List<short>();
        var used = new Dictionary<string, int>();
        var gap = new short[(int)(Rate * Gap)];
        foreach (var (file, id) in own)
        {
            if (pcm.Count >= seconds * Rate) break;
            var place = retailBanks[(lang, file)].FindIndex(t => t.Id == id);
            var name = VoxName(lang, file);
            var archive = vox(name);
            if (place < 0 || archive is null || !archive.IsValid(place)) continue;
            short[]? clip;
            try { clip = Decode(archive.Read(place)); }
            catch (Exception e) when (e is InvalidDataException or ArgumentException or IndexOutOfRangeException) { clip = null; }
            if (clip is null || clip.Length < Shortest * Rate) continue;
            if (pcm.Count > 0) pcm.AddRange(gap);
            pcm.AddRange(clip);
            used[name] = used.GetValueOrDefault(name) + 1;
        }
        if (pcm.Count == 0) return (null, "");
        return (Wav(pcm), $"{pcm.Count / (double)Rate:0.0} s: {string.Join(", ", used.Select(u => $"{u.Key} ({u.Value} lines)"))}");
    }

    // A VOX entry: a RIFF WAV -- LBA2's are IMA ADPCM, 22050 Hz mono -- whose first byte is a flag, not the 'R' (MESSAGE.CPP: FlagNextVoc,
    // 1 when the text's recording goes on in a further entry right after it, not in the index), or a Creative Voice File (the same flag
    // for its 'C'); as 16-bit mono at Rate. (Of a recording in parts, the first.)
    private static short[]? Decode(byte[] data)
    {
        if (data.Length > 44 && Encoding.ASCII.GetString(data, 1, 3) == "IFF" && Encoding.ASCII.GetString(data, 8, 4) == "WAVE") return FromWav(data);
        if (data.Length > 26 && Encoding.ASCII.GetString(data, 1, 18) == "reative Voice File") return FromVoc(data);
        return null;
    }

    private static short[]? FromVoc(byte[] voc)
    {
        int at = BitConverter.ToUInt16(voc, 20);
        var samples = new List<short>();
        var rate = 0;
        var bits = 8;
        while (at + 4 <= voc.Length)
        {
            var type = voc[at];
            if (type == 0) break;
            var size = voc[at + 1] | voc[at + 2] << 8 | voc[at + 3] << 16;
            var body = at + 4;
            if (body + size > voc.Length) size = voc.Length - body;
            switch (type)
            {
                case 1 when size >= 2:   // sound data: frequency divisor, codec (0 = 8-bit unsigned)
                    if (voc[body + 1] != 0) return null;
                    if (rate == 0) rate = 1_000_000 / (256 - voc[body]);
                    for (var i = body + 2; i < body + size; i++) samples.Add((short)((voc[i] - 128) << 8));
                    break;
                case 2:                  // continuation
                    if (bits == 8) for (var i = body; i < body + size; i++) samples.Add((short)((voc[i] - 128) << 8));
                    else for (var i = body; i + 1 < body + size; i += 2) samples.Add(BitConverter.ToInt16(voc, i));
                    break;
                case 3 when size >= 3:   // silence
                    samples.AddRange(new short[(voc[body] | voc[body + 1] << 8) + 1]);
                    break;
                case 9 when size >= 12:  // sound data, new format: rate, bits, channels, codec (0 = 8-bit unsigned, 4 = 16-bit signed)
                    {
                        if (rate == 0) rate = BitConverter.ToInt32(voc, body);
                        bits = voc[body + 4];
                        var channels = voc[body + 5];
                        int codec = BitConverter.ToUInt16(voc, body + 6);
                        if (channels != 1 || codec is not (0 or 4)) return null;
                        if (bits == 8) for (var i = body + 12; i < body + size; i++) samples.Add((short)((voc[i] - 128) << 8));
                        else for (var i = body + 12; i + 1 < body + size; i += 2) samples.Add(BitConverter.ToInt16(voc, i));
                    }
                    break;
            }
            at = body + size;
        }
        return rate == 0 ? null : Resample(samples, rate);
    }

    private static short[]? FromWav(byte[] wav)
    {
        int at = 12, rate = 0, bits = 0, channels = 0, format = 0, blockAlign = 0;
        while (at + 8 <= wav.Length)
        {
            var id = Encoding.ASCII.GetString(wav, at, 4);
            var size = BitConverter.ToInt32(wav, at + 4);
            var body = at + 8;
            if (id == "fmt ")
            {
                format = BitConverter.ToUInt16(wav, body);
                if (format is not (1 or 0x11)) return null;
                channels = BitConverter.ToUInt16(wav, body + 2);
                rate = BitConverter.ToInt32(wav, body + 4);
                blockAlign = BitConverter.ToUInt16(wav, body + 12);
                bits = BitConverter.ToUInt16(wav, body + 14);
                if (channels != 1) return null;
            }
            else if (id == "data" && rate > 0 && format == 0x11)
                return Resample(ImaAdpcm(wav, body, Math.Min(wav.Length, body + size), blockAlign), rate);
            else if (id == "data" && rate > 0)
            {
                var samples = new List<short>();
                var step = channels * bits / 8;
                for (var i = body; i + step <= Math.Min(wav.Length, body + size); i += step)
                    samples.Add(bits == 8 ? (short)((wav[i] - 128) << 8) : BitConverter.ToInt16(wav, i));
                return Resample(samples, rate);
            }
            at = body + size + (size & 1);
        }
        return null;
    }

    // IMA ADPCM (WAVE_FORMAT_DVI_ADPCM), mono: each block a 4-byte header (the first sample, the step index) then two samples a byte,
    // the low nibble first
    private static readonly int[] ImaSteps =
    {
        7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45, 50, 55, 60, 66, 73, 80, 88, 97, 107, 118, 130, 143, 157, 173,
        190, 209, 230, 253, 279, 307, 337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878,
        2066, 2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
        15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767,
    };
    private static readonly int[] ImaIndex = { -1, -1, -1, -1, 2, 4, 6, 8, -1, -1, -1, -1, 2, 4, 6, 8 };

    private static List<short> ImaAdpcm(byte[] data, int from, int to, int blockAlign)
    {
        var samples = new List<short>();
        if (blockAlign <= 4) return samples;
        for (var block = from; block + 4 <= to; block += blockAlign)
        {
            int predictor = BitConverter.ToInt16(data, block), index = Math.Clamp((int)data[block + 2], 0, 88);
            samples.Add((short)predictor);
            var end = Math.Min(to, block + blockAlign);
            for (var i = block + 4; i < end; i++)
                foreach (var nibble in new[] { data[i] & 0x0F, data[i] >> 4 })
                {
                    var step = ImaSteps[index];
                    var diff = step >> 3;
                    if ((nibble & 4) != 0) diff += step;
                    if ((nibble & 2) != 0) diff += step >> 1;
                    if ((nibble & 1) != 0) diff += step >> 2;
                    predictor = Math.Clamp((nibble & 8) != 0 ? predictor - diff : predictor + diff, short.MinValue, short.MaxValue);
                    index = Math.Clamp(index + ImaIndex[nibble], 0, 88);
                    samples.Add((short)predictor);
                }
        }
        return samples;
    }

    private static short[] Resample(List<short> samples, int rate)
    {
        if (rate == Rate || samples.Count == 0) return samples.ToArray();
        var count = (int)((long)samples.Count * Rate / rate);
        var result = new short[count];
        for (var i = 0; i < count; i++)
        {
            var x = (double)i * rate / Rate;
            var k = (int)x;
            var f = x - k;
            var a = samples[Math.Min(k, samples.Count - 1)];
            var b = samples[Math.Min(k + 1, samples.Count - 1)];
            result[i] = (short)Math.Round(a + (b - a) * f);
        }
        return result;
    }

    private static byte[] Wav(List<short> pcm)
    {
        var data = pcm.Count * 2;
        var wav = new MemoryStream(44 + data);
        var w = new BinaryWriter(wav);
        w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + data);
        w.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16);
        w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
        w.Write(Encoding.ASCII.GetBytes("data")); w.Write(data);
        foreach (var s in pcm) w.Write(s);
        return wav.ToArray();
    }

    // ---- a text file of TEXT.HQR (the engine's file numbers: island 12's, file 15, is in entries after the retail ones: PolarScenes)
    private static List<Text> LoadBank(HqrArchive text, int lang, int file)
    {
        var order = file < RetailFiles ? Lba2TextBank.IdsEntry(lang, file) : LBAAssembler.Terrain.Polar.PolarScenes.TextEntry(lang);
        var bank = new List<Text>();
        if (!text.IsValid(order) || !text.IsValid(order + 1)) return bank;
        var ids = text.Read(order);
        var data = text.Read(order + 1);
        for (var i = 0; i < ids.Length / 2; i++)
        {
            int off0 = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(i * 2));
            int off1 = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(i * 2 + 2));
            var bytes = Array.Empty<byte>();
            if (off1 > off0 && off1 <= data.Length)
            {
                var end = off1;
                while (end > off0 + 1 && data[end - 1] == 0) end--;
                bytes = data[(off0 + 1)..end];
            }
            bank.Add(new Text(BinaryPrimitives.ReadUInt16LittleEndian(ids.AsSpan(i * 2)), i, bytes));
        }
        return bank;
    }

    // A text's words as the game shows them: code page 850, '@' a new paragraph
    private static string Words(byte[] bytes) => Regex.Replace(Dos.GetString(bytes), @"\s*@\s*", "\n").Trim();

    // ---- every text a scene's life scripts say: its file, the scene, the actor who says it, how, the text id
    // (and each actor's first body(N) in its own life script: who an actor with no body in the scene becomes once it shows)
    private static IEnumerable<(int File, int Scene, int Actor, string How, int Id, SceneModel Model, int[] ScriptBody)> SaidTexts(string game)
    {
        var store = new SceneStore(SceneGame.Lba2, game);
        var hqr = HqrArchive.Open(store.ScenePath);
        for (var scene = 0; scene < store.SceneCount; scene++)
        {
            if (!hqr.IsValid(store.Entry(scene))) continue;
            SceneModel model;
            SceneScripts scripts;
            try { var record = hqr.Read(store.Entry(scene)); model = SceneSerializer.Parse(SceneGame.Lba2, record); scripts = SceneScripts.Load(record, scene); }
            catch (Exception e) when (e is InvalidDataException or SceneEditException or ArgumentException or IndexOutOfRangeException) { continue; }
            var file = model.Island == 12 ? RetailFiles : model.Island + 3;
            var lives = new string?[scripts.ActorCount];
            for (var actor = 0; actor < scripts.ActorCount; actor++)
            {
                try { lives[actor] = scripts.GetText(actor, ScriptKind.Life); }
                catch (Exception e) when (e is InvalidDataException or ArgumentException or IndexOutOfRangeException or InvalidOperationException) { }
            }
            var scriptBody = lives.Select(l => l is not null && Regex.Match(l, @"\bbody\((\d+)\)") is { Success: true } b ? int.Parse(b.Groups[1].Value) : -1).ToArray();
            for (var actor = 0; actor < scripts.ActorCount; actor++)
            {
                if (lives[actor] is not { } life) continue;
                foreach (Match m in Says.Matches(life))
                {
                    var how = m.Groups[1].Value;
                    var before = Regex.Matches(m.Groups[2].Value, @"\d+").Select(x => int.Parse(x.Value)).ToList();
                    var speaker = how switch
                    {
                        "message_obj" or "ask_choice_obj" or "add_message_obj" when before.Count >= 1 => before[0],
                        "pcx_mess_obj" when before.Count >= 3 => before[2],
                        "add_choice" => 0,
                        _ => actor,
                    };
                    yield return (file, scene, speaker, how, int.Parse(m.Groups[3].Value), model, scriptBody);
                }
            }
        }
    }

    private static string Readme(int languages, List<int> voiced, List<string> characters, List<NewLine> exported, int leftOut)
    {
        var s = new StringBuilder();
        s.Append("Little Big Racing Adventure: the new lines, for recording voices for them\r\n");
        s.Append("=========================================================================\r\n\r\n");
        s.Append("Made by LBA Assembler's tools\\ScriptRoundTrip: `ScriptRoundTrip newlines` (it wipes this folder and writes it again).\r\n\r\n");
        s.Append("Layout\r\n------\r\n");
        s.Append("  <Language>\\<Character>\\<bank>_<id>.txt   one new line, only its words, in that language (UTF-8; a new paragraph in the game\r\n");
        s.Append("                                           is a new line here). One recording per file.\r\n");
        s.Append("  <Language>\\<Character>\\sample.wav        the character's own voice from the retail game, some of their lines one after\r\n");
        s.Append("                                           another (16-bit mono, 22050 Hz): the voice to match.\r\n");
        s.Append($"  Languages: {string.Join(", ", Enumerable.Range(0, languages).Select(l => LanguageNames[l]))} -- the six the game's texts (TEXT.HQR) carry.\r\n");
        s.Append($"  LBA2 is voiced in {string.Join(", ", voiced.Select(l => LanguageNames[l]))} only: the other languages' sample.wav files are the English ones\r\n");
        s.Append("  (their folders' README.txt says so). A character the retail game never voiced has no sample.wav, a README.txt instead.\r\n");
        s.Append("  _Unknown: lines whose speaker couldn't be told (no script says them, or an unseen actor does): listen in game and move them.\r\n\r\n");
        s.Append("From a file back to the game\r\n----------------------------\r\n");
        s.Append("  <bank>_<id>: the text's id in the island's text file of TEXT.HQR (file = 3 + the island; Polar Island's is the engine's file 15):\r\n");
        foreach (var file in Enumerable.Range(GameFile, RetailFiles - GameFile + 1))
            s.Append($"    {BankName(file),-12} file {file,2}   voices {VoxName(0, file)[3..]} (EN_{VoxName(0, file)[3..]}, FR_..., DE_..., SP_..., IT_..., PO_...)\r\n");
        s.Append("  The game plays a text's recording from its island's VOX file (VOX\\<EN|FR|DE|SP|IT|PO>_<nnn>.VOX, an HQR of samples), the entry at\r\n");
        s.Append("  the text's PLACE in the file's list -- not its id (MESSAGE.CPP Speak). index.tsv gives each line's place (vox_entry). The mod's\r\n");
        s.Append("  lines are added at the end of the list, past the last recording, so they are shown, not spoken: a recording goes in at that\r\n");
        s.Append("  entry (the entries between filled too). A few replace a game text in place (status in index.tsv): their entry has the old\r\n");
        s.Append("  recording, to be replaced. A Spanish, Italian or Portuguese recording needs that language's VOX files, which LBA2 never had.\r\n\r\n");
        s.Append("Files\r\n-----\r\n");
        s.Append("  index.tsv     every exported line: character, file name, bank, text file, id, VOX entry, VOX file, status, where said, English\r\n");
        s.Append($"  left_out.tsv  the {leftOut} new texts not exported, and why: things described, signs, names and labels -- no one's voice\r\n");
        s.Append("  samples.tsv   each sample.wav: whose voice it is, how long, which VOX files and how many lines it is made of\r\n\r\n");
        s.Append("Characters (lines per language)\r\n-------------------------------\r\n");
        foreach (var c in characters) s.Append($"  {c}: {exported.Count(l => l.Character == c)}\r\n");
        return s.ToString();
    }
}
