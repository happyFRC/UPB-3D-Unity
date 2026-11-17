Shader "UPB/StarPoint3D"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
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
            Name "StarPoint3D"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 直接用 UV 生成圆形光点，不依赖贴图 alpha，因此不会出现方形底。
                float2 p = i.uv * 2.0 - 1.0;
                float r = length(p);

                float disk = 1.0 - smoothstep(0.82, 1.0, r);
                // 更宽、更强的外圈光晕；配合加法混合，整体亮度会明显提高。
                float halo = pow(saturate(1.0 - r), 1.8) * 1.35;
                float alpha = saturate(disk + halo) * _Color.a;

                clip(alpha - 0.001);
                // 加法混合 + HDR，核心和光晕都大幅提亮。
                return half4(_Color.rgb * 3.0, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
