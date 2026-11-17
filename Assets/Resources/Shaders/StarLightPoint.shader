// 屏幕空间恒星“光点”shader。
// 中心：稍微增白、带一点点中心高光的黑体色圆盘。
// 外层：两层高斯光晕（亮内晕 + 宽外晕），模拟图二那种平滑、明亮的 bloom 感。
// 使用 Additive 混合；方形 Image 的四个角 alpha=0，不会出现方框。
Shader "UPB/StarLightPoint" {
    Properties {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _CoreRadius ("Core Radius", Range(0.03, 0.49)) = 0.1111
        _CoreSoftness ("Core Softness", Range(0.001, 0.2)) = 0.02
        _CoreWhiten ("Core Whiten", Range(0, 1)) = 0.5

        _HaloSigmaInner ("Halo Sigma Inner", Range(0.03, 0.3)) = 0.12
        _HaloAlphaInner ("Halo Alpha Inner", Range(0, 2)) = 0.9
        _HaloSigmaOuter ("Halo Sigma Outer", Range(0.1, 0.6)) = 0.35
        _HaloAlphaOuter ("Halo Alpha Outer", Range(0, 2)) = 0.35
        _InnerWhiten ("Inner Halo Whiten", Range(0, 1)) = 0.25
        _HaloEdgeFade ("Halo Edge Fade", Range(0.5, 1)) = 0.9

        _Brightness ("Brightness", Range(0, 4)) = 1.2

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader {
        Tags {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil {
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
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;

            float _CoreRadius;
            float _CoreSoftness;
            float _CoreWhiten;
            float _HaloSigmaInner;
            float _HaloAlphaInner;
            float _HaloSigmaOuter;
            float _HaloAlphaOuter;
            float _InnerWhiten;
            float _HaloEdgeFade;
            float _Brightness;

            v2f vert (appdata_t v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 p = i.texcoord - 0.5;
                float r = length(p) * 2.0;

                // 中心圆盘：柔和边缘，靠近中心再额外增白一点。
                float core = 1.0 - smoothstep(
                    _CoreRadius - _CoreSoftness,
                    _CoreRadius + _CoreSoftness,
                    r);
                float coreT = saturate(1.0 - r / max(_CoreRadius, 1e-4));

                float3 baseColor = i.color.rgb;
                float3 white = float3(1.0, 1.0, 1.0);
                float3 coreColor = lerp(baseColor, white, _CoreWhiten + 0.15 * coreT);

                // 两层光晕：亮内晕 + 宽外晕。
                float inner = exp(-(r * r) / (2.0 * _HaloSigmaInner * _HaloSigmaInner)) * _HaloAlphaInner;
                float outer = exp(-(r * r) / (2.0 * _HaloSigmaOuter * _HaloSigmaOuter)) * _HaloAlphaOuter;

                // 光晕从圆盘内部就开始渗入，和圆盘边缘重叠，避免出现一圈暗环。
                float haloMask = smoothstep(
                    _CoreRadius - _CoreSoftness * 4.0,
                    _CoreRadius + _CoreSoftness,
                    r);
                float edgeFade = 1.0 - smoothstep(_HaloEdgeFade, 1.0, r);
                inner *= haloMask * edgeFade;
                outer *= haloMask * edgeFade;

                float3 innerColor = lerp(baseColor, white, _InnerWhiten);
                float3 rgb = coreColor * core + innerColor * inner + baseColor * outer;

                float alpha = saturate(core + inner + outer) * i.color.a;
                rgb *= _Brightness;

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
