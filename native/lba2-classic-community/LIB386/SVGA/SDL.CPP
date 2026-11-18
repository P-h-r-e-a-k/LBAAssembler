#include <SVGA/VIDEO.H>

#include <SVGA/DIRTYBOX.H>
#include <SVGA/SCREEN.H>
#include <SVGA/SDL.H>
#include <SYSTEM/LOG.H>
#include <SYSTEM/WINDOW.H>

#include <PLATFORM/PLATFORM.H>
#include <PLATFORM/STAGING_BUFFER.H>
#include <assert.h>
#include <stdlib.h>
#include <string.h>

// -----------------------------------------------------------------------------
extern "C" {

#define PAL_MAX_COLORS 256

// --- Public state ------------------------------------------------------------
void *Phys;
U32 ModeResX;
U32 ModeResY;
U32 TabOffPhysLine[ADELINE_MAX_Y_RES];

// --- Private state -----------------------------------------------------------
static bool paletteInitialized = false;
static U8 paletteRgb[PAL_MAX_COLORS * 3];
S32 screenLockCount = 0;
bool frameDirty = false;
static PrePresentFn s_prePresent = NULL;
static PrePresentFn s_overlayPrePresent = NULL;
static PrePresentNeedsArgbFn s_prePresentNeedsArgb = NULL;
static PrePresentNeedsArgbFn s_overlayPrePresentNeedsArgb = NULL;
static PresentTimingFn s_presentBegin = NULL;
static PresentTimingFn s_preFrame = NULL;   // (before each frame is presented: a resolution switch asked mid-frame -- SetPreFrameCallback)
static PresentTimingFn s_presentEnd = NULL;

/* The palette as 0xAARRGGBB, for the GPU renderer (GL_GetPaletteLUT) and the GPU present's ARGB frame: rebuilt from paletteRgb
   whenever it is asked for, so it is always the table the platform present converts the frame with. */
static U32 paletteLUT[PAL_MAX_COLORS];

/* GPU-acceleration seam for the present step. Registered by the GPU renderer
   when it goes active (see SetGpuPresentHooks below); NULL on the software
   path. The function-pointer indirection (rather than calling the renderer
   directly) keeps this library free of GPU dependencies, which the ASM
   equivalence tests link without. */
static GpuPresentFn s_gpuPresent = NULL;
static GpuIsActiveFn s_gpuIsActive = NULL;

/* Persistent staging buffer for GPU-present frames that need the ARGB conversion (the console or an overlay draws into it): a GPU
   renderer backend owns a window with no streaming texture to lock, so the frame is staged here and handed to the GPU present hook,
   which composites the Log overlay over its own framebuffer. */
static StagingBuffer s_gpuPresentStaging = {NULL, 0, 0};

static void PresentFrame();

void SetGpuPresentHooks(GpuPresentFn present, GpuIsActiveFn isActive) {
    s_gpuPresent = present;
    s_gpuIsActive = isActive;
}

S32 GpuPresentIsActive(void) {
    return (s_gpuIsActive != NULL) ? s_gpuIsActive() : 0;
}

/* Present pacing seam, same indirection rationale as the present hooks. The
   wanted value is held here so a choice made before the renderer initialized
   is replayed the moment its setter registers. */
static GpuVSyncFn s_gpuVSync = NULL;
static S32 s_gpuVSyncWanted = 1;

void SetGpuVSyncHook(GpuVSyncFn setVSync) {
    s_gpuVSync = setVSync;
    if (s_gpuVSync != NULL) {
        s_gpuVSync(s_gpuVSyncWanted);
    }
}

void GpuPresentSetVSync(S32 enabled) {
    s_gpuVSyncWanted = enabled ? 1 : 0;
    if (s_gpuVSync != NULL) {
        s_gpuVSync(s_gpuVSyncWanted);
    }
}

// --- Private functions -------------------------------------------------------
static void RebuildPaletteLUT() {
    for (int i = 0; i < PAL_MAX_COLORS; ++i) {
        paletteLUT[i] = ((U32)0xFF << 24) | ((U32)paletteRgb[i * 3] << 16) | ((U32)paletteRgb[i * 3 + 1] << 8) | paletteRgb[i * 3 + 2];
    }
}

/* The GPU renderer (LIB386/GL/RENDER_GL.CPP) converts this palette LUT to an
   RGBA texture atlas; it must see the same entries the present path uses, or
   GPU-drawn content would disagree with what Log shows. */
const U32 *GL_GetPaletteLUT(void) {
    RebuildPaletteLUT();
    return paletteLUT;
}

// --- Initialization ----------------------------------------------------------
bool InitVideo() {
    if (!IsWindowInitialized()) {
        Log_Error("Unable to initialize Video subsystem.\n"
                  "\tWindow subsystem was not initialized beforehand.");
        return false;
    }
    return true;
}

void EndVideo() { DestroyVideoSurface(); }

void DestroyVideoSurface() {
    StagingBufferFree(&s_gpuPresentStaging);
    if (Phys) {
        free(Phys);
        Phys = NULL;
    }
    paletteInitialized = false;
}

bool ResizeVideoSurface(U32 newW, U32 newH) {
    if (Phys)
        free(Phys);
    Phys = calloc((size_t)newW, (size_t)newH);   // (zeroed: upstream 1cdbe361, the backgrounds of the holomap and save zooms)
    if (Phys == NULL) {
        Log_Error("Unable to allocate video surface at %ux%u", newW, newH);
        return false;
    }
    ModeResX = newW;
    ModeResY = newH;
    S32 off = 0;
    for (S32 i = 0; i < ADELINE_MAX_Y_RES; i++) {
        TabOffPhysLine[i] = off;
        off += (S32)newW;
    }
    return true;
}

// --- Interface ---------------------------------------------------------------
bool CreateVideoSurface(U32 resX, U32 resY) {
    assert(!paletteInitialized);
    Phys = calloc((size_t)resX, (size_t)resY);
    if (Phys == NULL) {
        Log_Error("Unable to allocate video surface at %ux%u", resX, resY);
        return false;
    }
    memset(paletteRgb, 255, sizeof(paletteRgb));
    paletteInitialized = true;
    ModeResX = resX;
    ModeResY = resY;
    S32 off = 0;
    for (S32 i = 0; i < ADELINE_MAX_Y_RES; i++) {
        TabOffPhysLine[i] = off;
        off += VideoSurfacePitch();
    }
    return true;
}

U32 VideoSurfacePitch() { return ModeResX; }
void LockVideoSurface() { ++screenLockCount; }

void UnlockVideoSurface() {
    if (screenLockCount > 0)
        --screenLockCount;
    if (screenLockCount == 0 && frameDirty) {
        if (s_preFrame)
            s_preFrame();
        if (s_presentBegin)
            s_presentBegin();
        PresentFrame();
        if (s_presentEnd)
            s_presentEnd();
        frameDirty = false;
    }
}

void WaitVideoSync() {}

void SetVideoPalette(const U8 src[], S32 startIdx, S32 count) {
    assert(paletteInitialized);
    static U8 tmpColors[PAL_MAX_COLORS * 3];
    if (startIdx < 0)
        startIdx = 0;
    if (src == NULL || count < 0 || count > PAL_MAX_COLORS || startIdx > PAL_MAX_COLORS) {
        Log_Warn("Invalid palette update: start=%d count=%d", startIdx, count);
        return;
    }
    /* Keep the palette API's source ordering: count bounds the temporary
       table's fill, and the upload starts at its first entry. */
    for (S32 i = startIdx; i < count; ++i) {
        tmpColors[i * 3] = src[0];
        tmpColors[i * 3 + 1] = src[1];
        tmpColors[i * 3 + 2] = src[2];
        src += 3;
    }
    if (count > PAL_MAX_COLORS - startIdx) {
        Log_Warn("Invalid palette update: start=%d count=%d", startIdx, count);
        return;
    }
    memcpy(paletteRgb + startIdx * 3, tmpColors, (size_t)count * 3);
}

void SetVideoPaletteCol(S32 colorIdx, U8 r, U8 g, U8 b) {
    assert(paletteInitialized);
    if (colorIdx < 0 || colorIdx >= PAL_MAX_COLORS)
        return;
    paletteRgb[colorIdx * 3] = r;
    paletteRgb[colorIdx * 3 + 1] = g;
    paletteRgb[colorIdx * 3 + 2] = b;
}

void SetVideoPaletteSync(const U8 src[]) { SetVideoPalette(src, 0, PAL_MAX_COLORS); }

void CopyVideoArea(void *dst, const void *src, const U32 tabOffDst[], const T_BOX *area) {
    assert((src != Phys) && "Presentation surface as 'src' is not supported");
    assert((src != dst) && "Copy to same video memory is not supported");
    if (dst == Phys) {
        frameDirty = true;
        return;
    }
    U8 *dstMem = (U8 *)dst;
    const U8 *srcMem = (const U8 *)src;
    U32 pitch = tabOffDst[1];
    for (int y = area->y0; y < area->y1; ++y) {
        U8 *dstRow = dstMem + (y * pitch) + area->x0;
        const U8 *srcRow = srcMem + (y * pitch) + area->x0;
        memcpy(dstRow, srcRow, area->x1 - area->x0);
    }
}

void SetPrePresentCallback(PrePresentFn fn) { s_prePresent = fn; }
void SetOverlayPrePresentCallback(PrePresentFn fn) { s_overlayPrePresent = fn; }
void SetPrePresentNeedsArgb(PrePresentNeedsArgbFn fn) { s_prePresentNeedsArgb = fn; }
void SetOverlayPrePresentNeedsArgb(PrePresentNeedsArgbFn fn) { s_overlayPrePresentNeedsArgb = fn; }
void SetPreFrameCallback(PresentTimingFn fn) { s_preFrame = fn; }
void SetPresentTimingCallbacks(PresentTimingFn begin, PresentTimingFn end) {
    s_presentBegin = begin;
    s_presentEnd = end;
}

static void ApplyOverlays(U32 *pixels, U32 pitch, U32, U32, const U8 *palette) {
    if (s_prePresent)
        s_prePresent(pixels, pitch, ModeResX, ModeResY, palette);
    if (s_overlayPrePresent)
        s_overlayPrePresent(pixels, pitch, ModeResX, ModeResY, palette);
}

/* A GPU backend owns the window: the frame goes to its present hook, which composites Log over the GPU's own 3D frame (palette index
   0 its transparent colour) and swaps the window itself. With no callback that reads or writes an ARGB frame this time, Log's
   palette indices go as they are and the present shader looks them up (R8 present); otherwise the frame is converted here, the
   overlays draw on it, and the ARGB frame goes. */
static void PresentFrameGpu(const U8 *framePalette) {
    const S32 needsArgb = (s_prePresentNeedsArgb != NULL && s_prePresentNeedsArgb()) ||
                          (s_overlayPrePresentNeedsArgb != NULL && s_overlayPrePresentNeedsArgb());
    if (!needsArgb) {
        /* (the callbacks still run -- the control socket and the console are served there -- with no frame to draw on: every query said
           they wouldn't) */
        ApplyOverlays(NULL, 0, ModeDesiredX, ModeDesiredY, framePalette);
        s_gpuPresent(Log, ModeDesiredX, ModeDesiredX, ModeDesiredY, FALSE, TRUE);
        return;
    }
    U32 *pixels = (U32 *)StagingBufferEnsure(&s_gpuPresentStaging, ModeDesiredX, ModeDesiredY);
    if (pixels == NULL)
        return;
    const U32 pitch = ModeDesiredX * sizeof(U32);
    RebuildPaletteLUT();
    paletteLUT[0] &= 0x00FFFFFF; // (index 0: transparent over the GPU's frame)
    const U8 *logMem = (const U8 *)Log;
    for (U32 y = 0; y < ModeDesiredY; ++y) {
        const U8 *srcRow = logMem + y * ModeDesiredX;
        U32 *dstRow = (U32 *)((U8 *)pixels + y * pitch);
        for (U32 x = 0; x < ModeDesiredX; ++x)
            dstRow[x] = paletteLUT[srcRow[x]];
    }
    ApplyOverlays(pixels, pitch, ModeDesiredX, ModeDesiredY, framePalette);
    s_gpuPresent(pixels, pitch, ModeDesiredX, ModeDesiredY, FALSE, FALSE);
}

static void PresentFrame() {
    assert(paletteInitialized);
    assert(Log != NULL);
    /* An overlay can execute a command that changes the palette. Its colour
       table must still describe the pixels already converted for this frame. */
    static U8 framePalette[PAL_MAX_COLORS * 3];
    memcpy(framePalette, paletteRgb, sizeof(framePalette));
    if (s_gpuPresent != NULL && s_gpuIsActive != NULL && s_gpuIsActive()) {
        PresentFrameGpu(framePalette);
        return;
    }
    PlatformFrame frame = {
        (const U8 *)Log, ModeDesiredX, ModeDesiredY, ModeDesiredX, framePalette,
        (s_prePresent || s_overlayPrePresent) ? ApplyOverlays : NULL};
    g_platform.window.present(&frame);
}

void HandleEventsVideo(const PlatformEvent *event) {
    assert(event != NULL);
    if (event->type == PLATFORM_EVENT_WINDOW_FOCUS_GAINED && !AppActive) {
        BoxStaticAdd(0, 0, ModeDesiredX - 1, ModeDesiredY - 1);
        BoxCleanClip = TRUE;
    }
}

// =============================================================================
}
