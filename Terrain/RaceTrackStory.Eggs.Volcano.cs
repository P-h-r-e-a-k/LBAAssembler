using System.IO;
using System.Numerics;
using LbaBodyStudio;
using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;

namespace LBAAssembler.Terrain;

// The Island under Celebration's easter eggs (RaceTrackStory.Eggs.cs; the user, 2026-11-04: "Let's make all those changes", of the
// proposal's picks), with chapter 11's story (RaceTrackStory.Volcano.cs):
//   - Not that ring. The proposal: "stand at the lava's edge with the Dissidents' Ring (item 16) and use it: Twinsen holds it over the
//     lava. Zoe: 'Twinsen. We still need that.' Twinsen: '...Right. Sorry. It felt like the thing to do.'" (The Lord of the Rings: the
//     ring into the volcano.) Helpers on the plateau's rim where it drops to the lava, the nearest to the start line first: the ring used
//     from the inventory (use_inventory(16)) within reach of one, on foot: the two lines.
//   - The statue's feet. The proposal: "a crack high in the cavern roof. Through it, in chapters 11-13, darkness. Come back in Track select
//     after the Cup (or in chapter 14's ending), and there's a red glow and two enormous stone toes above the crack." (LBA2: the Dark
//     Monk's statue rises out of Celebration Island, right above this cave.) The cave has no roof to crack (the island is drawn open to its
//     sky), and the statue is Celebration Island's own decor (CELEBRA2.OBL), which no actor can carry and no variable can show or hide.
//     So a piece of roof of its own: a body made here (two slabs of dark rock with a crack between them) for the volcano's standing stone's
//     entity (218, beside its own body 0), in two kinds -- the crack black (darkness) or, the statue risen (var 79 set) or the Cup won
//     (chapter() past 14), lit red from above with two great grey stone toes hanging down through it -- hung high over the road a way on
//     from the start line, where the cars drive at it and under it and the camera, which looks little above the horizon, sees it ahead
//     (a place with no other part of the lap near its height). Its script picks the kind as the scene starts.
internal static partial class RaceTrackStory
{
    // the ring (item 16, the Dissidents'), how near the rim's helper Twinsen uses it, how many places along the rim, how far apart (cells)
    private const int RingReach = 1800, RimSpots = 5;
    private const double RimApart = 10;
    // the lava: the island's ground at its height or under it (SOUSCELB's lava is its "sea", at 0); the plateau's rim: ground within
    // RimDrop of the start line's, with lava within RimLava cells
    private const double LavaTop = 400, RimDrop = 1500, RimLava = 4;

    // the roof's piece: the standing stone's entity, its two new bodies (generic numbers the entity hasn't: an actor's is -128..127), how
    // high over the road it hangs, how far along the lap from the start line it may (cells, the first with room), how near no other part
    // of the lap may come round it (cells)
    private const int StoneEntity = 218, RoofDark = 100, RoofRisen = 101, RoofOver = 4500;
    private static readonly double[] RoofAlong = Enumerable.Range(0, 60).Select(k => 18 + 4.0 * k).ToArray();
    private const double RoofClear = 10;
    // its colours (island palette 38's ramp starts): the rock's dark brown, the toes' stone grey, the crack's black, the glow's reds
    private const int RoofRock = 48, ToeStone = 176, CrackBlack = 0, GlowRed = 72, GlowCore = 86;

