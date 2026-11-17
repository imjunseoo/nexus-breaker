using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>
    /// 게임 화면 밖(띠 오른쪽) 말풍선 패널에 뜨는 손님 방명록 / 알림 채팅.
    /// 인터넷 방송 채팅처럼 새 메시지가 아래에서 밀고 올라오고, 최근 5개만 보인다(넘치면 오래된 것부터 잘림).
    /// </summary>
    public class ChatLog : MonoBehaviour
    {
        class Line
        {
            public TextMeshProUGUI Text;
            public float Height;
        }

        public const int MaxLines = 5;
        // ui_chat 말풍선: 헤더 띠 y1~6 + 구분선 y7, 글자는 y≥8·x≥8(왼쪽 3px는 꼬리용 투명 여백) — ART_LOG
        const float PadLeft = 12f, PadRight = 9f, HeaderY = 8f, PadTop = 19f, PadBottom = 7f, Gap = 2f;
        const float SlideSpeed = 80f; // 새 줄이 밀고 올라오는 속도(px/초)

        readonly List<Line> _lines = new List<Line>(); // 0 = 가장 오래된 것
        RectTransform _panel, _content;
        float _slide; // 새 줄이 올라오는 애니메이션(남은 높이)

        public static ChatLog Create(RectTransform panelRect)
        {
            var a = GameAssets.Instance;
            var bg = Ui.Image(panelRect, "ChatBg", a.UiSprite("ui_chat") ?? a.UiSprite("ui_panel_dark") ?? Placeholder.PanelDark(), null, sliced: true);
            bg.preserveAspect = false;
            bg.rectTransform.Stretch();

            var log = panelRect.gameObject.AddComponent<ChatLog>();
            log._panel = panelRect;

            var header = Ui.Text(panelRect, "Header", "손님 방명록", 8f, Palette.Lamp0, TextAlignmentOptions.MidlineLeft).WithShadow();
            header.rectTransform.Place(PadLeft, HeaderY, 110f, 9f);

            log._content = Ui.Rect("Lines", panelRect);
            log._content.anchorMin = Vector2.zero;
            log._content.anchorMax = Vector2.one;
            log._content.offsetMin = new Vector2(PadLeft, PadBottom);
            log._content.offsetMax = new Vector2(-PadRight, -PadTop);
            log._content.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            return log;
        }

        // ───────────────────────── 메시지 ─────────────────────────

        static Color32 CustomerColor(string id) => id switch
        {
            "merchant" => Palette.Dust2,
            "raider" => Palette.Red3,
            "soldier" => Palette.Moss2,
            "cultist" => Palette.Magenta,
            "collector" => Palette.Lamp0,
            "alien" => Palette.Cyan,
            _ => Palette.Ash4,
        };

        /// <summary>손님 한마디: "이름: 메시지" (이름은 손님 타입 색).</summary>
        public void Customer(CustomerDef c, string message)
        {
            Push($"{Ui.Colorize(c.Name, CustomerColor(c.Id))}<color={Palette.Hex(Palette.Ash2)}>:</color> {message}");
        }

        /// <summary>시스템 알림(지층 도달, 창고 가득 등).</summary>
        public void System(string message, Color32? color = null)
        {
            Push(Ui.Colorize("· " + message, color ?? Palette.Ash3));
        }

        readonly Queue<string> _pending = new Queue<string>();

        void OnEnable()
        {
            // 꺼져 있는 동안 온 메시지(최근 5개)를 다시 켤 때 보여준다
            while (_pending.Count > 0) Push(_pending.Dequeue());
        }

        public void Push(string rich)
        {
            if (!isActiveAndEnabled)
            {
                _pending.Enqueue(rich);
                while (_pending.Count > MaxLines) _pending.Dequeue();
                return;
            }
            var t = Ui.Text(_content, "Msg", rich, 8f, Palette.Ash5, TextAlignmentOptions.BottomLeft);
            t.textWrappingMode = TextWrappingModes.Normal;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            float w = _content.rect.width;
            float h = Mathf.Ceil(t.GetPreferredValues(rich, w, 0f).y);
            rt.sizeDelta = new Vector2(w, h);

            _lines.Add(new Line { Text = t, Height = h });
            while (_lines.Count > MaxLines) RemoveOldest();
            _slide = h + Gap;
            Relayout();
        }

        void RemoveOldest()
        {
            Destroy(_lines[0].Text.gameObject);
            _lines.RemoveAt(0);
        }

        void Update()
        {
            if (_slide <= 0f) return;
            _slide = Mathf.MoveTowards(_slide, 0f, Time.unscaledDeltaTime * SlideSpeed);
            Relayout();
        }

        /// <summary>최신 줄을 맨 아래에 두고 위로 쌓는다. 패널 높이를 넘는 오래된 줄은 지운다.</summary>
        void Relayout()
        {
            float avail = _content.rect.height;
            float y = -Mathf.Round(_slide);
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                var ln = _lines[i];
                ln.Text.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Round(y));
                y += ln.Height + Gap;
            }
            // 위로 넘친 오래된 줄 제거(최신 줄은 항상 남김)
            float total = 0f;
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                total += _lines[i].Height + Gap;
                if (total - Gap > avail + 0.5f && i < _lines.Count - 1)
                {
                    for (int k = 0; k <= i; k++) Destroy(_lines[k].Text.gameObject);
                    _lines.RemoveRange(0, i + 1);
                    break;
                }
            }
        }
    }
}
