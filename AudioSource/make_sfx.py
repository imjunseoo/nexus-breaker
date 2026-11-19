"""Render every SFX in Docs/ASSET_SPEC.md section 4 -> Assets/Audio/SFX (WAV 44.1k/16/mono).

Usage: python make_sfx.py [name_filter]
"""
import json
import zlib
import sys
from pathlib import Path

import numpy as np

from nb.dsp import (SR, N, T, rng, db2a, hp, lp, bp, env_exp, env_points, modes, white, sine, saw, pulse, tri,
                    fm, svf, sat, room, resonator, peq, mix_into, fade, colored_noise)
from nb.sfxkit import (add, place, glass_ping, metal_bar, metal_plate, tin_can, thump, fm_bell, chip_note, finish)

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets" / "Audio" / "SFX"


# ============================================================ impacts
def hit(i):
    """main hammer hit, 5 variants: dull transient + metal modes + glassy crystal ring + small room"""
    r = rng(100 + i)
    fs = [1.0, 0.93, 1.07, 0.96, 1.04][i]
    n = N(1.0)
    th = thump(165 * fs, 68 * fs, 0.016, 0.05, n, attack=0.0015)
    knock = bp(white(n, 200 + i), 160, 1000) * env_exp(n, 0.013, 0.0008)
    click = bp(white(n, 300 + i), 900, 3200) * env_exp(n, 0.0022, 0.0004)
    fm_ = 355 * fs * r.uniform(0.97, 1.03)
    metal = metal_bar(fm_, 0.10, n, seed=400 + i, amps=(1.0, 0.45, 0.16, 0.05))
    sets = [(1, 2.31, 3.72), (1, 2.08, 3.41), (1, 2.52, 3.96), (1, 2.19, 3.58), (1, 2.40, 3.85)]
    fcry = [1150, 1040, 1270, 1100, 1210][i] * r.uniform(0.985, 1.015)
    cry = glass_ping(fcry, 0.24, n, seed=500 + i, partials=sets[i], amps=(1.0, 0.3, 0.09), attack=0.003)
    x = 0.95 * th + 0.5 * knock + 0.10 * click + 0.26 * metal + 0.12 * cry
    x = lp(x, 6000, 2)
    x = room(x, rt=0.32, wet=0.13, seed=21)
    return x


def hit_crit():
    r = rng(150)
    n = N(1.3)
    th = thump(140, 50, 0.03, 0.13, n, attack=0.002)
    sub = sine(48.0, n) * env_exp(n, 0.22, 0.004)
    knock = bp(white(n, 151), 120, 1400) * env_exp(n, 0.025, 0.001)
    click = bp(white(n, 152), 800, 3500) * env_exp(n, 0.003, 0.0004)
    metal = metal_bar(300, 0.2, n, seed=153, amps=(1.0, 0.55, 0.25, 0.08))
    plate = metal_plate(410, 0.16, n, seed=154)
    cry1 = glass_ping(1020, 0.4, n, seed=155, partials=(1, 2.27, 3.66), amps=(1, 0.35, 0.12))
    # sparkle: 4 glassy pings rising (pentatonic-ish), soft attacks
    sp = np.zeros(n)
    for k, (f, t0) in enumerate([(1175, 0.035), (1397, 0.08), (1568, 0.13), (1865, 0.19)]):
        mix_into(sp, glass_ping(f, 0.32, seed=160 + k, attack=0.004, amps=(1, 0.22, 0.06)), N(t0), 0.75 ** k)
    # debris crunch: grains
    db = np.zeros(n)
    for k in range(38):
        t0 = r.exponential(0.09)
        if t0 > 0.5:
            continue
        f = r.uniform(900, 3200)
        g = modes(N(0.03), [f, f * 1.7], [1, 0.4], [r.uniform(0.003, 0.009)] * 2, seed=170 + k)
        mix_into(db, g, N(0.01 + t0), r.uniform(0.2, 1.0) * np.exp(-t0 / 0.15))
    body = sat(1.0 * th + 0.55 * sub, 1.6)
    x = body + 0.5 * knock + 0.12 * click + 0.3 * metal + 0.16 * plate + 0.16 * cry1 + 0.13 * sp + 0.08 * db
    x = lp(x, 6500, 2)
    x = room(x, rt=0.9, wet=0.2, seed=22)
    return x


def helper_hit(i):
    r = rng(200 + i)
    fs = [1.0, 1.09, 0.93][i]
    n = N(0.6)
    th = thump(230 * fs, 125 * fs, 0.01, 0.022, n)
    click = bp(white(n, 210 + i), 1000, 3800) * env_exp(n, 0.0018, 0.0003)
    wrench = metal_bar(860 * fs * r.uniform(0.98, 1.02), 0.075, n, seed=220 + i, amps=(1.0, 0.4, 0.1, 0.0))
    cry = glass_ping(1480 * fs, 0.1, n, seed=230 + i, amps=(1, 0.25, 0.05))
    x = 0.7 * th + 0.12 * click + 0.38 * wrench + 0.07 * cry
    x = lp(x, 6000, 2)
    return room(x, rt=0.25, wet=0.1, seed=23)


