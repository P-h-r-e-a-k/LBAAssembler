#include "COMMON_SDL.H"

#include "SDL3/SDL.h"

#include <stdlib.h>

/* The game quits with exit(), which runs the exit handlers and then the static
   destructors of every loaded library while the audio device's callback thread is
   still running (upstream feb92518, 0510cebd: on Android that thread locked a mutex
   inside the system audio library after its destructor had run, and the process
   aborted). Closing audio stops and joins that thread; registered once audio is up. */
static void ClearAIL(void) { SDL_QuitSubSystem(SDL_INIT_AUDIO); }

void InitAIL() {
    /* Success is reported by the boot log's "Audio" line (see InitAdeline);
       only surface a failure here. */
    if (!SDL_InitSubSystem(SDL_INIT_AUDIO)) {
        SDL_Log("SDL AIL: Could not init audio: %s", SDL_GetError());
        return;
    }
    atexit(ClearAIL);
}
