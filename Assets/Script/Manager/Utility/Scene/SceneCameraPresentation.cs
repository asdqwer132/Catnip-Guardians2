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
    [Header("이미지 마스크")]
    [Tooltip("Image Mask에서 사용합니다. 알파가 있는 PNG의 불투명한 부분만 화면에 남깁니다.")]
    public Texture2D maskImage;
    public SceneMaskFit maskFit;
    [Range(0,1)] public float maskAlphaThreshold = .5f;
    [Range(0,.5f)] public float maskEdgeSoftness = .01f;
    public bool invertMask;
    private Camera boundCamera;
    private readonly SceneMaskTextureCache maskCache=new SceneMaskTextureCache();
    public SceneViewFrame UIFrame { get; private set; }
    public SceneViewShape EffectiveShape => UIFrame!=null && UIFrame.CanMask ? SceneViewShape.UIFrame :
        shape==SceneViewShape.UIFrame?SceneViewShape.Rectangle:shape;
    public void BindFrame(SceneViewFrame frame) => UIFrame=frame;
    public void UnbindFrame(SceneViewFrame frame) { if(UIFrame==frame) UIFrame=null; }
    private static readonly Dictionary<Camera, SceneCameraPresentation> entries = new Dictionary<Camera, SceneCameraPresentation>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => entries.Clear();
    private void OnEnable() { boundCamera = GetComponent<Camera>(); entries[boundCamera] = this; }
    private void OnDisable()
    {
        if (boundCamera != null && entries.TryGetValue(boundCamera, out var current) && current == this) entries.Remove(boundCamera);
        maskCache.Clear();
    }
    private void Update() { if(EffectiveShape==SceneViewShape.ImageMask) PrepareMask(); }
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
    {
        Rect rect=(boundCamera != null ? boundCamera : GetComponent<Camera>()).pixelRect;
        if(!SceneScreenPresentationMath.Contains(rect,point,EffectiveShape,RadiusInPixels)) return false;
        if(EffectiveShape==SceneViewShape.UIFrame) return UIFrame.ContainsWindowPoint(point,this);
        if(shape!=SceneViewShape.ImageMask || maskImage==null) return true;
        PrepareMask();
        Vector2 scale=SceneScreenPresentationMath.MaskUvScale(new Vector2(maskImage.width,maskImage.height),rect.size,maskFit);
        if(!SceneScreenPresentationMath.TryMaskUv(new Vector2((point.x-rect.x)/rect.width,(point.y-rect.y)/rect.height),scale,out var uv)) return false;
        // 비동기 복사가 끝나기 전에는 아직 확인되지 않은 영역으로 입력을 보내지 않습니다.
        if(maskCache.Pixels==null) return false;
        float alpha=SceneScreenPresentationMath.SampleMaskAlpha(maskCache.Pixels,maskCache.Width,maskCache.Height,uv);
        if(invertMask) alpha=1-alpha;
        return alpha>0 && alpha>=Mathf.Clamp01(maskAlphaThreshold);
    }
    private void PrepareMask() => maskCache.Prepare(maskImage,new Rect(0,0,1,1));
}
