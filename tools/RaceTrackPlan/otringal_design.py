# Otringal (OTRINGAL.ILE, the game's island 4, cells x 384..576, z 448..640) from the user's sketch (2026-10-08: "Green is the track,
# pink are jump points and the red is intended to be a tunnel to drive through and the orange line represents a pit lane"). The sketch is
# the editor's minimap turned a quarter clockwise and stretched along the island (sketch x = 1141 - 1.47 * render y, sketch y = render x
# - 23, the render 4 pixels a cell from cell (384, 448)): E:\dump\TEMP\otr\sketchfit.py, centreline.py. One raised road, round the lap:
#
#  - the square island (the walled yard south-east of the town, scene 92): east along its south side, north along its east side, west
#    along its north side past the spaceship's pad, down the game's own slope into the building at its foot, through the bridge to the
#    town -- the game's tunnel, the bridge's tube cut open for the road -- and on under the gatehouse, climbing, up into the town;
#  - north-west through the lower town, north up through the upper town onto the straight along the palace's east side (the start line,
#    the pit lane on its left, between the race lanes and the palace);
#  - west along the island's north edge, south down its west side and a jump over the inlet, an S round the town's tanks, and down past
#    the west pier over the sea to the islets (a scene of their own: cube (6,9) has ground but no scene in the game -- RaceTrackIsland
#    .NewCubes);
#  - east along the south coast and a jump over the sea back onto the square island.
#
# Version 2 (the user, 2026-10-08: "re-do the Otringal track so it's closer to ground level ... we're driving down the in-game slope,
# through a short tunnel then up back onto our track"): the deck follows the ground (CLEAR over the ground under it, no steeper than
# GRADE), over the sea and gullies as a bridge, and through the bridge at the game's own tunnel's floor.
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
    xi, zi = min(SIZE - 2, max(0, int(x))), min(SIZE - 2, max(0, int(z))); fx, fz = x - xi, z - zi
    return H[zi, xi] * (1 - fx) * (1 - fz) + H[zi, xi + 1] * fx * (1 - fz) + H[zi + 1, xi] * (1 - fx) * fz + H[zi + 1, xi + 1] * fx * fz

STEP = 0.5
H0 = 4.0                                # the deck's half width to its rail
ASPHALT, CURB = H0 - 1.0, H0 - 0.25
TUBE = 3.6                              # ... and through the game's tunnel: the bridge's walls are 5.2 cells either side of its middle
PIT = 4.5                               # the pit lane beside the straight, on its left (west), past a fence
SX = 499.0                              # the deck's middle along the straight: its left edge (the pit lane's) clear of the palace's porch
                                        # (x 491.5: its canopy and columns east of the palace, 23 24 30)
RX = SX + PIT / 2                       # ... the race lanes' middle; their left edge clear of the stair tower at the plateau's edge (69, x 491.1)
TZ = 594.45                             # the bridge's, its doorway's and the gatehouse's middle (z)

def C(x, z): return (x - OX, z - OZ)

