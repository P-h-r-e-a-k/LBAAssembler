# Little Big Racing Adventure: the walkthrough script

The user, 2026-11-03: "Further flesh out the story line we're creating. Work out each scene, each actor and their dialog. We don't need to
worry about existing quest flags, we can just reuse them instead of avoiding them. Give me a detailed walkthrough style script. Other
ideas I've had, to reuse the radio mechanic that Baldino uses to phone Twinsen perhaps with specific car updates for each track, and
perhaps Zoe acting as pit crew, we could have her as a second playable character. If only a single person is playing Zoe can just follow
Twinsen around. Twinsen is still the main character and we want to avoid character switching."

This is the story as the player meets it: where each chapter happens, who is there, what Twinsen does and what everyone says. The plan
([LBRA_STORY_PLAN.md](LBRA_STORY_PLAN.md)) has the mechanics: the script templates, the race files' keys and what has to be built. Where
the two differ, this script is the newer: it reuses the game's own flags (below), adds the radio and gives Zoe her part.

Nothing here is built yet beyond what is marked **(built)**. Names in *italics* are placeholders for the user to change.

---

## The running parts

### Twinsen and Zoe

Twinsen is the one you play, always. There's no switching.

**Zoe joins him in chapter 2** as his pit crew, and goes with him to every island after that.

- **One player:** Zoe is a companion. On foot she follows Twinsen at a few steps, as the game's own followers do (`MOVE_FOLLOW` on
  Twinsen). She waits where he talks to someone and has a line in most conversations (`message_obj(ZOE, ...)`). Talking to her (Action
  beside her) gives a hint for the step Twinsen is on. In a race she stands in the **pit box** by the start line, with a toolbox.
  - **Pit service:** driving slowly through the pit box beside her (under 20 km/h) is a pit stop: the item box is filled with a fuel can
    (the boost), a jolt shaken off (shrunk or skidding undone), and Zoe says one short line. Once a lap at most.
  - The pit box is the pit lane's box where the track has one (Desert, Citadel 2, the Emerald Moon), else a spot beside the road a few
    cells after the start line, where the marshal's plinth isn't.
