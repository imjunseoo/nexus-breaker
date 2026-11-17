"""Compose + render the BGM loops and the prestige stinger -> build/masters/*.wav (then encode_ogg.py -> Assets/Audio/BGM).

python make_bgm.py [main|shop|core|stinger]
Loops: sample-exact length (bars * beats * samples/beat, BPM chosen so a 16th is an integer number of samples).
Seamless: every note/reverb tail past the loop end is folded onto the start (wrap), then the master chain
(compressor, tape, wow) runs on [loop, loop] and the second copy is kept, so filter/LFO state is continuous.
"""
import sys
from pathlib import Path

import numpy as np

from nb.dsp import (SR, N, T, rng, mtof, db2a, lp, hp, peq, shelf, sat, chorus, make_ir, colored_noise, fade,
                    loop_lfo_freq, delay_line, pan, write_wav, wrap, mix_into, sine, white, svf, bp, env_points)
from nb.music import (Arr, guitar, strum, ep, pad_voice, chip, ocarina, tri_bass, saw_bass, mallet, fm_bell, theremin,
                      kick, snare, rim, hat, shaker, brush, tom, riser, finalize_loop, finalize_oneshot)

ROOT = Path(__file__).resolve().parent.parent
OUT = Path(__file__).resolve().parent / "build" / "masters"  # lossless masters; encode_ogg.py -> Assets/Audio/BGM

HALL = make_ir(rt60=2.4, predelay=0.028, stereo=True, seed=3, brightness=5200, hf_ratio=0.4)
ROOM = make_ir(rt60=0.7, predelay=0.008, stereo=True, seed=4, brightness=6000)
PLATE = make_ir(rt60=1.5, predelay=0.015, stereo=True, seed=5, brightness=6000, hf_ratio=0.5, er=4)


def place_melody(A, bus, bars, start_bar, inst, send, gain=1.0, p=0.0, legato_glide=False, **kw):
    """bars: list of lists of (beat, dur, midi). inst(freq, dur_sec, vel, ...)"""
    prev = None
    for bi, notes in enumerate(bars):
        for (b, d, m) in notes:
            if m is None:
                prev = None
                continue
            beat = A.pos(start_bar + bi, b)
            f = float(mtof(m))
            extra = dict(kw)
            if legato_glide and prev is not None and abs(prev - m) <= 5:
                extra["glide_from"] = float(mtof(prev))
            vel = 0.72 + 0.12 * (b == 0) + 0.05 * A.r.normal()
            A.add(bus, inst(f, A.sec(d) * 0.96, vel, **extra), beat, gain, p, jitter_ms=4, send=send)
            prev = m


# ============================================================================ MAIN (84 BPM, D minor, 36 bars)
MAIN_V = {  # guitar voicings (MIDI)
    "Dm9": [50, 57, 60, 64, 65], "Bbmaj7": [46, 53, 57, 62, 65], "Fmaj7": [41, 48, 57, 60, 64],
    "C6": [48, 55, 57, 64, 67], "Gm7": [43, 50, 53, 58, 62], "Gm9": [43, 50, 53, 57, 58],
    "A7sus": [45, 52, 55, 62, 64], "A7": [45, 52, 55, 61, 64], "Am7": [45, 52, 55, 60, 64],
    "C7sus": [48, 55, 58, 65, 67], "C7": [48, 55, 58, 64, 67],
}
MAIN_ROOT = {"Dm9": 38, "Bbmaj7": 34, "Fmaj7": 41, "C6": 36, "Gm7": 43, "Gm9": 43, "A7": 45, "Am7": 45, "C7": 36}
MAIN_PAD = {"Dm9": [57, 60, 64, 65], "Bbmaj7": [57, 62, 65, 69], "Fmaj7": [57, 60, 64, 67], "C6": [55, 60, 64, 69],
            "Gm7": [58, 62, 65, 69], "Gm9": [57, 58, 62, 65], "A7": [55, 61, 64, 69], "Am7": [55, 60, 64, 67],
            "C7": [58, 60, 64, 67]}

