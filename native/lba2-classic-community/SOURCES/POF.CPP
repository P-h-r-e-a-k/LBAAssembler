#include "C_EXTERN.H"
#include <GPU/GPURENDERER.H> // GpuRenderer_DrawRun
#include <GL/RENDER_GL.H>    // GpuScene
#include <POLYGON/POLY.H>    // GET_ZO
#include <3D/CAMERA.H>       // CameraZr
#include <stdlib.h>
#include <string.h>

//***************************************************************************
S32 PofDisplay3DExt(S32 x, S32 y, S32 z, S32 num_pof, S32 scale, S32 rotation) {
    S32 coul, n;
    S32 x0, y0, scale3d;
    S32 xpc, ypc;
    S16 *ptr_point, *ptrw;
    U8 p;
    U8 *ptr_pof;

    //	ptr_pof = (U8*)HQR_Get( HQR_Pof, num_pof ) ;

    ptr_pof = BufferPof + *(((U32 *)BufferPof) + num_pof);

    LongWorldRotatePoint(x, y, z);
    x0 = Z0;
    // (its depth on the GPU's frame outdoors -- the extras' sprites' (OBJECT.CPP TryGpuExtraBank); -1: drawn in Log, as ever)
    extern S32 GpuOverlay_LiveScene(void);
    const S32 gpuZo = (GpuRenderer_IsActive() && CubeMode == CUBE_EXTERIEUR && (GpuScene || GpuOverlay_LiveScene()))
                          ? (S32)GET_ZO(CameraZr - Z0) : -1;

    if (!LongProjectPoint(X0, Y0 + 1000, Z0))
        return FALSE;
    y0 = Yp;

    if (!LongProjectPoint(X0, Y0, Z0))
        return FALSE;
    xpc = Xp * 65536 + 32768;
    ypc = Yp * 65536 + 32768;

    scale3d = ((Yp - y0) * 65536 + scale / 2) / scale;

    coul = *ptr_pof++; // couleur

    n = *ptr_pof++; // nb point

    ClearScreenMinMax();

    if (rotation != 0) {
        ptrw = (S16 *)ptr_pof;
        ptr_point = (S16 *)Obj_ListRotatedPoints;

        for (p = 0; p < n; p++) {
            Rotate(ptrw[0], ptrw[1], rotation);

            Xp = (xpc + X0 * scale3d) / 65536;
            Yp = (ypc + Z0 * scale3d) / 65536;

            AdjustScreenMinMax();

            *ptr_point++ = (S16)Xp;
            *ptr_point++ = (S16)Yp;
            ptrw += 2;
        }

        ptr_pof = (U8 *)ptrw;
    } else {
        ptrw = (S16 *)ptr_pof;
        ptr_point = (S16 *)Obj_ListRotatedPoints;

        for (p = 0; p < n; p++) {
            Xp = (xpc + (*ptrw++) * scale3d) / 65536;
            Yp = (ypc + (*ptrw++) * scale3d) / 65536;

            AdjustScreenMinMax();

            *ptr_point++ = (S16)Xp;
            *ptr_point++ = (S16)Yp;
        }

        ptr_pof = (U8 *)ptrw;
    }

    // (outdoors on the GPU's frame: drawn in software as ever, then the pixels the lines changed laid on the frame as runs at the
    // pof's depth -- GpuRenderer_DrawRun -- and taken out of Log, which lies over the whole frame: left there, the star lay over the
    // bodies the GPU drew after it, where the software's later draws cover it)
    const S32 gpu = gpuZo >= 0 && ScreenXMin <= ScreenXMax && ScreenYMin <= ScreenYMax;
    S32 bx0 = 0, by0 = 0, bx1 = -1, by1 = -1;
    static U8 *before = NULL;
    static size_t beforeSize = 0;
    if (gpu) {
        bx0 = ScreenXMin < 0 ? 0 : ScreenXMin;
        by0 = ScreenYMin < 0 ? 0 : ScreenYMin;
        bx1 = ScreenXMax > ModeDesiredX - 1 ? ModeDesiredX - 1 : ScreenXMax;
        by1 = ScreenYMax > ModeDesiredY - 1 ? ModeDesiredY - 1 : ScreenYMax;
        const size_t need = (bx1 >= bx0 && by1 >= by0) ? (size_t)(bx1 - bx0 + 1) * (size_t)(by1 - by0 + 1) : 0;
        if (need > beforeSize) {
            U8 *grown = (U8 *)realloc(before, need);
            if (grown) {
                before = grown;
                beforeSize = need;
            }
        }
        if (need == 0 || need > beforeSize)
            bx1 = bx0 - 1;   // (nothing to lay: the software's lines stay in Log)
        else
            for (S32 y = by0; y <= by1; y++)
                memcpy(before + (size_t)(y - by0) * (bx1 - bx0 + 1), (U8 *)Log + TabOffLine[y] + bx0, (size_t)(bx1 - bx0 + 1));
    }

    n = *ptr_pof++; // nb line
    while (n > 0) {
        p = *ptr_pof++;

        ptr_point = (S16 *)Obj_ListRotatedPoints + p * 2;

        x0 = *ptr_point++;
        y0 = *ptr_point;

        p = *ptr_pof++;

        ptr_point = (S16 *)Obj_ListRotatedPoints + p * 2;

        Xp = *ptr_point++;
        Yp = *ptr_point;

        Line(x0, y0, Xp, Yp, coul);

        n--;
    }

    if (gpu && bx1 >= bx0) {
        const S32 w = bx1 - bx0 + 1;
        for (S32 y = by0; y <= by1; y++) {
            const U8 *row = (const U8 *)Log + TabOffLine[y];
            const U8 *was = before + (size_t)(y - by0) * w;
            for (S32 x = bx0; x <= bx1;) {
                if (row[x] == was[x - bx0] || row[x] == 0) {
                    x++;
                    continue;
                }
                const U8 c = row[x];
                S32 e = x;
                while (e + 1 <= bx1 && row[e + 1] == c && row[e + 1] != was[e + 1 - bx0])
                    e++;
                GpuRenderer_DrawRun(x, e, y, c, (U16)gpuZo);   // (FALSE: left in Log, as the software drew it)
                x = e + 1;
            }
        }
    }

    return (TRUE);
}
