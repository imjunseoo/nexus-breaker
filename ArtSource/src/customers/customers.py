"""8 customers (32x32, facing left). Tags walk(6x100) idle(4x180) buy(4x120) reject(4x120)."""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from rig import *


def P(s, w=None):
    return parse(s, pad=w)


C = {}

# ------------------------------------------------------------ survivor_a: hooded scavenger + backpack
C["survivor_a"] = Char(
    head=P("""
...nnnn...
..noonnnm.
.nonnnnnmm
.n7766nnmm
.n6166nnmm
n76666nnmm
.n6656nmmm
.nn55nnmm.
..rrrrrr..
""", 10),
    torso=P("""
.rrrqqq..
nooonnnm4
noonnnnm4
nonnnnnm4
nnnnnnmm4
nnnnnnmm.
nnnn3nmm.
.nnnnnmm.
.mmmmmmm.
""", 9),
    back=P("""
.444.
45554
45434
45554
44443
.333.
""", 5), back_at=(20, 13),
    pants="ihg", shoe="3", sleeve="nnm", eye=(3, 4), mouth=(3, 6),
)

# ------------------------------------------------------------ survivor_b: bandana, ponytail, red jacket
C["survivor_b"] = Char(
    head=P("""
...uuuuu..
..uvvuutt.
..u3333332
.33766633.
.3616663322
376666632.2
.36656632..
..3556...
""", 11),
    torso=P("""
..55555..
.vuuuuutt
.vuuuuuts
vuuuuuuts
vuuuutuuts
.uuuuuts.
.uuuu1ts.
.hhhhhhg.
.hhhhhgg.
""", 10),
    pants="ihg", shoe="2", sleeve="vut", eye=(3, 4), mouth=(3, 6), head_at=(10, 5), torso_at=(11, 13),
)

# ------------------------------------------------------------ merchant: wide hat, huge pack with pots
C["merchant"] = Char(
    head=P("""
....44444....
...4555554...
.4444444444..
....3333333..
...776663...
...616663...
..7666663...
...665663...
...3333.....
""", 13),
    torso=P("""
..nnnnn..
.onnnnnm.
onnnnnnmm
onnnn4nmm
onnnnnnmm
onn4nnnmm
onnnnnnmm
onnnnnnmm
.mmmmmmm.
""", 9),
    back=P("""
..1j1....
.1jkj1...
.444444..
45555554.
4566665j1
456ttu5jk
4566665j1
45555554.
44444444.
.33..33..
""", 9), back_at=(19, 9),
    pants="455", shoe="2", sleeve="nnm", eye=(4, 5), mouth=(4, 7), head_at=(9, 3),
)

# ------------------------------------------------------------ raider: mohawk, spiked pad, face paint
C["raider"] = Char(
    head=P("""
.....u....
....uu....
...uut....
..23332...
.2666663..
.61t6663..
76666663..
.6t11663..
..6666....
""", 10),
    torso=P("""
..22222...
.1j1j1222.
2jkj32223.
2333332223
.2333t2223
.2333t3223
.2223332..
.3j33333..
.3333332..
""", 10),
    pants="322", shoe="1", sleeve="332", eye=(2, 5), mouth=(3, 7), skin="6",
    height=-1, head_at=(11, 3), torso_at=(12, 12),
)

# ------------------------------------------------------------ soldier: helmet, fatigues, slung rifle
C["soldier"] = Char(
    head=P("""
...qqqqq..
..rqqqqqp.
.rqqqqqqqp
.ppppppppp
..6666ppp.
..61665p..
.766665p..
..66566...
...555....
""", 10),
    torso=P("""
.qqqqqqq.
rqqqqqqqp
rqq3qqqpp
rq333qqpp
rqq3qqqpp
rqqqqqqpp
r3333333p
.qqqqqqp.
.ppppppp.
""", 9),
    back=P("""
........1
.......12
......12.
.....12..
....12...
...12....
..12.....
.12......
""", 9), back_at=(17, 10),
    pants="qqp", shoe="1", sleeve="qqp", eye=(3, 5), mouth=(3, 7), head_at=(11, 4),
)

# ------------------------------------------------------------ cultist: dark hooded robe, nexus pendant
C["cultist"] = Char(
    head=P("""
....222...
..2333222.
.233322222
.235542222
.235142222
.255542222
.234442222
..23322...
""", 10),
    torso=P("""
..33322...
.3333222..
33333322..
33DE33222.
33CE332222
333B332222
33333322222
.333332222.
.33333222..
""", 11),
    pants="322", shoe="1", sleeve="332", eye=(4, 4), mouth=(4, 6), head_at=(11, 5), torso_at=(11, 13),
    leg_len=0.9,
)

# ------------------------------------------------------------ collector: ornate faded coat, top hat, monocle
C["collector"] = Char(
    head=P("""
....1111...
....1221...
....1221...
....1tt1...
..11111111.
...7766g...
...6kw66g..
..766166g..
...66566...
...k555....
""", 11),
    torso=P("""
..wttttw..
.vuutttts.
vuwutttts.
vuuwttts..
vuuutwtss.
vuuuttts..
vuwuttsss.
vuuutttss.
vuuuttss..
.ttssss...
""", 10),
    pants="h21", shoe="1", sleeve="uts", eye=(5, 7), mouth=(4, 8), head_at=(10, 2), torso_at=(11, 13),
    height=-1,
)

# ------------------------------------------------------------ alien: awkward human clothes, neon eyes
C["alien"] = Char(
    head=P("""
...nnnnnn..
..nnnnnnnm.
.mmmmmmmmmm
...0000001.
..0EDE0001.
..00000001.
..0000001..
...00001...
""", 11),
    torso=P("""
...nnnn...
..mnnnnm..
.nnnnnnnm.
nnnnnnnmm.
nnnnn1nmm.
nnnnnnnmm.
nnnnnnnmm.
nnnnn1nmm.
nnnnnnnmm.
.nnnnnnm..
.mmmmmmm..
""", 10),
    pants="211", shoe="0", sleeve="nnm", skin="0", hand="0", eye=(3, 4), mouth=(3, 6),
    head_at=(10, 2), torso_at=(11, 11), height=-2, arm_len=1.25, leg_len=1.15,
)


def customer_assets():
    return {k: build(k, v) for k, v in C.items()}


if __name__ == "__main__":
    only = [a for a in sys.argv[1:] if not a.startswith("--")]
    for k, a in customer_assets().items():
        if only and k not in only:
            continue
        a.preview_bg = (128, 120, 112)
        if "--export" in sys.argv:
            export(a, "customers", s=5)
        else:
            write_previews(a, "customers", s=5)