MEL_B1 = [
    [(0.5, 0.5, 69), (1.0, 0.5, 74), (1.5, 0.5, 76), (2.0, 1.5, 77), (3.5, 0.5, 76)],
    [(0.0, 1.0, 74), (1.0, 0.5, 72), (1.5, 0.5, 74), (2.0, 2.0, 69)],
    [(0.5, 0.5, 69), (1.0, 0.5, 72), (1.5, 0.5, 77), (2.0, 1.0, 76), (3.0, 0.5, 72), (3.5, 0.5, 74)],
    [(0.0, 1.5, 76), (1.5, 0.5, 74), (2.0, 1.0, 72), (3.0, 1.0, 67)],
    [(0.5, 0.5, 69), (1.0, 0.5, 74), (1.5, 0.5, 76), (2.0, 1.0, 77), (3.0, 0.5, 79), (3.5, 0.5, 81)],
    [(0.0, 1.5, 81), (1.5, 0.5, 79), (2.0, 1.0, 77), (3.0, 1.0, 74)],
    [(0.0, 0.5, 74), (0.5, 0.5, 77), (1.0, 1.0, 76), (2.0, 1.0, 74), (3.0, 1.0, 70)],
    [(0.0, 2.0, 73), (2.0, 1.0, 76), (3.0, 1.0, None)],
]
MEL_B2 = [
    [(0.5, 0.5, 69), (1.0, 0.5, 74), (1.5, 0.5, 76), (2.0, 1.5, 77), (3.5, 0.5, 79)],
    [(0.0, 1.0, 81), (1.0, 0.5, 79), (1.5, 0.5, 77), (2.0, 2.0, 74)],
    [(0.5, 0.5, 72), (1.0, 0.5, 77), (1.5, 0.5, 81), (2.0, 1.0, 84), (3.0, 1.0, 81)],
    [(0.0, 1.5, 79), (1.5, 0.5, 76), (2.0, 1.0, 72), (3.0, 1.0, 76)],
    [(0.0, 1.5, 77), (1.5, 0.5, 76), (2.0, 0.5, 74), (2.5, 0.5, 76), (3.0, 1.0, 77)],
    [(0.0, 1.0, 74), (1.0, 1.0, 77), (2.0, 1.0, 81), (3.0, 1.0, 79)],
    [(0.0, 1.5, 77), (1.5, 0.5, 76), (2.0, 1.0, 74), (3.0, 1.0, 70)],
    [(0.0, 2.0, 69), (2.0, 1.0, 73), (3.0, 1.0, 76)],
]
MEL_C = [  # bridge, ocarina
    [(0.0, 2.0, 77), (2.0, 1.0, 81), (3.0, 1.0, 79)],
    [(0.0, 3.0, 76), (3.0, 1.0, 72)],
    [(0.0, 2.0, 76), (2.0, 1.0, 79), (3.0, 1.0, 81)],
    [(0.0, 3.0, 77), (3.0, 1.0, 76)],
    [(0.0, 2.0, 74), (2.0, 1.0, 77), (3.0, 1.0, 81)],
    [(0.0, 2.0, 79), (2.0, 2.0, 82)],
    [(0.0, 3.0, 81), (3.0, 1.0, 79)],
    [(0.0, 2.0, 76), (2.0, 2.0, 73)],
]
MEL_D = [  # outro answer on mallet
    [(1.0, 0.5, 69), (1.5, 0.5, 74), (2.0, 2.0, 77)],
    [(0.0, 1.0, 74), (1.0, 1.0, 72), (2.0, 2.0, 69)],
    [],
    [(0.0, 2.0, 76), (2.0, 2.0, 73)],
]


