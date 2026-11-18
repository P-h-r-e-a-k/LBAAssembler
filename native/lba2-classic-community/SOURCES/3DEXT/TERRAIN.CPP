#include "DEFINES.H"
#include "../PERSO.H"
#include <stdlib.h> /* getenv: LBA2_TERRAIN_FLOAT */

#include <SVGA/SCREEN.H>
#include <GPU/GPURENDERER.H> // GpuRenderer_IsActive: exterior Log clear under the GPU hybrid

#define LIMITE_LOIN (60 * 1024)
#define COLOR_LIMITE 255
#define SIZE_TEXTURE 64

#define WORLD_SIZE 32768
#define NB_POLYS_COTE 4

// Taille 2D mini pour accepter un poly animé
#define ANI_POLY_STEP_Y 2
#define ANI_POLY_STEP_X 5

/* The terrain, sea and sky draw through one camera path (CAMERA.H), picked
 * as the 1997 Chooser picked a routine (CHOOSER.ASM, at 333929a).
 *
 * With the terrain-seams retail fix on, it is the integer one (IMATSTDI,
 * LROT3DI, LIROT3DI, LPROJ3DI): integer maths gives two neighbouring cubes the
 * same pixels on the edge they share, where the float path leaves hairline
 * seams, and the same on every platform. Its projection rounds instead of
 * truncating, so the ground lands where the float objects standing on it do
 * (LongProjectPointIRound). Decors and objects stay on the float path.
 *
 * With the fix off, it is the float one, with the 1997 nudge in
 * DrawHorizon2ZBuf: what a 1997 Pentium drew, and lba2cc before the fix. */
static const T_CAMERA_PATH *TerrPath = &CameraPathFloat;

/* Upstream switches this with its "Fix retail bugs" settings (RETAILFIX.CPP, which this engine has not taken): here the integer path is
   on, and LBA2_TERRAIN_FLOAT=1 puts the float one back -- what was drawn before, for comparing the two (2026-10-30). */
static S32 TerrainSeamsFixed(void) {
    static S32 on = -1;
    if (on < 0) {
        const char *f = getenv("LBA2_TERRAIN_FLOAT");
        on = (f && f[0] == '1') ? 0 : 1;
    }
    return on;
}

static void PickTerrainPath(void) {
    TerrPath = TerrainSeamsFixed() ? &CameraPathInteger : &CameraPathFloat;
}

#include "DRAWSKY.CPP"

S32 Sky_Y;
//S32	NbDrawnPolys ;

S32 FlagMasque = TRUE;

#if defined(DEBUG_TOOLS) && defined(LBA_GAME)
extern S32 DebugFps;
#endif

#ifndef LBA_GAME
S32 Reglage = 0;
#endif

#ifdef LBA_EDITOR // Special PERSO.EXE
#define INFO_DECORS 1024
extern S32 FlagInfos;
#endif

//****************************************************************************
//****************************************************************************

S32 ListTypePolygone[4] = {0, POLY_SOLID, POLY_GOURAUD_TABLE, POLY_DITHER_TABLE};
S32 ListTypePTexture[] = {
    0,
    POLY_TEXTURE,
    POLY_TEXTURE_FLAT,
    POLY_TEXTURE_GOURAUD,
    0,
    POLY_TEXTURE_INCRUST,
    POLY_TEXTURE_INCRUST_FLAT,
    POLY_TEXTURE_INCRUST_GOURAUD,
    0,
    POLY_TEXTURE_INCRUST,
    POLY_TEXTURE_INCRUST_FLAT,
    POLY_TEXTURE_INCRUST_GOURAUD,
    0,
    POLY_TEXTURE_INCRUST,
    POLY_TEXTURE_INCRUST_FLAT,
    POLY_TEXTURE_INCRUST_GOURAUD,
};

/*══════════════════════════════════════════════════════════════════════════*
		   ▀▀█▀▀ █▀▀▀▀ █▀▀▀█ █▀▀▀█ █▀▀▀█  █    ██▄ █
		     ██  ██▀▀  ██▀█▀ ██▀█▀ ██▀▀█  ██   ██▀██
		     ▀▀  ▀▀▀▀▀ ▀▀  ▀ ▀▀  ▀ ▀▀  ▀  ▀▀   ▀▀  ▀
 *══════════════════════════════════════════════════════════════════════════*/

/*──────────────────────────────────────────────────────────────────────────*/

void DrawSkySeaZBuf() {
    // draw Sky
    //	Sky_Y = 12500 ;

    PtrMap = SkySeaTexture;
    RepMask = 0x7F7F;

    DrawSky(Sky_Y);
    //	DrawSea( Y_SEA ) ;
}

/*──────────────────────────────────────────────────────────────────────────*/

void ClsTerrainZBuf() {
    extern U32 DecorFrame;
    DecorFrame++; // (a new frame: every decor may be drawn once again -- DECORS.CPP DrawnThisFrame)
    if (DecorFrame == 0) DecorFrame = 1;
    /* Software-plate passes force the software fillers while the backend is
       still active (GpuRenderer_SetSoftwarePolys): the clear must then be
       the software FogCoul fill, or the plate would start from zeros and
       ShadeBoxBlk would darken holes instead of scene colours. */
    if (GpuRenderer_IsActive() && !GpuRenderer_GetSoftwarePolys()) {
        /* The GPU target already holds this frame's terrain; only palette
           index 0 is transparent in the present composite, so a FogCoul fill
           here would park an opaque overlay over that target. Keep Log at
           index 0 so the offscreen scene shows through. */
        SetClearColor(0);
        Cls();
    } else {
        SetClearColor((U8)FogCoul);
        Cls();
        SetClearColor(0);
    }

    memset(PtrZBuffer, 255, SIZE_Z_BUFFER); // Clear ZBuffer
}

/*──────────────────────────────────────────────────────────────────────────*/
// A ground triangle's texture definition, and its page as the texture map (PtrMap): an island with more ground pages (VAR_EXT.H) has
// the page in IndexTex's top bits; a retail island's IndexTex is the definition alone, on GroundTexture.
static inline const T_HALF_TEX *GroundTexDef(const T_HALF_POLY *ptrh) {
    if (NbGroundPages > 1) {
        U32 page = ptrh->IndexTex >> GROUND_PAGE_SHIFT;
        PtrMap = (page AND page < (U32)NbGroundPages) ? GroundPageBuf + (page - 1) * 256 * 256 : GroundTexture;
        // (and the definition's eleventh bit the triangle's Dummy, unused by the retail islands: a cube of such an island has room for
        // 2048 -- 2026-10-06, Polar Island at twice LBA1's size, its road's smooth kerbs)
        return &ListTexDef[(ptrh->IndexTex & ((1 << GROUND_PAGE_SHIFT) - 1)) | (ptrh->Dummy << GROUND_PAGE_SHIFT)];
    }
    return &ListTexDef[ptrh->IndexTex];
}

// Whether a ground triangle is drawn: it has a colour or a texture, and it is not one of the ground's own animated sea or gas triangles
// (CodeJeu 12..15 -- Island CX's gas round its plateau, the shallow water at the retail islands' shores) while the editor's temporary
// "Sea/gas" toggle hides them, with the sea plane, to see what is under them (RENDERER_API.CPP lba2_renderer_set_draw_sea_polys; the game
// never turns it off). (RendererDrawSea: the sea plane's flag, here and in the horizon's cubes, DrawHorizon2ZBuf.)
extern U8 RendererDrawSeaPolys;
extern U8 RendererDrawSea;
static inline S32 HalfPolyShown(const T_HALF_POLY *h) {
    return (h->PolyFlag + h->TexFlag) AND(RendererDrawSeaPolys OR(h->CodeJeu & 12) != 12);
}

