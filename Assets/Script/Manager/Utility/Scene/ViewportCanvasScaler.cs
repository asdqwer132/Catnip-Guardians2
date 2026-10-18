using UnityEngine;
using UnityEngine.UI;

// 공통 프로필이 있으면 출력 화면 전체를 기준으로 UI 크기를 통일합니다.
// 프로필이 없는 기존 사용처는 카메라 영역 기준 계산을 유지합니다.
[ExecuteAlways]
[AddComponentMenu("Layout/Viewport Canvas Scaler")]
public sealed class ViewportCanvasScaler : CanvasScaler
{
    [Tooltip("같은 프로필을 연결하면 Overlay/Camera와 Viewport 비율에 관계없이 UI 픽셀 배율과 Reference PPU를 공유합니다.")]
    public SceneUIScaleProfile sharedScaleProfile;
    private Canvas targetCanvas;
    protected override void Handle()
    {
        if (targetCanvas == null) targetCanvas = GetComponent<Canvas>();
        if (sharedScaleProfile == null || targetCanvas == null || !targetCanvas.isRootCanvas ||
            targetCanvas.renderMode == RenderMode.WorldSpace)
        { base.Handle(); return; }
        // 카메라의 pixelRect는 UI가 놓일 영역이며, 공통 배율의 해상도가 아닙니다.
        Vector2 pixels = new Vector2(Screen.width, Screen.height);
        var camera = targetCanvas.renderMode == RenderMode.ScreenSpaceCamera ? targetCanvas.worldCamera : null;
        if (camera != null && camera.targetTexture != null)
            pixels = new Vector2(camera.targetTexture.width, camera.targetTexture.height);
        else if (targetCanvas.targetDisplay > 0 && targetCanvas.targetDisplay < Display.displays.Length)
        {
            var display = Display.displays[targetCanvas.targetDisplay];
            pixels = new Vector2(display.renderingWidth, display.renderingHeight);
        }
        if (pixels.x <= 0 || pixels.y <= 0) return;
        SetScaleFactor(sharedScaleProfile.CalculateScale(pixels));
        SetReferencePixelsPerUnit(sharedScaleProfile.ReferencePixelsPerUnit);
    }
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