def bgm_main():
    A = Arr(84, 36, seed=11, s16=0.055)
    pa = ["Dm9", "Bbmaj7", "Fmaj7", "C6", "Dm9", "Bbmaj7", "Gm7", "A7"]
    pc = ["Bbmaj7", "C6", "Am7", "Dm9", "Gm9", "C7", "Fmaj7", "A7"]
    prog = pa + pa + pc + pa + ["Dm9", "Bbmaj7", "Gm7", "A7"]
    sect = ["A1"] * 8 + ["B1"] * 8 + ["C"] * 8 + ["B2"] * 8 + ["D"] * 4
    r = rng(12)
    hall = {"hall": 0.22}
    arps = [[0, 2, 3, 4, 1, 3, 2, 4], [0, 3, 2, 4, 1, 4, 3, 2], [0, 2, 4, 3, 1, 3, 4, 2]]
    for bar, (ch, sc) in enumerate(zip(prog, sect)):
        b0 = bar * 4
        # ---------------- guitar
        halves = [(0, MAIN_V["A7sus"]), (2, MAIN_V["A7"])] if ch == "A7" else \
            [(0, MAIN_V["C7sus"]), (2, MAIN_V["C7"])] if (ch == "C7") else [(0, MAIN_V[ch])]
        gv = {"A1": 0.75, "B1": 0.55, "C": 0.6, "B2": 0.58, "D": 0.7}[sc]
        if sc in ("A1", "B1", "B2", "D"):
            pat = arps[bar % 3]
            step = 1.0 if (sc == "D" and bar >= 34) else 0.5
            cnt = int(4 / step)
            for k in range(cnt):
                beat = k * step
                v = [vv for (st, vv) in halves if st <= beat][-1]
                idx = pat[k % 8]
                ring = 4 - beat + 0.3
                vel = gv * (1.0 if k == 0 else 0.78) * (1 + 0.07 * r.normal())
                A.add("gtr", guitar(float(mtof(v[idx])), A.sec(ring), vel, seed=bar * 16 + k, t60=1.7,
                                    bright=0.3), b0 + beat, 1.0, -0.25 + 0.1 * (idx - 2) / 2, jitter_ms=5, send=hall)
        else:  # bridge: soft strums
            for beat, up, vel in [(0, False, 0.75), (1.5, True, 0.45), (2.5, False, 0.55), (3.5, True, 0.35)]:
                v = [vv for (st, vv) in halves if st <= beat][-1]
                fr = [float(mtof(m)) for m in v]
                dur = A.sec(1.6 if beat < 3.5 else 0.8)
                A.add("gtr", strum(fr, dur, gv * vel, seed=bar * 8 + int(beat * 2), up=up, t60=1.4, bright=0.25),
                      b0 + beat, 0.9, -0.3, jitter_ms=4, send=hall)
        # ---------------- pad
        if sc in ("A1", "C", "B2", "D"):
            lvl = {"A1": 0.10, "C": 0.13, "B2": 0.08, "D": 0.12}[sc]
            A.add("pad", pad_voice([float(mtof(m)) for m in MAIN_PAD[ch]], A.sec(4.0), cutoff=1100, attack=0.9,
                                   release=1.6, seed=bar, level=lvl), b0, 1.0, send={"hall": 0.35})
        # ---------------- bass
        root = MAIN_ROOT[ch]
        nxt = MAIN_ROOT[prog[(bar + 1) % len(prog)]]
        if sc == "D" and bar < 35:
            bpat = [(0, 3.8, 0)]
        elif sc == "C":
            bpat = [(0, 1.9, 0), (2.5, 0.45, 7), (3.0, 0.9, 12 if bar % 2 else 0)]
        else:
            last = (nxt - root) + (1 if nxt < root else -1) if bar % 4 == 3 else 7
            bpat = [(0, 1.4, 0), (1.5, 0.4, 0), (2.5, 0.45, 7), (3.0, 0.4, 12), (3.5, 0.45, last)]
        if sc == "A1" and bar < 2:
            bpat = [(0, 3.8, 0)]
        for (bb, dd, iv) in bpat:
            A.add("bass", tri_bass(float(mtof(root + iv)), A.sec(dd), 0.8 + 0.1 * (bb == 0), sub=0.7), b0 + bb, 1.0,
                  0.0, jitter_ms=3)
        # ---------------- drums
        dsend = {"room": 0.18}
        style = {"A1": "lite", "B1": "full", "C": "half", "B2": "full", "D": "hats"}[sc]
        if sc == "A1" and bar < 2:
            style = "hats"
        if style in ("full", "lite"):
            for kb in ([0, 1.75, 2.5] if bar % 2 == 0 else [0, 2.5, 2.75]):
                A.add("drums", kick(0.85 if kb == 0 else 0.65), b0 + kb, 1.0, 0, jitter_ms=2, send=dsend)
            for sb in (1, 3):
                if style == "full":
                    A.add("drums", snare(0.7, seed=bar * 4 + sb), b0 + sb, 1.0, 0.05, jitter_ms=4, send=dsend)
                else:
                    A.add("drums", rim(0.6, seed=bar * 4 + sb), b0 + sb, 1.0, 0.1, jitter_ms=4, send=dsend)
            for k in range(8):
                v = 0.55 if k % 2 == 0 else 0.35
                A.add("drums", hat(v * (1 + 0.1 * r.normal()), seed=bar * 32 + k), b0 + k * 0.5, 1.0, 0.25,
                      jitter_ms=4, send=dsend)
            if style == "full":
                for g in (1.75, 3.25):
                    A.add("drums", hat(0.2, seed=bar * 32 + 20 + int(g * 4)), b0 + g, 1.0, 0.3, jitter_ms=3)
            if bar % 8 == 7:
                for g, v in ((3.5, 0.3), (3.75, 0.45)):
                    A.add("drums", snare(v, seed=900 + bar, decay=0.08), b0 + g, 1.0, -0.05, jitter_ms=3, send=dsend)
        elif style == "half":
            A.add("drums", kick(0.75), b0, 1.0, 0, send=dsend)
            A.add("drums", kick(0.5), b0 + 2.75, 1.0, 0, send=dsend)
            A.add("drums", snare(0.6, seed=bar, decay=0.16), b0 + 2, 1.0, 0.05, jitter_ms=4, send={"room": 0.3})
            for k in range(8):
                A.add("drums", shaker(0.5 if k % 2 else 0.3, seed=bar * 8 + k), b0 + k * 0.5, 1.0, 0.35,
                      jitter_ms=5, send=dsend)
        elif style == "hats":
            if bar != 35:
                A.add("drums", kick(0.55), b0, 1.0, 0, send=dsend)
            for k in range(8):
                A.add("drums", hat(0.3 if k % 2 == 0 else 0.2, seed=bar * 32 + k), b0 + k * 0.5, 1.0, 0.25,
                      jitter_ms=4, send=dsend)
            if bar == 35:
                for g, v in ((2.5, 0.25), (3.0, 0.35), (3.5, 0.45), (3.75, 0.55)):
                    A.add("drums", snare(v, seed=950 + int(g * 4), decay=0.08), b0 + g, 1.0, 0, send=dsend)
        # ---------------- B2 chip arpeggio counterline (quiet, right)
        if sc == "B2":
            v = MAIN_PAD[ch]
            seq = [v[0] + 12, v[1] + 12, v[2] + 12, v[3] + 12, v[2] + 12, v[1] + 12]
            for k in range(16):
                m = seq[k % len(seq)]
                A.add("arp", chip(float(mtof(m)), A.sec(0.2), 0.35, width=0.125, tri_mix=0.5, vib_cents=0,
                                  lpf=2400), b0 + k * 0.25, 1.0, 0.45, send={"hall": 0.25})
    # ---------------- melodies
    lead_send = {"hall": 0.3, "echo": 0.25}
    place_melody(A, "lead", MEL_B1, 8, chip, lead_send, 1.0, 0.1, legato_glide=True, width=0.25, lpf=2900)
    place_melody(A, "lead", MEL_B2, 24, chip, lead_send, 1.0, 0.1, legato_glide=True, width=0.25, lpf=2900)
    place_melody(A, "lead2", MEL_C, 16, ocarina, {"hall": 0.4, "echo": 0.2}, 1.0, 0.05)
    place_melody(A, "lead2", MEL_D, 32, lambda f, d, v: mallet(f, d, v, 0.7), {"hall": 0.4, "echo": 0.3}, 0.9, 0.2)
    # ---------------- texture: tape hiss (circular noise -> loops perfectly)
    hiss = lp(hp(colored_noise(A.L, -3.0, 77), 300, 1), 5500, 2) * 0.004
    A.add("hiss", np.stack([hiss, np.roll(hiss, 12345)]), 0, swing=False)
    levels = {"lead": -21, "lead2": -22, "gtr": -23.5, "bass": -23, "drums": -23, "pad": -28, "arp": -31,
              "hiss": -52}
    return render_loop(A, "bgm_main", levels, target=-18.0)


