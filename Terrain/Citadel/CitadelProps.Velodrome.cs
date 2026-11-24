using System.IO;
using System.Numerics;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The Dome of the Slate's velodrome's riders and their teams' tents (the user, 2026-11-24: "We need to add some NPCs to the velodrome to
// bring it to life, perhaps some people riding laps, and their support team, and generally just people around").
//
// A rider is one body: a quetch of the game's own (BODY.HQR 120, a man of Citadel Island; 45, the quetch girl), crouched on a track bike
// in a team's skinsuit and helmet -- the torso down over the bars, the hands on the drops, each leg bent to its pedal -- and RiderPhases
// of them a rider, the cranks a turn's part further round each: the engine shows them in turn as the rider goes round (RACEMOD.CPP
// VeloFrame), so the legs pedal. Along z (the front +z), the wheels on y = 0: leaned into the banking by the engine, about z.
//
// Their bodies are an entity's of their own (InstallVelodrome: a new one at the entity table's end, like entity 17), numbered from 0:
// entity 17's numbers are all but used up, and builds of different islands add to it with numbers of their own -- the Imperial Hotel's
// are 229-244, and a second user of them overwrites the first's.
internal static partial class CitadelProps
{
    public const int RiderFirst = 0, RiderPhases = 6, RiderCount = 4, TeamTentRed = RiderCount * RiderPhases, TeamTentBlue = TeamTentRed + 1;
    public static int RiderBody(int rider, int phase) => RiderFirst + rider * RiderPhases + phase;

    // The velodrome's bodies into the game folder: appended to BODY.HQR, each its number on a new entity at the end of RESS.HQR's entity
    // table. Its number (-1: not made) and a line for the build's log.
    public static (int Entity, string Log) InstallVelodrome(string gameDirectory)
    {
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var header = Body.Read(HqrArchive.Open(bodyPath).Read(HeaderBody), 2, allowStatic: true).Header;
        var table = HqrArchive.Open(ressPath).Read(44);
        if (Lba2EntityTable.Parse(table).Entities.All(e => e.Id != Prop)) return (-1, $"WARNING: the velodrome's riders: no prop entity {Prop} -- not made");
        (table, var entity) = RaceTrackCharacterCars.WithEntityLike(table, Prop);
        var bodyFile = File.ReadAllBytes(bodyPath);
        var next = HqrArchive.CountEntries(bodyPath);
        var first = next;
        foreach (var (generic, _, build) in VelodromeBodies(gameDirectory))
        {
            bodyFile = HqrWriter.AppendEntry(bodyFile, HqrWriter.StoredEntry(build(header).Write()));
            table = RaceTrackBaldinoCar.WithBody(table, entity, generic, next++);
        }
        File.WriteAllBytes(bodyPath, bodyFile);
        HqrWriter.ReplaceInFile(ressPath, 44, table);
        return (entity, $"the velodrome's riders ({RiderCount} x {RiderPhases} pedalling) and the teams' tents: entity {entity}, its bodies 0-{TeamTentBlue} BODY.HQR {first}-{next - 1}");
    }

    // the two teams' colours (each a ramp's start: a lit body's polygons climb their ramp with the light), the riders' characters
    private const int KitRed = 4 * 16, KitBlue = 12 * 16, KitYellow = 6 * 16, KitWhite = 3 * 16, KitBlack = 3 * 16;
    // (the black's light: the greys' ramp from its dark end, kept to its first few shades)
    private const float KitBlackLight = 0.3f;
    // (the tents' white canvas: up the greys' ramp from its start, the light taking it the rest of the way -- from the start it showed dark)
    private const int Canvas = KitWhite + 5;
    private static readonly (int Character, int Jersey, int Trim, string Name)[] RiderKits =
    {
        (120, KitRed, KitWhite, "a Citadel Comets rider"),
        (45, KitRed, KitWhite, "a Citadel Comets rider"),
        (120, KitBlue, KitYellow, "a Slate Express rider"),
        (45, KitBlue, KitYellow, "a Slate Express rider"),
    };

    // the track bike: its wheels, bottom bracket, cranks, saddle and drops (the hands' place)
    private const float TrackWheel = 255, TrackBase = 490, CrankLength = 150, PedalOut = 95;
    private static readonly Vector3 TrackBracket = new(0, 265, -40), TrackSaddle = new(0, 760, -230), TrackHip = new(0, 800, -215);
    private static readonly Vector3 TrackDrop = new(185, 640, 505);
    // the torso's lean forward from upright, the head's back up from it
    private const float RiderLean = 0.95f, HeadUp = 0.55f;

