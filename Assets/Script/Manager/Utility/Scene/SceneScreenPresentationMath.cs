using UnityEngine;

public enum ScreenBackgroundFit { Cover, Contain, Stretch, Tile }
public enum SceneViewShape { Rectangle, RoundedRectangle, Ellipse }

public static class SceneScreenPresentationMath
{
    public static Vector2 BackgroundUvScale(Vector2 texture, Vector2 output, ScreenBackgroundFit fit)
    {
        if (texture.x <= 0 || texture.y <= 0 || output.x <= 0 || output.y <= 0) return Vector2.one;
        float relativeAspect = (output.x / output.y) / (texture.x / texture.y);
        if (fit == ScreenBackgroundFit.Cover) return new Vector2(Mathf.Min(1, relativeAspect), Mathf.Min(1, 1 / relativeAspect));
        if (fit == ScreenBackgroundFit.Contain) return new Vector2(Mathf.Max(1, relativeAspect), Mathf.Max(1, 1 / relativeAspect));
        return Vector2.one;
    }
    public static float CornerRadius(float referencePixels, float outputHeight, float referenceHeight, Vector2 viewportPixels)
    {
        if (float.IsNaN(referencePixels) || float.IsInfinity(referencePixels)) return 0;
        return Mathf.Clamp(referencePixels * outputHeight / Mathf.Max(1, referenceHeight), 0,
            Mathf.Max(0, Mathf.Min(viewportPixels.x, viewportPixels.y) * .5f));
    }
    public static bool Contains(Rect pixels, Vector2 point, SceneViewShape shape, float radius)
    {
        if (pixels.width <= 0 || pixels.height <= 0 || !pixels.Contains(point)) return false;
        Vector2 half = pixels.size * .5f, p = point - pixels.center;
        if (shape == SceneViewShape.Ellipse)
            return p.x * p.x / (half.x * half.x) + p.y * p.y / (half.y * half.y) <= 1;
        if (shape != SceneViewShape.RoundedRectangle) return true;
        radius = Mathf.Clamp(radius, 0, Mathf.Min(half.x, half.y));
        Vector2 q = new Vector2(Mathf.Max(0, Mathf.Abs(p.x) - half.x + radius), Mathf.Max(0, Mathf.Abs(p.y) - half.y + radius));
        return q.sqrMagnitude <= radius * radius;
    }
}
