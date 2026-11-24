using System.Buffers.Binary;
using System.IO;
using System.Linq;
using LbaBodyStudio;

namespace LBAAssembler.Terrain.Citadel;

// The lighthouse's telescope and the Wizard's hop (the user, 2026-11-03: "The lighthouse in Citadel.ile and Citabau.ile let's allow Twinsen
// to enter the door and get to the top. Let's also add in some binoculars like the ones already on Citadel island to the top of the
// lighthouse - the same area Twinsen stands in the scripted segment. If Twinsen uses the telescope let's have a small scripted cut scene of
// the weather wizard standing behind the cow and using Twinsen's hop forward with the 'x' key animation and the cow producing clovers the
// same as it does when Twinsen does this"; "this should only be after Raph has already unlocked it and not before"). The scenes' side is
// RaceTrackStory.Chapters (LighthouseClimb); here: the telescope on the lamp room's balcony, and the Wizard's hop.
internal static class CitadelLighthouse
{
    // ---- the telescope ----------------------------------------------------------------------------------------------------------------
    // On the lighthouse's balcony (cube (7,10): the tower, its decor at (13952, 3250, 11776), its floor up at 7628; the west railing x
    // 12223-12439, its top at 8331), west of where Twinsen stands in the spell's scene (track point 5, (13425, 7628, 11122)) -- he faces it
    // there, his back to the lamp room. A spyglass of the story's on the west railing's top (CitadelProps.RailScope, an actor of scene 46:
    // RaceTrackStory.Chapters' LighthouseClimb). Until 2026-11-09 the island's coin telescope from the park (cube (8,8)) was copied there,
    // post and all: "it's sticking out too far, we could probably mount it on the side of the lighthouse" (the user, 2026-11-08).
    // (on the north railing -- x 12223-13736, z 10317-10479, its top at 8168 -- west of the lamp room, the side towards the ship; aimed across
    // the island at the cow's field, scene 47's cube (8,7), where Rosa and the Wizard are: from the lighthouse, cube (7,10), that is north
    // and a little east -- the user, 2026-11-10: "should be pointing towards the other end of the island near the cow - so it needs moving
    // onto the wall nearest to the ship". Twinsen uses it facing north. Its tube is built along -x (CitadelProps.BuildRailScope): turned
    // by ScopeBeta onto the cow's bearing.)
    public const int ScopeX = 12850, ScopeY = 8168, ScopeZ = 10398;
    private const int CowIslandX = 8 * 32768 + 22600, CowIslandZ = 7 * 32768 + 23552, TopIslandX = 7 * 32768 + 13425, TopIslandZ = 10 * 32768 + 11122;
    public static int ScopeBeta => (RaceTrackMath.Turn(Math.Atan2(CowIslandX - TopIslandX, CowIslandZ - TopIslandZ)) - 3072) & 4095;

    // ---- the Wizard's hop ---------------------------------------------------------------------------------------------------------------
    // Twinsen's hop forward (entity 0's animation 42, the one the cow counts) on the weather wizard (entity 67, BODY.HQR 111: 21 bones to
    // Twinsen's 24 -- his robe's four his own): each bone the Wizard shares with Twinsen turned from the Wizard's own standing as far as
    // Twinsen's turns from his, his hands and robe as he stands. In place: the hop's steps left out, he hops where he stands behind her.
    public const int Wizard = 67, WizardBody = 111, Hop = 643;
    private const int Twinsen = 0, TwinsenHop = 42;
    // (the Wizard's bone slots, each from Twinsen's: -1 his own standing)
    private static readonly int[] FromTwinsen = { -1, 1, 2, 3, 4, 5, -1, 6, 7, -1, 8, 9, 10, 11, 12, 13, 14, -1, -1, -1, -1 };

