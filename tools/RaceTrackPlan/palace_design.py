"""The Emperor's palace race (the user, 2026-10-12: "through the palace rooms, coming out the window and around the roof tops?
Point-to-point i.e. no laps, finishing with massive jump to harbor teleport? - Twinsen, quick, Dark monk's in the palace, he's killing
everybody. Leading into final showdown at Celebration"), on the Palace island (Terrain/Palace: PALACE.ILE, its layout PALACE.JSON).

Second version, the same day (the user: "The palace track is far too narrow ... Let's make the inside tracks wider if we can do, and
perhaps loop in more rooms. Add a section where we're outside on the roof looping around the palace roof tops"): the island's doors are
four bricks wide now (16 cells: PalaceIsland.DoorBricks), the road 12 cells wide in the rooms and 14 outside.

Third version, the same day (the user: "For our palace track let's scale up the roof top so it covers more of the outside of the palace
area. For the inside of the palace let's remove the track but add some arrows to allow cars to take multiple directions, it has a single
entry and exit area so all cars will ultimately end up back on the same track"): the hilltop is flat over the corner of the rooms' L and
24 cells round them now (PalaceCliff.Terrace), and the race a sprint (the plan's open):
  - in the sixteen rooms no road at all (the plan's bare): the cars drive the rooms' floor -- the race-track mode keeps them on it, its
    open_area= (the plan's openAreas) -- from the start in the south-east room (154) to the north-west one (163) and out of its north door
    into the long hall, each room's ways on west and north shown by arrows on its floor (floorArrows), any way through the rooms' doors
    (all four of a room's walls have one). The route itself, the test pilot's: west along the bottom row (153, 152, 151) and north up the
    west column (155, 159, 163); the opponents after the first each their own way (openRoutes, in turn): north up the east column and
    west along the top row; a staircase from 153, north and west by turns; another from 158 (for a fourth opponent);
  - north up the long hall, a ramp up to the round window Twinsen leaves by, and out of it over the cliff's edge;
  - out over the cloud sea, north and round to the east, climbing, onto the hilltop east of the long hall;
  - round the roof top: south-east along the north-east coast, back west in a hairpin along the sixteen rooms' north side and over the long
    hall's roof between its towers and the domes, south down the west side, east along the south side, north up the sixteen rooms' roof's
    east side between the domes and its edge, and east along the runway to the cliff's edge;
  - the jump out over the harbour into the Dark Monk's teleporter on the pillar in the bay: the finish line at the ramp's lip, the
    teleporter's flash in mid-flight (the race-track mode's sprint, wake=, wake_flight=).

Usage: palace_design.py <game folder with PALACE.ILE and PALACE.JSON: the island built on its own, `ScriptRoundTrip palace`> [out plan]
"""
import json, math, sys
import numpy as np
sys.path.insert(0, __file__.rsplit('\\', 1)[0] if '\\' in __file__ else '.')
from ile import load_ile, hqr_entries

GAME = sys.argv[1] if len(sys.argv) > 1 else r'E:\dump\TEMP\palace3\v4'
OUT = sys.argv[2] if len(sys.argv) > 2 else r'E:\dump\LBAAssembler\docs\racetrack\palace_track_plan.json'
L = json.load(open(GAME + r'\PALACE.JSON'))
_, _, _, CUBES = load_ile(GAME + r'\PALACE.ILE')
SP = 0.5                                    # the plan's points, cells apart
FLOOR = L['floorTop']                       # the rooms' floor: what the cars drive on in them
TOP = L['hilltop']                          # the hilltop: the roofs' top
DECK = 60                                   # the raised road over what it lies on
RAMP_UP = 700                               # the jump's ramp
JUMP_APEX = 2600                            # the flight's top over the lip
HALF_IN, HALF_OUT = 5.5, 7.0                # the road's half width (cells): up to the round window (16 cells across), outside
# (a car's collision box: the engine keeps its middle RAISED_INSET -- 1.25 cells -- inside the rail, but the box it collides with is
# square, some 1,420 units either way of its middle whichever way it faces: 2.8 cells. So nothing may stand within 1.55 cells past the
# road's edge; and where the cars drive the floor, nothing within CAR_HALF of a line they follow)
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
px, pz, pr, ptop = L['pillar']
runway = L['corridors'][0]['points']
ring = L['corridors'][1]['points']          # the way round the palace's west and south sides
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