- **Two players:** player 2 *is* Zoe.
  - **In races** she drives her own car (Zoe's car, racer generic 4, which exists), on the same grid as Twinsen's opponents. She's
    Twinsen's team-mate: her items hit the opponents, never Twinsen. The race is won when **Twinsen** beats the one to beat. Zoe
    finishing ahead of the one to beat as well is a team bonus: a line from the marshal and a better prize where there is one.
  - **On foot** she walks herself, wherever she likes. The story's people talk to Twinsen only; Zoe can talk to anyone for her hint
    lines, and Action at a pickup works for her too, so she can fetch while Twinsen talks.
  - There's no pit crew with two players: she's on the grid.
  - This is built on the two-player mode that already exists (two game processes, the second drawn in the first as a ghost car). In the
    story, the second process plays Zoe; the first owns the story.
- **Where she is in a scene:** with one player, the companion actor stands beside Twinsen when a scene loads. She's left out of a few
  places:
  - the dream (chapter 0);
  - the storm (chapter 1), where she's at home;
  - the Elevator Platform (chapter 8), which is a head-to-head and has no pit box;
  - the final (chapter 14), where she's in the crowd on the dock.

### Baldino's radio

The **Portable Radio** (item 26) is the game's own: "This portable radio allows you to dialogue with Baldino and Zoé when they are
listening and if they are not out of range." Zoe brings it in chapter 2: Baldino sends it from the Desert.

From then on Baldino calls at every island, a few seconds after Twinsen arrives.

1. The radio beeps.
2. The line "The radio is ringing. Answer it from your inventory." is shown once, the first time.
3. Twinsen answers with the radio from his inventory (`use_inventory(26)`, as the game's own calls do). If he hasn't answered after 20
   seconds it answers itself, so it never blocks.
4. Baldino's call is the **car update for that track**: what he has changed on Twinsen's car, sent over by "the Esmers' delivery
   drone" before the race.
   - The update is the track's own best setup (top speed, acceleration, steering, gears: `UseTrackSetups`, which exists), applied for
     the story's race of that track.
   - Some updates add something for the start, a starting item (a new key, `start_item=`).

The calls also carry Baldino's side of the story. He's the one who keeps track of the masked driver's trail on his own big radio set at
home.

### The flag marshals (built)

Each race is started by a flag marshal in the middle of the road and finished by her waving the chequered flag beside the line:
- **Citadel 1, Citadel 2 and the Desert:** the School of Magic's assistant, *Flick*.
- **Every other track:** the Imperial Hotel's Sup, *Lyra*, the Cup's grid girl.

In the story they're also the Cup's officials. On foot each is an actor on her plinth who greets Twinsen, explains the island's
trouble, says the result and sends him on (the plan's marshal script).

### The game's flags, reused

The original quests don't run, so their flags are free to use for what they're named after.

| Flag | The game's meaning | The story's |
|---|---|---|
| `chapter()` / `inc_chapter()` (var 253) | the game's chapter; Citadel's fine weather from chapter 2 | the story's chapter: 0 the dream, 1 the storm, 2 the town circuit (fine weather, as the game has it), 3 the Desert ... 14 the final, 15 the Cup won. Each race won does `inc_chapter()`. |
| var 51 | the storm plot | the storm plot (built) |
| var 56 | the lighthouse keeper back | as built |
| var 70 | the Esmers landed | as built |
| var 74 | Baldino's car mended (3 = the buggy fixed) | 3 from the start: Twinsen's car is ready (built: BuggyAlways) |
| var 26 / item 26 | the portable radio | the radio, given in chapter 2 |
| var 79 | the statue risen | the statue risen, by the palace's race (built) |
| var 124 | the souvenir seller has told | the seller has told (chapter 6) |
| items 7, 10, 11, 13, 15, 16, 18, 20, 24, 30-35 | their own | as the plan's item table |
| vars 200-209 | the race mod's own (built) | as built |
| vars 212-224 | free | each chapter's quest steps (the plan's 3.1) |

---

## Chapter 0 — The dream (Polar Island) **(built)**

**Where:** Polar Island (scene 252), a dream; then Twinsen's house (scene 0).

**Cast:**

| Who | Made from | Where |
|---|---|---|
| FunFrock | his car (built) | ahead, on the road |
| The grobo | built (Polar Island's) | by the start, with the digger's key |
| Zoe | the game's Zoe, scene 0 actor 4 | at home, by the bed |

**Walkthrough:**
1. **Opening.** The new story begins in the dream (built). Twinsen stands on the dock at Polar Island beside his car. FunFrock's car is
   already driving off up the island.
   > **The grobo:** "He went that way! The big man in the big car! Take the digger key, quick!" *(built: PolarDream.ChaseGroboText)*
2. **The chase.** Twinsen chases FunFrock up the island, round the loop and over the jumps, to the leap onto the rocky peak (built).
   Losing runs the dream again.
3. **The finish.** As the car leaves the lip, the white fade begins (built), and a voice speaks over it (new):
   > **Sendell (unseen):** "Two worlds will race, Twinsen. And at the end of it, he will be waiting."
4. **Waking.** Twinsen wakes in his bed. Zoe shakes him awake (built), kisses him (built), and her opening line (built, chapter 1's
   first line) follows.

**Flags:** var 205 (built). The wake sets `chapter()` to 1.

---

## Chapter 1 — The storm (Citadel 1) **(built, with the thief's quest new)**

**Where:** Citadel Island in the rain:
- Twinsen's house (scene 0)
- the Weather Wizard's tent (scene 21)
- the storm track's start, near the Tavern (scene 42)
- the Tavern (scene 3)
- the harbour (scene 43)
- the lighthouse (scene 46)

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Zoe | the game's (scene 0 actor 4) | home |
| The Weather Wizard | the game's (scene 21 actor 2) | his tent |
| Raph | built (a copy of scene 2 actor 2) | lapping the storm track, then by the start line |
| Mr. Paul | built (a copy of scene 7 actor 2) | the start line |
| Tim the waiter | the game's (BODY 40) | the Tavern's door |
| The thief | the game's (BODY 117), copied | behind the harbour warehouse |
| Flick, the marshal | built (the race's); an actor on foot (new) | her plinth by the start line |
| The Esmers | built (a copy of scene 42 actor 6) | by the Tavern after the spell |

**Walkthrough:**

1. **Home.** Zoe's opening (built):
   > **Zoe:** "Twinsen, I am sick of all this rain! Go and find the Weather Wizard and get him to fix it!"

   And a new line after it:
   > **Zoe:** "Oh, and your driving gloves were on the washing line this morning. Now they're gone! Somebody's been light-fingered
   > again."

   Q_THIEF = 1. The attic shelf shows the darts again, not the gloves.
2. **The Wizard's tent.** (built)
   > **The Weather Wizard:** "I was going to clear the rain, but I need to do it from the top of the lighthouse, and that rascal Raph
   > is too busy at the race track and refuses to let me."

   The arrow points at the storm track's start line.
3. **The storm track.** Raph laps the track in his car until Twinsen stands on the start line. Then he screeches up (built):
   > **Raph:** "Hey! Get off the track, Twinsen! Are you trying to get yourself run over?!"

   Then Raph (built):
   > **Raph:** "I'm having too much fun, it can wait. I'll tell you what: if you can beat my time on the track, then I'll come with you
   > to the lighthouse. Mind you, you'll need a car and some driving gloves to take part."

   Mr. Paul (built):
   > **Mr. Paul:** "Sorry Twinsen, for safety reasons you'll need some driving gloves to take part."

   Flick, the marshal, on her plinth (new):
   > **Flick:** "I'm the School of Magic's assistant, but on race days I'm the marshal. No gloves, no flag, I'm afraid. I saw a fellow
   > with a pair stuffed in his pocket go past the Tavern."
4. **The Tavern.** Tim, at the door:
   > **Tim:** "A fellow ran past with gloves in one hand and a customer's umbrella in the other. Towards the harbour! If you catch him,
   > I want that umbrella back."

   Q_THIEF = 2. The arrow points at the harbour.
5. **The harbour.** The thief is behind the warehouse. When Twinsen comes within three cells:
   > **The thief:** "Can't catch me!"

   He runs round the warehouse to the end of the quay, where there's nowhere left to go (Q_THIEF = 3). Action beside him:
   > **The thief:** "Fine, fine! Here are your gloves. Nice gloves, too. See you on the town circuit, if they ever build one."

   Found: Driving gloves (item 7). He drops the umbrella. Q_THIEF = 4.
6. **The umbrella (optional).** Picked up: Customer's umbrella (item 20). Back to Tim:
   > **Tim:** "Our customer's umbrella! Have a drink on the house. Well, a lemonade: you're driving." *(10 kashes)*
7. **The race.** Raph now tells his time (built):
   > **Raph:** "A car and driving gloves: you're all set, Twinsen! My best lap is ## seconds. Beat that, and I'll come with you to the
   > lighthouse."

   Flick drops the flag. One lap against Raph's time (built).
8. **Won.** (built)
   > **Raph:** "You beat my time, Twinsen! A deal is a deal: I'm off to the lighthouse. See you there!"

   Zoe joins Twinsen, the two walking together (built, behaviour 5):
   > **Zoe:** "Twinsen, you did it! Raph's on his way to the lighthouse, and the Weather Wizard is meeting us there. Come on, let's go
   > together!"
9. **The lighthouse.** (built) The Wizard's spell. The storm clears and the game goes to chapter 2 (its own `inc_chapter`, var 253 =
   2: fine weather). The Esmers land by the Tavern. Their line gains a sentence (new):
   > **The Esmer:** "People from the planet Twinsun, we come to you in a spirit of peace. Thank you for clearing the rain which was
   > preventing us from landing. As a thank you, we'll build you an even better race track. It should be ready tomorrow... and tomorrow
   > we open a Cup for both your worlds."
10. **Tired.** Everyone Twinsen talks to (built):
    > "You look tired, Twinsen. You should go home and have a nap."

    The bed (built):
    > **Twinsen:** "What a day! A good night's sleep... Morning already! The aliens' new race track should be ready by now."

**Flags:** var 51, 56, 70 (built); `chapter()` 1 -> 2 by the game's own spell; Q_THIEF (213); items 7 and 20.

---

## Chapter 2 — The town circuit (Citadel 2)

**Where:** Citadel Island in fine weather:
- Twinsen's house (scene 0)
- the yard north of the house, where the cars park
- the town circuit's start, near the Dino-Fly (scene 49)
- the harbour (scene 43)

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Zoe | the game's (scene 0 actor 4), then the companion | beside her car in the yard |
| Flick | the marshal (built), on foot | her plinth |
| The Esmer herald | a copy of scene 42 actor 6 | beside the start line |
| Mr. Paul | built | the start line |
| Raph, the Tralu, the thief | the race's (built) | the grid |
| Bob | the game's Bob (BODY 152) | by the scrap heap at the harbour |
| Mrs. Brune of the ferries | the game's (BODY 62) | the ticket office |
| The masked driver | the black car (new) | past the finish |

**Walkthrough:**

1. **The morning.** Twinsen gets up. At the house door:
   > **Zoe:** "There's a crowd at the new track already! Come on, I want a look... and I've got my own car this time."
2. **The yard.** Zoe stands by her car, which has one wheel missing:
   > **Zoe:** "I'm racing too! Only... my car's back wheel came off on the way here. Bob says there's a spare in the scrap at the
   > harbour."

   Q_ZOECAR = 2. The arrow points at the harbour.
3. **The start line.** The Esmer herald:
   > **The Esmer herald:** "Welcome to the Two Worlds Cup! One race on every island of Twinsun and Zeelich, and to whoever wins them
   > all, a wish. Any wish. The first race is here, on Citadel."

   Flick:
   > **Flick:** "Six cars on the grid or Mr. Paul won't start it. That's you, Raph, Mr. Paul, the Tralu, that thief again... and Zoe,
   > if she ever gets her wheel on."

   Mr. Paul:
   > **Mr. Paul:** "Rules are rules, Twinsen. Six cars."

   The gate says it too (`gate=214 4`).
4. **The harbour.** Bob, by the scrap:
   > **Bob:** "A wheel? Somewhere under that lot. Mind the fish."

   The wheel is in the scrap, beside the road:
   > **Twinsen:** "A wheel. A bit rusty, but round."

   Q_ZOECAR = 3.
5. **Back to Zoe.**
   > **Zoe:** "Perfect!"

   She fits it.
   > **Zoe:** "There. And Twinsen, a parcel came for you on the morning ferry, from Baldino on the Desert. A portable radio! He says
   > he wants to hear how the Cup goes."

   Found: Portable Radio (item 26). Q_ZOECAR = 4.
   > **Zoe:** "Now you'd better watch your back on the grid."
6. **The first call.** The radio rings: "The radio is ringing. Answer it from your inventory." Twinsen answers.
   > **Baldino (radio):** "Hello? Twinsen? Can you hear me? Excellent, the radio works! Listen, I've been reading about this Cup. I
   > tuned your car before you ever knew it needed tuning: shorter gears for the town's tight corners. Try not to scratch the paint."

   Car update: the Citadel town circuit's best setup.
7. **The race.** Six cars, three laps (built). Flick drops the flag. Beat Mr. Paul.
8. **Won.** Mr. Paul (built):
   > **Mr. Paul:** "Well raced, Twinsen! You've won the prize: a ferry ticket."

   Found: Ferry Ticket (item 13).

   The herald:
   > **The Esmer herald:** "Twinsen of Citadel Island leads the Cup! Next: the Desert, the White Leaf Desert."

   **The masked driver:** past the finish, a black car stands with its engine running. When Twinsen comes near, it drives off and
   is gone.
   > **Raph:** "Did you see that black car? No number, no driver I could see. And gone before I'd blinked."
   >
   > **Zoe:** "Whoever it is, they weren't racing. They were watching."

   Zoe, after the race, if Zoe beat Mr. Paul too (two players):
   > **Zoe:** "Second! Or first, depending on who's counting."
9. **The ferry.** Zoe comes along now (one player: she follows Twinsen):
   > **Zoe:** "Every race needs a pit crew. That's me. You drive, I'll fix."

   At the harbour, Mrs. Brune:
   > **Mrs. Brune:** "One ticket for the Desert... and the young lady goes free, she says she's crew."

   The ferry crossing (the game's own, built: the ticket) lands them on the Desert. `inc_chapter()` -> 3.

**Flags:** Q_ZOECAR (214), item 26 (the radio), item 13 (built), `chapter()` 2 -> 3.

---

## Chapter 3 — The White Leaf Desert

**Where:**
- the White Leaf Desert's harbour
- the track's start by the Temple of Bù (scene 67)
- J. Baldino's house (scene 38) and outside it (scene 60)
- the School of Magic (scene 27)
- the track, where the three pages are

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Flick | the marshal (the Dean's own assistant) | her plinth |
| The Dean of the School of Magic | the game's (BODY 123) | the School's gate |
| Baldino | the game's (scene 38) | his house, then the grid |
| Moya the turtle, the Dino-Fly | the race's (built), on foot by the last bend | |
| The racer, the Desert champion | the race's main | the grid |
| Three exam pages | a new flat body | beside the road at 30, 55 and 80 % |
| Zoe | the companion | with Twinsen; the pit lane in the race |
| The masked driver | the black car | past the finish |

**Walkthrough:**

1. **Landing.** The radio rings. Baldino:
   > **Baldino (radio):** "Hello? Twinsen? That's it, I'm at my house on the Desert. Come and see me before you race: your car's going
   > to want sand filters."

   (The game's own 960 is close: "Hello? Twinsen? That's it, I fixed the car... I'm bringing it to Desert Island...")
2. **The start line.** Flick:
   > **Flick:** "Twinsen! Good to see you again. Bad news: my Dean has shut the track. The engines blew his students' exam papers all
   > over the island. Three pages. He's at the School's gate, and he's in a mood."

   Q_PAGES = 1.
3. **The School.** The Dean:
   > **The Dean:** "Shut it is! Three pages, gone with the wind of your engines. Bring them back, and I'll think about it."

   Q_PAGES = 2. The arrow points at page 1.
   > **Zoe:** "The pages blew along the road. If we drive the course we'll find them, and learn the bends while we're at it."
4. **The pages.** Each is picked up driving over it, or on foot:
   - > *"A page: Chapter 1, Levitation for Beginners."*
   - > *"Another page: Turning Sand into Slightly Different Sand."*
   - > *"The last page: Why Wizards Should Not Drive."*

   Q_PAGES 2 -> 5.
5. **Back to the Dean.**
   > **The Dean:** "All three! Very well. Here: a diploma. It says you may use the School's grounds, which include the track. Don't let
   > it go to your head."

   Found: Wizard's Diploma (item 30). Q_PAGES = 6.
6. **Baldino's house.** Baldino, at his workbench:
   > **Baldino:** "Twinsen! My radio works then. Sand filters, fitted. A longer top gear for the long straights. And a word of advice:
   > the champion brakes late at the Temple bend. Brake later."
   >
   > **Baldino:** "I'm racing too, of course. I need to see how my work goes against my work."

   Car update: the Desert's best setup.
7. **The race.** The line-up: the racer (the one to beat), Moya, the Dino-Fly, the Dean, Baldino (built). Zoe is in the pit lane.
   - > **Zoe (pit stop):** "In, out, fuel's in. Go!"
8. **Won.**
   > **Flick:** "The Desert champion, beaten by a ferry-ticket driver! The Cup goes on, to the Emerald Moon."
   >
   > **The racer:** "Next year, Twinsen."

   **The clue:** Moya, by the last bend:
   > **Moya:** "There was oil on the last bend this morning. That wasn't there yesterday. A black car came through at night, slow,
   > like it was looking for something."
   >
   > **Zoe:** "Looking for something on a race track... or putting something on one."

   The masked driver's car is past the finish again, and drives off.
9. **To the moon.** Baldino:
   > **Baldino:** "You think you're fast? On the moon I've got a lander that would leave your car standing. Come and race me up there.
   > My rocket leaves now, room for three."

   `inc_chapter()` -> 4. `change_cube(229)`.

**Flags:** Q_PAGES (215), item 30.

---

## Chapter 4 — The Emerald Moon

**Where:**
- the track's start (scene 229; the copies of the moon's outside scenes 74-77)
- the moon base: beside Baldino's old cell (scene 23), where HAL is
- the base's stores

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Lyra, the marshal | the race's (built), on the deck's edge | by the start |
| HAL | the game's (scene 23 actor 4: sprites 228/229 working, 230 cracked) | the base |
| HAL's mechanics | the game's (scene 23 actors 12-15, entity 57) | hiding in the stores |
| The radio | Portable Radio, already Twinsen's | |
| Baldino | his lander, the race's main | the grid |
| Zoe | the companion; the pit lane (built) in the race | |
| The masked driver | the black car | past the finish |

**Walkthrough:**

1. **Landing.** Lyra, on the deck's edge:
   > **Lyra:** "Welcome to the moon, racers. I'm Lyra, the Cup's marshal from here to the end. Bad news: the base's computer has locked
   > the start gate, and nobody can get it to talk sense."

   Q_HAL = 1.
2. **The radio rings.** Baldino, from his lander's cockpit:
   > **Baldino (radio):** "Twinsen? Baldino, from the pits. Low gravity up here: I've softened your springs and put weight in the
   > nose, so you won't flip on the loops. And don't touch HAL, he hates me."

   Car update: the Emerald Moon's best setup.
3. **The base. HAL** (cracked):
   > **HAL:** "HAL!! ZX81!! HAL!!" *(the game's own 546/545)*
   >
   > **Zoe:** "I think that means no."
4. **The stores.** The mechanics, hiding:
   > **A mechanic:** "It shouted at us all night. Its voice box is broken. There's a spare in... no, wait, we used it. Any radio would
   > do."

   Q_HAL = 2.
   > **Zoe:** "Twinsen... your radio."
   >
   > **Twinsen:** "Baldino won't mind."
   >
   > **Zoe:** "Baldino will mind."

   Twinsen hands the radio over: item 26 = 0 for now. Q_HAL = 3.
   > **A mechanic:** "Leave it to us."

   They walk to HAL (their own track scripts). HAL's sprite becomes 228. Q_HAL = 4.
5. **HAL, mended:**
   > **HAL:** "Thank you, Twinsen. The gate is open. I have calculated your chances against Baldino: they are poor. I will race too,
   > to see if I am right."
   >
   > **HAL:** "Your radio is returned. I have improved it: it now receives the Esmers' channel."

   Found: Portable Radio (item 26 = 1 again).
6. **The race.** Baldino in his lander (the one to beat), HAL driving itself (built). Lyra drops the flag.
7. **Won.**
   > **Baldino (on foot):** "Beaten in my own lander! I'll have to tune the other one."
   >
   > **Lyra:** "Two worlds, two races... and now Zeelich. The Esmers' ship leaves for Otringal."

   **The clue:** HAL:
   > **HAL:** "My log shows a launch from this moon last night: one black car, one passenger, to Zeelich. The passenger's name was not
   > given. His laugh was on file."
   >
   > **Zoe:** "Who leaves a laugh on file?"

   `inc_chapter()` -> 5. `change_cube(88)`.

**Flags:** Q_HAL (216); item 26 taken and given back.

---

## Chapter 5 — Otringal

**Where:**
- Otringal's Upper City, the track's start (scene 88)
- the Lower City (scene 89)
- the Casino's entrance (scene 135)
- the space port (scenes 86, 92)

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Lyra | the marshal | her plinth |
| The race steward | the casino's bouncer (BODY 419) | beside the start line |
| The Casino's slot machines | the game's (scene 135 actors 24, 42, 49) | the Casino |
| Johnny Rocket | the game's (BODY 154) | the space port |
| A Dark Monk priest | the Sup priest (BODY 292) | in the crowd at the palace bend |
| Rick | the game's (BODY 425) | the Lower City, at his piano |
| Zoe | the companion | |
| The masked driver | the black car | past the finish |

**Walkthrough:**

1. **Arrival.** Lyra:
   > **Lyra:** "Zeelich! Here the Cup's run by the Emperor's stewards, and they want an entry fee. The Esmers left you a little Zeelich
   > money to start you off."

   10 zlitos. Q_FEE = 1.
2. **The radio rings.**
   > **Baldino (radio):** "Otringal! Tunnels and town streets: I've given you short gears and a louder horn, people walk in the road
   > there. Oh, and don't go in the Casino. I lost a lander in that Casino."
3. **The steward:**
   > **The steward:** "A Travel Token for the Cup costs 30 zlitos. Twenty-five is a nice number too, but it's not 30."

   Without 30 zlitos:
   > **The steward:** "Short? There's a Casino round the corner. Some people leave it richer."

   The arrow points at the Casino.
4. **The Casino.** The game's own slot machines: 1 zlito a pull, three money symbols win 25.
   > **Zoe:** "Baldino said don't."
   >
   > **Twinsen:** "Baldino said don't go in. I'm going in a little."
5. **Rick (optional),** at his piano in the Lower City:
   > **Rick:** "The Cup? I'll play you something fast for it."

   He plays the race's music for the Otringal race (the game's own).
6. **Paying.** The steward asks (`ask_choice`): "Pay 30 zlitos?" Yes:
   > **The steward:** "One Travel Token. You're on the grid."

   Found: Travel Token (item 24). Q_FEE = 2.
7. **The race.** The prize wheel and the golden Gazogem (built). Beat the racer.
8. **Won.**
   > **Lyra:** "A win on Zeelich! The stewards give you this: the Fragment of the Sups. Win the fragments of all of Zeelich's peoples,
   > they say, and the Emperor's own gate will open to you."

   Found: Fragment of the Sups (item 32).

   **The priest,** as Twinsen passes on foot:
   > **The priest:** "Enjoy it while you can, little racer. The Cup's last race is already won."
   >
   > **Zoe:** "Charming."

   **The clue:** Johnny Rocket, at the space port:
   > **Johnny Rocket:** "A black car? Came off the moon ship last night and took the first shuttle to Twinsun. The souvenir seller on
   > Celebration Island sells pictures of every car he's seen. Ask him."
   >
   > **Johnny Rocket:** "I'll fly you there myself, if you like."

   `inc_chapter()` -> 6. `change_cube(223)`.

**Flags:** Q_FEE (217), item 24, item 32, money.

---

## Chapter 6 — The lava lake (Celebration 1) **(the seller's race is built)**

**Where:** Celebration Island before the statue: the souvenir seller's stall (scene 95) and the lava lake's track (scene 223).

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Lyra | the marshal | by the lava lake's start |
| The souvenir seller | built (scene 95 actor 5, entity 213) | his stall |
| Four Dark Monk statuettes | a new small body | round the island, beside the road |
| The kangaroo | the game's tourist (BODY 308) | by the dock, then the grid |
| The policeman | the race's (built) | the grid |
| Zoe | the companion | |
| The masked driver | the black car | past the finish |

**Walkthrough:**

1. **Arrival.** Lyra:
   > **Lyra:** "Celebration Island. The seller here won't even race until his stall is put right. You'll see why."
2. **The radio rings.**
   > **Baldino (radio):** "Celebration's lava lake: hot enough to melt tyres. Heat shields under the car, fitted. Don't stop on the
   > lava. I mean it."
3. **The stall.**
   > **The seller:** "Look at my stall! That kangaroo bounced into it and my souvenirs went everywhere. Four of them. Find them, and
   > maybe I'll race you. Maybe."

   Q_STALL = 2. The arrow points at the first.
4. **The kangaroo, any time:**
   > **The kangaroo:** "Boing! Sorry about the stall. Boing!"
5. **The souvenirs,** four times:
   > *"A little statue of the Dark Monk. Ugly, but someone will buy it."*
   >
   > **Zoe** (at the fourth): "Every one of these has the statue's face. Who'd want four?"
6. **The seller.**
   > **The seller:** "All four! Now, about that race: beat me, and I'll tell you what I saw on Island CX."

   This is his challenge (built), sending Twinsen to the lava lake (scene 223).
7. **The race.** Three laps (built). Beat the seller.
8. **Won.** The seller (rewritten, built: the after-race controller):
   > **The seller:** "I said I'd tell you. On Island CX I saw Franco engineers building a car. Black, no number. The driver wore a
   > mask, but I know a pair of glasses when I see one. And it ran on gazogem, the kind from the Francos' refinery."

   The seller's told-flag is set (var 124 = 1, the game's own).
   > **Zoe:** "Glasses... Twinsen, the dream. You told me about the dream."
   >
   > **Twinsen:** "It was a dream."
   >
   > **Lyra:** "The Francos' refinery? That's our next stop anyway. The ship leaves now."

   `inc_chapter()` -> 7. `change_cube(257)`.

**Flags:** Q_STALL (218), var 203/204 (built), var 124.

---

## Chapter 7 — The Island of the Francos

**Where:**
- the Francos' island (made twice its size, built; the start in scene 257)
- the refinery
- Mr. Kurtz's laboratory
- the village

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Lyra | the marshal | her plinth |
| De La Fontaine | the game's (BODY 450) | the refinery's gate |
| Mr. Kurtz | the game's (BODY 446) | his laboratory |
| The Franco nurse | the game's (BODY 447), with her pram | the village |
| The oil slick | the race's oil body | the dock, by the start |
| Zoe | the companion | |
| The masked driver | the black car | past the finish |

**Walkthrough:**

1. **Arrival.** Lyra:
   > **Lyra:** "The Francos' race is De La Fontaine's, and so is the refinery. Ask him about that fuel, if you dare."
2. **The radio rings.**
   > **Baldino (radio):** "The Francos' island is twice the size it looks, the race people made it so. Top speed up, bigger tank. And
   > Twinsen, find out who's buying that gazogem."
3. **De La Fontaine:**
   > **De La Fontaine:** "Who buys my gazogem is my business. Race me, if you want to talk."
4. **The dock.** A fresh slick by the start (the arrow):
   > **Twinsen:** "Fresh oil, still warm."
   >
   > **Zoe:** "I've got a can in the toolbox. Scoop some up."

   Found: Can of GazoGem (item 15).
5. **Mr. Kurtz:**
   > **Mr. Kurtz:** "A sample? Splendid! But my nurse has my glasses, and she's out with the pram. Bring her here and I'll tell you
   > everything that's in it."

   Q_FUEL = 2. The arrow points at the nurse.
6. **The nurse,** in the village:
   > **The nurse:** "His glasses? On his head, as always. I'll come and show him."

   She follows Twinsen (`MOVE_FOLLOW`), with Zoe beside her. Q_FUEL = 3.
7. **Mr. Kurtz, with the nurse:**
   > **Mr. Kurtz:** "On my head! Now then... this is the refinery's own gazogem, the best grade, sold only by the tanker-load. Keep the
   > can: it's evidence."

   Q_FUEL = 4.
8. **De La Fontaine again:**
   > **De La Fontaine:** "...So you know. Beat me on the track and I'll tell you who I sold it to."
9. **The race.** De La Fontaine in the gazogem tanker (the one to beat), Mr. Kurtz in his laboratory, the nurse in her pram (built).
10. **Won.**
    > **Lyra:** "The Fragment of the Francos is yours!"

    Found: Fragment of the Francos (item 31).

    **The clue:** De La Fontaine:
    > **De La Fontaine:** "A masked man paid for a whole tanker. Cash, Dark Monk coins. I sent it down the Elevator Platform to the
    > undergas, where he said he'd collect it."
    >
    > **De La Fontaine:** "Take the next convoy down and see for yourself."

    `inc_chapter()` -> 8. `change_cube(120)`.

**Flags:** Q_FUEL (219), items 15 and 31.

---

## Chapter 8 — The Elevator Platform (head-to-head)

**Where:** the Elevator Platform (scene 120).

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Lyra | the marshal, on the deck's edge | the start |
| The monkey monster | his crate (built) | the grid |
| The small monkey monsters | built (crate-throwers on the lower decks) | |
| The Franco policeman | the game's (BODY 86) | by the elevator |
| Zoe | the companion, watching from the elevator's cage | no pit box: a head-to-head |

**Walkthrough:**

1. **Arrival.** Lyra:
   > **Lyra:** "The monster won't let anybody down to the undergas without a race. It's just you and him."

   The monster, from his crate:
   > **The monkey monster:** "RAAARGH! Race! Now!"
2. **The radio rings.**
   > **Baldino (radio):** "That platform's a rollercoaster. Grip, grip and more grip, I've given you all I've got. Hold on to your hat.
   > And your lunch."
3. **The race.** The monster alone (built). No quest.
4. **Won.**
   > **The monkey monster:** "Hrrm. Good race. Grr."
   >
   > **Lyra:** "The elevator is yours. Down we go!"

   **The clue:** the policeman:
   > **The policeman:** "A black car went down before you. Didn't race the monster either. He's still sulking about it."

   `inc_chapter()` -> 9. `change_cube(98)`.

**Flags:** none new.

---

## Chapter 9 — Wannies Island

**Where:** Wannies Island near the elevator (scene 98), the frog and the tower; the city (scene 118).

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Lyra | the marshal, on the deck's edge | the start |
| The Wannie miner | the game's (BODY 342) | by his bulldozer |
| The old Wannie | the game's (BODY 386) | near the frog |
| The glove | Wannie's glove (item 11) | just inside the frog's mouth |
| Zoe | the companion | |
| The masked driver | the black car | past the finish |

**Walkthrough:**

1. **Arrival.** Lyra:
   > **Lyra:** "The Wannies' race is off. Their best driver won't drive."
2. **The radio rings.**
   > **Baldino (radio):** "The frog track! A lighter car for the frog's mouth, and a tighter steering rack for the tower's spiral.
   > Don't get eaten."
3. **The miner:**
   > **The Wannie miner:** "No glove, no driving. Safety first! They say that on Citadel too, I hear. I lost it by the frog. The frog
   > keeps snapping at me."
   >
   > **Zoe:** "Gloves again. Everyone on these islands loses their gloves."
4. **The old Wannie:**
   > **The old Wannie:** "The frog opens its mouth every few seconds. Your glove's just inside. Be quick, dear."
5. **The glove.** It's reachable while the frog's mouth is open (the race mode's timed frog); a shut mouth pushes Twinsen back out.
   Found: Wannie's glove (item 11).
6. **The miner:**
   > **The Wannie miner:** "My glove! Right, you. To the grid."

   Item 11 = 0.
7. **The race.** The miner's bulldozer (the one to beat), the mine cart, the old Wannie's firefly tart (built).
8. **Won.**
   > **Lyra:** "The Fragment of the Wannies!"

   Found: item 34.

   **The clue:** the old Wannie:
   > **The old Wannie:** "The black car's driver paid us to dig him a tunnel to Mosquibees Island. Paid in coins with the Dark Monk's
   > face on them. The tunnel's finished: you may as well use it."

   `inc_chapter()` -> 10. `change_cube(102)`.

**Flags:** Q_GLOVE (220), items 11 and 34.

---

## Chapter 10 — Mosquibees Island

**Where:** Mosquibees Island on the bridge (scene 102); the Queen's throne (scene 104).

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Lyra | the marshal, on the deck's edge | the start |
| The Queen's ventilation guy | the game's (BODY 197) | at the grid's gate |
| A Mosquibee of the hide-out | BODY 193 | hiding beside the road |
| The honey-pot Mosquibee | the game's (BODY 192) | by the start |
| The Queen | the race's main | the throne, then the grid |
| Zoe | the companion | |
| The masked driver | the black car | past the finish |

**Walkthrough:**

1. **Arrival.** Lyra:
   > **Lyra:** "The Queen races only those she's allowed onto her island. We're... not sure you are."
2. **The radio rings.**
   > **Baldino (radio):** "Mountain gearing for the Queen's climbs. Torque, torque, torque. And bow. Queens like a bow."
3. **The ventilation guy:**
   > **The ventilation guy:** "No pass, no grid. The Queen's orders."

   Then, quietly:
   > **The ventilation guy:** "...unless you know the right people. Try down the road."
4. **The honey-pot Mosquibee (optional):**
   > **The honey-pot Mosquibee:** "The Queen's never lost a race on her own island. Never!"
5. **The hide-out's Mosquibee,** hiding:
   > **The Mosquibee:** "You're the racer who's after the black car? So are we. Take this ring and show it at the grid: our friend there
   > will let you through. And after the race, come and find me. We've got a message for you."

   Found: Dissidents' Ring (item 16).
6. **The ventilation guy again:**
   > **The ventilation guy:** "Ah. That ring. Off you go, then. I never saw it."
7. **The race.** The Queen (the one to beat), the monkey monster in his war cart (built).
8. **Won.**
   > **The Queen:** "Hmph."
   >
   > **Lyra:** "You beat the Queen! The Fragment of the Mosquibees!"

   Found: item 33.

   **The clue:** the hide-out's Mosquibee, at the start line:
   > **The Mosquibee:** "Here's our message: a map. The black car's workshop is on Island CX, but its driver hides under Celebration
   > Island, in the volcano. Our hide-out is there. We'll take you."

   `inc_chapter()` -> 11. `change_cube(130)`.

**Flags:** Q_PASS (221), items 16 and 33.

---

## Chapter 11 — The Island under Celebration

**Where:** the volcano's hide-out (scene 130).

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Lyra | the marshal | her plinth |
| The dissident | the game's (BODY 270) | the hide-out |
| The hide-out's Mosquibee | BODY 193 | at the cracks |
| Three cooled lava slabs | the lava slab car's slab, as an object | beside the road |
| The ferryman | the game's (BODY 334) | his boat |
| Zoe | the companion | |
| The masked driver | the black car | past the finish |

**Walkthrough:**

1. **Arrival.** Lava balls are falling (the race's own lavaball=). Lyra:
   > **Lyra:** "The dissidents' hide-out is cracking under the lava. Nobody's racing until it holds."
2. **The radio rings.**
   > **Baldino (radio):** "Lava balls?! Armour plate on the roof. And a protection spell in the boot for the start, you'll want it."

   Car update: the Volcano's best setup, and a protection spell in the item box at the start (`start_item=`).
3. **The dissident:**
   > **The dissident:** "Help us seal the cracks, and I'll race you. I'll even tell you who's under the mask."
4. **The hide-out's Mosquibee:**
   > **The Mosquibee:** "Cold slabs of lava rock, three of them. They've cooled by the road."
5. **The slabs,** three times:
   > *"A slab of cooled lava. Heavy!"*
   >
   > **Zoe** (at the third): "Let me take that one."
6. **Sealed.**
   > **The Mosquibee:** "That'll hold!"
7. **The race.** The dissident (the one to beat), the ferryman in his boat, the hide-out's Mosquibee on a slab (built).
8. **Won.**
   > **Lyra:** "Well raced! The Cup goes on to Island CX."

   **The clue:** the dissident:
   > **The dissident:** "I promised. I've seen him without his mask: FunFrock. Yes, *that* FunFrock. He builds his cars on Island CX and
   > buys his soldiers from the Emperor's palace. He wants the Cup's wish, to bring back the Dark Monk."
   >
   > **Zoe:** "Twinsen... the dream."
   >
   > **Twinsen:** "I know."

   The radio rings at once, unasked:
   > **Baldino (radio):** "Twinsen? I heard that on the Esmers' channel. FunFrock! I'll fit everything I have on the car before the end.
   > Everything."

   **The ferryman:**
   > **The ferryman:** "Island CX? I'll take you, for a song. *The* song. Here, I'll teach you."

   Found: Ferryman's Song (item 18).
   > **The ferryman:** "...beautiful. Well, nearly. In you get."

   `inc_chapter()` -> 12. `change_cube(343)`.

**Flags:** Q_SLABS (222), item 18.

---

## Chapter 12 — Island CX

**Where:** Island CX, made twice its size (built: Mk2, scenes 340-343); its halls under the gas.

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Lyra | the marshal, on the deck's edge | the start |
| The Franco survivor | the game's (BODY 90), on his raft | the shore |
| A Franco trooper | BODY 290 | by the cannons |
| The workshop note | a new flat body | the halls, beside the road at 70 % |
| The Emperor's soldier | the race's main | the grid |
| Zoe | the companion | |

**Walkthrough:**

1. **Arrival.** Lyra:
   > **Lyra:** "Island CX. The race goes down through that door into the halls, and the door is locked. The soldiers lost the key."
2. **The radio rings.**
   > **Baldino (radio):** "Cannons and gas monsters! I've painted the car grey so the gunners don't see you. That's not a joke, it's
   > science. Mostly."
3. **The trooper:**
   > **The trooper:** "Lost it? The survivor on the raft has it. Won't give it back, either. Says his motor's out of fuel."
4. **The survivor:**
   > **The survivor:** "The Key for the passage? Mine. Fair and square. But if you had some fuel for my raft..."

   With the Can of GazoGem (item 15, from chapter 7):
   > **The survivor:** "Gazogem! The good stuff! Here, take the key."
   >
   > **Zoe:** "So much for evidence."

   Item 15 = 0. Found: Key for the passage to CX (item 35).
5. **The race.** Round the island under the cannons, down through the door and round the halls (built). The Emperor's soldier (the one to
   beat), the Franco trooper's tank, the survivor on his raft (built).
6. **Won.**
   > **Lyra:** "Island CX is yours!"

   **The clue:** the workshop note, in the empty workshop in the halls (the arrow):
   > *"To the palace. The Emperor will lend me his teleporter, or he will lose his throne. F."*
   >
   > **Lyra:** "The palace! The soldiers' transport leaves now, and we're on it."

   `inc_chapter()` -> 13. `change_cube(290)`.

**Flags:** Q_CXKEY (223), items 15 and 35.

---

## Chapter 13 — The Emperor's palace **(the sprint out of the window is built)**

**Where:** the palace's rooms (scenes 265-290); the start in the south-east room.

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Lyra | the marshal ("a Sup at home in the palace") | by the start |
| The sealed gate | the palace's door at the start room | |
| The Empress | the game's Emperor's wife (BODY 454) | the music room |
| The Emperor | the game's (EmperorBody) | locked in his bedroom |
| Stan | the game's (BODY 439), with his time machine | the library |
| The pighead guards | built (guard= lines) | the rooms |
| Zoe | the companion | |

**Walkthrough:**

1. **Arrival.** Lyra:
   > **Lyra:** "FunFrock has taken the palace! The gate's sealed with the Emperor's seal..."

   The four Fragments (items 31-34) open it:
   > **Lyra:** "...and it knows the Fragments of the four peoples when it sees them."
2. **The radio rings.**
   > **Baldino (radio):** "Indoors! Marble floors, so soft tyres. And Twinsen, don't break anything, those vases are older than me."
3. **The Empress:**
   > **The Empress:** "My husband! FunFrock's men have locked him in his bedroom. The lock answers to a song, but I can't sing."

   Twinsen has the Ferryman's Song:
   > *Twinsen sings the ferryman's song. It's... not good. The door opens anyway.*
   >
   > **Zoe:** "I've heard worse. Not much worse."
4. **The Emperor:**
   > **The Emperor:** "Twinsen of Twinsun! FunFrock has my teleporter, past the round window at the end of the long hall. If we get
   > there first, it's ours. Take this: you've earned it."

   Found: Emperor's Sword (item 10). Q_EMPEROR = 3.
5. **Stan, in the library:**
   > **Stan:** "The window? I could take you there in my time machine five minutes ago. No? Then race me there."
6. **The race.** The sprint to the window (built). Stan (the one to beat), the road sweeper, and the Emperor himself.
7. **Won.** The window shatters. The teleporter takes the car in mid-air (built). Over the flight (new):
   > **FunFrock (unseen):** "Very good, Twinsen. Now come and finish it. I'll be waiting, just like in your dream."

   The statue rises on Celebration Island (built, var 79 = 1). `inc_chapter()` -> 14.

**Flags:** Q_EMPEROR (224), item 10, var 79.

---

## Chapter 14 — Round the statue (Celebration 2): the final

**Where:** Celebration Island with the Dark Monk's statue risen (scene 95), the dock.

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Lyra | the marshal | the dock, by the start |
| FunFrock | the game's (BODY 313), on foot, unmasked | beside his black car on the grid |
| The Dark Monk, a Sup priest, the Franco priest | their cars (built, the statue's unlock) | the grid |
| An Esmer | a copy of scene 42 actor 6 | the dock: the Cup |
| Zoe, Raph, Mr. Paul, Baldino, Flick | copies of the game's own | the dock: the crowd |

**Walkthrough:**

1. **Landing,** from the teleporter, on the dock (built). FunFrock:
   > **FunFrock:** "We raced once, Twinsen, in your dream. You won, in your dream. This time it's real, and the wish is mine."
2. **The last call.**
   > **Baldino (radio):** "Twinsen. Everything I have is on that car. Best setup, and one last thing in the boot: a super jet-pack.
   > Use it on the spiral. And Twinsen... win."

   Car update: the statue track's best setup, and a super jet-pack in the item box (`start_item=`).
3. **The crowd.**
   > **Zoe:** "Go on, Twinsen! For Citadel!"
   >
   > **Raph:** "Beat his time like you beat mine!"
   >
   > **Flick** (who came over for the final): "Lyra's flag, my cheering."
4. **The race.** Round the statue in a spiral to its shoulders, a ring round its head and the bridge back down (built). The line-up:
   FunFrock (the one to beat), the Dark Monk, a Sup priest, the Franco priest. A loss:
   > **FunFrock:** "Again? I have all day."
5. **Won.**
   > **Lyra:** "Twinsen of Citadel Island wins the Two Worlds Cup!"
   >
   > **FunFrock:** "No! That wish was mine!"

   The Esmer:
   > **The Esmer:** "The Cup is yours, and its wish. What will it be?"

   A choice (`ask_choice`):
   - **"The statue gone, and FunFrock with it."**
     > **The Esmer:** "Granted."

     The statue goes (var 79 = 0) and `change_cube(95)` shows Celebration as it was. FunFrock is gone with it.
   - **"Another race. Right now."**
     > **FunFrock:** "Ha! You'll regret that."

     The race again, then the same choice. Choosing this a second time ends on FunFrock's laugh and the credits.
6. **The dock, after.**
   > **Zoe:** "Home, Twinsen?"
   >
   > **Raph:** "Only if you let me win sometimes."
   >
   > **Baldino:** "I'm keeping the car. For science."
   >
   > **The Esmer:** "Same time next year? We hear there's a temple, and a very long table..."

   `inc_chapter()` -> 15. `the_end()`.

---

## What this adds to the plan's list of things to build

- **The companion Zoe:** an actor of Zoe's entity in each start scene from chapter 2, made to follow Twinsen (`MOVE_FOLLOW`).
  - She stands beside Twinsen whenever a scene loads, and has her hint lines.
  - Her lines inside others' conversations are `message_obj(ZOE, ...)`.
  - Hidden while Twinsen drives; the pit crew takes over in the race.
- **The pit crew:**
  - The engine draws Zoe in the pit box during a race (as the marshal is drawn), with an idle animation and a "wave in" animation.
  - Pit service: under 20 km/h through the box gives a fuel can, undoes a jolt, says one line; once a lap.
  - New keys `pit_crew=` (her entity, body, animations, the box) and `pit_line=`.
- **Two-player story:**
  - The second process plays Zoe. In races she's on the grid in Zoe's car; on foot she walks.
  - The first process owns the story; the second follows its scenes and chapter.
  - Built on the two-player mode that exists.
- **The radio:**
  - At each arrival an invisible helper rings the radio and waits for `use_inventory(26)` (20 seconds, then it answers itself).
  - Then Baldino's line, and the car update: the track's best setup for the story's race (`UseTrackSetups` in the story's race files),
    and `start_item=` where the update puts something in the boot.
- **The flags:** the story's chapter is the game's `chapter()` (`inc_chapter()` at each win); the rest as in the table above.
- **About 230 new lines,** each in six languages.
