Shader "UPB/PlanetAtmosphere"
{
    Properties
    {
        _Color ("Color", Color) = (0.55,0.75,1,1)
        _Alpha0 ("Surface Alpha", Range(0,1)) = 0.5
        _PlanetRadius ("Planet Radius", Float) = 1
        _CenterWS ("Planet Center", Vector) = (0,0,0,0)
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
            Name "PlanetAtmosphere"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Alpha0;
                float _PlanetRadius;
                float4 _CenterWS;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 真实球壳：按视线到星球中心的最近距离判断大气高度。
                float3 rayDir = normalize(i.positionWS - _WorldSpaceCameraPos.xyz);
                float3 toCenter = _WorldSpaceCameraPos.xyz - _CenterWS.xyz;
                float t = -dot(toCenter, rayDir);
                float3 closest = _WorldSpaceCameraPos.xyz + rayDir * t;
                float r = distance(closest, _CenterWS.xyz) /
                          max(_PlanetRadius, 1e-6);

                // 大气壳：内外都发光。
                //   0.90R 处完全透明
                //   0.90R ~ 0.98R 线性升到表面 alpha 的 1/27（向内发光）
                //   0.98R ~ 1.00R 表面 alpha 的 1/9（球面内部整体再 ×1/3）
                //   1.00R ~ 1.05R 指数衰减：贴地猛降，尾部拖长，1.05R 处正好为 0
                //   1.05R 以外没有
                float alpha = 0.0;
                if (r < 0.90) {
                    discard;
                } else if (r < 0.98) {
                    alpha = _Alpha0 * (r - 0.90) / 0.08 * 0.0370370;
                } else if (r <= 1.00) {
                    alpha = _Alpha0 * 0.1111111;
                } else if (r < 1.05) {
                    // t: 0 = 地表, 1 = +0.05R 边界
                    // 指数型衰减：f(t) = (e^(-k t) - e^(-k)) / (1 - e^(-k))
                    //   f(0)=1、f(1)=0，起始最陡（贴地掉得极快），尾部拖得很长；
                    //   k 越大越极端（6 ≈ 1.01R 处只剩 30%，1.025R 处约 5%）。
                    const float k = 6.0;
                    float t = (r - 1.00) / 0.05;
                    float e0 = exp(-k);
                    float falloff = (exp(-k * t) - e0) / (1.0 - e0);
                    alpha = _Alpha0 * 0.3333333 * falloff;
                } else {
                    discard;
                }

                clip(alpha - 0.001);
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
