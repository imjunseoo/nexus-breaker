"""Side-view (facing LEFT) humanoid rig for 32x32 customers.
Parts per character are hand-drawn ASCII (fill-only); limbs are drawn with clean
thick lines; everything is outlined after composition."""
import sys, os, math
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "tools"))
from px import *
from pose import thick_line, disc

W = H = 32
HIP = (16, 21)  # hip pivot at bob 0
BOB_WALK = [0, 1, 0, 0, 1, 0]
GROUND = 30  # shoe bottom row; outline lands on y=31
# near-leg keyframes: (knee dx,dy rel. hip), (foot dx rel. hip, lift above ground)
LEG_A = [((-2, 5), (-4, 0)), ((-1, 4), (-3, 0)), ((0, 5), (0, 0)),
         ((2, 5), (4, 0)), ((2, 4), (4, 2)), ((-2, 3), (-2, 2))]
ARM_SWING = [(3, 6), (2, 7), (0, 7), (-3, 6), (-2, 7), (0, 7)]


class Char:
    def __init__(self, head, torso, pants="qqp", shoe="2", sleeve="nnm", skin="6",
                 head_at=(11, 4), torso_at=(12, 13), back=None, back_at=(0, 0), front=None, front_at=(0, 0),
                 eye=(2, 4), mouth=(2, 6), shoulder=(16, 15), leg_len=1.0, arm_len=1.0, hand="6",
                 extra=None, height=0, outline_key="1"):
        self.head, self.torso = head, torso
        self.pants, self.shoe, self.sleeve, self.skin = pants, shoe, sleeve, skin
        self.head_at, self.torso_at = head_at, torso_at
        self.back, self.back_at, self.front, self.front_at = back, back_at, front, front_at
        self.eye, self.mouth, self.shoulder = eye, mouth, shoulder
        self.leg_len, self.arm_len, self.hand = leg_len, arm_len, hand
        self.extra = extra  # callable(img, frame_ctx) for props
        self.height = height  # extra vertical offset (taller chars use negative)
        self.outline_key = outline_key


def leg(img, hip, knee, foot, ch, far):
    bands = ch.pants if not far else ch.pants[1:] + ch.pants[-1]
    if far:
        bands = "".join(darker(k) for k in ch.pants)
    hx, hy = hip
    fx, fy = hx + foot[0], GROUND - foot[1]
    kx = hx + knee[0]
    ky = min(hy + knee[1] * ch.leg_len, hy + (fy - hy) * 0.55)
    thick_line(img, hx, hy, kx, ky, bands)
    thick_line(img, kx, ky, fx, fy, bands)
    # shoe: 4px long toe pointing left, 2px tall
    sk = ch.shoe if not far else darker(ch.shoe)
    fx, fy = int(round(fx)), int(round(fy))
    for x in range(fx - 3, fx + 1):
        put(img, x, fy, sk)
    put(img, fx - 2, fy - 1, sk); put(img, fx - 1, fy - 1, sk); put(img, fx, fy - 1, sk)


def arm(img, sh, hand, ch, far):
    sx, sy = sh
    hx, hy = sx + hand[0] * ch.arm_len, sy + hand[1] * ch.arm_len
    bands = ch.sleeve[:2] if not far else "".join(darker(k) for k in ch.sleeve[1:3])
    thick_line(img, sx, sy, hx, hy, bands)
    hk = ch.hand if not far else darker(ch.hand)
    put(img, int(round(hx)), int(round(hy)), hk)
    put(img, int(round(hx)), int(round(hy)) + 1, hk)


