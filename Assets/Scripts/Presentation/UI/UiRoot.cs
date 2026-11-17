using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace NexusBreaker
{
    /// <summary>
    /// UI 전체 루트. 화면 = 아래 띠(384×96, 월드 + HUD) + 페이지가 열리면 위쪽 120px(상점/강화/설정).
    /// 정수 배율로 픽셀을 맞추고, 카메라 뷰포트를 띠 영역에 맞춘다.
    /// </summary>
    public class UiRoot : MonoBehaviour
    {
        public enum Page { None, Shop, Upgrade, Settings }

        public static UiRoot Instance { get; private set; }

        public Page Current { get; private set; } = Page.None;
        public bool PageOpen => Current != Page.None;
        public int Scale { get; private set; } = 3;
        public ModalPopup Modal { get; private set; }
        public FloatingTextLayer Floating { get; private set; }
        public event Action<Page> PageChanged;

        GameModel _model;
        Camera _cam;
        Canvas _canvas;
        CanvasScaler _scaler;
        RectTransform _frame, _strip, _pageArea, _pageContent;
        ToastFeed _toasts;
        ShopPage _shop;
        UpgradePage _upgrade;
        SettingsPage _settings;
        readonly Button[] _tabs = new Button[3];

        public RectTransform Frame => _frame;
        public int FrameHeightPx => WorldLayout.StripH + (PageOpen ? WorldLayout.PageH : 0);

        public static UiRoot Create(GameModel model, Camera cam)
        {
            var go = new GameObject("UI");
            var root = go.AddComponent<UiRoot>();
            root.Build(model, cam);
            return root;
        }

        void Build(GameModel model, Camera cam)
        {
            Instance = this;
            _model = model;
            _cam = cam;
            gameObject.layer = LayerMask.NameToLayer("UI");

            EnsureEventSystem();

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.pixelPerfect = true;
            _canvas.sortingOrder = 10;
            _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _scaler.referencePixelsPerUnit = WorldLayout.PPU;
            gameObject.AddComponent<GraphicRaycaster>();

            // 프레임: 화면 안의 게임 영역(왼쪽 아래 기준)
            _frame = Ui.Rect("Frame", transform);
            _frame.anchorMin = _frame.anchorMax = Vector2.zero;
            _frame.pivot = Vector2.zero;

            _strip = Ui.Rect("Strip", _frame);
            _strip.anchorMin = new Vector2(0f, 0f); _strip.anchorMax = new Vector2(1f, 0f);
            _strip.pivot = new Vector2(0.5f, 0f);
            _strip.sizeDelta = new Vector2(0f, WorldLayout.StripH);
            _strip.anchoredPosition = Vector2.zero;

            _pageArea = Ui.Rect("PageArea", _frame);
            _pageArea.anchorMin = new Vector2(0f, 1f); _pageArea.anchorMax = new Vector2(1f, 1f);
            _pageArea.pivot = new Vector2(0.5f, 1f);
            _pageArea.sizeDelta = new Vector2(0f, WorldLayout.PageH);
            _pageArea.anchoredPosition = Vector2.zero;
            var pageBg = Ui.Panel(_pageArea, "PageBg");
            pageBg.rectTransform.Stretch();

            string[] tabNames = { "상점", "강화", "설정" };
            Page[] tabPages = { Page.Shop, Page.Upgrade, Page.Settings };
            for (int i = 0; i < 3; i++)
            {
                Page p = tabPages[i];
                _tabs[i] = Ui.Button(_pageArea, "Tab" + i, tabNames[i], () => OpenPage(p));
                _tabs[i].GetComponent<RectTransform>().Place(4 + i * 42, 2, 40, 11);
            }
            var close = Ui.Button(_pageArea, "Close", GameAssets.Instance.UiIcon("close") != null ? "" : "×", ClosePage, GameAssets.Instance.UiIcon("close"));
            close.GetComponent<RectTransform>().Place(368, 2, 12, 11);

            _pageContent = Ui.Rect("Content", _pageArea).Place(0, 14, WorldLayout.StripW, WorldLayout.PageH - 14);
            _shop = ShopPage.Create(_pageContent, model);
            _upgrade = UpgradePage.Create(_pageContent, model);
            _settings = SettingsPage.Create(_pageContent, model, this);

            HudView.Create(_strip, model, this);
            Floating = FloatingTextLayer.Create(_strip, cam);
            _toasts = ToastFeed.Create(_strip, 92, 30, 200);
            Tooltip.Create(_frame);
            Modal = ModalPopup.Create(_frame);

            model.Toast += m => Toast(m);
            model.BoxOpened += OnBoxOpened;
            model.CustomerResolved += OnCustomerResolved;
            model.NexusDestroyed += OnNexusDestroyed;
            model.MarketEventStarted += ev => AudioManager.Instance?.Sfx("sfx_ui_open", 0.6f);

            ShowPage(Page.None);
            UpdateLayout();
        }

        static void EnsureEventSystem()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
            }
            // 구식 StandaloneInputModule 대신 Input System 모듈만 사용
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null) Destroy(legacy);
            var module = es.GetComponent<InputSystemUIInputModule>();
            if (module == null) module = es.gameObject.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null) module.AssignDefaultActions();
        }

        // ───────────────────────── 페이지 ─────────────────────────

        public void OpenPage(Page p)
        {
            if (p == Current) return;
            AudioManager.Instance?.Sfx(p == Page.None ? "sfx_ui_close" : "sfx_ui_open", 0.7f);
            ShowPage(p);
        }

        public void TogglePage(Page p) => OpenPage(Current == p ? Page.None : p);
        public void ClosePage() => OpenPage(Page.None);

        void ShowPage(Page p)
        {
            Current = p;
            _pageArea.gameObject.SetActive(p != Page.None);
            _shop.gameObject.SetActive(p == Page.Shop);
            _upgrade.gameObject.SetActive(p == Page.Upgrade);
            _settings.gameObject.SetActive(p == Page.Settings);
            for (int i = 0; i < _tabs.Length; i++)
                _tabs[i].GetComponent<Image>().color = (int)p - 1 == i ? Color.white : new Color(0.7f, 0.65f, 0.6f);
            Tooltip.Instance?.Hide();
            PageChanged?.Invoke(p);
            UpdateLayout();
        }

        // ───────────────────────── 레이아웃 ─────────────────────────

        void LateUpdate() => UpdateLayout();

        /// <summary>화면 크기에 맞는 정수 배율 계산 → 캔버스 스케일, 프레임 위치, 카메라 뷰포트.</summary>
        public void UpdateLayout()
        {
            int sw = Mathf.Max(1, Screen.width), sh = Mathf.Max(1, Screen.height);
            int fh = FrameHeightPx;
            int scale = Mathf.Max(1, Mathf.Min(sw / WorldLayout.StripW, sh / fh));
            Scale = scale;
            _scaler.scaleFactor = scale;

            int fwPx = WorldLayout.StripW * scale, fhPx = fh * scale;
            int ox = (sw - fwPx) / 2, oy = (sh - fhPx) / 2;

            _frame.anchoredPosition = new Vector2(ox / (float)scale, oy / (float)scale);
            _frame.sizeDelta = new Vector2(WorldLayout.StripW, fh);

            if (_cam != null)
            {
                _cam.pixelRect = new Rect(ox, oy, fwPx, WorldLayout.StripH * scale);
                _cam.orthographicSize = WorldLayout.HalfH;
            }
        }

        public void ApplyWindowSettings() => GameRoot.Instance?.ApplyWindowSettings();

        // ───────────────────────── 알림 ─────────────────────────

        public void Toast(string msg, Color32? color = null) => _toasts.Push(msg, color);

        void OnBoxOpened(ItemDef item, Grade g, bool isNew)
        {
            string name = Ui.Colorize(item.Name, Palette.GradeColor(item.Grade));
            Toast(isNew ? $"{name} 획득! {Ui.Colorize("NEW", Palette.Magenta)}" : $"{name} 획득");
        }

        void OnCustomerResolved(CustomerVisit v)
        {
            switch (v.Outcome)
            {
                case VisitOutcome.Bought:
                    double ratio = v.Market > 0 ? v.Price / v.Market : 1d;
                    string tag = ratio >= 2.5 ? Ui.Colorize(" 대박!", Palette.Magenta) : ratio >= 1.3 ? Ui.Colorize(" 비싸게!", Palette.Lamp0) : "";
                    Toast($"{v.Customer.Name}: {v.Item.Name} {BigNum.Price(v.Price)}너트에 구매{tag}");
                    break;
                case VisitOutcome.Rejected:
                    if (v.LookedAtMostExpensive != null)
                        Toast($"{v.Customer.Name}: \"{v.LookedAtMostExpensive.Name}, 너무 비싼데…\"", Palette.Ash3);
                    break;
                case VisitOutcome.EmptyShelf:
                    Toast($"{v.Customer.Name}: \"살 게 없네…\"", Palette.Ash3);
                    break;
            }
        }

        void OnNexusDestroyed()
        {
            Modal.Show("넥서스 파괴!",
                $"{RegionCatalog.Name(_model.Data.region)}의 넥서스를 부쉈다!\n넥서스 코어 {_model.CoresOnPrestige}개를 얻고 다음 지역으로 떠날 수 있다.\n(너트·강화·창고는 초기화, 코어·도감은 유지)",
                "다음 지역으로", () => _model.Prestige(), "조금 더 있기", null);
        }

        public void ShowOfflineReport(OfflineReport r)
        {
            if (r == null || r.Seconds < 60d) return;
            int boxes = 0; foreach (var b in r.Boxes) boxes += b;
            string body =
                $"{BigNum.Duration(r.Seconds)} 동안 수리공이 일했다{(r.HitTimeCap ? " (최대 시간까지만)" : "")}.\n" +
                $"데미지 {BigNum.Format(r.Damage)} · 박스 {boxes}개{(r.CappedByWarehouse ? " (창고 가득)" : "")}\n" +
                $"판매 {r.ItemsSold}개 · {Ui.Colorize("+" + BigNum.Price(r.NutsEarned) + " 너트", Palette.Lamp1)}";
            Modal.Show("자리 비운 사이", body);
            AudioManager.Instance?.Sfx("sfx_offline_reward");
        }

        /// <summary>DesktopWindow의 클릭 통과 판정용: 화면 좌표가 UI 요소 위인가.</summary>
        public bool IsOverUi(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            var results = new System.Collections.Generic.List<RaycastResult>();
            es.RaycastAll(data, results);
            return results.Count > 0;
        }
    }
}
