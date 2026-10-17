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
anchors = [  # (point, height of the floor the road lies on there), in the lap's order
    (0, FIRST), (i_west, FIRST),
    (i_hall_in - 6, FIRST), (i_hall_in + int(1.5 * 3072 / (GRADE * 512) / SP), LOWER),  # down into the lower hall (eased: 1.5 times as long)
    (i_hall_sw - int(1.5 * 3072 / (GRADE * 512) / SP), LOWER), (i_hall_sw, FIRST),      # up again before it turns west out of it
    (i_edge, FIRST),
    (i_crypt_w, CRYPT),                                                                 # down: the spiral west of the crypt
    (i_turn_n, CRYPT), (i_room_e, top(-5)),                                             # up into the crypt's east room
    (i_up_in, top(-5)),
    (n - 1, FIRST),                                                                     # up: the spiral east of it
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
cuts = set()
for i in range(0, n, 2):
    x, z = pts[i]
    reach = half[i] + CUT
    lo, hi = h[i] - DECK + 20, h[i] + HEAD
    for cz in range(int(math.floor(z - reach)), int(math.floor(z + reach)) + 1):
        for cx in range(int(math.floor(x - reach)), int(math.floor(x + reach)) + 1):
            if (cx + 0.5 - x) ** 2 + (cz + 0.5 - z) ** 2 > reach * reach: continue
            lx, lz = layout_of(cx, cz)
            b = bits.get((lx, lz), 0)
            if not b: continue
            for y in range(MINY, L['maxY'] + 1):
                if (b >> (y - MINY)) & 1 and top(y) > lo and top(y - 1) < hi: cuts.add((lx, y, lz))
print('cells cut', len(cuts))


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


def clear_of_road(x, z, margin):
    d = np.hypot(lap_pts[:, 0] - x, lap_pts[:, 1] - z) - half
    return float(d.min()) >= margin


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
    for off in (half[i] + 3.2, half[i] + 4.6, half[i] + 6.5, half[i] + 8.5):
        x, z = pts[i] + nrm * off
        kind = cycle[k % len(cycle)]
        top_ = flat_floor(x, z, 1 if kind in (CAULDRON, CROSS) else 0)
        if top_ is not None and abs(top(top_) - (h[i] - DECK)) <= LAYER + 10 and clear_of_road(x, z, 2.4):
            decor.append([kind, round(float(x), 2), round(float(z), 2), float(top(top_)), turn_to(x, z, *pts[i])])
            if kind in (GRAVE, CROSS):
                # (a grave's candles in front of it, toward the road)
                cx_, cz_ = pts[i] + nrm * (off - 1.4) + u * 0.8
                if flat_floor(cx_, cz_, 0) == top_ and clear_of_road(cx_, cz_, 1.6):
                    decor.append([CANDLES, round(float(cx_), 2), round(float(cz_), 2), float(top(top_)), turn_to(cx_, cz_, *pts[i])])
            k += 1; placed = True
            break
    if not placed:
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


spooks = []
# witches round the spirals, outside the road, a third and two thirds of the way up, throwing fire balls at the cars below them
for (cx_, cz_, r_), lo_, hi_ in ((SPIRAL_W, CRYPT, FIRST), (SPIRAL_E, top(-5), FIRST)):
    for frac, ph in ((0.42, 0.0), (0.85, math.pi)):
        spooks.append({'kind': 'witch', 'flags': 3, 'y': round(float(lo_ + (hi_ - lo_) * frac + 2500), 1), 'way': circle(cx_, cz_, r_ + 13, 24, ph)})
# witches over the halls: along the long corridor and across the lower hall, the crypt's rooms
for p0, p1, hy in (((W_END + 30, CORR_Z), (E_END - 20, CORR_Z), FIRST + 3200), ((330, 300), (395, 380), LOWER + 3400),
                   ((380, 560), (440, 600), CRYPT + 3400), ((470, 585), (530, 630), CRYPT + 3400)):
    spooks.append({'kind': 'witch', 'flags': 1, 'y': round(float(hy), 1), 'way': [list(map(float, p0)), list(map(float, p1))]})
# ghosts drifting across the road, a little over the cars
ghost_at = [nearest(p) for p in ((420, CORR_Z), (500, CORR_Z), (W_END, 360), (300, 340), (262, 372), (410, CRYPT_Z), (470, 618), (500, 560), (540, SPIRAL_E[1] + SPIRAL_E[2]))]
for j, i in enumerate(ghost_at):
    spooks.append({'kind': 'ghost', 'flags': 0, 'y': round(float(h[i] + 1300 + 300 * (j % 3)), 1), 'way': across(i, half[i] + 10)})
# bats wheeling high over the road
bat_at = [nearest(p) for p in ((450, CORR_Z), (360, 320), (270, 330), (400, 590), (500, 600), (520, 520))]
for j, i in enumerate(bat_at):
    spooks.append({'kind': 'bat', 'flags': 2, 'y': round(float(h[i] + 2600 + 400 * (j % 2)), 1), 'way': circle(pts[i][0], pts[i][1], 6 + 2 * (j % 3), 12, j)})
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


dives = []
for want in [nearest((412, CORR_Z)), nearest((505, CORR_Z)), nearest((276, 520), i_edge), o_down + int(0.62 * (o2 - o_down)),
             nearest((392, CRYPT_Z), i_down_out), o_up + int(0.55 * (o3 - o_up))]:
    # (the nearest place to the one wanted, along the lap, with the room over the road)
    i = next((want + s * j for j in range(0, 80, 2) for s in (1, -1) if dive_clear((want + s * j) % n)), None)
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


# ---- the start: on the straight through the crypt's west room, heading east (the grid behind it, in the room)
i_start = nearest((410, CRYPT_Z), i_down_out)
print('start', i_start, tuple(np.round(pts[i_start], 1)), 'height', round(h[i_start]))
# ---- where the opponents wait while the player qualifies (the lap has no pit lane): on the crypt room's floor north of the start straight,
# heading the way it runs -- each a column of floor at the crypt's level with nothing over it
PIT_Z = CRYPT_Z - 20
pit_spots = []
for px_ in (396, 407, 418):
    lx, lz = layout_of(px_, PIT_Z)
    ok = all(((bits.get((x_, z_), 0) >> (-8 - MINY)) & 1) and not any((bits.get((x_, z_), 0) >> (y - MINY)) & 1 for y in range(-7, -3))
             for x_ in range(lx - 1, lx + 2) for z_ in range(lz - 1, lz + 2))
    print('pit spot', px_, PIT_Z, 'clear floor' if ok else 'NOT CLEAR')
    pit_spots.append([float(px_), float(PIT_Z), 1.0, 0.0, float(CRYPT)])

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
    'themes': [{'from': 0, 'to': n - 1, 'theme': 'halloween'}],
    'railCamera': [9, 2100, 7, 500, 1000],
    'templeCuts': sorted([list(c) for c in cuts]),
    'pitSpots': pit_spots,
    'halloween': {'decor': decor, 'spooks': spooks, 'pumpkins': True, 'dives': dives, 'scares': scares, 'haunt': True},
    'keepAbove': 0,
}
json.dump(plan, open(OUT, 'w'))
print('wrote', OUT, len(P), 'points')
