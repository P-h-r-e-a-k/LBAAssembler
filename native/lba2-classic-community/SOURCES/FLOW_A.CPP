#include "FLOW.H"

#include <SVGA/CLIP.H>
#include <SVGA/SCREEN.H>
#include <POLYGON/POLY.H>
#include <GPU/GPURENDERER.H>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

extern U8 *PtrPal;

/* ── The HUD over the GPU renderer's frame (LBA Assembler's port of the GPU renderer, 2026-11-18) ─────────────────────────────────
   With a GPU backend the exterior scene is drawn in its own target, and Log -- the HUD, the menus, the dialogue -- is laid over it with
   palette index 0 see-through. What darkens the screen in software (ShadeBoxBlk: the race's minimap and pages, the menus' and the
   dialogue's boxes) darkened Log's own pixels, and over the GPU's frame those are index 0: it came out a black box. So:
   - in a scene frame (AffScene drawing it, GpuSceneDrawing), the GPU's frame is darkened under the box (flat shadow spans) and only
     Log's own pixels are darkened (ShadeBoxBlkOverlay);
   - outside one -- a menu or a dialogue over a frozen scene, which redraws and shades again and again and would darken the GPU's frame
     again each time -- what is shown is first made Log's own, once (GpuOverlay_Plate: the GPU's frame read back, as the palette's
     nearest colours, into Log's and Screen's see-through pixels) and Log is shown opaque until the next scene frame
     (GpuOverlay_Unplate, at AffScene's start), so the menus work on Log as they do in software. */
S32 GpuSceneDrawing = 0;
static S32 s_platePinned = 0;
static U8 *s_plateMask = NULL;       // the Screen pixels the plate filled (to empty again at the next scene frame)
static size_t s_plateMaskSize = 0;
// The Log pixels the plate filled, each with the colour it put there (0: not the plate's): at the next scene frame those still that colour
// are emptied again. Kept, they showed the frame the plate was read from for the rest of a dialogue: its speaking steps redraw only the
// speakers' boxes, every other pixel stayed the plate's -- the cut scene after Raph, Twinsen standing beside Zoe where he had been before
// the script put him in his car (in software he is in it), and round each speaker a box of the live picture that didn't match the stale
// one about it: the user's picture, 2026-11-25, "boxes around characters with OpenGL" (no renderer switch).
static U8 *s_plateLog = NULL;
static size_t s_plateLogSize = 0;
static U8 s_lutPal[768];
static U8 s_lut15[32768];            // RGB555 -> the palette's nearest colour (never index 0: see-through over the GPU)
static S32 s_lutMade = 0;
// The rows a dialogue's window has made its own (GpuOverlay_KeepPlate), shown opaque while it is up: its text's shadow and outline are
// palette index 0, see-through over the GPU's frame anywhere else -- the light ground under every line's shadow where the software's is
// black. Not a modal's pin of the overlay: the scene frames go on with the GPU's own shading round it (GpuOverlayLive).
static S32 s_keepOn = 0, s_keepX0 = 0, s_keepY0 = 0, s_keepX1 = 0, s_keepY1 = 0;

static S32 GpuOverlayLive(void) {
    return GpuRenderer_IsActive() AND !GpuRenderer_GetSoftwarePolys() AND (!GpuRenderer_IsOverlayOpaque() OR (s_keepOn AND !s_platePinned));
}

static void PlateLut(void) {
    if (s_lutMade AND !memcmp(s_lutPal, PtrPal, 768))
        return;
    memcpy(s_lutPal, PtrPal, 768);
    for (S32 c = 0; c < 32768; c++) {
        const S32 r = ((c >> 10) & 31) * 255 / 31, g = ((c >> 5) & 31) * 255 / 31, b = (c & 31) * 255 / 31;
        S32 best = 1, bestD = 0x7FFFFFFF;
        for (S32 i = 1; i < 256; i++) {
            const S32 dr = r - s_lutPal[i * 3], dg = g - s_lutPal[i * 3 + 1], db = b - s_lutPal[i * 3 + 2];
            const S32 d = dr * dr * 3 + dg * dg * 4 + db * db * 2;
            if (d < bestD) {
                bestD = d;
                best = i;
            }
        }
        s_lut15[c] = (U8)best;
    }
    s_lutMade = 1;
}

