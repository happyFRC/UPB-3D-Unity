using System;
using UnityEngine;

[Serializable]
public class PlanetData {
    public string name;
    public double temperature;      // K
    public double mass;             // Earth masses
    public double radius;           // Earth radii
    public double atmosphereMass;   // Earth atmosphere masses
    public double rotationPeriodDays = 1.0; // 自转周期（天）
    public int rotationDirection = 1; // +1 顺行，-1 逆行；缺省同地球
    public double axialTiltDeg;       // 自转轴倾角（度），0 = 不自转轴倾斜
    public double greenhouseFactor; // 0..1，大气温室效应比例
    public string heightMap;        // Resources path，高程图
    public double surfaceWaterDepthM; // 高程图上的水面深度（m）
    public double landHeightMaxM = 20000.0; // 高程图白色对应的高度
    public double soluteConcentrationMmolL; // 溶质浓度，mmol/L
    public double orbitRadiusKm;            // 卫星/伴星的轨道半径（km），默认场景摆放用
    public float atmosphereColorR = 0.55f;
    public float atmosphereColorG = 0.75f;
    public float atmosphereColorB = 1f;
    public float trailColorR = 200f / 255f;
    public float trailColorG = 200f / 255f;
    public float trailColorB = 200f / 255f;

    /// <summary>地表贴图（null = 不显示）。</summary>
    public Resource surface;

    /// <summary>大气贴图，叠加在 surface 之上（null = 不显示）。</summary>
    public Resource atmosphere;

    // 有大气贴图时默认只显示贴图（金星）；置 true 会额外保留外圈光晕（地球：云层 + 光晕）。
    public bool atmosphereShell;

    // 旧版单贴图字段：只有没有 surface 时才作为兼容回退。
    public string texture;

    public string SurfaceTexture {
        get {
            string path = surface != null ? surface.ToString() : null;
            return !string.IsNullOrEmpty(path) ? path : texture;
        }
    }

    public string AtmosphereTexture => atmosphere != null ? atmosphere.ToString() : string.Empty;

    // ~~~ 彩蛋（长方体，继承行星类）~~~
    public bool isBox;
    public double boxXKm;
    public double boxYKm;
    public double boxZKm;
    public string textureFront;
    public string textureSides;
    public string textureTopBackBottom;
    public bool ignoreGravity;
    public bool ignoreCollision;
    public bool ignoreThermal;
    public double impactGiftSun;

    /// <summary>
    /// 特殊行星分类。填 "SpecialPlanets" 表示不参与“改质量→变气态/变恒星”那套规则。
    /// </summary>
    public string category;
}

public class SandboxPlanetSnapshot {
    public double massEarth;
    public double radiusEarth;
    public double atmosphereMassEarth;
    public double temperatureK;
    public double rotationPeriodDays;
    public int rotationDirection = 1;
    public double axialTiltDeg;
    public double greenhouseFactor;
    public string heightMapPath;
    public double surfaceWaterDepthM;
    public double landHeightMaxM;
    public double soluteConcentrationMmolL;
    public Color atmosphereColor = new(0.55f, 0.75f, 1f, 1f);
    public Color trailColor = new(200f / 255f, 200f / 255f, 200f / 255f, 1f);
    public double densityGcm3;
    public double gravityG;
    public double pressureAtm;
    public double visualDiameter;
    public float emissionStrength;
    public Color emissionColor;
    public string surfaceTexturePath;
    public string atmosphereTexturePath;
    public bool atmosphereShell;

    // 彩蛋（长方体）：不按球体渲染，物理行为也可单独关掉。
    public bool isBox;
    public double boxXKm;
    public double boxYKm;
    public double boxZKm;
    public string textureFront;
    public string textureSides;
    public string textureTopBackBottom;
    public bool ignoreGravity;
    public bool ignoreCollision;
    public bool ignoreThermal;
    public double impactGiftSun;

    /// <summary>目录里的特殊分类（SpecialPlanets 等），空表示普通行星。</summary>
    public string category;

    public bool IsSpecial => !string.IsNullOrEmpty(category) &&
                             category == "SpecialPlanets";

    // 兼容旧代码：原来只有一个 texturePath，现在指地表层。
    public string texturePath => surfaceTexturePath;

    public SandboxPlanetSnapshot Clone() => (SandboxPlanetSnapshot)MemberwiseClone();

    public double VisualDiameter => visualDiameter;
}

/// <summary>
/// 行星数据模型。质量/半径使用地球单位；温度 K；大气质量使用地球大气质量。
/// 半径由质量 + 当前平均密度决定，密度/表面重力/表面气压都由这里自动算。
/// </summary>
public static class SandboxPlanetModel {
    public const double EarthRadiusKm = CelestialScale.EarthRadiusKm;
    public const double EarthDensityGcm3 = 5.5136;
    public const double EarthGravity = 9.80665;
    public const double EarthAtmosphereKg = 5.148e18;
    public const double SunSurfaceK = 5773.15;

    /// <summary>1 Rearth 球体在场景里的直径（Unity 单位），由 92760 km/单位统一换算。</summary>
    public const double VisualDiameterPerEarthRadius = CelestialScale.EarthDiameterUnits;

