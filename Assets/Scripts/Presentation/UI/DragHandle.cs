using UnityEngine;
using UnityEngine.EventSystems;

namespace NexusBreaker
{
    /// <summary>테두리 없는 데스크톱 창을 끌어서 옮기는 손잡이.</summary>
    public class DragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public void OnBeginDrag(PointerEventData e) => DesktopWindow.Instance?.BeginDrag();
        public void OnDrag(PointerEventData e) => DesktopWindow.Instance?.UpdateDrag();
        public void OnEndDrag(PointerEventData e) => DesktopWindow.Instance?.EndDrag();
    }
}
