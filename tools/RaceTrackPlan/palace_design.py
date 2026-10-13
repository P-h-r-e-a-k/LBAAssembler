"""The Emperor's palace race (the user, 2026-10-12: "through the palace rooms, coming out the window and around the roof tops?
Point-to-point i.e. no laps, finishing with massive jump to harbor teleport? - Twinsen, quick, Dark monk's in the palace, he's killing
everybody. Leading into final showdown at Celebration"), on the Palace island (Terrain/Palace: PALACE.ILE, its layout PALACE.JSON).

Second version, the same day (the user: "The palace track is far too narrow ... Let's make the inside tracks wider if we can do, and
perhaps loop in more rooms. Add a section where we're outside on the roof looping around the palace roof tops"): the island's doors are
four bricks wide now (16 cells: PalaceIsland.DoorBricks).

Third version, the same day (the user: "For our palace track let's scale up the roof top so it covers more of the outside of the palace
area. For the inside of the palace let's remove the track but add some arrows to allow cars to take multiple directions, it has a single
entry and exit area so all cars will ultimately end up back on the same track"): no road in the rooms, arrows on their floor, the
opponents each their own way through; a loop round a flat roof top.

Fourth version, 2026-10-13 (the user: "The roof of our Palace needs a lot of work. Let's make the roof top bigger, and remove the track from
the outside for now. The track on the outside needs to be raised in the air looping around the giant turrets that form the roof, which
means the existing track section outside needs to go"): the hilltop is flat 54 cells round the palace now (PalaceCliff), and the race a
sprint (the plan's open) through the palace alone:
  - in the sixteen rooms no road at all (the plan's bare): the cars drive the rooms' floor -- the race-track mode keeps them on it and
    inside the rooms, its open_area= (the plan's openAreas) -- from the start in the south-east room (154) to the north-west one (163) and
    out of its north door into the long hall, each room's ways on west and north shown by arrows on its floor (floorArrows), any way
    through the rooms' doors (all four of a room's walls have one). The route itself, the test pilot's: west along the bottom row (153,
    152, 151) and north up the west column (155, 159, 163); the opponents after the first each their own way (openRoutes, in turn): north
    up the east column and west along the top row; a staircase from 153, north and west by turns; another from 158 (for a fourth opponent);
  - north up the long hall and up a ramp to the round window Twinsen leaves by: the finish line in the window, and the leap out of it over
    the cloud sea between the hill's two arms -- won, the Dark Monk's teleporter takes the car in mid-flight (the race-track mode's sprint,
    wake=, wake_flight=); lost, the race is run again from the grid before it comes down.
(Until the road round the roof's turrets, high in the air, is made.)

Usage: palace_design.py <game folder with PALACE.ILE and PALACE.JSON: the island built on its own, `ScriptRoundTrip palace`> [out plan]
"""
import json, math, sys
import numpy as np
sys.path.insert(0, __file__.rsplit('\\', 1)[0] if '\\' in __file__ else '.')
from ile import load_ile, hqr_entries

GAME = sys.argv[1] if len(sys.argv) > 1 else r'E:\dump\TEMP\palace3\v6'
OUT = sys.argv[2] if len(sys.argv) > 2 else r'E:\dump\LBAAssembler\docs\racetrack\palace_track_plan.json'
L = json.load(open(GAME + r'\PALACE.JSON'))
_, _, _, CUBES = load_ile(GAME + r'\PALACE.ILE')
SP = 0.5                                    # the plan's points, cells apart
FLOOR = L['floorTop']                       # the rooms' floor: what the cars drive on in them
TOP = L['hilltop']                          # the hilltop: the roofs' top
DECK = 60                                   # the raised road over what it lies on
HALF_IN, HALF_OUT = 5.5, 7.0                # the road's half width (cells): up to the round window (16 cells across), out over the sea
JUMP_APEX = 2600                            # the leap's top over the window's sill
FLIGHT = 110                                # the leap's length (cells): a car at three times the race car's speed is still in the air when
                                            # the teleporter takes it (wake_flight=, 2.4 s)
