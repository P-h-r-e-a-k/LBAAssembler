# Little Big Racing Adventure: the walkthrough script

The user, 2026-11-03: "Further flesh out the story line we're creating. Work out each scene, each actor and their dialogue. We don't need to
worry about existing quest flags, we can just reuse them instead of avoiding them. Give me a detailed walkthrough style script. Other
ideas I've had, to reuse the radio mechanic that Baldino uses to phone Twinsen perhaps with specific car updates for each track, and
perhaps Zoe acting as pit crew, we could have her as a second playable character. If only a single person is playing Zoe can just follow
Twinsen around. Twinsen is still the main character and we want to avoid character switching."

This is the story as the player meets it: where each chapter happens, who is there, what Twinsen does and what everyone says. The plan
([LBRA_STORY_PLAN.md](LBRA_STORY_PLAN.md)) has the mechanics: the script templates, the race files' keys and what has to be built. Where
the two differ, this script is the newer: it reuses the game's own flags (below), adds the radio and gives Zoe her part.

Nothing here is built yet beyond what is marked **(built)**. Names in *italics* are placeholders for the user to change.

The same story as a player's guide, step by step with every line and no build notes: [LBRA_WALKTHROUGH.md](LBRA_WALKTHROUGH.md).

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
  - **In races she is not on the grid.** In story mode only Twinsen races, with one player or two (the user, 2026-11-03). Player 2's
    Zoe is the pit crew in the pit box: Twinsen's pit stop is hers to give. She presses Action as his car comes through the box, and a
    stop given on time is the same service as the one-player pit stop. She can't drive a car in a story race.
  - **On foot** she walks herself, wherever she likes. The story's people talk to Twinsen only; Zoe can talk to anyone for her hint
    lines, and Action at a pickup works for her too, so she can fetch while Twinsen talks.
  - Racing each other is for the Track select races outside the story, where two-player mode is as it is today.
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

### The magic slate

The user, 2026-11-03: "The magic slate from the original game we could re-use this but instead have it show maps of our race tracks
instead."

