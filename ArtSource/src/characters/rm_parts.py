"""Repairman parts (fill-only unless noted). Facing LEFT. Canvas 48x48, feet on y=47."""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "tools"))
from px import *
from pose import *

HEAD_F = parse("""
...................
.......xxxw.ww.....
.....xxwwwwvwwvv...
.....xwwwwwwwwvv...
....xwwwwwwwwwvvu..
....wwwwwwwwwwvvu..
.xxxxwwwwwwwwwwvvu.
......11111111111..
......45555554222..
......62266665232..
.....6l1666656422..
....776676665643...
.....23366655433...
......233465433....
......34333222.....
...................
""")

TORSO_F = parse("""
..qqpqqqqqqpp.
.rrqqqqrrqqqpp
rrqqqqqqqpqqqp
rqqpqqqqqpqqqp
rqqpqrrrqqqqpp
rqqpqpqpqqqppp
rqqpqpppqqqqpp
.rqpqqqqqqqqp.
.3344j44334443
.qqqqqqqqqqqp.
""")

POUCH = parse("""
.11111.
165554.
1444443
1433332
1433332
.11111.
""")

LEGS_STAND = parse("""
...rqqqqqqqqqqqpp..
..rqqqqqqqp.qqqqp..
..rqqqqqqp...qqqp..
.rqqqqqqp....rqqqp.
.r44qqqp.....r44qp.
.rqqqqp......rqqqp.
..2233.......22332.
.344443......344443
34555541....3455554
12222221....1222222
""")

LEGS_CROUCH = parse("""
...rqqqqqqqqqqqpp...
.rrqqqqqqqp.qqqqqp..
rqqqqqqqp....rqqqqp.
r44qqqqp.....r44qqp.
.rqqqqp.......rqqp..
.22332........2233..
.344443......344443.
34555541....3455554.
12222221....1222222.
""")

LEGS_LUNGE = parse("""
.....rqqqqqqqqqqpp.....
...rqqqqqqqp..qqqqp....
..rqqqqqqp.....qqqqp...
.rq44qqqp.......qqqqp..
.rqqqqqp.........r44qp.
..rqqqp...........rqqp.
..22332...........2233.
.344443...........34443
34555541.........345554
12222221.........122222
""")

MASK = parse("""
.11111.
1lkkjh1
1kjiih1
.1hhg1.
..111..
""")

# ---- hammer heads (fill only), keyed by direction handle->head
HEAD_U = parse("""
.lkkkkkkkj.
lkjjjjjjjih
kjjjiiiiihg
jiiihhhhhgg
.hhggggggg.
""")
HEAD_D = parse("""
.kkkkkkkjj.
kjjjjjjjjih
jjjiiiiiihg
iiihhhhhhgg
.hhggggggg.
""")
HEAD_L = parse("""
.lkj.
lkjjh
kjjih
kjiih
kjiih
jjihg
jiihg
jiihg
iihhg
ihhgg
.hgg.
""")
import math


def head_diag(ux, uy, cx, cy, length=10, width=5):
    """procedural rotated head; (ux,uy) = handle direction (grip->head)."""
    L = blank(48, 48)
    nx, ny = -uy, ux
    a = (cx - nx * length / 2, cy - ny * length / 2)
    b = (cx + nx * length / 2, cy + ny * length / 2)
    thick_line(L, a[0], a[1], b[0], b[1], "kjjih"[:width])
    return L


def hammer_layer(gx, gy, hx, hy, head=None, hox=0, hoy=0):
    """Hammer from grip end (gx,gy) to head center (hx,hy). Returns outlined layer."""
    L = blank(48, 48)
    thick_line(L, gx, gy, hx, hy, "64")
    # grip tape: first 3px of handle -> red wrap
    dx, dy = hx - gx, hy - gy
    n = math.hypot(dx, dy)
    for t in range(0, 4):
        x = gx + dx * t / n
        y = gy + dy * t / n
        for ox in (-1, 0, 1):
            for oy in (-1, 0, 1):
                xx, yy = round(x + ox * 0.5), round(y + oy * 0.5)
                k = get(L, xx, yy)
                if k == "6":
                    put(L, xx, yy, "u" if t % 2 == 0 else "t")
                elif k == "4":
                    put(L, xx, yy, "t" if t % 2 == 0 else "s")
    if head is None:
        blit(L, head_diag(dx / n, dy / n, hx, hy), 0, 0)
    else:
        blit(L, head, hx - hox, hy - hoy)
    return outline(L, "1")


GLOVE = parse("""
.776.
77665
66654
.554.
""")
GLOVE_FAR = recolor(GLOVE, {"7": "6", "6": "5", "5": "4", "4": "3"})


def arm_layer(sx, sy, ex, ey, hx, hy, far=False):
    """Shoulder -> elbow -> hand, sleeve olive, glove tan. outlined."""
    L = blank(48, 48)
    bands = "qpp" if far else "rqp"
    thick_line(L, sx, sy, ex, ey, bands)
    thick_line(L, ex, ey, hx, hy, bands)
    glove = GLOVE_FAR if far else GLOVE
    blit(L, glove, int(round(hx)) - 2, int(round(hy)) - 2)
    return outline(L, "1")


def body(tx=0, ty=0, hx=0, hy=0, legs=LEGS_STAND, lx=14, ly=38, pouch=True):
    c = blank(48, 48)
    blit(c, legs, lx, ly)
    blit(c, TORSO_F, 18 + tx, 28 + ty)
    blit(c, HEAD_F, 11 + tx + hx, 14 + ty + hy)
    c = outline(c, "1")
    if pouch:
        blit(c, POUCH, 28 + tx, 35 + ty)
    return c
