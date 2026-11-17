using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace NexusBreaker
{
    /// <summary>
    /// uGUI 생성 헬퍼. 모든 좌표는 "내부 픽셀"(캔버스 스케일 적용 전), 부모의 왼쪽 위 기준.
    /// </summary>
    public static class Ui
    {
        // ───────────────────────── 폰트 ─────────────────────────

        static TMP_FontAsset _font;
        public const float FontSize = 11f;   // → Galmuri11 12px
        public const float SmallFont = 9f;   // → Galmuri9 10px
        // 8 이하 → Galmuri7 8px

        public static TMP_FontAsset Font
        {
            get
            {
                if (_font != null) return _font;
                _font = GameAssets.Instance.font;
                if (_font == null) _font = DevOsFont();
                return _font;
            }
        }

        public static bool HasPixelFont => GameAssets.Instance.font != null;

        /// <summary>
        /// 요청 크기 → (폰트, 실제 크기). 픽셀 폰트가 있으면 설계 크기로 스냅해서 글자가 정확히 픽셀에 맞게 한다.
        /// </summary>
        public static TMP_FontAsset FontFor(float size, out float snapped)
        {
            var a = GameAssets.Instance;
            if (a.font == null) { snapped = size; return Font; }
            TMP_FontAsset f;
            if (size <= 8.5f && a.fontTiny != null) { f = a.fontTiny; snapped = 8f; }
            else if (size <= 10.5f && a.fontSmall != null) { f = a.fontSmall; snapped = 10f; }
            else { f = a.font; snapped = 12f; }
            EnsurePointFilter(f);
            return f;
        }

        /// <summary>동적 아틀라스가 새로 생기거나 커질 때 필터가 바뀌지 않게 매번 확인(가벼움).</summary>
        static void EnsurePointFilter(TMP_FontAsset f)
        {
            if (f == null || f.atlasTextures == null) return;
            foreach (var t in f.atlasTextures)
                if (t != null && t.filterMode != FilterMode.Point) t.filterMode = FilterMode.Point;
        }

        /// <summary>월드 위에 뜨는 글자용 1px 그림자(비트맵 폰트는 TMP 외곽선을 못 쓴다).</summary>
        public static TextMeshProUGUI WithShadow(this TextMeshProUGUI t)
        {
            if (t.GetComponent<TextShadow>() == null) t.gameObject.AddComponent<TextShadow>();
            return t;
        }

        /// <summary>
        /// 개발 중 임시: 프로젝트에 한글 폰트가 없으면 OS 폰트(맑은 고딕)를 런타임에 읽는다. 빌드에 포함되지 않는다.
        /// 정식 폰트(Galmuri 등)가 GameAssets.font에 연결되면 쓰이지 않는다.
        /// </summary>
        static TMP_FontAsset DevOsFont()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "malgun.ttf"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "gulim.ttc"),
            };
            foreach (var path in candidates)
            {
                try
                {
                    if (!File.Exists(path)) continue;
                    var fa = TMP_FontAsset.CreateFontAsset(path, 0, 32, 4, GlyphRenderMode.SDFAA, 1024, 1024);
                    if (fa != null)
                    {
                        fa.name = "DevOsFont";
                        return fa;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Ui] OS 폰트 로드 실패 {path}: {e.Message}");
                }
            }
            Debug.LogWarning("[Ui] 한글 폰트가 없습니다. GameAssets.font를 연결하세요.");
            return TMP_Settings.defaultFontAsset;
        }

        // ───────────────────────── 기본 ─────────────────────────

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            return (RectTransform)go.transform;
        }

        /// <summary>부모 왼쪽 위 기준 (x, y) 위치에 w×h 크기로 배치.</summary>
        public static RectTransform Place(this RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color? color = null, bool sliced = false, bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color ?? Color.white;
            img.raycastTarget = raycast;
            if (sliced && sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            img.preserveAspect = !sliced;
            return img;
        }

        public static Image Panel(Transform parent, string name, bool dark = false)
        {
            var a = GameAssets.Instance;
            var sprite = dark ? (a.UiSprite("ui_panel_dark") ?? Placeholder.PanelDark()) : (a.UiSprite("ui_panel") ?? Placeholder.Panel());
            var img = Image(parent, name, sprite, null, sliced: true, raycast: true);
            img.preserveAspect = false;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size = FontSize,
                                           Color32? color = null, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = FontFor(size, out float snapped);
            t.fontSize = snapped;
            t.color = color ?? Palette.Ash5;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.richText = true;
            t.text = text;
            t.margin = Vector4.zero;
            return t;
        }

        public static Button Button(Transform parent, string name, string label, UnityAction onClick, Sprite icon = null, float fontSize = SmallFont)
        {
            var a = GameAssets.Instance;
            var normal = a.UiSprite("ui_button", "ui_button/normal", "ui_button_normal", "ui_button") ?? Placeholder.Button();
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = normal;
            if (normal.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            img.raycastTarget = true;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var hover = a.UiSprite("ui_button", "ui_button/hover", "ui_button_hover");
            var pressed = a.UiSprite("ui_button", "ui_button/pressed", "ui_button_pressed");
            if (hover != null || pressed != null)
            {
                btn.transition = Selectable.Transition.SpriteSwap;
                btn.spriteState = new SpriteState { highlightedSprite = hover, pressedSprite = pressed, selectedSprite = normal };
            }
            else
            {
                var cb = btn.colors;
                cb.highlightedColor = new Color(1.15f, 1.1f, 1f);
                cb.pressedColor = new Color(0.75f, 0.7f, 0.65f);
                cb.selectedColor = Color.white;
                cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
                btn.colors = cb;
            }
            var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
            if (onClick != null) btn.onClick.AddListener(onClick);

            if (icon != null)
            {
                var ic = Image(rt, "Icon", icon);
                ic.rectTransform.Stretch(2f);
            }
            if (!string.IsNullOrEmpty(label))
            {
                var t = Text(rt, "Label", label, fontSize, Palette.Ash5, TextAlignmentOptions.Center);
                t.rectTransform.Stretch();
            }
            return btn;
        }

        public static void SetLabel(this Button b, string label)
        {
            var t = b.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.text = label;
        }

        // ───────────────────────── 입력 ─────────────────────────

        public static TMP_InputField NumberInput(Transform parent, string name, UnityAction<string> onEndEdit)
        {
            var rt = Rect(name, parent);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = GameAssets.Instance.UiSprite("ui_slot", "ui_slot/empty", "ui_slot") ?? Placeholder.Slot();
            if (bg.sprite.border != Vector4.zero) bg.type = UnityEngine.UI.Image.Type.Sliced;
            bg.raycastTarget = true;

            var area = Rect("Text Area", rt).Stretch(2f);
            area.gameObject.AddComponent<RectMask2D>();
            var text = Text(area, "Text", "", FontSize, Palette.Lamp1, TextAlignmentOptions.MidlineRight);
            text.rectTransform.Stretch();
            text.textWrappingMode = TextWrappingModes.NoWrap;

            var field = rt.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = text;
            field.targetGraphic = bg;
            field.fontAsset = FontFor(FontSize, out float inputSize);
            field.pointSize = inputSize;
            field.contentType = TMP_InputField.ContentType.IntegerNumber;
            field.characterLimit = 15;
            field.caretWidth = 1;
            field.customCaretColor = true;
            field.caretColor = Palette.Lamp1;
            field.selectionColor = new Color32(Palette.Violet.r, Palette.Violet.g, Palette.Violet.b, 160);
            var nav = field.navigation; nav.mode = Navigation.Mode.None; field.navigation = nav;
            if (onEndEdit != null) field.onEndEdit.AddListener(onEndEdit);
            return field;
        }

        public static Slider Slider(Transform parent, string name, float min, float max, UnityAction<float> onChange)
        {
            var rt = Rect(name, parent);
            var slider = rt.gameObject.AddComponent<Slider>();

            var bg = Image(rt, "Background", GameAssets.Instance.UiSprite("ui_slot", "ui_slot/empty", "ui_slot") ?? Placeholder.Slot(), null, sliced: true, raycast: true);
            bg.preserveAspect = false;
            var bgRt = bg.rectTransform;
            bgRt.anchorMin = new Vector2(0f, 0.25f); bgRt.anchorMax = new Vector2(1f, 0.75f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var fillArea = Rect("Fill Area", rt);
            fillArea.anchorMin = new Vector2(0f, 0.25f); fillArea.anchorMax = new Vector2(1f, 0.75f);
            fillArea.offsetMin = new Vector2(1f, 1f); fillArea.offsetMax = new Vector2(-1f, -1f);
            var fill = Image(fillArea, "Fill", Placeholder.White(), (Color)Palette.Rust3);
            fill.preserveAspect = false;
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

            var handleArea = Rect("Handle Slide Area", rt).Stretch();
            handleArea.offsetMin = new Vector2(2f, 0f); handleArea.offsetMax = new Vector2(-2f, 0f);
            var handle = Image(handleArea, "Handle", Placeholder.White(), (Color)Palette.Lamp1, raycast: true);
            handle.preserveAspect = false;
            handle.rectTransform.sizeDelta = new Vector2(3f, 0f);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f); handle.rectTransform.anchorMax = new Vector2(0f, 1f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            var nav = slider.navigation; nav.mode = Navigation.Mode.None; slider.navigation = nav;
            if (onChange != null) slider.onValueChanged.AddListener(onChange);
            return slider;
        }

        public static Toggle Toggle(Transform parent, string name, string label, bool value, UnityAction<bool> onChange)
        {
            var rt = Rect(name, parent);
            var toggle = rt.gameObject.AddComponent<Toggle>();
            var box = Image(rt, "Box", GameAssets.Instance.UiSprite("ui_slot", "ui_slot/empty", "ui_slot") ?? Placeholder.Slot(), null, sliced: true, raycast: true);
            box.preserveAspect = false;
            box.rectTransform.Place(0, 1, 10, 10);
            var check = Image(box.rectTransform, "Check", Placeholder.White(), (Color)Palette.Cyan);
            check.preserveAspect = false;
            check.rectTransform.Stretch(2f);
            var t = Text(rt, "Label", label, SmallFont);
            t.rectTransform.Place(14, 0, 200, 12);
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = value;
            var nav = toggle.navigation; nav.mode = Navigation.Mode.None; toggle.navigation = nav;
            if (onChange != null) toggle.onValueChanged.AddListener(onChange);
            return toggle;
        }

        public static string Nut(double v) => $"{BigNum.Price(v)}<size=80%> 너트</size>";

        public static string Colorize(string s, Color32 c) => $"<color={Palette.Hex(c)}>{s}</color>";
    }
}