PAD = 25                                    # where the leap comes down (no road: never reached)
# (a car's collision box: some 1,420 units either way of its middle whichever way it faces: 2.8 cells. Where the cars drive the floor,
# nothing within CAR_HALF of a line they follow; on the ramp nothing within the road's half-width less the rail's inset and a box)
CAR_BOX = 2.8
CAR_HALF = 1.7

# ---- where things are
rooms = {r['scene']: r for r in L['rooms']}
def mid(scene):
    r = rooms[scene]; return ((r['x0'] + r['x1'] + 1) / 2, (r['z0'] + r['z1'] + 1) / 2)
# (a row's doors are in a line 2 cells past its rooms' middles, a column's the same: the rooms' grids put them there)
def row(scene): return mid(scene)[1] + 2
def col(scene): return mid(scene)[0] + 2
win = L['window']
wx = (win['x0'] + win['x1']) / 2            # the round window's middle across, the wall's inner face and outer
w_in, w_out = win['z1'], win['z0']
sill = win['sill']
hall = rooms[80]
SIXTEEN = [s for s in rooms if s != 80]
sq_x0, sq_x1 = min(rooms[s]['x0'] for s in SIXTEEN), max(rooms[s]['x1'] for s in SIXTEEN) + 1
sq_z0, sq_z1 = min(rooms[s]['z0'] for s in SIXTEEN), max(rooms[s]['z1'] for s in SIXTEEN) + 1

# ---- the boxes: what a car hits, by height (decors with a real box; world units)
boxes = []
read, _ = hqr_entries(GAME + r'\PALACE.ILE')
cubemap = np.frombuffer(read(0)[:256], dtype=np.uint8).reshape(16, 16)
for cz in range(16):
    for cx in range(16):
        cid = cubemap[cz, cx] & 127
        if not cid or cid not in CUBES: continue
        for d in CUBES[cid].decors:
            if d[10] <= d[7]: continue
            boxes.append((cx * 32768 + d[6], d[7], cz * 32768 + d[8], cx * 32768 + d[9], d[10], cz * 32768 + d[11]))
boxes = np.array(sorted(set(boxes)), float)
print('collision boxes', len(boxes))


def blocked(x, z, y, half):
    """Whether a box stands in a car's way at (x, z) cells, its floor y: within `half` cells of the point, between y+120 and y+900."""
    X, Z = x * 512, z * 512
    hit = (boxes[:, 0] < X + half * 512) & (boxes[:, 3] > X - half * 512) & (boxes[:, 2] < Z + half * 512) & (boxes[:, 5] > Z - half * 512) \
          & (boxes[:, 1] < y + 900) & (boxes[:, 4] > y + 120)
    return bool(hit.any())


