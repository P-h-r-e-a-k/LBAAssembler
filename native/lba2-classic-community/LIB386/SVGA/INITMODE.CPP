#include <SVGA/INITMODE.H>

#include <SVGA/CLIP.H>
#include <SVGA/SCREEN.H>
#include <SVGA/VIDEO.H>
#include <SYSTEM/LIMITS.H>
#include <SYSTEM/LOG.H>
#include <SYSTEM/LOGPRINT.H>
#include <SYSTEM/WINDOW.H>

// -----------------------------------------------------------------------------
#ifdef __cplusplus
extern "C" {
#endif

// --- Initialization ----------------------------------------------------------
bool InitGraphics(U32 resX, U32 resY, bool fullscreen, bool opengl) {
    if (resY > ADELINE_MAX_Y_RES) {
        return false;
    }

    /* Resolution is reported by the boot log's "Render" line (see InitAdeline). */

    if (!CreateWindowSurface(resX, resY, fullscreen, opengl)) {
        /* No OpenGL here (a driver without it, SDL's dummy video driver): the window without it, software drawing it -- the GL
           backend then fails to attach a context and the boot falls back as for any GL failure (Renderer_InitBootBackend). Before,
           a player who had picked OpenGL on a machine without it had a game that never opened (2026-11-19). */
        if (!opengl) {
            return false;
        }
        Log_Warn("[GL] a window with OpenGL could not be created: opening one without it, drawing in software");
        if (!CreateWindowSurface(resX, resY, fullscreen, false)) {
            return false;
        }
    }

    if (!CreateVideoSurface(resX, resY)) {
        return false;
    }

    if (!CreateScreenMemory(resX, resY)) {
        return false;
    }

    SetClipWindow(0, 0, resX - 1, resY - 1);
    SetClip(0, 0, resX - 1, resY - 1);

    return true;
}

void EndGraphics() {
    // Empty
}

// =============================================================================
#ifdef __cplusplus
}
#endif
