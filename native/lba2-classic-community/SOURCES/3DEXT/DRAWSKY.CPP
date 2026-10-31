
// A world point through the terrain's camera path, as a clip vertex.
static void PathVertex(STRUC_CLIPVERTEX *v, S32 x, S32 y, S32 z) {
    TerrPath->WorldRotate(x, y, z);
    v->V_X0 = X0 - *TerrPath->Xr;
    v->V_Y0 = Y0 - *TerrPath->Yr;
    v->V_Z0 = *TerrPath->Zr - Z0;
}

S32 Draw_Poly(STRUC_CLIPVERTEX src[], S32 type) {
    S32 k, l;
    STRUC_CLIPVERTEX dst[10];
    STRUC_CLIPVERTEX dst1[10];

    k = ClipperZ(dst1, src, 4, NearClip + 1, 0);

    if (k) {
        k = ClipperZ(dst, dst1, k, Fill_Z_Fog_Far, -1);
        if (k) {
            for (l = 0; l != k; l++) {
                if (!TerrPath->Project(dst[l].V_X0 + *TerrPath->Xr, dst[l].V_Y0 + *TerrPath->Yr,
                                       *TerrPath->Zr - dst[l].V_Z0))
                    return FALSE;
                if ((Xp != (S32)(S16)Xp) OR(Yp != (S32)(S16)Yp))
                    return FALSE;
                Tab_Points[l].Pt_XE = (S16)Xp;
                Tab_Points[l].Pt_YE = (S16)Yp;
                Tab_Points[l].Pt_Light = dst[l].V_Light;
                Tab_Points[l].Pt_MapU = dst[l].V_MapU;
                Tab_Points[l].Pt_MapV = dst[l].V_MapV;
                Tab_Points[l].Pt_W = INV64(dst[l].V_Z0);
                Tab_Points[l].Pt_ZO = GET_ZO(dst[l].V_Z0);
            }

            if (Fill_Poly(type, 0, k, Tab_Points))
                return TRUE;
        }
    }
    return FALSE;
}

/*──────────────────────────────────────────────────────────────────────────*/

extern S32 (*TerrainReachHook)(void);
#define SKY_SEES_FAR 100000                // a far clip past this ...
#define SKY_FOG_START 14500                // ... and the sky's fade from here: the game's own, Desert Island's and Citadel Island's

void DrawSky(S32 y) {
    PickTerrainPath();

    U32 xx, zz, i, j;
    STRUC_CLIPVERTEX src[10];

    PtrMap = SkySeaTexture + 128;
    RepMask = 0x7F7F;
    PtrCLUTGouraud = PtrCLUTFog;
    //	SetCLUT( PalLevel ) ;

    src[0].V_MapU = 0;
    src[0].V_MapV = 0;
    src[1].V_MapU = 0;
    src[1].V_MapV = 127 * 256 + 255;
    src[2].V_MapU = 127 * 256 + 255;
    src[2].V_MapV = 127 * 256 + 255;
    src[3].V_MapU = 127 * 256 + 255;
    src[3].V_MapV = 0;

    // (out as far as the land is drawn: three cubes across, as the game has it, stopped in plain sight past the haze of a race that sees further -- a band of cloud
    // standing in the sky, Desert Island's start, 2026-10-31. And a sky's fade from where the game's would start: seen so far off, the plane's last stretch is
    // squeezed up against the horizon, and the land's fade there is a few pixels, a hard edge. Only the fog's start: the far clip is the z-buffer's scale, which
    // the land drawn before the sky is held to)
    const S32 reach = TerrainReachHook ? TerrainReachHook() : 1;
    const U32 across = NB_POLYS_COTE * (2 * reach + 1);
    const S32 fadeSky = ClipZFar > SKY_SEES_FAR;
    if (fadeSky) SetFog(SKY_FOG_START, ClipZFar);

    zz = -WORLD_SIZE * reach;
    for (i = 0; i < across; i++, zz += WORLD_SIZE / NB_POLYS_COTE) {
        xx = -WORLD_SIZE * reach;
        for (j = 0; j < across; j++, xx += WORLD_SIZE / NB_POLYS_COTE) {
            PathVertex(&src[0], xx, y, zz);
            PathVertex(&src[1], xx + WORLD_SIZE / NB_POLYS_COTE, y, zz);
            PathVertex(&src[2], xx + WORLD_SIZE / NB_POLYS_COTE, y, zz + WORLD_SIZE / NB_POLYS_COTE);
            PathVertex(&src[3], xx, y, zz + WORLD_SIZE / NB_POLYS_COTE);
            Draw_Poly(&src[0], 24);
        }
    }
    if (fadeSky) SetFog(StartZFog, ClipZFar);
}

