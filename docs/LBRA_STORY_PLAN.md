# Little Big Racing Adventure: the story (plan)

The user, 2026-11-03: "Plan out a story line for our racing adventure. The game needs to progress through all the tracks with story line
parts in-between. The track order that they're already listed in is the order we're to encounter them in-game. We should avoid having to
race the same track twice. Let's add in some new quests, we don't need to worry about the original quests." Then: "Flesh this out more
with actor scripting and detailed story quests."

This is a plan: nothing in it is built yet beyond what each chapter marks **(built)**.

**The walkthrough script** -- every scene, actor and line, with Baldino's radio calls and Zoe as pit crew -- is
[LBRA_STORY_SCRIPT.md](LBRA_STORY_SCRIPT.md) (2026-11-03). Where the two differ, the script is the newer: it reuses the game's own flags
(the story's chapter is the game's `chapter()`), and its marshals are the races' flag marshals.

## Contents

1. The story in one paragraph
2. How every chapter works
3. Shared pieces: variables, items, texts, arrows, the race files, the script templates
4. The chapters, 0 to 14
5. What the build and the engine need
6. Order of work and how each chapter is tested

---

## 1. The story in one paragraph

The Esmers land on Citadel Island and, to say thank you for clearing the storm, open **the Two Worlds Cup**: one race on every island of
Twinsun and Zeelich. Whoever wins the whole Cup gets one wish from the Esmers. Twinsen enters. But a masked driver in a black car is
following the Cup. He never races, yet he's always parked at the finish line, and oil and trouble turn up wherever he's been. Island by
island Twinsen finds out who he is: FunFrock, back from LBA1, the same FunFrock Twinsen chased in his dream on the first night. FunFrock
wants the wish, to bring back the Dark Monk's rule. The Cup ends on Celebration Island, round the risen statue, where the dream comes
true. Twinsen wins and wishes the statue away.

---

## 2. How every chapter works

Every chapter after the first two has the same six beats, and the same actors play them:

1. **Arrival.** `change_cube` puts Twinsen in the island's race start scene, beside his parked car. The Cup's **flag marshal** stands
   by the start line, the same one who starts and finishes every race there (the user, 2026-11-03): on Twinsun's tracks (Citadel 1,
   Citadel 2, the Desert) the School of Magic's assistant, the Temple of Bú's marshal; on every other track the Imperial Hotel's Sup
   (scene 169) in her red bikini, a grid girl. She greets Twinsen once and sets the chapter's quest going.
   - **One marshal, two halves.** In a race the engine draws her (`marshal=`: on her plinth, flags in hand, animated). On foot she is an
     actor of her entity (77 or 317) with her flag-marshal body (body 40), standing on the plinth's top, with the marshal script (3.6):
     she's the one who talks. So that there is only ever one of her, the race file gets a new key, `marshal_drive_only=1`: the engine
     draws hers only while Twinsen drives, and the actor hides itself while he does (`12 == comportement_hero()` -> `invisible(1)`).
2. **Quest.** One short quest of our own on that island, from 5 to 15 minutes, with one or two of the island's people. A holomap arrow
   points at the next step.
3. **Gate.** Until the quest is done, the start line won't start a race (the race file's `gate=`) and says why.
4. **Race.** Beat the track's main opponent (as now: `main=`). A loss: the marshal says "Again?" and the next crossing of the start line
   races again (the engine already reloads a finished race on re-entry).
5. **Clue.** Once Twinsen is out of his car after a win, the marshal or one of the island's people tells what they know about the
   masked driver.
6. **Travel.** The chapter number goes up and `change_cube` takes Twinsen to the next island's start scene. Nobody walks or sails
   through LBA2's open world between chapters, which is what keeps the original quests out of reach.

**One race per track.** Once a chapter is won, its start line is **shut**: it says "Closed for the season" and never starts a race
again in the story. Twinsen can't come back anyway, as there's no way back, but a saved game loaded on that island mustn't race it again.

**The masked driver** (chapters 2 to 11; on Island CX only his empty workshop): a black car parked past the finish line of each track. When Twinsen comes within about
12 cells it starts up and drives off out of sight, and is gone for the rest of the chapter.

**The original game's quests are ignored.** On each island, the original actors whose life scripts would pull Twinsen into LBA2's plot
(give an object, set a plot variable, change cube) get a plain chapter line in their place: "Good luck in the Cup!" Everyone else
stays as they are.

**The specials stay outside the story:** the Temple of Bú (Halloween) and The Long Table (Thanksgiving) remain seasonal Specials,
optional, never needed to finish the Cup. The Temple's line comes out of the story set. The ending's last line points at them.

---

## 3. Shared pieces

### 3.1 Game variables

The story's own variables. 200-209 are in use already; **212-229 and 231-239 are used by no script in the built game and not by the
engine** (checked 2026-11-03 with `scriptgrep2` on the user's LBRA folder, and the engine source for `ListVarGame[...]`). 211 is the
engine's (RACE_GRID_CAR_VAR) and 210 is kept spare.

| Var | Name | Values |
|---|---|---|
| 200 | DAY | built: 1 tired, 2 slept |
| 201 | TOWN_WON | built: 1 won, 2 ticket handed over |
| 202 | RAPH_BEATEN | built: 1 beaten, 2 said |
| 203, 204 | SELLER_BEATEN, SELLER_RACED | built: the lava lake |
| 205 | DREAM | built: Polar's wake steps |
| 206 | WEATHER | built: engine, Citadel's weather |
| 207, 208 | HAS_CAR, RAPH_HERE | built |
| 209 | GATE_SHUT | built: engine, 1 while a gate is shut |
| **212** | **CHAPTER** | 0 the dream, 1 the storm, ... 14 the final, 15 the Cup won |
| **213** | Q_THIEF | ch1: 0, 1 Zoe asked, 2 umbrella seen, 3 thief cornered, 4 gloves back |
| **214** | Q_LUCCAR | ch2: 0, 1 told, 2 asked, 3 wheel found, 4 fitted |
| **215** | Q_PAGES | ch3: 0, 1 asked, 2-4 pages found (1-3), 5 handed in |
| **216** | Q_HAL | ch4: 0, 1 asked, 2 radio found, 3 mechanics told, 4 HAL mended |
| **217** | Q_FEE | ch5: 0, 1 told the fee, 2 paid |
| **218** | Q_STALL | ch6: 0, 1 asked, 2-5 souvenirs found (1-4), 6 handed back |
| **219** | Q_FUEL | ch7: 0, 1 asked, 2 Kurtz asked for the nurse, 3 nurse found, 4 tested |
| **220** | Q_GLOVE | ch9: 0, 1 asked, 2 glove found, 3 given |
| **221** | Q_PASS | ch10: 0, 1 turned away, 2 ring given, 3 let onto the grid |
| **222** | Q_SLABS | ch11: 0, 1 asked, 2-4 slabs carried (1-3), 5 sealed |
| **223** | Q_CXKEY | ch12: 0, 1 asked, 2 traded |
| **224** | Q_EMPEROR | ch13: 0, 1 the Empress met, 2 the door open, 3 the Emperor out |
| **225** | RACE_WON | the chapter's race: 1 won (the engine's `win=`), 0 once the marshal has said so |
| **226** | RACE_RUN | 1 once the chapter's race is over, won or lost (`finish=`) |
| **227** | MASKED | the masked driver: the last chapter he drove off in |
| **228** | PURSE | ch5: 1 once the Esmers' purse is given |
| 229, 231-239 | spare | |

Each quest variable starts at 0 and the marshal's greeting moves it to 1, so "0" always means "not yet arrived".

### 3.2 Items

The inventory is the game's 40 fixed slots; item n's count is game variable n. Since the original quests are gone, their items are free,
and many already have names that fit our quests as they are, with no renaming:

