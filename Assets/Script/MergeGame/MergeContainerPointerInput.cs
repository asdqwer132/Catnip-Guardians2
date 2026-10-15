using UnityEngine;
using UnityEngine.EventSystems;

// 선택은 아이템을 예약합니다. 드래그 중 다른 아이템과 닿아도 선택한 아이템은 바뀌지 않습니다.
public sealed class MergeContainerPointerInput : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
{
    public MergeGameSceneController controller;
    private MergeContainerRuntime gesture;
    private Vector2 start;
    private Vector3 previous;
    private bool dragging, pressed;
    public void OnPointerDown(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || controller == null || !controller.CanInteract()) return;
        gesture = controller.Current;
        if (gesture == null || !controller.HasTarget) return;
        start = data.position; previous = controller.PointerWorld(start); pressed = true; dragging = false;
    }
    public void OnBeginDrag(PointerEventData data) { if (pressed) dragging = true; }
    public void OnDrag(PointerEventData data)
    {
        if (!pressed || gesture != controller.Current || !controller.CanInteract()) { Cancel(); return; }
        dragging = true;
        if (!gesture.Board.CanSelect(true) || !controller.PointerInside(data.position)) return;
        Vector3 point = controller.PointerWorld(data.position);
        controller.MarkDragSelection(point, true);
        gesture.SelectNearSegment(previous, point, gesture.Stats.dragRadius); previous = point;
    }
    public void OnEndDrag(PointerEventData data) { /* PointerUp에서 한 번만 확정합니다. */ }
    public void OnPointerUp(PointerEventData data)
    {
        if (!pressed || data.button != PointerEventData.InputButton.Left) return;
        pressed = false;
        controller.MarkDragSelection(Vector3.zero, false);
        if (controller == null || !controller.CanInteract() || gesture != controller.Current || !controller.PointerInside(data.position)) { Cancel(); return; }
        if (dragging)
        {
            if (!gesture.Board.CanSelect(true)) return;
            Vector3 point = controller.PointerWorld(data.position); gesture.SelectNearSegment(previous, point, gesture.Stats.dragRadius);
        }
        else
        {
            if (!gesture.Board.CanSelect(false)) return;
            gesture.SelectOne(controller.PointerWorld(data.position));
        }
        if (controller.catalog.selectionDelivery == MergeSelectionDelivery.OnRelease) controller.ThrowSelected(dragging);
    }
    private void Cancel() { pressed = dragging = false; if (controller != null) controller.ClearSelection(); gesture = null; }
    private void OnDisable() => Cancel();
}
