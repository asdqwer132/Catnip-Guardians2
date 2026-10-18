using UnityEngine;

public enum ScreenBackgroundFit { Cover, Contain, Stretch, Tile }
public enum SceneViewShape { Rectangle, RoundedRectangle, Ellipse, ImageMask }
public enum SceneMaskFit { Stretch, Cover, Contain }

public static class SceneScreenPresentationMath
{
    public static Vector2 MaskUvScale(Vector2 texture, Vector2 viewport, SceneMaskFit fit)
        => BackgroundUvScale(texture, viewport, fit == SceneMaskFit.Cover ? ScreenBackgroundFit.Cover :
            fit == SceneMaskFit.Contain ? ScreenBackgroundFit.Contain : ScreenBackgroundFit.Stretch);
    public static bool TryMaskUv(Vector2 viewportUv, Vector2 scale, out Vector2 uv)
    {
        uv = Vector2.Scale(viewportUv - new Vector2(.5f,.5f),scale) + new Vector2(.5f,.5f);
        return uv.x>=0 && uv.x<=1 && uv.y>=0 && uv.y<=1;
    }
    // 셰이더와 같은 mip 0 / Linear Clamp 샘플입니다. 이미지의 Read/Write 설정에 의존하지 않습니다.
    public static float SampleMaskAlpha(byte[] pixels, int width, int height, Vector2 uv)
    {
        if(pixels==null || width<=0 || height<=0 || pixels.Length!=(long)width*height) return 0;
        float x=Mathf.Clamp01(uv.x)*width-.5f, y=Mathf.Clamp01(uv.y)*height-.5f;
        int x0=Mathf.FloorToInt(x), y0=Mathf.FloorToInt(y);
        int a=Mathf.Clamp(x0,0,width-1), b=Mathf.Clamp(x0+1,0,width-1);
        int c=Mathf.Clamp(y0,0,height-1)*width, d=Mathf.Clamp(y0+1,0,height-1)*width;
        return Mathf.Lerp(Mathf.Lerp(pixels[c+a],pixels[c+b],x-x0),
            Mathf.Lerp(pixels[d+a],pixels[d+b],x-x0),y-y0)/255f;
    }
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