def ease(t, a=0.12):
    """0 to 1 over t 0..1, its slope rising evenly over the first `a` of it, steady, and falling evenly over the last `a`."""
    t = min(max(t, 0.0), 1.0); top = 1 / (1 - a)
    if t < a: return top * t * t / (2 * a)
    if t > 1 - a: return 1 - top * (1 - t) ** 2 / (2 * a)
    return top * (t - a / 2)


# ---- the rooms: the rows' door lines (z) and the columns' (x), a room's middle where its two cross
R0, R1, R2, R3 = row(154), row(158), row(162), row(166)    # the bottom row's ... the top row's
C0, C1, C2, C3 = col(151), col(152), col(153), col(154)    # the west column's ... the east column's
# (the start line in the east half of room 154, in the cube east of its middle -- the cube's edge is at x 512: the grid, 14 cells of it,
# behind the line in the same cube, the start's scene; room 154's chest stands on its row's door line east of the room's middle, so the
# grid is south of it, the way on west swinging up to the row's door line)
START_X = 514
GRID_Z = R0 + 11
BEHIND = rooms[154]['x1'] - 6
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
    if s in (154,):
        # (the start's room: the grid south of its door line -- the arrows where the cars pass, swinging up to it, or turning north)
        arrows += [(cx - 16, cz + 3, -1, 0), (cx, cz - 10, 0, -1)]
        continue
    if cx > C0: arrows.append((cx - 12, cz, -1, 0))
    if cz > R3 or s == 163: arrows.append((cx, cz - 12, 0, -1))
arrows += [(C0, hall['z1'] - 40, 0, -1), (C0, (hall['z0'] + hall['z1']) / 2 + 20, 0, -1)]

# ---- the route's corners (cells) and their radii, part by part
# (out of the round window north over the cloud sea, round to the east -- two quarter circles -- and south onto the hilltop east of the
# long hall: its climb from the window's sill to the hilltop that long, TOP_N the turn's top north of the window)
LX = 394                                                    # the way south onto the hilltop (x): east of the hall's towers, west of the rim
U = (LX - wx) / 2
TOP_N = 92
# (south-east along the north-east coast: the line of the rooms' outline from the long hall's north-east corner to the sixteen rooms',
# NE_OUT cells out from it -- the hilltop's flat 24 cells out)
hx, hz = hall['x1'] + 1, hall['z0']
ex, ez = sq_x1, sq_z0
dirx, dirz = (ex - hx) / math.hypot(ex - hx, ez - hz), (ez - hz) / math.hypot(ex - hx, ez - hz)
NE_OUT = 11
nx0, nz0 = hx + dirz * NE_OUT, hz - dirx * NE_OUT           # (the line's start, out from the hall's corner)
def on_ne(x=None, z=None):
    """The point of the north-east line at x, or at z."""
    s = (x - nx0) / dirx if x is not None else (z - nz0) / dirz
    return (nx0 + dirx * s, nz0 + dirz * s)
ROW_N = sq_z0 - 11                                          # back west along the sixteen rooms' north side (z) ...
W_X = sq_x0 - 11                                            # ... south down the west side (x) ...
S_Z = sq_z1 + 7                                             # ... east along the south side (z) ...
E_X = sq_x1 - 9                                             # ... north up the sixteen rooms' roof's east side (x)
RZ = runway[0][1]                                           # ... and east along the runway (z)
corners = GRID + [(C0, R0),                                 # west along the bottom row, north up the west column, the hall, the window
                  (wx, w_out - TOP_N), (LX, w_out - TOP_N), # north over the cloud sea, round to the east
                  on_ne(x=LX),                              # south onto the hilltop, south-east along the coast
                  on_ne(z=ROW_N),                           # the hairpin back west
                  (W_X, ROW_N), (W_X, S_Z), (E_X, S_Z), (E_X, RZ),
                  (px + 2, RZ)]                             # east along the runway, the jump to the pillar
radii = GRID_R + [TURN, U, U, 30, 13, 16, 14, 14, 8, 0]
pts = path(corners, radii)
n = len(pts)
S = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]


def nearest(p, lo=0, hi=None):
    hi = n if hi is None else hi
    return lo + int(np.argmin(np.hypot(pts[lo:hi, 0] - p[0], pts[lo:hi, 1] - p[1])))


lip_x = None
# (the lip: where the runway reaches the cliff's edge -- the ground falling away from the hilltop)
for x in np.arange(sq_x1, px, 0.5):
    if ground(x, RZ) < TOP - 400: lip_x = x - 2; break
