# Celebration Island before the statue (CELEBRAT): the lava lake lap, round the whole island. (2026-10-01, second version: the first,
# a figure of eight on the plateau, was 92 cells and too small, and its causeways paved the lava lake over.)
#
# The island is one cube: a dock along its west side (x 4-10, z 6-33, 420 high), a mesa over the rest of it -- a plateau 5,700-6,500 high
# round a lake of lava level with it (x 26-44, z 24-41), the temple on its west rim, two lava channels cutting its north side down to the
# sea, steep slopes all round down to the sea. The lap uses all of it, clockwise on the map, all of it a raised road on piers but the dock:
#   - the dock, heading north: the start line, the grid behind it;
#   - round onto the north shore, a low causeway over the sea in front of the lava falls: the first jump, over a gap in it;
#   - the long climb, 900 to 6,300: along the shore, round the north-east corner, down the east coast and round onto the south rim;
#   - north across the lava lake on a causeway over the lava: the second jump, over a gap in its middle;
#   - west along the north rim, over the west lava channel, round the north-west corner and south down the west cliffs behind the
#     temple: the third jump, over a gap beside the temple;
#   - a hairpin over the sea at the south-west corner, and the drop: off the end of the road, over the south-west hill and down onto
#     the dock, 5,900 below.
# The lake stays lava (a raised road leaves the ground as it is). Its jumps are gaps in the raised road (no road and no floor there), the
# drop a jump whose landing is lower than its take-off.
#
# Run with E:\dump\TEMP\celebrat holding H.npy and code.npy (the untouched island's heights and game codes). Writes
# docs/racetrack/celebrat_track_plan.json; --pictures draws the lap over the island and its height profile.
import json, math, os, sys
import numpy as np

D = 'E:/dump/TEMP/celebrat/'
OUT = 'E:/dump/LBAAssembler/docs/racetrack/celebrat_track_plan.json'
ORIGIN = 448
H = np.load(D + 'H.npy').astype(float)    # H[z, x], cells 0..64
CODE = np.load(D + 'code.npy')

def ground(x, z):
    xi, zi = min(63, max(0, int(x))), min(63, max(0, int(z))); fx, fz = x - xi, z - zi
    return (H[zi, xi] * (1 - fx) * (1 - fz) + H[zi, xi + 1] * fx * (1 - fz) + H[zi + 1, xi] * (1 - fx) * fz + H[zi + 1, xi + 1] * fx * fz)
def lava(x, z): return CODE[min(63, max(0, int(z))), min(63, max(0, int(x)))] in (9, 13)

# ---- the road: the dock's ground road a little narrower than the retail one, the raised road's rails RAISED_HALF from its middle
ASPHALT, CURB, VERGE, BLEND = 2.3, 2.8, 3.8, 2.0
RAISED_HALF = 3.05
STEP = 0.5
# the corners, clockwise on the map from the dock's north end, and their radii
V = [(7.2, 7.5),       # the dock's north end: onto the north shore
     (56.5, 7.5),      # the north-east corner: onto the east coast
     (57.0, 50.5),     # the south-east corner: onto the south rim
     (35.0, 50.5),     # onto the causeway north across the middle of the lake (and on up the west lava channel)
     (35.0, 17.0),     # onto the north rim, west
     (13.6, 17.0),     # the north-west corner: south down the west cliffs
     (13.6, 55.5),     # the hairpin's first half, west
     (7.2, 55.5)]      # its second half: north, to the drop
R = [3.5, 6.0, 6.0, 5.0, 4.0, 5.0, 3.0, 3.0]
START_Z = 13.5                     # the start line, on the dock heading north
FIRST = (7.2, 20.0)                # the lap's first point (on the dock, between the start line and the drop's landing)

# heights along the lap's stretches (set by where a point is), then smoothed and kept level over each jump
DOCK = 420.0
SHORE = 900.0                      # the north shore's causeway
TOP = 6300.0                       # the mesa: the south rim, the lake's causeway, the north rim, the west cliffs, the hairpin
NORTH_TOP = 6300.0                 # the north rim (the plateau stands up to 6,300 under it there: the road on it)
# the jumps: (name, the gap's middle, its length in cells): the gap is the road's own straight there, its lips half that either side
JUMPS = [('north shore', (34.5, 7.5), 7.0), ('lake', (35.0, 32.5), 8.0), ('temple', (13.6, 38.5), 7.0)]
DROP_LIP = (7.2, 45.5)             # the drop's take-off lip, heading north
DROP_LAND = (7.2, 33.0)            # ... and its landing lip, on the dock
RAMP, LANDING = 5.0, 5.0           # the ramps' lengths either side of a gap (RaceTrackOptions.JumpRampLength, JumpLandingLength)

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
    return np.array(pts)