void DrawOneSea(S32 y) // Si on souhaite que l'eau ne soit pas au niveau 0 :)
{
    PickTerrainPath();

    STRUC_CLIPVERTEX src[10];
    U32 i, j, xx, zz;
    S32 b;

    PtrMap = SkySeaTexture;
    RepMask = 0x7F7F;
    PtrCLUTGouraud = PtrCLUTFog;
    SetCLUT(PalLevel);

    b = 0;

    src[0].V_MapU = 0;               // xt0
    src[0].V_MapV = 0;               // yt0
    src[1].V_MapU = 0;               // xt1
    src[1].V_MapV = 127 * 256 + 255; // yt1
    src[2].V_MapU = 127 * 256 + 255; // xt2
    src[2].V_MapV = 127 * 256 + 255; // yt2
    src[3].V_MapU = 127 * 256 + 255; // xt3
    src[3].V_MapV = 0;               // yt3

    zz = 0;
    for (i = 0; i < NB_POLYS_COTE; i++, zz += WORLD_SIZE / NB_POLYS_COTE) {
        xx = 0;
        for (j = 0; j < NB_POLYS_COTE; j++, xx += WORLD_SIZE / NB_POLYS_COTE) {
            if (CubeBitField & (1 << b)) {
                PathVertex(&src[0], xx, y, zz);
                PathVertex(&src[1], xx, y, zz + WORLD_SIZE / NB_POLYS_COTE);
                PathVertex(&src[2], xx + WORLD_SIZE / NB_POLYS_COTE, y, zz + WORLD_SIZE / NB_POLYS_COTE);
                PathVertex(&src[3], xx + WORLD_SIZE / NB_POLYS_COTE, y, zz);
                Draw_Poly(src, 24);
            } else {
                // poly noir ?
                /* on verra		LongWorldRotatePoint( xx, y, zz ) ;
			src[0].V_X0    = X0-CameraXr ;
			src[0].V_Y0    = Y0-CameraYr ;
			src[0].V_Z0    = CameraZr-Z0 ;

			LongWorldRotatePoint( xx, y, zz+WORLD_SIZE/NB_POLYS_COTE ) ;
			src[1].V_X0    = X0-CameraXr ;
			src[1].V_Y0    = Y0-CameraYr ;
			src[1].V_Z0    = CameraZr-Z0 ;

			LongWorldRotatePoint( xx+WORLD_SIZE/NB_POLYS_COTE, y, zz+WORLD_SIZE/NB_POLYS_COTE ) ;
			src[2].V_X0    = X0-CameraXr ;
			src[2].V_Y0    = Y0-CameraYr ;
			src[2].V_Z0    = CameraZr-Z0 ;

			LongWorldRotatePoint( xx+WORLD_SIZE/NB_POLYS_COTE, y, zz ) ;
			src[3].V_X0    = X0-CameraXr ;
			src[3].V_Y0    = Y0-CameraYr ;
			src[3].V_Z0    = CameraZr-Z0 ;

			Draw_Poly(src,0) ;
*/
            }
            b++;
        }
    }
}
