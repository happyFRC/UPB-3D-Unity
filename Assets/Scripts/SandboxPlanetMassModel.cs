using System;

/// <summary>
/// 行星“改质量 → 改半径/温度”的模型。
///
/// 公式（M 用木星质量 M_Jup）：
///   f(M) = 10^(0.073 · (log10 M − (log10 M)²))
///   半径：R(km) = k · f(M)
///     · 绝对基准：k0 = 695700/10 = 69570 km（等价于 R(R☉)=f(M)/10）
///     · A 类（目录里已有的行星）：k = R_catalog / f(M_catalog)，由自身质量/半径反推，
///       保证 Mass 不变时半径就是目录半径。
///     · B 类（被改造成气态巨行星的）：k = 70000 km。
///
/// 阈值：
///   · M &gt; 77 M_Jup → 恒星（走恒星模型，金属度默认 0.02）
///   · 10 M_E &lt; M ≤ 77 M_Jup → 气态巨行星，表面温度由聚变功率反推
///   · M ≤ 10 M_E（且原本不是岩石行星）→ 岩石化：贴图换成 Random_RockyPlanet，
///     密度固定为“k=70000 km 时 M=10 M_E 对应的半径”算出的密度，之后 R 由 M/ρ 反算。
/// </summary>
public static class SandboxPlanetMassModel {
    public const double EarthMassKg = 5.9722e24;
    public const double JupiterMassEarth = 317.8;
    public const double SunMassEarth = 332946.0;
    public const double SunMassJup = 0.0009538;
    public const double EarthRadiusKm = 6371.0;
    public const double SunRadiusKm = 695700.0;

    /// <summary>超过这个质量就变成恒星。</summary>
    public const double StarThresholdJup = 77.0;
    /// <summary>超过这个质量才算气态巨行星。</summary>
    public const double GasGiantMinJup = 10.0;
    /// <summary>低于这个质量（且不是原生岩石行星）会被岩石化。</summary>
    public const double RockyMaxEarth = 10.0;

    /// <summary>B 类（改造型气态行星）的基准半径。</summary>
    public const double ConvertedGasRadiusKm = 70000.0;

    /// <summary>绝对基准：R(R☉) = f(M)/10。</summary>
    public const double AbsoluteScaleKm = SunRadiusKm / 10.0;

    /// <summary>f(M) = 10^(0.073·(log10 M − log10² M))，M 单位 M_Jup。</summary>
    public static double ShapeFactor(double massJup) {
        double log = Math.Log10(Math.Max(massJup, 1e-12));
        return Math.Pow(10.0, 0.073 * (log - log * log));
    }

    public static double MassJupToEarth(double massJup) => massJup * JupiterMassEarth;

    public static double MassEarthToJup(double massEarth) =>
        massEarth / JupiterMassEarth;

    /// <summary>A 类：由目录质量/半径反推比例系数 k（单位 km）。</summary>
    public static double ScaleFromCatalog(double massEarth, double radiusEarth) {
        double massJup = MassEarthToJup(Math.Max(massEarth, 1e-12));
        double radiusKm = Math.Max(radiusEarth, 1e-9) * EarthRadiusKm;
        return radiusKm / Math.Max(ShapeFactor(massJup), 1e-12);
    }

    /// <summary>给定比例系数，算半径（km）。</summary>
    public static double RadiusKm(double massEarth, double scaleKm) {
        return scaleKm * ShapeFactor(MassEarthToJup(Math.Max(massEarth, 1e-12)));
    }

    /// <summary>气态巨行星的表面温度：由聚变光度反推（L = (M/1000)^3.6 L☉）。</summary>
    public static double GasGiantTemperatureK(double massJup, double radiusKm) {
        double luminositySun = Math.Pow(Math.Max(massJup, 1e-9) / 1000.0, 3.6);
        double luminosityW = luminositySun * 3.828e26;
        double radiusM = Math.Max(radiusKm, 1.0) * 1000.0;
        double area = 4.0 * Math.PI * radiusM * radiusM;
        if (area <= 0.0) return 0.0;
        return Math.Pow(luminosityW / (area * 5.670374419e-8), 0.25);
    }

    /// <summary>岩石行星固定密度：3.5 g/cm³。</summary>
    public const double RockyDensityKgM3Value = 3500.0;

    public static double RockyDensityKgM3() => RockyDensityKgM3Value;

    /// <summary>岩石行星半径（km）：给定质量和固定密度。</summary>
    public static double RockyRadiusKm(double massEarth) {
        double massKg = Math.Max(massEarth, 1e-12) * EarthMassKg;
        double density = RockyDensityKgM3();
        double volume = massKg / Math.Max(density, 1e-9);
        double radiusM = Math.Pow(3.0 * volume / (4.0 * Math.PI), 1.0 / 3.0);
        return radiusM / 1000.0;
    }

    /// <summary>10 M_E 处的岩石半径（km），方便面板/日志查看。</summary>
    public static double RockyBoundaryRadiusKm() => RadiusKm(RockyMaxEarth, ConvertedGasRadiusKm);
}
