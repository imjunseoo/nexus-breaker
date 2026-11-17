using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NexusBreaker
{
    public class Tooltip : MonoBehaviour
    {
        public static Tooltip Instance { get; private set; }

        RectTransform _rt, _frame;
        TextMeshProUGUI _text;
        RectTransform _owner;

        // 패널 테두리(ui_panel_dark 9-slice 6px)보다 안쪽에 글자가 오도록
        const float PadX = 9f, PadTop = 7f, PadBottom = 8f;
        const float Width = 150f;

        public static Tooltip Create(RectTransform frame)
        {
            var panel = Ui.Panel(frame, "Tooltip", dark: true);
            panel.raycastTarget = false;
            var t = panel.gameObject.AddComponent<Tooltip>();
            t._rt = panel.rectTransform;
            t._frame = frame;
            t._text = Ui.Text(panel.rectTransform, "Text", "", Ui.SmallFont, Palette.Ash5, TextAlignmentOptions.TopLeft);
            t._text.textWrappingMode = TextWrappingModes.Normal;
            var trt = t._text.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.pivot = new Vector2(0f, 1f);
            trt.offsetMin = new Vector2(PadX, PadBottom);
            trt.offsetMax = new Vector2(-PadX, -PadTop);
            var fitter = panel.gameObject.AddComponent<LayoutElement>();
            fitter.ignoreLayout = true;
            panel.gameObject.SetActive(false);
            Instance = t;
            return t;
        }

        public void Show(string text, RectTransform anchor)
        {
            _owner = anchor;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _text.text = text;
            // 글자 칸의 안쪽 여백(offset)은 그대로 두고, 패널 크기만 글자에 맞춘다
            const float width = Width;
            Vector2 pref = _text.GetPreferredValues(text, width - PadX * 2f, 0f);
            float h = Mathf.Ceil(pref.y) + PadTop + PadBottom;
            _rt.anchorMin = _rt.anchorMax = new Vector2(0f, 1f);
            _rt.pivot = new Vector2(0f, 1f);
            _rt.sizeDelta = new Vector2(width, h);

            // 앵커(칸)의 오른쪽 아래에 띄우고, 프레임 밖이면 반대편으로
            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners); // 0=BL,1=TL,2=TR,3=BR
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_frame, RectTransformUtility.WorldToScreenPoint(null, corners[2]), null, out local);
            // local은 프레임 피벗 기준 → 왼쪽 위 기준 좌표로 변환
            float fx = local.x + _frame.rect.width * _frame.pivot.x;
            float fy = -(local.y - _frame.rect.height * (1f - _frame.pivot.y));
            float x = fx + 2f, y = fy;
            if (x + width > _frame.rect.width) x = Mathf.Max(0f, fx - width - anchor.rect.width - 4f);
            if (y + h > _frame.rect.height) y = Mathf.Max(0f, _frame.rect.height - h);
            _rt.anchoredPosition = new Vector2(Mathf.Round(x), -Mathf.Round(y));
        }

        public void Hide()
        {
            _owner = null;
            gameObject.SetActive(false);
        }

        public void HideIfOwner(RectTransform owner)
        {
            if (_owner == owner) Hide();
        }
    }
}
