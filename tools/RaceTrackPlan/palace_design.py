"""The Emperor's palace race (the user, 2026-10-12: "through the palace rooms, coming out the window and around the roof tops?
Point-to-point i.e. no laps, finishing with massive jump to harbor teleport? - Twinsen, quick, Dark monk's in the palace, he's killing
everybody. Leading into final showdown at Celebration"), on the Palace island (Terrain/Palace: PALACE.ILE, its layout PALACE.JSON).

Second version, the same day (the user: "The palace track is far too narrow ... Let's make the inside tracks wider if we can do, and
perhaps loop in more rooms. Add a section where we're outside on the roof looping around the palace roof tops"): the island's doors are
four bricks wide now (16 cells: PalaceIsland.DoorBricks).

Third version, the same day (the user: "For our palace track let's scale up the roof top so it covers more of the outside of the palace
area. For the inside of the palace let's remove the track but add some arrows to allow cars to take multiple directions, it has a single
entry and exit area so all cars will ultimately end up back on the same track"): no road in the rooms, arrows on their floor, the
opponents each their own way through; a loop round a flat roof top.

Fourth version, 2026-10-13 (the user: "The roof of our Palace needs a lot of work. Let's make the roof top bigger, and remove the track from
the outside for now. The track on the outside needs to be raised in the air looping around the giant turrets that form the roof, which
means the existing track section outside needs to go"): the hilltop is flat 54 cells round the palace now (PalaceCliff), and the race a
sprint (the plan's open) through the palace alone:
  - in the sixteen rooms no road at all (the plan's bare): the cars drive the rooms' floor -- the race-track mode keeps them on it and
    inside the rooms, its open_area= (the plan's openAreas) -- from the start in the south-east room (154) to the north-west one (163) and
    out of its north door into the long hall, each room's ways on west and north shown by arrows on its floor (floorArrows), any way
    through the rooms' doors (all four of a room's walls have one). The route itself, the test pilot's: west along the bottom row (153,
    152, 151) and north up the west column (155, 159, 163); the opponents after the first each their own way (openRoutes, in turn): north
    up the east column and west along the top row; a staircase from 153, north and west by turns; another from 158 (for a fourth opponent);
  - north up the long hall and up a ramp to the round window Twinsen leaves by: the finish line in the window, and the leap out of it over
    the cloud sea between the hill's two arms -- won, the Dark Monk's teleporter takes the car in mid-flight (the race-track mode's sprint,
    wake=, wake_flight=); lost, the race is run again from the grid before it comes down.
(Until the road round the roof's turrets, high in the air, is made.)

Fifth version, 2026-10-13, later (the user: "The start area needs changing so the start line lines up with the straight of the track",
"Add a raised track outside on the roof that loops around the roof turrets", "Enlarge the track width so it's the same width as a door
way", "Add an entrance door to the palace in the start room", and the fireplace in room 151 a teleporter to room 159's):
  - the grid in front of the palace's new doors in room 154's east wall, on the bottom row's carpet line, the start line ahead on it --
    the chest that stood there moved to the room's north wall (PalaceIsland);
  - out of the round window a raised road (RAISE over the roof's rock): north, east over the cloud sea and south past the long hall's
    turrets, climbing to the roof, on south down the middle way between the four turrets (they are SlotFill 0.80 of their places now: the
    ways between them some 24 cells), west through the middle way across, south down the west side, east along the south side and north
    up the east side -- round three sides of the south-west turret and two of the south-east one -- and east off the roof's edge: the
    leap over the harbour, through the Dark Monk's teleporter on its pillar (the finish line at the lip). (First east out over the sea
    and round by the turrets' north side: half a minute of road in the fog with nothing beside it);
  - the teleporter: the plan's teleports, from the island's layout (RACEMOD.CPP teleport=);
  - then the user's own way round the roof (2026-10-13, a drawing: "a proposed new track route for outside the palace ... starting at the
    point the current track exits the building, at the end of the route let's add a loop of track going up into the air, culminating
    with a large take off ramp"): out of the round window a hook east over the sea and back west between the long hall's two turrets,
    south down the west side of the second, east along the north-west turret's north side, south down the middle way, west through the
    middle way across, round the south-west turret -- south down the west side, east along the south side, north up the middle way --
    climbing all the way (BRIDGE: the second time through the middle crossing it passes over the first), east through the middle way's
    east half, north up the east side, west to a spiral out over the sea north of the turrets (SPIRAL_LOOPS climbing SPIRAL_RISE) and east
    off its top down a straight to a large kicker (KICK over RUNUP): the leap through the teleporter on its pillar -- moved onto the
    leap's line and raised to it (PalaceIsland.PillarNorth, PillarRise);
  - the carpets four bricks across (16 cells): the guards stand off them further.

Usage: palace_design.py <game folder with PALACE.ILE and PALACE.JSON: the island built on its own, `ScriptRoundTrip palace`> [out plan]
"""
import json, math, sys
import numpy as np
sys.path.insert(0, __file__.rsplit('\\', 1)[0] if '\\' in __file__ else '.')
from ile import load_ile, hqr_entries

