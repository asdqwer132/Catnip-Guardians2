using UnityEngine;

// 크기 계산은 화면 픽셀과 분리합니다. 같은 정규화 영역을 UI와 월드 배치에 사용합니다.
public static class SceneUILayout
{
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