def ground(x, z):
    """The island's ground height at (x, z) cells (bilinear in the cell)."""
    gx, gz = int(math.floor(x)), int(math.floor(z))
    cid = cubemap[gz // 64, gx // 64] & 127
    if not cid or cid not in CUBES: return -700
    H = CUBES[cid].heights
    lx, lz = gx % 64, gz % 64; fx, fz = x - gx, z - gz
    return (H[lz, lx] * (1 - fx) * (1 - fz) + H[lz, lx + 1] * fx * (1 - fz) + H[lz + 1, lx] * (1 - fx) * fz + H[lz + 1, lx + 1] * fx * fz)


def path(corners, radii, sp=SP):
    """A line through `corners` (cells), each inner one rounded to its radius (radii[i] for corners[i]; the ends' unused): straight runs
    and circular arcs, sp cells apart."""
    P = [np.array(c, float) for c in corners]
    out = [P[0]]
    cut = [0.0] * len(P)                    # (how far each corner's arc starts before it and ends after it)
    for i in range(1, len(P) - 1):
        a, b, c = P[i - 1], P[i], P[i + 1]
        u = (b - a) / np.linalg.norm(b - a); v = (c - b) / np.linalg.norm(c - b)
        turn = math.atan2(u[0] * v[1] - u[1] * v[0], float(u @ v))     # (> 0: to the right -- clockwise seen from above)
        r = radii[i]; cut[i] = r * math.tan(abs(turn) / 2)
        p0 = b - u * cut[i]
        centre = p0 + np.array([-u[1], u[0]]) * r * (1 if turn > 0 else -1)
        a0 = math.atan2(p0[1] - centre[1], p0[0] - centre[0])
        m = max(2, int(math.ceil(abs(turn) * r / (sp / 4))))
        out.extend(centre + r * np.array([math.cos(a0 + turn * k / m), math.sin(a0 + turn * k / m)]) for k in range(m + 1))
    out.append(P[-1])
    for i in range(len(P) - 1):
        if cut[i] + cut[i + 1] > np.linalg.norm(P[i + 1] - P[i]) + 1e-6:
            raise ValueError(f'corners {tuple(P[i])} and {tuple(P[i + 1])}: their arcs overlap ({cut[i]:.1f} + {cut[i + 1]:.1f} cells)')
    pts = [out[0]]
    for q in out[1:]:
        if np.linalg.norm(q - pts[-1]) > 1e-6: pts.append(q)
    pts = np.array(pts)
    d = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]
    s = np.arange(0, d[-1] + 1e-9, sp)
    return np.stack([np.interp(s, d, pts[:, 0]), np.interp(s, d, pts[:, 1])], 1)


# ---- the rooms: the rows' door lines (z) and the columns' (x), a room's middle where its two cross
R0, R1, R2, R3 = row(154), row(158), row(162), row(166)    # the bottom row's ... the top row's
C0, C1, C2, C3 = col(151), col(152), col(153), col(154)    # the west column's ... the east column's
# (the start line in the east half of room 154, the grid behind it by the room's east wall, all in one cube -- the start's scene; room
# 154's chest stands on its row's door line east of the room's middle, so the grid is south of it, the way on west swinging up to the
# row's door line)
START_X = C3 + 7
GRID_Z = R0 + 11
BEHIND = rooms[154]['x1'] - 6
if START_X // 64 != BEHIND // 64:
    raise ValueError(f'the grid ({START_X}..{BEHIND}) crosses a cube edge')
TURN = 12                                                   # a turn at a room's middle: its radius (cells)
HALL_JOIN = (C0, hall['z1'] - 36)                           # where every way through the rooms is the route again, up the long hall
GRID = [(BEHIND, GRID_Z), (START_X - 9, GRID_Z), (START_X - 25, R0)]
GRID_R = [0, 15, 15]
# (the opponents' ways: north at once through room 154's north door, or west first; a turn at each room's middle)
NORTH_FIRST = [(BEHIND, GRID_Z), (C3, GRID_Z)]
NORTH_FIRST_R = [0, 7]
open_routes = [
    # north up the east column (158, 162, 166), west along the top row (165, 164, 163)
    (NORTH_FIRST + [(C3, R3), (C0, R3), HALL_JOIN], NORTH_FIRST_R + [TURN, TURN, 0]),
    # a staircase: west into 153, north into 157, west into 156, north into 160, west into 159, north into 163
    (GRID + [(C2, R0), (C2, R1), (C1, R1), (C1, R2), (C0, R2), HALL_JOIN], GRID_R + [TURN] * 5 + [0]),
    # another: north into 158, west into 157, north into 161, west into 160, north into 164, west into 163
    (NORTH_FIRST + [(C3, R1), (C2, R1), (C2, R2), (C1, R2), (C1, R3), (C0, R3), HALL_JOIN], NORTH_FIRST_R + [TURN] * 6 + [0]),
]
# the arrows on the floor: in each room, in front of its west door and its north door -- the ways on (none out of the west column's west
# walls nor the top row's north walls but room 163's, into the long hall); and up the long hall
arrows = []
for s in SIXTEEN:
    cx, cz = col(s), row(s)
    if s == 154:
        # (the start's room: the grid south of its door line -- the arrows where the cars pass, swinging up to it, or turning north)
        arrows += [(cx - 16, cz + 3, -1, 0), (cx, cz - 10, 0, -1)]
        continue
    if cx > C0: arrows.append((cx - 12, cz, -1, 0))
    if cz > R3 or s == 163: arrows.append((cx, cz - 12, 0, -1))
