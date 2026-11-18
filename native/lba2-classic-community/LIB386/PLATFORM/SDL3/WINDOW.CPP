#include "PRIVATE.H"

#include <PLATFORM/STAGING_BUFFER.H>
#include <SYSTEM/ANDROID.H>

#include <assert.h>
#include <stdio.h>
#include <stdlib.h>

namespace {
bool s_initialized = false;
const char *s_title = NULL;
SDL_Window *s_window = NULL;
SDL_Renderer *s_renderer = NULL;
SDL_Texture *s_texture = NULL;
uint32_t s_width = 0;
uint32_t s_height = 0;
bool s_fullscreen = false;
SDL_ScaleMode s_scaleMode = SDL_SCALEMODE_NEAREST;
/* (LBA Assembler's own) a frame being presented -- the texture locked, its overlays running (SDL3_IsPresenting); the window's size
   changed and not yet taken (SDL3_TakeSettledResize), and when it last did */
bool s_presenting = false;
bool s_resizePending = false;
Uint64 s_resizeAt = 0;
#ifdef LBA2_SDL3_TESTS
void (*s_presentationProbe)(SDL_Renderer *) = NULL;
bool (*s_fullscreenProbe)(SDL_Window *, bool) = NULL;
#endif
#if defined(__ANDROID__) || defined(LBA2_SDL3_TEST_STAGING)
StagingBuffer s_staging = {NULL, 0, 0};
#endif

void HideGameCursor(void) {
    SDL_HideCursor();
    /* Some WMs show the default arrow until the cursor is cleared. */
    SDL_SetCursor(NULL);
}

/* Scale filter applied when the framebuffer is stretched to the window.

   NEAREST stays the default: it is the historical output exactly. It is also
   uneven at a non-integer window scale -- 1728x1080 shown on 3840x2400 is 20/9,
   so a source pixel becomes two output pixels here and three there, and that
   irregularity is what reads as chunkiness. LINEAR removes it by softening the
   fixed-pixel 2D layer along with everything else. PIXELART keeps texel
   interiors crisp and interpolates only across the texel seam.

   PIXELART needs SDL 3.4 at both compile and run time, which is why the two
   guards differ: the compile one keeps older SDL building, the runtime one
   catches headers and runtime disagreeing -- SDL_SetTextureScaleMode simply
   returns false there and would otherwise leave the texture on a mode nobody
   chose. */
bool ResolveScaleMode(PlatformPresentFilter mode, SDL_ScaleMode *out) {
    switch (mode) {
    case PLATFORM_FILTER_NEAREST:
        *out = SDL_SCALEMODE_NEAREST;
        return true;
    case PLATFORM_FILTER_LINEAR:
        *out = SDL_SCALEMODE_LINEAR;
        return true;
    case PLATFORM_FILTER_PIXELART:
#if SDL_VERSION_ATLEAST(3, 4, 0)
        if (SDL_GetVersion() >= SDL_VERSIONNUM(3, 4, 0)) {
            *out = SDL_SCALEMODE_PIXELART;
            return true;
        }
#endif
        return SDL_SetError("The pixelart filter needs SDL 3.4 or newer");
    }
    return SDL_SetError("Unknown present filter %d", (int)mode);
}

/* Apply the chosen filter and say so if the runtime refused it, rather than
   leaving the texture on a mode nobody asked for. */
void ApplyScaleMode(SDL_Texture *texture) {
    if (!SDL_SetTextureScaleMode(texture, s_scaleMode)) {
        Platform_Log(PLATFORM_LOG_WARN, "Unable to set texture scale mode.\n"
                                        "\tSDL message: %s",
                     SDL_GetError());
    }
}

void SetLogicalPresentation(uint32_t width, uint32_t height) {
    if (!SDL_SetRenderLogicalPresentation(s_renderer, (int)width, (int)height,
                                          SDL_LOGICAL_PRESENTATION_LETTERBOX)) {
        Platform_Log(PLATFORM_LOG_WARN, "Unable to set SDL logical presentation to %ux%u.\n"
                                        "\tSDL message: %s",
                     width, height, SDL_GetError());
    }
}

bool PresentTexture(void) {
    if (!SDL_SetRenderDrawColor(s_renderer, 0, 0, 0, 255) ||
        !SDL_RenderClear(s_renderer) ||
        !SDL_RenderTexture(s_renderer, s_texture, NULL, NULL)) {
        Platform_Log(PLATFORM_LOG_ERROR, "Unable to present SDL frame: %s", SDL_GetError());
        return false;
    }
#ifdef LBA2_SDL3_TESTS
    if (s_presentationProbe)
        s_presentationProbe(s_renderer);
#endif
    if (!SDL_RenderPresent(s_renderer)) {
        Platform_Log(PLATFORM_LOG_ERROR, "Unable to present SDL frame: %s", SDL_GetError());
        return false;
    }
    return true;
}

bool CoerceFullscreen(bool fullscreen) {
    return g_platform.capabilities.windowed ? fullscreen : true;
}

/* The GPU renderer (LIB386/GL, LIB386/SDL3GPU): the window the next SDL3_CreateWindow makes is an OpenGL one (SDL3_SetCreateOpenGL,
   from CreateWindowSurface's `opengl`), and the GL backend's context on it */
bool s_createOpenGL = false;
SDL_GLContext s_glContext = NULL;

/* The GL 2.1 / GLES 2.0 attribute set the renderer shaders are written for.
   SDL reads these when a window flagged SDL_WINDOW_OPENGL is created, so they
   must be set before every such creation (boot and runtime toggle alike). */
void SetGLContextAttributes(void) {
#ifdef USE_GLES2
    /* The ES profile is the whole request: on Android a desktop compatibility
       profile has no EGL config behind it, so window creation fails and the
       engine drops to the software renderer. */
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 2);
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 0);
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, SDL_GL_CONTEXT_PROFILE_ES);
#else
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 2);
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 1);
    SDL_GL_SetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, SDL_GL_CONTEXT_PROFILE_COMPATIBILITY);
