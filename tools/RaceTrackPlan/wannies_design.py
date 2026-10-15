# Wannies Island (PLATFORM.ILE, island 6; outside scenes 96-99, cubes (7,7)..(8,8)) -- the race track from the user's drawing (2026-10-14:
# "Proposed track for Platform.ile. Pink is jump, and purple is a boost pad. The massive blue circle is to be a giant frog that we drive
# through. The frog repeatedly opens and closes its mouth and extends its tongue as it does so, so we have to time the jump into the mouth.
# Exiting the frog we go up a slope to the top of the tower, then there's a loop coming down the tower looping around about three times,
# into the start finish line and the pit lane"; then: the boost pad and its jump a shortcut beside the road round the right, the road
# built up to the frog's jump "which then lifts the frog higher into the air, this will then make the slope to the top of the tower less
# steep", and a car meeting a shut mouth bounced back). Second round, 2026-10-15: twice down the tower, not three times; the start
# straight on south from it, a thinner pit lane of its own beside it; the shortcut a real one -- a high leap over the west island's loop
# -- and boost pads round that loop.
#
# The drawing is the editor's map turned a quarter anticlockwise and stretched (E:\dump\TEMP\wan\trace.py: u = 55 + 9 (z - 448),
# v = 5 + 5.82 (576 - x)); its line read off it (route_points.py) and laid on the island:
#  - the start straight over the lava east of the pier (its pit lane down the pier beside it), onto the east island, down its east side,
#    west along its south side (the shortcut off its corner: over a boost pad, up a ramp and a leap onto the S before the frog), the west
#    island's south loop (boost pads round it), north-east and north-west up to the frog's jump -- climbing all the way, the road high
#    over the islands, to the frog high in the air;
#  - the jump into the frog's mouth, through the frog, out of its back, east over the lava along the island's north edge, climbing to the
#    elevator tower's top (the Wannies' elevator, bodies 19-31: 2,950 to 24,455);
#  - twice round the tower down a helix (a stadium round it: the tower stands 15 cells from the island's north edge, cube row 6
#    is missing), out of it straight on south onto the start straight, the pit lane beside it.
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
PIT_X = 551.0                           # the pit lane: down the pier's east side, beside the start straight (2026-10-15) ...
PIT_HALF = 2.25                         # ... thinner than the road
PIT_SPOTS = (494.0, 498.0, 502.0)       # ... the opponents' waiting places along it
TOWER = (543.5, 467.5)                  # the elevator tower's middle (its top block 537.6..549.4 x 463.9..471.1, its legs 536.6..550.4 x 462.8..472.2)
TOWER_TOP = 24455
# the helix round the tower: a stadium -- two half circles (radius HR, their middles HZ0 and HZ1 on the tower's line north-south) joined
# by straights at x = TOWER.x -+ HR -- three times round clockwise, down from H_TOP; in from the west at its north point, out down its east
# straight
HR = 15.0
HZ0, HZ1 = 469.0, 478.0               # (the inner rail 10.25 from the middles: clear of the legs' corners, 9.3 off)
TURNS = 2                               # (three until 2026-10-15: "reduce the downward loops around the elevator by one")
H_TOP = 25000.0                         # the helix's top: over the tower's top
H_BOTTOM = 7000.0                       # out of it (3,300 until 2026-10-15: the start higher, the lap's climb to the frog shorter -- the
                                        # shortcut's leap onto the lap is up, not down)
START_H = 6500.0                        # the start straight (over the lava; the pier's ground 2,000)
CLIMB = 0.135                           # the lap's grade up to the frog but round the loop the shortcut leaps
H_FROG = 20400.0                        # the frog's floor (the jump's lip and landing)
GRADE = 0.17
CLEAR, LAVA, LAVA_MIN = 400.0, 700.0, 1400.0

def C(x, z): return (x - OX, z - OZ)

# ---- the lap: corners (island cells) and their radii, from the start straight's foot round to the frog's take-off ramp
ROUND = [((558.5, 490.0), 0.0),          # (the start straight: straight on south from the helix, over the lava east of the pier) ...
         ((558.5, 518.0), 10.0),         # ... onto the east island, south-south-east past its buildings ...
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
         ((497.5, 507.0), 7.0)]          # ... north, then west and round north-north-east up to the frog (FROG_ROUND) ...
