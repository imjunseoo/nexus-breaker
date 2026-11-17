using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NexusBreaker
{
    /// <summary>강화 페이지: 2열 × 6행 업그레이드 목록.</summary>
    public class UpgradePage : MonoBehaviour
    {
        class Row
        {
            public UpgradeDef Def;
            public TextMeshProUGUI Name, Effect;
            public Button Buy;
            public TextMeshProUGUI BuyLabel;
        }

        GameModel _model;
        readonly List<Row> _rows = new List<Row>();
        bool _dirty = true;
        float _poll;

        public static UpgradePage Create(RectTransform content, GameModel model)
        {
            var rt = Ui.Rect("UpgradePage", content).Stretch();
            var p = rt.gameObject.AddComponent<UpgradePage>();
            p.Build(rt, model);
            return p;
        }

        void Build(RectTransform rt, GameModel model)
        {
            _model = model;
            var all = UpgradeCatalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                var def = all[i];
                float x = 4 + (i / 6) * 190, y = 1 + (i % 6) * 17.5f; // 행 높이 17 + 간격 0.5
                var row = new Row { Def = def };

                var bg = Ui.Panel(rt, "Row_" + def.Id, dark: true);
                bg.rectTransform.Place(x, y, 186, 17);
                var icon = Ui.Image(bg.rectTransform, "Icon", GameAssets.Instance.UiIcon(def.Icon) ?? Placeholder.Get(Placeholder.Shape.Circle, 10, 10, Palette.Rust3, Palette.Outline, false));
                icon.rectTransform.Place(2, 2, 13, 13);

                // 이름(10px)과 효과(8px)를 위아래로 겹치지 않게 위쪽 정렬로 배치
                row.Name = Ui.Text(bg.rectTransform, "Name", "", Ui.SmallFont, Palette.Ash5, TextAlignmentOptions.TopLeft);
                row.Name.rectTransform.Place(17, -1, 110, 10);
                row.Effect = Ui.Text(bg.rectTransform, "Effect", "", 8f, Palette.Ash3, TextAlignmentOptions.TopLeft);
                row.Effect.rectTransform.Place(17, 8, 112, 9);

                UpgradeId id = def.Id;
                row.Buy = Ui.Button(bg.rectTransform, "Buy", "-", () => Buy(id), null, 8f); // 라벨이 있어야 Text가 생성됨
                row.Buy.GetComponent<RectTransform>().Place(130, 2, 54, 13);
                row.BuyLabel = row.Buy.GetComponentInChildren<TextMeshProUGUI>();
                _rows.Add(row);
            }
            model.EconomyChanged += () => _dirty = true;
        }

        void OnEnable() => _dirty = true;

        void Buy(UpgradeId id)
        {
            if (_model.TryBuyUpgrade(id)) AudioManager.Instance?.Sfx("sfx_upgrade");
            else AudioManager.Instance?.Sfx("sfx_error", 0.6f);
        }

        void Update()
        {
            _poll -= Time.unscaledDeltaTime;
            if (!_dirty && _poll > 0f) return;
            _dirty = false;
            _poll = 0.25f;

            foreach (var r in _rows)
            {
                int lv = _model.Level(r.Def.Id);
                bool maxed = _model.IsMaxed(r.Def.Id);
                r.Name.text = $"{r.Def.Name} <size=80%><color={Palette.Hex(Palette.Ash3)}>Lv{lv}</color></size>";
                r.Effect.text = r.Def.DescribeNext(lv);
                if (maxed)
                {
                    r.BuyLabel.text = "최대";
                    r.Buy.interactable = false;
                }
                else
                {
                    double cost = _model.UpgradeCost(r.Def.Id);
                    bool afford = _model.Data.nuts >= cost;
                    r.BuyLabel.text = Ui.Colorize(BigNum.Price(cost), afford ? Palette.Lamp1 : Palette.Red3);
                    r.Buy.interactable = afford;
                }
            }
        }
    }
}
