"""Nexus — Assets/Art/Nexus/nexus.aseprite (80x96, pivot bottom-centre).
Alien crystal pillar driven into the ground at an angle, junk absorbed into it.
Visible body is kept inside y>=14 so the 96px window never clips the tip
(canvas bottom = floor line)."""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "tools"))
from px import *
from pose import *

W, H = 80, 96
NEON = set(K[c] for c in "ABCDEF")

# ------------------------------------------------------------------ geometry
TIP = (31, 17)
# main pillar facets (polygon, key)
MAIN = [
    ([(25, 90), (35, 92), (28, 37), (19, 38)], "C"),          # left face (lit)
    ([(19, 38), (28, 37), TIP], "C"),                          # left termination
    ([(35, 92), (49, 92), (42, 37), (28, 37)], "B"),          # front face
    ([(28, 37), (42, 37), TIP], "C"),                          # front termination
    ([(49, 92), (57, 90), (51, 38), (42, 37)], "A"),          # right face (shadow)
    ([(42, 37), (51, 38), TIP], "B"),                          # right termination
]
LEFT_XTAL = [
    ([(15, 90), (23, 91), (10, 61), (5, 56)], "C"),
    ([(23, 91), (30, 88), (14, 62), (10, 61), (5, 56)], "B"),
]
RIGHT_XTAL = [
    ([(50, 91), (58, 92), (66, 66), (73, 61)], "B"),
    ([(58, 92), (65, 90), (69, 68), (66, 66), (73, 61)], "A"),
]
BACK_XTAL = [  # pokes out from behind the pillar on the right
    ([(50, 62), (53, 52), (64, 43)], "C"),
    ([(50, 62), (64, 43), (55, 63)], "B"),
]
FRONT_XTAL = [
    ([(40, 94), (45, 95), (49, 81), (52, 77)], "C"),
    ([(45, 95), (51, 94), (52, 82), (49, 81), (52, 77)], "A"),
]

# lit edges (highlight lines) and glints
EDGE_HI = [  # bright ridges facing the light
    [(19, 38), (28, 37)],
    [(19, 38), (25, 90)],
    [(19, 38), TIP],
    [(5, 56), (15, 90)],
]
EDGE_MID = [  # softer ridges
    [(28, 37), (35, 92)],
    [(28, 37), TIP],
    [(53, 52), (64, 43)],
    [(40, 94), (52, 77)],
    [(50, 91), (73, 61)],
]
EDGE_LO = [
    [(42, 37), (49, 92)],
    [(10, 61), (23, 91)],
    [(66, 66), (58, 92)],
]

CORE_C = (38, 60)
# glowing veins branching out of the core (pulse)
VEINS = [
    [(38, 51), (36, 45), (37, 41), (34, 37)],
    [(41, 54), (45, 47), (44, 42)],
    [(35, 63), (30, 67), (28, 76), (30, 84)],
    [(41, 66), (45, 72), (44, 80), (47, 88)],
    [(13, 86), (11, 76), (8, 66)],
    [(61, 86), (64, 76), (68, 67)],
]
VEIN_NODES = [(36, 45), (30, 67), (45, 72), (11, 76), (64, 76)]
GLINTS = [(21, 43), (22, 44), (24, 52), (8, 63), (55, 53), (28, 38)]


def core(img, phase=1):
    """inner core glow seen through the front face. phase 0..3 brightness."""
    cx, cy = CORE_C
    rings = [(9, 4, "C"), (7, 3, "F"), (4, 2, "E")]
    if phase == 0:
        rings = [(8, 3, "C"), (5, 2, "F")]
    if phase >= 2:
        rings = [(10, 5, "C"), (8, 3, "F"), (5, 2, "E")]
    for ry, rx, key in rings:
        for y in range(cy - ry, cy + ry + 1):
            for x in range(cx - rx, cx + rx + 1):
                if abs(x - cx) / (rx + 0.5) + abs(y - cy) / (ry + 0.5) <= 1.0:
                    put(img, x, y, key)
    if phase == 3:
        for y in range(cy - 2, cy + 3):
            put(img, cx, y, "E")
    return img


