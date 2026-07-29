Shader "Custom/ToonShader"
{
    Properties
    {
        _Color ("主颜色", Color) = (1, 1, 1, 1)
        _ColorShadow ("阴影颜色", Color) = (0.3, 0.3, 0.5, 1)
        _GlossinessColor ("高光颜色", Color) = (1, 1, 1, 1)
        _Glossiness ("高光强度", Range(1, 256)) = 50
        _MainTex ("主纹理", 2D) = "white" {}
        _fresnelScale ("菲涅尔缩放", Range(0, 5)) = 1.0
        _fresnelBase ("菲涅尔基础", Range(0, 1)) = 0.1
        _fresnelIndensity ("菲涅尔指数", Range(1, 10)) = 3.0
        _fresnelColor ("菲涅尔颜色", Color) = (1, 1, 1, 1)
        _OutlineStrength ("描边宽度", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            float4 _Color;
            float4 _ColorShadow;
            float4 _GlossinessColor;
            float _Glossiness;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _fresnelScale;
            float _fresnelBase;
            float _fresnelIndensity;
            float4 _fresnelColor;

            struct a2v
            {
                float4 vertex : POSITION;
                fixed3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed3 worldNormal : TEXCOORD0;
                float4 worldSpacePos : COLOR;
                float2 uv : TEXCOORD2;
                SHADOW_COORDS(1)
            };

            v2f vert(a2v v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldSpacePos = mul(unity_ObjectToWorld, v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag(v2f v) : SV_Target
            {
                fixed3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float diff = dot(v.worldNormal, lightDir);

                fixed4 textureColor = tex2D(_MainTex, v.uv);

                float3 viewDir = normalize(_WorldSpaceCameraPos - v.worldSpacePos.xyz);

                fixed3 reflectDir = normalize(reflect(-lightDir, v.worldNormal));
                float reflectDotView = dot(viewDir, reflectDir);
                float spec = pow(max(reflectDotView, 0), _Glossiness);
                float4 specular = 0;
                if (spec > 0.7)
                    specular = float4(_GlossinessColor.rgb, 1);

                float shadow = SHADOW_ATTENUATION(v);

                float fresnel = _fresnelBase + _fresnelScale * pow(1 - dot(viewDir, v.worldNormal), _fresnelIndensity);

                fixed4 diffuseColor;
                if (diff < 0 || shadow < 0.95)
                {
                    diffuseColor = (_ColorShadow * textureColor) * _LightColor0;
                    return diffuseColor;
                }
                else
                {
                    diffuseColor = (_Color * textureColor + specular) * _LightColor0;
                    return lerp(diffuseColor, diffuseColor * (1 - _fresnelColor.a) + _fresnelColor * _fresnelColor.a, fresnel);
                }
            }
            ENDCG
        }

        Pass
        {
            Name "OUTLINE"
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineStrength;

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : POSITION;
            };

            v2f vert(a2v v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                float2 offset = TransformViewToProjection(norm.xy);
                o.pos.xy += offset * _OutlineStrength;
                return o;
            }

            fixed4 frag(v2f i) : COLOR
            {
                return fixed4(0, 0, 0, 1);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}