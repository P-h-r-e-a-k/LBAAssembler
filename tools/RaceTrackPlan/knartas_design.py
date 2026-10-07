# Knartas Island (KNARTAS.ILE): a lap through its three parts -- the dock, the refinery and the village -- built up into the air: most of
# it a raised road at three levels that passes over itself, with vertical loops and jumps the engine carries the car over, so the lap is
# far longer than the island's flat ground would hold (the user, 2026-10-07: "loads of height ... lots of loops and jumps").
#
# The island is three cubes: the dock (7,7) -- a U of concrete piers round an inlet, its mouth to the west, the island's rocket on the
# inlet's south side; the refinery (8,7) -- a fenced compound of tanks, machine houses and a cracking tower, the rocks rising to its
# north; the village (8,8) -- the Knartas' furry dome huts on rocky hills. The only way between the dock and the rest is a little
# bridge (x 504-515, z 482-486). Ground: the dock's piers 1,550, the compound's floor 1,600, the village 400-1,700 (huts on plateaus),
# the rocks north of the refinery up to 5,575. Decor tops up to 5,550 on the dock (lamp posts, the airships), 5,952 in the refinery.
#
# Run with E:\dump\TEMP\knartas holding H.npy (heights, H[z, x], cells 0..128 from 448) and obstacles.npz (the decor's tops and bottoms
# every quarter cell, `who` their body); --pictures draws the lap. Writes docs/racetrack/knartas_track_plan.json.
import json, math, sys
import numpy as np

D = 'E:/dump/TEMP/knartas/'
OUT = 'E:/dump/LBAAssembler/docs/racetrack/knartas_track_plan.json'
ORIGIN = 448
H = np.load(D + 'H.npy').astype(float)
H = np.nan_to_num(H, nan=0.0)
OB = np.load(D + 'obstacles.npz'); TOP, BOT, WHO, RQ = OB['top'], OB['bot'], OB['who'], int(OB['R'])
PRESENT = {(7, 7), (8, 7), (8, 8)}

def ground(x, z):
    xi, zi = min(126, max(0, int(x))), min(126, max(0, int(z))); fx, fz = x - xi, z - zi
    return H[zi, xi] * (1 - fx) * (1 - fz) + H[zi, xi + 1] * fx * (1 - fz) + H[zi + 1, xi] * (1 - fx) * fz + H[zi + 1, xi + 1] * fx * fz

STEP = 0.5
H0, HL = 4.25, 6.0                     # the deck's half width to its rail; at a loop (the ring drifts DRIFT across it)
DRIFT = 3.0
ASPHALT, CURB = H0 - 1.0, H0 - 0.25
LOW_DOCK, LOW_REF = 3400.0, 1900.0     # the dock's road, over its piers (1,550), their railings (2,557) and crates (3,250); (the refinery's floor 1,600)
HIGH, TOPL = 6000.0, 5600.0            # the high road (over the refinery's buildings) and the top road (over the rocks north of it)

def C(x, z): return (x - ORIGIN, z - ORIGIN)
# the corners in the lap's order, each (x, z) and the radius it is rounded with (absolute cells: converted below)
K = [((498.0, 461.0), 5.0),            # the dock's east arm, north (the start straight), round onto the north arm ...
     ((467.0, 461.0), 5.0),            # ... west along it through the first loop, round south over the inlet's mouth and the rocket (a jump) ...
     ((467.0, 499.0), 5.0),            # ... east along the south arm, climbing over the start straight, the channel and the compound's fence ...
     ((535.0, 499.0), 6.0),            # ... round north between the tanks, the second loop over them ...
     ((535.0, 467.0), 5.0),            # ... east past the machine houses, among the pipes ...
     ((552.5, 467.0), 5.0),            # ... south: the leap over the cracking tower, landing past the compound ...
     ((552.5, 537.0), 5.0),            # ... down through the first hut, round west through the village's middle (a cluster of huts) ...
     ((521.0, 537.0), 5.0),            # ... south through the hut on the west side ...
     ((521.0, 567.0), 5.0),            # ... east along the south shore through the third loop ...
     ((563.0, 567.0), 5.0),            # ... north through the hut on the east side, climbing over the refinery's east side ...
     ((563.0, 456.0), 5.0),            # ... west at the top over the rocks (the fourth loop, a gap at its top) ...
     ((509.0, 456.0), 5.0),            # ... south down the channel between the dock and the refinery, over the bridge ...
     ((509.0, 505.0), 5.0),            # ... west round onto the start straight
     ((498.0, 505.0), 5.0)]
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
        if np.dot(q2 - q1, P[(i + 1) % n] - P[i]) < -1e-9: sys.exit(f'corners {i} and {i + 1}: their roundings overlap')
        m = max(1, int(round(np.linalg.norm(q2 - q1) / 0.02)))
        for k in range(m): out.append(q1 + (q2 - q1) * k / m)
    return np.array(out)

