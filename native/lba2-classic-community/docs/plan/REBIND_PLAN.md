# Rebinding every control: on foot, in the car, keyboard and controller

The user, 2026-11-25: "We need to look at tightening up the controls on the menu, and allowing a user to reassign controls for everything
including driving controls for both keyboard and car usage" -- "We need to be able to rebind keys for both controller and keyboard, and
perhaps the same key binded one way for when twinsen is in a car and another when he isn't".

## What there is (2026-11-26)

- `INPUT_BINDINGS.CPP`: 36 slots. 0-31 are the `Input` word's bits (`I_UP` .. `I_WEAPON_7`), 32-35 the four spells (read by
  `PERSO.CPP` with `CheckKey`). Each slot has two keyboard keys (`DefKeys`) and two pad buttons (`GamepadKeys`). `InitInput` folds
  both into one (key, mask) table for `GetInput`.
- Options > Controls (`GAMEMENU.CPP GereControllerMenu`): **Keyboard** (`CONFIG.CPP MenuConfig`) and **Game controller**
  (`MenuGamepadConfig`). These are the retail remap pages: all 36 rows, two columns, a clash refused, defaults / previous / accept.
  Saved to lba2.cfg as `Input<n>_1/_2` and `Gamepad<n>` / `Gamepad<n>_2`.
- One set for everything: in the car, `I_UP` accelerates, `I_DOWN` brakes, `I_LEFT`/`I_RIGHT` steer, `I_THROW` fires,
  `I_COMPORTEMENT` (Ctrl, pad LB) uses the power-up, Action gets him out when stopped, Shift opens the inventory outside races.
- Hard-coded in the race code, not rebindable:
  - handbrake: Z / pad B (`RACEMOD.CPP HandbrakeKey`);
  - change view: V / pad Back (`RaceCameraKey`; pad Back also stripped from the holomap in the car);
  - use item: also Ctrl and Right Ctrl read directly (`CheckKey(K_CTRL)`);
  - two players at one keyboard: player 1 W A S D, Q item, E handbrake, R view; player 2 the arrows, Right Ctrl, Right Shift
    (`RACEGAME.CPP RaceGame_SharedKeys` / `KeysBefore`, `NETRACE.CPP RaceNet_SharedKeys`). These are injected as Z / V presses.

## The plan

### Two sets

- **On foot**: the existing 36 slots, as now.
- **In the car**: its own keyboard and pad tables (`CarKeys`, `CarPadKeys`) for the car's actions only:
  Accelerate, Brake / reverse, Steer left, Steer right, Handbrake, Use power-up, Fire, Get out (two rows, Action's two slots),
  Change view, Inventory. Handbrake and Change view become slots of their own (36, 37: no `Input` bit, read like the spells).
- In the car, every slot not on the car page keeps its on-foot keys, except any key the car page uses. So rebinding Handbrake to H
  takes H off the holomap in the car only. Nothing that worked in the car stops working unless its key is given to a car action.
- A clash is refused only within one set's page, so one key can mean one thing on foot and another in the car.
- Defaults are today's keys. Nobody's controls change until they rebind.

### When the car set applies

Only in the game's own frame (`PERSO.CPP MainLoop`'s input read), with Twinsen in the car (`C_BUGGY`) and the race game on. Every
other input read uses the on-foot set: menus, the inventory, the holomap, dialogues and the remap pages. So a car rebind can never
lock anyone out of a menu. On a switch between sets, keys held at that moment are ignored until released. Without that, the key that
got him into the car would act in the car at once.

### Pages

Options > Controls (the race game's copy of the page):
- **Keyboard: on foot** -- the retail page, as now.
- **Keyboard: in the car** -- the same page over the car's rows.
- **Controller: on foot** / **Controller: in the car** -- the same for the pad.
- **Two players, one keyboard** -- each player's seven driving keys (player 1's and player 2's), replacing the fixed W A S D / arrows.
- Each page keeps Defaults / Previous / Accept. Saved at once to lba2.cfg (`CarInput<n>_1/_2`, `CarGamepad<n>_1/_2`) and, for the
  two-player keys, RACEGAME_SETTINGS.TXT.

The remap pages become one routine over a description: a table, its defaults, its rows (slot and label) and whether it captures keys
or pad buttons. The retail pages are the first two descriptions.

### Menus tightened

Menu input is fixed in its shared places (`MENU_INPUT.CPP`, `RACEGAME.CPP Menu/Step/Nav`, `GAMEMENU.CPP DoGameMenu`, the remap
pages): see the audit in the commit. The menus always take the arrows, Enter, Esc and the pad's d-pad, stick, A and B, whatever is
rebound, so no binding can leave a menu unreachable.

### Tests

- Harness: rebind handbrake (cfg), drive, the handbrake takes the new key and not Z. Bind a car key that is a foot key (H for
  handbrake) and check the holomap still opens on foot and not in the car.
- The switch: hold Space getting in, nothing fires in the car until it is released.
- A menu opened in the car navigates with the arrows after the car's arrows were rebound.
- Two players at one keyboard: rebound keys reach both games.
- The remap pages: screenshots of each, a clash refused, defaults restored.
