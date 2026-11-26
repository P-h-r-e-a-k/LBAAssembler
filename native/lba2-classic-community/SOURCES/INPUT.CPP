#include "C_EXTERN.H"

#include <stdio.h>

#include <SYSTEM/KEYBOARD.H>
#include <SYSTEM/KEYBOARD_KEYS.H>
#include <SYSTEM/TIMER.H>

#include "INPUT.H"
#include "MENU_INPUT.H"
#include "RACEGAME.H"

#include <SYSTEM/LOG.H>
#include "JOYSTICK.H"
#include "FOLLOWCAM_CFG.H"

#include "CONSOLE/CONSOLE.H"
#include <SYSTEM/INPUT.H>
#include <SVGA/SCREEN.H>
#include <SVGA/DIRTYBOX.H>

// Input
U32 WaitNoKey = 0;

#ifdef LBA_EDITOR
U32 WaitNoClick = 0;
#endif

U32 LastInput = 0;

/*══════════════════════════════════════════════════════════════════════════*
			██████┐ ██┐ ██┐ ██████┐ ██████┐
			└─██┌─┘ ███┐██│ └─██┌─┘ └─██┌─┘
			  ██│   ██████│   ██│     ██│
			  ██│   ██┌███│   ██│     ██│
			██████┐ ██│└██│ ██████┐   ██│
			└─────┘ └─┘ └─┘ └─────┘   └─┘
 *══════════════════════════════════════════════════════════════════════════*/

//-------------------------------------------------------------------------
void ReadInputConfig(void) {
    S32 initkeys = FALSE;
    S32 winmode;
    S32 n;
    char id[50];

    winmode = DefFileBufferReadValueDefault("WinMode", FALSE);

    if (!winmode) {
        RestoreInput();
    } else {
        const T_DEF_KEY *DEFKEYSDEFAULT;
        DEFKEYSDEFAULT = DefKeysDefault95;

        // On peut identifier chaque input avec un tableau de chaine,
        // mais bon, bof !!!

        for (n = 0; n < MAX_INPUT_SLOTS; n++) {
            snprintf(id, sizeof(id), "Input%d_1", n);
            DefKeys[n].Key1 = DefFileBufferReadValueDefault(id, DEFKEYSDEFAULT[n].Key1);

            snprintf(id, sizeof(id), "Input%d_2", n);
            DefKeys[n].Key2 = DefFileBufferReadValueDefault(id, DEFKEYSDEFAULT[n].Key2);

            if (DefKeys[n].Key1 OR DefKeys[n].Key2)
                initkeys = TRUE;
        }

        /* Blindage si les Inputs sont configurés à 0.
           A cfg with no input bindings at all leaves the game unplayable: the
           menu still opens, because its key is not in this table, but nothing
           moves, and the remap screen needs the very bindings that are gone.

           Clearing every keyboard binding IS reachable from the remap screen,
           so an empty keyboard table alone is not proof of corruption -- a
           gamepad-only layout looks exactly like that. The pad bindings decide
           between the two, and they have to be read from the cfg here rather
           than from GamepadKeys: ReadGamepadConfig runs after this function, so
           the live table still holds whatever preceded it. */
        if (!initkeys) {
            for (n = 0; n < MAX_INPUT_SLOTS; n++) {
                /* Defaulted exactly as ReadGamepadConfig will default them, so
                   the probe asks what the pad bindings are going to BE, not
                   what the file happens to spell out. A cfg predating gamepad
                   support has no Gamepad* keys at all and still ends up with
                   the default pad layout. */
                snprintf(id, sizeof(id), "Gamepad%d", n);
                if (DefFileBufferReadValueDefault(id, GamepadKeysDefault[n].Key1)) {
                    initkeys = TRUE;
                    break;
                }
                snprintf(id, sizeof(id), "Gamepad%d_2", n);
                if (DefFileBufferReadValueDefault(id, GamepadKeysDefault[n].Key2)) {
                    initkeys = TRUE;
                    break;
                }
            }
        }

        if (!initkeys) {
            Log_Warn("No usable keyboard or gamepad bindings in the config; "
                     "restoring the default layout.");
            RestoreInput();
        }
    }

    // (the car's own keys -- CarInput<slot>_1/_2, today's keys where unset: docs/plan/REBIND_PLAN.md)
    RestoreCarKeys(TRUE, FALSE);
    for (n = 0; n < CAR_PAGE_ROWS; n++) {
        const S32 slot = CarPageSlot[n];
        snprintf(id, sizeof(id), "CarInput%d_1", slot);
        CarKeys[slot].Key1 = DefFileBufferReadValueDefault(id, CarKeysDefault[slot].Key1);
        snprintf(id, sizeof(id), "CarInput%d_2", slot);
        CarKeys[slot].Key2 = DefFileBufferReadValueDefault(id, CarKeysDefault[slot].Key2);
    }
    // (two players at one keyboard: each player's driving keys -- TwoKeys<row>_1/_2)
    RestoreTwoKeys();
    for (n = 0; n < TWO_KEYS; n++) {
        snprintf(id, sizeof(id), "TwoKeys%d_1", n);
        TwoKeys[n].Key1 = DefFileBufferReadValueDefault(id, TwoKeysDefault[n].Key1);
        snprintf(id, sizeof(id), "TwoKeys%d_2", n);
        TwoKeys[n].Key2 = DefFileBufferReadValueDefault(id, TwoKeysDefault[n].Key2);
    }

    InitInput();
}