The **Magic Slate** (item 28, the game's own) is the Cup's map book. The Esmer herald gives it to Twinsen in chapter 2. Each page is
the map of one of the Cup's tracks, seen from above: the island's outline, the road, the start line, the checkpoints in order and the pit
box.

- **A page per track, added on arrival.** When Twinsen reaches a chapter's island, its track's page is added and the slate's icon shows
  in the corner (the game's `memo_ardoise(page)`, which already does both), just after Baldino's call. The pages build up in story order,
  so the slate also shows how far through the Cup he is.
- **Reading it.** Used from the inventory, it opens on the newest page; left and right turn the pages, as the game's slate does.
- **Two players:** the same slate; either can open it.
- The original game's slate pages (its plans and memos) are not used.
- **Two hidden pages** (the user, 2026-11-03: "a hidden map file in the museum showing construction blue prints that Twinsen can re-visit
  after collecting the magic slate to get the Polar Island track for his slate. We can also hide a race track magazine or design sheet
  somewhere for Twinsen to collect the Citadel 1 track. Perhaps it could be revealed in his neighbour's house if Twinsen gives him the
  gallic acid"). The dream and the storm track come before the slate, so their pages are found, not given; both are optional and on
  Citadel:
  - **Polar Island, the museum's blueprints** (chapter 1 to see them, chapter 2 to copy them): Q_BLUEPRINT, var 231.
  - **Citadel 1, the neighbour's design sheet**, brought out with gallic acid (chapter 1 to hear of it, chapter 2 to read it):
    Q_SHEET, var 232.

  With both, the slate has all fifteen tracks. A hidden page goes in at its own place in the book, before the town circuit, so the
  pages stay in story order.

**To build:**
- **The pictures.** Fifteen maps, one per story track (thirteen given on arrival, the dream's and the storm's found), made by the
  editor from each track's plan: the race-track mode already draws
  the road, start and checkpoints over the island. Each is a 640x480 picture with its palette in `SCREEN.HQR` (a slate page is entry
  `n * 2`, its palette `n * 2 + 1`), added at the end of the file under new page numbers.
- **The engine.** The slate holds 5 pages (`MAX_ARDOISE`), and a saved game stores exactly 5. That goes up to 16 for the story, with the
  saved game keeping its 5 retail bytes where they are and the extra pages stored with the race mod's own saved state, so old saves
  still load. The game's `memo_ardoise` adds a page at the end of the book; for the two hidden pages to sit in story order, the book is
  kept sorted by page number (the pages numbered in story order).

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
| var 28 / item 28 | the magic slate (its plans and memos) | the Cup's track maps, given in chapter 2 |
| var 79 | the statue risen | the statue risen, by the palace's race (built) |
| var 124 | the souvenir seller has told | the seller has told (chapter 6) |
| items 7, 10, 11, 13, 15, 16, 18, 20, 24, 30-35 | their own | as the plan's item table |
| vars 200-209 | the race mod's own (built) | as built |
| vars 212-224 | free | each chapter's quest steps (the plan's 3.1) |
| var 229 | free | DREAM_CAUGHT: Twinsen caught FunFrock in the dream (chapter 0) |
| var 231 | free | Q_BLUEPRINT: the museum's blueprints, the Polar Island page (chapters 1-2) |
| var 232 | free | Q_SHEET: the neighbour's design sheet, the Citadel 1 page (chapters 1-2) |
| var 233 | free | the Esmers' landing speech heard (chapter 2: it isn't said again) |
| var 17 / item 17 | the gallic acid (restores old documents; in the game it brings out the neighbour's map) | the same: it brings out the neighbour's design sheet |

### Everyone else on Citadel (built)

The user, 2026-11-03: "Every NPC we leave in the game should have updated text lines to suit our new mod." Citadel's 40 scenes have 120
people who speak, saying 328 texts. Their scripts still run as the game's (who stands where, who answers what and when), but 134 of
those texts are retold for the story (`Terrain/CitadelRetell.cs`, the lines in `CitadelRetell.Lines.cs`, six languages):
- **Twinsen's question to everyone** after the Wizard ("Do you know where I can find Raph the lighthouse keeper?") is now "Have you seen
  a thief with a pair of driving gloves?", so everyone's answer about Raph became a sighting of the thief, most of them pointing at the
  harbour.
- **The storm** lines know about the race track in the rain (the taxi, the tourist, the mother, the schoolchildren, the cow).
- **The fine-weather** lines know the Esmers built a circuit in one night and put the Tavern back (Tim, Luc, Pat, the old man, the
  drunk, Bazoo, the taxi, the lovesick boy, the Wizard).
- **The neighbour** has the faded design sheet instead of the sewer map, and **the sewer painter** follows the thief's footprints.

Not retold: prices, shop menus, greetings, "It's locked.", the jukebox, and the game's lines from later chapters, which the story never
reaches on Citadel.

The new lines are texts at the end of Citadel's file, so the CD voices (which go by a text's place) don't speak old words over them; the
game's texts that nothing says any more keep their place but lose their words, which keeps each language's file well under its 64 KB
limit (German 34 KB after).

### The 1996 demo's sewers

The user, 2026-11-03: "The Demo 96 files that we had, I want to pull the sewer files from there and have them in the sewers with our
racing mod. When I tested them previously the scripts weren't functioning correctly so this'll need additional analyses before we can
correctly pull them in."

The 1996 demo's sewers are its scenes 17 and 18: Citadel Island sewers that the retail game doesn't have (the painter repainting the
signs, barrels, gargoyle pillars). In the story they're where the thief runs in chapter 1. Retail LBA2 can't add scenes, only replace
them, so they take the retail sewers' slots, scenes 17 and 18.

**Done (the analysis, [LBA2_1996_DEMO.md](LBA2_1996_DEMO.md#the-demos-script-language)):**
- Why the scripts didn't work: the demo's script language isn't quite the retail one. Its `SAMPLE` instructions take a one-byte sample
  number where retail takes two, and it has five instructions of its own. Run as retail, the byte after a sample number was read as an
  instruction, and everything after it went wrong.
- The demo's language is now in the editor (`Opcodes.Demo96`). The 1996 demo's converter translates every script into the retail
  bytecode, and the sewers run in the retail engine: the painter walks his track and his checks run every frame.

**To do, to put them in the race mod's sewers:**
- **The interior.** The demo sewers are drawn with the demo's Citadel-interiors block library: 97 blocks between the two scenes. The
  retail game doesn't have that library. Only 3 of its blocks are also in the retail one, and retail library 1 has 219 blocks already,
  with room for 36 more at most (a grid's block number is one byte). So the demo's library goes in as a library of its own: a new
  `LBA_BKG.HQR` entry after the 18 retail ones, with the bricks after it moved up one (the header says where they start).
  - Its bricks: 1,020 of the 1,072 its blocks draw are retail bricks under other numbers, and they are renumbered. The 52 the retail game
    lacks go into the brick table too.
- **The actors' bodies and animations**: the demo's entities and animation numbers mapped onto the retail ones (the painter, the hero's
  animations), or the demo's bodies added as the race mod adds its own.
- **The sounds**: demo samples 54, 35 and 41 are retail samples 126, 617 and 501 byte for byte. 118, 52 and 37 are the demo's own: added,
  or the nearest retail ones.
- **The texts**: the painter's lines (demo texts 236, 237, 383) added to the race mod's Citadel texts under free numbers, and the
  scripts' `message()`s renumbered.
- **The doors**: the demo sewers' cube changes go to the demo's 17, 18, 21 and 34. They are pointed at the retail scenes the race mod
  uses: the harbour hatch (chapter 1) and the quay ladder.

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

The game opens on a race, a dream one: Twinsen against FunFrock. FunFrock is off first, and whether Twinsen catches him or not, the
dream ends the moment the race does.

1. **Opening.** The new story begins in the dream (built). Twinsen stands on the dock at Polar Island beside his car. FunFrock is
   already in his, the engine roaring.
   > **FunFrock:** "Still dreaming of beating me, Twinsen? Dream on. I'll even give you a head start... no, on second thoughts, I'll
   > take it!"

   His car pulls away up the island while Twinsen's is still on the dock: **FunFrock's head start** (new: he leaves a few seconds before
   Twinsen's start, as a ghost car is let go early; *8 seconds* to begin with, to be tuned so that catching him is possible but hard).
   > **The grobo:** "He went that way! The big man in the big car! Take the digger key, quick!" *(built: PolarDream.ChaseGroboText)*
2. **The chase.** Twinsen chases FunFrock up the island, round the loop and over the jumps, to the leap onto the rocky peak (built).
   **The race is run once.** Win or lose, it ends at the peak (new: today a loss runs the dream again).
3. **The finish.** As Twinsen's car leaves the lip, the white fade begins (built). What is said over it depends on the race:
   - **Twinsen caught FunFrock** (crossed the peak first):
     > **FunFrock (fading):** "Impossible! Nobody catches me... nobody..."
     >
     > **Sendell (unseen):** "Two worlds will race, Twinsen. And at the end of it, he will be waiting."
   - **FunFrock got there first:**
     > **FunFrock (fading, laughing):** "Too slow, Twinsen! Always one step behind!"
     >
     > **Sendell (unseen):** "Two worlds will race, Twinsen. You will have your chance again. And at the end of it, he will be
     > waiting."
4. **Waking.** As soon as the fade is white, Twinsen is in his bed (scene 0): Zoe shakes him awake (built) and kisses him (built).
   One line of hers first, depending on the dream:
   - caught him: > **Zoe:** "You were smiling in your sleep. And steering."
   - didn't: > **Zoe:** "You were shouting in your sleep. Something about a big man in a big car."

   Then her opening line (built, chapter 1's first line) follows.

**Flags:** var 205 (built). A new flag, DREAM_CAUGHT (var 229), is 1 when Twinsen beat FunFrock to the peak: it picks the lines above and,
later, FunFrock's first line in the final (chapter 14). The wake sets
`chapter()` to 1.

**Built:** the wake on a loss as on a win (the dream's race file: `dream_any_end=1`, the dream never run again); DREAM_CAUGHT set only
by a real win (`won_set=229 1`, RACEMOD.CPP: win= applies either way in such a dream); Zoe's two waking lines by it, and FunFrock's at
the final. **To build:** FunFrock's head start (his car let go before Twinsen's start); FunFrock's fading lines over the white.

---

## Chapter 1 — The storm (Citadel 1) **(built, with the thief's quest new)**

**Where:** Citadel Island in the rain (Citadel 1, the storm island, all chapter long: the Esmers don't land until the next morning):
- Twinsen's house (scene 0)
- the Weather Wizard's tent (scene 21)
- the storm track's start, where the Tavern stood (scene 42): the Tavern was taken down to make room for the track (its two buildings are
  gone from the Citadel 1 build; Tim stands on its doorstep, and Raph's car parks across the road from him, past the line by the sea,
  out of earshot of Tim, Mr. Paul and Flick -- parked beside Tim, Raph's lines came when Twinsen talked to Tim)
- the harbour (scene 43)
- the sewers under the harbour (the 1996 demo's sewers, scenes 17 and 18: see [The 1996 demo's sewers](#the-1996-demos-sewers) below)
- the lighthouse (scene 46)
- the Tralu's caves (scenes 2, 20 and 19; their mouth in the cliff below the Wizard's tent, scene 45): the regulars' secret bar, the
  Back Room, in the second hall (scene 19); the third hall (20) is the 1996 demo's

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Zoe | the game's (scene 0 actor 4) | home |
| The Weather Wizard | the game's (scene 21 actor 2) | his tent |
| Raph | built (a copy of scene 2 actor 2) | lapping the storm track, then by the start line |
| Mr. Paul | built (a copy of scene 7 actor 2) | the start line |
| Tim the waiter | the game's (BODY 40) | on the Tavern's old doorstep, with a tray and nowhere to serve |
| The thief | the game's (BODY 117), copied | behind the harbour warehouse, then the sewers |
| The sewer painter | the 1996 demo's (demo scene 17 actor 7) | the sewers, repainting the signs |
| Flick, the marshal | built (the race's); an actor on foot (new) | her plinth by the start line |
| Luc, Pat, Fab, the visitor, the drunk rabbibunny | the game's Tavern people (scene 3), copied | the secret bar (scene 19): Luc behind his counter of crates, the others sitting at the massive table |
| Tim, again | the same Tim | serving in the secret bar from the umbrella given back until Twinsen is tired -- off the doorstep meanwhile |
| The Tralu | the game's (scene 2 actor 3), no longer the boss | the secret bar (scene 19), at the round table by the north wall with a big tankard (gone from his own hall, scene 2) |

**Walkthrough:**

1. **Home.** Zoe's opening (built):
   > **Zoe:** "Twinsen, I am sick of all this rain! Go and find the Weather Wizard and get him to fix it!"

   And a new line after it:
   > **Zoe:** "Oh, and you left your driving gloves on the seat of your car, under the carport. This morning they were gone!
   > Somebody's been light-fingered again."

   Q_THIEF = 1. The attic shelf shows the darts again, not the gloves.

   In the yard, Zoe comes out of the door after him (the game's, scene 49 actor 4):
   > **Zoe:** "I'm off to watch the race practice. Don't forget that you can change the view by pressing "ENTER"."

   and goes off up the path north, towards the storm track, out of the yard (not the game's walk to the Dino-Fly; she isn't in the
   yard again this chapter: var 40 14 -- RaceTrackStory.Chapters.cs, YardZoeLeaves).
2. **The Wizard's tent.** (built)
   > **The Weather Wizard:** "I was going to clear the rain, but I need to do it from the top of the lighthouse, and that rascal Raph
   > is too busy at the race track and refuses to let me."

   The arrow points at the storm track's start line.
3. **The storm track.** Raph laps the track in his car until Twinsen stands on the start line. Then he screeches up (built):
   > **Raph:** "Hey! Get off the track, Twinsen! Are you trying to get yourself run over?!"

   Lapping, he does the same for the townsman who roams the track (scene 42's actor 4, entity 101: the race file's parade_walker=,
   RACEMOD.CPP ParadeStep -- on the road by the lanes' room, ahead within his braking; a text box for Twinsen on foot within 12 cells;
   not again for 45 s), then drives on (built):
   > **Raph:** "Hey, you! Get off the track! Are you trying to get yourself run over?!"

   Then Raph (built):
   > **Raph:** "I'm having too much fun, it can wait. I'll tell you what: if you can beat my time on the track, then I'll come with you
   > to the lighthouse. Mind you, you'll need a car and some driving gloves to take part."

   Mr. Paul (built):
   > **Mr. Paul:** "Sorry Twinsen, for safety reasons you'll need some driving gloves to take part."

   Flick, the marshal, on her plinth (new):
   > **Flick:** "I'm the School of Magic's assistant, but on race days I'm the marshal. No gloves, no flag, I'm afraid. I saw a fellow
   > with a pair stuffed in his pocket go past where the Tavern used to be."
4. **The Tavern's old spot.** Tim stands on the doorstep that is all that's left of the Tavern, in the rain, with a tray:
   > **Tim:** "Twenty years I served drinks here, Twinsen, and now look: a car park. They needed the room for the track, they said."
   >
   > **Tim:** "A fellow ran past with gloves in one hand and a customer's umbrella in the other. Towards the harbour! If you catch him,
   > I want that umbrella back. Not that I've anywhere to put it."

   Q_THIEF = 2. The arrow points at the harbour.
5. **The harbour.** The thief is behind the warehouse. When Twinsen comes within three cells:
   > **The thief:** "Can't catch me!"

   He runs round the warehouse and drops down the sewer hatch on the quay (Q_THIEF = 3). The hatch is open now; Twinsen follows him down.
6. **The sewers** (the 1996 demo's, scene 17 then 18). Down the ladder the thief is gone, splashing away somewhere ahead. The painter is
   at work on a sign (his walk and his paint pot are the demo's own script):
   > **The painter** (Action beside him; the demo's own line, demo text 236): "What a drag these sewers are! It stinks in here and I've
   > still got a whole bunch of signs to update. Phew! I'm up to my neck in it!!"
   >
   > **The painter** (asked again; new): "A fellow in a hurry? He went through to the far tunnel, and trod on my wet paint. Follow the
   > footprints."

   Through the far tunnel (scene 18) the footprints lead to a dead end under a grating, where the thief has nowhere left to go
   (Q_THIEF = 4). Action beside him:
   > **The thief:** "Fine, fine! Here are your gloves. Nice gloves, too. See you on the town circuit, if they ever build one."

   Found: Driving gloves (item 7). He drops the umbrella and is off up the ladder. Q_THIEF = 5. The ladder under the grating comes up on
   the quay.
7. **The umbrella (optional).** Picked up: Customer's umbrella (item 20). Back to Tim:
   > **Tim:** "Our customer's umbrella! I'd stand you a drink on the house, if I still had a house. Here." *(10 kashes)*

   **Two optional visits, any time in the chapter** (the slate's hidden pages, finished in chapter 2):
   - **The museum** (scene 15, by the rampart's lift). The guard at the desk lets the Cup's winner in free: "yes" to his ticket opens the
     hall's door and takes nothing (the game's 15 kashes were more than chapter 1's Twinsen has -- 2026-11-07). Miss Bloop, the curator, in the hall:
     > **Miss Bloop:** "Mind the cases, Twinsen. And the floor. And, well, everything: it's all very old."

     On the hall's north wall by the corner, over a sideboard, a picture of Twinsen holding a cup (a copy of the hall's portrait, its
     canvas painted again: Citadel/CitadelMuseum -- since 2026-11-07 head and shoulders, the gold cup held in front of his tunic, on a
     dark red ground, every shape outlined: the first, the whole figure with the cup over his head, was too small to make out). Action
     in front of it (an unseen helper under it, within 1300: TrophyPicture, six languages):
     > *A portrait in a gold frame. Its plate reads: TWINSEN, HERO OF TWINSUN. The painter has put a winner's cup in his hands... a cup
     > he has never won. Not yet.*

     The visitor's line (text 21 retold): "you look like the guy in the picture! The one with the trophy!"

     In the hall, under the window, a display case like the one with the old book (the museum's own bricks, Citadel/CitadelMuseum):
     under its glass a sheet of blueprints (Action by the case):
     > *A file of blueprints: "POLAR ISLAND CIRCUIT. Construction plans. Not to be built." A road from the dock round the island's rocky
     > head, a jump, a zigzag climb up the terraces, a high road along the cliffs, and a ramp leaping onto the peak.*
     >
     > **Twinsen:** "That's the track from my dream! Every bend of it. If only I had something to copy it onto."

     Miss Bloop, asked about it:
     > **Miss Bloop:** "That old thing? A man with glasses and a very big car left it here years ago. He said it was worth a fortune one
     > day. It's never been worth anything to me, but I keep it, because I keep everything."

     Q_BLUEPRINT = 1.
   - **The neighbour** (next door to Twinsen's house). The game's own neighbour, at his table with a sheet of paper and a magnifying
     glass:
     > **The neighbour:** "Hello neighbour."
     >
     > **The neighbour:** "Somebody pushed this under my door the night they built the storm track. Drawings, I think, but the rain got
     > to it and the ink's all faded. With a little gallic acid I could bring it back. There's none left on the island, mind: the
     > pharmacy's waiting for its delivery."

     Q_SHEET = 1.
   - **The secret bar** (built 2026-11-06; an easter egg: nothing points the way). The hints are only that Luc is nowhere to be seen:
     Tim, asked again on his doorstep (once a visit while the thief is out, and from the umbrella back):
     > **Tim:** "And don't ask me for a drink, Twinsen: the mayor's banned the stuff while there's a race track on the island. No
     > drinking and driving, he says. Funny, though... nobody's seen Luc all day. Nor the Tavern's bottles. Not my business. I'm only
     > the waiter."

     and the mushroom man by the Wizard's tent (once a visit):
     > **The mushroom man:** "Have you seen Luc, from the Tavern? Nor has anyone, since the mayor banned drink. And half his regulars go
     > missing of an evening, and come home singing. Funny, that. The mushrooms hear everything."

     The bar is the Tralu's caves' second hall (scene 19), reached through the cave's mouth below the tent, its first hall (2) and the
     third (20, the 1996 demo's: the ladder in its pool, the lever, the gate -- the pool's front row the demo's deadly water, its two
     rows at the ladder's foot water to wade in, Demo96Rooms.Wading: a run and a jump over the front row, and up the ladder). The
     build makes the hall bigger and furnishes it
     (`Terrain/Citadel/TraluBarRoom.cs`): the long back wall moved out, a massive old wooden table (the caves' own, three end to end)
     with benches, barrels for round tables, candlesticks; the hall's creatures and its two barred doors go. Luc behind a counter of
     crates; Pat, Fab, the visitor and the drunk rabbibunny sitting at the table; Tim serving there from the umbrella back until
     Twinsen is tired (and off his doorstep meanwhile); the Tralu, come over from his hall next door (2026-11-07, the user: "Move him
     into the bar area ... with a large tankard of beer"), standing at the round table by the old room's north wall, his tankard on
     it (a body of the build's, TraluBarRoom.InstallTankard: the props' entity 112, body 47):
     > **Luc:** "Shh! Come in, Twinsen, and keep it down. Welcome to the Back Room: the only bar on Citadel the mayor doesn't know
     > about. A proper table, and the Tavern's last bottles... And if you're racing today, it's lemonade for you."
     >
     > **The drunk rabbibunny:** "The mayor shays no drinking while there'sh a race track. [hic!] Sho I'm not drinking. I'm...
     > sitting. At a table. With a mug. [hic!] Don't tell the mayor."
     >
     > **Pat:** "No Tavern, no stage... so we brought the band down here. Listen to that echo! Every beat comes back three times.
     > Raph's meant to be playing with us, but he won't come off that track."
     >
     > **Fab:** "And the big fellow from next door came round to complain. He's still here. Third tankard."
     >
     > **The visitor:** "Sorry, I'm not from here. Back home we drink in bars. Here you drink in a cave, in secret, in a storm, at a
     > table fit for a giant. I love this island."
     >
     > **Tim:** "There you are, Twinsen! My shift on the doorstep's over, so I'm on duty down here. Old habits: I can't sit in a bar
     > without carrying a tray. Lemonade?"

     Luc, the first time, after his welcome (the cave's var 60 to 1; till the gate is open he says this again, then only his welcome):
     > **Luc:** "And between us: regulars don't come the long way round. There's a back gate, up from the old hall next door, and a key
     > to it hidden where Sendell keeps an eye on it. You didn't hear that from me."

     **The back gate** (built 2026-11-06, `Terrain/Citadel/RaceTrackStory.TraluCaves.cs`): the gate from the Tralu's hall (scene 2) to
     the bar stands on a ledge six layers up, which in the game had no way up (Twinsen came down off it and was stuck: the hall's other
     way out was the barred door only the Tralu's key opened). The build puts stairs up the ledge's east side (`TraluBarRoom.InstallStairs`;
     rebuilt 2026-11-07, the user: "flat against the wall ... as wide as that side of the gate's block ... a turn in the flight"): three
     steps along the hall's back wall, the ledge's side's whole width (z 19-20), to a landing (x 13-14), and three more turning south
     off it into the hall, solid stone down to the floor; the chain on the wall over them taken down (his head ran into it);
     the game's barred door (actor 8) moves onto the ledge across the gate's arch, drawn as scene 19's barred doors were, and the game's own
     key script opens it (var 60 to 3, gone for good); the way from the first hall into the Tralu's hall is open. The key is under the sign
     of Sendell on the ledge's face: Action there once Luc has told, while the gate is shut and Twinsen has no key, and a key jumps out.
     Come from the bar (up four new steps east of the table onto the corridor's landing, the hall's stalagmites there taken out, and
     south down the corridor: its two barred doors are gone), Twinsen lands on the near side of the gate and goes down the stairs; with
     the key, the cave's mouth, its first hall, the Tralu's hall, the stairs and the gate are the bar's quick way back.

     The Tralu, at his tankard (his own hall next door, scene 2, is empty) -- no longer the boss, he doesn't fight:
     > **The Tralu:** "Grrrr... A hundred years in these caves, with nothing but the drips for company. Then the rain, then that race
     > track, and now the whole Tavern's moved into my back room, with drums! A Tralu can't hear himself growl. ...So I may as well
     > have a tankard. And stop offering me peanuts."

     In chapter 2 the regulars are back in the Tavern and the Tralu, still at his tankard, is glad of the quiet: "...The whole bar to
     myself, and my tankard..."

     **The spider cave** (2026-11-07, the user: "It's the cave that has a large spider in it"): its lower mouth in the cliff south of
     the sign of Sendell (scene 47's door into 51) was taken out by the build, the town circuit's road running through the cliff there
     in the fine weather; in the storm the cliff and the mouth are the game's. Now shut only while the town's file is shown (door_off),
     its zone reaching 256 out in front of the cliff's face (it lay 8 behind it), and the way out landing Twinsen in front of the
     opening (RaceTrackStory.SpiderCaveMouth).
8. **The race.** Raph now tells his time (built):
   > **Raph:** "A car and driving gloves: you're all set, Twinsen! My best lap is ## seconds. Beat that, and I'll come with you to the
   > lighthouse."

   Flick drops the flag. One lap against Raph's time (built).
9. **Won.** (built; reworked 2026-11-07, the user's fix list of 2026-11-06, items 1, 10, 14, 15 -- RaceTrackStory.Apply step 4,
   RaceTrackStory.Evening.cs) Zoe has watched the practice from beside the start line since she left home (the yard's Zoe walks all the
   way out of the yard, YardZoeLeaves; scene 42's Zoe, a copy of scene 0's, there while var 40 > 2 and Raph not yet won over: "show
   him how it's done" / "go and beat his time"). Raph's time beaten (BeatVar 1): Raph out of his car -- the car out of sight and in no
   one's way while he is out of it (its life script; RACEMOD.CPP RaceMod_NoCollide: an invisible parade car) -- and over beside Zoe.
   Twinsen must talk to him (Action):
   > **Raph:** "You beat my time, Twinsen! A deal is a deal: I'm off to the lighthouse. See you there!"

   He runs off down the road towards the lighthouse (track points along the storm track's road, out of the cube's reach) and is gone;
   Zoe joins Twinsen, the two walking together (behaviour 5, her own body gone into theirs):
   > **Zoe:** "Twinsen, you did it! Raph's on his way to the lighthouse, and the Weather Wizard is meeting us there. Come on, let's go
   > together!"

   They walk there on foot (no change of scene any more): scene 42's south edge into the yard (49 -- its own walk-in for the game's
   walk to the lighthouse no longer puts them on its path, point 15), west into 46, round the back of the lighthouse's hill and up to
   its door. Flick after the race: "Raph's lap record, beaten in the pouring rain!..." (before it, with gloves: "Drive up to the line
   whenever you're ready...").
10. **The lighthouse.** (built) The Wizard casts his spell from the top. The thunder stops; the rain thins to a drizzle. **The island
    stays Citadel 1** and nobody lands yet (new: the spell no longer moves the game on to chapter 2, below).
    > **The Weather Wizard:** "There. A storm this old doesn't clear in an evening, mind you: give it the night. By morning you'll see
    > the sun."
    >
    > **Raph:** "Back to my car, then, and off to bed. Best race I've lost in years." (said by Raph, actor 5 -- his fiancee, actor 6,
    > said it until 2026-11-07)
    >
    > **Zoe:** "Something's up there, Twinsen. Above the clouds. Lights. Did you see them?"

    (2026-11-07, items 11, 15, 16, 21) No change of scene to the storm track's any more: Twinsen and Zoe are left on the top, each their
    own again (the couple's body, actor 12, gone; Zoe a copy of scene 0's, var_cube 44), Twinsen in body(1) -- his tunic once he has
    it, the game's own after a cutscene (body(0) took it off him for the rest of the story) -- and the scene's ways out open again
    (set_change_cube(1, 1)). Raph walks off down the hill and is gone past the bend (on a later visit he's not at the door: back with
    his car at the track, the one Raph); the Wizard: "And now, home to my tent. Mind the puddles on your way down, Twinsen!" -- and home
    in a flash (not at the door again either). Going down (the climb's Action at the lamp room's door) or talking to her, Zoe asks
    (Q_EVENING, var 58, 1 once answered):
    > **Zoe:** "Are you coming home, Twinsen?" -- "Yes, let's go home." (arm in arm, down and home together) / "I'll come home
    > soon." (**Zoe:** "Don't stay up here too long, then. I'll light the candle by the bed for you." -- she goes down ahead)
11. **Tired.** Everyone Twinsen talks to on the way home (built):
    > "You look tired, Twinsen. You should go home and have a nap."

    Tim, on his doorstep, if Twinsen passes -- and then he walks off along the road and is gone for the night (Q_EVENING 2):
    > **Tim:** "Lights over the sea, at this hour? First a race track, now this. I'm going home."

    Home is the house at night (scene 193 -- the retail demo's copy of the house, unused -- Citadel/CitadelNightHouse: scene 0's grid in darkened bricks, the candle by the bed and its
    light kept; the day's house sends Twinsen there while he is tired. In a build of all fifteen islands the darkened bricks don't fit
    -- LBA_BKG.HQR is at the engine's brick limit -- so the scene is made on the day's house's own grid, not dark, but still with Twinsen
    coming in by the front door, 2026-11-07; before, that build left the demo's copy as it was and he arrived in the bedroom). Zoe is there, come in with him or waiting by the dining table:
    > **Zoe:** "Bed, Twinsen. The Wizard said by morning, and morning comes quicker when you're asleep."

    The bed, its zone round all three open sides (it was beside the bed's foot, out in the room -- RaceTrackStory.cs, BedSides):
    > **Twinsen:** "What a day! A good night's sleep..."

    Into bed (the screen dark a moment), then:
    > **Zoe:** "Goodnight, Twinsen. Don't forget the candle." **Twinsen:** "Goodnight, Zoe."

    The candle out (the screen goes black): the night, and the morning in the day's house (chapter 2, step 1). The alien with his two
    dogs by the Tavern is there from chapter 2, not from the spell (scene 42's actors of entities 30 and 32).

**Flags:** var 51, 56 (built); Q_THIEF (213, now 1-5); items 7 and 20; Q_BLUEPRINT (231) and Q_SHEET (232) to 1 (optional). `chapter()` stays 1 to the end of the chapter: it goes to 2 in
the morning (chapter 2, step 1), not at the spell.

**To build:**
- The spell without the chapter change: the lighthouse's script sets var 51 (the storm plot done) but not the game's `inc_chapter`, and
  var 70 (the Esmers landed) waits for the morning too. Citadel 1's weather is the island's own (CITADEL.ILE is the storm island), so it
  keeps raining lightly until the morning, which is what the Wizard's line says.
- The bed's line, the night's fade, and the wake in the morning (chapter 2, step 1).
- The sewer hatch on the quay, open from Q_THIEF = 3; the thief's chase through the demo's sewers (his track in scenes 17 and 18); the
  footprints; the ladder up to the quay.

---

## Chapter 2 — The town circuit (Citadel 2)

**Where:** Citadel Island in fine weather, the next morning (Citadel 2: from the moment Twinsen steps out of his house, the island is the
fine-weather one, CITABAU, with the Esmers' town circuit on it):
- Twinsen's house (scene 0)
- the Esmers' landing place, by the town circuit
- the Tavern, back where it stood (outside, and its inside, scene 3)
- the yard north of the house, where the cars park
- the town circuit's start, near the Dino-Fly (scene 49)
- the harbour (scene 43)

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Zoe | the game's (scene 0 actor 4), then the companion | by the bed, then in the yard, fixing Luc's car |
| Luc | the game's Tavern boss, beside his barrel car (the character car) | the yard; once the wheel is on, behind his bar (scene 3); the grid |
| The Esmers | built (a copy of scene 42 actor 6, the game's landing) | their ship, by the town circuit |
| Tim the waiter | the game's (BODY 40) | the Tavern's door, then behind its bar |
| Raph | built | by the Tavern |
| Flick | the marshal (built), on foot | her plinth |
| The Esmer herald | a copy of scene 42 actor 6 | beside the start line |
| Mr. Paul | built | the start line |
| Raph, the Tralu, the thief | the race's (built) | the grid (Luc's barrel takes the sixth place) |
| Felix | the fisherman by the scrap (the landing crowd's actor, BODY 152; the user, 2026-11-09: "The man by the scrap is Felix the fisherman") | by the scrap heap at the harbour, the old tyre beside him |
| Mr. Bazoo | the game's shopkeeper (scene 14) | his shop: the tin of fishing bait on the counter |
| The bike taxi | the game's motorbike Rabbibunny (scene 48 actor 12) | at a stand of his own beside the town square, off the circuit |
| Mrs. Brune of the ferries | the game's (BODY 62) | the ticket office |
| The masked driver | the black car (new) | past the finish |

**Walkthrough:**

1. **The morning.** Sunlight through the window. Zoe wakes Twinsen again, as at the start of the game (the same shake, built), but this
   time she's excited:
   > **Zoe:** "Twinsen! Wake up! The aliens have landed! A big ship, right by the harbour road, and the whole island's out to look!"
   >
   > **Twinsen:** "Aliens? I was dreaming about... no. Never mind. I'm coming!"

   `inc_chapter()` -> 2 (the game's fine weather; var 70, the Esmers landed, = 1). Twinsen gets up; **the house door now leads out onto
   Citadel 2**: the sky clear, the storm track gone, the Esmers' new circuit round the town, and the Tavern standing again.
2. **The landing.** The Esmers' ship stands by the new circuit, islanders all round it. The Esmer speaks to Twinsen as he comes near (the
   game's landing speech, its end new):
   > **The Esmer:** "People from the planet Twinsun, we come to you in a spirit of peace. Thank you for clearing the rain which was
   > preventing us from landing."
   >
   > **The Esmer:** "We saw your little race track in the rain. Brave, but small, and it cost you your Tavern. So last night we built you
   > a bigger one, round your whole town, and we put your Tavern back where it stood, every stone of it."
   >
   > **The Esmer:** "And today we open a Cup for both of our worlds, Twinsun's and Zeelich's. The first race is here, on your new
   > circuit."

   The islanders around (each a line when Twinsen passes; the game's townsfolk bodies, each speaking in the colour the game gives its
   kind where it speaks -- the quetch and the grobo had colour 0, the palette's first row, and their lines typed out flashing through a
   rainbow; every person the story adds gets a colour, `RaceTrackStory.GiveVoice`):
   > **A Quetch:** "A whole circuit in one night! I went to bed with a puddle outside my door and woke up with a grandstand."
   >
   > **A Grobo:** "Did you see the Tavern? Back like it never left. Even the sign's been repainted."
   >
   > **A rabbibunny:** "They asked me where the bins went. Polite, these aliens."
3. **The Tavern.** Raph stands outside it, staring:
   > **Raph:** "I parked on this spot last night. Now there's a Tavern on it. Where's my car?"
   >
   > **Raph:** "...in the Esmers' car park, they say. With a wash and a polish. I could get used to aliens."

   Tim, at the door, then behind his bar inside (scene 3, the game's Tavern, open again):
   > **Tim:** "Twinsen! Look at it! The same bar, the same stools, the same stain on the ceiling... I asked them to leave that out, but
   > they said 'exactly as it was'. Come in, come in: the first drink in the new old Tavern is on the house."

   Then, if Twinsen gave the umbrella back in chapter 1 (and hasn't taken it since):
   > **Tim:** "And that umbrella you brought back is on its hook by the door. She never came for it. Take it, if you like: you've
   > earned it, and she'd only lose it again."

   If he didn't:
   > **Tim:** "And if you ever find my regular's umbrella, there's a hook by the door waiting for it."

   While Luc's barrel car is still a wheel short (Q_LUCCAR below 4):
   > **Tim:** "Luc's not in yet: he's out in the yard by your house with that barrel car of his, a wheel short."

   The umbrella hangs on its hook on the left side of the front door's arch (the side the camera sees; a hook and an umbrella of their
   own, `Citadel/CitadelProps`). Action by it (while it's back, Q_THIEF 6):
   > **Twinsen:** "Her umbrella. After yesterday, I'm not going anywhere without one."

   Found: the umbrella (item 20), gone from the hook. Once the wheel is on, **Luc is back behind his bar** ("Hello, Twinsen my friend",
   and his chapter 2 word of the life-points machines); after the race it's his round there (step 11).
4. **The house door again.** Zoe has gone ahead:
   > **Zoe** (at the door, if Twinsen goes home first): "There's a crowd at the new track already! Come on, I want a look... and bring
   > your toolbox. Somebody's going to need a mechanic today."
5. **The yard.** Luc, the Tavern's boss, stands by his car (his barrel on wheels, the character car), which has one wheel missing. Zoe
   is already under it with a spanner:
   > **Luc:** "They gave me my Tavern back, Twinsen, so I'm racing for the Tavern! Only... my back wheel came off on the way here."
   >
   > **Zoe:** "I can fit one, if someone finds a wheel. Felix at the harbour fished an old tyre out of the sea this morning."

   Q_LUCCAR = 2. The arrow points at the harbour. Luc and Zoe stand right outside the house's door: one Action is answered by one of
   them (the user, 2026-11-06: the wheel was told twice -- Luc answered, then Zoe the same Action).
6. **The start line.** The Esmer herald:
   > **The Esmer herald:** "Welcome to the Two Worlds Cup! One race on every island of Twinsun and Zeelich, and to whoever wins them
   > all, a wish. Any wish. The first race is here, on Citadel."
   >
   > **The Esmer herald:** "Every racer gets one of these. It shows the circuits of the Cup: a new one appears each time you reach its
   > island."

   Found: Magic Slate (item 28), its first page the map of the town circuit (see [The magic slate](#the-magic-slate)).

   Flick:
   > **Flick:** "Six cars on the grid or Mr. Paul won't start it. That's you, Raph, Mr. Paul, the Tralu, that thief again... and Luc,
   > if he ever gets his barrel back on four wheels."

   Mr. Paul:
   > **Mr. Paul:** "Rules are rules, Twinsen. Six cars."

   The gate says it too (`gate=214 4`).
7. **The bait.** Mr. Bazoo's shop (scene 14) sells a tin of fishing bait, any time (the tin on the counter's front: Action facing it):
   > **Mr. Bazoo:** "A tin of fishing bait costs 2 kashes." (the game's "No, I am not interested." / "OK, I'll buy it!")
   >
   > **Twinsen:** "Fishing bait. Somebody at the harbour will be glad of this."

   QBait (var 118) = 1. Asked again: **Mr. Bazoo:** "One tin's plenty, Twinsen: the fish round here aren't that hungry." (The inventory
   has no slot to spare -- its forty are the game's -- so the bait is the variable.)
8. **The harbour.** Felix, by the scrap, the old tyre beside him (a round rubber tyre on a rusty rim, `Citadel/CitadelProps`):
   > **Felix:** "See this? An old tyre I fished out of the harbour this morning, rim and all. Luc's been asking everyone for a wheel. It's
   > yours for a tin of bait: the fish won't bite on rust. Mr. Bazoo sells it in his shop."

   With the bait, at once:
   > **Twinsen:** "Here: a tin of bait, fresh from Mr. Bazoo's."
   >
   > **Felix:** "Deal! The tyre's yours. Mind, there might still be a crab in it."
   >
   > **Twinsen:** "A tyre. A bit rusty, but round."

   QBait = 2, the tyre gone from beside Felix; Q_LUCCAR = 3 if Luc has told (else it stays: the order doesn't matter). Asked again:
   **Felix:** "The fish are biting already. Good luck in the race, Twinsen!"
9. **Back to the yard.** If Twinsen comes with the tyre before Luc has told:
   > **Luc:** "A tyre! Twinsen, you read my mind: my back wheel came off on the way here, and I'm racing for the Tavern!"

   Zoe fits it in no time:
   > **Zoe:** "There. Good as new. Well, good as old."
   >
   > **Luc:** "Ha! First round's on me, both of you. After the race."
   >
   > **Zoe:** "And Twinsen, a parcel came for you on the morning ferry, from Baldino on the Desert. A portable radio! He says he wants
   > to hear how the Cup goes."

   Found: Portable Radio (item 26). Q_LUCCAR = 4. Luc goes back to his bar.
   > **Zoe:** "You drive, I'll keep the car in one piece. I'll be in the pit box by the start line."
10. **The first call.** The radio rings: "The radio is ringing. Answer it from your inventory." Twinsen answers.
   > **Baldino (radio):** "Hello? Twinsen? Can you hear me? Excellent, the radio works! Listen, I've been reading about this Cup. I
   > tuned your car before you ever knew it needed tuning: shorter gears for the town's tight corners. Try not to scratch the paint."

   Car update: the Citadel town circuit's best setup.
11. **The race.** The way from the yard to the start line comes onto the grid behind it (the town's roads, the lap's way down the
   circuit's east side). Before the race, Twinsen's qualifying lap: over the line the lap's way. Stopped at the line (or over it the
   wrong way), the car is put on pole and the screen says "Qualifying lap: over the start line!" (RACEMOD.CPP
   QualifyingArrival; the user, 2026-11-06: "arriving there in the car does nothing"); with the wheel not yet on, the gate's line
   instead: "Mr. Paul: six cars! Luc's a wheel short." Then six cars, three laps (built). Flick drops the flag. Beat
   Mr. Paul.
12. **Won.** A cut-scene (built: the race file's `win_stop=201`, RACEMOD.CPP): over the line the car brakes to a stop, Twinsen gets
   out, and his controls stay locked until Mr. Paul's prize has played (201 1 -> 2). The same `win_stop=` on the Desert, the Moon,
   Otringal, the Francos, the Elevator Platform, Wannies, the Mosquibees and the final (RaceCarEngineFile.WinStopVar); every story
   track's `once_won=1` keeps a won race from starting again at its start line. Mr. Paul (built):
   > **Mr. Paul:** "Well raced, Twinsen! You've won the prize: a ferry ticket. And the Cup's purse: fifty kashes, from the Esmers."

   Found: Ferry Ticket (item 13), 50 kashes.

   The herald:
   > **The Esmer herald:** "Twinsen of Citadel Island leads the Cup! Next: the Desert, the White Leaf Desert."

   **The masked driver:** past the finish, a black car stands with its engine running. When Twinsen comes near, it drives off and
   is gone.
   > **Raph:** "Did you see that black car? No number, no driver I could see. And gone before I'd blinked."
   >
   > **Zoe:** "Whoever it is, they weren't racing. They were watching."

   **Luc** (behind his bar): "First round's on me, as promised! To the Cup!"

   **The slate's two hidden pages (optional, before the ferry).** Once Twinsen has the slate (step 6), Zoe says, if he talks to her:
   > **Zoe:** "A book of race tracks... Twinsen, didn't you see a track in the museum? And our neighbour's been squinting at some
   > drawings all week."

   - **The museum's blueprints** (any time after step 6). The blueprints' display case again (or for the first time, with
     chapter 1's lines first):
     > **Twinsen:** "Now I've got something to copy it onto."

     He lays the slate on the blueprints, and the map draws itself onto a new page: the Polar Island track (`memo_ardoise`).
     > **Miss Bloop:** "Did that slate just copy my file? The Esmers have very clever toys."

     Q_BLUEPRINT = 2. The page goes in at the front of the book.
   - **The gallic acid** (after the race, with the purse). The pharmacy (scene 48's door, open again) had its delivery on the morning
     ferry. The pharmacist:
     > **The pharmacist:** "A flask of gallic costs 50 kashes." *(the game's own line)*

     A choice: buy it? Yes:
     > **The pharmacist:** "There. Mind, it's for old documents, not for engines, whatever the Desert racers tell you."

     Found: Gallic acid (item 17), for 50 kashes. (Without the purse, or with less than 50 kashes: "Come back when you've won
     something.")
   - **The neighbour's design sheet.** Twinsen gives him the gallic acid (`use_inventory(17)` beside him):
     > **The neighbour:** "Gallic acid! Let's see..."
     >
     > *He dabs the acid over the sheet. Lines come up out of the paper: a road round Citadel's town, the rampart walkway, a jump, a
     > pit lane where the Tavern stood. In the corner: "STORM CIRCUIT. Design sheet. Passed for safety: Mr. Paul."*
     >
     > **The neighbour:** "The gallic acid has revealed a text previously invisible. It's that race track they built in the rain! Here,
     > it's yours: I don't drive, and I'm not getting in a car with you."

     Item 17 = 0. The slate copies the sheet: the Citadel 1 page, second in the book. Q_SHEET = 2.
     > **Zoe:** "Two tracks we've already raced, and the whole Cup still to come. Your slate's filling up."
13. **The ferry.** Zoe comes along now (one player: she follows Twinsen):
   > **Zoe:** "Every race needs a pit crew. That's me. You drive, I'll fix."

   At the harbour, Mrs. Brune:
   > **Mrs. Brune:** "One ticket for the Desert... and the young lady goes free, she says she's crew."

   The ferry crossing (the game's own, built: the ticket) lands them on the Desert. `inc_chapter()` -> 3.

**Also (built 2026-11-07):** the bike taxi in the town square (scene 48) looped the square across the circuit; from chapter 2 he waits for
fares at a stand of his own off it (his ride as before).

**Flags:** `chapter()` 1 -> 2 at the morning wake, var 70 with it; Q_LUCCAR (214), QBait (118: 1 the bait bought, 2 traded for the tyre),
item 20 (the umbrella off its hook, optional), item 26 (the radio), item 28 (the magic slate), item
13 (built), the purse (50 kashes), Q_BLUEPRINT (231) and Q_SHEET (232) with item 17 (optional), `chapter()`
2 -> 3.

**To build:** the morning wake (Zoe's shake again, her new line, then `inc_chapter`); the house door leading out onto Citadel 2 from
chapter 2 (the door's cube change picks the CITABAU scene by `chapter()`); the Esmers' landing moved from the lighthouse to the morning,
with their new lines; the Tavern standing in the Citadel 2 build and its inside (scene 3) reachable again; the islanders' lines; Raph and
Tim at the Tavern. The hidden pages: the museum (scene 15) reachable in both Citadel builds (the rampart's lift pad does it today), the
blueprints' display case, Miss Bloop's and the neighbour's new lines, the pharmacy open and selling gallic acid, the purse
added to Mr. Paul's prize, and the two pages' pictures.

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
3. **The School.** The way in on foot (the user, 2026-11-06, "can't be walked into"; built 2026-11-07): from the road's verge a ramp
   (at most 355 a cell) down to a level sill under the School's gates (739, where the levelling left the gate's leaves; the lintel put
   on it too -- RaceTrackIsland.GateSill), a ramp down into the canyon, and the door under the face reaching a cell out in front of its
   doorway (scene 61's zone into 28 to x 10240, landing 768 further in; 28's way back out landing at x 10496 on -- ApplyDesert).
   Walked in and back out headless by inputs.
   The Dean:
   > **The Dean:** "Shut it is! Three pages, gone with the wind of your engines. Bring them back, and I'll think about it."

   Q_PAGES = 2. Three holomap arrows come on, one at each page (positions 224, 225 and 228 on a full build: the first free of 224-254
   no scene of the build has; label "An exam page", file 2) -- PagesInstall.
   > **Zoe:** "The pages blew along the road. If we drive the course we'll find them, and learn the bends while we're at it."
4. **The pages.** Each is picked up driving over it, or on foot:
   - > *"A page: Chapter 1, Levitation for Beginners."*
   - > *"Another page: Turning Sand into Slightly Different Sand."*
   - > *"The last page: Why Wizards Should Not Drive."*

   Q_PAGES 2 -> 5. Each page is item 6 (the pyramid key's slot, which the race build never gives -- its model OBJFIX 38's scroll,
   its texts 6/106/206 of file 2 the pages', the old ones kept at 60006/60106/60206) set to 1, 2, 3 -- its box shows the count (the
   race file's inv_count=6, RACEMOD.CPP) -- and its arrow off. Kept through a save and a load (a game variable).
5. **Back to the Dean.**
   > **The Dean:** "All three! Very well. Here: a diploma. It says you may use the School's grounds, which include the track. Don't let
   > it go to your head."

   The pages handed over (item 6 = 0). Found: Wizard's Diploma (item 30). Q_PAGES = 6.
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
   > **Lyra:** "Welcome to the moon, racers. I'm Lyra, the Cup's marshal from here to the end. Bad news: the base's computer won't switch
   > on the start lights, and nobody can get it to talk sense."

   Q_HAL = 1.
2. **The radio rings.** Baldino, from his lander's cockpit:
   > **Baldino (radio):** "Twinsen? Baldino, from the pits. Low gravity up here: I've softened your springs and put weight in the
   > nose, so you won't flip on the loops. And don't touch HAL, he hates me."

   Car update: the Emerald Moon's best setup.
3. **The base. HAL** (cracked): (the way down to the base and back up, the user, 2026-11-06 -- "jumping off the raised track on foot can
   strand Twinsen": RaceTrackIsland.Ladders, RaceTrackBuilder.Ladders -- a ladder up the road's side in every stretch of walkable ground a
   jump off the road lands in, where the climb is least, its decor a solid box from the ground to the road's top and a type 6 zone in
   front of it; 16 on the moon, from 810 to 7,291 high. Stretches no ladder fits: see the build log's "ladders:" line.)
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
   > **HAL:** "Thank you, Twinsen. The start lights are on. I have calculated your chances against Baldino: they are poor. I will race
   > too, to see if I am right."
   >
   > **HAL:** "Your radio is returned. I have improved it: it now receives the Esmers' channel."

   Found: Portable Radio (item 26 = 1 again).

   One HAL at a time (the user, 2026-11-06; built 2026-11-07): the race file's `opponent2_when=216 4 6` keeps HAL's car out of the pit
   lane and off the grid while Q_HAL is below 4 or 6 and more (RACEMOD.CPP OpponentAway); in the base, from the next visit after he is
   mended, his talker is gone and his screen sprite (actor 4: the coils' glow, his hum) off while Q_HAL is 4-5 -- his cabinet is the
   room's bricks and stays -- and a mechanic says (text 786, MechanicHalOut): "HAL's loaded himself into a racing car and gone out to the
   track. That cabinet's just a box till he's back." (In the mending visit HAL talks on, his song included: var_cube 71 marks him there.)
   The race gate's text: "Lyra: HAL won't switch on the start lights!" (RaceKeysA).
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
   > **Johnny Rocket:** "A black car? Came off the moon ship last night and took the first shuttle to Celebration Island. The
   > souvenir seller there sells pictures of every car he's seen. Ask him."
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
| The monkey monster | his crate (built: the story's race is him alone, RaceTrackIsland.StoryHeadToHead) | the grid, beside his crate |
| The small monkey monsters | built (crate-throwers on the lower decks): hazards, not racers | |
| Zoe | the companion, then in the elevator's cage | no pit box: a head-to-head |

The user, 2026-11-03: "We should add in some dialogue for the elevator track" -- then: "The elevator doesn't need a quest, perhaps some
pre-race dialogue from the Monkey monster, who I feel should be the only opponent of Twinsen for this race." No quest: the monster talks
before the race, and he gives the clue after it. (Track select's race keeps its extra drivers, the Franco policeman and the monkey with
the sword; the story's has the monster alone.)

**Walkthrough:**

1. **Arrival.** De La Fontaine's convoy pulls up on the top deck. Lyra, on the deck's edge:
   > **Lyra:** "Twinsen! Welcome to the Elevator Platform. The monster won't let anybody down to the undergas without a race. It's just
   > you and him."
2. **The monster,** beside his crate on the grid. When Twinsen comes near (or talks to him):
   > **The monkey monster:** "RAAARGH! You! Small blue one! You race me?"
   >
   > **The monkey monster:** "This my platform. My elevator. Nobody go down without race. Nobody beat me down. Nobody! Ever!"
   >
   > **The monkey monster:** "Black car came. At night. While I sleep! Not fair! Not a race! I wake up, he gone. RAAARGH!"
   >
   > **Zoe:** "So the masked driver sneaked past him. No wonder he's in a mood."
   >
   > **The monkey monster:** "You. You race fair? Down to bottom, up to top. My little ones throw crates. That is fair too. Ha!"

   Talking to him again before the race:
   > **The monkey monster:** "Hrrm. You still here? Race! Now!"
3. **The radio rings.**
   > **Baldino (radio):** "That platform's a rollercoaster. Grip, grip and more grip, I've given you all I've got. Hold on to your hat.
   > And your lunch."
4. **The start.** Zoe goes to the lift cage:
   > **Zoe:** "No pit stops on this one. I'll be in the lift cage, watching. Just don't fall off."

   The monster climbs into his crate:
   > **The monkey monster:** "RAAARGH! Go!"
5. **The race.** The monster alone (built). His little ones throw crates from the lower decks.

   Lost: > **The monkey monster:** "Ha! Too slow! Again? Again! RAAARGH!" The race restarts.
6. **Won.**
   > **The monkey monster:** "Hrrm. Good race. Grr." *(he thumps his chest)* "Real race. Not like black car."
   >
   > **The monkey monster:** "Black car went down behind the big tanker. Tanker never came up. Black car never came up. Something
   > down there, under the gas. You go see. You fast. Grr. Good."
   >
   > **Lyra:** "The elevator is yours. Down we go!"

   `inc_chapter()` -> 9. `change_cube(98)`.

**Flags:** none new.

**To build:** the monster on foot beside his crate on the grid before the race, with his lines (Action, or once as Twinsen comes near);
his start, lost and won lines (the race's own, as the other tracks' mains have); Lyra's and Zoe's lines.

---

## Chapter 9 — Wannies Island

**Where:** Wannies Island near the elevator (scene 98), the frog and the tower; the city (scene 118).

The elevator comes down into the game's own room at its foot (scene 123), whose laser barriers (actors 10-13) the game has the
magic ball thrown at their levers to switch off; the story's Twinsen has no ball, so while he has none they are off
(RaceTrackStory.Wannies.cs ElevatorFoot -- before 2026-11-07 he was shut in the room).
The game's two guards on the deck outside (scene 98, actors 4-5) are gone while he has no ball, and a ladder (the raised road's,
RaceTrackIsland.Wannies Ladders) goes up from that deck to the road, where his car stands on the start line (2026-11-07).

**Cast:**

| Who | Made from | Where |
|---|---|---|
| Lyra | the marshal, on the deck's edge | the start |
| The Wannie miner | the game's (BODY 342) | by his bulldozer |
| The old Wannie | the game's (BODY 386) | by the miner, watching the frog |
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
| Stan | the game's (BODY 439) | the library |
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
   > **Stan:** "The window? End of the long hall. I've timed that run from this desk more times than I've read these books. Race me
   > there."
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
   If Twinsen caught him in the dream (DREAM_CAUGHT = 1):
   > **FunFrock:** "We raced once, Twinsen, in your dream. You caught me, in your dream. This time it's real, and the wish is mine."

   If he didn't:
   > **FunFrock:** "We raced once, Twinsen, in your dream. You never caught me then, and you won't now. This time it's real, and the
   > wish is mine."
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

     The statue goes (var 79 = 0) and `change_cube(95)` shows Celebration as it was. FunFrock is gone with it. The dock's lines
     below wait for the scene loaded again (a scene variable set with the wish, cleared by the reload).
   - **"Another race. Right now."**
     > **FunFrock:** "Ha! You'll regret that."

     The race again, then the same choice. Choosing this a second time ends on FunFrock's laugh and the credits. (Asked once is
     game variable 52, which no race sets: the race's end sets Q_EMPEROR 224 back to 7, finish=224 7, over its 8.)
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
  - The second process plays Zoe. In story races she's never on the grid: she works the pit box (her Action as Twinsen's car comes
    through gives the stop). On foot she walks.
  - The first process owns the story; the second follows its scenes and chapter.
  - Built on the two-player mode that exists.
- **The radio:**
  - At each arrival an invisible helper rings the radio and waits for `use_inventory(26)` (20 seconds, then it answers itself).
  - Then Baldino's line, and the car update: the track's best setup for the story's race (`UseTrackSetups` in the story's race files),
    and `start_item=` where the update puts something in the boot.
- **The magic slate:** a track map per story track in `SCREEN.HQR`, `memo_ardoise(page)` at each arrival, the engine's slate raised from
  5 pages to 16 (saved games kept compatible).
- **The flags:** the story's chapter is the game's `chapter()` (`inc_chapter()` at each win); the rest as in the table above.
- **About 230 new lines,** each in six languages.
