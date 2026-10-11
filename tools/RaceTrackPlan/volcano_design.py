# Volcano Island (SOUSCELB.ILE, the game's island 11 -- under Celebration Island; cells 448..576 each way, cube (7,8) not there). First
# from the user's sketch (2026-10-08: "Pink are jump points, green is track, orange is pitlane. For this track let's add lava balls shooting
# up and out of the lava at points and raining down onto the track"; repo root volcanoTrack.png, the editor's minimap turned half a turn and
# stretched: E:\dump\TEMP\volc\centreline.py).
#
# 2026-10-11, version 3: the lap of 2026-10-08/10 again (a redesign round a summit was "worse than the one we had previously"), the
# plateau (the bluff, 12,750) in layers -- "Let's keep the lava waterfall but move it to the top of the bluff, to the left of where the
# crystal garden currently is. Let's add multiple layers of track ... cross by the lava waterfall twice, once passing in front of it, and once
# under the lava flow ... streams of lava randomly shooting up into the air sending lava balls showering down onto the track".
#
# Version 4, the same day: "Let's change the upward loop so it's bigger and loops around an extra time, let's also remove the jump down and
# have a slope instead, jumps downwards feel unnatural to drive and we should try to avoid them if possible. Let's also have a double spiral
# back down again. Let's also add in some red crystal shards that shoot up into the air from the volcano, then fall back down onto the track,
# we should see their shadows on the track getting slowly bigger until they hit with an explosion that stuns any cars in the vicinity".
# Clockwise:
#
#  - the start on the plateau's north straight, the pit lane on its right (inside the lap, easy to drive into), the boost panel in it;
#  - the climb: right at the straight's east end into a loop over the plateau's east part, twice round -- its second turn over its first,
#    HELIX_PITCH higher -- each time north past the lava fall down the crag's east face (RaceTrackLavaFall), then east over the loop's start;
#  - the double spiral down: north of the main lava channel, two and a half turns round at the grade's limit, each over the one below;
#  - the slope (the jump over the channel gone: a bridge down over it), between the channels, the second jump onto the east edge (a crystal
#    arch, the crystal field in the lava below), the shore jump, the shore -- both jumps landing as high as they leave now ("jumps
#    downwards feel unnatural"), the road going down after them;
#  - up the volcano's flank onto the plateau's south edge and its west side, under the crag's lava carried over the road on an aqueduct.
#
# Lava: geysers' streams and showers, single balls off the aqueduct and out of the fall's pool. Crystal shards (RACEMOD.CPP crystalshards=):
# red crystal shards shot up out of the volcano -- the crag's lake of lava -- falling onto the road round it, each one's shadow growing slowly
# on the road where it will land, an explosion there that stuns the cars near it.
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
HELIX_PITCH = 4800.0                    # a spiral's turns this far apart at least (the camera following the car fits between them: the
                                        # Elevator Platform's helix)

def C(x, z): return (x - OX, z - OZ)
# the corners in the lap's order, each (x, z) and the radius it is rounded with (island cells)
# the double spiral down: an ellipse round (SPIRAL_X, SPIRAL_Z), SPIRAL_RX by SPIRAL_RZ cells, clockwise from its west side, two and a half
# turns to its east side, each corner 30 degrees on (rounded SPIRAL_R)
SPIRAL_X, SPIRAL_Z, SPIRAL_RX, SPIRAL_RZ, SPIRAL_R, SPIRAL_TURNS = 534.0, 461.5, 10.0, 7.0, 3.0, 2.5
spiral = [((SPIRAL_X + SPIRAL_RX * math.cos(math.pi + q * math.pi / 6), SPIRAL_Z + SPIRAL_RZ * math.sin(math.pi + q * math.pi / 6)), SPIRAL_R)
          for q in range(1, int(SPIRAL_TURNS * 12))]