| Slot | The game's name | Our use |
|---|---|---|
| 7 | Driving gloves | ch1 (built: retexted from Baldino's car part) |
| 13 | Ferry Ticket | ch2's prize (built) |
| 20 | Customer's umbrella | ch1: the thief's umbrella, dropped when he's cornered |
| 26 | Portable Radio | ch4: HAL's new voice box |
| 30 | Wizard's Diploma | ch3: the Dean's pass onto the School's track |
| 24 | Travel Token | ch5: the Cup's entry token on Zeelich |
| 32 | Fragment of the Sups | ch5's prize (Otringal) |
| 15 | Can of GazoGem | ch7: the fuel sample; traded in ch12 |
| 31 | Fragment of the Francos | ch7's prize |
| 11 | Wannie's glove | ch9: the miner's lost glove |
| 34 | Fragment of the Wannies | ch9's prize |
| 16 | Dissidents' Ring | ch10: the dissidents' token |
| 33 | Fragment of the Mosquibees | ch10's prize |
| 18 | Ferryman's Song | ch11's prize: the ferryman sails for whoever sings it |
| 35 | Key for the passage to CX | ch12 |
| 10 | Emperor's Sword | ch13: the Emperor's thanks |

The four **Fragments** are the Zeelich half of the Cup: one for each Zeelich win (Otringal, Francos, Wannies, Mosquibees), and in
chapter 13 the four together open the palace's sealed gate.

Each item's found message, name and description are the game's own (TEXT.HQR file 2: n, 100+n, 200+n). Where one says something about
the old plot it is retexted the way the gloves were: the old text kept under an unused id (60000+), the new one appended unvoiced.

### 3.3 Texts

Every new line goes at the end of the island's own dialogue file (TEXT.HQR file 3 + island), in all six languages, appended so it's
unvoiced, as the storm's and the seller's lines are. In this plan each line is given in English with a name, `T_...`; the build gives each
the next free id.

### 3.4 Holomap arrows

Holomap positions 222-254 are no scene's. 222 and 223 are the storm's and the town's start lines (built). The story takes **224 + chapter**
for each chapter's quest arrow, and moves it with `set_holo_pos` as the quest goes on (one arrow per chapter, at its next step).
Each arrow's label is a new text in file 2.

### 3.5 Each chapter's race file

The story set already has one file per track (STORY_TRACK1..16.TXT, from `WriteStorySet`). Each chapter's file gets:

```
gate=212 <N> The Two Worlds Cup hasn't reached this island yet.
shut=212 <N+1> Closed for the season.                (new key, section 5)
gate=<quest var> <done> <the quest's gate line>
win=225 1
finish=226 1
unlock_win=<island id>                               (as now)
```

`gate=` takes "at least", and several gate lines are allowed (`s_gates[]`). The new `shut=` is its opposite: a race doesn't start while
the variable is at least the value.

### 3.6 Script templates

The scripts are LBA2 life scripts as the editor's compiler takes them, in the form RaceTrackStory.cs already writes. Upper-case names
stand for the numbers in 3.1-3.4. `12` is the behaviour Twinsen drives in (`Driving`).

**The marshal (one per chapter, in the start scene).** Greets, points the arrow, talks while the quest runs, and after the race says the
result, gives the prize, moves the chapter on and travels:

```c
void comportement_0()
{
    if (N != var_game(CHAPTER))
    {
        suicide();
    }
    else
    {
        set_comportement(comportement_1);
    }
}

void comportement_1()
{
    // arrival: once
    if (0 == var_game(Q))
    {
        message(T_ARRIVE);
        set_var_game(Q, 1);
        set_holo_pos(ARROW);
    }
    // after the race, on foot
    if (1 == var_game(RACE_RUN) && 12 != comportement_hero())
    {
        set_var_game(RACE_RUN, 0);
        if (1 == var_game(RACE_WON))
        {
            set_var_game(RACE_WON, 0);
            message(T_WON);
            message_obj(WITNESS, T_CLUE);        // the island's witness tells the clue
            found_object(PRIZE);
            set_var_game(PRIZE, 1);
            clr_holo_pos(ARROW);
            set_var_game(CHAPTER, N + 1);
            message(T_TRAVEL);
            change_cube(NEXT_START);
        }
        else
        {
            message(T_AGAIN);
        }
    }
    swif (1 == action())
    {
        if (1500 > distance(0) && 12 != comportement_hero())
        {
            if (DONE > var_game(Q))
            {
                message(T_MARSHAL_WAIT);         // "Not before <the quest>"
            }
            else
            {
                message(T_MARSHAL_READY);        // "The grid is waiting"
            }
        }
    }
}
```

**A person with lines by quest step:**

```c
void comportement_1()
{
    swif (1 == action())
    {
        if (1500 > distance(0) && 12 != comportement_hero())
        {
            if (1 == var_game(Q))
            {
                message(T_ASK);
                set_var_game(Q, 2);
                set_holo_pos(ARROW);               // the arrow moves to the next step
            }
            else
            {
                if (STEP == var_game(Q))
                {
                    message(T_THANKS);
                    set_var_game(Q, STEP + 1);
                }
                else
                {
                    message(T_MEANWHILE);
                }
            }
        }
    }
}
```

**A pickup on a trail.** Pages, souvenirs and slabs are picked up one at a time, in order. Only the next one is there, with the arrow
over it, so a trail leads Twinsen round the island, mostly along the track. Picked up on foot or by driving over it, which makes the
trail a lap of the course before the race:

```c
void comportement_0()
{
    if (N != var_game(CHAPTER) || K < var_game(Q))
    {
        suicide();                                // already picked up
    }
    else
    {
        set_comportement(comportement_1);
    }
}

void comportement_1()
{
    if (K == var_game(Q))
    {
        invisible(0);
        if (700 > distance(0))
        {
            sample(SAMPLE_PICKUP);
            message(T_FOUND_K);
            add_var_game(Q, 1);
            set_holo_pos(ARROW_NEXT);              // moved by the build to the next one's place
            suicide();
        }
    }
    else
    {
        invisible(1);                             // not yet: an earlier one first
    }
}
```

The arrow can't follow the trail by itself (a holomap position is a fixed place), so the build gives each trail **one arrow per pickup**:
224 + chapter for the first, and spare positions 239-254 for the others.

**The masked driver.** One black car per chapter, at the far side of the finish:

```c
void comportement_0()
{
    if (N != var_game(CHAPTER) || N <= var_game(MASKED))
    {
        suicide();
    }
    else
    {
        set_comportement(comportement_1);
    }
}

void comportement_1()
{
    if (6000 > distance(0))
    {
        set_var_game(MASKED, N);
        sample(SAMPLE_ENGINE);
        set_track(1);                              // label 1: drive off along two track points
        set_comportement(comportement_2);
    }
}

void comportement_2()
{
    if (2 == l_track())                           // label 2 at the far point
    {
        invisible(1);
        suicide();
    }
}
```

Its track script: `label(0); stop(); label(1); goto_point(P1); goto_point(P2); label(2); stop();` with P1 and P2 the build's points
along the road away from the start line. The car is an actor of the racer's entity (157) with a body of its own: FunFrock's car (racer
generic 17) recoloured black, made by RaceTrackCharacterCars as the next free racer generic.

**An original actor silenced.** Its life script is replaced by:

```c
void comportement_0()
{
    set_comportement(comportement_1);
}

void comportement_1()
{
    swif (1 == action())
    {
        if (1500 > distance(0) && 12 != comportement_hero())
        {
            message(T_GOOD_LUCK);
        }
    }
}
```

Which actors: those whose life script has `change_cube`, `found_object`, `give_bonus`, `inc_chapter` or a `set_var_game` of a plot
variable (anything but 0-39 counts already given). The build logs each one it silences, island by island.

### 3.7 Travel

| From | To | How | `change_cube` |
|---|---|---|---|
| Polar (dream) | Citadel, Twinsen's bed | Zoe wakes him **(built: `wake=`)** | scene 0 |
| Citadel 1 | Citadel 2 | the lighthouse, the Esmers land, a night's sleep **(built)** | same island |
| Citadel 2 | Desert | the ferry ticket at the harbour **(built: the ferry's script)** | the game's own |
| Desert | Emerald Moon | Baldino's rocket | 229 |
| Emerald Moon | Otringal | the Esmers' ship | 88 |
| Otringal | Celebration 1 | the spaceport, back to Twinsun | 223 |
| Celebration 1 | Francos | the Esmers' ship | 257 |
| Francos | Elevator | the refinery's tanker convoy | 120 |
| Elevator | Wannies | the elevator, down to the undergas | 98 |
| Wannies | Mosquibees | the Wannies' new tunnel | 102 |
| Mosquibees | Volcano | the dissidents' ferry | 130 |
| Volcano | Island CX | the ferryman, with his song | 343 |
| Island CX | Palace | the soldiers' transport | 290 |
| Palace | Celebration 2 | the Dark Monk's teleporter **(built: the window leap)** | 95 |

Each travel is the marshal's travel line, then a fade and `change_cube`. LBA2's ferry and ship films (`play_acf`) are played only
where the game has one that fits, and only with the CD (as the cinematics are now).

The Desert's arrival needs care: the ferry lands at the game's own harbour scene, not the race's start scene. Either the ferry's
script is given the Desert's start scene (67) to change cube to, or the marshal stands at the harbour.

---

## 4. The chapters

Each chapter lists who's in it (**Cast**), the quest step by step, the scripts that aren't just a template, every new line, the race
file's keys, and how it ends.

The characters are copies of the game's own actors where the game has one (found by entity in the scene named, as RaceTrackStory
does), and otherwise a new actor of the character's own body (BODY.HQR number given, the same body its car was made from).
"Beside the road at x %" means the build's `Beside()` at that share of the lap, off the road's verge.

---

### Chapter 0: Polar Island, the dream **(built)**

**Track:** Polar Island, a sprint chasing FunFrock up to Sendell. **Chapter var:** 0 (a new game).

**Cast:** FunFrock in his car (the chase, built); the grobo on the line (built).

**New:** one line at the finish, and the chapter variable.

- The finish's flight, before the white fade: `T_SENDELL` is said over it (the engine's `wake=` already says one line on waking;
  this is a second key, `dream_finish_text=`, said as the flight starts).
