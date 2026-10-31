# Online races

One player hosts and everyone else joins them, up to 16 in a race. Written 2026-10-31 on a friend's UDP room library, netkit (`SOURCES/NET/`, see its README).

## For a player

Main menu, **Multiplayer**.

- **Host a race:** give a name and, if you want one, a password (Enter alone for none). The game asks the router to open UDP port 21997. The lobby then shows the
  address to give to friends, the one for people in the same house (most routers don't loop their own outside address back) and the password. If the router says no, the
  game says why. Enter picks from every track, with no code, then the laps and the host's car, and starts the race for everyone.
- **Join a race:** give a name and the host's address, typed or pasted with Ctrl+V. `81.2.3.4`, or `81.2.3.4:21997` for another port. If the race has a password the
  game asks for it; a wrong one sends you back to the menu. Enter picks a car. The race starts when the host starts it. Joining with a race on, you pick a car and start
  from the grid straight away, and your time runs from everyone's go.

After a race, everyone is back in the lobby with the same players. A race online has the race's mushrooms and power-ups, cars touch, and its times don't go in Best laps.

- **Two players on this PC:** see below.

## Two players at one PC

**Multiplayer, Two players on this PC:** side by side, one above the other, or one on each screen, where each player picks their screen from the PC's list.

It's two copies of the game racing as an online race does, over the PC's own network (UDP 21998, no router). The engine has one camera, one screen buffer and one hero,
all globals, so a second view in one game would mean rewriting the renderer's state; two games leave all of that as it is, each with its own window, picture and HUD.

- The first game moves into its half of the screen, or its screen, with no border, and starts the second (`RaceNet_SpawnPlayer2`). `LocalPage` shapes each picture to its
  window (`LocalPicture`): 480 lines high at least, for the pages, and at most 1280 x 720 where it can, for the two software renderers sharing a PC. Width a multiple of 8.
- The second game finds `LBA2_PLAYER2`, joins by itself and goes straight to the lobby. Its saves and log are in `Player2` in the user's folder, with the first game's settings
  copied in. It has no sound, so the music plays once. `LBA2_WINDOW_RECT` places it, and it can't take keyboard focus, which stays with player 1.
- It goes when the first game goes, whether in the lobby or mid-race (`RaceGame_LocalAlone`), and the first game's window goes back to its size and place.
- **Controllers:** `LBA2_PAD` picks which pad a game uses. With two pads each player has one. With one, it's player 2's and player 1 has the keyboard.
  A pad works while its window hasn't the focus (`SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS`).
- **One keyboard:** player 2 has W A S D, Q the item (and Enter in the menus), E the handbrake, R the view, always, pad or not, by where the keys are whatever the keyboard's letters.
  Their game never has the keyboard's focus, so it asks Windows (`RaceNet_SharedKeys`), only with one of the two games in front (`LBA2_P1_PID`), or it would drive on what's
  typed elsewhere. Player 1's game leaves those keys alone (`RaceGame_SharedKeys`): S would save, W is the action key. Many keyboards can't report every combination of keys held at once.
- A resolution switch sets the window to the picture's size, so `LocalStart` sets the window again after it. With `LBA2_LOCAL_AREA` the two were the same size and it didn't show.
- `ScreenPage` names a screen by where it is from the main one (Main, Left, Above left), as two of a model have the same name. Windows's numbers aren't SDL's.

`LBA2_LOCAL_AREA` and `LBA2_LOCAL_PADS` are for tests: a part of the screen in place of all of it, with no focus taken, and a count of pads.

## How it works

`NETRACE.CPP` is the game's side. An online race is a time trial's race file with `online=1`, no ghost, and its race's power-ups added (`CopyPowerUps`), so there are no
computer cars, and `NetHiddenItems` shows the mushrooms the trial's file hides. Each player has a grid spot, by the order they joined (`NetGridSpot`); a track with no grid
spreads them round its one spot (`NetSpread`). The rule for everything between cars: **each game is the last word on its own car, and only that.** What a player sees hit them, did.

- **A thread of its own.** The game's loop stops for seconds while a scene loads or the pause menu is up, and netkit lets a player go after 6 s of silence. So
  netkit runs on a thread behind a lock, and the game reads and writes `NETRACE.CPP`'s state.
- **The start.** The host announces the track. Each game loads it and says it's ready as its car reaches the start. `NetRaceFrame` holds the count-down, showing
  "Waiting for the other players", until the host says go: when all are ready, or after 30 s. A game with no host left goes on alone.
- **The cars.** 30 times a second each game sends a ghost's sample: place, angles, body, animation, speed. A car is drawn where it is now, 40 ms ago at most, because cars
  touch by what the player sees and at 150 km/h 100 ms is two car lengths. So the sender's clock is moved on by half the round trip (a ping a second), past the newest
  sample the car goes on as it was going for up to 250 ms, and the jump a late sample would make is eased out over 90 ms. A move over 4,096 units, a rescue or a
  teleporter, isn't slid between (as a ghost's `RACE_GHOST_JUMP`).