#endif
    SDL_GL_SetAttribute(SDL_GL_DOUBLEBUFFER, 1);
    SDL_GL_SetAttribute(SDL_GL_DEPTH_SIZE, 16);
    SDL_GL_SetAttribute(SDL_GL_RED_SIZE, 8);
    SDL_GL_SetAttribute(SDL_GL_GREEN_SIZE, 8);
    SDL_GL_SetAttribute(SDL_GL_BLUE_SIZE, 8);
    SDL_GL_SetAttribute(SDL_GL_ALPHA_SIZE, 8);
}
} // namespace

extern "C" {

#ifdef LBA2_SDL3_TESTS
void SDL3_SetPresentationProbe(void (*probe)(SDL_Renderer *)) {
    s_presentationProbe = probe;
}
void SDL3_SetFullscreenProbe(bool (*probe)(SDL_Window *, bool)) {
    s_fullscreenProbe = probe;
}
#endif

bool SDL3_InitWindow(const char *title) {
    assert(title != NULL);
    if (!SDL3_InitVideo()) {
        Platform_Log(PLATFORM_LOG_ERROR, "Unable to initialize SDL Window/Video subsystem.\n"
                                         "\tSDL Message: %s",
                     SDL_GetError());
        return false;
    }
    s_title = title;
    s_initialized = true;
    return true;
}

void SDL3_EndWindow(void) {
    if (s_initialized) {
        SDL3_DestroyWindow();
        SDL_QuitSubSystem(SDL_INIT_VIDEO);
        s_initialized = false;
    }
}

bool SDL3_IsWindowInitialized(void) { return s_initialized; }

bool SDL3_CreateWindow(uint32_t width, uint32_t height, bool fullscreen) {
    assert(s_initialized);
    assert(s_window == NULL);
    fullscreen = CoerceFullscreen(fullscreen);
    s_fullscreen = fullscreen;

    SDL_PropertiesID props = SDL_CreateProperties();
    SDL_SetStringProperty(props, SDL_PROP_WINDOW_CREATE_TITLE_STRING, s_title);
    /* An editor that embeds this window in its own (LBA Assembler) takes it over as a child the moment it appears; asking for
       an off-screen position with LBA2_WINDOW_POS="x,y" means the window is never seen as a window of its own. */
    int startX = SDL_WINDOWPOS_CENTERED, startY = SDL_WINDOWPOS_CENTERED;
    /* The second player's game at one PC (RACEGAME.CPP LocalPage): its half of the screen or its own screen, with no border, and never taking the keyboard from
       the first player's window: LBA2_WINDOW_RECT="x,y,w,h". */
    int rectX = 0, rectY = 0, rectW = 0, rectH = 0;
    const char *rect = getenv("LBA2_WINDOW_RECT");
    const bool inRect = rect != NULL && sscanf(rect, "%d,%d,%d,%d", &rectX, &rectY, &rectW, &rectH) == 4;
    if (inRect) {
        startX = rectX;
        startY = rectY;
        fullscreen = false;
        s_fullscreen = false;
        SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_BORDERLESS_BOOLEAN, getenv("LBA2_WINDOW_TITLED") == NULL);
        SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_FOCUSABLE_BOOLEAN, false);
    }
    else if (const char *placed = getenv("LBA2_WINDOW_POS")) {
        int x, y;
        if (sscanf(placed, "%d,%d", &x, &y) == 2) {
            startX = x;
            startY = y;
        }
    }
    SDL_SetNumberProperty(props, SDL_PROP_WINDOW_CREATE_X_NUMBER, startX);
    SDL_SetNumberProperty(props, SDL_PROP_WINDOW_CREATE_Y_NUMBER, startY);
    /* (the window's size, not the picture's: `width` and `height` go on to be the presentation surface's) */
    SDL_SetNumberProperty(props, SDL_PROP_WINDOW_CREATE_WIDTH_NUMBER, inRect ? rectW : (int)width);
    SDL_SetNumberProperty(props, SDL_PROP_WINDOW_CREATE_HEIGHT_NUMBER, inRect ? rectH : (int)height);
