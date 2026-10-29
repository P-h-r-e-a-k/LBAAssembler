# Island CX Mk2's plan: Island CX's (docs/racetrack/cxtower_track_plan.json, tools/RaceTrackPlan/cxtower_design.py) twice its size --
# the island is built at 1:1 and made twice as big every way, heights too (RaceTrackIsland.CxTowerMk2: Scale 2, IslandScaler), about the
# corner of its cube, which is the plan's origin (448, 448): every place twice as far from it, every height and width twice, a point put
# between each two (half a cell apart, as Island CX's) and the stretches' point numbers twice. The user, 2026-10-29: "Let's recreate our
# CXTower.ile into CXTowerMk2.ile but let's double the size of it, we'll keep both islands for now, then select one to keep later".
#   python tools/RaceTrackPlan/cxtower_mk2.py  -> docs/racetrack/cxtowermk2_track_plan.json
import json, os

K = 2
here = os.path.dirname(os.path.abspath(__file__))
root = os.path.normpath(os.path.join(here, '..', '..'))
src = json.load(open(os.path.join(root, 'docs', 'racetrack', 'cxtower_track_plan.json'), encoding='utf-8'))

def xy(p): return [round(p[0] * K, 3), round(p[1] * K, 3)] + list(p[2:])
def hs(v): return [round(h * K, 1) for h in v]
def ws(v): return [round(w * K, 3) for w in v]

out = dict(src)
out['points'] = [xy(p) for p in src['points']]
out['heights'] = hs(src['heights'])
for key in ('raisedHalf', 'asphaltHalf', 'curbHalf', 'edgeMargin'):
    out[key] = round(src[key] * K, 3)
for key in ('raisedHalfs', 'lineHalfs'):
    out[key] = ws(src[key])
# (a slope as steep: maxGrade as it is)
out['checkpointTweaks'] = [dict(t, at=[round(t['at'][0] * K, 3), round(t['at'][1] * K, 3)]) if 'at' in t else t for t in src.get('checkpointTweaks', [])]
# pit spots: x, z, the way (a unit vector), the height
out['pitSpots'] = [[round(p[0] * K, 3), round(p[1] * K, 3), p[2], p[3], round(p[4] * K, 1)] for p in src['pitSpots']]
# tunnels: roofs over the road, the sunk ones' top and eave, the hall's ceiling and the wall over a free end -- heights and widths twice
tunnels = []
for t in src['tunnels']:
    t = dict(t)
    for key in ('roof', 'top', 'underGround', 'closeAbove', 'ceiling'):
        if key in t: t[key] = round(t[key] * K, 1)
    if 'eave' in t: t['eave'] = round(t['eave'] * K, 3)
    tunnels.append(t)
out['tunnels'] = tunnels
out['gasMonsters'] = [dict(m, every=m['every'] * K, out=m['out'] * K, floor=m['floor'] * K) for m in src['gasMonsters']]
out['flyers'] = [dict(f, out=f['out'] * K, height=f['height'] * K, floor=f['floor'] * K) for f in src['flyers']]
branches = []
for b in src.get('branches', []):
    b = dict(b)
    b['points'] = [xy(p) for p in b['points']]
    if 'heights' in b: b['heights'] = hs(b['heights'])
    if 'halfs' in b: b['halfs'] = ws(b['halfs'])
    branches.append(b)
out['branches'] = branches

# Half a cell apart again, as Island CX's points are (the road's own spacing): twice as far apart, the raised road (the plan's lap from its
# first point to its last) left out the road's point between the last and the first, which was built as a road on the ground -- the ground
# brought up to the deck there, with the gas's code (the car "into the sea" on the deck). A point between each two, the lap's and the
# branches' lists along with them, and every point number twice.
def between(v, closed):
    o = []
    for i, a in enumerate(v):
        o.append(a)
        if i + 1 < len(v) or closed:
            b = v[(i + 1) % len(v)]
            o.append([round((x + y) / 2, 3) for x, y in zip(a, b)] if isinstance(a, list) else round((a + b) / 2, 3))
    return o
