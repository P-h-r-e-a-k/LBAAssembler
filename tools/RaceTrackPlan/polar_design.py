# Polar Island's dream race (2026-10-05), from the user's drawing: Twinsen races FunFrock to Sendell, from the dock to the top of the rocky
# peak. Run in E:\dump\TEMP\ptrack with heights.csv (ScriptRoundTrip islandheights POLAR, LBA2_DIR a folder with the island) and
# columns.csv (ScriptRoundTrip polarcolumns) there; writes docs/racetrack/polar_track_plan.json.
#
# The route is a sprint, not a lap (the plan's `open`): from the start line on the dock north up the arm and the main straight through
# 107 into 109, round a hairpin at its top, back down 107's east side and west along 107's south strip, over a jump across the main
# straight where it meets the arm, on west and into 108 -- three legs of a serpentine up its terraces -- along its north edge, and a
# jump high over the plateau's pillars onto the rocky peak's top, where the finish line is (`finish`).
# Both jumps are carried jumps (the race-track mode carries the car along the plan's heights: RaceTrackPlan.ArcJumps), each on a raised
# stretch of its own (`raised`: two stretches, the ground road between them): the one over the main straight flies from cube (8, 7)
# into cube (7, 7) -- the main straight is 4 cells from the cube's edge there -- and only a carried jump changes cube in mid-air.
import json, csv, sys
import numpy as np

Hgt = {}
for row in csv.DictReader(open('heights.csv')):
    if row['height']: Hgt[(int(row['x']), int(row['z']))] = float(row['height'])


def ground(x, z):
    xi, zi = int(np.floor(x)), int(np.floor(z))
    fx, fz = x - xi, z - zi
    h = [Hgt.get((xi + a, zi + b), 0.0) for b in (0, 1) for a in (0, 1)]
    return (h[0] * (1 - fx) + h[1] * fx) * (1 - fz) + (h[2] * (1 - fx) + h[3] * fx) * fz


SP = 0.5               # cells between points
# control points, island cells, in race order
CTRL = [
    # the dock: the start straight, north
    (507, 556), (507, 548), (507, 538), (507, 526), (507.5, 512), (508, 498), (508, 484), (508, 471),
    # through the junction (the jump flies over here) and north along 107's lake shore
    (508, 458), (509, 446), (510.5, 434), (511.5, 422), (511, 410), (509, 398),
    # into 109 and round the hairpin at its top
    (505, 388), (499, 378), (491, 370), (483, 363), (477, 356), (476, 349), (480, 343.5), (488, 341), (499, 340.5), (508, 342.5),
    (515.5, 348.5), (520, 357), (522.5, 367), (524, 378),
    # down 107's east side
    (526.5, 390), (530, 402), (533, 414), (535, 426), (535.5, 437), (534, 446), (530.5, 452.5),
    # west along the south strip: the run-up, the jump over the main straight, the landing
    (524, 456), (516, 456), (508, 456), (500, 456), (492, 456), (484, 456), (476, 456),
    # into 108: the serpentine's first leg north, a hairpin, the second leg south, a hairpin, the third leg north
    (468, 455.5), (461, 453.5), (456.5, 448), (455, 440), (455, 432), (453.5, 425), (448, 420.5), (441.5, 422), (439, 428), (439, 434),
    (439.5, 440), (436, 446.5), (430, 448), (424.5, 445), (423, 439), (423, 431), (423.5, 423), (425.5, 416), (430, 410.5),
    # along 108's north edge to the carried jump onto the peak
    (437, 407.5), (443, 407), (449, 407), (455, 407), (461, 407), (467, 407), (473, 407), (477, 407), (480, 407), (482, 407), (484, 407),
]

# the jump over the main straight (along z 456, the straight at x 508, its road 5 cells either side): the ramp's foot and lip, the
# landing lip and the landing hill's foot, and the flight's top (over the straight: a car on it is about 600 high)
CROSS = [(521, 456), (515, 456), (501, 456), (498.5, 456)]
CROSS_APEX = 2700
# the jump onto the peak: the same four, the finish line; the pad's height (the peak's top layer, 46); the flight's top, over the
# plateau's pillars (up to layer 54: 13 824)
ARC = [(446, 407), (452, 407), (477, 407), (479.5, 407)]
FINISH = (481.5, 407)
PAD = 46 * 256 + 64
APEX = 15200
START = (507, 546)
MAXGRADE = 0.14
RAMP_UP = 700


def catmull(pts, sp):
    P = np.array(pts, float)
    P = np.vstack([P[0] * 2 - P[1], P, P[-1] * 2 - P[-2]])
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        seg = np.linalg.norm(p2 - p1)
        n = max(2, int(np.ceil(seg / sp * 4)))
        for k in range(n):
            t = k / n
            out.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(P[-2])
    out = np.array(out)
    # resampled every sp along the curve
    d = np.r_[0, np.cumsum(np.linalg.norm(np.diff(out, axis=0), axis=1))]
    s = np.arange(0, d[-1], sp)
    return np.stack([np.interp(s, d, out[:, 0]), np.interp(s, d, out[:, 1])], 1), s


pts, S = catmull(CTRL, SP)
n = len(pts)


def nearest(p, lo=0):
    return lo + int(np.argmin(np.hypot(pts[lo:, 0] - p[0], pts[lo:, 1] - p[1])))