    // Into the game folder: the animation (ANIM.HQR's end) as the Wizard's Hop (RESS.HQR's entity table). Lines for the build's log.
    public static List<string> Install(string gameDirectory)
    {
        var animPath = Path.Combine(gameDirectory, "ANIM.HQR");
        var bodyPath = Path.Combine(gameDirectory, "BODY.HQR");
        var ressPath = Path.Combine(gameDirectory, "RESS.HQR");
        var table = HqrArchive.Open(ressPath).Read(44);
        var entities = Lba2EntityTable.Parse(table).Entities;
        var wizard = entities.FirstOrDefault(e => e.Id == Wizard);
        var twinsen = entities.FirstOrDefault(e => e.Id == Twinsen);
        int AnimOf(Lba2EntityTable.Entity? e, int generic) => e is not null && e.Anims.Any(a => a.Generic == generic) ? e.Anims.First(a => a.Generic == generic).Anim : -1;
        if (wizard is null || RaceTrackGuards.BodyOf(table, Wizard, 0) != WizardBody || AnimOf(wizard, 0) < 0 || AnimOf(twinsen, 0) < 0 || AnimOf(twinsen, TwinsenHop) < 0)
            return new() { $"WARNING: the Wizard's hop: entity {Wizard} isn't the game's weather wizard (BODY.HQR {WizardBody}), or Twinsen's hop isn't there -- not made" };
        if (wizard.Anims.Any(a => a.Generic == Hop)) return new();
        var anims = HqrArchive.Open(animPath);
        var hop = WizardHop(anims.Read(AnimOf(twinsen, TwinsenHop)), anims.Read(AnimOf(twinsen, 0)), anims.Read(AnimOf(wizard, 0)));
        var animIndex = HqrArchive.CountEntries(animPath);
        File.WriteAllBytes(animPath, HqrWriter.AppendEntry(File.ReadAllBytes(animPath), HqrWriter.StoredEntry(hop.Write())));
        table = RaceTrackJumpAnim.WithAnim(table, Wizard, Hop, animIndex);
        var log = new List<string> { $"the Wizard's hop: ANIM.HQR {animIndex} ({hop.Frames.Count} keyframes, Twinsen's hop forward, ANIM.HQR {AnimOf(twinsen, TwinsenHop)}), entity {Wizard}'s animation {Hop}" };

        // the Wizard and Rosa as one body, standing and hopping (the telescope's shot: RaceTrackStory.Chapters' LighthouseClimb)
        var rosaBody = RaceTrackGuards.BodyOf(table, RosaEntity, 0);
        if (rosaBody is null || wizard.Bodies.Any(b => b.Generic == CowPair) || wizard.Anims.Any(a => a.Generic is PairStand or PairHop))
            log.Add($"WARNING: the Wizard and Rosa as one: Rosa (entity {RosaEntity}) has no body, or entity {Wizard} has body {CowPair} or animation {PairStand}/{PairHop} already -- not made");
        else
        {
            var bodies = HqrArchive.Open(bodyPath);
            byte[] pair;
            int rosaPoints;
            try { pair = PairBody(bodies.Read(WizardBody), bodies.Read(rosaBody.Value), RosaInPair, out rosaPoints); }
            catch (InvalidDataException e)
            {
                log.Add($"WARNING: the Wizard and Rosa as one: {e.Message}");
                File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
                return log;
            }
            var bodyIndex = HqrArchive.CountEntries(bodyPath);
            File.WriteAllBytes(bodyPath, HqrWriter.AppendEntry(File.ReadAllBytes(bodyPath), HqrWriter.StoredEntry(pair)));
            var stand = PairAnim(anims.Read(AnimOf(wizard, 0)));
            var pairHop = PairAnim(hop.Write());
            var pairIndex = HqrArchive.CountEntries(animPath);
            var bytes = HqrWriter.AppendEntry(File.ReadAllBytes(animPath), HqrWriter.StoredEntry(stand.Write()));
            File.WriteAllBytes(animPath, HqrWriter.AppendEntry(bytes, HqrWriter.StoredEntry(pairHop.Write())));
            table = RaceTrackBaldinoCar.WithBodyLike(table, Wizard, CowPair, bodyIndex, 0);
            table = RaceTrackJumpAnim.WithAnim(table, Wizard, PairStand, pairIndex);
            table = RaceTrackJumpAnim.WithAnim(table, Wizard, PairHop, pairIndex + 1);
            log.Add($"the Wizard and Rosa as one: BODY.HQR {bodyIndex} (his {WizardBody} and her {rosaBody} -- {rosaPoints} points -- her {RosaInPair} from him), " +
                    $"entity {Wizard}'s body {CowPair}; standing and hopping ANIM.HQR {pairIndex} and {pairIndex + 1}, its animations {PairStand} and {PairHop}");
        }
        File.WriteAllBytes(ressPath, HqrWriter.ReplaceEntry(File.ReadAllBytes(ressPath), 44, HqrWriter.StoredEntry(table)));
        return log;
    }

