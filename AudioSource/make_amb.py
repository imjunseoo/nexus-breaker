"""Ambience loops -> build/masters/*.wav (then encode_ogg.py -> Assets/Audio/Ambient), stereo, seamless.

Loop safety: noise is generated circularly (FFT-shaped, period = loop length), all modulators use frequencies
with an integer number of cycles per loop, filters run on [loop, loop] keeping the 2nd copy, and one-off events
are placed on a buffer longer than the loop and folded back (wrap) so their tails continue across the seam.
"""
import sys
from pathlib import Path

import numpy as np

from nb.dsp import (SR, N, T, TAU, rng, db2a, a2db, lp, hp, bp, svf, colored_noise, wrap, circular_process, mix_into,
                    pan, make_ir, conv, modes, resonator, env_points, env_exp, white, sat, write_wav, limiter,
                    true_peak, fade)
from nb.measure import integrated_lufs

ROOT = Path(__file__).resolve().parent.parent
OUT = Path(__file__).resolve().parent / "build" / "masters"  # lossless masters; encode_ogg.py -> Assets/Audio/Ambient


def periodic_lfo(L, seed, kmax=10, slope=1.0, kmin=1):
    """smooth random function with period L samples (sum of harmonics of 1/L), normalised to [-1, 1]"""
    r = rng(seed)
    t = np.arange(L) / L
    x = np.zeros(L)
    for k in range(kmin, kmax + 1):
        x += r.normal() / k ** slope * np.sin(TAU * k * t + r.uniform(0, TAU))
    return x / (np.max(np.abs(x)) + 1e-12)


def circ_filter(x, fn):
    return circular_process(x, fn, reps=2)


def finalize(y, target, ceiling=-1.5):
    y = y * db2a(target - integrated_lufs(y))
    if a2db(true_peak(y)) > ceiling:
        y = circular_process(y, lambda z: limiter(z, ceiling - 0.3), reps=2)
    return y - y.mean(axis=-1, keepdims=True)


# ============================================================ wasteland wind
def amb_wasteland(L_sec=60.0):
    L = N(L_sec)
    n_ext = L + N(8)  # event buffer with room for tails
    t = np.arange(L) / SR
    gust = periodic_lfo(L, 1, kmax=14, slope=0.9)           # slow gusts
    gust2 = periodic_lfo(L, 2, kmax=30, slope=1.2, kmin=4)  # faster flutter
    g = np.clip(0.55 + 0.35 * gust + 0.12 * gust2, 0.12, 1.2) ** 1.5

    def noise_st(seed, slope):
        a = colored_noise(L, slope, seed)
        b = colored_noise(L, slope, seed + 1)
        c = colored_noise(L, slope, seed + 2)
        return np.stack([0.75 * a + 0.45 * b, 0.75 * a + 0.45 * c])
    low = circ_filter(noise_st(10, -4.0), lambda z: lp(z, 220, 4)) * (0.6 + 0.4 * g)
    mid = circ_filter(noise_st(20, -3.0), lambda z: bp(z, 280, 950, 2)) * g
    hi = circ_filter(noise_st(30, -3.0), lambda z: bp(z, 900, 2600, 2)) * g ** 1.6
    # whistle: resonant band whose centre drifts with the gusts (periodic)
    fc = 520 + 260 * periodic_lfo(L, 3, kmax=6) + 120 * gust
    wn = colored_noise(L, -1.0, 40)
    whistle = circular_process(wn, lambda z: svf(z, np.concatenate([fc, fc]), 14.0, "bp"), reps=2)
    whistle = whistle * np.clip(g - 0.55, 0, None) ** 1.2
    whistle = np.stack([whistle, np.roll(whistle, N(0.013))])
    bed = 0.55 * low + 0.75 * mid + 0.14 * hi + 0.06 * whistle

    # ------------- distant events (placed on an extended buffer, then wrapped)
    ev = np.zeros((2, n_ext))
    far_ir = make_ir(rt60=3.2, predelay=0.04, stereo=True, seed=91, brightness=3500, hf_ratio=0.35)
    r = rng(50)

    def creak(seed, f0, dur):
        rr = rng(seed)
        m = N(dur + 0.3)
        imp = np.zeros(m)
        tt = 0.02
        while tt < dur:
            imp[N(tt)] = rr.uniform(0.5, 1.0)
            rate = 18 + 22 * np.sin(np.pi * tt / dur)
            tt += (1 / rate) * rr.uniform(0.7, 1.35)
        y = sum(gn * resonator(imp, f0 * k, 18) for k, gn in [(1, 1.0), (2.13, 0.6), (3.41, 0.3), (5.2, 0.12)])
        return lp(y * env_points(m, [(0, 0), (0.1, 1), (dur * 0.8, 0.8), (dur, 0), (dur + 0.3, 0)]), 2200, 2)

    def clank(seed, f0):
        m = N(1.5)
        y = modes(m, [f0 * k for k in (1, 1.59, 2.14, 2.65, 3.5)], [1, .7, .5, .3, .12], [.5, .4, .3, .2, .12],
                  seed=seed)
        return lp(y + 0.4 * np.sin(TAU * 70 * T(m)) * np.exp(-T(m) / 0.08), 1600, 2)

    def rattle(seed, dur):
        rr = rng(seed)
        m = N(dur + 0.2)
        y = np.zeros(m)
        tt = 0.0
        while tt < dur:
            f = rr.uniform(700, 1400)
            y[N(tt):N(tt) + N(0.04)] += modes(N(0.04), [f, f * 1.8], [1, .4], [0.01, 0.006], seed=int(tt * 1e4))[
                :len(y[N(tt):N(tt) + N(0.04)])] * rr.uniform(0.3, 1.0)
            tt += rr.exponential(0.045)
        return lp(y * env_points(m, [(0, 0), (0.2, 1), (dur - 0.2, 0.7), (dur, 0)]), 2500, 2)

    def gravel(seed, dur):
        rr = rng(seed)
        m = N(dur + 0.2)
        y = np.zeros(m)
        tt = 0.0
        while tt < dur:
            f = rr.uniform(400, 1800)
            seg = modes(N(0.03), [f, f * 1.7], [1, 0.4], [0.006, 0.004], seed=int(tt * 1e4) + seed)
            mix_into(y, seg, N(tt), rr.uniform(0.2, 1.0) * np.exp(-tt / dur * 2))
            tt += rr.exponential(0.02 + tt * 0.05)
        return lp(y, 2400, 2)

    events = [
        (4.2, creak(51, 310, 1.6), 0.5, -0.6),
        (11.8, clank(52, 233), 0.35, 0.5),
        (17.5, rattle(53, 2.2), 0.25, 0.3),
        (23.0, creak(54, 420, 1.1), 0.4, 0.55),
        (31.4, gravel(55, 1.4), 0.3, -0.35),
        (36.9, clank(56, 180), 0.3, -0.5),
        (42.6, creak(57, 260, 2.0), 0.45, -0.2),
        (49.3, rattle(58, 1.6), 0.2, -0.6),
        (55.8, creak(59, 370, 1.3), 0.35, 0.65),  # tail crosses the loop point -> wrapped
    ]
    for st, x, gn, p in events:
        mix_into(ev, pan(x, p), N(st), gn)
    ev = ev + 0.9 * conv(ev, far_ir)[:, :n_ext]
    ev = wrap(ev, L)
    lv_bed = integrated_lufs(bed)
    lv_ev = integrated_lufs(ev)
    ev *= db2a((lv_bed - 9) - lv_ev)  # events sit ~9 LU under the wind
    y = bed + ev
    y = circ_filter(y, lambda z: lp(hp(z, 25, 2), 7000, 2))
    return finalize(y, -24.0)


