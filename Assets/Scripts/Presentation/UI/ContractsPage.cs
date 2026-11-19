using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NexusBreaker
{
    /// <summary>의뢰 페이지(v0.2): 의뢰 카드 3장. 진행도·보상·보상 받기·바꾸기(5분에 한 번 무료).</summary>
    public class ContractsPage : MonoBehaviour
    {
        class Card
        {
            public RectTransform Root;
            public Image Icon;
            public TextMeshProUGUI Requester, Title, Quote, Progress, Reward, Empty;
            public RectTransform BarFill;
            public Button Claim, Reroll;
            public TextMeshProUGUI ClaimLabel, RerollLabel;
            public GameObject[] Body;
        }

        const float CardW = 122f, CardH = 104f, BarW = 108f;
        static readonly Color Disabled = new Color(0.62f, 0.58f, 0.55f);

        GameModel _model;
        readonly List<Card> _cards = new List<Card>();
        bool _dirty = true;
        float _poll;

        public static ContractsPage Create(RectTransform content, GameModel model)
        {
            var rt = Ui.Rect("ContractsPage", content).Stretch();
            var p = rt.gameObject.AddComponent<ContractsPage>();
            p.Build(rt, model);
            return p;
        }

        void Build(RectTransform rt, GameModel model)
        {
            _model = model;
            var a = GameAssets.Instance;
            for (int i = 0; i < Stats.ContractSlots; i++)
            {
                int slot = i;
                var c = new Card();
                var bg = Ui.Panel(rt, "Card" + i, dark: true);
                c.Root = bg.rectTransform;
                c.Root.Place(4 + i * (CardW + 4), 1, CardW, CardH);

                c.Icon = Ui.Image(c.Root, "Icon", null);
                c.Icon.rectTransform.Place(6, 5, 16, 16);

                c.Requester = Ui.Text(c.Root, "Requester", "", 8f, Palette.Ash3, TextAlignmentOptions.MidlineLeft);
                c.Requester.rectTransform.Place(25, 8, CardW - 31, 10);

                c.Title = Ui.Text(c.Root, "Title", "", Ui.SmallFont, Palette.Ash5, TextAlignmentOptions.TopLeft);
                c.Title.textWrappingMode = TextWrappingModes.Normal;
                c.Title.rectTransform.Place(7, 23, CardW - 14, 25);

                c.Quote = Ui.Text(c.Root, "Quote", "", 8f, Palette.Ash2, TextAlignmentOptions.MidlineLeft);
                c.Quote.rectTransform.Place(7, 49, CardW - 14, 9);

                var frame = Ui.Image(c.Root, "BarFrame", a.UiSprite("ui_bar", "ui_bar/frame", "ui_bar_frame", "ui_bar") ?? Placeholder.Slot(), null, sliced: true);
                frame.preserveAspect = false;
                frame.rectTransform.Place(7, 60, BarW + 4, 8);
                var fillSprite = a.Sprite("ui_bar/fill", "ui_bar_fill");
                var fill = Ui.Image(frame.rectTransform, "Fill", fillSprite ?? Placeholder.White(), fillSprite != null ? Color.white : (Color)Palette.Cyan);
                fill.preserveAspect = false;
                c.BarFill = fill.rectTransform;
                c.BarFill.Place(2, 2, 0, 4);

                c.Progress = Ui.Text(c.Root, "Progress", "", 8f, Palette.Ash4, TextAlignmentOptions.MidlineLeft);
                c.Progress.rectTransform.Place(7, 69, CardW - 14, 9);

                c.Reward = Ui.Text(c.Root, "Reward", "", 8f, Palette.Lamp1, TextAlignmentOptions.MidlineLeft);
                c.Reward.rectTransform.Place(7, 78, CardW - 14, 9);

                c.Claim = Ui.Button(c.Root, "Claim", "-", () => Claim(slot), null, Ui.SmallFont);
                c.Claim.GetComponent<RectTransform>().Place(6, 89, 72, 13);
                c.ClaimLabel = c.Claim.GetComponentInChildren<TextMeshProUGUI>();

                c.Reroll = Ui.Button(c.Root, "Reroll", "-", () => Reroll(slot), null, 8f);
                c.Reroll.GetComponent<RectTransform>().Place(80, 89, 36, 13);
                c.RerollLabel = c.Reroll.GetComponentInChildren<TextMeshProUGUI>();

                c.Empty = Ui.Text(c.Root, "Empty", "", Ui.SmallFont, Palette.Ash3, TextAlignmentOptions.Center);
                c.Empty.textWrappingMode = TextWrappingModes.Normal;
                c.Empty.rectTransform.Place(8, 30, CardW - 16, 40);

                c.Body = new[] { c.Icon.gameObject, c.Requester.gameObject, c.Title.gameObject, c.Quote.gameObject, frame.gameObject,
                                 c.Progress.gameObject, c.Reward.gameObject, c.Claim.gameObject, c.Reroll.gameObject };
                _cards.Add(c);
                UiTargets.Register("contract_claim_" + i, c.Claim);
            }
            model.ContractsChanged += () => _dirty = true;
            model.EconomyChanged += () => _dirty = true;
        }

        void OnEnable() => _dirty = true;

        void Claim(int slot)
        {
            if (!_model.ClaimContract(slot)) AudioManager.Instance?.Sfx("sfx_error", 0.6f);
        }

        void Reroll(int slot)
        {
            if (_model.RerollContract(slot)) AudioManager.Instance?.Sfx("sfx_ui_click", 0.7f);
            else AudioManager.Instance?.Sfx("sfx_error", 0.6f);
        }

        void Update()
        {
            // 남은 시간 표시 때문에 1초마다는 갱신
            _poll -= Time.unscaledDeltaTime;
            if (!_dirty && _poll > 0f) return;
            _dirty = false;
            _poll = 0.5f;

            for (int i = 0; i < _cards.Count; i++) Refresh(_cards[i], i < _model.Contracts.Count ? _model.Contracts[i] : null);
        }

        static string Mmss(double seconds)
        {
            int t = Mathf.CeilToInt((float)seconds);
            return $"{t / 60}:{t % 60:00}";
        }

        void Refresh(Card card, ContractState c)
        {
            bool show = _model.ContractsUnlocked && c != null && !c.IsEmpty;
            foreach (var go in card.Body) go.SetActive(show);
            card.Empty.gameObject.SetActive(!show);
            if (!show)
            {
                card.Empty.text = !_model.ContractsUnlocked
                    ? "튜토리얼을 마치면\n의뢰가 들어온다."
                    : $"새 의뢰를 기다리는 중…\n{Ui.Colorize(BigNum.Duration(System.Math.Max(0d, (c?.readyAt ?? 0d) - _model.Data.playTime)), Palette.Ash4)}";
                return;
            }

            var icon = GameAssets.Instance.UiIcon(ContractText.Icon(c)) ?? GameAssets.Instance.UiIcon("contract");
            card.Icon.sprite = icon ?? Placeholder.Get(Placeholder.Shape.Circle, 10, 10, Palette.Lamp0, Palette.Outline, false);
            card.Requester.text = $"{ContractText.RequesterName(c)}의 의뢰";
            card.Title.text = ContractText.Title(c);
            card.Quote.text = ContractText.Quote(c);

            float frac = c.goal > 0 ? Mathf.Clamp01((float)(c.progress / c.goal)) : 0f;
            card.BarFill.sizeDelta = new Vector2(Mathf.Round(BarW * frac), 4f);
            bool money = ContractText.TryKind(c, out var kind) && kind == ContractKind.EarnNuts;
            string prog = money ? $"{BigNum.Price(c.progress)} / {BigNum.Price(c.goal)}" : $"{(long)c.progress} / {(long)c.goal}";
            card.Progress.text = c.IsComplete ? Ui.Colorize("달성!", Palette.Lamp0) : prog;
            card.Reward.text = $"보상 {ContractText.Reward(c)}";

            card.Claim.interactable = c.IsComplete;
            // 누를 수 없는 버튼은 어둡게(스프라이트 교체 방식이라 기본 비활성 색이 안 먹음)
            card.Claim.image.color = c.IsComplete ? Color.white : Disabled;
            card.ClaimLabel.text = c.IsComplete ? "보상 받기" : "진행 중";

            bool canReroll = !c.IsComplete && _model.CanRerollContract;
            card.Reroll.gameObject.SetActive(!c.IsComplete);
            card.Reroll.interactable = canReroll;
            card.Reroll.image.color = canReroll ? Color.white : Disabled;
            card.RerollLabel.text = canReroll ? "바꾸기" : Mmss(_model.RerollCooldownLeft);
        }
    }
}
