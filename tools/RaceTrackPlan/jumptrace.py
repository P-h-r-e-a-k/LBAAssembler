"""Drives the built storm track (E:\\dump\\TEMP\\rt_cstorm) with the test pilot at a given frame time and reports the jump: when Twinsen is
switched to the flight's movement (move 12, moved by his animation), how long until the flight animation (200) is really playing, and
what animation was holding it off (genanim / flaganim: 2 is ANIM_ALL_THEN, which nothing interrupts).
    python jumptrace.py <dt ms> [car file] [look cells]"""
import re, subprocess, sys, os
dt = int(sys.argv[1])
car = sys.argv[2] if len(sys.argv) > 2 else r'E:\dump\TEMP\cstorm\car_storm_auto.txt'
look = sys.argv[3] if len(sys.argv) > 3 else '5'
ENG = r'E:\dump\LBAAssembler\native\lba2-classic-community\out\build\windows_ucrt64_static\SOURCES\lba2cc.exe'
user = r'E:\dump\TEMP\cstorm\user_jt_%d' % dt
os.makedirs(user, exist_ok=True)
ticks = int(40000 / dt) + 100          # two laps' worth
env = dict(os.environ, LBA2_RACETRACK_FILE=car)
args = [ENG, '--headless', '--no-audio', '--game-dir', r'E:\dump\TEMP\rt_cstorm', '--user-dir', user, '--no-autosave', '--resolution', '640x480',
        '--fixed-dt', str(dt), '--exec-at', '4', 'skipmodals 1', '--exec-at', '5', 'vargame 74 3', '--exec-at', '6', 'cube 42',
        '--exec-at', '40', 'teleport 24897 352 2540 4013', '--exec-at', '60', 'input action 5', '--exec-at', '90', 'autodrive 1 ' + look,
        '--exec-at', '91', 'objtrace 0', '--tick', str(ticks), '--exit']
out = subprocess.run(args, capture_output=True, text=True, env=env, timeout=600).stdout
rx = re.compile(r'\[obj\] t=(\d+) obj=0 pos=(-?\d+),(-?\d+),(-?\d+) .*? anim=(-?\d+) genanim=(-?\d+) flaganim=(-?\d+) frame=(-?\d+) track=(-?\d+) label=(-?\d+) comport=(-?\d+) move=(-?\d+) flags=(\d+)')
rows = [tuple(int(v) for v in m.groups()) for m in rx.finditer(out)]
laps = re.findall(r'\[racemod\] lap (\d+) in ([\d.]+) s', out)
print(f'dt {dt}: {len(rows)} traced frames; laps {laps}')
i = 0
jumps = 0
while i < len(rows):
    if rows[i][11] == 12:
        start = i
        while i < len(rows) and rows[i][11] == 12: i += 1
        seg = rows[start:i]
        first200 = next((k for k, r in enumerate(seg) if r[5] == 200), None)
        held = seg[:first200] if first200 is not None else seg
        holders = sorted({(r[5], r[6]) for r in held})
        t0 = seg[0][0]
        print(f'  jump {jumps + 1}: move 12 for {len(seg)} frames from t={t0} at ({seg[0][1]},{seg[0][2]},{seg[0][3]}); '
              f'flight anim after {first200 if first200 is not None else "never"} frames ({(seg[first200][0] - t0) if first200 is not None else "-"} ms); '
              f'held by (genanim, flaganim) {holders}; ends at ({seg[-1][1]},{seg[-1][2]},{seg[-1][3]})')
        jumps += 1
    else:
        i += 1
if jumps == 0: print('  no jump started')
