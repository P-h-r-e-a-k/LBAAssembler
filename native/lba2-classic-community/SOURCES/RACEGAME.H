#ifndef RACEGAME_H
#define RACEGAME_H

#include <SYSTEM/ADELINE_TYPES.H>

/* RACEGAME: the race game -- LBA Assembler's race tracks played as a game of their own (the standalone Little Big Halloween Racing
 * Adventure), when RACEGAME.TXT is beside the game's data. RACEGAME.CPP says what is in it. */

#define RACEGAME_START 2399                 // RaceGame_Menu: a race is set up (RaceMod_LoadFile) -- a new game, no introduction
#define RACEGAME_STORY_NEW 2398             // ... Story Mode's set loaded (story_file=): a new game of LBA2's, its introduction first
#define RACEGAME_TO_MENU 2396                // ... a replay over: the game left, the menu as at the start (its Achievements page)
#define RACEGAME_STORY_CONTINUE 2397        // ... the same, the story's own save (story.lba) put in current.lba: the game resumed from it

S32 RaceGame_On(void);
// An achievement earned (RACEMOD.CPP achieve=): its title in `title`, non-zero when it is new -- kept for the player, ACHIEVEMENTS.TXT
S32 RaceGame_Achieve(const char *id, char *title, S32 size);
// The bowling game's best score of a bowler, kept for the player: `score` (-1: only asked) kept when it is better; the best then
S32 RaceGame_BowlingBest(S32 bowler, S32 score);                      // RACEGAME.TXT read: the main menu is the race game's
// The race game's main menu, and its pages (Specials, a race or a time trial, the car, Best times): what MainGameMenu does next --
// 70 resume (1000 for Esc with a race on), 74 the options, 75 quit, RACEGAME_START a race, RACEGAME_STORY_NEW / _CONTINUE Story Mode.
// `inGame`: a race (or the story) is on to resume.
S32 RaceGame_Menu(S32 inGame);
S32 RaceGame_MenuText(S32 num, char *string);   // BuildCustomMenuText: a row of the race game's (non-zero), its label in `string`
void RaceGame_RowOver(S32 num, S32 x, S32 y);   // what lies over a row: a banner (the rows' own words already drawn)
S32 RaceGame_RowDisabled(S32 num);          // a row greyed and passed over (Story Mode, Track select until they open)
// OptionsMenu: the menu to show -- `menu` (the game's own), or in the race game a copy with its own rows after the game's (Race
// controls, power-ups; Updates); and a row picked there that isn't the game's: its page shown (non-zero), or 0 (not the race game's)
U16 *RaceGame_OptionsMenu(U16 *menu);
S32 RaceGame_OptionsRow(S32 num);
// RACEMOD.CPP: the player changed the race camera's view during a race (V): kept for the next race (RACEGAME_SETTINGS.TXT camera=)
void RaceGame_CameraChosen(S32 view);

void RaceGame_KeysBefore(void);            // INPUT.CPP: two players on one keyboard, before the keys are read
void RaceGame_SharedKeys(void);            // ... and after
void RaceGame_GhostChosen(int mode);       // RACEMOD.CPP: the ghost raced now, kept as the one chosen last
S32 RaceGame_RaceSkyOff(void);              // RACEMOD.CPP: Options has the sky off in races
void RaceGame_TestKeys(int down);           // (tests: player 2's keys as if down, -1 the keyboard's)
void RaceGame_SceneEntered(void);           // OBJECT.CPP: a scene entered (the autosave's moment) -- Story Mode's story.lba kept there
int RaceGame_StoryTwo(void);              // Story Mode with two players: player 1's game, player 2's joined (2026-11-11, player 2 is Zoe)
int RaceGame_StoryZoe(void);              // ... player 2's game, Zoe's, following player 1's story
int RaceGame_StoryShare(void);
void RaceGame_StoryZoeJoin(void);
int RaceGame_StoryZoeWait(char *path, int size);   // ... before the menus (tests): the first save to load waited for          // ... player 2's started straight into a game (tests): joined from it
int RaceGame_StoryTwoTest(int x, int y, int w, int h);   // (tests: the console's storytwo)           // ... player 1's: the story kept now and player 2's told to load it
void RaceGame_StoryTest(int on);            // (tests: the run played as Story Mode's story, the console's `storymode`)

/* The player playing is 18 or more (or the game has no players): Stella Entities' way in open (RACEMOD.CPP adult_var=) */
S32 RaceGame_PlayerAdult(void);
// A replay of an achievement started (its new game: no introduction film -- GAMEMENU.CPP new_game)
S32 RaceGame_Replaying(void);
#endif
