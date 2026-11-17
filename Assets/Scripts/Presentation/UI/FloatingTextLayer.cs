using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>월드 위치에서 떠오르는 숫자(데미지, 판매 금액). 오브젝트 풀 사용.</summary>
    public class FloatingTextLayer : MonoBehaviour
    {
        class Item { public TextMeshProUGUI Text; public float Life; public Vector2 Pos; }

        readonly List<Item> _active = new List<Item>();
        readonly Stack<TextMeshProUGUI> _pool = new Stack<TextMeshProUGUI>();
        RectTransform _rt;
        Camera _cam;
        const float Life = 0.8f;
        const int MaxActive = 24;

        public static FloatingTextLayer Create(RectTransform stripRect, Camera cam)
        {
            var rt = Ui.Rect("FloatingText", stripRect).Stretch();
            var l = rt.gameObject.AddComponent<FloatingTextLayer>();
            l._rt = rt;
            l._cam = cam;
            return l;
        }

        public void Spawn(Vector3 world, string text, Color32 color)
        {
            if (_active.Count >= MaxActive) Recycle(0);
            var t = _pool.Count > 0 ? _pool.Pop() : Ui.Text(_rt, "Float", "", Ui.SmallFont, color, TextAlignmentOptions.Midline);
            t.gameObject.SetActive(true);
            t.text = text;
            t.color = color;
            t.WithShadow();
            // 월드 → 화면 → 띠 사각형 로컬(왼쪽 위 기준)
            Vector2 screen = _cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, screen, null, out var local);
            var pos = new Vector2(local.x + _rt.rect.width * _rt.pivot.x, -(_rt.rect.height * (1f - _rt.pivot.y) - local.y));
            pos.x += Random.Range(-4f, 4f);
            t.rectTransform.Place(pos.x - 40f, -pos.y - 5f, 80f, 10f);
            _active.Add(new Item { Text = t, Life = Life, Pos = new Vector2(pos.x - 40f, -pos.y - 5f) });
        }

        void Recycle(int i)
        {
            var it = _active[i];
            _active.RemoveAt(i);
            it.Text.gameObject.SetActive(false);
            _pool.Push(it.Text);
        }

        void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var it = _active[i];
                it.Life -= Time.deltaTime;
                if (it.Life <= 0f) { Recycle(i); continue; }
                float k = 1f - it.Life / Life;
                float rise = Mathf.Round(k * 14f);
                it.Text.rectTransform.anchoredPosition = new Vector2(it.Pos.x, -(it.Pos.y - rise));
                var c = it.Text.color; c.a = it.Life < 0.25f ? it.Life / 0.25f : 1f; it.Text.color = c;
            }
        }
    }
}
