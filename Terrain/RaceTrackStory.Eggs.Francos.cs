namespace LBAAssembler.Terrain;

// The Island of the Francos' easter egg (RaceTrackStory.Eggs.cs; the user, 2026-11-04: "Let's make all those changes", of the proposal's
// picks): the horror. The proposal: "Mr. Kurtz, given the gazogem to smell: 'The horror... the horror...' A beat. 'No, wait, that's the
// nurse's cooking. The fuel's fine.'" -- LBA2's Kurtz is Heart of Darkness's (and Apocalypse Now's), whose last words are "The horror!
// The horror!". Said as he reads the sample with his glasses found (Q_FUEL 4, the nurse's word), before what he reads in it.
internal static partial class RaceTrackStory
{
    // Mr. Kurtz's two lines, for his talk (ApplyFrancos), before KurtzHead
    private static string KurtzHorror(Func<FrancosLine, int> say) => $@"                message({say(FrancosLine.KurtzHorror)});
                message({say(FrancosLine.KurtzCooking)});
";
}