# ============================================================ nexus hum
def amb_nexus_hum(L_sec=16.0):
    L = N(L_sec)
    t = np.arange(L) / SR
    q = 1.0 / L_sec  # frequency grid -> every partial completes an integer number of cycles

    def snap(f):
        return round(f / q) * q

    r = rng(70)
    out = np.zeros((2, L))
    # drone harmonics with slow beating pairs
    for h, a in [(1, 1.0), (2, 0.7), (3, 0.42), (4, 0.25), (5, 0.12), (6, 0.08)]:
        f = snap(55.0 * h)
        beat = q * (2 * h)  # beating partner
        ph = r.uniform(0, TAU)  # same phase both sides (mono-safe); width comes from the beating partner only
        for c in range(2):
            out[c] += a * (np.sin(TAU * f * t + ph) + 0.6 * np.sin(TAU * (f + beat * (1 if c == 0 else 1.5)) * t + ph))
    # pulse: 0.25 Hz breathing (4 cycles / loop) + 1/16 Hz swell
    pulse_ = 0.82 + 0.13 * np.sin(TAU * snap(0.25) * t) + 0.05 * np.sin(TAU * q * t)
    out *= pulse_
    # crystalline inharmonic overtones, slowly shimmering
    shimmer = np.zeros((2, L))
    for k, (ratio, a) in enumerate([(4.73, 0.12), (7.11, 0.07), (9.87, 0.04), (13.3, 0.022)]):
        f = snap(55.0 * ratio)
        am = 0.5 + 0.5 * np.sin(TAU * q * (k + 1) * t + k)
        ph = r.uniform(0, TAU)
        for c in range(2):
            shimmer[c] += a * am * np.sin(TAU * (f + c * 2 * q) * t + ph + 0.6 * c)
    # FM growl: carrier 110, mod 165 with breathing index
    idx = 0.6 + 0.4 * np.sin(TAU * snap(0.125) * t)
    growl = np.sin(TAU * snap(110.0) * t + idx * np.sin(TAU * snap(165.0) * t))
    # electric fizz: circular noise band, gated by the pulse
    fz = circular_process(colored_noise(L, -2.0, 71), lambda z: bp(z, 900, 2600, 2), reps=2)
    fz = fz * np.clip(np.sin(TAU * snap(0.25) * t), 0, None) ** 3
    y = out + 0.9 * shimmer + 0.12 * np.stack([growl, np.roll(growl, N(0.0007))]) + 0.02 * np.stack([fz, np.roll(fz, N(0.4))])
    y = circular_process(y, lambda z: lp(hp(sat(z * 0.25, 1.2) * 4, 30, 2), 5000, 2), reps=2)
    return finalize(y, -24.0)


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    which = sys.argv[1:] or ["wasteland", "hum"]
    if "wasteland" in which:
        y = amb_wasteland()
        write_wav(OUT / "amb_wasteland.wav", y, seed=3)
        print("amb_wasteland", y.shape[-1] / SR)
    if "hum" in which:
        y = amb_nexus_hum()
        write_wav(OUT / "amb_nexus_hum.wav", y, seed=4)
        print("amb_nexus_hum", y.shape[-1] / SR)