//-------------------------------------------------------------------------
void WriteInputConfig(void) {
    S32 n;
    char id[50];

    /* No "is there a config yet" guard. There used not to be one to write into
       until the engine had installed a default at boot; nothing installs one now
       that the game data's config is layered rather than copied, so a fresh
       profile reaches its first exit with no file. Refusing here dropped every
       binding on that first exit while the settings written after it created the
       file anyway, so a rebind made in the first session vanished. The writes
       below create the file like any other. */

    DefFileBufferWriteValue("WinMode", TRUE);

    // On peut identifier chaque input avec un tableau de chaine,
    // mais bon, bof !!!

    for (n = 0; n < MAX_INPUT_SLOTS; n++) {
        snprintf(id, sizeof(id), "Input%d_1", n);
        DefFileBufferWriteValue(id, DefKeys[n].Key1);

        snprintf(id, sizeof(id), "Input%d_2", n);
        DefFileBufferWriteValue(id, DefKeys[n].Key2);
    }

    for (n = 0; n < CAR_PAGE_ROWS; n++) {
        const S32 slot = CarPageSlot[n];
        snprintf(id, sizeof(id), "CarInput%d_1", slot);
        DefFileBufferWriteValue(id, CarKeys[slot].Key1);
        snprintf(id, sizeof(id), "CarInput%d_2", slot);
        DefFileBufferWriteValue(id, CarKeys[slot].Key2);
    }
    for (n = 0; n < TWO_KEYS; n++) {
        snprintf(id, sizeof(id), "TwoKeys%d_1", n);
        DefFileBufferWriteValue(id, TwoKeys[n].Key1);
        snprintf(id, sizeof(id), "TwoKeys%d_2", n);
        DefFileBufferWriteValue(id, TwoKeys[n].Key2);
    }
}

