# Celebration Island before the statue (CELEBRAT): the lava lake lap. The island is a mesa, its top a plateau round a lake of lava
# level with it, the temple on its west edge and lava channels pouring off its north side. The lap is a figure of eight on the
# plateau: a loop over the north (the plateau's north-west block, a causeway over the mouth of the north channel, where it leaves the
# lake, and the tongue of rock east of it) and a loop over the south (the ruins' terrace), joined by two causeways that cross in the
# middle of the lake. Both causeways jump there: each has a ramp up to a lip on its side of the crossing and a ramp down beyond it, so
# the two leaps cross over one hole of lava in the lake's middle. The start line is on the north side, before the channel's mouth; the
# grid stands behind it round the north-west corner, and the other cars wait for the qualifying lap in the ruins south of the temple.
#
# The road keeps within WALL_FREE of the ground under its middle: further, and the builder walls it (blocking rock at the road's edges,
# RaceTrackBuilder.PlannedProfile), which on a lap of tight corners stops a car that cuts one -- the north side crosses the channel where
# it is still nearly level with the lake, and the road rises over the north-west block's knoll and the south side's mound.
#
# Run with E:\dump\TEMP\celebrat holding H.npy and code.npy (the cube's heights and game codes; made from the untouched CELEBRAT.ILE
# when missing). Writes docs/racetrack/celebrat_track_plan.json; --pictures draws the lap over the island and its height profile.
import json, math, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

D = 'E:/dump/TEMP/celebrat/'
OUT = 'E:/dump/LBAAssembler/docs/racetrack/celebrat_track_plan.json'
ORIGIN = 448                      # the cube's first island cell (cube 7,7)
if not os.path.exists(D + 'H.npy') or not os.path.exists(D + 'code.npy'):
    from ile import load_ile
    m, ground_, objtex, cubes = load_ile('E:/dump/LBA2RaceTrackBuild/Pristine/CELEBRAT.ILE')
    c = list(cubes.values())[0]
    np.save(D + 'H.npy', c.heights)
    np.save(D + 'code.npy', np.array([[(int(c.polys[z, x * 2]) >> 12) & 15 for x in range(64)] for z in range(64)]))
H = np.load(D + 'H.npy').astype(float)    # H[z, x], cells 0..64
CODE = np.load(D + 'code.npy')              # game code per cell: 9 and 13 lava

# ---- the road: a little narrower than the retail road (the plateau is 30 cells across)
ASPHALT, CURB, VERGE, BLEND = 2.75, 3.25, 4.25, 3.0
STEP = 0.5
# the lap's corners in the order it runs, and each corner's radius. 0-1 the south side (east), 1 the south-east corner, 2 onto the first
# causeway (north-west across the lake), 3-4 up onto the north-west block and round, 4-5 the north side (east, over the channel's
# mouth), 5 round onto the tongue, 6 onto the second causeway (south-west across the lake), 7 down onto the terrace and 0 round onto the
# south side.
V = [(30.5, 44.5), (47.5, 44.5), (47.5, 39.5), (24.5, 25.5), (24.5, 20.5), (41.5, 20.5), (41.5, 24.5), (30.5, 40.5)]
R = [3.0, 3.5, 2.5, 3.0, 3.2, 3.0, 3.0, 3.0]
LIP = 4.5                          # each lip, cells from the crossing along its causeway (the other causeway's curbs are 3.25 across it)
RAMP, LANDING = 6.0, 6.0           # the ramps' lengths (RaceTrackOptions.JumpRampLength, JumpLandingLength, from the plan)
LAKE_LEVEL = 6050.0                # both causeways, level through the jump (the lake's lava is at 5700-5800)
WALL_FREE = 1150                   # the most the road is built up or cut down from the ground under its middle (the builder walls at 1200)
MAXG = 0.09

