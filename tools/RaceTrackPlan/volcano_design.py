# Volcano Island (SOUSCELB.ILE, the game's island 11 -- under Celebration Island; cells 448..576 each way, cube (7,8) not there). First
# from the user's sketch (2026-10-08: "Pink are jump points, green is track, orange is pitlane. For this track let's add lava balls shooting
# up and out of the lava at points and raining down onto the track"; repo root volcanoTrack.png, the editor's minimap turned half a turn and
# stretched: E:\dump\TEMP\volc\centreline.py).
#
# 2026-10-11, version 3. A redesign round a summit north of the main lava channel (2026-10-10, later) was "worse than the one we had
# previously"; the user: "Let's keep the lava waterfall but move it to the top of the bluff, to the left of where the crystal garden
# currently is. Let's add multiple layers of track to add some height to the track. Perhaps we can cross by the lava waterfall twice, once
# passing in front of it, and once under the lava flow. Perhaps we could have streams of lava randomly shooting up into the air sending lava
# balls showering down onto the track". So the lap of 2026-10-08/10 again, clockwise, with the plateau (the bluff, 12,750) in two layers:
#
#  - the start on the plateau's north straight, the pit lane on its right (inside the lap; easier to drive into since 2026-10-10: the deck
#    widening from the north-west bend's end, the fence from 7 cells on), the boost panel in it;
#  - the upper layer: right at the straight's east end, climbing south down the plateau's east edge, west, and north past the lava falls --
#    a crag on the plateau's west part, west of the crystal garden, 24,000 at its top, the lava pouring down its east face in three tiers
#    beside the road (RaceTrackLavaFall) -- then east over the climb (a crossing 3,600 over it) and north-east off the plateau onto the old
#    bridge east, 4,000 higher than it was: down to the jump over the main lava channel, a drop now;
#  - the rest of the lap as before: between the channels, the second jump onto the east edge (a crystal arch), the crystal field in the
#    lowland, the shore jump, the shore, up the volcano's flank onto the plateau's south edge;
#  - the lower layer: west along the south edge and north up the west side along the bluff's top, under the lava flow -- the crag's lava
#    carried west over the road on an aqueduct of rock and poured off the bluff into the sea (RaceTrackLavaFall.Aqueduct), dripping onto the
#    road under it.
#
# Lava geysers (RACEMOD.CPP lavageyser=): from the lava beside the lap, every so often a stream of lava shot up into the air, a shower of the
# game's lava balls coming down onto the road. The crystals (RaceTrackCrystals) of 2026-10-10: the lowland's field, the east edge's arch,
# what of the garden the upper layer leaves room for.
#
# Run with E:\dump\TEMP\volc holding H.npy and obstacles.npz (E:\dump\TEMP\otr\prep.py E:/dump/TEMP/volc 448 448 128); --pictures draws the
# lap over boxmap.png; --write writes docs/racetrack/volcano_track_plan.json.
import json, math, sys
import numpy as np

D = 'E:/dump/TEMP/volc/'
OUT = 'E:/dump/LBAAssembler/docs/racetrack/volcano_track_plan.json'
OX, OZ = 448, 448
H = np.nan_to_num(np.load(D + 'H.npy').astype(float), nan=0.0)
OB = np.load(D + 'obstacles.npz'); TOP, BOT, WHO, RQ = OB['top'], OB['bot'], OB['who'], int(OB['R'])
PRESENT = {(7, 7), (8, 7), (8, 8)}
SIZE = 128

def ground(x, z):
    xi, zi = min(SIZE - 1, max(0, int(x))), min(SIZE - 1, max(0, int(z))); fx, fz = x - xi, z - zi
    return H[zi, xi] * (1 - fx) * (1 - fz) + H[zi, xi + 1] * fx * (1 - fz) + H[zi + 1, xi] * (1 - fx) * fz + H[zi + 1, xi + 1] * fx * fz

