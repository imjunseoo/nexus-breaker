using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NexusBreaker
{
    /// <summary>
    /// 상점 페이지: 왼쪽 창고(박스·아이템) / 가운데 진열대 / 오른쪽 가격 편집기.
    /// 창고 아이템을 고르면 가격을 정해 진열, 진열대 칸을 고르면 가격을 바로 바꾸거나 내린다.
    /// </summary>
    public class ShopPage : MonoBehaviour
    {
        enum Sel { None, Warehouse, Shelf }

        GameModel _model;
        RectTransform _rt;

        // 창고
        TextMeshProUGUI _whHeader;
        readonly UiCell[] _boxCells = new UiCell[GradeInfo.Count];
        RectTransform _itemContent;
        readonly List<UiCell> _itemCells = new List<UiCell>();
        readonly List<string> _itemIds = new List<string>();

        // 진열대
        TextMeshProUGUI _shelfHeader;
        RectTransform _shelfRoot;
        readonly List<UiCell> _shelfCells = new List<UiCell>();

        // 편집기
        TextMeshProUGUI _name, _market, _speed, _hint;
        TMP_InputField _priceInput;
        Slider _slider;
        Button _placeBtn, _removeBtn, _placeAllBtn;
        RectTransform _editorRoot;
        readonly List<Button> _pctButtons = new List<Button>();

        Sel _sel;
        string _selItem;
        int _selSlot = -1;
        double _price;
        bool _dirty = true;
        float _speedRefresh;

        const float SliderRange = 1f; // 시장가 × 10^[-1, +1]

        public static ShopPage Create(RectTransform content, GameModel model)
        {
            var rt = Ui.Rect("ShopPage", content).Stretch();
            var p = rt.gameObject.AddComponent<ShopPage>();
            p.Build(rt, model);
            return p;
        }

        void Build(RectTransform rt, GameModel model)
        {
            _model = model;
            _rt = rt;
            var a = GameAssets.Instance;

            // ── 창고 ──
            _whHeader = Ui.Text(rt, "WhHeader", "", Ui.SmallFont, Palette.Ash4);
            _whHeader.rectTransform.Place(4, 0, 120, 10);

            for (int g = 0; g < GradeInfo.Count; g++)
            {
                var grade = (Grade)g;
                var cell = UiCell.Create(rt, $"Box_{grade}", 4 + g * 18, 11, 16);
                cell.SetIcon(a.Sprite(GradeInfo.BoxId(grade)) ?? Placeholder.Box(grade));
                cell.OnClick = () => OpenOne(grade);
                cell.TooltipText = () => $"{Ui.Colorize(GradeInfo.Name(grade) + " 박스", Palette.GradeColor(grade))} × {_model.Data.boxes[(int)grade]}\n클릭해서 하나 열기";
                _boxCells[g] = cell;
            }
            var openAll = Ui.Button(rt, "OpenAll", "전부", OpenAll);
            openAll.GetComponent<RectTransform>().Place(94, 11, 30, 16);

            // 아이템 목록(스크롤)
            var scrollRt = Ui.Rect("ItemScroll", rt).Place(4, 30, 120, 75);
            var scrollBg = scrollRt.gameObject.AddComponent<Image>();
            scrollBg.sprite = a.UiSprite("ui_panel_dark") ?? Placeholder.PanelDark();
            if (scrollBg.sprite.border != Vector4.zero) scrollBg.type = Image.Type.Sliced;
            scrollBg.raycastTarget = true;
            var viewport = Ui.Rect("Viewport", scrollRt).Stretch(2f);
            viewport.gameObject.AddComponent<RectMask2D>();
            _itemContent = Ui.Rect("Content", viewport);
            _itemContent.anchorMin = new Vector2(0f, 1f); _itemContent.anchorMax = new Vector2(1f, 1f);
            _itemContent.pivot = new Vector2(0f, 1f);
            _itemContent.anchoredPosition = Vector2.zero;
            _itemContent.sizeDelta = new Vector2(0f, 0f);
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = _itemContent;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 18f;

            // ── 진열대 ──
            _shelfHeader = Ui.Text(rt, "ShelfHeader", "", Ui.SmallFont, Palette.Ash4);
            _shelfHeader.rectTransform.Place(128, 0, 140, 10);
            _shelfRoot = Ui.Rect("Shelf", rt).Place(128, 11, 140, 94);

            // ── 편집기 ──
            _editorRoot = Ui.Rect("Editor", rt).Place(272, 0, 108, 106);
            _name = Ui.Text(_editorRoot, "Name", "", Ui.SmallFont, Palette.Ash5);
            _name.rectTransform.Place(0, 0, 108, 10);
            _market = Ui.Text(_editorRoot, "Market", "", Ui.SmallFont, Palette.Ash4);
            _market.rectTransform.Place(0, 11, 108, 10);

            _priceInput = Ui.NumberInput(_editorRoot, "Price", OnPriceTyped);
            _priceInput.GetComponent<RectTransform>().Place(0, 22, 72, 12);
            var unit = Ui.Text(_editorRoot, "Unit", "너트", Ui.SmallFont, Palette.Ash4);
            unit.rectTransform.Place(75, 22, 33, 12);

            _slider = Ui.Slider(_editorRoot, "Slider", -SliderRange, SliderRange, OnSlider);
            _slider.GetComponent<RectTransform>().Place(0, 36, 108, 8);

            double[] mults = { 0.5, 0.8, 1.0, 1.2, 1.5, 2.0 };
            string[] labels = { "-50%", "-20%", "시장가", "+20%", "+50%", "×2" };
            for (int i = 0; i < mults.Length; i++)
            {
                double m = mults[i];
                var b = Ui.Button(_editorRoot, "Pct" + i, labels[i], () => SetPriceFromMarket(m), null, 8f);
                b.GetComponent<RectTransform>().Place((i % 3) * 36, 46 + (i / 3) * 11, 35, 10);
                _pctButtons.Add(b);
            }

            _speed = Ui.Text(_editorRoot, "Speed", "", Ui.SmallFont, Palette.Ash5);
            _speed.rectTransform.Place(0, 69, 108, 10);

            _placeBtn = Ui.Button(_editorRoot, "Place", "진열하기", PlaceSelected);
            _placeBtn.GetComponent<RectTransform>().Place(0, 81, 53, 13);
            _removeBtn = Ui.Button(_editorRoot, "Remove", "내리기", RemoveSelected);
            _removeBtn.GetComponent<RectTransform>().Place(55, 81, 53, 13);
            _placeAllBtn = Ui.Button(_editorRoot, "PlaceAll", "같은 물건 모두 진열", PlaceAllSame, null, 8f);
            _placeAllBtn.GetComponent<RectTransform>().Place(0, 95, 108, 11);

            _hint = Ui.Text(_editorRoot, "Hint", "창고 물건이나\n진열대 칸을 골라\n가격을 정하세요.", Ui.SmallFont, Palette.Ash3, TextAlignmentOptions.TopLeft);
            _hint.textWrappingMode = TextWrappingModes.Normal;
            _hint.rectTransform.Place(0, 0, 108, 60);

            model.EconomyChanged += MarkDirty;
            Select(Sel.None, null, -1);
        }

        void OnDestroy() { if (_model != null) _model.EconomyChanged -= MarkDirty; }
        void MarkDirty() => _dirty = true;
        void OnEnable() => _dirty = true;

        void Update()
        {
            if (_dirty) { _dirty = false; RefreshAll(); }
            _speedRefresh -= Time.unscaledDeltaTime;
            if (_speedRefresh <= 0f) { _speedRefresh = 0.5f; RefreshEditorInfo(); RefreshShelfDots(); }
        }

        // ───────────────────────── 갱신 ─────────────────────────

        void RefreshAll()
        {
            var d = _model.Data;
            _whHeader.text = _model.WarehouseFull
                ? Ui.Colorize($"창고 {_model.WarehouseUsed}/{_model.WarehouseCap} (가득!)", Palette.Red3)
                : $"창고 {_model.WarehouseUsed}/{_model.WarehouseCap}";

            for (int g = 0; g < GradeInfo.Count; g++)
            {
                int n = d.boxes[g];
                _boxCells[g].SetCountText(n > 0 ? n.ToString() : "");
                _boxCells[g].Icon.color = n > 0 ? Color.white : new Color(1, 1, 1, 0.25f);
            }

            RefreshItems();
            RefreshShelf();

            // 선택 유효성
            if (_sel == Sel.Warehouse && _model.CountOf(_selItem) <= 0) Select(Sel.None, null, -1);
            else if (_sel == Sel.Shelf && (_selSlot >= d.shelf.Count || d.shelf[_selSlot].IsEmpty)) Select(Sel.None, null, -1);
            else RefreshEditorInfo();
        }

        void RefreshItems()
        {
            _itemIds.Clear();
            // 등급 높은 순 → 이름 순
            var stacks = new List<ItemStack>(_model.Data.warehouse);
            stacks.Sort((x, y) =>
            {
                var a = ItemCatalog.Get(x.id); var b = ItemCatalog.Get(y.id);
                int c = b.Grade.CompareTo(a.Grade);
                return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
            });
            foreach (var s in stacks) _itemIds.Add(s.id);

            const int cols = 6; const float cell = 18f, gap = 1f;
            while (_itemCells.Count < _itemIds.Count)
            {
                int idx = _itemCells.Count;
                var c = UiCell.Create(_itemContent, "Item" + idx, 0, 0, cell);
                c.OnClick = () => { if (idx < _itemIds.Count) Select(Sel.Warehouse, _itemIds[idx], -1); };
                c.TooltipText = () => idx < _itemIds.Count ? ItemTooltip(ItemCatalog.Get(_itemIds[idx]), null) : null;
                _itemCells.Add(c);
            }
            for (int i = 0; i < _itemCells.Count; i++)
            {
                var c = _itemCells[i];
                bool on = i < _itemIds.Count;
                c.gameObject.SetActive(on);
                if (!on) continue;
                var def = ItemCatalog.Get(_itemIds[i]);
                c.GetComponent<RectTransform>().Place((i % cols) * (cell + gap), (i / cols) * (cell + gap), cell, cell);
                c.SetIcon(GameAssets.Instance.ItemIcon(def.Id) ?? Placeholder.Item(def.Grade));
                c.SetCount(_model.CountOf(def.Id));
                c.SetDot(def.Grade >= Grade.Rare ? Palette.GradeColor(def.Grade) : (Color32?)null);
                c.SetSelected(_sel == Sel.Warehouse && _selItem == def.Id);
            }
            int rows = Mathf.CeilToInt(_itemIds.Count / (float)cols);
            _itemContent.sizeDelta = new Vector2(0f, rows * (cell + gap));
        }

        void RefreshShelf()
        {
            var shelf = _model.Data.shelf;
            _shelfHeader.text = $"진열대 {_model.OccupiedShelfCount}/{shelf.Count}";
            const int cols = 6; const float cell = 22f, gap = 1f;
            while (_shelfCells.Count < shelf.Count)
            {
                int idx = _shelfCells.Count;
                var c = UiCell.Create(_shelfRoot, "Slot" + idx, (idx % cols) * (cell + gap), (idx / cols) * (cell + gap), cell);
                c.OnClick = () => OnShelfClicked(idx);
                c.TooltipText = () =>
                {
                    if (idx >= _model.Data.shelf.Count) return null;
                    var s = _model.Data.shelf[idx];
                    return s.IsEmpty ? "빈 칸\n창고에서 물건을 골라 진열하세요." : ItemTooltip(ItemCatalog.Get(s.itemId), s);
                };
                _shelfCells.Add(c);
            }
            for (int i = 0; i < _shelfCells.Count; i++)
            {
                var c = _shelfCells[i];
                bool exists = i < shelf.Count;
                c.gameObject.SetActive(exists);
                if (!exists) continue;
                var s = shelf[i];
                if (s.IsEmpty)
                {
                    c.SetIcon(null);
                    c.SetCountText("");
                    c.SetDot(null);
                }
                else
                {
                    var def = ItemCatalog.Get(s.itemId);
                    c.SetIcon(GameAssets.Instance.ItemIcon(def.Id) ?? Placeholder.Item(def.Grade));
                    c.SetCountText(ShortPrice(s.price));
                }
                c.SetSelected(_sel == Sel.Shelf && _selSlot == i);
            }
            RefreshShelfDots();
        }

        void RefreshShelfDots()
        {
            var shelf = _model.Data.shelf;
            for (int i = 0; i < _shelfCells.Count && i < shelf.Count; i++)
            {
                var s = shelf[i];
                if (s.IsEmpty) { _shelfCells[i].SetDot(null); continue; }
                var def = ItemCatalog.Get(s.itemId);
                _shelfCells[i].SetDot(SpeedColor(GameModel.ClassifySpeed(_model.ExpectedSellSeconds(def, s.price))));
            }
        }

        static string ShortPrice(double p) => p < 10000 ? ((long)p).ToString() : BigNum.Format(p);

        public static Color32 SpeedColor(SellSpeed s) => s switch
        {
            SellSpeed.Instant => Palette.Cyan,
            SellSpeed.Fast => Palette.Moss2,
            SellSpeed.Normal => Palette.Lamp0,
            SellSpeed.Slow => Palette.Red3,
            SellSpeed.VerySlow => Palette.Red1,
            _ => Palette.Magenta,
        };

        string ItemTooltip(ItemDef def, ShelfSlot slot)
        {
            if (def == null) return null;
            double market = _model.MarketPrice(def);
            double trend = _model.MarketTrend(def);
            string arrow = trend > 0.02 ? Ui.Colorize($"▲{trend * 100:0}%", Palette.Moss2)
                         : trend < -0.02 ? Ui.Colorize($"▼{-trend * 100:0}%", Palette.Red3) : "";
            string s = $"{Ui.Colorize(def.Name, Palette.GradeColor(def.Grade))} <size=80%>[{GradeInfo.Name(def.Grade)}]</size>\n" +
                       $"시장가 {Ui.Colorize(BigNum.Price(market), Palette.Lamp1)} 너트 {arrow}\n" +
                       $"<size=85%><color={Palette.Hex(Palette.Ash3)}>{def.Description}</color></size>";
            if (slot != null)
            {
                var speed = GameModel.ClassifySpeed(_model.ExpectedSellSeconds(def, slot.price));
                s += $"\n진열가 {Ui.Colorize(BigNum.Price(slot.price), Palette.Lamp1)} · {Ui.Colorize(GameModel.SpeedLabel(speed), SpeedColor(speed))}";
            }
            return s;
        }

        // ───────────────────────── 선택·편집 ─────────────────────────

        void Select(Sel kind, string itemId, int slot)
        {
            _sel = kind;
            _selItem = itemId;
            _selSlot = slot;

            bool any = kind != Sel.None;
            foreach (Transform child in _editorRoot) child.gameObject.SetActive(any);
            _hint.gameObject.SetActive(!any);

            if (kind == Sel.Warehouse)
            {
                var def = ItemCatalog.Get(itemId);
                _price = _model.MarketPrice(def);
                _placeBtn.gameObject.SetActive(true);
                _placeAllBtn.gameObject.SetActive(true);
                _removeBtn.gameObject.SetActive(false);
                _placeBtn.GetComponent<RectTransform>().Place(0, 81, 108, 13);
            }
            else if (kind == Sel.Shelf)
            {
                var s = _model.Data.shelf[slot];
                _selItem = s.itemId;
                _price = s.price;
                _placeBtn.gameObject.SetActive(false);
                _placeAllBtn.gameObject.SetActive(false);
                _removeBtn.gameObject.SetActive(true);
                _removeBtn.GetComponent<RectTransform>().Place(0, 81, 108, 13);
            }

            foreach (var c in _itemCells) c.SetSelected(false);
            for (int i = 0; i < _itemCells.Count && i < _itemIds.Count; i++)
                _itemCells[i].SetSelected(kind == Sel.Warehouse && _itemIds[i] == itemId);
            for (int i = 0; i < _shelfCells.Count; i++) _shelfCells[i].SetSelected(kind == Sel.Shelf && i == slot);

            if (any) { SyncPriceWidgets(); RefreshEditorInfo(); }
        }

        void OnShelfClicked(int idx)
        {
            var shelf = _model.Data.shelf;
            if (idx >= shelf.Count) return;
            if (!shelf[idx].IsEmpty) { Select(Sel.Shelf, null, idx); return; }
            // 빈 칸 + 창고 아이템 선택 중 → 그 칸에 진열
            if (_sel == Sel.Warehouse && _selItem != null)
            {
                string id = _selItem;
                if (_model.PlaceOnShelf(id, _price, idx)) AudioManager.Instance?.Sfx("sfx_item_place");
                if (_model.CountOf(id) > 0) Select(Sel.Warehouse, id, -1);
            }
        }

        void RefreshEditorInfo()
        {
            if (_sel == Sel.None || _selItem == null) return;
            var def = ItemCatalog.Get(_selItem);
            if (def == null) return;
            string count = _sel == Sel.Warehouse ? $" ×{_model.CountOf(def.Id)}" : $" <size=80%>(진열 {_selSlot + 1}번)</size>";
            _name.text = Ui.Colorize(def.Name, Palette.GradeColor(def.Grade)) + count;
            double market = _model.MarketPrice(def);
            double trend = _model.MarketTrend(def);
            string arrow = trend > 0.02 ? Ui.Colorize($" ▲{trend * 100:0}%", Palette.Moss2)
                         : trend < -0.02 ? Ui.Colorize($" ▼{-trend * 100:0}%", Palette.Red3) : "";
            _market.text = $"시장가 {Ui.Colorize(BigNum.Price(market), Palette.Lamp1)}{arrow}";

            // 진열대 칸 선택 중이면 이미 진열된 상태 기준, 창고 선택이면 하나 더 진열된다고 가정
            double secs = _model.ExpectedSellSeconds(def, _price);
            var sp = GameModel.ClassifySpeed(secs);
            string when = double.IsInfinity(secs) ? "" : $" · 약 {BigNum.Duration(secs)}";
            _speed.text = $"예상 {Ui.Colorize(GameModel.SpeedLabel(sp), SpeedColor(sp))}{when}";
        }

        void SyncPriceWidgets()
        {
            _priceInput.SetTextWithoutNotify(((long)Math.Round(_price)).ToString(CultureInfo.InvariantCulture));
            var def = ItemCatalog.Get(_selItem);
            double market = Math.Max(1d, _model.MarketPrice(def));
            float v = (float)Math.Log10(Math.Max(1e-9, _price / market));
            _slider.SetValueWithoutNotify(Mathf.Clamp(v, -SliderRange, SliderRange));
        }

        void ApplyPrice(double price, bool syncWidgets)
        {
            _price = Math.Max(1d, Math.Round(price));
            if (_sel == Sel.Shelf) _model.SetPrice(_selSlot, _price);
            if (syncWidgets) SyncPriceWidgets();
            RefreshEditorInfo();
            AudioManager.Instance?.Sfx("sfx_price_tick", 0.5f);
        }

        void OnPriceTyped(string text)
        {
            if (_sel == Sel.None) return;
            if (double.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var p)) ApplyPrice(p, true);
            else SyncPriceWidgets();
        }

        void OnSlider(float v)
        {
            if (_sel == Sel.None) return;
            var def = ItemCatalog.Get(_selItem);
            double market = _model.MarketPrice(def);
            double p = market * Math.Pow(10d, v);
            // 보기 좋은 숫자로 반올림(유효숫자 2~3자리)
            double mag = Math.Pow(10d, Math.Max(0d, Math.Floor(Math.Log10(Math.Max(1d, p))) - 1d));
            ApplyPrice(Math.Round(p / mag) * mag, false);
            _priceInput.SetTextWithoutNotify(((long)_price).ToString(CultureInfo.InvariantCulture));
        }

        void SetPriceFromMarket(double mult)
        {
            if (_sel == Sel.None) return;
            ApplyPrice(_model.MarketPrice(_selItem) * mult, true);
        }

        void PlaceSelected()
        {
            if (_sel != Sel.Warehouse) return;
            string id = _selItem;
            if (_model.FirstEmptySlot() < 0) { UiRoot.Instance?.Toast("진열대가 가득 찼다. 진열대를 늘리거나 물건을 내리자."); return; }
            if (_model.PlaceOnShelf(id, _price)) AudioManager.Instance?.Sfx("sfx_item_place");
            if (_model.CountOf(id) > 0) Select(Sel.Warehouse, id, -1);
        }

        void PlaceAllSame()
        {
            if (_sel != Sel.Warehouse) return;
            string id = _selItem;
            int placed = 0;
            while (_model.CountOf(id) > 0 && _model.FirstEmptySlot() >= 0 && _model.PlaceOnShelf(id, _price)) placed++;
            if (placed > 0) AudioManager.Instance?.Sfx("sfx_item_place");
            if (_model.FirstEmptySlot() < 0 && _model.CountOf(id) > 0) UiRoot.Instance?.Toast("진열대가 가득 찼다.");
        }

        void RemoveSelected()
        {
            if (_sel != Sel.Shelf) return;
            _model.TakeOffShelf(_selSlot);
            AudioManager.Instance?.Sfx("sfx_ui_close", 0.6f);
            Select(Sel.None, null, -1);
        }

        void OpenOne(Grade g)
        {
            var item = _model.OpenBox(g);
            if (item == null) return;
        }

        void OpenAll()
        {
            var items = _model.OpenAllBoxes();
            if (items.Count > 1) UiRoot.Instance?.Toast($"박스 {items.Count}개를 열었다.");
        }
    }
}