    // ---- the Wizard and Rosa as one -------------------------------------------------------------------------------------------------
    // The telescope's shot of the Wizard hopping behind Rosa is one actor whose body is the two of them (the user, 2026-11-20: "This is a
    // scripted scene that we view through a telescope, wouldn't it be easier to just create a single actor for us to watch that's the
    // combination of the wizard and rosa?"): two actors pressed together are sorted whole, one drawn over the other -- his fur painted on her
    // flank as he hopped ("Get the wizard closer, fix the spot issue at the source please") -- where one body's polygons are sorted together.
    // His bones, and one more on his root holding her as she stands (she doesn't move in the shot); his standing and his hop with that bone
    // held still. The pair faces as Rosa does; he stands PairBack times the game's own distance behind her (WizardAtCow, 626 from her).
    public const int CowPair = 30, PairStand = 644, PairHop = 645, RosaEntity = 119;
    public static readonly (int X, int Y, int Z) RosaAt = (21984, 8750, 23440), WizardAtCow = (22600, 8750, 23552);
    public const int RosaBeta = 3000;
    public const double PairBack = 1.0;
    // (the Wizard's place from Rosa's, in the world; and Rosa's from his in the pair's own frame -- world to body: the engine's Rotate
    // turned back, x = X cos - Z sin, z = X sin + Z cos)
    public static (int X, int Y, int Z) WizardFromRosa => ((int)Math.Round((WizardAtCow.X - RosaAt.X) * PairBack), WizardAtCow.Y - RosaAt.Y, (int)Math.Round((WizardAtCow.Z - RosaAt.Z) * PairBack));
    public static System.Numerics.Vector3 RosaInPair
    {
        get
        {
            var (x, y, z) = WizardFromRosa;
            var a = RosaBeta * 2 * Math.PI / 4096;
            return new System.Numerics.Vector3((float)Math.Round(-x * Math.Cos(a) + z * Math.Sin(a)), -y, (float)Math.Round(-x * Math.Sin(a) - z * Math.Cos(a)));
        }
    }