def nexus_crack():
    r = rng(300)
    n = N(1.6)
    snap = bp(white(n, 301), 400, 3000) * env_exp(n, 0.008, 0.0005)
    pings = np.zeros(n)
    for k in range(7):
        f = r.uniform(850, 2300)
        mix_into(pings, glass_ping(f, r.uniform(0.08, 0.2), seed=310 + k, amps=(1, 0.3, 0.08)), N(r.uniform(0, 0.03)),
                 r.uniform(0.4, 1.0))
    crackle = np.zeros(n)
    t = 0.02
    k = 0
    while t < 0.95:
        f = r.uniform(600, 3200)
        g = modes(N(0.03), [f, f * 1.83], [1, 0.5], [r.uniform(0.002, 0.008)] * 2, seed=330 + k)
        mix_into(crackle, g, N(t), r.uniform(0.3, 1.0) * np.exp(-t / 0.45))
        t += r.exponential(0.012 + 0.05 * t)
        k += 1
    # creak: irregular stick-slip impulse train into resonators
    imp = np.zeros(n)
    t = 0.06
    while t < 0.85:
        imp[N(t)] = r.uniform(0.6, 1.0)
        rate = 34 - 24 * (t / 0.85)
        t += (1.0 / rate) * r.uniform(0.75, 1.3)
    creak = sum(g * resonator(imp, f, 14) for f, g in [(140, 1.0), (310, 0.7), (520, 0.45), (870, 0.2)])
    creak *= env_points(n, [(0, 0), (0.08, 1), (0.6, 0.7), (0.9, 0)])
    groan = sine(62 - 18 * np.clip(T(n) / 1.2, 0, 1), n) * env_exp(n, 0.45, 0.02) * 0.6
    x = 0.45 * snap + 0.22 * pings + 0.3 * crackle + 0.9 * creak + 0.55 * groan
    x = lp(x, 6500, 2)
    return room(x, rt=1.1, wet=0.24, seed=24)


def nexus_destroy():
    r = rng(350)
    n = N(3.4)
    t = T(n)
    f_boom = 30 + (95 - 30) * np.exp(-t / 0.35)
    boom = sat(sine(f_boom, n) * env_exp(n, 0.85, 0.004) * 1.4, 2.0)
    impact = lp(white(n, 351), 2800, 2) * env_exp(n, 0.03, 0.0006)
    rumble = lp(colored_noise(n, -6.0, 352), 170, 4) * env_exp(n, 0.9, 0.01)
    # shatter: hundreds of crystal grains, density decaying
    sh = np.zeros(n)
    for k in range(320):
        t0 = r.exponential(0.3)
        if t0 > 1.8:
            continue
        f = float(np.exp(r.uniform(np.log(800), np.log(4200))))
        tau = r.uniform(0.015, 0.11)
        g = glass_ping(f, tau, seed=400 + k, amps=(1, 0.3, 0.08), attack=0.0008)
        mix_into(sh, g, N(0.005 + t0), r.uniform(0.2, 1.0) * (1000 / f) ** 0.5 * np.exp(-t0 / 0.6))
    # falling chunks hitting the ground
    ch = np.zeros(n)
    for k in range(16):
        t0 = 0.25 + r.uniform(0, 1.6) ** 1.3
        f = r.uniform(140, 420)
        g = thump(f * 1.6, f, 0.01, 0.03, N(0.3)) + 0.4 * metal_plate(f * 2.3, 0.05, N(0.3), seed=600 + k)
        mix_into(ch, g, N(t0), r.uniform(0.25, 0.8) * np.exp(-(t0 - 0.25) / 1.2))
    # core flash: alien FM chord swell + downward whoom
    idx = 1.6 * np.exp(-t / 0.5) + 0.25
    flash = sum(fm(n, f, 1.414, idx) for f in (233.1, 349.2, 466.2)) / 3
    flash *= env_points(n, [(0, 0), (0.06, 1), (0.4, 0.55), (2.6, 0)])
    whoom = sine(180 + 700 * np.exp(-t / 0.45), n) * env_points(n, [(0, 0), (0.1, 1), (1.4, 0)]) * \
        (1 + 0.25 * np.sin(2 * np.pi * 7 * t))
    x = (1.0 * boom + 0.55 * impact + 0.7 * rumble + 0.22 * sh + 0.55 * ch + 0.2 * flash + 0.1 * whoom)
    x = lp(x, 7000, 2)
    return room(x, rt=2.1, wet=0.3, seed=25, brightness=5000)