# (the return leg's points: after the hairpin at 109's top, so the main straight's own points under the jump are never taken)
leg = nearest((530.5, 452.5))
cross = [nearest(p, leg) for p in CROSS]
arc = [nearest(p, cross[-1]) for p in ARC]
fin, start = nearest(FINISH, arc[-1]), nearest(START)
print('points', n, 'length %.0f cells' % S[-1], 'start', start, 'jump over the straight', cross, 'jump onto the peak', arc, 'finish', fin)

# heights: the ground under the line (the highest of a small cross round each point: the line runs over terraces' edges), smoothed
raw = np.array([max(ground(x + dx, z + dz) for dx, dz in ((0, 0), (1.5, 0), (-1.5, 0), (0, 1.5), (0, -1.5))) for x, z in pts])
raw = np.maximum(raw, 600)                 # (over the sea: a causeway at least this high)


def smooth(v, cells):
    w = int(cells / SP)
    k = np.exp(-0.5 * (np.arange(-3 * w, 3 * w + 1) / w) ** 2); k /= k.sum()
    pad = np.r_[np.full(3 * w, v[0]), v, np.full(3 * w, v[-1])]
    return np.convolve(pad, k, 'valid')


h = smooth(raw, 6)


# A carried jump's way: from the ramp's foot (on the road as it is there) up RAMP_UP, curving, to the lip; a flight to the landing lip,
# `after` + 250, its top `apex`; a hill curving down onto `after` at the landing hill's foot. Returns the bump the flight was given.
def carried(foot, lip, land, landFoot, after, apex):
    base = h[foot]
    for i in range(foot, lip + 1):
        t = (i - foot) / max(1, lip - foot); h[i] = base + RAMP_UP * t * t
    L = base + RAMP_UP
    for i in range(lip, land + 1):
        t = (i - lip) / max(1, land - lip); h[i] = L + (after + 250 - L) * t
    ts = np.linspace(0, 1, 201)
    bump = next(b for b in np.arange(0, 20000, 50) if (L + (after + 250 - L) * ts + b * 4 * ts * (1 - ts)).max() >= apex)
    for i in range(lip, land + 1):
        t = (i - lip) / max(1, land - lip); h[i] += bump * 4 * t * (1 - t)
    for i in range(land, landFoot + 1):
        t = (i - land) / max(1, landFoot - land); h[i] = after + 250 * (1 - t) ** 2
    return bump


# the jump over the straight: from the road's own height to the road's own height beyond it
crossBump = carried(*cross, h[cross[3]], CROSS_APEX)
# the jump onto the peak: onto the pad, and the pad on to the end
arcBump = carried(*arc, PAD, APEX)
h[arc[3]:] = PAD
# grade limits outside the jumps (the road climbs 108's terraces as a ramp, not a cliff): each point no further from its neighbour than
# the grade allows, sweeping both ways; the jumps' own stretches held
held = np.zeros(n, bool); held[cross[0]:cross[3] + 1] = True; held[arc[0]:] = True
step = MAXGRADE * SP * 512
for _ in range(4):
    for i in range(1, n):
        if not held[i]: h[i] = np.clip(h[i], h[i - 1] - step, h[i - 1] + step)
    for i in range(n - 2, -1, -1):
        if not held[i]: h[i] = np.clip(h[i], h[i + 1] - step, h[i + 1] + step)
h2 = smooth(h, 2)
h = np.where(held, h, h2)

# checks: grades, how close the route comes to itself, the lowest over the ground
grade = np.abs(np.diff(h)) / (SP * 512)
print('steepest grade outside the jumps %.3f' % max(g for i, g in enumerate(grade) if not held[i] and not held[i + 1]))
mind = 1e9; where = None
flight = set(range(cross[1], cross[2] + 1))
for i in range(0, n, 2):
    d = np.hypot(pts[:, 0] - pts[i, 0], pts[:, 1] - pts[i, 1])
    far = np.abs(S - S[i]) > 30
    j = np.argmin(np.where(far, d, 1e9))
    if d[j] < mind and i not in flight and j not in flight:
        mind, where = d[j], (i, j)
print('closest approach of two parts of the route (outside the flight over the straight) %.1f cells at %s and %s'
      % (mind, tuple(np.round(pts[where[0]], 1)), tuple(np.round(pts[where[1]], 1))))
under = [i for i in range(cross[0]) if np.hypot(pts[i, 0] - 508, pts[i, 1] - 456) < 1]
print('the flight over the straight: %.0f at its top, the straight under it at %.0f' % (h[cross[1]:cross[2] + 1].max(), h[under[0]] if under else -1))
diff = h - np.array([ground(x, z) for x, z in pts])
print('road over the ground: from %.0f to %.0f (outside the jumps)' % (diff[~held].min(), diff[~held].max()))

plan = {
    'originCellX': 0, 'originCellZ': 0,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in pts],
    'heights': [round(float(v), 1) for v in h],
    'open': True, 'start': start, 'finish': fin,
    'maxGrade': 1.8,
    'asphaltHalf': 3.0, 'curbHalf': 3.75, 'vergeHalf': 5.0, 'blend': 4.0,
    'raised': [cross[0], cross[3], arc[0], n - 1], 'raisedHalf': 3.25,
    'arcJumps': [cross, arc],
}
out = sys.argv[1] if len(sys.argv) > 1 else r'E:\dump\LBAAssembler\docs\racetrack\polar_track_plan.json'
json.dump(plan, open(out, 'w'))
print('wrote', out, 'flight bumps', crossBump, arcBump)
