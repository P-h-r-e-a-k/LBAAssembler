#ifndef INPUT_BINDINGS_H
#define INPUT_BINDINGS_H

/* Which physical key carries which intent, and the fold that turns that into
 * the Input bitfield.
 *
 * Pure lookups over data: no file IO, no polling, no engine state. That is the
 * line this module is cut along, and what lets tests/input_bindings drive it
 * with nothing underneath but DefineInputKeys. INPUT.CPP keeps the halves that
 * cannot be: reading and writing the config file, and sampling the keyboard,
 * the pad and the console every frame.
 *
 * INPUT.H includes this header, so the eighteen files that already include it
 * see the tables as before. Narrowing that to the handful that actually rebind
 * keys belongs with the work that gives the config screen and the cfg reader
 * one entry point each, not with the move.
 */

#include <SYSTEM/ADELINE_TYPES.H>

// The number of bindable actions. The first 32 are the Input word (INPUT.H's
// I_* bits, one per slot); the last four are the spell bits of a second word,
// which is why they cannot share the folded table -- I_PINGOUIN is (1 << 0),
// the same bit as I_UP. PERSO.CPP reads those four with a direct CheckKey.
//
// Not spelled MAX_INPUT: that name belongs to POSIX. <linux/limits.h> gives it
// the value 255, the size of the terminal type-ahead buffer, and Bionic reaches
// it through <limits.h> where desktop glibc does not. A translation unit that
// includes this header ahead of a system header would then declare these tables
// at one size and define them at another, which the compiler reports as a
// redefinition with a different type. A name this module owns cannot be
// clobbered by include order, and that, rather than the number, is the point.
#define MAX_INPUT_SLOTS 36

typedef struct {
    U32 Key1;
    U32 Key2;
} T_DEF_KEY;

// The Windows 95 default layout: what RestoreInput installs, and the value a
// binding the config file leaves unset falls back to. Const like the pad table
// below it, since a default nothing may edit is the point of having one.
extern const T_DEF_KEY DefKeysDefault95[MAX_INPUT_SLOTS + 1];

extern T_DEF_KEY DefKeys[MAX_INPUT_SLOTS + 1];
extern T_DEF_KEY MemoDefKeys[MAX_INPUT_SLOTS + 1];

// tableau de configuration des touches utilisé au cours du jeu
// Read by nothing in this build: the combined table InitInput hands to
// DefineInputKeys replaced it. Moved with the tables it belongs to rather than
// dropped, so the move stays a move.
extern U32 TabInputBit[MAX_INPUT_SLOTS * 2 + 1];

// Parallel gamepad binding table: Key1 / Key2 are alternate bindings (OR),
// same semantics as DefKeys (see KEYBOARD_KEYS.H for K_GAMEPAD_*). 0 =
// unbound. Folded into the combined input table by InitInput() alongside
// DefKeys, so the pad flows through GetInput()'s NoRepeatInput edge-filter.
extern T_DEF_KEY GamepadKeys[MAX_INPUT_SLOTS];
extern const T_DEF_KEY GamepadKeysDefault[MAX_INPUT_SLOTS];

// How many entries InitInput() handed to DefineInputKeys.
extern U32 NbInput;

//-------------------------------------------------------------------------
extern void InitInput(void);
//-------------------------------------------------------------------------
extern void RestoreInput(void);
//-------------------------------------------------------------------------

// In the car: a second set of keys and pad buttons for the car's own actions (docs/plan/REBIND_PLAN.md; the user, 2026-11-25: "perhaps
// the same key binded one way for when twinsen is in a car and another when he isn't"). Indexed like the slots above -- 0-31 the Input
// word's bits -- plus two of the car's own with no Input bit, read with Bindings_CarHeld: the handbrake and the race's view (Z and V,
// a pad's B and Back, until then fixed in RACEMOD.CPP). Only the slots on the car's page (CarPageRows) are the car's; in the car every
// other slot keeps its on-foot keys but for any the car's page uses.
#define CAR_SLOT_HANDBRAKE 36
#define CAR_SLOT_VIEW 37
#define CAR_SLOTS 38
#define CAR_PAGE_ROWS 11

