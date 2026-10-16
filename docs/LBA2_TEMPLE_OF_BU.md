# The Temple of Bú: LBA1's temple as an LBA2 island, and its Halloween race (2026-10-16)

![The race through the temple](temple/race_sheet.png)

The user: "Port the temple of Bu from LBA1 into LBA2 as a separate island. Make sure it is visually the same with the same colour palette,
which means when viewed from the same angle as the fixed LBA1 angle it should match. After it's been ported generate a Halloween inspired
race track for it. This will be a separate release for a Halloween special track before our full racing mod is released. Let's make it as
creepy, scary, and Halloween inspired as we can do, let's turn our mushroom pickups into mini pumpkins. Add lots of typical Halloween decor,
pumpkins, witches, ghosts etc."

**To use it:**

- **The island on its own:** Tools > **LBA2: Temple of Bú (from LBA1)…** adds it to the game folder (both game folders must be set under
  File > Settings: it reads LBA1's scenes). The same menu builds it again or takes it out. Then pick `TEMPLE.ILE` in the Island list.
- **The race:** tick **The Temple of Bú: the Halloween race** in the race track window and Build. The build makes the island afresh, with
  doorways cut where the road goes. Then open `TEMPLE.ILE` and Play.

Only LBA Assembler's own engine draws it right: it is raced as Otringal's island 4 drawn from `TEMPLE.ILE`, in a palette and under a sky of its
own (the race file's `island_file=4 temple`, `island_palette=40`, `island_sky=24`).

## The port

![LBA1 (left) and the port from LBA1's angle (right)](temple/lba1_and_port_closeup.png)

![The whole temple: LBA1's joined map (left) and the port (right)](temple/lba1_and_port_whole.png)

**The layout** (`Terrain/Temple/TempleLayout.cs`): the temple is LBA1's scenes 8 (the first floor), 40 (the crypt, sixteen layers lower, and
the long corridor) and 41, placed as the joined map (`Lba1Areas`, "Temple of Bú") has them: 24,516 cells.

**The frame** (`TempleFrame`): every cell is three times LBA1's size each way (Scale 3). The corridors, five to seven cells wide in LBA1, are
15 to 21 cells for the cars, the palace's doorway width. The temple's 34 layers then stand 26,112 high, under the engine's 32,767. A uniform
scale keeps the look from LBA1's angle the same. The island is 11 × 8 cubes (2..12 × 3..10), the temple from cell (224, 256).

**The objects** (`TempleObjects.cs`): every LBA1 cell is a box textured with its brick's own faces, unskewed from the isometric sprite (the
code is Polar Island's, `PolarTextures`). LBA1's interior bricks draw only the faces the grid leaves to be seen: a floor tile only its top,
a wall brick only the side it faces. So a brick that draws any face whole is a whole block. Only bricks that draw no face whole (a lamp's
pole, a rail) get the box their outline fits. Where a brick draws nothing on a face, the face is see-through (polygon type 12, "incrusted":
colour 0 isn't drawn): the railings' gaps, the cobwebs' strands, the skull lamps' surroundings. A face is left out where the box beyond it
is solid there. The bodies only draw (their decors' boxes touch nothing). What is solid is 7,663 merged boxes of their own, with LBA1's
stairs (shapes 2-5) as four steps each. 1,108 different brick faces on 11 object pages, 74,332 faces in 1,589 bodies.

**The colours** (`TemplePalette.cs`): an island's palette colours the cars, Twinsen and the screen's text by index too, and every retail
island has the same palette, tuned a little. So LBA1's whole palette won't do. Instead, Otringal's has the temple's colours written into it:
each, the most drawn first, into the nearest slot no other has taken, when it is within 24 (a colour an actor drawn there hardly sees
change). 96 of the temple's 106 colours get slots of their own, 99.98 % of its pixels: those are exact. The shading and fog table is made
for that palette. Its row 12 (the level objects' textures are drawn at) is the palette itself. Its fog is black: far rooms fade into the
dark, as LBA1 draws the temple in the black. Measured on a render from LBA1's angle: 100 % of the temple's drawn pixels are LBA1 palette
colours, and the mean colour is within 0.7 (of 255) of LBA1's own picture's.

**RESS.HQR**: the palette goes in entry 40 and a night sky (dark violet clouds, a black sea) in entry 24. These are RESS_XPL0 + 13 and
RESS_SKYSEA0 + 13, slots the retail file leaves empty.

**The engine**:

- `AMBIANCE.CPP` ChoicePalette takes the race-track mode's `island_palette=` and the editor view's `IslandPaletteOverride`. The editor's
  island loads left CubeMode at interior, so its palette had always been citabau's (only its colours, converted with the island's own
  palette, hid it).
- `EXTFUNC.CPP` takes `island_sky=`.
- `RENDERER_API.CPP` maps "temple" to island 4 with entries 40 and 24.
- Object pages: 32, the decor's page in its Body's bits 18-22 (`MAX_OBJ_PAGES`, `IslandFile.MaxObjectPages`).

**Commands** (ScriptRoundTrip):

- `templebuild <LBA1> <LBA2 game>`: the plain island into a folder.
- `templeregion <LBA1> <png> [x0 x1 z0 z1]`: LBA1's own picture of the temple.
- `templemap <LBA1> <png>`: a height map.
- `templecolours`, `templebricks`: the colour and brick studies.
- `polarview` with `VIEW_ISLAND=TEMPLE VIEW_Y=<height>` renders it from any angle. LBA1's is pitch 341, yaw 3584.

## The Halloween race

![The route over the temple, coloured by height](temple/route.png)

The plan is `docs/racetrack/temple_track_plan.json`, from `tools/RaceTrackPlan/temple_design.py`, which reads a plain build's `TEMPLE.JSON`.
LBA1's temple is a dungeon at three heights: the first floor and its corridor at 14,080, scene 8's lower hall at 11,008, and the crypt at
1,792. In LBA1 these are joined by narrow stairs, with dead ends and no way round. The lap is a raised road a little over the floors (rail
held). Where it passes walls, it goes through doorways cut in them: 819 cells, the plan's `templeCuts`. Between the heights it runs on
ramps, and down to the crypt and back up on spirals through the black void round the temple. 1,719 cells, steepest 12 %; about 65 s a lap
with a car at 110 km/h.

1. The start, on the straight through the crypt's west room, heading east. The opponents wait on its floor while the player qualifies (the
   plan's pit spots).
2. Through the crypt's rooms: the lower passage, the south-east room, north into the east room.
3. Out of its east side and up a spiral to the long corridor's east end.
4. West along the corridor: its floor is twelve cells wide, so the road is eight, and the spiral frieze on its north wall stays whole.
5. North into the first floor, down a ramp into the lower hall, round it, up into the west rooms and round them.
6. Out over the first floor's open south edge and down a spiral to the crypt.

**Halloween** (`TempleHalloween.cs`, the plan's `halloween`):

![A jack-o'-lantern by the road](temple/jack_o_lantern.png)

- *The road*: the deck theme `halloween`: near black, pumpkin-orange and black curbs, glowing orange rail tops, dark plum sides. The start
  gantry is in orange and black squares (`DeckTheme.Gantry`).
- *Decor* (drawn only, nothing to bump into), 86 pieces: jack-o'-lanterns beside the road on the floors and hanging in the dark over the
  void. Their carved faces glow in the palette's fire colours (240-247, the same in every island's palette). There are gravestones and
  crosses with candles, witches' cauldrons brimming with glowing green brew, and a totem of giant jack-o'-lanterns up each spiral's middle.
- *Spooks*, 27: bodies of BODY.HQR the race-track mode flies and draws itself, as it draws the Wannies' frog (`RACEMOD.CPP spook=<body>
  <flags> <height> <way...>`). There are no actors, so no scene limit and no animations to fit. Witches on their broomsticks circle the
  spirals and patrol the corridor, the lower hall and the crypt. Every few seconds one stops and throws a fire ball at a car below her
  (flags 1; a hit stuns the car as a gas monster's does, "hit by a witch's fire ball"). Ghosts drift across the road a little over the
  cars, and bats wheel high over it (flags 2: round and round their way).
- *Mini pumpkins*: the power-ups' mushrooms, the fakes the cars drop and the item box's icon are a jack-o'-lantern. It is the mushroom
  entity 112's body 3.
- *The music*: the scenes play LBA2's own Temple of Bù's music (3) and its ambient sounds (scene 10).
- *The line-up*: the Dark Monk (the one to beat), Ker'aooc in her cauldron and Tabata on his spell book.

The scenes are 301-327, one for each cube the road's width crosses (`TempleTrack.SceneCubes`). The start scene is 326.

**Tests**:

- `E:\dump\TEMP\temple\pilot\run.sh <tag> <ticks>` is the headless pilot. CARF=car_fast.txt is a fast car with no qualifying.
- `E:\dump\TEMP\temple\ui\ui_temple.ps1` builds in the race window, opens TEMPLE.ILE and Plays.
- A regression build of every other track gives its 44 files byte-identical to the code before.

![The editor: TEMPLE.ILE open](temple/editor_view.png)

![Play in the editor](temple/editor_play.png)
