using TMPro;
using UnityEngine;

namespace NexusBreaker
{
    /// <summary>설정: 창 옵션(투명·항상 위·클릭 통과·크기), 볼륨, 통계, 초기화·종료.</summary>
    public class SettingsPage : MonoBehaviour
    {
        GameModel _model;
        UiRoot _root;
        TextMeshProUGUI _stats;
        float _poll;

        public static SettingsPage Create(RectTransform content, GameModel model, UiRoot root)
        {
            var rt = Ui.Rect("SettingsPage", content).Stretch();
            var p = rt.gameObject.AddComponent<SettingsPage>();
            p.Build(rt, model, root);
            return p;
        }

        void Build(RectTransform rt, GameModel model, UiRoot root)
        {
            _model = model;
            _root = root;
            var s = model.Data.settings;

            // ── 창 ──
            var head1 = Ui.Text(rt, "WinHead", "창", Ui.SmallFont, Palette.Lamp0);
            head1.rectTransform.Place(6, 0, 100, 10);
            Ui.Toggle(rt, "Transparent", "투명 배경", s.transparentBackground, v => { s.transparentBackground = v; root.ApplyWindowSettings(); })
              .GetComponent<RectTransform>().Place(6, 12, 110, 12);
            Ui.Toggle(rt, "Topmost", "항상 위", s.alwaysOnTop, v => { s.alwaysOnTop = v; root.ApplyWindowSettings(); })
              .GetComponent<RectTransform>().Place(6, 25, 110, 12);
            Ui.Toggle(rt, "ClickThrough", "빈 곳 클릭 통과", s.clickThrough, v => { s.clickThrough = v; root.ApplyWindowSettings(); })
              .GetComponent<RectTransform>().Place(6, 38, 110, 12);

            var scaleLabel = Ui.Text(rt, "ScaleLabel", "창 크기", Ui.SmallFont, Palette.Ash4);
            scaleLabel.rectTransform.Place(6, 52, 40, 12);
            for (int i = 1; i <= 4; i++)
            {
                int scale = i + 1; // 2x ~ 5x
                var b = Ui.Button(rt, "Scale" + scale, $"{scale}x", () => { s.windowScale = scale; root.ApplyWindowSettings(); });
                b.GetComponent<RectTransform>().Place(44 + (i - 1) * 19, 52, 18, 12);
            }

            // ── 소리 ──
            var head2 = Ui.Text(rt, "SoundHead", "소리", Ui.SmallFont, Palette.Lamp0);
            head2.rectTransform.Place(130, 0, 100, 10);
            MakeVolume(rt, "배경음", 12, s.bgmVolume, v => { s.bgmVolume = v; AudioManager.Instance?.ApplyVolumes(s); });
            MakeVolume(rt, "효과음", 25, s.sfxVolume, v => { s.sfxVolume = v; AudioManager.Instance?.ApplyVolumes(s); });
            MakeVolume(rt, "환경음", 38, s.ambientVolume, v => { s.ambientVolume = v; AudioManager.Instance?.ApplyVolumes(s); });

            // ── 통계 ──
            _stats = Ui.Text(rt, "Stats", "", 8f, Palette.Ash3, TextAlignmentOptions.TopLeft);
            _stats.textWrappingMode = TextWrappingModes.Normal;
            _stats.rectTransform.Place(130, 54, 150, 50);

            // ── 기타 ──
            var reset = Ui.Button(rt, "Reset", "처음부터", () =>
                root.Modal.Show("세이브 초기화", "모든 진행 상황(코어·도감 포함)이 지워집니다.\n되돌릴 수 없어요.", "초기화",
                    () => GameRoot.Instance.ResetSave(), "취소"));
            reset.GetComponent<RectTransform>().Place(300, 70, 78, 14);
            var quit = Ui.Button(rt, "Quit", "저장하고 종료", () => GameRoot.Instance.SaveAndQuit());
            quit.GetComponent<RectTransform>().Place(300, 88, 78, 14);
            var hint = Ui.Text(rt, "Hint", "오른쪽 위 핀 버튼을 끌면 창이 움직여요", 8f, Palette.Ash2);
            hint.rectTransform.Place(6, 68, 130, 10);
            var version = Ui.Text(rt, "Version", $"버전 {Application.version}", 8f, Palette.Ash3);
            version.rectTransform.Place(6, 82, 120, 10);
        }

        void MakeVolume(RectTransform rt, string label, float y, float value, UnityEngine.Events.UnityAction<float> onChange)
        {
            var t = Ui.Text(rt, "Vol_" + label, label, Ui.SmallFont, Palette.Ash4);
            t.rectTransform.Place(130, y, 40, 12);
            var sl = Ui.Slider(rt, "Slider_" + label, 0f, 1f, onChange);
            sl.SetValueWithoutNotify(value);
            sl.GetComponent<RectTransform>().Place(172, y + 2, 110, 8);
        }

        void Update()
        {
            _poll -= Time.unscaledDeltaTime;
            if (_poll > 0f) return;
            _poll = 0.5f;
            var d = _model.Data;
            _stats.text =
                $"코어 {d.cores} (데미지 ×{_model.CoreMult:0.0}) · 지역 {d.region + 1}\n" +
                $"총 타격 {BigNum.Format(d.lifetime.totalHits)} · 판매 {BigNum.Format(d.lifetime.itemsSold)}개\n" +
                $"번 너트 {BigNum.Format(d.lifetime.nutsEarned)} · 도감 {d.codex.Count}/{ItemCatalog.All.Count}\n" +
                $"최고 판매 {(string.IsNullOrEmpty(d.lifetime.bestSaleItem) ? "-" : ItemCatalog.Get(d.lifetime.bestSaleItem)?.Name + " " + BigNum.Price(d.lifetime.bestSale))}";
        }
    }
}