# the climb's loop: its corners, twice round
LOOP = [((511.0, 487.0), 4.5), ((487.5, 487.0), 4.0), ((487.5, 474.0), 4.0)]
K = ([((542.0, 505.0), 5.0),             # off the spiral's foot, down the slope over the main lava channel: east-south-east, the second jump
      ((568.0, 516.5), 4.0),             # ... south along the east edge (clear of the island's edge, x 576), the crystal arch ...
      ((568.0, 551.0), 4.0),             # ... south-west: the shore jump ...
      ((556.0, 566.0), 4.0),             # ... west along the shore ...
      ((538.0, 566.0), 5.0),             # ... north up the volcano's flank ...
      ((537.0, 556.0), 4.0),
      ((553.0, 547.0), 4.0),
      ((555.0, 538.0), 4.0),             # ... north-west, climbing onto the plateau ...
      ((512.0, 497.0), 8.0),             # ... west along its south edge ...
      ((462.0, 497.0), 6.0),             # ... north up its west side along the bluff, under the lava flow ...
      ((461.0, SZ), 6.0),                # ... east along its north straight (the start, the pit lane on the right) ...
      ((511.0, SZ), 5.0)]                # ... right at its end into the climb's loop: south, west, north past the falls, east ...
     + LOOP + [((511.0, 474.0), 4.0)] + LOOP   # ... and round again over it ...
     + [((523.0, 474.0), 4.0)]           # ... east off it over its start, left (north) into the double spiral down ...
     + spiral)                           # ... round it to its east side, and south off it
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
# (the point nearest p within `window` cells after the point `after`: a spiral's turns are over each other)
def nearest_after(p, heading, after, window):
    best, bd = after, 1e9
    for q in range(int(window / STEP)):
        k = (after + q) % N
        d = math.hypot(X[k] - (p[0] - OX), Z[k] - (p[1] - OZ)) + (100 if heading is not None and T[k] @ np.array(heading) < 0.7 else 0)
        if d < bd: bd, best = d, k
    return best
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

MARK = {'start': nearest(START, E)}
MARK['climb0'] = nearest_after((508.0, SZ + 1), S_, MARK['start'], 40)            # the climb's loop ...
MARK['t1W0'] = nearest_after((487.5, 485.0), N_, MARK['climb0'], 90)               # ... past the falls the first time ...
MARK['t1W1'] = nearest_after((487.5, 477.0), N_, MARK['t1W0'], 20)
MARK['t2E0'] = nearest_after((511.0, 478.0), S_, MARK['t1W1'], 40)                 # ... the second time round ...
MARK['t2W0'] = nearest_after((487.5, 485.0), N_, MARK['t2E0'], 60)
MARK['t2W1'] = nearest_after((487.5, 477.0), N_, MARK['t2W0'], 20)
MARK['top'] = nearest_after((517.0, 474.0), E, MARK['t2W1'], 40)                   # ... off it over its start: the top
MARK['dIn'] = nearest_after((523.0, 466.0), N_, MARK['top'], 20)                   # the double spiral
MARK['dS1'] = nearest_after((534.0, 467.5), W_, MARK['dIn'] + int(70 / STEP), 60)  # (its second time along its south side)
MARK['dOut'] = nearest_after((545.0, 466.0), S_, MARK['dIn'] + int(120 / STEP), 60)  # ... off it south
MARK['slope'] = nearest_after((543.5, 485.0), S_, MARK['dOut'], 30)                # down the slope over the channel
MARK['cEnd'] = nearest_after((542.0, 500.0), S_, MARK['dOut'], 50)                 # the stretch between the channels
MARK['eastN'] = nearest((568, 524), S_)                                            # the east edge
MARK['eastS'] = nearest((568, 546), S_)
MARK['shoreE'] = nearest((548, 566), W_)                                           # the south shore
MARK['shoreW'] = nearest((538, 563), N_)
MARK['flank1'] = nearest((554, 543), N_)                                           # up the volcano's flank
MARK['flank2'] = nearest((530, 520), (-0.72, -0.69))
MARK['plateauS'] = nearest((505, 497), W_)                                         # onto the plateau
MARK['westS'] = nearest((461.5, 490), N_)                                          # the west side, under the lava flow
MARK['westN'] = nearest((461.5, 472), N_)

