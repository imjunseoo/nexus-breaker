"""Build ArtSource/previews/contact_sheet.png (+ scene_mockup.png) by reading the actual
.aseprite files back through the Aseprite CLI (so the sheet shows what Unity will import)."""
import json, os, subprocess, sys
import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(__file__))
from px import ROOT, ASEPRITE, TMP, PREVIEWS, font
from verify import ITEMS, CUSTOMERS

ART = os.path.join(ROOT, "Assets", "Art")
OUT = os.path.join(TMP, "_sheet")
BG = (92, 88, 86)
INK = (230, 224, 210)


def frames_of(rel, all_layers=False, layer=None):
    """returns (list of RGBA frames, tags dict name->(from,to))"""
    os.makedirs(OUT, exist_ok=True)
    key = rel.replace("/", "_").replace(".aseprite", "") + (f"_{layer}" if layer else "")
    js, png = os.path.join(OUT, key + ".json"), os.path.join(OUT, key + ".png")
    cmd = [ASEPRITE, "-b", "--list-tags"]
    if layer:
        cmd += ["--layer", layer]
    cmd += [os.path.join(ART, rel), "--data", js, "--format", "json-array", "--sheet", png, "--sheet-type", "horizontal"]
    subprocess.run(cmd, check=True, capture_output=True)
    d = json.load(open(js, encoding="utf-8"))
    sheet = Image.open(png).convert("RGBA")
    fr = []
    for f in d["frames"]:
        r = f["frame"]
        fr.append(sheet.crop((r["x"], r["y"], r["x"] + r["w"], r["y"] + r["h"])))
    tags = {t["name"]: (t["from"], t["to"]) for t in d["meta"]["frameTags"]}
    return fr, tags


def up(im, s):
    return im.resize((im.width * s, im.height * s), Image.NEAREST)


class Sheet:
    def __init__(self, width):
        self.W = width
        self.blocks = []  # (title, image)

    def add(self, title, im):
        self.blocks.append((title, im))

    def row(self, ims, s, gap=8, labels=None):
        ims = [up(i, s) for i in ims]
        lab = 14 if labels else 0
        w = sum(i.width for i in ims) + gap * (len(ims) + 1)
        h = max(i.height for i in ims) + gap * 2 + lab
        out = Image.new("RGBA", (w, h), BG + (255,))
        d = ImageDraw.Draw(out)
        x = gap
        for k, i in enumerate(ims):
            out.alpha_composite(i, (x, gap + lab + (h - 2 * gap - lab - i.height)))
            if labels:
                d.text((x, 2), labels[k], fill=INK, font=font(11))
            x += i.width + gap
        return out

    def grid(self, ims, s, cols, labels=None, gap=8):
        rows = []
        for r in range(0, len(ims), cols):
            rows.append(self.row(ims[r:r + cols], s, gap, labels[r:r + cols] if labels else None))
        w = max(r.width for r in rows)
        out = Image.new("RGBA", (w, sum(r.height for r in rows)), BG + (255,))
        y = 0
        for r in rows:
            out.alpha_composite(r, (0, y)); y += r.height
        return out

    def render(self, path):
        title_h = 26
        H = sum(b.height + title_h for _, b in self.blocks) + 40
        W = max(self.W, max(b.width for _, b in self.blocks) + 20)
        out = Image.new("RGBA", (W, H), BG + (255,))
        d = ImageDraw.Draw(out)
        d.text((10, 8), "Nexus Breaker — art contact sheet (all frames read back from Assets/Art/*.aseprite)", fill=INK, font=font(16))
        y = 36
        for t, b in self.blocks:
            d.text((10, y + 4), t, fill=(242, 184, 75), font=font(14))
            y += title_h
            out.alpha_composite(b, (10, y))
            y += b.height
        out.save(path)
        return path


def tag_frames(fr, tags, name):
    a, b = tags[name]
    return fr[a:b + 1]


