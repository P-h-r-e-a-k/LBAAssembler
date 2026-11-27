#include "INPUT_BINDINGS.H"

#include <string.h> // memcpy

#include <SYSTEM/INPUT.H>         // DefineInputKeys
#include <SYSTEM/KEYBOARD_KEYS.H> // K_*

// The 1997 source carried two default layouts, DefKeysDefault for DOS and this
// one for Windows 95. Only the second was ported. A binding set is where the
// first would come back if anyone wants it.
const T_DEF_KEY DefKeysDefault95[MAX_INPUT_SLOTS + 1] =
    {
        {K_GRAY_UP, K_NUMPAD_8},    // I_UP
        {K_GRAY_DOWN, K_NUMPAD_2},  // I_DOWN
        {K_GRAY_LEFT, K_NUMPAD_4},  // I_LEFT
        {K_GRAY_RIGHT, K_NUMPAD_6}, // I_RIGHT
        {K_ALT, 0},                 // I_THROW
        {K_CTRL, 0},                // I_COMPORTEMENT
        {K_SHIFT, 0},               // I_INVENTORY
        {K_SPACE, K_NUMPAD_5},      // I_ACTION_M
        {K_W, K_GRAY_END},          // I_ACTION_ALWAYS
        {K_ENTER, K_NUMPAD_ENTER},  // I_RETURN
        {K_F10, 0},                 // I_MENUS
        {K_H, K_TAB},               // I_HOLOMAP
        {K_P, 0},                   // I_PAUSE
        {K_X, 0},                   // I_ESQUIVE
        {K_F5, 0},                  // I_NORMAL
        {K_F6, 0},                  // I_SPORTIF
        {K_F7, 0},                  // I_AGRESSIF
        {K_F8, 0},                  // I_DISCRET
        {K_F1, 0},                  // I_HELP
        {K_F2, K_S},                // I_SAVE
        {K_F3, K_L},                // I_LOAD
        {K_F4, K_O},                // I_OPTIONS
        {K_BACKSPACE,
         0},                                // I_CAMERA (backtick used for console)
        {K_NUMPAD_PLUS, K_GRAY_PAGE_UP},    // I_CAMERA_LEVEL_PLUS
        {K_NUMPAD_MOINS, K_GRAY_PAGE_DOWN}, // I_CAMERA_LEVEL_MOINS
        {K_1, 0},                           // I_WEAPON_1
        {K_2, 0},                           // I_WEAPON_2
        {K_3, 0},                           // I_WEAPON_3
        {K_4, 0},                           // I_WEAPON_4
        {K_5, 0},                           // I_WEAPON_5
        {K_6, 0},                           // I_WEAPON_6
        {K_7, 0},                           // I_WEAPON_7
        {K_N, 0},                           // I_PINGOUIN
        {K_J, 0},                           // I_JETPACK
        {K_C, 0},                           // I_PROTECTION
        {K_F, 0},                           // I_FOUDRE
};

T_DEF_KEY MemoDefKeys[MAX_INPUT_SLOTS + 1];
T_DEF_KEY DefKeys[MAX_INPUT_SLOTS + 1];

// tableau de configuration des touches utilisé au cours du jeux
U32 TabInputBit[MAX_INPUT_SLOTS * 2 + 1];

// Combined keyboard+gamepad key/mask table handed to GetInput(). Folding the
// gamepad in here (instead of OR-ing it into Input afterward) makes it a
// first-class input: it flows through the one Input rebuild + NoRepeatInput
// edge-filter, so held pad buttons edge-detect like keyboard keys and survive a
// mid-frame GetInput() rebuild. Only the first 32 slots map to Input bits; four
// keys per slot (keyboard Key1/Key2 + gamepad Key1/Key2) => 32 * 4 entries.
#define INPUT_TABLE_SLOTS 32
static U32 CombinedInputKeys[INPUT_TABLE_SLOTS * 4];
static U32 CombinedInputMasks[INPUT_TABLE_SLOTS * 4];

T_DEF_KEY GamepadKeys[MAX_INPUT_SLOTS];

