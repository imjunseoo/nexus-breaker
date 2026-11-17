"""props.aseprite — one prop per layer, each also a slice of the same name.
Every slice's bottom row = ground contact. Canvas 160x72."""
import sys, os, math
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "tools"))
from px import *
from pose import poly_fill, disc

CW, CH = 160, 72


def P(s, w):
    return parse(s, pad=w)


def car_wreck():
    """burnt-out hatchback, rusted, flat tires, 56x24"""
    c = blank(56, 24)
    body = [(2, 21), (2, 13), (6, 11), (14, 10), (20, 3), (36, 3), (44, 10), (52, 11), (54, 14), (54, 21)]
    poly_fill(c, body, "5")
    poly_fill(c, [(2, 16), (54, 16), (54, 21), (2, 21)], "4")
    poly_fill(c, [(2, 13), (54, 13), (54, 14), (2, 14)], "6")
    # roof dent + windows (empty frames)
    poly_fill(c, [(16, 10), (21, 5), (27, 5), (27, 10)], "2")
    poly_fill(c, [(29, 10), (29, 5), (35, 5), (41, 10)], "2")
    for x in range(21, 36):
        put(c, x, 4, "6")
    put(c, 28, 5, "4"); put(c, 28, 9, "4")
    # rust holes / scorch
    for (x, y, k) in [(8, 17, "3"), (9, 17, "3"), (9, 18, "2"), (40, 18, "3"), (41, 18, "2"), (30, 15, "3"),
                      (46, 13, "3"), (12, 13, "t"), (13, 13, "t"), (35, 12, "t")]:
        put(c, x, y, k)
    # door seam + handle
    for y in range(11, 21):
        put(c, 28, y, "3")
    put(c, 24, 14, "j"); put(c, 32, 14, "j")
    # bumpers + broken lamp
    poly_fill(c, [(0, 17), (3, 17), (3, 20), (0, 20)], "h")
    poly_fill(c, [(53, 17), (56, 17), (56, 20), (53, 20)], "h")
    put(c, 2, 14, "w"); put(c, 53, 14, "u")
    # flat tyres
    for cx in (12, 44):
        for y in range(18, 24):
            for x in range(cx - 5, cx + 6):
                if (x - cx) ** 2 / 30 + (y - 21.5) ** 2 / 7 <= 1:
                    put(c, x, y, "2")
        put(c, cx, 20, "h"); put(c, cx - 1, 20, "g"); put(c, cx, 21, "g")
    return outline_inside(c)


def outline_inside(c):
    """outline that stays inside the given canvas (shrinks by keeping 1px margin)"""
    h, w = c.shape
    big = blank(w + 2, h + 2)
    blit(big, c, 1, 1)
    big = outline(big, "1")
    return big[1:-1, 1:-1] if False else big


TIRES = P("""
....111111....
..1122222211..
.122hgggg2221.
.12g211112g21.
.122hgggg2221.
..1122222211..
.1122222222211
12hggggggg2221
12g21111112g21
12hggggggg2221
.112222222211.
.1122222222211
12hggggggg2221
12g21111112g21
12hggggggg2221
.11222222221 1
...1111111111.
""".replace("221 1", "2211"), 15)

DRUM = P("""
.11111111.
1kjjjjjih1
1jhhhhhhg1
1rqqqqqqp1
1rqq5qqqp1
1hhhhhhhg1
1rqqqqqqp1
1rqqqq54p1
1rqqqqqqp1
1hhhhhhhg1
1rqqqqqqp1
1rq5qqqqp1
1rqqqqqqp1
1jhhhhhhg1
.11111111.
""", 10)

DRUM_FALLEN = P("""
..11111111111...
.1rrqhqqqhqqq11.
1jrqqhq5qhqqqp1k1
1hrqqhqqqhqq5p1j1
1hrqqhqqqhqqqp1h1
1hqqqhqqqhqqqp1g1
.1pppgpppgppp11.
..11111111111...
""", 17)

SANDBAGS = P("""
.....1111111.1111111....
....1onnnnnm1onnnnnm1...
...1onnnnnnnmnnnnnnnm1..
...1nnnnnnnmmnnnnnnmm1..
.1111111111111111111111.
1onnnnnm1onnnnnm1onnnnm1
1nnnnnnnmnnnnnnnmnnnnnm1
1mnnnnmmmnnnnnmmmnnnnmm1
.11111111111111111111111
""", 24)

SIGN = P("""
...1111111...
..1jkkkkkj1..
.1jkuuuuukj1.
1jkuuuuuuukj1
1kullllllluk1
1kuuuuuuuuuk1
.1jkuuuuukj1.
..1jkkkkkj1..
...1111h11...
......1hg1...
......1hg1...
.....1hg1....
.....1hg1....
.....1hg1....
....1hg1.....
....1hg1.....
....1hg1.....
....1hg1.....
....1hg1.....
....1hg1.....
...111111....
..1hhhhgg1...
..11111111...
""", 13)

CONE = P("""
...11...
...1v1..
..1vu1..
..1ll1..
.1lkl1..
.1vuut1.
.1vuut1.
1hhhhgg1
11111111
""", 8)

SCRAP = P("""
............11.........
...........1j1.....11..
....11.....1j1....1v1..
...1ij1...1hi1...1vu1..
..1ijhh1111hh1.111ut1..
.1ijh5h1jjk1h11jjh1t11.
.1hh54h1jkh1hh1ihh1111.
1ihh44hhkjhh1hhhhhghhh1
1hgg4hggjhhhgghhgg5ggg1
1gggggggggggggggggg44g1
.1111111111111111111111
""", 23)

LAMP = P("""
.111111.......
1jkkkkj1......
.1hhhh11......
..1121h1......
...1x11h1.....
....1..1h1....
........1h1...
........1h1...
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
.........1h1..
........1jhg1.
.......1jhhgg1
.......1111111
""", 14)

PROPS = [  # name, sprite fn/grid, x, y on the sheet
    ("car_wreck", None, 0, 2),
    ("tire_stack", TIRES, 60, 9),
    ("oil_drum", DRUM, 78, 11),
    ("oil_drum_fallen", DRUM_FALLEN, 92, 18),
    ("sandbags", SANDBAGS, 112, 17),
    ("road_sign", SIGN, 140, 3),
    ("traffic_cone", CONE, 2, 50),
    ("scrap_pile", SCRAP, 14, 48),
    ("lamp_post", LAMP, 44, 29),
]


def build():
    layers = [p[0] for p in PROPS]
    a = Asset("Assets/Art/Environment/props.aseprite", CW, CH, layers)
    cels = {}
    for name, spr, x, y in PROPS:
        if spr is None:
            spr = car_wreck()
        L = blank(CW, CH)
        blit(L, spr, x, y)
        cels[name] = L
        bb = bbox(L)
        a.slices.append(dict(name=name, rect=(int(bb[0]), int(bb[1]), int(bb[2] - bb[0] + 1), int(bb[3] - bb[1] + 1))))
    a.add(cels, 100)
    a.preview_bg = (150, 132, 100)
    return a


if __name__ == "__main__":
    a = build()
    if "--export" in sys.argv:
        export(a, "environment", s=4)
    else:
        write_previews(a, "environment", s=4)
    for s in a.slices:
        print(s["name"], s["rect"])
