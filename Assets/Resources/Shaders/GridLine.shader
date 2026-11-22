// 沙盘网格线：顶点色 × 材质色，并按“相机到原点的距离 × 倍数”做圆盘裁切。
// 圆盘外不绘制，边缘用 smoothstep 柔和淡出，避免硬切边。
Shader "UPB/GridLine"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "GridLine"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half4 _Color;

            // 由 InfiniteGrid 每帧写入的全局参数（世界坐标 / Unity 单位）。
            float4 _GridClipCenter;
            float _GridClipRadius;
            float _GridClipFade;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);

                o.positionWS = positionWS;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 到网格原点的水平距离：超出半径直接丢弃，靠近边缘时平滑淡出。
                float dist = length(i.positionWS.xz - _GridClipCenter.xz);
                float edge = 1.0 - smoothstep(_GridClipRadius - _GridClipFade,
                    _GridClipRadius, dist);

                half4 color = i.color * _Color;
                color.a *= edge;
                clip(color.a - 0.001);
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
