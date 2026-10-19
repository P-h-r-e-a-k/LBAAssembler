#ifndef RACEUPDATE_H
#define RACEUPDATE_H

/* RACEUPDATE: the race game's updater started (RACEGAME.CPP, 2026-10-19): the installer's copy beside the game (Update\LBRA-Setup.exe)
 * run as `LBRA-Setup.exe --check-update "<install dir>" --wait-pid <the game's pid> [--manual]`, hidden and detached -- it looks for a
 * new version, downloads it, and installs it once the game is closed (the installer's Updater.cs). Windows only: elsewhere nothing. */

// Whether the file is there.
int RaceUpdate_Exists(const char *pathUtf8);
// The updater started (non-zero), or not (its exe missing, CreateProcess failed). Paths in UTF-8 (the engine's code page).
int RaceUpdate_Spawn(const char *exeUtf8, const char *installDirUtf8, int manual);

#endif