# absorbed junk silhouettes seen inside the front face (deep tone)
GHOSTS = parse("""
..AAAA.....
.AAAAAAA...
AAAAAAAAAA.
A.AA..A.AA.
.A......A..
""")  # a car swallowed by the crystal
WHEEL = parse("""
.AAA.
A.A.A
AAAAA
A.A.A
.AAA.
""")

SIGN = parse("""
..111111..
.1uuuuuu1.
1uullllu1.
1uuuuuuuu1
1uullllu1.
.1uuuuuu1.
..111111..
""".replace("1uullllu1.\n1uuuuuuuu1", "1uuuuuuuu1\n1ullllllu1"))
SIGN = parse("""
..1111..
.1uuuu1.
1uuuuuu1
1lllllll
1uuuuuu1
.1uuuu1.
..1111..
""".replace("1lllllll", "1llllll1"))
POLE = parse("""
.1hi1
1hi1.
""")
TIRE = parse("""
...11111...
.11222221..
1222h32221.
12h1...1h21
122.....221
12h1...1h21
1222h32221.
.11222221..
...11111...
""".replace("1222h32221.", "12223232221").replace(".11222221..", ".112222211."))
PIPE = parse("""
1111111.
uttttts1
tssss2s1
1111111.
""")
RUBBLE_ROCKS = [
    parse("""
.111.
1kji1
1jih1
.111.
"""),
    parse("""
..111..
.1jjh1.
1kjihh1
1jihhg1
.11111.
"""),
    parse("""
.11.
1ih1
1hg1
.11.
"""),
]


def rubble(img):
    """Ground mound of dirt + concrete chunks at the base."""
    mound = [(4, 96), (8, 90), (16, 88), (24, 89), (34, 90), (44, 91), (54, 89), (64, 88), (72, 89), (77, 96)]
    m = poly_fill(img, mound, "3", mask_only=True)
    for y in range(H):
        for x in range(W):
            if m[y, x] and img[y, x] < 0:
                img[y, x] = K["4" if (x * 7 + y * 3) % 11 == 0 else ("3" if y < 93 else "2")]
    # top lip highlight
    for x in range(W):
        col = np.where(m[:, x])[0]
        if len(col):
            y0 = col[0]
            if img[y0, x] in (K["3"], K["4"], K["2"]):
                img[y0, x] = K["5"]
    for (spr, x, y) in [(RUBBLE_ROCKS[1], 9, 87), (RUBBLE_ROCKS[0], 31, 89), (RUBBLE_ROCKS[2], 55, 88),
                        (RUBBLE_ROCKS[0], 70, 89), (RUBBLE_ROCKS[2], 20, 90)]:
        blit(img, spr, x, y)
    return img


def crystal_layer():
    c = blank(W, H)
    for group in (BACK_XTAL, RIGHT_XTAL, MAIN, LEFT_XTAL, FRONT_XTAL):
        for pts, key in group:
            poly_fill(c, pts, key)
    return c