arrows += [(C0, hall['z1'] - 40, 0, -1), (C0, (hall['z0'] + hall['z1']) / 2 + 20, 0, -1)]

# ---- the route: the grid, west along the bottom row, north up the west column, the long hall, the round window, the leap north out over
# the cloud sea and the stretch of road it comes down on
if abs(C0 - wx) > 0.01:
    raise ValueError(f'the west column ({C0}) and the round window ({wx}) are not in line')
corners = GRID + [(C0, R0), (wx, w_out - FLIGHT - PAD)]
radii = GRID_R + [TURN, 0]
pts = path(corners, radii)
n = len(pts)
S = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]


def nearest(p, lo=0, hi=None):
    hi = n if hi is None else hi
    return lo + int(np.argmin(np.hypot(pts[lo:hi, 0] - p[0], pts[lo:hi, 1] - p[1])))


start = nearest((START_X, GRID_Z), 0, 200)
i_ramp_foot = nearest((wx, w_in + 30)); i_ramp_top = nearest((wx, w_in + 8), i_ramp_foot)
i_window_in = nearest((wx, w_in), i_ramp_foot); i_window_out = nearest((wx, w_out), i_window_in)
lip = nearest((wx, w_out - 2), i_window_out)
land = nearest((wx, w_out - FLIGHT), lip); land_foot = n - 1
i_hall = nearest(HALL_JOIN, 0, i_ramp_foot)

# ---- heights: the rooms' floor (no road there); the ramp up to the window's sill, level through the window; the leap -- up JUMP_APEX
# and down to the sill's height -- and the stretch of road beyond
h = np.full(n, FLOOR, float)
for i in range(i_ramp_foot, i_ramp_top + 1):
    t = (i - i_ramp_foot) / max(1, i_ramp_top - i_ramp_foot); h[i] = FLOOR + (sill + DECK - FLOOR) * (t * t * (3 - 2 * t))
lipH = sill + DECK
h[i_ramp_top:] = lipH
for i in range(lip, land + 1):
    t = (i - lip) / max(1, land - lip); h[i] = lipH + JUMP_APEX * 4 * t * (1 - t)

# ---- widths (cells from the road's middle to its rail): up to the round window as wide as it allows, wider beyond; the grid's places
# by room 154's south wall two abreast. (In the rooms there is no road: the width only places the grid and the scenes)
half = np.full(n, HALF_IN)
half[land:] = HALF_OUT
HALF_GRID = 3.5
for i in range(0, n):
    if i <= start + 6: half[i] = HALF_GRID
    elif i <= start + 26: half[i] = HALF_GRID + (HALF_IN - HALF_GRID) * (i - start - 6) / 20

# ---- the opponents' ways through the rooms, a cell apart (RaceTrackBuilder.RouteThrough)
routes = [path(c, r, 1.0) for c, r in open_routes]