n = len(out['points'])
for key in ('points', 'heights', 'raisedHalfs', 'lineHalfs'):
    if len(out[key]) != n: raise SystemExit(f'{key}: {len(out[key])} values for {n} points')
    out[key] = between(out[key], True)
out['heights'] = [round(h, 1) for h in out['heights']]
if src['raised'] != [0, n - 1]: raise SystemExit(f"raised {src['raised']}: only the whole lap is mapped")
out['raised'] = [0, 2 * n - 1]
out['under'] = [[2 * a, 2 * b] for a, b in src['under']]
for group in ('tunnels', 'gasMonsters', 'flyers'):
    out[group] = [dict(t, **{'from': 2 * t['from'], 'to': 2 * t['to']}) for t in out[group]]
for b in branches:
    m = len(b['points'])
    for key in ('points', 'heights', 'halfs'):
        if key in b:
            if len(b[key]) != m: raise SystemExit(f'branch {key}: {len(b[key])} values for {m} points')
            b[key] = between(b[key], False)
    if 'heights' in b: b['heights'] = [round(h, 1) for h in b['heights']]
    b['from'], b['to'] = 2 * b['from'], 2 * b['to']
    if 'hidden' in b: b['hidden'] = [[2 * x, 2 * y] for x, y in b['hidden']]
    if set(b) - {'points', 'heights', 'halfs', 'from', 'to', 'hidden'}: raise SystemExit(f'a branch has keys this script does not map: {sorted(b)}')
for t in src['tunnels']:
    if set(t) - {'from', 'to', 'roof', 'sunk', 'top', 'eave', 'underGround', 'closeAbove', 'ceiling', 'pieces'}: raise SystemExit(f'a tunnel has keys this script does not map: {sorted(t)}')

# The start, the grid and the pits in one cube: twice as big, the start straight runs over the edge between the island's new cubes (x 64,
# the middle of Island CX's one cube) -- the line at 60, Twinsen's spot behind the car on pole past the edge, which a scene's record can't
# hold, and two of the three pits past it, which the build leaves out. The line 12 cells back, at 72 (the straight's start, the grid round
# the bend behind it), the pits the first two as they are and the third 4.5 cells behind the second (the cars aren't bigger: Island CX's
# own spacing), its height the pit lane's there.
def lane_height(x):
    pts, hts = branches[0]['points'], branches[0]['heights']
    for i in range(len(pts) - 1):
        (x0, z0), (x1, z1) = pts[i][:2], pts[i + 1][:2]
        if z0 == z1 and min(x0, x1) <= x <= max(x0, x1):
            return round(hts[i] + (hts[i + 1] - hts[i]) * (x - x0) / (x1 - x0), 1)
    raise SystemExit(f'the pit lane has no straight at x {x}')
START_X = 72.0
out['start'] = min(range(len(out['points'])), key=lambda i: abs(out['points'][i][0] - START_X) + abs(out['points'][i][1] - out['points'][2 * src['start']][1]))
second = out['pitSpots'][1]
third_x = round(second[0] - 4.5, 3)
out['pitSpots'][2] = [third_x, second[1], second[2], second[3], lane_height(third_x)]

known = {'originCellX', 'originCellZ', 'points', 'heights', 'maxGrade', 'raised', 'raisedHalf', 'asphaltHalf', 'curbHalf', 'raisedHalfs', 'lineHalfs',
         'checkpointTweaks', 'start', 'pitSpots', 'raisedCut', 'edgeMargin', 'keepBodies', 'under', 'tunnels', 'gasMonsters', 'flyers', 'branches'}
left = set(src) - known
if left: raise SystemExit(f'the plan has keys this script does not scale: {sorted(left)}')
path = os.path.join(root, 'docs', 'racetrack', 'cxtowermk2_track_plan.json')
open(path, 'w', encoding='utf-8', newline='\n').write(json.dumps(out, separators=(', ', ': ')))
print(path, len(out['points']), 'points')
