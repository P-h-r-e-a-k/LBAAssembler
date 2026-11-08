using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// Citadel Island's easter eggs (the user, 2026-11-04, of the proposal's picks: "Let's make all those changes" -- RaceTrackStory.Eggs.cs):
//   - The storm night through the binoculars (chapter 1): after the spell, before bed, the lighthouse's telescope doesn't show the Wizard
//     and the cow -- it shows the Esmers' lights coming down through the clouds over the sea, and on the ship's ramp, small and black, a
//     car (the masked driver's first sighting is otherwise chapter 2). The ship is Otringal's shuttle (scene 92's, entity 47), its lights
//     flickering as its own scripts flicker them (bodies 58 and 59), sinking over the sea west of the lighthouse.
//   - The Wizard's encore (chapter 2): the Weather Wizard by the town circuit's start line, pleased with himself -- "Any weather you like
//     for your race" -- sunshine, rain or snow (EggWeather: the race-track mode's weather_var= rains or snows on the race).
//   - Mrs. Brune's sisters (chapter 2): asked again at the ferry office, her four sisters went to Zeelich on holiday and never wrote -- the
//     palace dungeon's four rabbibunny captives (RaceTrackStory.Palace's eggs, the Brune sisters).
// Their lines at the end of Citadel Island's texts (GroupB.AddLines), after the story's.
internal static partial class RaceTrackStory
{
    private enum CitadelEggLine { StormLights, StormCar, WizardAsk, Sunshine, Rain, Snow, WizardSun, WizardRain, WizardSnow, BruneSisters }
    private static readonly string[][] CitadelEggLines =
    {
        new[]
        {
            "Lights... coming down through the clouds, out over the sea.",
            "Des lumières... qui descendent à travers les nuages, au-dessus de la mer.",
            "Lichter... sie kommen durch die Wolken herunter, draußen über dem Meer.",
            "Luces... bajando entre las nubes, sobre el mar.",
            "Delle luci... scendono tra le nuvole, sopra il mare.",
            "Luzes... a descer por entre as nuvens, sobre o mar.",
        },
        new[]
        {
            "And on its ramp, something small and black... A car? Up there?",
            "Et sur sa rampe, quelque chose de petit et de noir... Une voiture ? Là-haut ?",
            "Und auf seiner Rampe etwas Kleines, Schwarzes... Ein Auto? Da oben?",
            "Y en su rampa, algo pequeño y negro... ¿Un coche? ¿Ahí arriba?",
            "E sulla sua rampa, qualcosa di piccolo e nero... Una macchina? Lassù?",
            "E na rampa, uma coisa pequena e preta... Um carro? Lá em cima?",
        },
        new[]
        {
            "Any weather you like for your race, Twinsen. I'm on a roll!",
            "Le temps que tu veux pour ta course, Twinsen. Je suis en forme !",
            "Jedes Wetter, das du willst, für dein Rennen, Twinsen. Ich bin in Fahrt!",
            "El tiempo que quieras para tu carrera, Twinsen. ¡Estoy en racha!",
            "Il tempo che vuoi per la tua corsa, Twinsen. Sono in vena!",
            "O tempo que quiseres para a tua corrida, Twinsen. Estou em maré de sorte!",
        },
        new[] { "Sunshine", "Soleil", "Sonnenschein", "Sol", "Sole", "Sol" },
        new[] { "Rain", "Pluie", "Regen", "Lluvia", "Pioggia", "Chuva" },
        new[] { "Snow", "Neige", "Schnee", "Nieve", "Neve", "Neve" },
        new[]
        {
            "Sunshine it is. You can't improve on perfection.",
            "Va pour le soleil. On ne fait pas mieux que la perfection.",
            "Also Sonnenschein. Perfektion kann man nicht verbessern.",
            "Pues sol. La perfección no se puede mejorar.",
            "E sole sia. La perfezione non si migliora.",
            "Sol, então. Não se melhora a perfeição.",
        },
        new[]
        {
            "Rain! Just like old times. Mind the puddles.",
            "De la pluie ! Comme au bon vieux temps. Attention aux flaques.",
            "Regen! Wie in alten Zeiten. Pass auf die Pfützen auf.",
            "¡Lluvia! Como en los viejos tiempos. Cuidado con los charcos.",
            "Pioggia! Come ai vecchi tempi. Attento alle pozzanghere.",
            "Chuva! Como nos velhos tempos. Cuidado com as poças.",
        },
        new[]
        {
            "Snow! On Citadel Island! Let's see what the Esmers make of that.",
            "De la neige ! Sur l'île de la Citadelle ! Voyons ce qu'en pensent les Esmers.",
            "Schnee! Auf der Zitadelleninsel! Mal sehen, was die Esmer dazu sagen.",
            "¡Nieve! ¡En la isla de la Ciudadela! A ver qué opinan los Esmers.",
            "Neve! Sull'isola della Cittadella! Vediamo cosa ne pensano gli Esmer.",
            "Neve! Na ilha da Cidadela! Vamos ver o que os Esmers acham disto.",
        },
        new[]
        {
            "My four sisters went to Zeelich on holiday and never wrote. Not one postcard!",
            "Mes quatre sœurs sont parties en vacances sur Zeelich et n'ont jamais écrit. Pas une carte postale !",
            "Meine vier Schwestern sind nach Zeelich in den Urlaub gefahren und haben nie geschrieben. Nicht eine Postkarte!",
            "Mis cuatro hermanas se fueron de vacaciones a Zeelich y nunca escribieron. ¡Ni una postal!",
            "Le mie quattro sorelle sono andate in vacanza su Zeelich e non hanno mai scritto. Neanche una cartolina!",
            "As minhas quatro irmãs foram de férias para Zeelich e nunca escreveram. Nem um postal!",
        },
    };