# ---- the palace's own people in the cars' way (2026-10-13, the user: "The retail game has enemies inside the palace, 3 pigs, Stan from
# Time commando, and lots of soldiers with swords, let's add some of these enemies inside as obstacles, perhaps the could chase and attack
# the cars, or throw random things at them"; RACEMOD.CPP guard=): each [kind, which, x, z, floor, face x, face z, leash] -- kind 0 one of
# the Emperor's soldiers, sword out (steps into the road at a car and strikes), 1 a pighead (throws things at the cars; which: the red
# one with the hammer, the brown with the garden tool, the yellow with the broom), 2 Stan (as a soldier). Each stands on the tiles by a
# room's carpets -- they run through the room's middle, a row's and a column's, two bricks (8 cells) wide -- facing where they cross, and
# steps out at most `leash` cells -- far enough to reach a car on the carpet's middle.
CARPET_HALF = 4.0
def free(x, z, r=1.2): return not blocked(x, z, FLOOR, r)
def post(cx, cz, ox, oz, kind, which, leash, room):
    """A guard by the carpets' crossing at (cx, cz): `ox`, `oz` cells off it (the first free spot along the way out, or None)."""
    for k in range(0, 8):
        x, z = cx + ox * (1 + k * 0.12), cz + oz * (1 + k * 0.12)
        if free(x, z) and all(free(cx + (x - cx) * t, cz + (z - cz) * t, 0.6) for t in (0.85, 0.7)):
            fx, fz = cx - x, cz - z; l = math.hypot(fx, fz)
            return [kind, which, round(x, 2), round(z, 2), FLOOR, round(fx / l, 3), round(fz / l, 3), leash]
    print(f'  WARNING: no free spot for a guard in room {room} at ({cx + ox:.1f}, {cz + oz:.1f})')
    return None
guards = []
SOLDIER, PIG, STAN = 0, 1, 2
# (a soldier in every room but the start's, by turns in a corner of its carpets' crossing; the pigheads further off in two rooms on the
# race's way -- 152 on the bottom row, and 163, where every way meets before the long hall: in 157 and 160, on the opponents' other ways
# only, no car came near enough for a throw; soldiers in the long hall, by its carpet. Not Stan nor the pighead with the broom: they race,
# the user's line-up -- CharactersForTrack.txt -- Stan in his time machine, and the road sweeper)
corner = [(1, 1), (-1, 1), (-1, -1), (1, -1)]
for k, s in enumerate(sorted(SIXTEEN)):
    if s == 154: continue
    sx, sz = corner[k % 4]
    g = post(col(s), row(s), sx * (CARPET_HALF + 3), sz * (CARPET_HALF + 3), SOLDIER, 0, 6.5, s)
    if g: guards.append(g)
for which, s in enumerate([152, 163]):
    # (the first of the crossing's four corners with room for him, from a different one in each room)
    ways = [corner[(which + 2 + t) % 4] for t in range(4)]
    sx, sz = next((w for w in ways if free(col(s) + w[0] * (CARPET_HALF + 10), row(s) + w[1] * (CARPET_HALF + 9))), ways[0])
    g = post(col(s), row(s), sx * (CARPET_HALF + 10), sz * (CARPET_HALF + 9), PIG, which, 1.0, s)
    if g: guards.append(g)
# (the long hall: soldiers by its carpet either side, by turns, facing across it)
hx = col(163)
for k, hz in enumerate([hall['z1'] - 52, hall['z1'] - 82, hall['z1'] - 112, hall['z1'] - 142]):
    side = 1 if k % 2 == 0 else -1
    g = post(hx, hz, side * (CARPET_HALF + 4), 0.01, SOLDIER, 0, 7.0, 80)
    if g: guards.append(g)
print('guards:', len(guards), '--', ', '.join(f'{["soldier", "pighead", "Stan"][g[0]]} at ({g[2]:.1f}, {g[3]:.1f})' for g in guards))

# ---- checks
def check(lo, hi, what):
    bad = [i for i in range(lo, hi) if blocked(pts[i][0], pts[i][1], h[i] - DECK, max(0.5, half[i] - 1.25 + CAR_BOX))]
    if bad: print(f'  {what}: {len(bad)} points blocked, e.g. {[tuple(np.round(pts[i], 1)) for i in bad[:8]]}')
    else: print(f'  {what}: clear')
    return bad
def check_floor(line, what):
    bad = [p for p in line if blocked(p[0], p[1], FLOOR, CAR_HALF)]
    if bad: print(f'  {what}: {len(bad)} points blocked, e.g. {[tuple(np.round(p, 1)) for p in bad[:8]]}')
    else: print(f'  {what}: clear ({len(line)} points)')
    return bad