# ------------------------------------------------------------------ damage
CRACKS = {  # cumulative per stage
    1: [[(47, 72), (44, 64), (46, 57), (43, 51)],
        [(64, 82), (66, 76), (64, 71)],
        [(24, 62), (22, 57), (23, 53)]],
    2: [[(42, 62), (46, 68), (44, 74), (48, 81)],
        [(53, 44), (50, 51), (53, 58)],
        [(10, 69), (13, 75), (12, 82)],
        [(33, 86), (31, 80), (34, 76)]],
    3: [[(29, 47), (33, 53), (36, 57)],
        [(35, 64), (31, 70), (33, 78), (30, 85)],
        [(21, 50), (26, 56), (24, 63), (28, 71)],
        [(48, 40), (45, 46), (47, 52)],
        [(7, 62), (9, 68)]],
    4: [[(20, 74), (24, 79), (22, 86)],
        [(50, 76), (54, 82), (52, 88)],
        [(26, 42), (23, 46), (25, 49)],
        [(56, 52), (54, 58)]],
}
CHUNKS = {  # polygons knocked off (erased), cumulative
    1: [[(69, 61), (80, 55), (80, 66), (70, 66)]],
    2: [[(62, 76), (66, 71), (70, 72), (80, 60), (80, 50), (60, 50)],
        [(58, 63), (53, 66), (55, 70), (53, 74), (60, 76)]],
    3: [[(0, 0), (80, 0), (80, 36), (53, 36), (47, 37), (42, 31), (37, 34), (33, 27), (28, 33), (24, 30), (17, 36), (0, 36)],
        [(48, 64), (51, 54), (58, 40), (66, 40), (66, 64)]],
    4: [[(0, 40), (0, 66), (8, 72), (12, 66), (9, 63), (14, 60), (14, 40)]],
}
HOLE = [(31, 47), (33, 43), (35, 46), (38, 42), (41, 47), (44, 46), (44, 51), (47, 55), (45, 59), (47, 64), (44, 68), (43, 74), (40, 72), (37, 77), (34, 73), (31, 74), (31, 68), (28, 63), (30, 58), (28, 53), (31, 51)]
# fallen tip lying in the rubble (s3+)
FALLEN_TIP = parse("""
......AAAA...
...AAACCCEA..
.AACCCCCBBBA.
ACECCBBBBBAA.
.AAABBBAAAA..
....AAAA.....
""")
SHARDS_GROUND = [
    (parse(".A.|AEA|ACA|.A.".replace("|", chr(10))), 26, 88),
    (parse("AA.|ACA|.AA".replace("|", chr(10))), 50, 89),
    (parse(".AA|ACA|AA.".replace("|", chr(10))), 36, 90),
    (parse("A.|CA|AA".replace("|", chr(10))), 62, 90),
]


def crack(img, pts, glow, every=2):
    """fissure: dark gap with light leaking out of it."""
    L = blank(W, H)
    polyline(L, pts, "A")
    ys, xs = np.where(L >= 0)
    order = np.argsort(ys)
    for n, i in enumerate(order):
        x, y = xs[i], ys[i]
        if img[y, x] < 0:
            continue
        img[y, x] = K[glow] if n % every == 0 else K["A"]
        if get(img, x + 1, y) in "BCDE":
            put(img, x + 1, y, "A")
        # lit lip on the upper-left side
        if get(img, x - 1, y) in "BC" and n % 3 == 1:
            put(img, x - 1, y, "C" if get(img, x - 1, y) == "B" else "E")
    return img


def core_big(img, phase):
    cx, cy = CORE_C
    rings = [(11, 6, "F"), (9, 5, "C"), (7, 4, "D"), (4, 2, "E")]
    if phase >= 2:
        rings = [(12, 7, "F"), (10, 5, "D"), (7, 4, "E")]
    for ry, rx, key in rings:
        for y in range(cy - ry, cy + ry + 1):
            for x in range(cx - rx, cx + rx + 1):
                if abs(x - cx) / (rx + 0.5) + abs(y - cy) / (ry + 0.5) <= 1.0:
                    put(img, x, y, key)
    return img


PARTICLES = [(14, 40, "D"), (60, 30, "E"), (24, 24, "C"), (70, 48, "D"), (6, 50, "C"), (52, 22, "E"),
             (44, 14, "D"), (10, 30, "E")]


def particles(img, f, n, stage):
    for i, (x, y0, k) in enumerate(PARTICLES[:n]):
        y = 16 + (y0 - 16 - 3 * f - 2 * i) % 60
        xx = x + (1 if (f + i) % 4 == 3 else 0)
        if get(img, xx, y) == ".":
            put(img, xx, y, k)
            if k == "E" and get(img, xx, y + 1) == ".":
                put(img, xx, y + 1, "C")
    return img


