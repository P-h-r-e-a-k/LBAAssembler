using LBAAssembler.LbaScript;
using LBAAssembler.Scenes;
using LBAAssembler.Terrain.Citadel;

namespace LBAAssembler.Terrain;

// Citadel Island's two LBA1 rooms, the story's side (the user, 2026-11-04: "Let's make all those changes", of the easter eggs proposal:
// "The LBA1 secret room in the sewers" for chapter 1 and "The Tavern's cellar, 'exactly as it was'" for chapter 2). The rooms themselves,
// the doorway in the sewers' wall and the props' bodies are the build's (CitadelLba1Rooms, run before the story); here, in the scenes:
//   - the demo's sewers (scene 17): the loose slab of red-brown bricks in the doorway at the foot of the far halls' stairs, from the thief
//     caught on (Q_THIEF 5, or any later chapter). Hit -- a magic ball, or Twinsen's fist in aggressive mode, both the engine's hit_by() 0
//     -- or the Action key beside it in aggressive mode (the key that acts in every behaviour, action(): in aggressive mode the other one
//     fights): it slides back into the dark (the game's sewer door's sound, 350) and the doorway's zone opens (set_change_cube): into the
//     secret room. Come back out of it, Twinsen finds it open (it starts so with him beside it).
//   - the secret room (scene 226): LBA1's secret sewer, three old signs leaning on its west wall -- "CLONE CHECKPOINT: PAPERS PLEASE", "BY
//     ORDER OF DR FUNFROCK", "WANTED: TWINSEN" -- each read with Action, and a clover box (once: EggSewerClover, the game's clover box
//     copied, scene 13's actor 16).
//   - the painter (the demo's, actor 7 of scene 17), asked once the box is taken: "Those old signs? I painted them, back when FunFrock was
//     paying. I'd rather not talk about it." (once a visit; then his own line again)
//   - the Tavern (scene 3), chapter 2 on: a hatch behind Tim's bar (sprite 280, RaceTrackScenes.HatchSprite); Action on it, down into the
//     cellar (change_cube). And Tim asked, once the cellar's clover box is taken: "We never had a cellar. ...Did we? They said 'exactly as
//     it was'." (once a visit, before his own lines)
//   - the cellar (scene 227): LBA1's Tavern cellar -- its barrels and wine jars are LBA1's bricks -- a dusty bottle on its floor (Action: its
//     label's year, 1994) and a clover box (once: EggCellarClover); its door up leads back behind the bar.
// The lines: new texts at the end of Citadel Island's text file (3), in the six languages (GroupB.AddLines).
internal static partial class RaceTrackStory
{
    // the doorway's zone into the secret room, switched on by the slab (set_change_cube); a per-visit flag of the painter's and Tim's
    // (var_cube 42: no script of the scenes uses it)
    private const int SecretDoorSwitch = 215, Once3 = 42;
    // the slab's slide: the game's sewer door's sound, how far back it goes (into the dark behind the wall: its box, 227 either way of
    // it, clear of the doorway) and in how many steps
    private const int SlideSample = 350, SlideTo = -460, SlideSteps = 8, StepLabel = 20;
    // Twinsen this near it as the scene starts: he has just come back out of the room (CitadelLba1Rooms.SewerReturn: on the landing, 1,500
    // from it at most; the nearest other way in, the far halls' door at the top of the stairs, lands him 2,700 or so away), and finds it open
    private const int BackOut = 1800;
    // the game's clover box (scene 13's actor 16: a sprite, its pick-up sound 506)
    private const int CloverScene = 13, CloverActor = 16, CloverSample = 506;
    // aggressive behaviour (COMMON.H C_AGRESSIF)
    private const int Aggressive = 2;

