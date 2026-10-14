# Wannies Island (PLATFORM.ILE, island 6; outside scenes 96-99, cubes (7,7)..(8,8)) -- the race track from the user's drawing (2026-10-14:
# "Proposed track for Platform.ile. Pink is jump, and purple is a boost pad. The massive blue circle is to be a giant frog that we drive
# through. The frog repeatedly opens and closes its mouth and extends its tongue as it does so, so we have to time the jump into the mouth.
# Exiting the frog we go up a slope to the top of the tower, then there's a loop coming down the tower looping around about three times,
# into the start finish line and the pit lane"; then: the boost pad and its jump a shortcut beside the road round the right, the road
# built up to the frog's jump "which then lifts the frog higher into the air, this will then make the slope to the top of the tower less
# steep", and a car meeting a shut mouth bounced back).
#
# The drawing is the editor's map turned a quarter anticlockwise and stretched (E:\dump\TEMP\wan\trace.py: u = 55 + 9 (z - 448),
# v = 5 + 5.82 (576 - x)); its line read off it (route_points.py) and laid on the island:
#  - the start on the pier south of the platform, south down it onto the east island, east and down its east side, west along its south
#    side (the boost pad and the jump a shortcut north of the road there: the next round), the west island's south loop, north-east and
#    north-west up to the frog's jump -- climbing all the way, the road high over the islands, to the frog high in the air;
#  - the jump into the frog's mouth, through the frog, out of its back, east over the lava along the island's north edge, climbing to the
#    elevator tower's top (the Wannies' elevator, bodies 19-31: 2,950 to 24,455);
#  - three times round the tower down a helix (a stadium round it: the tower stands 15 cells from the island's north edge, cube row 6
#    is missing), out of it south onto the pier and the start straight, the pit lane beside it.
#
# Run with E:\dump\TEMP\wan holding H.npy and obstacles.npz (E:\dump\TEMP\otr\prep.py E:/dump/TEMP/wan 448 448 128); --write writes
# docs/racetrack/wannies_track_plan.json; --pictures draws the lap over the island's map (E:\dump\TEMP\wan\lap.png).
import json, math, sys
import numpy as np

D = 'E:/dump/TEMP/wan/'
OUT = 'E:/dump/LBAAssembler/docs/racetrack/wannies_track_plan.json'
OX, OZ = 448, 448
H = np.nan_to_num(np.load(D + 'H.npy').astype(float), nan=0.0)
OB = np.load(D + 'obstacles.npz'); TOP, BOT, WHO, RQ = OB['top'], OB['bot'], OB['who'], int(OB['R'])
PRESENT = {(7, 7), (8, 7), (7, 8), (8, 8)}
SIZE = 128

def ground(x, z):
    xi, zi = min(SIZE - 1, max(0, int(x))), min(SIZE - 1, max(0, int(z))); fx, fz = x - xi, z - zi
    return H[zi, xi] * (1 - fx) * (1 - fz) + H[zi, xi + 1] * fx * (1 - fz) + H[zi + 1, xi] * (1 - fx) * fz + H[zi + 1, xi + 1] * fx * fz

STEP = 0.5
H0 = 4.75                               # the deck's half width to its rail (the other tracks')
ASPHALT, CURB = H0 - 1.0, H0 - 0.25
PIT = 4.5                               # the pit lane beside the start straight, on its left (east, over the lava beside the pier)
TOWER = (543.5, 467.5)                  # the elevator tower's middle (its top block 537.6..549.4 x 463.9..471.1, its legs 536.6..550.4 x 462.8..472.2)
TOWER_TOP = 24455
# the helix round the tower: a stadium -- two half circles (radius HR, their middles HZ0 and HZ1 on the tower's line north-south) joined
# by straights at x = TOWER.x -+ HR -- three times round clockwise, down from H_TOP; in from the west at its north point, out down its east
# straight
HR = 15.0
HZ0, HZ1 = 469.0, 478.0               # (the inner rail 10.25 from the middles: clear of the legs' corners, 9.3 off)
TURNS = 3
H_TOP = 25000.0                         # the helix's top: over the tower's top
H_BOTTOM = 3300.0                       # out of it
START_H = 2600.0                        # the start straight on the pier (its ground 2,000)
H_FROG = 20400.0                        # the frog's floor (the jump's lip and landing)
GRADE = 0.17
CLEAR, LAVA, LAVA_MIN = 400.0, 700.0, 1400.0