    internal static IEnumerable<(int, string, Func<byte[], Body>)> VelodromeBodies(string gameDirectory)
    {
        for (var r = 0; r < RiderCount; r++)
            for (var p = 0; p < RiderPhases; p++)
            {
                int rider = r, phase = p;
                yield return (RiderBody(r, p), $"{RiderKits[r].Name}, pedalling ({p + 1} of {RiderPhases})", h => BuildRider(h, gameDirectory, rider, phase));
            }
        yield return (TeamTentRed, "the Citadel Comets' tent", h => BuildTeamTent(h, KitRed, KitWhite));
        yield return (TeamTentBlue, "the Slate Express's tent", h => BuildTeamTent(h, KitBlue, KitYellow));
    }

    // the right pedal's angle (0 forward, a quarter up), the cranks turning forward over the top: each phase a part further round
    private static float CrankAngle(int phase) => MathF.PI / 2 - phase * MathF.Tau / RiderPhases;
    private static Vector3 Pedal(int phase, float side)
    {
        var a = CrankAngle(phase) + (side > 0 ? 0 : MathF.PI);
        return TrackBracket + new Vector3(side * PedalOut, CrankLength * MathF.Sin(a), CrankLength * MathF.Cos(a));
    }

    private static Body BuildRider(byte[] header, string gameDirectory, int rider, int phase)
    {
        var kit = RiderKits[rider];
        var character = Body.Read(HqrArchive.Open(Path.Combine(gameDirectory, "BODY.HQR")).Read(kit.Character), 2);
        var m = new CarMesh(kit.Name, new[] { -1 }) { Rounded = true, Limit = 1100 };
        TrackBike(m, kit.Trim, kit.Jersey, phase);
        SeatRider(m, character, kit.Jersey, phase);
        return m.ToBody(header);
    }

    // ---- the bike: a disc wheel behind, a deep rim and spokes in front, a fixed gear's chainring, drop bars, no brakes
    private static void TrackBike(CarMesh m, int frame, int rim, int phase)
    {
        const float R = TrackWheel, Base = TrackBase;
        void Bar(Vector3 a, Vector3 b, float t, int c) => m.Strut(0, a, b, t, c);
        foreach (var hz in new[] { -Base, Base })
        {
            var hub = new Vector3(0, R, hz);
            ShopTyre(m, hub, R, 22, 14);
            m.Light = 1f;
            if (hz < 0)
            {
                // the disc: a plate each side from the hub to the rim
                foreach (var side in new[] { -1f, 1f })
                {
                    var disc = m.Loop(0, hub + new Vector3(side * 14, 0, 0), new Vector3(0, R - 12, 0), new Vector3(0, 0, R - 12), 12);
                    var centre = m.P(0, hub + new Vector3(side * 22, 0, 0));
                    m.Cap(disc, centre, rim, hub - new Vector3(side * 40, 0, 0));
                }
            }
            else
            {
                // the deep rim and its spokes
                var outer = m.Loop(0, hub, new Vector3(0, R - 10, 0), new Vector3(0, 0, R - 10), 12);
                var inner = m.Loop(0, hub, new Vector3(0, R - 70, 0), new Vector3(0, 0, R - 70), 12);
                for (var k = 0; k < 12; k++) m.Both(new[] { outer[k], outer[(k + 1) % 12], inner[(k + 1) % 12], inner[k] }, rim);
                var h = m.P(0, hub);
                for (var k = 0; k < 12; k += 2) m.Line(h, inner[k], ShopSteel);
            }
        }
        Vector3 bracket = TrackBracket, seat = new(0, 690, -195), head = new(0, 700, 375), headLow = new(0, 590, 395);
        Vector3 rear = new(0, R, -Base), front = new(0, R, Base);
        Bar(bracket, seat, 40, frame);
        Bar(seat, head, 36, frame);
        Bar(bracket, headLow, 42, frame);
        Bar(bracket, rear, 24, frame);
        Bar(seat, rear, 22, frame);
        Bar(headLow, front, 28, frame);
        Bar(head, headLow, 38, frame);
        // the saddle on its post; the stem; the drops, round from the tops down and back
        Bar(seat, TrackSaddle - new Vector3(0, 20, -10), 22, ShopSteel);
        m.Box(0, TrackSaddle, new Vector3(90, 30, 230), ShopDark);
        var stem = head + new Vector3(0, 20, 90);
        Bar(head, stem, 26, ShopSteel);
        Bar(new Vector3(-TrackDrop.X, stem.Y, stem.Z), new Vector3(TrackDrop.X, stem.Y, stem.Z), 22, ShopDark);
        foreach (var s in new[] { -1f, 1f })
        {
            Vector3 top = new(s * TrackDrop.X, stem.Y, stem.Z), hook = new(s * TrackDrop.X, stem.Y - 40, TrackDrop.Z + 15),
                    drop = new(s * TrackDrop.X, TrackDrop.Y, TrackDrop.Z), end = new(s * TrackDrop.X, TrackDrop.Y - 10, TrackDrop.Z - 90);
            Bar(top, hook, 24, ShopDark);
            Bar(hook, drop, 24, ShopDark);
            Bar(drop, end, 24, ShopDark);
        }
        // the chainring on the right, the cranks and pedals turned to the phase
        var ring = m.Loop(0, bracket + new Vector3(PedalOut - 30, 0, 0), new Vector3(0, 95, 0), new Vector3(0, 0, 95), 10);
        var mid = m.P(0, bracket + new Vector3(PedalOut - 26, 0, 0));
        m.Light = 0.8f;
        m.Cap(ring, mid, ShopSteel, bracket);
        m.Light = 1f;
        foreach (var s in new[] { 1f, -1f })
        {
            var p = Pedal(phase, s);
            Bar(bracket + new Vector3(s * (PedalOut - 20), 0, 0), p - new Vector3(s * 10, 0, 0), 20, ShopSteel);
            m.Box(0, p + new Vector3(s * 25, 0, 0), new Vector3(60, 18, 80), ShopDark);
        }
    }