#ifdef __ANDROID__
    /* Resizable permits portrait rotation, which can race surface teardown.
       A TV needs fullscreen at creation to avoid a later EGL surface transition;
       on phones the same property can race the native window's mutex. */
    if (IsAndroidTVDevice())
        SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_FULLSCREEN_BOOLEAN, true);
#else
    SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_RESIZABLE_BOOLEAN, true);
    SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_FULLSCREEN_BOOLEAN, fullscreen);
    /* Without this a scaled display (macOS Retina, a Wayland output with a
       scale factor) hands the renderer a drawable in points and the system
       resamples the result up to the panel, on top of the present filter.
       Window sizes and mouse coordinates stay in points either way. */
    SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_HIGH_PIXEL_DENSITY_BOOLEAN, true);
#endif
    /* SDL_GL_CreateContext (SDL3_CreateGLContext) can only attach to a window created with the OpenGL flag, and the attributes must
       land before the creation (SDL picks the context profile then) */
    if (s_createOpenGL) {
        SetGLContextAttributes();
        SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_OPENGL_BOOLEAN, true);
    }
    s_window = SDL_CreateWindowWithProperties(props);
    SDL_DestroyProperties(props);
    if (s_window == NULL) {
        Platform_Log(PLATFORM_LOG_ERROR, "Unable to create SDL window.\n"
                                         "\tResolution: %ux%u.\n\tSDL message: %s",
                     width, height, SDL_GetError());
        return false;
    }
    if (s_fullscreen && !SDL_SetWindowFullscreen(s_window, true)) {
        Platform_Log(PLATFORM_LOG_WARN, "Unable to switch SDL window to fullscreen.\n"
                                        "\tSDL message: %s",
                     SDL_GetError());
        s_fullscreen = false;
    }
    /* An OpenGL window needs SDL's OpenGL renderer: another driver would have SDL recreate the window without the flag, which leaves
       the GL backend nothing to attach a context to */
    s_renderer = SDL_CreateRenderer(s_window, s_createOpenGL ? "opengl" : NULL);
    if (s_renderer == NULL) {
        Platform_Log(PLATFORM_LOG_ERROR, "Unable to create SDL renderer.\n"
                                         "\tSDL message: %s",
                     SDL_GetError());
        SDL_DestroyWindow(s_window);
        s_window = NULL;
        return false;
    }
    SDL_SetRenderVSync(s_renderer, 1);
    s_texture = SDL_CreateTexture(s_renderer, SDL_PIXELFORMAT_ARGB8888,
                                  SDL_TEXTUREACCESS_STREAMING, width, height);
    if (s_texture == NULL) {
        Platform_Log(PLATFORM_LOG_ERROR, "Unable to create SDL streaming texture.\n"
                                         "\tSize: %ux%u.\n\tSDL message: %s",
                     width, height, SDL_GetError());
        SDL_DestroyRenderer(s_renderer);
        s_renderer = NULL;
        SDL_DestroyWindow(s_window);
        s_window = NULL;
        return false;
    }
    ApplyScaleMode(s_texture);
    s_width = width;
    s_height = height;
    SetLogicalPresentation(width, height);
    HideGameCursor();
    return true;
}