//-------------------------------------------------------------------------
void ReadGamepadConfig(void) {
    JoystickDeadzone = DefFileBufferReadValueDefault("GamepadDeadzone", 8000);
    if (JoystickDeadzone < 0)
        JoystickDeadzone = 0;
    if (JoystickDeadzone > 32767)
        JoystickDeadzone = 32767;

    GamepadCameraAnalog = DefFileBufferReadValueDefault("GamepadCameraAnalog", TRUE);
    if (GamepadCameraAnalog != 0)
        GamepadCameraAnalog = TRUE;
    else
        GamepadCameraAnalog = FALSE;

    GamepadCameraSensX = DefFileBufferReadValueDefault("GamepadCameraSensX", GAMEPAD_CAM_SENS_DEFAULT);
    if (GamepadCameraSensX < 1)
        GamepadCameraSensX = 1;
    if (GamepadCameraSensX > 10)
        GamepadCameraSensX = 10;

    GamepadCameraSensY = DefFileBufferReadValueDefault("GamepadCameraSensY", GAMEPAD_CAM_SENS_DEFAULT);
    if (GamepadCameraSensY < 1)
        GamepadCameraSensY = 1;
    if (GamepadCameraSensY > 10)
        GamepadCameraSensY = 10;

    GamepadCameraInvertY = DefFileBufferReadValueDefault("GamepadCameraInvertY", FALSE);
    if (GamepadCameraInvertY != 0)
        GamepadCameraInvertY = TRUE;
    else
        GamepadCameraInvertY = FALSE;

    GamepadCamMaxBeta = DefFileBufferReadValueDefault("GamepadCamMaxBeta", GAMEPAD_CAM_BETA_MAX_DEFAULT);
    if (GamepadCamMaxBeta < 1)
        GamepadCamMaxBeta = 1;
    if (GamepadCamMaxBeta > 200)
        GamepadCamMaxBeta = 200;

    GamepadCamMaxAlpha = DefFileBufferReadValueDefault("GamepadCamMaxAlpha", GAMEPAD_CAM_ALPHA_MAX_DEFAULT);
    if (GamepadCamMaxAlpha < 1)
        GamepadCamMaxAlpha = 1;
    if (GamepadCamMaxAlpha > 100)
        GamepadCamMaxAlpha = 100;

    char id[32];
    for (S32 n = 0; n < MAX_INPUT_SLOTS; n++) {
        snprintf(id, sizeof(id), "Gamepad%d", n);
        GamepadKeys[n].Key1 =
            DefFileBufferReadValueDefault(id, GamepadKeysDefault[n].Key1);
        snprintf(id, sizeof(id), "Gamepad%d_2", n);
        GamepadKeys[n].Key2 =
            DefFileBufferReadValueDefault(id, GamepadKeysDefault[n].Key2);
    }
    // (the car's own buttons: CarGamepad<slot>_1/_2)
    RestoreCarKeys(FALSE, TRUE);
    for (S32 n = 0; n < CAR_PAGE_ROWS; n++) {
        const S32 slot = CarPageSlot[n];
        snprintf(id, sizeof(id), "CarGamepad%d_1", slot);
        CarPadKeys[slot].Key1 = DefFileBufferReadValueDefault(id, CarPadKeysDefault[slot].Key1);
        snprintf(id, sizeof(id), "CarGamepad%d_2", slot);
        CarPadKeys[slot].Key2 = DefFileBufferReadValueDefault(id, CarPadKeysDefault[slot].Key2);
    }

    // Fold the freshly-loaded gamepad bindings into the combined input table.
    // Boot order runs this after ReadInputConfig()'s InitInput(), so this is
    // the rebuild that actually captures the configured pad keys.
    InitInput();
}

//-------------------------------------------------------------------------
void WriteGamepadConfig(void) {
    /* Same as WriteInputConfig above: no file yet is the ordinary first-exit
       state of a fresh profile, not a reason to drop the pad settings. */

    DefFileBufferWriteValue("GamepadDeadzone", JoystickDeadzone);
    DefFileBufferWriteValue("GamepadCameraAnalog", GamepadCameraAnalog);
    DefFileBufferWriteValue("GamepadCameraSensX", GamepadCameraSensX);
    DefFileBufferWriteValue("GamepadCameraSensY", GamepadCameraSensY);
    DefFileBufferWriteValue("GamepadCameraInvertY", GamepadCameraInvertY);
    DefFileBufferWriteValue("GamepadCamMaxBeta", GamepadCamMaxBeta);
    DefFileBufferWriteValue("GamepadCamMaxAlpha", GamepadCamMaxAlpha);

    char id[32];
    for (S32 n = 0; n < MAX_INPUT_SLOTS; n++) {
        snprintf(id, sizeof(id), "Gamepad%d", n);
        DefFileBufferWriteValue(id, GamepadKeys[n].Key1);
        snprintf(id, sizeof(id), "Gamepad%d_2", n);
        DefFileBufferWriteValue(id, GamepadKeys[n].Key2);
    }
    for (S32 n = 0; n < CAR_PAGE_ROWS; n++) {
        const S32 slot = CarPageSlot[n];
        snprintf(id, sizeof(id), "CarGamepad%d_1", slot);
        DefFileBufferWriteValue(id, CarPadKeys[slot].Key1);
        snprintf(id, sizeof(id), "CarGamepad%d_2", slot);
        DefFileBufferWriteValue(id, CarPadKeys[slot].Key2);
    }
}

