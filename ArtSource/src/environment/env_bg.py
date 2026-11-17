"""Background strips 384x96 (+ ground 384x16). All tile seamlessly in X (wrap at 384).
Floor line: y=80 (16px from bottom of the 96px window)."""
import sys, os, math
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "tools"))
from px import *
from pose import poly_fill, polyline, disc

W, H = 384, 96
FLOOR = 80
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0


def wput(img, x, y, key):
    put(img, x % W, y, key)


# ================================================================== sky
SKY_BANDS = [("g", 0), ("h", 16), ("m", 34), ("n", 50), ("o", 63), ("w", 74), ("x", 86)]


def sky():
    img = blank(W, H)
    for y in range(H):
        # find band
        for i in range(len(SKY_BANDS) - 1):
            k0, y0 = SKY_BANDS[i]
            k1, y1 = SKY_BANDS[i + 1]
            if y0 <= y < y1 or i == len(SKY_BANDS) - 2:
                break
        # transition zone: last 5 rows before y1 dither toward k1
        zone = 5
        t = (y - (y1 - zone)) / zone
        for x in range(W):
            if t > 0 and BAYER[y % 4, x % 4] < t * 0.85:
                img[y, x] = K[k1]
            else:
                img[y, x] = K[k0]
    # smog clouds: soft elongated blobs, dithered rims
    clouds = [(30, 20, 110, 3, "h"), (190, 28, 120, 3, "m"), (290, 18, 70, 2, "h"), (70, 44, 130, 3, "n"),
              (250, 50, 110, 2, "m"), (10, 62, 90, 2, "o"), (150, 66, 140, 3, "n")]
    for (x0, y0, L, th, k) in clouds:
        for j in range(L):
            u = j / (L - 1)
            hh = th * math.sin(math.pi * u) ** 0.6
            for dy in range(-int(math.ceil(hh)), int(math.ceil(hh)) + 1):
                fall = abs(dy) / max(0.5, hh)
                if fall > 1:
                    continue
                if BAYER[(y0 + dy) % 4, (x0 + j) % 4] < 1.15 - fall:
                    wput(img, x0 + j, y0 + dy, k)
    # hazy sun low on the right, cut by smog bands
    sx, sy = 341, 50
    for y in range(sy - 10, sy + 11):
        for x in range(sx - 10, sx + 11):
            d = math.hypot(x - sx, (y - sy) * 1.05)
            if d <= 6.5:
                img[y, x] = K["x"]
            elif d <= 9.5 and img[y, x] != K["x"]:
                if BAYER[y % 4, x % 4] < (9.5 - d) / 3.0:
                    img[y, x] = K["x"] if img[y, x] == K["w"] else K["w"]
    for (y, x0, x1) in [(48, 330, 352), (52, 328, 354), (53, 332, 350)]:
        for x in range(x0, x1):
            if img[y, x] == K["x"] and (x + y) % 3:
                img[y, x] = K["w"]
    return img


# ================================================================== far skyline
FAR = [  # x, width, height, top
    (0, 14, 30, "flat"), (12, 10, 44, "broken"), (24, 18, 26, "flat"), (40, 8, 52, "spire"),
    (50, 22, 34, "tilt"), (70, 12, 22, "flat"), (82, 16, 40, "broken"), (100, 26, 28, "flat"),
    (124, 10, 48, "antenna"), (136, 20, 20, "flat"), (156, 14, 38, "broken"), (172, 24, 30, "tilt"),
    (198, 10, 56, "spire"), (210, 18, 24, "flat"), (228, 14, 42, "broken"), (244, 30, 18, "flat"),
    (276, 12, 36, "antenna"), (290, 20, 28, "broken"), (312, 16, 46, "tilt"), (330, 22, 24, "flat"),
    (352, 12, 38, "broken"), (366, 18, 30, "flat"),
]


