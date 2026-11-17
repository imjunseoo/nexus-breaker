"""Shop stall — wrecked bus turned into a market stall. 128x80, bottom row = ground contact.
idle (4x200): lantern flicker + rooftop flag flutter."""
import sys, os, math
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "tools"))
from px import *
from pose import poly_fill, polyline, disc

W, H = 128, 80


def R(img, x, y, w, h, k):
    img[max(0, y):y + h, max(0, x):x + w] = K[k]


def bus_body():
    c = blank(W, H)
    # main shell (rounded roof corners)
    R(c, 6, 26, 116, 40, "n")
    R(c, 8, 24, 112, 2, "n"); R(c, 10, 23, 108, 1, "n")
    # roof highlight + lower body shadow
    R(c, 10, 23, 108, 1, "o"); R(c, 8, 24, 112, 1, "o")
    R(c, 6, 58, 116, 8, "m")
    # side stripe
    R(c, 6, 50, 116, 2, "3")
    # windows row (front cab + rear)
    for x in list(range(10, 24, 7)) + list(range(98, 118, 7)):
        R(c, x, 30, 5, 10, "g")
        R(c, x, 30, 5, 1, "1")
        put(c, x + 1, 31, "i"); put(c, x + 1, 32, "h")
    # boarded rear windows (planks across)
    for x in (98, 105, 112):
        R(c, x - 1, 33, 7, 2, "5"); R(c, x - 1, 35, 7, 1, "3")
    # front: windshield + bumper + headlight
    R(c, 3, 34, 4, 26, "n"); R(c, 3, 34, 1, 26, "o")
    R(c, 2, 58, 6, 4, "h"); R(c, 2, 58, 6, 1, "j")
    put(c, 3, 52, "x"); put(c, 4, 52, "w"); put(c, 3, 53, "w")
    R(c, 3, 29, 5, 12, "g"); R(c, 3, 29, 5, 1, "1"); put(c, 4, 31, "i"); put(c, 4, 32, "i"); put(c, 5, 33, "h")
    R(c, 6, 42, 116, 1, "m")
    for xx in range(9, 120, 6):
        put(c, xx, 47, "m"); put(c, xx, 63, "4")
    for (x, y) in ((44, 60), (80, 46), (18, 56)):
        put(c, x, y, "m"); put(c, x + 1, y, "m"); put(c, x, y + 1, "o")
    # rust patches (hand placed blobs)
    for (x, y, w, h) in [(30, 60, 9, 4), (64, 56, 5, 6), (88, 61, 12, 3), (14, 44, 4, 5), (116, 42, 4, 8)]:
        R(c, x, y, w, h, "5")
        R(c, x + 1, y + 1, max(1, w - 2), max(1, h - 2), "4")
    # counter opening (dark interior) + shelves with goods
    R(c, 26, 32, 68, 20, "2")
    R(c, 26, 32, 68, 1, "1")
    R(c, 28, 40, 64, 1, "4"); R(c, 28, 41, 64, 1, "3")  # shelf
    goods = ["jjh", "uut", "kkj", "rrq", "wwv", "6654", "lkj", "ih", "vu", "kj"]
    x = 30
    for i, g in enumerate(goods * 2):
        if x > 88:
            break
        hgt = 3 + (i % 3)
        for j, k in enumerate(g):
            for yy in range(40 - hgt, 40):
                put(c, x + j, yy, k)
        x += len(g) + 2
    # counter plank (front), lit top
    R(c, 22, 52, 76, 3, "6"); R(c, 22, 52, 76, 1, "7"); R(c, 22, 55, 76, 1, "4")
    for xx in range(24, 96, 9):
        put(c, xx, 53, "4")
    # door at the rear (closed, dented)
    R(c, 100, 42, 14, 22, "m"); R(c, 100, 42, 14, 1, "o"); R(c, 107, 42, 1, 22, "3")
    put(c, 104, 52, "j"); put(c, 110, 52, "j")
    # painted nut sign board on the rear side
    R(c, 98, 26, 20, 3, "2")
    return c


NUT_SIGN = parse("""
....1111....
..11wwww11..
.1wwvvvvww1.
1wwv1111vww1
1wv1....1vw1
1wv1....1vv1
1wwv1111vvv1
.1wvvvvvvv1.
..11vvvv11..
....1111....
""".replace("....", "....", 1))


def wheels(c):
    for (cx, blocks) in ((24, False), (102, True)):
        if blocks:  # missing wheel -> cinder blocks
            for (x, y) in ((96, 66), (104, 66), (100, 71)):
                R(c, x, y, 8, 5, "i"); R(c, x, y, 8, 1, "j")
                R(c, x + 2, y + 2, 2, 2, "g"); R(c, x + 5, y + 2, 2, 2, "g")
            R(c, 96, 76, 16, 4, "h")
            continue
        disc(c, cx, 68, 9.5, "1")
        disc(c, cx, 68, 8, "2")
        disc(c, cx, 68, 4, "h")
        disc(c, cx, 68, 2, "j")
        put(c, cx - 2, 64, "g"); put(c, cx - 3, 65, "g")
    # flat tire at front squashed into ground
    R(c, 10, 77, 30, 3, "2")