const T_DEF_KEY GamepadKeysDefault[MAX_INPUT_SLOTS] = {
    {K_GAMEPAD_RTRIGGER, 0},                         // 0  I_UP
    {K_GAMEPAD_LTRIGGER, 0},                         // 1  I_DOWN
    {K_GAMEPAD_LSTICK_LEFT, 0},                      // 2  I_LEFT
    {K_GAMEPAD_LSTICK_RIGHT, 0},                     // 3  I_RIGHT
    {K_GAMEPAD_RSHOULDER, 0},                        // 4  I_THROW
    {K_GAMEPAD_LSHOULDER, 0},                        // 5  I_COMPORTEMENT
    {K_GAMEPAD_Y, 0},                                // 6  I_INVENTORY
    {K_GAMEPAD_X, 0},                                // 7  I_ACTION_M
    {K_GAMEPAD_A, 0},                                // 8  I_ACTION_ALWAYS
    {K_GAMEPAD_B, 0},                                // 9  I_RETURN
    {K_GAMEPAD_START, 0},                            // 10 I_MENUS
    {K_GAMEPAD_BACK, 0},                             // 11 I_HOLOMAP
    {0, 0},                                          // 12 I_PAUSE (unbound; Start opens the in-game menu)
    {K_GAMEPAD_LSTICK_BTN, K_GAMEPAD_RSTICK_BTN},    // 13 I_ESQUIVE
    {0, 0},                                          // 14 I_NORMAL
    {0, 0},                                          // 15 I_SPORTIF
    {0, 0},                                          // 16 I_AGRESSIF
    {0, 0},                                          // 17 I_DISCRET
    {0, 0},                                          // 18 I_HELP
    {0, 0},                                          // 19 I_SAVE
    {0, 0},                                          // 20 I_LOAD
    {0, 0},                                          // 21 I_OPTIONS
    {K_GAMEPAD_RSTICK_LEFT, K_GAMEPAD_RSTICK_RIGHT}, // 22 I_CAMERA (classic-mode turn; analog orbit owns Auto camera)
    {K_GAMEPAD_RSTICK_UP, 0},                        // 23 I_CAMERA_LEVEL_PLUS
    {K_GAMEPAD_RSTICK_DOWN, 0},                      // 24 I_CAMERA_LEVEL_MOINS
    {0, 0},                                          // 25 I_WEAPON_1
    {0, 0},                                          // 26
    {0, 0},                                          // 27
    {0, 0},                                          // 28
    {0, 0},                                          // 29
    {0, 0},                                          // 30
    {0, 0},                                          // 31
    {K_GAMEPAD_DPAD_RIGHT, 0},                       // 32 I_PINGOUIN
    {K_GAMEPAD_DPAD_DOWN, 0},                        // 33 I_JETPACK
    {K_GAMEPAD_DPAD_UP, 0},                          // 34 I_PROTECTION
    {K_GAMEPAD_DPAD_LEFT, 0},                        // 35 I_FOUDRE
};

U32 NbInput = MAX_INPUT_SLOTS;

