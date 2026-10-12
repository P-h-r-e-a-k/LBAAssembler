"""The Emperor's palace race (the user, 2026-10-12: "through the palace rooms, coming out the window and around the roof tops?
Point-to-point i.e. no laps, finishing with massive jump to harbor teleport? - Twinsen, quick, Dark monk's in the palace, he's killing
everybody. Leading into final showdown at Celebration"), on the Palace island (Terrain/Palace: PALACE.ILE, its layout PALACE.JSON).

A sprint (the plan's open), one raised road from end to end:
  - the start in the sixteen rooms' south-east room (154), the grid behind the line; west through the bottom row of rooms (153, 152, 151)
    door by door, north up the west column (155, 159, 163) and on into the long hall (80) -- a red carpet on the floors, two bricks'
    doors a little wider than it;
  - north down the long hall to its end, a ramp up to the round window Twinsen leaves by, and out of it over the cliff's edge;
  - a road out over the cloud sea, round to the east and climbing to the hilltop, up onto it along the way across its rocks north-east
    of the long hall (PalaceIsland's corridor) to the sixteen rooms' roof;
  - in among the palace's domes on the rooftops: the north-east hall ahead down the way across the rocks, a swing west into the
    courtyard under the big dome, back east along the north-east hall's north wall and south down its east side;
  - east along the runway to the cliff's edge, and the jump out over the harbour into the Dark Monk's teleporter on the pillar in the bay:
    the finish line at the ramp's lip, the teleporter's flash in mid-flight (the race-track mode's sprint, wake=, wake_flight=).

Usage: palace_design.py <game folder with PALACE.ILE and PALACE.JSON> [out plan]
"""
import json, math, sys
import numpy as np
sys.path.insert(0, __file__.rsplit('\\', 1)[0] if '\\' in __file__ else '.')
from ile import load_ile

GAME = sys.argv[1] if len(sys.argv) > 1 else r'E:\dump\TEMP\elev\lba2'
OUT = sys.argv[2] if len(sys.argv) > 2 else r'E:\dump\LBAAssembler\docs\racetrack\palace_track_plan.json'
L = json.load(open(GAME + r'\PALACE.JSON'))
_, _, _, CUBES = load_ile(GAME + r'\PALACE.ILE')
m_raw = open(GAME + r'\PALACE.ILE', 'rb').read()
SP = 0.5                                    # the plan's points, cells apart
FLOOR = L['floorTop']                       # what a car drives on in the rooms
TOP = L['hilltop']                          # the hilltop: the roofs' top
DECK = 60                                   # the raised road over what it lies on (a carpet indoors, a road on the roofs)
RAMP_UP = 700                               # the jump's ramp
JUMP_APEX = 2600                            # the flight's top over the lip

# ---- where things are
rooms = {r['scene']: r for r in L['rooms']}
def mid(scene):
    r = rooms[scene]; return ((r['x0'] + r['x1'] + 1) / 2, (r['z0'] + r['z1'] + 1) / 2)
win = L['window']
wx = (win['x0'] + win['x1']) / 2            # the round window's middle across, the wall's inner face and outer
w_in, w_out = win['z1'], win['z0']
sill = win['sill']
hall = rooms[80]
px, pz, pr, ptop = L['pillar']
runway = L['corridors'][0]['points']
landing = L['corridors'][1]['points']

# ---- the boxes: what a car hits, by height (decors with a real box; world units)
boxes = []
from ile import hqr_entries
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


# ---- the route's control points (cells), part by part, and the height each part runs at
s154, s153, s152, s151 = mid(154), mid(153), mid(152), mid(151)
s155, s159, s163 = mid(155), mid(159), mid(163)
hx = (hall['x0'] + hall['x1'] + 1) / 2
# (the doors between the rooms are two bricks -- 8 cells -- wide, 2 cells past each room's middle: the bottom row's along z = ROW, the west
# column's along x = COL; room 154's chest stands on its middle line east of its door, so the grid is south of it, the road swinging up to
# the door)
ROW, COL = s154[1] + 2, s151[0] + 2.5
# (the start line in the east half of room 154, in the cube east of its middle -- the cube's edge is at x 512: the grid, 14 cells of it,
# behind the line in the same cube, the start's scene)
START_X = 514
rooms_ctrl = [
    (rooms[154]['x1'] - 6, ROW + 10),          # the route's start, behind the grid, south of room 154's chest
    (START_X, ROW + 10), (START_X - 9, ROW + 9.5), (START_X - 16, ROW + 6), (START_X - 22, ROW + 1.5), (START_X - 26, ROW), (rooms[154]['x0'] + 4, ROW),
    (s153[0], ROW), (s152[0], ROW),
    (COL + 9, ROW), (COL + 1, ROW - 6), (COL, ROW - 15),
    (COL, s155[1]), (COL, s159[1]), (COL, s163[1]),
    (COL, hall['z1'] - 6), (COL, (hall['z0'] + hall['z1']) / 2), (wx, w_in + 40),
]
# (the ramp up to the round window: on the sill's height before the step it stands on -- a brick deep, as high as the sill -- level over
# it and through the window)
ramp_ctrl = [(wx, w_in + 30), (wx, w_in + 6), (wx, w_in), (wx, w_out - 2)]
# (out over the cloud sea: north from the window, round to the east, and south onto the hilltop at the way across the rocks)
lx = landing[-1][0]                         # (the way across the rocks: north to south, at this x)
bridge_ctrl = [(wx, w_out - 20), (wx + 10, w_out - 40), (wx + 28, w_out - 50), ((wx + lx) / 2, w_out - 52),
               (lx - 22, w_out - 48), (lx - 6, w_out - 34), (lx, w_out - 12), (lx, w_out + 40), (lx, w_out + 80)]