# ============================================================ boxes
def box_drop():
    n = N(0.6)
    th = thump(150, 78, 0.012, 0.06, n)
    body = peq(peq(bp(white(n, 501), 170, 750), 230, 6, 3), 420, 4, 3) * env_exp(n, 0.035, 0.001)
    dust = bp(white(n, 502), 700, 2600) * env_exp(n, 0.12, 0.015, delay=0.004)
    grit = bp(white(n, 503), 300, 1500, 3) * env_exp(n, 0.02, 0.001)
    one = 1.0 * th + 0.9 * body + 0.06 * dust + 0.25 * grit
    x = place(0.6, [(0, one, 1.0), (0.11, 0.8 * th + 0.8 * body, 0.32), (0.19, th, 0.1)])
    x = lp(x, 5500, 2)
    return room(x, rt=0.28, wet=0.1, seed=26)


def box_open_common():
    """cardboard: tape rip (stick-slip grains) + flaps"""
    r = rng(520)
    n = N(0.9)
    rip = np.zeros(n)
    t = 0.0
    k = 0
    while t < 0.3:
        rate = 260 + 520 * (t / 0.3)
        g = bp(white(N(0.006), 530 + k), 900, 3600) * env_exp(N(0.006), 0.0012, 0.0002)
        mix_into(rip, g, N(t), r.uniform(0.3, 1.0))
        t += r.exponential(1.0 / rate)
        k += 1
    bed = bp(white(n, 521), 700, 2600) * env_points(n, [(0, 0), (0.02, 1), (0.28, 0.8), (0.32, 0)])
    rip = rip + 0.35 * bed
    flap = (bp(white(n, 522), 140, 650) * env_exp(n, 0.05, 0.004) + 0.6 * thump(110, 70, 0.01, 0.04, n))
    rustle = bp(white(n, 523), 900, 2800) * env_exp(n, 0.13, 0.01) * (0.6 + 0.4 * rng(524).random(n) ** 3)
    x = place(0.9, [(0, rip, 0.55), (0.34, flap, 0.9), (0.45, flap, 0.45), (0.36, rustle, 0.12)])
    x = lp(x, 5200, 2)
    return room(x, rt=0.3, wet=0.12, seed=27)


def box_open_rare():
    """metal case: two latch clicks + lid clack + short sparkle"""
    def latch(f, seed):
        return add(0.6 * modes(N(0.08), [f, f * 2.1, f * 3.3], [1, 0.5, 0.2], [0.03, 0.015, 0.008], seed=seed),
                   0.25 * bp(white(N(0.02), seed + 1), 1500, 4500) * env_exp(N(0.02), 0.0015, 0.0002),
                   0.4 * thump(400, 220, 0.004, 0.01, N(0.05)))
    lid = 0.7 * metal_plate(520, 0.16, N(0.9), seed=541) + 0.8 * thump(160, 90, 0.01, 0.04, N(0.9))
    sparkle = np.zeros(N(1.4))
    for k, (f, t0) in enumerate([(659.3, 0.0), (880.0, 0.06), (1108.7, 0.12), (1318.5, 0.18)]):
        b = glass_ping(f, 0.38, seed=550 + k, partials=(1, 2.0, 3.0), amps=(1, 0.18, 0.05), attack=0.004)
        mix_into(sparkle, b, N(t0), 0.85 ** k)
    x = place(1.6, [(0, latch(1850, 542), 0.7), (0.09, latch(1700, 544), 0.6), (0.21, lid, 0.6),
                    (0.30, sparkle, 0.32)])
    x = lp(x, 6000, 2)
    return room(x, rt=0.7, wet=0.2, seed=28)


def box_open_alien():
    """organic pod: wet squelch sweep + bubble pop + gas hiss + FM shimmer"""
    n = N(1.9)
    t = T(n)
    squelch = svf(lp(white(n, 561), 3000, 4), 250 + 1150 * np.clip(t / 0.25, 0, 1) ** 1.5, 6.0, "bp")
    squelch *= env_points(n, [(0, 0), (0.02, 1), (0.22, 0.6), (0.4, 0)])
    pop = sine(300 + 600 * np.clip((t - 0.18) / 0.04, 0, 1), n) * env_exp(n, 0.03, 0.002, delay=0.18)
    hiss = bp(white(n, 562), 500, 2400) * env_points(n, [(0, 0), (0.2, 0), (0.25, 1), (0.6, 0)])
    idx = 1.8 * np.exp(-t / 0.5) + 0.3 * (1 + np.sin(2 * np.pi * 3.0 * t))
    chord = [369.99, 554.37, 830.61]
    shim = sum(fm(n, f * (1 + 0.004 * np.sin(2 * np.pi * (4.1 + j) * t)), 1.414, idx) for j, f in enumerate(chord)) / 3
    shim *= env_points(n, [(0, 0), (0.15, 0), (0.3, 1), (0.8, 0.6), (1.8, 0)])
    rise = sine(300 + 900 * np.clip((t - 0.2) / 0.7, 0, 1) ** 1.2, n) * (0.6 + 0.4 * np.sin(2 * np.pi * 11 * t)) * \
        env_points(n, [(0, 0), (0.2, 0), (0.3, 1), (0.9, 0.4), (1.2, 0)])
    x = 0.55 * squelch + 0.5 * pop + 0.08 * hiss + 0.22 * shim + 0.07 * rise
    x = lp(x, 6500, 2)
    return room(x, rt=1.4, wet=0.3, seed=29, brightness=5500)