fine = fillet(V, R)
# (the lap starts at FIRST: the fine line rolled round to its nearest point)
k0 = int(np.argmin(np.hypot(fine[:, 0] - FIRST[0], fine[:, 1] - FIRST[1])))
fine = np.roll(fine, -k0, axis=0)
seg = np.linalg.norm(np.diff(np.vstack([fine, fine[:1]]), axis=0), axis=1)
cum = np.concatenate([[0], np.cumsum(seg)]); total = cum[-1]
N = int(round(total / STEP)); s = np.arange(N) * total / N
X = np.interp(s, cum, np.append(fine[:, 0], fine[0, 0])); Z = np.interp(s, cum, np.append(fine[:, 1], fine[0, 1]))
T = np.stack([np.gradient(X), np.gradient(Z)], 1); T /= np.linalg.norm(T, axis=1)[:, None]
Nn = np.stack([-T[:, 1], T[:, 0]], 1)
def nearest(p, heading=None):
    d = np.hypot(X - p[0], Z - p[1])
    if heading is not None: d = d + 100 * (T @ np.array(heading) < 0.7)
    return int(np.argmin(d))
def ahead(a, b): return ((b - a) % N) * STEP

# ---- the stretches
iDockEnd = nearest((7.2, 11.0), (0, -1))          # the dock's road ends here, round its corner the raised road begins
iRaise = nearest((11.0, 7.5), (1, 0))             # the raised road's first point: off the dock, onto the shore
iClimb0 = nearest((47.0, 7.5), (1, 0))            # the climb from the shore's height ...
iClimb1 = nearest((42.0, 50.5), (-1, 0))          # ... to the mesa's
iNorth0 = nearest((31.0, 17.0), (-1, 0))          # the north rim
iNorth1 = nearest((18.5, 17.0), (-1, 0))
iLip = nearest(DROP_LIP, (0, -1)); iLand = nearest(DROP_LAND, (0, -1))
iStart = nearest((7.2, START_Z), (0, -1))

target = np.zeros(N)
for k in range(N):
    a = ahead(iRaise, k)
    if ahead(iRaise, k) > ahead(iRaise, iLip) or k == iLand: target[k] = DOCK          # the dock (and the drop's gap, which is the dock's level after it)
    elif a <= ahead(iRaise, iClimb0): target[k] = DOCK + (SHORE - DOCK) * min(1, a / 8)
    elif a <= ahead(iRaise, iClimb1):
        f = (a - ahead(iRaise, iClimb0)) / (ahead(iRaise, iClimb1) - ahead(iRaise, iClimb0))
        target[k] = SHORE + (TOP - SHORE) * (f * f * (3 - 2 * f) * 0.3 + f * 0.7)
    else: target[k] = TOP
    if ahead(iRaise, iNorth0) - 6 <= a <= ahead(iRaise, iNorth1) + 6:
        edge = min(a - (ahead(iRaise, iNorth0) - 6), ahead(iRaise, iNorth1) + 6 - a)
        target[k] = TOP + (NORTH_TOP - TOP) * min(1, edge / 6)
Y = target.copy()
# the jumps' level stretches: a ramp's length before the take-off lip to a landing ramp's length after the landing lip
jumps = []
fixed = np.zeros(N, bool)
for name, mid, gap in JUMPS:
    c = nearest(mid)
    lip = (c - int(round(gap / 2 / STEP))) % N; land = (c + int(round(gap / 2 / STEP))) % N
    jumps.append((name, lip, land))
    a = (lip - int(round((RAMP + 1) / STEP))) % N; b = (land + int(round((LANDING + 1) / STEP))) % N
    lvl = float(Y[c])
    k = a
    while True:
        Y[k] = lvl; fixed[k] = True
        if k == b: break
        k = (k + 1) % N
jumps.append(('drop', iLip, iLand))
# the drop: level at the top from a ramp's length before its lip, at the dock's after its landing lip
for k in range((iLip - int(round((RAMP + 1) / STEP))) % N, iLip + 1): Y[k % N] = TOP; fixed[k % N] = True
for k in range(iLip + 1, iLand): Y[k] = DOCK; fixed[k] = True
# smoothing (but not the level stretches), the raised road's own grade is free
w = int(round(4 / STEP))
for _ in range(3):
    pad_ = np.concatenate([Y[-w:], Y, Y[:w]])
    sm = np.convolve(pad_, np.ones(2 * w + 1) / (2 * w + 1), 'same')[w:-w]
    Y = np.where(fixed, Y, sm)
