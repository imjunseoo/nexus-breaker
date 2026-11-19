using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NexusBreaker
{
    /// <summary>
    /// 도감 페이지(v0.2): 왼쪽은 등급별 물건 칸(못 찾은 건 실루엣), 오른쪽은 고른 물건의 설명과 기록.
    /// </summary>
    public class CodexPage : MonoBehaviour
    {
        public static CodexPage Instance { get; private set; }
        const float Cell = 16f, Gap = 2f, RowH = 20f, GridX = 50f;
        // 못 찾은 물건: 아주 어둡게(모양만 어렴풋이)
        static readonly Color Silhouette = new Color(0.2f, 0.18f, 0.24f, 1f);

        GameModel _model;
        readonly List<(UiCell cell, ItemDef def)> _cells = new List<(UiCell, ItemDef)>();
        readonly TextMeshProUGUI[] _gradeLabels = new TextMeshProUGUI[GradeInfo.Count];
        Image _bigIcon;
        TextMeshProUGUI _name, _grade, _desc, _stats, _summary;
        string _selected;
        bool _dirty = true;

        public static CodexPage Create(RectTransform content, GameModel model)
        {
            var rt = Ui.Rect("CodexPage", content).Stretch();
            var p = rt.gameObject.AddComponent<CodexPage>();
            p.Build(rt, model);
            return p;
        }

        void Build(RectTransform rt, GameModel model)
        {
            _model = model;
            Instance = this;
            var a = GameAssets.Instance;

            for (int g = 0; g < GradeInfo.Count; g++)
            {
                var grade = (Grade)g;
                var label = Ui.Text(rt, "Grade" + g, "", 8f, Palette.GradeColor(grade), TextAlignmentOptions.MidlineLeft);
                label.rectTransform.Place(4, 1 + g * RowH, GridX - 5, Cell);
                _gradeLabels[g] = label;

                int col = 0;
                foreach (var def in ItemCatalog.OfGrade(grade))
                {
                    var d = def;
                    var cell = UiCell.Create(rt, "Item_" + def.Id, GridX + col * (Cell + Gap), 1 + g * RowH, Cell);
                    cell.OnClick = () => Select(d.Id);
                    cell.TooltipText = () => _model.IsDiscovered(d.Id) ? d.Name : $"??? [{GradeInfo.Name(d.Grade)}]";
                    _cells.Add((cell, def));
                    col++;
                }
            }

            // 오른쪽 상세
            var detail = Ui.Panel(rt, "Detail", dark: true);
            detail.rectTransform.Place(236, 1, 144, 104);
            var dr = detail.rectTransform;

            var iconBg = Ui.Image(dr, "IconBg", a.UiSprite("ui_slot", "ui_slot/empty", "ui_slot_empty", "ui_slot") ?? Placeholder.Slot(), null, sliced: true);
            iconBg.preserveAspect = false;
            iconBg.rectTransform.Place(7, 7, 36, 36);
            _bigIcon = Ui.Image(iconBg.rectTransform, "Icon", null);
            _bigIcon.rectTransform.Place(2, 2, 32, 32);

            _name = Ui.Text(dr, "Name", "", Ui.SmallFont, Palette.Ash5, TextAlignmentOptions.TopLeft);
            _name.textWrappingMode = TextWrappingModes.Normal;
            _name.rectTransform.Place(47, 8, 90, 22);
            _grade = Ui.Text(dr, "Grade", "", 8f, Palette.Ash3, TextAlignmentOptions.MidlineLeft);
            _grade.rectTransform.Place(47, 32, 90, 9);

            _desc = Ui.Text(dr, "Desc", "", 8f, Palette.Ash3, TextAlignmentOptions.TopLeft);
            _desc.textWrappingMode = TextWrappingModes.Normal;
            _desc.rectTransform.Place(8, 47, 128, 28);

            _stats = Ui.Text(dr, "Stats", "", 8f, Palette.Ash4, TextAlignmentOptions.TopLeft);
            _stats.textWrappingMode = TextWrappingModes.Normal;
            _stats.rectTransform.Place(8, 77, 128, 20);

            _summary = Ui.Text(dr, "Summary", "", Ui.SmallFont, Palette.Ash4, TextAlignmentOptions.Center);
            _summary.textWrappingMode = TextWrappingModes.Normal;
            _summary.rectTransform.Place(8, 20, 128, 64);

            model.BoxOpened += (item, g, isNew) => { if (isNew) _dirty = true; };
            model.CustomerResolved += v => { if (v.Outcome == VisitOutcome.Bought && v.Item != null && v.Item.Id == _selected) _dirty = true; };
            model.Prestiged += () => _dirty = true;
        }

        void OnEnable() => _dirty = true;

        /// <summary>자동 검증용: 물건 하나를 고른 상태로.</summary>
        public void DebugSelect(string id) { _selected = id; _dirty = true; }

        void Select(string id)
        {
            _selected = id;
            _dirty = true;
            AudioManager.Instance?.Sfx("sfx_ui_click", 0.5f);
        }

        void Update()
        {
            if (!_dirty) return;
            _dirty = false;

            for (int g = 0; g < GradeInfo.Count; g++)
            {
                _model.GradeProgress((Grade)g, out int found, out int total);
                // 다 모은 등급은 숫자를 금색으로
                _gradeLabels[g].text = $"{GradeInfo.Names[g]} <color={Palette.Hex(found == total ? Palette.Lamp1 : Palette.Ash4)}>{found}/{total}</color>";
            }

            foreach (var (cell, def) in _cells)
            {
                bool known = _model.IsDiscovered(def.Id);
                var icon = GameAssets.Instance.ItemIcon(def.Id) ?? Placeholder.Item(def.Grade);
                // 못 찾은 물건은 실루엣(검게)
                cell.SetIcon(icon, known ? Color.white : Silhouette);
                cell.SetSelected(def.Id == _selected);
            }

            var sel = ItemCatalog.Get(_selected);
            bool any = sel != null;
            _bigIcon.transform.parent.gameObject.SetActive(any);
            _name.gameObject.SetActive(any);
            _grade.gameObject.SetActive(any);
            _desc.gameObject.SetActive(any);
            _stats.gameObject.SetActive(any);
            _summary.gameObject.SetActive(!any);

            if (!any)
            {
                int n = _model.DiscoveredCount, total = ItemCatalog.All.Count;
                _summary.text = $"도감 {Ui.Colorize($"{n}/{total}", Palette.Lamp1)} ({n * 100 / Mathf.Max(1, total)}%)\n\n" +
                                Ui.Colorize("칸을 누르면 자세히 볼 수 있다.", Palette.Ash3);
                return;
            }

            bool knownSel = _model.IsDiscovered(sel.Id);
            var big = GameAssets.Instance.ItemIcon(sel.Id) ?? Placeholder.Item(sel.Grade);
            _bigIcon.sprite = big;
            _bigIcon.color = knownSel ? Color.white : Silhouette;
            _name.text = knownSel ? Ui.Colorize(sel.Name, Palette.GradeColor(sel.Grade)) : "???";
            _grade.text = $"[{GradeInfo.Name(sel.Grade)}] {(knownSel ? ContractText.CategoryName(sel.Category) : "")}";
            if (!knownSel)
            {
                _desc.text = "아직 발견하지 못한 물건이다. 박스를 열다 보면 언젠가…";
                _stats.text = "";
                return;
            }
            _desc.text = sel.Description;
            var e = _model.CodexOf(sel.Id);
            // v0.1 세이브는 발견 기록만 있고 개수 기록이 없다 → 최소 1개
            long found2 = System.Math.Max(1, e?.found ?? 0), sold = e?.sold ?? 0;
            string best = e != null && e.bestPrice > 0 ? $"{BigNum.Price(e.bestPrice)} 너트" : "-";
            _stats.text = $"얻은 수 {found2} · 판 수 {sold}\n최고 판매가 {Ui.Colorize(best, Palette.Lamp1)}";
        }
    }
}