bool SDL3_DisplaySize(uint32_t *outW, uint32_t *outH) {
    if (outW == NULL || outH == NULL || s_window == NULL)
        return false;
    SDL_DisplayID display = SDL_GetDisplayForWindow(s_window);
    if (display == 0)
        return false;
    const SDL_DisplayMode *mode = SDL_GetCurrentDisplayMode(display);
    if (mode == NULL || mode->w <= 0 || mode->h <= 0)
        return false;
    *outW = (uint32_t)mode->w;
    *outH = (uint32_t)mode->h;
    return true;
}

bool SDL3_PrimaryDisplaySize(uint32_t *outW, uint32_t *outH) {
    if (outW == NULL || outH == NULL || !SDL_WasInit(SDL_INIT_VIDEO))
        return false;
    const char *driver = SDL_GetCurrentVideoDriver();
    if (driver == NULL || SDL_strcmp(driver, "dummy") == 0 || SDL_strcmp(driver, "offscreen") == 0)
        return false;
    SDL_DisplayID display = SDL_GetPrimaryDisplay();
    if (display == 0)
        return false;
    const SDL_DisplayMode *mode = SDL_GetCurrentDisplayMode(display);
    if (mode == NULL || mode->w <= 0 || mode->h <= 0)
        return false;
    *outW = (uint32_t)mode->w;
    *outH = (uint32_t)mode->h;
    return true;
}

bool SDL3_WindowSize(int32_t *width, int32_t *height) {
    if (s_window == NULL || width == NULL || height == NULL)
        return false;
    return SDL_GetWindowSize(s_window, width, height);
}

bool SDL3_ResizeWindow(uint32_t width, uint32_t height, bool keepWindowSize) {
    if (s_window != NULL && s_renderer == NULL) {
        /* (a GPU renderer's window, no streaming texture: the renderer makes its own target at the new size) */
        SDL3_SetGpuSurfaceSize(width, height, !keepWindowSize);
        return true;
    }
    if (s_window == NULL || s_renderer == NULL)
        return false;
    /* Window size and framebuffer size are independent. Automatic per-scene
       downshifts leave the player's window alone; their explicit choice moves it. */
    if (!keepWindowSize)
        SDL_SetWindowSize(s_window, (int)width, (int)height);
    if (s_texture) {
        SDL_DestroyTexture(s_texture);
        s_texture = NULL;
    }
    s_texture = SDL_CreateTexture(s_renderer, SDL_PIXELFORMAT_ARGB8888,
                                  SDL_TEXTUREACCESS_STREAMING, (int)width, (int)height);
    if (s_texture == NULL) {
        Platform_Log(PLATFORM_LOG_ERROR, "Unable to recreate SDL streaming texture at %ux%u.\n"
                                         "\tSDL message: %s",
                     width, height, SDL_GetError());
        return false;
    }
    ApplyScaleMode(s_texture);
    s_width = width;
    s_height = height;
    SetLogicalPresentation(width, height);
    return true;
}

void SDL3_DestroyWindow(void) {
#if defined(__ANDROID__) || defined(LBA2_SDL3_TEST_STAGING)
    StagingBufferFree(&s_staging);
#endif
    if (s_texture) {
        SDL_DestroyTexture(s_texture);
        s_texture = NULL;
    }
    if (s_renderer) {
        SDL_DestroyRenderer(s_renderer);
        s_renderer = NULL;
    }
    if (s_window) {
        SDL_DestroyWindow(s_window);
        s_window = NULL;
    }
    s_width = 0;
    s_height = 0;
}

