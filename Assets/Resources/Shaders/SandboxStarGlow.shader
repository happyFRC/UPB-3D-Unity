// 恒星“黑体光球”渲染模式：球体本身发出黑体辐射色，中心微微泛白、边缘带一点光晕。
Shader "UPB/SandboxStarGlow"
{
    Properties
    {
        _Color ("Blackbody Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Float) = 1.35
        _WhiteCore ("White Core", Range(0,1)) = 0.38
        _EdgeGlow ("Edge Glow", Range(0,2)) = 0.45
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
            Name "StarGlow"
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
                float _Intensity;
                float _WhiteCore;
                float _EdgeGlow;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                float ndotv = saturate(dot(n, viewDir));

                half3 baseCol = _Color.rgb;

                // 正对镜头的中心区域微微泛白（不是纯白，留一点星体本色）。
                half3 core = lerp(baseCol, half3(1.0, 1.0, 1.0), pow(ndotv, 5.0) * _WhiteCore);

                // 边缘略微提亮，看起来像球体自己在发光。
                half3 glow = 1.0 + _EdgeGlow * pow(1.0 - ndotv, 3.0);

                half3 color = core * glow * _Intensity;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