def fillet(V, R):
    n = len(V); V = [np.array(v, float) for v in V]; tang = []
    for i in range(n):
        a, v, b = V[i - 1], V[i], V[(i + 1) % n]
        d1 = (v - a) / np.linalg.norm(v - a); d2 = (b - v) / np.linalg.norm(b - v)
        ang = math.acos(np.clip(d1 @ d2, -1, 1)); t = R[i] * math.tan(ang / 2)
        tang.append((v - d1 * t, v + d2 * t, d1, d2, ang, t))
    pts = []
    for i in range(n):
        p1, p2, d1, d2, ang, t = tang[i]
        if ang > 1e-6:
            side = np.sign(d1[0] * d2[1] - d1[1] * d2[0]); n1 = np.array([-d1[1], d1[0]]) * side; c = p1 + n1 * R[i]
            a0 = math.atan2(p1[1] - c[1], p1[0] - c[0]); a1 = a0 + side * ang
            m = max(2, int(round(R[i] * ang / 0.02)))
            pts += [c + R[i] * np.array([math.cos(a0 + (a1 - a0) * k / m), math.sin(a0 + (a1 - a0) * k / m)]) for k in range(m)]
        q1, q2 = tang[i][1], tang[(i + 1) % n][0]
        if np.dot(q2 - q1, tang[(i + 1) % n][2]) < -1e-6: sys.exit(f'corners {i} and {(i + 1) % n} overlap')
        m = max(1, int(round(np.linalg.norm(q2 - q1) / 0.02)))
        pts += [q1 + (q2 - q1) * k / m for k in range(m)]
    return np.array(pts), tang

fine, tang = fillet(V, R)
seg = np.linalg.norm(np.diff(np.vstack([fine, fine[:1]]), axis=0), axis=1)
cum = np.concatenate([[0], np.cumsum(seg)]); total = cum[-1]
N = int(round(total / STEP)); s = np.arange(N) * total / N
X = np.interp(s, cum, np.append(fine[:, 0], fine[0, 0])); Z = np.interp(s, cum, np.append(fine[:, 1], fine[0, 1]))
T = np.stack([np.gradient(X), np.gradient(Z)], 1); T /= np.linalg.norm(T, axis=1)[:, None]
Nn = np.stack([-T[:, 1], T[:, 0]], 1)
def along(a, b): return ((b - a) % N) * STEP          # cells from point a forward to point b

# ---- the crossing: the two causeways (sides 2-3 and 6-7) meet in the lake
def line_x(p, q, r_, t_):
    p, q, r_, t_ = map(np.array, (p, q, r_, t_))
    d1, d2 = q - p, t_ - r_
    a = np.array([[d1[0], -d2[0]], [d1[1], -d2[1]]]); u, w = np.linalg.solve(a, r_ - p)
    return p + d1 * u
