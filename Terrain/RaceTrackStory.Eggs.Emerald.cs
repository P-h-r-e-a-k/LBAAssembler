using System.IO;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Emerald Moon's easter eggs (chapter 4; RaceTrackStory.Eggs.cs says what the eggs are; their lines are the last of the moon's,
// RaceTrackStory.Emerald.cs MoonLines). The user, 2026-11-04, of the proposal's picks: "Let's make all those changes".
internal static partial class RaceTrackStory
{
    // ---- Daisy, Daisy (the proposal: "after HAL is mended, talk to him three more times. The third time his voice slows: "Daisy...
    // Daisy... give me your answer do... I'm... half... crazy..." Then normal: "Excuse me. A diagnostic."" -- LBA2's HAL is 2001's
    // already, and HAL 9000 sings "Daisy Bell" as he is switched off): HAL's talker in the moon base (scene 23, ApplyEmerald), once he is
    // mended, counts what Twinsen says to him in a visit (var_cube HalTalks: the base's own scripts use 0-2); the third time is the song
    // and the diagnostic, every other time his thanks. (Not built: the proposal's slowed robot sample under the song -- the line alone.)
    private const int HalTalks = 70;
    private static string HalDaisy(int thanks, int daisy, int diagnostic) => $@"                add_var_cube({HalTalks}, 1);
                if (3 == var_cube({HalTalks}))
                {{
                    message({daisy});
                    message({diagnostic});
                }}
                else
                {{
                    message({thanks});
                }}";

    // ---- Petanque in low gravity (the proposal: "behind the base, the Desert's petanque player (in a space suit), staring up. "In this
    // gravity my boules never come down. I threw one on Tuesday." Wait near him and, a minute later, a boule lands at his feet:
    // "Tuesday's."" -- the Apollo 14 golf balls): the bowler is a figure in the space suit Twinsen wears out on the moon (entity
    // SpaceSuit: the hero's file for behaviour C_SCAPH_EXT_NORM, OBJECT.CPP LoadFicPerso, BODY.HQR 20) -- the Desert's bowlers have no suit,
    // and out here nobody goes without one -- standing off the roads beyond the moon base from the start line. Twinsen near him (within
    // BowlerNear) starts Tuesday's boule (a gold ball, SPRITES.HQR BouleSprite) on its way down from far above him (a track point
    // BouleDrop over his feet: out of sight), BouleWait seconds and then a slow fall in the moon's gravity to his feet; Twinsen gone
    // (past BowlerFar) puts it back up. Landed: "Tuesday's." -- and again whenever he's asked after; asked before, his boules that never come down.
    private const int SpaceSuit = 10, SpaceSuitBody = 0, BouleSprite = 157, BouleWait = 48, BouleSpeed = 1000, BouleDrop = 9000;
    private const int BowlerNear = 2500, BowlerFar = 5000;
    // (how far beyond the base's middle from the start line he stands, and how clear of the roads, in cells)
    private const double BowlerBeyond = 9, BowlerClear = 2.5;

    private static void MoonPetanque(IslandBeatsA b, RaceTrackReport report, int upLine, int tuesdayLine)
    {
        // the moon base: the middle of the doors into it from the moon's outside scenes (each a door to a hall with a door to scene 23)
        var doors = new List<(double X, double Z)>();
        foreach (var s in b.OutsideScenes)
        {
            var m = b.Store.Load(s);
            foreach (var z in m.Zones.Where(z => z.Type == 0 && b.Store.SceneExists(z.Num)))
            {
                SceneModel hall;
                try { hall = b.Store.Load(z.Num); } catch (Exception e) when (e is InvalidDataException or IOException) { continue; }
                if (hall.CubeMode != 1 && hall.Zones.Any(h => h.Type == 0 && h.Num == BaseScene))
                    doors.Add((m.CubeX * 64 + (z.X0 + z.X1) / 1024.0, m.CubeY * 64 + (z.Z0 + z.Z1) / 1024.0));
            }
        }
        if (doors.Count == 0 || report.StartLine.Count == 0) { b.Log.Add("the petanque player left out: no door into the moon base"); return; }
        double bx = doors.Average(d => d.X), bz = doors.Average(d => d.Z);
        var start = report.StartLine[0];
        double dx = bx - start.X, dz = bz - start.Z, len = Math.Sqrt(dx * dx + dz * dz);
        if (len < 1) { dx = 1; dz = 0; len = 1; }
        if (OffRoadA(report, bx + dx / len * BowlerBeyond, bz + dz / len * BowlerBeyond, BowlerClear) is not { } spot || b.SceneAt(spot.X, spot.Z) is not (var scene and >= 0))
        {
            b.Log.Add("the petanque player left out: no ground off the roads beyond the moon base");
            return;
        }
        var at = Facing(spot, bx, bz);
        b.Edit(scene, $"the petanque player in a space suit beyond the moon base at cell ({at.X:0.#}, {at.Z:0.#}): Tuesday's boule comes down", model =>
        {
            var bowler = SceneOps.BlankActor(SceneGame.Lba2, 0, 0, 0, SpaceSuit);
            Place(bowler, model, at);
            bowler.Body = SpaceSuitBody; bowler.Flags = 0x0803; bowler.CoulObj = 4; bowler.Armor = 51;
            // (the boule: a sprite with no collisions, on a track from high over his feet down to them -- a little in front of him)
            int fx = bowler.X + (int)Math.Round(Math.Sin(at.Beta * Math.PI / 2048) * 400), fz = bowler.Z + (int)Math.Round(Math.Cos(at.Beta * Math.PI / 2048) * 400);
            var high = SceneOps.AddTrackPoint(model, new SceneTrackPoint(fx, bowler.Y + BouleDrop, fz));
            var low = SceneOps.AddTrackPoint(model, new SceneTrackPoint(fx, bowler.Y + 60, fz));
            var boule = RaceTrackScenes.SpriteProp(fx, bowler.Y + BouleDrop, fz, BouleSprite);
            var bouleIndex = model.Actors.Count;
            var addBoule = IslandBeatsA.Add(model, boule, "void comportement_0()\n{\n}\n");
            var addBowler = IslandBeatsA.Add(model, bowler, $@"void comportement_0()
{{
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if ({BowlerNear} > distance(0) && {Driving} != comportement_hero())
    {{
        set_track_obj({bouleIndex}, label_1);
        set_comportement(comportement_2);
    }}
    swif (1 == action())
    {{
        if (1500 > distance(0) && {Driving} != comportement_hero())
        {{
            message({upLine});
        }}
    }}
}}

void comportement_2()
{{
    if ({BowlerFar} < distance(0) || {Driving} == comportement_hero())
    {{
        set_track_obj({bouleIndex}, label_0);
        set_comportement(comportement_1);
    }}
    else if (2 == l_track_obj({bouleIndex}))
    {{
        message({tuesdayLine});
        set_comportement(comportement_3);
    }}
    swif (1 == action())
    {{
        if (1500 > distance(0))
        {{
            message({upLine});
        }}
    }}
}}

void comportement_3()
{{
    swif (1 == action())
    {{
        if (1500 > distance(0) && {Driving} != comportement_hero())
        {{
            message({tuesdayLine});
        }}
    }}
}}
");
            return scripts =>
            {
                addBoule(scripts);
                addBowler(scripts);
                scripts.SetText(bouleIndex, ScriptKind.Track, $@"label(0);
pos_point({high});
stop();

label(1);
wait_nb_second({BouleWait});
speed({BouleSpeed});
goto_point_3d({low});
speed(0);

label(2);
stop();
");
            };
        });
    }
}