// A cell of the ground, or (s > 1, the ground further off: TileStepFitted) the s x s cells from it as one, all of one look: their
// outer corners, and the first cell's texture, colour and cut
static inline void DrawFeuillePolyZBuf(S32 xi, S32 zi, T_SOMMET_ROT *ptrs, S32 s = 1) {
    S32 color;
    T_HALF_POLY *ptrh;
    S32 f0, f1, f2, f3;
    S32 i0, i1, i2, i3;
    S32 x0, z0;
    S32 x1, z1;
    S32 x2, z2;
    S32 x3, z3;
    U8 type;
    U16 fog;

    ptrh = &MapPolyGround[zi * (64 * 2) + xi * 2];

    // optimisation globale affichage terrain
    if (!(ptrh->PolyFlag + ptrh->TexFlag +
          (ptrh + 1)->PolyFlag + (ptrh + 1)->TexFlag)) {
        return;
    }

    x0 = ptrs[0].X2D;
    z0 = ptrs[0].Y2D;
    x1 = ptrs[65 * s].X2D;
    z1 = ptrs[65 * s].Y2D;
    x3 = ptrs[s].X2D;
    z3 = ptrs[s].Y2D;
    x2 = ptrs[65 * s + s].X2D;
    z2 = ptrs[65 * s + s].Y2D;

    f0 = ptrs[0].Zrot;

    /*	PtrCLUTGouraud = PtrCLUTFog +
		(16*256) * BoundRegleTrois(0,15,
					ClipZFar - StartZFog,
					f0 - StartZFog ) ;
*/
    fog = (U16)(PalLevel + 16 * BoundRegleTrois(0, 15,
                                                ClipZFar - StartZFog,
                                                f0 - StartZFog));
    SetCLUT(fog);

    f0 = GET_ZO(f0);
    f1 = GET_ZO(ptrs[65 * s].Zrot);
    f3 = GET_ZO(ptrs[s].Zrot);
    f2 = GET_ZO(ptrs[65 * s + s].Zrot);

    {
        U8 *i = &MapIntensity[zi * 65 + xi];

        i0 = (i[0] & 15) * 256 - 255;
        i1 = (i[65 * s] & 15) * 256 - 255;
        i3 = (i[s] & 15) * 256 - 255;
        i2 = (i[65 * s + s] & 15) * 256 - 255;

        if (i0 < 0)
            i0 = 0; // groumpf
        if (i1 < 0)
            i1 = 0; // groumpf
        if (i2 < 0)
            i2 = 0; // groumpf
        if (i3 < 0)
            i3 = 0; // groumpf

/*		i0 = i[0]*256 		;
		i1 = i[65]*256		;
		i3 = i[1]*256 		;
		i2 = i[65+1]*256 	;
*/	}

if (ptrh->Sens == 0) {
    if (HalfPolyShown(ptrh)) {
        // type 0 triangle 0----------------------------

        Tab_Points[0].Pt_XE = (S16)x0;
        Tab_Points[0].Pt_YE = (S16)z0;
        Tab_Points[1].Pt_XE = (S16)x1;
        Tab_Points[1].Pt_YE = (S16)z1;
        Tab_Points[2].Pt_XE = (S16)x2;
        Tab_Points[2].Pt_YE = (S16)z2;

        if (TestVuePoly(Tab_Points)) {
            color = (ptrh->Bank << 4) + 11;

            Tab_Points[0].Pt_Light = (S16)i0;
            Tab_Points[1].Pt_Light = (S16)i1;
            Tab_Points[2].Pt_Light = (S16)i2;

            Tab_Points[0].Pt_ZO = (U16)f0;
            Tab_Points[1].Pt_ZO = (U16)f1;
            Tab_Points[2].Pt_ZO = (U16)f2;

            if (ptrh->PolyFlag) {
                if (ptrh->PolyFlag == 1) {
                    color += ((i0 + i1 + i2) * 21845) >> 24; // *21845 >> 16 EQU / 3
                }

                Fill_Poly(ListTypePolygone[ptrh->PolyFlag],
                          color, 3,
                          Tab_Points);
            }

            if (ptrh->TexFlag) {
                T_HALF_TEX *ptrt;

                ptrt = (T_HALF_TEX *)GroundTexDef(ptrh);

                Tab_Points[0].Pt_MapU = ptrt->Tx0;
                Tab_Points[0].Pt_MapV = ptrt->Ty0;
                Tab_Points[1].Pt_MapU = ptrt->Tx1;
                Tab_Points[1].Pt_MapV = ptrt->Ty1;
                Tab_Points[2].Pt_MapU = ptrt->Tx2;
                Tab_Points[2].Pt_MapV = ptrt->Ty2;

                if (ptrh->TexFlag == 2) {
                    color = ((i0 + i1 + i2) * 21845) >> 24; // *21845 >> 16 EQU / 3
                }

                type = (U8)ListTypePTexture[ptrh->TexFlag + (ptrh->PolyFlag << 2)];

                if (Fill_Poly(type, color, 3, Tab_Points)) {
                    if (MaxPolySea AND(ptrh->CodeJeu & 12) == 12) { // hihi je suis un polygone animé...

                        if (NbAniPoly < MaxPolySea - 1) {
                            PtrAniPoly->X0 = (S16)Min3(x0, x1, x2);
                            PtrAniPoly->Y0 = (S16)Min3(z0, z1, z2);
                            PtrAniPoly->X1 = (S16)Max3(x0, x1, x2);
                            PtrAniPoly->Y1 = (S16)Max3(z0, z1, z2);

                            if (((PtrAniPoly->Y1 - PtrAniPoly->Y0) > ANI_POLY_STEP_Y)
                                    AND((PtrAniPoly->X1 - PtrAniPoly->X0) > ANI_POLY_STEP_X)) {
                                PtrAniPoly->Type = type;
                                PtrAniPoly->Color = (U8)color;

                                PtrAniPoly->Fog = fog;

                                PtrAniPoly->Tab_Points[0] = Tab_Points[0];
                                PtrAniPoly->Tab_Points[1] = Tab_Points[1];
                                PtrAniPoly->Tab_Points[2] = Tab_Points[2];

                                NbAniPoly++;
                                PtrAniPoly++;
                            }
                        }
#ifdef DEBUG_TOOLS
                        else {
                            Message("MaxPolySea Atteint !", TRUE);
                        }
#endif
                    }
                }
            }
        }
    }
    // type 0 triangle 1----------------------------------

    ptrh++;

    if (HalfPolyShown(ptrh)) {
        Tab_Points[0].Pt_XE = (S16)x2;
        Tab_Points[0].Pt_YE = (S16)z2;
        Tab_Points[1].Pt_XE = (S16)x3;
        Tab_Points[1].Pt_YE = (S16)z3;
        Tab_Points[2].Pt_XE = (S16)x0;
        Tab_Points[2].Pt_YE = (S16)z0;

        if (TestVuePoly(Tab_Points)) {
            color = (ptrh->Bank << 4) + 11;

            Tab_Points[0].Pt_Light = (S16)i2;
            Tab_Points[1].Pt_Light = (S16)i3;
            Tab_Points[2].Pt_Light = (S16)i0;

            Tab_Points[0].Pt_ZO = (U16)f2;
            Tab_Points[1].Pt_ZO = (U16)f3;
            Tab_Points[2].Pt_ZO = (U16)f0;

            if (ptrh->PolyFlag) {
                if (ptrh->PolyFlag == 1) {
                    color += ((i0 + i2 + i3) * 21845) >> 24; // *21845 >> 16 EQU / 3
                }
                Fill_Poly(ListTypePolygone[ptrh->PolyFlag],
                          color, 3,
                          Tab_Points);
            }

            if (ptrh->TexFlag) {
                T_HALF_TEX *ptrt;

                ptrt = (T_HALF_TEX *)GroundTexDef(ptrh);

                Tab_Points[0].Pt_MapU = ptrt->Tx0;
                Tab_Points[0].Pt_MapV = ptrt->Ty0;
                Tab_Points[1].Pt_MapU = ptrt->Tx1;
                Tab_Points[1].Pt_MapV = ptrt->Ty1;
                Tab_Points[2].Pt_MapU = ptrt->Tx2;
                Tab_Points[2].Pt_MapV = ptrt->Ty2;

                if (ptrh->TexFlag == 2) {
                    color = ((i0 + i2 + i3) * 21845) >> 24; // *21845 >> 16 EQU / 3
                }

                type = (U8)ListTypePTexture[ptrh->TexFlag + (ptrh->PolyFlag << 2)];

                if (Fill_Poly(type, color, 3, Tab_Points)) {
                    if (MaxPolySea AND(ptrh->CodeJeu & 12) == 12) { // hihi je suis un polygone animé...
                        if (NbAniPoly < MaxPolySea - 1) {
                            PtrAniPoly->X0 = (S16)Min3(x0, x2, x3);
                            PtrAniPoly->Y0 = (S16)Min3(z0, z2, z3);
                            PtrAniPoly->X1 = (S16)Max3(x0, x2, x3);
                            PtrAniPoly->Y1 = (S16)Max3(z0, z2, z3);

                            if (((PtrAniPoly->Y1 - PtrAniPoly->Y0) > ANI_POLY_STEP_Y)
                                    AND((PtrAniPoly->X1 - PtrAniPoly->X0) > ANI_POLY_STEP_X)) {
                                PtrAniPoly->Type = type;
                                PtrAniPoly->Color = (U8)color;

                                PtrAniPoly->Fog = fog;

                                PtrAniPoly->Tab_Points[0] = Tab_Points[0];
                                PtrAniPoly->Tab_Points[1] = Tab_Points[1];
                                PtrAniPoly->Tab_Points[2] = Tab_Points[2];

                                NbAniPoly++;
                                PtrAniPoly++;
                            }
                        }
#ifdef DEBUG_TOOLS
                        else {
                            Message("MaxPolySea Atteint !", TRUE);
                        }
#endif
                    }
                }
            }
        }
    }
} else // type == 1---------------------------------------------------
{
    // type 1 triangle 0

    if (HalfPolyShown(ptrh)) {
        Tab_Points[0].Pt_XE = (S16)x3;
        Tab_Points[0].Pt_YE = (S16)z3;
        Tab_Points[1].Pt_XE = (S16)x0;
        Tab_Points[1].Pt_YE = (S16)z0;
        Tab_Points[2].Pt_XE = (S16)x1;
        Tab_Points[2].Pt_YE = (S16)z1;

        if (TestVuePoly(Tab_Points)) {
            color = (ptrh->Bank << 4) + 11;

            Tab_Points[0].Pt_Light = (S16)i3;
            Tab_Points[1].Pt_Light = (S16)i0;
            Tab_Points[2].Pt_Light = (S16)i1;

            Tab_Points[0].Pt_ZO = (U16)f3;
            Tab_Points[1].Pt_ZO = (U16)f0;
            Tab_Points[2].Pt_ZO = (U16)f1;

            if (ptrh->PolyFlag) {
                if (ptrh->PolyFlag == 1) {
                    color += ((i0 + i1 + i3) * 21845) >> 24; // *21845 >> 16 EQU / 3
                }
                Fill_Poly(ListTypePolygone[ptrh->PolyFlag],
                          color, 3,
                          Tab_Points);
            }

            if (ptrh->TexFlag) {
                T_HALF_TEX *ptrt;

                ptrt = (T_HALF_TEX *)GroundTexDef(ptrh);

                Tab_Points[0].Pt_MapU = ptrt->Tx0;
                Tab_Points[0].Pt_MapV = ptrt->Ty0;
                Tab_Points[1].Pt_MapU = ptrt->Tx1;
                Tab_Points[1].Pt_MapV = ptrt->Ty1;
                Tab_Points[2].Pt_MapU = ptrt->Tx2;
                Tab_Points[2].Pt_MapV = ptrt->Ty2;

                if (ptrh->TexFlag == 2) {
                    color = ((i0 + i1 + i3) * 21845) >> 24; // *21845 >> 16 EQU / 3
                }

                type = (U8)ListTypePTexture[ptrh->TexFlag + (ptrh->PolyFlag << 2)];

                if (Fill_Poly(type, color, 3, Tab_Points)) {
                    if (MaxPolySea AND(ptrh->CodeJeu & 12) == 12) { // hihi je suis un polygone animé...
                        if (NbAniPoly < MaxPolySea - 1) {
                            PtrAniPoly->X0 = (S16)Min3(x0, x1, x3);
                            PtrAniPoly->Y0 = (S16)Min3(z0, z1, z3);
                            PtrAniPoly->X1 = (S16)Max3(x0, x1, x3);
                            PtrAniPoly->Y1 = (S16)Max3(z0, z1, z3);

                            if (((PtrAniPoly->Y1 - PtrAniPoly->Y0) > ANI_POLY_STEP_Y)
                                    AND((PtrAniPoly->X1 - PtrAniPoly->X0) > ANI_POLY_STEP_X)) {
                                PtrAniPoly->Type = type;
                                PtrAniPoly->Color = (U8)color;

                                PtrAniPoly->Fog = fog;

                                PtrAniPoly->Tab_Points[0] = Tab_Points[0];
                                PtrAniPoly->Tab_Points[1] = Tab_Points[1];
                                PtrAniPoly->Tab_Points[2] = Tab_Points[2];

                                NbAniPoly++;
                                PtrAniPoly++;
                            }
                        }
#ifdef DEBUG_TOOLS
                        else {
                            Message("MaxPolySea Atteint !", TRUE);
                        }
#endif
                    }
                }
            }
        }
    }
    // type 1 triangle 1

    ptrh++;

    if (HalfPolyShown(ptrh)) {
        Tab_Points[0].Pt_XE = (S16)x1;
        Tab_Points[0].Pt_YE = (S16)z1;
        Tab_Points[1].Pt_XE = (S16)x2;
        Tab_Points[1].Pt_YE = (S16)z2;
        Tab_Points[2].Pt_XE = (S16)x3;
        Tab_Points[2].Pt_YE = (S16)z3;

        if (TestVuePoly(Tab_Points)) {
            color = (ptrh->Bank << 4) + 11;

            Tab_Points[0].Pt_Light = (S16)i1;
            Tab_Points[1].Pt_Light = (S16)i2;
            Tab_Points[2].Pt_Light = (S16)i3;

            Tab_Points[0].Pt_ZO = (U16)f1;
            Tab_Points[1].Pt_ZO = (U16)f2;
            Tab_Points[2].Pt_ZO = (U16)f3;

            if (ptrh->PolyFlag) {
                if (ptrh->PolyFlag == 1) {
                    color += ((i1 + i2 + i3) * 21845) >> 24; // *21845 >> 16 EQU / 3
                }

                Fill_Poly(ListTypePolygone[ptrh->PolyFlag],
                          color, 3,
                          Tab_Points);
            }

            if (ptrh->TexFlag) {
                T_HALF_TEX *ptrt;

                ptrt = (T_HALF_TEX *)GroundTexDef(ptrh);

                Tab_Points[0].Pt_MapU = ptrt->Tx0;
                Tab_Points[0].Pt_MapV = ptrt->Ty0;
                Tab_Points[1].Pt_MapU = ptrt->Tx1;
                Tab_Points[1].Pt_MapV = ptrt->Ty1;
                Tab_Points[2].Pt_MapU = ptrt->Tx2;
                Tab_Points[2].Pt_MapV = ptrt->Ty2;

                if (ptrh->TexFlag == 2) {
                    color = ((i1 + i2 + i3) * 21845) >> 24; // *21845 >> 16 EQU / 3
                }

                type = (U8)ListTypePTexture[ptrh->TexFlag + (ptrh->PolyFlag << 2)];

                if (Fill_Poly(type, color, 3, Tab_Points)) {
                    if (MaxPolySea AND(ptrh->CodeJeu & 12) == 12) { // hihi je suis un polygone animé...
                        if (NbAniPoly < MaxPolySea - 1) {
                            PtrAniPoly->X0 = (S16)Min3(x1, x2, x3);
                            PtrAniPoly->Y0 = (S16)Min3(z1, z2, z3);
                            PtrAniPoly->X1 = (S16)Max3(x1, x2, x3);
                            PtrAniPoly->Y1 = (S16)Max3(z1, z2, z3);

                            if (((PtrAniPoly->Y1 - PtrAniPoly->Y0) > ANI_POLY_STEP_Y)
                                    AND((PtrAniPoly->X1 - PtrAniPoly->X0) > ANI_POLY_STEP_X)) {
                                PtrAniPoly->Type = type;
                                PtrAniPoly->Color = (U8)color;

                                PtrAniPoly->Fog = fog;

                                PtrAniPoly->Tab_Points[0] = Tab_Points[0];
                                PtrAniPoly->Tab_Points[1] = Tab_Points[1];
                                PtrAniPoly->Tab_Points[2] = Tab_Points[2];

                                NbAniPoly++;
                                PtrAniPoly++;
                            }
                        }
#ifdef DEBUG_TOOLS
                        else {
                            Message("MaxPolySea Atteint !", TRUE);
                        }
#endif
                    }
                }
            }
        }
    }
}

#if !(defined(DEMO) || defined(LBA_GAME))
if (FillFeuille & 4) {
    DrawColFeuille3D(x0, z0,
                     x1, z1,
                     x2, z2,
                     x3, z3,
                     MapPolyGround[zi * 64 * 2 + xi * 2].Sens,
                     CYAN,
                     MapPolyGround[zi * 64 * 2 + xi * 2].Col,
                     MapPolyGround[zi * 64 * 2 + xi * 2 + 1].Col);
}

if (FillFeuille & 2) {
    DrawFeuille3D(x0, z0,
                  x1, z1,
                  x2, z2,
                  x3, z3,
                  MapPolyGround[zi * 64 * 2 + xi * 2].Sens,
                  GREEN,
                  FALSE);
}
#endif
}