extern T_DEF_KEY CarKeys[CAR_SLOTS];
extern T_DEF_KEY CarPadKeys[CAR_SLOTS];
extern const T_DEF_KEY CarKeysDefault[CAR_SLOTS];
extern const T_DEF_KEY CarPadKeysDefault[CAR_SLOTS];
// The car page's rows, in order: each its slot and its words
extern const S32 CarPageSlot[CAR_PAGE_ROWS];
extern const char *const CarPageLabel[CAR_PAGE_ROWS];
// Whether a slot is one of the car's own
extern S32 Bindings_CarSlot(S32 slot);
// The car's keys back to the defaults (keyboard, pad or both)
extern void RestoreCarKeys(S32 keyboard, S32 pad);

// Which set the Input word comes from: non-zero the car's. A change folds the table again, and the keys held at that moment are ignored
// until they are let go (Bindings_FilterSwitch) -- the key that put him in the car does nothing in it.
extern void Bindings_UseCar(S32 car);
extern S32 Bindings_InCar(void);
// After GetInput: the Input word less the keys held across a change of set
extern U32 Bindings_FilterSwitch(U32 input);
// One of the car's own slots (CAR_SLOT_*) held: any of its keys or buttons, or a press put in this poll (Bindings_Inject: the second
// player's keys at a shared keyboard)
extern S32 Bindings_CarHeld(S32 slot);
extern void Bindings_Inject(S32 slot);
extern void Bindings_ClearInjected(void);

// Two players at one keyboard (RACEGAME.CPP RaceGame_SharedKeys, NETRACE.CPP RaceNet_SharedKeys): each player's driving keys, rows
// 0-6 player 1's (on the left: W A S D, Q, E, R), 7-13 player 2's (the arrows, Right Ctrl, Right Shift, /) -- accelerate, brake, left,
// right, item, handbrake, view. Keyboard keys only (each player's pad is their own), saved in lba2.cfg as TwoKeys<row>_1/_2.
#define TWO_ROWS 7
#define TWO_KEYS (2 * TWO_ROWS)
#define TWO_ACCEL 0
#define TWO_BRAKE 1
#define TWO_LEFT 2
#define TWO_RIGHT 3
#define TWO_ITEM 4
#define TWO_HANDBRAKE 5
#define TWO_VIEW 6
extern T_DEF_KEY TwoKeys[TWO_KEYS];
extern const T_DEF_KEY TwoKeysDefault[TWO_KEYS];
extern const S32 TwoKeySlot[TWO_KEYS];
extern const char *const TwoKeyLabel[TWO_KEYS];
extern void RestoreTwoKeys(void);

// The other direction: which keys carry a slot, and which slot a key carries.
//
// InitInput folds the two tables into the flat (key, mask) array GetInput scans,
// which answers "is this slot held" and nothing else. Three readers want the
// question the other way round and each does it by hand today: the key-config
// screen indexes DefKeys positionally to say what a slot is bound to, a profile
// system would need the same to name a binding, and anything re-expressing an
// action as a key press needs a key to press.
//
// Slot indices run 0..MAX_INPUT_SLOTS-1 and cover the spells at 32-35, which the
// combined table stops short of: they have no Input bit (see INPUT_TABLE_SLOTS
// in the .CPP) but they are bound the same way and PERSO.CPP polls them with the
// same OR of four keys. The reverse direction has no reason to inherit that
// ceiling, so it does not.

// Keys bound to `slot`, written to `out` in the order the forward fold reads
// them: keyboard Key1, keyboard Key2, pad Key1, pad Key2, skipping unbound
// zeroes. Returns how many were written, so 0 means the slot cannot be
// expressed as a key press at all.
extern S32 Bindings_KeysForSlot(S32 slot, U32 *out, S32 max);

// The slot `key` is bound to, or -1 for a key no slot claims. The first match in
// slot order when a table binds one key twice, which Bindings_CheckReversible
// is what refuses.
extern S32 Bindings_SlotForKey(U32 key);

// Whether the live tables can be read backwards without changing meaning, which
// is not a property the forward direction needs and not one a cfg guarantees.
// Two ways it fails, and each is a different repair, so they are reported apart:
//
//   BINDINGS_REVERSE_SHARED   one key on two slots, so pressing it for the slot
//                             that is held also raises the slot that is not.
//   BINDINGS_REVERSE_UNBOUND  a slot with all four keys zero, so a held slot has
//                             no key to press.
//
// `badSlot` and `badKey` receive the first offender, either may be NULL.
#define BINDINGS_REVERSE_OK 0
#define BINDINGS_REVERSE_SHARED 1
#define BINDINGS_REVERSE_UNBOUND 2
extern S32 Bindings_CheckReversible(S32 *badSlot, U32 *badKey);

#endif // INPUT_BINDINGS_H
