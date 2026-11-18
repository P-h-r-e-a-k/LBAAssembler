#include <SYSTEM/WINDOW.H>

#include <PLATFORM/PLATFORM.H>
#include <SVGA/SCREEN.H> /* ModeDesiredX/Y for the GPU letterbox remap */
#include <SVGA/SDL.H>    /* GpuPresentIsActive: which presenter owns the window */
#include <assert.h>
#include <stdlib.h>

// -----------------------------------------------------------------------------
extern "C" {

// --- Public state ------------------------------------------------------------
bool AppActive = false;
void (*Window_QuitHook)(void) = NULL;

// --- Initialization ----------------------------------------------------------
bool InitWindow(const char *title) { return g_platform.window.init(title); }
void EndWindow() { g_platform.window.shutdown(); }
bool IsWindowInitialized() { return g_platform.window.initialized(); }

// --- The GPU renderer's window (LIB386/PLATFORM/SDL3/WINDOW.CPP, where the window lives) --------------------------------------
void SDL3_SetCreateOpenGL(bool opengl);
SDL_Window *SDL3_GetSDLWindow(void);
bool SDL3_CreateGLContext(void);
bool SDL3_GLMakeCurrent(void);
void SDL3_GLSwapWindow(void);
void SDL3_DestroyGLContext(void);
bool SDL3_RecreateWindow(U32 width, U32 height, bool fullscreen, int shape);
void SDL3_SetGpuSurfaceSize(U32 width, U32 height, bool resizeWindow);
bool SDL3_WindowSize(S32 *width, S32 *height);

SDL_Window *Window_GetSDLWindow(void) { return SDL3_GetSDLWindow(); }
S32 Window_CreateGLContext(void) { return SDL3_CreateGLContext() ? TRUE : FALSE; }
S32 Window_GLMakeCurrent(void) { return SDL3_GLMakeCurrent() ? TRUE : FALSE; }
void Window_GL_SwapWindow(void) { SDL3_GLSwapWindow(); }
void Window_DestroyGLContext(void) { SDL3_DestroyGLContext(); }
bool Window_RecreateWindowForSW(U32 resX, U32 resY, bool fullscreen) { return SDL3_RecreateWindow(resX, resY, fullscreen, 0); }
bool Window_RecreateWindowForGL(U32 resX, U32 resY, bool fullscreen) { return SDL3_RecreateWindow(resX, resY, fullscreen, 1); }
bool Window_RecreateWindowForGPU(U32 resX, U32 resY, bool fullscreen) { return SDL3_RecreateWindow(resX, resY, fullscreen, 2); }
void Window_SetWindowSize(U32 w, U32 h) { SDL3_SetGpuSurfaceSize(w, h, true); }

// --- Interface ---------------------------------------------------------------
bool CreateWindowSurface(U32 resX, U32 resY, bool fullscreen, bool opengl) {
    SDL3_SetCreateOpenGL(opengl);
    return g_platform.window.create(resX, resY, fullscreen);
}
void DestroyWindowSurface() { g_platform.window.destroy(); }
bool ResizeWindowSurface(U32 width, U32 height, bool keepWindowSize) {
    return g_platform.window.resize(width, height, keepWindowSize);
}
bool Window_GetDisplayDimensions(U32 *width, U32 *height) {
    return g_platform.window.display_size(width, height);
}
bool Window_GetPrimaryDisplayDimensions(U32 *width, U32 *height) {
    return g_platform.window.primary_display_size(width, height);
}
void WindowToSurfaceCoords(S32 x, S32 y, S32 *sx, S32 *sy) {
    /* A GPU renderer owns the letterbox (its present goes through its own API, not SDL's renderer, so it centres its own viewport):
       the mouse is mapped through the same centred rectangle */
    S32 winW = 0, winH = 0;
    if (GpuPresentIsActive() && ModeDesiredX > 0 && ModeDesiredY > 0 && SDL3_WindowSize(&winW, &winH) && winW > 0 && winH > 0) {
        const double windowAspect = (double)winW / (double)winH;
        const double contentAspect = (double)ModeDesiredX / (double)ModeDesiredY;
        S32 vpX = 0, vpY = 0, vpW = winW, vpH = winH;
        if (windowAspect > contentAspect) {
            vpW = (S32)((double)winH * contentAspect + 0.5);
            vpW = (vpW + 1) & ~1;
            if (vpW > winW)
                vpW = winW;
            vpX = (winW - vpW) / 2;
        } else {
            vpH = (S32)((double)winW / contentAspect + 0.5);
            vpH = (vpH + 1) & ~1;
            if (vpH > winH)
                vpH = winH;
            vpY = (winH - vpH) / 2;
        }
        S32 px = (S32)((double)(x - vpX) * (double)ModeDesiredX / (double)vpW);
        S32 py = (S32)((double)(y - vpY) * (double)ModeDesiredY / (double)vpH);
        *sx = px < 0 ? 0 : (px >= (S32)ModeDesiredX ? (S32)ModeDesiredX - 1 : px);
        *sy = py < 0 ? 0 : (py >= (S32)ModeDesiredY ? (S32)ModeDesiredY - 1 : py);
        return;
    }
    g_platform.window.surface_coords(x, y, sx, sy);
}
void SetWindowRelativeMouse(bool enabled) { g_platform.window.set_relative_mouse(enabled); }
void SetWindowMouseGrab(bool enabled) { g_platform.window.set_mouse_grab(enabled); }
void SetWindowFullscreen(bool fullscreen) { g_platform.window.set_fullscreen(fullscreen); }
bool GetWindowFullscreen() { return g_platform.window.get_fullscreen(); }
bool Window_IsFullscreen(void) { return g_platform.window.is_fullscreen(); }
bool Window_SupportsWindowedMode(void) { return g_platform.capabilities.windowed; }
bool Window_CanLoseNativeWindow(void) { return g_platform.capabilities.transient_window_loss; }
void Window_SetVSync(int enabled) {
    /* (for the GPU renderer first: it may not exist yet -- the settings come before it -- and is handed the value when it does) */
    GpuPresentSetVSync(enabled ? 1 : 0);
    g_platform.window.set_vsync(enabled != 0);
}
bool Window_SetPresentFilter(int mode) { return g_platform.window.set_present_filter((PlatformPresentFilter)mode); }
void ManageWindow() {}

// (LBA Assembler's own: the SDL3 backend's, where the window lives -- LIB386/PLATFORM/SDL3/WINDOW.CPP)
bool SDL3_IsPresenting(void);
void SDL3_SetWindowTitle(const char *title);
bool SDL3_TakeSettledResize(U32 settleMs, U32 *outPixelW, U32 *outPixelH);
bool SDL3_GetWindowedSize(S32 *outW, S32 *outH, bool *outMaximized);
void SDL3_RestoreWindowedSize(S32 w, S32 h, bool maximized);
void SDL3_SetWindowVisible(bool visible);
bool Window_IsRendererTextureLocked(void) { return SDL3_IsPresenting(); }
void SetWindowTitle(const char *title) { SDL3_SetWindowTitle(title); }
bool Window_TakeSettledResize(U32 settleMs, U32 *outPixelW, U32 *outPixelH) { return SDL3_TakeSettledResize(settleMs, outPixelW, outPixelH); }
bool Window_GetWindowedSize(S32 *outW, S32 *outH, bool *outMaximized) { return SDL3_GetWindowedSize(outW, outH, outMaximized); }
void Window_RestoreWindowedSize(S32 w, S32 h, bool maximized) { SDL3_RestoreWindowedSize(w, h, maximized); }
void Window_SetVisible(bool visible) { SDL3_SetWindowVisible(visible); }

void HandleEventsWindow(const PlatformEvent *event) {
    assert(event != NULL);
    if (!IsWindowInitialized())
        return;
    switch (event->type) {
    case PLATFORM_EVENT_WINDOW_FOCUS_GAINED:
        AppActive = true;
        AppFocusAudio_OnChange(true);
        break;
    case PLATFORM_EVENT_WINDOW_FOCUS_LOST:
        AppActive = false;
        AppFocusAudio_OnChange(false);
        break;
    case PLATFORM_EVENT_QUIT:
        // (the player closed the window: a clean end, not a crash -- the game's hook says so before the exit handlers run)
        if (Window_QuitHook) Window_QuitHook();
        exit(0); // TODO: Implement graceful exit
        break;
    default:
        break;
    }
}

// =============================================================================
}