CX, CZ = line_x(V[2], V[3], V[6], V[7])
near = np.hypot(X - CX, Z - CZ)
c1 = int(np.argmin(np.where(np.arange(N) < N // 2, near, 1e9)))
c2 = int(np.argmin(np.where(np.arange(N) >= N // 2, near, 1e9)))
angle = math.degrees(math.acos(abs(T[c1] @ T[c2])))
li = int(round(LIP / STEP))
jumps = [((c1 - li) % N, (c1 + li) % N), ((c2 - li) % N, (c2 + li) % N)]

# ---- heights: the ground along the lap (the road's footprint), over the lava the causeways' level; the jumps level; then each kink
# rounded, the road kept within WALL_FREE of the ground under its middle, and the grades limited
def ground(x, z):
    xi, zi = min(63, max(0, int(x))), min(63, max(0, int(z)))
    fx, fz = x - xi, z - zi
    return (H[zi, xi] * (1 - fx) * (1 - fz) + H[zi, xi + 1] * fx * (1 - fz) + H[zi + 1, xi] * (1 - fx) * fz + H[zi + 1, xi + 1] * fx * fz)
def lava(x, z): return CODE[min(63, max(0, int(z))), min(63, max(0, int(x)))] in (9, 13)
G = np.array([ground(x, z) for x, z in zip(X, Z)])
foot = np.array([np.median([ground(X[k] + Nn[k, 0] * t, Z[k] + Nn[k, 1] * t) for t in np.linspace(-CURB, CURB, 7)]) for k in range(N)])
base = np.clip(np.where([lava(x, z) for x, z in zip(X, Z)], LAKE_LEVEL, foot), 5950, 6400)
fixed = np.zeros(N, bool); target = base.copy()
def hold(a, b, level):
    k = a
    while True:
        target[k] = level; fixed[k] = True
        if k == b: break
        k = (k + 1) % N
pad = int(round((RAMP + 0.5) / STEP)), int(round((LANDING + 0.5) / STEP))
for lip, land in jumps: hold((lip - pad[0]) % N, (land + pad[1]) % N, LAKE_LEVEL)
Y = target.copy()
w = int(round(8 / STEP))
lim = MAXG * STEP * 512 * 0.9
for _ in range(3):
    pad_ = np.concatenate([Y[-w:], Y, Y[:w]])
    sm = np.convolve(pad_, np.ones(2 * w + 1) / (2 * w + 1), 'same')[w:-w]
    Y = np.where(fixed, target, sm)
# no deeper than WALL_FREE into the ground: raised there, and the grade limited by raising the road either side (a hump over the
# knoll), never by cutting the knoll back down
Y = np.where(fixed, Y, np.maximum(Y, G - WALL_FREE))
for _ in range(2 * N):
    changed = False
    for k in range(N):
        if fixed[k]: continue
        need = max(Y[(k - 1) % N], Y[(k + 1) % N]) - lim
        if Y[k] < need - 1e-6: Y[k] = need; changed = True
    if not changed: break
grade = (np.roll(Y, -1) - Y) / (STEP * 512)

# ---- the start line, the grid behind it, and where the others wait
# on the north side, just before the causeway over the channel's mouth: the grid stands behind it round the north-west corner (the
# race-track mode lines the cars up along the lap); out of the south-east corner, where the first causeway starts, the line's end
# reached across the south side the grid would stand on
START = int(np.argmin(np.hypot(X - 33.0, Z - V[4][1])))
PIT_SPOTS = [[21.5, 46.0, 1.0, 0.0], [21.5, 49.0, 1.0, 0.0], [25.0, 43.2, 1.0, 0.0], [26.5, 48.5, 1.0, 0.0], [18.5, 44.5, 1.0, 0.0]]

# ---- checks
out = []
out.append(f'lap {total:.1f} cells, {N} points; crossing at ({CX:.1f}, {CZ:.1f}), points {c1} and {c2}, {angle:.0f} degrees apart')
def straight(k):
    def run(dirn):
        m = 0
        while m < N // 2 and abs(T[(k + dirn * (m + 1)) % N] @ T[k]) > math.cos(math.radians(1.0)): m += 1
        return m * STEP
    return run(-1), run(1)
for name, (lip, land), c in (('first causeway', jumps[0], c1), ('second causeway', jumps[1], c2)):
    before, after = straight(c)
    gap = along(lip, land)
    sc = max(0.6, math.ceil((2.5 + gap + 3.5) * 512 / 8990 * 100) / 100); fl = 8990 * sc / 512
    out.append(f'  {name}: lips {gap:.1f} apart; flight x{sc:.2f} = {fl:.1f} cells from 2.5 before the take-off lip to {fl - 2.5 - gap:.1f} past the landing lip; '
               f'straight {before:.1f} before the crossing and {after:.1f} after (needs {LIP + 2.5:.1f} and {fl - 2.5 - LIP:.1f})')
edge = np.minimum.reduce([X, Z, 64 - X, 64 - Z])
out.append(f'  nearest the cube edge {edge.min():.1f} cells')
TEMPLE = (17.1, 31.7, 25.6, 41.8)
dx = np.maximum(np.maximum(TEMPLE[0] - X, 0), X - TEMPLE[2]); dz = np.maximum(np.maximum(TEMPLE[1] - Z, 0), Z - TEMPLE[3])
out.append(f'  the temple: the curbs {np.hypot(dx, dz).min() - CURB:.1f} cells from it at the nearest')
idx = np.arange(N); close = []
for i in range(0, N, 2):
    dd = np.hypot(X - X[i], Z - Z[i]); sep = np.minimum(np.abs(idx - i), N - np.abs(idx - i))
    nr = (sep > 40) & (dd < 2 * VERGE) & (np.hypot(X - CX, Z - CZ) > 9) & (np.hypot(X[i] - CX, Z[i] - CZ) > 9)
    if nr.any(): close.append((dd[nr].min(), i))
if close: out.append(f'  WARNING: parts of the lap {min(close)[0]:.1f} cells apart at ({X[min(close)[1]]:.1f}, {Z[min(close)[1]]:.1f})')
off = Y - G
if np.abs(off).max() > 1200: out.append(f'  WARNING: the road is {off[np.abs(off).argmax()]:+.0f} from the ground at ({X[np.abs(off).argmax()]:.1f}, {Z[np.abs(off).argmax()]:.1f}): walled there')
out.append(f'  the road against the ground under its middle: {off.min():+.0f} to {off.max():+.0f}')
fill = Y - np.array([min(ground(X[k] + Nn[k, 0] * CURB, Z[k] + Nn[k, 1] * CURB), ground(X[k] - Nn[k, 0] * CURB, Z[k] - Nn[k, 1] * CURB)) for k in range(N)])
out.append(f'  the most built up under a curb: {fill.max():.0f} at ({X[fill.argmax()]:.1f}, {Z[fill.argmax()]:.1f})')
out.append(f'  steepest {grade.max() * 100:.1f} % up, {-grade.min() * 100:.1f} % down; heights {Y.min():.0f} to {Y.max():.0f}')
out.append(f'  start line at point {START} ({X[START]:.1f}, {Z[START]:.1f})')
print('\n'.join(out))

plan = {
    'originCellX': ORIGIN, 'originCellZ': ORIGIN,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in zip(X, Z)],
    'heights': [round(float(y), 1) for y in Y],
    'maxGrade': round(float(max(grade.max(), -grade.min())) + 0.01, 3),
    'asphaltHalf': ASPHALT, 'curbHalf': CURB, 'vergeHalf': VERGE, 'blend': BLEND,
    'gapJumps': [[int(a), int(b)] for a, b in jumps],
    'jumpRampLength': RAMP, 'jumpLandingLength': LANDING, 'jumpMinScale': 0.6,
    'start': int(START),
    'pitSpots': PIT_SPOTS,
    # the temple (its hall, its west tower and its roofs) stays where it is
    'keepBodies': [0, 1, 6, 7],
}
json.dump(plan, open(OUT, 'w'))
np.savez(D + 'lap.npz', X=X, Z=Z, Y=Y, G=G, T=T, Nn=Nn, jumps=np.array(jumps), start=START)
print('wrote', OUT)

if '--pictures' in sys.argv:
    from PIL import Image, ImageDraw
    S, M = 16, 30
    im = Image.open(D + 'base.png').convert('RGB') if os.path.exists(D + 'base.png') else Image.new('RGB', (64 * S + 2 * M,) * 2, 'white')
    ov = Image.new('RGBA', im.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
    def px(x, z): return (M + x * S, M + z * S)
    for i in range(0, N, 1):
        a = px(X[i] + Nn[i, 0] * CURB, Z[i] + Nn[i, 1] * CURB); b = px(X[i] - Nn[i, 0] * CURB, Z[i] - Nn[i, 1] * CURB)
        d.line([a, b], fill=(40, 40, 40, 90), width=5)
    gapped = np.zeros(N, bool)
    for lip, land in jumps:
        k = lip
        while k != land: gapped[k] = True; k = (k + 1) % N
    for i in range(N):
        t = (Y[i] - 5900) / 600
        col = (255, 60, 255, 255) if gapped[i] else (int(255 * min(1, max(0, t))), 200, int(255 * (1 - min(1, max(0, t)))), 255)
        d.line([px(X[i], Z[i]), px(X[(i + 1) % N], Z[(i + 1) % N])], fill=col, width=4)
    for k in range(0, N, 24):
        a = px(X[k], Z[k]); b = px(X[k] + T[k, 0] * 2, Z[k] + T[k, 1] * 2); d.line([a, b], fill=(255, 0, 0, 255), width=3)
    a = px(X[START] + Nn[START, 0] * CURB, Z[START] + Nn[START, 1] * CURB); b = px(X[START] - Nn[START, 0] * CURB, Z[START] - Nn[START, 1] * CURB)
    d.line([a, b], fill=(255, 255, 255, 255), width=4)
    for x, z, hx, hz in PIT_SPOTS: d.rectangle([px(x - 0.8, z - 0.8), px(x + 0.8, z + 0.8)], outline=(255, 255, 255, 255), width=2)
    Image.alpha_composite(im.convert('RGBA'), ov).convert('RGB').save(D + 'design_top.png')
    W, Hh = 1200, 260
    pr = Image.new('RGB', (W, Hh), (20, 20, 30)); d2 = ImageDraw.Draw(pr)
    lo, hi = 3000, 7500
    def py(v): return Hh - 10 - (v - lo) / (hi - lo) * (Hh - 20)
    for i in range(N - 1):
        d2.line([(s[i] / total * W, py(Y[i])), (s[i + 1] / total * W, py(Y[i + 1]))], fill=(255, 220, 0), width=2)
        d2.point((s[i] / total * W, py(G[i])), fill=(120, 200, 120))
    pr.save(D + 'design_profile.png')
    print('pictures in', D)
