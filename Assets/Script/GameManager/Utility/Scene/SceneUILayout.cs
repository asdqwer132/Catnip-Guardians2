using UnityEngine;

// 크기 계산은 화면 픽셀과 분리합니다. 같은 정규화 영역을 UI와 월드 배치에 사용합니다.
public static class SceneUILayout
{
    private static readonly Vector3[] viewportCorners = new Vector3[4];

    // Layout regions live on a full-screen Overlay; content canvases keep following
    // their own cameras. Reading from a cropped camera canvas would feed its viewport
    // back into itself every frame.
    public static bool TryViewportRect(RectTransform area, Camera destination, out Rect viewport)
    {
        viewport = default;
        if (area == null || !area.gameObject.activeInHierarchy || area.rect.width <= 0 || area.rect.height <= 0)
            return false;
        var canvas = area.GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        canvas = canvas.rootCanvas;
        if (!canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.ScreenSpaceOverlay) return false;
        if (destination != null && (destination.targetDisplay != canvas.targetDisplay || destination.targetTexture != null))
            return false;

        Vector2 output = new Vector2(Screen.width, Screen.height);
        if (canvas.targetDisplay > 0)
        {
            if (canvas.targetDisplay >= Display.displays.Length) return false;
            var display = Display.displays[canvas.targetDisplay];
            output = new Vector2(display.renderingWidth, display.renderingHeight);
        }
        if (output.x <= 0 || output.y <= 0) return false;
        area.GetWorldCorners(viewportCorners);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var corner in viewportCorners)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, corner);
            if (!float.IsFinite(point.x) || !float.IsFinite(point.y)) return false;
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        min = new Vector2(Mathf.Clamp01(min.x / output.x), Mathf.Clamp01(min.y / output.y));
        max = new Vector2(Mathf.Clamp01(max.x / output.x), Mathf.Clamp01(max.y / output.y));
        if (max.x <= min.x || max.y <= min.y) return false;
        viewport = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    public static void ItemRegions(Vector2 gameShare, float lowerSidebarShare,
        out Rect game, out Rect series, out Rect materials, out Rect recipe)
    {
        float x = SafeShare(gameShare.x, .8f), y = SafeShare(gameShare.y, .8f);
        float lower = SafeShare(lowerSidebarShare, .5f) * y;
        game = new Rect(0, 0, x, y);
        series = new Rect(0, y, 1, 1 - y);
        materials = Rect.MinMaxRect(x, lower, 1, y);
        recipe = Rect.MinMaxRect(x, 0, 1, lower);
    }
    public static float SafeShare(float value, float fallback)
        => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, .05f, .95f);

    public static void SetRect(RectTransform target, Rect normalized)
    {
        if (target == null) return;
        if (target.anchorMin != normalized.min) target.anchorMin = normalized.min;
        if (target.anchorMax != normalized.max) target.anchorMax = normalized.max;
        if (target.offsetMin != Vector2.zero) target.offsetMin = Vector2.zero;
        if (target.offsetMax != Vector2.zero) target.offsetMax = Vector2.zero;
    }

    public static float CanvasScale(Vector2 pixels, Vector2 reference, float match)
    {
        float x = pixels.x / Mathf.Max(1, reference.x), y = pixels.y / Mathf.Max(1, reference.y);
        return Mathf.Pow(Mathf.Max(.0001f, x), 1f - Mathf.Clamp01(match)) *
            Mathf.Pow(Mathf.Max(.0001f, y), Mathf.Clamp01(match));
    }
}
