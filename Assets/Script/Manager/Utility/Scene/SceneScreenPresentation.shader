Shader "Hidden/Catnip/Scene Screen Presentation"
{
    Properties { _BackgroundTex("Background", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BackgroundTex); SAMPLER(sampler_BackgroundTex);
        float4 _BackgroundTint, _EmptyColor, _Viewport, _UvScale, _ViewShape;
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
        half4 FragShape(Varyings i) : SV_Target
        {
            float2 halfSize = max(_ViewShape.xy * .5, 1);
            float2 p = (BottomLeftUv(i.uv) - .5) * _ViewShape.xy;
            float radius = min(_ViewShape.z, min(halfSize.x, halfSize.y));
            float2 q = abs(p) - halfSize + radius;
            float distance = length(max(q,0)) + min(max(q.x,q.y),0) - radius;
            if (_ViewShape.w > 1.5) distance = (length(p / halfSize) - 1) * min(halfSize.x,halfSize.y);
            float aa = max(fwidth(distance),.5);
            float outside = smoothstep(-aa,aa,distance);
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
            Name "Rounded View"
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragShape
            ENDHLSL
        }
    }
}
