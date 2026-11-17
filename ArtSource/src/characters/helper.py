"""Helper — Assets/Art/Characters/helper.aseprite (32x32, facing left).
Small survivor worker with a big wrench. idle(4x160) swing(4x90, impact = 3rd frame)."""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "customers"))
from rig import *


def P(s, w=None):
    return parse(s, pad=w)


HELPER = Char(
    head=P("""
...hhhh...
..hiiiihg.
.hjiiiihgg
.gggggggg.
..77665g..
..71665...
.7666654..
..66665...
...555....
""", 10),
    torso=P("""
..kkkk...
.vvkkuut.
vvuvkuut.
vuuvkuut.
vuuukuut.
vuuuuuts.
.3343333.
.iiiiiih.
""", 9),
    pants="hgg", shoe="3", sleeve="ihh", eye=(3, 5), mouth=(3, 7),
    head_at=(11, 5), torso_at=(12, 13), shoulder=(16, 15), leg_len=0.8, arm_len=0.9, hand="6",
    height=2,
)
# legs are shorter: hip lower
WR_UP = P("""
k.k
kjj
.j.
.j.
.i.
.i.
.h.
""", 3)
WR_DIAG = P("""
k.....
kj....
.jj...
...j..
....i.
.....h
""", 6)
WR_LEFT = P("""
kk......
.jjjiihh
kj......
""", 8)
WR_DOWN = P("""
.h.
.i.
.i.
.j.
.j.
kjj
k.k
""", 3)


def frame(bob, hand, prop, off, legs=None, far=(1, 6), head=(0, 0), face=None):
    return compose(HELPER, bob, legs, (hand, far), head_d=head, face=face, prop=(prop, off))


def helper_asset():
    a = Asset("Assets/Art/Characters/helper.aseprite", 32, 32, ["body"])
    idle = []
    for i, b in enumerate([0, 0, 1, 1]):
        idle.append(frame(b, (-1, 6), WR_DOWN, (-1, -1), face=blink(HELPER) if i == 3 else None))
    a.tag("idle", idle, 160)
    sw = [
        frame(0, (2, -6), WR_UP, (-1, -6), head=(1, 0), legs=(((-2, 5), (-3, 0)), ((2, 5), (3, 0)))),
        frame(0, (-4, -4), WR_DIAG, (-5, -5), head=(-1, 0), legs=(((-2, 5), (-3, 0)), ((2, 5), (3, 0)))),
        frame(1, (-6, 2), WR_LEFT, (-7, -1), head=(-1, 1), legs=(((-3, 4), (-5, 0)), ((2, 4), (4, 0))),
              face=frown(HELPER)),
        frame(1, (-3, 6), WR_DOWN, (-2, -1), legs=(((-2, 4), (-3, 0)), ((2, 4), (3, 0)))),
    ]
    a.tag("swing", sw, 90)
    return a


if __name__ == "__main__":
    a = helper_asset()
    a.preview_bg = (128, 120, 112)
    if "--export" in sys.argv:
        export(a, "characters", s=6)
    else:
        write_previews(a, "characters", s=6)
