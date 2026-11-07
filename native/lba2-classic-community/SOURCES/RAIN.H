#ifndef RAIN_H
#define RAIN_H

typedef struct
{
    S32 XRain;
    S32 YRain;
    S32 ZRain;
    S32 Timer;
} T_RAIN;

/*--------------------------------------------------------------------------*/
extern void InitOneRain(T_RAIN *pt);
/*--------------------------------------------------------------------------*/
extern void InitRain(void);
/*--------------------------------------------------------------------------*/
extern void GereRain(void);
/*--------------------------------------------------------------------------*/
extern void ClearImpactRain(void);
/*--------------------------------------------------------------------------*/
extern void AffRain(void);
/*--------------------------------------------------------------------------*/
// What the rain or the snow drew, to be cleaned next frame: its box kept inside the clip window (RAIN.CPP)
extern void WeatherBoxMovingAdd(S32 x0, S32 y0, S32 x1, S32 y1);
/*--------------------------------------------------------------------------*/

// Falling snow, on an island whose map says so (SNOW.CPP): whether it falls here (and its flakes set going when it starts to), and they
// moved and drawn as the rain's are
extern S32 SnowFalls(void);
extern void InitSnow(void);
extern void GereSnow(void);
extern void AffSnow(void);
/*--------------------------------------------------------------------------*/

extern S32 OldRealTime;
extern S32 DeltaRain;
extern U32 RealTime;
extern S32 LastTimer;
extern S32 KeepImpactRain;

#endif // RAIN_H
