#include "GAMEMENU_MOUSE.H"

#include "C_EXTERN.H"
#include "UI_LAYOUT.H" /* UI_CENTER_X, UI_VCENTER_OFS */
#include "RACEGAME.H"  /* RaceGame_RowDisabled */

#include <SYSTEM/MOUSE.H>
#include <SYSTEM/WINDOW.H>
#include <SVGA/SCREEN.H> /* ModeDesiredX/Y for the centred + floated hit-test */

extern U16 GameMainMenu[];

#define GM_HAUTEUR_STANDARD 50
#define GM_MENU_SPACE 6
// Menu centre X. Must track DrawGameMenu's draw (DrawOneChoice at the same
// anchor); was hardcoded 320, so mouse clicks missed the menu at any width
// != 640. UI_CENTER_X is that anchor named once in UI_LAYOUT.H, so the hit
// test cannot drift from the draw again.
#define GM_MENU_XMID UI_CENTER_X
#define GM_MENU_HALF_WIDTH 275

static int s_menuMouseRefCount = 0;
static S32 s_pressRow = -1;
static S32 s_prevClick = 0;
static bool s_prevAppActive = true;

void GameMenuMouseResetPress(void) {
    s_pressRow = -1;
    s_prevClick = 0;
}

void MenuMouseApplyCursorSprite(void) {
    /* No-op: menus use the default BinGphMouse arrow set by InitMouse(). */
}

void MenuMouseAcquire(void) {
    if (!FlagMenuMouse)
        return;
    s_menuMouseRefCount++;
    if (s_menuMouseRefCount == 1) {
        ShowMouseAfterFirstMotion();
    }
}

void MenuMouseRelease(void) {
    if (!FlagMenuMouse)
        return;
    if (s_menuMouseRefCount > 0)
        s_menuMouseRefCount--;
    if (s_menuMouseRefCount == 0) {
        GameMenuMouseResetPress();
        ShowMouse(false);
        InitMouse();
    }
}

S32 MenuMouse_GetRefCount(void) {
    return s_menuMouseRefCount;
}

void GameMenuMousePollFocus(void) {
    bool active = AppActive;

    if (s_prevAppActive && !active)
        GameMenuMouseResetPress();
    s_prevAppActive = active;
}

S32 GameMenuRowSpace(S32 nb) {
    (void)nb;
    return GM_MENU_SPACE;
}

// A menu with more rows than the screen has room for scrolls, rows at their usual size and gap (the user, 2026-11-14: "We can add a
// scrollbar if menus require it, we don't have to force them all to fit" -- "There's already a scrolling list on "Track select" that we can
// re-use": its bar down the list's right edge, the thumb the rows shown). Room: the screen, less a margin.
#define GM_MENU_ROOM_TOP 10
#define GM_MENU_ROOM_BOTTOM 10
#define GM_SCROLLBAR_GAP 8
#define GM_SCROLLBAR_WIDTH 16
static const U16 *s_scrollMenu = NULL;
static S32 s_scrollNb = -1;
static S32 s_scrollFirst = 0;

void GameMenuLayout(const U16 *ptrmenu, S32 *ytop, S32 *first, S32 *vis) {
    const S32 nb = ptrmenu[1], ycenter = ptrmenu[2], sel = ptrmenu[0];
    const S32 step = GM_HAUTEUR_STANDARD + GM_MENU_SPACE;
    const S32 screenH = 480 + 2 * UI_VCENTER_OFS;
    S32 fit = (screenH - GM_MENU_ROOM_TOP - GM_MENU_ROOM_BOTTOM + GM_MENU_SPACE) / step;
    if (fit < 1) fit = 1;
    S32 y = ycenter == 0 ? GM_HAUTEUR_STANDARD / 2 + 10 + UI_VCENTER_OFS : 0;
    if (ycenter == 0) {
        const S32 below = (screenH - GM_MENU_ROOM_BOTTOM - (y - GM_HAUTEUR_STANDARD / 2) + GM_MENU_SPACE) / step;
        if (below < fit) fit = below < 1 ? 1 : below;
    }
    const S32 v = nb < fit ? nb : fit;
    if (ptrmenu != s_scrollMenu || nb != s_scrollNb) {
        s_scrollMenu = ptrmenu;
        s_scrollNb = nb;
        s_scrollFirst = 0;
    }
    S32 f = s_scrollFirst;
    if (sel < f) f = sel;
    if (sel >= f + v) f = sel - v + 1;
    if (f > nb - v) f = nb - v;
    if (f < 0) f = 0;
    s_scrollFirst = f;
    if (ycenter != 0) {
        // (as DrawGameMenu always placed them: the list's height centred on the row y given; then kept on the screen)
        y = ycenter + UI_VCENTER_OFS - (GM_HAUTEUR_STANDARD * v + (v - 1) * GM_MENU_SPACE) / 2;
        const S32 bottom = y + (v - 1) * step + GM_HAUTEUR_STANDARD / 2;
        if (bottom > screenH - 4) y -= bottom - (screenH - 4);
        if (y - GM_HAUTEUR_STANDARD / 2 < 4) y = 4 + GM_HAUTEUR_STANDARD / 2;
    }
    *ytop = y;
    *first = f;
    *vis = v;
}

void GameMenuScrollbarRect(const U16 *ptrmenu, S32 *x0, S32 *y0, S32 *x1, S32 *y1) {
    S32 y, first, vis;
    GameMenuLayout(ptrmenu, &y, &first, &vis);
    *x0 = GM_MENU_XMID + GM_MENU_HALF_WIDTH + GM_SCROLLBAR_GAP;
    *x1 = *x0 + GM_SCROLLBAR_WIDTH;
    *y0 = y - GM_HAUTEUR_STANDARD / 2;
    *y1 = y + (vis - 1) * (GM_HAUTEUR_STANDARD + GM_MENU_SPACE) + GM_HAUTEUR_STANDARD / 2;
}

