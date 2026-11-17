using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NexusBreaker
{
    /// <summary>
    /// 첫 판매·첫 강화까지 안내하는 튜토리얼. 각 단계는 플레이어가 실제로 그 행동을 하면 넘어간다.
    /// 말풍선 + 깜빡이는 화살표 + 대상 강조 테두리. 언제든 건너뛸 수 있고, 진행 상태는 세이브(tutorialStep)에 저장된다.
    /// </summary>
    public class TutorialView : MonoBehaviour
    {
        enum Step { HitNexus = 0, WaitBox = 1, OpenShop = 2, OpenBox = 3, SelectItem = 4, Place = 5, WaitSale = 6, Upgrade = 7, Done = 8 }

        GameModel _m;
        UiRoot _ui;
        WorldDirector _world;
        Camera _cam;
        RectTransform _root;
        Image _highlight;
        TextMeshProUGUI _arrow;
        Image _bubble;
        TextMeshProUGUI _text;
        Button _skip;
        float _doneTimer;
        int _lastStep = -2;

        const float BubbleW = 150f;

        int StepIndex
        {
            get => _m.Data.tutorialStep;
            set
            {
                _m.Data.tutorialStep = value;
                if (value >= 0) AudioManager.Instance?.Sfx("sfx_ui_open", 0.45f);
            }
        }

        public static TutorialView Create(UiRoot ui, GameModel model, WorldDirector world, Camera cam)
        {
            var rt = Ui.Rect("Tutorial", ui.Frame).Stretch();
            // 팝업(오프라인 정산 등)보다는 아래에 그려지도록
            rt.SetSiblingIndex(ui.Modal.transform.GetSiblingIndex());
            var t = rt.gameObject.AddComponent<TutorialView>();
            t.Build(rt, ui, model, world, cam);
            return t;
        }

        void Build(RectTransform rt, UiRoot ui, GameModel m, WorldDirector world, Camera cam)
        {
            _root = rt;
            _ui = ui;
            _m = m;
            _world = world;
            _cam = cam;
            var a = GameAssets.Instance;

            var hlSprite = a.UiSprite("ui_slot", "ui_slot/selected", "ui_slot_selected") ?? Placeholder.Get(Placeholder.Shape.Panel, 12, 12, Palette.Shadow, Palette.Lamp0, false);
            _highlight = Ui.Image(_root, "Highlight", hlSprite, (Color)Palette.Lamp1, sliced: true);
            _highlight.preserveAspect = false;
            _highlight.fillCenter = false;
            BottomLeft(_highlight.rectTransform);

            _arrow = Ui.Text(_root, "Arrow", "▼", Ui.FontSize, Palette.Lamp1, TextAlignmentOptions.Midline).WithShadow();
            BottomLeft(_arrow.rectTransform);
            _arrow.rectTransform.sizeDelta = new Vector2(12f, 12f);

            _bubble = Ui.Panel(_root, "Bubble", dark: true);
            _bubble.raycastTarget = false;
            BottomLeft(_bubble.rectTransform);
            _text = Ui.Text(_bubble.rectTransform, "Text", "", Ui.SmallFont, Palette.Ash5, TextAlignmentOptions.TopLeft);
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.rectTransform.anchorMin = Vector2.zero; _text.rectTransform.anchorMax = Vector2.one;
            _text.rectTransform.offsetMin = new Vector2(5f, 12f);
            _text.rectTransform.offsetMax = new Vector2(-5f, -4f);

            _skip = Ui.Button(_bubble.rectTransform, "Skip", "건너뛰기", Skip, null, 8f);
            var srt = _skip.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(1f, 0f);
            srt.pivot = new Vector2(1f, 0f);
            srt.anchoredPosition = new Vector2(-3f, 2f);
            srt.sizeDelta = new Vector2(40f, 10f);

            m.Hit += h => { if (StepIndex == (int)Step.HitNexus && h.Source == HitSource.Click) StepIndex = (int)Step.WaitBox; };
            m.BoxOpened += (item, g, isNew) => { if (StepIndex == (int)Step.OpenBox) StepIndex = (int)Step.SelectItem; };
            m.CustomerResolved += v =>
            {
                if (StepIndex != (int)Step.WaitSale) return;
                if (v.Outcome == VisitOutcome.Bought) StepIndex = (int)Step.Upgrade;
                else m.HurryNextCustomer(); // 첫 판매를 너무 오래 기다리지 않게
            };
            m.Upgraded += (id, lv) => { if (StepIndex == (int)Step.Upgrade) StepIndex = (int)Step.Done; };

            if (StepIndex == (int)Step.HitNexus) m.PrimeFirstBox();
        }

        static void BottomLeft(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
        }

        void Skip()
        {
            StepIndex = -1;
            _root.gameObject.SetActive(false);
        }

        void Update()
        {
            int step = StepIndex;
            if (step < 0) { SetVisible(false); enabled = false; return; }

            AdvanceByState(ref step);
            if (StepIndex < 0) return;

            if (step != _lastStep)
            {
                _lastStep = step;
                if (step == (int)Step.WaitSale) _m.HurryNextCustomer();
            }

            if (_ui.Modal.IsOpen) { SetVisible(false); return; }
            SetVisible(true);

            Resolve(step, out string text, out Rect? target);
            Layout(text, target);
        }

        /// <summary>이벤트 없이 상태로 판단하는 진행/되돌림.</summary>
        void AdvanceByState(ref int step)
        {
            var shop = ShopPage.Instance;
            switch ((Step)step)
            {
                case Step.WaitBox:
                    if (_m.BoxCount > 0) step = StepIndex = (int)Step.OpenShop;
                    break;
                case Step.OpenShop:
                    if (_ui.Current == UiRoot.Page.Shop) step = StepIndex = (int)Step.OpenBox;
                    break;
                case Step.OpenBox:
                    if (_m.BoxCount == 0 && _m.ItemCountInWarehouse > 0) step = StepIndex = (int)Step.SelectItem;
                    break;
                case Step.SelectItem:
                    if (_m.OccupiedShelfCount > 0) step = StepIndex = (int)Step.WaitSale;
                    else if (shop != null && shop.HasWarehouseSelection) step = StepIndex = (int)Step.Place;
                    break;
                case Step.Place:
                    if (_m.OccupiedShelfCount > 0) step = StepIndex = (int)Step.WaitSale;
                    else if (shop != null && !shop.HasWarehouseSelection && _ui.Current == UiRoot.Page.Shop) step = StepIndex = (int)Step.SelectItem;
                    break;
                case Step.WaitSale:
                    // 진열한 걸 다시 내렸으면 고르기 단계로
                    if (_m.OccupiedShelfCount == 0 && _m.Data.run.itemsSold == 0 && _m.ItemCountInWarehouse > 0)
                        step = StepIndex = (int)Step.SelectItem;
                    break;
                case Step.Done:
                    _doneTimer += Time.unscaledDeltaTime;
                    if (_doneTimer > 7f) { StepIndex = -1; SetVisible(false); }
                    break;
            }
        }

        void Resolve(int step, out string text, out Rect? target)
        {
            target = null;
            bool shopOpen = _ui.Current == UiRoot.Page.Shop;
            switch ((Step)step)
            {
                case Step.HitNexus:
                    text = "넥서스를 클릭해서\n부숴 보세요!";
                    target = WorldRect(NexusCore);
                    return;
                case Step.WaitBox:
                    text = "계속 두드리면\n박스가 나와요!";
                    target = WorldRect(NexusCore);
                    return;
                case Step.OpenShop:
                    text = "박스가 나왔어요!\n상점을 열어 보세요.";
                    target = UiRect("btn_shop");
                    return;
                case Step.OpenBox when !shopOpen:
                case Step.SelectItem when !shopOpen:
                case Step.Place when !shopOpen:
                    text = "상점을 다시\n열어 주세요.";
                    target = UiRect("btn_shop");
                    return;
                case Step.OpenBox:
                    text = "박스를 눌러\n열어 보세요.";
                    for (int g = 0; g < GradeInfo.Count && target == null; g++)
                        if (_m.Data.boxes[g] > 0) target = UiRect("shop_box_" + g);
                    return;
                case Step.SelectItem:
                    text = "창고에서 물건을\n하나 골라 보세요.";
                    target = UiRect("shop_item_0");
                    return;
                case Step.Place:
                    text = "가격을 정하고 [진열하기]를\n누르세요. 시장가 근처면\n금방 팔려요!";
                    target = UiRect("shop_place");
                    return;
                case Step.WaitSale:
                    text = "손님이 사 가길\n기다려 보세요…";
                    target = WorldRect(new Bounds(WorldLayout.StallPos + new Vector3(0f, 0.95f, 0f), new Vector3(3.2f, 1.3f, 0f)));
                    return;
                case Step.Upgrade:
                    if (_ui.Current == UiRoot.Page.Upgrade)
                    {
                        text = _m.CanAfford(UpgradeId.Hammer) ? "[망치 강화]를\n눌러 보세요." : "너트가 조금 모자라요.\n조금만 더 팔아 봐요!";
                        target = UiRect("upgrade_buy_" + UpgradeId.Hammer);
                    }
                    else
                    {
                        text = "팔렸어요! 번 너트로\n망치를 강화해 보세요.";
                        target = UiRect("btn_upgrade");
                    }
                    return;
                default:
                    text = "좋아요! 이제 박스 열고,\n팔고, 강화하면서\n넥서스를 부숴 보세요!";
                    return;
            }
        }

        // ───────────────────────── 배치 ─────────────────────────

        /// <summary>넥서스 전체는 띠 화면보다 커서, 클릭을 유도할 가운데 코어 부분만 강조한다.</summary>
        static Bounds NexusCore => new Bounds(WorldLayout.NexusCenter + new Vector3(0f, -0.15f, 0f), new Vector3(1.3f, 1.3f, 0f));

        Rect? UiRect(string name)
        {
            var rt = UiTargets.Get(name);
            if (rt == null) return null;
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            Vector2 a = _root.InverseTransformPoint(c[0]);
            Vector2 b = _root.InverseTransformPoint(c[2]);
            return ToBottomLeft(a, b);
        }

        Rect? WorldRect(Bounds bw)
        {
            if (_cam == null) return null;
            Vector2 a = _root.InverseTransformPoint(_cam.WorldToScreenPoint(bw.min));
            Vector2 b = _root.InverseTransformPoint(_cam.WorldToScreenPoint(bw.max));
            return ToBottomLeft(a, b);
        }

        /// <summary>_root 피벗 기준 로컬 좌표 → 왼쪽 아래 기준 좌표.</summary>
        Rect ToBottomLeft(Vector2 a, Vector2 b)
        {
            var off = new Vector2(_root.rect.width * _root.pivot.x, _root.rect.height * _root.pivot.y);
            a += off; b += off;
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        void Layout(string text, Rect? target)
        {
            float fw = _root.rect.width, fh = _root.rect.height;
            float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
            float bob = Mathf.Round(Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 2f);

            _text.text = text;
            float textH = _text.GetPreferredValues(text, BubbleW - 10f, 0f).y;
            float bh = Mathf.Ceil(textH) + 16f;
            _bubble.rectTransform.sizeDelta = new Vector2(BubbleW, bh);

            Vector2 bubblePos;
            if (target.HasValue)
            {
                var r = target.Value;
                _highlight.gameObject.SetActive(true);
                _highlight.rectTransform.anchoredPosition = new Vector2(Mathf.Round(r.xMin - 2f), Mathf.Round(r.yMin - 2f));
                _highlight.rectTransform.sizeDelta = new Vector2(Mathf.Round(r.width + 4f), Mathf.Round(r.height + 4f));
                var hc = _highlight.color; hc.a = pulse; _highlight.color = hc;

                _arrow.gameObject.SetActive(true);
                float cx = r.center.x, cy = r.center.y;
                // 말풍선 자리: 위 → 아래 → 오른쪽 → 왼쪽 순으로 화면 안에 들어가는 곳
                if (r.yMax + 12f + bh <= fh)
                {
                    _arrow.text = "▼";
                    _arrow.rectTransform.anchoredPosition = new Vector2(Mathf.Round(cx - 6f), Mathf.Round(r.yMax + 1f + bob));
                    bubblePos = new Vector2(cx - BubbleW / 2f, r.yMax + 12f);
                }
                else if (r.yMin - 12f - bh >= 0f)
                {
                    _arrow.text = "▲";
                    _arrow.rectTransform.anchoredPosition = new Vector2(Mathf.Round(cx - 6f), Mathf.Round(r.yMin - 13f - bob));
                    bubblePos = new Vector2(cx - BubbleW / 2f, r.yMin - 12f - bh);
                }
                else if (r.xMax + 14f + BubbleW <= fw)
                {
                    _arrow.text = "◀";
                    _arrow.rectTransform.anchoredPosition = new Vector2(Mathf.Round(r.xMax + 1f + bob), Mathf.Round(cy - 6f));
                    bubblePos = new Vector2(r.xMax + 14f, cy - bh / 2f);
                }
                else
                {
                    _arrow.text = "▶";
                    _arrow.rectTransform.anchoredPosition = new Vector2(Mathf.Round(r.xMin - 13f - bob), Mathf.Round(cy - 6f));
                    bubblePos = new Vector2(r.xMin - 14f - BubbleW, cy - bh / 2f);
                }
            }
            else
            {
                _highlight.gameObject.SetActive(false);
                _arrow.gameObject.SetActive(false);
                // 대상이 없으면 수리공 머리 위
                var head = WorldRect(new Bounds(WorldLayout.RepairmanPos + new Vector3(0f, 1.1f, 0f), Vector3.zero));
                float hx = head.HasValue ? head.Value.center.x : fw / 2f;
                float hy = head.HasValue ? head.Value.center.y : 40f;
                bubblePos = new Vector2(hx - BubbleW / 2f, hy + 4f);
            }

            bubblePos.x = Mathf.Clamp(bubblePos.x, 2f, Mathf.Max(2f, fw - BubbleW - 2f));
            bubblePos.y = Mathf.Clamp(bubblePos.y, 2f, Mathf.Max(2f, fh - bh - 2f));
            _bubble.rectTransform.anchoredPosition = new Vector2(Mathf.Round(bubblePos.x), Mathf.Round(bubblePos.y));
        }

        void SetVisible(bool on)
        {
            if (_bubble.gameObject.activeSelf != on) _bubble.gameObject.SetActive(on);
            if (!on)
            {
                _highlight.gameObject.SetActive(false);
                _arrow.gameObject.SetActive(false);
            }
        }
    }
}
