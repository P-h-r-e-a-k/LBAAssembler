#ifndef RACEMOD_H
#define RACEMOD_H

#include <SYSTEM/ADELINE_TYPES.H>

// The race-track options of the engine (RACEMOD.CPP). All off unless the level editor names a car setup file in LBA2_RACETRACK_FILE.

#define RACE_MAX_GEARS 8

typedef struct {
    S32 Gears;                    // 1..RACE_MAX_GEARS
    S32 GearTop[RACE_MAX_GEARS];  // top speed of each gear, world units per second
    S32 Accel;                    // speed gained per ms of throttle, in thousandths (4000 = the original car's 4), for a gear whose top speed is RACE_REF_TOP
    S32 Brake;                    // speed lost per ms of braking, thousandths
    S32 Coast;                    // speed lost per ms with neither pedal (and above the gear's top speed), thousandths
    S32 Reverse;                  // top speed backwards
    S32 Steer;                    // turn per second, 4096ths of a turn
    S32 Automatic;                // the gears change by themselves
    S32 Hud;                      // gear, speed and lap times on screen
    S32 HasLine;                  // a start line to count laps at
    S32 LineCubeX, LineCubeZ;     // the island cube it is in
    S32 LineX0, LineZ0, LineX1, LineZ1; // its ends, cube-local world units
    S32 LineDirX, LineDirZ;       // the way a lap crosses it (any length)
} T_RACE_CAR;

// The top speed the acceleration is given for: the original buggy's (BUGGY.CPP MAX_SPEED).
#define RACE_REF_TOP 3800

// TRUE when the engine was started for a race-track mod.
S32 RaceMod_Active(void);
const T_RACE_CAR *RaceMod_Car(void);

// The current gear (1..Gears), and gear changes: X up, Z down (edges of the keys), or by themselves.
S32 RaceMod_Gear(void);
void RaceMod_ResetGear(void);
void RaceMod_UpdateGear(S32 speed);

// The laps: called every frame the hero drives, with his position (cube-local); a lap is counted when the move since the last call
// crosses the start line the right way.
void RaceMod_Lap(S32 x, S32 z);

// Every frame before the scene is drawn: the opponent moves along its line and its car is put there, in the player's scene.
void RaceMod_Frame(void);

// The display (gear, speed, lap times, checkpoints, race position), drawn over the scene while the hero drives.
void RaceMod_Draw(void);

#endif
