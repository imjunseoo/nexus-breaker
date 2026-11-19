"""Repairman — Assets/Art/Characters/repairman.aseprite (48x48).
Tags: idle(4x150) swing(6x70, impact=4th) swing_crit(8x60, impact=5th) cheer(4x120).
--heavy builds the alternative heavy-maul take into ArtSource/compare/claude_heavy/."""
import sys, os, shutil
sys.path.insert(0, os.path.dirname(__file__))
from rm_parts import *


def head_variant(kind):
    h = HEAD_F.copy()
    if kind == "effort":  # squint + gritted teeth
        put(h, 6, 10, "1"); put(h, 7, 10, "1"); put(h, 8, 10, "6")
        put(h, 6, 9, "6"); put(h, 7, 9, "2"); put(h, 8, 9, "2")
        put(h, 6, 13, "1"); put(h, 7, 13, "l"); put(h, 8, 13, "k"); put(h, 9, 13, "1")
    if kind == "joy":  # happy closed eye + open mouth
        put(h, 6, 10, "6"); put(h, 7, 10, "1"); put(h, 8, 10, "6")
        put(h, 6, 9, "6"); put(h, 7, 9, "6"); put(h, 8, 9, "2"); put(h, 6, 10, "1"); put(h, 8, 10, "1")
        put(h, 6, 13, "1"); put(h, 7, 13, "s"); put(h, 8, 13, "1"); put(h, 7, 12, "1")
    return h


def patch(img, x, y, grid):
    """grid chars: '.' keep, '_' clear, else palette key."""
    rows = [r.strip() for r in grid.strip("\n").split("\n")]
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch == ".":
                continue
            put(img, x + i, y + j, "." if ch == "_" else ch)
    return img


def frame(tx, ty, hx, hy, legs, lx, ly, far, ham, near, ham_front=True, mask=(0, 0),
          face="normal", pouch=(0, 0), smear=None):
    c = blank(48, 48)
    blit(c, legs, lx, ly)
    blit(c, TORSO_F, 18 + tx, 28 + ty)
    blit(c, head_variant(face), 11 + tx + hx, 14 + ty + hy)
    c = outline(c, "1")
    blit(c, POUCH, 28 + tx + pouch[0], 35 + ty + pouch[1])
    if far:
        under(c, arm_layer(*far, far=True), 0, 0)
    if smear is not None:
        under(c, smear, 0, 0)
    if not ham_front:
        under(c, hammer_layer(*ham), 0, 0)
    blit(c, MASK, 18 + tx + hx + mask[0], 28 + ty + hy + mask[1])
    if ham_front:
        blit(c, hammer_layer(*ham), 0, 0)
    blit(c, arm_layer(*near), 0, 0)
    return c


def smear_arc(cx, cy, r, a0, a1, width=6):
    """motion smear trailing toward angle a1 (head end, fattest). degrees, 0=right, 90=down."""
    import math
    L = blank(48, 48)
    n = 40
    for i in range(n):
        t = i / (n - 1)
        a = math.radians(a0 + (a1 - a0) * t)
        w = 1.0 + (width - 1.0) * (t ** 1.5)
        key = "i" if t < 0.35 else ("j" if t < 0.75 else "k")
        disc(L, cx + math.cos(a) * r, cy + math.sin(a) * r, w / 2, key)
    return L


# ----------------------------------------------------------------- poses (game build)
IDLE_HAM = (11, 32, 11, 44, HEAD_D, 5, 2)


def idle_frames():
    out = []
    for ty, hy, my in [(0, 0, 0), (1, -1, 0), (1, 0, 1), (0, 1, 1)]:
        out.append(frame(0, ty, 0, hy, LEGS_STAND, 14, 38,
                         (26, 31 + ty, 20, 34 + ty, 13, 31),
                         IDLE_HAM,
                         (24, 32 + ty, 18, 35 + ty, 12, 32),
                         mask=(0, my), pouch=(0, -ty if ty else 0)))
    return out


