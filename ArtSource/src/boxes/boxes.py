"""Loot boxes 16x16 — tags idle(2x300) open(5x70).
Each box = CLOSED grid + OPEN grid (flaps/lid open, interior visible); frames composed
from them. Lid = rows <= LID_ROW of CLOSED."""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "tools"))
from px import *
from pose import disc


def P(s):
    return parse(s, pad=16)


BOX = {}

BOX["common"] = dict(
    closed=P("""
................
................
................
................
..111111111111..
..17777oo77761..
..16666kk66651..
..15555nn55541..
..16666nn66541..
..16446nn66541..
..16466nn66541..
..16666nn66541..
..15555nn55441..
..14444444443 1.
..111111111111..
................
""".replace("3 1.", "31..")),
    open=P("""
................
................
................
11............11
171..........171
1671111111111761
.11233333333211.
..13222222223 1.
..16666nn66541..
..16446nn66541..
..16466nn66541..
..16666nn66541..
..15555nn55441..
..14444444443 1.
..111111111111..
................
""".replace("3 1.", "31..")),
    glint=[(3, 5, "l"), (4, 4, "x")],
    beam=("x", "w", "v"),
)

BOX["uncommon"] = dict(
    closed=P("""
................
................
................
................
..111111111111..
..17677767776 1.
..1j55555555j1..
..1466666666541.
..1433333333341.
..1j66766666j41.
..1466666665541.
..1433333333341.
..1j66666666j41.
..1455555554431.
..111111111111..
................
""".replace("6 1.", "61..").replace("541.", "541").replace("341.", "341").replace("j41.", "j41").replace("431.", "431")),
    open=P("""
................
.1111...........
.17761..........
..17661.........
...1766111111...
..1j11222222j1..
..1j32222222j1..
..1466666666541.
..1433333333341.
..1j66766666j41.
..1466666665541.
..1433333333341.
..1j66666666j41.
..1455555554431.
..111111111111..
................
""".replace("541.", "541").replace("341.", "341").replace("j41.", "j41").replace("431.", "431")),
    glint=[(12, 6, "l"), (3, 9, "k")],
    beam=("x", "w", "v"),
)

BOX["rare"] = dict(
    closed=P("""
................
................
................
.....111111.....
.....1h11h1.....
..111h1111h111..
..1lkkkkkkkkj1..
..1kjjjjjjjjh1..
..1111wxw11111..
..1jiiiwwiiih1..
..1jiiiiiiiih1..
..1kjjjjjjjjh1..
..1jiiiiiiiih1..
..1hhhhhhhhhg1..
..111111111111..
................
"""),
    open=P("""
................
..111111111111..
..1lkkkkkkkkj1..
..1kjjjjjjjjh1..
..1hhhhhhhhhg1..
..111111111111..
..1g22222222g1..
..1g22222222g1..
..1111wxw11111..
..1jiiiwwiiih1..
..1jiiiiiiiih1..
..1kjjjjjjjjh1..
..1jiiiiiiiih1..
..1hhhhhhhhhg1..
..111111111111..
................
"""),
    glint=[(3, 6, "l"), (10, 9, "l")],
    beam=("l", "k", "j"),
)

BOX["alien"] = dict(
    closed=P("""
................
................
................
.......11.......
.....11CC11.....
....1CCEECC1....
...1CBBDDBBC1...
..1CBDBBBBDBB1..
..1CBBBFFBBBA1..
..1BDBBFFBBDA1..
..1CBBBBBBBBA1..
..1BBDBBBBDAA1..
...1BBBDDBAA1...
....1AAAAAA1....
.....111111.....
................
"""),
    open=P("""
................
................
...11......11...
..1CC1....1CB1..
..1CEC1..1CBB1..
...1CB1111BB1...
...1CAEEEEAB1...
..1CBAAEEAABB1..
..1CBBBFFBBBA1..
..1BDBBFFBBDA1..
..1CBBBBBBBBA1..
..1BBDBBBBDAA1..
...1BBBDDBAA1...
....1AAAAAA1....
.....111111.....
................
"""),
    glint=[(7, 5, "l"), (4, 9, "E")],
    beam=("E", "D", "C"),
)