# ============================================================ shop
def item_place():
    n = N(0.3)

    def knock(fs, seed):
        return (modes(n, [210 * fs, 470 * fs, 980 * fs], [1, 0.55, 0.25], [0.03, 0.02, 0.012], seed=seed) +
                0.35 * bp(white(n, seed + 1), 300, 2400) * env_exp(n, 0.004, 0.0004) +
                0.5 * thump(150 * fs, 100 * fs, 0.006, 0.02, n))
    x = place(0.3, [(0, knock(1.0, 571), 1.0), (0.038, knock(1.12, 573), 0.3)])
    x = lp(x, 5500, 2)
    return room(x, rt=0.2, wet=0.08, seed=30)


def price_tick():
    n = N(0.08)
    x = modes(n, [1500, 2650], [1, 0.25], [0.010, 0.005], seed=581, attack=0.0006) + \
        0.3 * bp(white(n, 582), 800, 3000) * env_exp(n, 0.0008, 0.0002)
    return lp(x, 5000, 2)


def customer_enter():
    r = rng(590)
    items = []
    for k, (f, t0) in enumerate([(784.0, 0.0), (988.0, 0.075), (880.0, 0.16), (1046.5, 0.27), (932.3, 0.40)]):
        c = add(tin_can(f * r.uniform(0.995, 1.005), r.uniform(0.32, 0.5), seed=591 + k),
                0.12 * bp(white(N(0.01), 600 + k), 1500, 4000) * env_exp(N(0.01), 0.001))
        items.append((t0, c, r.uniform(0.6, 1.0) * (0.85 ** k)))
    x = place(1.6, items)
    x = lp(x, 5500, 2)
    return room(x, rt=0.6, wet=0.18, seed=31)


def nut_clinks(seed, count, span, fmin=950, fmax=1850, can=True, dur=0.7):
    r = rng(seed)
    out = np.zeros(N(dur))
    t = 0.0
    for k in range(count):
        f = r.uniform(fmin, fmax)
        c = modes(N(0.15), [f, f * 1.62, f * 2.31], [1, 0.32, 0.1], [r.uniform(0.03, 0.06), 0.025, 0.012],
                  seed=seed * 10 + k, attack=0.0005)
        c = add(c, 0.08 * bp(white(N(0.01), seed * 10 + k), 1200, 3500) * env_exp(N(0.01), 0.0008))
        mix_into(out, c, N(t), r.uniform(0.45, 1.0) * (0.92 ** k))
        t += r.exponential(span / count)
    if can:
        out += 0.4 * tin_can(470 * r.uniform(0.97, 1.03), 0.1, len(out), seed=seed + 5)
    return out


def purchase(i):
    x = nut_clinks(700 + i, [6, 5, 7][i], [0.2, 0.17, 0.24][i], dur=0.7)
    x = lp(x, 5800, 2)
    return room(x, rt=0.35, wet=0.12, seed=32)


def purchase_big():
    pour = nut_clinks(760, 20, 0.6, fmin=900, fmax=1900, dur=1.4)
    heavy = nut_clinks(770, 3, 0.25, fmin=650, fmax=900, can=False, dur=1.0)
    bells = np.zeros(N(1.8))
    for k, (f, t0) in enumerate([(440.0, 0.05), (554.4, 0.12), (659.3, 0.19), (880.0, 0.26)]):
        mix_into(bells, fm_bell(f, 0.6, 2.0, 1.1, attack=0.004), N(t0), 0.9 ** k)
    x = place(1.8, [(0, pour, 0.7), (0.02, heavy, 0.6), (0, bells, 0.32)])
    x = lp(x, 6000, 2)
    return room(x, rt=0.8, wet=0.2, seed=33)


def customer_reject():
    """non-verbal grumble 'uh-hmm' (glottal saw + formants) over a faint low buzzer"""
    n = N(0.8)
    t = T(n)
    r = rng(800)

    def syll(t0, t1, f0a, f0b, formants):
        m = N(t1 - t0 + 0.1)
        tt = T(m)
        f0 = f0a + (f0b - f0a) * np.clip(tt / (t1 - t0), 0, 1)
        f0 *= 1 + 0.006 * r.standard_normal(m).cumsum() / np.sqrt(np.arange(1, m + 1))
        src = lp(saw(f0, m), 2500, 2)
        v = sum(g * resonator(src, f, f / bw) for f, bw, g in formants)
        e = env_points(m, [(0, 0), (0.03, 1), (t1 - t0 - 0.05, 0.8), (t1 - t0, 0)])
        return v * e
    s1 = syll(0.0, 0.17, 118, 108, [(520, 90, 1.0), (1050, 120, 0.4), (2300, 200, 0.1)])
    s2 = syll(0.0, 0.36, 110, 86, [(270, 60, 1.0), (950, 150, 0.15), (2200, 250, 0.04)])
    buzz = lp(pulse(92.0, n, 0.5) + pulse(92.6, n, 0.5), 600, 2) * env_points(n, [(0, 0), (0.22, 0), (0.26, 1),
                                                                                    (0.55, 0.7), (0.62, 0)])
    x = place(0.8, [(0, s1, 1.0), (0.22, s2, 1.1), (0, buzz, 0.05)])
    x = lp(x, 3500, 2)
    return room(x, rt=0.25, wet=0.08, seed=34)


