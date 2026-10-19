using UnityEngine;

public enum ScreenBackgroundFit { Cover, Contain, Stretch, Tile }
public enum SceneViewShape { Rectangle, RoundedRectangle, Ellipse, ImageMask, UIFrame }
public enum SceneMaskFit { Stretch, Cover, Contain }

public static class SceneScreenPresentationMath
{
    // 바깥과 이어진 투명 영역만 제외하므로 원/별/여러 렌즈의 닫힌 내부를 모두 찾습니다.
    public static byte[] FillFrameWindows(byte[] alpha,int width,int height,float threshold)
    {
        if(alpha==null || width<=0 || height<=0 || alpha.Length!=(long)width*height) return null;
        byte cutoff=(byte)Mathf.Clamp(Mathf.CeilToInt(threshold*255),1,255);
        var outside=new bool[alpha.Length]; var queue=new int[alpha.Length]; int head=0,tail=0;
        void Visit(int p) { if(!outside[p] && alpha[p]<cutoff) { outside[p]=true; queue[tail++]=p; } }
        for(int x=0;x<width;x++) { Visit(x); Visit((height-1)*width+x); }
        for(int y=0;y<height;y++) { Visit(y*width); Visit(y*width+width-1); }
        while(head<tail)
        {
            int p=queue[head++],x=p%width;
            if(x>0) Visit(p-1); if(x+1<width) Visit(p+1);
            if(p>=width) Visit(p-width); if(p+width<alpha.Length) Visit(p+width);
        }
        var filled=new byte[alpha.Length];
        for(int i=0;i<filled.Length;i++) filled[i]=outside[i]?(byte)0:(byte)255;
        return filled;
    }
    public static Rect PreserveFrameAspect(Rect rect,Vector2 spriteSize,Vector2 pivot)
    {
        if(rect.width<=0 || rect.height<=0 || spriteSize.x<=0 || spriteSize.y<=0) return rect;
        float aspect=spriteSize.x/spriteSize.y;
        if(aspect>rect.width/rect.height) { float h=rect.width/aspect; rect.y+=(rect.height-h)*pivot.y; rect.height=h; }
        else { float w=rect.height*aspect; rect.x+=(rect.width-w)*pivot.x; rect.width=w; }
        return rect;
    }
    public static bool FrameScreenMap(Vector2 origin,Vector2 right,Vector2 top,out Vector4 rowX,out Vector4 rowY)
    {
        Vector2 a=right-origin,b=top-origin;
        float det=a.x*b.y-a.y*b.x;
        rowX=rowY=Vector4.zero;
        if(Mathf.Abs(det)<.00000001f) return false;
        rowX=new Vector4(b.y/det,-b.x/det,(-b.y*origin.x+b.x*origin.y)/det,0);
        rowY=new Vector4(-a.y/det,a.x/det,(a.y*origin.x-a.x*origin.y)/det,0);
        return true;
    }
    public static Vector2 FrameUv(Vector2 screen,Vector4 rowX,Vector4 rowY)
        => new Vector2(rowX.x*screen.x+rowX.y*screen.y+rowX.z,rowY.x*screen.x+rowY.y*screen.y+rowY.z);
    public static float SliceCoordinate(float value,float destMin,float destMax,float sourceMin,float sourceMax)
    {
        if(value<destMin && destMin>0) return value/destMin*sourceMin;
        if(value>1-destMax && destMax>0) return 1-(1-value)/destMax*sourceMax;
        return sourceMin+(value-destMin)/Mathf.Max(.000001f,1-destMin-destMax)*(1-sourceMin-sourceMax);
    }
    public static Vector2 SlicedFrameUv(Vector2 uv,Vector4 dest,Vector4 source)
        => new Vector2(SliceCoordinate(uv.x,dest.x,dest.z,source.x,source.z),SliceCoordinate(uv.y,dest.y,dest.w,source.y,source.w));
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