STEP = 0.5
H0 = 4.75                               # (4.0 until 2026-10-10: "make the entire track slightly wider if we can")
ASPHALT, CURB = H0 - 1.0, H0 - 0.25
PIT = 4.5                               # the pit lane beside the plateau's north straight, on its right (south: inside the lap)
TOPD = 13000.0                          # the plateau's road (over its sand, 12,750)
NZ = 461.5                              # the north straight's race lanes' middle
SZ = NZ + PIT / 2                       # the deck's middle there (the pit lane south of the race lanes: the deck's middle south of theirs)
UP = 17300.0                            # the upper layer's top, past the falls and over the climb

def C(x, z): return (x - OX, z - OZ)
# the corners in the lap's order, each (x, z) and the radius it is rounded with (island cells)
K = [((543.5, 465.0), 4.0),             # the old bridge east's end: south, the jump over the main lava channel ...
     ((542.0, 505.0), 5.0),             # ... east-south-east: the jump over the second channel onto the east edge ...
     ((568.0, 516.5), 4.0),             # ... south along the east edge (clear of the island's edge, x 576), the crystal arch ...
     ((568.0, 551.0), 4.0),             # ... south-west: the jump down to the south shore ...
     ((556.0, 566.0), 4.0),             # ... west along the shore ...
     ((538.0, 566.0), 5.0),             # ... north up the volcano's flank ...
     ((537.0, 556.0), 4.0),
     ((553.0, 547.0), 4.0),
     ((555.0, 538.0), 4.0),             # ... north-west, climbing onto the plateau ...
     ((512.0, 497.0), 8.0),             # ... west along its south edge (the lower layer) ...
     ((462.0, 497.0), 6.0),             # ... north up its west side along the bluff, under the lava flow ...
     ((461.0, SZ), 6.0),                # ... east along its north straight (the start, the pit lane on the right) ...
     ((505.0, SZ), 5.0),                # ... right at its end, climbing south down the plateau's east edge (the upper layer) ...
     ((505.0, 486.0), 4.5),             # ... west ...
     ((487.0, 486.0), 4.0),             # ... north past the lava falls (on the left) ...
     ((487.0, 475.0), 4.0),             # ... east, over the climb ...
     ((514.0, 475.0), 4.0),             # ... north-east off the plateau ...
     ((520.0, 465.0), 4.0)]             # ... east along the old bridge
K = [(C(*p), r) for p, r in K]

def rounded_polyline(K):
    n = len(K); P = [np.array(p, float) for p, r in K]; out = []
    arcs = []
    for i in range(n):
        a, b, c = P[i - 1], P[i], P[(i + 1) % n]; r = K[i][1]
        u1 = (b - a) / np.linalg.norm(b - a); u2 = (c - b) / np.linalg.norm(c - b)
        turn = math.atan2(u1[0] * u2[1] - u1[1] * u2[0], u1 @ u2)
        t = r * math.tan(abs(turn) / 2)
        p1, p2 = b - u1 * t, b + u2 * t
        nrm = np.array([-u1[1], u1[0]]) * (1 if turn > 0 else -1)
        arcs.append((p1, p2, p1 + nrm * r, r, turn))
    for i in range(n):
        p1, p2, centre, r, turn = arcs[i]
        a0 = math.atan2(p1[1] - centre[1], p1[0] - centre[0])
        m = max(2, int(round(r * abs(turn) / 0.02)))
        for k in range(m): out.append(centre + r * np.array([math.cos(a0 + turn * k / m), math.sin(a0 + turn * k / m)]))
        q1, q2 = p2, arcs[(i + 1) % n][0]
        if np.dot(q2 - q1, P[(i + 1) % n] - P[i]) < -1e-6: sys.exit(f'corners {i} and {i + 1}: their roundings overlap')
        m = max(1, int(round(np.linalg.norm(q2 - q1) / 0.02)))
        for k in range(m): out.append(q1 + (q2 - q1) * k / m)
    return np.array(out)