def echo_fx(A, beats=0.75, repeats=5, fb=0.55):
    def fn(e):
        d = N(A.sec(beats))
        out = np.zeros((2, e.shape[-1] + d * repeats))
        mono = e.mean(axis=0)
        for k in range(1, repeats + 1):
            tap = lp(mono, 2600 - 250 * k, 1)
            mix_into(out, pan(tap, -0.6 if k % 2 else 0.6), d * k, fb ** (k - 1))
        return out
    return fn


BUS_FX = {
    "drums": lambda A: (lambda x: lp(sat(x * 1.2, 1.3) / 1.2, 7000, 2)),
    "gtr": lambda A: (lambda x: chorus(hp(x, 90, 2), rate=loop_lfo_freq(0.6, A.L), depth_ms=1.6, base_ms=8, mix=0.35)),
    "pad": lambda A: (lambda x: hp(x, 140, 2)),
    "bass": lambda A: (lambda x: lp(x, 1400, 2)),
    "lead": lambda A: (lambda x: hp(x, 220, 2)),
    "lead2": lambda A: (lambda x: hp(x, 200, 2)),
    "arp": lambda A: (lambda x: hp(x, 300, 2)),
}


def render_loop(A, name, levels, target=-18.0, echo_beats=0.75, extra_fx=None, returns=None):
    fx = {k: v(A) for k, v in BUS_FX.items()}
    fx.update(extra_fx or {})
    rt = {"hall": 0.55, "room": 0.5, "plate": 0.5, "echo": 0.45}
    rt.update(returns or {})
    effects = {"hall": (HALL, rt["hall"]), "room": (ROOM, rt["room"]), "plate": (PLATE, rt["plate"]),
               "echo": (echo_fx(A, echo_beats), rt["echo"], {"hall": 0.25})}
    pre = A.mixdown(fx, effects, levels)
    y = finalize_loop(pre, A.L, target_lufs=target)
    OUT.mkdir(parents=True, exist_ok=True)
    write_wav(OUT / f"{name}.wav", y, seed=7)
    print(f"{name}: {A.L / SR:.3f}s ({A.L} samples) {A.bars} bars @ {A.bpm} BPM")
    return y


# ============================================================================ SHOP (98 BPM, F major, shuffle, 32 bars)
SHOP_V = {  # rootless EP voicings
    "Fmaj7": [57, 60, 64, 67], "Dm7": [53, 57, 60, 64], "Gm7": [53, 57, 58, 62], "C7": [52, 57, 58, 62],
    "Am7": [55, 59, 60, 64], "D7": [54, 59, 60, 64], "Bbmaj7": [57, 60, 62, 65], "Bbm6": [55, 58, 61, 65],
    "D7b9": [54, 57, 60, 63],
}
SHOP_ROOT = {"Fmaj7": 41, "Dm7": 38, "Gm7": 43, "C7": 36, "Am7": 45, "D7": 38, "Bbmaj7": 34, "Bbm6": 34,
             "D7b9": 38}