GAME = sys.argv[1] if len(sys.argv) > 1 else r'E:\dump\TEMP\palace3\v6'
OUT = sys.argv[2] if len(sys.argv) > 2 else r'E:\dump\LBAAssembler\docs\racetrack\palace_track_plan.json'
L = json.load(open(GAME + r'\PALACE.JSON'))
_, _, _, CUBES = load_ile(GAME + r'\PALACE.ILE')
SP = 0.5                                    # the plan's points, cells apart
FLOOR = L['floorTop']                       # the rooms' floor: what the cars drive on in them
TOP = L['hilltop']                          # the hilltop: the roofs' top
DECK = 60                                   # the raised road over what it lies on
HALF_IN, HALF_OUT = 5.5, 7.0                # the road's half width (cells): up to the round window (16 cells across), out over the sea
JUMP_APEX = 1400                            # the leap's top over its lip (its top under 32,767)
FLIGHT = 80                                 # the leap's length (cells): a car at three times the race car's speed is still in the air when
                                            # the teleporter takes it (wake_flight=, 2.4 s) -- within the island's cubes
PAD = 12                                    # where the leap comes down (no road: never reached)
RAISE = 1500                                # the road round the turrets: this much over the roof's rock
KICK = 1000                                 # the take-off ramp: its rise to the leap's lip over its last RUNUP cells
RUNUP = 12
GAP_OVER = 380                              # the road through the gap between the long hall's turrets: this much over the roof's rock
BRIDGE = 4500                               # the road's rise round the south-west turret: its second way through the middle crossing over its first
SPIRAL_LOOPS = 1.5                          # the spiral at the end: its turns ...
SPIRAL_RISE = 5000                          # ... and its climb (3,333 a turn: the turns pass over one another, the straight off its top over
                                            # its first turn)
HALF_MID = 6.0                              # the road's half width over the palace, between its turrets ...
HALF_TIGHT = 5.5                            # ... and through the middle way's east half (TR and BR some 16 cells apart)
# (a car's collision box: some 1,420 units either way of its middle whichever way it faces: 2.8 cells. Where the cars drive the floor,
# nothing within CAR_HALF of a line they follow; on the ramp nothing within the road's half-width less the rail's inset and a box)
CAR_BOX = 2.8
CAR_HALF = 1.7

# ---- where things are
rooms = {r['scene']: r for r in L['rooms']}
def mid(scene):
    r = rooms[scene]; return ((r['x0'] + r['x1'] + 1) / 2, (r['z0'] + r['z1'] + 1) / 2)
# (a row's doors are in a line 2 cells past its rooms' middles, a column's the same: the rooms' grids put them there)
def row(scene): return mid(scene)[1] + 2
def col(scene): return mid(scene)[0] + 2
win = L['window']
wx = (win['x0'] + win['x1']) / 2            # the round window's middle across, the wall's inner face and outer
w_in, w_out = win['z1'], win['z0']
sill = win['sill']
hall = rooms[80]
SIXTEEN = [s for s in rooms if s != 80]
sq_x0, sq_x1 = min(rooms[s]['x0'] for s in SIXTEEN), max(rooms[s]['x1'] for s in SIXTEEN) + 1
sq_z0, sq_z1 = min(rooms[s]['z0'] for s in SIXTEEN), max(rooms[s]['z1'] for s in SIXTEEN) + 1