sq_z0 = rooms[166]['z0']; sq_z1 = rooms[151]['z1'] + 1; sq_x0 = rooms[151]['x0']; sq_x1 = rooms[154]['x1'] + 1
# (the rooftops, among the domes -- the cluster over the sixteen rooms' middle, so its halls are where they are from it, cells: the way
# across the rocks comes down in line with the north-east hall (x 455-488, z 551-590 here); the road swings west into the courtyard
# north of it, under the big dome's hall (to x 427), back east along the north-east hall's north wall, south down its east side, past the
# small east dome (from z 600) and east onto the runway)
cx, cz = (sq_x0 + sq_x1) / 2, (sq_z0 + sq_z1) / 2
roof_ctrl = [(lx, sq_z0 - 22), (lx, sq_z0 + 10), (lx - 4, sq_z0 + 26), (cx + 22, sq_z0 + 40), (cx + 18, sq_z0 + 51),
             (cx + 30, sq_z0 + 59), (cx + 52, sq_z0 + 59), (cx + 68, sq_z0 + 67), (cx + 73, sq_z0 + 82),
             (cx + 74, runway[0][1] - 16), (cx + 80, runway[0][1] - 5), (cx + 92, runway[0][1]), (sq_x1 + 6, runway[0][1])]
CTRL = rooms_ctrl + ramp_ctrl + bridge_ctrl + roof_ctrl


def catmull(pts, sp):
    P = np.array(pts, float)
    P = np.vstack([P[0] * 2 - P[1], P, P[-1] * 2 - P[-2]])
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        n = max(2, int(np.ceil(np.linalg.norm(p2 - p1) / sp * 4)))
        for k in range(n):
            t = k / n
            out.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(P[-2])
    out = np.array(out)
    d = np.r_[0, np.cumsum(np.linalg.norm(np.diff(out, axis=0), axis=1))]
    s = np.arange(0, d[-1], sp)
    return np.stack([np.interp(s, d, out[:, 0]), np.interp(s, d, out[:, 1])], 1), s


# the route to the runway's end, then the jump: straight on east over the harbour to the pillar's middle
pts, S = catmull(CTRL, SP)
lip_x = None
# (the lip: where the runway reaches the cliff's edge -- the ground falling away from the hilltop)
for x in np.arange(sq_x1, px, 0.5):
    if ground(x, runway[0][1]) < TOP - 400: lip_x = x - 2; break
jump_pts = np.array([(x, runway[0][1]) for x in np.arange(pts[-1][0] + SP, px + 2 + SP / 2, SP)])
pts = np.vstack([pts, jump_pts])
n = len(pts)
S = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]


def nearest(p, lo=0):
    return lo + int(np.argmin(np.hypot(pts[lo:, 0] - p[0], pts[lo:, 1] - p[1])))


i_window_in = nearest((wx, w_in)); i_window_out = nearest((wx, w_out))
i_ramp_foot = nearest((wx, w_in + 30)); i_ramp_top = nearest((wx, w_in + 6))
# (the climb done before the hill's edge, where the way across the rocks begins: the first place south along it the ground is the hilltop's)
edge_z = next(z for z in np.arange(w_out - 30, sq_z0, 0.5) if ground(lx, z) > TOP - 300)
i_bridge_end = nearest((lx, edge_z - 8), i_window_out)
print("the hill's edge along the way across the rocks at z %.1f" % edge_z)
i_roof = nearest((lx, sq_z0 - 22), i_window_out)
foot = nearest((lip_x - 14, runway[0][1]), i_roof); lip = nearest((lip_x, runway[0][1]), foot)
land = nearest((px - pr + 1, pz), lip); land_foot = n - 1
start = nearest((START_X, ROW + 10))

# ---- heights: the carpet over the rooms' floor; the ramp up to the window's sill; the road out over the sea climbing to the hilltop; the
# rooftops; the jump's ramp, flight and landing on the pillar (as high as its lip)
h = np.full(n, FLOOR + DECK, float)
for i in range(i_ramp_foot, i_ramp_top + 1):
    t = (i - i_ramp_foot) / max(1, i_ramp_top - i_ramp_foot); h[i] = FLOOR + DECK + (sill + DECK - FLOOR - DECK) * (t * t * (3 - 2 * t))