SHOP_3RD = {"Fmaj7": 4, "Dm7": 3, "Gm7": 3, "C7": 4, "Am7": 3, "D7": 4, "Bbmaj7": 4, "Bbm6": 3, "D7b9": 4}
MEL_SA = [
    [(0, 0.5, 69), (0.5, 0.5, 72), (1, 1, 76), (2, 0.5, 74), (2.5, 0.5, 72), (3, 1, 69)],
    [(0.5, 0.5, 69), (1, 0.5, 72), (1.5, 0.5, 74), (2, 1.5, 77), (3.5, 0.5, 76)],
    [(0, 1, 74), (1, 0.5, 70), (1.5, 0.5, 74), (2, 1, 77), (3, 1, 74)],
    [(0, 1.5, 76), (1.5, 0.5, 67), (2, 0.5, 70), (2.5, 0.5, 72), (3, 1, 76)],
    [(0, 1, 72), (1, 0.5, 76), (1.5, 0.5, 79), (2, 1, 76), (3, 0.5, 72), (3.5, 0.5, 69)],
    [(0, 1, 78), (1, 0.5, 76), (1.5, 0.5, 74), (2, 1, 72), (3, 1, 69)],
    [(0, 0.5, 70), (0.5, 0.5, 74), (1, 0.5, 77), (1.5, 0.5, 76), (2, 1, 74), (3, 0.5, 70), (3.5, 0.5, 72)],
    [(0, 1.5, 76), (1.5, 0.5, 74), (2, 1, 72), (3, 1, None)],
]
MEL_SA2 = MEL_SA[:4] + [
    [(0, 0.5, 76), (0.5, 0.5, 79), (1, 1, 81), (2, 0.5, 79), (2.5, 0.5, 76), (3, 1, 72)],
    [(0, 1.5, 78), (1.5, 0.5, 81), (2, 0.5, 78), (2.5, 0.5, 76), (3, 1, 74)],
    [(0, 1, 77), (1, 0.5, 74), (1.5, 0.5, 70), (2, 0.5, 72), (2.5, 0.5, 74), (3, 1, 77)],
    [(0, 1, 76), (1, 0.5, 74), (1.5, 0.5, 76), (2, 2, 72)],
]
MEL_SB = [
    [(0, 1.5, 74), (1.5, 0.5, 72), (2, 1, 69), (3, 1, 77)],
    [(0, 1.5, 73), (1.5, 0.5, 72), (2, 1, 70), (3, 1, 67)],
    [(0, 1, 72), (1, 1, 76), (2, 1, 79), (3, 1, 76)],
    [(0, 1.5, 78), (1.5, 0.5, 75), (2, 2, 74)],
    [(0, 0.5, 70), (0.5, 0.5, 74), (1, 1, 77), (2, 0.5, 81), (2.5, 0.5, 79), (3, 1, 77)],
    [(0, 1, 76), (1, 1, 79), (2, 1, 82), (3, 1, 81)],
    [(0, 2, 81), (2, 1, 79), (3, 1, 76)],
    [(0, 0.5, 79), (0.5, 0.5, 77), (1, 0.5, 76), (1.5, 0.5, 74), (2, 1, 72), (3, 1, 70)],
]