start = nearest((START_X, GRID_Z), 0, 200)
i_ramp_foot = nearest((wx, w_in + 30)); i_ramp_top = nearest((wx, w_in + 8), i_ramp_foot)
i_window_in = nearest((wx, w_in), i_ramp_foot); i_window_out = nearest((wx, w_out), i_window_in)
# (the climb done before the hilltop's edge, where the way south onto it meets the ground: LAND_EARLY cells north of the first place
# the ground under it -- its middle and its edges -- is near the hilltop's)
i_turned = nearest((LX, w_out - TOP_N + U), i_window_out)
LAND_EARLY = 8
i_edge = next(i for i in range(i_turned, n) if max(ground(pts[i][0] + dx, pts[i][1]) for dx in (-HALF_OUT, 0, HALF_OUT)) > TOP - 600)
i_land = i_edge - int(LAND_EARLY / SP)
foot = nearest((lip_x - 14, RZ), i_land); lip = nearest((lip_x, RZ), foot)
land = nearest((px - pr + 1, pz), lip); land_foot = n - 1
i_hall = nearest(HALL_JOIN, 0, i_ramp_foot)
print("the route: the hilltop's edge on the way south onto it at z %.1f (point %d)" % (pts[i_edge][1], i_edge))

# ---- heights: the rooms' floor (no road there); the ramp up to the window's sill; the road out over the sea climbing to the hilltop;
# round the roof top; the jump's ramp, flight and landing on the pillar (as high as its lip)
h = np.full(n, TOP + DECK, float)
h[:i_ramp_foot] = FLOOR
for i in range(i_ramp_foot, i_ramp_top + 1):
    t = (i - i_ramp_foot) / max(1, i_ramp_top - i_ramp_foot); h[i] = FLOOR + (sill + DECK - FLOOR) * (t * t * (3 - 2 * t))
h[i_ramp_top:i_window_out + 1] = sill + DECK
for i in range(i_window_out, i_land + 1):
    h[i] = sill + DECK + (TOP - sill) * ease((S[i] - S[i_window_out]) / (S[i_land] - S[i_window_out]))
base = TOP + DECK
for i in range(foot, lip + 1):
    t = (i - foot) / max(1, lip - foot); h[i] = base + RAMP_UP * t * t
lipH = base + RAMP_UP
landH = max(lipH, ptop + DECK)
ts = np.linspace(0, 1, 201)
bump = next(b for b in np.arange(0, 20000, 50) if (lipH + (landH - lipH) * ts + b * 4 * ts * (1 - ts)).max() >= lipH + JUMP_APEX)
for i in range(lip, land + 1):
    t = (i - lip) / max(1, land - lip); h[i] = lipH + (landH - lipH) * t + bump * 4 * t * (1 - t)
h[land:] = landH

# ---- widths (cells from the road's middle to its rail): up to the round window as wide as it allows, outside wider, easing from one to
# the other over the road out of the window; the grid's places by room 154's south wall two abreast. (In the rooms there is no road: the
# width only places the grid and the scenes)
half = np.full(n, HALF_OUT)
half[:i_window_out + 1] = HALF_IN
for i in range(i_window_out, min(n, i_window_out + 40)):
    half[i] = HALF_IN + (HALF_OUT - HALF_IN) * (i - i_window_out) / 40
HALF_GRID = 3.5
for i in range(0, n):
    if i <= start + 6: half[i] = HALF_GRID
    elif i <= start + 26: half[i] = HALF_GRID + (HALF_IN - HALF_GRID) * (i - start - 6) / 20

# ---- the opponents' ways through the rooms, a cell apart (RaceTrackBuilder.RouteThrough)
routes = [path(c, r, 1.0) for c, r in open_routes]

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
print('route', n, 'points,', round(S[-1]), 'cells; start', start, 'the hall', i_hall, 'window', i_window_in, i_window_out, 'on the hilltop from', i_land,
      'jump', foot, lip, land, land_foot)
check_floor(pts[:i_ramp_foot], 'the rooms and the hall (the route)')
for k, rt in enumerate(routes): check_floor(rt, f'the rooms (way {k + 1})')
check(i_ramp_foot, i_window_out + 1, 'the ramp and the window')
check(i_window_out, foot, 'out of the window and round the roof top')
grade = np.abs(np.diff(h)) / (SP * 512)
print('steepest: the ramp to the window %.2f, the road out to the hilltop %.3f (%.0f cells of it)' %
      (grade[i_ramp_foot:i_window_in].max(), grade[i_window_out:i_land].max(), S[i_land] - S[i_window_out]))