grade = (np.roll(Y, -1) - Y) / (STEP * 512)
raised = np.zeros(N, bool)
for k in range(N):
    raised[k] = ahead(iRaise, k) <= ahead(iRaise, iLip)

# ---- the drop's flight: from 2.5 cells before the lip to 3.5 past the landing lip, a hop then the dive (RaceTrackJumpAnim.DropAt)
# (the builder's: a hop as steep as the retail flight's first climb, clear of the take-off ramp's lip, then a smooth dive from a fifth of
# the way; the flight starts on the take-off ramp, RAMP_UP * Rise(0.5) up it, and ends 30 over the landing ramp, DROP_RAMP high)
HOP, DIVE_FROM, RAMP_UP, DROP_RAMP = 1200.0, 0.2, 800.0, 200.0
def rise(t):
    t = min(max(t, 0), 1); return (t * t / 0.6 if t < 0.3 else t - 0.15) / 0.85
def drop_at(u, drop):
    v = min(max((u - DIVE_FROM) / (1 - DIVE_FROM), 0), 1)
    return 4 * HOP * u * (1 - u) - drop * v * v * (3 - 2 * v)

# ---- checks
out = [f'lap {total:.1f} cells, {N} points; raised from point {iRaise} to {iLip} ({ahead(iRaise, iLip):.1f} cells), the dock {ahead(iLip, iRaise):.1f}']
edge = np.minimum.reduce([X, Z, 64 - X, 64 - Z])
out.append(f'  nearest the cube edge {edge.min():.1f} cells (the road needs 6.5)')
clear = [(Y[k] - ground(X[k], Z[k]), k) for k in range(N) if raised[k]]
low = min(clear)
out.append(f'  the raised road over the ground: {low[0]:.0f} at the least (point {low[1]}, ({X[low[1]]:.1f}, {Z[low[1]]:.1f}))')
gr = grade[raised & (np.arange(N) != iLip)]
out.append(f'  steepest {gr.max() * 100:.1f} % up, {-gr.min() * 100:.1f} % down on the raised road; heights {Y.min():.0f} to {Y.max():.0f}')
for c, k in sorted(clear)[:3]: out.append(f'    low over the ground: {c:.0f} at ({X[k]:.1f}, {Z[k]:.1f})')
for name, lip, land in jumps:
    gap = ahead(lip, land)
    before = (lip - int(round(RAMP / STEP))) % N
    turn = math.degrees(math.acos(np.clip(T[before] @ T[land], -1, 1)))
    out.append(f'  jump {name}: lips {gap:.1f} cells apart at ({X[lip]:.1f}, {Z[lip]:.1f}) -> ({X[land]:.1f}, {Z[land]:.1f}), level {Y[lip]:.0f} -> {Y[land]:.0f}, the road turns {turn:.0f} degrees over it')
    if name == 'drop':
        f0 = (lip - 5) % N; flight = gap + 2.5 + 3.5
        takeoff = RAMP_UP * rise(0.5); dropH = Y[lip] + takeoff - (Y[land] + DROP_RAMP * rise(1 - 3.5 / LANDING) + 30)
        worst = 1e9
        for k in range(-5, int(round((gap + 3.5) / STEP)) + 1):
            i = (lip + k) % N; u = (k * STEP + 2.5) / flight
            y = Y[lip] + takeoff + drop_at(u, dropH); g = ground(X[i], Z[i])
            worst = min(worst, y - g)
        lipClear = takeoff + drop_at(2.5 / flight, dropH) - RAMP_UP
        out.append(f'    the drop {dropH:.0f}, its flight {flight:.1f} cells: at the least {worst:.0f} over the ground under it, {lipClear:.0f} over the take-off lip')
# two parts of the lap side by side at one level
idx = np.arange(N); close = []
for i in range(0, N, 2):
    dd = np.hypot(X - X[i], Z - Z[i]); sep = np.minimum(np.abs(idx - i), N - np.abs(idx - i))
    near = (sep > 40) & (dd < 2 * RAISED_HALF + 0.2) & (np.abs(Y - Y[i]) < 1500)
    if near.any(): close.append((dd[near].min(), i))