/*──────────────────────────────────────────────────────────────────────────*/
static inline void FillBlackPolyZBuf(S32 xi, S32 zi, T_SOMMET_ROT *ptrs, U8 *ptra) {
    T_HALF_POLY *ptrh;
    //	S32 		f0, f1, f2, f3 	;
    //	S32 		f00, f10, f20, f30 ;
    S32 x0, z0;
    S32 x1, z1;
    S32 x2, z2;
    S32 x3, z3;
    U8 b0, b1, b2, b3;

    ptrh = &MapPolyGround[zi * (64 * 2) + xi * 2];

    x0 = ptrs[0].X2D;
    z0 = ptrs[0].Y2D;
    b0 = (U8)(ptra[0] & 32);
    x1 = ptrs[65].X2D;
    z1 = ptrs[65].Y2D;
    b1 = (U8)(ptra[65] & 32);
    x3 = ptrs[1].X2D;
    z3 = ptrs[1].Y2D;
    b3 = (U8)(ptra[1] & 32);
    x2 = ptrs[66].X2D;
    z2 = ptrs[66].Y2D;
    b2 = (U8)(ptra[66] & 32);

    /*
	xi *= 512 ;
	zi *= 512 ;

	f0 = GET_ZO(ptrs[0].Zrot)	;
	f1 = GET_ZO(ptrs[65].Zrot)	;
	f3 = GET_ZO(ptrs[1].Zrot)	;
	f2 = GET_ZO(ptrs[66].Zrot)	;

	LongWorldRotatePoint(xi, 0, zi)	;
	f00 = GET_ZO(CameraZr - Z0)	;
	LongWorldRotatePoint(xi, 0, zi+512);
	f10 = GET_ZO(CameraZr - Z0)	;
	LongWorldRotatePoint(xi+512, 0, zi);
	f30 = GET_ZO(CameraZr - Z0)	;
	LongWorldRotatePoint(xi+512, 0, zi+512);
	f20 = GET_ZO(CameraZr - Z0)	;
*/

    if (!(b0 | b1)) {
        Tab_Points[0].Pt_XE = (S16)x0;
        Tab_Points[0].Pt_YE = (S16)z0;
        Tab_Points[1].Pt_XE = (S16)x1;
        Tab_Points[1].Pt_YE = (S16)z1;
        Tab_Points[2].Pt_XE = (S16)x1;
        Tab_Points[2].Pt_YE = (S16)((S32)ModeDesiredY - 1);
        Tab_Points[3].Pt_XE = (S16)x0;
        Tab_Points[3].Pt_YE = (S16)((S32)ModeDesiredY - 1);

        Tab_Points[0].Pt_Light = (S16)0;
        Tab_Points[1].Pt_Light = (S16)0;
        Tab_Points[2].Pt_Light = (S16)0;
        Tab_Points[3].Pt_Light = (S16)0;

        Tab_Points[0].Pt_ZO = (U16)0;
        Tab_Points[1].Pt_ZO = (U16)0;
        Tab_Points[2].Pt_ZO = (U16)0;
        Tab_Points[3].Pt_ZO = (U16)0;

        Fill_Poly(0, 0, 4, Tab_Points);
    }

    if (!(b1 | b2)) {
        Tab_Points[0].Pt_XE = (S16)x1;
        Tab_Points[0].Pt_YE = (S16)z1;
        Tab_Points[1].Pt_XE = (S16)x2;
        Tab_Points[1].Pt_YE = (S16)z2;
        Tab_Points[2].Pt_XE = (S16)x2;
        Tab_Points[2].Pt_YE = (S16)((S32)ModeDesiredY - 1);
        Tab_Points[3].Pt_XE = (S16)x1;
        Tab_Points[3].Pt_YE = (S16)((S32)ModeDesiredY - 1);

        Tab_Points[0].Pt_Light = (S16)0;
        Tab_Points[1].Pt_Light = (S16)0;
        Tab_Points[2].Pt_Light = (S16)0;
        Tab_Points[3].Pt_Light = (S16)0;

        Tab_Points[0].Pt_ZO = (U16)0;
        Tab_Points[1].Pt_ZO = (U16)0;
        Tab_Points[2].Pt_ZO = (U16)0;
        Tab_Points[3].Pt_ZO = (U16)0;

        Fill_Poly(0, 0, 4, Tab_Points);
    }

    if (!(b2 | b3)) {
        Tab_Points[0].Pt_XE = (S16)x2;
        Tab_Points[0].Pt_YE = (S16)z2;
        Tab_Points[1].Pt_XE = (S16)x3;
        Tab_Points[1].Pt_YE = (S16)z3;
        Tab_Points[2].Pt_XE = (S16)x3;
        Tab_Points[2].Pt_YE = (S16)((S32)ModeDesiredY - 1);
        Tab_Points[3].Pt_XE = (S16)x2;
        Tab_Points[3].Pt_YE = (S16)((S32)ModeDesiredY - 1);

        Tab_Points[0].Pt_Light = (S16)0;
        Tab_Points[1].Pt_Light = (S16)0;
        Tab_Points[2].Pt_Light = (S16)0;
        Tab_Points[3].Pt_Light = (S16)0;

        Tab_Points[0].Pt_ZO = (U16)0;
        Tab_Points[1].Pt_ZO = (U16)0;
        Tab_Points[2].Pt_ZO = (U16)0;
        Tab_Points[3].Pt_ZO = (U16)0;

        Fill_Poly(0, 0, 4, Tab_Points);
    }

    if (!(b3 | b0)) {
        Tab_Points[0].Pt_XE = (S16)x3;
        Tab_Points[0].Pt_YE = (S16)z3;
        Tab_Points[1].Pt_XE = (S16)x0;
        Tab_Points[1].Pt_YE = (S16)z0;
        Tab_Points[2].Pt_XE = (S16)x0;
        Tab_Points[2].Pt_YE = (S16)((S32)ModeDesiredY - 1);
        Tab_Points[3].Pt_XE = (S16)x3;
        Tab_Points[3].Pt_YE = (S16)((S32)ModeDesiredY - 1);

        Tab_Points[0].Pt_Light = (S16)0;
        Tab_Points[1].Pt_Light = (S16)0;
        Tab_Points[2].Pt_Light = (S16)0;
        Tab_Points[3].Pt_Light = (S16)0;

        Tab_Points[0].Pt_ZO = (U16)0;
        Tab_Points[1].Pt_ZO = (U16)0;
        Tab_Points[2].Pt_ZO = (U16)0;
        Tab_Points[3].Pt_ZO = (U16)0;

        Fill_Poly(0, 0, 4, Tab_Points);
    }
}
/*──────────────────────────────────────────────────────────────────────────*/
// How many cubes round the camera's are drawn: 1, the 1997 game's 3 x 3, unless the hook says more. A race draws 2 (RACEMOD.CPP), its straights
// running on past the next cube: the user, 2026-10-30, on Polar Island, the track "still isn't drawing in until the last second". Past the
// first ring only a cube the camera can see: ahead of it, and nearer than the far clip.
S32 (*TerrainReachHook)(void) = NULL;
#define CUBE_SIZE (512 * 64)

