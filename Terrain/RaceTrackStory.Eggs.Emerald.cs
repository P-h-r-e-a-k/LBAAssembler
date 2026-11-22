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
    // (how clear of the roads he stands, in cells)
    private const double BowlerClear = 2.5;

    // (gentle: no steeper than this over half a cell -- 30 degrees, under the 40 Twinsen walks up -- and how far out from a door, in cells)
    private const double BowlerRise = 148, BowlerFrom = 3, BowlerTo = 12;

    // A spot for him: from each door in turn, rings further out, the first off the roads (BowlerClear) on gentle ground whose straight
    // way from the door is gentle too -- (island cells, the ground's height)
    private static (double X, double Z, double Y)? BowlerSpot(RaceTrackReport report, List<(double X, double Z)> doors,
        List<(double X0, double X1, double Y0, double Y1, double Z0, double Z1)> decors)
    {
        if (report.GroundAfter is not { } ground) return null;
        // (no decor standing there -- a building over the spot, or on the straight way to it: the first try stood him in the moon base's
        // entrance block, its ground low and gentle under it, and the game put him up on its roof; island cells)
        bool Clear(double x, double z, double y, double pad)
        {
            double wx = x * 512, wz = z * 512, p = pad * 512;
            return !decors.Any(d => wx + p > d.X0 && wx - p < d.X1 && wz + p > d.Z0 && wz - p < d.Z1 && d.Y1 > y + 100 && d.Y0 < y + 2000);
        }
        bool Gentle(double x0, double z0, double x1, double z1)
        {
            var steps = Math.Max(1, (int)Math.Ceiling(Math.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0)) / 0.5));
            var last = ground(x0, z0);
            for (var k = 1; k <= steps; k++)
            {
                double x = x0 + (x1 - x0) * k / steps, z = z0 + (z1 - z0) * k / steps;
                var g = ground(x, z);
                if (Math.Abs(g - last) > BowlerRise || !Clear(x, z, g, 0.5)) return false;
                last = g;
            }
            return true;
        }
        foreach (var (dx, dz) in doors)
            for (var r = BowlerFrom; r <= BowlerTo; r += 0.5)
            {
                var steps = (int)Math.Ceiling(2 * Math.PI * r / 0.5);
                for (var k = 0; k < steps; k++)
                {
                    double a = 2 * Math.PI * k / steps, px = dx + r * Math.Sin(a), pz = dz + r * Math.Cos(a);
                    if (OffRoads(report, px, pz) < BowlerClear) continue;
                    var g = ground(px, pz);
                    if (g < 200 || !Clear(px, pz, g, 1.5)) continue;
                    if (!Gentle(px - 0.5, pz, px + 0.5, pz) || !Gentle(px, pz - 0.5, px, pz + 0.5)) continue;
                    // (the way from the door, from a cell out of it -- the door's own zone is at the base's wall)
                    double ox = dx + Math.Sin(a), oz = dz + Math.Cos(a);
                    if (!Gentle(ox, oz, px, pz)) continue;
                    return (px, pz, g);
                }
            }
        return null;
    }

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
        // (where he stands: walked to from a door into the base -- the user, 2026-11-21: "I'm not sure he's reachable on foot"; he stood 9
        // cells beyond the base from the start line, off the roads, on a steep strip between the base's platform and the raised road, the
        // goto planner's "nowhere to stand": Twinsen stopped 2,867 short. Now: out from a door, off the roads, on gentle ground, and the
        // straight way to it from the door gentle all along)
        var decors = IslandBake.DecorBoxes(IslandFile.Load(Path.Combine(b.GameDirectory, b.Island.IleFile)));
        if (BowlerSpot(report, doors, decors) is not { } spot || b.SceneAt(spot.X, spot.Z) is not (var scene and >= 0))
        {
            b.Log.Add("the petanque player left out: no gentle ground off the roads walked to from a door of the moon base");
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
            // (seen only as it falls and once landed -- its track past label 3 -- hidden while it waits up there: the race's raised roads and
            // loops near him showed it hanging in the sky, a yellow ball, the user, 2026-11-21)
            var addBoule = IslandBeatsA.Add(model, boule, @"void comportement_0()
{
    if (3 == l_track() || 2 == l_track())
    {
        invisible(0);
    }
    else
    {
        invisible(1);
    }
}
");
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

label(3);
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