# ---- the boxes: what a car hits, by height (decors with a real box; world units)
boxes = []
read, _ = hqr_entries(GAME + r'\PALACE.ILE')
cubemap = np.frombuffer(read(0)[:256], dtype=np.uint8).reshape(16, 16)
for cz in range(16):
    for cx in range(16):
        cid = cubemap[cz, cx] & 127
        if not cid or cid not in CUBES: continue
        for d in CUBES[cid].decors:
            if d[10] <= d[7]: continue
            boxes.append((cx * 32768 + d[6], d[7], cz * 32768 + d[8], cx * 32768 + d[9], d[10], cz * 32768 + d[11]))
boxes = np.array(sorted(set(boxes)), float)
print('collision boxes', len(boxes))


def blocked(x, z, y, half):
    """Whether a box stands in a car's way at (x, z) cells, its floor y: within `half` cells of the point, between y+120 and y+900."""
    X, Z = x * 512, z * 512
    hit = (boxes[:, 0] < X + half * 512) & (boxes[:, 3] > X - half * 512) & (boxes[:, 2] < Z + half * 512) & (boxes[:, 5] > Z - half * 512) \
          & (boxes[:, 1] < y + 900) & (boxes[:, 4] > y + 120)
    return bool(hit.any())


def ground(x, z):
    """The island's ground height at (x, z) cells (bilinear in the cell)."""
    gx, gz = int(math.floor(x)), int(math.floor(z))
    cid = cubemap[gz // 64, gx // 64] & 127
    if not cid or cid not in CUBES: return -700
    H = CUBES[cid].heights
    lx, lz = gx % 64, gz % 64; fx, fz = x - gx, z - gz
    return (H[lz, lx] * (1 - fx) * (1 - fz) + H[lz, lx + 1] * fx * (1 - fz) + H[lz + 1, lx] * (1 - fx) * fz + H[lz + 1, lx + 1] * fx * fz)


def path(corners, radii, sp=SP):
    """A line through `corners` (cells), each inner one rounded to its radius (radii[i] for corners[i]; the ends' unused): straight runs
    and circular arcs, sp cells apart."""
    P = [np.array(c, float) for c in corners]
    out = [P[0]]
    cut = [0.0] * len(P)                    # (how far each corner's arc starts before it and ends after it)
    for i in range(1, len(P) - 1):
        a, b, c = P[i - 1], P[i], P[i + 1]
        u = (b - a) / np.linalg.norm(b - a); v = (c - b) / np.linalg.norm(c - b)
        turn = math.atan2(u[0] * v[1] - u[1] * v[0], float(u @ v))     # (> 0: to the right -- clockwise seen from above)
        r = radii[i]; cut[i] = r * math.tan(abs(turn) / 2)
        p0 = b - u * cut[i]
        centre = p0 + np.array([-u[1], u[0]]) * r * (1 if turn > 0 else -1)
        a0 = math.atan2(p0[1] - centre[1], p0[0] - centre[0])
        m = max(2, int(math.ceil(abs(turn) * r / (sp / 4))))
        out.extend(centre + r * np.array([math.cos(a0 + turn * k / m), math.sin(a0 + turn * k / m)]) for k in range(m + 1))
    out.append(P[-1])
    for i in range(len(P) - 1):
        if cut[i] + cut[i + 1] > np.linalg.norm(P[i + 1] - P[i]) + 1e-6:
            raise ValueError(f'corners {tuple(P[i])} and {tuple(P[i + 1])}: their arcs overlap ({cut[i]:.1f} + {cut[i + 1]:.1f} cells)')
    pts = [out[0]]
    for q in out[1:]:
        if np.linalg.norm(q - pts[-1]) > 1e-6: pts.append(q)
    pts = np.array(pts)
    d = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]
    s = np.arange(0, d[-1] + 1e-9, sp)
    return np.stack([np.interp(s, d, pts[:, 0]), np.interp(s, d, pts[:, 1])], 1)


# ---- the rooms: the rows' door lines (z) and the columns' (x), a room's middle where its two cross
R0, R1, R2, R3 = row(154), row(158), row(162), row(166)    # the bottom row's ... the top row's
C0, C1, C2, C3 = col(151), col(152), col(153), col(154)    # the west column's ... the east column's
# (the start line in the east half of room 154, the grid behind it in front of the palace's doors in the room's east wall -- the way in --
# on the bottom row's door line, all in one cube -- the start's scene; the room's chest stood there: the island moved it to the north
# wall)
START_X = C3 + 3
GRID_Z = R0
BEHIND = (L['entrance'][0] if L.get('entrance') else rooms[154]['x1']) - 12   # (and the camera behind the grid clear of the doors' frame)
if START_X // 64 != BEHIND // 64:
    raise ValueError(f'the grid ({START_X}..{BEHIND}) crosses a cube edge')