def bgm_shop():
    A = Arr(98, 32, seed=21, s8=0.16)
    pa = ["Fmaj7", "Dm7", "Gm7", "C7", "Am7", "D7", "Gm7", "C7"]
    pb = ["Bbmaj7", "Bbm6", "Am7", "D7b9", "Gm7", "C7", "Fmaj7", "C7"]
    prog = pa + pa + pb + pa
    sect = ["A0"] * 8 + ["A1"] * 8 + ["B"] * 8 + ["A2"] * 8
    r = rng(22)
    comp = [[(0, 1.3, 0.75), (1.5, 0.4, 0.5), (2.5, 1.2, 0.6)],
            [(0, 0.4, 0.7), (0.5, 1.0, 0.55), (2, 0.4, 0.6), (3.5, 0.5, 0.5)],
            [(0, 2.0, 0.7), (2.5, 0.4, 0.5), (3.5, 0.4, 0.45)]]
    for bar, (ch, sc) in enumerate(zip(prog, sect)):
        b0 = bar * 4
        # EP comping
        pat = comp[bar % 3] if sc != "A0" or bar >= 2 else [(0, 3.8, 0.7)]
        for (bb, dd, vv) in pat:
            for k, m in enumerate(SHOP_V[ch]):
                A.add("ep", ep(float(mtof(m)), A.sec(dd), vv * (0.9 + 0.1 * r.normal()) * 0.7), b0 + bb, 1.0,
                      -0.35 + 0.2 * k, jitter_ms=6, send={"plate": 0.2})
        # bass
        root = SHOP_ROOT[ch]
        nr = SHOP_ROOT[prog[(bar + 1) % len(prog)]]
        if sc == "A0":
            line = [(0, 1.8, root), (2, 1.8, root + 7)]
        else:
            appr = nr + (1 if r.random() < 0.5 else -1)
            while appr - root > 9:
                appr -= 12
            while appr - root < -7:
                appr += 12
            line = [(0, 0.9, root), (1, 0.9, root + (7 if r.random() < 0.6 else 12)), (2, 0.9, root + SHOP_3RD[ch] +
                                                                                       (12 if r.random() < 0.3 else 0)),
                    (3, 0.85, appr)]
        for (bb, dd, m) in line:
            A.add("bass", tri_bass(float(mtof(m)), A.sec(dd), 0.8, sub=0.5, lpf=900, pluck=0.6), b0 + bb, 1.0, 0.0,
                  jitter_ms=4)
        # drums
        ds = {"room": 0.2}
        A.add("drums", kick(0.6, decay=0.18), b0, 1.0, 0, send=ds)
        A.add("drums", kick(0.5, decay=0.18), b0 + 2, 1.0, 0, send=ds)
        if bar % 2 == 1 and sc != "A0":
            A.add("drums", kick(0.35, decay=0.15), b0 + 2.5, 1.0, 0, send=ds)
        for sb in (1, 3):
            A.add("drums", brush(0.6, seed=bar * 4 + sb), b0 + sb, 1.0, 0.1, jitter_ms=5, send=ds)
            if sc != "A0":
                A.add("drums", snare(0.22, seed=bar * 4 + sb, decay=0.07), b0 + sb, 1.0, 0.1, jitter_ms=3, send=ds)
        for k in range(8):
            A.add("drums", shaker(0.55 if k % 2 else 0.32, seed=bar * 8 + k), b0 + k * 0.5, 1.0, 0.4, jitter_ms=5)
        if sc == "A2":
            for hb in (1, 3):
                A.add("drums", hat(0.3, seed=bar * 2 + hb, lpf=7000), b0 + hb, 1.0, -0.3, jitter_ms=3, send=ds)
        if bar % 2 == 1:
            A.add("drums", rim(0.35, seed=bar), b0 + 3.5, 1.0, -0.2, jitter_ms=4, send=ds)
        if bar % 8 == 7 and sc != "A2":
            for g, v in ((3.0, 0.3), (3.5, 0.4)):
                A.add("drums", snare(v, seed=700 + bar + int(g * 2), decay=0.09), b0 + g, 1.0, 0, send=ds)
        # warm pad under the bridge
        if sc == "B":
            A.add("pad", pad_voice([float(mtof(m)) for m in SHOP_V[ch]], A.sec(4.0), cutoff=1200, attack=0.5,
                                   release=1.2, seed=bar, level=0.1), b0, 1.0, send={"hall": 0.3})
    send = {"plate": 0.25, "echo": 0.18}
    chip_kw = dict(width=0.5, tri_mix=0.45, lpf=3000, vib_cents=10, vib_rate=5.2)
    place_melody(A, "lead", MEL_SA, 8, chip, send, 1.0, 0.1, legato_glide=True, **chip_kw)
    place_melody(A, "lead2", MEL_SB, 16, lambda f, d, v: mallet(f, d, v, 0.55), {"plate": 0.3, "echo": 0.2}, 1.0,
                 0.15)
    place_melody(A, "lead", MEL_SA2, 24, chip, send, 1.0, 0.1, legato_glide=True, **chip_kw)
    place_melody(A, "lead2", [[(b, d, m - 12 if m else None) for (b, d, m) in bar_] for bar_ in MEL_SA2], 24,
                 lambda f, d, v: mallet(f, d, v * 0.6, 0.5), {"plate": 0.3}, 1.0, -0.2)
    hiss = lp(hp(colored_noise(A.L, -3.0, 78), 300, 1), 5500, 2) * 0.004
    A.add("hiss", np.stack([hiss, np.roll(hiss, 9876)]), 0, swing=False)
    levels = {"lead": -21, "lead2": -22.5, "ep": -23, "bass": -23, "drums": -23.5, "pad": -29, "hiss": -52}
    return render_loop(A, "bgm_shop", levels, target=-18.0, echo_beats=0.5,
                       extra_fx={"ep": lambda x: chorus(hp(x, 110, 2), rate=loop_lfo_freq(4.5, A.L), depth_ms=0.6,
                                                         base_ms=4, mix=0.3)})


# ============================================================================ CORE (90 BPM, C# phrygian, 28 bars)
CORE_PAD = {"C#m9": [49, 56, 63, 64], "D/C#": [50, 57, 61, 66], "G#7b9": [56, 60, 63, 66, 69],
            "Amaj7": [57, 61, 64, 68], "B6": [59, 63, 66, 68]}
CORE_ROOT = {"C#m9": 37, "D/C#": 37, "G#7b9": 44, "Amaj7": 45, "B6": 47}
CORE_BELL = [
    [(0, 2, 73), (2, 1, 76), (3, 1, 74)],
    [(0, 3, 68), (3, 1, 71)],
    [(0, 1.5, 74), (1.5, 0.5, 73), (2, 2, 69)],
    [(0, 2, 72), (2, 2, 68)],
]
CORE_THEREMIN = [  # (beat offset from bar 16, midi) - holds then glides
    (0, 76), (3.4, 76), (4, 75), (6.4, 75), (6.8, 78), (7.6, 78), (8, 80), (10.0, 80), (10.6, 78), (11.6, 78),
    (12, 76), (15.6, 76), (16, 73), (17.6, 73), (18, 76), (19.6, 76), (20, 75), (21.6, 75), (22, 71), (23.6, 71),
    (24, 72), (25.6, 72), (26, 75), (27.6, 75), (28, 73), (31.0, 73)]