# ---- heights, close over the ground (as Otringal's version 2): CLEAR over the median of the ground across the deck (a rock at one rail is
# cut away, RaisedCut), at least LAVA_MIN over the lava (its ground is 0..700) -- and the climb lifted: from its start up at LIFT_GRADE (a
# turn of its loop HELIX_PITCH higher than the last) to its top over its start -- then spread out at GRADE each way and eased. The double
# spiral comes down at GRADE (its turns HELIX_PITCH apart and more). The jumps land as high as they leave: the ground under their landing
# and the road after it lifted to their lip's height (the road then going down after them at the grade), worked out twice.
CLEAR, LAVA, LAVA_MIN, GRADE = 400.0, 700.0, 1400.0, 0.17
loop_len = ahead(MARK['t1W0'], MARK['t2W0'])
LIFT_GRADE = HELIX_PITCH * 1.04 / (loop_len * 512)
gs = np.array([np.median([ground(X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o) for o in np.linspace(-HALF[k], HALF[k], 9)]) for k in range(N)])
floor0 = np.where(gs < LAVA, LAVA_MIN, np.maximum(gs + CLEAR, LAVA_MIN))
for k in range(N):
    if between(k, MARK['climb0'], MARK['top']):
        floor0[k] = max(floor0[k], TOPD + 150 + ahead(MARK['climb0'], k) * 512 * LIFT_GRADE)
g = GRADE * STEP * 512
def heights(floor):
    F = floor.copy()
    for _ in range(4):
        for k in range(N): j = (k + 1) % N; F[j] = max(F[j], F[k] - g)
        for k in range(N - 1, -1, -1): j = (k + 1) % N; F[k] = max(F[k], F[j] - g)
    Y = F.copy()
    for k in range(N): Y[k] = np.mean(F[[(k + d) % N for d in range(-6, 7)]])
    return np.maximum(Y, F - 60)

# the jumps the engine carries the car over: (name, foot, lip, landing lip, landing foot as (x, z, heading)), the ramp's angle
JUMPS = [('second', (547.5, 507.4, SE_), (550.4, 508.7, SE_), (562.5, 514.0, SE_), (564.5, 514.9, SE_), 18.0),
         ('shore', (567.4, 552.8, SW_), (565.6, 555.0, SW_), (559.6, 562.5, SW_), (558.6, 563.7, SW_), 16.0)]
JI = [(name, nearest(f[:2], f[2]), nearest(l[:2], l[2]), nearest(ld[:2], ld[2]), nearest(lf[:2], lf[2]), deg) for name, f, l, ld, lf, deg in JUMPS]
# (the lip's height as the ramp makes it: curving up from its foot at the jump's angle -- the road's own height at the lip is under it, and a
# landing lifted to that dropped 600 in a cell and a half after touching down)
def lip_height(iF, iL, deg):
    th = math.radians(deg); k1 = ahead(iF, iL) * 512
    return Y[iF] + k1 / math.sin(th) * (1 - math.cos(th))
Y = heights(floor0)
for _ in range(3):
    floor = floor0.copy()
    for name, iF, iL, iD, iDF, deg in JI:
        for k in range(iD, iDF + 1): floor[k % N] = max(floor[k % N], lip_height(iF, iL, deg) + 150)
    Y = heights(floor)

def flight_heights(iFoot, iLip, iLand, iLandFoot, theta):
    y0, y3 = Y[iFoot], Y[iLandFoot]
    k1 = ahead(iFoot, iLip) * 512; L_ = ahead(iLip, iLand) * 512; k2 = ahead(iLand, iLandFoot) * 512
    R1 = k1 / math.sin(theta); yl = y0 + R1 * (1 - math.cos(theta)); sl = math.tan(theta)
    land_y = max(yl, y3)                 # (as high as it leaves, at the least)
    def at(d):
        if d <= k1: return y0 + R1 - math.sqrt(max(0, R1 * R1 - d * d))
        if d <= k1 + L_:
            u = d - k1
            a = (land_y - yl - sl * L_) / (L_ * L_)
            return yl + sl * u + a * u * u
        u = d - k1 - L_; f = u / k2
        return land_y + (y3 - land_y) * ease(f)
    return at, yl, land_y

