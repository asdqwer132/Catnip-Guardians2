using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Rendering;

public class ResponsiveSceneLayoutTests
{
    [Test] public void NonReadableDxt5MaskUsesAnUncompressedCopyAndPreservesTheActiveRenderTarget()
    {
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null || !SystemInfo.SupportsTextureFormat(TextureFormat.DXT5))
            Assert.Ignore("이 GPU 회귀 검사는 DXT5 텍스처를 지원하는 Unity 그래픽 장치에서 실행합니다.");
        var host=new GameObject("Compressed mask camera",typeof(Camera),typeof(SceneCameraPresentation));
        var mask=new Texture2D(16,16,TextureFormat.RGBA32,false);
        RenderTexture previous=RenderTexture.active;
        try
        {
            var pixels=new Color32[256];
            for(int y=0;y<16;y++) for(int x=0;x<16;x++) pixels[y*16+x]=new Color32(255,255,255,(byte)(x<8?0:255));
            mask.SetPixels32(pixels); mask.Apply(false,false); mask.Compress(true); mask.Apply(false,true);
            Assert.That(mask.format,Is.EqualTo(TextureFormat.DXT5));
            Assert.That(mask.isReadable,Is.False);
            var camera=host.GetComponent<Camera>(); camera.enabled=false;
            var presentation=host.GetComponent<SceneCameraPresentation>();
            presentation.shape=SceneViewShape.ImageMask; presentation.maskImage=mask;
            Vector2 hidden=camera.pixelRect.min+Vector2.Scale(camera.pixelRect.size,new Vector2(.25f,.5f));
            Vector2 visible=camera.pixelRect.min+Vector2.Scale(camera.pixelRect.size,new Vector2(.75f,.5f));
            presentation.ContainsScreenPoint(hidden);
            Assert.That(RenderTexture.active,Is.EqualTo(previous));
            if(SystemInfo.supportsAsyncGPUReadback) AsyncGPUReadback.WaitAllRequests();
            Assert.That(presentation.ContainsScreenPoint(hidden),Is.False);
            Assert.That(presentation.ContainsScreenPoint(visible),Is.True);
            Assert.That(RenderTexture.active,Is.EqualTo(previous));
        }
        finally
        {
            if(SystemInfo.supportsAsyncGPUReadback) AsyncGPUReadback.WaitAllRequests();
            RenderTexture.active=previous; Object.DestroyImmediate(host); Object.DestroyImmediate(mask);
        }
    }
    [Test] public void ImageMaskFiltersHiddenGameplayAndCanvasInputWithoutChangingCameraViewport()
    {
        var cameraHost=new GameObject("Masked camera",typeof(Camera),typeof(SceneCameraPresentation));
        var canvasHost=new GameObject("Existing UI",typeof(RectTransform),typeof(Canvas),typeof(ViewportCanvasScaler));
        var mask=new Texture2D(2,2,TextureFormat.RGBA32,false);
        try
        {
            var clear=new Color32(255,255,255,0); var solid=new Color32(0,0,0,255);
            mask.SetPixels32(new[] {clear,solid,clear,solid}); mask.Apply();
            var camera=cameraHost.GetComponent<Camera>(); camera.rect=new Rect(.1f,.2f,.6f,.7f);
            Rect viewport=camera.rect;
            var presentation=cameraHost.GetComponent<SceneCameraPresentation>();
            presentation.shape=SceneViewShape.ImageMask; presentation.maskImage=mask;
            var canvas=canvasHost.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera;
            var filter=canvasHost.GetComponent<ViewportCanvasScaler>();
            Vector2 hidden=camera.pixelRect.min+Vector2.Scale(camera.pixelRect.size,new Vector2(.25f,.5f));
            Vector2 visible=camera.pixelRect.min+Vector2.Scale(camera.pixelRect.size,new Vector2(.75f,.5f));
            Assert.That(presentation.ContainsScreenPoint(hidden),Is.False);
            Assert.That(filter.IsRaycastLocationValid(hidden,camera),Is.False);
            Assert.That(presentation.ContainsScreenPoint(visible),Is.True);
            Assert.That(filter.IsRaycastLocationValid(visible,camera),Is.True);
            presentation.invertMask=true;
            Assert.That(presentation.ContainsScreenPoint(hidden),Is.True);
            Assert.That(presentation.ContainsScreenPoint(visible),Is.False);
            presentation.invertMask=false; presentation.maskAlphaThreshold=0;
            Assert.That(presentation.ContainsScreenPoint(hidden),Is.False);
            presentation.maskAlphaThreshold=1;
            Assert.That(presentation.ContainsScreenPoint(visible),Is.True);
            presentation.maskImage=null;
            Assert.That(presentation.ContainsScreenPoint(visible),Is.True,"마스크를 지우면 사각형으로 돌아갑니다.");
            Assert.That(camera.rect,Is.EqualTo(viewport));
        }
        finally { Object.DestroyImmediate(canvasHost); Object.DestroyImmediate(cameraHost); Object.DestroyImmediate(mask); }
    }
    [Test] public void NavigationPaddingMatchesOverlayAndCameraWithoutChangingReservedHeight()
    {
        var host=new GameObject("General");
        var cameraHost=new GameObject("Navigation",typeof(Camera),typeof(SceneCameraBinding));
        var canvasHost=new GameObject("Existing UI",typeof(RectTransform),typeof(Canvas),typeof(ViewportCanvasScaler));
        var panelHost=new GameObject("Existing header",typeof(RectTransform));
        var profile=ScriptableObject.CreateInstance<SceneUIScaleProfile>();
        try
        {
            cameraHost.GetComponent<SceneCameraBinding>().role=SceneCameraRole.Navigation;
            var canvas=canvasHost.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvasHost.GetComponent<ViewportCanvasScaler>().sharedScaleProfile=profile;
            var area=panelHost.GetComponent<RectTransform>(); area.SetParent(canvasHost.transform,false);
            var general=host.AddComponent<GameGeneralSceneController>(); general.autoLoadChildren=general.loadCoreScene=false;
            general.navigationCanvas=canvas; general.navigationArea=area; general.navigationHeightRatio=.15f;
            general.navigationPadding=new SceneViewportPadding {left=20,right=40,top=12,bottom=24};
            Rect expected=general.navigationPadding.Inset(new Rect(0,.85f,1,.15f),new Vector2(Screen.width,Screen.height),profile.CalculateScale(new Vector2(Screen.width,Screen.height)));
            for(int i=0;i<3;i++)
            {
                general.ApplyLayout();
                Assert.That(general.NavigationTopInset,Is.EqualTo(.15f));
                Assert.That(area.anchorMin.x,Is.EqualTo(expected.xMin).Within(.00001f));
                Assert.That(area.anchorMin.y,Is.EqualTo(expected.yMin).Within(.00001f));
                Assert.That(area.anchorMax.x,Is.EqualTo(expected.xMax).Within(.00001f));
                Assert.That(area.anchorMax.y,Is.EqualTo(expected.yMax).Within(.00001f));
                Assert.That(cameraHost.GetComponent<Camera>().rect,Is.EqualTo(expected));
            }
        }
        finally { Object.DestroyImmediate(host); Object.DestroyImmediate(panelHost); Object.DestroyImmediate(canvasHost); Object.DestroyImmediate(cameraHost); Object.DestroyImmediate(profile); }
    }
    [Test] public void MeasuredHeaderPaddingDoesNotAccumulateOrReduceReservedSpace()
    {
        var host=new GameObject("General");
        var canvasHost=new GameObject("Existing UI",typeof(RectTransform),typeof(Canvas));
        var panelHost=new GameObject("Existing header",typeof(RectTransform));
        try
        {
            var canvas=canvasHost.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var area=panelHost.GetComponent<RectTransform>(); area.SetParent(canvasHost.transform,false);
            SceneUILayout.SetRect(area,new Rect(0,.8f,1,.2f));
            var general=host.AddComponent<GameGeneralSceneController>(); general.autoLoadChildren=general.loadCoreScene=false;
            general.navigationCanvas=canvas; general.navigationArea=area; general.navigationSizeMode=NavigationSizeMode.MeasuredPanel;
            Canvas.ForceUpdateCanvases(); general.ApplyLayout();
            float baseline=general.NavigationHeight;
            general.navigationPadding=new SceneViewportPadding {left=12,right=24,top=8,bottom=16};
            general.ApplyLayout(); Canvas.ForceUpdateCanvases();
            Vector2 min=area.offsetMin,max=area.offsetMax;
            for(int i=0;i<3;i++)
            {
                general.ApplyLayout(); Canvas.ForceUpdateCanvases();
                Assert.That(general.NavigationHeight,Is.EqualTo(baseline).Within(.00001f));
                Assert.That((area.offsetMin-min).magnitude,Is.LessThan(.01f));
                Assert.That((area.offsetMax-max).magnitude,Is.LessThan(.01f));
            }
            general.enabled=false;
            Assert.That(area.offsetMin.magnitude,Is.LessThan(.01f));
            Assert.That(area.offsetMax.magnitude,Is.LessThan(.01f));
        }
        finally { Object.DestroyImmediate(host); Object.DestroyImmediate(panelHost); Object.DestroyImmediate(canvasHost); }
    }
    [Test] public void CameraCanvasWidthTracksViewportResizeEvenWhenSharedScaleDoesNotChange()
    {
        var cameraHost = new GameObject("Item camera",typeof(Camera));
        var canvasHost = new GameObject("Existing item UI",typeof(RectTransform),typeof(Canvas),typeof(ViewportCanvasScaler));
        var target = new RenderTexture(1600,900,0);
        var profile = ScriptableObject.CreateInstance<SceneUIScaleProfile>();
        try
        {
            var camera = cameraHost.GetComponent<Camera>(); camera.targetTexture = target;
            var canvas = canvasHost.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
            var scaler = canvasHost.GetComponent<ViewportCanvasScaler>(); scaler.sharedScaleProfile = profile;
            profile.matchWidthOrHeight = 1;
            var handle = typeof(ViewportCanvasScaler).GetMethod("Handle",BindingFlags.NonPublic|BindingFlags.Instance);
            float previousScale = 0;
            foreach(float width in new[] {.4f,.7f,.25f,.4f})
            {
                camera.rect = new Rect(0,0,width,.85f);
                handle.Invoke(scaler,null); Canvas.ForceUpdateCanvases();
                if(previousScale>0) Assert.That(canvas.scaleFactor,Is.EqualTo(previousScale).Within(.00001f));
                previousScale = canvas.scaleFactor;
                var size = canvasHost.GetComponent<RectTransform>().rect.size;
                Assert.That(size.x*canvas.scaleFactor,Is.EqualTo(camera.pixelRect.width).Within(.05f));
                Assert.That(size.y*canvas.scaleFactor,Is.EqualTo(camera.pixelRect.height).Within(.05f));
            }
        }
        finally { Object.DestroyImmediate(canvasHost); Object.DestroyImmediate(cameraHost); Object.DestroyImmediate(target); Object.DestroyImmediate(profile); }
    }
    [Test] public void EqualFrameMultipliersProduceEqualScreenBordersInOverlayAndCameraCanvases()
    {
        var cameraHost = new GameObject("Item camera", typeof(Camera));
        var overlayHost = new GameObject("Existing overlay", typeof(RectTransform), typeof(Canvas), typeof(ViewportCanvasScaler));
        var itemHost = new GameObject("Existing item UI", typeof(RectTransform), typeof(Canvas), typeof(ViewportCanvasScaler));
        var overlayFrame = new GameObject("Overlay frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var itemFrame = new GameObject("Item frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var profile = ScriptableObject.CreateInstance<SceneUIScaleProfile>();
        var texture = new Texture2D(64,64);
        var sprite = Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect,new Vector4(18,18,18,18));
        try
        {
            var camera = cameraHost.GetComponent<Camera>(); camera.orthographic = true; camera.transform.position = new Vector3(0,0,-10);
            var overlay = overlayHost.GetComponent<Canvas>(); overlay.renderMode = RenderMode.ScreenSpaceOverlay;
            var item = itemHost.GetComponent<Canvas>(); item.renderMode = RenderMode.ScreenSpaceCamera; item.worldCamera = camera; item.planeDistance = 10;
            var overlayScaler = overlayHost.GetComponent<ViewportCanvasScaler>(); var itemScaler = itemHost.GetComponent<ViewportCanvasScaler>();
            overlayScaler.sharedScaleProfile = itemScaler.sharedScaleProfile = profile;
            // 서로 다른 과거 Inspector 값이 남아 있어도 공통 프로필이 우선되어야 합니다.
            overlayScaler.referenceResolution = new Vector2(2400,600); itemScaler.referenceResolution = new Vector2(960,1147.5f);
            overlayScaler.referencePixelsPerUnit = 100; itemScaler.referencePixelsPerUnit = 32;
            overlayFrame.transform.SetParent(overlay.transform,false); itemFrame.transform.SetParent(item.transform,false);
            var a = overlayFrame.GetComponent<Image>(); var b = itemFrame.GetComponent<Image>();
            a.sprite = b.sprite = sprite; a.type = b.type = Image.Type.Sliced;
            SceneUILayout.SetRect(a.rectTransform,new Rect(0,0,1,1)); SceneUILayout.SetRect(b.rectTransform,new Rect(0,0,1,1));
            var handle = typeof(ViewportCanvasScaler).GetMethod("Handle",BindingFlags.Instance|BindingFlags.NonPublic);
            foreach (var viewport in new[] {new Rect(.6f,0,.4f,.85f),new Rect(.75f,0,.25f,1),new Rect(0,0,1,1)})
            foreach (float multiplier in new[] {.5f,1f,2f})
            {
                camera.rect = viewport;
                a.pixelsPerUnitMultiplier = b.pixelsPerUnitMultiplier = multiplier;
                handle.Invoke(overlayScaler,null); handle.Invoke(itemScaler,null); Canvas.ForceUpdateCanvases();
                Assert.That(item.scaleFactor,Is.EqualTo(overlay.scaleFactor).Within(.00001f));
                Assert.That(item.referencePixelsPerUnit,Is.EqualTo(overlay.referencePixelsPerUnit));
                Assert.That(ScreenBorder(b,camera),Is.EqualTo(ScreenBorder(a,null)).Within(.05f));
                Assert.That(camera.rect,Is.EqualTo(viewport),"공통 배율 때문에 카메라 영역 비율을 변경하면 안 됩니다.");
            }
        }
        finally
        {
            Object.DestroyImmediate(overlayFrame); Object.DestroyImmediate(itemFrame); Object.DestroyImmediate(overlayHost);
            Object.DestroyImmediate(itemHost); Object.DestroyImmediate(cameraHost); Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture); Object.DestroyImmediate(profile);
        }
    }
    private static float ScreenBorder(Image image, Camera camera)
    {
        // 실제 Sliced 메시의 첫 두 X 경계를 화면으로 투영해 테두리 두께를 비교합니다.
        using (var vertices = new VertexHelper())
        {
            typeof(Image).GetMethod("OnPopulateMesh",BindingFlags.NonPublic|BindingFlags.Instance,null,new[] {typeof(VertexHelper)},null)
                .Invoke(image,new object[] {vertices});
            var xs = new List<float>();
            for (int i=0;i<vertices.currentVertCount;i++)
            {
                var vertex = new UIVertex(); vertices.PopulateUIVertex(ref vertex,i);
                float x = RectTransformUtility.WorldToScreenPoint(camera,image.rectTransform.TransformPoint(vertex.position)).x;
                if (!xs.Exists(value => Mathf.Abs(value-x)<.001f)) xs.Add(x);
            }
            xs.Sort(); Assert.That(xs.Count,Is.GreaterThanOrEqualTo(4));
            return xs[1]-xs[0];
        }
    }
    [Test] public void OverlayHeaderAndContentUseTheSameFifteenPercentBoundary()
    {
        var host = new GameObject("General ratio test");
        var canvasHost = new GameObject("Existing canvas", typeof(RectTransform), typeof(Canvas));
        var areaHost = new GameObject("Existing header", typeof(RectTransform));
        try
        {
            var canvas = canvasHost.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var area = areaHost.GetComponent<RectTransform>(); area.SetParent(canvasHost.transform, false);
            var general = host.AddComponent<GameGeneralSceneController>();
            general.autoLoadChildren = general.loadCoreScene = false;
            general.navigationCanvas = canvas; general.navigationArea = area;
            general.navigationSizeMode = NavigationSizeMode.ScreenRatio; general.navigationHeightRatio = .15f;
            // 이전 고정 높이/오프셋이 있어도 화면 비율 기준으로 연결되어야 합니다.
            area.sizeDelta = new Vector2(100, 202.5f); area.anchoredPosition = new Vector2(20, 10);
            general.ApplyLayout();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(area.anchorMin, Is.EqualTo(new Vector2(0, .85f)));
            Assert.That(area.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(area.offsetMin, Is.EqualTo(Vector2.zero)); Assert.That(area.offsetMax, Is.EqualTo(Vector2.zero));
            GameViewportLayout.Calculate(GameScreenLayout.SideBySide, .6f, true, 0, general.NavigationTopInset, out var battle, out var item);
            Assert.That(battle.yMax, Is.EqualTo(area.anchorMin.y).Within(.00001f));
            Assert.That(item.yMax, Is.EqualTo(area.anchorMin.y).Within(.00001f));
        }
        finally { Object.DestroyImmediate(host); Object.DestroyImmediate(areaHost); Object.DestroyImmediate(canvasHost); }
    }

    [Test] public void WorldBoxKeepsItsFrameOccupancyWhenCameraAspectAndFrameSizeChange()
    {
        var cameraHost = new GameObject("Item view", typeof(Camera));
        var canvasHost = new GameObject("Existing item UI", typeof(RectTransform), typeof(Canvas));
        var frameHost = new GameObject("Existing frame", typeof(RectTransform));
        var boxHost = new GameObject("Existing world box");
        var fitterHost = new GameObject("Box binding");
        try
        {
            var camera = cameraHost.GetComponent<Camera>(); camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
            var canvas = canvasHost.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 10;
            var frame = frameHost.GetComponent<RectTransform>(); frame.SetParent(canvasHost.transform, false);
            SceneUILayout.SetRect(frame, new Rect(0, 0, .8f, .8f));
            var fitter = fitterHost.AddComponent<UIWorldBoundsFitter>();
            fitter.frame = frame; fitter.worldCamera = camera; fitter.worldRoot = boxHost.transform;
            fitter.captureBoundsFromChildren = false;
            fitter.localBounds = new Bounds(new Vector3(.2f, -1, 0), new Vector3(6, 8, 0));
            fitter.frameOccupancy = new Rect(.06f, .06f, .88f, .88f);
            foreach (var viewport in new[] { new Rect(.6f, 0, .4f, .85f), new Rect(0, 0, 1, 1), new Rect(.2f, 0, .3f, 1) })
            {
                camera.rect = viewport;
                // 레거시 레이아웃 컨트롤러 없이 Canvas 갱신만으로 박스도 따라가야 합니다.
                Canvas.ForceUpdateCanvases();
                var corners = new Vector3[4]; frame.GetWorldCorners(corners);
                Vector2 frameMin = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 frameMax = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                Vector2 actualMin = camera.WorldToScreenPoint(boxHost.transform.TransformPoint(fitter.localBounds.min));
                Vector2 actualMax = camera.WorldToScreenPoint(boxHost.transform.TransformPoint(fitter.localBounds.max));
                Vector2 size = frameMax - frameMin;
                Assert.That((actualMin - frameMin - size * .06f).magnitude, Is.LessThan(.05f));
                Assert.That((actualMax - frameMin - size * .94f).magnitude, Is.LessThan(.05f));
                Assert.That(fitter.FitNow(), Is.False, "같은 레이아웃으로 다시 호출하면 누적 변형이 없어야 합니다.");
            }
        }
        finally
        {
            Object.DestroyImmediate(fitterHost); Object.DestroyImmediate(boxHost); Object.DestroyImmediate(frameHost);
            Object.DestroyImmediate(canvasHost); Object.DestroyImmediate(cameraHost);
        }
    }
}
