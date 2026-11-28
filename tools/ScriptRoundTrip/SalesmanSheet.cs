namespace ScriptRoundTrip;

// salesmansheet <LBA1 folder> <game folder> <out.png>: LBA1's hair dryer salesman as LBA2 makes him (Terrain/HairDryerSalesman: his body and
// his four animations converted), his animations' keyframes drawn a row each -- standing, walking, his stagger, his knock
internal static class SalesmanSheet
{
    public static int Run(string[] args)
    {
        if (args.Length < 4) { Console.WriteLine("salesmansheet <LBA1 folder> <game folder> <out.png>"); return 1; }
        var body = LBAAssembler.Terrain.HairDryerSalesman.Body(args[1], args[2]);
        var anims = LBAAssembler.Terrain.HairDryerSalesman.Animations(args[1]);
        Console.WriteLine($"body: {body.Vertices.Count} points, {body.Bones.Count} bones, {body.Faces.Count} faces, {body.Lines.Count} lines, {body.Spheres.Count} spheres");
        foreach (var (generic, anim) in anims) Console.WriteLine($"generic {generic}: {anim.Frames.Count} frames, {anim.Frames[0].Bones.Length} bone slots, {anim.Frames.Sum(f => f.FrameTime)} ms");
        // (through the bytes, as the game reads them)
        var round = LbaBodyStudio.Body.Read(body.Write(), 2);
        AnimSheetCommand.Render(round, anims.Select(a => a.Anim.Write()).ToList(), args[3]);
        Console.WriteLine(args[3]);
        return 0;
    }
}
