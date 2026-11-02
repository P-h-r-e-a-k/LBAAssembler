# The Long Table (HARVEST.ILE, an island of its own, made by the build: Terrain/Harvest). The user, 2026-11-02: "Spend a couple of hours
# designing a thanksgiving track. This will be a totally new track we create from scratch instead of based on anything. Let's make it Canadian
# thanksgiving related too and not just American". The design and its sources: docs/LBA2_HARVEST_TRACK.md.
#
# One island, two Thanksgivings, one table. The north half is Canada's in October, the south half the United States' in November, the border
# between them, and across the border the Long Table, big enough to drive on. The lap is a figure of eight: over the table from October into
# November, and under it, between its legs, back again.
#
# The whole lap is a raised road (as Volcano Island's): the road passes over itself at the table, and the ground under it is made by this
# script to fit it (the land a little under the deck, the bog and the lake lower). Clockwise round Canada, anticlockwise round the States:
#
#  - the Fall Fair: the start on the north straight, heading east, the pit lane on its right;
#  - the Regatta: on over the lake of the giant pumpkin boats, the leap over their lane (landing no lower than it leaves);
#  - the Maple Run: up through the red hills to the table's height;
#  - the Horn of Plenty: into the cornucopia's tip, out of its mouth onto the table, the first border crossing on its cloth;
#  - the Long Table: west along it, Canada's end (butter tarts, pumpkin pie) to the States' (pecan pie, Mash Mountain, its gravy), round the
#    turkey and the mountain;
#  - the Pepper Mill: off the table's west end and down round the mill, a turn and a quarter;
#  - the Harbour: south along the wharves, the drawbridge's leap;
#  - Parade Avenue: east along the south coast under the balloons;
#  - the Turkey Bowl: through the stadium, round the field;
#  - the Corn Maze: narrow, walled in corn, back and forth;
#  - Under the Table: north between its legs and the chairs', the second border crossing;
#  - the Bog: west on a boardwalk over the flooded cranberries;
#  - the Habitation: through the courtyard of the fort, its hall's feast laid; and round onto the north straight.
#
# --check prints the checks; --pictures draws the map; --write writes docs/racetrack/harvest_track_plan.json and the ground.
import json, math, os, sys, zlib
import numpy as np

OUT = 'E:/dump/LBAAssembler/docs/racetrack/'
PICTURES = 'E:/dump/TEMP/harvest/'
OX, OZ = 256, 256                       # the island's cells from cube (4, 4); the plan's points are cells from there
CUBES_X, CUBES_Z = 6, 5
SIZE_X, SIZE_Z = CUBES_X * 64, CUBES_Z * 64
BORDER_Z = 160.0
STEP = 0.5
H0, ASPHALT, CURB = 5.0, 4.0, 4.75      # the road's widths: to the rail, the asphalt's edge, the curbs' outer edge
GRADE = 0.15                            # the steepest it is driven (Temple's verdict: 80 km/h and a car that can still be steered)

LAND, BOG, LAKE, TABLE_Y = 900.0, 620.0, 900.0, 6200.0
HELIX_X, HELIX_Z, HELIX_R = 92.0, 183.0, 18.0
TABLE = (118.0, 142.0, 280.0, 178.0)    # the table's top: x0, z0, x1, z1
MASH_Z = 151.5                          # Mash Mountain's middle, its foot between the table's edge and the road

# A circle as corners (the way volcano_design.py winds its spirals): the polygon round it, each corner rounded with the circle's own radius,
# from angle a0 to a1 (degrees: 0 east, 90 south; less and less is anticlockwise on the map)
def circle(cx, cz, r, a0, a1, step=30.0):
    n = max(1, int(round(abs(a1 - a0) / step))); th = math.radians((a1 - a0) / n); R = r / math.cos(th / 2)
    return [((cx + R * math.cos(math.radians(a0) + (q + 0.5) * th), cz + R * math.sin(math.radians(a0) + (q + 0.5) * th)), r) for q in range(n)]

NZ = 46.0                               # the north straight
K = [   # the north straight, on over the lake
     ((298, NZ), 30), ((352, 72), 30),
        # the Regatta's leap, heading south; the Maple Run's bends, climbing
     ((358, 136), 14), ((302, 106), 12), ((312, 160), 16),
        # the table, west: its cloth, the horn, then round the turkey (south of the road) and Mash Mountain (north of it)
     ((262, 160), 40), ((226, 150), 40), ((190, 150), 40), ((160, 168), 40), ((134, 168), 30), ((120, 165), 30)]
K += circle(HELIX_X, HELIX_Z, HELIX_R, -90.0, -90.0 - 450.0)
K += [  # the harbour, south; the corner into Parade Avenue, its square half way along
     ((66, 288), 34), ((120, 288), 60), ((170, 272), 60), ((220, 288), 60),
        # the stadium: in by its south-west gate, round the field, out by the north-west
     ((352, 290), 30), ((354, 214), 30),
        # the Corn Maze
     ((262, 214), 8), ((258, 258), 8), ((230, 258), 8), ((232, 204), 8), ((214, 204), 8), ((212, 262), 8), ((194, 262), 8),
        # under the table, north, a chicane between its legs and the chairs'; the Bog, west on its boardwalk
     ((194, 204), 24), ((204, 182), 24), ((184, 142), 24), ((194, 116), 24), ((194, 96), 14), ((150, 84), 30), ((110, 112), 30),
        # the Habitation, north through its courtyard, and round onto the straight
     ((48, 118), 16), ((42, NZ), 20)]

