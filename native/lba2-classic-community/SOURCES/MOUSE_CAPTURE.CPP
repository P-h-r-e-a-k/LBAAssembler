#include "MOUSE_CAPTURE.H"

#include "CONSOLE/CONSOLE.H" /* Console_IsOpen */
#include <SYSTEM/LOG.H>
#include <SYSTEM/WINDOW.H>

#include <stdlib.h> /* atexit, getenv */

S32 MouseConfine = MOUSE_CONFINE_DEFAULT;

/* Presents since the camera last asked to orbit. One present without a
 * request is allowed, so a gameplay frame that presents twice does not drop
 * relative mode in between; a screen with its own loop never asks, and ends it
 * on its second frame. */
#define ORBIT_GRACE_PRESENTS 1
static S32 s_presentsSinceOrbit = ORBIT_GRACE_PRESENTS + 1;

static T_MOUSE_CAPTURE s_current = MOUSE_CAPTURE_FREE;
static S32 s_applied = 0;

/* (LBA Assembler's own) Whether this window sits inside another program's -- the editor's Play, which asks for an off-screen
   start with LBA2_WINDOW_POS and takes the window over as a child of its own (WINDOW.CPP, RES_SWITCH.CPP Res_FitWanted). There
   the window is still "full screen" to SDL -- it was created so from lba2.cfg's DisplayFullScreen, the boot log says
   "fullscreen" -- but the screen is the editor's, with its own panels and its Stop button beside the game, and the
   mouse_confine rule for full screen clipped the cursor to the game's corner of it whenever SDL took the window for the one in
   front. Confining is the editor's to decide there, and it never does; the camera's orbit (relative motion while the right
   button is held) is a gesture the player makes and ends, and is kept. */
static S32 Embedded(void) {
    static S32 embedded = -1;
    if (embedded < 0)
        embedded = getenv("LBA2_WINDOW_POS") != NULL ? 1 : 0;
    return embedded;
}

/* Gives the mouse back as the program ends, whichever way it ends cleanly (atexit, registered the first time the mouse is
   taken). The engine never closes its window nor quits SDL on the way out -- TheEnd is an exit() -- so nothing of SDL's own
   undoes a grab: SDL lifts its clip when its window loses the focus, and a window that is simply gone with its process never
   does. A cursor clip is the desktop's, not the window's: it outlives the window and the program that set it until something
   else changes it, and in the editor nothing does -- its window was in front all along -- which is what the user met,
   2026-11-03, after a game over in Play (the engine's run ends there: MainLoop returns 2, the harness path exits): "my mouse was
   trapped inside the game viewport even though the game had seemingly ended". */
static void ReleaseAtExit(void) {
    if (!s_applied || s_current == MOUSE_CAPTURE_FREE)
        return;
    Log_Info("[mouse] the program ends with the mouse %s: given back", MouseCapture_Name(s_current));
    if (s_current == MOUSE_CAPTURE_RELATIVE)
        SetWindowRelativeMouse(false);
    SetWindowMouseGrab(false);
    s_current = MOUSE_CAPTURE_FREE;
}

T_MOUSE_CAPTURE MouseCapture_Decide(S32 confine, S32 fullscreen, S32 consoleOpen, S32 orbit) {
    if (orbit && !consoleOpen)
        return MOUSE_CAPTURE_RELATIVE;
    if (confine == MOUSE_CONFINE_ALWAYS || (confine == MOUSE_CONFINE_FULLSCREEN && fullscreen))
        return MOUSE_CAPTURE_CONFINED;
    return MOUSE_CAPTURE_FREE;
}

void MouseCapture_RequestOrbit(void) { s_presentsSinceOrbit = 0; }

void MouseCapture_CarryOrbit(void) {
    if (s_presentsSinceOrbit <= ORBIT_GRACE_PRESENTS)
        s_presentsSinceOrbit = 0;
}

void MouseCapture_Update(void) {
    const S32 orbit = s_presentsSinceOrbit <= ORBIT_GRACE_PRESENTS;
    if (s_presentsSinceOrbit <= ORBIT_GRACE_PRESENTS)
        s_presentsSinceOrbit++;

    const T_MOUSE_CAPTURE want =
        MouseCapture_Decide(MouseConfine, Window_IsFullscreen() && !Embedded() ? 1 : 0, Console_IsOpen(), orbit);
    if (s_applied && want == s_current)
        return;
    if (!s_applied)
        atexit(ReleaseAtExit);

    const bool relative = want == MOUSE_CAPTURE_RELATIVE;
    const bool grab = want != MOUSE_CAPTURE_FREE;
    if (!s_applied || relative != (s_current == MOUSE_CAPTURE_RELATIVE))
        SetWindowRelativeMouse(relative);
    if (!s_applied || grab != (s_current != MOUSE_CAPTURE_FREE))
        SetWindowMouseGrab(grab);
    s_current = want;
    s_applied = 1;
}

T_MOUSE_CAPTURE MouseCapture_Current(void) { return s_current; }

const char *MouseCapture_Name(T_MOUSE_CAPTURE mode) {
    switch (mode) {
    case MOUSE_CAPTURE_CONFINED:
        return "confined";
    case MOUSE_CAPTURE_RELATIVE:
        return "relative";
    default:
        return "free";
    }
}