# ---- the roulette wheel by the casino (the user, 2026-10-09: "The jump approaching the casino the landing is too tight, let's change this
# area, let's have the jump land into a giant rotating roulette wheel complete with colours, numbers, and a giant white ball that rolls
# around for cars to avoid. Cars land in the roulette wheel from the jump, and drive around in a circle and out a hole in the bottom"). A
# bowl on the town's square north of the casino, over the inlet's gully, its floor a cone down to a hole in its middle; the jump over the
# inlet lands on its west side heading south, and the lane runs round it most of a turn (south, east, north-north-east), dives through a
# half turn into the hole and the car drops through it (a carried jump) onto the road under the wheel, which runs on east-south-east
# out from under it to the corner past the casino. (A first try, the dive a half turn through the wheel's very middle, 2.75 cells round
# and its lane 1.6 cells either side: the test pilot stuck fast where it began -- the engine keeps a car's middle 640 inside the rails.) The lane is the raised road's (its rails the engine's), banked as the cone is; the wheel
# itself is RaceTrackRoulette's (the plan's 'roulette').
RW_HALF = 2.0                           # the lane's half width in the wheel
RW_R0 = 5.8                             # its middle's distance from the wheel's middle round the bowl
RW_RIM = RW_R0 + RW_HALF + 0.15         # the bowl's wall (its inner face)
RW_HOLE = 2.8                           # the hole in the bottom
RW_PASS = 1.5                           # the dive's half turn passes this far from the wheel's middle, over the hole
RW_LIP = 3.1                            # where the lane ends, its middle this far from the wheel's middle (the drop's lip) ...
RW_LIP_HALF = 1.0                       # ... and its half width there
RW_YH, RW_S = 9400.0, 170.0             # the bowl's floor at the hole's edge, and how much it rises a cell outwards
RW_DIVE = -70.0                         # where the lane leaves the round for the dive (degrees round from east, towards south)
RW_OUT = 4.5                            # the road under the wheel: straight on from its middle this far, then on to the corner past the casino
RW_LAND = 5.5                           # the drop lands on it this far from the lip (cells along the way)
RW_WALL = 250.0                         # the bowl's wall over its floor at the rim
RW_JUMP_FLOOR = 9750.0                  # the deck at the foot of the jump into it
WEST_X = 454.8                          # the road down the west side (6.5 cells off the cubes west of it, which the island hasn't got)
RWX, RWZ = WEST_X + RW_R0, 520.0         # the wheel's middle: the jump lands heading south on its west side (its wall just short of the casino)
def cone(r): return RW_YH + RW_S * (r - RW_HOLE)

def wheel_path(step=0.02):
    """the lane's way round the wheel (island cells) from where the jump lands to the end of the straight past its middle, and the way it heads there"""
    pts = []
    a0, a1 = math.radians(180.0), math.radians(RW_DIVE)
    n = int(round(RW_R0 * (a0 - a1) / step))
    for k in range(n):
        a = a0 - (a0 - a1) * k / n; pts.append((RWX + RW_R0 * math.cos(a), RWZ + RW_R0 * math.sin(a)))
    # (the dive: a half turn inside the lane's round, to RW_PASS past the wheel's middle -- over the hole)
    rho = (RW_R0 + RW_PASS) / 2; mx, mz = RWX + (RW_R0 - rho) * math.cos(a1), RWZ + (RW_R0 - rho) * math.sin(a1)
    n = int(round(rho * math.pi / step))
    for k in range(n):
        a = a1 - math.pi * k / n; pts.append((mx + rho * math.cos(a), mz + rho * math.sin(a)))
    ex, ez = mx + rho * math.cos(a1 - math.pi), mz + rho * math.sin(a1 - math.pi)
    hx, hz = math.sin(a1 - math.pi), -math.cos(a1 - math.pi)
    n = int(round(RW_OUT / step))
    for k in range(n): pts.append((ex + hx * RW_OUT * k / n, ez + hz * RW_OUT * k / n))
    return pts, (hx, hz), (ex, ez)
WHEEL, (RW_HX, RW_HZ), (RW_EX, RW_EZ) = wheel_path()
WIN = (RWX - RW_R0, RWZ)                # where the lane in the wheel starts (the jump's landing)
POUT = (RW_EX + RW_HX * RW_OUT, RW_EZ + RW_HZ * RW_OUT)