def C(x, z): return (x - OX, z - OZ)

# ---- the lap: corners (island cells) and their radii, from the start straight's foot round to the frog's take-off ramp
ROUND = [((545.5, 498.0), 0.0),          # (the start straight: south down the pier) ...
         ((547.5, 513.0), 8.0),          # ... onto the east island ...
         ((565.0, 520.0), 7.0),          # ... east, round north of its buildings ...
         ((566.5, 537.0), 7.0),          # ... south down its east side ...
         ((569.5, 548.0), 6.0),          # ... out to the island's edge past the mine's machinery ...
         ((569.5, 565.0), 5.0),          # ... round east and south of the mine's machinery (87-93: 8,657 high, x 555.8..566, z 551.9..559.1;
                                         # the island's edges x 576, z 576: the rail 1.75 cells in) ...
         ((549.0, 565.0), 8.0),          # ... west along its south side ...
         ((532.5, 555.5), 8.0),          # ... (the shortcut's fork: next round) ...
         ((513.0, 557.0), 8.0),
         ((498.0, 547.5), 7.0),          # ... (the shortcut's join) ...
         ((470.0, 548.0), 7.0),          # ... west onto the west island ...
         ((466.5, 567.0), 6.0),          # ... its south loop: south ...
         ((455.0, 569.5), 6.0),          # ... west ...
         ((455.5, 545.0), 7.0),          # ... north ...
         ((468.0, 532.5), 8.0),          # ... north-east ...
         ((497.5, 521.5), 8.0),          # ... east along the channel ...
         ((497.5, 507.0), 7.0),          # ... north ...
         ((477.0, 497.0), 7.0),          # ... north-west up to the frog ...
         ((477.0, 489.5), 0.0)]          # ... its take-off ramp's lip
FROG_LIP = (477.0, 489.5)               # the take-off lip (the drawing's pink dot 478.0, 489.6), the mouth (pink 477.3, 484.7)
FROG_MOUTH = (477.0, 484.5)
FROG_EXIT = (480.0, 467.0)              # out of its back (the drawing's line leaves at 483.7, 466.9)
APPROACH = [((477.0, 489.5), 0.0), ((477.0, 476.0), 6.0),   # through the frog, north ...
            ((486.0, 458.0), 8.0),       # ... out of its back, north-east over the lava ...
            ((530.0, 454.0), 6.0)]       # ... east along the island's north edge to the helix's top (its north point 543.5, 454)

def rounded(K, closed=False):
    """A polyline through corners rounded with their radii (a corner of radius 0 is a point the line passes through)."""
    n = len(K); P = [np.array(p, float) for p, r in K]; out = []
    arcs = []
    for i in range(n):
        if not closed and (i == 0 or i == n - 1) or K[i][1] <= 0: arcs.append(None); continue
        a, b, c = P[i - 1], P[i], P[(i + 1) % n]; r = K[i][1]
        u1 = (b - a) / np.linalg.norm(b - a); u2 = (c - b) / np.linalg.norm(c - b)
        turn = math.atan2(u1[0] * u2[1] - u1[1] * u2[0], u1 @ u2)
        t = r * math.tan(abs(turn) / 2)
        p1, p2 = b - u1 * t, b + u2 * t
        nrm = np.array([-u1[1], u1[0]]) * (1 if turn > 0 else -1)
        arcs.append((p1, p2, p1 + nrm * r, r, turn))
    for i in range(n if closed else n - 1):
        if arcs[i] is None: q1 = P[i]
        else:
            p1, p2, centre, r, turn = arcs[i]
            a0 = math.atan2(p1[1] - centre[1], p1[0] - centre[0])
            m = max(2, int(round(r * abs(turn) / 0.02)))
            for k in range(m): out.append(centre + r * np.array([math.cos(a0 + turn * k / m), math.sin(a0 + turn * k / m)]))
            q1 = p2
        j = (i + 1) % n
        q2 = P[j] if arcs[j] is None else arcs[j][0]
        if np.dot(q2 - q1, P[j] - P[i]) < -1e-6: sys.exit(f'corners {i} and {j}: their roundings overlap')
        m = max(1, int(round(np.linalg.norm(q2 - q1) / 0.02)))
        for k in range(m): out.append(q1 + (q2 - q1) * k / m)
    if not closed: out.append(P[-1])
    return np.array(out)