fine = rounded_polyline(K)
START = (490.0, SZ)                     # the start line on the north straight (heading east: its grid behind it, west, on the level)
k0 = int(np.argmin(np.hypot(fine[:, 0] - C(*START)[0] + 20, fine[:, 1] - C(*START)[1])))
fine = np.roll(fine, -k0, axis=0)
seg = np.linalg.norm(np.diff(np.vstack([fine, fine[:1]]), axis=0), axis=1)
cum = np.concatenate([[0], np.cumsum(seg)]); total = cum[-1]
N = int(round(total / STEP)); s = np.arange(N) * total / N
X = np.interp(s, cum, np.append(fine[:, 0], fine[0, 0])); Z = np.interp(s, cum, np.append(fine[:, 1], fine[0, 1]))
T = np.stack([np.gradient(X), np.gradient(Z)], 1); T /= np.linalg.norm(T, axis=1)[:, None]
Nn = np.stack([-T[:, 1], T[:, 0]], 1)    # across the road (the builder's Across, (-Tz, Tx): south when heading east -- the pit lane is on +Nn)
def nearest(p, heading=None):
    p = C(*p); d = np.hypot(X - p[0], Z - p[1])
    if heading is not None: d = d + 100 * (T @ np.array(heading) < 0.7)
    return int(np.argmin(d))
def ahead(a, b): return ((b - a) % N) * STEP
def between(k, a, b): return ahead(a, k) <= ahead(a, b)
def ease(f): f = min(1, max(0, f)); return f * f * (3 - 2 * f)

E, W_, S_, N_ = (1, 0), (-1, 0), (0, 1), (0, -1)
SE_ = (0.915, 0.405); SW_ = (-0.625, 0.781)
# ---- widths: the usual, and the north straight with its pit lane on its right (south: +Nn heading east), easy to drive into: the deck
# widened over 8 cells from the north-west bend's end, the fence 7 cells on from where it is full width (a car keeping to the right out of
# the bend is in the pit lane without turning)
def ramp(a, b, k): return ease(ahead(a, k) / ahead(a, b))
HALF = np.full(N, H0)
PIT_HALF = H0 + PIT / 2
PIT_MID = PIT_HALF - PIT / 2
iPit0 = nearest((466.0, SZ), E); iPit1 = nearest((474.0, SZ), E); iPit2 = nearest((499.0, SZ), E); iPit3 = nearest((503.5, SZ), E)
iFence0 = nearest((481.0, SZ), E); iFence1 = nearest((497.0, SZ), E)
for k in range(N):
    if between(k, iPit0, iPit1): HALF[k] = max(HALF[k], H0 + (PIT_HALF - H0) * ramp(iPit0, iPit1, k))
    elif between(k, iPit1, iPit2): HALF[k] = PIT_HALF
    elif between(k, iPit2, iPit3): HALF[k] = max(HALF[k], PIT_HALF + (H0 - PIT_HALF) * ramp(iPit2, iPit3, k))
PIT_WAIT = (482.5, 486.0, 489.5)        # the opponents' waiting spots in the pit lane, within the fence, behind the line (cube (7,7))

MARK = {
    'start':    nearest(START, E),
    'climb0':   nearest((505, 468), S_),          # the upper layer's climb ...
    'falls0':   nearest((487, 485), N_),          # ... past the falls ...
    'falls1':   nearest((487, 477), N_),
    'cross':    nearest((505, 475), E),           # ... over the climb ...
    'bridge':   nearest((530, 465), E),           # ... the old bridge east
    'cEnd':     nearest((542, 500), S_),          # the stretch between the channels
    'eastN':    nearest((568, 524), S_),          # the east edge
    'eastS':    nearest((568, 546), S_),
    'shoreE':   nearest((548, 566), W_),          # the south shore
    'shoreW':   nearest((538, 563), N_),
    'flank1':   nearest((554, 543), N_),          # up the volcano's flank
    'flank2':   nearest((530, 520), (-0.72, -0.69)),
    'plateauS': nearest((505, 497), W_),          # onto the plateau
    'westS':    nearest((461.5, 490), N_),        # the west side, under the lava flow
    'westN':    nearest((461.5, 472), N_),
}