void SDL3_SurfaceCoords(int32_t x, int32_t y, int32_t *sx, int32_t *sy) {
    assert(sx != NULL && sy != NULL);
    if (s_renderer == NULL || s_width == 0 || s_height == 0) {
        *sx = x;
        *sy = y;
        return;
    }
    float logicalX = 0.0f;
    float logicalY = 0.0f;
    SDL_RenderCoordinatesFromWindow(s_renderer, (float)x, (float)y, &logicalX, &logicalY);
    int32_t mappedX = (int32_t)logicalX;
    int32_t mappedY = (int32_t)logicalY;
    if (mappedX < 0)
        mappedX = 0;
    else if (mappedX >= (int32_t)s_width)
        mappedX = (int32_t)s_width - 1;
    if (mappedY < 0)
        mappedY = 0;
    else if (mappedY >= (int32_t)s_height)
        mappedY = (int32_t)s_height - 1;
    *sx = mappedX;
    *sy = mappedY;
}

void SDL3_SetRelativeMouse(bool enabled) {
    if (s_window)
        SDL_SetWindowRelativeMouseMode(s_window, enabled);
}

void SDL3_SetMouseGrab(bool enabled) {
    if (s_window && !SDL_SetWindowMouseGrab(s_window, enabled))
        Platform_Log(PLATFORM_LOG_WARN, "Unable to change SDL3 mouse grab: %s", SDL_GetError());
}

void SDL3_SetFullscreen(bool fullscreen) {
    fullscreen = CoerceFullscreen(fullscreen);
    const bool previous = s_fullscreen;
    s_fullscreen = fullscreen;
    if (s_window == NULL)
        return;
    bool changed;
#ifdef LBA2_SDL3_TESTS
    if (s_fullscreenProbe)
        changed = s_fullscreenProbe(s_window, fullscreen);
    else
#endif
        changed = SDL_SetWindowFullscreen(s_window, fullscreen);
    if (!changed) {
        Platform_Log(PLATFORM_LOG_WARN, "Unable to change SDL fullscreen state to %d.\n"
                                        "\tSDL message: %s",
                     fullscreen ? 1 : 0, SDL_GetError());
        s_fullscreen = previous;
        return;
    }
    if (s_renderer && s_texture)
        PresentTexture();
}

bool SDL3_GetFullscreen(void) { return s_fullscreen; }
bool SDL3_IsFullscreen(void) {
    return s_window && ((SDL_GetWindowFlags(s_window) & SDL_WINDOW_FULLSCREEN) != 0);
}

void SDL3_SetVSync(bool enabled) {
    /* (the GL context and the SDL renderer are presenters apart: the swap interval is the context's) */
    if (s_glContext != NULL) {
        SDL_GL_SetSwapInterval(enabled ? 1 : 0);
        return;
    }
    if (s_renderer)
        SDL_SetRenderVSync(s_renderer, enabled ? 1 : 0);
}

bool SDL3_SetPresentFilter(PlatformPresentFilter mode) {
    SDL_ScaleMode resolved = SDL_SCALEMODE_NEAREST;
    if (!ResolveScaleMode(mode, &resolved))
        return false;
    if (s_texture && !SDL_SetTextureScaleMode(s_texture, resolved))
        return false;
    const bool changed = (resolved != s_scaleMode);
    s_scaleMode = resolved;
    /* A paused or menu screen does not present again until something moves. */
    if (changed && s_renderer && s_texture)
        PresentTexture();
    return true;
}

void SDL3_HandleWindowEvent(const SDL_Event *event) {
    if (!s_initialized)
        return;
    switch (event->type) {
    case SDL_EVENT_WINDOW_FOCUS_GAINED:
    case SDL_EVENT_WINDOW_MOUSE_ENTER:
        if (s_window)
            HideGameCursor();
        break;
    case SDL_EVENT_WINDOW_RESIZED:
        if (s_renderer && s_texture)
            PresentTexture();
        s_resizePending = true;
        s_resizeAt = SDL_GetTicks();
        break;
    case SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
        s_resizePending = true;
        s_resizeAt = SDL_GetTicks();
        break;
    default:
        break;
    }
}