def building(img, x, w, h, top, fill, dark, light, win, seed):
    rng = np.random.default_rng(seed)
    base = FLOOR + 4
    h = int(h * 0.8)
    yt = base - h
    for xx in range(x, x + w):
        for yy in range(yt, base):
            wput(img, xx, yy, fill)
    if top == "broken":  # jagged collapsed top
        cut = rng.integers(3, 9)
        for i in range(w):
            depth = int(abs(math.sin(i * 0.9 + seed)) * cut) + (i * cut // w if seed % 2 else (w - i) * cut // w)
            for yy in range(yt, yt + depth):
                wput(img, x + i, yy, ".")
    elif top == "tilt":  # slanted roof slab
        for i in range(w):
            for yy in range(yt, yt + (w - i) // 3):
                wput(img, x + i, yy, ".")
    elif top == "spire":
        for i in range(w):
            for yy in range(yt - 10 + abs(i - w // 2) * 3, yt):
                wput(img, x + i, yy, fill)
    elif top == "antenna":
        for yy in range(yt - 12, yt):
            wput(img, x + w // 2, yy, fill)
        for yy in range(yt - 6, yt):
            wput(img, x + w // 2 - 2, yy, fill)
        wput(img, x + w // 2, yt - 13, "u")
    # left rim light
    for yy in range(yt, base):
        if get(img, x % W, yy) == fill:
            wput(img, x, yy, light)
    # windows
    for yy in range(yt + 4, base - 3, 4):
        for xx in range(x + 2, x + w - 2, 3):
            if get(img, xx % W, yy) != fill:
                continue
            r = rng.random()
            if r < 0.35:
                wput(img, xx, yy, dark)
            elif r < 0.39:
                wput(img, xx, yy, win)
    return img


def far():
    img = blank(W, H)
    for i, (x, w, h, top) in enumerate(FAR):
        building(img, x, w, h, top, "m", "h", "n", "w", i * 7 + 3)
    # a broken crane
    polyline(img, [(258, 84), (258, 40)], "m"); polyline(img, [(259, 84), (259, 40)], "m")
    polyline(img, [(240, 42), (282, 38)], "m")
    polyline(img, [(282, 38), (282, 52)], "h")
    for i in range(0, 40, 4):
        put(img, 258 - (i % 8 == 0), 80 - i, "h")
    return img


# ================================================================== mid ruins


def mid():
    img = blank(W, H)
    rng = np.random.default_rng(11)
    # broken wall chunks with window holes
    walls = [(8, 34, 30), (96, 28, 22), (150, 40, 34), (236, 26, 26), (318, 36, 20)]
    for (x, w, h) in walls:
        top = [(x, FLOOR + 3), (x, FLOOR - h + 6), (x + 4, FLOOR - h), (x + w // 3, FLOOR - h + 3),
               (x + w // 2, FLOOR - h - 3), (x + 2 * w // 3, FLOOR - h + 8), (x + w, FLOOR - h + 12), (x + w, FLOOR + 3)]
        m = poly_fill(blank(W + 64, H), top, "3", mask_only=True)
        ys, xs = np.where(m)
        for yy, xx in zip(ys, xs):
            img[yy, xx % W] = K["3"]
        # brick courses + holes
        for yy in range(FLOOR - h, FLOOR + 3):
            for xx in range(x, x + w):
                if img[yy, xx % W] != K["3"]:
                    continue
                if xx - x < 2:
                    img[yy, xx % W] = K["4"]
                elif xx - x > w - 4:
                    img[yy, xx % W] = K["2"]
        for (hx, hy) in [(x + 5, FLOOR - h + 10), (x + w - 12, FLOOR - h + 14)]:
            for yy in range(hy, hy + 7):
                for xx in range(hx, hx + 5):
                    if img[yy, xx % W] >= 0 and FLOOR - h + 6 < yy:
                        img[yy, xx % W] = T
        # lit top edge
        for xx in range(x, x + w):
            col = np.where(img[:, xx % W] >= 0)[0]
            col = col[col < FLOOR]
            if len(col):
                img[col[0], xx % W] = K["5"]
    # utility poles + sagging wires
    poles = [(64, 44), (132, 40), (214, 46), (292, 42), (372, 44)]
    for (x, h) in poles:
        for yy in range(FLOOR - h, FLOOR + 4):
            wput(img, x, yy, "2"); wput(img, x + 1, yy, "3")
        for xx in range(x - 4, x + 6):
            wput(img, xx, FLOOR - h + 3, "2")
        wput(img, x - 4, FLOOR - h + 2, "g"); wput(img, x + 5, FLOOR - h + 2, "g")
    for i in range(len(poles)):
        (x0, h0), (x1, h1) = poles[i], poles[(i + 1) % len(poles)]
        if x1 < x0:
            x1 += W
        for side in (-4, 5):
            a = (x0 + side, FLOOR - h0 + 2)
            b = (x1 + side, FLOOR - h1 + 2)
            n = b[0] - a[0]
            for j in range(n + 1):
                t = j / max(1, n)
                sag = 6 * math.sin(math.pi * t)
                wput(img, int(a[0] + j), int(round(a[1] + (b[1] - a[1]) * t + sag)), "2")
    # rubble mounds
    for (x, w) in [(40, 30), (118, 24), (196, 34), (270, 28), (350, 30)]:
        for xx in range(w):
            hh = int(5 * math.sin(math.pi * xx / w) + rng.integers(0, 2))
            for yy in range(FLOOR + 3 - hh, FLOOR + 4):
                wput(img, x + xx, yy, "4" if yy == FLOOR + 3 - hh else ("3" if (xx + yy) % 4 else "g"))
    # billboard frame
    for xx in range(170, 206):
        wput(img, xx, 36, "2"); wput(img, xx, 54, "2")
    for yy in range(36, FLOOR + 3):
        wput(img, 174, yy, "2"); wput(img, 200, yy, "2")
    for yy in range(37, 54):
        for xx in range(171, 205):
            if not (186 < xx < 196 and 42 < yy < 50):
                wput(img, xx, yy, "4" if yy < 46 else "3")
            if yy == 37 and xx < 204:
                wput(img, xx, yy, "5")
            if 39 < yy < 44 and 176 < xx < 186:
                wput(img, xx, yy, "t" if yy < 42 else "s")
    return img


# ================================================================== ground strip 384x16


def ground():
    img = blank(W, 16)
    rng = np.random.default_rng(5)
    # irregular top edge: 0..2
    top = [int(1 + math.sin(x * 0.11) + math.sin(x * 0.37 + 1) * 0.6) for x in range(W)]
    for x in range(W):
        for y in range(max(0, top[x]), 16):
            img[y, x] = K["4"] if y < 4 else (K["3"] if y < 10 else K["2"])
        img[max(0, top[x]), x] = K["5"]
        if top[x] + 1 < 16:
            img[top[x] + 1, x] = K["4"]
    # dither between bands
    for y in (3, 4, 9, 10):
        for x in range(W):
            if BAYER[y % 4, x % 4] < 0.5:
                img[y, x] = K["3"] if y < 6 else K["2"]
    # cracked asphalt slabs
    for (x0, w) in [(20, 46), (110, 60), (210, 38), (290, 70)]:
        for x in range(x0, x0 + w):
            for y in range(6, 13):
                if (x - x0) in (0, w - 1) or y in (6, 12):
                    img[y, x % W] = K["2"]
                elif y == 7:
                    img[y, x % W] = K["h"]
                else:
                    img[y, x % W] = K["g"]
        cx = x0 + w // 3
        for k in range(6):
            img[6 + k, (cx + (k % 2)) % W] = K["1"]
    # pebbles + dead grass tufts
    for _ in range(60):
        x, y = int(rng.integers(0, W)), int(rng.integers(3, 15))
        img[y, x] = K["j"] if rng.random() < 0.5 else K["i"]
        if y + 1 < 16:
            img[y + 1, x] = K["h"]
    for _ in range(26):
        x = int(rng.integers(0, W))
        y = top[x]
        for dx, hgt in [(0, 2), (1, 3), (2, 1)]:
            for k in range(hgt):
                if y - k >= 0:
                    img[y - k, (x + dx) % W] = K["r"] if k else K["q"]
    return img


if __name__ == "__main__":
    s, f, m, g = sky(), far(), mid(), ground()
    comp = s.copy()
    blit(comp, f, 0, 0); blit(comp, m, 0, 0); blit(comp, g, 0, FLOOR)
    zoom_png(comp, os.path.join(PREVIEWS, "tmp", "env_comp.png"), s=3)


def build_all():
    out = []
    for name, fn, h in (("bg_sky", sky, 96), ("bg_far", far, 96), ("bg_mid", mid, 96), ("ground", ground, 16)):
        a = Asset(f"Assets/Art/Environment/{name}.aseprite", W, h, [name])
        a.add(fn(), 100)
        a.preview_bg = (150, 132, 100) if name != "bg_sky" else BG_DEFAULT
        out.append(a)
    return out


if "--export" in sys.argv:
    for a in build_all():
        export(a, "environment", s=2)