# ---- heights, close over the ground (as Otringal's version 2): CLEAR over the median of the ground across the deck (a rock at one rail is
# cut away, RaisedCut), at least LAVA_MIN over the lava (its ground is 0..700) -- and the upper layer lifted: from the climb's start up at
# LIFT_GRADE (under the grade's limit: the spreading keeps the start straight level) to UP past the falls, held there over the crossing --
# then spread out at GRADE each way and eased
CLEAR, LAVA, LAVA_MIN, GRADE, LIFT_GRADE = 400.0, 700.0, 1400.0, 0.17, 0.155
gs = np.array([np.median([ground(X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o) for o in np.linspace(-HALF[k], HALF[k], 9)]) for k in range(N)])
floor = np.where(gs < LAVA, LAVA_MIN, np.maximum(gs + CLEAR, LAVA_MIN))
lift_end = (MARK['cross'] + int(4 / STEP)) % N
for k in range(N):
    if between(k, MARK['climb0'], lift_end):
        floor[k] = max(floor[k], min(UP, TOPD + 150 + ahead(MARK['climb0'], k) * 512 * LIFT_GRADE))
# (across the channel jump's gap the spreading lets the road drop JUMP_DROP more: the bridge comes off the upper layer 4,000 higher than it
# did, and the jump takes that -- spread across it at the grade, it held the whole east half of the lap 4,000 over its ground, and with no
# spreading across it the stretch between the channels sank into its rocks; the gap a straight line for the easing)
JUMP_DROP = 4000.0
DROP = [('channel', (543.3, 474.7, S_), (542.6, 488.7, S_))]
drops = [(nearest(l[:2], l[2]), nearest(d_[:2], d_[2])) for name, l, d_ in DROP]
def step_drop(k):
    for a, b in drops:
        if between(k, a, b) and k != b: return JUMP_DROP / max(1, int(ahead(a, b) / STEP))
    return 0.0
g = GRADE * STEP * 512
F = floor.copy()
for _ in range(3):
    for k in range(N): j = (k + 1) % N; F[j] = max(F[j], F[k] - g - step_drop(k))
    for k in range(N - 1, -1, -1): j = (k + 1) % N; F[k] = max(F[k], F[j] - g - step_drop(k))
for a, b in drops:
    n_ = int(ahead(a, b) / STEP)
    for q in range(1, n_): F[(a + q) % N] = F[a] + (F[b] - F[a]) * q / n_
Y = F.copy()
for k in range(N): Y[k] = np.mean(F[[(k + d) % N for d in range(-6, 7)]])
Y = np.maximum(Y, F - 60)

# ---- the jumps the engine carries the car over: (name, foot, lip, landing lip, landing foot as (x, z, heading)), the ramp's angle
def flight_heights(iFoot, iLip, iLand, iLandFoot, theta, top_needed, land_at=None):
    y0, y3 = Y[iFoot], Y[iLandFoot]
    k1 = ahead(iFoot, iLip) * 512; L_ = ahead(iLip, iLand) * 512; k2 = ahead(iLand, iLandFoot) * 512
    R1 = k1 / math.sin(theta); yl = y0 + R1 * (1 - math.cos(theta)); sl = math.tan(theta)
    def at(d):
        if d <= k1: return y0 + R1 - math.sqrt(max(0, R1 * R1 - d * d))
        if d <= k1 + L_:
            u = d - k1
            a = (land_y - yl - sl * L_) / (L_ * L_)
            return yl + sl * u + a * u * u
        u = d - k1 - L_; f = u / k2
        return land_y + (y3 - land_y) * ease(f)
    land_y = max(y3 + 300, min(yl, top_needed)) if land_at is None else land_at
    return at, yl, land_y