def rounded_polyline(K):
    n = len(K); P = [np.array(p, float) for p, r in K]; out = []; arcs = []
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
        if np.dot(q2 - q1, P[(i + 1) % n] - P[i]) < -1e-6: sys.exit(f'corners {i} {K[i]} and {i + 1} {K[(i + 1) % n]}: their roundings overlap')
        m = max(1, int(round(np.linalg.norm(q2 - q1) / 0.02)))
        for k in range(m): out.append(q1 + (q2 - q1) * k / m)
    return np.array(out)

fine = rounded_polyline(K)
START = (150.0, NZ)                     # the start line, heading east, the grid behind it on the level
k0 = int(np.argmin(np.hypot(fine[:, 0] - (START[0] - 60), fine[:, 1] - START[1])))
fine = np.roll(fine, -k0, axis=0)
seg = np.linalg.norm(np.diff(np.vstack([fine, fine[:1]]), axis=0), axis=1)
cum = np.concatenate([[0], np.cumsum(seg)]); total = cum[-1]
N = int(round(total / STEP)); s = np.arange(N) * total / N
X = np.interp(s, cum, np.append(fine[:, 0], fine[0, 0])); Z = np.interp(s, cum, np.append(fine[:, 1], fine[0, 1]))
T = np.stack([np.gradient(X), np.gradient(Z)], 1); T /= np.linalg.norm(T, axis=1)[:, None]
Nn = np.stack([-T[:, 1], T[:, 0]], 1)    # across the road (the builder's Across, (-Tz, Tx): south when heading east)

def ahead(a, b): return ((b - a) % N) * STEP
def near(x, z, heading=None, after=None, window=None):
    d = np.hypot(X - x, Z - z)
    if heading is not None:
        h = np.array(heading, float); h /= np.linalg.norm(h)
        d = np.where(T @ h > 0.5, d, 1e9)
    if after is not None:
        d = np.where(((np.arange(N) - after) % N) * STEP <= (window or total), d, 1e9)
    return int(np.argmin(d))
E_, W_, S_, N_ = (1, 0), (-1, 0), (0, 1), (0, -1)

# ---- the lap's places
M = {}
M['start'] = near(START[0], NZ, E_)
M['lake0'] = near(250, NZ, E_)
M['leapFoot'] = near(353, 78, S_); M['leapLip'] = near(353.3, 84, S_); M['leapLand'] = near(354.3, 104, S_); M['leapEnd'] = near(354.6, 110, S_)
M['maple0'] = near(354.6, 112, S_)
M['cloth0'] = near(304, 160, W_)         # the foot of the cloth: the table's height from the horn on
M['horn0'] = near(286, 160, W_)
M['table0'] = near(272, 160, W_, after=M['horn0'], window=30)
M['border1'] = near(200, 150, W_)
M['table1'] = near(118, 165, W_)
M['helix0'] = near(HELIX_X, 165, W_, after=M['table1'], window=60)
M['helix1'] = (M['helix0'] + int(2 * math.pi * HELIX_R * 1.25 / STEP)) % N
M['bridgeFoot'] = near(71.5, 214, S_); M['bridgeLip'] = near(71, 220, S_); M['bridgeLand'] = near(69.5, 240, S_); M['bridgeEnd'] = near(69, 246, S_)
M['parade0'] = near(90, 288, E_); M['parade1'] = near(268, 288, E_)
M['bowl0'] = near(282, 288, E_); M['bowl1'] = near(286, 214, W_)
M['maze0'] = near(272, 214, W_)
M['under0'] = near(203, 186, N_); M['border2'] = near(194, BORDER_Z, N_); M['under1'] = near(186, 134, N_)
M['bog0'] = near(180, 90, W_); M['bog1'] = near(66, 116, W_)
M['fort0'] = near(46, 100, N_); M['fort1'] = near(44, 70, N_)
order = sorted(M, key=lambda k: (M[k] - M['start']) % N)

# ---- the heights: the land's, the bog's, the table's; straight ramps at the driven grade, smoothed
floor = np.full(N, LAND)
def span(a, b): return (np.arange(N) - a) % N <= (b - a) % N
floor[span(M['bog0'], M['bog1'])] = BOG
floor[span(M['horn0'], M['table1'])] = TABLE_Y
# the Maple Run: its hills' road a little over the land, rising to the table's height at the horn (the cloth's last stretch steady)
Y = floor.copy()
def ramp(a, b, y0, y1):
    k = a; n = (b - a) % N or 1; i = 0
    while True:
        f = i / n; Y[k] = y0 + (y1 - y0) * (f * f * (3 - 2 * f)); i += 1
        if k == b: break
        k = (k + 1) % N
ramp(M['maple0'], M['horn0'], LAND, TABLE_Y)
ramp(M['table1'], M['helix0'], TABLE_Y, TABLE_Y)
# the Pepper Mill: down at a steady rate from the table to the land
ramp(M['helix0'], M['helix1'], TABLE_Y, LAND)
ramp((M['bog0'] - int(16 / STEP)) % N, M['bog0'], LAND, BOG)
ramp(M['bog1'], (M['bog1'] + int(16 / STEP)) % N, BOG, LAND)
# (the corners smoothed: a car feels a change of grade)
for _ in range(2):
    Y = np.mean([np.roll(Y, d) for d in range(-10, 11)], axis=0)
