using UnityEngine;
using UnityEngine.UI;

// 공통 프로필이 있으면 출력 화면 전체를 기준으로 UI 크기를 통일합니다.
// 프로필이 없는 기존 사용처는 카메라 영역 기준 계산을 유지합니다.
[ExecuteAlways]
[AddComponentMenu("Layout/Viewport Canvas Scaler")]
public sealed class ViewportCanvasScaler : CanvasScaler, ICanvasRaycastFilter
{
    [Tooltip("같은 프로필을 연결하면 Overlay/Camera와 Viewport 비율에 관계없이 UI 픽셀 배율과 Reference PPU를 공유합니다.")]
    public SceneUIScaleProfile sharedScaleProfile;
    private Canvas targetCanvas;
    private Vector2 previousPixels;
    private Rect previousViewport;
    private Camera previousCamera;
    private float previousScale;
    private bool layoutInitialized;
    public bool IsRaycastLocationValid(Vector2 screenPoint,Camera eventCamera)
    {
        if(targetCanvas==null) targetCanvas=GetComponent<Canvas>();
        if(targetCanvas==null || targetCanvas.renderMode!=RenderMode.ScreenSpaceCamera) return true;
        var view=SceneCameraPresentation.ForCamera(targetCanvas.worldCamera);
        return view==null || view.ContainsScreenPoint(screenPoint);
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        Canvas.preWillRenderCanvases += Handle;
    }
    protected override void OnDisable()
    {
        Canvas.preWillRenderCanvases -= Handle;
        layoutInitialized = false;
        base.OnDisable();
    }
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
        float scale = sharedScaleProfile.CalculateScale(pixels);
        SetScaleFactor(scale);
        SetReferencePixelsPerUnit(sharedScaleProfile.ReferencePixelsPerUnit);
        Rect viewport = camera != null ? camera.pixelRect : new Rect(Vector2.zero, pixels);
        if (!layoutInitialized || pixels != previousPixels || viewport != previousViewport || camera != previousCamera || !Mathf.Approximately(scale, previousScale))
        {
            layoutInitialized = true; previousPixels = pixels; previousViewport = viewport; previousCamera = camera; previousScale = scale;
            // 높이가 같은 채로 가로만 바뀌면 배율은 같아도 카메라 Canvas의 가로 크기는 달라집니다.
            if (camera != null) { camera.ResetAspect(); targetCanvas.worldCamera = camera; }
            var root = targetCanvas.transform as RectTransform;
            if (root != null)
            {
                if (camera != null)
                {
                    root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewport.width / scale);
                    root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewport.height / scale);
                }
                root.ForceUpdateRectTransforms();
                LayoutRebuilder.MarkLayoutForRebuild(root);
            }
        }
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
