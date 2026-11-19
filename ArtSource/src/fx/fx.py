"""FX sprites. Pivot = centre. No outline on light effects (they're emissive), debris outlined."""
import sys, os, math
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "tools"))
from px import *
from pose import disc, thick_line, polyline


def P(s, w):
    return parse(s, pad=w)


# ------------------------------------------------------------------ hit spark 16x16
HIT = [P("""
................
................
................
................
.......x........
.......x........
......xwx.......
....xxwllwxx....
......xwx.......
.......x........
.......x........
................
................
................
................
................
""", 16), P("""
................
................
....x.....E.....
.....x...x......
......x.x.......
.......w........
...x..wlw..x....
.xxwwwlllwwwxx..
...x..wlw..x....
.......w........
......x.x.......
.....x...x......
....E.....x.....
................
................
................
""", 16), P("""
................
...w........E...
................
....w.....x.....
................
.......x........
..x....w....x...
w......w......w.
..x....w....x...
.......x........
................
....x.....w.....
................
...E........w...
................
................
""", 16), P("""
..v..........D..
................
................
................
................
................
................
v..............v
................
................
................
................
................
................
..D..........v..
................
""", 16)]

# ------------------------------------------------------------------ crit burst 32x32


def crit_frame(t):
    img = blank(32, 32)
    c = 15.5
    if t == 0:
        disc(img, c, c, 6, "x"); disc(img, c, c, 4.5, "l")
        for a in range(0, 360, 45):
            r = math.radians(a)
            polyline(img, [(c + math.cos(r) * 6, c + math.sin(r) * 6), (c + math.cos(r) * 10, c + math.sin(r) * 10)], "w")
        return img
    if t == 1:
        for a in range(0, 360, 45):
            r = math.radians(a + 22.5)
            L = 15 if a % 90 == 0 else 11
            thick_line(img, c, c, c + math.cos(r) * L, c + math.sin(r) * L, "xw" if a % 90 == 0 else "w")
        for a in range(0, 360, 15):
            r = math.radians(a)
            put(img, int(round(c + math.cos(r) * 9)), int(round(c + math.sin(r) * 9)), "F" if a % 30 else "x")
        disc(img, c, c, 4, "l"); disc(img, c, c, 2, "x")
        return img
    if t == 2:
        for a in range(0, 360, 45):
            r = math.radians(a + 22.5)
            L0, L1 = (8, 15) if a % 90 == 0 else (7, 12)
            polyline(img, [(c + math.cos(r) * L0, c + math.sin(r) * L0), (c + math.cos(r) * L1, c + math.sin(r) * L1)], "x" if a % 90 == 0 else "w")
        for a in range(0, 360, 12):
            r = math.radians(a)
            put(img, int(round(c + math.cos(r) * 12)), int(round(c + math.sin(r) * 12)), "F" if (a // 12) % 2 else "v")
        disc(img, c, c, 2.2, "x"); put(img, 15, 15, "l"); put(img, 16, 16, "l")
        return img
    if t == 3:
        for a in range(0, 360, 20):
            r = math.radians(a + 10)
            put(img, int(round(c + math.cos(r) * 14)), int(round(c + math.sin(r) * 14)), "F" if (a // 20) % 2 else "v")
        for a in range(0, 360, 90):
            r = math.radians(a + 22.5)
            polyline(img, [(c + math.cos(r) * 13, c + math.sin(r) * 13), (c + math.cos(r) * 15, c + math.sin(r) * 15)], "w")
        put(img, 15, 15, "w"); put(img, 16, 16, "v")
        return img
    for a in range(0, 360, 40):
        r = math.radians(a + 25)
        put(img, int(round(c + math.cos(r) * 15)), int(round(c + math.sin(r) * 15)), "t" if (a // 40) % 2 else "s")
    return img


# ------------------------------------------------------------------ debris 8x8 (crystal chips)
DEBRIS = {
    "a": P("""
........
...11...
..1EC1..
.1ECBB1.
.1CBBA1.
..1BA1..
...11...
........
""", 8),
    "b": P("""
........
.11.....
.1E11...
..1CC11.
..1CBBA1
...1BAA1
....111.
........
""", 8),
    "c": P("""
........
........
..1111..
.1DECB1.
..1CBA1.
...1A1..
....1...
........
""", 8),
    "d": P("""
........
....1...
...1E1..
..1CC1..
..1CB1..
.1CBA1..
.1BA1...
..11....
""", 8),
}

# ------------------------------------------------------------------ box poof 24x24
PUFF_S = P("""
..kk..
.kllk.
kljjjk
.kjjk.
..kk..
""", 6)
PUFF_M = P("""
...kkk...
.kkllkk..
kllljjjk.
kljjjjjjk
.kjjijjk.
..kjiij..
...kkk...
""", 9)


def poof(t):
    img = blank(24, 24)
    c = 12
    if t == 0:
        disc(img, c, c + 2, 4, "k"); disc(img, c - 1, c + 1, 2.5, "l")
        return img
    pts = [(-1, 0), (1, 0), (0, -1), (-0.8, -0.7), (0.8, -0.7), (-0.6, 0.6), (0.6, 0.6)]
    spread = [0, 4, 7, 9, 10][t]
    for i, (dx, dy) in enumerate(pts):
        x = c + dx * spread
        y = c + 2 + dy * spread - t
        spr = PUFF_M if (t <= 2 and i < 3) else PUFF_S
        if t >= 3:
            spr = recolor(spr, {"l": "k", "k": "j", "j": "i"})
        if t >= 4:
            spr = recolor(spr, {"k": "j", "j": "i", "i": "h"})
            if i % 2:
                continue
        blit(img, spr, int(round(x - spr.shape[1] / 2)), int(round(y - spr.shape[0] / 2)))
    return img


# ------------------------------------------------------------------ nut sparkle 8x8
NUT_SP = [P("""
........
........
........
...x....
........
........
........
........
""", 8), P("""
........
........
...x....
..xlx...
...x....
........
........
........
""", 8), P("""
...x....
...w....
...x....
xwxlxwx.
...x....
...w....
...x....
........
""", 8), P("""
........
...w....
........
.w.x.w..
........
...w....
........
........
""", 8)]

# ------------------------------------------------------------------ level up 32x32
CHEV = P("""
....11....
...1xx1...
..1xwwx1..
.1xw11wx1.
1xw1..1wx1
1w1....1w1
.1......1.
""", 10)


def level_up(t):
    img = blank(32, 32)
    # light column
    if 1 <= t <= 4:
        h = [0, 12, 24, 30, 26][t]
        for y in range(30 - h, 31):
            for x in range(13, 19):
                d = abs(x - 15.5)
                if d < 1.0:
                    put(img, x, y, "l" if t < 3 else "x")
                elif d < 2.0:
                    put(img, x, y, "x" if t < 4 else "w")
                elif d < 3.0 and y > 30 - h * 0.6:
                    put(img, x, y, "w" if t < 4 else "v")
    # ground ring
    if t <= 2:
        rx = [5, 9, 12][t]
        for a in range(0, 360, 10):
            r = math.radians(a)
            put(img, int(round(15.5 + math.cos(r) * rx)), int(round(28 + math.sin(r) * rx * 0.3)), "w" if t < 2 else "v")
    # chevrons rising
    for k, y0 in enumerate([24, 30]):
        y = y0 - t * 4
        if t >= 1 and -7 < y < 32:
            ch = CHEV if t < 5 else recolor(CHEV, {"x": "w", "w": "v"})
            blit(img, ch, 11, y)
    # sparkles
    sp = [(6, 22 - t * 3, "x"), (25, 18 - t * 3, "w"), (9, 12 - t * 2, "l"), (23, 26 - t * 4, "x")]
    for (x, y, k) in sp:
        if t >= 1 and 0 <= y < 32:
            put(img, x, y, k)
            if t in (2, 3):
                put(img, x - 1, y, "w"); put(img, x + 1, y, "w"); put(img, x, y - 1, "w"); put(img, x, y + 1, "w")
    return img


# ------------------------------------------------------------------ weakpoint 24x24 (v0.2)
# A glowing crack that opens on the nexus crystal: pale-cyan core, cyan rim, magenta glow, and a deep
# violet outline so it reads on top of the purple crystal. A dashed target ring pulses in `loop`
# (clickable at a glance, ~19px across). Pivot = centre (12, 12).
WP_C = (12, 12)
WP_MAIN = [(6, 18), (7, 16), (9, 15), (9, 13), (11, 12), (11, 10), (13, 9), (15, 8), (16, 6), (18, 5)]
WP_BRANCH = [[(11, 12), (13, 13), (14, 15), (16, 15)], [(9, 15), (7, 13), (6, 13)], [(13, 9), (12, 7), (11, 6)]]


def _dil(m, diag=True):
    g = m.copy()
    g[1:] |= m[:-1]; g[:-1] |= m[1:]; g[:, 1:] |= m[:, :-1]; g[:, :-1] |= m[:, 1:]
    if diag:
        g[1:, 1:] |= m[:-1, :-1]; g[1:, :-1] |= m[:-1, 1:]; g[:-1, 1:] |= m[1:, :-1]; g[:-1, :-1] |= m[1:, 1:]
    return g


def _poly_mask(pts, frac=1.0):
    """core mask of a polyline, optionally only the middle `frac` of it."""
    img = blank(24, 24)
    segs = list(zip(pts, pts[1:]))
    n = len(segs)
    keep = range(int(round(n * (1 - frac) / 2)), n - int(round(n * (1 - frac) / 2)))
    for i in keep:
        (x0, y0), (x1, y1) = segs[i]
        line(img, x0, y0, x1, y1, "E")
    return img >= 0


def _crack(core, core_k="E", rim_k="D", glow_k=None, outline_k="A", glow_mask=None):
    """thin crack: 1px core, 1px rim (4-neighbours), checkered magenta glow, deep outline."""
    img = blank(24, 24)
    rim = _dil(core, diag=False) & ~core
    glow = (_dil(core | rim, diag=False) & ~(core | rim)) if glow_k else np.zeros_like(core)
    yy, xx = np.mgrid[0:24, 0:24]
    glow &= (yy + xx) % 2 == 0
    if glow_mask is not None:
        glow &= glow_mask
    body = core | rim | glow
    if outline_k:
        img[_dil(body, diag=False) & ~body] = K[outline_k]
    img[glow] = K[glow_k] if glow_k else T
    img[rim] = K[rim_k]
    img[core] = K[core_k]
    return img


def _ring(img, r, key, phase=0, dash=3, gap=2):
    """dashed circle: `dash` lit steps then `gap` empty steps, rotated by `phase` steps."""
    cx, cy = WP_C
    n = max(24, int(2 * math.pi * r))
    for i in range(n):
        if (i + phase) % (dash + gap) >= dash:
            continue
        a = 2 * math.pi * i / n
        x, y = int(round(cx + math.cos(a) * r)), int(round(cy + math.sin(a) * r))
        if 0 <= x < 24 and 0 <= y < 24 and img[y, x] < 0:
            put(img, x, y, key)
    return img


def _rays(img, length, key):
    cx, cy = WP_C
    for dx, dy in ((1, -1), (-1, 1), (1, 1), (-1, -1)):
        for t in range(3, 3 + length):
            x, y = cx + dx * t, cy + dy * t
            if img[y, x] < 0:
                put(img, x, y, key)
    return img


def _shard(img, x, y, size):
    """small crystal chip: violet body, cyan lit edge, deep outline."""
    x, y = int(round(x)), int(round(y))
    if size >= 2:
        chip = P("""
.AA.
AECA
ACBA
.AA.
""", 4)
    else:
        chip = P("""
C
""", 1)
    blit(img, chip, x - chip.shape[1] // 2, y - chip.shape[0] // 2)
    return img


def weakpoint():
    main_full = _poly_mask(WP_MAIN)
    full = main_full.copy()
    for b in WP_BRANCH:
        full |= _poly_mask(b)
    near_main = _dil(_dil(main_full))
    tags = {}
    # spawn: point of light -> crack splits -> light leaks -> target ring forms
    f0 = blank(24, 24)
    for (x, y, k) in ((12, 12, "l"), (11, 12, "E"), (13, 12, "E"), (12, 11, "E"), (12, 13, "E"),
                      (10, 12, "D"), (14, 12, "D"), (12, 10, "D"), (12, 14, "D")):
        put(f0, x, y, k)
    f1 = _crack(_poly_mask(WP_MAIN, 0.5), "E", "D")
    f2 = _crack(full, "l", "D", "F", glow_mask=near_main)
    _rays(f2, 3, "F")
    f3 = _crack(full, "E", "D", "F", glow_mask=near_main)
    _ring(f3, 9, "D", 0)
    tags["spawn"] = [f0, f1, f2, f3]
    # loop: slow pulse (brightness up/down over 6 frames), ring breathes 8<->9 and creeps around
    bright = [0, 1, 2, 2, 1, 0]
    radius = [8, 8, 9, 9, 9, 8]
    loop = []
    for i, b in enumerate(bright):
        f = _crack(full, "l" if b == 2 else "E", "D", "F" if b >= 1 else None,
                   glow_mask=near_main if b < 2 else None)
        if b == 2:
            _rays(f, 2, "F")
        _ring(f, radius[i], "F" if b >= 1 else "C", phase=i)
        loop.append(f)
    tags["loop"] = loop
    # break: white-hot flash -> crystal shards fly out with an expanding ring -> sparks fade
    brk = []
    flash = _crack(_dil(full), "l", "E", "D")
    _rays(flash, 5, "E")
    brk.append(flash)
    dirs = [(math.cos(a), math.sin(a)) for a in [k * math.pi / 3 + 0.3 for k in range(6)]]
    for t, (r_sh, r_ring, size, crack_k) in enumerate(((6, 10, 2, "E"), (8.5, 11, 2, "D"), (10, None, 1, "C"),
                                                        (11.2, None, 1, None), (None, None, 0, None))):
        f = blank(24, 24)
        if crack_k:
            f = _crack(full if t == 0 else _poly_mask(WP_MAIN, 0.6), crack_k, "C" if t else "D", outline_k="A")
        if r_ring:
            _ring(f, r_ring, "E" if t == 0 else "D", phase=t, dash=4, gap=1)
        if r_sh:
            for k, (dx, dy) in enumerate(dirs):
                rr = r_sh - (k % 2) * 1.2
                _shard(f, WP_C[0] + dx * rr, WP_C[1] + dy * rr, size)
        # sparks between the shards
        for k, (dx, dy) in enumerate(dirs):
            rr = (r_sh or 11.5) + 1.5
            a = math.atan2(dy, dx) + 0.5
            x, y = int(round(WP_C[0] + math.cos(a) * rr)), int(round(WP_C[1] + math.sin(a) * rr))
            if 0 <= x < 24 and 0 <= y < 24 and f[y, x] < 0 and (t < 4 or k % 2 == 0):
                put(f, x, y, "F" if t % 2 else "E")
        brk.append(f)
    tags["break"] = brk
    # fade: glow dies, crack seals back into crystal colours, two last motes
    fd0 = _crack(full, "D", "C", None)
    fd1 = _crack(_poly_mask(WP_MAIN, 0.5), "C", "B", None)
    fd2 = blank(24, 24)
    put(fd2, 11, 12, "C"); put(fd2, 13, 10, "B")
    tags["fade"] = [fd0, fd1, fd2]
    return tags


def build():
    out = []
    a = Asset("Assets/Art/FX/fx_hit_spark.aseprite", 16, 16, ["fx"]); a.tag("play", HIT, 50); out.append(a)
    a = Asset("Assets/Art/FX/fx_crit_burst.aseprite", 32, 32, ["fx"]); a.tag("play", [crit_frame(t) for t in range(5)], 50); out.append(a)
    a = Asset("Assets/Art/FX/fx_debris.aseprite", 8, 8, ["fx"])
    for k in "abcd":
        a.tag(k, [DEBRIS[k]], 100)
    out.append(a)
    a = Asset("Assets/Art/FX/fx_box_poof.aseprite", 24, 24, ["fx"]); a.tag("play", [poof(t) for t in range(5)], 60); out.append(a)
    a = Asset("Assets/Art/FX/fx_nut_sparkle.aseprite", 8, 8, ["fx"]); a.tag("play", NUT_SP, 80); out.append(a)
    a = Asset("Assets/Art/FX/fx_level_up.aseprite", 32, 32, ["fx"]); a.tag("play", [level_up(t) for t in range(6)], 60); out.append(a)
    wp = weakpoint()
    a = Asset("Assets/Art/FX/fx_weakpoint.aseprite", 24, 24, ["fx"])
    for name, ms in (("spawn", 60), ("loop", 90), ("break", 50), ("fade", 80)):
        a.tag(name, wp[name], ms)
    out.append(a)
    for x in out:
        x.preview_bg = (70, 66, 72)
    return out


if __name__ == "__main__":
    for a in build():
        if "--export" in sys.argv:
            export(a, "fx", s=6)
        else:
            write_previews(a, "fx", s=6)
