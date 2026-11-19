using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NexusBreaker
{
    /// <summary>띠 화면(384×96) 위 HUD: 넥서스 HP, 너트, 박스·창고, 페이지 버튼, 시장 이벤트.</summary>
    public class HudView : MonoBehaviour
    {
        GameModel _model;
        UiRoot _root;
        RectTransform _hpFill;
        TextMeshProUGUI _hpText, _nuts, _boxes, _event, _contracts;
        GameObject _contractBtn;
        float _blink;
        Image _hpFillImg;
        float _barW;
        double _shownNuts;
        float _refresh;

        public static HudView Create(RectTransform strip, GameModel model, UiRoot root)
        {
            var rt = Ui.Rect("HUD", strip).Stretch();
            var h = rt.gameObject.AddComponent<HudView>();
            h.Build(rt, model, root);
            return h;
        }

        void Build(RectTransform rt, GameModel model, UiRoot root)
        {
            _model = model;
            _root = root;
            var a = GameAssets.Instance;

            // HP 바
            _barW = 176f;
            var frameSprite = a.UiSprite("ui_bar", "ui_bar/frame", "ui_bar_frame", "ui_bar");
            var frame = Ui.Image(rt, "HpFrame", frameSprite ?? Placeholder.Slot(), null, sliced: true);
            frame.preserveAspect = false;
            frame.rectTransform.Place(6, 4, _barW + 4, 8);
            _hpFillImg = Ui.Image(frame.rectTransform, "HpFill", a.Sprite("ui_bar/fill", "ui_bar_fill") ?? Placeholder.White(),
                a.Sprite("ui_bar/fill", "ui_bar_fill") != null ? Color.white : (Color)Palette.Cyan);
            _hpFillImg.preserveAspect = false;
            _hpFill = _hpFillImg.rectTransform;
            _hpFill.Place(2, 2, _barW, 4);

            _hpText = Ui.Text(rt, "HpText", "", Ui.SmallFont, Palette.Ash4);
            _hpText.WithShadow();
            _hpText.rectTransform.Place(6, 13, 200, 10);

            _event = Ui.Text(rt, "Event", "", Ui.SmallFont, Palette.Lamp0);
            _event.WithShadow();
            _event.rectTransform.Place(6, 23, 200, 10);

            // 너트
            var nutIcon = Ui.Image(rt, "NutIcon", a.Sprite("ui_nut_small") ?? a.UiIcon("nut") ?? Placeholder.Get(Placeholder.Shape.Circle, 8, 8, Palette.Ash3, Palette.Outline, false));
            nutIcon.rectTransform.Place(196, 4, 8, 8);
            _nuts = Ui.Text(rt, "Nuts", "0", Ui.FontSize, Palette.Lamp1);
            _nuts.WithShadow();
            _nuts.rectTransform.Place(206, 2, 100, 12);

            // 박스·창고 → 클릭하면 상점 페이지
            var boxBtn = Ui.Button(rt, "Boxes", "", () => _root.OpenPage(UiRoot.Page.Shop));
            boxBtn.GetComponent<Image>().color = new Color(1, 1, 1, 0f);
            boxBtn.GetComponent<RectTransform>().Place(194, 15, 110, 10);
            _boxes = Ui.Text(boxBtn.transform, "Text", "", Ui.SmallFont, Palette.Ash4);
            _boxes.WithShadow();
            _boxes.rectTransform.Stretch();

            // 의뢰(v0.2) → 클릭하면 의뢰 페이지
            var cBtn = Ui.Button(rt, "Contracts", "", () => _root.TogglePage(UiRoot.Page.Contracts));
            cBtn.GetComponent<Image>().color = new Color(1, 1, 1, 0f);
            cBtn.GetComponent<RectTransform>().Place(194, 25, 110, 10);
            _contracts = Ui.Text(cBtn.transform, "Text", "", Ui.SmallFont, Palette.Ash4);
            _contracts.WithShadow();
            _contracts.rectTransform.Stretch();
            _contractBtn = cBtn.gameObject;
            UiTargets.Register("hud_contracts", cBtn);

            // 오른쪽 위 아이콘 버튼
            UiTargets.Register("btn_shop", MakeIconButton(rt, "BtnShop", "shop", "상", 310, () => _root.TogglePage(UiRoot.Page.Shop)));
            UiTargets.Register("btn_upgrade", MakeIconButton(rt, "BtnUpgrade", "hammer", "강", 328, () => _root.TogglePage(UiRoot.Page.Upgrade)));
            MakeIconButton(rt, "BtnSettings", "settings", "설", 346, () => _root.TogglePage(UiRoot.Page.Settings));
            var grip = Ui.Button(rt, "Grip", a.UiIcon("pin") != null ? "" : "≡", null, a.UiIcon("pin"));
            grip.GetComponent<RectTransform>().Place(364, 3, 16, 16);
            grip.gameObject.AddComponent<DragHandle>();

            _shownNuts = model.Data.nuts;
            Refresh(true);
        }

        Button MakeIconButton(RectTransform parent, string name, string icon, string fallbackLabel, float x, UnityEngine.Events.UnityAction onClick)
        {
            var sprite = GameAssets.Instance.UiIcon(icon);
            var b = Ui.Button(parent, name, sprite != null ? "" : fallbackLabel, onClick, sprite);
            b.GetComponent<RectTransform>().Place(x, 3, 16, 16);
            return b;
        }

        void Update()
        {
            // 너트 숫자 부드럽게 따라가기
            double target = _model.Data.nuts;
            _shownNuts = System.Math.Abs(target - _shownNuts) < 1d ? target : _shownNuts + (target - _shownNuts) * System.Math.Min(1d, Time.deltaTime * 10d);
            _nuts.text = BigNum.Price(_shownNuts);

            _blink += Time.unscaledDeltaTime;
            _refresh -= Time.deltaTime;
            if (_refresh <= 0f) { _refresh = 0.1f; Refresh(false); }
        }

        void RefreshContracts()
        {
            bool show = _model.ContractsUnlocked && !_model.Data.nexusDestroyed;
            _contractBtn.SetActive(show);
            if (!show) return;
            int claim = _model.ClaimableContracts;
            if (claim > 0)
            {
                // 보상 대기 중: 천천히 깜빡여서 눈에 띄게
                bool on = Mathf.Repeat(_blink, 1.2f) < 0.8f;
                _contracts.text = Ui.Colorize($"의뢰 완료 {claim}건! 보상 받기", on ? Palette.Lamp1 : Palette.Lamp0);
                return;
            }
            double best = 0d;
            foreach (var c in _model.Contracts)
                if (!c.IsEmpty && c.goal > 0d) best = System.Math.Max(best, c.progress / c.goal);
            _contracts.text = $"의뢰 진행 중 · 최고 {best * 100:0}%";
        }

        void Refresh(bool force)
        {
            var d = _model.Data;
            float frac = (float)_model.HpFraction;
            _hpFill.sizeDelta = new Vector2(Mathf.Max(0f, Mathf.Round(_barW * frac)), 4f);
            int st = _model.StratumNow;
            string stratum = st == 0 ? Stats.StratumNames[st] : $"{Stats.StratumNames[st]} 경도×{1d / Stats.StratumHardness[st]:0.#}";
            _hpText.text = d.nexusDestroyed
                ? Ui.Colorize("넥서스 파괴됨!", Palette.Magenta)
                : $"{BigNum.Format(d.nexusHp)} / {BigNum.Format(d.nexusMaxHp)}  <color={Palette.Hex(Palette.Cyan)}>{stratum}</color>  {RegionCatalog.Name(d.region)}";

            int boxes = _model.BoxCount;
            string boxText = boxes > 0 ? Ui.Colorize($"박스 {boxes}", Palette.Lamp0) : "박스 0";
            string wh = _model.WarehouseFull ? Ui.Colorize($"창고 {_model.WarehouseUsed}/{_model.WarehouseCap}", Palette.Red3)
                                             : $"창고 {_model.WarehouseUsed}/{_model.WarehouseCap}";
            _boxes.text = $"{boxText} · {wh}";

            RefreshContracts();

            var ev = _model.ActiveEvent;
            _event.text = ev != null ? $"[{ev.Title}] {ev.Description} ({BigNum.Duration(d.activeEventRemaining)})" : "";
        }
    }
}
