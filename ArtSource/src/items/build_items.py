"""Build all 30 item icons -> Assets/Art/Items/item_<id>.aseprite"""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "tools"))
sys.path.insert(0, os.path.dirname(__file__))
from px import *
from itemsheet import sheet
import items_common, items_uncommon, items_rare, items_alien

ORDER = """snack_shrimp choco_bun water_bottle lighter cup_ramen bandage battery canned_food cigarettes
mask_filter diary teddy_bear broken_radio melted_watch rusty_toolbox lp_record gas_mask medkit gold_tooth
film_camera generator_part glow_jelly warm_stone zero_g_spoon humming_cube alien_remote crystal_shard
core_fragment singing_skull star_map""".split()


def centered(frames):
    """shift all frames so the union bbox is centred in 16x16"""
    u = np.zeros((16, 16), bool)
    for f in frames:
        u |= f >= 0
    ys, xs = np.where(u)
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    dx = (16 - (x1 - x0 + 1)) // 2 - x0
    dy = (16 - (y1 - y0 + 1)) // 2 - y0
    out = []
    for f in frames:
        g = blank(16, 16)
        blit(g, f, dx, dy)
        out.append(g)
    return out


def all_items():
    d = {}
    for mod in (items_common, items_uncommon, items_rare):
        for k, v in mod.ITEMS.items():
            d[k] = centered([v])
    for k, v in items_alien.ANIM.items():
        d[k] = centered(v)
    return d


if __name__ == "__main__":
    items = all_items()
    assert sorted(items) == sorted(ORDER), set(ORDER) ^ set(items)
    for k in ORDER:
        fr = items[k]
        a = Asset(f"Assets/Art/Items/item_{k}.aseprite", 16, 16, ["icon"])
        if len(fr) > 1:
            a.tag("idle", fr, 150)
        else:
            a.add(fr[0])
        if "--export" in sys.argv:
            export(a, "items")
        else:
            write_previews(a, "items")
    sheet({k: items[k] for k in ORDER}, os.path.join(PREVIEWS, "items", "_items_sheet.png"), s=4, cols=10)
