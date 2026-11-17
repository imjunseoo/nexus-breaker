"""ui_chat — guest-book speech-bubble panel. 32x32, 1 frame, no tags.
9-slice border 8px (applied by code at runtime). Corner cells hold all decoration:
top-left rivet, top-right typing dots, bottom-left tail pointing LEFT (toward the stall).
Edges are uniform along their stretch axis; centre is flat '2' (same as ui_panel_dark)."""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "tools"))
from px import *

# each row = left 8px cell + middle key (repeated 16px) + right 8px cell
ROWS = [
    ("......11", "1", "111111.."),   # 0  rounded top
    (".....1jj", "j", "jjjjjj1."),   # 1  header highlight
    ("....1jii", "i", "iiiiiih1"),   # 2
    ("....1kji", "i", "iiiiiih1"),   # 3  rivet
    ("....1jgi", "i", "ixixixh1"),   # 4  typing dots
    ("....1iii", "i", "iwiwiwh1"),   # 5
    ("....1hhh", "h", "hhhhhhg1"),   # 6  header shadow
    ("....1111", "1", "11111111"),   # 7  separator / inset top
] + [("....1hg1", "2", "2222hgg1")] * 16 + [   # 8..23 stretchable sides
    ("....1hg1", "2", "2222hgg1"),   # 24
    ("...1hhg1", "2", "2222hgg1"),   # 25 tail begins
    ("..1hhgg1", "2", "2222hgg1"),   # 26
    (".1hhggg1", "2", "2222hgg1"),   # 27
    ("1hggggg1", "h", "hhhhhgg1"),   # 28 tail tip (points left) + inset bottom light edge
    (".11ggggg", "g", "ggggggg1"),   # 29
    ("...1gggg", "g", "gggggg1."),   # 30
    ("....1111", "1", "111111.."),   # 31 rounded bottom
]


def chat():
    assert len(ROWS) == 32
    return parse("\n".join(l + m * 16 + r for l, m, r in ROWS))


if __name__ == "__main__":
    img = chat()
    a = Asset("Assets/Art/UI/ui_chat.aseprite", 32, 32, ["chat"])
    a.add(img)
    a.slices.append(dict(name="ui_chat", rect=(0, 0, 32, 32), center=(8, 8, 16, 16)))
    a.preview_bg = (70, 66, 72)
    if "--export" in sys.argv:
        export(a, "ui", s=6)
    # 9-slice preview at the in-game size (160x96)
    def nine(im, b, w, h):
        H0, W0 = im.shape
        xs = list(range(b)) + [b + (i * (W0 - 2 * b)) // (w - 2 * b) for i in range(w - 2 * b)] + list(range(W0 - b, W0))
        ys = list(range(b)) + [b + (i * (H0 - 2 * b)) // (h - 2 * b) for i in range(h - 2 * b)] + list(range(H0 - b, H0))
        out = blank(w, h)
        for j, yy in enumerate(ys):
            for i, xx in enumerate(xs):
                out[j, i] = im[yy, xx]
        return out
    big = nine(img, 8, 160, 96)
    from PIL import Image
    p1 = scaled(img, 8, (70, 66, 72)); p2 = scaled(big, 3, (70, 66, 72))
    out = Image.new("RGBA", (p1.width + p2.width + 30, max(p1.height, p2.height) + 20), (70, 66, 72, 255))
    out.alpha_composite(p1, (10, 10)); out.alpha_composite(p2, (p1.width + 20, 10))
    out.save(os.path.join(PREVIEWS, "ui", "ui_chat_9slice_160x96.png"))