/* The GPU's frame read back, with the palette index each pixel was drawn with, which the GPU's target keeps
   (GpuRenderer_ReadbackPresentedIndexed): the plate's and the save picture's. */
static U32 *s_frameRgb = NULL;
static U8 *s_frameDrawn = NULL;
static size_t s_frameSize = 0;
static S32 ReadFrame(U32 w, U32 h) {
    if (s_frameSize < (size_t)w * h) {
        free(s_frameRgb);
        free(s_frameDrawn);
        s_frameRgb = (U32 *)malloc((size_t)w * h * sizeof(U32));
        s_frameDrawn = (U8 *)malloc((size_t)w * h);
        s_frameSize = (s_frameRgb AND s_frameDrawn) ? (size_t)w * h : 0;
    }
    if (!s_frameSize OR !GpuRenderer_ReadbackPresentedIndexed(s_frameRgb, s_frameDrawn, w, h))
        return FALSE;
    PlateLut();
    return TRUE;
}

/* One pixel of the frame read back as a palette index: the index it was drawn with, where its colour is still that index's (give or
   take the smoothing of the textures) -- what a box shades next, through the palette's ramps, is then the index the software would
   have shaded. The nearest colour of the whole palette was often another ramp's: the dialogue box's kerbs purple where the
   software's are grey, dark paving flecked with light (2026-11-26). Not where it has changed since -- a shadow darkened it, a TRANS
   polygon blended it: the nearest colour then. Never 0 (see-through over the GPU). */
static U8 FrameIndex(size_t i) {
    const U32 c = s_frameRgb[i];
    const U8 d = s_frameDrawn[i];
    if (d != 0) {
        const S32 dr = (S32)((c >> 16) & 0xFF) - PtrPal[d * 3], dg = (S32)((c >> 8) & 0xFF) - PtrPal[d * 3 + 1],
                  db = (S32)(c & 0xFF) - PtrPal[d * 3 + 2];
        if (abs(dr) <= 48 AND abs(dg) <= 48 AND abs(db) <= 48)
            return d;
    }
    return s_lut15[((c >> 9) & 0x7C00) | ((c >> 6) & 0x03E0) | ((c >> 3) & 0x001F)];
}

void GpuOverlay_Plate(void) {
    if (!GpuOverlayLive() OR !Log OR !PtrPal)
        return;
    const U32 w = ModeDesiredX, h = ModeDesiredY;
    if (s_plateLogSize < (size_t)w * h) {
        free(s_plateLog);
        s_plateLog = (U8 *)calloc((size_t)w * h, 1);
        s_plateLogSize = s_plateLog ? (size_t)w * h : 0;
    }
    if (s_plateMaskSize < (size_t)w * h) {
        free(s_plateMask);
        s_plateMask = (U8 *)malloc((size_t)w * h);
        s_plateMaskSize = s_plateMask ? (size_t)w * h : 0;
    }
    if (!s_plateMask OR !s_plateLog OR !ReadFrame(w, h))
        return;
    U8 *log = (U8 *)Log, *screen = (U8 *)Screen;
    for (size_t i = 0; i < (size_t)w * h; i++) {
        const U8 index = FrameIndex(i);
        if (log[i] == 0) {
            log[i] = index;
            s_plateLog[i] = index;
        }
        s_plateMask[i] = 0;
        if (screen AND screen[i] == 0) {
            screen[i] = index;
            s_plateMask[i] = 1;
        }
    }
    GpuRenderer_SetOverlayOpaqueRect(0, 0, (S32)w - 1, (S32)h - 1);
    s_platePinned = 1;
}

