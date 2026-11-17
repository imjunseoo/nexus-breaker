using TMPro;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>
    /// TMP 텍스트 뒤에 1px(오른쪽 아래) 검은 그림자 복제본을 둔다.
    /// 비트맵(픽셀) 폰트는 TMP 외곽선/언더레이를 지원하지 않아서 이 방식으로 가독성을 확보한다.
    /// 그림자는 형제 오브젝트(바로 앞 순서)라서 본문보다 먼저 그려진다.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextShadow : MonoBehaviour
    {
        public Vector2 offset = new Vector2(1f, -1f);
        public Color32 color = Palette.Black;

        TextMeshProUGUI _src, _shadow;
        RectTransform _srcRt, _shRt;
        string _lastText;

        void Awake()
        {
            _src = GetComponent<TextMeshProUGUI>();
            _srcRt = (RectTransform)transform;
            var go = new GameObject(name + "_Shadow", typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform.parent, false);
            go.transform.SetSiblingIndex(transform.GetSiblingIndex());
            _shRt = (RectTransform)go.transform;
            _shadow = go.AddComponent<TextMeshProUGUI>();
            _shadow.raycastTarget = false;
            Sync(force: true);
        }

        void OnEnable() { if (_shadow != null) _shadow.gameObject.SetActive(true); }
        void OnDisable() { if (_shadow != null) _shadow.gameObject.SetActive(false); }
        void OnDestroy() { if (_shadow != null) Destroy(_shadow.gameObject); }

        void LateUpdate() => Sync(force: false);

        void Sync(bool force)
        {
            if (_shadow == null) return;
            if (force || _shadow.font != _src.font) _shadow.font = _src.font;
            _shadow.fontSize = _src.fontSize;
            _shadow.alignment = _src.alignment;
            _shadow.textWrappingMode = _src.textWrappingMode;
            _shadow.overflowMode = _src.overflowMode;
            _shadow.richText = true;
            if (force || _lastText != _src.text)
            {
                _lastText = _src.text;
                // 색 태그를 지운 텍스트에 그림자 색 하나만 적용
                _shadow.text = StripColor(_src.text);
            }
            var c = color; c.a = (byte)(_src.color.a * 255f * 0.85f);
            _shadow.color = c;

            _shRt.anchorMin = _srcRt.anchorMin;
            _shRt.anchorMax = _srcRt.anchorMax;
            _shRt.pivot = _srcRt.pivot;
            _shRt.sizeDelta = _srcRt.sizeDelta;
            _shRt.anchoredPosition = _srcRt.anchoredPosition + offset;
            // 그림자가 본문 바로 앞(먼저 그려짐)에 있도록 유지
            int si = _shRt.GetSiblingIndex(), ti = transform.GetSiblingIndex();
            if (si > ti) _shRt.SetSiblingIndex(ti);
            else if (si < ti - 1) _shRt.SetSiblingIndex(ti - 1);
        }

        static string StripColor(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('<') < 0) return s;
            var sb = new System.Text.StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] == '<')
                {
                    int end = s.IndexOf('>', i);
                    if (end > i)
                    {
                        string tag = s.Substring(i, end - i + 1);
                        if (!tag.StartsWith("<color") && !tag.StartsWith("</color")) sb.Append(tag);
                        i = end + 1;
                        continue;
                    }
                }
                sb.Append(s[i]);
                i++;
            }
            return sb.ToString();
        }
    }
}