def nexus(stage=0, f=0):
    """stage 0..4, f = pulse frame 0..3"""
    phase = [0, 1, 3, 1][f] if stage < 3 else [1, 2, 3, 2][f]
    c = crystal_layer()
    for y in range(80, H):
        for x in range(W):
            v = c[y, x]
            if v == K["C"] and y > 82:
                c[y, x] = K["B"]
            elif v == K["B"] and y > 85:
                c[y, x] = K["A"]
    blit(c, GHOSTS, 41, 74)
    blit(c, WHEEL, 44, 40)
    core(c, phase)
    vk = {0: "C", 1: "C", 2: "D", 3: "D"}[phase]
    for v in VEINS:
        polyline(c, v, vk)
    for (x, y) in VEIN_NODES:
        put(c, x, y, "D" if phase < 2 else "E")
    for e in EDGE_MID:
        polyline(c, e, "C")
    for e in EDGE_HI:
        polyline(c, e, "E")
    for e in EDGE_LO:
        polyline(c, e, "A")
    for (x, y) in GLINTS:
        if get(c, x, y) in "BC":
            put(c, x, y, "E")
    xtal = c >= 0
    # damage
    ck = "E" if phase >= 2 else "D"
    for st in range(1, stage + 1):
        for pts in CRACKS[st]:
            crack(c, pts, ck if st < 3 else ("F" if phase == 0 else "E"), every=2 if st < 3 else 1)
    for st in range(1, stage + 1):
        for pts in CHUNKS[st]:
            m = poly_fill(c, pts, "A", mask_only=True)
            xtal &= ~m
    if stage >= 4:
        hm = poly_fill(c, HOLE, "A", mask_only=True)
        c[hm & xtal] = K["A"]
        inner = blank(W, H)
        poly_fill(inner, [(33, 49), (38, 46), (43, 52), (43, 64), (38, 72), (33, 66), (32, 56)], "B")
        c[(inner >= 0) & hm & xtal] = K["B"]
        core_big(c, phase)
    c[~xtal] = T
    # fracture faces: pixels on a fresh break get a light rim (top-left lit)
    if stage >= 1:
        rim = np.zeros_like(xtal)
        for st in range(1, stage + 1):
            for pts in CHUNKS[st]:
                m = poly_fill(blank(W, H), pts, "A", mask_only=True)
                g = np.zeros_like(m)
                g[1:, :] |= m[:-1, :]; g[:-1, :] |= m[1:, :]; g[:, 1:] |= m[:, :-1]; g[:, :-1] |= m[:, 1:]
                rim |= g & xtal
        c[rim] = K["E"] if phase >= 2 else K["C"]
    c = outline(c, "A")
    g = blank(W, H)
    rubble(g)
    if stage >= 3:
        blit(g, FALLEN_TIP, 0, 84)
    for i, (spr, x, y) in enumerate(SHARDS_GROUND):
        if stage >= 1 + i:
            blit(g, spr, x, y)
    blit(g, PIPE, 52, 56)
    if stage < 4:
        blit(g, POLE, 9, 77)
        blit(g, POLE, 7, 78)
        blit(g, SIGN, 1, 70)
    else:
        blit(g, rot90(SIGN), 3, 86)
    blit(g, TIRE, 59, 82)
    blit(c, g, 0, 0)
    particles(c, f, 3 + stage, stage)
    return c


def nexus_s0():
    return nexus(0, 0)


# ------------------------------------------------------------------ hit / destroy
def split_layers(stage, f, glow_all=False):
    """returns (crystal_only, ground_and_junk) for a stage"""
    full = nexus(stage, f)
    g = blank(W, H)
    rubble(g)
    if stage >= 3:
        blit(g, FALLEN_TIP, 0, 84)
    for i, (spr, x, y) in enumerate(SHARDS_GROUND):
        if stage >= 1 + i:
            blit(g, spr, x, y)
    if stage >= 4:
        blit(g, rot90(SIGN), 3, 86)
    blit(g, TIRE, 59, 82)
    xt = full.copy()
    xt[g >= 0] = T
    # drop the pipe/sign/particles from crystal layer: keep only neon + outline-ish
    keep = np.isin(xt, [K[k] for k in "ABCDEF"])
    xt[~keep] = T
    return xt, g