    // ---- the rider, from the character's own body: bent onto the bike, painted in the kit, a helmet on
    private static void SeatRider(CarMesh m, Body character, int jersey, int phase)
    {
        var driver = new CarDriver(character);
        var world = character.World();
        float floor = world.Min(v => v.Y), height = world.Max(v => v.Y) - floor;
        Vector3 Pivot(int b) => driver.Posed[character.Bones[b].Pivot];
        int[] Below(int b) => driver.Below(b);
        // the legs: each side's bone turning at the hips with the most below it, reaching the floor
        (int Thigh, int[] Lower) Leg(float side)
        {
            var best = -1; var bestY = float.MinValue;
            for (var b = 1; b < character.Bones.Count; b++)
            {
                var p = world[character.Bones[b].Pivot];
                if (p.X * side < 15 || p.Y - floor < height * 0.3f || p.Y - floor > height * 0.65f) continue;
                var below = Below(b);
                if (below.Length == 0) continue;
                var low = below.Append(b).SelectMany(x => Enumerable.Range(character.Bones[x].Start, character.Bones[x].Count)).Select(i => world[i].Y).DefaultIfEmpty(float.MaxValue).Min();
                if (low - floor > height * 0.12f) continue;
                if (p.Y > bestY) { best = b; bestY = p.Y; }
            }
            if (best < 0) throw new InvalidDataException($"{character.Bones.Count}-bone body: no leg on side {side}");
            return (best, Below(best));
        }
        var right = Leg(1); var left = Leg(-1);
        var (rightArm, leftArm) = driver.FindArms(new HashSet<int>(new[] { right.Thigh, left.Thigh }.Concat(right.Lower).Concat(left.Lower)));
        if (rightArm is not { } ra || leftArm is not { } la) throw new InvalidDataException("a rider without two arms");
        var chest = character.Bones[ra.Upper].Parent;
        var head = Below(chest).Where(b => character.Bones[b].Parent == chest && b != ra.Upper && b != la.Upper)
                               .OrderByDescending(b => world[character.Bones[b].Pivot].Y).First();
        var torso = Below(chest).Prepend(chest).ToArray();
        var headBones = Below(head).Prepend(head).ToArray();

        // the body's coordinates to the bike's: the hips' middle on the saddle, the legs as long as the pedals' far reach asks
        var hips = (Pivot(right.Thigh) + Pivot(left.Thigh)) / 2;
        float LegLength((int Thigh, int[] Lower) leg) { var knee = Pivot(leg.Lower[0]); return Vector3.Distance(Pivot(leg.Thigh), knee) + Vector3.Distance(knee, driver.Far(leg.Lower, knee)); }
        var scale = Math.Clamp(Vector3.Distance(TrackHip, TrackBracket) + CrankLength, 0, 2000) / (0.97f * (LegLength(right) + LegLength(left)) / 2);
        scale = Math.Clamp(scale, 0.85f, 1.3f);
        Vector3 Place(Vector3 p) => (p - hips) * scale + TrackHip;
        Vector3 Unplace(Vector3 p) => (p - TrackHip) / scale + hips;

        // the torso down over the bars, the head back up to look ahead
        driver.Turn(torso, Pivot(chest), Quaternion.CreateFromAxisAngle(Vector3.UnitX, RiderLean));
        driver.Turn(headBones, Pivot(head), Quaternion.CreateFromAxisAngle(Vector3.UnitX, -HeadUp));
        // the hands to the drops, the elbows out and down; the feet to the pedals, the knees forward
        Reach(driver, character, ra.Upper, ra.Fore, Unplace(TrackDrop), new Vector3(0.7f, -0.7f, 0));
        Reach(driver, character, la.Upper, la.Fore, Unplace(TrackDrop with { X = -TrackDrop.X }), new Vector3(-0.7f, -0.7f, 0));
        Reach(driver, character, right.Thigh, right.Lower, Unplace(Pedal(phase, 1) + new Vector3(-20, 20, 0)), new Vector3(0.12f, 0.25f, 1));
        Reach(driver, character, left.Thigh, left.Lower, Unplace(Pedal(phase, -1) + new Vector3(20, 20, 0)), new Vector3(-0.12f, 0.25f, 1));

        // the skinsuit: the torso and the upper arms the team's colour, the hips and the thighs black; the rest as the character is
        var suit = torso.Except(headBones).Except(ra.Fore).Except(la.Fore).ToHashSet();
        var shorts = Enumerable.Range(0, character.Bones.Count).Where(b => !torso.Contains(b) && !right.Lower.Contains(b) && !left.Lower.Contains(b)).ToHashSet();
        (int, float, int)? Paint(Face f, int[] bones)
        {
            if (bones.All(suit.Contains)) return (jersey, CarDriver.Own, CarDriver.Flat);
            if (bones.All(b => shorts.Contains(b) || b == right.Thigh || b == left.Thigh)) return (KitBlack, KitBlackLight, CarDriver.Flat);
            return (f.Colour, CarDriver.Own, f.Material == 0 ? 0 : -1);
        }
        m.Light = 1;
        driver.Seat(m, Enumerable.Range(0, character.Bones.Count).ToHashSet(), Place, -10000, scale, Paint, bone: 0);

        // the helmet: a shell over the top of the head, drawn out behind
        var headPoints = headBones.SelectMany(b => Enumerable.Range(character.Bones[b].Start, character.Bones[b].Count)).Select(i => Place(driver.Posed[i])).ToList();
        if (headPoints.Count > 0)
        {
            var lo = headPoints.Aggregate(Vector3.Min); var hi = headPoints.Aggregate(Vector3.Max);
            var c = (lo + hi) / 2;
            float rx = (hi.X - lo.X) / 2 * 1.08f + 12, ry = (hi.Y - lo.Y) / 2 * 0.75f + 10, rz = (hi.Z - lo.Z) / 2 * 1.1f + 12;
            var centre = new Vector3(c.X, c.Y + ry * 0.35f, c.Z);
            int[]? last = null;
            foreach (var lat in new[] { 0f, 0.45f, 0.85f, 1.2f })
            {
                float cr = MathF.Cos(lat), sr = MathF.Sin(lat);
                var ringNow = m.Loop(0, centre + new Vector3(0, ry * sr, -rz * 0.15f * (1 - sr)), new Vector3(rx * cr, 0, 0), new Vector3(0, 0, rz * cr), 10);
                if (last is not null) m.Skin(last, ringNow, _ => jersey, centre);
                last = ringNow;
            }
            m.Cap(last!, m.P(0, centre + new Vector3(0, ry * 1.02f, -rz * 0.05f)), jersey, centre);
            // its tail, back over the neck
            var tail = m.P(0, centre + new Vector3(0, ry * 0.2f, -rz * 1.55f));
            var rim = m.Loop(0, centre, new Vector3(rx, 0, 0), new Vector3(0, 0, rz), 10);
            for (var k = 0; k < 10; k++)
            {
                var a = (k + 0.5f) * MathF.Tau / 10;
                if (MathF.Sin(a) > -0.2f) continue;   // (the back half only)
                m.Out(new[] { rim[k], rim[(k + 1) % 10], tail }, jersey, centre + new Vector3(0, -ry, 0));
            }
        }
    }