extern S32 PtrProjectPoint(S32 xw, S32 yw, S32 zw);

// A cube past the first ring (ox, oy from the camera's), by the camera the land is drawn with: all four corners outside one side of its
// view -- behind it, past the far clip, off the left or the right of the screen -- and it can't be seen. `wide` leaves room for the camera's
// tilt, which leans the sides of the view out, and for hills standing up out of the cube. (From the matrix, not BetaCam's quadrant: that
// took the corner the rows start from for the corner the camera looks at, which is the opposite one, and left out the cubes straight
// ahead: the road only came into view at the first ring, 2026-10-31)
static S32 CubeInView(S32 ox, S32 oy, double cx, double cz, double lookX, double lookZ, double sideX, double sideZ, double wide, double farClip) {
    S32 behind = 0, past = 0, right = 0, left = 0;
    for (S32 c = 0; c < 4; c++) {
        const double dx = (ox + (c & 1)) * (double)CUBE_SIZE - cx;
        const double dz = (oy + (c >> 1)) * (double)CUBE_SIZE - cz;
        const double fore = dx * lookX + dz * lookZ;
        const double side = dx * sideX + dz * sideZ;
        if (fore <= 0) behind++;
        if (fore > farClip) past++;
        if (side > wide * fore) right++;
        if (-side > wide * fore) left++;
    }
    return behind < 4 AND past < 4 AND right < 4 AND left < 4;
}