    // the storm night: the lighthouse's telescope sets var_cube StormLook to 1 in chapter 1 (LighthouseClimb); the ship -- scene 92's
    // shuttle, its actor 2 -- sinks down its track points over the sea west of the lighthouse while the camera follows it
    internal const int StormLook = 50;
    private const int SpacePortScene = 92, SpacePortShip = 2, ShipEntity = 47, ShipLook = 10, ShipLooked = 11;
    // (its way down: straight down out of the clouds to hover over the waves, a little each fifth of a second, its lights flickering -- the
    // follow camera centred on it as the shot starts keeps it in sight: drifting sideways, it went out of the picture)
    private static readonly (int X, int Y, int Z) ShipFrom = (3000, 7200, 9800), ShipTo = (3000, 4600, 9800);
    private const int ShipSteps = 26;
    // (its camera zone: alpha, beta, distance -- from the lighthouse's side, looking out to sea)
    private const int ShipCamera = 72;
    private static readonly int[] ShipShot = { 120, 2048, 16000 };
    // (still, its rest animation: the track's steps put it where it is, a fifth of a second apart. The animation it was copied with, 211,
    // is its take-off -- it rose out of the picture between the steps; its hover, 213, moves it too: it drifted between the steps, and
    // after the look it climbed on out of the island for good -- the user, 2026-11-07: the ship "didn't seem to appear correctly", and a
    // second look left Twinsen frozen)
    private const int ShipHover = 0;
    // the Wizard's encore and Mrs. Brune's second word: per-visit flags of their scenes' own (var_cube)
    private const int BruneSaid = 50;