# (the channel jump's foot after the bridge's bend south, x 543.5: the bend's end at z 469)
JUMPS = [('channel', (543.4, 469.5, S_), (543.3, 474.7, S_), (542.6, 488.7, S_), (542.4, 492.0, S_), 14.0, None),
         ('second', (547.5, 507.4, SE_), (550.4, 508.7, SE_), (562.5, 514.0, SE_), (564.5, 514.9, SE_), 18.0, None),
         ('shore', (567.4, 552.8, SW_), (565.6, 555.0, SW_), (559.6, 562.5, SW_), (558.6, 563.7, SW_), 16.0, None)]
gap = np.zeros(N, bool); arcs = []
for name, f, l, ld, lf, deg, landing in JUMPS:
    iF, iL, iD, iDF = nearest(f[:2], f[2]), nearest(l[:2], l[2]), nearest(ld[:2], ld[2]), nearest(lf[:2], lf[2])
    at, yl, land_y = flight_heights(iF, iL, iD, iDF, math.radians(deg), Y[iDF] + 600)
    k = iF
    while True:
        Y[k] = at(ahead(iF, k) * 512)
        if k == iDF: break
        k = (k + 1) % N
    k = (iL + 1) % N
    while k != iD: gap[k] = True; k = (k + 1) % N
    arcs.append((name, iF, iL, iD, iDF))

# ---- the lava falls (RaceTrackLavaFall): a crag on the plateau's west part, west of the crystal garden, the falls down its east face beside
# the upper layer's way north (its west rail 1.25 cells off the face), 24,000 at its top, three tiers; and its lava carried west on an
# aqueduct over the west side (the lower layer: the deck 13,150, the aqueduct's underside 15,600) and poured off the bluff into the sea
FALL_AT, FALL_W = (481.5, 481.0), 12.0
lava_fall = {'at': [FALL_AT[0] - OX, FALL_AT[1] - OZ], 'facing': [1.0, 0.0], 'width': FALL_W, 'top': 24000.0, 'depth': 11.5, 'wing': 1.5,
             'pool': 1.0, 'tiers': 3, 'tierDepth': 2.0, 'rock': [530.0 - OX, 466.0 - OZ], 'lava': [515.0 - OX, 479.0 - OZ],
             'aqueduct': {'to': [452.5 - OX, 481.0 - OZ], 'surface': 16300.0, 'width': 1700.0}}

# ---- lava: single balls (lavaBalls) and geysers (lavaGeysers: a stream of lava shot up into the air, a shower of `balls` lava balls onto
# the road) -- each source (x, z[, its height]), the road it rains on (from a point to a point: the race-track mode picks a place along it
# for each ball), every so many ms
def P_(x, z, h=None): return [x - OX, z - OZ] + ([h] if h is not None else [])
def J(name, which): return [a for a in arcs if a[0] == name][0][which]
LAVA = [  # off the aqueduct over the west side, dripping onto the lower layer under it
        (P_(461.5, 481.0, 15700.0), nearest((461.5, 486), N_), nearest((461.5, 476), N_), 2400),
          # out of the falls' pool, onto the upper layer beside it
        (P_(483.0, 481.0), MARK['falls0'], MARK['falls1'], 3200)]
GEYSERS = [  # the main channel, under the bridge and the jump over it
           (P_(526.0, 480.0), MARK['bridge'], J('channel', 1), 7000, 5),
           (P_(536.0, 480.0), J('channel', 4), MARK['cEnd'], 7600, 4),
             # the second channel and the lowland
           (P_(556.0, 492.0), MARK['cEnd'], J('second', 1), 8200, 4),
           (P_(559.0, 530.0), MARK['eastN'], MARK['eastS'], 6800, 5),
             # the shore's lava
           (P_(556.0, 572.0), MARK['shoreE'], MARK['shoreW'], 7400, 4),
           (P_(526.0, 560.0), MARK['shoreW'], MARK['flank1'], 8000, 5)]
