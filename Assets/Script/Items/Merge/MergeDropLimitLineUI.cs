using UnityEngine;
using UnityEngine.UI;

// 실제 판정선의 두 끝을 UI 좌표로 투영합니다. 상자 전환/카메라 이동/화면 크기 변경에도 같은 선을 표시합니다.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class MergeDropLimitLineUI : MonoBehaviour
{
    public MergeBoxSceneController boxes;
    public MergegameManager board;
    public Camera worldCamera;
    public Image lineImage;
    [Min(.1f)] public float thickness = 4f;

    private void OnEnable()
    {
        Canvas.willRenderCanvases += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= Refresh;
        SetVisible(false);
    }

    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        var current = boxes != null ? boxes.SelectedBoard : board;
        var zone = current != null ? current.dropZone : null;
        if (lineImage == null) return;
        var rect = lineImage.rectTransform;
        var parent = rect.parent as RectTransform;
        var canvas = lineImage.canvas;
        if (zone == null || !zone.isActiveAndEnabled || !zone.blockAboveLine || worldCamera == null ||
            !worldCamera.isActiveAndEnabled || parent == null || canvas == null)
        { SetVisible(false); return; }

        canvas = canvas.rootCanvas;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && uiCamera == null)
        { SetVisible(false); return; }

        Vector3 start = worldCamera.WorldToScreenPoint(zone.transform.TransformPoint(zone.lineStart));
        Vector3 end = worldCamera.WorldToScreenPoint(zone.transform.TransformPoint(zone.lineEnd));
        if (start.z <= 0f || end.z <= 0f ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, start, uiCamera, out var a) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, end, uiCamera, out var b))
        { SetVisible(false); return; }

        Vector2 delta = b - a;
        Vector3 scale = rect.localScale;
        if (delta.sqrMagnitude < .0001f || Mathf.Abs(scale.x) < .0001f || Mathf.Abs(scale.y) < .0001f)
        { SetVisible(false); return; }
        Vector2 center = (a + b) * .5f;
        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        float width = delta.magnitude / Mathf.Abs(scale.x), height = Mathf.Max(.1f, thickness) / Mathf.Abs(scale.y);
        if (rect.localRotation != rotation) rect.localRotation = rotation;
        if (Mathf.Abs(rect.rect.width - width) > .001f) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        if (Mathf.Abs(rect.rect.height - height) > .001f) rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        Vector3 pivotOffset = rotation * Vector3.Scale(rect.rect.center, scale);
        Vector3 position = new Vector3(center.x - pivotOffset.x, center.y - pivotOffset.y, rect.localPosition.z);
        if (rect.localPosition != position) rect.localPosition = position;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (lineImage != null && lineImage.enabled != visible) lineImage.enabled = visible;
    }
}