fine = rounded_polyline(K)
k0 = int(np.argmin(np.hypot(fine[:, 0] - (498 - ORIGIN), fine[:, 1] - (500 - ORIGIN))))
fine = np.roll(fine, -k0, axis=0)
seg = np.linalg.norm(np.diff(np.vstack([fine, fine[:1]]), axis=0), axis=1)
cum = np.concatenate([[0], np.cumsum(seg)]); total = cum[-1]
N = int(round(total / STEP)); s = np.arange(N) * total / N
X = np.interp(s, cum, np.append(fine[:, 0], fine[0, 0])); Z = np.interp(s, cum, np.append(fine[:, 1], fine[0, 1]))
T = np.stack([np.gradient(X), np.gradient(Z)], 1); T /= np.linalg.norm(T, axis=1)[:, None]
Nn = np.stack([-T[:, 1], T[:, 0]], 1)
def nearest(p, heading=None):
    p = C(*p); d = np.hypot(X - p[0], Z - p[1])
    if heading is not None: d = d + 100 * (T @ np.array(heading) < 0.7)
    return int(np.argmin(d))
def ahead(a, b): return ((b - a) % N) * STEP
def between(k, a, b): return ahead(a, k) <= ahead(a, b)
def ease(f): f = min(1, max(0, f)); return f * f * (3 - 2 * f)
def trap(f, q=0.25):
    f = min(1, max(0, f))
    if f < q: v = f * f / (2 * q)
    elif f <= 1 - q: v = q / 2 + (f - q)
    else: v = 1 - q - (1 - f) ** 2 / (2 * q)
    return min(1, v / (1 - q))

# ---- heights: keyframes along the lap (a mark and its height), each stretch between two a grade that eases in and out (trap)
E, W_, S_, N_ = (1, 0), (-1, 0), (0, 1), (0, -1)
MARK = {
    'start':      nearest((498, 478), N_),
    'dock0':      nearest((498, 500), N_),
    'nArm1':      nearest((472, 461), W_),
    'sArm0':      nearest((467, 494), S_),
    'over':       nearest((493, 499), E),
    'climb1':     nearest((528, 499), E),
    'north0':     nearest((535, 493), N_), 'north1': nearest((535, 472), N_),
    'east1':      nearest((547, 467), E),
    'hut88':      nearest((552.5, 526), S_),
    'mid0':       nearest((547, 537), W_), 'mid':  nearest((534, 537), W_), 'mid1': nearest((526, 537), W_),
    'hut81':      nearest((521, 549), S_),
    'south0':     nearest((526, 567), E), 'south1': nearest((558, 567), E),
    'climbJ0':    nearest((563, 534), N_), 'climbJ1': nearest((563, 498), N_),
    'hut84':      nearest((563, 555), N_),
    'up1':        nearest((563, 475), N_),
    'top0':       nearest((558, 456), W_), 'top1': nearest((518, 456), W_),
    'bridge':     nearest((509, 486), S_),
    'down1':      nearest((509, 499), S_),
}
def hut_floor(x, z): return ground(x - ORIGIN, z - ORIGIN)
KEYS = [('dock0', LOW_DOCK), ('start', LOW_DOCK), ('nArm1', LOW_DOCK), ('sArm0', LOW_DOCK), ('over', 5200.0), ('climb1', HIGH), ('east1', HIGH),
        ('hut88', hut_floor(552.5, 526) + 30), ('mid0', 1500.0), ('mid', hut_floor(534, 537) + 30), ('mid1', 1700.0),
        ('hut81', hut_floor(521, 549) + 30), ('south0', 1300.0), ('south1', 1300.0), ('hut84', hut_floor(563, 555) + 30),
        ('climbJ0', 2700.0), ('climbJ1', 4000.0), ('up1', TOPL), ('top1', TOPL), ('bridge', 4200.0), ('down1', LOW_DOCK + 100)]