TURN = 12                                                   # a turn at a room's middle: its radius (cells)
HALL_JOIN = (C0, hall['z1'] - 36)                           # where every way through the rooms is the route again, up the long hall
GRID = [(BEHIND, GRID_Z)]
GRID_R = [0]
# (the opponents' ways: north at once through room 154's north door, or west first; a turn at each room's middle)
NORTH_FIRST = [(BEHIND, GRID_Z), (C3, GRID_Z)]
NORTH_FIRST_R = [0, 7]
open_routes = [
    # north up the east column (158, 162, 166), west along the top row (165, 164, 163)
    (NORTH_FIRST + [(C3, R3), (C0, R3), HALL_JOIN], NORTH_FIRST_R + [TURN, TURN, 0]),
    # a staircase: west into 153, north into 157, west into 156, north into 160, west into 159, north into 163
    (GRID + [(C2, R0), (C2, R1), (C1, R1), (C1, R2), (C0, R2), HALL_JOIN], GRID_R + [TURN] * 5 + [0]),
    # another: north into 158, west into 157, north into 161, west into 160, north into 164, west into 163
    (NORTH_FIRST + [(C3, R1), (C2, R1), (C2, R2), (C1, R2), (C1, R3), (C0, R3), HALL_JOIN], NORTH_FIRST_R + [TURN] * 6 + [0]),
]
# the arrows on the floor: in each room, in front of its west door and its north door -- the ways on (none out of the west column's west
# walls nor the top row's north walls but room 163's, into the long hall); and up the long hall
arrows = []
for s in SIXTEEN:
    cx, cz = col(s), row(s)
    if s == 154:
        # (the start's room: the grid on its door line -- the arrows where the cars pass, on west, or turning north)
        arrows += [(cx - 16, cz, -1, 0), (cx, cz - 10, 0, -1)]
        continue
    if cx > C0: arrows.append((cx - 12, cz, -1, 0))
    if cz > R3 or s == 163: arrows.append((cx, cz - 12, 0, -1))
arrows += [(C0, hall['z1'] - 40, 0, -1), (C0, (hall['z0'] + hall['z1']) / 2 + 20, 0, -1)]

# ---- the route: the grid, west along the bottom row, north up the west column, the long hall, the round window; then the user's way round
# the roof (above) and the spiral up to the leap through the teleporter on its pillar, and the stretch the leap would come down on
if abs(C0 - wx) > 0.01:
    raise ValueError(f'the west column ({C0}) and the round window ({wx}) are not in line')
# (the lines the road runs along, cells -- where the turrets leave room for it: tools measured them on the island's collision)
ZN = w_out - 23                             # north of the round window: the hook east ...
XHOOK = hall['x1'] + 44                     # ... and south, over the sea east of the long hall's turrets ...
ZGAP = (hall['z0'] + sq_z0) / 2             # ... and west between them, over the long hall's roof
XW = sq_x0 - 7                              # the west side: the long hall's second turret's, then the south-west turret's
ZNS = sq_z0 - 0.5                           # the north-west turret's north side
XV = (sq_x0 + sq_x1) / 2 + 2                # the middle way down, both halves
ZH1 = (sq_z0 + sq_z1) / 2 - 2.5             # the middle way across, west half (the first time through the middle crossing)
ZS = sq_z1 + 3                              # the south side
ZH2 = (sq_z0 + sq_z1) / 2 - 0.5             # the middle way across, east half (the second time, over the first)
XE = sq_x1 + 4                              # the east side
ZA = sq_z0 - 2                              # west to the spiral: its south side
PX, PZ = L['pillar'][0], L['pillar'][1]     # the teleporter's pillar: the leap's line
R_SP = (ZA - PZ) / 2                        # the spiral: from its south side (ZA) to its north (PZ), the leap's line east off its top
CX, CZ = XE - 53, PZ + R_SP
X_LIP = PX - 23                             # the leap's lip: the teleporter some 2.4 s on at the race car's speed
CORNERS = [(wx, ZN), (XHOOK, ZN), (XHOOK, ZGAP), (XW, ZGAP), (XW, ZNS), (XV, ZNS), (XV, ZH1), (XW, ZH1), (XW, ZS), (XV, ZS), (XV, ZH2), (XE, ZH2),
           (XE, ZA), (CX, ZA)]