BOX["unknown"] = dict(
    closed=P("""
................
................
................
..111111111111..
..12222222222 1.
..12000000000 1.
..120111011101..
..120101010101..
..120111011101..
..120000001001..
..120000000001..
..120F00000D01..
..120000000001..
..111111111111..
................
................
""".replace("2 1.", "21..").replace("0 1.", "01..")),
    open=P("""
................
...11.....11....
..1221...1221...
...1221.1221....
..111111111111..
..1AAAAAAAAAA1..
..12AFFDDFFA21..
..120111011101..
..120101010101..
..120111011101..
..120000001001..
..120000000001..
..120F00000D01..
..120000000001..
..111111111111..
................
"""),
    glint=[(5, 7, "D"), (9, 11, "F")],
    beam=("F", "D", "C"),
)


def squash(img):
    """1px squash: drop one interior row, push top down, widen bottom by 1 each side"""
    ys, xs = np.where(img >= 0)
    y0, y1 = ys.min(), ys.max()
    mid = (y0 + y1) // 2
    out = blank(16, 16)
    blit(out, img[:mid], 0, 1)
    blit(out, img[mid + 1:], 0, mid + 1)
    return out


def beam(img, keys, h, w):
    """light column rising from the box opening (no outline)"""
    ys, xs = np.where(img >= 0)
    cx = (xs.min() + xs.max()) / 2.0
    top = ys.min()
    L = blank(16, 16)
    for y in range(max(0, top + 4 - h), top + 5):
        t = (top + 4 - y) / max(1, h)
        ww = w * (1.0 - 0.5 * t)
        for x in range(16):
            d = abs(x + 0.5 - (cx + 0.5))
            if d <= ww / 2:
                key = keys[0] if d <= ww / 6 else (keys[1] if d <= ww / 3 else keys[2])
                put(L, x, y, key)
    under(img, L, 0, 0)
    return img


def sparkles(img, pts, key):
    for (x, y) in pts:
        if get(img, x, y) == ".":
            put(img, x, y, key)
    return img


def frames_for(name):
    b = BOX[name]
    C, O = b["closed"], b["open"]
    idle2 = C.copy()
    for (x, y, k) in b["glint"]:
        put(idle2, x, y, k)
    if name == "unknown":  # glitch: shift two rows
        idle2[6] = np.roll(idle2[6], 1)
        idle2[10] = np.roll(idle2[10], -1)
    if name == "alien":
        idle2[idle2 == K["D"]] = K["E"]
    k0, k1, k2 = b["beam"]
    o1 = squash(C)
    o2 = blank(16, 16)
    lid_rows = 7
    blit(o2, C[lid_rows:], 0, lid_rows)
    blit(o2, C[:lid_rows], 0, -2)  # lid pops up
    sparkles(o2, [(3, 5), (12, 4)], k1)
    o3 = beam(O.copy(), b["beam"], 4, 6)
    o4 = beam(O.copy(), b["beam"], 9, 9)
    sparkles(o4, [(2, 2), (13, 1), (1, 6), (14, 5)], k0)
    o5 = beam(O.copy(), (k1, k2, k2), 3, 4)
    sparkles(o5, [(3, 1), (12, 3)], k2)
    if name == "rare":  # light pours out from under the lid
        for img, keys in ((o3, "kj"), (o4, "lk"), (o5, "jh")):
            band = img[6:8]
            band[band == K["2"]] = K[keys[0]]
            img[7, 4:12][img[7, 4:12] == K[keys[0]]] = K[keys[1]]
    return [C, idle2], [o1, o2, o3, o4, o5]


if __name__ == "__main__":
    for name in BOX:
        idle, opn = frames_for(name)
        a = Asset(f"Assets/Art/Boxes/box_{name}.aseprite", 16, 16, ["box"])
        a.tag("idle", idle, 300)
        a.tag("open", opn, 70)
        if "--export" in sys.argv:
            export(a, "boxes", s=6)
        else:
            write_previews(a, "boxes", s=6)