KEYS = sorted(((MARK[m], h) for m, h in KEYS), key=lambda t: t[0])
Y = np.zeros(N)
for i in range(len(KEYS)):
    (a, ha), (b, hb) = KEYS[i], KEYS[(i + 1) % len(KEYS)]
    span = ahead(a, b)
    k = a
    while True:
        Y[k] = ha + (hb - ha) * trap(ahead(a, k) / span) if span > 0 else ha
        if k == b: break
        k = (k + 1) % N

# ---- the jumps the engine carries the car over: (name, foot, lip, landing lip, landing foot as (x, z, heading)) and the ramp's angle
def flight_heights(iFoot, iLip, iLand, iLandFoot, theta, top_needed):
    """the car's way from the ramp's foot to the landing hill's foot: a ramp curving up (a circle's arc, level at its foot) to the lip, a
    parabola to the landing lip, a hill curving down to its foot -- the heights at its ends the road's own there"""
    y0, y3 = Y[iFoot], Y[iLandFoot]
    k1 = ahead(iFoot, iLip) * 512; L = ahead(iLip, iLand) * 512; k2 = ahead(iLand, iLandFoot) * 512
    R1 = k1 / math.sin(theta); yl = y0 + R1 * (1 - math.cos(theta)); sl = math.tan(theta)
    # the landing hill: a circle's arc down to its foot, its slope at the lip whatever the parabola brings
    def at(d):
        if d <= k1: return y0 + R1 - math.sqrt(max(0, R1 * R1 - d * d))
        if d <= k1 + L:
            u = d - k1; yland = land_y
            # parabola from (0, yl) with slope sl to (L, yland)
            a = (yland - yl - sl * L) / (L * L)
            return yl + sl * u + a * u * u
        u = d - k1 - L; f = u / k2
        return land_y + (y3 - land_y) * ease(f)
    land_y = max(y3 + 300, min(yl, top_needed))
    return at, yl, land_y

JUMPS = [('inlet', (467, 465.5, S_), (467, 470.5, S_), (467, 488.0, S_), (467, 493.0, S_), 55.0),
         ('tower', (552.5, 472.0, S_), (552.5, 476.0, S_), (552.5, 507.0, S_), (552.5, 512.0, S_), 22.0),
         ('fence', (563, 534.0, N_), (563, 529.0, N_), (563, 503.0, N_), (563, 498.0, N_), 30.0)]
gap = np.zeros(N, bool); arcs = []
for name, f, l, ld, lf, deg in JUMPS:
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

LOOPS = [('dock', (483.0, 461.0), 4.0, 0.0), ('tanks', (535.0, 483.0), 4.0, 0.0), ('rocksE', (549.0, 456.0), 4.0, 0.0), ('rocks', (527.0, 456.0), 4.0, 35.0), ('shore', (547.0, 567.0), 4.0, 0.0)]
LOOP_RUN, LOOP_EASE = 9.0, 6.0
loops = [(nm, nearest(p), R, gp) for nm, p, R, gp in LOOPS]
for nm, c, R, gp in loops:                     # (level through a loop's run)
    for k in range(N):
        if min(ahead(c, k), ahead(k, c)) <= LOOP_RUN: Y[k] = Y[c]