void DrawHorizon2ZBuf() {
    S32 flag = FALSE;
    S32 cx = CameraX;
    S32 cy = CameraY;
    S32 cz = CameraZ;
    // The camera's own cube's sea squares: a neighbour that fails to load sets CubeBitField to the whole cube (LoadCube), and with no
    // neighbour loaded the own cube is not reloaded below. Kept, or a one-cube island (Island CX) has its sea drawn under all of it,
    // over the squares its info word leaves without and through anything under the sea's height there.
    U16 ownBitField = CubeBitField;
    S32 cxI = CameraXI;
    S32 cyI = CameraYI;
    S32 czI = CameraZI;
    S32 seamsFixed = TerrainSeamsFixed();

    // the corner behind the camera first, row by row, the nearest land first (the four tables of the 1997 game, TerrNO and so on, by the
    // camera's turn)
    const S32 reach = TerrainReachHook ? TerrainReachHook() : 1;
    // (the way the camera looks and its right, along the ground: a point's depth is CameraZr - its rotated z, so the third row is backwards)
    const double lookLen = sqrt((double)MatriceWorld.F.M31 * MatriceWorld.F.M31 + (double)MatriceWorld.F.M33 * MatriceWorld.F.M33);
    const double sideLen = sqrt((double)MatriceWorld.F.M11 * MatriceWorld.F.M11 + (double)MatriceWorld.F.M13 * MatriceWorld.F.M13);
    const double lookX = lookLen > 0 ? -MatriceWorld.F.M31 / lookLen : 0, lookZ = lookLen > 0 ? -MatriceWorld.F.M33 / lookLen : 0;
    const double sideX = sideLen > 0 ? MatriceWorld.F.M11 / sideLen : 0, sideZ = sideLen > 0 ? MatriceWorld.F.M13 / sideLen : 0;
    const double wide = 1.3 * XCentre / (FRatioX > 0 ? FRatioX : 1);
    const S32 stepX = (BetaCam >= 0 AND BetaCam < 2048) ? 1 : -1;
    const S32 stepY = (BetaCam >= 1024 AND BetaCam < 3072) ? 1 : -1;

    for (S32 row = -reach; row <= reach; row++)
    for (S32 col = -reach; col <= reach; col++) {
        const S32 ox = col * stepX, oy = row * stepY;
        S32 x, y;

        x = CurrentCubeX + ox;
        y = CurrentCubeY + oy;

        if (x >= 0 AND x <= 15 AND y >= 0 AND y <= 15) {
            if ((ox > 1 OR ox < -1 OR oy > 1 OR oy < -1) AND !CubeInView(ox, oy, cx, cz, lookX, lookZ, sideX, sideZ, wide, ClipZFar))
                continue;
            if (ox OR oy) {
                // PreClipping du Cube
                /*				if( PtrProjectPoint( 0, 0, 0 )
				OR  PtrProjectPoint( 32767, 0, 0 )
				OR  PtrProjectPoint( 32767, 0, 32767 )
				OR  PtrProjectPoint( 0, 0, 32767 ) )
*/
                {
#ifdef LBA_GAME
                    if (seamsFixed) {
                        // Whole cubes only. The 1997 game adds the cube's x and y offsets
                        // as well, a 1-unit nudge per neighbouring cube (LBA2.DOS
                        // 0x65e2b, on every CPU path); on the integer terrain it opens
                        // seams instead of hiding them. Each camera steps from its own
                        // position, so the integer one stays clear of the float maths.
                        SetPosCamera(cx - ox * CUBE_SIZE, cy, cz - oy * CUBE_SIZE);
                        SetPosCameraI(cxI - ox * CUBE_SIZE, cyI, czI - oy * CUBE_SIZE);
                    } else {
                        SetPosCamera(cx - ox * CUBE_SIZE + ox, cy, cz - oy * CUBE_SIZE + oy);
                    }
#else
                    SetPosCamera(cx - ox * CUBE_SIZE + ox * Reglage, cy, cz - oy * CUBE_SIZE + oy * Reglage);
#endif
                    if (LoadCube(x, y)) {
                        AffichageTerrainZBuf();
                        flag = TRUE;
                    } else if (RendererDrawSea)
                        DrawOneSea(Y_SEA);
                }
            }
        }
    }

    SetPosCamera(cx, cy, cz);
    if (seamsFixed)
        SetPosCameraI(cxI, cyI, czI);

    if (flag) {
        if (!LoadCube(CurrentCubeX, CurrentCubeY)) {
            // on est dans un cube phantom !!!!!!!
            InitSeaCube();
            if (RendererDrawSea)
                DrawOneSea(Y_SEA);
        }
    } else {
        CubeBitField = ownBitField;
    }
}

/*──────────────────────────────────────────────────────────────────────────*/
U32 MaskVisible(U8 *in_ptr) {
    // This code conversion could be improved...

    U8 *firstRow = in_ptr;
    U8 *secondRow = in_ptr + 65;

    U8 maskH = *firstRow;
    U8 maskL = *firstRow;

    maskH |= *secondRow;
    maskL &= *secondRow;

    maskH |= *(firstRow + 1);
    maskL &= *(firstRow + 1);

    maskH |= *(secondRow + 1);
    maskL &= *(secondRow + 1);

    U32 finalMask = 0;
    finalMask |= (maskH << 8);
    finalMask |= maskL;
    finalMask &= 0x20FF;

    return finalMask;
}

/*──────────────────────────────────────────────────────────────────────────*/
// A cell some of whose corners are behind the camera's near plane, drawn clipped against it in the camera's space (ClipperZ, as the
// sea's Draw_Poly), each triangle with DrawFeuillePolyZBuf's own colour and texture. The engine used to paint such a cell black
// from its visible edge down to the bottom of the screen (FillBlackPolyZBuf): right for flat ground passing under a camera close
// over it, whose near part is off the bottom of the screen anyway, but a wall or a terrace beside or below the camera crosses the
// plane too -- the stacked terraces of a race-track mod's mountain lap, the camera following the car a few cells from a cliff --
// and the black then covered a whole side of the view, the road and the car.
static void DrawClippedTriangle(const STRUC_CLIPVERTEX *a, const STRUC_CLIPVERTEX *b, const STRUC_CLIPVERTEX *c, T_HALF_POLY *ptrh) {
    STRUC_CLIPVERTEX src[3], dst[8];
    S32 k, l, color, light;

    if (!HalfPolyShown(ptrh))
        return;
    src[0] = *a;
    src[1] = *b;
    src[2] = *c;
    if (ptrh->TexFlag) {
        const T_HALF_TEX *t = GroundTexDef(ptrh);
        src[0].V_MapU = t->Tx0;
        src[0].V_MapV = t->Ty0;
        src[1].V_MapU = t->Tx1;
        src[1].V_MapV = t->Ty1;
        src[2].V_MapU = t->Tx2;
        src[2].V_MapV = t->Ty2;
    }
    k = (S32)ClipperZ(dst, src, 3, NearClip + 1, 0);
    if (k < 3 OR k > 8)
        return;
    for (l = 0; l < k; l++) {
        if (!LongProjectPoint(dst[l].V_X0 + CameraXr, dst[l].V_Y0 + CameraYr, CameraZr - dst[l].V_Z0))
            return;
        if ((Xp != (S32)(S16)Xp) OR(Yp != (S32)(S16)Yp))
            return;
        Tab_Points[l].Pt_XE = (S16)Xp;
        Tab_Points[l].Pt_YE = (S16)Yp;
        Tab_Points[l].Pt_Light = (S16)dst[l].V_Light;
        Tab_Points[l].Pt_ZO = (U16)GET_ZO(dst[l].V_Z0);
    }
    if (!TestVuePoly(Tab_Points))
        return;
    // (the colour's light is the whole triangle's average, as DrawFeuillePolyZBuf's)
    light = a->V_Light + b->V_Light + c->V_Light;
    color = (ptrh->Bank << 4) + 11;
    if (ptrh->PolyFlag) {
        if (ptrh->PolyFlag == 1)
            color += (light * 21845) >> 24;
        Fill_Poly(ListTypePolygone[ptrh->PolyFlag], color, k, Tab_Points);
    }
    if (ptrh->TexFlag) {
        for (l = 0; l < k; l++) {
            Tab_Points[l].Pt_MapU = dst[l].V_MapU;
            Tab_Points[l].Pt_MapV = dst[l].V_MapV;
        }
        if (ptrh->TexFlag == 2)
            color = (light * 21845) >> 24;
        Fill_Poly((U8)ListTypePTexture[ptrh->TexFlag + (ptrh->PolyFlag << 2)], color, k, Tab_Points);
    }
}