# ============================================================ meta / ui
def upgrade():
    ticks = [add(modes(N(0.06), [1600 * s, 1600 * s * 2.3], [1, 0.3], [0.022, 0.01], seed=810 + k),
                 0.4 * thump(300, 180, 0.004, 0.012, N(0.05))) for k, s in enumerate((1.0, 1.05, 1.1))]
    arp = np.zeros(N(1.2))
    notes = [587.3, 740.0, 880.0, 1174.7]
    for k, f in enumerate(notes):
        d = 0.08 if k < 3 else 0.28
        mix_into(arp, chip_note(f, d, 0.25, 0.5, vib=0.004 if k == 3 else 0.0, lpf=3000), N(0.06 * k), 0.8)
    clank = metal_plate(440, 0.3, seed=820)
    x = place(1.4, [(0, ticks[0], 0.5), (0.05, ticks[1], 0.5), (0.10, ticks[2], 0.55), (0.14, arp, 0.3),
                    (0.32, clank, 0.18)])
    x = lp(x, 6000, 2)
    return room(x, rt=0.6, wet=0.16, seed=35)


def ui_click():
    n = N(0.1)
    t = T(n)
    tock = sine(900 + 500 * np.exp(-t / 0.004), n) * env_exp(n, 0.012, 0.0006)
    body = modes(n, [320, 760], [1, 0.4], [0.015, 0.008], seed=901)
    clk = bp(white(n, 902), 1000, 3500) * env_exp(n, 0.0009, 0.0002)
    return lp(0.7 * tock + 0.5 * body + 0.15 * clk, 5000, 2)


def ui_hover():
    n = N(0.06)
    x = sine(1150.0, n) * env_exp(n, 0.011, 0.002) + 0.3 * sine(575.0, n) * env_exp(n, 0.015, 0.002)
    return lp(x, 4000, 2)


def _blip(f0, f1, dur, n):
    t = T(n)
    f = f0 + (f1 - f0) * np.clip(t / dur, 0, 1)
    return (0.7 * sine(f, n) + 0.3 * tri(f, n)) * env_points(n, [(0, 0), (0.004, 1), (dur, 0.6), (dur + 0.06, 0)])


def _slide(f0, f1, dur, seed):
    n = N(dur + 0.05)
    t = T(n)
    x = svf(bp(white(n, seed), 400, 3500, 4), f0 + (f1 - f0) * np.clip(t / dur, 0, 1), 3.0, "bp")
    return x * env_points(n, [(0, 0), (0.02, 1), (dur, 0.3), (dur + 0.05, 0)])


def ui_open():
    x = place(0.4, [(0, _blip(520, 700, 0.05, N(0.15)), 0.6), (0.07, _blip(780, 1040, 0.06, N(0.18)), 0.5),
                    (0, _slide(700, 2000, 0.16, 911), 0.07)])
    return room(lp(x, 5000, 2), rt=0.3, wet=0.1, seed=36)


def ui_close():
    x = place(0.38, [(0, _blip(780, 600, 0.05, N(0.15)), 0.55), (0.07, _blip(520, 390, 0.06, N(0.18)), 0.6),
                     (0, _slide(1800, 650, 0.15, 921), 0.07)])
    return room(lp(x, 5000, 2), rt=0.3, wet=0.1, seed=36)


def prestige():
    n = N(3.2)
    t = T(n)
    whoosh = svf(lp(white(n, 931), 4000, 4), 300 + 3000 * np.clip(t / 1.6, 0, 1) ** 2, 2.5, "bp")
    whoosh *= env_points(n, [(0, 0), (1.3, 1), (1.7, 0.4), (2.4, 0)])
    arp = np.zeros(n)
    for k, m in enumerate([62, 66, 69, 74, 78, 81]):
        f = 440 * 2 ** ((m - 69) / 12)
        mix_into(arp, fm_bell(f, 0.9, 2.0, 1.0, attack=0.005), N(0.13 * k), 0.75 * 0.93 ** k)
    pad = np.zeros(n)
    for j, m in enumerate([50, 57, 62, 66, 69, 76]):
        f = 440 * 2 ** ((m - 69) / 12)
        pad += saw(f * (1 + 0.003 * (j % 3 - 1)), n) + saw(f * (1 - 0.004), n, 0.37)
    pad = svf(pad / 12, 500 + 2600 * np.clip((t - 0.5) / 1.3, 0, 1), 0.8, "lp")
    pad *= env_points(n, [(0, 0), (0.6, 0.05), (1.6, 1), (2.2, 0.8), (3.1, 0)])
    sub = sine(73.4, n) * env_points(n, [(0, 0), (1.6, 1), (3.0, 0)])
    sp = np.zeros(n)
    r = rng(940)
    for k in range(16):
        t0 = 1.4 + r.uniform(0, 1.2)
        mix_into(sp, glass_ping(r.uniform(1200, 2800), 0.18, seed=950 + k, amps=(1, 0.2, 0.04), attack=0.003),
                 N(t0), r.uniform(0.2, 0.6))
    x = 0.16 * whoosh + 0.3 * arp + 0.45 * pad + 0.35 * sub + 0.05 * sp
    x = lp(x, 6500, 2)
    return room(x, rt=1.8, wet=0.3, seed=37, brightness=5500)


