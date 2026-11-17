# The Emerald Moon's spur (2026-11-17; run after emerald_design.py, with --write): a ramp off the pit lane's west edge down to the landing pad, where the user drew it (lime,
# just west of the pit lane by the start line's bridge, down to the two round pads at the lower left). Added to the plan as a branch with
# to -1 (RaceTrackBuilder: a spur, its last point on the ground).
import json, math, sys
P = r'e:/dump/LBAAssembler/docs/racetrack/emerald_track_plan.json'
raw = open(P, encoding='utf-8').read()
p = json.loads(raw)
assert json.dumps(p) == raw.rstrip('\n'), 'the plan does not round-trip'
p.pop('branches', None)

# control points (plan cells): on the pit strip (x 45.5-53.5 there), west off it at right angles, bending south-west, then south to the
# foot north of the saucer's platform (its north edge z 92)
ctrl = [(49.0, 71.0), (46.5, 71.0), (44.0, 71.2), (41.2, 72.6), (39.0, 75.5), (37.6, 79.5), (36.9, 83.5), (36.6, 87.0), (36.5, 89.0)]

def catmull(pts, steps=40):
    out = []
    ext = [pts[0]] + pts + [pts[-1]]
    for i in range(1, len(ext) - 2):
        p0, p1, p2, p3 = ext[i - 1], ext[i], ext[i + 1], ext[i + 2]
        for k in range(steps):
            t = k / steps
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[j]) + (-p0[j] + p2[j]) * t + (2 * p0[j] - 5 * p1[j] + 4 * p2[j] - p3[j]) * t2 + (-p0[j] + 3 * p1[j] - 3 * p2[j] + p3[j]) * t3) for j in range(2)))
    out.append(pts[-1])
    return out

dense = catmull(ctrl)
# resample every half cell
pts = [dense[0]]
acc = 0.0
for a, b in zip(dense, dense[1:]):
    d = math.dist(a, b)
    while acc + d >= 0.5:
        f = (0.5 - acc) / d
        a = (a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f)
        d = math.dist(a, b)
        pts.append(a)
        acc = 0.0
    acc += d
if math.dist(pts[-1], ctrl[-1]) > 0.2:
    pts.append(ctrl[-1])
n = len(pts)

# heights: the pit lane's 5000 while on the strip and a cell past its edge (x 45.5), down by a smooth step to the ground (325-390 there:
# its 390 plus a little) two cells before the foot, level after
DECK, GROUND, EDGE = 5000.0, 400.0, 45.5
s = [0.0]
for a, b in zip(pts, pts[1:]):
    s.append(s[-1] + math.dist(a, b))
leave = next(i for i, q in enumerate(pts) if q[0] < EDGE - 1.0)
flat = n - 1 - 4
heights = []
for i in range(n):
    if i <= leave:
        heights.append(DECK - 8)
    elif i >= flat:
        heights.append(GROUND)
    else:
        # (a straight slope with eased ends, BLEND cells each: its steepest as little over the straight one as can be)
        L, BLEND = s[flat] - s[leave], 2.5
        u = s[i] - s[leave]
        g = 1.0 / (L - BLEND)          # the straight part's slope, per cell, of the whole drop
        if u < BLEND:
            e = g * u * u / (2 * BLEND)
        elif u > L - BLEND:
            v = L - u
            e = 1.0 - g * v * v / (2 * BLEND)
        else:
            e = g * (u - BLEND / 2)
        heights.append(round(DECK + (GROUND - DECK) * e, 1))
# (its own deck from two cells in on the strip, 8 under its surface there: the strip drawn over it, no gap where they meet -- the user,
# 2026-11-17, "join the slope neatly to our track without the massive gap")
hidden_last = max(i for i, q in enumerate(pts) if q[0] > EDGE + 2.0)
grades = [abs(heights[i + 1] - heights[i]) / (512 * (s[i + 1] - s[i])) for i in range(n - 1)]
print(f'{n} points, {s[-1]:.1f} cells; leaves the strip at {leave} ({pts[leave][0]:.2f}, {pts[leave][1]:.2f}); hidden 0..{hidden_last}; flat from {flat}; steepest grade {max(grades):.2f}')
# the lap's point where it leaves: the nearest of the lap's (the straight at x 60.5)
lap = p['points']
frm = min(range(len(lap)), key=lambda k: math.dist(lap[k], pts[0]))
print('from lap point', frm, lap[frm])
p['branches'] = [{
    'points': [[round(x, 3), round(z, 3)] for x, z in pts],
    'heights': heights,
    'from': frm,
    'to': -1,
    'hidden': [[0, hidden_last]],
}]
if '--write' in sys.argv:
    open(P, 'w', encoding='utf-8', newline='').write(json.dumps(p))
    print('written')