static void DrawFeuillePolyClipped(S32 xi, S32 zi) {
    static const S32 ox[4] = {0, 0, 1, 1}, oz[4] = {0, 1, 1, 0}; // DrawFeuillePolyZBuf's corners 0..3
    T_HALF_POLY *ptrh = &MapPolyGround[zi * (64 * 2) + xi * 2];
    STRUC_CLIPVERTEX c[4];
    S32 k, depth;

    if (!(ptrh->PolyFlag + ptrh->TexFlag + (ptrh + 1)->PolyFlag + (ptrh + 1)->TexFlag))
        return;
    for (k = 0; k < 4; k++) {
        const S32 vx = xi + ox[k], vz = zi + oz[k];
        S32 light = (MapIntensity[vz * 65 + vx] & 15) * 256 - 255;
        LongWorldRotatePoint(vx * 512, MapSommetY[vz * (NB_COTE + 1) + vx], vz * 512);
        c[k].V_X0 = X0 - CameraXr;
        c[k].V_Y0 = Y0 - CameraYr;
        c[k].V_Z0 = CameraZr - Z0;
        c[k].V_MapU = 0;
        c[k].V_MapV = 0;
        c[k].V_Light = (U16)(light < 0 ? 0 : light);
        c[k].V_Dummy = 0;
    }
    // (the fog of its first corner, as DrawFeuillePolyZBuf's, at least at the near plane)
    depth = c[0].V_Z0 > NearClip ? c[0].V_Z0 : NearClip;
    SetCLUT((U16)(PalLevel + 16 * BoundRegleTrois(0, 15, ClipZFar - StartZFog, depth - StartZFog)));
    if (ptrh->Sens == 0) {
        DrawClippedTriangle(&c[0], &c[1], &c[2], ptrh);
        DrawClippedTriangle(&c[2], &c[3], &c[0], ptrh + 1);
    } else {
        DrawClippedTriangle(&c[3], &c[0], &c[1], ptrh);
        DrawClippedTriangle(&c[1], &c[2], &c[3], ptrh + 1);
    }
}

/*──────────────────────────────────────────────────────────────────────────*/
// MaskVisible's for a block of s x s cells: its four outer corners
static inline U32 MaskVisibleStep(const U8 *p, S32 s) {
    const U8 a = p[0], b = p[s], c = p[65 * s], d = p[65 * s + s];
    const U8 maskH = (U8)(a | b | c | d), maskL = (U8)(a & b & c & d);
    return (((U32)maskH << 8) | maskL) & 0x20FF;
}

/*──────────────────────────────────────────────────────────────────────────*/
// The ground's detail by distance (2026-11-18, LBRA's players: "complaints of frame rate being bad on Polar Island especially"). A
// cube's 64 x 64 cells cost the same drawn under the camera or six cubes off, and on an island whose land fills the view to a far clip
// of 200,000 -- Polar Island's thirty-odd land cubes past the first ring -- the ground's triangles were four fifths of the frame. A cube
// is drawn in tiles of 8 x 8 cells, each in blocks of TileStep x TileStep cells (1, 2, 4 or 8): the largest whose block, at the tile's
// nearest point, is no wider than TerrainLodPixels on the screen -- by the projection's own scale, so a larger window keeps more of the
// detail -- and only where its blocks would look as their cells do: every cell of a block the same ground (a road's kerb, a path, a
// shore keep their cells), and its points within TerrainLodPixels / 8 pixels on the screen of the block's flat corners (a cliff, a
// bank, a hump keep their shape). Where a tile meets a coarser one, the finer tile's points along their edge are put on the coarser
// one's straight edge (TerrainVertex), so no crack opens between them; along a cube's side by the steps the distance alone gives, which
// the cube beside works out the same. TerrainLodPixels 0 (console terrain_lod 0): every cell, as the game was.
#define TERRAIN_TILE 8
#define TERRAIN_TILES (NB_COTE / TERRAIN_TILE)
S32 TerrainLodPixels = 32;
static S32 DistSteps[TERRAIN_TILES + 2][TERRAIN_TILES + 2]; // [tz + 1][tx + 1]: the cube's tiles and the ring round it, by distance
static S32 TileSteps[TERRAIN_TILES + 2][TERRAIN_TILES + 2]; // ... and the steps drawn (the cube's own tiles: as far as they fit)
static U32 VertexStamp[(NB_COTE + 1) * (NB_COTE + 1)];     // a point worked out for this cube already (== VertexFrame)
static U32 VertexFrame = 0;

// Each tile of each cube of the island, worked out once: Look bit k, its blocks of 2 << k cells each one ground; Bend[k], the most a
// point of such a block is off its corners' surface (height units)
typedef struct {
    U8 Made;
    U8 Look;
    U16 Bend[3];
} T_TILE_FIT;
static T_TILE_FIT TileFit[16 * 16][TERRAIN_TILES * TERRAIN_TILES];
static U32 TileFitIsland = 0;
extern U32 IslandLoads;                // LOADISLE.CPP LoadIsland
extern S32 DecorCubeX, DecorCubeZ;     // the cube LoadCube loaded last (DECORS.CPP)

// (a half cell's look: all but the step's sample and the collision bit; the way the cell is cut in two left out too -- a block is cut
// as its first cell is)
static inline U32 HalfLook(const T_HALF_POLY *h) {
    U32 v;
    memcpy(&v, h, sizeof(v));
    return v & 0xFFFCF0FFu;
}

static const T_TILE_FIT *TileFitOf(S32 tx, S32 tz) {
    if (TileFitIsland != IslandLoads) {
        memset(TileFit, 0, sizeof(TileFit));
        TileFitIsland = IslandLoads;
    }
    const S32 cube = (DecorCubeZ & 15) * 16 + (DecorCubeX & 15);
    T_TILE_FIT *fit = &TileFit[cube][tz * TERRAIN_TILES + tx];
    if (fit->Made)
        return fit;
    fit->Made = 1;
    for (S32 k = 0; k < 3; k++) {
        const S32 s = 2 << k;
        S32 same = 1, bend = 0;
        for (S32 bz = tz * TERRAIN_TILE; bz < (tz + 1) * TERRAIN_TILE; bz += s) {
            for (S32 bx = tx * TERRAIN_TILE; bx < (tx + 1) * TERRAIN_TILE; bx += s) {
                const T_HALF_POLY *first = &MapPolyGround[bz * (64 * 2) + bx * 2];
                const U32 l0 = HalfLook(first), l1 = HalfLook(first + 1);
                const S32 h00 = MapSommetY[bz * (NB_COTE + 1) + bx], h10 = MapSommetY[bz * (NB_COTE + 1) + bx + s];
                const S32 h01 = MapSommetY[(bz + s) * (NB_COTE + 1) + bx], h11 = MapSommetY[(bz + s) * (NB_COTE + 1) + bx + s];
                for (S32 j = 0; j <= s; j++) {
                    for (S32 i = 0; i <= s; i++) {
                        if (i < s AND j < s) {
                            const T_HALF_POLY *h = &MapPolyGround[(bz + j) * (64 * 2) + (bx + i) * 2];
                            if (HalfLook(h) != l0 OR HalfLook(h + 1) != l1)
                                same = 0;
                        }
                        const double u = (double)i / s, v = (double)j / s;
                        const double flat = h00 * (1 - u) * (1 - v) + h10 * u * (1 - v) + h01 * (1 - u) * v + h11 * u * v;
                        const S32 off = (S32)fabs(MapSommetY[(bz + j) * (NB_COTE + 1) + bx + i] - flat);
                        if (off > bend)
                            bend = off;
                    }
                }
            }
        }
        if (same)
            fit->Look |= (U8)(1 << k);
        fit->Bend[k] = (U16)(bend > 65535 ? 65535 : bend);
    }
    return fit;
}

