using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NexusBreaker
{
    /// <summary>아이콘 칸(창고 아이템, 박스, 진열대 슬롯). 마우스를 올리면 툴팁, 클릭하면 콜백.</summary>
    public class UiCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public Image Background { get; private set; }
        public Image Icon { get; private set; }
        public TextMeshProUGUI Count { get; private set; }
        public Image Dot { get; private set; }

        public Action OnClick;
        public Func<string> TooltipText;

        Sprite _normal, _selected;
        bool _isSelected;

        public static UiCell Create(Transform parent, string name, float x, float y, float size)
        {
            var rt = Ui.Rect(name, parent).Place(x, y, size, size);
            var cell = rt.gameObject.AddComponent<UiCell>();
            var a = GameAssets.Instance;
            cell._normal = a.UiSprite("ui_slot", "ui_slot/empty", "ui_slot_empty", "ui_slot") ?? Placeholder.Slot();
            cell._selected = a.UiSprite("ui_slot", "ui_slot/selected", "ui_slot_selected") ?? Placeholder.Get(Placeholder.Shape.Panel, 12, 12, Palette.Shadow, Palette.Lamp0, false);

            cell.Background = rt.gameObject.AddComponent<Image>();
            cell.Background.sprite = cell._normal;
            if (cell._normal.border != Vector4.zero) cell.Background.type = Image.Type.Sliced;
            cell.Background.raycastTarget = true;

            cell.Icon = Ui.Image(rt, "Icon", null);
            cell.Icon.rectTransform.Stretch(2f);
            cell.Icon.enabled = false;

            cell.Count = Ui.Text(rt, "Count", "", 8f, Palette.Ash5, TextAlignmentOptions.BottomRight);
            cell.Count.rectTransform.Stretch(0f);
            cell.Count.rectTransform.offsetMax = new Vector2(0f, 0f);
            cell.Count.WithShadow();

            cell.Dot = Ui.Image(rt, "Dot", Placeholder.White());
            cell.Dot.preserveAspect = false;
            cell.Dot.rectTransform.Place(1, 1, 3, 3);
            cell.Dot.enabled = false;
            return cell;
        }

        public void SetIcon(Sprite s, Color? tint = null)
        {
            Icon.sprite = s;
            Icon.enabled = s != null;
            Icon.color = tint ?? Color.white;
        }

        public void SetCount(int n) => Count.text = n > 1 ? n.ToString() : "";
        public void SetCountText(string s) => Count.text = s;

        public void SetDot(Color32? c)
        {
            Dot.enabled = c.HasValue;
            if (c.HasValue) Dot.color = c.Value;
        }

        public void SetSelected(bool on)
        {
            if (_isSelected == on) return;
            _isSelected = on;
            Background.sprite = on ? _selected : _normal;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            var text = TooltipText?.Invoke();
            if (!string.IsNullOrEmpty(text)) Tooltip.Instance?.Show(text, (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData e) => Tooltip.Instance?.Hide();

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            OnClick?.Invoke();
            // 클릭으로 내용이 바뀌었을 수 있으니 툴팁 갱신
            var text = TooltipText?.Invoke();
            if (!string.IsNullOrEmpty(text)) Tooltip.Instance?.Show(text, (RectTransform)transform);
            else Tooltip.Instance?.Hide();
        }

        void OnDisable() { if (Tooltip.Instance != null) Tooltip.Instance.HideIfOwner((RectTransform)transform); }
    }
}
