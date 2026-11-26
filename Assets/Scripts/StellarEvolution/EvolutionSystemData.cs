using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>一颗行星的轨道数据。半长轴单位是 km，渲染时按 1 单位 = 92760 km 换算。</summary>
[Serializable]
public class EvolutionPlanetData {
    public string name;
    public double semiMajorAxisAu;
    public double semiMajorAxisKm;
    public double eccentricity;
    public float colorR;
    public float colorG;
    public float colorB;

    public Color Color => new(colorR, colorG, colorB, 1f);

    /// <summary>优先用 km；旧 JSON 只有 AU 时也能换算。</summary>
    public double EffectiveSemiMajorAxisKm => semiMajorAxisKm > 0.0
        ? semiMajorAxisKm
        : semiMajorAxisAu * EvolutionSystemDefaults.AuKm;
}

/// <summary>一个恒星系。保存到 Data/EvolutionSystems/[name].json。</summary>
[Serializable]
public class EvolutionSystemData {
    public string name;
    public List<EvolutionPlanetData> planets = new();
}

/// <summary>默认太阳系和公共换算。</summary>
public static class EvolutionSystemDefaults {
    public const double KmPerUnit = CelestialScale.KmPerUnit;
    public const double AuKm = CelestialScale.AuKm;
    public const double UnitsPerAu = CelestialScale.UnitsPerAu;
    public const string SolarSystemName = "太阳系";

    public static string PlanetName(int index) => $"Planet {index + 1:00}";

    public static EvolutionSystemData SolarSystem() {
        return new EvolutionSystemData {
            name = SolarSystemName,
            planets = new List<EvolutionPlanetData> {
                P("水星", 5.791e7, 0.2056, 0.50f, 0.50f, 0.50f),
                P("金星", 1.082e8, 0.0068, 0.96f, 0.87f, 0.70f),
                P("地球", 1.496e8, 0.0167, 0.53f, 0.81f, 0.92f),
                P("火星", 2.279e8, 0.0934, 0.88f, 0.25f, 0.16f),
                P("木星", 7.785e8, 0.0489, 0.55f, 0.35f, 0.20f),
                P("土星", 1.433e9, 0.0565, 0.82f, 0.70f, 0.40f),
                P("天王星", 2.872e9, 0.0457, 0.68f, 0.90f, 0.95f),
                P("海王星", 4.495e9, 0.0113, 0.20f, 0.35f, 0.75f),
            }
        };
    }

    private static EvolutionPlanetData P(
            string name, double aKm, double e, float r, float g, float b) {
        return new EvolutionPlanetData {
            name = name,
            semiMajorAxisAu = aKm / AuKm,
            semiMajorAxisKm = aKm,
            eccentricity = e,
            colorR = r,
            colorG = g,
            colorB = b
        };
    }
}
