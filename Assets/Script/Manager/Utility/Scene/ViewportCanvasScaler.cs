using UnityEngine;
using UnityEngine.UI;

// 기본 CanvasScaler의 해상도 정책을 유지하되, Camera UI는 자기 카메라의 픽셀 영역을 기준으로 계산합니다.
[ExecuteAlways]
[AddComponentMenu("Layout/Viewport Canvas Scaler")]
public sealed class ViewportCanvasScaler : CanvasScaler
{
    private Canvas targetCanvas;
    protected override void HandleScaleWithScreenSize()
    {
        if (targetCanvas == null) targetCanvas = GetComponent<Canvas>();
        if (targetCanvas == null || targetCanvas.renderMode != RenderMode.ScreenSpaceCamera || targetCanvas.worldCamera == null)
        { base.HandleScaleWithScreenSize(); return; }
        Vector2 pixels = targetCanvas.worldCamera.pixelRect.size;
        if (pixels.x <= 0 || pixels.y <= 0) return;
        float x = pixels.x / Mathf.Max(1, referenceResolution.x), y = pixels.y / Mathf.Max(1, referenceResolution.y);
        float scale = screenMatchMode == ScreenMatchMode.Expand ? Mathf.Min(x, y) :
            screenMatchMode == ScreenMatchMode.Shrink ? Mathf.Max(x, y) :
            SceneUILayout.CanvasScale(pixels, referenceResolution, matchWidthOrHeight);
        SetScaleFactor(scale);
        SetReferencePixelsPerUnit(referencePixelsPerUnit);
    }
}