def compose(ch, bob=0, legs=None, arms=((0, 7), (0, 7)), head_d=(0, 0), face=None, prop=None,
            ctx=None, arm_front_over=True):
    """legs: ((kneeA,footA),(kneeB,footB)) near/far; arms: (near_hand, far_hand) offsets."""
    if legs is None:
        legs = (((-1, 5), (-1, 0)), ((1, 5), (1, 0)))
    dy = bob + ch.height
    hip = (HIP[0], HIP[1] + dy)
    body = blank(W, H)
    # far limbs first
    far = blank(W, H)
    leg(far, hip, legs[1][0], legs[1][1], ch, True)
    arm(far, (ch.shoulder[0] + 1, ch.shoulder[1] + dy), arms[1], ch, True)
    far = outline(far, ch.outline_key)
    if ch.back is not None:
        b = blank(W, H)
        blit(b, ch.back, ch.back_at[0], ch.back_at[1] + dy)
        b = outline(b, ch.outline_key)
        blit(body, b, 0, 0)
    blit(body, far, 0, 0)
    core = blank(W, H)
    leg(core, hip, legs[0][0], legs[0][1], ch, False)
    blit(core, ch.torso, ch.torso_at[0], ch.torso_at[1] + dy)
    hd = ch.head.copy()
    if face:
        face(hd)
    blit(core, hd, ch.head_at[0] + head_d[0], ch.head_at[1] + dy + head_d[1])
    core = outline(core, ch.outline_key)
    blit(body, core, 0, 0)
    if ch.front is not None:
        f = blank(W, H)
        blit(f, ch.front, ch.front_at[0], ch.front_at[1] + dy)
        blit(body, outline(f, ch.outline_key), 0, 0)
    na = blank(W, H)
    arm(na, (ch.shoulder[0], ch.shoulder[1] + dy), arms[0], ch, False)
    if prop is not None:
        p, (px_, py_) = prop
        hx = ch.shoulder[0] + arms[0][0] * ch.arm_len
        hy = ch.shoulder[1] + dy + arms[0][1] * ch.arm_len
        blit(na, p, int(round(hx)) + px_, int(round(hy)) + py_)
    blit(body, outline(na, ch.outline_key), 0, 0)
    if ch.extra:
        ch.extra(body, dict(bob=dy, **(ctx or {})))
    return body


# ---------------------------------------------------------------- animations
COIN = parse("""
.1.
1w1
.1.
""")
COIN2 = parse("""
.11.
1xw1
1wv1
.11.
""")


def blink(ch):
    ex, ey = ch.eye
    def f(h):
        put(h, ex, ey, ch.skin if get(h, ex, ey) != "." else ".")
        put(h, ex, ey, darker(ch.skin, 2))
    return f


def smile(ch):
    mx, my = ch.mouth
    def f(h):
        put(h, mx, my, "1"); put(h, mx + 1, my, "1")
    return f


def frown(ch):
    mx, my = ch.mouth
    def f(h):
        put(h, mx, my, "1"); put(h, mx + 1, my + 0, "1"); put(h, mx - 0, my - 1, darker(ch.skin))
    return f


def walk(ch):
    out = []
    for i in range(6):
        la = LEG_A[i]
        lb = LEG_A[(i + 3) % 6]
        an = ARM_SWING[i]
        af = ARM_SWING[(i + 3) % 6]
        out.append(compose(ch, BOB_WALK[i], (la, lb), (an, af), ctx=dict(anim="walk", f=i)))
    return out


def idle(ch):
    out = []
    for i, b in enumerate([0, 0, 1, 1]):
        hd = (0, -1 if i == 3 else 0) if False else (0, 0)
        out.append(compose(ch, b, None, ((0, 7), (1, 7)), face=blink(ch) if i == 3 else None,
                           ctx=dict(anim="idle", f=i)))
    return out


def buy(ch):
    out = []
    out.append(compose(ch, 1, None, ((-1, 6), (1, 7)), head_d=(0, 1), face=smile(ch), ctx=dict(anim="buy", f=0)))
    out.append(compose(ch, 0, None, ((-5, 2), (1, 7)), face=smile(ch), prop=(COIN2, (-2, -4)), ctx=dict(anim="buy", f=1)))
    f = compose(ch, -1, (((-1, 4), (-1, 1)), ((1, 4), (1, 1))), ((-5, 1), (1, 6)), face=smile(ch),
                prop=(COIN2, (-2, -4)), ctx=dict(anim="buy", f=2))
    sx, sy = ch.shoulder[0] - 7, ch.shoulder[1] - 4 + ch.height
    for (x, y) in [(sx - 1, sy), (sx + 4, sy - 2), (sx + 1, sy - 3)]:
        if get(f, x, y) == ".":
            put(f, x, y, "x")
    out.append(f)
    out.append(compose(ch, 1, None, ((-1, 6), (1, 7)), head_d=(0, 1), face=smile(ch), ctx=dict(anim="buy", f=3)))
    return out


def reject(ch):
    out = []
    for i, (hx, arm_n) in enumerate([(-1, (-3, 2)), (1, (-3, 1)), (-1, (-3, 2)), (0, (0, 7))]):
        out.append(compose(ch, 0, None, (arm_n, (1, 7)), head_d=(hx, 0), face=frown(ch),
                           ctx=dict(anim="reject", f=i)))
    return out


def build(name, ch):
    a = Asset(f"Assets/Art/Customers/customer_{name}.aseprite", W, H, ["body"])
    a.tag("walk", walk(ch), 100)
    a.tag("idle", idle(ch), 180)
    a.tag("buy", buy(ch), 120)
    a.tag("reject", reject(ch), 120)
    return a
