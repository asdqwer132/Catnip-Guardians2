
Shader "UI/GradientDarken"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Gradient Settings)]
        _Darkness ("Darkness", Range(0,1)) = 0.75
        _FadeHeight ("Fade Height", Range(0.001,1)) = 0.65
        _GradientCurve ("Gradient Curve", Range(0.2,4)) = 1
        _GradientStrength ("Gradient Strength", Range(1,8)) = 1
        _DarkColor ("Dark Color", Color) = (0,0,0,1)

        [Enum(BottomToTop,0,TopToBottom,1,LeftToRight,2,RightToLeft,3,CenterToOutside,4,OutsideToCenter,5)]
        _Direction ("Gradient Direction", Float) = 0

        [Header(Alpha Gradient)]
        [Toggle] _UseAlphaGradient ("Use Alpha Gradient", Float) = 0
        _AlphaStrength ("Alpha Strength", Range(0,1)) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                float4 localPosition : TEXCOORD1;
            };

            sampler2D _MainTex;

            fixed4 _Color;
            fixed4 _DarkColor;
            fixed4 _TextureSampleAdd;

            float _Darkness;
            float _FadeHeight;
            float _GradientCurve;
            float _GradientStrength;
            float _Direction;

            float _UseAlphaGradient;
            float _AlphaStrength;

            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;

                o.localPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;

                return o;
            }

            // 6방향 그라데이션 계산
            // 반환값 1 = 어두운 쪽
            // 반환값 0 = 밝은 쪽
            float CalculateGradient(float2 uv)
            {
                float distanceFromDark = 0.0;

                // 아래 -> 위
                if (_Direction < 0.5)
                {
                    distanceFromDark = uv.y;
                }
                // 위 -> 아래
                else if (_Direction < 1.5)
                {
                    distanceFromDark = 1.0 - uv.y;
                }
                // 왼쪽 -> 오른쪽
                else if (_Direction < 2.5)
                {
                    distanceFromDark = uv.x;
                }
                // 오른쪽 -> 왼쪽
                else if (_Direction < 3.5)
                {
                    distanceFromDark = 1.0 - uv.x;
                }
                // 원형
                else
                {
                    float radius = saturate(
                        length((uv - 0.5) * 2.0)
                    );

                    // 중심에서 바깥으로 밝아짐
                    if (_Direction < 4.5)
                    {
                        distanceFromDark = radius;
                    }
                    // 바깥에서 중심으로 밝아짐
                    else
                    {
                        distanceFromDark = 1.0 - radius;
                    }
                }

                // 그라데이션 기본 분포
                float t = saturate(
                    distanceFromDark / max(_FadeHeight, 0.001)
                );

                float gradient = 1.0 - t;

                // 그라데이션 중간톤 강도
                float exponent =
                    max(_GradientCurve, 0.001) /
                    max(_GradientStrength, 1.0);

                gradient = pow(
                    saturate(gradient),
                    exponent
                );

                return saturate(gradient);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col =
                    (tex2D(_MainTex, i.uv) + _TextureSampleAdd)
                    * i.color;

                float gradient = CalculateGradient(i.uv);

                // 1. 어두운 색 그라데이션
                col.rgb = lerp(
                    col.rgb,
                    _DarkColor.rgb,
                    saturate(gradient * _Darkness)
                );

                // 2. 투명 그라데이션
                if (_UseAlphaGradient > 0.5)
                {
                    float alphaFactor = 1.0 -
                        saturate(gradient * _AlphaStrength);

                    col.a *= alphaFactor;
                }

                // 3. UI RectMask2D 처리
                #ifdef UNITY_UI_CLIP_RECT

                col.a *= UnityGet2DClipping(
                    i.localPosition.xy,
                    _ClipRect
                );

                #endif

                // 4. UI Alpha Clip 처리
                #ifdef UNITY_UI_ALPHACLIP

                clip(col.a - 0.001);

                #endif

                return col;
            }

            ENDCG
        }
    }
}