def swing_frames():
    F = []
    F.append(frame(1, 1, 1, 0, LEGS_CROUCH, 14, 39,
                   (27, 32, 25, 36, 21, 34), (19, 36, 31, 24),
                   (25, 33, 22, 37, 19, 35)))
    F.append(frame(1, -1, 1, 0, LEGS_STAND, 14, 38,
                   (28, 29, 30, 22, 28, 16), (28, 18, 30, 3, HEAD_U, 5, 2),
                   (26, 30, 29, 23, 27, 17), ham_front=False, mask=(1, 1), pouch=(0, 1)))
    F.append(frame(-1, 0, -2, 1, LEGS_STAND, 14, 38,
                   (25, 31, 22, 25, 18, 20), (19, 21, 9, 9),
                   (23, 31, 20, 26, 17, 21), mask=(1, 0), face="effort",
                   smear=smear_arc(18, 21, 14.5, -40, -118, 8)))
    F.append(frame(-2, 2, -3, 2, LEGS_LUNGE, 11, 38,
                   (23, 33, 20, 36, 16, 33), (17, 33, 4, 33, HEAD_L, 2, 5),
                   (22, 34, 19, 37, 15, 33), mask=(-1, 0), face="effort", pouch=(0, -1)))
    F.append(frame(-2, 1, -2, 1, LEGS_LUNGE, 11, 38,
                   (23, 32, 20, 35, 16, 31), (17, 31, 5, 26),
                   (22, 33, 19, 36, 15, 31), mask=(-1, 1), pouch=(0, 1)))
    F.append(frame(-1, 1, -1, 1, LEGS_CROUCH, 14, 39,
                   (25, 32, 22, 36, 17, 35), (17, 35, 12, 43, HEAD_D, 5, 2),
                   (24, 33, 21, 37, 16, 35), mask=(0, 1)))
    return F


def crit_frames():
    F = []
    # 1 deep crouch, hammer cocked on the shoulder
    F.append(frame(1, 2, 1, 1, LEGS_CROUCH, 14, 39,
                   (27, 33, 25, 37, 21, 35), (19, 37, 32, 26),
                   (25, 34, 22, 38, 19, 36), face="effort"))
    # 2 leap, hammer swinging up and back
    F.append(frame(1, -3, 1, 0, LEGS_CROUCH, 14, 35,
                   (27, 27, 29, 21, 27, 15), (27, 16, 37, 5),
                   (25, 28, 28, 22, 26, 16), ham_front=False, mask=(1, 1), pouch=(0, 1)))
    # 3 apex, arched back, hammer fully loaded behind
    F.append(frame(1, -4, 2, 0, LEGS_CROUCH, 14, 34,
                   (27, 26, 30, 20, 30, 14), (31, 15, 42, 10),
                   (25, 27, 29, 21, 29, 15), ham_front=False, mask=(1, 2), pouch=(0, 2)))
    # 4 downswing smear, dropping
    F.append(frame(-1, -2, -2, 1, LEGS_STAND, 14, 36,
                   (25, 29, 22, 23, 18, 18), (18, 19, 8, 8),
                   (23, 29, 20, 24, 17, 19), mask=(1, -1), face="effort", pouch=(0, -1),
                   smear=smear_arc(18, 19, 15, 0, -118, 8)))
    # 5 IMPACT: heavy squash, hammer low at the front
    F.append(frame(-2, 3, -3, 3, LEGS_LUNGE, 11, 38,
                   (23, 34, 20, 37, 16, 35), (16, 35, 3, 37, HEAD_L, 2, 5),
                   (22, 35, 19, 38, 15, 35), mask=(-1, 0), face="effort", pouch=(0, -2)))
    # 6 impact hold (shake)
    f = frame(-2, 3, -3, 3, LEGS_LUNGE, 11, 38,
              (23, 34, 20, 37, 16, 35), (16, 35, 3, 37, HEAD_L, 2, 5),
              (22, 35, 19, 38, 15, 35), mask=(-1, 1), face="effort", pouch=(0, -1))
    g = blank(48, 48); blit(g, f[:, :47], 1, 0); F.append(g)
    # 7 recoil
    F.append(frame(-2, 2, -2, 1, LEGS_LUNGE, 11, 38,
                   (23, 33, 20, 36, 16, 32), (17, 32, 6, 26),
                   (22, 34, 19, 37, 15, 32), mask=(-1, 1), pouch=(0, 1)))
    # 8 recover
    F.append(frame(-1, 1, -1, 1, LEGS_CROUCH, 14, 39,
                   (25, 32, 22, 36, 17, 35), (17, 35, 12, 43, HEAD_D, 5, 2),
                   (24, 33, 21, 37, 16, 35), mask=(0, 1)))
    return F


