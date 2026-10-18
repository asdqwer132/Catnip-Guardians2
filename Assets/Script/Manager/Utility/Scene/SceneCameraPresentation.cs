using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
    private Texture2D sampledMask;
    private byte[] maskPixels;
    private int maskWidth, maskHeight, maskVersion;
    private uint sampledUpdateCount;
    private bool retryReadback;
    private static readonly Dictionary<Camera, SceneCameraPresentation> entries = new Dictionary<Camera, SceneCameraPresentation>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => entries.Clear();
    private void OnEnable() { boundCamera = GetComponent<Camera>(); entries[boundCamera] = this; }
    private void OnDisable()
    {
        if (boundCamera != null && entries.TryGetValue(boundCamera, out var current) && current == this) entries.Remove(boundCamera);
        maskVersion++; sampledMask=null; maskPixels=null; retryReadback=false;
    }
    private void Update() { if(shape==SceneViewShape.ImageMask) PrepareMask(); }
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
        if(!SceneScreenPresentationMath.Contains(rect,point,shape,RadiusInPixels)) return false;
        if(shape!=SceneViewShape.ImageMask || maskImage==null) return true;
        PrepareMask();
        Vector2 scale=SceneScreenPresentationMath.MaskUvScale(new Vector2(maskImage.width,maskImage.height),rect.size,maskFit);
        if(!SceneScreenPresentationMath.TryMaskUv(new Vector2((point.x-rect.x)/rect.width,(point.y-rect.y)/rect.height),scale,out var uv)) return false;
        // 비동기 복사가 끝나기 전에는 아직 확인되지 않은 영역으로 입력을 보내지 않습니다.
        if(maskPixels==null) return false;
        float alpha=SceneScreenPresentationMath.SampleMaskAlpha(maskPixels,maskWidth,maskHeight,uv);
        if(invertMask) alpha=1-alpha;
        return alpha>0 && alpha>=Mathf.Clamp01(maskAlphaThreshold);
    }
    private void PrepareMask()
    {
        if(sampledMask==maskImage && (maskImage==null || sampledUpdateCount==maskImage.updateCount))
        {
            if(retryReadback) { retryReadback=false; ReadMaskSynchronously(); }
            return;
        }
        sampledMask=maskImage; maskPixels=null; retryReadback=false;
        int version=++maskVersion;
        if(sampledMask==null) return;
        sampledUpdateCount=sampledMask.updateCount;
        maskWidth=sampledMask.width; maskHeight=sampledMask.height;
        if(sampledMask.isReadable)
        {
            try { CacheAlpha(sampledMask.GetPixels32()); return; }
            catch(UnityException) { /* CPU 디코딩을 지원하지 않는 압축 포맷은 GPU에서 복사합니다. */ }
        }
        if(!SystemInfo.supportsAsyncGPUReadback) { ReadMaskSynchronously(); return; }
        // 한 번만 CPU로 복사한 뒤 클릭은 배열로 판정합니다. 원본의 Read/Write 변경은 필요 없습니다.
        AsyncGPUReadback.Request(sampledMask,0,TextureFormat.RGBA32,request =>
        {
            if(this==null || !isActiveAndEnabled || version!=maskVersion || sampledMask!=maskImage) return;
            if(request.hasError) { retryReadback=true; return; }
            var pixels=request.GetData<Color32>();
            maskPixels=new byte[pixels.Length];
            for(int i=0;i<pixels.Length;i++) maskPixels[i]=pixels[i].a;
        });
    }
    private void CacheAlpha(Color32[] pixels)
    {
        maskPixels=new byte[pixels.Length];
        for(int i=0;i<pixels.Length;i++) maskPixels[i]=pixels[i].a;
    }
    private void ReadMaskSynchronously()
    {
        // 비동기 전송을 지원하지 않는 기기에서만 마스크 변경 시 한 번 복사합니다.
        RenderTexture previous=RenderTexture.active;
        RenderTexture copy=RenderTexture.GetTemporary(maskWidth,maskHeight,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
        Texture2D readable=null;
        try
        {
            Graphics.Blit(sampledMask,copy);
            RenderTexture.active=copy;
            readable=new Texture2D(maskWidth,maskHeight,TextureFormat.RGBA32,false,true);
            readable.ReadPixels(new Rect(0,0,maskWidth,maskHeight),0,0,false);
            CacheAlpha(readable.GetPixels32());
        }
        finally
        {
            RenderTexture.active=previous; RenderTexture.ReleaseTemporary(copy);
            if(readable!=null) { if(Application.isPlaying) Destroy(readable); else DestroyImmediate(readable); }
        }
    }
}
