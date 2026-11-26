#include "MENU_INPUT.H"

#include <SYSTEM/KEYBOARD.H>
#include <SYSTEM/KEYBOARD_KEYS.H>
#include <SYSTEM/TIMER.H>

static U32 s_held = 0;
static U32 s_input = 0;
static U32 s_suppressed = 0;

// One way for every menu to step (the menu audit, 2026-11-26 -- the user, "tightening up the controls on the menu"): a direction steps
// the poll it is pressed, then, held, again after MENU_REPEAT_DELAY and every MENU_REPEAT_RATE (a slider's every MENU_REPEAT_FAST) --
// the keys, the stick and the d-pad alike. Before, the game's menus stepped once a press, the save list every 150 ms from the first, the
// remap pages 400 then 40, and a volume slider every 5 ms from the first: a tap moved it several steps.
#define MENU_REPEAT_DELAY 350
#define MENU_REPEAT_RATE 90
#define MENU_REPEAT_FAST 20
static U32 s_live = 0;                      // the directions held and not suppressed last poll
static U32 s_step = 0, s_stepFast = 0;      // the steps this poll (taken by MenuInput_TakeStep / TakeStepFast)
static U32 s_nextAt[MENU_INPUT_COUNT], s_nextFastAt[MENU_INPUT_COUNT];
static U32 s_injected = 0;                  // directions put in for this poll (MenuInput_Inject)

void MenuInput_Poll(S32 isolated) {
    s_held = 0;
    if (CheckKey(K_UP) || CheckKey(K_NUMPAD_8) ||
        CheckKey(K_GAMEPAD_LSTICK_UP) || CheckKey(K_GAMEPAD_DPAD_UP))
        s_held |= MENU_INPUT_UP;
    if (CheckKey(K_DOWN) || CheckKey(K_NUMPAD_2) ||
        CheckKey(K_GAMEPAD_LSTICK_DOWN) || CheckKey(K_GAMEPAD_DPAD_DOWN))
        s_held |= MENU_INPUT_DOWN;
    if (CheckKey(K_LEFT) || CheckKey(K_NUMPAD_4) ||
        CheckKey(K_GAMEPAD_LSTICK_LEFT) || CheckKey(K_GAMEPAD_DPAD_LEFT))
        s_held |= MENU_INPUT_LEFT;
    if (CheckKey(K_RIGHT) || CheckKey(K_NUMPAD_6) ||
        CheckKey(K_GAMEPAD_LSTICK_RIGHT) || CheckKey(K_GAMEPAD_DPAD_RIGHT))
        s_held |= MENU_INPUT_RIGHT;
    s_held |= s_injected;
    s_injected = 0;
    s_suppressed &= s_held;
    s_step = s_stepFast = 0;
    if (isolated) {
        s_suppressed |= s_held;
        s_held = s_input = 0;
        s_live = 0;
        return;
    }
    s_input = s_held & ~s_suppressed;
    const U32 now = TimerSystemHR;
    for (S32 d = 0; d < MENU_INPUT_COUNT; d++) {
        const U32 bit = 1u << d;
        if (!(s_input & bit))
            continue;
        if (!(s_live & bit)) {
            s_step |= bit;
            s_stepFast |= bit;
            s_nextAt[d] = s_nextFastAt[d] = now + MENU_REPEAT_DELAY;
            continue;
        }
        if ((S32)(now - s_nextAt[d]) >= 0) {
            s_step |= bit;
            s_nextAt[d] = now + MENU_REPEAT_RATE;
        }
        if ((S32)(now - s_nextFastAt[d]) >= 0) {
            s_stepFast |= bit;
            s_nextFastAt[d] = now + MENU_REPEAT_FAST;
        }
    }
    s_live = s_input;
}

U32 MenuInput_Get(void) {
    return s_input;
}

U32 MenuInput_Held(void) {
    return s_held;
}

void MenuInput_Suppress(U32 directions) {
    s_suppressed |= directions;
    s_input &= ~directions;
    s_step &= ~directions;
    s_stepFast &= ~directions;
    s_live &= ~directions;
}

U32 MenuInput_Suppressed(void) {
    return s_suppressed;
}

void MenuInput_Reset(U32 suppressed) {
    s_input = s_held = 0;
    s_step = s_stepFast = s_live = 0;
    s_suppressed = suppressed & MENU_INPUT_ALL;
}

S32 MenuInput_TakeStep(U32 direction) {
    if (!(s_step & direction))
        return 0;
    s_step &= ~direction;
    s_stepFast &= ~direction;
    return 1;
}

S32 MenuInput_TakeStepFast(U32 direction) {
    if (!(s_stepFast & direction))
        return 0;
    s_stepFast &= ~direction;
    s_step &= ~direction;
    return 1;
}

void MenuInput_Inject(U32 directions) {
    s_injected |= directions & MENU_INPUT_ALL;
}

S32 MenuInput_NavigationKey(void) {
    if (s_input & MENU_INPUT_UP)
        return K_UP;
    if (s_input & MENU_INPUT_DOWN)
        return K_DOWN;
    if (s_input & MENU_INPUT_LEFT)
        return K_LEFT;
    if (s_input & MENU_INPUT_RIGHT)
        return K_RIGHT;
    return 0;
}
