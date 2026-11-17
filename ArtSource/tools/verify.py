"""Verify every .aseprite in Assets/Art against Docs/ASSET_SPEC.md (sizes, tags, frame counts,
durations) and the 32-color palette. Run: python ArtSource/tools/verify.py"""
import json, os, subprocess, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from px import ROOT, ASEPRITE, RGB, TMP

ART = os.path.join(ROOT, "Assets", "Art")
ITEMS = """snack_shrimp choco_bun water_bottle lighter cup_ramen bandage battery canned_food cigarettes
mask_filter diary teddy_bear broken_radio melted_watch rusty_toolbox lp_record gas_mask medkit gold_tooth
film_camera generator_part glow_jelly warm_stone zero_g_spoon humming_cube alien_remote crystal_shard
core_fragment singing_skull star_map""".split()
CUSTOMERS = "survivor_a survivor_b merchant raider soldier cultist collector alien".split()

# path -> (w, h, {tag: (frames, ms)} or None for "any/none")
SPEC = {
    "Characters/repairman.aseprite": (48, 48, {"idle": (4, 150), "swing": (6, 70), "swing_crit": (8, 60), "cheer": (4, 120)}),
    "Characters/helper.aseprite": (32, 32, {"idle": (4, 160), "swing": (4, 90)}),
    "Nexus/nexus.aseprite": (80, 96, {**{f"s{i}_idle": (4, 200) for i in range(5)}, "hit": (2, 50), "destroy": (12, 80)}),
    "FX/fx_hit_spark.aseprite": (16, 16, {"play": (4, 50)}),
    "FX/fx_crit_burst.aseprite": (32, 32, {"play": (5, 50)}),
    "FX/fx_debris.aseprite": (8, 8, {"a": (1, None), "b": (1, None), "c": (1, None), "d": (1, None)}),
    "FX/fx_box_poof.aseprite": (24, 24, {"play": (5, 60)}),
    "FX/fx_nut_sparkle.aseprite": (8, 8, {"play": (4, 80)}),
    "FX/fx_level_up.aseprite": (32, 32, {"play": (6, 60)}),
    "Environment/bg_sky.aseprite": (384, 96, None),
    "Environment/bg_far.aseprite": (384, 96, None),
    "Environment/bg_mid.aseprite": (384, 96, None),
    "Environment/ground.aseprite": (384, 16, None),
    "Environment/shop_stall.aseprite": (128, 80, {"idle": (4, 200)}),
    "Environment/props.aseprite": (None, None, None),
    "UI/ui_panel.aseprite": (24, 24, None),
    "UI/ui_panel_dark.aseprite": (24, 24, None),
    "UI/ui_button.aseprite": (24, 16, {"normal": (1, None), "hover": (1, None), "pressed": (1, None)}),
    "UI/ui_slot.aseprite": (20, 20, {"empty": (1, None), "selected": (1, None)}),
    "UI/ui_bar.aseprite": (64, 8, {"frame": (1, None), "fill": (1, None)}),
    "UI/ui_icons.aseprite": (16, 16, {n: (1, None) for n in "nut hammer helper speed crit box warehouse shelf customer shop settings prestige_core close pin ghost".split()}),
    "UI/ui_nut_small.aseprite": (8, 8, None),
    "UI/ui_chat.aseprite": (32, 32, None),  # added on request (guest-book speech bubble)
}
for b in "common uncommon rare alien unknown".split():
    SPEC[f"Boxes/box_{b}.aseprite"] = (16, 16, {"idle": (2, 300), "open": (5, 70)})
for it in ITEMS:
    SPEC[f"Items/item_{it}.aseprite"] = (16, 16, "item")
for c in CUSTOMERS:
    SPEC[f"Customers/customer_{c}.aseprite"] = (32, 32, {"walk": (6, 100), "idle": (4, 180), "buy": (4, 120), "reject": (4, 120)})

PAL = set(RGB)


def inspect(path):
    d = os.path.join(TMP, "_verify")
    os.makedirs(d, exist_ok=True)
    js, png = os.path.join(d, "v.json"), os.path.join(d, "v.png")
    subprocess.run([ASEPRITE, "-b", "--list-tags", "--list-layers", "--list-slices", path, "--data", js,
                    "--format", "json-array", "--sheet", png], check=True, capture_output=True)
    meta = json.load(open(js, encoding="utf-8"))
    return meta, np.array(Image.open(png).convert("RGBA"))


def main():
    ok = True
    missing = []
    for rel, (w, h, tags) in SPEC.items():
        p = os.path.join(ART, rel)
        if not os.path.exists(p):
            missing.append(rel)
            continue
        meta, sheet = inspect(p)
        errs = []
        fw, fh = meta["frames"][0]["sourceSize"]["w"], meta["frames"][0]["sourceSize"]["h"]
        if w and (fw, fh) != (w, h):
            errs.append(f"size {fw}x{fh} != {w}x{h}")
        got = {t["name"]: (t["from"], t["to"]) for t in meta["meta"]["frameTags"]}
        durs = [f["duration"] for f in meta["frames"]]
        if isinstance(tags, dict):
            for tn, (n, ms) in tags.items():
                if tn not in got:
                    errs.append(f"missing tag {tn}")
                    continue
                a, b = got[tn]
                if b - a + 1 != n:
                    errs.append(f"tag {tn} has {b - a + 1} frames, want {n}")
                if ms and any(durs[i] != ms for i in range(a, b + 1)):
                    errs.append(f"tag {tn} durations {durs[a:b + 1]} want {ms}")
        elif tags == "item":
            if len(durs) > 1 and "idle" not in got:
                errs.append("multi-frame item without idle tag")
        px = sheet[sheet[..., 3] > 0]
        if len(px) and not np.all(sheet[..., 3][sheet[..., 3] > 0] == 255):
            errs.append("semi-transparent pixels")
        off = set(map(tuple, px[:, :3].tolist())) - PAL
        if off:
            errs.append(f"{len(off)} off-palette colors")
        status = "OK " if not errs else "ERR"
        ok &= not errs
        print(f"{status} {rel:45s} {fw}x{fh} tags={list(got)} {'; '.join(errs)}")
    print(f"\n{len(SPEC) - len(missing)}/{len(SPEC)} present")
    if missing:
        print("missing:", ", ".join(missing))
    return ok and not missing


if __name__ == "__main__":
    sys.exit(0 if main() else 1)
