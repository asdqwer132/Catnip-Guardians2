using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;
using UnityEngine.SceneManagement;

// 사용자가 만든 Image에 붙입니다. UI/Canvas/카메라는 생성하지 않습니다.
[ExecuteAlways,DisallowMultipleComponent,RequireComponent(typeof(Image))]
[AddComponentMenu("UI/Scene View Frame")]
public sealed class SceneViewFrame : MonoBehaviour
{
    [Header("프레임 안에 보여줄 카메라")]
    public Camera targetCamera;
    [Tooltip("직접 카메라를 연결하지 않을 때 사용할 씬입니다. 비우면 이 UI가 속한 씬을 사용합니다.")]
    public string targetScene;
    public SceneCameraRole targetRole=SceneCameraRole.Content;
    [Header("중앙이 투명한 닫힌 프레임")]
    [Tooltip("Simple / Sliced Image를 지원합니다. Sliced의 Sprite Border와 Pixels Per Unit Multiplier를 그대로 따릅니다.")]
    [Range(.01f,1)] public float frameAlphaThreshold=.5f;
    private Image image;
    private SceneCameraPresentation boundView;
    private readonly SceneMaskTextureCache cache=new SceneMaskTextureCache();
    private byte[] sourcePixels,filledPixels;
    private float builtThreshold;
    public Texture2D WindowMask { get; private set; }
    private Image FrameImage => image!=null?image:(image=GetComponent<Image>());
    public bool CanMask => isActiveAndEnabled && FrameImage.isActiveAndEnabled && FrameImage.overrideSprite!=null &&
        (FrameImage.type==Image.Type.Simple || FrameImage.type==Image.Type.Sliced) && FrameImage.canvas!=null && FrameImage.canvas.isActiveAndEnabled &&
        FrameImage.canvas.rootCanvas.renderMode!=RenderMode.WorldSpace;
    private void Reset()
    {
        image=GetComponent<Image>();
        if(image!=null) { image.preserveAspect=true; image.raycastTarget=false; }
    }
    private void OnEnable() { Canvas.preWillRenderCanvases+=Refresh; Refresh(); }
    private void LateUpdate() => Refresh();
    private void OnDisable()
    {
        Canvas.preWillRenderCanvases-=Refresh;
        if(boundView!=null) boundView.UnbindFrame(this);
        boundView=null; cache.Clear(); sourcePixels=filledPixels=null; ReleaseMask();
    }
    private void Refresh()
    {
        Camera camera=targetCamera;
        if(camera==null)
        {
            Scene scene=string.IsNullOrWhiteSpace(targetScene)?gameObject.scene:SceneManager.GetSceneByName(targetScene);
            var binding=SceneCameraBinding.InScene(scene,targetRole);
            if(binding!=null) camera=binding.Camera;
        }
        var view=camera!=null?camera.GetComponent<SceneCameraPresentation>():null;
        if(view!=boundView)
        {
            if(boundView!=null) boundView.UnbindFrame(this);
            boundView=view;
            if(boundView!=null) boundView.BindFrame(this);
        }
        if(CanMask) PrepareWindow();
    }
    private void PrepareWindow()
    {
        var sprite=FrameImage.overrideSprite;
        Vector4 outer=DataUtility.GetOuterUV(sprite);
        if(!cache.Prepare(sprite.texture,Rect.MinMaxRect(outer.x,outer.y,outer.z,outer.w)))
        { sourcePixels=filledPixels=null; ReleaseMask(); return; }
        float threshold=Mathf.Clamp(frameAlphaThreshold,.01f,1);
        if(sourcePixels==cache.Pixels && WindowMask!=null && builtThreshold==threshold) return;
        sourcePixels=cache.Pixels; builtThreshold=threshold;
        filledPixels=SceneScreenPresentationMath.FillFrameWindows(sourcePixels,cache.Width,cache.Height,threshold);
        ReleaseMask();
        WindowMask=new Texture2D(cache.Width,cache.Height,TextureFormat.RGBA32,false,true)
        { name="Scene frame window",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave };
        var colors=new Color32[filledPixels.Length];
        for(int i=0;i<colors.Length;i++) colors[i]=new Color32(255,255,255,filledPixels[i]);
        WindowMask.SetPixels32(colors); WindowMask.Apply(false,true);
    }
    private void ReleaseMask()
    {
        if(WindowMask!=null) { if(Application.isPlaying) Destroy(WindowMask); else DestroyImmediate(WindowMask); }
        WindowMask=null;
    }
    public struct Geometry
    {
        public Vector4 rowX,rowY,destBorder,sourceBorder;
        public Vector2 Uv(Vector2 screen) => SceneScreenPresentationMath.FrameUv(screen,rowX,rowY);
        public Vector2 SpriteUv(Vector2 uv) => SceneScreenPresentationMath.SlicedFrameUv(uv,destBorder,sourceBorder);
    }
    public bool TryGeometry(SceneCameraPresentation view,out Geometry geometry)
    {
        geometry=default;
        if(!CanMask || view==null) return false;
        var canvas=FrameImage.canvas.rootCanvas;
        if(canvas.renderMode==RenderMode.WorldSpace) return false;
        Camera uiCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        var camera=view.GetComponent<Camera>();
        if((canvas.renderMode!=RenderMode.ScreenSpaceOverlay && uiCamera==null) || canvas.targetDisplay!=camera.targetDisplay ||
            (uiCamera!=null?uiCamera.targetTexture:null)!=camera.targetTexture) return false;
        var sprite=FrameImage.overrideSprite;
        Rect rect=FrameImage.GetPixelAdjustedRect();
        Vector4 padding=DataUtility.GetPadding(sprite);
        bool sliced=FrameImage.type==Image.Type.Sliced && sprite.border!=Vector4.zero;
        if(sliced)
        {
            float ppu=Mathf.Max(.0001f,FrameImage.pixelsPerUnit*FrameImage.pixelsPerUnitMultiplier);
            Vector4 border=sprite.border/ppu;
            Rect original=FrameImage.rectTransform.rect;
            for(int axis=0;axis<2;axis++)
            {
                float ratio=original.size[axis]>0?rect.size[axis]/original.size[axis]:1;
                border[axis]*=ratio; border[axis+2]*=ratio;
                float sum=border[axis]+border[axis+2];
                if(sum>rect.size[axis] && sum>0) { float factor=rect.size[axis]/sum; border[axis]*=factor; border[axis+2]*=factor; }
            }
            padding/=ppu;
            rect=Rect.MinMaxRect(rect.xMin+padding.x,rect.yMin+padding.y,rect.xMax-padding.z,rect.yMax-padding.w);
            if(rect.width<=0 || rect.height<=0) return false;
            geometry.destBorder=new Vector4(Mathf.Max(0,border.x-padding.x)/rect.width,Mathf.Max(0,border.y-padding.y)/rect.height,
                Mathf.Max(0,border.z-padding.z)/rect.width,Mathf.Max(0,border.w-padding.w)/rect.height);
            Vector4 outer=DataUtility.GetOuterUV(sprite),inner=DataUtility.GetInnerUV(sprite);
            if(outer.z<=outer.x || outer.w<=outer.y) return false;
            geometry.sourceBorder=new Vector4((inner.x-outer.x)/(outer.z-outer.x),(inner.y-outer.y)/(outer.w-outer.y),
                (outer.z-inner.z)/(outer.z-outer.x),(outer.w-inner.w)/(outer.w-outer.y));
        }
        else
        {
            if(FrameImage.type==Image.Type.Simple && FrameImage.preserveAspect)
                rect=SceneScreenPresentationMath.PreserveFrameAspect(rect,sprite.rect.size,FrameImage.rectTransform.pivot);
            rect=Rect.MinMaxRect(rect.xMin+rect.width*padding.x/sprite.rect.width,rect.yMin+rect.height*padding.y/sprite.rect.height,
                rect.xMax-rect.width*padding.z/sprite.rect.width,rect.yMax-rect.height*padding.w/sprite.rect.height);
        }
        if(rect.width<=0 || rect.height<=0) return false;
        Vector2 output=view.OutputSize;
        if(output.x<=0 || output.y<=0) return false;
        Vector2 Project(float x,float y)
        {
            Vector2 screen=RectTransformUtility.WorldToScreenPoint(uiCamera,FrameImage.rectTransform.TransformPoint(new Vector3(x,y,0)));
            return new Vector2(screen.x/output.x,screen.y/output.y);
        }
        return SceneScreenPresentationMath.FrameScreenMap(Project(rect.xMin,rect.yMin),Project(rect.xMax,rect.yMin),Project(rect.xMin,rect.yMax),
            out geometry.rowX,out geometry.rowY);
    }
    public bool ContainsWindowPoint(Vector2 screen,SceneCameraPresentation view)
    {
        if(!CanMask) return false;
        PrepareWindow();
        if(WindowMask==null || !TryGeometry(view,out var g)) return false;
        Vector2 uv=g.Uv(new Vector2(screen.x/view.OutputSize.x,screen.y/view.OutputSize.y));
        if(uv.x<0 || uv.y<0 || uv.x>1 || uv.y>1) return false;
        Vector2 sample=g.SpriteUv(uv);
        bool clearCenter=FrameImage.type==Image.Type.Sliced && !FrameImage.fillCenter &&
            uv.x>=g.destBorder.x && uv.x<=1-g.destBorder.z && uv.y>=g.destBorder.y && uv.y<=1-g.destBorder.w;
        return SceneScreenPresentationMath.SampleMaskAlpha(filledPixels,cache.Width,cache.Height,sample)>=.5f &&
            (clearCenter || SceneScreenPresentationMath.SampleMaskAlpha(sourcePixels,cache.Width,cache.Height,sample)<builtThreshold);
    }
}