    // His body with hers on one more bone (child of his root, pivoted on his root's first point): her points where she stands, `rosaAt` from
    // his origin; her polygons' points and lights moved past his (the points' lights first, then the faces': his faces' after her points).
    internal static byte[] PairBody(byte[] wizard, byte[] rosa, System.Numerics.Vector3 rosaAt, out int rosaPoints)
    {
        var w = Palace.PalaceTorturers.Parts.Read(wizard);
        var r = Palace.PalaceTorturers.Parts.Read(rosa);
        if (w.Bones[0].Count < 1) throw new InvalidDataException("the Wizard's root bone has no point to pivot Rosa on");
        if (r.Blocks.Any(k => (k.Type & 0x4000) != 0)) throw new InvalidDataException("Rosa's body has environment-mapped polygons: not merged");
        if (w.Points.Count != w.Normals.Count || r.Points.Count != r.Normals.Count) throw new InvalidDataException("a normal a point, in both bodies");
        static int U(byte[] b, int at) => BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(at));
        static void W(byte[] b, int at, int v) => BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(at), checked((ushort)v));
        int nW = w.Points.Count, fW = w.FaceNormalCount, nR = r.Points.Count, bone = w.Bones.Count;
        var origin = w.World()[0];
        var her = r.World();
        foreach (var (_, records) in w.Blocks)
            foreach (var rec in records)
                if (U(rec, 10) >= nW) W(rec, 10, U(rec, 10) + nR);
        foreach (var (type, records) in r.Blocks)
        {
            var corners = (type & 0x8000) != 0 ? 4 : 3;
            foreach (var rec in records)
            {
                for (var c = 0; c < corners; c++) W(rec, c * 2, U(rec, c * 2) + nW);
                W(rec, 10, U(rec, 10) < nR ? U(rec, 10) + nW : U(rec, 10) + nW + fW);
                // (a textured one's texture: hers after his in the body's list -- STRUC_POLY3_TEXTURE's HandleText at 6, the quad's at 28)
                if ((type & 0xFF) > 7) W(rec, corners == 4 ? 28 : 6, U(rec, corners == 4 ? 28 : 6) + w.TextureCount);
            }
            w.Blocks.Add((type, records));
        }
        static short S(float v) => checked((short)Math.Round(v));
        for (var i = 0; i < nR; i++)
        {
            var q = her[i] + rosaAt - origin;
            w.Points.Add((S(q.X), S(q.Y), S(q.Z), bone));
            var n = r.Normals[i];
            w.Normals.Add((n.X, n.Y, n.Z, bone));
        }
        w.Bones.Add((0, 0, nR, r.FaceNormalCount));
        w.FaceNormals = w.FaceNormals.Concat(r.FaceNormals).ToArray();
        w.FaceNormalCount = fW + r.FaceNormalCount;
        byte[] Shifted(byte[] list, int count, params int[] fields)
        {
            var copy = (byte[])list.Clone();
            for (var i = 0; i < count; i++) foreach (var f in fields) W(copy, i * 8 + f, U(copy, i * 8 + f) + nW);
            return copy;
        }
        w.Textures = w.Textures.Concat(r.Textures).ToArray(); w.TextureCount += r.TextureCount;
        w.Lines = w.Lines.Concat(Shifted(r.Lines, r.LineCount, 4, 6)).ToArray(); w.LineCount += r.LineCount;
        w.Spheres = w.Spheres.Concat(Shifted(r.Spheres, r.SphereCount, 4)).ToArray(); w.SphereCount += r.SphereCount;
        var all = w.World();
        int[] box = { (int)Math.Floor(all.Min(v => v.X)), (int)Math.Ceiling(all.Max(v => v.X)), (int)Math.Floor(all.Min(v => v.Y)), (int)Math.Ceiling(all.Max(v => v.Y)),
                      (int)Math.Floor(all.Min(v => v.Z)), (int)Math.Ceiling(all.Max(v => v.Z)) };
        for (var b = 0; b < 6; b++) BinaryPrimitives.WriteInt32LittleEndian(w.Header.AsSpan(8 + b * 4), box[b]);
        // (cut at the engine's close plane, not its near one: the telescope's camera stands a few cells from the pair, and her head, nearest
        // it, was cut away at the near plane -- AFF_OBJ.CPP MASK_OBJECT_CLOSE. The user, 2026-11-24: "Half of the cow's head isn't displaying
        // cleanly when viewed through the telescope - it's invisible")
        BinaryPrimitives.WriteInt32LittleEndian(w.Header, BinaryPrimitives.ReadInt32LittleEndian(w.Header) | Palace.BrickBuilding.CloseFlag);
        rosaPoints = nR;
        return w.Write();
    }

    // One of his animations for the pair: his bone slots as they are, steps none, and her bone's slot held still
    internal static Anim PairAnim(byte[] his)
    {
        var anim = new Anim { Game = 2, LoopFrame = BinaryPrimitives.ReadUInt16LittleEndian(his.AsSpan(4)) };
        foreach (var (time, bones) in Raw(his))
        {
            var slots = bones.Select((v, b) => b == 0 ? AnimBone.Neutral : v.Type == 1 ? new AnimBone(1, v.X, v.Y, v.Z) : new AnimBone(0, v.X / 4096f, v.Y / 4096f, v.Z / 4096f)).ToList();
            slots.Add(AnimBone.Neutral);
            anim.Frames.Add(new AnimFrame(time, 0, 0, 0, slots.ToArray()));
        }
        return anim;
    }

    public static Anim WizardHop(byte[] hop, byte[] twinsenStand, byte[] wizardStand)
    {
        var frames = Raw(hop);
        var tw = Raw(twinsenStand)[0].Bones;
        var wz = Raw(wizardStand)[0].Bones;
        if (wz.Length < FromTwinsen.Length || tw.Length <= FromTwinsen.Max() || frames[0].Bones.Length <= FromTwinsen.Max())
            throw new InvalidDataException("The Wizard's or Twinsen's animations aren't the game's: too few bones.");
        var anim = new Anim { Game = 2, LoopFrame = BinaryPrimitives.ReadUInt16LittleEndian(hop.AsSpan(4)) };
        foreach (var (time, v) in frames)
        {
            var bones = new AnimBone[FromTwinsen.Length];
            bones[0] = AnimBone.Neutral;
            for (var b = 1; b < bones.Length; b++)
            {
                var own = wz[b];
                var m = FromTwinsen[b];
                var (x, y, z) = (own.X, own.Y, own.Z);
                if (m >= 0 && (v[m].Type & 1) == (own.Type & 1) && (tw[m].Type & 1) == (own.Type & 1))
                    (x, y, z) = (x + v[m].X - tw[m].X, y + v[m].Y - tw[m].Y, z + v[m].Z - tw[m].Z);
                bones[b] = (own.Type & 1) == 1 ? new AnimBone(1, x, y, z) : new AnimBone(0, x / 4096f, y / 4096f, z / 4096f);
            }
            anim.Frames.Add(new AnimFrame(time, 0, 0, 0, bones));
        }
        return anim;
    }

    // An animation's keyframes as the file has them: each bone slot's type and three values (an angle's 4096ths of a turn, a translation's units)
    private static List<(int Time, (int Type, int X, int Y, int Z)[] Bones)> Raw(byte[] d)
    {
        int frames = BinaryPrimitives.ReadUInt16LittleEndian(d), have = BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(2));
        var list = new List<(int, (int, int, int, int)[])>();
        for (var f = 0; f < frames; f++)
        {
            var at = 8 + f * (8 + have * 8);
            var bones = new (int, int, int, int)[have];
            for (var b = 0; b < have; b++)
            {
                var q = at + 8 + b * 8;
                short S(int o) => BinaryPrimitives.ReadInt16LittleEndian(d.AsSpan(q + o));
                var type = S(0) & 1;
                // (an angle as the engine reads it: 12 bits, signed)
                int A(int o) => type == 1 ? S(o) : ((S(o) & 0xFFF) ^ 0x800) - 0x800;
                bones[b] = (type, A(2), A(4), A(6));
            }
            list.Add((BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(at)), bones));
        }
        return list;
    }
}