def offline_reward():
    n = N(2.0)
    bells = np.zeros(n)
    for k, (m, t0) in enumerate([(74, 0.0), (78, 0.12), (81, 0.24)]):
        f = 440 * 2 ** ((m - 69) / 12)
        mix_into(bells, fm_bell(f, 0.9, 2.0, 1.2, attack=0.004), N(t0), 0.9 ** k)
    pad = sum(sine(440 * 2 ** ((m - 69) / 12), n) for m in (50, 57, 62, 66)) / 4
    pad *= env_points(n, [(0, 0), (0.3, 1), (1.0, 0.6), (1.9, 0)])
    jingle = nut_clinks(960, 5, 0.18, dur=0.8)
    x = place(2.0, [(0, bells, 0.4), (0, pad, 0.25), (0.34, jingle, 0.22)])
    x = lp(x, 6000, 2)
    return room(x, rt=1.0, wet=0.22, seed=38)


def error():
    def buzz(f):
        n = N(0.14)
        return lp(pulse(f, n, 0.5) + pulse(f * 1.013, n, 0.5), 1100, 2) * \
            env_points(n, [(0, 0), (0.005, 1), (0.08, 0.85), (0.1, 0)])
    th = thump(110, 70, 0.008, 0.02, N(0.12))
    x = place(0.4, [(0, buzz(155.0), 0.5), (0.135, buzz(146.0), 0.5), (0, th, 0.6)])
    return room(lp(x, 4000, 2), rt=0.2, wet=0.06, seed=39)


# ============================================================ v0.2: weakpoint / contracts
def weakpoint_spawn():
    """glowing crack appears: one clear soft-attack crystal 'ting' + 3 tiny rising pings + faint upward shimmer.
    Heard every 1-2 min for hours -> quiet, no sharp transient, energy kept under ~3 kHz."""
    n = N(1.4)
    t = T(n)
    ting = glass_ping(880.0, 0.38, n, seed=1001, partials=(1.0, 2.32, 3.74), amps=(1.0, 0.18, 0.04), attack=0.004,
                      detune=0.002)
    body = sine(440.0, n) * env_exp(n, 0.12, 0.006) * 0.25  # soft low support so it is not just a thin ping
    sp = np.zeros(n)
    for k, (f, t0) in enumerate([(1174.7, 0.07), (1318.5, 0.12), (1760.0, 0.18)]):
        mix_into(sp, glass_ping(f, 0.22, seed=1010 + k, amps=(1.0, 0.12, 0.02), attack=0.005), N(t0), 0.8 ** k)
    glide = sine(1100 + 600 * np.clip((t - 0.05) / 0.4, 0, 1) ** 0.7, n) * (0.6 + 0.4 * np.sin(2 * np.pi * 17 * t))
    glide *= env_points(n, [(0, 0), (0.08, 0), (0.2, 1), (0.45, 0.3), (0.6, 0)])
    x = 1.0 * ting + body + 0.32 * sp + 0.05 * glide
    x = lp(x, 5500, 2)
    return room(x, rt=0.7, wet=0.22, seed=40, brightness=5500)


