#pragma once

#include <SYSTEM/ADELINE_TYPES.H>
#include <PLATFORM/PLATFORM.H>

// -----------------------------------------------------------------------------
#ifdef __cplusplus
extern "C" {
#endif

// --- Public state ------------------------------------------------------------
extern bool AppActive; ///< Application running and in focus status
/// Called as the window is closed (its close button, Alt+F4), before the program exits: the game marks it a clean end
extern void (*Window_QuitHook)(void);

// --- Initialization ----------------------------------------------------------
bool InitWindow(const char *title);
void EndWindow();
bool IsWindowInitialized();

// --- Interface ---------------------------------------------------------------
/** Create the SDL window, renderer, and streaming texture at resX*resY. When
 *  `fullscreen` is true the window is created fullscreen up front (via the
 *  SDL create-time property), avoiding a windowed-then-flipped frame on boot.
 *  When `opengl` is true the window is created as an SDL OpenGL window (the
 *  GL renderer backend's SDL_GL_CreateContext requires that flag at creation
 *  time); the SDL software renderer keeps working on such a window, picking
 *  the OpenGL renderer driver, so the flag is harmless on the software path. */
bool CreateWindowSurface(U32 resX, U32 resY, bool fullscreen, bool opengl);
void DestroyWindowSurface();

/** Resize the existing presentation surface in place, without
 *  destroying the host window itself. Used by the runtime resolution switcher
 *  (docs/RUNTIME_RESOLUTION.md / SOURCES/RES_SWITCH.CPP) so a switch keeps
 *  focus, multi-monitor placement, and (in fullscreen) the fullscreen state
 *  without a destroy/recreate round-trip. Returns true on success. */
/* Resize the framebuffer the window presents. `keepWindowSize` is for render
   sizes the player did not choose (an automatic per-scene downshift): the
   window is left exactly as it is, manual resizes included, and logical
   presentation scales the framebuffer into it either way. A player's own
   resolution choice passes false and moves the window with it. */
bool ResizeWindowSurface(U32 newW, U32 newH, bool keepWindowSize);

/** Query the current display's logical dimensions. Writes both outputs and
 *  returns true on success. Returns false if the backend hasn't initialised a
 *  window yet; *outW and *outH are untouched in that case. */
bool Window_GetDisplayDimensions(U32 *outW, U32 *outH);

/** Query the primary display's logical dimensions WITHOUT a window, for the
 *  boot path that needs the display size before CreateWindowSurface runs.
 *  Requires the platform video subsystem. Returns false (outputs untouched) when video
 *  isn't initialised, no display is reported, or the driver is the headless
 *  dummy/offscreen one, so resolution auto-detection never fires under the
 *  test harness. */
bool Window_GetPrimaryDisplayDimensions(U32 *outW, U32 *outH);
void WindowToSurfaceCoords(S32 windowX, S32 windowY, S32 *surfaceX, S32 *surfaceY);
void SetWindowFullscreen(bool fullscreen);
bool GetWindowFullscreen();
// Actual host window fullscreen state, not the
// cached request flag — for accurate reporting.
bool Window_IsFullscreen(void);

// Whether this platform has a meaningful windowed/fullscreen choice. True on
// desktop; false on always-fullscreen devices (Android phones/TVs), where a
// "Windowed" option is meaningless. Lets UI hide the fullscreen toggle without
// platform #ifdefs at the call site.
bool Window_SupportsWindowedMode(void);

// Whether this platform can refuse to create a window for a reason that passes.
// True on Android, where the window belongs to whatever is in front: with a
// permission screen up, SDL cannot fetch the native window at all, and the same
// call succeeds once the player comes back. False on desktop, where a window
// that cannot be created now will not create itself later either, so a boot
// failure there is final and immediate.
bool Window_CanLoseNativeWindow(void);

// Toggle renderer vsync at runtime. On (the default) paces presents to the display
// refresh; off lets the loop run flat-out. The control harness turns it off so a
// scripted run is not capped at ~60 fps -- presentation only, no effect on rendered
// pixels or simulation state.
void Window_SetVSync(int enabled);

// Choose how the frame is filtered when stretched to the window: one of
// PLATFORM_FILTER_*. Presentation only. False when this backend cannot do the
// mode asked for, in which case the one in use stays.
bool Window_SetPresentFilter(int mode);

// The native SDL window handle. GPU renderer backends (GL, SDL3 GPU) bind to a
// context/device on this window and never go through the SDL software renderer.
// Pointer identity only; the type is forward-declared so the header does not
// pull in SDL3.
typedef struct SDL_Window SDL_Window;
SDL_Window *Window_GetSDLWindow(void);

// OpenGL context lifecycle for the GL renderer backend. The context lives with
// the window (it must be created on the same thread that owns the window);
// the renderer backend consumes it via Window_GLMakeCurrent. CreateGLContext
// requires the window to have been created as an OpenGL window (the present
// mode selection does that when the GL backend is active); it returns FALSE
// otherwise so a wrongly-ordered boot fails at init, not first draw. The
// software and GL presenters cannot share one window, so the Display menu's
// renderer toggles by recreating the window with or without the OpenGL flag.
S32 Window_CreateGLContext(void);
S32 Window_GLMakeCurrent(void);
void Window_GL_SwapWindow(void);
void Window_DestroyGLContext(void);
bool Window_RecreateWindowForGL(U32 resX, U32 resY, bool fullscreen);
bool Window_RecreateWindowForSW(U32 resX, U32 resY, bool fullscreen);
/* Recreate the window plain — no OpenGL flag, no SDL renderer — for the
 * SDL3 GPU backend. The GPU device claims a window that has neither; the
 * software and GL presenters cannot leave their window shape in place, so
 * the renderer switch recreates for each target (see the two above). */
bool Window_RecreateWindowForGPU(U32 resX, U32 resY, bool fullscreen);
void Window_SetWindowSize(U32 w, U32 h);

void SetWindowRelativeMouse(bool enable);

// --- LBA Assembler's own (the SDL3 backend's, LIB386/PLATFORM/SDL3/WINDOW.CPP) ------------------------------------------------
/** Whether a frame is being presented right now (the presentation texture locked, its overlays running): a resolution switch
 *  destroys and recreates that texture, which is only safe outside it -- the console's res_do_switch and RES_SWITCH.CPP wait. */
bool Window_IsRendererTextureLocked(void);
// The window's title bar text, after InitWindow (the race game names its window: RACEGAME.TXT window_title=). Kept for a window made later.
void SetWindowTitle(const char *title);
/* The window resized by the player (or maximised, restored, made full screen): once its size has stayed put for `settleMs` (a
   drag's many size changes come as one), its client size in pixels -- returns true once per settled change. False while nothing
   changed, while it is still changing, while minimised, and under the headless video drivers. The race game fits its render size
   to it (RES_SWITCH.CPP Res_ApplyWindowFit). */
bool Window_TakeSettledResize(U32 settleMs, U32 *outPixelW, U32 *outPixelH);
/* The window's size in screen units and whether it is maximised; false with no window. */
bool Window_GetWindowedSize(S32 *outW, S32 *outH, bool *outMaximized);
/* Give the (windowed) window a size of the player's from an earlier run, kept on the screen and centred on it, maximised if it was. */
void Window_RestoreWindowedSize(S32 w, S32 h, bool maximized);
// Show/hide the window without destroying it: the editor's native renderer (RENDERER_API.CPP) reads its pixels back itself and
// never wants the window on screen. No-op before the window is made.
void Window_SetVisible(bool visible);
// Keep the cursor inside the window (lifted by the host while unfocused).
void SetWindowMouseGrab(bool enable);
void ManageWindow();
void HandleEventsWindow(const PlatformEvent *event);

// Pause audio while the window is not focused. The window layer feeds focus
// changes to AppFocusAudio_OnChange(); the audio/game layer registers what to
// pause (on focus lost) and resume (on focus gained). A once-per-cycle guard
// inside AppFocusAudio_OnChange ensures one pause and one resume per focus
// cycle, mirroring the original PauseMiles/ResumeMiles + `milespaused`. The
// policy lives in FOCUS_AUDIO.CPP (no SDL) so the guard is host-testable.
// Either hook may be NULL.
typedef void (*AppFocusAudioFn)(void);
void SetAppFocusAudioHooks(AppFocusAudioFn onFocusLost, AppFocusAudioFn onFocusGained);
void AppFocusAudio_OnChange(bool gainedFocus);

// =============================================================================
#ifdef __cplusplus
}
#endif