// The step of tile (tx, tz) of the cube being drawn by its distance alone -- any integers: one past its side is the neighbour cube's
// tile, the same answer as that cube's own (the camera, CameraX/CameraZ, is where it is from the cube being drawn: DrawHorizon2ZBuf's
// SetPosCamera). *dist: the tile's nearest point's distance.
static S32 TileStepByDistance(S32 tx, S32 tz, double *dist) {
    *dist = 0;
    if (TerrainLodPixels <= 0 OR FRatioX <= 0)
        return 1;
    const double size = TERRAIN_TILE * 512.0;
    const double x0 = tx * size, z0 = tz * size, cx = CameraX, cz = CameraZ;
    const double dx = cx < x0 ? x0 - cx : (cx > x0 + size ? cx - x0 - size : 0);
    const double dz = cz < z0 ? z0 - cz : (cz > z0 + size ? cz - z0 - size : 0);
    const double d = sqrt(dx * dx + dz * dz);
    *dist = d;
    if (d <= 0)
        return 1;
    const double cell = 512.0 * FRatioX / d; // a cell's width on the screen there, in pixels
    S32 step = 1;
    while (step < TERRAIN_TILE AND cell * step * 2 <= TerrainLodPixels)
        step *= 2;
    return step;
}

// ... and as drawn, a tile of the cube's own: no coarser than its blocks still look as their cells do
static S32 TileStepFitted(S32 tx, S32 tz, S32 step, double d) {
    if (step <= 1)
        return 1;
    const T_TILE_FIT *fit = TileFitOf(tx, tz);
    const double most = TerrainLodPixels / 8.0; // pixels a point may move on the screen
    while (step > 1) {
        const S32 k = step == 2 ? 0 : (step == 4 ? 1 : 2);
        if ((fit->Look & (1 << k)) AND fit->Bend[k] * FRatioX / d <= most)
            break;
        step >>= 1;
    }
    return step;
}

// A point of the cube's ground, rotated and projected into SommetRot / SommetAffichable (once a cube). On the edge between two tiles
// whose coarser step it is not on, it is put on that tile's straight edge: its height the line's between the coarser step's points.
static void TerrainVertex(S32 vx, S32 vz) {
    const S32 at = vz * (NB_COTE + 1) + vx;
    if (VertexStamp[at] == VertexFrame)
        return;
    VertexStamp[at] = VertexFrame;

    S32 y = MapSommetY[at];
    const S32 onX = (vx % TERRAIN_TILE) == 0, onZ = (vz % TERRAIN_TILE) == 0;
    if (onX != onZ) {
        // (on a tile's side, not at its corner: the coarser of the two tiles' steps along it -- on the cube's own side, the steps by
        // distance, which the cube beside works out the same)
        if (onX) {
            const S32 row = vz / TERRAIN_TILE, col = vx / TERRAIN_TILE;
            S32(*steps)[TERRAIN_TILES + 2] = (col == 0 OR col == TERRAIN_TILES) ? DistSteps : TileSteps;
            const S32 a = steps[row + 1][col], b = steps[row + 1][col + 1];
            const S32 step = a > b ? a : b;
            const S32 off = vz % step;
            if (off) {
                const S32 y0 = MapSommetY[(vz - off) * (NB_COTE + 1) + vx], y1 = MapSommetY[(vz - off + step) * (NB_COTE + 1) + vx];
                y = y0 + (y1 - y0) * off / step;
            }
        } else {
            const S32 row = vz / TERRAIN_TILE, col = vx / TERRAIN_TILE;
            S32(*steps)[TERRAIN_TILES + 2] = (row == 0 OR row == TERRAIN_TILES) ? DistSteps : TileSteps;
            const S32 a = steps[row][col + 1], b = steps[row + 1][col + 1];
            const S32 step = a > b ? a : b;
            const S32 off = vx % step;
            if (off) {
                const S32 y0 = MapSommetY[vz * (NB_COTE + 1) + vx - off], y1 = MapSommetY[vz * (NB_COTE + 1) + vx - off + step];
                y = y0 + (y1 - y0) * off / step;
            }
        }
    }

    U8 *ptra = &SommetAffichable[0][0] + at;
    T_SOMMET_ROT *ptrs = &SommetRot[0][0] + at;
    S32 zr;

    TerrPath->WorldRotate(vx * 512, y, vz * 512);

    zr = *TerrPath->Zr - Z0;

    // cone clipping

    if (zr <= NearClip) {
        *ptra = 32;
    } else if ((zr < ClipZFar)
                   AND TerrPath->Project(X0, Y0, Z0)) {
        U8 flag;

        flag = 16;

        if (Xp < ClipXMin)
            flag |= 1;
        if (Xp > ClipXMax)
            flag |= 2;
        if (Yp < ClipYMin)
            flag |= 4;
        if (Yp > ClipYMax)
            flag |= 8;

        *ptra = flag;

        ptrs->Zrot = zr;

        ptrs->X2D = (S16)Xp;
        ptrs->Y2D = (S16)Yp;

    } else {
        *ptra = 0;
    }
}

/*──────────────────────────────────────────────────────────────────────────*/
void AffichageTerrainZBuf(void) {
    S16 *ptrm;
    U8 *ptra;
    T_SOMMET_ROT *ptrs;

    // affichage des objets du decors
    AffichageObjetDecorsZBuf();

    // calcul les coordonnées 2D de chaque points
    // (the cube in tiles of 8 x 8 cells, each drawn in blocks of its own step's cells and only its blocks' corners worked out:
    // TerrainLodPixels)
    PickTerrainPath();
    for (S32 tz = -1; tz <= TERRAIN_TILES; tz++) {
        for (S32 tx = -1; tx <= TERRAIN_TILES; tx++) {
            double d;
            const S32 step = TileStepByDistance(tx, tz, &d);
            const S32 own = tx >= 0 AND tx < TERRAIN_TILES AND tz >= 0 AND tz < TERRAIN_TILES;
            DistSteps[tz + 1][tx + 1] = step;
            TileSteps[tz + 1][tx + 1] = own ? TileStepFitted(tx, tz, step, d) : step;
        }
    }
    if (++VertexFrame == 0) {
        memset(VertexStamp, 0, sizeof(VertexStamp));
        VertexFrame = 1;
    }
    for (S32 tz = 0; tz < TERRAIN_TILES; tz++) {
        for (S32 tx = 0; tx < TERRAIN_TILES; tx++) {
            const S32 s = TileSteps[tz + 1][tx + 1];
            for (S32 vz = tz * TERRAIN_TILE; vz <= (tz + 1) * TERRAIN_TILE; vz += s)
                for (S32 vx = tx * TERRAIN_TILE; vx <= (tx + 1) * TERRAIN_TILE; vx += s)
                    TerrainVertex(vx, vz);
        }
    }

    PtrMap = GroundTexture;
    RepMask = 0xFFFF;

    for (S32 tz = 0; tz < TERRAIN_TILES; tz++) {
        for (S32 tx = 0; tx < TERRAIN_TILES; tx++) {
            const S32 s = TileSteps[tz + 1][tx + 1];
            for (S32 z = tz * TERRAIN_TILE; z < (tz + 1) * TERRAIN_TILE; z += s) {
                for (S32 x = tx * TERRAIN_TILE; x < (tx + 1) * TERRAIN_TILE; x += s) {
                    const S32 at = z * (NB_COTE + 1) + x;
                    S32 mask;

                    ptra = &SommetAffichable[0][0] + at;
                    ptrs = &SommetRot[0][0] + at;

                    mask = s == 1 ? MaskVisible(ptra) : MaskVisibleStep(ptra, s);

                    switch (mask & 0xFF) {
                    case 32:
                        break;
                    case 16:
                        DrawFeuillePolyZBuf(x, z, ptrs, s);
                        break;
                    case 0:
                        // some corners in front of the camera, some behind it or inside its near plane: drawn clipped against the plane
                        // (it was painted black from its visible edge to the bottom of the screen: DrawFeuillePolyClipped) -- a block's
                        // cells one by one, each clipped on its own
                        if (mask) {
                            for (S32 dz = 0; dz < s; dz++)
                                for (S32 dx = 0; dx < s; dx++)
                                    DrawFeuillePolyClipped(x + dx, z + dz);
                        }
                        break;
                    }
                }
            }
        }
    }

    if (RendererDrawSea)
        DrawOneSea(Y_SEA);

#ifdef LBA_EDITOR
    if (FlagInfos & INFO_DECORS) {
        AffichageObjetDecorsZV();
    }
#endif

    BoxStaticAdd(0, 0, ModeDesiredX - 1, ModeDesiredY - 1);
}