print('the jump: lip %.0f at %s, landing %.0f at %s, the flight\'s top %.0f; %.1f cells' %
      (h[lip], tuple(np.round(pts[lip], 1)), h[land], tuple(np.round(pts[land], 1)), h[lip:land + 1].max(), S[land] - S[lip]))
# (the road over the ground, from the window round the roof top: the least clearance, the cross under it as well as its middle)
low = min((h[i] - DECK - max(ground(pts[i][0] + dx, pts[i][1] + dz) for dx, dz in ((0, 0), (half[i], 0), (-half[i], 0), (0, half[i]), (0, -half[i]))), i)
          for i in range(i_window_out + 4, foot))
print('the least clearance over the ground from the window round the roof top: %.0f at %s (point %d)' % (low[0], tuple(np.round(pts[low[1]], 1)), low[1]))
# (two parts of the road side by side on the roof top: the least gap between their rails)
gap = min((np.hypot(*(pts[i] - pts[j])) - half[i] - half[j], i, j) for i in range(i_land, foot, 4) for j in range(i + 120, foot, 4))
print('the least gap between two parts of the road on the roof top: %.1f cells (%s and %s)' % (gap[0], tuple(np.round(pts[gap[1]], 1)), tuple(np.round(pts[gap[2]], 1))))
k = 8
rad = []
for i in range(k, n - k):
    a, b, c = pts[i - k], pts[i], pts[i + k]
    ab, bc, ca = np.linalg.norm(b - a), np.linalg.norm(c - b), np.linalg.norm(a - c)
    area = abs((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])) / 2
    rad.append((ab * bc * ca / (4 * area) if area > 1e-6 else 1e9, i))
rad.sort()
shown = []
for r, i in rad:
    if len(shown) == 10: break
    if all(np.hypot(*(pts[i] - pts[j])) > 10 for _, j in shown): shown.append((r, i))
print('tightest turns (radius in cells at point):', ', '.join('%.1f at %s' % (r, tuple(np.round(pts[i], 1))) for r, i in shown))
print('the parts: the rooms and the hall %.0f cells, out of the window to the hilltop %.0f, round the roof top %.0f, the runway %.0f' %
      (S[i_ramp_foot], S[i_land] - S[i_window_out], S[foot] - S[i_land], S[lip] - S[foot]))
print('the opponents\' ways through the rooms: ' + ', '.join('%.0f cells' % (len(rt) - 1) for rt in routes) + ' (the route %.0f to the same place)' % S[i_hall])
print('about %.0f s at the top speed (34 km/h, 9.4 cells a second)' % (S[lip] / 9.44))

plan = {
    'originCellX': 0, 'originCellZ': 0,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in pts],
    'heights': [round(float(v), 1) for v in h],
    'open': True, 'start': start, 'finish': lip,
    'maxGrade': 2.0,
    'asphaltHalf': 3.0, 'curbHalf': 3.5, 'vergeHalf': 4.0, 'blend': 2.0,
    'raised': [0, n - 1], 'raisedHalf': HALF_OUT,
    'raisedHalfs': [round(float(v), 2) for v in half],
    # (no road through the rooms and up the long hall to the ramp: the cars on the floor, where they like)
    'bare': [[0, i_ramp_foot - 1]],
    'openAreas': [[sq_x0, sq_z0, sq_x1, sq_z1, FLOOR], [hall['x0'], hall['z0'], hall['x1'] + 1, hall['z1'] + 1, FLOOR]],
    'floorArrows': [[round(float(x), 2), round(float(z), 2), FLOOR, dx, dz] for x, z, dx, dz in arrows],
    'openRoutes': [[[round(float(x), 2), round(float(z), 2)] for x, z in rt] for rt in routes],
    'arcJumps': [[foot, lip, land, land_foot, 0]],
    # (the camera rides the road behind the car, low, outside -- 9 cells behind, 2,100 over the road, looking 7 cells ahead at a point
    # 1,000 over it, its sight line to the car's middle 500 over the road: a level view, the car in the picture's lower third; in the
    # rooms, where there is no road, it follows the car's own trail, as low -- RACEMOD.CPP OpenCamera)
    'railCamera': [9, 2100, 7, 500, 1000],
    'themes': [{'from': 0, 'to': i_window_out, 'theme': 'carpet'}, {'from': i_window_out, 'to': n - 1, 'theme': 'palace'}],
    # (the palace -- its rooms, roofs, furniture and domes -- stays where it is under and beside the road: every decor of the island's own
    # bodies on, PALACE.OBL's from 151; and nothing is cut into the ground)
    'keepAbove': 0,
}
json.dump(plan, open(OUT, 'w'))
print('wrote', OUT)