def awning(c, f):
    # canvas canopy: faded stripes, scalloped edge, patched
    top, bot = 22, 31
    for x in range(18, 104):
        y0 = top + (x - 18) // 30
        stripe = "o" if ((x - 18) // 6) % 2 == 0 else "t"
        shade = {"o": "n", "t": "s"}[stripe]
        for y in range(y0, bot):
            put(c, x, y, stripe if y < bot - 2 else shade)
        # scallops
        if (x - 18) % 6 in (1, 2, 3, 4):
            put(c, x, bot, shade)
        if (x - 18) % 6 in (2, 3):
            put(c, x, bot + 1, shade)
    # patches + stitches
    for (x, y) in ((40, 25), (76, 26)):
        R(c, x, y, 5, 3, "q"); put(c, x, y, "1"); put(c, x + 4, y + 2, "1")
    # poles
    for x in (20, 100):
        for y in range(bot, 76):
            put(c, x, y, "h"); put(c, x + 1, y, "g")
        put(c, x, 76, "2"); put(c, x + 1, 76, "2")
    # lantern hanging under the awning (x=60)
    lx = 60
    put(c, lx, 32, "1"); put(c, lx, 33, "1")
    R(c, lx - 2, 34, 5, 1, "1")
    flame = [("w", "x"), ("x", "x"), ("w", "w"), ("x", "w")][f]
    R(c, lx - 2, 35, 5, 5, "1")
    R(c, lx - 1, 35, 3, 4, flame[0])
    put(c, lx, 36, flame[1]); put(c, lx, 37, "x" if f != 2 else "w")
    R(c, lx - 2, 40, 5, 1, "2")
    # light spill on the counter/goods (radius flickers)
    rad = [11, 13, 10, 12][f]
    for y in range(30, 56):
        for x in range(lx - rad, lx + rad + 1):
            d = math.hypot(x - lx, (y - 37) * 1.3)
            if d > rad or not (0 <= x < W):
                continue
            k = get(c, x, y)
            lit = {"2": "3", "3": "4", "4": "5", "6": "7", "s": "t", "t": "u", "n": "o", "g": "h"}
            if k in lit and d < rad * 0.65:
                put(c, x, y, lit[k])


def roof(c, f):
    # tire pile + crate + antenna with a flag
    for (x, y) in ((30, 18), (36, 18), (33, 14)):
        R(c, x, y, 7, 5, "2"); R(c, x + 2, y + 1, 3, 3, "1"); R(c, x, y, 7, 1, "g")
    R(c, 70, 13, 14, 10, "5"); R(c, 70, 13, 14, 1, "6"); R(c, 76, 13, 1, 10, "4"); R(c, 70, 18, 14, 1, "4")
    R(c, 86, 17, 10, 6, "h"); R(c, 86, 17, 10, 1, "j")
    for y in range(2, 23):
        put(c, 112, y, "h")
    flags = [["uuuut", "uuut.", "uut.."], ["uuuuu", "uuuut", "tt..."], ["uuut.", "uuuut", "uuut."],
             ["uuuuu", "uut..", "uuut."]][f]
    for j, row in enumerate(flags):
        for i, k in enumerate(row):
            if k != ".":
                put(c, 113 + i, 3 + j, k)


def stall(f):
    c = bus_body()
    wheels(c)
    roof(c, f)
    awning(c, f)
    sign = blank(W, H)
    R(sign, 98, 9, 20, 14, "3"); R(sign, 98, 9, 20, 1, "4"); R(sign, 98, 22, 20, 1, "2")
    for yy in range(23, 24):
        put(sign, 101, yy, "2"); put(sign, 114, yy, "2")
    blit(sign, NUT_SIGN, 102, 11)
    sign = outline(sign, "1")
    c = outline(c, "1")
    under(c, sign, 0, 0) if False else blit(c, sign, 0, 0)
    return c


if __name__ == "__main__":
    a = Asset("Assets/Art/Environment/shop_stall.aseprite", W, H, ["stall"])
    a.tag("idle", [stall(f) for f in range(4)], 200)
    a.preview_bg = (150, 132, 100)
    if "--export" in sys.argv:
        export(a, "environment", s=3)
    else:
        write_previews(a, "environment", s=3)
        zoom_png(stall(0), os.path.join(PREVIEWS, "tmp", "stall_4x.png"), s=4, bg=(150, 132, 100))
