# Online races

One player hosts and everyone else joins them, up to 16 in a race. Written 2026-10-31 on a friend's UDP room library, netkit (`SOURCES/NET/`, see its README).

## For a player

Main menu, **Multiplayer**.

- **Host a race:** give a name, and the game asks the router to open UDP port 21997. The lobby then shows the address to give to friends, and the one for
  people in the same house (most routers don't loop their own outside address back). If the router says no, the game says why. Enter picks the track, its laps
  and the host's car, and starts the race for everyone.
- **Join a race:** give a name and the host's address, typed or pasted with Ctrl+V. `81.2.3.4`, or `81.2.3.4:21997` for another port. Enter picks a car. The
  race starts when the host starts it.

After a race, everyone is back in the lobby with the same players.

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
- **Controllers:** `LBA2_PAD` picks which pad a game uses. With two pads each player has one. With one, it's player 2's and player 1 has the keyboard. With none the game says so.
  A pad works while its window hasn't the focus (`SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS`).

`LBA2_LOCAL_AREA` and `LBA2_LOCAL_PADS` are for tests: a part of the screen in place of all of it, with no focus taken, and a count of pads.

## How it works

`NETRACE.CPP` is the game's side. An online race is a time trial's race file with `online=1` and no ghost, so there are no computer cars and every player
starts from the same spot. Nothing about the race itself is new: the game is the same on each PC, and the only thing shared is where the cars are.

- **A thread of its own.** The game's loop stops for seconds while a scene loads or the pause menu is up, and netkit lets a player go after 6 s of silence. So
  netkit runs on a thread behind a lock, and the game reads and writes `NETRACE.CPP`'s state.
- **The start.** The host announces the track. Each game loads it and says it's ready as its car reaches the start. `NetRaceFrame` holds the count-down, showing
  "Waiting for the other players", until the host says go: when all are ready, or after 30 s. A game with no host left goes on alone.
- **The cars.** 30 times a second each game sends a ghost's sample: place, angles, body, animation. A car is drawn 100 ms in the past, between the samples
  either side of that moment, so a late packet doesn't make it jump. The clocks needn't agree: each game takes the quickest trip it has seen as the difference.
  A move over 4,096 units, a rescue or a teleporter, isn't slid between (as a ghost's `RACE_GHOST_JUMP`).
- **Cars pass through each other.** With no one game in charge there's no one place both agree a crash happened. `DrawNetCars` draws them as a ghost played
  back is, solid, with the player's name over them.
- **The order.** Finished first by time, GO to the line; then by lap and checkpoint (`DrawNetBoard`). Everyone counts from the same GO.

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
closing itself with player 1 quitting in mid-race. Not tested: one on each screen, which needs the screens free, and real pads: the test drives both games by injected keys.

## Not done

- Power-ups don't reach other players: each game's are its own.
- No room password: anyone with the address can join. Nothing is encrypted (netkit's `CHANGES.txt`).
- Two players need a pad between them: two on one keyboard isn't done, as the second window never has the keyboard.
- Joining mid-race waits for the next race. The host leaving ends the session: there's no handing over.
- Track select's code is asked of the host as usual.
- IPv4 only, and a host behind the provider's NAT can't be reached.
