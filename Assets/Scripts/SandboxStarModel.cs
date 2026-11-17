using System;
using UPB.SSE;
using UnityEngine;

/// <summary>
/// 一个恒星在某个年龄下的“快照”。
/// 质量/金属度/年龄是输入，半径/温度/光度/密度是 SSE 模型算出来的只读结果。
/// </summary>
public class SandboxStarSnapshot {
    public double mass;          // 当前质量，M☉
    public double metallicity;   // Z（质量分数）
    public double ageMyr;        // 年龄，Myr
    public double radiusRsun;    // 半径，R☉
    public double temperatureK;  // 表面温度，K
    public double luminosityLsun;// 光度，L☉
    public double densityGcm3;   // 平均密度，g/cm³
    public double lifespanMyr;   // 该恒星的总寿命，Myr
    public double coreTemperatureK; // SSE 的近似中心温度，K（不可用时为 0）
    public double rotationPeriodDays = 25.38; // 自转周期，天
    public string spectralType;

    /// <summary>场景里使用的视觉直径：1 R☉ 对应 15 个 Unity 单位（和现有恒星场景一致）。</summary>
    public double VisualDiameter => radiusRsun * SandboxStarModel.VisualUnitsPerRsun;

    /// <summary>场景里使用的视觉半径。</summary>
    public double VisualRadius => VisualDiameter * 0.5;
}

/// <summary>
/// 用项目里的 SSE 引擎计算恒星在给定 (质量, 金属度, 年龄) 下的状态。
/// 只读参数全部由这里生成，UI 不直接改它们。
/// </summary>
public static class SandboxStarModel {
    public const double MinMass = 0.077;
    public const double MaxMass = 100.0;
    public const double MinMetallicity = 1e-4;
    public const double MaxMetallicity = 0.03;
    /// <summary>1 R☉ 球体在场景里的直径（Unity 单位），由 92760 km/单位统一换算。</summary>
    public const double VisualUnitsPerRsun = CelestialScale.SunDiameterUnits;

    public const double SunMass = 1.0;
    public const double SunRadiusKm = CelestialScale.SunRadiusKm;
    public const double SunLuminosityW = 3.828e26;
    public const double EarthMassPerSun = 332946.0487;
    public const double EarthRadiusPerSun = CelestialScale.SunRadiusKm / CelestialScale.EarthRadiusKm;
    /// <summary>1 M☉ / 1 R☉³ 换算到 g/cm³ 的系数（再除以 4/3πR³）。</summary>
    public const double SolarDensityGcm3 = 5.905;
    public const double SunSurfaceK = 5773.15;

    /// <summary>
    /// 计算恒星快照。失败时返回 false，error 里是给用户看的中文原因。
    /// </summary>
    public static bool TryCompute(
            double mass, double metallicity, double ageMyr,
            out SandboxStarSnapshot snapshot, out string error) {
        return TryCompute(mass, metallicity, ageMyr, 25.38, out snapshot, out error);
    }

    public static bool TryCompute(
            double mass, double metallicity, double ageMyr, double rotationPeriodDays,
            out SandboxStarSnapshot snapshot, out string error) {
        snapshot = null;
        error = null;

        if (double.IsNaN(mass) || double.IsInfinity(mass) ||
            mass < MinMass || mass > MaxMass) {
            error = $"质量超出模型范围（{MinMass} ~ {MaxMass} Msun）";
            return false;
        }

        if (double.IsNaN(metallicity) || double.IsInfinity(metallicity) ||
            metallicity < MinMetallicity || metallicity > MaxMetallicity) {
            error = $"金属度超出模型范围（{MinMetallicity:0.####} ~ {MaxMetallicity:0.###}）";
            return false;
        }

        if (double.IsNaN(ageMyr) || double.IsInfinity(ageMyr) || ageMyr < 0.0) {
            error = "年龄不能为负数";
            return false;
        }

        try {
            Track track = new();
            int rc = SseEngine.engine_evolve(mass, metallicity, 0.0, 0.0, 1, 1, track);
            if (rc != 0 || track == null || track.last == null) {
                error = "恒星演化模型计算失败" +
                        (string.IsNullOrEmpty(track?.error) ? "" : "：" + track.error);
                return false;
            }

            double lifespan = track.last.time;
            if (ageMyr > lifespan + 1e-3) {
                error = $"年龄不能超过该恒星的寿命（约 {lifespan:F0} Myr）";
                return false;
            }

            // SSE 输出的是离散轨迹点。相邻两个点之间做线性插值，
            // 这样 5 Myr 这种小于采样间隔的年龄差也能反映到参数上。
            TrackPoint before = null;
            TrackPoint after = null;

            if (track.points != null && track.points.Count > 0) {
                for (int i = 0; i < track.points.Count; i++) {
                    TrackPoint candidate = track.points[i];
                    if (candidate.time <= ageMyr + 1e-9) {
                        before = candidate;
                    } else {
                        after = candidate;
                        break;
                    }
                }

                if (before == null) before = track.points[0];
                if (after == null) after = before;
            } else {
                before = track.last;
                after = track.last;
            }

            double radius;
            double temperature;
            double luminosity;
            double currentMass;
            double coreTemperature;

            double dt = after.time - before.time;
            if (dt > 1e-12) {
                double f = (ageMyr - before.time) / dt;
                if (f < 0.0) f = 0.0;
                if (f > 1.0) f = 1.0;

                radius = Lerp(before.radius, after.radius, f);
                temperature = Lerp(before.temperature, after.temperature, f);
                luminosity = Lerp(before.luminosity, after.luminosity, f);
                currentMass = Lerp(
                    before.mass > 0.0 ? before.mass : mass,
                    after.mass > 0.0 ? after.mass : mass,
                    f);
                coreTemperature = Lerp(before.core_temperature, after.core_temperature, f);
            } else {
                radius = before.radius;
                temperature = before.temperature;
                luminosity = before.luminosity;
                currentMass = before.mass > 0.0 ? before.mass : mass;
                coreTemperature = before.core_temperature;
            }

            if (radius <= 0.0 || temperature <= 0.0) {
                error = "恒星演化模型返回了无效的半径或温度";
                return false;
            }

            double volumeSolar = 4.0 / 3.0 * Math.PI * radius * radius * radius;
            double density = volumeSolar > 0.0
                ? currentMass / volumeSolar * SolarDensityGcm3
                : 0.0;

            string spectral = Spectrum.GetSpectrum(
                radius, currentMass, temperature).ToString();

            snapshot = new SandboxStarSnapshot {
                mass = mass,
                metallicity = metallicity,
                ageMyr = ageMyr,
                radiusRsun = radius,
                temperatureK = temperature,
                luminosityLsun = luminosity,
                densityGcm3 = density,
                lifespanMyr = lifespan,
                coreTemperatureK = coreTemperature,
                rotationPeriodDays = rotationPeriodDays > 0.0 ? rotationPeriodDays : 25.38,
                spectralType = spectral,
            };
            return true;
        }
        catch (Exception e) {
            error = "恒星演化模型计算失败：" + e.Message;
            return false;
        }
    }

    private static double Lerp(double a, double b, double t) {
        return a + (b - a) * t;
    }
}
