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
        shapePass = new PresentationPass("Rounded scene view", 1,
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
        if (view.shape != SceneViewShape.Rectangle) { shapePass.Setup(material); renderer.EnqueuePass(shapePass); }
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
            public Texture texture; public Color tint, empty;
            public Vector4 viewport, uvScale, shape;
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
                data.shape = new Vector4(pixels.width, pixels.height, view.RadiusInPixels, (int)view.shape);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PassData d, RasterGraphContext context) =>
                {
                    properties.Clear();
                    properties.SetTexture("_BackgroundTex", d.texture);
                    properties.SetColor("_BackgroundTint", d.tint); properties.SetColor("_EmptyColor", d.empty);
                    properties.SetVector("_Viewport", d.viewport); properties.SetVector("_UvScale", d.uvScale); properties.SetVector("_ViewShape", d.shape);
                    context.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3, 1, properties);
                });
            }
        }
    }
}
#endif