    // the secret room's signs: along its west wall (x 49: its face at 25600), between the elephant's plinth (z 49-50) and the low ladder's
    // posts (z 55-56), on its floor (layer 8, 2304), each with its line -- the north wall's free stretch (x 58-62) is behind the east wall's
    // tall end from where the camera looks; turned (SignBeta) from the bodies' own way, their face to +z, to face the room. The clover box
    // on the floor before the elephant's statue; the cellar's bottle by LBA1's bottles and barrel at the wine jars' end of its floor
    // (layer 0, 256), its clover box further along that floor
    private static readonly (int Z, int Generic, Lba1RoomLine Line)[] Signs =
    {
        (26450, CitadelLba1Rooms.SignCheckpoint, Lba1RoomLine.SignCheckpoint),
        (27100, CitadelLba1Rooms.SignOrder, Lba1RoomLine.SignOrder),
        (27750, CitadelLba1Rooms.SignWanted, Lba1RoomLine.SignWanted),
    };
    private const int SignWallX = 25610, SignBeta = 1024, RoomFloor = 2304;
    private static readonly (int X, int Y, int Z) RoomClover = (27136, 2304, 28672);
    private static readonly (int X, int Y, int Z) CellarBottle = (20224, 256, 2304), CellarClover = (21760, 256, 5376);

    private enum Lba1RoomLine { SignCheckpoint, SignOrder, SignWanted, TwinsenWanted, PainterSigns, TimCellar, Bottle }
    private static readonly string[][] Lba1RoomLines =
    {
        new[]
        {
            "An old sign: \"CLONE CHECKPOINT. PAPERS, PLEASE.\" The paint is flaking off.",
            "Un vieux panneau : \"POSTE DE CONTRÔLE DES CLONES. VOS PAPIERS, S'IL VOUS PLAÎT.\" La peinture s'écaille.",
            "Ein altes Schild: \"KLON-KONTROLLPUNKT. PAPIERE, BITTE.\" Die Farbe blättert ab.",
            "Un viejo cartel: \"CONTROL DE CLONES. SUS PAPELES, POR FAVOR.\" La pintura se está desconchando.",
            "Un vecchio cartello: \"POSTO DI CONTROLLO DEI CLONI. DOCUMENTI, PREGO.\" La vernice si sta scrostando.",
            "Um letreiro velho: \"POSTO DE CONTROLO DE CLONES. DOCUMENTOS, POR FAVOR.\" A tinta está a descascar.",
        },
        new[]
        {
            "Another sign: \"BY ORDER OF DR FUNFROCK.\" Whatever the order was, somebody has scratched it out.",
            "Un autre panneau : \"PAR ORDRE DU DR FUNFROCK.\" Quel que soit cet ordre, quelqu'un l'a gratté.",
            "Noch ein Schild: \"AUF BEFEHL VON DR. FUNFROCK.\" Was auch immer befohlen war, jemand hat es weggekratzt.",
            "Otro cartel: \"POR ORDEN DEL DR. FUNFROCK.\" Fuera cual fuera la orden, alguien la ha rascado.",
            "Un altro cartello: \"PER ORDINE DEL DOTT. FUNFROCK.\" Qualunque fosse l'ordine, qualcuno l'ha grattato via.",
            "Outro letreiro: \"POR ORDEM DO DR. FUNFROCK.\" Fosse qual fosse a ordem, alguém a raspou.",
        },
        new[]
        {
            "A poster: \"WANTED: TWINSEN. ESCAPED FROM THE CITADEL'S ASYLUM. DANGEROUS.\"",
            "Une affiche : \"RECHERCHÉ : TWINSEN. ÉVADÉ DE L'ASILE DE LA CITADELLE. DANGEREUX.\"",
            "Ein Plakat: \"GESUCHT: TWINSEN. AUS DER ANSTALT DER ZITADELLE ENTFLOHEN. GEFÄHRLICH.\"",
            "Un cartel: \"SE BUSCA: TWINSEN. FUGADO DEL ASILO DE LA CIUDADELA. PELIGROSO.\"",
            "Un manifesto: \"RICERCATO: TWINSEN. EVASO DAL MANICOMIO DELLA CITTADELLA. PERICOLOSO.\"",
            "Um cartaz: \"PROCURA-SE: TWINSEN. FUGIDO DO ASILO DA CIDADELA. PERIGOSO.\"",
        },
        new[]
        {
            "It doesn't look anything like me. ...Well, the ponytail.",
            "Ça ne me ressemble pas du tout. ...Bon, la queue de cheval.",
            "Das sieht mir überhaupt nicht ähnlich. ...Na gut, der Pferdeschwanz.",
            "No se me parece en nada. ...Bueno, la coleta.",
            "Non mi somiglia per niente. ...Be', la coda di cavallo.",
            "Não se parece nada comigo. ...Bem, o rabo de cavalo.",
        },
        new[]
        {
            "Those old signs? I painted them, back when FunFrock was paying. I'd rather not talk about it.",
            "Ces vieux panneaux ? C'est moi qui les ai peints, à l'époque où FunFrock payait. Je préfère ne pas en parler.",
            "Diese alten Schilder? Die habe ich gemalt, damals, als FunFrock noch bezahlt hat. Ich rede lieber nicht darüber.",
            "¿Esos carteles viejos? Los pinté yo, cuando pagaba FunFrock. Prefiero no hablar de ello.",
            "Quei vecchi cartelli? Li ho dipinti io, ai tempi in cui pagava FunFrock. Preferisco non parlarne.",
            "Esses letreiros velhos? Fui eu que os pintei, no tempo em que o FunFrock pagava. Prefiro não falar disso.",
        },
        new[]
        {
            "We never had a cellar. ...Did we? They said 'exactly as it was'.",
            "On n'a jamais eu de cave. ...Si ? Ils ont dit 'exactement comme avant'.",
            "Wir hatten nie einen Keller. ...Oder doch? Sie sagten 'genau wie vorher'.",
            "Nunca tuvimos bodega. ...¿O sí? Dijeron 'exactamente como estaba'.",
            "Non abbiamo mai avuto una cantina. ...O sì? Hanno detto 'esattamente com'era'.",
            "Nunca tivemos uma cave. ...Ou tivemos? Disseram 'exatamente como estava'.",
        },
        new[]
        {
            "A dusty bottle. The label has faded, but the year can still be read: 1994.",
            "Une bouteille poussiéreuse. L'étiquette a pâli, mais on lit encore l'année : 1994.",
            "Eine verstaubte Flasche. Das Etikett ist verblasst, aber die Jahreszahl ist noch zu lesen: 1994.",
            "Una botella polvorienta. La etiqueta está descolorida, pero aún se lee el año: 1994.",
            "Una bottiglia impolverata. L'etichetta è sbiadita, ma l'anno si legge ancora: 1994.",
            "Uma garrafa cheia de pó. O rótulo desbotou, mas ainda se lê o ano: 1994.",
        },
    };

