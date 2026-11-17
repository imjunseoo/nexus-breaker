using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NexusBreaker
{
    /// <summary>화면 가운데 확인 창(오프라인 정산, 넥서스 파괴, 초기화 확인).</summary>
    public class ModalPopup : MonoBehaviour
    {
        TextMeshProUGUI _title, _body;
        Button _ok, _cancel;
        Action _onOk, _onCancel;

        public static ModalPopup Create(RectTransform frame)
        {
            // 뒤를 막는 반투명 막
            var blocker = Ui.Image(frame, "ModalBlocker", Placeholder.White(), new Color(0.06f, 0.05f, 0.05f, 0.65f), raycast: true);
            blocker.preserveAspect = false;
            blocker.rectTransform.Stretch();
            var m = blocker.gameObject.AddComponent<ModalPopup>();

            var panel = Ui.Panel(blocker.rectTransform, "Panel");
            var prt = panel.rectTransform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(220f, 84f);
            prt.anchoredPosition = Vector2.zero;

            m._title = Ui.Text(prt, "Title", "", Ui.FontSize, Palette.Lamp1, TextAlignmentOptions.Center);
            m._title.rectTransform.Place(6, 4, 208, 14);
            m._body = Ui.Text(prt, "Body", "", Ui.SmallFont, Palette.Ash5, TextAlignmentOptions.Top);
            m._body.textWrappingMode = TextWrappingModes.Normal;
            m._body.rectTransform.Place(8, 20, 204, 44);
            m._ok = Ui.Button(prt, "Ok", "확인", () => m.Close(true));
            m._ok.GetComponent<RectTransform>().Place(122, 66, 60, 14);
            m._cancel = Ui.Button(prt, "Cancel", "취소", () => m.Close(false));
            m._cancel.GetComponent<RectTransform>().Place(38, 66, 60, 14);

            blocker.gameObject.SetActive(false);
            return m;
        }

        public bool IsOpen => gameObject.activeSelf;

        public void Show(string title, string body, string okLabel = "확인", Action onOk = null, string cancelLabel = null, Action onCancel = null)
        {
            _title.text = title;
            _body.text = body;
            _ok.SetLabel(okLabel);
            _onOk = onOk;
            _onCancel = onCancel;
            bool hasCancel = !string.IsNullOrEmpty(cancelLabel);
            _cancel.gameObject.SetActive(hasCancel);
            if (hasCancel) _cancel.SetLabel(cancelLabel);
            _ok.GetComponent<RectTransform>().Place(hasCancel ? 122 : 80, 66, 60, 14);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        void Close(bool ok)
        {
            gameObject.SetActive(false);
            var cb = ok ? _onOk : _onCancel;
            _onOk = _onCancel = null;
            cb?.Invoke();
        }
    }
}
