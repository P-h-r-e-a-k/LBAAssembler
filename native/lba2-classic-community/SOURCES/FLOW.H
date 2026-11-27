/*══════════════════════════════════════════════════════════════════════════*
             █▀▀▀█ █▀▀▀█ █▀▀▀█ ▀▀█▀▀  █    █▀▀▀▀ █     █▀▀▀▀ ██▀▀▀
             ██▀▀▀ ██▀▀█ ██▀█▀   ██   ██   ██    ██    ██▀▀  ▀▀▀▀█
	     ▀▀    ▀▀  ▀ ▀▀  ▀   ▀▀   ▀▀   ▀▀▀▀▀ ▀▀▀▀▀ ▀▀▀▀▀ ▀▀▀▀▀
 *══════════════════════════════════════════════════════════════════════════*/
/*──────────────────────────────────────────────────────────────────────────*/

#ifndef FLOW_H
#define FLOW_H

#include <SYSTEM/ADELINE_TYPES.H>

typedef struct
{
    S16 Alpha;
    S16 Beta;
    S16 OuvertureAlpha;
    S16 OuvertureBeta;
    S16 XMin;
    S16 YMin;
    S16 ZMin;
    S16 XMax;
    S16 YMax;
    S16 ZMax;

    S16 Delay;
    S16 Speed;
    S16 Weight;
    S16 NbDot;

    S16 Bank;
    U8 Coul;
    U8 Range;
    U32 Flags;
} T_FLOW; // Sert uniquement au chargement des flows

typedef struct {
    S32 X;
    S32 Y;
    S32 Z;
    S32 Vx;
    S32 Vy;
    S32 Vz;
    S32 Delay;
    S32 Poids;
    S32 Couleur;
    S32 Mode;
} S_ONE_DOT;

typedef struct {
    S32 Flag;
    S32 Owner;
    S32 NumPoint;
    S32 NbDot;
    S32 OrgX;
    S32 OrgY;
    S32 OrgZ;
    S32 XMin;
    S32 YMin;
    S32 ZMin;
    S32 XMax;
    S32 YMax;
    S32 ZMax;
    U32 FlowTimerStart;
    S_ONE_DOT *PtrListDot;
} S_PART_FLOW;

/*──────────────────────────────────────────────────────────────────────────*/

#ifdef LBA_EDITOR
#define PATH_FLOW "f:\\projet\\lba2\\datas\\flow\\"
#endif

#define FLOW_WAIT_COOR 1
#define FLOW_EXTRA_SPRITE 2
#define FLOW_EXTRA_OBJ 4
#define FLOW_EXTRA_POF 8

#define MAX_FLOWS 10
#define MAX_FLOW_DOTS 100
#define MAX_FLOW_EXTRA_DOTS 10

extern S_PART_FLOW ListPartFlow[MAX_FLOWS];
extern S_ONE_DOT *ListFlowDots;

#ifndef LBA_EDITOR
//extern T_HQR_HEADER	*HQR_Flows ;
//extern	U32	FlowMem ;
#endif

extern T_FLOW *TabPartFlow;

/*──────────────────────────────────────────────────────────────────────────*/

/*--------------------------------------------------------------------------*/
extern S_PART_FLOW *GetFirstFlowDispo(void);
/*--------------------------------------------------------------------------*/
extern void RazListPartFlow();
/*--------------------------------------------------------------------------*/
extern U32 InitPartFlow();
/*--------------------------------------------------------------------------*/
extern void FreePartFlow();
/*--------------------------------------------------------------------------*/
extern U32 CreateParticleFlow(S32 flag, S32 owner, S32 num_point,
                              S32 orgx, S32 orgy, S32 orgz,
                              S32 beta,
                              S32 num_flow);

/*--------------------------------------------------------------------------*/
extern U32 CreateExtraParticleFlow(S32 type, S32 owner, S32 num, S32 num2,
                                   S32 orgx, S32 orgy, S32 orgz,
                                   S32 beta, S32 num_flow, S32 hitforce,
                                   S32 scale, S32 transparent, S32 tempo);

/*--------------------------------------------------------------------------*/
extern S32 AffParticleFlow(S_PART_FLOW *ptrf);

/*--------------------------------------------------------------------------*/
extern S32 AnimParticleFlow(S_PART_FLOW *ptrf);

/*--------------------------------------------------------------------------*/
extern void AnimAllFlow();
/*--------------------------------------------------------------------------*/
extern void AfficheAllFlow();
/*--------------------------------------------------------------------------*/

//-------------	FLOW_A.ASM

#ifdef __cplusplus
extern "C" {
#endif

/*──────────────────────────────────────────────────────────────────────────*/
extern void BoxFlow(S32 x, S32 y, S32 coul);

#pragma aux BoxFlow "*" parm caller[eax][ebx][ecx]

/*──────────────────────────────────────────────────────────────────────────*/
extern void ShadeBoxBlk(S32 X0, S32 Y0, S32 X1, S32 Y1, S32 deccoul);

#pragma aux ShadeBoxBlk "*" parm caller[] modify[eax ecx edx]

/* GpuScene interior dual path: darken non-zero Log overlay only (see FLOW_A.CPP). */
extern void ShadeBoxBlkOverlay(S32 X0, S32 Y0, S32 X1, S32 Y1, S32 deccoul);
/* The HUD over the GPU renderer's frame (FLOW_A.CPP): */
extern S32 GpuSceneDrawing;                 // AffScene drawing a frame
extern void GpuOverlay_Plate(void);         // what is shown made Log's own, Log opaque till the next scene frame
extern void GpuOverlay_Unplate(void);
extern void GpuOverlay_KeepPlate(S32 x0, S32 y0, S32 x1, S32 y1); // that part of it the page's own, kept by the next Unplate
extern void GpuOverlay_SceneFrame(S32 speaking);                // a scene frame begins: past a dialogue, its kept window let go
extern S32 GpuOverlay_ScreenIndexed(U8 *dst, S32 asShown);                   // the picture on the screen as palette indices (GPU renderer)
extern U8 GpuOverlay_Black(void);           // a black other than index 0
extern U8 GpuOverlay_Ink(U8 colour);         // a 2D colour drawn over the play's 3D view: index 0, see-through there, as that black
extern S32 GpuOverlay_LiveScene(void);
extern void GpuOverlay_ShadeBegin(S32 deccoul);
extern void GpuOverlay_ShadeRun(S32 x0, S32 y, S32 x1);
extern void GpuOverlay_ShadeEnd(void);

/*──────────────────────────────────────────────────────────────────────────*/
extern void CopyBlockShade(S32 x0, S32 y0, S32 x1, S32 y1, U8 *src,
                           S32 xd, S32 yd, U8 *dst, S32 shade);

#pragma aux CopyBlockShade "*" parm caller[edx][ecx][ebx][eax][esi] modify[edi]

/*──────────────────────────────────────────────────────────────────────────*/
#ifdef __cplusplus
}
#endif

#endif // FLOW_H
/*──────────────────────────────────────────────────────────────────────────*/
