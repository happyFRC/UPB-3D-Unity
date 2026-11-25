Shader "UPB/SandboxPlanet"
{
    Properties
    {
        [MainTexture] _SurfaceMap ("Surface Map", 2D) = "white" {}
        _AtmosphereMap ("Atmosphere Map", 2D) = "black" {}
        _TemperatureMap ("Surface Temperature Map", 2D) = "black" {}
        _TemperatureValueMap ("Surface Temperature Value Map", 2D) = "black" {}
        _PatchEmissionStrength ("Patch Emission Strength", Float) = 0
        _HeightMap ("Height Map", 2D) = "black" {}
        _HasHeightMap ("Has Height Map", Float) = 0
        _LandHeightMaxM ("Land Height Max M", Float) = 20000
        _WaterDepthM ("Water Depth M", Float) = 0
        _WaterColor ("Water Color", Color) = (0.1137255,0.1725490,0.2745098,1)
        _HasWater ("Has Water", Float) = 0
        _FreezePointK ("Freeze Point K", Float) = 273.15
        _BoilPointK ("Boil Point K", Float) = 373.15
        _IceColor ("Ice Color", Color) = (0.78,0.80,0.82,1)
        _SnowColor ("Snow Color", Color) = (1,1,1,1)
        _AtmosphereStrength ("Atmosphere Strength", Float) = 0
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _AmbientColor ("Ambient", Color) = (0.008,0.010,0.016,1)
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionStrength ("Emission Strength", Float) = 0
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
            Name "PlanetForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_SurfaceMap);
            SAMPLER(sampler_SurfaceMap);
            TEXTURE2D(_AtmosphereMap);
            SAMPLER(sampler_AtmosphereMap);
            TEXTURE2D(_TemperatureMap);
            SAMPLER(sampler_TemperatureMap);
            TEXTURE2D(_TemperatureValueMap);
            SAMPLER(sampler_TemperatureValueMap);
            TEXTURE2D(_HeightMap);
            SAMPLER(sampler_HeightMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _SurfaceMap_ST;
                float4 _AtmosphereMap_ST;
                half4 _BaseColor;
                half4 _AmbientColor;
                half4 _EmissionColor;
                float _EmissionStrength;
                float _AtmosphereStrength;
                float _HasHeightMap;
                float _LandHeightMaxM;
                float _WaterDepthM;
                half4 _WaterColor;
                half4 _IceColor;
                half4 _SnowColor;
                float _HasWater;
                float _FreezePointK;
                float _BoilPointK;
                float _PatchEmissionStrength;
            CBUFFER_END

            // 没有恒星时的方向光全局参数。
            float _SandboxFallbackLightOn;
            float4 _SandboxFallbackLightDir;
            half4 _SandboxFallbackLightColor;

            // 全局恒星数据纹理：256x2。
            // 第 0 行 = xyz 世界位置 + w 光度；第 1 行 = 黑体颜色。
            // 用纹理而不是 shader 数组，避免 URP 下数组只绑定第 0 个元素的问题。
            #define MAX_STAR_LIGHTS 256
            TEXTURE2D(_SandboxStarData);
            float _SandboxStarCount;
            float _SandboxUnitsPerAu;
            // 由 SandboxStar.IlluminationFadeStartAu / IlluminationFadeEndAu 写入。
            float _SandboxLightFadeStartAu;
            float _SandboxLightFadeEndAu;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 normalOS : TEXCOORD4;
                float2 uv : TEXCOORD2;
                float2 uvAtmosphere : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.normalOS = normalize(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _SurfaceMap);
                o.uvAtmosphere = TRANSFORM_TEX(v.uv, _AtmosphereMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 surface = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, i.uv)
                                * _BaseColor;
                half4 atmosphere = SAMPLE_TEXTURE2D(
                    _AtmosphereMap, sampler_AtmosphereMap, i.uvAtmosphere);

                // 大气层覆盖在地表之上：大气贴图的 alpha 决定覆盖强度。
                // 没有大气贴图时 _AtmosphereStrength = 0，完全不影响地表。
                float atmosphereAlpha = saturate(atmosphere.a * _AtmosphereStrength);
                half4 albedo = surface;

                // 每个纬度/经度斑块独立温度对应的黑体颜色。
                // 用物体空间法线采样，避免球体 UV 接缝/自转方向导致昼夜面反掉。
                float3 nOS = normalize(i.normalOS);
                float patchLon = atan2(nOS.z, nOS.x);
                float patchLat = asin(clamp(nOS.y, -1.0, 1.0));
                // 纹理 x=0 对应经度 0；不要再加 0.5，否则会整体偏 180°。
                float2 patchUV = float2(
                    frac(patchLon / 6.28318530718),
                    saturate(patchLat / 3.14159265359 + 0.5));
                float patchTempK = SAMPLE_TEXTURE2D(
                    _TemperatureValueMap, sampler_TemperatureValueMap, patchUV).r;

                // 高程图：黑色 = 0 km，白色 = 20 km；水深为 0 时完全不显示水。
                float heightValue = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, i.uv).r;
                float elevationM = heightValue * _LandHeightMaxM;
                float depthOn = step(0.001, _WaterDepthM);
                float flooded = step(elevationM, _WaterDepthM) * depthOn;
                float waterMask = flooded * _HasHeightMap;
                // 超过该压力/浓度下的沸点：水蒸发。
                waterMask *= 1.0 - step(_BoilPointK, patchTempK);

                // 结冰判据：低于冰点再 15K 才开始积雪/结冰。
                float cold = 1.0 - step(_FreezePointK - 15.0, patchTempK);
                float snow = cold * _HasWater;
                float landMask = _HasHeightMap * (1.0 - flooded) * _HasWater;
                albedo.rgb = lerp(albedo.rgb, _SnowColor.rgb, landMask * snow);
                albedo.rgb = lerp(albedo.rgb, _WaterColor.rgb, waterMask);
                albedo.rgb = lerp(albedo.rgb, _IceColor.rgb, waterMask * snow);

                albedo.rgb = lerp(albedo.rgb, atmosphere.rgb, atmosphereAlpha);

                half4 patch = SAMPLE_TEXTURE2D(_TemperatureMap, sampler_TemperatureMap, patchUV);
                float patchGlow = saturate(patch.a);
                albedo.rgb = lerp(albedo.rgb, patch.rgb, patchGlow * 0.35);

                half3 n = normalize(i.normalWS);

                half3 lit = half3(0, 0, 0);
                int count = (int)clamp(_SandboxStarCount, 0.0, (float)MAX_STAR_LIGHTS);
                float unitsPerAu = max(_SandboxUnitsPerAu, 1.0);

                // 没有恒星时用一盏几何方向光替代恒星阵列，保证空场景创建行星可见。
                if (count == 0 && _SandboxFallbackLightOn > 0.5) {
                    half3 fallbackDir = normalize((half3)_SandboxFallbackLightDir.xyz);
                    half fallbackNdl = saturate(dot(n, fallbackDir));
                    lit += _SandboxFallbackLightColor.rgb * fallbackNdl;
                }

                for (int k = 0; k < count; k++)
                {
                    // 直接读取像素，不经过 sampler/filter，避免数组或采样器绑定的问题。
                    float4 starPos = LOAD_TEXTURE2D(_SandboxStarData, int2(k, 0));
                    float4 starColor = LOAD_TEXTURE2D(_SandboxStarData, int2(k, 1));

                    float3 toStar = starPos.xyz - i.positionWS;
                    float distanceUnits = length(toStar);
                    if (distanceUnits < 1e-9) continue;

                    float distanceAu = max(distanceUnits / unitsPerAu, 1e-9);
                    float luminosity = max(starPos.w, 1e-8);

                    // 与 C#（SandboxStar.IlluminationFadeStartAu/EndAu）一致的规则：
                    // 1) 太阳在 1 AU 处亮度 = 1；
                    // 2) 阈值按 √L 缩放的等效距离判断：90~900 AU 平滑淡出，更远完全照不亮；
                    // 3) 单颗恒星封顶 1.2，避免近距离过曝。
                    float fadeStart = _SandboxLightFadeStartAu > 0.0
                        ? _SandboxLightFadeStartAu : 90.0;
                    float fadeEnd = _SandboxLightFadeEndAu > fadeStart + 0.001
                        ? _SandboxLightFadeEndAu : fadeStart + 810.0;
                    float irradiance = luminosity / (distanceAu * distanceAu);
                    float effectiveAu = distanceAu / sqrt(luminosity);
                    float t = saturate((effectiveAu - fadeStart) / (fadeEnd - fadeStart));
                    float farFade = 1.0 - smoothstep(0.0, 1.0, t);
                    float brightness = min(irradiance, 1.2) * farFade;
                    if (brightness <= 0.0001) continue;

                    half3 l = (half3)(toStar / distanceUnits);
                    half ndl = saturate(dot(n, l));
                    lit += (half3)starColor.rgb * brightness * ndl;
                }

                // 软压缩，防止多颗恒星叠加后死白。
                half3 compressed = lit / (half3(1, 1, 1) + lit * 0.55);
                half3 ambient = albedo.rgb * _AmbientColor.rgb + half3(0.010, 0.011, 0.014);

                // 行星自身的黑体自发光（高温）。
                half3 emission = _EmissionColor.rgb * _EmissionStrength;
                emission += patch.rgb * patchGlow * _PatchEmissionStrength;

                return half4(albedo.rgb * (compressed + ambient) + emission, albedo.a);
            }
            ENDHLSL
        }
    }

    // 不再回退到 URP Lit：本 Shader 已强制打进 Player。
    // 如果这里写 URP Lit，Always Included 会把 URP Lit 的全部关键字变体一起拉进来。
    FallBack Off
}