lava = [{'source': src, 'from': int(a), 'to': int(b), 'every': every} for src, a, b, every in LAVA]
geysers = [{'source': src, 'from': int(a), 'to': int(b), 'every': every, 'balls': n} for src, a, b, every, n in GEYSERS]

# ---- the crystals (RaceTrackCrystals): the field in the lowland, the arch on the east edge, the garden inside the upper layer's loop and on
# the plateau's south part
CRYSTALS = [(557.5, 516.0, 2, 3.0, 300, 10), (551.0, 517.0, 1, 3.5, 250, 5), (552.0, 524.0, 2, 3.5, 80, 7), (557.0, 529.0, 1, 3.0, 20, 8),
            (558.0, 538.0, 1, 3.0, 190, 9), (553.0, 540.0, 2, 3.5, 330, 6),
            (496.0, 480.5, 2, 3.0, 160, 0), (500.5, 478.5, 1, 2.5, 280, 3), (474.0, 492.0, 1, 2.5, 20, 0), (480.0, 492.0, 2, 2.5, 100, 0)]
crystals = [{'at': [x - OX, z - OZ], 'body': b, 'scale': sc, 'turn': t, 'lean': l} for x, z, b, sc, t, l in CRYSTALS]
arches = [{'at': [568.0 - OX, 530.0 - OZ], 'spread': 3.0}]

# ---- the boost panel (RACEMOD.CPP boostpanel=: a car with two wheels on it is at 120 km/h at once, held a while): in the pit lane, past
# the start line and the opponents' waiting spots -- its first and last point, how far across the road (Across, cells)
BOOST = [(491.5, 496.5)]
boost = [[int(nearest((a, SZ), E)), int(nearest((b, SZ), E)), PIT_MID - 1.5, PIT_MID + 1.5] for a, b in BOOST]

grade = (np.roll(Y, -1) - Y) / (STEP * 512)
ride = ~np.zeros(N, bool)
for name, iF, iL, iD, iDF in arcs:
    for k in range(N):
        if between(k, iF, iDF): ride[k] = False

# ---- checks
out = [f'lap {total:.1f} cells, {N} points; heights {Y.min():.0f} to {Y.max():.0f}; steepest driven {np.abs(grade[ride]).max() * 100:.1f} %']
steep = sorted(((abs(grade[k]), k) for k in range(N) if ride[k]), reverse=True)
sp = []
for g_, k in steep:
    if all(min(abs(k - j), N - abs(k - j)) > 20 for j in sp): sp.append(k)
    if len(sp) == 5: break