def cheer_frames():
    F = []
    # 1 crouch, hammer on the shoulder
    F.append(frame(1, 1, 1, 0, LEGS_CROUCH, 14, 39,
                   (27, 32, 25, 36, 21, 34), (19, 36, 31, 24),
                   (25, 33, 22, 37, 19, 35), face="joy"))
    # 2 hop, hammer thrust up
    F.append(frame(1, -3, 1, -1, LEGS_CROUCH, 14, 36,
                   (28, 26, 30, 19, 28, 13), (28, 15, 28, 2, HEAD_U, 5, 2),
                   (26, 27, 29, 20, 27, 14), ham_front=False, face="joy", mask=(1, 1), pouch=(0, 1)))
    # 3 apex, hammer tipped forward a touch
    F.append(frame(1, -4, 1, -1, LEGS_CROUCH, 14, 35,
                   (28, 25, 30, 18, 28, 12), (28, 14, 24, 5),
                   (26, 26, 29, 19, 27, 13), ham_front=False, face="joy", mask=(1, 2), pouch=(0, 2)))
    # 4 land
    F.append(frame(1, 2, 1, 1, LEGS_CROUCH, 14, 39,
                   (27, 33, 25, 37, 21, 35), (19, 37, 31, 25),
                   (25, 34, 22, 38, 19, 36), face="joy", mask=(0, -1), pouch=(0, -1)))
    return F


# ----------------------------------------------------------------- heavy maul variant (alternative)
# heavy maul: head 13x7 (HEAD_U/D centre 6,3; HEAD_L centre 3,6), 3px handle
IDLE_HAM_HEAVY = (11, 31, 11, 43, HEAD_D_HEAVY, 6, 3)

DUST_S = parse("""
.lk..
lkjk.
kjjik
.iii.
""")
DUST_M = parse("""
..lk....
.lkkjk..
lkjjjjk.
kjjiijjk
.iiiiii.
""")
DUST_L = parse("""
...lkk.....
.lkkjjkk...
lkjjjjjjk..
kjjiijjjjk.
kjiiiiiijjk
.iiiiiiiii.
""")


def dust(img, pts):
    """ground puffs (no outline). Drawn over empty pixels and over the ground line only,
    never over the character."""
    for spr, x, y in pts:
        h, w = spr.shape
        for j in range(h):
            for i in range(w):
                v = spr[j, i]
                X, Y = x + i, y + j
                if v >= 0 and 0 <= X < 48 and 0 <= Y < 48 and (img[Y, X] < 0 or Y >= 46):
                    img[Y, X] = v
    return img


def idle_frames_heavy():
    out = []
    for ty, hy, my in [(0, 0, 0), (1, -1, 0), (1, 0, 1), (0, 1, 1)]:
        out.append(frame(0, ty, 0, hy, LEGS_STAND, 14, 38,
                         (26, 31 + ty, 20, 34 + ty, 13, 31),
                         IDLE_HAM_HEAVY,
                         (24, 32 + ty, 18, 35 + ty, 12, 32),
                         mask=(0, my), pouch=(0, -ty if ty else 0)))
    return out


# frame 4 = impact must still START at 3*70 = 210 ms and the clip stays 420 ms;
# the time is redistributed for weight: slow heave, hang at the top, snap down,
# hold the impact, let the maul drop, settle.
SWING_MS = [80, 100, 30, 100, 60, 50]
# impact = 5th frame, starts at 4*60 = 240 ms; total 480 ms
CRIT_MS = [70, 60, 80, 30, 90, 60, 50, 40]


