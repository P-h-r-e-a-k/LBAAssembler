#pragma once

#include "SDK.H"

#ifdef LBA2_USE_SDL2
struct AILAudioStream;
#else
typedef SDL_AudioStream AILAudioStream;
#endif

typedef void(SDLCALL *AILAudioCallback)(void *, AILAudioStream *, int, int);

bool AILAudio_Initialized(void);
AILAudioStream *AILAudio_Open(const SDL_AudioSpec *spec, AILAudioCallback callback, void *userdata);
void AILAudio_Destroy(AILAudioStream *stream);
void AILAudio_Lock(AILAudioStream *stream);
void AILAudio_Unlock(AILAudioStream *stream);
bool AILAudio_Put(AILAudioStream *stream, const void *data, int bytes);
bool AILAudio_Flush(AILAudioStream *stream);
int AILAudio_Queued(AILAudioStream *stream);
bool AILAudio_Gain(AILAudioStream *stream, float gain);
bool AILAudio_Resume(AILAudioStream *stream);
bool AILAudio_Pause(AILAudioStream *stream);
bool AILAudio_LoadWAV(const char *path, SDL_AudioSpec *spec, Uint8 **data, Uint32 *bytes);
bool AILAudio_LoadWAVMemory(const void *data, int bytes, SDL_AudioSpec *spec, Uint8 **pcm, Uint32 *pcmBytes);

#ifndef LBA2_USE_SDL2
/* LBA2_AUDIO_CAPTURE: every stream on no device, pulled by the video's dump (FRAMEDUMP.CPP) on the game's clock */
bool AILAudio_Capturing(void);
void AILAudio_CaptureMix(Sint16 *out, int frames);
#endif

#if defined(LBA2_USE_SDL2) && defined(LBA2_AUDIO_TESTS)
void AILAudio_TestRead(AILAudioStream *stream, float *output, int samples);
void AILAudio_TestMix(float *output, int samples);
int AILAudio_TestConvertedBytes(AILAudioStream *stream);
#endif
