"""The Temple of Bú's Halloween race (the user, 2026-10-16: "Port the temple of Bu from LBA1 into LBA2 as a separate island ... After it's been
ported generate a Halloween inspired race track for it. This will be a separate release for a Halloween special track before our full racing
mod is released. Let's make it as creepy, scary, and Halloween inspired as we can do, let's turn our mushroom pickups into mini pumpkins. Add
lots of typical Halloween decor, pumpkins, witches, ghosts etc"), on the temple island (Terrain/Temple: TEMPLE.ILE, its layout TEMPLE.JSON).

LBA1's temple is a dungeon of three scenes at three heights -- the first floor (scene 8) and its long corridor east, 14,080 up; a lower hall in
scene 8, 11,008; the crypt (scene 40's rooms), 1,792 -- joined in LBA1 by narrow stairs, with dead ends and no way round. The lap is a raised
road (the race-track mode's, held on its rails) a little over the floors, through doorways cut in the walls where it passes them (the plan's
templeCuts: TempleObjects leaves those cells out), and between the heights on ramps -- and, down to the crypt and back up, on spirals through
the black void round the temple:
  - the start straight west along the long corridor;
  - north into the first floor's south side, down a ramp into the lower hall and round it, up into the first floor's west rooms and round
    them;
  - out over the first floor's open south edge into the dark, and down a spiral to the crypt;
  - through the crypt's three rooms -- west to east along their bottom, north through the east room;
  - out of its east side into the dark, up a spiral to the long corridor's east end: the start straight.
The start and finish line is on the straight through the crypt's west room, a slip lane beside it where the opponents wait while Twinsen
drives his qualifying lap.

Usage: temple_design.py <game folder with TEMPLE.ILE and TEMPLE.JSON: the island built on its own, `ScriptRoundTrip templebuild`> [out plan]
"""
import json, math, sys
import numpy as np
sys.path.insert(0, __file__.rsplit('\\', 1)[0] if '\\' in __file__ else '.')
from ile import load_ile, hqr_entries

GAME = sys.argv[1] if len(sys.argv) > 1 else r'E:\dump\TEMP\temple\game'
OUT = sys.argv[2] if len(sys.argv) > 2 else r'E:\dump\LBAAssembler\docs\racetrack\temple_track_plan.json'
L = json.load(open(GAME + r'\TEMPLE.JSON'))
SCALE, OX, OZ, BASE, MINY, LAYER = L['scale'], L['offsetX'], L['offsetZ'], L['baseY'], L['minY'], L['layer']
W, D = L['width'], L['depth']
bits = {}
for x, z, b, t, c in L['columns']: bits[(x, z)] = b


def top(layer):
    """The world height of a layer's top (a cell at that layer stands from top(layer - 1) to top(layer))."""
    return BASE + (layer - MINY + 1) * LAYER


FIRST, LOWER, CRYPT = top(8), top(4), top(-8)  # the first floor and its corridor, scene 8's lower hall, the crypt
SP = 0.5                                     # the plan's points, cells apart
DECK = 60                                    # the raised road over the floor it lies on
HALF = 4.5                                   # the road's half width (cells) ...
HALF_CORRIDOR = 4.0                          # ... along the long corridor (its floor 12 cells across)
CUT = 1.6                                    # cells either side of the road cut out of the walls, past its half width: a car held on the
                                             # road (its middle the rail's 1.25 inside its edge) with its box (2.8 round it) touches none
HEAD = 3600                                  # ... and that high over the road: the cars and the camera behind them
GRADE = 0.12                                 # the ramps' steepest
CAR_BOX = 2.8


# ---- the collision boxes (decors with a real box; world units)
boxes = []
read, _ = hqr_entries(GAME + r'\TEMPLE.ILE')
_, _, _, CUBES = load_ile(GAME + r'\TEMPLE.ILE')
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


def path(corners, radii, sp=SP):
    """A line through `corners` (cells), each inner one rounded to its radius: straight runs and circular arcs, sp cells apart."""
    P = [np.array(c, float) for c in corners]
    out = [P[0]]
    cut = [0.0] * len(P)
    for i in range(1, len(P) - 1):
        a, b, c = P[i - 1], P[i], P[i + 1]
        u = (b - a) / np.linalg.norm(b - a); v = (c - b) / np.linalg.norm(c - b)
        turn = math.atan2(u[0] * v[1] - u[1] * v[0], float(u @ v))
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
    return resample(np.array(pts), sp)


def resample(pts, sp=SP):
    d = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]
    s = np.arange(0, d[-1] + 1e-9, sp)
    return np.stack([np.interp(s, d, pts[:, 0]), np.interp(s, d, pts[:, 1])], 1)


def spiral(cx, cz, r, phi0, turns, sp=SP):
    """Round (cx, cz) at radius r from the angle phi0 (0 east, pi/2 south), `turns` times -- positive turns the angle growing: clockwise
    seen from above (x east, z south) -- sp cells apart."""
    phi = np.linspace(phi0, phi0 + 2 * math.pi * turns, 40000)
    return resample(np.stack([cx + r * np.cos(phi), cz + r * np.sin(phi)], 1), sp)


def join(*pieces):
    pts = pieces[0]
    offsets = [0]
    for p in pieces[1:]:
        same = np.linalg.norm(p[0] - pts[-1]) < 1e-6
        offsets.append(len(pts) - (1 if same else 0))
        pts = np.concatenate([pts, p[1:] if same else p])
    return pts, offsets


def ease(t): return t * t * (3 - 2 * t)


# ---- the route (island cells: x east, z south)
CORR_Z = 466                                 # the long corridor's middle (its floor z 460..471, the frieze's wall north of it)
E_END = 556                                  # its east end, where the spiral up comes in
W_END = 386                                  # its west end: north into the first floor
EDGE_Z = 446                                 # the first floor's open south edge
# the spirals, each (x, z) its middle and r its radius (cells), round the anticlockwise way seen from above (x east, z south)
SPIRAL_W = (302, 560, 26)                    # west of the crypt: in at its west point heading south, out at its south point heading east
SPIRAL_E = (604, 520, 24)                    # east of it: in at its south point heading east, out at its east point heading north


def spiral(cx, cz, r, phi0, turns, sp=SP):
    """Round (cx, cz) at radius r from the angle phi0 (0 the east point, pi/2 the south one), the anticlockwise way seen from above --
    the angle falling -- `turns` times, sp cells apart."""
    phi = np.linspace(phi0, phi0 - 2 * math.pi * turns, 40000)
    return resample(np.stack([cx + r * np.cos(phi), cz + r * np.sin(phi)], 1), sp)


lap1 = path([(E_END, CORR_Z), (W_END, CORR_Z), (W_END, 312), (340, 312), (340, 372), (300, 372), (300, 312), (248, 312), (248, 400),
             (276, 400), (SPIRAL_W[0] - SPIRAL_W[2], SPIRAL_W[1])], [0, 12, 12, 10, 10, 10, 12, 10, 10, 10, 0])
down = spiral(SPIRAL_W[0], SPIRAL_W[1], SPIRAL_W[2], math.pi, 1.25)
CRYPT_Z = SPIRAL_W[1] + SPIRAL_W[2]          # the straight through the crypt's west room (the start's)
lap2 = path([tuple(down[-1]), (432, CRYPT_Z), (432, 618), (500, 618), (500, SPIRAL_E[1] + SPIRAL_E[2]),
             (SPIRAL_E[0], SPIRAL_E[1] + SPIRAL_E[2])], [0, 10, 10, 12, 12, 0])
up = spiral(SPIRAL_E[0], SPIRAL_E[1], SPIRAL_E[2], math.pi / 2, 1.25)
lap3 = path([tuple(up[-1]), (SPIRAL_E[0] + SPIRAL_E[2], CORR_Z), (E_END, CORR_Z)], [0, 12, 0])
pts, (o1, o_down, o2, o_up, o3) = join(lap1, down, lap2, up, lap3)
# (a lap: its last point is its first)
if np.linalg.norm(pts[-1] - pts[0]) < 1e-6: pts = pts[:-1]
n = len(pts)
S = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]
print('route', n, 'points,', round(S[-1] + np.linalg.norm(pts[0] - pts[-1])), 'cells')