// ---- In the car ------------------------------------------------------------------------------------------------------------------------
// The defaults are the keys the car took before it had a set of its own: the on-foot keys of its slots, with the race's handbrake (Z, B)
// and view (V, Back), and Right Ctrl for the power-up as RACEMOD.CPP read it.
T_DEF_KEY CarKeys[CAR_SLOTS];
T_DEF_KEY CarPadKeys[CAR_SLOTS];
const T_DEF_KEY CarKeysDefault[CAR_SLOTS] = {
    {K_GRAY_UP, K_NUMPAD_8},    // 0  I_UP: accelerate
    {K_GRAY_DOWN, K_NUMPAD_2},  // 1  I_DOWN: brake, reverse
    {K_GRAY_LEFT, K_NUMPAD_4},  // 2  I_LEFT: steer left
    {K_GRAY_RIGHT, K_NUMPAD_6}, // 3  I_RIGHT: steer right
    {K_ALT, 0},                 // 4  I_THROW: fire
    {K_CTRL, K_CTRL_RIGHT},     // 5  I_COMPORTEMENT: use the power-up
    {K_SHIFT, 0},               // 6  I_INVENTORY
    {K_SPACE, K_NUMPAD_5},      // 7  I_ACTION_M: get out (stopped)
    {K_W, K_GRAY_END},          // 8  I_ACTION_ALWAYS: get out (stopped)
    {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
    {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
    {K_Z, 0},                   // 36 the handbrake
    {K_V, 0},                   // 37 the race's view
};
const T_DEF_KEY CarPadKeysDefault[CAR_SLOTS] = {
    {K_GAMEPAD_RTRIGGER, 0},    // 0  accelerate
    {K_GAMEPAD_LTRIGGER, 0},    // 1  brake, reverse
    {K_GAMEPAD_LSTICK_LEFT, 0}, // 2  steer left
    {K_GAMEPAD_LSTICK_RIGHT, 0},// 3  steer right
    {K_GAMEPAD_RSHOULDER, 0},   // 4  fire
    {K_GAMEPAD_LSHOULDER, 0},   // 5  use the power-up
    {K_GAMEPAD_Y, 0},           // 6  inventory
    {K_GAMEPAD_X, 0},           // 7  get out
    {K_GAMEPAD_A, 0},           // 8  get out
    {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
    {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
    {K_GAMEPAD_B, 0},           // 36 the handbrake
    {K_GAMEPAD_BACK, 0},        // 37 the race's view
};
const S32 CarPageSlot[CAR_PAGE_ROWS] = {0, 1, 2, 3, CAR_SLOT_HANDBRAKE, 5, 4, 8, 7, CAR_SLOT_VIEW, 6};
const char *const CarPageLabel[CAR_PAGE_ROWS] = {"Accelerate", "Brake, reverse", "Steer left", "Steer right", "Handbrake", "Use power-up",
                                                 "Fire", "Get out (stopped)", "Get out, more keys", "Change view", "Inventory"};

S32 Bindings_CarSlot(S32 slot) {
    for (S32 r = 0; r < CAR_PAGE_ROWS; r++)
        if (CarPageSlot[r] == slot) return TRUE;
    return FALSE;
}

void RestoreCarKeys(S32 keyboard, S32 pad) {
    if (keyboard) memcpy(CarKeys, CarKeysDefault, sizeof(CarKeys));
    if (pad) memcpy(CarPadKeys, CarPadKeysDefault, sizeof(CarPadKeys));
}

// ---- Two players at one keyboard -------------------------------------------------------------------------------------------------------
T_DEF_KEY TwoKeys[TWO_KEYS];
const T_DEF_KEY TwoKeysDefault[TWO_KEYS] = {
    {K_W, 0}, {K_S, 0}, {K_A, 0}, {K_D, 0}, {K_Q, 0}, {K_E, 0}, {K_R, 0},
    {K_GRAY_UP, 0}, {K_GRAY_DOWN, 0}, {K_GRAY_LEFT, 0}, {K_GRAY_RIGHT, 0}, {K_CTRL_RIGHT, 0}, {K_SHIFT_RIGHT, 0}, {PLATFORM_SCANCODE_SLASH, 0},
};
const S32 TwoKeySlot[TWO_KEYS] = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13};
const char *const TwoKeyLabel[TWO_KEYS] = {
    "Player 1: accelerate", "Player 1: brake, reverse", "Player 1: steer left", "Player 1: steer right", "Player 1: use item",
    "Player 1: handbrake", "Player 1: camera view",
    "Player 2: accelerate", "Player 2: brake, reverse", "Player 2: steer left", "Player 2: steer right", "Player 2: use item",
    "Player 2: handbrake", "Player 2: camera view",
};

void RestoreTwoKeys(void) { memcpy(TwoKeys, TwoKeysDefault, sizeof(TwoKeys)); }

static S32 s_inCar = 0;
static S32 s_switchArmed = 0;
static U32 s_switchHeld = 0;

// A key one of the car's own slots takes (keyboard or pad)
static S32 CarTakes(U32 key) {
    if (!key) return FALSE;
    for (S32 r = 0; r < CAR_PAGE_ROWS; r++) {
        const S32 s = CarPageSlot[r];
        if (CarKeys[s].Key1 == key || CarKeys[s].Key2 == key || CarPadKeys[s].Key1 == key || CarPadKeys[s].Key2 == key) return TRUE;
    }
    return FALSE;
}

void Bindings_UseCar(S32 car) {
    car = car ? 1 : 0;
    if (car == s_inCar) return;
    s_inCar = car;
    InitInput();
    s_switchArmed = 1;
}

S32 Bindings_InCar(void) { return s_inCar; }

U32 Bindings_FilterSwitch(U32 input) {
    // (the first read after a change: what is held now is ignored till it is let go)
    if (s_switchArmed) { s_switchHeld = input; s_switchArmed = 0; }
    s_switchHeld &= input;
    return input & ~s_switchHeld;
}

U32 Bindings_SwitchHeld(void) { return s_switchHeld; }

//-------------------------------------------------------------------------
void InitInput(void) {
    // Rebuild the combined table from the current keyboard (DefKeys) and gamepad
    // (GamepadKeys) bindings. Call after either changes (key remap, config load,
    // restore-defaults). Spell slots 32-35 stay out of the Input path and are
    // read via direct CheckKey(DefKeys[..]) in PERSO.CPP.
    // In the car (Bindings_UseCar): the car's own slots from its tables, every other slot its on-foot keys but any the car's take.
    S32 n = 0;
    for (S32 i = 0; i < INPUT_TABLE_SLOTS; i++) {
        U32 mask = (1u << i);
        U32 keys[4] = {DefKeys[i].Key1, DefKeys[i].Key2, GamepadKeys[i].Key1, GamepadKeys[i].Key2};
        if (s_inCar) {
            if (Bindings_CarSlot(i)) {
                keys[0] = CarKeys[i].Key1;
                keys[1] = CarKeys[i].Key2;
                keys[2] = CarPadKeys[i].Key1;
                keys[3] = CarPadKeys[i].Key2;
            } else {
                for (S32 k = 0; k < 4; k++)
                    if (CarTakes(keys[k])) keys[k] = 0;
            }
        }
        for (S32 k = 0; k < 4; k++) {
            CombinedInputKeys[n] = keys[k];
            CombinedInputMasks[n++] = mask;
        }
    }

    NbInput = (U32)n;

    DefineInputKeys(n, CombinedInputKeys, CombinedInputMasks);
}

//-------------------------------------------------------------------------
// The reverse of the fold above. Pure lookups over the two tables, in the same
// order InitInput reads them, so a key this hands back is one the forward
// direction would have folded into that slot's mask.
S32 Bindings_KeysForSlot(S32 slot, U32 *out, S32 max) {
    S32 n = 0;

    if (slot < 0 OR slot >= MAX_INPUT_SLOTS OR out == NULL)
        return 0;

    const U32 candidates[4] = {DefKeys[slot].Key1, DefKeys[slot].Key2,
                               GamepadKeys[slot].Key1, GamepadKeys[slot].Key2};
    for (S32 i = 0; i < 4 AND n < max; i++) {
        if (candidates[i] != 0)
            out[n++] = candidates[i];
    }
    return n;
}

//-------------------------------------------------------------------------
S32 Bindings_SlotForKey(U32 key) {
    // Zero is the unbound marker in both tables, not a key. Answering the
    // question for it would report every empty cell as a match.
    if (key == 0)
        return -1;

    // Through the same helper rather than a second copy of the four-cell read,
    // so the two directions cannot drift apart on which cells count.
    for (S32 i = 0; i < MAX_INPUT_SLOTS; i++) {
        U32 keys[4];
        S32 n = Bindings_KeysForSlot(i, keys, 4);
        for (S32 k = 0; k < n; k++) {
            if (keys[k] == key)
                return i;
        }
    }
    return -1;
}

//-------------------------------------------------------------------------
S32 Bindings_CheckReversible(S32 *badSlot, U32 *badKey) {
    // Unbound first: a slot with no key is the answer to "why did this action
    // not come back", and reporting a sharing pair from further down the table
    // would bury it.
    for (S32 i = 0; i < MAX_INPUT_SLOTS; i++) {
        U32 keys[4];
        if (Bindings_KeysForSlot(i, keys, 4) == 0) {
            if (badSlot)
                *badSlot = i;
            if (badKey)
                *badKey = 0;
            return BINDINGS_REVERSE_UNBOUND;
        }
    }

    for (S32 i = 0; i < MAX_INPUT_SLOTS; i++) {
        U32 keys[4];
        S32 n = Bindings_KeysForSlot(i, keys, 4);
        for (S32 k = 0; k < n; k++) {
            // The lookup scans in slot order, so a key names the lowest slot
            // that binds it. A key of this slot naming a different one is the
            // sharing case, and it is this slot that cannot be expressed.
            S32 owner = Bindings_SlotForKey(keys[k]);
            if (owner != i) {
                if (badSlot)
                    *badSlot = i;
                if (badKey)
                    *badKey = keys[k];
                return BINDINGS_REVERSE_SHARED;
            }
        }
    }

    return BINDINGS_REVERSE_OK;
}

//-------------------------------------------------------------------------
void RestoreInput() {
    const T_DEF_KEY *DEFKEYSDEFAULT;
    DEFKEYSDEFAULT = DefKeysDefault95;

    memcpy(DefKeys, DEFKEYSDEFAULT, sizeof(T_DEF_KEY) * MAX_INPUT_SLOTS);
    memcpy(GamepadKeys, GamepadKeysDefault, sizeof(GamepadKeys));
}