# the helix: the stadium's centre line from its north point clockwise, `turns` times round and on to the foot of its east straight
def stadium(turns_):
    tx = TOWER[0]
    north, east_top, east_bot = (tx, HZ0 - HR), (tx + HR, HZ0), (tx + HR, HZ1)
    pts = []
    def arc(cz, a0, a1):
        m = int(round(HR * abs(math.radians(a1 - a0)) / 0.02))
        for k in range(m):
            a = math.radians(a0 + (a1 - a0) * k / m); pts.append((tx + HR * math.cos(a), cz + HR * math.sin(a)))
    def straight(p, q):
        m = int(round(math.dist(p, q) / 0.02))
        for k in range(m): pts.append((p[0] + (q[0] - p[0]) * k / m, p[1] + (q[1] - p[1]) * k / m))
    for t in range(turns_):
        arc(HZ0, -90, 0)                                   # the north half circle's east quarter
        straight(east_top, east_bot)                       # down the east side
        arc(HZ1, 0, 180)                                   # the south half circle
        straight((tx - HR, HZ1), (tx - HR, HZ0))           # up the west side
        arc(HZ0, 180, 270)                                 # the north half circle's west quarter
    arc(HZ0, -90, 0)
    straight(east_top, east_bot)
    return np.array(pts)
helix = stadium(TURNS)
OUTWAY = [((TOWER[0] + HR, HZ1), 0.0), ((TOWER[0] + HR, 486.0), 9.0), ((546.5, 492.0), 0.0)]   # out of it south-west to the pier
lap_part = rounded(ROUND)
approach = rounded(APPROACH)
outway = rounded(OUTWAY)
join = rounded([(tuple(outway[-1]), 0.0), ((545.5, 498.0), 0.0)])
pieces = [lap_part, approach, helix, outway, join]
pts = pieces[0]
piece_at = [0.0]                         # where each piece starts round the lap (cells)
for p in pieces[1:]:
    piece_at.append(piece_at[-1] + float(np.linalg.norm(np.diff(pts[-2:] if False else np.vstack([pts[-1:], p[:1]]), axis=0), axis=1).sum())
                    + 0.0)
    pts = np.concatenate([pts, p[1:] if np.linalg.norm(p[0] - pts[-1]) < 1e-6 else p])
lengths = [float(np.linalg.norm(np.diff(p, axis=0), axis=1).sum()) for p in pieces]
piece_at = [0.0]
for i in range(1, len(pieces)):
    piece_at.append(piece_at[-1] + lengths[i - 1] + float(np.linalg.norm(pieces[i][0] - pieces[i - 1][-1])))
fine = np.array([C(x, z) for x, z in pts])
fine = fine[np.concatenate([[True], np.hypot(*np.diff(fine, axis=0).T) > 1e-9])]
seg = np.linalg.norm(np.diff(np.vstack([fine, fine[:1]]), axis=0), axis=1)
gapclose = seg[-1]
cum = np.concatenate([[0], np.cumsum(seg)]); total = cum[-1]
N = int(round(total / STEP)); s = np.arange(N) * total / N
X = np.interp(s, cum, np.append(fine[:, 0], fine[0, 0])); Z = np.interp(s, cum, np.append(fine[:, 1], fine[0, 1]))
T = np.stack([np.gradient(X), np.gradient(Z)], 1); T /= np.linalg.norm(T, axis=1)[:, None]
Nn = np.stack([-T[:, 1], T[:, 0]], 1)    # across the road (the builder's Across, (-Tz, Tx): west when heading south)
def ahead(a, b): return ((b - a) % N) * STEP
def between(k, a, b): return ahead(a, k) <= ahead(a, b)
def nearest_after(p, after, window):
    best, bd = after, 1e9
    for q in range(int(window / STEP)):
        k = (after + q) % N
        d = math.hypot(X[k] - (p[0] - OX), Z[k] - (p[1] - OZ))
        if d < bd: bd, best = d, k
    return best