def swing_frames_heavy():
    F = []
    # 1 heave the maul onto the shoulder: deep crouch, strain
    F.append(frame(1, 2, 1, 1, LEGS_CROUCH, 14, 39,
                   (27, 33, 25, 37, 21, 35), (19, 37, 32, 25),
                   (25, 34, 22, 38, 19, 36), face="effort"))
    # 2 hang at the top: arched back, maul tipping behind
    F.append(frame(2, -1, 2, 0, LEGS_STAND, 14, 38,
                   (28, 29, 30, 22, 29, 16), (29, 17, 34, 7),
                   (26, 30, 29, 23, 28, 17), ham_front=False, mask=(1, 1), pouch=(0, 1), face="effort"))
    # 3 snap down (smear), body thrown forward by the weight
    F.append(frame(-1, 0, -2, 1, LEGS_STAND, 14, 38,
                   (25, 31, 22, 25, 18, 20), (19, 21, 9, 10),
                   (23, 31, 20, 26, 17, 21), mask=(1, 0), face="effort",
                   smear=smear_arc(18, 21, 14.5, -30, -122, 10)))
    # 4 IMPACT: maul buried low in front, full squash, dust kicks up
    f = frame(-2, 3, -3, 3, LEGS_LUNGE, 11, 38,
              (23, 34, 20, 37, 16, 35), (16, 35, 4, 36, HEAD_L_HEAVY, 3, 6),
              (22, 35, 19, 38, 15, 35), mask=(-1, 0), face="effort", pouch=(0, -2))
    F.append(dust(f, [(DUST_M, 1, 43), (DUST_S, 34, 43)]))
    # 5 too heavy to bounce: it drops onto the ground, he follows it down
    f = frame(-2, 3, -3, 2, LEGS_LUNGE, 11, 38,
              (23, 34, 20, 37, 17, 36), (17, 36, 8, 42),
              (22, 35, 19, 38, 16, 36), mask=(-1, 1), pouch=(0, 0))
    F.append(dust(f, [(DUST_L, 0, 42), (DUST_M, 33, 43)]))
    # 6 settle: straighten and lean on the handle (-> idle)
    f = frame(-1, 1, -1, 1, LEGS_CROUCH, 14, 39,
              (25, 32, 21, 35, 14, 32), (12, 32, 10, 43, HEAD_D_HEAVY, 6, 3),
              (24, 33, 19, 36, 13, 33), mask=(0, 1))
    F.append(dust(f, [(DUST_S, 0, 41)]))
    return F


def crit_frames_heavy():
    F = []
    # 1 deep crouch, maul cocked on the shoulder
    F.append(frame(1, 2, 1, 1, LEGS_CROUCH, 14, 39,
                   (27, 33, 25, 37, 21, 35), (19, 37, 32, 26),
                   (25, 34, 22, 38, 19, 36), face="effort"))
    # 2 leap, maul swinging up and back
    F.append(frame(1, -3, 1, 0, LEGS_CROUCH, 14, 35,
                   (27, 27, 29, 21, 27, 15), (27, 16, 36, 7),
                   (25, 28, 28, 22, 26, 16), ham_front=False, mask=(1, 1), pouch=(0, 1)))
    # 3 apex hang, arched back, maul fully loaded behind
    F.append(frame(1, -4, 2, 0, LEGS_CROUCH, 14, 34,
                   (27, 26, 30, 20, 30, 14), (31, 15, 40, 12),
                   (25, 27, 29, 21, 29, 15), ham_front=False, mask=(1, 2), pouch=(0, 2), face="effort"))
    # 4 downswing smear, dropping
    F.append(frame(-1, -2, -2, 1, LEGS_STAND, 14, 36,
                   (25, 29, 22, 23, 18, 18), (18, 19, 8, 9),
                   (23, 29, 20, 24, 17, 19), mask=(1, -1), face="effort", pouch=(0, -1),
                   smear=smear_arc(18, 19, 15, 0, -120, 11)))
    # 5 IMPACT: heaviest squash, maul low at the front, big dust
    f = frame(-2, 3, -3, 3, LEGS_LUNGE, 11, 38,
              (23, 34, 20, 37, 16, 35), (16, 35, 3, 38, HEAD_L_HEAVY, 3, 6),
              (22, 35, 19, 38, 15, 35), mask=(-1, 0), face="effort", pouch=(0, -2))
    F.append(dust(f, [(DUST_L, 0, 42), (DUST_M, 33, 43)]))
    # 6 impact hold (1px shake), dust rising
    f = frame(-2, 3, -3, 3, LEGS_LUNGE, 11, 38,
              (23, 34, 20, 37, 16, 35), (16, 35, 3, 38, HEAD_L_HEAVY, 3, 6),
              (22, 35, 19, 38, 15, 35), mask=(-1, 1), face="effort", pouch=(0, -1))
    dust(f, [(DUST_L, 0, 41), (DUST_L, 33, 42)])
    g = blank(48, 48); blit(g, f[:, :47], 1, 0); F.append(g)
    # 7 maul drops to the ground, he is dragged forward
    f = frame(-2, 3, -3, 2, LEGS_LUNGE, 11, 38,
              (23, 34, 20, 37, 17, 36), (17, 36, 8, 42),
              (22, 35, 19, 38, 16, 36), mask=(-1, 1))
    F.append(dust(f, [(DUST_M, 0, 40), (DUST_S, 35, 43)]))
    # 8 settle on the handle
    F.append(frame(-1, 1, -1, 1, LEGS_CROUCH, 14, 39,
                   (25, 32, 21, 35, 14, 32), (12, 32, 10, 43, HEAD_D_HEAVY, 6, 3),
                   (24, 33, 19, 36, 13, 33), mask=(0, 1)))
    return F


