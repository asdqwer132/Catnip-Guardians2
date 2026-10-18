using System.Collections.Generic;
using UnityEngine;

// 카메라/Canvas를 추가하지 않고 기존 카메라의 렌더링 옵션만 등록합니다.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public sealed class SceneCameraPresentation : MonoBehaviour
{
    public GameScreenBackgroundProfile background;
    public SceneViewShape shape;
    [Tooltip("Background Profile의 Reference Height 해상도에서 사용할 모서리 반지름(픽셀)입니다.")]
    [Min(0)] public float cornerRadius = 48;
    private Camera boundCamera;
    private static readonly Dictionary<Camera, SceneCameraPresentation> entries = new Dictionary<Camera, SceneCameraPresentation>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => entries.Clear();
    private void OnEnable() { boundCamera = GetComponent<Camera>(); entries[boundCamera] = this; }
    private void OnDisable() { if (boundCamera != null && entries.TryGetValue(boundCamera, out var current) && current == this) entries.Remove(boundCamera); }
    public static SceneCameraPresentation ForCamera(Camera camera)
        => camera != null && entries.TryGetValue(camera, out var value) && value != null && value.isActiveAndEnabled ? value : null;
    public Vector2 OutputSize
    {
        get
        {
            var camera = boundCamera != null ? boundCamera : GetComponent<Camera>();
            if (camera.targetTexture != null) return new Vector2(camera.targetTexture.width, camera.targetTexture.height);
            if (camera.targetDisplay > 0 && camera.targetDisplay < Display.displays.Length)
            { var display = Display.displays[camera.targetDisplay]; return new Vector2(display.renderingWidth, display.renderingHeight); }
            return new Vector2(Screen.width, Screen.height);
        }
    }
    public float RadiusInPixels => SceneScreenPresentationMath.CornerRadius(cornerRadius, OutputSize.y,
        background != null ? background.referenceHeight : 1440, (boundCamera != null ? boundCamera : GetComponent<Camera>()).pixelRect.size);
    public bool ContainsScreenPoint(Vector2 point)
        => SceneScreenPresentationMath.Contains((boundCamera != null ? boundCamera : GetComponent<Camera>()).pixelRect, point, shape, RadiusInPixels);
}
