#ifndef GAMEMENU_MOUSE_H
#define GAMEMENU_MOUSE_H

#include <SYSTEM/ADELINE_TYPES.H>

#ifdef __cplusplus
extern "C" {
#endif

void MenuMouseAcquire(void);
void MenuMouseRelease(void);
void MenuMouseApplyCursorSprite(void);
void GameMenuMouseResetPress(void);
void GameMenuMousePollFocus(void);

/// Non-zero when menu mouse cursor should be active (config + refcount).
S32 MenuMouse_GetRefCount(void);

/// The gap between a menu's rows: a list of nine (the race game's, in a race) closes up to fit a 480-tall screen.
S32 GameMenuRowSpace(S32 nb);

/// A menu's rows on the screen: the first shown's centre y, the first shown, how many (fewer than all: the list scrolls).
void GameMenuLayout(const U16 *ptrmenu, S32 *ytop, S32 *first, S32 *vis);
/// A scrolling menu's bar, right of its rows.
void GameMenuScrollbarRect(const U16 *ptrmenu, S32 *x0, S32 *y0, S32 *x1, S32 *y1);
/// Scroll a menu to show rows from `first`, the choice kept among them. Non-zero: changed.
S32 GameMenuScrollTo(U16 *ptrmenu, S16 *selected, S32 first);

/// Hit-test which menu row (0..nb-1) contains (mx,my), or -1. Mirrors DrawGameMenu geometry.
S32 GameMenuHitTestRow(const U16 *ptrmenu, U32 mx, U32 my, const U16 *gameMainMenuPtr);

/**
 * After MyGetInput in DoGameMenu: hover, press/release, optional return.
 * @return -1 = no action; >= 0 = return this menu mess num (same as Enter on current row).
 */
S32 GameMenuMouseProcessDoGameMenu(U16 *ptrmenu, S16 *selected, S32 *flag);

#ifdef __cplusplus
}
#endif

#endif