# ---- the refinery's pipes (RaceTrackPipes): gantries over the high road where it runs through the compound -- steam from their tops,
# oil dripping from every other one onto the road -- clear of the loops' runs and the jumps
PIPES = [((513.0, 499.0), E, (530.0, 499.0), E), ((538.0, 467.0), E, (547.0, 467.0), E), ((563.0, 495.0), N_, (563.0, 476.0), N_)]
pipes = []
for pa, ha, pb, hb in PIPES:
    i0, i1 = nearest(pa, ha), nearest(pb, hb)
    for nm, c, R, gp in loops:
        if between(c, i0, i1) or min(ahead(c, i0), ahead(i0, c), ahead(c, i1), ahead(i1, c)) <= LOOP_RUN + 1:
            sys.exit(f'pipes {pa}-{pb}: in loop {nm}')
    pipes.append((i0, i1))

HALF = np.full(N, H0)
for nm, c, R, gp in loops:
    for k in range(N):
        d = min(ahead(c, k), ahead(k, c))
        if d <= LOOP_RUN: HALF[k] = max(HALF[k], HL)
        elif d <= LOOP_RUN + LOOP_EASE: HALF[k] = max(HALF[k], H0 + (HL - H0) * ease((LOOP_RUN + LOOP_EASE - d) / LOOP_EASE))

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
out.append('  steepest at: ' + ', '.join(f'{abs(grade[k]) * 100:.1f}% point {k} ({X[k] + ORIGIN:.0f}, {Z[k] + ORIGIN:.0f})' for k in sp))
def edge_dist(x, z):
    x, z = x + ORIGIN, z + ORIGIN; best = 1e9
    for dz in (-1, 0, 1):
        for dx in (-1, 0, 1):
            q = (int(x // 64) + dx, int(z // 64) + dz)
            if q in PRESENT: continue
            qx = min(max(x, q[0] * 64), q[0] * 64 + 64); qz = min(max(z, q[1] * 64), q[1] * 64 + 64)
            best = min(best, math.hypot(x - qx, z - qz))
    return best
edge = np.array([edge_dist(x, z) for x, z in zip(X, Z)])
out.append(f'  nearest a missing cube: {edge.min():.1f} cells at ({X[edge.argmin()] + ORIGIN:.1f}, {Z[edge.argmin()] + ORIGIN:.1f}) (needs 6.5)')
clear = sorted((Y[k] - max(ground(X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o) for o in np.linspace(-HALF[k], HALF[k], 9)), k) for k in range(N) if not gap[k])
out.append('  the deck under the ground: ' + ', '.join(f'{c:.0f} at ({X[k] + ORIGIN:.1f}, {Z[k] + ORIGIN:.1f}) point {k}' for c, k in clear[:4] if c < 0))
hits = {}
for k in range(N):
    if gap[k]: continue
    for o in np.linspace(-HALF[k] - 0.3, HALF[k] + 0.3, 17):
        x, z = X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o
        i, j = int(z * RQ), int(x * RQ)
        if not (0 <= i < TOP.shape[0] and 0 <= j < TOP.shape[1]) or TOP[i, j] < 0: continue
        if TOP[i, j] > Y[k] - 350 and BOT[i, j] < Y[k] + 1400:
            b = int(WHO[i, j]); hits.setdefault(b, []).append((round(float(x + ORIGIN), 1), round(float(z + ORIGIN), 1), int(TOP[i, j]), int(Y[k])))
out.append('  decor in the road\'s space (body: places, e.g.): ' + '; '.join(f'{b}: {len(h)} {h[0]}' for b, h in sorted(hits.items())))
idx = np.arange(N); close = []
def seg_dist(p1, p2, q1, q2):
    def d_pt(p, a, b):
        ab = b - a; t = np.clip(((p - a) @ ab) / max(ab @ ab, 1e-12), 0, 1); return np.linalg.norm(p - a - ab * t)
    def cross(a, b, c, d):
        def o(p, q, r): return np.sign((q[0] - p[0]) * (r[1] - p[1]) - (q[1] - p[1]) * (r[0] - p[0]))
        return o(a, b, c) != o(a, b, d) and o(c, d, a) != o(c, d, b)
    if cross(p1, p2, q1, q2): return 0.0
    return min(d_pt(p1, q1, q2), d_pt(p2, q1, q2), d_pt(q1, p1, p2), d_pt(q2, p1, p2))
P2 = np.stack([X, Z], 1)
for i in range(0, N, 2):
    dd = np.hypot(X - X[i], Z - Z[i]); sep = np.minimum(np.abs(idx - i), N - np.abs(idx - i))
    cand = np.where((sep > 40) & (dd < HALF + HALF[i] + 1) & (np.abs(Y - Y[i]) < 1700))[0]
    for j in cand:
        d = seg_dist(P2[i] - Nn[i] * HALF[i], P2[i] + Nn[i] * HALF[i], P2[j] - Nn[j] * HALF[j], P2[j] + Nn[j] * HALF[j])
        if d < 0.6: close.append((abs(Y[j] - Y[i]), dd[j], i, int(j)))
seen = []
for dy, dmin, i, j in sorted(close):
    if any(abs(i - a) < 20 and abs(j - b) < 20 for a, b in seen): continue
    seen.append((i, j))
    out.append(f'  CLASH: points {i} ({X[i] + ORIGIN:.1f}, {Z[i] + ORIGIN:.1f}) {Y[i]:.0f} and {j} ({X[j] + ORIGIN:.1f}, {Z[j] + ORIGIN:.1f}) {Y[j]:.0f}: {dmin:.1f} cells apart, {dy:.0f} between')
    if len(seen) > 12: break
for nm, c, R, gp in loops:
    cx, cz = int((X[c] + ORIGIN) // 64), int((Z[c] + ORIGIN) // 64)
    ends = [(X[c] + T[c, 0] * a * (R + 0.75) + ORIGIN, Z[c] + T[c, 1] * a * (R + 0.75) + ORIGIN) for a in (-1, 1)]
    same = all(int(x // 64) == cx and int(z // 64) == cz for x, z in ends)
    # what stands over the ring: other parts of the lap within its reach and under its top
    ring_top = Y[c] + 2 * R * 512 + 600
    over = [k for k in range(N) if min(abs(k - c), N - abs(k - c)) > 40 and math.hypot(X[k] - X[c], Z[k] - Z[c]) < R + HALF[k] + 1 and Y[k] < ring_top + 600 and Y[k] > Y[c] - 600]
    out.append(f'  loop {nm} at ({X[c] + ORIGIN:.1f}, {Z[c] + ORIGIN:.1f}) {Y[c]:.0f}: {"whole" if same else "ACROSS A CUBE EDGE"} cube, gap {gp}, its top {ring_top:.0f}' + (f', CLASH with points {over[0]}..{over[-1]}' if over else ''))
for name, iF, iL, iD, iDF in arcs:
    out.append(f'  jump {name}: points {iF} {iL} {iD} {iDF}; lips ({X[iL] + ORIGIN:.1f}, {Z[iL] + ORIGIN:.1f}) {Y[iL]:.0f} -> ({X[iD] + ORIGIN:.1f}, {Z[iD] + ORIGIN:.1f}) {Y[iD]:.0f}, top {Y[iF:iDF].max() if iF < iDF else 0:.0f}')
for m, k in MARK.items():
    out.append(f'    {m:8s} point {k:4d} ({X[k] + ORIGIN:6.1f}, {Z[k] + ORIGIN:6.1f}) {Y[k]:6.0f} (ground {ground(X[k], Z[k]):5.0f})')
print('\n'.join(out))

plan = {
    'originCellX': ORIGIN, 'originCellZ': ORIGIN,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in zip(X, Z)],
    'heights': [round(float(y), 1) for y in Y],
    'maxGrade': round(float(np.abs(grade).max()) + 0.05, 3),
    'raised': [0, N - 1], 'raisedHalf': H0, 'asphaltHalf': ASPHALT, 'curbHalf': CURB,
    'raisedHalfs': [round(float(h), 3) for h in HALF],
    'loops': [[int(c), R, DRIFT, gp, H0] for nm, c, R, gp in loops],
    'arcJumps': [[int(iF), int(iL), int(iD), int(iDF), 0] for name, iF, iL, iD, iDF in arcs],
    'start': int(MARK['start']),
    # (where the opponents wait while the player qualifies: on the dock's east arm, under the start straight's west edge -- not on the
    # road: with no pit lane their grid spots ran back round the U-turn and they sat in the way)
    'pitSpots': [[491.75 - ORIGIN, z - ORIGIN, 0.0, -1.0, 1550.0] for z in (476.0, 481.0, 485.5)],
    'raisedCut': True,
    # (the huts the road drives through: made bigger where there is room round them, cut open at the road and two-sided; the village's
    # middle is a cluster of four domes, big enough as it is)
    'driveThrough': [{'body': 88, 'scale': 1.4}, {'body': 81, 'scale': 1.6}, {'body': 84, 'scale': 1.3}, {'body': 77, 'scale': 1.0}],
    # (a hut beside the village's middle road, kept)
    'keepBodies': [73],
    'pipes': [{'from': int(i0), 'to': int(i1), 'every': 6.0, 'drip': True} for i0, i1 in pipes],
}
if '--write' in sys.argv:
    json.dump(plan, open(OUT, 'w'))
    print('written', OUT)
np.savez(D + 'lap.npz', X=X + ORIGIN, Z=Z + ORIGIN, Y=Y, HALF=HALF, gap=gap)

if '--pictures' in sys.argv:
    from PIL import Image, ImageDraw
    Zm = 8
    img = Image.open(D + 'boxmap.png').convert('RGB')
    d = ImageDraw.Draw(img)
    P = lambda x, z: ((x + ORIGIN - 448) * Zm, (z + ORIGIN - 448) * Zm)
    lo, hi = Y.min(), Y.max()
    for k in range(N):
        j = (k + 1) % N
        t = (Y[k] - lo) / (hi - lo)
        col = (int(255 * t), int(255 * (1 - abs(2 * t - 1))), int(255 * (1 - t)))
        if gap[k]: col = (255, 255, 255)
        d.line([P(X[k], Z[k]), P(X[j], Z[j])], fill=col, width=4 if not gap[k] else 1)
    for nm, c, R, gp in loops: d.ellipse([P(X[c] - 1.5, Z[c] - 1.5), P(X[c] + 1.5, Z[c] + 1.5)], outline=(255, 0, 255), width=3)
    for m, k in MARK.items(): d.text(P(X[k], Z[k]), m, fill=(255, 255, 255))
    for i0, i1 in pipes:
        k = i0
        while k != i1:
            d.ellipse([P(X[k] - 0.3, Z[k] - 0.3), P(X[k] + 0.3, Z[k] + 0.3)], outline=(255, 160, 0)); k = (k + 6) % N
    img.save(D + 'design.png')
    W, Hh = 1400, 360
    pr = Image.new('RGB', (W, Hh), (20, 20, 30)); d2 = ImageDraw.Draw(pr)
    TV = max(12000, hi + 500)
    for k in range(N - 1):
        d2.line([(k / N * W, Hh - 10 - Y[k] / TV * (Hh - 20)), ((k + 1) / N * W, Hh - 10 - Y[k + 1] / TV * (Hh - 20))], fill=(255, 220, 0) if not gap[k] else (255, 255, 255), width=2)
        d2.point((k / N * W, Hh - 10 - ground(X[k], Z[k]) / TV * (Hh - 20)), fill=(120, 200, 120))
    for i, (m, k) in enumerate(MARK.items()): d2.text((k / N * W, 5 + (i % 4) * 10), m, fill=(200, 200, 255))
    pr.save(D + 'profile.png')