bool SDL3_Present(const PlatformFrame *frame) {
    if (frame == NULL || frame->width != s_width || frame->height != s_height ||
        s_renderer == NULL || s_texture == NULL) {
        SDL_SetError("Indexed frame does not match the presentation surface");
        Platform_Log(PLATFORM_LOG_ERROR, "%s", SDL_GetError());
        return false;
    }
    void *pixels = NULL;
    uint32_t pitch = 0;
#if defined(__ANDROID__) || defined(LBA2_SDL3_TEST_STAGING)
    /* MTE can invalidate SDL_LockTexture's storage between frames. Persistent
       owned memory plus an upload keeps the write pointer's tag stable. */
    pixels = StagingBufferEnsure(&s_staging, frame->width, frame->height);
    if (pixels == NULL) {
        SDL_SetError("Unable to allocate presentation staging buffer");
        Platform_Log(PLATFORM_LOG_ERROR, "%s", SDL_GetError());
        return false;
    }
    pitch = frame->width * sizeof(uint32_t);
#else
    int texturePitch = 0;
    s_presenting = true;
    if (!SDL_LockTexture(s_texture, NULL, &pixels, &texturePitch)) {
        s_presenting = false;
        Platform_Log(PLATFORM_LOG_ERROR, "Unable to lock presentation texture: %s", SDL_GetError());
        return false;
    }
    pitch = (uint32_t)texturePitch;
#endif
    bool converted = Platform_ConvertIndexedFrame(frame, (uint32_t *)pixels, pitch);
    if (converted && frame->overlay)
        frame->overlay((uint32_t *)pixels, pitch, frame->width, frame->height, frame->palette_rgb);
#if defined(__ANDROID__) || defined(LBA2_SDL3_TEST_STAGING)
    if (converted && !SDL_UpdateTexture(s_texture, NULL, pixels, (int)pitch)) {
        Platform_Log(PLATFORM_LOG_ERROR, "Unable to upload presentation texture: %s", SDL_GetError());
        return false;
    }
#else
    SDL_UnlockTexture(s_texture);
    s_presenting = false;
#endif
    return converted && PresentTexture();
}

/* ---- LBA Assembler's own (LIB386/SYSTEM/WINDOW.CPP's Window_* forward here) ---------------------------------------------- */

bool SDL3_IsPresenting(void) { return s_presenting; }

void SDL3_SetWindowTitle(const char *title) {
    static char kept[128];
    if (!title || !title[0])
        return;
    snprintf(kept, sizeof(kept), "%s", title);
    s_title = kept;
    if (s_window)
        SDL_SetWindowTitle(s_window, kept);
}

static bool HeadlessVideoDriver(void) {
    const char *driver = SDL_GetCurrentVideoDriver();
    return driver == NULL || SDL_strcmp(driver, "dummy") == 0 || SDL_strcmp(driver, "offscreen") == 0;
}

bool SDL3_TakeSettledResize(uint32_t settleMs, uint32_t *outPixelW, uint32_t *outPixelH) {
    if (!s_resizePending || s_window == NULL || outPixelW == NULL || outPixelH == NULL)
        return false;
    if (SDL_GetTicks() - s_resizeAt < (Uint64)settleMs)
        return false;
    s_resizePending = false;
    if (HeadlessVideoDriver() || (SDL_GetWindowFlags(s_window) & SDL_WINDOW_MINIMIZED) != 0)
        return false;
    int w = 0, h = 0;
    if (!SDL_GetWindowSizeInPixels(s_window, &w, &h) || w <= 0 || h <= 0)
        return false;
    *outPixelW = (uint32_t)w;
    *outPixelH = (uint32_t)h;
    return true;
}

bool SDL3_GetWindowedSize(int32_t *outW, int32_t *outH, bool *outMaximized) {
    if (s_window == NULL || outW == NULL || outH == NULL || outMaximized == NULL)
        return false;
    int w = 0, h = 0;
    if (!SDL_GetWindowSize(s_window, &w, &h) || w <= 0 || h <= 0)
        return false;
    *outW = (int32_t)w;
    *outH = (int32_t)h;
    *outMaximized = (SDL_GetWindowFlags(s_window) & SDL_WINDOW_MAXIMIZED) != 0;
    return true;
}