print('route', n, 'points,', round(S[-1]), 'cells; start', start, 'the hall', i_hall, 'window', i_window_in, i_window_out, 'the leap', lip, land, land_foot)
check_floor(pts[:i_ramp_foot], 'the rooms and the hall (the route)')
for k, rt in enumerate(routes): check_floor(rt, f'the rooms (way {k + 1})')
check(i_ramp_foot, lip + 1, 'the ramp and the window')
grade = np.abs(np.diff(h)) / (SP * 512)
print('steepest: the ramp to the window %.2f' % grade[i_ramp_foot:i_window_in].max())
# (the leap over the ground: the least clearance under it, and under the stretch of road beyond -- the hill's arms either side)
low = min((h[i] - DECK - max(ground(pts[i][0] + dx, pts[i][1]) for dx in (-HALF_OUT - 3, 0, HALF_OUT + 3)), i) for i in range(lip, n))
print('the leap: lip %.0f at %s, its top %.0f, down at %s; %.1f cells; the least clearance over the ground under it and beyond %.0f at %s' %
      (h[lip], tuple(np.round(pts[lip], 1)), h[lip:land + 1].max(), tuple(np.round(pts[land], 1)), S[land] - S[lip], low[0], tuple(np.round(pts[low[1]], 1))))
print('the parts: the rooms and the hall %.0f cells, the ramp and the window %.0f, the leap %.0f, the road beyond %.0f' %
      (S[i_ramp_foot], S[lip] - S[i_ramp_foot], S[land] - S[lip], S[-1] - S[land]))
print('the opponents\' ways through the rooms: ' + ', '.join('%.0f cells' % (len(rt) - 1) for rt in routes) + ' (the route %.0f to the same place)' % S[i_hall])
print('about %.0f s to the window at the top speed (34 km/h, 9.4 cells a second)' % (S[lip] / 9.44))

plan = {
    'originCellX': 0, 'originCellZ': 0,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in pts],
    'heights': [round(float(v), 1) for v in h],
    'open': True, 'start': start, 'finish': lip,
    'maxGrade': 2.0,
    'asphaltHalf': 3.0, 'curbHalf': 3.5, 'vergeHalf': 4.0, 'blend': 2.0,
    'raised': [0, n - 1], 'raisedHalf': HALF_OUT,
    'raisedHalfs': [round(float(v), 2) for v in half],
    # (no road through the rooms and up the long hall to the ramp: the cars on the floor, where they like; nor where the leap comes down --
    # never reached: a win's teleporter takes the car in mid-air, a loss's race again from the grid starts before it lands -- a stretch of
    # road on a pier out in the haze)
    'bare': [[0, i_ramp_foot - 1], [land, n - 1]],
    'openAreas': [[sq_x0, sq_z0, sq_x1, sq_z1, FLOOR], [hall['x0'], hall['z0'], hall['x1'] + 1, hall['z1'] + 1, FLOOR]],
    'floorArrows': [[round(float(x), 2), round(float(z), 2), FLOOR, dx, dz] for x, z, dx, dz in arrows],
    'openRoutes': [[[round(float(x), 2), round(float(z), 2)] for x, z in rt] for rt in routes],
    'guards': guards,
    # (the leap out of the window: its ramp the ramp up to the sill; the camera behind the car)
    'arcJumps': [[i_ramp_foot, lip, land, land_foot, 0]],
    # (the camera rides the road behind the car, low -- 9 cells behind, 2,100 over the road, looking 7 cells ahead at a point 1,000 over
    # it, its sight line to the car's middle 500 over the road; in the rooms, where there is no road, it follows the car's own trail, as
    # low -- RACEMOD.CPP OpenCamera)
    'railCamera': [9, 2100, 7, 500, 1000],
    'themes': [{'from': 0, 'to': lip, 'theme': 'carpet'}, {'from': lip, 'to': n - 1, 'theme': 'palace'}],
    # (the palace -- its rooms, roofs, furniture and domes -- stays where it is under and beside the road: every decor of the island's own
    # bodies on, PALACE.OBL's from 151; and nothing is cut into the ground)
    'keepAbove': 0,
}
json.dump(plan, open(OUT, 'w'))
print('wrote', OUT)