    private static void Lba1Rooms(StoryParts p)
    {
        bool secret = p.Store.SceneExists(CitadelLba1Rooms.SecretRoom), cellar = p.Store.SceneExists(CitadelLba1Rooms.Cellar);
        var doorway = secret && CitadelLba1Rooms.HasDoorway(p.Store.Directory);
        if (!doorway && !cellar) { p.Log.Add("LBA1's secret sewer room and Tavern cellar left out: the build didn't put them in (no LBA1 folder, or not the demo's sewers)"); return; }
        var first = GroupB.AddLines(p.Store.Directory, CitadelTexts, Lba1RoomLines, p.Log, "the LBA1 rooms' (easter eggs)");
        int Say(Lba1RoomLine line) => first + (int)line;
        if (doorway) SecretRoom(p, Say);
        else p.Log.Add("LBA1's secret sewer room left out: no doorway in the sewers' wall (scene 17 isn't the 1996 demo's sewers)");
        if (cellar) TavernCellar(p, Say);
    }

    // a prop of the build's (CitadelLba1Rooms' bodies): the mushroom's entity with that body, standing still, no shadow (Twinsen walks
    // round it: the engine's collisions take any actor with a body), its armour beyond harm
    private static SceneActorModel Prop(int x, int y, int z, int generic, int beta = 0)
    {
        var a = SceneOps.BlankActor(SceneGame.Lba2, x, y, z, entity: CitadelLba1Rooms.PropEntity);
        a.Body = generic; a.Beta = beta; a.Flags = 0x1000; a.Armor = 51; a.LifePoints = 50;
        return a;
    }

