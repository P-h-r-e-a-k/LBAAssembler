# Volcano Island (SOUSCELB.ILE, the game's island 11 -- under Celebration Island; cells 448..576 each way, cube (7,8) not there) from
# the user's sketch (2026-10-08: "Pink are jump points, green is track, orange is pitlane. For this track let's add lava balls shooting up
# and out of the lava at points and raining down onto the track"). The sketch (repo root volcanoTrack.png) is the editor's minimap turned
# half a turn and stretched: sketch x = 1111 - 2.05 * render x, sketch y = 725 - 1.39 * render y (the render 4 pixels a cell from cell
# (448, 448)): E:\dump\TEMP\volc\centreline.py. One raised road round the island:
#
#  - round the sand plateau at the volcano's top (12,750): its north straight with the start line and the pit lane on its right, its
#    west side, its south edge;
#  - down off its east end and a jump south over the main lava channel; a short stretch on the rocks between two channels and a jump east
#    over the second one onto the island's east edge; south along it and a jump south-west down to the south shore;
#  - west along the shore and back up the volcano's flank onto the plateau.
#
# Lava balls (RACEMOD.CPP lavaball=): from points in the lava beside the lap, balls of fire shot up out of it, falling onto the road, a
# shadow on the road where each will land -- the game's own lava balls since 2026-10-10 (the user: "the lava balls that are currently
# coming out of the lava are different to the retail ones, so let's switch these").
#
# 2026-10-10 (the user): "the pit lane is on the wrong side of the start/finish straight" -- its stripe and fence were on the race lanes'
# north, the grid moved south onto the pit lane (the signs Otringal's, whose straight runs north); "make the entire track slightly wider"
# (H0 4.0 to 4.75); a lava fall beside the plateau's south edge, a crag of rock with lava pouring down its face into a pool by the road and
# balls shot out of it onto the road ("In scene 100 ... there's a lava water fall next to a section of track that shoots lava balls, let's
# steal this idea"; RaceTrackLavaFall, RACEMOD.CPP lavafall=); a boost panel in the pit lane ("cars ... drive over it with at least two
# wheels ... immediately shoot a car up to 120kph, let's test this by putting this on the pit lane"; RACEMOD.CPP boostpanel=).
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
TOPD, SHORE = 13000.0, 4500.0           # the plateau's road (over its sand, 12,750) and the south shore's
NZ = 461.5                              # the north straight's race lanes' middle
SZ = NZ - PIT / 2 * -1                  # the deck's middle there (the pit lane south of the race lanes: the deck's middle south of theirs)

def C(x, z): return (x - OX, z - OZ)
# the corners in the lap's order, each (x, z) and the radius it is rounded with (island cells)
K = [((543.5, SZ), 5.0),                # the north straight's east end, off the plateau: south, the jump over the main lava channel ...
     ((542.0, 505.0), 5.0),             # ... east-south-east: the jump over the second channel onto the east edge ...
     ((568.0, 516.5), 4.0),             # ... south along the east edge (clear of the island's edge, x 576) ...
     ((568.0, 551.0), 4.0),             # ... south-west: the jump down to the south shore ...
     ((556.0, 566.0), 4.0),             # ... west along the shore ...
     ((538.0, 566.0), 5.0),             # ... north up the volcano's flank ...
     ((537.0, 556.0), 4.0),
     ((553.0, 547.0), 4.0),
     ((555.0, 538.0), 4.0),             # ... north-west, climbing onto the plateau ...
     ((512.0, 497.0), 8.0),             # ... west along its south edge ...
     ((462.0, 497.0), 6.0),             # ... north up its west side ...
     ((461.0, SZ), 6.0)]                # ... east along its north straight (the start, the pit lane on the right)
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
def trap(f, q=0.15):
    f = min(1, max(0, f))
    if f < q: v = f * f / (2 * q)
    elif f <= 1 - q: v = q / 2 + (f - q)
    else: v = 1 - q - (1 - f) ** 2 / (2 * q)
    return min(1, v / (1 - q))

E, W_, S_, N_ = (1, 0), (-1, 0), (0, 1), (0, -1)
SE_ = (0.915, 0.405); SW_ = (-0.625, 0.781)
# ---- widths: the usual, and the north straight with its pit lane on its right (south: +Nn heading east)
def ramp(a, b, k): return ease(ahead(a, k) / ahead(a, b))
HALF = np.full(N, H0)
PIT_HALF = H0 + PIT / 2
iPit0 = nearest((470.0, SZ), E); iPit1 = nearest((476.0, SZ), E); iPit2 = nearest((500.0, SZ), E); iPit3 = nearest((506.0, SZ), E)
for k in range(N):
    if between(k, iPit0, iPit1): HALF[k] = max(HALF[k], H0 + (PIT_HALF - H0) * ramp(iPit0, iPit1, k))
    elif between(k, iPit1, iPit2): HALF[k] = PIT_HALF
    elif between(k, iPit2, iPit3): HALF[k] = max(HALF[k], PIT_HALF + (H0 - PIT_HALF) * ramp(iPit2, iPit3, k))
