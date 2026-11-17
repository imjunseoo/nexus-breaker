using TMPro;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>
    /// 고정 크기 칸(버튼·탭·목록 행)에 든 글자가 칸을 넘치지 않게, 픽셀 폰트 단계(12 → 10 → 8px) 중
    /// 들어가는 가장 큰 크기를 고른다. 글자나 칸 크기가 바뀔 때만 다시 계산한다.
    /// 픽셀 폰트는 설계 크기로만 찍어야 또렷해서, 연속적인 auto-size 대신 단계로만 줄인다.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextFit : MonoBehaviour
    {
        public float maxSize = 12f;

        static readonly float[] Tiers = { 12f, 10f, 8f };

        TextMeshProUGUI _t;
        string _lastText;
        Vector2 _lastSize;

        void Awake() => _t = GetComponent<TextMeshProUGUI>();

        void OnEnable() => _lastText = null; // 다시 보일 때 재계산

        void LateUpdate()
        {
            var size = _t.rectTransform.rect.size;
            if (_t.text == _lastText && size == _lastSize) return;
            _lastText = _t.text;
            _lastSize = size;
            Fit(size);
        }

        void Fit(Vector2 size)
        {
            if (!Ui.HasPixelFont || size.x <= 0f || size.y <= 0f) return;
            bool wrap = _t.textWrappingMode != TextWrappingModes.NoWrap;
            bool singleLine = !wrap && (_t.text == null || _t.text.IndexOf('\n') < 0);

            foreach (float tier in Tiers)
            {
                if (tier > maxSize + 0.01f) continue;
                _t.font = Ui.FontFor(tier, out float snapped);
                _t.fontSize = snapped;
                Vector2 pref = _t.GetPreferredValues(_t.text, wrap ? size.x : 0f, 0f);
                // 한 줄이면 줄 높이 대신 글자 높이(≈ 폰트 크기)로 판단 — 픽셀 폰트는 줄 간격이 넉넉해서
                float needH = singleLine ? snapped : pref.y;
                if (pref.x <= size.x + 0.5f && needH <= size.y + 0.5f) return;
            }
        }
    }
}
