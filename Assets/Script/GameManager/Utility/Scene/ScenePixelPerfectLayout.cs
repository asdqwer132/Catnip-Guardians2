using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Pixel Perfect owns world snapping; the scene layout owns viewports and UI scale.
// Upscale RT/Crop also resize the UI render target, so cameras using this bridge
// use native Pixel Snapping with an integer zoom inside their assigned viewport.
public static class ScenePixelPerfectLayout
{
    private sealed class CameraState
    {
        public readonly Camera camera;
        public readonly HashSet<Object> owners = new HashSet<Object>();
        public Rect viewport;
        public bool Driving => driving;
        private PixelPerfectCamera pixelPerfect;
        private PixelPerfectCamera.CropFrame originalCrop;
        private PixelPerfectCamera.GridSnapping originalSnapping;
        private float originalSize;
        private bool driving;

        public CameraState(Camera camera) { this.camera = camera; viewport = camera.rect; }

        public void Apply()
        {
            if (camera == null) return;
            var current = camera.GetComponent<PixelPerfectCamera>();
            bool active = current != null && current.isActiveAndEnabled && camera.orthographic;
            if (driving && (current != pixelPerfect || !active)) Restore();
            if (!active) { viewport = camera.rect; return; }
            if (!driving)
            {
                pixelPerfect = current;
                originalCrop = current.cropFrame;
                originalSnapping = current.gridSnapping;
                originalSize = camera.orthographicSize;
                driving = true;
            }
            // Keep edits made in the Inspector so disabling this integration
            // restores the user's latest requested native mode.
            if (pixelPerfect.cropFrame != PixelPerfectCamera.CropFrame.None) originalCrop = pixelPerfect.cropFrame;
            if (pixelPerfect.gridSnapping != PixelPerfectCamera.GridSnapping.PixelSnapping) originalSnapping = pixelPerfect.gridSnapping;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            if (camera.rect != viewport) camera.rect = viewport;
            float size = OrthographicSize(camera.pixelWidth, camera.pixelHeight,
                pixelPerfect.assetsPPU, pixelPerfect.refResolutionX, pixelPerfect.refResolutionY);
            if (!Mathf.Approximately(camera.orthographicSize, size)) camera.orthographicSize = size;
        }

        public void Restore()
        {
            if (!driving) return;
            if (pixelPerfect != null)
            {
                pixelPerfect.cropFrame = originalCrop;
                pixelPerfect.gridSnapping = originalSnapping;
            }
            if (camera != null)
            {
                camera.rect = viewport;
                camera.orthographicSize = originalSize;
                camera.ResetWorldToCameraMatrix();
            }
            driving = false;
            pixelPerfect = null;
        }
    }

    private static readonly Dictionary<Camera, CameraState> states = new Dictionary<Camera, CameraState>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        foreach (var state in states.Values) state.Restore();
        states.Clear();
        Canvas.preWillRenderCanvases -= PrepareCanvases;
        RenderPipelineManager.beginContextRendering -= BeforeContextRendering;
        RenderPipelineManager.beginCameraRendering -= BeforeCameraRendering;
    }

    public static void Register(Camera camera, Object owner)
    {
        if (camera == null || owner == null) return;
        if (!states.TryGetValue(camera, out var state))
        {
            if (states.Count == 0)
            {
                Canvas.preWillRenderCanvases += PrepareCanvases;
                RenderPipelineManager.beginContextRendering += BeforeContextRendering;
            }
            states.Add(camera, state = new CameraState(camera));
        }
        state.owners.Add(owner);
        Prepare(camera);
    }

    public static void Unregister(Camera camera, Object owner)
    {
        if (ReferenceEquals(camera, null) || !states.TryGetValue(camera, out var state)) return;
        state.owners.Remove(owner);
        if (state.owners.Count > 0) return;
        state.Restore();
        states.Remove(camera);
        if (states.Count == 0)
        {
            Canvas.preWillRenderCanvases -= PrepareCanvases;
            RenderPipelineManager.beginContextRendering -= BeforeContextRendering;
            RenderPipelineManager.beginCameraRendering -= BeforeCameraRendering;
        }
    }

    public static void SetViewport(Camera camera, Rect viewport)
    {
        if (camera == null) return;
        if (states.TryGetValue(camera, out var state)) state.viewport = viewport;
        if (camera.rect != viewport) camera.rect = viewport;
        Prepare(camera);
    }

    public static void Prepare(Camera camera)
    {
        if (camera == null || !states.TryGetValue(camera, out var state)) return;
        state.Apply();
    }

    private static void PrepareCanvases()
    {
        foreach (var state in states.Values) state.Apply();
    }

    private static void BeforeContextRendering(ScriptableRenderContext context, List<Camera> cameras)
    {
        bool needed = false;
        foreach (var state in states.Values)
        {
            state.Apply();
            needed |= state.Driving;
        }
        // PixelPerfectCamera may have been enabled after this bridge. Order the
        // correction after its native callback once per render context, not on
        // every Canvas/layout update. Cameras without PPC need no render hook.
        RenderPipelineManager.beginCameraRendering -= BeforeCameraRendering;
        if (needed) RenderPipelineManager.beginCameraRendering += BeforeCameraRendering;
    }

    private static void BeforeCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (states.TryGetValue(camera, out var state)) state.Apply();
    }

    public static float OrthographicSize(int pixelWidth, int pixelHeight, int assetsPPU, int referenceWidth, int referenceHeight)
    {
        int zoom = Mathf.Max(1, Mathf.Min(Mathf.Max(1, pixelWidth) / Mathf.Max(1, referenceWidth),
            Mathf.Max(1, pixelHeight) / Mathf.Max(1, referenceHeight)));
        return Mathf.Max(1, pixelHeight) / (2f * Mathf.Max(1, assetsPPU) * zoom);
    }
}