void SDL3_RestoreWindowedSize(int32_t w, int32_t h, bool maximized) {
    if (s_window == NULL || HeadlessVideoDriver() || w <= 0 || h <= 0)
        return;
    /* Never bigger than the screen it is on (a size kept from a bigger monitor), then centred on it. */
    SDL_Rect usable;
    const SDL_DisplayID display = SDL_GetDisplayForWindow(s_window);
    if (display != 0 && SDL_GetDisplayUsableBounds(display, &usable)) {
        if (w > usable.w)
            w = usable.w;
        if (h > usable.h)
            h = usable.h;
    }
    SDL_SetWindowSize(s_window, w, h);
    SDL_SetWindowPosition(s_window, SDL_WINDOWPOS_CENTERED_DISPLAY(display), SDL_WINDOWPOS_CENTERED_DISPLAY(display));
    if (maximized)
        SDL_MaximizeWindow(s_window);
}

void SDL3_SetWindowVisible(bool visible) {
    if (s_window == NULL)
        return;
    if (visible)
        SDL_ShowWindow(s_window);
    else
        SDL_HideWindow(s_window);
}

/* ---- The GPU renderer's window (LIB386/GL, LIB386/SDL3GPU; LIB386/SYSTEM/WINDOW.CPP's Window_* forward here) ---------------------- */

void SDL3_SetCreateOpenGL(bool opengl) { s_createOpenGL = opengl; }

SDL_Window *SDL3_GetSDLWindow(void) { return s_window; }

bool SDL3_GLMakeCurrent(void) {
    if (s_glContext == NULL || s_window == NULL)
        return false;
    if (!SDL_GL_MakeCurrent(s_window, s_glContext)) {
        /* Some SDL3/EGL stacks report a failure with no message while the context still becomes current: judged by the current
           context, so a spurious error cannot take down a live context */
        if (SDL_GL_GetCurrentContext() == s_glContext)
            return true;
        Platform_Log(PLATFORM_LOG_WARN, "Window_GLMakeCurrent: %s", SDL_GetError());
        return false;
    }
    return true;
}

bool SDL3_CreateGLContext(void) {
    if (s_glContext != NULL)
        return true;
    if (s_window == NULL) {
        Platform_Log(PLATFORM_LOG_WARN, "Window_CreateGLContext: no window to attach a context to");
        return false;
    }
    if ((SDL_GetWindowFlags(s_window) & SDL_WINDOW_OPENGL) == 0) {
        Platform_Log(PLATFORM_LOG_WARN, "Window_CreateGLContext: the window is not an OpenGL window "
                                        "(select the GL renderer before creating the window)");
        return false;
    }
    s_glContext = SDL_GL_CreateContext(s_window);
    if (s_glContext == NULL) {
        Platform_Log(PLATFORM_LOG_WARN, "Window_CreateGLContext: SDL_GL_CreateContext failed: %s", SDL_GetError());
        return false;
    }
    if (!SDL3_GLMakeCurrent()) {
        SDL_GL_DestroyContext(s_glContext);
        s_glContext = NULL;
        return false;
    }
    return true;
}

void SDL3_GLSwapWindow(void) {
    if (s_glContext != NULL && s_window != NULL)
        SDL_GL_SwapWindow(s_window);
}

void SDL3_DestroyGLContext(void) {
    if (s_glContext == NULL)
        return;
    if (s_window != NULL)
        SDL_GL_MakeCurrent(s_window, NULL);
    SDL_GL_DestroyContext(s_glContext);
    s_glContext = NULL;
}

/* The renderer switch: the software presenter (an SDL renderer streamed to), the GL one (a GL context swapped to) and the SDL3 GPU one
   (a device claiming a window with neither) can't share a window, so it is made again in the shape asked for -- 0 software, 1 OpenGL,
   2 plain (SDL3 GPU) -- where the old one was and as big, its borders and focus as they were (the second player's window, a test's
   window put off to one side, stay where they are). */
