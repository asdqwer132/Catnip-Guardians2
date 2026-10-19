#if UNITY_6000_0_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// URP 17 RenderGraph 전용입니다. 호환 모드의 오래된 Execute/Blit API를 사용하지 않습니다.
public sealed class SceneScreenPresentationFeature : ScriptableRendererFeature
{
    public Shader presentationShader;
    private Material material;
    private PresentationPass backgroundPass, shapePass;
    public override void Create()
    {
        CoreUtils.Destroy(material);
        if (presentationShader != null) material = CoreUtils.CreateEngineMaterial(presentationShader);
        backgroundPass = new PresentationPass("Shared screen background", 0,
            RenderPassEvent.BeforeRenderingTransparents, RenderPassEvent2D.BeforeRenderingSprites);
        shapePass = new PresentationPass("Scene view mask", 1,
            RenderPassEvent.BeforeRenderingPostProcessing, RenderPassEvent2D.BeforeRenderingPostProcessing);
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var view = SceneCameraPresentation.ForCamera(renderingData.cameraData.camera);
        if (material == null || view == null || view.background == null) return;
        // Renderer2D의 Clear 이후, 첫 스프라이트 배치 이전에 한 번만 배경을 그립니다.
        var layers = SortingLayer.layers;
        if (layers.Length == 0) return;
        backgroundPass.renderPassSortingLayerID = layers[0].id;
        backgroundPass.Setup(material); renderer.EnqueuePass(backgroundPass);
        if (view.EffectiveShape != SceneViewShape.Rectangle) { shapePass.Setup(material); renderer.EnqueuePass(shapePass); }
    }
    protected override void Dispose(bool disposing) => CoreUtils.Destroy(material);

    private sealed class PresentationPass : ScriptableRenderPass2D
    {
        private readonly int shaderPass;
        private Material material;
        private readonly string label;
        private static readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        private sealed class PassData
        {
            public Material material; public int pass;
            public Texture texture, mask; public Color tint, empty;
            public Vector4 viewport, uvScale, shape, maskScale, maskSettings;
            public Vector4 frameX,frameY,frameDest,frameSource;
        }
        public PresentationPass(string label, int shaderPass, RenderPassEvent injection, RenderPassEvent2D injection2D)
        {
            this.label = label; this.shaderPass = shaderPass; renderPassEvent = injection;
            renderPassEvent2D = injection2D;
            // 각 카메라의 크기에 맞는 중간 색상 버퍼를 URP가 관리합니다. 해상도 변경 시 별도 RT 캐시가 남지 않습니다.
            requiresIntermediateTexture = true;
        }
        public void Setup(Material value) => material = value;
        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var view = SceneCameraPresentation.ForCamera(cameraData.camera);
            if (view == null || view.background == null || material == null) return;
            var resources = frameData.Get<UniversalResourceData>();
            var profile = view.background;
            using (var builder = graph.AddRasterRenderPass<PassData>(label, out var data))
            {
                data.material = material; data.pass = shaderPass;
                data.texture = profile.backgroundTexture != null ? profile.backgroundTexture : Texture2D.whiteTexture;
                data.tint = profile.backgroundTexture != null ? profile.tint : profile.emptyColor;
                data.empty = profile.emptyColor;
                Rect rect = cameraData.camera.rect;
                data.viewport = new Vector4(rect.x, rect.y, rect.width, rect.height);
                Vector2 scale = SceneScreenPresentationMath.BackgroundUvScale(new Vector2(data.texture.width, data.texture.height), view.OutputSize, profile.fit);
                if (profile.fit == ScreenBackgroundFit.Tile) scale = profile.tileCount;
                data.uvScale = new Vector4(scale.x, scale.y, (int)profile.fit, 0);
                Rect pixels = cameraData.camera.pixelRect;
                data.shape = new Vector4(pixels.width, pixels.height, view.RadiusInPixels, (int)view.EffectiveShape);
                data.mask=view.maskImage!=null?view.maskImage:Texture2D.whiteTexture;
                Vector2 maskScale=SceneScreenPresentationMath.MaskUvScale(new Vector2(data.mask.width,data.mask.height),pixels.size,view.maskFit);
                data.maskScale=new Vector4(maskScale.x,maskScale.y,0,0);
                data.maskSettings=new Vector4(Mathf.Clamp01(view.maskAlphaThreshold),Mathf.Max(.0001f,view.maskEdgeSoftness),
                    view.invertMask?1:0,view.maskImage!=null?1:0);
                if(view.EffectiveShape==SceneViewShape.UIFrame)
                {
                    bool ready=view.UIFrame.TryGeometry(view,out var geometry) && view.UIFrame.WindowMask!=null;
                    data.mask=view.UIFrame.WindowMask!=null?view.UIFrame.WindowMask:Texture2D.whiteTexture;
                    data.frameX=geometry.rowX; data.frameY=geometry.rowY;
                    data.frameDest=geometry.destBorder; data.frameSource=geometry.sourceBorder;
                    data.maskSettings=new Vector4(.5f,Mathf.Max(.0001f,view.maskEdgeSoftness),0,ready?1:0);
                }
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                {
                    properties.Clear();
                    properties.SetTexture("_BackgroundTex", d.texture);
                    properties.SetColor("_BackgroundTint", d.tint); properties.SetColor("_EmptyColor", d.empty);
                    properties.SetVector("_Viewport", d.viewport); properties.SetVector("_UvScale", d.uvScale); properties.SetVector("_ViewShape", d.shape);
                    properties.SetTexture("_MaskTex",d.mask); properties.SetVector("_MaskScale",d.maskScale); properties.SetVector("_MaskSettings",d.maskSettings);
                    properties.SetVector("_FrameMapX",d.frameX); properties.SetVector("_FrameMapY",d.frameY);
                    properties.SetVector("_FrameDestBorder",d.frameDest); properties.SetVector("_FrameSourceBorder",d.frameSource);
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3, 1, properties);
                });
            }
        }
    }
}
#endif
