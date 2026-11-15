Shader "Hidden/Catnip/Scene Screen Presentation"
{
    Properties
    {
        _BackgroundTex("Background", 2D) = "white" {}
        _MaskTex("View Mask", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
        TEXTURE2D(_BackgroundTex); SAMPLER(sampler_BackgroundTex);
        TEXTURE2D(_MaskTex);
        float4 _BackgroundTint, _EmptyColor, _Viewport, _UvScale, _ViewShape;
        float4 _MaskScale, _MaskSettings;
        float4 _FrameMapX,_FrameMapY,_FrameDestBorder,_FrameSourceBorder;
        struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
        Varyings Vert(uint vertexID : SV_VertexID)
        {
            Varyings o;
            o.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
            o.uv = GetFullScreenTriangleTexCoord(vertexID);
            return o;
        }
        float2 BottomLeftUv(float2 uv)
        {
            #if UNITY_UV_STARTS_AT_TOP
            uv.y = 1 - uv.y;
            #endif
            return uv;
        }
        half3 Background(float2 localUv)
        {
            float2 globalUv = _Viewport.xy + BottomLeftUv(localUv) * _Viewport.zw;
            float2 uv = (globalUv - .5) * _UvScale.xy + .5;
            if (_UvScale.z > 2.5) uv = frac(globalUv * _UvScale.xy);
            if (_UvScale.z > .5 && _UvScale.z < 1.5 && (any(uv < 0) || any(uv > 1))) return _EmptyColor.rgb;
            return SAMPLE_TEXTURE2D(_BackgroundTex, sampler_BackgroundTex, uv).rgb * _BackgroundTint.rgb;
        }
        half4 FragBackground(Varyings i) : SV_Target { return half4(Background(i.uv),1); }
        float SliceUv(float value,float destMin,float destMax,float sourceMin,float sourceMax)
        {
            if(value<destMin && destMin>0) return value/destMin*sourceMin;
            if(value>1-destMax && destMax>0) return 1-(1-value)/destMax*sourceMax;
            return sourceMin+(value-destMin)/max(.000001,1-destMin-destMax)*(1-sourceMin-sourceMax);
        }
        half4 FragShape(Varyings i) : SV_Target
        {
            float2 halfSize = max(_ViewShape.xy * .5, 1);
            float2 p = (BottomLeftUv(i.uv) - .5) * _ViewShape.xy;
            float radius = min(_ViewShape.z, min(halfSize.x, halfSize.y));
            float2 q = abs(p) - halfSize + radius;
            float distance = length(max(q,0)) + min(max(q.x,q.y),0) - radius;
            if (_ViewShape.w > 1.5 && _ViewShape.w < 2.5) distance = (length(p / halfSize) - 1) * min(halfSize.x,halfSize.y);
            float aa = max(fwidth(distance),.5);
            float outside = smoothstep(-aa,aa,distance);
            if (_ViewShape.w > 2.5)
            {
                float2 uv=(BottomLeftUv(i.uv)-.5)*_MaskScale.xy+.5;
                float2 frameUv=uv;
                if(_ViewShape.w>3.5)
                {
                    float2 screenUv=_Viewport.xy+BottomLeftUv(i.uv)*_Viewport.zw;
                    frameUv=float2(dot(float3(screenUv,1),_FrameMapX.xyz),dot(float3(screenUv,1),_FrameMapY.xyz));
                    uv=float2(SliceUv(frameUv.x,_FrameDestBorder.x,_FrameDestBorder.z,_FrameSourceBorder.x,_FrameSourceBorder.z),
                        SliceUv(frameUv.y,_FrameDestBorder.y,_FrameDestBorder.w,_FrameSourceBorder.y,_FrameSourceBorder.w));
                }
                float alpha=SAMPLE_TEXTURE2D_LOD(_MaskTex,sampler_LinearClamp,uv,0).a;
                if(_MaskSettings.z>.5) alpha=1-alpha;
                float visible=smoothstep(max(0,_MaskSettings.x-_MaskSettings.y),min(1,_MaskSettings.x+_MaskSettings.y),alpha);
                if(any(uv<0) || any(uv>1) || any(frameUv<0) || any(frameUv>1)) visible=0;
                // 이미지를 지정하기 전에는 기존 사각형 화면을 유지합니다.
                outside=_MaskSettings.w>.5?1-visible:(_ViewShape.w>3.5?1:0);
            }
            clip(outside - .001);
            return half4(Background(i.uv),outside);
        }
        ENDHLSL
        Pass
        {
            Name "Shared Background"
            Blend One Zero
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragBackground
            ENDHLSL
        }
        Pass
        {
            Name "Scene View Mask"
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragShape
            ENDHLSL
        }
    }
}