def weakpoint_break():
    """weak point shattered: low thump + crisp crack + ~100 crystal fragments + rising pentatonic sparkle tail
    (D major, ~1 s) + faint alien FM shimmer. One step above sfx_hit_crit."""
    r = rng(1100)
    n = N(1.8)
    t = T(n)
    # low 'thump' + sub
    th = thump(120, 42, 0.035, 0.16, n, attack=0.002)
    sub = sine(45.0, n) * env_exp(n, 0.28, 0.005)
    body = sat(1.1 * th + 0.6 * sub, 1.8)
    knock = bp(white(n, 1101), 120, 1200) * env_exp(n, 0.03, 0.001)
    # crisp crack transient
    crack = bp(white(n, 1102), 600, 4500, 2) * env_exp(n, 0.012, 0.0004)
    # core resonance of the broken crystal
    core = add(glass_ping(698.5, 0.45, n, seed=1103, partials=(1, 2.27, 3.66), amps=(1, 0.3, 0.08)),
               0.8 * glass_ping(1046.5, 0.35, n, seed=1104, partials=(1, 2.41, 3.9), amps=(1, 0.25, 0.06)))
    # shatter: fragment grains, dense at first
    sh = np.zeros(n)
    for k in range(110):
        t0 = r.exponential(0.07)
        if t0 > 0.45:
            continue
        f = float(np.exp(r.uniform(np.log(900), np.log(4000))))
        g = glass_ping(f, r.uniform(0.012, 0.07), seed=1200 + k, amps=(1, 0.25, 0.05), attack=0.0006)
        mix_into(sh, g, N(0.004 + t0), r.uniform(0.25, 1.0) * (1000 / f) ** 0.5 * np.exp(-t0 / 0.15))
    # sparkle tail: rising D-major pentatonic bells, then scattered soft twinkles to ~1.2 s
    tail = np.zeros(n)
    for k, (m, t0) in enumerate([(81, 0.10), (86, 0.17), (88, 0.24), (90, 0.31), (93, 0.38), (95, 0.46)]):
        f = 440.0 * 2 ** ((m - 69) / 12)
        b = add(0.6 * fm_bell(f, 0.5, 2.0, 0.8, attack=0.004),
                glass_ping(f, 0.4, seed=1300 + k, amps=(1, 0.15, 0.03), attack=0.004))
        mix_into(tail, b, N(t0), 0.85 * 0.9 ** k)
    for k in range(14):
        t0 = 0.45 + r.uniform(0, 0.75)
        f = 440.0 * 2 ** ((r.choice([81, 83, 85, 86, 88, 90, 93]) - 69) / 12)
        mix_into(tail, glass_ping(f, 0.16, seed=1400 + k, amps=(1, 0.12, 0.02), attack=0.003), N(t0),
                 r.uniform(0.15, 0.4) * (1 - (t0 - 0.45) / 0.9))
    # faint alien shimmer under the tail (quartal D-A-E, FM sqrt2)
    idx = 0.9 * np.exp(-t / 0.4) + 0.2
    shim = sum(fm(n, f * (1 + 0.003 * np.sin(2 * np.pi * (5.3 + j) * t)), 1.414, idx)
               for j, f in enumerate((587.3, 880.0, 1318.5))) / 3
    shim *= env_points(n, [(0, 0), (0.08, 0), (0.25, 1), (0.7, 0.5), (1.25, 0)])
    x = (body + 0.45 * knock + 0.28 * crack + 0.22 * core + 0.2 * sh + 0.2 * tail + 0.06 * shim)
    x = lp(x, 6500, 2)
    return room(x, rt=1.2, wet=0.26, seed=41, brightness=5500)


def contract_done():
    """contract completed: short soft two-note chime (E5 -> A5)"""
    n = N(1.6)

    def chime(f, tau, seed):
        m = N(tau * 5)
        tt = T(m)
        x = modes(m, [f, f * 2.0, f * 3.01], [1.0, 0.18, 0.05], [tau, tau * 0.5, tau * 0.25], seed=seed,
                  attack=0.006, detune=0.0015)
        return x + 0.15 * sine(f / 2, m) * np.exp(-tt / (tau * 0.4))  # warm undertone
    x = place(1.3, [(0.0, chime(659.3, 0.3, 1501), 0.8), (0.13, chime(880.0, 0.4, 1502), 1.0)])
    x = lp(x, 5000, 2)
    return room(x, rt=0.6, wet=0.18, seed=42)


def contract_claim():
    """reward claimed: rubber stamp 'tak' on paper/wooden desk + small nut jingle"""
    n = N(0.4)
    th = thump(240, 140, 0.004, 0.018, n, attack=0.0008)
    desk = modes(n, [190, 410, 820], [1.0, 0.5, 0.2], [0.04, 0.025, 0.012], seed=1601)
    slap = bp(white(n, 1602), 700, 3000, 2) * env_exp(n, 0.005, 0.0005)
    stamp = 0.9 * th + 0.5 * desk + 0.35 * slap
    jingle = nut_clinks(1610, 4, 0.13, dur=0.7)
    x = place(1.0, [(0, stamp, 1.0), (0.026, 0.6 * th + 0.3 * desk, 0.22), (0.12, jingle, 0.45)])
    x = lp(x, 5800, 2)
    return room(x, rt=0.35, wet=0.12, seed=43)


# ============================================================ registry
# name: (render fn, target momentary-max LUFS (dual mono), description)
SFX = {}
for i in range(5):
    SFX[f"sfx_hit_{i + 1:02d}"] = (lambda i=i: hit(i), -22.0, "망치 타격(기본). 둔탁한 트랜지언트+금속 모드+유리질 결정 공진+짧은 룸. 가장 자주 → 가장 부드럽게")
SFX["sfx_hit_crit"] = (hit_crit, -17.0, "치명타: 더 깊은 저음 붐+판금 공진+상승하는 결정 반짝임+파편 크런치")
for i in range(3):
    SFX[f"sfx_helper_hit_{i + 1:02d}"] = (lambda i=i: helper_hit(i), -26.0, "조수 타격: 작은 렌치 땡그랑")
