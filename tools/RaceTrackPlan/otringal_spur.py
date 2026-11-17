# Otringal's spaceport spur (2026-11-17; run with --write): a ramp off the square island's east leg down to the spaceport's floor inside
# the U, west along the old saucer pad's row, where the arrival ship now stands -- the user, 2026-11-17: "We need to also add a small ramp
# onto and off the track from the spaceport area". Added to the plan as a branch with to -1 (RaceTrackBuilder: a spur, its last point on
# the ground). Plan cells are cube (8, 9)'s local cells + 128.
import json, math, sys
P = r'e:/dump/LBAAssembler/docs/racetrack/otringal_track_plan.json'
raw = open(P, encoding='utf-8').read()
p = json.loads(raw)
assert json.dumps(p) == raw.rstrip('\n'), 'the plan does not round-trip'
p.pop('branches', None)

# control points (plan cells): on the east leg's middle (x 171) by its lap point 1052, west at right angles across its west half and down to
# the spaceport's floor, local x 31
ctrl = [(171.0, 162.2), (168.0, 162.2), (165.0, 162.2), (162.0, 162.2), (159.0, 162.2)]

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

# heights: the leg's 3800 while on it and a cell past its west edge (x 167.5), down by a smooth step to the floor (3400 there, a little
# over) two cells before the foot, level after
DECK, GROUND, EDGE = 3800.0, 3410.0, 167.5
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
# the lap's point where it leaves: the nearest of the lap's (the east leg at x 171)
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