if close: out.append(f'  WARNING: two parts of the lap {min(close)[0]:.1f} cells apart at one level, at ({X[min(close)[1]]:.1f}, {Z[min(close)[1]]:.1f})')
TEMPLE = (17.1, 31.7, 25.6, 41.8)
dx = np.maximum(np.maximum(TEMPLE[0] - X, 0), X - TEMPLE[2]); dz = np.maximum(np.maximum(TEMPLE[1] - Z, 0), Z - TEMPLE[3])
out.append(f'  the temple: the rails {(np.hypot(dx, dz) - RAISED_HALF)[raised].min():.1f} cells from it at the nearest')
out.append(f'  start line at point {iStart} ({X[iStart]:.1f}, {Z[iStart]:.1f}), heading ({T[iStart,0]:+.2f}, {T[iStart,1]:+.2f})')
print('\n'.join(out))

PIT_SPOTS = [[13.0, 17.0, 0.0, -1.0], [15.5, 17.0, 0.0, -1.0], [13.0, 20.5, 0.0, -1.0], [15.5, 20.5, 0.0, -1.0], [14.2, 23.5, 0.0, -1.0]]
plan = {
    'originCellX': ORIGIN, 'originCellZ': ORIGIN,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in zip(X, Z)],
    'heights': [round(float(y), 1) for y in Y],
    'maxGrade': round(float(max(grade[raised & (np.arange(N) != iLip)].max(), -grade[raised & (np.arange(N) != iLip)].min())) + 0.01, 3),
    'asphaltHalf': ASPHALT, 'curbHalf': CURB, 'vergeHalf': VERGE, 'blend': BLEND,
    'raised': [int(iRaise), int(iLip)], 'raisedHalf': RAISED_HALF, 'raisedCut': True,
    'gapJumps': [[int(lip), int(land)] for name, lip, land in jumps],
    'jumpRampLength': RAMP, 'jumpLandingLength': LANDING, 'jumpMinScale': 0.6,
    'start': int(iStart),
    'pitSpots': PIT_SPOTS,
    'gravity': 0.8,
    # the temple (its hall, its west tower and its roofs) stays where it is
    'keepBodies': [0, 1, 6, 7],
}
json.dump(plan, open(OUT, 'w'))
np.savez(D + 'lap2.npz', X=X, Z=Z, Y=Y, T=T, raised=raised)
print('wrote', OUT)

if '--pictures' in sys.argv:
    from PIL import Image, ImageDraw
    S, M = 14, 20
    im = Image.new('RGB', (64 * S + 2 * M,) * 2, (25, 45, 90)); d = ImageDraw.Draw(im)
    for zc in range(64):
        for xc in range(64):
            h = ground(xc + 0.5, zc + 0.5)
            if lava(xc + 0.5, zc + 0.5): col = (200, 40, 20)
            elif h <= 50: continue
            else: g = int(60 + 170 * h / 7500); col = (g, int(g * 0.85), int(g * 0.7))
            d.rectangle([M + xc * S, M + zc * S, M + xc * S + S - 1, M + zc * S + S - 1], fill=col)
    def px(x, z): return (M + x * S, M + z * S)
    for i in range(N):
        a = px(X[i] + Nn[i, 0] * RAISED_HALF, Z[i] + Nn[i, 1] * RAISED_HALF); b = px(X[i] - Nn[i, 0] * RAISED_HALF, Z[i] - Nn[i, 1] * RAISED_HALF)
        t = (Y[i] - 400) / 6200
        d.line([a, b], fill=(int(60 + 190 * t), int(60 + 120 * t), 200 - int(120 * t)) if raised[i] else (90, 90, 90), width=3)
    for name, lip, land in jumps:
        k = lip
        while k != land:
            d.line([px(X[k], Z[k]), px(X[(k + 1) % N], Z[(k + 1) % N])], fill=(255, 255, 0), width=5); k = (k + 1) % N
        d.text(px(X[lip] + 1, Z[lip] - 2), name, fill=(255, 255, 255))
    a = px(X[iStart] + Nn[iStart, 0] * 3.5, Z[iStart] + Nn[iStart, 1] * 3.5); b = px(X[iStart] - Nn[iStart, 0] * 3.5, Z[iStart] - Nn[iStart, 1] * 3.5)
    d.line([a, b], fill=(255, 255, 255), width=4)
    for i in range(0, N, 24):
        d.text(px(X[i] + 0.6, Z[i] + 0.6), f'{int(Y[i])}', fill=(230, 230, 230))
    im.save(D + 'lap2.png'); print('picture', D + 'lap2.png')