Y[span(M['horn0'], M['table1'])] = TABLE_Y

# ---- the carried jumps (RaceArc): the ramp's foot, its lip, the landing lip, the landing's foot; the ramp's angle
JUMPS = [('regatta', 'leapFoot', 'leapLip', 'leapLand', 'leapEnd', 16.0), ('drawbridge', 'bridgeFoot', 'bridgeLip', 'bridgeLand', 'bridgeEnd', 15.0)]
gap = np.zeros(N, bool); arcs = []
for name, a, b, c, d, deg in JUMPS:
    iF, iL, iD, iDF = M[a], M[b], M[c], M[d]; th = math.radians(deg)
    y0, y3 = Y[iF], Y[iDF]
    k1, L_, k2 = ahead(iF, iL) * 512, ahead(iL, iD) * 512, ahead(iD, iDF) * 512
    R1 = k1 / math.sin(th); yl = y0 + R1 * (1 - math.cos(th)); sl = math.tan(th)
    land_y = max(yl, y3)                 # (as high as it leaves, at the least: no downward jumps)
    k = iF
    while True:
        dist = ahead(iF, k) * 512
        if dist <= k1: Y[k] = y0 + R1 - math.sqrt(max(0, R1 * R1 - dist * dist))
        elif dist <= k1 + L_:
            u = dist - k1; a_ = (land_y - yl - sl * L_) / (L_ * L_); Y[k] = yl + sl * u + a_ * u * u
        else:
            f = (dist - k1 - L_) / k2; Y[k] = land_y + (y3 - land_y) * (f * f * (3 - 2 * f))
        if k == iDF: break
        k = (k + 1) % N
    k = (iL + 1) % N
    while k != iD: gap[k] = True; k = (k + 1) % N
    arcs.append((name, iF, iL, iD, iDF, yl, land_y))

# ---- the road's width
halfs = np.full(N, H0)
halfs[span(M['maze0'], M['under0'] - int(12 / STEP))] = 4.5
halfs[span(M['table0'], M['table1'])] = 5.5
halfs[span((M['start'] - int(70 / STEP)) % N, (M['start'] + int(40 / STEP)) % N)] = 7.0
halfs = np.convolve(np.concatenate([halfs[-20:], halfs, halfs[:20]]), np.ones(41) / 41, mode='valid')