PIT_WAIT = (476.0, 480.0, 484.0)        # the opponents' waiting spots in the pit lane (x: behind the line, cube (7,7))

# ---- heights, close over the ground (as Otringal's, version 2: the user wanted that lap "closer to ground level", 2026-10-08): CLEAR over
# the median of the ground across the deck (a rock at one rail is cut away, RaisedCut), at least LAVA_MIN over the lava (its ground is
# 0..700), then spread out at GRADE each way (the deck rises before a rise in the ground) and eased
MARK = {
    'start':    nearest(START, E),
    'plateauE': nearest((505, SZ), E),          # the plateau's east end: down from here
    'cEnd':     nearest((542, 500), S_),        # the stretch between the channels
    'eastN':    nearest((568, 524), S_),        # the east edge
    'eastS':    nearest((568, 546), S_),
    'shoreE':   nearest((548, 566), W_),        # the south shore
    'shoreW':   nearest((538, 563), N_),
    'flank1':   nearest((554, 543), N_),        # up the volcano's flank
    'flank2':   nearest((530, 520), (-0.72, -0.69)),
    'plateauS': nearest((505, 497), W_),        # onto the plateau
    'plateauW': nearest((462, 480), N_),
}
CLEAR, LAVA, LAVA_MIN, GRADE = 400.0, 700.0, 1400.0, 0.17
gs = np.array([np.median([ground(X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o) for o in np.linspace(-HALF[k], HALF[k], 9)]) for k in range(N)])
floor = np.where(gs < LAVA, LAVA_MIN, np.maximum(gs + CLEAR, LAVA_MIN))
g = GRADE * STEP * 512
F = floor.copy()
for _ in range(3):
    for k in range(N): j = (k + 1) % N; F[j] = max(F[j], F[k] - g)
    for k in range(N - 1, -1, -1): j = (k + 1) % N; F[k] = max(F[k], F[j] - g)
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

# (the sketch's pink dots: (543.3, 474.7) -> (542.3, 488.7); (550.4, 510.9) -> (571.4, 519.0), the east edge moved in to x 568;
# (571.0, 555.8) -> (562.2, 568.2))
JUMPS = [('channel', (543.4, 469.0, S_), (543.3, 474.7, S_), (542.6, 488.7, S_), (542.4, 492.0, S_), 20.0, None),
         ('second', (547.5, 507.4, SE_), (550.4, 508.7, SE_), (562.5, 514.0, SE_), (564.5, 514.9, SE_), 18.0, None),
         ('shore', (567.4, 552.8, SW_), (565.6, 555.0, SW_), (559.6, 562.5, SW_), (558.6, 563.7, SW_), 16.0, None)]
gap = np.zeros(N, bool); arcs = []
for name, f, l, ld, lf, deg, landing in JUMPS:
    iF, iL, iD, iDF = nearest(f[:2], f[2]), nearest(l[:2], l[2]), nearest(ld[:2], ld[2]), nearest(lf[:2], lf[2])
    foot_y, land_at = landing if landing is not None else (None, None)
    if foot_y is not None: Y[iDF] = foot_y
    at, yl, land_y = flight_heights(iF, iL, iD, iDF, math.radians(deg), Y[iDF] + 600, land_at)
    k = iF
    while True:
        Y[k] = at(ahead(iF, k) * 512)
        if k == iDF: break
        k = (k + 1) % N
    k = (iL + 1) % N
    while k != iD: gap[k] = True; k = (k + 1) % N
    arcs.append((name, iF, iL, iD, iDF))

# ---- lava balls: from points in the lava beside the lap, onto the road -- each source (x, z) and the road it rains on, from a mark to a
# mark (the race-track mode picks a place along it each time), every so many ms
LAVA = [((536.0, 480.0), ('channel', 'cEnd'), 5200), ((526.0, 480.0), ('plateauE', 'channel'), 6100),
        ((556.0, 492.0), ('cEnd', 'second'), 5600), ((559.0, 530.0), ('eastN', 'eastS'), 4800),
        ((556.0, 572.0), ('shoreE', 'shoreW'), 5000), ((526.0, 560.0), ('shoreW', 'flank1'), 5900)]
def mark_or_jump(m):
    if m in MARK: return MARK[m]
    return [a for a in arcs if a[0] == m][0][4]
lava = []
for (sx, sz), (m0, m1), every in LAVA:
    a, b = mark_or_jump(m0), mark_or_jump(m1)
    lava.append({'source': [sx - OX, sz - OZ], 'from': int(a), 'to': int(b), 'every': every})