FLASH_FULL = {"A": "E", "B": "l", "C": "l", "D": "l", "E": "l", "F": "l"}
FLASH_HALF = {"A": "B", "B": "C", "C": "E", "D": "E", "E": "l", "F": "E"}


def tint(img, mapping, keep_outline=True):
    out = img.copy()
    xt = img >= 0
    # outline pixels = A touching transparency
    edge = np.zeros_like(xt)
    if keep_outline:
        e = ~xt
        nb = np.zeros_like(xt)
        nb[1:, :] |= e[:-1, :]; nb[:-1, :] |= e[1:, :]; nb[:, 1:] |= e[:, :-1]; nb[:, :-1] |= e[:, 1:]
        edge = xt & nb & (img == K["A"])
    for a, b in mapping.items():
        out[(img == K[a]) & ~edge] = K[b]
    return out


def hit_frames():
    xt, g = split_layers(0, 1)
    f1 = tint(xt, FLASH_FULL); blit(f1, g, 0, 0)
    xt2 = tint(xt, FLASH_HALF); blit(xt2, g, 0, 0)
    return [f1, xt2]


def voronoi_shards(mask, seeds):
    ys, xs = np.where(mask)
    S = np.array(seeds, float)
    d = (xs[:, None] - S[None, :, 0]) ** 2 + (ys[:, None] - S[None, :, 1]) ** 2
    lab = np.full(mask.shape, -1, int)
    lab[ys, xs] = d.argmin(1)
    return lab


SEEDS = [(30, 22), (24, 40), (36, 40), (46, 42), (20, 56), (30, 54), (44, 56), (54, 50), (10, 66), (22, 72),
         (34, 66), (46, 70), (56, 70), (66, 72), (14, 84), (28, 84), (40, 84), (52, 86), (62, 84)]


def ring(img, cx, cy, r, key, step=1):
    import math
    for i in range(0, 360, step):
        a = math.radians(i)
        put(img, int(round(cx + math.cos(a) * r)), int(round(cy + math.sin(a) * r * 0.8)), key)
    return img


def rays(img, cx, cy, length, key, n=8, phase=0):
    import math
    for i in range(n):
        a = math.radians(i * 360 / n + phase)
        x1, y1 = cx + math.cos(a) * length, cy + math.sin(a) * length
        polyline(img, [(cx, cy), (x1, y1)], key)
    return img


DUST = [  # puff sprites (fill), grow over time
    parse("""
.kk.
kjjk
.kj.
"""),
    parse("""
..kkk..
.kkjjk.
kjjjijk
.kjiij.
..jjj..
"""),
    parse("""
...kkk...
.kkkjjkk.
kkjjjjjjk
kjjiiijjk
.jjiiiij.
..jj.jj..
"""),
]