/*

void	AffichageTerrainZBuf( void )
{
	S16		*ptrm	;
	U8		*ptra	;
	T_SOMMET_ROT	*ptrs	;

	// affichage des objets du decors
	AffichageObjetDecorsZBuf() ;

	// calcul les coordonnées 2D de chaque points
	ptra = &SommetAffichable[0][0];
	ptrm = MapSommetY ;
	ptrs = &SommetRot[0][0] ;

	for (S32 z=0; z<=NB_COTE*512; z+=512)
	{
		for (S32 x=0; x<=NB_COTE*512; x+=512)
		{
			S32	zr ;

			LongWorldRotatePoint(x, *ptrm, z);

			zr = CameraZr - Z0 ;// l'ecran

			// cone clipping

			if( (zr<ClipZFar)
			AND (zr>NearClip)
			AND LongProjectPoint( X0,Y0,Z0 ) )
			{
				U8	flag	;

				flag = 16	;

				if( Xp < ClipXMin )	flag |= 1 ;
				if( Xp > ClipXMax )	flag |= 2 ;
				if( Yp < ClipYMin )	flag |= 4 ;
				if( Yp > ClipYMax )	flag |= 8 ;

				*ptra		= flag 	;

				ptrs->Zrot 	= zr;

				// Add 0.5 bias before truncation to reduce edge artifacts from coordinate precision loss
				ptrs->X2D	= (S16)(Xp + 0.5);
				ptrs->Y2D	= (S16)(Yp + 0.5);

			}
			else
			{
				*ptra = 0	;
			}

			ptra++;
			ptrm++;
			ptrs++;
		}
	}

	PtrMap = GroundTexture ;
	RepMask = 0xFFFF ;

	ptra  = &SommetAffichable[0][0] ;
	ptrs  = &SommetRot[0][0] ;

	for(z=0; z<NB_COTE; z++)
	{
		for(S32 x=0; x<NB_COTE; x++)
		{
			if (MaskVisible(ptra) == 16)
			{
				DrawFeuillePolyZBuf( x, z, ptrs) ;
			}

			ptra++	;
			ptrs++	;
		}

		ptra++	;
		ptrs++	;
	}

	BoxStaticAdd( 0, 0, ModeDesiredX-1, ModeDesiredY-1 ) ;
}

*/
/*──────────────────────────────────────────────────────────────────────────*/
//****************************************************************************

void AffichageTerrainRapide(void) {
    PickTerrainPath();
    for (S32 xi = 0; xi <= NB_COTE; xi++) {
        for (S32 zi = 0; zi <= NB_COTE; zi++) {
            TerrPath->WorldRotate(xi * 512,
                                  (S32)MapSommetY[zi * 65 + xi],
                                  zi * 512);

            if (TerrPath->Project(X0, Y0, Z0)) {
                Plot(Xp, Yp, WHITE);
            }
        }
    }
}

/*──────────────────────────────────────────────────────────────────────────*/
/*──────────────────────────────────────────────────────────────────────────*/

void CopyBlock256(U8 *sour, U8 *dest) {
    S32 y;

    for (y = 0; y < 32; y++) {
        memcpy(dest, sour, 32);
        dest += 256;
        sour += 256;
    }
}

/*──────────────────────────────────────────────────────────────────────────*/
S32 RefreshAniPoly = TRUE;
S32 NbToRefresh = 0;

/*──────────────────────────────────────────────────────────────────────────*/
void ClearAniPoly(void) {
    NbAniPoly = 0;
    PtrAniPoly = PtrPolySea;
    RefreshAniPoly = NbAniPoly;
    NbToRefresh = NbAniPoly / 4;
}

/*──────────────────────────────────────────────────────────────────────────*/
void DoAnimatedPolys() {
    if (!NbAniPoly OR !MaxPolySea)
        return;

    if (TimerRefHR > TimerAniPoly) {
        CntAniPoly8++;
        CntAniPoly8 &= 7; // 0-8
        CntAniPoly6++;
        CntAniPoly6 %= 6; // 0-5

        TimerAniPoly = TimerRefHR + 1000 / 4; // voir pour nb frame/sec

        if (OffsetAniPoly1 != -1) {
            CopyBlock256(SkySeaTexture + 128 * 256 + CntAniPoly8 * 32,
                         GroundTexture + OffsetAniPoly1);
        }

        if (OffsetAniPoly2 != -1) {
            CopyBlock256(SkySeaTexture + (128 + 32) * 256 + CntAniPoly8 * 32,
                         GroundTexture + OffsetAniPoly2);
        }

        if (OffsetAniPoly3 != -1) {
            CopyBlock256(SkySeaTexture + (128 + 32 + 32) * 256 + 32 * 2 + CntAniPoly6 * 32,
                         GroundTexture + OffsetAniPoly3);
        }

        if (OffsetAniPoly4 != -1) {
            CopyBlock256(SkySeaTexture + (128 + 32 + 32 + 32) * 256 + 32 * 2 + CntAniPoly6 * 32,
                         GroundTexture + OffsetAniPoly4);
        }

        RefreshAniPoly = NbAniPoly;
        NbToRefresh = NbAniPoly / 4;
    }
}

/*──────────────────────────────────────────────────────────────────────────*/

void DrawAnimatedPolys() {
    S32 n;
    S_ANI_POLY *ptr;

    if (!NbAniPoly OR !MaxPolySea OR !RefreshAniPoly)
        return;

    PtrMap = GroundTexture;
    RepMask = 0xFFFF;

    Switch_Fillers(FILL_POLY_FOG_NZW);

    if (NbToRefresh > RefreshAniPoly)
        NbToRefresh = RefreshAniPoly;

    ptr = PtrAniPoly = (S_ANI_POLY *)&PtrPolySea[RefreshAniPoly - 1];
    for (n = 0; n < NbToRefresh; n++, ptr--) {
        UnsetClip();

        SetClip(ptr->X0, ptr->Y0, ptr->X1, ptr->Y1);

        if (ClipXMin <= ClipXMax
                            AND ClipYMin <= ClipYMax) {
            SetCLUT(ptr->Fog);

            Fill_Poly(ptr->Type, ptr->Color, 3, ptr->Tab_Points);

            BoxStaticAdd(ClipXMin, ClipYMin, ClipXMax, ClipYMax);
        }

        RefreshAniPoly--;
    }

    // protec au cas ou
    if (RefreshAniPoly < 0)
        RefreshAniPoly = 0;

    // Met a jour Screen pour recouvrements
    BoxBlitStaticListToScreen();

    Switch_Fillers(FILL_POLY_TEXTURES);
    SetCLUT(PalLevel);

    UnsetClip();
}

/*──────────────────────────────────────────────────────────────────────────*/

void FiltreAnimatedPolys() {
    S32 n;
    S_ANI_POLY *ptr;

    if (!NbAniPoly OR !MaxPolySea)
        return;

    ptr = PtrAniPoly = (S_ANI_POLY *)PtrPolySea;

    for (n = 0; n < NbAniPoly;) {
        Fill_Poly(POLY_FLAG_ZBUFFER, 0, 3, ptr->Tab_Points);

        if (IsPolygonHidden) {
            if (NbAniPoly > 1) {
                memcpy((char *)ptr, (char *)&PtrPolySea[NbAniPoly - 1], sizeof(S_ANI_POLY));
            }
            NbAniPoly--;
        } else {
            n++;
            ptr++;
        }
    }
}