h[i_ramp_top:i_window_out + 1] = sill + DECK
for i in range(i_window_out, i_bridge_end + 1):
    t = (i - i_window_out) / max(1, i_bridge_end - i_window_out); h[i] = sill + DECK + (TOP + DECK - sill - DECK) * (t * t * (3 - 2 * t))
h[i_bridge_end:] = TOP + DECK
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

# ---- widths (cells from the road's middle to its rail): the rooms' doors are two bricks, 8 cells -- the carpet 7 across, half a cell in from
# each side; outside wider
half = np.full(n, 4.5)
half[:i_window_out + 1] = 3.5
# (the grid: two cars abreast in room 154 -- wider round the start)

# ---- checks
def check(lo, hi, what):
    bad = [i for i in range(lo, hi) if blocked(pts[i][0], pts[i][1], h[i] - DECK, max(0.5, half[i] - 1.25))]
    if bad: print(f'  {what}: {len(bad)} points blocked, e.g. {[tuple(np.round(pts[i], 1)) for i in bad[:6]]}')
    else: print(f'  {what}: clear')
    return bad
print('route', n, 'points,', round(S[-1]), 'cells; start', start, 'window', i_window_in, i_window_out, 'bridge to', i_bridge_end,
      'roof from', i_roof, 'jump', foot, lip, land, land_foot)
check(0, i_ramp_foot, 'the rooms and the hall')
check(i_ramp_foot, i_window_out + 1, 'the ramp and the window')
check(i_roof, foot, 'the rooftops')
grade = np.abs(np.diff(h)) / (SP * 512)
print('steepest: the ramp to the window %.2f, the road out to the hilltop %.3f' % (grade[i_ramp_foot:i_window_in].max(), grade[i_window_out:i_bridge_end].max()))
print('the jump: lip %.0f at %s, landing %.0f at %s, the flight\'s top %.0f; %.1f cells' %
      (h[lip], tuple(np.round(pts[lip], 1)), h[land], tuple(np.round(pts[land], 1)), h[lip:land + 1].max(), S[land] - S[lip]))
under = [ground(*pts[i]) for i in range(i_window_out, i_bridge_end)]
print('the road out over the sea: the ground under it %.0f to %.0f, the road %.0f to %.0f' % (min(under), max(under), h[i_window_out], h[i_bridge_end]))
# (the road over the ground, from the window to the rooftops: the least clearance, the cross under it as well as its middle)
low = min((h[i] - DECK - max(ground(pts[i][0] + dx, pts[i][1] + dz) for dx, dz in ((0, 0), (half[i], 0), (-half[i], 0), (0, half[i]), (0, -half[i]))), i)
          for i in range(i_window_out + 4, i_roof))
print('the least clearance over the ground from the window to the rooftops: %.0f at %s (point %d)' % (low[0], tuple(np.round(pts[low[1]], 1)), low[1]))
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
    if len(shown) == 8: break
    if all(np.hypot(*(pts[i] - pts[j])) > 10 for _, j in shown): shown.append((r, i))
print('tightest turns (radius in cells at point):', ', '.join('%.1f at %s' % (r, tuple(np.round(pts[i], 1))) for r, i in shown))

plan = {
    'originCellX': 0, 'originCellZ': 0,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in pts],
    'heights': [round(float(v), 1) for v in h],
    'open': True, 'start': start, 'finish': lip,
    'maxGrade': 2.0,
    'asphaltHalf': 3.0, 'curbHalf': 3.5, 'vergeHalf': 4.0, 'blend': 2.0,
    'raised': [0, n - 1], 'raisedHalf': 4.5,
    'raisedHalfs': [round(float(v), 2) for v in half],
    'arcJumps': [[foot, lip, land, land_foot, 0]],
    # (the camera rides the road behind the car, low: through the rooms' doors -- their lintels 4,400 over the carpet -- and under their
    # roofs; the follow camera's arm put it over the roofs, the rooms seen through their rock -- cells behind, units over the road, cells
    # ahead it looks at; its sight line to the car's middle, 500 over the road, so the eye stays that low -- at the car's wheels it rose to
    # 4,000 and looked steeply down, the rooms' walls and the domes out of the picture -- and looking at a point 1,000 over the road ahead:
    # a level view, the car in the picture's lower third; 9 cells behind, so round the tightest bends -- the eye comes as near as 5 cells --
    # the car is still past the near plane, 3,000)
    'railCamera': [9, 2100, 7, 500, 1000],
    'themes': [{'from': 0, 'to': i_window_out, 'theme': 'carpet'}, {'from': i_window_out, 'to': n - 1, 'theme': 'palace'}],
    # (the palace -- its rooms, roofs, furniture and domes -- stays where it is under and beside the road: every decor of the island's own
    # bodies on, PALACE.OBL's from 151; and nothing is cut into the ground)
    'keepAbove': 0,
}
json.dump(plan, open(OUT, 'w'))
print('wrote', OUT)