//-------------------------------------------------------------------------
// The car's own slots with no Input bit -- the handbrake, the race's view (INPUT_BINDINGS.H CAR_SLOT_*): held when any of their keys or
// buttons is, or when this poll put a press in (the second player's keys at a shared keyboard, RACEGAME.CPP RaceGame_SharedKeys)
static U32 s_carInjected = 0;

S32 Bindings_CarHeld(S32 slot) {
    if (slot < 0 || slot >= CAR_SLOTS) return FALSE;
    if (slot >= 32 && (s_carInjected & (1u << (slot - 32)))) return TRUE;
    const U32 keys[4] = {CarKeys[slot].Key1, CarKeys[slot].Key2, CarPadKeys[slot].Key1, CarPadKeys[slot].Key2};
    for (S32 i = 0; i < 4; i++)
        if (keys[i] && CheckKey(keys[i])) return TRUE;
    return FALSE;
}

void Bindings_Inject(S32 slot) {
    if (slot >= 32 && slot < CAR_SLOTS) s_carInjected |= 1u << (slot - 32);
}

void Bindings_ClearInjected(void) { s_carInjected = 0; }

//-------------------------------------------------------------------------
// The pad in a menu, the usual way round: A picks, B goes back (Start, the menu's own button, closes it as before). The game binds B to
// I_RETURN -- Enter, which picks in a menu -- so a menu reads I_RETURN less the pad's B (the user, 2026-11-25, "We need to look at
// tightening up the controls on the menu": B picked the row and A did nothing, the other way round to every pad game)
// (A and B pressed this poll, from TabKeys as GetInput left them -- the pad's state and a harness's `key pad-a` alike; MyKey's pad scancode
// comes from the device alone, and only when no keyboard key is down)
static S32 s_padAWas = 0, s_padBWas = 0, s_padAEdge = 0, s_padBEdge = 0;
static void MenuPadEdges(S32 clear) {
    const S32 a = !clear && CheckKey(K_GAMEPAD_A) != 0, b = !clear && CheckKey(K_GAMEPAD_B) != 0;
    s_padAEdge = a && !s_padAWas;
    s_padBEdge = b && !s_padBWas;
    if (!clear) {
        s_padAWas = a;
        s_padBWas = b;
    }
}
S32 MenuPad_Accept(void) { return s_padAEdge; }
S32 MenuPad_Back(void) { return s_padBEdge; }
U32 MenuPad_Input(void) { return CheckKey(K_GAMEPAD_B) ? Input & ~(U32)I_RETURN : Input; }

//-------------------------------------------------------------------------
// The game's own frame's read (PERSO.CPP MainLoop): the car's keys while Twinsen drives. Every other read -- the menus, the inventory, the
// holomap, a dialogue, the remap pages -- takes the on-foot keys, so a key moved in the car never leaves a menu out of reach.
static S32 s_gameRead = 0, s_gameCar = 0;

void MyGetInputGame(S32 car) {
    s_gameRead = 1;
    s_gameCar = car;
    MyGetInput();
    s_gameRead = 0;
}

