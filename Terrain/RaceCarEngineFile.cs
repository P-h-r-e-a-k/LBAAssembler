using System.Globalization;
using System.IO;
using System.Text;

namespace LBAAssembler.Terrain;

// The race car's setup (RaceCarSetup, kept in the settings) written as the engine's car file for its race-track mode (native RACEMOD.CPP), with
// what the game folder's RACETRACK.JSON says about the track: the start line, the checkpoints and the opponents' lines.
internal static class RaceCarEngineFile
{
    // The engine's car setup file (RACEMOD.CPP's format), with the start line, the checkpoints and the opponents from the game folder's
    // RACETRACK.JSON when it has one (each opponent's line in the file named in `pathFiles`, in the order of Opponents). Of an island with
    // a track in each weather file, the one the folder was built to race (RaceTrackService.Raced), in its weather; the other's cars are
    // kept out of sight.
    internal static string EngineFile(this RaceCarSetup car, RaceTrackService.TrackInfo? info, IReadOnlyList<string>? pathFiles = null, string? raisedFile = null)
    {
        var track = info is null ? null : RaceTrackService.Raced(info);
        string N(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        var gears = Math.Clamp(car.Gears, 1, RaceCarSetup.MaxGears);
        var text = new StringBuilder("# LBA Assembler race car setup (read by the engine's race-track mode, RACEMOD.CPP)\n");
        text.Append($"gears={gears}\n");
        for (var g = 0; g < gears; g++) text.Append($"gear{g + 1}={RaceCarSetup.KmhToUnits(Math.Clamp(car.TopKmh(g), 3, 150))}\n");
        text.Append($"accel={N(RaceCarSetup.OriginalAccel * Math.Clamp(car.AccelerationPercent, 10, 1000) / 100.0)}\n");
        text.Append($"brake={N(RaceCarSetup.OriginalBrake * Math.Clamp(car.BrakingPercent, 10, 1000) / 100.0)}\n");
        text.Append($"coast={N(RaceCarSetup.OriginalCoast * Math.Clamp(car.CoastingPercent, 0, 1000) / 100.0)}\n");
        text.Append($"reverse={RaceCarSetup.KmhToUnits(Math.Clamp(car.ReverseKmh, 1, 150))}\n");
        text.Append($"steer={(int)Math.Round(RaceCarSetup.OriginalSteer * Math.Clamp(car.SteeringPercent, 10, 1000) / 100.0)}\n");
        text.Append($"automatic={(car.Automatic ? 1 : 0)}\n");
        text.Append($"hud={(car.ShowDisplay ? 1 : 0)}\n");
        // (a line's height, when it has one: a lap that passes over itself crosses the line's place at other heights too)
        static string Height(RaceTrackService.StartLineInfo line) => line.Y is { } y ? $" {y}" : "";
        if (track?.StartLine is { } l) text.Append($"startline={l.CubeX} {l.CubeZ} {l.X0} {l.Z0} {l.X1} {l.Z1} {l.DirX} {l.DirZ}{Height(l)}\n");
        foreach (var c in track?.Checkpoints ?? new()) text.Append($"checkpoint={c.CubeX} {c.CubeZ} {c.X0} {c.Z0} {c.X1} {c.Z1} {c.DirX} {c.DirZ}{Height(c)}\n");
        // a raised road: the file with its middle, point by point (the engine's floor there)
        if (raisedFile is not null) text.Append($"raised={raisedFile}\n");
        // the island's file with the statue (Celebration Island), whatever the game's own variable says
        if (info is not null && RaceTrackIsland.ByName(info.Island).Statue) text.Append("statue=1\n");
        // the grid spots, and whether a qualifying lap sets the order the cars line up in (RACEMOD.CPP)
        foreach (var g in track?.Grid ?? new()) text.Append($"grid={string.Join(' ', g)}\n");
        foreach (var g in track?.Pits ?? new()) text.Append($"pit={string.Join(' ', g)}\n");
        if (track?.Grid is { Count: > 0 }) text.Append($"qualifying={(car.Qualifying ? 1 : 0)}\n");
        if (RaceTrackService.FineWeather(info, car.FineWeather)) text.Append("weather=fine\n");
        if (track?.StoryArrow is >= 0 and var arrow) text.Append($"holo_arrow={arrow}\n");
        var opponents = car.Opponents(track);
        for (var i = 0; i < opponents.Count && pathFiles is not null && i < pathFiles.Count; i++)
        {
            // (the engine's first opponent is "opponent_", the next "opponent2_" and so on)
            var key = i == 0 ? "opponent" : $"opponent{i + 1}";
            var o = opponents[i];
            text.Append($"# {o.Name}\n{key}_path={pathFiles[i]}\n{key}_grid={o.Grid}\n{key}_pace={Math.Clamp(o.Pace, 10, 300)}\n");
            text.Append($"{key}_top={(int)Math.Round(o.Line.Top * 100)}\n{key}_grip={(int)Math.Round(o.Line.Grip * 100)}\n{key}_catchup={(car.OpponentsFightBack ? RaceCarSetup.CatchUpPercent : 0)}\n");
            text.Append($"{key}_name={(o.Name == "the racer" ? "The racer" : o.Name)}\n");
            // (the animations it stands and drives with: the cars' are the racer entity's 0 and 1, the bike's his own)
            if (o.Name == BikerName) text.Append($"{key}_anim={RaceTrackScenes.BikerIdleAnim} {RaceTrackScenes.BikerRideAnim}\n");
            foreach (var (scene, actor) in o.Actors) text.Append($"{key}_actor={scene} {actor}\n");
        }
        // the cars of the opponents the setup doesn't race: hidden, so they don't stand where a racing car lines up
        // (and all of the other weather's track's: that isn't the island the game draws)
        var parked = car.Parked(track);
        if (info?.Twin is { } fine) parked.AddRange(AllCars(ReferenceEquals(track, fine) ? info : fine));
        foreach (var (scene, actor) in parked) text.Append($"hide_actor={scene} {actor}\n");
        return text.ToString();
    }

    // The scenes' copies of every opponent's car a track has.
    private static IEnumerable<(int Scene, int Actor)> AllCars(RaceTrackService.TrackInfo track) =>
        (track.Opponent ?? new()).Select(a => (a.Key, a.Value)).Concat((track.Rivals ?? new()).SelectMany(r => r.Actors.Select(a => (a.Key, a.Value))));

    // The scenes' copies of the cars of the opponents the track has and the setup doesn't race.
    internal static List<(int Scene, int Actor)> Parked(this RaceCarSetup car, RaceTrackService.TrackInfo? track)
    {
        var list = new List<(int, int)>();
        if (!car.Opponent && track?.Opponent is { } actors) list.AddRange(actors.Select(a => (a.Key, a.Value)));
        foreach (var r in track?.Rivals ?? new())
            if (r.Name == "Baldino" && !car.Baldino || r.Name == BikerName && !car.Biker) list.AddRange(r.Actors.Select(a => (a.Key, a.Value)));
        return list;
    }

    // The opponents the car setup races, in the engine's order: the retail track's racer, then Baldino (each when the track has it and the
    // setup races it), with its line, how many points before the start line it starts, its skill (the engine's pace), its character (the
    // share of the car's top speed and cornering it drives with, as the build planned its line: RaceTrackBuilder) and the scenes' copies of
    // its car.
    internal static List<(string Name, List<int[]> Path, int Grid, int Pace, RaceTrackBuilder.RacingLine Line, Dictionary<int, int> Actors)> Opponents(this RaceCarSetup car, RaceTrackService.TrackInfo? track)
    {
        var list = new List<(string, List<int[]>, int, int, RaceTrackBuilder.RacingLine, Dictionary<int, int>)>();
        if (car.Opponent && track?.Path is { Count: > 0 } path && track.Opponent is { Count: > 0 } actors)
            list.Add(("the racer", path, track.PathGrid, car.RacerSkill, RaceTrackBuilder.RacerLine, actors));
        foreach (var r in track?.Rivals ?? new())
        {
            if (r.Name == "Baldino" && car.Baldino && r.Path.Count > 0 && r.Actors.Count > 0)
                list.Add((r.Name, r.Path, r.Grid, car.BaldinoSkill, RaceTrackBuilder.BaldinoLine, r.Actors));
            if (r.Name == BikerName && car.Biker && r.Path.Count > 0 && r.Actors.Count > 0)
                list.Add((r.Name, r.Path, r.Grid, car.BikerSkill, RaceTrackBuilder.BikerLine, r.Actors));
        }
        return list;
    }

    public const string BikerName = "The biker";

    // The engine's car setup file, and beside it each opponent's line (racepath.txt, racepath2.txt ...).
    internal static void WriteEngineFile(this RaceCarSetup car, string path, RaceTrackService.TrackInfo? info)
    {
        var files = new List<string>();
        var track = info is null ? null : RaceTrackService.Raced(info);
        foreach (var (o, i) in car.Opponents(track).Select((o, i) => (o, i)))
        {
            var file = Path.Combine(Path.GetDirectoryName(path) ?? ".", i == 0 ? "racepath.txt" : $"racepath{i + 1}.txt");
            File.WriteAllLines(file, o.Path.Select(p => string.Join(' ', p)));
            files.Add(file);
        }
        string? raised = null;
        if (track?.Raised is { Count: > 1 } road)
        {
            raised = Path.Combine(Path.GetDirectoryName(path) ?? ".", "raceraised.txt");
            File.WriteAllLines(raised, road.Select(p => string.Join(' ', p)));
        }
        File.WriteAllText(path, car.EngineFile(info, files, raised));
    }
}
