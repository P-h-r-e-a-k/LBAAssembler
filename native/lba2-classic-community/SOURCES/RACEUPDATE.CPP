/* RACEUPDATE: the race game's updater started -- RACEUPDATE.H says what it is. Its own file for windows.h, whose names (Font, RGB, ...)
 * the engine's sources have too. */
#include "RACEUPDATE.H"

#include <SYSTEM/LOG.H>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include <cwchar>

// A UTF-8 path as a full wide one, its trailing separators off (a "dir\" before a closing quote would escape it). Zero: it can't be.
static int Wide(const char *utf8, wchar_t *out, DWORD size) {
    wchar_t raw[MAX_PATH * 4];
    if (!MultiByteToWideChar(CP_UTF8, 0, utf8, -1, raw, (int)(sizeof(raw) / sizeof(raw[0])))) return 0;
    const DWORD n = GetFullPathNameW(raw, size, out, NULL);
    if (!n || n >= size) return 0;
    size_t len = wcslen(out);
    while (len > 3 && (out[len - 1] == L'\\' || out[len - 1] == L'/')) out[--len] = 0;
    return 1;
}

int RaceUpdate_Exists(const char *pathUtf8) {
    wchar_t path[MAX_PATH * 4];
    if (!Wide(pathUtf8, path, MAX_PATH * 4)) return 0;
    const DWORD attr = GetFileAttributesW(path);
    return attr != INVALID_FILE_ATTRIBUTES && !(attr & FILE_ATTRIBUTE_DIRECTORY);
}

int RaceUpdate_Spawn(const char *exeUtf8, const char *installDirUtf8, int manual) {
    wchar_t exe[MAX_PATH * 4], dir[MAX_PATH * 4], work[MAX_PATH * 4];
    if (!Wide(exeUtf8, exe, MAX_PATH * 4) || !Wide(installDirUtf8, dir, MAX_PATH * 4)) return 0;
    const DWORD attr = GetFileAttributesW(exe);
    if (attr == INVALID_FILE_ATTRIBUTES || (attr & FILE_ATTRIBUTE_DIRECTORY)) return 0;
    wcscpy(work, exe);
    wchar_t *slash = wcsrchr(work, L'\\');
    if (slash) *slash = 0;
    static wchar_t cmd[MAX_PATH * 10];
    swprintf(cmd, sizeof(cmd) / sizeof(cmd[0]), L"\"%ls\" --check-update \"%ls\" --wait-pid %lu%ls", exe, dir, (unsigned long)GetCurrentProcessId(),
             manual ? L" --manual" : L"");
    // (no STARTF_USESHOWWINDOW/SW_HIDE: the updater has no window of its own, and a hidden show state would hide the first one it opens --
    // its "Install it now?" question)
    STARTUPINFOW si;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    PROCESS_INFORMATION pi;
    ZeroMemory(&pi, sizeof(pi));
    // (no handles handed down, no window, a process group of its own: it outlives the game, which it waits for)
    if (!CreateProcessW(exe, cmd, NULL, NULL, FALSE, CREATE_NO_WINDOW | CREATE_NEW_PROCESS_GROUP, NULL, slash ? work : NULL, &si, &pi)) {
        Log_Warn("[racegame] the updater could not be started (%s): error %lu", exeUtf8, (unsigned long)GetLastError());
        return 0;
    }
    CloseHandle(pi.hThread);
    CloseHandle(pi.hProcess);
    Log_Info("[racegame] the updater started (pid %lu): %s --check-update%s", (unsigned long)pi.dwProcessId, exeUtf8, manual ? " --manual" : "");
    return 1;
}

#else

int RaceUpdate_Exists(const char *pathUtf8) {
    (void)pathUtf8;
    return 0;
}

int RaceUpdate_Spawn(const char *exeUtf8, const char *installDirUtf8, int manual) {
    (void)exeUtf8;
    (void)installDirUtf8;
    (void)manual;
    return 0;
}

#endif