def nearest(p, lo=0, hi=None):
    hi = n if hi is None else hi
    d = np.hypot(pts[lo:hi, 0] - p[0], pts[lo:hi, 1] - p[1])
    return lo + int(d.argmin())


# ---- the heights: each level from where it is reached to where it is left, ramps between (eased, no steeper than GRADE)
i_west = nearest((W_END, CORR_Z))
i_hall_in = nearest((W_END, 392), i_west)            # the lower hall's south wall
i_hall_n = nearest((W_END, 312), i_west)
i_hall_sw = nearest((340, 372), i_hall_n)
i_west_rooms = nearest((300, 372), i_hall_sw)
i_edge = nearest((276, EDGE_Z), i_west_rooms)
i_down_in, i_down_out = o_down, o2
i_crypt_w = nearest((372, SPIRAL_W[1] + SPIRAL_W[2]), i_down_out)
i_turn_n = nearest((500, 612), i_crypt_w)
i_room_e = nearest((500, SPIRAL_E[1] + SPIRAL_E[2]), i_turn_n)
i_up_in, i_up_out = o_up, o3
# (the climb up the east spiral ends CLIMB_EARLY cells short of the corridor's east end, before the giant web's doorway: ending there, the
# road was still climbing through the doorway, up to 113 under the corridor's height, so its floor cells were cut from under the deck --
# the 2026-10-18 audit's holes either side of the road under the web)
CLIMB_EARLY = 6
i_climbed = nearest((E_END + CLIMB_EARLY, CORR_Z), o3)
anchors = [  # (point, height of the floor the road lies on there), in the lap's order
    (0, FIRST), (i_west, FIRST),
    (i_hall_in - 6, FIRST), (i_hall_in + int(1.5 * 3072 / (GRADE * 512) / SP), LOWER),  # down into the lower hall (eased: 1.5 times as long)
    (i_hall_sw - int(1.5 * 3072 / (GRADE * 512) / SP), LOWER), (i_hall_sw, FIRST),      # up again before it turns west out of it
    (i_edge, FIRST),
    (i_crypt_w, CRYPT),                                                                 # down: the spiral west of the crypt
    (i_turn_n, CRYPT), (i_room_e, top(-5)),                                             # up into the crypt's east room
    (i_up_in, top(-5)),
    (i_climbed, FIRST), (n - 1, FIRST),                                                 # up: the spiral east of it
]
floor_h = np.zeros(n)
for (a, ha), (b, hb) in zip(anchors, anchors[1:]):
    for i in range(a, b + 1):
        t = (S[i] - S[a]) / max(1e-9, S[b] - S[a])
        floor_h[i] = ha + (hb - ha) * (ease(t) if abs(hb - ha) < 4000 else t)
h = floor_h + DECK
grade = np.abs(np.diff(h)) / (np.maximum(np.diff(S), 1e-6) * 512)
print('steepest %.3f at point %d; the drop %d over %d cells, the climb %d over %d' % (grade.max(), grade.argmax(), FIRST - CRYPT, round(S[i_crypt_w] - S[i_edge]),
      FIRST - top(-5), round(S[n - 1] - S[i_up_in])))


# ---- the cells the road passes through: cut out of the temple (the plan's templeCuts) -- every layout cell within the road's half width and
# CUT of a point whose height overlaps the road's slab and the room over it
def layout_of(cx, cz): return int(math.floor((cx - OX) / SCALE)), int(math.floor((cz - OZ) / SCALE))


half = np.full(n, HALF)
corridor = (np.abs(pts[:, 1] - CORR_Z) < 1.0) & (pts[:, 0] > W_END + 10) & (pts[:, 0] < E_END + 1)
half[corridor] = HALF_CORRIDOR


# ---- the slip lane (the user, 2026-10-17: "We need to add a slip lane for the temple track, currently trying to complete a qualifying lap
# gets blocked with the other cars on the start/finish line"): where the opponents wait while Twinsen drives his qualifying lap, out of the
# racing lane -- their grid spots, where the race-track mode stood them while it had nowhere else (the waiting spots on the crypt's floor
# this plan gave before were in the cube north of the start's, and the build writes only the start cube's). A strip of road beside the
# start straight (the plan's raisedStrips, as the Emerald Moon's pit lane: RaceTrackBuilder's StripPieces, RACEMOD.CPP StripLo/Hi), on its
# north side -- the inside of the lap, away from the racing line, which keeps to the straight's middle and goes to its south side for the
# corner after the line: out of the road's edge past the west room's doorway walls, apart from it beside the grid and through the start
# line (clear space between them), back into it before the corner. Across, cells: negative north (the builder's Across, the left of the
# way the lap runs, is south here). All of it in the start's cube: the cube north of it (z < 576) isn't one of the race's scenes, so the
# engine holds a car half a cell inside the edge (z 576.5) -- the lane's rail (1.25 inside its edge) holds it no further north than that.
SLIP_FAR, SLIP_NEAR = -10.75, -6.25          # the lane's north edge and its south one (1.75 cells of clear space from the road's edge)
SLIP_OUT = (389.5, 394.5)                    # x: its north edge out of the road's edge, ...
SLIP_APART = (393.5, 395.5)                  # ... then its south edge away from it -- ...
SLIP_BACK = (414.5, 416.5)                   # ... back to it ...
SLIP_IN = (416.0, 421.0)                     # ... and its north edge back in, before the corner (its arc from x 422)
SLIP_SPOTS = [396.0, 400.0, 404.0, 408.0, 412.0]     # x: the waiting spots along its middle, the first opponent's first (five: a grid's)


def ramp(x, a, b): return min(1.0, max(0.0, (x - a) / (b - a)))


slip_lo, slip_hi = np.zeros(n), np.zeros(n)
for i in range(n):
    x, z = pts[i]
    if abs(z - CRYPT_Z) > 0.01 or abs(h[i] - (CRYPT + DECK)) > 1 or not SLIP_OUT[0] <= x <= SLIP_IN[1]: continue
    far = -half[i] + (SLIP_FAR + half[i]) * ramp(x, *SLIP_OUT) * (1 - ramp(x, *SLIP_IN))
    near = -half[i] + (SLIP_NEAR + half[i]) * ramp(x, *SLIP_APART) * (1 - ramp(x, *SLIP_BACK))
    if near - far > 1e-6: slip_lo[i], slip_hi[i] = round(far, 3), round(near, 3)
slip = np.nonzero(slip_hi > slip_lo)[0]
print('slip lane: points %d..%d, x %.1f..%.1f, %.2f..%.2f cells across' % (slip[0], slip[-1], pts[slip[0]][0], pts[slip[-1]][0], slip_lo.min(), slip_hi[slip].max()))


def slip_off(x, z):
    """How far (x, z) is from the slip lane (cells; 0 on it)."""
    best = 1e9
    if not (SLIP_OUT[0] - 30 < x < SLIP_IN[1] + 30 and abs(z - CRYPT_Z) < 30): return best
    for i in slip:
        u = pts[min(n - 1, i + 1)] - pts[max(0, i - 1)]; u = u / max(1e-9, np.linalg.norm(u)); nrm = np.array([-u[1], u[0]])
        d = np.array([x, z]) - pts[i]
        a, s = float(d @ nrm), abs(float(d @ u))
        best = min(best, math.hypot(s, max(slip_lo[i] - a, 0.0, a - slip_hi[i])))
    return best


# (2026-10-18, the port's audit: the cuts left black pits beside the road -- floor cells beside the ramps cut down with the walls -- and
# thin tops of walls hanging over it) -- each island cell within reach of the road's points (every other point): a cell of a layout
# column there is cut where it reaches into the room over the road, from DECK - 20 under the road's surface (no floor pokes through the
# deck) to HEAD over a point's. The surface it is kept under: under the deck (within DECK_EDGE of its edge: the car's box, 633 round its
# middle and unturned, reaches half a cell past the edge on a diagonal), the lowest of the road's within the car's length (CAR_ALONG)
# of the point nearest the cell; past it, beside the road, the highest within reach -- so the floor beside a ramp stays where it is no
# higher than the road beside it, the car held on its deck never reaching it. Each pass of the road by a column (its points within
# reach SAME_PASS apart in height: the spirals' turns, the corridor over the crypt) on its own.
SAME_PASS = 1500
DECK_EDGE = 1.0
CAR_ALONG = 2.0
LAP = S[-1] + float(np.linalg.norm(pts[0] - pts[-1]))
cell_points = {}                                # island cell -> {point: (distance squared, under the deck)}