// The rows shown from `first` on: the choice kept among them (moved to the nearest one that can be chosen). Non-zero: anything changed.
S32 GameMenuScrollTo(U16 *ptrmenu, S16 *selected, S32 first) {
    S32 y, f, vis;
    GameMenuLayout(ptrmenu, &y, &f, &vis);
    const S32 nb = ptrmenu[1];
    if (vis >= nb)
        return 0;
    if (first > nb - vis) first = nb - vis;
    if (first < 0) first = 0;
    S32 sel = *selected;
    if (first == f)
        return 0;
    s_scrollFirst = first;
    if (sel < first) sel = first;
    if (sel >= first + vis) sel = first + vis - 1;
    for (S32 k = 0; k < vis && RaceGame_RowDisabled(ptrmenu[4 + 1 + sel * 2]); k++)
        sel = sel + 1 < first + vis ? sel + 1 : first;
    *selected = (S16)sel;
    ptrmenu[0] = (U16)sel;
    return 1;
}

S32 GameMenuHitTestRow(const U16 *ptrmenu, U32 mx, U32 my, const U16 *gameMainMenuPtr) {
    S32 nb = ptrmenu[1];
    S32 ycenter = ptrmenu[2];
    S32 y;
    S32 n;
    S32 x0 = GM_MENU_XMID - GM_MENU_HALF_WIDTH;
    S32 x1 = GM_MENU_XMID + GM_MENU_HALF_WIDTH;

    if (nb <= 0)
        return -1;

    // Y must track DrawGameMenu's (GameMenuLayout: the rows shown of a scrolling list)
    S32 first, vis;
    (void)ycenter;
    GameMenuLayout(ptrmenu, &y, &first, &vis);

    for (n = first; n < first + vis; n++) {
        S32 num = ptrmenu[4 + n * 2 + 1];

#ifdef DEMO
        if (gameMainMenuPtr != NULL && ptrmenu == gameMainMenuPtr && (num == 72 || num == 73)) {
            y += GM_HAUTEUR_STANDARD + GameMenuRowSpace(nb);
            continue;
        }
#else
        (void)gameMainMenuPtr;
#endif
        // (a race game's greyed row: no hover, no click -- RACEGAME.CPP)
        if (RaceGame_RowDisabled(num)) {
            y += GM_HAUTEUR_STANDARD + GameMenuRowSpace(nb);
            continue;
        }

        {
            S32 y0 = y - GM_HAUTEUR_STANDARD / 2;
            S32 y1 = y + GM_HAUTEUR_STANDARD / 2;
            if ((S32)mx >= x0 && (S32)mx <= x1 && (S32)my >= y0 && (S32)my <= y1)
                return n;
        }
        y += GM_HAUTEUR_STANDARD + GameMenuRowSpace(nb);
    }
    return -1;
}

S32 GameMenuMouseProcessDoGameMenu(U16 *ptrmenu, S16 *selected, S32 *flag) {
    S32 row;
    S32 cur;
    S32 leftDown;
    S32 prevLeft;
    S32 leftDownEdge;
    S32 leftUpEdge;
    S32 rel;
    S32 mess;
    bool mouseMoved;

    if (!FlagMenuMouse || s_menuMouseRefCount <= 0)
        return -1;

    GameMenuMousePollFocus();
    mouseMoved = MouseConsumeMotionFlag();

    cur = Click;
    leftDown = (cur & 1) != 0;
    prevLeft = (s_prevClick & 1) != 0;
    leftDownEdge = leftDown && !prevLeft;
    leftUpEdge = !leftDown && prevLeft;
    s_prevClick = cur;

    // (a scrolling list's bar: a click or a drag on it scrolls there, as Track select's)
    {
        S32 bx0, by0, bx1, by1, yt, first, vis;
        GameMenuLayout(ptrmenu, &yt, &first, &vis);
        if (vis < ptrmenu[1]) {
            GameMenuScrollbarRect(ptrmenu, &bx0, &by0, &bx1, &by1);
            if (leftDown && s_pressRow < 0 && (S32)MouseX >= bx0 - 6 && (S32)MouseX <= bx1 + 6 && (S32)MouseY >= by0 && (S32)MouseY <= by1) {
                const S32 to = ((S32)MouseY - by0) * (ptrmenu[1] - vis + 1) / (by1 - by0 + 1);
                if (GameMenuScrollTo(ptrmenu, selected, to))
                    *flag = 1;
                return -1;
            }
        }
    }

    row = GameMenuHitTestRow(ptrmenu, MouseX, MouseY, GameMainMenu);

    if (leftDownEdge) {
        if (row >= 0) {
            s_pressRow = row;
            if (*selected != row) {
                *selected = row;
                *flag = 1;
                ptrmenu[0] = *selected;
            }
        }
    }

    if (leftDown && s_pressRow >= 0) {
        if (*selected != s_pressRow) {
            *selected = s_pressRow;
            *flag = 1;
            ptrmenu[0] = *selected;
        }
    } else if (!leftDown && s_pressRow < 0) {
        /* Hover: only update selection when the mouse actually moved. */
        if (mouseMoved && row >= 0 && row != *selected) {
            *selected = row;
            *flag = 1;
            ptrmenu[0] = *selected;
        }
    }

    if (leftUpEdge && s_pressRow >= 0) {
        rel = GameMenuHitTestRow(ptrmenu, MouseX, MouseY, GameMainMenu);
        if (rel == s_pressRow) {
            mess = ptrmenu[4 + 1 + s_pressRow * 2];
            s_pressRow = -1;
            return mess;
        }
        s_pressRow = -1;
    }

    return -1;
}