def cheer_frames_heavy():
    F = []
    # 1 crouch, maul on the shoulder
    F.append(frame(1, 1, 1, 0, LEGS_CROUCH, 14, 39,
                   (27, 32, 25, 36, 21, 34), (19, 36, 31, 24),
                   (25, 33, 22, 37, 19, 35), face="joy"))
    # 2 hop, maul heaved up
    F.append(frame(1, -3, 1, -1, LEGS_CROUCH, 14, 36,
                   (28, 26, 30, 19, 28, 13), (28, 15, 28, 4, HEAD_U_HEAVY, 6, 3),
                   (26, 27, 29, 20, 27, 14), ham_front=False, face="joy", mask=(1, 1), pouch=(0, 1)))
    # 3 apex, maul tipped forward a touch
    F.append(frame(1, -4, 1, -1, LEGS_CROUCH, 14, 35,
                   (28, 25, 30, 18, 28, 12), (28, 14, 23, 6),
                   (26, 26, 29, 19, 27, 13), ham_front=False, face="joy", mask=(1, 2), pouch=(0, 2)))
    # 4 land heavily
    f = frame(1, 2, 1, 1, LEGS_CROUCH, 14, 39,
              (27, 33, 25, 37, 21, 35), (19, 37, 31, 25),
              (25, 34, 22, 38, 19, 36), face="joy", mask=(0, -1), pouch=(0, -1))
    F.append(dust(f, [(DUST_S, 9, 44), (DUST_S, 33, 44)]))
    return F


def build_asset(heavy=False):
    """heavy=False: the build used by the game. heavy=True: alternative heavy-maul take."""
    HEAVY["on"] = heavy
    out = "ArtSource/compare/claude_heavy/repairman.aseprite" if heavy else "Assets/Art/Characters/repairman.aseprite"
    a = Asset(out, 48, 48, ["body"])
    if heavy:
        a.tag("idle", idle_frames_heavy(), 150)
        a.tag("swing", swing_frames_heavy(), SWING_MS)
        a.tag("swing_crit", crit_frames_heavy(), CRIT_MS)
        a.tag("cheer", cheer_frames_heavy(), 120)
    else:
        a.tag("idle", idle_frames(), 150)
        a.tag("swing", swing_frames(), 70)
        a.tag("swing_crit", crit_frames(), 60)
        a.tag("cheer", cheer_frames(), 120)
    return a


if __name__ == "__main__":
    heavy = "--heavy" in sys.argv
    a = build_asset(heavy)
    cat = "characters_heavy" if heavy else "characters"
    if "--export" in sys.argv:
        export(a, cat)
    else:
        write_previews(a, cat)