/* The picture on the screen as palette indices, into dst (ModeDesiredX x ModeDesiredY): Log over the GPU's frame as the present lays
   it -- Log's index 0 the frame's pixel, but inside the overlay's opaque rect, where it is black. The save's picture (SAVEGAME.CPP):
   scaled down from Log alone it was black wherever the GPU drew -- outdoors, every save made on OpenGL, its picture in the game menu's
   box and the save list black. FALSE without a GPU renderer (Log is the picture). */
S32 GpuOverlay_ScreenIndexed(U8 *dst) {
    if (!dst OR !GpuRenderer_IsActive() OR !Log OR !PtrPal)
        return FALSE;
    const U32 w = ModeDesiredX, h = ModeDesiredY;
    if (!ReadFrame(w, h))
        return FALSE;
    const U8 *log = (const U8 *)Log;
    for (U32 y = 0; y < h; y++)
        for (U32 x = 0; x < w; x++) {
            const size_t i = (size_t)y * w + x;
            dst[i] = (log[i] != 0 OR GpuRenderer_InOverlayOpaque((S32)x, (S32)y)) ? log[i] : FrameIndex(i);
        }
    return TRUE;
}

void GpuOverlay_Unplate(void) {
    if (!s_platePinned)
        return;
    s_platePinned = 0;
    // (a dialogue's window kept opaque: s_keepOn -- the rect's far edges exclusive, as the present shader tests them)
    if (s_keepOn)
        GpuRenderer_SetOverlayOpaqueRect(s_keepX0, s_keepY0, s_keepX1 + 1, s_keepY1 + 1);
    else
        GpuRenderer_SetOverlayOpaqueRect(0, 0, 0, 0);
    U8 *screen = (U8 *)Screen;
    if (screen AND s_plateMask AND s_plateMaskSize >= (size_t)ModeDesiredX * ModeDesiredY)
        for (size_t i = 0; i < (size_t)ModeDesiredX * ModeDesiredY; i++)
            if (s_plateMask[i])
                screen[i] = 0;
    // (and Log's: see-through again where it is still the plate's -- what was drawn or shaded over it since is the page's own and stays)
    U8 *log = (U8 *)Log;
    if (log AND s_plateLog AND s_plateLogSize >= (size_t)ModeDesiredX * ModeDesiredY)
        for (size_t i = 0; i < (size_t)ModeDesiredX * ModeDesiredY; i++) {
            if (s_plateLog[i] AND log[i] == s_plateLog[i])
                log[i] = 0;
            s_plateLog[i] = 0;
        }
}

/* A part of the plated picture the page has made its own, in Log and in Screen both: kept by GpuOverlay_Unplate as it is. A dialogue's
   window (MESSAGE.CPP NewInitDialWindow): the rows from its top down shaded and copied into Screen, which the software keeps frozen
   while the speakers' boxes redraw above it -- its page turns and the boxes cleaned over it come back from Screen. Emptied at the
   next scene frame, the box's darkest pixels (shaded to the colour the plate had put there) went see-through, and Screen's copy
   was emptied whole: each speaking step's clean of a box reaching into the window (the after-Raph cut scene's parked car, at the
   bottom) left a hole of the bare ground there, the car gone from under the text. Put back from ScreenAux as the dialogue ends. */
void GpuOverlay_KeepPlate(S32 x0, S32 y0, S32 x1, S32 y1) {
    if (!s_platePinned OR !s_plateLog OR !s_plateMask)
        return;
    const S32 w = (S32)ModeDesiredX, h = (S32)ModeDesiredY;
    if ((size_t)w * h > s_plateLogSize OR (size_t)w * h > s_plateMaskSize)
        return;
    if (x0 < 0)
        x0 = 0;
    if (y0 < 0)
        y0 = 0;
    if (x1 > w - 1)
        x1 = w - 1;
    if (y1 > h - 1)
        y1 = h - 1;
    s_keepOn = 1;
    s_keepX0 = x0;
    s_keepY0 = y0;
    s_keepX1 = x1;
    s_keepY1 = y1;
    for (S32 y = y0; y <= y1; y++)
        for (S32 x = x0; x <= x1; x++) {
            s_plateLog[(size_t)y * w + x] = 0;
            s_plateMask[(size_t)y * w + x] = 0;
        }
}