- The wake sets CHAPTER to 1.

**Lines:**
- `T_SENDELL` (Sendell, unseen): "Two worlds will race, Twinsen. And at the end of it, he will be waiting."

---

### Chapter 1: Citadel 1, the storm **(built, with a new quest)**

**Track:** Citadel Island's storm track, beat Raph's time. **Chapter var:** 1.

**Already built:** Zoe's rain line, the Weather Wizard, Raph lapping the track, Mr. Paul's gloves gate, Raph's time, the lighthouse and
the spell, the Esmers' thanks, the tired line, the bed.

**What changes:** the gloves are no longer on the attic shelf. The thief has them.

**Cast (new):**

| Who | Made from | Where | Script |
|---|---|---|---|
| The thief | the game's thief (BODY 117), copied | first by the bakery, then behind the harbour warehouse | below |
| The umbrella | item 20, its OBJFIX model as a sprite-less object actor | dropped by the thief | pickup (no trail) |
| Tim the waiter | the game's Tim (BODY 40) | the tavern's door | person, lines by step |

**The quest: the stolen gloves.**

1. Zoe's opening line gains a second line, `T_ZOE_GLOVES`: "Oh, and you left your driving gloves on the seat of your car, under the
   carport. This morning they were gone!" Q_THIEF = 1. (The user, 2026-11-06: not a washing line -- gloves drying outside in the rain
   made no sense; left in the car, and the thief took them from there.) The attic shelf now shows the darts again (the gloves' attic edit is taken out).
2. Tim at the tavern: `T_TIM` "A fellow ran past with gloves in one hand and a customer's umbrella in the other. Towards the
   harbour!" Q_THIEF = 2, arrow to the harbour.
3. The thief, behind the harbour warehouse, runs off when Twinsen comes within 3 cells (he uses the game's own thief anims), round
   the warehouse, and is cornered at the end of the quay: Q_THIEF = 3.
4. Action by him: `T_THIEF` "Fine, fine! Here are your gloves. Nice gloves, too. See you on the town circuit, if they ever build
   one." He drops the umbrella. Found: driving gloves (item 7 = 1), Q_THIEF = 4.
5. The umbrella (optional): back to Tim, `T_TIM_UMBRELLA` "Our customer's umbrella! Have a drink on the house... well, a lemonade.
   You're driving." 10 kashes (`add_gold_pieces(10)`).

**The thief's script:**

```c
void comportement_0()
{
    if (1 != var_game(CHAPTER) || 4 <= var_game(Q_THIEF))
    {
        suicide();
    }
    else
    {
        set_comportement(comportement_1);
    }
}

void comportement_1()
{
    // waiting behind the warehouse: Twinsen comes close, he runs to the quay's end
    if (2 <= var_game(Q_THIEF) && 1500 > distance(0))
    {
        message(T_THIEF_RUN);                 // "Can't catch me!"
        set_track(1);                         // run: goto_point(quay corner), goto_point(quay end)
        set_comportement(comportement_2);
    }
}

void comportement_2()
{
    if (2 == l_track())                       // at the quay's end: cornered
    {
        set_var_game(Q_THIEF, 3);
        set_comportement(comportement_3);
    }
}

void comportement_3()
{
    swif (1 == action())
    {
        if (1500 > distance(0))
        {
            message(T_THIEF);
            found_object(7);
            set_var_game(7, 1);
            set_var_game(Q_THIEF, 4);
            // the umbrella drops beside him: actor UMBRELLA made visible
            set_comportement_obj(UMBRELLA, comportement_1);
        }
    }
}
```

**Lines:**
- `T_ZOE_GLOVES` (Zoe): as above.
- `T_TIM` (Tim): as above.
- `T_THIEF_RUN` (the thief): "Can't catch me!"
- `T_THIEF` (the thief): as above.
- `T_TIM_UMBRELLA` (Tim): as above.

**Race file:** unchanged (the gloves gate is item 7, as now), plus `gate=212 1 ...`.

**Afterwards (built):** Raph to the lighthouse, the spell, the Esmers land. The Esmer's thanks gains: "...and tomorrow we open a Cup for
both of our worlds." The bed sets DAY = 2, and also CHAPTER = 2.

**A side detail (built 2026-11-05): the secret tavern.** The Tralu's cave had dropped out of the story (Raph is at the race track, and
there's no travel in chapter 1), so: the mayor has banned drink while the island has a race track, and the Tavern's regulars drink in
secret in the cave (scene 2). Tim on the doorstep (asked again) and the mushroom man by the Wizard's tent hint at it; in the cave's first
hall, Luc behind a bar of crates, the drunk rabbibunny, Pat and Fab drumming, the visitor from the Tavern, and the Tralu -- the game's
own, no longer the boss -- grumbling in the corner that his quiet cave has got too noisy. In chapter 2 they are back in the rebuilt
Tavern and the Tralu is glad of the quiet. (`RaceTrackStory.TraluTavern.cs`; the lines are in the walkthrough.)
Moved 2026-11-06 (the user: "somebody sitting down in mid-air ... move the bar scene area to scene 19 ... a secret easter egg"): the
bar is the Back Room, the caves' second hall (scene 19) made bigger with a massive old table and barrels for round tables, everyone
sitting on its benches; the hints say only that Luc is nowhere to be seen; Tim serves there from the umbrella back until Twinsen is
tired; the Tralu stays in his own hall (scene 2), grumbling at the noise next door.

---

### Chapter 2: Citadel 2, the town circuit

**Track:** the Esmers' town circuit, beat Mr. Paul. **Chapter var:** 2.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the School of Magic's assistant (entity 77, body 40) | on her plinth by the town circuit's start line | marshal |
| Zoe | the game's Zoe, copied | in the yard north of the house (where the start cars park), fixing Luc's car | person |
| Luc | the Tavern's boss, beside his barrel car (the character car) | the yard | person |
| The spare wheel | a body of its own: one wheel of the racer's car, as a static object | the harbour's scrap heap | pickup |
| Mr. Paul | built (prize) | the start line | built |
| The masked driver | the black car | past the finish line | masked |

**The quest: Luc's car.** (Zoe is never on the grid in story mode, with one player or two: she's the pit crew. The script,
[LBRA_STORY_SCRIPT.md](LBRA_STORY_SCRIPT.md), has the lines.)

1. Morning (after the bed). The marshal: `T_CUP` "Welcome to the Two Worlds Cup! One race on every island of Twinsun and Zeelich,
   and one wish for whoever wins them all. The first race is here, on Citadel." Q_LUCCAR = 1, arrow to the yard.
2. Luc, beside his barrel car, Zoe under it: `T_LUC_CAR` "They gave me my Tavern back, so I'm racing for the Tavern! Only... my back
   wheel came off on the way here." Q_LUCCAR = 2, arrow to the harbour.
3. The wheel at the harbour: picked up, `T_WHEEL` "A wheel. A bit rusty, but round." Q_LUCCAR = 3. (Not an inventory item: the
   pickup just sets the step.)
4. Zoe fits it: `T_ZOE_THANKS` "There. Good as new. Well, good as old." Q_LUCCAR = 4.

Until then, the gate says `T_GATE_LUC` "Mr. Paul: six cars on the grid or no race." (Luc's car is one of the six: Raph, Luc, Mr. Paul,
the Tralu, the thief and Twinsen.)

**Race file:** `gate=212 2`, `gate=214 4 Mr. Paul: six cars on the grid or no race.`, the town's own keys (built: 3 laps,
`win=201 1`), plus `win=225 1`, `finish=226 1`, `shut=212 3`.