SFX["sfx_nexus_crack"] = (nexus_crack, -18.0, "파손 단계: 결정 스냅+크래클 알갱이+삐걱이는 스틱슬립+저음 신음")
SFX["sfx_nexus_destroy"] = (nexus_destroy, -14.0, "넥서스 파괴: 저음 붐+수백 개 결정 파편+떨어지는 덩어리+코어 FM 섬광")
SFX["sfx_box_drop"] = (box_drop, -21.0, "박스 낙하: 흙바닥 쿵+골판지 몸통+먼지+작은 튕김")
SFX["sfx_box_open_common"] = (box_open_common, -21.0, "골판지 테이프 찢기(스틱슬립)+덮개 펄럭")
SFX["sfx_box_open_rare"] = (box_open_rare, -20.0, "금속 걸쇠 2번+뚜껑 철컥+짧은 반짝임 아르페지오")
SFX["sfx_box_open_alien"] = (box_open_alien, -19.0, "외계 포드: 질척한 필터 스윕+거품 팝+가스+FM 시머 화음")
SFX["sfx_item_place"] = (item_place, -24.0, "진열대에 물건 톡 놓기(나무 모드 2타)")
SFX["sfx_price_tick"] = (price_tick, -32.0, "가격 조절 틱(아주 작게)")
SFX["sfx_customer_enter"] = (customer_enter, -22.0, "손님 도착: 매달린 깡통 풍경 딸랑")
for i in range(3):
    SFX[f"sfx_purchase_{i + 1:02d}"] = (lambda i=i: purchase(i), -21.0, "판매 성공: 너트가 깡통에 짤랑")
SFX["sfx_purchase_big"] = (purchase_big, -18.0, "고가 판매: 너트 쏟아짐+따뜻한 벨 아르페지오")
SFX["sfx_customer_reject"] = (customer_reject, -22.0, "손님 거절: '어-흠' 비언어 투덜거림+희미한 저음 부저")
SFX["sfx_upgrade"] = (upgrade, -19.0, "업그레이드: 래칫 3틱+상승 칩 아르페지오+금속 확인음")
SFX["sfx_ui_click"] = (ui_click, -26.0, "UI 클릭: 부드러운 톡")
SFX["sfx_ui_hover"] = (ui_hover, -33.0, "UI 호버: 아주 작은 블립")
SFX["sfx_ui_open"] = (ui_open, -25.0, "UI 열기: 상승 2음+패널 슬라이드")
SFX["sfx_ui_close"] = (ui_close, -25.0, "UI 닫기: 하강 2음+패널 슬라이드")
SFX["sfx_prestige"] = (prestige, -15.0, "환생: 휘익 스윕+상승 D장조 벨 아르페지오+열리는 패드 화음")
SFX["sfx_offline_reward"] = (offline_reward, -19.0, "오프라인 정산 팝업: 3음 벨 차임+패드+너트 짤랑")
SFX["sfx_error"] = (error, -22.0, "오류(너트 부족 등): 낮은 2연 부저+툭")


SFX["sfx_weakpoint_spawn"] = (weakpoint_spawn, -26.0, "v0.2 약점 생성: 부드러운 어택의 맑은 결정 '팅'(A5)+작게 올라가는 3음 반짝임. 1~2분마다 → 일반 타격보다 4 LU 작게")
SFX["sfx_weakpoint_break"] = (weakpoint_break, -15.5, "v0.2 약점 파괴: 낮은 쿵+시원한 크랙+결정 파편 110알+상승 D장조 펜타토닉 벨 꼬리(~1.2s)+옅은 외계 시머. 치명타보다 1.5 LU 크고 화려하게")
SFX["sfx_contract_done"] = (contract_done, -22.0, "v0.2 의뢰 달성: 부드러운 두 음 차임(E5→A5)")
SFX["sfx_contract_claim"] = (contract_claim, -21.0, "v0.2 의뢰 보상: 고무 도장 '탁'(나무 책상+종이)+너트 짤랑")


MAXDUR = {"sfx_nexus_destroy": 2.9, "sfx_prestige": 2.8, "sfx_weakpoint_break": 1.6}


def main(filt=None):
    OUT.mkdir(parents=True, exist_ok=True)
    from nb.dsp import write_wav
    meta = {}
    for name, (fn, target, desc) in SFX.items():
        if filt and filt not in name:
            continue
        x = finish(fn(), target, max_dur=MAXDUR.get(name))
        write_wav(OUT / f"{name}.wav", x, seed=zlib.crc32(name.encode()) & 0xFFFF)
        meta[name] = {"target_lufs_m": target, "desc": desc, "dur": round(len(x) / SR, 3)}
        print(f"{name:28s} {len(x) / SR:6.3f}s")
    mp = Path(__file__).parent / "build" / "sfx_meta.json"
    mp.parent.mkdir(exist_ok=True)
    old = json.loads(mp.read_text(encoding="utf-8")) if mp.exists() else {}
    old.update(meta)
    mp.write_text(json.dumps(old, ensure_ascii=False, indent=1), encoding="utf-8")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else None)