# ---- checks
grade = (np.roll(Y, -1) - Y) / (STEP * 512)
ride = ~np.zeros(N, bool)
for name, iF, iL, iD, iDF, _, _ in arcs: ride[span(iF, iDF)] = False
out = [f'lap {total:.1f} cells, {N} points; heights {Y.min():.0f} to {Y.max():.0f}; steepest driven {np.abs(grade[ride]).max() * 100:.1f} % (limit {GRADE * 100:.0f})']
kappa = np.abs(T[:, 0] * np.gradient(T[:, 1]) - T[:, 1] * np.gradient(T[:, 0])) / STEP
tightest = 1 / max(kappa.max(), 1e-6)
out.append(f'tightest bend radius {tightest:.1f} cells at ({X[int(np.argmax(kappa))]:.0f}, {Z[int(np.argmax(kappa))]:.0f})')
problems = []
if np.abs(grade[ride]).max() > GRADE + 0.005: problems.append('steeper than the limit: ' + ', '.join(f'{k}' for k in np.where(np.abs(grade) * ride > GRADE + 0.005)[0][:6]))
for name, iF, iL, iD, iDF, yl, land_y in arcs:
    cubes = {(int((X[k] + OX) // 64), int((Z[k] + OZ) // 64)) for k in range(iF, iDF + 1)}
    out.append(f'jump {name}: lip {Y[iL]:.0f}, landing {Y[iD]:.0f}, {ahead(iL, iD):.1f} cells, cubes {sorted(cubes)}')
    if Y[iD] < Y[iL] - 1: problems.append(f'{name}: lands lower than it leaves')
    if len(cubes) > 1: problems.append(f'{name}: not in one cube')
    if any((X[k] % 64 < 1 or X[k] % 64 > 63 or Z[k] % 64 < 1 or Z[k] % 64 > 63) for k in (iL, iD)): problems.append(f'{name}: a lip on a cube edge')
# (where the road comes near itself: above and below by enough for a car and the camera, or far enough apart side by side)
pairs = []
for a in range(0, N, 4):
    d = np.hypot(X - X[a], Z - Z[a]); along_ = np.minimum((np.arange(N) - a) % N, (a - np.arange(N)) % N) * STEP
    for b in np.where((along_ > 40) & (d < halfs + halfs[a] + 1.0))[0]:
        if a < b: pairs.append((a, int(b), d[b], abs(Y[a] - Y[b])))
if pairs:
    least = min(p[3] for p in pairs)
    out.append(f'{len(pairs)} near passes, the least height between them {least:.0f}')
    if least < 2800: problems.append('roads closer than 2,800 over each other: ' + ', '.join(f'({X[p[0]]:.0f},{Z[p[0]]:.0f})' for p in pairs if p[3] < 2800)[:200])
margin = min(X.min() - 0, Z.min() - 0, SIZE_X - X.max(), SIZE_Z - Z.max())
out.append(f'least cells to the island\'s edge {margin:.1f}; x {X.min():.0f}..{X.max():.0f}, z {Z.min():.0f}..{Z.max():.0f}')
if margin < 12: problems.append('too near the island\'s edge')
route = sorted({(int((X[k] + OX) // 64), int((Z[k] + OZ) // 64)) for k in range(N)})
out.append(f'{len(route)} cubes the road runs through of {CUBES_X * CUBES_Z}')
# (a lap's time: the road's bends limit a car, a typical 20 cells/s on the straights)
SPEED_TOP, GRIP = 20.0, 26.0
v = np.minimum(SPEED_TOP, np.sqrt(GRIP / np.maximum(kappa, 1e-4)))
for _ in range(3):
    for k in range(N - 1, -1, -1): v[k] = min(v[k], v[(k + 1) % N] + 0.14)
t = float(np.sum(STEP / v)); out.append(f'about {t:.0f} s a lap by the rough speed model')
legs = [(order[i], order[(i + 1) % len(order)]) for i in range(len(order))]
out.append('  ' + ', '.join(f'{a} +{ahead(M[a], M[b]):.0f}' for a, b in legs))
print('\n'.join(out))
if problems: print('PROBLEMS:\n  ' + '\n  '.join(problems))

# ---- the plan
PIT = 4.5                               # the pit lane on the straight's south side (+Across), the race lanes north of the deck's middle
iFence0, iFence1 = (M['start'] - int(26 / STEP)) % N, (M['start'] + int(22 / STEP)) % N
plan = {
    'originCellX': OX, 'originCellZ': OZ,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in zip(X, Z)],
    'heights': [round(float(y), 1) for y in Y],
    'maxGrade': round(float(np.abs(grade).max()) + 0.05, 3),
    'raised': [0, N - 1], 'raisedHalf': H0, 'asphaltHalf': ASPHALT, 'curbHalf': CURB,
    'raisedHalfs': [round(float(h), 3) for h in halfs],
    'arcJumps': [[int(iF), int(iL), int(iD), int(iDF), 0] for name, iF, iL, iD, iDF, _, _ in arcs],
    'start': int(M['start']),
    'pitStripe': [int(iFence0), int(iFence1), 2.5],
    'pitSpots': [[x, NZ + 4.75, 1.0, 0.0, float(Y[M['start']])] for x in (START[0] - 14, START[0] - 10.5, START[0] - 7)],
    'pitFence': True,
    'gridShift': -PIT / 2,
    'raisedCut': True,
    # the camera rides the road behind the car, as in the palace: on an arm it climbs through the table when the road goes under it, and through the horn's wall
    'railCamera': [11, 1900, 7, 500, 1000],
    # the road's own surfaces: a cranberry runner down the table, the bog's boardwalk and the wharves' planks, the stadium's cinders
    'themes': [{'from': int(a), 'to': int(b), 'theme': t} for a, b, t in sorted(((M['table0'], M['table1'], 'runner'), (M['bog0'] - 30, M['bog1'] + 30, 'boardwalk'), (M['bowl0'], M['bowl1'], 'cinder')))],
}
# ---- what moves, what comes down. Spooks (RACEMOD.CPP spook=): Canada geese in Vs, the pumpkin boats, the parade's balloons; the mushrooms pumpkins, the Temple's
def ring(cx, cz, rx, rz, lag, out=0.0, n=24):
    return [[round(cx + (rx + out) * math.cos(k * math.tau / n + lag), 2), round(cz + (rz + out) * math.sin(k * math.tau / n + lag), 2)] for k in range(n)]
spooks = []
for flock, (cx, cz, rx, rz, y, lag0) in enumerate(((180, 86, 128, 58, 4400.0, 0.0), (214, 216, 120, 56, 4800.0, 2.0))):
    for g in range(7):
        rank = (g + 1) // 2; side = -1 if g % 2 else 1
        spooks.append({'kind': 'goose', 'flags': 2, 'y': y + rank * 60, 'way': ring(cx, cz, rx, rz, lag0 - rank * 0.05, side * rank * 3.0)})
for lane, (x0, x1, z) in enumerate(((332, 372, 93), (372, 332, 96), (336, 368, 98), (368, 336, 92))):
    spooks.append({'kind': 'pboat', 'flags': 0, 'y': 60.0, 'way': [[x0, z], [x1, z]]})
parade = list(range(M['parade0'], M['parade1'], int(14 / STEP)))
paradeWay = [[round(float(X[k]), 2), round(float(Z[k] + 2 * math.sin(k / 30)), 2)] for k in parade]
for i, what in enumerate(('balloonTurkey', 'balloonPumpkin', 'balloonGoose')):
    a = len(paradeWay) * i // 3
    spooks.append({'kind': what, 'flags': 2 if False else 0, 'y': 4400.0 + i * 500, 'way': paradeWay[a:] + paradeWay[:a]})
# Canada geese crossing the road on foot, in threes, each to the far edge and back (flags 1024)
for k in (near(205, NZ, E_), (M['bog0'] + int(46 / STEP)) % N, (M['maple0'] + int(24 / STEP)) % N, (M['fort1'] - int(10 / STEP)) % N):
    for g in range(3):
        j = (k + g * 7) % N; w = float(halfs[j]) + 3.0; way = [[round(float(X[j] + Nn[j, 0] * s * w), 2), round(float(Z[j] + Nn[j, 1] * s * w), 2)] for s in (1, -1)]
        spooks.append({'kind': 'gooseWalk', 'flags': 1024, 'y': float(round(Y[j])), 'way': way if g % 2 else way[::-1]})
plan['halloween'] = {'pumpkins': True, 'spooks': spooks}
# spilt cranberries on the Bog's boardwalk, gravy on the table
spills = [(M['bog0'] + int(24 / STEP)) % N, (M['bog0'] + int(66 / STEP)) % N, near(216, 150, W_), near(150, 168, W_), near(180, 156, W_)]
plan['spills'] = [[round(float(X[k] + Nn[k, 0] * (2.0 if i % 2 else -2.0)), 2), round(float(Z[k] + Nn[k, 1] * (2.0 if i % 2 else -2.0)), 2), float(round(Y[k]))] for i, k in enumerate(spills)]
# Mash Mountain's crater, its gravy going up and raining on the road round it; the stadium's kicks coming down on the road round the field
MASH = [146.0, MASH_Z, TABLE_Y + 4600]
plan['lavaGeysers'] = [{'source': MASH, 'from': int(near(182, 156, W_)), 'to': int(near(120, 165, W_)), 'every': 6200, 'balls': 5}]
kick = [318.0, 250.0, 2600.0]
plan['crystalShards'] = [{'source': kick, 'from': int(M['bowl0']), 'to': int((M['bowl0'] + M['bowl1']) // 2), 'every': 7400, 'balls': 2},
                         {'source': kick, 'from': int((M['bowl0'] + M['bowl1']) // 2), 'to': int(M['bowl1']), 'every': 8200, 'balls': 2}]

# ---- the ground: an array of heights (a vertex each way) and of what each cell is, for HarvestTerrain
KINDS = ['sea', 'maple', 'grass', 'fair', 'bog', 'dike', 'shore', 'dirt', 'rug', 'border', 'stubble', 'corn', 'street', 'turf', 'wharf']
KIND = {k: i for i, k in enumerate(KINDS)}
def smooth_noise(seed, cell):
    r = np.random.RandomState(seed)
    gw, gd = SIZE_X // cell + 3, SIZE_Z // cell + 3
    g = r.rand(gd, gw)
    xs = np.arange(SIZE_X + 1) / cell; zs = np.arange(SIZE_Z + 1) / cell
    x0 = xs.astype(int); z0 = zs.astype(int); fx = xs - x0; fz = zs - z0
    fx = fx * fx * (3 - 2 * fx); fz = fz * fz * (3 - 2 * fz)
    a = g[np.ix_(z0, x0)] * (1 - fx) + g[np.ix_(z0, x0 + 1)] * fx
    b = g[np.ix_(z0 + 1, x0)] * (1 - fx) + g[np.ix_(z0 + 1, x0 + 1)] * fx
    return (a.T * (1 - fz) + b.T * fz).T
n0, n1, n2 = smooth_noise(11, 40), smooth_noise(12, 14), smooth_noise(13, 5)
zz, xx = np.mgrid[0:SIZE_Z + 1, 0:SIZE_X + 1].astype(float)
coast = np.minimum.reduce([xx - 16, SIZE_X - 12 - xx, zz - 16, SIZE_Z - 10 - zz]) + (n0 - 0.5) * 16
G = 720 + (n0 - 0.5) * 320 + (n1 - 0.5) * 160
def blend(a, f): return a * (1 - f[..., None]) if False else None
def ease(f): f = np.clip(f, 0, 1); return f * f * (3 - 2 * f)
G = G * ease((coast + 4) / 14) + (-500) * (1 - ease((coast + 4) / 14))
def sdbox(x0, z0, x1, z1):
    return np.maximum.reduce([x0 - xx, xx - x1, z0 - zz, zz - z1])
kind = np.where(zz < BORDER_Z, KIND['grass'], KIND['stubble'])
# the Maple Run's hills, and its road's own hill round it, the land's lower than the table's
maple = ease((xx - 286) / 30) * ease((156 - zz) / 30) * ease((zz - 112) / 22)
G = G + maple * (500 + 700 * n0)
kind[(zz > 60 + (n1 - 0.5) * 30) & (zz < BORDER_Z - 2 + (n1 - 0.5) * 16)] = KIND['maple']
# the fair round the north straight
fair = sdbox(56, 20, 262, 66)
kind[fair < 0] = KIND['fair']
# the road's ground: the land a little under the deck, where it runs on the land
follow = np.ones(N, bool)
for a, b in [(M['lake0'], M['leapEnd']), (M['cloth0'], M['helix1']), (M['bridgeFoot'], M['bridgeEnd']), (M['bog0'], M['bog1'])]: follow[span(a, b)] = False
under_deck = np.zeros_like(G); weight = np.zeros_like(G)
BLEND = 16
for k in range(0, N, 3):
    if not follow[k]: continue
    r = halfs[k] + 1.5 + BLEND; x0, x1 = int(max(0, X[k] - r)), int(min(SIZE_X, X[k] + r + 1)); z0, z1 = int(max(0, Z[k] - r)), int(min(SIZE_Z, Z[k] + r + 1))
    d = np.hypot(xx[z0:z1, x0:x1] - X[k], zz[z0:z1, x0:x1] - Z[k])
    w = 1 - ease((d - halfs[k] - 1.5) / BLEND)
    better = w > weight[z0:z1, x0:x1]
    weight[z0:z1, x0:x1][better] = w[better]; under_deck[z0:z1, x0:x1][better] = (Y[k] - 180)
G = G * (1 - weight) + under_deck * weight
# the lake of the regatta: open water under the road; its shore
lake = np.hypot((xx - 310) / 50, (zz - 70) / 42) - 1 + (n1 - 0.5) * 0.22
G = np.where(lake < 0.12, np.minimum(G, 620 - 1300 * ease(-lake / 0.12 + 1)), G)
kind[(lake >= -0.02) & (lake < 0.12)] = KIND['shore']
# the bog: flooded beds between dikes, low, the boardwalk over them
bog = sdbox(72, 70, 186, 134) + (n2 - 0.5) * 3
inbog = bog < 0
G = np.where(bog < 12, G * ease(bog / 12) + 430 * (1 - ease(bog / 12)), G)
dikes = inbog & ((np.abs((xx - 72) % 38 - 19) > 17.5) | (np.abs((zz - 70) % 32 - 16) > 14.5) | (bog > -2))
G[dikes] = 520
kind[inbog] = KIND['bog']; kind[dikes] = KIND['dike']
# the fort's yard
fort = sdbox(26, 66, 66, 106); G = np.where(fort < 10, G * ease(fort / 10) + 780 * (1 - ease(fort / 10)), G); kind[fort < 0] = KIND['dirt']
# the rug under the table, and the border's line of stones either side of it
rug = sdbox(TABLE[0] - 12, TABLE[1] - 8, TABLE[2] + 12, TABLE[3] + 8)
G = np.where(rug < 8, G * ease(rug / 8) + 780 * (1 - ease(rug / 8)), G); kind[rug < 0] = KIND['rug']
kind[(np.abs(zz - BORDER_Z) < 1.5) & (rug >= 0)] = KIND['border']
# the corn; the avenue; the stadium; the harbour and its inlet under the drawbridge
kind[sdbox(202, 192, 276, 272) < 0] = KIND['corn']
street = sdbox(70, 268, 288, 312); G = np.where(street < 6, G * ease(street / 6) + 760 * (1 - ease(street / 6)), G); kind[street < 0] = KIND['street']
bowl = np.hypot((xx - 322) / 54, (zz - 250) / 58) - 1
G = np.where(bowl < 0.2, G * ease(bowl / 0.2) + 760 * (1 - ease(bowl / 0.2)), G); kind[bowl < 0.1] = KIND['street']
field = sdbox(296, 222, 340, 278); kind[field < 0] = KIND['turf']
wharf = sdbox(16, 192, 62, 268); G = np.where(wharf < 6, np.minimum(G, 760 * ease(wharf / 6) + 700 * (1 - ease(wharf / 6))), G); kind[wharf < 0] = KIND['wharf']
inlet = np.maximum(np.abs(zz - 230) - 7 + (n2 - 0.5) * 2, xx - 104)
G = np.where(inlet < 6, np.minimum(G, 700 - 1500 * ease((6 - inlet) / 6)), G)
kind[(G < 0)] = KIND['sea']
kind[(G >= 0) & (G < 260)] = KIND['shore']
kindc = kind[:-1, :-1]
heights_i = np.round(G).astype(np.int16)

# ---- the things on the ground, by what they are: [x, z, y, turn (1/4096), scale]
rand = np.random.RandomState(1578)
def gh(x, z):
    i, j = int(np.clip(x, 0, SIZE_X)), int(np.clip(z, 0, SIZE_Z)); return float(G[j, i])
road = np.stack([X, Z], 1)[::4]; roadh = halfs[::4]
def clear_of_road(x, z, m): return (np.hypot(road[:, 0] - x, road[:, 1] - z) - roadh).min() > m
D = {}
def put(what, x, z, y=None, turn=0, scale=1.0):
    D.setdefault(what, []).append([round(float(x), 2), round(float(z), 2), round(float(gh(x, z) if y is None else y), 1), int(turn) & 4095, round(float(scale), 2)])
# the trees: maples in Canada, bare oaks in the States, thinned so a cube stays quick to draw
places = []
for what, allowed, spacing in (('maple', [KIND['maple'], KIND['grass']], 9.0), ('oak', [KIND['stubble']], 15.0)):
    for j in range(16, SIZE_Z - 16, int(spacing)):
        for i in range(16, SIZE_X - 12, int(spacing)):
            x, z = i + rand.rand() * spacing * 0.8, j + rand.rand() * spacing * 0.8
            if kind[int(z), int(x)] not in allowed or not clear_of_road(x, z, 5.0): continue
            if what == 'maple': v = int(rand.randint(3)); name = 'maple%d' % v
            else: name = 'oak'
            put(name, x, z, turn=rand.randint(4096), scale=0.8 + rand.rand() * 0.6)

TOP = TABLE_Y - 70                      # the table's cloth, a little under the road on it
FEET = 700.0                            # where what stands on the rug or the street stands from
# -- the Fall Fair
for x in (72, 98, 124, 176, 202, 228, 254): put('tent' if x % 4 == 0 else 'tent2', x, 26, turn=rand.randint(4096))
for x in (70, 96, 188, 216, 244): put('tent2' if x % 4 == 0 else 'tent', x, 62, turn=rand.randint(4096))
put('ferris', 150, 22)
for x, z in ((86, 32), (112, 62), (186, 34), (214, 32), (232, 62), (118, 28)): put('stall', x, z, turn=(z > 46) * 2048)
for k in range(14):
    x, z = 60 + rand.rand() * 196, 22 + rand.rand() * 46
    if clear_of_road(x, z, 6.0) and all(math.hypot(x - t[0], z - t[1]) > 7 for n in ('tent', 'tent2') for t in D.get(n, [])): put('prize' if k % 3 == 0 else 'bales', x, z, turn=rand.randint(4096))
# -- the regatta: moored boats, the lane's buoys under the leap
for x, z in ((296, 64), (318, 84), (282, 78), (328, 36), (326, 98), (304, 34)): put('boat', x, z, y=-40, turn=rand.randint(4096))
for x in range(330, 372, 7):
    for z in (88, 102): put('buoy', x, z, y=0)
# -- hay and pumpkins beside the Maple Run and the fields
for k in range(70):
    x, z = 20 + rand.rand() * (SIZE_X - 40), 20 + rand.rand() * (SIZE_Z - 40); c = kind[int(z), int(x)]
    if c in (KIND['maple'], KIND['grass'], KIND['stubble']) and clear_of_road(x, z, 4.0) and G[int(z), int(x)] > 300:
        put('hay' if k % 2 == 0 else 'pumpkin', x, z, turn=rand.randint(4096))
# -- the horn of plenty, its mouth on the table, the road through it; what spills out of it
hornTip, hornMouth = near(296, 160, W_), near(262, 160, W_)
put('horn', 279, 160, y=TABLE_Y, scale=TABLE_Y - float(Y[hornTip]))
for x, z, what in ((262, 149, 'pumpkin'), (258, 147, 'cob'), (266, 145, 'apple'), (254, 145, 'squash'), (262, 171, 'squash'), (258, 174, 'cob'), (268, 174, 'apple'), (252, 172, 'apple')):
    put(what, x, z, y=TOP, turn=rand.randint(4096))
# -- the feast, Canada's end first, the road between the dishes
for x, z in ((246, 145), (254, 146), (258, 170), (270, 149), (272, 172)): put('tart', x, z, y=TOP)
put('pie', 244, 171, y=TOP)
put('cran', 228, 170, y=TOP)
put('turkey', 206, 168, y=TOP)
put('fork', 186, 172, y=TOP, turn=1024)
put('knife', 193, 172, y=TOP, turn=3072)
put('gravyboat', 161, 147, y=TOP, turn=300)
put('sweet', 172.5, 146.5, y=TOP, turn=0)
put('mash', 146, MASH_Z, y=TOP)
put('pecan', 124, 150, y=TOP)
for x, z in ((222, 175), (168, 176), (124, 176), (262, 144)): put('candle', x, z, y=TOP)
archAt = near(176, 160, W_, after=M['table0'], window=120)
put('arch', X[archAt], Z[archAt], y=TOP, turn=int(math.atan2(-Nn[archAt, 1], Nn[archAt, 0]) * 2048 / math.pi))
# -- the chairs: the road under two of them, between the legs
for x in (140, 189, 232, 262): put('chair', x, 128, y=FEET)
for x in (150, 200, 250): put('chair', x, 192, y=FEET, turn=2048)
put('mill', HELIX_X, HELIX_Z, y=FEET)
# -- the Habitation
fortx0, fortx1, fortz0, fortz1 = 26, 66, 66, 106
for x in range(fortx0 + 6, fortx1 - 2, 8):
    for z in (fortz0, fortz1):
        if clear_of_road(x, z, 3.5): put('palisade', x, z)
for z in range(fortz0 + 6, fortz1 - 2, 8):
    for x in (fortx0, fortx1): put('palisade', x, z, turn=1024)
for x, z in ((fortx0, fortz0), (fortx1, fortz0), (fortx0, fortz1), (fortx1, fortz1)): put('bastion', x, z)
put('hall', 58, 86, turn=1024)
put('shed', 33, 80, turn=1024); put('shed', 33, 94, turn=1024)
# -- the harbour
put('ship', 36, 230, y=0); put('ship', 7, 208, y=0, turn=1024); put('ship', 8, 258, y=0, turn=1024)
for x, z in ((28, 204), (46, 212), (34, 250), (52, 258), (24, 262), (44, 200), (56, 248)): put('crates', x, z, turn=rand.randint(4096))
for x, z in ((60, 219), (82, 219), (59, 241), (81, 241)): put('tower', x, z)
# -- Parade Avenue
for i, x in enumerate(range(86, 282, 13)): put('bldg%d' % (i * 3 % 4), x, 305)
for i, x in enumerate(range(97, 136, 13)): put('bldg%d' % (i * 5 % 4), x, 262, turn=2048)
put('review', 151, 262)
# -- the Turkey Bowl
for x in range(304, 360, 16): put('stand', x, 198)
for z in range(224, 288, 16): put('stand', 370, z, turn=3072)
for z in (236, 252, 268): put('stand', 283, z, turn=1024)
for x in range(304, 352, 16): put('stand', x, 304, turn=2048)
put('lines', 318, 250, y=gh(318, 250) + 70)
put('goal', 318, 225, turn=0); put('goal', 318, 276)
put('board', 332, 186)
# -- the Corn Maze: walls of stalks down both sides, a second row behind
def in_garden(x, z): return 237 < x < 254 and 212 < z < 252
for k in range(M['maze0'], M['under0'] - int(16 / STEP), 12):
    turn = int(math.atan2(T[k, 0], T[k, 1]) * 2048 / math.pi - 1024) & 4095
    for side in (1, -1):
        for out_ in (halfs[k] + 2.4, halfs[k] + 5.6):
            x, z = X[k] + Nn[k, 0] * side * out_, Z[k] + Nn[k, 1] * side * out_
            # (a piece is 6 cells long: its two ends and its middle clear of the road, on a bend's inside too)
            if in_garden(x, z): continue
            if all(clear_of_road(x + T[k, 0] * d, z + T[k, 1] * d, 2.0) for d in (-3.2, 0, 3.2)): put('corn', x, z, y=gh(x, z), turn=turn)
# (the pocket inside the first hairpin: a Three Sisters garden, its hills in rows)
for z in range(217, 250, 6):
    for x in (240, 245.5, 251):
        if clear_of_road(x, z, 3.0): put('sisters', x, z, turn=rand.randint(4096))
# -- the border, its line of stones, a flag of each country at each end
for x in list(range(20, 104, 12)) + list(range(296, 362, 12)):
    if kind[int(BORDER_Z), x] == KIND['border'] and clear_of_road(x, BORDER_Z, 3.0): put('post', x, BORDER_Z)
for x in (22, 356):
    put('flagCA', x, BORDER_Z - 4); put('flagUS', x, BORDER_Z + 4)

for what, l in sorted(D.items()): print(f'  {what}: {len(l)}', end='')
print()
print(f'horn: tip y {Y[hornTip]:.0f}, mouth y {Y[hornMouth]:.0f}')

if '--write' in sys.argv:
    json.dump(plan, open(OUT + 'harvest_track_plan.json', 'w'))
    head = np.array([SIZE_X, SIZE_Z], np.int32).tobytes()
    open(OUT + 'harvest_ground.bin', 'wb').write(zlib.compress(head + heights_i.tobytes() + kindc.astype(np.uint8).tobytes(), 9))
    json.dump(D, open(OUT + 'harvest_decor.json', 'w'))
    print('written', OUT, 'ground', os.path.getsize(OUT + 'harvest_ground.bin'))

# ---- the map
if '--pictures' in sys.argv:
    from PIL import Image, ImageDraw
    os.makedirs(PICTURES, exist_ok=True)
    Z_ = 3
    COLOURS = {'sea': (58, 92, 128), 'maple': (176, 74, 34), 'grass': (150, 138, 62), 'fair': (204, 176, 96), 'bog': (150, 30, 44), 'dike': (120, 110, 70), 'shore': (150, 140, 110),
               'dirt': (120, 88, 56), 'rug': (150, 60, 48), 'border': (240, 240, 240), 'stubble': (128, 112, 72), 'corn': (196, 170, 72), 'street': (110, 108, 110), 'turf': (70, 130, 70), 'wharf': (110, 84, 60)}
    pal = np.array([COLOURS[k] for k in KINDS], np.uint8)
    shade = np.clip(1 + (np.gradient(G.astype(float), axis=1) + np.gradient(G.astype(float), axis=0)) / 500, 0.5, 1.4)[:-1, :-1, None]
    img = np.clip(pal[kindc] * shade, 0, 255).astype(np.uint8)
    im = Image.fromarray(img).resize((SIZE_X * Z_, SIZE_Z * Z_), Image.NEAREST); dr = ImageDraw.Draw(im)
    def box(x0, z0, x1, z1, c, o=None): dr.rectangle([x0 * Z_, z0 * Z_, x1 * Z_, z1 * Z_], fill=c, outline=o)
    box(TABLE[0], TABLE[1], TABLE[2], TABLE[3], (232, 222, 196), (90, 60, 30))
    for what, l in D.items():
        c = {'m': (220, 60, 30), 'o': (70, 50, 40)}.get(what[0], (255, 255, 0))
        for a in l: dr.ellipse([a[0] * Z_ - 2, a[1] * Z_ - 2, a[0] * Z_ + 2, a[1] * Z_ + 2], fill=c)
    for c in range(1, CUBES_X): dr.line([c * 64 * Z_, 0, c * 64 * Z_, SIZE_Z * Z_], fill=(0, 0, 0))
    for c in range(1, CUBES_Z): dr.line([0, c * 64 * Z_, SIZE_X * Z_, c * 64 * Z_], fill=(0, 0, 0))
    lo, hi = Y.min(), Y.max()
    for k in range(0, N):
        f = (Y[k] - lo) / (hi - lo); c = (255, int(255 * (1 - f)), 0) if not gap[k] else (255, 255, 255)
        r = halfs[k] * Z_
        dr.ellipse([X[k] * Z_ - r, Z[k] * Z_ - r, X[k] * Z_ + r, Z[k] * Z_ + r], fill=(30, 30, 30))
    for k in range(0, N):
        f = (Y[k] - lo) / (hi - lo); c = (255, int(230 * (1 - f)), 40) if not gap[k] else (255, 255, 255)
        dr.ellipse([X[k] * Z_ - 2, Z[k] * Z_ - 2, X[k] * Z_ + 2, Z[k] * Z_ + 2], fill=c)
    for name in order:
        k = M[name]; dr.text((X[k] * Z_ + 6, Z[k] * Z_ + 6), name, fill=(255, 255, 255))
    k = M['start']; dr.rectangle([X[k] * Z_ - 3, (Z[k] - 7) * Z_, X[k] * Z_ + 3, (Z[k] + 7) * Z_], fill=(255, 255, 255))
    im.save(PICTURES + 'map.png'); print('map', PICTURES + 'map.png')