/* A scene frame begins (OBJECT.CPP AffScene): one that isn't a dialogue's speaking step is past the dialogue -- its kept window
   no longer shown opaque. */
void GpuOverlay_SceneFrame(S32 speaking) {
    if (!s_keepOn OR speaking)
        return;
    s_keepOn = 0;
    if (!s_platePinned)
        GpuRenderer_SetOverlayOpaqueRect(0, 0, 0, 0);
}

/* (a scene frame: the GPU's frame under the box darkened, Log's own pixels too) */
static void ShadeBoxGpu(S32 x0, S32 y0, S32 x1, S32 y1, S32 deccoul) {
    GpuRenderer_ShadowBeginFlat(deccoul);
    for (S32 y = y0; y <= y1; y++)
        GpuRenderer_ShadowSpan(x0, y, x1);
    GpuRenderer_ShadowEnd();
    ShadeBoxBlkOverlay(x0, y0, x1, y1, deccoul);
}

/* A black of the palette's other than index 0 (which shows the GPU's frame through): the race's black boxes */
U8 GpuOverlay_Black(void) {
    static U8 *pal = NULL;
    static U8 black = 0;
    static U8 seen[768];
    if (!PtrPal)
        return 0;
    if (pal != PtrPal OR memcmp(seen, PtrPal, 768)) {
        pal = PtrPal;
        memcpy(seen, PtrPal, 768);
        S32 best = 0x7FFFFFFF;
        for (S32 i = 1; i < 256; i++) {
            const S32 d = PtrPal[i * 3] + PtrPal[i * 3 + 1] + PtrPal[i * 3 + 2];
            if (d < best) {
                best = d;
                black = (U8)i;
            }
        }
    }
    return black;
}

/* The race's minimap darkens Log's own pixels under it: over the GPU's frame, in a scene frame, a see-through one is left see-through and
   given back to be darkened on the GPU's frame instead (GpuOverlay_ShadeRun) */
S32 GpuOverlay_LiveScene(void) { return GpuSceneDrawing AND GpuOverlayLive(); }
void GpuOverlay_ShadeBegin(S32 deccoul) { GpuRenderer_ShadowBeginFlat(deccoul); }   // (one batch: each begin flushes the GPU's)
void GpuOverlay_ShadeRun(S32 x0, S32 y, S32 x1) { GpuRenderer_ShadowSpan(x0, y, x1); }
void GpuOverlay_ShadeEnd(void) { GpuRenderer_ShadowEnd(); }

void BoxFlow(S32 x, S32 y, S32 coul) {
    if (x >= ClipXMax || x < ClipXMin || y >= ClipYMax || y < ClipYMin) {
        return;
    }

    U8 *dst = (U8 *)Log + TabOffLine[y] + x;
    U32 screenWidth = TabOffLine[1];
    dst[0] = coul;
    dst[1] = coul;
    dst[screenWidth] = coul;
    dst[screenWidth + 1] = coul;
}

void ShadeBoxBlkOverlay(S32 x0, S32 y0, S32 x1, S32 y1, S32 deccoul);

void ShadeBoxBlk(S32 x0, S32 y0, S32 x1, S32 y1, S32 deccoul) {
    if (x0 > ClipXMax || x1 < ClipXMin || y0 > ClipYMax || y1 < ClipYMin) {
        return;
    }

    if (x0 < ClipXMin) {
        x0 = ClipXMin;
    }
    if (x1 > ClipXMax) {
        x1 = ClipXMax;
    }
    if (y0 < ClipYMin) {
        y0 = ClipYMin;
    }
    if (y1 > ClipYMax) {
        y1 = ClipYMax;
    }

    // (over the GPU renderer's frame: see GpuSceneDrawing above)
    if (GpuOverlayLive()) {
        if (GpuSceneDrawing) {
            ShadeBoxGpu(x0, y0, x1, y1, deccoul);
            return;
        }
        GpuOverlay_Plate();
    }

    S32 deltaY = y1 - y0 + 1;
    S32 deltaX = x1 - x0 + 1;

    U8 *dst = (U8 *)Log + TabOffLine[y0] + x0;
    U32 screenWidth = TabOffLine[1];

    U32 clutOffset = 15 * 256;
    U8 clutOffsetHighByte = (clutOffset >> 8) & 0xFF;
    clutOffsetHighByte -= (U8)(deccoul & 0xFF);
    clutOffset = (clutOffset & 0xFFFF00FF) | (clutOffsetHighByte << 8);
    U8 *clut = PtrCLUTGouraud + clutOffset;

    for (S32 y = 0; y < deltaY; y++) {
        for (S32 x = 0; x < deltaX; x++) {
            dst[x] = clut[dst[x]];
        }
        dst += screenWidth;
    }
}