def ease(f): f = min(1, max(0, f)); return f * f * (3 - 2 * f)

# ---- marks round the lap
MARK = {}
MARK['start'] = nearest_after((545.5, 508.0), 0, 30)                           # the start line on the pier
MARK['lip'] = nearest_after(FROG_LIP, 0, total)                                # the frog's take-off lip ...
MARK['foot'] = (MARK['lip'] - int(10 / STEP)) % N                              # ... its ramp's foot
MARK['mouth'] = nearest_after(FROG_MOUTH, MARK['lip'], 20)                     # ... the mouth: the landing
MARK['back'] = nearest_after(FROG_EXIT, MARK['mouth'], 40)                     # ... out of its back
def at_cells(c): return int(round(c / total * N)) % N
MARK['top'] = at_cells(piece_at[2])                                            # the helix's top: its north point, the first time
MARK['hOut'] = at_cells(piece_at[3])                                           # out of the helix
MARK['pier'] = at_cells(piece_at[4] + lengths[4])                              # on the pier: the start straight's foot

# ---- heights: up from the start straight to the frog at the same grade all the way (the road high over the islands), the frog level from
# its lip through it, up to the helix's top, down the helix at an even grade, down to the start straight
HALF = np.full(N, H0)
Y = np.zeros(N)
def climb(a, b, h0, h1, shape=lambda t: t):
    L = ahead(a, b)
    k = a
    while True:
        Y[k] = h0 + (h1 - h0) * shape(ahead(a, k) / L if L else 1)
        if k == b: break
        k = (k + 1) % N
climb(MARK['pier'], MARK['start'], START_H, START_H)
climb(MARK['start'], MARK['foot'], START_H, H_FROG - 900)
for k in range(MARK['foot'], MARK['lip'] + 1): Y[k % N] = H_FROG - 900 + 900 * ease(ahead(MARK['foot'], k) / ahead(MARK['foot'], MARK['lip']))
climb(MARK['lip'], MARK['back'], H_FROG, H_FROG)
climb(MARK['back'], MARK['top'], H_FROG, H_TOP, lambda t: t * t * (3 - 2 * t) * 0.5 + t * 0.5)
climb(MARK['top'], MARK['hOut'], H_TOP, H_BOTTOM)
climb(MARK['hOut'], MARK['pier'], H_BOTTOM, START_H, ease)
# (the jump into the frog: the gap from the lip to the mouth -- no road there)
gap = np.zeros(N, bool)
for k in range(MARK['lip'] + 1, MARK['mouth']): gap[k % N] = True
arcs = [('frog', MARK['foot'], MARK['lip'], MARK['mouth'], (MARK['mouth'] + int(4 / STEP)) % N)]

# ---- the pit lane: beside the start straight on its left (east: +Nn heading south is west -- the pit lane on -Nn), the deck widened
PIT_HALF = H0 + PIT / 2
PIT_MID = PIT_HALF - PIT / 2
iPit0 = (MARK['pier'] + 4) % N; iPit1 = (iPit0 + int(6 / STEP)) % N
iPit3 = nearest_after((547.5, 513.0), MARK['start'], 30); iPit2 = (iPit3 - int(5 / STEP)) % N
for k in range(N):
    if between(k, iPit0, iPit1): HALF[k] = H0 + (PIT_HALF - H0) * ease(ahead(iPit0, k) / ahead(iPit0, iPit1))
    elif between(k, iPit1, iPit2): HALF[k] = PIT_HALF
    elif between(k, iPit2, iPit3): HALF[k] = PIT_HALF + (H0 - PIT_HALF) * ease(ahead(iPit2, k) / ahead(iPit2, iPit3))

grade = (np.roll(Y, -1) - Y) / (STEP * 512)
ride = ~gap

# ---- checks
out = [f'lap {total:.1f} cells, {N} points (the last joint {gapclose:.2f} cells); heights {Y.min():.0f} to {Y.max():.0f}; steepest driven '
       f'{np.abs(grade[ride]).max() * 100:.1f} %']