- **Touching.** `NetContact`: each game moves its own car only and sees the other where it draws it, so each works out the same knock from its own side: the two speeds
  along the line between them shared out, with a little bounce, part into the car's speed and the rest into a shove sideways that fades. Overlapping, a car is eased off the
  other by half the overlap a frame. It all goes through the car's own move (`RaceMod_CarPush`), so walls stop it. Two cars found one inside the other, a rescue, pass through until clear.
- **Power-ups.** Every game runs every power-up (`NetSay`, `NetEvents`, reliable). A mushroom taken is gone from every road. A drop lands where it landed for the player who dropped it, on every
  road; the game whose car takes it tells the others to clear it. A ball sets off from the thrower in every game and only the game of the car it hits calls it a hit; a red ball is after the
  car ahead by the thrower's reckoning, and the others see it run on along the road. Lightning and ghosts reach every car. Each draw is weighted by how far back the player is among the players.
- **The order.** Finished first by time, GO to the line; then by lap and how far round (`DrawNetBoard`). Everyone counts from the same GO; a late joiner's time has how late they were added.
- **Joining late.** The host keeps the track and when it went, and sends a player who arrives during a race (`Admit`). They pick a car and go at once.
- **Password.** netkit has none, so it's the game's, with `NetKit_Kick` added to netkit. The host asks a new player a random number, and they answer it hashed with the password. Keeps strangers out; nothing is encrypted. Not saved.
- **Versions.** What must match is the tracks and cars, not the build: a key of `RACEGAME.TXT` and every race file (`IdentifyGame`), and the protocol. Two builds of the same tracks race each other.
  A game with another key is turned away, and told the host's version and its own. The version shown is from `Update\install.json`.

## The router

`NETUPNP.CPP`, from what the user's Sagemcom F@st 5364 taught us. Any of it may be true of another router.

- `GetSpecificPortMappingEntry` on a port with no mapping crashes its UPnP service, which comes back on another TCP port. So it's asked only after adding, and the
  router is searched for afresh each time.
- A router can list a WAN connection it doesn't use: the one that gives an outside address is the one.
- A timed port can be refused (725): then one with no time is asked for.
- An outside address that is private, or 100.64/10, means the provider's network is in front of the router. The port opens and reaches nobody, and the game says so.
- The port has a 2-hour lease, and comes off when hosting stops or the game quits by its menu. A game that crashes leaves it to the lease.

Port 21997: nothing common uses it. Proven through the router from outside by UPnP, 2026-10-30.

## Tested

Two games on one PC, each in its own sandbox, driven through the real menus by injected keys (`--listen`): hosting, the router opening the port and closing it again
on the real router (result 200), typing an address, two races on one session, on the Elevator Platform and Mosquibees Island, the host quitting mid-race. With 240 ms round trip through a relay too
(`delay.py`). Two players on the same PC share a clock, so a real internet game has not been played yet.

Two players at one PC: through the real menus in a 1200 x 640 part of the screen, side by side and one above the other, 56 to 60 frames a second in each window, and player 2's game
closing itself with player 1 quitting in mid-race. One on each screen on the user's three 1920 x 1080 monitors, 2026-10-31: each window fills its screen, two different pairs picked,
55 to 60 frames a second, and leaving puts player 1's window back. That test found player 1's window stuck at the picture's size and the screen list's rows blank, neither visible from the logs.

Limits fixed 2026-10-31, tested by `run_limits.ps1`, `run_items.ps1` and `run_sweep.ps1`: the wrong password refused and the right one let in, a player joining a race 12 s in and finishing it, another key
turned away and told, every power-up reaching the other game, and contact from the grid on every track. Player 2's keys are tested by `p2keys`, from `RaceGame_SharedKeys` on: **real keys
through `GetAsyncKeyState`, real pads and a game between two houses have still not been tried.**

## Not done

- Nothing is encrypted (netkit's `CHANGES.txt`).
- The host leaving ends the session: there's no handing over.
- IPv4 only, and a host behind the provider's NAT can't be reached.
- Another player's protection spell isn't drawn on their car. A walking penguin goes its own way in each game.