CORNERS_R = [12, 16, 14, 12, 12, 12, 10, 12, 14, 14, 10, 12, 12, 0]
corners = GRID + [(C0, R0)] + CORNERS
radii = GRID_R + [TURN] + CORNERS_R
main = path(corners, radii)
# (the spiral: from its south point heading west, turning right -- north, east, south -- SPIRAL_LOOPS turns, off its north point heading east)
phi = np.arange(SP / R_SP, SPIRAL_LOOPS * 2 * math.pi + 1e-9, SP / R_SP)
spiral = np.stack([CX - R_SP * np.sin(phi), CZ + R_SP * np.cos(phi)], 1)
top = spiral[-1]
straight = np.stack([np.arange(top[0] + SP, X_LIP + FLIGHT + PAD + 1e-9, SP), np.full(len(np.arange(top[0] + SP, X_LIP + FLIGHT + PAD + 1e-9, SP)), top[1])], 1)
pts = np.concatenate([main, spiral, straight])
n = len(pts)
S = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]
i_sp0 = len(main) - 1; i_sp1 = len(main) + len(spiral) - 1


def nearest(p, lo=0, hi=None):
    hi = n if hi is None else hi
    return lo + int(np.argmin(np.hypot(pts[lo:hi, 0] - p[0], pts[lo:hi, 1] - p[1])))


start = nearest((START_X, GRID_Z), 0, 200)
i_ramp_foot = nearest((wx, w_in + 30)); i_ramp_top = nearest((wx, w_in + 8), i_ramp_foot)
i_window_in = nearest((wx, w_in), i_ramp_foot); i_window_out = nearest((wx, w_out), i_window_in)
i_over = nearest((hall['x1'] + 16, ZGAP), i_window_out)          # over the island: the long hall's east rim, west into the gap
i_roof = nearest((XV, ZNS + 10), i_over)                         # up to RAISE over the roof by the middle way
i_cross1 = nearest((XV - 12, ZH1), i_roof)                       # the first time through the middle crossing: on west from it ...
i_cross2 = nearest((XV, ZH2 + 14), nearest((XV, ZS), i_cross1))  # ... the second, north into it: BRIDGE over the first by then
lip = nearest((X_LIP, PZ), i_sp1); foot = nearest((X_LIP - RUNUP, PZ), i_sp1)
land = nearest((X_LIP + FLIGHT, PZ), lip); land_foot = n - 1
i_hall = nearest(HALL_JOIN, 0, i_ramp_foot)

# ---- heights: the rooms' floor (no road there); the ramp up to the window's sill, level through the window; out of it climbing over the
# sea to GAP_OVER over the roof's rock where it comes over the long hall; up to RAISE over it by the middle way; level through the
# middle crossing; climbing round the south-west turret to BRIDGE more by the second time through the crossing; level to the spiral; up
# the spiral; level down the straight to the take-off ramp; the leap -- up JUMP_APEX and down -- and the stretch of road beyond
def ease(t): return t * t * (3 - 2 * t)
def climb(a, b, h0, h1, shape=ease):
    for i in range(a, b + 1):
        h[i] = h0 + (h1 - h0) * shape((S[i] - S[a]) / max(1e-6, S[b] - S[a]))
def linear(t): return t
h = np.full(n, FLOOR, float)
for i in range(i_ramp_foot, i_ramp_top + 1):
    t = (i - i_ramp_foot) / max(1, i_ramp_top - i_ramp_foot); h[i] = FLOOR + (sill + DECK - FLOOR) * ease(t)
sillH = sill + DECK
gapH = TOP + GAP_OVER
roofH = TOP + RAISE
bridgeH = roofH + BRIDGE
topH = bridgeH + SPIRAL_RISE
h[i_ramp_top:] = sillH
climb(i_window_out, i_over, sillH, gapH)
climb(i_over, i_roof, gapH, roofH)
h[i_roof:i_cross1 + 1] = roofH
climb(i_cross1, i_cross2, roofH, bridgeH)
h[i_cross2:i_sp0 + 1] = bridgeH
climb(i_sp0, i_sp1, bridgeH, topH, linear)
h[i_sp1:foot + 1] = topH
for i in range(foot, lip + 1):
    h[i] = topH + KICK * ease((i - foot) / max(1, lip - foot))