**After the race:** Mr. Paul's ferry ticket (built). Then the marshal:
- `T_WON_2` "Twinsen of Citadel Island leads the Cup! Next stop: the Desert. Mrs. Brune's ferry is waiting."
- The clue (Raph, `message_obj`): `T_CLUE_2` "Did you see that black car past the finish? No number, no driver I could see. And
  gone before I'd blinked."
- CHAPTER = 3. No `change_cube`: Twinsen takes the ferry with his ticket (built), whose crossing lands him on the Desert.

**Lines:**
- `T_CUP`, `T_LUC_CAR`, `T_WHEEL`, `T_ZOE_THANKS`, `T_GATE_LUC`, `T_WON_2`, `T_CLUE_2`: as above.
- `T_AGAIN_2` (the marshal, a loss): "So close! The grid is yours whenever you're ready."

---

### Chapter 3: Desert Island

**Track:** the Desert, beat the racer (the Desert champion). **Chapter var:** 3.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the School of Magic's assistant (entity 77, body 40): the Dean's own assistant, which gives her a line about him | on her plinth by the start line | marshal |
| The Dean of the School of Magic | the game's Dean (BODY 123) | the School's gate, or beside the start line if the School is far | person |
| Three exam pages | a body of its own: a flat sheet (the Polar dream's tart method, a white flat body) | beside the road at 30 %, 55 % and 80 % of the lap | trail pickup |
| Moya the turtle | the game's Moya (BODY 212) | by the last bend | witness |
| The racer | the race's main (built) | the grid | |
| The masked driver | black car | past the finish | masked |

**The quest: the Dean's lost pages.**

1. Arrival (the marshal, `T_ARRIVE_3`): "The Desert champion is waiting for you. But the Dean has shut the track!" Q_PAGES = 1,
   arrow to the Dean.
2. The Dean: `T_DEAN` "Shut it is! Your engines blew my students' exam papers all over the island. Three pages. Bring them back, and
   I'll think about it." Q_PAGES = 2, arrow to page 1.
3. The pages, in order round the track (trail): `T_PAGE_1` "A page: *Chapter 1, Levitation for Beginners*." `T_PAGE_2` "Another
   page: *Turning Sand into Slightly Different Sand*." `T_PAGE_3` "The last page: *Why Wizards Should Not Drive*." Q_PAGES 2 -> 5.
4. Back to the Dean: `T_DEAN_THANKS` "All three! Very well. Here: a diploma. It says you may use the School's grounds, which include
   the track. Don't let it go to your head." Found: Wizard's Diploma (item 30). Q_PAGES = 6.

**The gate:** `gate=30 1 The Dean: no diploma, no race.`

**Race file:** `gate=212 3`, `gate=30 1 ...`, `win=225 1`, `finish=226 1`, `shut=212 4`, `unlock_win=desert`.

**After the race:**
- The marshal: `T_WON_3` "The Desert champion beaten! The Cup goes on, to the Emerald Moon."
- The clue (Moya, `message_obj`): `T_CLUE_3` "There was oil on the last bend this morning. That wasn't there yesterday. A black car
  came through at night, slow, like it was looking for something."
- Baldino, who raced: `T_BALDINO` "You think you're fast? On the moon I've got a lander that would leave your car standing. Come and
  race me up there. My rocket leaves now."
- CHAPTER = 4, `change_cube(229)`.

**Lines:** `T_ARRIVE_3`, `T_DEAN`, `T_PAGE_1..3`, `T_DEAN_THANKS`, `T_WON_3`, `T_CLUE_3`, `T_BALDINO`, `T_AGAIN_3`.

**Check first:** where the School of Magic's people are kept. The Desert keeps its off-road actors (KeepOffRoadActors), so the Dean may
already stand in an outside scene. Otherwise he's added beside the start line.

---

### Chapter 4: the Emerald Moon

**Track:** the Emerald Moon, beat Baldino in his lander. **Chapter var:** 4.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the Sup in her bikini (entity 317, body 40) | on the edge of the deck by the start line (the moon's start is raised: no plinth) | marshal |
| HAL | the game's HAL: scene 23 actor 4, a sprite actor (228/229 working, 230 cracked) | where it is, in the moon base | below |
| HAL's mechanics | the game's: scene 23 actors 12-15, entity 57 (BODY 97/98) | hiding in the base's store room | person |
| The radio | item 26, Portable Radio | the base's stores | pickup |
| Baldino | the race's main (built) | the grid | |
| The masked driver | black car | past the finish | masked |

**The quest: mend HAL.**

1. Arrival (the marshal, `T_ARRIVE_4`): "Welcome to the moon. Bad news: the base's computer has locked the track's start gate, and
   nobody can get it to talk sense." Q_HAL = 1, arrow to the base.
2. HAL (cracked sprite 230): `T_HAL_BROKEN` "HAL!! ZX81!! HAL!!" (the game's own 546/545 if they suit). Then the mechanics, hiding:
   `T_MECH` "It shouted at us all night. Its voice box is broken. There was a portable radio in the stores; a radio would do."
   Q_HAL = 2, arrow to the stores.
3. The radio in the stores: found, item 26. Q_HAL = 3.
4. Back to the mechanics: `T_MECH_FIX` "Leave it to us." They walk to HAL (the game's own walk to it, their track script), HAL's
   sprite becomes 228, Q_HAL = 4.
5. HAL: `T_HAL_MENDED` "Thank you, Twinsen. The gate is open. I have calculated your chances against Baldino: they are poor. I will
   race too, to see if I am right."

**HAL's script (scene 23 actor 4, replacing its own):**

```c
void comportement_0()
{
    if (4 != var_game(CHAPTER))
    {
        set_sprite(229);
        suicide();                    // (or keep it, working, as the game had it)
    }
    else
    {
        set_comportement(comportement_1);
    }
}

void comportement_1()
{
    if (4 > var_game(Q_HAL))
    {
        set_sprite(230);              // cracked
    }
    else
    {
        set_sprite(228);
    }
    swif (1 == action())
    {
        if (2000 > distance(0))
        {
            if (4 > var_game(Q_HAL))
            {
                message(T_HAL_BROKEN);
            }
            else
            {
                message(T_HAL_MENDED);
            }
        }
    }
}
```

**The gate:** `gate=216 4 The gate is locked: HAL won't open it.`

**Race file:** `gate=212 4`, `gate=216 4 ...`, `win=225 1`, `finish=226 1`, `shut=212 5`, `unlock_win=moon`.

**After the race:**
- The marshal: `T_WON_4` "Two worlds, two races... and now Zeelich! Our ship leaves for Otringal."
- The clue (HAL): `T_CLUE_4` "My log shows a launch from this moon last night: one black car, one passenger, to Zeelich. The
  passenger's name was not given. His laugh was on file."
- CHAPTER = 5, `change_cube(88)`.

**Check first:** the moon base (scene 23) must be reachable from the race's copies, scenes 229-232. If its door zone was lost in the
copy, the build adds one.

---

### Chapter 5: Otringal

**Track:** Otringal, through the tunnel, the town and round the palace; beat the racer. **Chapter var:** 5.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the Sup in her bikini (entity 317, body 40) | on her plinth by the start line | marshal, plus the purse |
| The race steward | the casino's bouncer (BODY 419) | beside the start line | person |
| The casino's slot machines | the game's (scene 135: three machines, actors 24/42/49) | the casino | the game's own |
| Johnny Rocket | the game's Johnny Rocket (BODY 154) | by the spaceport | witness, travel |
| A Dark Monk priest | the Sup priest (BODY 292) | in the crowd by the palace bend | watches, says one line |
| The masked driver | black car | past the finish | masked |

**The quest: the entry fee.**

1. Arrival (the marshal, `T_ARRIVE_5`): "Zeelich! Here the Cup is run by the Emperor's stewards, and they want an entry fee. The Esmers
   left you a little Zeelich money to start you off." `add_gold_pieces(10)`, PURSE = 1, Q_FEE = 1, arrow to the steward.
2. The steward: `T_STEWARD` "A Travel Token for the Cup costs 30 zlitos. Twenty-five is a nice number too, but it's not 30." With
   30 or more on Twinsen: an `ask_choice` "Pay 30 zlitos?" Yes: `give_gold_pieces(30)`, found Travel Token (item 24), Q_FEE = 2.
3. Without the money: the steward: `T_STEWARD_CASINO` "Short? There's a casino round the corner. Some people leave it richer."
   Arrow to the casino. The casino's three machines are the game's own (1 zlito a pull; three money symbols pay 25). Twinsen comes
   back when he has 30.

**The steward's talk:**

```c
swif (1 == action())
{
    if (1500 > distance(0) && 12 != comportement_hero())
    {
        if (2 <= var_game(Q_FEE))
        {
            message(T_STEWARD_READY);
        }
        else
        {
            if (30 <= nb_gold_pieces())
            {
                add_choice(T_YES);
                add_choice(T_NO);
                ask_choice(T_STEWARD_PAY);
                if (T_YES == choice())
                {
                    give_gold_pieces(30);
                    found_object(24);
                    set_var_game(24, 1);
                    set_var_game(Q_FEE, 2);
                    clr_holo_pos(ARROW);
                }
            }
            else
            {
                message(T_STEWARD_CASINO);
                set_holo_pos(CASINO_ARROW);
            }
        }
    }
}
```

**The gate:** `gate=24 1 The steward: no Travel Token, no race.`

**Race file:** `gate=212 5`, `gate=24 1 ...`, `win=225 1`, `finish=226 1`, `shut=212 6`, `unlock_win=otringal`. The prize wheel and
the golden Gazogem are built.

**After the race:**
- The marshal: `T_WON_5` "A win on Zeelich! The stewards give you this: the Fragment of the Sups. Win the Fragments of all Zeelich's
  peoples, they say, and the Emperor's own gate will open to you." Found: Fragment of the Sups (item 32).
- The priest in the crowd, as Twinsen passes on foot: `T_PRIEST` "Enjoy it while you can, little racer. The Cup's last race is
  already won."
- The clue (Johnny Rocket): `T_CLUE_5` "A black car? Came off the moon ship last night and took the first shuttle to Twinsun. The
  souvenir seller on Celebration Island sells pictures of every car he's seen. Ask him." Then `T_JOHNNY` "I'll fly you there
  myself, if you like."
- CHAPTER = 6, `change_cube(223)`.

**Check first:** the casino (scene 135) is an inside scene of the game's Otringal. It must be reachable from the race's Otringal (its
door). The forced key after 20 pulls is harmless: the key opens nothing we use.

---

### Chapter 6: Celebration 1, the lava lake **(the seller's race is built)**

**Track:** Celebration Island's lava lake, beat the souvenir seller. **Chapter var:** 6.

**Already built:** the seller (scene 95 actor 5, entity 213) challenges Twinsen to the race (scene 223) until he's beaten, and tells
what he saw after a win (`ApplyCelebration`). His lines are rewritten (below), and the quest goes before his challenge.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the Sup in her bikini (entity 317, body 40) | by the race's start line (scene 223) | marshal |
| The souvenir seller | built | his stall, scene 95 | built, rewritten |
| Four souvenirs | a body of its own: a small statuette of the Dark Monk, a statue souvenir | round the island, beside the road at 20/45/65/85 % | trail pickup |
| The kangaroo | the game's kangaroo tourist (BODY 308) | by the dock | witness |
| The masked driver | black car | past the finish | masked |

**The quest: the seller's stall.**

1. Arrival (the marshal, `T_ARRIVE_6`): "Celebration Island. The seller here won't even race until his stall is put right. You'll
   see why." Q_STALL = 1.
2. The seller: `T_SELLER_STALL` "Look at my stall! That kangaroo bounced into it and my souvenirs went everywhere. Four of them.
   Find them, and maybe I'll race you. Maybe." Q_STALL = 2, arrow to the first.
3. The four statuettes, in order (trail): `T_SOUVENIR_K` "A little statue of the Dark Monk. Ugly, but someone will buy it."
   Q_STALL 2 -> 6.
4. The seller: `T_SELLER_BACK` "All four! Now, about that race: beat me, and I'll tell you what I saw on Island CX." This is his
   built challenge, which now waits for Q_STALL = 6 (his `if (0 == var_game(SELLER_BEATEN))` gains `&& 6 <= var_game(Q_STALL)`;
   before that he says `T_SELLER_STALL`).
5. The kangaroo, any time: `T_KANGAROO` "Boing! Sorry about the stall. Boing!"

**Race file:** `gate=212 6`, `gate=218 6 The seller: my stall first!`, the built keys (`win=203 1`, `finish=204 1`) plus
`win=225 1`, `finish=226 1`, `shut=212 7`, `unlock_win=celebration`.

**After the race:** the built after-race controller (scene 223) says the information, rewritten:
- `T_SELLER_CLUE` "I said I'd tell you. On Island CX I saw Franco engineers building a car. Black, no number. The driver wore a mask,
  but I know a pair of glasses when I see one. And it ran on gazogem, the kind from the Francos' refinery."
- Then the marshal (instead of `change_cube(95)`): `T_WON_6` "The Francos' refinery? Then that's our next stop anyway. The ship
  leaves now." CHAPTER = 7, `change_cube(257)`.

The game's Rick arrow (`set_holo_pos(136)`, var 124) is taken out of the seller's lines: it pointed into the old plot.

---

### Chapter 7: the Island of the Francos

**Track:** the Island of the Francos, over the dock, the refinery and the village; beat De La Fontaine. **Chapter var:** 7.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the Sup in her bikini (entity 317, body 40) | on her plinth by the start line | marshal |
| De La Fontaine | the game's (BODY 450) | by the refinery's gate | person, then witness |
| Mr. Kurtz | the game's (BODY 446) | his laboratory, or beside the road by the village if the lab is far | person |
| The Franco nurse | the game's (BODY 447), with her pram | the village, beside the road at 60 % | person |
| The oil slick | a slick (the race's oil body, RaceTrackOil) | the dock, by the start | pickup: a Can of GazoGem |
| The masked driver | black car | past the finish | masked |

**The quest: the fuel sample.**

1. Arrival (the marshal, `T_ARRIVE_7`): "The Francos' race is De La Fontaine's, and so is the refinery. Ask him about that fuel, if
   you dare." Q_FUEL = 1.
2. De La Fontaine: `T_DLF` "Who buys my gazogem is my business. Race me, if you want to talk." He won't say more. Then the slick on
   the dock catches Twinsen's eye (the arrow): a fresh one, from the black car.
3. The slick: Twinsen scoops some up: found Can of GazoGem (item 15), `T_SAMPLE` "Fresh oil, still warm. A sample for someone who
   knows what it is." Arrow to Kurtz.
4. Mr. Kurtz: `T_KURTZ` "A sample? Splendid! But my nurse has my glasses, and she's out with the pram. Bring her here and I'll tell
   you everything that's in it." Q_FUEL = 2, arrow to the nurse.
5. The nurse: `T_NURSE` "His glasses? On his head, as always. I'll come and show him." She walks back with Twinsen (MOVE_FOLLOW,
   the game's follow behaviour) to Kurtz. Q_FUEL = 3.
6. Kurtz: `T_KURTZ_TEST` "On my head! Now then... this is the refinery's own gazogem, the best grade, sold only by the tanker-load.
   Keep the can; it's evidence." Q_FUEL = 4.
7. De La Fontaine (now): `T_DLF_CAUGHT` "...So you know. Beat me on the track and I'll tell you who I sold it to."

**The nurse's script (her walk with Twinsen):**

```c
void comportement_1()
{
    swif (1 == action())
    {
        if (1500 > distance(0) && 12 != comportement_hero() && 2 == var_game(Q_FUEL))
        {
            message(T_NURSE);
            set_dir(MOVE_FOLLOW, 0);        // follows Twinsen (actor 0)
            set_comportement(comportement_2);
        }
    }
}

void comportement_2()
{
    if (2000 > distance(KURTZ))           // beside Mr. Kurtz
    {
        set_dir(NO_MOVE);
        set_var_game(Q_FUEL, 3);
        message_obj(KURTZ, T_KURTZ_TEST);
        set_var_game(Q_FUEL, 4);
        set_comportement(comportement_3);
    }
}
```

(Kurtz and the nurse must be in the same scene for `distance(KURTZ)`. If they aren't, the nurse disappears when Twinsen leaves her
scene, and is placed beside Kurtz with Q_FUEL = 3.)

**The gate:** `gate=219 4 De La Fontaine: what's your business here?`

**Race file:** `gate=212 7`, `gate=219 4 ...`, `win=225 1`, `finish=226 1`, `shut=212 8`, `unlock_win=francos`.

**After the race:**
- The marshal: `T_WON_7` "The Fragment of the Francos is yours!" Found: item 31.
- The clue (De La Fontaine): `T_CLUE_7` "A masked man paid for a whole tanker. Cash, Dark Monk coins. I sent it down the Elevator
  Platform to the undergas, where he said he'd collect it. Take the next convoy down and see for yourself."
- CHAPTER = 8, `change_cube(120)`.

---

### Chapter 8: the Elevator Platform (head-to-head)

**Track:** the Elevator Platform's rollercoaster, Twinsen against the monkey monster alone (StoryHeadToHead, built). **Chapter var:** 8.

**No quest**, as the user asked for this track: just Twinsen and the monster.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the Sup in her bikini (entity 317, body 40) | on the edge of the deck by the start line (all raised: no plinth) | marshal, no quest |
| The monkey monster | the race's (BODY 388, in his crate) | the grid | |
| The Franco policeman | the game's (BODY 86) | by the elevator | witness |
| The masked driver | black car | past the finish | masked |

**Arrival:** the marshal, `T_ARRIVE_8`: "The monster won't let anybody down to the undergas without a race. It's just you and him."
The monster himself, from his crate, as Twinsen comes near: `T_MONSTER` "RAAARGH! Race! Now!" The quest var is skipped: the marshal
greets with no arrow, and nothing gates the race but the chapter.

**Race file:** `gate=212 8`, `win=225 1`, `finish=226 1`, `shut=212 9`, `unlock_win=elevator`.

**After the race:**
- The marshal: `T_WON_8` "The elevator is yours. Down we go!"
- The clue (the policeman): `T_CLUE_8` "A black car went down before you. Didn't race the monster either. The monster's still
  sulking about it."
- CHAPTER = 9, `change_cube(98)`.

---

### Chapter 9: Wannies Island

**Track:** Wannies Island, through the frog and down the tower; beat the Wannie miner. **Chapter var:** 9.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the Sup in her bikini (entity 317, body 40) | on her plinth by the start line | marshal |
| The Wannie miner | the game's miner (BODY 342) | by his bulldozer, beside the start line | person |
| The old Wannie | the game's (BODY 386) | near the frog | person, witness |
| The glove | item 11, Wannie's glove | beside the road at the frog's mouth (the race's timed frog) | pickup |
| The masked driver | black car | past the finish | masked |

**The quest: the miner's glove.** (Chapter 1's gag the other way round.)

1. Arrival (the marshal, `T_ARRIVE_9`): "The Wannies' race is off. Their best driver won't drive." Q_GLOVE = 1.
2. The miner: `T_MINER` "No glove, no driving. Safety first! They say that on Citadel too, I hear. I lost it by the frog. The frog
   keeps snapping at me." Q_GLOVE = 2, arrow to the old Wannie.
3. The old Wannie: `T_OLD_WANNIE` "The frog opens its mouth every few seconds. Your glove's just inside. Be quick, dear." Arrow to
   the glove.
4. The glove, just inside the frog's mouth, is reachable only while the mouth is open (the race's own timer, RACEMOD frog=). Found
   Wannie's glove (item 11). If the mouth shuts with Twinsen inside, he's pushed back out (the frog's bounce, built).
5. Back to the miner: `T_MINER_THANKS` "My glove! Right, you. To the grid." The glove goes back to him (item 11 = 0), Q_GLOVE = 3.

**The gate:** `gate=220 3 The Wannie miner: no glove, no driving.`

**Race file:** `gate=212 9`, `gate=220 3 ...`, `win=225 1`, `finish=226 1`, `shut=212 10`, `unlock_win=wannies`.

**After the race:**
- The marshal: `T_WON_9` "The Fragment of the Wannies!" Found: item 34.
- The clue (the old Wannie): `T_CLUE_9` "The black car's driver paid us to dig him a tunnel to Mosquibees Island. Paid in coins with
  the Dark Monk's face on them. The tunnel's finished: you may as well use it."
- CHAPTER = 10, `change_cube(102)`.

**Check first:** the frog's mouth on foot. The frog is drawn and timed by the race mode (frog=). Is it running while Twinsen is on foot
outside a race? If not, the glove goes beside the frog instead.

---

### Chapter 10: Mosquibees Island

**Track:** Mosquibees Island's mountain lap, beat the Queen. **Chapter var:** 10.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the Sup in her bikini (entity 317, body 40) | on her plinth by the start line | marshal |
| The Queen's ventilation guy | the game's (BODY 197) | at the grid's gate | person (gatekeeper) |
| A Mosquibee of the hide-out | BODY 193 | hiding beside the road at 40 % | person (the dissident's contact) |
| The honey-pot Mosquibee | the game's (BODY 192) | by the start | gossip |
| The masked driver | black car | past the finish | masked |

**The quest: the Queen's pass.**

1. Arrival (the marshal, `T_ARRIVE_10`): "The Queen races only those she's allowed onto her island. We're... not sure you are."
   Q_PASS = 1.
2. The ventilation guy, at the grid: `T_FAN_NO` "No pass, no grid. The Queen's orders." Then, quietly: `T_FAN_HINT` "...unless
   you know the right people. Try down the road."
3. The honey-pot Mosquibee (optional): `T_HONEY` "The Queen's never lost a race on her own island. Never!"
4. The hide-out's Mosquibee, hiding: `T_REBEL_ASK` "You're the racer who's after the black car? So are we. Take this ring and show it
   at the grid: our friend there will let you through. And after the race, come and find me. We've got a message for you." Found:
   Dissidents' Ring (item 16), Q_PASS = 2.
5. The ventilation guy again: `T_FAN_YES` "Ah. That ring. Off you go, then. I never saw it." Q_PASS = 3.

**The gate:** `gate=221 3 The ventilation guy: no pass, no grid.`

**Race file:** `gate=212 10`, `gate=221 3 ...`, `win=225 1`, `finish=226 1`, `shut=212 11`, `unlock_win=mosquibees`.

**After the race:**
- The marshal: `T_WON_10` "You beat the Queen! The Fragment of the Mosquibees!" Found: item 33.
- The Queen (the race's main, `message_obj` if she stands in the scene; otherwise the marshal reads it): `T_QUEEN` "Hmph."
- The clue (the hide-out's Mosquibee, who has come to the start line): `T_CLUE_10` "Here's our message: a map. The black car's
  workshop is on Island CX, but its driver hides under Celebration Island, in the volcano. Our hide-out is there. We'll take you."
- CHAPTER = 11, `change_cube(130)`.

---

### Chapter 11: Volcano Island

**Track:** Volcano Island, over the lava; beat the dissident. **Chapter var:** 11.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the Sup in her bikini (entity 317, body 40) | on her plinth by the start line | marshal |
| The dissident | the game's (BODY 270) | by the hide-out, beside the start | person, then witness |
| The hide-out's Mosquibee | BODY 193 | at the hide-out's cracks | person |
| Three lava slabs | the lava slab car's slab (BODY of LavaSlab, as a static object) | beside the road at 25/50/75 % | trail pickup |
| The ferryman | the game's (BODY 334) | at his boat, by the lava's edge | person, travel |
| The masked driver | black car | past the finish | masked |

The race's own lava balls (lavaball=) are falling the whole time: the hide-out is under attack, which is why it needs mending.

**The quest: seal the hide-out.**

1. Arrival (the marshal, `T_ARRIVE_11`): "The dissidents' hide-out is cracking under the lava. Nobody's racing until it holds."
   Q_SLABS = 1.
2. The dissident: `T_DISSIDENT` "Help us seal the cracks, and I'll race you. I'll even tell you who's under the mask." Arrow to the
   hide-out's Mosquibee.
3. The hide-out's Mosquibee: `T_HIDEOUT` "Cold slabs of lava rock, three of them. They've cooled by the road." Q_SLABS = 2, arrow to
   slab 1.
4. The slabs, in order (trail): `T_SLAB` "A slab of cooled lava. Heavy!" Q_SLABS 2 -> 5. Each one carried: Twinsen can't drive
   while carrying one (his behaviour is the game's carrying one, if a slab can be carried; otherwise each slab is "taken back" as
   it's picked up).
5. The Mosquibee: `T_SEALED` "That'll hold!" Q_SLABS = 6.

**The gate:** `gate=222 6 The dissident: the hide-out first!`

**Race file:** `gate=212 11`, `gate=222 6 ...`, `win=225 1`, `finish=226 1`, `shut=212 12`, `unlock_win=volcano`.

**After the race:**
- The marshal: `T_WON_11` "Well raced! The Cup goes on to Island CX."
- The clue (the dissident): `T_CLUE_11` "I promised. I've seen him without his mask: FunFrock. Yes, *that* FunFrock. He builds his
  cars on Island CX and buys his soldiers from the Emperor's palace. He wants the Cup's wish, to bring back the Dark Monk."
- The ferryman: `T_FERRYMAN` "Island CX? I'll take you, for a song. *The* song. Here, I'll teach you." Found: Ferryman's Song
  (item 18). `T_FERRY_GO` "...beautiful. Well, nearly. In you get."
- CHAPTER = 12, `change_cube(343)`.

---

### Chapter 12: Island CX

**Track:** Island CX, round the island under its cannons, down through a door and twice round the halls; beat the Emperor's soldier.
**Chapter var:** 12.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the Sup in her bikini (entity 317, body 40) | on her plinth by the start line | marshal |
| The Franco survivor | the game's (BODY 90), on his raft | at the shore by the start | person (the trade) |
| A Franco trooper | BODY 290 | by the cannons | gossip |
| The door to the halls | the track's own door (built: the race goes through it) | | opened by the gate |
| The workshop's note | a body of its own: a sheet, as the exam pages | the halls, beside the road at 70 % | pickup (after the race) |
| The masked driver | black car | none here: his workshop is empty | |

**The quest: the key for the passage.**

1. Arrival (the marshal, `T_ARRIVE_12`): "Island CX. The race goes down through that door into the halls, and the door is locked.
   The soldiers lost the key." Q_CXKEY = 1.
2. The trooper: `T_TROOPER` "Lost it? The survivor on the raft has it. Won't give it back, either. Says his motor's out of fuel."
3. The survivor: `T_SURVIVOR` "The Key for the passage? Mine. Fair and square. But if you had some fuel for my raft..." With the
   Can of GazoGem (item 15, from chapter 7): `T_SURVIVOR_TRADE` "Gazogem! The good stuff! Here, take the key." Item 15 = 0, found
   Key for the passage to CX (item 35), Q_CXKEY = 2.

**The gate:** `gate=35 1 The door to the halls is locked.`

**Race file:** `gate=212 12`, `gate=35 1 ...`, `win=225 1`, `finish=226 1`, `shut=212 13`, `unlock_win=cx`.

**After the race:**
- The marshal: `T_WON_12` "Island CX is yours!"
- The clue (the note, now in the empty workshop in the halls; the arrow points at it): `T_NOTE` *"To the palace. The Emperor will
  lend me his teleporter, or he will lose his throne. F."*
- The marshal: `T_TRAVEL_12` "The palace! The soldiers' transport leaves now, and we're on it."
- CHAPTER = 13, `change_cube(290)` (once the note is read: the marshal's travel waits for the note's own variable).

---

### Chapter 13: the Palace **(the sprint out of the window is built)**

**Track:** the Emperor's palace: through its rooms, upstairs past the throne, out of the round window; beat Stan. **Chapter var:** 13.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the Sup in her bikini (entity 317, body 40): a Sup at home in the palace | by the start (the south-east room) | marshal |
| The sealed gate | the palace's own door at the start room | | opened by the four Fragments |
| The Empress | the game's Emperor's wife (BODY 454) | the music room | person |
| The Emperor | the game's Emperor (EmperorBody) | locked in his bedroom | person |
| Stan | the game's Stan (BODY 439) | the library | person |
| The pighead guards | built (guard= lines) | the rooms | built |

**The quest: the Emperor's escape.**

1. Arrival (the marshal, `T_ARRIVE_13`): "FunFrock has taken the palace! The gate's sealed with the Emperor's seal..." The four
   Fragments (items 31-34) open it: `T_FRAGMENTS` "...and it knows the Fragments of the four peoples when it sees them." The gate's
   door opens (`set_door_*` on it). Q_EMPEROR = 1.
2. The Empress: `T_EMPRESS` "My husband! FunFrock's men have locked him in his bedroom. The lock answers to a song, but I can't
   sing." Twinsen has the Ferryman's Song (item 18): `T_SING` "Twinsen sings the ferryman's song. It's... not good. The door opens
   anyway." Q_EMPEROR = 2.
3. The Emperor: `T_EMPEROR` "Twinsen of Twinsun! FunFrock has my teleporter, past the round window at the end of the long hall. If
   we get there first, it's ours. Take this: you've earned it." Found: Emperor's Sword (item 10). Q_EMPEROR = 3.
4. Stan, in the library: `T_STAN` "The window? End of the long hall. I've timed that run from this desk more times than I've read these books. Race me there."

**The gate:** `gate=224 3 The Emperor first!`

**Race file:** the palace's own (built: a sprint, `win=79 1`, the window smash, the teleporter's `wake=`), plus `gate=212 13`,
`gate=224 3 ...`, `win=225 1`, `finish=226 1`, `shut=212 14`.

**After the race (built):** the window shatters; the teleporter takes the car in mid-air; the statue rises (var 79 = 1); Twinsen lands
on Celebration Island. The new part, said in the flight, before the landing:
- `T_TELEPORT` (FunFrock's voice): "Very good, Twinsen. Now come and finish it. I'll be waiting, just like in your dream."
- CHAPTER = 14 (set by the teleport's `wake=` scene).

---

### Chapter 14: Celebration 2, round the statue (the final)

**Track:** Celebration Island round the statue, a spiral to its shoulders, a ring round its head, the bridge back down. Beat FunFrock.
**Chapter var:** 14.

**No quest.** The final.

**Cast:**

| Who | Made from | Where | Script |
|---|---|---|---|
| The flag marshal | the Sup in her bikini (entity 317, body 40) | the dock, by the start | marshal (the final) |
| An Esmer | the game's alien (scene 42 actor 6), copied | the dock | hands over the Cup and asks for the wish |
| FunFrock | the game's FunFrock (BODY 313), on foot, unmasked | beside his black car on the grid | person |
| Zoe, Raph, Mr. Paul, Baldino | copies of the game's own | on the dock (the crowd) | one line each |
| The grid | **new line-up:** FunFrock (main, his car recoloured black), the Dark Monk (BODY 311's car), a Sup priest (292's) and the Franco priest (310's) | | |

**Arrival:** the teleporter's landing on the dock (built), then:
- FunFrock, unmasked: `T_FUNFROCK` "We raced once, Twinsen, in your dream. You won, in your dream. This time it's real, and the wish
  is mine."
- The marshal: `T_FINAL` "The last race of the Two Worlds Cup!"
- Zoe (the crowd): `T_ZOE_FINAL` "Go on, Twinsen! For Citadel!"

**Race file:** the statue track's own (built), with its roster changed (RaceDriver.Celebration: FunFrock, Dark Monk, Sup priest, Franco
priest), plus `gate=212 14`, `win=225 1`, `finish=226 1`, `shut=212 15`, `unlock_win=statue`. A loss: FunFrock laughs (`T_FUNFROCK_LAUGH`
"Again? I have all day.") and the race runs again.

**The ending:**
1. Out of the car: the marshal: `T_CUP_WON` "Twinsen of Citadel Island wins the Two Worlds Cup!" Then the Esmer on the dock: "The Cup is
   yours, and its wish. What will it be?"
2. FunFrock: `T_FUNFROCK_LOST` "No! That wish was mine!"
3. An `ask_choice`:
   - "The statue gone, and FunFrock with it." (the story's ending)
   - "Another race. Right now." (FunFrock's own ending: `T_FUNFROCK_AGAIN` "Ha! You'll regret that." and the credits)
4. The statue gone: var 79 = 0, `change_cube(95)` shows Celebration as it was before the statue rose. The crowd on the dock: Zoe
   `T_ZOE_END` "Home, Twinsen?" Raph `T_RAPH_END` "Only if you let me win sometimes." The marshal's last line: `T_SAME_TIME` "Same time
   next year? We hear there's a temple, and a very long table..."
5. CHAPTER = 15, `the_end()`.

---

## 5. What the build and the engine need

**The engine (RACEMOD.CPP):**
- `shut=<var> <from> <text>`: a start line that won't start the story's race once the variable is at least `from`, saying the text.
  One more list beside `s_gates`, checked where `ClosedGate()` is.
- `dream_finish_text=<id>` for chapter 0's Sendell line (said as the dream's last flight starts).
- Nothing else: `gate=`, `win=`, `finish=`, `unlock_win=`, `wake=` and the rematch on re-entry are all there.

**The build (LBA Assembler, Terrain):**
- **RaceTrackStory, chapter by chapter.** A new static class per chapter group (as `ApplyCelebration` is), each adding its actors and
  scripts with the `Edit()` and `Add()` helpers RaceTrackStory.cs already has, and logging each step ("left out" when the game's own
  script isn't as expected).
- **The marshal** for every chapter from 2: the race's flag marshal (built, 2026-11-03: TempleMarshal.For -- the School of Magic's
  assistant on Citadel and the Desert, the Sup in her bikini elsewhere) as an actor on foot -- her entity, her flag-marshal body, on her
  plinth's top where the race file's `marshal=` has her -- with the marshal template, and the engine's new `marshal_drive_only=1`.
- **The black car:** FunFrock's car recoloured, a new racer generic, in RaceTrackCharacterCars. One actor of entity 157 per start
  scene from chapter 2 to 11, with the masked template, placed with `Beside()` 20 cells past the start line, its two track points
  further along the road.
- **The pickups' bodies:** an exam page (flat white sheet), a spare wheel, a Dark Monk statuette, a slab of cooled lava, the workshop
  note. Each a static body appended to the entity of an object, as the gloves' shelf body is.
- **The trail arrows:** holomap positions 224 + chapter and 239-254, each placed at its pickup (RaceTrackStory.Arrow, which today
  places the start-line arrows).
- **Silencing original actors:** a pass over each island's outside scenes in the story set, which replaces the life script of any
  actor that changes cube, gives an object or sets a plot variable, with the good-luck line. Logged by island.
- **Travel:** each marshal's `change_cube` to the next island's start scene (3.7). The Desert's arrival through the ferry, as noted.
- **Celebration 2's line-up:** `RaceDriver.Celebration` = FunFrock (main), the Dark Monk, a Sup priest, the Franco priest. The cars
  exist; they're the `statue` unlock's.
- **The story set:** the Temple's line taken out; each file's new keys (3.5).
- **The texts:** about 150 new lines, each in six languages. English first, then translated as the storm's were.
- **New game variable names** as constants in RaceTrackStory (3.1), with the free range checked again by `scriptgrep2` at build time:
  the build stops with a message if a script anywhere uses one.

**The installer (LBRA):** nothing new: it writes the story set as now. Story Mode's "Continue" keeps working, since every state is a
game variable and saves keep them.

---

## 6. Order of work, and how each chapter is tested

**Order:**
1. The engine's `shut=` and the chapter variable through the story set, with the marshal in chapter 2 (the town) only. This proves
   arrival, after-race, prize and travel on a chapter that's mostly built.
2. Chapter 1's thief and chapter 0's Sendell line.
3. Chapters 3 to 14 in story order, one island per round.
4. The silencing pass, island by island as each chapter is built.
5. The texts translated, last, once the English is settled.

**Testing each chapter** (as the storm's story was tested):
- A save at the chapter's start: `vargame 212 <N>` plus the earlier chapters' items and variables, `cube <start scene>`.
- The quest walked through with `teleport` and `input action` (no keys sent to the user's window), each line checked in the log
  ("[racemod] text N said") and a few with `ui dialog <id> <png>`.
- The race run by the pilot (run_story.ps1 style), once lost and once won: the rematch line, then the clue, the prize, the chapter
  number and the next island's start scene.
- A finished chapter's start line driven over again: "Closed for the season", and no race.
- The whole story run end to end once all chapters are in, from New story to `the_end()`.