gap = np.zeros(N, bool); arcs = []
for name, iF, iL, iD, iDF, deg in JI:
    at, yl, land_y = flight_heights(iF, iL, iD, iDF, math.radians(deg))
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
          # out of the falls' pool, onto the climb's first time past it
        (P_(483.0, 481.0), MARK['t1W0'], MARK['t1W1'], 3200)]
GEYSERS = [  # the main channel: onto the slope over it, and the double spiral's last time along its south side over it
           (P_(536.0, 480.0), MARK['dOut'], MARK['cEnd'], 7000, 5),
           (P_(526.0, 480.0), MARK['dS1'], nearest_after((525.0, 466.0), N_, MARK['dS1'], 20), 7600, 4),
             # the second channel and the lowland
           (P_(556.0, 492.0), MARK['cEnd'], J('second', 1), 8200, 4),
           (P_(559.0, 530.0), MARK['eastN'], MARK['eastS'], 6800, 5),
             # the shore's lava
           (P_(556.0, 572.0), MARK['shoreE'], MARK['shoreW'], 7400, 4),
           (P_(526.0, 560.0), MARK['shoreW'], MARK['flank1'], 8000, 5)]
# crystal shards (crystalShards: as a geyser's, out of the volcano -- the crag's lake of lava -- `shards` at a time): onto the start
# straight, the climb's first time along its south side and its second down its east side, the west side, the plateau's south edge
VOLCANO = P_(474.5, 481.0, 23700.0)
SHARDS = [(nearest((472.0, SZ), E), nearest((500.0, SZ), E), 9000, 3),
          (nearest_after((508.0, 488.0), W_, MARK['climb0'], 60), nearest_after((491.0, 488.0), W_, MARK['climb0'], 70), 10400, 3),
          (MARK['t2E0'], nearest_after((511.0, 486.0), S_, MARK['t2E0'], 20), 11200, 3),
          (nearest((461.5, 494.0), N_), nearest((461.5, 468.0), N_), 9800, 3),
          (nearest((503.0, 497.0), W_), nearest((468.0, 497.0), W_), 10600, 3)]
shards = [{'source': VOLCANO, 'from': int(a), 'to': int(b), 'every': every, 'balls': n} for a, b, every, n in SHARDS]
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
for l in lava + geysers + shards:
    sx, sz = l['source'][:2]
    out.append(f'  {"shards" if l in shards else "lava geyser" if "balls" in l else "lava ball"} from ({sx + OX:.0f}, {sz + OZ:.0f}) onto points {l["from"]}..{l["to"]}, {min(np.hypot(X - sx, Z - sz)):.1f} cells from the road')
for m, k in MARK.items():
    out.append(f'    {m:9s} point {k:4d} ({X[k] + OX:6.1f}, {Z[k] + OZ:6.1f}) {Y[k]:6.0f} (ground {ground(X[k], Z[k]):5.0f})')
out.append(f'  pit lane points {iPit1}..{iPit2}, its fence {iFence0}..{iFence1}; start point {MARK["start"]}')
out.append(f'  the climb: its loop {loop_len:.1f} cells round, at {LIFT_GRADE * 100:.1f} %, to {Y[MARK["top"]]:.0f}; the double spiral from {Y[MARK["dIn"]]:.0f} '
           f'to {Y[MARK["dOut"]]:.0f}; the slope to {Y[MARK["cEnd"]]:.0f}')
# (each point over another stretch of the lap: the least height between them -- a spiral's turns over each other, the crossings)
under = []
for i in range(0, N, 2):
    dd = np.hypot(X - X[i], Z - Z[i]); sep = np.minimum(np.abs(idx - i), N - np.abs(idx - i))
    below = (sep > 40) & (dd < 2.0) & (Y < Y[i])
    if below.any(): under.append((Y[i] - Y[below].max(), i))
if under:
    least = sorted(under)[:3]
    out.append('  stacked: ' + ', '.join(f'{c:.0f} over the stretch under ({X[i] + OX:.1f}, {Z[i] + OZ:.1f})' for c, i in least))
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
    'crystalShards': shards,
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
