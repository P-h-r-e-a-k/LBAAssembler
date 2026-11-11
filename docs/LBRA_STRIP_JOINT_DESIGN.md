# The strip joint -- design (draft 2, 2026-11-11)

The user, 2026-11-11: "Design a strip joint. This should be based on the look and feel of the bar and casino on Otringal. This should have a
stage area with poles. Some bouncers -- we can reuse the ones from the bar. An office area. A bar. And a back stage area for our performers
to get changed." Then: "It will be for Emerald moon. There's to be a hidden elevator inside Baldino's cell in the corner immediately to the
left of the door, that takes us down to a corridor that then takes us left into the area under the giant reactor that's on the moon, as a
result the strip joint should be roughly circular in dimension."

A design to agree before anything is built. Pictures (E:\dump\TEMP\nov11\club\): club_plan_round.png (the floor plan),
moon\cell_lift.png (where the lift would be in Baldino's cell), club_look.png (the three Otringal scenes it borrows its look from).

## The way in

- **Baldino's old cell** (the Emerald Moon base, scene 54: the bunk bed, the table and chairs, the barred door): a hidden lift in the
  corner immediately to the left of the door. Nothing shows until Twinsen stands on it (or presses Action on it), then the floor goes
  down -- a platform like the house's lift to its secret chamber.
- **The corridor** at its foot, in the moon base's own look (scene 54's metal walls and floor tiles): Twinsen turns left from the lift and
  walks to the club's door, under the reactor.

## The club: round, under the reactor

About 40 cells across. The reactor's glowing column comes down through the middle of the room. Otringal's look inside -- Rick's bar's
cloud walls, octagon tiles, brass lamps and green tables, Rick's office's teal and cream, the casino's gold.

| Area | Where | What's in it |
|---|---|---|
| **Entrance** | east, where the corridor comes in | Two of Rick's bouncers (entity 297) either side of the door. |
| **Stage** | the middle, round the reactor's column | A raised round wooden stage with **three brass poles** round the column; a **catwalk** from backstage to it. |
| **Floor** | a ring round the stage | Rick's round green tables and chairs, octagon tiles, brass lamps on the curved cloud walls. |
| **Bar** | an arc along the south-west wall | A curved counter with stools, a barman, a shelf of bottles behind. |
| **Backstage** | north, off the rim, where the catwalk starts | The performers' dressing room: mirrors with lights, a clothes rail, lockers, a folding screen. A "staff only" bouncer at its door to the floor. |
| **Office** | north-east, off the rim | Rick's office look: desk and globe lamp, sofa and armchair, a picture; a bouncer (entity 302, "No touch the Boss!") at its door. |

## The people

- **Bouncers**: Rick's, entity 297 (two at the entrance, one at backstage) and 302 (the office).
- **Performers**: the bikini Sup and the rabbibunny marshal models, in their stage costumes, on the poles. They need a few new
  animations: a pole spin, a sway, a walk down the catwalk.
- **The owner** in the office, a barman, customers at the tables.

## How it's built

1. Baldino's cell (scene 54): the lift's platform and its zone in the corner by the door, hidden until used.
2. New interior scenes (free slots under 255): the corridor and the club, or one scene holding both.
3. The club's grid: a round room laid out cell by cell, from Rick's bar's and office's block library; the corridor from scene 54's.
4. The reactor's column, the poles, the mirrors, the rail and the lockers as new props; Rick's furniture as the game's own bodies.
5. The actors and their scripts: the bouncers' lines, the dancers on the poles, the barman.

About a day and a half: the round grid and the dancers' animations are the biggest parts.

## Questions for the user

1. **The lift's corner**: the yellow mark in moon\cell_lift.png, just inside the door by the corner pillar -- is that the corner you mean?
2. **When** can Twinsen use the lift: from chapter 4 (the Emerald Moon's chapter), or only after the race, or only once Baldino tells him?
3. **Getting in** at the club's door: open to anyone, or the bouncers want something (kashes, a password)?
4. **The performers**: the bikini Sup and the rabbibunny, or others? How many on the poles at once?
5. **A name** for the club, over its door.
