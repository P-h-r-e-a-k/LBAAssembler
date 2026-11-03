namespace LBAAssembler.LbaScript;

// The 1996 LBA2 demo's script language (E:\Downloads\LBA2-96-Demo\LBA2DEMO.EXE). Its conditions (0-40) are
// the retail ones; its track instructions (0-39) too but for SPRITE, whose sprite number is one byte.
// Its life instructions stop at 148 and differ from retail
// in a few places, found by disassembling the demo's DoLife switch (jump table at 0x236EC) and tracking how
// far each case moves the script pointer (2026-11-03):
//   * SAMPLE, SAMPLE_RND, SAMPLE_ALWAYS and SAMPLE_STOP take a one-byte sample number (retail: two bytes);
//     REPEAT_SAMPLE a byte sample and a byte count.
//   * 69, 73, 83-87, 90-92, 103, 104 and 111 have no case at all in the demo's engine (retail ASK_CHOICE,
//     CLR_HOLO_POS, SET_FRAME...IMPACT_POINT, NO_CHOC...CINEMA_MODE, ANIM_TEXTURE, ADD_MESSAGE_OBJ,
//     STATE_INVENTORY), yet its scripts use ASK_CHOICE and ASK_CHOICE_OBJ with the retail operands: the
//     scripts were compiled for a newer language than the demo runs. They keep the retail operands here.
//   * 70, 72, 88, 99 and 132 are instructions of the demo's own (DEMO_*, below).
// docs/LBA2_1996_DEMO.md has the details.
internal static class Demo96Tables
{
    private static IEnumerable<LifeOpDef> Life()
    {
        foreach (var d in Lba2Tables.LifeDefs)
        {
            if (d.Id > 148) continue;
            yield return d.Id switch
            {
                // two bytes: a 0x30-byte record's index and an on/off flag (bit 0 of the record's third byte)
                70 => new(70, "DEMO_70", LifeForm.Plain, new[] { Lba2Tables.U8("index"), Lba2Tables.U8("on") }),
                // no bytes: shows the holomap's on-screen icon when the holomap is owned (retail SET_HOLO_POS without the position)
                72 => new(72, "DEMO_72", LifeForm.Plain, Lba2Tables.None),
                // an actor: throws extras about it (random offsets of +-256/128/256 around its position)
                88 => new(88, "DEMO_88", LifeForm.Plain, new[] { Lba2Tables.Obj() }),
                // no bytes: one call into the palette code (beside PALETTE / FADE_TO_PAL / ECLAIR)
                99 => new(99, "DEMO_99", LifeForm.Plain, Lba2Tables.None),
                122 or 123 or 124 or 125 => d with { Args = new[] { Lba2Tables.U8("sample") } },
                126 => d with { Args = new[] { Lba2Tables.U8("sample"), Lba2Tables.U8("count") } },
                _ => d,
            };
            // no bytes: in scenes below 20, the talking actor says a line -- the hero one of five picked at random
            // from a per-scene table in the executable, any other actor its own
            if (d.Id == 131) yield return new(132, "DEMO_132", LifeForm.Plain, Lba2Tables.None);
        }
    }

    public static OpcodeSet Build() => new()
    {
        Name = "LBA2 1996 demo",
        LifeDefs = Life().ToArray(),
        // SPRITE takes a one-byte sprite number (retail: two bytes)
        TrackDefs = Lba2Tables.TrackDefs.Where(d => d.Id <= 39).Select(d => d.Id == 38 ? d with { Args = new[] { Lba2Tables.U8("sprite") } } : d).ToArray(),
        CondDefs = Lba2Tables.CondDefs.Where(d => d.Id <= 40).ToArray(),
        MoveNames = Lba2Tables.MoveNames,
        MoveTakesParam = Lba2Tables.MoveTakesParam,
        AndIf = Opcodes.Lba2.AndIf,
        HasSwitch = true,
        TrackHiddenDefault = Opcodes.Lba2.TrackHiddenDefault,
    };
}
