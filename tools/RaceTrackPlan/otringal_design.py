# Otringal (OTRINGAL.ILE, the game's island 4, cells x 384..576, z 448..640) from the user's sketch (2026-10-08: "Green is the track,
# pink are jump points and the red is intended to be a tunnel to drive through and the orange line represents a pit lane"). The sketch is
# the editor's minimap turned a quarter clockwise and stretched along the island (sketch x = 1141 - 1.47 * render y, sketch y = render x
# - 23, the render 4 pixels a cell from cell (384, 448)): E:\dump\TEMP\otr\sketchfit.py, centreline.py. One raised road, round the lap:
#
#  - the square island (the walled yard south-east of the town, scene 92): east along its south side, north along its east side, west
#    along its north side onto the bridge to the town -- through a tunnel over the bridge;
#  - up through the town, north-east, climbing over its roofs onto the straight along the palace's east side (the start line, the pit
#    lane on its left, between the race lanes and the palace), high over the town's north at 14,600;
#  - west along the island's north edge, south down its west side and a jump over the inlet, an S round the town's tanks, and down past
#    the west pier over the sea to the islets (a scene of their own: cube (6,9) has ground but no scene in the game -- RaceTrackIsland
#    .SceneFor);
#  - east along the south coast and a jump over the sea back onto the square island.
#
# Run with E:\dump\TEMP\otr holding H.npy and obstacles.npz (prep.py: islandheights, decorpoints, decorlist, islandrender of the island);
# --pictures draws the lap over boxmap.png. --write writes docs/racetrack/otringal_track_plan.json.
import json, math, sys
import numpy as np

D = 'E:/dump/TEMP/otr/'
OUT = 'E:/dump/LBAAssembler/docs/racetrack/otringal_track_plan.json'
OX, OZ = 384, 448                       # the plan's origin: the island's west and north cells
H = np.nan_to_num(np.load(D + 'H.npy').astype(float), nan=0.0)
OB = np.load(D + 'obstacles.npz'); TOP, BOT, WHO, RQ = OB['top'], OB['bot'], OB['who'], int(OB['R'])
PRESENT = {(6, 8), (6, 9), (7, 7), (7, 8), (7, 9), (8, 7), (8, 9)}
SIZE = 192

def ground(x, z):
    """the ground's height at plan cells (x, z)"""
    xi, zi = min(SIZE - 1, max(0, int(x))), min(SIZE - 1, max(0, int(z))); fx, fz = x - xi, z - zi
    return H[zi, xi] * (1 - fx) * (1 - fz) + H[zi, xi + 1] * fx * (1 - fz) + H[zi + 1, xi] * (1 - fx) * fz + H[zi + 1, xi + 1] * fx * fz

STEP = 0.5
H0 = 4.0                                # the deck's half width to its rail
ASPHALT, CURB = H0 - 1.0, H0 - 0.25
PIT = 4.5                               # the pit lane beside the straight, on its left (west), past a fence
RX = 497.0                              # the straight's race lanes' middle (the palace's box ends at x 488.5: the pit lane between)
SX = RX - PIT / 2                       # the deck's middle there
SQ, TOWN, HIGH, TANKS, COAST = 5000.0, 9200.0, 14000.0, 13200.0, 7400.0

def C(x, z): return (x - OX, z - OZ)
# the corners in the lap's order, each (x, z) and the radius it is rounded with (island cells)
K = [((555.0, 620.0), 5.0),             # the square island: east along its south side, round north ...
     ((555.0, 595.0), 5.0),             # ... west along its north side, over the bridge through the tunnel ...
     ((477.0, 594.0), 8.0),             # ... into the town, north-west ...
     ((470.0, 573.0), 6.0),             # ... north-east up through it, climbing over its roofs ...
     ((SX, 524.0), 10.0),               # ... north: the straight along the palace's east side (the start, the pit lane on the left) ...
     ((SX, 453.0), 7.0),                # ... west along the north edge ...
     ((453.0, 455.0), 6.0),             # ... south down the west side, the jump over the inlet ...
     ((452.5, 524.5), 3.5),             # ... the S round the tanks, over them: east ...
     ((470.0, 524.5), 4.0),             # ... south ...
     ((470.0, 541.0), 4.0),             # ... west ...
     ((443.0, 541.0), 4.0),             # ... north-west, past the west pier ...
     ((437.0, 532.0), 4.0),
     ((429.0, 532.0), 4.0),             # ... south ...
     ((426.0, 557.0), 4.0),             # ... west over the pier's yard ...
     ((408.0, 560.0), 4.0),             # ... south over the sea to the islets ...
     ((410.0, 590.0), 6.0),             # ... south-east over them ...
     ((431.0, 613.0), 6.0),             # ... east along the south coast ...
     ((457.0, 613.0), 5.0),
     ((468.0, 630.0), 5.0),
     ((491.0, 631.0), 5.0),             # ... north-east ...
     ((499.0, 620.0), 4.0)]             # ... east: the jump over the sea onto the square island
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
START = (SX, 488.0)                     # the start line on the straight (heading north: its grid behind it, south, on the level)
k0 = int(np.argmin(np.hypot(fine[:, 0] - C(*START)[0], fine[:, 1] - C(*START)[1] - 20)))
fine = np.roll(fine, -k0, axis=0)
seg = np.linalg.norm(np.diff(np.vstack([fine, fine[:1]]), axis=0), axis=1)
cum = np.concatenate([[0], np.cumsum(seg)]); total = cum[-1]
N = int(round(total / STEP)); s = np.arange(N) * total / N
X = np.interp(s, cum, np.append(fine[:, 0], fine[0, 0])); Z = np.interp(s, cum, np.append(fine[:, 1], fine[0, 1]))
T = np.stack([np.gradient(X), np.gradient(Z)], 1); T /= np.linalg.norm(T, axis=1)[:, None]
Nn = np.stack([-T[:, 1], T[:, 0]], 1)    # across the road (the builder's Across: east when heading north)
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