# ---- the lava fall (RaceTrackLavaFall): a crag of rock on the plateau inside the lap, by its south edge, lava pouring down its face (south,
# towards the road) into a pool beside the road's inner rail; balls shot out of the pool onto the road along the straight there (the
# race-track mode's lavaball=, the pool their source), and the game's spray of lava at the fall's foot and off its lip (lavafall=). The
# face's foot (x, z), the way it looks, the fall's width, the crag's height over the plateau, its depth back from the face and its width
# either side of the fall, the pool's reach out from the face; the ground copied for its rock and its lava (the island's own, cells).
FALL_AT, FALL_W = (486.0, 489.0), 3.0
lava_fall = {'at': [FALL_AT[0] - OX, FALL_AT[1] - OZ], 'facing': [0.0, 1.0], 'width': FALL_W, 'height': 4600.0, 'depth': 8.0, 'wing': 6.0,
             'pool': 2.0, 'rock': [530.0 - OX, 466.0 - OZ], 'lava': [515.0 - OX, 479.0 - OZ]}
lava.append({'source': [FALL_AT[0] - OX, FALL_AT[1] + 1.5 - OZ], 'from': int(nearest((501.0, 497.0), W_)), 'to': int(nearest((471.0, 497.0), W_)), 'every': 2600})

# ---- the boost panel (RACEMOD.CPP boostpanel=: a car with two wheels on it is at 120 km/h at once): in the pit lane, past the start line
# and the opponents' waiting spots -- a car has to leave the race lanes before the pit lane's fence begins to take it. Its first and last
# point, how far across the road (Across, cells: the pit lane's middle +/- 1.5)
BOOST = [(492.0, 497.0)]
PIT_MID = PIT_HALF - PIT / 2
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
idx = np.arange(N); close = []
for i in range(0, N, 2):
    if gap[i]: continue
    dd = np.hypot(X - X[i], Z - Z[i]); sep = np.minimum(np.abs(idx - i), N - np.abs(idx - i))
    nearby = (sep > 40) & (dd < HALF + HALF[i] + 0.6) & (np.abs(Y - Y[i]) < 2600) & ~gap
    if nearby.any(): close.append((dd[nearby].min(), i, int(np.argmax(nearby))))
for dmin, i, j in sorted(close)[:4]:
    out.append(f'  CLASH: ({X[i] + OX:.1f}, {Z[i] + OZ:.1f}) {Y[i]:.0f} and ({X[j] + OX:.1f}, {Z[j] + OZ:.1f}) {Y[j]:.0f}: {dmin:.1f} cells apart')
for l in lava:
    sx, sz = l['source']
    out.append(f'  lava ball from ({sx + OX:.0f}, {sz + OZ:.0f}) (ground {ground(sx, sz):.0f}) onto points {l["from"]}..{l["to"]}, {min(np.hypot(X - sx, Z - sz)):.1f} cells from the road')
for m, k in MARK.items():
    out.append(f'    {m:9s} point {k:4d} ({X[k] + OX:6.1f}, {Z[k] + OZ:6.1f}) {Y[k]:6.0f} (ground {ground(X[k], Z[k]):5.0f})')
out.append(f'  pit lane points {iPit1}..{iPit2}; start point {MARK["start"]}')
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
    # the stripe between the pit lane and the race lanes (south of them, +Across), first and last point; the opponents' waiting spots in
    # the pit lane's middle, facing along the straight (east), on the deck; Citadel Island's white fence along the stripe; the grid on the
    # race lanes (north of the deck's middle, -Across)
    'pitStripe': [int(iPit1), int(iPit2), round(float(PIT_HALF - PIT), 3)],
    'pitSpots': [[x - OX, round(SZ + PIT_MID - OZ, 3), 1.0, 0.0, TOPD] for x in PIT_WAIT],
    'pitFence': True,
    'gridShift': -PIT / 2,
    'raisedCut': True,
    'lavaBalls': lava,
    'lavaFalls': [lava_fall],
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
    for l in lava:
        sx, sz = l['source']
        d.ellipse([P(sx - 1, sz - 1), P(sx + 1, sz + 1)], outline=(255, 80, 0), width=3)
        for k in (l['from'], l['to']): d.line([P(sx, sz), P(X[k], Z[k])], fill=(255, 120, 0), width=1)
    for m, k in MARK.items(): d.text(P(X[k], Z[k]), m, fill=(255, 255, 255))
    # (the pit lane's stripe, the boost panel, the lava fall's crag and pool)
    st = plan['pitStripe']
    for k in range(st[0], st[1]): d.point(P(X[k] + Nn[k, 0] * st[2], Z[k] + Nn[k, 1] * st[2]), fill=(255, 255, 255))
    for a, b, lo, hi in boost:
        for k in range(a, b + 1):
            for o in np.linspace(lo, hi, 7): d.point(P(X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o), fill=(0, 255, 255))
    fx, fz = lava_fall['at'][0], lava_fall['at'][1]; half = FALL_W / 2 + lava_fall['wing']
    d.rectangle([P(fx - half, fz - lava_fall['depth']), P(fx + half, fz)], outline=(160, 160, 160), width=2)
    d.rectangle([P(fx - FALL_W / 2 - 1.5, fz), P(fx + FALL_W / 2 + 1.5, fz + lava_fall['pool'])], fill=(255, 90, 0))
    img.save(D + 'design.png')
