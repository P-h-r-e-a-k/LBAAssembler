#ifndef MENU_INPUT_H
#define MENU_INPUT_H

#include <SYSTEM/ADELINE_TYPES.H>

#define MENU_INPUT_COUNT 4
#define MENU_INPUT_UP (1u << 0)
#define MENU_INPUT_DOWN (1u << 1)
#define MENU_INPUT_LEFT (1u << 2)
#define MENU_INPUT_RIGHT (1u << 3)
#define MENU_INPUT_ALL (MENU_INPUT_UP | MENU_INPUT_DOWN | MENU_INPUT_LEFT | MENU_INPUT_RIGHT)

/* Fixed arrows/keypad and left stick/D-pad, sampled without another keyboard/replay poll. */
extern void MenuInput_Poll(S32 isolated);
extern U32 MenuInput_Get(void);
extern U32 MenuInput_Held(void);
extern void MenuInput_Suppress(U32 directions);
extern U32 MenuInput_Suppressed(void);
extern void MenuInput_Reset(U32 suppressed);
extern S32 MenuInput_NavigationKey(void);
/* A step this poll -- the poll a direction is pressed, then held after 350 ms every 90 ms (Fast: every 20 ms, for a slider) -- taken:
   a second read the same poll has none. The menus' one way of stepping (the menu audit, 2026-11-26). */
extern S32 MenuInput_TakeStep(U32 direction);
extern S32 MenuInput_TakeStepFast(U32 direction);
/* Directions held for the next poll that no key of this game's carries: the second player's arrows at a shared keyboard
   (RACEGAME.CPP RaceGame_SharedKeys) */
extern void MenuInput_Inject(U32 directions);

#endif
