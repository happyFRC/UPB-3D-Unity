Shader "Custom/ScreenSpaceLine" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _PixelWidth ("Pixel Width", Float) = 2.0
    }

    SubShader {
        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass {
            Name "ScreenSpaceLine"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                // 顶点法线：指向线段的“侧面”方向
                float3 normalOS : NORMAL;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _PixelWidth;
            CBUFFER_END

            Varyings vert(Attributes input) {
                Varyings output;

                // 顶点在世界空间的位置
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                // 侧面方向（世界空间）
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                // 转到裁剪空间
                float4 positionCS = TransformWorldToHClip(positionWS);

                // 把侧面方向转到裁剪空间（近似：只取 xy 分量）
                float3 normalCS = TransformWorldToHClipDir(normalWS);

                // 屏幕空间偏移：让 _PixelWidth 像素对应到裁剪空间
                float2 offset = normalize(normalCS.xy) * _PixelWidth * 2.0 / _ScreenParams.xy;

                // 应用偏移，乘 w 保证透视正确
                positionCS.xy += offset * positionCS.w;

                output.positionCS = positionCS;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                return input.color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}