    public static bool TryCreate(PlanetData data,
            out SandboxPlanetSnapshot snapshot, out string error) {
        snapshot = null;
        error = null;
        if (data == null) {
            error = "行星数据为空";
            return false;
        }
        if (!TryCompute(data.mass, data.radius, data.atmosphereMass, data.temperature,
                data.SurfaceTexture, data.AtmosphereTexture, out snapshot, out error)) {
            return false;
        }
        // 0 表示潮汐锁定/不自转；负数才回退到默认 1 天。
        snapshot.rotationPeriodDays = data.rotationPeriodDays >= 0.0
            ? data.rotationPeriodDays
            : 1.0;
        snapshot.rotationDirection = data.rotationDirection >= 0 ? 1 : -1;
        snapshot.axialTiltDeg = data.axialTiltDeg;
        snapshot.atmosphereShell = data.atmosphereShell;

        // 彩蛋字段直接透传（普通行星全是默认值）。
        snapshot.isBox = data.isBox;
        snapshot.boxXKm = data.boxXKm;
        snapshot.boxYKm = data.boxYKm;
        snapshot.boxZKm = data.boxZKm;
        snapshot.textureFront = data.textureFront;
        snapshot.textureSides = data.textureSides;
        snapshot.textureTopBackBottom = data.textureTopBackBottom;
        snapshot.ignoreGravity = data.ignoreGravity;
        snapshot.ignoreCollision = data.ignoreCollision;
        snapshot.ignoreThermal = data.ignoreThermal;
        snapshot.impactGiftSun = data.impactGiftSun;
        snapshot.category = data.category;
        snapshot.greenhouseFactor = Math.Clamp(data.greenhouseFactor, 0.0, 1.0);
        snapshot.heightMapPath = data.heightMap;
        snapshot.surfaceWaterDepthM = Math.Max(0.0, data.surfaceWaterDepthM);
        snapshot.landHeightMaxM = data.landHeightMaxM > 0.0 ? data.landHeightMaxM : 20000.0;
        snapshot.soluteConcentrationMmolL =
            Math.Clamp(data.soluteConcentrationMmolL, 0.0, 5400.0);
        snapshot.atmosphereColor = new Color(
            Mathf.Clamp01(data.atmosphereColorR),
            Mathf.Clamp01(data.atmosphereColorG),
            Mathf.Clamp01(data.atmosphereColorB), 1f);
        snapshot.trailColor = new Color(
            Mathf.Clamp01(data.trailColorR),
            Mathf.Clamp01(data.trailColorG),
            Mathf.Clamp01(data.trailColorB), 1f);
        return true;
    }

    public static bool TryCompute(double massEarth, double radiusEarth,
            double atmosphereMassEarth, double temperatureK,
            string surfaceTexturePath, string atmosphereTexturePath,
            out SandboxPlanetSnapshot snapshot, out string error) {
        snapshot = null;
        error = null;

        if (double.IsNaN(massEarth) || double.IsInfinity(massEarth) || massEarth <= 0.0) {
            error = "行星质量必须大于 0";
            return false;
        }
        if (double.IsNaN(radiusEarth) || double.IsInfinity(radiusEarth) || radiusEarth <= 0.0) {
            error = "行星半径必须大于 0";
            return false;
        }
        if (double.IsNaN(temperatureK) || double.IsInfinity(temperatureK) || temperatureK < 0.0) {
            error = "行星温度不能为负数";
            return false;
        }
        if (double.IsNaN(atmosphereMassEarth) || double.IsInfinity(atmosphereMassEarth) ||
            atmosphereMassEarth < 0.0) {
            error = "大气质量不能为负数";
            return false;
        }

        double volume = radiusEarth * radiusEarth * radiusEarth;
        double density = EarthDensityGcm3 * massEarth / volume;
        double gravity = massEarth / (radiusEarth * radiusEarth);
        double pressure = atmosphereMassEarth * massEarth /
                          (radiusEarth * radiusEarth * radiusEarth * radiusEarth);

        var (r, g, b) = Spectrum.GetBlackbodyColor(temperatureK);
        Color emissionColor = new(r, g, b, 1f);
        float emissionStrength = 0f;
        if (temperatureK >= 500.0) {
            emissionStrength = Mathf.Clamp01(
                Utils.GetStarEmissionStrength(temperatureK / SunSurfaceK) * 0.35f);
        }

        snapshot = new SandboxPlanetSnapshot {
            massEarth = massEarth,
            radiusEarth = radiusEarth,
            atmosphereMassEarth = atmosphereMassEarth,
            temperatureK = temperatureK,
            densityGcm3 = density,
            gravityG = gravity,
            pressureAtm = pressure,
            visualDiameter = radiusEarth * VisualDiameterPerEarthRadius,
            emissionStrength = emissionStrength,
            emissionColor = emissionColor,
            surfaceTexturePath = surfaceTexturePath,
            atmosphereTexturePath = atmosphereTexturePath
        };
        return true;
    }

    public static double RadiusForMass(double massEarth, double densityGcm3) {
        if (massEarth <= 0.0 || densityGcm3 <= 0.0) return 0.0;
        return Math.Cbrt(massEarth / densityGcm3 * EarthDensityGcm3);
    }
}
