namespace LBAAssembler.Terrain;

// The story's easter eggs (the user, 2026-11-03: "come up with small easter eggs we can add into each island that fits in with the game,
// they can be small subtle things, or large hidden things"; 2026-11-04, of the proposal's picks: "Let's make all those changes"). Each
// egg is built where its island's story is (RaceTrackStory.<island>.cs, RaceTrackStory.Chapters.cs for Citadel Island), its lines at the
// end of that island's lines; what they share is here: the game variables that keep what was found from chapter to chapter.
//
// The variables: game variables no script of the built game uses and the engine doesn't (checked 2026-11-04 with scriptgrep2 on a full
// build of the fifteen story islands, and the engine source for ListVarGame[...]): 196-199, 210, 225, 226, 247, 250, and 153-155.
// (235-237 are the videos seen, PLAYACF.CPP FLAG_ACF; 251-254 the engine's; 75, 96, 106, 114, 172 are free too, kept spare -- 172 is
// read by scene 62's dead branch; 52 is the final's: another race asked for once, RaceTrackStory.Final.cs FinalAskedAgain; 58 chapter 1's
// evening's, RaceTrackStory.Evening.cs QEvening; 118 the bait's, Chapters.cs QBait.)
internal static partial class RaceTrackStory
{
    // the Little Big Postcards: how many found (0-13), and the chapter the last one was found in (each chapter's card is there while
    // the chapter is, and only while this is below it: found once)
    public const int EggCards = 196, EggLastCard = 197;
    // the Desert's fourth exam page: 1 found, 2 given to the Dean, 3 Flick has denied it
    public const int EggPage = 198, PageFound = 1, PageGiven = 2, PageDenied = 3;
    // Bù's lap record: 1 beaten (the race-track mode's lap_record=), 2 the Dean's 50 kashes given
    public const int EggBu = 199, BuBeaten = 1, BuPaid = 2;
    // the fifth Dark Monk statuette: 1 found on the lava lake, 2 shown to the seller (and sitting on the statue's ring in the final)
    public const int EggStatuette = 210, StatuetteFound = 1, StatuetteShown = 2;
    // the Brune sisters: 1 all four spoken to in the palace dungeon (they're in the final's crowd with Mrs. Brune)
    public const int EggSisters = 155;
    // the Elevator Platform's monster beaten by ten seconds (the race-track mode's margin_win=): 1 (his "BLUE ONE" sign in the final's crowd)
    public const int EggMonster = 225;
    // Island CX's race finished without one cannon hit (the race-track mode's no_hit=): 1, 2 once the trooper and Baldino have said so
    public const int EggGrey = 226;
    // Sendell's light on the Polar dream's loop: 1 driven through (her line at the white changes)
    public const int EggSendell = 247;
    // the Weather Wizard's weather for the town circuit (chapter 2): 0 sunshine, 1 rain, 2 snow (the race-track mode's weather_var=)
    public const int EggWeather = 250, WeatherSun = 0, WeatherRain = 1, WeatherSnow = 2;
    // the clover boxes of the LBA1 rooms: the sewers' secret room (Citadel 1), the Tavern's cellar (Citadel 2): 1 taken
    public const int EggSewerClover = 153, EggCellarClover = 154;
    // (212 is the race file's: RaceTrackFrog.DecorVar hides Wannies' still frog -- the sisters took 155)

    // ---- the race file's keys for the eggs (RaceCarEngineFile, in the story) ----------------------------------------------------------
    // Bù's lap record, 1:02 (the Desert's tablet); the monster beaten by ten seconds; not one of Island CX's missiles; the Wizard's weather
    // on the town circuit; and the Tortoise and the Hare on the Island of the Francos: Twinsen still on the grid TortoiseSeconds after GO,
    // De La Fontaine -- the race's one to beat, in his tanker, and the fable's own teller -- comes by at a walk, says its moral (EggText),
    // and the race starts again. (Moya the turtle isn't in the Francos' line-up: De La Fontaine is.)
    public const int BuRecordMs = 62000, MonsterMarginMs = 10000, TortoiseSeconds = 30;
    private const string TortoiseDriver = "De La Fontaine";
    internal static IEnumerable<string> EggRaceKeys(RaceTrackIsland island, bool town, RaceTrackService.StoryInfo? story, IList<string> opponents)
    {
        if (island == RaceTrackIsland.Desert) yield return $"lap_record={BuRecordMs} {EggBu} {BuBeaten}";
        if (island == RaceTrackIsland.Elevator) yield return $"margin_win={MonsterMarginMs} {EggMonster} 1";
        if (island == RaceTrackIsland.CxTowerMk2 || island == RaceTrackIsland.CxTower) yield return $"no_hit={EggGrey} 1";
        if (town) yield return $"weather_var={EggWeather}";
        // (the final's: the seller's reward for the thirteen postcards -- RaceTrackStory.Eggs.Postcards.cs)
        if (island == RaceTrackIsland.Celebration) yield return $"unlock_at={EggLastCard} {PostcardsRewarded} {PostcardUnlock}";
        if (island == RaceTrackIsland.Knartas && story is { EggText: >= 0 } s && opponents.IndexOf(TortoiseDriver) is var k and >= 0)
            yield return $"tortoise={TortoiseSeconds} {k + 1} {s.EggText}";
    }

    // The eggs' part of an island's story (RaceTrackService.Story, after the island's own): what the race file needs to know of them, or null
    private static readonly string[][] TortoiseMoral =
    {
        new[]
        {
            "De La Fontaine trundles past in his tanker: \"Rien ne sert de courir; il faut partir à point.\" No use running, Twinsen, if you never set off!",
            "De La Fontaine passe tranquillement dans sa citerne : « Rien ne sert de courir ; il faut partir à point. » Inutile de courir, Twinsen, si tu ne pars jamais !",
            "De La Fontaine zuckelt in seinem Tankwagen vorbei: \"Rien ne sert de courir; il faut partir à point.\" Rennen nützt nichts, Twinsen, wenn man nie losfährt!",
            "De La Fontaine pasa despacito en su camión cisterna: \"Rien ne sert de courir; il faut partir à point.\" ¡De nada sirve correr, Twinsen, si nunca sales!",
            "De La Fontaine passa piano piano con la sua cisterna: \"Rien ne sert de courir; il faut partir à point.\" Non serve correre, Twinsen, se non parti mai!",
            "De La Fontaine passa devagarinho no seu camião-cisterna: \"Rien ne sert de courir; il faut partir à point.\" De nada serve correr, Twinsen, se nunca partes!",
        },
    };
    internal static RaceTrackService.StoryInfo? EggInfo(string gameDirectory, RaceTrackIsland island, List<string> log)
    {
        if (island != RaceTrackIsland.Knartas) return null;
        var moral = GroupB.AddLines(gameDirectory, 3 + island.IslandByte, TortoiseMoral, log, "the Tortoise and the Hare's moral (tortoise=):");
        return new RaceTrackService.StoryInfo(-1, -1, EggText: moral);
    }
}