out.append('  steepest at: ' + ', '.join(f'{abs(grade[k]) * 100:.1f}% point {k} ({X[k] + OX:.0f}, {Z[k] + OZ:.0f})' for k in sp))
def edge_dist(x, z):
    x, z = x + OX, z + OZ; best = 1e9
    for dz in (-1, 0, 1):
        for dx in (-1, 0, 1):
            q = (int(x // 64) + dx, int(z // 64) + dz)
            if q in PRESENT: continue
            qx = min(max(x, q[0] * 64), q[0] * 64 + 64); qz = min(max(z, q[1] * 64), q[1] * 64 + 64)
            best = min(best, math.hypot(x - qx, z - qz))
    return best
edge = np.array([edge_dist(x, z) - HALF[k] for k, (x, z) in enumerate(zip(X, Z))])
out.append(f'  the rail nearest a missing cube: {edge.min():.1f} cells at ({X[edge.argmin()] + OX:.1f}, {Z[edge.argmin()] + OZ:.1f})')
clear = sorted((Y[k] - max(ground(X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o) for o in np.linspace(-HALF[k], HALF[k], 9)), k) for k in range(N) if not gap[k])
out.append('  the deck under the ground: ' + ', '.join(f'{c:.0f} at ({X[k] + OX:.1f}, {Z[k] + OZ:.1f}) point {k}' for c, k in clear[:6] if c < 300))
hits = {}
for k in range(N):
    if gap[k]: continue
    for o in np.linspace(-HALF[k] - 0.3, HALF[k] + 0.3, 17):
        x, z = X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o
        i, j = int(z * RQ), int(x * RQ)
        if not (0 <= i < TOP.shape[0] and 0 <= j < TOP.shape[1]) or TOP[i, j] < 0: continue
        if TOP[i, j] > Y[k] - 350 and BOT[i, j] < Y[k] + 1400:
            b = int(WHO[i, j]); hits.setdefault(b, []).append((round(float(x + OX), 1), round(float(z + OZ), 1), int(TOP[i, j]), int(Y[k])))
out.append('  decor in the road\'s space (body: places, e.g.): ' + '; '.join(f'{b}: {len(h)} {h[0]}' for b, h in sorted(hits.items())))
for name, iF, iL, iD, iDF in arcs:
    flight = [Y[q % N] for q in range(iL, iD + (N if iD < iL else 0))]
    out.append(f'  jump {name}: lip {Y[iL]:.0f}, landing {Y[iD]:.0f}, top {max(flight):.0f}, {ahead(iL, iD):.1f} cells; foot {Y[iF]:.0f} landing foot {Y[iDF]:.0f}')
# (where the lap passes over or under itself: the nearest points of two stretches apart round the lap, and how far over one the other is)
idx = np.arange(N); close = []; cross = []
for i in range(0, N, 2):
    if gap[i]: continue
    dd = np.hypot(X - X[i], Z - Z[i]); sep = np.minimum(np.abs(idx - i), N - np.abs(idx - i))
    near_ = (sep > 40) & (dd < HALF + HALF[i] + 0.6) & ~gap
    if (near_ & (np.abs(Y - Y[i]) < 2600)).any(): close.append((dd[near_ & (np.abs(Y - Y[i]) < 2600)].min(), i, int(np.argmax(near_ & (np.abs(Y - Y[i]) < 2600)))))
    if near_.any(): j = int(np.argmax(near_)); cross.append((i, j, Y[i] - Y[j]))
for dmin, i, j in sorted(close)[:4]:
    out.append(f'  CLASH: ({X[i] + OX:.1f}, {Z[i] + OZ:.1f}) {Y[i]:.0f} and ({X[j] + OX:.1f}, {Z[j] + OZ:.1f}) {Y[j]:.0f}: {dmin:.1f} cells apart')
over = [c for c in cross if c[2] > 0]
if over:
    least = min(over, key=lambda c: c[2])
    out.append(f'  layers: {len(over)} points over another stretch; the least clearance {least[2]:.0f} at ({X[least[0]] + OX:.1f}, {Z[least[0]] + OZ:.1f}) over ({X[least[1]] + OX:.1f}, {Z[least[1]] + OZ:.1f})')
for l in lava + geysers:
    sx, sz = l['source'][:2]
    out.append(f'  lava {"geyser" if "balls" in l else "ball"} from ({sx + OX:.0f}, {sz + OZ:.0f}) onto points {l["from"]}..{l["to"]}, {min(np.hypot(X - sx, Z - sz)):.1f} cells from the road')
for m, k in MARK.items():
    out.append(f'    {m:9s} point {k:4d} ({X[k] + OX:6.1f}, {Z[k] + OZ:6.1f}) {Y[k]:6.0f} (ground {ground(X[k], Z[k]):5.0f})')
out.append(f'  pit lane points {iPit1}..{iPit2}, its fence {iFence0}..{iFence1}; start point {MARK["start"]}')
print('\n'.join(out))

plan = {
    'originCellX': OX, 'originCellZ': OZ,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in zip(X, Z)],
    'heights': [round(float(y), 1) for y in Y],
    'maxGrade': round(float(np.abs(grade).max()) + 0.05, 3),
    'raised': [0, N - 1], 'raisedHalf': H0, 'asphaltHalf': ASPHALT, 'curbHalf': CURB,
    'raisedHalfs': [round(float(h), 3) for h in HALF],
    'arcJumps': [[int(iF), int(iL), int(iD), int(iDF), 0] for name, iF, iL, iD, iDF in arcs],
    'start': int(MARK['start']),
    # the stripe between the pit lane and the race lanes (south of them, +Across), first and last point (the fence's); the opponents'
    # waiting spots in the pit lane's middle, facing along the straight (east), on the deck; Citadel Island's white fence along the
    # stripe; the grid on the race lanes (north of the deck's middle, -Across)
    'pitStripe': [int(iFence0), int(iFence1), round(float(PIT_HALF - PIT), 3)],
    'pitSpots': [[x - OX, round(SZ + PIT_MID - OZ, 3), 1.0, 0.0, TOPD] for x in PIT_WAIT],
    'pitFence': True,
    'gridShift': -PIT / 2,
    'raisedCut': True,
    'lavaBalls': lava,
    'lavaGeysers': geysers,
    'lavaFalls': [lava_fall],
    'crystals': crystals,
    'crystalArches': arches,
    'boostPanels': boost,
}
if '--write' in sys.argv:
    json.dump(plan, open(OUT, 'w'))
    print('written', OUT)
np.savez(D + 'lap.npz', X=X + OX, Z=Z + OZ, Y=Y, HALF=HALF, gap=gap)

if '--pictures' in sys.argv:
    from PIL import Image, ImageDraw
    Zm = 8
    img = Image.open(D + 'boxmap.png').convert('RGB')
    d = ImageDraw.Draw(img)
    P = lambda x, z: (x * Zm, z * Zm)
    lo, hi = Y.min(), Y.max()
    for k in range(N):
        j = (k + 1) % N
        t = (Y[k] - lo) / (hi - lo)
        col = (int(255 * t), int(255 * (1 - abs(2 * t - 1))), int(255 * (1 - t)))
        if gap[k]: col = (255, 255, 255)
        for o in (-HALF[k], HALF[k]):
            d.line([P(X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o), P(X[j] + Nn[j, 0] * o, Z[j] + Nn[j, 1] * o)], fill=col, width=2 if not gap[k] else 1)
    for l in lava + geysers:
        sx, sz = l['source'][:2]
        d.ellipse([P(sx - 1, sz - 1), P(sx + 1, sz + 1)], outline=(255, 80, 0) if 'balls' in l else (255, 200, 0), width=3)
        for k in (l['from'], l['to']): d.line([P(sx, sz), P(X[k], Z[k])], fill=(255, 120, 0), width=1)
    for m, k in MARK.items(): d.text(P(X[k], Z[k]), m, fill=(255, 255, 255))
    st = plan['pitStripe']
    for k in range(st[0], st[1]): d.point(P(X[k] + Nn[k, 0] * st[2], Z[k] + Nn[k, 1] * st[2]), fill=(255, 255, 255))
    for a, b, lo_, hi_ in boost:
        for k in range(a, b + 1):
            for o in np.linspace(lo_, hi_, 7): d.point(P(X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o), fill=(0, 255, 255))
    fx, fz = lava_fall['at']; half = FALL_W / 2 + lava_fall['wing']
    d.rectangle([P(fx - lava_fall['depth'], fz - half), P(fx, fz + half)], outline=(160, 160, 160), width=2)
    aq = lava_fall['aqueduct']['to']
    d.line([P(fx - lava_fall['depth'], fz), P(aq[0], aq[1])], fill=(255, 90, 0), width=6)
    for c in crystals:
        cx, cz = c['at']
        d.ellipse([P(cx - 0.8, cz - 0.8), P(cx + 0.8, cz + 0.8)], fill=(230, 40, 100))
    img.save(D + 'design.png')