lipH = topH + KICK
h[lip:] = lipH
for i in range(lip, land + 1):
    t = (i - lip) / max(1, land - lip); h[i] = lipH + JUMP_APEX * 4 * t * (1 - t)

# ---- widths (cells from the road's middle to its rail): up to the round window as wide as it allows, wider beyond (10 cells on, clear of
# its wall), narrower over the palace between its turrets -- the middle way's east half narrowest -- each change over 10 cells; the
# grid's places two abreast. (In the rooms there is no road: the width only places the grid and the scenes)
half = np.full(n, HALF_IN)
def among(x, z, x0, z0, x1, z1): return min(1, max(0, min(x - x0, x1 - x, z - z0, z1 - z) / 10))
for i in range(i_window_out, n):
    x, z = pts[i]
    w = HALF_IN + (HALF_OUT - HALF_IN) * min(1, max(0, (S[i] - S[i_window_out] - 10) / 10))
    over = max(among(x, z, sq_x0 - 12, sq_z0 - 12, sq_x1 + 1, sq_z1 + 12), among(x, z, hall['x0'] - 14, hall['z0'] + 6, hall['x1'] + 12, sq_z0))
    w += (HALF_MID - w) * over
    w += (HALF_TIGHT - w) * among(x, z, XV + 14, ZH2 - 12, XE - 10, ZH2 + 12)
    half[i] = w
HALF_GRID = 3.5
for i in range(0, n):
    if i <= start + 6: half[i] = HALF_GRID
    elif i <= start + 26: half[i] = HALF_GRID + (HALF_IN - HALF_GRID) * (i - start - 6) / 20

# ---- the opponents' ways through the rooms, a cell apart (RaceTrackBuilder.RouteThrough)
routes = [path(c, r, 1.0) for c, r in open_routes]

# ---- the palace's own people in the cars' way (2026-10-13, the user: "The retail game has enemies inside the palace, 3 pigs, Stan from
# Time commando, and lots of soldiers with swords, let's add some of these enemies inside as obstacles, perhaps the could chase and attack
# the cars, or throw random things at them"; RACEMOD.CPP guard=): each [kind, which, x, z, floor, face x, face z, leash] -- kind 0 one of
# the Emperor's soldiers, sword out (steps into the road at a car and strikes), 1 a pighead (throws things at the cars; which: the red
# one with the hammer, the brown with the garden tool, the yellow with the broom), 2 Stan (as a soldier). Each stands on the tiles by a
# room's carpets -- they run through the room's middle, a row's and a column's, two bricks (8 cells) wide -- facing where they cross, and
# steps out at most `leash` cells -- far enough to reach a car on the carpet's middle.
CARPET_HALF = 8.0                           # (the carpets four bricks across since 2026-10-13, as the doors)
def free(x, z, r=1.2): return not blocked(x, z, FLOOR, r)
def post(cx, cz, ox, oz, kind, which, leash, room):
    """A guard by the carpets' crossing at (cx, cz): `ox`, `oz` cells off it (the first free spot along the way out, or None)."""
    for k in range(0, 8):
        x, z = cx + ox * (1 + k * 0.12), cz + oz * (1 + k * 0.12)
        if free(x, z) and all(free(cx + (x - cx) * t, cz + (z - cz) * t, 0.6) for t in (0.85, 0.7)):
            fx, fz = cx - x, cz - z; l = math.hypot(fx, fz)
            return [kind, which, round(x, 2), round(z, 2), FLOOR, round(fx / l, 3), round(fz / l, 3), leash]
    print(f'  WARNING: no free spot for a guard in room {room} at ({cx + ox:.1f}, {cz + oz:.1f})')
    return None
guards = []
SOLDIER, PIG, STAN = 0, 1, 2
# (a soldier in every room but the start's, by turns in a corner of its carpets' crossing; the pigheads further off in two rooms on the
# race's way -- 152 on the bottom row, and 163, where every way meets before the long hall: in 157 and 160, on the opponents' other ways
# only, no car came near enough for a throw; soldiers in the long hall, by its carpet. Not Stan nor the pighead with the broom: they race,
# the user's line-up -- CharactersForTrack.txt -- Stan in his time machine, and the road sweeper)
corner = [(1, 1), (-1, 1), (-1, -1), (1, -1)]
for k, s in enumerate(sorted(SIXTEEN)):
    if s == 154: continue
    sx, sz = corner[k % 4]
    g = post(col(s), row(s), sx * (CARPET_HALF + 3), sz * (CARPET_HALF + 3), SOLDIER, 0, 10.0, s)
    if g: guards.append(g)
