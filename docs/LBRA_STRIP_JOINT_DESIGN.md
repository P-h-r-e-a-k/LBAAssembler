# The strip joint -- design (draft, 2026-11-11)

The user, 2026-11-11: "Design a strip joint. This should be based on the look and feel of the bar and casino on Otringal. This should have a
stage area with poles. Some bouncers -- we can reuse the ones from the bar. An office area. A bar. And a back stage area for our performers
to get changed."

A design to agree before anything is built. Pictures: E:\dump\TEMP\nov11\club\club_plan.png (the floor plan) and club_look.png (the three
scenes it borrows from).

## The look

Otringal's own, from three of its scenes:

- **Rick's bar (scene 136)**: sky-blue walls painted with clouds over a dark stone base; floors of grey and teal octagon tiles; wooden
  balustrades; brass wall lamps with white globes; green card tables; a raised wooden stage with orange velvet curtains. Its bouncers:
  entity 297, three of them in the bar.
- **Rick's office (scene 137)**: dark teal walls; cream sofas and an armchair; a desk with a globe lamp; orange curtains; a framed picture;
  a plant. Its guard: entity 302 ("No touch the Boss!").
- **The casino (scenes 135, 133)**: slot machines, green-railed pits, gold chain ornaments: touches for the lobby and the bar.

The room is one new interior scene whose bricks are Rick's bar's and office's own (their block library), laid out afresh, as the Tralu's
secret bar was made from scene 19's hall.

## The rooms (floor plan: 46 by 36 cells, north at the top; the game's camera looks in from the south, as in Rick's bar)

| Area | Where | What's in it |
|---|---|---|
| **Lobby** | south-east, the street door | Two of Rick's bouncers (entity 297) either side of the inner door; a casino slot machine or two. |
| **Main floor** | the middle | Octagon tiles, Rick's round green tables with chairs, brass wall lamps on the cloud walls. |
| **Stage** | the north wall, raised | Rick's wooden stage and orange velvet curtains, a runway out into the room; **three brass poles**: two on the stage, one at the runway's end. |
| **Bar** | the west wall | A long counter with stools (red seats) and a barman; a shelf of bottles with brass lamps behind. |
| **Office** | north-east | Rick's office: teal walls, desk and globe lamp, sofa and armchair, a picture; a bouncer (entity 302) at its door. |
| **Backstage** | north-west, behind the curtains | The performers' dressing room: four mirrors with lights, a clothes rail, lockers, a folding screen; a door onto the stage and a "staff only" bouncer at its door to the main floor. |
| **Staff corridor** | behind the stage | Joins backstage and the office. |

## The people

- **Bouncers**: Rick's, entity 297 (two in the lobby, one at the backstage door) and 302 (the office door). They turn Twinsen away from the
  staff-only doors the way Rick's say "The boss don't see nobody."
- **Performers**: the story's own dancers -- the bikini Sup and the rabbibunny marshal models -- on the poles, in their stage costumes. They
  need a few new animations: a pole spin, a sway, a walk down the runway. Nothing more than the game shows today.
- **The owner** in the office, a barman, and customers at the tables (Otringal's townsfolk, as Rick's bar has).

## How it's built

1. A new scene slot (a free one under 255, so the holomap and saves hold it).
2. Its grid made from Rick's bar's block library: floors, walls, the stage and the balustrades placed by cell to this plan.
3. The furniture as the game's own bodies where they exist (Rick's tables, sofas, desk, lamps), the poles, mirrors, clothes rail and lockers
   as new props (the props' entity, as the garage's).
4. The actors and their scripts: the bouncers' lines, the dancers' animations on the poles, the barman.
5. A door to it on the island chosen, with its way back out.

About a day's work once the questions below are answered. The grid and the dancers' animations are the biggest parts.

## Questions for the user

1. **Where** is it: on Otringal (which street, which door), or another island?
2. **Its place in the game**: part of the story (which chapter, and does Twinsen have a reason to go in), or a place to find and visit?
3. **Getting in**: open to anyone, members only (a token, kashes at the door), or only at night?
4. **The performers**: the bikini Sup and the rabbibunny, or others? How many on the poles at once?
5. **A name** for the club, for its sign over the street door.