    // the game's clover box (scene 13's), taken once: `taken` the game variable
    private static Action<SceneScripts>? CloverBox(StoryParts p, SceneModel model, (int X, int Y, int Z) at, int taken)
    {
        if (Copy(p.Originals, CloverScene, CloverActor) is not { } box) return null;
        (box.X, box.Y, box.Z) = at;
        var index = model.Actors.Count;
        return p.Add(model, box, $@"void comportement_0()
{{
    if (0 < var_game({taken}))
    {{
        suicide();
    }}
    set_comportement(comportement_1);
}}

void comportement_1()
{{
    if ({index} == col_obj(0) || 0 == col())
    {{
        sample({CloverSample});
        inc_clover_box();
        set_var_game({taken}, 1);
        suicide();
    }}
}}
");
    }

    private static void SecretRoom(StoryParts p, Func<Lba1RoomLine, int> say)
    {
        int room = CitadelLba1Rooms.SecretRoom;
        var (rx, ry, rz) = CitadelLba1Rooms.RoomArrival;
        p.Edit(Sewers, $"LBA1's secret room: a loose slab of red-brown bricks in the sewers' back wall (cell {CitadelLba1Rooms.HoleX}, {CitadelLba1Rooms.HoleZ}) from the thief caught on; hit, or Action in aggressive mode, it slides back into scene {room}'s doorway", model =>
        {
            int z = CitadelLba1Rooms.HoleMiddleZ, y = CitadelLba1Rooms.HoleFirst * 256;
            var slab = Prop(CitadelLba1Rooms.HoleX * 512 + 256, y, z, CitadelLba1Rooms.SlabGeneric);
            // (in the way while it stands -- CHECK_OBJ_COL -- and hit by a magic ball or a fist: its armour takes no harm, hit_by() tells)
            slab.Flags = 0x1001;
            // (its slide: a step back every tenth of a second, to out past the wall's back, where it can't be bumped into -- an invisible
            // actor still is. A body with no animation of its own doesn't walk to a point (goto_point turns it and ends), and a pos_point in
            // its track is undone by the frame's move, measured from where it was; so its track keeps the time, a label a step, and its life
            // puts it at each step's point)
            var first = model.TrackPoints.Count;
            for (var k = 1; k <= SlideSteps; k++) model.TrackPoints.Add(new SceneTrackPoint(slab.X + (SlideTo - slab.X) * k / SlideSteps, y, z));
            var open = first + SlideSteps - 1;
            var timing = string.Concat(Enumerable.Range(1, SlideSteps).Select(k => $"label({StepLabel + k});\nwait_nb_dizieme(1);\n"));
            var stepping = string.Concat(Enumerable.Range(1, SlideSteps).Select(k => $@"    if ({StepLabel + k} == l_track())
    {{
        pos_point({first + k - 1});
    }}
"));
            // the doorway's zone, off until the slab has gone: from the doorway out over the landing's first half cell, as wide as the
            // doorway less Twinsen's edges; he lands where he came in in LBA1, facing the room (his place in it added: x up to 760, z to 383)
            model.Zones.Add(new SceneZoneModel
            {
                Type = 0, Num = room, X0 = 0, X1 = 760, Z0 = CitadelLba1Rooms.HoleZ * 512 + 64, Z1 = CitadelLba1Rooms.HoleZ * 512 + 447, Y0 = y, Y1 = y + 1535,
                Info = new[] { rx - 760, ry, rz - 192, 0, SecretDoorSwitch, 0, 0, 0 },
            });
            var index = model.Actors.Count;
            var life = p.Add(model, slab, $@"void comportement_0()
{{
    if (1 < chapter() || {GlovesBack} <= var_game({QThief}))
    {{
        if ({BackOut} > distance(0))
        {{
            pos_point({open});
            invisible(1);
            set_change_cube({SecretDoorSwitch}, 1);
            end_life();
        }}
        else
        {{
            set_comportement(comportement_1);
        }}
    }}
    else
    {{
        end_life();
    }}
}}

void comportement_1()
{{
    if (0 == hit_by() || (1 == action() && {Aggressive} == comportement_hero() && 1200 > distance(0)))
    {{
        sample({SlideSample});
        set_track(label_10);
        set_comportement(comportement_2);
    }}
}}

void comportement_2()
{{
{stepping}    if (11 == l_track())
    {{
        pos_point({open});
        invisible(1);
        set_change_cube({SecretDoorSwitch}, 1);
        end_life();
    }}
}}
");
            return scripts =>
            {
                life(scripts);
                scripts.SetText(index, ScriptKind.Track, $@"label(0);
stop();

label(10);
background(0);
{timing}
label(11);
stop();
");
                // the painter, asked once the room's clover box is taken: the signs were his work (once a visit, then his own line)
                var painter = scripts.GetText(DemoPainterActor, ScriptKind.Life);
                scripts.SetText(DemoPainterActor, ScriptKind.Life, Loose(painter, "if (21 == l_track())\n{", $@"if (21 == l_track() && 0 < var_game({EggSewerClover}) && 0 == var_cube({Once3}))
        {{
            set_var_cube({Once3}, 1);
            message({say(Lba1RoomLine.PainterSigns)});
            set_var_cube(1, 0);
        }}
        else if (21 == l_track())
        {{"));
            };
        });

        p.Edit(room, $"LBA1's secret room: three old signs on its back wall (texts {say(Lba1RoomLine.SignCheckpoint)}-{say(Lba1RoomLine.TwinsenWanted)}) and a clover box (variable {EggSewerClover})", model =>
        {
            var edits = new List<Action<SceneScripts>>();
            foreach (var (z, generic, line) in Signs)
            {
                var talk = $"            message({say(line)});" + (line == Lba1RoomLine.SignWanted ? $"\n            message_obj(0, {say(Lba1RoomLine.TwinsenWanted)});" : "");
                edits.Add(p.Add(model, Prop(SignWallX, RoomFloor, z, generic, SignBeta), Talker($"0 <= chapter()", talk, 800)));
            }
            if (CloverBox(p, model, RoomClover, EggSewerClover) is { } box) edits.Add(box);
            else p.Log.Add($"the secret room's clover box left out: no clover box in scene {CloverScene}");
            return scripts => { foreach (var e in edits) e(scripts); };
        });
    }

    private static void TavernCellar(StoryParts p, Func<Lba1RoomLine, int> say)
    {
        int cellar = CitadelLba1Rooms.Cellar;
        var (hx, hy, hz) = CitadelLba1Rooms.Hatch;
        p.Edit(TavernScene, $"the Tavern's cellar: a hatch behind Tim's bar from chapter 2 on (Action: down into scene {cellar}); Tim asked once the cellar's clover box is taken", model =>
        {
            var hatch = RaceTrackScenes.SpriteProp(hx, hy, hz, RaceTrackScenes.HatchSprite, 9);
            var life = p.Add(model, hatch, $@"void comportement_0()
{{
    if (1 < chapter())
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
    swif (1 == action())
    {{
        if (700 > distance(0) && {Driving} != comportement_hero())
        {{
            change_cube({cellar});
        }}
    }}
}}
");
            return scripts =>
            {
                life(scripts);
                var tim = scripts.GetText(TavernTim, ScriptKind.Life);
                scripts.SetText(TavernTim, ScriptKind.Life, Loose(tim, "else if (0 == var_cube(1))\n{\nif (1 == var_game(51))", $@"else if (0 == var_cube(1) && 1 < chapter() && 0 < var_game({EggCellarClover}) && 0 == var_cube({Once3}))
        {{
            set_var_cube({Once3}, 1);
            message({say(Lba1RoomLine.TimCellar)});
        }}
        else if (0 == var_cube(1))
        {{
            if (1 == var_game(51))"));
            };
        });

        p.Edit(cellar, $"the Tavern's cellar: a dusty bottle (text {say(Lba1RoomLine.Bottle)}) and a clover box (variable {EggCellarClover})", model =>
        {
            var edits = new List<Action<SceneScripts>>
            {
                p.Add(model, Prop(CellarBottle.X, CellarBottle.Y, CellarBottle.Z, CitadelLba1Rooms.BottleGeneric), Talker("0 <= chapter()", $"            message({say(Lba1RoomLine.Bottle)});", 800)),
            };
            if (CloverBox(p, model, CellarClover, EggCellarClover) is { } box) edits.Add(box);
            else p.Log.Add($"the cellar's clover box left out: no clover box in scene {CloverScene}");
            return scripts => { foreach (var e in edits) e(scripts); };
        });
    }
}