for which, s in enumerate([152, 163]):
    # (the first of the crossing's four corners with room for him, from a different one in each room)
    ways = [corner[(which + 2 + t) % 4] for t in range(4)]
    sx, sz = next((w for w in ways if free(col(s) + w[0] * (CARPET_HALF + 10), row(s) + w[1] * (CARPET_HALF + 9))), ways[0])
    g = post(col(s), row(s), sx * (CARPET_HALF + 10), sz * (CARPET_HALF + 9), PIG, which, 1.0, s)
    if g: guards.append(g)
# (the long hall: soldiers by its carpet either side, by turns, facing across it)
hx = col(163)
for k, hz in enumerate([hall['z1'] - 52, hall['z1'] - 82, hall['z1'] - 112, hall['z1'] - 142]):
    side = 1 if k % 2 == 0 else -1
    g = post(hx, hz, side * (CARPET_HALF + 4), 0.01, SOLDIER, 0, 10.0, 80)
    if g: guards.append(g)
print('guards:', len(guards), '--', ', '.join(f'{["soldier", "pighead", "Stan"][g[0]]} at ({g[2]:.1f}, {g[3]:.1f})' for g in guards))

# ---- checks
def check(lo, hi, what):
    bad = [i for i in range(lo, hi) if blocked(pts[i][0], pts[i][1], h[i] - DECK, max(0.5, half[i] - 1.25 + CAR_BOX))]
    if bad: print(f'  {what}: {len(bad)} points blocked, e.g. {[tuple(np.round(pts[i], 1)) for i in bad[:8]]}')
    else: print(f'  {what}: clear')
    return bad
def check_floor(line, what):
    bad = [p for p in line if blocked(p[0], p[1], FLOOR, CAR_HALF)]
    if bad: print(f'  {what}: {len(bad)} points blocked, e.g. {[tuple(np.round(p, 1)) for p in bad[:8]]}')
    else: print(f'  {what}: clear ({len(line)} points)')
    return bad
print('route', n, 'points,', round(S[-1]), 'cells; start', start, 'the hall', i_hall, 'window', i_window_in, i_window_out, 'the leap', lip, land, land_foot)
check_floor(pts[:i_ramp_foot], 'the rooms and the hall (the route)')
for k, rt in enumerate(routes): check_floor(rt, f'the rooms (way {k + 1})')
check(i_ramp_foot, i_window_out + 1, 'the ramp and the window')
check(i_window_out, lip + 1, 'the road round the turrets')
grade = np.abs(np.diff(h)) / (SP * 512)
print('steepest: the ramp to the window %.2f, the climb to the roof %.3f, round the south-west turret %.3f, the spiral %.3f, the take-off ramp %.3f' %
      (grade[i_ramp_foot:i_window_in].max(), grade[i_window_out:i_roof].max(), grade[i_cross1:i_cross2].max(), grade[i_sp0:i_sp1].max(), grade[foot:lip].max()))
# (where the road passes over itself -- the middle crossing, the spiral's turns: the least height between the two, its decks and a car)
over_min = (1e9, None)
for i in range(i_window_out, lip, 4):
    d = np.hypot(pts[i:lip, 0] - pts[i][0], pts[i:lip, 1] - pts[i][1])
    for j in np.nonzero((d < half[i] + half[i:lip]) & (S[i:lip] - S[i] > 60))[0][::4]:
        k = i + j; dh = abs(h[k] - h[i])
        if dh < over_min[0]: over_min = (dh, (tuple(np.round(pts[i], 1)), round(h[i]), round(h[k])))