# the frog: the road straight through it along the drawing's line, from its mouth (the drawing's pink dot 477.3, 484.7) to where the line
# leaves the circle (483.7, 466.9) -- and straight before it, up the take-off ramp (RAMP cells) and over the gap (GAP) into the mouth
FROG_MOUTH = (477.3, 484.7)
FROG_DIR = np.array([483.7 - 477.3, 466.9 - 484.7]); FROG_DIR /= np.linalg.norm(FROG_DIR)
FROG_LEN, GAP, RAMP = 18.0, 5.0, 10.0
FROG_HALF = 3.5                         # the road's half width through the frog (its throat 4.2 cells either side: RaceTrackFrog.ThroatHalf)
def along(t): return (float(FROG_MOUTH[0] + FROG_DIR[0] * t), float(FROG_MOUTH[1] + FROG_DIR[1] * t))
FROG_LIP, FROG_EXIT = along(-GAP), along(FROG_LEN)
ROUND += [(along(-26.0), 7.0),          # ... the turn onto the frog's line (its bend over 10.8 cells before the ramp's foot) ...
          (FROG_LIP, 0.0)]              # ... its take-off ramp's lip
APPROACH = [(FROG_LIP, 0.0), (along(26.0), 8.0),            # through the frog, out of its back, on along its line ...
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
OUTWAY = [((TOWER[0] + HR, HZ1), 0.0), ((TOWER[0] + HR, 486.0), 0.0)]   # out of it straight on south (the user, 2026-10-15)
lap_part = rounded(ROUND)
approach = rounded(APPROACH)
outway = rounded(OUTWAY)
join = rounded([(tuple(outway[-1]), 0.0), (ROUND[0][0], 0.0)])
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
MARK['start'] = nearest_after((558.5, 497.0), 0, 30)                           # the start line on the straight
MARK['lip'] = nearest_after(FROG_LIP, 0, total)                                # the frog's take-off lip ...
MARK['foot'] = (MARK['lip'] - int(RAMP / STEP)) % N                            # ... its ramp's foot
MARK['mouth'] = nearest_after(FROG_MOUTH, MARK['lip'], 20)                     # ... the mouth: the landing
MARK['back'] = nearest_after(FROG_EXIT, MARK['mouth'], 40)                     # ... out of its back
def at_cells(c): return int(round(c / total * N)) % N
MARK['top'] = at_cells(piece_at[2])                                            # the helix's top: its north point, the first time
MARK['hOut'] = at_cells(piece_at[3])                                           # out of the helix
MARK['pier'] = at_cells(piece_at[4] + lengths[4])                              # on the pier: the start straight's foot
# (the shortcut's ends on the lap -- its own way further on: where it leaves the lap, where its leap comes down onto it, where it is back)
SC_F = nearest_after((536.2, 557.4), MARK['start'], 300)                   # off the lap: straight on north-west where the lap turns west
SC_LIP = np.array([510.0, 533.0])                                         # its take-off (the user's pink, 2026-10-15) ...
SC_D = nearest_after((484.0, 507.8), SC_F, 400)                           # ... its landing: on the S, west past its turn (the other pink)
SC_J = (SC_D + int(7 / STEP)) % N                                         # ... back on the lap's own road

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
H_FORK = START_H + ahead(MARK['start'], SC_F) * 512 * CLIMB
H_LAND = H_FROG - 900 - ahead(SC_D, MARK['foot']) * 512 * CLIMB
climb(MARK['start'], SC_F, START_H, H_FORK)
climb(SC_F, SC_D, H_FORK, H_LAND)                                         # round the loop the shortcut leaps: gently
climb(SC_D, MARK['foot'], H_LAND, H_FROG - 900)
for k in range(MARK['foot'], MARK['lip'] + 1): Y[k % N] = H_FROG - 900 + 900 * ease(ahead(MARK['foot'], k) / ahead(MARK['foot'], MARK['lip']))
climb(MARK['lip'], MARK['back'], H_FROG, H_FROG)
climb(MARK['back'], MARK['top'], H_FROG, H_TOP, lambda t: t * t * (3 - 2 * t) * 0.5 + t * 0.5)
climb(MARK['top'], MARK['hOut'], H_TOP, H_BOTTOM)
climb(MARK['hOut'], MARK['pier'], H_BOTTOM, START_H, ease)
# (the jump into the frog: the gap from the lip to the mouth -- no road there)
gap = np.zeros(N, bool)
for k in range(MARK['lip'] + 1, MARK['mouth']): gap[k % N] = True
arcs = [('frog', MARK['foot'], MARK['lip'], MARK['mouth'], (MARK['mouth'] + int(4 / STEP)) % N)]

# ---- (the pit lane: a branch of its own beside the start straight -- further on, with the shortcut)
# (narrower up the frog's ramp and through it -- into its mouth -- and back out after it)
iWide = (MARK['back'] + int(6 / STEP)) % N
for k in range(N):
    if between(k, MARK['foot'], MARK['lip']): HALF[k] = H0 + (FROG_HALF - H0) * ease(ahead(MARK['foot'], k) / ahead(MARK['foot'], MARK['lip']))
    elif between(k, MARK['lip'], MARK['back']): HALF[k] = FROG_HALF
    elif between(k, MARK['back'], iWide): HALF[k] = FROG_HALF + (H0 - FROG_HALF) * ease(ahead(MARK['back'], k) / ahead(MARK['back'], iWide))

grade = (np.roll(Y, -1) - Y) / (STEP * 512)
ride = ~gap

# ---- the shortcut (the user, 2026-10-15: "change the jump section so it acts as a short cut ... add some boost pads around the other loop
# of track that the jump skips so if driven well it's the same as if not quicker than taking the jump"): a branch of the raised road.
# Where the lap turns west at the east island's south-west corner it goes straight on north-west over its boost pad, bends north-north-west
# up a ramp to the take-off (the user's pink), and leaps -- high, hanging in the air SC_FLIGHT_MS, curving west over the S's turn -- onto
# the lap past the turn (the other pink): the loop round the west island's south left out. The lap climbs only gently round that loop and
# CLIMB elsewhere, so that the leap is up onto the lap (no jump lands lower than it took off). Round the loop, stronger boost pads
# (LOOP_PADS, SC_LOOP_LIFT times the car's top speed; the shortcut's own the engine's): driven well, the loop is as quick. Level with the lap
# where it leaves it (a little over it while the two decks overlap), no deck of its own where it lies on the lap's road.
SC_HALF = 3.5                           # its half width
SC_OVER = 40.0                          # over the lap's deck where they overlap
SC_RAMP, SC_KICK, SC_ARC = 9.0, 700.0, 2500.0
SC_FLIGHT_MS = 2400                     # the leap's time in the air (the engine's arcjump= sixth number)
SC_LOOP_LIFT = 2.1                      # the loop's boost pads' lift (the engine's own: 1.7)
P0 = np.array([X[SC_F] + OX, Z[SC_F] + OZ]); d0 = T[SC_F] / np.linalg.norm(T[SC_F])
# (the ramp's way: from the take-off towards the S's turn; the bend onto it where the north-west line meets it)
u = np.array([497.5, 514.0]) - SC_LIP; u /= np.linalg.norm(u)
t_, s_ = np.linalg.solve(np.array([[d0[0], u[0]], [d0[1], u[1]]]), SC_LIP - P0)
V2 = P0 + d0 * t_
turn2 = math.acos(max(-1, min(1, float(d0 @ u))))
tan2 = 10.0 * math.tan(turn2 / 2)
dr = rounded([(tuple(P0), 0.0), (tuple(V2), 10.0), (tuple(SC_LIP), 0.0)])
# (the leap: on along the ramp's way, curving round onto the lap's way where it comes down)
PD = np.array([X[SC_D] + OX, Z[SC_D] + OZ]); tD = T[SC_D] / np.linalg.norm(T[SC_D])
a_, b_ = np.linalg.solve(np.array([[u[0], tD[0]], [u[1], tD[1]]]), PD - SC_LIP)
V3 = SC_LIP + u * a_
turn3 = math.acos(max(-1, min(1, float(u @ tD))))
R3 = min(18.0, 0.9 * min(a_, b_) / math.tan(turn3 / 2))
fl = rounded([(tuple(SC_LIP), 0.0), (tuple(V3), R3), (tuple(PD), 0.0)])
def resample(pl):
    pl = pl[np.concatenate([[True], np.hypot(*np.diff(pl, axis=0).T) > 1e-9])]
    cum = np.concatenate([[0], np.cumsum(np.hypot(*np.diff(pl, axis=0).T))])
    m = max(1, int(round(cum[-1] / STEP)))
    t = np.linspace(0, cum[-1], m + 1)
    return np.interp(t, cum, pl[:, 0]), np.interp(t, cum, pl[:, 1]), cum[-1]
DX_, DZ_, dr_len = resample(dr); FX_, FZ_, fl_len = resample(fl)
tailK = [k % N for k in range(SC_D + 1, SC_J + 1)]
BX = list(DX_) + list(FX_[1:]) + [X[k] + OX for k in tailK]; BZ = list(DZ_) + list(FZ_[1:]) + [Z[k] + OZ for k in tailK]
BM = len(BX)
sc_cum = np.array([dr_len + fl_len])
sL = len(DX_) - 1                       # the take-off
sF = sL - int(SC_RAMP / STEP)           # the ramp's foot
sD = sL + len(FX_) - 1                  # the landing (on the lap)
sDF = min(BM - 1, sD + int(4 / STEP))   # its foot
bend_start = int((t_ - tan2) / STEP)
bend_end = int((t_ - tan2 + turn2 * 10.0) / STEP) + 1
def lap_y(x, z):
    k = min(range(N), key=lambda k: (X[k] + OX - x) ** 2 + (Z[k] + OZ - z) ** 2)
    return Y[k], math.hypot(X[k] + OX - x, Z[k] + OZ - z), HALF[k]
BY = np.zeros(BM); B_HALF = np.full(BM, SC_HALF); b_hidden = np.zeros(BM, bool)
# (with the lap -- a little over its deck, eased in -- until the two have parted, then up at CLIMB to the ramp's foot)
def over(dist, lh, half): return SC_OVER * min(1.0, max(0.0, (dist + half - lh - 0.3) / 1.5))
kd = next(k for k in range(BM) if lap_y(BX[k], BZ[k])[1] >= lap_y(BX[k], BZ[k])[2] + SC_HALF)
for k in range(kd): ly, dist, lh = lap_y(BX[k], BZ[k]); BY[k] = ly + over(dist, lh, SC_HALF)
for k in range(kd, sF + 1): BY[k] = BY[kd - 1] + CLIMB * (k - kd + 1) * STEP * 512
yFoot = BY[sF]; yLip = yFoot + SC_KICK; yLand = Y[SC_D]
for k in range(sF + 1, BM):
    if k <= sL: BY[k] = yFoot + SC_KICK * ease((k - sF) / (sL - sF))
    elif k < sD:
        f = (k - sL) / (sD - sL)
        BY[k] = yLip + (yLand - yLip) * f + 4 * SC_ARC * f * (1 - f)
    else: BY[k] = lap_y(BX[k], BZ[k])[0]
for k in range(BM):
    ly, dist, lh = lap_y(BX[k], BZ[k])
    if dist + SC_HALF <= lh + 0.3 and abs(BY[k] - ly) < 120: b_hidden[k] = True        # wholly on the lap's road, at its height
b_gap = np.zeros(BM, bool); b_gap[sL + 1:sD] = True
b_hidden &= ~b_gap
SC_PAD = (max(0, bend_start - int(6 / STEP)), max(0, bend_start - int(2 / STEP)))   # its boost pad: 4 cells on the straight before the bend
bgrade = np.diff(BY) / (STEP * 512)
hid = [[int(a), int(b)] for a, b in zip(*[np.flatnonzero(np.diff(np.concatenate([[0], b_hidden.astype(int), [0]])) == v) - (0 if v == 1 else 1) for v in (1, -1)])]
# the loop's boost pads: just past the fork, on the lap's way west past its corner, on the S's lower leg -- each 4 cells, the road's middle
LOOP_PADS = []
for where, after in (((None, 14.0), SC_F), (((487.0, 547.3), None), SC_F), (((481.0, 527.0), None), SC_F)):
    if where[0] is None: k = (after + int(where[1] / STEP)) % N
    else: k = nearest_after(where[0], after, 300)
    LOOP_PADS.append([int(k), int((k + int(4 / STEP)) % N), -1.6, 1.6, SC_LOOP_LIFT])

# ---- the pit lane (the user, 2026-10-15: "The main path now goes straight on, with a thinner pitlane to the side"): a branch of the raised
# road off the start straight's west side just out of the helix, down the pier's east side past the start line -- the opponents' waiting
# places along it -- and back onto the straight before it bends onto the east island. Level with the straight beside it (a little over it
# where the two decks overlap), no deck of its own where it lies on the straight's.
PL_F = nearest_after((558.5, 480.0), MARK['hOut'], 20)
PL_J = nearest_after((558.5, 515.5), PL_F, 60)
pl = rounded([((X[PL_F] + OX, Z[PL_F] + OZ), 0.0), ((558.5, 483.0), 5.0), ((PIT_X, 490.5), 5.0), ((PIT_X, 505.5), 5.0), ((558.5, 513.0), 5.0),
              ((X[PL_J] + OX, Z[PL_J] + OZ), 0.0)])
pl = pl[np.concatenate([[True], np.hypot(*np.diff(pl, axis=0).T) > 1e-9])]
pl_cum = np.concatenate([[0], np.cumsum(np.hypot(*np.diff(pl, axis=0).T))])
PM = int(round(pl_cum[-1] / STEP))
PX = np.interp(np.linspace(0, pl_cum[-1], PM + 1), pl_cum, pl[:, 0]); PZ = np.interp(np.linspace(0, pl_cum[-1], PM + 1), pl_cum, pl[:, 1])
straightK = [k % N for k in range(PL_F, PL_F + int(ahead(PL_F, PL_J) / STEP) + 1)]
_sz = np.array([Z[k] + OZ for k in straightK]); _sy = np.array([Y[k] for k in straightK]); _o = np.argsort(_sz)
def straight_y(z): return float(np.interp(z, _sz[_o], _sy[_o]))
PY = np.array([straight_y(z) for z in PZ]); P_HALF = np.full(PM + 1, PIT_HALF); p_hidden = np.zeros(PM + 1, bool)
for k in range(PM + 1):
    off = abs(PX[k] - (X[PL_F] + OX))
    if off + PIT_HALF <= H0 + 0.3: p_hidden[k] = True
    else: PY[k] += SC_OVER * max(0.0, min(1.0, (off + PIT_HALF - H0 - 0.3) / 1.5, (H0 + PIT_HALF + 1.5 - off) / 1.5))   # (eased in and out)
pit_hid = [[int(a), int(b)] for a, b in zip(*[np.flatnonzero(np.diff(np.concatenate([[0], p_hidden.astype(int), [0]])) == v) - (0 if v == 1 else 1) for v in (1, -1)])]
PIT_SPOT_LIST = []
for zs in PIT_SPOTS:
    k = int(np.argmin(np.abs(PZ - zs) + 100 * (np.abs(PX - PIT_X) > 0.2)))
    PIT_SPOT_LIST.append([round(float(PX[k] - OX), 3), round(float(PZ[k] - OZ), 3), 0.0, 1.0, round(float(PY[k]), 1)])

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
# the shortcut: its marks, steepest grade, the jump, what it passes over, its decor hits, how near the rest of the lap is
out.append(f'  shortcut: {BM} points, {dr_len:.1f} cells to its take-off, a leap of {fl_len:.1f} (curving {math.degrees(turn3):.0f} deg, radius {R3:.1f}), then {len(tailK) * STEP:.1f} along the lap; '
           f'the lap there {ahead(SC_F, SC_J):.1f} cells; the lap {H_FORK:.0f} at the fork, {H_LAND:.0f} where the leap comes down; loop pads {LOOP_PADS}')
for name, k in (('fork', 0), ('pad', SC_PAD[0]), ('padEnd', SC_PAD[1]), ('bend0', bend_start), ('foot', sF), ('lip', sL), ('land', sD), ('landFt', sDF), ('join', BM - 1)):
    out.append(f'    {name:6s} {k:4d} ({BX[k]:6.1f}, {BZ[k]:6.1f}) {BY[k]:6.0f} (ground {ground(BX[k] - OX, BZ[k] - OZ):5.0f}){" hidden" if b_hidden[k] else ""}')
out.append(f'    steepest driven {max(abs(g) for i, g in enumerate(bgrade) if not b_gap[i] and not b_gap[i + 1]) * 100:.1f} %; the jump {BY[sD] - BY[sL]:+.0f} over {(sD - sL) * STEP:.1f} cells; hidden {hid}')
bh = {}
for k in range(BM):
    if b_gap[k]: continue
    nx_, nz_ = -(BZ[min(k + 1, BM - 1)] - BZ[max(k - 1, 0)]), BX[min(k + 1, BM - 1)] - BX[max(k - 1, 0)]
    ln = math.hypot(nx_, nz_) or 1
    for o in np.linspace(-SC_HALF - 0.3, SC_HALF + 0.3, 9):
        x, z = BX[k] - OX + nx_ / ln * o, BZ[k] - OZ + nz_ / ln * o
        i, j = int(z * RQ), int(x * RQ)
        if not (0 <= i < TOP.shape[0] and 0 <= j < TOP.shape[1]) or TOP[i, j] < 0: continue
        if TOP[i, j] > BY[k] - 350 and BOT[i, j] < BY[k] + 1400: bh.setdefault(int(WHO[i, j]), []).append((round(x + OX, 1), round(z + OZ, 1)))
out.append('    decor in its space: ' + '; '.join(f'{b}: {len(h)} {h[0]}' for b, h in sorted(bh.items())))
fl = list(range(sL, sD + 1))
out.append(f'    under the flight: the ground at most {max(ground(BX[k] - OX, BZ[k] - OZ) for k in fl):.0f}, the flight at least {min(BY[k] for k in fl):.0f}')
cl = []
for k in range(0, BM, 2):
    if b_hidden[k] or b_gap[k]: continue
    for m in range(0, N, 2):
        if between(m, SC_F, SC_J) and abs(Y[m] - BY[k]) < 2600:
            dd = math.hypot(X[m] + OX - BX[k], Z[m] + OZ - BZ[k]) - HALF[m] - SC_HALF
            if dd < 0.5: cl.append((round(dd, 1), k, m, round(BY[k] - Y[m])))
out.append(f'    its deck near the lap (cells apart, its point, the lap point, height over it): {sorted(cl)[:6]}')
pgrade = np.diff(PY) / (STEP * 512)
out.append(f'  pit lane: {PM + 1} points, {pl_cum[-1]:.1f} cells from the straight at ({PX[0]:.1f}, {PZ[0]:.1f}) to ({PX[-1]:.1f}, {PZ[-1]:.1f}), {PY.min():.0f} to {PY.max():.0f}, '
           f'steepest {np.abs(pgrade).max() * 100:.1f} %, hidden {pit_hid}; its spots {[(p[0] + OX, p[1] + OZ, p[4]) for p in PIT_SPOT_LIST]}; '
           f'the straight at the start line {X[MARK["start"]] + OX:.1f}, the pit lane {PX[int(np.argmin(np.abs(PZ - (Z[MARK["start"]] + OZ))))]:.1f}')
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
    'pitSpots': PIT_SPOT_LIST,
    'boostPanels': LOOP_PADS,
    'raisedCut': True,
    'keepBodies': [19, 20, 21, 27, 28, 29, 30, 31],
    # the giant frog (RaceTrackFrog): into its mouth -- the carried jump's landing -- and out of its back; the camera kept low in its throat
    'frog': {'mouth': int(MARK['mouth']), 'back': int(MARK['back'])},
    'tunnels': [{'from': int(MARK['mouth']), 'to': int(MARK['back']), 'roof': 2700, 'pieces': False}],
    # the shortcut (RaceTrackPlan.Branches): off the lap at SC_F, over the boost pad, up the ramp, the jump, onto the lap at SC_J
    'branches': [{
        'points': [[round(float(x - OX), 3), round(float(z - OZ), 3)] for x, z in zip(BX, BZ)],
        'heights': [round(float(y), 1) for y in BY],
        'halfs': [round(float(h), 3) for h in B_HALF],
        'from': int(SC_F), 'to': int(SC_J),
        'arcJumps': [[int(sF), int(sL), int(sD), int(sDF), -1, SC_FLIGHT_MS]],
        'boostPanels': [[int(SC_PAD[0]), int(SC_PAD[1]), -1.6, 1.6]],
        'hidden': hid,
    }, {
        # the pit lane: off the start straight at PL_F, back onto it at PL_J
        'points': [[round(float(x - OX), 3), round(float(z - OZ), 3)] for x, z in zip(PX, PZ)],
        'heights': [round(float(y), 1) for y in PY],
        'halfs': [round(float(h), 3) for h in P_HALF],
        'from': int(PL_F), 'to': int(PL_J),
        'hidden': pit_hid,
    }],
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
    for k in range(PM):
        d.line([P(PX[k] - OX, PZ[k] - OZ), P(PX[k + 1] - OX, PZ[k + 1] - OZ)], fill=(80, 80, 80) if p_hidden[k] else (255, 120, 0), width=3)
    for p_ in PIT_SPOT_LIST: d.ellipse([P(p_[0], p_[1])[0] - 4, P(p_[0], p_[1])[1] - 4, P(p_[0], p_[1])[0] + 4, P(p_[0], p_[1])[1] + 4], fill=(255, 255, 0))
    for k in range(BM - 1):
        col = (255, 255, 255) if b_gap[k] else (255, 0, 255) if SC_PAD[0] <= k <= SC_PAD[1] else (80, 80, 80) if b_hidden[k] else (255, 160, 0)
        d.line([P(BX[k] - OX, BZ[k] - OZ), P(BX[k + 1] - OX, BZ[k + 1] - OZ)], fill=col, width=3)
    img.save(D + 'lap.png')