    // A limb turned so its far end is at `target` (the body's own coordinates), keeping its two lengths; the joint between bent towards
    // `hint`. From where the limb is now posed (CarDriver.Reach measures from the body as it stood: a torso leaned first moved the shoulders).
    private static void Reach(CarDriver driver, Body character, int upper, int[] fore, Vector3 target, Vector3 hint)
    {
        Vector3 Pivot(int b) => driver.Posed[character.Bones[b].Pivot];
        var root = Pivot(upper); var joint = Pivot(fore[0]); var end = driver.Far(fore, joint);
        float l1 = Vector3.Distance(root, joint), l2 = Vector3.Distance(joint, end);
        var reach = target - root;
        var d = Math.Clamp(reach.Length(), Math.Abs(l1 - l2) + 1, l1 + l2 - 1);
        var u = Vector3.Normalize(reach);
        var along = (l1 * l1 - l2 * l2 + d * d) / (2 * d);
        var h = MathF.Sqrt(MathF.Max(0, l1 * l1 - along * along));
        var bend = hint - u * Vector3.Dot(hint, u);
        bend = bend.LengthSquared() < 1e-6f ? Vector3.UnitZ : Vector3.Normalize(bend);
        var newJoint = root + u * along + bend * h;
        driver.Turn(fore.Prepend(upper), root, CarDriver.Between(joint - root, newJoint - root));
        driver.Turn(fore, newJoint, CarDriver.Between(driver.Far(fore, newJoint) - newJoint, root + u * d - newJoint));
    }

