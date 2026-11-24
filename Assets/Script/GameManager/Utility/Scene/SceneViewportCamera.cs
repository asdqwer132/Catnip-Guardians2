using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 월드는 전체 RT, UI는 독립 표시 카메라. PixelPerfect는 분할 Rect를 건드리지 않습니다.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(-8500)]
public sealed class SceneViewportCamera : MonoBehaviour
{
    public GameScreenLayoutProfile profile;
    public GameScreenRegion region;
    public bool previewLayout = true;
    [Tooltip("UI 출력용 URP Renderer. -1이면 프로젝트 기본 Renderer를 사용합니다.")]
    public int uiRendererIndex = -1;
    public SceneViewportSurface surface;
    public Canvas[] canvases = System.Array.Empty<Canvas>();
    public Camera DisplayCamera => display;
    public Rect ScreenRect => display != null ? display.pixelRect : World.pixelRect;
    private Camera world, display;
    private RenderTexture texture, previousTarget;
    private Rect previousRect;
    private float previousOrtho;
    private bool previousEnabled;
    private bool initialized, composed;
    private Rect composition;
    private struct CanvasState { public Canvas canvas; public RenderMode mode; public Camera camera; public float distance; }
    private readonly List<CanvasState> states = new List<CanvasState>();
    private PixelPerfectCamera pixel;
    private int oldPPU, oldX, oldY;
    private PixelPerfectCamera.CropFrame oldCrop;
    private PixelPerfectCamera.GridSnapping oldSnap;
    private bool capturedPixel;
    private static readonly Dictionary<Camera, SceneViewportCamera> entries = new Dictionary<Camera, SceneViewportCamera>();
    public Camera World => world != null ? world : (world = GetComponent<Camera>());
    public static SceneViewportCamera For(Camera camera) => camera != null && entries.TryGetValue(camera,out var value) ? value : null;
    public void SetComposition(Rect viewport)
    {
        if(!initialized) Apply();
        composed=true;composition=viewport;
        World.enabled=viewport.width>0 && viewport.height>0;
        Apply();
    }
    public void ClearComposition() { composed=false;if(initialized) World.enabled=previousEnabled;Apply(); }
    private void OnEnable()
    {
        entries[World] = this; Canvas.preWillRenderCanvases += Apply;
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += BeforeSave;
#endif
        Apply();
    }
#if UNITY_EDITOR
    private void BeforeSave(UnityEngine.SceneManagement.Scene scene,string path)
    { if(scene==gameObject.scene) { Restore();UnityEditor.EditorApplication.delayCall += ResumePreview; } }
    private void ResumePreview() { if(this!=null && isActiveAndEnabled) Apply(); }
#endif
    private void LateUpdate() => Apply();
    public void Apply()
    {
        if (!isActiveAndEnabled || profile == null || surface == null) return;
        if (!initialized)
        {
            initialized = true; previousEnabled=World.enabled; previousTarget = World.targetTexture; previousRect = World.rect; previousOrtho = World.orthographicSize;
            var go = new GameObject("Viewport UI Camera",typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(transform,false);
            display = go.GetComponent<Camera>();
            display.orthographic = true; display.nearClipPlane = .01f; display.farClipPlane = 1f;
            display.clearFlags = CameraClearFlags.Depth; display.cullingMask = World.cullingMask;
            display.tag="Untagged";
            display.allowHDR = false; display.allowMSAA = false;
            display.GetUniversalAdditionalCameraData().SetRenderer(uiRendererIndex);
            foreach(var canvas in canvases)
            {
                if(canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
                states.Add(new CanvasState { canvas=canvas,mode=canvas.renderMode,camera=canvas.worldCamera,distance=canvas.planeDistance });
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = display; canvas.planeDistance = .5f;
                var scaler=canvas.GetComponent<ViewportCanvasScaler>();
                if(scaler != null && profile.uiScale != null) scaler.sharedScaleProfile=profile.uiScale;
            }
        }
        var presentation=World.GetComponent<SceneCameraPresentation>();
        if(presentation != null)
        {
            var outputView=display.GetComponent<SceneCameraPresentation>();
            if(outputView==null) outputView=display.gameObject.AddComponent<SceneCameraPresentation>();
            outputView.background=presentation.background;outputView.shape=presentation.shape;outputView.cornerRadius=presentation.cornerRadius;
            outputView.maskImage=presentation.maskImage;outputView.maskFit=presentation.maskFit;outputView.maskAlphaThreshold=presentation.maskAlphaThreshold;
            outputView.maskEdgeSoftness=presentation.maskEdgeSoftness;outputView.invertMask=presentation.invertMask;
        }
        Vector2 output = new Vector2(Screen.width,Screen.height);
        if(World.targetDisplay > 0 && World.targetDisplay < Display.displays.Length)
        { var d=Display.displays[World.targetDisplay]; output=new Vector2(d.renderingWidth,d.renderingHeight); }
        Rect viewport = composed ? composition : previewLayout ? profile.Region(region,output) : new Rect(0,0,1,1);
        bool visible=viewport.width>0 && viewport.height>0;
        display.enabled=World.enabled && visible; surface.enabled=visible;
        if(!visible || output.x<=0 || output.y<=0) return;
        display.depth=World.depth+1; display.targetDisplay=World.targetDisplay;
        display.cullingMask=World.cullingMask; display.orthographicSize=1;
        var size=profile.RenderSize(viewport,output);
        Rect displayRect=profile.DisplayRegion(viewport,output,size);
        if(display.rect!=displayRect) display.rect=displayRect;
        if(texture==null || texture.width!=size.x || texture.height!=size.y)
        {
            World.targetTexture=null; surface.texture=null; ReleaseTexture();
            texture=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32)
            { name="Scene viewport "+region,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave };
            texture.Create(); World.targetTexture=texture; surface.texture=texture;
        }
        Rect fullTexture=new Rect(0,0,1,1);
        if(World.rect!=fullTexture) World.rect=fullTexture;
        pixel=World.GetComponent<PixelPerfectCamera>();
        if(pixel != null && pixel.enabled)
        {
            if(!capturedPixel) { capturedPixel=true;oldPPU=pixel.assetsPPU;oldX=pixel.refResolutionX;oldY=pixel.refResolutionY;oldCrop=pixel.cropFrame;oldSnap=pixel.gridSnapping; }
            pixel.assetsPPU=Mathf.Max(1,profile.worldPixelsPerUnit);pixel.refResolutionX=size.x;pixel.refResolutionY=size.y;
            pixel.cropFrame=PixelPerfectCamera.CropFrame.None;pixel.gridSnapping=PixelPerfectCamera.GridSnapping.PixelSnapping;
        }
        World.orthographicSize=size.y/(2f*Mathf.Max(1,profile.worldPixelsPerUnit));
    }
    private void ReleaseTexture() { if(texture==null)return;texture.Release();DestroyOwned(texture);texture=null; }
    private static void DestroyOwned(Object value) { if(Application.isPlaying) Destroy(value);else DestroyImmediate(value); }
    private void OnDisable()
    {
        Canvas.preWillRenderCanvases -= Apply; entries.Remove(World);
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaving-=BeforeSave;
#endif
        Restore();
    }
    private void Restore()
    {
        if(!initialized) return;
        foreach(var state in states) if(state.canvas != null) { state.canvas.renderMode=state.mode;state.canvas.worldCamera=state.camera;state.canvas.planeDistance=state.distance; }
        states.Clear(); if(surface!=null) surface.texture=null;World.targetTexture=previousTarget;World.rect=previousRect;World.orthographicSize=previousOrtho;World.enabled=previousEnabled;
        if(capturedPixel && pixel != null) { pixel.assetsPPU=oldPPU;pixel.refResolutionX=oldX;pixel.refResolutionY=oldY;pixel.cropFrame=oldCrop;pixel.gridSnapping=oldSnap; }
        capturedPixel=false; ReleaseTexture(); if(display!=null) DestroyOwned(display.gameObject);display=null;initialized=false;
    }
    public bool IsReady => initialized && texture != null && display != null;
    public Vector2 ToRenderPoint(Vector2 screen) => Vector2.Scale((screen-ScreenRect.min)/ScreenRect.size,new Vector2(texture.width,texture.height));
    public Vector3 ToScreenPoint(Vector3 render)
    { Vector2 p=ScreenRect.min+Vector2.Scale((Vector2)render/new Vector2(texture.width,texture.height),ScreenRect.size);return new Vector3(p.x,p.y,render.z); }
}
