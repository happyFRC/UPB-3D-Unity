// 彩蛋长方体（Coke_Car / HydrogenBox）用的着色器：
// 按面的朝向选贴图：+Z 正面、±X 侧面、±Y 与 -Z 用 TopBackBottom。
Shader "UPB/EasterEggBox"
{
    Properties
    {
        _FrontTex ("Front", 2D) = "white" {}
        _SideTex ("Sides", 2D) = "white" {}
        _TopTex ("Top/Back/Bottom", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "EasterEggBox"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            TEXTURE2D(_FrontTex);     SAMPLER(sampler_FrontTex);
            TEXTURE2D(_SideTex);      SAMPLER(sampler_SideTex);
            TEXTURE2D(_TopTex);       SAMPLER(sampler_TopTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.normalOS = v.normalOS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalOS);
                float3 an = abs(n);

                half3 col;
                if (an.y > an.x && an.y > an.z)
                {
                    col = SAMPLE_TEXTURE2D(_TopTex, sampler_TopTex, i.uv).rgb;      // 顶 / 底
                }
                else if (an.x > an.z)
                {
                    col = SAMPLE_TEXTURE2D(_SideTex, sampler_SideTex, i.uv).rgb;    // 左右
                }
                else if (n.z > 0.0)
                {
                    col = SAMPLE_TEXTURE2D(_FrontTex, sampler_FrontTex, i.uv).rgb;  // 正面
                }
                else
                {
                    col = SAMPLE_TEXTURE2D(_TopTex, sampler_TopTex, i.uv).rgb;      // 背面
                }

                // 简单方向光，让长方体有体积感（和沙盘兜底光方向一致）。
                float3 L = normalize(float3(-0.45, 0.65, -0.6));
                float ndl = saturate(dot(normalize(i.normalWS), L));
                col *= (0.55 + 0.45 * ndl);

                return half4(col * _Color.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