# ---- heights: keyframes along the lap, each stretch between two a grade that eases in and out (trap)
E, W_, S_, N_ = (1, 0), (-1, 0), (0, 1), (0, -1)
MARK = {
    'sq0':      nearest((532, 620), E),         # the square island, landed
    'sqN':      nearest((555, 605), N_),
    'tunnel0':  nearest((530, 595), W_),        # the tunnel over the bridge
    'tunnel1':  nearest((498, 595), W_),
    'town0':    nearest((474, 585), N_),
    'town1':    nearest((473, 565), N_),        # (up through the town, over its roofs)
    'straight0': nearest((SX, 522), N_),
    'start':    nearest(START, N_),
    'north1':   nearest((SX, 466), N_),
    'nw':       nearest((470, 453), W_),
    'west0':    nearest((453, 470), S_),
    'west1':    nearest((453, 500), S_),
    'tanks0':   nearest((462, 524.5), E),       # the S over the tanks
    'tanks1':   nearest((455, 541), W_),
    'pier0':    nearest((428, 545), S_),
    'pier1':    nearest((416, 559), W_),
    'sea0':     nearest((408, 575), S_),
    'isles':    nearest((420, 602), (0.7, 0.7)),
    'coast0':   nearest((445, 613), E),
    'coast1':   nearest((480, 631), E),
}
# (the north and the west side high: east of the west side's road the ground is a cliff of 12,700-13,700 at z 496-512, and the road
# can't go further west -- cube (6,7) isn't there)
KEYS = [('sq0', SQ), ('sqN', SQ), ('tunnel0', SQ), ('tunnel1', SQ), ('town1', TOWN), ('straight0', HIGH - 400),
        ('start', HIGH), ('north1', HIGH), ('nw', HIGH), ('west0', HIGH), ('west1', HIGH), ('tanks0', TANKS), ('tanks1', TANKS),
        ('pier0', 10600.0), ('pier1', 8600.0), ('sea0', COAST), ('isles', COAST), ('coast0', COAST), ('coast1', COAST)]
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

# (the sketch's pink dots: the inlet's at (451.1, 514.5) -> (451.8, 522.9), the sea's at (504.7, 620.4) -> (525.1, 619.5))
JUMPS = [('inlet', (453, 506.0, S_), (453, 512.5, S_), (453, 521.5, S_), (453, 524.0, S_), 26.0, None),
         ('sea', (497, 620.0, E), (505.0, 620.0, E), (525.0, 620.0, E), (531.0, 620.0, E), 24.0, None)]
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

# ---- widths: the usual, and the straight with its pit lane on its left (west: -Nn heading north)
def ramp(a, b, k): return ease(ahead(a, k) / ahead(a, b))
HALF = np.full(N, H0)
PIT_HALF = H0 + PIT / 2
iPit0 = nearest((SX, 516.0), N_); iPit1 = nearest((SX, 508.0), N_); iPit2 = nearest((SX, 470.0), N_); iPit3 = nearest((SX, 463.0), N_)
for k in range(N):
    if between(k, iPit0, iPit1): HALF[k] = max(HALF[k], H0 + (PIT_HALF - H0) * ramp(iPit0, iPit1, k))
    elif between(k, iPit1, iPit2): HALF[k] = PIT_HALF
    elif between(k, iPit2, iPit3): HALF[k] = max(HALF[k], PIT_HALF + (H0 - PIT_HALF) * ramp(iPit2, iPit3, k))
