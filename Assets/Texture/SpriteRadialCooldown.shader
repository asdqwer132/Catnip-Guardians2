
Shader "Custom/URP/SpriteRadialCooldown"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Overlay Color", Color) = (0, 0, 0, 0.75)

        [Enum(Radial360,0,Horizontal,1,Vertical,2)]
        _FillMode ("Fill Mode", Float) = 0

        _FillAmount ("Fill Amount", Range(0, 1)) = 1

        [Toggle] _Clockwise ("Clockwise", Float) = 1
        _StartAngle ("Start Angle", Range(0, 360)) = 0

        _BoundsMin ("Bounds Min", Vector) = (-0.5,-0.5,0,0)
        _BoundsSize ("Bounds Size", Vector) = (1,1,0,0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _FillAmount;
                float _FillMode;
                float _Clockwise;
                float _StartAngle;
                float4 _BoundsMin;
                float4 _BoundsSize;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 localPos : TEXCOORD1;
                half4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                o.localPos = v.positionOS.xy;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 size = max(
                    _BoundsSize.xy,
                    float2(0.00001, 0.00001)
                );

                float2 uv = (i.localPos - _BoundsMin.xy) / size;

                float fill = saturate(_FillAmount);
                float progress = 0;

                // Radial 360
                if (_FillMode < 0.5)
                {
                    float2 dir = uv - 0.5;
                    float angle = atan2(dir.x, dir.y);

                    progress = frac(
                        angle / 6.28318530718
                        - _StartAngle / 360.0 + 1.0
                    );

                    if (_Clockwise < 0.5)
                        progress = frac(1.0 - progress);
                }
                // Horizontal: Left -> Right
                else if (_FillMode < 1.5)
                {
                    progress = uv.x;
                }
                // Vertical: Bottom -> Top
                else
                {
                    progress = uv.y;
                }

                // Fill 0 = hidden, Fill 1 = fully visible
                clip(fill - progress - 0.00001);

                half4 tex = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    i.uv
                );

                half4 col;
                col.rgb = _Color.rgb * i.color.rgb;
                col.a = tex.a * _Color.a * i.color.a;

                return col;
            }

            ENDHLSL
        }
    }
}