    // Not that ring: helpers along the plateau's rim in the start scene (its model). Lines for the log go to the story's.
    private static Action<SceneScripts> NotThatRing(LateStory s, SceneModel model, int zoe, int twinsen)
    {
        var edits = new List<Action<SceneScripts>>();
        var spots = Rim(s.Report, model, RimSpots);
        if (spots.Count == 0) { s.Log.Add("not that ring: no rim over the lava in the start scene -- left out"); return _ => { }; }
        foreach (var (x, z, y, lava) in spots)
        {
            var at = RaceTrackScenes.InvisibleHelper(RaceTrackScenes.CubeLocal(x, model.CubeX), (int)Math.Round(y), RaceTrackScenes.CubeLocal(z, model.CubeY), 4);
            edits.Add(s.Add(model, at, $@"void comportement_0()
{{
    if ({VolcanoChapter} <= chapter())
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
    if (1 == use_inventory({DissidentsRing}) && {RingReach} > distance(0) && {Driving} != comportement_hero())
    {{
        message_zoe({zoe});
        message_obj(0, {twinsen});
    }}
}}
"));
            s.Log.Add($"not that ring: the plateau's rim at cell ({x:0.#}, {z:0.#}), {y:0} high, the lava towards ({lava.X:0.#}, {lava.Z:0.#})");
        }
        return scripts => { foreach (var e in edits) e(scripts); };
    }

    // Up to `count` places on the plateau's rim in the scene's cube (island cells, the ground's height), each with the nearest lava below
    // it: the ground within RimDrop of the start line's, off the roads, and lava within RimLava cells -- the nearest to the start line
    // first, RimApart apart.
    private static List<(double X, double Z, double Y, (double X, double Z) Lava)> Rim(RaceTrackReport report, SceneModel model, int count)
    {
        var found = new List<(double X, double Z, double Y, (double X, double Z) Lava)>();
        if (report.GroundAfter is not { } ground || report.StartLine.Count == 0) return found;
        var s = report.StartLine[0];
        for (var r = 0.0; r <= 60 && found.Count < count; r += 0.5)
        {
            var steps = r == 0 ? 1 : (int)Math.Ceiling(2 * Math.PI * r / 0.5);
            for (var k = 0; k < steps && found.Count < count; k++)
            {
                double a = 2 * Math.PI * k / steps, px = s.X + r * Math.Sin(a), pz = s.Z + r * Math.Cos(a);
                double cx = px - model.CubeX * 64, cz = pz - model.CubeY * 64;
                if (cx < 3 || cx > 61 || cz < 3 || cz > 61) continue;
                if (found.Any(f => Math.Abs(f.X - px) + Math.Abs(f.Z - pz) < RimApart)) continue;
                var g = ground(px, pz);
                if (Math.Abs(g - s.Y) > RimDrop || OffRoads(report, px, pz) < 1.5) continue;
                (double X, double Z)? lava = null;
                for (var d = 1.0; d <= RimLava && lava is null; d += 0.5)
                    for (var j = 0; j < 16 && lava is null; j++)
                    {
                        double lx = px + d * Math.Sin(Math.PI * j / 8), lz = pz + d * Math.Cos(Math.PI * j / 8);
                        if (ground(lx, lz) <= LavaTop) lava = (lx, lz);
                    }
                if (lava is { } l) found.Add((px, pz, g, l));
            }
        }
        return found;
    }

    // The statue's feet: the roof's piece over the road RoofAlong cells on from the start line (the first of them with no other part of
    // the lap within RoofClear cells round it and near its height) in the start scene's cube
    private static void StatueFeet(LateStory s)
    {
        var report = s.Report;
        var n = report.LapX.Length;
        if (n < 10 || report.LapY.Length != n || report.StartLine.Count == 0) { s.Log.Add("the statue's feet: no lap to hang the roof's piece over -- left out"); return; }
        var line = report.StartLine[0];
        var from = Enumerable.Range(0, n).OrderBy(i => Math.Pow(report.LapX[i] - line.X, 2) + Math.Pow(report.LapZ[i] - line.Z, 2)).First();
        // (the lap's points by how far along it they are from the start line's, and the lap's way there: the way it goes on from the line)
        var ahead = (report.LapX[(from + 3) % n] - report.LapX[from]) * line.DirX + (report.LapZ[(from + 3) % n] - report.LapZ[from]) * line.DirZ >= 0;
        int At(int k) => ahead ? (from + k) % n : (from - k % n + n) % n;
        var along = new double[n];
        for (var k = 1; k < n; k++)
        {
            int a = At(k - 1), b = At(k);
            along[k] = along[k - 1] + Math.Sqrt(Math.Pow(report.LapX[b] - report.LapX[a], 2) + Math.Pow(report.LapZ[b] - report.LapZ[a], 2));
        }
        // (in the start scene's cube: the race-track mode keeps the start scene on while the car climbs out of its cube and back, so an
        // actor of the next scene over a road there isn't drawn while the cars pass under it -- seen 2026-11-04, the cars on the spiral's
        // upper turn past the cube's edge in scene 130)
        if (!s.Store.SceneExists(s.StartScene)) { s.Log.Add("the statue's feet: no start scene -- left out"); return; }
        var home = s.Store.Load(s.StartScene);
        bool Home(int i) => report.LapX[i] - home.CubeX * 64 is > 2 and < 62 && report.LapZ[i] - home.CubeY * 64 is > 2 and < 62;
        int? place = null;
        foreach (var want in RoofAlong)
        {
            var k = Enumerable.Range(0, n).OrderBy(j => Math.Abs(along[j] - want)).First();
            int i = At(k);
            if (!Home(i)) continue;
            double py = report.LapY[i] + RoofOver;
            // (no other part of the lap round it between the road under it and over the roof's top)
            var clear = Enumerable.Range(0, n).All(j => Math.Abs(along[j] - along[k]) < RoofClear + 2 ||
                Math.Pow(report.LapX[At(j)] - report.LapX[i], 2) + Math.Pow(report.LapZ[At(j)] - report.LapZ[i], 2) > RoofClear * RoofClear ||
                report.LapY[At(j)] < report.LapY[i] - 500 || report.LapY[At(j)] > py + 4000);
            if (clear) { place = k; break; }
        }
        if (place is not { } pk) { s.Log.Add("the statue's feet: no room over the road near the start line -- left out"); return; }
        s.Log.Add($"the statue's feet: over the road {along[pk]:0} cells on from the start line");
        var top = At(pk);
        double x = report.LapX[top], z = report.LapZ[top], y = report.LapY[top] + RoofOver;
        var scene = s.StartScene;
        if (!InstallRoof(s.Game, s.Log)) return;
        // (its crack along the road there)
        var next = At(pk + 1);
        var beta = RaceTrackMath.Turn(Math.Atan2(report.LapX[next] - x, report.LapZ[next] - z));
        s.Edit(scene, $"the statue's feet: a crack in the cave's roof at cell ({x:0.#}, {z:0.#}), {y:0} high (entity {StoneEntity}, body {RoofDark}; " +
                      $"{RoofRisen} -- the toes -- once var {StatueRisen} is set or the Cup won)", model =>
        {
            var roof = SceneOps.BlankActor(SceneGame.Lba2, RaceTrackScenes.CubeLocal(x, model.CubeX), (int)Math.Round(y), RaceTrackScenes.CubeLocal(z, model.CubeY), entity: StoneEntity);
            // (drawn only: no shadow, nothing touches it, not clipped by its middle -- a big one; NO_SHADOW | NO_PRE_CLIP)
            roof.Flags = 0x1000 | 0x80000; roof.Body = RoofDark; roof.Anim = 0; roof.Beta = beta; roof.Armor = 51; roof.LifePoints = -1;
            return s.Add(model, roof, $@"void comportement_0()
{{
    if (0 < var_game({StatueRisen}) || {FinalChapter} < chapter())
    {{
        body({RoofRisen});
    }}
    end_life();
}}
");
        });
    }

    // The roof's two bodies into the game folder (BODY.HQR's end) as the standing stone entity's RoofDark and RoofRisen (RESS.HQR's entity
    // table) -- a body already there as made here is kept. False (and a line for the log) when the entity isn't the game's standing stone.
    private static bool InstallRoof(string game, List<string> log)
    {
        var bodyPath = Path.Combine(game, "BODY.HQR");
        var ressPath = Path.Combine(game, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        var entity = Lba2EntityTable.Parse(table).Entities.FirstOrDefault(e => e.Id == StoneEntity);
        if (entity is null || !entity.Anims.Any(a => a.Generic == 0) || !entity.Bodies.Any(b => b.Generic == 0))
        {
            log.Add($"WARNING: the statue's feet: entity {StoneEntity} isn't the volcano's standing stone -- left out");
            return false;
        }
        var header = Body.Read(HqrArchive.Open(bodyPath).Read(RaceTrackBaldinoCar.RacerBody), 2).Header;
        var changed = false;
        foreach (var (generic, risen) in new[] { (RoofDark, false), (RoofRisen, true) })
        {
            var bytes = Roof(header, risen).Write();
            var count = HqrArchive.CountEntries(bodyPath);
            if (entity.Bodies.FirstOrDefault(b => b.Generic == generic) is { Body: var have } && have > 0 && have < count &&
                HqrArchive.Open(bodyPath).Read(have).AsSpan().SequenceEqual(bytes)) continue;
            File.WriteAllBytes(bodyPath, HqrWriter.AppendEntry(File.ReadAllBytes(bodyPath), HqrWriter.StoredEntry(bytes)));
            table = RaceTrackBaldinoCar.WithBody(table, StoneEntity, generic, count);
            changed = true;
            log.Add($"the statue's feet: the roof's piece {(risen ? "with the statue's toes" : "dark")}, BODY.HQR {count}, entity {StoneEntity}'s body {generic}");
        }
        if (changed) File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return true;
    }

    // The roof's piece (units: its middle at the actor's place, y up, the road's way +z): two slabs of rock either side of a crack across
    // the road (along x), lumps of rock under them; the crack black, or -- `risen` -- glowing red, a red glow high over it, and two great
    // grey stone toes side by side hanging down through it, round-tipped, toenails towards the cars coming.
    private static Body Roof(byte[] header, bool risen)
    {
        var m = new CarMesh(risen ? "the cave's roof, the statue's toes" : "the cave's roof", new[] { -1 });
        const float W = 9000, D = 7000, T = 1100, Gap = 1400;
        m.Light = 0.55f;
        m.Box(0, new Vector3(0, T / 2, -(D + Gap) / 4), new Vector3(W, T, (D - Gap) / 2), RoofRock);
        m.Box(0, new Vector3(0, T / 2, (D + Gap) / 4), new Vector3(W, T, (D - Gap) / 2), RoofRock);
        // (lumps of rock under the slabs, so the roof doesn't read as two boards -- clear of the crack)
        foreach (var (lx, lz, size) in new[] { (-3200f, -2200f, 900f), (-1800f, 2400f, 700f), (2600f, -2000f, 800f), (3300f, 2300f, 1000f), (-400f, -2900f, 600f), (1200f, 2900f, 650f), (-3600f, 1700f, 550f) })
            m.Box(0, new Vector3(lx, -size / 3, lz), new Vector3(size * 1.2f, size, size), RoofRock);
        m.Light = 1;
        if (!risen)
        {
            // the crack: darkness in it
            m.Flat(0, CrackBlack, new Vector3(-W / 2, T * 0.6f, -Gap / 2 - 20), new Vector3(W / 2, T * 0.6f, -Gap / 2 - 20), new Vector3(W / 2, T * 0.6f, Gap / 2 + 20), new Vector3(-W / 2, T * 0.6f, Gap / 2 + 20));
            return m.ToBody(header);
        }
        // the crack glowing red, and a red glow high above it (the lava-lit sky round the statue), brighter in its middle
        m.Flat(0, GlowRed, new Vector3(-W / 2, T * 0.6f, -Gap / 2 - 20), new Vector3(W / 2, T * 0.6f, -Gap / 2 - 20), new Vector3(W / 2, T * 0.6f, Gap / 2 + 20), new Vector3(-W / 2, T * 0.6f, Gap / 2 + 20));
        m.Flat(0, GlowRed, new Vector3(-W / 2, T + 2500, -D / 2), new Vector3(W / 2, T + 2500, -D / 2), new Vector3(W / 2, T + 2500, D / 2), new Vector3(-W / 2, T + 2500, D / 2));
        m.Flat(0, GlowCore, new Vector3(-1800, T + 2480, -1500), new Vector3(1800, T + 2480, -1500), new Vector3(1800, T + 2480, 1500), new Vector3(-1800, T + 2480, 1500));
        // the toes: the big toe and the next, side by side, from over the roof down through the crack, a round tip under each, a nail on
        // each tip's front
        // (unlit greys: lit, the toes' undersides -- all anyone sees of them -- were as dark as the rock; a sphere is drawn in its own colour)
        foreach (var (tx, half, low) in new[] { (-650f, 600f, -900f), (650f, 470f, -600f) })
        {
            var centre = new Vector3(tx, (T + 900 + low) / 2, 0);
            var size = new Vector3(half * 2, T + 900 - low, Gap - 160) / 2;
            var c = new[] { new Vector3(-1, -1, -1), new(1, -1, -1), new(1, -1, 1), new(-1, -1, 1), new(-1, 1, -1), new(1, 1, -1), new(1, 1, 1), new(-1, 1, 1) }
                .Select(v => m.P(0, centre + v * size)).ToArray();
            foreach (var (a, b, cc, d, colour) in new[] { (0, 1, 2, 3, ToeStone + 6), (4, 5, 6, 7, ToeStone + 6), (0, 1, 5, 4, ToeStone + 9), (1, 2, 6, 5, ToeStone + 7), (2, 3, 7, 6, ToeStone + 8), (3, 0, 4, 7, ToeStone + 7) })
                m.Out(new[] { c[a], c[b], c[cc], c[d] }, colour, centre, unlit: true);
            m.Ball(0, new Vector3(tx, low, 0), (int)half, ToeStone + 8);
            m.Flat(0, ToeStone + 12, new Vector3(tx - half * 0.55f, low - half * 0.6f, -half - 30), new Vector3(tx + half * 0.55f, low - half * 0.6f, -half - 30),
                new Vector3(tx + half * 0.6f, low + half * 0.3f, -Gap / 2 + 60), new Vector3(tx - half * 0.6f, low + half * 0.3f, -Gap / 2 + 60));
        }
        return m.ToBody(header);
    }
}