PIT_WAIT = (496.0, 500.0, 504.0)        # where the opponents wait in the pit lane while the player qualifies (z: behind the line, cube (7,7))

# ---- the tunnel over the bridge (RaceTrackTunnel): its first and last point, and the roof's height over the deck
TUNNEL = (nearest((529, 595), W_), nearest((499, 595), W_))
TUNNEL_ROOF = 2800.0

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
    worst = (1e9, None)
    k = iL
    while k != iD:
        for o in np.linspace(-1.5, 1.5, 5):
            x, z = X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o
            i, j = int(z * RQ), int(x * RQ)
            if 0 <= i < TOP.shape[0] and 0 <= j < TOP.shape[1] and TOP[i, j] >= 0 and Y[k] - TOP[i, j] < worst[0]: worst = (Y[k] - TOP[i, j], int(WHO[i, j]))
        if Y[k] - ground(X[k], Z[k]) < worst[0]: worst = (Y[k] - ground(X[k], Z[k]), -1)
        k = (k + 1) % N
    flight = [Y[q % N] for q in range(iL, iD + (N if iD < iL else 0))]
    out.append(f'  jump {name}: lip {Y[iL]:.0f}, landing {Y[iD]:.0f}, top {max(flight):.0f}, {ahead(iL, iD):.1f} cells; least clearance {worst[0]:.0f} over {"the ground" if worst[1] == -1 else "body " + str(worst[1])}')
idx = np.arange(N); close = []
for i in range(0, N, 2):
    if gap[i]: continue
    dd = np.hypot(X - X[i], Z - Z[i]); sep = np.minimum(np.abs(idx - i), N - np.abs(idx - i))
    nearby = (sep > 40) & (dd < HALF + HALF[i] + 0.6) & (np.abs(Y - Y[i]) < 2600) & ~gap
    if nearby.any(): close.append((dd[nearby].min(), i, int(np.argmax(nearby))))
for dmin, i, j in sorted(close)[:4]:
    out.append(f'  CLASH: ({X[i] + OX:.1f}, {Z[i] + OZ:.1f}) {Y[i]:.0f} and ({X[j] + OX:.1f}, {Z[j] + OZ:.1f}) {Y[j]:.0f}: {dmin:.1f} cells apart')
for m, k in MARK.items():
    out.append(f'    {m:9s} point {k:4d} ({X[k] + OX:6.1f}, {Z[k] + OZ:6.1f}) {Y[k]:6.0f} (ground {ground(X[k], Z[k]):5.0f}) cube ({int((X[k] + OX) // 64)},{int((Z[k] + OZ) // 64)})')
out.append(f'  tunnel: points {TUNNEL[0]}..{TUNNEL[1]}, {ahead(*TUNNEL):.0f} cells; pit lane points {iPit1}..{iPit2}; start point {MARK["start"]}')
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
    # the stripe between the pit lane and the race lanes (west of them, -Across), first and last point; the opponents' waiting spots in
    # the pit lane's middle, facing up the straight, on the deck; Citadel Island's white fence along the stripe; the grid on the race lanes
    'pitStripe': [int(iPit1), int(iPit2), round(float(-(PIT_HALF - PIT)), 3)],
    'pitSpots': [[round(SX - (PIT_HALF - PIT / 2) - OX, 3), z - OZ, 0.0, -1.0, HIGH] for z in PIT_WAIT],
    'pitFence': True,
    'gridShift': PIT / 2,
    'raisedCut': True,
    'tunnels': [{'from': int(TUNNEL[0]), 'to': int(TUNNEL[1]), 'roof': TUNNEL_ROOF}],
    # (the raised road in pieces of 10 cells, not 4: the town's and the palace's cubes are near their 200 decors already)
    'raisedPiece': 10.0,
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
    k = TUNNEL[0]
    while k != TUNNEL[1]:
        d.point(P(X[k], Z[k]), fill=(255, 0, 0)); k = (k + 1) % N
    for m, k in MARK.items(): d.text(P(X[k], Z[k]), m, fill=(255, 255, 255))
    img.save(D + 'design.png')
    W, Hh = 1600, 360
    prof = Image.new('RGB', (W, Hh), (20, 20, 30)); pd = ImageDraw.Draw(prof)
    for k in range(N - 1):
        x0, x1 = k * W / N, (k + 1) * W / N
        pd.line([(x0, Hh - Y[k] / 20000 * Hh), (x1, Hh - Y[k + 1] / 20000 * Hh)], fill=(255, 255, 255) if not gap[k] else (255, 80, 80))
        g0 = ground(X[k], Z[k]); pd.point((x0, Hh - g0 / 20000 * Hh), fill=(120, 90, 60))
    for m, k in MARK.items(): pd.text((k * W / N, 4), m[:7], fill=(200, 200, 100))
    prof.save(D + 'profile.png')