def bgm_core():
    A = Arr(90, 28, seed=31, s16=0.0)
    p1 = ["C#m9", "C#m9", "D/C#", "G#7b9"]
    p3 = ["Amaj7", "B6", "G#7b9", "C#m9"]
    prog = p1 * 4 + p3 * 2 + p1
    sect = ["P1"] * 8 + ["P2"] * 8 + ["P3"] * 8 + ["P4"] * 4
    r = rng(32)
    for bar, (ch, sc) in enumerate(zip(prog, sect)):
        b0 = bar * 4
        # dark pad + ring-mod shimmer
        fr = [float(mtof(m)) for m in CORE_PAD[ch]]
        pv = pad_voice(fr, A.sec(4.0), cutoff=900 if sc != "P3" else 1400, attack=1.2, release=2.0, seed=bar,
                       level=0.12)
        A.add("pad", pv, b0, 1.0, send={"hall": 0.4})
        tt = T(pv.shape[-1])
        rm = pv * np.sin(2 * np.pi * 311.13 * tt + 0.8 * np.sin(2 * np.pi * 0.37 * tt))
        A.add("alien", lp(rm, 2500, 2), b0, 1.0, send={"hall": 0.6})
        # pulse bass
        root = CORE_ROOT[ch]
        if sc == "P1" and bar < 2:
            steps = [(0, 0), (4, 0)]
        elif sc == "P4":
            steps = [(0, 0), (3, 0), (4, 12), (6, 0)]
        else:
            steps = [(k, iv) for k, iv in enumerate([0, 0, 12, 0, 0, 12, 0, 7])]
        acc = [1.0, 0.55, 0.8, 0.55, 0.9, 0.55, 0.8, 0.65]
        for (k, iv) in steps:
            A.add("bass", saw_bass(float(mtof(root + iv)), A.sec(0.42), 0.75 * acc[k], cutoff=320, env_amt=700),
                  b0 + k * 0.5, 1.0, 0.0, jitter_ms=2)
        # heartbeat
        hb = [(0, 0.85), (0.5, 0.6)] + ([(2, 0.6), (2.5, 0.45)] if sc == "P3" else [])
        for bb, v in hb:
            A.add("drums", tom(52.0, v, 0.28), b0 + bb, 1.0, 0, send={"room": 0.15})
        # geiger ticks
        if sc in ("P2", "P3", "P4"):
            for k in range(16):
                if r.random() < (0.22 if sc != "P4" else 0.12):
                    f = r.uniform(1200, 2100)
                    tk = modes_tick(f, int(r.integers(1 << 30)))
                    A.add("ticks", tk, b0 + k * 0.25, 1.0, float(r.uniform(-0.7, 0.7)), send={"room": 0.3},
                          swing=False)
        # P3 drive: toms + soft snare + hats
        if sc == "P3":
            for bb, f, v in ((1.5, 98.0, 0.5), (3.0, 82.0, 0.6), (3.5, 73.0, 0.45)):
                A.add("drums", tom(f, v, 0.25), b0 + bb, 1.0, 0.2 * np.sign(f - 85), send={"hall": 0.15})
            A.add("drums", snare(0.4, seed=bar, tone_hz=170, decay=0.16, lpf=5000), b0 + 2, 1.0, 0, send={"hall": 0.2})
            for k in range(8):
                A.add("drums", hat(0.25 if k % 2 else 0.15, seed=bar * 8 + k, lpf=6500), b0 + k * 0.5, 1.0, 0.3)
        # risers into P2 / P3
        if bar in (7, 15, 23):
            A.add("fx", riser(A.sec(4.0), 250, 2500, seed=bar), b0, 1.0, 0, send={"hall": 0.4})
    # bells P2 + P3
    for start, g in ((8, 1.0), (12, 0.9), (16, 0.6), (20, 0.6), (24, 0.5)):
        mel = CORE_BELL if start != 24 else CORE_BELL[:1]
        place_melody(A, "bell", mel, start, lambda f, d, v: fm_bell(f, d, v * g, 3.5, 1.2, 1.3),
                     {"hall": 0.45, "echo": 0.35}, 1.0, 0.25)
    # theremin lead in P3
    pth = [(A.sec(b), m) for (b, m) in CORE_THEREMIN]
    A.add("lead", theremin(pth, 0.8), A.pos(16), 1.0, -0.1, send={"hall": 0.45, "echo": 0.25}, swing=False)
    levels = {"pad": -24, "alien": -31, "bass": -22, "drums": -24, "ticks": -33, "bell": -23, "lead": -21, "fx": -32}
    return render_loop(A, "bgm_core", levels, target=-18.0, echo_beats=0.75,
                       extra_fx={"alien": lambda x: chorus(x, rate=loop_lfo_freq(0.3, A.L), depth_ms=3, base_ms=12,
                                                           mix=0.5),
                                 "bell": lambda x: hp(x, 250, 2), "ticks": lambda x: lp(x, 4500, 2)},
                       returns={"hall": 0.65})