/* Dual-path shadow darken (GpuScene interior): same CLUT row as ShadeBoxBlk,
   but index 0 is the overlay's transparent hole — leave it so the FBO shows
   through. clut[0] is an opaque shade colour (224 on the retail fog table),
   so applying it would paint the whole shadow span as a solid hole over the
   room and body. Non-zero overlay pixels (sprites, HUD, re-blit) still darken. */
void ShadeBoxBlkOverlay(S32 x0, S32 y0, S32 x1, S32 y1, S32 deccoul) {
    if (x0 > ClipXMax || x1 < ClipXMin || y0 > ClipYMax || y1 < ClipYMin) {
        return;
    }

    if (x0 < ClipXMin) {
        x0 = ClipXMin;
    }
    if (x1 > ClipXMax) {
        x1 = ClipXMax;
    }
    if (y0 < ClipYMin) {
        y0 = ClipYMin;
    }
    if (y1 > ClipYMax) {
        y1 = ClipYMax;
    }

    S32 deltaY = y1 - y0 + 1;
    S32 deltaX = x1 - x0 + 1;

    U8 *dst = (U8 *)Log + TabOffLine[y0] + x0;
    U32 screenWidth = TabOffLine[1];

    U32 clutOffset = 15 * 256;
    U8 clutOffsetHighByte = (clutOffset >> 8) & 0xFF;
    clutOffsetHighByte -= (U8)(deccoul & 0xFF);
    clutOffset = (clutOffset & 0xFFFF00FF) | (clutOffsetHighByte << 8);
    U8 *clut = PtrCLUTGouraud + clutOffset;

    for (S32 y = 0; y < deltaY; y++) {
        for (S32 x = 0; x < deltaX; x++) {
            if (dst[x] != 0) {
                dst[x] = clut[dst[x]];
            }
        }
        dst += screenWidth;
    }
}

void CopyBlockShade(S32 x0, S32 y0, S32 x1, S32 y1, U8 *src,
                    S32 xd, S32 yd, U8 *dst, S32 shade) {
    U8 *srcPtr = src + TabOffLine[y0] + x0;
    U8 *dstPtr = dst + TabOffLine[yd] + xd;

    S32 deltaX = x1 - x0;
    if (deltaX < 0)
        return;
    deltaX++;
    if (deltaX > 640)
        return;

    S32 deltaY = y1 - y0;
    if (deltaY < 0)
        return;
    deltaY++;
    if (deltaY > 480)
        return;

    S32 scanLine = TabOffLine[1];
    S32 deltaScreen = scanLine - deltaX;
    U8 shadeAmount = (U8)(shade & 0xFF);
    S32 nbLines = deltaY + 1;

    for (S32 y = 0; y < nbLines; y++) {
        for (S32 x = 0; x < deltaX; x++) {
            U8 srcPixel = *srcPtr;
            U8 highNibble = srcPixel & 0xF0;
            S32 shadedValue = (srcPixel & 0x0F) - shadeAmount;

            if (shadedValue >= 0) {
                *dstPtr = (U8)(highNibble | (shadedValue & 0x0F));
            } else {
                *dstPtr = highNibble;
            }

            srcPtr++;
            dstPtr++;
        }

        srcPtr += deltaScreen;
        dstPtr += deltaScreen;
    }
}