def main():
    S = Sheet(1800)
    # characters
    fr, tg = frames_of("Characters/repairman.aseprite")
    ims, labs = [], []
    for t in ("idle", "swing", "swing_crit", "cheer"):
        for i, f in enumerate(tag_frames(fr, tg, t)):
            ims.append(f); labs.append(f"{t} {i + 1}")
    S.add("Characters / repairman (48x48)", S.grid(ims, 3, 11, labs))
    fr, tg = frames_of("Characters/helper.aseprite")
    S.add("Characters / helper (32x32)", S.row(fr, 4, labels=[f"{t} {i+1}" for t in ("idle", "swing") for i in range(4)]))
    # nexus
    fr, tg = frames_of("Nexus/nexus.aseprite")
    st = [tag_frames(fr, tg, f"s{i}_idle")[0] for i in range(5)] + tag_frames(fr, tg, "hit")
    S.add("Nexus (80x96) — s0..s4, hit", S.row(st, 3, labels=["s0", "s1", "s2", "s3", "s4", "hit 1", "hit 2"]))
    S.add("Nexus — destroy (12 frames)", S.row(tag_frames(fr, tg, "destroy"), 2, labels=[str(i + 1) for i in range(12)]))
    # items
    ims, labs = [], []
    for it in ITEMS:
        f, _ = frames_of(f"Items/item_{it}.aseprite")
        ims.append(f[0]); labs.append(it[:12])
    S.add("Items (16x16) — 30", S.grid(ims, 4, 15, labs))
    # boxes
    ims, labs = [], []
    for b in "common uncommon rare alien unknown".split():
        f, tg = frames_of(f"Boxes/box_{b}.aseprite")
        for i, x in enumerate(f):
            ims.append(x); labs.append(f"{b[:5]} {'idle' if i < 2 else 'open'}{i + 1 if i < 2 else i - 1}")
    S.add("Boxes (16x16) — idle 2 + open 5", S.grid(ims, 4, 14, labs))
    # fx
    blocks = []
    for name in "fx_hit_spark fx_crit_burst fx_debris fx_box_poof fx_nut_sparkle fx_level_up".split():
        f, _ = frames_of(f"FX/{name}.aseprite")
        blocks.append((name, f))
    rows = [S.row(f, 4 if f[0].width < 32 else 3, labels=[(n if i == 0 else "") + f" {i+1}" for i in range(len(f))], gap=14) for n, f in blocks]
    w = max(r.width for r in rows)
    fxim = Image.new("RGBA", (w, sum(r.height for r in rows)), BG + (255,))
    y = 0
    for r in rows:
        fxim.alpha_composite(r, (0, y)); y += r.height
    S.add("FX", fxim)
    # customers
    ims, labs = [], []
    for c in CUSTOMERS:
        f, tg = frames_of(f"Customers/customer_{c}.aseprite")
        for t in ("walk", "idle", "buy", "reject"):
            a, b = tg[t]
            pick = {"walk": [a, a + 2, a + 4], "idle": [a], "buy": [a + 1], "reject": [a + 1]}[t]
            for p in pick:
                ims.append(f[p]); labs.append(f"{c[:9]} {t}")
    S.add("Customers (32x32) — walk 1/3/5, idle, buy, reject", S.grid(ims, 3, 12, labs))
    # environment
    env = {}
    for n in "bg_sky bg_far bg_mid ground shop_stall props".split():
        env[n], _ = frames_of(f"Environment/{n}.aseprite")
    S.add("Environment — bg_sky / bg_far / bg_mid / ground", S.grid([env["bg_sky"][0], env["bg_far"][0], env["bg_mid"][0], env["ground"][0]], 2, 2))
    S.add("Environment — shop_stall idle (4) / props", S.row(env["shop_stall"] + [env["props"][0]], 2))
    # UI (multi-state files use one 1-frame tag per state/icon)
    ui_ims, ui_labs = [], []
    for n in ("ui_panel", "ui_panel_dark", "ui_button", "ui_slot", "ui_bar", "ui_nut_small"):
        f, tg = frames_of(f"UI/{n}.aseprite")
        if tg:
            for t, (a0, _) in tg.items():
                ui_ims.append(f[a0]); ui_labs.append(f"{n}/{t}")
        else:
            ui_ims.append(f[0]); ui_labs.append(n)
    S.add("UI", S.row(ui_ims, 4, labels=ui_labs, gap=14))
    f, tg = frames_of("UI/ui_icons.aseprite")
    S.add("UI — ui_icons (15 tags)", S.row([f[a0] for t, (a0, _) in tg.items()], 4, labels=list(tg), gap=14))
    p = S.render(os.path.join(PREVIEWS, "contact_sheet.png"))
    print(p)

    # scene mockup at 3x: 384x96 main strip
    sc = Image.new("RGBA", (384, 96), (0, 0, 0, 255))
    sc.alpha_composite(env["bg_sky"][0], (0, 0))
    sc.alpha_composite(env["bg_far"][0], (0, 0))
    sc.alpha_composite(env["bg_mid"][0], (0, 0))
    sc.alpha_composite(env["ground"][0], (0, 80))
    nx, _ = frames_of("Nexus/nexus.aseprite")
    sc.alpha_composite(nx[0], (96 - 40, 80 - 96))
    sc.alpha_composite(env["shop_stall"][0], (384 - 128 - 8, 80 - 80))
    pr, _ = frames_of("Environment/props.aseprite", layer="oil_drum")
    sc.alpha_composite(pr[0].crop((78, 11, 88, 26)), (236, 80 - 15))
    rm, tg = frames_of("Characters/repairman.aseprite")
    sc.alpha_composite(rm[tg["swing"][0] + 3], (140 - 24, 80 - 48))
    hp, _ = frames_of("Characters/helper.aseprite")
    sc.alpha_composite(hp[0].transpose(Image.FLIP_LEFT_RIGHT), (30 - 16, 80 - 32))
    for i, (c, x) in enumerate((("collector", 262), ("survivor_a", 300), ("alien", 334))):
        f, tg = frames_of(f"Customers/customer_{c}.aseprite")
        sc.alpha_composite(f[tg["idle"][0]] if i == 0 else f[tg["walk"][0] + i], (x - 16, 80 - 32))
    bx, _ = frames_of("Boxes/box_rare.aseprite")
    sc.alpha_composite(bx[0], (158, 80 - 16))
    up(sc, 3).save(os.path.join(PREVIEWS, "scene_mockup.png"))
    # transparent-mode mockup
    tr = Image.new("RGBA", (384, 96), (0, 0, 0, 0))
    tr.alpha_composite(env["ground"][0], (0, 80))
    tr.alpha_composite(nx[0], (96 - 40, 80 - 96))
    tr.alpha_composite(env["shop_stall"][0], (384 - 128 - 8, 80 - 80))
    tr.alpha_composite(rm[tg["swing"][0] + 3] if False else rm[0], (140 - 24, 80 - 48))
    desk = Image.new("RGBA", (384, 96), (52, 84, 120, 255))
    desk.alpha_composite(tr)
    up(desk, 3).save(os.path.join(PREVIEWS, "scene_mockup_transparent.png"))


if __name__ == "__main__":
    main()