void MyGetInput() {
    S32 suppressConsole;
    Bindings_ClearInjected();
    Bindings_UseCar(s_gameRead && s_gameCar);
    // GetJoys() writes the gamepad state into TabKeys; GetInput() then folds
    // both keyboard and gamepad into Input through the combined table built by
    // InitInput(), so the pad honours NoRepeatInput like the keyboard does.
    GetJoys((U32 *)&TabKeys[256]);
    RaceGame_KeysBefore();
    GetInput(0); // no norepeat keys
    // (a change of set: the keys held then do nothing till let go)
    Input = Bindings_FilterSwitch(Input);
    MenuPadEdges(FALSE);

    MyKey = Key;
    // Gamepad "press any key" parity: if no keyboard key was pressed this
    // frame, expose the first newly-pressed gamepad scancode through MyKey so
    // intro/logo skips, WaitInput(), and menu prompts all respond to pad input
    // without needing to touch each callsite. Gamepad scancodes are >= 1024 so
    // they can't collide with existing MyKey == K_FOO keyboard branches.
    if (!MyKey) {
        MyKey = (S32)JoystickFirstPressedScancode();
    }
    RaceGame_SharedKeys();

    /* Reserve configured console toggle key from gameplay so toggle isn't handled twice. */
    {
        int toggle_scancode = Console_GetToggleScancode();
        if (toggle_scancode > 0 && toggle_scancode < TABKEYS_NUM_KEYS)
            TabKeys[(U32)toggle_scancode] = 0;
        if (Key == toggle_scancode)
            Key = 0;
    }
    suppressConsole = Console_ShouldSuppressInputThisFrame();
    MenuInput_Poll(suppressConsole || Console_IsOpen());
    /* When we just closed the console, suppress all game input for one frame so the toggle key doesn't trigger camera etc. */
    if (suppressConsole) {
        Key = 0;
        Input = 0;
        MyKey = 0;
        MenuPadEdges(TRUE);
        /* Clear I_CAMERA keys (index 22 per INPUT.H) so they don't fire on the suppress frame. */
        TabKeys[DefKeys[22].Key1] = 0;
        TabKeys[DefKeys[22].Key2] = 0;
        return;
    }
    if (Console_IsOpen()) {
        Console_Update();
        /* While the console is open, completely isolate game input:
		   no keys or joystick bits should be visible to gameplay or debug tools. */
        Key = 0;
        MyKey = 0;
        Input = 0;
        MenuPadEdges(TRUE);
        memset(TabKeys, 0, sizeof(TabKeys));
#ifdef LBA_EDITOR
        MyClick = 0;
#endif
        /* Force present so console updates (keypress, Enter/new line) are visible every frame.
           Marked as an overlay so it does not step the fixed-dt virtual clock: the main-loop
           render still presents this tick, and counting both ran the game clock at double
           rate for as long as the console was open. */
        BoxStaticAdd(0, 0, (S32)(ModeDesiredX - 1), (S32)(ModeDesiredY - 1));
        Timer_FixedDtOverlayPresent();
        BoxUpdate();
        return;
    }

    // Attente d'événement de touche dans la boucle

    if (WaitNoKey) {
        if (MyKey) {
            MyKey = 0;
        } else
            WaitNoKey = 0;
    }

#ifdef LBA_EDITOR
    MyClick = Click;

    if (WaitNoClick) {
        if (MyClick & WaitNoClick) {
            MyClick &= ~(WaitNoClick);
        } else
            WaitNoClick = 0;
    }
#endif
}

//***************************************************************************
/*──────────────────────────────────────────────────────────────────────────*/
void WaitNoInput(void) {
    MyGetInput();

    // (a second at most: a pad's stick resting off centre, or a trigger, held the page here for ever with nothing drawn -- the menu audit,
    // 2026-11-26. What is still held then is masked till let go, so it falls through to nothing)
    ManageTime();
    const U32 until = TimerSystemHR + 1000;
    while (MyKey OR Input OR MenuInput_Held()
#ifdef LBA_EDITOR
               OR MyClick
#endif
    ) {
        ManageTime();
        if ((S32)(TimerSystemHR - until) >= 0) {
            InitWaitNoInput(Input);
            InitWaitNoKey();
            MenuInput_Suppress(MENU_INPUT_ALL);
            break;
        }
        MyGetInput();
    }
}

void WaitNoMenuInput(void) {
    if (!MenuInput_Held())
        return;
    SaveTimer();
    while (MenuInput_Held())
        MyGetInput();
    RestoreTimer();
}

/*──────────────────────────────────────────────────────────────────────────*/
void WaitInput(void) {
    MyGetInput();

    while (!MyKey AND !Input
#ifdef LBA_EDITOR
               AND !MyClick
#endif
    )
        MyGetInput();
}
//***************************************************************************