    // ---- a team's tent in the infield: four poles, a peaked roof of white canvas panels and the team's colour, its second colour round
    // its edge (the valance)
    private static Body BuildTeamTent(byte[] header, int colour, int stripe)
    {
        var m = new CarMesh("a team's tent", new[] { -1 }) { Rounded = true, Limit = 1100 };
        m.Light = 1f;
        const float Half = 900, Eave = 1500, Peak = 2350, Valance = 180;
        foreach (var (x, z) in new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })
            m.Strut(0, new Vector3(x * (Half - 30), 0, z * (Half - 30)), new Vector3(x * (Half - 30), Eave, z * (Half - 30)), 40, ShopSteel);
        var corners = new[] { new Vector3(Half, Eave, Half), new Vector3(-Half, Eave, Half), new Vector3(-Half, Eave, -Half), new Vector3(Half, Eave, -Half) };
        var peak = new Vector3(0, Peak, 0);
        var inside = new Vector3(0, Eave, 0);
        for (var k = 0; k < 4; k++)
        {
            Vector3 a = corners[k], b = corners[(k + 1) % 4];
            // each side of the roof in three stripes, from the eave to the peak
            for (var s = 0; s < 3; s++)
            {
                Vector3 a0 = Vector3.Lerp(a, b, s / 3f), a1 = Vector3.Lerp(a, b, (s + 1) / 3f);
                var hs = new[] { m.P(0, a0), m.P(0, a1), m.P(0, peak) };
                m.Out(hs, s == 1 ? colour : Canvas, inside);
                m.In(new[] { m.P(0, a0), m.P(0, a1), m.P(0, peak) }, s == 1 ? colour : Canvas, inside - new Vector3(0, 600, 0));
            }
            // the valance: a band hanging from the eave
            var va = new[] { m.P(0, a), m.P(0, b), m.P(0, b - new Vector3(0, Valance, 0)), m.P(0, a - new Vector3(0, Valance, 0)) };
            m.Both(va, colour);
            // and its second colour's band along its foot
            Vector3 Proud(Vector3 v, float down) => new(v.X * 1.012f, v.Y - down, v.Z * 1.012f);
            var vb = new[] { m.P(0, Proud(a, Valance - 40)), m.P(0, Proud(b, Valance - 40)), m.P(0, Proud(b, Valance + 5)), m.P(0, Proud(a, Valance + 5)) };
            m.Both(vb, stripe);
        }
        return m.ToBody(header);
    }
}
