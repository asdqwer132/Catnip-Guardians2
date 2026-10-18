using NUnit.Framework;
using UnityEngine;

public class ResponsiveSceneLayoutTests
{
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
                Canvas.ForceUpdateCanvases(); fitter.FitNow();
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
