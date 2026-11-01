// ──────────────────────────────────────────────────────────────────────────
// PNG Screenshot Support (using stb_image_write)
// ──────────────────────────────────────────────────────────────────────────
#ifndef SAVEPNG_H
#define SAVEPNG_H

#include <SYSTEM.H>

extern void SavePNG(char *filename, void *screen, S32 xres, S32 yres, void *ptrpalette);
extern S32 SaveRgbPNG(char *filename, void *rgb, S32 xres, S32 yres);

#endif // SAVEPNG_H