    private static void CitadelEggs(StoryParts p, string gameDirectory)
    {
        var first = GroupB.AddLines(gameDirectory, CitadelTexts, CitadelEggLines, p.Log, "Citadel Island's easter eggs'");
        int Say(CitadelEggLine l) => first + (int)l;

        // ---- the storm night through the binoculars ----
        var ship = Copy(p.Originals, SpacePortScene, SpacePortShip);
        if (ship is null || ship.Entity != ShipEntity) p.Log.Add($"the storm night through the binoculars left out: no shuttle (scene {SpacePortScene}'s actor {SpacePortShip}, entity {ShipEntity})");
        else p.Edit(LighthouseScene, "the storm night (chapter 1): through the telescope, the Esmers' ship sinking out of the clouds over the sea, a black car on its ramp", model =>
        {
            var firstPoint = model.TrackPoints.Count;
            for (var k = 0; k <= ShipSteps; k++)
            {
                var t = k / (double)ShipSteps;
                // (easing into its hover over the waves)
                var e = 1 - (1 - t) * (1 - t);
                model.TrackPoints.Add(new SceneTrackPoint((int)(ShipFrom.X + (ShipTo.X - ShipFrom.X) * e), (int)(ShipFrom.Y + (ShipTo.Y - ShipFrom.Y) * e), (int)(ShipFrom.Z + (ShipTo.Z - ShipFrom.Z) * e)));
            }
            ship.X = ShipFrom.X; ship.Y = ShipFrom.Y; ship.Z = ShipFrom.Z; ship.Beta = 1024; ship.Anim = ShipHover;
            // (the shot: a camera zone round its way down, off until the telescope's -- set_camera -- as the cow's is: the follow camera
            // let it drift out of the picture)
            int[] shot = { ShipFrom.X / 512, (ShipFrom.Y + ShipTo.Y) / 2 / 256, ShipFrom.Z / 512, ShipShot[0], ShipShot[1], 0, ShipShot[2], 8 };
            model.Zones.Add(new SceneZoneModel { Type = 1, Num = ShipCamera, X0 = ShipFrom.X - 900, X1 = ShipFrom.X + 900, Y0 = ShipTo.Y - 800, Y1 = ShipFrom.Y + 800, Z0 = ShipFrom.Z - 900, Z1 = ShipFrom.Z + 900, Info = shot });
            // (seen only in its shot; nothing collides with it, it casts no shadow)
            ship.Flags = 0x180000u | RaceTrackScenes.InvisibleFlag | 0x1000u;
            var index = model.Actors.Count;
            var life = p.Add(model, ship, $@"void comportement_0()
{{
    if (1 == chapter())
    {{
        set_comportement(comportement_1);
    }}
    else
    {{
        suicide();
    }}
}}

void comportement_1()
{{
    if (1 == var_cube({StormLook}))
    {{
        set_var_cube({StormLook}, 2);
        set_dir_obj(0, NO_MOVE);
        cinema_mode(1);
        invisible(0);
        cam_follow({index});
        set_camera({ShipCamera}, 1);
        set_track(label_{ShipLook});
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
    if ({ShipLooked} == l_track())
    {{
        message_obj(0, {Say(CitadelEggLine.StormLights)});
        message_obj(0, {Say(CitadelEggLine.StormCar)});
        set_camera({ShipCamera}, 0);
        cam_follow(0);
        cinema_mode(0);
        invisible(1);
        set_dir_obj(0, MOVE_MANUAL);
        set_var_cube({StormLook}, 0);
        set_track(label_0);
        set_comportement(comportement_1);
    }}
}}
");
            var track = new System.Text.StringBuilder("label(0);\nstop();\n\n");
            track.Append($"label({ShipLook});\nanim({ShipHover});\n");
            for (var k = 0; k <= ShipSteps; k++)
                track.Append($"pos_point({firstPoint + k});\nbody({(k % 2 == 0 ? 58 : 59)});\nwait_nb_dizieme(2);\n");
            track.Append("wait_nb_second(2);\n\n");
            track.Append($"label({ShipLooked});\npos_point({firstPoint});\nstop();\n");
            return scripts =>
            {
                life(scripts);
                scripts.SetText(index, ScriptKind.Track, track.ToString());
            };
        });

        // ---- the Wizard's encore by the town circuit's start line ----
        var wizard = Copy(p.Originals, LighthouseScene, LighthouseWizard);
        if (wizard is null || p.TownScenes.StartScene < 0 || p.Town.StartLine.Count == 0) p.Log.Add("the Wizard's encore left out: no Wizard (scene 46's actor 4) or no town circuit start scene");
        else
        {
            var start = p.Town.StartLine[0];
            p.Edit(p.TownScenes.StartScene, "the Wizard's encore (chapter 2): sunshine, rain or snow for the town circuit's race (the race-track mode's weather_var=)", model =>
            {
                // (beside the line a few cells back -- the herald and Mr. Paul stand nearer it -- or, the line being on its bridge over the
                // sea, on the nearest ground off the roads; in the start scene's cube)
                bool InCube((double X, double Z, double Y, int Beta) a) => (int)Math.Floor(a.X / 64) == model.CubeX && (int)Math.Floor(a.Z / 64) == model.CubeY;
                (double X, double Z, double Y, int Beta)? at = null;
                foreach (var back in new[] { 7.0, 9.0, 5.5, 11.0 })
                    if (Beside(p.Town, start, back, 1, new[] { 1, -1 }) is { } b && InCube(b)) { at = b; break; }
                if (at is null)
                    foreach (var back in new[] { 8.0, 12.0, 4.0 })
                        if (OffRoad(p.Town, start.X - start.DirX * back + start.DirZ * 4, start.Z - start.DirZ * back - start.DirX * 4, 1, 24) is { } o
                            && Facing(o, start.X, start.Z) is var f && InCube(f)) { at = f; break; }
                if (at is not { } w) throw new NotTheGames("no room off the roads by the town circuit's start line");
                Place(wizard, model, w);
                p.Log.Add($"the Wizard's encore at cell ({w.X:0.#}, {w.Z:0.#}), {w.Y:0}");
                var index = model.Actors.Count;
                return p.Add(model, wizard, Talker("2 == chapter()", $@"            add_choice({Say(CitadelEggLine.Sunshine)});
            add_choice({Say(CitadelEggLine.Rain)});
            add_choice({Say(CitadelEggLine.Snow)});
            ask_choice_obj({index}, {Say(CitadelEggLine.WizardAsk)});
            if ({Say(CitadelEggLine.Rain)} == choice())
            {{
                set_var_game({EggWeather}, {WeatherRain});
                message({Say(CitadelEggLine.WizardRain)});
            }}
            else if ({Say(CitadelEggLine.Snow)} == choice())
            {{
                set_var_game({EggWeather}, {WeatherSnow});
                message({Say(CitadelEggLine.WizardSnow)});
            }}
            else
            {{
                set_var_game({EggWeather}, {WeatherSun});
                message({Say(CitadelEggLine.WizardSun)});
            }}"));
            });
        }

        // ---- Mrs. Brune's sisters, at the ferry office, asked again ----
        p.Edit(FerryOffice, "Mrs. Brune asked again in chapter 2: her four sisters on Zeelich, and not one postcard", _ => scripts =>
        {
            var brune = scripts.GetText(3, ScriptKind.Life);
            scripts.SetText(3, ScriptKind.Life, Loose(brune, $"message_zoe({p.Say(ChapterLine.ZoeCrew)}); message({p.Say(ChapterLine.Brune)});", $@"if (0 == var_cube({BruneSaid}))
                    {{
                        message_zoe({p.Say(ChapterLine.ZoeCrew)});
                        message({p.Say(ChapterLine.Brune)});
                        set_var_cube({BruneSaid}, 1);
                    }}
                    else
                    {{
                        message({Say(CitadelEggLine.BruneSisters)});
                    }}"));
        });
    }
}
