/// <summary>
/// 沙盘统一尺度：1 Unity 单位 = 92760 km。
/// 恒星、行星、轨道、距离 HUD、网格都从这里取，不再各文件写死 92760 / 15 / 2。
/// </summary>
public static class CelestialScale {
    public const double KmPerUnit = 92760.0;
    public const double AuKm = 149597870.7;
    public const double UnitsPerAu = AuKm / KmPerUnit;

    public const double SunRadiusKm = 695700.0;
    public const double EarthRadiusKm = 6371.0;

    /// <summary>太阳直径对应的 Unity 单位（1 R☉ 球体的 scale）。</summary>
    public const double SunDiameterUnits = 2.0 * SunRadiusKm / KmPerUnit;
    /// <summary>地球直径对应的 Unity 单位（1 Rearth 球体的 scale）。</summary>
    public const double EarthDiameterUnits = 2.0 * EarthRadiusKm / KmPerUnit;

    public static double KmToUnits(double km) => km / KmPerUnit;
    public static double UnitsToKm(double units) => units * KmPerUnit;

    /// <summary>半径(km) -> 半径(Unity 单位)。</summary>
    public static double RadiusToUnits(double radiusKm) => radiusKm / KmPerUnit;
    /// <summary>半径(km) -> 直径(Unity 单位)，也是球体 transform.localScale。</summary>
    public static double DiameterToUnits(double radiusKm) => 2.0 * radiusKm / KmPerUnit;

    public static double AuToUnits(double au) => au * UnitsPerAu;
    public static double UnitsToAu(double units) => units / UnitsPerAu;
}
