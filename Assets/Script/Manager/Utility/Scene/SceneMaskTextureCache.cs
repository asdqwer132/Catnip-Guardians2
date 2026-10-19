using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

// 압축/읽기 전용 원본을 변경하지 않고 필요한 스프라이트 영역의 알파만 보관합니다.
public sealed class SceneMaskTextureCache : IDisposable
{
    public byte[] Pixels { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    private Texture2D source;
    private Rect region;
    private uint updateCount;
    private int version;
    private bool retry;

    public bool Prepare(Texture2D texture, Rect uvRegion)
    {
        if(source==texture && region==uvRegion && (texture==null || updateCount==texture.updateCount))
        {
            if(retry) { retry=false; Copy(false); }
            return Pixels!=null;
        }
        Clear(); source=texture; region=uvRegion;
        if(source==null || region.width<=0 || region.height<=0) return false;
        updateCount=source.updateCount;
        Width=Mathf.Max(1,Mathf.RoundToInt(source.width*region.width));
        Height=Mathf.Max(1,Mathf.RoundToInt(source.height*region.height));
        if(source.isReadable && !GraphicsFormatUtility.IsCompressedFormat(source.graphicsFormat))
        {
            try
            {
                var colors=source.GetPixels32();
                Pixels=new byte[Width*Height];
                for(int y=0;y<Height;y++) for(int x=0;x<Width;x++)
                {
                    int sx=Mathf.Clamp(Mathf.FloorToInt((region.x+(x+.5f)/Width*region.width)*source.width),0,source.width-1);
                    int sy=Mathf.Clamp(Mathf.FloorToInt((region.y+(y+.5f)/Height*region.height)*source.height),0,source.height-1);
                    Pixels[y*Width+x]=colors[sy*source.width+sx].a;
                }
                return true;
            }
            catch(UnityException) { /* CPU 디코딩 미지원 포맷은 아래 RGBA 버퍼를 사용합니다. */ }
        }
        Copy(true);
        return Pixels!=null;
    }
    private void Copy(bool allowAsync)
    {
        int requestVersion=version;
        var copy=RenderTexture.GetTemporary(Width,Height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
        bool queued=false;
        try
        {
            var previous=RenderTexture.active;
            try { Graphics.Blit(source,copy,region.size,region.min); }
            finally { RenderTexture.active=previous; }
            if(!allowAsync || !CanReadAsynchronously(copy)) { ReadCopy(copy); return; }
            AsyncGPUReadback.Request(copy,0,TextureFormat.RGBA32,request =>
            {
                try
                {
                    if(version!=requestVersion) return;
                    if(request.hasError) { retry=true; return; }
                    var colors=request.GetData<Color32>();
                    Pixels=new byte[colors.Length];
                    for(int i=0;i<colors.Length;i++) Pixels[i]=colors[i].a;
                }
                finally { RenderTexture.ReleaseTemporary(copy); }
            });
            queued=true;
        }
        finally { if(!queued) RenderTexture.ReleaseTemporary(copy); }
    }
    private static bool CanReadAsynchronously(RenderTexture copy)
    {
        if(!SystemInfo.supportsAsyncGPUReadback) return false;
#if UNITY_6000_0_OR_NEWER
        return SystemInfo.IsFormatSupported(copy.graphicsFormat,GraphicsFormatUsage.ReadPixels);
#else
        return SystemInfo.IsFormatSupported(copy.graphicsFormat,FormatUsage.ReadPixels);
#endif
    }
    private void ReadCopy(RenderTexture copy)
    {
        var previous=RenderTexture.active;
        Texture2D readable=null;
        try
        {
            RenderTexture.active=copy;
            readable=new Texture2D(Width,Height,TextureFormat.RGBA32,false,true);
            readable.ReadPixels(new Rect(0,0,Width,Height),0,0,false);
            var colors=readable.GetPixels32();
            Pixels=new byte[colors.Length];
            for(int i=0;i<colors.Length;i++) Pixels[i]=colors[i].a;
        }
        finally
        {
            RenderTexture.active=previous;
            if(readable!=null) { if(Application.isPlaying) UnityEngine.Object.Destroy(readable); else UnityEngine.Object.DestroyImmediate(readable); }
        }
    }
    public void Clear() { version++; source=null; Pixels=null; retry=false; Width=Height=0; }
    public void Dispose() => Clear();
}