def near(cell, i, d2, deck):
    e = cell_points.setdefault(cell, {})
    if i not in e or d2 < e[i][0]: e[i] = (d2, e.get(i, (0, False))[1] or deck)
    elif deck: e[i] = (e[i][0], True)


for i in range(0, n, 2):
    x, z = pts[i]
    reach = half[i] + CUT
    for cz in range(int(math.floor(z - reach)), int(math.floor(z + reach)) + 1):
        for cx in range(int(math.floor(x - reach)), int(math.floor(x + reach)) + 1):
            d2 = (cx + 0.5 - x) ** 2 + (cz + 0.5 - z) ** 2
            if d2 > reach * reach: continue
            near((cx, cz), i, d2, d2 <= (half[i] + DECK_EDGE) ** 2)
# (and over the slip lane, as over the road: CUT past its edges)
for i in slip:
    for cz in range(int(math.floor(pts[i][1] + slip_lo[i] - CUT)), int(math.floor(pts[i][1] + slip_hi[i] + CUT)) + 1):
        for cx in range(int(math.floor(pts[i][0] - 1)), int(math.floor(pts[i][0] + 1)) + 1):
            off = slip_off(cx + 0.5, cz + 0.5)
            if off > CUT: continue
            near((cx, cz), i, (cx + 0.5 - pts[i][0]) ** 2 + (cz + 0.5 - pts[i][1]) ** 2, off <= DECK_EDGE)
col_cells = {}
for cell in cell_points: col_cells.setdefault(layout_of(*cell), []).append(cell)
col_points = {key: sorted({i for c in cells_ for i in cell_points[c]}) for key, cells_ in col_cells.items()}


def passes(points):
    """The points (indices) in groups by height: each group a pass of the road (SAME_PASS apart from the next)."""
    out = []
    for i in sorted(points, key=lambda k: (h[k], k)):
        if out and h[i] - h[out[-1][-1]] < SAME_PASS: out[-1].append(i)
        else: out.append([i])
    return out


def along(a, b):
    d = abs(S[a] - S[b])
    return min(d, LAP - d)


def room_of(key):
    """A column's rooms over the road: each pass's (the highest a cell may top out at and not be cut, and its points' heights)."""
    out = []
    for p in passes(col_points[key]):
        mine = set(p)
        lo = math.inf
        for cell in col_cells[key]:
            e = {i: v for i, v in cell_points[cell].items() if i in mine}
            if not e: continue
            j = min(e, key=lambda i: (e[i][0], i))
            if any(deck for _, deck in e.values()):
                lo = min(lo, min(h[i] for i in e if along(i, j) <= CAR_ALONG) - DECK + 20)
            else:
                lo = min(lo, max(h[i] for i in p) - DECK + 20)
        out.append((lo, [h[i] for i in p]))
    return out


def present(b, y): return (b >> (y - MINY)) & 1


cuts = set()
rooms = {key: room_of(key) for key in col_points}
for key, room in rooms.items():
    b = bits.get(key, 0)
    for y in range(MINY, L['maxY'] + 1):
        if present(b, y) and any(top(y) > lo and top(y - 1) < max(hs) + HEAD for lo, hs in room): cuts.add((key[0], y, key[1]))
# ... and what a cut leaves of a column over it one or two layers thick -- a wall's top hanging over the road's edge, a thin slab of
# lintel -- taken with it
THIN = 2
caps = 0
for key in {(c[0], c[2]) for c in cuts}:
    b = bits.get(key, 0)
    high = max(c[1] for c in cuts if (c[0], c[2]) == key)
    over = [y for y in range(high + 1, L['maxY'] + 1) if present(b, y)]
    if 0 < len(over) <= THIN:
        cuts.update((key[0], y, key[1]) for y in over)
        caps += len(over)
print('cells cut', len(cuts), f'({caps} of them thin tops left over the cuts)')


# ---- the floors the cuts take (TempleIsland.Carve fills them again): for each column the road comes within reach of, the layer of the
# floor round it -- the commonest top among its neighbours' kept cells no higher than the road's surface there (the nearest point's) --
# and the highest a cell may stand in it without reaching into the road's room. Where the cuts (or what they took with them: a statue
# standing on no floor, a row of rocks down to its foot) leave the column nothing from that floor up to that height, the build lays a
# floor cell there, at the lower of the two: a doorway's floor where LBA1 had a wall standing on nothing, the ledge of a trench where a
# ramp goes down through a floor. Each [x, z, the highest, the layer to lay it at, the floor's layer round it] -- or [x, z, the highest]
# where no floor is round it (the build then takes away the pieces the cuts split off higher than that, hanging over the road).
def kept(key, y): return present(bits.get(key, 0), y) and (key[0], y, key[1]) not in cuts


def kept_top(key, surface):
    return next((y for y in range(L['maxY'], MINY - 1, -1) if top(y) <= surface + 1 and kept(key, y)), None)


cut_cols = {(c[0], c[2]) for c in cuts}
SKIRT_MOST = 5                                  # a trench's wall no more than this many layers over its bottom gets a skirt


def kept_above(key, y):
    """The lowest kept cell of a column over layer y."""
    return next((k for k in range(y + 1, L['maxY'] + 1) if kept(key, k)), None)


def highest_of(key):
    lo = min(r[0] for r in rooms[key])
    return max((y for y in range(MINY - 1, L['maxY'] + 1) if top(y) <= lo), default=MINY - 1)


floors, bottoms = [], {}
for key in sorted(col_points):
    lx, lz = key
    highest = highest_of(key)
    if highest < MINY: continue
    cx, cz = OX + SCALE * (lx + 0.5), OZ + SCALE * (lz + 0.5)
    surface = h[min(col_points[key], key=lambda i: (pts[i][0] - cx) ** 2 + (pts[i][1] - cz) ** 2)]
    tops = [kept_top((lx + dx, lz + dz), surface) for dx in (-1, 0, 1) for dz in (-1, 0, 1)
            if (dx or dz) and (lx + dx, lz + dz) not in cut_cols]
    tops = [t for t in tops if t is not None]
    if tops:
        floor = max(set(tops), key=lambda t: (tops.count(t), t))
        floors.append([lx, lz, highest, min(floor, highest), floor])
        if key in cut_cols: bottoms[key] = min(floor, highest)
        continue
    # (no floor round it at the road's height: where the road goes down through a floor standing higher than it on both sides -- the crypt
    # west room's stepped gallery, its steps up to four layers over the road -- a cut column's floor laid at the road's height under the
    # deck, the steps' walls beside it skirted down to it, below; over the void, nothing)
    walls = [kept_above((lx + dx, lz + dz), highest) for dx, dz in ((1, 0), (-1, 0), (0, 1), (0, -1)) if (lx + dx, lz + dz) not in cut_cols]
    walls = [w for w in walls if w is not None and w <= highest + SKIRT_MOST]
    if key in cut_cols and walls:
        floors.append([lx, lz, highest, highest, min(walls)])
        bottoms[key] = highest
    else:
        floors.append([lx, lz, highest])
# (and a cut column with neither, among cut columns whose floors are laid -- under the deck where a ramp leaves a floor, all round it cut:
# the floor its neighbours' are laid at, the commonest, no higher than its own highest)
grew = True
while grew:
    grew = False
    for f in floors:
        if len(f) != 3 or (f[0], f[1]) not in cut_cols: continue
        near_ = [bottoms[(f[0] + dx, f[1] + dz)] for dx in (-1, 0, 1) for dz in (-1, 0, 1) if (dx or dz) and (f[0] + dx, f[1] + dz) in bottoms]
        near_ = [b_ for b_ in near_ if b_ <= f[2]]
        if not near_: continue
        lay = max(set(near_), key=lambda t: (near_.count(t), t))
        f.extend([lay, lay])
        bottoms[(f[0], f[1])] = lay
        grew = True
