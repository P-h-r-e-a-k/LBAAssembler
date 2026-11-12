# The hidden bowling alley in Otringal -- a proposal

The user, 2026-11-12: a hidden bowling area in Otringal; Francos as the pins, Spheros as the balls; the bowlers the big ones (388 monkey
monster, 363 monkey monster with sword, 419 sailor, 295 green gloom); reached by a lift in the centre of the square platform by the prison's
exit (picture: E:\dump\TEMP\user_screenshots\2026-11-12_otringal_bowling_lift.png); finding it is an achievement; an unlockable mini-game on
LBRA's main menu, playing as one of the four (not Twinsen: too short to use a Sphero for a ball).

Decided already: the lift (an Action lift like Stella Entities'), the alley a new interior scene, 2-3 lanes with the bowlers on the outer two,
the achievement ("bowling" is in the engine's list; its trigger is the alley's scene entered), the unlock per player profile.

## To build, in order

1. **The way in.** In Otringal's outside scene by the prison's exit: the platform's middle rectangle gets the lift's plate (CitadelProps lift
   pieces, as Stella's), Action -> down into the alley scene (393). The race track's corner stays clear (the platform is beside it).
2. **The alley (scene 393).** An interior on a grid of Otringal's (the casino's, 135, for its gold and brass), three lanes of boards as one
   prop (a lane body), a pin deck at the far end, a ball return, a scoreboard prop, a way back up (the lift again).
3. **The bowlers.** Copies of the four big characters, one each on the two outer lanes in turn, each with a short bowling animation (a swing
   and a release), a Sphero (entity "sphero", the rolling creature) as the ball; ten Francos stand at the far end as the pins, and fall by
   their hit/fall animations when the ball reaches them, then are set up again for the next frame.
4. **The mini-game (main menu: Bowling, unlocked by finding the alley).** Pick one of the four. Per frame (ten, two balls each):
   - *aim:* Left/Right slides the bowler along the foul line; Space starts a power bar that swings left-right-left; Space again releases
     (a second bar for the hook: how far the Sphero curves);
   - *the roll:* the Sphero travels down the lane at a speed from the power, its line curved by the hook; pins it touches fall, and the ones
     they touch (a pin's fall pushes the next, simple radius test);
   - *scoring:* the usual -- strike 10 + the next two balls, spare 10 + the next one -- on a scoreboard prop and a HUD line; the best score is
     kept per player (BESTTIMES-style file in the player's folder); a perfect game is an achievement of its own.
5. **Engine side.** The mini-game is a race-game page set in the alley scene (RACEGAME.CPP) like the story's pages: it starts the scene with
   the bowler chosen, takes the keys itself (as the pool's swim does, RACEMOD.CPP PoolFrame), moves the Sphero and the pins' actors by script
   variables, and returns to the menu when the game is over.

## Questions for the user
- Is one bowler enough for the mini-game's first version, or do the four play differently (the monkey monsters strong and wild, the sailor
  steady, the gloom a heavy hook)?
- The alley's look: Otringal's gold casino, or a plain dive bar with neon?
- Should Twinsen be able to watch the bowlers from the side (he cannot bowl himself) -- yes, I'd keep it as the place's atmosphere.
