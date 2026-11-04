using LbaBodyStudio;

namespace LBAAssembler.Terrain;

// The gold black car (the user, 2026-11-04, of the easter-egg proposal's picks: "Let's make all those changes" -- the Little Big
// Postcards' reward, RaceTrackStory.Eggs.Postcards.cs): the souvenir seller's souvenir of the masked driver's car, FunFrock's own, in gold --
// "I made a souvenir of that black car too, in gold. Nobody bought it." Its red is gold (the red ramp's shades the gold ramp's), its gold
// as it was. Made and installed as the Halloween monsters are (InstallMonsters, at the very end of every build), race car GoldCarNumber
// of RACECARS.JSON, its island the postcards' (LBRA's installer: won by the race-track mode's unlock_at=, "postcards").
internal static partial class RaceTrackCharacterCars
{
    public const int GoldCarNumber = 228, GoldCarGeneric = 72;
    public const string Postcards = "The Little Big Postcards";
    public static readonly Car GoldCar = new("The gold black car", "the souvenir seller's souvenir", 313, GoldCarGeneric, BuildGoldCar, Postcards);

    private const int RedRamp = 64, GoldRamp = 96;
    public static Body BuildGoldCar(Body funfrock, Body racer)
    {
        var car = BuildFunFrock(funfrock, racer);
        static int Gilded(int c) => c >= RedRamp && c < RedRamp + 16 ? GoldRamp + (c - RedRamp) : c;
        for (var i = 0; i < car.Faces.Count; i++) car.Faces[i] = car.Faces[i] with { Colour = Gilded(car.Faces[i].Colour) };
        for (var i = 0; i < car.Lines.Count; i++) car.Lines[i] = car.Lines[i] with { Colour = Gilded(car.Lines[i].Colour) };
        for (var i = 0; i < car.Spheres.Count; i++) car.Spheres[i] = car.Spheres[i] with { Colour = Gilded(car.Spheres[i].Colour) };
        return car;
    }
}