print('floors under the cuts', len(floors), f'({len(bottoms)} cut columns with a floor to lay)')
# ---- the skirts: under a kept floor (a step, a ledge) beside a cut column whose floor is laid lower, the void down to that floor filled
# (TempleIsland.Carve) -- a trench's wall from its floor up, where it showed the dark under the floor beside it. Each [x, z, from layer,
# to layer].
skirts = {}
for (lx, lz), b in sorted(bottoms.items()):
    for dx, dz in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        k = (lx + dx, lz + dz)
        if k in cut_cols: continue
        w = kept_above(k, b)
        if w is None or w - b - 1 < 1 or w > b + SKIRT_MOST: continue
        if any(kept(k, y) for y in range(b + 1, w)): continue
        y1 = min(w - 1, highest_of(k)) if k in col_points else w - 1
        if y1 <= b: continue
        skirts[k] = (min(b + 1, skirts.get(k, (99, 0))[0]), max(y1, skirts.get(k, (99, -99))[1]))
skirts = [[k[0], k[1], a, b_] for k, (a, b_) in sorted(skirts.items())]
print('skirts', len(skirts), 'columns,', sum(b_ - a + 1 for _, _, a, b_ in skirts), 'cells')


# ---- a picture of the route over the temple from above (PLOT=<png>: E:\dump\TEMP\temple\design\top.png's frame, a cell 4 pixels)
import os
if os.environ.get('PLOT'):
    from PIL import Image, ImageDraw
    im = Image.open(os.environ.get('TOP', r'E:\dump\TEMP\temple\design\top.png')).convert('RGB')
    dr = ImageDraw.Draw(im)
    def px(x, z): return (4 * x - 896, 4 * z - 1024)
    lo_h, hi_h = h.min(), h.max()
    for i in range(n):
        x, z = pts[i]; x2, z2 = pts[(i + 1) % n]
        t = (h[i] - lo_h) / (hi_h - lo_h)
        dr.line([px(x, z), px(x2, z2)], fill=(int(255 * t), int(80 + 100 * (1 - t)), int(255 * (1 - t))), width=4 * int(2 * HALF) // 2)
    for (lx, y, lz) in cuts:
        u, v = px(OX + SCALE * lx + 1.5, OZ + SCALE * lz + 1.5)
        dr.rectangle([u - 2, v - 2, u + 2, v + 2], outline=(255, 255, 255))
    for k in range(0, n, 100):
        dr.text(px(*pts[k]), str(k), fill=(255, 255, 0))
    im.resize((im.width // 2, im.height // 2)).save(os.environ['PLOT'])
    print('plot', os.environ['PLOT'])


# ---- Halloween (Terrain/Temple/TempleHalloween): the decor about the road and in the dark round it, the spooks flying over it
LANTERN, GIANT, GRAVE, CROSS, CANDLES, CAULDRON = range(6)
cut_columns = {(c[0], c[2]) for c in cuts}
lap_pts = pts                                   # (the lap as built, in its own order: distances from it)


def column_top(lx, lz):
    b = bits.get((lx, lz), 0)
    if not b: return None
    return max(y for y in range(MINY, L['maxY'] + 1) if (b >> (y - MINY)) & 1)


def flat_floor(cx, cz, r):
    """The layer of the floor round island cell (cx, cz), r layout cells round it all at one top and none of them cut; else None."""
    lx, lz = layout_of(cx, cz)
    tops = set()
    for dz in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if (lx + dx, lz + dz) in cut_columns: return None
            t = column_top(lx + dx, lz + dz)
            if t is None: return None
            tops.add(t)
    return tops.pop() if len(tops) == 1 else None


# how far each piece of decor standing on a floor reaches round its middle (cells: TempleHalloween's models)
REACH = {LANTERN: 1.25, GRAVE: 1.05, CROSS: 1.0, CANDLES: 0.5, CAULDRON: 1.25}


def footprint_on(x, z, layer, reach):
    """Every layout column under the square `reach` cells round (x, z) a floor at `layer`, none of them cut (the 2026-10-18 audit: a
    jack-o'-lantern by the west rooms stood part-way into a wall, the floor under its middle only looked at)."""
    cols_ = {layout_of(x + a * reach, z + b * reach) for a in (-1, 0, 1) for b in (-1, 0, 1)}
    return all(c not in cut_columns and column_top(*c) == layer for c in cols_)


def clear_of_road(x, z, margin):
    d = np.hypot(lap_pts[:, 0] - x, lap_pts[:, 1] - z) - half
    # (and of the slip lane beside the start straight)
    return float(d.min()) >= margin and slip_off(x, z) >= margin


def heading(i):
    a, b = pts[max(0, i - 2)], pts[min(n - 1, i + 2)]
    d = b - a
    return d / max(1e-9, np.linalg.norm(d))


def turn_to(fx, fz, tx, tz):
    """The decor's turn (0..4095) that has its front (+z) look from (fx, fz) towards (tx, tz)."""
    return int(round(math.atan2(tx - fx, tz - fz) * 4096 / (2 * math.pi))) & 4095


decor = []
cycle = [LANTERN, GRAVE, CANDLES, LANTERN, CROSS, CAULDRON, LANTERN, GRAVE, GRAVE, CANDLES]
k = 0
for i in range(10, n, int(18 / SP)):
    u = heading(i)
    side = 1 if (i // int(18 / SP)) % 2 == 0 else -1
    nrm = np.array([-u[1], u[0]]) * side
    placed = False
    kind = cycle[k % len(cycle)]
    for off in (half[i] + 3.2, half[i] + 4.6, half[i] + 6.5, half[i] + 8.5):
        x, z = pts[i] + nrm * off
        top_ = flat_floor(x, z, 1 if kind in (CAULDRON, CROSS) else 0)
        if top_ is not None and abs(top(top_) - (h[i] - DECK)) <= LAYER + 10 and clear_of_road(x, z, 2.4):
            # (a place for it; but where its footprint is on a wall or over a drop, the next one out -- none: this one left out, the
            # cycle of kinds going on as if it stood there)
            placed = True
            if not footprint_on(x, z, top_, REACH[kind]): continue
            decor.append([kind, round(float(x), 2), round(float(z), 2), float(top(top_)), turn_to(x, z, *pts[i])])
            if kind in (GRAVE, CROSS):
                # (a grave's candles in front of it, toward the road)
                cx_, cz_ = pts[i] + nrm * (off - 1.4) + u * 0.8
                if flat_floor(cx_, cz_, 0) == top_ and clear_of_road(cx_, cz_, 1.6) and footprint_on(cx_, cz_, top_, REACH[CANDLES]):
                    decor.append([CANDLES, round(float(cx_), 2), round(float(cz_), 2), float(top(top_)), turn_to(cx_, cz_, *pts[i])])
            break
    if placed: k += 1
    else:
        # (over the dark: a jack-o'-lantern floating beside the road, a little over it)
        x, z = pts[i] + nrm * (half[i] + 4.0)
        lx, lz = layout_of(x, z)
        if all(column_top(lx + a, lz + b) is None for a in (-1, 0, 1) for b in (-1, 0, 1)) and clear_of_road(x, z, 2.4):
            decor.append([LANTERN, round(float(x), 2), round(float(z), 2), round(float(h[i] + 250 + 500 * ((i // 36) % 3)), 1), turn_to(x, z, *pts[i])])
# (up each spiral's middle, a totem of giant jack-o'-lanterns looking out every way)
for (cx_, cz_, r_), lo_, hi_ in ((SPIRAL_W, CRYPT, FIRST), (SPIRAL_E, top(-5), FIRST)):
    for j, y in enumerate(np.arange(lo_ - 1500, hi_ + 1, 2400)):
        decor.append([GIANT, float(cx_), float(cz_), round(float(y), 1), (j * 1365) & 4095])
print('halloween decor', len(decor), {nm: sum(1 for d in decor if d[0] == kk) for kk, nm in enumerate(['lantern', 'giant', 'grave', 'cross', 'candles', 'cauldron'])})


# ---- skeletons (the user, 2026-10-17: "Let's make the temple track creepier with some skeletons"): LBA1's own skeleton, posed
# (TempleHalloween.Skeletons) -- lying by the road, slumped against the walls beside it, hanging in chains on those walls, and hanging on long
# chains in the dark beside the spirals -- seen from the road, never on it (decor drawn only, nothing to bump into)
SK_LYING, SK_SLUMPED, SK_HANGING, SK_CHAINED = range(6, 10)
SLUMP_REACH = 2.7                               # how far a slumped skeleton's feet reach out from the wall it sits against (cells)
HANG_LIFT = 900                                 # a skeleton hung on a wall: its feet this far over the floor
SK_MOST = {SK_LYING: 7, SK_SLUMPED: 9, SK_HANGING: 9, SK_CHAINED: 7}


def far_from_decor(x, z, d=2.6):
    return all((x - e[1]) ** 2 + (z - e[2]) ** 2 >= d * d for e in decor)


def wall_beside(i, side, most=9.0):
    """Out from the road at point i, to its `side` (1 its left, -1 its right): the floor the road lies on, all the way to the first wall --
    a column standing two layers or more over that floor -- within `most` cells of the road's middle; (cells out to the wall's face, the
    floor's layer, the way out) or None (the dark, a step, a doorway cut)."""
    u = heading(i); nrm = np.array([-u[1], u[0]]) * side
    floor_layer = None
    for off in np.arange(half[i] + 0.5, half[i] + most, 0.25):
        x, z = pts[i] + nrm * off
        lx, lz = layout_of(x, z)
        if (lx, lz) in cut_columns: return None
        t = column_top(lx, lz)
        if t is None: return None
        if floor_layer is None:
            if abs(top(t) - (h[i] - DECK)) > LAYER + 10: return None
            floor_layer = t; continue
        if t >= floor_layer + 2: return off, floor_layer, nrm
        if t != floor_layer: return None
    return None


skeletons = []
for c, i in enumerate(range(19, n, int(15 / SP))):
    side = 1 if c % 2 else -1
    count = lambda kind: sum(1 for s_ in skeletons if s_[0] == kind)
    placed = False
    # (none by the giant web's doorway, its columns at layout x 111: there it is the web the car comes up to -- the cuts keep the doorway's
    # floor whole since 2026-10-18, walls to hang one on)
    if math.hypot(pts[i][0] - (OX + SCALE * 111 + 1.5), pts[i][1] - CORR_Z) < 10: continue
    for sd in (side, -side):
        w = wall_beside(i, sd)
        if w is None: continue
        off, layer, nrm = w
        # (its back to the wall, facing the road: slumped where its feet stay clear of the road's edge, else hung on the wall)
        at = off - 0.35
        kind = SK_SLUMPED if at - SLUMP_REACH >= half[i] + 0.4 and count(SK_SLUMPED) < SK_MOST[SK_SLUMPED] else SK_HANGING
        if kind == SK_HANGING and (count(SK_HANGING) >= SK_MOST[SK_HANGING] or c % 2): continue
        x, z = pts[i] + nrm * at
        if not far_from_decor(x, z) or any((x - s_[1]) ** 2 + (z - s_[2]) ** 2 < 36 for s_ in skeletons): continue
        y = top(layer) + (HANG_LIFT if kind == SK_HANGING else 0)
        skeletons.append([kind, round(float(x), 2), round(float(z), 2), float(y), turn_to(x, z, *(pts[i]))])
        placed = True
        break
    if placed or c % 3: continue
    # (no wall near: lying on the floor beside the road, or hung in the dark over the void beside it)
    u = heading(i); nrm = np.array([-u[1], u[0]]) * side
    for off in (half[i] + 2.6, half[i] + 3.6, half[i] + 5.0):
        x, z = pts[i] + nrm * off
        t = flat_floor(x, z, 1)
        if count(SK_LYING) < SK_MOST[SK_LYING] and t is not None and abs(top(t) - (h[i] - DECK)) <= LAYER + 10 and clear_of_road(x, z, 2.2) and far_from_decor(x, z):
            skeletons.append([SK_LYING, round(float(x), 2), round(float(z), 2), float(top(t)), (turn_to(x, z, *pts[i]) + 1024 * (1 if c % 2 else -1)) & 4095])
            placed = True
            break
    if placed: continue
    x, z = pts[i] + nrm * (half[i] + 3.2)
    lx, lz = layout_of(x, z)
    if count(SK_CHAINED) < SK_MOST[SK_CHAINED] and all(column_top(lx + a, lz + b) is None for a in (-1, 0, 1) for b in (-1, 0, 1)) \
            and clear_of_road(x, z, 2.4) and far_from_decor(x, z, 3.0):
        skeletons.append([SK_CHAINED, round(float(x), 2), round(float(z), 2), round(float(h[i] + 700), 1), turn_to(x, z, *pts[i])])
decor += skeletons
print('skeletons', len(skeletons), {nm: sum(1 for d in skeletons if d[0] == kk) for kk, nm in ((SK_LYING, 'lying'), (SK_SLUMPED, 'slumped'), (SK_HANGING, 'hanging'), (SK_CHAINED, 'chained'))})


def circle(cx, cz, r, m=24, phase=0.0):
    return [[round(float(cx + r * math.cos(phase + 2 * math.pi * j / m)), 2), round(float(cz + r * math.sin(phase + 2 * math.pi * j / m)), 2)] for j in range(m)]


def across(i, reach):
    """A way across the road at point i, `reach` cells either side of it."""
    u = heading(i); nrm = np.array([-u[1], u[0]])
    a, b = pts[i] - nrm * reach, pts[i] + nrm * reach
    return [[round(float(a[0]), 2), round(float(a[1]), 2)], [round(float(b[0]), 2), round(float(b[1]), 2)]]


# (2026-10-18, the port's audit: the ghosts drifted across the road 1,300-1,900 over it, the chase camera's eye 2,100 over it went through
# them and the screen filled with their sheets; witches crossed ramps at the cars' height and passed through the deck) -- every spook's
# height is kept clear of the road wherever its way comes over it (within its half width and two cells): its body's lowest (bobbing) at
# least SPOOK_CLEAR over the road there, and over the road the camera comes from (its eye 2,100 over the road 9 cells behind the car: the
# 20 points before, up a ramp higher); or, a witch's, its highest SPOOK_UNDER under the road's surface (the deck under it) -- the
# height asked for, or the nearest that is
SPOOK_BODY = {'witch': (390, 1420), 'ghost': (0, 1680), 'bat': (100, 250)}     # its body under and over its height (the bodies')
SPOOK_BOB = 260                                                                  # RACEMOD.CPP RACE_SPOOK_BOB
SPOOK_CLEAR, SPOOK_UNDER = 2500, 1000


def way_points(way, loop):
    w = [np.array(p, float) for p in way]
    segs = list(zip(w, w[1:] + ([w[0]] if loop else [])))
    out = []
    for a, b in segs:
        m = max(1, int(np.linalg.norm(b - a) / 0.5))
        out.extend(a + (b - a) * k / m for k in range(m + 1))
    return out


def spook_height(kind, way, loop, y0, under=False):
    over = set()
    for q in way_points(way, loop):
        d = np.hypot(pts[:, 0] - q[0], pts[:, 1] - q[1])
        over.update(int(i) for i in np.nonzero(d < half + 2)[0])
    if not over: return round(float(y0), 1)
    low, tall = SPOOK_BODY[kind]
    road = [(h[i], max(h[(i - k) % n] for k in range(21))) for i in sorted(over)]
    def clear(y):
        return all(y - low - SPOOK_BOB >= cam + SPOOK_CLEAR or (under and y + tall + SPOOK_BOB <= r - SPOOK_UNDER) for r, cam in road)
    y = next(y0 + s * d for d in range(0, 30000, 25) for s in (1, -1) if clear(y0 + s * d))
    return round(float(y), 1)


spooks = []
# witches round the spirals, outside the road, a third and two thirds of the way up, throwing fire balls at the cars below them
for (cx_, cz_, r_), lo_, hi_ in ((SPIRAL_W, CRYPT, FIRST), (SPIRAL_E, top(-5), FIRST)):
    for frac, ph in ((0.42, 0.0), (0.85, math.pi)):
        way = circle(cx_, cz_, r_ + 13, 24, ph)
        spooks.append({'kind': 'witch', 'flags': 3, 'y': spook_height('witch', way, True, lo_ + (hi_ - lo_) * frac + 2500, under=True), 'way': way})
# witches over the halls: along the long corridor and across the lower hall (over its floor, where the road is down on it, not the ramps
# either side), the crypt's rooms
for p0, p1, hy in (((W_END + 30, CORR_Z), (E_END - 20, CORR_Z), FIRST + 3200), ((360, 300), (392, 328), LOWER + 3400),
                   ((380, 560), (440, 600), CRYPT + 3400), ((470, 585), (530, 630), CRYPT + 3400)):
    way = [list(map(float, p0)), list(map(float, p1))]
    spooks.append({'kind': 'witch', 'flags': 1, 'y': spook_height('witch', way, False, hy), 'way': way})
# ghosts drifting across the road, over the cars and the camera (the crypt's west room's past the start line and the flag marshal's places
# by it: across at the line, it drifted through her -- RACEMOD.CPP marshal=)
ghost_at = [nearest(p) for p in ((420, CORR_Z), (500, CORR_Z), (W_END, 360), (300, 340), (262, 372), (417, CRYPT_Z), (470, 618), (500, 560), (540, SPIRAL_E[1] + SPIRAL_E[2]))]
for j, i in enumerate(ghost_at):
    way = across(i, half[i] + 10)
    spooks.append({'kind': 'ghost', 'flags': 0, 'y': spook_height('ghost', way, False, h[i] + 2800 + 300 * (j % 3)), 'way': way})
# bats wheeling high over the road
bat_at = [nearest(p) for p in ((450, CORR_Z), (360, 320), (270, 330), (400, 590), (500, 600), (520, 520))]
for j, i in enumerate(bat_at):
    way = circle(pts[i][0], pts[i][1], 6 + 2 * (j % 3), 12, j)
    spooks.append({'kind': 'bat', 'flags': 2, 'y': spook_height('bat', way, True, h[i] + 2600 + 400 * (j % 2)), 'way': way})
for (cx_, cz_, r_), lo_, hi_ in ((SPIRAL_W, CRYPT, FIRST), (SPIRAL_E, top(-5), FIRST)):
    for frac in (0.2, 0.62):
        spooks.append({'kind': 'bat', 'flags': 2, 'y': round(float(lo_ + (hi_ - lo_) * frac + 1800), 1), 'way': circle(cx_, cz_, r_ * 0.5, 12, frac * 9)})
print('spooks', len(spooks), {kk: sum(1 for s_ in spooks if s_['kind'] == kk) for kk in ('witch', 'ghost', 'bat')})
assert len(spooks) <= 48


# ---- the scares (the user, 2026-10-17: "also do visual scares, perhaps a witch dive bombing at the player and pulling up at the last second
# to avoid a collision"): the places the witches dive at Twinsen's car from (RACEMOD.CPP dive=: as the car comes 28 cells from one along the
# lap, now and then a witch sets off from high over it and dives along the road at the car) -- where nothing stands over the road, from a
# few cells past the place back to where she climbs away behind the car: the long corridor, the straight over the dark, the spirals, the
# crypt's straight -- each [x, z, the road's height there]
DIVE_SPAN = (-24, 3)                            # cells along the lap she flies over, from the place (the car comes from behind it)
DIVE_ROOM = 9000                                # nothing over the road this high over it


def open_over(i):
    """Nothing of the temple over the road at point i, its width and a cell either side, up to DIVE_ROOM over it."""
    u = heading(i); nrm = np.array([-u[1], u[0]])
    for off in np.arange(-half[i] - 1, half[i] + 1.01, 0.5):
        x, z = pts[i] + nrm * off
        b = bits.get(layout_of(x, z), 0)
        if not b: continue
        for y in range(MINY, L['maxY'] + 1):
            if (b >> (y - MINY)) & 1 and top(y - 1) >= h[i] - DECK + 20 and top(y - 1) < h[i] + DIVE_ROOM: return False
    return True


def dive_clear(i):
    return all(open_over((i + j) % n) for j in range(int(DIVE_SPAN[0] / SP), int(DIVE_SPAN[1] / SP) + 1, 2))


# ... and the start gantry (its beam over the road at the line) no nearer than GANTRY_CLEAR cells to where she flies (the 2026-10-18 audit:
# the crypt straight's witch, her place 1 cell past the line, dived through the beam)
GANTRY_CLEAR = 10
i_line = nearest((410, CRYPT_Z), i_down_out)


def gantry_clear(i):
    ahead = S[i_line] - S[i]
    ahead = (ahead + LAP / 2) % LAP - LAP / 2
    return not (DIVE_SPAN[0] - GANTRY_CLEAR <= ahead <= DIVE_SPAN[1] + GANTRY_CLEAR)


dives = []
for want in [nearest((412, CORR_Z)), nearest((505, CORR_Z)), nearest((276, 520), i_edge), o_down + int(0.62 * (o2 - o_down)),
             nearest((392, CRYPT_Z), i_down_out), o_up + int(0.55 * (o3 - o_up))]:
    # (the nearest place to the one wanted, along the lap, with the room over the road)
    i = next((want + s * j for j in range(0, 80, 2) for s in (1, -1) if dive_clear((want + s * j) % n) and gantry_clear((want + s * j) % n)), None)
    if i is None: print('dive place near point', want, 'has no room over the road'); continue
    i %= n
    dives.append([round(float(pts[i][0]), 2), round(float(pts[i][1]), 2), round(float(h[i]), 1)])
print('dives', len(dives), dives)

# ... and what jumps up out of the floor beside the road as the car comes by (scare=): skeletons reaching out at it, and ghosts -- each
# [0 skeleton | 1 ghost, x, z, the floor's height, the way it faces (at the road)]
scares = []
for j, i in enumerate(range(60, n, int(48 / SP))):
    u = heading(i)
    for sd, off in ((1, 2.3), (-1, 2.3), (1, 3.1), (-1, 3.1)):
        nrm = np.array([-u[1], u[0]]) * sd
        x, z = pts[i] + nrm * (half[i] + off)
        t = flat_floor(x, z, 1)
        if t is None or abs(top(t) - (h[i] - DECK)) > LAYER + 10 or not clear_of_road(x, z, 1.9) or not far_from_decor(x, z, 2.2): continue
        scares.append([0 if j % 3 != 2 else 1, round(float(x), 2), round(float(z), 2), float(top(t)), turn_to(x, z, *pts[i])])
        break
print('scares', len(scares), {nm: sum(1 for s_ in scares if s_[0] == kk) for kk, nm in enumerate(('skeleton', 'ghost'))})


# ---- the giant web (the user, 2026-10-17: "Can we also improve the temple track add in some cobwebs, perhaps a giant cobweb we have to
# drive through"): across the long corridor's east doorway, between the two columns either side of the road there (LBA1's, layout x 111, z
# 67 and 72: their faces 6 cells either side of the road's middle) -- the doorway the lap comes into from the dark at the top of the east
# spiral, 60 cells of straight leading up to it: seen coming across the dark, and driven straight through (RACEMOD.CPP web=: the cars tear
# it) -- [x, z, the road's height there, the lap's way through it x and z, the columns' faces either side of the road's middle (cells)]
WEB_X = OX + SCALE * 111 + 1.5                  # the columns' middles
WEB_HALF = 6.0
i_web = nearest((WEB_X, CORR_Z))
u_web = heading(i_web)
giant_webs = [[round(float(WEB_X), 2), float(CORR_Z), round(float(h[i_web]), 1), round(float(u_web[0]), 3), round(float(u_web[1]), 3), WEB_HALF]]
print('giant web', giant_webs)

# ... and cobwebs about the lap (TempleHalloween.Cobwebs: decor drawn only, off the road -- never over it -- where the camera following the
# car sees them), each [kind, x, z, y, turn (, a drape's span in cells; which of three a corner's or a wall's is)]:
#   - hanging from the tops of two walls across the corner they make (y the lower wall's top; a big one where both walls run on for two
#     layout cells and rise high enough): the rooms' and corridors' corners beside the road, their walls WEB_RISE layers or more over the
#     floor the road lies on, the corner facing the road with nothing between;
#   - across an alcove or a door gap off the road (y its floor): a layout cell of that floor between two such walls, open towards the road;
#   - between two of LBA1's columns beside the road (block 24, `ScriptRoundTrip templeblocks`) with nothing between them (y the top
#     thread's height): WEB_COLUMNS;
#   - flat on a wall, as LBA1's own cobwebs are (y the wall's top): either side of each doorway the road goes through, on the wall's face
#     the car comes up to; and on the walls beside the road where none of the others is near;
#   - a spider hanging on its thread in front of every third corner's and every third wall's (y its middle).
# No two nearer than WEB_APART cells, nor nearer the giant web than WEB_CLEAR.
CW_CORNER, CW_CORNER_BIG, CW_SHEET, CW_DRAPE, CW_SPIDER, CW_WALL = range(10, 16)
WEB_RISE = 3                                    # layers over the floor, at the least
WEB_APART = 7.0
WEB_CLEAR = 14.0
WEB_EDGE = 1.0                                  # cells from the road's edge to a web's nearest point, at the least ...
WEB_FAR = 14.0                                  # ... and from it to a corner, at the most
CORNER_REACH = {CW_CORNER: 1700 / 512, CW_CORNER_BIG: 2700 / 512}      # how far along each wall a corner's web reaches (cells)
WALL_HALF = 1500 / 512                          # how far either side of its middle a wall's reaches (cells)
WALL_EVERY = 12.0                               # a wall's beside the road about every this many cells of the lap
WEB_COLUMNS = [((107, 67), (111, 67)), ((81, 123), (81, 127)), ((94, 107), (98, 107))]
TURN_INTO = {(1, 1): 0, (1, -1): 1024, (-1, -1): 2048, (-1, 1): 3072}  # the turn that has a corner's web face into the room that way


def rise(lx, lz, floor_layer):
    t = column_top(lx, lz)
    return -99 if t is None or (lx, lz) in cut_columns else t - floor_layer


def in_sight(x0, z0, x1, z1, floor_layer):
    """Nothing rising two layers or more over floor_layer between (x0, z0) and (x1, z1) (island cells), the road's doorways apart."""
    steps = int(math.hypot(x1 - x0, z1 - z0) / 0.5) + 1
    return all(rise(*layout_of(x0 + (x1 - x0) * k / steps, z0 + (z1 - z0) * k / steps), floor_layer) < 2 for k in range(1, steps))


def road_off(x, z):
    """The lap's point nearest (x, z), and how far (x, z) is from the road's edge there (cells)."""
    d = np.hypot(lap_pts[:, 0] - x, lap_pts[:, 1] - z)
    i = int(d.argmin())
    return i, float(d[i] - half[i])


webs = []
for (lx, lz) in sorted(bits):
    t = column_top(lx, lz)
    if t is None or (lx, lz) in cut_columns: continue
    cx, cz = OX + SCALE * (lx + 0.5), OZ + SCALE * (lz + 0.5)
    i, off = road_off(cx, cz)
    if off > WEB_FAR + 3 or abs(top(t) - (h[i] - DECK)) > LAYER + 10: continue
    for sx in (-1, 1):
        for sz in (-1, 1):
            walls = [rise(lx + sx, lz, t), rise(lx, lz + sz, t), rise(lx + sx, lz + sz, t)]
            if min(walls) < WEB_RISE: continue
            vx, vz = OX + SCALE * (lx + (sx > 0)), OZ + SCALE * (lz + (sz > 0))
            i2, off2 = road_off(vx, vz)
            into = np.array([-sx, -sz]) / math.sqrt(2)
            to_road = (lap_pts[i2] - np.array([vx, vz])) / max(1e-6, off2 + half[i2])
            if off2 > WEB_FAR or float(into @ to_road) < 0.25 or not in_sight(vx - sx, vz - sz, *lap_pts[i2], t): continue
            low = min(walls[0], walls[1])
            big = rise(lx + sx, lz - sz, t) >= WEB_RISE and rise(lx - sx, lz + sz, t) >= WEB_RISE and low >= 5
            kind = CW_CORNER_BIG if big and off2 >= CORNER_REACH[CW_CORNER_BIG] + WEB_EDGE else CW_CORNER
            if off2 < CORNER_REACH[kind] + WEB_EDGE: continue
            webs.append((i2, [kind, float(vx), float(vz), float(top(t + low) - 40), TURN_INTO[(-sx, -sz)]], (sx, sz)))
    for ux, uz in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        vx_, vz_ = -uz, ux
        if min(rise(lx + vx_, lz + vz_, t), rise(lx - vx_, lz - vz_, t)) < WEB_RISE or rise(lx - ux, lz - uz, t) != 0: continue
        back = rise(lx + ux, lz + uz, t)
        if back < WEB_RISE and (back != 0 or ux < 0 or uz < 0): continue      # (an alcove; or a door gap, once)
        mx, mz = cx - ux * (1.2 if back > 0 else 0), cz - uz * (1.2 if back > 0 else 0)
        i2, off2 = road_off(mx, mz)
        to_road = (lap_pts[i2] - np.array([mx, mz])) / max(1e-6, off2 + half[i2])
        if off2 < WEB_EDGE + 1.5 or off2 > WEB_FAR or (back > 0 and float(np.array([-ux, -uz]) @ to_road) < 0.25): continue
        if not in_sight(mx - ux * 0.6, mz - uz * 0.6, *lap_pts[i2], t): continue
        webs.append((i2, [CW_SHEET, round(float(mx), 2), round(float(mz), 2), float(top(t)), turn_to(0, 0, ux, uz)], None))
for (ax_, az_), (bx_, bz_) in WEB_COLUMNS:
    x0_, z0_ = OX + SCALE * (ax_ + 0.5), OZ + SCALE * (az_ + 0.5)
    x1_, z1_ = OX + SCALE * (bx_ + 0.5), OZ + SCALE * (bz_ + 0.5)
    mx, mz = (x0_ + x1_) / 2, (z0_ + z1_) / 2
    i2, off2 = road_off(mx, mz)
    span = math.hypot(x1_ - x0_, z1_ - z0_)
    # (the top thread at the columns' capitals: 6.5 of their 8 layers up from the floor beside them)
    floor_layer = min(column_top(*c) for c in ((ax_ + (bz_ - az_) // 4, az_ + (bx_ - ax_) // 4), (ax_ - (bz_ - az_) // 4, az_ - (bx_ - ax_) // 4)) if column_top(*c) is not None)
    webs.append((i2, [CW_DRAPE, round(float(mx), 2), round(float(mz), 2), float(top(floor_layer) + 6.5 * LAYER), turn_to(0, 0, -(z1_ - z0_), x1_ - x0_), round(span, 1)], None))
# (the picks: along the lap, none too near another or the giant web; a spider in front of every third corner's)
cobwebs = []


def by_skeleton(x, z):
    """A skeleton sitting or hanging against a wall within SKELETON_CLEAR cells (the 2026-10-18 audit: corner webs drawn through the crypt's
    slumped skeletons)."""
    return any(d_[0] in (SK_HANGING, SK_SLUMPED) and math.hypot(x - d_[1], z - d_[2]) < SKELETON_CLEAR for d_ in decor)


SKELETON_CLEAR = 4.0
for i2, w, corner in sorted(webs, key=lambda e: (e[0], e[1][0])):
    if any(math.hypot(w[1] - c[1], w[2] - c[2]) < WEB_APART for c in cobwebs) or math.hypot(w[1] - WEB_X, w[2] - CORR_Z) < WEB_CLEAR: continue
    if by_skeleton(w[1], w[2]): continue
    # (a corner's one of three, in turn)
    if corner: w = w + [sum(1 for c in cobwebs if c[0] in (CW_CORNER, CW_CORNER_BIG)) % 3]
    cobwebs.append(w)
    if corner and sum(1 for c in cobwebs if c[0] in (CW_CORNER, CW_CORNER_BIG)) % 3 == 1:
        # (in front of the web's middle, its back to the room)
        sx, sz = corner
        reach = CORNER_REACH[w[0]]
        cobwebs.append([CW_SPIDER, round(w[1] - sx * reach * 0.6, 2), round(w[2] - sz * reach * 0.6, 2), w[3] - 1750, turn_to(0, 0, -sx, -sz)])
# (then webs flat on the walls, where none of those is near, each on a wall rising 3 to 6 layers over the floor -- not the corridor's
# frieze -- along its whole width: first either side of each doorway the road goes through, on the wall's face the car comes up to, clear
# of the doorway; then on the walls beside the road about every WALL_EVERY cells of the lap, not where a skeleton hangs)
for i in range(0, n, 2):
    lx, lz = layout_of(*pts[i])
    if (lx, lz) in cut_columns or (layout_of(*pts[(i + 4) % n]) not in cut_columns and layout_of(*pts[(i + 6) % n]) not in cut_columns): continue
    u = heading(i); nrm_ = np.array([-u[1], u[0]])
    # (the face: the first cut cell's edge ahead along the road)
    face = next((a for a in np.arange(0.25, 4.0, 0.25) if layout_of(*(pts[i] + u * a)) in cut_columns), None)
    floor_layer = column_top(lx, lz)
    if face is None or floor_layer is None: continue
    for sd in (1, -1):
        across_ = half[i] + CUT + WALL_HALF + 0.4
        x, z = pts[i] + u * (face - 0.3) + nrm_ * sd * across_
        rises = [rise(*layout_of(*(pts[i] + u * (face + 0.5) + nrm_ * sd * (across_ + s_))), floor_layer) for s_ in (-WALL_HALF, 0, WALL_HALF)]
        if min(rises) < 3 or max(rises) > 6: continue
        if any(math.hypot(x - c[1], z - c[2]) < WEB_APART for c in cobwebs) or math.hypot(x - WEB_X, z - CORR_Z) < WEB_CLEAR or by_skeleton(x, z): continue
        cobwebs.append([CW_WALL, round(float(x), 2), round(float(z), 2), float(top(floor_layer + min(rises)) - 40), turn_to(0, 0, -u[0], -u[1]),
                        sum(1 for c in cobwebs if c[0] == CW_WALL) % 3])
last_wall = -1e9
for i in range(0, n, 2):
    if S[i] - last_wall < WALL_EVERY: continue
    for sd in ((1, -1) if (i // 40) % 2 else (-1, 1)):
        found = wall_beside(i, sd, most=8.0)
        if found is None: continue
        off, layer, nrm = found
        x, z = pts[i] + nrm * (off - 0.3)
        tang = heading(i)
        rises = [rise(*layout_of(*(pts[i] + nrm * (off + 0.4) + tang * s_)), layer) for s_ in (-WALL_HALF, 0, WALL_HALF)]
        if min(rises) < 3 or max(rises) > 6: continue
        if any(math.hypot(x - c[1], z - c[2]) < WEB_APART for c in cobwebs) or math.hypot(x - WEB_X, z - CORR_Z) < WEB_CLEAR: continue
        if by_skeleton(x, z): continue
        cobwebs.append([CW_WALL, round(float(x), 2), round(float(z), 2), float(top(layer + min(rises)) - 40), turn_to(x, z, *pts[i]),
                        sum(1 for c in cobwebs if c[0] == CW_WALL) % 3])
        # (a spider hanging in front of every third one's middle)
        if sum(1 for c in cobwebs if c[0] == CW_WALL) % 3 == 2:
            sx_, sz_ = pts[i] + nrm * (off - 0.75)
            cobwebs.append([CW_SPIDER, round(float(sx_), 2), round(float(sz_), 2), cobwebs[-1][3] - 1750, turn_to(sx_, sz_, *pts[i])])
        last_wall = S[i]
        break
decor += cobwebs
print('cobwebs', len(cobwebs), {nm: sum(1 for c in cobwebs if c[0] == kk) for kk, nm in ((CW_CORNER, 'corner'), (CW_CORNER_BIG, 'big corner'), (CW_SHEET, 'sheet'), (CW_WALL, 'wall'),
                                                                                     (CW_DRAPE, 'drape'), (CW_SPIDER, 'spider'))})


# ---- the start: on the straight through the crypt's west room, heading east (the grid behind it, in the room)
i_start = nearest((410, CRYPT_Z), i_down_out)
print('start', i_start, tuple(np.round(pts[i_start], 1)), 'height', round(h[i_start]))
# ... the grid in two columns with a lane between them (the user, 2026-10-18: "let's have her start the race from the grid between the
# cars, with cars going either side of her"): the flag marshal stands in the road's middle ahead of the front row (Temple.TempleMarshal),
# the cars GRID_SIDE either side of it -- 2.75 cells: a gap of 3 cells between the columns' car boxes (1.24 cells round a car's middle), and
# a car's middle half a cell inside its rail (1.25 inside the 4.5-cell road's edge) -- pole GRID_FIRST behind the line, its nose (1.74
# cells ahead of its middle) a cell and a quarter behind her (RaceTrackScenes.GridSpot; the usual: 3 back, 1.75 across)
GRID_FIRST, GRID_SIDE = 5.0, 2.75
# ---- the pumpkin rows (RaceTrackScenes.MushroomSpots, a row every ~8 s of driving): each pumpkin of a row at least this far from its
# cube's edges (the 2026-10-18 audit: the row on the ramp down into the lower hall stood across x 384, the ramp running along that cube
# edge, so the car in one cube's scene saw only the two pumpkins in it)
ITEM_CUBE_MARGIN = 3.0
# ---- where the opponents wait while the player qualifies: in the slip lane, along its middle, heading the way the lap runs, on its deck
# (the road's height there) -- in the start's cube (the build writes no other cube's) and clear of the lane's edges and ends
slip_mid = (SLIP_FAR + SLIP_NEAR) / 2
pit_spots = []
for px_ in SLIP_SPOTS:
    i = nearest((px_, CRYPT_Z + slip_mid), i_down_out)
    k = [j for j in slip if abs(pts[j][0] - px_) <= 1.2]
    ok = len(k) > 0 and all(slip_lo[j] <= slip_mid - 1.2 and slip_hi[j] >= slip_mid + 1.2 for j in k) and CRYPT_Z + slip_mid - 1.2 >= 576
    print('pit spot', px_, CRYPT_Z + slip_mid, 'in the slip lane' if ok else 'NOT IN THE SLIP LANE')
    pit_spots.append([float(px_), float(CRYPT_Z + slip_mid), 1.0, 0.0, float(h[i])])

# ---- the lap from the start
order = np.r_[i_start:n, 0:i_start]
P, H, HF = pts[order], h[order], half[order]
plan = {
    'originCellX': 0, 'originCellZ': 0,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in P],
    'heights': [round(float(v), 1) for v in H],
    'start': 0,
    'asphaltHalf': HALF - 1.0, 'curbHalf': HALF - 0.25, 'vergeHalf': HALF + 0.5, 'blend': 2.0,
    'raised': [0, n - 1], 'raisedHalf': HALF,
    'raisedHalfs': [round(float(v), 2) for v in HF],
    'raisedStrips': [[float(a), float(b)] for a, b in zip(slip_lo[order], slip_hi[order])],
    'themes': [{'from': 0, 'to': n - 1, 'theme': 'halloween'}],
    'railCamera': [9, 2100, 7, 500, 1000],
    'templeCuts': sorted([list(c) for c in cuts]),
    'templeFloors': floors,
    'templeSkirts': skirts,
    'pitSpots': pit_spots,
    'gridFirst': GRID_FIRST, 'gridSide': GRID_SIDE,
    'itemCubeMargin': ITEM_CUBE_MARGIN,
    'halloween': {'decor': decor, 'spooks': spooks, 'pumpkins': True, 'dives': dives, 'scares': scares, 'haunt': True, 'giantWebs': giant_webs},
    'keepAbove': 0,
}
json.dump(plan, open(OUT, 'w'))
print('wrote', OUT, len(P), 'points')
