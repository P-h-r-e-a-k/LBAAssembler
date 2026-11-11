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

## Getting in

The user, 2026-11-11: "Twinsen can access this area at any time whilst on Emerald moon. There's no charge to get in." The lift works from
the moment he arrives on the Moon (chapter 4 on); the bouncers at the entrance let him through and only keep him out of backstage and the
office.

## The people

- **Bouncers**: Rick's, entity 297 (two at the entrance, one at backstage) and 302 (the office).
- **Performers** (the user: "a few female characters of different species from both worlds, they should be topless just wearing thongs"):
  women of the game's own cast, all of them clearly adults -- none of those the game calls a girl:

  | Performer | Species | World | From |
  |---|---|---|---|
  | the Hacienda's bather | Grobo | Twinsun | the Desert (the grobo lady in the bath) |
  | the School of Magic's assistant | Rabbibunny | Twinsun | the flag marshal on Twinsun's tracks |
  | the pharmacy's customer | Quetch | Twinsun | Citadel Island (the umbrella's owner) |
  | the Imperial Hotel's Sup in her bikini | Sup | Zeelich | the flag marshal on Zeelich's tracks |
  | a Franco lady | Franco | Zeelich | Otringal's streets |
  | the saucer's stewardess | Sup | Zeelich | the Moon (a Sup lady aboard the saucer) |

  Each one a copy of her model made over for the stage: topless, in a thong -- her top's faces in her skin's colour, a thong in place of
  her clothes below. Three on the poles at a time, the others backstage at the mirrors or by the bar between turns, taking turns.
  New animations: a pole spin, a sway, a walk along the catwalk.
- **The owner** in the office, a barman, customers at the tables.

## How it's built

1. Baldino's cell (scene 54): the lift's platform and its zone in the corner by the door, hidden until used.
2. New interior scenes (free slots under 255): the corridor and the club, or one scene holding both.
3. The club's grid: a round room laid out cell by cell, from Rick's bar's and office's block library; the corridor from scene 54's.
4. The reactor's column, the poles, the mirrors, the rail and the lockers as new props; Rick's furniture as the game's own bodies.
5. The actors and their scripts: the bouncers' lines, the dancers on the poles, the barman.

About a day and a half: the round grid and the dancers' animations are the biggest parts.

## Questions for the user

1. **The performers**: the six above, or others in or out?
2. **A name** for the club, over its door.

(Answered: any time on the Moon; no charge; women of several species from both worlds, topless, in thongs; the lift's corner is the one in
moon\cell_lift2.png -- inside the cell, left of the door, hidden behind the stone pillar beside it.)