def modes_tick(f, seed):
    from nb.dsp import modes as _modes
    n = N(0.06)
    return _modes(n, [f, f * 1.9], [1, 0.3], [0.008, 0.004], seed=seed % 100000) * 0.3


# ============================================================================ STINGER (prestige, ~8.5 s, one-shot)
def stinger_prestige():
    A = Arr(84, 3, tail=0.0, seed=41)  # 3 bars = 8.57 s, no wrap (L == n)
    # rising chip triplets + accelerating toms + riser
    for k, m in enumerate([62, 66, 69, 74, 78, 81]):
        A.add("lead", chip(float(mtof(m)), A.sec(0.3), 0.55 + 0.05 * k, width=0.25, vib_cents=0, lpf=3000), k / 3, 1.0,
              -0.3 + 0.12 * k, send={"hall": 0.3})
    for bb, v in zip([0, 0.67, 1.17, 1.5, 1.75, 1.92], [0.35, 0.42, 0.5, 0.58, 0.66, 0.75]):
        A.add("drums", tom(73.4, v, 0.22), bb, 1.0, 0, send={"hall": 0.2}, swing=False)
    A.add("fx", riser(A.sec(2.0), 300, 3000, seed=5, q=1.2), 0, 1.0, send={"hall": 0.3})
    # stabs: Bbmaj9 -> C6/9 -> D add9 (bVI - bVII - I)
    stabs = [(2.0, 0.75, [46, 53, 57, 62, 65, 72], 34), (2.75, 0.75, [48, 55, 57, 62, 64], 36),
             (3.5, 6.0, [50, 57, 62, 66, 69, 76], 38)]
    for bb, dd, v, root in stabs:
        fr = [float(mtof(m)) for m in v]
        A.add("gtr", strum(fr, A.sec(dd), 0.8, seed=int(bb * 10), t60=3.0 if dd > 2 else 1.2, bright=0.4), bb, 1.0,
              -0.25, send={"hall": 0.3}, swing=False)
        A.add("pad", pad_voice(fr[1:], A.sec(dd), cutoff=1500, attack=0.05, release=1.8 if dd > 2 else 0.3,
                               seed=int(bb * 10), level=0.12), bb, 1.0, send={"hall": 0.4}, swing=False)
        A.add("bass", tri_bass(float(mtof(root)), A.sec(dd), 0.9, sub=0.8), bb, 1.0, swing=False)
        A.add("drums", kick(0.8 if dd > 2 else 0.6), bb, 1.0, send={"room": 0.2}, swing=False)
    # final chord: bells + cymbal swell + fanfare
    for k, m in enumerate([74, 78, 81, 86]):
        A.add("bell", fm_bell(float(mtof(m)), 2.0, 0.6, 2.0, 1.0, 1.6), 3.5 + k / 6, 1.0, -0.4 + 0.25 * k,
              send={"hall": 0.4}, swing=False)
    n = N(4.0)
    cym = lp(hp(white(n, 9), 2500, 2), 6500, 2) * env_points(n, [(0, 0), (0.05, 1), (3.8, 0)]) ** 2
    A.add("fx", np.stack([cym, np.roll(cym, 777)]) * 0.12, 3.5, 1.0, send={"hall": 0.3}, swing=False)
    A.add("lead", chip(float(mtof(81)), A.sec(0.45), 0.7, width=0.25, lpf=3000), 3.5, 1.0, 0.1, send={"hall": 0.3},
          swing=False)
    A.add("lead", chip(float(mtof(86)), A.sec(3.0), 0.75, width=0.25, lpf=3000, glide_from=float(mtof(81)),
                       vib_cents=16), 4.0, 1.0, 0.1, send={"hall": 0.35, "echo": 0.2}, swing=False)
    levels = {"lead": -20, "gtr": -22, "pad": -24, "bass": -23, "drums": -22, "bell": -25, "fx": -28}
    fx = {k: v(A) for k, v in BUS_FX.items()}
    effects = {"hall": (HALL, 0.6), "room": (ROOM, 0.5), "echo": (echo_fx(A, 0.75), 0.4, {"hall": 0.25})}
    pre = A.mixdown(fx, effects, levels)
    pre = pre[:, :N(8.6)]
    y = finalize_oneshot(pre, target_lufs=-17.0, fade_out=2.0, wow_ms=0.3)
    OUT.mkdir(parents=True, exist_ok=True)
    write_wav(OUT / "stinger_prestige.wav", y, seed=7)
    print(f"stinger_prestige: {y.shape[-1] / SR:.2f}s")


if __name__ == "__main__":
    which = sys.argv[1:] or ["main", "shop", "core", "stinger"]
    if "main" in which:
        bgm_main()
    if "shop" in which:
        bgm_shop()
    if "core" in which:
        bgm_core()
    if "stinger" in which:
        stinger_prestige()
