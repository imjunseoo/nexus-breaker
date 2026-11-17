using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>띠 화면 위쪽 가운데에 잠깐 떴다 사라지는 알림/판매 로그.</summary>
    public class ToastFeed : MonoBehaviour
    {
        class Entry { public TextMeshProUGUI Text; public float Life; }

        readonly List<Entry> _entries = new List<Entry>();
        RectTransform _rt;
        const int MaxLines = 3;
        const float LineH = 12f; // Galmuri9(10px)의 줄 높이
        const float Life = 3.2f;

        public static ToastFeed Create(RectTransform parent, float x, float y, float w)
        {
            var rt = Ui.Rect("Toasts", parent).Place(x, y, w, LineH * MaxLines);
            var f = rt.gameObject.AddComponent<ToastFeed>();
            f._rt = rt;
            return f;
        }

        public void Push(string text, Color32? color = null)
        {
            if (_entries.Count >= MaxLines)
            {
                Destroy(_entries[0].Text.gameObject);
                _entries.RemoveAt(0);
            }
            var t = Ui.Text(_rt, "Toast", text, Ui.SmallFont, color ?? Palette.Ash5, TextAlignmentOptions.Center);
            t.WithShadow();
            _entries.Add(new Entry { Text = t, Life = Life });
            Relayout();
        }

        void Relayout()
        {
            for (int i = 0; i < _entries.Count; i++)
                _entries[i].Text.rectTransform.Place(0, i * LineH, _rt.rect.width, LineH);
        }

        void Update()
        {
            bool changed = false;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                e.Life -= Time.deltaTime;
                if (e.Life <= 0f)
                {
                    Destroy(e.Text.gameObject);
                    _entries.RemoveAt(i);
                    changed = true;
                    continue;
                }
                var c = e.Text.color; c.a = Mathf.Clamp01(e.Life / 0.5f); e.Text.color = c;
            }
            if (changed) Relayout();
        }
    }
}