print('the road over itself: the least height between two of its ways %.0f at %s' % over_min)
if over_min[0] < 2800: print('  WARNING: under 2,800 -- a car would hit the deck over it')
low = min((h[i] - DECK - max(ground(pts[i][0], pts[i][1]), -700), i) for i in range(i_window_out, lip))
print('the road round the turrets: the least clearance over the ground %.0f at %s (height %.0f)' % (low[0], tuple(np.round(pts[low[1]], 1)), h[low[1]]))
# (the leap over the ground: the least clearance under it, and under the stretch of road beyond -- the hill's arms either side)
low = min((h[i] - DECK - max(ground(pts[i][0] + dx, pts[i][1]) for dx in (-HALF_OUT - 3, 0, HALF_OUT + 3)), i) for i in range(lip, n))
print('the leap: lip %.0f at %s, its top %.0f, down at %s; %.1f cells; the least clearance over the ground under it and beyond %.0f at %s' %
      (h[lip], tuple(np.round(pts[lip], 1)), h[lip:land + 1].max(), tuple(np.round(pts[land], 1)), S[land] - S[lip], low[0], tuple(np.round(pts[low[1]], 1))))
print('the parts: the rooms and the hall %.0f cells, the ramp and the window %.0f, the road round the turrets %.0f, the spiral %.0f, the straight to the lip %.0f, the leap %.0f, the road beyond %.0f' %
      (S[i_ramp_foot], S[i_window_out] - S[i_ramp_foot], S[i_sp0] - S[i_window_out], S[i_sp1] - S[i_sp0], S[lip] - S[i_sp1], S[land] - S[lip], S[-1] - S[land]))
print('heights: over the long hall %.0f, round the turrets %.0f, the bridge %.0f, the spiral\'s top %.0f, the lip %.0f; the teleporter\'s ring %.0f, the car there %.0f' %
      (gapH, roofH, bridgeH, topH, lipH, L['pillar'][3] + 2740, lipH + JUMP_APEX * 4 * (23 / FLIGHT) * (1 - 23 / FLIGHT)))
print('the opponents\' ways through the rooms: ' + ', '.join('%.0f cells' % (len(rt) - 1) for rt in routes) + ' (the route %.0f to the same place)' % S[i_hall])
print('about %.0f s to the lip at the top speed (34 km/h, 9.4 cells a second)' % (S[lip] / 9.44))

plan = {
    'originCellX': 0, 'originCellZ': 0,
    'points': [[round(float(x), 3), round(float(z), 3)] for x, z in pts],
    'heights': [round(float(v), 1) for v in h],
    'open': True, 'start': start, 'finish': lip,
    'maxGrade': 2.0,
    'asphaltHalf': 3.0, 'curbHalf': 3.5, 'vergeHalf': 4.0, 'blend': 2.0,
    'raised': [0, n - 1], 'raisedHalf': HALF_OUT,
    'raisedHalfs': [round(float(v), 2) for v in half],
    # (no road through the rooms and up the long hall to the ramp: the cars on the floor, where they like; nor where the leap comes down --
    # never reached: a win's teleporter takes the car in mid-air, a loss's race again from the grid starts before it lands -- a stretch of
    # road on a pier out in the haze)
    'bare': [[0, i_ramp_foot - 1], [land, n - 1]],
    'openAreas': [[sq_x0, sq_z0, sq_x1, sq_z1, FLOOR], [hall['x0'], hall['z0'], hall['x1'] + 1, hall['z1'] + 1, FLOOR]],
    'floorArrows': [[round(float(x), 2), round(float(z), 2), FLOOR, dx, dz] for x, z, dx, dz in arrows],
    'openRoutes': [[[round(float(x), 2), round(float(z), 2)] for x, z in rt] for rt in routes],
    'guards': guards,
    # (the leap off the roof's edge over the harbour: its ramp the runway's rise; the camera behind the car)
    'arcJumps': [[foot, lip, land, land_foot, 0]],
    # (the fireplace made a teleporter: into room 151's, out of room 159's -- the island's layout)
    'teleports': L.get('teleports') or [],
    # (the camera rides the road behind the car, low -- 9 cells behind, 2,100 over the road, looking 7 cells ahead at a point 1,000 over
    # it, its sight line to the car's middle 500 over the road; in the rooms, where there is no road, it follows the car's own trail, as
    # low -- RACEMOD.CPP OpenCamera)
    'railCamera': [9, 2100, 7, 500, 1000],
    'themes': [{'from': 0, 'to': i_window_out, 'theme': 'carpet'}, {'from': i_window_out, 'to': n - 1, 'theme': 'palace'}],
    # (the palace -- its rooms, roofs, furniture and domes -- stays where it is under and beside the road: every decor of the island's own
    # bodies on, PALACE.OBL's from 151; and nothing is cut into the ground)
    'keepAbove': 0,
}
json.dump(plan, open(OUT, 'w'))
print('wrote', OUT)
