Shader "UPB/AntialiasedGrid"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _GridLineColor ("Line Color", Vector) = (1, 1, 1, 1)
        [HideInInspector] _GridAxisColor ("Axis Color", Vector) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _GridLineColor;
                float4 _GridAxisColor;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 gridUV : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 gridPosition : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.gridPosition = float3(input.gridUV, output.positionCS.w);
                return output;
            }

            float2 UnitsPerPixel(float2 position)
            {
                float2 dx = ddx(position);
                float2 dy = ddy(position);
                return max(sqrt(dx * dx + dy * dy), 1e-8);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 grid = input.gridPosition.xy;
                float2 footprint = UnitsPerPixel(grid);
                float2 distanceToLine = abs(frac(grid + 0.5) - 0.5);
                float2 lines = 1.0 - saturate(distanceToLine / footprint);
                // Fade each direction between four and two pixels per cell, before it aliases.
                lines *= 1.0 - smoothstep(0.25, 0.5, footprint);

                // Dividing by clip W gives screen-linear coordinates for the non-repeating axes.
                float2 axis = grid / input.gridPosition.z;
                float2 axes = 1.0 - saturate(abs(axis) / UnitsPerPixel(axis));

                float lineAlpha = max(lines.x, lines.y) * _GridLineColor.a * _Color.a;
                float axisAlpha = max(axes.x, axes.y) * _GridAxisColor.a * _Color.a;
                lineAlpha *= 1.0 - axisAlpha;
                float3 rgb = _Color.rgb * (_GridLineColor.rgb * lineAlpha + _GridAxisColor.rgb * axisAlpha);
                return half4(rgb, lineAlpha + axisAlpha);
            }
            ENDHLSL
        }
    }
}