bool SDL3_RecreateWindow(uint32_t width, uint32_t height, bool fullscreen, int shape) {
    fullscreen = CoerceFullscreen(fullscreen);
    int x = SDL_WINDOWPOS_CENTERED, y = SDL_WINDOWPOS_CENTERED, w = (int)width, h = (int)height;
    SDL_WindowFlags flags = 0;
    SDL3_DestroyGLContext();
#if defined(__ANDROID__) || defined(LBA2_SDL3_TEST_STAGING)
    StagingBufferFree(&s_staging);
#endif
    if (s_texture) {
        SDL_DestroyTexture(s_texture);
        s_texture = NULL;
    }
    if (s_renderer) {
        SDL_DestroyRenderer(s_renderer);
        s_renderer = NULL;
    }
    if (s_window) {
        flags = SDL_GetWindowFlags(s_window);
        if ((flags & SDL_WINDOW_FULLSCREEN) == 0) {
            SDL_GetWindowPosition(s_window, &x, &y);
            SDL_GetWindowSize(s_window, &w, &h);
        }
        /* (a fullscreen window left mid-transition leaves its space behind on macOS: settled and left first) */
        SDL_SyncWindow(s_window);
        if ((flags & SDL_WINDOW_FULLSCREEN) != 0) {
            SDL_SetWindowFullscreen(s_window, false);
            SDL_SyncWindow(s_window);
        }
        SDL_DestroyWindow(s_window);
        s_window = NULL;
    }
    s_fullscreen = fullscreen;

    SDL_PropertiesID props = SDL_CreateProperties();
    SDL_SetStringProperty(props, SDL_PROP_WINDOW_CREATE_TITLE_STRING, s_title);
    SDL_SetNumberProperty(props, SDL_PROP_WINDOW_CREATE_X_NUMBER, x);
    SDL_SetNumberProperty(props, SDL_PROP_WINDOW_CREATE_Y_NUMBER, y);
    SDL_SetNumberProperty(props, SDL_PROP_WINDOW_CREATE_WIDTH_NUMBER, w);
    SDL_SetNumberProperty(props, SDL_PROP_WINDOW_CREATE_HEIGHT_NUMBER, h);
    if (flags & SDL_WINDOW_BORDERLESS)
        SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_BORDERLESS_BOOLEAN, true);
    if (flags & SDL_WINDOW_NOT_FOCUSABLE)
        SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_FOCUSABLE_BOOLEAN, false);
    if (shape == 1) {
        SetGLContextAttributes();
        SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_OPENGL_BOOLEAN, true);
    }
#ifdef __ANDROID__
    if (IsAndroidTVDevice())
        SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_FULLSCREEN_BOOLEAN, true);
#else
    SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_RESIZABLE_BOOLEAN, true);
    SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_FULLSCREEN_BOOLEAN, fullscreen);
    SDL_SetBooleanProperty(props, SDL_PROP_WINDOW_CREATE_HIGH_PIXEL_DENSITY_BOOLEAN, true);
#endif
    s_window = SDL_CreateWindowWithProperties(props);
    SDL_DestroyProperties(props);
    if (s_window == NULL) {
        Platform_Log(PLATFORM_LOG_ERROR, "Unable to make the window again (renderer %d).\n"
                                         "\tResolution: %ux%u.\n\tSDL message: %s",
                     shape, width, height, SDL_GetError());
        return false;
    }
    if (s_fullscreen && !SDL_SetWindowFullscreen(s_window, true)) {
        Platform_Log(PLATFORM_LOG_WARN, "Unable to switch the window to fullscreen.\n\tSDL message: %s", SDL_GetError());
        s_fullscreen = false;
    }
    s_width = width;
    s_height = height;
    if (shape == 0) {
        s_renderer = SDL_CreateRenderer(s_window, NULL);
        if (s_renderer == NULL) {
            Platform_Log(PLATFORM_LOG_ERROR, "Unable to make the SDL renderer again.\n\tSDL message: %s", SDL_GetError());
            return false;
        }
        SDL_SetRenderVSync(s_renderer, 1);
        s_texture = SDL_CreateTexture(s_renderer, SDL_PIXELFORMAT_ARGB8888, SDL_TEXTUREACCESS_STREAMING, (int)width, (int)height);
        if (s_texture == NULL) {
            Platform_Log(PLATFORM_LOG_ERROR, "Unable to make the streaming texture again.\n\tSDL message: %s", SDL_GetError());
            return false;
        }
        ApplyScaleMode(s_texture);
        SetLogicalPresentation(width, height);
    }
    HideGameCursor();
    return true;
}

/* The GL path's picture size: its viewport follows the window's backing size */
void SDL3_SetGpuSurfaceSize(uint32_t width, uint32_t height, bool resizeWindow) {
    if (s_window != NULL && resizeWindow)
        SDL_SetWindowSize(s_window, (int)width, (int)height);
    s_width = width;
    s_height = height;
}
}
