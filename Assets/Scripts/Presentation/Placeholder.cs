using System.Collections.Generic;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>아트가 아직 없을 때 쓰는 런타임 생성 도트 스프라이트(팔레트 색만 사용).</summary>
    public static class Placeholder
    {
        public const float PPU = 32f;
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public enum Shape { Rect, Crystal, Person, Box, Circle, Panel }

        public static Sprite Get(Shape shape, int w, int h, Color32 fill, Color32 outline, bool pivotBottom)
        {
            string key = $"{shape}_{w}_{h}_{fill.r}{fill.g}{fill.b}_{outline.r}{outline.g}{outline.b}_{pivotBottom}";
            if (Cache.TryGetValue(key, out var s) && s != null) return s;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "ph_" + key,
            };
            var px = new Color32[w * h];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < px.Length; i++) px[i] = clear;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (Inside(shape, x, y, w, h)) px[y * w + x] = fill;

            // 1px 외곽선
            var outlined = (Color32[])px.Clone();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (px[y * w + x].a == 0) continue;
                bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1
                            || px[y * w + x - 1].a == 0 || px[y * w + x + 1].a == 0
                            || px[(y - 1) * w + x].a == 0 || px[(y + 1) * w + x].a == 0;
                if (edge) outlined[y * w + x] = outline;
            }

            // 좌상단 하이라이트 한 줄
            var hi = Color32.Lerp(fill, new Color32(255, 255, 255, 255), 0.35f);
            for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int i = y * w + x;
                if (outlined[i].a == 0 || outlined[i].Equals(outline)) continue;
                if (outlined[i + 1].Equals(outline) || outlined[i - w].Equals(outline)) continue;
                if (outlined[i - 1].Equals(outline) || outlined[i + w].Equals(outline)) outlined[i] = hi;
            }

            tex.SetPixels32(outlined);
            tex.Apply(false, false);
            var pivot = pivotBottom ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, PPU, 0, SpriteMeshType.FullRect,
                shape == Shape.Panel ? new Vector4(4, 4, 4, 4) : Vector4.zero);
            s.name = key;
            Cache[key] = s;
            return s;
        }

        static bool Inside(Shape shape, int x, int y, int w, int h)
        {
            float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
            switch (shape)
            {
                case Shape.Crystal:
                    // 비스듬한 결정 기둥: 아래는 넓고 위는 뾰족
                    float half = Mathf.Lerp(0.42f, 0.04f, v);
                    float center = 0.5f + (v - 0.5f) * 0.12f;
                    return Mathf.Abs(u - center) <= half;
                case Shape.Person:
                    bool head = (u - 0.5f) * (u - 0.5f) * 4f + (v - 0.82f) * (v - 0.82f) * 25f <= 0.25f;
                    bool body = v < 0.70f && v > 0.0f && Mathf.Abs(u - 0.5f) < 0.22f;
                    return head || body;
                case Shape.Box:
                    return v < 0.8f && u > 0.08f && u < 0.92f;
                case Shape.Circle:
                    return (u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f) <= 0.25f;
                default:
                    return true;
            }
        }

        public static Sprite Nexus() => Get(Shape.Crystal, 80, 96, Palette.Purple, Palette.Void, true);
        public static Sprite Repairman() => Get(Shape.Person, 20, 32, Palette.Rust3, Palette.Outline, true);
        public static Sprite Helper() => Get(Shape.Person, 14, 24, Palette.Moss1, Palette.Outline, true);
        public static Sprite Customer(Color32 c) => Get(Shape.Person, 16, 28, c, Palette.Outline, true);
        public static Sprite Box(Grade g) => Get(Shape.Box, 16, 16, Palette.GradeColor(g), Palette.Outline, true);
        public static Sprite Item(Grade g) => Get(Shape.Circle, 14, 14, Palette.GradeColor(g), Palette.Outline, false);
        public static Sprite Stall() => Get(Shape.Rect, 128, 64, Palette.Rust1, Palette.Outline, true);
        public static Sprite Ground() => Get(Shape.Rect, 384, 16, Palette.Dust0, Palette.Rust0, false);
        public static Sprite Spark() => Get(Shape.Circle, 6, 6, Palette.Lamp1, Palette.Lamp0, false);
        public static Sprite Debris() => Get(Shape.Rect, 3, 3, Palette.Violet, Palette.Void, false);
        public static Sprite Panel() => Get(Shape.Panel, 12, 12, Palette.Ash0, Palette.Black, false);
        public static Sprite PanelDark() => Get(Shape.Panel, 12, 12, Palette.Shadow, Palette.Black, false);
        public static Sprite Button() => Get(Shape.Panel, 12, 12, Palette.Rust2, Palette.Outline, false);
        public static Sprite Slot() => Get(Shape.Panel, 12, 12, Palette.Shadow, Palette.Ash1, false);
        public static Sprite White() => Get(Shape.Rect, 4, 4, new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), false);
    }
}