for name, (a, b) in {'start to the frog': (MARK['start'], MARK['foot']), 'frog to the top': (MARK['back'], MARK['top']),
                     'the helix': (MARK['top'], MARK['hOut']), 'out of it': (MARK['hOut'], MARK['pier'])}.items():
    out.append(f'  {name}: {ahead(a, b):.0f} cells, {Y[a]:.0f} to {Y[b]:.0f}, mean grade {abs(Y[b] - Y[a]) / (ahead(a, b) * 512) * 100:.1f} %')
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
idx = np.arange(N); close = []; under = []
for i in range(0, N, 2):
    if gap[i]: continue
    dd = np.hypot(X - X[i], Z - Z[i]); sep = np.minimum(np.abs(idx - i), N - np.abs(idx - i))
    near_ = (sep > 40) & (dd < HALF + HALF[i] + 0.6) & ~gap
    if (near_ & (np.abs(Y - Y[i]) < 2800)).any():
        m = near_ & (np.abs(Y - Y[i]) < 2800); j = int(np.argmax(m)); close.append((abs(Y[i] - Y[j]), i, j))
    below = near_ & (Y < Y[i])
    if below.any(): under.append((Y[i] - Y[below].max(), i))
for dy, i, j in sorted(close)[:4]:
    out.append(f'  CLASH: ({X[i] + OX:.1f}, {Z[i] + OZ:.1f}) {Y[i]:.0f} and ({X[j] + OX:.1f}, {Z[j] + OZ:.1f}) {Y[j]:.0f}: {dy:.0f} apart')
if under:
    out.append('  stacked: the least ' + ', '.join(f'{c:.0f} at ({X[i] + OX:.1f}, {Z[i] + OZ:.1f})' for c, i in sorted(under)[:3]))
for m, k in MARK.items():
    out.append(f'    {m:6s} point {k:4d} ({X[k] + OX:6.1f}, {Z[k] + OZ:6.1f}) {Y[k]:6.0f} (ground {ground(X[k], Z[k]):5.0f})')
print('\n'.join(out))

plan = {
    'originCellX': OX, 'originCellZ': OZ,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in zip(X, Z)],
    'heights': [round(float(y), 1) for y in Y],
    'maxGrade': round(float(np.abs(grade[ride]).max()) + 0.05, 3),
    'raised': [0, N - 1], 'raisedHalf': H0, 'asphaltHalf': ASPHALT, 'curbHalf': CURB,
    'raisedHalfs': [round(float(h), 3) for h in HALF],
    'arcJumps': [[int(iF), int(iL), int(iD), int(iDF), 0] for name, iF, iL, iD, iDF in arcs],
    'start': int(MARK['start']),
    'pitStripe': [int(iPit1), int(iPit2), round(float(-(PIT_HALF - PIT)), 3)],
    'pitFence': True,
    'gridShift': PIT / 2,
    'raisedCut': True,
    'keepBodies': [19, 20, 21, 27, 28, 29, 30, 31],
}
if '--write' in sys.argv:
    json.dump(plan, open(OUT, 'w'))
    print('written', OUT)
np.savez(D + 'lap.npz', X=X + OX, Z=Z + OZ, Y=Y, HALF=HALF, gap=gap)

if '--pictures' in sys.argv:
    from PIL import Image, ImageDraw
    Zm = 6
    img = Image.open('E:/dump/TEMP/kmq/plat_top.png').convert('RGB')
    d = ImageDraw.Draw(img)
    P = lambda x, z: (x * Zm, z * Zm)
    lo, hi = Y.min(), Y.max()
    for k in range(N):
        j = (k + 1) % N
        t = (Y[k] - lo) / (hi - lo)
        col = (int(255 * t), int(255 * (1 - abs(2 * t - 1))), int(255 * (1 - t)))
        if gap[k]: col = (255, 255, 255)
        for o in (-HALF[k], HALF[k]):
            d.line([P(X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o), P(X[j] + Nn[j, 0] * o, Z[j] + Nn[j, 1] * o)], fill=col, width=1)
    for m, k in MARK.items(): d.text(P(X[k], Z[k]), m, fill=(255, 255, 255))
    img.save(D + 'lap.png')
