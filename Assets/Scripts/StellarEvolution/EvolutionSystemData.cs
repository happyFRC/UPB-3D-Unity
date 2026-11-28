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

/// <summary>一个恒星系。内置的由 SandboxRegister 注册（BuiltinSandboxObjects）；玩家自建的仍可另存为 JSON。</summary>
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

    /// <summary>默认太阳系：从 SandboxRegister 的注册表里取。</summary>
    public static EvolutionSystemData SolarSystem() {
        EvolutionSystemData system = SandboxRegister.FindEvolutionSystem(SolarSystemName);
        if (system != null) return system;
        EvolutionSystemData[] all = SandboxRegister.EvolutionSystems;
        return all.Length > 0 ? all[0] : new EvolutionSystemData { name = SolarSystemName };
    }
}