def dust_layer(t, xs=(12, 26, 40, 54, 66)):
    L = blank(W, H)
    if t < 0:
        return L
    spr = DUST[min(2, t // 2)]
    for i, x in enumerate(xs):
        y = 86 - t * 2 - (i % 2) * 2
        sp = spr if t < 7 else recolor(spr, {"k": "j", "j": "i", "i": "h"})
        if t >= 9:
            sp = recolor(sp, {"j": ".", "k": ".", "i": "h"})
        blit(L, sp, x - spr.shape[1] // 2 + (t // 3) * (1 if i % 2 else -1), y)
    return L


def destroy_frames():
    out = []
    xt, g = split_layers(4, 2)
    cx, cy = CORE_C
    # 1-2 overload
    f = tint(xt, {"B": "C", "A": "B"}); core_big(f, 3); blit(f, g, 0, 0); out.append(f)
    f = tint(xt, FLASH_HALF); core_big(f, 3); rays(f, cx, cy, 14, "E", 8, 22); blit(f, g, 0, 0); out.append(f)
    # 3 white flash
    f = tint(xt, FLASH_FULL); rays(f, cx, cy, 30, "l", 8, 22); rays(f, cx, cy, 20, "E", 8, 0)
    blit(f, g, 0, 0); out.append(f)
    # 4.. shards
    mask = xt >= 0
    rng = np.random.default_rng(7)
    ys0, xs0 = np.where(mask)
    extra = [(int(xs0[i]), int(ys0[i])) for i in rng.choice(len(xs0), 16, replace=False)]
    seeds = SEEDS + extra
    lab = voronoi_shards(mask, seeds)
    vel = []
    for (sx, sy) in seeds:
        dx, dy = sx - cx, sy - cy
        n = max(1.0, (dx * dx + dy * dy) ** 0.5)
        vel.append((dx / n * 2.6 + rng.uniform(-0.4, 0.4), dy / n * 1.4 - 1.6 + rng.uniform(-0.6, 0.3)))
    for k, t in enumerate(range(1, 9)):  # frames 4..11
        f = blank(W, H)
        blit(f, g, 0, 0)
        if t <= 3:  # core burst ring
            ring(f, cx, cy, 6 + t * 7, "E" if t < 3 else "C", 2 if t < 3 else 6)
            ring(f, cx, cy, 3 + t * 5, "F", 3)
            if t == 1:
                for r in range(4, 0, -1):
                    core_small = blank(W, H)
                disc(f, cx, cy, 4, "l"); disc(f, cx, cy, 2, "E")
        L = blank(W, H)
        for i, (vx, vy) in enumerate(vel):
            sm = lab == i
            if not sm.any():
                continue
            ys, xs = np.where(sm)
            dx = int(round(vx * t * 1.3))
            dy = int(round(vy * t + 0.95 * t * t))
            src = xt[ys, xs]
            if t == 1:
                src = np.where(src == K["A"], K["C"], np.where(src == K["B"], K["E"], src))
            elif t >= 4:
                src = np.where(np.isin(src, [K["E"], K["D"], K["F"]]), K["C"], src)
            if t >= 6:
                src = np.where(src == K["C"], K["B"], src)
            nx, ny = xs + dx, ys + dy
            if (ny.max() if len(ny) else 0) >= 92:  # landed -> gone (aftermath leaves pieces)
                continue
            ok = (nx >= 0) & (nx < W) & (ny >= 0)
            L[ny[ok], nx[ok]] = src[ok]
        L = outline(L, "A")
        blit(f, L, 0, 0)
        blit(f, dust_layer(t - 2), 0, 0)
        out.append(f)
    # 12 aftermath
    f = blank(W, H)
    blit(f, g, 0, 0)
    for (spr, x, y) in [(SHARDS_GROUND[0][0], 20, 87), (SHARDS_GROUND[1][0], 44, 88), (FALLEN_TIP, 26, 85),
                        (SHARDS_GROUND[2][0], 58, 87)]:
        blit(f, spr, x, y)
    put(f, 39, 86, "E"); put(f, 38, 86, "D"); put(f, 40, 87, "D"); put(f, 39, 85, "C")
    out.append(f)
    return out


def build_asset():
    a = Asset("Assets/Art/Nexus/nexus.aseprite", W, H, ["nexus"])
    for st in range(5):
        a.tag(f"s{st}_idle", [nexus(st, f) for f in range(4)], 200)
    a.tag("hit", hit_frames(), 50)
    a.tag("destroy", destroy_frames(), 80)
    return a


if __name__ == "__main__":
    a = build_asset()
    if "--export" in sys.argv:
        export(a, "nexus", s=3)
    else:
        write_previews(a, "nexus", s=3)