# the corners in the lap's order, each (x, z) and the radius it is rounded with (island cells)
K = [((555.0, 620.0), 5.0),             # the square island: east along its south side, round north ...
     ((555.0, TZ), 5.0),                # ... west along its north side, down the slope, through the bridge and under the gatehouse ...
     ((477.0, TZ), 8.0),                # ... into the town, north-west ...
     ((470.0, 573.0), 6.0),             # ... north up through it ...
     ((SX, 524.0), 10.0),               # ... north: the straight along the palace's east side (the start, the pit lane on the left) ...
     ((SX, 453.0), 7.0),                # ... west along the north edge ...
     ((WEST_X, 455.0), 6.0),            # ... south down the west side, the jump over the inlet into the roulette wheel ...
     (WIN, 0.0),                        # ... (the wheel's lane, wheel_path, spliced in between these two) ...
     (POUT, 0.0),                       # ... out from under it east-south-east, past the casino (its building from z 528.8) ...
     ((470.0, POUT[1] + (470.0 - POUT[0]) * RW_HZ / RW_HX), 4.0),   # ... south ...
     ((470.0, 541.0), 4.0),             # ... west ...
     ((443.0, 541.0), 4.0),             # ... north-west, past the west pier ...
     ((437.0, 532.0), 4.0),
     ((420.8, 533.0), 4.0),             # ... south through the pier's yard, west of its warehouse (51, x 427.4) ...
     ((420.3, 553.0), 4.0),             # ... west, north of the little house at its corner ...
     ((408.0, 557.0), 4.0),             # ... south over the sea to the islets ...
     ((410.5, 590.0), 6.0),             # ... east along their north shore, north of the rock and the trees on them (105, 18) ...
     ((433.0, 590.0), 5.0),             # ... south-east over the sea ...
     ((446.0, 609.0), 5.0),             # ... east along the south coast ...
     ((459.0, 614.0), 4.0),
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
# (the wheel's lane in place of the straight between its two sharp corners)
def at_corner(p):
    d = np.hypot(fine[:, 0] - C(*p)[0], fine[:, 1] - C(*p)[1]); i = int(np.argmax(d < 1e-6))
    if d[i] >= 1e-6: sys.exit(f'no corner at {p}')
    return i
iW_, iP_ = at_corner(WIN), at_corner(POUT)
fine = np.vstack([fine[:iW_], np.array([C(x, z) for x, z in WHEEL]), fine[iP_:]])
fine = fine[np.concatenate([[True], np.hypot(*np.diff(fine, axis=0).T) > 1e-9])]
START =(SX, 488.0)                     # the start line on the straight (heading north: its grid behind it, south, on the level)
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
def span_of(a, b):
    k = a
    while True:
        yield k
        if k == b: break
        k = (k + 1) % N
E, W_, S_, N_ = (1, 0), (-1, 0), (0, 1), (0, -1)

# ---- widths: the usual; narrower through the game's tunnel (from the top of the slope to past the gatehouse); the straight with its
# pit lane on its left (west: -Nn heading north)
def ramp(a, b, k): return ease(ahead(a, k) / ahead(a, b))
HALF = np.full(N, H0)
iTube0, iTube1 = nearest((544.0, TZ), W_), nearest((540.0, TZ), W_)
iTube2, iTube3 = nearest((489.0, TZ), W_), nearest((485.0, TZ), W_)
for k in range(N):
    if between(k, iTube0, iTube1): HALF[k] = H0 + (TUBE - H0) * ramp(iTube0, iTube1, k)
    elif between(k, iTube1, iTube2): HALF[k] = TUBE
    elif between(k, iTube2, iTube3): HALF[k] = TUBE + (H0 - TUBE) * ramp(iTube2, iTube3, k)
PIT_HALF = H0 + PIT / 2
iPit0 = nearest((SX, 511.0), N_); iPit1 = nearest((SX, 504.0), N_); iPit2 = nearest((SX, 470.0), N_); iPit3 = nearest((SX, 463.0), N_)
for k in range(N):
    if between(k, iPit0, iPit1): HALF[k] = max(HALF[k], H0 + (PIT_HALF - H0) * ramp(iPit0, iPit1, k))
    elif between(k, iPit1, iPit2): HALF[k] = PIT_HALF
    elif between(k, iPit2, iPit3): HALF[k] = max(HALF[k], PIT_HALF + (H0 - PIT_HALF) * ramp(iPit2, iPit3, k))
# (the wheel's lane: from where the jump lands round to its lip over the hole, where its middle comes within RW_LIP of the wheel's middle)
def wheel_r(k): return math.hypot(X[k] + OX - RWX, Z[k] + OZ - RWZ)
iWin = nearest(WIN, S_)
iRWLip = iWin
while wheel_r(iRWLip) > RW_LIP: iRWLip = (iRWLip + 1) % N
iRWDive = (iWin + int(round(RW_R0 * math.radians(180.0 - RW_DIVE) / STEP))) % N
for k in span_of(iWin, iRWLip): HALF[k] = RW_HALF
# (narrowing over the last two cells into the hole, which the car is carried over -- the drop's ramp: its rails clear of the hole, and the
# race-track mode sets the car down the middle of the lip)
for j, k in enumerate(span_of((iRWLip - 4) % N, iRWLip)): HALF[k] = RW_HALF + (RW_LIP_HALF - RW_HALF) * ease(j / 4)
iRWLand = iRWLip
while ahead(iRWLip, iRWLand) < RW_LAND: iRWLand = (iRWLand + 1) % N
iRWFoot = (iRWLand + 4) % N
# (the lane and the drop out of the wheel: their heights its own -- the ground's under them neither raises nor smooths the road beside them)
WHEELED = np.zeros(N, bool)
for k in span_of(iWin, iRWLand): WHEELED[k] = True
PIT_WAIT = (496.0, 500.0, 504.0)        # where the opponents wait in the pit lane while the player qualifies (z: behind the line, cube (7,7))

# ---- heights. Floors: CLEAR over the ground under the deck (its 80th percentile across it: a rock at one rail is cut down, RaisedCut),
# at least SEA over the sea, over the decors the road passes over (RIDE: their tops); ceilings: the game's tunnel -- the bridge's floor,
# and under the gatehouse its base less a car's room. Then both brought to a grade: floors spread out under GRADE each way (the deck
# rises before a rise in the ground), ceilings too, and the deck the lower of the two (where a ceiling wins, the ground is cut: a
# cutting). Steeper (STEEP) from the top of the slope down into the tunnel and from its end up into the upper town.
CLEAR, SEA = 400.0, 900.0
PCT = 50                                # the ground under the deck: its median across it (a rock at one side is cut away)
GRADE, STEEP = 0.17, 0.30
TUBE_DECK = 1000.0                      # the bridge's tube is 0..2666 high at its middle, its walls' tops 1866: the deck through it
GATE_BASE, CAR_ROOM = 3900.0, 1250.0    # the gatehouse stands at 3900 over the climb out of the tunnel; the bridge's roof CAR_ROOM over its deck
SLAB, UNDER_SLAB = 320.0, 1150.0        # (the tunnel's roof slab under the gatehouse, level with its base: the deck this far under it -- the cars are 800 high)
RIDE = {99: 450.0, 100: 450.0}          # (the statue at the foot of the palace's cliff, under the straight: the deck over it)
gs = np.array([np.percentile([ground(X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o) for o in np.linspace(-HALF[k], HALF[k], 9)], PCT) for k in range(N)])
floor = np.where(gs < 100, SEA, gs + CLEAR)
for k in range(N):
    for o in np.linspace(-HALF[k], HALF[k], 9):
        i, j = int((Z[k] + Nn[k, 1] * o) * RQ), int((X[k] + Nn[k, 0] * o) * RQ)
        if 0 <= i < TOP.shape[0] and 0 <= j < TOP.shape[1] and TOP[i, j] >= 0 and int(WHO[i, j]) in RIDE:
            floor[k] = max(floor[k], TOP[i, j] + RIDE[int(WHO[i, j])])
ceil = np.full(N, 1e9)
iDoor = nearest((528.0, TZ), W_)        # the doorway at the slope's foot (body 144, x 525..527.9)
iTubeW = nearest((501.8, TZ), W_)       # the bridge's west end
iGateW = nearest((490.8, TZ), W_)       # the gatehouse's west face (body 112, x 490.8..497)
for k in span_of(iDoor, iTubeW): floor[k] = ceil[k] = TUBE_DECK
# (the jump over the inlet into the roulette wheel: its take-off high enough for the flight to clear the wheel's wall)
JUMP_FOOT, JUMP_LIP, JUMP_DEG = 503.0, 509.5, 24.0
for k in span_of(nearest((WEST_X, JUMP_FOOT - 6.0), S_), nearest((WEST_X, JUMP_FOOT), S_)): floor[k] = max(floor[k], RW_JUMP_FLOOR)
for k in span_of(iTubeW, iGateW): ceil[k] = GATE_BASE - SLAB - UNDER_SLAB
iSlope0 = nearest((556.0, 606.0), N_)   # the steep stretches: down to the doorway, and up from the bridge into the upper town
iClimb1 = nearest((471.0, 570.0), N_)
steep = np.zeros(N, bool)
for k in span_of(iSlope0, iDoor): steep[k] = True
for k in span_of(iTubeW, iClimb1): steep[k] = True
g = np.where(steep, STEEP, GRADE) * STEP * 512      # each segment's (k to k + 1) most change
def spread(v, up):
    v = v.copy()
    for _ in range(3):
        for k in range(N):                # forwards
            j = (k + 1) % N
            v[j] = max(v[j], v[k] - g[k]) if up else min(v[j], v[k] + g[k])
        for k in range(N - 1, -1, -1):    # backwards
            j = (k + 1) % N
            v[k] = max(v[k], v[j] - g[k]) if up else min(v[k], v[j] + g[k])
    return v
floor[WHEELED] = -1e9
F = spread(floor, True); Cc = spread(ceil, False)
Y = np.minimum(F, Cc)
# (eased: the kinks of the spread rounded off, a few cells each way -- not near the game's tunnel, whose floor and roofs are as they are)
hard = ceil < 1e8
Ys = Y.copy()
for k in range(N):
    w = [(k + d) % N for d in range(-6, 7)]
    w = [j for j in w if not WHEELED[j]]
    if not hard[w].any() and not WHEELED[k]: Ys[k] = np.mean(Y[w])
Y = np.minimum(np.maximum(Ys, np.minimum(F, Cc) - 60), Cc)
# the wheel's lane on its cone; the drop through the hole: on down the lane's slope at the lip and falling, onto the road under the wheel
for k in span_of(iWin, iRWLip): Y[k] = cone(wheel_r(k))
RW_G0 = max(0.0, (Y[(iRWLip - 1) % N] - Y[iRWLip]) / (STEP * 512)); RW_DL = ahead(iRWLip, iRWLand) * 512
RW_B = (Y[iRWLip] - Y[iRWLand] - RW_G0 * RW_DL) / RW_DL ** 2
if RW_B <= 0: sys.exit(f'the drop through the hole: the road under the wheel ({Y[iRWLand]:.0f}) too high for it')
for k in span_of(iRWLip, iRWLand):
    d = ahead(iRWLip, k) * 512; Y[k] = Y[iRWLip] - RW_G0 * d - RW_B * d * d

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
def pt(p): return p if isinstance(p, (int, np.integer)) else nearest(p[:2], p[2])
JUMPS = [('inlet', (WEST_X, JUMP_FOOT, S_), (WEST_X, JUMP_LIP, S_), iWin, (iWin + 5) % N, JUMP_DEG, cone(RW_R0)),
         ('sea', (497, 620.0, E), (505.0, 620.0, E), (525.0, 620.0, E), (531.0, 620.0, E), 24.0, None)]
gap = np.zeros(N, bool); arcs = []
for name, f, l, ld, lf, deg, landing in JUMPS:
    iF, iL, iD, iDF = pt(f), pt(l), pt(ld), pt(lf)
    at, yl, land_y = flight_heights(iF, iL, iD, iDF, math.radians(deg), Y[iDF] + 600, landing)
    for k in span_of(iF, iDF): Y[k] = at(ahead(iF, k) * 512)
    k = (iL + 1) % N
    while k != iD: gap[k] = True; k = (k + 1) % N
    arcs.append((name, iF, iL, iD, iDF))
# (the drop through the wheel's hole: carried from two cells before its lip to two past where it lands)
k = (iRWLip + 1) % N
while k != iRWLand: gap[k] = True; k = (k + 1) % N
arcs.append(('hole', (iRWLip - 4) % N, iRWLip, iRWLand, iRWFoot))
# the lane banked as the cone is: across it (Nn), the cone's rise outwards
BANK = np.zeros(N)
for k in span_of(iWin, iRWLip):
    rx, rz = X[k] + OX - RWX, Z[k] + OZ - RWZ
    BANK[k] = RW_S / 512 * (rx * Nn[k, 0] + rz * Nn[k, 1]) / math.hypot(rx, rz)
# where the white ball rolls: the lane round the bowl, clear of the landing and the dive
iBall0, iBall1 = (iWin + 8) % N, (iRWDive - 6) % N

# ---- the game's tunnel: the bridge's tube and the building at the slope's foot cut open for the road (RaceTrackDriveThrough, their roofs
# kept: a passage CAR_ROOM high), a tunnel's pieces through the rock between the doorway and the bridge, and under the gatehouse a sunk
# tunnel (the ground cut down between its walls, its roof level with the town's ground) climbing to its west face; the camera kept under
# the roofs all through. The gatehouse stays where it is (StayPut: the cutting past its west face reaches the ground under its origin).
iRock0 = nearest((525.0, TZ), W_); iRock1 = nearest((522.0, TZ), W_)
TUNNELS = [
    {'from': int(iDoor), 'to': int(iGateW), 'roof': 1600.0, 'pieces': False},
    {'from': int(iRock0), 'to': int(iRock1), 'roof': 1600.0, 'sunk': True},
    {'from': int(iTubeW), 'to': int(iGateW), 'roof': 2400.0, 'sunk': True, 'top': GATE_BASE, 'eave': 1.0},
]
DRIVE_THROUGH = [{'body': b, 'scale': 1.0, 'clearance': CAR_ROOM, 'half': TUBE} for b in (144, 143, 111)]
KEEP = [9, 10, 11, 12, 13, 14, 25, 26, 69, 70, 71, 77, 99, 100, 112]     # the palace, the stair towers, the town's big buildings, the gatehouse

grade = (np.roll(Y, -1) - Y) / (STEP * 512)
ride = ~np.zeros(N, bool)
for name, iF, iL, iD, iDF in arcs:
    for k in range(N):
        if between(k, iF, iDF): ride[k] = False

# ---- checks
MARK = {
    'start':   nearest(START, N_), 'north1': nearest((SX, 466), N_), 'nw': nearest((470, 453), W_), 'west0': nearest((453, 470), S_),
    'west1':   nearest((453, 500), S_), 'tanks0': nearest((462, 523.5), E), 'tanks1': nearest((455, 541), W_), 'pier0': nearest((421, 545), S_),
    'pier1':   nearest((416, 555), W_), 'sea0': nearest((408, 575), S_), 'isles': nearest((422, 590), E), 'coast0': nearest((446, 609), (0.6, 0.8)),
    'coast1':  nearest((480, 631), E), 'sq0': nearest((532, 620), E), 'sqN': nearest((555, 605), N_), 'pad': nearest((549.5, TZ), W_),
    'door':    iDoor, 'tubeW': iTubeW, 'gateW': iGateW, 'town0': nearest((474, 585), N_), 'town1': nearest((473, 565), N_),
    'straight0': nearest((SX, 522), N_), 'wheel': iWin, 'dive': iRWDive, 'hole': iRWLip, 'under': iRWLand,
}
out = [f'lap {total:.1f} cells, {N} points; heights {Y.min():.0f} to {Y.max():.0f}; steepest driven {np.abs(grade[ride]).max() * 100:.1f} %']
steepest = sorted(((abs(grade[k]), k) for k in range(N) if ride[k]), reverse=True)
sp = []
for g_, k in steepest:
    if all(min(abs(k - j), N - abs(k - j)) > 20 for j in sp): sp.append(k)
    if len(sp) == 6: break
out.append('  steepest at: ' + ', '.join(f'{abs(grade[k]) * 100:.1f}% point {k} ({X[k] + OX:.0f}, {Z[k] + OZ:.0f})' for k in sp))
over = Y - gs
out.append(f'  over the ground (its 80th percentile across): {np.percentile(over[~gap], 50):.0f} typical, {over[~gap].max():.0f} at the most')
deep = sorted(((over[k], k) for k in range(N) if not gap[k] and over[k] < CLEAR - 300), key=lambda t: t[0])
cuts, seen = [], []
for c, k in deep:
    if all(min(abs(k - j), N - abs(k - j)) > 16 for j in seen): seen.append(k); cuts.append((c, k))
out.append('  cuttings (the deck under the ground): ' + ', '.join(f'{-c + 450:.0f} deep at ({X[k] + OX:.1f}, {Z[k] + OZ:.1f}) point {k}' for c, k in cuts[:10]))
hits = {}
for k in range(N):
    if gap[k]: continue
    for o in np.linspace(-HALF[k] - 0.75, HALF[k] + 0.75, 19):
        x, z = X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o
        i, j = int(z * RQ), int(x * RQ)
        if not (0 <= i < TOP.shape[0] and 0 <= j < TOP.shape[1]) or TOP[i, j] < 0: continue
        if TOP[i, j] > Y[k] - 320 and BOT[i, j] < Y[k] + 1400:
            b = int(WHO[i, j]); hits.setdefault(b, []).append((round(float(x + OX), 1), round(float(z + OZ), 1), int(BOT[i, j]), int(TOP[i, j]), int(Y[k])))
out.append('  decor in the road\'s space (body: count, first (x, z, bottom, top, deck)): ')
for b, h in sorted(hits.items()):
    out.append(f'    {b}{" KEPT" if b in KEEP else ""}: {len(h)} {h[0]} .. {h[-1]}')
for name, iF, iL, iD, iDF in arcs:
    worst = (1e9, None)
    k = iL
    while k != iD:
        for o in np.linspace(-1.5, 1.5, 5):
            x, z = X[k] + Nn[k, 0] * o, Z[k] + Nn[k, 1] * o
            i, j = int(z * RQ), int(x * RQ)
            if 0 <= i < TOP.shape[0] and 0 <= j < TOP.shape[1] and TOP[i, j] >= 0 and Y[k] - TOP[i, j] < worst[0]: worst = (Y[k] - TOP[i, j], int(WHO[i, j]))
            if Y[k] - ground(x, z) < worst[0]: worst = (Y[k] - ground(x, z), -1)
        k = (k + 1) % N
    flight = [Y[q % N] for q in range(iL, iD + (N if iD < iL else 0))]
    out.append(f'  jump {name}: foot {Y[iF]:.0f}, lip {Y[iL]:.0f}, landing {Y[iD]:.0f}, foot {Y[iDF]:.0f}, top {max(flight):.0f}, {ahead(iL, iD):.1f} cells; least clearance {worst[0]:.0f} over {"the ground" if worst[1] == -1 else "body " + str(worst[1])}')
idx = np.arange(N); close = []
for i in range(0, N, 2):
    if gap[i]: continue
    dd = np.hypot(X - X[i], Z - Z[i]); sep = np.minimum(np.abs(idx - i), N - np.abs(idx - i))
    nearby = (sep > 40) & (dd < HALF + HALF[i] + 0.6) & (np.abs(Y - Y[i]) < 2600) & ~gap
    if nearby.any(): close.append((dd[nearby].min(), i, int(np.argmax(nearby))))
for dmin, i, j in sorted(close)[:4]:
    out.append(f'  CLASH: ({X[i] + OX:.1f}, {Z[i] + OZ:.1f}) {Y[i]:.0f} and ({X[j] + OX:.1f}, {Z[j] + OZ:.1f}) {Y[j]:.0f}: {dmin:.1f} cells apart')
# (the flight into the wheel over its wall: the car's underside over the wall's top where it crosses it)
k = iWin
while wheel_r(k) < RW_RIM + 0.3: k = (k - 1) % N
out.append(f'  roulette: lane points {iWin}..{iRWLip} ({ahead(iWin, iRWLip):.1f} cells, {Y[iWin]:.0f} down to {Y[iRWLip]:.0f}), the drop {Y[iRWLip]:.0f} to '
           f'{Y[iRWLand]:.0f} over {ahead(iRWLip, iRWLand):.1f} cells; the jump crosses the wall at ({X[k] + OX:.1f}, {Z[k] + OZ:.1f}) {Y[k] - cone(RW_RIM) - RW_WALL:.0f} over its top')
for m, k in MARK.items():
    out.append(f'    {m:9s} point {k:4d} ({X[k] + OX:6.1f}, {Z[k] + OZ:6.1f}) {Y[k]:6.0f} (ground {gs[k]:5.0f}) cube ({int((X[k] + OX) // 64)},{int((Z[k] + OZ) // 64)})')
out.append(f'  pit lane points {iPit1}..{iPit2}; start point {MARK["start"]}; tunnel {iDoor}..{iGateW}')
print('\n'.join(out))

plan = {
    'originCellX': OX, 'originCellZ': OZ,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in zip(X, Z)],
    'heights': [round(float(y), 1) for y in Y],
    'maxGrade': round(float(np.abs(grade).max()) + 0.05, 3),
    'raised': [0, N - 1], 'raisedHalf': H0, 'asphaltHalf': ASPHALT, 'curbHalf': CURB,
    'raisedHalfs': [round(float(h), 3) for h in HALF],
    'bank': [round(float(b), 4) for b in BANK],
    'arcJumps': [[int(iF), int(iL), int(iD), int(iDF), 0] for name, iF, iL, iD, iDF in arcs],
    'start': int(MARK['start']),
    # the stripe between the pit lane and the race lanes (west of them, -Across), first and last point; the opponents' waiting spots in
    # the pit lane's middle, facing up the straight, on the deck; Citadel Island's white fence along the stripe; the grid on the race lanes
    'pitStripe': [int(iPit1), int(iPit2), round(float(-(PIT_HALF - PIT)), 3)],
    'pitSpots': [[round(SX - (PIT_HALF - PIT / 2) - OX, 3), z - OZ, 0.0, -1.0, round(float(Y[nearest((SX, z), N_)]), 1)] for z in PIT_WAIT],
    'pitFence': True,
    'gridShift': PIT / 2,
    'raisedCut': True,
    'tunnels': TUNNELS,
    'driveThrough': DRIVE_THROUGH,
    'keepBodies': KEEP,
    'stayPut': [112],
    # (the raised road in pieces of 10 cells, not 4: the town's and the palace's cubes are near their 200 decors already)
    'raisedPiece': 10.0,
    # the Francos' machine gun on a knoll inside the corner past the start, where the game has it beside the road (the user, 2026-10-09:
    # "move it to the high ground that's just above the track at that corner, and add in a franco to fire in bursts so that the bullets
    # only land on the inside of the corner"): the knoll's middle (plan cells), the corner's points, its top over the corner's deck
    'gunners': [{'knoll': [79.5, 15.5], 'from': 178, 'to': 204, 'rise': 500}],
    # the roulette wheel the jump over the inlet lands in (RaceTrackRoulette): its middle (plan cells), its bowl's wall and its hole (cells
    # from the middle), its floor's height at the hole and its rise a cell outwards, the wall's height; the lane round it (points), where
    # the white ball rolls (points), how fast the wheel turns (degrees a second)
    'roulette': {'centre': [round(RWX - OX, 3), round(RWZ - OZ, 3)], 'rim': RW_RIM, 'hole': RW_HOLE, 'holeY': RW_YH, 'slope': RW_S, 'wall': RW_WALL,
                 'from': int(iWin), 'to': int(iRWLip), 'ballFrom': int(iBall0), 'ballTo': int(iBall1), 'spin': 24.0,
                 # (its legs: degrees round from east towards south, clear of the road under it)
                 'legs': [110.0, 230.0, 340.0]},
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
    for m, k in MARK.items(): d.text(P(X[k], Z[k]), m, fill=(255, 255, 255))
    img.save(D + 'design.png')
    W, Hh = 1600, 360
    prof = Image.new('RGB', (W, Hh), (20, 20, 30)); pd = ImageDraw.Draw(prof)
    for k in range(N - 1):
        x0, x1 = k * W / N, (k + 1) * W / N
        pd.line([(x0, Hh - Y[k] / 16000 * Hh), (x1, Hh - Y[k + 1] / 16000 * Hh)], fill=(255, 255, 255) if not gap[k] else (255, 80, 80))
        pd.point((x0, Hh - gs[k] / 16000 * Hh), fill=(160, 120, 70))
    for m, k in MARK.items(): pd.text((k * W / N, 4 + (hash(m) % 3) * 10), m[:7], fill=(200, 200, 100))
    prof.save(D + 'profile.png')
